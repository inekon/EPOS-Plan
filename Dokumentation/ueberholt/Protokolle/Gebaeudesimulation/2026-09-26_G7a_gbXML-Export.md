# Protokoll G7a — gbXML-Export, Stufe 1: Daten ohne Geometrie (26.09.2026)

**Auftrag.** Stufe G7a der Gebäudesimulation: EPOS-Plan schreibt für **ein Projektgebäude** eine
gbXML-Datei ohne Geometrie — Zonen, Bauteilflächen, Öffnungen, Fenstertypen, die vollständige Kette
`Construction`/`Layer`/`Material` mit Schichtumkehr, eine gekennzeichnete Ersatzschichtung (D10) und
deterministische Kennungen (D5); die Datei ist schemagültig aus dem Code gebaut und im Test lokal
gegen die Schemakopie geprüft. Nach Export und Rückimport kommt jedes Feld auf 1e‑6 gleich zurück
oder steht in einer benannten Verlustliste. Kein Schemaschritt, keine Änderung der Testdatenbank,
keine Einfrierregel berührt; die Referenzbasis R19 bleibt byte-gleich, weil der Export nur liest.

**Anwenderentscheide vom 26.09.2026 (E48, Konzept N1.53):** **F1** — G7a wird jetzt gebaut, als
**Bauabweichung** von D2/E27, nicht als Auslieferungsabweichung: Die Funktion steht hinter dem
Freigabeschalter `GebaeudeExportRegeln.GbxmlExportFreigegeben`, der im Entwicklungsstand an ist und vor
jeder Auslieferung ausgeschaltet wird; ausgeliefert wird G7a samt Wiki und Logbuch erst mit G7b.
**F2** — die Schemakopie `GreenBuildingXML_Ver8.01.xsd` (387 450 Byte) liegt lokal unter
`Referenzlaeufe/Schemakopien/`, per `.gitignore` ausgeschlossen, mit LIESMICH (D17). **F3 = (a)** —
ein Gebäude ohne Zonen (Klassenweg) exportiert den Übernahmevorschlag, wie „Hülle und Zonen…" ihn
bildet, mit Faktor und Grundlage in `Campus/Description`.

## 1 Wellen

| Welle | Inhalt | Commits | Gate |
|---|---|---|---|
| W1 | Schemakopie lokal (`.gitignore`, LIESMICH); `GbxmlVokabular` (Tabellen des Lesers, Einheiten und Schemawerte als Konstanten); optionale Exportfelder am `GebaeudeAbbild`; Naht `IGebaeudeSchreiber` mit `GbxmlSchreiber`, `GebaeudeExportProfil`, `GebaeudeExportBilanz`, `GebaeudeExportKennung`, Freigabeschalter `GebaeudeExportRegeln`; Umkehrtabelle `GbxmlUmkehrung` (9 Bauteilarten × 5 Randbedingungen × 3 Neigungsklassen) | `65d538a43`, `c66c03d2f`, `02a7ab803`, `3c9e8f54e`, `9d84bce15`; Merge `c23299a73` | Tests 15 160, SQL 1 946/0, 14/14 byte-gleich |
| W2 | Trennfläche zur Nachbarzone in der Umkehrtabelle (Randkürzel `zo`, nach G6b W1); Gruppenkapazitäten als Hilfe am Ende von `ErsatzparameterRC` und `SimulationProtokoll.Stand/Herausnehmen`; Dateitexte `GEXP_DATEI_*`; `GebaeudeExportSatz` (Leseweg über die Controller des Laufs), `GebaeudeExportAblauf.Vorbereiten/Schreiben`, `GebaeudeExportPlan`, Verlustliste `GebaeudeExportVerluste` | `162061090`, `75c1308c9`, `a4e0f3ad8`, `1d4c056a6`; Nachzug `702edb0b4` (Energiestandard aus Schritt 148 als Verlust eingestuft) | Tests 15 343, SQL 1 981/0, 14/14 byte-gleich |
| W3 | Exportdialog `GebaeudeExportDialog` mit Textbündel und KI-Sicht, Hülle `GebaeudeExportHuelle`, Knopf „Exportieren (gbXML)…" im Gebäudedialog, Dateiweg über `Dienste.Datei`, 71 Texte `GEXP_*` in beiden Sprachen, Pflegestellen der Maske | `0ccb91a00`, `4eed73215` | Tests 15 474, SQL 1 988/0, 14/14 byte-gleich |
| W4 | Probe 3 (Schemaprüfung aller Probenausgaben, Messung des Inhaltsmodells), Papiere | `90f5dee98`, Papiere | siehe Abschnitt 6 |

