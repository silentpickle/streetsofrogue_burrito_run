"""Recompute evidence for native dog approach and cosmetic hydrant marking."""
import math


def number(value):
    return type(value) in (int, float) and math.isfinite(value)


def distance(a, b):
    if not (isinstance(a, list) and isinstance(b, list) and len(a) >= 2 and len(b) >= 2 and
            all(number(v) for v in a[:2] + b[:2])):
        return None
    return math.hypot(a[0] - b[0], a[1] - b[1])


def assess(report):
    events = report['native_tests']
    checks = []

    def rows(name):
        return [r for r in events if r['event'] == 'native-test-' + name]

    def check(name, condition):
        checks.append(dict(name=name, passed=bool(condition)))

    starts, ends, cleanup = rows('hydrant-start'), rows('hydrant-ended'), rows('hydrant-cleanup')
    baseline = starts[0]['data'].get('player') if len(starts) == 1 else None
    check('One complete hydrant experiment with owned cleanup', len(starts) == len(ends) == len(cleanup) == 1 and
          ends[0]['data'].get('status') == 'COMPLETE' and starts[0]['seq'] < cleanup[0]['seq'] < ends[0]['seq'])
    fixtures, discoveries, cases = rows('hydrant-fixture'), rows('hydrant-discovered'), rows('hydrant-case')
    check('Exactly four distinct hydrant cases', len(fixtures) == len(discoveries) == len(cases) == 4 and
          {r['data'].get('case') for r in cases} == {'mark', 'removed', 'timeout', 'release'})
    keys = []
    for name, expected in [('mark', 'marked'), ('removed', 'target-gone-or-ineligible'),
                           ('timeout', 'timed-out'), ('release', 'test-release')]:
        groups = [[r for r in group if r['data'].get('case') == name] for group in (fixtures, discoveries, cases)]
        valid = all(len(g) == 1 for g in groups)
        check(name + ': unique fixture/discovery/result', valid)
        if not valid:
            continue
        fixture, discovery, result = (g[0] for g in groups)
        f, snap, c = fixture['data'], discovery['data'].get('snapshot', {}), result['data']
        key = c.get('key'); keys.append(key)
        notices = [r for r in rows('pet-hydrant-noticed') if r['data'].get('key') == key]
        actions = [r for r in rows('pet-hydrant-action') if r['data'].get('key') == key]
        poses = [r for r in rows('pet-hydrant-marking') if r['data'].get('key') == key]
        action = c.get('action', {})
        check(name + ': exact activation discovered by ordinary hydrant census', isinstance(key, str) and bool(key) and
              snap.get('key') == key and snap.get('nativeId') == f.get('uid') and snap.get('instance') == f.get('instance') and
              snap.get('kind') == 'Object' and 'hydrant' in snap.get('tags', []) and snap.get('active') is True and
              snap.get('destroyed') is False and snap.get('collection') in ('objectRealList', 'detail') and
              fixture['seq'] < discovery['seq'] < result['seq'])
        check(name + ': one notice and actual terminal action', len(notices) == len(actions) == 1 and
              notices[0]['data'].get('snapshot', {}).get('key') == key and actions[0]['data'] == action and
              action.get('outcome') == expected and fixture['seq'] < notices[0]['seq'] < actions[0]['seq'] < result['seq'])
        notice = notices[0]['data'] if len(notices) == 1 else {}
        notice_distance = distance(notice.get('petPosition'), notice.get('targetPosition'))
        check(name + ': actual target lies within bounded notice radius', number(notice_distance) and 0 <= notice_distance <= 4)
        check(name + ': player and dog health preserved, visual cleared', isinstance(baseline, dict) and bool(baseline) and
              c.get('player') == baseline and number(f.get('health')) and c.get('health') == f['health'] and c.get('visual') is False)
        if name == 'release':
            check('Release cancels marking interest and restores native wandering', c.get('intent') == 'Wander' and
                  c.get('followingUid') is None and c.get('defaultGoal') == 'WanderFar' and not poses)
        else:
            check(name + ': actual native Follow resumes', c.get('following') is True and c.get('intent') == 'Follow' and
                  c.get('rootGoal') == 'GoalFollow' and isinstance(baseline, dict) and c.get('followingUid') == baseline.get('uid'))
        if name != 'removed':
            check(name + ': native hydrant state and position unchanged', c.get('hydrantUnchanged') is True and
                  c.get('hydrant') == f.get('hydrant') and c.get('hydrant', {}).get('destroyed') is False)
        else:
            check('Removed hydrant cancels before any marking pose', c.get('hydrant', {}).get('destroyed') is True and not poses)
        if name == 'mark':
            pose = poses[0]['data'] if len(poses) == 1 else {}
            approach = distance(notice.get('petPosition'), pose.get('petPosition'))
            arrival = distance(pose.get('petPosition'), notice.get('targetPosition'))
            check('One visible bounded marking pose near the actual hydrant', len(poses) == 1 and pose.get('visual') is True and
                  number(pose.get('distance')) and 0 <= pose['distance'] <= 1.6 and number(action.get('markSeconds')) and
                  number(approach) and approach >= .4 and number(arrival) and 0 <= arrival <= 1.6 and
                  1.25 <= action['markSeconds'] <= 2 and number(action.get('distance')) and 0 <= action['distance'] <= 1.6 and
                  notices[0]['seq'] < poses[0]['seq'] < actions[0]['seq'] if len(notices) == len(actions) == 1 else False)
            check('Temporary native idle anchor restored after marking',
                  distance(action.get('idleAnchorBefore'), action.get('idleAnchorAfter')) == 0)
        if name == 'timeout':
            notice_time = notices[0]['data'].get('gameTime') if len(notices) == 1 else None
            check('Unfinished approach cancels at the eight-second deadline without marking', not poses and
                  number(notice_time) and number(action.get('gameTime')) and 8 <= action['gameTime'] - notice_time <= 9)
    check('Four distinct native hydrant activation keys', len(keys) == 4 and all(isinstance(k, str) and k for k in keys) and len(set(keys)) == 4)
    quiet = rows('hydrant-quiet')
    check('Three full cooldowns with no repeated interest or visual leak', len(quiet) == 3 and
          all(number(r['data'].get('seconds')) and r['data']['seconds'] >= 45 and r['data'].get('player') == baseline for r in quiet) and
          len(rows('pet-hydrant-noticed')) == len(rows('pet-hydrant-action')) == 4 and len(rows('pet-hydrant-marking')) == 1)
    check('Marked hydrant remains unchanged beyond the cooldown', bool(keys) and
          any(r['data'].get('key') == keys[0] and r['data'].get('unchanged') is True for r in quiet))
    check('Owned native dog/hydrant and visual retire', len(cleanup) == 1 and
          cleanup[0]['data'].get('hydrantDestroyed') is True and
          all(cleanup[0]['data'].get(k) is False for k in ('dogActive', 'indexedDogLive', 'indexedHydrantLive', 'visual')))
    check('Terminal player state preserved', len(ends) == 1 and ends[0]['data'].get('player') == baseline)
    return dict(checks=checks, cases=[r['data'] for r in cases], terminal=[r['data'] for r in ends])


def render(result):
    return '\n'.join(['## Native dog hydrant interest', '',
                      'Approach, cosmetic marking and return; removal, timeout, release, repeat suppression and owned cleanup.',
                      'No native hydrant interaction, water/hazard effect or new NPC reaction is claimed.', ''] +
                     [f"- {'PASS' if c['passed'] else 'MISSING/FAIL'}: {c['name']}" for c in result['checks']]) + '\n'
