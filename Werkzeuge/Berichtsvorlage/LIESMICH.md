# Berichtsvorlage — die Word-Vorlage des Berichts pflegen

Konsolenwerkzeug (`net10.0`, DocumentFormat.OpenXml) zum
[Konzept Berichtsvorlagen](../../Dokumentation/aktuell/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md),
Etappen BV-E0 bis BV-E2, Abschnitte 4.9, 5.6, 6.3 und Anhang B.3. Es pflegt drei Dateien unter
`WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/`:

| Datei | Rolle |
|---|---|
| `Berichtsvorlage.docx` | Stilvorlage des heutigen `WordBerichtGenerator` (Seiteneinrichtung, Kopfzeile mit dem Firmenlogo, Fußzeile, Stile; der Rumpf wird beim Erzeugen geleert); ausgeliefert als Rückfall des Codes und Quelle der beiden anderen |
| `Berichtsvorlage_Standard.docx` | Standardvorlage mit Platzhaltern; ausgeliefert. Ab BV-E2 im vollen Aufbau ohne Kommentare (`--standard`), in BV-E1 die Stufe mit dem Sammelanker `{{bericht.inhalt}}` (`--sammelanker`) |
| `Berichtsvorlage_Beispiel.docx` | Beispielvorlage aus dem bisherigen Bericht im vollen Aufbau, erläutert in Word-Kommentaren (Lehrvorlage); Anschauung, nicht ausgeliefert |

Das Werkzeug hat eine **eigene Projektmappe** `Berichtsvorlage.sln` und gehört bewusst **nicht** in
`WP-Plan.sln` (Muster: `Werkzeuge/Auslieferungsvorlage`). Es verweist nicht auf `EPOS.Kern`; ob die
Beispielvorlage zum Code passt (Kapitelfolge, Kapitelköpfe), prüft die Wache
`EPOS.Kern.Tests/BerichtsvorlageDateiWacheTests` in jedem Kern-Lauf.

## Aufruf

```bash
# Stilvorlage bereinigen — wiederholbar, ein zweiter Lauf findet nichts und schreibt nichts
dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- bereinigen \
    WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx

# Beispielvorlage (voller Aufbau, mit Kommentaren) aus der bereinigten Stilvorlage bauen
dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- beispiel \
    WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx \
    WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Beispiel.docx

# Standardvorlage im vollen Aufbau ohne Kommentare bauen (ab BV-E2, ausgeliefert)
dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- beispiel \
    WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx \
    WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Standard.docx --standard

# Standardvorlage in der Stufe mit Sammelanker bauen (BV-E1)
dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- beispiel \
    WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx \
    WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Standard.docx --sammelanker
```

| Schalter von `beispiel` | Wirkung | `EPOS.Vorlage` |
|---|---|---|
| — | Beispielvorlage: voller Aufbau, drei Kommentare | `beispiel` |
| `--standard` | Standardvorlage: voller Aufbau, keine Kommentare | `standard` |
| `--sammelanker` | Standardvorlage in der Stufe mit Sammelanker: Rumpf nur `{{bericht.inhalt}}`, keine Kommentare | `standard-sammelanker` |
| `--katalogfassung <n>` | `EPOS.Katalogfassung` in `custom.xml`, ganze Zahl ab 1; Vorgabe 2 (Katalog v2 führt `text.kapitel_*`) | — |

`--standard` und `--sammelanker` schließen einander aus. Der Name des Ziels muss zur Art passen:
`Berichtsvorlage_Standard.docx` entsteht nur mit `--standard` oder `--sammelanker`, `Berichtsvorlage_Beispiel.docx`
nur ohne beide — so trägt die ausgelieferte Standardvorlage nie die Kommentare der Lehrvorlage. Jedes andere Ziel,
etwa eine Probe, nimmt jede Art.

Beide Modi arbeiten auf einer Arbeitskopie im Temp-Ordner, prüfen sie und ersetzen das Ziel erst, wenn alles
grün ist. Die Ausgabe nennt den Befund vorher und nachher.

