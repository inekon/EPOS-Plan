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

DER LAUF IST WIEDERHOLBAR: Aendert sich kein Schluessel, laesst ein zweiter Lauf die Datei
byte-gleich liegen (Befund #152). Jeder Aufruf prueft das selbst - er setzt den Erzeuger ein
zweites Mal auf sein eigenes Ergebnis an und bricht ab, wenn dabei etwas anderes herauskommt.
Der Trockenlauf nennt dazu die Zeichenbilanz gegen die Datei auf der Platte.

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
# Rohe Bytes des Bestands (BOM, CRLF) - Massstab fuer den Byte-Vergleich unten. Der
# Textmodus oben normalisiert beim Lesen jeden Zeilenumbruch auf LF (Befund #anschliessend
# an #152: ein zweiter Lauf blieb im ZEICHEN-Vergleich "unveraendert", waehrend die Datei
# auf der Platte von CRLF auf LF kippte - der Trockenlauf sah das nicht, weil er nur mit
# ebenso normalisiertem alt verglich). Deshalb unten der BYTE-Vergleich gegen alt_disk.
alt_disk=open(pfad+'Resource.Designer.cs','rb').read()
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
def erzeugen(alt):
    """Baut den Dateiinhalt aus dem KOPF der vorhandenen Datei und den resx-Eintraegen.

    DER KOPF ENDET NACH DER CULTURE-EIGENSCHAFT MIT "        }" - und keinen Zeichen
    mehr. Die Leerzeile davor ("        \\n", acht Leerzeichen und Umbruch) gehoert
    schon zu block(): Sie ist dessen erste Zeile. Wer sie im Kopf stehen laesst,
    schreibt sie zweimal, und weil der naechste Lauf denselben Kopf wieder einliest,
    haengt JEDER Lauf neun Zeichen an (Befund #152, 07.09.2026: 1 823 894 ->
    1 823 903 -> 1 823 912 Byte, "Bloecke gleich 5207, abweichend 0" bei jedem Lauf).
    Darum rstrip() OHNE Argument: es nimmt Leerzeichen UND Umbrueche.
    """
    kopf_ende=alt.index('        /// <summary>\n        ///   Sucht eine lokalisierte Zeichenfolge')
    kopf=alt[:kopf_ende].rstrip()
    assert kopf.endswith('}'), 'Kopf unerwartet'
    # BOM erzwingen, unabhaengig davon, ob die eingelesene Datei sie noch trug: Der
    # Textmodus (encoding='utf-8', keine '-sig'-Endung) liesse eine fehlende BOM sonst
    # klaglos durchrutschen, und der Byte-Vergleich unten saehe zwei gleichermassen
    # BOM-lose Staende als "unveraendert" an (.editorconfig verlangt BOM fuer .cs).
    if not kopf.startswith('﻿'): kopf='﻿'+kopf
    return kopf+'\n'+''.join(block(n,w) for n,w in eintraege)+'    }\n}\n'
neu=erzeugen(alt)
# SELBSTPROBE DER WIEDERHOLBARKEIT (Befund #152): Ein zweiter Lauf, angesetzt auf das
# Ergebnis des ersten, muss ZEICHENGLEICH sein - sonst waechst die Datei bei jedem
# Aufruf. Die Probe kostet nichts und laeuft auch im Trockenlauf mit.
nochmal=erzeugen(neu)
assert nochmal==neu, f'nicht wiederholbar: zweiter Lauf {len(nochmal)-len(neu):+d} Zeichen'
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
# Bestand ist BOM und CRLF (.editorconfig); neu traegt intern nur LF (einfachere Regex),
# darum hier auf die Bytes bringen, die tatsaechlich geschrieben wuerden - das BOM steckt
# schon als ﻿ im KOPF (Textmodus-Lesen mit 'utf-8' schneidet es nicht ab, anders als
# 'utf-8-sig'), CRLF kommt erst hier per replace herein.
# Zeilenenden folgen der Datei auf der Platte: liegt sie mit LF (so im Repositorium), bleibt
# LF; nur eine CRLF-Datei wird mit CRLF geschrieben - sonst kippt jeder Lauf alle Zeilen.
zeilenende='\r\n' if b'\r\n' in alt_disk else '\n'
neu_disk=neu.replace('\n',zeilenende).encode('utf-8')
# Der BYTE-Vergleich sagt VOR dem Schreiben, was ein Schreiblauf auf der Platte aendern
# wuerde; "0" heisst, die Datei ist auf dem Stand (Inhalt UND Zeilenenden UND BOM) und der
# Lauf laesst sie byte-gleich liegen. Ein reiner Zeichen-Vergleich saehe das nicht, weil
# der Textmodus CRLF und LF gleich einliest (Befund s.o.).
unterschied=len(neu_disk)-len(alt_disk)
print(f'Datei {len(alt_disk)} Byte, erzeugt {len(neu_disk)} ({unterschied:+d}); '
      + ('unveraendert' if neu_disk==alt_disk else 'ABWEICHEND')
      + '; zweiter Lauf +0 (wiederholbar)')
if len(sys.argv)>1 and sys.argv[1]=='schreiben':
    open(pfad+'Resource.Designer.cs','wb').write(neu_disk); print('geschrieben', len(neu_disk), 'Byte')
