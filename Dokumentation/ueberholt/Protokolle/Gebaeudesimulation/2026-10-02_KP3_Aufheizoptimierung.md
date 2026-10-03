# Protokoll KP3 — Aufheizoptimierung, Ergebnisse, Referenzprojekt und neue Basis der Konditionierungsprofile

**Stand 02.10.2026 · in Umsetzung (R1, D1, R2, O1, R3, R4, D2, R5 gebaut; die Basis heißt R34, weil R31, R32 und R33 am 02.10.2026 für die Rechenwegbefunde, die Solarthermie und die Viertelstunden vergeben wurden).** Grundlage: [Entwurf KP3](../../../aktuell/Gebaeudesimulation/2026-10-02_Entwurf_KP3.md)
(zwölf Wellen in vier Spuren, Festlegungen nach der Umsetzung als N1.69), Entscheid E58 (Leitkonzept N1.67, Teilkonzept 9.8),
Entscheide E59 und E60 (Leitkonzept N1.68, Teilkonzept 9.9),
[Protokoll KP2](2026-09-30_KP2_Konditionierung_Oberflaeche.md). Je Welle ein Agent im eigenen Worktree mit eigenem Gate
(`Werkzeuge/Gate/gate_linux.sh`), Merge durch die Orchestrierung, Gate über den gemeinsamen Stand, Statuszeile, Push.

## 1. Verfahren

Wie bei KP2: Auftrag als Datei, Agent im eigenen Worktree (`model: opus`), Abnahme mit vollem Gate im Worktree, danach
Merge → Gate im Hauptbaum → Statuszeile und Protokoll → Push → CI-Nachweis. Bis RP1 gilt die Basis R30 (16 Projekte,
487 CSV byte-gleich), ab RP2 die Basis R34 mit 17 Projekten.

**Zweiter Entscheid E59 (02.10.2026)** — nach E58 eine Vorgabe des Anwenders mit drei Rückfragen: die Rampe je Gebäude
(rechnet der Rechenweg schon), ein Aufschlag je Projekt in Stunden und Prozent (es gilt das Maximum), eine manuelle
Aufheizzeit je Gebäude mit Vorschlägen; Umsetzung nach D2, vor RP1. Die Papiere schrieb ein Agent im Worktree `kp3-e59`
(kein Code): Entwurf KP3 (Wellen R5 und O1b, Schritt KP-S4 mit Platzhalter 171, Festlegungen 34–43, F9, N-AH11 und
N-AH12, 16,75–21,75 PT), Teilkonzept 9.9, Leitkonzept N1.68 — die Festlegungen der Wellen werden damit **N1.69** —,
Register (P15–P17), Statusdateien, Glossar. **E60 (03.10.2026)** aus dem
[Konzept Heizlastspitzen](../../../aktuell/Gebaeudesimulation/2026-10-03_Konzept_Heizlastspitzen_Glaettung.md):
Auslegungsgröße stationäre Auslegungsheizlast plus P_auf, in O2 und O3 neben ideale Spitze und Tagesmittel gestellt, kein
Filter im Rechenweg; in denselben Papieren eingetragen. **Folgeentscheide am 03.10.2026:** P16 — der Aufschlag gilt nur
kalenderbezogen an Rampen mit n > 1; P17 (b) — Auslegungsgröße = Auslegungsheizlast + Aufheizzuschlag P_auf − Φ_stat
(Muster Φ_RH der DIN EN 12831-1); P15 (b) — Vorschlagsspanne aus τ₂ des Gebäudes,
[max(1, t_auf,max); min(47, ⌈τ₂ · ln 10⌉)] h; Schemaweg A1 — Ergebnisspalte `Aufheiz_Art` ohne Neubau von
`Tab_ErgebnisGebaeude`, `GEKOPPELT` an der Zone als Neubau von `Tab_ErgebnisZone`. Nachgetragen im selben Worktree.

## 2. Was gebaut ist

### R1 — Aufheizantwort, Stufenformel, Kappungsanteil

- **`Zonenmodell2K.Aufheizantwort(strahlungsanteil, zusatzleitwertWK)`**: zustandsfrei, eigenes geregeltes Fallsystem,
  berührt den Puffer `_heizen` nicht; ΔB und G_0 exakt aus dem linearen Term von `Konstante`; Speicher mit vier Plätzen
  (`AUFHEIZANTWORT_PLAETZE`) und bitgenauem Schlüssel (Anteil, Zusatzleitwert); `StationaererZustand(…)` als Startzustand
  der Nachweise. Typ `Aufheizantwort` (A, z, ΔB, v, G_0, H_s, τ₁/τ₂, r_k, C_k, C_w, `Bei`, `Stunde`, `Ausgang(M) = z·M·v`).
- **`Aufheizstufen`** mit `Aufheizform` und `Aufheizwahl`: `StundenmittelW`, `AugenblickW`, `AbsenkformW`,
  `AbsenkformGeschlossen`, `ErsteOrdnungMittel`, `ErsteOrdnungAugenblick`, `FormZurQuelle(bool quelleGrenze)` nach E58 F1 (b),
  `Waehlen(…)` (kleinstes n mit Φ − P_auf ≤ `Rechenrand.Zu(P_auf)`, n ≤ n_max, sonst „unerreichbar“ mit N = 0); jede
  Exponentialfunktion über `Uebergangsrechner.Bei`; kein Aufrufer im Lauf, keine Ressourcenschlüssel.
- **Kappungsanteil** (Befund B1, Festlegung 20): `Schritt` und `SchrittMitMuster` zählen die Zeit im Betriebsfall
  `Heizgrenze` in einem eigenen Akkumulator und schreiben ihn als `heizleistungMaxAnteil` auch in den idealen Rückweg; mit
  Übergabe bleibt der Ausdruck aus den Begrenzungsgründen; gelesen wird der Anteil im Lauf weiterhin nur mit Kopplung —
  keine Zahl ändert sich.
- **Tests:** `KappungsanteilTests` (4), `AufheizantwortTests` (N-AH0, innere Identitäten, Speicher; 7),
  `AufheizStufenformelTests` (N-AH1, N-AH2, N-AH4, N-AH5).

### D1 — Schemaschritte 160 und 161, Datenweg

- **Schritt 160 `AufheizvorgabeSchema`** (fünf Spalten an `Tab_Einstellungen`, 32 → 37) und **Schritt 161
  `AufheizErgebnisSchema`** (14 Spalten an `Tab_ErgebnisGebaeude`, 26 → 40, und `Tab_ErgebnisZone`, 15 → 29, dort dazu
  `Sommerlueftungsstunden_H`); wiederholbar je Spalte, CHECKs beim `ADD COLUMN`, kein DML, keine neue Tabelle (153 STRICT),
  die Stempeltrigger aus Schritt 159 unberührt; Wertlisten `DbWerte.AUFHEIZ_*`; eingehängt in `Paketanhebung` (zwei
  DDL-Stufen), `SchemaMigration` der Schale, `Werkzeuge/Testdatenbankschema`, `TestDatenbank.cs`. `SchemaStand.Zielversion`
  = `AufheizErgebnisSchema.SCHRITT`. Nummern gegen origin geprüft (02.10.2026, 08:21 UTC und nach dem Gate: dort 159).
- **Testdatenbank** von 159 auf 161 gehoben (33 Spalten): 81 170 432 Byte, LFS-SHA-256
  `117f44f96b4530540348202ecb9cbd123128953ff0d7af0e77abb8cded9fe3dd`; einziger Wertunterschied `Tab_Applikation.SchemaVersion`
  159 → 161; `integrity_check` ok; Nachtrag in `Referenzlaeufe/LIESMICH.md`. Die Basis R30 bleibt — der Referenzlauf liest
  die Ergebnistabellen nicht, der Schalter steht überall auf 0.
