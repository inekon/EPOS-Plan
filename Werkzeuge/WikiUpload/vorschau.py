#!/usr/bin/env python3
"""Vorschau einer Wiki-Quelle mit den Diagrammvorlagen, ohne etwas zu speichern.

Loest die Vorlagen aus Projekte/Wiki/Vorlage - *.wiki lokal auf (Parameter,
#if, #ifeq, #switch; fremde Vorlagen wie Anker oder Hinweis bleiben stehen),
laesst das Ergebnis vom Wiki per action=parse saeubern und schreibt je eine
HTML-Seite hell und dunkel mit dem Stilblatt des Wikis. Mit --bild entstehen
zusaetzlich Aufnahmen (Chromium, Pfad aus EPOS_CHROME oder dem Playwright-Ordner).

Das Werkzeug liest nur: action=parse und action=raw aendern im Wiki nichts.

Aufruf:
  python3 Werkzeuge/WikiUpload/vorschau.py <quelle.wiki> <zielordner> [--bild] [--breite 900]
"""
import glob, json, os, re, subprocess, sys, urllib.parse

WURZEL = os.path.abspath(os.path.join(os.path.dirname(os.path.abspath(__file__)), '..', '..'))
VORLAGEN = os.path.join(WURZEL, 'Projekte', 'Wiki')
API = 'https://wiki.epos-plan.de/api.php'
INDEX = 'https://wiki.epos-plan.de/index.php'

_vorlagen = {}
PARAM = re.compile(r'\{\{\{([^{}|]+?)(?:\|([^{}]*?))?\}\}\}')


def vorlage(name):
    name = name.strip()
    if name not in _vorlagen:
        pfad = os.path.join(VORLAGEN, 'Vorlage - ' + name + '.wiki')
        rumpf = None
        if os.path.exists(pfad):
            text = open(pfad, encoding='utf-8').read()
            m = re.search(r'<includeonly>(.*?)</includeonly>', text, re.S)
            rumpf = m.group(1) if m else ''
        _vorlagen[name] = rumpf
    return _vorlagen[name]


def params_einsetzen(text, params):
    def ersetzen(m):
        name = m.group(1).strip()
        if name in params:
            return params[name]
        return m.group(2) if m.group(2) is not None else m.group(0)
    alt = None
    while alt != text:
        alt, text = text, PARAM.sub(ersetzen, text)
    return text


def schliessend(s, i):
    tiefe, j = 0, i
    while j < len(s):
        if s.startswith('{{', j):
            tiefe += 1; j += 2; continue
        if s.startswith('}}', j):
            tiefe -= 1; j += 2
            if tiefe == 0:
                return j
            continue
        j += 1
    raise ValueError('offene Klammer: %r' % s[i:i + 60])


def teilen(s):
    teile, tiefe, links, start, j = [], 0, 0, 0, 0
    while j < len(s):
        if s.startswith('{{', j): tiefe += 1; j += 2; continue
        if s.startswith('}}', j): tiefe -= 1; j += 2; continue
        if s.startswith('[[', j): links += 1; j += 2; continue
        if s.startswith(']]', j): links -= 1; j += 2; continue
        if s[j] == '|' and tiefe == 0 and links == 0:
            teile.append(s[start:j]); start = j + 1
        j += 1
    teile.append(s[start:])
    return teile


def gleichheit(s):
    tiefe, j = 0, 0
    while j < len(s):
        if s.startswith('{{', j): tiefe += 1; j += 2; continue
        if s.startswith('}}', j): tiefe -= 1; j += 2; continue
        if s[j] == '=' and tiefe == 0:
            return j
        j += 1
    return -1


def aufloesen(text, tiefe=0):
    if tiefe > 40:
        raise RecursionError('Vorlagen zu tief verschachtelt')
    aus, i = [], 0
    while True:
        k = text.find('{{', i)
        if k < 0:
            aus.append(text[i:]); break
        aus.append(text[i:k])
        e = schliessend(text, k)
        aus.append(auswerten(text[k + 2:e - 2], text[k:e], tiefe))
        i = e
    return ''.join(aus)


