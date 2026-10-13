"""Independent native inventory and interruption checks; export detached replay records."""
import json
from report_pet_food import displacement

COORDINATION = {'employment-before', 'following-before', 'recruit-before', 'employment-during', 'recruit-during', 'specialist-task', 'replacement-root'}
CASES = {'prefer-fud', 'prefer-banana', 'distance', 'protected', 'removed', 'timeout', 'new-command', 'actor-removed', 'blocked', 'refused', 'pause', 'combat', 'native-wander'} | COORDINATION


def assess(report):
    """Verify native choice, transfer and interruption evidence independently.

    The reader checks observed state and command preservation rather than trusting emitted
    success labels.
    """
    rows = [r['data'] for r in report['native_tests'] if r['event'] == 'native-test-npc-desire-case']
    checks = []

    def check(name, value):
        checks.append(dict(name=name, passed=bool(value)))

    complete = [r for r in rows if r.get('phase') == 'complete']
    check('All twenty pre-transition NPC cases completed exactly once', len(complete) == len(CASES) and {r.get('case') for r in complete} == CASES)
    check('No NPC failure records', not any(r.get('phase') == 'failed' for r in rows))
    starts = [r for r in rows if r.get('phase') == 'start']
    check('Canonical Hobo identity and coordination revision recorded for every case', len(starts) == len(CASES) + 1 and
          all(r.get('agentName') == 'Hobo' and r.get('coordinationRevision') == 3 for r in starts))
    for name in sorted(CASES):
        group = [r for r in complete if r.get('case') == name]
        r = group[0] if len(group) == 1 else {}
        check(name + ': released action and preserved player', r.get('active') is False and r.get('playerUnchanged') is True)
        if name in ('prefer-fud', 'prefer-banana', 'distance', 'pause', 'native-wander'):
            expected = 'fud' if name in ('prefer-fud', 'pause', 'native-wander') else 'banana'
            other = 'banana' if expected == 'fud' else 'fud'
            check(name + ': exact desired inventory', r.get(expected) == 1 and r.get(other) == 0 and r.get('outcome') == 'picked-up')
        elif name != 'actor-removed':
            check(name + ': no inventory gain', r.get('fud') == r.get('banana') == 0)
        if name == 'new-command':
            check('New native command survived release', r.get('jobBig') == 'GoHere' and r.get('outcome') == 'new-native-command')
        elif name == 'actor-removed':
            check('Removed NPC enrollment cleared', r.get('enrolled') is False and r.get('outcome') in ('actor-removed', 'actor-dead') and r.get('retiredDead') is True)
        elif name in ('removed', 'timeout', 'refused', 'combat'):
            check(name + ': correct outcome', r.get('outcome') == {'removed': 'target-unavailable', 'timeout': 'timed-out', 'actor-removed': 'actor-removed', 'refused': 'pickup-unconfirmed', 'combat': 'emergency'}[name])
        if name in ('employment-before', 'following-before', 'recruit-before'):
            check(name + ': native commitment rejected pickup and enrollment', r.get('beginRejected') is True and r.get('enrollRejected') is True)
        if name in ('employment-before', 'employment-during', 'recruit-before', 'recruit-during'):
            check(name + ': employer preserved', type(r.get('player')) is int and r.get('employer') == r['player'] and r.get('hasEmployer') is True)
        if name in ('following-before', 'recruit-before', 'recruit-during'):
            check(name + ': following preserved', type(r.get('player')) is int and r.get('following') == r['player'] and r.get('hasFollowing') is True)
        if name == 'employment-during':
            check(name + ': employment interrupted action', r.get('outcome') == 'native-employment')
        if name == 'recruit-during':
            check(name + ': recruitment interrupted action and autonomy remained inhibited', r.get('outcome') in ('native-recruitment', 'native-employment') and r.get('decision') == 'native-employment')
        if name in ('specialist-task', 'replacement-root'):
            check(name + ': native job, codes and destination preserved', r.get('outcome') == 'native-job' and
                  r.get('job') == r.get('jobCode') == 'Ruckus' and r.get('jobBig') == '' and r.get('jobBigCode') == 'None' and
                  isinstance(r.get('assignedPos'), list) and len(r['assignedPos']) == 3 and r.get('assignedPos') == r.get('nativeDestination'))
        if name == 'replacement-root':
            check(name + ': exact replacement root preserved', r.get('replacementRootPreserved') is True)
        if name == 'native-wander':
            check(name + ': enrolled actual native wandering root and preserved default', r.get('enrollmentRoot') == 'GoalWanderFar' and r.get('defaultGoal') == 'WanderFar')
    pause = [r['data'] for r in report['native_tests'] if r['event'] == 'native-test-npc-pause']
    check('Pause preserved action, position, clock and inventory', len(pause) == 1 and pause[0].get('unchanged') is True)
    scope = [r['data'] for r in report['native_tests'] if r['event'] == 'native-test-npc-scope-ended']
    check('Native level reload cleared enrollment and action', len(scope) == 1 and scope[0].get('cleared') is True and
          scope[0].get('ready') is True and isinstance(scope[0].get('oldLevel'), int) and
          isinstance(scope[0].get('newLevel'), int) and scope[0]['newLevel'] > scope[0]['oldLevel'])
    blocked = [r for r in complete if r.get('case') == 'blocked']
    check('Native obstruction rejected item', len(blocked) == 1 and blocked[0].get('eligibility') == 'obstructed')
    events = report['native_tests']
    for name in ('prefer-fud', 'prefer-banana', 'distance', 'pause', 'refused', 'native-wander'):
        starts = [r for r in events if r['event'] == 'native-test-npc-desire-case' and r['data'].get('case') == name and r['data'].get('phase') == 'start']
        ends = [r for r in events if r['event'] == 'native-test-npc-desire-case' and r['data'].get('case') == name and r['data'].get('phase') == 'complete']
        window = [r['data'] for r in events if r['event'] == 'native-test-npc-probe' and len(starts) == len(ends) == 1 and starts[0]['seq'] < r['seq'] < ends[0]['seq']]
        calls = [r for r in window if r.get('phase') == 'pickup-called']
        check(name + ': exactly one native pickup call', len(calls) == 1)
        if name != 'refused':
            check(name + ': native ground retirement and exact transfer', len(calls) == 1 and calls[0].get('beforeCount') == 0 and calls[0].get('count') == 1 and calls[0].get('itemActive') is False)
        if name == 'native-wander':
            arrivals = [r for r in window if r.get('phase') == 'arrived']
            after = [r for r in window if r.get('phase') == 'native-after-release']
            check(name + ': actual approach from native wandering', len(arrivals) == 1 and (displacement(arrivals[0].get('start'), arrivals[0].get('position')) or 0) > 0.5)
            check(name + ': native behavior resumed after pickup', len(after) == 1 and after[0].get('root') in ('GoalIdle', 'GoalWanderFar') and after[0].get('actor') == starts[0]['data'].get('actor') and after[0].get('job') == after[0].get('jobBig') == '')
    for name in COORDINATION | {'new-command', 'combat'}:
        begin = [r for r in events if r['event'] == 'native-test-npc-desire-case' and r['data'].get('case') == name and r['data'].get('phase') == 'start']
        end = [r for r in events if r['event'] == 'native-test-npc-desire-case' and r['data'].get('case') == name and r['data'].get('phase') == 'complete']
        window = [r['data'] for r in events if r['event'] == 'native-test-npc-probe' and len(begin) == len(end) == 1 and begin[0]['seq'] < r['seq'] < end[0]['seq']]
        check(name + ': no native pickup call during interruption', len(begin) == len(end) == 1 and not any(r.get('phase') == 'pickup-called' for r in window))
        if name not in ('employment-before', 'following-before', 'recruit-before'):
            after = [r for r in window if r.get('phase') == 'native-after-release']
            check(name + ': native behavior observed after release', len(after) == 1 and isinstance(after[0].get('root'), str) and
                  after[0]['root'] not in ('', 'PickupGoal') and after[0].get('actor') == begin[0]['data'].get('actor') and type(after[0].get('actor')) is int)
    decisions = [r['data'].get('capture') for r in report['native_tests'] if r['event'] == 'native-test-npc-decision']
    check('Versioned decisions captured', bool(decisions) and all(isinstance(d, dict) and d.get('Input', {}).get('PolicyVersion') == 2 and 'NativeObligation' in d.get('Input', {}) and isinstance(d.get('Output'), dict) for d in decisions))

    def native_precedence(capture):
        if not isinstance(capture, dict) or not isinstance(capture.get('Input'), dict) or not isinstance(capture.get('Output'), dict):
            return False
        source, result = capture['Input'], capture['Output']
        obligation, current = source.get('NativeObligation'), source.get('CurrentTarget')
        if type(source.get('Emergency')) is not bool or obligation is not None and not isinstance(obligation, str):
            return False
        if source['Emergency']:
            return result.get('Intention') == 'cancel' and result.get('Target') is None and result.get('Reason') == 'native-emergency'
        if obligation is not None:
            return result.get('Intention') == ('cancel' if current is not None else 'idle') and result.get('Target') is None and result.get('Reason') == obligation
        return True

    check('Captured decisions respect danger and native obligations', bool(decisions) and all(native_precedence(d) for d in decisions))
    return dict(checks=checks)


def export_replays(report, root):
    """Export detached native decision envelopes for the pure policy runner.

    Existing captures must match exactly; a different result at the same path is evidence to
    preserve, not overwrite.
    """
    directory = root / 'npc-replays'
    directory.mkdir(exist_ok=True)
    for row in report['native_tests']:
        if row['event'] == 'native-test-npc-decision':
            path = directory / (str(row['seq']) + '.json')
            content = json.dumps(row['data']['capture'], indent=2) + '\n'
            if path.exists():
                if path.read_text() != content:
                    raise ValueError('Refusing to overwrite changed replay ' + str(path))
            else:
                path.write_text(content)
