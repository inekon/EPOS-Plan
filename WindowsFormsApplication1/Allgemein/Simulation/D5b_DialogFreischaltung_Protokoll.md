# Etappe D5b — Dialog-Freischaltung der Quellenwahl

Stand 16.08.2026 · Umsetzung zu [`Konzept_KonfigUI_Hydraulik.md`](Konzept_KonfigUI_Hydraulik.md),
Etappe **D5b**: die sieben Restpunkte aus
[`D5a_KombiKaskade_Protokoll.md`](D5a_KombiKaskade_Protokoll.md), Abschnitt 9.16.

D5a hat den RECHENKERN von Kombispeicher und Kessel-Kaskade gebaut; einrichten ließ sich
beides nur über die Datenbank. D5b macht daraus eine bedienbare Funktion: Der Heizkessel
wählt seinen Quellpuffer in der Kartenansicht, die beiden Verbotsregeln aus Konzept
Abschnitt 7 melden **im Dialog** statt erst im Lauf, und der Kombispeicher zeigt seine
Entladeposition in **beiden** Kanälen.

**Codestand:** Haupt-Checkout `C:\Waermeplan\WP_Plan`, HEAD **5caa283**, Working Tree =
D5b. **Nichts committet.** Build: 0 Fehler, exakt die sechs Bestandswarnungen
(`WErzeugerModel` CS0108, `StromverbraucherStammCtrl` CS0108, `KlimaregionStammCtrl`
CS0109 ×2, `MDIMainForm` CS4014 + CS1998).

---

## 1. Was umgesetzt ist

| # | Restpunkt aus 9.16 | Ergebnis |
|---|---|---|
| 1 | Positionsanzeige des Kombispeichers nur im Heizkanal | beide Kanäle, je mit Kanalnamen (2 Katalogschlüssel) |
| 2 | Quellen-Freischaltung je `ID_Type` | Kessel wählt Puffer-Quelle in der Karte; BHKW/Solarthermie bekommen keinen Chip |
| 3 | Dialog-Zyklusprüfung | `WaermesenkeClass.RingMeldung` — dieselbe Ebenen-Relaxation wie die Engine |
| 4 | Dialog-Kurzschlussprüfung (auch Kessel) | `WaermesenkeClass.KurzschlussMeldung` |
| 5 | `WQ_ID_Puffer` als führende Quellidentität (E0) | zweite Auflösungskette in der Anzeige beseitigt; nicht aufgelöster Altbestand wird im Dialog ausgewiesen |
| 6 | Groß-/Kleinschreibung `IstKombiVerwendung` ↔ `IstKombi` (K3-2) | Normalisierung an EINER Stelle (`WirksameVerwendung`) |
| 7 | Prüfpunkt zu E-K2-3 | Konstellation über `WS_Ladegrenze` konstruiert, Wirkung gemessen (Abschnitt 4) |

### 1a. Quellenwahl je Erzeugerart

Die Grenze zieht **eine** Funktion, `WaermequelleClass.QuellenwahlMoeglich(ID_Type)`:
Wärmepumpe und Heizkessel ja, Solarthermie und BHKW nein. Das ist genau die Grenze, die
die Engine seit der D5a-Nacharbeit zieht (Befund E-K2-2: nur diese beiden Modularten
werten eine Ebenenmaske aus) — was die Engine ohnehin mit einer Warnung verwirft, darf die
Oberfläche gar nicht erst anbieten.

Das Angebot je Art kommt aus `TypWerteFuer`/`TypAnzeigeFuer`:

| Art | Auswahl im Inline-Dropdown |
|---|---|
| Wärmepumpe | Außenluft, Konstante Temperatur, Pufferspeicher, Quellprofil, CSV-Datei, Erdreich (unverändert, indexgleich zu `TypWerte`) |
| Heizkessel | **Systemrücklauf** (`WQ_Typ = ""`), **Pufferspeicher** |
| Solarthermie, BHKW | — (leere Liste; die Karte trägt keinen Quellen-Chip) |

