# Umsetzungskonzept: Teillast und Takten der Kältemaschine (KM3)

**Stand 09.10.2026 — Fassung 1, Umsetzungsentwurf zur Abnahme durch den Anwender** · Codestand `adb2c94db`
(Zweig `ios_migration_september`) · Schemastand 205 (`UebergabegrenzeSchema`; 206 und 207 angemeldet) ·
Referenzbasis `2026-10-09_R46_Geraetegrenzen` · Fachkonzept
[`Konzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md`](Konzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md)
(Fassung 1, Fragen KM3‑Q1 bis KM3‑Q11 offen).

**Geltung und Abgrenzung.** Das Fachkonzept sagt, *was* gerechnet wird (Lastachse, Takten, Ränder, Skalierung,
Eingaben, Schema, Oberfläche, Regressionsnetz, Fragen). Dieses Papier sagt, *wie, wo, in welcher Reihenfolge und mit
welcher Abnahme* es gebaut wird; Verweise der Form „FK 3.3“ zeigen auf Abschnitte des Fachkonzepts. Es plant nach den
Empfehlungen zu KM3‑Q1 bis KM3‑Q11; entscheidet der Anwender anders, ändert sich der Schnitt dort, wo es angegeben
ist.

---

## 1 Zielbild und Grundsätze

1. **Opt-in.** Jede Rechenwirkung hängt an `Teillast_Weg` der Projektkopie; leer rechnet jede Maschine Zeichen für
   Zeichen wie heute (FK 8.1). Die Basis wechselt allein um das neue Referenzprojekt 1063.
2. **Eine Rechenlogik.** Takten über `Waermepumpentakt` (Teillastfaktor, Mehrstrom, Starts, `VORGABE_CD`), keine
   zweite Formel; die Starts- und Laufphasenzählung von `Kaeltekaskade.Taktverlust` wird so verallgemeinert, dass
   Wärmepumpe und Kältemaschine sie teilen.
3. **Rechnen in reinen Klassen.** Lastachse und Gütegrad als zustandslose Funktionen (`Kaeltemaschinenteillast`,
   `KaeltemaschinenRand`) ohne Datenbank; `Kaeltemaschine.StundeEinzeln` ruft sie.
4. **Eine Quelle der Spalten.** Neue Fachspalten in `KaeltemaschineSchema.Fachspalten`, damit Projektkopie,
   Schreibwege, Katalogabgleich, Projektpaket und Katalogfassung sie ohne eigene Liste führen.
5. **Keine Normtexte, keine Gerätedaten.** Vorgaben als benannte Konstanten mit Kennzeichnung „Vorgabe“; Normen nur
   als Namen; Beispiele und Tests mit neutralen Namen.

## 2 Ausgangslage im Code (Kurzfassung, Einzelheiten FK 2)

