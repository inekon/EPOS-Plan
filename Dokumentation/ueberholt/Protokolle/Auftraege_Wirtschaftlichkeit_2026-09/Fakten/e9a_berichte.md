

===== Bericht 1 (2026-09-24T00:45:37.312Z) =====

**Phase 1 von E9a ist fertig. Gebaut und committet ist alles; Kern-Filter und Windows-Schale bauen mit 0 Fehlern. Kein einziger Test ist bisher gelaufen, auch der Referenzlauf nicht.** Ich warte auf „Tests freigegeben".

Arbeitsort war `C:\Waermeplan\EPOS-Plan\.claude\worktrees\e9`, Zweig `e9`. Kein Push, kein Merge, kein Stash.

## Commits (8, alle auf 46023235)
| Commit | Inhalt |
|---|---|
| `7f481f6a` | E9a/1 Schritt 116 (B), Szenariorahmen |
| `12d3f0a6` | E9a/2 Schritt 117 (C), Trägerpreise |
| `ac7f3b90` | E9a/3 Schritt 118 (D), Erlössätze, Zielversion 118 |
| `399a5a18` | E9a/4 Testdatenbank auf Schemastand 118 |
| `1626b5ff` | E9a/5 Daten |
| `d2ef1862` | E9a/6 Kern liest die Paare |
| `a066b2fd` | E9a/7 Berichte |
| `0da92072` | E9a/8 Tests |

## Bau und Prüfung je Punkt
- **1–3 Schema:** 18 Spalten. 116 hat Zeitraum als ganze Jahre (eigene Spalte, nicht `Szen_*_Dauer`) und Menge. 117 hat sechs Preisspalten an `energy_project_settings`. 118 hat je vier Spalten an der Wirtschaftlichkeit und an der PV-Zeile.
  - Doppelpflicht erfüllt: `WirtschaftlichkeitCtrl` (CREATE und `SpalteSicher`), die Nachzieh-Liste der Testdatenbank und das Werkzeug.
  - `energy_project_settings` legt der Kern nirgends selbst an, deshalb dort nur Schema.
  - Die Spalten kopiert `VariantenCtrl` beim Anlegen einer Variante mit.
- **4 Testdatenbank:** siehe eigener Abschnitt unten.
- **5 Daten:** Neue Felder am Szenariosatz, an der Trägerkarte (sechs) und an der PV-Karte (vier).
  - Leer oder 0 heißt „wie Erwartet". Gepflegt ist ein Wert erst, wenn er sich um mehr als 1e−9 vom Erwartungswert unterscheidet.
- **6 Kern:** Die Stellen je Größe stehen unten. Erwartet bekommt immer dieselbe Referenz wie bisher.
- **7 Berichte:**
  - Die Nachweiszeile je Szenario nennt immer T und Einspeisevergütung, Mengenänderung und Trägerpreise nur, wenn gepflegt.
  - Der Parameterblock der Formelmappe hat jetzt 15 statt 12 Zeilen. Gepflegte Trägerpreise hängen je Stand, Träger und Preisart eine Zeile an.
  - Verlauf und Zahlungsgliederung laufen je Szenario über dessen eigenen Zeitraum.
  - Der Hinweistext `WIRT_SZEN_HINWEIS` bleibt, die Anhang-E-Checkliste ist unverändert.
- **8 Tests:** 22 neue Fälle in `EPOS.Kern.Tests\SzenarioParameterTests.cs`; in `BerichtBlattstrukturWacheTests.cs` ist die Stufe-0-Prüfung auf die drei neuen Zeilen angepasst.
  - Abgedeckt je Größe: Nullsemantik, 1e−9-Regel, Wirkungsrichtung, Speicherweg über die drei Controller.
  - Auf Projekt 1030: Zeitraum je Szenario ist Zahl für Zahl ein Lauf mit diesem T; Menge ±10 % verschiebt die Energiekosten symmetrisch um die Festbeträge.
  - Außerdem Staffel-Kohärenz, Rollenmodell und die Werkzeug-Wache (liest die Repo-Datei schreibgeschützt).
  - Einspeisevergütung PV/KWK auf einem synthetischen Prüfstand.
- **9 A/B-Nachweis:** kommt in Phase 2.
- **Weitere Prüfungen:** SQL-Prüfer mit 1752 Texten, 0 Fundstellen. Designer unverändert und wiederholbar.

