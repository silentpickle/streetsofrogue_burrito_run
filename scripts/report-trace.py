#!/usr/bin/env python3
"""Summarize schema-1 native bark traces; missing evidence never becomes a pass."""
import argparse
import json
import math
import struct
from pathlib import Path


def number(value):
    return isinstance(value, (int, float)) and not isinstance(value, bool) and math.isfinite(value)


def point(value):
    return isinstance(value, list) and len(value) == 3 and all(number(v) for v in value)


def distance(first, second):
    # Unity positions are float32; restore that precision from round-trip JSON numbers.
    dx = single(single(first[0]) - single(second[0]))
    dy = single(single(first[1]) - single(second[1]))
    return single(math.sqrt(single(single(dx * dx) + single(dy * dy))))


def single(value):
    return struct.unpack('f', struct.pack('f', value))[0]


def reject_constant(value):
    raise ValueError(f"non-finite JSON value {value}")


def unique_object(pairs):
    result = {}
    for key, value in pairs:
        if key in result:
            raise ValueError(f"duplicate JSON key {key}")
        result[key] = value
    return result


def load(path):
    records, warnings = [], []
    previous = 0
    if path.stat().st_size > 9 * 1024 * 1024:
        return [], ["Trace exceeds the reader's 9 MiB bound."]
    with path.open(encoding="utf-8") as stream:
        for line_no, line in enumerate(stream, 1):
            if not line.strip():
                continue
            try:
                value = json.loads(line, parse_constant=reject_constant, object_pairs_hook=unique_object)
                if not isinstance(value, dict) or value.get("schema") != 1:
                    raise ValueError("unsupported schema or non-object record")
                if not isinstance(value.get("data"), dict):
                    raise ValueError("missing data object")
                if not isinstance(value.get("attempt"), int) or isinstance(value["attempt"], bool):
                    raise ValueError("invalid attempt ID")
                if not isinstance(value.get("seq"), int) or isinstance(value["seq"], bool):
                    raise ValueError("invalid sequence")
                if value["seq"] != previous + 1:
                    warnings.append(f"Line {line_no}: sequence gap/reorder ({previous} -> {value['seq']}).")
                previous = value["seq"]
                if not number(value.get("gameTime")) or not number(value.get("wallSeconds")):
                    raise ValueError("missing numeric clocks")
                if not isinstance(value.get("session"), str) or not isinstance(value.get("build"), str):
                    raise ValueError("missing session/build identity")
                records.append(value)
            except (ValueError, TypeError) as error:
                warnings.append(f"Line {line_no}: {error}.")
    if not records:
        warnings.append("No readable schema-1 records; no gameplay result can be established.")
    return records, warnings


