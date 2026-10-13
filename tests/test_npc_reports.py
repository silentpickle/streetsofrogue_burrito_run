"""Synthetic negative controls for native evidence readers; not gameplay evidence."""
import copy
import sys
import unittest
from pathlib import Path
sys.path.insert(0, str(Path(__file__).resolve().parents[1] / 'scripts'))
import report_npc_desires as desires
import report_npc_probe as probe


class NpcEvidenceTests(unittest.TestCase):
    @staticmethod
    def wandering_capture():
        row = dict(case='native-wander', phase='complete', actor=20, active=False, playerUnchanged=True,
                   fud=1, banana=0, outcome='picked-up', enrollmentRoot='GoalWanderFar', defaultGoal='WanderFar')
        return {'native_tests': [
            {'seq': 1, 'event': 'native-test-npc-desire-case', 'data': dict(row, phase='start')},
            {'seq': 2, 'event': 'native-test-npc-probe', 'data': dict(phase='arrived', actor=20, start=[0, 0, 0], position=[1, 0, 0])},
            {'seq': 3, 'event': 'native-test-npc-probe', 'data': dict(phase='pickup-called', actor=20, beforeCount=0, count=1, itemActive=False)},
            {'seq': 4, 'event': 'native-test-npc-probe', 'data': dict(phase='native-after-release', actor=20, root='GoalWanderFar', job='', jobBig='')},
            {'seq': 5, 'event': 'native-test-npc-desire-case', 'data': row}]}

    def test_wandering_case_positive_control(self):
        self.assertTrue(all(c['passed'] for c in self.case_checks(self.wandering_capture(), 'native-wander')))

    def test_wandering_requires_actual_root_movement_and_transfer(self):
        for row, field, value in [(4, 'enrollmentRoot', 'GoalIdle'), (4, 'defaultGoal', 'DoNothing'),
                                  (1, 'position', [0, 0, 0]), (2, 'count', 0), (2, 'itemActive', True),
                                  (3, 'root', 'PickupGoal'), (3, 'actor', 21)]:
            capture = self.wandering_capture(); capture['native_tests'][row]['data'][field] = value
            with self.subTest(row=row, field=field):
                self.assertFalse(all(c['passed'] for c in self.case_checks(capture, 'native-wander')))

    @staticmethod
    def coordination_capture(name):
        row = dict(case=name, phase='complete', actor=20, active=False, playerUnchanged=True, fud=0, banana=0,
                   agentName='Hobo', coordinationRevision=2, player=10, employer=10, following=10,
                   hasEmployer=True, hasFollowing=True, beginRejected=True, enrollRejected=True,
                   outcome='native-employment', decision='native-employment', job='Ruckus', jobCode='Ruckus',
                   jobBig='', jobBigCode='None', assignedPos=[1, 2, 0], nativeDestination=[1, 2, 0], replacementRootPreserved=True)
        if name in ('specialist-task', 'replacement-root'):
            row['outcome'] = 'native-job'
        return {'native_tests': [
            {'seq': 1, 'event': 'native-test-npc-desire-case', 'data': dict(row, phase='start')},
            {'seq': 2, 'event': 'native-test-npc-probe', 'data': {'phase': 'ended', 'actor': 20, 'outcome': row['outcome']}},
            {'seq': 3, 'event': 'native-test-npc-probe', 'data': {'phase': 'native-after-release', 'actor': 20, 'root': 'GoalDoJob'}},
            {'seq': 4, 'event': 'native-test-npc-desire-case', 'data': row}]}

    def case_checks(self, capture, name):
        return [c for c in desires.assess(capture)['checks'] if c['name'].startswith(name + ':')]

    def test_coordination_cases_have_positive_controls(self):
        for name in desires.COORDINATION:
            with self.subTest(case=name):
                self.assertTrue(all(c['passed'] for c in self.case_checks(self.coordination_capture(name), name)))

    def test_missing_or_contradictory_commitment_evidence_fails(self):
        for name, field in [('employment-before', 'beginRejected'), ('following-before', 'enrollRejected'),
                            ('recruit-before', 'employer'), ('recruit-during', 'following'),
                            ('employment-during', 'outcome'), ('recruit-during', 'decision')]:
            for value in (None, False, 'wrong'):
                capture = self.coordination_capture(name)
                capture['native_tests'][-1]['data'][field] = value
                with self.subTest(case=name, field=field, value=value):
                    self.assertFalse(all(c['passed'] for c in self.case_checks(capture, name)))

    def test_changed_native_job_fields_or_root_fail(self):
        for field in ('job', 'jobCode', 'jobBig', 'jobBigCode', 'assignedPos', 'nativeDestination', 'replacementRootPreserved'):
            capture = self.coordination_capture('replacement-root')
            capture['native_tests'][-1]['data'][field] = 'wrong'
            with self.subTest(field=field):
                self.assertFalse(all(c['passed'] for c in self.case_checks(capture, 'replacement-root')))

    def test_interrupted_action_cannot_call_pickup(self):
        capture = self.coordination_capture('recruit-during')
        capture['native_tests'][1]['data']['phase'] = 'pickup-called'
        self.assertFalse(all(c['passed'] for c in self.case_checks(capture, 'recruit-during')))

    def test_missing_or_still_owned_post_release_root_fails(self):
        for root in (None, '', 'PickupGoal'):
            capture = self.coordination_capture('specialist-task')
            capture['native_tests'][2]['data']['root'] = root
            self.assertFalse(all(c['passed'] for c in self.case_checks(capture, 'specialist-task')))

    def test_post_release_evidence_must_belong_to_same_actor(self):
        capture = self.coordination_capture('specialist-task')
        capture['native_tests'][2]['data']['actor'] = 21
        self.assertFalse(all(c['passed'] for c in self.case_checks(capture, 'specialist-task')))

    def test_v2_decisions_require_obligation_field(self):
        capture = {'Input': {'PolicyVersion': 2, 'NativeObligation': None}, 'Output': {}}
        report = {'native_tests': [{'event': 'native-test-npc-decision', 'data': {'capture': capture}}]}
        versioned = lambda: next(c['passed'] for c in desires.assess(report)['checks'] if c['name'] == 'Versioned decisions captured')
        self.assertTrue(versioned())
        del capture['Input']['NativeObligation']
        self.assertFalse(versioned())
        capture['Input']['PolicyVersion'] = 1
        self.assertFalse(versioned())

    def test_probe_requires_canonical_identity(self):
        capture = {'native_tests': [{'event': 'native-test-npc-probe', 'data': {'agentName': 'SlumDweller'}}]}
        checks = probe.assess(capture)['checks']
        self.assertFalse(next(c['passed'] for c in checks if c['name'] == 'Canonical Hobo enrolled'))

    def test_decision_cannot_claim_pickup_over_native_commitment(self):
        capture = {'Input': {'PolicyVersion': 2, 'Emergency': False, 'NativeObligation': 'native-employment', 'CurrentTarget': 'food'},
                   'Output': {'Intention': 'cancel', 'Reason': 'native-employment', 'Target': None}}
        report = {'native_tests': [{'event': 'native-test-npc-decision', 'data': {'capture': capture}}]}
        consistent = lambda: next(c['passed'] for c in desires.assess(report)['checks'] if c['name'] == 'Captured decisions respect danger and native obligations')
        self.assertTrue(consistent())
        capture['Output']['Intention'] = 'pickup'
        self.assertFalse(consistent())
        capture['Output']['Intention'] = 'cancel'
        capture['Output']['Target'] = 'food'
        self.assertFalse(consistent())
        capture['Output']['Target'] = None
        capture['Input']['Emergency'] = True
        self.assertFalse(consistent())

    def test_empty_capture_cannot_pass(self):
        for reader in (desires, probe):
            self.assertFalse(all(c['passed'] for c in reader.assess({'native_tests': []})['checks']))

    def test_terminal_label_alone_cannot_pass(self):
        data = {'native_tests': [{'event': 'native-test-npc-gate', 'data': {'status': 'COMPLETE'}}]}
        self.assertFalse(all(c['passed'] for c in probe.assess(data)['checks']))

    def test_claimed_case_completion_does_not_prove_pickup(self):
        rows = []
        for name in desires.CASES:
            rows.append({'seq': len(rows), 'event': 'native-test-npc-desire-case', 'data': {
                'case': name, 'phase': 'complete', 'active': False, 'playerUnchanged': True,
                'fud': 1, 'banana': 0, 'outcome': 'picked-up'}})
        checks = desires.assess({'native_tests': rows})['checks']
        self.assertFalse(next(c['passed'] for c in checks if c['name'] == 'prefer-fud: exactly one native pickup call'))

    def test_missing_scope_and_pause_fail(self):
        checks = desires.assess({'native_tests': []})['checks']
        self.assertFalse(next(c['passed'] for c in checks if c['name'].startswith('Pause')))
        self.assertFalse(next(c['passed'] for c in checks if c['name'].startswith('Native level')))

    def test_inventory_gain_without_retirement_fails(self):
        data = {'native_tests': [{'event': 'native-test-npc-probe', 'data': {
            'phase': 'ended', 'outcome': 'picked-up', 'beforeCount': 0, 'count': 1, 'itemActive': True}}]}
        checks = probe.assess(data)['checks']
        self.assertFalse(next(c['passed'] for c in checks if c['name'].startswith('Native pickup')))
        changed = copy.deepcopy(data)
        changed['native_tests'][0]['data']['itemActive'] = False
        checks = probe.assess(changed)['checks']
        self.assertTrue(next(c['passed'] for c in checks if c['name'].startswith('Native pickup')))


if __name__ == '__main__':
    unittest.main()
