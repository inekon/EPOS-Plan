# KIF11 — Maske beim Anzeigenamen im Hilfe-Assistenten (Protokoll, 10.10.2026)

Statuszeile #911 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Konzept
[`Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`](../../../aktuell/Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md)
(Abschnitt 3.4, Absatz „Die Maske beim Namen“); Vorgänger
[`KIF9_Setzer_Erzeugermasken_Protokoll.md`](KIF9_Setzer_Erzeugermasken_Protokoll.md) und
[`KIF10_Sperrgrund_Freigabe_Protokoll.md`](KIF10_Sperrgrund_Freigabe_Protokoll.md). Zweig `claude/assistent-maske` (Opus), Commit `6321fc0b`.

## Anlass

Anwendermeldung vom 10.10.2026 mit Bildschirmfotos: Im Dialog „Wärmepumpen Verwaltung“ (eingebettete Maske `Form_WP_Anlage`,
Anzeigename „Wärmepumpe im Projekt“) lehnte der Assistent „setze Vorlauftemperatur auf 50°C und Rücklauftemperatur auf 45°C“ ab.
`formular_ausfuellen` mit `maske: "Wärmepumpe im Projekt"` meldete: „Die Maske „Wärmepumpe im Projekt“ ist für den Assistenten nicht
freigegeben. Freigegeben sind: … Wärmepumpe im Projekt … (82)“; ebenso `dialog_oeffnen`. Forderung: alle Eingabemasken für Anlagen und
Komponenten sollen per Assistent gesetzt werden können.

## Befund

Das Modell übernahm den Anzeigenamen aus dem Feldblock des Prompts („Werte der offenen Maske „Wärmepumpe im Projekt“ …“).
`KiDialogKatalog.Kennt` prüfte nur den Schlüssel (`Form_WP_Anlage`); `BrueckenGrund` und `ZielGrund` in `KiAktionenDialog` lehnten
deshalb ab und listeten die Anzeigenamen der Katalogmasken: 20 gezeigt, „… (82)“ = 102 Masken − 20 = Rest. Die Maske war freigegeben,
`vorlauf` und `ruecklauf` setzbar; der Build des Anwenders war aktuell. Kein Test hatte je einen Anzeigenamen als `maske` übergeben.

## Änderungen je Datei

- `KiKern/KiDialogKatalog.cs`: neue Methode `Aufloesen(genannt, out kandidaten)` — Schlüssel direkt, dann die Stufen von `KiWahl.Treffer`
  (Schlüssel buchstabengetreu, Schlüssel gefaltet, Anzeigename gefaltet, eindeutiger Anfang, eindeutig enthaltener Teil), zuletzt Vergleich
  ohne Bindestriche; mehrere Treffer ergeben Kandidaten „Anzeigename (Schlüssel)“.
- `EPOS.Kern/Allgemein/KI/Aktionen/KiAktionenDialog.cs`: `Maskenschluessel` gibt den aufgelösten Schlüssel an `dialog_lesen`, `feld_setzen`,
  `formular_ausfuellen`, `reihe_setzen`, `dialog_speichern`, `dialog_aktion_ausfuehren` und `FeldzugangGrund`; Helfer `AufgeloesteMaske` in
  `BrueckenGrund`/`ZielGrund`; `dialog_oeffnen` ruft `Aufloesen`; `Freigegeben()` ersetzt `Anzeigenamen()` — alle Listen (`MaskeUnbekannt`,
  `KeineOffen`, `dialog_oeffnen`) nennen „Anzeigename (Schlüssel)“; eine genannte, nicht offene Maske ergibt `MaskeNichtOffen` mit Zusatz
  `KI_DLG_OFFENE_MASKE`.
- `KiDialogTexte`: `MaskeNameMehrdeutig`, `OffeneMaske`. `KiMaskenbruecke.Dialogdaten`: Feldblock-Kopf nennt den Schlüssel.
- Werkzeugkatalog Weg A und B ziehen die Beschreibung des Parameters `maske` aus denselben Ressourcen.
- `EPOS.UI/Dialoge/Waermepumpe/WaermepumpenDialog.razor` (Randbefund): setzt `SatzArt`/`SatzName` der `Zweispaltenauswahl`; der Kopf zeigte „Kein Satz gewählt“.
- Konzept Dialogintegration 3.4: Absatz „Die Maske beim Namen“.

## Texte (beide Sprachen)