| Ort | Heute | Andockpunkt |
|---|---|---|
| `EPOS.Kern/Allgemein/Simulation/Kaelte/Kaeltemaschine.cs` | `StundeEinzeln`: Strom = Kälte / EER_KF, Takt nur gezählt; `Stunde`: gleichmäßige Teilung auf `Anzahl`; `AusModell` | Lastachse, Takt, Folgeschaltung, neue Felder aus dem Modell |
| `…/Kaelte/Kaeltemaschine.cs` (`KaeltemaschinenKennlinie`) | Randwert außerhalb der Stützstellen | Randweg „Gütegrad“ |
| `…/Kaelte/KaelteFestwerte.cs` | Grädigkeiten, freie Kühlung | Mindesthub, Extrapolationsweite, Vorgabekurven, Teillastgrenze |
| `EPOS.Kern/Allgemein/Simulation/Kaeltekaskade.cs` | `MaschineRechnen` (Strom, Zähler), `Taktverlust` der Wärmepumpe | Mehrstrom, Starts, Taktstrom der Maschine |
| `EPOS.Kern/Allgemein/Simulation/Waermepumpentakt.cs` | Taktformel, `VORGABE_CD` | unverändert gerufen |
| `EPOS.Kern/Allgemein/Simulation/SimulationControl.Kaelte.cs`, `SimulationRunner.cs` | Protokollhinweise, Ergebniszeile `ErgebnisKaeltemaschineModel` | neue Ergebnisspalten, Hinweis verworfene Kurve |
| `EPOS.Kern/Allgemein/Import/KaeltemaschineImportLeser.cs`, `KaeltemaschinenKennfeld.cs` | Copper ohne `eir-f-plr`, CSV ohne Teillastzeilen | Kurve, Regelung, CSV-Anpassung |
| `EPOS.Kern/Allgemein/Katalog/KaeltemaschinenTypkennfelder.cs` | `Einspielen` (Schritt 203) | Kurve beim Einspielen, `Ergaenzen()` |
| `EPOS.Kern/Allgemein/Update/KaeltemaschineSchema.cs` (182), `KaeltemaschineAnlageSchema.cs` (183) | Spalten, `Fachspalten`, `Tab_ErgebnisKaeltemaschine` | neuer Schritt `KaeltemaschineTeillastSchema` |
| `EPOS.Kern/Model/KaeltemaschineModel.cs`, `Controller/KaeltemaschineStammCtrl.cs`, `KaeltemaschineAnlageCtrl.cs` | Lesen, `Pruefen`, `Speichern` | acht Felder, Plausibilität, Kontrollwert `Nenn_EER` |
| `EPOS.UI/Dialoge/Erzeuger/KaeltemaschineKatalogDialog.razor` (+ Daten, Texte, KiSicht), `KaeltemaschineAnlageDialog.razor` (+ …), `EPOS.UI.Daten/Erzeuger/Kaeltemaschine*Huelle.cs` | Gruppen „Kenndaten“, „Kennlinie“ | Gruppe „Teillast und Takten“, Schnellwahlen, Lesewerte |
| `EPOS.Kern/Allgemein/Bericht/KennzahlenKatalog.cs`, `Vorlagen/Vorlagenfeldkatalog.cs` (Fassung 17), `KaelteProduktionBild.cs` | `kaelte.km.*` | neue Kennzahlen, Tafel, Fassung + 1 |
| `EPOS.Kern.Tests/Kaeltemaschine*Tests.cs`, `EPOS.UI.Tests/Dialoge/Kaeltemaschine*Tests.cs` | Rechenweg, Schema, Import, Dialoge, Wache 1055 | neue Klassen nach Abschnitt 4 |

## 3 Etappen

Je Etappe eine Folge von Agentenwellen, jede mit höchstens rund 150 Werkzeugaufrufen, im eigenen Worktree,
committet auf dem Wellenzweig, ohne Push, ohne CI-Lauf und ohne vollständiges Gate (das fährt die Orchestrierung nach
dem Merge). Builds laufen nie parallel; `MSBUILDDISABLENODEREUSE=1` vor Build und Test; Tests mit `--filter` auf die
betroffenen Klassen und den xUnit-Schaltern aus `CLAUDE.md`. Das Modell wird je Auftrag ausdrücklich gesetzt.

### 3.1 KM3‑E1 — Schema, Katalog, Import, Typkennfelder

- **Ziel:** FK 4 und FK 6 — Spalten, Lesen und Schreiben, Prüfregeln, Import, ergänzte Typkennfelder; **keine
  Rechenwirkung** (die Felder werden gelesen, aber nicht gerechnet).
- **Vorher:** Schemanummer in der Zeile „Schemaschritt angemeldet“ der
  [Statusdatei](../Status_iOS_Migration.md) anmelden (gebaut als 210) und allein diese Zeile sofort pushen lassen
  (Orchestrierung).
- **Schema:** `EPOS.Kern/Allgemein/Update/KaeltemaschineTeillastSchema.cs` (acht Eingabespalten an beiden Tabellen,
  fünf Ergebnisspalten, CHECK, Wiederholbarkeit, DML nur über `KaeltemaschinenTypkennfelder.Ergaenzen()`), Einträge in
  `SchemaStand.Zielversion`, Paketanhebung, `SchemaMigration` der Windows-Schale, `Werkzeuge/Testdatenbankschema`,
  Testkopie; Testdatenbank auf den neuen Stand heben (LFS-Filter aktiv).
- **Katalog und Controller:** `KaeltemaschineSchema.Fachspalten` erweitern; `KaeltemaschineModel`,
  `KaeltemaschineStammCtrl` (Lesen, `Pruefen` mit Plausibilität FK 3.2 und Kontrollwert `Nenn_EER`, `Speichern`),
  Projektkopie in `KaeltemaschineAnlageCtrl`; Katalogabgleich und Projektpaket prüfen.
- **Import:** `KaeltemaschineImportLeser` (`eir-f-plr`, `compressor_speed`), CSV-Teillastzeilen mit
  Kleinste-Quadrate-Anpassung, Kommentar in `Quellen/Kaeltemaschine_Kennfeldvorlage.csv`;
  `KaeltemaschinenTypkennfelder.Einspielen` und `Ergaenzen`.