- **Datenweg:** Record `Aufheizvorgabe` (`EPOS.Kern/Model/`, Normalisierung nach Festlegung 24, wirksame Werte (a), 2 K,
  ρ 0,2, täglich), `KonfigurationCtrl.AufheizvorgabeLesen` (fehlende Zeile oder Spalte = aus) und `-Schreiben` (ein
  `UPDATE`), Naht `SimulationWaermebedarf.AufheizvorgabeProjekt` (in `KlimakalenderLesen` zurückgesetzt, ohne Aufrufer im
  Rechenweg); je 14 Felder in `ErgebnisGebaeudeModel`/`ErgebnisZoneModel`, `ErgebnisCtrl` schreibt und liest nach
  Vorhandensein, die Zonenzeile aus einer gemeinsamen Spaltenliste (B23); `SimulationKonfigHuelle.Speichern` reicht die
  Aufheizvorgabe beim Neuanlegen der Einstellungszeile nach (wie Kühlbetrieb und Anlagenkopplung).
- **Tests:** `AufheizSchemaTests` (9), `AufheizvorgabeTests` (10), `ErgebnisAufheizTests` (6); Spaltenzahl-Konstanten
  (B24) auf `AufheizErgebnisSchema.SPALTENZAHL_*`; `Auslieferungsvorlage.Tests` 44/44; `SqlDialektPruefer` 2 135 Texte,
  0 Fundstellen.

### R2 — Aufheizplan einer Zone, Einbau in den Einzonenlauf

- **`Aufheizplan` und `Aufheizoptimierung`** (reine Funktionen): Sprünge (Festlegung 7), Absenkdauer D, Ring über den
  Jahreswechsel, Fenster des Tages und T_a (Festlegung 9), θ_N nach E58 F2 (b), Φ_stat und Antwort mit `AequivalentN`
  (Außenform, Mitglied von `GebaeudeModellEingang`), P_auf nach Quelle (Grenze → Augenblick, Ziel → Stundenmittel, E58 F1 (b)),
  Bemessungsfall je Variante, täglich/fest, Deckel 48, Rampe nach Festlegung 8 mit Kühlkappung, Zähler W1/W2/W4/W5,
  Rampenmaske; `HeizsollwertMitRampeSetzen` als einziger Schreibweg nach dem Bauen.
- **Einbau** in `Vdi6007Rechenweg.Rechnen` über `ZonenEingang.Einzeln` (Festlegung 1), Eigenschaft `Aufheizvorgabe` (Vorgabe aus),
  Testnaht `AufheizleistungTestW`, `LetzterAufheizplan` für R4; die eine Zeile in `SimulationWaermebedarf` neben dem Kühlschalter.
  Schalter aus = kein Aufruf: Referenzlauf byte-gleich.
- **Tests:** `AufheizRaenderTests` (N-AH6, N-AH10 ohne Zonen, je gegen die Vorschrift der Festlegung 8 nachgerechnet),
  `AufheizGrenzfallTests` (N-AH8: 17 Einzonengebäude der 16 Referenzprojekte, 5 840 Sprünge mit n = 1, bitgleich zu „aus“),
  `AufheizLaufTests` (Projekt 1018, Gebäude 10632: t_auf,max 5 h (a) / 9 h (b), T_a,B −9,3 °C, P_auf 136,9 kW Ziel, 19 Rampentage,
  Σ 34 h, längste Rampe 5 h, Spitze 37,36 → 32,12 kW, Jahresheizwärme 68,252 → 68,289 MWh, Nachweisband 4 → 0 Überschreitungen,
  zwei Läufe bitgleich), `AufheizStufenformelTests` um „fest ≥ täglich“ (75 600 Gitterpunkte).

### O1 — Projekteinstellung „Aufheizoptimierung“

- **Abschnitt** `section.epos-simkonfig-aufheizung` in „Weitere Einstellungen“ nach der Anlagenkopplung: Schalter; bei an
  Bemessung (a)/(b), ΔT_K nur bei (b) (0–10 K, Vorgabe 2), Reserve ρ 1–100 % (Vorgabe 20, als Anteil gespeichert, Hinweis zu
  E58 F7 (c)), Art täglich/fest, Zeile zu Bemessung und Art, bei AK1 eine Zeile zu W5, Herleitungszeilen je Gebäude nur mit
  Delegat (D2). Jedes Feld schreibt sofort die ganze Einstellung, normalisiert über den Record (Festlegung 24); Fehlschlag baut
  den Abschnitt mit dem alten Stand neu auf und meldet.
- **Schreibweg** `KonfigurationCtrl.AufheizvorgabeSetzen` mit Vormerksatz (Muster `KuehlbetriebSetzen`; „aus und leer“ ohne
  Einstellungszeile wird nicht geschrieben); `ParameterDaten.Aufheizung`, `SimulationParameterDienste.AufheizvorgabeSchreiben`,
  Delegat `AufheizHerleitung`, Naht in `SimulationErgebnisHuelle.ParameterGaben` (dort hängen auch Kühlschalter und Kopplung).
- **Assistent:** fünf KI-Felder (`aufheizoptimierung`, `aufheiz_bemessung`, `aufheiz_abzug`, `aufheiz_reserve`, `aufheiz_art`),
  benannte Absagen (Werte bei Schalter aus, ΔT_K bei (a), Grenzen, Fehlschlag), Aktionswissen „Aufheizoptimierung einstellen“;
  `KiMaskenabdeckungWacheTests` 4 → 9, `KiSimulationMaskeTests` 48 → 53 Felder.
- **Texte:** 25 Schlüssel (`SIMKONF_AUFH_*` 17, `KI_DLG_SIM_AUFH_*` 8) in beiden Sprachen, Glossar (Bemessung → design basis,
  Abzug ΔT_K → deduction ΔT_K, Art der Aufheizzeit → preheat time mode, Herleitungszeile → derivation line), eine Zeile in
  `help_mapping.txt` (Anker `#aufheizoptimierung` folgt mit dem Wiki in KP4).
- **Tests:** `AufheizvorgabeSetzenTests` (5), 12 bunit-Fälle, KI-Tests.

### R3 — Aufheizplanung im Mehrzonenweg

- **Nachbarform (B4):** `GebaeudeModellEingang.AequivalentN(tag, aussen, nachbarC)` nimmt den Zähler der Außenglieder der Außenform
  und hängt Σ U·A_j·θ_j in der Reihenfolge von `ZonenEingang.ThetaEq` an; die Außenform mit zwei Argumenten lehnt Nachbarglieder
  weiter benannt ab. `ZonenEingang.AequivalentN(tag, aussen, thetaAir)` und `ZuluftN` (derselbe Ausdruck wie `ThetaLue`); Φ_stat
  mit der Nachbarform und θ_Lue an der Stelle der Außenluft. Feste Nachbarn im Sprung: beheizte bei s_k(h_s − 1) ohne Rampe, „aus“
  oder unbeheizte beim Startwert nach N1.56 Nr. 7; in Bemessung und Zielleistung beheizte bei ihrem θ_T,max.
- **Zonenzustände (Festlegung 25, B5):** `Aufheizzone.AusZonen` (`Beheizt`, `AequivalentNachbarn`, `Zuluft`, `NachbarnImSprung`,
  `NachbarnInDerBemessung`); unbeheizt → UNBEHEIZT ohne Rampe; GEKOPPELT bei `KopplungWirksam` oder im Mehrzonenweg bei
  `KopplungAlsIdealeLast` mit wirksamer Heizseite; θ_T,max aus der eigenen Reihe (`Aufheizoptimierung.ThetaTMax`).
- **Einbau (Festlegung 1):** `ZonenEingang.Bauen(…, aufheizvorgabe, aufheizleistungTestW)` ruft am Ende
  `Aufheizoptimierung.AnwendenZonen` — erst alle Zonen planen, dann alle Rampen schreiben — in beiden Aufbauten (adiabater
  Vorlauf der 4-K-Regel, gekoppelter Lauf); Schalter aus = kein Aufruf. Plan je Zone an `ZonenEingang.Aufheizplan`, Gebäude über
  `ZonenEingang.Gebaeudezonen`; `Zonenrechnung.Rechnen` und `Vdi6007Rechenweg.RechnenMehrzonen` reichen Vorgabe und Testnaht
  durch; Gebäudewerte in `Mehrzonenergebnis.Aufheizgebaeude`; `LetzterAufheizplan` bleibt im Mehrzonenweg `null`.
  `Zonenschleife.StartwerteRechnen`/`MitStartsollwert` sind rein und statisch (eine Quelle für Vorlauf und Planung).
