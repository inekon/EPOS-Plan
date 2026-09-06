#!/usr/bin/env python3
"""Erzeugt EPOS.Kern/MyResource/Resource.Designer.cs vollstaendig neu aus Resource.resx.

Ohne Visual Studio (Linux, macOS, CI) gibt es keinen ResXFileCodeGenerator; wer Schluessel
von Hand an die Designer-Datei anhaengt, erzeugt Luecken, falsche Reihenfolge und beim
naechsten VS-Lauf Riesen-Diffs. Dieses Werkzeug schreibt die Datei so, wie der
StronglyTypedResourceBuilder sie schreibt: Kopf bis zur Culture-Eigenschaft unveraendert,
danach eine Eigenschaft je String-Eintrag der neutralen resx, alphabetisch ohne Beachtung
der Gross-/Kleinschreibung, Doc-Kommentar mit XML-Escapes (&, <, >, "), Werte ueber
512 Zeichen abgeschnitten, BOM und LF wie bisher. Nicht-String-Eintraege (Color1, Bitmap1,
Icon1) tragen im Kern keine Eigenschaft.

Aufruf (Repowurzel):
    python3 Werkzeuge/ResourceDesigner/designer_neu.py            # nur pruefen (Trockenlauf)
    python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben  # Datei neu schreiben
Danach EPOS.Kern bauen; die Satellitendatei Resource.en-US.resx braucht keinen Designer.
"""
import re, html, sys, os
wurzel=os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
pfad=os.path.join(wurzel,'EPOS.Kern','MyResource')+os.sep
resx=open(pfad+'Resource.resx',encoding='utf-8').read()
alt=open(pfad+'Resource.Designer.cs',encoding='utf-8').read()
eintraege=[]
for m in re.finditer(r'<data name="([^"]+)"([^>]*)>(.*?)</data>',resx,re.S):
    name,attr,body=m.group(1),m.group(2),m.group(3)
    if 'type=' in attr and 'System.String' not in attr: continue
    vm=re.search(r'<value>(.*?)</value>',body,re.S)
    wert=html.unescape(vm.group(1)) if vm else ''
    eintraege.append((name,wert))
namen=[n for n,_ in eintraege]
assert len(namen)==len(set(namen)), 'doppelte Schluessel'
for n in namen: assert re.fullmatch(r'[A-Za-z_][A-Za-z0-9_]*',n), n
eintraege.sort(key=lambda e:(e[0].upper(),e[0]))
def kommentar(w):
    if len(w)>512: w=w[:512]+' [rest der Zeichenfolge wurde abgeschnitten]&quot;'
    w=w.replace('\r\n','\n').replace('&','&amp;').replace('<','&lt;').replace('>','&gt;').replace('"','&quot;')
    zeilen=w.split('\n')
    erste='        ///   Sucht eine lokalisierte Zeichenfolge, die '+zeilen[0]
    rest=['        ///'+z for z in zeilen[1:]]
    alle=[erste]+rest; alle[-1]+=' ähnelt.'
    return '\n'.join(alle)
def block(n,w):
    return ('        \n        /// <summary>\n'+kommentar(w)+'\n        /// </summary>\n'
            f'        public static string {n} {{\n            get {{\n'
            f'                return ResourceManager.GetString("{n}", resourceCulture);\n'
            '            }\n        }\n')
kopf_ende=alt.index('        /// <summary>\n        ///   Sucht eine lokalisierte Zeichenfolge')
kopf=alt[:kopf_ende].rstrip(' ')  # endet nach der Culture-Eigenschaft mit "        }\n"
assert kopf.rstrip().endswith('}'), 'Kopf unerwartet'
neu=kopf.rstrip('\n')+'\n'+''.join(block(n,w) for n,w in eintraege)+'    }\n}\n'
# Vergleich mit den vorhandenen Bloecken
alte={}
for m in re.finditer(r'(        /// <summary>\n        ///   Sucht eine lokalisierte Zeichenfolge.*?\n        /// </summary>\n        public static string (\w+) \{\n.*?\n        \}\n)',alt,re.S):
    alte[m.group(2)]=m.group(1)
gleich=abw=0; beispiele=[]
for n,w in eintraege:
    if n in alte:
        b=block(n,w)[9:]  # ohne die Leerzeile davor
        if b==alte[n]: gleich+=1
        else:
            abw+=1
            if len(beispiele)<4: beispiele.append((n,alte[n][:220],b[:220]))
print(f'Eintraege: {len(eintraege)} (vorher {len(alte)}); Bloecke gleich {gleich}, abweichend {abw}, neu {len(eintraege)-len(alte)}')
for n,a,b in beispiele: print('---',n); print('ALT:',repr(a)); print('NEU:',repr(b))
if len(sys.argv)>1 and sys.argv[1]=='schreiben':
    open(pfad+'Resource.Designer.cs','w',encoding='utf-8',newline='\n').write(neu); print('geschrieben', len(neu), 'Zeichen')
