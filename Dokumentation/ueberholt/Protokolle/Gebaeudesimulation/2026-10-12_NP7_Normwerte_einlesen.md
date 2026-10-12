# NP7 — Normwerte der DIN einlesen (12.10.2026)

Auftrag aus Entscheid E131 (Anwender 12.10.2026): Die Kategorie „DIN/TS 18599-10“ des Katalogs der Nutzungsprofile bleibt
ohne Werte ausgeliefert; „Normwerte einlesen“ füllt sie in der Datenbank des Anwenders aus seiner CSV oder einer
HottCAD-Projektdatei (Weg 2), eine Anweisung an den Assistenten führt durch das Übertragen aus der eigenen Normausgabe
(Weg 3). Konzept: [Nutzungsprofile](../../../aktuell/Konzept_Nutzungsprofile_EPOS-Plan.md) 4.1, 4.3, 6.4, 6.5.

## Entscheidung zur Einheit

Außenluft je Person steht als **eigene Spalte** `Aussenluft_Person` (m³/(h·Person)) und nicht als dritte Einheit von
`Aussenluft_Einheit`. Begründung: Die Tabelle der Norm führt die Außenluft je Person und je Fläche **nebeneinander**; mit
einer dritten Einheit könnte ein Profil nur einen der beiden Werte tragen, und eine Projektdatei mit beiden Angaben verlöre
einen. Dazu bleibt der Schritt ein reines `ADD COLUMN`; der Neubau der STRICT-Tabelle samt `CHECK` entfällt. Vorrang im
Generator: die raum- oder flächenbezogene Außenluft geht vor, die Außenluft je Person gilt nur ohne sie.

## Gebaut

| Teil | Inhalt |
|---|---|
| Schemaschritt 220 (`RaumnutzungNormwerteSchema`) | `Aussenluft_Person` REAL ≥ 0, `Werte_Herkunft` (`NULL`/`'EINGELESEN'`), `Werte_Quelle` (≤ 400), `Werte_Datum` (`JJJJ-MM-TT`), alle nullbar mit `CHECK`; eingehängt in `SchemaMigration`, `Werkzeuge/Testdatenbankschema`, Testvorrichtung und Paketanhebung (Art Katalog); Testdatenbank auf 220 gehoben (LFS) |
| Kern `RaumnutzungNormwerte` | Vorschau aus CSV oder Projektdatei, Zuordnung über DIN-Nummer und Namen der Wohnprofile, Abweichungen je Spalte, Einlesen mit Überschreiben nur auf Wunsch, Herkunft, Rücknahme, CSV-Vorlage ohne Werte |
| `RaumnutzungCtrl`, `Raumnutzungsprofil`, CSV | die vier Spalten gelesen und geschrieben (nur wenn der Schritt steht), Prüfung ≥ 0, CSV-Spalte `Aussenluft_Person` hinter `Aussenluft_Ausserhalb` |
| Projektdatei | `MinimumExternalAirFlowBasedOnPersons` wird `Aussenluft_Person`; die Meldung nennt nur noch die fehlende Belegung |
| Generator | Außenluft je Person ÷ Fläche je Person ÷ lichte Höhe = 1/h; neuer Hinweis `LueftungOhneBelegung` statt stiller Annahme |
| Auslieferungsvorlage | Schritt 3d entfernt eingelesene Normwerte, Prüfberichtzeile „eingelesene Normwerte entfernt: n Profil(e) der DIN“; die Prüfung der Normkategorie zählt die neuen Spalten mit |
| Oberfläche | Profileditor: Feld „Außenluft je Person“, Marke „eingelesen“ in Liste und Editor (Quelle, Tag); Feld im Assistenten (`np_luft_person`) |
| Weg 3 | Wissensabschnitt des Assistenten: Vorlage über den CSV-Export der leeren Kategorie, Spalten und Einheiten, Zuordnung über die Nummer; der Assistent nennt selbst keinen Normwert |

Keine Normwerte in Code, Tests, Proben, Protokoll und Wiki — Tests mit runden Phantasiewerten. Die Projektdatei des
Anwenders wurde nicht benutzt.

## Offen (NP7b)

- Der Dialog „Normwerte einlesen…“ am Blatt „Nutzungsprofile“ (Dateiwahl CSV oder Projektdatei, Vorschau, Rückfrage
  „überschreiben“, „eingelesene Werte entfernen“) als Razor-Komponente mit Hülle in `EPOS.UI.Daten` und bunit-Test; der
  Kern liefert alle Wege. Der Wissensabschnitt des Assistenten nennt den Knopf schon — vor einem Push gehört NP7b dazu.
- Zuordnung der HottCAD-Nummern 70 und 71 auf die Wohnprofile beim Einlesen aus der Projektdatei (heute nur über den
  Namen); Entscheid des Anwenders.
- Wiki-Quelle und Logbuch-Entwurf mit dem Dialog.
