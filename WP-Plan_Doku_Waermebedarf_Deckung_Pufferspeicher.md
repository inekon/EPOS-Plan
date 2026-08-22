# EPOS-Plan (WP-Plan) — Wärmebedarfsberechnung, Bedarfsdeckung, Pufferspeicher und Zweikanal-Logik

**Technische Dokumentation aus Code-Analyse**

| | |
|---|---|
| Stand der Dokumentation | 22.08.2026 |
| Analysierter Codestand | `C:\Users\DirkEngelmann\DB_Migration\WindowsFormsApplication1` (aktueller Entwicklungsstand, Dateidaten bis Anfang Juni 2026) |
| Vergleichsstand (alt) | `C:\Users\DirkEngelmann\source\repos\WP-PLAN` (Nov 2025) |
| Weitere Quellen | Projekt-Doku `EPOS-Plan_Webdoku_Dokumentation.html` (13.08.2026), `WP-Plan_Brauchwassertypen_VDI6002.md`, `DB_Migration\README.md` |
| Klärung mit Auftraggeber | „Zweikanal-Logik" = getrennte Führung von **Heizung und Warmwasser**; DB_Migration ist der maßgebliche Codestand |

**Kurzfassung der drei wichtigsten Befunde**

1. Die **Wärmebedarfsberechnung** ist zweistufig: Tagesheizlast über den nativen Legacy-Rechenkern `bhkwplan.dll` (per COM-Server angebunden), danach Verteilung auf Stunden über Tagesgangkurven. **Warmwasser wird im aktuellen Stand als eigener Stundenvektor** aus Brauchwassertypen (12 Monatswerte × 168-h-Wochenprofil) berechnet — die alte Pauschalvermischung mit der Heizlast ist behoben. Vor der Erzeugersimulation werden aber alle Anteile zu **einem** Summenvektor addiert; die Kanaltrennung geht dort verloren.
2. Die **Bedarfsdeckung** ist eine rein serielle, konfigurierbare Erzeugerkaskade über einen Restwärmevektor (8.760 h). **Eine Pufferspeicher-Bewirtschaftung (Beladen/Entladen) existiert im Code nicht.** Es gibt vollständige Stammdaten, Import und drei parallele Zuordnungswege — aber null Rechenanbindung. Die einzige „Berechnung" ist die Anzeige `Volumen × 1,16` (ohne Temperaturspreizung). Die Webdoku beschreibt an dieser Stelle ein Soll, kein Ist.
3. Die **Zweikanal-Logik (Heizung/Warmwasser)** ist im Code derzeit nur bedarfsseitig vorhanden (getrennte Erfassung, gemeinsame Deckung). Empfehlung: **eine** Deckungs- und Speicher-Engine mit dem Temperaturniveau als Eigenschaft — die getrennte Bedarfserfassung beibehalten, aber keinen zweiten Rechenpfad je Kanal aufbauen (Begründung und Umsetzungsvorschlag in Kap. 6).

---

## 1 Architekturüberblick

EPOS-Plan ist eine C#-WinForms-Anwendung (MDI) mit einer Access-Datenbank `Kenndaten.accdb` als Stammdaten- und Projektspeicher. Der Zugriff läuft historisch über ODBC (`RecordSet`, `Program.DBConnection`, DSN „TEST"), in neueren Teilen über OLE DB mit Parametern (`DataRepository`, `DbClass`).

