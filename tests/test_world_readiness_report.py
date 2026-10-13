import copy
import importlib.util
from pathlib import Path
import unittest
from test_world_harness_report import REPORT, performance_evidence

SPEC = importlib.util.spec_from_file_location('readiness', Path(__file__).parents[1] / 'scripts/report_world_readiness.py')
READINESS = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(READINESS)


def busy_evidence():
    report = performance_evidence()
    for row in report['native_tests']:
        row['event'] = row['event'].replace('native-test-performance-', 'native-test-readiness-busy-')
        data = row['data']
        if row['event'] in ('native-test-start', 'native-test-ended'):
            data['extension'] = 'busy'
        if 'result' in data:
            data['result']['scenario'] = 'harness-controlled-busy'
            data.update(pulses=300, noises=1200, projectiles=300, commands=18, movement=1.)
        if row['event'].startswith('native-test-readiness-busy-'):
            data['actors'] = [dict(state=dict(actor=dict(instance=100 + i, uid=10 + i))) for i in range(4)]
    for trial in report['trials']:
        trial['scenario'] = 'harness-controlled-busy'
    # Keep ordered provenance, including cleanup before the terminal.
    end = report['native_tests'].pop()
    first = dict(seq=11, event='native-test-readiness-start', data=dict(mode='busy'), wallSeconds=0)
    report['native_tests'].insert(9, first)
    report['native_tests'].extend([
        dict(event='native-test-readiness-cleanup', data=dict(activeKeys=[], nativeLive=False, retirement=[dict(nativeLive=False, nativeActive=False)] * 4, ownedCount=4, keys=['a', 'b', 'c', 'd'])),
        dict(event='native-test-readiness-ended', data=dict(mode='busy', status='COMPLETE')), end])
    for i, row in enumerate(report['native_tests']):
        row['seq'] = i + 1
    return report


def extension_evidence(mode):
    events = []
    def event(name, data, wall=0):
        events.append(dict(event='native-test-' + name, seq=len(events) + 1, data=data, wallSeconds=wall))
    event('start', dict(extension=mode))
    event('readiness-start', dict(mode=mode))
    def frame(quality, count):
        return dict(quality=quality, entities=[dict(kind='Actor', key=str(i)) for i in range(count)])
    if mode == 'advanced':
        event('readiness-crowd-baseline', dict(frame=frame(0, 4)))
        for at in (1., 2.):
            event('readiness-load-sample', dict(stage='window', evidence=dict(windowWallSeconds=at, windowMeanMs=1.1, windowFrames=60, windowOverBudget=0)))
        for prefix, reason, at in [('window', 'window-budget', 2.), ('backpressure', 'writer-backpressure', 40.)]:
            if prefix == 'backpressure':
                event('readiness-slow-writer', dict(milliseconds=2000, scope='not performance evidence'))
            event('readiness-' + prefix + '-reduced', dict(emitted=120, evidence=dict(reason=reason, quality=1,
                  nearbyActorLimit=2, windowMeanMs=1.1, saturated=True)), at)
            event('readiness-sample', dict(stage=prefix + '-recover', frame=frame(1, 2)), at + 1)
            event('readiness-' + prefix + '-recovered', dict(frame=frame(0, 4), evidence=dict(quality=0, reason='gradual-recovery',
                  nearbyActorLimit=8, clean=True)), at + 31)
    else:
        objects = [dict(nativeType=kind, tags=[tag], active=True, destroyed=False, collection='objectRealList',
                   data=dict(coverage='native-fields-at-sample')) for kind, tag in [('Item', 'food'), ('FireHydrant', 'hydrant'), ('Door', 'door')]]
        event('readiness-objects', dict(entities=objects))
        for i in range(3):
            event('readiness-door-state', dict(state=i, native=dict(data=dict(open=i == 1, locked=i == 2)), before=dict(player=1), after=dict(player=1)))
        event('readiness-ui-before', dict(player=dict(health=100)))
        event('readiness-ui-paused', dict(open=True, blocked=True, timeScale=0))
        event('readiness-ui-resumed', dict(open=False, blocked=False, timeScale=1, player=dict(health=100)))
    event('readiness-cleanup', dict(keys=['a', 'b', 'c', 'd'], activeKeys=[], nativeLive=False, retirement=[dict(nativeLive=False, nativeActive=False)] * 4, ownedCount=4))
    if mode == 'native':
        event('readiness-reload-request', dict(oldLevel=1, oldWorldKeys=['live-player', 'live-object']))
        event('readiness-reloaded', dict(oldLevel=1, newLevel=2, oldKeys=['a', 'b', 'c', 'd'], staleKeys=[], oldWorldKeys=['live-player', 'live-object'], staleWorldKeys=[]))
    event('readiness-ended', dict(status='COMPLETE', mode=mode))
    event('ended', dict(status='COMPLETE', extension=mode))
    return dict(native_tests=events)


