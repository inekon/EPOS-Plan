# KI‑F4 — Kosten und Wirtschaftlichkeit für den Hilfe-Assistenten (Protokoll, 21.09.2026)

Statuszeile #423 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Konzept
[`Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md`](../../../aktuell/Konzept_KI-Assistent_Dialogintegration_EPOS-Plan.md)
(Stufe S4, KI‑D‑Q5 und KI‑D‑Q6); Vorgänger [`KIF3_Bedarf_Klima_Protokoll.md`](KIF3_Bedarf_Klima_Protokoll.md),
[`KIF2_Simulationskonfiguration_Protokoll.md`](KIF2_Simulationskonfiguration_Protokoll.md),
[`KIF1b_Wahlfelder_Protokoll.md`](KIF1b_Wahlfelder_Protokoll.md), [`KIF1_Erzeugermasken_Protokoll.md`](KIF1_Erzeugermasken_Protokoll.md).
Zweig `ki-f4`, Commits `c2ba9efd`, `132cb62e`, `0399cbaa`, `0d1af723`; Merge `3f097b12`.

## Die Masken

| Katalogschlüssel | Dialog | Bindung | Felder (gesamt / nur lesbar / Wahl) | Haken | Ziel | dialog_oeffnen |
|---|---|---|---|---|---|---|
| `Form_Energietraeger` | Energieträgerverwaltung mit den Bausteinen Einstellungen, Strompreis-Details, Brennstoffbestandteile | `EnergietraegerKiSicht` | 45 / 2 / 3 | Auffrischen, Speichern | `ENERGIETRAEGER_VERWALTUNG` | beide Plattformen benannt abgelehnt |
| `Form_Kosten_Auswahl` | Energieträger Variante | `EnergietraegerVarianteKiSicht` | 2 / 0 / 1 | Auffrischen | `ENERGIETRAEGER_VARIANTE` | iOS ja, Windows benannt abgelehnt |
| `Form_LeistungspreisReihe` | Saisonale Leistungspreis-Sätze | `LeistungspreisReiheKiSicht` | 3 / 2 / 0 | Auffrischen | `ENERGIETRAEGER_VERWALTUNG` | beide abgelehnt |
| `Form_Kostenprofil` | Kostenprofil | `KostenprofilKiSicht` | 3 / 1 / 1 | Auffrischen | `ENERGIETRAEGER_VERWALTUNG` | beide abgelehnt |
| `Form_KostenAdmin` | Administration Kostenfaktoren | `KostenfaktorKatalogKiSicht` | 2 / 0 / 1 | Auffrischen | `KOSTENVERWALTUNG` | beide abgelehnt |
| `Form_Emissionskatalog` | Emissionsfaktor-Katalog | `EmissionskatalogKiSicht` | 12 / 0 / 3 | Auffrischen | `ENERGIETRAEGER_VERWALTUNG` | beide abgelehnt |
| `Form_Nutzungsdauer` | Nutzungsdauern | `NutzungsdauerKiSicht` | 8 (5 Kopf, 3 Spalten) / 0 / 1 | Auffrischen | `NUTZUNGSDAUER_VERWALTUNG` | beide abgelehnt |
| `Form_VorlagenPosition` | Position bearbeiten (Kostenvorlage) | `VorlagenPositionKiSicht` | 6 / 0 / 2 | Auffrischen | `KOSTENVERWALTUNG` | beide abgelehnt |
| `Form_CaseEingabe` | Eingabe Worst/Best Case | `CaseEingabeKiSicht` | 7 / 0 / 0 | Auffrischen | `KOSTENVERWALTUNG` | beide abgelehnt |
| `Form_WirtschaftlichkeitParameter` | Wirtschaftlichkeitsparameter | `WirtschaftlichkeitParameterKiSicht` | 26 / 0 / 3 | Auffrischen | `BERICHTE_KOSTEN` | beide ja |
| `Form_BhkwWirtschaftlichkeit` | BHKW-Wirtschaftlichkeit | `BhkwWirtschaftlichkeitKiSicht` | 25 / 0 / 9 | Auffrischen | `BHKW_WIRTSCHAFTLICHKEIT` | iOS ja, Windows benannt abgelehnt |
| `Form_Tarifstruktur` | Tarifstruktur | `TarifstrukturKiSicht` | 28 / 0 / 3 | Auffrischen | `BERICHTE_KOSTEN` | beide ja |
| `Form_PhotovoltaikVerguetung` | Photovoltaik-Vergütung | `PhotovoltaikVerguetungKiSicht` | 16 / 0 / 4 | Auffrischen | `BERICHTE_KOSTEN` | beide ja |
| `Form_Gesetzesparameter` | Gesetzliche Parameter (Katalog) | `GesetzeskatalogKiSicht` | 2 / 0 / 2 | Auffrischen | `GESETZESKATALOG` | beide abgelehnt |
| `Form_GesetzparameterZeile` | Gesetzliche Parameter (Zeile) | `GesetzeskatalogZeileKiSicht` | 7 / 0 / 3 | Auffrischen | `GESETZESKATALOG` | beide abgelehnt |
| `Kostenseite` | Seite Kosten (Berichte & Kosten) | `KostenSeiteKiSicht` | 3 / 2 / 1 | Auffrischen | `BERICHTE_KOSTEN` | beide ja |
| `Wirtschaftlichkeitsseite` | Seite Wirtschaftlichkeit (Berichte & Kosten) | `WirtschaftlichkeitSeiteKiSicht` | 6 / 0 / 5 | Auffrischen, Schreibgeschützt, Speichern | `BERICHTE_KOSTEN` | beide ja |
| `Kostenverwaltung` (Bestand) | Kostenverwaltung | `KostenKomponenteStand` | 8 → 9 / 5 / 2 | unverändert | unverändert | unverändert |

