"""Synthetic negative controls; these are not game evidence."""
import copy
import sys
import unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'scripts'))
import report_banana_quest as reader


class BananaEvidenceTests(unittest.TestCase):
    def exchange(self):
        native = dict(quests=[dict(type='Retrieve', status='Accepted')], numQuests=1, floorComplete=False, canExit=False)
        before = dict(playerBananas=3, recipientBananas=0, money=10, kits=0, playerHealth=100)
        after = dict(playerBananas=2, recipientBananas=1, money=25, kits=0, playerHealth=100)
        return dict(native_tests=[dict(event='native-test-quest-case', data=dict(case='cash', phase='exchange', state='Completed', winner='buyer',
                    extra=dict(recipient=0, before=before, after=after, nativeBefore=native, nativeAfter=copy.deepcopy(native),
                               duplicateBefore=after, duplicateAfter=copy.deepcopy(after), thanksBubble=True, pickupVerified=True)))])

    def passed(self, data, name):
        return next(c['passed'] for c in reader.assess(data)['checks'] if c['name'] == name)

    def test_empty_and_labels_cannot_pass(self):
        for data in ({'native_tests': []}, {'native_tests': [dict(event='native-test-quest-case', data=dict(case='cash', phase='complete', state='Completed'))]}):
            self.assertFalse(all(c['passed'] for c in reader.assess(data)['checks']))

    def test_startup_requires_live_playable_encounter(self):
        name = 'Normal startup offered: favor exists on playable floor after character selection'
        row = dict(phase='offered', state='Offered', sameEncounter=True, ready=True, floor='Slums 1',
                   level=1, nativeQuestCount=2, characterSelect=False)
        data = dict(native_tests=[dict(event='native-test-quest-startup', data=row)])
        self.assertTrue(self.passed(data, name))
        for key, value in (('sameEncounter', False), ('characterSelect', True), ('level', 0),
                           ('nativeQuestCount', 0), ('state', 'Expired'), ('ready', False)):
            altered = copy.deepcopy(data)
            altered['native_tests'][0]['data'][key] = value
            self.assertFalse(self.passed(altered, name))

    def test_exact_inventory_deltas_required(self):
        data = self.exchange()
        name = 'cash: exactly one banana and selected reward verified'
        self.assertTrue(self.passed(data, name))
        for key, value in (('playerBananas', 1), ('recipientBananas', 0), ('money', 40), ('kits', 1)):
            altered = copy.deepcopy(data)
            altered['native_tests'][0]['data']['extra']['after'][key] = value
            self.assertFalse(self.passed(altered, name))

    def test_duplicate_payment_and_changed_native_objective_fail(self):
        data = self.exchange()
        data['native_tests'][0]['data']['extra']['duplicateAfter']['money'] = 40
        self.assertFalse(self.passed(data, 'cash: repeat and other recipient calls cannot pay again'))
        data = self.exchange()
        data['native_tests'][0]['data']['extra']['nativeAfter']['floorComplete'] = True
        self.assertFalse(self.passed(data, 'cash: native quest and exit state unchanged by exchange'))

    def test_no_bubble_and_duplicate_records_fail(self):
        data = self.exchange()
        data['native_tests'][0]['data']['extra']['thanksBubble'] = False
        self.assertFalse(self.passed(data, 'cash: actual native thanks bubble'))
        data['native_tests'].append(copy.deepcopy(data['native_tests'][0]))
        self.assertFalse(self.passed(data, 'cash: exactly one banana and selected reward verified'))


if __name__ == '__main__':
    unittest.main()
