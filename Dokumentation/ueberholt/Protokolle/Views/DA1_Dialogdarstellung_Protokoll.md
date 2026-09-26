# DA‑1 — Dialogdarstellung: Projektkopfseite, Projektdialoge, Kachel „Zuletzt geöffnet“ (Protokoll, 26.09.2026)

Statuszeile #542 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md).
Zweig `ios_migration_september` im Hauptbaum; Commits `af52ed5d1` (Projektkopfseite),
`ad88cd2ec` (Projektdialoge) mit Merge `91869613e`, dazu die Papiere. Kein Schemaschritt,
Testdatenbank unberührt, kein Rechenweg berührt, kein Referenzlauf.

## Anlass

Drei Meldungen des Anwenders aus der Arbeit am Gerät (Windows, 26.09.2026): Die Projektkopfseite
des Assistenten zeigt die Felder weit auseinandergezogen, das dritte Feldpaar läuft aus dem Bild;
„Projekt öffnen“ und „Speichern unter“ öffnen fast bildschirmfüllend, der Inhalt steht im oberen
Drittel; die Kachel „Zuletzt geöffnet“ der Startseite führt nicht mehr auf das zuletzt geöffnete
Projekt.

## 1. Projektkopfseite des Assistenten (`af52ed5d1`)

**Ursache.** Der Hausraster legte auf dem breiten Assistenten drei Feldpaare nebeneinander, jede
Beschriftung in einer festen Spalte von 12 rem (`--epos-beschriftung-breite`): große Lücken
zwischen Beschriftung und Feld, das dritte Paar außerhalb des sichtbaren Bereichs.

**Änderung.**

- `EPOS.UI/Seiten/Assistent/ProjektKopfSeite.razor`: Einleitung, Raster, Hinweise und Beschreibung
  in einem Block `.epos-projektkopf-formular` (höchstens 60 rem, linksbündig unter dem breiten
  Gruppenkopf); Einleitungssatz und Pflichtlegende eng beieinander.
- `EPOS.UI/wwwroot/epos-ui.css`, nur unter `.epos-projektkopf`: genau zwei Spalten
  (Name/Klimaregion, Kunde/Bearbeiter, Änderungs-/Erstelldatum), Beschriftung über dem Feld und
  halbfett, Klappliste der Klimaregion ab linker Feldkante, gesperrte Datumsfelder als Anzeige
  (Seitenfläche, leise Schrift, gestrichelter Rahmen), unter 900 px eine Spalte. Hausraster und
  alle übrigen Masken unverändert.

**Abnahme.** Zwei Tests in `EPOS.UI.Tests/Seiten/ProjektKopfSeiteTests.cs` (Block und
Stilregeln); Gate unten.

## 2. Projektdialoge nach Inhalt (`ad88cd2ec`, Merge `91869613e`)

**Ursache.** `ProjektWahlHuelle` (Wunschmaß 760 × 560) und `ProjektKopieFenster` (940 × 660)
öffneten ohne Dialogart, also als Fachdialog. `Fenstermass.Vorgabe` nimmt für den Fachdialog das
Größere aus Wunschmaß und 85 % × 90 % des Arbeitsbereichs — auf 1920 × 1040 also 1632 × 896.

**Änderung.**

- `EPOS.UI/Dienste/Fenstermass.cs`: neue `Dialogart.Inhaltsmass` (wächst nicht mit dem Schirm,
  nur mit der Skalierung, gedeckelt bei 92 % des Arbeitsbereichs) und
  `Fenstermass.Projektdialog` = 1180 × (342 + 8 × 53) = 1180 × 766 CSS-Pixel, aus dem Layout
  abgeleitet.
- `BlazorDialogForm.Vorgabemass` reicht `DeviceDpi / 96` des aktiven Fensters als Skalierung
  durch.
- `ProjektWahlHuelle.cs` und `ProjektKopieFenster.cs` wünschen `Projektdialog` und öffnen als
  `Inhaltsmass`.
- `epos-ui.css`: eigener Block hinter `.epos-projektkopie-raster` — ist der Dialog Wurzel seines
  Fensters, nimmt er die volle Höhe, die Liste füllt die freie Höhe (mindestens drei Zeilen), die
  Knopfleiste steht unten; die Felder von „Speichern unter“ mit 9 rem Beschriftungsspalte. Die
  iOS-Wurzel (`AppWurzel`) ist unberührt.
