"""Synthetic parser cases. These are report validation, not native gameplay evidence."""
import copy
import importlib.util
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location('report_trace', Path(__file__).parents[1] / 'scripts/report-trace.py')
report = importlib.util.module_from_spec(spec)
spec.loader.exec_module(report)


def rows():
    target = dict(uid=20, instance=200, name='fixture', position=[5.0, 0.0, 0.0])
    first = dict(target, rootGoal='GoalNoiseReact', rootNoiseId=7, rootNoiseInstance=700)
    moved = dict(first, position=[4.0, 0.0, 0.0])
    events = [
        ('session', 0, 0, {}),
        ('requested', 1, 0, dict(trigger='guard-test', cooldownRemaining=0.0, count=0, levelCap=6)),
        ('emitted', 1, 1, dict(source=dict(uid=10, instance=100, position=[0.0, 0.0, 0.0]),
                              target=target, noiseId=7, noiseInstance=700, spawned=True, observationSeconds=8)),
        ('reaction', 1, 1.1, dict(actor=target, noiseId=7, matchingNativeActivation=True)),
        ('sample', 1, 1.2, dict(actors=[first], targetRootMatchesNoise=True,
                                noiseIdObserved=7, noiseInstanceObserved=700)),
        ('sample', 1, 1.4, dict(actors=[moved], targetRootMatchesNoise=True,
                                noiseIdObserved=7, noiseInstanceObserved=700)),
        ('terminal', 1, 9.0, dict(status='PASS', reason='window-complete', approached=True, samples=2,
                                maxMatchingDisplacement=1, maxMatchingApproach=1, droppedRecords=0))]
    return [dict(schema=1, seq=index + 1, event=event, attempt=attempt, session='test', build='test-only',
                 levelId=1, gameTime=time, wallSeconds=time, data=data)
            for index, (event, attempt, time, data) in enumerate(events)]


def result(values):
    return report.analyze(values, [])[0][0]


