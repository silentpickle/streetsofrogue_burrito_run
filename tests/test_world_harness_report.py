import copy
import importlib.util
from pathlib import Path
import unittest

SPEC = importlib.util.spec_from_file_location('harness_report', Path(__file__).parents[1] / 'scripts/report-world-harness.py')
REPORT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(REPORT)


def evidence():
    events = []

    def event(name, data):
        events.append(dict(seq=len(events) + 1, event='native-test-' + name, level=1, data=data))

    event('start', dict(fixtureRevision=1, repeats=2))
    for repeat in (1, 2):
        key = f'1:100:20:{repeat}'
        event('fixture', dict(repeat=repeat, instance=100, uid=20, requestedGoal='DoNothing'))
        entity = dict(key=key, instance=100, nativeId=20, level=1, kind='Actor', name='Courier',
                      collection='agentList', active=True, destroyed=False, x=2., y=0., gameTime=10.,
                      data=dict(coverage='native-fields-at-sample', health=100, defaultGoal='DoNothing', rootGoal='GoalDoNothing'))
        queries = [dict(range=r, origin=[0., 0.], min=1.75 if r == 'Band' else 0,
                        max=1.75 if r == 'Outside' else 2.25, total=1, keys=[key])
                   for r in ('Within', 'Band', 'Outside', 'All')]
        state = dict(player=dict(health=100), actor=dict(health=100))
        event('observation', dict(repeat=repeat, native=entity, indexed=copy.deepcopy(entity), before=state,
                                 after=copy.deepcopy(state), queries=queries, errors=0, worldDrops=0, workerDrops=0,
                                 perception=dict(distance=2., clearLOS=True, insideCone=False, insideNativeRange=True),
                                 navigation=dict(actor=key, state=dict(rootGoal='GoalDoNothing')),
                                 tile=dict(available=True, possibleToWalk=True), cameraCount=1))
        event('cleanup', dict(repeat=repeat, key=key))
        event('retired', dict(repeat=repeat, key=key, nativeActive=False, indexedActive=False))
    event('ended', dict(status='COMPLETE'))
    return dict(issues=[], identities=[('test-session', 'test-build')], native_tests=events)


def performance_evidence():
    data = evidence()
    data['native_tests'][0]['data']['performance'] = True
    terminal = data['native_tests'].pop()
    terminal['data'].update(performance=True, trialsStarted=9)
    context = dict(level=1, controller=2, floor=1, playerInstance=3, playerUid=4, position=[0., 0., 0.],
                   health=100, combat=False, focused=True, cameras=[dict(CameraId=5)], nativeActors=30)
    data['trials'] = []
    for trial in range(1, 10):
        mode = ('Off', 'Inspect', 'Record')[(trial - 1) % 3]
        result = dict(mode=mode, scenario='harness-stationary', valid=True, reason=None, warmupSeconds=10,
                      durationSeconds=60.01, frames=3600, workerDropped=0, worldDropped=0, level=1,
                      frameMedianMs=16.7, frameP95Ms=17., frameP99Ms=18., diagnosticMeanMs=.3,
                      diagnosticP95Ms=.8, framesOver50Ms=0, nativeFreezeFrames=0)
        for kind, fields, wall in [('start', dict(mode=mode), trial * 71.),
                                   ('trial', dict(result=copy.deepcopy(result)), trial * 71. + 70.01)]:
            data['native_tests'].append(dict(seq=len(data['native_tests']) + 1, level=1, wallSeconds=wall,
                event='native-test-performance-' + kind, data=dict(trial=trial, context=copy.deepcopy(context), **fields)))
        data['trials'].append(result)
    terminal['seq'] = len(data['native_tests']) + 1
    data['native_tests'].append(terminal)
    return data


