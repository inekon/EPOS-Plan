# Übergabe: Sitzung „Dialoge und Korrekturen“ — Stand 27.09.2026, Wechsel in die Cloud-Umgebung

Die Sitzung „Dialoge und Korrekturen“ (Dialogdesign, Fehlerbilder aus Bildschirmfotos und PDF,
Solarthermie, Kessel, Projektpaket-Migration) wechselt vom Windows-Rechner in eine
Cloud-Umgebung (Claude Code im Web, gestartet aus der Desktop-App). Dort gibt es nur, was im
Repositorium liegt. Dieses Papier nennt den Stand, die Regeln der Sitzung, die Nachbarn und
das, was in der Cloud anders läuft.

## 1 Stand

Alles ist gepusht: `origin/ios_migration_september` = `39569593f`, Kern-Lauf 36303564776 grün.
Referenzbasis **R23 `Referenzlaeufe/2026-09-26_R23_KesselBereitschaft`**, Testdatenbank Schemastand
151 (LFS `22b1f882…`, Schemaschritt 151 gehört der Gebäudesimulation), nächster Schemaschritt
**152**, nächste freie Statusnummer **#588** — vor jeder Vergabe auf `origin` messen
(`git show origin/ios_migration_september:Dokumentation/aktuell/Status_iOS_Migration.md | grep -oE '^\| \*\*#5[89][0-9]'`),
weil die Berichtsvorlagen-Cloud-Sitzung Nummern ohne Abstimmung vergibt (#566, #581).

Zeilen dieser Sitzung in [`../Status_iOS_Migration.md`](../Status_iOS_Migration.md): #542–#557,
#559, #560, #562–#565, #567–#569, #571–#577, #580, #582, #584–#587. Protokolle:
[`DA1`](../../ueberholt/Protokolle/Views/DA1_Dialogdarstellung_Protokoll.md),
[`DA2`](../../ueberholt/Protokolle/Views/DA2_Dialoge_Korrekturen_Welle_Protokoll.md),
[`SA1`](../../ueberholt/Protokolle/Simulation/SA1_Waermeautarkie_Solarthermie_Protokoll.md),
[`SK1`](../../ueberholt/Protokolle/Simulation/SK1_Kessel_Bereitschaft_kW_Protokoll.md),
[`SK2`](../../ueberholt/Protokolle/Simulation/SK2_Kessel_Bereitschaft_Stunden_R23_Protokoll.md),
[`PT1`](../../ueberholt/Protokolle/Views/PT1_Projektpaket_Anhebung_Protokoll.md),
[`PT2`](../../ueberholt/Protokolle/Views/PT2_Projektpaket_Anhebung_Stufe2_Protokoll.md).
Logbuch-Sätze für die Version **1.2.0.5** und die Kandidaten des nächsten Sammel-Uploads
stehen in [`../Wiki_Update_2026-09-26.md`](../Wiki_Update_2026-09-26.md).

## 2 Was aussteht

### 2.1 Entscheide des Anwenders (nichts davon ist begonnen)

| Thema | Frage | Empfehlung |
|---|---|---|
| Kessel (#568, SK2) | (a) Bereitschaftsverlust des Elektrokessels nur im Nutzungsgrad, nicht im Netzbezug; (b) „Maximale Brennstoffleistung Gas“ bei mehreren Kesseln = Summe der Einzelmaxima; (d) Heiztag = Raumwärme > 0 — VDI-6007-Gebäude sind fast ganzjährig „betriebsbereit“ | (a) belassen, (b) belassen mit Kurztext, (d) Schwelle je Gebäudemodell prüfen |
| Kessel-Kennlinie (#569) | Fragen F1–F5 des [Konzepts](../Konzept_Kessel_Kennlinie_EPOS-Plan.md) | E1 + E2 |
| Berichte & Kosten (#574) | BN-Q1–Q3 des [Konzepts](../Konzept_BerichteKosten_Navigation_EPOS-Plan.md), Mockups A/B/C | Variante A |
| Zapfprofil-Dialog (#572) | Struktur statt Überlagerung in Überlagerung — Nachtrag N35 im Zapfprofil-Umsetzungskonzept | Blattwechsel „‹ Warmwasser“ |
| Wirtschaftlichkeit (#582) | Soll der Bericht dem gewählten Szenario folgen? | ja, als eigener Auftrag |
| Repowurzel | `Berichtsvorlagen/Mitgeliefert/` kam mit einem Sync-Commit herein (Laufzeiterzeugnis?) | entfernen und in `.gitignore` |
| Abnahmen | Sichtprüfung der Paketanhebung mit einem echten Altpaket (Stufe 1 + 2); Sichtabnahme der Dialogwellen im Windows-Build; zweite Datenbank für die Bilder „Zapfprofilgenerator nicht verfügbar“ | — |
| Wiki | Sammel-Upload (Gebäude, Wärmebedarf, Prozesswärme, Strombedarf, Gerätekataloge, Photovoltaik, Simulationsergebnisse, Zapfprofil, Berichtsvorlagen, Wirtschaftlichkeit, Projekttransfer) mit Logbuch 1.2.0.5 — höchstens einmal je Woche, 1.2.0.4 lief am 26.09. | Anwender entscheidet, ob früher |
| iOS | Projekt 1049 in den iOS-Prüfmodus (braucht einen iOS-Lauf) | nur nach Freigabe |

### 2.2 Ohne Entscheid möglich (kleine Welle)

- Detailzeilen der Paketanhebung Stufe 2 (Register 62–92) sind deutsche Literale → Ressourcen
  de/en wie in Stufe 1 (`Paketanhebung.Text`, Schlüssel `TRANSFER_ANHEBUNG_S<Nr>…`).
- Zwei ungenutzte Schlüssel `CHART_CSV_HEIZKESSEL`, `CHART_CSV_RESTWAERME` aus den `.resx` entfernen.
- Ferien-Flag: Oberfläche und Kern nutzen jetzt dieselbe Regel; ein gespeicherter Wert 0 wird
  weiter als „0“ angezeigt (Platzhalter „keine“ nur bei leerem Feld) — bewusst so belassen.

## 3 Regeln dieser Sitzung (Anwenderentscheide, gelten weiter)

- **Modellwahl:** Die Hauptsitzung orchestriert; Agenten bekommen ausdrücklich `sonnet`
  (Standard: klar umrissene Umsetzung, Tests, Papiere, Wiki), `opus` nur mit Begründung
  (Konzeptpapiere, Fehlersuche im Rechenweg, Neueinfrierung, widersprüchliche Stände), `haiku`
  für Zählungen und mechanische Pflege. Kein Agent mit `fable`.
- **Token-sparsam:** Gate selbst per Shell (Build, Tests, ChartProben, Referenzlauf, SQL-Prüfer),
  Papiere mit Sonnet aus der Vorgängerzeile als Muster, mehrere fertige Zweige in einer Welle
  (ein Gate, eine Papierrunde, ein Push), Aufträge kurz, Berichte ≤ 25 Zeilen, höchstens 3–4
  Opus-Agenten parallel. Anwenderrahmen 27.09.: Wochenlimit bis 85 % nutzen, darüber nur auf Zuruf.
- **Push ohne Rückfrage** nach grünem Gate auf `ios_migration_september` (nie `main`, kein Force,
  keine Tags). **Kein iOS-, macOS- oder Setup-Lauf ohne Rückfrage — jedes Mal.**
- **Reihenfolge einer Welle:** Merge → Gate → Statuszeile und Protokoll → Push → CI-Kennung
  nachtragen (`https://api.github.com/repos/inekon/EPOS-Plan/actions/runs?head_sha=<voll>`,
  ohne `gh`) → Nachbarn den SHA melden.
- **Gate bei Schemaschritten** zusätzlich: `dotnet test Werkzeuge/Auslieferungsvorlage/Auslieferungsvorlage.sln -c Release`
  (zählt STRICT-Tabellen; nicht im Kern-Filter — Kern-Lauf 36302076165 war deshalb rot).
- **Konflikte in `.resx`:** Vereinigung beider Seiten; die gemeinsame Schlusszeile `</data>`
  eines Hunks geht bei der Vereinigung verloren — XML danach auf Wohlgeformtheit prüfen und den
  Designer im Prüfmodus ziehen. Statusdatei: origin-Fassung nehmen, eigene Zeilen gezielt einfügen.
- **Logbuch:** alle Sätze dieser Sitzung unter Version 1.2.0.5; ein Satz je sichtbarer Änderung.

## 4 Nachbarsitzungen

Auf dem Windows-Rechner laufen „Gebäudesimulation“ (KP1 Konditionierungsprofile, Schema ab 152,
Nummern ab #588 nach Messung), „Zapfprofil“ (#579, Hoteltyp) und die Cloud-Sitzung
„EPOS-Plan Berichterstellung“ (BV-E9, #581). Aus der Cloud erreicht man sie nicht per
Sitzungsnachricht; Abstimmung läuft dann allein über `origin` (Statusdatei, Testdatenbank-oid,
Schemaschritte) — vor jedem Push `git fetch` und mergen, bei Testdatenbank-Konflikten die
origin-Fassung nehmen und eigene Skripte darauf neu anwenden.

## 5 Was in der Cloud anders ist

- Die Umgebung klont frisch; **LFS** muss nachgeholt werden (`git lfs install && git lfs pull
  --include Referenzlaeufe/Kenndaten_Test.sqlite`), sonst liegt eine Zeigerdatei von 130 Byte.
- **SDK** nach `global.json` (10.0.400): das Einrichtungsskript der Umgebung installiert es
  (`dotnet-install.sh --version 10.0.400`), dazu `git-lfs` und `python3`; Python-Werkzeuge laufen
  mit `python3` statt `py`.
- **Gate auf Linux:** Kern-Filter, Tests, ChartProben, `EPOS.Referenzlauf` gegen R23 und der
  SQL-Prüfer laufen wie in `kern.yml`; die Windows-Schale baut mit
  `-p:EnableWindowsTargeting=true` (0 Fehler ist der Nachweis), ein Anwender-Build entsteht nicht.
- **Nicht im Repositorium** und daher in der Cloud nicht vorhanden: die Gedächtnisdateien des
  lokalen Kontos (ihr Inhalt steht in Abschnitt 3), die Materialordner auf `Z:` und
  `C:\Waermeplan` (Kenndaten.accdb, Testprojekte, Normen-PDF), die Bildschirmfotos und das
  Befund-PDF der Sitzung, die Scratchpad-Skripte (`einfuegen.py`, `union.py`,
  `gate_welle.sh` — ihre Wirkung ist in Abschnitt 3 beschrieben).
- Netzwerkregel der Umgebung muss `nuget.org`, `api.github.com` und die LFS-Ablage von
  GitHub zulassen.