**Neue Ressourcenschlüssel (17, de/en, Designer neu erzeugt):**
- `WIRT_SZ_`: `LEISTUNGSPREIS_OHNE_WIRKUNG`, `ROLLEN_STROMPREIS`, `ROLLEN_EINSPEISUNG`, `PV_DIALOG_EINSPEISUNG`, `TRAEGERPREISE`, `TP_ARBEIT`, `TP_GRUND`, `TP_LEISTUNG`
- `WIRT_FM_PARAM_`: `MENGE`, `VERGUETUNG`, `VERGUETUNG_KWK`, `TP_ARBEIT`, `TP_GRUND`, `TP_LEISTUNG`
- `WIRT_ANN_`: `MENGE`, `VERGUETUNG`, `VERGUETUNG_KWK`

## Stellen, die je Szenario lesen
- **Zeitraum:** `WirtschaftlichkeitParameter.FuerSzenario`. Damit wirkt er auf Horizont, Restwert, Ersatz, PV-, KWKG- und CO₂-Reihen. Für den Verlauf gibt es `BerechneVerlaufSzenarienJeZeitraum`; das nutzen der Excel-Verlaufsblock, die Word-Bausteine und die Hülle `KapitalwertVerlaufHuelle`.
- **Menge:** `WirtschaftlichkeitCtrl.Szenariodaten` mit `SzenarioMengen.Variante`. Ein zweiter Aufruf derselben Funktion steckt in `EndenergieAufloeser` für die nach Endenergie bemessenen Betriebskosten.
- **Trägerpreise:** Die Regel steht einmal in `TraegerpreisSzenario.Wirksam`. Sie wird angewandt in `KostenEmissionRechner.LadeTraeger` (Energiekosten, Anlagenzeilen, Parameterblock) und in `StromArbeitspreisEurJeKwh` (PV-Mehrbezug, vermiedener Bezug).
- **Einspeisevergütung PV und KWK:** `FuerSzenario`.
- **DV-Entgelt und PPA-Preis:** `ProjektPhotovoltaikCtrl.FuerSzenario` in der PV-Vergütungsrechnung.

## Lücke bei 115
- **Mechanisch geht es:** Die Migration überspringt Schritte mit Nummer ≤ Stand und läuft von 114 direkt nach 118. Werkzeug und Nachzieh-Liste prüfen jede Spalte einzeln und setzen die Marke erst am Ende. Die Kopie ging von 114 auf 118.
- **Die dokumentierte Regel „lückenlos aufsteigend" ist verletzt**, bis 115 da ist.
- **Das eigentliche Risiko:** Eine Anwender-Datenbank, die ein E9a-Build ohne 115 auf 118 hebt, führt 115 später nie mehr aus. Deshalb muss z3 (Schritt 115) vor oder zusammen mit E9a in den Arbeitszweig.
- **Beim Merge der Testdatenbank:** die Datei von z3 (Stand 115) nehmen und das Werkzeug darüber laufen lassen.
- **Stand origin:** Laut Fetch ist 115 noch nicht gepusht, `Zielversion` dort ist 114.

## Testdatenbank
- LFS-Zeiger von `sha256:8a3bebaf…` (67.751.936 Byte, Stand 114) auf `sha256:64383984…` (67.756.032 Byte, Stand 118).
- Alle 18 neuen Spalten sind leer, die Referenzprojekte tragen also keine Pflege.
- Ein zweiter Werkzeuglauf ändert den Inhalt nicht. Die Dateibytes ändern sich dabei, weil das Werkzeug die Datei neu schreibt.

## Abweichungen und Befunde
1. **Formelmappe Stufe 1/2 rechnet nur Erwartet.** „T je Szenario" wirkt deshalb im Parameterblock und im Verlaufsblock (Jahre jenseits von T_s bleiben leer). Kapitalwert und Annuität je Szenario gibt es in der Mappe nicht als Formel.
2. **Neuer Stand auf origin:** 39 Commits (E8c). Ein Probe-Merge per `git merge-tree` (ohne Arbeitsbaum) zeigt Konflikte nur in den beiden `.resx`, sechs Codedateien lassen sich automatisch zusammenführen. Nach dem Merge muss der Designer neu erzeugt werden. Die Testdatenbank ist auf origin unverändert.
3. **Szenariopreis ohne Erwartet-Preis:** Best und Worst rechnen dann, Erwartet zeigt die Datenlücke. Das sollte der Dialog in E9b kennzeichnen.
4. **Menge und Ertragsänderung multiplizieren sich** auf den Erlöszeilen (+10 % und +10 % ergeben +21 %). Das folgt aus Q2 a.
5. **0 in der Datenbank:** Der Wirtschaftlichkeits-Speicherweg schreibt eine 0 als 0 und liest sie als leer. Trägerkarte und PV-Karte schreiben NULL. Rechnerisch folgenlos.
6. **Zeitreihen auch bei Szenario-Leistungspreis:** Ein Strom-Leistungspreis allein im Szenario lässt den Sammler die Zeitreihen holen. Das ist richtig, kostet aber Laufzeit.
7. **Hinweistext:** `WIRT_SZEN_HINWEIS` stimmt nur noch ohne Pflege; E9b nimmt ihn heraus.