class PerformanceReportTests(unittest.TestCase):
    def test_nine_trials_pass_independent_gates(self):
        result = REPORT.assess(performance_evidence(), performance=True)
        self.assertEqual('PASS', result['status'])
        self.assertEqual(9, len(result['comparison']['trials']))

    def test_functional_only_cannot_pass_performance(self):
        self.assertEqual('INCOMPLETE/FAIL', REPORT.assess(evidence(), performance=True)['status'])

    def test_missing_duplicate_or_reordered_trial_fails(self):
        for change in ('missing', 'duplicate', 'reordered'):
            data = performance_evidence()
            if change == 'missing':
                data['native_tests'].pop(10)
            elif change == 'duplicate':
                data['native_tests'].append(copy.deepcopy(data['native_tests'][10]))
            else:
                data['native_tests'][10]['seq'] = 1
            self.assertEqual('INCOMPLETE/FAIL', REPORT.assess(data, performance=True)['status'])

    def test_short_trial_invalid_or_loss_fails(self):
        for field, value in [('durationSeconds', 59), ('warmupSeconds', 0), ('valid', False),
                             ('workerDropped', 1), ('diagnosticP95Ms', float('nan')), ('frames', True)]:
            data = performance_evidence()
            data['native_tests'][10]['data']['result'][field] = value
            self.assertEqual('INCOMPLETE/FAIL', REPORT.assess(data, performance=True)['status'])

    def test_context_changes_fail(self):
        for field, value in [('position', [1., 0., 0.]), ('focused', False), ('health', 99), ('combat', True),
                             ('playerInstance', 99), ('level', 2), ('cameras', [])]:
            data = performance_evidence()
            data['native_tests'][10]['data']['context'][field] = value
            self.assertEqual('INCOMPLETE/FAIL', REPORT.assess(data, performance=True)['status'])

    def test_population_can_evolve_without_freezing_residents(self):
        data = performance_evidence()
        data['native_tests'][10]['data']['context']['nativeActors'] = 25
        self.assertEqual('PASS', REPORT.assess(data, performance=True)['status'])

    def test_summary_disagreement_and_loss_fail(self):
        data = performance_evidence()
        data['trials'][0]['frameP95Ms'] = 30
        self.assertEqual('INCOMPLETE/FAIL', REPORT.assess(data, performance=True)['status'])

    def test_malformed_context_and_frame_count_fail_closed(self):
        data = performance_evidence()
        data['native_tests'][9]['data']['context'] = []
        self.assertEqual('INCOMPLETE/FAIL', REPORT.assess(data, performance=True)['status'])
        data = performance_evidence()
        data['native_tests'][10]['data']['result']['frames'] = None
        self.assertEqual('INCOMPLETE/FAIL', REPORT.assess(data, performance=True)['status'])
        data = performance_evidence()
        data['issues'] = ['capture loss']
        self.assertEqual('INCOMPLETE/FAIL', REPORT.assess(data, performance=True)['status'])

    def test_clean_capture_with_regression_needs_investigation(self):
        for field, value in [('frameP95Ms', 20.), ('diagnosticMeanMs', 1.1), ('diagnosticP95Ms', 2.1)]:
            data = performance_evidence()
            data['native_tests'][12]['data']['result'][field] = value
            data['native_tests'][18]['data']['result'][field] = value
            data['trials'][1][field] = value
            data['trials'][4][field] = value
            self.assertEqual('NEEDS INVESTIGATION', REPORT.assess(data, performance=True)['status'])

    def test_no_baseline_headroom_needs_investigation(self):
        data = performance_evidence()
        for row in data['trials']:
            row['frameP95Ms'] = 40.
        for row in data['native_tests']:
            if row['event'] == 'native-test-performance-trial':
                row['data']['result']['frameP95Ms'] = 40.
        self.assertEqual('NEEDS INVESTIGATION', REPORT.assess(data, performance=True)['status'])


class HarnessReportTests(unittest.TestCase):
    def fails(self, data):
        self.assertNotEqual('PASS', REPORT.assess(data)['status'])

    def test_complete_positive_evidence(self):
        self.assertEqual('PASS', REPORT.assess(evidence())['status'])

    def test_ordinary_detail_refresh_preserves_discovery(self):
        data = evidence()
        data['native_tests'][2]['data']['indexed']['collection'] = 'detail'
        self.assertEqual('PASS', REPORT.assess(data)['status'])

    def test_harness_cannot_seed_its_own_discovery(self):
        data = evidence()
        data['native_tests'][2]['data']['indexed']['collection'] = 'harness-read-only'
        self.fails(data)

    def test_capture_complete_cannot_hide_missing_observation(self):
        data = evidence()
        data['native_tests'].pop(2)
        self.fails(data)

    def test_inspector_changed_player_health(self):
        data = evidence()
        data['native_tests'][2]['data']['after']['player']['health'] = 99
        self.fails(data)

    def test_stale_index_is_not_discovery_pass(self):
        data = evidence()
        data['native_tests'][2]['data']['indexed']['gameTime'] = 8
        self.fails(data)

    def test_lifecycle_core_cannot_stand_in_for_native_details(self):
        data = evidence()
        data['native_tests'][2]['data']['indexed']['data']['coverage'] = 'lifecycle-core'
        self.fails(data)

    def test_invalid_query_membership(self):
        data = evidence()
        data['native_tests'][2]['data']['queries'][2]['max'] = 3
        self.fails(data)

    def test_retirement_request_is_not_actual_retirement(self):
        data = evidence()
        data['native_tests'][4]['data']['nativeActive'] = True
        self.fails(data)

    def test_capture_loss_invalidates_terminal_complete(self):
        data = evidence()
        data['issues'].append('shutdown record reports capture loss/error')
        self.fails(data)

    def test_pause_or_exit_requires_incomplete_result(self):
        data = evidence()
        data['native_tests'][-1]['data']['status'] = 'INTERRUPTED'
        self.fails(data)

    def test_duplicate_or_reordered_observation(self):
        data = evidence()
        data['native_tests'][2]['seq'] = 9
        self.fails(data)
        data = evidence()
        data['native_tests'].append(copy.deepcopy(data['native_tests'][2]))
        self.fails(data)

    def test_malformed_numeric_and_query_values_fail_closed(self):
        for field, value in [('native', {}), ('indexed', []), ('queries', [None]), ('cameraCount', True)]:
            with self.subTest(field=field):
                data = evidence()
                data['native_tests'][2]['data'][field] = value
                self.fails(data)


if __name__ == '__main__':
    unittest.main()