- **Tests:** `KaeltemaschineTeillastSchemaTests` (Spalten, CHECK, Wiederholung, Typkennfelder ergänzt, eigene Sätze
  unberührt), `KaeltemaschineImportTests` erweitert, `KaeltemaschinenTypkennfelderSchemaTests` um die Kurve,
  `KaeltemaschineDatenbankTests` (Lesen/Speichern), `KatalogabgleichTests`.
- **Abnahme:** Build `WP-Plan.Kern.slnf`; Filter-Tests grün; `SqlDialektPruefer` grün; Referenzlauf der CI-Auswahl
  gegen R46 unverändert; Windows-Schale baut auf Linux.
- **Wellen (2):** **E1‑a** `opus` Schema, Modell, Controller, Testdatenbank, SQL-Dialekt (≈ 120 Aufrufe);
  **E1‑b** `opus` Import, CSV-Vorlage, Typkennfelder `Ergaenzen`, Tests (≈ 100 Aufrufe).
- **Statuszeile:** „KM3‑E1 Schema <Nr>, Katalog, Import, Typkennfelder — gebaut, Basis unverändert“.

### 3.2 KM3‑E2 — Rechenweg, Tests, Referenzprojekt 1063, Basis R47

- **Ziel:** FK 3, FK 5.1 bis 5.3, FK 8.
- **Code:** `Kaelte/Kaeltemaschinenteillast.cs` (Normierung, g(PLR), Takt über `Waermepumpentakt`),
  `Kaelte/KaeltemaschinenRand.cs` (Gütegrad), `KaeltemaschinenKennlinie.Auswerten` mit Randweg,
  `Kaeltemaschine.StundeEinzeln`/`Stunde` (Folgeschaltung nur mit Weg), `KaeltemaschinenStunde` um Mehrstrom, Starts,
  Lastgrad, Extrapoliert; `Kaeltekaskade.MaschineRechnen` (Mehrstrom vor Hilfsstromzuschlag, `Taktstrom_stuendlich`,
  gemeinsame Startzählung); `SimulationRunner` schreibt die Ergebnisspalten; `SimulationControl.Kaelte` meldet
  verworfene Kurven und extrapolierte Stunden.
- **Tests:** `KaeltemaschineTeillastTests` (Zahlenbeispiele FK 3.2 bis 3.4, Vorgabe C_d, ungültige Kurve,
  Folgeschaltung, ohne Weg byte-gleich gegen `StundeEinzeln` des Bestands), `KaeltemaschineRechenwegTests` erweitert,
  Kaskadenprobe mit Kältespeicher (Taktstunden sinken mit Speicher).
- **Referenzprojekt:** `Referenzlaeufe/Skripte/referenzprojekt_1063_kaeltemaschine_teillast.py` (Muster
  `referenzprojekt_1055_kaeltemaschine.py`), Werte nach FK 8.2; Wache
  `KaeltemaschineTeillastReferenzprojektWacheTests`; Abschnitt in `Referenzlaeufe/LIESMICH.md` mit Einfrierregel;
  Einfrierregel-Satz in `CLAUDE.md` (FK 8.3) im selben Commit; CI-Auswahl in `kern.yml` und `CLAUDE.md` um 1063
  (KM3‑Q10).
- **Abnahme:** Referenzlauf aller 27 Projekte gegen R46 innerhalb der CI-Toleranz unverändert, 1063 neu und erklärt
  (Taktstunden, Mehrstrom, extrapolierte Stunden, Jahres-EER gegen 1055); Basis R47 eingefroren; Byte-Vergleich als
  Information; `dotnet run --project EPOS.Referenzlauf -c Release -- vergleich <R46> <neu>` → GESAMT PASS für alle
  Bestandsprojekte.
- **Wellen (3):** **E2‑a** `opus` reine Klassen und Rechenproben (≈ 90); **E2‑b** `opus` Einbau in Maschine und
  Kaskade, Ergebnisspalten, Protokoll, Referenzlauf gegen R46 (≈ 130); **E2‑c** `opus` Referenzprojekt 1063, Wache,
  Basis R47, LIESMICH, Einfrierregel (≈ 120; Vergleichstafel und Protokoll der Basis darf ein `sonnet`-Nachzug
  übernehmen).
- **Statuszeile:** „KM3‑E2 Rechenweg Teillast und Takten, Referenzprojekt 1063, Basis R47“.

### 3.3 KM3‑E3 — Dialoge, Bericht, Kennzahlen, Vorlagen