def analyze(records, file_warnings):
    warnings = list(file_warnings)
    builds = {(row['session'], row['build']) for row in records}
    if len(builds) > 1:
        warnings.append("Mixed session/build identities in a single trace.")
    if not any(row.get('event') == 'session' for row in records):
        warnings.append("Session header missing; coverage settings unavailable.")
    if any(row.get('event') == 'capture-error' and row['attempt'] == 0 for row in records):
        warnings.append("Global capture error recorded; coverage is incomplete.")
    groups = {}
    for row in records:
        if row['attempt'] > 0:
            groups.setdefault((row['session'], row['attempt']), []).append(row)
    results = []
    for (session, attempt), rows in groups.items():
        issues = list(warnings)
        requests = [r for r in rows if r.get('event') == 'requested']
        emissions = [r for r in rows if r.get('event') == 'emitted']
        terminals = [r for r in rows if r.get('event') == 'terminal']
        if len(requests) != 1:
            issues.append("Missing or duplicated request.")
        if len(emissions) > 1 or len(terminals) > 1:
            issues.append("Conflicting emission/terminal records.")
        if not terminals:
            issues.append("No terminal record: exit, truncation or unfinished observation.")
        if any(r.get('event') == 'capture-error' for r in rows):
            issues.append("Attempt capture error recorded.")
        for row in rows:
            if emissions and row.get('event') == 'environment' and row['data'].get('world', {}).get('available') is not True:
                issues.append("Environment unavailable or capture failed.")
                break
        emission = emissions[0] if emissions else None
        terminal = terminals[0] if terminals else None
        data = emission['data'] if emission else {}
        outcome = terminal['data'] if terminal else {}
        if terminal and (outcome.get('droppedRecords', 0) or outcome.get('writerFailed') or outcome.get('fileLimited')):
            issues.append("Writer reports dropped records, file cap or write failure.")
        target = data.get('target') or {}
        source = data.get('source') or {}
        observed_reaction = any(
            r.get('event') == 'reaction' and r['data'].get('matchingNativeActivation') is True
            and r['data'].get('noiseId') == data.get('noiseId')
            and (r['data'].get('actor') or {}).get('uid') == target.get('uid')
            and (r['data'].get('actor') or {}).get('instance') == target.get('instance')
            for r in rows) if emission else False
        sampled = [r for r in rows if r.get('event') == 'sample']
        max_displacement, max_approach, matching = 0.0, 0.0, 0
        anchor = None
        last_matching_time = None
        for row in sampled:
            frame = row['data']
            actors = frame.get('actors') or []
            actor = next((a for a in actors if isinstance(a, dict)
                          and a.get('uid') == target.get('uid')
                          and a.get('instance') == target.get('instance')), None)
            if frame.get('targetRootMatchesNoise') is not True:
                anchor = None
                continue
            if (not actor or actor.get('rootGoal') != 'GoalNoiseReact'
                    or actor.get('rootNoiseId') != data.get('noiseId')
                    or actor.get('rootNoiseInstance') != data.get('noiseInstance')
                    or frame.get('noiseIdObserved') != data.get('noiseId')
                    or frame.get('noiseInstanceObserved') != data.get('noiseInstance')
                    or not all(point(p) for p in (actor.get('position'), target.get('position'), source.get('position')))):
                issues.append("A matching sample has incomplete or inconsistent actor/noise/position evidence.")
                anchor = None
                continue
            # Recompute from raw positions, independently of the producer's summary flags.
            prior_reaction = any(r.get('event') == 'reaction' and r['seq'] < row['seq']
                                 and (r['data'].get('actor') or {}).get('uid') == target.get('uid')
                                 and (r['data'].get('actor') or {}).get('instance') == target.get('instance')
                                 and r['data'].get('noiseId') == data.get('noiseId')
                                 and r['data'].get('matchingNativeActivation') is True for r in rows)
            if not prior_reaction:
                anchor = None
                continue
            matching += 1
            if anchor is None or row['gameTime'] - last_matching_time > 0.6 or row['gameTime'] < last_matching_time:
                anchor = actor['position']
            last_matching_time = row['gameTime']
            max_displacement = max(max_displacement, distance(actor['position'], anchor))
            max_approach = max(max_approach, single(distance(anchor, source['position'])
                               - distance(actor['position'], source['position'])))
        measured = observed_reaction and max_displacement >= single(0.64) and max_approach >= single(0.64)
        if emission and terminal:
            if outcome.get('samples') != len(sampled):
                issues.append("Sample count differs from terminal summary.")
            if terminal['levelId'] != emission['levelId']:
                issues.append("Level changed during observation.")
            if outcome.get('approached') is not measured:
                issues.append("Terminal movement flag disagrees with independently recomputed evidence.")
            for field, actual in (('maxMatchingDisplacement', max_displacement), ('maxMatchingApproach', max_approach)):
                if not number(outcome.get(field)) or not math.isclose(outcome[field], actual, abs_tol=0.00001):
                    issues.append(f"Terminal {field} disagrees with coordinate evidence.")
            if (outcome.get('reason') == 'window-complete'
                    and terminal['gameTime'] - emission['gameTime'] < data.get('observationSeconds', 8)):
                issues.append("Observation ended before its declared window.")
        status = outcome.get('status', 'INCONCLUSIVE')
        if status not in ('PASS', 'FAIL', 'SKIPPED', 'INTERRUPTED', 'INCONCLUSIVE'):
            issues.append("Unknown terminal status.")
        if status == 'PASS' and not (emission and measured and outcome.get('reason') == 'window-complete'):
            issues.append("PASS lacks the required matching native movement evidence.")
        if status == 'SKIPPED' and emission:
            issues.append("SKIPPED conflicts with an emission.")
        if issues:
            status = 'INCONCLUSIVE'
        results.append(dict(session=session, attempt=attempt, status=status,
                            trigger=requests[0]['data'].get('trigger') if requests else 'unknown',
                            request=requests[0]['data'] if requests else {}, emission=data, outcome=outcome,
                            targetReaction=observed_reaction, matchingSamples=matching,
                            maxDisplacement=max_displacement, maxApproach=max_approach,
                            samples=len(sampled), issues=list(dict.fromkeys(issues)), rows=rows))
    return results, warnings


