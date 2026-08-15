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

Der en-US-Satellit enthält 43 Einträge für **28 Steuerelemente, die es nicht mehr gibt**
(`groupBox1`–`groupBox6`, `label5`, `label6`, `label10`, `label13`, `label14`, `label15`,
`label17`, `label18`, `label20`, `label57`, `checkBox_Heizstab`, `btn_Strom_Simu_Start`,
`comboBox_Bereitschaft`, `comboBox_Stromspeicher_LadeenergieMax_auswahl`,
`comboBox7_Stromspeicher_LadeleistungMax_auswahl`,
`comboBox8_Stromspeicher_LadeenergieMin_auswahl`, `textBox_Netzverluste`,
`textBox_Speicher_Ladeschwelle`, `textBox_untere_PGrenze`,
`textBox_Stromspeicher_Ladeenergie_max`, `textBox_Stromspeicher_Ladeenergie_min`,
`textBox_Stromspeicher_Ladeleistung_max`). Für jedes wurde geprüft: **null Treffer** in
`Form_Simulation_Config.cs`, `.Designer.cs` und `.Uebersicht.cs`.

> **Berichtigt in der Nacharbeit (Abschnitt 25.4).** Hier stand „16 Steuerelemente" — gezählt
> waren nur die Namen mit einem `.Text`-Eintrag. Die 43 Einträge verteilen sich tatsächlich auf
> **28** Steuerelemente; die übrigen 12 tauchen nur mit `.Location` oder `.Size` auf.

Sie sind wirkungslos, weil `ApplyResources` für diese Namen nie aufgerufen wird. Entfernt
wurden sie **nicht** — der Auftrag für L1 war ausdrücklich additiv. **Aufräumkandidat für
Etappe 2.** *(In der Nacharbeit erledigt, Abschnitt 25.4.)*

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

---

# Etappe 2b — Rest-Simulationsbereich, Konsolidierung, Verdeckungsfix

**Stand: 15.08.2026.** Ausgangsstand: Commit `925c37f` (Paket 9, Etappe 2). Diese Etappe
schließt den in Abschnitt 13, Punkt 8 benannten Restblock, räumt die letzte doppelte
Erzeugerzuordnung weg und behebt einen im Harness nachgewiesenen Anzeigefehler in
`Form_Simulation_Config`.

**Kurzbilanz:** **318 neue Fundstellen** greifen jetzt zusätzlich auf den Katalog zu
(31 → 349 in den betroffenen Dateien), **11 Schlüssel** kamen dazu (530 → **541**).
Der Rechenweg ist unverändert: **208 von 208 Ergebnisdateien byte-identisch** gegen die
neue Basis `2026-08-15_B2` — und **208 von 208 byte-identisch zwischen deutscher und
englischer Oberfläche**.

## 15. Neue Referenzbasis `2026-08-15_B2`

### 15.1 Warum

Der Etappe-2-Befund aus Abschnitt 12.2 ist damit erledigt: Die produktive
`Kenndaten.accdb` trägt den Zeitstempel **15.08.2026 11:58**; der Anwender hat in
**Projekt 1024** das zweite Wärmepumpenmodul (`CS7800iLW 12`) entfernt. Gegen die alte
Basis `2026-08-14_B1-Fixes` war Projekt 1024 dadurch dauerhaft FAIL, ohne dass eine
Codeänderung daran beteiligt gewesen wäre.

```
2026-08-14_B1-Fixes vs 2026-08-15_B2 : 193 byte-/MD5-gleich, 15 abweichend
                                       (alle 15 in Projekt_1024)
Toleranzvergleich                    : 8 x PASS, Projekt_1024 FAIL (75.575 Abweichungen)
   aggregate.csv [WaermepumpeModul[1].*] : 6 Eintraege fehlen im neuen Lauf
```

### 15.2 Wie

Gerechnet mit dem Referenzlauf-Werkzeug, feste Projektliste
`1007,1008,1010,1011,1017,1018,1021,1023,1024`, Arbeitskopie-Mechanismus der Suite.
Die produktive Datenbank wurde ausschließlich **gelesen**; eine `Kenndaten.laccdb` lag
nicht daneben (die Anwendung des Anwenders lief, hatte die Datei aber nicht geöffnet).

Die Basis wurde **zweimal** gerechnet und beide Läufe gegeneinander geprüft:

| Lauf | Herkunft des Codes |
|---|---|
| erster Lauf | Arbeitsverzeichnis auf `925c37f`; abweichend nur die fünf gesperrten Dateien |
| Kontrolllauf | eigener **git-Arbeitsbaum auf `925c37f`**, also der reine Commit-Stand |

```
Arbeitsverzeichnis vs reiner HEAD : 208 byte-/MD5-gleich, 0 abweichend
```

Damit ist zugleich belegt, dass die unkommittierten Änderungen an den fünf gesperrten
Dateien den Rechenweg nicht berühren — die eingefrorene Basis **ist** der Commit-Stand.

### 15.3 Selbstvergleich

Ein zweiter Lauf desselben Codes auf derselben Quelle:

```
Projekt_1007..1024 : 9 von 9 PASS   (2.295.987 Werte innerhalb der Toleranz)
CSV-Dateien byte-/MD5-gleich : 208
CSV-Dateien abweichend       : 0
```

`Referenzlaeufe/LIESMICH.md` ist um die neue Basis samt Begründung ergänzt; `B1-Fixes`
steht dort jetzt als vorheriger Stand. Die eingefrorenen Ordner der Vorgänger wurden
**nicht** angefasst.

## 16. Stellen-Bilanz Etappe 2b

Gezählt werden Zugriffe auf `MyResource.Resource.*` außerhalb von Kommentaren.

| Datei | vorher | nachher | neu | Schlüssel | Rest |
|---|---:|---:|---:|---:|---|
| `Views/Simulation/Form_Simulation_Detail.cs` | 0 | 147 | **147** | 98 | 5 (begründet) |
| `Views/Simulation/Form_Waermesenke.cs` | 0 | 42 | **42** | 35 | 0 |
| `Views/Simulation/Form_QuelleErdreich.cs` | 0 | 38 | **38** | 37 | 3 (begründet) |
| `Views/Simulation/NavigatorStrom.cs` | 0 | 28 | **28** | 21 | 0 |
| `Views/Simulation/NavigatorUebersicht.cs` | 0 | 22 | **22** | 18 | 3 (Schriftnamen) |
| `Views/Simulation/DashboardForm.cs` | 0 | 12 | **12** | 12 | 0 |
| `Views/Simulation/TabNavigationManager.cs` | 0 | 4 | **4** | 4 | 0 |
| `Views/Simulation/NavigatorWaerme.cs` | 24 | 32 | **8** | 24 | 0 |
| `Allgemein/Simulation/WaermesenkeClass.cs` | 2 | 23 | **21** | 20 | 2 (Engine-Protokoll) |
| `Views/Simulation/Form_KonfigPufferspeicher.cs` | 5 | 1 | **−4** | 1 | 0 |
| | **31** | **349** | **+318** | **233** | |

`Form_KonfigPufferspeicher` verliert Fundstellen, weil die dortige vierte Kopie der
Erzeugerzuordnung entfallen ist (Abschnitt 18).

### 16.1 Was in den einzelnen Dateien passiert ist

**`Form_Simulation_Detail.cs`** — der größte Brocken. Umgestellt sind die
Spaltenköpfe aller sechs Ergebnistabellen (Heizkessel, BHKW, Solarthermie,
Photovoltaik, Wärmepumpe, Pufferspeicher), beide CSV-Export-Knöpfe samt Mouseover und
Kopfzeilen, alle 13 MessageBoxen, die Menüliste links, die Legende und die fünf
Segmente des Übersichts-Kreisdiagramms, sämtliche Diagrammtitel und Achsen, die neun
Brennstoffzeilen des BHKW und die Pendelspeicher-Beschriftung.

Dazu die **Drei-Schichten-Trennung an der Stelle, die das Etappe-2-Protokoll
ausdrücklich benannt hat** (Abschnitt 13, Punkt 3): In `UpdateTabPages` stehen Vergleich
und Anzeige im selben `if`-Block. Der Vergleich läuft jetzt gegen `DbWerte.ERZEUGER_*`,
die unmittelbar folgende `new ListViewItem(…)` bekommt den Ressourcenschlüssel — vier
Wärmeerzeuger plus Photovoltaik und Stromspeicher.

**Chart-Serien auf technische Schlüssel**, Muster aus L6: neun `private const string`
(`HEIZWAERMEBEDARF`, `WARMWASSERBEDARF`, `HEIZSTAB`, `WAERMEPRODUKTION`, `WAERMEBEDARF`,
`STROMBEDARF`, `SPEICHERFUELLSTAND`, `UEBERSCHUSS`, `PHOTOVOLTAIK`), zwei
`SerieAnlegen`-Überladungen (Vektor und `PointF[]`), der Anzeigetext ausschließlich in
`Series.LegendText`. Alle Nachschlagestellen sind nachgezogen —
`chart_PV.Series.IndexOf(…)` und `_chartManager[9]._chart.Series[…]` in den beiden
Checkbox-Handlern der PV-Seite wären sonst mit einer übersetzten Legende ins Leere
gelaufen. Nebenbei ist damit dieselbe Uneinheitlichkeit beseitigt wie in
`NavigatorWaerme`: Diagramm 4 hieß „Wärmebedarf" mit Umlaut, die Diagramme 8 und 10
„Waermebedarf" ohne.

