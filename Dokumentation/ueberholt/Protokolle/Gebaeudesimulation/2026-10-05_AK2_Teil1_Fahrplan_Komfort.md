# Protokoll AK2 Teil 1 — Anlagenfahrplan als Verfügbarkeit, Komfortkennzahlen (05.10.2026)

**Sitzung:** Gebäudesimulation (Cloud), Wellen AK2-1, AK2-2a, AK2-2b (E22, E67, E81, E82), Commits AK2-1 `e852670`, `7dbe3d1`, `d754c27`, `a846719`, `d30b432`, `059d102`, `4e1213e`, Merge `0c66e02`; AK2-2a `566e123`, `bd318b0`, `9199c3e`, `0139922`; AK2-2b `574986b`, `ea39e24`, `ed08f04`, `4c73015`; Merge origin `2317087`.
**Entscheid:** E81 und E82 (Anwender, 05.10.2026; die Entscheide Q30 und Q31 heißen nach der Kollision mit dem Zonenbaum der IFC-Sitzung E81 und E82, E79 gehört dieser Sitzung). Offen: Q32 (Nennleistung als Schranke). Schemaschritt 186; Basis R36 unverändert.

## 1 Auftrag

Stufe AK2 der Anlagenkopplung (Arbeitsplan `Dokumentation/aktuell/Gebaeudesimulation/2026-10-05_Plan_AK2.md`): der Erzeugerfahrplan als Verfügbarkeit je Stunde, die Verteilung auf Gebäude und Zonen im Zweipass, die vierte Grenze im Schritt H und die Komfortkennzahlen. Dieses Protokoll deckt die ersten drei Wellen; Oberfläche, Bericht und Wiki (AK2-3) und das Referenzprojekt 1056 mit Basis R37 (AK2-4) folgen.

## 2 Vorgehen

Drei Wellen in strenger Folge, je ein Worktree, Commits sofort, kein Push. AK2-1 legte Schema und Leser, AK2-2a den Rechenweg, AK2-2b die Kennzahlen. Gate 729 auf dem Merge der ersten Welle im eigenen Worktree; für 2a und 2b liefen die betroffenen Tests gefiltert, das Gate der beiden folgt mit AK2-3 als Gate 731.

## 3 Ergebnis

**AK2-1 (Schema und Leser).**
- Schritt 186 `AnlagenfahrplanSchema`: `Tab_Energieanlagen.Zeitprogramm` TEXT und `Vorlauf_Max` REAL; an `Tab_ErgebnisEnergiebedarf` `Komfort_Unterschreitungsstunden`, `Komfort_Kelvinstunden`, `Komfort_Laengste_Strecke`, `Fahrplan_Begrenzt_Stunden`, `Komfort_Ueberschreitungsstunden`, `Komfort_Kelvinstunden_Kuehlung`. Alle nullbar, keine Saat.
- `AnlagenSql.Einfuegen` je Datenbankstand (74 Platzhalter); Duplizieren, Projektpaket und Komponentenübernahme erhalten NULL.
- Leser `Anlagenzeitprogramm`: 168 Faktoren, durch `;` getrennt, Wochenbeginn aus dem Kalender, benannte Fehler.
- Testdatenbank auf 186 (86 917 120 Byte, OID 3878da28…).

**AK2-2a (Rechenweg).**
- `Anlagenverfuegbarkeit`, `Verfuegbarkeitsgrund`, `Anlagenfahrplan` im Profilweg: Nennleistung der Wärmepumpe aus `Tab_WP.Nennleistung`, von Kessel und BHKW aus `Ptherm`; Sperrzeit aus dem Sperrprofil, Zeitprogramm, Abschaltpunkt; Speichervorrat `Q_max` über die Sperrdauer; Vorlaufangebot aus `Vorlauf_Max` oder `Vorlauf`.
- `Verfuegbarkeitsverteilung`: proportional, Randfall, Rundungsrest an den größten Anteil, Summe in Id-Reihenfolge.
- Zweipass `SimulationWaermebedarf.FahrplanVorbereiten` und `Verteilen` mit zweiter Stufe auf die Zonen; `Stundenrand.MitVerfuegbarkeit`; Schritt H trägt `Verfuegbarkeit` und `VorlaufAnlage` als vierte Grenze (Gründe 8 → 10).
- Zähler `Fahrplan_Begrenzt_Stunden` (NULL ohne Kappung); Modultrennungswache Satz 5.