| Rückgabe | Bedeutung |
|---|---|
| 0 | geschrieben, oder nichts zu tun |
| 2 | Aufruf falsch, oder Ziel und Art passen nicht zusammen |
| 3 | Datei fehlt oder ist keine `.docx` |
| 4 | Prüfung rot: Validator, Stilregeln, Platzhalterregeln, oder die Quelle von `beispiel` ist nicht bereinigt |
| 1 | unerwarteter Fehler |

## Prüfungen beider Modi

- `OpenXmlValidator` in jeder Fassung von Office 2007 bis 2021 mit 0 Fehlern.
- Stilregeln: keine doppelte `w:styleId`; die acht Stile des Generators (`Title`, `Subtitle`,
  `Heading1–3`, `Normal`, `Hinweis`, `Beschriftung`); `Heading1–3` mit `w:outlineLvl`; das Absatzformat
  „EPOS Kapitelkopf“ (`EPOSKapitelkopf`, basedOn `Heading1`, Gliederungsebene 1, next `Normal`).
- Vorlagen mit Platzhaltern: jeder Platzhalter ungeteilt in einem eigenen Run mit `w:noProof`; kein „INEKON“
  in Rumpf, Kopf- und Fußzeile; im vollen Aufbau jeder Kapitelplatzhalter mit `|ohne titel` unmittelbar unter
  seinem Kapitelkopf `{{text.kapitel_<name>}}` und jeder Kapitelkopf über seinem Kapitel; in der Kopfzeile
  genau ein Bild, der Bildplatzhalter des Logos mit dem Platzhalterbild; drei Kommentare in der
  Beispielvorlage, keiner in den Standardvorlagen; `EPOS.Katalogfassung` und `EPOS.Vorlage` mit den Werten
  des Laufs in `custom.xml`.

## bereinigen

Die Vorlage stammt aus der JavaScript-Bibliothek `docx`: Sie schreibt eigene Vorgaben für `Title` und
`Heading1–6` und hängt die Absatzstile des Aufrufers mit derselben ID an. So standen `Title` und `Heading1–3`
je zweimal im Stilteil, und der Validator meldete je Fassung vier Fehler („should have unique value“).

**Welche Definition bleibt, ist gemessen:** Word 16.0 (Build 16.0.20326) über COM, je eine Probe mit der
Originaldatei, nur der ersten und nur der letzten Definition, dazu der Bericht 1030 mit allen Bausteinen.

| Stil | 1. Definition (docx-Vorgabe) | 2. Definition (Vorlage) | Word zeigt |
|---|---|---|---|
| Titel | 28 pt, nächster Absatz Standard | 28 pt fett 1F4E79, Abstand 120/12 pt, ohne `w:next` | 28 pt fett 1F4E79, 120/12 pt, nächster Absatz **Standard** |
| Überschrift 1 | 16 pt 2E74B5 | 15 pt fett 1F4E79, 18/8 pt, Rahmen unten, `outlineLvl 0` | wie die 2. |
| Überschrift 2 | 13 pt 2E74B5 | 12,5 pt fett 1F4E79, 14/6 pt, `outlineLvl 1` | wie die 2. |
| Überschrift 3 | 12 pt 1F4D78 | 11 pt fett 595959, 10/5 pt, `outlineLvl 2` | wie die 2. |

Word vereinigt die Definitionen, und die spätere gewinnt; was nur die frühere trägt (beim Titel `w:next`,
bei allen vier `w:qFormat`), wirkt trotzdem. Deshalb bleibt je ID die **letzte** Definition, und Angaben,
die nur eine frühere trägt, werden übernommen. Die erste allein änderte Titel und jede Überschrift; mit der
bereinigten Fassung misst Word die Stilprobe und alle Absätze des Berichts 1030 (neun Seiten,
Inhaltsverzeichnis mit 19 Einträgen) zeilengleich zur alten Vorlage. Außer `word/styles.xml` bleibt jeder
Teil des Pakets byte-gleich.

Dazu legt `bereinigen` das Absatzformat „EPOS Kapitelkopf“ an, wenn es fehlt. Der heutige Generator benutzt
es nicht; der Bericht ändert sich dadurch nicht.

## beispiel