Die Simulation ist in Klassen unter `Allgemein\Simulation\` organisiert:

| Klasse | Aufgabe | Zeitraster |
|---|---|---|
| `SimulationWaermebedarf` | Wärmebedarf: Gebäude, Brauchwasser, Prozesswärme, externer Lastgang, Netzverluste | 8.760 h |
| `SimulationControl` | Orchestrierung der Erzeugerkaskade und der Strombilanz | 8.760 h / 35.040 ¼ h |
| `SimulationWaermepumpe` | Wärmepumpen (bis 10 Module) mit Kennfeld, Betriebsarten, Heizstab | 8.760 h |
| `SimulationSPK` | Spitzen-/Heizkessel-Kaskade (bis 6 Kessel), Brennstoffe, Wirkungsgrade | 8.760 h |
| `SimulationSolarthermie` | Kollektorfelder nach EN-12975-Wirkungsgradmodell | 8.760 h |
| `SimulationStrombedarf` | Strombedarf aus Verbraucherprofilen und Lastgängen | 35.040 ¼ h |
| `SimulationPV` | PV-Erzeugung inkl. **Batteriespeicher-Bilanz** | intern 8.760 h, außen ¼ h |
| `SimulationSSP` | Stromspeicher — **leerer Stub** (siehe 7) | ¼ h |

Zwei UI-Einstiege benutzen dieselbe Engine mit jeweils eigenen Objektinstanzen: `Form_Simulation_Detail` (Registerkarten je Erzeuger, Navigator-Dashboards) und `Form_Simulation_Kurz` (Kompaktansicht).

### 1.1 Der native Rechenkern `bhkwplan.dll`

Die Kernmathematik des Wärmebedarfs steckt nicht im C#-Code, sondern in der nativen DLL `bhkwplan.dll` — dem Erbe des Vorgängerprodukts BHKW-Plan. Angebunden ist sie über einen **Out-of-Process-COM-Server** (`CSExeCOMServer.exe`, Klasse `SimpleObject`), der 14 Funktionen per `[DllImport]` durchreicht:

`TaeglHeizlastWG`, `SolareGewinneC`, `SpezWaermeverlusteC`, `StdWerte`, `strom_wochetojahr`, `monats_summe`, `netzverlustec`, `heapsort`, `normieren`, `vector_summe`, `vectoren_addieren`, `vector_init`, `Watt_To_kW`, `monats_grenzen`.

Konsequenzen: Die Feldgrößen (8760/365/168/192/12) sind in der DLL fest verdrahtet, die eigentliche Heizlast-Bilanz (Speicherwirkung der Bauweise, Absenklogik, Begrenzung auf Maximalraumtemperatur) ist aus C# heraus **nicht einsehbar, nicht testbar und nicht debugbar**. `SolareGewinneC` und `SpezWaermeverlusteC` geben Festkomma-`int` (×100) zurück, `TaeglHeizlastWG` ganzzahlige Wh/Tag.

---

## 2 Wärmebedarfsberechnung

Einstieg: `SimulationWaermebedarf.Waermebedarf_berechnen(ID_Projekt, ID_Klimaregion)`. Ablauf in dieser Reihenfolge:

```
Vektoren nullen
→ Klimadaten (365 Tage) + Stundentemperatur (8760 h) aus DB
→ je Gebäude: Tagesheizlast (365 Werte) → Stundenverteilung → aufaddieren
→ Watt → kW
→ externe Lastgänge addieren
→ Prozesswärme berechnen + addieren
→ Brauchwasser berechnen + addieren        ← eigener Kanal bis hierher
→ Netzverluste gleichmäßig aufschlagen
→ Summen, Maximum, Dauerlinie
```

### 2.1 Gebäudeheizwärme

**Datenbasis** je Projektgebäude (`Abfrage_Projektgebaeude`, 57 Spalten → `ProjektGebaeudeModel`): Hüllflächen mit U-Werten (Außenwand, Fenster, Dach, Grundfläche, Sonstiges), drei Wärmebrücken-Paare (ψ/Länge), Fensterflächen Nord/Ost/Süd mit Durchlassgrad, Raumsolltemperaturen (Tag, Nachtabsenkung, Wochenende, Ferien), Maximalraumtemperatur, Bauweise (Speicherfähigkeit), Luftwechselrate, Raumhöhe, vier Ferienzeiträume. Klimadaten je Region: `Tab_Klimadaten` (365 Tage: Solarstrahlung N/O/S/W, Tagesmitteltemperatur, Wochenend-Kennung, Tagtyp Wohnen/Nichtwohnen) und `Tab_Solar` (8.760 Stundentemperaturen).

**Tagesheizlast** — für jeden der 365 Tage drei DLL-Aufrufe:

```csharp
Solare_Gewinne[Tag]     = com.I_SolareGewinneC(Sol_N, A_N, Sol_W, Sol_O, A_O, Sol_S, A_S, g) / 100;
SpezWaermeverluste[Tag] = com.I_SpezWaermeverlusteC(U·A aller Bauteile, ψ·L, T_außen,
                                                    Wohnfläche, Raumhöhe, Luftwechsel) / 100;
Heizlast[Tag]           = com.I_TaeglHeizlastWG(Tag, WE-Absenkung, T_WE, Ferien-Absenkung, T_Ferien,
                                                T_Tag, T_Nacht, innere Gewinne, solare Gewinne,
                                                spez. Verluste, Bauweise, T_außen, T_max,
                                                Projektfläche, Referenzfläche);
