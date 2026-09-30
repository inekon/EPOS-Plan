# SK6 — Kessel-Kennlinie, Etappen E2b und E3: Brennwertkennzeichen (Schritt 158), Brennwertkennlinie, Basis R28

Stand: 30.09.2026 · Zweig `ios_migration_september` · Opus-Agent im Worktree, Zweig `kessel-e3` ab `c0321b1a`
(lokales `ios_migration_september` mit E2 und KP2 K3, Schemastand 157). **Schemaschritt 158.** Konzept
[`Konzept_Kessel_Kennlinie_EPOS-Plan.md`](../../../aktuell/Konzept_Kessel_Kennlinie_EPOS-Plan.md) (4.1, 5, 6, 7, 7.2);
Vorgänger [`SK5_Kessel_Kennlinie_E2_R27_Protokoll.md`](SK5_Kessel_Kennlinie_E2_R27_Protokoll.md).

Commits:
- `aaf9c9a9` Kern, Schemaschritt 158, Oberfläche, Ressourcen, Tests
- `53c4487c` Testdatenbank auf Schemastand 158, Kommentar des 1050-Skripts
- `5d220896` trockenes η₃₀ auf zwölf Stellen gerundet
- `63c5a774` Basis R28 eingefroren, R27 archiviert, Basisname nachgezogen
- Papiere (Konzept, Wiki-Quelle, dieses Protokoll, Index) im Commit danach

## 1 Auftrag und Entscheide

Auftrag „Kessel-Kennlinie E2b und E3, gemeinsame Basis R28“. Es gelten:

- **B-1 (30.09.2026): „Auch Projekte nachziehen“.** Die Normvorgabe von η₃₀ richtet sich nach der Bauart, gelesen am
  Schalter `Brennwert` der Projektkopie. Ein Schemaschritt setzt ihn in jeder Projektkopie, deren Katalogsatz ein
  Brennwertkessel ist — auch in Anwenderdatenbanken beim Update, nur setzen, nie löschen; ohne Verweis gilt die Bauart
  in der Beschreibung wie beim Import; was ohne Zuordnung bleibt, steht im Bericht.
- **F5:** Rückfall-Rücklauf 50 °C.
- E3 nach Konzept 4.1; neue Betriebsschwellen über `Rechenrand.SchwelleErreicht`, transzendente Funktionen nur über
  die Plattformnaht; Obergrenze Hs/Hi wie in E2.

## 2 E2b — Schemaschritt 158 (`KesselBrennwertNachzug`)

- **Nummer** gegen `origin/ios_migration_september` gemessen: dort Zielversion 157 (Saat der Konditionierungsvorlagen),
  also `SCHRITT = KonditionierungsvorlagenSaatSchema.SCHRITT + 1` = 158.
- **Verweis Projektkopie → Katalogsatz:** der Bezeichner, der Weg des Programms (`HeizkesselCtrl.CopyFromStamm` legt die
  Kopie unter dem Bezeichner des Katalogsatzes an, `CopyFromStamm(string)` findet ihn darüber wieder). Eine ID-Beziehung
  gibt es nicht. Widersprechen sich mehrere Sätze gleichen Namens, entscheidet die Leistung auf 0,05 kW (Regel der
  E1-Nachpflege); `Tab_Heizkessel_STAMM` führt inzwischen einen eindeutigen Index auf `Bezeichner`, der Fall ist nur in
  älteren Anwenderdatenbanken möglich.
- **Brennwertkessel ist ein Katalogsatz** nach seinem Schalter **oder** nach seiner Bauart in der Beschreibung
  (`HeizkesselImportSatz.IstBrennwert`) — dieselbe Regel, mit der Import und Nachpflege den Schalter setzen. Grund: Ein
  Anwenderkatalog, den die Nachpflege (E1, Werkzeugweg) nicht erreicht hat, führt den Schalter nur bei 6 von 46
  Brennwertgeräten; der Schritt wäre dort sonst fast wirkungslos. In der Testdatenbank sind beide Merkmale deckungsgleich
  (46 Sätze).
- **Ohne eindeutigen Katalogsatz** die Bauart in der Beschreibung der Kopie; nur setzen, nie löschen; wiederholbar. Der
  Bericht nennt die nach der Beschreibung gesetzten Kopien, widersprüchliche Katalogsätze und jede Kopie ohne Zuordnung.