- **Gebäudewerte (Festlegung 22):** Datensatz `Aufheizgebaeude` aus `Aufheizoptimierung.Gebaeudewerte` als Aggregator für R4;
  `Aufheizplan` dazu `SprungstundenAus`, `SprungstundenAusHeizperiode`, `Kuehlkappmaske`, `Spruenge` mit Stundenlisten;
  `HeizleistungMaxStundenH` = Σ_h max_z Anteil (die Anteile liefert R4).
- **Tests:** `AufheizMehrzonenTests` (N-AH9), `AufheizGrenzfallTests` (N-AH8 mit Zonen), `AufheizRaenderTests` (N-AH10 mit Zonen).
- Commits `efdd9544` (Nachbarform, Zustände), `8fef938e` (N-AH9), `420cdcf7` (N-AH10), `746b7b9e` (N-AH8); Gate 664 im Worktree.

### R4 — Lauf, Ergebnis, Laufhinweise

- **Kappungsreihe (Festlegung 20, B7):** beide Jahresschleifen (`Vdi6007Rechenweg.Laufen`, `Zonenlauf`) schreiben den Kappungsanteil
  je Stunde in einen eigenen Akkumulator, Zeile für Zeile gleich (Orakel `ZonenEingangTests` erweitert); neue Felder
  `HeizleistungMaxAnteil` und `HeizleistungMaxStundenH` am Ergebnis, keine bestehende Summe ändert sich; `Laufen` mit optionalem
  `aufheizplan`; `HeizkreisErgebnis` unberührt (gekoppelt ist der Akkumulator bitgleich zum Heizkreis, Test an 1047).
- **W3 (Festlegung 19):** `Aufheizoptimierung.Nachweisbandtage` zählt im Fenster [h_s − n + 1, h_s + 2], unten über den Jahresring,
  oben bis Stunde 8 759, Vergleich über `Rechenrand`, nur an Tagen ohne W1/W2.
- **`Aufheizergebnis`** als Record in `GebaeudeModellErgebnis.cs` mit den Feldnamen von `ErgebnisGebaeudeModel` (D2 übernimmt eins
  zu eins), dazu Unterzahlen, Maske, W3-Tage; `Bilden(plan, …)` je Einzone/Zone, `Gebaeude(Aufheizgebaeude, Zonen)` am
  Mehrzonengebäude (in `Zonenrechnung` vor `Gebaeudeergebnis` gebildet und übergeben); `Skaliert` multipliziert nur P_auf;
  NULL-Regeln nach Festlegung 25; keine Datenbank.
- **Nutzungszeit (Festlegung 10, B10):** `NutzungBei` = Bestandsausdruck ∧ ¬Rampenmaske; `Skaliert` und die Zonen tragen die Maske,
  das Mehrzonengebäude die Vereinigung; Untertemperatur-Hinweis unverändert.
- **Laufhinweise (Festlegung 21):** `Vdi6007Rechenweg.HinweisAufheizung` über `HinweisEinmal`, einmal je Gebäude aus `Melden` und
  `MeldenMehrzonen`, nicht im Probelauf; sechs Schlüssel je Sprache (`SIMENG_AUFH_W1` … `W5`, `SIMENG_AUFH_W2_BEMESSUNG`).
- **Tests:** `AufheizNachweisbandTests` (N-AH3), `AufheizDeterminismusTests` (N-AH7 Lauf, gestörter Lauf in einer zweiten Ladekopie
  über `AssemblyLoadContext`), `AufheizNutzungszeitTests`, `AufheizHinweisTests`, Skalierungstests, `ZonenEingangTests` erweitert.
- Commits `a302b3de` (Kappungsreihe, W3, Ergebnis, Maske, Hinweise), `0a3dd371` (N-AH3), `49689037` (Orakel), `50dcd756`
  (Nutzungszeit, Skalierung), `15d06d3f` (Hinweise), `389d56cf` (N-AH7), `2acc65ba` (BOM); Gate 669 im Worktree.

### D2 — Kennzahlen, Export, Auskunft

- **Kennzahlen:** `GebaeudeKennzahlen.Aufheizwerte` (Record `Aufheizkennzahlen`) ist die eine Stelle der NULL-Regeln; Gebäude- und Zonenzeile
  tragen alle 14 Aufheizspalten (Festlegung 25) und `Sommerlueftungsstunden_H` NULL ohne Sommerlüftung (Festlegung 26, auch je Zone;
  `ErgebnisGebaeudeTests:260`, `GebaeudeBedarfNachtauskuehlungTests:98` nachgezogen). P_auf nur endlich und > 0 (Testnaht +∞ und
  `Heizleistung_Max` = 0 → NULL, auch im Export ohne Schlüssel); Rundreise über `ErgebnisCtrl` je Gebäude und Zone.
- **Export nach E32 (Festlegungen 27, 28):** Texte `Aufheizzustand`, `Aufheizbemessung`, `Aufheizleistungsquelle` hinter `Geb[n].Modell`,
  elf Zahlen nur bei Zustand, Zonenschlüssel `Geb[n].Zone[k].*` (Aufheiz-, Nachtauskühl-, Sommerlüftungswerte), `heizsollwert_<n>.csv` bei
  Heizkalender oder Aufheizung ≠ null (auch GEKOPPELT), Nachtauskühlung nur gesetzt — in den 16 Referenzprojekten unverändert.
- **Bedarfsauskunft:** `GebaeudeBedarfErgebnis.Ergebniszeile` und `GebaeudeBedarfZone.Ergebniszeile` statt Einzelfelder, aus derselben Stelle
  wie der Lauf (O2 stellt dar).
- **Auskunft der Aufheizbemessung ohne Jahreslauf** (Grundsatz 3, B14): baut mit dem Konditionierungssatz wie der Lauf, ruft `Bemessen`; Faktor
  Flächenangabe wie der Lauf, Gebäude mit Zone 1, Verbrauchsangabe `null` („erst der Lauf“); Mehrzonen mit Regelpaaren brauchen den adiabaten
  Vorlauf (je Zone ein Jahr). **Herleitungszeile** in `SimulationErgebnisHuelle.ParameterGaben` (`AufheizHerleitung`), de/en, auch für
  Tagesbilanz-Gebäude und Fehler des Eingangsbauers, ohne C_w; elf Schlüssel `SIMKONF_AUFH_HRL_*`/`SIMKONF_AUFH_QUELLE_*`.
- **Hinweis `SIMENG_AUFH_VERBRAUCH`** (B11) einmal je Gebäude mit Verbrauchsangabe, nur bei geplantem Zustand mit Rampentag.
- **Außerhalb der Spurenregel, bestätigt:** Kennzeichen `SommerlueftungGesetzt` und `HeizkalenderWirksam` als init-Eigenschaften in
  `GebaeudeModellErgebnis` (gesetzt in `Vdi6007Rechenweg.Laufen`, `Zonenlauf.Ergebnis`, `Zonenrechnung.Gebaeudeergebnis`, von `Skaliert`
  getragen); Auslagerungen `Vdi6007Rechenweg.EingangBauen`/`ZonenBauen`/`Mehrzonenweg`/`Zonenklima`/`Zonenkonditionierung` und
  `Zonenrechnung.ZonenBauen`, die der Lauf selbst ruft (Regel „Auskunft ruft den Rechenweg“), Referenzlauf byte-gleich.
- **Tests:** `AufheizExportTests`, `AufheizAuskunftTests`, `AufheizDeterminismusTests` (N-AH7 Zeile/Export), `GebaeudeBedarfCtrlTests`,
  `ErgebnisGebaeudeTests`, Hüllentest der Herleitungszeile (16 Fälle, beide Kulturen), Hinweistest.