Neu: `KI_DLG_MASKE_NAME_MEHRDEUTIG` (Kandidaten mit Schlüssel), `KI_DLG_OFFENE_MASKE` („Geöffnet ist gerade die Maske „{0}“ ({1}).“).
Geändert: `KI_DIALOGDATEN_BLOCK_KOPF` („Werte der offenen Maske „{0}“ (Schlüssel {2}, {1} Felder):“), `KI_REG_ERL_MASKE` („Schlüssel oder
Anzeigename der Maske, z. B. „Form_WP_Anlage“ oder „Wärmepumpe im Projekt“. Ohne Angabe gilt die gerade geöffnete steuerbare Maske.“),
`KI_AKTION_ERL_MASKE_OEFFNEN`.

## Tests

- `EPOS.Kern.Tests/KiMaskenwegTests`: 6 neue Methoden mit 15 Fällen (`Der_Anzeigename_gilt_als_maske_fuer_jede_Formularaktion`,
  `Dialog_lesen_liest_die_Maske_beim_Anzeigenamen`, `Gefaltete_Namen_und_der_Schluessel_gelten_weiter`,
  `Ein_mehrdeutiger_Maskenname_nennt_die_Kandidaten_mit_Schluessel`, `Eine_unbekannte_Maske_nennt_die_Freigaben_mit_Anzeigename_und_Schluessel`,
  `Genannt_aber_nicht_offen_nennt_die_offene_Maske`); angepasst `Ein_unbekanntes_Feld_fuehrt_auf_die_Liste_mit_Anzeigenamen`.
- `KiRegisterS3Tests`: `Dialog_oeffnen_nimmt_den_Anzeigenamen` (2), `Dialog_oeffnen_nennt_die_Freigaben_mit_Anzeigename_und_Schluessel`.
- `KiMaskenbrueckeTests`: Feldblock-Kopf.
- bunit `EPOS.UI.Tests/Dialoge/WaermepumpeAnlageDialogTests.Der_Assistent_fuellt_die_Maske_beim_Anzeigenamen_aus` (vorher rot mit dem Text
  der Bildschirmfotos): Vorlauf 50, Rücklauf 45, Klappliste „50, 35, 45, 55“; Helfer `KiSetzweg.Ausfuehren`.
- Filterlauf: EPOS.Kern.Tests 699, EPOS.UI.Tests 1 010, KiKern.Tests 549, SpeicherEngine.Tests 1, 0 rot; Build 0 Fehler. Gate 911 auf `6321fc0b` grün: Kern-Build 0 Fehler; ChartProben alle grün, 222 Hashes gleich der Messlatte 2026-10-10; Tests 22 220 grün, 0 rot (EPOS.Kern.Tests 12 982 + 7 übersprungen, EPOS.UI.Tests 8 265, KiKern.Tests 549, SpeicherEngine.Tests 397, SpeicherPlanung.Tests 27 + 1 übersprungen); Dokumentationswachen 35 grün; Referenzlauf 28 von 28 PASS gegen R50 (9 686 136 Werte in Toleranz); Plattformnachweis (gestörter Lauf) PASS.

## Offen

1. Teiltreffer gelten jetzt auch für Maskennamen (ein eindeutiger Teilname öffnet die Maske) — gewollte Lockerung.
2. `KiWahl.Falte` lässt Bindestriche stehen; für Maskennamen fängt es der Rückfall ab, Feldnamen wie bisher.
3. `AktiveMaske` bei Überlagerungen: ohne `maske` gilt die zuletzt angemeldete; beim Schließen einer Überlagerung nach späterer dritter
   Anmeldung könnte eine andere als die sichtbare gelten — nicht geprüft; die genannte Maske hat jetzt Vorrang.
4. Die Freigabeliste zeigt 20 Einträge und „… (Rest)“, mit Schlüsseln länger.
5. Windows-Sichtabnahme durch den Anwender; Wiki-Upload mit dem nächsten Sammel-Upload; CI-Vermerk nach dem Push.

## Verweise

Statuszeile #911 und Nach #911; KIF9 ([`KIF9_Setzer_Erzeugermasken_Protokoll.md`](KIF9_Setzer_Erzeugermasken_Protokoll.md)); KIF10
([`KIF10_Sperrgrund_Freigabe_Protokoll.md`](KIF10_Sperrgrund_Freigabe_Protokoll.md)); Konzept Dialogintegration 3.4; Wiki-Quelle
`Projekte/Wiki/Programm Dokumentation - Hilfe-Assistent.wiki`.
