#!/usr/bin/env python3
"""Recompute the small native harness gates; never promote a captured COMPLETE to PASS."""
import argparse
import hashlib
import importlib.util
import json
import math
import re
import statistics
from pathlib import Path

SPEC = importlib.util.spec_from_file_location('world_report', Path(__file__).with_name('report-world.py'))
WORLD = importlib.util.module_from_spec(SPEC)
SPEC.loader.exec_module(WORLD)


def number(value):
    return type(value) in (int, float) and math.isfinite(value)


def assess(report, performance=False, pressure=False):
    checks = []

    def check(name, condition):
        checks.append(dict(name=name, passed=bool(condition)))

    events = report['native_tests']
    starts = [r for r in events if r['event'] == 'native-test-start']
    ends = [r for r in events if r['event'] == 'native-test-ended']
    check('Complete ordered capture, single session/build', not report['issues'])
    check('One revision-1 harness start, two repeats declared', len(starts) == 1 and
          starts[0]['data'].get('fixtureRevision') == 1 and starts[0]['data'].get('repeats') == 2)
    check('One completed terminal record', len(ends) == 1 and ends[0]['data'].get('status') == 'COMPLETE')
    keys = []
    levels = set()
    expected_order = []
    for repeat in (1, 2):
        group = {}
        for name in ('fixture', 'observation', 'cleanup', 'retired'):
            rows = [r for r in events if r['event'] == 'native-test-' + name and r['data'].get('repeat') == repeat]
            check(f'{repeat}: exactly one {name} record', len(rows) == 1)
            if len(rows) == 1:
                group[name] = rows[0]
        if len(group) != 4:
            continue
        expected_order += [group[n]['seq'] for n in ('fixture', 'observation', 'cleanup', 'retired')]
        levels.update(r['level'] for r in group.values())
        fixture, observed, cleanup, retired = (group[n]['data'] for n in ('fixture', 'observation', 'cleanup', 'retired'))
        native, indexed = observed.get('native', {}), observed.get('indexed', {})
        if not isinstance(native, dict) or not isinstance(indexed, dict):
            check(f'{repeat}: native/index snapshots available', False)
            continue
        key = indexed.get('key')
        keys.append(key)
        check(f'{repeat}: fixture identity discovered by ordinary awareness pipeline', isinstance(key, str) and bool(key) and
              native.get('key') == key and all(indexed.get(k) == native.get(k) for k in ('instance', 'nativeId', 'level')) and
              indexed.get('instance') == fixture.get('instance') and indexed.get('nativeId') == fixture.get('uid') and
              indexed.get('kind') == 'Actor' and indexed.get('name') == 'Courier' and
              indexed.get('collection') in ('agentList', 'detail') and indexed.get('active') is True and indexed.get('destroyed') is False)
        data = indexed.get('data', {})
        check(f'{repeat}: detailed native actor fields completed', isinstance(data, dict) and
              data.get('coverage') == 'native-fields-at-sample' and number(data.get('health')) and
              data.get('defaultGoal') == fixture.get('requestedGoal') == 'DoNothing' and
              isinstance(data.get('rootGoal'), str) and bool(data['rootGoal']))
        coords = [native.get('x'), native.get('y'), indexed.get('x'), indexed.get('y'),
                  native.get('gameTime'), indexed.get('gameTime')]
        check(f'{repeat}: fresh indexed position agrees with native read', all(number(v) for v in coords) and
              0 <= coords[4] - coords[5] <= 1 and math.hypot(coords[0] - coords[2], coords[1] - coords[3]) <= .1)
        before, after = observed.get('before'), observed.get('after')
        check(f'{repeat}: same-frame inspection preserves player/actor state', isinstance(before, dict) and
              all(isinstance(before.get(k), dict) and bool(before[k]) for k in ('actor', 'player')) and before == after)
        queries = observed.get('queries', [])
        query_ok = isinstance(queries, list) and len(queries) == 4
        seen = set()
        for query in queries if isinstance(queries, list) else []:
            if not isinstance(query, dict):
                query_ok = False
                continue
            range_name = query.get('range')
            origin = query.get('origin')
            lo, hi = query.get('min'), query.get('max')
            if not (isinstance(range_name, str) and isinstance(origin, list) and len(origin) == 2 and
                    all(number(v) for v in origin) and number(lo) and number(hi) and all(number(v) for v in coords[:4])):
                query_ok = False
                continue
            distance = math.hypot(indexed['x'] - origin[0], indexed['y'] - origin[1])
            included = (range_name == 'All' or range_name == 'Within' and distance <= hi or
                        range_name == 'Band' and lo <= distance <= hi or range_name == 'Outside' and distance > hi)
            query_ok &= included and query.get('keys') == [key] and type(query.get('total')) is int and query['total'] == 1
            seen.add(range_name)
        check(f'{repeat}: native within/band/outside/all query memberships', query_ok and
              seen == {'Within', 'Band', 'Outside', 'All'})
        perception, navigation, tile = (observed.get(k, {}) for k in ('perception', 'navigation', 'tile'))
        check(f'{repeat}: numeric perception, path and tile evidence', isinstance(perception, dict) and
              number(perception.get('distance')) and type(perception.get('clearLOS')) is bool and
              type(perception.get('insideCone')) is bool and type(perception.get('insideNativeRange')) is bool and
              isinstance(navigation, dict) and navigation.get('actor') == key and isinstance(navigation.get('state'), dict) and
              isinstance(tile, dict) and tile.get('available') is True and type(tile.get('possibleToWalk')) is bool and
              type(observed.get('cameraCount')) is int and observed['cameraCount'] > 0)
        check(f'{repeat}: no capture errors or drops at observation', all(type(observed.get(k)) is int and
              observed[k] == 0 for k in ('errors', 'worldDrops', 'workerDrops')))
        check(f'{repeat}: owned removal observed natively and in index', cleanup.get('key') == retired.get('key') == key and
              retired.get('nativeActive') is False and retired.get('indexedActive') is False)
    check('Two distinct activation keys on one native level', len(keys) == 2 and all(isinstance(k, str) for k in keys) and
          len(set(keys)) == 2 and len(levels) == 1 and next(iter(levels), 0) > 0)
    check('Setup/read/removal evidence stays in order', len(expected_order) == 8 and expected_order == sorted(expected_order) and
          len(starts) == len(ends) == 1 and starts[0]['seq'] < expected_order[0] < expected_order[-1] < ends[0]['seq'])
    comparison = assess_performance(report) if performance else None
    if comparison:
        checks += comparison['checks']
    pressure_result = assess_pressure(report) if pressure else None
    if pressure_result:
        checks += pressure_result['checks']
    capture_ok = checks and all(c['passed'] for c in checks)
    status = 'INCOMPLETE/FAIL' if not capture_ok else 'NEEDS INVESTIGATION' if comparison and not comparison['numericPass'] else 'PASS'
    return dict(status=status,
                identities=report['identities'], checks=checks, issues=report['issues'],
                terminal=[r['data'] for r in ends], comparison=comparison, pressure=pressure_result)