Aufbau nach Anhang B.3, sprachneutral: Beschriftungen, Festtexte und Kapiteltitel sind `{{text.*}}` (4.9).

| Nr. | Absatz | Inhalt |
|---|---|---|
| 1 | Titel | `{{bericht.titel}}` |
| 2 | Untertitel | `{{bericht.untertitel}}` |
| 3 | Tabelle Beschriftung · Wert (wie die heutige Eigenschaftstabelle) | `{{text.kunde}}` · `{{projekt.kunde}}`, `{{text.bearbeiter}}` · `{{projekt.bearbeiter}}`, `{{text.ersteller}}` · `{{ersteller.firma}}`, `{{text.varianten}}` · `{{bericht.varianten.liste}}`, `{{text.datum}}` · `{{bericht.datum}}` |
| 4 | Hinweis | `{{bericht.gebaeudemodell.ausweis\|leer statt strich}}` |
| 5 | Hinweis, Ende von Abschnitt 1 | `{{text.erstellt_mit}} {{ersteller.programm}} {{ersteller.version}}`; Abschnittswechsel „nächste Seite“ |
| 6 | Standard | `{{kapitel.inhalt}}` (bringt seine Überschrift „Inhalt“ selbst mit) |
| 7–20 | je Kapitel „EPOS Kapitelkopf“ und Standard | Kapitelkopf `{{text.kapitel_<name>}}`, darunter `{{kapitel.<name>\|ohne titel}}` — für `projekt`, `komponenten`, `ergebnisse`, `vergleich`, `wirtschaftlichkeit`, `anhang`, `anhang_e` |

Die Kapitel stehen in der Folge des heutigen Berichts. Den Kapitelkopf füllt der Katalog mit dem Titel, den der
Bericht heute druckt, in der Sprache des Berichts — deutsch Projektbeschreibung, Komponenten & Varianten,
Berechnungsergebnisse je Variante, Variantenvergleich, Wirtschaftlichkeit, Anhang und Checkliste für den
Bewertungsbericht (DIN EN 17463, Anhang E). Wer den Platzhalter durch eigenen Text ersetzt, bekommt diesen Text in
jeder Sprache; das Format „EPOS Kapitelkopf“ lässt den Kopf mit seinem Kapitel entfallen, auch mit eigenem Text.

Das Deckblatt ist Abschnitt 1 ohne Kopf- und Fußzeile. Ab Abschnitt 2 gilt die Kopfzeile der Vorlage mit
`{{ersteller.programm}}` statt „EPOS-Plan“ und dem Bildplatzhalter des Logos (unten) und die Fußzeile: links
`{{ersteller.firma}}` statt „INEKON GmbH“, in der Mitte `{{bericht.datum}}` an der Stelle des DATE-Felds, rechts
`{{text.seite}}` mit den Feldern PAGE und NUMPAGES.

Drei Word-Kommentare erläutern die Beispielvorlage: am Deckblatt Platzhalter, Formatangaben, Kopf- und Fußzeile
samt Logo — Word erlaubt in Kopf- und Fußzeilen keine Kommentare — und den Zweck der Datei; am
Inhaltsverzeichnis die Kapitelplatzhalter; am ersten Kapitelkopf, dass der Kapitelkopf ein Platzhalter für den
Kapiteltitel in der Sprache des Berichts ist und durch eigenen Text ersetzt werden darf. Die Standardvorlagen
tragen keine Kommentare: Die Engine entfernte sie bei jedem Lauf und nennte ihre Zahl in der Laufmeldung.

Mit `--sammelanker` besteht der Rumpf nur aus dem Absatz `{{bericht.inhalt}}`; Kopf- und Fußzeile wie oben.

### Logo der Kopfzeile: Bildplatzhalter (Entscheid BV-E2-1)