def auswerten(innen, roh, tiefe):
    teile = teilen(innen)
    kopf = teile[0]
    if kopf.startswith('#if:'):
        wahr = aufloesen(kopf[4:], tiefe + 1).strip()
        zweig = teile[1] if wahr and len(teile) > 1 else (teile[2] if not wahr and len(teile) > 2 else '')
        return aufloesen(zweig, tiefe + 1).strip()
    if kopf.startswith('#ifeq:'):
        a = aufloesen(kopf[6:], tiefe + 1).strip()
        b = aufloesen(teile[1], tiefe + 1).strip() if len(teile) > 1 else ''
        zweig = teile[2] if a == b and len(teile) > 2 else (teile[3] if a != b and len(teile) > 3 else '')
        return aufloesen(zweig, tiefe + 1).strip()
    if kopf.startswith('#switch:'):
        wert = aufloesen(kopf[8:], tiefe + 1).strip()
        vorgabe, durchfall = '', False
        for t in teile[1:]:
            g = gleichheit(t)
            if g < 0:
                durchfall = durchfall or aufloesen(t, tiefe + 1).strip() == wert
                continue
            k, v = t[:g].strip(), t[g + 1:]
            if k == '#default':
                vorgabe = v
            elif durchfall or aufloesen(k, tiefe + 1).strip() == wert:
                return aufloesen(v, tiefe + 1).strip()
        return aufloesen(vorgabe, tiefe + 1).strip()
    rumpf = vorlage(aufloesen(kopf, tiefe + 1))
    if rumpf is None:
        return roh
    params, pos = {}, 1
    for t in teile[1:]:
        g = gleichheit(t)
        if g >= 0:
            params[t[:g].strip()] = aufloesen(t[g + 1:], tiefe + 1).strip()
        else:
            params[str(pos)] = aufloesen(t, tiefe + 1); pos += 1
    return aufloesen(params_einsetzen(rumpf, params), tiefe + 1)


def curl(argumente, eingabe=None):
    r = subprocess.run(['curl', '-sS', '--max-time', '60'] + argumente, input=eingabe, capture_output=True)
    if r.returncode != 0:
        raise RuntimeError(r.stderr.decode('utf-8', 'replace'))
    return r.stdout


def wiki_parse(text):
    daten = urllib.parse.urlencode({'action': 'parse', 'format': 'json', 'formatversion': '2',
                                    'prop': 'text', 'disablelimitreport': '1', 'contentmodel': 'wikitext',
                                    'title': 'Vorschau', 'text': text}).encode('utf-8')
    antwort = json.loads(curl(['-X', 'POST', API, '--data-binary', '@-',
                               '-H', 'Content-Type: application/x-www-form-urlencoded'], daten))
    return antwort['parse']['text']


def chrome():
    kandidaten = [os.environ.get('EPOS_CHROME', '')] + sorted(glob.glob('/opt/pw-browsers/chromium-*/chrome-linux/chrome'))
    return next((k for k in kandidaten if k and os.path.exists(k)), '')


def main():
    if len(sys.argv) < 3:
        print(__doc__); sys.exit(2)
    quelle, ziel = sys.argv[1], os.path.abspath(sys.argv[2])
    bild = '--bild' in sys.argv
    breite = int(sys.argv[sys.argv.index('--breite') + 1]) if '--breite' in sys.argv else 900
    os.makedirs(ziel, exist_ok=True)
    name = os.path.splitext(os.path.basename(quelle))[0]
    ausgedehnt = aufloesen(open(quelle, encoding='utf-8').read())
    open(os.path.join(ziel, name + '.aufgeloest.wiki'), 'w', encoding='utf-8').write(ausgedehnt)
    html = wiki_parse(ausgedehnt)
    css = curl([INDEX + '?title=MediaWiki:Common.css&action=raw']).decode('utf-8')
    browser = chrome() if bild else ''
    if bild and not browser:
        print('Kein Chromium gefunden (EPOS_CHROME setzen) - nur HTML.')
    for dunkel in (False, True):
        klasse = 'skin-theme-clientpref-night' if dunkel else 'skin-theme-clientpref-day'
        pfad = os.path.join(ziel, name + ('_dunkel' if dunkel else '_hell') + '.html')
        open(pfad, 'w', encoding='utf-8').write(
            '<!doctype html><html class="%s"><head><meta charset="utf-8"><style>%s\n'
            'body{margin:0;padding:16px;background:var(--epos-flaeche);color:var(--epos-text);'
            'font-family:system-ui,sans-serif;font-size:15px}</style></head>'
            '<body><div class="mw-parser-output">%s</div></body></html>' % (klasse, css, html))
        print(pfad)
        if browser:
            png = pfad[:-5] + '.png'
            subprocess.run([browser, '--headless=new', '--no-sandbox', '--disable-gpu', '--hide-scrollbars',
                            '--screenshot=' + png, '--window-size=%d,2400' % breite, 'file://' + pfad],
                           capture_output=True)
            print(png)


if __name__ == '__main__':
    main()
