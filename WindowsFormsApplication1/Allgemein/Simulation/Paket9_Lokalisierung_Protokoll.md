# Paket 9 „Lokalisierung" — Umsetzungsprotokoll

Grundlage: [`Konzept_Simulation_QuellenSenken.md`](Konzept_Simulation_QuellenSenken.md), Kapitel 13.6.
Ausgangsstand: Commit `19aa1ff` (Paket 8), Branch `main`.

---

# Etappe 1 — Teilpakete L0, L1, L2

**Stand: 15.08.2026.** Diese Etappe legt das Fundament und ändert **kein Laufzeitverhalten**.
Sie ist gegen die Referenzbasis `Referenzlaeufe/2026-08-14_B1-Fixes` vollständig
regressionsfrei — 208 von 208 Ergebnisdateien sind **byte-identisch**.

## 0. Ist-Befund — was die Auszählung gegenüber dem Konzept korrigiert

Das Konzept (Fassung 12) beschreibt einen Stand von vor Paket 4. Die Auszählung am heutigen
Code ergibt in drei Punkten deutlich andere Zahlen. Alle drei sind für die Planung der
Folge-Etappen wesentlich.

| Größe | Konzept 13.6 | **verifiziert 15.08.2026** | Bemerkung |
|---|---|---|---|
| Hartkodierte benutzersichtbare Texte | 287 Fundstellen | **610 Fundstellen** | +113 % |
| davon eindeutige Texte | 156 | **452** | +190 % |
| betroffene Dateien | 16 | **33** | |
| DB-Wert-Literale | „62 verstreute" | **44 eindeutige Werte**, ~177 Fundstellen | davon 13 Bodentyp-Schlüssel |
| `MyResource.Resource`-Schlüssel | 7 (`KONFIG_*`) | **17** (7 × `KONFIG_*` + 10 × `Text_*`) | die `Text_*` waren im Konzept nicht erfasst |

**Ursache des Zuwachses:** Die Pakete 2 bis 8 haben rund **305 Fundstellen / 285 eindeutige
Texte** hinzugefügt — im Wesentlichen die Erzeugerübersicht mit ihren neun Spalten und neun
Tooltips (Paket 2), die Erdreich-/VDI-4640-Auswertung (Paket 3), die Speicheranzeigen und
Speicher-Ergebnistabelle (Paket 7) und vor allem der **Protokollkanal `SimulationProtokoll`**
(Paket 8), der die früheren MessageBoxen der Engine durch 34 Protokollmeldungen ersetzt hat.
Der Aufwandsansatz für L2 (2,0 PT für 156 Schlüssel) trägt diesen Umfang nicht; die
Fortschreibung steht in Abschnitt 6.

### Weitere Befunde des Ist-Zustands

**Die Abdeckungszahlen der en-US-Satelliten im Konzept sind irreführend.** Das Konzept nennt
„65 von 298" bzw. „10 von 98". Diese Zahlen zählen *alle* `.resx`-Einträge, also überwiegend
Layout-Eigenschaften (`Location`, `Size`, `TabIndex`, `AutoSize`, `Anchor`, `Font` …), die in
einem Satelliten gar nicht stehen müssen. Zählt man nur die **übersetzbaren Text-Einträge**,
ist die Lücke minimal:

| Formular | Text-Einträge neutral | davon in en-US | tatsächlich fehlend |
|---|---|---|---|
| `Form_Simulation_Config` | 16 | 13 | **3** |
| `Form_KonfigPufferspeicher` | 7 | 6 | **1** |

**`MyResource\Resource.Designer.cs` ist eingecheckter Quelltext, kein Build-Erzeugnis.** Die
`.csproj` verknüpft `MyResource\Resource.resx` mit dem Entwurfszeit-Generator
`PublicResXFileCodeGenerator` (`LastGenOutput = Resource.Designer.cs`). Dieser Generator läuft
**ausschließlich in Visual Studio**, nicht im MSBuild-Lauf. Wer einen Schlüssel ergänzt, muss
die zugehörige Eigenschaft also **mitpflegen** — sonst kompiliert der Zugriff nicht.

> **Beobachtet während dieser Etappe:** Auf dem Arbeitsplatz liefen drei Visual-Studio-Instanzen.
> Nach der Änderung an `Resource.en-US.resx` hat Visual Studio `Resource.Designer.cs`
> selbsttätig neu erzeugt und dabei die Kommentare von Englisch auf Deutsch umgestellt
> (29 Zeilen, **ausschließlich Kommentare**; alle 17 Eigenschaften unverändert, nachgewiesen
> per Testtreiber). Das ist harmlos, zeigt aber: bei offener IDE ändern sich Dateien
> nebenher. Für L2 wurden die Designer-Eigenschaften deshalb **erzeugt und eingecheckt**, damit
> der Build unabhängig davon korrekt ist, ob Visual Studio den Generator gerade laufen lässt.

**Der `Resource.en-US.resx` enthielt einen doppelten Schlüssel.** `KONFIG_STROMSPEICHER` stand
zweimal darin: einmal als leerer `ResXNullRef`, einmal mit dem Wert `Electricity storage`.
Die Warnung MSB3568 ist projektweit über `NoWarn` unterdrückt, der Fehler blieb deshalb
unsichtbar. Bereinigt (siehe L1).

---

## 1. L0.1 — Encoding vereinheitlicht

Ziel: im Simulationsbereich ausschließlich **UTF-8 mit BOM**. Die Signatur macht die Kodierung
für Visual Studio, MSBuild, git und Kommandozeilenwerkzeuge eindeutig; ohne sie lesen Werkzeuge
ohne explizite Angabe die Datei als ANSI und zerstören die Umlaute — genau so sind die
Schäden im Bestand entstanden.

### Gruppe A — Windows-1252 ohne BOM → UTF-8 mit BOM (echte Umkodierung)

Hier ändert sich die **Byte-Darstellung der Umlaute**. 8 Dateien:

| Datei | Umlaut-Zeilen | vorher | nachher |
|---|---:|---|---|
| `Views/Pufferspeicher/Form_PufferSp_Bearbeiten.designer.cs` | 3 | Windows-1252 | UTF-8 BOM |
| `Views/Pufferspeicher/Form_PufferSp_einlesen.cs` | 1 | Windows-1252 | UTF-8 BOM |
| `Views/Pufferspeicher/Form_PufferSp_einlesen.designer.cs` | 3 | Windows-1252 | UTF-8 BOM |
| `Controller/BHKWCtrl.cs` | 14 | Windows-1252 | UTF-8 BOM |
| `Controller/BrauchwasserCtrl.cs` | 7 | Windows-1252 | UTF-8 BOM |
| `Controller/StromverbraucherCtrl .cs` | 7 | Windows-1252 | UTF-8 BOM |
| `Controller/Z_ProjektStromganglinieCtrl.cs` | 4 | Windows-1252 | UTF-8 BOM |
| `Controller/Z_ProjektStromverbraucherCtrl.cs` | 4 | Windows-1252 | UTF-8 BOM |

Die fünf Controller sind **nicht beliebig gewählt**: Es sind genau die Nicht-UTF-8-Controller,
die aus dem Simulationsbereich heraus benutzt werden (`SimulationBHKW` → `BHKWCtrl`,
`SimulationWaermebedarf` → `BrauchwasserCtrl`, `SimulationStrombedarf` →
`StromverbraucherCtrl`/`Z_ProjektStromganglinieCtrl`/`Z_ProjektStromverbraucherCtrl`).

### Gruppe B — UTF-8 ohne BOM → UTF-8 mit BOM (nur Signatur ergänzt)

Kein Zeichen ändert sich, es kommen drei Bytes an den Dateianfang. 12 Dateien:

`Allgemein/Simulation/`: `Ladeordnung.cs`, `SimulationProtokoll.cs`, `SimulationPufferspeicher.cs`,
`StilleDb.cs`, `WaermequelleClass.cs`, `WaermesenkeClass.cs` ·
`Views/Pufferspeicher/`: `Form_PufferSp_Projekt.cs` ·
`Views/Simulation/`: `Form_QuellePufferspeicher.cs`, `Form_Quellprofil.cs`,
`Form_Simulation_Config.Uebersicht.cs`, `Form_Waermesenke.cs`, `TabListMapper.cs`

