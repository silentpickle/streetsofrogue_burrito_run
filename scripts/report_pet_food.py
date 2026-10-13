"""Independent checks for the bounded native dog-food experiment."""
import math


def number(value):
    return type(value) in (int, float) and math.isfinite(value)


def displacement(a, b):
    if not (isinstance(a, list) and isinstance(b, list) and len(a) >= 2 and len(b) >= 2 and
            all(number(v) for v in a[:2] + b[:2])):
        return None
    return math.hypot(a[0] - b[0], a[1] - b[1])


def assess(report):
    events = report['native_tests']
    checks = []

    def check(name, condition):
        checks.append(dict(name=name, passed=bool(condition)))

    def rows(name):
        return [r for r in events if r['event'] == 'native-test-' + name]

    starts, ends, cleanup = rows('food-start'), rows('food-ended'), rows('food-cleanup')
    check('One completed food experiment and native cleanup', len(starts) == len(ends) == len(cleanup) == 1 and
          ends[0]['data'].get('status') == 'COMPLETE' and starts[0]['seq'] < cleanup[0]['seq'] < ends[0]['seq'])
    baseline = starts[0]['data'].get('player') if len(starts) == 1 else None
    fixtures, discovered, cases = rows('food-fixture'), rows('food-discovered'), rows('food-case')
    check('Exactly five distinct bounded cases', len(fixtures) == len(discovered) == len(cases) == 5 and
          {r['data'].get('case') for r in cases} == {'stack', 'full', 'eat', 'removed', 'release'})
    keys = []
    for name in ('stack', 'full', 'eat', 'removed', 'release'):
        groups = [[r for r in group if r['data'].get('case') == name] for group in (fixtures, discovered, cases)]
        valid = all(len(g) == 1 for g in groups)
        check(name + ': one fixture/discovery/result', valid)
        if not valid:
            continue
        fixture, discovery, result = (g[0] for g in groups)
        f, snap, c = fixture['data'], discovery['data'].get('snapshot', {}), result['data']
        key = c.get('key'); keys.append(key)
        check(name + ': ordinary food census discovered exact native activation', isinstance(key, str) and bool(key) and
              snap.get('key') == key and snap.get('nativeId') == f.get('uid') and snap.get('instance') == f.get('instance') and
              snap.get('kind') == 'Item' and 'food' in snap.get('tags', []) and snap.get('active') is True and
              snap.get('destroyed') is False and snap.get('collection') in ('itemList', 'detail') and
              fixture['seq'] < discovery['seq'] < result['seq'])
        check(name + ': player health/money/inventory unchanged', isinstance(baseline, dict) and bool(baseline) and c.get('player') == baseline)
        notices = [r for r in rows('pet-food-noticed') if r['data'].get('key') == key]
        actions = [r for r in rows('pet-food-action') if r['data'].get('key') == key]
        if name == 'stack':
            check('Stack remains intact without a detour', f.get('count') == 2 and c.get('untouched') is True and not notices and not actions)
            continue
        check(name + ': one bounded notice from World Awareness', len(notices) == 1 and
              notices[0]['data'].get('snapshot', {}).get('key') == key and
              number(notices[0]['data'].get('distance')) and 0 <= notices[0]['data']['distance'] <= 6 and
              fixture['seq'] < notices[0]['seq'] < result['seq'])
        if name == 'release':
            check('Release cancels food and preserves native wandering', c.get('untouched') is True and
                  c.get('intent') == 'Wander' and c.get('following') is False and c.get('defaultGoal') == 'WanderFar' and not actions)
            continue
        action = c.get('action', {})
        check(name + ': one actual action followed by native Follow', len(actions) == 1 and actions[0]['data'] == action and
              action.get('key') == key and action.get('foodUid') == f.get('uid') and c.get('following') is True and
              c.get('rootGoal') == 'GoalFollow' and c.get('followingUid') == baseline.get('uid') and
              len(notices) == 1 and notices[0]['seq'] < actions[0]['seq'] < result['seq'])
        if name in ('full', 'eat'):
            approach = displacement(notices[0]['data'].get('petPosition'), action.get('petPosition')) if len(notices) == 1 else None
            check(name + ': measured arrival and actual approach', number(action.get('distance')) and 0 <= action['distance'] <= .8 and
                  number(approach) and approach >= .5)
        if name == 'full':
            check('Healthy sniff preserves food and health', action.get('outcome') == 'sniff-full-health' and
                  c.get('nativeActive') is True and c.get('count') == 1 and
                  number(f.get('health')) and f['health'] == f.get('maximum') == c.get('endHealth'))
        elif name == 'eat':
            check('Eating consumes exactly one native inventory item and heals', action.get('outcome') == 'ate' and
                  action.get('inventoryBefore') == 1 and action.get('inventoryAfter') == 0 and
                  all(number(action.get(k)) for k in ('healthBefore', 'healthAfter')) and
                  action['healthBefore'] == f.get('health') < action['healthAfter'] <= f.get('maximum', 0) and
                  c.get('endHealth') == action['healthAfter'])
            check('Consumed ground activation retired natively and in awareness', c.get('nativeActive') is False and c.get('indexedRetired') is True)
        else:
            check('Removed target cancels without eating', action.get('outcome') == 'food-gone-or-ineligible' and
                  action.get('inventoryBefore') == action.get('inventoryAfter') == 0 and
                  c.get('nativeActive') is False and c.get('indexedRetired') is True and c.get('startHealth') == c.get('endHealth'))
    check('Food cases use five distinct activation keys', len(keys) == 5 and all(isinstance(k, str) and k for k in keys) and len(set(keys)) == 5)
    cooldowns = rows('food-cooldown')
    check('Three eight-second cooldowns preserve Follow with no repeated action', len(cooldowns) == 3 and
          all(number(r['data'].get('seconds')) and r['data']['seconds'] >= 8 and r['data'].get('following') is True for r in cooldowns) and
          len(rows('pet-food-action')) == 3 and len(rows('pet-food-noticed')) == 4)
    check('Owned dog and last food retired natively and from index', len(cleanup) == 1 and
          all(cleanup[0]['data'].get(k) is False for k in ('actorActive', 'foodActive', 'indexedActorLive', 'indexedFoodLive')) and
          isinstance(cleanup[0]['data'].get('actorKey'), str) and cleanup[0]['data'].get('foodKey') == (keys[-1] if keys else None))
    check('Native pet removal retains the one-per-level spawn limit', len(cleanup) == 1 and cleanup[0]['data'].get('secondSpawnRejected') is True)
    check('Terminal player state remains unchanged', len(ends) == 1 and ends[0]['data'].get('player') == baseline)
    return dict(checks=checks, cases=[r['data'] for r in cases], terminal=[r['data'] for r in ends])


def render(result):
    return '\n'.join(['## Native dog food interaction', '',
                      'Controlled Courier dog fixture: stack rejection, healthy sniff, injured native eating/Follow, target removal and release.',
                      'Native healing/count changes and world retirement are required. This does not establish subjective clarity or combat/hazard acceptance.', ''] +
                     [f"- {'PASS' if c['passed'] else 'MISSING/FAIL'}: {c['name']}" for c in result['checks']]) + '\n'