**`Form_Waermesenke.cs`** — vollständig umgestellt (Fenstertitel, beide Gruppen,
Radiobuttons, alle Auswahlfelder, die Prioritätslisten, der Positionstext „Lädt als n.
von m", alle vier MessageBoxen). Die Rollennamen „Hauptsenke"/„Zweitsenke", die als
Platzhalter in die Fehlermeldungen wandern, kommen aus `SIM_ROLLE_*`; die
Steuerlogik arbeitet unverändert mit `WaermesenkeClass.ZIEL_*` und
`WaermequelleClass.SENKE_*` — Persistenzwerte, die nicht übersetzt werden.

**`Form_QuelleErdreich.cs`** — vollständig umgestellt. Zwei Besonderheiten:
Die Zeichenkette „1,5" mit hartkodiertem Dezimalkomma ist durch die
`Vorgabe(double)`-Hilfsfunktion aus L3 ersetzt (`ErdreichTemperatur.TIEFE_DEFAULT`,
formatiert mit `"0.##"` wie alle übrigen Ausgaben des Dialogs). Und die
Bodenkennwertzeile setzt die **Formatangaben des Quelltexts** (`0.0`, `0.00`) jetzt auf
die *Werte* statt auf die Formatzeichenkette — der Katalog führt die Platzhalter
normalisiert, die Zahlendarstellung bleibt dadurch unverändert.

**`NavigatorStrom.cs`** — sieben technische Serienschlüssel, `SerieAnlegen` wie in
`NavigatorWaerme`, CSV-Export und Diagrammbeschriftung. „PV" bleibt zugleich Schlüssel
und Anzeigetext: das Kürzel ist in beiden Sprachen dasselbe, ein eigener `LegendText`
wäre eine Ressource mit zweimal demselben Wert.

**`NavigatorUebersicht.cs`** — beide Donut-Diagramme (Segmentnamen und Kachelüberschriften),
die beiden KPI-Kacheln, die Tabellenüberschrift und die fünf Zeilen der Ergebnistabelle.
Die `Name`-Eigenschaften der beiden DataGridView-Spalten („Erzeuger", „Ergebnis")
bleiben technische Zugriffsschlüssel; übersetzt wird `HeaderText`.

**`DashboardForm.cs`** — drei technische Serienschlüssel, die drei Legendentexte, beide
Achsentitel und die vier Anzeigetexte. Die **Monatsnamen kommen jetzt aus
`CultureInfo.CurrentUICulture`** statt aus `CurrentCulture` — dieselbe Festlegung wie in
L3 (`Form_Quellprofil`): der Monatsname ist Anzeige und folgt der Oberflächensprache.
`CurrentCulture` bleibt unangetastet.

**`TabNavigationManager.cs`** — die vier Navigationsknöpfe. Die `.resx` legt Umbrüche als
LF ab, der Bestand setzte `Environment.NewLine`; deshalb steht an jeder der vier Stellen
`.Replace("\n", Environment.NewLine)` (Muster aus L4).

### 16.2 Designer-gebundene Texte — programmatisch statt `Localizable`

`NavigatorStrom`, `NavigatorUebersicht`, `NavigatorWaerme` und `DashboardForm` tragen
ihre Beschriftungen in der Designer-`.resx`. Der Auftraggeber hat entschieden, diese
Dateien **nicht** auf `Localizable` umzubauen: Eine solche Ressource trägt je Kultur auch
Position und Größe, und ein Handumbau ohne den WinForms-Designer verschöbe
Steuerelemente.

Stattdessen setzt jede der vier Klassen die Texte in einer eigenen Methode
`BeschriftungenSetzen()` **im Konstruktor, direkt nach `InitializeComponent()`**, aus dem
Katalog. Die Designer-Fassung bleibt als deutsche Entwurfszeit-Vorbelegung stehen.

| Klasse | gesetzte Steuerelemente |
|---|---|
| `NavigatorStrom` | 7 Serien-Checkboxen + Entwurfszeit-Titel des Charts |
| `NavigatorWaerme` | 7 Serien-Checkboxen + Entwurfszeit-Titel — **schließt Punkt 3 aus Abschnitt 13** |
| `NavigatorUebersicht` | `bt_WaermebedarfUebersicht` |
| `DashboardForm` | `groupPV`, `groupST`, `lblSpeicherInfo` |

> **Reihenfolge ist in `NavigatorWaerme` wesentlich.** `BeschriftungenSetzen()` steht dort
> **vor** `InitPufferCheckBox()`, weil diese die programmatischen Steuerelemente an
> `checkBox_BHKW.Right` bzw. `checkBox_Waermebedarf.Right` ausrichtet — und die Breite
> hängt am Text. Auf deutscher Oberfläche ändert sich nichts (zeichengleiche Texte), auf
> englischer rücken Checkbox und Auswahlliste passend mit.

**Nicht gesetzt** wurden: `NavigatorUebersicht.label_1`/`label_2` (beide stehen im
Designer auf `Visible = false` und werden nirgends eingeblendet), die reinen
Entwurfszeit-Vorbelegungen in `DashboardForm` (`lblNutzungsgradST`, `lblCO2`, `lblTest` —
sie werden von `UpdateSimulationData` vor der ersten Anzeige überschrieben) und der
Fenstertitel von `DashboardForm` (das Formular wird mit `TopLevel = false` und
`FormBorderStyle.None` eingebettet, seine Titelzeile ist nie sichtbar).

### 16.3 Nachtrag: `WaermesenkeClass.Pruefen` war noch deutsch

Beim Restzählen fiel auf, dass die **Prüf- und Anzeigefunktionen** von
`WaermesenkeClass` in Etappe 2 nicht mitgenommen worden waren, obwohl der Katalog die
Schlüssel führt (`SIM_KEIN_PUFFER_GEWAEHLT`, `SIM_PUFFER_VERWENDUNG_PASST_NICHT`,
`SIM_PUFFER_QUELLE_UND_SENKE`, `SIM_ZWEITSENKE_GLEICH_HAUPTSENKE`,
`SIM_KEIN_BRAUCHWASSERBEDARF`, `SIM_HEIZKREIS*`, `SIM_PUFFER_*_KURZ`,
`SIM_ZIEL_PUFFERSPEICHER_*`, `SIM_KEINE_SENKENDATEN`, `SIM_PUFFER_MIT_VOLUMEN`).
Die L6-Bilanz in Abschnitt 9 wies für diese Datei nur 2 Fundstellen aus.

Das ist nachgeholt (21 Fundstellen). Es war **nicht optional**: Diese Meldungen erscheinen
in genau den Dialogen, die diese Etappe übersetzt hat — `Form_Waermesenke` reicht
`erg.Fehler` unverändert an die MessageBox durch. Ohne den Nachtrag hätte eine englische
Oberfläche einen englischen Fenstertitel über einer deutschen Meldung gezeigt.

Dabei ist zugleich der in Etappe 1 (Abschnitt 5.5) angemeldete Vorbehalt für diese
Meldungen erledigt: Die eingesetzten Verwendungswerte laufen über
`WaermesenkeClass.VerwendungAnzeige(...)` und sind damit übersetzt, statt als deutscher
Persistenzwert in einer englischen Meldung zu stehen.

## 17. Ressourcenkatalog: +11 Schlüssel

Alle elf sind **additiv** in beiden `.resx` und in `Resource.Designer.cs` ergänzt.
Bestand jetzt **541 Schlüssel** (vorher 530).

| Schlüssel | DE | EN | Grund |
|---|---|---|---|
| `CHART_LEGENDE_HEIZWAERMEBEDARF` | Heizwärmebedarf | Space heating demand | Legendentext, hing bisher am Seriennamen (Glossar Z. 43) |
| `CHART_LEGENDE_WARMWASSERBEDARF` | Warmwasserbedarf | DHW demand | dito; Kürzel DHW nach Glossar |
| `CHART_LEGENDE_WAERMEPRODUKTION` | Wärmeproduktion | Heat generation | dito |
| `CHART_LEGENDE_UEBERSCHUSS` | Überschuss | Surplus | dito (Glossar Z. 147) |
| `CHART_LEGENDE_PROFIL_LASTGANG` | Profil/Lastgang | Profile/load curve | dito; Wortwahl wie `CHART_CSV_PROFIL_LASTGANG` |
| `CHART_TITEL_STROMVERLAUF_JAHRESGANGLINIE` | Stromverlauf Jahresganglinie␠ | Electricity profile, annual load profile␠ | Entwurfszeit-Titel `NavigatorStrom.chart7`; **abschließendes Leerzeichen** wie im Bestand, über `xml:space="preserve"` erhalten |
| `SIM_BTN_WAERMEBEDARF_UEBERSICHT` | Wärmebedarf Übersicht... | Heat demand overview... | Designer-Knopf `NavigatorUebersicht` |
| `SIM_CHK_WAERMEBEDARF_EINBLENDEN` | Wärmebedarf einblenden | Show heat demand | Designer-Checkbox `NavigatorWaerme` |
| `SIM_DASH_GRUPPE_PV` | Photovoltaik Autarkie | Photovoltaic self-sufficiency | Designer-Gruppe `DashboardForm` |
| `SIM_DASH_GRUPPE_ST` | Solarthermie Deckung | Solar thermal coverage | dito |
| `SIM_DASH_SPEICHER_INFO` | Theoretischer Speicher (PV) (kWh): | Theoretical storage (PV) (kWh): | dito |

**Wiederverwendet statt neu angelegt** — der Katalog führt gleiche deutsche Texte unter
einem Schlüssel (Etappe 1, Abschnitt 5.1): `CHART_ACHSE_STROMBEDARF` („Strombedarf") dient
zugleich als Legendentext der Strombedarfs-Serien, `PSP_CHECKBOX_SPEICHERFUELLSTAND`
(„Speicherfüllstand") als Legendentext der PV-Speicherserie, `CHART_LEGENDE_WAERMEBEDARF`
für die drei Wärmebedarfs-Serien und `CHART_SEGMENT_HEIZSTAB` für die vier
Heizstab-Serien.

## 18. Aufräumen: die vierte Erzeugerzuordnung ist weg

`Form_KonfigPufferspeicher.ErzeugerDbWert` — die in Abschnitt 13, Punkt 2 benannte
vierte Kopie — ist entfernt. Die eine Aufrufstelle benutzt jetzt
`ErzeugerKatalog.DbWert(...)`; Verhalten und tolerante Regel für unbekannte Werte sind
identisch (die Kopie kannte fünf Erzeuger, `ErzeugerKatalog` kennt dieselben fünf plus
Photovoltaik und Stromspeicher, die an dieser Stelle nie auftreten). Damit gibt es die
Zuordnung „DB-Wert ↔ Anzeigename" **nur noch einmal im Projekt**. Nebenbei ist der
Fenstertitel der Fehlermeldung dieses Dialogs auf `PSP_TITEL_ZUORDNUNG` umgestellt.

**Suche nach weiteren Duplikaten** — Volltextsuche über alle `KONFIG_*`-Zugriffe und alle
`*Anzeige`-Funktionen des Simulationsbereichs:

| Fundstelle | Bewertung |
|---|---|
| `Form_Simulation_Config.cs:1146`, `.Uebersicht.cs:684` (`KONFIG_GESAMTSYSTEM`) | **keine Zuordnung**, sondern zwei einzelne Anzeigen. Kein Duplikat. |
| `WaermesenkeClass.VerwendungAnzeige`, `.ZielAnzeige` | je die EINE Quelle; `Form_PufferSp_Projekt` und `Form_Simulation_Config.Uebersicht` greifen darauf zu |
| `SimulationPufferspeicher.RolleAnzeige/BezeichnerAnzeige`, `Ladeordnung.ErzeugerName`, `WaermequelleClass.TypAnzeige`, `ErdreichTemperatur.KatalogAnzeige` | je die EINE Quelle, in L6 angelegt |
| `Form_PufferSp_Projekt.VerwendungItem` (Zeile 182–192) | greift auf dieselben zwei Ressourcenschlüssel zu wie `VerwendungAnzeige`. **Keine zweite Wahrheit** (identische Schlüssel), aber eine zweite Fundstelle — als Kandidat für L8 vermerkt, nicht angefasst. |

Weitere Anzeige↔DB-Zuordnungen gibt es im Simulationsbereich nicht.

## 19. Verdeckungsfix `Form_Simulation_Config` — Befund und Behebung

### 19.1 Der Befund des Auftrags ließ sich NICHT bestätigen

Der Auftrag nannte `groupBox_PufferSp` und `lblStatus` als letzte Kandidaten des
Musters aus Commit `d49075e` („BaseForm-Erbe"). Ein Reflexions-Harness (x86, gegen die
gebaute Assembly, drei Fenstergrößen) misst das Gegenteil:

```
Basisklasse             : System.Windows.Forms.Form          <- NICHT BaseForm
AutoScroll              : False
AutoScrollPosition      : {X=0,Y=0}   VScroll sichtbar=False
  -> Ursache aus d49075e (verpasster Scrollversatz) AUSGESCHLOSSEN
```

Das gilt in **allen drei** gemessenen Größen (552, 380 und 300 px Nutzhöhe). `d49075e`
behob den Fall, dass unsichtbar gestartete Steuerelemente den **AutoScroll-Versatz** der
`BaseForm` verpassen. `Form_Simulation_Config` erbt von `Form`, setzt `AutoScroll` nicht
und hat in keiner Größe einen Scrollversatz — das Muster „sichtbar starten, in `OnLoad`
ausblenden" wäre hier wirkungslos.

**`groupBox_PufferSp` hat zudem gar keinen Anzeigeweg.** `RUBRIK_SICHTBAR` steht seit
Paket 2, Etappe A auf `false`; `AktualisierePufferSpSichtbarkeit()` steigt dann sofort
aus, und der einzige verbleibende Auslöser `checkBox_PufferSp` ist selbst
`Visible = false`. Im Harness über den produktiven Weg gemessen:

```
groupBox_PufferSp : Visible=False  (checkBox_PufferSp.Visible=False)
   ok: bleibt ausgeblendet (Konzept 4.4, Etappe A) - kein Anzeigeweg, keine Verdeckung moeglich.
```

Die Gruppe wird von `InitPufferspeicherRubrik` **absichtlich** an den unteren Rand
geparkt (`btn_Speichern.Top - PLATZ_FUSSZEILE`), damit der freiwerdende Bereich an die
Übersicht geht. Sie bleibt unangetastet.

### 19.2 Zwei ANDERE, gemessene Fehler an `lblStatus`

Der Harness fand am selben Formular zwei echte Anzeigefehler — nicht durch einen
verpassten Scrollversatz, sondern durch die **Verankerung**:

```
=== Normalgroesse (wie geoeffnet) ===
  ClientSize    : {Width=1175, Height=552}
  lblStatus     : Bounds={X=585,Y=535,Width=263,Height=20}      -> Unterkante 555
  btn_Speichern : Bounds={X=854,Y=510,Width=193,Height=30}
      BEFUND: lblStatus ragt aus der Nutzflaeche heraus; sichtbarer Anteil Height=17

=== verkleinert auf 380 px Nutzhoehe ===
  lblStatus     : Bounds={X=585,Y=363,Width=263,Height=20}
      BEFUND: lblStatus ragt aus der Nutzflaeche heraus
      BEFUND: lblStatus verdeckt von groupBox_Uebersicht {X=267,Y=109,Width=889,Height=309}
              (ZIndex 4 vor 5)

ERGEBNIS: 5 Befund(e).
```

1. **Unterkante abgeschnitten, immer.** Die Entwurfsposition (y = 390 bei 427 px
   Nutzhöhe) wandert über `Anchor = Bottom` mit, während `InitPufferspeicherRubrik`
   (+105 px) und `ExtrapolationSchalterPlatzieren` (+`fehlt`) die Nutzhöhe erhöhen **und**
   die Zeile zusätzlich absolut verschieben. Ergebnis: Unterkante 555, Nutzfläche endet
   bei 552 — die letzten drei Pixel der Meldung „✔ Konfiguration erfolgreich
   gespeichert" fehlten.
2. **Verdeckung beim Verkleinern.** Der Dialog ist `Sizable` und hat keinen
   Scrollbereich. Zieht man ihn kleiner, wandert die untenverankerte Statuszeile nach
   oben über `groupBox_Uebersicht` — und die stand mit Z-Index 4 **vor** der Zeile
   (Z-Index 5).

### 19.3 Die Behebung

Neu in `Form_Simulation_Config.cs`: `StatuszeileAusrichten()`, aufgerufen im Konstruktor
**nach** `InitPufferFusszeile()` — dort verschiebt `ExtrapolationSchalterPlatzieren` die
Zeile zuletzt.

- **Senkrecht auf die Knopfzeile zentrieren** statt fester Absolutposition. Die Knöpfe
  werden von beiden Umbauschritten ohnehin nachgezogen und liegen damit garantiert in
  der Nutzfläche.
- **`lblStatus.BringToFront()`** — kein Geschwister kann die Zeile mehr verdecken.
- **`MindestgroesseFestlegen()`** setzt `MinimumSize` auf die Größe des fertig
  aufgebauten Inhalts, **gedeckelt auf die Arbeitsfläche des Bildschirms**. Dieselbe
  Vorgehensweise wie `BaseForm.OnLoad` („automatische Mindestgröße"), nur ohne den
  dortigen Scrollbereich. Die Deckelung verhindert, dass der Dialog auf einem kleinen
  Bildschirm größer als dieser wird und sich dann nicht mehr verkleinern ließe.

### 19.4 Harness-Beweis nachher

Derselbe Harness, dieselben drei Größen, gegen die neu gebaute Assembly:

```
=== Normalgroesse (wie geoeffnet) ===
  lblStatus : Bounds={X=585,Y=515,Width=263,Height=20}
      ok: lblStatus liegt vollstaendig in der Nutzflaeche {X=0,Y=0,Width=1175,Height=552}
      ok: lblStatus hat ZIndex 0 - kein verdeckendes Geschwister-Control

=== verkleinert auf 380 px / 300 px Nutzhoehe ===
      ok: lblStatus liegt vollstaendig in der Nutzflaeche
      ok: lblStatus hat ZIndex 0 - kein verdeckendes Geschwister-Control

ERGEBNIS: BESTANDEN - lblStatus vollstaendig sichtbar und unverdeckt,
                     Fusszeile aus Paket 8 vollstaendig.
```

### 19.5 Keine Regression an der Paket-8-Fußzeile

Der Harness prüft die vier von Paket 8 eingebauten Steuerelemente in jeder Größe mit:
vorhanden, sichtbar, Text gesetzt, vollständig **oberhalb** der Knopfzeile.

```
label_PufferListe          : Visible=True  Bounds={X=267,Y=424,W=889,H=20}  Text=gesetzt  ueber der Knopfzeile=True
btn_PufferVerwalten        : Visible=True  Bounds={X=267,Y=448,W=240,H=28}  Text=gesetzt  ueber der Knopfzeile=True
checkBox_KaskadeZweikanalig: Visible=True  Bounds={X=811,Y=454,W=219,H=23}  Text=gesetzt  ueber der Knopfzeile=True
checkBox_Extrapolation     : Visible=True  Bounds={X=811,Y=481,W=264,H=23}  Text=gesetzt  ueber der Knopfzeile=True
```

Werte **vor und nach** dem Fix identisch — `InitPufferFusszeile` und
`ExtrapolationSchalterPlatzieren` sind unverändert; die neue Methode liest deren
Ergebnis nur aus.

## 20. Verifikation

### 20.1 Build

```
MSBuild.exe WP-Plan.sln -p:Configuration=Debug -p:Platform=x86
MSBuild.exe Referenzlauf\Referenzlauf.csproj -p:Configuration=Debug -p:Platform=x86
```

**0 Fehler, exakt 6 Bestandswarnungen** — dieselben wie in Etappe 1 und 2:
2 × CS0108 (`WErzeugerModel.ID_Projekt`, `StromverbraucherStammCtrl.items`),
2 × CS0109 (`KlimaregionStammCtrl.rows`, `.items`), 1 × CS4014 und 1 × CS1998
(beide `MDIMainForm.cs`). `Referenzlauf.csproj` ebenfalls 0 Fehler.

> **Ein Fehler, der beim ersten Bauen auffiel und behoben ist:**
> `TabNavigationManager.cs` liegt als einzige Datei des Bereichs **außerhalb** des
> Namensraums `WindowsFormsApplication1` (globaler Namensraum, nur `using`). Dort löst
> `MyResource.Resource` nicht auf; die vier Zugriffe sind deshalb voll qualifiziert
> (`WindowsFormsApplication1.MyResource.Resource.…`).

### 20.2 Regressionslauf — aus einem eigenen Arbeitsbaum

Während dieser Etappe hat der Anwender im Arbeitsverzeichnis **parallel gearbeitet**
(`SchemaKatalog.cs`, `SchemaMigration.cs`, `WErzeugerCtrl.cs`, `MDIMainForm.cs`,
`Form_Start.cs`, `Form_PufferSp.cs` — zwischen 12:56 und 13:00 geändert). Zwei dieser
Dateien gehören zur Schema-Migration, die die Referenzlauf-Suite auf ihrer Arbeitskopie
ausführt.

Die Verifikation läuft deshalb **nicht aus dem Arbeitsverzeichnis**, sondern aus zwei
eigenen git-Arbeitsbäumen auf `925c37f`:

| Arbeitsbaum | Inhalt |
|---|---|
| `head` | reiner Commit-Stand — hat die Basis `2026-08-15_B2` gerechnet |
| `mine` | Commit-Stand **plus ausschließlich die 14 Dateien dieser Etappe** |

Damit sind die parallelen Änderungen des Anwenders aus beiden Seiten des Vergleichs
draußen.

```
Projekt_1007: PASS (29 Dateien, 324210 Werte)
Projekt_1008: PASS (21 Dateien, 227847 Werte)
Projekt_1010: PASS (18 Dateien, 201540 Werte)
Projekt_1011: PASS (29 Dateien, 324232 Werte)
Projekt_1017: PASS (20 Dateien, 245378 Werte)
Projekt_1018: PASS (19 Dateien, 210343 Werte)
Projekt_1021: PASS (21 Dateien, 227840 Werte)
Projekt_1023: PASS (25 Dateien, 262917 Werte)
Projekt_1024: PASS (26 Dateien, 271680 Werte)

GESAMT: PASS (2.295.987 Werte innerhalb der Toleranz)

CSV-Dateien byte-/MD5-gleich : 208
CSV-Dateien abweichend       : 0
CSV-Dateien fehlend          : 0
```

**9 von 9 PASS, 208 von 208 byte-identisch** gegen `Referenzlaeufe/2026-08-15_B2`.

> **Gegenprobe aus dem vollständigen Arbeitsverzeichnis.** Zum Abschluss wurde derselbe
> Lauf noch einmal aus dem echten Arbeitsverzeichnis gerechnet — also **mit** den
> parallelen Änderungen des Anwenders und dem inzwischen dazugekommenen Commit `e8836c6`
> („UI-Sichttest-Fixes: Projektkontext nach Neu/Bearbeiten, Pufferspeicher-Moduldaten per
> ID"). Ergebnis: ebenfalls **208 von 208 byte-identisch**. Die Trennung über die
> Arbeitsbäume war eine Vorsichtsmaßnahme, keine Notwendigkeit — auch die
> Schema-Migrationsarbeit des Anwenders verändert die Referenzergebnisse (noch) nicht.

### 20.3 Sprachgleichheit DE ↔ EN

Derselbe Arbeitsbaum, dieselben neun Projekte, einmal ohne und einmal mit
`EPOS_REFLAUF_UICULTURE=en-US`:

```
SPRACHGLEICHHEIT DE vs EN : 208 byte-gleich, 0 abweichend
EN gegen Basis (Toleranz) : 9 von 9 PASS, 2.295.987 Werte
```

**208 von 208 Ergebnisdateien byte-identisch** über alle neun Projekte. Damit ist
nachgewiesen, dass keiner der 318 neu umgestellten Texte als Steuerwert dient.

### 20.4 Ressourcen-Ladeprüfung und Platzhalterproben

Testtreiber außerhalb des Repos, lädt die gebaute Assembly per `Assembly.LoadFrom` und
arbeitet über Reflexion:

```
Ressourcenbloecke geprueft : 94
Einzelabrufe geprueft      : 34.545   (jeder Zeichenketten-Schluessel unter Invariant, de-DE, en-US)
resx neutral / en-US       : 541 / 541
MyResource.Resource        : 541 Schluessel / 541 Eigenschaften
Leere Werte de-DE / en-US  : 0 / 0
Eintraege mit Platzhaltern : 103
Probeformatierungen        : 206      (je Eintrag neutral UND en-US)
DbWerte-Konstanten         : 51
ERGEBNIS: BESTANDEN - kein Fehler.
```

Geprüft wird damit: kein Block, der sich nicht laden lässt; kein Schlüssel, der unter
einer Kultur `null` liefert; Gleichstand `.resx` ↔ `Resource.Designer.cs`; gleiche
Schlüsselmenge in beiden `.resx`; für **jeden** Eintrag mit `{n}` eine Probeformatierung
in beiden Sprachen **und** der Vergleich der Platzhalter*nummern* zwischen neutral und
en-US (eine Übersetzung mit vergessenem `{1}` fiele hier auf). Zusätzlich: die zwölf
Leitkonstanten aus `DbWerte` sind unverändert deutsch.

Fokusprobe der **elf neuen Schlüssel** — jeder liefert in beiden Sprachen einen Wert:

```
CHART_LEGENDE_HEIZWAERMEBEDARF             DE=[Heizwärmebedarf]  EN=[Space heating demand]
CHART_LEGENDE_PROFIL_LASTGANG              DE=[Profil/Lastgang]  EN=[Profile/load curve]
CHART_LEGENDE_UEBERSCHUSS                  DE=[Überschuss]  EN=[Surplus]
CHART_LEGENDE_WAERMEPRODUKTION             DE=[Wärmeproduktion]  EN=[Heat generation]
CHART_LEGENDE_WARMWASSERBEDARF             DE=[Warmwasserbedarf]  EN=[DHW demand]
CHART_TITEL_STROMVERLAUF_JAHRESGANGLINIE   DE=[Stromverlauf Jahresganglinie ]  EN=[Electricity profile, annual load profile ]
SIM_BTN_WAERMEBEDARF_UEBERSICHT            DE=[Wärmebedarf Übersicht...]  EN=[Heat demand overview...]
SIM_CHK_WAERMEBEDARF_EINBLENDEN            DE=[Wärmebedarf einblenden]  EN=[Show heat demand]
SIM_DASH_GRUPPE_PV                         DE=[Photovoltaik Autarkie]  EN=[Photovoltaic self-sufficiency]
SIM_DASH_GRUPPE_ST                         DE=[Solarthermie Deckung]  EN=[Solar thermal coverage]
SIM_DASH_SPEICHER_INFO                     DE=[Theoretischer Speicher (PV) (kWh):]  EN=[Theoretical storage (PV) (kWh):]
Neue Schluessel: 11   ohne Wert: 0
```

### 20.5 Hardcoding-Restzählung

Volltextsuche über die zwölf in dieser und der letzten Etappe bearbeiteten Dateien des
Bereichs; gezählt werden Zeichenkettenliterale außerhalb von Kommentarzeilen, ohne reine
Bezeichner, Zahlen und Formatangaben. **43 Treffer, davon 0 unbegründet:**

| Gruppe | Anzahl | Begründung |
|---|---:|---|
| SQL-Fragmente und Spaltenlisten | 15 | Datenzugriff, laut L2 außerhalb des Katalogs |
| Schriftartnamen (`"Segoe UI"`) | 5 | technisch, sprachneutral |
| Kommentartext **hinter** Code auf derselben Zeile | 8 | Scanner-Treffer, kein Laufzeittext |
| Einheiten- und Trennzeichen-Verkettung (`" l)"`, `" h/a"`, `" — "`, `" · "`, `": "`, `"\r\n  "`, `" %"`) | 11 | sprachneutral; die eingesetzten Teile kommen bereits übersetzt |
| Datums-/Zeitformat `"dd/MM H:mm ["` | 2 | Formatmuster, sprachneutral |
| `HilfeKontext.SetzeBereich(...)` | 2 | Bedienkontext für den KI-Assistenten; in L2 ausdrücklich vom Katalog ausgenommen |

**Bleibt genau ein echter Rest, dokumentiert statt umgestellt:**

| Stelle | Text | Grund |
|---|---|---|
| `WaermesenkeClass.cs:460-467` | zwei `SimulationProtokoll`-Warnungen zur Senkennormalisierung | Gehören zum **Engine-Protokollkanal**, für den der Katalog nur 29 der rund 70 Meldungen führt (Etappe 2, Abschnitt 9.5). Ohne Katalogeintrag wäre eine Übersetzung eine Erfindung. Offener Punkt, siehe Abschnitt 21. |

Der Bereich `Allgemein/Simulation/` außerhalb von `WaermesenkeClass` wurde **nicht**
umgestellt und ist in dieser Zählung nicht enthalten — dort liegen die übrigen
Engine-Protokollmeldungen ohne Katalogschlüssel (siehe Abschnitt 21, Punkt 1).

### 20.6 Kodierung und Zeilenenden

Alle 14 geänderten Quelldateien behalten ihre Kodierung: **UTF-8 mit BOM**, kein
Mojibake, geprüft mit einem eigenen Prüfer. Die drei Dateien mit reinen LF-Zeilenenden
(`WaermesenkeClass.cs`, `Form_QuelleErdreich.cs`, `Form_Waermesenke.cs`) behalten LF; die
übrigen CRLF. Ein beim Skripteinsatz eingeschlepptes CRLF in `WaermesenkeClass.cs`
(Zeile 151) und 15 zusätzliche Leerzeilen wurden zurückgenommen und nachgemessen:

```
WaermesenkeClass.cs     UTF8-BOM  LF        leere Plus-Zeilen im Diff: 0
Form_QuelleErdreich.cs  UTF8-BOM  LF        leere Plus-Zeilen im Diff: 0
Form_Simulation_Detail  UTF8-BOM  CRLF      leere Plus-Zeilen im Diff: 0
```

## 21. Offene Punkte für die Reviews, L7 und L8

1. **Engine-Protokollmeldungen ohne Katalogschlüssel — der größte verbliebene Block.**
   Der Katalog führt 29 `SIMENG_*`-Schlüssel; die Engine erzeugt deutlich mehr
   Protokolltexte. Betroffen sind `SimulationControl.cs`, `SimulationBHKW.cs`,
   `SimulationWaermebedarf.cs`, `WaermequelleClass.cs`, `ErdreichAuswertung.cs`,
   `VDI4640Pruefung.cs`, `ErdreichTemperatur.cs` sowie die zwei Reststellen in
   `WaermesenkeClass.cs`. Das ist eine eigene Katalogerhebung (L2-Nachtrag) plus Umbau
   — nicht Teil dieses Auftrags.

   > **Berichtigt und neu gefasst in Abschnitt 25.11 (a).** `ErdreichAuswertung.cs`,
   > `VDI4640Pruefung.cs` und `ErdreichTemperatur.cs` gehören **nicht** in diese Liste — dort
   > waren die 41 Schlüssel längst vorhanden, es fehlte nur die Anbindung (in der Nacharbeit
   > erledigt). Und der verbleibende Block ist kein Übersetzungsproblem, sondern ein
   > Kanalproblem: rund 50 `Console.WriteLine` laufen am Protokollkanal aus Paket 8 vorbei.
2. **`richTextBox_Info.Text` in `Form_Simulation_Detail` ist nicht übersetzt.**
   *(Erledigt in der Nacharbeit, Abschnitt 25.5 — der neutrale Eintrag ist Klartext, kein RTF.)*
   Der Erklärtext zu den drei BHKW-Betriebsarten steht in der **neutralen** `.resx` des
   Formulars; `Form_Simulation_Detail.en-US.resx` (240 Einträge) kennt ihn nicht. Die
   drei Suchbegriffe für den Fettdruck kommen jetzt aus dem Katalog
   (`SIM_BETRIEBSART_*`) — auf englischer Oberfläche findet `IndexOf` sie im deutschen
   Text nicht, es bleibt also beim unformatierten deutschen Erklärtext. Kein Fehler,
   keine Ausnahme; die saubere Lösung ist ein `richTextBox_Info.Text`-Eintrag in der
   `en-US.resx` über den WinForms-Designer.
3. **UI-Sichttest auf englischer Oberfläche steht weiter aus** (Punkt 1 aus Abschnitt 13,
   unverändert). Neu hinzu kommen: die sieben Serien-Checkboxen beider Navigatoren
   (jetzt übersetzt, feste Positionen aus dem Designer), die Legenden der elf
   Diagramme in `Form_Simulation_Detail`, die Spaltenbreiten der sechs Ergebnistabellen
   (`-2` = automatisch, unkritisch) und die vier Navigationsknöpfe des
   `TabNavigationManager` (feste Zellen im TableLayoutPanel).
4. **`Form_PufferSp_Projekt.VerwendungItem`** greift auf dieselben zwei
   Ressourcenschlüssel zu wie `WaermesenkeClass.VerwendungAnzeige` — keine zweite
   Wahrheit, aber eine zweite Fundstelle. Zusammenlegen wäre eine Aufräumarbeit für L8.
5. **Bestandsübersetzungen** von `Form_Simulation_Config.en-US.resx` und
   `Form_KonfigPufferspeicher.en-US.resx` widersprechen weiter dem Glossar
   („buffer memory", „producer") — Punkt 4 aus Abschnitt 13, unverändert.
   *(Für `Form_Simulation_Config.en-US.resx` erledigt, Abschnitt 25.3;
   `Form_KonfigPufferspeicher.en-US.resx` und `Form_Simulation_Detail.en-US.resx`
   stehen weiter aus — siehe Abschnitt 26.)*
6. **43 verwaiste Einträge** in `Form_Simulation_Config.en-US.resx` — Punkt 5 aus
   Abschnitt 13, unverändert. *(Erledigt in der Nacharbeit, Abschnitt 25.4; es sind
   43 Einträge auf 28 Steuerelementen, nicht auf 16.)*
7. **Zeilenenden** (jetzt 3 der geänderten Dateien mit LF) und **Encoding-Baustellen
   außerhalb des Simulationsbereichs** — unverändert.
   *(Geschlossen bzw. erledigt, Abschnitt 25.11 g und 25.10.)*
8. **`Form_Simulation_Config` erbt nicht von `BaseForm`.** Das ist der Grund, warum das
   Muster aus `d49075e` hier nicht greift (Abschnitt 19.1) — und zugleich der Grund,
   warum der Dialog ohne `MinimumSize` beliebig klein gezogen werden konnte. Die neue
   `MindestgroesseFestlegen()` deckt das ab; ob das Formular langfristig auf `BaseForm`
   umgestellt werden soll (dann mit Scrollbereich), ist eine Architekturentscheidung.
   Dieselbe Frage steht laut `d49075e` noch für `Form_Gebaeude`,
   `Form_Solarganglinie_Admin`, `Form_AdminWaermeeinlesen` und `Form_WPAuswahl`.

## 22. Was diese Etappe NICHT getan hat

- **Kein Commit.** Der Arbeitsstand liegt unkommittiert im Arbeitsverzeichnis.
- **Gesperrte Dateien unberührt:** `Controller/WizardCtrl.cs`, `Model/WErzeugerModel.cs`,
  `Views/BHKW/Form_BHKWEing.cs`, `Views/Heizkessel/Form_Heizkessel.cs`,
  `Views/Wizard/WizardParent.cs`; ebenso `Referenzlaeufe/2026-08-14_B0/lauf_protokoll.md`,
  `DB-Backup/` und alle bestehenden `Referenzlaeufe/*`-Ordner. Neu angelegt wurde
  ausschließlich `Referenzlaeufe/2026-08-15_B2/`.
- **Die parallel geänderten Dateien des Anwenders nicht angefasst** — sie sind aus der
  Verifikation herausgehalten (Abschnitt 20.2), nicht verändert.
- **Keine `.Designer.cs` und keine Formular-`.resx` geändert** — auch nicht additiv. Die
  Designer-Texte werden programmatisch überschrieben (Abschnitt 16.2).
- **`CurrentCulture` nicht gesetzt.** Nur `CurrentUICulture`; die
  Monatsnamen-Umstellung in `DashboardForm` liest `CurrentUICulture`, sie setzt nichts.
- **Registry nicht angefasst.** Der Sprachtest lief über `EPOS_REFLAUF_UICULTURE`.
- **Produktive Datenbank nur lesend** benutzt (über die Arbeitskopie der Suite).

---

# Nachtrag L8 — Regelverankerung und Prüfrezeptur

**Stand: 15.08.2026, nachgetragen im Review zu `97183a2`.** L8 verlangt zwei Dinge, die bis
dahin offen waren: die Drei-Schichten-Regel in der `CLAUDE.md` und eine „Build-Prüfung gegen
neue Hardcodings".

## 23. Drei-Schichten-Regel in der `CLAUDE.md`

Konzept 13.6 sagt ausdrücklich: „Diese Regel gehört in `CLAUDE.md`, damit sie in künftigen
Arbeitssitzungen erhalten bleibt." Sie stand dort bis zu diesem Nachtrag **nicht** — weder in
der Wurzel-`CLAUDE.md` noch in der des Anwendungsprojekts.

Ergänzt im Abschnitt **Konventionen** von
[`WindowsFormsApplication1/CLAUDE.md`](../../CLAUDE.md): Persistenz deutsch und eingefroren
über `Allgemein/DbWerte.cs`, Schlüssel sprachneutral und ASCII, Anzeige ausschließlich über
`MyResource.Resource.*`, Verweis auf `Lokalisierung_Katalog.md` und auf die Prüfrezeptur.
Kein Anzeigetext darf Steuerwert sein.

## 24. Statt eines Analyzers: eine Prüfrezeptur

Ein Roslyn-Analyzer ist für diesen Bestand **überzogen**: Er müsste je Zeichenkette
entscheiden, ob sie Anzeige, Schlüssel, SQL-Fragment, Spaltenname oder Diagnoseausgabe ist —
eine Unterscheidung, die die Abschnitte 2, 5.3, 9.5, 12.6 und 20.5 dieses Protokolls von Hand
und mit Begründung treffen. Mechanisiert ergäbe das entweder Dutzende Fehlalarme pro Build
oder eine Unterdrückungsliste, die den Analyzer wirkungslos macht.

Stattdessen liegt neben dem Katalog
[`Lokalisierung_Pruefung.md`](Lokalisierung_Pruefung.md) mit **sechs wiederholbaren
Prüfungen** samt fertigen Befehlen: P1 neue hartkodierte Anzeigetexte, P2 Anzeigetext als
Steuerwert (Muster B0-9/B0-10/B0-11), P3 Persistenzwerte als Literal, P4 Katalog-Gleichstand
`.resx` ↔ `.resx` ↔ `Designer`, P5 Kodierung/BOM/Mojibake, P6 Sprachgleichheitslauf als
harte Laufzeitprobe.

**Einmal ausgeführt auf `97183a2`**; der Ist-Stand steht im letzten Abschnitt jener Datei.
Zusammengefasst: P2, P3, P4 und P6 ohne Befund (P4 541/541/541, P6 208/208 byte-identisch in
beiden Sprachen). P1 findet in den Views **keinen** benutzersichtbaren deutschen Text mehr;
die 28 verbleibenden Fundstellen liegen sämtlich in `Allgemein/Simulation/` und sind der
bereits benannte offene Punkt (Abschnitt 21, Punkt 1). P5 meldet **einen neuen, kleinen
Befund**: vier `Views/*/…en-US.resx` und `WindowsFormsApplication1.csproj` tragen keine BOM
und verletzen damit die mit L0 eingeführte `.editorconfig` — fachlich unkritisch (die
XML-Deklaration trägt `encoding="utf-8"`), aber Visual Studio zieht sie beim nächsten
Speichern unprotokolliert nach. Nachzuholen, wenn diese Dateien ohnehin angefasst werden
(offene Punkte 5 und 6 aus Abschnitt 21 betreffen genau zwei davon).

---

# Nacharbeit — konsolidierte Behebung der beiden Paket-9-Reviews

**Stand: 15.08.2026, unkommittiert auf `97183a2`.** Die beiden Reviews zu Paket 9 haben neun
Befunde geliefert (drei ERNST, fünf GERING, einer rein dokumentarisch). Dieser Abschnitt hält
fest, was daraus geworden ist — einschließlich der zwei Stellen, an denen die Messung dem
Befund widerspricht.

## 25. Nacharbeit

### 25.1 N1 — 41 fertig übersetzte, nicht verdrahtete Katalogschlüssel angeschlossen

Der größte der drei ERNST-Befunde und zugleich der billigste: Die Schlüssel lagen seit L2 mit
deutschem **und** englischem Wert im Katalog, im Quelltext stand aber weiter das Literal. Es
fehlte nur die Anbindung.

| Datei | Stellen | Wie |
|---|---:|---|
| `ErdreichTemperatur.cs:158-198` | 13 | Der Bodentyp-Katalog trägt in Spalte 2 jetzt den **Ressourcenschlüssel** statt des deutschen Texts (neues Feld `Bodenkennwerte.AnzeigeSchluessel`). `Untergrund` ist vom Feld zur **Eigenschaft** geworden und löst bei jedem Zugriff über `Resource.ResourceManager.GetString` auf — nicht bei der Initialisierung des `static readonly`-Arrays, sonst fröre die Sprache auf den ersten Zugriff ein. |
| `ErdreichTemperatur.cs:471-486` | 1 | `Kennwerte.Zeile()` auf `SIMQ_PROFIL_KENNWERTE_ZEILE`; die Formatangabe `F1` bleibt im Quelltext und wird **vor** dem Einsetzen angewandt (Lesehinweis des Katalogs). |
| `VDI4640Pruefung.cs:267-500` | 14 | `Pruefzeile.Text()` und die Zweige von `PruefeKollektor`/`PruefeSonde`. Die Ausrichtung `{0,-18}` des alten Musters ist als `PadRight(18)`, die Grenzwertdarstellung `{2:0.#}` als `ToString("0.#")` in den Quelltext gewandert. |
| `ErdreichAuswertung.cs:87-200, 320-370` | 13 | `Kurztext()`, `Frosttext()`, die drei `Grenze`-Texte und der Ersatzname `Anlage {0}`. `FROST_NORMBASIS` war eine `public const string` — eine Konstante kann nicht übersetzen; sie ist jetzt eine statische Eigenschaft auf `SIMQ_FROST_NORMBASIS`. |

**Drei-Schichten-Prüfung je Stelle, mit zwei Ausnahmen als Ergebnis:**

- **Bodentyp-ComboBox — risikofrei bestätigt.** Lese- und Schreibweg laufen über den
  **Index** (`Form_QuelleErdreich.cs:427` setzt `SelectedIndex` aus `KatalogIndex(Bodentyp)`,
  `:560` liest `Katalog[SelectedIndex].Schluessel`). Der Anzeigetext ist an keiner Stelle
  Steuerwert. Der Harness prüft den Rundlauf in beiden Sprachen: 13 von 13 Indizes gleich,
  Katalogschlüssel sprachunabhängig.
- **`VDI4640Pruefung.Bodenarten` bleibt deutsch.** „Sand", „Sandiger Ton", „Lehm", „Schluff"
  sind **Steuerwerte** — `BodenartIndex()` sucht darüber die Spalte der Tabelle A2. Sie
  erscheinen als Platzhalter in `SIMQ_VDI4640_GRUNDLAGE_KOLLEKTOR` und
  `SIMQ_ERDREICH_BODENKENNWERTE`; die englische Meldung mischt dort die Sprachen. Umgestellt
  wurde deshalb **nur der Rahmensatz**.
- **`MONATSKUERZEL` bleibt deutsch.** Für Monatsnamen gibt es bewusst keinen Katalogeintrag,
  und `CultureInfo` liefert im Deutschen „Mrz" statt „Mär" — die Umstellung hätte die deutsche
  Anzeige verändert. Siehe 25.7.

**Ein Nebenbefund, der dabei aufgefallen ist:** Der `#if DEBUG`-Selbsttest in
`VDI4640Pruefung.cs:722` prüfte den Klemmungs-Hinweis mit
`Hinweis.IndexOf("außerhalb des kodierten Tabellenbereichs …")` — also gegen ein **deutsches
Literal**. Mit der Übersetzung wäre der Selbsttest auf englischer Oberfläche stillschweigend
fehlgeschlagen. Er vergleicht jetzt gegen den Katalogeintrag.

### 25.2 N2 — `ChartManager.cs` in den Lokalisierungsumfang

`Allgemein/GrafikTools/ChartManager.cs` lag außerhalb des in L2 gezogenen Bereichs, liefert
aber die Achsentitel **aller** Diagramme des Simulationsbereichs.

**(a) Der Achsentitel der Aufrufer wurde überschrieben.** `FormatXAxisWithDate()` setzte hart
`ca.AxisX.Title = "Zeitverlauf (Monate)"`. Von den acht lokalisierten `XAxisTitle`-Zuweisungen
liefen **sechs** über genau diesen Zweig und zeigten deshalb weiter den deutschen Text:

| Aufrufer | Achse | vorher | jetzt |
|---|---|---|---|
| `Form_Simulation_Detail.cs:1670, 1694, 1909` | Datum | „Zeitverlauf (Monate)" | `CHART_ACHSE_JAHRESSTUNDEN` |
| `Form_Simulation_Detail.cs:1953` | Datum | „Zeitverlauf (Monate)" | `CHART_ACHSE_MONATE` |
| `NavigatorStrom.cs:155`, `NavigatorWaerme.cs:240` | Datum | „Zeitverlauf (Monate)" | `CHART_ACHSE_MONATE` |
| `Form_Simulation_Detail.cs:2000` | Zahl | schon richtig | unverändert |
| `Form_Simulation_Detail.cs:1818` | XY | schon richtig | unverändert |

Der Titel wird jetzt nur noch gesetzt, wenn `XAxisTitle` **leer** ist; dann greift
`CHART_ACHSE_MONATE`. Damit die Vorgabe kein deutsches Literal mehr ist, steht das Feld
`XAxisTitle` jetzt auf `""` statt auf „Zeitverlauf (Jahresstunden)" — alle 15 Instanzen im
Projekt setzen den Titel ohnehin selbst.

> **Bewusste Nebenwirkung außerhalb des Simulationsbereichs.** Drei Diagramme mit Datumsachse
> setzen ebenfalls einen Titel, der bisher verschluckt wurde: `Form_ErgBrauchwasserwaerme.cs:181`
> („Jahresstunde") und `Form_Klimadaten.cs:103, 119` („Jahresstunden"). Sie zeigen künftig **ihren
> eigenen** Titel statt „Zeitverlauf (Monate)". Das ist die Absicht der Aufrufer; die drei Texte
> sind hartkodiert deutsch und gehören zu keinem lokalisierten Bereich. Für den UI-Sichttest
> vorgemerkt (Abschnitt 26).

**(b) Tooltip-Literale.** `ChartMouseWheel2.HandleMouseMove` baute den Mouseover-Text mit
`$"Einheit: …"` und `$"Wert: … "`. Beide sind jetzt Katalogeinträge (`CHART_TOOLTIP_EINHEIT`,
`CHART_TOOLTIP_WERT`, in beiden `.resx` und im Designer); die Zahlenformate `0` und `N2`
bleiben im Quelltext.

**(c) Sweep über die restliche Datei.** Weitere benutzersichtbare Literale: **keine**. Was der
Scanner findet, ist begründet: `"0°C"` an der Nulllinie (Einheit, sprachneutral), `"Segoe UI"`
(Schriftart), `"MainLegend"`/`"#LEGENDTEXT"`/`"X"`/`"Y"` (technische Schlüssel), Zahl- und
Datumsformate, ein `Console.WriteLine` (Diagnose, in L2 ausgenommen) und
`$"{values[i]:0.0}%"` im Ringdiagramm.

### 25.3 N3(a) — en-US-Bestandsübersetzungen glossarkonform

`Views/Simulation/Form_Simulation_Config.en-US.resx`, reine `<value>`-Änderungen, keine
Layout- oder Metadaten-Einträge angefasst:

| Eintrag | EN alt | EN neu | Glossar |
|---|---|---|---|
| `label11.Text` | Define generator, assign buffer **memory**: | … assign buffer **storage**: | Pufferspeicher = buffer storage |
| `label7.Text` | Change the buffer **memory**, **forward, backward** | Change the buffer **storage**, **flow, return** | Vorlauf = flow, Rücklauf = return |
| `label12.Text` | Select **producers** in order | Select **generators** in order | Erzeuger = generator |
| `label2.Text` | **Power** generator: | **Electricity** generator: | Strom = electricity, nicht power (= Leistung) |
| `groupBox_PufferSp.Text` | Buffer **memory allocation** | Buffer **storage assignment** | wie `PSP_TITEL_ZUORDNUNG` im Hauptkatalog |
| `checkBox_PufferSp.Text` | Show buffer **memory allocation** | Show buffer **storage assignment** | dito |

Der anschließende Sweep über die restlichen zehn Text-Einträge der Datei ergab **keine
weitere Abweichung** ($this, `btn_Hinzu`, `btn_Loeschen`, `btn_OK`, `btn_Speichern`,
`groupBox_Tools`, `label1`, `label3`, `label21`, `lblStatus` decken sich mit Glossar und
Hauptkatalog). Die längeren englischen Texte können nicht abgeschnitten werden: alle
betroffenen Beschriftungen tragen in der **neutralen** `.resx` `AutoSize = True`, die
`…​.Size`-Einträge des Satelliten werden zur Laufzeit neu berechnet.

### 25.4 N3(b) — 43 verwaiste Einträge entfernt

Alle 43 Einträge aus `Form_Simulation_Config.en-US.resx` entfernt; die Datei geht von 68 auf
**25 echte Einträge** zurück (plus die vier Beispielzeilen des ResX-Schemakopfs).

Vor dem Entfernen wurde **je Steuerelement** geprüft, nicht je Eintrag: die 43 Einträge
verteilen sich auf **28** Namen, und für jeden dieser 28 gilt **0 Treffer** in
`Form_Simulation_Config.cs`, `.Designer.cs` und `.Uebersicht.cs` **und 0 Einträge in der
neutralen `Form_Simulation_Config.resx`**. Der Designer legt genau 28 Steuerelemente an —
keiner der 28 toten Namen ist darunter. Die Zahl „16 Steuerelemente" aus Abschnitt 4.6 ist
damit berichtigt (dort gezählt waren nur die Namen mit `.Text`-Eintrag).

Nach dem Entfernen ist die Datei als XML validiert.

### 25.5 N3(c) — `richTextBox_Info` übersetzt

Der Erklärtext zu den drei BHKW-Betriebsarten liegt in der **neutralen** `.resx` als
**Klartext, nicht als RTF** — die Prüfung des Eintragsformats war die eigentliche Frage des
Befunds. Damit ist der Nachtrag in `Form_Simulation_Detail.en-US.resx` unkritisch: ein
`<data name="richTextBox_Info.Text" xml:space="preserve">` mit demselben Aufbau (drei
Überschriften, drei Absätze, Leerzeile dazwischen).

Die drei Überschriften sind **wortgleich mit den Katalogwerten** `SIM_BETRIEBSART_*`
(„Heat-led (standard)", „Electricity-led (economic)", „Without feed-in (zero export)") —
damit findet `MacheTextAbschnittFett` sie auch auf englischer Oberfläche und der Fettdruck
funktioniert dort erstmals. Offener Punkt 2 aus Abschnitt 21 ist erledigt.

### 25.6 N4 — Beschriftung und Satzform getrennt

`SIM_ROLLE_HAUPTSENKE`/`_ZWEITSENKE` tragen im Englischen die klein geschriebene Satzform
(„main sink"), weil sie als Platzhalter `{0}` in `SIM_KEIN_PUFFER_GEWAEHLT` &Co. eingesetzt
werden (`WaermesenkeClass.cs:547, 560`). Als **Beschriftung** ist das falsch. Neu:
`SIM_GRUPPE_HAUPTSENKE` („Hauptsenke"/„Main sink") und `SIM_SPALTE_ZWEITSENKE`
(„Zweitsenke"/„Secondary sink"), eingesetzt in `Form_Waermesenke.cs:113` (GroupBox),
`Form_Simulation_Config.Uebersicht.cs:225` (Spaltenkopf) und
`Form_PufferSp_Projekt.cs:633-634` (Tabellenzelle). Die beiden Satz-Einsätze bleiben
unverändert.

Damit sind zwei in Etappe 1 zusammengeführte Schlüssel wieder aufgenommen — die
Zusammenführung war eine Gleichheit im **Deutschen**, die im Englischen nicht gilt. Die
Tabelle „Zusammengeführte Schlüssel" im Katalog ist entsprechend berichtigt.

### 25.7 N5 — Volumenfilter: gemessen, teilweise behoben, teilweise widerlegt

Der Befund lautete: `SelectedIndex = 0` in `PufferSpFilter.VolumenfilterFuellen` löse
`SelectedIndexChanged` aus und ändere damit den Öffnungszustand des Dialogs (Sortierung,
NULL-Sätze). **Zwei Messungen widersprechen dem erwarteten Fix:**

1. **Die Bestandsvorbelegung löste dasselbe Ereignis aus.** Vor Paket 9 stand dort
   `comboBox_Volumen.Text = "Alle"`. Der `Text`-Setzer der `ComboBox` sucht den Eintrag in der
   Liste und setzt `SelectedIndex` — gemessen in einem eigenen WinForms-Harness:

   ```
   HandleCreated=True
   A  cb.Text="Alle"     -> SelectedIndexChanged 1x, SelectedIndex=0   (Bestand vor Paket 9)
   B  cb.SelectedIndex=0 -> SelectedIndexChanged 1x, SelectedIndex=0   (Stand Paket 9)
   C  Handler abgeklemmt -> SelectedIndexChanged 0x, SelectedIndex=0
   C' Anwenderwechsel    -> SelectedIndexChanged 1x, SelectedIndex=3
   ```

   `SetFilter()` lief beim Öffnen also **schon vorher**. Sortierung (`order by Bezeichner`) und
   Trefferliste sind gegenüber dem Stand vor Paket 9 unverändert — es gibt hier keine
   Regression aus Paket 9.

2. **Das Ereignis ist an dieser Stelle tragend.** `Form_PufferSp_Load` füllt die rechte Liste
   zuerst aus `Tab_Pufferspeicher` — der **Projekttabelle**. Erst `SetFilter()` ersetzt sie
   durch den **Katalog** `Tab_Pufferspeicher_STAMM`. Ein Abklemmen des Ereignisses beim Füllen
   ließe beim Öffnen die falsche Tabelle im Dialog stehen.

**Deshalb wurde das Ereignis bewusst NICHT unterdrückt** (Begründung als Klassenkommentar an
`VolumenfilterFuellen` hinterlegt). Behoben wurde der zweite Teil des Befunds, und der ist
echt:

> `Gesamtvolumen Like '%'` wandelt die Zahl in Text und vergleicht; für `NULL` ergibt das in
> Jet/ACE wieder `NULL` — ein Katalogsatz ohne gepflegtes Gesamtvolumen fällt aus dem Zweig
> „Alle" heraus, ohne dass irgendwo eine Meldung erscheint. Stufe 0 lautet jetzt
> `(Gesamtvolumen IS NULL OR Gesamtvolumen Like '%')`. Die Klammer ist nötig, weil die
> Aufrufer das Prädikat mit `and` an den Herstellerfilter hängen.

Im Auslieferungskatalog der Arbeitsplatz-Datenbank tritt der Fall derzeit nicht auf (gelesene
Gegenprobe: `Tab_Pufferspeicher_STAMM` hat 2 Sätze, davon 0 mit `Gesamtvolumen IS NULL`) — die
Absicherung wirkt gegen Importe, nicht gegen den Bestand.

### 25.8 N6 — `MindestgroesseFestlegen()` nach `OnShown`

`StatuszeileAusrichten()` läuft im Konstruktor (das muss es auch — die Zeile muss vor dem
ersten Zeichnen sitzen) und rief von dort `MindestgroesseFestlegen()` auf. Deren
`Screen.FromControl(this)` hat zwei Nachteile im Konstruktor: es **erzwingt die Fensterhandle**,
bevor der Aufbau fertig ist, und es misst den **falschen Bildschirm** — `StartPosition` wirkt
erst beim Anzeigen, das Formular steht bis dahin an seiner Entwurfsposition.

Der Aufruf steht jetzt in einem `OnShown`-Override. Harness-Nachweis in beiden Sprachen:

```
nach Konstruktor : MinimumSize={Width=0, Height=0}  IsHandleCreated=False
nach Show()      : MinimumSize={Width=1191, Height=591}  Size={Width=1191, Height=591}
   ok: MinimumSize auf die Arbeitsflaeche des Bildschirms gedeckelt {Width=1280, Height=752}
lblStatus        : Bounds={X=585,Y=515,Width=263,Height=20}  ZIndex=0
   ok: lblStatus vollstaendig in der Nutzflaeche {X=0,Y=0,Width=1175,Height=552}
   ok: lblStatus auf die Knopfzeile zentriert
nach Verkleinern auf 380x300 : Size={Width=1191, Height=591}
   ok: lblStatus auch nach dem Verkleinern vollstaendig sichtbar
```

`lblStatus` liegt auf **exakt denselben Koordinaten** wie im Nachweis aus Abschnitt 19.4, und
die vier Fußzeilen-Steuerelemente aus Paket 8 sind unverändert (19.5). Kein Rückschritt.

### 25.9 N7 — Katalog-Kleinigkeiten

- **`Text_Hinweis` EN „Hint" → „Note".** Vorher geprüft, wo der Schlüssel verwendet wird:
  drei Stellen, alle in `Form_Start.cs` (820, 1176, 1475), alle als **Titel** eines
  `Form_Hinweis`. Genau der im Glossar Kapitel 8 geführte Fall („Hinweis → Note,
  MessageBox-Titel"). Keine Verwendung außerhalb, kein Risiko.
- **`Text_Ausgewaehlt` und `Text_nicht_geoffnet` entfernt.** Repo-weite Suche über alle
  Dateitypen: **0 Referenzen** außer in den beiden `.resx` und im Designer (Treffer in
  `bin/` und `obj/` sind die eingebetteten Ressourcen der letzten Übersetzung).
- **`SIM_SPALTE_PRIO` EN „Prio" → „prio"**, damit Katalog und die in Abschnitt 5.5
  festgehaltene Entscheidung („Prio / WP-Prio → prio / HP prio") übereinstimmen;
  `SIM_SPALTE_WPPRIO` steht bereits auf „HP prio".
- **Monatsnamen-Split — bewusste Konsequenz, hier dokumentiert.** Der Simulationsbereich
  zeigt Monatsnamen aus **drei** Quellen, und das bleibt so:

  | Quelle | Kultur | Wirkung von `EPOS_REFLAUF_UICULTURE` bzw. der UI-Sprache |
  |---|---|---|
  | `DashboardForm` (Monatsspalte) | `CurrentUICulture` | folgt der Oberfläche |
  | Chart-Achsen über `LabelStyle.Format = "MMM"` | `CurrentCulture` | folgt **nicht** der Oberfläche — bleibt deutsch |
  | `ErdreichTemperatur.MONATSKUERZEL` | fest | bleibt deutsch |

  Paket 9 setzt ausdrücklich **nur** `CurrentUICulture` (Konzept 13.6, „Nicht Teil dieses
  Pakets"). Auf englischer Oberfläche stehen die Chart-Achsen deshalb weiter auf „Jan, Feb,
  Mrz …", und die Kennwertzeile des Erdreichdialogs auf „(Feb)". Das ist kein Fehler, sondern
  die Kehrseite der Entscheidung, die Zahlen- und Datumsformatierung unangetastet zu lassen.
  Wer das ändern will, ändert `CurrentCulture` — und damit auch Dezimaltrennzeichen und
  Tausenderpunkt in **allen** Ausgaben, einschließlich der Referenzlauf-CSV.

### 25.10 N8 — BOM nachgepflegt

Fünf Dateien auf UTF-8 **mit** BOM gebracht, jeweils durch reines Voranstellen der drei Bytes
`EF BB BF`; der Rest ist byte-gleich (im selben Lauf zurückgelesen und verglichen):

```
Form_Simulation_Config.en-US.resx      Rest byte-gleich=True
Form_KonfigPufferspeicher.en-US.resx   Rest byte-gleich=True   7049 -> 7052 Bytes
Form_PufferSp.en-US.resx               Rest byte-gleich=True  10257 -> 10260 Bytes
Form_Simulation_Detail.en-US.resx      Rest byte-gleich=True
WindowsFormsApplication1.csproj        Rest byte-gleich=True   8714 -> 8717 Bytes
```

*(Die beiden erstgenannten `.resx` wurden in derselben Nacharbeit ohnehin inhaltlich geändert;
die BOM ist dort Teil des Neuschreibens.)*

Es bleiben zwei `.resx` **ohne** BOM: `Form_PufferSp_Bearbeiten.en-US.resx` und
`Form_PufferSp_einlesen.en-US.resx`. Beide sind **rein asciisch** — es gibt kein Zeichen, das
eine falsch geratene Kodierung zerstören könnte. Sie lagen außerhalb der Schreibmenge dieser
Nacharbeit und sind der letzte Rest von P5.

### 25.11 N9 — Berichtigungen an diesem Protokoll

**(a) Abschnitt 21, Punkt 1 war in der Ursachenzuschreibung falsch.** Dort stand, der größte
verbliebene Block seien „Engine-Protokollmeldungen ohne Katalogschlüssel", und
`ErdreichAuswertung.cs`, `VDI4640Pruefung.cs` und `ErdreichTemperatur.cs` wurden dazugezählt.
Für diese drei Dateien stimmte das **nicht**: die 41 Schlüssel waren vorhanden und übersetzt,
es fehlte allein die Anbindung (25.1).

**Der echte Folgeblock ist ein anderer — und er ist kein Übersetzungsproblem.** Die
P1-Messung nach der Nacharbeit findet noch **28 Fundstellen** in `SimulationControl.cs` (15),
`SimulationBHKW.cs` (6), `SimulationWaermebedarf.cs` (3) sowie `SimulationWaermepumpe.cs`,
`SimulationSPK.cs`, `SimulationKanaele.cs` und `WaermequelleClass.cs` (je 1). Sieht man sie
sich einzeln an, sind sie fast durchweg **Fortsetzungszeilen mehrzeiliger
`Console.WriteLine`-Verkettungen** (die erste Zeile mit dem Aufruf filtert die Rezeptur
heraus, die zweite und dritte nicht). Der Rest sind ein `ArgumentException`-Text
(`SimulationSPK.cs:772`), zwei dokumentierte In-Memory-Etiketten und drei Vergleiche gegen
den DB-Wert `Tab_…​.Einheit`.

Damit verschiebt sich der Befund:

| gezählt | Zahl | Bewertung |
|---|---:|---|
| `SimulationProtokoll.Aktuell.*`-Aufrufe im Bereich | 14 | **alle bereits lokalisiert** (`SIMENG_*`) |
| `Console.WriteLine("…")` in `Allgemein/Simulation/*.cs` | 50 | nach der L2-Regel (Konzept 13.4) **außerhalb** des Katalogs — und genau das ist das Problem |

Der eigentliche Rückstand ist also **nicht**, dass 28 Zeichenketten unübersetzt sind, sondern
dass rund 50 Diagnoseausgaben der Engine **am Protokollkanal aus Paket 8 vorbeilaufen**:
`SimulationControl` (24), `WaermequelleClass` (12), `SimulationBHKW` (3),
`SimulationWaermebedarf` (2), `WaermesenkeClass` (2), `SimulationWaermepumpe` und
`SimulationSolarthermie` (je 1) und weitere. Sie erscheinen nur auf der Konsole, werden von
`Referenzlauf/Protokoll.cs` nicht als Hinweis oder Warnung gezählt und tauchen im
Lauf-Protokoll des Anwenders nicht auf. Erst wenn eine Meldung diesen Weg nimmt, stellt sich
die Frage nach ihrer Übersetzung.

**Das Folgepaket lautet deshalb: erst kanalisieren, dann katalogisieren** — je Meldung
entscheiden, ob sie in den Protokollkanal gehört (dann `SimulationProtokoll.*` plus
`SIMENG_*`-Schlüssel) oder reine Entwicklerdiagnose bleibt (dann `Console.WriteLine`, ohne
Katalog). Nicht Teil von Paket 9.

**(b) „Genau eine echte Reststelle" (Abschnitt 20.5) war zu streng gezählt.** Die beiden
Stellen `WaermesenkeClass.cs:459` und `:465` sind `Console.WriteLine` und damit nach der
L2-Regel (Konzept 13.4) ausdrücklich **außerhalb** des Katalogs. Der echte Rest der zwölf
umgebauten Dateien ist damit **0**.

> **Dafür ein anderer, bisher unbenannter Rückstand aus Paket 8:** Genau diese beiden Meldungen
> berichten **stille Datenkorrekturen** — eine halbe Puffer-Konfiguration wird auf den Heizkreis
> zurückgesetzt, eine Zweitsenke ohne Puffer fällt weg. Sie gehen über `Console.WriteLine` und
> damit **am Protokollkanal aus Paket 8 vorbei**: kein `SimulationProtokoll.Warnung`, keine
> Zählung in `Referenzlauf/Protokoll.cs`, keine Anzeige im Lauf-Protokoll. Der Kommentar an der
> Stelle sagt selbst „Sie gehört ins Lauf-Protokoll" — umgesetzt ist das nicht. Diese beiden
> sind der Musterfall des in (a) beschriebenen Folgepakets: **erst kanalisieren, dann
> katalogisieren.**

**(c) „20/20 zeichengleich" (Abschnitt 1) ist präziser zu fassen.** Von den 20 Dateien der
L0.1-Umkodierung sind **16 mechanisch nachweisbar** zeichengleich (Byte-Vergleich nach
Rückkonvertierung). Für die vier übrigen gilt der Nachweis nur **zusammen mit L0.2 im selben
Commit**, weil dort im selben Zug Literale durch `DbWerte`-Konstanten ersetzt wurden — ein
reiner Zeichenvergleich schlägt dort naturgemäß an. Die Formulierung „20 von 20" verschweigt
diese Einschränkung.

**(d) Abschnitt 4.6 und Abschnitt 21, Punkt 6:** 43 verwaiste Einträge verteilen sich auf
**28** Steuerelemente, nicht auf 16 (berichtigt in 4.6, Herleitung in 25.4).

**(e) Dateibuchhaltung Etappe 2.** Abschnitt 10 nennt die geänderten Dateien; die vollständige
Zählung für Etappe 2 lautet **24 `.cs` + 2 `.resx` + 1 `.csproj` = 27 Dateien**. Diese
Nacharbeit ändert **17** Dateien (9 `.cs`, 6 `.resx`, 1 `.csproj`, 1 `.Designer.cs`) plus drei
Dokumentationsdateien.

**(f) Der Metrikwechsel 72.501 → 34.545 → 1.629 ist ein Wechsel der Bezugsmenge, kein Einbruch.**

| Abschnitt | Zahl | gezählt wurde |
|---|---:|---|
| 7.2 (Etappe 1) | 72.501 | **jeder** Eintrag **aller 94** Ressourcenblöcke der Assembly (auch `Size`, `Location`, `Point`, `Boolean`) × 3 Kulturen |
| 20.4 (Etappe 2b) | 34.545 | nur die **Zeichenketten**-Einträge derselben 94 Blöcke × 3 Kulturen |
| 25.12 (Nacharbeit) | 1.629 | nur der **Katalog** `MyResource.Resource` — 543 Schlüssel × 3 Kulturen |

Die drei Zahlen messen unterschiedlich weite Kreise um dieselbe Sache. Für den Katalog, um den
es in Paket 9 geht, ist die dritte die aussagekräftige; die Formularsatelliten sind in P4/P5
und im Regressionslauf abgedeckt.

**(g) Offener Punkt 7 (Zeilenenden) ist geschlossen.** `.gitattributes` beginnt mit
`* text=auto`: git normalisiert Zeilenenden beim Einchecken und checkt sie nach
`core.autocrlf` wieder aus. Ob eine Datei im Arbeitsbaum LF oder CRLF trägt, ist damit ohne
Wirkung auf das Repository — die drei LF-Dateien (`WaermesenkeClass.cs`,
`Form_QuelleErdreich.cs`, `Form_Waermesenke.cs`) sind kein Befund. Die `.editorconfig`-Regel
`end_of_line = crlf` greift beim nächsten Speichern in Visual Studio und ist damit
selbstheilend. **Der BOM-Teil (F1) ist mit 25.10 erledigt**, bis auf die zwei rein asciischen
Satelliten.

**(h) Ergebnis der Anbindung in Zahlen.**

| Größe | vorher | nachher |
|---|---:|---:|
| Katalogschlüssel (`MyResource.Resource`) | 541 | **543** (+4 neu, −2 tot) |
| davon im Quelltext verdrahtet — vorher offen | 41 offen | **0 offen** |
| Literale in `ErdreichTemperatur`/`VDI4640Pruefung`/`ErdreichAuswertung` | 41 | **0** (bis auf `MONATSKUERZEL` und die vier Bodenart-Steuerwerte) |
| lokalisierte `XAxisTitle`-Zuweisungen, die tatsächlich wirken | 2 von 8 | **8 von 8** |
| Einträge in `Form_Simulation_Config.en-US.resx` | 68 | **25** |
| `.resx`/`.csproj` ohne BOM im Prüfbereich | 7 | **2** (beide rein asciisch) |

### 25.12 Verifikation der Nacharbeit

**Build** (VS-MSBuild, x86, mit umgelenktem `OutDir` — `bin\` des Anwenders unberührt, die
Anwendung durfte laufen):

```
MSBuild.exe WindowsFormsApplication1\WindowsFormsApplication1.csproj ^
            -p:Configuration=Debug -p:Platform=x86 -p:OutDir=<Arbeitsordner>
```

**0 Fehler, exakt 6 Bestandswarnungen** — dieselben wie in allen Etappen davor
(2 × CS0108, 2 × CS0109, 1 × CS4014, 1 × CS1998). `Referenzlauf.csproj` im Arbeitsbaum
ebenfalls 0 Fehler.

**Ressourcen-Ladeprüfung** (Testtreiber außerhalb des Repos, `Assembly.LoadFrom` + Reflexion):

```
resx neutral / en-US       : 543 / 543
Resource.Designer.cs       : 543
Eigenschaften zur Laufzeit : 543
Ressourcenbloecke geprueft : 3        (Invariant, de-DE, en-US)
Einzelabrufe geprueft      : 1.629    (543 Schluessel x 3 Kulturen)
Leere Werte de-DE / en-US  : 0 / 0
Eintraege mit Platzhaltern : 105
Probeformatierungen        : 210      (je Eintrag neutral UND en-US)
```

Vier Mengen deckungsgleich, kein Schlüssel liefert `null`, und für jeden Eintrag mit `{n}`
stimmen die Platzhalter**nummern** zwischen neutral und en-US überein.

Fokusprobe der vier neuen und der berichtigten Schlüssel:

```
CHART_TOOLTIP_EINHEIT      DE=[Einheit: {0}]   EN=[Unit: {0}]
CHART_TOOLTIP_WERT         DE=[Wert: {0} {1}]  EN=[Value: {0} {1}]
SIM_GRUPPE_HAUPTSENKE      DE=[Hauptsenke]     EN=[Main sink]
SIM_SPALTE_ZWEITSENKE      DE=[Zweitsenke]     EN=[Secondary sink]
Text_Hinweis               EN=[Note]     SIM_SPALTE_PRIO  EN=[prio]
Text_Ausgewaehlt entfernt = ja            Text_nicht_geoffnet entfernt = ja
```

**Regression und Sprachgleichheit** (eigener git-Arbeitsbaum auf `97183a2` plus ausschließlich
den 17 Dateien dieser Nacharbeit — die parallele Arbeit anderer Sitzungen im Arbeitsverzeichnis
ist damit aus dem Vergleich heraus):

```
DE gegen Referenzbasis 2026-08-15_B2 : 9 von 9 PASS, 2.295.987 Werte
                                       208 von 208 byte-/MD5-gleich, 0 abweichend, 0 fehlend
EN (EPOS_REFLAUF_UICULTURE=en-US)    : 9 von 9 PASS gegen dieselbe Basis
SPRACHGLEICHHEIT DE vs EN            : 208 von 208 byte-/MD5-gleich, 0 abweichend
```

Das ist der harte Nachweis, dass auch die **engine-nahen** Dateien dieser Nacharbeit
(`ErdreichTemperatur`, `VDI4640Pruefung`, `ErdreichAuswertung`) ausschließlich Anzeigewege
umgestellt haben: Keiner der 41 Texte geht in eine Rechnung.

**Harness-Proben** (Reflexion gegen die gebaute Assembly, jeweils de-DE **und** en-US):

```
Bodentyp-ComboBox   de-DE: 13 Eintraege -> Ton/Schluff, trocken | Sand, feucht | Gneis
                    en-US: 13 Eintraege -> Clay/silt, dry       | Sand, moist  | Gneiss
                    Auswahl-Rundlauf ueber den Index 13/13 gleich, Schluessel sprachunabhaengig

Kennwerte.Zeile()   de-DE: min 4,2 °C (Feb)  ·  max 15,8 °C (Aug)  ·  Mittel 9,5 °C
                    en-US: min 4,2 °C (Feb)  ·  max 15,8 °C (Aug)  ·  mean   9,5 °C

Anzeigetext()       de-DE: Entzugsleistung   6.480 W / 250 m² = 25,9 W/m²   Grenze 16 W/m²  !
                    en-US: Extraction rate   6.480 W / 250 m² = 25,9 W/m²   Limit  16 W/m²  !

Kurztext()          de-DE: Erdreich WP-01: Entzug 12.345 kWh/a (inkl. Speicherladung), Spitze …
                    en-US: Ground   WP-01: Extraction 12.345 kWh/a (incl. storage charging), …

ChartManager        Datumsachse + CHART_ACHSE_JAHRESSTUNDEN -> "Jahresstunden" / "Hours of the year"
                    Datumsachse + CHART_ACHSE_MONATE        -> "Monate"        / "Months"
                    Datumsachse, Titel leer  (Vorgabe)      -> "Monate"        / "Months"
                    Zahlenachse, Titel gesetzt (unveraendert)-> "Jahresstunden" / "Hours of the year"
                    XY-Achse,    Titel gesetzt (unveraendert)-> "Temperatur…"   / "Temperature…"

Volumenfilter       SelectedIndex nach dem Fuellen = 0, SelectedIndexChanged 1x (wie im Bestand)
                    Stufe 0 -> (Gesamtvolumen IS NULL OR Gesamtvolumen Like '%')
                    Freitext -> dasselbe Praedikat;  SQL je Stufe DE == EN: True

Form_Simulation_Config  MinimumSize im Konstruktor leer, in OnShown gesetzt und auf die
                        Arbeitsflaeche gedeckelt; lblStatus und die vier Paket-8-Steuerelemente
                        auf denselben Koordinaten wie in 19.4/19.5
```

Alle Proben **BESTANDEN, kein Fehler**.

**Prüfrezeptur P1–P6** einmal vollständig gelaufen; der Ist-Stand steht in
[`Lokalisierung_Pruefung.md`](Lokalisierung_Pruefung.md). Kurzfassung: P2, P3, P4, P6 ohne
Befund (P4 543/543/543/543, P6 208/208 in beiden Richtungen); P1 findet in den Views keinen
benutzersichtbaren deutschen Text und in den drei Erdreich-Dateien **gar keinen** mehr; P5
meldet nur noch die zwei rein asciischen Satelliten.

## 26. Was diese Nacharbeit NICHT getan hat

- **Kein Commit.** Der Arbeitsstand liegt unkommittiert im Arbeitsverzeichnis.
- **Die Dateien der parallel laufenden Sitzungen nicht angefasst:**
  `Views/Prozesswärme/Form_Prozesswaerme.cs`, `Views/Stromverbraucher/Form_Stromverbraucher.cs`,
  `Controller/WizardCtrl.cs`, `Views/Wizard/WizardParent.cs` und die sechs
  `Controller/*KontextMenuCtrl.cs`. Sie sind auch aus der Verifikation herausgehalten (eigener
  Arbeitsbaum).
- **`Referenzlaeufe/*` und `DB-Backup/` unberührt**; der Regressionslauf hat seine Ergebnisse in
  einem eigenen Arbeitsbaum außerhalb des Repos abgelegt. Produktive Datenbank **nur lesend**
  (zwei `SELECT COUNT(*)` auf `Tab_Pufferspeicher_STAMM` für die NULL-Gegenprobe in 25.7).
- **`CurrentCulture` weiterhin nicht gesetzt** — siehe den Monatsnamen-Split in 25.9.
- **Die Engine-Protokollmeldungen nicht umgestellt** (25.11 a) — eigenes Folgepaket.
- **`Form_Simulation_Detail.en-US.resx` nur um einen Eintrag ergänzt**, nicht durchgesehen.
  Beim Nachtragen fielen dort Bestandsübersetzungen auf, die dem Glossar widersprechen
  („Power consumption SPK:", „Power requirements:", „Heat requirement" für Wärmebedarf) — 244
  Einträge, ein eigener Durchgang. Nicht Teil dieses Auftrags.
- **Kein UI-Sichttest.** L7 bleibt offen und braucht den Anwender; die Liste dazu steht in
  Abschnitt 21, Punkt 3, ergänzt um die drei Diagrammtitel aus 25.2 und um einen Messwert aus
  dem Harness: `checkBox_Extrapolation` ist auf englischer Oberfläche 361 px breit und reicht
  damit rund 8 px über die Nutzfläche hinaus (deutsch: 264 px). Das ist ein Layout-, kein
  Übersetzungsbefund.
