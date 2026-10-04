import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

SPEC = importlib.util.spec_from_file_location('world_report', Path(__file__).parents[1] / 'scripts/report-world.py')
REPORT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(REPORT)


class WorldReportTests(unittest.TestCase):
    def record(self, seq, event='sample', **changes):
        value = dict(schema=1, stream='world', seq=seq, session='s', build='b', event=event,
                     level=1, gameTime=seq, wallSeconds=seq, data={})
        value.update(changes)
        return json.dumps(value) + '\n'

    def read(self, content):
        with tempfile.TemporaryDirectory() as folder:
            path = Path(folder) / 'world.jsonl'
            path.write_text(content)
            return REPORT.analyze([path])

    def test_valid_capture_is_not_automatic_gameplay_pass(self):
        result = self.read(self.record(1, 'session') + self.record(2, 'session-ended'))
        self.assertFalse(result['issues'])
        self.assertIn('INCONCLUSIVE', REPORT.render(result))

    def test_shutdown_loss_is_not_hidden(self):
        result = self.read(self.record(1, 'session-ended', data={'workerDrops': 1}))
        self.assertTrue(result['issues'])

    def test_malformed_trial_types_do_not_crash(self):
        self.assertIn('INCONCLUSIVE', REPORT.assess_trials([{'scenario': 'ordinary', 'mode': 'Off',
            'valid': True, 'durationSeconds': '60', 'frames': []}])[0])

    def test_malformed_sample_renders_safely(self):
        result = self.read(self.record(1, data={'entities': [None, {'data': []}]}) + self.record(2, 'session-ended'))
        self.assertIn('Latest observed state', REPORT.render(result))

    def test_paused_incident_uses_current_selection(self):
        result = self.read(self.record(1, data={'observer': 'old', 'entities': []}) +
            self.record(2, 'incident', data={'current': {'observer': 'current', 'target': 'sign', 'entities': []}}) +
            self.record(3, 'session-ended'))
        self.assertIn('observer: current; target: sign', REPORT.render(result))

    def test_boolean_schema_rejected(self):
        self.assertTrue(self.read(self.record(1, schema=True))['issues'])

    def test_gap_and_rotation_remain_incomplete(self):
        result = self.read(self.record(10, 'sample') + self.record(11, 'session-ended'))
        self.assertTrue(any('discontinuity' in x for x in result['issues']))

    def test_mixed_sessions(self):
        result = self.read(self.record(1) + self.record(2, 'session-ended', session='other'))
        self.assertIn('missing or mixed session/build identity', result['issues'])

    def test_truncated_record(self):
        result = self.read(self.record(1).rstrip())
        self.assertTrue(any('truncated' in x for x in result['issues']))

    def test_nan_rejected(self):
        result = self.read(self.record(1, gameTime=float('nan')))
        self.assertTrue(any('non-finite' in x for x in result['issues']))

    def test_duplicate_keys_rejected(self):
        result = self.read('{"schema":1,"schema":1}\n')
        self.assertTrue(any('duplicate key' in x for x in result['issues']))

    def test_loss_cannot_be_hidden_by_terminal(self):
        result = self.read(self.record(1, streamDropped=1) + self.record(2, 'session-ended'))
        self.assertIn('world writer dropped records', result['issues'])

    def test_lifecycle_requests_do_not_prove_destruction(self):
        result = self.read(self.record(1, 'effect-created', data={'key': 'effect'}) +
                           self.record(2, 'effect-retire-request', data={'key': 'effect'}) + self.record(3, 'session-ended'))
        self.assertEqual(1, result['paired_effects'])
        self.assertIn('retirement-request', REPORT.render(result))

    def test_benchmarks_need_both_conditions_and_repeats(self):
        self.assertTrue(all('INCONCLUSIVE' in x for x in REPORT.assess_trials([])))

    def test_regression_is_identified(self):
        trials = [dict(scenario='ordinary', mode=mode, valid=True, durationSeconds=60, frames=1800,
                       frameMedianMs=20, frameP95Ms=20 if mode == 'Off' else 25,
                       diagnosticMeanMs=.4, diagnosticP95Ms=.8)
                  for mode in ['Off', 'Inspect', 'Record'] for _ in range(3)]
        self.assertIn('NEEDS INVESTIGATION', REPORT.assess_trials(trials)[0])

    def test_no_baseline_headroom_cannot_certify(self):
        trials = [dict(scenario='ordinary', mode=mode, valid=True, durationSeconds=60, frames=1800,
                       frameMedianMs=35, frameP95Ms=40, diagnosticMeanMs=.2, diagnosticP95Ms=.5)
                  for mode in ['Off', 'Inspect', 'Record'] for _ in range(3)]
        self.assertIn('no measured 30 FPS headroom', REPORT.assess_trials(trials)[0])

    def test_negative_trial_timing_is_invalid(self):
        trials = [dict(scenario='ordinary', mode=mode, valid=True, durationSeconds=60, frames=1800,
                       frameMedianMs=10, frameP95Ms=-1, diagnosticMeanMs=.2, diagnosticP95Ms=.5)
                  for mode in ['Off', 'Inspect', 'Record'] for _ in range(3)]
        self.assertIn('invalid numeric', REPORT.assess_trials(trials)[0])


if __name__ == '__main__':
    unittest.main()
