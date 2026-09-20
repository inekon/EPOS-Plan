# KI‑F1 — Erzeugermasken des Projekts für den Hilfe-Assistenten (Protokoll, 20.09.2026)

Statuszeile #416 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Konzept
[`Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`](../../../aktuell/Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md)
(Stufe S4, KI‑D‑Q5) und
[`Konzept_KI-Assistent_Aufgabensteuerung.md`](../../../aktuell/Konzept_KI-Assistent_Aufgabensteuerung.md) 11.3–11.5.
Zweig `ki-f1`, Commits `01617a6d`, `a1224b3c`, `9104079d`, `3c2af2d9`; Merge `8c883202`.

## Anlass und Entscheid

Der Assistent sollte „die Vorlauftemperatur aller Heizkessel, bei denen sie nicht gesetzt ist,
auf 55 °C" setzen. `anlagen_auflisten` fand die Anlage, `dialog_oeffnen` lehnte ab: „Die Maske
‚Form_Heizkessel' ist für den Assistenten nicht freigegeben." Freigegeben waren sieben Masken
(Heizkessel bearbeiten, Photovoltaik, Pufferspeicher bearbeiten, Wärmepumpen verwalten,
Kostenverwaltung, Simulation, Stromspeicher-Auslegung). Eine Bestandsaufnahme zählte 119
Razor-Masken mit Eingabefeldern. Der Anwender entschied: alle Masken mit Einstellwerten
freigeben, in sechs Wellen, mit den genannten Ausnahmen (KI‑D‑Q5).

## Der Weg je Maske

Katalogeintrag (`KiDialoge.cs`: je Feld Schlüssel, Eigenschaftspfad am Daten-Objekt, Anzeigename
aus der vorhandenen Beschriftungsressource, Erläuterung als eigener Ressourcenschlüssel, Typ,
Einheit, `leerErlaubt`, `nurLesen`; Knöpfe als Positivliste), Konstante in `KiMaskennamen`,
Navigationsziel in `KiMaskenziele`, Anmeldung im Razor-Dialog über `KiMaskenanmeldung.Fuer`
mit `KiMaskenhaken` (Auffrischen, Prüfen, Schreibschutz, Speichern = der Speicherweg des Dialogs),
Wächter in `KiDialogkatalogTests` (jeder Feldpfad am Daten-Objekt und im Markup, Zählung) und
`KiRegisterS3Tests` (jede Katalogmaske hat ein Ziel), ein bunit-Fall je Maske.

## Die sieben Masken

| Katalogschlüssel | Dialog | Daten-Objekt | Felder (nur lesbar / Liste) | Knöpfe | Haken |
|---|---|---|---|---|---|
| `Form_Heizkessel` | Heizkessel im Projekt | `ErzeugerZeile` | 3 (1 / 0) | OK, Abbrechen | Auffrischen, Schreibschutz, Speichern |
| `Form_BHKWEing` | BHKW im Projekt | `ErzeugerZeile` | 4 (1 / 0) | OK, Abbrechen | Auffrischen, Schreibschutz, Speichern |
| `Form_PufferSp` | Pufferspeicher im Projekt | `ErzeugerZeile` | 1 (1 / 0) | OK, Abbrechen | Auffrischen, Schreibschutz |
| `Form_Stromspeicher` | Stromspeicher im Projekt | `ErzeugerZeile` | 1 (1 / 0) | OK, Abbrechen | Auffrischen, Schreibschutz |
| `Form_SolarKollektoren` | Solarkollektoren | `SolarkollektorenEingaben` | 5 (0 / 0) | Übernehmen, OK, Abbrechen | Auffrischen, Schreibschutz, Speichern |
| `Form_WP_Anlage` | Wärmepumpe Anlage | `WaermepumpeAnlageDaten` | 21 (2 / 0) | OK, Abbrechen | Auffrischen, Prüfen, Schreibschutz |
| `Form_PV` (erweitert) | Photovoltaik | `ErzeugerZeile` | 14 (0 / 7) | OK, Abbrechen | unverändert |

`Form_PufferSp` und `Form_Stromspeicher` tragen nur den Anlagennamen: Diese Masken führen keinen
Einstellwert der Anlage. Freigegeben sind sie trotzdem, damit `dialog_lesen` die gewählte Anlage
nennt und `feld_setzen` benannt ablehnt statt „Maske nicht freigegeben".

**Navigationsziel.** Die Projektmasken gehen aus der Erzeugerkarte der Startseite auf und
brauchen eine gewählte Anlage; Ziel ist die Startseite (`KiMaskenziele.STARTSEITE`, Wächter gegen
`Seitenschluessel.Startseite`). Ein Reiterwunsch ist über `OeffneMaske` nicht erreichbar — die
Wurzel setzt ihn allein aus ihrem Rückwegstapel. Unter Windows kennt die Navigation den Schlüssel
nicht, `dialog_oeffnen` lehnt benannt ab (wie bei der Stromspeicher-Ansicht); auf iOS wechselt die
Wurzel die Ansicht. Offen: ein Weg über die Wurzelseite zum Reiter Energieerzeuger.

**Zwei Eingriffe über das Anmelden hinaus.** Die fünf Zahlen der Kollektorgruppe lagen als
private Felder der Komponente und gingen erst mit „Übernehmen" in die Zeile; eine Setzung in die
Zeile wäre auf der Maske unsichtbar geblieben. Sie stehen jetzt in der Klasse
`SolarkollektorenEingaben` (`EPOS.UI/Dialoge/Solarthermie/SolarkollektorenDaten.cs`); das Markup
bindet dieselben Felder, der Speicherhaken ist der Weg des Knopfes. `KiMaskenwegTests`: `vorlauf`
und `ruecklauf` stehen an fünf Masken und sind mehrdeutig — die Absage rät nicht; der Befundfall
nimmt Felder, die es an genau einer Maske gibt, dazu ein eigener Fall für die Mehrdeutigkeit.

**Bewusst nicht deklariert.** Energieträger- und Brennstoffverweise (rohe Ids einer
kontextabhängigen Liste), der Aufklapper „Alle Daten" (Katalogspalten über ein Profil),
Aperturfläche (gerechnet aus Modulfläche und Anzahl), Modul und Gerät eines PV-Strangs, die
Wechselrichter-Überlagerung mit eigenem Arbeitsstand, die Kennlinientabelle der Wärmepumpe.

## Zahlen und Abnahme

52 neue Ressourcenschlüssel je Sprache, zwei neue Einheitenzeichen (`a`, `h/Tag`), 17 neue Tests;
Katalog 13 Masken. Worktree: Kern-Filter 0 Fehler, 10 056 Tests grün (1 übersprungen). Gate im
Hauptbaum auf `8c883202`: siehe Statuszeile #416.