- `WindowsFormsApplication1/CLAUDE.md`, Regel (f): Dialogart `Inhaltsmass` für Dialoge, deren
  Inhalt die Größe bestimmt.

**Abnahme.** `EPOS.UI.Tests/FenstermassTests.cs`: Maß, Schirmunabhängigkeit, Skalierung und
Deckel, dazu eine Wache über beide Hüllen; Gate unten.

## 3. Kachel „Zuletzt geöffnet“ — Untersuchung

**Befund: keine Regression.** Die Kachel liest Name und Id aus `Tab_Applikation`
(`ProjektKontextCtrl.ZuletztGeoeffnet`). Gemerkt wird ausdrücklich über
`ProjektKontextCtrl.ZuletztGeoeffnetMerken` durch die Kacheln „Projekt neu“, „Projekt öffnen“
und „Zuletzt geöffnet“ sowie den Rückweg aus dem Assistenten; der Variantenwechsel im Kopfband und
die Menüwege „Neu“/„Bearbeiten“ merken nicht (Bestandsverhalten, `ProjektKontextCtrl.Setzen`).
Wird das gemerkte Projekt gelöscht, leert `ProjektCtrl.LoeschenMitVorarbeiten` in Schritt 3 den
Merker (`Tab_Applikation.ID_Projekt = 0`, Name leer) — bewusst, damit die Kachel nicht auf ein
nicht mehr vorhandenes Projekt zeigt. Die Kachel fällt dann auf die Projektliste zurück
(`StartseiteHuelle.ProjektZuletzt`, zuletzt geänderte zuerst). Keine Codeänderung.

## Nebenbefund: KI-Tests schreiben in das Protokoll des Anwenders

`KiFeldSetzenTests` und `KiMaskenhakenTests` hängen Zeilen an die echte Datei
`%ProgramData%\EPOS_PLAN\ki_aktionen.txt`: `KiAusfuehrung.ProtokollPfad` bildet den Pfad aus
`DataRepository.GetDBPath()`, und die Tests setzen keine Pfadüberschreibung. Die Tests bleiben
grün; die Datei des Anwenders sammelt Testzeilen. Nicht behoben — Folgeauftrag.

## Zahlen und Abnahme

Gate auf `91869613e`, nacheinander:

| Schritt | Ergebnis |
|---|---|
| Kern-Filter Release | 0 Fehler, 56 Warnungen (Bestand) |
| Tests des Kern-Filters, voll | 0 Fehler: EPOS.Kern.Tests 8 208 erfolgreich / 1 übersprungen, EPOS.UI.Tests 6 558, KiKern.Tests 549, SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27 / 1 übersprungen — zusammen 15 728 erfolgreich, 2 übersprungen |
| Windows-Schale Debug x64 | kein Übersetzungsfehler; 10 Fehler allein im Kopierschritt (5 × MSB3021, 5 × MSB3027), weil die laufende `EPOS_Plan.exe` und Visual Studio den Ausgabeordner sperren — nicht behoben |
| Ressourcen-Designer (prüfen) | 12 329 Einträge, abweichend 0, neu 0; wiederholbar |

Kein Referenzlauf (kein Rechenweg berührt), kein iOS-Lauf (iOS-Hülle nicht berührt; der
Kern-Lauf auf ubuntu ist der Nachweis nach dem Push).

## Offene Punkte

1. **Rückfragen zur Kachel „Zuletzt geöffnet“ an den Anwender:**
   (a) Soll die Kachel nach dem Löschen des gemerkten Projekts auf das zuletzt geänderte Projekt
   zurückfallen, statt die Projektliste zu zeigen?
   (b) Sollen auch der Variantenwechsel im Kopfband und die Menüwege „Neu“/„Bearbeiten“ das
   Projekt als zuletzt geöffnet merken?
   (c) Soll die Kachel den Namen des gemerkten Projekts anzeigen?
2. **KI-Protokoll der Tests:** `KiFeldSetzenTests` und `KiMaskenhakenTests` auf eine
   Pfadüberschreibung (Testordner) umstellen, damit kein Test in `%ProgramData%\EPOS_PLAN`
   schreibt.
3. **Windows-Schale** nach dem Schließen der laufenden Anwendung und von Visual Studio neu bauen
   und die beiden Projektdialoge sowie die Projektkopfseite am Gerät ansehen.
4. **Logbuch-Vorschlag** (Version beim Anwender zu erfragen): „Die Dialoge ‚Projekt öffnen‘ und
   ‚Speichern unter‘ öffnen in der Größe ihres Inhalts.“
