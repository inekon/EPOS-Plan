# KIF12 — „Unverändert“ als Erfolg, Ausweichnamen der Parameter (Protokoll, 10.10.2026)

Statuszeile #915 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Konzept
[`Konzept_KI-Assistent_Aufgabensteuerung.md`](../../../aktuell/Konzept_KI-Assistent_Aufgabensteuerung.md) (Abschnitt 3.5, Punkt 6
„Nichts zu ändern“); Vorgänger [`KIF11_Maske_beim_Anzeigenamen_Protokoll.md`](KIF11_Maske_beim_Anzeigenamen_Protokoll.md). Zweig
`claude/assistent-unveraendert` (Opus), Commit `63b08a17`.

## Anlass

Anwendermeldung vom 10.10.2026 mit Bildschirmfoto: Im Dialog „Wärmepumpen Verwaltung“ (Maske „Wärmepumpe im Projekt“, Stand Vorlauf 55 °C,
Rücklauf 45 °C) tippte der Anwender „setze die Vorlauftemperatur auf 55°C und die Rücklauftemperatur auf 45°C“. Runde 1: Das Modell schickte
den Parameter `mask` statt `maske`: „Den Parameter „mask“ kennt diese Aktion nicht“. Runde 2: `formular_ausfuellen` mit
`maske: "Wärmepumpe im Projekt"`, `vorlauf=55; ruecklauf=45` ergab rot „Aktion nicht ausgeführt … tragen alle genannten Felder den Wert bereits;
es gibt nichts zu ändern“ (Status abgelehnt). Die Maskenauflösung aus #911 griff; der Anwender las die rote Absage als Fehlschlag.

## Befund

Der Fall „Wert steht schon“ lief über die Ablehnung (`MehrfeldGrund`, `ReihenGrund`), `feld_setzen` hatte die Prüfung gar nicht. Das Modell
nutzt gelegentlich Synonyme für Parameternamen; die Prüfung lehnte jeden unbekannten Namen ab.

## Änderungen je Datei

- `KiKern/KiAktion` und `KiAusfuehrung`: neue optionale Prüfung `OhneAenderung` nach der Vorbedingung; liefert sie einen Text, gibt
  `VorbereitenAsync` direkt `Ausgefuehrt` mit `Anzahl` 0 und Merkmal `KiErgebnis.Unveraendert` (`KiErgebnis.OhneAenderung(text)`) zurück —
  ohne Vorschau, Freigabe und Klick, Protokollzeile „ausgeführt, 0x“; auch der Lauf nach dem Klick prüft sie erneut.
- Gilt für `feld_setzen`, `formular_ausfuellen`, `reihe_setzen`; `MehrfeldGrund`/`ReihenGrund` lehnen den Fall nicht mehr ab. Gemischter Fall:
  `formular_ausfuellen` setzt und zählt nur die sich ändernden Felder, die übrigen stehen als „Trugen den Wert bereits: …“ im Ergebnis.
- `KiVerlaufstexte.Schritte`: der Fall erscheint neutral in der Rolle Assistent, nicht rot. Rückgabe an das Modell
  `{"status":"ausgefuehrt","anzahl":0,"text":"…"}`.
- `KiKern/KiPruefung.cs` (Schritt 0 von `Pruefe`): Ausweichnamen-Tafel (unten).
- Konzept Aufgabensteuerung 3.5 Punkt 6.

## Texte (beide Sprachen)

`KI_DLG_OHNE_AENDERUNG` („In „{0}“ tragen die Felder bereits die genannten Werte: {1}. Keine Änderung nötig.“), `KI_DLG_REIHE_OHNE_AENDERUNG`
(nennt die Maske), neu `KI_DLG_FELD_OHNE_AENDERUNG`, `KI_DLG_FELDER_BEREITS`, `KI_AKT_UNVERAENDERT` („Unverändert: {0} — {1}“).

## Ausweichnamen-Tafel

`mask`, `form`, `formular`, `dialog` → `maske`; `field` → `feld`; `value` → `wert`; `values`, `fields` → `werte`; `start`, `from` → `ab`;
`button` → `knopf`. Greift nur, wenn die Aktion den Zielnamen kennt und den Ausweichnamen nicht selbst deklariert; ein gesetzter Zielname
gewinnt; jeder andere unbekannte Name bleibt benannt abgelehnt.

## Tests

- `EPOS.UI.Tests/KiFeldSetzenTests`: `Feld_setzen_mit_dem_Stand_meldet_unveraendert_ohne_Bestaetigung`,
  `Formular_ausfuellen_mit_dem_Stand_meldet_unveraendert_ohne_Bestaetigung`, `Feld_setzen_mit_neuem_Wert_bleibt_bestaetigungspflichtig`.
- `WaermepumpeAnlageDialogTests`: `Der_Assistent_meldet_unveraendert_wenn_Vorlauf_und_Ruecklauf_schon_stehen` (spielt die Meldung mit `mask`
  nach), `Der_Assistent_setzt_nur_das_geaenderte_Feld`.
- `EPOS.Kern.Tests/KiVerlaufstexteTests.Unveraendert_erscheint_neutral_und_nicht_als_Absage`.
- `KiKern.Tests/KiPruefungTests` (4): `Ausweichname_greift_fuer_mask_und_values`, `Unbekannter_Name_bleibt_benannt_abgelehnt`,
  `Zielname_gewinnt_gegen_den_Ausweichnamen`, `Ausweichname_ohne_Zielparameter_bleibt_unbekannt`; angepasst
  `KiReiheSetzenTests.Ohne_Aenderung_gibt_es_nichts_zu_bestaetigen`.
- Filterlauf: KiKern 553, EPOS.UI 1 035, EPOS.Kern 724, SpeicherEngine 1, 0 rot; Build 0 Fehler. Gate ⟨Zahlen folgen⟩.

## Offen

1. „Bereits gesetzt“ entscheidet ein Textvergleich: „55 °C“ gegen „55“ zählt als Änderung und geht in die Bestätigung.
2. Die Werkzeugrückgabe trägt kein eigenes Feld `unveraendert`; das Modell erkennt den Fall am Text — im echten Gespräch nicht erprobt.
3. `KiVorbereitung.Ablehnung` heißt weiter so, trägt hier aber einen Erfolg.
4. Ausweichnamen `start`/`from` → `ab`, `button` → `knopf` gehen über den Anlass hinaus.

## Verweise

Statuszeile #915 und Nach #915; KIF11 ([`KIF11_Maske_beim_Anzeigenamen_Protokoll.md`](KIF11_Maske_beim_Anzeigenamen_Protokoll.md)); Konzept
Aufgabensteuerung 3.5; Wiki-Quelle `Projekte/Wiki/Programm Dokumentation - Hilfe-Assistent.wiki`.