def assess_pressure(report):
    checks = []
    def check(name, condition):
        checks.append(dict(name=name, passed=bool(condition)))
    events = report['native_tests']
    group = {}
    for name in ('start', 'stopped', 'detail', 'recovered', 'retired'):
        matches = [r for r in events if r['event'] == 'native-test-pressure-' + name]
        check('Pressure: exactly one ' + name, len(matches) == 1)
        if len(matches) == 1:
            group[name] = matches[0]
    if len(group) != 5:
        return dict(checks=checks, batches=0, effects=0, pooledInstances=0)
    start, stop, detail, recovered, retired = (group[n] for n in ('start', 'stopped', 'detail', 'recovered', 'retired'))
    core = start['data'].get('core') or {}
    full = detail['data'].get('indexed') or {}
    se = start['data'].get('evidence') or {}
    re = recovered['data'].get('evidence') or {}
    check('Pressure: clean normal baseline and declared bounded fixture',
          start['data'].get('maxBatches') == 32 and start['data'].get('batchSize') == 64 and
          se.get('quality') == 0 and se.get('clean') is True)
    check('Pressure: lifecycle core becomes ordinary native actor detail',
          isinstance(core.get('key'), str) and bool(core['key']) and core.get('key') == full.get('key') == retired['data'].get('key') and
          core.get('kind') == full.get('kind') == 'Actor' and core.get('name') == full.get('name') == 'Courier' and
          all(core.get(k) == full.get(k) for k in ('instance', 'nativeId', 'level', 'activation')) and
          core.get('collection', '').startswith('lifecycle:') and
          core.get('data', {}).get('coverage') == 'lifecycle-core; census-details-pending; viewport-unknown' and
          full.get('collection') in ('agentList', 'detail') and full.get('data', {}).get('coverage') == 'native-fields-at-sample' and
          full.get('data', {}).get('defaultGoal') == 'DoNothing' and number(full.get('data', {}).get('health')) and
          isinstance(full.get('data', {}).get('rootGoal'), str) and bool(full['data']['rootGoal']) and full.get('active') is True and
          number(core.get('gameTime')) and number(full.get('gameTime')) and number(detail.get('gameTime')) and
          core['gameTime'] <= full['gameTime'] <= detail['gameTime'] and detail['gameTime'] - full['gameTime'] <= 1)
    samples = [r for r in events if r['event'] == 'native-test-pressure-sample']
    evidence = [stop['data'].get('evidence', {})] + [r['data'] for r in samples]
    reduced = [e for e in evidence if type(e.get('quality')) is int and 1 <= e['quality'] <= 4]
    def real_pressure(e):
        return (e.get('reason') == 'writer-backpressure' and e.get('saturated') is True or
                e.get('reason') == 'frame-budget' and number(e.get('frameWorkMs')) and e['frameWorkMs'] > 2 or
                e.get('reason') == 'window-budget' and number(e.get('windowMeanMs')) and
                (e['windowMeanMs'] > 1 or type(e.get('windowFrames')) is int and type(e.get('windowOverBudget')) is int and
                 e['windowFrames'] > 0 and e['windowOverBudget'] * 20 > e['windowFrames']))
    check('Pressure: actual observed reduction and reduced detail policy', bool(reduced) and
          any(real_pressure(e) for e in reduced) and all(e.get('nearbyActorLimit') == 2 for e in reduced))
    check('Pressure: natural recovery after at least 30 seconds', number(stop.get('wallSeconds')) and
          number(recovered.get('wallSeconds')) and 30 <= recovered['wallSeconds'] - stop['wallSeconds'] <= 150 and
          re.get('quality') == 0 and re.get('reason') == 'gradual-recovery' and re.get('clean') is True and
          re.get('nearbyActorLimit') == 8 and re.get('interval') == .2)
    contexts = [start['data'].get('context', {}), recovered['data'].get('context', {})]
    fields = ('level', 'controller', 'floor', 'playerInstance', 'playerUid', 'health', 'focused', 'cameras')
    positions = [c.get('position') for c in contexts]
    check('Pressure: stationary player and camera/focus context',
          all(k in contexts[0] and contexts[0][k] == contexts[1].get(k) for k in fields) and
          all(c.get('combat') is False for c in contexts) and
          all(isinstance(p, list) and len(p) == 3 and all(number(x) for x in p) for p in positions) and math.dist(*positions) <= .05)
    batches = [r for r in events if r['event'] == 'native-test-pressure-batch']
    check('Pressure: emission bounded and batch sequence complete', 0 < len(batches) <= 32 and
          [r['data'].get('batch') for r in batches] == list(range(1, len(batches) + 1)) and
          stop['data'].get('batches') == len(batches) and all(start['seq'] < r['seq'] < stop['seq'] for r in batches))
    keys = []
    instances = {}
    effects_ok = True
    births = {r['data'].get('key'): r for r in report.get('effects', []) if r['event'] == 'effect-created'}
    pools = {r['data'].get('key'): r for r in report.get('effects', []) if r['event'] == 'effect-retire-request' and
             r['data'].get('method') == 'PoolRecycle'}
    for batch in batches:
        effects = batch['data'].get('effects', [])
        effects_ok &= isinstance(effects, list) and len(effects) == 64
        for effect in effects if isinstance(effects, list) else []:
            if not isinstance(effect, dict) or not isinstance(effect.get('indexed'), dict):
                effects_ok = False
                continue
            entity = effect['indexed']; key = entity.get('key'); native = entity.get('nativeId'); instance = entity.get('instance')
            if not isinstance(key, str) or type(instance) is not int or type(native) is not int:
                effects_ok = False
                continue
            keys.append(key); instances.setdefault(instance, set()).add((native, key))
            birth, pool = births.get(key), pools.get(key)
            effects_ok &= (entity.get('kind') == 'Noise' and entity.get('active') is True and
                           entity.get('collection', '').startswith('lifecycle:') and effect.get('volume') == 0 and
                           effect.get('dontAttract') is True and effect.get('nativeActiveAfterCleanup') is False and
                           birth is not None and pool is not None and
                           start['seq'] < birth['seq'] < pool['seq'] < batch['seq'] and
                           birth['data'].get('nativeId') == native and birth['data'].get('instance') == instance)
    check('Pressure: every fixture effect has native activation and actual recycling', effects_ok and
          len(keys) == 64 * len(batches) and len(keys) == len(set(keys)))
    pooled = sum(len(ids) > 1 for ids in instances.values())
    check('Pressure: native pooled instance reuse has fresh native IDs/keys', pooled > 0 and
          all(len({native for native, _ in ids}) == len(ids) for ids in instances.values()))
    check('Pressure: witness natively/index retired after recovery', retired['data'].get('nativeActive') is False and
          retired['data'].get('indexedActive') is False)
    terminal = [r for r in events if r['event'] == 'native-test-ended']
    declared = [r for r in events if r['event'] == 'native-test-start']
    functional_retirements = [r for r in events if r['event'] == 'native-test-retired']
    check('Pressure: complete ordered single-level scope and terminal declaration', len(terminal) == len(declared) == 1 and
          declared[0]['data'].get('pressure') is True and terminal[0]['data'].get('pressure') is True and
          len(functional_retirements) == 2 and functional_retirements[-1]['seq'] < start['seq'] < stop['seq'] <
          detail['seq'] < recovered['seq'] < retired['seq'] < terminal[0]['seq'] and
          len({r['level'] for r in group.values()}) == 1 and all(e.get('clean') is True for e in evidence))
    return dict(checks=checks, batches=len(batches), effects=len(keys), pooledInstances=pooled,
                reducedQuality=max([e['quality'] for e in reduced], default=0), stop=stop['data'], recovered=recovered['data'])


