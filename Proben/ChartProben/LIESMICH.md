# ChartProben — der plattformfreie Nachweis des Diagramm-Renderers

**Zweck.** `EPOS.Kern/Allgemein/Bericht/ChartRenderer` zeichnet jedes Diagrammbild des
Berichts und der Oberfläche. Die Probe hält ihn auf einem nackten Linux-Abbild fest:
Sie zeichnet **jedes** Bild aus synthetischen Reihen (Sinus und Rampe, fest verdrahtet —
keine Datenbank, kein Zufall, keine Datei) und prüft je Bild PNG-Signatur, Bildmaße,
Farbvielfalt, jede erwartete Palettenfarbe und den **Determinismus** (zweimal zeichnen
liefert byte-gleiche Dateien). Rot wird sie, sobald der Renderer eine Windows-API braucht
oder sich ein Bild ändert.

Neben den Maßproben stehen **Gegenproben**: zwei Zeichenwege, die sich unterscheiden
*müssen* (`… (wirkt)`) — ein Fensterparameter, eine Strichart oder ein Legendenname, den der
Renderer stillschweigend überginge, bestünde jede Maß- und Farbprüfung und täte trotzdem
nichts. Dazu kommt die Versatzprobe der Legendenzeilen (`… (Versatz … px)`).

Die Probe läuft in `kern.yml` bei jedem Push.

---

## Aufruf

```bash
dotnet run --project Proben/ChartProben -c Release
```

| Schalter | Wirkung |
|---|---|
| `--ziel <ordner>` | Zielordner der Maßproben-Bilder. Vorgabe: `artifacts/chartproben` (steht in `.gitignore`) |
| `--ablage <ordner>` | legt **jedes** gezeichnete Bild als PNG in diesem Ordner ab — auch die beiden Bilder je Gegenprobe (`…_a.png` / `…_b.png`) und die beiden der Versatzprobe (`…_wenige.png` / `…_viele.png`). Dateiname = Probenname |
| `--hashes <datei>` | schreibt die **Hash-Messlatte**: je Bild eine Zeile aus SHA-256, zwei Leerzeichen und `<name>.png`, nach Name geordnet, mit LF und ohne BOM — das Format von `sha256sum`, also mit `sha256sum -c` im Ablageordner nachrechenbar |

Ohne `--ablage` und ohne `--hashes` verhält sich die Probe unverändert.

```bash
# Messlatte erzeugen und gegen den Bestand halten
dotnet run --project Proben/ChartProben -c Release -- \
    --ablage /tmp/chartbilder --hashes /tmp/neu.sha256
diff Proben/ChartProben/Messlatte_2026-09-20.sha256 /tmp/neu.sha256
```

---

## Die Hash-Messlatte

`Messlatte_2026-09-20.sha256` ist der eingefrorene Stand **aller** Probebilder. Sie ist die
Abnahme des Umbaus auf das **Zeichenmodell**
([Konzept DG-1](../../Dokumentation/aktuell/Konzept_Diagramme_Interaktiv_EPOS-Plan.md),
Etappe E1): Dort wird der Renderer hinter einem Modell aus Primitiven zerlegt, **ohne dass
sich ein Bild ändern darf**. Der Nachweis ist kein Urteil, sondern ein leerer Text-Diff
gegen diese Datei.

- **Warum Hashes und keine Bilder.** Bilder gehören nicht ins Repository
  (`EPOS.Kern.Tests/RepositoryOrdnungWacheTests`); die Hashliste ist eine kleine Textdatei
  und sagt dasselbe.
- **Warum alle Bilder und nicht nur die 51 Maßproben.** Was die Messlatte nicht nennt, kann
  sich beim Umbau unbemerkt ändern. Deshalb stehen auch die Bilder der Gegen- und
  Versatzproben darin, die im Bestand nur miteinander verglichen und nie geschrieben werden.
- **Umfang.** 72 Proben (51 Maßproben, 20 Gegenproben, 1 Versatzprobe) ergeben **91 Bilder**
  und ebenso viele Zeilen.
- **Die Messlatte gilt für die Vorgabe-Palette.** Die Farben der Diagramme sind eine
  Anwendungseinstellung (Rubrik „Diagramme"); die Probe setzt deshalb zu Beginn ausdrücklich
  `Farbpalette.Vorgabe`, damit die Hashliste unabhängig von einer Anwendereinstellung bleibt.
  Die Gegenprobe `palette_abweichend_wirkt` zeichnet dasselbe Bild ein zweites Mal mit einer
  getauschten Rolle; ihre zwei Bilder stehen **nicht** in der Ablage und nicht in der
  Messlatte — sonst hinge die eingefrorene Liste an einer Einstellung.
- **Wann sie neu eingefroren wird.** Nur, wenn ein Bild sich **bewusst** ändern soll — die
  Etappe E4 des Konzepts nennt den Fall (Linien gebündelt statt jeder n-te). Dann entsteht
  eine neue Datei mit dem Datum des Tages, und die alte wird im selben Schritt entfernt.
- Die Datei steht als `text eol=lf` in `.gitattributes`: Ein Auschecken mit `autocrlf`
  machte sonst CRLF daraus, und der Vergleich schlüge in jeder Zeile fehl.
- **Die Messlatte ist plattformgebunden.** Sie ist auf dem Linux-Läufer der CI eingefroren.
  Der Maler holt seine Schrift über `SKFontManager.Default`, also aus den Systemschriften der
  Plattform; auf Windows weichen deshalb **alle 91 Hashes** ab, obwohl die Probe dort dieselben
  72 Bilder mit 0 Verstößen meldet (gemessen 20.09.2026). Der Text-Diff gegen die Messlatte gilt
  auf dem Linux-Läufer; auf Windows zählt das strukturelle Ergebnis der Probe.
