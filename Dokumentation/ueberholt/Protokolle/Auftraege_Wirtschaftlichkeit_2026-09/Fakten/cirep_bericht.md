# CI-Reparatur Kulturleck — Bericht (Opus, 25.09.2026 ca. 20:25, cirep = 3d703eb7 auf 868afc57; Merge #515 = 86c4fa55 auf pm26 [nach Merge origin c02dbfb4])

Anlass: Anwender 25.09.2026 „Neu seit heute Nachmittag: beheben" — Windows-CI 36161752448 auf main 822ba803 (Push #513) 1/7.270 Kern-Tests rot:
`GebaeudeHochrechnungTests.Eine_Zone_ohne_Bezugsflaeche_ist_ein_benannter_Fehler` (`:213`) erwartet „Null" (deutsch), Meldung kam englisch.
**Befund A (echt, behoben):** `GebaeudeHochrechnungTests.cs:22` pinnte keine Kultur, hält aber deutsche Meldetexte fest; grün nur hinter einer von sechs
Klassen, die eine `Kulturvorrichtung` als Feld anlegen und nie entsorgen (nicht IDisposable): `DwdTryLeserTests.cs:26`, `EmissionsspalteTests.cs:25`,
`ErloesrubrikTests.cs:28`, `KwkgPauschaleZeileTests.cs:33`, `KwkgSatzHerkunftTests.cs:34`, `TryPaketLeserTests.cs:28` — die Vorrichtung setzt
`DefaultThreadCurrent(UI)Culture = de-DE` prozessweit und blieb bis Laufende stehen; die neuen Gebäudeimport-Klassen der Cloud-Commits (e0d4eaa1..822ba803)
verschoben die Reihenfolge → Fall lief unter en-US. Keiner der Cloud-Commits setzt eine Kultur (die vermutete Zeile in TestDatenbank.cs gibt es dort nicht).
Änderung 3d703eb7 (7 Dateien, +25/−7, nur Testcode): GebaeudeHochrechnungTests eigene Kulturvorrichtung + IDisposable (Muster GebaeudeRundlaufTests);
die sechs Klassen IDisposable mit `Dispose() => _kultur.Dispose()`; EPOS.UI.Tests ohne solche Klasse. Nachweis: Fehler nachgestellt mit
vorübergehendem Modulinitialisierer en-US (nicht committet): vorher Klasse allein 1/12 rot mit der CI-Meldung, hinter DwdTryLeser grün; nachher unter
en-US Kern 7.283/7.284 (einziger roter Fall = bestehender KulturwaechterTests, der die Probe-Datei meldet, erwartet); Endstand deutsch mit CI-Schaltern:
gefiltert 660/660, Kern voll 7.281/7.282 (1 übersprungen; fremder testhost lief parallel, Wartezeit nicht eingehalten).
**Befund B (kein Fehler, nichts geändert):** 17 rote UI-Tests kamen nur ohne xUnit-Schalter (parallele Sammlungen melden Dialoge gleichzeitig an der
prozessweiten `KiMaskenbruecke`; `AktiveMaske()` = zuletzt angemeldete, `KiMaskenbruecke.cs:656`, Anmeldung 548–590); Masken bunt gemischt; fc0b7e5c
(bb876a21/b9c26b88) fügt keinen An-/Abmeldeweg hinzu (`GebaeudeKatalogDialog.razor:1193/1289` unverändert). Ohne Schalter: HEAD 868afc57 3 Läufe 19/14/19 rot
von 6.378, Basis edf89ae8 2 Läufe 17/16 rot von 6.375; mit Schaltern auf 868afc57 UI 6.378/6.378 grün. Kern-CI fc0b7e5c 36164923229 grün.
Zurückgenommene Vorschläge für später: Wächter in KulturwaechterTests (jede Kulturvorrichtung wird entsorgt, war grün); `xunit.runner.json`
`parallelizeTestCollections: false` für EPOS.UI.Tests (drei Durchgänge 6.378/6.378 grün).
Statuszeilenvorschlag (Wortlaut des Agenten): CI-Reparatur Kulturleck (3d703eb7, nur Tests): Die Windows-CI 36161752448 auf main 822ba803 war mit 1/7.270
rot. Ursache: GebaeudeHochrechnungTests hielt deutsche Meldetexte fest, pinnte aber keine Kultur. Grün war die Klasse nur hinter einer der sechs Klassen
(DwdTryLeser, Emissionsspalte, Erloesrubrik, KwkgPauschaleZeile, KwkgSatzHerkunft, TryPaketLeser), die ihre Kulturvorrichtung nie entsorgten und so de-DE
prozessweit stehen ließen. Die neuen Gebäudeimport-Klassen verschoben die Reihenfolge. Die Klasse pinnt jetzt selbst de-DE, die sechs Lecks sind
IDisposable. Unter simulierter en-US-Kultur ist der Kern grün bis auf den erwarteten Wächterfund der Probe; ohne Probe Kern 7.281/7.282 grün (1
übersprungen). Der lokale UI-Befund mit 17 roten Masken-Tests war kein Fehler: Er entstand ohne die xUnit-Schalter durch parallele Anmeldungen an der
statischen KiMaskenbruecke und tritt genauso auf edf89ae8 auf. Mit Schaltern ist UI 6.378/6.378 grün.