Diese Gruppe geht über den engen Wortlaut des Auftrags („die NICHT UTF-8 sind") hinaus. Sie
wurde bewusst mitgenommen, weil die neue `.editorconfig` UTF-8 **mit BOM** vorschreibt und
diese zwölf Dateien sonst beim nächsten Speichern in Visual Studio unkontrolliert und
unprotokolliert nachgezogen worden wären.

### Nachweis der Zeichengleichheit

Für jede der 20 Dateien wurde die HEAD-Fassung aus dem git-Objektspeicher geholt, mit der
jeweiligen Quellkodierung dekodiert und **zeichenweise** gegen die neue Fassung verglichen
(nach Normierung der Zeilenenden, weil `.gitattributes` mit `* text=auto` im Objektspeicher LF
ablegt). Ergebnis:

```
20 von 20 Dateien ZEICHENGLEICH, 0 abweichende Zeichenzeilen
```

`git diff` zeigt entsprechend nur die Umlaut-Zeilen plus die BOM-Zeile. Kein Mojibake: Aus
`0xFC` wurde `ü`, aus `0xDC` wurde `Ü` — geprüft an allen betroffenen Zeilen.

### Zeilenenden — ein Befund, der nicht behoben wurde

Beim Konvertieren fiel auf, dass **9 der 20 Dateien im Arbeitsverzeichnis reine LF-Zeilenenden
tragen**, nicht CRLF: `Ladeordnung.cs`, `StilleDb.cs`, `WaermequelleClass.cs`,
`WaermesenkeClass.cs`, `Form_PufferSp_Projekt.cs`, `Form_QuellePufferspeicher.cs`,
`Form_Quellprofil.cs`, `Form_Waermesenke.cs`, `TabListMapper.cs` — durchweg Dateien aus den
Paketen 2 bis 8.

Die Zeilenenden wurden **absichtlich nicht** umgestellt. Eine Umstellung auf CRLF würde jede
Zeile jeder dieser Dateien im Diff verändern und damit den Nachweis „nur Umlaut-Zeilen haben
sich geändert" unmöglich machen. git meldet beim nächsten Zugriff ohnehin
`LF will be replaced by CRLF` und normalisiert selbst; die neue `.editorconfig` sorgt dafür,
dass Visual Studio es beim nächsten Speichern ebenfalls tut. **Offener Punkt für L8.**

### `.editorconfig`

Neu angelegt in der **Repo-Wurzel** `C:\Waermeplan\WP_Plan\.editorconfig` (`root = true`):
`*.cs`, `*.resx` und die Projektdateien auf `charset = utf-8-bom` und `end_of_line = crlf`,
Markdown auf `utf-8` ohne BOM (BOM stört in Web-Ansichten), JSON/YAML auf `utf-8`.

> **Bewusst kein globales `charset` unter `[*]`.** Eine solche Vorgabe hätte auch für
> Datendateien gegolten — insbesondere für die CSV-Dateien unter `Referenzlaeufe/`, die die
> Regressionsbasis des Simulationskerns bilden. Öffnet und speichert jemand eine davon in
> Visual Studio, bekäme sie eine BOM, und der Byte-Vergleich der Referenzlauf-Suite
> (208 Dateien, MD5) schlüge fehl — ohne dass sich ein einziger Zahlenwert geändert hätte.
> Die Kodierung wird deshalb ausschließlich für Quell- und Projektdateien festgelegt; ein
> Kommentar in der Datei hält den Grund fest.

### Nicht konvertiert — Restbestand

**Gesperrte Dateien** (unkommittierte Nutzerarbeit, laut Auftrag unantastbar):
`Controller/WizardCtrl.cs`, `Model/WErzeugerModel.cs`, `Views/BHKW/Form_BHKWEing.cs`,
`Views/Heizkessel/Form_Heizkessel.cs`, `Views/Wizard/WizardParent.cs`.
Alle fünf sind bereits UTF-8 mit BOM — sie wären ohnehin keine Kandidaten gewesen. Kein
Handlungsbedarf, auch nicht später.

**Außerhalb des Simulationsbereichs** verbleiben im Ordner `Controller/` weitere Dateien mit
abweichender Kodierung. Keine davon wird aus dem Simulationsbereich heraus benutzt:

| Datei | Kodierung |
|---|---|
| `Controller/BHKWStammCtrl.cs` | Windows-1252 |
| `Controller/KlimaregionStammCtrl.cs` | Windows-1252 |
| `Controller/StromganglinieStammCtrl.cs` | Windows-1252 |
| `Controller/Z_ProjektSolarganglinieCtrl.cs` | Windows-1252 |
| `Controller/BerichtCtrl.cs` | UTF-8 ohne BOM |
| `Controller/ProjektDuplizierenCtrl.cs` | UTF-8 ohne BOM |
| `Controller/VariantenCtrl.cs` | UTF-8 ohne BOM |
| `Controller/WaermebedarfExternKontextMenuCtrl.cs` | UTF-8 ohne BOM |

Ebenfalls außerhalb: **`Allgemein/BhkwPlan.cs`** (UTF-8 ohne BOM) — der Rechenkern. Er liegt
in `Allgemein/`, nicht in `Allgemein/Simulation/`, und war damit nicht Teil des beauftragten
Bereichs. Da die Datei gültiges UTF-8 ist, besteht kein akutes Risiko; sie ist aber der
naheliegendste nächste Kandidat, weil sie fachlich zum Simulationskern gehört.

Laut `CLAUDE.md` sind projektweit 93 von 372 `.cs`-Dateien nicht UTF-8. Eine Bereinigung des
Gesamtbestands ist ein eigenes Vorhaben; die `.editorconfig` liegt dafür jetzt bereit.

**Ergebnis:** `Allgemein/Simulation`, `Views/Simulation` und `Views/Pufferspeicher` sind
**vollständig UTF-8 mit BOM** — verifiziert nach dem Umbau.

---

## 2. L0.2 — `Allgemein/DbWerte.cs`

Neue Klasse `WindowsFormsApplication1.DbWerte` mit **51 `public const string`** — jeder Wert,
der als Zeichenkette in `Kenndaten.accdb` steht oder gegen sie verglichen wird. Jede Gruppe
trägt den Kommentar „Persistenzwert, immer deutsch, eingefroren (Drei-Schichten-Regel)" samt
Angabe der Spalte, in der sie steht.

| Gruppe | DB-Feld | Konstanten |
|---|---|---:|
| Erzeugerart | `Tab_Einstellungen.Tool_1..6`, `Z_ProjektPufferSp.Erzeuger` | 8 |
| Wärmesenken-Ziel | `Tab_Energieanlagen.WS_Ziel`, `.WS_Ziel2` | 3 |
| Wärmesenken-Typ | `Tab_Energieanlagen.WS_Typ` | 3 |
| Wärmequellen-Typ | `Tab_Energieanlagen.WQ_Typ` | 6 |
| Erdreich-Quellsystem | `Tab_Energieanlagen.WQ_Quellsystem` | 2 |
| Bodentyp (VDI 4640 Bl. 1) | `Tab_Energieanlagen.WQ_Bodentyp` | 13 |
| Pufferspeicher-Verwendung | `Tab_Pufferspeicher.Verwendung` | 3 |
| Pufferspeicher-Speichertyp + Bezeichner | `Tab_Pufferspeicher.Speichertyp`, `.Bezeichner` | 4 |
| WP-Bauart | `Tab_WP.Typ` | 3 |
| WP-Betriebsart | `Tab_Energieanlagen.Betriebsart` | 3 |
| Betriebsmodus | `Tab_Energieanlagen.BM_Typ` | 3 |
| | **Summe** | **51** |

### Konsolidiert statt dupliziert

Der Auftrag warnt zu Recht vor einer zweiten Wahrheit. Die Auszählung hat gezeigt, dass es
**schon vorher mehrere Wahrheiten gab**: `"Heizung"` war an drei Stellen unabhängig als
Konstante definiert (`ProjektPuffer`, `WaermequelleClass`, `SimulationPufferspeicher`),
`"Brauchwasser"` ebenfalls dreimal, `"PufferHeizung"` und `"Heizkreis"` je zweimal.

Deshalb wurden die vorhandenen Konstanten **nicht ersetzt, sondern auf `DbWerte` umgehängt**.
Sie bleiben als `const string`-Aliasse bestehen — alle bisherigen Aufrufstellen funktionieren
unverändert weiter, aber sie definieren nichts mehr selbst:

| Klasse | umgehängte Konstanten |
|---|---:|
| `WaermequelleClass` | 12 (`MODUS_*`, `SENKE_*`, `TYP_*`) |
| `WaermesenkeClass` | 5 (`ZIEL_*`, `VERWENDUNG_*`) |
| `SimulationPufferspeicher` | 3 (`VERWENDUNG_*`) |
| `ErdreichTemperatur` | 3 (`QUELLSYSTEM_*`, `BODENTYP_DEFAULT`) |
| `ProjektPuffer` | 6 (`BEZ_*`, `VERWENDUNG_*`, `ERZEUGER_*`, `WS_ZIEL_*`, `SPEICHERTYP_*`) |
| | **29** |

`SchemaMigration` verweist bereits auf `ProjektPuffer` und erbt die Umstellung damit
automatisch.

**Bewusst nicht zusammengelegt:** die `int`-Konstanten `WizardItemClass.WP_TYP…PUFFER_TYP` und
`ProjektPuffer.TYP_WP…TYP_PUFFER`. Sie sind wertgleich, aber der Kommentar in `ProjektPuffer`
begründet die Trennung ausdrücklich („damit weder Migration noch Controller von der UI-Schicht
abhängen"). Das ist eine Architekturentscheidung, keine Nachlässigkeit — und `DbWerte` führt
ohnehin nur Zeichenketten.

### Ersetzte Literale — nur Engine, wie beauftragt

**104 Codestellen** in den Engine-/Simulationsdateien wurden umgestellt, alle rein mechanisch
und wertidentisch:

| Datei | Stellen | Art |
|---|---:|---|
| `SimulationControl.cs` | 28 | Erzeugervergleiche `tool[i] == …`, `KaskadeEnthaelt(…)` |
| `WaermequelleClass.cs` | 12 + 2 + 3 | Alias-Definitionen, `wpTyp == "Luft-Wasser"`, **3 SQL-Literale** `Erzeuger='Wärmepumpe'` |
| `VDI4640Pruefung.cs` | 18 | `case`-Marken der Bodentyp-Zuordnung (`const string` ist als `case`-Marke zulässig) |
| `ErdreichTemperatur.cs` | 13 + 3 | Katalogschlüssel in `Katalog[]`, Alias-Definitionen |
| `SimulationWaermepumpe.cs` | 6 | `model.Betriebsart == …` |
| `Ladeordnung.cs` | 4 | **nur `KaskadenLiteral`** |
| `WaermesenkeClass.cs` | 5 | Alias-Definitionen |
| `ProjektPuffer.cs` | 6 | Alias-Definitionen |
| `SimulationPufferspeicher.cs` | 3 | Alias-Definitionen |
| `ErdreichAuswertung.cs` | 1 | `string.Equals(typ, "Luft-Wasser", OrdinalIgnoreCase)` |

Die drei SQL-Stellen sind die im Konzept ausdrücklich genannten
(`… AND Erzeuger='Wärmepumpe' ORDER BY Prioritaet`). Sie lauten jetzt
`" AND Erzeuger='" + DbWerte.ERZEUGER_WAERMEPUMPE + "' ORDER BY Prioritaet"` — dieselbe
erzeugte Abfrage, aber die Zeichenkette steht nur noch einmal im Projekt.

### Bewusst NICHT ersetzt

Diese Stellen sehen wie DB-Werte aus, sind aber keine. Sie zu ersetzen wäre falsch:

| Stelle | Grund |
|---|---|
| `Ladeordnung.ErzeugerName` (4×) | **Anzeigename**, steht direkt über `KaskadenLiteral` mit identischen Zeichenketten. Wandert nach L2 in den Ressourcenkatalog. |
| `SimulationControl` `SenkeAufHeizkreisZurueck(…, "Wärmepumpe")` (4×) | Der Parameter `art` landet ausschließlich in einer Protokollzeile — Anzeige, kein DB-Wert. |
| `SimulationControl:1309/1992`, `WaermequelleClass:613` (`sp.Erzeuger = …`) | `SimulationPufferspeicher.Erzeuger` wird **nirgends persistiert**; `Tab_ErgebnisPufferspeicher` hat keine solche Spalte. Reines In-Memory-Etikett. |
| `SimulationWaermepumpe:248` `rs.Read("Heizung")` | `"Heizung"` ist hier ein **Spaltenname** in `Tab_WP`, kein Datenwert. |
| `VDI4640Pruefung.BODENART_*` (`"Sand"`, `"Lehm"`, `"Schluff"`, `"Sandiger Ton"`) | Interne Zeilenschlüssel der VDI-4640-Tabelle, aus `WQ_Bodentyp` abgeleitet — stehen nie in der Datenbank. |
| Selbsttestblöcke unter `#if DEBUG` in `VDI4640Pruefung.cs` und `ErdreichTemperatur.cs` | Testdaten, kein Produktivpfad. |
| Formulare (`Views/…`) | Laut Auftrag **Etappe 2**. Rund 47 Fundstellen, Liste in Abschnitt 6. |

### Zwei Befunde, die dabei aufgefallen sind

**Befund L0-1 — `Speichertyp` wird lokalisiert in die Datenbank geschrieben.**
`Form_PufferSp_Bearbeiten.cs:139` schreibt `model.Speichertyp = comboBox_Speichertyp.Text`.
Die ComboBox-Einträge stammen aus der `.resx` des Formulars. Auf englischer Oberfläche landen
damit `"Solar storage"`, `"Buffer storage"`, `"Combination storage"` in
`Tab_Pufferspeicher.Speichertyp` — statt `"Solarspeicher"`, `"Pufferspeicher"`,
`"Kombispeicher"`. Das ist derselbe Fehlertyp wie B0-9 bis B0-11 und verletzt die
Drei-Schichten-Regel. Die Konstanten `DbWerte.PSP_SPEICHERTYP_*` stehen bereit; **Behebung
gehört zu L5.**

**Befund L0-2 — `Form_PufferSp_Projekt.cs:157-160` zeigt DB-Werte als Auswahltext.**
Die Verwendungs-ComboBox füllt sich direkt aus `WaermesenkeClass.VERWENDUNG_HEIZUNG` /
`…_BRAUCHWASSER` und liest über `SelectedItem.ToString()` zurück. Die Konstante ist an dieser
Stelle korrekt, aber der angezeigte Text ist nicht lokalisierbar. Der Katalog führt dafür
eigene Anzeigeschlüssel; **Umbau in Etappe 2.**

---

## 3. L0.3 — Glossar DE → EN

Neu: [`Glossar_Lokalisierung.md`](Glossar_Lokalisierung.md).

Rund **200 Fachbegriffe** in zwölf Abschnitten: Anlagentechnik, Wärmebedarf, Quellen und
Senken, Hydraulik und Temperaturen, Speicher, Betrieb und Kennzahlen, Zeit und Profile,
Oberfläche, Engine-Meldungen, dazu die Negativliste der Persistenzwerte, die Schreibregeln und
die Bestandsübersetzungen mit Vorrang.

Terminologiequellen in dieser Reihenfolge: EN 12831 / EN 15316 / EN 14511, EN 12977 /
EN 15316-4-3, VDI 4640, VDI 4655, EN 50524 / EN 61724.

Festgehaltene Entscheidungen, bei denen die naheliegende Übersetzung falsch wäre:

| DE | EN | statt |
|---|---|---|
| Heizkessel | boiler | „heating kettle" |
| Pufferspeicher | buffer storage | „buffer memory" (siehe unten), „buffer tank" |
| Vorlauftemperatur | flow temperature | „supply temperature" |
| Außenluft | outdoor air | „ambient air" (= Umgebungsluft) |
| Erdreich | ground | „soil" (bodenkundlicher Begriff) |
| Ganglinie | load profile | „curve" |
| Vollbenutzungsstunden | full-load hours | „operating hours" (= Betriebsstunden) |
| Nennleistung | rated output | „nominal power" |
| Einstrahlung / Globalstrahlung | irradiance / global irradiation | die beiden nicht vertauschen (Leistung vs. Energie) |
| Strombedarf | electricity demand | „power demand" (power = Leistung) |

**Bestandsübersetzungen mit Vorrang:** Die sieben ausgelieferten `KONFIG_*`-Werte bleiben
unverändert, auch wo das Glossar anders formuliert (z. B. `KONFIG_BHKW` = „CHP" statt
„CHP unit"). Sie sind in Kapitel 12 des Glossars mit ihrer Abweichung dokumentiert. Für neue
Schlüssel gilt ausschließlich das Glossar.

---

## 4. L1 — Satelliten bereinigt und vervollständigt

### 4.1 Gelöscht

| Datei | Begründung |
|---|---|
| `MyResource/Resource.de-DE.resx` | Vollständig redundant. Alle 17 Schlüssel **zeichengleich** zur neutralen Datei — programmatisch verglichen, 0 Abweichungen. Deutsch ist die Fallback-Kultur. |
| `MyResource/Resource.en-US.Designer.cs` | 0 Byte. Satellitendateien dürfen keinen Code-Generator haben. |
| `Views/Simulation/Form_Simulation_Config.de-DE.resx` | Enthielt **nur zwei Layout-Einträge** für `btn_Strom_Simu_Start` (`Location` 333,27 / `Size` 136,30). |
| `Views/Simulation/Form_KonfigPufferspeicher.de-DE.resx` | Vier Einträge: `label1`/`label2` je `Size` und `Text`. |

**Zur Frage, ob die Layout-Einträge verhaltensrelevant waren** — sie waren es nicht:

- **`btn_Strom_Simu_Start` existiert nicht mehr.** Die Suche über alle `.cs`-Dateien des
  Projekts liefert null Treffer; auch die neutrale `.resx` und der Designer kennen das
  Steuerelement nicht. Die Einträge in der de-DE- **und** der en-US-Datei sind toter Bestand.
  Es entfällt also nichts.
- **`Form_KonfigPufferspeicher`**: Hier lag ein echter Unterschied vor. Die de-DE-Datei setzte
  `label1.Text` = „Erzeuger auswählen**:**" und `label2.Text` = „Pufferspeicher auswählen**:**"
  — jeweils **mit Doppelpunkt** —, die neutrale Datei dagegen ohne. Da `Program.cs`
  `CurrentUICulture` auf exakt `de-DE` setzt, wurde bisher die Fassung **mit** Doppelpunkt
  angezeigt. Damit die deutsche Oberfläche unverändert bleibt, wurden **die beiden Texte mit
  Doppelpunkt in die neutrale `.resx` übernommen**. Die ebenfalls abweichenden `Size`-Werte
  (127,17 statt 124,17 bzw. 158,17 statt 155,17) wurden **nicht** übernommen: beide Labels
  stehen auf `AutoSize = True`, die Größe wird zur Laufzeit ohnehin neu berechnet.
  Die englischen Texte bleiben unverändert ohne Doppelpunkt — das entspricht exakt dem
  bisherigen Verhalten.

### 4.2 `.csproj`

Der Generator-Eintrag für `MyResource\Resource.en-US.resx` (Verweis auf die gelöschte 0-Byte-Datei)
wurde entfernt. Nur die **neutrale** Datei trägt noch einen Generator. Ein Kommentar an dieser
Stelle hält fest, dass `Resource.Designer.cs` eingecheckter Quelltext ist und beim Ergänzen von
Schlüsseln mitgepflegt werden muss.

### 4.3 Doppelter Schlüssel behoben

`MyResource/Resource.en-US.resx`: Der leere `ResXNullRef`-Eintrag für `KONFIG_STROMSPEICHER`
wurde entfernt, ebenso die nur dafür nötige `<assembly alias="System.Windows.Forms" …>`-Zeile.
Bleibt der Eintrag mit dem Wert `Electricity storage`.

### 4.4 en-US-Satelliten vervollständigt

Ergänzt wurden **ausschließlich additive String-Einträge**, kein einziger Layout-Schlüssel.
Kein englischer Text erzwang eine Größenanpassung.

| Datei | Schlüssel | DE | EN |
|---|---|---|---|
| `Form_Simulation_Config.en-US.resx` | `btn_Hinzu.Text` | Hinzufügen... | Add... |
| | `groupBox_Tools.Text` | Erzeuger && Speicher | Generators && storage |
| | `lblStatus.Text` | ✔ Konfiguration erfolgreich gespeichert | ✔ Configuration saved successfully |
| `Form_KonfigPufferspeicher.en-US.resx` | `btn_OK.Text` | OK | OK |

Das doppelte `&&` ist die WinForms-Maskierung für ein angezeigtes `&` und wurde übernommen
(im XML als `&amp;&amp;`).

### 4.5 Die 7 `KONFIG_*`-Schlüssel — Konsistenzprüfung

Alle sieben lösen unter **de-DE** (über den neutralen Rückfall, nachdem der de-DE-Satellit
entfernt wurde) und unter **en-US** korrekt auf — mit dem Testtreiber nachgewiesen:

| Schlüssel | de-DE | en-US |
|---|---|---|
| `KONFIG_BHKW` | BHKW | CHP |
| `KONFIG_HEIZKESSEL` | Heizkessel | Boiler |
| `KONFIG_SOLARTHERMIE` | Solarthermie | Solar thermal energy |
| `KONFIG_WAERMEPUMPE` | Wärmepumpe | Heat pump |
| `KONFIG_PHOTOVOLTAIK` | Photovoltaik | Photovoltaics |
| `KONFIG_STROMSPEICHER` | Stromspeicher | Electricity storage |
| `KONFIG_GESAMTSYSTEM` | Gesamtsystem | Overall system |

Die deutschen Werte sind **zeichengleich mit den `DbWerte`-Persistenzwerten** — das ist der
Grund, warum das Rückwärts-Mapping in `Form_Simulation_Config.cs:1043-1045` (erst `DisplayName`,
dann `DbValue`) auf deutscher Oberfläche funktioniert und auf englischer nicht (Bestandsfehler
B0-11). Beim Umbau in L4 muss die Reihenfolge der beiden Vergleiche erhalten bleiben.

### 4.6 Zusätzlicher Befund: 43 verwaiste Einträge in `Form_Simulation_Config.en-US.resx`

Der en-US-Satellit enthält 43 Einträge für **16 Steuerelemente, die es nicht mehr gibt**
(`groupBox1`–`groupBox6`, `label5`, `label10`, `label13`, `label15`, `label17`, `label18`,
`label20`, `label57`, `checkBox_Heizstab`, `btn_Strom_Simu_Start`). Für jedes wurde geprüft:
**null Treffer** in `Form_Simulation_Config.cs`, `.Designer.cs` und `.Uebersicht.cs`.

Sie sind wirkungslos, weil `ApplyResources` für diese Namen nie aufgerufen wird. Entfernt
wurden sie **nicht** — der Auftrag für L1 war ausdrücklich additiv. **Aufräumkandidat für
Etappe 2.**

---

## 5. L2 — Ressourcenkatalog

### 5.1 Umfang

Der Katalog wurde aus fünf Teilerhebungen zusammengeführt, jede über einen abgegrenzten
Dateisatz, damit die Texte **direkt aus dem Quelltext** übernommen werden konnten und nicht
aus einer Zwischenliste:

| Teil | Dateisatz | Zeilen |
|---|---|---:|
| A | `Allgemein/Simulation/` (15 Dateien, Engine) | 107 |
| B | `Views/Simulation/`: `Form_Simulation_Detail`, `Navigator*`, `DashboardForm`, `TabNavigationManager`, `TabListMapper` | 141 |
| C | `Views/Simulation/`: `Form_Simulation_Config` (+ `.Uebersicht`), `Form_KonfigPufferspeicher`, `Form_Waermesenke` | 139 |
| D | `Views/Simulation/`: `Form_QuelleErdreich`, `Form_Quellprofil`, `Form_QuellePufferspeicher` | 73 |
| E | `Views/Pufferspeicher/` (5 Dateien) | 87 |
| | **Rohzeilen** | **547** |

Nach Zusammenführung gleicher deutscher Texte — 33 Zeilen entfielen, 26 Texte kamen unter
mehreren Schlüsseln vor:

| Kategorie | Schlüssel | Inhalt |
|---|---:|---|
| `SIM_*` | 169 | Simulation allgemein: Dialoge, Spaltenköpfe, Meldungen, Übersicht, Senken |
| `SIMQ_*` | 138 | Wärmequellen: Erdreich/VDI 4640, Quellprofil, Quellspeicher, Quelltemperatur |
| `PSP_*` | 123 | Pufferspeicher: Verwaltung, Projektzuordnung, Speicherregelung, Schwellen |
| `CHART_*` | 52 | Diagrammtitel, Achsen, Legenden, CSV-Kopfzeilen, Dateinamensvorschläge |
| `SIMENG_*` | 29 | Engine- und Protokollmeldungen (`SimulationProtokoll`, Paket 8) |
| | **511** | |

**Bestand im Katalog nach L2: 17 + 511 = 528 Schlüssel** in `MyResource/Resource.resx`
(neutral = deutsch) und `MyResource/Resource.en-US.resx` (englisch).

### 5.2 Designer-Eigenschaften

`MyResource/Resource.Designer.cs` wurde **vollständig neu erzeugt** — im Format, das der
`StronglyTypedResourceBuilder` selbst schreibt, alphabetisch sortiert, mit Kurzvorschau des
deutschen Werts im XML-Kommentar. **528 Eigenschaften**, geprüft gegen die Schlüsselmenge der
kompilierten Ressource: keine Eigenschaft ohne Schlüssel, kein Schlüssel ohne Eigenschaft.

Das war nötig, weil der Generator nur zur Entwurfszeit in Visual Studio läuft (siehe
Abschnitt 0). Der Build ist damit unabhängig davon korrekt, ob die IDE den Generator
angeworfen hat.

### 5.3 Was nicht in den Katalog kam — und warum

Die Auszählung fand 452 eindeutige Texte, der Katalog führt 511 Schlüssel. Die Differenz
erklärt sich dadurch, dass die Teilerhebungen feiner unterschieden haben (z. B. Tooltips, die
in der Grobzählung unter einem Eintrag liefen) — **und** dass fünf Gruppen bewusst
ausgeschlossen wurden:

| Ausgeschlossen | Begründung |
|---|---|
| **DB-Persistenzwerte** | Stehen in `DbWerte.cs` und bleiben deutsch (Drei-Schichten-Regel). Wo derselbe Wortlaut zugleich Anzeige ist — `Ladeordnung.ErzeugerName`, `WaermesenkeClass.ZielAnzeige`, die Verwendungs-ComboBox aus Befund L0-2 — steht er **sehr wohl** im Katalog, mit eigenem Schlüssel und der Fundstelle der Anzeigestelle. |
| **Monats- und Wochentagsnamen** | 31 Texte (`"Jan"`…`"Dez"`, `"Januar"`…`"Dezember"`, `"Montag"`…`"Sonntag"`). Sie kommen laut Konzept in **L3 über `CultureInfo`** — eine Ressource dafür wäre die falsche Lösung. |
| **Reine Einheiten und Symbole** | `"°C"`, `"%"`, `"kW"`, `"MWh/a"`, `"W/m"`, `"W/m²"`, `"kWh/(m²·a)"`, `"#"`, `"-"`, `"✔"`, `"→"`, `"•"`, `"📂"`. Sprachneutral. Einheiten **innerhalb** eines Satzes sind natürlich Teil des Textes. |
| **Chart-Serien*namen*, die als Zugriffsschlüssel dienen** | `Series["Gesamt"]`, `"Waermepumpe"` (ohne Umlaut!), `"Waermebedarf"`, `"PV"`, `"Profil/Lastgang"`, `"Direktverbrauch"`, `"Speichernutzung"`, `"Lücke (Netzbezug)"`. Sie sind Schicht 2 der Drei-Schichten-Regel. Der zugehörige **`LegendText`** steht dagegen im Katalog. Die Trennung Serienname/Legendentext ist Aufgabe von **L6**. |
| **`Console.WriteLine`, `throw new …Exception`, `#if DEBUG`** | Rund 69 Konsolenzeilen, 6 Ausnahmetexte, ~70 Selbsttesttexte. Die Konsolenzeilen sind laut Konzept 13.4 ausdrücklich erwünscht und werden von der Referenzlauf-Suite mitgelesen. |

Ebenfalls nicht aufgenommen: die Kontexttexte für `HilfeKontext.SetzeBereich(...)` (erreichen
den Anwender nur mittelbar über die KI-Hilfe) und Spaltenköpfe von ListViews mit
`HeaderStyle = None` (unsichtbar; im Code je Fall geprüft).

### 5.4 Wortlaut-Kontrolle

Für jeden Katalogeintrag wurde geprüft, ob der deutsche Text so im Quelltext auffindbar ist:

```
389 von 511 woertlich im Quelltext auffindbar
122 mit Platzhalter oder aus Teilstuecken zusammengesetzt
```

> **Wichtige Einschränkung für Etappe 2.** Die 122 zusammengesetzten Texte tragen im Katalog
> normalisierte Platzhalter `{0}`, `{1}` … — die **Formatangaben des Quelltexts**
> (`{0:N0}`, `{0:0.0}`, `{0:F1}`, die Ausrichtung `{0,-18}`) sind dabei **verlorengegangen**.
> Wer beim Umbau den Ressourcenwert unverändert in `string.Format` einsetzt, ändert die
> Zahlendarstellung. Die Formatangabe ist deshalb an jeder Fundstelle aus dem Quelltext zu
> übernehmen. Der Lesehinweis steht auch im Kopf von `Lokalisierung_Katalog.md`.

### 5.5 Übersetzungsentscheidungen

Alle englischen Werte folgen [`Glossar_Lokalisierung.md`](Glossar_Lokalisierung.md). Fälle, in
denen das Glossar keine Entsprechung führte und eine Festlegung nötig war:

| DE | gewählt EN | Bemerkung |
|---|---|---|
| Heizstab | immersion heater | EN 15316 kennt auch „electric back-up heater" |
| Netzverluste | network losses | EN 15316 legte „distribution losses" nahe; „network" passt zum Wärmenetz |
| Kennlinie | characteristic curve | Glossar führt nur „Kennfeld = performance map" |
| Kenndaten | performance data | dito |
| laufzeit-/leistungs-/PV-optimiert | runtime-/output-/PV-optimised | britische Schreibweise, analog Glossar „utilisation" |
| Entzugsarbeit | extracted energy | Glossar führt nur „Entzugsleistung = extraction rate" |
| Geschiebemergel/-lehm | glacial till/boulder clay | nach VDI 4640 Bl. 1, Tabelle 1 |
| Sonde (Erdsonde) | BHE | Kürzel aus dem Glossar; „borehole heat exchanger" ist als Feldbeschriftung zu lang |
| Verwendung | use | ohne Glossardeckung, projektweit einheitlich |
| Anlage | unit | ohne Glossardeckung |
| Prio / WP-Prio | prio / HP prio | Kürzel beibehalten — feste Spaltenbreiten (40 bzw. 62 px) |
| Jahresgang (Temperatur) | annual variation | bewusst **nicht** „load profile" — es geht um Temperatur, nicht um Last |

**Ein inhaltlicher Vorbehalt, der in Etappe 2 zu klären ist:** Mehrere Meldungen setzen
DB-Werte als Platzhalter ein — etwa „Der Pufferspeicher „{0}" hat die Verwendung „{1}", die
{2} verlangt aber „{3}"." Die Platzhalterwerte (`Heizung`, `Brauchwasser`) bleiben nach der
Drei-Schichten-Regel deutsch. Die englische Meldung **mischt damit die Sprachen**. Sauber wäre
eine Anzeigefunktion, die den DB-Wert vor dem Einsetzen über den Katalog übersetzt
(`PSP_VERWENDUNG_HEIZUNG_ANZEIGE`, `PSP_VERWENDUNG_BRAUCHWASSER_ANZEIGE` liegen dafür bereit).

### 5.6 Ergebnis

`Lokalisierung_Katalog.md` (604 Zeilen) liegt neben dem Glossar. Je Kategorie eine Tabelle
`Schlüssel | DE | EN | Fundstellen`, dazu die Tabelle der 33 zusammengeführten Schlüssel.
Etappe 2 kann damit mechanisch arbeiten: Fundstelle aufschlagen, Zeichenkette durch
`MyResource.Resource.<Schlüssel>` ersetzen, Formatangabe übernehmen.

---

## 6. Übergabe an Etappe 2

### 6.1 Was bereitliegt

| Artefakt | Zweck |
|---|---|
| [`Lokalisierung_Katalog.md`](Lokalisierung_Katalog.md) | 511 Schlüssel mit DE, EN und **allen** Fundstellen — die Arbeitsliste |
| [`Glossar_Lokalisierung.md`](Glossar_Lokalisierung.md) | verbindliche Terminologie für alles, was noch dazukommt |
| [`../DbWerte.cs`](../DbWerte.cs) | 51 Persistenzwerte; die Negativliste zum Katalog |
| `MyResource/Resource.resx` / `.en-US.resx` | 528 Schlüssel, beide Sprachen gepflegt |
| `MyResource/Resource.Designer.cs` | 528 typisierte Eigenschaften, eingecheckt |

### 6.2 Offene Punkte, nach Dringlichkeit

1. **Aufwandsansatz fortschreiben.** L2 war mit 2,0 PT für 156 Schlüssel angesetzt; es sind
   511 geworden. Die Folgepakete L3 bis L6 beziffern ihre Textmengen ebenfalls nach dem alten
   Stand (L3: 71 Texte, L4: 86 Texte, L6: 30 Lookups) und sind entsprechend zu korrigieren.
   Die Zahlen je Datei stehen in den Fundstellenspalten des Katalogs.
2. **Formatangaben.** 122 Katalogeinträge tragen normalisierte Platzhalter (Abschnitt 5.4).
   Vor dem Umbau je Fundstelle die Formatangabe aus dem Quelltext übernehmen.
3. **DB-Wert-Literale in den Formularen** — der in dieser Etappe bewusst offengelassene Teil
   von L0.2, rund **47 Fundstellen**:
   `Form_Simulation_Config.cs` (18), `Form_Simulation_Detail.cs` (14),
   `Form_Simulation_Config.Uebersicht.cs` (6), `Form_KonfigPufferspeicher.cs` (5),
   übrige (4). Dabei zu beachten: In `Form_Simulation_Detail.cs:619-668` stehen **Vergleich und
   Anzeige im selben `if`-Block** — der Vergleich bekommt `DbWerte.*`, die unmittelbar folgende
   `new ListViewItem(…)` bekommt den Ressourcenschlüssel. `"Gesamtsystem"` fehlte im Bestand
   ganz als Konstante und ist jetzt `DbWerte.ERZEUGER_GESAMTSYSTEM`.
4. **Befund L0-1 (gehört zu L5).** `Form_PufferSp_Bearbeiten.cs:139` schreibt den lokalisierten
   ComboBox-Text nach `Tab_Pufferspeicher.Speichertyp`. Auf englischer Oberfläche landen
   englische Werte in der Datenbank. Konstanten `DbWerte.PSP_SPEICHERTYP_*` liegen bereit.
   Gleicher Fehlertyp wie B0-9/B0-10/B0-11.
5. **Befund L0-2.** `Form_PufferSp_Projekt.cs:157-160` zeigt DB-Werte als Auswahltext.
   Anzeigeschlüssel liegen im Katalog.
6. **43 verwaiste Einträge** in `Form_Simulation_Config.en-US.resx` entfernen (Abschnitt 4.6) —
   16 Steuerelemente, die es nicht mehr gibt, nachweislich ohne Wirkung.
7. **Bestandsübersetzungen überarbeiten.** Die vorhandenen en-US-Werte von
   `Form_Simulation_Config` und `Form_KonfigPufferspeicher` widersprechen teilweise dem
   Glossar. Sie wurden in L1 **nicht** angefasst (Auftrag war rein additiv), sollten aber
   nachgezogen werden:

   | Schlüssel | heute | nach Glossar |
   |---|---|---|
   | `label7.Text`, `groupBox_PufferSp.Text`, `checkBox_PufferSp.Text` (Config) | „buffer memory" | „buffer storage" |
   | `label12.Text`, `label11.Text`, `label2.Text` (Config) | „producer", „Power generator" | „heat generator", „electricity generator" |
   | `label1.Text` (KonfigPufferspeicher) | „Select producer" | „Select heat generator:" (Doppelpunkt fehlt zusätzlich) |
   | `label7.Text` (Config) | „Change the buffer memory, forward, backward" | Vor-/Rücklauf → „flow/return" |

8. **Zeilenenden.** 9 Dateien tragen LF statt CRLF (Abschnitt 1). Für L8 vorgemerkt.
9. **Restliche Encoding-Baustellen** außerhalb des Simulationsbereichs (Abschnitt 1,
   8 Controller-Dateien; projektweit 93 von 372).

### 6.3 Werkzeuge, die weiterverwendet werden sollten

Im Arbeitsverzeichnis dieser Etappe liegen vier Skripte, die für die Folgepakete nützlich
bleiben (kein Repo-Inhalt, bewusst außerhalb):

- **Kodierungsprüfer** — meldet je Datei UTF8-BOM / UTF8-ohne-BOM / ANSI-1252 / ASCII.
- **Konverter** mit eingebautem Zeichengleichheitsnachweis (MD5 über die dekodierte Fassung).
- **Zeichengleichheitsprüfer** gegen `HEAD` — dekodiert den git-Blob mit der Quellkodierung und
  vergleicht zeichenweise; normalisiert dabei die Zeilenenden.
- **Ressourcen-Testtreiber** — lädt die gebaute Assembly per `Assembly.LoadFrom` und ruft jeden
  Schlüssel jedes Ressourcenblocks unter neutral, de-DE und en-US ab; prüft zusätzlich die
  `DbWerte`-Konstanten und den Gleichstand von `.resx` und `Resource.Designer.cs`.
  **Dieser Treiber sollte nach jedem Teilpaket laufen** — er fängt genau die Fehler, die sonst
  erst zur Laufzeit in der englischen Oberfläche auffallen.

---

## 7. Verifikation

Alle vier geforderten Nachweise sind erbracht.

### 7.1 Build

```
"C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" ^
    WP-Plan.sln -t:Rebuild -p:Configuration=Debug -p:Platform=x86
```

**0 Fehler, exakt 6 Bestandswarnungen** — unverändert dieselben wie vor der Etappe:
2 × CS0108 (`WErzeugerModel.ID_Projekt`, `StromverbraucherStammCtrl.items`),
2 × CS0109 (`KlimaregionStammCtrl.rows`, `.items`),
1 × CS4014 und 1 × CS1998 (beide `MDIMainForm.cs`).

`Referenzlauf\Referenzlauf.csproj` wurde separat gebaut (es ist bewusst nicht Teil der
Solution): **0 Fehler**.

> **Hinweis zum Ausgabepfad:** Während der gesamten Arbeit lief die Anwendung EPOS-Plan auf dem
> Arbeitsplatz und hielt `bin\x86\Debug\net8.0-windows\WindowsFormsApplication1.exe` gesperrt
> (MSB3021/MSB3027). Es wurde deshalb mit umgelenktem `BaseOutputPath` in ein Arbeitsverzeichnis
> gebaut. Der Kompilierlauf selbst ist davon unberührt; die laufende Anwendung des Anwenders
> wurde **nicht** beendet.

### 7.2 Ressourcen-Ladeprüfung

Eigener Testtreiber (Konsolenprogramm außerhalb des Repos, lädt die gebaute Assembly per
`Assembly.LoadFrom` und arbeitet über Reflexion — dadurch ohne die COM-Referenzen der
Anwendung baubar).

Geprüft wurde für **jeden** Ressourcenblock der Assembly: die Schlüsselmenge der neutralen
Kultur, dann jeder einzelne Schlüssel unter `InvariantCulture`, `de-DE` und `en-US`.
Fehler wären: eine Ausnahme beim Laden, ein doppelter Schlüssel, oder ein Schlüssel, der unter
einer Kultur `null` liefert, obwohl der neutrale Wert gesetzt ist.

Endstand nach L2 (mit allen 511 neuen Schlüsseln):

```
Ressourcenbloecke in der Assembly : 94
Bloecke mit mindestens einem Satelliten : 57
Satellit de-DE : vorhanden     Satellit en-US : vorhanden
Einzelabrufe geprueft : 72.501
ERGEBNIS: BESTANDEN - kein Fehler, kein fehlender Schluessel.
```

*(Zwischenstand nach L0/L1, vor Anlage des Katalogs: 70.968 Einzelabrufe, ebenfalls bestanden.)*

Zusätzlich im selben Lauf:

- **`DbWerte`: 51 von 51 Konstanten wertgleich** mit den erwarteten Zeichenketten
  (Sollwerte im Testtreiber unabhängig hinterlegt, nicht aus der Klasse abgeleitet).
- **12 Alias-Stichproben** in `WaermequelleClass`, `WaermesenkeClass`,
  `SimulationPufferspeicher`, `ErdreichTemperatur` und `ProjektPuffer` — alle wertgleich.
- **`MyResource.Resource`: 528 Schlüssel ↔ 528 generierte Eigenschaften**, keine Eigenschaft
  ohne Schlüssel, kein Schlüssel ohne Eigenschaft; alle 528 liefern sowohl unter de-DE
  (deutscher Wert über den neutralen Rückfall) als auch unter en-US (englischer Wert) eine
  Zeichenkette. Damit ist auch nachgewiesen, dass die 4 Einträge mit XML-Sonderzeichen
  (`&`, `<`, `>`) korrekt maskiert sind und die 11 Einträge mit führenden bzw. abschließenden
  Leerzeichen über `xml:space="preserve"` erhalten bleiben.

### 7.3 Regressionslauf — Flag `Kaskade_Zweikanalig` AUS

Referenz: `Referenzlaeufe/2026-08-14_B1-Fixes`, feste Projektliste
`1007,1008,1010,1011,1017,1018,1021,1023,1024`, Arbeitskopie-Mechanismus der Suite.

```
Projekt_1007: PASS (29 Dateien, 324210 Werte)
Projekt_1008: PASS (21 Dateien, 227847 Werte)
Projekt_1010: PASS (18 Dateien, 201540 Werte)
Projekt_1011: PASS (29 Dateien, 324232 Werte)
Projekt_1017: PASS (20 Dateien, 245378 Werte)
Projekt_1018: PASS (19 Dateien, 210343 Werte)
Projekt_1021: PASS (21 Dateien, 227840 Werte)
Projekt_1023: PASS (25 Dateien, 262917 Werte)
Projekt_1024: PASS (26 Dateien, 271686 Werte)

GESAMT: PASS (2.295.993 Werte innerhalb der Toleranz)
```

**9 von 9 PASS.** Darüber hinaus wurde der schärfere Nachweis geführt: **MD5 je Ergebnisdatei**.

```
CSV-Dateien byte-/MD5-gleich : 208
CSV-Dateien abweichend       : 0
CSV-Dateien fehlend          : 0
```

**208 von 208 Dateien sind byte-identisch** — einschließlich der `aggregate.csv`. Die Etappe
ist damit nicht nur innerhalb der Toleranz, sondern bitgenau verhaltensneutral.

Der Lauf wurde **zweimal** durchgeführt: einmal nach Abschluss von L0/L1 (also nach dem
gesamten Code-Umbau) und ein zweites Mal auf dem Endstand nach L2. Beide Male 9/9 PASS,
2.295.993 Werte, 208/208 byte-identisch. Der zweite Lauf belegt, dass das Anlegen der 511
Ressourcenschlüssel den Rechenweg nicht berührt — was zu erwarten war, da noch keine
Codestelle auf sie zugreift, aber nachgewiesen gehört.

> `Kenndaten.laccdb` war während des Laufs vorhanden (die Anwendung lief). Die Suite kopiert
> die Datenbank auch dann lesend und weist im Protokoll darauf hin. Da Referenz und Vergleich
> auf demselben Datenstand rechnen und alle 208 Dateien byte-gleich sind, ist der Nachweis
> davon unberührt.

Der Lauf wurde bewusst **außerhalb des Repos** abgelegt
(Arbeitsverzeichnis, nicht `Referenzlaeufe/`), damit dieser Etappe kein Ordner im
Repository zugerechnet wird. Die Referenzbasis bleibt unverändert `2026-08-14_B1-Fixes`.

### 7.4 Encoding

Siehe Abschnitt 1: 20 von 20 Dateien zeichengleich, 0 abweichende Zeichenzeilen, kein
Mojibake, Zeilenenden unverändert.

---

## 8. Was diese Etappe NICHT getan hat

- **Kein Commit.** Der Arbeitsstand liegt unkommittiert im Arbeitsverzeichnis.
- **Keine Code-Umstellung auf Ressourcen.** Der Katalog ist angelegt, die Fundstellen im Code
  zeigen weiterhin auf ihre hartkodierten Zeichenketten. Das ist Etappe 2.
- **Registry-Schlüssel `@"Software\\wp-plan"` in `Program.cs:45/48` nicht angefasst** — der
  literale Doppel-Backslash bleibt, wie die Randnotiz des Konzepts es verlangt. Die
  Nutzer-Registry wurde nicht verändert.
- **`CurrentCulture` nicht angefasst.** Weiterhin wird nur `CurrentUICulture` gesetzt; Zahlen-
  formatierung und -parsing bleiben unverändert (Konzept: eigenes Vorhaben).
- **Designer-Dateien der Formulare nicht bearbeitet.** Die einzige geänderte `*.Designer.cs`
  ist `Form_PufferSp_Bearbeiten.designer.cs` / `Form_PufferSp_einlesen.designer.cs` — und dort
  ausschließlich die **Byte-Kodierung**, zeichengleich nachgewiesen.
- **Gesperrte Dateien, `DB-Backup/`, `Referenzlaeufe/*` unberührt.** Die produktive Datenbank
  wurde nur lesend (über die Arbeitskopie der Suite) benutzt.

---

# Etappe 2 — Teilpakete L3, L4, L5, L6 (+ `SIMENG_*`)

**Stand: 15.08.2026.** Diese Etappe stellt den Code auf den in Etappe 1 angelegten
Ressourcenkatalog um. Ausgangsstand: `d49075e` (HEAD zum Zeitpunkt der Arbeit; über
`06b4f37` hinaus ist der UI-Sichttest-Fix an `Wizard_WPItem` hinzugekommen, der diese
Etappe nicht berührt).

**Kurzbilanz:** 366 Fundstellen im Quelltext greifen jetzt auf **293 Katalogschlüssel**
zu (vorher 23 Fundstellen auf 7 `KONFIG_*`-Schlüssel). 20 Quelldateien geändert,
2 neu angelegt. Der Rechenweg ist unverändert: **208 von 208 Ergebnisdateien
byte-identisch** zum Lauf desselben Datenstands ohne diese Änderungen — und
**208 von 208 byte-identisch zwischen deutscher und englischer Oberfläche**.

## 9. Stellen-Bilanz je Teilpaket

Gezählt werden Zugriffe auf `MyResource.Resource.*` im Quelltext („Fundstellen") und die
Zahl der dabei benutzten unterschiedlichen Schlüssel.

| Teilpaket | Datei | Fundstellen | Schlüssel | Rest |
|---|---|---:|---:|---|
| **L3** | `Views/Simulation/Form_QuellePufferspeicher.cs` | 21 | 17 | 0 |
| | `Views/Simulation/Form_Quellprofil.cs` | 30 | 26 | 0 |
| | *dazu* Monats- und Wochentagsnamen über `CultureInfo` | (19 Texte) | – | 0 |
| **L4** | `Views/Simulation/Form_Simulation_Config.cs` | 30 | 28 | 1 (begründet) |
| | `Views/Simulation/Form_Simulation_Config.Uebersicht.cs` | 85 | 80 | 1 (begründet) |
| | `Views/Simulation/ErzeugerKatalog.cs` **(neu)** | 14 | 7 | 0 |
| **L5** | `Views/Pufferspeicher/Form_PufferSp_Projekt.cs` | 74 | 68 | 0 |
| | `Views/Pufferspeicher/Form_PufferSp_Bearbeiten.cs` | 11 | 5 | 0 |
| | `Views/Pufferspeicher/Form_PufferSp_einlesen.cs` | 5 | 5 | 0 |
| | `Views/Pufferspeicher/Form_PufferSp.cs` | 3 | 3 | 0 |
| | `Views/Pufferspeicher/Form_PufferSp_Admin.cs` | 3 | 3 | 0 |
| | `Views/Pufferspeicher/PufferSpFilter.cs` **(neu)** | 8 | 6 | 0 |
| **L6** | `Views/Simulation/NavigatorWaerme.cs` | 24 | 23 | 0 |
| | `Allgemein/Simulation/WaermequelleClass.cs` | 7 | 7 | 1 (kein UI-Text) |
| | `Allgemein/Simulation/Ladeordnung.cs` | 8 | 8 | 0 |
| | `Allgemein/Simulation/SimulationPufferspeicher.cs` | 3 | 3 | 0 |
| | `Allgemein/Simulation/WaermesenkeClass.cs` | 2 | 2 | 0 |
| **SIMENG** | `SimulationControl.cs` | 6 | 6 | 0 |
| | `SimulationRunner.cs` | 6 | 6 | 1 (Persistenz) |
| | `SimulationWaermepumpe.cs` | 5 | 4 | 0 |
| | `SimulationSPK.cs` | 4 | 3 | 0 |
| | `SimulationStrombedarf.cs` | 4 | 4 | 0 |
| | `SimulationProtokoll.cs` | 3 | 3 | 1 (Präfix, gewollt) |
| | `SimulationWaermebedarf.cs` | 3 | 3 | 0 |
| | `SimulationBHKW.cs` | 1 | 1 | 0 |
| | **Summe** | **366** | **293** | |

### 9.1 L3 — programmatische Quellendialoge

**`Form_QuellePufferspeicher`** und **`Form_Quellprofil`** sind vollständig umgestellt:
Fenstertitel, Gruppen, Beschriftungen, Reiter, Knöpfe, sämtliche MessageBoxen.

**Monats- und Wochentagsnamen kommen jetzt aus `CultureInfo.CurrentUICulture.DateTimeFormat`**
(`MonthNames`, `DayNames`) statt aus zwei eigenen Arrays. Sie liegen bewusst als
**Eigenschaften** vor und nicht als `static readonly`-Felder: ein Feld würde beim ersten
Typzugriff eingefroren und bliebe bei der Sprachumschaltung im Prozess (siehe
Abschnitt 12.3) auf der alten Sprache stehen. `DayNames` beginnt mit Sonntag, das
Datenmodell mit Montag — der Versatz `(t + 1) % 7` steht kommentiert in
`Wochentagsnamen`. Unter de-DE liefert das zeichengleich „Januar"…„Dezember" und
„Montag"…„Sonntag".

**Die fünf Dezimalkomma-Vorgaben** (`"10,0"`, `"5,0"`, `"0,0"` in
`Form_QuellePufferspeicher`, `"10,0"`, `"0,0"` in `Form_Quellprofil`) stehen nicht mehr als
Zeichenkette im Quelltext. Formatiert wird der **Zahlenwert** über eine neue
Hilfsfunktion `Vorgabe(double)`.

> **Abweichung mit Begründung.** Der Auftrag nennt „kulturinvariant formatieren".
> `Vorgabe` formatiert mit `CultureInfo.CurrentCulture` — aus zwei Gründen:
> (1) Genau diese Kultur benutzen `SetControls` und `TagAnzeigen`, die die Felder
> unmittelbar danach überschreiben (`ToString("F1")`); jede andere Wahl brächte in der
> Maske zwei Schreibweisen nebeneinander. (2) `InvariantCulture` zeigte einem deutschen
> Anwender „10.0" statt „10,0" und verletzte damit die harte Vorgabe „deutsche
> Oberfläche bleibt zeichengleich". Kulturneutral ist die Stelle trotzdem: im Quelltext
> steht kein Komma mehr, und **gelesen** wird ohnehin kulturinvariant über
> `WaermequelleClass.ZahlParsen` (nimmt Komma UND Punkt). `CurrentCulture` selbst wird
> nach wie vor nicht gesetzt (Konzept 13.6, „Nicht Teil dieses Pakets").
> Die Vorgabewerte liegen jetzt als Konstanten `VORGABE_MONATSWERT` /
> `VORGABE_WOCHENWERT` vor und werden vom Setter `Monatswerte` mitbenutzt — die
> frühere doppelte 10 ist damit weg.

**Feste Pixel-Geometrie.** In `Form_QuellePufferspeicher` beginnt die Eingabespalte der
Parametergruppe nicht mehr fest bei x = 180, sondern hinter der breitesten der drei
Beschriftungen (`Math.Max(l1.Right, l2.Right, l3.Right) + 12`), nach oben auf 200 px
gekappt, damit die Felder nicht in die Kapazitätsanzeige bei x = 285 laufen. Auf Deutsch
greift die Untergrenze — das Layout ist unverändert. Auf Englisch („Usable temperature
spread [K]:") rückt die Spalte um wenige Pixel nach rechts. Dieselbe Rechnung steht im
Speicherregelungs-Dialog (`Form_Simulation_Config`, Untergrenze 280, Obergrenze 340).

Die Serie des Vorschau-Diagramms heißt jetzt `QUELLTEMPERATUR` (technischer Schlüssel),
der Anzeigetext steht in `LegendText`.

### 9.2 L4 — `Form_Simulation_Config` (+ `.Uebersicht`)

Neben der Textumstellung zwei strukturelle Änderungen.

**(a) Die duplizierten `LanguageItem`-Listen sind zusammengeführt.** Dieselbe Zuordnung
„DB-Wert ↔ Anzeigename" stand **viermal** im Quelltext, mit unterschiedlichem Inhalt:

| Fundstelle | Inhalt |
|---|---|
| `Form_Simulation_Config`, Konstruktor | 4 Einträge, ohne Gesamtsystem |
| `Form_Simulation_Config.ZuordnungenLaden` | 5 Einträge, Reihenfolge …, Wärmepumpe, Gesamtsystem |
| `Form_Simulation_Config.btn_Speichern_Click` | 5 Einträge, Reihenfolge …, Gesamtsystem, Wärmepumpe |
| `Form_KonfigPufferspeicher.ErzeugerDbWert` | dieselbe Zuordnung als `if`-Kette (Behebung B0-9) |

Neu: **`Views/Simulation/ErzeugerKatalog.cs`** mit dem (aus dem Formular herausgelösten)
Typ `LanguageItem` und den Funktionen `Anzeige(dbWert)`, `DbWert(anzeige)` und
`Liste(params string[])`. Die DB-Werte kommen aus `DbWerte.ERZEUGER_*`; die
Reihenfolgen `WAERMEERZEUGER`, `STROMERZEUGER`, `ENERGIESPEICHER`, `ZUORDENBAR` stehen
einmal. `Liste()` erzeugt bewusst je Aufruf eine neue Liste — die vier
Wärmeerzeuger-ComboBoxen sollen unabhängig voneinander selektieren, und die Anzeigenamen
werden erst beim Aufruf aufgelöst.

`DbWert()` behält die B0-11-Reihenfolge (erst Anzeigename, dann DB-Wert) und lässt
Unbekanntes unverändert durch; `Anzeige()` ebenso. `Form_KonfigPufferspeicher.ErzeugerDbWert`
ist **nicht** angefasst worden (Datei außerhalb des L4-Auftrags), steht aber als vierte
Kopie weiter im Bestand — **offener Punkt für L8**, siehe Abschnitt 13.

**(b) `_zuordnungen` führt intern den DB-Wert statt des Anzeigenamens.** Das ist B0-11 zu
Ende gedacht: Vorher übersetzte `ZuordnungenLaden` beim Lesen in den Anzeigenamen, und
`btn_Speichern_Click` übersetzte beim Schreiben zurück. Der Umweg funktionierte nur,
solange die Sprache zwischen Anlegen und Speichern gleich blieb. Jetzt gilt:

- `ZuordnungenLaden` übernimmt `ctrlpsp.items[i].Erzeuger` **unverändert**;
- `RefreshZuordnungAnzeige` übersetzt erst beim Füllen der ListView (`ErzeugerKatalog.Anzeige`);
- `btn_Hinzu_Click` bildet den vom Zuordnungsdialog gelieferten Anzeigenamen sofort auf
  den DB-Wert ab;
- `ZugeordnetePufferSp` und `AktualisiereErzeugerUebersicht` vergleichen DB-Werte;
- `btn_Speichern_Click` schreibt `ErzeugerKatalog.DbWert(z[0])` — bei einem DB-Wert
  wirkungslos, aber die tolerante Absicherung gegen Alt- und Fremdwerte bleibt stehen.

Damit enthält die Steuerlogik dieses Formulars **keinen lokalisierten Text mehr**.
Zusätzlich sind die DB-Wert-Literale beider Dateien auf `DbWerte.*` umgestellt
(`"Gesamtsystem"`, die vier Erzeugerarten im `switch` von `AnlagenImProjekt`,
`"Luft-Wasser"` an zwei Stellen, `Erzeuger='Wärmepumpe'` in der Speicherregelung).

**Paket-2/8-Bestandteile mitgenommen:** Kaskaden- und Extrapolationsschalter samt ihren
Mouseover-Hinweisen und den vier Statusmeldungen, die neun Spaltenköpfe der Übersicht,
die sechs Mouseover-Texte der Übersicht und die fünf der Alt-Zuordnung.

**Zeilenumbrüche.** Die `.resx` legt Umbrüche als LF ab (XML-Normierung), der Bestand
setzte an mehreren Stellen `Environment.NewLine`. Wo das der Fall war, steht jetzt
`.Replace("\n", Environment.NewLine)` — und zwar **auf der Formatzeichenkette, vor dem
Einsetzen**. Andersherum wären die bereits mit `Environment.NewLine` verketteten
Referenzlisten doppelt umgebrochen worden.

### 9.3 L5 — Pufferspeicher-Dialoge

Alle 17 MessageBoxen der fünf Dialoge sind umgestellt; dazu die Beschriftungen,
Spaltenköpfe, Knopftexte und Statusmeldungen von `Form_PufferSp_Projekt` (74 Fundstellen).

**Pflicht-Fix Befund L0-1 — `Speichertyp` wurde lokalisiert in die Datenbank geschrieben.**
`Form_PufferSp_Bearbeiten.cs:139` setzte `model.Speichertyp = comboBox_Speichertyp.Text`.
Auf englischer Oberfläche landeten damit „Solar storage", „Buffer storage",
„Combination storage" in `Tab_Pufferspeicher_STAMM.Speichertyp`.

Behoben über den **Auswahlindex**, der sprachfrei ist:

| Weg | vorher | jetzt |
|---|---|---|
| Schreiben | `comboBox.Text` | `SpeichertypDbWert()` → `SPEICHERTYP_DB_WERTE[SelectedIndex]` (`DbWerte.PSP_SPEICHERTYP_*`) |
| Lesen | `SetzeText(comboBox, row, "Speichertyp")` | `SpeichertypAnzeigen(row)` → `SpeichertypIndex(wert)` → `SelectedIndex` |

`SpeichertypIndex` prüft in dieser Reihenfolge: deutscher Persistenzwert → angezeigter
Text der aktuellen Sprache → **englischer Altwert**. Die drei Altwerte stehen als
`SPEICHERTYP_ALTWERTE_EN` eingefroren im Quelltext, ausdrücklich **nicht** als Ressource:
Sie beschreiben Altdaten und dürfen sich mit einer Übersetzungskorrektur nicht
mitändern. Ein Datensatz, der vor der Behebung auf englischer Oberfläche gespeichert
wurde, geht damit im Dialog wieder auf und trägt nach dem nächsten Speichern wieder den
deutschen Wert. Freitext (die ComboBox lässt ihn zu) läuft unverändert durch, damit eine
bewusste Eingabe nicht stillschweigend umgeschrieben wird.

Die Probe dazu steht in Abschnitt 12.5.

**Befund L0-2 gleich mitbehoben.** `Form_PufferSp_Projekt` setzte die DB-Werte „Heizung"
und „Brauchwasser" unmittelbar als ComboBox-Einträge und las sie über
`SelectedItem.ToString()` als Steuerwert zurück. Neu: die Klasse `VerwendungItem`
(`DbWert` + `Anzeige`) und die beiden Zugriffe `VerwendungWaehlen(dbWert)` /
`GewaehlteVerwendung()`. Für die Anzeige eines Verwendungs-DB-Werts an anderer Stelle gibt
es jetzt **`WaermesenkeClass.VerwendungAnzeige(dbWert)`** — der eine erlaubte Übergang von
der Persistenz- in die Anzeigeschicht. Er wird von der Projektliste des Dialogs, der
Fußzeile der Konfigurationsübersicht und der Verwendungswechsel-Rückfrage benutzt; damit
ist auch der in Etappe 1 (Abschnitt 5.5) angemeldete Vorbehalt „die englische Meldung
mischt die Sprachen" erledigt.

**B0-9/B0-10/B0-11 verifiziert — und B0-10 gehärtet.** B0-9 und B0-11 sind behoben
(B0-11 jetzt strukturell, siehe 9.2). B0-10 war behoben, wäre durch die Lokalisierung
aber **zurückgekommen**: Der Volumenfilter verglich den angezeigten ComboBox-Text gegen
deutsche Literale; mit übersetzten Einträgen hätte kein Vergleich mehr getroffen, und es
hätte wieder nur der Vorbelegungszweig gegriffen. Neu:
**`Views/Pufferspeicher/PufferSpFilter.cs`** — Filterstufen über den **Auswahlindex**,
die sechs SQL-Prädikate stehen dort einmal statt zweimal (`Form_PufferSp` und
`Form_PufferSp_Admin` hatten sie doppelt). Der Herstellerfilter kennt keinen festen
Eintrag „Alle"; er vergleicht weiterhin einen Text, aber gegen denselben Ressourcenwert,
mit dem er vorbelegt wird — Vorbelegung und Vergleich passen damit in jeder Sprache
zusammen. Nebenbei verdoppelt `HerstellerSql` jetzt einfache Anführungszeichen im
Herstellernamen; vorher hätte ein Apostroph das Prädikat zerrissen.

### 9.4 L6 — `NavigatorWaerme` und Klassen

**Chart-Serien auf technische Schlüssel.** Die sieben Serien hießen bisher nach ihrem
deutschen Anzeigetext, und zwar uneinheitlich („Wärmebedarf" mit Umlaut, „Waermepumpe"
ohne). Jetzt: `WAERMEBEDARF`, `GESAMT`, `WAERMEPUMPE`, `HEIZSTAB`, `HEIZKESSEL`,
`SOLARTHERMIE`, `BHKW_WAERME` als `private const string`, der Anzeigetext ausschließlich
in `Series.LegendText`. Alle **30 Nachschlagestellen** (`Series["…"]`,
`Series.IndexOf(…)`) sind nachgezogen. Eine neue Hilfsfunktion `SerieAnlegen(schluessel,
legende, farbe, werte)` legt beides in einem Schritt an. Die Speicherserien tragen ihre
technischen Schlüssel (`PUFFER_<ID>` / `QUELLE_<AnlagenID>`) schon seit Paket 7 — das
Muster ist damit im ganzen Navigator einheitlich.

> **Eine sichtbare Änderung, bewusst in Kauf genommen.** Die Legende zeigte für die
> Wärmepumpe bisher **„Waermepumpe"** — der Serienname war umlautfrei, weil er zugleich
> Zugriffsschlüssel war. Mit der Trennung Schlüssel/Anzeigetext steht dort jetzt
> **„Wärmepumpe"**. Das ist die einzige Stelle dieser Etappe, an der sich die deutsche
> Oberfläche ändert; sie behebt einen Schreibfehler, der nur aus der technischen
> Doppelnutzung stammte.

**`WaermequelleClass.TypAnzeige`** ist von `static readonly string[]` auf eine
**Eigenschaft** umgestellt, die je Aufruf ein neues Array aus den sechs `SIMQ_TYP_*`
liefert. Ein Feld hätte die Sprache beim ersten Typzugriff eingefroren. Die
Indexkopplung zu `TypWerte` bleibt unverändert und ist im Kommentar geschärft: der
Steuerwert ist der Index, der Text ist Anzeige.

**`WaermequelleClass.CSV_FORMAT_HINWEIS`** ist von `const string` auf eine **Eigenschaft**
umgestellt (`SIMQ_CSV_FORMAT_HINWEIS`) — eine Konstante kann keine Ressource
referenzieren, genau der Fall, den Konzept 13.6 nennt. Beide Aufrufstellen bleiben
unverändert.

Dazu lokalisiert: `SimulationPufferspeicher.RolleAnzeige()`/`BezeichnerAnzeige()`,
`Ladeordnung.ErzeugerName()` und die beiden `ToString()` der Lade- und Entladeeinträge.
Bei `ErzeugerName` ist im Kommentar jetzt festgehalten, dass das direkt darunter stehende
`KaskadenLiteral` für dieselben Typen die **Persistenzwerte** liefert — bis Paket 9 waren
beide Zeichenketten identisch, was die Verwechslung leicht machte.

**Nicht umgesetzt: „Designer auf `Localizable`" für `NavigatorWaerme`.** Wie vom
Auftraggeber entschieden — eine `Localizable`-Ressource trägt je Kultur auch Position und
Größe; ein Handumbau ohne den WinForms-Designer verschöbe Steuerelemente. Die
programmatisch angelegten Texte (`checkBox_Puffer`, CSV-Knopf, Speicherauswahl) sind
stattdessen im Quelltext auf den Katalog umgestellt. Die im Designer liegenden Checkboxen
(`checkBox_Gesamt`, `checkBox_WP`, …) bleiben deutsch — **offener Punkt für L8**.

### 9.5 `SIMENG_*` — Engine- und Protokollmeldungen

Alle 29 Katalogschlüssel sind angeschlossen: Abbruchgründe des Runners, Kessel- und
BHKW-Obergrenzen, Pendelspeicher, Ladeordnung, Stromprofile, Tagesverteilung,
Prozesswärme, Brauchwasser, WP-Kenndaten und die beiden Extrapolationsmeldungen.

**Was ausdrücklich NICHT lokalisiert wurde:**

| Stelle | Grund |
|---|---|
| `SimulationProtokoll.Eintragen`: `"Simulation " + art + ": "` | Konsolenpräfix. `Referenzlauf/Protokoll.cs:67-68` zählt Warnungen und Fehler über genau diese Token (`"Simulation Warnung:"`, `"FEHLER:"`). Eine Übersetzung setzte die Auswertung der Lauf-Protokolle stillschweigend auf null. Lokalisiert ist der **Meldungsinhalt** und die Anzeigefassung in `AlsText()`. |
| `SimulationWaermepumpe`, Schlüssel von `HinweisEinmal` | Der Einmal-Schlüssel ist Schicht 2. Änderte er sich mit der Sprache, käme die Meldung in einer Sprache mehrfach und in der anderen gar nicht. |
| `SimulationRunner.cs:499` `mo.Modul = … ?? "Standard BHKW"` | **Persistenzwert.** Der Wert wird nach `Tab_ErgebnisBHKWModul.Modul` geschrieben und von der Referenzlauf-Suite als Skalar exportiert; übersetzt ließe er DE- und EN-Läufe auseinanderlaufen. Katalogkorrektur dokumentiert. |
| `Console.WriteLine`-Zeilen der Engine | Wie in L2 festgelegt (Konzept 13.4): sie sind Diagnose, keine Oberfläche, und die Suite liest sie mit. |

## 10. Neue und geänderte Dateien

**Neu (2):**

| Datei | Zweck |
|---|---|
| `Views/Simulation/ErzeugerKatalog.cs` | `LanguageItem` + die EINE Zuordnung DB-Wert ↔ Anzeigename (ersetzt vier Kopien) |
| `Views/Pufferspeicher/PufferSpFilter.cs` | Volumen- und Herstellerfilter der beiden Katalogdialoge, indexbasiert (ersetzt zwei Kopien, härtet B0-10) |

**Geändert (20):** die 16 Dateien der Bilanz in Abschnitt 9 plus
`MyResource/Resource.resx`, `MyResource/Resource.en-US.resx`,
`MyResource/Resource.Designer.cs` und `Referenzlauf/Program.cs`.

### 10.1 Ressourcenkatalog: +2 Schlüssel, 2 Berichtigungen

| Schlüssel | DE | EN | Grund |
|---|---|---|---|
| `CHART_LEGENDE_GESAMT` | Gesamt | Total | neu — Legendentext der Serie `GESAMT` |
| `CHART_LEGENDE_WAERMEBEDARF` | Wärmebedarf | Heat demand | neu — Legendentext der Serie `WAERMEBEDARF` |

`SIMENG_STROMPROFILE_DIAGNOSE` trägt jetzt **zwei** Platzhalter
(`…nicht berechnet werden{0} - {1}`): `{0}` nimmt den optionalen Zusatz
`SIMENG_STROMPROFIL_ZULETZT_BEARBEITET` auf, `{1}` die Ausnahmemeldung. Mit nur einem
Platzhalter wäre der Zusatz beim Umbau verlorengegangen; die deutsche Ausgabe ist
zeichengleich. Zur Katalogkorrektur bei `SIM_BHKW_MODUL_STANDARD` siehe Abschnitt 9.5.

**Bestand jetzt: 530 Schlüssel** in beiden `.resx` und 530 Eigenschaften in
`Resource.Designer.cs`.

### 10.2 Zwei Änderungen, die nicht von dieser Etappe stammen

- **`Resource.Designer.cs` wurde während der Arbeit von Visual Studio neu erzeugt.** Die
  laufende IDE bemerkt eine geänderte `.resx` und wirft ihren Generator an — dasselbe
  Verhalten, das schon in Etappe 1 (Abschnitt 0) beobachtet wurde. Die beiden neuen
  Eigenschaften standen dadurch doppelt in der Datei (einmal von der IDE, einmal von
  Hand); die Handfassung wurde entfernt. Der Endstand ist über den Testtreiber geprüft:
  530 Schlüssel ↔ 530 Eigenschaften, keine Dublette.
- **`WindowsFormsApplication1.csproj`** hat dabei einen
  `<Compile Update="MyResource\Resource.Designer.cs">`-Eintrag mit
  `DesignTime`/`AutoGen`/`DependentUpon` bekommen — ebenfalls von der IDE. Der Eintrag ist
  reine Darstellungs-Metadaten (Verschachtelung im Projektbaum), verhaltensneutral und
  sachlich richtig; er wurde stehen gelassen, weil Visual Studio ihn sonst beim nächsten
  Speichern erneut anlegt.

### 10.3 `Referenzlauf/Program.cs`

Eine additive Ergänzung: `OberflaechenspracheSetzen()` liest die Umgebungsvariable
**`EPOS_REFLAUF_UICULTURE`** und setzt daraus `Thread.CurrentUICulture` sowie
`CultureInfo.DefaultThreadCurrentUICulture`. **`CurrentCulture` bleibt unangetastet.**
Ohne die Variable ändert sich nichts — dann gilt wie bisher die Systemkultur.

Eine Umgebungsvariable statt eines Arguments, weil jedes Projekt in einem eigenen
Kindprozess rechnet: die Umgebung wird vererbt, ein Argument müsste durchgereicht werden.
Die **Registry des Anwenders wird nicht angefasst** (`HKCU\Software\wp-plan` bleibt, wie
sie ist).

## 11. Kodierung

Alle 20 geänderten Quelldateien behalten ihre Kodierung: **UTF-8 mit BOM**, kein
Mojibake. Die sieben Dateien, die schon vor dieser Etappe reine LF-Zeilenenden trugen
(`Ladeordnung.cs`, `WaermequelleClass.cs`, `WaermesenkeClass.cs`,
`Form_PufferSp_Projekt.cs`, `Form_QuellePufferspeicher.cs`, `Form_Quellprofil.cs`,
`Referenzlauf/Program.cs`), behalten sie — eine Umstellung würde jede Zeile im Diff
verändern und den Nachweis „nur diese Stellen wurden angefasst" unmöglich machen.
**Offener Punkt für L8**, unverändert gegenüber Etappe 1.

Die beiden neuen Dateien sind nach der `.editorconfig` angelegt: **UTF-8 mit BOM, CRLF**.

## 12. Verifikation

### 12.1 Build

```
MSBuild.exe WP-Plan.sln -t:Build -p:Configuration=Debug -p:Platform=x86 -p:BaseOutputPath=<Arbeitsordner>
MSBuild.exe Referenzlauf\Referenzlauf.csproj -t:Build -p:Configuration=Debug -p:Platform=x86
```

**0 Fehler, exakt 6 Bestandswarnungen** — dieselben wie in Etappe 1: 2 × CS0108
(`WErzeugerModel.ID_Projekt`, `StromverbraucherStammCtrl.items`), 2 × CS0109
(`KlimaregionStammCtrl.rows`, `.items`), 1 × CS4014 und 1 × CS1998 (beide `MDIMainForm.cs`).
`Referenzlauf.csproj` ebenfalls 0 Fehler.

> Der Ausgabepfad der Solution wurde wie in Etappe 1 in einen Arbeitsordner umgelenkt.
> Die Anwendung des Anwenders lief während dieser Etappe nicht (keine
> `Kenndaten.laccdb`); sie wurde zu keinem Zeitpunkt beendet.

### 12.2 Regressionslauf — und ein Befund zur Referenzbasis

Der erste Vergleich gegen `Referenzlaeufe/2026-08-14_B1-Fixes` meldete **8 × PASS und
Projekt 1024 FAIL** (75.575 Abweichungen). Ursache ist **nicht** diese Etappe:

- In `Projekt_1024/aggregate.csv` fehlt im neuen Lauf der komplette Block
  `WaermepumpeModul[1]` („CS7800iLW 12"). Eine **zweite Wärmepumpe ist im Projekt nicht
  mehr vorhanden** — das kann kein Codeumbau bewirken.
- Die produktive `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` trägt den Zeitstempel
  **15.08.2026 11:58** und wurde damit **nach** der Verifikation von Etappe 1 (11:00–11:25)
  vom Anwender geändert.

Nachweis über einen **Baselinelauf aus einem eigenen git-Arbeitsbaum auf `HEAD`
(`d49075e`, also ohne die Änderungen dieser Etappe)**, gerechnet auf demselben,
aktuellen Datenstand:

```
B1-Fixes            vs HEAD(d49075e) : 193 byte-gleich, 15 abweichend  (alle 15 in Projekt_1024)
B1-Fixes            vs Paket9-Etappe2: 193 byte-gleich, 15 abweichend  (dieselben 15 Dateien)
HEAD(d49075e)       vs Paket9-Etappe2: 208 byte-gleich,  0 abweichend
```

Der Baselinelauf zeigt **exakt dieselbe** Abweichung. Der Unterschied zur eingefrorenen
Basis stammt also vollständig aus der geänderten Datenbank; gegenüber dem Lauf **ohne**
diese Etappe auf **demselben** Datenstand sind **208 von 208 Ergebnisdateien
byte-identisch**. Der Toleranzvergleich der Suite bestätigt das:

```
Projekt_1007: PASS   Projekt_1011: PASS   Projekt_1021: PASS
Projekt_1008: PASS   Projekt_1017: PASS   Projekt_1023: PASS
Projekt_1010: PASS   Projekt_1018: PASS   Projekt_1024: PASS
GESAMT: PASS (2.295.987 Werte innerhalb der Toleranz)
```

Der Lauf wurde nach der Endfassung des Codes wiederholt — dasselbe Ergebnis
(208/208 byte-gleich).

> **Empfehlung an den Auftraggeber:** Die Referenzbasis `2026-08-14_B1-Fixes` bildet den
> Datenstand von gestern ab. Sobald die Änderungen an Projekt 1024 gewollt sind, sollte
> eine neue Basis auf dem heutigen Datenstand eingefroren werden — sonst schleppt jede
> Folgeprüfung diese eine erklärungsbedürftige Abweichung mit. Die eingefrorenen
> Lauf-Protokolle wurden von dieser Etappe **nicht** angefasst.

### 12.3 Sprachgleichheit (L7-Vorstufe)

Derselbe Lauf, dieselben neun Projekte, einmal mit deutscher und einmal mit englischer
Oberflächensprache (`EPOS_REFLAUF_UICULTURE=en-US`, siehe 10.3):

```
SPRACHGLEICHHEIT DE vs EN : 208 byte-gleich, 0 abweichend
Toleranzvergleich          : 9 von 9 PASS, 2.295.987 Werte
```

**208 von 208 Ergebnisdateien sind byte-identisch** — über alle neun Projekte, nicht nur
über die geforderten drei (1007, 1023, 1024). Damit ist nachgewiesen, dass kein
lokalisierter Text als Steuerwert dient.

Die Konsolenausgabe zeigt zugleich beides, was sie zeigen soll — den übersetzten
Meldungsinhalt und das **unveränderte deutsche Präfix**:

```
Simulation Hinweis: Heat pump 'CS7800iLW 12': the source temperature falls below the
lowest data point of the performance curve (-5,0 °C). Extrapolation is applied
(project setting "Allow extrapolation of the performance curve").
```

Die Zahl steht als `-5,0` da — `CurrentCulture` ist unverändert deutsch, wie es das
Konzept verlangt.

### 12.4 Ressourcen-Ladeprüfung und Platzhalterprobe

Testtreiber außerhalb des Repos, lädt die gebaute Assembly per `Assembly.LoadFrom` und
arbeitet über Reflexion:

```
Ressourcenbloecke geprueft : 94
Einzelabrufe geprueft      : 72.501     (jeder Schluessel unter Invariant, de-DE, en-US)
MyResource.Resource        : 530 Schluessel / 530 Eigenschaften
Eintraege mit Platzhaltern : 103
Probeformatierungen        : 206        (je Eintrag neutral UND en-US)
Leere Werte de-DE / en-US  : 0 / 0
resx neutral / en-US       : 530 / 530
ERGEBNIS: BESTANDEN - kein Fehler.
```

Geprüft wird damit: kein doppelter Schlüssel, kein Schlüssel, der unter einer Kultur
`null` liefert, Gleichstand `.resx` ↔ `Resource.Designer.cs`, gleiche Schlüsselmenge in
beiden `.resx` — und für **jeden** Eintrag mit `{n}` eine Probeformatierung mit
Ersatzargumenten. Geprüft wird dabei auch, dass die **Platzhalterzahl in neutral und
en-US übereinstimmt**; eine Übersetzung mit einem vergessenen `{1}` fiele hier auf.
Keine `FormatException`.

### 12.5 Befund L0-1 — Schreib-/Leseprobe

Eigener Reflexions-Testtreiber gegen die gebaute Assembly, **ohne Datenbankzugriff**:
geprüft wird genau der Code, der den Wert bildet (`InitDatensatzUpdate` →
`PufferSpModel.Speichertyp`) bzw. ihn anzeigt (`SpeichertypAnzeigen`).

```
=== Oberflaechensprache de-DE ===
  Schreiben [0] Anzeige="Solarspeicher"  -> DB="Solarspeicher"   OK
  Schreiben [1] Anzeige="Pufferspeicher" -> DB="Pufferspeicher"  OK
  Schreiben [2] Anzeige="Kombispeicher"  -> DB="Kombispeicher"   OK
  Lesen  DB="Solarspeicher"  -> Index 0, Anzeige="Solarspeicher"   OK   (alle drei)
  Altwert DB="Solar storage" -> Index 0, Anzeige="Solarspeicher"   OK
     Rueckschreiben -> DB="Solarspeicher"  OK                           (alle drei)

=== Oberflaechensprache en-US ===
  Schreiben [0] Anzeige="Solar storage"       -> DB="Solarspeicher"   OK
  Schreiben [1] Anzeige="Buffer storage"      -> DB="Pufferspeicher"  OK
  Schreiben [2] Anzeige="Combination storage" -> DB="Kombispeicher"   OK
  Lesen  DB="Solarspeicher"  -> Index 0, Anzeige="Solar storage"    OK   (alle drei)
  Altwert DB="Solar storage" -> Index 0, Anzeige="Solar storage"    OK
     Rueckschreiben -> DB="Solarspeicher"  OK                            (alle drei)

ERGEBNIS: BESTANDEN
```

**Der DB-Wert ist unter beiden Sprachen immer deutsch**, der Anzeigetext folgt der
Sprache, und ein englischer Altwert wird beim Lesen erkannt und beim nächsten Speichern
auf den deutschen Persistenzwert zurückgeführt.

### 12.6 Hardcoding-Restzählung

Volltextsuche über die 16 Zieldateien: alle Zeichenkettenliterale außerhalb von
Kommentaren, ohne reine Bezeichner. Von 115 Treffern sind 113 SQL-Fragmente,
Parameternamen (`@id`, `@proj`), Spaltennamen, Escape-Sequenzen oder
`Console.WriteLine`-Diagnosen — alle laut L2 ausdrücklich außerhalb des Katalogs.

**Benutzersichtbare deutsche Literale, die bleiben — 2 Stück, beide begründet:**

| Stelle | Text | Begründung |
|---|---|---|
| `Form_Simulation_Config.cs:144` | `HilfeKontext.SetzeBereich("Simulation Konfiguration (Erzeuger definieren, Pufferspeicher zuordnen)")` | Bedienkontext für den KI-Hilfe-Assistenten. Erreicht den Anwender nur mittelbar; in L2 ausdrücklich vom Katalog ausgenommen. |
| `Form_Simulation_Config.Uebersicht.cs:580` | `" l)"` | Interpunktion und Einheit der Fußzeilen-Aufzählung `Bezeichner (Verwendung, n l)`. Sprachneutral zusammengesetzt; Bezeichner und Verwendung kommen bereits übersetzt. |

Dazu drei Stellen, die **absichtlich** deutsch bleiben und keine Oberfläche sind:
`SPEICHERTYP_ALTWERTE_EN` (Bestandstoleranz, 9.3), `"Simulation "` als Konsolenpräfix
(9.5) und `WaermequelleClass.cs:634` `sp.Erzeuger = "Wärmequelle"` (reines
In-Memory-Etikett, nirgends persistiert — schon in Etappe 1, Abschnitt 2 belegt).

## 13. Offene Punkte für L7 und L8

1. **UI-Sichttest steht aus.** Die Layoutrisiken sind gerechnet, nicht gesehen. Zu
   prüfen sind auf **englischer** Oberfläche: die Parametergruppe von
   `Form_QuellePufferspeicher` (Beschriftungen ↔ Eingabespalte ↔ Kapazitätsanzeige),
   die Checkbox „Source available without limit …" (endet rechnerisch knapp vor dem
   Gruppenrand), die neun Spalten der Erzeugerübersicht mit ihren festen Breiten
   (`SPALTEN_BREITEN`, u. a. „prio" in 40 px und „HP prio" in 62 px), die drei
   Radiobuttons des Betriebsmodus-Dialogs, die Knopftexte von `Form_PufferSp_Projekt`
   (214 px) und die Legende des Wärme-Navigators.
2. **`Form_KonfigPufferspeicher.ErzeugerDbWert`** ist die vierte, jetzt überflüssige
   Kopie der Erzeugerzuordnung. Sie sollte auf `ErzeugerKatalog.DbWert` umgestellt
   werden — die Datei lag außerhalb des L4-Auftrags.
3. **Designer-Texte des Wärme-Navigators** (`checkBox_Gesamt`, `checkBox_WP`,
   `checkBox_Heizstab`, `checkBox_SPK`, `checkBox_ST`, `checkBox_BHKW`,
   `checkBox_Waermebedarf`) sind weiterhin deutsch. Sie gehören in die
   `NavigatorWaerme.en-US.resx` und damit an den WinForms-Designer, nicht an die Hand.
4. **Bestandsübersetzungen** von `Form_Simulation_Config.en-US.resx` und
   `Form_KonfigPufferspeicher.en-US.resx` widersprechen weiter dem Glossar
   („buffer memory", „producer") — offener Punkt 7 aus Etappe 1, unverändert.
5. **43 verwaiste Einträge** in `Form_Simulation_Config.en-US.resx` — offener Punkt 6
   aus Etappe 1, unverändert.
6. **Zeilenenden** (7 Dateien mit LF) und **Encoding-Baustellen außerhalb des
   Simulationsbereichs** — offene Punkte 8 und 9 aus Etappe 1, unverändert.
7. **Neue Referenzbasis** auf dem heutigen Datenstand einfrieren (siehe 12.2).
8. **`Views/Simulation/NavigatorStrom.cs`, `NavigatorUebersicht.cs`, `DashboardForm.cs`,
   `Form_Simulation_Detail.cs`, `Form_Waermesenke.cs`, `Form_QuelleErdreich.cs`,
   `TabNavigationManager.cs`** tragen laut Katalog zusammen noch rund 220 Fundstellen.
   Sie waren nicht Teil des Etappe-2-Auftrags (L3–L6) und sind der größte verbliebene
   Block.

## 14. Was diese Etappe NICHT getan hat

- **Kein Commit.** Der Arbeitsstand liegt unkommittiert im Arbeitsverzeichnis.
- **Gesperrte Dateien unberührt:** `Controller/WizardCtrl.cs`, `Model/WErzeugerModel.cs`,
  `Views/BHKW/Form_BHKWEing.cs`, `Views/Heizkessel/Form_Heizkessel.cs`,
  `Views/Wizard/WizardParent.cs`. Ebenso `Views/Wizard/Wizard_WPItem.*` und
  `Views/Wärmepumpe/Form_WPAuswahl.cs` (parallele Arbeiten), `DB-Backup/` und die
  untracked `Referenzlaeufe/*`-Ordner.
- **Keine `.Designer.cs` eines Formulars geändert**, keine `.resx` eines Formulars
  geändert — auch nicht additiv. Der L0-1-Fix betrifft ausschließlich
  `Form_PufferSp_Bearbeiten.cs`.
- **`CurrentCulture` nicht gesetzt** — weder in der Anwendung noch in der
  Referenzlauf-Suite. Nur `CurrentUICulture`.
- **Registry nicht angefasst.** `@"Software\\wp-plan"` in `Program.cs` bleibt unverändert.
- **Produktive Datenbank nur lesend** benutzt (über die Arbeitskopie der Suite). Die
  Läufe dieser Etappe liegen außerhalb des Repos in einem Arbeitsverzeichnis; unter
  `Referenzlaeufe/` ist kein neuer Ordner entstanden.
