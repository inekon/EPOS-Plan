# KONTEXT — Lizenz-Zustimmung beim ersten Start (verdrahtet 29.08.2026)

## Auslöser

Bei der Erstellung der Wiki-Hilfeseite „Lizenz" (29.08.2026, Hilfesystem-Wikiumstellung) fiel
auf: Die Wiki-Seite „Installation und Update" beschreibt einen Zustimmungsdialog zur
Lizenzvereinbarung beim ersten Programmstart — im Code existierte
`Form_Lizenz.ZustimmungSicherstellen()` (`WindowsFormsApplication1\Views\Help\Form_Lizenz.cs`)
zwar, wurde aber nirgends aufgerufen. Denselben Befund führt
`KONTEXT_Designer_Migration_Dialoge.md` als Nebenbefund „toter Code".

## Befund zur Historie (Frage: verloren gegangen oder nie verdrahtet?)

**Nie verdrahtet.** Beweis über die Git-Historie (alle Branches):

- `git log --all -S "ZustimmungSicherstellen"` liefert genau zwei Commits:
  `52a805e` (27.07.2026, führt die Methode samt Einbau-Empfehlung im XML-Kommentar ein —
  „Aufruf beim Programmstart, zum Beispiel in Program.Main …") und `218616b` (20.08.2026,
  berührt nur die Datei selbst).
- In beiden Ständen wie in HEAD gibt es nur die zwei Fundstellen in `Form_Lizenz.cs`
  (Kommentar + Definition); `git log --all -S … -- WindowsFormsApplication1/Program.cs`
  ist leer. Ein Aufruf existierte also zu keinem Zeitpunkt in irgendeiner Datei.
- Der unmittelbar vorausgehende Stand `2679a0d` (27.07.2026, 01:07) enthält den Bezeichner
  noch gar nicht.

Eine bewusste Deaktivierung ist nirgends dokumentiert: `EPOS-Plan_Konzept_Lizenzierung.md`
kennt keine EULA-/Erststart-Regel (behandelt nur Aktivierung/Token), das
Designer-Migrations-Dokument führt die Stelle als Befund, nicht als Entscheid. Die Methode
war von Anfang an als Erststart-Pfad gebaut (eigener Abschnitt „Zustimmung beim ersten
Start", Zustimmen-/Ablehnen-Knöpfe über `Form_Lizenz(true)`, Ablage per
`ZustimmungMerken()`), der empfohlene Aufruf wurde nur nie gesetzt.

## Entscheidung und Einbau

Verdrahtet in `Program.Main` (`WindowsFormsApplication1\Program.cs`), **nach** der
ACE-Provider-Prüfung und **vor** `SchemaMigration.Ausfuehren`:

```csharp
if (!Form_Lizenz.ZustimmungSicherstellen()) return;
```

- **Nach der ACE-Prüfung:** Eine nicht startfähige Installation braucht keine Zustimmung.
- **Vor der Schema-Migration:** Wer ablehnt, dessen `Kenndaten.accdb` wird nicht angefasst
  (die Migration ist der erste Schreibzugriff im Startpfad).

Verhalten (unverändert, wie von der Methode vorgesehen):

- Einmal **je Windows-Benutzer**: Ablage `HKCU\Software\wp-plan\LizenzZugestimmt`
  (Programmversion + Datum). Bestandsanwender ohne diesen Wert sehen den Dialog beim
  nächsten Start genau einmal (nachgeholte Zustimmung — gewollt).
- „Ablehnen" beendet das Programm, bevor Fenster geöffnet oder Daten geschrieben werden.
- Fehlerpfad defensiv: Ist die Registry nicht lesbar, blockiert der Start **nicht**
  (`catch { return true; }` in `ZustimmungSicherstellen`).

## Beweis

x64-Build erfolgreich (29.08.2026, VS-MSBuild, `WP-Plan.sln` Debug|x64):
`WindowsFormsApplication1 -> bin\x64\Debug\net8.0-windows\WindowsFormsApplication1.dll`,
keine Fehler, nur die bekannten Altwarnungen (CS0108/CS0109/CS1998).

## Folge für die Wiki-Dokumentation

Die Aussage der Wiki-Seite „Installation und Update" (Zustimmungsdialog beim ersten Start)
ist ab diesem Stand zutreffend — keine Korrektur der Seite nötig. Feinheit, falls dort
präzisiert werden soll: Die Zustimmung gilt je Windows-Benutzerkonto (HKCU), nicht je
Rechner, und wird bei einem Update nicht erneut abgefragt (der Registry-Wert bleibt
bestehen; er hält die Version fest, mit der zugestimmt wurde).
