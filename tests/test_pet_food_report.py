import copy
import importlib.util
from pathlib import Path
import unittest

SPEC = importlib.util.spec_from_file_location('pet_food', Path(__file__).parents[1] / 'scripts/report_pet_food.py')
FOOD = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(FOOD)


def evidence():
    events = []
    player = dict(uid=1, health=100, money=0, inventory=[dict(name='Phone', count=1)])

    def add(name, **data):
        events.append(dict(event='native-test-' + name, data=data, seq=len(events) + 1))

    add('food-start', player=player)
    for i, case in enumerate(('stack', 'full', 'eat', 'removed', 'release')):
        key = 'food-' + case
        count = 2 if case == 'stack' else 1
        health = 18 if case in ('eat', 'removed', 'release') else 33
        add('food-fixture', case=case, uid=10 + i, instance=100 + i, count=count, health=health, maximum=33)
        snapshot = dict(key=key, nativeId=10 + i, instance=100 + i, kind='Item', tags=['food'],
                        active=True, destroyed=False, collection='itemList')
        add('food-discovered', case=case, snapshot=snapshot)
        if case != 'stack':
            add('pet-food-noticed', key=key, snapshot=snapshot, distance=2., petPosition=[0., 0., 0.])
        if case in ('full', 'eat', 'removed'):
            outcome = dict(full='sniff-full-health', eat='ate', removed='food-gone-or-ineligible')[case]
            action = dict(key=key, foodUid=10 + i, outcome=outcome, distance=.6, petPosition=[1.5, 0., 0.],
                          inventoryBefore=1 if case == 'eat' else 0, inventoryAfter=0,
                          healthBefore=health, healthAfter=33 if case == 'eat' else health)
            add('pet-food-action', **action)
            add('food-case', case=case, key=key, action=action, following=True, rootGoal='GoalFollow', followingUid=1,
                nativeActive=case == 'full', indexedRetired=case != 'full', count=1 if case == 'full' else None,
                startHealth=health, endHealth=action['healthAfter'], resultDisplacement=1.5, player=player)
            add('food-cooldown', key=key, seconds=8.5, following=True)
        else:
            add('food-case', case=case, key=key, untouched=True, intent='Wander' if case == 'release' else 'Follow',
                following=False, defaultGoal='WanderFar', player=player)
    add('food-cleanup', actorKey='dog', foodKey='food-release', secondSpawnRejected=True, actorActive=False, foodActive=False,
        indexedActorLive=False, indexedFoodLive=False)
    add('food-ended', status='COMPLETE', player=player)
    return dict(native_tests=events)


class FoodReportTests(unittest.TestCase):
    def setUp(self):
        self.report = copy.deepcopy(evidence())

    def passes(self):
        return all(c['passed'] for c in FOOD.assess(self.report)['checks'])

    def change(self, event, case, **values):
        for r in self.report['native_tests']:
            if r['event'] == 'native-test-' + event and (case is None or r['data'].get('case') == case):
                r['data'].update(values)

    def test_valid_native_evidence(self):
        self.assertTrue(self.passes())

    def test_intent_cannot_replace_actual_healing(self):
        # Mutate both copies of the action, so equality alone cannot catch this.
        for r in self.report['native_tests']:
            d = r['data']
            a = d.get('action', d)
            if a.get('outcome') == 'ate':
                a['healthAfter'] = a['healthBefore']
        self.assertFalse(self.passes())

    def test_return_label_cannot_replace_native_goal(self):
        self.change('food-case', 'eat', rootGoal='GoalDoJob')
        self.assertFalse(self.passes())

    def test_return_distance_cannot_replace_approach(self):
        for r in self.report['native_tests']:
            a = r['data'].get('action', r['data'])
            if a.get('outcome') == 'ate':
                a['petPosition'] = [0., 0., 0.]
        self.change('food-case', 'eat', resultDisplacement=10.)
        self.assertFalse(self.passes())

    def test_ground_item_must_retire(self):
        self.change('food-case', 'eat', indexedRetired=False)
        self.assertFalse(self.passes())

    def test_wrong_activation_rejected(self):
        self.change('food-discovered', 'eat', snapshot=dict(key='recycled', nativeId=12, instance=102))
        self.assertFalse(self.passes())

    def test_player_inventory_change_rejected(self):
        self.change('food-case', 'eat', player=dict(uid=1, health=100, money=1, inventory=[]))
        self.assertFalse(self.passes())

    def test_healthy_food_preservation_required(self):
        self.change('food-case', 'full', nativeActive=False)
        self.assertFalse(self.passes())

    def test_stack_and_release_must_preserve_food(self):
        for name in ('stack', 'release'):
            with self.subTest(name=name):
                self.setUp()
                self.change('food-case', name, untouched=False)
                self.assertFalse(self.passes())

    def test_missing_case_rejected(self):
        self.report['native_tests'] = [r for r in self.report['native_tests'] if r['data'].get('case') != 'removed']
        self.assertFalse(self.passes())

    def test_cleanup_must_be_observed(self):
        self.change('food-cleanup', None, indexedActorLive=True)
        self.assertFalse(self.passes())

    def test_one_per_level_limit_required_after_cleanup(self):
        self.change('food-cleanup', None, secondSpawnRejected=False)
        self.assertFalse(self.passes())

    def test_repeat_consumption_rejected(self):
        self.report['native_tests'].append(copy.deepcopy(next(r for r in self.report['native_tests'] if r['event'] == 'native-test-pet-food-action')))
        self.assertFalse(self.passes())

    def test_short_cooldown_rejected(self):
        self.change('food-cooldown', None, seconds=7.)
        self.assertFalse(self.passes())


if __name__ == '__main__':
    unittest.main()
