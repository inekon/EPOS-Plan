# Übergabe Kälteanlagen, Fortsetzung — Sitzung „Kälteanlagen“ an die nächste Sitzung, Stand 12.10.2026

Diese Übergabe setzt [Übergabe Teil Programm](2026-10-10_Uebergabe_Kaelteanlagen_Programm.md) und
[Übergabe Kälteanlagen](2026-10-10_Uebergabe_Kaelteanlagen.md) fort. Sie nennt, was die Sitzung seit dem 10.10.2026 gebaut
hat, welche Entscheide gelten, was beim Anwender offen ist und womit die nächste Sitzung anfängt. Arbeitszweig ist
`ios_migration_september`; der Sitzungszweig der abgebenden Sitzung war `claude/relaxed-hamilton-wp2szg`.

## 1. Gebaut und gepusht

Alle Wellen sind auf `origin/ios_migration_september` und haben einen grünen Kern-Lauf der CI. Läufe auf dem Arbeitszweig
werden fast immer von neueren Pushes anderer Sitzungen abgebrochen; maßgeblich ist deshalb der vollständige Lauf auf dem
Sitzungszweig mit demselben Commit.

| Welle | Status | Inhalt | Schema | CI (Sitzungszweig) |
|---|---|---|---|---|
| K‑F1 | #945 | Rückkühlwerk als eigenes Glied (Katalog `RKW`, Projektkopie, Anlagenzeile, Weg `FEST` byte-gleich) | 214 | 38112610415 |
| K‑F2 | #959 | Rechenweg des Rückkühlwerks: lastabhängige Annäherung, Bezugstemperatur je Bauart, Ventilator, Wasserbilanz, Ergebnisse, Kennzahlen | — | 38134650660 |
| KM4‑E1 | #958 | Kältemitteltabelle `Tab_Kaeltemittel_STAMM` (Register `KMT`), `ID_Kaeltemittel`, `Tab_Projekt.Inbetriebnahmejahr`, Zuordnung des Textbestands | 216 | 38134650660 |
| Kühlkonzept 5.6 | #971 | gleichzeitige Kälte- und Wärmenutzung (KWN), Fälle a–e, Kältespeicher als Quelle der Wärmepumpe, Stufenplan KWN‑0 bis KWN‑7 | — | 38134650660 |
| KM4‑E2 | #970 | Regelsaat `FGAS` (Gesetzkatalog Generation 10) nach Anhang IV der VO (EU) 2024/573, Zulässigkeitsprüfung (Warnung, nie Sperre), Warnkriterium, Kältebahn, Berichtsspalte | — | 38145588105 |
| KM4‑E2‑c | #975 | 18 Kältemittel am Volltext gesichert, F‑Gas nach Art. 2 Buchst. a, R1233zd(E) als HFCKW | — | 38151212797 |
| KM4‑E3 | #993 | Oberfläche: Kältemittel-Klappliste mit Lesezeilen und Schnellwahl im Katalogdialog der Kältemaschine, Inbetriebnahmejahr, Lesezeilen der Wärmepumpe, Zulässigkeitszeile, Kachelhinweis, Verwaltungsdialog „Kältemittel“ | — | 38170437900 |
| KM4‑E4 | #994 | Wiki-Quellen, Logbuch-Entwurf, Kühlkonzept §14/§6.3, KM4-Papiere „wie gebaut“ nach `ueberholt/` | — | 38170437900 |
| K‑F3 | #1005 | Teil-Freikühlung `REIHE` (Vorkühlung mit dem Massenstrom der Stunde), Ventilatorstrom der freien Kühlung, Ergebnisse, Kennzahlen; Berichtskatalogfassung 22 | — | 38184507448 |
| KWN‑0 | #1006 | Prozesskälte als Unterposten des Kühlkanals (`Kaelte_Art` Raum/Prozess), Feld „Art“ in Kältebedarf extern und Kältebedarfsprofile | 218 | 38184507448 |
| Fix | #1007 | Kältebedarf extern: Speichern über den Dialog behält die Deckungsfelder (Split, EER, Kühlträger, Zähler) | — | 38184507448 |

Die Protokolle liegen unter `Dokumentation/ueberholt/Protokolle/Kaelteanlagen/`; die KM4-Papiere „wie gebaut“ unter
[`ueberholt/Konzept_Kaeltemittel_Stammdatum_EPOS-Plan.md`](../../ueberholt/Konzept_Kaeltemittel_Stammdatum_EPOS-Plan.md) und
[`ueberholt/Umsetzungskonzept_Kaeltemittel_Stammdatum_EPOS-Plan.md`](../../ueberholt/Umsetzungskonzept_Kaeltemittel_Stammdatum_EPOS-Plan.md).
Basis ist R52 (`Referenzlaeufe/2026-10-11_R52_Vorheizen`, 30 Projekte); kein Referenzprojekt hat ein Rückkühlwerk, eine
Prozesskältezeile oder ein Inbetriebnahmejahr.