**CI.** W1 ist grün im Kern-Lauf 36204063428 auf `5df980a3`; für W2 (`702edb0b4`) und W3
(`4eed73215`) ist die CI auf dem Arbeitszweig grün, die Läufe zeigen auf die Folgestände. Die
Schemaprüfung läuft in der CI nicht (die Schemakopie liegt dort nicht bei); dort decken die
Namensraum-Wache und die eigene Verweisprüfung (Probe 11) den Export ab.

## 2 Wie gebaut

**Architektur.** Die Hülle (`EPOS.UI.Daten/Bedarf/GebaeudeExportHuelle.cs`) liest den Satz einmal über
`GebaeudeExportSatz.Lesen` — im Kern, über die Controller des Laufs (`GebaeudeBedarfCtrl.Projektgebaeude`,
`GebaeudeZonenCtrl.LesenJeGebaeude` bzw. auf dem Klassenweg `GebaeudeZonenCtrl.Uebernahme`,
`BauteilaufbauCtrl.LesenJeProjekt`, `BaustoffCtrl.LesenProjekt`, Kühlschalter und Klimaregion) —
abseits des Oberflächenfadens (`Kulturweitergabe`). `GebaeudeExportAblauf.Vorbereiten(satz, profil)`
bildet daraus den `GebaeudeExportPlan` (Abbild, Meldungen, Ablehnung) und berührt die Datenbank nicht;
`Schreiben(plan, stream, profil, abbruch)` gibt ihn an den Schreiber des Profils und liefert eine
`GebaeudeExportBilanz` (Flächen, Öffnungen, Aufbauten, Ersatzaufbauten, Meldungen, Byte). Die Naht ist
`IGebaeudeSchreiber.Schreiben(GebaeudeAbbild, Stream, GebaeudeExportProfil, CancellationToken)` — der
Schreiber bekommt das **Abbild**, nicht den Satz; `ImportBilanz` passt nicht, ihre Zähler sind die eines
Katalogimports. Wirksame Werte kommen über dieselben Funktionen wie der Lauf
(`GebaeudeZonenabbildung`, `Bauteilreduktion.Uebergangswiderstaende`, die Gruppenkapazitäten aus
`ErsatzparameterRC`); eine zweite Herleitung gibt es nicht.

**Datei.** Wurzel `version="6.01"` (die Schemakopie führt 8.01 nicht; Kommentar im Quelltext), SI-Einheiten;
`Campus` mit `Name`, `Description`, `Location` (nur mit PLZ), `Building` samt `Space` und allen `Surface`
mit `Opening`; an der Wurzel `Construction`, `Layer`, `Material`, `WindowType`, `Zone` und
`DocumentHistory` (`ProgramInfo`, `PersonInfo` mit Programmname und Fassung, `CreatedBy` mit dem einzigen
Zeitstempel). UTF-8 ohne BOM, Zeilenende `\n`, Zahlen kulturfrei ohne Exponenten; geschrieben wird erst
in den Speicher, dann ganz — ein Fehler hinterlässt keine halbe Datei.

**Fachregeln.** Umkehrtabelle mit Zielwert und erwarteter Rückkehr je Zelle (gleich, benannter Wechsel,
Ablehnung); eine Trennfläche zur Nachbarzone ist eine Innenfläche mit dem Raum der Nachbarzone. Je
Aufbau und Übergangsfall (Richtung, Randbedingung) eine `Construction` mit dem U dieses Falls. Ruhende
Luftschicht als Dicke und Widerstand nach Tabelle 8 (Kandidat (i), Abschnitt 5); eine Luftschicht mit
äquivalentem λ unter 5 kg/m³ wird auf 5 kg/m³ angehoben. Ersatzschichtung für ein Bauteil ohne Schichten
mit U und der flächenbezogenen Kapazität, mit der die Gruppe im Lauf rechnet, in den Stoffwertbändern;
sonst masselos mit Meldung. Innenpaare gleicher Art, Fläche und (gespiegelten) Aufbaus werden eine Fläche
mit zweimal demselben Raum, eine Zeile ohne Partner halb und beidseitig. Öffnungen im Wirt gleicher
Zone, Randbedingung, gleichen Azimuts und gleicher Neigung, sonst im Ersatzwirt mit Meldung, sonst
Ablehnung. `Space`/`Zone` mit Fläche, Volumen, Infiltration, Personen, Geräteleistung als Mittelwert ohne
Zeitplan und Heizsollwert; gekühlt nur mit Projektschalter und Kühlschalter der Zone. `buildingType` aus
einer Datentabelle im Profil, sonst `Unknown` mit Meldung; Wasserzeichen der Testlizenz in
`Campus/Description`. Kennungen nur aus Schlüsseln und festen Kürzeln (Auftrag 2.2).