- **Verdrahtet:** `SchemaStand.Zielversion`, `SchemaMigration` der Schale (Konstante, Schritt, stiller Aufruf im
  Engine-Modus, Nachprobe `Vollstaendig`), `Paketanhebung` als **Umformung** (die Paketzeilen `Tab_Heizkessel` werden
  gehoben; den Katalogsatz sucht die Stufe im Katalog des Ziels, wo ihn das Programm nach dem Einspielen sucht; Text
  `TRANSFER_ANHEBUNG_KESSEL_BRENNWERT` de/en), `Werkzeuge/Testdatenbankschema`, Testvorrichtung `TestDatenbank.cs`.
- **Auslieferungsvorlage:** Sie trägt keine Projektkopie; ihre Tests prüfen den Schemastand gegen die Zielversion und
  sind mit der Testdatenbank 158 grün, ohne Änderung am Werkzeug.
- **Testdatenbank:** aus `052f5aa8…` (Schemastand 157, 81 698 816 Byte) mit `Werkzeuge/Testdatenbankschema` auf 158:
  19 Projektkopien nach dem Katalogsatz gekennzeichnet (1007, 1008, 1018, 1026 bis 1031, 1039 bis 1046, 1048, 1049),
  keine nach der Beschreibung, keine ohne Zuordnung; ohne Kennzeichen bleiben die Elektrokessel von 1017, 1024 und 1047
  (ihr Katalogsatz ist keiner). Zeilenvergleich über alle Tabellen: sonst allein `SchemaVersion` 157 → 158; Schema gleich,
  `integrity_check` ok, `foreign_key_check` leer; zweiter Lauf „steht bereits“; das 1050-Skript meldet „nichts zu tun“.
  Neue Fassung **81 137 664 Byte, LFS-SHA-256 `5d59041ffa44d7c0aa9a74c845b0a78e2cfe0c484c244d69603c352742ab27b3`**,
  mit LFS-Filter committet.
- **Tests:** `KesselBrennwertNachzugTests` (Nummer, Register, Regel ohne Datenbank, Schritt aus dem Stand davor — 22 nach
  Katalog, 1 nach Beschreibung, Elektrokessel bleiben —, nie löschen, ohne Zuordnung benannt, Repo-Datei, Werkzeug-Wache);
  `ProjektpaketAnhebungTests` hebt ein Paket mit gelöschtem Kennzeichen und findet es wieder gesetzt;
  `PaketanhebungRessourcenTests` um den neuen Schlüssel.

## 3 E3 — Brennwertkennlinie

- **`Kesselkennlinie`** (zustandslos):
  - Brennstofftafel (Konzept 7.2): Gas 57 °C / Δ₃₀ 0,08, Heizöl 47 °C / 0,04, Holz und Pellets 50 °C / 0,05 (Δ₃₀ eigene
    Ableitung, zwei Drittel von Hs/Hi − 1), sonst kein Kondensationsgewinn.
  - `RechnetMitBrennwertkennlinie`: `Brennwert` = 1, `Kennlinie_Brennwert` = 1, Brennstoff mit Δ₃₀ > 0; der Elektrokessel
    nie.
  - `Kondensationsanteil` g = (T_Tau − T_RL)/(T_Tau − 30) auf [0; 1,2]; NaN trägt keinen Gewinn.
  - `Eta30Trocken` = η₃₀ − Δ₃₀, auf zwölf Stellen gerundet (1,05 − 0,08 ist binär 0,97000000000000008; gerundet ist die
    trockene Kurve bei η₃₀,tr = η₁₀₀ flach und bitgleich η₁₀₀, der Teillastbrennstoff genau 0 — ohne die Rundung stand dort
    ein Rest von −6·10⁻¹³ kWh/a, der im gestörten Lauf byte-verschieden ausfiel).
  - `EtaBrennwert` = η_tr(β) + Δ₃₀ · g(T_RL), höchstens Hs/Hi, nie unter der trockenen Kurve; Prüfpunkte β = 0,3 bei 30 °C
    → η₃₀ und β = 1 am Taupunkt → η₁₀₀ (bitgenau).
  - `Ruecklauf`: Kette Heizkreis → Speicher → Paar → 50 °C, die erste endliche Stufe gilt.
  - `Brennwertbetrieb`: Rücklauf unter dem Taupunkt über `Rechenrand.SchwelleErreicht` — die einzige neue Schwelle; sie
    zählt nur Stunden und Wärme, der Wirkungsgrad selbst ist in T_RL stetig.
