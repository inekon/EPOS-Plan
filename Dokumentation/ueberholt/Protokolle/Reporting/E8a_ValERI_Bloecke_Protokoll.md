# E8a — ValERI-Ansicht vollständig: Block 2 mit Zahlungsstrombild, Block 4 mit Spannenbild und Verlauf, Gliederung mit Nominalsumme, Brückenbild, „Was daraus im Lauf wird", Fußzeile (Protokoll, 23.09.2026)

Statuszeile #454 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Etappe E8 (Teil a — V‑C) des
Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 5 Zeile E8); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.10, § 2.11.3, § 2.11.4 (V‑C) und § 2.13 (5); Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`,
Kategorie 8 (Zonen „ValERI-Bewertung — die fünf Blöcke", „Woraus entsteht die Zahl?", „Der Zahlungsstrom über die Jahre",
„Was ist angenommen?"), Anhangzeilen U41, U42 und U46 bis U49. Entscheide: E6‑Q1 (R‑E6, 23.09.2026: ja), E5b‑4 (R‑E5,
22.09.2026, nach Empfehlung) im
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md),
die vier Fragen dieser Etappe unter R‑E8a. Anlass: der Anwender, „fahre fort" (23.09.2026) — Etappe E8, Teil a; Teil b
(Formelmappe Stufen 0 bis 3, Anhang-E-Checkliste U43, Anhang-D-Gegenprobe) lief parallel im Worktree `e8b` und folgt als
#455. Zweig `e8a` von `591229e1` (`origin`, Schemastand 113, Basis R12), zwei Phasen und vier Nachzüge; Phase 1:
`ddf252bb` (E8a/1), `894c970b` (E8a/2), `836cf54c` (E8a/3), `6d979cdc` (E8a/4), `a03b4b5b` (E8a/5), `e86394ed` (E8a/6),
`e71f2d4a` (E8a/7); Phase 2: Merge `8a586448` (Arbeitszweig `bba1f1a7`: Kühlung KU1 Welle 4 mit der Basis R13),
`5c3cc58e` (E8a/8, Zahlungsstrombild nach dem Entscheid E8a‑Q1), `51bf6d6b` (E8a/9), Merge `ac51fdb5` (Arbeitszweig
`2edc081e`: #450); Nachzüge: Merges `b5de6db9` (`7a32b6f3`, Protokoll-Nachtrag #450), `45154b9f` (`cd2ac9ec`, #457) und
`bf113cf8` (`ada7d1ac`, #456) — alle ohne Konflikt. Merge `485052c6` auf dem Hilfszweig `pm5` (Basis
`origin/ios_migration_september` = `ada7d1ac`; 25 Dateien, +4 841/−46; der Baum gleicht `bf113cf8`). Opus 5.5 im
Worktree `.claude/worktrees/e8a`. Die Punktnummern folgen dem Auftrag (1 = Block 2, 2 = Block 4, 3 = U46, 4 = U41,
5 = U47, 6 = U48); gebaut wurde in der Folge 2, 1, 3, 5, 6, 4, danach die Nachbesserung E8a/7 und nach dem Entscheid
U42 (E8a/8). **Keine Rechenwirkung, kein Schemaschritt** — `SchemaStand.Zielversion` bleibt 113.

## Befund vor der Welle

- **Die Darstellung „ValERI-Bewertung" (#434)** führte die Blöcke 1, 3, 4 und 5; an der Stelle von Block 2
  „Zahlungsreihen" stand eine benannte Hinweiszeile (`WIRT_VALERI_BLOCK_2_HINWEIS`). Block 4 „Unsicherheit" zeigte
  Bandbreite, Vorschlag, Hinweistext und Sensitivität, aber weder das Spannenbild noch den Verlauf, die unter „Wie
  sicher ist das?" der Darstellung „Kennzahlen" stehen — die Tafel der fünf Blöcke im Mockup nennt dort den Verlauf.
  Frage E6‑Q1, am 23.09.2026 entschieden: „ja — dieselben Bausteine wie unter ‚Wie sicher ist das?'" (U49).
- **Die Gliederung „Woraus entsteht die Zahl?"** führte Betriebs- und Energiekosten als Werte des ersten Jahres
  [€/a], Ersatz und Restwert als Barwert, keine Nominalsumme und keine Differenzspalte (U46).
- **Brückenbild (U41), Tafel „Was daraus im Lauf wird" (U47), Fußzeile „Drei Szenarien gerechnet…" (U48):** im Code
  nirgends vorhanden; nach E5b‑4 (22.09.2026, nach Empfehlung) Bau mit E8.
- **Das Zahlungsstrombild (U42)** trug im Mockup-Anhang „offen — Konzeptentscheid nötig" und nicht den Zusatz „mit
  E8"; der Auftrag stellte es als Frage E8a‑Q1.
- **Die Jahresreihen** stehen seit Etappe E7 im Zahlungsbild des `KapitalwertRechner` — je Lauf und Szenario;
  gespeicherte Ergebnisse tragen nur Summen, keine Jahresreihen.
- **Zwei Unklarheiten der Papiere** (Faktenblatt E8): Konzept § 2.11.3 nennt fünf Darstellungsblöcke nach den
  Kostenarten (Investitionskosten … Wirtschaftlichkeit über Nutzungsdauer), Mockup, Code und § 2.11.4 zählen „1 ·
  Gegenstand und Rahmen" bis „5 · Deklarationen"; die Tafelzeile E8 des Analysepapiers § 5 nannte E6‑Q1 nicht, die
  Prosa darunter schon. Beides ist mit den Papieren zu #454 aufgelöst (Konzept § 2.11.3 mit einer Fußnote,
  Analysepapier § 5 mit E6‑Q1 in der Tafelzeile).

## Gebaut — Phase 1 (E8a/1 bis E8a/7)

- **Block 4 vollständig — E6‑Q1, U49 (E8a/1, `ddf252bb`).** Block 4 „Unsicherheit" zeigt jetzt auch das Spannenbild
  und den Verlauf mit drei Szenarien — dieselben Bausteine wie unter „Wie sicher ist das?": das Fragment
  `Spannenteil` mit dem Modell der Ansicht und die Komponente `KapitalwertVerlaufAbschnitt` mit derselben Datenseite
  und derselben Fassung; keine zweite Zeichenlogik, keine zweite Datenquelle. Reihenfolge: Bandbreite, Spannenbild,
  Vorschlag, Hinweistext, Verlauf, Sensitivität. Es steht immer nur eine der beiden Darstellungen auf der Seite, die
  Kennungen der Bilder bleiben je Blatt eindeutig. bunit: Stellung in Block 4, das SVG des Spannenbilds gleicht dem
  der Darstellung „Kennzahlen", der Verlauf ruft dieselbe Datenseite, je Darstellung genau ein Verlauf.
- **Block 2 „Zahlungsreihen" (E8a/2, `894c970b`).** Je Stand und Szenario eine Jahrestafel (DIN EN 17463, 6.1 bis
  6.4): die sechs Bestandteile Investition, Betriebskosten, Energiekosten, Erlöse, Ersatzbeschaffungen und Restwert,
  Netto und Barwert je Jahr, darunter „Summe nominal" und die Barwerte (Summe = Nettobarwert). Zwei Klapplisten
  „Stand:" und „Szenario:" wählen die Tafel; Vorgabe ist die **Leitversion** im Erwartungsfall — die Version mit der
  größten Kapitalwertdifferenz im Szenario Erwartet, in der Sicht „Zwei Stände" der Stand B
  (`Zahlungsgliederungen.Leitversion`). **Quelle** sind die Zahlungsbilder des Laufs dieser Sitzung: die drei
  Szenarioläufe, die der Verlauf mit „Berechnen" ohnehin rechnet; nur bei verstelltem Verlaufshorizont rechnet die
  Hülle eigens über T und merkt sich das Ergebnis (`KapitalwertVerlaufHuelle.GliederungenUeberT`). **Neue
  Kern-Klasse `Zahlungsgliederung`:** Sie ordnet das fertige Zahlungsbild in die sechs Bestandteile — Investition =
  I₀ nach Zuschussabzug im Jahr 0; Betriebskosten = die Betriebszeile beider Preissteigerungstöpfe; Energiekosten =
  Energie und CO₂-Abgabe; Erlöse = Einspeiseerlös und die benannten Erlösreihen, im Jahr 0 die Pauschale nach § 9
  KWKG (Block A der Erlösrubrik); Ersatzbeschaffungen = die Ersatzreihe; Restwert = der nominale Restwert im Jahr T
  — und zinst je Bestandteil mit dem Faktor (1 + i)^−t des Rechners ab. Sie prüft sich selbst (die sechs Barwerte
  ergeben den Kapitalwert des Bildes) und wird gegen das gespeicherte Ergebnis abgeglichen; gezeigt wird nur eine
  Reihe, deren Kapitalwert zum Ergebnis der Seite passt (`Zahlungsgliederungen`, Toleranz 0,01 €). Ohne Lauf in der
  Sitzung steht die Hinweiszeile, ihr Text ist neu gefasst: „Die Zahlungsreihen je Jahr stehen nach dem nächsten
  Rechenlauf („Berechnen" oder „Aktualisieren" im Verlauf) — die gespeicherten Ergebnisse tragen nur Summen, keine
  Jahresreihen." Die Hülle `ZahlungsreihenAnsicht` (neu, `EPOS.UI.Daten`) formatiert nur. Tests
  `ZahlungsgliederungTests` (neu), bunit Block 2 mit Wahl von Stand und Szenario und abweichendem Stand.
- **U46 — die Gliederung mit Nominalsumme und Differenzspalte (E8a/3, `836cf54c`).** „Woraus entsteht die Zahl?"
  beginnt mit der „Gliederung des Kapitalwerts — Szenario …": je Bestandteil und Stand der Barwert, darunter leise
  die Nominalsumme (bei der Investition keine — sie fließt im Jahr 0), als letzte Spalte „Differenz ‹Leitversion› −
  ‹Referenz›", als letzte Zeile der Nettobarwert. Die Differenzspalte ist Bestandteil für Bestandteil die Differenz
  der Barwerte (`Zahlungsgliederung.Differenz`) und geht in der Kapitalwertdifferenz auf. Die Tafel folgt der
  Szenario-Klappliste; ohne Jahresreihen steht eine benannte Zeile (`WIRT_GL_NICHT_GERECHNET`), die Zeilentafel
  darunter bleibt unverändert. bunit: je Bestandteil Barwert und Nominalsumme, die Differenzspalte in der Summe
  gleich der Kapitalwertdifferenz.
- **U47 — Tafel „Was daraus im Lauf wird" (E8a/4, `6d979cdc`).** In „Was ist angenommen?" unter dem Hinweistext (U10):
  je Szenario in der Reihenfolge der Bandbreite (Ungünstig, Erwartet, Günstig) die Wirkung auf die Leitversion — die
  Investition I₀ nach Zuschussabzug, die Jahre der fälligen Ersatzbeschaffungen (ohne Ersatz „keine") und der Restwert
  am Ende, nominal. Die drei Spalten sind die Szenarioläufe der Bandbreite; Prüffall mit der Prüfgruppe 1040
  (Kapitalwertdifferenzen und Restwerte der drei Spalten gleich Bandbreite und Lauf). Ohne Jahresreihen steht keine
  Tafel.
- **U48 — die Fußzeile (E8a/5, `a03b4b5b`).** Im Fuß von „Was ist angenommen?" steht über dem Knopffuß die Zeile
  „Drei Szenarien gerechnet · Annahmen aus Vorgaben, nichts gepflegt": wie viele Szenarien für die gezeigten Stände
  ein Ergebnis tragen („Drei Szenarien gerechnet", „{0} von drei Szenarien gerechnet", „Noch kein Szenario
  gerechnet") und woher die Annahmen kommen; ist ein Feld des Szenario-Parametersatzes gepflegt, nennt sie „Annahmen
  gepflegt: …" mit den Namen nach der Regel der Annahmentafel (`ValeriAusweis.Szenarienfuss`). Nach dem OK im
  Parameterdialog frischt die Seite auf. bunit: „Vorgaben" ohne Pflege, „gepflegt" nach einer Pflege.
- **U41 — das Brückenbild (E8a/6, `e86394ed`).** `ChartRenderer.KapitalwertBruecke`/`KapitalwertBrueckeModell`
  zeichnet „Von der Investition zur Kapitalwertdifferenz" als Wasserfall nach dem Muster des Spannenbilds: je
  Bestandteil eine Säule vom Stand vor bis nach dem Schritt (rot mindert, grün mehrt die Differenz, gestrichelt
  verbunden), zuletzt die Ergebnissäule ΔKW von null bis zur Summe in der Hausfarbe; Nulllinie, Werte am Element,
  Euro-Achse mit Raster, Legende ohne Schalten; ein reines Pixelbild 1240 × 610, ohne zeichenbaren Schritt 1240 × 200
  mit Leerhinweis; deterministisch. Die Schritte sind dieselben Zahlen wie die Differenzspalte
  (`Brueckenschritt.Aus`). **Seite:** unter der Gliederung, Leitversion gegen Referenz, folgt der
  Szenario-Klappliste. **Wortbericht:** im Baustein Wirtschaftlichkeit nach den Verlaufsbildern und vor der
  Mehrjahrestafel, aus denselben drei Läufen im Erwartungsfall. ChartProben (`Program.Bruecke.cs`): drei Maßproben,
  zwei Gegenproben, drei SVG-Proben; `ZeichenmodellWacheTests` zählt 32 Zeichenmethoden, die SVG-Wache des
  Wortberichts prüft das Bild mit.
- **Nachbesserung (E8a/7, `e71f2d4a`).** Ein Stand mit gerechnetem Ergebnis, zu dem der Verlauf keine Reihe trägt (nach
  dem letzten Lauf angehakt), fehlte in Block 2 und in der Gliederung still mit „—"; `Zahlungsgliederungen.Abweichend`
  nimmt ihn jetzt auf, und `WIRT_ZR_ABWEICHEND` sagt: „‹Stand›: Zu den gespeicherten Ergebnissen liegen keine
  passenden Jahresreihen vor — „Berechnen" rechnet sie neu." Ein Stand mit Fehlgrund bleibt draußen; ihn erklärt das
  Warnband.
- **Ressourcen Phase 1** (de und en): 39 neu — `WIRT_GL_*` 16 (die sechs Bestandteile, Nettobarwert, Kopf
  „Bestandteil", drei Unterzeilen, Titel, Unterzeile, „nominal {0}", „Differenz {0} − {1}", `WIRT_GL_NICHT_GERECHNET`),
  `WIRT_ZR_*` 5, `WIRT_LW_*` 5, `WIRT_FUSS_*` 5, `WIRT_BR_*` 8; geändert `WIRT_VALERI_BLOCK_2_HINWEIS`. Designer neu
  erzeugt (8 431 Blöcke). Kein SQL.

## Phase 2 und die Nachzüge

- **Merge `8a586448`** holt den Arbeitszweig `bba1f1a7` (Kühlung KU1 Welle 4 mit der neuen Referenzbasis
  `2026-09-23_R13_Kuehlung`, Testdatenbank mit Kühlung in 1017, Zielversion 113) — ohne Konflikt; je Sprache 8 430
  Einträge, Designer unverändert. Die Testdatenbank kam in der Fassung von `origin` (LFS `769143e4…`, 67 751 936 Byte,
  Schemastand 113, Katalog-Generation 9), ohne eigenen Commit.
- **E8a/8 `5c3cc58e` — das Zahlungsstrombild (U42),** gebaut nach dem Entscheid E8a‑Q1 (Lesart a) in derselben Welle:
  `ChartRenderer.Zahlungsstrom`/`ZahlungsstromModell` (1240 × 620, ein reines Pixelbild mit Wert am Element) zeichnet
  den absoluten Zahlungsstrom EINER Version in einem Szenario als gestapelte Jahresbalken — die Reihen sind die
  Positionsspalten der Mehrjahrestafel ohne Summenspalten (Investition und Ersatz, Betrieb, Energie, CO₂-Abgabe,
  Einspeisung, KWK-Zuschlag, bis zu drei Steuerspalten, PV-Vergütung, die KWKG-Pauschale im Jahr 0), Einnahmen nach
  oben, Ausgaben nach unten, feste Farbe je Spalte; die Ersatzjahre tragen ein Band mit Dreieck, der Schlüssel steht
  unter der Achse; keine Differenz, kein Restwert — die Unterzeile sagt „ohne Restwert". **Seite:** in Block 2 unter
  der Jahrestafel, es folgt der Wahl von Stand und Szenario; die Werte kommen aus derselben Verlaufslinie wie die
  Gliederung, nur wo die Jahresreihen zum gespeicherten Lauf passen (`Zahlungsgliederungen.Posten`). **Wortbericht:**
  über jeder Mehrjahrestafel (je Version, Szenario Erwartet), ein vorhandener Baustein ohne Umbau. ChartProben
  (`Program.Zahlungsstrom.cs`): drei Maß-, zwei Gegen-, drei SVG-Proben (lückenlose Stapelung, Marken der Ersatzjahre);
  die Wache der Zeichenmethoden steht auf 33, die Wortbericht-SVG-Wache prüft das Bild mit. Ressourcen `WIRT_ZS_TITEL`,
  `WIRT_ZS_UNTER`, `WIRT_ZS_ERSATZJAHR`, `WIRT_ZS_LEER`. Die PNG-Bilder sind angesehen, im Browser ist das Bild nicht
  geprüft.
- **E8a/9 `51bf6d6b`** berichtigt den bunit-Fall aus E8a/1: Er las den Zeichenzähler erst nach dem Umschalten und
  verglich das Spannenbild samt der Ereigniskennungen, die bunit je Instanz fortzählt. Der Zähler steht jetzt vor dem
  Klick, verglichen wird das SVG ohne die `blazor:`-Kennungen; die Aussage bleibt.
- **Merge `ac51fdb5`** holt `2edc081e` (#450 Administrationsdialoge Stufe 5, Merge mit KU1 Welle 4) — ohne Konflikt; je
  Sprache 8 493 Einträge (59 aus #450, 43 aus E8a), Designer 8 494 Blöcke, unverändert beim Neuerzeugen.
- **Nachzüge:** `b5de6db9` (`7a32b6f3`, nur der Protokoll-Nachtrag zu #450), `45154b9f` (`cd2ac9ec`, #457 Preisbasis am
  Arbeitspreis: ein Schlüssel umbenannt, zwei neu — je 8 495 Einträge) und `bf113cf8` (`ada7d1ac`, #456
  KI-Maskensteuerung: 18 Schlüssel neu — **je 8 513 Einträge, Designer 8 514**); alle ohne Konflikt, Schlüsselmengen
  beider Sprachen gleich, keine Doppel, Nähte geprüft, Designer neu erzeugt und unverändert; Testdatenbank unberührt.
- **Merge `485052c6`** auf dem Hilfszweig `pm5` über `ada7d1ac` (25 Dateien, +4 841/−46; der Baum gleicht `bf113cf8`).

## Fragen aus der Etappe

Der Phase‑1-Bericht nennt vier Fragen; der Anwender hat sie am 23.09.2026 (21:30) entschieden, im Wortlaut: „Vier Fragen
aus E8a: 1-4 Empfehlung". Sie stehen im Entscheidungsregister als **R‑E8a**.

| Frage | Lesarten | Empfehlung | Stand |
|---|---|---|---|
| **E8a‑Q1** Zahlungsstrombild U42 | (a) gestapelte Jahresbalken des absoluten Zahlungsstroms einer Version mit Stand- und Szenariowahl wie Block 2, Reihen wie die Spalten der Mehrjahrestafel, Ausgaben nach unten, Ersatzjahre markiert, keine Differenzdarstellung; (b) nur Netto je Jahr mit kumulierter Linie; (c) weglassen — die Tafel in Block 2 genügt | a, als eigener kleiner Schritt mit Bildprobe | **entschieden: a** — gebaut E8a/8 in derselben Welle |
| **E8a‑Q2** Leitversion | gebaut: die größte Kapitalwertdifferenz im Erwartungsfall, in Sicht 2 der Stand B; Alternativen: die vorgeschlagene Version oder eine Auswahlliste | so lassen | **entschieden: so lassen** |
| **E8a‑Q3** Jahresreihen gleich beim Öffnen | nur mit mitgespeicherten Jahresreihen, also einer neuen Fassung des Nachweisumschlags | nicht in E8 — E8 sieht keine Speicheränderung vor | **entschieden: nicht in E8** — bis zum ersten Lauf der Sitzung steht die Hinweiszeile |
| **E8a‑Q4** Fußzeile U48 | nach dem Merge mit e8b in dieselbe Reihe wie die Knöpfe setzen | ja | **entschieden: ja** — erledigt mit #455 (E8b-Nachzug) |

## Nachweis „keine Rechenwirkung" und Bildprobe

- **Rechenwerte:** Alles in dieser Etappe ist Ausgabe; gerechnet wird nichts Neues. `Zahlungsgliederung` ordnet das
  fertige Zahlungsbild und prüft, dass die sechs Barwerte den Kapitalwert ergeben; gezeigt wird nur, was zum
  gespeicherten Ergebnis passt. Die Ankertests (`WirtschaftlichkeitAnkerTests`) laufen im vollen Lauf unverändert
  grün; keine Simulationsgröße ist berührt.
- **Referenzlauf** 13/13 PASS gegen `2026-09-23_R13_Kuehlung`, 4 145 687 Werte in der Toleranz, 387/387 CSV
  byte-gleich (Phase 2 und Gate).
- **Bildprobe Phase 1:** 153 Prüfungen, 0 Verstöße, 139 Hashes — auf Windows gegen den selbst gebauten Basisstand
  `591229e1` auf demselben Rechner: 132 alte Hashes gleich, 7 neu (`kapitalwert_bruecke`,
  `kapitalwert_bruecke_unter_referenz`, `kapitalwert_bruecke_leer`, `kapitalwert_bruecke_schritt_wirkt_a/_b`,
  `kapitalwert_bruecke_reihenfolge_wirkt_a/_b`).
- **Bildprobe Phase 2:** 161 Bilder geprüft, 0 Verstöße; Hashliste 146 Zeilen — 132 aus der Basis `591229e1` gleich
  (darunter die 21 Bilder des Zapfprofilgenerators), 7 Brückenbilder und 7 Zahlungsstrombilder (`zahlungsstrom`,
  `zahlungsstrom_ohne_ersatz`, `zahlungsstrom_leer`, `zahlungsstrom_ersatzjahr_wirkt_a/_b`,
  `zahlungsstrom_vorzeichen_wirkt_a/_b`) neu. Die LIESMICH der ChartProben trägt einen Abschnitt „Etappe E8a".

## Abweichungen vom Mockup und vom Auftrag

- **Jahresreihen erst nach einem Lauf in der Sitzung** — gespeicherte Ergebnisse tragen keine; bis dahin steht eine
  benannte Hinweiszeile (E8a‑Q3).
- **Die Fußzeile U48** stand als eigene Zeile über dem Knopffuß, nicht in derselben Reihe — Abstand zum Knopf
  „Anhang-E-Checkliste…" (U43), den e8b dort einbaut; mit #455 in die Knopfreihe (E8a‑Q4).
- **Die Unterzeilen der Gliederung** heißen „nach Zuschussabzug", „einschließlich CO₂-Abgabe" und „zahlungswirksam —
  Block A" statt der Kategorienummern; Nullwerte stehen als „0", nicht als „—".
- **Die Zeile in U47** heißt „Restwert am Ende, nominal".
- **Die Brücke** hat eine Euro-Achse mit Raster und die Unterzeile im Bild.
- **Im Wortbericht** steht die Brücke nach den Verlaufsbildern und vor der Mehrjahrestafel, weil der Bericht keine
  Gliederungstafel hat — eine weitere Bildstelle mit Überschrift 2 im bestehenden Baustein; die Wachen für die
  Reihenfolge der Überschriften 1 und die Tabellenzahl berührt das nicht.
- **Das Zahlungsstrombild** steht in Block 2 der Darstellung „ValERI-Bewertung" unter der Jahrestafel (das Mockup
  zeichnet es in „Woraus entsteht die Zahl?") und im Wortbericht über jeder Mehrjahrestafel; die Unterzeile sagt
  „ohne Restwert".
- **Die LIESMICH der ChartProben** ist um den Abschnitt „Etappe E8a" ergänzt.
- **Die Bilder** sind im eingebauten Browser nicht geprüft, weil die Seite nur in der Anwendung läuft; Nachweis sind
  die bunit-Tests und die angesehenen PNG-Bilder der Bildprobe.

## Zahlen und Abnahme

- **Phase 1** (Worktree `e8a`): Kern-Filter (Release) und Windows-Schale (Debug x64) 0 Fehler, Designer ohne
  Abweichung, ChartProben 153/0 — kein `dotnet test`, kein Referenzlauf.
- **Phase 2** (nach dem Merge `ac51fdb5`): Kern-Filter und Windows-Schale 0 Fehler; bunit der Seite 302/302; voller
  Lauf `WP-Plan.Kern.slnf` 0 Fehler, **11 868 bestanden, 1 übersprungen** (EPOS.Kern 5 331, EPOS.UI 5 600, KiKern 524,
  SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen); Referenzlauf 13/13 PASS gegen R13, 4 145 687 Werte,
  387/387 CSV byte-gleich; ChartProben 161 Bilder, 0 Verstöße, Hashliste 146 Zeilen; Designer unverändert. Ein
  gefilterter Kern-Lauf (68/68) lief neben einem fremden Testprozess — Befund unten; gezählt sind die Läufe danach.
- **Nach den Nachzügen** (`bf113cf8`): bunit 343/343, Kern-Wachen 128/128.
- **Gate auf dem Gesamtstand mit #455** (Merge `704356a4` = e8a und e8b über `ada7d1ac`, 22:32–22:38), grün: Kern-Filter
  (Release) 0 Fehler; ChartProben 161 Bilder, 0 Verstöße; Tests Kern-Filter 0 Fehler, **11 942 bestanden,
  1 übersprungen** (EPOS.Kern 5 366, EPOS.UI 5 639, KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 und 1
  übersprungen); Dokumentationswachen 26/26; Referenzlauf 13/13 PASS gegen `2026-09-23_R13_Kuehlung`, 4 145 687 Werte
  in der Toleranz, 387/387 CSV byte-gleich; SQL-Prüfer 1 737 Texte, 0 Fundstellen; Windows-Schale 0 Fehler.
- **Tests** (neu oder erweitert): `ZahlungsgliederungTests` (neu, 21 Fälle: Summe der Barwerte, Zuordnung jeder
  Zahlung, Nominalreihe, Zins je Szenario, drei Läufe = Lauf, Abgleich, Differenz, Leitversion, Hülle nach Lauf und
  über T, Tafeln, Prüffall 1040, Fußzeile, Brücke, Zahlungsstrom), bunit `WirtschaftlichkeitErgebnisansichtTests`
  (19 → 26), `KapitalwertVerlaufAbschnittTests` (15 → 16), `WirtschaftlichkeitSeiteTests` (52 → 53),
  `WordBerichtSvgWacheTests` (Brücke und Zahlungsstrom), `ZeichenmodellWacheTests` (32 → 33 Zeichenmethoden).
- **Ressourcen** (de und en): **43 neu** — die 39 der Phase 1 und `WIRT_ZS_TITEL`, `WIRT_ZS_UNTER`,
  `WIRT_ZS_ERSATZJAHR`, `WIRT_ZS_LEER`; **einer geändert** (`WIRT_VALERI_BLOCK_2_HINWEIS`); keiner gestrichen. Je
  Sprache 8 513 Einträge über `ada7d1ac`, Designer 8 514 Blöcke.
- **Kein Schemaschritt;** `SchemaStand.Zielversion` = 113. Die Testdatenbank ist unverändert (LFS `769143e4…`, die
  Fassung zur Basis R13, Katalog-Generation 9).

## Abnahme am Gerät (A‑E8a‑1, Windows und iPad)

(1) Darstellung „ValERI-Bewertung", Block 2: die Klapplisten „Stand:" und „Szenario:" (Vorgabe die Leitversion im
Erwartungsfall), die Jahrestafel mit den sechs Bestandteilen, Netto und Barwert, „Summe nominal" und Barwert; vor dem
ersten Lauf der Sitzung die Hinweiszeile, nach „Berechnen" die Tafel; darunter das Zahlungsstrombild desselben Standes
im selben Szenario (Ausgaben nach unten, Ersatzjahre mit Band und Dreieck). (2) Block 4: Bandbreite, Spannenbild,
Vorschlag, Hinweistext, Verlauf, Sensitivität; je Darstellung genau ein Verlauf. (3) „Woraus entsteht die Zahl?": die
Gliederung des Kapitalwerts mit Nominalsumme unter jedem Barwert und der Spalte „Differenz ‹Leitversion› −
‹Referenz›", deren Summe die Kapitalwertdifferenz ist; die Tafel folgt der Szenario-Klappliste. (4) Das Brückenbild
unter der Gliederung und im Wortbericht (nach den Verlaufsbildern); im Wortbericht über jeder Mehrjahrestafel das
Zahlungsstrombild. (5) „Was ist angenommen?": die Tafel „Was daraus im Lauf wird" unter dem Hinweistext. (6) Die
Fußzeile vor einer Pflege „Drei Szenarien gerechnet · Annahmen aus Vorgaben, nichts gepflegt", nach einer Pflege im
Parameterdialog „Annahmen gepflegt: …"; mit #455 steht sie in der Knopfreihe. (7) Englisch.

## Befunde nebenbei

- **Die Windows-Messlatte des Gates** (außerhalb des Repos) hatte 111 Zeilen und war schon vor E8a veraltet: Ihr
  fehlten die 21 Bilder des Zapfprofilgenerators (#451), das Gate-Skript meldete deshalb auch ohne E8a „ungleich".
  Mit ihnen und den E8a-Bildern: 111 → 139 (Phase 1) → 146 (Phase 2); die lokale Messlatte ist auf 146 Zeilen
  nachgezogen. Die Messlatte im Repository (Linux) trägt die 14 neuen Bilder noch nicht; sie kommen mit dem nächsten
  Einfrieren dazu (LIESMICH der ChartProben).
- **Die Jahresreihen sind nicht gespeichert.** Block 2, die Gliederung mit Nominalsumme, das Brückenbild, das
  Zahlungsstrombild und die Tafel „Was daraus im Lauf wird" stehen erst nach einem Lauf in der Sitzung; beim Öffnen
  einer Vergleichsgruppe zeigt die Seite die Hinweiszeilen (E8a‑Q3: nicht in E8).
- **Der Platz der Fußzeile:** über dem Knopffuß statt in der Knopfreihe, um den Merge mit e8b nicht zu belasten —
  nach E8a‑Q4 in die Knopfreihe, erledigt mit #455.
- **Ein Codekommentar ist mit E8a/8 überholt:** Der Kopfkommentar der Darstellung „ValERI-Bewertung" in
  `EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor` (Zeile 478) sagt noch „Das Zahlungsstrombild (U42) ist nicht
  gebaut - Konzeptentscheid offen". Kein Papier dieses Auftrags ändert Code; berichtigen mit dem nächsten Auftrag an
  der Seite.
- **Die Testregel ist einmal verletzt worden:** Ein gefilterter Kern-Lauf (Zahlungsgliederung, beide Wachen,
  Blattstruktur, NachtraegeE5b, Anker; 68/68 grün) lief parallel zu einem fremden Testprozess aus dem Worktree
  `agent-afe1c27a…` — `tasklist` war abgefragt, aber nicht abgewartet. Alle späteren Läufe liefen über ein Warteskript
  mit 60-Sekunden-Prüfung.
- **Die Doppelbelegung „fünf Blöcke"** (Konzept § 2.11.3 gegen die Nummerierung 1–5) und die fehlende E6‑Q1 in der
  § 5-Tafelzeile E8 (Faktenblatt E8, Unklarheiten 1 und 2) sind mit diesen Papieren aufgelöst.

## Offen

- **Abnahme am Gerät** A‑E8a‑1 (sieben Punkte oben).
- **Nächste Etappe: E8 Teil b** (#455) — die Formelmappe Stufen 0 bis 3 samt Blattstruktur- und ClosedXML-Wache, die
  Anhang-E-Checkliste (U43) und die Anhang-D-Gegenprobe gegen `KapitalwertRechner.Rechne`; mit ihr die Fußzeile U48
  in der Knopfreihe (E8a‑Q4). Danach **E9** (V‑E, Szenarioabdeckung; ihre Schemaschritte nehmen die nächste freie
  Nummer bei der Umsetzung).
- **Jahresreihen beim Öffnen** nur mit einer neuen Nachweisfassung — nicht in E8 (E8a‑Q3).
- **Messlatte der ChartProben im Repository** auf Linux neu einfrieren (14 neue Bilder der Etappe E8a).
- **Der überholte Codekommentar** zu U42 in `WirtschaftlichkeitSeite.razor`.
- **Push** nach der Regel des Anwenders ohne Rückfrage aus dem Hilfszweig `pm5` in einem Zug mit #455 auf
  `ios_migration_september` und `main`; Kern-Lauf danach.
- **Papiere mit der Statuszeile:** Register (E6‑Q1 gebaut #454, E5b‑4 vollständig, V‑1, EZ‑2, EZ‑9, EZ‑10, neue Familie
  R‑E8a), Konzept (Kopf, § 2.10, § 2.11.3 Fußnote, § 2.11.4 V‑C, § 2.13 (5), § 6.1, § 7 und Anhang), Protokoll der
  Entscheidwege (§ 8.13, § 8.14, Kopf), Analysepapier (Kopf, Nachtrag, § 5 mit E6‑Q1 in der Tafelzeile), Szenarienkonzept
  (Kopf, § 11, § 11.1), Mockup (Anhang U41, U42, U46 bis U49 erledigt, Ressourcentafel der Kategorie 8, Stand-Absatz),
  Logbuch-Sätze und die Wiki-Quelle der Seite Wirtschaftlichkeit, Index Reporting.