**Oberfläche.** Knopf „Exportieren (gbXML)…" (iOS: „Exportieren und teilen (gbXML)…") im Aktionsschlitz
des Gebäudedialogs neben „Hülle und Zonen…"; die Hülle reicht ihn nur bei angeschaltetem
Freigabeschalter; eine Zeile ohne Projektkopie ist weich gesperrt und nennt den Grund. Der Exportdialog ist
eine eigene Überlagerung: Format und Umfang als Text, PLZ freiwillig (Plan entprellt neu gebildet, ohne
Datenbank), Meldungsliste vor dem Schreiben, „Speichern…" weich gesperrt bis zur Bestätigung durch den
Anwender; eine Ablehnung nennt den Grund und trägt nur „Schließen". Windows: `DateiSpeichernAsync`;
iOS: Ablage in den Dokumenten mit Gebäude-ID im Dateinamen, danach Teilen über `MitSystemOeffnen`; die
Rückmeldung nennt den Pfad. Hilfe-Assistent: PLZ setzbar, Bestätigung nur zu lesen.

## 3 Abweichungen vom Auftrag und Festlegungen

Die Festlegungen der Umsetzung stehen gesammelt im Konzept N1.54 (benannt, nicht entschieden). Vom
Auftrag weichen ab:

- **ZONE-Spalte offen:** Der Auftrag sah eine Ablehnung vor, bis G6b `ID_Nachbarzone` bringt; G6b W1 war
  zu Beginn von W2 in origin, der Orchestrator hat die Öffnung freigegeben (≤ 1 PT).
- **Signaturen:** Softwarearchitektur 1.5 nannte zwei widersprüchliche Signaturen der Naht und des
  Ablaufs; gebaut ist die Fassung dieses Protokolls (Abschnitt 2), nachgezogen in 1.5 und 4.6.
- **Leseweg im Kern:** `GebaeudeExportSatz.Lesen` statt eines Lesewegs in der Hülle; die Hülle ruft ihn.
  `GebaeudeExportSatz.MitPlz` bildet den Plan zu jeder PLZ ohne zweites Lesen.
- **Oberfläche:** eine fünfte Pflegestelle (`KiChatKontext`, Bereich Gebäude); Hilfeschlüssel
  `GebaeudeExport.btn_Help` ohne Anker bis G7b; drei zusätzliche Texte (`GEXP_BTN_SCHLIESSEN`,
  `GEXP_VORBEREITUNG`, `GEXP_MSG_VORBEREITUNG_FEHLER`).

**Nebenbefund.** Die Schemakopie kennt als Dichteeinheit `GramsPerCubicCm`, der Importleser las
`KgPerCubicCm`; behoben im G4-Nachzug `5df980a3`, der Export schreibt `KgPerCubicM`.

## 4 Probe 3 — Schemaprüfung, gemessen am 26.09.2026

Gegen `Referenzlaeufe/Schemakopien/GreenBuildingXML_Ver8.01.xsd`, ohne Auflöser, nie aus dem Netz
(`EPOS.Kern.Tests/GbxmlProbe3Tests`). Alle acht Probenausgaben sind gültig, ohne Fehler und ohne Warnung:

| Probe | Byte | Space | Surface | Opening | Construction | Layer | Material | WindowType | Zone | Location |
|---|---|---|---|---|---|---|---|---|---|---|
| Bauteilweg mit Schichten, mit PLZ, mit Tür | 12 972 | 2 | 9 | 2 | 7 | 11 | 11 | 1 | 1 | ja |
| Bauteilweg mit Schichten, ohne PLZ, mit Tür | 12 917 | 2 | 9 | 2 | 7 | 11 | 11 | 1 | 1 | nein |
| U-Wert-Haus (Ersatzschichtung), mit PLZ | 11 494 | 1 | 7 | 1 | 7 | 7 | 7 | 1 | 1 | ja |
| U-Wert-Haus (Ersatzschichtung), ohne PLZ | 11 439 | 1 | 7 | 1 | 7 | 7 | 7 | 1 | 1 | nein |
| Klassenweg, mit PLZ | 13 471 | 1 | 7 | 4 | 7 | 7 | 7 | 4 | 1 | ja |
| Klassenweg, ohne PLZ | 13 416 | 1 | 7 | 4 | 7 | 7 | 7 | 4 | 1 | nein |
| Zwei Zonen mit Trennfläche | 14 475 | 3 | 10 | 2 | 8 | 11 | 11 | 1 | 2 | ja |
| Testlizenz, englisch | 13 093 | 2 | 9 | 2 | 7 | 11 | 11 | 1 | 1 | ja |

