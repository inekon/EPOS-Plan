# Protokoll AS1: Stufe 1 Administration und Support — Entwicklermodus

**Auftrag des Anwenders (11.10.2026):** „Starte Stufe 1." Konzept:
[`Konzept_Administration_Support_EPOS-Plan.md`](../../../aktuell/Konzept_Administration_Support_EPOS-Plan.md),
Entwicklerseite:
[`Entwicklerhandbuch_Administration_Support_EPOS-Plan.md`](../../../aktuell/Entwicklerhandbuch_Administration_Support_EPOS-Plan.md).
Statuszeile #974. Zweig `claude/gracious-brown-qx5kb2`, Stand `5eb9e5f2`. Kein Schemaschritt, kein
Rechenweg (kein Referenzlauf nötig).

## 1 Zuschnitt und Verträge

Drei Opus-5.5-Agenten in eigenen Worktrees, in drei Wellen: Welle 0 legt den Kern (Verträge für die
anderen), Welle 1a Menü, Marke und Schale, Welle 1b das Verbergen der Bereiche. Vertrag zwischen den
Wellen: `Entwicklermodus.Aktiv` und `Freigabestufe` aus dem Kern; die Oberfläche bekommt das
Kennzeichen als Parameter aus den Hüllen, nie direkt aus der Registry.

## 2 Welle 0 — Kern (`as1-kern`, `31570bae` Kern, `55f65a71` Tests)

- `EPOS.Kern/Allgemein/Freigabe/Freigabestufe.cs` (Anwender/Entwickler), `Entwicklermodus.cs`:
  Quellen in Rangfolge — Sperre `HKLM\Software\wp-plan\EntwicklermodusGesperrt` über `LiesMaschine`,
  Umgebungsvariable `EPOS_ENTWICKLER`, Startargument `--entwickler`, Einstellung `Entwicklermodus`
  unter HKCU; Mitglieder `Stufe`, `Aktiv`, `Quelle`, `Gesperrt`, `QuelleText`, `Ermitteln`,
  `Einschalten`, `Ausschalten(out grund)`.
- `Entwicklerbereiche.cs`: neun Schlüssel mit Zweck.
- `EPOS.Kern/Allgemein/Diagnose/Diagnosedaten.cs`: 14 Zeilen (Version, Plattform, Sprache,
  Datenbankpfad, Schemastand „{0} (Ziel {1})", Katalogfassung, Lizenztyp, -nummer, Firma, Status,
  gültig bis, Gerätekennung-Kurzform mit acht Hexzeichen, Protokollordner, Entwicklermodus); jede
  Zeile in eigener Fehlerschranke; Lizenzstatus ohne Uhrprüfung und ohne Nebenwirkung.
- 30 Ressourcenschlüssel `ENTW_*`/`DIAG_*`.
- Tests: `EntwicklermodusTests` (9), `EntwicklerbereicheTests` (2), `DiagnosedatenTests` (4) — 15 grün.

## 3 Welle 1a — Menü, Marke, Schale (`as1-menue`, `8b918f8e` Menü, `6e4712ac` Schalen, `7e093588` Tests)

- `Menuepunkt` mit Parameter `stufe`; `Menuetabelle.Sichtbar(Freigabestufe)` liefert je Stufe eine
  zugeschnittene Kopie, einmal gebildet.
- Menükopf „Entwicklung" vor „Sprache" mit sieben Punkten (Ausnahmeprotokoll öffnen,
  Protokollordner öffnen, Datenbankordner öffnen, Diagnosedaten kopieren, Entwicklerwerkzeuge der
  Oberfläche, Auslieferungsstand wiederherstellen…, Entwicklermodus ausschalten); sechs neue
  `Seitenschluessel`.
- Marke „Entwickler" neben der Versionszeile in `Hauptfenster.razor` (Parameter `EntwicklerMarke`,
  `EntwicklerMarkeTip`, `FreigabeAnmelden`; Stil mit `--epos-warn-text/-flaeche/-rahmen`).
