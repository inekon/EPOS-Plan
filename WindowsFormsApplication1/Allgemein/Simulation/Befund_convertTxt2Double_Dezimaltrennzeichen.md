# Befund: `convertTxt2Double`, Dezimalkomma und der VDI-3805-Import

Stand 18.08.2026. Nicht committet. Abschnitte 1–6: Analyse; **Abschnitt 7: die
Vorschläge aus Abschnitt 4 sind am selben Tag umgesetzt worden** (Zeilenangaben in
Abschnitt 3 beziehen sich auf den Stand davor).

Ausgangspunkt ist der letzte Punkt in Abschnitt 4 von
[`VdiImport_WP_Transaktion_Protokoll.md`](VdiImport_WP_Transaktion_Protokoll.md): beim
Smoke-Test des Wärmepumpen-Imports wurde aus „5,5" der Wert 55 und aus „4,2" der Wert 42.
Dieses Dokument klärt die drei dort offenen Fragen: (1) Welches Dezimaltrennzeichen liefern
echte VDI-3805-Dateien? (2) Wo wird `convertTxt2Double` überall gespeist, und wo kommt ein
Komma realistisch an? (3) Wie sähe robustes Parsen aus?

**Kurzfassung:** Echte VDI-3805-Dateien verwenden den **Dezimalpunkt** — für den Regelfall
des Imports ist das eine **Entwarnung**, die im Smoke sichtbaren Kommawerte waren
synthetische Harness-Daten. Realistisch bleibt das Komma an den **von Hand editierbaren
Textfeldern** der Einlese-Dialoge (Heizkessel, Pufferspeicher): dort wird „91,5" heute
kommentarlos zu 915 gespeichert. Dazu ein latenter Fallstrick in `Form_AdminPV`
(DB-Rundreise durch die Systemkultur). Vorschlag: `convertTxt2Double` intern auf die
`ZahlParsen`-Regel stellen (Komma und Punkt, kein Tausendertrennzeichen, Fehler bleiben
erkennbar) und die Textbox-Stellen nach dem Hausmuster auf `ZahlPruefen` heben.

---

## 1. Mechanik des Fehlverhaltens (nachgemessen)

`Program.convertTxt2Double` ([`Program.cs:311`](../../Program.cs)) ruft
`Convert.ToDouble(txt, CultureInfo.InvariantCulture)` auf. `Convert.ToDouble` parst mit
`NumberStyles.Float | NumberStyles.AllowThousands`, und .NET prüft die **Gruppengröße von
Tausendertrennzeichen nicht**. In der invarianten Kultur ist das Komma
Tausendertrennzeichen — ein Dezimalkomma wird deshalb nie abgewiesen, sondern still
„verschluckt".

Messung (PowerShell, .NET; 18.08.2026):

| Eingabe   | `Convert.ToDouble(…, Invariant)` | `Program.ZahlParsen` |
|-----------|----------------------------------|----------------------|
| `5,5`     | **55**                           | 5.5 |
| `4,2`     | **42**                           | 4.2 |
| `17,50`   | **1750**                         | 17.5 |
| `0,36`    | **36**                           | 0.36 |
| `3,37`    | **337**                          | 3.37 |
| `-0,45`   | **-45**                          | -0.45 |
| `1.234,5` | FormatException                  | abgelehnt (`false`) |
| `abc`     | FormatException                  | abgelehnt (`false`) |
| `4.27`    | 4.27 (korrekt)                   | 4.27 |

Drei Verhaltensweisen von `convertTxt2Double`, die man zusammen sehen muss:

* **Komma → stiller Faktor-10/100/1000-Fehler** (Kern des Befunds).
* **Nicht parsbarer Text → FormatException** (kein `TryParse`): im Mehrfach-Übernahmepfad
  der Einlese-Dialoge gefangen und als „Fehler" gezählt, in den Filterschleifen (s. u.)
  ungefangen.
* **Leerstring → 0** (expliziter Sonderfall) — auf diesen Vertrag verlassen sich die
  Aufrufer.

## 2. Frage 1: Echte VDI-3805-Dateien liefern den Dezimalpunkt

