# Protokoll AS2: Stufe 2 Administration und Support — Fernwartung unter Windows

**Auftrag des Anwenders (11.10.2026):** Stufe 2 des Konzepts umsetzen. Konzept:
[`Konzept_Administration_Support_EPOS-Plan.md`](../../../aktuell/Konzept_Administration_Support_EPOS-Plan.md),
Entwicklerseite:
[`Entwicklerhandbuch_Administration_Support_EPOS-Plan.md`](../../../aktuell/Entwicklerhandbuch_Administration_Support_EPOS-Plan.md).
Statuszeile #969. Zweig `claude/gracious-brown-qx5kb2`, Stand `bb594b7a` (Wiki-Merge `01125073`).
Kein Schemaschritt, kein Rechenweg (kein Referenzlauf nötig). iOS bleibt zurückgestellt.

## 1 Zuschnitt

Zwei Wellen in eigenen Worktrees: Welle 2a (Opus 5.5, Zweig `as2-code`) baut Kern, Menü, Dialog,
Hülle und Windows-Fenster; Welle 2b (Sonnet 5.5, Zweig `as2-wiki`) schreibt die Wiki-Quellen.
Vertrag zwischen Kern und Oberfläche: `LizenzCtrl.FernwartungErlaubt()` liefert
`Fernwartungsfreigabe(Erlaubt, Grund)`; die Hülle setzt daraus den Sperrgrund am Menüpunkt und
die Freigabe im Dialog.

## 2 Umnummerierung und origin-Merge

- `03db01f5`: Umnummerierung der eigenen Statuszeilen #958–#960 auf **#966–#968**, weil andere
  Sitzungen #958–#965 vergeben hatten.
- `0a7115a9`: Merge von `origin/ios_migration_september` (brachte Schemaschritt 216 und die
  Testdatenbank neu); Konflikt nur in der Statusdatei.

## 3 Welle 2a — Kern, Menü, Dialog (`as2-code`)

- `18904d9d` Lizenzbedingung im Kern und Ressourcen: `EPOS.Kern/Allgemein/Support/Fernwartung.cs`
  (Konstante `SEITE` = `https://epos-plan.de/support/fernwartung`, Record
  `Fernwartungsfreigabe(Erlaubt, Grund)`); `LizenzCtrl.FernwartungErlaubt()` ohne Nebenwirkung, die
  Regel als `internal FernwartungErlaubt(token, status)`; `LizenzManager.AnkerOhneSchreiben()`;
  23 neue Schlüssel je Sprache im Block „Administration und Support, Stufe 2 (Welle 2a)"
  (`FW_*`, `SUP_KONTAKT_TELEFON/MAIL/ZEITEN`, `MENU_FERNWARTUNG`), Designer neu erzeugt;
  `FernwartungFreigabeTests` (13 Fälle).
- `3e710091` Menüpunkt und Menüsperre: `Menuepunkt.Sperrgrund` und `MitSperrgrund`,
  `Menuetabelle.Sichtbar(stufe, sperrgrund)`, Punkt `MenuItem_Fernwartung` unter Hilfe nach
  „Dokumentation", `Seitenschluessel.Fernwartung`; `Menueband.razor` zeichnet den gesperrten Punkt
  (Klasse `epos-menue-gesperrt`, `aria-disabled`, Grund als `title`, Klick löst nichts aus), Stil in
  `epos-ui.css`; `MenuebandTests` +5 Fälle; Menübaum 79 Punkte (vorher 78), Anwenderbaum 71
  (vorher 70, `EntwicklerbereicheWacheTests`).
- `e4d9dd36` Dialog, Hülle, Windows-Fenster: `FernwartungDialog.razor` mit `FernwartungDaten.cs`
  (`FernwartungTexte`), `EPOS.UI.Daten/Hilfe/FernwartungHuelle.cs`,
  `WindowsFormsApplication1/Views/Help/FernwartungFenster.cs`, Fall `Fernwartung` in
  `HauptfensterHuelle` (Sperrgrund aus der Freigabe, Menü nach dem Lizenzdialog neu versorgt);
  `FernwartungDialogTests` (6 Fälle).
- Merge `bb594b7a`.

## 4 Welle 2b — Wiki-Quellen (`as2-wiki`)

`e40ea99a`: neue Quelle `Projekte/Wiki/Programm Dokumentation - Fernwartung.wiki`, Abschnitt
„Unterstützung" in `Installation und Update.wiki`, Software-Service in `Über EPOS-Plan.wiki`, Zeile
`Programm Dokumentation/Fernwartung` in `Werkzeuge/WikiUpload/seiten.tsv`,
`Form_Fernwartung.btn_Help = Fernwartung#fernwartung` in `help_mapping.txt`. Merge `01125073`.