class TraceReportTests(unittest.TestCase):
    def test_recomputes_numeric_movement(self):
        observed = result(rows())
        self.assertEqual(observed['status'], 'PASS')
        self.assertEqual(observed['maxApproach'], 1.0)
        self.assertEqual(observed['maxDisplacement'], 1.0)

    def test_sideways_motion_is_not_approach(self):
        values = rows()
        values[5]['data']['actors'][0]['position'] = [5.0, 1.0, 0.0]
        self.assertEqual(result(values)['status'], 'INCONCLUSIVE')

    def test_root_goal_alone_is_not_reaction(self):
        values = rows()
        values[3]['data']['matchingNativeActivation'] = False
        self.assertFalse(result(values)['targetReaction'])
        self.assertEqual(result(values)['status'], 'INCONCLUSIVE')

    def test_reaction_alone_is_not_movement(self):
        values = rows()
        values[5]['data']['targetRootMatchesNoise'] = False
        self.assertEqual(result(values)['status'], 'INCONCLUSIVE')

    def test_reused_noise_does_not_attribute_movement(self):
        for key in ('noiseIdObserved', 'noiseInstanceObserved'):
            values = rows()
            values[5]['data'][key] += 1
            self.assertEqual(result(values)['status'], 'INCONCLUSIVE')

    def test_reused_actor_does_not_attribute_movement(self):
        values = rows()
        values[5]['data']['actors'][0]['instance'] = 201
        self.assertEqual(result(values)['status'], 'INCONCLUSIVE')

    def test_missing_terminal_is_inconclusive(self):
        self.assertEqual(result(rows()[:-1])['status'], 'INCONCLUSIVE')

    def test_interruption_preserves_prior_positive_observation(self):
        values = rows()
        values[-1]['data'].update(status='INTERRUPTED', reason='combat-started')
        observed = result(values)
        self.assertEqual(observed['status'], 'INTERRUPTED')
        self.assertEqual(observed['maxApproach'], 1.0)

    def test_skip_without_emission(self):
        values = rows()[:2]
        values.append(dict(values[-1], seq=3, event='terminal', data=dict(status='SKIPPED', reason='no-pet')))
        self.assertEqual(result(values)['status'], 'SKIPPED')

    def test_premature_completion_does_not_pass(self):
        values = rows()
        values[-1]['gameTime'] = 2
        self.assertEqual(result(values)['status'], 'INCONCLUSIVE')

    def test_dropped_samples_and_changed_level_are_inconclusive(self):
        values = rows()
        values[-1]['data']['droppedRecords'] = 2
        self.assertEqual(result(values)['status'], 'INCONCLUSIVE')
        values = rows()
        values[-1]['levelId'] = 2
        self.assertEqual(result(values)['status'], 'INCONCLUSIVE')

    def test_duplicate_terminal_and_count_mismatch(self):
        values = rows()
        values.append(copy.deepcopy(values[-1]))
        self.assertEqual(result(values)['status'], 'INCONCLUSIVE')
        values = rows()
        values[-1]['data']['samples'] = 3
        self.assertEqual(result(values)['status'], 'INCONCLUSIVE')

    def test_inflated_summary_and_pre_goal_motion_do_not_pass(self):
        values = rows()
        values[-1]['data']['maxMatchingApproach'] = 99
        self.assertEqual(result(values)['status'], 'INCONCLUSIVE')
        values = rows()
        values[4]['data']['actors'][0]['position'] = [4.0, 0.0, 0.0]
        self.assertEqual(result(values)['maxApproach'], 0)
        self.assertEqual(result(values)['status'], 'INCONCLUSIVE')

    def test_large_gap_and_intervening_goal_exclude_motion(self):
        values = rows()
        values[5]['gameTime'] = 2.0
        self.assertEqual(result(values)['maxApproach'], 0)
        values = rows()
        extra = copy.deepcopy(values[4])
        extra['data']['targetRootMatchesNoise'] = False
        extra['gameTime'] = 1.3
        values.insert(5, extra)
        self.assertEqual(result(values)['maxApproach'], 0)

    def test_unity_single_precision_threshold(self):
        values = rows()
        values[1]['data']['trigger'] = 'manual'
        values[2]['data']['target']['position'] = [0.64, 0.0, 0.0]
        values[4]['data']['actors'][0]['position'] = [0.64, 0.0, 0.0]
        values[5]['data']['actors'][0]['position'] = [0.0, 0.0, 0.0]
        values[-1]['data'].update(maxMatchingDisplacement=0.64, maxMatchingApproach=0.64)
        self.assertEqual(result(values)['status'], 'PASS')

    def test_malformed_unknown_schema_and_sequence_gaps(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'trace.jsonl'
            values = rows()
            values[3]['seq'] = 50
            path.write_text('\n'.join(json.dumps(row) for row in values) + '\n{"schema":', encoding='utf-8')
            parsed, warnings = report.load(path)
            self.assertTrue(warnings)
            self.assertEqual(report.analyze(parsed, warnings)[0][0]['status'], 'INCONCLUSIVE')
            path.write_text('{"schema":2}\n', encoding='utf-8')
            self.assertEqual(report.load(path)[0], [])

    def test_nonfinite_and_duplicate_keys_rejected(self):
        with tempfile.TemporaryDirectory() as directory:
            path = Path(directory) / 'trace.jsonl'
            for content in ('{"schema":1,"schema":2}', '{"value":NaN}'):
                path.write_text(content, encoding='utf-8')
                self.assertEqual(report.load(path)[0], [])

    def test_no_claim_about_fun_or_audibility(self):
        text = report.render(Path('synthetic.jsonl'), [result(rows())], [])
        self.assertIn('Useful distraction, audible output and fun remain unverified', text)
        self.assertIn('recomputed maximum displacement/approach: 1.000/1.000', text)


if __name__ == '__main__':
    unittest.main()
