# E7c3 — Vollbenutzungsstunden nach Definition, B‑6, Kapitalwert 1024, Energiesteuer-Vorschau und die Reste aus E7c2 (Protokoll, 23.09.2026)

Statuszeile #452 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Etappe E7 (Teil c3) des
Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 5 Zeile E7 — mit diesem Teil ist E7 abgeschlossen); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.2, § 3.6, § 3.7, § 3.9, § 4, § 5, § 6.2 und § 6.3 Nr. 9h; die Entscheide im
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
unter R‑E7c1 (E7c1‑Q2 präzisiert, E7c1‑Q8) und R‑E7c2 (E7c2‑Q4 Rest, Q5 b, Q7, Q8 b), die acht Fragen dieser
Etappe unter R‑E7c3. Anlass: der Anwender, „fahre fort" (23.09.2026), und seine Definition der
Vollbenutzungsstunden vom selben Tag (17:10) — Etappe E7, Teil c3. Zweig `e7c3` von `c0131b2b` (`origin`,
Schemastand 113), zwei Phasen und ein Nachzug; Phase 1: `ba9d0b13` (E7c3/5), `3e7bc8b4` (E7c3/3), `92aa069a`
(E7c3/6), `f114499b` (E7c3/7), `46291409` (E7c3/2), `d5f2d00a`, `a2863b31`, `8820ec1e`, `27e4cd7b`, `38ca5bc5`
(E7c3/1a bis E7c3/1e), `18d148a4` (E7c3/4), `5f7711a7` (E7c3/8), `cabd55a9` (E7c3/10); Phase 2: Merge `91315089`
(Arbeitszweig `8c94ee3a`), `29fbbc77` (E7c3/1f), `77618ce2` (E7c3/1g), `ffff2fe2` (E7c3/11, Testdatenbank, vom
Orchestrator committet); Nachzug: Merges `716b6449` (Arbeitszweig `39c63361`) und `c3246cb6` (Arbeitszweig
`ffc27d18`), `6ebb26b6` (E7c3/12, Testdatenbank der Z2-Fassung); nach dem Gate `035b14db` (E7c3/13,
Testerwartung). Merge `9c7a0023` auf dem Hilfszweig `pm4` (Basis `origin/ios_migration_september` = `ffc27d18`;
38 Dateien, +3 908/−400; der Baum gleicht `6ebb26b6`), dazu der Nachtrags-Merge `387c2d9f` (der Baum gleicht
`035b14db`). Opus 5.5 im Worktree `.claude/worktrees/e7c3`. Die Punktnummern folgen dem Auftrag (1 = B‑6 …
10 = Ankertests); gebaut wurde in der Folge 5, 3, 6, 7, 2, 1, 4, 8, 9, 10. Einen Schemaschritt gibt es nicht,
`SchemaStand.Zielversion` bleibt 113.

## Befund vor der Welle