## 2. Entscheide des Anwenders

| Kennung | Inhalt |
|---|---|
| E123 | KM4‑Q1 bis Q17 nach Empfehlung (Kältemittel als Stammdatum, Prüfung warnt nur) |
| E126 | Anhang IV Nr. 7 d: Lesart GWP ≥ 750 ab 1.1.2027 für Kühler über 12 kW (der deutsche Wortlaut nennt „GWP von 750“) |
| E127 | KWN‑Q1 bis Q13 nach Empfehlung; KWN‑Q11: eine Wärmepumpe mit Speicherquelle hat keine zweite Quelle als Rückfall |
| ohne Nummer, 11.10.2026 | KM4‑E3‑a ohne Abstimmung mit der Sitzung „Dialoge und Korrekturen“ im Aufbau von KM5; der Verwaltungsdialog „Kältemittel“ bleibt im Aufbau, die gewählte Zeile wird hervorgehoben; die acht Gemische sind `GESICHERT` (Massenanteile nach ISO 817); Datenverlust in „Kältebedarf extern“ prüfen und beheben (erledigt #1007) |

## 3. Offen beim Anwender

- **Logbuch-Version** für KM3 (Vorschlag 1.2.1) und KM4 (Entwurf mit Platzhalter `‹Version›` in
  `Dokumentation/aktuell/Wiki_Update_2026-09-26.md`, Abschnitt 2).
- **Wiki-Upload** der vier Seiten (Gerätekataloge, Kälteerzeugung, Wärmepumpe, Kurzanleitung) samt Logbuch: Statusdatei,
  Abschnitt „Offen“, Punkt „Nach #994“. Hochgeladen wird gesammelt, höchstens einmal je Woche.
- **Windows-Sichtabnahme** KM3 und KM4‑E3: neun Punkte unter „Nach #993“.
- **Vor der nächsten Auslieferung** die produktive Datenbank abgleichen, damit das Katalogpaket den gesicherten
  Kältemittelstand trägt (#975).
- **VDI-3805-Dateien** und Luftfeuchte der Testklimadaten: unverändert offen (Übergabe Teil Programm, Abschnitt 3).

## 4. Nächste Arbeit (Empfehlung)

1. **KWN‑1** Kopplungsvorstufe an der **Kältemaschine** (Fälle a, b, c; Kühlkonzept 5.6.6 und 5.6.13): Schema `KWN-S1`
   (`Kopplung_*` an `Tab_Energieanlagen`, Kälteziele in `Z_AnlageSenke`), Kopplungsbuchung und Kopplungsprobe, Strom einmal
   gebucht und nach Nutzenergie aufgeteilt, Temperaturpaar der Prozesskälte wirksam für gekoppelte Geräte. Schemanummer vor
   dem Bau anmelden (zuletzt frei: 221; 220 hält die Sitzung Gebäudesimulation).
2. **K‑F5** Oberfläche des Rückkühlwerks (Katalog, Gruppe „Rückkühlung“ in der Anlage), Berichtszeile, Wasserkosten als
   Kostenkomponente, Wiki. Parallel zu KWN‑1 möglich.
3. **K‑F4** Referenzprojekt mit Rückkühlwerk und Feuchtesaat (1055 erzeugt bei 6 °C Kaltwasser keine Mischstunde), neue
   Basis; Nummer mit der Sitzung Gebäudesimulation abstimmen, die R53 vorbereitet.
4. Danach KWN‑2 (Wärmepumpe direkt), KWN‑2S (Kältespeicher als Quelle; die Quellenwahl `QuellePufferspeicherHuelle` listet
   heute auch Kältespeicher), KWN‑3 bis KWN‑7; K‑B, K‑D, K‑E, K‑H, KB‑C nach der Übergabe Teil Programm.

## 5. Schnittstellen

- **Sitzung Gebäudesimulation** (`session_01X2CX69qnhXXtc6epxnPUv6`): Schemaschritte 217, 219, 220, Basis R52/R53,
  `Bivalenzvorgaben` (Klappliste der Wärmepumpe, nicht ändern), Kälteschranke AK3.
- **Sitzung „Dialoge und Korrekturen“:** Kältedialoge (KM5, Katalogauswahl). Nicht erreichbar gewesen; Änderungen an deren
  Rahmen nur nach Freigabe des Anwenders.
- **Befund für eine andere Sitzung:** `PlatzhalterkatalogDialog` setzt kein `aria-selected` an der gewählten Zeile.

## 6. Arbeitsweise (Erfahrungen dieser Sitzung)

- **Statusnummern und Schemaschritte unmittelbar vor dem Commit messen** und dann sofort pushen; andere Sitzungen vergeben
  laufend Nummern. Bei abgelehntem Push origin mergen, neu nummerieren, erneut pushen.
- **Berichtskatalogfassung:** jede Welle mit Vorlagenfeldern bekommt eine eigene benannte Konstante (`FASSUNG_*`); bei
  Kollision rückt die eigene Welle auf die nächste freie Nummer, Messlatte und zehn Berichtsvorlagen werden neu gebaut
  (`Werkzeuge/Berichtsvorlage … alle`).
- **Testdatenbank:** Ändert origin sie während einer Welle, die Seite von origin nehmen und den eigenen Schritt mit
  `Werkzeuge/Testdatenbankschema` neu heben; danach Zellvergleich. Setzt die Gesetzkatalog-Nachsaat
  `Tab_Applikation.Kostenkatalog_Geaendert`, wird der Stempel zurückgesetzt (Freigabe des Anwenders vom 11.10.2026).
- **Gate:** `Werkzeuge/Gate/gate_rest_linux.sh` enthält den vollen Kerntest nicht; ihn danach getrennt fahren
  (rund 75 Minuten). Bekannte Zeitprobe unter Last: `KatalogImportDialogTests.Eine_sortierte_Liste_kommt_ebenso_zur_Ruhe`
  (allein grün).
- **Plattenplatz:** fertige Worktrees sofort entfernen (`git worktree remove -f -f`), der Zweig bleibt.
- **.NET-SDK:** wie in der Übergabe Teil Programm, Abschnitt 5 (Debian-12-Pakete per `dpkg -x` nach `/root/.dotnet`).
- **EUR-Lex:** In der Cloud nicht erreichbar. Der Anwender hat den Volltext der VO (EU) 2024/573 als PDF bereitgestellt; im
  Repository stehen nur Fundstellen (Anhang, Nummer, Seite), keine Textabschrift.

## 7. Einstiegs-Prompt für die nächste Sitzung

```
Du übernimmst die Sitzung „Kälteanlagen“ für EPOS-Plan (Repo inekon/EPOS-Plan, Arbeitszweig ios_migration_september).
Lies zuerst CLAUDE.md, dann Dokumentation/aktuell/Kälteanlagen/2026-10-12_Uebergabe_Kaelteanlagen_Fortsetzung.md
vollständig, dazu die Übergabe Teil Programm (2026-10-10_Uebergabe_Kaelteanlagen_Programm.md, Abschnitte 3 und 5), im
Kühlkonzept Dokumentation/aktuell/Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md den Abschnitt 5.6 (nur gezielt, das
Papier ist groß), den Entwurf Dokumentation/aktuell/Kälteanlagen/2026-10-10_Entwurf_Split_VRF_Rueckkuehlwerk.md §5 und §10,
die Zeilen KAE, K-F, KM4 und KWN sowie die Entscheide E117–E127 in Dokumentation/aktuell/Status_Gebaeudesimulation_VDI6007.md,
den Kopf und die letzten zwanzig Zeilen der Tabelle in Dokumentation/aktuell/Status_iOS_Migration.md und
`git log --format='%h %<(72,trunc)%s' -n 25 origin/ios_migration_september`.

Rolle: Du orchestrierst — zerlegen, abnehmen, zusammenführen, Rechenwegentscheide, Antworten an mich. Umsetzung über die
Agentendefinitionen unter .claude/agents/ (opus-umsetzung, sonnet-mechanik, haiku-pruefung); Modell bei jedem Aufruf
ausdrücklich setzen. Je Welle ein Worktree; Agenten committen, pushen nicht. Wellenfolge: Merge auf aktuellem origin → Gate
(gate_rest_linux.sh plus voller Kerntest) → Statuszeile und Protokoll → Push auf ios_migration_september und den
Sitzungszweig → CI-Vermerk aus dem Kern-Lauf des Sitzungszweigs. Merge und Push nach grünem Gate ohne Rückfrage. Nie ohne
meine Freigabe: macOS-, iOS- und Setup-Läufe, Wiki-Upload, Pull Requests, Force-Push auf ios_migration_september, Tags.
Schemanummer vor dem Bau in der Kopfzeile der Statusdatei anmelden und allein diese Zeile sofort pushen. Keine Hersteller-
und Modellnamen in Auslieferung und Wiki. Antworten auf Deutsch, knapp, Zahlen in Tabellen. Abschnitt 6 der Übergabe gilt.

Erste Schritte: (1) Stand prüfen — git status sauber, keine Konfliktmarker, keine liegen gebliebene AGENT_LAEUFT, .NET-SDK
vorhanden, Kopf der Statusdatei (Schemaschritte bis 220, nächster freier 221; Basis R52). (2) Den Stand in höchstens fünf
Zeilen melden. (3) Die offenen Punkte beim Anwender aus Abschnitt 3 nennen. (4) KWN‑1 (Kopplungsvorstufe an der
Kältemaschine) mit Aufwand und Wellenschnitt vorschlagen, K‑F5 als Parallelwelle; ohne meinen Auftrag keine Welle beginnen.
```