def assess_performance(report):
    checks = []
    def check(name, condition):
        checks.append(dict(name=name, passed=bool(condition)))
    events = report['native_tests']
    starts = [r for r in events if r['event'] == 'native-test-performance-start']
    ends = [r for r in events if r['event'] == 'native-test-performance-trial']
    declared = [r['data'] for r in events if r['event'] in ('native-test-start', 'native-test-ended')]
    check('Performance requested and completed explicitly', len(declared) == 2 and
          all(d.get('performance') is True for d in declared) and declared[-1].get('trialsStarted') == 9)
    check('Exactly nine performance starts and nine results', len(starts) == len(ends) == 9)
    rows = []
    baseline_context = starts[0]['data'].get('context') if starts else None
    stable_fields = ('level', 'controller', 'floor', 'playerInstance', 'playerUid', 'health', 'focused', 'cameras')
    def stable(context):
        if not isinstance(context, dict) or not isinstance(baseline_context, dict):
            return False
        positions = (context.get('position'), baseline_context.get('position'))
        return (all(k in context and k in baseline_context and context[k] == baseline_context[k] for k in stable_fields) and
                context.get('combat') is False and type(context.get('focused')) is bool and
                type(context.get('nativeActors')) is int and context['nativeActors'] > 0 and
                isinstance(context.get('cameras'), list) and len(context['cameras']) > 0 and
                all(isinstance(p, list) and len(p) == 3 and all(number(v) for v in p) for p in positions) and
                math.dist(*positions) <= .05)
    sequences = []
    for index in range(1, 10):
        ss = [r for r in starts if r['data'].get('trial') == index]
        es = [r for r in ends if r['data'].get('trial') == index]
        mode = ('Off', 'Inspect', 'Record')[(index - 1) % 3]
        if len(ss) != 1 or len(es) != 1:
            check(f'Trial {index}: one ordered start/result', False)
            continue
        start, end = ss[0], es[0]
        start_context = start['data'].get('context')
        end_context = end['data'].get('context')
        start_context = start_context if isinstance(start_context, dict) else {}
        end_context = end_context if isinstance(end_context, dict) else {}
        sequences.extend((start['seq'], end['seq']))
        result = end['data'].get('result')
        check(f'Trial {index}: stable stationary player, level, camera mapping and focus',
              stable(start['data'].get('context')) and stable(end['data'].get('context')))
        if not isinstance(result, dict):
            check(f'Trial {index}: measured summary available', False)
            continue
        check(f'Trial {index}: correct mode, full warm-up/measurement, valid capture',
              start['data'].get('mode') == result.get('mode') == mode and result.get('scenario') == 'harness-stationary' and
              result.get('valid') is True and result.get('reason') is None and result.get('warmupSeconds') == 10 and
              number(result.get('durationSeconds')) and 60 <= result['durationSeconds'] <= 75 and
              number(start.get('wallSeconds')) and number(end.get('wallSeconds')) and
              70 <= end['wallSeconds'] - start['wallSeconds'] <= 85 and
              type(result.get('frames')) is int and result['frames'] > 0 and
              result.get('workerDropped') == result.get('worldDropped') == 0 and
              result.get('level') == start_context.get('level'))
        metrics = ('frameMedianMs', 'frameP95Ms', 'frameP99Ms', 'diagnosticMeanMs', 'diagnosticP95Ms')
        check(f'Trial {index}: finite timing and hitch evidence', all(number(result.get(k)) and result[k] >= 0 for k in metrics) and
              type(result.get('frames')) is int and result['frames'] > 0 and
              type(result.get('framesOver50Ms')) is int and 0 <= result['framesOver50Ms'] <= result['frames'] and
              type(result.get('nativeFreezeFrames')) is int and 0 <= result['nativeFreezeFrames'] <= result.get('frames', 0))
        rows.append(dict(trial=index, **result, nativeActorsStart=start_context.get('nativeActors'),
                         nativeActorsEnd=end_context.get('nativeActors')))
    terminal = [r for r in events if r['event'] == 'native-test-ended']
    retired = [r for r in events if r['event'] == 'native-test-retired']
    check('All trials ordered after fixture retirement and before terminal', len(sequences) == 18 and
          sequences == sorted(set(sequences)) and len(terminal) == 1 and len(retired) == 2 and
          retired[-1]['seq'] < sequences[0] < sequences[-1] < terminal[0]['seq'])
    check('Native trial summaries agree with independent harness evidence', report.get('trials') ==
          [r['data'].get('result') for r in ends])
    numeric_pass = False
    comparisons = []
    if all(c['passed'] for c in checks):
        groups = {m: [r for r in rows if r['mode'] == m] for m in ('Off', 'Inspect', 'Record')}
        baseline = statistics.median(r['frameP95Ms'] for r in groups['Off'])
        for mode in ('Inspect', 'Record'):
            p95 = statistics.median(r['frameP95Ms'] for r in groups[mode])
            delta = (p95 / baseline - 1) if baseline > 0 else None
            within = (0 < baseline < 1000 / 30 and delta <= .05 and p95 < 1000 / 30 and
                      all(r['diagnosticMeanMs'] < 1 and r['diagnosticP95Ms'] < 2 for r in groups[mode]))
            comparisons.append(dict(mode=mode, baselineP95Ms=baseline, frameP95Ms=p95, change=delta, withinTarget=within))
        numeric_pass = all(c['withinTarget'] for c in comparisons)
    return dict(checks=checks, trials=rows, comparisons=comparisons, numericPass=numeric_pass,
                focused=baseline_context.get('focused') if isinstance(baseline_context, dict) else None)