```

Die Skalierung vom Referenzgebäude auf das Projekt erfolgt **linear** über das Flächenverhältnis (letzte beiden Parameter). Wochenend- und Ferienabsenkung werden als Flags übergeben; Ferienzeitraum 1 läuft über den Jahreswechsel.

**Einschwingen:** Vor der Hauptschleife (Tag 0–364) läuft dieselbe Rechnung für die Tage **350–364**. Die Ergebnisse werden verworfen — der Zweck ist, den internen Zustand der nativen `TaeglHeizlastWG` (thermische Trägheit des Gebäudes) über den Jahreswechsel „warmzufahren", damit der 1. Januar nicht mit kaltem Modell startet. Es gibt keine Konvergenzprüfung; für schwere Bauweisen sind 15 Tage knapp.

**Verbrauchsdaten-Rückrechnung:** Ist statt der Wohnfläche ein Verbrauch angegeben, wird zuerst in Nutzwärme umgerechnet (Öl: ×η×10,08 kWh/l; Gas: ×η×11,48 kWh/m³; Gas Ho: ×η/1,1; Brennstoff MWh: ×η; Nutzwärme MWh: direkt). Danach ein Referenzlauf mit der Katalogfläche; aus dem Verhältnis Soll-/Ist-Jahreswärme wird eine **fiktive Fläche** bestimmt, mit der das Gebäudemodell exakt den Verbrauch reproduziert (wegen der linearen Skalierung genügt eine Iteration).

**Verteilung Tag → Stunde:** `Abfrage_Tagverteilung` liefert je Gebäudetyp 192 Werte = **8 Tagtypen × 24 h**. Der native `StdWerte` wählt je Tag über `TagTyp_W` (Wohngebäude, Auswahl per String-Vergleich `"Wohngebaeude  VDI 2067"` — mit zwei Leerzeichen!) bzw. `TagTyp_NW` den passenden 24-h-Block und verteilt die Tagesheizlast. Ergebnis je Gebäude wird auf den Gesamtvektor addiert, dann `Watt_To_kW`.

### 2.2 Warmwasser (Brauchwasser)

**Der aktuelle Stand führt Warmwasser als eigenen Kanal** — das ist der wichtigste Umbau gegenüber dem alten Code:

| | Alt (Nov 2025) | Aktuell (Juni 2026) |
|---|---|---|
| Modell | `WW_Bedarf × Bewohner × 1000 / 365`, jeden Tag gleich | Brauchwassertyp-Katalog: 12 Monatswerte × 168-h-Wochenprofil |
| Zeitprofil | bekam das **Heizungs**-Tagesprofil aufgeprägt | eigenes Zapf-Wochenprofil (`Tab_Brauchwassertyp`) |
| Einbindung | in `Heizlast[Tag]` **vor** der Stundenverteilung vermischt | eigener Vektor `brauchwasserwerte[8760]`, Addition erst am Ende |
| Bezug | je Gebäude über Bewohnerzahl | je Projekt (`Z_Projekt_Brauchwasser`), Jahressumme skalierbar |

Rechengang in `Brauchwasserwaerme_berechnen()`:

```csharp
// je zugeordnetem Brauchwasserprofil des Projekts:
monats_waerme[0..11]  ← Tab_Brauchwasser.Monat_1..12
monats_waerme[i]      ×= Z_Projekt_Brauchwasser.Summe / Σ(Monatswerte)   // Skalierung auf Objektgröße
wochen_waerme[0..167] ← Tab_Brauchwassertyp."1".."168"
temp = com.I_strom_wochetojahr(wochen_waerme, monats_waerme, mo_anfang, mo_ende)
brauchwasserwerte += temp
```

Der native `strom_wochetojahr` kachelt das Wochenprofil übers Jahr und normiert monatsweise auf die Monatsenergien — die **Monatsreihe bestimmt die Energiemenge**, das Wochenprofil nur die Form. Ergebnisgrößen: `Waermebedarf_Brauchwasser` (Jahressumme) und `Waermebedarf_Brauchwasser_Monat[12]` — beide bleiben als getrennte Ausweisgrößen erhalten, der Stundenvektor wird in Zeile 217 auf den Gesamtwärmebedarf addiert.

Die **VDI-6002-Brauchwassertypen** (Wohnen, Studentenwohnheim, Seniorenheim, Krankenhaus, Hotel, Pflegeheim, Büro, Sportstätte, Schule …) sind laut Projekt-Doku vom 02.08.2026 als Katalogdaten eingespielt; die Rechenlogik im Code ist dafür bereits vollständig. Zwei Punkte sind dabei zu prüfen (siehe auch Kap. 7): die **Einheit der Monatswerte** (der Rechenpfad erwartet kWh, die VDI-Tabelle im Projektdokument weist MWh aus — Faktor-1000-Risiko) und die noch fehlende Stamm-/Projekt-Trennung (`ReadOnly`-Kataloge).

Das Gebäudefeld `WW_Bedarf` und das Flag `DezentralWarmwasser` werden noch aus der DB gelesen, aber **nirgends mehr ausgewertet** (Altlast).

### 2.3 Prozesswärme, externer Lastgang, Netzverluste

**Prozesswärme** funktioniert strukturell identisch zum Brauchwasser (Tabellen `Tab_Prozesswaerme` / `Tab_Prozesstyp` / `Z_Projekt_Prozesswaerme`, Vektor `prozesswerte[8760]`, seit Juni ebenfalls mit Jahressummen-Skalierung).

**Externer Lastgang:** je zugeordneter Ganglinie (`Z_ProjektWaermebedarf` → `Tab_Waermebedarf` → `Tab_WaermebedarfDaten`, 8.760 Werte) wird der Vektor addiert. Achtung: Die Addition erfolgt **nach** der W→kW-Umrechnung des Gebäudeanteils — externe Werte müssen also bereits in kW vorliegen; das wird nirgends geprüft. Beim Einlesen mehrerer Ganglinien wird der Arbeitsvektor zwischen den Ganglinien nicht genullt (Restwerte-Risiko bei unvollständigen Reihen).

**Netzverluste** werden als Konstante auf jede Stunde aufgeschlagen: bei Einheit „%" `(Gesamt × 1000 × p) / 876000` pro Stunde, sonst `Absolutwert / 8760`. Kein Temperatur- oder Lastbezug. Die %-Formel enthält einen **Einheitenfehler** (unterstellt MWh, der Vektor läuft aber in kW/kWh → aufgeschlagener Verlust um Faktor 1000 zu groß, während der *ausgewiesene* Wert korrekt ist) — Details in Kap. 7.

### 2.4 Ergebnisgrößen

Gebildet werden: Jahressummen je Anteil (Gebäude, Extern, Prozess, Brauchwasser, Netzverluste), Monatssummen je Anteil, `Waermebedarf_Max` (Jahreshöchstlast), die auf das Maximum **normierte** Jahresdauerlinie (absteigend sortiert per Heapsort) und die chronologische Ganglinie. Diese Werte leben nur im Objekt und werden von den Views angezeigt; die frühere Persistenz nach `Tab_Simulation_Ergebnis` ist im aktuellen Stand für die Wärmeseite **entfallen** (nur der Strompfad schreibt noch — ohne WHERE-Klausel, siehe Kap. 7).

---

## 3 Wärmebedarfsdeckung durch die Erzeuger

### 3.1 Orchestrierung (`SimulationControl.Do_Simulation`)

Die Konfiguration kommt aus `Tab_Einstellungen` je Projekt: sechs Slots `Tool_1 … Tool_6`, gesetzt in `Form_Simulation_Config` über sechs ComboBoxen. **Die Slot-Position ist die Priorität** — eine eigene Prioritätsspalte gibt es nicht.

```
Eingang = Wärmebedarf (8760 h, kW)
für Slot 1..4:                              // Wärmeerzeuger
    "Wärmepumpe"   → Ausgang = WP-Simulation(Eingang)
    "Heizkessel"   → Ausgang = SPK-Simulation(Eingang)
    "Solarthermie" → Ausgang = Solar-Simulation(Eingang)
    Eingang = Ausgang                       // Restwärme wandert weiter