Der zweite Space der Bauteilwege ist der Platzhalter „unbeheizt" (`Unconditioned`, ohne Zone).
Gegenproben (W1): `version="8.01"` und eine Gebäudeart außerhalb von `buildingTypeEnum` fallen auf.

**Gemessen am Inhaltsmodell:**

| Messpunkt | Befund | Folge |
|---|---|---|
| Reihenfolge der Folgen | Jedes geschriebene Element (`gbXML`, `Campus`, `Building`, `Space`, `Surface`, `Opening`, `RectangularGeometry`, `Construction`, `Layer`, `Material`, `WindowType`, `Zone`, `ProgramInfo`, `PersonInfo`, `CreatedBy`) führt seine Kinder in einer unbegrenzten Auswahl (`choice[0..unbounded]`) | keine Reihenfolge, keine Pflichtkinder; die Folge der Datei ist frei gewählt |
| `Location` optional? | ja — ein Kind der Auswahl von `Campus`; steht es, verlangt es `ZipcodeOrPostalCode` | ohne PLZ entfällt `Location`; die PLZ bleibt freiwillig |
| Pflichtkinder `RectangularGeometry` | keine; `CartesianPoint` ist wählbar | kein Rückfall `CartesianPoint` im Ursprung nötig |
| `Opening/U-value` | zulässig (`U-value [0..1]`) | die Tür trägt ihr U an der Öffnung |
| Ort von `DocumentHistory` | Kind der Wurzel; seine Auswahl verlangt mindestens zwei Kinder | geschrieben: `ProgramInfo`, `PersonInfo`, `CreatedBy` |
| `buildingTypeEnum` | 35 Werte; die zehn Zielwerte der Profiltabelle und `Unknown` sind darin; das Vokabular führt genau diese 35 | die Tabelle im Profil ist gültig |
| `versionEnum` | führt `6.01`, nicht `8.01` | Wurzel `version="6.01"` |

## 5 Weitere Messungen

- **Umkehrtabelle:** 126 exportierbare Zellen über Schreiber, Leser und Bauteilvorschlag gemessen,
  0 Abweichungen von der erwarteten Rückkehr (W1: 108 Zellen, die ZONE-Spalte abgelehnt; mit W2 geöffnet).
- **Rundlauf (Probe 1):** 1a am Probenabbild, auch mit unsymmetrischer Schichtfolge (Innendämmung);
  1b über die Testdatenbank (Projekt 1039, Gebäude 10643, im Test aufgebaut, nicht gesät): alle Zeilen
  kommen wirksam gleich zurück (Art, Randbedingung, Fläche, Neigung, Azimut, U, g, Name), ebenso
  Infiltration, Tagessollwert und Fläche je Nutzer; 1c Importprobe → EPOS → Export → Import mit gleichen
  Flächen je Gruppe und gleichem Σ U·A.
- **Probe 11:** Kennungen stabil, auch nach Neuspeichern der Aufbauten (neue Schicht-Ids); ein
  Projektduplikat trägt andere. **Probe 12:** feste Uhr gibt dieselben Bytes, zwei Uhren unterscheiden sich
  an genau einer Stelle, Deutsch und Englisch schreiben dieselben Zahlen.
- **Luftschicht-Rückweg:** Kandidat (i) — Dicke und Widerstand nach Tabelle 8, Name gleich dem Bezeichner
  — kehrt mit dem Namensabgleich vollständig als Luftschicht ohne Masse zurück, mit gleichem U; Kandidat
  (ii) — λ = d/R, ρ = 5 kg/m³, c = 1 000 J/(kgK) — kehrt als gewöhnliche Schicht mit ρ = 5 zurück, U auf
  1e‑12 gleich. **Festgeschrieben ist (i).** Ohne Namensabgleich kehrt (i) masselos zurück (nur U); der
  Importdialog gleicht immer ab.
- **Laufzeit Klassenweg:** Die Hochrechnung (ein Jahreslauf) für Projekt 1039, Gebäude 10642, dauerte
  164 ms (W2) bzw. 109 ms (W4), weit unter der Schwelle von 1 s. Ihre Einträge im Simulationsprotokoll
  werden herausgenommen und als Exportmeldungen gezeigt; das Protokoll des letzten Laufs bleibt unverändert.
- **Ersatzschichtung:** Σ κ·A trifft die Gruppenkapazität des Laufs auf 1e‑9, das U der Ersatzschicht das
  U des Bauteils; Bänder, Grenzfälle, der Vorbehalt wörtlich und die Stoffnamen gegen den Namensabgleich
  (deutsch und englisch) sind geprüft.

