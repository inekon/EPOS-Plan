# Protokoll P2 — Pufferspeicher-Auslegung: Oberfläche mit drei Einstiegen (03.10.2026)

Fortsetzung von [`2026-10-03_P1_Pufferauslegung.md`](2026-10-03_P1_Pufferauslegung.md) nach dem Auftrag „starte P2
Oberfläche“ (Anwender, 03.10.2026). Grundlage: Konzept
[`Konzept_Pufferspeicher_Auslegung_EPOS-Plan.md`](../../../aktuell/Konzept_Pufferspeicher_Auslegung_EPOS-Plan.md)
Abschnitt 6 und das Mockup `Dokumentation/aktuell/Mockups/Pufferspeicher_Auslegung_Mockup.html`. Bau durch einen
Opus-Agenten im Worktree, Zweig `claude/p2-pufferauslegung` auf `333528be`. Statuszeile **#682**.

## 1 Was gebaut ist

| Welle | Commit | Inhalt |
|---|---|---|
| W5 | `c76804c4` | `EPOS.UI/Seiten/Pufferspeicher/PufferAuslegungSeite.razor` mit `PufferAuslegungDaten`, `PufferAuslegungTexte`, `PufferAuslegungKiSicht`; Hülle `EPOS.UI.Daten/Pufferspeicher/PufferAuslegungHuelle.cs` mit der Naht `Pufferauslegungswege.Fenster`; freie Ansicht der `AppWurzel` (Seitenschlüssel `PUFFER_AUSLEGUNG`, Rückweg, Text bei fehlender Ansicht), `IProjektQuelle`; CSS-Block `.epos-pausl-*` nur mit Tokens; 255 Ressourcenschlüssel de/en (`PAUS_*`, 17 Warncodes `PA_*`, `KI_DLG_PAUS_*`, `SIMERG_ZUSTAND_ANLASS_PUFFERAUSLEGUNG`); KI-Maske `Form_PufferAuslegung` (Katalog 89 → 90 Masken, 13 Felder, Sichtklasse, Ausnahmegrund, Maskenziel) |
| W6 | `ad26ba3c` | Einstiege: „Pufferspeicher auslegen…“ in der Simulationskonfiguration (ein Puffer → dieser, sonst neu; gültiges Simulationsergebnis gilt nach Übernahme als veraltet), „Auslegen…“ im `PufferSpProjektDialog` und im Kachel-Dialog `PufferspeicherDialog` (Windows-Hülle schließt das Fenster und öffnet die Ansicht), „An Speicherauslegung übergeben…“ im Zapfprofil-Auslegungsdialog aktiviert (Windows: eigenes Fenster `PufferAuslegungFenster`; Zielpuffer: Puffer mit Brauchwasser, sonst Heizungspuffer als Kombi bei Frischwasserstation, sonst neuer Speicher {B}); `help_mapping.txt`; iOS-Naht `IosProjektQuelle.PufferAuslegungGaben`; Kern: `PufferAuslegungCtrl.ZapfprofilAus(Auslegungsrechnung, out text)` aus `AusGenerator` herausgelöst (gleicher Summenweg über Topologiegruppen für den ungespeicherten Zapfprofil-Stand, Rechenweg unverändert) |
| W7 | `4940f1eb` | bunit `PufferAuslegungSeiteTests` (15 Fälle, de und en), `PufferAuslegungHuelleTests` (8 Fälle mit Testdatenbank: 1045 Kombi, 1041 Prozess neu, Übernahme nur auf Kopien); Ergänzungen in `SimulationKonfigSeiteTests`, `PufferSpProjektDialogTests`, `PufferspeicherDialogTests`, `ZapfprofilAuslegungDialogTests`, `AppWurzelTests`, `TestProjektquelle`; Wachen `KiDialogkatalogTests`, `KiMaskenabdeckungWacheTests` (26 Eingabestellen, 13 im Katalog, Rest vermerkt) |
| Merge | ``558123c9`` | Zusammenführung mit origin (Welle M6 Katalogabgleich, Schema 172, Statuszeile #678, iOS-Betriebskalender): Ressourcen beidseitig zusammengeführt, Designer neu erzeugt |

## 2 Festlegungen beim Bau (Abweichungen von Mockup und Konzept 6)

- **Rechnen:** Schalter rechnen sofort; Zahlenfelder markieren das Ergebnis als veraltet, gerechnet wird beim Wechsel in
  Schritt 4 oder mit „Neu rechnen“ (Betriebssimulation über 8 760 h mit Bisektion).
- **Nutzen-Aufwand-Zeile, Betriebsbild und Dauerlinie entfallen**, weil der Kern keine Nachbarstufen liefert (V47);
  nur Kennzahlen.
- **Nicht gespeichert:** Kriterienschalter, Expertenweg der Sperrzeit, Auslegungsheizlast, Wohneinheiten (keine Spalten
  in `Tab_PufferAuslegung`); sie wirken nur in der Sitzung. Geschrieben wird nur mit „Auslegung speichern“ oder
  „Übernehmen“, keine Rückfrage beim Verlassen.
- **Einstiege „Auslegen…“** verlassen den Dialog wie Abbrechen (Hinweiszeile); die Konfiguration liest den Speicher beim
  Rückweg frisch.
- **Herkunfts- und Rechenwegtexte** kommen als deutscher Klartext aus dem Kern (in Englisch deutsch); Marken,
  Kriteriennamen und Warntexte aus den Ressourcen, der Klartext der Warnung mit Zahlen als Tooltip.
- **Menü unverändert** (kein Menüpunkt, wie bei der Stromspeicher-Auslegung). Hilfe zeigt bis P3 auf die Wiki-Seite
  „Pufferspeicher“.
- **iOS:** Konfiguration und Pufferverwaltung als freie Ansicht; die Zapfprofil-Übergabe ist weich gesperrt und nennt den
  Grund (kein eigenes Fenster); der Kachel-Knopf nur unter Windows. Ein iOS-Lauf wäre begründet (Adapter geändert) und
  ist **nicht** gestartet (Rückfrage).

## 3 Nachweise

Gate des Agenten (`4940f1eb` auf `333528be`): Kern-Filter 0 Fehler; Tests Kern 10 286 (1 übersprungen), UI 7 333,
KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen); Windows-Schale auf Linux 0 Fehler; ResourceDesigner
Prüfmodus sauber; SQL-Prüfer 2 211 Texte, 0 Fundstellen; Referenzlauf CI-Sieben 7/7 PASS gegen R33.

