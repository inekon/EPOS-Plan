# Einheitenbruch zwischen Brennstoff-Stamm und Umrechnungstabelle — Analyse und Vorgehensweise

Stand: 30.08.2026. Betrachtet wurden die vier Ableitungsstellen der Umrechnungsregel, die Tabellen `Tab_Brennstoff_Stamm`, `energy_carrier`, `energy_conversion`, `energy_project_settings` und `energy_price` sowie alle 25 Brennstoffe des Katalogs. **Entscheidung 30.08.2026: Weg (a) — siehe 5.1.**

**Bezugsstand.** Codebasis ist der Zweig `Pufferspeicher` = Commit **`2ab47b1`** (Etappe BK3, 30.08.2026), lokal deckungsgleich mit `claude/nostalgic-matsumoto-481128`. Dieser Stand liegt **nicht auf origin** (13 Commits vor `origin/Pufferspeicher` = `acf2e30`) und **nicht in main**. `main` = `b3d305b` führt `ZIEL_VERSION` **55** mit den geparkten Schritten 56/57, der Pufferspeicher-Strang **61**. Zeilenangaben ohne Revisionsvorsatz beziehen sich auf `2ab47b1`. Diese Konzeptdatei selbst liegt auf `claude/lucid-cori-a9a425`.

**Aufgabe.** Der in BK3 § 5 gemessene Einheitenbruch — `Tab_Brennstoff_Stamm.Einheit` und `energy_conversion` verwenden für dieselben Brennstoffe unterschiedliche Schreibweisen, die Identitätsregel-Ableitung liefert deshalb für 9 von 25 Brennstoffen `-1` — soll behoben werden. Drei Wege stehen zur Wahl. Dieses Dokument arbeitet sie aus und legt sie **zur Entscheidung** vor.

Dieses Dokument ist reine Analyse und Planung — **es wurde keine Zeile Code und kein Datensatz geändert.** Die Produktiv-Datenbank wurde nie geöffnet, sondern nur als Dateikopie gelesen.

**Messbasis.** Kopie von `C:\ProgramData\EPOS_PLAN\Kenndaten.accdb` (151.949.312 B, Dateistand 30.08.2026 06:13:43; keine `Kenndaten.laccdb` vorhanden, die Anwendung war zu). `Tab_Applikation.SchemaVersion` = **61**. Erhebung 30.08.2026. Alle Zahlen in Abschnitt 3 sind auf dieser Kopie **gemessen**, nicht geschätzt.

---

## 1. Kurzfassung des Befunds

Die Umrechnungsregel eines Trägers wird über einen **Textvergleich** gefunden: gesucht wird die Identitätsregel `from_unit = to_unit = Tab_Brennstoff_Stamm.Einheit` zum Brennstoff. Für die Gase steht dort „m³", `energy_conversion` kennt aber nur `Nm³ → Nm³` — die Suche geht leer aus und liefert `-1`. **9 von 25 Brennstoffen** sind betroffen: 1 Stadtgas, 2 Erdgas LL, 3 Erdgas E, 4 Flüssiggas Propan, 5 Flüssiggas Butan, 12 Holz, 14 Biogas, 24 Sonstige, 25 Wasserstoff; 16 finden ihre Regel.

**Drei Korrekturen gegenüber der bisherigen Formulierung des Befunds:**

