# R54 „Dachflächen“ — Satteldächer der Zonenprojekte 1052 und 1054 (E141)

Protokoll der Basis `Referenzlaeufe/2026-10-14_R54_Dachflaechen` nach Anwenderentscheid **E141**
([Status Gebäudesimulation](../../../aktuell/Status_Gebaeudesimulation_VDI6007.md)). Ausgangsstand `b67d99952`,
Schemastand 227, Vorgängerbasis R53 „Zapfprofiloptionen“.

## Befund

In `Tab_Bauteil` trugen die Dächer der Referenzprojekte 1052 und 1054 die geneigte Fläche mit Neigung 0° und ohne
Azimut: ID 9 (1052, Zone „Gästezimmer“, 428,28 m²), ID 24 (1052, „Gastronomie und Verwaltung“, 285,52 m²), ID 40 und
ID 55 (dieselben Zonen in 1054). Die Kellerdecken darunter haben 318,93 bzw. 212,62 m²; das Verhältnis 0,745 = cos 41,9°
weist auf ein Satteldach mit rund 42°. Die Übernahme „Gebäude als eine Zone“ liefert die Dachfläche der Vorlage 1018
waagerecht; der Bauplan von 1052 übernahm sie so. Die Modellprüfung meldete in beiden beheizten Zonen 26 % offene Hülle
(`HUELLE_OFFEN`, Protokoll [MA-3](2026-10-13_MA-3_Huelle_geschlossen.md)).

## Azimut-Entscheidung

Ein geneigtes Bauteil an Außenluft ohne Azimut ist im Kern **unzulässig**, nicht unbestimmt:
`GebaeudeZonenCtrl.BrauchtAzimut` verlangt ihn für jede Neigung außer 0° und 180°; der Zonendialog lehnt das Schreiben
ab, die Modellprüfung meldet `AZIMUT_FEHLT` rot, und `BauteilEingang` wirft im Gebäudelauf `AzimutFehlt`. Es gibt
weder eine Vorgabe Süd noch eine Mittelung. Deshalb ist jedes der vier Dächer **in zwei Hälften geteilt** statt nur die
Neigung zu setzen.

## Berichtigung

Auf der Testdatenbank (Arbeitskopie im Worktree, `python3 -I` mit `sqlite3`) je Dachzeile 9, 24, 40, 55:

- die Ränge der Bauteile hinter dem Dach derselben Zone um eins erhöht;
- die alte Zeile wird zur Nordhälfte: Bezeichner „Dach Nord“, Fläche halbiert, Neigung 42, Azimut 0;
- eine neue Zeile „Dach Süd“ mit den übrigen Spalten der Nordhälfte (Zone, Art, U-Wert 0,3, Randbedingung
  Außenluft, Herkunft VORGABE, halbe Fläche), Neigung 42, Azimut 180, Rang direkt hinter der Nordhälfte; neue IDs 80
  bis 83 am Ende (zu 9, 24, 40, 55).

Danach 83 Bauteilzeilen (vorher 79), `integrity_check` ok, `foreign_key_check` leer, Dateigröße unverändert
107 864 064 Byte, SHA-256 `1f2cf95f46f7eec275132eb22fea948be11d77f3103331d102f840c65edda04a`.

Die Hüllbilanz (`Zonenkopplungsregeln.Bilanz`) schließt je beheizte Zone: Σ A·cos 42° = 318,27 gegen 318,93 m²
(Gästezimmer) und 212,18 gegen 212,62 m² (Gastronomie), Abweichung 0,2 %; die waagerechte Bilanz bleibt durch die
gegenüberliegenden Hälften geschlossen. Die Modellprüfung von 1054 meldet keinen Hüllbefund mehr.

Der Bauplan `Referenzlaeufe/Skripte/referenzprojekt_1052_bauplan.cs` teilt das Dach der Übernahme ebenso (Konstanten
`DACHNEIGUNG`, `DACH_NORD`, `DACH_SUED`), damit der bitgleiche Nachbau der Wache die gesäten Zellen trifft.

## Vergleich R53 → R54

