# P646 — Nachlese der Wärmegestehung (#642): Stromsteuer einmal, EZ‑6 gilt, Register, Konzept, Zielvorgabe, Altläufe (Protokoll, 02.10.2026)

Statuszeile vorläufig #649 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md) (die Orchestrierung prüft
die Nummer beim Push); Auftrag [`P646_Auftrag_2026-10-02.md`](../Auftraege_Wirtschaftlichkeit_2026-09/P646_Auftrag_2026-10-02.md)
der Sitzung „EPOS Plan Wirtschaftlichkeit". Vorgänger: [`W642_Waermegestehung_Protokoll.md`](W642_Waermegestehung_Protokoll.md)
(#642, nachgetragen mit dieser Welle) und [`P641_Laufvermerk_Protokoll.md`](P641_Laufvermerk_Protokoll.md) (#645). Zweig `p646`
ab `3112d2d69` (Auftrag auf `1aca6a44c`, origin/ios_migration_september mit #645).

## Anlass und Entscheid

Die fachliche Prüfung der Wärmegestehung (#642) vom 02.10.2026 ergab sieben Befunde: (1) die Stromsteuer zählte im Modus
ERLOES zweimal, (2) der Wärmestrom nahm den Preis des Netzträgers statt des eigenen Trägers der Anlage (EZ‑6), (3) zwei Preise
desselben Eigenstroms waren unbenannt, (4) die Zielvorgabe EZ‑9 rechnete die Gestehung mit dem Projekt-Kapitalwert, (5) die
Entscheide von #642 standen nur in Kommentaren und der Statuszeile, (6) gespeicherte Läufe zeigten unter dem neuen Kurztext
die alte Zahl, (7) Grenzen der Zuordnung waren unbenannt. Anwenderentscheid 02.10.2026 (**EZ‑21**): „Befunde wie Empfehlung
umsetzen". Kein Schemaschritt (Zielversion 158), kein Rechenweg der Simulation, keine neue Basis (R30; die Wärmegestehung
steht in keiner CSV der Basis).

## Regeln

1. **Stromsteuer einmal.** `Waermegestehung.ErloesReiheZaehlt` zählt `STROMSTEUER_BEFREIUNG` (§ 9 Abs. 1 Nr. 3 StromStG,
   nur Modus ERLOES) nicht mehr zur Gestehung: Die Stromgutschrift `Eigenstrom [MWh] × 1.000 × p_Arbeit,Netz` enthält die
   Stromsteuer schon. Erlöse_Wärme = KWK-Einspeisung + KWKG + Pauschale + Energiesteuer (+ die negative Reihe
   `STROMSTEUER_ENTLASTUNG_ENTGANGEN`). Kapitalwert und übrige Kennzahlen buchen die Reihe im Modus ERLOES unverändert.
2. **EZ‑6 in der Gestehung.** `WaermeArbeitEur = Σ_eigen min(M_eigen, Rest) × 1.000 × p_eigen + Rest × 1.000 × p_Netz`, Rest
   beginnt beim Wärmestrom des Laufs (WP + Heizstab + Elektrokessel). `M_eigen` je Erzeugerzeile
   (`Waermegestehung.WaermestromJeModul`: Strom + Heizstab je WP-Modulzeile, Stromeinsatz je Elektrokessel-Modulzeile), wenn
   die Anlage gleichen Bezeichners einen eigenen, dem Projekt zugeordneten ELECTRICITY-Träger führt, der vom bepreisenden
   Träger abweicht (`ProjektEnergietraegerCtrl.EigeneStromTraeger`, dieselbe Erkennung wie `EndenergieAufloeser`); `p_eigen`
   der Arbeitspreis dieses Trägers im Szenario (`KostenEmissionRechner.ArbeitspreisJeKwh`). Ohne eigenen Träger Zeichen für
   Zeichen `Wärmestrom × 1.000 × p_Netz` (Netz- bzw. Rückfallträger); eigener Träger ohne Arbeitspreis → `p_Netz` (§ 6.3 Nr. 43).
   Menge, Preis des Netzeintrags und Stromgutschrift bleiben beim Netzträger („Arbeitspreis bleibt").
3. **Vermerk alter Läufe.** `ErgebnisNachweisUmschlag.FASSUNG = 13` und `FASSUNG_WAERMEGESTEHUNG = 13` (12 = Umfang nur
   Wärmeerzeugung, #642; 13 = Stromsteuer einmal und eigener Träger, P646; Fassung 13 ohne neues Feld); `Uebernimm` setzt
   `WirtschaftlichkeitErgebnis.GestehungAlteFormel = Version < 13`, `LadeErgebnisse` ohne Umschlag ebenso — damit auch Läufe
   aus #642 (Nachtrag, Entscheid der Orchestrierung im Rahmen von Befund 6). Die Zeile `GESTEHUNGSKOSTEN` trägt dann den
   Warntext `WIRT_GESTEHUNG_ALTER_LAUF` („gespeicherter Lauf nach einer früheren Regel der Wärmegestehungskosten — die Zahl nach
   heutiger Regel liegt mit der nächsten Rechnung vor"): Zeichen in der Zelle der Kennzahltafel,
   Zeile unter der Tafel; Word und Excel über dieselbe Zellwarnung. Frisch gerechnet nie.

## Code

- `3c3fd1108` Punkt 1: `Waermegestehung.ErloesReiheZaehlt` (Zeile 113–137, Begründung im Kommentar), Kommentare in
  `WirtschaftlichkeitCtrl.BaueWaermeEingabe` (um Zeile 6942 und 6994); `StromsteuerBefreiungModusTests.Stundenreihen` internal.
- `a2102b5a3` Punkt 2: `Waermegestehung.EigenerStrom`, `WaermestromJeModul`, `WaermestromArbeitEur` (um Zeile 196–280);
  `KostenEmissionRechner` Netzeintrag (um Zeile 760–778) und `EigenerWaermestrom` (neu, um Zeile 1582; SQL
  `SELECT ID, Bezeichner FROM Tab_Energieanlagen WHERE ID_Projekt = ?`); Feldkommentar `EnergieTraegerNachweis.WaermeArbeitEur`.
- `15e55c75a` Punkt 6: `ErgebnisNachweisUmschlag` (Konstante, `Uebernimm`), `WirtschaftlichkeitErgebnis.GestehungAlteFormel`,
  `WirtschaftlichkeitCtrl.LadeErgebnisse`, `WirtschaftlichkeitZeilen` (Warntext der Gestehungszeile), neue Ressource
  `WIRT_GESTEHUNG_ALTER_LAUF` (de, en), Designer neu (13 527 Einträge, +1, zweiter Lauf +0); Hülle
  `WirtschaftlichkeitSeiteGaben.Matrixzeile` internal für die bUnit-Probe.

## Tests

- `WaermegestehungTests` 43 → 46 Fälle (21 → 24 Methoden): Befreiungsreihe zählt nicht; Wärmestrom je Erzeugerzeile; Preis
  des eigenen Trägers, ohne Zuordnung Netz-/Rückfallpreis bitgleich, ohne Arbeitspreis Netzpreis, Menge gekappt; Vermerk
  bei Fassung 11 und 12, nicht bei 13, nicht frisch, nicht ohne Zahl.
- `WaermegestehungAnkerTests` 6 → 8: **ERLOES** — 1030 mit den flachen Stundenreihen des Prüffalls B6, Befreiung 432,3 MWh ×
  20,50 €/MWh = 8.862,15 €/a, Kapitalwert ERLOES − AUSWEIS = 8.862,15 × RBF(3 %, 20) = 131.846,41 €, Gestehung in beiden
  Modi 0,0068420905 €/kWh, nach #642 im Modus ERLOES 0,0068421 − 8.862,15 ÷ 6.137.560 = 0,0053982 €/kWh; **eigener Träger** —
  1024 mit Elektrokessel auf Träger 58 (0,30 €/kWh), WP auf 60: Wärmestrom 43,03 × 467,46 + 52,99 × 300 = 36.011,80 €/a
  statt 44.885,51, Energie-Annuität −8.873,71 €/a (p_E = 0), Gestehung 0,0616162 → 0,0388473 €/kWh; Kapitalwert,
  Energiekosten, Gutschrift 34.549,97 €/a unverändert. **Anker 1019 0,140677, 1024 0,061616, 1030 0,006842 und die § 9b-Fälle
  unverändert** — kein Prüfstand rechnet im Modus ERLOES (Testdatenbank: `Stromst_Befreiung_Modus` leer bis auf 1048
  AUSWEIS), keiner führt einen abweichenden Stromträger (1019 und 1030 ohne `ID_Carrier` an Wärmeerzeugern, 1024 mit 0).
- `ErgebnisansichtTests` (Wöhler ohne Umschlag: Vermerk; frisch gebucht 1040–1042: keiner), bUnit
  `WirtschaftlichkeitErgebnisansichtTests` 31 → 32 (Fassung 12 zeigt Zeichen und Zeile, Fassung 13 nicht); Fassungspins
  12 → 13 in `ErgebnisansichtTests` und `WiederholperiodeTests`. Gegenprobe Nachtrag: Konstante zurück auf 12 — Kern- und
  bUnit-Probe rot.
- Gegenproben: Befreiungsreihe wieder eingehängt — Einheitstest und ERLOES-Anker rot; eigener Träger ausgehängt
  (`KostenEmissionRechner` ohne Liste, `WaermestromArbeitEur` ohne Schleife) — Einheitstest und Anker 1024 rot; Warntext
  ausgehängt — Kern- und bUnit-Probe rot. Je danach zurückgebaut.
- Läufe im Worktree (Debug, `xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`, kein fremder testhost):
  Kern-Filter 0 Fehler; Auftragsfilter `EPOS.Kern.Tests` 187 → 192, `EPOS.UI.Tests` 265 → 266, `SpeicherEngine.Tests` 48,
  alle grün — darin `WirtschaftlichkeitAnkerTests` 10, `BerichtVorlagenMesslatteTests` 7 (sechs Bericht-Messlatten
  byte-gleich), `StromGruppenregelTests` 32, `DokumentationLinkWacheTests` 9; dazu `KapitalwertAnkerZerlegungTests` 2,
  `StromsteuerBefreiungModusTests` 10, `ErgebnisansichtTests` 56; `SqlDialektPruefer` 2 129 SQL-Texte, 0 Fundstellen;
  Windows-Schale (x64, Debug, `EnableWindowsTargeting`) 0 Fehler.

## Papiere

- Register (`ac95e0ade`): EZ‑20 (die drei Entscheide von #642 im Wortlaut), EZ‑21, Vermerke an EZ‑6, EZ‑9, EZ‑17 (#644),
  Quellenabsatz, Familientafel (21), Kopf. Protokoll W642 nachgetragen, Index Reporting 150 → 152 mit diesem Protokoll.
- Konzept (`aec73f1f5`): Kopf, § 3.1 (sieben Erlösreihen, Kennzahltafel), § 3.4, § 3.6 (zwei Werte des Eigenstroms), § 3.8
  (Gestehung: Befreiung zählt nicht, § 9b-Abzug je Jahr ohne p_E), § 6.1 (#642, P646), § 6.2, § 6.3 Nr. 40–43, § 6.5.
- Zielvorgabe: Beispielprojekt § 4c nach § 3.1 nachgerechnet — Stamm = Variante 2: 2.400 + 148.262,4 − 2.885,7 =
  147.776,7 €/a → 7,56 ct/kWh; Variante 1 = Variante 3: 19.554,8 + 59.564,2 + 312.811,2 − 315.129,6 + 21.884,0 − 62.499,7 =
  36.184,9 €/a → 1,85 ct/kWh (Eigenstrom 1.094,2 MWh aus der Strommatrix, Erlöse = Barwert Block A ohne § 9b des
  Netzbezugs); Mockup `Dialog_Formel_Zahlenprobe.html` (Tafel und Sicht 2) und Rechenweg 08 mit diesen Zahlen; Einheiten-Papier
  (Fundstelle 4899) und Mockup `Ergebnis_Bandbreite_Herkunft.html` gekennzeichnet „Formel bis 30.09.2026, heute § 3.1".
- Rechenweg 08: Regel und drei Grenzen (Punkt 7). Statusdatei: Nach #612 (b) mit #644. Wiki-Quelle Wirtschaftlichkeit,
  Anker `waermegestehung`: Strompreis des eigenen Trägers, Stromsteuer einmal, Vermerk alter Läufe; Gegenlese mit dem Muster
  aus `CLAUDE.md` ohne Treffer. Logbuch 1.2.0.6: der Satz des Auftrags (Version bestätigt der Anwender beim Upload).

## Abweichungen und Lesart

- Punkt 6 „an der Kennzahl": als Zellwarnung der Kennzahl gebaut (Zeichen in der Zelle, Zeile unter der Tafel) — derselbe
  Weg wie die Warnung des Zinsfußes; der Vermerk erscheint damit auch in Word und Excel eines gespeicherten Laufs. Die
  Herleitungszeilen „Menge × Preis" stehen nur in der Gliederung, die Kennzahltafel führt keine leisen Zeilen.
- Nachtrag der Orchestrierung: Auch Läufe der Fassung 12 aus #642 tragen den Vermerk — Umschlag auf Fassung 13, § 6.3 Nr. 43
  (Teil Läufe aus #642) erledigt.
- Der zweite Mockup `Ergebnis_Bandbreite_Herkunft.html` trug dieselben alten Zahlen (mit eigener PV-Variante) — gekennzeichnet,
  nicht nachgerechnet.

## Offen

- Wiki-Upload der Quelle Wirtschaftlichkeit mit dem nächsten Sammel-Upload; Logbuch-Version beim Anwender.
- § 6.3 Nr. 40, 41 (§ 9b-Sockel, Deckelung) — nur mit Anwenderentscheid.
- Sichtabnahme der Seite im Windows-Build (Vermerk an einem gespeicherten Altlauf, z. B. Gruppe „Wöhler").

## Gate

offen (volles Gate durch die Orchestrierung).

## Commit

offen — Statuszeile, Merge und Push durch die Orchestrierung.
