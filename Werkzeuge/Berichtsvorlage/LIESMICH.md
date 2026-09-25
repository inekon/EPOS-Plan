# Berichtsvorlage — die Word-Vorlage des Berichts pflegen

Konsolenwerkzeug (`net10.0`, DocumentFormat.OpenXml) zum
[Konzept Berichtsvorlagen](../../Dokumentation/aktuell/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md),
Etappen BV-E0 und BV-E1, Abschnitt 6.3 und Anhang B.3. Es pflegt drei Dateien unter
`WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/`:

| Datei | Rolle |
|---|---|
| `Berichtsvorlage.docx` | Stilvorlage des heutigen `WordBerichtGenerator` (Seiteneinrichtung, Kopf- und Fußzeile, Stile; der Rumpf wird beim Erzeugen geleert); ausgeliefert als Rückfall des Codes und Quelle der beiden anderen |
| `Berichtsvorlage_Standard.docx` | Standardvorlage in der Stufe mit dem Sammelanker `{{bericht.inhalt}}` (BV-E1); ausgeliefert |
| `Berichtsvorlage_Beispiel.docx` | Beispielvorlage aus dem bisherigen Bericht mit Platzhaltern `{{…}}`; Anschauung, nicht ausgeliefert, ab BV-E2 die Standardvorlage |

Das Werkzeug hat eine **eigene Projektmappe** `Berichtsvorlage.sln` und gehört bewusst **nicht** in
`WP-Plan.sln` (Muster: `Werkzeuge/Auslieferungsvorlage`). Es verweist nicht auf `EPOS.Kern`; ob die
Beispielvorlage zum Code passt (Kapitelfolge, Kapitelüberschriften), prüft die Wache
`EPOS.Kern.Tests/BerichtsvorlageDateiWacheTests` in jedem Kern-Lauf.

## Aufruf

```bash
# Stilvorlage bereinigen — wiederholbar, ein zweiter Lauf findet nichts und schreibt nichts
dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- bereinigen \
    WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx

# Beispielvorlage aus der bereinigten Stilvorlage bauen (voller Aufbau, ab BV-E2)
dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- beispiel \
    WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx \
    WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Beispiel.docx

# Standardvorlage in der Stufe mit Sammelanker bauen (BV-E1, ausgeliefert)
dotnet run --project Werkzeuge/Berichtsvorlage -c Release -- beispiel \
    WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx \
    WindowsFormsApplication1/Allgemein/Bericht/Vorlagen/Berichtsvorlage_Standard.docx --sammelanker
```

Beide Modi arbeiten auf einer Arbeitskopie im Temp-Ordner, prüfen sie und ersetzen das Ziel erst, wenn alles
grün ist. Die Ausgabe nennt den Befund vorher und nachher.

| Rückgabe | Bedeutung |
|---|---|
| 0 | geschrieben, oder nichts zu tun |
| 2 | Aufruf falsch |
| 3 | Datei fehlt oder ist keine `.docx` |
| 4 | Prüfung rot: Validator, Stilregeln, Platzhalterregeln, oder die Quelle von `beispiel` ist nicht bereinigt |
| 1 | unerwarteter Fehler |

## Prüfungen beider Modi

- `OpenXmlValidator` in jeder Fassung von Office 2007 bis 2021 mit 0 Fehlern.
- Stilregeln: keine doppelte `w:styleId`; die acht Stile des Generators (`Title`, `Subtitle`,
  `Heading1–3`, `Normal`, `Hinweis`, `Beschriftung`); `Heading1–3` mit `w:outlineLvl`; das Absatzformat
  „EPOS Kapitelkopf“ (`EPOSKapitelkopf`, basedOn `Heading1`, Gliederungsebene 1, next `Normal`).
- Beispielvorlage: jeder Platzhalter ungeteilt in einem eigenen Run mit `w:noProof`; kein „INEKON“ in
  Rumpf, Kopf- und Fußzeile; jeder Kapitelplatzhalter mit `|ohne titel` unter einem Kapitelkopf.

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

Aufbau nach Anhang B.3, sprachneutral: Beschriftungen und Festtexte sind `{{text.*}}`.

| Nr. | Absatz | Inhalt |
|---|---|---|
| 1 | Titel | `{{bericht.titel}}` |
| 2 | Untertitel | `{{bericht.untertitel}}` |
| 3 | Tabelle Beschriftung · Wert (wie die heutige Eigenschaftstabelle) | `{{text.kunde}}` · `{{projekt.kunde}}`, `{{text.bearbeiter}}` · `{{projekt.bearbeiter}}`, `{{text.ersteller}}` · `{{ersteller.firma}}`, `{{text.varianten}}` · `{{bericht.varianten.liste}}`, `{{text.datum}}` · `{{bericht.datum}}` |
| 4 | Hinweis | `{{bericht.gebaeudemodell.ausweis\|leer statt strich}}` |
| 5 | Hinweis, Ende von Abschnitt 1 | `{{text.erstellt_mit}} {{ersteller.programm}} {{ersteller.version}}`; Abschnittswechsel „nächste Seite“ |
| 6 | Standard | `{{kapitel.inhalt}}` (bringt seine Überschrift „Inhalt“ selbst mit) |
| 7–20 | je Kapitel „EPOS Kapitelkopf“ und Standard | Projektbeschreibung, Komponenten & Varianten, Berechnungsergebnisse je Variante, Variantenvergleich, Wirtschaftlichkeit, Anhang, Checkliste für den Bewertungsbericht (DIN EN 17463, Anhang E) — darunter je `{{kapitel.<name>\|ohne titel}}` |

Das Deckblatt ist Abschnitt 1 ohne Kopf- und Fußzeile. Ab Abschnitt 2 gilt die Kopfzeile der Vorlage mit
`{{ersteller.programm}}` statt „EPOS-Plan“ und die Fußzeile: links `{{ersteller.firma}}` statt „INEKON GmbH“,
in der Mitte `{{bericht.datum}}` an der Stelle des DATE-Felds, rechts `{{text.seite}}` mit den Feldern PAGE
und NUMPAGES. Die Kapitel stehen in der Folge des heutigen Berichts, die Überschriften sind die, die er heute
druckt. Zwei Word-Kommentare erläutern Platzhalter, Formatangaben, Kapitel und den Zweck der Datei.

Mit `--sammelanker` entsteht die Stufe für BV-E1: Der Rumpf besteht nur aus dem Absatz `{{bericht.inhalt}}`,
Kopf- und Fußzeile wie oben, ohne Kommentare.

**Aus `--sammelanker` entsteht die Standardvorlage `Berichtsvorlage_Standard.docx`** (Aufruf oben). Sie wird
mit `Berichtsvorlage.docx` in beiden Lieferwegen ausgeliefert (`WindowsFormsApplication1.csproj`, MauiAsset in
`EPOS.iOS/EPOS.iOS.csproj`), die Beispielvorlage in keinem. **Nach jeder Änderung an `Berichtsvorlage.docx` —
auch nach `bereinigen` — sind Standard- und Beispielvorlage neu zu erzeugen.** `beispiel` schreibt wiederholbar
byte-gleich, denn es ändert eine Kopie der Quelle: Die Zeitstempel im Paket und die Daten in
`docProps/core.xml` stammen aus ihr, ein zweiter Lauf ändert keine Datei (nur ein Teil, den die Quelle nicht
führt — etwa der Kommentarteil —, bekäme beim Anlegen die Laufzeit). Inhalt und Lieferwege halten
`EPOS.Kern.Tests/BerichtsvorlageDateiWacheTests` und `AuslieferungsvorlagenWacheTests`.
