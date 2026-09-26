# Gebäudesimulation — Übergabe auf ein anderes Konto (26.09.2026)

Übergabe der Sitzung „Gebäudesimulation EPOS-Plan“ beim Wochenkontingent von über 80 %. Sie sagt, was
fertig ist, was offen ist und wie es weitergeht. Maßgeblich für den Stand bleiben die
[Statusdatei](../Status_Gebaeudesimulation_VDI6007.md), das [Register](../Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md)
und `git log origin/ios_migration_september`; die Parallelübergabe zu G6c und Klasse M/A steht in
[`2026-09-26_Uebergabe_G6c_Katalog_M_A.md`](2026-09-26_Uebergabe_G6c_Katalog_M_A.md).

## 1. Fertig und gepusht

| Stufe | Inhalt | Nachweis |
|---|---|---|
| AK1 | Heizkreis und Kühlübergabe (E36, E37), Referenzprojekt 1047 mit Kopplung | Basis R15 eingeführt, heute in R20 enthalten |
| Bestandsvergleich (Q24 Bed. 1) | Werkzeug `Werkzeuge/Gebaeudevergleich`, alle 27 Gebäude des Bestands erklärt | Protokoll `ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_Bestandsvergleich_Q24.md` |
| G6a | Pflege mehrerer Zonen (E46) | #526 |
| G6b | Mehrzonen-Rechnung bis 50 Zonen, Schritt 147, E49, N1.55/N1.56 | #538, CI 36229121548 grün, Protokoll `…/2026-09-26_G6b_Mehrzonenrechnung.md` |
| G7a | gbXML-Export ohne Geometrie hinter Freigabeschalter, E48, N1.53/N1.54 | #529, Protokoll `…/2026-09-26_G7a_gbXML-Export.md` |

Parallel abgeschlossen: G3 und G4b (Sitzung G3), G4c, G4a, E43 und E47 (Sitzung G4). Aktuelle
Basis `Referenzlaeufe/2026-09-26_R20_Zapfprofil`, vierzehn Projekte, CI rechnet sechs. Zuletzt
belegt: Entscheid E51 (G3), Nachtrag N1.58, Schemaschritt 148; der nächste freie Schritt ist 149 —
Nummern immer unmittelbar vor dem Eintrag gegen origin prüfen.

## 2. Offen beim Anwender

