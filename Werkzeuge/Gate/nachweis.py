#!/usr/bin/env python3
"""Trägt den Nachweis des Gates in Statuszeilen ein.
Aufruf: nachweis.py <Status_iOS_Migration.md> <gate.log> <gate-sha> <zeilen: 1039,1040,...> [--zusatz "Text"]
Ersetzt in den genannten Zeilen den Platzhalter „**Nachweis:** Gate läuft, wird nachgetragen.“ (oder
„**Nachweis:** Gate läuft, wird nachgetragen. Bisher: …“ bis zum Zeilenende der Zelle) durch den Nachweistext."""
import sys, re, io, argparse

ap = argparse.ArgumentParser()
ap.add_argument('status'); ap.add_argument('log'); ap.add_argument('sha'); ap.add_argument('zeilen')
ap.add_argument('--zusatz', default='')
a = ap.parse_args()
log = io.open(a.log, encoding='utf-8', errors='replace').read()

def zahl(s): return s.replace(',', '').strip()
tests = {}
for m in re.finditer(r'^(Passed|Failed)!\s+- Failed:\s+(\d+), Passed:\s+(\d+), Skipped:\s+(\d+), Total:\s+(\d+).*?- (\S+?)\.dll', log, re.M):
    tests[m.group(6)] = (int(m.group(3)), int(m.group(4)), int(m.group(2)))
def t(name):
    if name not in tests: return f'{name} ?'
    p, s, f = tests[name]
    txt = f'{name} {p:,}'.replace(',', ' ')
    if s: txt += f' / {s} übersprungen'
    txt += f' / {f} rot' if f else ''
    return txt
sql = re.search(r'(\d+) SQL-Texte geprueft: (\d+) Fundstellen', log)
sqltxt = f'{int(sql.group(1)):,} Texte / {sql.group(2)} Fundstellen'.replace(',', ' ') if sql else '?'
designer = 'wiederholbar' if re.search(r'^>>> 1 Designer: PASS', log, re.M) else 'PRÜFEN'
bau = 'Kern-Filter Release und Windows-Schale (Linux, EnableWindowsTargeting) je 0 Fehler' if re.search(r'^>>> 3 Bau Kern: PASS', log, re.M) and re.search(r'^>>> 4 Bau Windows: PASS', log, re.M) else 'Bau PRÜFEN'
wz = 'Werkzeugtests Formularkarte, Auslieferungsvorlage, Gebäudevergleich, ZapfprofilValidierung grün' if re.search(r'^>>> 6 Werkzeugtests: PASS', log, re.M) else 'Werkzeugtests PRÜFEN'
# Referenzlauf: PASS, wenn alle gerechneten Projekte PASS sind und FAIL nur „im Vergleichslauf nicht vorhanden“ ist
passz = re.findall(r'^Projekt_(\d+): PASS \((\d+) Dateien, (\d+) Werte\)', log, re.M)
echt_fail = [l for l in re.findall(r'^Projekt_\d+: FAIL.*$', log, re.M) if 'nicht vorhanden' not in l]
if passz and not echt_fail:
    summe = sum(int(w) for _, _, w in passz)
    reftxt = f'Referenzlauf {len(passz)} CI-Projekte gegen R52 `2026-10-11_R52_Vorheizen` alle PASS ({summe:,} Werte; FAIL nur für nicht gerechnete Projekte)'.replace(',', ' ')
else:
    reftxt = f'Referenzlauf PRÜFEN ({len(echt_fail)} echte FAIL-Zeilen)'
nachweis = (f'**Nachweis:** Gate auf `{a.sha}` — {bau}; Tests {t("EPOS.Kern.Tests")}, {t("EPOS.UI.Tests")}, {t("KiKern.Tests")}, '
            f'{t("SpeicherEngine.Tests")}, {t("SpeicherPlanung.Tests")}; {wz}; Designer {designer}; SQL-Prüfer {sqltxt}; {reftxt}.'
            + (f' {a.zusatz}' if a.zusatz else '') + ' CI-Vermerk mit dem nächsten Kern-Lauf.')
s = io.open(a.status, encoding='utf-8', newline='').read()
nl = '\r\n' if '\r\n' in s else '\n'
lines = s.split(nl); n = 0
for z in a.zeilen.split(','):
    for i, l in enumerate(lines):
        if l.startswith(f'| **#{z.strip()}** |'):
            neu = re.sub(r'\*\*Nachweis:\*\* Gate läuft, wird nachgetragen\.(?: Bisher:[^|]*)?', lambda m: nachweis + ' ', l, count=1)
            if neu == l: print(f'#{z}: Platzhalter nicht gefunden'); continue
            lines[i] = neu; n += 1; break
    else:
        print(f'#{z}: Zeile nicht gefunden')
io.open(a.status, 'w', encoding='utf-8', newline='').write(nl.join(lines))
print(f'{n} Zeilen nachgetragen:', nachweis[:160], '…')