- Commits `ed7bf075` (Ergebniszeile), `33ea28be` (Export), `bdd08341` (Auskunft, Herleitungszeile), `7fb53e94` (Hinweis), `5ac9377e` (N-AH7);
  Gate 670 im Worktree.

### R5 — Aufschlag und manuelle Aufheizzeit (E59), Schemaschritt 174

- **Schema KP-S4** `AufheizManuellSchema` (`SCHRITT = KatalogfassungStufe2Schema.SCHRITT + 1` = 174): `Tab_Einstellungen.Aufheiz_Aufschlag_H` (INTEGER 0–24) und
  `Aufheiz_Aufschlag_Prozent` (REAL 0–100), `Tab_Gebaeude.Aufheizzeit_Manuell_H` (INTEGER 1–47, nur Projektgebäude), `Aufheiz_Art` TEXT
  TAEGLICH/FEST/MANUELL per ADD COLUMN an `Tab_ErgebnisGebaeude` und `Tab_ErgebnisZone`, E60-Spalten `Auslegungsheizlast_Kw` und `Aufheizzuschlag_Kw`,
  GEKOPPELT im CHECK von `Tab_ErgebnisZone` (kleiner Neubau), `Abfrage_Projektgebaeude` neu (103 Spalten); `DbWerte`, `SchemaStand`, `Paketanhebung`,
  Schalen-Migration, `Werkzeuge/Testdatenbankschema`, `TestDatenbank.cs`; Testdatenbank 174 (81 412 096 Byte, LFS `e85c3bdb…`), LIESMICH-Nachtrag.
- **Record `Aufheizvorgabe`:** `AufschlagH`/`AufschlagProzent` (0 und leer → NULL), `KonfigurationCtrl` Lesen/Schreiben nach Vorhandensein der Spalten.
- **Rechenweg:** n' = min(48, n + max(AufschlagH, ⌈n·p/100⌉)) nur auf Rampen, die ein Kalendersprung auslöst (n > 1; P16), Aufrundung über `Rechenrand.Zu`;
  Begrenzung D + 1, W2 zählt mit n'; manuelle Zeit t: n = t + 1 an jedem Sprung, Fenstergrenze W = min(D, t + 1), Bemessung läuft weiter (Herleitung),
  Aufschlag wirkt nicht auf t; Zonen erben; Katalogkopie NULL, Duplikat und Variante kopieren; Eingangsbauer lehnt t außerhalb 1–47 benannt ab.
- **Ergebnis, Kennzahlen, Export, Auskunft:** `Aufheiz_Art` (NULL bei GEKOPPELT/UNBEHEIZT), Zuschlag = max(0, P_auf − Φ_stat(θ_T,max, T_a,B)), bei W1 0,
  skaliert wie P_auf, Mehrzonen als Summe; Φ_HL aus `GebaeudeModellEingang.AuslegungsheizlastW` (NULL ohne Übergabe); Zonenzeile schreibt GEKOPPELT;
  Export `Geb[n].Aufheizart` (nur Gebäude) und `Geb[n].Aufheizzeit_Manuell` nur gesetzt, E60-Werte nicht im Export (kein Schlüssel im Papier);
  `Aufheizauskunft` mit `Art`, `AufheizzeitManuellH`, `Tau2H`; `GebaeudeExportVerluste` stuft `Aufheizzeit_Manuell_H` als benannten Verlust ein.
- **Tests:** `AufheizManuellSchemaTests` (Kette, Reihenfolgeanker), `AufheizvorgabeTests`, `AufheizAufschlagTests` (N-AH11), `AufheizManuellTests` (N-AH12),
  neun Bestandstests mit Spaltenzahlen nachgezogen; `AnlagenkopplungDialogKernTests` führt das Projektfeld.
- Commits `a894c495`, `34ec3418` (173, vor dem Umhängen), `b03bf720`, `6030b1a8`, `f5c8bb42`, Merge `863419b2`, `3107548f` (Testdatenbank 174); Gate 682 und 686.

## 3. Schemaschritte

| Schritt | Klasse | Inhalt | Testdatenbank |
|---|---|---|---|
| 160 | `AufheizvorgabeSchema` | `Aufheizoptimierung`, `Aufheiz_Bemessung`, `Aufheiz_Abzug_K`, `Aufheiz_Reserve`, `Aufheiz_Art` an `Tab_Einstellungen` | 161 (`117f44f9`) |
| 161 | `AufheizErgebnisSchema` | 14 Ergebnisspalten je Gebäude und Zone, `Sommerlueftungsstunden_H` an der Zone | 161 |
| 174 | `AufheizManuellSchema` | Aufschläge am Projekt, manuelle Aufheizzeit am Gebäude, `Aufheiz_Art` und E60-Spalten im Ergebnis, GEKOPPELT an der Zone, Sicht neu | 174 (`3107548f`) |

## 4. Befunde der Umsetzung

- **Druckstellen N-AH0 treffen alle** (Prüfsatz, Haus aus 1045): H_s 971,76 W/K, G_0 2 425,12 W/K, τ 1,0168/4,5395 h,
  C_w 5,781/6,931 kWh/K, Φ_stat 32,07 kW, Spitze ohne Rampe 37,04 kW; bei P = 35,3 kW n = 5 im Mittel (35 217 W; Stundenbeginn
  35 662 W), n = 7 im Augenblick (n = 6: 35 335 W); a = 0,3: Φ_stat 34,42 kW, n = 32 im Mittel. C₁/C₂ stehen im Teilkonzept nur
  auf zwei Stellen und werden auf die halbe letzte Stelle geprüft.
- **Neue Zahl:** mit a = 0,3 braucht die Augenblicksform bei 35,3 kW **n = 37** (35 286 W; H_s 1 042,9 W/K, G_0 3 155,1 W/K,
  τ 0,982/3,878 h) — mit Quelle Grenze (F1 (b)) läge der Prüfsatz über dem bisher genannten n = 32; nachzutragen in
  Teilkonzept 4.3 mit dem Abschluss.
- Bei a > 0 koppelt nicht nur G_rad die Massen, sondern auch die auf beide Oberflächen verteilte Leistung; zusammenfallende
  Eigenwerte entstehen nur bei G_rad = a·w_AW·G_c,IW/(1 − a) mit angeglichener Diagonale — so sind die synthetischen Sätze gebaut.
- **Variante und Duplikat nehmen keine Ergebnisse mit** (Bestand, `ProjektDuplizierenCtrl.IstErgebnisTabelle`, Entscheid
  23.09.2026): die Projekteinstellung wandert mit Duplikat, Variante und Transfer, die Ergebnisse bleiben beim Quellprojekt;
  nur der `.wpx`-Transfer trägt die Ergebnisspalten.
- Eine Stunde nur mit Kühlübergabe trägt den Kappungsanteil aus dem eigenen Akkumulator (bisher 0, dort nicht gelesen).

- **Sprungstunde und Überlappung (R2):** Bei gestufter Absenkung hebt die Rampe des späteren Sprungs die Sprungstunde des
  früheren über das Maximum; „die Sprungstunde nie“ (Festlegung 8) gilt je eigener Rampe.
- **ρ-Probe mit dem echten Kern (R2, (a), ρ 20 %, täglich):** t_auf,max / Rampentage — 1007: 1 h / 1; 1008 (10576): 5 h / 3;
  1017: 5 h / 5; 1018 und 1049: 5 h / 19; 1023, 1024, 1039 (10644), 1050: 2 h / 1; übrige 1039, 1041, 1042, 1045: 0 / 0. Mit (b)
  steigt t_auf,max bis 9 h, die Rampentage kaum — Grundlage für RP1 und den Entscheid P14.
- **Naht der Oberfläche (O1):** liegt in `SimulationErgebnisHuelle.ParameterGaben`, nicht in `SimulationKonfigHuelle`; der
  Wiki-Anker `#aufheizoptimierung` fehlt noch (Hilfepille zeigt auf die Seite); eine weiche Sperre hat keinen Fall, weil die
  abhängigen Felder ausgeblendet sind — der Sperrzustand der Seite gilt wie bei den Nachbarn.