- **Rechenweg** (`SimulationSPK`): Eingänge `Heizkreisruecklauf` (AK1, von `SimulationControl` wie der Vorlauf der
  Wärmepumpe) und `RuecklaufPaarLesen` (Kette Anlage → Heizkessel); der Senkenspeicher wird nach dem Öffnen der Registry
  gesetzt (erster Puffer der Senkenliste in Rangfolge, wie das Bezugspaar „Berechnet“) und in `Stunde_Start` einmal je
  Stunde gelesen (`RL_eff`, geschichtet `T_unten`). `Stunde_Abschluss` nimmt je Laufstunde η_eff, trennt den
  Teillastbrennstoff (gegen die trockene Kurve) vom Brennwertbrennstoff und schreibt Brennwertstunden und -wärme,
  wärmegewichteten Rücklauf und Rücklaufreihe mit. Jeder Kessel ohne Brennwertkennlinie rechnet Anweisung für Anweisung
  wie in E2.
- **Keine Plattformnaht nötig:** keine transzendente Funktion; `Math.Round` ist IEEE-Arithmetik.
- **Laufprotokoll:** Stützwerte (Taupunkt, Δ₃₀, η₃₀ trocken, Hs/Hi, Rückfall bzw. Paar), Rücklauf aus dem Senkenspeicher,
  am Jahresende die Laufstunden je Stufe der Kette, mittlerer Rücklauf, Brennwertstunden; die **Kohärenzzeile** (Konzept 5)
  als Hinweis, wenn der Rücklauf in mindestens der Hälfte der Betriebsstunden über dem Taupunkt lag (ganzzahlig
  verglichen). Der Warnkriterienkatalog prüft vor dem Lauf und kennt den Rücklauf nicht; der Kartenhinweis
  „Brennwertkessel ohne Kennlinie“ bleibt offen (nach E2b träfe er jeden Brennstoffkessel der Bestandsprojekte).
- **Oberfläche und Export (Konzept 5):** Gruppe „Betrieb“ mit mittlerem Rücklauf, Anteil der Stunden und der Wärme im
  Brennwertbetrieb und Brennwertbrennstoff; Kesseltabelle mit Rücklauf und Brennwertanteil (Striche ohne
  Brennwertkennlinie); CSV-Export je Kessel die Rücklaufreihe; `aggregate.csv` je Kessel `RuecklaufMittel`,
  `Brennwertstunden`, `BrennwertWaermeKwh`, `BrennwertKwh` (ohne Brennwertkennlinie 0 — ein später eingeschalteter Kessel
  ändert eine Zahl, statt einen Schlüssel hinzuzufügen); 14 Ressourcenschlüssel de/en, Designer gezogen;
  `ParameterVerwendung` führt `Kennlinie_Brennwert` als gerechnet.
- **Tests:** `KesselKennlinieTests` um die Kurve (Prüfpunkte, trockenes η₃₀, Heizöl, Klemmung, NaN, Obergrenze, Wahl,
  Kette, Schwelle), den Lauf von 1050 (Rückfall 50 °C in jeder Laufstunde, Ergebnisseite, Protokoll) und je einen Lauf
  an einer Arbeitskopie für die Stufen Paar (1050 mit 55/35 °C), Senkenspeicher (1050 lädt den Puffer der Wärmepumpen)
  und Heizkreis (1047 mit AK1, Kessel zum Gas-Brennwertkessel gemacht); 1007 als Brennwertkessel ohne Kennlinie und als
  Niedertemperaturkessel ohne Kennzeichen; `ErzeugerReiterTests` um die Brennwertgrößen.

## 4 Basis R28 und A/B gegen R27