## 6 Gate W4

Kern-Filter 0 Fehler; Tests 15 476 bestanden, 0 Fehler, 2 übersprungen (Bestand); Windows-Schale 0 Fehler; SqlDialektPruefer 1 988 Texte, 0 Fundstellen; Referenzlauf
gegen R19 14/14 PASS, 432 CSV byte-gleich.

## 7 Offen

- **Windows-Sichtabnahme** (Punkteliste in der Statusdatei, Nach #529).
- **Zwei Freigabeschalter** müssen vor jeder Auslieferung aus sein: `GebaeudeZonenregeln.MehrereZonenFreigegeben`
  (ohne G6b, E46/A1) und `GebaeudeExportRegeln.GbxmlExportFreigegeben` (ohne G7b, E48/F1).
- **Wiki und Logbuch** erst mit G7b; der Upload ist zurückgestellt. Entwurf des Wiki-Abschnitts unten,
  Entwurf des Logbuch-Eintrags: „Gebäude lassen sich aus dem Gebäudedialog im Format gbXML ausgeben – mit
  Zonen, Bauteilflächen und Schichtaufbauten." (Versionsnummer beim Anwender).
- **Auf G7b verschoben:** `PolyLoop`, `ShellGeometry`, `Results`, die Kennzeichnung „schematisch" an drei
  Stellen und die 3D-Ansicht, der Einstieg im Bedarfsdialog, der Hilfe-Anker, der iOS-Prüfmodus mit
  gbXML-Export (Probe 20, ein macOS-Lauf nach Rückfrage). **G7d:** die Round-Trip-Sperre (Probe 13).

## 8 Entwurf des Wiki-Abschnitts (für G7b)

Für `Projekte/Wiki/Programm Dokumentation - Gebäudeimport.wiki`, vor „Grenzen"; gegen den Prüfausdruck aus
`CLAUDE.md` gelesen, ohne Treffer, ohne Produkt- und Programmnamen.

```
== Gebäudedaten ausgeben ==
{{Anker|export}}

Im Dialog '''Eingabe der Gebäudedaten''' steht neben ''Hülle und Zonen…'' der Knopf '''Exportieren (gbXML)…''' – auf dem iPad '''Exportieren und teilen (gbXML)…'''. Er schreibt die Daten des markierten Gebäudes im Projekt in eine Datei im Format '''gbXML'''. Eine Zeile, die noch nicht mit ''OK'' gespeichert ist, lässt sich nicht exportieren; der Knopf nennt den Grund. Exportiert wird immer der gespeicherte Stand.

Die Datei enthält die Zonen des Gebäudes mit Fläche, Volumen, Luftwechsel, Personen, Geräteleistung und Heizsollwert, jedes Bauteil mit Fläche, Ausrichtung, Neigung und Randbedingung, Fenster und Türen mit U-Wert und Gesamtenergiedurchlassgrad und die Schichtaufbauten mit Dicke und Stoffwerten. Ein Gebäude ohne Zonen wird so ausgegeben, wie ''Hülle und Zonen…'' es als eine Zone übernehmen würde, samt Hochrechnung; Faktor und Grundlage stehen in der Datei.

Die Datei enthält '''keine Geometrie''': keine Umrisse, keine Lage der Räume zueinander. Programme, die ein Gebäudemodell aus Polygonen erwarten, können sie deshalb nicht als Rechenmodell verwenden; sie eignet sich als Beleg, zur Archivierung und zum Wiedereinlesen in EPOS-Plan. Nicht enthalten sind außerdem Zeitpläne, Nacht- und Wochenendabsenkung, Wärmebrücken, Rahmenanteil, Kühlung und Wärmeübergabe sowie die Anlagentechnik.

Hat ein Bauteil keinen Schichtaufbau, schreibt EPOS-Plan eine gekennzeichnete Ersatzschicht aus U-Wert und Speichermasse. Sie '''trifft U-Wert und Gesamtwärmekapazität, nicht die Lage der Masse'''.

Vor dem Schreiben zeigt der Dialog, was ersetzt, gewechselt oder weggelassen wird; erst nach der Bestätigung lässt sich speichern. Lässt sich das Gebäude nicht ausgeben, nennt der Dialog den Grund. Die '''Postleitzahl''' ist freiwillig und wird nicht gespeichert: Mit ihr trägt die Datei den Standort und die Nordrichtung, ohne sie keinen Ort. Auf dem iPad liegt die Datei danach in den Dokumenten von EPOS-Plan und öffnet das Teilen-Menü.
```