- **`ZonenschleifeProbenTests.Synthetisch(2)` (R3)** baut 0 Wohnungen (f = 1/0) und damit keine beheizte Zone; N-AH9 nutzt einen
  eigenen Bauer nach demselben Muster. **Die Testdatenbank führt keine Zonen** (`Tab_Zone` leer): N-AH8 mit Zonen geht über die
  Datenbank leer aus und stützt sich auf eine Mehrzonenfassung jedes VDI-Gebäudes (halbiert mit Trennwand und Luftstrom, dazu ein
  unbeheizter Nebenraum) — bis G6d echte Zonenkalender säht.
- **Startwertregel (R3):** Der Bezug „Muster `:1477-1482`“ im Entwurf zeigt auf die Auslegungsheizlast der Kopplung; die
  Startwertregel steht in `Zonenschleife` und `StartwerteUnbeheiztC`. **AK1 im Mehrzonenweg:** `KopplungWirksam` ist dort immer
  `false` (A4 (a), ideale Last) — ohne die eigene Regel (Abschnitt 5) griffe GEKOPPELT nie.
- **N-AH9-Band (R3):** Der Fall „täglich (a) mit Wochenende“ ist der bindende — größte Stundenleistung 100,44 % P_auf (Band
  1,01·P_auf); ohne Planung läge die Sprungstunde bei 121,22 %. Φ_stat mit Nachbarn 11 683 W / 11 162 W gegen 12 483 W / 11 992 W
  mit Nachbarn bei 0 °C (B4).

- **B11 sichtbar (R4):** Im Klassenweg wirkt `Heizleistung_Max` ungeskaliert auf den Katalogbau; ein für das wirkliche Gebäude
  eingetragener Wert wird dort viel zu knapp (0,6 · P_auf · Faktor ergab 359 W1-Tage), und mit Quelle Grenze weist das Ergebnis
  P_auf = `Heizleistung_Max` · Faktor aus, nicht die Eingabe — die Herleitungszeile (D2) nennt bei Faktor ≠ 1 Eingabe und Faktor;
  der Hinweis „Schalter wirkt an einem Gebäude mit Verbrauchsangabe“ gehört in `SimulationWaermebedarf` (D2).
- **W3 schlägt am Bürogebäude nie an (R4),** auch nicht bei Grenze 1,02·Φ_stat und ρ 2 %: die Formel liegt wegen Sonne und
  Gewinnen auf der sicheren Seite; erst ein Kälteeinbruch in den zwei Stunden nach dem Sprung löst W3 aus (sechs Einbruchstage).

- **GEKOPPELT an der Zone (D2):** `Tab_ErgebnisZone.Aufheiz_Zustand` kennt GEKOPPELT laut CHECK nicht, obwohl R3 eine Zone im
  Mehrzonenweg (AK1 als ideale Last, 1047) so setzt; D2 hält die Zonenzeile ohne Aufheizwerte, der Zustand steht am Gebäude —
  Schemanachtrag mit dem E59-Schritt (R5). **B14 bleibt für die Übergabe-Auskunft (H10) offen:** `UebergabeEingang`/`KuehluebergabeEingang`
  bauen ohne Konditionierungssatz. **B11:** mit Verbrauchsangabe bleibt der Faktor bis zum Lauf offen.

- **O1b-Datenverlust (R5):** `SimulationKonfigSeite.razor` baut die `Aufheizvorgabe` bei jeder Feldänderung mit fünf Argumenten neu; ein gespeicherter
  Aufschlag fiele still auf NULL zurück — O1b reicht `AufschlagH`/`AufschlagProzent` in allen Settern und in `SimulationKiSicht` durch. **O2:**
  `GebaeudeStammCtrl.SET_SPALTEN` führt `Aufheizzeit_Manuell_H` nicht (Setzweg fehlt). **Auslegungsheizlast ohne Übergabe NULL** (O2/O3).
  `BETRIEB_SQLITE.md` 6.5 nennt noch 172 (Welle A).

## 5. Festlegungen der Wellen (für N1.69)

- R1: Index 1 der schnelle, Index 2 der langsame Modus (Teilkonzept 4.2); im zusammenfallenden Zweig r_k, C_k NaN, τ₁ = τ₂ =
  −1/μ, C_w endlich; erste Ordnung als `double` (mindestens 1, +∞ bei P ≤ Φ_stat); unerreichbar = N = 0 mit der Leistung bei
  n_max; `AbsenkformGeschlossen` vergleicht Φ_stat(θ_T) − G_0·ΔT > `Rechenrand.Zu(0)`; `FormZurQuelle(bool)` statt eines
  Quellen-Enums; Nachweise mit dem Sprung 17 → 21 °C.
- D1: `ErgebnisCtrl` schreibt das Modell unverändert, die NULL-Regeln der Festlegung 25 setzt D2 in `GebaeudeKennzahlen`;
  ungültige Werte lässt der Record stehen, `AufheizvorgabeSchreiben` scheitert dann an der Spalte (`false`, Zeile unverändert);
  keine Ressourcenschlüssel in D1.

- R2: Bemessungsfenster θ_N in min(D, 48), im Lauf min(D, t_auf,max + 1); W2 täglich nur bei erreichbarem Bedarf (W1 hat
  Vorrang), fest bei t_auf,max + 1 > D + 1, Wache `TageBemessungBegrenzt` = 0; Tage nach dem Tag der Sprungstunde, Rampentag =
  n > 1, `AufheizstundenH` = Σ (n − 1) neben `MaskenstundenH`, W4 je Übergang aus NaN mit Unterzahl Heizperiode; Randvergleiche
  über `Rechenrand` (`Zu(θ_T)`, `SchwelleErreicht`), Kühlkappe notfalls um ein ulp gesenkt; θ_T,max ohne Nutzungsstunde =
  höchster endlicher Sollwert, Erdreich der Zielleistung am Tag der kältesten Stunde, `Heizleistung_Max` nur wenn endlich;
  `AequivalentN` mit Nachbargliedern und `Aufheizzone.Aus` an gekoppelten Zonen lehnen benannt ab (R3).
- O1: die Naht übergibt den Record als Ganzes; Reserve 1–100 % in Oberfläche und KI (Datenbank > 0); Zeile zu W5 bei AK1;
  Glossarbegriffe wie oben.

- R3: GEKOPPELT im Mehrzonenweg bei `KopplungAlsIdealeLast` und wirksamer Heizseite nach `Waermeuebergabe.KopplungWirksamFuer`
  (die Konditionierung behandelt die Zonen dann als gekoppelt; N-AH10 verlangt W5); feste Nachbarwerte aus den Reihen ohne Rampe,
  gelesen vor jeder Rampe, Startwert = Vorlaufwert aus `StartwerteRechnen` einmal je Gebäude, ein beheizter Nachbar mit endlichem
  Sollwert am Vorlaufbeginn und „aus“ im Sprung bekommt einen Jacobi-Schritt von den Startwerten; Gebäudezustand GEKOPPELT nur,
  wenn alle beheizten Zonen gekoppelt sind, sonst UNERREICHBAR, sobald eine geplante Zone es ist (t_auf,max `null`), sonst
  BEMESSEN; P_auf Summe und T_a,B Minimum ohne NaN; `AufheizstundenH` des Gebäudes = Vereinigung der Rampenfenster
  (h_s − n + 1 … h_s − 1, bei Überlappung weniger als Σ (n − 1)) neben vereinigter Maske und `MaskenstundenH`; W4 vereinigt über
  die Übergangsstunden, Tageszähler aus den Sprunglisten; Startwerte aus `Zonenschleife` herausgezogen, nicht abgeschrieben.