- Schale: `Program.Main(string[] args)`, `Entwicklermodus.Ermitteln(args)` nach der Belegung der
  Dienste; `HauptfensterHuelle` liefert `Eintraege = Menuetabelle.Sichtbar(Stufe)` und die Marke;
  `Views/Entwicklung/EntwicklungHuelle.cs`; `BlazorSeite.EntwicklerwerkzeugeOeffnen()`; Fenstertitel
  „(Entwicklermodus)"; Geste **Strg+Umschalt+Alt+E** in `Hauptfensterrahmen.ProcessCmdKey` mit
  Rückfrage; Ein- und Ausschalten versorgen Menü und Marke sofort neu (`FreigabeAnmelden`).
- iOS: `MauiProgram.cs` ruft `Entwicklermodus.Ermitteln(Array.Empty<string>())` (nicht gebaut).
- Zahlen: vollständiger Baum 78 Punkte, 15 Trenner, 63 handelnd; Anwenderbaum 70/13/56 wie vorher.
- Tests: 119 gefilterte grün, `EPOS.UI.Tests` 8614 grün, Kern nach Filter 150 grün; neue Wache
  `EPOS.UI.Tests/Bausteine/EntwicklerbereicheWacheTests.cs`, bunit-Proben in `HauptfensterTests`.
- Abweichungen: Geste im Hauptfenster statt im Dialog „Über EPOS-Plan" (der ist bis Stufe 3 ein
  Meldungsfeld); kein `ENTW_MARKE_TIP` (Tooltip = `QuelleText`); Vorgabe von `Hauptfenster.Eintraege`
  ist `Sichtbar(Anwender)`; das Argument „wiederherstellen" wird durchgereicht, bewirkt nichts;
  `help_mapping.txt` nicht nachgezogen (keine Wache verlangt es).

## 4 Welle 1b — Bereiche verbergen (`as1-bereiche`, `8ec5af9b` Umsetzung, `6361a90f` Tests)

- Das Kennzeichen kommt aus den Gaben der Hüllen (`Entwicklermodus.Aktiv`); die Elemente tragen
  `data-stufe="entwickler" data-bereich="<SCHLÜSSEL>"`.
- Gebäudeimport `ProjektdateiFreigegeben` (Quellen, Dazuladen, Entfernen, Zonierungswahl, Profile
  übernehmen; gelesener Stand bleibt sichtbar; `*.sqproj` ohne Modus aus dem Dateifilter);
  Nutzungsprofile `RaumnutzungWeg.ProjektdateiFreigegeben`; iOS `Dateifilter.cs` ohne `.sqproj`
  ohne Modus (nicht gebaut).
- KI: `KiDialoge.NUR_ENTWICKLER` (Gebäudeimport, Feld `zonierung`), Absage
  `KI_ABSAGE_NUR_ENTWICKLER` in `KiAktionenDialog.FeldzugangGrund`.
- Katalogabgleich (Parameter `Entwickler`, Wiederherstellen); KI-Chat und KI-Einstellungen
  (Parameter `Entwickler`: Werkzeuge…, Protokoll anzeigen, Modell neu erkennen, Weg B);
  Gebäudeexport `ZusageFreigegeben` (IDS).