## Fragen E9a‑Q1 bis Q7 (gebaut ist jeweils die Empfehlung)
- **Q1 Umfang von D:**
  - a (gebaut): Einspeisevergütung, Einspeisevergütung KWK, DV-Entgelt, PPA-Preis.
  - b: zusätzlich Spot-Aufschlag und die beiden Marktwertfelder.
- **Q2 Mengenfaktor:**
  - a (gebaut): alle kWh-Mengen, Stundenreihen und die Bezugsspitze. Leistungen, Prozente, Gerätedaten, Festbeträge und gesetzliche Sätze bleiben; Deckel greifen nach dem Faktor.
  - b: nur Erzeugung und Einspeisung.
- **Q3 Trägerpreise:**
  - a (gebaut): Der Szenariopreis ersetzt den Preis als Ganzes. Eine gepflegte Staffel oder Saisonreihe gilt weiter, dazu eine Kohärenzzeile.
  - b: nur ein Aufschlag auf den Energieanteil.
- **Q4 Zeitraum:**
  - a (gebaut): wirkt auf Horizont, Restwert und Ersatzbeschaffungen.
  - b: nur auf den Horizont.
- **Q5 Vorgaben:**
  - a (gebaut): Die neuen Größen haben keine Vorgabe.
  - b: Vorgaben wie bei den sieben W5‑B‑9-Größen.
- **Q6 Risiko und n-jährliche Zeitpunkte:**
  - a (Empfehlung): nicht Teil von E9, nur ein Vermerk im Register.
  - b: in E9b.
- **Q7 Tarif-Rollenmodell:**
  - Befund: Im Rollenmodell ist der Erwartet-Strompreis der Reststromtarif, die Einspeisung bewertet der Einspeisetarif. Ein Szenario-Strompreis kürzt sich heraus, eine Szenario-Einspeisevergütung wirkt nicht. Gaspreise und Menge wirken weiter.
  - a (gebaut): Die Rollenpreise bleiben in allen Szenarien Erwartet. Beides wird mit einer Kohärenzzeile benannt, ebenso eine flache Einspeisevergütung neben aktivem PV-Vergütungsdialog.
  - b: eigene Best/Worst-Preise am Tarifsatz, das bräuchte einen neuen Schemaschritt.
  - c: Szenario-Strompreis als Faktor auf die Rollenpreise; nicht empfohlen, weil das die Wirkung verdeckt.

## Was damit erledigt ist
- **V‑G5 teilweise:** Rahmen, Mengen, Trägerpreise und Erlössätze je Szenario sind im Kern; die Pflege über den Dialog kommt mit E9b.
- **V‑4:** Der Hinweistext gilt nur noch ohne Pflege; er entfällt mit E9b.
- **Mockup U15 teilweise:** Der Kern ist fertig; der A/B-Nachweis folgt in Phase 2, Dialog und Wegfall des Hinweistexts mit E9b.

## Logbuch-Entwürfe (erst mit E9b veröffentlichen, Versionsnummer beim Anwender erfragen)
- *szenarien:* „Die Szenarien Günstig und Ungünstig können einen eigenen Betrachtungszeitraum, eine Mengenänderung, eigene Energieträgerpreise sowie eigene Einspeisevergütungen, DV-Entgelte und PPA-Preise führen."
- *wirtschaftlichkeit:* „Bericht und Formelmappe nennen je Szenario Betrachtungszeitraum, Mengenänderung, Einspeisevergütungen und gepflegte Energieträgerpreise."

**Offen für Phase 2:** vor jedem Testlauf `tasklist` auf fremde testhost prüfen, dann der gefilterte und der volle Testlauf, der Referenzlauf 13/13 gegen R13, die A/B-Tafeln (1030 für BHKW, PV-Projekt nach Messung, da 1042 keine PV-Anlage führt) und der Zellvergleich Wertfassung gegen Formelfassung.

===== Bericht 2 (2026-09-24T01:05:16.552Z) =====

**Phase 2 ist grün:** 12.177 Tests bestanden, 0 Fehler, 1 übersprungen. Der Referenzlauf ergibt 13/13 PASS, alle 387 CSV sind byte-gleich zu R13. In allen A/B-Fällen bleibt Erwartet bitgleich. Kein Push, kein CI-Lauf.