Katalog 34 → 51 Masken, 330 → 522 Felddeklarationen. Alle 17 neuen Masken binden über eine
Sichtklasse, stehen mit Begründung in `OhneMarkupprobe` und tragen je einen bunit-Zeugen.

## Entscheidungen

Eigene Katalogschlüssel bekamen nur Masken mit eigenem Fenster oder Dialog. Die Bausteine
Einstellungen, Strompreis-Details und Brennstoffbestandteile gehen in keinem eigenen Fenster auf
und sind Felder der Energieträgerverwaltung (daher 45); `VorlagenZeile` steht bereits als Spalten
der Kostenverwaltung, `ErtragBonus` ist deren Reiterblatt „Ertrag", die zwei Editoren des
Emissionskatalogs sind Überlagerungen in ihm. Die Seite „Berichte & Kosten" trägt nur, welches
Reiterblatt vorn steht (Navigation, kein Einstellwert) und bekam keinen Schlüssel; ihr
Seitenschlüssel ist das Öffnungsziel der Blätter Kosten und Wirtschaftlichkeit. Die
Navigationsziele nutzen ausschließlich bestehende Schlüssel (`Seitenschluessel.*`,
`Ansichten.BerichteKosten`); keine Plattformschale wurde angefasst, `dialog_oeffnen` lehnt
benannt ab, wo ein Ziel auf der Plattform nicht erreichbar ist (je Konstante dokumentiert).

In der Kostenverwaltung ist das Wahlfeld `variante` ergänzt, nur lesbar, weil ein Wechsel einen
anderen Positionssatz nachlädt; seine Einträge kommen als benannte Wahlquelle. Ausgenommen nach
KI‑D‑Q5: Vorlagen-Übernahme, Spotpreis-Import, Kapitalwertverlauf (Anzeige), die Knopfleiste.

## Bewusst nicht deklariert

Wertetafeln mit eigenem Editor: die zwölf Monatssätze der Leistungspreisreihe, die 36 Zellen des
Kostenprofils, die 24 Leistungsstufenzellen der Tarifstruktur, die Raster der Trägerkarte
(Emissionszeilen, Umrechnungsregeln, Preishistorie), die Katalogliste der gesetzlichen Parameter
und die Vergleichswahl der Seiten Kosten und Wirtschaftlichkeit (Menge von Verweisen).

Drei Lücken der Kostenverwaltung bleiben offen: Komponentenwahl, PV‑Wahl und PV‑Projekt des
Reiters „Ertrag" liegen in privaten Feldern beziehungsweise im Baustein `ErtragBonus`; sie zu
deklarieren hieße, die Maske von ihrem Daten-Objekt auf eine Sichtklasse umzustellen und dabei
ihre Markup-Probe aufzugeben — ein eigener Schritt (Statusdatei, Nach #423).

## Zahlen und Abnahme

240 neue Ressourcenschlüssel je Sprache (zwei Vorlagen statt doppelter Schlüssel:
`KI_DLG_ET_AKTIV_VORLAGE` für die 13 Bestandteil-Schalter, `KI_DLG_BLOCK_VORLAGE` für
Bezug/Einspeisung/Reststrom und Best/Worst), Designer neu erzeugt und wiederholbar; 16 neue
bunit-Zeugen, Zählungen in `KiDialogkatalogTests` fortgeschrieben, `KiRegisterS3Tests` grün.
Worktree: Kern-Filter 0 Fehler, 10 239 Tests grün (1 übersprungen); 58 Dateien mit BOM und CRLF
geprüft. Gate im Hauptbaum auf `3f097b12`: siehe Statuszeile #423.
