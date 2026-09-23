# E7c1 — K‑1 Fall 2 (Abwärmeabfuhr und Stromkennzahl), Förderende 2030, Kohärenzzeile „Anlagenart fehlt" (Protokoll, 23.09.2026)

Statuszeile #440 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Etappe E7 (Teil c1) des
Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 5 Zeile E7, § 6 Schritt A); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 3.6, § 3.9 und § 6.3 Nr. 30; die Entscheide im
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
unter R‑A (A2\*, A20), R‑NR (Nr. 30), R‑EZ (EZ‑5) und R‑E7 — E7‑Q1 (Lesart b), E7‑Q2 (fünf Teilantworten, (2)
mit der Auflage zu σ) und E7‑Q3 (Lesart b), alle am 23.09.2026 entschieden —, die acht Fragen dieser Etappe
unter R‑E7c1. Anlass: die Anwenderentscheide E7‑Q1 bis E7‑Q3 vom 23.09.2026. Zweig `e7c1` von `e06d7eae`
(Spitze von `e7b`), zwei Phasen und zwei Nachzüge; Phase 1: `e531f0bf` (E7c1/1), `152827a3` (E7c1/2),
`e8a6a773` (E7c1/3), `d17be688` (E7c1/4), `f1014e66` (E7c1/5), `45553be3` (E7c1/6), `8ca5bcbe` (E7c1/7);
Phase 2: Merge `91dd77ba` (Arbeitszweig `954d4dcc` mit Schemaschritt 104) und `1599faf4` (E7c1/8); `ccf9f22f`
(E7c1/9, Testdatenbank, vom Orchestrator committet); zweiter Nachzug: Merge `c3bf4882` (Arbeitszweig `6b5179a6`
mit der Welle #441 der Nachbarsitzung und den Papieren zu #439). Merge `ea8e2a12` in `ios_migration_september`
(Basis `6b5179a6`; 30 Dateien, +3 511/−82). Opus 5.5 im Worktree `.claude/worktrees/e7c1`.

## Befund vor der Welle

- **K‑1 — der zweite Fall des § 2 Nr. 16 KWKG fehlte.** Verfügt eine Anlage über eine Vorrichtung zur
  Abwärmeabfuhr (Notkühler), ist KWK-Strom `Nutzwärme × Stromkennzahl`, nicht die Nettostromerzeugung.
  `WirtschaftlichkeitCtrl.ReiheJeAnlage` rechnete stets `max(0, Klemme − Hilfsstrom)`, und das Datenmodell führte
  weder Kennzeichen noch Stromkennzahl (Konzept § 3.6 Befund K‑1; Register EZ‑5, A2\*). Gemessen mit E7a (#437):
  Die Wärmeproduktion liegt je Modul vor, der Wärmeüberschuss nur als Projektsumme (in allen
  BHKW-Basisprojekten 0). E7‑Q2 hat die fünf Teilfragen entschieden: (1) die Wärme bleibt je Modul, nur der
  Überschuss wird nach P_el verteilt; (2) σ ist die gepflegte Geräteeigenschaft oder P_el ÷ P_th der
  Gerätezeile — sonst keine Vorgabe, sondern die Kohärenzzeile „Stromkennzahl fehlt" und kein Wert nach Fall 2
  (Auflage des Anwenders); (3) die Kürzung geht zuerst von der Einspeisung ab; (4) der Ersatzweg rechnet nach
  P_el; (5) die Felder stehen in der Überlagerung „Sätze und Herkunft".
- **A20 — die Realisierungsfrist war eine Konstante.** Die Prüfkette setzte das Fristende auf den 31.12. des
  Jahres „Stichtag + `KWKG_REALISIERUNG_JAHRE`" mit `KWKG_REALISIERUNG_JAHRE = 4` — und nur, wenn ein Stichtag
  gepflegt war; ein Förderende 2030 führte der Gesetzeskatalog nicht (R‑U5). E7‑Q3, Lesart b: Das Katalogdatum
  2030 ist das Ende der Frist zur Inbetriebnahme an Stelle der vier Jahre, die Zuschlagsreihe läuft bis zum Ende
  des Kontingents.
- **Nr. 30 — die Kern-Regel war schon da, die Kohärenzzeile nicht.** Ohne gepflegtes Kontingent und ohne
  Anlagenart leitete `KwkgKontingentRechner.Ableiten` 0 h mit Grund ab (`WIRT_KWKG_KONTINGENT_OHNE_ART`); eine
  Kohärenzzeile „Anlagenart fehlt" gab es nicht. E7‑Q1, Lesart b: Der Zuschlag entfällt nur, wo das Kontingent
  aus der Anlagenart abzuleiten ist, und nur dann erscheint die Zeile; die Anlagenart des 1030-BHKW wird in der
  Testdatenbank gepflegt. Schemaschritt 102 und „(bitte wählen)" waren mit #437 gebaut.
- **Im Bestand:** Kein Projekt konnte das Kennzeichen tragen. KWKG-Sätze führt allein 1030 (Anlagen 14920 und
  14921, 8,0 / 4,0 ct/kWh, Kontingent 30.000 h gepflegt, Anlagenart NULL, Stichtag 2026, Inbetriebnahme 2027);
  das Fristende liegt für 1030 nach beiden Lesarten am 31.12.2030.

## Gebaut — Phase 1 (E7c1/1 bis E7c1/7)

- **Schemaschritt 105 (E7c1/1).** `SchemaKatalog.Schritt105_KwkgAbwaermeabfuhr` legt an `Tab_Energieanlagen`
  zwei Spalten an: `KWKG_Abwaermeabfuhr` (INTEGER NOT NULL DEFAULT 0, `CHECK (… IN (0,1))`) und
  `KWKG_Stromkennzahl` (REAL, nullbar); die Tabelle bleibt `STRICT`. `SCHRITT_105_KWKG_ABWAERMEABFUHR` ist reines
  DDL, wiederholbar und ergebnisneutral (0 heißt Fall 1); `SchemaStand.Zielversion` = 105; `TestDatenbank`,
  `Werkzeuge/Testdatenbankschema` und die Vorsorge `WirtschaftlichkeitCtrl.StelleTabellenSicher` ziehen nach.
  Migrationstest `KwkgAbwaermeabfuhrSchrittTests` (Liste und Typen, Arbeitskopie mit Bestand 0 bzw. NULL, der
  `CHECK` lässt nur 0 und 1 durch). Gebaut ist der Schritt gleich als 105; Schritt 104 kam erst mit dem Nachzug,
  die Migration 103 → 105 lief an einer Kopie sauber über die Lücke.
- **K‑1 Fall 2 (E7c1/2).** `KwkStromRechner` (neu, rein, ohne Datenbank): σ ist die gepflegte
  `KWKG_Stromkennzahl` (Herkunft „gepflegt"), sonst P_el ÷ P_th der Gerätezeile (`Tab_BHKW`, „berechnet"),
  sonst keine („fehlt"); eine gepflegte Zahl ≤ 0 gilt als nicht gepflegt. Nutzwärme = Wärmeproduktion des
  Moduls − sein Anteil am Wärmeüberschuss des Projekts (nur der Überschuss wird nach P_el verteilt), bei 0
  geklemmt; KWK-Strom = `min(Netto, Nutzwärme × σ)`; Kürzung = Netto − KWK-Strom, zuerst von der Einspeisung,
  dann vom Eigenverbrauch. In `ReiheJeAnlage` bleibt der Anteil am Split physikalisch (nach Netto), gekürzt wird
  die zugeteilte Menge; ohne bestimmbares σ ist der Zuschlag der Anlage 0 (Herleitung
  `WIRT_KWKG_FALL2_OHNE_SIGMA`, Kohärenzzeile „Stromkennzahl fehlt"). Der Ersatzweg verteilt Nutzwärme und
  Nettostromerzeugung des Projekts nach P_el auf die Anlagen (führt keine Anlage P_el, zu gleichen Teilen), die
  Summe der Kürzungen geht zuerst von der Einspeisung ab, die Zeile `WIRT_KWKG_FALL2_ERSATZ` nennt es. Die
  Herleitung je Anlage (`WIRT_KWKG_FALL2_ANLAGE`) nennt Fall, σ mit Herkunft, Nutzwärme (Wärmeproduktion −
  Anteil am Überschuss), KWK-Strom als `min(…)` und die Kürzung, davon Einspeisung und Eigenverbrauch.
  `KwkgModulNachweis` bekommt sieben nullbare Felder (`Abwaermeabfuhr`, `Stromkennzahl`,
  `StromkennzahlHerkunft`, `NutzwaermeMWh`, `KwkStromMWh`, `KuerzungMWh`, `HerleitungKwkStrom`) — bei Fall 1 alle
  leer, der Umschlag unverändert, **die Nachweisfassung bleibt 7**. `LiesAnlagen` liest Kennzeichen, Kennzahl und
  P_th in einer fünften Fähigkeitsstufe. Ohne Kennzeichen rechnet jede Anlage Zeile für Zeile wie vorher.
- **A20 — das Fristende als Katalogdatum (E7c1/3).** Die Konstante `KWKG_REALISIERUNG_JAHRE` ist gestrichen. Der
  Gesetzeskatalog führt `KWKG_INBETRIEBNAHME_FRISTENDE` = 2030 (Einheit Jahr, gemeint der 31.12., Stichjahr 2020,
  Status gesichert, Quelle „KWKG 2025 § 6 — Inbetriebnahme bis zum 31.12. dieses Jahres (Förderende, R-U5)") in
  **Katalog-Generation 8**; bestehende Datenbanken bekommen die Zeile per Nachsaat.
  `WirtschaftlichkeitCtrl.FristendeInbetriebnahme` liest ihn nullbar mit dem Inbetriebnahmejahr; Projektblock
  und Prüfkette je Anlage halten die Inbetriebnahme gegen dieses Datum — **auch ohne Stichtag**. Nach dem
  Fristende gibt es keinen Zuschlag, mit Hinweis (`WIRT_KWKG_ANLAGE_FRIST` je Anlage, geändert: nennt Fristende
  und Katalogherkunft; `WIRT_KWKG_NACH_FRISTENDE` im Projektblock). Ohne Katalogwert gibt es keine stille
  Vorgabe, sondern die Zeile „ungeprüft" (`WIRT_KWKG_FRISTENDE_FEHLT`), und die Anlage bleibt förderfähig. Die
  Reihe läuft unverändert bis zum Ende des Kontingents, eine Höchstdauer in Kalenderjahren gibt es nicht (Test:
  Inbetriebnahme 01.10.2026 → Jahr 12 = 2037). Tests `KwkgFoerderendeTests` (Katalogdatum, bis und nach dem
  Fristende je Anlage und im Projektblock, ohne Stichtag, ohne Katalogwert, Reihe bis 2037);
  `KatalogpflegeTests` auf Generation 8 mit 53 KWKG-Zeilen.
- **Nr. 30 — die Kohärenzzeile „Anlagenart fehlt" (E7c1/4).** `KontingentDerAnlage` hält eine Anlage ohne
  Anlagenart, deren Kontingent abzuleiten ist, in den Datenlücken des Laufs fest (`KwkgLuecken`, Regel- und
  Ersatzweg); die Herleitung nennt den Fall ohne den Nachsatz „es gilt der eingetragene Wert"
  (`WIRT_KWKG_KONTINGENT_ANLAGE_OHNE_ART`, neu). `KohaerenzPruefung` meldet `KOH_KWKG_ANLAGENART_FEHLT` — Schwere
  Hinweis, ohne Betrag, eine Zeile mit allen betroffenen Anlagen —, ebenso `KOH_KWKG_STROMKENNZAHL_FEHLT` aus
  E7c1/2; beide stehen vor den Steuerzeilen, weil sie an keinem Steuerpfad hängen. Ein gepflegtes Kontingent
  erzeugt keine Zeile. Tests `KwkgAnlagenartFehltTests` (gepflegtes Kontingent ohne Zeile; ohne Kontingent und
  Anlagenart kein Zuschlag samt Zeile; mit Anlagenart abgeleitet; Ersatzweg; BHKW ohne Sätze ohne Zeile; die
  Testdaten-Pflege 1030 lässt die Anker stehen).
- **Die Überlagerung „Sätze und Herkunft" (E7c1/5).** In der Gruppe „Angaben der gewählten Anlage" des
  BHKW-Dialogs steht die Zeile „KWK-Strom (§ 2 Nr. 16 KWKG): Fall 1 — Nettostromerzeugung." bzw. „… Fall 2 —
  Vorrichtung zur Abwärmeabfuhr, Stromkennzahl σ … (Herkunft)" (`BHW_A_KWK_*`) mit dem Knopf „Sätze und
  Herkunft…" (`BHW_UEB_KNOPF_ANLAGE`). Die Überlagerung „Sätze und Herkunft — ‹Anlage›" (Mockup U22, gebaut so
  weit, wie die zwei Felder es brauchen) führt die Gruppe „KWK-Zuschlag — diese Anlage" mit der Wahl Fall 1 /
  Fall 2 samt Wirkung je Fall (Fall 1: die Nettostromerzeugung des zuletzt gebuchten Laufs; Fall 2:
  `min(Nettostromerzeugung ; Nutzwärme × σ)`), die Tafel Größe · Vorschlag · Herkunft · eigener Wert (leer =
  Vorschlag) · gilt mit der Zeile „Stromkennzahl σ" (Vorschlag P_el ÷ P_th aus `KwkStromRechner`, drei
  Nachkommastellen; „Vorschlag übernehmen" leert das Feld, ohne P_th weich gesperrt), eine Erklärzeile und die
  Knöpfe Abbrechen und Übernehmen. Sie hält einen eigenen Zwischenstand: „Übernehmen" legt ihn auf den
  Arbeitsstand der Anlage, geschrieben wird erst im OK-Weg des Dialogs über `KwkgAnlagenCtrl.Speichere(g, true)`
  (liest Kennzeichen, Kennzahl und P_th und schreibt beide Spalten; eine Kennzahl ≤ 0 wird NULL; der Bestandsweg
  mit acht Spalten bleibt). Die Hülle in `EPOS.UI.Daten` brauchte keine Codeänderung. Die Mengenkette der Gruppe
  Hilfsstrom zeigt die Fall-2-Herleitung des Laufs. Tests: bunit `BhkwSaetzeHerkunftTests` (sechs Fälle), der
  Rundweg des Kern-Controllers in `KwkgFall2Tests`.
- **Ankertests und Kommentare (E7c1/6, E7c1/7).** Die Ankertests halten im Klassenkommentar fest: alt = neu (kein
  Kennzeichen im Bestand, Inbetriebnahme 2027 vor dem Fristende 31.12.2030, Kontingent gepflegt); zwei
  Kommentare, die noch die Frist von vier Jahren nannten, sind nachgezogen.

## Phase 2 und die Nachzüge (Merge `91dd77ba`, E7c1/8, E7c1/9, Merge `c3bf4882`)

- **Merge `91dd77ba`** holt den Arbeitszweig `954d4dcc` (Merge #439 mit Schemaschritt 104). Konflikte in
  `SchemaStand` (Zielversion 105, der Vermerk „104 kommt mit dem Nachzug" entfällt, die Kette 101
  Gebäudespalten, 102 Anlagenart, 103 Tww, 104 Staffel, 105 K‑1 steht im Kommentar) und `SchemaMigration` (104 in
  der Fassung des Arbeitszweigs, 105 dahinter); `Werkzeuge/Testdatenbankschema` ohne die Platzhalterzeile zu 104;
  `TestDatenbank.SchemaNachziehen` führt 101 bis 105 in dieser Reihenfolge (von git zusammengeführt, geprüft) —
  die Kette steht an allen vier Stellen. Ressourcen de/en je 7 729 Einträge ohne Duplikat, Designer-Prüfung
  7 726.
- **E7c1/8 — zwei Katalogpflege-Tests zählen nach der Nachsaat.** Der erste Lauf nach dem Nachzug zeigte zwei
  rote Fälle aus E7c1/3: Mit Generation 8 zählt der Katalog nach der Nachsaat 227 Zeilen (226 Vorbelegung und die
  Generationsmarke), und „Anlegen" zog die Nachsaat mitten im Fall nach. Beide Fälle rufen jetzt
  `StelleKatalogSicher` vor dem Zählen — auf der Repo-Datenbank (Generation 7) wie auf einer nachgezogenen
  (Generation 8) dieselbe Zahl.
- **E7c1/9 — die Testdatenbank auf Schemastand 105** (vom Orchestrator committet): `Werkzeuge/Testdatenbankschema`
  legt die zwei Spalten an und sät die Katalog-Generation 8 nach (eine Zeile, `KWKG_INBETRIEBNAHME_FRISTENDE`);
  `Tab_Applikation` trägt 105. Dazu das Testdaten-UPDATE nach E7‑Q1: Die Anlagen 14920 und 14921 des Projekts
  1030 tragen `KWKG_Anlagenart = 'NEUANLAGE'` statt NULL (nur die Neuanlage erreicht 30.000 Vbh ohne
  Kostenanteil); das Kontingent 30.000 h bleibt gepflegt, kein Anker bewegt sich. LFS-Zeiger, SHA-256
  `66aa52b0…`, Größe unverändert 67 727 360 Byte, `quick_check` ok. Der Nachtrag „Schemastand 105" in
  `Referenzlaeufe/LIESMICH.md` steht mit den Papieren zu #440.
- **Merge `c3bf4882` — der zweite Nachzug** holt den Arbeitszweig `6b5179a6` (die Welle #441 der Nachbarsitzung
  mit `5d00937b`, `898f110c`, `bef715e8` und die Papiere zu #439) ohne Konflikt. Ressourcen de/en je 7 736
  Einträge (43 aus E7c1, 7 aus #441), keine Duplikate; der Designer ist neu erzeugt, 7 733 Einträge; die
  Testdatenbank bleibt auf dem Stand 105 aus `ccf9f22f`; Kern-Filter und Windows-Schale je 0 Fehler.

## Fragen aus der Etappe

Der Zwischenbericht nannte acht Fragen an den Anwender; sie stehen im Entscheidungsregister als **R‑E7c1**. Alle
acht sind offen; gebaut ist jeweils Lesart a.

| Frage | Stand |
|---|---|
| **E7c1‑Q1** — Winzige Kürzungen bei berechnetem σ: Bei 1030 mit σ = 50 ÷ 81 entsteht aus der Rundung auf 0,01 MWh eine Kürzung von 0,002 MWh (KWKG Jahr 1 7.315,96 → 7.315,92 €). (a) die Formel ohne Toleranz, die Kürzung steht sichtbar in der Herleitungszeile; (b) Kürzungen unter 0,01 MWh als 0 werten. Empfehlung a | offen — gebaut ist a |
| **E7c1‑Q2** — Kontingentverbrauch in Fall 2: (a) Vollbenutzungsstunden und Kontingent zählen weiter nach dem Bruttostrom des Moduls, nur die bezahlte Menge sinkt; (b) die Vollbenutzungsstunden aus dem KWK-Strom — das Kontingent reicht länger, die Reihe wird länger. Empfehlung: vorerst a; vorher am Gesetzestext prüfen, ob die Definition der Vollbenutzungsstunden auf den KWK-Strom abstellt — wenn ja, b mit E7c2 | offen — gebaut ist a, Prüfauftrag gegen die Vbh-Definition |
| **E7c1‑Q3** — Fristende auch ohne Stichtag: (a) ja, das Datum ist absolut; (b) nur prüfen, wenn ein Stichtag gepflegt ist. Empfehlung a | offen — gebaut ist a |
| **E7c1‑Q4** — Das Fristende fehlt im Katalog: (a) der Zuschlag bleibt, mit der Zeile „ungeprüft"; (b) dann kein Zuschlag. Empfehlung a | offen — gebaut ist a |
| **E7c1‑Q5** — Schwere der zwei Kohärenzzeilen „Stromkennzahl fehlt" und „Anlagenart fehlt": (a) Hinweis; (b) Warnung. Empfehlung a — die Folge steht schon in der Herleitungszeile | offen — gebaut ist a |
| **E7c1‑Q6** — Die Nachweisfassung bleibt 7, obwohl `KwkgModulNachweis` sieben nullbare Felder bekommt (wie beim Vorgänger #351). Empfehlung: so lassen | offen — gebaut: Fassung 7 |
| **E7c1‑Q7** — Der Rest der Überlagerung „Sätze und Herkunft" (Anlagenart, Tatbestand, Satztafel, Energie- und Stromsteuer, „Wirkung Jahr 1", „Wahl und Herkunft…"), der KI-Feldkatalog (die zwei Felder) und die Berichtsspalten zu Fall 2 (Modultafeln in Word und Excel). Empfehlung: mit E7c2 | offen — Empfehlung E7c2 |
| **E7c1‑Q8** — Katalogzeilen ohne Leser: `KWKG_REALISIERUNGSFRIST` und `KWKG_STICHTAG_DAUERBETRIEB` werden weiter gesät, aber von keinem Code mehr gelesen. Empfehlung: in einer späteren Generation kennzeichnen oder entfernen | offen — Empfehlung spätere Generation |

## A/B-Nachweis

Gemessen an Kopien der Testdatenbank im Scratchpad des Agenten: vorher die Kopie mit Schemastand 103 auf
`e06d7eae`, nachher die nach 105 migrierte Kopie; nach dem ersten Nachzug auf dem Stand 105 wiederholt, mit und
ohne das Testdaten-UPDATE.

**Die dreizehn Basisprojekte:**

| Projekt | Größe | vorher | nachher | Grund |
|---|---|---|---|---|
| alle dreizehn | Ankerweg, frische Simulation, KWKG-Reihe (8 011 Werte) | — | 0 Abweichungen | kein Projekt trägt das Kennzeichen |
| 1024 | Kapitalwert / Energiekosten / Betriebskosten | −2.896.359,13 € / 188.167,18 €/a / 99,00 €/a | gleich | kein KWKG-Modul |
| 1030 | Kapitalwert / KWKG Jahr 1 (Ankerweg) | −21.895.377,28 € / 7.315,96 € | gleich | Inbetriebnahme 2027 vor dem Fristende nach beiden Lesarten; Kontingent gepflegt |
| 1030 | frisch gerechnet: Kapitalwert / KWKG Jahr 1 | −31.141.242,71 € / 7.322,63 € | gleich | ebenso |
| 1030 | Energiekosten / Reihe | 1.176.906,60 €/a / Jahre 1–12 | gleich | — |
| 1042 | Kaskade | 13.000,00 € | gleich | — |
| alle dreizehn, nach dem Nachzug auf 105 | Basismessung | Stand vor dem Nachzug | 8 095 von 8 095 Werten gleich, auch mit dem Testdaten-UPDATE | kein Anker bewegt sich |

**Proben an 1030** (Ankerweg, Kapitalwert / KWKG Jahr 1; alle zwanzig Proben nach dem Nachzug wiederholt,
0 Abweichungen):

| Probe | vorher | nachher | Grund |
|---|---|---|---|
| σ gepflegt 0,5 | −21.895.377,28 € / 7.315,96 € | −21.904.948,06 € / 6.137,94 € | 605,52 MWh × 0,5 = 302,76 MWh KWK-Strom, Kürzung 71,02 MWh (frisch gerechnet 6.138,92 €) |
| σ berechnet 50 ÷ 81 | ebenso | −21.895.377,57 € / 7.315,92 € | Kürzung 0,002 MWh aus der Rundung auf 0,01 MWh (E7c1‑Q1) |
| σ berechnet, dazu 100 MWh Wärmeüberschuss | 7.315,96 € | −21.902.427,26 € / 6.448,21 € | Nutzwärme 520,77 MWh, KWK-Strom 321,47 MWh |
| ohne σ (P_th leer) | 7.315,96 € | −21.945.748,56 € / 1.116,03 € | Zuschlag der Anlage 0, Kohärenzzeile „Stromkennzahl fehlt" |
| Ersatzweg, σ 0,5 | −21.895.377,34 € / 7.315,95 € | −21.902.856,75 € / 6.395,35 € | Kürzung zusammen 54,40 MWh |
| Einspeisung zuerst (künstlicher Bedarf, σ 0,5) | KWKG Jahr 1 10.184,46 € | 7.828,43 € | Einspeisung 146,55 → 75,53 MWh, Eigenverbrauch 227,23 MWh unverändert |
| Stichtag 2025, Inbetriebnahme 06/2030 (je Anlage oder am Projekt) | −21.954.815,75 € / 0 | −21.896.087,48 € / 5.899,97 € | alte Regel: mehr als vier Jahre nach dem Stichtag; neue: vor dem 31.12.2030 |
| Inbetriebnahme 31.12.2030 bzw. 03/2031 | 5.899,97 € bzw. 0 | gleich | nur der Hinweistext ändert sich |
| Inbetriebnahme 03/2031 ohne Stichtag | 5.899,97 € | −21.954.815,75 € / 0 | das Fristende gilt auch ohne Stichtag (E7c1‑Q3) |
| Anlagenart NEUANLAGE (Testdaten) | Anker | gleich (1 948 von 1 948 Werten) | Kontingent gepflegt |
| ohne Kontingent und ohne Anlagenart | 1.116,03 € | gleich, dazu die Kohärenzzeile „Anlagenart fehlt" | Nr. 30 |

## Zahlen und Abnahme

- **Im Worktree `e7c1`** (ohne anderen Testprozess): nach dem ersten Nachzug Migrationstest 3/3; die gefilterten
  Klassen im Kern zunächst 238 von 240 (die zwei Katalogpflege-Fälle), nach E7c1/8 132/132; Oberfläche
  (BHKW-Dialog, Wachen) 303/303; voller Lauf auf der Repo-Datenbank (Stand 104): EPOS.Kern 4 811 von 4 812 — rot
  allein die Schemastand-Wache —, EPOS.UI 5 284, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 und 1
  übersprungen; `Auslieferungsvorlage.Tests` 14 von 26 rot, alle „Schemastand 104 (erwartet 105)"; Gegenprobe mit
  einer außerhalb des Repos auf 105 gebrachten Kopie samt den 1030-Daten an Stelle der Repo-Datenbank: Kern-Filter
  11 033 grün und 1 übersprungen, Auslieferungsvorlage 26/26 (danach aus LFS zurückgeholt, Prüfsumme gleich);
  Referenzlauf aller dreizehn Projekte auf der 105-Kopie gegen `2026-09-22_R11_Bestandsbefunde` 13/13 PASS,
  3 882 737 Werte, 357/357 Dateien byte-gleich, mit den 1030-Daten ebenso — die Basis bleibt R11; SQL-Prüfer
  (105-Kopie) 1 645 Texte, 0 Fundstellen; Kern-Filter und Windows-Schale 0 Fehler. Nach dem zweiten Nachzug
  (`c3bf4882`): Migrationstest 4/4; gefiltert Kern 380/380 und 92/92, Oberfläche 151/151 und 169/169.
- **Gate #440** auf `ea8e2a12` (09:45–09:49): Kern-Filter (Release) 0 Fehler; ChartProben alle grün, 111 Bilder
  gleich der Messlatte; Tests 0 Fehler, 11 058 bestanden, 1 übersprungen (EPOS.Kern 4 832, EPOS.UI 5 289, KiKern
  524, SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen) — die Schemastand-Wache ist mit der
  Testdatenbank auf 105 grün; Dokumentationswachen 26/26; Windows-Schale (Worktree `e7c1`, `c3bf4882`) 0 Fehler.
- **Ressourcen** (de und en): **43 neu** — K‑1 (11): `WIRT_KWKG_SIGMA_GEPFLEGT`, `_BERECHNET`, `_FEHLT`;
  `WIRT_KWKG_FALL2_ANLAGE`, `_OHNE_SIGMA`, `_ERSATZ`, `_ERSATZ_ANLAGE`, `_ERSATZ_OHNE_SIGMA`, `_VERTEILUNG_PEL`,
  `_VERTEILUNG_GLEICH`; `KOH_KWKG_STROMKENNZAHL_FEHLT`. A20 (2): `WIRT_KWKG_NACH_FRISTENDE`,
  `WIRT_KWKG_FRISTENDE_FEHLT`. Nr. 30 (2): `WIRT_KWKG_KONTINGENT_ANLAGE_OHNE_ART`, `KOH_KWKG_ANLAGENART_FEHLT`.
  Überlagerung (28): `BHW_UEB_KNOPF_ANLAGE`, `BHW_UEB_TITEL`, `BHW_UEB_G_KWKG`, `BHW_UEB_KWK_STROM`,
  `BHW_UEB_FALL1_MENGE`, `BHW_UEB_FALL1_OHNE_LAUF`, `BHW_UEB_FALL2_FORMEL`, `BHW_UEB_SP_GROESSE`,
  `BHW_UEB_SP_VORSCHLAG`, `BHW_UEB_SP_HERKUNFT`, `BHW_UEB_SP_EIGEN`, `BHW_UEB_SP_GILT`, `BHW_UEB_LEER_VORSCHLAG`,
  `BHW_UEB_GILT_VORSCHLAG`, `BHW_UEB_GILT_EIGEN`, `BHW_UEB_GILT_EIGEN_OHNE`, `BHW_UEB_GILT_KEINE`,
  `BHW_UEB_GILT_FALL1`, `BHW_UEB_INFO_SIGMA`, `BHW_UEB_UEBERNEHMEN`, `BHW_FLD_FALL1`, `BHW_FLD_ABWAERMEABFUHR`,
  `BHW_FLD_STROMKENNZAHL`, `BHW_HERL_STROMKENNZAHL`, `BHW_HERL_STROMKENNZAHL_OHNE`, `BHW_A_KWK_FALL1`,
  `BHW_A_KWK_FALL2`, `BHW_A_KWK_FALL2_OHNE` (Namen nach dem Ressourcenplan des Mockups). **1 geändert** —
  `WIRT_KWKG_ANLAGE_FRIST` (nennt Fristende und Katalogherkunft). Gestrichen ist keine Ressource, nur die
  Konstante `KWKG_REALISIERUNG_JAHRE`. Neu außerdem der Katalogschlüssel `KWKG_INBETRIEBNAHME_FRISTENDE`. Nach
  dem zweiten Nachzug je 7 736 Einträge, Designer 7 733.
- **Schemaschritt 105** (`SCHRITT_105_KWKG_ABWAERMEABFUHR`), `SchemaStand.Zielversion` = 105; der nächste freie
  Schritt ist **106**.

## Abnahme am Gerät (A‑E7c1‑1, Windows und iPad)

(1) Dialog BHKW-Wirtschaftlichkeit, Gruppe „Angaben der gewählten Anlage": die Zeile „KWK-Strom (§ 2 Nr. 16
KWKG): Fall 1 — Nettostromerzeugung." und der Knopf „Sätze und Herkunft…". (2) Die Überlagerung „Sätze und
Herkunft — ‹Anlage›": Wahl Fall 1 / Fall 2 mit der Wirkung je Fall, die Zeile „Stromkennzahl σ" mit dem Vorschlag
P_el ÷ P_th (drei Nachkommastellen), Herkunft, eigenem Wert (leer = Vorschlag), „Vorschlag übernehmen" (leert das
Feld; ohne P_th grau mit Grund) und „gilt"; Abbrechen ändert nichts, Übernehmen legt die Wahl in den
Arbeitsstand, OK schreibt, Wiederöffnen hält Kennzeichen und Kennzahl. (3) Nach „Berechnen" die Herleitung je
Anlage im Ausweis der KWKG-Zeile — Fall 2 mit gepflegtem σ (Herkunft „gepflegt") und mit berechnetem σ
(„berechnet aus P_el ÷ P_th der Gerätezeile = … kW ÷ … kW"), Nutzwärme, KWK-Strom und Kürzung, davon Einspeisung
und Eigenverbrauch. (4) Die Kohärenzzeilen „Stromkennzahl fehlt" (Kennzeichen gesetzt, weder eigener Wert noch
P_el und P_th) und „Anlagenart fehlt" (weder Kontingent noch Anlagenart) — Hinweis ohne Betrag, auf der Seite,
im BHKW-Dialog und in den Berichten; ein gepflegtes Kontingent ohne Anlagenart bringt keine Zeile. (5) Eine
Anlage mit Inbetriebnahme nach dem 31.12.2030: kein Zuschlag, der Hinweis nennt das Ende der Frist zur
Inbetriebnahme und seine Herkunft aus dem Katalog. (6) Englisch.

## Befunde nebenbei

- **Katalogzeilen ohne Leser:** `KWKG_REALISIERUNGSFRIST` und `KWKG_STICHTAG_DAUERBETRIEB` werden weiter gesät,
  aber von keinem Code mehr gelesen (E7c1‑Q8).
- **KI-Feldkatalog:** Der Hilfe-Assistent kennt die zwei neuen Felder der Überlagerung noch nicht (E7c1‑Q7).
- **Berichtsspalten:** Die Modultafeln im Wort- und Excelbericht führen keine Spalten zu Fall 2 (Stromkennzahl,
  Nutzwärme, KWK-Strom, Kürzung); die Herleitung steht im Ausweis und im Nachweisumschlag (E7c1‑Q7).
- **Mockup-Schlüsselname:** Das Mockup nannte die Mengenzeile `WIRT_KWKG_MENGE_FALL2`; gebaut ist sie als
  Herleitung je Anlage unter `WIRT_KWKG_FALL2_ANLAGE`. Die Ressourcentafel der Kategorie 5 ist nachgezogen.
- **Der Rest der Überlagerung** (Mockup U22): Anlagenart, Tatbestand, Satztafel, Energie- und Stromsteuer,
  „Wirkung Jahr 1" und der Knopf „Wahl und Herkunft…" stehen weiter im Formular bzw. fehlen (E7c1‑Q7).

## Offen

- **Abnahme am Gerät** A‑E7c1‑1 (sechs Punkte oben).
- **Die acht Fragen E7c1‑Q1 bis E7c1‑Q8** beim Anwender (Register R‑E7c1); gebaut ist jeweils Lesart a, zu Q2
  steht die Prüfung gegen die Definition der Vollbenutzungsstunden aus.
- **Nächste Etappe: E7c2** — der Rest der Überlagerung „Sätze und Herkunft", der KI-Feldkatalog und die
  Berichtsspalten zu Fall 2, dazu S‑2 (A3), V‑2/V‑1 (A4), die Schritte E, F, G, B‑4 Rest, B‑6 und der Kapitalwert
  1024 (−676.036,81 € gegen den früheren Konzeptwert). Von A20 bleiben der Mindestabstand (nur mit dem
  Inbetriebnahmedatum der Altanlage) und ETS 2 (mit dem Preispfad) offen.
- **Push** auf Zuruf; der Merge `ea8e2a12` hat noch keinen CI-Lauf.
- **Papiere mit der Statuszeile:** Register (A2\*, A20, Nr. 30, E7‑Q1 bis E7‑Q3, EZ‑5, EZ‑9, EZ‑10, neue Familie
  R‑E7c1), Konzept (Kopf mit Schemastand 105, § 2.2, § 3.6, § 3.9, § 3.10, § 4, § 5, § 6.1, § 6.3 Nr. 30, § 7,
  Anhang), Protokoll der Entscheidwege (§ 8.7, § 8.8), Analysepapier (Kopf, Nachtrag, § 0, § 5, § 6), Rechenweg
  05, Mockup (Ressourcentafel der Kategorie 5, U1, U22, U32, Stand-Absatz), Logbuch-Sätze und die Wiki-Quelle der
  Seite Wirtschaftlichkeit; der Nachtrag „Schemastand 105" in `Referenzlaeufe/LIESMICH.md`; die Köpfe des
  Szenarienkonzepts und der Mockup-Prüfung auf Schemastand 105.
