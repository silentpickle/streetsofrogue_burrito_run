"""Independent assertions for bounded readiness extensions, using actual native records."""
import json
import math


def assess(report, mode, performance):
    checks = []
    def check(name, condition):
        checks.append(dict(name='Readiness: ' + name, passed=bool(condition)))
    events = report['native_tests']
    def rows(name):
        return [r for r in events if r['event'] == 'native-test-readiness-' + name]
    def one(name):
        found = rows(name)
        check('one ' + name, len(found) == 1)
        return found[0] if len(found) == 1 else {'seq': -1, 'data': {}, 'wallSeconds': 0}
    start, end, cleanup = one('start'), one('ended'), one('cleanup')
    declarations = [r['data'] for r in events if r['event'] in ('native-test-start', 'native-test-ended')]
    check('extension receipt agrees with native records', mode in ('advanced', 'busy', 'native') and
          len(declarations) == 2 and all(d.get('extension') == mode for d in declarations) and
          start['data'].get('mode') == end['data'].get('mode') == mode)
    check('completed and owned fixtures retired natively/indexed', end['data'].get('status') == 'COMPLETE' and
          start['seq'] < cleanup['seq'] < end['seq'] and cleanup['data'].get('activeKeys') == [] and
          ((mode != 'native' and cleanup['data'].get('nativeActive') is False) or
           cleanup['data'].get('nativeLive') is False and len(cleanup['data'].get('retirement', [])) == 4 and
           all(e.get('nativeLive') is False and (e.get('nativeActive') is False or e.get('nativeDestroyed') is True) for e in cleanup['data']['retirement'])) and cleanup['data'].get('ownedCount') == 4 and
          len(cleanup['data'].get('keys', [])) == len(set(cleanup['data'].get('keys', []))) == 4)
    comparison = None
    def actor_count(frame):
        return sum(e.get('kind') == 'Actor' for e in (frame or {}).get('entities', []))
    if mode == 'advanced':
        baseline = one('crowd-baseline')
        check('ordinary crowded detail before pressure', baseline['data'].get('frame', {}).get('quality') == 0 and
              actor_count(baseline['data'].get('frame')) >= 3)
        windows = {}
        for row in rows('load-sample') + rows('window-reduced'):
            e = row['data'].get('evidence', {})
            if row['data'].get('stage') in (None, 'window') and e.get('windowWallSeconds', 0) > 0:
                windows[e['windowWallSeconds']] = e
        ordered = sorted(windows.items())
        def pressured(e):
            return e.get('windowMeanMs', 0) > 1 or e.get('windowOverBudget', 0) * 20 > e.get('windowFrames', 0)
        check('two consecutive actual pressured windows', any(
            1 <= b[0] - a[0] <= 1.2 and pressured(a[1]) and pressured(b[1]) for a, b in zip(ordered, ordered[1:])))
        for prefix, reason in (('window', 'window-budget'), ('backpressure', 'writer-backpressure')):
            reduced, recovered = one(prefix + '-reduced'), one(prefix + '-recovered')
            evidence = reduced['data'].get('evidence', {})
            restore = recovered['data'].get('evidence', {})
            real = (evidence.get('windowMeanMs', 0) > 1 or evidence.get('windowOverBudget', 0) * 20 > evidence.get('windowFrames', 0)) if prefix == 'window' else evidence.get('saturated') is True
            check(prefix + ': real policy trigger and bounded emissions', evidence.get('reason') == reason and real and
                  evidence.get('quality', 0) > 0 and evidence.get('nearbyActorLimit') == 2 and
                  0 < reduced['data'].get('emitted', 0) <= (4800 if prefix == 'window' else 1024))
            samples = [r for r in rows('sample') if r['data'].get('stage') == prefix + '-recover']
            reduced_frames = [r['data'].get('frame') or {} for r in samples if (r['data'].get('frame') or {}).get('quality', 0) > 0]
            check(prefix + ': actual crowded detail shedding', bool(reduced_frames) and any(
                  actor_count(f) <= 2 + sum(e.get('kind') == 'Actor' and bool(e.get('key')) and e.get('key') in (f.get('observer'), f.get('target')) for e in f.get('entities', []))
                  for f in reduced_frames))
            check(prefix + ': natural recovery and actual detail restoration', 30 <= recovered['wallSeconds'] - reduced['wallSeconds'] <= 150 and
                  reduced['seq'] < recovered['seq'] < cleanup['seq'] and restore.get('quality') == 0 and
                  restore.get('reason') == 'gradual-recovery' and restore.get('nearbyActorLimit') == 8 and restore.get('clean') is True and
                  recovered['data'].get('frame', {}).get('quality') == 0 and actor_count(recovered['data'].get('frame')) >= 3)
        fault = one('slow-writer')
        check('backpressure fault is explicitly bounded and labeled', fault['data'].get('milliseconds') == 2000 and
              'not performance evidence' in fault['data'].get('scope', ''))
    elif mode == 'busy':
        comparison = performance(report, busy=True)
        checks += comparison['checks']
        trials = rows('busy-trial')
        starts = rows('busy-start')
        def identities(row):
            return [(a.get('state', {}).get('actor', {}).get('instance'), a.get('state', {}).get('actor', {}).get('uid')) for a in row['data'].get('actors', [])]
        baseline_ids = identities(starts[0]) if starts else []
        check('same four native actor identities throughout all trial boundaries', len(baseline_ids) == len(set(baseline_ids)) == 4 and
              all(type(instance) is int and type(uid) is int for instance, uid in baseline_ids) and
              all(identities(r) == baseline_ids for r in starts + trials))
        check('identical bounded native activity and actual movement in nine trials', len(trials) == 9 and all(
            r['data'].get('pulses') == 300 and r['data'].get('noises') == 1200 and r['data'].get('projectiles') == 300 and
            r['data'].get('commands') == 18 and r['data'].get('movement', 0) > 0 and len(r['data'].get('actors', [])) == 4 for r in trials))
    elif mode == 'native':
        objects = one('objects')['data'].get('entities', [])
        check('fresh ordinary food/hydrant/door classifications', len(objects) == 3 and all(
            e.get('nativeType') == kind and tag in e.get('tags', []) and e.get('active') is True and e.get('destroyed') is False and
            not e.get('collection', '').startswith('lifecycle:') and e.get('data', {}).get('coverage') == 'native-fields-at-sample'
            for e, kind, tag in zip(objects, ('Item', 'FireHydrant', 'Door'), ('food', 'hydrant', 'door'))))
        doors = rows('door-state')
        check('three native door metadata states and read invariance', len(doors) == 3 and all(
            r['data'].get('state') == i and r['data'].get('native', {}).get('data', {}).get('open') == (i == 1) and
            r['data'].get('native', {}).get('data', {}).get('locked') == (i == 2) and
            isinstance(r['data'].get('before'), dict) and r['data']['before'] == r['data'].get('after')
            for i, r in enumerate(doors)))
        before, paused, resumed = one('ui-before'), one('ui-paused'), one('ui-resumed')
        check('API inspector open pauses and gates world input', paused['data'].get('open') is True and
              paused['data'].get('blocked') is True and paused['data'].get('timeScale') == 0)
        check('API close resumes and preserves player state', resumed['data'].get('open') is False and
              resumed['data'].get('blocked') is False and resumed['data'].get('timeScale') == 1 and
              before['data'].get('player') == resumed['data'].get('player') and before['seq'] < paused['seq'] < resumed['seq'])
        request, reload = one('reload-request'), one('reloaded')
        check('native scope reload invalidates old activation keys', cleanup['seq'] < request['seq'] < reload['seq'] < end['seq'] and
              reload['data'].get('newLevel', 0) > reload['data'].get('oldLevel', 0) > 0 and
              reload['data'].get('staleKeys') == [] and len(reload['data'].get('oldKeys', [])) == 4 and
              bool(reload['data'].get('oldWorldKeys')) and reload['data'].get('oldWorldKeys') == request['data'].get('oldWorldKeys') and
              reload['data'].get('staleWorldKeys') == [])
    return dict(mode=mode, checks=checks, comparison=comparison,
                evidence=[r for r in events if r['event'].startswith('native-test-readiness-')])


def render(result):
    lines = ['## Bounded readiness: ' + result['mode'], '',
             'Scope: owned native fixtures only. Busy activity is actor motion plus transient Noise/projectile lifecycles, not combat/hazard equivalence.',
             'Door checks cover metadata fields, not collider traversal. UI checks use the native API, not physical key/mouse input.',
             'Native reload covers scope reset, not elevator/floor progression. Other hardware and full release matrices remain open.', '']
    comparison = result.get('comparison')
    if comparison:
        for row in comparison['trials']:
            lines.append(f"Trial {row['trial']} {row['mode']}: frame p95 {row['frameP95Ms']:.3f} ms; diagnostics {row['diagnosticMeanMs']:.3f}/{row['diagnosticP95Ms']:.3f} ms mean/p95; quality {row.get('quality')}.")
        lines += ['', 'Numeric comparisons: ' + json.dumps(comparison['comparisons'])]
    return '\n'.join(lines)
