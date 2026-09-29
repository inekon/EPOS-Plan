# SK4 — Kessel-Kennlinie, Etappe E1: Daten und Import (#616)

Stand: 29.09.2026 · Zweig `ios_migration_september` · Opus-Agent im Worktree, Zweig `kessel-e1` ab `c47869c1`.
Commits:
- `154f4f4a` Schemaschritt (Betreff nennt noch 155; die Nummer ist in `e77af823` auf 156 gesetzt)
- `cb021443` Modell und Controller
- `6f9952a1` Import aus Satz 710.01
- `e77af823` Merge #608
- `271ac146` Nachpflege, Werkzeugweg, Testdatenbank
- `ea625475` Oberfläche
- `5231d10b` Katalogfilter-Wachen
- `7b57f62b` Papiere
- `f88fd158` Merge #615

Merge nach `ios_migration_september` `ac5ae487`. **Schemaschritt 156.** Konzept
[`Konzept_Kessel_Kennlinie_EPOS-Plan.md`](../../../aktuell/Konzept_Kessel_Kennlinie_EPOS-Plan.md); Statuszeile „#616“ in
[`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Vorgänger
[`SK3_Kessel_Heizgrenze_R24_Protokoll.md`](SK3_Kessel_Heizgrenze_R24_Protokoll.md).

## 1 Auftrag und Entscheide

Anwenderauftrag 29.09.2026: „starte das Konzept in der Reihenfolge E1 → E2 → E3 → E4.“ Die Entscheide vom selben Tag:

- **F1** und **E4**: Normvorgaben für leere Felder.
- **F2**: den Bestand des Katalogs nachpflegen.
- **F3**: η₁₀₀ aus Satz 710.01.
- **F4**: Referenzprojekt als Kopie von 1023, nicht in der CI.
- **F5**: Rückfall-Rücklauf 50 °C.

E1 legt nur Daten an. Kein Rechenweg liest sie; die Wirkung kommt mit E2 bis E4.

## 2 Umsetzung

- **Schema 156** (`KesselKennlinieSchema`, `SCHRITT = TwwFuellstandSchema.SCHRITT + 1`): fünf Spalten, an
  `Tab_Heizkessel_STAMM` und `Tab_Heizkessel` gleich. `Wirkungsgrad_Teillast30` (REAL, η bei 30 % Last, Hi),
  `Kennlinie_Brennwert` (INTEGER 0/1, Vorgabe 0), `Mindestleistung` (REAL, kW), `Anfahrverlust_kWh` (REAL),
  `Mindestlaufzeit_min` (INTEGER). Nullbar heißt „nicht gepflegt“. Kein DML; der Kessel rechnet unverändert.
- **Modell und Controller** lesen und schreiben die Gruppe; die Projektkopie trägt den Katalogsatz Spalte für Spalte.
- **VDI-3805-Import (Blatt 3, Satz 710.01):** η₁₀₀, η₃₀ und die kleinste Leistung, dazu die Kennzeichnung der
  Brennwertgeräte. Die Spaltenbelegung ist empirisch über 1 650 Sätze aus 14 Dateien bestimmt, weil der Normtext von
  Blatt 3 nicht vorlag. Ein unplausibler η₃₀ („9.5“) bleibt leer.
- **Nachpflege des Katalogs** (Werkzeugweg `KesselkatalogNachpflege`, kein Schemaschritt):
  - 60 von 63 Sätzen nachgepflegt: η₃₀ in 46, η₁₀₀ in 27, Mindestleistung in 60, `Brennwert` 0 → 1 in 40.
  - Damit gibt es 46 Brennwertgeräte statt 6, genau die 46, deren Beschreibung „Brennwert“ nennt (Datenfehler D-1 behoben).
  - Drei Sätze ohne passenden Dateisatz bleiben (zwei Elektrokessel und „Test“).
  - `Brennwert` wird nur gesetzt, nie gelöscht. Die Projektkopien sind zellgleich geblieben; ein zweiter Lauf ändert nichts.
  - In 27 Sätzen ändert sich η₁₀₀ nach F3 (etwa 0,874 → 0,96). Das wirkt nur auf Projekte, die neu aus dem Katalog angelegt werden.
- **Oberfläche:** Gruppe „Kennlinie“ im Kesseleditor und im Aufklapper, KI-Feldtafel, Ressourcen de/en. Die rohen
  Ressourcenschlüssel im Importdialog sind behoben; eine Wache prüft jetzt alle Importarten.
- **Testdatenbank:** LFS `111be18945ece5aba5e605eeca082993646e0a267428eb7400d29c3fa2b4c39d` (71 626 752 Byte),
  Schemastand 156, mit LFS-Filter committet.

## 3 Vorgaben für leere Felder (Konzept 7.1, wirksam ab E2/E4)

| Größe | Vorgabe | Herkunft |
|---|---|---|
| η₃₀ | Brennwertkessel η₁₀₀ + 0,06 (höchstens Hs/Hi), Niedertemperaturkessel = η₁₀₀, Standardkessel η₁₀₀ − 0,03 | Richtlinie 92/42/EWG Art. 5, VO (EU) 813/2013 Anhang III |
| Mindestleistung | 30 % der Nennleistung (Gas-Brennwertkessel), sonst 60 % | Prüfpunkt 30 %, Auswertung der VDI-Dateien |
| Anfahrverlust | 0,002 h × Nennleistung | eigene Abschätzung, keine Normquelle |
| Mindestlaufzeit | 10 min | übliche Werkseinstellung |

Keine Normtabelle ist abgeschrieben, die Werte sind gerundet.

## 4 Gate

GATE_PLATZHALTER

## 5 Offene Punkte

1. **E2 Teillast:** η je Stunde aus der Kennlinie, Normvorgaben wirksam, Referenzprojekt als Kopie von 1023 (nicht in der CI), neue Basis.
2. **E3 Brennwert:** Rücklaufkette mit Rückfall 50 °C (F5). **E4 Takten:** Starts aus Mindestleistung und Mindestlaufzeit, Anfahrverlust.
3. Logbuch-Satz: Version beim Anwender bestätigen (vorgeschlagen 1.2.6).
4. Nicht angefasst, vorbestehend: Profileinheit „%“ beim Bereitschaftsverlust gegen kW im Editor.
5. Der KP2-Entwurf der Gebäudesimulation plant seine Saat als Schritt 156; nach diesem Schritt wird das 157.