- R4: W3 zählt jeden Sprung, auch mit n = 1, unten über den Ring, oben bis 8 759; W3 am Gebäude = Vereinigung der W3-Tage der
  Zonen, jede Zone schließt nur ihre eigenen W1/W2-Tage aus; GEKOPPELT und UNBEHEIZT tragen nur Zustand und `HeizleistungMax_H`;
  P_auf NaN wird `null`, +∞ (Testnaht) bleibt +∞; Gebäude im Mehrzonenweg: Mittel und Überhitzung ohne die vereinigte Rampenmaske,
  `ZonenAnhaengen` unverändert; W1 meldet auch bei unerreichbarer Bemessung ohne W1-Tag (Zahl 0); die Hinweise nennen Zahlen ohne
  kW, weil sie vor der Skalierung entstehen; `AufheizBemessung` steht auch an der Zone, D2 übernimmt es nur am Gebäude.

- D2: P_auf in Spalte und Export nur endlich und > 0, die Auskunft behält +∞ roh; NULL-Regeln allein in `GebaeudeKennzahlen.Aufheizwerte`;
  Export „vollständig“ schließt die Texte ein, Zonenschlüssel auch für Nachtauskühlung und Sommerlüftung; `heizsollwert_<n>.csv` bei
  Heizkalender oder Aufheizung ≠ null; Faktor der Auskunft (Fläche wie der Lauf, Zone 1, Verbrauch `null`); feste Nennleistung geht nicht
  ein; C_w nicht in der Herleitungszeile; Hinweis Verbrauchsangabe nur bei geplantem Zustand mit Rampentag; `AufheizBemessung` nur am Gebäude.

- R5: manuelle Zeit ersetzt auch die Fenstergrenze W = min(D, t + 1); Aufrundung ⌈n·p/100⌉ trägt `Rechenrand.Zu`; `Aufheiz_Art` NULL bei GEKOPPELT und
  UNBEHEIZT; Export `Aufheizart` nur am Gebäude; E60-Werte nicht im Export; Auskunft behält Zustand und t_auf,max der Bemessung, die Ergebniszeile hat bei
  MANUELL Zustand BEMESSEN mit t; Eingangsbauer lehnt t außerhalb 1–47 ab; Sicht `Abfrage_Projektgebaeude` neu gebaut (Festlegung 43), `Tab_ErgebnisGebaeude` nicht.

## 6. Nachweise

| Nachweis | Ergebnis |
|---|---|
| N-AH1 (R1) | 420 Fälle × n = 1 … 24 (29 Projektgebäude der Testdatenbank, sechs synthetische Sätze), größte relative Abweichung 2,98e-15; Kappungsschärfe: Grenze Φ̂·(1 + 1e-9) kappt nicht, Φ̂·(1 − 1e-6) kappt die Sprungstunde; zwölf Fälle im zusammenfallenden Zweig |
| N-AH2 (R1) | 370 Fälle, 33 670 Rampen, Abweichung 2,87e-15; nie über der Gleichgewichtsform; 50 Fälle ohne Vorbedingung ausgelassen |
| N-AH4 (R1) | 15 120 Proben je Form, keine Ausnahme (9 386 mit n_F = n) |
| N-AH5 (R1) | 49 000 Gitterpunkte, keine Ausnahme; (b) ≥ (a) mit ΔT_K 0 … 10 K; „fest ≥ täglich“ folgt mit R2 |
| Kappungsanteil (R1) | ideal = gekoppelt im Grenzfall bitgleich; `SchrittMitMuster` = `Schritt` bitgleich; Anteil = Σ Abschnittsdauern Heizgrenze |
| Abnahme R1 im Worktree (`ead963fd`) | Kern-Filter 0 Fehler, ChartProben gleich der Messlatte (200), Kern 9 839, UI 7 233, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (2 übersprungen), Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 byte-gleich gegen R30**, gestörter Lauf PASS, Windows-Schale 0 Fehler |
| Abnahme D1 im Worktree (`36c26656`) | Kern-Filter 0 Fehler, ChartProben JA, Kern 9 850, UI 7 233, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (2 übersprungen), Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 byte-gleich gegen R30**, gestörter Lauf PASS, Windows-Schale 0 Fehler, `Auslieferungsvorlage.Tests` 44/44, `SqlDialektPruefer` 0 Fundstellen |
| Gate im Hauptbaum nach den Merges R1 (`a3c3c655`) und D1 (`54bf19bb`) mit `origin` #654–#656 (`54bf19bb`), danach Merge `origin` #657 (`c457460b`, IFC-Import, Testdatenbank-Kopien) mit gezielter Nachprüfung | Gate auf `54bf19bb`: Kern-Filter 0 Fehler, ChartProben gleich der Messlatte (200), Kern 9 905, UI 7 239, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (2 übersprungen) — alle grün, Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 CSV byte-gleich gegen R30**, gestörter Lauf PASS, Windows-Schale 0 Fehler, Designer unverändert; gezielte Nachprüfung auf `c457460b` nach dem Merge #657: Kern-Filter 0 Fehler, 1 558 betroffene Kern-Tests (Aufheiz, Schema, Testdatenbank, Ifc, Zonenmodell, Ergebnis, Konditionierung, Wachen) und 7 239 UI-Tests grün, Referenzlauf 16/16 PASS, 487/487 byte-gleich, gestörter Lauf PASS, Windows-Schale 0 Fehler |