def fmt(value):
    if number(value):
        return f'{value:.3f}'
    return str(value) if value is not None else 'unknown'


def render(path, results, warnings):
    lines = [f'# Bark trace report — {path.name}', '',
             'PASS means observed approach across consecutive matching native goal samples. '
             'Sampling cannot prove continuous goal execution or isolate all competing stimuli. '
             'Useful distraction, audible output and fun remain unverified.', '']
    if warnings:
        lines += ['Capture warnings:'] + [f'- {w}' for w in warnings] + ['']
    if not results:
        lines += ['No attempted bark recorded. This establishes no gameplay outcome.', '']
    for result in results:
        request, emission, outcome = result['request'], result['emission'], result['outcome']
        lines += [f"## Attempt {result['attempt']} ({result['session']}) — {result['status']}", '',
                  f"Trigger: {result['trigger']}; terminal reason: {outcome.get('reason', 'missing')}.",
                  f"Cooldown remaining: {fmt(request.get('cooldownRemaining'))} game seconds; "
                  f"barks: {fmt(request.get('count'))}/{fmt(request.get('levelCap'))}."]
        if emission:
            target = emission.get('target') or {}
            lines += [f"Source position: {(emission.get('source') or {}).get('position')}; "
                      f"target: {target.get('uid')} ({target.get('name')}) at {target.get('position')}.",
                      f"Initial distance: {fmt(emission.get('startDistance'))}; "
                      f"volume: {fmt(emission.get('volume'))}; radius: {fmt(emission.get('radius'))}; "
                      f"noise: {emission.get('noiseId')}/{emission.get('noiseInstance')}.",
                      f"Target health: {fmt(target.get('health'))}/{fmt(target.get('healthMax'))}; "
                      f"hearing range: {fmt(target.get('hearingRange'))}; "
                      f"default/root goal: {target.get('defaultGoal')}/{target.get('rootGoal')}.",
                      f"Native target reaction: {'observed' if result['targetReaction'] else 'not observed; cause unknown'}.",
                      f"Matching-goal samples: {result['matchingSamples']}/{result['samples']}; "
                      f"recomputed maximum displacement/approach: {result['maxDisplacement']:.3f}/"
                      f"{result['maxApproach']:.3f} world units.",
                      f"Maximum recorded sample gap: {fmt(outcome.get('maxSampleGapSeconds'))} game seconds; "
                      f"maximum snapshot cost: {fmt(outcome.get('maxCaptureMs'))} ms.",
                      f"Audio playback requested: {emission.get('audioPlaybackRequested')}; audible output unverified."]
        candidates = next((r['data'] for r in result['rows'] if r.get('event') == 'candidates'), None)
        if candidates:
            lines += [f"Candidate scan: {candidates.get('totalScanned')} scanned; "
                      f"{candidates.get('omitted')} omitted from detail. Rule counts: {candidates.get('reasonCounts')}."]
            for candidate in candidates.get('actors', [])[:8]:
                actor = candidate.get('actor') or {}
                lines += [f"- NPC {actor.get('uid')} {actor.get('name')}: "
                          f"distance={fmt(candidate.get('distance'))}, LOS={candidate.get('lineOfSight')}, "
                          f"default/root={actor.get('defaultGoal')}/{actor.get('rootGoal')}, "
                          f"reasons={candidate.get('reasons')}."]
        if result['issues']:
            lines += ['Evidence limits:'] + [f'- {issue}' for issue in result['issues']]
        lines += ['']
    return '\n'.join(lines)


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('trace', type=Path)
    parser.add_argument('--output', type=Path, help='Save a Markdown report; stdout by default')
    args = parser.parse_args()
    try:
        records, warnings = load(args.trace)
        results, warnings = analyze(records, warnings)
        report = render(args.trace, results, warnings)
        if args.output:
            if args.output.resolve() == args.trace.resolve():
                parser.error('report output must not overwrite the source trace')
            args.output.write_text(report, encoding='utf-8')
        else:
            print(report)
        return 2 if warnings or any(r['issues'] for r in results) else 0
    except (OSError, UnicodeError, TypeError, ValueError, KeyError, AttributeError, OverflowError) as error:
        print(f'INCONCLUSIVE: trace could not be interpreted: {error}')
        return 2


if __name__ == '__main__':
    raise SystemExit(main())