def render(result):
    lines = ['# Native World Awareness harness', '', result['status'], '', f"Build/session: {result['identities']}", '',
             'Scope: two controlled native Courier fixtures. These gates cover discovery, detailed actor metadata,',
             'four query memberships, perception/navigation/tile capture, same-frame read invariance and owned retirement.',
             ('Stationary performance evidence follows; full World Awareness, busy load, input isolation, level transitions and dog behavior remain open.'
              if result.get('comparison') else
              'This does not certify full World Awareness, performance, input isolation, level transitions or dog behavior.'), '']
    lines += [f"- {'PASS' if c['passed'] else 'MISSING/FAIL'}: {c['name']}" for c in result['checks']]
    lines += ['', 'Terminal: ' + json.dumps(result['terminal']), '', 'Capture issues: ' + json.dumps(result['issues'])]
    comparison = result.get('comparison')
    if comparison:
        lines += ['', '## Stationary performance comparison', '',
                  'One generated level/build, three warmed trials per mode. Residents continue simulating.',
                  'Focus stability and player guards run throughout; camera mapping is checked at boundaries.',
                  'Common harness coordinator cost is included. This does not certify busy load, allocations or other hardware.', '',
                  f"Focus at baseline: {comparison['focused']} (false means background capture).", '',
                  '| Trial | Mode | Seconds | Frames | Frame p95 ms | Diagnostic mean / p95 ms | >50 ms | Native actors start / end | Quality |',
                  '|---|---|---|---|---|---|---|---|---|']
        for t in comparison['trials']:
            def fmt(key):
                value = t.get(key)
                return f'{value:.3f}' if number(value) else str(value)
            lines.append(f"| {t['trial']} | {t.get('mode')} | {fmt('durationSeconds')} | {t.get('frames')} | {fmt('frameP95Ms')} | "
                         f"{fmt('diagnosticMeanMs')} / {fmt('diagnosticP95Ms')} | {t.get('framesOver50Ms')} | "
                         f"{t.get('nativeActorsStart')} / {t.get('nativeActorsEnd')} | {t.get('quality')} |")
        for c in comparison['comparisons']:
            delta = f"{c['change']:.2%}" if c['change'] is not None else 'undefined'
            lines.append(f"\n{c['mode']}: {'WITHIN NUMERIC TARGET' if c['withinTarget'] else 'NEEDS INVESTIGATION'}; "
                         f"median trial frame p95 {c['frameP95Ms']:.3f} ms, Off {c['baselineP95Ms']:.3f} ms, change {delta}.")
    pressure = result.get('pressure')
    if pressure:
        lines += ['', '## Controlled native pressure/recovery', '',
                  f"Batches: {pressure['batches']}; native effects: {pressure['effects']}; reused native instances: {pressure['pooledInstances']}.",
                  'Attraction-disabled Noise fixtures were recycled immediately. Only actual inspector work drives quality.',
                  'This validates the bounded lifecycle burst, not paired busy performance or allocation/hardware acceptance.', '',
                  'Evidence: ' + json.dumps({k: v for k, v in pressure.items() if k != 'checks'})]
    return '\n'.join(lines) + '\n'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('run', type=Path)
    args = parser.parse_args()
    args.run = args.run.resolve()
    paths = sorted((args.run / 'logs').glob('world-*-????.jsonl'))
    if not paths:
        parser.exit(2, 'No native evidence captured; see run logs.\n')
    try:
        report = WORLD.analyze(paths)
        receipt = json.loads((args.run / 'harness-run.json').read_text())
        binary_hash = hashlib.sha256((args.run / 'BurritoRun.dll').read_bytes()).hexdigest()
        if receipt.get('dllSha256') != binary_hash:
            report['issues'].append('preserved fixture binary does not match run receipt')
        if receipt.get('profile') != str(args.run / 'profile'):
            report['issues'].append('run receipt does not identify this isolated profile')
        for path in (args.run / 'logs').glob('game-*.log'):
            if re.search(r'^\s*(?:[\w.]+)?\w*Exception:', path.read_text(errors='replace'), re.MULTILINE):
                report['issues'].append('native engine exception recorded; see ' + path.name)
        manifests = list((args.run / 'logs').glob('world-*-manifest.json'))
        if len(manifests) != 1:
            report['issues'].append('expected one world manifest')
        else:
            manifest = json.loads(manifests[0].read_text())
            if manifest.get('closed') is not True or manifest.get('dropped') or manifest.get('retiredSegments') or manifest.get('failure'):
                report['issues'].append('world manifest incomplete/loss/rotation/error')
        starts = [r['data'] for r in report['native_tests'] if r['event'] == 'native-test-start']
        performance = receipt.get('performance', False)
        pressure = receipt.get('pressure', False)
        if type(performance) is not bool or len(starts) != 1 or starts[0].get('performance', False) != performance:
            report['issues'].append('performance mode receipt does not agree with native start')
        if type(pressure) is not bool or len(starts) != 1 or starts[0].get('pressure', False) != pressure or pressure and performance:
            report['issues'].append('pressure mode receipt does not agree with native start')
        result = assess(report, performance=bool(performance), pressure=bool(pressure))
        result['dllSha256'] = binary_hash
        (args.run / 'report.md').write_text(render(result))
        (args.run / 'report.json').write_text(json.dumps(result, indent=2) + '\n')
        (args.run / 'world-report.md').write_text(WORLD.render(report))
        print(f"{result['status']}: {args.run / 'report.md'}")
        return 0 if result['status'] == 'PASS' else 2
    except (OSError, ValueError, TypeError, KeyError) as error:
        parser.exit(2, str(error) + '\n')


if __name__ == '__main__':
    raise SystemExit(main())