`Referenzlaeufe/2026-09-30_R28_Kesselbrennwert/` — sechzehn Projekte, 487 CSV, 2 984 Skalare, auf Linux eingefroren.
Zweiter Lauf 487/487 byte-gleich; gestört (`--stoerung ulp`) gegen ungestört 16/16 PASS, 480/487 byte-gleich (dieselben
sieben Dateien wie mit R27). R27 archiviert unter [`Referenzbasen/`](../../Referenzbasen/LIESMICH.md). Vorab: R27 rechnet
auf der Ausgangsfassung `052f5aa8…` 487/487 byte-gleich.

A/B gegen R27 (`vergleich --ohne` die vier neuen Schlüssel): **4/16 PASS** (1017, 1023, 1024, 1047), 471/487 CSV
byte-gleich; abgewichen ist allein `aggregate.csv`, jede Zeitreihe ist byte-gleich. Getrennt über einen Zwischenlauf mit
dem Rechenweg E3 auf `052f5aa8…` (vor dem Schritt 158): E3 allein gegen R27 15/16 PASS (nur 1050), E2b danach elf
Projekte.

| Ursache | Projekte | Wirkung |
|---|---|---|
| E2b (Schritt 158) | 1007, 1008, 1018, 1030, 1039, 1040, 1041, 1042, 1045, 1046, 1049 | η₃₀ = η₁₀₀ + 0,06 statt η₁₀₀; Brennstoff, Nutzungsgrad, `Eta30`, `EtaBetrieb`, `TeillastKwh`, Kesselemissionen, bei sechs Projekten die Gasspitze |
| E3 (Brennwertkennlinie) | 1050 | η_eff am Rückfall 50 °C: Brennstoff, Nutzungsgrad, `EtaBetrieb`, `TeillastKwh` → 0, Kesselemissionen, Gasspitze |
| neue Skalare | alle sechzehn | vier Schlüssel je Kessel (64) |

Je Projekt (Brennstoff = `HeizkesselModul[0].Verbrauch` in MWh/a, Nutzungsgrad in %):

| Projekt | Ursache | η₁₀₀ | η₃₀ R27 → R28 | Brennstoff R27 → R28 | Nutzungsgrad R27 → R28 | η Betrieb | Laststufe | Teillast kWh/a | Brennwert kWh/a |
|---|---|---|---|---|---|---|---|---|---|
| 1007, 1046 | E2b | 0,876 | 0,876 → 0,936 | 10,55 → 10,01 (−5,1 %) | 85,80 → 90,43 | 0,924 | 0,23 | −540 | 0 |
| 1008 | E2b | 0,876 | 0,876 → 0,936 | 23,65 → 22,86 (−3,3 %) | 86,90 → 89,89 | 0,906 | 0,38 | −787 | 0 |
| 1018 | E2b | 1,000 | 1,000 → 1,060 | 11,09 → 10,46 (−5,7 %) | 100,00 → 106,00 | 1,060 | 0,03 | −628 | 0 |
| 1030 | E2b | 1,000 | 1,000 → 1,060 | 5 403,10 → 5 203,20 (−3,7 %) | 100,00 → 103,84 | 1,038 | 0,36 | −199 903 | 0 |
| 1039 | E2b | 0,980 | 0,980 → 1,040 | 295,17 → 293,35 (−0,6 %) | 98,00 → 98,61 | 0,986 | 0,76 | −1 818 | 0 |
| 1040 | E2b | 1,000 | 1,000 → 1,060 | 16,19 → 15,27 (−5,7 %) | 100,00 → 105,96 | 1,060 | 0,09 | −911 | 0 |
| 1041 | E2b | 1,000 | 1,000 → 1,060 | 149,91 → 142,85 (−4,7 %) | 100,00 → 104,94 | 1,049 | 0,21 | −7 061 | 0 |
| 1042 | E2b | 0,980 | 0,980 → 1,040 | 19,32 → 18,21 (−5,7 %) | 98,00 → 104,00 | 1,040 | 0,05 | −1 115 | 0 |
| 1045 | E2b | 1,000 | 1,000 → 1,060 | 22,27 → 21,01 (−5,7 %) | 100,00 → 105,97 | 1,060 | 0,09 | −1 255 | 0 |
| 1049 | E2b | 1,000 | 1,000 → 1,060 | 6,32 → 5,96 (−5,7 %) | 100,00 → 106,00 | 1,060 | 0,06 | −358 | 0 |
| 1050 | E3 | 0,970 | 1,050 → 1,050 | 81,96 → 80,66 (−1,6 %) | 97,37 → 98,94 | 0,991 | 0,82 | 0 | −1 722 |
| 1023 | — | 0,874 | 0,934 → 0,934 | 91,02 → 91,02 | 87,67 → 87,67 | 0,878 | 0,82 | −396 | 0 |
| 1017, 1024, 1047 | — | Elektrokessel ohne Kennlinie, unverändert |||||||| |