- **Ziel:** FK 5.3, 5.4, FK 7.1 bis 7.3.
- **Oberfläche:** Gruppe „Teillast und Takten“ im Katalogdialog (Felder, Lesezeile, Kurvenbild),
  Schnellwahlen „Kurve aus Typkennfeld“, „Typkennfeld auf Datenblatt skalieren…“ (Kern: Skalierung als Funktion in
  `KaeltemaschineStammCtrl`), Auskunft „Teillastpunkte prüfen…“ (Kern: Auswertung ohne Speicherung); Lesewerte im
  Anlagendialog; Kacheln im Ergebnisreiter; Hüllen `KaeltemaschineKatalogHuelle`, `KaeltemaschineAnlageHuelle`;
  KI-Sichten; Ressourcen in beiden Sprachen, `designer_neu.py schreiben`.
- **Bericht:** Kennzahlen `kaelte.km.taktstrom`, `.starts`, `.teillastanteil`, `.lastgrad`, `.jaz_verdichter`;
  Tafel „Teillast und Takten der Kältemaschinen“ in `Vorlagenfeldkatalog`, Vorlagen-Katalogfassung + 1 (heute 17 →
  18; zur Bauzeit die nächste), Word- und Excel-Bausteine, CSV-Export; Vorlagen über `Werkzeuge/Berichtsvorlage` neu
  bauen.
- **Tests:** bunit `KaeltemaschineKatalogDialogTests` (Gruppe, Platzhalter „Vorgabe“, Lesemodus, Schnellwahlen),
  `KaeltemaschineAnlageDialogTests`; `KaeltemaschineBerichtTests`, `VorlagenfeldkatalogWacheTests`,
  `BerichtsvorlageDateiWacheTests`, `AuslieferungsvorlagenWacheTests`; Kultur de-DE über `Kulturvorrichtung`.
- **Abnahme:** Filter-Tests grün; Windows-Schale baut auf Linux; Vorlagen mit grünem `OpenXmlValidator`; ChartProben
  nur, wenn das Bild (KM3‑Q9 b) kommt.
- **Wellen (3):** **E3‑a** `opus` Katalogdialog, Schnellwahlen, Auskunft, Hülle, Ressourcen, bunit (≈ 140);
  **E3‑b** `opus` Anlagendialog, Kennzahlen, Tafel, Export, KI-Sichten (≈ 110); **E3‑c** `sonnet` Vorlagen neu bauen,
  Vorlagenwachen (≈ 60).
- **Statuszeile:** „KM3‑E3 Gruppe Teillast und Takten, Bericht, Vorlagen-Katalogfassung <n>“.

### 3.4 KM3‑E4 — Wiki, Logbuch, Konzepte „wie gebaut“

- **Ziel:** FK 7.4; beide Papiere bekommen einen Abschnitt „Umsetzung — wie gebaut“ und wandern per `git mv` nach
  `Dokumentation/ueberholt/`, Indexzeilen in `Dokumentation/LIESMICH.md` angepasst.
- **Wiki:** `Projekte/Wiki/Programm Dokumentation - Kühlung.wiki`, `Projekte/Wiki/Grundlagen - Kühlung.wiki`;
  Logbuch-Entwurf (zwei Sätze, Version beim Anwender erfragen); ausstehender Upload in der Statusdatei vermerken.
- **Abnahme:** `DokumentationLinkWacheTests`, `WikiProduktdatenWacheTests`, Gegenlesemuster aus `CLAUDE.md` ohne
  Treffer.
- **Wellen (1):** **E4‑a** `sonnet` (≈ 60).
- **Statuszeile:** „KM3‑E4 Wiki-Quellen, Logbuch-Entwurf, Konzepte nach ueberholt“.

**Summe:** **9 Wellen** (E1 2, E2 3, E3 3, E4 1), davon 7 `opus` und 2 `sonnet`, rund 930 Werkzeugaufrufe; dazu je
Etappe Merge, Gate, Statuszeile und Protokoll durch die Orchestrierung (`sonnet`). Reihenfolge je Etappe: **Merge →
Gate → Statuszeile und Protokoll → Push (auf Zuruf) → Nachweis**; ein iOS-Lauf ist nicht begründet (keine Änderung an
der iOS-Hülle). Abhängigkeiten: E2 nach E1 (Spalten), E3 nach E2 (Ergebnisspalten); E3‑a kann parallel zu E2 laufen,
wenn es nur Eingabefelder anfasst.

## 4 Prüfungen und Abnahme

**4.1 Rechenproben (FK 10) → Testklassen.**