Slot 5: "Photovoltaik"  → Strombilanz (¼-h-Raster)
Slot 6: "Stromspeicher" → (Stub, defekt)
```

Jeder Erzeuger erhält den **Restwärmevektor** seines Vorgängers und gibt seinen eigenen Rest zurück — eine reine, speicherlose Lastfolge-Kaskade. Parallel wird die Strombilanz geführt: WP-Strom und Heizstab (aus Stundenwerten auf ¼ h expandiert) sowie Kesselstrom werden dem Strombedarf **zugeschlagen**, PV wird abgezogen. „BHKW" ist in der Konfiguration wählbar, hat aber **keinen Zweig** in der Kaskade — die Auswahl wird stillschweigend übersprungen (es existieren nur BHKW-Stammdaten, keine Simulation).

### 3.2 Wärmepumpe (`SimulationWaermepumpe`)

Je Projekt bis zu 10 Module aus `Tab_Energieanlagen` (Bezeichner, Betriebsart, Vorlauf, Volumen) plus `Tab_WP` (Nennleistung, Heizstableistung „Heizung"). Das **Kennfeld** kommt aus `Tab_Kenndaten` je `ID_WP` und `Vorlauf` (die Vorlauftemperatur ist reiner Kennlinien-Selektor; `Ruecklauf` wird gespeichert, aber nie gerechnet). Je Stunde:

1. **Kennfeld-Interpolation** über die Außentemperatur: linear zwischen den Stützstellen für COP und P_therm, `P_el = P_therm / COP`. Oberhalb der Kennlinie wird geklemmt, unterhalb nach einmaliger Nutzer-Rückfrage linear **extrapoliert** (die Rückfrage ist eine modale MessageBox mitten in der Rechenschleife).
2. **Betriebsart** (nur bei `Bivalenter_Betrieb`): *Teilparallel* — unter dem Abschaltpunkt ist die WP aus; *Alternativ* — sobald P_therm < Restbedarf, ist die WP komplett aus; *Parallel* — keine Einschränkung (leerer Zweig).
3. **Sperrzeiten:** `stunde % 24` im Fenster `Sperrzeit_von/bis` → WP aus. Ohne Puffer fällt der Bedarf ersatzlos an den nächsten Erzeuger.
4. **Deckung:** Reicht die Leistung nicht, läuft das Modul mit Volllast (`Laufzeit += 1`); sonst Teillast mit exakt dem Restbedarf (`Strom += Rest/COP`, `Laufzeit += Rest/P_therm`). Ein **Teillast-COP existiert nicht** (Volllast-COP für alle Betriebspunkte), Takten wird nicht abgebildet.
5. **Heizstab:** global aktivierbar (`WP_Heizstab` in `Tab_Einstellungen`), Leistung je Modul aus `Tab_WP.Heizung`; deckt Restbedarf nach allen WP-Modulen. (Bug: Stundenvektor wird je Modul überschrieben statt aufsummiert, Summen stimmen.)
6. **Bivalenzpunkt** = höchste Außentemperatur, bei der nach allen WP-Modulen (vor Heizstab) Restwärme bleibt.

Ergebnisse je Modul und gesamt: Wärmeproduktion, Strombedarf, Heizstabarbeit, Laufzeit/Vollbenutzungsstunden, Restbedarfsvektor.

### 3.3 Spitzenkessel (`SimulationSPK`)

Bis zu 6 Kessel aus dem Brennstoffkatalog (`[DB-Heizung]` bzw. neue `Tab_Brennstoff_Stamm`-Struktur: P_therm, η_Gas, η_Öl, Brennstoffcode, Betriebsbereitschaftsverlust). Je Stunde deckt Kessel 1 bis zu seiner Nennleistung, der Rest geht an Kessel 2 usw. — Reihenfolge = Listenposition, kein Wirkungsgrad-Ranking, keine Mindestlast.

Nach der Jahresschleife wird je Kessel der **Jahresnutzungsgrad** aus Betriebsstunden und Bereitschaftsverlust korrigiert (Brennwertkessel pauschal η−0,02; alle Kessel außer dem letzten gelten als ganzjährig betriebsbereit 8.760 h, der letzte mit konfigurierbarer Bereitschaft, Standard 6.000 h). Brennstoffverbrauch wird nach Energieträger getrennt summiert; ein Elektrokessel (Brennstoff „Strom") fließt in die Strombilanz. Die Emissionsberechnung ist im aktuellen Stand **funktionslos** (liefert konstant 0, siehe Kap. 7); die Emissionsfaktoren der Stammdaten werden gar nicht erst gelesen.

### 3.4 Solarthermie und Photovoltaik

**Solarthermie** ist ein regulärer Kaskaden-Slot (kein automatischer Vorrang). Je Kollektorfeld: Einstrahlung auf die geneigte Ebene (eigener `SolarCalculator`), dann EN-12975-Modell `η = η₀·IAM − a₁·ΔT/G − a₂·ΔT²/G` mit **fest angenommener Speichertemperatur 50 °C** und pauschalen Leitungsverlusten (Faktor 0,92). Deckung = `min(Erzeugung, Restbedarf)`; der Überschuss wird summiert und angezeigt, aber **verworfen** — genau hier fehlt der Pufferspeicher am sichtbarsten.

**Photovoltaik** rechnet intern stündlich (Bedarf wird aus ¼ h gemittelt, Ergebnis wieder expandiert) und enthält die einzige **funktionierende Speicherbilanz** des Produkts: eine Batterie-SOC-Bilanz (`laden = min(Überschuss, Kapazität − SOC)`, `entladen = min(Restbedarf, SOC)`, Wechselrichter pauschal 0,95, Ladeleistung = 1 C). Diese Logik ist die natürliche Vorlage für die fehlende thermische Speicherbilanz.

---

## 4 Pufferspeicher — Soll laut Webdoku, Ist im Code

### 4.1 Soll (Webdoku, Stand 13.08.2026)

Die Webdoku beschreibt: Kapazität aus Volumen und nutzbarer Spreizung (1.000 l × 20 K ≈ 23 kWh), stündliche Bilanzierung des Speicherinhalts (nimmt Überschüsse auf, deckt Bedarf, verliert Bereitschaftswärme), **Bewirtschaftung per Hysterese** mit einstellbarer Ein-/Abschaltschwelle in Prozent der Kapazität, „solange der Speicher den Bedarf deckt, bleibt der Erzeuger aus", Zuordnung von Speichern zu Erzeugern im Simulations-Konfigurationsdialog mit Vor-/Rücklauftemperatur.

### 4.2 Ist im Code: Verwaltung ja, Simulation nein

**Es existiert keine stündliche Speicherbilanz für thermische Speicher** — kein Beladen, kein Entladen, kein Füllstand, keine Verluste, keine Hysterese. Im Einzelnen:

**Vorhandene Stammdaten** (`Tab_Pufferspeicher` → `PufferSpModel`): Bezeichner, Hersteller, Speichertyp (Solarspeicher / Pufferspeicher / Kombispeicher), Bereitschaftsverluste, Gesamtvolumen [l], Investitionskosten. Dazu ein VDI-3805-Import (`PufferSpImport`: Volumen, Verluste, Typ aus Satzart 710.03) und vollständige Pflege-Formulare (`Form_PufferSp_Admin`, `_Bearbeiten`, `_einlesen`). Kein Feld für Schichtung, Temperaturgrenzen, Lade-/Entladeleistung oder Wärmetauscher. **Kein einziges Feld wird in einer Berechnung gelesen** — auch der Bereitschaftsverlust nicht.

**Drei parallele, unverbundene Zuordnungswege** Speicher ↔ Erzeuger:

1. `Form_Simulation_Config` + `Form_KonfigPufferspeicher` → Tabelle `Z_ProjektPufferSp` (Erzeuger als String „BHKW"/„Heizkessel"/„Solarthermie"/„Wärmepumpe"/„Gesamtsystem", Pufferbezeichner als String-Referenz, Vorlauf, Rücklauf, Priorität aus der Zeilenreihenfolge). **Wird von keiner Simulationsklasse gelesen.**
2. Wizard-/Komponentenpfad → `Tab_Energieanlagen` mit `ID_Type = PUFFER_TYP (12)` und `ID_PUFFER`. **Wird nie ausgewertet.**
3. Feld `Volumen` am WP-Datensatz in `Tab_Energieanlagen` → `SimulationWaermepumpe`: `if (model.Volumen > 0) Volumen_Pufferspeicher = model.Volumen;` — bei mehreren WP gewinnt die letzte.

**Die einzige Rechenoperation mit einem Puffervolumen im gesamten Produkt** (`Form_Simulation_Detail`, Z. 454):

```csharp
textBox_Pufferspeicher.Text = (sim.simulation_wp.Volumen_Pufferspeicher * 1.16).ToString();
```

Das ist Volumen [l] × 1,16 Wh/(l·K) = Kapazität **pro Kelvin**; die Spreizung (ΔT aus den erfassten Vor-/Rücklauftemperaturen) fehlt, der Wert ist dimensional unvollständig. Für den Webdoku-Beispielwert (1.000 l, 20 K → 23,2 kWh) müsste `× 1,16 × ΔT / 1000` gerechnet werden.

**Toter Code als Fossil einer früheren Bewirtschaftung** (`SimulationWaermepumpe`, Z. 150–160, unverändert seit mindestens Nov 2025):

```csharp
double Rest_Speicher, KapazitaetPendelspeicher, Solar_Speicher, Speicher;
Rest_Speicher = 0; KapazitaetPendelspeicher = 0; Solar_Speicher = 0; Speicher = 0;
for (int stunde = 0; stunde < 8760; stunde++)
{
    Rest_waerme  = Waermebedarf_stuendlich[stunde];
    Rest_Speicher = KapazitaetPendelspeicher - Solar_Speicher - Speicher;   // nie gelesen, alles 0
    ...
```

Der Begriff „Pendelspeicher" und die Struktur „Kapazität − Solaranteil − belegter Anteil" stammen erkennbar aus dem BHKW-Plan-Vorgänger.

**Vorbereitete, aber ungenutzte Konfigurationsfelder** in `Tab_Einstellungen` (gelesen und geschrieben von `KonfigurationCtrl`, von keiner UI gesetzt, von keiner Simulation gelesen): `Ladefuellstand_Min`, `Ladefuellstand_Max`, `Ladeleistung_Max` (+ drei `*_Auswahl`-Strings), `Ladeschwellwert`. Das sind exakt die Felder, die die Webdoku-Hysterese (Ein-/Abschaltschwelle) braucht — die Infrastruktur wartet auf die Logik.

### 4.3 Fachliche Folgen des fehlenden Speichermodells

Ohne Speicherbilanz verfällt der Solarthermie-Überschuss vollständig (eine Solaranlage kann nur decken, was zeitgleich gebraucht wird), wirken WP-Sperrzeiten ungepuffert (der Bedarf fällt sofort an Kessel/Heizstab, statt aus einem vorgeladenen Puffer zu kommen), gibt es kein Takt-/Mindestlaufzeitverhalten, fehlen die Speicher-Bereitschaftsverluste in der Jahresbilanz, und Bivalenzpunkt sowie „minimal erforderliche Spitzenkesselleistung" fallen systematisch zu ungünstig aus, weil kein Puffer Lastspitzen kappt. Der in der Webdoku beschriebene Sommer-Effekt („Erzeuger bleibt aus, solange der Speicher deckt") ist derzeit nicht erzielbar.

---

## 5 Vorschlag: Soll-Logik der Pufferspeicher-Bewirtschaftung

Die funktionierende Batterie-Bilanz in `SimulationPV` liefert das Muster; für die Wärmeseite kommt die Hysterese und die Erzeuger-Ankopplung hinzu. Vorschlag für **eine** zentrale Speicherstufe in der Kaskade (statt Speicherlogik in jedem Erzeuger):

**Parameter je Speicher** (alle Felder existieren bereits in DB/Modellen): Kapazität `Q_max = V [l] × 1,16 Wh/(l·K) × (T_VL − T_RL) / 1000` [kWh], Bereitschaftsverlust je Stunde (aus `Bereitschaftsverluste`, auf h umgerechnet), Einschaltschwelle `S_ein` und Abschaltschwelle `S_aus` in % von `Q_max` (Felder `Ladefuellstand_Min/Max` bzw. `Ladeschwellwert`), optional maximale Lade-/Entladeleistung (`Ladeleistung_Max`, sonst unbegrenzt).

**Stundenschleife** (integriert in `SimulationControl`, zwischen Bedarf und Erzeugerkaskade):

```
für jede Stunde h:
    Q_speicher -= Verlust_h                            // Bereitschaftsverlust, ≥ 0 klemmen
    Bedarf_h    = Waermebedarf[h]

    // 1) Entladen: Speicher deckt zuerst
    entnahme    = min(Bedarf_h, Q_speicher, P_entlade_max)
    Bedarf_h   -= entnahme;  Q_speicher -= entnahme

    // 2) Hysterese: läuft der geführte Erzeuger?
    wenn Q_speicher ≤ S_ein × Q_max:  erzeuger_an = wahr
    wenn Q_speicher ≥ S_aus × Q_max:  erzeuger_an = falsch

    // 3) Erzeugerkaskade auf den Restbedarf
    erzeugung   = Kaskade(Bedarf_h)                    // wie bisher

    // 4) Beladen: Überschuss und gezielte Nachladung
    wenn erzeuger_an:
        nachladung = min(P_erzeuger_frei, (S_aus×Q_max − Q_speicher), P_lade_max)
        Q_speicher += nachladung                       // zählt als Erzeugung (Brennstoff/Strom!)
    Q_speicher += min(Solar_Ueberschuss_h, Q_max − Q_speicher)   // Solarüberschuss einlagern
```

Damit ergeben sich genau die in der Webdoku beschriebenen Effekte: Sommerbetrieb der WP in Blöcken (Speicher deckt, Erzeuger pausiert), nutzbarer Solarüberschuss, gepufferte Sperrzeiten (Vorladung vor dem Sperrfenster als zweiter Ausbauschritt), und in der Jahresbilanz liegt die Wärmeproduktion um die Speicherverluste über dem Bedarf. Die Zuordnung Speicher ↔ Erzeuger sollte dabei auf **einen** Weg konsolidiert werden (`Z_ProjektPufferSp` als führende Tabelle mit Fremdschlüssel statt String-Referenz; `ID_PUFFER`/`Volumen` in `Tab_Energieanlagen` stilllegen).

---

## 6 Zweikanal-Logik Heizung / Warmwasser — Analyse und Empfehlung

Gemeint ist (Klärung vom 22.08.2026) die **getrennte Führung der beiden Bedarfskanäle Heizwärme und Warmwasser** durch Simulation und Speicher. Befund, Bewertung und Empfehlung:

### 6.1 Was der Code heute tut

Die Kanäle sind **bei der Erfassung getrennt** (Gebäudeheizwärme aus dem Tagesheizlast-Modell, Warmwasser aus Brauchwassertypen mit eigenem Zapfprofil — seit Juni 2026 sauber entmischt) und werden **vor der Deckung zu einem Vektor addiert**. Die gesamte Erzeuger- und (künftige) Speicherlogik sieht nur noch eine Summenlast. Damit gehen zwei Informationen verloren: das **Temperaturniveau** (Heizkreis z. B. 35 °C, Trinkwarmwasser ≥ 60 °C — entscheidend für WP-COP und nutzbare Speicherspreizung) und die **Zuordenbarkeit** (welcher Erzeuger deckt welchen Kanal). Die Webdoku verspricht darüber hinaus eine „Wärmesenke"-Auswahl je Wärmepumpe (gesamter Bedarf / nur Warmwasser / nur Heizwärme) — dafür gibt es im Code **keinerlei Ansatz** (0 Treffer auf „Senke").

### 6.2 Was eine volle Zweikanal-Deckung kosten würde

Konsequent zu Ende gedacht hieße Zweikanal: zwei Restwärmevektoren durch die komplette Kaskade, Kanalzuordnung je Erzeuger, zwei Speicher (Heizungspuffer + TWW-Speicher) oder ein Kombispeicher mit zwei Zonen, Vorrangladung TWW, zwei Kennfeld-Arbeitspunkte je WP (der `Vorlauf`-Selektor müsste je Kanal unterschiedlich greifen), doppelte Ergebnisdarstellung. Das verdoppelt die Zahl der Pfade durch eine Logik, die heute schon die größte Baustelle des Produkts ist (Kap. 4/7) — und es müsste gebaut werden, **bevor** überhaupt eine einfache Speicherbilanz existiert.

### 6.3 Empfehlung: Einkanal-Engine mit Temperaturniveau als Attribut — Zweikanal ablösen

Die Zweikanal-Logik **kann und sollte als Rechenarchitektur abgelöst werden**, ohne die fachlich nötigen Unterscheidungen aufzugeben:

1. **Erfassung getrennt lassen** (ist bereits so): `Waermebedarf_Gebaeude`, `brauchwasserwerte`, `prozesswerte`, Extern bleiben eigene Vektoren — sie kosten nichts und tragen die Ausweisgrößen (Monatsdiagramme, „Wärmelast nach Heizwärme und Warmwasser getrennt").
2. **Eine Deckungs- und Speicher-Engine** über den Summenvektor (wie heute), erweitert um die Speicherstufe aus Kap. 5. Kein zweiter Kaskadenpfad.
3. **Temperaturniveau als Stundenattribut statt als Kanal:** aus den vorhandenen Teilvektoren lässt sich je Stunde der WW-Anteil `f_WW[h] = brauchwasserwerte[h] / Waermebedarf[h]` bilden. Damit können die beiden relevanten Effekte punktuell abgebildet werden, ohne die Kaskade zu verdoppeln: (a) die WP rechnet mit einem **anteilsgewichteten Vorlauf** (bzw. interpoliert zwischen zwei Kennlinien 35 °C/55 °C statt nur einer), (b) der Speicher rechnet mit einer anteilsgewichteten nutzbaren Spreizung. Das ist eine kleine, lokal begrenzte Erweiterung von `berechne_wptherm` bzw. der Speicherstufe.
4. **„Wärmesenke" als Bedarfsfilter, nicht als zweiter Rechenpfad:** soll eine Maschine nur Warmwasser (oder nur Heizung) decken, bekommt sie als Eingang schlicht `min(Restbedarf, Kanalanteil)` — ein Vorfilter vor dem Erzeugeraufruf, die Engine selbst bleibt einkanaig. Damit ist das Webdoku-Versprechen erfüllbar, ohne die Kaskade zu spalten.
5. **Nicht empfohlen** ist die vollständige Streichung der Kanalinformation (reiner Summenvektor ohne `f_WW`): dann wären WP-Auslegung auf TWW-Temperatur, Sommer-Grundlast-Dimensionierung (BHKW/Solar) und die versprochene getrennte Ergebnisdarstellung nicht mehr herleitbar.

Kurz: **Zweikanal in der Deckungslogik ablösen (ja, möglich), Kanaltrennung in der Bedarfserfassung und als Anteilsattribut behalten.** Reihenfolge der Umsetzung: erst Speicherstufe (Kap. 5) auf dem Summenvektor, dann gewichteter Vorlauf/Spreizung über `f_WW`, zuletzt optional der Wärmesenke-Filter.

---

## 7 Auffälligkeiten aus der Code-Analyse (priorisiert)

Bei der Analyse sind Fehler und Altlasten aufgefallen, die die dokumentierten Rechenwege direkt betreffen. Die wichtigsten, nach Dringlichkeit:

**Rechenrelevant (verfälschen Ergebnisse):**

1. **Netzverluste um Faktor 1000 zu hoch** aufgeschlagen bei Einheit „%" (Einheitenfehler in `SimulationWaermebedarf`, Z. 225; der ausgewiesene Wert ist korrekt, der stündliche Aufschlag nicht).
2. **Ferienabsenkung wirkt nicht bzw. ganzjährig:** In der Hauptschleife fehlt die Zeile `Ferien_Absenkung = F_Absenkung[Tag] ? 1 : 0` — es gilt für alle 365 Tage der Zustand von Tag 364 (Einschwingschleife). Alt wie neu.
3. **Ganzzahldivision** bei solaren Gewinnen und spez. Wärmeverlusten (`/ 100` statt `/ 100f` an 3 von 4 Stellen) — Nachkommastellen werden abgeschnitten, im Winter relativer Fehler bis 100 %.
4. **Einheiten-Mix kWh/MWh in der Anzeige:** Deckungsgrade und Restwärme mischen `Waermebedarf_Gesamt` (kWh) mit Erzeugerwerten (MWh) (`Form_Simulation_Detail` Z. 434 ff., 673; `Form_Simulation_Kurz` Z. 101/105).
5. **Brauchwasser-Katalogeinheit klären:** Rechenpfad erwartet Monatswerte in kWh, die VDI-6002-Projektdoku weist MWh aus — Faktor-1000-Risiko bei der Erstbefüllung.
6. **SPK-Emissionen konstant 0** (`Em_x = Em_x + Verbrauch × Em_x` mit Startwert 0; Emissionsfaktoren werden nie gelesen) plus wiederholte /1000-Division in der Kesselschleife.
7. **Nur das letzte Stromprofil zählt** (`SimulationStrombedarf`: Addition auskommentiert, `temp` wird je Profil überschrieben) — Strombedarf bei mehreren Verbraucherprofilen zu klein.
8. **Stromspeicher-Slot zerstört die Strombilanz:** `SimulationSSP` ist ein leerer Stub, der den Eingangsvektor zurückgibt; `SubVectors(x, x)` nullt den Reststrom → scheinbare 100-%-Autarkie. (Die echte Batterie-Logik sitzt in `SimulationPV`.)
9. **Aliasing bei 0 Kesseln** (`SimulationSPK`: `Restwaerme = Waermebedarf` als Referenz) — der nächste `Init()` kann den Projekt-Wärmebedarf nullen.
10. **Öl-Wärme durch Gas-Wirkungsgrad geteilt**, Elektrokessel-Strom aus kumulierter Fremdwärme, Brennstoffcode 5 unerreichbar (`SimulationSPK`).

**Robustheit / Absturzrisiken:** Schaltjahr sprengt die festen 8760er-Felder (`Init.Monatswerte_berechnen` nutzt das Systemjahr → 8784 h, Puffergrenzen der nativen DLL); `Gasspitze_Kessel[5]` bei `MAX_SPK = 6`; WP-Guard lehnt genau 10 WP ab (`>= 10` bei `MAX_WP = 10`); externe Lastgänge ohne Grenz-/Resetprüfung; Division durch 0 bei leerer WP-Liste bzw. Monatssumme 0; modale MessageBox (Extrapolationsfrage) mitten in der Rechenschleife verhindert Automatisierung.

**Konsistenz / Altlasten:** Stundenvektoren `Heizstab_stuendlich` und `Kesselleistung_stuendlich` werden je Modul/Kessel überschrieben statt summiert (Navigator-Kurven falsch, Summen korrekt); Wärme-Ergebnispersistenz entfallen, Strom-`UPDATE Tab_Simulation_Ergebnis` ohne WHERE; dreifache Pufferzuordnung (Kap. 4.2); tote Felder `WW_Bedarf`/`DezentralWarmwasser`, `REF_*`-Typen, Pendelspeicher-Fossil, `TestePVAnlage()` im Produktivpfad, hartkodierter Webserver-Start (`dotnet serve -d C:\WPFake`) und DSN „TEST" in `Program.cs`; SQL durchgängig per String-Verkettung (Injektion/Apostroph-Bruch), nur neuere Controller parametrisiert.

---

## 8 Quellen und untersuchte Dateien

**Codestand aktuell** — `DB_Migration\WindowsFormsApplication1\`: `Allgemein\Simulation\` (SimulationWaermebedarf, SimulationControl, SimulationWaermepumpe, SimulationSPK, SimulationSolarthermie, SimulationPV, SimulationSSP, SimulationStrombedarf, Init), `Views\Simulation\` (Form_Simulation_Detail/_Kurz/_Config, Form_KonfigPufferspeicher, Navigator*), `Views\Pufferspeicher\`, `Views\Brauchwasser\`, `Controller\` (PufferSpCtrl, Z_ProjektPufferSpCtrl, BrauchwasserCtrl, Z_ProjektBrauchwasserCtrl, WErzeugerCtrl, WPCtrl, KonfigurationCtrl, BrennstoffCtrl, ProjektGebaeudeCtrl u. a.), `Model\` (PufferSpModel, Z_ProjektPufferSpModel, BrauchwasserModel, WErzeugerModel u. a.), `Allgemein\Import\VDI 3805\PufferSpImport.cs`, `Program.cs`.

**Codestand alt (Vergleich)** — `source\repos\WP-PLAN\`: `Classes\Simulation\` (SimulationWaermebedarf, SimulationWaermepumpe, SimulationSPK), `Views\Hauptformular\FormMain.cs`, `CSExeCOMServer\SimpleObject.cs` (COM-Wrapper um `bhkwplan.dll`).

**Projekt-Dokumente:** `EPOS-Plan_Webdoku_Dokumentation.html` (Soll-Beschreibung Pufferspeicher/Simulation/Wärmesenke), `WP-Plan_Brauchwassertypen_VDI6002.md` (Brauchwasser-Kataloge), `DB_Migration\README.md` und `Datenbank-Migration-Plan.md` (DB-Versionsmigration Juni 2026).

*Hinweis zur Methodik: Die Aussagen zu „nicht vorhanden" beruhen auf Volltextsuchen über beide Codestände (u. a. „Kanal", „Senke", „Belad/Entlad", „Hysterese", „Fuellstand/Ladezustand" im Wärmekontext). Die native `bhkwplan.dll` liegt nur als Binärdatei vor; ihre interne Heizlast-Logik ist aus dem Quellcode nicht ableitbar und hier nach Schnittstelle und Verhalten beschrieben.*

