"""Recompute NPC probe gates from observed action transitions."""
from report_pet_food import displacement


def assess(report):
    rows = [r['data'] for r in report['native_tests'] if r['event'] == 'native-test-npc-probe']
    gates = [r['data'] for r in report['native_tests'] if r['event'] == 'native-test-npc-gate']
    checks = []

    def check(name, passed):
        checks.append(dict(name=name, passed=bool(passed)))

    def phase(name):
        return [r for r in rows if r.get('phase') == name]

    check('NPC gate completed', len(gates) == 1 and gates[0].get('status') == 'COMPLETE')
    check('Canonical Hobo enrolled', bool(rows) and all(r.get('agentName') == 'Hobo' for r in rows))
    arrivals = phase('arrived')
    check('NPC actually approached', bool(arrivals) and all((displacement(r.get('start'), r.get('position')) or 0) > 0.5 for r in arrivals))
    outcomes = {r.get('outcome'): r for r in phase('ended')}
    pickup = outcomes.get('picked-up', {})
    check('Native pickup transferred exactly one and retired ground item', pickup.get('beforeCount') == 0 and pickup.get('count') == 1 and pickup.get('itemActive') is False)
    for name in ('cancelled', 'emergency'):
        r = outcomes.get(name, {})
        check(name + ': no inventory gain', r.get('count') == r.get('beforeCount') == 0)
        check(name + ': no stale command', r.get('job') == r.get('jobBig') == '')
    after = phase('native-after-release')
    check('Observed native behavior after release', len(after) >= 2 and all(r.get('root') not in (None, 'PickupGoal') and r.get('job') != 'GoHere' and r.get('jobBig') != 'GoHere' for r in after))
    return dict(checks=checks)
