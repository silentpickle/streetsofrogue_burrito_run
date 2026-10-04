#!/usr/bin/env python3
"""Read world awareness JSONL without importing Unity or trusting capture verdicts."""
import argparse
import collections
import json
import math
from pathlib import Path

MAX_RECORD_BYTES = 8 * 1024 * 1024
MAX_TOTAL_BYTES = 256 * 1024 * 1024


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate key: {key}")
        result[key] = value
    return result


def reject_constant(value):
    raise ValueError(f"non-finite JSON: {value}")


def percentile(values, fraction):
    if not values:
        return None
    ordered = sorted(values)
    return ordered[int((len(ordered) - 1) * fraction)]


def analyze(paths):
    counts = collections.Counter()
    issues = []
    performance = []
    trials = []
    effects = []
    native_tests = []
    identities = set()
    last = 0
    total_bytes = 0
    latest_sample = None
    latest_incident = None
    created = set()
    retired = set()
    last_census = None
    levels = set()
    for path in sorted(paths):
        with Path(path).open('rb') as stream:
            number = 0
            while True:
                raw = stream.readline(MAX_RECORD_BYTES + 1)
                if not raw:
                    break
                number += 1
                total_bytes += len(raw)
                if len(raw) > MAX_RECORD_BYTES or total_bytes > MAX_TOTAL_BYTES:
                    raise ValueError('capture exceeds reader size limits')
                try:
                    if not raw.endswith(b'\n'):
                        raise ValueError('truncated record')
                    record = json.loads(raw, object_pairs_hook=unique_object, parse_constant=reject_constant)
                    if not isinstance(record, dict) or type(record.get('schema')) is not int or record.get('schema') != 1 or record.get('stream') != 'world':
                        raise ValueError('unsupported world schema')
                    if not all(isinstance(record.get(k), str) and record[k] for k in ('session', 'build')):
                        raise ValueError('invalid session/build identity')
                    if type(record.get('level')) is not int:
                        raise ValueError('invalid level')
                    seq = record['seq']
                    if type(seq) is not int or seq <= 0:
                        raise ValueError('invalid sequence')
                    if seq != last + 1:
                        issues.append(f'sequence discontinuity {last} -> {seq}; missing or rotated evidence')
                    last = seq
                    identities.add((record['session'], record['build']))
                    if not isinstance(record['event'], str) or not isinstance(record['data'], dict):
                        raise ValueError('invalid event/data')
                    for clock in ['gameTime', 'wallSeconds']:
                        value = record[clock]
                        if isinstance(value, bool) or not isinstance(value, (int, float)) or not math.isfinite(value):
                            raise ValueError('invalid clock')
                    levels.add(record['level'])
                    kind, data = record['event'], record['data']
                    counts[kind] += 1
                    if kind.startswith('native-test-'):
                        native_tests.append(record)
                    if record.get('streamDropped', 0):
                        issues.append('world writer dropped records')
                    if kind == 'capture-error':
                        issues.append('native capture error: ' + str(data.get('reason')))
                    if kind == 'performance':
                        performance.append(data)
                        if data.get('workerDropped') or data.get('worldDropped') or data.get('captureErrors') or data.get('error'):
                            issues.append('performance record reports capture loss/error')
                    elif kind == 'session-ended' and (data.get('errors') or data.get('workerDrops') or data.get('worldDrops')):
                        issues.append('shutdown record reports capture loss/error')
                    elif kind == 'benchmark-trial':
                        trials.append(data)
                    elif kind in ('effect-created', 'effect-retire-request'):
                        effects.append(record)
                        if kind == 'effect-created' and data.get('key'):
                            created.add(data['key'])
                        elif data.get('key'):
                            retired.add(data['key'])
                    elif kind in ('sample', 'incident-frame'):
                        latest_sample = data
                    elif kind == 'incident':
                        latest_incident = data
                    elif kind == 'census':
                        last_census = data
                    elif kind == 'effect-created' and data.get('key'):
                        created.add(data['key'])
                    elif kind == 'effect-retire-request' and data.get('key'):
                        retired.add(data['key'])
                except (ValueError, KeyError, TypeError, UnicodeDecodeError) as error:
                    issues.append(f'{Path(path).name}:{number}: {error}')
    if not counts:
        issues.append('empty capture')
    if len(identities) != 1:
        issues.append('missing or mixed session/build identity')
    if not counts['session-ended']:
        issues.append('session still active or missing shutdown record')
    return dict(counts=dict(counts), issues=sorted(set(issues)), identities=sorted(identities),
                levels=sorted(levels), bytes=total_bytes, latest_sample=latest_sample,
                latest_incident=latest_incident, census=last_census, performance=performance,
                trials=trials, paired_effects=len(created & retired), native_tests=native_tests, effects=effects)


def finite_nonnegative(value):
    return type(value) in (int, float) and math.isfinite(value) and value >= 0


