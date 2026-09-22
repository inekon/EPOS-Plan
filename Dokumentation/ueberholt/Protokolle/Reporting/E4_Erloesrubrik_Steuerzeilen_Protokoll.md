# E4 — Erlösrubrik und Steuerzeilen: zwei Steuerbeträge, Gründe je Position, Komponente innen (Protokoll, 22.09.2026)

Statuszeile #432 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Etappe E4 des Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 3 R8, § 4 A12, § 5 Zeile E4); Konzept § 6.3 Punkte 9a (B7‑1), 9d (B7‑4) und 9i; Mockup
`../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Kategorie 7 „Erlösrubrik" und Anhangzeilen U6 und U7 — vom
Anwender am 22.09.2026 als verbindliche Zielvorgabe abgenommen. Entscheide: Q15 (22.09.2026, U6 ja, Leistungsanteil
projektweit), A12 (Näherung V‑4, ausgewiesen), Q11 (HT/NT gehört zu E7, nicht hierher). Anwender 22.09.2026: „E4 starten mit
Q15 ja". Zweig `e4` von `76893913`, Commits `1a4ec24d` (E4/1, U7), `ee3efacc` (E4/2, 9d), `74aa192d`, `f9fe94a1` und
`468ce047` (E4/3, E4/3a und E4/3b: U6 samt nachgezogener Blattstruktur-Wache); Merge `1190622c` in
`ios_migration_september`. Beide Teile Opus 5 im Worktree `.claude/worktrees/e4`.

## Befund vor der Welle

- **B7‑1 (9a):** `SteuerErgebnis.EnergiesteuerEur` kam als eine Summe zurück; die Erlösrubrik konnte § 53/§ 53a
  (Blockheizkraftwerk) und § 54 (Heizstoff im Kessel) nur gemeinsam ausweisen. An einem Projekt mit Blockheizkraftwerk und
  Kessel war der Betrag keiner Anlage zuzuordnen (Mockup: 21.203,4 € beim Blockheizkraftwerk, 2.885,7 € beim Kessel).
- **B7‑4 (9d):** Die Begründungen `STEUER_*` standen als ein mit „ | " verbundener Text im Hinweisfeld des Laufs. Eine
  Nullzeile der Rubrik konnte deshalb nur die Bedingung ihrer Position nennen („nur produzierendes Gewerbe"), nicht die
  Feststellung des Laufs.
- **U6:** Der Zeilenkatalog trug kein Anlagenfeld; die vermiedenen Stromkosten entstanden projektweit aus
  `VermiedenMengeMWh`; die Strommatrix trennt nach Tarifzone, nicht nach Anlage (Befund R8, V‑4). A und B als äußere
  Ordnung waren gebaut, die Gliederung nach Komponente nicht.

## Teil a — U7 und 9d (E4/1, E4/2)

- **Zwei Beträge, zwei Zeilen (U7).** `SteuerErgebnis` führt `Energiesteuer53Eur`, `Energiesteuer54Eur` und
  `Energiesteuer54SockelEur`; `EnergiesteuerEur` ist nur noch ihre Summe (Eigenschaft, nicht Feld). Je gerechneter Position
  ein `EnergiesteuerNachweis` (Paragraf, Menge in der gesetzlichen Einheit, Satz, Betrag). Die Rubrik führt
  `ERL_A_ENERGIESTEUER` (§ 53/§ 53a) und `ERL_A_ENERGIESTEUER_54` (§ 54), je mit Herleitungszeile; beim § 54 schließt der
  Sockelbetrag die Kette. Ein vor U7 gebuchter Stand kennt seine Aufteilung nicht und zieht die Gruppe auf die eine
  Gesamtzeile zurück — die Summe des Blocks A bleibt so in jedem Fall zahlengleich. Nachweisumschlag Fassung 4 (beide
  Beträge, Sockel, Nachweisliste); ältere Fassungen laden und melden die fehlende Aufteilung. Zahlenprobe im Rechnertest:
  21.202,71 € nach § 53a und 2.885,72 € nach § 54, zusammen 24.088,43 €.
- **Gründe je Position (9d).** `SteuerPosition` (`ENERGIEST_53`, `ENERGIEST_54`, `STROMST_BEFREIUNG`,
  `STROMST_ENTLASTUNG`); `SteuerErgebnis.PositionsGruende` ordnet jede Begründung ihrer Position zu — der erste Grund gilt,
  er ist der, an dem die Rechnung ausgestiegen ist; die flache Liste bleibt wortgleich für das Hinweisfeld. Je Geldzeile
  eine Herleitungszeile (Text, Einzug 1, ohne Summen- und Excelwirkung): Herleitung, wo es eine gibt, sonst der Grund; ohne
  Feststellung bleibt sie leer und entfällt. KWKG- und Einspeisegrund kommen aus dem Modulnachweis. Umschlag Fassung 5.

## Teil b — U6 Komponente innen (E4/3)

- **Anlagenbezug je Zeile.** `WirtZeile.Komponente` mit den sprachneutralen Kennungen `KOMPONENTE_BHKW`, `_PV`, `_KESSEL`,
  `_PROJEKTWEIT`, dazu `Komponentenfolge` und `Komponentenname`. Die Zuordnung entsteht einmal beim Bau des Katalogs:
  KWKG-Kette, § 53/§ 53a und Stromsteuer-Befreiung zum Blockheizkraftwerk, § 54 zum Kessel, PV-Block zur Photovoltaik,
  § 9b-Entlastung projektweit; der Einspeiseerlös zur Anlage, die einspeist, projektweit, wenn beide einspeisen.
- **Gliederung.** A/B bleibt außen. Je Komponente Kopf (`WIRT_ERL_KOMPONENTE`), Zeilen, Zwischensumme
  (`WIRT_ERL_TEILSUMME`), zuletzt „projektweit" (`WIRT_ERL_PROJEKTWEIT`); die Gesamtsumme des Blocks A summiert dieselben
  Summanden wie vor U6, nicht die Zwischensummen. Block B wird nicht summiert; seine Komponentenblöcke enden mit
  „vermiedene Kosten wirksam — {Komponente}" (`WIRT_ERL_B1_WIRKSAM`). `Sichtbare()` unterscheidet Blockkopf und
  Komponentenkopf, sonst fiele der Blockkopf weg, sobald ihm ein Komponentenkopf folgt.
- **Vermiedene Stromkosten je Anlage.** `VermiedenAnlageNachweis.Verteile()` ist der eine Verteilschlüssel: Menge,
  Arbeitsanteil und § 9b-Abzug anteilig nach den Eigenverbrauchsmengen, die letzte Zeile bekommt den Rest, damit die Summe
  die Ausgangsgröße bitgenau trifft; bei mehr als einer Anlage `IstNaeherung`, und die Herleitung nennt Menge, Anteil und
  „Näherung" (A12). Der Leistungsanteil bleibt projektweit (Q15) und trägt ohne Bezugsspitze
  `WIRT_ERL_GRUND_KEINE_BEZUGSSPITZE`. Ohne Aufteilung bleibt die eine projektweite Kette (Rückfall wie bei U7).
- **Eine Zeilendefinition.** Ergebnisseite, Wortbericht, Tabellenbericht und BHKW-Vorschau brauchten keine eigene Zeile:
  Kopf-, Einzugs- und Summenbehandlung bestanden, alle vier lesen `Kennzahlen`/`Sichtbare`; die Excel-Wertspalten bleiben
  numerisch (Kopf ohne Wert, Zwischensumme als Zahl). Nachweisumschlag Fassung 6 mit `VermiedenJeAnlage` (Komponente,
  Anlage, Menge, Schlüssel, Anteil, Näherungskennzeichen); Fassungen 1–5 laden weiter. Kein Schemaschritt: die Aufteilung
  reist in der bestehenden Spalte `Nachweis_Json`.

## Abweichungen vom Mockup, mit Grund

1. **Vermiedene Menge ohne Photovoltaik.** Das Mockup teilt 1.179,7 MWh auf Blockheizkraftwerk 1.094,2 und Photovoltaik
   85,5 MWh. Gemessen: `StromMatrix.Baue` bildet „Bedarf ohne Anlage" als Strombedarf minus PV-Eigennutzung, also ist
   `VermiedenMengeMWh` allein der KWK-Eigenverbrauch. Der Lauf bringt deshalb nur das Blockheizkraftwerk in den Schlüssel;
   der vermiedene Bezug der Photovoltaik bleibt seine Ausweiszeile im Block Photovoltaik. Die Regel ist generisch und liefert
   die Mockup-Zahlen, sobald sie die Mockup-Mengen bekommt (Ankertest 293.245,6 + 22.914,0 = 316.159,6 €/a). Die Menge
   selbst zu ändern wäre rechenwirksam (drei gespeicherte Spalten in `Tab_ErgebnisWirtschaftlichkeit`) — Frage U6‑Q1, E7.
2. **Kopf „projektweit"** überall in der kurzen Form; das Mockup schreibt in Block A „projektweit — hängt an keiner
   Anlage". Ein Schlüssel trägt einen Text, die Schlüsseltafel nennt nur `WIRT_ERL_PROJEKTWEIT` (U6‑Q2).
3. **Komponentenname „Kessel"** statt des Anlagennamens „Gas-Brennwertkessel": Die Zeilen sind Summen über alle Kessel bzw.
   Module; ein Anlagenname wäre falsch, sobald es zwei gibt. Drei Schlüssel `WIRT_ERL_K_BHKW`, `_K_PV`, `_K_KESSEL`, die die
   Schlüsseltafel nicht führt (U6‑Q3).
4. **Abschluss des Blocks B** wortgleich, aber über `WIRT_ERL_B1_WIRKSAM` statt `WIRT_ERL_TEILSUMME` — „Summe …" in einem
   Block, der nicht summiert wird, wäre eine falsche Aussage.
5. **Vorschau** ohne Zeilenpräfix „projektweit: {0}": Sie liest den einen Katalog; eine zweite Beschriftungsregel wäre eine
   zweite Wahrheit.
6. **Word und Excel** zeichnen Komponentenköpfe mit der Hinterlegung der Blockköpfe; die Tabellenmittel kennen keine
   zweite Überschriftsebene.
7. **Referenzexport ohne Wirtschaftlichkeit:** `Referenzlauf/Ergebnisexport.cs` liest `Tab_Ergebnis` und die fünf
   Techniktabellen, nicht `Tab_ErgebnisWirtschaftlichkeit`. Die Rubrik kann den Byte-Vergleich nicht bewegen; der Nachweis
   der Etappe sind die Ankertests (A11), der Referenzlauf belegt allein die unveränderte Simulation.

## Zahlen und Abnahme

- **Teil a:** voller Lauf 10 439 Tests grün, Referenzlauf 13/13 (Gesamt PASS). Zeugen `SteuerGutschriftRechnerTests`,
  `ErloesrubrikTests` (Blocksumme unverändert 14.575 €), `ErgebnisNachweisPersistenzTests` (Fassung 4 hin und zurück,
  Fassung 3 ohne Aufteilung), `BhkwWirtschaftlichkeitDialogTests` (bunit: beide Zeilen, Herleitung, Grund).
- **Teil b:** gefilterte Tests 170/170 (Kern 95, UI 75), voller Lauf 10 451 Tests grün (1 übersprungen),
  Referenzlauf 13/13 PASS über 3 882 737 Werte, alle 357 CSV byte-gleich; Designer 7 514 Einträge unverändert.
  Ein Befund behoben: `BerichtBlattstrukturWacheTests` pinnt absolute Zeilennummern des Excel-Blatts; der
  Block „projektweit" mit Kopf und Zwischensumme verschob jeden Szenarioblock um zwei Zeilen — E4/3b
  (`468ce047`) zieht die Nummern nach und setzt zwei Anker auf Komponentenkopf und Zwischensumme, alle
  Zahlenanker wertgleich. Neun neue Fälle in `ErloesrubrikTests`
  (Zahlenprobe über den Verteilschlüssel, eine Anlage bekommt alles, Zwischensummen = Blocksumme A 14.575 €/a,
  Anlagenbezug und Kopffolge Blockheizkraftwerk · Kessel · projektweit, Leistungsanteil projektweit mit Grund, Ausweis in
  Komponentenblöcken, Herleitung mit Anteil und Näherung, Rückfall, Excel-Wertspalten numerisch);
  `ErgebnisNachweisPersistenzTests` (Fassung 6, Fassung 5 lädt ohne Aufteilung); bunit-Vorschau mit Zwischensumme.
- **Gate #432** auf `1190622c`: Kern-Filter 0 Fehler, ChartProben 91 Bilder gleich Windows-Messlatte, Tests 10 451
  grün (1 übersprungen: KiKern 524, SpeicherEngine 386, SpeicherPlanung 27, UI 5 213, Kern 4 301), Wachen 24/24;
  Designer 7 514 Einträge unverändert; Windows-Schale auf dem Merge-Stand im Worktree 0 Fehler.
- **Ressourcen:** 22 neue Schlüssel de/en — U7: `WIRT_ERL_ENERGIEST_54`, `WIRT_ERL_ENERGIEST_GESAMT`,
  `WIRT_ERL_ENERGIEST_HERLEITUNG`, `WIRT_ERL_ENERGIEST_SOCKEL`; 9d: `WIRT_ERL_HERLEITUNG`,
  `WIRT_ERL_GRUND_KEIN_BHKW_BRENNSTOFF`, `WIRT_ERL_GRUND_KEIN_KESSELBRENNSTOFF`, `WIRT_ERL_GRUND_KWKG_MENGE`,
  `WIRT_ERL_GRUND_KWKG_SATZ`, `WIRT_ERL_GRUND_KWKG_KONTINGENT`, `WIRT_ERL_GRUND_KEINE_EINSPEISUNG`,
  `WIRT_ERL_GRUND_OHNE_VERGUETUNG`; U6: `WIRT_ERL_KOMPONENTE`, `WIRT_ERL_PROJEKTWEIT`, `WIRT_ERL_TEILSUMME`,
  `WIRT_ERL_K_BHKW`, `WIRT_ERL_K_PV`, `WIRT_ERL_K_KESSEL`, `WIRT_ERL_B1_ANTEIL`, `WIRT_ERL_B1_NAEHERUNG`,
  `WIRT_ERL_B1_WIRKSAM`, `WIRT_ERL_GRUND_KEINE_BEZUGSSPITZE`. Ein umformulierter Schlüssel:
  `WIRT_ERL_A_ENERGIESTEUER` (nur noch § 53/§ 53a; der alte Wortlaut lebt als `WIRT_ERL_ENERGIEST_GESAMT` für
  die Rückfallzeile eines Standes vor U7). Netto kein entfernter Schlüssel (`WIRT_ERL_ENERGIEST_SATZ` kam mit
  E4/1 und ging mit E4/2). Designer 7 514 Einträge, wiederholbar. Kein Schemaschritt.

## Abnahme am Gerät (A‑E4‑1, Windows)

Wirtschaftlichkeitsseite eines Projekts mit Blockheizkraftwerk und Kessel (Testdatenbank 1030): Block A gliedert nach
Blockheizkraftwerk, Kessel und „projektweit", jeder Block mit Zwischensumme, Gesamtsumme wie vor der Welle; die
Energiesteuer steht als zwei Zeilen (§ 53/§ 53a beim Blockheizkraftwerk, § 54 beim Kessel) mit Herleitungszeile; eine
Nullzeile nennt den Grund des Laufs. Block B endet je Komponente mit „vermiedene Kosten wirksam". BHKW-Dialog, Gruppe
Vorschau: dieselbe Gliederung mit „Summe Blockheizkraftwerk". Wortbericht und Excel: Komponentenköpfe und Zwischensummen
vorhanden, Wertspalten in Excel numerisch. Ein vor der Welle gerechneter Stand zeigt weiter eine Energiesteuerzeile, bis er
neu gerechnet ist.

## Offen

- **U6‑Q1 (rechenwirksam, E7):** Soll „Bedarf ohne Anlage" der Differenzmethode weiter *ohne Blockheizkraftwerk* heißen
  (heutiger Stand) oder *ohne jede Eigenerzeugung* (Mockup-Beispiel)? Nur im zweiten Fall enthält die vermiedene Menge den
  PV-Eigenverbrauch und die § 9b-Korrektur greift auch auf ihn; betroffen `VermiedenArbeit`, `VermiedenLeistung`,
  `VermiedenGesamt`, `VermiedenEntlastung9b` — Ausweisgrößen, aber gespeicherte Spalten; Kapitalwert unberührt.
  **Entschieden 22.09.2026 (Anwender, nach Empfehlung und genauerer Erläuterung): ohne jede Eigenerzeugung**, KWK-Split
  unverändert, Umsetzung mit E7 und A/B-Nachweis, Anker 316.159,6 €/a.
- **U6‑Q2 bis U7‑Q2 — entschieden 22.09.2026 (Anwender, nach Empfehlung):** Block-A-Kopf „projektweit" bleibt kurz
  (kein zweiter Schlüssel); Komponentenname „Kessel" statt des Anlagennamens; „Herleitung" bleibt der eine Titel der
  Herleitungszeile für Herleitung wie Grund; vor U7 gerechnete Stände zeigen bis zum nächsten Lauf die eine
  Energiesteuerzeile, kein Nachziehlauf (wie Nr. 31). Das Mockup ist an den zwei Kopfwortlauten der Kategorie 7
  angeglichen („projektweit", „Kessel"); die Abweichungen 2 und 3 oben sind damit der abgenommene Stand.
- **Mockup-Nachzug:** Schlüsseltafeln (die `geplant`-Marken zu U6 und U7, die neuen Schlüssel aus Abweichung 3 und 4) und
  Anhangzeilen U6/U7 auf „erledigt" — mit den Papieren dieser Statuszeile.
