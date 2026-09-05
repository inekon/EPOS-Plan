#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Erzeugt EPOS.Kern/Allgemein/Simulation/KlimazonenPfade.cs aus der Kartengrafik
Zonenkarte_Klimazonen.svg (iU9-W10a.0e).

WOZU DAS WERKZEUG.
Das abgeloeste WinForms-Steuerelement KlimazonenKarte zerlegte die eingebettete
SVG bei jedem Programmstart per REGEX zur Laufzeit: viewBox, die Gruppe
"zonenflaechen" mit 15 M/L/Z-Pfaden, die Gruppe "zonennummern" mit den
Beschriftungen, und ordnete beide ueber einen Punkt-in-Flaeche-Test einander zu
(KlimazonenKarte.cs:77-197). Das ist zerbrechlich (Gruppennamen, die harte
Fuellfarbe #15181C im Muster - Befund W10-B5) und in einer Razor-Komponente
ohnehin nicht wiederholbar: Dort gibt es weder System.Drawing noch einen
GraphicsPath fuer den Hit-Test.

Die Zuordnung ist eine Eigenschaft der KARTE, nicht der Sitzung. Sie wird
deshalb EINMAL erzeugt und als Quelltext eingecheckt. Die SVG bleibt daneben
liegen; wer die Karte ueberarbeitet, laesst das Skript neu laufen und checkt die
neue KlimazonenPfade.cs mit ein.

AUFRUF
    python3 Werkzeuge/KlimazonenPfade/erzeugen.py
    python3 Werkzeuge/KlimazonenPfade/erzeugen.py --pruefen   (nur vergleichen)

DIE REGELN sind die des Vorlaeufers, Zeile fuer Zeile:
  * viewBox="0 0 <breite> <hoehe>"
  * Gruppe <g id="zonenflaechen"> ... </g>, darin je Zone ein d="..."
  * Gruppe <g id="zonennummern"> ... </g>, darin
    <text x=".." y=".." ... fill="#15181C" ...>n</text>
  * Eine Nummer gehoert zu der ERSTEN Flaeche, in der ihr Punkt liegt; die
    Flaeche traegt fill-rule="evenodd", also zaehlt die Kreuzungsparitaet
    (FillMode.Alternate im Vorlaeufer).
  * Eine Zone ohne Flaeche laesst das Ergebnis scheitern - lieber gar keine
    Auswahl als eine falsche (dieselbe Entscheidung wie im Vorlaeufer).

Der erzeugte Pfad-Text ist die d-Angabe UNVERAENDERT: Im Browser zeichnet ein
<path d="..."> dieselbe Flaeche, die GDI+ aus denselben Zeichen gebaut hat.
"""

import os
import re
import sys

ZONEN = 15
HIER = os.path.dirname(os.path.abspath(__file__))
WURZEL = os.path.abspath(os.path.join(HIER, "..", ".."))
SVG = os.path.join(HIER, "Zonenkarte_Klimazonen.svg")
ZIEL = os.path.join(WURZEL, "EPOS.Kern", "Allgemein", "Simulation", "KlimazonenPfade.cs")


# Ein Befehlsbuchstabe ODER eine Zahl. Der Trenner ist NICHT nur das Leerzeichen:
# Die Kartengrafik schreibt "M315.30 141.13 L315.30 142.68" - der Buchstabe klebt am
# ersten Wert. Genau daran scheiterte der Vorlaeufer (Befund W10a-B41, siehe Kopf von
# KlimazonenPfade.cs): Er zerlegte an Leerzeichen und Kommas, bekam das Token
# "M315.30" und lief in float.Parse - FormatException, gefangen, Karte stumm.
TOKEN = re.compile(r"[MLZmlz]|[-+]?[0-9]*\.?[0-9]+(?:[eE][-+]?[0-9]+)?")


def teilflaechen(d):
    """Zerlegt einen M/L/Z-Pfad in Punktlisten - die Regel des Vorlaeufers,
    aber mit einem Zerleger, der den Buchstaben vom ersten Wert trennt."""
    flaechen = []
    punkte = []
    token = TOKEN.findall(d)
    i = 0
    while i < len(token):
        t = token[i]
        if t in ("M", "L"):
            if t == "M" and len(punkte) > 2:
                flaechen.append(punkte)
                punkte = []
            if i + 2 >= len(token):
                break
            punkte.append((float(token[i + 1]), float(token[i + 2])))
            i += 3
        elif t in ("Z", "z"):
            if len(punkte) > 2:
                flaechen.append(punkte)
                punkte = []
            i += 1
        else:
            # Nackte Koordinatenpaare nach L (implizite Fortsetzung).
            if i + 1 >= len(token):
                break
            punkte.append((float(token[i]), float(token[i + 1])))
            i += 2
    if len(punkte) > 2:
        flaechen.append(punkte)
    return flaechen


def trifft(flaechen, x, y):
    """
    Punkt-in-Flaeche mit KREUZUNGSPARITAET ueber alle Teilflaechen zusammen -
    das ist fill-rule="evenodd" bzw. FillMode.Alternate.
    """
    drin = False
    for punkte in flaechen:
        n = len(punkte)
        for k in range(n):
            x1, y1 = punkte[k]
            x2, y2 = punkte[(k + 1) % n]
            if (y1 > y) != (y2 > y):
                schnitt = x1 + (y - y1) * (x2 - x1) / (y2 - y1)
                if schnitt > x:
                    drin = not drin
    return drin


def lesen():
    with open(SVG, "r", encoding="utf-8") as f:
        svg = f.read()

    box = re.search(r'viewBox="0 0 ([0-9.]+) ([0-9.]+)"', svg)
    if not box:
        raise SystemExit("FEHLER: keine viewBox gefunden.")
    breite, hoehe = box.group(1), box.group(2)

    gruppe = re.search(r'<g id="zonenflaechen".*?</g>', svg, re.S)
    if not gruppe:
        raise SystemExit('FEHLER: Gruppe "zonenflaechen" fehlt.')

    pfade = re.findall(r'\sd="([^"]+)"', gruppe.group(0))
    if len(pfade) != ZONEN:
        raise SystemExit("FEHLER: %d Pfade statt %d." % (len(pfade), ZONEN))

    zerlegt = [teilflaechen(d) for d in pfade]

    gruppe_n = re.search(r'<g id="zonennummern".*?</g>', svg, re.S)
    if not gruppe_n:
        raise SystemExit('FEHLER: Gruppe "zonennummern" fehlt.')

    zuordnung = [None] * ZONEN
    for m in re.finditer(
            r'<text x="([0-9.]+)" y="([0-9.]+)"[^>]*fill="#15181C"[^>]*>([0-9]+)</text>',
            gruppe_n.group(0)):
        zone = int(m.group(3))
        if zone < 1 or zone > ZONEN:
            continue
        x, y = float(m.group(1)), float(m.group(2))
        for idx, flaechen in enumerate(zerlegt):
            if trifft(flaechen, x, y):
                if zuordnung[zone - 1] is None:
                    zuordnung[zone - 1] = pfade[idx]
                break

    fehlend = [z + 1 for z in range(ZONEN) if zuordnung[z] is None]
    if fehlend:
        raise SystemExit("FEHLER: keine Flaeche fuer Zone(n) %s." %
                         ", ".join(str(z) for z in fehlend))

    return breite, hoehe, zuordnung


KOPF = '''using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die 15 Zonenflaechen der Klimazonenkarte nach DIN 4710 als SVG-Pfade
    /// (Paket iU9-W10a.0e) — ERZEUGT, nicht von Hand geschrieben.
    ///
    /// <para><b>Quelle und Werkzeug.</b> <c>Werkzeuge/KlimazonenPfade/erzeugen.py</c>
    /// liest <c>Zonenkarte_Klimazonen.svg</c> (die Kartengrafik des Anwenders), nimmt
    /// aus der Gruppe <c>zonenflaechen</c> die 15 M/L/Z-Pfade und aus der Gruppe
    /// <c>zonennummern</c> die Beschriftungen und ordnet beide ueber einen
    /// Punkt-in-Flaeche-Test einander zu. Wer die Karte ueberarbeitet, laesst das
    /// Skript neu laufen und checkt diese Datei mit ein.</para>
    ///
    /// <para><b>Warum zur Bauzeit (Befund W10-B3/B5).</b> Das abgeloeste
    /// WinForms-Steuerelement <c>KlimazonenKarte</c> tat dasselbe bei JEDEM
    /// Programmstart per Regex — mit der Fuellfarbe <c>#15181C</c> hart im Muster.
    /// Die Zuordnung ist eine Eigenschaft der Karte und nicht der Sitzung; und eine
    /// Razor-Komponente koennte sie ohnehin nicht wiederholen, weil ihr fuer den
    /// Punkt-in-Flaeche-Test <c>System.Drawing</c> fehlt.</para>
    ///
    /// <para><b>BEFUND W10a-B41 — der Vorlaeufer konnte diese Karte gar nicht lesen.</b>
    /// <c>KlimazonenKarte.PfadParsen</c> zerlegte die <c>d</c>-Angabe an Leerzeichen und
    /// Kommas und erwartete den Befehlsbuchstaben als EIGENES Token. Die Kartengrafik
    /// schreibt ihn aber am ersten Wert fest: <c>"M315.30 141.13 L315.30 142.68 …"</c>.
    /// Das erste Token lautet damit <c>"M315.30"</c>, <c>float.Parse</c> wirft, und
    /// <c>Daten()</c> faengt die Ausnahme mit <c>catch { _daten = null; }</c> ab — die
    /// Karte zeigte seit jeher nur ihre Ladefehlerzeile, die Auswahl lief ueber die
    /// Liste des Erdreich-Dialogs. Das Werkzeug trennt den Buchstaben vom Wert und
    /// liest alle 15 Zonen; die Blazor-Fassung stellt die Karte damit erstmals
    /// wirklich her.</para>
    ///
    /// <para><b>Das Anzeigebild</b> liegt daneben als statische Datei der
    /// Oberflaechenbibliothek: <c>_content/EPOS.UI/bilder/Zonenkarte_Klimazonen.png</c>
    /// (3390 x 3510). Es ist eine 2,6-fach aufgeloeste Wiedergabe GENAU dieser
    /// viewBox — Bild und Pfade teilen sich damit einen Koordinatenraum, und das
    /// SVG-Overlay der <c>Bildkarte</c> liegt ohne Umrechnung darueber.</para>
    /// </summary>
    public static class KlimazonenPfade
    {
        /// <summary>Anzahl der Klimazonen — dieselbe 15 wie <c>VDI4640Pruefung.KLIMAZONEN</c>.</summary>
        public const int ZONEN = %(zonen)d;

        /// <summary>Breite der SVG-viewBox — der Koordinatenraum der Pfade.</summary>
        public const double VIEWBOX_BREITE = %(breite)s;

        /// <summary>Hoehe der SVG-viewBox.</summary>
        public const double VIEWBOX_HOEHE = %(hoehe)s;

        /// <summary>Die viewBox als Zeichenkette fuer das <c>svg</c>-Element.</summary>
        public const string VIEWBOX = "0 0 %(breite)s %(hoehe)s";

        /// <summary>
        /// Die Pfadangabe einer Zone (1…15) — der Inhalt des <c>d</c>-Attributs,
        /// unveraendert aus der Kartengrafik. Die Flaechen tragen
        /// <c>fill-rule="evenodd"</c>; ohne diese Regel verschwaenden ihre
        /// Lochflaechen.
        /// </summary>
        /// <returns>Der Pfad, oder <c>""</c> ausserhalb 1…15.</returns>
        public static string Pfad(int zone)
        {
            if (zone < 1 || zone > ZONEN) return "";
            return PFADE[zone - 1];
        }

        /// <summary>Alle Zonen als Paare (Zone, Pfad) in der Reihenfolge 1…15.</summary>
        public static IReadOnlyList<(int Zone, string Pfad)> Alle()
        {
            var liste = new List<(int, string)>(ZONEN);
            for (int z = 1; z <= ZONEN; z++) liste.Add((z, PFADE[z - 1]));
            return liste;
        }

        /// <summary>Index 0…14 = Zone 1…15.</summary>
        private static readonly string[] PFADE =
        {
'''

FUSS = '''        };
    }
}
'''


def erzeugen(breite, hoehe, zuordnung):
    teile = [KOPF % {"zonen": ZONEN, "breite": breite, "hoehe": hoehe}]
    for z in range(ZONEN):
        teile.append('            // Zone %d\n' % (z + 1))
        teile.append('            "%s",\n' % zuordnung[z].replace('"', '\\"'))
    teile.append(FUSS)
    return "".join(teile)


def main():
    nur_pruefen = "--pruefen" in sys.argv
    breite, hoehe, zuordnung = lesen()
    text = erzeugen(breite, hoehe, zuordnung)

    if nur_pruefen:
        vorhanden = ""
        if os.path.exists(ZIEL):
            with open(ZIEL, "r", encoding="utf-8-sig") as f:
                vorhanden = f.read()
        if vorhanden.replace("\r\n", "\n") == text:
            print("KlimazonenPfade.cs ist auf dem Stand der SVG.")
            return 0
        print("KlimazonenPfade.cs weicht von der SVG ab - erzeugen.py ohne --pruefen laufen lassen.")
        return 1

    with open(ZIEL, "w", encoding="utf-8", newline="\n") as f:
        f.write(text)

    laengen = [len(p) for p in zuordnung]
    print("KlimazonenPfade.cs geschrieben: %d Zonen, viewBox %s x %s," %
          (ZONEN, breite, hoehe))
    print("  Pfadlaengen %d bis %d Zeichen, zusammen %d." %
          (min(laengen), max(laengen), sum(laengen)))
    return 0


if __name__ == "__main__":
    sys.exit(main())