**Eine Regelverletzung vorweg:** Den Einzellauf der 22 neuen Tests (etwa 3 s) habe ich gestartet, während ein fremder testhost lief. Er gehörte zu z3 (`Auslieferungsvorlage.Tests`, gestartet 02:50:11). Ich hatte die tasklist-Prüfung und den Testlauf in einen Aufruf gepackt, deshalb hat die Prüfung den Lauf nicht aufgehalten. Meine Ergebnisse sind grün. Ob der z3-Lauf gestört wurde, weiß ich nicht; er sollte sein Ergebnis prüfen. Danach habe ich vor jedem Lauf getrennt geprüft und gewartet; der volle Lauf startete erst, als kein fremder testhost mehr lief.

## Merge
- **Merge-Commit:** `bdd06e6e` mit den Eltern `0da92072` (e9) und `a1df2dbe` (origin), Opus-Trailer.
- **Konflikte:** nur in `Resource.resx` und `Resource.en-US.resx`; beide Seiten hängen am Dateiende an. Beide Seiten behalten, die Naht `</data>` geschlossen.
- **Prüfung der Ressourcen:** je Sprache 8.770 Schlüssel (8.753 von origin plus 17 aus E9a), keiner doppelt, de/en deckungsgleich, BOM und CRLF. Der Designer, neu erzeugt, ist unverändert.
- **Rest:** Die Codedateien wurden automatisch zusammengeführt. Der LFS-Zeiger bleibt `64383984…`, Stand 118.
- **Bau danach:** Kern-Filter, Windows-Schale und `EPOS.Referenzlauf` je 0 Fehler.
- **origin:** unverändert `a1df2dbe`, `Zielversion` dort weiter 114; Schritt 115 ist noch nicht gepusht.

## Tests
| Projekt | bestanden | Fehler | übersprungen |
|---|---|---|---|
| EPOS.Kern.Tests | 5.503 | 0 | 0 |
| EPOS.UI.Tests | 5.737 | 0 | 0 |
| KiKern.Tests | 524 | 0 | 0 |
| SpeicherEngine.Tests | 386 | 0 | 0 |
| SpeicherPlanung.Tests | 27 | 0 | 1 |

Der gefilterte Lauf (`SzenarioParameterTests` und `BerichtBlattstrukturWacheTests`) ergab 52/52, davon die 22 neuen Fälle 22/22. Die Datenbankfälle liefen wirklich mit der Testdatenbank (74–290 ms je Fall).

## Referenzlauf gegen `2026-09-23_R13_Kuehlung`
- 13/13 PASS mit 4.145.687 Werten innerhalb der Toleranz.
- Alle 387 CSV sind byte-gleich zur Basis.

## Zellvergleich der Mappen (Messprogramm aus E8b)
Verglichen habe ich 15 Prüfgruppen: die 13 aus E8b plus `hybarten` und `hybluecke` aus E8c. Grundlage war der Stand vor E9a (`a1df2dbe`) gegen den gemergten Stand, beide auf derselben Testdatenbank.
- **Je Gruppe:** 3 Zeilen eingeschoben (Menge, Einspeisevergütung PV, Einspeisevergütung KWK), keine Zeile verloren.
- **Geänderte Zellen:** genau 2 Textzellen je Gruppe, die Annahmenzeilen Günstig und Ungünstig; sie nennen jetzt „T = 20 a" und die Einspeisevergütung. Keine Zahl weicht ab, die Formelzahl ist gleich.
- **Wertfassung gleich Formelfassung:** Excel 16 rechnet nach voller Neuberechnung in allen 15 Gruppen jede Formelzelle auf ihren gespeicherten Wert.
- **Weitere Prüfungen:** ClosedXML-Nachrechnung „abweichend 0" (nicht rechenbar sind wie bekannt nur NPV/PMT/IRR), OpenXML-Prüfung 0 Fehler, Gliederung des Wortberichts unverändert.