## 5 Abweichungen vom Auftrag

- Das Windows-Fenster liegt im Ordner `Views/Help` (dort liegt schon `LizenzHuelle`).
- `LizenzManager.AnkerOhneSchreiben()` ist neu: nur so wird `UhrManipuliert` erkannt, ohne die
  Lizenzablage zu schreiben.
- Zwei Sperrfälle zusätzlich zum Konzept: unbekannter Lizenztyp, Token eines fremden Geräts.
- Telefon in en-US mit Landesvorwahl („+49 711 9959 826 50"), in de „0711 9959 826 50".
- Der Knopf „Fernwartungsseite öffnen" ist auch ohne vorhandenen Rückruf zum Öffnen der Adresse
  gesperrt (sichtbar bleibt er, der Grund steht darüber).
- Die Wiki-Seiten verweisen auf `Programm Dokumentation/Fernwartung` statt auf einen Kurzlink.

## 6 Gate (Orchestrierung)

Das Gate lief am 11.10. im Hauptbaum nach dem Merge beider Wellen (`bb594b7a`), über den SDK-Umweg (10.0.112 statt 10.0.400) und mit der echten Testdatenbank:

| Prüfung | Ergebnis |
|---|---|
| `WP-Plan.Kern.slnf` Release | 0 Fehler, 155 Warnungen (Bestand) |
| Windows-Schale auf Linux (`EnableWindowsTargeting`) | 0 Fehler, 16 Warnungen (Bestand) |
| `EPOS.UI.Tests` | 8825 von 8825 |
| `KiKern.Tests` | 553 von 553 |
| `SpeicherEngine.Tests` | 397 von 397 |
| `SpeicherPlanung.Tests` | 27 von 28, 1 übersprungen (Bestand) |
| `EPOS.Kern.Tests` nach Filter (Fernwartung, Lizenz, Menue, Hilfe, Ressource, Wache, Entwickler, Diagnosedaten, Dokumentation, Wiki, HelpMapping, Auslieferungskennzeichen) | 1009 von 1010, rot: `KiDialogaufrufTests.Jeder_Hilfeschluessel_des_Bestands_findet_einen_Bereich` |

Die Wache verlangt für jeden Hilfeschlüssel aus `help_mapping.txt` einen Bereich in `KiChatKontext.BEREICH_JE_HILFEPRAEFIX`; der neue Schlüssel `Form_Fernwartung.btn_Help` hatte keinen. Behebung `964d9c10`: eine Zeile, Bereich Hilfe wie `Form_KiChat`. Danach sind `KiDialogaufrufTests`, `KiEntwicklerbereichTests` und die `HelpMapping`-Wachen mit 46 von 46 grün. Der volle Kern-Lauf (rund 57 Minuten) ist die CI; Push-SHA und CI-Kennung stehen im Statuspunkt „Nach #969“ (e).

## 7 Umgebung

Nur SDK 10.0.112 statt 10.0.400 installiert; Bau über den Helfer außerhalb des Repositoriums.

## 8 Offene Punkte

- Sichtabnahme des Anwenders unter Windows (Menüpunkt gesperrt/frei je Lizenzlage, Dialog, Knopf
  öffnet die Seite, Kopieren); Probelauf des Dialogs im eigenen Fenster mit `fensterprobe.mjs`
  nicht gezogen (der Dialog nutzt nur die bestehenden Klassen `epos-dialog-kopf`, `epos-leiste`).
- Betrieb bei INEKON (Anwender, Konzept 4.4/4.8): Fernwartungsseite unter
  `epos-plan.de/support/fernwartung`, Fernwartungsserver, gestalteter und signierter Client; ohne
  Seite führt der Knopf ins Leere.
- Wiki-Upload gebündelt (neue Seite Fernwartung, „Installation und Update", „Über EPOS-Plan") mit
  Logbuch-Satz; Versionsnummer beim Anwender erfragen.
- Stufe 3 (Dialog „Über EPOS-Plan", die Geste zieht dorthin) auf Zuruf; Nachzug Zapfprofil-Rubrik
  offen (Status, „Nach #968" (f)).
- Word-Fassung des Entwicklerhandbuchs neu erzeugen (eigener Auftrag); Push-SHA und CI-Kennung nach
  dem Push in der Statuszeile nachtragen.
