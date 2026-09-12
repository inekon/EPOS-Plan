# Prüfbericht: Berichtsmodul EPOS-Plan — Code, Datenbank und Konzepte

**Stand:** 12.08.2026 · Prüfung unabhängig vom Umsetzungsstand der Phasen 1–5
**Prüfumfang:** Quellcode `C:\Waermeplan\WP_Plan\WindowsFormsApplication1` (Auszug, ~65 Dateien) gegen das Leitkonzept; Konsistenz der Konzeptdokumente; DB-Struktur `Kenndaten.accdb` (**ausstehend**, Kap. 6)
**Ablage:** `Allgemein\Reporting\` · Bezug: `Konzept_Berichtserstellung_EPOS-Plan.md` (Fassung 3.1), `Konzept_Wirtschaftlichkeit.md` (Fassung 2.1), `Allgemein\Bericht\LIESMICH_Phase1.md`

---

## 1. Prüfauftrag und Quellen

Auftrag (Session 12.08.2026): Datenstruktur der `Kenndaten.accdb` und den Code
in `WindowsFormsApplication1` gegen die Berichts-Konzepte prüfen; die Konzepte
auf Konsistenz prüfen und konsolidieren.

**Wichtige Einordnung:** Die im Chat übergebenen Konzeptdateien waren
**Vorfassungen** (Erstfassung `Konzept_Berichtserstellung_EPOSPlan.md` vom
11.08. mit offener Code-Verifikation sowie `Konzept_Variantenbericht.md`
Fassung 2 vom 10.08.). Im Repo lag bereits die **konsolidierte Fassung 3** des
Leitkonzepts, in der beide aufgegangen sind, und der Code enthielt die
Umsetzung der **Phasen 1–5** (`Allgemein\Bericht\`, `Form_Bericht`,
`VariantenCtrl`, `Form_AlsVariante`, Änderungen in `ErgebnisCtrl`). Geprüft
wurde deshalb der reale Code-Stand gegen Fassung 3 — nicht die Altfassungen.

Die DB-Prüfung konnte nicht durchgeführt werden: Der Zugriffsdialog für
`C:\ProgramData\EPOS_PLAN` blieb dreimal unbeantwortet (zuletzt war das
Desktop-Fenster nicht verfügbar). Kap. 6 enthält die vollständige Checkliste
zum Nachholen.

## 2. Gesamtbild

Der Code ist gegenüber dem Leitkonzept **strukturell treu und funktional weit
gediehen**: DTO-Sammler, Baustein-Architektur, Blocksplitting mit wiederholter
Stamm-Spalte, Δ aus Rohwerten, Inhaltsverzeichnis mit Feldaktualisierung,
Vorlagen-Fallback, Excel-Mappe (echte Werte, Autofilter, Δ%-Block,
Detailblätter), In-Memory-Simulation je Variante und der Berichtsdialog sind
wie beschrieben umgesetzt.

**Fachlich belastbar** sind die Kennzahlgruppen Energiebilanz und Effizienz.
**Nicht belastbar** sind derzeit **Emissionen und Kosten (einfach)**: Der
Brennstoff des Spitzenkessels fällt lautlos aus Kosten und CO₂ heraus (N1), und
ein Preis von 0 wird als gültig verbucht (N2) — die tragende Konzeptregel
„keine stillen Teilsummen" ist damit verletzt, der Bericht kann sich selbst
widersprechen (Brennstoffmenge ausgewiesen, zugehörige Kosten/CO₂ fehlen).
Betriebsrisiken: modale Fehlerdialoge aus dem Hintergrund-Thread (N3),
PV-Doppelzählung in Strombilanz-Diagramm und Excel-Monatswerten (N5).

**Befundstatus:** B2 und B7 (neuer Weg) sind sauber erledigt; **B1, B3 und B5
sind entgegen der Statusdoku nur zur Hälfte erledigt**; B4 und B6 unverändert
offen. Sieben Konzept↔Code-Abweichungen wurden identifiziert und in
**Fassung 3.1** des Leitkonzepts als dokumentierte Entscheidungen nachgezogen
(Kap. 4/5).

## 3. Code gegen Konzept (Fassung 3)

### 3.1 Konzepttreue — Zusammenfassung

**Wie im Konzept umgesetzt (belegt):** unbegrenzte Variantenzahl mit
Blocksplitting zu 3 und wiederholter Stamm-Spalte (`WordBerichtGenerator.cs:170,335-343`);
Δ-Spalte genau bei einer Variante und kompakte Δ%-Tabelle ab zwei
(`BausteineVergleich.cs:249,142-146`); Δ aus Rohwerten (`WordBerichtGenerator.cs:354-373`);
TOC + `UpdateFieldsOnOpen` (`:206-215,113-120`); Vorlagen-Fallback mit
programmatischen Ersatz-Styles (`:42-58,123-151`); Konfig-Persistenz je Stamm
als JSON in Tabelle `Berichtskonfiguration` (`BerichtCtrl.cs:95-160`);
Statusprüfung fehlend/veraltet mit ⚠ (`BerichtsDatenSammler.cs:66-102`);
In-Memory-Simulation je Variante mit frischer `SimulationRunner`-Instanz und
Persistierung im selben Zug (`BerichtsDatenSammler.cs:182-216`); alle vier
Ganglinientypen (`ChartRenderer.cs:143,156,175,199`); Excel mit Übersicht/
Vergleich/Detailblättern, echten Zahlenwerten, fixierten Köpfen, Autofilter,
Δ%-Block (`ExcelBerichtGenerator.cs`); „—" statt 0 durchgängig (`double?`);
Kollisionsnamen ohne stilles Überschreiben (`BerichtCtrl.cs:36-52`);
Threading sauber über `Task.Run` + `IProgress<T>` + `CancellationToken`
(`Form_Bericht.cs:390-458`); O10 (Alt-Konzept) ist erledigt —
`ProjektDuplizierenCtrl` kopiert `Tab_ProjektWerte` generisch mit
(`ProjektDuplizierenCtrl.cs:245-252`).

**Abweichungen vom Konzept (in Fassung 3.1 nachgezogen):**

| # | Abweichung | Beleg | Behandlung |
|---|---|---|---|
| A1 | Diagramme über **GDI+/System.Drawing statt ScottPlot** (ScottPlot referenziert, ungenutzt) | `ChartRenderer.cs:3-8`; `csproj:94` | als Entscheidung dokumentiert (F3.1 Kap. 6) |
| A2 | PNGs entstehen **in-memory**, kein Temp-Ordner/finally | `ChartRenderer.cs:479-486` | F3.1 Kap. 6/8.2/10 angepasst |
| A3 | Farbzuordnung fest je **Erzeuger**, nicht je Variante; Balken nur Stamm/Variante-Zweifarbigkeit | `ChartRenderer.cs:25-33,120` | Ist dokumentiert; Variantenfarben als Nacharbeit |
| A4 | `Berichtskonfiguration`/`Tab_Variante`-Anlage per **Ad-hoc-DDL** statt `UpdateDatabaseFromScript` | `BerichtCtrl.cs:139-160`; `VariantenCtrl.cs:234-255` | als Ist dokumentiert; Nachdokumentation im nächsten Skriptstand |
| A5 | Lokalisierung über **Code-Wörterbuch** statt Satelliten-`.resx`; Diagramme/Excel-Köpfe/Fließtexte deutsch | `BerichtTexte.cs:36-98` | F3.1 EP10/Kap. 8.5 angepasst, Restumfang benannt |
| A6 | `IBerichtsBaustein` ohne `SchreibeExcel`; Excel monolithisch; Baustein-Reihenfolge aus Codeliste | `IBerichtsBaustein.cs:8-17`; `WordBerichtGenerator.cs:82-93` | F3.1 Kap. 8.3 angepasst |
| A7 | Dialog: kein Dateinamensvorschlag (nur Zielordner); Wirtschaftlichkeit hart gesperrt statt `IWirtschaftlichkeitProvider`+Tooltip | `Form_Bericht.cs:158-172,268-315` | bleibt Konzept-Soll → Nacharbeiten N22 bzw. Phase 6 |
| A8 | Kennzahlenkatalog inhaltlich unter Kap.-5-Soll (Details 3.4) | `KennzahlenKatalog.cs:70-150` | Restumfang in F3.1 Kap. 5 ausgewiesen |
| A9 | Balkendiagramme: nur 4 feste Schlüssel, kein CO₂-/Kosten-Diagramm | `BausteineVergleich.cs:152-153` | Nacharbeit (mit A8) |
| A10 | Anhang ohne Annahmen (Emissionsfaktoren, Preise, Strommix) und ohne Excel-Verweis; Hinweistext veraltet | `BausteineStandard.cs:89-97` | Nacharbeit N17 |
| A11 | Wirtschaftlichkeit (EP 11) noch nicht begonnen — bewusst Phase 6 | nur Kommentar `WordBerichtGenerator.cs:92` | planmäßig offen |
| A12 | Menüeinträge „Projekt → Bericht erstellen…"/„Als Variante speichern…" fehlen im MDI-Menü (bewusst offen, Designer-Konvention) | `MDIMainForm.*` ohne Treffer | Handgriff in Visual Studio (LIESMICH_Phase1) |

### 3.2 Status der bekannten Befunde B1–B7

| Befund | Status | Beleg |
|---|---|---|
| **B1** `carrier_id` in beiden Modultabellen | **teilweise** — Spalten (`ErgebnisCtrl.cs:745-758`, Muster `StelleXSpaltenSicher` ✓), Befüllung beim Speichern (`:76-77,258-259,323-324` via `CarrierIdFuerProjekt` `:778-806`) ✓, Lesen (`:547,594`) ✓. **Aber:** `EnergieMengen` nutzt weiter die Heuristik `CarrierFor()` (`EnergieMengen.cs:42,56,123-132`) statt `mo.CarrierId` — genau die vom Befund abgelöste Stelle bleibt aktiv (N11) |
| **B2** `ErgebnisCtrl.Delete` | **behoben** — `Delete(int)` mit gebundenem Parameter und `trans.Commit()` (`ErgebnisCtrl.cs:39-62`); alte Signatur `[Obsolete]` (`:34-35`) |
| **B3** Kesselmodul `Waermeproduktion` (+ Rundung) | **teilweise** — INSERT enthält `Waermeproduktion` und rundet (`ErgebnisCtrl.cs:307,319,321`; Spalte via `:754`). **Aber:** `SimulationRunner.BaueErgebnis` setzt `mo.Waermeproduktion`/`mo.Brennstoff`/`mo.Verbrauch` für Kesselmodule **nie** (`SimulationRunner.cs:281-289`) → immer 0/"" in der DB (N1) |
| **B4** `ProjektModel.m_nNetzverluste`/`m_szEinheit` | **offen** — unverändert (`ProjektModel.cs:18-19`), keine Verwendung im Auszug; faktische Quelle `KonfigurationCtrl` (`SimulationRunner.cs:43-44,70-71`) |
| **B5** Waisen-Prüfung `Tab_Variante` | **faktisch offen** — `VariantenCtrl.EntferneWaisen` (`:210-229`) existiert, aber (a) kein Aufrufer im gesamten Auszug, (b) SQL mit zwei ungeklammerten `LEFT JOIN` ist in Jet/ACE ein Syntaxfehler, verschluckt von `catch {}` (N4) |
| **B6** Kostenmodul | **offen (bestätigt)** — `Form_KostenAdmin.cs:75` (`Values (neueBezeichnung)` als SQL-Bezeichner, Parameter ohne Platzhalter); `Form_Kosten.cs:907-909` (`energy_price`-Ersteintrag ohne `leistungspreis`; Default landet nur in `energy_project_settings.custom_price_power`, `:928`); `ucFuelSettings.cs:415-417` (Speichern nur über eigenen Button) |
| **B7** Δ auf Rohwerten | **neuer Weg behoben** (`WordBerichtGenerator.cs:354-373`; Excel `:182`); **Altbericht offen** (`ProjektvergleichBericht.cs:430,589-590`, über Button „(alt)" erreichbar) — erledigt sich mit Stilllegung des Altberichts |

### 3.3 Neue Befunde N1–N27 (Code-Review 12.08.2026)

#### Hoch

**N1 — Brennstoff des Spitzenkessels fällt lautlos aus Kosten und CO₂**
`SimulationRunner.cs:281-289` · `KostenEmissionRechner.cs:52-53,61-62`
`BaueErgebnis` befüllt für Kesselmodule nur `Modul`, `Waerme_Gas`, `Waerme_Oel`,
`Jahresnutzungsgrad` — `Verbrauch` bleibt 0. `add()` verwirft `mwh <= 0` **vor**
der Träger-Prüfung; der Kessel taucht weder in `verbrauchJeTraeger` noch in
`verbrauchOhneTraeger` auf. Folge: `co2Vollstaendig` bleibt true, `CO2Gesamt` =
nur Netzstrom, `Energiekosten` = nur Strom — während „Brennstoffeinsatz gesamt"
(`KennzahlenKatalog.cs:99-100`) die korrekte MWh-Summe aus den Aggregatspalten
zeigt. Der Bericht widerspricht sich; „keine stillen Teilsummen" verletzt.
*Empfehlung:* `Verbrauch`/`Brennstoff`/`Waermeproduktion` je Kesselmodul in
`BaueErgebnis` analog zum BHKW-Weg (`ErgebnisCtrl.cs:227-232`) anteilig
verteilen; zusätzlich in `add()` Nullmengen nur überspringen, wenn tatsächlich
kein Verbrauch besteht.

**N2 — Preis 0 maskiert den Katalogpreis und erzeugt 0 € statt „—"**
`KostenEmissionRechner.cs:201-202,227-231`
`W()` liefert nur bei fehlender Spalte/`DBNull` null; eine vorhandene 0 in
`custom_price_work` ergibt 0.0 → `sPreis ?? kPreis` greift nicht auf
`energy_carrier.price_work` durch, `kostenVollstaendig` bleibt true →
Energiekosten mit 0 € als „vollständig" ausgewiesen. Gleiches beim Grundpreis.
*Empfehlung:* 0 als „nicht gesetzt" behandeln (`> 0`-Prüfung) oder explizit
zwischen „nicht gepflegt" und „bewusst 0" unterscheiden.

**N3 — Modale MessageBoxen aus dem Hintergrund-Thread**
`DataRepository.cs:43,66,90,148` · `ErgebnisCtrl.cs:58,422` · Worker:
`Form_Bericht.cs:409-425`
Alle Repository-Fehlerpfade zeigen `MessageBox.Show`; der Sammel-/
Generierungslauf läuft in `Task.Run`. Eine fehlende Abfrage/Tabelle erzeugt
je Träger und Variante einen modalen Dialog auf einem Nicht-UI-Thread — der
Lauf hängt, Abbruch greift nicht, Dialog ggf. hinter dem Hauptfenster.
*Empfehlung:* im Berichtspfad exception-basierte/stille Repository-Overloads
verwenden oder Meldungen in `daten.Warnungen` sammeln.

**N4 — `EntferneWaisen` nicht lauffähig und ohne Aufrufer (B5)**
`VariantenCtrl.cs:210-229`
Zwei `LEFT JOIN` ohne Klammern sind in Jet/ACE ungültig; korrekt:
`FROM (Tab_Variante v LEFT JOIN Tab_Projekt p ON …) LEFT JOIN Tab_Projekt s ON …`.
Fehler wird von `catch {}` verschluckt → liefert immer 0. Kein Aufrufer.
*Empfehlung:* SQL klammern; einmalig in `Form_Variantentest.Form_Load` bzw.
`BerichtCtrl` aufrufen.

**N5 — Strombilanz zählt PV doppelt**
`ChartRenderer.cs:181-188` · `ExcelBerichtGenerator.cs:330` · Quelle:
`ZeitreihenExtraktor.cs:54-55` (Semantik: `SimulationRunner.cs:325-326`,
`ErgebnisModel.cs:188,190`)
`PV_GENUTZT` wird aus `simulation_pv.Stromproduktion` befüllt — das ist die
**Gesamterzeugung**, nicht der Eigenverbrauch. Chart/Excel beschriften sie als
„PV-Eigenverbrauch" und stapeln sie mit BHKW-Strom und Netzbezug, daneben
erscheint `PV_UEBERSCHUSS` als „Einspeisung" → Stapelsumme übersteigt die
Bedarfslinie um den Exportanteil. *Empfehlung:* Eigenverbrauch elementweise als
`Stromproduktion − Ueberschuss` bilden, oder Reihe als „PV-Erzeugung"
beschriften und nicht stapeln.

#### Mittel

**N6 — Kennzahlen und Ganglinien können aus verschiedenen Läufen stammen**
`BerichtsDatenSammler.cs:201-216` — liefert `Save(frisch)` −1 (Rollback),
werden Zeitreihen trotzdem aus dem neuen Lauf gezogen, `Load()` liest den
alten; Simulationsfehler mit vorhandenem Altergebnis läuft ohne Warnung weiter.
*Empfehlung:* Save-/Simulationsfehler in `daten.Warnungen`; bei Save-Fehler
`v.Zeitreihen = null`.

**N7 — Kultur-Fallen (Berichtssprache greift nicht durch)**
Hart `de-DE`: `ChartRenderer.cs:35` (+ deutsche Titel/Legenden/Monatsnamen
`:148,161,182-188,309,498-502,556`), `AbweichungsErmittler.cs:111,139,205`;
`EnergieMengen.cs:99` (`string.Format` ohne Kultur → Thread-Kultur;
`Program.cs:52-61` setzt nur `CurrentUICulture`); `WordBerichtGenerator.cs:372`
(`"±0,0 %"` fest); `ExcelBerichtGenerator.cs:183` (Formatcode `;±0,0` — `,` ist
in Excel Tausendertrennzeichen), `:137-139,222-225,250-252,337-346` (Blattköpfe/
Monatsnamen fest deutsch).

**N8 — Abbruch wirkt erst nach der laufenden Variante**
`BerichtsDatenSammler.cs:131,191,199` · `Form_Bericht.cs:343-346` —
`runner.Simuliere()` kennt kein Token; `FormClosing` blockiert zusätzlich.
*Empfehlung:* Token in die Fortschritts-Callbacks der Simulation reichen oder
je Gewerk prüfen.

**N9 — Teil-Dateien bleiben bei Abbruch/Fehler liegen**
`BerichtCtrl.cs:40-52,73-84` · `WordBerichtGenerator.cs:43-75` — Vorlage wird
sofort ans Ziel kopiert; bricht ein Baustein ab, bleibt eine unvollständige
.docx. *Empfehlung:* in Temp-Datei schreiben, nach Erfolg `File.Move`.

**N10 — DB-Fehler sehen aus wie „Gewerk nicht vorhanden"**
`DataRepository.cs:41-45` (Fehler → leere `DataTable`) ·
`ProjektDetails.cs:75-84` — Komponentenmatrix zeigt „—" statt Fehler; die
Abweichungserkennung meldet fälschlich „Bestand: nicht vorhanden"
(`AbweichungsErmittler.cs:128-134`).

**N11 — `CarrierFor`-Heuristik weiter aktiv und fehleranfällig (B1-Rest)**
`EnergieMengen.cs:123-132` — bei unbekanntem Bezeichner wird
`id_brennstoff=0` abgefragt und ein beliebiger Treffer übernommen;
`bezeichner.Trim()` nicht null-sicher; `RecordSet` ohne `using` (Leak bei
Exception; `RecordSet.cs:6,144` ist `IDisposable`). *Empfehlung:* auf
`mo.CarrierId` umstellen, Heuristik nur als Fallback für Altdaten.

**N12 — Hilfsstrom kann als Brennstoffmenge ausgewiesen werden**
`EnergieMengen.cs:134-141` — `DominanterVerbrauch` nimmt `h.Stromverbrauch` in
die Kandidatenliste; bei kleinem Brennstoff- und größerem Hilfsstromwert wird
Hilfsstrom als „dominanter Brennstoff" umgerechnet.

**N13 — Deckungs-Kuchen mischt Bezugsgrößen**
`BausteineVergleich.cs:186-204` · Quellen: `SimulationRunner.cs:152,191-192,266-267`
(Bezug = Gesamtwärmebedarf) vs. `:302-303` (Solarthermie: Bezug = Restbedarf
ihrer Stufe) — Prozentwerte werden addiert, Rest als `100 − Summe`; bei aktiver
Solarthermie verzerrt.

**N14 — Netzbezug kann negativ werden**
`SimulationControl.cs:184` (`SubVectors(…, false)` beim BHKW ohne
Nullbegrenzung, anders als PV `:194`) → Reihe `NETZBEZUG`
(`ZeitreihenExtraktor.cs:58`) kann negative Stunden enthalten;
`ChartRenderer.cs:345-348` zeichnet negative Balkenhöhen, Excel-Monatstabelle
(`ExcelBerichtGenerator.cs:353`) weist negative Werte aus.

**N15 — Abweichungserkennung sieht nur die erste Zeile je Gewerk**
`ProjektDetails.cs:70` · `AbweichungsErmittler.cs:39-52,160-170` — auch die
Gruppe „Anlage" (14 Merkmale) vergleicht nur `Rows[0]`; bei Kaskaden/mehreren
Kesseln bleiben Unterschiede ab dem zweiten Gerät unsichtbar (nur die Anzahl
wird gemeldet).

**N16 — Neue Varianten erben den Simulationsstand des Stamms**
`ProjektDuplizierenCtrl.cs:245-252` (kopiert generisch jede Tabelle mit
`ID_Projekt`, also auch `Tab_Ergebnis*` und `Berichtskonfiguration`) →
frisch angelegte Variante zeigt den Stamm-Zeitstempel, `ErgebnisFehlte`/
`ErgebnisVeraltet` sind false — kein ⚠, obwohl nie eigenständig gerechnet.
*Empfehlung:* `Tab_Ergebnis` in `AUSNAHME_TABELLEN` (`:58-60`) aufnehmen oder
nach dem Duplizieren `new ErgebnisCtrl().Delete(neueId)`.

**N17 — Anhang weist die Rechenannahmen nicht aus**
`BausteineStandard.cs:89-97` — Konzept fordert Emissionsfaktoren, Energiepreise,
Klimadatensatz, Excel-Verweis; der Strommix-Parameter 380 g/kWh
(`KostenEmissionRechner.cs:30`, Kommentar `:24-25` „im Anhang auszuweisen")
erscheint nirgends; der Hinweistext `:96-97` nennt die Emissions-/
Kostenverrechnung noch „Ausbaustufe" (nach Phase 5 falsch).

**N18 — Preishistorie `energy_price` wird ignoriert**
`KostenEmissionRechner.cs:155-184` — gelesen werden nur
`energy_project_settings.custom_price_*` und `energy_carrier.price_*`;
`energy_price` (`valid_from`/`valid_to`, gepflegt in `ucFuelSettings.cs:327,346`)
bleibt unbenutzt → nicht der zeitlich gültige Preis. Zudem wird
`price_base`/`custom_price_base` pauschal als €/a addiert (`:84,106`), obwohl
bei Strom daneben ein Leistungspreis (€/kW·a) existiert.

#### Niedrig

**N19** — GDI-Leak: `new SolidBrush(C_STAMM)` in `ChartRenderer.cs:388` nie
disposed (ein Handle je Diagramm); Parameter `breite` (`:385`) unbenutzt.
**N20** — Off-by-one: `ChartRenderer.cs:415` (`xpos[i] / (n-1)`) — Label
„8.760 h" liegt außerhalb der Zeichenfläche.
**N21** — Toter Code: `Form_Bericht.BaueZusammenfassung`
(`Form_Bericht.cs:461-488`) wird nicht aufgerufen.
**N22** — Dateinamensvorschlag fehlt im Dialog (nur Zielordner,
`Form_Bericht.cs:159-171`; Konzept-Mockup zeigt den vollen Namen).
**N23** — Alle Varianten abgewählt + gespeichert → beim nächsten Öffnen wieder
alle angehakt (`Form_Bericht.cs:253-254`, Bedingung `VariantenIds.Count == 0`).
**N24** — `WordKontext.MitStil` schickt **jeden** Absatztext durchs
Übersetzungswörterbuch (`WordBerichtGenerator.cs:187`) — ein Projekt-/
Variantenname wie „Anhang"/„Stamm"/„Kosten" würde im en-Bericht mitübersetzt.
**N25** — Excel: Brennstoffmengen als Text (`ExcelBerichtGenerator.cs:259`,
Quelle `EnergieMengen.cs:99` liefert „1.234 l"); Blatt „Vergleich" entsteht
unabhängig vom Baustein `B_VERGLEICH` (`:37`).
**N26** — `ws.Columns().AdjustToContents(1, 60)`
(`ExcelBerichtGenerator.cs:121,193,270`) bindet an die Überladung
`(startRow, endRow)` — vermutlich nicht die beabsichtigte Breitenbegrenzung.
**N27** — Statische Kompilierbarkeit, **nicht prüfbar (Dateien nicht im
Auszug)**: fünf im `ZeitreihenExtraktor` referenzierte Member sind im Auszug
nirgends definiert — `simulation_wp.WP_Waermeproduktion_stuendlich` (`:37`),
`simulation_spk.Kesselleistung_stuendlich` (`:47`),
`simulation_solarthermie.Waermeproduktion` (`:49`; das Aggregat heißt in
`SimulationRunner.cs:300` `Waermeproduktion_gesamt`),
`simulation_pv.Speicherfuellstand` (`:56`), `puffer_wp.SOC_stuendlich`
(`:64-65`). Ebenfalls nicht im Auszug: `Vorlagen/Berichtsvorlage.docx` (Inhalt).
→ Beim nächsten Build unter Windows gegenprüfen (Projekt kompiliert laut
Build-Artefakten vom 11.08. — `bin\x86\Debug`-Stand —, daher vermutlich nur
Namensdifferenz zwischen Auszug und Engine-Vollstand).

### 3.4 Ergänzende Prüfergebnisse

- **KostenEmissionRechner nachgerechnet:** Einheitenlogik korrekt
  (Brennstoff-CO₂ `MWh × g/kWh / 1000 = t/a`; spezifisch `t/a × 1000 / MWh =
  g/kWh`; Menge `MWh × 1000 / (kWh/Einheit)`, Direktabrechnung `MWh × 1000 ×
  €/kWh` — deckungsgleich mit `EnergieMengen.Menge()` und `ucFuelSettings`).
  CO₂-Kette wie Vorgabe 11.08. umgesetzt; Vorrangprüfung `> 0` behandelt einen
  bewusst gepflegten Faktor 0 (z. B. Biomasse) als „nicht gesetzt" — fachlich
  klären. `FindeStromTraeger` nimmt den ersten `ELECTRICITY`-Träger — bei
  mehreren Stromtarifen (HT/NT) undefiniert.
- **Sprachabdeckung:** zweisprachig sind Kapitelüberschriften, fette
  Tabellenköpfe, Kennzahl-Labels; deutsch bleiben Fließ-/Hinweistexte,
  Merkmalslabels (Kenndaten/Abweichungen), Gewerknamen, alle Diagramm- und
  Excel-Beschriftungen sowie der Dialog `Form_Bericht`.
- **`AnlegenAusStamm` dupliziert über den Projektnamen** (`VariantenCtrl.cs:122`
  → `Duplizieren(stammName, …)` → `GetProjektId`): bei zwei gleichnamigen
  Projekten wird ggf. das falsche kopiert — perspektivisch auf ID-basiertes
  Duplizieren umstellen.
- **Variante-als-Ausgangspunkt** korrekt: `Form_AlsVariante.Zeige` löst über
  `StammRefDerVariante` auf den Stamm auf (`Form_AlsVariante.cs:40-48`).
- **Kein SQL-Injection-Risiko im neuen Code** (durchgängig `OleDbParameter`);
  Access-Syntax korrekt bis auf N4.

---

## 4. Konsistenz der Konzeptdokumente (Befunde K1–K12)

| # | Befund | Behandlung (12.08.2026) |
|---|---|---|
| K1 | Leitkonzept F3 nannte **ScottPlot** als Chart-Technik, Temp-Ordner-PNGs und Variantenfarben; Code (bewusst, laut Statusdoku) nutzt GDI+, in-memory, Erzeugerfarben | in **F3.1** als Entscheidung nachgezogen (Kap. 2/6/8.1/8.2/10) |
| K2 | F3 EP 10/Kap. 8.5 nannte Satelliten-`.resx` für Berichtstexte; Code nutzt das Wörterbuch `BerichtTexte` mit Teilabdeckung | in **F3.1** angepasst inkl. Restumfang |
| K3 | F3 Kap. 8.3 beschrieb `SchreibeExcel` je Baustein und Reihenfolge aus der Konfiguration; Code: Excel monolithisch, Reihenfolge aus Codeliste | in **F3.1** angepasst |
| K4 | F3 Kap. 8.4 verlangte Anlage der `Berichtskonfiguration` über `UpdateDatabaseFromScript`; Code legt ad hoc an (ebenso `VariantenCtrl`) | in **F3.1** als Ist dokumentiert; Nachdokumentation im nächsten Skriptstand vereinbart |
| K5 | `Konzept_Wirtschaftlichkeit.md` (Repo-F2) enthielt **tote Querverweise** auf die Kapitelstruktur des aufgelösten Variantenbericht-Konzepts („Berichtskonzept Kapitel 6 / 3.4 / 7.1", „6.1 im Berichtskonzept") — diese Kapitel existieren in F3 nicht bzw. bedeuten anderes | in **F2.1** korrigiert (jetzt: Kap. 5.8 dieses Dokuments; Leitkonzept Kap. 3.1/7/8.1) |
| K6 | Der **Datenvertrag** (`WirtschaftlichkeitErgebnisModel` + `IWirtschaftlichkeitProvider`, C#-Definition) stand nur im aufgelösten `Konzept_Variantenbericht.md` F2 Kap. 6.1 — bei der Konsolidierung auf F3 ging die maßgebliche Quelle verloren | in **F2.1 als Kap. 5.8** übernommen (inkl. Darstellungsregeln Word/Excel) |
| K7 | F2 Kap. 6 referenzierte `VergleichsDaten` — die Klasse heißt im umgesetzten Stand `BerichtsDaten`/`BerichtsDatenSammler` | in **F2.1** korrigiert |
| K8 | Statusdoku `LIESMICH_Phase1.md` meldete B1/B3 „behoben", Waisen-Prüfung vorhanden und „keine stillen Teilsummen" — vom Review teilweise widerlegt (3.2, N1/N2/N4/N11) | **Prüfvermerk** in der Statusdoku ergänzt; F3.1 Kap. 11 korrigiert |
| K9 | Kopfzeilen inkonsistent: F3 „Entscheidungsrunden 1–4" vs. LIESMICH „Runden 1–5"; F3-Status „Code- und DB-Verifikation abgeschlossen" (DB-Nachprüfung faktisch offen) | in **F3.1** vereinheitlicht (Runden 1–5; DB-Nachprüfung als ausstehend deklariert) |
| K10 | F3 Kap. 12 führte die Phasen ohne Ist-Status, obwohl 1–5 umgesetzt sind | **F3.1**: Statusspalte + Restarbeiten + Restaufwand |
| K11 | Die im Chat übergebenen Dateien (Erstfassung EPOSPlan, Variantenbericht F2) sind **Altstände** — Weitergabe/Parallelpflege würde die Konsolidierung rückgängig machen | maßgeblich ist ausschließlich `Allgemein\Reporting\` (F3.1/F2.1 + Prüfbericht); Altstände sind im Claude-Projekt archiviert |
| K12 | F3 Kap. 5 (Emissionen) nannte die CO₂-Quellen unvollständig (ohne `Tab_Brennstoff_Stamm`-Zwischenstufe und Strommix-Wert aus der Phase-5-Vorgabe) | in **F3.1** präzisiert (Faktorkette + 380 g/kWh + Einheiten-Prüfauftrag) |

Keine inhaltlichen Widersprüche bestehen zwischen F3.1 und F2.1 hinsichtlich
Methode (Kapitalwert nach DIN EN 17463), Persistenzmodell
(`Tab_ProjektWirtschaftlichkeit`/`Tab_ErgebnisWirtschaftlichkeit` mit FK auf den
Simulationslauf), UI-Reiter und Berichts-Andockpunkt.

---

## 5. Durchgeführte Konsolidierung (Dateistand nach dieser Prüfung)

| Datei | Änderung |
|---|---|
| `Allgemein\Reporting\Konzept_Berichtserstellung_EPOS-Plan.md` | **Fassung 3 → 3.1**: Kopf/Status, Konsolidierungsvermerk, EP 10, Kap. 2 (Charts), 3.1 (Ist-Hinweise), 5 (CO₂-Kette, Umsetzungsstand Katalog), 6/6.1 (GDI+, in-memory, Farben, Ist der Balkendiagramme), 8.1–8.5 (Struktur-Kommentar, Ablauf, Baustein-Abstraktion, Ad-hoc-DDL, Wörterbuch-Lokalisierung), 10 (Teil-Dateien), 11 (Befundstatus B1–B7 + Verweis N1–N27), 12 (Phasenstatus + Restaufwand), Fußzeile |
| `Allgemein\Reporting\Konzept_Wirtschaftlichkeit.md` | **Fassung 2 → 2.1**: Änderungsvermerk, Statusmarker in Kap. 3.5 (Befunde 1–6), **neues Kap. 5.8 Datenvertrag** (Ergebnisklasse + Provider, Darstellung Word/Excel), Querverweise auf das Leitkonzept korrigiert (Kap. 5.5/5.7/6), `VergleichsDaten` → `BerichtsDaten`-Terminologie |
| `Allgemein\Reporting\LIESMICH.md` | Stand 12.08.2026, Dateirollen (3.1/2.1), Prüfbericht-Zeile, Abschnitt „Umsetzungs- und Prüfstand" |
| `Allgemein\Bericht\LIESMICH_Phase1.md` | Prüfvermerk 12.08.2026 (Korrektur B1/B3/B5, N1/N2) |
| `Allgemein\Reporting\Pruefbericht_Berichtsmodul_2026-08-12.md` | **neu** (dieses Dokument) |

Unverändert gelassen: Quellcode (Prüfung, keine Fixes — Nacharbeiten in Kap. 7),
`Konzept_Variantenbericht.md`/`LIESMICH_Geruest.md` (Verweis-Stubs, zur
Handlöschung markiert), `Reporting_Geruest.zip` (Archiv).

---

## 6. Offene DB-Verifikation — Checkliste für `Kenndaten.accdb`

Der Zugriff auf `C:\ProgramData\EPOS_PLAN` wurde in dieser Session nicht
erteilt. Sobald verfügbar (Ordner freigeben oder Datei erneut anhängen), sind
folgende Punkte am Schema zu verifizieren (Aussagen stammen bislang aus der
Verifikation vom 11.08. bzw. aus dem Code):

1. **`Tab_Variante`**: Spalten `ID, ID_Projekt, ID_ProjektRef, Variantenname`;
   keine FK-Löschweitergabe (Grundlage B5/N4).
2. **`Tab_Ergebnis*`** (13 Tabellen): vorhanden; nach einem Simulationslauf mit
   neuem Code: `carrier_id` in `Tab_ErgebnisBHKWModul` und
   `Tab_ErgebnisHeizkesselModul` **befüllt**; `Waermeproduktion`-Spalte im
   Kesselmodul (nach N1-Fix auch befüllt); Rundung der Werte (2 NK).
3. **`Berichtskonfiguration`**: entsteht beim ersten Öffnen von `Form_Bericht`
   (`ID` PK, `ProjektID` UNIQUE, `KonfigJson` Memo/LONGTEXT, `GeaendertAm`).
4. **`energy_project_settings`**: Spalten `co2`, `custom_price_work`,
   `custom_price_base`, `custom_price_power`; **`energy_carrier`**: `co2`,
   `price_work`, `price_base`, `id_brennstoff`, `pricing_model`;
   **`Tab_Brennstoff_Stamm`**: `CO2`-Spalte — **Einheit klären** (Annahme des
   `KostenEmissionRechner`: g/kWh = kg/MWh für alle drei Quellen; LIESMICH_Phase1
   Prüfpunkt 1).
5. **`energy_price`**: `valid_from`/`valid_to`, `leistungspreis` (Befund B6:
   Ersteintrag lässt `leistungspreis` leer); Preishistorie als Stützstellen für
   N18/Wirtschaftlichkeit.
6. **Abfragen**: `Abfrage_Energietraeger_Effektiv`, `Abfrage_Kosten_*`,
   `Abfrage_ProjektKostenEnergie`, `Abfrage_ProjektKostenInvestBetrieb`,
   `Abfrage_ProjektKostenKomponenten` vorhanden und spaltenkompatibel zu
   `KostenEmissionRechner`/`Form_Kosten`.
7. **`Tab_Projekt`**: `Aenderungsdatum` (Grundlage der Veraltet-Warnung);
   bestätigt **kein** Feld „Variantenbezeichner"; keine Spalten für
   `m_nNetzverluste`/`m_szEinheit` (B4).
8. **`Tab_ProjektWerte`**: `ProjektID`, `KomponentenID`, `KategorieID`,
   `Gruppe`, `Betrag`, `Nutzungsdauer`, Best-/Worst-Felder;
   `Tab_KostenKomponente`/`Tab_KostenKategorie`/`Tab_Kostenfaktor` vorhanden.
9. **`Tab_Energieanlagen`** und Gewerk-Projekttabellen: Spalten, die
   `ProjektDetails`/`AbweichungsErmittler` referenzieren (Code ist
   spaltentolerant, stille Ausfälle aber möglich — N10).
10. **Noch nicht vorhanden** (geplant, Wirtschaftlichkeits-Paket):
    `Tab_ProjektWirtschaftlichkeit`, `Tab_ErgebnisWirtschaftlichkeit`.
11. Stichprobe: CO₂-Faktorwerte je Träger (Projekt- vs. Katalogwert) und ein
    manuell nachgerechneter Emissions-/Kostenwert gegen den Bericht.

---

## 7. Priorisierte Nacharbeiten (Code)

**Sofort (Belastbarkeit/Betrieb, ~2–3 PT):**

1. **N1** Kesselmodul-`Verbrauch`/`Brennstoff`/`Waermeproduktion` in
   `SimulationRunner.BaueErgebnis` befüllen (analog BHKW-Weg) — schließt B3.
2. **N2** Preis/Faktor 0 als „nicht gesetzt" behandeln (oder explizit
   unterscheiden) — stellt die „—"-Regel wieder her.
3. **N3** Berichtspfad auf stille/exception-basierte Repository-Aufrufe
   umstellen; Meldungen in `daten.Warnungen`.
4. **N5** PV-Eigenverbrauch = `Stromproduktion − Ueberschuss` (Chart + Excel).
5. **N4** `EntferneWaisen`: SQL klammern + Aufruf einbauen — schließt B5.

**Danach (~1–2 PT):** N11/B1-Rest (`EnergieMengen` auf `CarrierId`), N16
(`Tab_Ergebnis` beim Duplizieren ausnehmen oder Ergebnis löschen), N6
(Lauf-Konsistenz bei Save-/Sim-Fehler), N9 (Temp-Datei + `File.Move`), N8
(Abbruch in die Simulation reichen).

**Kür (~2–3 PT):** Kennzahlkatalog-Restumfang (Kap. 5 F3.1) + CO₂-/Kosten-
Balkendiagramme, N17 (Anhang-Annahmen inkl. Strommix), N18 (Preishistorie,
Leistungspreis), N7 (Kultur/Übersetzung Diagramme + Excel), N12–N15,
N19–N26; Menü-Einbindung (2 Einträge in Visual Studio, LIESMICH_Phase1);
Altbericht „(alt)" nach Praxistest stilllegen (erledigt B7 vollständig).

**Phase 6 (eigenes Paket):** Wirtschaftlichkeit nach `Konzept_Wirtschaftlichkeit.md`
Fassung 2.1 (Stufen W1–W3, Datenvertrag Kap. 5.8, UI-Reiter, DB-Zusätze über
`UpdateDatabaseFromScript`).

---

*Prüfung durchgeführt in der Cowork-Session vom 12.08.2026 (Code-Review als
unabhängiger Agentenlauf mit Datei-/Zeilenbelegen). Zeilenangaben beziehen sich
auf den Quellstand vom 11.08.2026 (mtime der geprüften Dateien); nach
Code-Änderungen können sich Zeilennummern verschieben.*

