# E25 — Prüfprojekt 1048 „PV mit Preisen“ in der Testdatenbank, ohne Referenzrolle (Protokoll, 25.09.2026)

Statuszeile #519 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Anlass: der Befund aus E9a
(#461), dass kein Projekt der Testdatenbank eine PV-Anlage mit vollständigem Preissatz führt, und E21‑Q9 a (eigene
Welle, ein neues Projekt außerhalb der Referenzliste); der Anwender am 25.09.2026, „nehme die Empfehlungen vor: für
Später“. Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 6.3 Nr. 35 (neu, erledigt) und § 2.11.5 (Szenarien C und D);
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑E25 (neu) und R‑E21 (E21‑Q9); Herkunft des Punkts:
[`E21_Pflege_Ressourcen_Testdaten_Protokoll.md`](E21_Pflege_Ressourcen_Testdaten_Protokoll.md); Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5; das Projekt und sein Skript in [`Referenzlaeufe/LIESMICH.md`](../../../../Referenzlaeufe/LIESMICH.md), Abschnitte
„Das Prüfprojekt 1048 „PV mit Preisen“ (ohne Referenzrolle)“ und „Aktuelle Basis“ (Nachtrag). Vorgänger:
[`E26_PvAusweis_Strommatrix_R18_Protokoll.md`](E26_PvAusweis_Strommatrix_R18_Protokoll.md) (E26 ist vor E25
gemergt; seine Befunde N1 und N3 stammen aus E25). Zweig `e25` von `4434983b`; Opus 5.5 im Worktree
`.claude/worktrees/e25`: `51eee6a6` (E25/1+2), `a499feb7` (Merge `origin` `ba798d8a`, konfliktfrei), `65458a10`
(E25/1b), `a313d776` (E25/3), `e0f6d847` (E25/3, LIESMICH). Merge `3936003c` („Merge e25: Pruefprojekt 1048 PV mit
vollstaendigen Preisen in der Testdatenbank (#519)“) auf `pm26` über `origin` = `ba798d8a` (#518 samt Papieren).
**Kein Schemaschritt** — Schemastand 144 bleibt; neu sind allein Zeilen der Testdatenbank (LFS-SHA-256 `19a7b632…` →
`b68638da…`). **Keine neue Basis:** 1048 ist kein Referenzprojekt, die Basis `2026-09-25_R18_PvAusweis` bleibt.

## Befund vor der Welle (Phase 0, Testdatenbank `76dd9e48…`, Schemastand 143)

1. **Die Lücke ist bestätigt.** `Tab_ProjektPhotovoltaik` und `Tab_ProjektTarif` haben 0 Zeilen; PV-Kostenpositionen
   (Komponente 3) führt nur 1026, mit 0 €; einen Stromträger 60 mit Preis hat nur 1030. Frei ist die Projekt-ID 1048
   (`sqlite_sequence` 1047; `ProjektDuplizierenCtrl.cs:378` vergibt MAX + 1).
2. **Die Vorlage 1040:** Wärmepumpe, Kessel mit Träger 63, PV 20 × 260 W = 5,2 kWp, fünf Puffer, Kühlbetrieb 0,
   20 Investitionszeilen mit 54.975,50 €; das Gebäude 10645 rechnet auf dem Tagesbilanz-Weg, in der Kopie wird
   `Gebaeude_Modell` NULL (VDI 6007, wie 1045). Ausgeschieden: 1045 (Ost/West, der Wechselrichter kappt), 1026
   (Prüffall ohne Strom), 1028 (Batterie und Solar schlucken den Überschuss), 1007 und 1046 (Brennstoff ohne Träger,
   Flotte), 1017 und 1047 (Kühlbetrieb).
3. **Der Kopierweg:** `ProjektDuplizierenCtrl.Duplizieren` aus einem dotnet-Dateiskript — er kopiert keine Ergebnisse
   und keine `Tab_ProjektPhotovoltaik`. Kostenpositionen ohne neue Katalogzeile über die StammIDs 80 (Photovoltaik),
   149 (Wartung/Inspektion PV) und 150 (Instandhaltung PV); nicht über `KostenVorlagenUebernahmeCtrl.AusVorlage`,
   das fünf Katalogzeilen anlegte und mit `NutzungsdauerID` `NutzungsdauerTests.cs:140` bräche. Die Szenariowerte der
   Positionen sind €-Beträge (`InvestKaskade.cs:200`).
4. **Die Szenariospalten:** `energy_project_settings.custom_price_*_best/_worst`,
   `Tab_ProjektWirtschaftlichkeit.Einspeiseverguetung_Best/_Worst`, `Szen_*`, `Tab_ProjektPhotovoltaik.DvEntgelt/
   PpaPreis_Best/_Worst` (Konzept § 2.11.5); eine flache Einspeisevergütung neben einem aktiven Vergütungsdialog ist
   wirkungslos (E9a‑Q7).
5. **Keine Ergebniszeilen nötig:** `BerichtsDatenSammler.cs:363-376` rechnet frisch (0,2–0,4 s), wie bei 1045, 1046
   und 1047.
6. **Werkzeuge und Wachen:** das Werkzeug `Testdatenbankschema` ist generisch; die Auslieferungsvorlage entfernt alle
   Projekte (`Projektsicht.cs`, `Vorlagenbau.cs:94-108`), ihre 36 Tests zählen keine Projekte, solange keine
   Katalogzeile dazukommt; die Wiki-Wache (`WikiProduktdatenWacheTests.cs:88-95`) liest Namen auch aus
   Projekttabellen — darum bleiben die Gerätezeilen unbenannt, nur Projektname und Beschreibung sind neutral.
7. **Messprobe** (Kopie 1040, VDI 6007, Preissatz wie gebaut): 20 Module = 5,20 kWp — Eigenverbrauch 2,60 MWh,
   Einspeisung 2,44 MWh, Energiekosten 10.137 €/a, Investition 61.216 €, Erlös PV 195,20 / 214,72 / 175,68 €/a,
   Kapitalwert Erwartet −247.194,45 €; 40 Module = 10,40 kWp — 3,07 / 7,06 MWh, 9.507 €/a, 67.456 €, 564,80 / 621,28 /
   508,32 €/a, Kapitalwert −237.134,73 €.
8. **Nebenbefunde:** **N1** — `Ergebnis.Photovoltaik.Stromproduktion` lag unter dem Überschuss (`SimulationRunner.cs:989`
   summierte die Direktverbrauchsreihe); **N3** — `StromMatrix.Baue` nahm als Bedarf nur den Haushaltsstrom
   (`STROMBEDARF`), ohne Wärmepumpe, Heizstab und Hilfsenergie. Beide als Kern-Befunde dem Anwender gemeldet, E25
   verankert sie nicht; behoben mit E26 (#518). **N2** — das Modul der Kopie rechnet mit Ersatzwerten (T_NOCT 0 → 45,
   gamma_PMP 0), Bestand.

## Gebaut

- **E25/1+2** (`51eee6a6`): das Skript `Referenzlaeufe/Skripte/pruefprojekt_1048_pv_preise.cs` (Aufruf
  `dotnet run … -- <db> [--trocken]`; prüft die Vorlage 1040 und die Ziel-ID 1048, kopiert per
  `ProjektDuplizierenCtrl` in eine Arbeitsdatei, setzt die Zielzellen, prüft jede, prüft 1040 unverändert,
  `integrity_check` und `foreign_key_check`, ersetzt erst dann; Rückgabe 0 angelegt oder vorhanden, 2 bei Abweichung;
  IDs zur Laufzeit; Datumsfelder fest 2026-09-25) und der Zwischenstand von `PvPreisProjektTests` (11/11 grün auf der
  Arbeitskopie über den Entwicklungshaken `EPOS_E25_TESTDB`).
- **Merge** `a499feb7` von `origin` `ba798d8a` (E24 und E26 samt Papieren), konfliktfrei.
- **E25/1b** (`65458a10`): das Skript einmal auf der Repo-Fassung `19a7b632…` (Schemastand 144, mit den Zellen von
  E24), LFS-Zeiger committet; Entwicklungshaken entfernt; zwei Tests nach E26 (Erzeugung/Eigenverbrauch/Einspeisung,
  vermiedene Kosten im Rollentarif); Zählungen nachgezogen; LIESMICH-Abschnitt zum Prüfprojekt.
- **E25/3** (`a313d776`): drei Wachen an 1048 angepasst, voller Lauf; (`e0f6d847`): LIESMICH „Aktuelle Basis“ mit SHA
  `b68638da…` und dem Nachtrag Prüfprojekt 1048.
- **E25/4** (auf `pm26` nach dem Merge): Kopfabsatz der LIESMICH ohne Nummer (Befund 6 unter „Abweichungen und
  Befunde“).

## Das Prüfprojekt 1048

| Ort | vorher (Kopie von 1040) | nachher | Quelle |
|---|---|---|---|
| `Tab_Projekt` | Beschreibung, Datumsfelder der Kopie | Beschreibung neutral mit Anlass; Änderungs- und Erstelldatum fest 2026-09-25 | E25‑Q5 |
| Gebäude, `Gebaeude_Modell` | `TAGESBILANZ` | NULL (VDI 6007) | E25‑Q1; der Tagesbilanz-Weg bleibt allein bei 1040 |
| `PV_Leistung` | 20 Module (5,20 kWp) | **40 Module (10,40 kWp)**, das kopierte Modul bleibt | E25‑Q2 |
| Erdgas-Zeile (Träger 63, kopiert) | ohne Preis | 0,80 €/Nm³ (Günstig 0,70 / Ungünstig 0,95), Grundpreis 150 €/a | E25‑Q5 |
| Stromträger 60 (neu) | — | nach dem Muster von 1030 (Umrechnung 51, CO₂/SO₂/NOx 560/200/280, kWh): 0,30 €/kWh (0,26 / 0,36), 120 €/a (100 / 150), Leistungspreis 0 | E25‑Q5 |
| Parametersatz (neu, über `WirtschaftlichkeitCtrl.SpeichereParameter`) | — | Zins 3 %, 20 a, Energie 2 %/a, Betrieb 1,5 %/a, Einspeisevergütung PV 0,08 €/kWh (0,10 / 0,06) | E25‑Q3, Q5 |
| Kostenposition Kategorie 1 (neu) | — | StammID 80, 1.200 €/kWp = 12.480 € (10.400 / 14.560), 25 a, ohne `NutzungsdauerID` | E25‑Q7 |
| Kostenpositionen Kategorie 2 (neu) | — | StammID 149 Wartung 150 €/a (120 / 180); StammID 150 Instandhaltung 1 % der Investition (Pflichtpositionen) | E25‑Q7 |
| 20 kopierte Investitionszeilen | 54.975,50 € | unverändert, einschließlich der Solarthermie-Position 3.775 € ohne Anlage | E25‑Q10 |

**Nicht angelegt:** Vergütungszeile, Tarifstruktur, Ergebnis- und Katalogzeilen; kein `VACUUM`. Das Skript ist
wiederholbar: ein zweiter Lauf meldet „steht schon“ und gibt 0 zurück.

**Zellvergleich** aller Tabellen samt `sqlite_sequence` gegen `19a7b632…`: Schema gleich (145 Tabellen), **0 bestehende
Zeilen geändert oder entfernt**, 44.537 neue Zeilen in 30 Tabellen (35.040 Viertelstundenwerte der Stromganglinie,
8.760 Solarwerte, 365 Klimatage, 165 Kennlinienzeilen, 23 Kostenpositionen), 25 fortgeschriebene Zähler;
`integrity_check` ok, `foreign_key_check` leer; 70.680.576 Byte, LFS-SHA-256 `b68638da…`. Keine Einfrierregel ist
berührt — 1048 ist kein Referenzprojekt, die Vorlage 1040 bleibt Zelle für Zelle.

## Wirkung von E26 an 1048

| Größe | vor E26 (Arbeitskopie) | nach E26 |
|---|---|---|
| `Photovoltaik.Stromproduktion` | 6,37 MWh | 13,43 MWh (= Summe der Module) |
| Strommatrix-Bedarf | 8,00 MWh | 31,50 MWh |
| PV-Eigenverbrauch | 3,07 MWh | 6,368 MWh |
| Einspeisung | 7,06 MWh | 7,06 MWh |
| Rollentarif 0,30 €/kWh: vermiedene Menge | −17,13 MWh | +6,368 MWh |
| Rollentarif: vermiedene Kosten | −5.140 €/a | +1.910,38 €/a |

Die Kapitalwert-Anker sind unverändert: Erwartet −237.134,7270351314 €, Günstig −204.001,50754334457 €, Ungünstig
−279.852,53447363194 €. „PV: vermiedener Bezug“ steht nur bei aktivem Vergütungsdialog (`WirtschaftlichkeitCtrl.cs:6399`)
und ist bei 1048 mit flacher Vergütung null — so gebaut; mit Dialog auf einer Arbeitskopie 6,368 MWh × 0,30 / 0,26 /
0,36 €/kWh (Test).

## Tests

`EPOS.Kern.Tests/PvPreisProjektTests.cs`, **13 Fälle:** Aufbau; keine Referenzrolle; Preissatz; Kapitalwert-Anker
relativ 1e‑6; Einspeiseerlös je Szenario (7,06 MWh × 0,08 = 564,80 €/a; Günstig × 0,10 × 1,1 = 776,60 €/a; Ungünstig
× 0,06 × 0,9 = 381,24 €/a); Investition je kWp (54.975,50 + 12.480 = 67.455,50 €; 59.877,95 / 75.033,05 €);
Betriebskosten (274,80 / 224,00 / 325,60 €/a); Erzeugung, Eigenverbrauch und Einspeisung nach E26; vermiedene Kosten im
Rollentarif = Eigenverbrauch × Bezugspreis; Szenario C „4 von 18 Parametern szenariert“; Erwartet bitgleich ohne
Szenariopreise; Szenario D mit DV-Entgelt (Marktprämie 0,40 ct/kWh [0,20 / 0,60], Kohärenzzeile
`WIRT_SZ_PV_DIALOG_EINSPEISUNG`) und PPA (7,0 [8,0 / 5,5] ct/kWh) samt vermiedenem Bezug; Formelmappe mit
ClosedXML.

**Zählungen nachgezogen:** `GebaeudeKatalogverweisTests` 27 → 28 und 23 → 24; `ErgebnisansichtTests` 101/95 → 122/115,
Hinweise 27/33/7 → 32/39/8; `PreisbasisSchrittTests` kWh 8 → 9, Nm³ 18 → 19.

**Wachen an 1048 angepasst:** `TestDatenbankEntsorgungWacheTests` — die neue Klasse nimmt `new TestDatenbank()` in
`using` bzw. einer Vorrichtung mit `Dispose` statt der Fabrik `Neue()`; `SzenarioParameterTests` (E9a) nimmt 1048 aus;
`StromsteuerBefreiungModusTests` nimmt 1048 aus (der Parameterdialog schreibt „AUSWEIS“, ausdrücklich geprüft).

## Fragen aus der Welle

Die Fragen stellt der Phase‑0-Bericht; entschieden hat sie der Orchestrator am 25.09.2026 (~15:20) mit der
Baufreigabe, alle a, nach Empfehlung; gebaut ist jeweils der Entscheid (→ Register R‑E25).

| Frage | Entscheid |
|---|---|
| **E25‑Q1** Vorlage | a — Kopie von 1040, Gebäude nach VDI 6007 |
| **E25‑Q2** Anlagengröße | a — 40 Module (10,40 kWp) |
| **E25‑Q3** Vergütung | a — flache Einspeisevergütung im Parametersatz; DV-Entgelt und PPA nur im Test |
| **E25‑Q4** Tarif | a — Flat, keine Tarifstruktur |
| **E25‑Q5** Werte | a — wie im Entwurf |
| **E25‑Q6** Prüfart der Tests | a — Beziehungen und Kapitalwert-Anker relativ 1e‑6 |
| **E25‑Q7** PV-Kostenpositionen | a — direkte Zeilen ohne `NutzungsdauerID` |
| **E25‑Q8** Reihenfolge | a — erst E24 mergen, dann das Skript einmal auf der Repo-Datenbank, ein LFS-Objekt |
| **E25‑Q9** Ablage des Skripts | a — unter `Referenzlaeufe/Skripte/` samt Abschnitt in `Referenzlaeufe/LIESMICH.md` |
| **E25‑Q10** kopierte Zeilen | a — unverändert, einschließlich der Solarthermie-Position 3.775 € ohne Anlage |

## Abweichungen und Befunde

1. **„PV: vermiedener Bezug“ nur bei aktivem Vergütungsdialog** (`WirtschaftlichkeitCtrl.cs:6399`) — an 1048 mit
   flacher Vergütung null, so gebaut. Punkt (2) von A‑E26‑1 ist an 1048 deshalb nur mit aktivem Vergütungsdialog zu
   sehen.
2. **Kapitalwert-Anker nach E26 unverändert** — dieselben Werte wie im Zwischenstand vor E26; E26 bewegt den Ausweis,
   nicht den Kapitalwert.
3. **N1 und N3 an 1048 gemessen** (Tafel „Wirkung von E26 an 1048“).
4. **Andere Zeilen-ID der Stromzeile als auf der Arbeitskopie:** die Datenpflege E24 hat vorher eine Zeile in
   `energy_project_settings` angelegt; das Skript vergibt die IDs zur Laufzeit.
5. **Testhost-Regel einmal verletzt:** ein gefilterter Lauf von 68 Fällen (7 s) bei zwei fremden testhosts, grün.
6. **Gate #519 auf `3936003c` zunächst rot, 1 von 7.451 Kern-Tests:** `PvPreisProjektTests.Das_Projekt_hat_keine_Referenzrolle`
   prüft die ersten 800 Zeichen des Abschnitts „## Aktuelle Basis“ in `Referenzlaeufe/LIESMICH.md` auf die Nummer
   1048 — der Kopfabsatz der aktuellen Basis nennt kein Prüfprojekt, der Nachtrag darunter darf es. Der letzte
   E25-Commit `e0f6d847` (LIESMICH-Nachtrag, nach dem vollen Lauf des Agenten) hatte 1048 im Kopfabsatz genannt.
   Behoben auf `pm26` als E25/4 (Kopfabsatz der LIESMICH ohne Nummer; Wortlaut jetzt „das Prüfprojekt „PV mit
   Preisen“ kam ohne Referenzrolle hinzu“), die Klasse danach grün.

## Nachweis

- **Datenbank:** Zellvergleich im Abschnitt „Das Prüfprojekt 1048“.
- **Referenzlauf** aller vierzehn Projekte gegen R18: 14/14 PASS, 4.610.207 Werte, 432/432 CSV byte-gleich.
- **Tests** (Worktree `e25`, voller Lauf mit den xUnit-Schaltern): Kern 7.450 und 1 übersprungen, UI 6.395, KiKern 549,
  SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen, 0 Fehler; Auslieferungsvorlage-Tests 36/36;
  SqlDialektPruefer 1.927/0; `Testdatenbankschema --trocken` 0/0 (Schritt 144).
- **Merge** `3936003c` auf `pm26` über `origin` = `ba798d8a`.
- **Gate:** NACHTRAG-519-GATE
- **CI:** NACHTRAG-519-CI

## Abnahme am Gerät (A‑E25‑1, Windows, an der Testdatenbank)

1. 1048 öffnen und rechnen.
2. Kapitalwerte: Erwartet −237.135 €, Günstig −204.002 €, Ungünstig −279.853 €.
3. Erlösrubrik, Einspeiseerlös: 564,80 / 776,60 / 381,24 €/a.
4. PV-Stromerzeugung 13,43 MWh; Hinweis „4 von 18 Parametern szenariert“.
5. Formelmappe: Parameterblock mit 0,08 / 0,10 / 0,06 €/kWh und den Trägerpreisen.
6. Im Rollentarif vermiedene Kosten rund 1.910 €/a.

Belegt in `PvPreisProjektTests`; die Sichtprüfung liegt beim Anwender.

## Logbuch

Kein Eintrag. E25 legt ein Projekt in der Testdatenbank an, das nur Tests, Gate, CI und Referenzlauf lesen; die
Anwendung, ihre Masken und ihre Rechenregeln sind unverändert, die Auslieferungsvorlage entfernt alle Projekte, und
die Datenbank des Anwenders ist nicht berührt.

## Papiere mit der Statuszeile

Mit dem Merge (E25/1b, E25/3): `Referenzlaeufe/LIESMICH.md` — Abschnitt „Das Prüfprojekt 1048 „PV mit Preisen“ (ohne
Referenzrolle)“, das Skript unter „Was hier liegt“, „Aktuelle Basis“ mit SHA `b68638da…` und dem Nachtrag
Prüfprojekt 1048 (R18 bleibt). Mit dieser Statuszeile: Konzept (Kopf mit Codestand `3936003c` und „E25 ohne Schritt“;
Schrittabsatz; § 6.3 neue Nr. 35 erledigt; § 7), Register (Kopf, Familientafel, neue Familie R‑E25, R‑E21 Q9 gebaut),
Analysepapier (Kopf, § 5 Zeile E25), Protokoll der Entscheidwege (Kopf von § 8, § 8.52, § 8.53),
Dokumentations-Index (Reporting 140 → 141), Statusdatei (#519, Nach #519; in #514, #515 und #518 der CI-Vermerk des
Pushs `ba798d8a`). Kein Mockup (kein Dialog), kein Wiki.

## Offen

- Die **Abnahme am Gerät** A‑E25‑1 (sechs Schritte oben); A‑E26‑1 ist an 1048 prüfbar.
- An 1048 sind **keine Vergütungszeile und keine Tarifstruktur** angelegt (E25‑Q3, E25‑Q4); DV-Entgelt, PPA und der
  vermiedene Bezug laufen nur im Test auf einer Arbeitskopie.
- **Nach jeder Neufassung der Testdatenbank ohne 1048** wird das Skript auf der neuen Fassung erneut gezogen.
- **Gate** und **CI** (Nachweis oben).