1. **Der Bruch ist breiter als „m³/Nm³".** Es sind **drei Einheitengruppen**: sechs m³-Brennstoffe (1, 2, 3, 14, 24, 25), zwei kg-Brennstoffe (4, 5), ein rm-Brennstoff (12). Wer nur die Gase bedient, heilt mit **(a)** höchstens fünf der neun Fälle; **(b)**, auf dieselbe Gruppe beschränkt, käme auf sechs, weil sie auch den trägerlosen Brennstoff 24 erreicht.
2. **Eine Regelgruppe „z-Faktor 67–72" gibt es so nicht.** Gemessen sind **fünf** z-Regeln 68–72 (`m³ → Nm³`, Faktor 1, Brennstoffe 1, 2, 3, 14, 25) **plus einen Fremdkörper**: Regel **67** heißt ebenfalls „z-Faktor", bildet aber `Nm³ → kWh` mit Faktor **0,5** ab und widerspricht dem gepflegten Heizwert 10,5 kWh/Nm³ um Faktor 21. **Keine der sechs Regeln wird von einer Bestandszeile referenziert.**
3. **Die Waise ist genau eine Zeile und hat eine Begleitspur.** `energy_project_settings` **Zeile ID 10076**, Projekt **1039** („Mehrgebäude"), Träger **63** (Erdgas E), `ID_Umrechnung = -1`. Aus demselben Speichervorgang stammt `energy_price` **id 10141** mit `arbeitspreis_unit = 'm³'` (valid_from 27.08.2026 17:00:57, Heizwert 10,5) — die einzige von 14 Preiszeilen des Trägers 63, die nicht „Nm³" trägt.

Praktische Folge des `-1`: keine Einheiten-Vorwahl im Trägerdialog, deshalb stille Vorwahl der **ersten von ACE gelieferten Regel**, die beim ersten Speichern festgeschrieben wird (2.2). Der Rechenkern ist **nicht** betroffen — Abrechnung, Wirtschaftlichkeit und Simulation rechnen über Hi/Hs, nicht über `energy_conversion.factor` (`EnergieEinheitenPruefung.cs:686–688`).

---

## 2. Wie die Ableitung heute funktioniert

### 2.1 Vier Ableitungsstellen, ein Prädikat

| # | Ort | Rev | Einheitenquelle |
|---|---|---|---|
| 1 | `Controller/WizardCtrl.cs:1615–1625` — `ConvIdErmitteln` | beide | `Tab_Brennstoff_Stamm.Einheit` |
| 2 | `Controller/EnergietraegerKatalogCtrl.cs:234–257` — `UmrechnungFuer` | **nur `2ab47b1`** | Stamm, delegiert an #1 |
| 3 | `Views/Kosten/Form_Kosten_Auswahl.cs:57–103` — `FetchAdditionalData` + `GetConvID` | beide | `Tab_Brennstoff_Stamm.Einheit` |
| 4 | `Views/Kosten/Form_Kosten_VarAuswahl.cs:68–110` (zeichengleiche Kopie von #3) | beide | `Tab_Brennstoff_Stamm.Einheit` |

Das Prädikat, wörtlich (`WizardCtrl.cs:1618`):

```sql
SELECT ID FROM ENERGY_CONVERSION WHERE id_brennstoff = ? AND from_unit = ? AND to_unit = ?
```

Beide Einheitenparameter tragen **denselben C#-String** — gesucht wird ausdrücklich die Identitätsregel. Gefiltert wird nach **Brennstoff, nicht nach Träger**; mehrere Träger teilen sich einen Brennstoff. **Kein Trim** — bewusst so und kommentiert (`EnergietraegerKatalogCtrl.cs:247–249`: „BEWUSST OHNE Trim: TraegerSatzAnlegen trimmt ebenfalls nicht… beide Wege sollen dieselbe Zeile schreiben"). Groß-/Kleinschreibung ist nur deshalb unerheblich, weil ACE `=` auf TEXT case-insensitiv auswertet; im C#-Code steht kein `StringComparison`. Kein `ORDER BY`, kein `TOP 1`, kein Dublettencheck; `-1` entsteht ausschließlich bei `o == null`.

Seit BK3 hängen **beide Schreibwege an dieser einen Ableitung** — `ConvIdErmitteln` wurde genau dafür von `private` auf `internal` gehoben (`WizardCtrl.cs:1610–1613`). Deshalb bleibt bei Weg (c) `ConvIdErmitteln` selbst unberührt; zu ändern sind allein die zwei Einheiten-Nachschläge davor, während (a)/(b) den Code gar nicht berühren. Die **Leseseite** arbeitet dagegen bereits durchgängig mit `energy_carrier.billing_unit`: `EnergieEinheitenPruefung.cs:548` (Startknoten der kWh-Prüfkette), `WirtschaftlichkeitCtrl.cs:3224,3230–3231` (Rechnung, mit `.Trim()`), `EnergieMengen.cs:62–79`, `UcBkKosten.cs:857,894`, `ucFuelSettings.cs:837,1492–1493`. `Tab_Brennstoff_Stamm` kommt in `ucFuelSettings.cs` **überhaupt nicht vor** (0 Treffer, beide Revisionen).

Drei Eigenschaften der Stammspalte, alle für die Wegwahl tragend:

- **Schreiber: keiner.** `INSERT INTO Tab_Brennstoff…` / `UPDATE Tab_Brennstoff…` haben in beiden Revisionen **0 Treffer** — kein Pflegedialog, kein Importer (VDI 3805, CSV, CEC, Pan, Spotpreis). Für Weg (a): nichts dreht eine Angleichung zurück.
- **Angezeigt wird sie nirgends direkt** — sichtbar nur über Kopien: `energy_price.arbeitspreis_unit` (`WizardCtrl.cs:1542`, `Form_Kosten.cs:2161`, `Form_BHKWEing.cs:624`, `Form_Heizkessel.cs:396`) und `energy_project_settings.ID_Umrechnung` über die Regelsuche.
- **Die dritte Kopie ist der wichtigste Nebenbefund:** beim Anlegen eines neuen Katalogträgers wandert der Stammtext in `energy_carrier.billing_unit` (`Form_Kosten.cs:2113,2122`; `Form_BHKWEing.cs:550,559`; `Form_Heizkessel.cs:320,329`). **Jeder heute neu angelegte Gasträger bekommt damit `billing_unit = "m³"`** — genau den Zustand, den Schritt 26a einmalig beseitigt hat.

### 2.2 Was das `-1` im Trägerdialog anrichtet

Die Klappliste `cmbUnit` wird mit **allen** Regeln des Brennstoffs gefüllt (`ucFuelSettings.cs:1935–1954`) — kein Filter auf `from_unit`, `aktiv` oder Träger, **kein `ORDER BY`**; angezeigt wird die `to_unit` (`DisplayMember = "ToUnitCode"`, `:1489`). Die Fehlvorwahl entsteht in vier Schritten: (1) `cmbUnit.DataSource = _conversions;` (`:1488`) setzt implizit `SelectedIndex = 0`, während der Handler abgehängt ist (`:1487`/`:1490`) — nichts wird umgerechnet, nichts protokolliert, die Auswahl steht trotzdem. (2) Die Vorwahl greift nicht: `GetTargetUnitByConversionId(-1)` (`:1986–1998`) liefert `null`, der `FirstOrDefault`-Vergleich scheitert, der `if`-Block wird übersprungen — **kein Rückfall auf `baseUnit`**, denn der `else`-Zweig (`:1565`) greift nur ohne Projektzeile. (3) Die Beschriftungen folgen der Fehlauswahl (`:1581–1587`): `€/{conv.ToUnitCode}`, ebenso Heizwert und Brennwert. (4) **Speichern schreibt sie fest** (`:1734–1738`): `SpeichereWerte` liest nicht das Feld `id_conversion`, sondern fragt die ComboBox erneut ab — `int currentConvID = GetConvID(cmbUnit.SelectedItem);` — und legt das Ergebnis in `ID_Umrechnung` ab (`:1866–1884` UPDATE, `:1888–1905` INSERT-Rückfall).

Kein Riegel greift: `SpeichernErlaubt` (`:1717–1726`) prüft ausschließlich die kWh-Bedingung, die Einheitenwahl selbst wird nie beurteilt. Auch die `EnergieEinheitenPruefung` schlägt nicht an — sie startet für Träger 63 mit „Nm³" aus `billing_unit` und meldet **keinen Befund** (BK3-Probe [2]). **Der Bruch ist von der bestehenden Prüfmechanik nicht erfasst.** Der Trägerdialog ist zugleich der einzige Schreiber, der `-1` in `DBNull` übersetzt (`:1878`/`:1900`); die fünf übrigen Wege legen die rohe `-1` ab. Leserseitig sind beide gleichwertig (`ucFuelSettings.cs:1535` `?? -1`; `EnergieEinheitenPruefung.cs:563` `> 0`).

### 2.3 Der Präzedenzfall — Schritt 26a und Leitentscheidung L4

Für die Trägerseite ist dieselbe Frage bereits entschieden und ausgeführt. Migrationsschritt **26a `NormkubikUmbenennen`** (`SchemaMigration.cs:4832–4906`) stellt `energy_carrier.billing_unit` der Gasträger von „m³" auf „Nm³" und zieht `energy_price` nach. `DbWerte.cs:1810–1815` hält die Begründung fest:

> **Reine Semantik.** Die Umbenennung ändert KEINEN Zahlenwert: Die Katalog-Heizwerte der Gasträger sind seit jeher Normwerte …

Und zu „m³" selbst (`DbWerte.cs:1794–1796`):

> Bleibt als `from_unit` des z-Faktors stehen, ist ab Schritt 26 aber **keine Abrechnungseinheit eines Gasträgers mehr.**

Das ist Leitentscheidung **L4**. Weg (a) schreibt sie auf die Stammseite fort; Weg (b) stellt sich für die Gase gegen sie (4.2). Vier übertragbare Merkmale von 26a: Werte aus `DbWerte`-Konstanten statt Literalen; eng geführtes `WHERE` (Preismodell **und** exakter Alttext — ausdrücklich so, damit das ASCII-`m3` der Öl-/Feststoffregeln nicht getroffen wird, `:4814–4820`); `user_edited`-Schutz; Idempotenzbegründung im XML-Kommentar samt der Ausnahme `AND [to_unit] <> 'Nm³'`, ohne die ein zweiter Lauf die Tabelle wachsen ließe (im Trockentest 5 Zeilen je Durchgang).

---

## 3. Vollständiges Inventar

### 3.1 Die 25 Brennstoffe — die **9 betroffenen fett**

„Ident." = ID der heute gefundenen Identitätsregel; „Best." = Zeilen in `energy_project_settings` an diesem Brennstoff.

| ID | Name | Einh. | Träger | billing_unit | Ident. | (a) | (b) | (c) | Best. |
|---|---|---|---|---|---|---|---|---|---|
| **1** | **Stadtgas** | m³ | 64 | Nm³ | **-1** | → Nm³ ⇒ R36 | m³→m³ | Nm³ ⇒ R36 | 1 |
| **2** | **Erdgas LL** | m³ | 52 | Nm³ | **-1** | → Nm³ ⇒ R38 | m³→m³ | Nm³ ⇒ R38 | 0 |
| **3** | **Erdgas E** | m³ | 63 | Nm³ | **-1** | → Nm³ ⇒ R40 | m³→m³ | Nm³ ⇒ R40 | 14 |
| **4** | **Flüssiggas (Propan)** | kg | – | – | **-1** | nur L ⇒ R42 = **echter Einheitenwechsel** | kg→kg | **kein Träger** | 0 |
| **5** | **Flüssiggas (Butan)** | kg | 72 | kg | **-1** | nur L ⇒ R45 = **echter Einheitenwechsel** | kg→kg | kg ⇒ **keine Regel** | 0 |
| 6 | Heizöl S | L | 67 | L | 29 | n/a | n/a | L ⇒ R29 | 1 |
| 7 | Heizöl M | L | – | – | 32 | n/a | n/a | – | 0 |
| 8 | Heizöl L | L | 62, 70, 71 | L | 35 | n/a | n/a | L ⇒ R35 | 4 |
| 9 | Heizöl EL | L | 56 | L | 3 | n/a | n/a | L ⇒ R3 | 0 |
| 10 | Koks | kg | 59 | kg | 48 | n/a | n/a | kg ⇒ R48 | 0 |
| 11 | Kohle | kg | – | – | 50 | n/a | n/a | – | 0 |
| **12** | **Holz** | rm | – | – | **-1** | nur kg ⇒ R8 = **echter Einheitenwechsel** | rm→rm | **kein Träger** | 0 |
| 13 | Elektr. Energie | kWh | 54, 58, 60 | kWh | 51 | n/a | n/a | kWh ⇒ R51 | 9 |
| **14** | **Biogas** | m³ | 49, 61, 65, 66 | Nm³ | **-1** | → Nm³ ⇒ R53 | m³→m³ | Nm³ ⇒ R53 | 4 |
| 15 | Pellets | kg | – | – | 58 | n/a | n/a | – | 0 |
| 16 | Rapsöl | L | – | – | 55 | n/a | n/a | – | 0 |
| 17 | Tierische Fette | kg | 69 | kg | 60 | n/a | n/a | kg ⇒ R60 | 1 |
| 18 | Heizöl Bio 5 | L | – | – | **6 und 13** | n/a | n/a | – | 0 |
| 19 | Heizöl Bio 10 | L | 53 | L | 16 | n/a | n/a | L ⇒ R16 | 0 |
| 20 | Heizöl Bio 15 | L | 57 | L | 19 | n/a | n/a | L ⇒ R19 | 0 |
| 21 | Heizöl Bio 20 | L | – | – | 22 | n/a | n/a | – | 0 |
| 22 | Heizöl EL schwefelarm | L | – | – | 25 | n/a | n/a | – | 0 |
| 23 | Fernwärme | kWh | 51 | kWh | 63 | n/a | n/a | kWh ⇒ R63 | 0 |
| **24** | **Sonstige** | m³ | – | – | **-1** | **heilt nicht — gar keine Regel** | m³→m³ | **kein Träger** | 0 |
| **25** | **Wasserstoff** | m³ | 68 | Nm³ | **-1** | → Nm³ ⇒ R66 | m³→m³ | Nm³ ⇒ R66 | 0 |

Regelmengen der neun Fälle: B1 {36 Nm³→Nm³, 68 m³→Nm³} · B2 {38, 69} · B3 {40 Nm³→Nm³, 67 Nm³→kWh, 70 m³→Nm³} · B4 {42 L→L, 44 L→kg} · B5 {45 L→L, 46 L→kg} · B12 {7 kg→rm, 8 kg→kg, 9 kg→SRM, 10 kg→t} · B14 {53, 71} · **B24 { }** · B25 {66, 72}.

### 3.2 Was unverändert bleibt

- **16 Brennstoffe** finden ihre Identitätsregel und werden von keinem der drei Wege berührt.
- **`energy_project_settings`: 34 Zeilen, davon 33 mit gültigem Umrechnungsverweis.** Der LEFT-JOIN-Test über alle Tabellen zeigt genau **eine** Referenzverletzung (3.3); alle 34 Trägerverweise sind gültig, `ID_Umrechnung IS NULL` kommt **0-mal** vor. Verteilung je Regel: `-1` → 1 · 29 → 1 · 35 → 4 · 36 → 1 · **40 → 13** · 51 → 8 · 52 → 1 · 53 → 4 · 60 → 1. Von 65 Regeln sind **8** referenziert, 57 referenzlos.
- 19 der 34 Zeilen (56 %) hängen an Brennstoffen der 9er-Liste (Träger 63 → 14, 49 → 3, 64 → 1, 66 → 1) — **alle außer der Waise zeigen bereits auf gültige `Nm³ → Nm³`-Regeln. Kein Weg muss einen bestehenden Verweis umschreiben**; zu reparieren ist genau 1 Zeile.
- **Katalog unangetastet:** alle 65 Regeln `user_edited = False` und `aktiv = True`, alle 27 Träger `is_active = True`, alle 25 Brennstoffe `ReadOnly = False`. Ein (a)- oder (b)-Skript liefe gegen keine geschützten Zeilen.

### 3.3 Die Waise

`energy_project_settings` **ID 10076** — `ID_Projekt = 1039` („Mehrgebäude"), `ID_Energieträger = 63`, `ID_Umrechnung = -1`, `custom_hi = 10.5`, `custom_hs = 11.6`, `custom_price_work/_base/_power = 0`, `co2 = 240`, `so2 = 0.3`, `nox = 110`; sämtliche Aufschlags- und Anteilsspalten NULL bzw. `False`, `Anteil_Modus = „Gesamtwert"`. Nach BK3 unverändert (BK3-Probe [7]: „Zeile 10076 feldweise identisch"). Die **Begleitspur**: `energy_price` **id 10141**, `ID_Projekt 1039`, `arbeitspreis_unit = 'm³'`, `valid_from 27.08.2026 17:00:57`, Heizwert 10,5. Träger 63 führt 14 Preiszeilen — dreizehn mit „Nm³", diese eine mit „m³". Beide stammen aus **demselben Speichervorgang**. Der Heizwert 10,5 ist „kWh je Nm³"; die Preiszeile behauptet „kWh je m³".

### 3.4 Die z-Regeln 67–72

| ID | Brennstoff | from → to | factor | faktor_name | user_edited | aktiv | Referenzen |
|---|---|---|---|---|---|---|---|
| **67** | 3 Erdgas E | **Nm³ → kWh** | **0,5** | z-Faktor | False | True | **0** |
| 68 | 1 Stadtgas | m³ → Nm³ | 1 | z-Faktor | False | True | **0** |
| 69 | 2 Erdgas LL | m³ → Nm³ | 1 | z-Faktor | False | True | **0** |
| 70 | 3 Erdgas E | m³ → Nm³ | 1 | z-Faktor | False | True | **0** |
| 71 | 14 Biogas | m³ → Nm³ | 1 | z-Faktor | False | True | **0** |
| 72 | 25 Wasserstoff | m³ → Nm³ | 1 | z-Faktor | False | True | **0** |

`energy_conversion` hat **keine** Spalte für Zustandszahl oder Brennwert; die Bedeutung hängt allein am Freitext `faktor_name` (59× „Umrechnungsfaktor", 6× „z-Faktor"). Der Faktor fließt rechnerisch **ausschließlich** in die Einheitenumrechnung der Dialogfelder ein (`ucFuelSettings.cs:1604/1615/1626` hin, `:1639–1643` zurück). Die z-Faktoren 68–72 stehen alle auf 1 — `m³ → Nm³` ist im Bestand numerisch wirkungslos, was Weg (a) stützt: die Umbenennung ändert kein Rechenergebnis.

### 3.5 Angrenzend, ausdrücklich **nicht** Gegenstand dieses Konzepts

Diese sieben Befunde sind mitgemessen worden. Sie sind real, gehören aber nicht zur Aufgabe.

1. **Regel 67 als Fremdkörper** — Name „z-Faktor", aber `Nm³ → kWh` mit Faktor 0,5; ein z-Faktor ist definitionsgemäß Volumen → Normvolumen, und 0,5 kWh/Nm³ widerspricht `hi = 10,5` um Faktor 21. **Referenzlos** (Randfrage 5.3.4).
2. **Doppelte Identitätsregel bei Brennstoff 18** (Heizöl Bio 5): zwei `L → L`-Regeln, **IDs 6 und 13**, aus einem doppelt eingespielten Satz (die Tripel 4/5/6 und 11/12/13 sind wertgleich). `ExecuteScalar` nimmt ohne `ORDER BY` willkürlich eine davon.
3. **5 Träger ohne Brennstoff** (`ID_Brennstoff = 0`, Brennstoff 0 existiert nicht): 73 Steinkohle, 74 Braunkohlebrikett, 75 Scheitholz, 76 Holzpellets, 77 Holzhackschnitzel — alle `billing_unit = kg`, alle 0 Bestandszeilen. `WHERE id_brennstoff = 0` liefert immer leer: auf **keinem** der drei Wege je eine Umrechnung.
4. **10 Brennstoffe ohne Träger**: 4, 7, 11, 12, 15, 16, 18, 21, 22, 24 — davon stehen 4, 12 und 24 auf der 9er-Liste.
5. **Holz (12) widerspricht sich schon im Katalog**: `Einheit = rm`, aber `PreisEinheit = €/kg`.
6. **Vier Kubikmeter-Schreibweisen, codepointgenau.** `m³` = U+006D **U+00B3** (Stamm.Einheit, `from_unit` der z-Regeln, die eine Preiszeile) · `m3` = U+006D **U+0033**, **nur als `to_unit`** in den Regeln 1, 4, 11, 14, 17, 20, 23, 27, 30, 33, 59, 62 · `Nm³` = U+004E U+006D **U+00B3** (billing_unit aller Gasträger) · `€/m3` mit ASCII-3 in `PreisEinheit` der sechs m³-Brennstoffe. Innerhalb einer Spalte keine gemischte Schreibweise.
7. **`EnergieEinheitenPruefung` erkennt den Bruch nicht** (2.2). Ein Prüfschritt „Stamm.Einheit ↔ billing_unit ↔ vorhandene Identitätsregel" wäre ein eigenständiger vierter Baustein (Randfrage 5.3.5).

---

## 4. Die drei Lösungswege

### 4.1 Weg (a) — `Tab_Brennstoff_Stamm.Einheit` auf die energy_conversion-Schreibweise ziehen

**Beschreibung.** Für die fünf Gase mit Träger (1, 2, 3, 14, 25) wird der Stammtext „m³" auf „Nm³" gestellt — dieselbe Umbenennung, die Schritt 26a auf der Trägerseite schon vollzogen hat: reine Semantik, kein Zahlenwert. Für 4, 5 und 12 wäre es **kein** Schreibweisen-, sondern ein **echter Einheitenwechsel** (4/5 hätten nur `L → L`, 12 nur `kg → kg` als Identitätsregel), sie sind hier ausgenommen. Brennstoff 24 hat gar keine Regel und ist nicht erreichbar.

**Umsetzungsskizze.** Ein Migrationsschritt ist fachlich **nicht zwingend** — es geht um Zeileninhalte, nicht um Spalten. Gewählt wird er trotzdem, weil nur er jede Installation erreicht: derselbe Stammtext „m³" steht in jeder ausgelieferten `Kenndaten.accdb`, und eine einmalige Datenpflege heilte nur die eine Datenbank, an der sie ausgeführt wird. Nummer **62** — die nächste freie; 56–61 sind auf dem Pufferspeicher-Strang belegt, `SCHRITT_62` und `ZIEL_VERSION = 62` sind baumweit ohne Treffer. Reihenfolge zwingend nach der seit dem Vorfall vom 29.08.2026 geltenden Konvention: **erst Schrittkonstante, Methode und `SCHRITTE`-Eintrag, DANN das Ziel** (`SchemaMigration.cs:88–110`). Muster ist `NormkubikUmbenennen` (`:4832–4906`): `DbWerte.EINHEIT_KUBIKMETER` → `DbWerte.EINHEIT_NORMKUBIKMETER` statt Literalen, eng geführtes `WHERE`, Zeilenzahl in `l.Notiz`. Wegen der **ACE-Falle** — `UPDATE … WHERE x IN (SELECT …)` mit Parameter in der Unterabfrage trifft in ACE null Zeilen, ohne Fehler und ohne Warnung (`:4622–4636`) — werden die Brennstoffnummern **vorher** per parametrisierter Abfrage aufgelöst und als ganzzahlige `IN`-Liste interpoliert; `GasBrennstoffListe` (`:4718–4737`) ist direkt wiederverwendbar. Ihr Auswahlkriterium ist gemessen, nicht angenommen: `SELECT DISTINCT [ID_Brennstoff] FROM [energy_carrier] WHERE [pricing_model] = ? AND [ID_Brennstoff] IS NOT NULL` mit `CARRIER_GAS = "GASEOUS_FUEL"` (`:4720–4724`, Konstante `:4516`; die Begründung für `GASEOUS_FUEL` statt `GAS` steht in `DbWerte.cs:1731–1738`). Sie liefert damit genau **{1, 2, 3, 14, 25}** — der trägerlose Brennstoff 24 bleibt automatisch draußen, ohne dass der Schritt ihn eigens ausschließen müsste.

**Wirkung** — *Bestandszeilen:* keine; die Spalte wird von keiner Rechnung und keiner Anzeige gelesen (2.1), die 33 gültigen Verweise bleiben unberührt. *Waise:* **keine.** Das gilt grundsätzlich für alle drei Wege: **keine Änderung an der Ableitung erreicht je eine Bestandszeile.** `TraegerSatzAnlegen` ist ein reines `INSERT` (`WizardCtrl.cs:1570–1602`, dort ausdrücklich „BESTANDSZEILEN BLEIBEN UNANGETASTET (kein Heilungsschritt)"), `InsProjekt` ebenso. Der einzige Schreiber auf einer bestehenden Zeile ist `ucFuelSettings.SpeichereWerte`, und der nimmt `GetConvID(cmbUnit.SelectedItem)` — die Listenindex-0-Mechanik aus 2.2 — statt der Ableitung. Zeile 10076 ändert sich deshalb nur durch einen Speichervorgang im Trägerdialog oder durch die ausdrückliche Heilung in P4 (Entscheid 5.3.1). *z-Regeln 67–72:* **keine** — der Schritt arbeitet auf `Tab_Brennstoff_Stamm`, nicht auf `energy_conversion`, und ist damit von der 26a-Idempotenzfalle frei.

**Reichweite: 5 von 9** (1, 2, 3, 14, 25 ⇒ R36/38/40/53/66). Nicht erreicht: 4, 5, 12 (nur über echten Einheitenwechsel) und 24 (keine Regel vorhanden).

**Fallstricke.** **(a) schließt als einziger Weg die Regressionsquelle:** solange der Stammtext „m³" bleibt, bekommt jeder neu angelegte Gasträger `billing_unit = "m³"` und stellt den Zustand vor Schritt 26a wieder her (2.1) — **(b) und (c) schließen diese Quelle nicht.** Eine (a)-Variante, die auch „kg"/„rm" umschriebe, ließe dagegen die Energiesteuer-Gutschrift für Flüssiggas **still auf `null` fallen**: `SteuerGutschriftRechner.cs:596,604,622` verzweigt über `IstEinheit(a.Abrechnungseinheit, "l")` bzw. `"kg"`, und `a.Abrechnungseinheit` stammt aus `billing_unit` — kein Fehler, nur ein anderer Begründungstext; mittelbar betroffen ist (a) ohnehin nur über **neu angelegte** Träger (analog `ucBrennstoffBestandteile.cs:694–720`, nur `2ab47b1`). Der Rückweg besteht allein aus einem zweiten Migrationsschritt — `.accdb` ist von `.gitignore` ausgeschlossen.

### 4.2 Weg (b) — fehlende Identitätsregeln in `energy_conversion` säen

**Beschreibung.** Neun neue Zeilen: `m³ → m³` für 1, 2, 3, 14, 24, 25; `kg → kg` für 4, 5; `rm → rm` für 12. Jeweils `factor = 1`, `user_edited = FALSE`, `aktiv = TRUE`, `faktor_name = DbWerte.UMRECHNUNG_NAME_STANDARD` (= „Umrechnungsfaktor", `2ab47b1 Allgemein/DbWerte.cs:1725` — nachgeprüft) und **nicht** „z-Faktor", sonst stünde eine zweite gleichnamige Zeile im Regelblock des Dialogs (genau dafür existiert Schritt 26c `IdentitaetsregelnBerichtigen`, `SchemaMigration.cs:5005–5027`).

**Umsetzungsskizze.** Ebenfalls Schritt **62**, Muster `ZFaktorSaeen` (`:4927–4986`): je Brennstoff einzeln; Idempotenz per `COUNT(*)` über das Tripel `(id_brennstoff, from_unit, to_unit)`; ID-Vergabe per `MAX(ID)+1` (kein AutoWert); **alle acht Spalten ausdrücklich** schreiben, keine Spaltendefaults. Dieselbe Reihenfolge-Konvention und dieselbe ACE-Vorsichtsregel wie bei (a). Der natürliche Schlüssel für Export/Import (`ProjektExportImportCtrl.cs:101`: `id_brennstoff, from_unit, to_unit`) trägt die neuen Zeilen ohne Zusatzarbeit.

**Wirkung** — *Bestandszeilen:* keine; es werden nur Zeilen hinzugefügt, keine bestehende Referenz zeigt auf sie. *Waise:* **keine** — aus demselben Grund wie bei (a): die Saat ändert nur, was eine Ableitung fände, und keine Ableitung schreibt auf eine Bestandszeile. Zeile 10076 bliebe auf `-1`, bis der Dialog sie speichert oder P4 sie heilt; nach der Saat stünde dem Dialog dabei zusätzlich ein m³-Eintrag zur Auswahl — fachlich die falsche Regel (siehe Fallstricke). *z-Regeln 67–72:* Faktor 1 ist rechnerisch neutral, und `KwhStufen` überspringt Selbstbezüge als Zwischenstufe ausdrücklich (`EnergieEinheitenPruefung.cs:749`). **Aber:** eine `m³ → m³`-Regel fällt genau in das `from_unit`-Raster von Schritt 26a (`:4866–4872`: `from_unit = 'm³' AND to_unit <> 'Nm³' AND user_edited = FALSE`) und würde bei einem **Wiederholungslauf** von 26a zu `Nm³ → m³` umgeschrieben. Bei frischer Migration läuft 26a vor einem späteren Seed-Schritt — dann kein Konflikt.

**Reichweite: 9 von 9** — der einzige Weg, der auch 4, 5, 12 und 24 erreicht.

**Fallstricke — für die Gase wiegen sie schwer.**

- **Nach der Saat gewinnt für die Gase die neue `m³ → m³`-Regel die Ableitung**, weil Stamm.Einheit „m³" nun from = to trifft — nicht mehr `-1`, aber auch **nicht** die Nm³-Identität. Der Wizard schriebe die m³-Identität in `ID_Umrechnung`, während `billing_unit` „Nm³" sagt und die Katalogwerte Normwerte sind (Hi 10,5 kWh je **Nm³**). Der Dialog zeigte dann **„€/m³" auf Zahlen, die je Nm³ gelten** — stiller als heute: statt gar keiner Vorwahl eine plausibel aussehende falsche.
- **Widerspruch zur Leitentscheidung L4** (2.3): eine Identitätsregel `m³ → m³` behauptet, m³ sei eine Abrechnungseinheit eines Gasträgers — genau das, was Schritt 26 verneint hat.
- **Klapplisten-Zweideutigkeit.** Für Erdgas E stünden nebeneinander: 40 (Nm³→Nm³, Anzeige „Nm³"), 70 (m³→Nm³, Anzeige **ebenfalls** „Nm³"), 67 (Anzeige „kWh") und neu m³→m³ (Anzeige „m³"). Schon heute stehen zwei Einträge mit identischem Anzeigetext in der Liste; `FirstOrDefault(c => c.ToUnitCode == …)` nimmt willkürlich den ersten. **(b) verschärft eine bereits bestehende Zweideutigkeit.**
- **Doppelte Identität bei 4 und 5**: dort existiert bereits `L → L`; mit `kg → kg` hätten beide Brennstoffe zwei Identitätsregeln. Die Ableitung trifft je nach Einheit genau eine — die Zweideutigkeit ist fachlich, nicht technisch.
- Für **kg/rm** gilt keiner dieser Einwände: keine L4-Entscheidung, keine konkurrierende Normeinheit, keine 26a-Kollision. **(b) ist für 4, 5 und 12 sauber und für 1, 2, 3, 14, 25 schädlich.** Für 24 („Sonstige") ist (b) der einzige denkbare Weg — dort ist vorher zu klären, ob dieser Sammeleintrag überhaupt eine Einheit tragen soll (5.3.3).

### 4.3 Weg (c) — die Ableitung auf `energy_carrier.billing_unit` umstellen

**Beschreibung.** Beide Schreibwege lesen die Einheit künftig aus der Katalogzeile des Trägers statt aus dem Brennstoff-Stamm. Das gliche die Schreibseite an eine **bereits geltende Konvention** an: die gesamte Leseseite arbeitet schon mit `billing_unit` (2.1).

**Umsetzungsskizze — zwei Codestellen, kein Schemaschritt.** In `WizardCtrl.TraegerSatzAnlegen` (`2ab47b1 :1501`; auf HEAD gibt es die Methode nicht, die Mechanik steht dort inline in `Add_Projekt_Energietraeger`, `:1407`) wird die ohnehin vorhandene Abfrage `SELECT ID_Brennstoff FROM energy_carrier WHERE id = ?` um `, billing_unit` erweitert; der Aufruf `GetValueById("Tab_Brennstoff_Stamm","Einheit",…)` (`:1517`) entfällt — **eine Abfrage weniger, keine neue Signatur**, `ConvIdErmitteln(int, string)` bleibt unverändert. In `EnergietraegerKatalogCtrl.UmrechnungFuer` (`:238–240`) dieselbe Erweiterung, `GetValueById` entfällt, Signatur bleibt. **Warum `Form_Kosten_Auswahl` / `Form_Kosten_VarAuswahl` dort nicht umstellbar sind:** beide laufen, **bevor** der Träger existiert. Es gibt zu diesem Zeitpunkt keine `energy_carrier`-Zeile und damit keine `billing_unit`; `SelectedBillingUnit` ist trotz seines Namens der Stammwert und die **Vorlage** für die künftige `billing_unit`. Entweder bleibt dort der Stammtext — dann bleibt die Vorwahl an dieser Stelle `-1` — oder man ändert die Vorlage, was auf Weg (a) hinausläuft.

**Wirkung** — *Bestandszeilen:* keine; (c) ändert nur, welche ID bei **künftigen** Schreibvorgängen in `ID_Umrechnung` landet. *Waise:* **keine** — auch (c) greift ausschließlich in die INSERT-Wege ein und erreicht Zeile 10076 nie. Sie bleibt auf `-1`, bis der Dialog sie speichert (dann greift die Listenindex-0-Mechanik aus 2.2, nicht die umgestellte Ableitung) oder bis P4 sie heilt. *z-Regeln 67–72:* **keine**, (c) fasst `energy_conversion` nicht an.

**Reichweite: 5 von 9** (1, 2, 3, 14, 25 ⇒ R36/38/40/53/66). Brennstoff 5 scheitert an der fehlenden `kg → kg`-Regel (`billing_unit = kg`); 4, 12 und 24 haben gar keinen Träger — dort läuft der Schreibweg allerdings auch nie: ohne Träger entsteht kein Trägersatz.

**Fallstricke.** **(c) heilt Bestand, nicht Neuanlagen** — ein neu angelegter Gasträger bekäme weiter `billing_unit = "m³"` aus dem Stammtext, und (c) fände dafür erneut keine Regel; ohne (a) bleibt die Regressionsquelle offen. `energy_price.arbeitspreis_unit` bliebe der Stammtext (`WizardCtrl.cs:1542`) — Preishistorie „m³" gegen Recheneinheit „Nm³", genau das, was Schritt 26a Teil 3 (`:4886–4898`) einmalig geradegezogen hat. Ein **Anzeigebruch entsteht nicht:** der Stammtext wird nirgends angezeigt; nach (c) zeigte der Dialog „Nm³" und träfe die Nm³-Regel. Der **Trim-Randfall** bleibt: `billing_unit` wird von `WirtschaftlichkeitCtrl:3231` getrimmt, von `ConvIdErmitteln` nicht — ein Randleerzeichen erzeugte `-1`, während die `EnergieEinheitenPruefung` den Träger für in Ordnung hielte. **`SteuerGutschriftRechner` bleibt unberührt**, er liest `billing_unit`, die (c) nicht ändert.

### 4.4 Kombinationen

| Kombination | Bewertung |
|---|---|
| **(a) + (b) nur für kg/rm** | Deckt 5 Gase sauber und 4/5/12 nativ ab; einzige Kombination, die 8 der 9 Fälle ohne fachlichen Widerspruch heilt. Beides in **einem** Schritt 62 mit zwei Teilen (62a Stammtext, 62b Saat). Für 4 und 12 ohne praktische Wirkung, solange sie keinen Träger haben. |
| **(a) + (c)** | Fachlich konsistent und redundant zugleich: nach (a) sind Stammtext und `billing_unit` für die Gase identisch, (c) ändert dann **kein messbares Ergebnis**. Der Gewinn ist strukturell — eine Wahrheitsquelle statt zweier. Als Folgepaket sinnvoll, als Sofortmaßnahme entbehrlich. |
| **(b) + (c)** | **Widersprüchlich.** (c) liest „Nm³" und trifft R40, (b) legt eine m³-Identität an, die der Wizard nach (c) gar nicht mehr findet — tote Katalogzeilen mit zusätzlichem Klapplisteneintrag. |
| **(a) + (b) für alle 9** | (b) hebt für die Gase auf, was (a) geradezieht: nach (a) steht „Nm³" im Stamm, die frisch gesäte m³-Identität bliebe unerreichbar und wäre reiner Ballast. |
| **alle drei** | Keine zusätzliche Heilung gegenüber (a) + (b für kg/rm) + (c), aber die Ballast- und Zweideutigkeitsnachteile von (b) für die Gase. Nicht empfohlen. |

---

## 5. Zu entscheiden, bevor programmiert wird

### 5.1 Empfehlung

Empfehlung: **(a) für die fünf Gase sofort als Schemaschritt 62, (c) als Folgepaket, (b) nur nach ausdrücklichem Fachentscheid und dann nur für kg/rm — für die Gase abgelehnt.**

(a) ist die bereits getroffene Entscheidung L4, nur auf die zweite Tabelle angewandt: reine Semantik, kein Zahlenwert, durch die z-Faktoren 68–72 = 1 auch rechnerisch nachweislich neutral. (a) ist zugleich der einzige Weg, der die Regressionsquelle „neuer Träger kopiert den Stammtext in `billing_unit`" schließt; ohne ihn erzeugt jede Neuanlage den Bruch erneut. (b) heilt für die Gase zwar die Zählung, führt aber eine Regel ein, die dem Katalog widerspricht und den Dialog `€/m³` auf Nm³-Zahlen schreiben ließe — der Zustand danach wäre schwerer zu erkennen als der heutige. (c) ist billig und richtig, ändert nach (a) aber nichts Messbares und gehört deshalb in ein Folgepaket.

**Entschieden (30.08.2026).** Der Anwender wählt **Weg (a)**: Der Stammtext der fünf Gase (Brennstoffe 1, 2, 3, 14, 25) wird auf „Nm³" gezogen, ausgeführt als Schemaschritt **62**. Die Wege **(b) und (c) sind nicht beauftragt** — auch nicht als Folgepaket; das Paket P3 aus § 6 entfällt damit vorerst, die Empfehlung oben bleibt als Begründung stehen. Die Randfragen **5.3.1 bis 5.3.5** (Waisenheilung, kg-/rm-Abrechnung, Brennstoff 24, Regel 67, Prüfschritt) sind **offen** und mit dieser Entscheidung ausdrücklich **nicht** mitentschieden. **Die Umsetzung ist damit noch nicht freigegeben:** Nach der Konzept-vor-Code-Regel des Projekts ist zuerst dieses Konzept abzunehmen; die Arbeit gehört anschließend nach § 6 und § 9 auf den Pufferspeicher-Strang (`2ab47b1`), nicht in diesen Worktree.

### 5.2 Varianten-Tabelle

| Weg | Wirkung | Kosten / Risiko |
|---|---|---|
| **(a) Stammtext angleichen** | heilt 5 von 9; schließt die Regressionsquelle; Bestand, Waise und z-Regeln unberührt | 1 Migrationsschritt nach bewährtem Muster; Risiko gering — die Spalte hat keinen Leser außer den vier Regelsuchen und keinen Schreiber |
| **(b) Identitätsregeln säen** | heilt 9 von 9; Bestand und Waise unberührt; 9 neue Katalogzeilen | 1 Migrationsschritt; **fachliches Risiko hoch für die Gase** (L4-Widerspruch, stille Falschvorwahl, 26a-Wiederholungsraster, dritter gleichlautender Klapplisteneintrag); für kg/rm unbedenklich |
| **(c) Ableitung auf `billing_unit`** | heilt 5 von 9 bei künftigen Schreibvorgängen; gliche Schreib- an Leseseite an | 2 Codestellen, je eine Spalte mehr im vorhandenen ID-Select, eine Abfrage weniger; **heilt Neuanlagen nicht**, Preishistorie bleibt am Stammtext |
| **(a) + (b für kg/rm)** | heilt 8 von 9 ohne fachlichen Widerspruch | 1 Schritt mit zwei Teilen; setzt Randfrage 5.3.2 voraus |
| **(a) + (c)** | wie (a), zusätzlich eine Wahrheitsquelle statt zweier | (a)-Kosten + 2 Codestellen; nach (a) ergebnisneutral |

### 5.3 Randfragen — **Anwenderentscheide**

1. **Waisenheilung (offener Punkt aus BK3 § 6, Nr. 1).** Soll `energy_project_settings` ID 10076 von `-1` auf **40** (Nm³→Nm³) gesetzt und die Begleitzeile `energy_price` id 10141 von `arbeitspreis_unit = 'm³'` auf `'Nm³'` gezogen werden? Beides sind **Bestandsdaten eines Anwenderprojekts** (1039 „Mehrgebäude"), deshalb ein eigener Entscheid und nicht Teil von (a)/(b)/(c). Dafür spricht, dass Heizwert 10,5 und Preis beide Normwerte sind; dagegen nur die Regel, Projektstände nicht ungefragt anzufassen. Ohne Heilung bleibt die Zeile still stehen, bis der Dialog sie das nächste Mal speichert.
2. **Brennstoffe 4, 5 und 12 — native kg-/rm-Abrechnung gewünscht?** Nur Weg (b) erreicht sie. (a) käme dort einem echten Einheitenwechsel gleich (4/5 → L, 12 → kg), (c) läuft ins Leere: 4 und 12 haben keinen Träger, 5 hat `billing_unit = kg` ohne passende Regel. Praktisch wirksam wäre (b) heute nur für **5** (Träger 72). Soll Flüssiggas nach Masse und Holz nach Raummaß abgerechnet werden können — oder bleibt es bei Liter bzw. Kilogramm?
3. **Brennstoff 24 „Sonstige"** hat weder Träger noch irgendeine Regel und trägt `Einheit = m³`. Soll dieser Sammeleintrag überhaupt eine feste Einheit führen? Wenn ja, welche — dann wäre (b) der einzige Weg. Wenn nein, bleibt er dauerhaft bei `-1`, was ohne Träger folgenlos ist.
4. **Fremdkörper Regel 67.** Soll `Nm³ → kWh` mit Faktor 0,5 im selben Zug bereinigt werden — auf den gepflegten Heizwert 10,5 gezogen, umbenannt (sie ist kein z-Faktor) oder deaktiviert? Die Regel ist **referenzlos**, ein Eingriff berührt keinen Projektstand; sie erscheint aber im Klapplisten- und im Regelblock für Erdgas E.
5. **Prüfschritt nachrüsten?** Soll `EnergieEinheitenPruefung` um einen Befund „Stamm.Einheit ↔ billing_unit ↔ vorhandene Identitätsregel" erweitert werden (3.5, Nr. 7)? Das verhindert die Wiederkehr, ist aber ein eigenständiges Paket.

---

## 6. Vorgehensweise — **nur für den Empfehlungsfall** (a) jetzt, (c) danach

### P1 — Schemaschritt 62: Stammtext der Gase angleichen *(die eigentliche Aufgabe)*

- **Zweig:** Die Umsetzung gehört auf den **Pufferspeicher-Strang** (`2ab47b1`), wo BK3 und die Schemaschritte 56–61 liegen. Auf main (`b3d305b`, Ziel 55, 56/57 geparkt) wäre die Nummer 62 ein Vorgriff und löste beim Programmstart die Zielstand-Warnung mit Sperre des Simulationsbereichs aus.
- Reihenfolge einhalten: **Schrittkonstante `SCHRITT_62_…` mit XML-Doc (Anlass, Umfang, Idempotenzzusage) → Methode → `SCHRITTE`-Eintrag → erst dann `ZIEL_VERSION = 62`.**
- Methode nach Muster `NormkubikUmbenennen`: Gas-Brennstoffnummern vorab per parametrisierter Abfrage auflösen (`GasBrennstoffListe` wiederverwenden), dann `UPDATE Tab_Brennstoff_Stamm SET Einheit = ? WHERE ID IN (<int-Liste>) AND Einheit = ?` mit `DbWerte.EINHEIT_NORMKUBIKMETER` / `EINHEIT_KUBIKMETER`. Betroffene Zeilenzahl (erwartet 5) in `l.Notiz`. Idempotenz ergibt sich aus `AND Einheit = 'm³'`: ein zweiter Lauf trifft null Zeilen.

### P2 — Abnahme

Prüfliste Abschnitt 7. Vorher `Kenndaten.laccdb` prüfen und eine datierte Kopie nach `DB-Backup/` legen.

### P3 — Weg (c) *(Folgepaket, Begründung 4.3)*

Die zwei Codestellen umstellen, `Form_Kosten_Auswahl`/`_VarAuswahl` bewusst unverändert lassen (4.3). Nach P1 ergebnisneutral — deshalb eine reine Strukturbereinigung, die getrennt geprüft und gemergt werden kann.

### P4 — Waisenheilung *(nur nach Entscheid 5.3.1)*

Eine Zeile in `energy_project_settings`, optional eine in `energy_price`. Als eigener, klein gehaltener Migrationsschritt (dann Nummer 63) oder als einmalige Datenpflege — der Unterschied ist, ob andere Installationen dieselbe Waise haben können. **Nicht erhoben:** ob außerhalb dieser Datenbank weitere `-1`-Zeilen existieren.

---

## 7. Prüfliste für die Abnahme

| # | Prüfung | Erwartung |
|---|---|---|
| 1 | `SELECT Einheit FROM Tab_Brennstoff_Stamm WHERE ID IN (1,2,3,14,25)` | 5× `Nm³` (mit U+00B3) |
| 2 | `ConvIdErmitteln(3, "Nm³")` | **40** |
| 3 | `ConvIdErmitteln` für 1 / 2 / 14 / 25 | **36 / 38 / 53 / 66** |
| 4 | Ableitung über alle 25 Brennstoffe | ≠ `-1` für die Empfehlungsmenge {1,2,3,14,25}; unverändert für die 16 heilen; weiterhin `-1` für {4,5,12,24} |
| 5 | `Tab_Applikation.SchemaVersion` nach Migrationslauf | **62** |
| 6 | Migration ein zweites Mal laufen lassen | 0 geänderte Zeilen, kein Fehler, `l.Notiz` weist den Nulltreffer aus |
| 7 | `SELECT COUNT(*) FROM energy_conversion` | **65** — unverändert, (a) legt keine Regel an |
| 8 | z-Regeln 67–72 feldweise | unverändert (from/to/factor/faktor_name/aktiv/user_edited) |
| 9 | Bestandszeile 10076 | **unverändert** (`ID_Umrechnung = -1`), solange 5.3.1 nicht entschieden ist; nach P4: `40` |
| 10 | `energy_project_settings` gesamt | 34 Zeilen, 34 gültige Trägerverweise; gültige Umrechnungsverweise 33 (bzw. 34 nach P4) |
| 11 | Neuen Gasträger über `Form_Kosten` anlegen | `energy_carrier.billing_unit = "Nm³"` (nicht „m³") — Regressionsquelle geschlossen |
| 12 | **Neues** Projekt anlegen und Träger 63 zuordnen, einmal über den Wizard und einmal über den Ä10-Weg (`Form_Energietraeger` → `InsProjekt`) | die **neu angelegte** Zeile in `energy_project_settings` trägt `ID_Umrechnung = 40`. Das ist die eigentliche Wirkung von (a); die Klapplisten-Vorwahl im Dialog läuft über `projectSettings.IDUmrechnung` und prüft (a) **nicht** |
| 13 | Trägerdialog für Träger 63 in einem Projekt **mit** bestehender Regel 40 öffnen (**nicht** Projekt 1039) | Klappliste auf `Nm³` vorgewählt, Beschriftungen `€/Nm³`, `kWh/Nm³`; kein Sprung auf die erste Listenregel — Regressionsprobe, sie besteht schon vor (a) |
| 14 | Denselben Dialog **ohne Änderung** schließen, Zeile danach lesen | `ID_Umrechnung` unverändert (Regressionsprobe zu BK3 § 6, Nr. 5) |
| 15 | `EnergieEinheitenPruefung` für Träger 63 | weiterhin ohne Befund (sie startet auf `billing_unit`, das sich nicht ändert) |
| 16 | Wirtschaftlichkeitslauf eines Projekts mit Träger 63 vor/nach | zahlengleich — Hi/Hs unverändert, `factor` wird von der Rechnung nicht gelesen |

**Reihenfolge beachten.** Die Prüfungen 13 und 14 dürfen **nicht** auf Projekt 1039 laufen: `Form_Kosten.OnFormClosing` (`:587–608`) ruft `SaveProjectAndHistory` auch ohne Nutzeraktion (BK3 § 6, Nr. 5). Ein bloßes Öffnen und Schließen des Dialogs auf 1039 überschriebe die Waise mit der Listenindex-0-Regel und machte Prüfung 9 unbrauchbar.

---

## 8. Aufwand

| Paket | Umfang | Aufwand |
|---|---|---|
| P1 Schemaschritt 62 | 1 Konstante, 1 Methode, 1 `SCHRITTE`-Eintrag, `ZIEL_VERSION` | 2–3 h |
| P2 Abnahme | 16 Prüfungen, davon 5 in der laufenden Anwendung | 2–4 h |
| P3 Weg (c) | 2 Codestellen + Regressionsprobe | 1–2 h |
| P4 Waisenheilung | 1–2 Zeilen, nur nach Entscheid 5.3.1 | 1–2 h |
| (b) für kg/rm, falls entschieden | Saat nach `ZFaktorSaeen`-Muster, 3 Zeilen | 2–3 h |

**Empfehlungsfall P1 + P2: rund 4–7 h.** P3 im selben Zug oder danach.

---

## 9. Fallstricke bei der Umsetzung

- **ACE-Falle `UPDATE … WHERE x IN (SELECT …)`.** Mit Parameter in der Unterabfrage trifft das in ACE **null Zeilen — ohne Fehler und ohne Warnung** (`SchemaMigration.cs:4622–4636`). IDs vorher per parametrisierter Abfrage auflösen und als ganzzahlige Liste interpolieren. Die `NonQuery`-Zeilenzahl **immer** in `l.Notiz`, sonst bleibt ein Nulltreffer unbemerkt.
- **Reihenfolge-Konvention.** Erst Schrittkonstante, Methode und `SCHRITTE`-Eintrag, **dann** `ZIEL_VERSION`. Der umgekehrte Weg hat am 29.08.2026 jeden Programmstart mit einer Zielstand-Warnung enden lassen und den Simulationsbereich gesperrt.
- **`user_edited`-Schutz.** Jedes `UPDATE` auf `energy_conversion` muss `AND (user_edited = FALSE OR user_edited IS NULL)` führen. Im Bestand stehen zwar alle 65 Regeln auf `False`, in Anwenderdatenbanken nicht zwingend.
- **Kein Trim im Prädikat.** `ConvIdErmitteln` trimmt bewusst nicht, die `EnergieEinheitenPruefung` schon. Ein Randleerzeichen in `Einheit` oder `billing_unit` erzeugt ein `-1`, das die Prüfung nicht meldet.
- **Codepoints `U+00B3` vs. ASCII-`3`.** `m³`/`Nm³` tragen U+00B3, die `to_unit`-Werte `m3` und die `PreisEinheit`-Werte `€/m3` das ASCII-Zeichen. `WHERE Einheit = 'm3'` trifft nichts, ein zu weites `LIKE` trifft zu viel — vor Zeichenänderungen Bytebeweis führen.
- **`.accdb` nie in einen Commit.** Die Endung ist in `.gitignore` ausgeschlossen; ein Rückweg über Git existiert nicht. Sicherung ausschließlich als datierte Kopie unter `DB-Backup/`.
- **`Kenndaten.laccdb` vor jedem Schreibzugriff prüfen.** Existiert sie, ist die Datenbank geöffnet. Für Messläufe grundsätzlich mit einer Laufkopie arbeiten, wie es die BK-Reihe tut.
- **Worktree statt Haupt-Checkout.** Diese Analyse ist im Git-Worktree `.claude/worktrees/lucid-cori-a9a425` auf Zweig `claude/lucid-cori-a9a425` entstanden, dessen `SchemaMigration.cs` die main-Lage trägt (Ziel 55, 56/57 geparkt). Der Bezugsstand `2ab47b1` liegt im Haupt-Checkout auf `Pufferspeicher` mit Ziel 61. Vor der Umsetzung in den richtigen Arbeitsbaum wechseln, sonst entsteht Schritt 62 gegen die falsche Schemalage.
- **Zweigwahl.** Der Bezugsstand ist ungepusht (13 Commits vor `origin/Pufferspeicher`), und `claude/nostalgic-matsumoto-481128` existiert nur lokal. Vor der Umsetzung klären, ob zuerst gepusht oder auf dem lokalen Strang weitergearbeitet wird.

---

## 10. Verwandte Dokumente

- [`KONTEXT_Stammdaten_Aenderbarkeit.md`](KONTEXT_Stammdaten_Aenderbarkeit.md) — Gerüst und Tonfall dieses Dokuments; Umgang mit Katalogzeilen und Migrations-Kollisionen
- `WindowsFormsApplication1/Allgemein/Reporting/BK3_InsProjekt_Defaultspalten_Protokoll.md` — § 5 hält den Einheitenbruch und die Zählung „16 mit / 9 ohne Identitätsregel" fest, § 6 die fünf offenen Anwenderentscheide. **Nur auf dem Zweig `Pufferspeicher` (`2ab47b1`) vorhanden**, nicht in main und nicht in diesem Worktree
- `WindowsFormsApplication1/Allgemein/Reporting/BK1_Traegerzuordnung_Protokoll.md` — Vorstufe (Standard-Stromträger, Katalogwahrheit), ebenfalls nur auf `Pufferspeicher`
- [`Konzept_Kosten_Energietraeger_EPOS-Plan.md`](Konzept_Kosten_Energietraeger_EPOS-Plan.md) — Fachkonzept der Energieträger-Kostenseite
- [`Konzept_Kostendialoge_EPOS-Plan.md`](Konzept_Kostendialoge_EPOS-Plan.md) — Trägerdialog und Kostenformulare
- [`CLAUDE.md`](CLAUDE.md) — Umgang mit der Datenbank, `.gitignore`-Regel, Sperrdateiprüfung