**Größenordnung.** Die meisten Kessel der Referenzprojekte laufen im Mittel unter 30 % Last und rechnen deshalb fast
durchweg mit η₃₀: Mit der Vorgabe des Brennwertkessels sinkt ihr Brennstoff um 0,06/1,06 ≈ 5,7 %. 1039 läuft im Mittel
bei 76 % Last und spart nur 0,6 %. Die Kessel mit dem Platzhalter η₁₀₀ = 1,0 (1018, 1030, 1040, 1041, 1045, 1049) kommen
auf einen heizwertbezogenen Jahresnutzungsgrad bis 106 % — unter Hs/Hi und für einen Brennwertkessel in Teillast richtig,
aber auf einem ungepflegten Nennwert gerechnet; die Ergebnisseite nennt den Platzhalter weiterhin. 1050: Mit η₃₀,tr =
1,05 − 0,08 = η₁₀₀ ist die trockene Kurve flach, der ganze Gewinn gegenüber η₁₀₀ ist Kondensation (0,08 · 7/27 = 0,021
bei 50 °C Rücklauf), 1 722 kWh/a Brennstoff; der Rücklauf liegt in allen 5 053 Laufstunden unter dem Taupunkt.

## 5 Einfrierregel

„Gesäte Kesseldaten“ (`CLAUDE.md`, `Referenzlaeufe/LIESMICH.md`) ist erweitert: Das Kennzeichen `Brennwert` steht jetzt
bei jedem Brennstoffkessel der Referenzprojekte; beim Referenzprojekt 1050 gehört dazu, was den Rücklauf seiner
Brennwertkennlinie bestimmt (Temperaturpaar an Anlagenzeile und Projektkessel, Senken des Kessels in `Z_AnlageSenke`,
Kopplungsstufe).

## 6 Gate

(folgt nach dem Merge mit `origin/ios_migration_september`)

## 7 Wiki und Logbuch

Wiki-Quelle `Projekte/Wiki/Programm Dokumentation - Heizkessel.wiki`: Abschnitt „Brennwertkennlinie“ (Formel,
Rücklaufkette, Brennstofftafel, neutrales Beispiel), die Gruppe „Kennlinie“ (Brennwertkennlinie rechnet), Kennzeichen
Brennwertkessel (folgt dem Katalog), Ergebnisse (Brennwertgrößen, Rücklaufreihe) und Fallstricke. Tabu-Grep leer, keine
Produktdaten.

Logbuch unter **1.2.0.6** (Vorschlag):
- „Brennwertkessel mit eingeschalteter Brennwertkennlinie rechnen je Stunde mit dem Rücklauf aus Heizkreis, Speicher
  oder Temperaturpaar; unter dem Taupunkt steigt ihr Wirkungsgrad.“
- „Heizkessel in bestehenden Projekten übernehmen das Kennzeichen Brennwertkessel aus dem Katalog.“

## 8 Offene Punkte

1. **E4 Takten.**
2. Kartenhinweis „Brennwertkessel ohne Kennlinie“ (Konzept 5) — Anwenderentscheidung, weil er nach E2b jeden
   Brennstoffkessel der Bestandsprojekte träfe.
3. Vorlagenfelder des Berichts (Konzept 5) und die kleine Kurve im Katalogeditor.
4. Brennstofftafel (7.2) gegen DIN EN 15316-4-1 abgleichen; Δ₃₀ von Holz und Pellets ist eine eigene Ableitung.
5. Windows-Lauf gegen R28 steht aus (erwartet wie in `Werkzeuge/Gate/LIESMICH.md`).
6. Statuszeile in `Status_iOS_Migration.md` beim Zusammenführen.