1. **Windows-Sichtabnahmen:** G6a und G6b (Punkte im Block „Nach #538“ der
   [iOS-Statusdatei](../Status_iOS_Migration.md)), G7a („Nach #529“), E47 Baualtersklassen und
   Energiestandard — vor E47 die Datenbank sichern, Schritt 148 benennt Auslieferungssätze um.
2. **Wiki-Sammel-Upload 1.2.0.4**, einschließlich der Seite „Mehrzonenmodell“ (Anwender 26.09.2026);
   der Abschnitt zum gbXML-Export kommt erst mit G7b. Der Upload läuft über ein Bot-Passwort, das
   beim Anwender bleibt: Rechte „Grundlegende Rechte“, „Vorhandene Seiten bearbeiten“, „Seiten
   erstellen, bearbeiten und verschieben“, bei Bedarf „Geschützte Seiten bearbeiten“; Konto in der
   Gruppe Bot. Die Vorbereitung des Upload-Skripts (Live-Abgleich, Upload, Nachprobe) wurde vom
   Anwender angehalten; dabei zeigte der Live-Stand bereits einen Upload vom 26.09.2026 um 13:01 UTC.
   Vor einem weiteren Upload deshalb je Seite den Live-Stand gegen die Repo-Quelle abgleichen und nur
   Fehlendes nachladen (Anmeldung mit `WIKI_BOT_USER`/`WIKI_BOT_PASSWORT` durch den Anwender).
3. **Vor jeder Auslieferung** den Schalter `GebaeudeExportRegeln.GbxmlExportFreigegeben` ausschalten,
   bis G7b folgt.

## 3. Nächste Stufen (jeweils auf Auftrag)

| Stufe | Inhalt | Voraussetzung |
|---|---|---|
| G6c | Zonenimport mehrerer Zonen aus IFC/gbXML | läuft bei der Sitzung G3 (E50, E51) |
| G6d | Referenzprojekt mit Zonen, Einfrierregel „gesäte Zonendaten“, neue Basis (2–3 PT) | G6b fertig |
| G7b | gbXML-Geometrie, Einstieg im Bedarfsdialog; danach Auslieferung von G7a | Probe 20 braucht einen macOS-Lauf, nur nach Rückfrage |
| KU3, AK2, AK3 | Kältemaschine und freie Kühlung; Erzeugerfahrplan; geschlossener Kreis | nach der Feldphase (E27) |
| GA | Altweg ablösen | Q24 Bedingungen (2) Feldphase und (4) Ausbauprobe |

## 4. Arbeitsweise, die sich bewährt hat

- Vor jeder Stufe ein Entwurf nur lesend (Leser, zwei Entwürfe, Gegenprüfung, Synthese mit
  Anwenderfragen), dann die Umsetzung als Agent im eigenen Worktree in Wellen.
- Haltepunkte H-S vor jedem Schemacommit und H-P vor jedem Papiercommit; die Nummern vergibt der
  Orchestrator nach Prüfung gegen origin. Schemanummern gelten nach „wer zuerst pusht“, eine
  Nummer wird nie übersprungen.
- Parallele Sitzungen per Nachricht abstimmen: belegte Dateien, Nummern, Merge-Reihenfolge.
- Merge → Gate (volles Test-Gate, Referenzlauf 14/14 byte-gleich) → Push → CI-Nachweis nach
  Commit-Kennung. Neue Tests mit `double` vergleichen mit Toleranz statt Stellenzahl
  (Linux und Windows weichen im letzten Bit ab).

## 5. Für einen anderen Computer: was nicht im Repository liegt

Das lokale Gedächtnis der bisherigen Sitzung und einige gitignorierte Dateien liegen nur auf dem
bisherigen Rechner. Auf einem neuen Rechner gilt:

- **Neu klonen, `git lfs install`**, dann `git lfs pull --include Referenzlaeufe/Kenndaten_Test.sqlite`;
  in jedem neuen Worktree ist die Testdatenbank zunächst ein Zeiger (`git lfs checkout …`).
- **Nicht im Repository, bei Bedarf neu beschaffen** (jeweils gitignoriert):
  - `Referenzlaeufe/Normzahlen/aixlib/` — AixLib-Normfälle für die lokalen Normproben (vom Anwender abgelegt);
  - `Referenzlaeufe/Schemakopien/GreenBuildingXML_Ver8.01.xsd` — für Probe 3 von G7a, Freigabe des
    Downloads liegt vor (F2, E48), Quelle GitHub `GreenBuildingXML/gbXML_Schemas`;
  - die VDI-6007-Quellen auf dem Netzlaufwerk `Y:` (nur lesend);
  - die Arbeitsdatenbank `%ProgramData%\EPOS_PLAN\Kenndaten.sqlite` samt `DB-Backup/` und die Ergebnisse
    des Bestandsvergleichs unter `%TEMP%\EPOS_Gebaeudevergleich\` bleiben auf dem bisherigen Rechner.
- **Arbeitsregeln, die bisher im Gedächtnis standen:**
  - Nach grünem Gate committen und pushen ohne Rückfrage; macOS-/iOS- und Setup-Läufe nur nach Rückfrage.
  - Beim Wochenkontingent von 80 % die Übergabe vorbereiten (alles committet und gepusht, Status und Übergabe nachgezogen).
  - Schemanummern und Entscheid-/Nachtrags-/Auftragsnummern erst unmittelbar vor dem Commit gegen origin
    prüfen; „wer zuerst pusht, behält die Nummer“, keine Lücke. Konflikt der Testdatenbank: origin-Fassung
    nehmen, eigene Schritte mit `Werkzeuge/Testdatenbankschema` bzw. `Referenzlaeufe/Skripte/*.py` neu anwenden,
    Zellvergleich.
  - CI-Nachweis über `gh run list --workflow kern.yml` nach `headSha`; ein abgebrochener Lauf ist kein Nachweis.
    Bei rotem Lauf zuerst prüfen, ob die verursachende Sitzung schon repariert.
  - Hauptbaum-Sperre `AGENT_LAEUFT` atomar anlegen (`set -o noclobber`) und erst nach **erfolgreichem** Push löschen.
  - Die Arbeitsdatenbank nur mit `immutable=1` lesen; schreiben nur auf ausdrücklichen Auftrag, vorher
    Sicherung per `VACUUM INTO` nach `DB-Backup/`, danach `quick_check` und `foreign_key_check`.
  - Anmeldungen mit Passwörtern (Wiki-Bot) führt der Anwender selbst aus.
  - Bash-Heredocs ab etwa 7 KB brechen ab; große Skripte mit dem Schreibwerkzeug anlegen.