| Probe | Klasse | Etappe |
|---|---|---|
| Spalten, CHECK, Wiederholung, Typkennfelder ergänzt, Prüfsumme | `KaeltemaschineTeillastSchemaTests` | E1 |
| Copper-Kurve, CSV-Anpassung (3 Zeilen Kurve, 2 Zeilen keine), `coeff4` ignoriert | `KaeltemaschineImportTests` | E1 |
| Plausibilität, Kontrollwert `Nenn_EER` | `KaeltemaschineDatenbankTests` | E1 |
| Lastachse 2,375 kWh; Takt 0,638 kWh, Mehrstrom 0,058 kWh; linear mit C_d 0,550 kWh | `KaeltemaschineTeillastTests` | E2 |
| Gütegrad 6,519 und 2,298; Mindesthub; Weite 10 K; Deckel 15 | `KaeltemaschineTeillastTests` | E2 |
| ohne `Teillast_Weg` byte-gleich; ungültige Kurve → linear mit Meldung; Folgeschaltung | `KaeltemaschineTeillastTests`, `KaeltemaschineRechenwegTests` | E2 |
| Wache 1063 | `KaeltemaschineTeillastReferenzprojektWacheTests` | E2 |
| Dialog, Schnellwahlen, Auskunft | `EPOS.UI.Tests/Dialoge/KaeltemaschineKatalogDialogTests` | E3 |
| Tafel, Kennzahlen, Vorlagen | `KaeltemaschineBerichtTests`, Vorlagenwachen | E3 |

**4.2 Referenzlauf.** Vor und nach E2 gegen R46 (`lauf` mit den Projekten der CI-Auswahl in der Welle, alle 27 in
E2‑c); Toleranz der CI (Betrag ≥ 1 relativ 1e‑4, sonst absolut 0,01). Erwartung: alle Bestandsprojekte unverändert,
1063 neu; Begründung und neue Basis in `Referenzlaeufe/LIESMICH.md`.

**4.3 Weitere Abnahmen.** `SqlDialektPruefer` nach jeder neuen Anweisung; Linux-Bau der Windows-Schale bei jeder
Änderung an Hülle oder Naht; Windows-Sichtabnahme durch den Anwender nach E3 (Gruppe, Schnellwahlen, Tafel).

## 5 Risiken und Festlegungen

| Risiko | Festlegung |
|---|---|
| Parallelsitzungen und Schemanummern (206, 207 angemeldet) | Nummer vor dem Bau anmelden; `SCHRITT` hängt über `+ 1` an der Vorgängerklasse zur Bauzeit; bei Verschiebung nur Konstante und Statuszeile anpassen |
| Testdatenbank in parallelen Wellen | nur E1‑a und E2‑c heben sie; LFS-Filter aktiv; vor dem Merge den Stand von `origin` mergen und den Schritt neu laufen lassen |
| Vorlagen-Katalogfassung (17) gleichzeitig von anderer Welle gehoben | zur Bauzeit die nächste freie Fassung; Konflikt in `Vorlagenfeldkatalog` inhaltlich zusammenführen |
| Byte-Gleichheit | Opt-in über `Teillast_Weg`; Probe „ohne Weg Zeichen für Zeichen“ und Referenzlauf vor jeder Basis |
| Neue Schwellen (PLR_min, Teillastgrenze 0,95, Extrapolationsweite) | über `Rechenrand.SchwelleErreicht`, nicht `>=` |
| Ergänzte Typkennfelder in Anwenderdatenbanken | DML nur an `ReadOnly = 1` mit Schlüssel `KM:TYPKENNFELD_…` und nur leere Felder; Projektkopien unberührt |
| Taktformel für Kaltwassersätze nicht am Normtext geprüft (Recherche, offene Klärung) | Hausmuster der Wärmepumpe; Normkauf EN 14825:2022 beim Anwender; bei Abweichung nur `Waermepumpentakt` anpassen (eine Stelle) |
| Normzahlen, Geräte- und Firmendaten | nie ins Repositorium; Vorgaben als benannte Konstanten; Beispiele neutral |

## 6 Aufwand

| Etappe | Wellen | Modell | Werkzeugaufrufe (Schätzung) |
|---|---|---|---|
| KM3‑E1 | 2 | opus | ≈ 220 |
| KM3‑E2 | 3 (+ 1 Nachzug) | opus (+ sonnet) | ≈ 340 |
| KM3‑E3 | 3 | opus, opus, sonnet | ≈ 310 |
| KM3‑E4 | 1 | sonnet | ≈ 60 |
| **Summe** | **9 (+ 1)** | | **≈ 930** |

Dazu die Orchestrierung: vier Merges mit Gate (`sonnet`), Statuszeilen und Protokolle (`sonnet`), eine
Anmeldung der Schemanummer.
