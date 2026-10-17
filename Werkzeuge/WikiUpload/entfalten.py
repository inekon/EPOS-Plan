"""Fuehrt hart umbrochene Absaetze, Listenpunkte und Tabellenzellen einer Wiki-Quelle auf eine Zeile zusammen.

MediaWiki liest eine eingerueckte Folgezeile als <pre>-Block und eine nicht eingerueckte Folgezeile eines
Listenpunkts als neuen Absatz. Geschuetzt bleiben Kommentare, <pre>, <math>, <syntaxhighlight>, <nowiki>,
<gallery>, <poem> und mehrzeilige Vorlagenaufrufe (dort werden nur eingerueckte Folgezeilen angehaengt).
Aufruf: entfalten.py [--schreiben] datei...
"""
import io, re, sys
LISTE = re.compile(r'^[*#:;]')
BLOCKTAG = r'(div|/div|table|/table|pre|blockquote|/blockquote|center|/center|gallery|references|syntaxhighlight|h[1-6]|ul|/ul|ol|/ol|li|dl|hr|!--|noinclude|/noinclude|includeonly|/includeonly|onlyinclude|/onlyinclude|templatedata|section|br\s*/?>\s*$)'
BLOCK = re.compile(r'^(\{\||\|\}|\|-|\|\+|==|\{\{|\}\}|\[\[(Datei|File|Kategorie|Category|Bild|Image):|----|__[A-Z]+__|<' + BLOCKTAG + ')', re.I)
SCHUTZ = re.compile(r'<!--|<(pre|math|syntaxhighlight|source|nowiki|gallery|poem|chem|ce|templatedata)\b[^>]*?(?<!/)>', re.I)
def zustand(zeile, zu):
    pos = 0
    while True:
        if zu is None:
            m = SCHUTZ.search(zeile, pos)
            if not m: return zu
            zu = '-->' if m.group(0) == '<!--' else '</%s>' % m.group(1).lower()
            pos = m.end()
        else:
            i = zeile.lower().find(zu, pos)
            if i < 0: return zu
            pos = i + len(zu); zu = None
def vorlagentiefe(zeile, tiefe):
    z = re.sub(r'<!--.*?-->', '', zeile)
    return max(0, tiefe + z.count('{{') - z.count('}}'))
def text_zeile(z):
    """Zeile, an die eine Folgezeile angehaengt werden darf."""
    if not z.strip(): return False
    if LISTE.match(z): return True
    if re.match(r'^(\||!)(?![-}+])', z): return True       # Tabellenzelle
    return not BLOCK.match(z) and not z.startswith(' ')
def entfalten(text):
    zeilen = text.split('\n'); aus = []; regeln = {'einzug': 0, 'liste': 0, 'absatz': 0, 'zelle': 0, 'vorlage_einzug': 0}
    zu = None; tiefe = 0; letzte_geschuetzt = False; offen_einzug = []
    for z in zeilen:
        geschuetzt = zu is not None
        zu_neu = zustand(z, zu)
        tiefe_vorher = tiefe
        if not geschuetzt and aus and not letzte_geschuetzt and z.strip():
            v = aus[-1]
            if z.startswith((' ', '\t')) and text_zeile(v):
                regeln['vorlage_einzug' if tiefe_vorher > 0 else 'einzug'] += 1
                aus[-1] = v.rstrip() + ' ' + z.strip(); zu = zu_neu; tiefe = vorlagentiefe(z, tiefe); letzte_geschuetzt = zu is not None; continue
            if tiefe_vorher == 0 and not z.startswith((' ', '\t')) and not LISTE.match(z) and not BLOCK.match(z) and text_zeile(v):
                art = 'liste' if LISTE.match(v) else ('zelle' if re.match(r'^(\||!)', v) else 'absatz')
                regeln[art] += 1
                aus[-1] = v.rstrip() + ' ' + z.strip(); zu = zu_neu; tiefe = vorlagentiefe(z, tiefe); letzte_geschuetzt = zu is not None; continue
        if not geschuetzt and z.startswith((' ', '\t')) and z.strip() and not (aus and text_zeile(aus[-1])):
            offen_einzug.append(z[:80])
        aus.append(z); zu = zu_neu; tiefe = vorlagentiefe(z, tiefe) if not geschuetzt else tiefe
        letzte_geschuetzt = zu is not None
    return '\n'.join(aus), regeln, offen_einzug
if __name__ == '__main__':
    schreiben = '--schreiben' in sys.argv
    for pfad in [a for a in sys.argv[1:] if not a.startswith('--')]:
        roh = io.open(pfad, 'rb').read(); bom = roh.startswith(b'\xef\xbb\xbf')
        text = roh.decode('utf-8-sig')
        neu, regeln, offen = entfalten(text)
        if neu != text:
            print('%-70s %s%s' % (pfad[-70:], ' '.join('%s=%d' % kv for kv in regeln.items() if kv[1]), '  OFFEN-EINZUG=%d' % len(offen) if offen else ''))
            if schreiben:
                io.open(pfad, 'wb').write((b'\xef\xbb\xbf' if bom else b'') + neu.encode('utf-8'))
        elif offen:
            print('%-70s unveraendert  OFFEN-EINZUG=%d' % (pfad[-70:], len(offen)))