Der Normtext (VDI 3805 Blatt 1, Ausgabe 2022-07) ist kostenpflichtig und war nicht frei
einsehbar; die frei zugängliche Vorschau enthält den Zahlenformat-Abschnitt nicht.
Geprüft wurden stattdessen **zwei echte Herstellerdatensätze** vom ETU-Downloadportal
`vdi3805-bim.de` (Blatt 22, Wärmepumpen — klassisches Semikolonformat, genau das, was
`Allgemein\Import\VDI 3805\WaermepumpenImport.cs` liest):

* **Nibe** (`PART22_Nibe_DE_DEU_200506_20170721_KATALOG_2016.vdi`, Richtlinien-Ausgabe
  2005-06): Kopf `010;22;200506;Nibe Systemtechnik GmbH;…`, Datenzeilen z. B.
  `700;1;;F1245 5kW, …;4.65;4.3;1.08;…` und `710.03;1;4.3;50;-8;67;0.36;…` —
  durchgehend Punkt.
* **Viessmann** (`PART22_Viessmann_DE_DEU_201903_20201009_KATALOG.vdi`, Ausgabe 2019-03 —
  also mit den Satzarten **710.09/710.91**, die `WaermepumpenImport` auswertet):
  `700;1;1;Typ BWT 221.B06;5.73;4.6;1.25;…` und `710.91;1;-10;4.27;1.26;3.37;` —
  durchgehend Punkt. Die Feldlage passt zum Importer (`token[3]` = Leistung,
  `token[5]` = COP).
* Die gerenderte Portal-Ansicht (partview.php) zeigt dieselben Werte ebenfalls mit Punkt
  (COP 3.93 / 4.72 / 5.32).

**Folgerung:** Bei normkonformen Dateien werden COP, Ptherm, Pkuehl, Nennleistung und
Kühlleistung **korrekt** geparst — die im Smoke-Test beobachteten Werte (COP 42, Ptherm 55)
stammten aus den synthetischen Harness-Kommawerten, nicht aus einem realen Dateiproblem.
Die Befürchtung „alle importierten Kennlinien um Größenordnungen falsch" bestätigt sich
für den Regelfall **nicht**.

Restrisiko am Dateipfad bleibt dreifach:

1. Eine nicht normkonforme oder von Hand bearbeitete Datei mit Komma wird **still falsch**
   gelesen statt abgewiesen — genau das zeigte der Smoke.
