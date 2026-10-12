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

## NP7b — Dialog „Normwerte einlesen…“

| Teil | Inhalt |
|---|---|
| Blatt „Nutzungsprofile“ | unter dem Katalogbaum, nur bei gewählter Kategorie DIN: „Normwerte einlesen…“ und „Eingelesene Werte entfernen…“ (weich gesperrt ohne eingelesene Profile, Rückfrage mit Zahl, Vorgabe „Nicht entfernen“); danach liest das Blatt neu, zieht den Entwurf des gewählten Profils nach und nennt die Zahl in einer leisen Zeile |
| `NormwerteEinlesenDialog` | Überlagerung im Blatt (Titel und Kreuz an der Überlagerung, `TitelText=""`; allein gezeichnet mit eigenem Titel und Kreuz), Hilfepille `Nutzungsprofile.dlg_Normwerte` → Abschnitt `#normwerte-einlesen`; Knöpfe „CSV-Datei wählen…“, „Projektdatei wählen…“, „Vorlage speichern…“; Vorschau als Tabellen „Treffer“ (Nummer, Profil, Stand ohne Werte / Werte vorhanden / eingelesene Werte vorhanden, abweichende Spalten), Zählung, Abweichungen je Spalte, „Nicht übernommen“ (unbekannte Nummer, ohne Wert, doppelte Nummer, Meldung); Schalter „vorhandene Werte überschreiben“ (aus); `SpeichernLeiste` mit OK „Übernehmen“ (weich gesperrt mit Grund) und Abbrechen |
| Hülle `RaumnutzungNormwerteHuelle` (`EPOS.UI.Daten`) | Dateiwahl über `Dienste.Datei`, CSV über `RaumnutzungNormwerte.AusCsv`, Projektdatei über `ProjektdateiProfileHuelle.LesenAsync` mit der Grenze der Plattform, geschrieben wird nur die zuletzt geladene Vorschau; Vorlage über `Vorlage()` (auf iOS danach Teilen); `null` ohne Schritt 220 |
| Kern | `RaumnutzungNormwerte.Vorschau` führt `OhneWerte` und `Doppelt` eigens (die Meldungen bleiben vollständig); kein Schemaschritt |
| Texte | 40 Schlüssel `RNP_NW_*` in beiden Sprachen, `Resource.Designer.cs` neu erzeugt |
| Assistent, Wache | Wissensabschnitt an den Dialog angeglichen (Knöpfe nur an DIN, Vorlage im Dialog, Schalter, Tabelle „Nicht übernommen“, Rückfrage beim Entfernen); `KiMaskenabdeckungWacheTests`: Wirt `GebaeudeKatalogDialog`, eine Eingabestelle (Schalter) mit Vermerk — Angabe der Handlung „Übernehmen“, kein Einstellwert |
| Wiki-Quelle | `Programm Dokumentation - Nutzungsprofile.wiki`: Abschnitt „Normwerte einlesen“ (Anker `normwerte-einlesen`), Anker `csv-format`, Hinweise am Katalogbaum, Außenluft je Person im Profileditor; Gegenlesemuster ohne Treffer |

**Prüfung:** `WP-Plan.Kern.slnf` Release und die Windows-Schale (Linux, `EnableWindowsTargeting`) mit 0 Fehlern.
`EPOS.UI.Tests` gefiltert (Normwerte, Raumnutzung, Nutzungsprofil, Ki, Wache, Ressource, Stilblatt, Hilfe, Fensterrahmen,
Schliesskreuz, Ueberlagerungstitel): 1 217 grün, davon 13 neu in `NormwerteEinlesenTests` (ohne Gaben, Vorschau, Überschreiben
aus und an, Abbrechen, Kreuz, Ablehnung, Vorlage, nur an DIN, Überlagerung mit Meldung, Entfernen mit Rückfrage, Texte de/en).
`EPOS.Kern.Tests` gefiltert (Raumnutzung, Nutzungsprofil, Ki, Wache, Ressource, Hilfe, HelpMapping, Wissen): 1 431 grün, davon
1 neu (doppelte Nummern und Profile ohne Wert, Hülle). Dialogkopf und Schlussleiste unverändert gegenüber den
Nachbardialogen (Überlagerung, kein eigenes Fenster) — `fensterprobe.mjs` nicht gezogen.

**Logbuch-Entwurf** (nicht hochgeladen): „Im Blatt Nutzungsprofile liest ‚Normwerte einlesen…‘ die Werte der Kategorie
DIN/TS 18599-10 aus einer eigenen CSV-Datei oder einer Projektdatei ein; ‚Eingelesene Werte entfernen…‘ nimmt sie zurück.“

**Windows-Sichtabnahme:** Knöpfe erscheinen nur an der Kategorie DIN und brechen die Knopfzeile unter dem Baum sauber um;
die Überlagerung im breiten Maß, Tabellen „Treffer“ und „Nicht übernommen“ rollen in ihrem Rahmen; Dateiwahl CSV und
`.sqproj` mit den richtigen Filtern; „Vorlage speichern…“ schlägt `Normwerte_Vorlage.csv` vor; Esc und Kreuz schließen
ohne zu schreiben; die Marke „eingelesen“ steht nach „Übernehmen“ sofort an Liste und Editor; Rückfrage beim Entfernen.

## Offen

- Zuordnung der HottCAD-Nummern 70 und 71 auf die Wohnprofile beim Einlesen aus der Projektdatei (heute nur über den
  Namen); Entscheid des Anwenders.
- Wiki-Upload der Seite „Nutzungsprofile“ und des Logbuch-Satzes mit dem nächsten gebündelten Upload.