**AK2-2b (Komfortkennzahlen).**
- `Komfortkennzahlen`: Schwelle 1,0 K als Festwert `KOMFORT_SCHWELLE_K` (E81), Nutzungszeit; Zone → Gebäude: Stunde zählt bei mindestens einer Zone, Kelvinstunden flächengewichtet; Gebäude → Projekt: Stunden bei mindestens einem Gebäude, Kelvinstunden Summe, längste Strecke Maximum. Kälteseite spiegelbildlich.
- `Bedarfsbegriff` je Gebäude (Rückwirkung oder feste Last), `KomfortUndRestbedarf`; Hinweise `SIMENG_AK2_PROFILWEG_NAEHERUNG` und `SIMENG_AK2_FESTE_LAST`; Bedarfsauskunft `GebaeudeBedarfCtrl` mit Fahrplan.
- Projektspalten nur bei greifender Schranke, sonst NULL (byte-gleich). `Umschaltung` bleibt als Verfügbarkeitsgrund benannt offen (Konzept 7.4).

## 4 Festlegungen und Befund 1054 / Q32

- **Befund:** Die Nennleistung als Schranke (`LEISTUNGSGRENZE`) kappt das Referenzprojekt 1054 in 10 Stunden (Kessel 80 kW und BHKW 30,8 kW liegen unter dem unbegrenzten Bedarf). Deshalb kappen vorerst nur Ausfälle: Sperrzeit, Zeitprogramm, Abschaltpunkt, Speicher leer. Der Schalter `leistungsgrenzeAlsSchranke` steht auf `false`.
- **Q32 (offen, beim Anwender):** Soll die Nennleistung als Schranke gelten? Vorschlag: erst mit AK3.
- **Befund Bestand:** Kein Referenzprojekt hat `Sperrung = 1`; 15 Projekte tragen eine inaktive Sperrzeit 14–17 Uhr; gekoppelt rechnen nur 1047 und 1054. Darum ändert AK2 keine Referenzzahl.
- Entscheide Q30/Q31 heißen E81/E82 (Kollision mit E79 der IFC-Sitzung beim Merge `2317087`).

## 5 Nachweise

- **Gate 729** (`0c66e02`, AK2-1, eigener Worktree): Kern-Filter 0 Fehler, ChartProben grün, `EPOS.Kern.Tests` 10 980 grün (3 übersprungen), `EPOS.UI.Tests` 7 521, Dokumentationswachen 35, Referenzlauf 20/20 PASS gegen R36 und 608/608 byte-gleich, Plattformnachweis PASS, Schale 0 Fehler, Designer unverändert, SQL-Dialekt 2 346 Texte, 0 Fundstellen, Werkzeugtests grün.
- **AK2-1:** 1 610 Tests grün; die acht CI-Projekte byte-gleich.
- **AK2-2a:** 2 130 Tests grün; 20/20 byte-gleich gegen R36.
- **AK2-2b:** 157 + 8 + 3 Tests grün; 20/20 byte-gleich.
- Gate der Wellen 2a und 2b: folgt mit AK2-3 als Gate 731.

## 6 Offenes

- Q32 beim Anwender; Upload des Datenbankobjekts 186.
- Weitere Wellen: AK2-3 (läuft), AK2-4 Referenzprojekt 1056 und Basis R37.
- `Umschaltung` als Verfügbarkeitsgrund; Komfortspalten auch ohne greifende Schranke (Frage für AK2-4); `GebaeudeModellErgebnis.Skaliert` ohne Zonen (vorhandenes Verhalten).
- CI-Kennung nach dem Push.
