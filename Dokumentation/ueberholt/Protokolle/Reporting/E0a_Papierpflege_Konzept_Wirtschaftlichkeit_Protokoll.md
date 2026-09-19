# E0a — Papierpflege des Wirtschaftlichkeitskonzepts (ohne Entscheid)

**Datum:** 19.09.2026 · **Zweig:** `worktree-agent-a2e0f0ca5c16fbe1e` (eigener Worktree, von
`ios_migration_september` `455edfd4` abgezweigt, nicht gepusht)

**Auftrag:** Die Berichtigungen aus `2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`
(§ 7, § 5, § 4) mit den Protokollen `Analyse_Konzept_Wirtschaftlichkeit_2026-09-19/01…08` und aus
`2026-09-19_Pruefung_Mockups_Wirtschaftlichkeit.md` (§ 3.2, § 4) mit
`Pruefung_Mockups_2026-09-19/02_Konsistenz_Papiere.md` anwenden — **ohne** den Schnitt in drei
Papiere und **ohne** einen der offenen Entscheide vorwegzunehmen.

**Commits (neun, in der Reihenfolge):**

| SHA | Betreff | `git diff --stat` |
|---|---|---|
| `1fe063dc` | E0a: Konzept Wirtschaftlichkeit — Kopf, Geltung und Dialograum | 1 Datei, +213 / −127 |
| `571c8cea` | E0a: Konzept Wirtschaftlichkeit — Rechenwege und Befunde | 1 Datei, +51 / −32 |
| `5ca9ef5e` | E0a: Konzept Wirtschaftlichkeit — Entscheide, Umsetzungsstand, Anhang | 1 Datei, +167 / −70 |
| `5d0dd85b` | E0a: Nutzungsdauer- und Szenarienkonzept nachgezogen | 2 Dateien, +38 / −13 |
| `ba2ea185` | E0a: Rechenwege 04, 05 und 08 nachgezogen | 3 Dateien, +11 / −6 |
| `25d9d7b4` | E0a: LIESMICH des Ordners Wirtschaftlichkeit_Kosten nachgezogen | 1 Datei, +18 / −9 |
| `3cdce7d7` | E0a: Mockup-Code-Spannen relativ schreiben (Wache grün) | 1 Datei, +3 / −3 |
| `4b8664a2` | E0a: Protokoll der Papierpflege | 1 Datei, neu |
| *(Nachtrag)* | E0a: § 2.16 — zwei Bedingungen der iOS-Erreichbarkeit | 2 Dateien, +8 / −5 |

**Zeilenzahlen der Papiere vorher → nachher:**

| Datei | vorher | nachher |
|---|---|---|
| `aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` | 2 427 | 2 632 |
| `aktuell/Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md` | 572 | 597 |
| `aktuell/Konzept_Nutzungsdauer_AfA_EPOS-Plan.md` | 264 | 264 |
| `aktuell/Wirtschaftlichkeit_Kosten/LIESMICH.md` | 95 | 104 |
| `aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/08_Wirtschaftlichkeit_Nutzungsdauer.md` | 186 | 190 |
| `aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/04_Energiekosten.md` | 164 | 165 |
| `aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/05_Verguetungen_BHKW.md` | 313 | 313 |

Alle sieben Dateien sind UTF-8 **ohne** BOM mit **LF** — so, wie sie vorlagen (vor dem Schreiben
mit `head -c 3 | od` und `grep -c $'\r$'` gemessen, danach gegengeprüft).

---

## 1 Quelle `07_Konzeptqualitaet_Entscheide.md` — 22 Stellen

Zeilennummern des **neuen** Stands im konsolidierten Konzept.

