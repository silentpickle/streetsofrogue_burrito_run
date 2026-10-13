import copy
import importlib.util
from pathlib import Path
import unittest

SPEC = importlib.util.spec_from_file_location('hydrant_report', Path(__file__).parents[1] / 'scripts/report_pet_hydrant.py')
HYDRANT = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(HYDRANT)


def evidence():
    events = []
    player = dict(uid=1, health=100, inventory=[dict(name='Phone', count=1)])

    def add(name, **data):
        events.append(dict(event='native-test-' + name, seq=len(events) + 1, data=data))

    add('hydrant-start', player=player)
    for i, case in enumerate(('mark', 'removed', 'timeout', 'release')):
        key = 'hydrant-' + case
        state = dict(uid=10+i, instance=20+i, state=0, destroyed=False, position=[2, 0, 0])
        add('hydrant-fixture', case=case, uid=10+i, instance=20+i, health=33, hydrant=state)
        snapshot = dict(key=key, nativeId=10+i, instance=20+i, kind='Object', tags=['hydrant'],
                        active=True, destroyed=False, collection='objectRealList')
        add('hydrant-discovered', case=case, snapshot=snapshot)
        add('pet-hydrant-noticed', key=key, snapshot=snapshot, gameTime=i*60,
            petPosition=[0, 0, 0], targetPosition=[2, 0, 0], destination=[1, 0, 0])
        if case == 'mark':
            add('pet-hydrant-marking', key=key, distance=1, visual=True, petPosition=[1, 0, 0])
        outcome = dict(mark='marked', removed='target-gone-or-ineligible', timeout='timed-out', release='test-release')[case]
        action = dict(key=key, outcome=outcome, gameTime=i*60+8.1, markSeconds=1.3 if case == 'mark' else 0,
                      distance=1, petPosition=[1, 0, 0])
        if case == 'mark':
            action.update(idleAnchorBefore=[0, 0, 0], idleAnchorAfter=[0, 0, 0])
        add('pet-hydrant-action', **action)
        after = dict(state, destroyed=case == 'removed')
        add('hydrant-case', case=case, key=key, action=action, player=player, health=33, visual=False,
            following=case != 'release', followingUid=None if case == 'release' else 1, rootGoal='GoalFollow',
            intent='Wander' if case == 'release' else 'Follow', defaultGoal='WanderFar',
            hydrantUnchanged=case != 'removed', hydrant=after)
        if case != 'release':
            add('hydrant-quiet', key=key, seconds=46, unchanged=case != 'removed', player=player)
    add('hydrant-cleanup', dogActive=False, hydrantDestroyed=True, indexedDogLive=False, indexedHydrantLive=False, visual=False)
    add('hydrant-ended', status='COMPLETE', player=player)
    return dict(native_tests=events)


class HydrantReportTests(unittest.TestCase):
    def setUp(self):
        self.report = copy.deepcopy(evidence())

    def passes(self):
        return all(c['passed'] for c in HYDRANT.assess(self.report)['checks'])

    def change(self, event, **data):
        row = next(r for r in self.report['native_tests'] if r['event'] == 'native-test-' + event)
        row['data'].update(data)

    def test_complete(self):
        self.assertTrue(self.passes())

    def test_complete_alone_is_insufficient(self):
        self.report['native_tests'] = self.report['native_tests'][-1:]
        self.assertFalse(self.passes())

    def test_missing_case(self):
        self.report['native_tests'] = [r for r in self.report['native_tests'] if r['data'].get('case') != 'timeout']
        self.assertFalse(self.passes())

    def test_wrong_activation(self):
        self.report['native_tests'][2]['data']['snapshot']['instance'] = 999
        self.assertFalse(self.passes())

    def test_seeded_discovery(self):
        self.report['native_tests'][2]['data']['snapshot']['collection'] = 'lifecycle:test'
        self.assertFalse(self.passes())

    def test_hydrant_changed(self):
        self.change('hydrant-case', hydrantUnchanged=False)
        self.assertFalse(self.passes())

    def test_visual_leaked(self):
        self.change('hydrant-case', visual=True)
        self.assertFalse(self.passes())

    def test_no_visible_pose(self):
        self.change('pet-hydrant-marking', visual=False)
        self.assertFalse(self.passes())

    def test_nan_distance(self):
        self.change('pet-hydrant-marking', distance=float('nan'))
        self.assertFalse(self.passes())

    def test_too_short_mark(self):
        self.change('pet-hydrant-action', markSeconds=.1)
        self.assertFalse(self.passes())

    def test_intent_is_not_native_follow(self):
        self.change('hydrant-case', rootGoal='GoalIdle')
        self.assertFalse(self.passes())

    def test_incomplete_cleanup(self):
        self.change('hydrant-cleanup', indexedHydrantLive=True)
        self.assertFalse(self.passes())

    def test_repeat_notice(self):
        row = next(r for r in self.report['native_tests'] if r['event'] == 'native-test-pet-hydrant-noticed')
        self.report['native_tests'].append(copy.deepcopy(row))
        self.assertFalse(self.passes())

    def test_incomplete_cooldown(self):
        self.change('hydrant-quiet', seconds=8)
        self.assertFalse(self.passes())
