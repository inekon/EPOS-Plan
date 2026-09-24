# E16 — Wiederholperiode je Kostenposition: „alle n Jahre" nach DIN EN 17463, 6.3.1, Schemaschritt 129 (Protokoll, 24.09.2026)

Statuszeile #484 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Auftrag des Anwenders vom
24.09.2026 („V‑G3 n‑jährliche Zeitpunkte: Ausbau der Bemessung an den Kostenpositionen (eine Wiederholperiode je
Position plus Rechenweg und Ausweis), ebenfalls mit Schemaspalte"). Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.11.2 (Lücke V‑G3), § 2.11.4 (V‑E), § 2.13 (3), § 3.1, § 3.4;
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑V (V‑G3) und R‑E16 (neu); Rechenweg
[`08_Wirtschaftlichkeit_Nutzungsdauer.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Rechenweg/08_Wirtschaftlichkeit_Nutzungsdauer.md);
Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5 und § 6; Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Kategorie 2 (Zone „Zeileneditor der
Betriebsseite", Ressourcentafel), Kategorie 8 (Zone „Bericht und Ausgabe", Ressourcentafel) und Stand-Absatz des
Anhangs. Vorgänger: [`E17_Nicht_monetaere_Wirkungen_Protokoll.md`](E17_Nicht_monetaere_Wirkungen_Protokoll.md). Zweig
`e16` von `87dd9fd4` (`origin` nach dem Push von #479), Opus 5.5 im Worktree `.claude/worktrees/e16`, zwei Phasen:
`68a4915a` (E16/1), `7572c0cf` (E16/2), `b8ff3931` (E16/3), `c5ac83f7` (E16/4), `43776f61` (E16/5) in Phase 1; in
Phase 2 die Zusammenführung `cb06a7c3` mit `origin` = `3c2f1752` (#488), `de49dec9` (E16/6), die Zusammenführung
`6ddb0132` mit `origin` = `c778ab12` (Anlagenkopplung AK1 Welle 3 mit Schemaschritt 128, die Entscheide E14/E15/E17),
`37538993` (E16/7) und `7c8f4fc4` (E16/8). Merge `ae7b0ed0` („Merge e16: Wiederholperiode je Kostenposition - alle n
Jahre nach DIN EN 17463 6.3.1, Schritt 129 (#484)") über `c778ab12`. Basis `2026-09-24_R14_Kaelteerzeuger`.
**Schemaschritt 129, ohne Pflege ergebnisneutral** — keine Zeile der Testdatenbank pflegt eine Periode, die Basis
bleibt. Mit E16 ist die Gap-Tafel V‑G des Konzepts (§ 2.11.2) geschlossen.

## Befund vor der Welle

Die Norm kennt vier Zeitpunktarten eines Zahlungsstroms: Periode 0, jährlich, alle n Jahre und einmalig im Jahr k
(DIN EN 17463, 6.3.1). EPOS bildete drei davon ab — die Investition in Periode 0, die jährliche Betriebsposition und
über das Startjahr die einmalige Zahlung bzw. den späteren Beginn —, dazu die Ersatzkette über die Nutzungsdauer. Die
Lücke V‑G3 stand im Konzept (§ 2.11.2) auf „teilweise": Eine Betriebsposition, die nur alle n Jahre fällig wird (etwa
eine Dichtheitsprüfung alle 2 Jahre), ließ sich nur als jährlicher Durchschnitt erfassen. E9 hatte V‑G3 nicht gebaut
(E9a‑Q6 a, → Register R‑E9a); der Anwender gab die Lücke am 24.09.2026 als eigenen Auftrag frei.

## Gebaut

- **E16/1 — Schemaschritt 129** (in Phase 1 vorläufig 128): die Spalte `Wiederholperiode_a` (INTEGER, nullbar, ohne
  Vorgabe) an `Tab_ProjektWerte` und `Tab_KostenVorlagePosition`; leer, 0 und 1 heißen jährlich — reines DDL. Die
  Zahl steht allein in `WiederholperiodeSchema.SCHRITT` (`EPOS.Kern/Allgemein/Update/WiederholperiodeSchema.cs`);
  `SchemaStand.Zielversion`, `SchemaMigration.SCHRITT_WIEDERHOLPERIODE` (Methode `Schritt_Wiederholperiode`), das
  Werkzeug `Werkzeuge/Testdatenbankschema` und die Testvorrichtung `EPOS.Kern.Tests/TestDatenbank.cs` leiten sich
  davon ab. Werkzeug-Probelauf in Phase 1 auf einer Kopie: zwei Spalten neu, SQL-Prüfer 1.823/0.
- **E16/2 — Kern:** `KapitalwertRechner` führt die Klasse `Wiederholposten` (Betrag je Zahlung, Preisstand Jahr 1,
  Startjahr, Periode, Topf) und die eine Regel `ZahltImJahr(startJahr, periode, t)`: s = Startjahr, ohne Startjahr
  (≤ 1) das Jahr 1; gezahlt wird in den Jahren s, s + n, s + 2n … ≤ T (E16‑Q1 a), fortgeschrieben im Zahlungsjahr
  wie eine jährliche Position desselben Topfes mit p_B bzw. — im Endenergie-Topf — mit p_E. `Rechne(… wiederholt)`
  nimmt die Liste optional; ohne Liste läuft der alte Weg Zeichen für Zeichen, das `Zahlungsbild` weist die Liste als
  `Wiederholt` aus. `WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe` legt eine Position mit n ≥ 2 in
  `BetriebsTopfe.Wiederholt` statt in die Summen der Töpfe, neu sind `WiederholtErstesJahr` und `ErstesJahr`;
  `Gesamt`, `ProjektEingabe.Wiederholt`, `BaueEingabe` und die Kopie „ohne KWKG" führen die Liste mit. Die
  Sensitivität (`RechneBild`) skaliert den p_E-Anteil mit dem Energiefaktor und rechnet den investitionsgekoppelten
  Anteil additiv. `BetriebskostenJahr` zählt eine Periode mit Startjahr ≤ 1 in die Betriebskosten p. a. (die
  Jahr‑1-Zahl), eine spätere nicht (E16‑Q3 a). Nur Betriebspositionen tragen eine Periode (E16‑Q2 a) — eine Investition
  „alle n Jahre" ist die Ersatzkette über die Nutzungsdauer. Der Controller `EPOS.Kern/Controller/Wiederholperiode.cs`
  ist der eine Lese- und Schreibweg für Dialog, Vorlagenübernahme und Kostenwelt, tolerant gegen eine Datenbank ohne
  Spalte (dann jährlich); Obergrenze 99. Ausweis: `LiesBetriebskostenPositionen` trägt die Periode,
  `KostenPositionNachweis.Wiederholperiode` (nur bei n ≥ 2 geschrieben), **Nachweisumschlag Fassung 10 → 11**;
  `WirtschaftlichkeitZeilen.HerleitungZeile` nennt in der Herleitungsspalte „alle n Jahre ab Jahr X" (ohne Startjahr
  X = 1), neben „ab Jahr X" mit „ · " verbunden.
- **E16/3 — Dialog:** Der Zeileneditor „Position bearbeiten" (`VorlagenPositionDialog`) führt auf der Betriebsseite
  das Ganzzahlfeld „Zahlung alle: [n] Jahre" (1 … 99, Vorgabe 1; leer, 0 oder negativ heißt 1, darüber wird geklemmt)
  mit der Zeile „1 = jährlich. Ab 2 zahlt die Position im Startjahr und danach alle n Jahre (DIN EN 17463, 6.3.1)." und
  bei n ≥ 2 der Herleitung „alle n Jahre ab Jahr X" aus dem Kern (Startjahr der Projektzeile). Das Feld steht nur auf
  der Betriebsseite und nur, wo die Datenbank die Spalte führt. `VorlagenPositionErgebnis` und
  `VorlagenPositionKiSicht` tragen die Periode, `KostenKomponenteHuelle.EditorGaben`/`EditorFertig` reichen sie durch;
  mitgeführt in `KostenVorlagenCtrl` (Kostenvorlagen), `KostenVorlagenUebernahmeCtrl` („Aus Vorlage übernehmen…" aus
  einer Vorlage und aus einer anderen Anlage) und `KostenProjektPositionenCtrl` (Kategorie 2). Der Hilfe-Assistent
  kennt das Feld `wiederholperiode` (`KiDialoge`, `KiDialogTexte`); Maskenwache `VorlagenPositionDialog` 8 → 9,
  KI-Dialogkatalog „acht" → „neun" Felder.
- **E16/4 — Ausweis in der Formelmappe:** `ExcelFormelmappe.Mehrjahrestabelle` führt je Topf, der eine Periode trägt,
  eine Hilfsspalte „Positionen alle n Jahre mit p_B [€/a]" bzw. „… mit p_E [€/a]" mit
  `IF(AND(Jahr>=s,MOD(Jahr-s,n)=0),Betrag,0)` je Position; die Basisspalte trägt den jährlichen Rest, die
  Betriebszelle rechnet `(Basis+Wiederholt)*(1+p)^(Jahr-1)`; gegengerechnet gegen das Zahlungsbild. Ohne Periode steht
  keine Spalte da, die Mappe bleibt die von vorher.
- **E16/5 — Tests:** `WiederholperiodeTests` (Kern, 17 Fakten und Theorien mit 16 Datenzeilen, 31 Fälle): der Schritt
  an beiden Tabellen, die eine Quelle für Migration, Werkzeug und Testdatenbank, die Spalte leer auf dem Stand des
  Schritts, die Zahlungsjahre als Tafel, die Normierung, jährlich bitgleich, alle zwei Jahre in 1, 3, 5 … gleich der
  Handrechnung, Startjahr 3 in 3, 5, 7 … im eigenen Topf, am Rand höchstens eine Zahlung, Schreiben und Lesen,
  Vorlagenübernahme, Umschlag, Herleitungsspalte, Gliederung und Berichte, Kapitalwert des Laufs gleich der
  Handrechnung, Formelmappe mit und ohne Periode. Fünf bUnit-Fälle in `VorlagenPositionDialogTests` (ohne Periode kein
  Feld, Vorgabe 1, Herleitung und OK, Vorbelegung mit Klemmung, Assistent). Angepasst: `ErgebnisansichtTests`
  (Fassung 11), `KiMaskenabdeckungWacheTests` (8 → 9), `KiDialogkatalogTests` („neun").
- **Phase 2:** Zusammenführung `cb06a7c3` mit `origin` = `3c2f1752` ohne Konflikt; **E16/6** (`de49dec9`):
  Testdatenbank 127 → 128 (LFS `f4a6ee8b…`, überholt). Danach hatte die Anlagenkopplung AK1, Welle 3, den Schritt 128
  gepusht (`01408e8b`, `ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS`, vier Spalten an `Tab_ErgebnisGebaeude`,
  Testdatenbank `81209c50…`). Zusammenführung `6ddb0132` mit `origin` = `c778ab12`: Konflikte in `SchemaStand.cs`,
  `SchemaMigration.cs` (drei Blöcke), `Werkzeuge/Testdatenbankschema/Program.cs`, `EPOS.Kern.Tests/TestDatenbank.cs`
  und der Testdatenbank — überall 128 (Heizkreis) vor der Wiederholperiode, die Testdatenbank in der Fassung von
  `origin`. **E16/7** (`37538993`): `WiederholperiodeSchema.SCHRITT` 128 → **129** (die einzige Stelle; Kommentare
  „nach 128 (Heizkreis)"; die Werkzeug-Wache prüft 128 vor 129 und `SCHRITT` > `SCHRITT_HEIZKREIS`). **E16/8**
  (`7c8f4fc4`): Testdatenbank 128 → **129** vom Werkzeug aus der Fassung von `origin` — zwei Spalten neu, keine
  Tabelle, Schritt 128 fand nichts offen; 133 Tabellen (132 STRICT und `sqlite_sequence`), 14 Sichten, 210 Indizes;
  `integrity_check` ok, `foreign_key_check` leer; **67.796.992 Byte** (Größe unverändert), **LFS
  `4c546a7c05137b9e45549d3d0dd171150f6f74734327bafaf61ad5405ff8345a`**; keine Zeile pflegt `Wiederholperiode_a`. Die Zahl
  129 steht nur in `WiederholperiodeSchema.SCHRITT`, in Betreff und Rumpf von E16/7 und E16/8 und als `SchemaVersion`
  der Testdatenbank; der Betreff von E16/1 nennt noch 128 (Geschichte).

## Schlüssel

Je Sprache 10.001 → 10.009 Einträge, **8 neu**: `WIRT_BK_ALLE_N_JAHRE` („alle {0} Jahre ab Jahr {1}"),
`WDH_LBL_PERIODE` („Zahlung alle:"), `WDH_EINHEIT` („Jahre"), `WDH_INFO` (die Zeile unter dem Feld),
`KI_DLG_VOP_WDH_NAME` („Zahlung alle … Jahre"), `KI_DLG_VOP_WDH_ERL` (die Erklärung des Assistenten),
`WIRT_FM_MJ_WDH_PB` und `WIRT_FM_MJ_WDH_PE` (die Hilfsspalten der Formelmappe). Der Designer führt 10.006
Eigenschaften (9.998 vor E16), wiederholbar; de/en deckungsgleich.

## Fragen aus der Welle

Gebaut ist jeweils Lesart a (die Empfehlung); offen beim Anwender (→ Register R‑E16).

| Frage | Lesarten | Empfehlung |
|---|---|---|
| **E16‑Q1** Zahlungsjahre einer Position „alle n Jahre" | (a) s, s + n, s + 2n … ≤ T mit s = Startjahr, ohne Startjahr 1 (`KapitalwertRechner.ZahltImJahr`); (b) n, 2n, 3n … ab Jahr n | a |
| **E16‑Q2** Welche Positionen eine Periode tragen | (a) nur Betriebspositionen — eine Investition „alle n Jahre" ist die Ersatzkette über die Nutzungsdauer; (b) auch Investitionspositionen | a |
| **E16‑Q3** Die Betriebskosten p. a. | (a) die Jahr‑1-Zahl — eine Periode mit Startjahr ≤ 1 zählt, eine spätere nicht; die Gliederungsprobe geht auf; (b) der Jahresdurchschnitt Betrag ÷ n | a |
| **E16‑Q4** `SpeicherAuslegungCtrl.Modulkosten` (neu, Abweichung 8) | (a) so lassen — die Speicherauslegung liest eine Betriebsposition mit Periode weiter als jährlich (Randfall); (b) die Periode dort mitrechnen | a |

## Abweichungen und Befunde

1. **Eigene Liste statt Feld:** Die Positionen mit Periode stehen als eigene Liste (`Wiederholposten`), nicht als
   Feld der (Betrag, Startjahr)-Paare — die bestehenden Ausdrücke bleiben unverändert, ohne Liste ist der Weg bitgleich.
2. **Beschriftung:** „Zahlung alle:" mit der Einheit „Jahre" hinter dem Feld.
3. **Obergrenze 99:** länger als jeder Betrachtungszeitraum; eine Position, deren zweite Zahlung jenseits von T läge,
   zahlt einmal im Startjahr.
4. **Herleitung im Vorlageneditor auf Jahr 1:** Eine Vorlage kennt kein Startjahr; die Herleitung nennt dort „ab
   Jahr 1".
5. **Nachweisumschlag Fassung 10 → 11:** Jede Betriebskostenposition trägt ihre Periode (nur bei n ≥ 2 geschrieben);
   ein älterer Umschlag liest seine Positionen als jährlich.
6. **Stufe 3 der Formelmappe:** Bemessene Betriebskosten bleiben Menge × Satz je Zahlung; die Periode steht dort in
   der Herleitungsspalte, gerechnet wird sie in der Mehrjahrestabelle (Stufe 1).
7. **Alte Sicht `LiesBetriebskosten(out abJahr)`** (vor FX3) kennt keine Perioden — mit Kommentar im Code vermerkt.
8. **Speicherauslegung:** `SpeicherAuslegungCtrl.Modulkosten` liest eine Betriebsposition mit Periode weiter als
   jährlich — als Frage E16‑Q4 gestellt.
9. **Hinweis „Startjahre gesetzt"** erscheint auch bei einer Position mit Periode und Startjahr ≥ 2 (Befund, bleibt).

Dazu die **Schrittnummer-Messung:** In Phase 1 war 128 frei (`origin` bei `3c2f1752` auf Zielversion 127); die
Anlagenkopplung AK1, Welle 3, pushte 128 um 21:22 Uhr (`01408e8b`, ohne Statuszeile) — nach der Regel „wer zuerst
pusht" wird E16 **129** (E16/7, E16/8). Die **Maskenwache** zählt am `VorlagenPositionDialog` 9 Eingabestellen statt 8.

## Nachweis

- **Ohne Pflege bitgleich:** `WiederholperiodeTests.Jaehrlich_rechnet_der_Kern_bitgleich`, `WirtschaftlichkeitAnkerTests`
  unverändert grün, kein Anker neu gesetzt; Referenzlauf 13/13 gegen `2026-09-24_R14_Kaelteerzeuger` PASS, 394/394 CSV
  byte-gleich, 4.207.049 Werte — die Basis bleibt, keine Zeile der Testdatenbank pflegt eine Periode.
- **A/B-Nachweis an 1030** (Arbeitskopie; Zeile 101600098 „Wartung Kessel", 2.000 €/a, auf n = 2; Handrechnung
  Σ 2.000 · (1 + p_B)^(t−1) / (1 + i)^t über t = 2, 4, … 20 — die entfallenen Jahre):

  | Szenario | T | i | p_B | Kapitalwert vorher [€] | Kapitalwert nachher [€] | Δ Lauf [€] | Handrechnung [€] |
  |---|---|---|---|---|---|---|---|
  | Erwartet | 20 | 3 % | 1,5 % | −21.895.377,28 | −21.878.549,68 | +16.827,59 | 16.827,59 |
  | Günstig | 20 | 2 % | 0,5 % | −21.981.462,76 | −21.964.493,60 | +16.969,16 | 16.969,16 |
  | Ungünstig | 20 | 4 % | 2,5 % | −21.840.407,19 | −21.823.718,84 | +16.688,35 | 16.688,35 |

  Abweichung Lauf gegen Handrechnung < 1e‑9 €; „vorher Erwartet" ist der gepinnte Anker −21.895.377,28 €; die
  Betriebskosten p. a. bleiben 20.000 €/a (E16‑Q3 a). Das Prüfprogramm war eine vorübergehende Testklasse, gelöscht,
  nicht committet.
- **Ressourcen:** je Sprache 10.009, keine Dublette, de/en deckungsgleich; Designer 10.006, wiederholbar (+0).
- **Phase 1** (Worktree `e16`): Builds 0 Fehler (Kern-Filter, WindowsFormsApplication1 x64, Werkzeug,
  `EPOS.Referenzlauf`), SQL-Prüfer 1.823/0; kein `dotnet test`.
- **Phase 2** (auf `7c8f4fc4`, derselbe Baum wie `ae7b0ed0`): gefiltert Kern 240/240 (Wiederholperiode,
  Ergebnisansicht, Gliederung, Risiko, Wirkungen, Katalogreparatur, Heizkreis, Migrationsketten, Erstbereitstellung,
  Formelmappe, Blattstruktur), UI 239/239 (Zeileneditor, Maskenwache, KI-Dialogkatalog); voller Lauf EPOS.Kern.Tests
  6.111 / 0 Fehler, EPOS.UI.Tests 6.017 / 0, KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27 und
  1 übersprungen; Auslieferungsvorlage.Tests 30/30; Referenzlauf 13/13 gegen R14 (oben); SQL-Prüfer 1.824 Texte / 0
  Fundstellen (332 dynamisch, 1.492 in Ordnung); Designer 10.006. Der Zwischenstand auf 128 war ebenso grün (Kern
  196/196, `WiederholperiodeTests` 31/31, UI 239/239; voll Kern 6.107, UI 6.017, 549, 386, 27 und 1, Auslieferungsvorlage
  30/30).
- **Gate auf `ae7b0ed0`:** NACHTRAG-484-GATE
- **CI:** NACHTRAG-484-CI

## Abnahme am Gerät (A‑E16‑1, Windows)

1. **Feld im Zeileneditor:** Kostenverwaltung, Betriebsseite, Stift einer Wartungsposition: Unter der Positionsart
   steht „Zahlung alle: [1] Jahre" mit der Zeile „1 = jährlich …"; auf 2 gesetzt nennt die Zeile „alle 2 Jahre ab
   Jahr X". Auf der Investitionsseite steht das Feld nicht.
2. **Kapitalwert:** Rechnen — der Kapitalwert steigt um den Barwert der entfallenen Jahre; die Betriebskosten p. a.
   bleiben die Zahl des ersten Jahres.
3. **Berichte:** Die Betriebskostentabelle in Word und Excel nennt in der Herleitung „alle 2 Jahre ab Jahr 1", ohne
   Gliederungswarnung; die Formelmappe trägt die Hilfsspalte mit `MOD`.
4. **Rückweg:** Die Position zurück auf 1 — der alte Kapitalwert steht wieder da.

## Logbuch

Im Update-Papier (Version 1.2.0.4, Sammel-Upload 26.09.2026), Stichwort `kosten`: „Betriebskostenpositionen lassen
sich im Zeileneditor mit ‚Zahlung alle n Jahre' führen (z. B. Dichtheitsprüfung alle 2 Jahre); die Wirtschaftlichkeit
rechnet sie nur in ihren Zahlungsjahren."

## Papiere mit der Statuszeile

Register (Kopf, Familientafel mit R‑V „V‑G3 gebaut #484 (Schritt 129)" und der neuen Familie R‑E16, R‑V mit dem Vermerk
und der Zeile V‑G3, R‑E9a bei E9a‑Q6, R‑E16 mit E16‑Q1…Q4, EZ‑9, EZ‑10), Konzept (Kopf mit Codestand `ae7b0ed0` und
Schemastand 129, Schrittabsatz mit 128 und 129, § 2.11.2 Zeile V‑G3, § 2.11.4 V‑E und Fußnote, § 2.11.6 Stufe 1,
§ 2.13 (3), § 3.1, § 3.4, § 6.1, § 6.2, § 7 und Anhang), Rechenweg 08 (Berechnungsgrundlage mit den Zahlungsjahren,
Gap-Tafel V‑G3), Analysepapier (Kopf, Nachtrag #484, § 5 Zeile E16 und Stand der Etappen, § 6 Schritt 129), Protokoll
der Entscheidwege (Kopf, Kopf von § 8, § 8.33, § 8.34), `Referenzlaeufe/LIESMICH.md` (Nachtrag Schemastand 129),
Mockup (Kategorie 2: Zone „Zeileneditor der Betriebsseite" und Ressourcentafel; Kategorie 8: Betriebskostentabelle,
Formelmappe, Ressourcentafel; Stand-Absatz), Update-Papier und die Wiki-Quellen Kosten (Anker `zahlung-alle-n-jahre`)
und Wirtschaftlichkeit (`bericht-betriebskosten`, `formelmappe`), Index (Reporting 131 → 132).

## Offen

- **Fragen E16‑Q1…Q4** beim Anwender (gebaut jeweils a).
- **Abnahme am Gerät** A‑E16‑1 (vier Schritte oben).
- **Gate** auf `ae7b0ed0` und **CI** (Nachweis oben).
- Der **Wiki-Sammel-Upload** am 26.09.2026 (Version 1.2.0.4, freigegeben).
- Die Gap-Tafel V‑G des Konzepts (§ 2.11.2) ist geschlossen; aus der Welle bleiben die Befunde 7 und 9 (oben).
