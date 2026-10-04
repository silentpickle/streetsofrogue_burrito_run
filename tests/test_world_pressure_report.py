import copy
import unittest
from test_world_harness_report import REPORT, evidence


def pressure_evidence():
    data = evidence()
    data['native_tests'][0]['data']['pressure'] = True
    terminal = data['native_tests'].pop()
    terminal['data']['pressure'] = True
    context = dict(level=1, controller=2, floor='1-1', playerInstance=3, playerUid=4, position=[0., 0., 0.],
                   health=100, combat=False, focused=False, cameras=[dict(camera=5)])
    core = dict(key='1:200:40:3', instance=200, nativeId=40, activation=3, level=1, name='Courier', kind='Actor',
                gameTime=10., collection='lifecycle:SpawnAgent', data=dict(coverage='lifecycle-core; census-details-pending; viewport-unknown'))
    seq = 9
    data['effects'] = []
    def record(name, fields, wall=15):
        nonlocal seq
        seq += 1
        r = dict(seq=seq, event=name, data=fields, level=1, wallSeconds=wall, gameTime=wall)
        data['effects' if name.startswith('effect-') else 'native_tests'].append(r)
        return r
    baseline = dict(quality=0, clean=True, nearbyActorLimit=8, interval=.2)
    reduced = dict(quality=1, clean=True, nearbyActorLimit=2, interval=.2, reason='frame-budget', frameWorkMs=3)
    recovered = dict(quality=0, clean=True, nearbyActorLimit=8, interval=.2, reason='gradual-recovery')
    record('native-test-pressure-start', dict(maxBatches=32, batchSize=64, core=core, context=copy.deepcopy(context), evidence=baseline))
    effects = []
    for i in range(64):
        key = f'1:500:{100+i}:{10+i}'
        entity = dict(key=key, instance=500, nativeId=100+i, kind='Noise', active=True, collection='lifecycle:SpawnNoise')
        record('effect-created', dict(key=key, instance=500, nativeId=100+i, type='Noise'))
        record('effect-retire-request', dict(key=key, method='PoolRecycle'))
        effects.append(dict(indexed=entity, volume=0, dontAttract=True, nativeActiveAfterCleanup=False))
    record('native-test-pressure-batch', dict(batch=1, effects=effects))
    record('native-test-pressure-stopped', dict(batches=1, evidence=reduced), wall=16)
    full = copy.deepcopy(core)
    full.update(collection='agentList', active=True, gameTime=17., data=dict(coverage='native-fields-at-sample', defaultGoal='DoNothing', health=100, rootGoal='GoalIdle'))
    record('native-test-pressure-detail', dict(indexed=full), wall=17)
    record('native-test-pressure-sample', reduced, wall=17)
    record('native-test-pressure-recovered', dict(evidence=recovered, context=copy.deepcopy(context)), wall=46)
    record('native-test-pressure-retired', dict(key=core['key'], nativeActive=False, indexedActive=False), wall=51)
    terminal['seq'] = seq+1
    data['native_tests'].append(terminal)
    return data


class PressureReportTests(unittest.TestCase):
    def result(self, data):
        return REPORT.assess(data, pressure=True)

    def test_complete_native_pressure_recovery(self):
        result = self.result(pressure_evidence())
        self.assertEqual('PASS', result['status'])
        self.assertEqual(64, result['pressure']['effects'])
        self.assertEqual(1, result['pressure']['pooledInstances'])

    def test_functional_complete_is_not_pressure_acceptance(self):
        self.assertEqual('INCOMPLETE/FAIL', self.result(evidence())['status'])

    def test_no_real_reduction_or_missing_recovery_fails(self):
        for remove in ('native-test-pressure-sample', 'native-test-pressure-recovered'):
            data = pressure_evidence()
            if remove.endswith('sample'):
                for r in data['native_tests']:
                    if r['event'] == 'native-test-pressure-stopped':
                        r['data']['evidence']['frameWorkMs'] = .5
                    if r['event'] == remove:
                        r['data']['frameWorkMs'] = .5
            else:
                data['native_tests'] = [r for r in data['native_tests'] if r['event'] != remove]
            self.assertEqual('INCOMPLETE/FAIL', self.result(data)['status'])

    def test_missing_effect_cleanup_duplicate_or_stale_identity_fails(self):
        for field, value in [('nativeActiveAfterCleanup', True), ('dontAttract', False), ('volume', 1)]:
            data = pressure_evidence()
            batch = next(r['data'] for r in data['native_tests'] if r['event'] == 'native-test-pressure-batch')
            batch['effects'][0][field] = value
            self.assertEqual('INCOMPLETE/FAIL', self.result(data)['status'])
        data = pressure_evidence()
        data['effects'].pop()
        self.assertEqual('INCOMPLETE/FAIL', self.result(data)['status'])
        data = pressure_evidence()
        batch = next(r['data'] for r in data['native_tests'] if r['event'] == 'native-test-pressure-batch')
        batch['effects'][1] = copy.deepcopy(batch['effects'][0])
        self.assertEqual('INCOMPLETE/FAIL', self.result(data)['status'])

    def test_partial_detail_and_forced_early_recovery_fail(self):
        for name, field, value in [('native-test-pressure-detail', 'collection', 'lifecycle:SpawnAgent'),
                                   ('native-test-pressure-detail', 'key', 'other')]:
            data = pressure_evidence()
            next(r for r in data['native_tests'] if r['event'] == name)['data']['indexed'][field] = value
            self.assertEqual('INCOMPLETE/FAIL', self.result(data)['status'])
        data = pressure_evidence()
        next(r for r in data['native_tests'] if r['event'] == 'native-test-pressure-recovered')['wallSeconds'] = 20
        self.assertEqual('INCOMPLETE/FAIL', self.result(data)['status'])

    def test_state_change_interruption_and_capture_loss_fail(self):
        data = pressure_evidence()
        next(r for r in data['native_tests'] if r['event'] == 'native-test-pressure-recovered')['data']['context']['health'] = 90
        self.assertEqual('INCOMPLETE/FAIL', self.result(data)['status'])
        data = pressure_evidence()
        data['native_tests'][-1]['data']['status'] = 'INTERRUPTED'
        self.assertEqual('INCOMPLETE/FAIL', self.result(data)['status'])
        data = pressure_evidence()
        data['issues'] = ['writer loss']
        self.assertEqual('INCOMPLETE/FAIL', self.result(data)['status'])


if __name__ == '__main__':
    unittest.main()
