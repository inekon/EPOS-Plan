# Umsetzungsstand Berichtserstellung + Wirtschaftlichkeit

**Stand: 15.08.2026 (nach Phase 11 + verbindlicher Rechenkette); Nachtrag 28.08.2026
(Dreikanal-Berichtsteile, siehe Ende von Abschnitt 1)** · Bezug:
`Konzept_Berichtserstellung_EPOS-Plan.md`, `Konzept_Wirtschaftlichkeit.md` (Fassung 7).
Diese Datei beschreibt nur den **aktuellen** Stand; die Phasen-Historie steht in
`Allgemein\Bericht\LIESMICH_Phase1.md`.

## 1. Was die App heute kann

### Berichtserstellung (Phasen 1–5)
- **Jeder Berichtslauf rechnet neu** (Nutzeranforderung 15.08.2026, Kap. 6):
  erst frische Simulation aller gewählten Projekte, dann die
  Wirtschaftlichkeitsrechnung derselben Gruppe, dann erst die Bausteine —
  für Word und Excel derselbe Einstieg.
- **Varianten-Verwaltung**: Vergleichsgruppe = Stammprojekt + Varianten
  (`Tab_Variante`, `VariantenCtrl`); Anlegen aus dem Stamm, Waisen-Prüfung.
- **Word-Vergleichsbericht** (OpenXML, Vorlage `Berichtsvorlage.docx` mit
  INEKON-CI, frei anpassbar): Deckblatt, Inhaltsverzeichnis, Projektbeschreibung
  inkl. Gebäude, Komponenten-Matrix + Kenndaten + Abweichungen je Variante
  (3 Stufen), Ergebnisse je Variante, Variantenvergleich mit Δ-Tabellen und
  Blocksplitting, Anhang (Simulationsstände, Methodik).
- **Diagramme** (GDI+, PNG): Deckungs-Kuchen, Vergleichs-Balken, 4 Ganglinientypen
  (Wärme-Jahresverlauf, Jahresdauerlinie, Strombilanz je Monat, Speicherverlauf);
  Daten aus frischer In-Memory-Simulation je Projekt (keine gemischten Stände).
- **Excel-Bericht** (ClosedXML): Kennzahlen-, Vergleichs- und Detailblätter.
- **Konfigurierbare Bausteine** je Bericht (`Berichtskonfiguration`, JSON in DB);
  zweisprachig (de/en) über `BerichtTexte`, Zahlenformate kulturabhängig.
- **Emissionen**: CO₂/SO₂/NOx je Energieträger über die Kette
  `energy_project_settings → Tab_Brennstoff_Stamm → energy_carrier`
  (Projektwert vor Stammdaten vor Fallback); Einheiten verifiziert
  (CO₂ g/kWh, SO₂/NOx mg/kWh).
