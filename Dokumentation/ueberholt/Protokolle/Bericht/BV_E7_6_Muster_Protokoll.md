# BV-E7-6 — Muster im Vorlagenordner und Export der Standardvorlage (Protokoll)

Nachzug zur Etappe BV-E7 des Konzepts
[`Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md`](../../../aktuell/Konzept_Berichtsvorlagen_Platzhalter_EPOS-Plan.md)
(Abschnitte 6.3, 10.2, 10.3 und 14). Auftrag #556, Anwenderaufträge vom 26.09.2026: „Die Berichtsvorlage sollte
zugänglich sein als Template zu eigenen Vorlagen … im Verzeichnis aus den Einstellungen (Bericht)
C:\Users\Dirk\Documents\EPOS-Plan\Berichtsvorlagen“ und „Die Word-Vorlage Standard (EPOS-Plan) soll auch in das
Vorlagenverzeichnis exportiert werden können (als Beispielvorlage)“. Der gültige Stand steht im Konzept (Rev. 10) und in
der [Statusdatei](../../../aktuell/Status_iOS_Migration.md). Vorgänger:
[`BV_E7_Excel_Rahmen_Protokoll.md`](BV_E7_Excel_Rahmen_Protokoll.md). Zweig `claude/intelligent-bohr-hthrk8` ab
`eaa9d6f3` (#549); der Agentenzweig `worktree-agent-ad48c52f10c4d944f`, umgesetzt am 26.09.2026 von einem Agenten
(Opus 5.5) im eigenen Worktree, zusammengeführt, geprüft und beschrieben von einem Abschluss-Agenten. Kein
Schemaschritt, kein Rechenweg (Referenzlauf GESAMT: PASS), keine Referenzbasis neu eingefroren. `EPOS.iOS/MauiProgram.cs`
ist berührt.

| Teil | Gegenstand | Commits |
|---|---|---|
| Muster im Vorlagenordner | `BerichtsvorlagenCtrl.MusterBereitstellen` (`BerichtsvorlagenCtrl.Muster.cs`), Aufruf beim Start beider Schalen und nach dem Ordnerwechsel, Ressourcen `BV_MUSTER_*`, Tests `BerichtsvorlagenMusterTests` | `0039a690` |
| Wiki | Abschnitt „Muster im Vorlagenordner“ der Seite „Berichtsvorlagen“ | `c24ee299` |
| Export | Menü „…“ der Standardvorlage: „In den Vorlagenordner exportieren…“, `BerichtsvorlagenCtrl.Exportieren` | `d8406184` |
| Abschluss | Merge (konfliktfrei), zweimal Merge `origin/ios_migration_september` (#551 konfliktfrei; #546, #552, #554, #555 mit Schemaschritt 150 und neuer Testdatenbank, konfliktfrei), Gate, Papiere | `eb857789`, `3c40f752`, `71fdf279` |

---

## 1 Muster im Vorlagenordner

- **Entscheid BV-E7-6 (Anwender 26.09.2026):** Unterordner `Mitgeliefert` im Vorlagenordner, von EPOS aktuell gehalten,
  dazu Baukasten und Excel-Standardmappe.
- **Inhalt (sieben Dateien):** `Berichtsvorlage_Standard.docx`, Kurzbericht de/en (aus `IPfade.Berichtsvorlagen`),
  `Berichtsvorlage_Baukasten.docx` und `…_en.docx` (aus dem Katalog), `Berichtsvorlage_Excel_Standard.xlsx`
  (Standardmappe mit Blattmarken), `LIESMICH.txt` zweisprachig.
- **Aktualisierung:** geschrieben nur bei geändertem Inhalt; `Inhaltsschluessel` ist ein SHA-256 über die Paketteile
  ohne Zeitstempel und core-Eigenschaften, Beziehungs-Kennungen des SDK normiert. Schreiben über eine Zwischendatei und
  `File.Move`; die Dateien sind schreibgeschützt, eine schreibgeschützte alte Fassung wird ersetzt. Fremde Dateien im
  Unterordner und alles im Vorlagenordner selbst bleiben unberührt; die Vorlagenliste liest nur die oberste Ebene.
- **Befund statt Ausnahme:** `Musterbefund` mit `Musterzustand` je Datei (geschrieben, gleich, Quelle fehlt, Fehler).
- **Aufruf** im Hintergrund über `Kulturweitergabe`: `WindowsFormsApplication1/Program.cs`, `EPOS.iOS/MauiProgram.cs`
  (Schritt 3b) und `EinstellungenBerichtGaben.OrdnerSetzen`.

## 2 Export der Standardvorlage

- Menü „…“ der mitgelieferten Word-Vorlage: **„In den Vorlagenordner exportieren…“** mit Namensfrage (Vorschlag
  „Beispiel – Standard“); es entsteht eine bearbeitbare Kopie unter den eigenen Vorlagen, gewählt bleibt die aktuelle
  Vorlage. Danach unter Windows „Im Ordner zeigen“, ohne Ordnerweg (iOS) das Teilen-Blatt.
- `BerichtsvorlagenCtrl.Exportieren` teilt mit `NeueVorlage` den Weg `KopiereMuster`; Seite `Handlung.Namensvorschlag`,
  `Benannthandlung`, `HANDLUNG_EXPORTIEREN`; Ressourcen `BV_VORLAGEN_EXPORTIERT`, `BK_BER_VORLAGE_*EXPORT*` (de/en).

## 3 Nachweis

Tests: `BerichtsvorlagenMusterTests` (11), ein Hüllenfall in `BerichtsvorlagenHuelleTests`, ein bunit-Fall in
`BerichtSeiteVorlagenTests`. Gate auf dem Merge-Stand: Kern-Filter Release und Windows-Schale (Linux,
`EnableWindowsTargeting`) je 0 Fehler, voller Testlauf, Designer wiederholbar, SQL-Prüfer ohne Fundstelle, Referenzlauf
der sechs CI-Projekte gegen R21 `2026-09-26_R21_BhkwDeckung` GESAMT: PASS; Zahlen in der Statuszeile #556.

## 4 Offen

(a) Anwenderprobe unter Windows (Ordner `Mitgeliefert` nach Programmstart, Export, Schreibschutz beim Kopieren im
Explorer — die `LIESMICH.txt` erklärt ihn); (b) iOS am Gerät: Schreibschutz in der Dateien-App, iOS-Lauf auf
Anwenderwunsch zurückgestellt; (c) der Explorer öffnet nach dem Export ohne Rückfrage; (d) Menü „…“ mit Export für
Excel-Vorlagen folgt mit dem Excel-Menü (BV-E8/später); (e) Wiki-Upload „Berichtsvorlagen“ und Logbuch.