Gate auf dem Merge-Stand (``558123c9``, Schemastand 172): Kern-Filter 0 Fehler; Tests Kern 10 305 (1 übersprungen), UI 7 341, KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen), alle grün; Windows-Schale auf Linux 0 Fehler; Referenzlauf 16/16 PASS gegen R33, 487/487 CSV byte-gleich; SQL-Prüfer 2 231 Texte, 0 Fundstellen; ResourceDesigner unverändert.

Nachtest nach dem zweiten Merge (origin #679–#681): Kern-Filter 0 Fehler; Tests Kern 10 333 (1 übersprungen), UI 7 357, KiKern 549, SpeicherEngine 397, SpeicherPlanung 27 (1 übersprungen); Windows-Schale 0 Fehler; Referenzlauf 16/16 PASS gegen R33, 487/487 byte-gleich; SQL-Prüfer 2 231 Texte 0 Fundstellen; ResourceDesigner unverändert.

## 4 Offen (P3)

Berichtsabschnitt Pufferauslegung; Wiki-Seite „Pufferspeicher auslegen“ und Umstellen von `help_mapping`,
Grundlagenseite ergänzen; Logbuch-Satz (Version erfragen); Ressourcenschlüssel für Herkunft und Rechenweg im Kern;
Spalten für Kriterienschalter, Expertenweg, Heizlast und Wohneinheiten, falls sie gespeichert werden sollen;
Nachbarstufen im Kern für die Nutzen-Aufwand-Zeile (V47); Vorbelegung aus den Teillastfeldern der Welle M4;
Sichtabnahme unter Windows; iOS-Lauf nach Rückfrage.
