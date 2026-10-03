# Protokoll P4d — Pufferspeicher-Auslegung: Nutzen-Aufwand-Zeile mit Nachbarstufen, Speicher-gegen-Leistung-Kurve, Aufheizkriterium (03.10.2026)

Anwenderauftrag 03.10.2026 (Folgeauftrag 4, V47/E-P32, und V30 aus Folgeauftrag 10). Opus-Agent im Worktree, Zweig
`claude/p4d-pufferauslegung` auf `3766779a`; Merge (Fast-Forward) auf `2bd36693`. Kein Schemaschritt; die Saat der
neuen Vorgabeschlüssel ist mit der Bereinigung von Schritt 179 nachgetragen. Statuszeile **#701**. Dieses Protokoll
trägt auch das **Gate über alle Wellen** des Tages ab P4b (Abschnitt 4).

## 1 Was gebaut ist

| Commit | Inhalt |
|---|---|
| `84e08e87` | `PufferNachbarstufen.cs` (`PufferAuslegung.Nachbarstufen(eingang, ergebnis, anzahl = 2)`): Empfehlung und je zwei Nenninhalte darunter und darüber (über dem Listenende im Raster weiter); je Stufe D1 und D2 mit festem Volumen ohne Bisektion (Heizzone, sonst Prozesszone), Mehrvolumen, Verlust kWh/a nach K11 einheitlich mit der Klasse-C-Grenze, JAZ-Hinweis als Textbaustein (qualitativ); **Kurve (V47)**: Leistungsstufen 60–140 % der Ladeleistung, Laufvolumen = D_max + L(P) − L(P_lade) nach Lindley, V = Laufvolumen · 1000/(c · ΔT_B · η_s) mit ΔT_B 50 K bei Trinkwasserspeicher, Laufzeit = Tagesbedarf/P mit Band 16–20 h; ohne Zapfprofil, Ladeleistung, D_max oder Zapfreihe Hinweis statt Kurve. Hülle misst die Rechenzeit: unter 1 000 ms rechnet sie die Nachbarstufen mit, sonst Knopf „Nachbarstufen rechnen“. Seite: Nutzen-Aufwand-Tabelle und Kurve in Schritt 4; Bericht: Zeile je Nachbarstufe bei nachgerechneter gespeicherter Auslegung |
| `b9694778` | **K12 „Aufheizen nach Absenkung“ (V30):** V_auf = max(Φ_n − P_gen, 0) · n · h/(c · ΔT · η_s), Φ_n Summe `Aufheiz_Leistung_Kw` der Gebäude des jüngsten Laufs mit Gebäudeergebnis, n längste `Aufheizzeit_Max_H` (sonst Vorgabe `Pufferauslegung.Aufheiz.Dauer_h` = 2), P_gen Nennleistung Rang 1; aktiv nach Anwenderschalter, sonst nach `Pufferauslegung.Aufheiz.Nutzungsprofil.<Profil>` (nur BUERO_SCHULE); ohne Bemessung inaktiv mit `PA-AUFHEIZ-KEINE-BEMESSUNG`; Herkunft „Konzept Pufferauslegung V30 / KP3“; Karte K12 in Schritt 3 (Stufe Standard, Kartenzahl 9 → 10); `PufferAuslegungCtrl.Aufheizbemessung` (`SQL_AUFHEIZ`) |
| `2bd36693` | `PufferNachbarstufenTests`, `PufferAufheizTests`, `PufferAufheizCtrlTests` (Ergebniszeilen auf Projekt 1052 in der Arbeitskopie, weil die Testdatenbank keine Gebäudeergebnisse führt), `PufferNachbarstufenHuelleTests`, `PufferAuslegungSeiteTests` (+4); Warncodes 18 → 19 |

42 Ressourcenschlüssel de/en (`PAUS_NA_*`, `PAUS_KURVE_*`, `PAUS_JAZ_*`, `PAUS_KRIT_K12`, `PAUS_HERK_*`, `PAUS_WEG_K12*`,
`PA_AUFHEIZ_KEINE_BEMESSUNG(_TEXT)`, `PAUS_BERICHT_NA_ZEILE`).

## 2 Festlegungen

- Handrechnung K12: Φ_n 80 kW, P_gen 50 kW, n 2 h, ΔT 20 K, η_s 0,85 → 3 042,6 l (gerundet 3 043 l).
- Laufzeit auf der Testdatenbank (1045, Kombipuffer): Auslegung samt Zeile 5 ms, Nachbarstufen 3 ms.
- Der Schalter K12 des Anwenders wird nicht gespeichert (Kriterienmaske kennt neun Vorlagenschalter); dafür wäre ein
  weiteres Bit in `Kriterien_Aktiv` nötig — mit dem nächsten Schemaschritt.
- Zeitmessung in der Hülle, nicht im Kern (Kern ohne Uhr).

## 3 Bereinigung von Schritt 179 (Anwenderentscheid 03.10.2026)

Der Verweis `Tab_Gebaeude.ID_Konditionierungsvorlage` aus P4c wird nicht angelegt: Die Pufferauslegung braucht nur die
Nutzung, die seit Schritt 176 als Kopie in `Tab_Konditionierungskalender.Nutzung` steht (Linie „Kopie statt Verweis“,
KP1b, #687, 176); ein Fremdschlüssel auf die Vorlagentabelle würde Katalogabgleich und Vorlagenlöschen auf Projektdaten
durchschlagen lassen. Schritt 179 trägt damit sechs Spalten (`Tab_PufferAuslegung` fünf, `Tab_Pufferspeicher.ID_Stamm`)
und sät die Vorgabeschlüssel `Pufferauslegung.Aufheiz.*` (Saat 148 → 154). Testdatenbank aus der Fassung 176 neu
gehoben: 83 169 280 Byte, LFS-SHA-256 `799da43afcbd99469445311e8c5168109df5fed5a87f7da69d8a984fbadf57f2` (origin-Fassung 176 `bb8dd3dc…` mit den Referenzprojekten 1051 und 1052 → Zapfkatalog-Skript 24 Zeilen → Schemawerkzeug 176 → 179; `integrity_check` ok, `foreign_key_check` leer, Saat 154, STRICT 163)

## 4 Gate über alle Wellen des Tages ab P4b (Stand `80e1e5b9` plus Papiere)

Kern-Filter 0 Fehler; Tests Kern 10 597 (1 übersprungen), UI 7 401, KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen), alle grün; Windows-Schale auf Linux 0 Fehler; Referenzlauf 16/16 PASS gegen R33, 487/487 CSV byte-gleich; SQL-Prüfer 2 296 Texte, 0 Fundstellen; ResourceDesigner unverändert; `Auslieferungsvorlage.Tests` 47/47.
