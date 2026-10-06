# Protokoll BA-2 — Typaufbauten und Ersatzaufbau beim Import (06.10.2026)

**Sitzung:** IFC / Gebäudeimport, Statuszeile **#786**. Ein Opus-Agent im Worktree. Commits `0c1a5057` (Schema 192 und Saat), `b422b3b3` (Testdatenbank, beim Merge verworfen), `7c91f3cf` (Ersatzaufbau, Stufen), `725515b2` (Auslieferungsvorlage); Merge `f2d016d3` auf 190/191 mit Testdatenbank von 191 auf 192, Merge `42bf8935` auf den gepushten Stand mit 191.
**Entscheid:** E95; Konzept [Bauteilaufbau beim Gebäudeimport](../../../aktuell/Gebaeudesimulation/2026-10-06_Konzept_Bauteilaufbau_Import.md) 5.3 und Welle BA-2.

## 1 Auftrag

Bauteile der Stufen B und C ohne echten Aufbau rechnen nicht mehr masselos: Der Import setzt einen Ersatzaufbau aus Typaufbauten ein, abgeglichen auf das U der Datei.

## 2 Gebaut

- **Schemaschritt 192 `TypaufbauSchema`:** Spalte `Typaufbau TEXT` an `Tab_Bauteilaufbau` und `Tab_Bauteilaufbau_STAMM` (nullbar, `CHECK` auf neun Codes; NULL = echter Aufbau), Fachspalte in `Katalogfassung`.
- **Typsaat** (Katalog): neun Typaufbauten mit 31 Schichten. Außenwand: massiv außen gedämmt, monolithisch, massiv ungedämmt, massiv innen gedämmt, Holzleichtbau; Dach: Stahlbeton gedämmt, Sparrendach; Boden: Dämmung oben (gegen Erdreich), Dämmung unten (Kellerdecke). Stoffwerte nur aus der Normsaat (DIN 4108-4:2020-11, DIN EN ISO 10456:2010-05).
- **Ersatzaufbau beim Import** für opake Hüllbauteile der Stufen B/C ohne echten Aufbau: Typwahl nach Bauart und Baualtersklasse (massive Außenwand bis Klasse F ungedämmt, ab G außen gedämmt); Abgleich geschlossen über R (Dämmdicke im Band 1 mm bis 40 cm; ohne Dämmung λ im Band 0,5- bis 3-fach); Rückfälle mit Vermerk (Typwechsel, ohne Dämmung, Band). Die Projektkopie trägt die Herkunft VORGABE und den Code in `Typaufbau`; das U der Zeile bleibt (Gl. 27).
- **Stufen:** erkennen den Ersatz (`Aufbauluecke.Ersatzaufbau`), die Bauteile zählen B/C statt A.
- Innenbauteile bleiben im Klassenweg (E95-5).
- **Auslieferungsvorlage:** nimmt Spalte und Saat mit; Referenzlauf byte-gleich, keine Einfrierregel berührt (die Saat ist Katalog, kein Referenzprojekt trägt einen Aufbau).

## 3 Befund an den Anwenderdateien

Masselose opake Außenfläche vorher → nachher; Jahresheizwärme Z4 in MWh/a; Spitze in kW.

| Datei | masselos | Jahresheizwärme Z4 | Spitze |
|---|---|---|---|
| MFH 1984 | 69,1 % → 0,4 % | 46,27 → 46,01 | 27,05 → 24,80 |
| MFH 1964 | 100 % → 0,45 % | 41,36 → 41,09 | 29,94 → 27,16 |
| Sportheim | 76,6 % → 0,26 % | 59,15 → 58,69 | 52,58 → 49,28 |
| Verwaltung | 92,4 % → 0,44 % | 238,31 → 233,59 | 263,26 → 261,22 |
| WG-EH55 | 93,6 % → 0 % | 17,67 → 17,53 | 21,77 → 20,83 |
| Produktion | 99,2 % → 0,12 % | 1856,82 → 1828,57 | 1273,75 → 1251,39 |

Nachher bleiben nur Türen masselos; die Auslegungsheizlast ist unverändert.

## 4 Abweichungen vom Konzept

- Neun statt rund 14 Typen: die Innentypen entfallen nach E95-5.
- Der Namensabgleich von Schichtsätzen auf Typgruppen (5.3) ist nicht gebaut.
- Die erweiterte Stofftafel für λ/ρ und der U-Abgleich der Dämmschicht bei fehlendem λ (5.1) sind nicht gebaut.
- Der Import nimmt die Typen aus der Code-Saat, derselben Quelle, die der Schritt in den Katalog sät.

## 5 Prüfung

Ohne Befund: P6c im Gesamtlauf grün; der frühere rote Lauf lief gegen eine Datenbank ohne Zielstand.

## 6 Gate 786

⟨GATE786⟩

## 7 Offen

- **BA-3** (Oberfläche mit Bauteilsteckbrief, E98) und **BA-4b** (Aufbauten aus der Projektdatei, E98) sind gebaut; Gate und Push folgen als eigene Welle.
- Katalogfassung bei der nächsten Auslieferung anheben.
- Logbuch-Entwurf (Version beim Anwender erfragen): „Importierte Gebäude erhalten für Bauteile ohne vollständigen Aufbau einen Ersatzaufbau aus Typaufbauten, abgeglichen auf den U-Wert der Datei.“
- Hinweis an die Sitzung Wirtschaftlichkeit: Schemaschritt 193 hängt an 192.