class ExtensionChecks(unittest.TestCase):
    def test_bounded_extensions(self):
        for mode in ('advanced', 'native'):
            result = READINESS.assess(extension_evidence(mode), mode, REPORT.assess_performance)
            self.assertTrue(all(c['passed'] for c in result['checks']), result['checks'])

    def reject_mutation(self, mode, event, mutate):
        report = extension_evidence(mode)
        row = next(r for r in report['native_tests'] if r['event'] == 'native-test-readiness-' + event)
        mutate(row['data'])
        self.assertFalse(all(c['passed'] for c in READINESS.assess(report, mode, REPORT.assess_performance)['checks']))

    def test_burst_cannot_certify_window_trigger(self):
        self.reject_mutation('advanced', 'window-reduced', lambda d: d['evidence'].update(reason='frame-budget'))

    def test_unsaturated_writer_cannot_certify_backpressure(self):
        self.reject_mutation('advanced', 'backpressure-reduced', lambda d: d['evidence'].update(saturated=False))

    def test_policy_number_does_not_prove_detail_shedding(self):
        self.reject_mutation('advanced', 'sample', lambda d: d['frame'].update(entities=[dict(kind='Actor')] * 8))

    def test_restored_policy_needs_actual_detail(self):
        self.reject_mutation('advanced', 'window-recovered', lambda d: d['frame'].update(entities=[]))

    def test_active_destroyed_native_object_is_retired(self):
        report = extension_evidence('native')
        row = next(r for r in report['native_tests'] if r['event'] == 'native-test-readiness-cleanup')
        row['data']['retirement'] = [dict(nativeLive=False, nativeActive=True, nativeDestroyed=True)] * 4
        self.assertTrue(all(c['passed'] for c in READINESS.assess(report, 'native', REPORT.assess_performance)['checks']))

    def test_live_native_object_cannot_be_declared_retired(self):
        self.reject_mutation('native', 'cleanup', lambda d: d.update(retirement=[dict(nativeLive=False, nativeActive=True, nativeDestroyed=False)] * 4))

    def test_old_keys_must_be_absent_after_reload(self):
        self.reject_mutation('native', 'reloaded', lambda d: d.update(staleKeys=['a']))

    def test_lifecycle_core_cannot_replace_ordinary_object_detail(self):
        self.reject_mutation('native', 'objects', lambda d: d['entities'][0].update(collection='lifecycle:SpawnItem'))

    def test_inspection_cannot_change_state(self):
        self.reject_mutation('native', 'door-state', lambda d: d.update(after=dict(player=2)))

    def test_paused_ui_needs_input_guard(self):
        self.reject_mutation('native', 'ui-paused', lambda d: d.update(blocked=False))


class ReadinessChecks(unittest.TestCase):
    def check_report(self, report):
        return READINESS.assess(report, 'busy', REPORT.assess_performance)

    def test_valid_comparable_activity(self):
        result = self.check_report(busy_evidence())
        self.assertTrue(all(c['passed'] for c in result['checks']), result['checks'])
        self.assertTrue(result['comparison']['numericPass'])

    def test_completion_does_not_replace_native_activity(self):
        report = busy_evidence()
        for row in report['native_tests']:
            if row['event'] == 'native-test-readiness-busy-trial': row['data']['movement'] = 0
        self.assertFalse(all(c['passed'] for c in self.check_report(report)['checks']))

    def test_reused_actor_cannot_be_a_comparable_fixture(self):
        report = busy_evidence()
        row = next(r for r in report['native_tests'] if r['event'] == 'native-test-readiness-busy-trial')
        row['data']['actors'][0]['state']['actor']['uid'] = 999
        self.assertFalse(all(c['passed'] for c in self.check_report(report)['checks']))

    def test_missed_schedule_fails(self):
        report = busy_evidence()
        row = next(r for r in report['native_tests'] if r['event'] == 'native-test-readiness-busy-trial')
        row['data']['pulses'] = 299
        self.assertFalse(all(c['passed'] for c in self.check_report(report)['checks']))

    def test_native_cleanup_required(self):
        report = busy_evidence()
        next(r for r in report['native_tests'] if r['event'] == 'native-test-readiness-cleanup')['data']['nativeLive'] = True
        self.assertFalse(all(c['passed'] for c in self.check_report(report)['checks']))

    def test_busy_cannot_borrow_stationary_summary(self):
        report = busy_evidence()
        row = next(r for r in report['native_tests'] if r['event'] == 'native-test-readiness-busy-trial')
        row['data']['result']['scenario'] = 'harness-stationary'
        self.assertFalse(all(c['passed'] for c in self.check_report(report)['checks']))

    def test_numeric_failure_is_preserved(self):
        report = busy_evidence()
        for row in report['native_tests']:
            if row['event'] == 'native-test-readiness-busy-trial' and row['data']['result']['mode'] == 'Record':
                row['data']['result']['diagnosticMeanMs'] = 1.1
        report['trials'] = [copy.deepcopy(r['data']['result']) for r in report['native_tests'] if r['event'] == 'native-test-readiness-busy-trial']
        result = self.check_report(report)
        self.assertTrue(all(c['passed'] for c in result['checks']))
        self.assertFalse(result['comparison']['numericPass'])


if __name__ == '__main__':
    unittest.main()