2. Nicht-numerischer Feldinhalt wirft `FormatException`. In `UebernehmeEintrag` gefangen
   (Eintrag zählt als „Fehler", ohne Hinweis auf das Feld), in den **Filterschleifen
   ungefangen** — der Dialog stürzt schon beim Befüllen der Liste:
   [`Form_WP_einlesen.cs:117`](../../Views/Wärmepumpe/Form_WP_einlesen.cs),
   [`Form_Heizkessel_einlesen.cs:64`](../../Views/Heizkessel/Form_Heizkessel_einlesen.cs),
   [`Form_PufferSp_einlesen.cs:106`](../../Views/Pufferspeicher/Form_PufferSp_einlesen.cs).
3. Der Solarkollektoren-Import hat sein eigenes, toleranteres `ParseDouble`
   (`Solarkollektorenlmport.cs:132`: Komma→Punkt, `TryParse`) — er ist vom Befund nicht
   betroffen, nutzt aber `NumberStyles.Any` (inkl. `AllowThousands`), siehe Abschnitt 5.

## 3. Frage 2: Aufruferkataster

28 Aufrufstellen in 4 Dateien des aktiven Projekts (die Altkopien
`WindowsFormsApplication1 - Kopie` und `mit_Puffer_KI_Lösungsversuch` sind hier
ausgeklammert). Zeilennummern: Stand Hauptarbeitsverzeichnis 18.08.2026 (inkl. der nicht
committeten Aufräumklammer in `Form_WP_einlesen.cs`).

### Gruppe A — direkt aus der VDI-Datei gespeist (Dezimalpunkt, s. Abschnitt 2)

| Stelle | Wert |
|---|---|
| `Form_WP_einlesen.cs:117` | Filter: `szThLeistung` |
| `Form_WP_einlesen.cs:223/230/233` | Nennleistung, Heizstab, Kühlleistung |
| `Form_WP_einlesen.cs:276/278` | Kennlinie Heizen: COP, Ptherm |
| `Form_WP_einlesen.cs:290/292` | Kennlinie Kühlen: COP, Pkuehl |
| `Form_Heizkessel_einlesen.cs:64` | Filter: `m_szThLeistung` |
| `Form_Heizkessel_einlesen.cs:362–364` | `szNOx`, `szCO2`, `szCO` (Zwischenspeicher aus der Datei, Zeilen 145–147) |
| `Form_PufferSp_einlesen.cs:106` | Filter: `m_szVolumen` |

Bei normkonformen Dateien korrekt; Risiko nur wie in Abschnitt 2 (stilles Fehllesen
abweichender Dateien, Absturz der Filter bei Nicht-Zahlen).

### Gruppe B — Textboxen, VDI-vorbefüllt, aber von Hand editierbar (**Komma realistisch**)

| Stelle | Feld | Vorbefüllung |
|---|---|---|
| `Form_Heizkessel_einlesen.cs:345` | `textBox_ThLeistung` | Zeile 140 |
| `Form_Heizkessel_einlesen.cs:348–352` | `textBox__Wirkungsgrad` | Zeile 143 |
| `Form_Heizkessel_einlesen.cs:358` | `textBox_Versluste` | Zeile 142 |
| `Form_PufferSp_einlesen.cs:245` | `textBox_Versluste` | Zeile 73 |

Die Handkorrektur dieser Felder ist **dokumentierter Anwendungsfall** (Kommentar am
Einzelpfad von `btn_Uebernehmen_Click`, `Form_Heizkessel_einlesen.cs:164–166: „die
Detailfelder werden nicht neu besetzt, damit eine Korrektur von Hand erhalten bleibt").
Ein deutscher Anwender, der dort „91,5" eintippt, speichert kommentarlos 915. Es gibt an
diesen Feldern **keine** Zahlprüfung (kein `ZahlFaerben`, kein `checkDouble`; einziger
TextChanged-Handler ist der Suchfilter). Nicht parsbare Eingaben werfen und werden im
Übernahmepfad nur pauschal als „Fehler" gezählt. **Das ist die dringendste Gruppe.**

(`Form_PufferSp_einlesen.cs:246` liest daneben `textBox_Volumen` über `convertTxt2Int` —
`Int32.TryParse` liefert bei „750.0" oder „750,0" still 0, s. Abschnitt 5.)

### Gruppe C — DB-Rundreise durch die Systemkultur (`Form_AdminPV`)

`listBox_PV_SelectedIndexChanged` ([`Form_AdminPV.cs:150–170`](../../Views/Photovoltaik/Form_AdminPV.cs))
formatiert beim Auswählen eines Eintrags die DB-Werte mit `ToString("F2")` bzw.
`ToString()` — **Systemkultur**, auf deutschem Windows also mit Komma — in die Textboxen
und liest sie **sofort** per `convertTxt2Double` ins `model` zurück: aus 17,5 (Anzeige
„17,50") wird `model.m_Wirkungsgrad = 1750`.

Derzeit **folgenlos**, weil `btn_Speichern_Click` (Folgepaket zu ab5bf32) sämtliche
Model-Felder vor dem Schreiben aus `ZahlPruefen`-Ergebnissen neu setzt — und `ZahlPruefen`
die deutsche Anzeige korrekt parst. Es bleibt ein latenter Fallstrick: jede künftige
Verwendung von `model` zwischen Auswahl und Speichern übernähme die verfälschten Werte.
Die zehn `convertTxt2Double`-Aufrufe dort sind faktisch tote, aber vergiftete Zuweisungen.

## 4. Frage 3: Vorschlag für robustes Parsen

Das Vorbild existiert im Haus: `Program.ZahlParsen` / `Program.ZahlPruefen`
([`Program.cs:209/275`](../../Program.cs)) — Komma **und** Punkt als Dezimaltrennzeichen,
`NumberStyles.Float` (also **ohne** `AllowThousands`), invariant, `TryParse` statt Wurf,
bei `ZahlPruefen` sprechende Meldung + Fokus. Drei Stufen:

1. **Zentral, chirurgisch:** `convertTxt2Double` intern auf die `ZahlParsen`-Regel
   stellen — `Trim`, Komma→Punkt, `double.TryParse(NumberStyles.Float, Invariant)`;
   Leerstring → 0 (Bestandsvertrag); nicht parsbar → weiterhin `FormatException` werfen
   (hält die bestehenden Fehlerpfade der Einlese-Dialoge am Leben, statt still 0 zu
   liefern). Wirkung: die Tausendertrennzeichen-Falle verschwindet an allen 28 Stellen auf
   einmal; „5,5" und „5.5" bedeuten dasselbe. Bewusste Verhaltensänderung: bisher
   akzeptierte en-US-Eingaben wie „1,234.5" (heute 1234.5) würden abgewiesen — in der
   deutschen Oberfläche kein realistischer Verlust, und der `ZahlPruefen`-Meldetext nennt
   ohnehin „Dezimaltrennzeichen Komma oder Punkt".
2. **Je Dialog (Hausmuster ab5bf32):** die Gruppe-B-Stellen beim Übernehmen auf
   `Program.ZahlPruefen` heben (Meldung + Fokus + Dialog bleibt offen statt Exception),
   wie in `Form_AdminPV.btn_Speichern_Click` bereits geschehen. Die zehn Lade-Aufrufe in
   `Form_AdminPV.listBox_PV_SelectedIndexChanged` entweder streichen (das Model wird beim
   Speichern ohnehin neu besetzt) oder auf `ZahlParsen` stellen.
3. **Dateipfad:** im VDI-Import nicht parsbare Pflichtfelder **erkennbar** machen —
   Eintrag als Fehler mit Feldname zählen statt `FormatException` aus der Tiefe — und die
   drei Filterschleifen gegen Nicht-Zahlen absichern (sonst stürzt der Dialog schon beim
   Listenaufbau).

## 5. Randnotizen (angrenzend, nicht Kern des Befunds)

* `Solarkollektorenlmport.ParseDouble` nutzt nach dem Komma→Punkt-Ersetzen
  `NumberStyles.Any` — `AllowThousands` bleibt damit aktiv. Auf `NumberStyles.Float`
  engen, dann ist auch dieser Weg gruppenzeichenfrei.
* `Program.convertTxt2Int` (`Program.cs:321`) liefert bei nicht parsbarem Text **still 0**
  (`Int32.TryParse` ohne Kulturangabe): „35.0"/„35,0" → 0. Betroffen u. a.
  `Form_PufferSp_einlesen.cs:246` (Volumen) und die Vorlauf-/Last-Felder im WP-Import.
  Echte Dateien liefern dort bislang ganze Zahlen; beim Umbau mit absichern.
* Die Kultur-Inkonsistenz im Bestand ist als Befund bekannt
  ([`WPPlan_Code_Befunde.md`](../../../WPPlan_Code_Befunde.md), Punkte 5 und 28:
  `checkDouble` kulturabhängig vs. `convertTxt2Double` invariant). Dieses Dokument
  präzisiert die `AllowThousands`-Falle und liefert das Aufruferkataster.

## 6. Quellen

* Echtdaten: ETU-Downloadportal —
  [Nibe-Datensatz](https://www.vdi3805-bim.de/VDIFiles/Blatt_22/PART22_Nibe_DE_DEU_200506_20170721_KATALOG_2016.vdi),
  [Viessmann-Datensatz](https://www.vdi3805-bim.de/VDIFiles/Blatt_22/PART22_Viessmann_DE_DEU_201903_20201009_KATALOG.vdi),
  [Portal-Ansicht](https://www.vdi3805-bim.de/partview.php?file=VDIFiles//Blatt_22/PART22_Nibe_DE_DEU_200506_20170721_KATALOG_2016.vdi&tpart=22&tmanufacturerID=0&productlevel=3&100=3&110=6&700=1&group=Unbekannte+Produktserie)
  (alle abgerufen 18.08.2026).
* Norm-Übersicht: [VDI 3805 Blatt 22 (VDI-Seite)](https://www.vdi.de/richtlinien/details/vdi-3805-blatt-22-produktdatenaustausch-in-der-technischen-gebaeudeausruestung-waermepumpen),
  [VDI 3805 Blatt 1, 2022-07 (DIN Media)](https://www.dinmedia.de/en/technical-rule/vdi-3805-blatt-1/350219035) —
  Normtext kostenpflichtig, Zahlenformat-Abschnitt nicht frei einsehbar.
* Messungen: PowerShell/.NET 8 auf diesem Rechner, 18.08.2026 (Tabelle in Abschnitt 1).

## 7. Umsetzung (18.08.2026)

Alle drei Stufen aus Abschnitt 4 plus die erste Randnotiz aus Abschnitt 5 sind umgesetzt.
Nicht committet.

### 7.1 Änderungen

| Datei | Änderung |
|---|---|
| `Program.cs` | **Stufe 1:** `convertTxt2Double` parst jetzt nach der `ZahlParsen`-Regel (Komma ODER Punkt, kein Tausendertrennzeichen). Vertrag erhalten: leer/null → 0, nicht parsbar → `FormatException` — neu mit dem Wert im Meldetext (`Keine gültige Zahl: "…"`), damit die „Fehler"-Zählungen der Einlese-Dialoge aussagekräftiger protokollieren. |
| `Views\Heizkessel\Form_Heizkessel_einlesen.cs` | **Stufe 2:** Einzelpfad von `btn_Uebernehmen_Click` prüft `textBox_ThLeistung`, `textBox__Wirkungsgrad`, `textBox_Versluste` vorab mit `ZahlPruefen` (`leerErlaubt: true` — leer bleibt 0). **Stufe 3:** Filterschleife (`FuelleListe`) auf `ZahlParsen` mit 0-Fallback. |
| `Views\Pufferspeicher\Form_PufferSp_einlesen.cs` | **Stufe 2:** Einzelpfad prüft `textBox_Versluste` mit `ZahlPruefen` (`leerErlaubt: true`). **Stufe 3:** Filterschleife auf `ZahlParsen` mit 0-Fallback. |
| `Views\Wärmepumpe\Form_WP_einlesen.cs` | **Stufe 3:** Filterschleife auf `ZahlParsen` mit 0-Fallback. (Der übrige Weg ist rein dateigespeist und über Stufe 1 abgedeckt; die frische Aufräumklammer blieb unberührt.) |
| `Views\Photovoltaik\Form_AdminPV.cs` | **Gruppe C:** die zehn `model.* = convertTxt2Double(…)`-Rückleser in `listBox_PV_SelectedIndexChanged` gestrichen — dort wird nur noch die Anzeige besetzt, das Model füllt allein `btn_Speichern_Click` aus den `ZahlPruefen`-Ergebnissen. |
| `Allgemein\Import\VDI 3805\Solarkollektorenlmport.cs` | **Randnotiz 1:** `ParseDouble` von `NumberStyles.Any` auf `NumberStyles.Float` geengt (kein `AllowThousands` mehr). |

### 7.2 Bewusste Verhaltensänderungen

* „5,5" ergibt jetzt überall 5.5 statt 55 (der Kern des Befunds).
* Nur-Whitespace ergibt 0 statt `FormatException` (Angleichung an den Leer-→-0-Vertrag).
* en-US-Gruppenschreibweise „1,234.5" (bisher 1234.5) wird abgewiesen; „1,234" bedeutet
  jetzt 1.234 statt 1234 — in der deutschen Oberfläche die richtige Lesart.
* Die drei Listenfilter stürzen bei nicht parsbaren Datei-Werten nicht mehr ab; solche
  Einträge zählen als 0 kW bzw. 0 l und bleiben beim Standardfilter sichtbar — den Fehler
  meldet erst die Übernahme.
* Neue Wurf-Fälle entstehen nicht: die neue Regel wirft nur dort, wo die alte auch warf
  (Müll, gemischte Schreibweise), und zusätzlich nie.

### 7.3 Bewusst nicht angefasst

* Feldgenaue Fehlermeldungen je VDI-Datenfeld (die `FormatException` nennt jetzt den Wert,
  der Mehrfachpfad weiterhin den Eintrag — das genügt als Diagnose, ohne die Importer
  umzubauen).
* Die Mehrfachpfade prüfen weiterhin nicht modal je Eintrag (Zählung wie Bestand); die
  Detailfelder werden dort ohnehin je Eintrag frisch aus der Datei besetzt.
* ~~`convertTxt2Int`~~ und ~~`Program.checkDouble`~~ — im zweiten Durchgang doch erledigt,
  siehe 7.5.

### 7.4 Verifikation

Build: Full-MSBuild VS 2022, `WindowsFormsApplication1.csproj`, Debug x86,
`ArtifactsPath=%TEMP%\wpb2` (mit `-restore`). Baseline **vor** den Edits und Prüfbuild
**nach** den Edits: je **0 Fehler, 6 Warnungen** — identisch die bekannten
Bestandswarnungen (`CS0108`×2, `CS0109`×2, `CS1998`, `CS4014`).

Parserregel: identisch zu `ZahlParsen`; das Verhalten ist mit der Messtabelle in
Abschnitt 1 belegt (Spalte `Program.ZahlParsen`).

Kodierung (vorher → nachher, byteweise geprüft):

| Datei | vorher | nachher |
|---|---|---|
| `Program.cs` | UTF-8 BOM, 20142 Bytes | UTF-8 BOM strikt, 20677 Bytes, +4 High-Bytes (zwei neue „ü"), CRLF |
| `Form_Heizkessel_einlesen.cs` | **cp1252** (6 High-Bytes, kein BOM), 16792 Bytes | unverändert cp1252, **weiterhin exakt 6 High-Bytes**, 18582 Bytes, CRLF — Bearbeitung deshalb byte-sicher über Encoding-1252-Ersetzung, Einfügungen rein ASCII |
| `Form_PufferSp_einlesen.cs` | UTF-8 BOM, 10428 Bytes | UTF-8 BOM strikt, 11075 Bytes, High-Bytes unverändert 5, CRLF |
| `Form_WP_einlesen.cs` | rein ASCII, kein BOM | **weiterhin 0 High-Bytes**, CRLF, Einfügungen ASCII („zaehlt", „Uebernahme") |
| `Form_AdminPV.cs` | UTF-8 BOM, 13084 Bytes | UTF-8 BOM strikt, 12574 Bytes, High-Bytes unverändert 11, CRLF |
| `Solarkollektorenlmport.cs` | UTF-8 BOM, 4540 Bytes | UTF-8 BOM strikt, 4703 Bytes, High-Bytes unverändert 27, CRLF |

Kein Lauf gegen eine echte `.vdi`-Datei (weiterhin keine Beispieldatei im Repo — die in
Abschnitt 6 verlinkten Portal-Datensätze wären dafür geeignet).

### 7.5 Nachtrag: Bereinigung (18.08.2026, zweiter Durchgang)

* **`convertTxt2Int` gehärtet** (`Program.cs`): akzeptiert jetzt zusätzlich
  Dezimalschreibweisen ganzer Zahlen — „35.0"/„35,0" → 35, „750.0" → 750 (Randnotiz 2;
  relevant für `textBox_Volumen` im Pufferspeicher-Dialog und die
  Vorlauf-/Temperatur-/Lastfelder des WP-Imports). Vertrag unverändert: leer oder nicht
  ganzzahlig parsbar → 0, kein Wurf. Nachgemessen: 35→35, 35.0→35, „35,0"→35, 750.0→750,
  35.5→0, −5.0→−5, abc→0, leer→0, 2147483648.0→0 (Überlauf), 1e2→100.
* **`Program.checkDouble` entfernt** — toter Code: kein einziger lebender Aufrufer mehr,
  nur noch Erwähnungen in Kommentaren der bereits auf `ZahlPruefen`/`ZahlFaerben`
  umgestellten Dialoge. Damit ist die Kulturinkonsistenz aus `WPPlan_Code_Befunde.md`
  (Punkte 5/28) an dieser Stelle gegenstandslos. **`checkInt` bleibt:** zwei lebende
  Aufrufer in `Form_Heizkessel.cs` (Vor-/Rücklauf, TextChanged-Muster mit Undo); für
  Ganzzahlen ist die Kulturfrage ohne praktische Wirkung. Der Sperren-Kommentar in
  `Program.cs` nennt nur noch `checkInt`.
* **`Form_AdminPV.listBox_PV_SelectedIndexChanged`:** auch die drei verbliebenen
  String-Zuweisungen ins Model entfernt — die Methode besetzt jetzt ausschließlich die
  Anzeige, das Model füllt allein `btn_Speichern_Click`.

Prüfbuild danach erneut **0 Fehler, 6 bekannte Warnungen** (Liste unverändert);
`Program.cs` und `Form_AdminPV.cs` weiterhin strikt UTF-8 mit BOM, CRLF. Offen bleiben
nur die zwei ersten Punkte in 7.3.