def assess_trials(trials):
    rows = []
    for scenario in ('ordinary', 'busy'):
        selected = [t for t in trials if isinstance(t, dict) and t.get('scenario') == scenario]
        groups = {mode: [t for t in selected if t.get('mode') == mode and t.get('valid') is True and
                         finite_nonnegative(t.get('durationSeconds')) and t['durationSeconds'] >= 60 and
                         type(t.get('frames')) is int and t['frames'] > 0]
                  for mode in ('Off', 'Inspect', 'Record')}
        if any(len(items) != 3 for items in groups.values()):
            rows.append(f'{scenario}: INCONCLUSIVE — need three valid 60-second trials per mode.')
            continue
        required = ('frameP95Ms', 'diagnosticMeanMs', 'diagnosticP95Ms', 'frameMedianMs')
        if any(any(type(t.get(k)) not in (int, float) or not math.isfinite(t[k]) or t[k] < 0 for k in required)
               for ts in groups.values() for t in ts):
            rows.append(f'{scenario}: INCONCLUSIVE — invalid numeric trial summary.')
            continue
        baseline = percentile([t['frameP95Ms'] for t in groups['Off']], .5)
        if not baseline or baseline >= 1000 / 30:
            rows.append(f'{scenario}: INCONCLUSIVE — baseline has no measured 30 FPS headroom.')
            continue
        for mode in ('Inspect', 'Record'):
            p95 = percentile([t['frameP95Ms'] for t in groups[mode]], .5)
            overhead_ok = all(t['diagnosticMeanMs'] < 1 and t['diagnosticP95Ms'] < 2 for t in groups[mode])
            delta = p95 / baseline - 1
            verdict = 'WITHIN NUMERIC TARGET' if delta <= .05 and p95 <= 1000 / 30 and overhead_ok else 'NEEDS INVESTIGATION'
            rows.append(f'{scenario}/{mode}: {verdict}; p95={p95:.3f} ms, baseline={baseline:.3f} ms, change={delta:.1%}. Hitch patterns and setup comparability still require review.')
    return rows


def render(report):
    lines = ['# World awareness evidence', '',
             'This report summarizes captured facts. It cannot reconstruct missing LOS, native decisions, damage attribution or routes.', '',
             f"Identity: {report['identities']}", f"Levels: {report['levels']}; bytes read: {report['bytes']}",
             f"Events: {report['counts']}", f"Effects with both creation and retirement-request records: {report['paired_effects']}", '',
             '## Capture integrity', '']
    lines += ['- ' + issue for issue in report['issues']] or ['No structural loss detected. This is not a gameplay or performance pass.']
    if report['census']:
        lines += ['', '## Latest census', '', json.dumps(report['census'], indent=2)]
    incident = report.get('latest_incident')
    sample = incident.get('current') if isinstance(incident, dict) and isinstance(incident.get('current'), dict) else report['latest_sample']
    if sample:
        lines += ['', '## Latest observed state', '', f"Game time: {sample.get('gameTime')}; observer: {sample.get('observer')}; target: {sample.get('target')}"]
        for entity in sample.get('entities', []) if isinstance(sample.get('entities'), list) else []:
            if not isinstance(entity, dict):
                continue
            data = entity.get('data') if isinstance(entity.get('data'), dict) else {}
            lines.append(f"- {entity.get('key')} {entity.get('name')}: ({entity.get('x')}, {entity.get('y')}), health={data.get('health', 'unknown')}/{data.get('healthMax', 'unknown')}, goal={data.get('rootGoal', 'unknown')}, captured={entity.get('gameTime')}")
        lines += ['', 'Selected perception: ' + json.dumps(sample.get('selectedPerception')), '',
                  'Navigation: ' + json.dumps(sample.get('navigation'))]
    if report['performance']:
        p = report['performance'][-1]
        lines += ['', '## Latest performance window', '',
                  f"Mode={p.get('mode')}, paused={p.get('paused')}, quality={p.get('quality')}, frames={p.get('windowFrames')}",
                  f"Frame p95={p.get('frameP95Ms')} ms; diagnostic mean/p95={p.get('diagnosticMeanMs')}/{p.get('diagnosticP95Ms')} ms.",
                  'Managed-memory deltas are not total allocation measurements. Single windows do not establish release performance.']
    lines += ['', '## Benchmark gate', ''] + assess_trials(report['trials'])
    if report['issues']:
        lines += ['', 'Capture integrity is incomplete: benchmark numbers cannot certify this run.']
    lines += ['', 'Modest-hardware and live split-screen validation require their own captured runs.']
    return '\n'.join(lines) + '\n'


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('paths', nargs='+', type=Path)
    parser.add_argument('--output', type=Path)
    args = parser.parse_args()
    if args.output and args.output.resolve() in [p.resolve() for p in args.paths]:
        parser.error('refusing to overwrite a source capture')
    try:
        report = analyze(args.paths)
        text = render(report)
        if args.output:
            args.output.write_text(text)
        else:
            print(text, end='')
        return 2 if report['issues'] else 0
    except (OSError, ValueError) as error:
        parser.exit(2, str(error) + '\n')


if __name__ == '__main__':
    raise SystemExit(main())
