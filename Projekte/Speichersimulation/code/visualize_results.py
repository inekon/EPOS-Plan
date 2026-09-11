"""Generate an offline, interactive HTML viewer using only the standard library.

Run from Speichersimulation: python code/visualize_results.py --open
Charts use the computed results; changing form values never simulates in JavaScript.
No remote scripts, telemetry, API requests, or chart-library downloads are required.
"""
import argparse
import csv
from datetime import datetime, timezone
import hashlib
import json
import math
from pathlib import Path
import webbrowser

ROOT = Path(__file__).resolve().parents[1]


def default_config_path():
    """Prefer the user's saved inputs; an explicit --config still takes precedence."""
    gui = ROOT / 'config_gui.json'
    return gui if gui.exists() else ROOT / 'config_beispiel.json'


def read_result(directory, default_config_path, group, example_hash):
    summary = json.loads((directory / 'summary.json').read_text(encoding='utf-8-sig'))
    with (directory / 'timeseries.csv').open(encoding='utf-8-sig', newline='') as f:
        reader = csv.DictReader(f)
        columns = reader.fieldnames
        required = {'timestamp', 'load_kw', 'pv_kw', 'grid_kw', 'buy_eur_kwh', 'sell_eur_kwh'}
        if not columns or not required.issubset(columns):
            raise ValueError(f'{directory}: required columns missing')
        columns = ['timestamp'] + [c for c in columns if c != 'timestamp']
        rows = []
        previous = None
        for record in reader:
            t = datetime.fromisoformat(record['timestamp'].replace('Z', '+00:00'))
            if t.tzinfo is None or (previous and (t - previous).total_seconds() != 900):
                raise ValueError(f'{directory}: invalid interval timestamps')
            values = [float(record[c]) for c in columns[1:]]
            if not all(math.isfinite(v) for v in values):
                raise ValueError(f'{directory}: non-finite result')
            rows.append([t.timestamp() * 1000] + values)
            previous = t
    if not rows:
        raise ValueError(f'{directory}: empty result')
    # Never derive a percentage SoC or a reference grid limit from unrelated parameters.
    config = None
    config_status = 'missing'
    expected = summary.get('config_sha256')
    for candidate in [directory / 'config_snapshot.json', default_config_path, ROOT / 'config_beispiel.json']:
        if candidate.exists() and expected and hashlib.sha256(candidate.read_bytes()).hexdigest() == expected:
            config = json.loads(candidate.read_text(encoding='utf-8-sig'))
            config_status = 'hash_verified'
            break
    logs_path = directory / 'solver_log.json'
    return {'name': directory.name, 'group': group, 'source': str(directory),
            'summary': summary, 'columns': columns, 'rows': rows, 'config': config,
            'config_status': config_status,
            'synthetic': example_hash is not None and summary.get('input_sha256') == example_hash,
            'solver_log': json.loads(logs_path.read_text(encoding='utf-8')) if logs_path.exists() else []}


def build(config_path, result_paths, output):
    config = json.loads(config_path.read_text(encoding='utf-8-sig'))
    example_file = ROOT / 'beispieldaten' / 'synthetisch_3_tage.csv'
    example_hash = hashlib.sha256(example_file.read_bytes()).hexdigest() if example_file.exists() else None
    results = []
    warnings = []
    seen = set()
    for source, group in result_paths:
        folders = [source] if (source / 'summary.json').exists() else sorted(
            source.glob('*/summary.json'), key=lambda p: p.stat().st_mtime, reverse=True)
        for entry in folders:
            directory = entry.parent if entry.is_file() else entry
            if directory.resolve() in seen:
                continue
            seen.add(directory.resolve())
            try:
                results.append(read_result(directory, config_path, group, example_hash))
            except (ValueError, KeyError, OSError) as err:
                warnings.append(str(err))
    if not results:
        raise ValueError('Keine gültigen Ergebnisse gefunden. Zuerst code/run.py ausführen.')
    data = {'generated_at': datetime.now(timezone.utc).isoformat(), 'root': str(ROOT),
            'config_path': str(config_path.resolve()), 'config': config, 'results': results,
            'warnings': warnings}
    # Protect the enclosing script element when paths or imported labels contain HTML.
    payload = json.dumps(data, ensure_ascii=False, separators=(',', ':'), allow_nan=False)
    payload = payload.replace('&', '\\u0026').replace('<', '\\u003c').replace('>', '\\u003e')
    template = (ROOT / 'code' / 'visualization_template.html').read_text(encoding='utf-8')
    if template.count('__EMS_DATA__') != 1:
        raise ValueError('Invalid visualization template')
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(template.replace('__EMS_DATA__', payload), encoding='utf-8')
    return {'output': str(output.resolve()), 'runs': len(results), 'warnings': warnings}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--config', type=Path, default=None)
    parser.add_argument('--results', type=Path, action='append', help='Result folder or parent; repeatable')
    parser.add_argument('--out', type=Path, default=ROOT / 'Auswertung.html')
    parser.add_argument('--open', action='store_true')
    args = parser.parse_args()
    args.config = args.config or default_config_path()
    sources = ([(p, 'Ausgewählte Läufe') for p in args.results] if args.results else
               [(ROOT / 'ergebnisse', 'Eigene Läufe'), (ROOT / 'beispielergebnisse', 'Mitgelieferte Beispiele')])
    print(json.dumps(build(args.config, sources, args.out), ensure_ascii=False, indent=2))
    if args.open:
        webbrowser.open(args.out.resolve().as_uri())