- Einstellungsdialog: Parameter `Entwickler`, Reiterblatt `ENTWICKLUNG`, fünf Textfelder der
  Zapfprofil-Vorgaben (`EinstellungenCtrl`: `Zapf*`-Felder, `ZapfprofilVorgabenLesen` immer,
  `…Schreiben` nur im Modus, leer = Schlüssel löschen; „Zurücksetzen" lässt sie stehen).
- `BlazorDienste.WebviewWerkzeugeFreigegeben` (Debug oder Modus). 14 Ressourcen.
- Tests: `EPOS.UI.Tests` 8612 grün (12 neu), Kern nach Filter 529 grün; neu `KiEntwicklerbereichTests`
  (3) und die Wache `EPOS.UI.Tests/Bausteine/EntwicklerbereicheQuellenWacheTests.cs`; elf bestehende
  Testklassen setzen das Kennzeichen in ihren Render-Helfern.
- Nebenbefund: Die Schlüsseltexte `Zapfprofil.Katalogordner`/`Importordner` stehen doppelt (Kern
  `EinstellungenCtrl` und `EPOS.UI.Daten/ZapfprofilHuelle`).

## 5 Merge

- `d39e5b83`: origin/ios_migration_september (51 Commits, #948–#957 anderer Sitzungen). Die eigenen
  Statuszeilen #948/#949 dieser Sitzung wurden auf **#972/#973** umnummeriert.
- `01dc4314`: Welle 1a. Ressourcen beider Seiten vereinigt; Git legt das gemeinsame schließende
  `</data>` hinter den Block, deshalb beide Seiten plus ein `</data>`; Designer neu erzeugt.
- `5eb9e5f2`: Welle 1b. Konflikte mit AH-V5 im Einstellungsdialog und -controller beidseitig
  übernommen; Eingabebilanz der KI-Maskenwache für den Einstellungsdialog 14 + 5 = 19.

## 6 Gate (Orchestrierung, `5eb9e5f2`)

- Kern-Filter 0 Fehler (155 Warnungen, vorher 151); Windows-Schale auf Linux 0 Fehler (16 Warnungen);
  SQL-Dialekt-Prüfer 2 813 Texte, 0 Fundstellen; keine Konfliktmarker.
- Testlauf des Gates: `EPOS.UI.Tests` 8 779 grün, `KiKern.Tests` 553, `SpeicherEngine.Tests` 397, `SpeicherPlanung.Tests` 27 (1 übersprungen), `EPOS.Kern.Tests` nach Filter (Entwickler, Diagnose, Ki, Einstellungen, Resource, HelpMapping, Dokumentation, RepositoryOrdnung, Wiki, GebaeudeImport, Raumnutzung, Katalogabgleich, GebaeudeExport, Lizenz) 1 359 grün in 4 min 44 s — alles mit SDK 10.0.112; der vollständige Kern-Lauf ist der CI-Lauf
- Referenzlauf nicht betroffen (kein Rechenweg).
- CI: Push `5eb9e5f2` läuft; der Lauf auf `6f166a81` war rot durch die fremde Einstufung von
  `Vorheizzeit_H` (AH-V6 auf origin bringt die Reparatur, mit dem Merge übernommen).
- Nachtrag: gepusht `5876851d`, Kern-Lauf 38134668642 (kern.yml, ubuntu) grün, abgeschlossen 12:21 UTC.

## 7 Umgebung

Nur SDK 10.0.112 statt 10.0.400 installiert; Bau über den Helfer `/home/user/dn.sh` außerhalb des
Repositoriums; die Testdatenbank kam per `git lfs pull`.

## 8 Offene Punkte

- Sichtabnahme des Anwenders unter Windows (ohne Modus kein Projektdatei-Knopf und kein Kopf
  „Entwicklung"; mit `--entwickler` beides und die Marke; Geste mit Rückfrage; HKLM-Sperre schlägt
  alles; Entwicklerwerkzeuge); Tastenfolge bei Fokus in der WebView2 ungeprüft.
- iOS-Hülle berührt (`EPOS.iOS/MauiProgram.cs`, `Dienste/Dateifilter.cs`, nicht gebaut): ein
  `ios.yml`-Lauf wäre begründet, nur nach Rückfrage beim Anwender.
- Stufe 2 (Fernwartung) nach der Zuarbeit aus Konzept 4.8; Stufe 3 (Dialog „Über EPOS-Plan", die
  Geste wandert dorthin).
- Nebenbefunde: doppelte Ordner-Schlüssel des Zapfprofils; Warnungen 151 → 155 (Herkunft prüfen);
  `help_mapping.txt` ohne die neuen Menüpunkte; ältere Zahlen in Code-Kommentaren („54 Punkte" in
  `Hauptfensterrahmen.cs`).
- Word-Fassung des Entwicklerhandbuchs neu erzeugen (eigener Auftrag); Push-SHA und CI-Kennung nach
  dem Push in der Statuszeile nachtragen.