- **Nachtrag 28.08.2026 — Dreikanal-Berichtsteile** (Konzeptumsetzung
  Brauchwasser/Heizung/Pufferspeicher, Pakete E1/P2/E2): Der Kennzahlenkatalog
  weist die drei Wärmebedarfe („davon Heizung/Brauchwasser/Prozesswärme") und
  **Deckungsgrade je Bedarfsart** aus (die eine Umrechnung liegt zentral im
  `KennzahlenKatalog`); neuer Word-Abschnitt **Speichertemperaturen** (Tabelle
  `T_oben_Mittel`/`T_oben_Min` je Speicher, entfällt ohne Werte) mit dem
  **fünften Ganglinientyp** (Temperaturverlauf, `ChartRenderer.Speichertemperaturen`);
  SOC-Zeitreihen laufen **je Speicher** über `PUFFER_<ID>` (der Alias `puffer_wp`
  ist abgelöst, `ChartRenderer.Speicherverlauf` zeichnet mehrlinig); der
  `ZeitreihenSatz` führt zusätzlich `PUFFER_<ID>_TOBEN/_TUNTEN`,
  `QUELLTEMP_<AnlagenID>` (Booster) sowie `BEDARF_<KANAL>`- und
  `DECKUNG_<ERZEUGER>_<KANAL>`-Reihen. Offen: der Bericht zeichnet die
  Kanal-Bedarfs-/Deckungsreihen noch nicht als eigenes Diagramm (E2-O2) und der
  Variantenbericht kennt den fünften Ganglinientyp noch nicht (P2-Restpunkt).

### Wirtschaftlichkeit (Phasen 6–10)
- **W1 — Kapitalwertmethode DIN EN 17463**: KW, Annuität, Barwerte je
  Vergleichsgruppe; Stamm = Referenz; Ersatzbeschaffung (ganzjährig gerundet),
  Restwert linear; Szenarien Erwartung/Best/Worst aus `Tab_ProjektWerte`;
  fehlende Preise erscheinen als Begründung, nie als 0.
- **W2 — Erweiterungen**: Differenz-Kennzahlen (KW-Diff, dynamische Amortisation
  mit Interpolation, IRR per Bisektion), Sensitivitäten (Zins ±1 pp, Preisst.
  Energie ±1 pp, Invest ±10 %, Energiekosten ±10 %) + Novellen-Zeile
  „KWKG-Bonus entfällt", BEHG-CO₂-Abgabe (nur fossile Kategorien).
- **W3 — Tarifmodell + Emissionsbilanz**: HT/NT × Winter/Sommer-Strommatrix aus
  den Stundenreihen (Referenzjahr 2026), zweistufige Leistungspreis-Staffel,
  Tarifersatz nur bei vollständig gepflegten Preisen (sonst Flat + Hinweis);
  Emissionsbilanz gekoppelt/getrennt gegen wählbaren Referenz-Kraftwerkspark
  (`Tab_Kraftwerkspark`, vorbefüllt) inkl. CO₂-Vermeidung — nur bei aktuellem
  Rechenstand (`ErgebnisAktuell`).
- **KWKG 2025** (Konzept Kap. 8): degressive Vbh-Staffel in `Tab_KWKG_Staffel`
  (2020: 5 000 → ab 2030: 2 500; Kontingent 30 000 Vbh, pflegbar), Deckel-Override
  (0 = Staffel), Fristenlogik § 6 (Stichtag ≤ 31.12.2026, Realisierung bis
  31.12. des 4. Folgejahres), Guards > 500 kW und Heizöl-Neuanlagen,
  Negativpreis-Abschlag (kontingentschonend), Nachweiszeile in Reiter/Word/Excel.
- **Kategorisierter Parameterdialog** (Phase 10): „Allgemein" immer sichtbar
  (Zins, T, Preissteigerungen); Erzeuger-Gruppen nur, wenn der Typ in der
  Vergleichsgruppe vorkommt — „Photovoltaik" (Einspeisevergütung), „BHKW — KWKG
  2025", „Brennstoff — BEHG und Emissionsbilanz" (CO₂-Preis, Kraftwerkspark).
  Werte ausgeblendeter Gruppen bleiben beim Speichern erhalten.
- **Referenzkessel aus der DB** (Phase 11): Wirkungsgrad + Brennstoff der
  getrennten Referenz kommen aus dem größten Heizkessel des Stammprojekts
  (`Tab_Heizkessel`; Öl → Wirkungsgrad_Öl, sonst _Gas; Plausibilitätsband
  50–115 %, sonst gespeicherte Vorgabe) — nicht mehr im Dialog gepflegt,
  dort nur noch Info-Anzeige; Quelle wird im Bericht ausgewiesen.
- **Kapitalwert-Verlauf** (Phase 11): Dialog „Verlauf…" mit zwei
  Liniendiagrammen — Differenz zur Stamm-Referenz (Nulldurchgang = dynamische
  Amortisation) und absolute kumulierte Barwerte; **Zeitraum frei wählbar
  (2–60 a, auch > T** — Neuberechnung mit verändertem Horizont, gespeicherte
  Ergebnisse unverändert**)**, Szenario wählbar; im Word-Bericht als Diagramme,
  im Excel-Blatt als Jahresreihen-Block (mit Konsistenz-Gate bei fehlenden
  Stundenreihen). Reihen ohne Restwert; Restwert-Barwerte werden ausgewiesen.
- **Ausgabe**: Reiter im Wirtschaftlichkeits-Dialog, Word-Baustein, Excel-Blatt
  (Szenarioblöcke, Sensitivität, Strommatrix, Emissionsbilanz; Zeilen nur, wenn
  die Funktion aktiv ist).

### Kostenmodul-Befunde (behoben)
B4 Kostenfaktoren anleg-/löschbar · B5 `energy_price.leistungspreis` wird
befüllt · B6 Formular-Schließen ohne Speichern-Button verliert keine Werte und
belebt gelöschte Träger nicht wieder.

## 2. Datenbank (legt die App beim ersten Start selbst an)

`Berichtskonfiguration`, `Tab_Variante`, `Tab_ProjektWirtschaftlichkeit`,
`Tab_ErgebnisWirtschaftlichkeit`, `Tab_ErgebnisWirtSensitivitaet`,
`Tab_ProjektTarif`, `Tab_ErgebnisStromMatrix`, `Tab_Kraftwerkspark`
(vorbefüllt), `Tab_KWKG_Staffel` (vorbefüllt, 8 Zeilen) sowie zusätzliche
Spalten in Bestandstabellen (`carrier_id`, `Waermeproduktion`, KWKG-Spalten)
per additivem Schema-Upgrade — bestehende Daten bleiben unberührt.

## 3. Verifikation

Rechenkern numerisch verifiziert (Python-Nachbau, 40 + 31 Prüfungen bestanden,
Details in `PRUEFBERICHT_Rechenkern.md` inkl. 13-Punkte-Abnahme-Checkliste);
jede Phase mit Opus-Code-Review (Phase 11 mit zweitem Verifikationslauf),
alle Befunde behoben — darunter zwei erst in Phase 11 entdeckte
Compile-Fehler aus Altphasen (typografische Anführungszeichen in
`EmissionsBilanzRechner.cs` und `BausteineVergleich.cs`, beide korrigiert).
Der Visual-Studio-Build ist noch nicht gelaufen (in der Cloud-Umgebung kein
Compiler).

## 4. Offene Schritte (Nutzerseite)

1. **Build**: `dotnet restore` / VS-Build (x86); neue Pakete: ClosedXML 0.105.1,
   SixLabors.Fonts 1.0.1 (gepinnt). Compilerfehler einfach zurückmelden.
2. ~~**Zwei Designer-Handgriffe**~~ **erledigt (22.08.2026)** — beide ohne Eingriff in
   Designer- oder `.resx`-Dateien, siehe Kap. 7.
3. **Funktionstests** nach `PRUEFBERICHT_Rechenkern.md` Kap. 2 + 3 sowie
   Testschritte Phase 10 im LIESMICH.

## 5. Bewusst offener Scope

Preisszenarien aus der `energy_price`-Historie · positionsbezogene
Zins-Overrides je Gewerk · exakte Spotpreis-Stundenrechnung für den
Negativpreis-Abschlag (§ 7 Abs. 5 KWKG) · getrennte KWK-Strommengen aus der
Simulation statt min-Regel · Gegenprüfung an `VALERI_Vorlage_V7.xlsx`
(Datei bei Bedarf anhängen).

## 6. Verbindliche Rechenkette je Berichtslauf (15.08.2026)

**Anforderung:** Bei der Berichtserstellung — Word **und** Excel — werden immer alle
ausgewählten Varianten frisch simuliert und wirtschaftlich gerechnet. Ein Bericht darf
nie auf veralteten Ergebnissen oder einer übersprungenen Rechnung stehen.

### 6.1 Ist-Befund vor dem Umbau

| Stelle | Befund |
|---|---|
| `Allgemein/Bericht/BerichtsDatenSammler.cs:198` | Simuliert nur bei `neuRechnen \|\| ErgebnisFehlte \|\| mitZeitreihen`. Wer im Dialog „Vor Ausgabe neu rechnen" abwählte und den Baustein „Ergebnisse je Variante" nicht brauchte, bekam den gespeicherten Altstand. |
| `Views/Bericht/Form_Bericht.cs` (Checkbox `chkNeuRechnen`) | Die Abwahl war persistiert (`BerichtsKonfiguration.NeuRechnen`) und galt beim nächsten Öffnen wieder. |
| `Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs:38` | Baustein las **nur** `WirtschaftlichkeitCtrl.LadeErgebnisse(...)` — den im Reiter gespeicherten Stand. Ohne Reiterbesuch: Kapitel nur mit Hinweis „noch keine Wirtschaftlichkeit berechnet". |
| `Allgemein/Bericht/ExcelBerichtGenerator.cs:212` | Dasselbe für das Excel-Blatt. Word und Excel rechneten also **nie** selbst. |
| `Views/Varianten/ProjektvergleichBericht.cs:73` | Alter Direktbericht (Schaltfläche „Vergleich" in `Form_Variantentest`) lud nur `ErgebnisCtrl.Load(...)` — gar keine Simulation. |

**Belegt auf einer Wegwerf-Kopie der Produktiv-DB** (Stamm 1019 „Wöhler" + Varianten 1023/1024),
Altpfad gegen Neupfad ohne jede Eingabeänderung:

| Wert | Altpfad (gespeicherter Stand) | Neupfad (frisch) |
|---|---|---|
| Energiekosten Stamm | 31.774 €/a | 40.636 €/a |
| CO₂ Stamm | 50,76 t/a | 64,94 t/a |
| Kapitalwert Variante „Test2" | — (`Fehlgrund`: Energiekosten nicht bestimmbar) | −2.142.731 € |
| Wirtschaftlichkeits-Stand | 12.08. 10:15, für 2 von 3 Projekten `ErgebnisAktuell = false` | Laufzeitpunkt, alle `true` |

### 6.2 Umbau

| Datei | Änderung |
|---|---|
| `Allgemein/Bericht/BerichtsDatenSammler.cs` | Neu: `SammleFuerBericht(...)` — einziger Einstieg der Berichtserzeugung. Ruft `Sammle(..., neuRechnen: true, ...)` und danach `RechneWirtschaftlichkeit(...)`. Fortschritt über `FortschrittMitZusatz` (Gesamtzahl +1 für den Wirtschaftlichkeitsschritt). |
| `Allgemein/Bericht/BerichtsDaten.cs` | Neue Felder `Wirtschaftlichkeit` (Ergebnisse dieses Laufs) und `WirtschaftlichkeitFehler`. |
| `Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs` | Quelle ist `daten.Wirtschaftlichkeit`; `LadeErgebnisse` nur noch Rückfallnetz, das im Bericht ausgewiesen wird. Hinweistexte auf den Berichtslauf umgestellt. |
| `Allgemein/Bericht/ExcelBerichtGenerator.cs` | Dieselbe Quelle und derselbe Rückfall — Word und Excel zeigen zwingend dieselben Zahlen. |
| `Allgemein/Bericht/Bausteine/BausteineStandard.cs` | Anhang/Methodik nennt die Kette ausdrücklich. |
| `Views/Bericht/Form_Bericht.cs` | Checkbox „Vor Ausgabe neu rechnen" **entfernt** (Hinweisbeschriftung an ihrer Stelle); vor dem Start Aufwandsabfrage mit Projektzahl; Aufruf auf `SammleFuerBericht`. |
| `Views/Varianten/ProjektvergleichBericht.cs` | `SimuliereGruppe(...)` vor dem Laden der Ergebnisse; Meldungen der Läufe über `Laufmeldungen`. |
| `Views/Varianten/Form_Variantentest.cs` | Zeigt `Laufmeldungen` in der Abschlussmeldung. |

Die Wirtschaftlichkeit wird **gruppenweise** gerechnet (ein Aufruf von
`WirtschaftlichkeitCtrl.Berechne` für Stamm + alle Varianten), weil die Kennzahlen einer
Variante Differenzen gegen den Stamm sind. Es wurde keine Rechenformel verändert oder
dupliziert — nur der bestehende Weg aufgerufen und persistiert.

**Fehlerverhalten** (wie bisher bei Simulationsfehlern im Sammler): Der Lauf bricht nicht
ab. Das betroffene Projekt bekommt `VariantenDaten.Fehler` bzw. den `Fehlgrund` der
Wirtschaftlichkeit, beides erscheint mit Variantennamen in `BerichtsDaten.Warnungen` →
Abschlussmeldung des Dialogs, Anhangtabelle „Simulationsstände" und Kapitel „Hinweise
dieses Berichtslaufs". Kein halber Bericht ohne Hinweis.

### 6.3 Aufwandshinweis

Der Schnellpfad „ohne Neuberechnung" **entfällt bewusst**. Kosten je Berichtslauf:
n Projekte × (Simulation + Zeitreihen) + einmal Wirtschaftlichkeit für die Gruppe.
Gemessen headless (3 Projekte der Gruppe 1019/1023/1024, Debug-x86):
**22–24 s** Simulation + Wirtschaftlichkeit, **7–12 s** Word + Excel — rund **35 s**
für einen vollständigen Bericht mit drei Projekten. Der Dialog beziffert die Projektzahl
vor dem Start.

### 6.4 Verifikation (headless, Wegwerf-Kopie der `Kenndaten.accdb`)

| Test | Ergebnis |
|---|---|
| **Frische** — Eingaben auf der Kopie geändert (Raumsolltemperatur 20 → 23 °C nur im Stamm, Arbeitspreis 0,35 → 0,55 €/kWh nur im Stamm, Investition der Variante 1024 6.001 → 26.001 €), danach Bericht **ohne** manuelle Simulation | Word: Wärmebedarf Stamm 390 → **554** MWh/a, Wärmelast 209 → **230** kW, Netzbezug 116 → **156** MWh/a, Energiekosten 40.636 → **86.042** €/a, Nettobarwert −613.035 → **−1.288.569** €, Investition Test2 12.001 → **32.001** €, CO₂ 64,9 → **87,6** t/a. Excel-Rohwerte identisch (554.29 / 86042.5 / −1288569 / 32001). Nur der Stamm ändert sich — die Varianten haben eigene Datensätze. |
| **Mehrvarianten** — 3 Projekte gewählt | Alle drei mit `FrischSimuliert = true` und je eigener neuer `Tab_Ergebnis`-ID (172/173/174), je eigener Wirtschaftlichkeitszeile, alle persistierten Ergebnisse `ErgebnisAktuell = true`. |
| **Nie gerechnete Wirtschaftlichkeit** — `Tab_ErgebnisWirtschaftlichkeit`, `…WirtSensitivitaet`, `…StromMatrix` komplett geleert | Bericht enthält die vollständigen Kennzahlen (Werte identisch zum Vorlauf), kein „noch nie berechnet"-Hinweis. |
| **Fehlerfall** — `Extrapolation_erlaubt = FALSE` + Ergebnis der Variante 1023 gelöscht | Stamm und Variante 1024 vollständig; Variante 1023 mit Fehlertext in der Anhangtabelle („Fehler: Simulation fehlgeschlagen …") und zwei Zeilen unter „Hinweise dieses Berichtslaufs" (Simulation **und** Wirtschaftlichkeit unvollständig). Lauf läuft durch, Dateien werden erzeugt. |
| **Alter Direktbericht** (`ProjektvergleichBericht`) | Simuliert jetzt selbst: neue Ergebnis-IDs 180/181; die gescheiterte Variante steht mit Namen in den `Laufmeldungen`. |
| **Engine-Ergebnisneutralität** — Referenzlauf gegen `Referenzlaeufe/2026-08-15_B2` | **8 von 8 gerechneten Projekten PASS**, 190 Dateien, 2.094.447 Werte. Projekt **1010 fehlt**, weil es am 15.08.2026 gegen 22:50 aus der produktiven `Kenndaten.accdb` gelöscht wurde (mit 1016, 1020, 1025), während die Sitzung lief — reiner Datenstand, kein Codeeffekt: die Änderung fasst keine Datei unter `Allgemein/Simulation/`, `Allgemein/BhkwPlan.cs`, `Controller/` oder `Model/` an. |

Build: VS-MSBuild x86 im eigenen Arbeitsbaum (HEAD + geänderte Dateien, weil der
Haupt-Checkout parallele WIP enthielt) — **0 Fehler, 6 Bestandswarnungen**.

### 6.5 Katalog-Kandidaten (Lokalisierung)

Neue deutsche Texte liegen in der Berichts-Textquelle `Allgemein/Bericht/BerichtTexte.cs`
(nicht in `MyResource`). Zweisprachig hinterlegt sind:
„Wirtschaftlichkeit konnte für diesen Bericht nicht berechnet werden — …",
„⚠ Die Wirtschaftlichkeitsrechnung dieses Berichtslaufs ist fehlgeschlagen — …",
„Für diesen Bericht wurde jedes aufgeführte Projekt neu simuliert …".
Noch **nur deutsch** (dynamisch zusammengesetzt bzw. UI-Text, Übersetzung offen):
die Warnungstexte aus `BerichtsDatenSammler.RechneWirtschaftlichkeit` („Variante 'X':
Wirtschaftlichkeit unvollständig — …"), die Hinweistexte im
`WirtschaftlichkeitBaustein` mit eingebettetem Fehlergrund, die Dialogtexte in
`Form_Bericht` („Jeder Bericht rechnet neu: …", Aufwandsabfrage) sowie die
`Laufmeldungen` aus `ProjektvergleichBericht`.

### 6.6 Offene Punkte

- Der Reiter „Wirtschaftlichkeit" (`Form_Wirtschaftlichkeit.btnBerechnen_Click`) sammelt
  weiterhin mit `neuRechnen = false`: liegt ein **veraltetes** Simulationsergebnis vor,
  rechnet er die Wirtschaftlichkeit darauf. Für den Bericht ist das folgenlos (der
  rechnet ohnehin alles neu), für die Anzeige im Reiter nicht. Bewusst nicht mit
  geändert, um die Berichtsanforderung nicht mit einer Verhaltensänderung im Reiter zu
  vermischen.
- `BerichtsKonfiguration.NeuRechnen` bleibt als totes Feld im Konfigurations-JSON, damit
  gespeicherte Konfigurationen weiter lesbar sind; es wird nur noch auf `true` gesetzt.
- Referenzprojekt 1010 fehlt seit dem 15.08.2026 in der produktiven Datenbank. Die Basis
  `2026-08-15_B2` deckt damit nur noch acht Projekte ab — beim nächsten Basiswechsel
  entweder neu einfrieren oder ein Ersatzprojekt aufnehmen.

## 7. Einstiegspunkte in der Oberfläche (22.08.2026)

Die beiden zuletzt offenen „Designer-Handgriffe" sind erledigt — beide **ohne** Eingriff
in `MDIMainForm.Designer.cs`, `Form_Start.Designer.cs` oder deren `.resx`
(Projektkonvention, CLAUDE.md). Die Beschriftungen kommen deshalb aus `MyResource` und
nicht aus der Formular-Ressource; ein Sprachwechsel startet das Programm ohnehin neu.

| Einstieg | Umsetzung |
|---|---|
| **Wirtschaftlichkeit** | Bereits mit dem Umbau des Reiters „Berichte & Kosten" erledigt: `UcBerichteKosten` trägt die vier Seiten Übersicht / Kosten / **Wirtschaftlichkeit** / Bericht. Der geplante Einzelknopf in `Form_Start` entfällt damit; der alte Dialogweg (`Form_Variantentest`) bleibt als Rückfallnetz stehen, ist aber nicht mehr verdrahtet. |
| **Menü „Projekte"** | `MDIMainForm.BaueVariantenMenue()` hängt beim Start zwei Einträge an: „Als Variante speichern…" (`MENU_VARIANTE_SPEICHERN`) und „Varianten und Bericht…" (`MENU_VARIANTEN_BERICHT`). |
| **Als Variante speichern…** | Neuer Dialog `Views/Varianten/Form_AlsVariante.cs` — Oberfläche vollständig im Code (Muster `UcBkUebersicht`), keine Designer-/`.resx`-Datei. Fragt den Bezeichner ab und ruft `VariantenCtrl.AnlegenAusStamm` — **keine zweite Anlegelogik**. Ist das offene Projekt selbst eine Variante, wird ihr Stammprojekt verwendet. Danach zieht `Form_Start.VariantenAnzeigeAktualisieren()` Auswahlfeld und Reiter nach. |
| **Varianten und Bericht…** | `Form_Start.ZeigeBerichteKosten(UcBerichteKosten.SEITE_UEBERSICHT)` — wählt `tabPage6`, baut die Seite bei Bedarf auf (das `Selected`-Ereignis bleibt aus, wenn der Reiter schon vorne liegt) und stellt sie auf „Übersicht". |

Neue Ressourcenschlüssel in `MyResource` (de **und** en-US, `Resource.Designer.cs`
mitgepflegt): `MENU_VARIANTE_SPEICHERN`, `MENU_VARIANTEN_BERICHT`, `VAR_DLG_TITEL`,
`VAR_DLG_HINWEIS`, `VAR_MSG_KEIN_PROJEKT`. Wiederverwendet statt doppelt angelegt:
`BK_LBL_BEZEICHNER`, `BK_BTN_ANLEGEN`, `SIM_BTN_ABBRECHEN`, `BK_MSG_VARIANTE_ANGELEGT`,
`BK_MSG_ANLEGEN_FEHLGESCHLAGEN`, `BK_MSG_ANLEGEFEHLER`, `BK_MSG_KEIN_STAMM`.

**Verifikation:** VS-MSBuild x86 (Debug) — 0 Fehler, nur Bestandswarnungen; Build in ein
eigenes Ausgabeverzeichnis, weil die laufende Anwendung die DLLs im `bin` sperrt. Beide
Kulturen der kompilierten Ressourcen auf die fünf Schlüssel geprüft (5/5, Umlaute und
Anführungszeichen korrekt). **Ein Klicktest in der laufenden Anwendung steht noch aus.**