Lauf aller sechsunddreißig Referenzprojekte gegen die berichtigte Testdatenbank, `vergleich` gegen R53:
**GESAMT PASS, 36/36 Projekte, alle 1 171 CSV byte-gleich — auch 1052 und 1054.**

Grund: Die Gebäude von 1052 und 1054 rechnen ohne den Schalter „Strahlung auf Außenbauteile“
(`Tab_Gebaeude.Aussenbauteile_Strahlung` = 0). Ein opakes Bauteil an Außenluft geht dann allein mit U·A gegen θ_out
ein (Gl. (32) ohne Δθ_lw und Δθ_kw); Fläche und U-Wert des Dachs sind unverändert (zwei Hälften ergeben die alte
Fläche). Neigung und Azimut wirken nur über die Einstrahlung der opaken Fläche. Heizwärme, Spitze, Raumtemperaturen
und Aufheizplan der Zonen bleiben damit gleich (1052: 54,00 MWh Heizwärme des Gebäudes wie in R53). Die Basis wird
nach der Einfrierregel „gesäte Zonendaten“ trotzdem neu eingefroren, weil sie den berichtigten Datenstand der
Testdatenbank festhält.

Gegenprobe: die übrigen vierunddreißig Projekte PASS und byte-gleich; ein zweiter Lauf der achtzehn Projekte der
CI-Auswahl gegen R54 ist GESAMT PASS und byte-gleich.

## Dateien

- `Referenzlaeufe/Kenndaten_Test.sqlite` (LFS) — vier Dächer geteilt;
- `Referenzlaeufe/2026-10-14_R54_Dachflaechen/` — neue Basis (36 Projekte, 1 171 CSV, Protokoll);
- `Dokumentation/ueberholt/Referenzbasen/2026-10-13_R53_Zapfprofiloptionen/protokoll.txt` — Protokoll von R53, die
  Messdaten von R53 sind aus dem Arbeitsbaum entfernt; Wegweiser `Dokumentation/ueberholt/Referenzbasen/LIESMICH.md`;
- `Referenzlaeufe/LIESMICH.md` — aktuelle Basis R54, Vorgängerbasis R53, Vermerk an der Einfrierregel, Zonentabelle
  von 1052, Bauteilzahl von 1054;
- `Referenzlaeufe/Skripte/referenzprojekt_1052_bauplan.cs` — Satteldach im Bauplan;
- `.github/workflows/kern.yml`, `.github/workflows/ios.yml`, `CLAUDE.md`, `Dokumentation/LIESMICH.md`,
  `EPOS.Kern.Tests/FreieKuehlungReferenzprojektWacheTests.cs`, `EPOS.Kern.Tests/VorheizReferenzprojektWacheTests.cs` —
  Basisordner R54;
- `EPOS.Kern.Tests/ZonenReferenzprojektWacheTests.cs` (16/15/2 Bauteile, Neigung und Azimut der Hälften, Hülle je
  beheizte Zone geschlossen), `EPOS.Kern.Tests/ZonenHeizkreisReferenzprojektWacheTests.cs` (33 Bauteile),
  `EPOS.Kern.Tests/ModellpruefungHuelleTests.cs` (1054 schließt die Hülle jeder Zone, kein `AZIMUT_FEHLT`).

## Tests

- `dotnet build WP-Plan.Kern.slnf -c Release`: 0 Fehler.
- `EPOS.Kern.Tests` mit der Auswahl ReferenzprojektWache, Zonen, Gebaeude, Schema, Dokumentation, Repository,
  Modellpruefung: 2 652 Tests, 2 650 grün nach Nachlauf, 2 übersprungen (vorbestehend). Drei Rote des ersten Laufs
  rührten vom Zwischenstand mit zwei Basisordnern und einem nicht nachgezogenen Verweis der Basenhistorie her; im
  Nachlauf der betroffenen Klassen (59 Tests) und der Link-Wache (9 Tests) grün.
- Zweiter Lauf der achtzehn CI-Projekte gegen R54: GESAMT PASS, 601 CSV byte-gleich.
- `Werkzeuge/SqlDialektPruefer`: 2 977 SQL-Texte, 0 Fundstellen.

## Logbuch

Keiner — Datenberichtigung der Testdatenbank ohne sichtbare Funktionsänderung.