Erdreich, Quellprofil, CSV und konstante Temperatur beschreiben die **Verdampfer**seite und
haben am Brenner keine Entsprechung; `SimulationSPK` liest keine davon. Aus demselben Grund
blendet `Form_QuellePufferspeicher` beim Kessel die Rubrik „Parameter der Wärmequelle"
(Quelltemperatur, nutzbare Spreizung, Regeneration, „unbegrenzt verfügbar") aus und
**schreibt sie auch nicht** — sonst überschriebe eine Kesselbearbeitung die Vorgaben einer
Wärmepumpe mit den Vorbelegungen 10 °C / 5 K. An ihre Stelle tritt die Erklärung der
Kaskade samt Anteilsformel.

Die Karte des Kessels zeigt jetzt in jedem Fall einen anklickbaren Quellen-Chip: ohne
Quellpuffer „Quelle: Systemrücklauf", mit Quellpuffer „Quelle: *Name* · Kaskade" —
derselbe Chip wie bei der Wärmepumpe, seit D5b auch mit Editorziel (vorher
`ChipZiel.Keines`, Review-2-Befund K3-1).

### 1b. Die beiden Dialogprüfungen

Beide sind dialogfrei in `WaermesenkeClass` und ohne Oberfläche aufrufbar — dieselbe
Bauart wie `Pruefen` für die Senkenseite, damit ein Prüfprogramm sie headless fahren kann.
Gerufen werden sie aus `Form_Simulation_Config.WqCombo_SelectedIndexChanged`, **bevor
irgendetwas geschrieben wird**:

* **Kurzschluss** (`KurzschlussMeldung`): Der gewünschte Quellpuffer ist zugleich Haupt-
  oder Zweitsenke derselben Anlage (Konzept 4.6). Gegenstück zu Punkt 4 in `Pruefen`, nur
  von der anderen Seite gefragt. Gilt für Wärmepumpe **und** Heizkessel — die Engine weist
  seit E-K2-1 beide ab.
* **Ring** (`RingMeldung`): Eine Anlage lädt einen Speicher, aus dem sie über weitere
  Erzeuger wieder ihre eigene Quellwärme bezieht — auch indirekt über A→B→C→A. Gerechnet
  wird **die Ebenen-Relaxation aus `Kaskadenschleife.EbenenRelaxieren`**, nicht eine eigene
  Ringsuche: Eine zweite Auslegung derselben Frage könnte eine Konfiguration durchlassen,
  an der die Engine hinterher abbricht. Übernommen sind auch die beiden Einschränkungen der
  Engine — Quellbezüge zählen nur bei WP und Kessel (E-K2-2), und der Selbstbezug ist
  übersprungen, weil er der Kurzschluss ist und einen eigenen Text bekommt.

  Anders als die Engine arbeitet die Prüfung auf der **Datenbanksicht** statt auf den
  Ladeaufträgen (die entstehen erst im Lauf); die Bedingung „lädt" ist dieselbe wie in
  `Ladeordnung.Ladereihenfolge` — Puffer-ID auf einem der beiden Senkenfelder UND ein
  Puffer-Ziel dazu.

**Die Engine-Guards bleiben unangetastet.** Sie sind die zweite Verteidigungslinie für
Altdaten und für jeden Weg, der nicht über diesen Dialog läuft.

### 1c. E0 — `WQ_ID_Puffer` als führende Identität

Der Schreibpfad war seit D2/D3 in Ordnung (`Form_QuellePufferspeicher` liefert die ID,
`WqCombo_SelectedIndexChanged` schreibt den Fremdschlüssel zuerst). Offen waren zwei
Stellen:

* **Zweite Auflösungskette in der Anzeige.** `WaermequelleAnzeige` löste den Namen selbst
  auf: Fremdschlüssel, sonst der Bezeichnertext **roh**. Damit konnte die Karte einen Namen
  zeigen, zu dem es im Projekt gar keinen Puffer gibt — also eine Quelle, die im Lauf nicht
  existiert. Jetzt läuft sie über `WaermesenkeClass.QuellPufferDerAnlage`, dieselbe
  Rangfolge wie Engine und Erzeugerkarte; der Alttext bleibt reiner Anzeige-Rückfall.
* **Nicht aufgelöster Altbestand.** Schritt 9 der `SchemaMigration` (Regel R7) hat
  `WQ_Puffer` nur bei **eindeutigen** Treffern in `WQ_ID_Puffer` überführt. Bleibt der
  Bezeichner allein stehen, baut die Engine **keinen** Quellbezug auf
  (`QuellbezuegeAufbauen` verlangt `WQ_ID_Puffer > 0`) — und der Dialog wählte still den
  namensgleichen Puffer (oder, ohne Treffer, schlicht den ersten der Liste) und schrieb
  dessen ID beim Bestätigen fest. Das ist die richtige Auflösung, aber der Anwender muss
  sehen, dass er sie gerade trifft: Dafür steht jetzt der Hinweis
  `SIMQ_PUFFER_HINWEIS_ALTBEZEICHNER` im Dialog.

### 1d. K3-2 — Schreibweise der Verwendung

`WaermesenkeClass` vergleicht `OrdinalIgnoreCase`, der Rechenkern ordinal. Ein DB-Wert
`"kombi"` stand damit in **beiden** Entladereihenfolgen — die Anzeige nimmt ihn an —,
verhielt sich im Lauf aber wie ein Heizungspuffer: Der Warmwasserkanal bekam eine Zusage,
die niemand einlöst.

Aufgelöst an **einer** Stelle: `WirksameVerwendung` normalisiert auf den Persistenzwert,
und sie ist die Zeile, über die `SimulationControl.SpeicherRegistryAufbauen` die
`SimulationPufferspeicher.Verwendung` füllt. Nach dieser Zeile gibt es im Rechenkern nur
noch kanonische Werte, und die ordinalen Vergleiche dort sind wieder richtig.

**Bestandspfad unberührt:** Für die drei kanonischen Werte, für die leere Angabe und für
jeden unbekannten Wert ist das Ergebnis Zeichen für Zeichen das bisherige. Die
Referenz-Datenbank führt ausschließlich `""` (111×), `"Heizung"` (6×) und `"Brauchwasser"`
(1×) — an der Kopie nachgezählt, siehe Abschnitt 3.

---

## 2. Änderungen je Datei

Zeilennummern nach dem Endstand dieser Etappe.

| Datei | Stelle | Inhalt |
|---|---|---|
| `Allgemein/DbWerte.cs` | 142 | `WQ_TYP_OHNE = ""` — der leere Spaltenwert als benannter Steuerwert |
| `Allgemein/Simulation/WaermequelleClass.cs` | 55 | Alias `TYP_OHNE` |
| " | 100–160 | `QuellenwahlMoeglich`, `TypWerteFuer`, `TypAnzeigeFuer` (Freischaltung je `ID_Type`) |
| `Allgemein/Simulation/WaermesenkeClass.cs` | 553–612 | `WirksameVerwendung` über `NormalisierteVerwendung` (K3-2) |
| " | 793–840 | `QuellPruefErgebnis`, `QuellePruefen` |
| " | 842–875 | `KurzschlussMeldung` |
| " | 877–979 | `RingMeldung` (Ebenen-Relaxation auf der Datenbanksicht) |
| " | 981–1020 | `LaderEintragen`, `RingBeteiligte` |
| `Allgemein/Simulation/SimulationPufferspeicher.cs` | 570–585 | Begründung, warum der ordinale Vergleich in `IstKombi` nach D5b tragfähig ist |
| `Views/Simulation/Form_Simulation_Config.Karten.cs` | 919–987 | `QuellenChip` — Freischaltung je Art, Kessel-Chip „Systemrücklauf", Kaskaden-Chip anklickbar |
| `Views/Simulation/Form_Simulation_Config.Uebersicht.cs` | 59–66 | Feld `_wqTypen` (die gerade angebotene Werteliste) |
| " | 407–428 | `WaermequelleAnzeige` über `QuellPufferDerAnlage` (E0) |
| " | 718–748 | `WaermequelleBearbeiten`: Türsteher über `QuellenwahlMoeglich`, Luft-Wasser-Sperre bleibt WP-spezifisch |
| " | 825–878 | `WaermequelleAuswahlAnzeigen` mit den Listen je Art und der Vorauswahl |
| " | 885–905 | `WqCombo_SelectedIndexChanged`: Index gegen `_wqTypen`, neuer Zweig `TYP_OHNE` (Kaskade abbauen, FK auf NULL) |
| " | 923–999 | Kessel-Fassung des Quellendialogs, die beiden Prüfungen vor dem Schreiben, Verdampfer-Parameter nur für die WP |
| `Views/Simulation/Form_QuellePufferspeicher.cs` | 46–60 | Felder `_gbParameter`, `_lblHinweisArt`, `_lblKaskade` |
| " | 100–116 | `ID_Type` (vorbelegt WP) und `IstKessel` |
| " | 205–218 | `_lblLeer` 242/34 → 240/48 (drei Zeilen für den Alt-Hinweis) |
| " | 294–306 | Kaskadenerklärung an der Stelle der Verdampfer-Rubrik |
| " | 338, 352–378 | `ArtAnwenden` — Rubrik und Erklärtexte je Erzeugerart |
| " | 396–420 | `PufferListeLaden` mit dem Alt-Bezeichner-Hinweis (E0) |
| " | 520–534 | `btnOk_Click`: keine Verdampfer-Prüfung beim Kessel |
| `Views/Pufferspeicher/Form_PufferSp_Projekt.cs` | 693–710 | `EntladungAnzeigen` verzweigt beim Kombispeicher |
| " | 714–748 | `KombiPositionstext`, `KanalPositionstext` |
| `MyResource/Resource.resx`, `Resource.en-US.resx`, `Resource.Designer.cs` | — | 11 neue Schlüssel, ein Text gekürzt (Nachtrag in [`Lokalisierung_Katalog.md`](Lokalisierung_Katalog.md)) |

---

## 3. Regression

Alle Läufe auf **einer** vollständig migrierten Kopie der produktiven `Kenndaten.accdb`
(`Referenzlauf.exe migration`, Quelle **nur gelesen**, keine `Kenndaten.laccdb` vorhanden).
Projektmenge 1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024.

| Lauf | Vergleich | Ergebnis |
|---|---|---|
| **Flag AUS**, Umgebungsprobe **vor** den Änderungen | eingefrorene Basis `Referenzlaeufe/2026-08-15_B3` gegen HEAD `5caa283` | 8/8 PASS, 2 094 447 Werte |
| **Flag AUS** | eingefrorene Basis `2026-08-15_B3` gegen **D5b** | **8/8 PASS, 2 094 447 Werte, 190/190 Dateien byte-/MD5-gleich** |
| **Flag AN**, unpräpariert (alle acht Projekte `Kaskade_Zweikanalig = True`) | HEAD `5caa283` gegen **D5b**, **dieselbe** Kopie | **8/8 PASS, 2 094 458 Werte, 190/190 byte-/MD5-gleich** |

Die zwei Dateien, die B3 mehr führt (`lauf_protokoll.md`,
`lauf_protokoll_werkzeug.md`), sind Protokolle, keine Ergebnisse.

**Zur Normalisierung aus K3-2 gehört der Datenbefund:** In der Kopie trägt
`Tab_Pufferspeicher.Verwendung` ausschließlich `""` (111 Zeilen), `"Heizung"` (6) und
`"Brauchwasser"` (1) — keine abweichende Schreibweise. Damit ist die Normalisierung auf
dieser Datenbasis nachweislich wirkungslos, und genau das zeigen die beiden
Byte-Gleichheiten.

---

## 4. E-K2-3 — die auslösende Konstellation, jetzt gemessen

D5a konnte die Wirkung des Fixes nicht belegen (Abschnitt 9.12: „Ein Beleg über eine
gemessene Ergebnisänderung steht aus; er wäre mit einer Ladegrenze (`WS_Ladegrenze`)
unterhalb des Füllstands zu konstruieren"). Das ist nachgeholt.

**Die Konstellation.** `Bilanzraum = Ladefähigkeit + min(Kanalbedarf, Entnahmefähigkeit)`;
`Entnahmefähigkeit` ist unbegrenzt, solange keine Entladeleistung gepflegt ist. Der Fix
wirkt also genau dann, wenn **Ladefähigkeit = 0** und **`rest_heiz` = 0** und
**`rest_ww` > 0** zusammenkommen: Ohne ihn ist der Bilanzraum 0, `Ladefaehig` liefert
`false`, das Modul wird in Phase B übersprungen, und die nächste Kaskadenstufe deckt den
Warmwasserbedarf. Erzwungen wird das über **zwei verschiedene Ladeobergrenzen auf
demselben Kombispeicher**: Die Wärmepumpen dürfen nur bis 10 %, der Heizkessel bis zur
regulären Abschaltschwelle — er hält den Speicher also über der Grenze der Wärmepumpen.

Projekt 1023, Flag AN, eigene Kopie:

```sql
UPDATE Tab_Einstellungen  SET Kaskade_Zweikanalig = True                       WHERE ID_Projekt = 1023
UPDATE Tab_Pufferspeicher SET Verwendung='Kombi', Schwelle_Ein=90, Schwelle_Aus=95 WHERE ID = 1018023
UPDATE Tab_Energieanlagen SET WS_Ziel='PufferKombi', WS_ID_Puffer=1018023, WS_Ladegrenze=10 WHERE ID IN (11203, 11204)
UPDATE Tab_Energieanlagen SET WS_Ziel='PufferKombi', WS_ID_Puffer=1018023, WS_Ladegrenze=0  WHERE ID = 11205
```

Gemessen mit zwei Bauzuständen derselben Quellen, die sich in **einer Zeile**
unterscheiden (`SimulationWaermepumpe.Kanalbedarf`, Zeile `if (sp.IstKombi) return
rest_heiz + rest_ww;`):

| Größe | ohne Fix | **mit Fix** | Δ |
|---|---|---|---|
| WP-Wärmeproduktion | 116,31 MWh | **126,77 MWh** | **+10,46** |
| WP-Betriebsstunden (Modul 0 / 1) | 5576 / 4933 | **6259 / 5082** | +683 / +149 |
| Heizkessel, Wärmeproduktion | 92,19 MWh | **81,69 MWh** | −10,50 |
| **Gasverbrauch** | 106,29 MWh | **94,62 MWh** | **−11,67** |
| WP-Stromverbrauch | 47,51 MWh | 50,47 MWh | +2,96 |
| Ladung des Kombispeichers | 87 561,3 kWh | **92 245,8 kWh** | +4 684,5 |
| Entladung des Kombispeichers | 87 284,7 kWh | 92 011,9 kWh | +4 727,2 |
| Restwärmebedarf nach der Wärmepumpe | 217,23 MWh | **206,80 MWh** | −10,43 |
| Ergebnisdateien mit Unterschied | — | **6 von 22** (`aggregate`, `kessel_leistung`, `kessel_restwaerme`, `reststrom_viertelstunde`, `wp_produktion`, `wp_strom`) | |

**Damit ist der Fix belegt.** Die Wärmepumpe läuft in den Stunden, in denen der
Kombispeicher zwar nicht mehr ladefähig ist, aber offenen Warmwasserbedarf durchreichen
kann — Wärme, die vorher der nachgeschaltete Erzeuger liefern musste. Der Effekt zeigt
genau die Richtung, die Review 2 beschrieben hat.

---

## 5. Flag AN — Stichprobe Kessel-Kaskade über den NEUEN Dialogweg

Zwei identische Wegwerf-Kopien, beide mit derselben SQL-Grundlage (Flag AN, WP 11203/11204
laden Puffer 1018023, `Tab_Heizkessel` 70/50). Der Unterschied ist allein, **wie** der
Quellbezug des Kessels entsteht:

* **Kopie „ksql":** `UPDATE Tab_Energieanlagen SET WQ_Typ='Pufferspeicher',
  WQ_ID_Puffer=1018023, WQ_Puffer='Vitocell 140-E 600 Ltr' WHERE ID = 11205`
* **Kopie „kui":** über den neuen Dialogweg — Erzeugerkarte des Kessels →
  Quellen-Dropdown Eintrag „Pufferspeicher" → `Form_QuellePufferspeicher` → Speicher
  markieren → OK. Gefahren im Prüfprogramm über
  `Form_Simulation_Config.WqCombo_SelectedIndexChanged`, also die echte Ereigniskette.

| Prüfung | Ergebnis |
|---|---|
| Dialogweg: Meldungen | **keine** |
| Dialogweg: geschriebene Spalten | `WQ_Typ = 'Pufferspeicher'`, `WQ_ID_Puffer = 1018023`, `WQ_Puffer = 'Vitocell 140-E 600 Ltr'`; `WQ_Spreizung` unberührt (NULL) |
| Lauf „ksql" gegen Lauf „kui" | **25/25 Dateien byte-/MD5-gleich** |

Und die Zahlen sind **Stelle für Stelle die aus D5a, Abschnitt 9.4** — dieselbe Hydraulik,
über die Oberfläche eingerichtet:

```
WP-Produktion                 130 000,321 kWh      (D5a: 130 000,3)
Kessel, brennstoffbasiert      86 234,99  kWh      (D5a:  86 235,0)
Gasverbrauch                   99,31      MWh      (D5a:  99,3103)
Heizstab                       48 440,12  kWh      (D5a:  48 440,1)
Puffer 1018023  Ladung         87 868,61  kWh      (D5a:  87 868,6)
                Entladung      87 642,58  kWh      (D5a:  87 642,6)
                Verluste          226,03  kWh · SOC am Jahresende 0,000
```

---

## 6. Oberflächen- und Datenproben (Prüfprogramm)

Wegwerf-Harness nach dem bewährten Muster (net8-x86-Konsole, `Assembly.LoadFrom` +
Reflection, `Properties.Settings.DBPath` auf eine Wegwerf-Kopie umgebogen, OleDb-RID-Falle
beachtet). Der modale Quellendialog wird von einem WinForms-Timer bedient, der **während**
`ShowDialog` mitläuft; Meldungsfenster fängt ein Wächter-Thread über `EnumThreadWindows`
und die Fensterklasse `#32770`. **88 Proben, 0 Fehler.**

| Block | Proben | Kernaussagen |
|---|---|---|
| **T1** Freischaltung je `ID_Type` | 9 | WP 6 Typen; Kessel genau `["", "Pufferspeicher"]` — kein Erdreich/Profil/CSV/Konstant; BHKW und Solarthermie leere Liste; Anzeige indexgleich |
| **T2** Quellen-Chip der Karte | 4 | Kessel ohne Quellpuffer: „Quelle: Systemrücklauf" mit `ChipZiel.Quelle`; mit Quellpuffer: „Quelle: *Name* · Kaskade", ebenfalls anklickbar; BHKW mit Quellpuffer: Chip **nicht** anklickbar |
| **T3** Roundtrip über den Dialogweg | 7 | `WQ_Typ`/`WQ_ID_Puffer`/`WQ_Puffer` geschrieben, `WQ_Spreizung` unberührt; Rückweg „Systemrücklauf" setzt `WQ_Typ` leer **und** `WQ_ID_Puffer` auf NULL |
| **T4** Kurzschluss | 11 | Positiv: Quelle = Hauptsenke, Quelle = Zweitsenke, beides auch an der WP — Meldung nennt Speicher und Rolle. Negativ: anderer Puffer, keine Puffer-Senke. **Dialogweg:** genau eine Meldung, **nichts geschrieben** |
| **T5** Ring | 9 | Positiv: direkter Ring über zwei Anlagen, **indirekter Ring A→B→C→A**. Negativ: Booster-Kette WP1→A→WP2→B, dreistufige Kette ohne Ringschluss, BHKW-Quellbezug (bekommt keine Ebene). Selbstbezug bleibt **Kurzschluss**, nicht Ring. **Dialogweg:** eine Ringmeldung, **nichts geschrieben** |
| **T6** Kombi in beiden Kanälen | 16 | de **und** en zweizeilig („Heizkanal: als 2. von 80 entladen." / „Warmwasserkanal: als 2. von 2 entladen."), kein Mojibake, Text passt gemessen ins Feld (288×32, benötigt 218×30 bzw. 224×30); reiner Heizungspuffer bleibt beim einzeiligen Bestandssatz; der Kombi steht in **beiden** Entladereihenfolgen |
| **T7** Katalog | 11 | alle 11 neuen Schlüssel in de **und** en vorhanden, verschieden, ohne `U+FFFD` und ohne `??` |
| **T8** K3-2 | 13 | `"kombi"`/`"KOMBI"` → `"Kombi"`, `"brauchwasser"` → `"Brauchwasser"`, `""` → `"Heizung"`, `"Unsinn"` unverändert; danach greifen `SimulationPufferspeicher.IstKombi` und `BedientKanal` in beiden Kanälen |
| **T9** E0 / Dialogfassungen | 8 | Bezeichner-Altbestand wird aufgelöst und in Karte **und** Dialog ausgewiesen (Hinweis passt in 300×48, benötigt 299×30); Kessel-Fassung blendet die Verdampfer-Rubrik aus und die Kaskadenerklärung ein (passt in 590×130, benötigt 582×120), WP-Fassung umgekehrt |

Sichtbarkeits-Asserts laufen über ein **angezeigtes** Formular (`Show()` + `DoEvents()`) —
`Control.Visible` hängt sonst an der Elternkette und ist immer `false`.

---

## 7. Kodierung, Zeilenenden, Diff

Byteweise nachgezählt (CR-Bytes = Zeilenzahl, kein `U+FFFD`): die **elf** Code- und
Ressourcendateien sind **UTF-8 mit BOM**, die **drei** Markdown-Dateien ohne BOM — beides
ist die jeweilige Bestandskonvention. **Alle vierzehn ausschließlich CRLF.**

Zwei Dateien — `WaermequelleClass.cs` und `Form_QuellePufferspeicher.cs` — hatten beim
Bearbeiten ihre CR verloren und sind byte-genau zurückgestellt; `git diff` meldet keine
Zeilenendenwarnung mehr.

`git status` zeigt genau die beabsichtigten Dateien (Pfade ab
`WindowsFormsApplication1/`):

```
 M Allgemein/DbWerte.cs
 M Allgemein/Simulation/SimulationPufferspeicher.cs
 M Allgemein/Simulation/WaermequelleClass.cs
 M Allgemein/Simulation/WaermesenkeClass.cs
 M Allgemein/Simulation/D5a_KombiKaskade_Protokoll.md      (Verweis in 9.16)
 M Allgemein/Simulation/Lokalisierung_Katalog.md           (Nachtrag D5b)
 M MyResource/Resource.Designer.cs · Resource.resx · Resource.en-US.resx
 M Views/Pufferspeicher/Form_PufferSp_Projekt.cs
 M Views/Simulation/Form_QuellePufferspeicher.cs
 M Views/Simulation/Form_Simulation_Config.Karten.cs
 M Views/Simulation/Form_Simulation_Config.Uebersicht.cs
?? Allgemein/Simulation/D5b_DialogFreischaltung_Protokoll.md
```

`Views/Wizard/Wizard_WPItem*` und `Views/Start/Form_Start*` sind unangetastet;
`bin\` wurde nicht beschrieben (alle Bauten mit eigenem `OutDir`).

---

## 8. Restpunkte für D4 (und danach)

> **Nachtrag 16.08.2026:** Die Punkte 1–3 sind mit Etappe D4 erledigt, Punkt 4 teilweise
> (Ergebnisansicht ja, Bericht offen). Umsetzung, erklärte CSV-Erweiterung und
> Verifikation in [`D4_SchemaAnsicht_Protokoll.md`](D4_SchemaAnsicht_Protokoll.md).
> Die in Punkt 2 genannte Ableitung steht seitdem als eigene Klasse `Hydraulikbild`;
> `RingMeldung` rechnet sie nicht mehr selbst, sondern ruft sie auf.

1. **Ansicht „Schema"** (D4) — Hydraulikschema als GDI+-Panel, Umschalter,
   Auswahl-Synchronisation. Unverändert offen.
2. **Kaskadenband** — die automatisch abgeleitete Kaskadenkette als Pillen-Band unter dem
   Schema (Konzept 3). Die Daten liegen bereit: `Kaskadenkontext.QuellpufferJeAnlage`,
   `Ladeauftrag.Ebene`, und seit D5b auch die dialogseitige Ableitung in
   `WaermesenkeClass.RingMeldung` (Lader je Puffer, Quelle je Anlage) — sie lässt sich für
   die Kettenbildung wiederverwenden, statt sie ein drittes Mal zu schreiben.
3. **Ergebnisspalte für die Quellwärme des Kessels** — `SimulationSPK.Quellwaerme_gesamt`
   und `Quellwaerme_stuendlich` stehen im Rechenkern bereit, `Tab_ErgebnisHeizkessel` hat
   keine Spalte dafür. Damit ist die Kaskade in der Ergebnisansicht bisher nur indirekt
   (über den gesunkenen Gasverbrauch) sichtbar. Gleiche Familie wie die vorgemerkte Spalte
   `Speicherladung` (5-1d).
4. **Vollzyklen des Kombispeichers** — `KennzahlenBerechnen` bezieht sie auf
   `Ladung_gesamt`; bei einem Speicher, der beide Kanäle bedient, wird die Kennzahl groß
   (Szenario aus Abschnitt 4: 6627). Bekannter Durchsatz-Effekt an einem kleinen Speicher
   (Befund N6), kein neuer Befund — aber für die Berichtsanzeige zu entschärfen.

**Weiter offen aus den D5a-Listen, bewusst nicht angefasst:**

* `SimulationBHKW.ZweitsenkenRaum` behandelt den Kombispeicher als „eigenen Kanal" und
  lässt den Durchsatzterm weg (Review 1, K3-4) — als Modellentscheidung dokumentiert.
* Freistehender Quellpuffer, `Anteil_Umbuchen` ohne bekannte Herkunft, kesselseitige
  Quelltemperatur als Puffer-Vorlauf (Review 1, K3-5/6/7).
* Stufe 3 der Quellspeicher-Auflösung (`WQ_Puffer` gegen den **Katalog** `_STAMM`,
  `WaermequelleClass.QuellspeicherZeile`) baut einen Speicher auf, der zu keinem Projekt
  gehört. Sie ist mit D5b **nicht** entfernt worden: Das wäre eine Ergebnisänderung für
  Altbestände außerhalb der Referenzmenge und gehört in eine eigene Etappe mit eigenem
  Nachweis. Der Dialog macht den Fall jetzt wenigstens sichtbar (Abschnitt 1c).
* **Release-Notiz aus D5a 9.15 gilt unverändert:** Ein Projekt mit Kombispeicher darf nicht
  mit einer Fassung vor D5a geöffnet werden. D5b legt ebenfalls keine neue Spalte an
  (Schemastand bleibt 9) — der vorgemerkte Schemastand 10 als Riegel ist weiterhin offen.

---

## 9. Reproduktion

```powershell
$msb = "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe"

# 1. Anwendung und Referenzlauf bauen (Ausgabe NIE nach bin\)
& $msb C:\Waermeplan\WP_Plan\WP-Plan.sln -p:Configuration=Debug -p:Platform=x86 -p:OutDir=<scratch>\app\
& $msb C:\Waermeplan\WP_Plan\Referenzlauf\Referenzlauf.csproj -t:Restore,Build `
       -p:Configuration=Debug -p:Platform=x86 -p:OutDir=<scratch>\ref\

# 2. HEAD-Vergleichsstand aus einem eigenen Arbeitsbaum (danach wieder entfernen)
git worktree add <scratch>\wt_head HEAD --detach
& $msb <scratch>\wt_head\WindowsFormsApplication1\WindowsFormsApplication1.csproj -t:Restore,Build `
       -p:Configuration=Debug -p:Platform=x86 -p:OutDir=<scratch>\head\
#    ref-Ordner kopieren, WindowsFormsApplication1.dll durch die HEAD-Fassung ersetzen

# 3. Migrierte Wegwerf-Kopie (Produktiv-DB nur LESEN, vorher Kenndaten.laccdb pruefen)
<scratch>\ref\Referenzlauf.exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb <scratch>\DB

# 4. Regression Flag AUS gegen die eingefrorene Basis, danach Flag AN gegen HEAD
foreach ($id in 1007,1008,1011,1017,1018,1021,1023,1024) {
    <scratch>\ref\Referenzlauf.exe projekt $id <scratch>\Lauf\Projekt_$id <scratch>\DB
}
<scratch>\ref\Referenzlauf.exe vergleich `
    C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-08-15_B3 <scratch>\Lauf
#    zusaetzlich rekursiver MD5-Vergleich beider Ordner

# 5. Szenarien: je eine Kopie von <scratch>\DB, praepariert per 32-bit-PowerShell + ACE
#    (Flag, Verwendung, Schwellen, WS_Ziel/WS_ID_Puffer/WS_Ladegrenze, WQ_Typ/WQ_ID_Puffer,
#     Tab_Heizkessel.Vorlauf/Ruecklauf)

# 6. Vergleichsbau zu E-K2-3: EINE Zeile in SimulationWaermepumpe.Kanalbedarf
#    auskommentiert, eigener OutDir, danach zurueckgedreht (git diff leer).

# 7. Prueflauf: eigenes Konsolenprojekt, Assembly.LoadFrom auf die App-DLL,
#    Properties.Settings.Default.DBPath auf die Wegwerf-Kopie; Modus
#    "--kaskade <idPuffer>" richtet die Kessel-Kaskade ueber den Dialogweg ein.
```

Arbeitsbaum, Datenbankkopien, Vergleichsbauten und Prüfprogramm sind Wegwerf-Material und
nach der Abnahme gelöscht; die Zahlen dieses Protokolls sind der Beleg.