- **Die Vollbenutzungsstunden in Fall 2 kamen aus dem KWK-Strom (E7c2/7, `a339a633`).** Nach E7c1‑Q2 Lesart b
  zählten sie bei einer Anlage mit Vorrichtung zur Abwärmeabfuhr aus ihrem KWK-Strom (KWK-Strom ÷ P_el), in Fall 1
  aus dem Bruttostrom; Kontingentverbrauch und Jahresdeckel liefen in Fall 2 also über Stunden nach dem
  Hilfsstromabzug, und eine Anlage mit Kennzeichen bekam bei bindendem Deckel auch ohne Kürzung einen höheren
  Jahresbetrag (Befund aus #446). Der Anwender hat am 23.09.2026 (17:10, mit Anlage) die Definition vorgegeben:
  „Vollbenutzungsstunden (siehe Anlage) — gehe nach dieser Definition": **Vbh = W_a ÷ P_Nenn** — W_a die jährlich
  erzeugte Arbeit in kWh/a (thermisch oder elektrisch), P_Nenn die installierte Nennleistung in kW (passend zur
  gewählten Arbeit); eine rechnerische Kennzahl, ein normierter Indikator für die Auslastung modulierend oder
  getaktet laufender KWK-Anlagen. Folge für die App: Kontingentverbrauch und Jahresdeckel zählen in Fall 1 und
  Fall 2 nach dem erzeugten Modulstrom (brutto) ÷ P_el,Nenn, der KWK-Strom bestimmt nur die bezahlte Menge.
  E7c2/7 wird zurückgebaut, E7c1‑Q2 b ist damit präzisiert, E7c2‑Q7 erledigt sich (der Deckel zählt die
  Bruttostunden).
- **E7c2‑Q5 b und E7c2‑Q8 b** (entschieden 23.09.2026, Bau mit E7c3): Der Satz der Speicherbewertung
  (`VpvCtKwh`) nahm den gerundeten EV-Mix; die Überlagerung „Sätze und Herkunft" zeigte Satz und Betrag der
  Energiesteuer nur für die gebuchte Wahl, die übrigen Wahlen als Text.
- **Brennstoff 24 „Sonstige"** führt seit Schritt 113 kWh (E7c2‑Q4), sein Stamm trug aber H_i = H_s = 0; ein neu
  zugeordneter Träger bekam in der Preishistorie den Heizwert 0.
- **E7c1‑Q8:** `KWKG_REALISIERUNGSFRIST` und `KWKG_STICHTAG_DAUERBETRIEB` werden seit A20 (#440) von keinem Code
  mehr gelesen, wurden aber weiter mit dem Status GESICHERT gesät.
- **Kapitalwert 1024:** gemessen −2.896.359,13 € gegen den Konzeptwert −2.220.322,32 € (−676.036,81 €); mit E1
  (#380) eingegrenzt, nicht nachgerechnet — Kandidaten Kesselbrennstoff B‑1/#331, Hilfsstrom #365/#366,
  Schemaschritte 93–96.
- **B‑6** (Konzept § 4): Fehler wurden geschluckt (`catch {}` ⇒ still 0) — in den fünf Prioritätsdateien
  `KohaerenzPruefung`, `EmissionsBilanzRechner`, `Emissionsquelle`, `GesetzKatalog` und `WirtschaftlichkeitCtrl`
  zusammen 100 leere Fänge.
- **U22:** Die Überlagerung trug alle Wahlen, die Wirkung einer Wahl stand aber als Absatz darunter; die Zeichnung
  sieht eine Zeile je Wahl vor.
- **Konzept § 6.3 Nr. 9h:** die geräteeigenen Dauerspalten und der Anschluss der Speicherflotte — A7 und A8 binden
  beides an ND‑S3 (E10); der Auftrag verlangte hier nur die Messung.

## Gebaut — Phase 1 (E7c3/1a bis E7c3/10)

- **Vollbenutzungsstunden nach Definition (E7c3/5, `ba9d0b13`).** Vbh = erzeugte Arbeit ÷ P_Nenn — die elektrische
  Arbeit brutto an den Klemmen durch die elektrische Nennleistung, in Fall 1 und Fall 2 gleich. Kontingentverbrauch,
  Jahresdeckel und Kontingentreihe zählen in Fall 2 wieder die Bruttostunden wie in Fall 1, auch auf dem Ersatzweg;
  der KWK-Strom (`min(Netto, Nutzwärme × σ)`) bestimmt allein die bezahlte Menge. Die Herleitungszeile nennt in
  Fall 2 „Vbh = erzeugte Arbeit ÷ P_Nenn = … MWh ÷ … kW = … h/a (brutto an den Klemmen, wie in Fall 1)" und den
  KWK-Strom als bezahlte Menge (`WIRT_KWKG_FALL2_VBH`, `WIRT_KWKG_FALL2_VBH_ERSATZ` neu gefasst), der KI-Feldtext
  `KI_DLG_BHW_ABWAERME_ERL` ebenso. Der Rückfall `VbhDerAnlage` ohne gespeicherte Modulzahl nimmt die
  Bruttoerzeugung statt der Nettomenge nach Hilfsstrom. `KwkgFall2Tests` stehen wieder auf den Werten vor E7c2/7,
  zwei Tests sind neu (Hilfsstrom, Rückfall).
- **E7c2‑Q5 b — der Satz der Speicherbewertung ungerundet (E7c3/3, `3e7bc8b4`).** `PvErloesRechner.VpvCtKwh` nimmt
  bei fester Einspeisevergütung den ungerundeten anzulegenden Wert abzüglich des Abschlags — denselben Satz, mit dem
  `Rechne` die Einspeisung vergütet (V‑1, A4). Der Override bleibt, die Marktprämie bleibt beim gerundeten
  anzulegenden Wert wie in `Rechne`. Zwei Tests in `PvErloesRechnerEegTests`.
- **Katalog-Generation 9 — Nachpflege statt Saat (E7c3/6, `92aa069a`).** Neu ist die Nachpflege des
  Gesetzeskatalogs (`GesetzKatalog.Nachpflege`): Generation 9 sät keine Zeile, sie pflegt bestehende — in jeder
  Datenbank mit einem Saatstand darunter genau einmal, beim Start oder beim ersten Katalogzugriff, ohne
  Schemaschritt (`AktuelleGeneration` 9, `JuengsteSaatgeneration` 8, `ZuletztNachgepflegt` als Diagnose im
  Migrationsprotokoll und in `Werkzeuge/Testdatenbankschema`). Scheitert ein Schritt, steigt der Marker nicht, und
  der Grund steht in den Saatwarnungen. Schritt 1: `Tab_Brennstoff_Stamm` ID 24 „Sonstige" H_i = H_s = 1,0, nur wo
  beide noch 0 oder leer sind. `KatalogNachpflegeTests` neu (4), Katalogpflege- und Eindeutigkeitstest auf die
  Pflegegeneration angepasst.
- **E7c1‑Q8 — zwei KWKG-Katalogzeilen abgekündigt (E7c3/7, `f114499b`).** Neuer Status ABGEKUENDIGT
  (`DbWerte.GESETZ_STATUS_ABGEKUENDIGT`, zwölf Zeichen = `CHECK` der Spalte) als vierter Eintrag in
  `GesetzKatalog.Statuswerte` neben GESICHERT, VORLAEUFIG und PROGNOSE; die Pflegemaske behält ihn beim Bearbeiten.
  Die Vorbelegung sät `KWKG_REALISIERUNGSFRIST` und `KWKG_STICHTAG_DAUERBETRIEB` weiter, mit diesem Status; in
  bestehenden Datenbanken setzt ihn die Nachpflege derselben Generation 9. Gelöscht wird nichts — Wert, Stichjahr
  und Quelle bleiben. Drei Tests in `KatalogNachpflegeTests` (Vorbelegung, Nachpflege, Wache „kein Leser im Kern"),
  die Statusliste ist auf vier Werte eingefroren.
- **Kapitalwert 1024 nachgerechnet (E7c3/2, `46291409`)** — der Befund im nächsten Abschnitt; neuer Ankertest
  `Kapitalwert_1024_mit_dem_Strompreis_vor_Schritt_83`, der Kommentar des Ankers nennt den Befund.
- **B‑6 — die fünf Prioritätsdateien ohne stille Fehler (E7c3/1a bis E7c3/1e).**
  - *Gruppe 1, `KohaerenzPruefung` (E7c3/1a, `d5f2d00a`):* Die zehn Teilprüfungen laufen weiter je für sich
    gekapselt, aber über `Teilpruefung()`: Scheitert eine, steht an ihrer Stelle die Warnung „Prüfung „X" nicht
    ausführbar: ‹Grund›" (`KOH_PRUEFUNG_NICHT_AUSFUEHRBAR`, zehn Namen `KOH_TP_*`). Die Lesehelfer
    `AnlagenMitAnteil`, `AnlagenMitHilfsenergiePosition`, `TraegerMitCo2Anteil` und `StromsteuerRoh` tragen kein
    eigenes `catch` mehr — vorher las sich ein Fehler als leere Liste bzw. „nie gepflegt" —, der Fehler erreicht die
    Teilprüfung; der Kostendialog (U31) zeigt dieselbe Zeile. Benannt bleiben drei Rückfälle: `Aktiv()` (nur
    Zahlumwandlung), `TraegerName` (Anzeige „#Id"), `T()` (nur Fehler der Ressourcensuche). Neu für alle Gruppen:
    `Fehlergrund.Text` (Fehlerart: Meldung, einzeilig, gekürzt).
  - *Gruppen 2 und 3, `EmissionsBilanzRechner` und `Emissionsquelle` (E7c3/1b, `a2863b31`):* Katalog der
    Kraftwerksparks, Laden des Ergebnisses, biogene Einstufung eines Trägers und Faktoren des Referenzkessels fingen
    still. Jetzt steht ein Katalogfehler als benannter Grund in `Katalogfehler`; ein gewählter, aber unlesbarer Park
    oder ein unlesbares Ergebnis liefert eine Bilanz, die allein den Grund nennt („Emissionsbilanz — Stufe „X"
    nicht ausführbar: ‹Grund›", `EMB_STUFE_*`); Nebenschritte hängen ihren Grund an den Hinweis, die bestimmbaren
    Zahlen bleiben. In `Emissionsquelle` bleiben die benannten Rückfälle der Lesekette (0 bzw. nicht gepflegt,
    Vorgabewerte 435 bzw. 200 g/kWh, Modus CO₂), aber ein Lesefehler steht in `Emissionsfaktoren.Lesefehler` und an
    der Herkunft („… — Brennstoffkatalog nicht lesbar: ‹Grund›", `EMQ_*`).
  - *Gruppe 4, `GesetzKatalog` (E7c3/1c, `8820ec1e`):* Ein Lesefehler der Katalogtabelle führte still in die
    Rückfallebene (Vorbelegung); jetzt nennt `GesetzKatalog.Lesefehler` den Grund, die Rückfallebene bleibt der Weg.
    Die Pflegewege melden ihr Scheitern weiter über den Rückgabewert, der Grund steht in `LetzterFehler`; die Anlage
    der Tabelle schreibt ihr Scheitern in die Saatwarnungen.
  - *Der strenge Leseweg (E7c3/1d, `27e4cd7b`):* Beim Nachmessen der Fehlerpfade zeigte sich: `DataRepository`
    wirft bei einem Abfragefehler **nie** — es meldet ihn selbst (Dialog, im Engine-Modus still in die Sammelliste)
    und liefert eine leere Tabelle bzw. null. Die benannten Fänge der Gruppen 1 bis 4 griffen deshalb nur bei
    Umwandlungsfehlern, eine unlesbare Tabelle las sich weiter wie eine leere. `StilleDb` bekommt
    `TabelleStreng`/`ScalarStreng` — dieselbe Verbindung, dieselbe Parameterübersetzung, derselbe Typ-Rückweg, aber
    der Fehler wird weitergereicht. Darüber lesen `KohaerenzPruefung`, `EmissionsBilanzRechner`, `Emissionsquelle`
    und `GesetzKatalog`; die fehlende Katalogtabelle bleibt der benannte Rückfall ohne Fehler. Dazu: die biogene
    Stufenzeile steht je Grund einmal (nicht je Modul), „Referenzkessel-Träger ohne CO₂-Faktor" nur bei echter
    Datenlücke, ein unlesbarer Stromträger heißt an der Herkunft des Netzstroms „nicht lesbar" statt „kein
    Stromträger zugeordnet". Test `Der_strenge_Leseweg_reicht_den_Fehler_weiter`.
  - *Gruppe 5, `WirtschaftlichkeitCtrl` (E7c3/1e, `38ca5bc5`):* alle 58 `catch`-Stellen benannt. Jede Rechenstufe,
    deren Scheitern still in einen Rückfall führte, schreibt an jede Ergebniszeile des Projekts eine Warnung
    „Rechenstufe „X" nicht ausführbar: ‹Grund›" (je Projekt und Grund einmal; `WIRT_STUFE_NICHT_AUSFUEHRBAR` und
    zwölf Stufennamen): Parameter, Tarif, Anlagen, elektrische Leistung, Energieträger, Heizölprüfung,
    Betriebskosten, Gesetzeskatalog, Kohärenzprüfung, KWKG-Satzherleitung, Speichern und Laden der Ergebnisse. Der
    Rückfall selbst bleibt, wie er war. Die Lesestellen dieser Stufen lesen über den strengen Weg; die schmalste
    Stufe der Anlagenleiter liest streng, die breiteren bleiben der benannte Rückfall über den Engine-Modus.
    Speichern nennt sein Scheitern an jeder Zeile (der Kapitalwert gilt, gespeichert ist er nicht); Laden setzt
    `Ladefehler` (`LadeErgebnisse`, `LadeSensitivitaet`, `LadeStromMatrix`), `SpeichereParameter` und
    `SpeichereTarif` setzen `Speicherfehler`, die Tabellenvorsorge `Vorsorgewarnung`. Die Lesehelfer fangen nur noch
    Umwandlungsfehler (`IstZahlfehler`); Ausweise ohne Kapitalwertwirkung (vermiedener Bezug, Menge, frische Basis im
    Dialog, Elektrokessel-Grund, Ergebnis-ID) tragen benannte Fänge mit Begründung. `LadeTarif` setzt bei einem
    Lesefehler `Aktiv = false` — ein halb gelesener Tarif rechnet nicht.
  - Zusammen lesen 26 Stellen über den strengen Weg. `RobustheitB6Tests` führt 17 Fälle, je auf einer eigenen Kopie
    mit umbenannter Tabelle oder kaputtem Wert. Nachgemessen auf Scratchpad-Kopien (kein `dotnet test`): neun
    Fehlerlagen — darunter eine umbenannte Anlagentabelle, ein kaputtes Datum im Parametersatz, ein Speichern mit
    Abbruch-Trigger und ein Laden mit unlesbarem Zeitstempel —, jede ergab die erwartete Zeile, ohne Fehler steht
    keine. **Rest außerhalb der Priorität:** 29 leere `catch` in 13 Dateien und fünf stille Lesestellen im
    Engine-Modus (E7c3‑Q5).
- **E7c2‑Q8 b — die Energiesteuer-Vorschau je Wahl im Kern (E7c3/4, `18d148a4`).** Der Lauf rechnet für jede Anlage
  mit Brennstoff die Energiesteuer jeder wählbaren Entlastung vor — keine, § 53 voll und energetisch, § 53a Abs. 5,
  § 54: Satz, Menge und Wirkung im ersten Jahr (`SteuerGutschriftRechner.Vorschau`, Zeilen
  `EnergiesteuerVorschauZeile`). Einen zweiten Rechenweg gibt es nicht: Es ist dieselbe Energiesteuerrechnung wie im
  Lauf, auf einer Kopie der Steuereingabe, in der allein die Wahl dieser Anlage gesetzt ist; die Wirkung ist die
  Entlastung des Projekts mit der Wahl minus der mit „keine" für diese Anlage — Sockel und Mischlage wirken wie im
  Lauf, und ihr Grund steht an der Zeile. Die Eingabe des Laufs bleibt unberührt. Die Vorschau reist im
  Nachweisumschlag (**Fassung 8**; ältere Fassungen lesen sich mit leerer Vorschau) und steht damit auch am gebuchten
  Stand. Die Überlagerung nennt je Wahl Satz und Betrag (§ 54 mit dem Sockel, den die Wahl auslöst; ohne Position
  der Grund), die Aufteilung ihren Betrag bei § 53 samt Stromanteil; ohne Vorschau stehen der Text der Vorschrift und
  ein Hinweis auf den nächsten Lauf. `WirtschaftlichkeitZeilen.Mengeneinheit` ist öffentlich (Einheit des Satzes).
  Tests `EnergiesteuerVorschauTests` (acht Fälle; die Sollwerte sind die Handproben des Rechenwegs 05), zwei Fälle
  in `BhkwSaetzeHerkunftTests`; `ErgebnisansichtTests` pinnt die Fassung alt 7, neu 8. Sieben Schlüssel
  `BHW_UEB_ES_VORSCHAU*` und `BHW_UEB_AUF_VORSCHAU_*`.
- **U22 — die Wahlen als Anzeigezeilen (E7c3/8, `5f7711a7`).** Die Überlagerung „Sätze und Herkunft" zeigt jede Wahl
  so, wie das Mockup sie zeichnet: Wahlknopf, Text und dahinter ihre Wirkung in **einer** Zeile („→ 30.000 Vbh",
  „→ 5,50 €/MWh · 26.383,5 €/a") statt eines Absatzes darunter — Anlagenart, Tatbestand, Fall des § 2 Nr. 16,
  Energiesteuerentlastung, Aufteilung, Unternehmensart und Modus. Die Wahl bleibt im Formular: Dessen Klapplisten
  stehen unverändert, gewählt wird dort oder in der Überlagerung, „Übernehmen" schreibt wie bisher (E7c3‑Q7).
  Baustein `Optionsgruppe.Wirkungen` (je Id ein Text im Label hinter der Beschriftung, ein Klick darauf wählt mit;
  Stilregel `.epos-option-wirkung` leise mit Tabellenziffern, lange Gründe brechen in der Zeile um); die
  Beschreibungen der übrigen Masken bleiben. Sichtprüfung der Zeilen mit Edge als statisches Bild. Tests
  `OptionsgruppeTests` (Wirkung in der Zeile, Wahl per Klick), `BhkwSaetzeHerkunftTests` (jede Wahl als Zeile mit
  Wirkung, Klapplisten des Formulars bleiben; die Energiesteuerzeilen aus E7c3/4 in der Zeile).
- **Konzept § 6.3 Nr. 9h — nur gemessen**, ohne Commit; die Tafel steht unten.
- **Ankertests (E7c3/10, `cabd55a9`).** Keiner der vier Anker bewegt sich: 1024 −2.896.359,13 € (Energiekosten
  188.167,18 €/a), 1030 −21.895.377,28 € (Energiekosten 1.176.906,60 €/a), Betriebskosten 99,00 €/a, Kaskade
  13.000,00 €. Kopf der Testklasse mit den Gründen, je Anker ein Vermerk „E7c3: alt = neu".

## Der Kapitalwert 1024 (E7c3/2)

Das Konzept § 6.2 führte −2.220.322,32 €, gemessen sind −2.896.359,13 € — eine Abweichung von −676.036,81 €. Sie
ist nachgerechnet und kein Codefehler; beide Anteile sind Datenstand:

| Anteil | Betrag | Herkunft |
|---|---|---|
| (1) Schemaschritt 83 (#313, 17.09.2026) | −676.495,37 € | Er hat die aktiven Strompreisanteile des Stromträgers 60 (Modus „aufgeschlüsselt": 6,440 + 2,946 + 2,050 + 0,110 + 0,200 = 11,746 ct/kWh) in den Arbeitspreis gefaltet — 35,000 → 46,746 ct/kWh; vorher rechnete die Wirtschaftlichkeit sie nicht (Projektschalter `Aufschlaege_Anwenden`). 387,12 MWh Netzbezug × 0,11746 €/kWh = 45.471,12 €/a × Rentenbarwertfaktor 14,877475 (3 %, 20 a). Belegt an den Sicherungen des Anwenders: 0,35 €/kWh am 02.09. und 14.09. (Schemastand 61 bzw. 74), 0,46746 €/kWh ab 17.09. (Schemastand 87) |
| (2) Übernahme Access → SQLite (02.09.2026) | +458,56 € | FX1: „Anker-Aktualisierung −2.219.863,76, Datenstand"; genauer aufteilen lässt sich dieser Rest ohne den Code aus der Access-Zeit nicht |
| **Summe** | **−676.036,81 €** | |

Die drei Kandidaten des Konzepts (Kesselbrennstoff B‑1/#331, Hilfsstrom #365/#366, Schemaschritte 93–96) tragen
0,00 € bei: Mit 35,000 ct/kWh rechnet der heutige Kern bitgleich −2.219.863,761540025 € — den Wert von B5 und FX1
bis FX4 (03. bis 14.09.2026). Das hält der neue Ankertest fest. Der Anker −2.896.359,13 € bleibt, mit der Herkunft
„Schritt 83 (+ Datenstand 02.09.)"; der Konzeptwert ist ersetzt (E7c3‑Q1, gebaut ist Lesart a).

## Phase 2 und der Nachzug

- **Merge `91315089`** holt den Arbeitszweig `8c94ee3a` (Kühlung KU1 Welle 2, #447 und #448 der Sitzung „Dialog
  Design"). Konflikte nur in den zwei Ressourcendateien, beide Seiten hatten ans Ende angehängt: beide Blöcke
  behalten, erst E7c3 (43 Schlüssel), dann `origin` (41 neue; drei von `origin` gestrichene bleiben gestrichen —
  `KBROW_BTN_BEARBEITEN`, `KFLT_SP_AUSLIEFERUNG`, `KLIMA_SP_SCHREIBSCHUTZ`). Git hatte das gemeinsame Ende
  `</data></root>` aus beiden Konfliktseiten gezogen; beim bloßen Zusammenfügen fehlte an der Naht ein `</data>`,
  und der Designer verlor dadurch `ADM_AW_GEWAEHLT` — ergänzt, beide Dateien als XML geprüft: je 8 091 Einträge,
  keiner doppelt. Designer neu erzeugt (8 092 Blöcke, wiederholbar). Automatisch zusammengeführt: `DbWerte.cs`,
  `Resource.Designer.cs`, `epos-ui.css`, `SchemaMigration.cs`.
- **E7c3/1f `29fbbc77`** — der Windows-Umbruch `\r\n` einer Fehlermeldung wurde zu zwei Leerzeichen; jetzt ist
  jeder Umbruch genau eines (Test `Der_Grund_nennt_Fehlerart_und_Meldung_einzeilig_und_gekuerzt`, im gefilterten
  Lauf aufgefallen). Kein Rechenweg betroffen.
- **E7c3/1g `77618ce2`** — die Blattstruktur-Wache rechnet ihre Prüfgruppe ohne Speichern. Die Gruppe trägt die
  erfundenen Projekt-Ids 9001/9002, die es in `Tab_Projekt` nicht gibt; ihr Speichern scheiterte schon immer am
  Fremdschlüssel, nur still. Seit E7c3/1e stand das als Warnzeile „Rechenstufe „Speichern der Ergebnisse" nicht
  ausführbar" an jedem Ergebnis und verschob im Excel-Blatt die Ankerzeilen um drei (voller Lauf: zwei Fälle rot).
  Die Zahlen sind dieselben, gespeichert wurde auch vorher nichts; `BerichtBlattstrukturWacheTests` und
  `ReferenzprojektTests` 22 von 22.
- **E7c3/11 `ffff2fe2` — die Testdatenbank auf Katalog-Generation 9** (vom Orchestrator committet):
  `Werkzeuge/Testdatenbankschema` pflegt die Repo-Testdatenbank (Fassung `689a0755…`) nach. Zellvergleich über
  alle 130 Tabellen: genau fünf Zellen — Brennstoff 24 H_i 0 → 1 und H_s 0 → 1, `KWKG_REALISIERUNGSFRIST` und
  `KWKG_STICHTAG_DAUERBETRIEB` Status GESICHERT → ABGEKUENDIGT, Katalog-Marker 8 → 9; Schema gleich, Integrität
  ok, ein zweiter Lauf ändert nichts. LFS-SHA-256 `db143098…`, 67 743 744 Byte; Schemastand 113 unverändert.
- **Merges `716b6449` und `c3246cb6`** holen die Arbeitszweige `39c63361` (Dialog Design #449 Stufe 4, Kühlung KU1
  Wellen 2 und 3) und `ffc27d18` (Zapfprofilgenerator Z2, #451, mit dem fiktiven Testkatalog in der Testdatenbank
  `a228185d`). Der erste ohne Konflikt (je 8 157 Einträge: 8 114 von `origin`, 43 aus E7c3; Designer 8 158
  Blöcke); Z2 kam kurz danach, deshalb ein zweiter Merge statt einem. Im zweiten Konflikte in den zwei
  Ressourcendateien — beide Blöcke behalten (43 aus E7c3, 234 aus Z2), dieselbe Naht `</data>` ergänzt, als XML
  geprüft: je **8 391** Einträge, keiner doppelt, jedes `<data>` mit genau einem `<value>`; Designer neu erzeugt
  und geprüft, **8 392** Blöcke, wiederholbar — und in der Testdatenbank, dort die Fassung von `origin` übernommen
  (LFS `fc13e3a7…`, Schemastand 113 mit dem Z2-Testkatalog). Die 8 118 Einträge, die die Nachbarsitzung nannte,
  sind die Zählung mit `grep '<data name='` samt vier Beispielen aus dem Dateikopf.
- **E7c3/12 `6ebb26b6` — die Testdatenbank der Z2-Fassung auf Katalog-Generation 9:** dasselbe Werkzeug auf der
  Fassung `fc13e3a7…`; Zellvergleich über 130 Tabellen (10 496 533 Zellen) gegen die Fassung von `origin`: genau
  dieselben fünf Zellen, der Z2-Testkatalog ist unberührt, Schema gleich, Integrität ok, ein zweiter Lauf pflegt
  nichts. LFS-SHA-256 `9df1b7a5…`, 67 751 936 Byte. Sie ersetzt E7c3/11. Der Nachtrag „Katalog-Generation 9" in
  `Referenzlaeufe/LIESMICH.md` steht mit den Papieren zu #452.
- **Merge `9c7a0023`** auf dem Hilfszweig `pm4` über `ffc27d18` (38 Dateien, +3 908/−400, der Baum gleicht
  `6ebb26b6`).
- **E7c3/13 `035b14db` und Nachtrags-Merge `387c2d9f`.** Der erste Gate-Lauf auf dem Baum von `9c7a0023` zeigte
  genau einen roten Fall: `GaseNormkubikmeterTests.Die_fuenf_Gase_fuehren_Nm3_und_der_Brennstoff_24_kWh` erwartete
  noch H_i = H_s = 0 (E7c2/10: Schritt 113 zieht nur den Stammtext), die Testdatenbank steht seit E7c3/12 aber auf
  Generation 9. Der Test ruft jetzt `GesetzKatalog.StelleKatalogSicher` (auf Generation 9 ohne Wirkung, auf einer
  älteren Kopie die Nachpflege) und erwartet 1,0; Kommentar alt/neu mit Grund. Nachgemessen: die Gase-,
  Robustheits-, Katalog- und Generationstests und die Schemastand-Wache 164 von 164. Der Nachtrags-Merge
  `387c2d9f` auf `pm4` trägt die Korrektur; gepusht werden `9c7a0023` und `387c2d9f` zusammen.

## Fragen aus der Etappe

**Entschieden und umgesetzt — die Vbh-Definition** (Anwender 23.09.2026, 17:10, mit Anlage): „Vollbenutzungsstunden
(siehe Anlage) — gehe nach dieser Definition": Vbh = W_a ÷ P_Nenn, die erzeugte Arbeit brutto durch die
Nennleistung, in Fall 1 und Fall 2 gleich — gebaut E7c3/5. Sie präzisiert E7c1‑Q2 b (Laufzeit des BHKW,
hochgerechnet auf Vollbenutzungsstunden) und erledigt E7c2‑Q7 (der Deckel zählt die Bruttostunden); beides im
Register unter R‑E7c1 und R‑E7c2.

Der Phase‑1-Bericht nennt acht Fragen; sie stehen im Entscheidungsregister als **R‑E7c3** und sind **offen** —
gebaut ist jeweils Lesart a.

| Frage | Lesarten | Empfehlung | Stand |
|---|---|---|---|
| **E7c3‑Q1** Kapitalwert 1024 | (a) die Konzepttafel auf den gemessenen Wert — es gilt ein einziger Strompreis, und die Anteile waren am Träger aktiv; (b) der Projektschalter war aus, dann müsste 1024 in der Testdatenbank auf 35 ct/kWh zurück (Datenkorrektur, Anker −2.219.863,76 €) | a | offen — gebaut ist a (E7c3/2, der Anker bleibt) |
| **E7c3‑Q2** Katalog-Generation 9 | (a) als Nachpflege beim Start bzw. beim ersten Katalogzugriff; (b) als Schemaschritt 114 | a | offen — gebaut ist a (E7c3/6, E7c3/7); die Testdatenbank steht auf Generation 9, die Tests gehen in beiden Fällen |
| **E7c3‑Q3** Status ABGEKUENDIGT | (a) als vierter Status; (b) nur als Vermerk in der Quelle | a | offen — gebaut ist a (E7c3/7) |
| **E7c3‑Q4** strenger Leseweg (B‑6) | (a) `StilleDb.TabelleStreng`/`ScalarStreng` reichen den Fehler weiter; (b) bei `DataRepository` bleiben und nur leere Tabellen erkennen — dann geht der Grund verloren, und der Dialog bleibt | a | offen — gebaut ist a (E7c3/1d, E7c3/1e) |
| **E7c3‑Q5** B‑6-Rest außerhalb der Priorität (29 `catch` in 13 Dateien, fünf stille Lesestellen im Engine-Modus) | (a) eine eigene kleine Etappe; (b) so lassen | a | offen — nicht gebaut |
| **E7c3‑Q6** `Ladefehler`, `Speicherfehler`, `Vorsorgewarnung` in der Oberfläche | (a) in Statuszeile und BHKW-Dialog zeigen; (b) nur im Kern lassen | a, in der nächsten Welle | offen — gebaut sind die Eigenschaften im Kern, die Oberfläche zeigt sie nicht |
| **E7c3‑Q7** U22 | (a) Zeilen in der Überlagerung, die Klapplisten im Formular bleiben; (b) wie die ursprüngliche Zeichnung von U22 — die Klapplisten des Formulars werden Anzeigezeilen | a | offen — gebaut ist a (E7c3/8) |
| **E7c3‑Q8** Vorschau bei „Projektvorgabe" mit mehreren Anlagen | (a) je Anlage; (b) zusätzlich eine Projektvorschau | a | offen — gebaut ist a (E7c3/4) |

Der Zwischenbericht nannte noch eine Frage zum Rückfall `VbhDerAnlage` (ohne gespeicherte Modulzahl nimmt er den
Bruttostrom; Empfehlung: so lassen, kein Basisprojekt betroffen) — sie ist in der Liste der acht nicht mehr
enthalten und steht hier als Abweichung (unten).

## A/B-Nachweis

Gemessen mit dem Messprogramm des Agenten auf Kopien der Testdatenbank (Schemastand 113) im Scratchpad; verglichen
wird jeweils mit dem vorigen Commit, nach jedem Punkt die dreizehn Basisprojekte.

**Die dreizehn Basisprojekte:**

| Projekt | Größe | vorher | nachher | Grund |
|---|---|---|---|---|
| alle dreizehn | Anker, frischer Lauf, KWKG- und Ersatzreihen, Energiekosten, Emissionen — nach jedem der Punkte | — | 9.519 von 9.519 Werten gleich | kein Projekt trägt das Kennzeichen „Vorrichtung zur Abwärmeabfuhr", den PV-Vergütungsdialog oder den Brennstoff 24; im Bestand scheitert keine Stufe; Punkt 4 fügt allein die Vorschauzeilen hinzu |
| 1024 | Kapitalwert (Anker) | −2.896.359,13 € | gleich | — |
| 1030 | Kapitalwert (Anker) | −21.895.377,28 € | gleich | — |
| 1024 | Betriebskosten (Anker) | 99,00 €/a | gleich | — |
| 1042 | Kaskade (Anker) | 13.000,00 € | gleich | — |

**Proben** (18 Proben, außerhalb der gewollten Änderungen aus Punkt 5 Wert für Wert gleich; dazu die
Katalogmessung):

| Punkt | Probe | vorher | nachher | Grund |
|---|---|---|---|---|
| 5 | 1030, σ gepflegt 0,5: Vollbenutzungsstunden | 6.055,2 h/a | 7.475,69 h/a | brutto aus der erzeugten Arbeit |
| 5 | ebenso: KWKG Jahr 1 · Kapitalwert | 7.316,03 € · −21.895.376,67 € | 6.137,94 € · −21.904.948,06 € | die Werte vor E7c2/7; frisch gerechnet 7.316,95 → 6.138,92 € |
| 5 | σ berechnet, dazu 100 MWh Wärmeüberschuss: KWKG Jahr 1 | 7.316,03 € | 6.448,21 € | ebenso |
| 5 | Ersatzweg, σ 0,5: KWKG Jahr 1 | 7.316,00 € | 6.395,35 € | ebenso |
| 5 | ohne Deckel: Kapitalwert | −21.890.762,63 € | −21.900.597,13 € | das Kontingent zählt die Bruttostunden |
| 5 | Rückfall ohne gespeicherte Modulzahl, 5 % Hilfsstrom: Vbh · KWKG Jahr 1 | 6.613,42 h/a (netto) · 7.316,00 € | 7.475,6 h/a (brutto) · 6.600,94 € | = Weg mit gespeicherter Modulzahl (6.600,90 €) |
| 3 | Rechner, Inbetriebnahme 08/2026, `VpvCtKwh` bei fester Vergütung: 30 kWp · 40 kWp · 100 kWp · 750 kWp · Volleinspeisung 40 kWp | 7,01 · 6,92 · 6,03 · 5,52 · 10,74 ct/kWh | 7,006667 · 6,92 · 6,032 · 5,518933 · 10,735 ct/kWh | ungerundeter Mix, jetzt gleich dem `EvCt` der Erlösreihe |
| 6 | Testdatenbank: Brennstoff 24 H_i/H_s · Katalog-Marker | 0/0 · 8 | 1,0/1,0 · 9 | 227 Katalogzeilen unverändert, ein zweiter Lauf pflegt nichts |
| 7 | `KWKG_REALISIERUNGSFRIST`, `KWKG_STICHTAG_DAUERBETRIEB` | GESICHERT | ABGEKUENDIGT | Werte 4 und 2026 bleiben |
| 2 | 1024 mit 35,000 ct/kWh | — | −2.219.863,761540025 € | bitgleich mit B5 und FX1 bis FX4 |
| 1 | Basisprojekte und Proben nach jeder der Gruppen 1 bis 5 | — | gleich | im Bestand scheitert keine Teilprüfung und keine Stufe; neun Fehlerlagen nachgestellt, jede mit der erwarteten Zeile |
| 4 | Handprobe Rechenweg 05, Vorschau je Wahl im Jahr 1: § 53 voll · § 53 energetisch (Anteil 0,458) · § 53a Abs. 5 · § 54 (nach 250 € Sockel) | — | 26.383,46 € · 12.079,17 € · 21.202,71 € · 6.369,85 € | `EnergiesteuerVorschauTests` |
| 4 | 1030 mit § 53 für das Projekt: Zeile der gebuchten Wahl | — | je Anlage gleich dem gebuchten Betrag; Rundweg über die Datenbank gleich | Ausweis unverändert |

## Konzept § 6.3 Nr. 9h — die Messung

| Tabelle | Nutzungsdauer gepflegt (Testdatenbank) | wer liest sie |
|---|---|---|
| `Tab_BHKW` | 3 von 6 (10 a); Stamm 44 von 79 | kein Leser in der Wirtschaftlichkeit |
| `Tab_Heizkessel` | 1 von 22 (20 a); Stamm 0 von 63 | kein Leser in der Wirtschaftlichkeit |
| `Tab_StromspeicherVariante` | 13 von 13 (20 a) | rechnet in der Speicherwirtschaftlichkeit und im Peak-Shaving |

Die Nutzungsdauer-Tabelle führt abweichende Werte: BHKW-Modul 15 a, Batterie 10 a. Die Speicherflotte von 1046
führt zwei Einheiten mit Ersatzintervall 10 a und Restwert 500 bzw. 300 €. **Vorschlag für ND‑S3:** A8 — die
Gerätespalten nur als „Gerätedaten" kennzeichnen; Speichervariante und Flottenintervall — die Tabelle nur als Vorgabe
für neue Einträge nehmen, das bewegt nichts; ein linearer Restwert aus der Nutzungsdauer erst mit ND‑S3 und einer neu
eingefrorenen Basis für 1046 (Einfrierregel).

## Zahlen und Abnahme

- **Im Worktree `e7c3`** (Phase 2, nach dem Merge `91315089`): Kern-Filter 0 Fehler, Windows-Schale 0 Fehler (vier
  Warnungen aus dem Bestand); gefiltert Kern 680 von 681 (rot der Umbruch-Fall, nach E7c3/1f `RobustheitB6Tests`
  17/17), Oberfläche 359/359; voller Lauf 1 zwei Fälle rot (Blattstruktur-Wache, nach E7c3/1g 22/22); **voller
  Lauf 2 (19:11–19:14) 0 Fehler** — EPOS.Kern 5 175, EPOS.UI 5 471, KiKern 524, SpeicherEngine 386, SpeicherPlanung
  27 und 1 übersprungen. Referenzlauf 13/13 PASS gegen `2026-09-23_R12_Gebaeudemodell`, 4 250 839 Werte in der
  Toleranz, 399/399 CSV byte-gleich — mit der Repo-Datenbank und mit der auf Generation 9 gezogenen Kopie; Designer
  unverändert und wiederholbar; SQL-Prüfer 1 714 Texte, 0 Fundstellen. Der erste volle Lauf (18:53) startete,
  während ein Testlauf aus dem Worktree `z2` lief; der Agent hat nur seinen eigenen Prozessbaum beendet und danach
  gewartet — es zählen nur die Läufe danach.
- **Nach dem Nachzug** (E7c3/12): Kern-Filter und Windows-Schale 0 Fehler; gefiltert Kern 840/840 (E7c3-Klassen,
  alle Wachen, Migrations- und Schemastand-Tests, dazu Zapfprofil- und TWW-Tests wegen der neuen Datenbank),
  Oberfläche 213/213; Referenzlauf 13/13 PASS gegen R12, 399/399 CSV byte-gleich.
- **Gate #452** auf dem Baum von `9c7a0023` (Worktree `e7c3`, 19:34–19:39), vervollständigt nach E7c3/13
  (Nachtrags-Merge `387c2d9f`): Kern-Filter (Release) 0 Fehler; ChartProben 145 Bilder geprüft, 0 Verstöße;
  Tests Kern-Filter 0 Fehler, **11 791 bestanden, 1 übersprungen** (EPOS.Kern 5 293 nach der Korrektur, EPOS.UI
  5 561, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen); Dokumentationswachen 26/26;
  Referenzlauf 13/13 PASS gegen `2026-09-23_R12_Gebaeudemodell`, 4 250 839 Werte in der Toleranz, 399/399 CSV
  byte-gleich — die Basis bleibt R12; SQL-Prüfer 1 728 Texte, 0 Fundstellen; Windows-Schale 0 Fehler. Im ersten
  Lauf stand EPOS.Kern bei 5 292 von 5 293 (der Gase-Test, oben).
- **Ressourcen** (de und en): **43 neu** — sieben Vorschauzeilen der Überlagerung (`BHW_UEB_ES_VORSCHAU`,
  `BHW_UEB_ES_VORSCHAU_54`, `BHW_UEB_ES_VORSCHAU_GRUND`, `BHW_UEB_ES_VORSCHAU_OHNE`,
  `BHW_UEB_ES_VORSCHAU_OHNE_SATZ`, `BHW_UEB_AUF_VORSCHAU_VOLL`, `BHW_UEB_AUF_VORSCHAU_ENERGETISCH`); elf der
  Kohärenzprüfung (`KOH_PRUEFUNG_NICHT_AUSFUEHRBAR` und zehn `KOH_TP_*`); dreizehn der Wirtschaftlichkeit
  (`WIRT_STUFE_NICHT_AUSFUEHRBAR` und zwölf `WIRT_STUFE_*`); fünf der Emissionsbilanz (`EMB_STUFE_NICHT_AUSFUEHRBAR`,
  `EMB_STUFE_KATALOG`, `EMB_STUFE_ERGEBNIS`, `EMB_STUFE_BIOGEN`, `EMB_STUFE_REFKESSEL`); sieben der
  Emissionsquelle (`EMQ_NICHT_LESBAR` und sechs `EMQ_WAS_*`). **Drei geändert:** `WIRT_KWKG_FALL2_VBH`,
  `WIRT_KWKG_FALL2_VBH_ERSATZ` (die Formel „Vbh = erzeugte Arbeit ÷ P_Nenn … (brutto an den Klemmen, wie in Fall 1)
  …; der KWK-Strom … bestimmt allein die bezahlte Menge") und `KI_DLG_BHW_ABWAERME_ERL`. Keiner gestrichen. Je
  Sprache 8 348 → 8 391 Einträge über `ffc27d18`; Designer 8 392 Blöcke.
- **Kein Schemaschritt;** `SchemaStand.Zielversion` = 113, der nächste freie Schritt bleibt **114**. Die
  Katalog-Generation 9 ist Nachpflege (E7c3‑Q2).

## Abnahme am Gerät (A‑E7c3‑1, Windows und iPad)

(1) BHKW-Dialog, „Sätze und Herkunft…" und „Wahl und Herkunft…": jede Wahl — Anlagenart, Tatbestand, Fall des § 2
Nr. 16, Energiesteuerentlastung, Aufteilung, Unternehmensart, Modus — als eine Zeile mit Wahlknopf, Text und der
Wirkung dahinter („→ 30.000 Vbh", „→ 5,50 €/MWh · 26.383,5 €/a"); ein Klick auf Text oder Wirkung wählt mit, lange
Gründe brechen in der Zeile um; die Klapplisten des Formulars stehen unverändert daneben. (2) Nach „Berechnen" die
Energiesteuer-Vorschau je Wahl — keine, § 53 voll, § 53 energetisch, § 53a Abs. 5, § 54 (mit dem Sockel) — mit Satz
und Betrag im ersten Jahr; die gebuchte Wahl nennt den gebuchten Betrag; ein gespeicherter Lauf der Fassung 7 zeigt
den Text der Vorschrift und den Hinweis auf den nächsten Lauf. (3) Warnzeile: auf einer Kopie der Datenbank eine
Tabelle umbenennen (etwa `Tab_Energieanlagen`) und rechnen — die Kohärenzzeilen nennen „Prüfung „…" nicht
ausführbar: ‹Grund›" bzw. „Rechenstufe „…" nicht ausführbar: ‹Grund›" als Warnung, auf der Seite, im Wort- und im
Tabellenbericht; ohne Fehler keine solche Zeile. (4) Herleitung einer Anlage mit Fall 2: die Zeile „Vbh = erzeugte
Arbeit ÷ P_Nenn = … MWh ÷ … kW = … h/a (brutto an den Klemmen, wie in Fall 1)" und der KWK-Strom als bezahlte
Menge. (5) Gesetzliche Parameter: die zwei KWKG-Zeilen mit dem Status „ABGEKUENDIGT", der Status bleibt beim
Bearbeiten; Brennstoffkatalog: „Sonstige" mit Heizwert und Brennwert 1,0. (6) Englisch.

## Befunde nebenbei

- **`DataRepository` wirft nie.** Ein Abfragefehler erscheint als Dialog oder, im Engine-Modus, still in der
  Sammelliste; der Aufrufer bekommt eine leere Tabelle bzw. null. Jeder Fang um einen `DataRepository`-Aufruf sieht
  deshalb nur Umwandlungsfehler — der strenge Leseweg (E7c3/1d) ist der Weg, einen Lesefehler als Grund zu erhalten
  (E7c3‑Q4).
- **Der B‑6-Rest:** 29 leere `catch` in 13 Dateien außerhalb der fünf Prioritätsdateien und fünf stille Lesestellen
  im Engine-Modus (E7c3‑Q5).
- **Nachweisfassung 8.** Die Energiesteuer-Vorschau hebt den Umschlag von 7 auf 8; ein älterer Umschlag liest sich
  mit leerer Vorschau, die Überlagerung nennt dann den Grund. `ErgebnisansichtTests` pinnt die Fassung.
- **Abweichungen vom vorherigen Verhalten:** Ein Lesefehler in den fünf Dateien erscheint als Zeile am Ergebnis statt
  als Dialog; eine fehlende Spalte `Hilfsenergie_Anteil` ist ein Lesefehler statt „kein Anteil" (nur Datenbanken
  vor Schritt 61, die den Kern nicht mehr erreichen); ein nicht lesbarer Tarif gilt als nicht aktiv; ohne
  gespeicherte Modul-Vbh rechnet der Rückfall mit dem Bruttostrom.
- **Die Testerwartung aus E7c2/10 stand gegen Generation 9.** `GaseNormkubikmeterTests` erwartete beim Brennstoff 24
  H_i = H_s = 0; mit der auf Generation 9 gezogenen Testdatenbank (E7c3/12) war der Fall im Gate rot, behoben mit
  E7c3/13 (`035b14db`, Nachtrags-Merge `387c2d9f`). Im vollen Lauf der Phase 2 war der Fall grün, weil die
  Repo-Datenbank dort noch auf Generation 8 stand.
- **Die Naht `</data>` beim Zusammenführen der Ressourcen** — zweimal: Git zieht das gemeinsame Ende `</data></root>`
  aus beiden Konfliktseiten; wer die Blöcke nur aneinanderhängt, verliert an der Naht ein `</data>` und im Designer
  einen Schlüssel. Nach jedem solchen Merge die Dateien als XML prüfen und den Designer neu erzeugen.
- **Die Blattstruktur-Wache** rechnete ihre Prüfgruppe mit erfundenen Projekt-Ids und speicherte — still
  gescheitert, bis B‑6 es sichtbar machte (E7c3/1g).
- **161 Kopieordner `epos-kerntest-*` in `%TEMP%`** (rund 11 GB, 18:02 bis 19:01, ohne Besitzmarke, vermutlich aus
  den Läufen des Worktrees `z2` vor dem Leck-Fix); der nächste Testlauf räumt sie nach zwei Stunden Schonfrist.
  Gelöscht ist nichts.
- **Das Grundlagenpapier KWKG** führt die Definition der Vollbenutzungsstunden nicht als Programmregel und ist
  unverändert; sein Absatz „Folge für die Software" in § 1.2a nennt noch den Stand vor #440 (nur Fall 1, weder
  Kennzeichen noch Stromkennzahl).
- **Mockup, Kategorie 4:** Der Kasten „Der Nachweisumschlag und seine Grenzen" nennt die Fassung 3 und vier Listen
  mit sieben Skalaren — Stand vor U7 (#432); heute ist die Fassung 8.

## Offen

- **Die acht Fragen E7c3‑Q1 bis E7c3‑Q8** beim Anwender (Register R‑E7c3; gebaut ist jeweils a, Q6 a wäre ein Bau
  der nächsten Welle).
- **Abnahme am Gerät** A‑E7c3‑1 (sechs Punkte oben).
- **Nächste Etappe: E8** — die fünf ValERI-Blöcke vollständig (dazu Block 2 und das Zahlungsstrombild U42), die
  Formelmappe Stufen 0 bis 3, die Anhang-E-Checkliste (U43), die Anhang-D-Gegenprobe, Nominalsummen und
  Brückenbild der Gliederung, „Was daraus im Lauf wird" und die Fußzeile (U41, U46 bis U48) und E6‑Q1 (Verlauf und
  Spannenbild in Block 4); vorab die ClosedXML-Fragen aus dem Prüfprotokoll `05/§ 3.3` des Analysepapiers. E7 ist
  mit diesem Teil abgeschlossen; nach Entscheid kommen der B‑6-Rest (E7c3‑Q5) und die Anzeige der drei
  Kerneigenschaften (E7c3‑Q6) dazu.
- **Push** nach der Regel des Anwenders ohne Rückfrage aus dem Hilfszweig `pm4` (`9c7a0023` und `387c2d9f` samt
  dieser Papiere) auf `ios_migration_september` und `main`.
- **Papiere mit der Statuszeile:** Register (E7c1‑Q2 präzisiert, E7c1‑Q6 und Q8, E7c2‑Q4, Q5, Q7 und Q8, neue
  Familie R‑E7c3), Konzept (Kopf, § 2.2, § 3.6, § 3.7, § 3.9, § 3.10, § 4, § 5, § 6.1, § 6.2, § 6.3 Nr. 9h, § 7 und
  Anhang), Protokoll der Entscheidwege (§ 8.11, § 8.12, § 0.5 und Kopf), Analysepapier (Kopf, Nachtrag, § 5),
  Rechenwege 04, 05 und 06, Mockup (Ressourcentafeln der Kategorien 4, 5 und 8, U22, U39, Stand-Absatz),
  Logbuch-Sätze und die Wiki-Quelle der Seite Wirtschaftlichkeit, der Nachtrag „Katalog-Generation 9" in
  `Referenzlaeufe/LIESMICH.md`; die Berichtigung der Vbh-Aussagen aus #440 und #446 in der Statusdatei (Nach #440
  (a), Nach #446 (a), „präzisiert #452").