| Abnahme R2 im Worktree (`29e7f8b9`) | Kern 9 946, UI 7 239, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (2 übersprungen), Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 byte-gleich gegen R30**, gestörter Lauf PASS, Windows-Schale 0 Fehler |
| Abnahme O1 im Worktree (`732c5918`) | Kern 9 938, UI 7 254, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (2 übersprungen), Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 byte-gleich gegen R30**, gestörter Lauf PASS, Windows-Schale 0 Fehler, Designer ohne Abweichung |
| Gate im Hauptbaum nach den Merges O1 (`057765f0`) und R2 (`db1de321`) mit `origin` #659 (Basis **R31**) | Kern-Filter 0 Fehler, ChartProben gleich der Messlatte (200), Kern 9 976, UI 7 254, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (2 übersprungen) — alle grün, Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 CSV byte-gleich gegen R31**, gestörter Lauf PASS, Windows-Schale 0 Fehler, Designer unverändert (auf `db1de321`); gezielte Nachprüfung auf `5d2bedc7` nach dem Merge `origin` #660–#662: Kern-Filter 0 Fehler, 4 004 betroffene Kern-Tests und 7 256 UI-Tests grün, Referenzlauf 16/16 PASS, 487/487 byte-gleich gegen R31, gestörter Lauf PASS, Windows-Schale 0 Fehler |
| N-AH9 (R3) | Dreizonengebäude (zwei Wohnungen, Trennwand U·A 15,0 W/K = 3,06 % H_s, Luftstrom 40 m³/h, unbeheizter Keller am Erdreich), feste Ränder −5 °C ohne Sonne und Gewinne: Nachbarform gegen `ThetaEq` 182 Stunden, größte relative Abweichung 0, `ZuluftN` = `ThetaLue` bitgleich; Formel je Zone gegen die volle Zonenschleife im Band 1,01·P_auf über [h_s − n + 1, h_s + 2], fünf Fälle (täglich (a)/(b), fest (a): 730 Rampen, 92,9 %; täglich (a) mit Wochenende: 522 Rampen, 100,44 %; fest (b) mit Wochenende: 626 Rampen, 94,29 %), Bemessung mit Nachbarn bitgleich nachgerechnet, Keller UNBEHEIZT ohne Rampe (dieselbe Instanz), Aggregation synthetisch (GEMISCHT, UNERREICHBAR, GEKOPPELT, Σ_h max_z) und am Lauf |
| N-AH8 mit Zonen (R3) | Testdatenbank ohne Zonen (0 Gebäude über die Datenbank); Mehrzonenfassung jedes VDI-Gebäudes der 16 Projekte: 17 Gebäude, 51 Zonen bitgleich zu „aus“ (Gebäude und jede Zone), 2 Zonen GEKOPPELT (1047), 17 UNBEHEIZT, 11 680 Sprünge mit n = 1 |
| N-AH10 mit Zonen (R3) | AK1-Gebäude mit Zonen und unbeheiztem Keller: GEKOPPELT und UNBEHEIZT, Reihen unverändert, bitgleich zu „aus“, ohne Stufe rampt dasselbe Gebäude; Zone ohne Kalender (B5): eigener Tagwert 22 °C → θ_T,max 22 °C bei Auslegungsraumtemperatur 20 °C, ohne eigenen Tagwert erbt sie 20 °C, mit Kalender 23 °C; beide Aufbauten planen, Schalter aus ruft nichts, zwei Läufe bitgleich |
| Abnahme R3 im Worktree (`746b7b9e`, Gate 664) | Kern-Filter 0 Fehler, ChartProben gleich der Messlatte (200), Kern 10 011 (1 übersprungen), UI 7 256, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen) — alle grün, Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 CSV byte-gleich gegen R31**, gestörter Lauf PASS, Windows-Schale 0 Fehler |
| Nachprüfung im Hauptbaum nach dem Fast-Forward auf `746b7b9e` (origin unverändert) | Kern-Filter 0 Fehler, 115 Aufheiz- und Zonentests (`Aufheiz*Tests`, `ZonenEingangTests`, `Zonenschleife*Tests`) grün auf demselben Stand, den das Gate im Worktree vollständig geprüft hat |
| N-AH3 (R4) | Bürogebäude, synthetisches Jahr mit Sonne, Gewinnen, Lüftungskalender, Büro-Kalender aller fünf Größen, sechs Varianten: (i) Band in jedem Fenster ohne W1–W3 ohne Ausnahme, höchste Stundenleistung 0,947–0,959·P_auf, Kappungsanteil 0; (ii) W3 unabhängig nachgezählt = Lauf = `Aufheiztage_Nachweisband` in beiden Jahresschleifen (0 in fünf Varianten, 6 beim Kälteeinbruch); (iii) Vorausrechnung auf einer Kopie des Lösers bitgleich, 49–103 Fenster, 237–1 358 Stunden je Variante; (iv) Überlagerung der Stufenantworten rel. 3,4e-15 (Ziel: alle Fenster geregelt; Grenze: 20–21 Fenster voll, übrige bis zur ersten ungeregelten Stunde); ohne Rampe lägen 16 von 49 (Ziel) bzw. 84 von 104 (Grenze) Fenster über dem Band |
| N-AH7 Lauf (R4) | 1018/10632 einzonig und als Mehrzonenfassung, 1008/10576: zwei Läufe und de-DE gegen en-US bitgleich; ulp-Störung in einer zweiten Ladekopie kippt kein n (365, 730, 365 Sprünge), Hash der Heizreihe verschieden |
| Nutzungszeit (R4) | Personen ab 6 Uhr, Sprung um 7 Uhr: 49 von 90 Rampenstunden in der Anwesenheit; Mittel und Überhitzung bitgleich zur Nachrechnung über (Anwesenheit ∧ ¬Maske); ohne Schalter wie bisher |
| Skalierung (R4) | 1018/10632: Faktor 0,253121, P_auf 136,857 → 34,641 kW mit dem Faktor der Spitzen; mit Verbrauchsangabe 80 MWh/a bleibt die Jahreswärme mit und ohne Rampe, Faktor 0,296692 → 0,296527 (B11) |
| Abnahme R4 im Worktree (`2acc65ba`, Gate 669 auf `389d56cf`) | Kern-Filter 0 Fehler, ChartProben gleich der Messlatte (200), Kern 10 050 (1 übersprungen; ein roter Lauf der `QuelltextKodierungWache` auf `389d56cf`, behoben durch `2acc65ba` — BOM zweier neuer Testdateien, danach BOM-, Kultur-, Aufheiz- und `ZonenEingang`-Tests 162 grün), UI 7 256, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen), Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 CSV byte-gleich gegen R31**, gestörter Lauf PASS, Windows-Schale 0 Fehler, Designer unverändert, beide `.resx` gültig (13 642 Einträge, keine Dubletten) |
| Gate im Hauptbaum nach dem Merge R4 (`7ba61a6b`) | Kern-Filter 0 Fehler, ChartProben gleich der Messlatte (200), Kern 10 051 (1 übersprungen), UI 7 256, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen) — alle grün, Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 CSV byte-gleich gegen R31**, gestörter Lauf PASS, Windows-Schale 0 Fehler, Designer unverändert (13 643 Einträge), kein BOM in Markdown, kein Konfliktmarker |
| Gate im Hauptbaum nach dem Merge `origin` #665–#667 (`4b4a84e2`, Testdatenbank 164) | Kern-Filter 0 Fehler, ChartProben gleich der Messlatte (200), Kern 10 090 (1 übersprungen), UI 7 268, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen) — alle grün, Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 CSV byte-gleich gegen R31**, gestörter Lauf PASS, Windows-Schale 0 Fehler, Designer unverändert (13 677 Einträge) |
| Gate 669 im Hauptbaum nach dem Merge `origin` #668 (`8a73e236`, Welle M2, Testdatenbank 165, Basis R32) | Kern-Filter 0 Fehler, ChartProben gleich der Messlatte (200), Kern 10 111 (1 übersprungen), UI 7 272, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen) — alle grün, Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 CSV byte-gleich gegen R32**, gestörter Lauf PASS, Windows-Schale 0 Fehler, Designer unverändert (13 700 Einträge) |
| Export, Auskunft (D2) | aus: in allen 16 Projekten (17 Gebäudesätze, 123 Schlüssel) jeder Schlüssel und jede Reihe schon in der Basis, kein `heizsollwert_`; an (1018): drei Texte, elf Zahlen gleich der Ergebniszeile (t_auf,max 5 h, T_a,B −9,26 °C, P_auf 34,641 kW, 19 Rampentage), `heizsollwert_0.csv` mit 34 Rampenstunden, wieder aus → weg; Heizkalender ohne Schalter: Datei mit 2 496 NaN ohne Aufheizschlüssel; Auskunft = Lauf bitgleich an 17 VDI-Gebäuden (1 GEKOPPELT, 6 mit Faktor ≠ 1, 1040 Tagesbilanz), 1018 in fünf Varianten, Mehrzonen in drei Fällen, 10632 mit Heizkalender 14 h/182 Sprünge (ohne Satz 5 h/365 — B14) |
| N-AH7 Zeile und Export (D2) | 1018 einzonig, 1018 Mehrzonenfassung, 1008: zwei Läufe und de-DE/en-US bitgleich; gestörter Lauf mit gleichen Zuständen, Zählern und Schlüsseln (365, 730, 365 Sprünge mit gleichem n) |
| Sommerlüftung NULL (D2) | ohne Sommerlüftung NULL (1039, 1018, 1007) in Zeile und Auskunft; mit Sommerlüftung die Zahl des Modells, auch je Zone |
| Abnahme D2 im Worktree (`5ac9377e`, Gate 670) | Kern-Filter 0 Fehler, ChartProben JA (200), Kern 10 116 (1 übersprungen), UI 7 284, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (1 übersprungen), Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 CSV byte-gleich gegen R31**, gestörter Lauf PASS, Windows-Schale 0 Fehler, Designer ohne Abweichung, beide `.resx` je 13 688 Einträge ohne Dubletten, SqlDialektPruefer 2 151 Texte 0 Fundstellen |
| Gate im Hauptbaum nach dem Merge D2 (`221da584`, Basis **R33**, Testdatenbank 170) | Kern-Filter 0 Fehler, ChartProben gleich der Messlatte (200), Kern 10 281 (1 übersprungen), UI 7 320, KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen) — alle grün, Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 CSV byte-gleich gegen R33**, gestörter Lauf PASS, Windows-Schale 0 Fehler, Designer unverändert (13 863 Einträge), kein BOM in Markdown, kein Konfliktmarker |
| N-AH11 Aufschlag (R5) | Formel n = 4: (0,0)→4, (2,0)→6, (0,50)→6, (2,50)→6, (3,50)→7, (1,75)→7, (24,100)→28; Aufrundung 30 % → 6, ganzzahlige Anteile ohne weitere Rundung; Deckel 48 bei (24,100) mit n = 30 und (0,100) mit n = 47; n = 1 bleibt 1; Zone D = 8: n' höchstens 9, W2 = 365 genau am Deckel D + 1, Bemessung, t_auf,max, Rampentage, W1, W4 unverändert; kein Aufschlag ohne Sprung; Büro-Montag D = 61: 30 → 48; (0,0) und NULL = ohne Felder, Schalter aus bitgleich (1018); 1018 mit (2,50): 19 Rampentage, 34 → 74 Rampenstunden, längste Rampe 5 → 8 h; Mehrzonen je Zone, Keller UNBEHEIZT; N-AH8 grün |
| N-AH12 manuell (R5) | t ∈ {1, 5, 47} mit/ohne Aufschlag, täglich/fest: n = 2, 6, 9 (47 begrenzt D), BEMESSEN, MANUELL, kein W1, Bemessung identisch; unerreichbare Bemessung rampt mit t + 1, 0 W1-Tage (ohne Wert 365), Zuschlag 0; Deckel am Montag 47 → 48; AK1 bleibt W5; Dreizonengebäude t = 5: 730 Sprünge, 1 825 h (ohne 2 920 h); 1018 ohne Wert TAEGLICH 5 h/19 Tage/34 h, mit t = 5: 365 Tage, 1 825 h, τ₂ 3,91 h, P_auf 34,64 kW, Zuschlag 5,77 kW, Φ_HL NULL (keine Übergabe); Export nur gesetzt; Datenbank-Rundreise mit GEKOPPELT an der Zone; Katalogkopie NULL, Duplikat und Variante 7 |
| Abnahme R5 im Worktree (`3107548f`, Gate 686; Gate 682 auf `f5c8bb42` vor dem Umhängen) | Kern-Filter 0 Fehler, ChartProben JA (200), Kern 10 428 (1 übersprungen), UI 7 357, KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen), Wachen 35/35, Referenzlauf 16/16 **PASS, 487/487 CSV byte-gleich gegen R33**, gestörter Lauf PASS, SqlDialektPruefer 2 255 Texte 0 Fundstellen, Windows-Schale 0 Fehler, Designer unverändert, `Auslieferungsvorlage.Tests` 47/47 |
## 7. Offen

