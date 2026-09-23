# E6 — Verlauf mit drei Szenarien: Dreierreihe, Strichart, Abschnitt der Seite, Berichte, Nachträge E5b (Protokoll, 23.09.2026)

Statuszeile #436 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Etappe E6 des Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 3.3 A3, § 5 Zeile E6); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.13 (5) und § 2.11.3 (kumulierter diskontierter Cashflow mit drei Szenarien); Szenarienkonzept
[`Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md`](../../../aktuell/Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md) § 11
(K8: Wegfall des Knopfes „Verlauf…"); Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Kategorie 8, Zone
„Wie sicher ist das?" (Bandbreite mit Spannenbild, „Der Verlauf über die Zeit — alle drei Szenarien"), Anhangzeilen U3, U13
und der Rest von U2 — vom Anwender am 22.09.2026 als verbindliche Zielvorgabe abgenommen. Entscheide: K8/V‑1 (18.09.2026,
der Knopf „Verlauf…" entfällt mit dem Verlauf auf der Seite), E5‑Q2 (Szenarionamen „Ungünstig"/„Günstig"), E5b‑2, E5b‑3 und
E5b‑4 (22.09.2026 nach Empfehlung). Anwender 22.09.2026: „im Anschluss fahre fort mit E6". Zweig `e6` von `f96b59db`, zwei
Phasen; Phase 1: `ef84a3ec` (E6/1), `4bb36b7f` (E6/2), `c9cb66df` (E6/3), `10867009` (E6/4), `c2e5cd95` (E6/5), `ec68db47`
(E6/6), `d8a090b3` (E6/7), `0dcde77d` (E6/8), `a63fb89d` (E6/9), `7bbcb0e6` (E6/10), `946fa043` (E6/11), `921b4472` (E6/12),
`410d0dd9` (E6/13); dazu die Zusammenführungen `67e56293` (Arbeitszweig `211b7c9b`, nur Papiere, ohne Konflikt) und
`c54188c2` (origin `d6c89d24`, sechs Commits der Gebäudesimulation mit der Basis R11; ein Konflikt, Konzept § 6.3 Nr. 20/21);
Phase 2: `0bfee5b1` (E6/14), `d30485bb` (E6/15), `6be317f1` (E6/16), `fa80786a` (E6/17). Merge `57b15a7c` in
`ios_migration_september`. Opus 5.5 im Worktree `.claude/worktrees/e6`.

## Befund vor der Welle

- **Konzept § 2.13 (5), gemessen:** Der Knopf „Verlauf…" öffnete `KapitalwertVerlaufDialog`, der **ein** Szenario je Lauf
  rechnete, immer bei Erwartet begann (die Seite reichte ihre Szenariowahl nicht durch) und zwei Bilder zeigte — die
  Differenz zur Referenz und die kumulierten Barwerte je Projekt absolut. Der Renderer (`ChartRenderer.KapitalwertVerlauf`)
  kannte keine Szenarien, zeichnete aber beliebig viele Reihen auf eine Jahresachse mit Name, Farbe und Strichart je Reihe;
  er konnte kein Flächenband, nicht mehr als etwa zwei Legendenzeilen im festen Maß 1240 × 620 und nicht mehr als acht
  unterscheidbare Farben.
- **Analysepapier A3:** `BerechneVerlauf` rechnete ein Szenario je Lauf; `VerlaufsReihen` vergab Farben nach laufendem
  Index, die Namen trugen kein Szenario; `Reihe.Gestrichelt` war ein `bool` — drei Szenarien brauchen eine **dritte
  Strichart**; das Bildmaß trug zwei Legendenzeilen.
- **Mockup-Anhang:** U3 (Verlauf mit drei Szenarien in einem Bild, Wegfall von „Verlauf…" als Rest von U2), U13
  (Spaltengruppe je Szenario im Tabellenbericht, Bild im Wortbericht, Excel-Blatt des Verlaufs); der Schlüssel
  `WIRT_BTN_VERLAUF_EXCEL` stand in der Schlüsseltafel als `geplant · U13`.
- **Statusdatei Nach #434 (d):** Der Wortbericht schrieb die Überschrift der Szenarienübersicht fest als
  „Szenarien Worst / Erwartet / Best", ohne Ressourcenschlüssel — gegen E5‑Q2.
- **Entscheide E5b‑2, E5b‑3 und E5b‑4** (22.09.2026) waren nicht gebaut: Vorschlagssatz für das Stammprojekt,
  „Bericht erzeugen" nur für diesen Lauf, Spannen-Balkenbild mit E6.

## Gebaut — Phase 1 (E6/1 bis E6/8)

- **Dritte Strichart (E6/1).** `ChartRenderer.Reihe` und `ChartRenderer.Segment` führen statt des Schalters `Gestrichelt`
  die Aufzählung `ChartRenderer.Strichart` (Durchgezogen, Gestrichelt, Gepunktet) mit der Vorgabe Durchgezogen; die
  Strichfolge steht einmal in `ChartRenderer.Strichfolge` (Gestrichelt wie bisher 8/5, Gepunktet 2,5/3,5 mit stumpfer
  Kappe). Die Legende umrandet das Farbfeld jeder nicht durchgezogenen Linie in ihrer Strichfolge. Alle Aufrufer
  wertgleich umgestellt (Speicherflotte, Speicherbetriebsbild, Simulationshülle, Kern-Tests, ChartProben); auf Windows
  91 von 91 Bildern byte-gleich zum Basisstand.
- **Dreierreihe im Kern (E6/2).** `WirtschaftlichkeitCtrl.BerechneVerlaufSzenarien` rechnet drei vollständige Läufe über
  `BerechneVerlauf` (Ungünstig, Erwartet, Günstig) mit derselben Referenz und ohne zu speichern — das Muster von
  `BerechneBandbreite` — und sammelt sie in `WirtschaftlichkeitVerlaufSzenarien`: die Läufe je Szenario, die Stände mit
  Differenzlinie in Gruppenreihenfolge, je Stand und Szenario Differenzlinie, Restwert-Differenz und Nulldurchgang, die
  Stände ohne Reihe. `KapitalwertRechner.Nulldurchgang` legt die Regel von `AmortisationDifferenz` auf die fertige
  kumulierte Differenzreihe (lineare Interpolation im Jahr des Durchgangs; ohne Mehrinvestition 0 oder keiner). Keine
  Rechenwirkung.
- **Verlaufsbild (E6/3).** `ChartRenderer.VerlaufsReihenSzenarien` gibt jedem Stand mit Differenzlinie eine Farbe seines
  Platzes in der Gruppe (Rollen `SERIE_1` bis `SERIE_8`) und jedem Szenario eine Strichart — Erwartet durchgezogen und
  kräftiger, Ungünstig gestrichelt, Günstig gepunktet —, Reihenname „Stand · Szenario", dazu je Linie ihren Nulldurchgang;
  mehr als acht gewählte Stände lehnt sie benannt ab (`WIRT_VERL_ZU_VIELE`). `ChartRenderer.KapitalwertSzenarienModell`
  und `KapitalwertSzenarien` zeichnen die Linien mit derselben Skala wie das Bild je Version (Achsen in `VerlaufAchsen`),
  die Marken der Nulldurchgänge mit dem Wert am Element und einer Beschriftung in bis zu drei Zeilen und die
  **zweigeteilte Legende** (erst die Stände mit Farbe, dann die drei Szenarien mit Strichart und die Marke). Bildmaß
  1240 × 620 für zwei Legendenzeilen, jede weitere Zeile verlängert das Bild um 30 px; die Zeichenfläche bleibt 1090 × 400.
- **ChartProben (E6/4).** Neue Datei `Program.Szenarien.cs`: Maßproben `kapitalwert_szenarien` (drei Stände,
  1240 × 620), `…_eine_variante` (Fall der Sicht 2), `…_acht_varianten` (1240 × 650, die Legende bricht um),
  `…_neun_varianten` (die benannte Ablehnung); Gegenproben `strichart_gepunktet_wirkt`,
  `kapitalwert_szenarien_legende_zweigeteilt_wirkt`, `kapitalwert_szenarien_nulldurchgang_wirkt`; SVG-Proben
  `svg_kapitalwert_szenarien` und `svg_kapitalwert_szenarien_stricharten`.
- **Excel-Blatt „Verlauf" (E6/5).** `VerlaufExcel.SchreibeBlatt` hängt an eine Mappe das Blatt „Verlauf": je Jahr eine
  Zeile, je Stand und Szenario eine Spalte in einer Spaltengruppe je Szenario (Ungünstig · Erwartet · Günstig, Kopf über den
  Spalten), dieselben Linien wie das Bild; darunter je Spalte Nulldurchgang [a] (fehlt er: „—", nie 0), Restwert-Barwert
  am Horizontende und Kapitalwertdifferenz = Endwert + Restwert. `SchreibeMappe` schreibt eine eigene Mappe mit nur diesem
  Blatt — der Weg des Knopfes „Verlauf nach Excel…"; der Tabellenbericht hängt dasselbe Blatt an.
- **Seite (E6/6).** Der Verlauf steht als eigener Abschnitt in „Wie sicher ist das?" nach der Bandbreite und vor der
  Sensitivität (`KapitalwertVerlaufAbschnitt`): Zeitraum (2 bis 60 Jahre, Vorgabe der Betrachtungszeitraum),
  „Aktualisieren" (während der Rechnung „Abbrechen"), „Verlauf nach Excel…" (`WIRT_BTN_VERLAUF_EXCEL`, über
  `Dienste.Datei`), je Stand und je Szenario ein Haken, der nur neu zeichnet; das Bild als `DiagrammSvg` mit Zoom und
  Zeigerzeile, die Legende nicht schaltbar; darunter Nulldurchgänge, Restwerte, Hinweis und Status. Sicht 1 und Sicht 2
  wie bisher — in Sicht 2 die eine Linie B − A in drei Stricharten. Die Hülle des Dialogs wird die Hülle des Abschnitts
  (`KapitalwertVerlaufHuelle`, plattformfrei in `EPOS.UI.Daten`): Sie übernimmt die Eingangsdaten jedes Rechenlaufs der
  Seite und rechnet die drei Läufe gleich mit, rechnet bei Wechsel von Sicht, Referenz oder Horizont aus denselben Daten
  nach und sammelt nur, wo Stände fehlen. Die Zeilen unter dem Bild stehen in `VerlaufZeilen` (Kern), damit Seite und
  Wortbericht denselben Satz tragen. **Entfallen:** der Knopf „Verlauf…" (die Fußleiste trägt höchstens vier Knöpfe), der
  Unterdialog Verlauf, `KapitalwertVerlaufDialog`, `KapitalwertVerlaufBilder` und ihre bunit-Tests.
- **Tests (E6/7).** Kern `VerlaufSzenarienTests` (Sammlung Testdatenbank): Strichart wertgleich zum alten Schalter und
  Vorgabe byte-gleich; Dreierreihe = drei Einzelläufe Zahl für Zahl, frei wählbarer Horizont, ohne Schreiben in die
  Datenbank, Sicht 2 allein B gegen A; Nulldurchgang nach der Regel und gleich `AmortisationDifferenz` bzw. der Kennzahl
  des Laufs; Reihenbildung, zweigeteilte Legende, fester Farbplatz, Ablehnung über acht Ständen; Bildmaß und Marken; Blatt
  „Verlauf" mit Spaltengruppen und Strich statt Null; Hülle schreibt über `Dienste.Datei`. bunit
  `KapitalwertVerlaufAbschnittTests`: Platzhalter ohne Datenseite, Bild und Haken, Haken zeichnet ohne zu rechnen,
  Aktualisieren mit dem Zeitraum, Abbrechen, Fehlerband, „Verlauf nach Excel…" mit der Wahl, gesperrt ohne Bild; auf der
  Seite die Stelle zwischen Bandbreite und Sensitivität, vier Fußknöpfe ohne Verlauf.
- **Berichte (E6/8).** Wortbericht: Der Verlauf rechnet alle drei Szenarien (`BerechneVerlaufSzenarien`); das Dreierbild
  (Titel `WIRT_VERL_BILD`, Legende zweigeteilt, Nulldurchgänge markiert, Fuß `WIRT_VERL_FUSS`) tritt an die Stelle des
  Differenzbildes im Erwartungsfall, darunter die Zeilen der Nulldurchgänge und Restwerte; das Bild „Kumulierte Barwerte je
  Version" (U18) bleibt unverändert, die Mehrjahrestabelle nimmt den Erwartungsfall; Hinweis `WIRT_VERL_WORT_HINWEIS`
  statt des festen Satzes; die Überschrift der Szenarienübersicht aus `WIRT_SZ_UEBERSCHRIFT` mit `WIRT_SZEN_*` (Befund
  Nach #434 (d)). Tabellenbericht: der Block „Kapitalwert-Verlauf" im Blatt „Wirtschaftlichkeit" mit einer Spaltengruppe
  je Szenario (je Projekt und Differenz), das Blatt „Verlauf" hinter „Wirtschaftlichkeit"; Zahlen unverändert.
  Blattstruktur-Wache nachgezogen (sechs Blätter), SVG-Wache um das Dreierbild.

## Nachträge E5b (E6/9 bis E6/11)

- **E5b‑2 — Vorschlagssatz für das Stammprojekt (E6/9).** Ist eine Variante die Referenz und schneidet das Stammprojekt
  am besten ab, lautet der Vorschlag „Stammprojekt beibehalten" (neuer Schlüssel `WIRT_EMPF_SATZ_STAMM`) statt „Variante
  „Stamm"". `VariantenEmpfehlung.IstStamm` kommt aus den Ergebnissen; Seite, Word und Excel bilden den Satz aus den
  Urteilen der Bandbreite. Test `NachtraegeE5bTests`.
- **E5b‑3 — „Bericht erzeugen" merkt sich nichts (E6/10).** `BerichtSeiteGaben.Erstellen(auftrag, melder,
  auswahlMerken)`: gemerkt wird nur beim Lauf der Berichtsseite, `ErzeugeFuerVergleich` übergibt `false`; die gespeicherte
  Konfiguration der Gruppe ist nach „Bericht erzeugen" Zeichen für Zeichen gleich (Prüfgruppe 1040).
- **E5b‑4 — Spannenbild der Bandbreite je Version (E6/11).** Je Version ein Balken vom kleinsten bis zum größten der drei
  Szenariowerte (dieselbe Spanne wie die Tafel, E5‑Q4), der Erwartungsfall als Punkt mit Betrag, die Referenz als
  Nulllinie: `ChartRenderer.KapitalwertSpanne` mit `Spannenbalken.Aus(WirtschaftlichkeitBandbreite)` und `SpannenTexte`,
  plattformfrei, ein reines Pixelbild; Vorzeichenfarben `RASTER_GUT`/`RASTER_SCHLECHT`. Auf der Seite unter der
  Bandbreitentafel und vor dem Verlauf (`ErgebnisAnsicht.Spannenbild`), im Wortbericht nach Bandbreitentafel und
  Fußtext. ChartProben `Program.Spanne.cs`: drei Maßproben, zwei Gegenproben, zwei SVG-Proben.
- **ChartProben-LIESMICH (E6/12)** mit dem Abschnitt „Etappe E6" und dem Verfahren, die Messlatte auf dem Linux-Läufer
  nachzuziehen; `--svg-alle` schreibt auch die drei Modelle der Etappe. **Hilfezuordnung (E6/13):** die Zeile
  `Form_WirtschaftlichkeitVerlauf.btn_Help` hatte keinen Leser mehr; der Abschnitt nimmt den Hilfeschlüssel der Seite.

## Phase 2 (E6/14 bis E6/17)

Vor der Testfreigabe sind der Arbeitszweig `211b7c9b` (A13-Konzeptschnitt, Statusdatei #435, Papiere) und origin
`d6c89d24` (Gebäudesimulation: 2-K-Löser, Vorlauf, Bestandsbefunde, Basis `2026-09-22_R11_Bestandsbefunde`) in `e6`
zusammengeführt. Der einzige Konflikt stand im Wirtschaftlichkeitskonzept, § 6.3 Nr. 20/21: die gekürzte Fassung aus A13
(Register, „siehe Protokoll") mit dem Basisnamen aus origin; Ressourcen beider Seiten vollständig, Designer gleich dem
erzeugten.

- **E6/14 — Δ-Kopf des Verlaufs nennt die Referenz** (Fehler, kein Entscheid). Der Verlauf-Block des Blattes
  „Wirtschaftlichkeit" schrieb fest „Δ ‹Stand› − Stamm", auch wenn gegen eine Variante gerechnet wurde; in Sicht 2 stand
  „Δ Stamm − Stamm". `WirtschaftlichkeitVerlauf.IdReferenz` trägt die aufgelöste Referenz, der Kopf nennt sie mit dem Namen
  ihrer Spalte.
- **E6/15 — Szenarionamen in beiden Berichten** (folgt E5‑Q2): `WIRT_SZ_SP_WORST` „ΔKW Ungünstig [€]", `WIRT_SZ_SP_BEST`
  „ΔKW Günstig [€]", `WIRT_SZ_BANDBREITE_TITEL` „… (Ungünstig / Erwartet / Günstig)"; die Szenarioblöcke des Excel-Blattes
  tragen den Anzeigenamen statt des gespeicherten Schlüssels; die Übersetzung der alten festen Überschrift ist entfernt.
- **E6/16 — Seitentests gezielt:** Zwei Fälle nahmen die erste `.epos-seite-zeile` bzw. `.epos-herleitung-text` der Seite
  und trafen seit E6/6 die Elemente des Verlaufsabschnitts; sie suchen jetzt die Szenariozeile bzw. die Herleitungszeile
  mit der Referenz. Die Seite ist unverändert.
- **E6/17 — `ZeichenmodellWache`:** Die Wache pinnt die Zahl der öffentlichen Bildmethoden des Renderers; E6 bringt zwei
  dazu (`KapitalwertSzenarien`, `KapitalwertSpanne`), 26 → 28, die Regel bleibt geprüft.

## Fragen aus der Etappe

Der Agent meldete vier Fragen. Zwei sind Fragen an den Anwender und im Entscheidungsregister als **R‑E6** geführt, zwei hat
er in Phase 2 gebaut:

| Frage | Stand |
|---|---|
| **E6‑Q1** Block 4 „Unsicherheit" der Darstellung „ValERI-Bewertung" zeigt Bandbreite, Vorschlag, Hinweistext und Sensitivität, aber weder den Verlauf noch das Spannenbild — die Tafel der fünf Blöcke im Mockup nennt dort den Verlauf. Beide auch in Block 4? | offen — Empfehlung: ja, dieselben Bausteine |
| **E6‑Q2** Das Spannenbild färbt Punkt, Betrag und Balkenenden nach dem Vorzeichen (grün über, rot unter der Referenz) und teilt die Achse in vollen Euro; das Mockup zeichnet eine Farbe und die Achse in Millionen. So lassen? | offen — Empfehlung: so lassen |
| Szenarionamen „Ungünstig"/„Günstig" auch in der Szenarientafel des Wortberichts und in den Excel-Blöcken | gebaut mit E6/15 — folgt E5‑Q2, kein neuer Entscheid |
| Δ-Kopf des Verlaufs im Tabellenbericht | gebaut mit E6/14 — ein Fehler, kein Entscheid |

## Abweichungen vom Mockup

1. **Marken der Nulldurchgänge** schwarz mit hellem Ring statt Bernstein.
2. **Szenarionamen an den Marken** nur bei einem Stand („Ungünstig 3,02 a"), sonst nur der Wert.
3. **Strichstärken** 3 px (Erwartet) und 2,5 px (Ungünstig, Günstig).
4. **Legendenmuster** der Szenarien in Textfarbe — die Strichart gilt für jede Farbe.
5. **Das Bild wächst** je weitere Legendenzeile um 30 px; die Zeichenfläche bleibt gleich.
6. **Die Legende schaltet nicht** — ein Eintrag „Variante 1" trüge drei Linien, „Ungünstig" eine je Stand; gefiltert wird
   über die Haken der Bedienleiste.
7. **Zeitraumfeld und „Aktualisieren" bleiben** (aus dem entfallenen Dialog übernommen).
8. **Im Wortbericht ersetzt das Dreierbild das Differenzbild „Erwartet"** — dessen Linien sind die durchgezogenen des
   Dreierbildes; das Bild „Kumulierte Barwerte je Version" bleibt.
9. **Englisch** „progression" für Verlauf.
10. **Spannenbild:** Achse in Euro, Vorzeichenfarben (E6‑Q2), keine gerundeten Ecken, Titel „je Version" statt „je
    Variante", nur in der Darstellung „Kennzahlen" (E6‑Q1).

## Zahlen und Abnahme

- **Im Worktree `e6`** (Stand `fa80786a`): Kern-Filter 0 Fehler, Windows-Schale 0 Fehler; voller Lauf Kern 4 591, UI
  5 230, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen — 0 Fehler, 10 758 grün; Referenzlauf gegen
  `2026-09-22_R11_Bestandsbefunde` 13/13 PASS über 3 882 737 Werte, 357/357 CSV byte-gleich. **Keine Rechenwirkung:** Der
  gebuchte Lauf rechnet unverändert, Verlauf und Spannenbild rechnen ohne Speichern; die Wirtschaftlichkeit steht nicht im
  Referenzexport (A11), Nachweis sind die Kern- und bunit-Tests.
- **ChartProben:** 122 Prüfungen (84 Proben: 58 Maß-, 25 Gegen-, 1 Versatzprobe; 38 SVG-Proben), 0 Verstöße, 108 Hashes —
  die 91 Bilder vor der Etappe byte-gleich, 17 neu (`kapitalwert_szenarien`, `…_eine_variante`, `…_acht_varianten`,
  `…_neun_varianten`, `kapitalwert_spanne`, `…_unter_referenz`, `…_leer` und die Gegenproben a/b zu
  `strichart_gepunktet_wirkt`, `kapitalwert_szenarien_legende_zweigeteilt_wirkt`, `kapitalwert_szenarien_nulldurchgang_wirkt`,
  `kapitalwert_spanne_erwartet_wirkt`, `kapitalwert_spanne_spanne_wirkt`), dazu vier SVG-Proben. Die Repo-Messlatte
  `Proben/ChartProben/Messlatte_2026-09-20.sha256` (Linux, 91 Zeilen) bleibt; die Probe liest sie nicht, `kern.yml` ruft
  ohne `--hashes`. Die lokale Windows-Messlatte des Gates führt 108 Zeilen.
- **Gate #436** auf `57b15a7c`: Kern-Filter (Release) 0 Fehler, ChartProben alle grün, 108 Bilder, Hashes gleich der
  Windows-Messlatte (91 alte unverändert, 17 neu), Tests 10 758 grün, 1 übersprungen (KiKern 524, SpeicherEngine 386,
  SpeicherPlanung 27 und 1 übersprungen, EPOS.UI 5 230, EPOS.Kern 4 591), Dokumentationswachen 24/24, Designer 7 594
  Einträge unverändert; Windows-Schale auf dem Merge-Stand 0 Fehler.
- **Ressourcen:** 34 neue Schlüssel de/en — `WIRT_VERL_*` (25: `_TITEL`, `_BILD`, `_ERKLAERUNG`, `_FUSS`, `_WORT_HINWEIS`,
  `_LEG_VARIANTEN`, `_LEG_SZENARIEN`, `_NULLDURCHGANG`, `_NULLZEILE`, `_KEIN_NULL`, `_STATUS`, `_FEHLEND`, `_NOCH_NICHT`,
  `_ZU_VIELE`, `_KEINE_REIHE`, `_BLATT`, `_BLATT_TITEL`, `_ZEILE_NULL`, `_ZEILE_RESTWERT`, `_ZEILE_KW`, `_EXCEL_TITEL`,
  `_EXCEL_DATEI`, `_EXCEL_OK`, `_EXCEL_FEHLER`, `_EXCEL_LEER`), `WIRT_SPANNE_*` (6: `_TITEL`, `_LEG_SPANNE`,
  `_LEG_ERWARTET`, `_LEG_UNTER`, `_ACHSE`, `_LEER`), `WIRT_BTN_VERLAUF_EXCEL`, `WIRT_SZ_UEBERSCHRIFT`,
  `WIRT_EMPF_SATZ_STAMM`. Geändert (3): `WIRT_SZ_SP_WORST`, `WIRT_SZ_SP_BEST`, `WIRT_SZ_BANDBREITE_TITEL`. Gestrichen (10,
  ohne Leser): `WVERL_TITEL`, `WVERL_TITEL_STAMM`, `WVERL_LBL_SZENARIO`, `WVERL_BTN_SCHLIESSEN`, `WVERL_BILD_DIFF`,
  `WVERL_BILD_ABS`, `WVERL_UNTER_DIFF`, `WVERL_UNTER_ABS`, `WVERL_STATUS_KOPF`, `WIRT_BTN_VERLAUF`. Designer 7 594 Einträge
  (mit den drei Schlüsseln der Gebäudesimulation aus der Zusammenführung). Kein Schemaschritt.

## Abnahme am Gerät (A‑E6‑1, Windows und iPad)

Wirtschaftlichkeitsseite einer Vergleichsgruppe mit gerechnetem Stand, neun Punkte: (1) Unter „Wie sicher ist das?" stehen
Bandbreite, Spannenbild, Verlauf und Sensitivität in dieser Reihenfolge. (2) Die Haken je Stand und Szenario zeichnen nur
neu; ein anderer Zeitraum rechnet mit „Aktualisieren" nach. (3) „Verlauf nach Excel…" schreibt eine Mappe mit dem Blatt
„Verlauf". (4) Die Fußleiste trägt höchstens vier Knöpfe, „Verlauf…" steht nicht darin. (5) Sicht 2 zeigt die eine Linie
„Δ B − A" in drei Stricharten. (6) Ist eine Variante die Referenz und schneidet der Stamm am besten ab, lautet der Vorschlag
„Stammprojekt beibehalten". (7) Die Berichtsseite zeigt nach „Bericht erzeugen" ihre Auswahl unverändert. (8) Wortbericht mit
Dreierbild, Nulldurchgangs- und Restwertzeile, Spannenbild und der Überschrift „Szenarien Ungünstig / Erwartet / Günstig";
Tabellenbericht mit Spaltengruppen je Szenario und dem Blatt „Verlauf". (9) Englisch.

## Offen

- **Zwei Anwenderfragen:** E6‑Q1 (Verlauf und Spannenbild auch in Block 4 der ValERI-Ansicht; Empfehlung: ja, dieselben
  Bausteine) und E6‑Q2 (Spannenbild mit Vorzeichenfarben und Achse in vollen Euro; Empfehlung: so lassen) — Register R‑E6.
- **Repo-Messlatte auf Linux nachziehen:** ein Lauf mit `--ablage` und `--hashes` auf dem Linux-Läufer ergibt 108 Zeilen,
  deren 91 alte der bisherigen Datei gleichen müssen (Verfahren in `Proben/ChartProben/LIESMICH.md`); freiwillig, weil die
  Probe die Datei nicht liest. **Schriftrisiko:** die neuen Bilder hängen an der Schrift des Läufers (Liberation Sans
  erwartet).
- **Word-Hinweis ohne Referenz:** „Differenzdiagramm entfällt — für das Stammprojekt konnte keine Zahlungsreihe gerechnet
  werden" nennt auch bei einer Variante als Referenz das Stammprojekt (Kleinigkeit).
- **Kommentare:** `KnopfleistenWacheTests` und `UeberlagerungstitelTests` nennen den entfallenen Dialog nur noch in
  Kommentaren.
- **Bei der Papierpflege gemessen (nicht E6):** E5‑Q2 ist nicht überall nachgezogen — die Klammer des Vorschlagssatzes
  lautet „(Worst …, Best …)" (`WIRT_EMPF_ALLE_POSITIV`, `WIRT_EMPF_NUR_ERWARTET`), die Spalten der Szenariotafel im
  Parameterdialog heißen „Best"/„Worst" (`WPAR_SZ_SPALTE_BEST`, `_WORST`); die Kachel „Kapitalwert ggü. Stamm"
  (`WIRT_KACHEL_KW`) und der Methodiksatz am Anfang des Wortberichts nennen den Stamm auch bei einer Variante als
  Referenz. Kleinigkeiten für die nächste Codewelle; die Wiki-Quelle beschreibt die Beschriftungen, wie sie sind.
- **E8-Stücke aus E5b‑4:** Nominalsummen und Differenzspalte der Gliederung, Brückenbild, Tafel „Was daraus im Lauf wird",
  Fußzeile „Drei Szenarien gerechnet…" — Mockup-Anhang U41 und U46 bis U48.
- **Papiere mit der Statuszeile:** Register (E5b‑2/3/4 gebaut, R‑E6, K8), Konzept (§ 2.13 (5), § 6.1, § 6.3 Nr. 9j, § 7,
  Referenzbasis R11) samt der kleinen Papierpflege aus Nach #435 (a), Mockup-Nachzug (U3, U13, Rest von U2, U46 bis U48
  für die E8-Stücke, U49 für E6‑Q1, Schlüsseltafel), Logbuch-Sätze und Wiki-Quelle der Seite Wirtschaftlichkeit — mit den
  Papieren dieser Statuszeile.