## A/B-Tafeln (Kapitalwerte in €, Arbeitskopie, frisch simuliert mit Zeitreihen)
| Größe | Projekt | Pflege Günstig / Ungünstig | KW Erwartet vorher = nachher | KW Günstig vorher → nachher | KW Ungünstig vorher → nachher | Wirkung je Jahr |
|---|---|---|---|---|---|---|
| Zeitraum | 1030 | T 25 / 15 a (Erwartet 20) | −31.141.243 | −31.309.742 → −38.224.910 | −31.005.298 → −23.801.792 | Restwert-Barwert 169.045 → 141.856 / 158.501 → 224.645 |
| Menge | 1030 | −10 % / +10 % | −31.141.243 | −31.309.742 → −28.253.736 | −31.005.298 → −34.007.774 | Energiekosten 1.624.616 → 1.462.515 / 1.786.718 |
| Arbeitspreis | 1030 | Erdgas 0,74 / 0,94 €/Nm³ (Erwartet 0,84) | −31.141.243 | → −30.177.956 | → −32.117.259 | Energiekosten ∓63.282 |
| Grundpreis | 1030 | Strom 2.000 / 2.800 €/a (Erwartet 2.400) | −31.141.243 | → −31.302.588 | → −31.012.326 | Energiekosten ∓400 |
| Leistungspreis | 1030 | Strom 60 / 100 €/(kW·a); Ausgangslage Erwartet 80 | −33.993.113 | −34.187.033 → −33.467.710 | −33.832.191 → −34.538.914 | ∓40.220 (20 € × 2.011 kW Bezugsspitze) |
| Einspeisevergütung KWK | 1030 | 0,07 / 0,03 €/kWh; Ausgangslage Erwartet 0,05 | −31.140.951 | +141 | −96 | Erlös KWK 22 → 30 / 18 → 11 |
| Einspeisevergütung PV | 1040* | 0,10 / 0,06 €/kWh; Ausgangslage Erwartet 0,08 | −215.178 | −210.726 → −209.909 | −219.621 → −220.176 | Erlös PV 200 → 250 / 163 → 123 |
| DV-Entgelt | 1040* | 0,20 / 0,60 ct/kWh (Erwartet 0,40, Marktprämie) | −215.280 | −210.848 → −210.767 | −219.704 → −219.759 | Erlös PV 192 → 197 / 157 → 153 |
| PPA-Preis | 1040* | 8,0 / 5,5 ct/kWh (Erwartet 7,0, sonstige DV) | −215.516 | −211.134 → −210.726 | −219.898 → −220.315 | Erlös PV 175 → 200 / 143 → 112 |

Nach dem Zurücksetzen jeder Pflege kommen die Ausgangszahlen exakt wieder. Die Differenzen sind symmetrisch bzw. proportional, wie gebaut.

\* **Warum 1040 mit Ausgangslage:** Kein PV-Projekt der Testdatenbank rechnet Energiekosten. 1026, 1028, 1040 und 1045 haben keinen Stromträger, 1007 hat Brennstoff ohne Träger; 1018, das einzige Projekt mit nennenswerter KWK-Einspeisung (27,5 MWh), ebenfalls. Nur auf der Arbeitskopie hat 1040 deshalb die Trägerpreise von 1030 bekommen (Gas und Strom). Für DV-Entgelt und PPA kommt eine Zeile im PV-Vergütungsdialog dazu. 1040 hat nur 2,27 MWh PV-Überschuss, 1030 nur 0,39 MWh KWK-Einspeisung; deshalb sind die Beträge in diesen Zeilen klein.

## Befunde für die Papiere
- **Zeitraum:** Bei einem kostendominierten Projekt senkt ein längerer Zeitraum den absoluten Kapitalwert. „Günstig = länger" ist also nicht allgemein günstig; das sollte der Dialog in E9b sagen.
- **Vorgaben unsortiert:** Schon ohne Pflege liegt der absolute Kapitalwert von Günstig unter Erwartet (−31,31 Mio. gegen −31,14 Mio.), weil der niedrigere Zins die Kosten höher abzinst. Das stammt aus W5‑B‑9, nicht aus E9a.
- **Mengen:** Das Vorzeichen habe ich für 1030 als Günstig −10 % / Ungünstig +10 % gewählt, weil dort die Kosten überwiegen.
- **Lücke in der Testdatenbank:** Es fehlt ein PV-Projekt mit vollständigen Preisen. Das wäre eine eigene Datenaufgabe.

**Offen:** Der zweite Nachzug mit Schritt 115 (Testdatenbank von z3 auf Stand 115, dann Werkzeuglauf auf 118) kommt wie besprochen, sobald 115 auf origin ist. Der e9-Baum ist sauber; die Arbeitskopie des Referenzlaufs habe ich entfernt.

Alle Protokolle liegen in `C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan\b08323f3-360f-41d1-97bf-333e60c170b3\scratchpad`:
- Tests: `test_voll.txt`, `test_gefiltert.txt`
- Referenzlauf: `referenzlauf_vergleich.txt`, Lauf in `R13_E9a\`
- A/B: `ab\ab_nachweis.cs`, Tafel in `ab\ab_tafel.md`
- Zellvergleich: `e9a\vergleich_basis_neu.txt`, Mappen in `e9a\basis\` und `e9a\neu\`