- ~~R2~~, ~~R3~~, ~~R4~~ erledigt (siehe Abschnitt 2); R3 war (Mehrzonen): Nachbarform von `AequivalentN` (B4), Luftkopplungen in Φ_stat, `Aufheizzone.Aus` für gekoppelte Zonen, θ_T,max je Zone (B5), Zustand UNBEHEIZT, Einbau am Ende von `ZonenEingang.Bauen` in beiden Aufbauten, Gebäudewerte nach Festlegung 22, `LetzterAufheizplan` im Mehrzonenweg. **R4:** Pläne und `Aufheizgebaeude` in `GebaeudeModellErgebnis` samt `Skaliert` und Zonen, `HeizleistungMaxStundenH` aus den Kappungsanteilen beider Jahresschleifen, W3 je Zone und Gebäude, vereinigte Rampenmaske in `NutzungBei`, Laufhinweise auch im Mehrzonenweg, Rücksetzen von `LetztesMehrzonenergebnis` bei Fehler entscheiden; G6d: echte Zonenkalender in der Testdatenbank für N-AH8 mit Zonen. Aus dem Wortlaut R4: P_auf skalieren (Festlegung 16), W3 aus den Sprüngen über [h_s − n + 1, h_s + 2], Kappungsreihe in beiden Jahresschleifen, Rampenmaske in `NutzungBei`, Laufhinweise `SIMENG_AUFH_W1…W5`. Aus R2 übernommen: `_vdi6007.Aufheizvorgabe = AufheizvorgabeProjekt` neben `Kuehlbetrieb`; W1 und Deckel aus
  `Aufheizwahl` (N = 0); θ_N nach F2 (b); Φ_stat mit `AequivalentN` statt θ_eq = T_a; Antwort mit dem unbedingten
  Zusatzleitwert der Sprungstunde (`ZusatzleitwertWK(h_s, false, false)`); „fest ≥ täglich“ in N-AH5.
- ~~D2~~ erledigt (siehe Abschnitt 2); D2 war: `GebaeudeKennzahlen` übernimmt `GebaeudeModellErgebnis.Aufheizung` in Gebäude- und Zonenzeile (P_auf = +∞ der Testnaht: Umgang in Spalte und Export), Export nach E32 mit `Aufheizzustand`, Auskunft und Herleitungszeile mit Konditionierungssatz (B14; bei Faktor ≠ 1 Eingabe und Faktor), Hinweis bei Verbrauchsangabe in `SimulationWaermebedarf` (B11), N-AH7 für Ergebniszeile und Export; aus dem Wortlaut: füllt die neuen Modellfelder (auch `SommerlueftungsstundenH` der Zone, `HeizleistungMaxStundenH`
  als Σ der Anteile aus R4), Festlegung 25/26, Export nach E32.
- ~~O1~~ erledigt; ~~`AufheizHerleitung` belegen~~ mit D2 erledigt; offen: Wiki-Anker `#aufheizoptimierung` und `help_mapping.txt` mit KP4.
- **Abschluss A:** Teilkonzept 4.3 (n = 37 bei a = 0,3 in der Augenblicksform), 10.3 „elfte“ Einfrierregel, 5.3 `GEMISCHT`,
  4.7 Residuen je Luftwechsel, Glossar „Nachweisband“ je Quelle, Liste in `GebaeudeRueckwegTests.cs`; N1.69.
- **RP1:** ρ_min (P14) an allen 17 VDI-Gebäuden messen; 1051 im gestörten Lauf (Ladekopie-Muster aus `AufheizDeterminismusTests`); Bauwahl mit B11: knappe `Heizleistung_Max` im Rahmen des Katalogbaus.
- **R5 (E59, nach D2, vor RP1):** Schemaschritt `AufheizManuellSchema` (Nummer spät gegen origin, Platzhalter 171; drei
  Eingabespalten, `Aufheiz_Art` an Gebäude und Zone, `Auslegungsheizlast_Kw` und `Aufheizzuschlag_Kw` am Gebäude, alles
  per `ADD COLUMN`; Neubau nur von `Tab_ErgebnisZone` für `GEKOPPELT`; Sicht `Abfrage_Projektgebaeude`), Testdatenbank
  anheben, Aufschlag nach Festlegung 35 (nur Rampen mit n > 1), Art „manuell“ samt Vererbung und Kopierwegen
  (Festlegungen 37, 38), Ergebniszeile, Auslegungsgröße und Export `Geb[n].Aufheizart` (Festlegungen 39, 41), τ₂ in der
  Auskunft (Festlegung 40), N-AH11, N-AH12.
- **O1b (E59):** zwei Aufschlagfelder in der Projekteinstellung, `SIMKONF_AUFH_AUFSCHLAG_*`, zwei KI-Felder, Glossar.
- **O2 erweitert (E59, E60):** Feld „Aufheizzeit manuell (h)“ im Reiter „Konditionierung“ des Gebäudedialogs mit Vorschlägen
  (bemessene Zeit, Spanne aus τ₂) und weicher Sperre; Auslegungsgröße Auslegungsheizlast + Aufheizzuschlag mit ihren
  Teilen, daneben ideale Spitze, Tagesmittel und P_auf mit Quelle.
- **O3 erweitert (E59, E60):** Abweichungsmerkmale „Art“, „Aufheizzeit manuell (h)“, „Aufschlag (h)“, „Aufschlag (%)“;
  Auslegungsgröße in der Gebäudetafel.
- Reihenfolge der offenen Wellen: D2 → R5 → O1b/O2 → O3 → RP1 → RP2 → A (Alternative: RP1/RP2 nach R5 parallel zu O1b–O3).
- Beim Anwender: P14 (ρ nach der Messung in RP1), SA1 der KP2-Oberfläche, SA-KP3 am Ende.