Die Stilvorlage trägt rechts in der Kopfzeile das Firmenlogo des Herstellers. In jeder Art von `beispiel` wird es
zum **Bildplatzhalter**: Das Bild bleibt an Ort, in Größe und Umbruch — eingebettet (`wp:inline`) hinter dem
rechtsbündigen Tabulator, 857250 × 466725 EMU (67,5 × 36,75 pt, 2,38 × 1,30 cm) —, trägt im Alternativtext
(`wp:docPr/@descr`) `{{bild.ersteller.logo}}` und zeigt statt des Logos ein neutrales Platzhalterbild. Beim
Erstellen des Berichts setzt die Engine dort das Logo aus der Einstellung `BerichtLogo` ein oder entfernt das
Bild, wenn keines gesetzt ist. Wer ein festes eigenes Logo will, ersetzt in seiner Kopie das Bild und entfernt
den Alternativtext.

Zweck: Der Bericht nennt den Ersteller, nicht den Hersteller (BV-Q8) — das Logo der Kopfzeile ist das des
Lizenznehmers. Als Bildplatzhalter behält die Kopfzeile ihre Gestaltung, und jeder Lizenznehmer bekommt sein Logo
über die Einstellung, ohne eine eigene Vorlage anzulegen. Die übrigen Bildplatzhalter (Berichtsbilder,
Bildgröße Stufe 2) kommen mit BV-E5.

**Das Platzhalterbild** ist `Logoplatzhalter.png` in diesem Ordner, eingebettet als Ressource des Werkzeugs —
eine feste Datei, damit jeder Lauf auf jedem Rechner dieselben Bytes schreibt; eine Zeichnung zur Laufzeit hinge
von den Schriften des Rechners ab. PNG, 150 × 82 Pixel wie das JPEG-Logo der Stilvorlage, 160 dpi (so entspricht
die Bildgröße der Ausdehnung in der Kopfzeile), 8 Bit RGB, 1446 Byte: hellgrau `F2F2F2` mit einem Pixel Rahmen
`BFBFBF` und dem Wort „Logo“ in `808080`, Calibri 34 Pixel, mittig. Das Werkzeug bricht ab, wenn das Pixelmaß
des Platzhalters nicht dem des Logos der Quelle entspricht; ein neues Platzhalterbild ersetzt die Datei in
gleicher Art. Im Paket liegt es unter `/word/media/logoplatzhalter.png` und übernimmt die Beziehungskennung des
Logos, dessen Bildteil samt Beziehung gelöst wird. Den Tausch macht `System.IO.Packaging` direkt, denn das SDK
legt einen neuen Bildteil unter Windows an der Paketwurzel an (`/media/image.png`) und vergibt ohne Vorgabe eine
zufällige Beziehungskennung.

### custom.xml

`docProps/custom.xml` führt zwei Eigenschaften (Konzept 5.6 und 10.1); dieselben Namen liest der
`Vorlagenpruefer` des Kerns.

| Eigenschaft | Typ | Wert |
|---|---|---|
| `EPOS.Katalogfassung` | `vt:i4` | aus `--katalogfassung`, Vorgabe 2 |
| `EPOS.Vorlage` | `vt:lpwstr` | `beispiel`, `standard` oder `standard-sammelanker` |

Vorhandene Eigenschaften bleiben; eine gleichnamige bekommt den neuen Wert und behält ihre Nummer (`pid`), eine
neue die nächste freie ab 2.

### Auslieferung und Wiederholbarkeit

Stil- und Standardvorlage werden in beiden Lieferwegen ausgeliefert (`WindowsFormsApplication1.csproj`,
MauiAsset in `EPOS.iOS/EPOS.iOS.csproj`), die Beispielvorlage in keinem. **Nach jeder Änderung an
`Berichtsvorlage.docx` — auch nach `bereinigen` — und am Werkzeug sind Standard- und Beispielvorlage neu zu
erzeugen.** `beispiel` schreibt wiederholbar byte-gleich: Es ändert eine Kopie der Quelle, die Daten in
`docProps/core.xml` stammen aus ihr, und zum Schluss bekommt jeder Eintrag des Pakets den Zeitstempel der Quelle —
auch das neu angelegte Platzhalterbild, das die Paketbibliothek sonst mit der Laufzeit stempelte. Inhalt und
Lieferwege halten `EPOS.Kern.Tests/BerichtsvorlageDateiWacheTests` und `AuslieferungsvorlagenWacheTests`.