| § der Quelle | Stelle (neu) | alt | neu |
|---|---|---|---|
| 2.2 | Z. 3 | „Stand 02.09.2026 · Codestand `922228a` (Branch `ios_migration`) · `SchemaMigration.ZIEL_VERSION` = 61 · Schemaschritt 62 vergeben (U-1), neue ab 63" | „Stand 19.09.2026 · Codestand `e1c4275e` (vor dem Umschreiben der Geschichte am 12.09.2026: `922228a`) · `SchemaStand.Zielversion` = 96 · Schemaschritte 90–96 vergeben, neue ab 97" |
| 2.1 | Z. 25–30 | — | neuer Absatz „Wo die drei Quellen heute liegen": zwei in `ueberholt/` (nie Regelquelle), das Grundlagenpapier in `aktuell/` |
| 2.1 | Z. 32–34 | Kasten „Arbeitsregel (Anwender, 30.08.2026): erst das Konzept, keine Umsetzung … ausdrücklich nicht implementiert" | Kasten „Stand der Umsetzung: § 6.1. Was hier als Soll steht, ist gebaut, sofern § 6.1 die Etappe führt." |
| 2.1 | Z. 36–39 | — | neuer Absatz **„Herkunft"**: die Arbeitsregel als Satz über die Entstehung, nicht als geltende Regel |
| 2.3 | Z. 43, 44 | „Formelkarte `rechenwege_formelkarte.md` (30.08.2026, gegen `b2ad3e3`)" / „Feldkarte `b5_feldkarte.md`" | beide mit „**nicht erhalten**: lag im Sitzungs-Scratchpad; Belege heute an der Codestelle"; `b2ad3e3` → `2cfb871d` |
| 2.3 | Z. 45–47 | Quellnamen ohne Pfad | die drei Quellen als Verweise auf ihren heutigen Ablageort |
| 2.4 | Z. 50–58 | Überschrift „Begleitende Artifacts", Einleitung „maßgeblich ist dieses Dokument", **sechs** Tabellenzeilen | Überschrift „Begleitendes Artifact", **eine** Zeile (739d3cca) mit „die Repo-Datei `../Mockups/Dialog_Formel_Zahlenprobe.html` führt"; die fünf abgelösten in **einem** Satz mit ihren Repo-Nachfolgern |
| 2.5 | Z. 135–142 | „## 2.2 `Form_BhkwWirtschaftlichkeit` — neu (BW9)" + „Sechs Gruppen an einem Ort" | „## 2.2 `BhkwWirtschaftlichkeitDialog` (Hilfekennung `Form_BhkwWirtschaftlichkeit`)" + „**Gebaut** (§ 6.1, Etappe B5) … **acht Gruppen**" mit den zwei zusätzlichen Gruppen benannt |
| 2.11 | Z. 2237 (§ 5 K2) | „rechnet fest Weg B (% des Bedarfs); Wege A und C nur in der Kostenposition \| Dialog benennt die Basis klar" | „Weg B — % des **Endenergiebedarfs**, bewertet mit dem eigenen Trägerpreis der Anlage \| **erledigt mit #365/#366** (Schemaschritt 94)" |
| 2.11 | § 5 K9 | Zeile „K9 \| § 6.1 zählt ‚9 Felder', real 11 \| Konzeptkorrektur" | **gestrichen** (der Satz steht in § 6.1 nicht mehr) |
| 2.11 | Z. 2293–2303 | „Folge für den Schema-Nummernraum: 62 ist damit vergeben — neue Schritte ab 63 … für M-3 / § 9 Nr. 3 auf **63** nachzuziehen" | „Der Schemaschritt ist noch nicht vergeben. Schritt 62 ist anderweitig belegt (`Schritt_62_KlimaWaisen`); U-1 bekommt **Schritt 103**" — der Nachzieh-Auftrag ist gestrichen |
| 2.10 | Z. 2293–2307 | „liegt derzeit **nur** auf Zweig `claude/lucid-cori-a9a425` … noch nicht gemergt" | Zweig- und Commitangabe gestrichen; Verweis auf `ueberholt/Konzept_Einheitenbruch_…`, die fünf Randfragen ausdrücklich **hierher übernommen**, Verwechslungshinweis auf `aktuell/Konzept_Einheiten_EPOS-Plan.md` |
| 2.11 | Z. 986–991 (§ 2.13 Kopf) | „die **fünf** Punkte und ihre Messung" | „die fünf Punkte, **dazu (6) als Verweis**, und ihre Messung" |
| 2.11 | Z. 762 (V-3 ValERI) / Z. 2226 (Befund V-3) | s. Quelle 02 | unverändert bzw. s. Quelle 02 |
| 2.6 / 1.5 | Z. 2373–2375 | — | Vorbemerkung zu § 6.3: Nummern bleiben, Sprünge bleiben, erledigte Punkte werden gekennzeichnet statt entfernt |
| 2.6 / 1.5 | Z. 2377–2385 | B5-Kernaufgaben 1–3 als offen | alle drei durchgestrichen mit Beleg (K7-Codestelle, U31/#347/#364, B5/#286 ff.) |
| 2.6 / 1.5 | Z. 2409, 2434 | 9c „hat keine Rubrikzeile" / 9g „nicht gebaut" | beide erledigt mit **U17 (#346)** samt Codestellen und Test |
| 2.6 / 1.5 | Z. 2450–2455 | 9h „fünf fehlende Stücke" | „**drei** fehlende Stücke (Mockup-Anhang **U39**)"; die zwei mit #357 erledigten benannt |
| 2.6 / 1.5 | Z. 2469, 2472, 2480, 2487, 2491 | Nr. 12, 14, 18, 20, 21 als offen | 12 erledigt (W5‑B‑8), 14 überholt (Katalog führt den Satz; offen bleibt die fehlende Wache), 18 „vermutlich überholt, nachmessen", 20 planbar statt blockiert, 21 teils erledigt mit #333 |
| 2.7 | Z. 2499–2517 | sieben Fallstricke gleichrangig | zwei gelten weiter, drei ACE-Fallen „nur noch für den Migrationslauf", zwei als überholt benannt und gestrichen |
| 2.8 (+ 03/§ 6.2) | Z. 2521–2530 | § 6.5 mit acht Zeilen | Stromsteuer-Wache präzisiert, **drei** statt vier Orte der Einspeisevergütung, **fünf** CREATE-Tabellen + 55 `SpalteSicher`, Sicht statt Access-Abfrage, `Form_Kosten`/`UcBkKosten` gegenstandslos, **zwei** statt drei Vorrang-Implementierungen |
| 2.9 | Z. 2559–2575 | § 7 mit B5/B6/B7/B8/B9 und der Reihenfolgebegründung | B5/B6/B7 nach § 6.1 verschoben; **B8** auf S-2, V-3-Rest, B-6, I-5 gekürzt; **B9** planbar mit Grundlagen § 5 als Abnahmeliste; Reihenfolgebegründung durch die heute offenen Etappen ersetzt, Verweis auf den Etappenplan E0–E12 |
| 2.9 | Z. 2576–2591 | „Voraussetzungen vor der Umsetzung — zwei Entscheidungen … beide inzwischen gefallen" | „Offene Entscheide vor der nächsten Codeetappe": **A1, A2, A5, A11, A13**, ausdrücklich als **offen** benannt |
| 5 | Z. 2349–2356 | § 6.1 mit 19 Etappenzeilen | **acht** neue Zeilen: B5 (#286), B6 (#328, Schritt 88), B-1/#331 mit Basis #333, VV (#359, Schritt 93), Hilfsstrom (#365/#366, Schritt 94), Nutzungsdauern S2 (#357), Übernahme (#363), Bezugsgrößen (#364) — Inhalt und Ergebniswirkung aus den Statuszeilen |
| 4 | Z. 2318–2321 | R-U-Tafel mit fünf Zeilen | **R-U6…R-U9** aus Grundlagen § 10 Nr. 1–4 (45‑€‑Mechanismus ETS 2, § 10 Abs. 3 BEHG, Projektionsbericht 2026, Enddatum der Versteigerungsphase) |
| 2.12 | Z. 2596–2629 | — | neuer **Anhang „Kürzel und Etappen"** mit der Übersetzungstafel (21 Zeilen) und den zwei Kennungsfallen |
| 1.10 | Z. 508, 753, 2243 und Szenarienkonzept Z. 581 | „K-8" | „K8" (vier Stellen, keine „K-8" mehr im Bestand) |
| 1.10 | Z. 965–977 | Namensvorsicht mit K-1/K1 und B-1/B-1 | um **U-1/U1**, **V-1/V-1** und **V-Gn gegen Gn** erweitert |

## 2 Quelle `01_Rechenkern_Kosten_Energie.md` § 7 — 13 Stellen

| Stelle (neu) | alt | neu |
|---|---|---|
| Z. 1508–1518 (§ 3.1) | A_t mit zwei Preissteigerungstöpfen | dritte Zeile `Endenergie_1 × (1+p_E)^(t−1)` und `Ersatz_t = A₀ × (1+p_I)^t`, Verweis auf Szenarienkonzept § 10 |
| Z. 1531 | — | Rahmentabelle um `Preissteigerung_Investition` (Schritt 72) |
| Z. 1536 | „Genau zwei Preissteigerungsreihen" | „Drei Preissteigerungsreihen (p_B, p_E, p_I)" |
| Z. 1587 (§ 3.2) | „ohne ORDER BY (Befund I-3)" | „ohne `ORDER BY`; die Runde 3 friert ihre Basiszeilen vorher ein (I-3 erledigt)" |
| Z. 1603 | „`EUR_PRO_KWP` \| Σ PV_Leistung × Satz \| ⚠ I-1" | „Σ (Modulanzahl × Modulleistung)/1000 × Satz \| `PhotovoltaikCtrl.KwpSumme`" |
| Z. 1652–1654 (§ 3.4) | „Sperre zuerst: fehlt Menge oder Satz ⇒ Betrag = 0" | „… dann gilt der **erfasste Betrag** (Anwenderentscheid I-2, 30.08.2026)" |
| Z. 1667 | „Auflöser null ⇒ Betrag 0" | „Auflöser null ⇒ **erfasster Betrag** (I-2)" |
| Z. 1668–1670 | „Rückfall-ermittelbare Arten (9 Stück)" / „Übrige Arten (`EUR_PRO_H`, `EUR_PRO_KWH`, …): nur Konserve" | „(10 Stück) … `EUR_PRO_H` und die beiden `EUR_PRO_KWH_*` seit FX2 frisch" / „**Nur Konserve** bleiben `PROZENT_BRENNSTOFFKOSTEN` und `PROZENT_STROMKOSTEN`" |
| Z. 1714–1716 | „`InvestSummeFuer`: `SUM(EingegebenerWert)` … abgeleitete Beträge fehlen (B-5)" | „Summe der Investitionskaskade (`InvestKaskade.Summen`) … (B-5 erledigt)" |
| Z. 2191–2193, 2206 (§ 4) | I-1, I-2, I-3, B-5 mit ⚠ | alle vier auf ✔ mit Beleg; bei I-3 „entschied früher die Datenbank" statt „ACE" |
| Z. 2203 | „B-4: Vier Arten nie frisch" | „Zwei Arten nie frisch" |
| Z. 2185 (§ 4 Kopf) | „Aus der Abnahmeliste der Formelkarte." | Zusatz „die Datei ist nicht erhalten — die Belege stehen heute an der Codestelle" |
| Z. 2368 (§ 6.2) | „Die 1030-Anker sind … überholt und müssen neu gesetzt werden." | „1030 ist auf der **Investitionsseite** neu verankert (410.000,00 €); Kapitalwert und Betriebskosten tragen weiterhin keinen Anker." |
| Z. 2566 (§ 7 B8) | „I-3 (ORDER BY)" | entfällt mit der neuen B8-Zeile (I-3 ist erledigt) — die Formulierung „Runde 3 reihenfolgeunabhängig" steht jetzt an Z. 1587 und Z. 2193 |
| Z. 113–117 (§ 2 Kopf) | — | `Form_*`/`Uc*` **einmal** erklärt: eingefrorene Hilfe- und KI-Kennungen, keine Klassennamen |
| Z. 154–155 | „8 Bestandsfelder (heute in `Form_KwkgModule`)" | „heute die eigene Gruppe ‚Angaben der gewählten Anlage' … die Maske `Form_KwkgModule` ist mit BK1 aufgelöst" |

## 3 Quelle `02_Rechenkern_Verguetung_Steuern.md` § 8 — 12 Stellen (Kopfzeile s. Quelle 07)

| Stelle (neu) | alt | neu |
|---|---|---|
| Z. 1869–1871 | „Die projektweite Ableitung bleibt für den Ersatzweg stehen" | „Der Ersatzweg leitet das Kontingent **ebenfalls je Anlage** ab und mischt es leistungsgewichtet (BK1a)" |
| Z. 1963–1964 | „nächster freier Schemaschritt ist **92** (90 ist BK1a, 91 ist BK1b)" | „**97** (90 BK1a, 91 BK1b, 92 Vergleichsprojekt, 93 Vergütung je Variante, 94 Hilfsstrom-Bemessung, 95 KL-3 Klimaspalten, 96 FK-2 Projekt-Fremdschlüssel)" |
| Z. 2224 | „S-1 · S-3 · S-4 · S-5 · **S-6**: … `STROMST_REDUZIERT_SATZ` ungesät" | S-6 als eigene ✔-Zeile mit Saat- und Lesestelle; der Rest von S-1/S-3/S-4/S-5 bleibt offen |
| Z. 2472 | § 6.3 Nr. 14 „bleibt Konstante bis zur Katalog-Nachpflege" | durchgestrichen, „überholt"; offen bleibt allein die fehlende Wache |
| Z. 2409 | 9c „hat keine Rubrikzeile" | erledigt mit U17/#346 (s. Quelle 07) |
| Z. 2434 | 9g „nicht gebaut" | erledigt mit U17/#346 |
| Z. 2377–2379 | § 6.3 B5-Kernaufgabe 1 (K7) | erledigt, Codestelle genannt; K7 auch in § 5 (Z. 2242) und in `Rechenweg/05` |
| Z. 1053–1056 (§ 2.13 (4)) | „Eigenverbrauchsmengen je Anlage aus der Strommatrix, die dort getrennt vorliegen" | „braucht einen **Verteilschlüssel je Anlage**; die Strommatrix trennt nur nach **Tarifzone**, der Kern verteilt nach dem Netto-Stromanteil (V-4)" |
| Z. 2107 (§ 3.9) | „Strommix-Rückfall \| Hinweis (435 g/kWh)" | „**Laufhinweis** ohne Wertangabe, kein `KohaerenzHinweis` (`WirtschaftlichkeitCtrl.cs:5465`)" |
| Z. 2293–2303 | 62/63-Absatz | gestrichen (s. Quelle 07 § 2.11) |
| Z. 135, 253, 272 | § 2.2 / § 2.3 / § 2.4 mit `Form_*` | Razor-Dialogname mit der Hilfekennung als Paar |
| Z. 2226 | V-3 „keine eigene Spalte" | Zusatz „`ErloesReihe.PV_VERGUETUNG` existiert; es fehlen **ein Aufruf** in `Mehrjahresbild.Baue` **und ein Ressourcenschlüssel** — damit S-Aufgabe, keine offene Frage" |

## 4 Quelle `03_Datenmodell_Schema.md` § 6.2 — 14 Stellen

| Stelle (neu) | alt | neu |
|---|---|---|
| Z. 90–91 (§ 1.2) | „Kostenart · Bemessung · Satz · Menge · Betrag · IstErloes …" | Spaltennamen `Einheitpreis`, `EingegebenerWert` genannt; `ID_AnlageGeraet`, `StammID`, `VorlageID`, `NutzungsdauerID` ergänzt |
| Z. 105–108 | „die Wirtschaftlichkeitsspalten an `Tab_Energieanlagen` (KWKG_*, …)" | „die **neun** `KWKG_*`-Spalten … samt `ID_Carrier`" |
| Z. 784 (§ 2.11.5, Zeile „Rahmen") | „je Größe `_Best`/`_Worst` (8 Spalten) … **neu**" | „**6 von 8 vorhanden** seit Schritt 71 … neu ist allein Best/Worst des Betrachtungszeitraums. Namensvorsicht: `Szen_*_Dauer` ist die Nutzungsdaueränderung" |
| Z. 1016–1017 (§ 2.13 (3)) | „103 von 109 Investitionspositionen keine Dauer" | „**95 von 101**; 27 davon tragen einen Betrag (Messung 19.09.2026)" |
| Z. 1038–1040 (§ 2.13 (3) Nr. 5) | „handgepflegtes `ErsatzintervallJahre` und einen `RestwertEuro`" | „die gleichnamigen **Felder des Flottenstands** (JSON in `Tab_SpeicherAuslegung`), nicht über Spalten — berührt die **Einfrierregel** des Projekts 1046" |
| Z. 1963 (§ 3.6) | „nächster freier Schemaschritt ist 92" | 97 (s. Quelle 02) |
| Z. 3 und Z. 2293 (§ 5) | „Schemaschritt 62 vergeben (U-1)" | Kopfzeile ohne Schritt 62; „Schritt 62 ist anderweitig vergeben … U-1 bekommt Schritt 103", dazu die Messung vom 19.09.2026 (`Tab_Brennstoff_Stamm.Einheit` = `m³`, `energy_carrier.billing_unit` = `Nm³` seit Schritt 26a) |
| Z. 2299–2307 (§ 5) | „liegt nur auf Zweig … noch nicht gemergt" | Verweis auf `ueberholt/`, Bezugsstand Access `SchemaVersion 61`, Verwechslungshinweis |
| Z. 2525 (§ 6.5) | „vier Tabellen; neue Spalten gehören an beide Stellen" | „**fünf** Tabellen mit eigenem `CREATE TABLE` … dazu 55 `SpalteSicher` auf sechs Tabellen; die CREATE tragen weder `STRICT` noch Fremdschlüssel (ADR-001 Option B)" |
| Z. 2524 (§ 6.5) | „BHKW-Einspeisevergütung an **vier** Orten … drei Felder zu viel" | „an **drei** Orten" mit Nennung; der vierte ist mit Schritt 84/85 entfallen |
| Z. 2526 (§ 6.5) | „Komponenten-IDs hart verdrahtet … `Form_Kosten` gegen `UcBkKosten`" | durchgestrichen, **gegenstandslos**; die Unterscheidung liegt im Kern (`KostenVorlagenCtrl.IstErfassungsgruppe`) |
| Z. 2525, 2527 (§ 6.5) | „gespeicherte Access-Abfrage" (zweimal) | „gespeicherte **Sicht** `Abfrage_Kostenfaktoren` / `Abfrage_Energietraeger_Effektiv` (`sql/schema/002_views.sql`)"; eigene Zeile „Access ist abgelöst" |
| Z. 2523 (§ 6.5) | „eine Wache hält beide zusammen" | „eine Wache prüft **Modell gegen Konstante** (`StrompreisZerlegungTests`), **nicht** Konstante gegen Katalog — eine Wache dafür fehlt" |
| Z. 2472 (§ 6.3 Nr. 14) | s. Quelle 02 | s. Quelle 02 |

**Nicht übernommen:** die zwei **neuen** offenen Punkte (25) `KWKG_Anlagenart = ''` und (26)
`Nachweis_Json` in 0 von 78 Ergebniszeilen. Sie sind keine Berichtigung einer bestehenden Stelle,
sondern neue Punkte; sie gehören in die Etappenplanung (E-Reihe), nicht in die Papierpflege ohne
Entscheid.

## 5 Quelle `04_Oberflaeche_Huellen.md` § 3 (n‑1…n‑15) und § 8.2

| Nr | Stelle (neu) | alt | neu |
|---|---|---|---|
| n‑1 | Z. 124, 125 | `Form_BhkwWirtschaftlichkeit` / `Form_PhotovoltaikVerguetung` in § 2.1 | `BhkwWirtschaftlichkeitDialog` / `PhotovoltaikVerguetungDialog` |
| n‑2 | Z. 142 | „Knopf in der Fußleiste von `UcWirtschaftlichkeit`" | „Fußleiste von `WirtschaftlichkeitSeite.razor`" |
| n‑3 | Z. 154–155 | „8 Bestandsfelder (heute in `Form_KwkgModule`)" | Gruppe „Angaben der gewählten Anlage"; Maske mit BK1 aufgelöst |
| n‑4 | Z. 162, 163 | „DateTimePicker mit Haken" | „Datumsfeld mit Kontrollkästchen" |
| n‑5 | Z. 164, 165, 174, 175 | „ComboBox" (vier Felder) | „Baustein `Auswahlfeld`" |
| n‑6 | — | „Tooltip" gegen „Werkzeugtipp" | **nicht geändert** (reine Wortwahl, keine Berichtigung; die Analyse nennt es als Uneinheitlichkeit, nicht als Fehler) |
| n‑7 | Z. 253–258 | „914 × 724, festes Fenster … zwei Spalten" | „Sieben Gruppen untereinander, einspaltig"; das Wunschmaß als Konstante der Hülle benannt |
| n‑8 | Z. 259–265 | „CheckBox" | „Kontrollkästchen" |
| n‑9 | Z. 259, 261 | „Radio", „Radios:" | „`Optionsgruppe`" |
| n‑10 | Z. 348 | „`UcBkKosten.cs:771-772`" | „`EPOS.UI/Seiten/Berichte/KostenSeite.razor` mit `KostenSeiteGaben.cs:658`" |
| n‑11 | Z. 502–506 (§ 2.7) | „Designer-basiert (FK1/Ä6), Texte über `MyResource` mit GetString-Rückfall" | „Razor-Komponenten **ohne Designer**: Texte über `[Parameter]`-Vorgaben und `*Texte`-Bündel, in der Hülle mit `Resource.*` bzw. `T(schlüssel, rückfall)` belegt" |
| n‑12 | Z. 508 | „Fußleiste von `UcWirtschaftlichkeit`" | „Fußleiste der Wirtschaftlichkeitsseite (`…/WirtschaftlichkeitSeite.razor`)" |
| n‑13 | Z. 567, 786, 814 | „`Form_CaseEingabe` (±-Knopf)" | „`CaseEingabeDialog` (±-Knopf; Hilfekennung `Form_CaseEingabe`)" |
| n‑14 | Z. 636, 659, 666 | „Kennzahlengrid", „ListView", „unter dem Grid" | „Vergleichstabelle (`<table class="epos-raster epos-matrix">`)", „Variantenliste (eine `Optionsgruppe` je Zeile, kein ListView)" |
| n‑15 | Z. 656, 741 | „`UcWirtschaftlichkeit`" | `WirtschaftlichkeitSeite.razor` mit der Hilfekennung |
| § 8.2 | Z. 508–512 | „Die Fußleiste … ist voll — **sieben** Knöpfe" | „führt **fünf Knöpfe** … nach dem Wegfall von ‚Verlauf…' vier. Lücke K8 ist damit **gegenstandslos**" |
| § 8.2 | Z. 1063–1065 | „Die Hinweiszeile füllt heute nur die Windows-Hülle" | Zusatz: **einziger** Schreiber (`:404`), Textbildung liegt im Kern (`NutzungsdauerAbgleich.Hinweis`) |
| § 8.2 | Z. 1057–1061 und Z. 1086–1088 | „gehören nach `EPOS.UI.Daten`" | Zusatz: dort gibt es **keinen Ordner `Wirtschaftlichkeit`**, er ist mit anzulegen (heutige Ordnerliste genannt) |
| § 8.2 | Z. 1440–1444 (§ 2.16 Punkt 4) | „eine plattformfreie Hülle in `EPOS.UI.Daten` gibt es nicht — auf iOS ist der Dialog nicht erreichbar" | „**aus zwei Gründen** nicht erreichbar, und beide müssen fallen": die fehlende Hülle **und** der leere Wirt (`IProjektQuelle.BerichteKostenGaben` liefert `null`, kein Seitenschlüssel in der Whitelist von `AppWurzel.razor`) |
| § 8.2 | Z. 2243 (K9) | „§ 6.1 zählt ‚9 Felder', real 11 · Konzeptkorrektur" | Zeile **gestrichen** (07/§ 2.11); K7 als erfüllt gekennzeichnet |
| § 8.2 | Z. 2264 (ET-D-3) | „genau zwei Einträge — Abrechnungseinheit und kWh" | Zusatz „**umgesetzt**; offener Rest **U32**: Der Kartenzustand fällt weiter auf `ID_Umrechnung = -1` zurück" |
| § 8.2 | Z. 3 (Kopfstand) | „Stand 02.09.2026" | 19.09.2026 (s. Quelle 07 § 2.2) |

## 6 Quelle `05_Berichte_VALERI_Nutzungsdauer.md` § 8.1 und § 8.2

| Papier · Stelle (neu) | alt | neu |
|---|---|---|
| Konzept Z. 1027–1032 | „Knopf ‚Nutzungsdauern vorbelegen' … **nicht gebaut**" und „`NutzungsdauerID` … **kein Dialog** lässt sie wählen" | eigener Absatz „**Erledigt mit #357**" mit beiden Codestellen; offen bleibt der gesperrte Zwilling auf der Betriebsseite |
| Konzept Z. 719 (V-G11) | „fehlt" | „**Freitext umgesetzt** (W5‑B‑12/G6); es fehlen **Kategorie und Beurteilung**" |
| Konzept Z. 811 (§ 2.11.6) | „eine Klasse, **1 380 Zeilen**" | „1 412 Zeilen (gemessen 19.09.2026)" |
| Konzept Z. 865–866 (§ 2.11.6) | „deckt kein Test den **Excel-Generator** ab … eine Wache über die Blattstruktur" | „den Excel- **und den Word-Generator** … eine Wache über **beide** Blattstrukturen" |
| Konzept Z. 1076–1083 (§ 2.13 (5)) | „das Lesen von `Gestrichelt` … steht" / „Farben nach laufendem Index" | „**`Reihe.Gestrichelt` ist ein `bool`** und trägt zwei Stricharten — drei Szenarien brauchen eine dritte"; „die Palette hat **acht** Farben, mit Farbe = Variante reicht sie bis acht Varianten" |
| Szenarienkonzept Z. 318–321 (§ 9.1) | „Maßstab ist die Kapitalwertdifferenz **zum Stamm**" | „zur **gewählten Referenz** … seit #358 je Vergleichsgruppe wählbar"; Nachzieh-Hinweis auf `WIRT_EMPF_KEINE` |
| Szenarienkonzept Z. 331–332 | „gegenüber dem **Stammprojekt** wirtschaftlich" | „gegenüber der **Referenz** wirtschaftlich" |
| Szenarienkonzept Z. 248 (§ 7.1) | „Referenzfall \| Stammprojekt" | „die **wählbare Referenz je Vergleichsgruppe** (`ID_Referenzprojekt`, seit #358; Vorgabe = Stammprojekt)" |
| Szenarienkonzept Z. 466–469 (§ 10.3) | „**Zielstand:** `SchemaStand.Zielversion = 72`" | „**Zielstand dieser Etappe** … Der heutige Schemastand ist ein anderer (19.09.2026: 96, neue ab 97)" |
| Szenarienkonzept Z. 576–597 (§ 11) | keine Rückverweise, keine Stufenangabe | Satz „Die Stufen 0 bis 3 zählt § 2.11.6" und neuer **§ 11.1** mit der Zuordnung W5‑B‑9…W5‑B‑12 ↔ V-A…V-E samt Nummernvorsicht G1…G11 gegen V-G1…V-G12 |
| Konzept Z. 692–702 (§ 2.11.2) | Gap-Tafel ohne Übersetzung | Tafel „V-Gn ↔ Gn" am Anfang des Abschnitts |
| Konzept Z. 742–758 (§ 2.11.4) | Etappentafel mit drei Spalten | vierte Spalte „entspricht / bereits geliefert durch" |
| `Rechenweg/08` Z. 39 | Knöpfe „Anhang-E-Checkliste…", „XLSX mit Formeln exportieren…" ohne Stand | „**beides Mockup-Knöpfe: im Bestand gibt es weder ein Element noch einen Ressourcenschlüssel dafür**" |
| `Rechenweg/08` Z. 107–108 | — | neuer Satz: die Referenzmappe des Bestands für A8/B9 ist noch zu benennen, samt maßgeblichem Kapitalwertblatt |

## 7 Quelle Mockup-Prüfung § 3.2 (ohne Stern)

| Papier · Stelle (neu) | alt | neu |
|---|---|---|
| Konzept Z. 135–142 (§ 2.2) | sechs Gruppen, „neu (BW9)" | acht Gruppen, gebauter Razor-Dialog (s. Quelle 07 § 2.5) |
| Konzept Z. 176 | „Hilfsenergieanteil [%] … Vorschlag BHKW 2–4 %" | „Hilfsenergieanteil **[% des Endenergiebedarfs]** … Bemessen wird am Endenergiebedarf (Brennstoff) dieser Anlage — nicht an den Kosten (Schritt 94, #365/#366)" |
| Konzept Z. 205–210 | Codeblock „Einspeisung 5,57 ct/kWh … Eigenstrom 0,00 … Kontingent 15.000 Vbh" | „Einspeisung **5,5667** … Eigenstrom **2,4167** … Kontingent 30.000 Vbh" mit den Herleitungen des Mockups |
| Konzept Z. 224 (§ 2.2 Gruppe 3) | „Erdgas 4,42 €/MWh · **2.480 MWh** … = 10.962 €/a" | „**4.797,2 MWh × 4,42 €/MWh = 21.203,4 €/a**" (Musterprojekt) |
| Konzept Z. 231 (§ 2.2 Gruppe 4) | ein Sprungknopf „Strombezug…" | „**zwei Sprungknöpfe** ‚Strombezug…' und ‚BHKW-Tarif…'" mit dem Speicherhinweis |
| Konzept Z. 2561 (§ 7) | B5 als vorgeschlagene Etappe | B5 als **umgesetzt**, Zeile nach § 6.1 |
| Konzept Z. 253–270 (§ 2.3) | „zwei Spalten", sieben Gruppen in Konzeptreihenfolge | einspaltig/untereinander; Gruppennamen und -reihenfolge aus `PhotovoltaikVerguetungDialog.razor` (Anlage · Anzulegender Wert · Vermarktung · Vergütungsausfall (§ 51 / § 51a) · Strompreis / Bezugsbewertung · 60‑%‑Wirkleistungsbegrenzung (§ 9 Abs. 2) · Σ Vorschau) |
| Konzept Z. 262 | Gruppe „Anlage" mit vier Feldern | Feld **„Degradation [%/a]"** ergänzt (mit #348 hierher verlegt) |
| Konzept Z. 272–289 (§ 2.4) | vier Gruppen ohne Vergleichsprojekt und Szenarien | **Vergleichsprojekt** (§ 2.9, Schritt 92) in der Gruppe Allgemein und ein Absatz zum **Szenarien-Parametersatz** mit Verweis auf das Szenarienkonzept § 4 |
| Konzept Z. 502, 508–512 (§ 2.7) | „Fußknöpfe 110 × 30"; „sieben Knöpfe" | „**Fußknöpfe mindestens 88 × 44**"; „**fünf Knöpfe**, K8 gegenstandslos" |
| Konzept Z. 519–531, 539, 553, 576–578 (§ 2.8) | „Reiter Investition/Betrieb/Ertrag", „Unter dem **Satz** steht die Herleitung", drei Fußknöpfe | Optionsgruppe **plus zwei Reiter**; Spalten „Aktionen · Position · Bemessung · Satz · Betrag netto · Nutzungsdauer · Worst/Best"; „Unter dem **Betrag**" (U28/#345); **vier Rasterknöpfe** und die Leiste „Abbrechen · Speichern · OK" |
| Konzept Z. 910–918 (§ 2.12) | dieselbe Dialogform in Kurzfassung | gleich nachgezogen |
| Konzept Z. 986 (§ 2.13) | „die fünf Punkte" | „fünf Punkte, dazu (6) als Verweis" |
| Konzept Z. 1112–1116 (§ 2.13) | „der Kopfabschnitt ‚Was sich ändert' führt alle fünf Punkte" | auf das abzulösende Mockup `../Mockups/Ergebnis_Bandbreite_Herkunft.html` bezogen |
| Konzept Z. 2377–2385, 2450–2455 | § 6.3 B5-Kernaufgaben 1–2, Nr. 9h | erledigt bzw. auf U39 verengt (s. Quelle 07) |
| Konzept Z. 1465–1466 (§ 2.16) | ein Anker `#pvkosten` für zwei Kategorien | Kategorie 3 unter `#pvkosten`, Kategorie 6 unter `#pv` |
| Konzept Z. 54 (Artifacts) und Z. 903 (§ 2.12) | Artifact ohne Vermerk | „**die Repo-Datei … führt**", das Artifact trägt einen älteren Zahlenstand |
| Konzept Z. 2106 (§ 3.9) | Tabelle ohne CO₂-Doppelansatz | neue Zeile „**CO₂-Bestandteil im Arbeitspreis und BEHG-Reihe gleichzeitig aktiv**" als **Soll**, mit „nicht gebaut, Etappe E2" |
| `Rechenweg/04` Z. 149, 152 | „872,3 × 65,00 = **56.699,50 €/a**" | „872,329 × 65,00 = **56.701,38 €/a**", die gerundete Variante ausdrücklich genannt |
| `Rechenweg/04` Z. 133–135 | „auf die brennwertbezogene Abrechnungsmenge gehört 181,4" | „gehört der **KATALOGWERT** 181,4 (`GESETZ_EF_BILANZ_EBEV_ERDGAS_HO`) — keine Umrechnung des Beispiels" |
| `Rechenweg/05` Z. 303 | K-1 „Schemaschritt 90" | „Schemaschritt 97" |
| `Rechenweg/05` Z. 307 | „K7 … auf 11 Spalten erweitern — B5-Kernaufgabe" | „✔ **K7** … **erledigt**: `Speichere(g, mitSteuerangaben)` schreibt 8 + 4 Spalten" |
| `LIESMICH.md` Z. 103, 104 | `../KONTEXT_…` und `../Konzept_BHKW_…` (tot) | `../../ueberholt/…`, als Geschichte gekennzeichnet; dazu der Selbstverweis Z. 3 ohne `../` |
| `LIESMICH.md` Z. 69–78 | Reiter Investition/Betrieb/Ertrag-Bonus, sieben Spalten alt, drei Fußknöpfe | Optionsgruppe, zwei Reiter, sieben Spalten neu, vier Rasterknöpfe |
| `LIESMICH.md` Z. 38–42 | Artifact ohne Vermerk | „**Die Repo-Datei führt**" |
| `LIESMICH.md` Z. 53–56 | Lesereihenfolge mit vier Schritten | um die beiden Papiere vom 19.09.2026 auf sechs Schritte ergänzt |
| `Konzept_Nutzungsdauer_AfA_EPOS-Plan.md` Z. 76 | „Schemastand: Zielversion 74" | „Schemastand: **Zielversion 96** (Stand 19.09.2026)" |

---

## 8 Übersprungene Punkte — mit Entscheidkennung

| Punkt | Quelle | Grund |
|---|---|---|
| Schnitt in drei Papiere | Analyse § 3.4 | **A13 offen** — ausdrücklich nicht beauftragt; in § 7 als offener Entscheid benannt |
| Codestand aus der Kopfzeile streichen | Mockup-Prüfung § 3.2, `02/f‑1` | **Q25 offen** — stattdessen als `e1c4275e` mit dem Zusatz „vor dem Umschreiben `922228a`" geführt |
| „Vorschlag am Feld" mit der Überlagerung U22 zusammenführen | `02/d‑2` | **Q2 offen** — Satz „Entscheid Q2 offen" an Z. 212 eingefügt, der Text bleibt |
| Beispielzahlen und Rundung des Mockups gegen die Papiere | `01/B2–B4`, Mockup-Prüfung § 4 | **Q3–Q6 offen** — nur die zwei ausdrücklich beauftragten Stellen in `Rechenweg/04` geändert; `Beispielprojekt.md` und `Rechenweg/02`/`07` nicht angefasst |
| Kopfband, Fehlerfarbe, neuer Abschnitt „Hausstil Dialoge" | `02/d‑11`, `04/§ 6` | **Q8/Q9 offen** — Satz „Entscheid Q9 offen" an Z. 516; kein neuer Hausstil-Abschnitt |
| Degradation V-E gegen G3 auflösen | `07/§ 2.11`, `05/§ 8.2` | **A5 offen** — an **beiden** Stellen als offener Entscheid vermerkt (Konzept Z. 701 und Z. 754, Szenarienkonzept Z. 271); keine Nummer und kein Entscheid geändert |
| Wortlaut des Szenario-Hinweistexts angleichen | `02/d‑18` | **A14 offen** — Satz „Entscheid A14 offen" an Z. 895 |
| „Höfingen" neutralisieren | Mockup-Prüfung § 4 | **A16 offen** — der Name bleibt in Konzept und `Rechenweg/08` unverändert |
| Hilfe/`help_mapping.txt` (`Form_BhkwWirtschaftlichkeit.btn_Help`) | `02/§ 8`, `05/§ 6` | **A18 offen**, und die Datei steht außerhalb des Auftrags |
| § 6.3 neu und lückenlos nummerieren | `07/§ 2.6` | ausdrücklich untersagt — Nummern und Durchstreichungen bleiben, erledigte Punkte werden nur gekennzeichnet |
| Zwei **neue** offene Punkte (25) und (26) in § 6.3 | `03/§ 6.2` | keine Berichtigung einer bestehenden Stelle, sondern neue Sachpunkte; gehören in die Etappenplanung |
| „Tooltip" gegen „Werkzeugtipp" vereinheitlichen | `04/n‑6` | reine Wortwahl ohne Sachfehler; nicht beauftragt |
| iOS-Erreichbarkeit des PV-Dialogs „zwei Bedingungen statt einer" | `04/§ 8.2`, Z. 1339 alt | die Stelle gehört zu § 2.16 und wäre eine neue Aussage über den Code; nicht beauftragt |
| `Status_iOS_Migration.md`, `Dokumentation/LIESMICH.md`, Mockups, `EPOS.UI/CLAUDE.md`, Wiki-Quellen, Code | Auftrag | ausdrücklich ausgenommen — keine dieser Dateien ist angefasst |

---

## 9 Abnahme

```
export PATH="/c/Program Files/dotnet:$PATH"
dotnet test EPOS.Kern.Tests/EPOS.Kern.Tests.csproj -c Release \
  --filter "FullyQualifiedName~DokumentationLinkWacheTests|FullyQualifiedName~RepositoryOrdnungWacheTests" \
  --logger "console;verbosity=minimal" -nologo
```

**Bestanden: 19 von 19, 0 Fehler.**

Der erste Lauf war **rot**: Drei neu gesetzte Code-Spannen der Form `Mockups/<name>.html` trafen
vom Ordner `Wirtschaftlichkeit_Kosten/` aus keine Datei — die Wache löst sie relativ zur
Fundstelle auf und verlangt `Dokumentation/aktuell/Mockups`. Behoben mit `3cdce7d7`
(`../Mockups/…`, wie an den übrigen sechs Stellen des Papiers); danach grün.

`git status` ist sauber; nichts ist gepusht, kein CI-Lauf ausgelöst, der Hauptbaum nicht angefasst.
