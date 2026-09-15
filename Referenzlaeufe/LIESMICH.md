# Referenzlauf-Suite (Paket B1)

Regressionsbasis für den Simulationskern von EPOS-Plan.

Vor jedem Umbau an der Engine wird der aktuelle Stand als CSV eingefroren; nach dem Umbau
läuft derselbe Satz Projekte erneut und wird mit Toleranz gegen den eingefrorenen Stand
verglichen. Was sich dabei ändert, ist entweder gewollt — dann wird die Referenz neu
gesetzt — oder ein Fehler.

Grundlage: `Dokumentation/ueberholt/Protokolle/Simulation/Konzept_Simulation_QuellenSenken.md`,
Paket B1, Kapitel 9.

## Git LFS (Anwenderentscheid AUF‑Q2 vom 12.09.2026, Auftrag #243)

**Die Testdatenbank `Kenndaten_Test.sqlite` (68 MB) liegt in Git LFS**, zusammen
mit den 68 Herstellerarchiven unter `VDI-3805-Daten/**/*.zip|*.vdi|*.VDI` (97 MB) — 69
Dateien, rund 165 MB. Alles andere bleibt ein normaler Git-Blob: die CSV der Basen, die
Importproben, die CEC- und PAN-Listen, PDF, JPG, XLSX. Die Regeln stehen in der
`.gitattributes`; die Wache `EPOS.Kern.Tests/RepositoryOrdnungWacheTests` prüft, dass sie
dort stehen.

Die Umstellung gilt **ab** dem Commit von #243.

**Einmal je Rechner:**

```
git lfs install
```

**Nach dem Klonen** (oder wenn statt der Datenbank eine 130-Byte-Textdatei daliegt):

```
git lfs pull                                                   # alles, 165 MB
git lfs pull --include="Referenzlaeufe/Kenndaten_Test.sqlite"  # nur die Testdatenbank
```

**Gezielt ziehen ist die Regel, nicht die Ausnahme.** GitHub gibt je Konto 1 GB
LFS-Speicher und **1 GB Bandbreite je Monat** frei; ein einziger ungefilterter Abruf kostet
165 MB davon. Die Workflows halten sich daran: `actions/checkout` läuft **ohne** `lfs: true`,
`kern.yml`, `windows.yml` (Job `build-test`) und `ios.yml` ziehen nur die Testdatenbank und
legen `.git/lfs` in den Actions-Cache (Schlüssel aus dem Hash des Zeigers — er wechselt genau
mit der Datenbank). Ungefiltert zieht allein der Job `installer`, weil das Setup
`VDI-3805-Daten\*` mit einpackt und Zeigerdateien im Installer ein Auslieferungsfehler wären.

**Wer die Testdatenbank ändert, committet sie nur mit aktivem LFS-Filter.** Ohne
`git lfs install` legt Git wieder einen 68-MB-Blob in die Geschichte, und der ist nicht mehr
herauszubekommen. Die Probe: `git show :Referenzlaeufe/Kenndaten_Test.sqlite | head -3` muss
`version https://git-lfs.github.com/spec/v1` zeigen, `git lfs ls-files | wc -l` muss 69
ergeben.

**Liegt ein Zeiger statt der Datenbank**, bricht nicht irgendwann ein SELECT mit „file is not
a database" ab, sondern es meldet sich mit Ursache und Abhilfe, wer die Datei öffnet:
`EPOS.Kern.Tests/TestDatenbank.cs`, `Referenzlauf/DbUmgebung.cs` und
`Werkzeuge/Auslieferungsvorlage/Argumente.cs` (Rückgabe 2). Dieselbe Probe fährt jeder der
drei Workflows, bevor er etwas baut.

## Die Einfrierregel: Emissionsfaktoren (Anwenderentscheid Em‑9.8‑Q4)

`aggregate.csv` führt zehn Emissionsskalare je Projekt mit Kessel- bzw. BHKW-Stufe
(`Em.Kessel.Co2T`, `…So2Kg`, `…NoxKg`, `…CoKg`, `…StaubKg` und dieselben fünf für `Em.Bhkw.`).
Damit ist `Kenndaten_Test.sqlite` an den **Emissionsfaktoren** regressionsrelevant:

> **Wer einen gesäten Emissionsfaktor der Testdatenbank ändert, friert im selben Schritt
> die Basis neu ein und begründet den Wechsel hier.**
>
> Betroffen ist jede Änderung an `emissionsart` (Auswahl, Äquivalenzfaktor), an einem
> **aktiven** `emissionswert` (81 Zeilen), an `Tab_Brennstoff_Stamm.CO2/SO2/NOx/Staub`
> (25 Sätze), an `energy_project_settings.co2/so2/nox` der zwölf Referenzprojekte und am
> Berechnungsmodus (`Tab_Projekt.Emission_Berechnungsmodus`) eines von ihnen.
>
> **Nicht** betroffen ist die Pflege von **Vorlagen** (`ist_aktiv = falsch`, 224 Zeilen) —
> sie erreichen die Lesekette gar nicht.

Ohne diese Regel fiele die CI beim nächsten Katalogschritt rot aus, ohne dass jemand mit dem
Zusammenhang rechnete. Herleitung und Messung stehen in
[`Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md`](../Dokumentation/aktuell/Konzept_Emissionsarten_CO2-Aequivalent_EPOS-Plan.md)
§ 11.2.6; der Entscheid selbst in § 8 („Em‑9.8 / Em‑9.9 — die sieben Fragen aus § 11.5").

## Die zweite Einfrierregel: PV-Modulkoeffizienten (Befund W6‑B‑5)

Dieselbe Klasse von Falle, andere Spalte. **`T_NOCT` aus `Tab_PV_STAMM`/`Tab_PV` geht in beide
PV-Modelle**: `SimulationPV.NoctDesModuls` nimmt den Katalogwert, sobald er im Fenster
20…60 °C liegt, und sonst den Rückfall 45 °C. Ein einziger geänderter NOCT verschiebt damit
die Jahreserzeugung eines Projekts.

> **Wer einen gesäten Modulkoeffizienten der Testdatenbank ändert, friert im selben Schritt
> die Basis neu ein und begründet den Wechsel hier.**
>
> Betroffen ist jede Änderung an `alpha_SC`, `beta_OC`, `gamma_PMP` oder `T_NOCT` in
> `Tab_PV_STAMM` (6 Sätze) und `Tab_PV` (9 Sätze) — und ebenso das Anlegen eines neuen
> PV-Moduls, das ein Referenzprojekt benutzt.
>
> **Rechenwirkung hat davon nur `T_NOCT`** (und `gamma_PMP`, das aber in keinem
> Referenzprojekt verdorben war). `alpha_SC` und `beta_OC` liest kein Rechenweg — sie
> speisen die Strangplausibilität (die Ampel des PV-Dialogs) und die Importprüfung. Der
> Gegenbeweis steht im
> [`protokoll.txt`](../Dokumentation/ueberholt/Referenzbasen/2026-09-07_R6_PvKoeffizienten/protokoll.txt)
> der Basis `2026-09-07_R6_PvKoeffizienten`.

## Die dritte Einfrierregel: die Flottenparameter des Projekts 1046 (SP‑O‑8)

Dieselbe Klasse von Falle, dritter Ort. Projekt **1046 „Prüfprojekt Speicherflotte"** führt den
einzigen aktivierten Stand `@Projektflotte` in `Tab_SpeicherAuslegung`; er schaltet im
gewöhnlichen Projektlauf den **Flottenpfad** ein (`SpeicherFlottenProjektCtrl.IstAktiv` →
`SimulationControl.SpeicherlaufAusfuehren`), und
`aggregate.csv` führt dafür 42 Skalare und vier Ganglinien. Jede Zahl dieses Standes geht damit
unmittelbar in die Referenz.

> **Wer den Stand `@Projektflotte` des Projekts 1046 ändert, friert im selben Schritt die
> Basis neu ein und begründet den Wechsel hier.**
>
> Betroffen ist jede Änderung an Einheitenzahl, Kapazität, Lade-/Entladeleistung,
> Richtungswirkungsgraden, SoC-Grenzen, Start-SoC, Peak-Reserve, Hilfsverbrauch,
> Betriebsziel, Verteilung, Peak-Ziel, Netzladung, Batterieexport, Energie-Ausgleichswert
> oder Lebensdauerkurve — und ebenso jede Änderung an den Projektzeilen von 1046 selbst.
>
> **Nicht** betroffen sind Auslegungs- und Arbeitsstände unter einem ANDEREN Bezeichner als
> `@Projektflotte`: Sie erreichen den Projektlauf nicht. Die übrigen Projekte der
> Testdatenbank führen gar keinen Flottenstand.

Das Projekt entsteht wiederholbar aus
[`Skripte/pruefprojekt_1046_speicherflotte.py`](Skripte/pruefprojekt_1046_speicherflotte.py);
Herleitung der Größen, der drei Gegenproben und der Wahl des Peak-Ziels stehen im
`protokoll.txt` der Basis `2026-09-11_R7_Speicherflotte`.

## Entfernte Basen

**`Referenzlaeufe/Importproben` gehört zum Testbestand und wird nie gelöscht; wer die Ordner der
Basen aufräumt, lässt `2026-09-11_R7_Speicherflotte`, `Kenndaten_Test.sqlite`, `Importproben`,
`Skripte` und `LIESMICH.md` stehen.**

Außer der aktuellen liegt hier keine Basis mehr: Die 24 historischen Referenzbasen (7 731
Dateien, 1 016,7 MB) sind am 11.09.2026 aus dem Arbeitsbaum gefallen (**SYNC‑Q1**: „entfernt
lassen") und seit dem Umschreiben der Git-Geschichte (**AUF‑Q1**, 12.09.2026) auch dort nicht
mehr enthalten; kein Test, kein Gate, keine CI liest eine entfernte Basis. **Die Messdaten
sind endgültig weg** (rund 7 700 CSV-Dateien) — eine alte Zahl steht nur noch im Protokoll.
Erhalten sind die **Protokolle** aller 24 Basen samt der Tabelle Basis → Datum → Zweck →
Protokoll unter
[`Dokumentation/ueberholt/Referenzbasen/`](../Dokumentation/ueberholt/Referenzbasen/LIESMICH.md);
**welche Basis wann von welcher abgelöst wurde und warum**, steht ebendort in der ausgelagerten
[Basenhistorie](../Dokumentation/ueberholt/Referenzbasen/LIESMICH_Basenhistorie_bis_2026-09-12.md).

## Aktuelle Basis

**`2026-09-11_R7_Speicherflotte/`** — **dreizehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024,
1030, 1039, 1040, 1041, 1042, 1045, 1046), **345 CSV**, **1 937 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` auf Linux gegen `Kenndaten_Test.sqlite` (**Schemastand 73**;
die Datei selbst steht auf **Schemastand 77** — der Lauf gegen diese Basis bleibt davon
byte-gleich, siehe die Nachträge am Ende des Abschnitts). Gegen diese Basis hält
`.github/workflows/kern.yml` (1030, 1007, 1017, 1045, **1046**) jeden Push, `ios.yml` den
iZ6-Vergleich für 1030; das Gate der Orchestrierung zieht getrennt nach. Sie ist die **einzige**
Basis im Arbeitsbaum.

> **Anlass: der Anwenderentscheid SP‑O‑8, „Empfehlung".** Das Mehrspeicherkonzept hat mit
> `SpeicherEngine/Flotten*.cs`, den Controllern `SpeicherFlotten*Ctrl` und der Weiche in
> `SimulationControl.Stromspeicher.cs` einen **zweiten Speicherpfad** in den gewöhnlichen
> Projektlauf gelegt, den **kein Referenzprojekt betrat** — sie fahren alle die Einzelanlage
> über `StromspeicherSimCtrl.RechneAktiveVariante`. Eine stille Änderung an Verteilung,
> Reserve, Richtungswirkungsgrad oder Netzbilanz wäre in keinem Referenzlauf aufgefallen.
>
> **Das dreizehnte Projekt: 1046 „Prüfprojekt Speicherflotte"**, eine Tiefkopie von **1007**
> („Laurentiuskirche" — von den drei Projekten mit Strombedarf UND PV das mit der höchsten
> Bezugsspitze, 19,776 kW gegen 8,37 kW bei 1040 und 1045; und das einzige, das schon
> Stromspeicheranlagen führt, wodurch dasselbe Projekt mit ausgeschalteter Flotte den
> Einzelpfad rechnet). Darin eine **Flotte aus zwei Einheiten**: A mit 24 kWh an 10/12 kW
> (η 0,96/0,94, SoC 0,05–0,95, Reserve 2,4 kWh), B mit 16 kWh an 6/7 kW (η 0,93/0,91,
> SoC 0,10–0,90, Reserve 1,2 kWh). Betriebsziel **`PeakShaving`** gegen **16,0 kW**,
> Verteilung **`Kaskade`**, Netzladung frei, Batterieexport gesperrt. **Kein planendes Ziel** —
> `PvPlanung`, `Arbitrage` und `MultiUse` brauchen einen `IFlottenPlaner` und damit
> Google OR-Tools, die der plattformfreie Referenzlauf bewusst nicht einbindet.
>
> **Die zwölf übrigen Projekte sind byte-gleich zur Vorgängerbasis** — `diff -rq` je Projekt
> ohne einen einzigen Unterschied in 312 CSV. Der Schritt auf diese Basis ist reine
> **Erweiterung**: das neue Projekt in der Testdatenbank und 42 Skalare plus vier Ganglinien
> im Export, beide unter der Bedingung „die Flotte hat gerechnet".
>
> | Projekt 1046 | Flotte AUS (Einzelspeicher) | Flotte AN | Differenz |
> |---|---:|---:|---:|
> | Bezugsspitze [kW] | 19,7762 | **16,7428** | −3,0334 (−15,3 %) |
> | Netzbezug [kWh] | 50 538,68 | **51 611,01** | +1 072,33 (+2,1 %) |
> | Intervalle über 16 kW | 2 864 | **20** | — |
> | CSV / Skalare | 29 / 99 | **33 / 145** | +4 / +46 |
>
> Der höhere Netzbezug ist die Rechnung, nicht ein Fehler: Peak Shaving mit freigegebener
> Netzladung kauft unter dem Peak-Ziel und gibt später mit Wirkungsgradverlust wieder ab
> (122,42 kWh Umwandlungsverlust); bezahlt wird das mit dem Leistungspreis auf 3,03 kW weniger
> Bezugsspitze.
>
> **Die Flottenwege sind wirklich betreten:** A entlädt in 1 761 und lädt in 877 Intervallen,
> B in 1 177 bzw. 734, „nur B entlädt" in 1 152 (A steht an seiner unteren Grenze — das ist
> die **Kaskade**); beide SoC-Bänder voll ausgefahren, die **Peak-Reserve** nur bei
> tatsächlicher Peak-Überschreitung freigegeben (254 bzw. 672), gleichzeitiges Laden und
> Entladen innerhalb der Flotte: **0 Intervalle**.
>
> **Keine Lebensdauerkurve, mit Absicht:** `RainflowKurve` bleibt leer, der Miner-Schaden
> damit 0. Mit Kurve bricht die Auswertung ab, sobald eine Zyklustiefe außerhalb der
> Stützstellen liegt — ein Referenzprojekt, das bei einer harmlosen Änderung nicht abweicht,
> sondern abstürzt, wäre ein schlechtes Regressionsnetz. Der Skalar steht trotzdem, damit eine
> später hinterlegte Kurve eine ZAHL ändert und keinen SCHLÜSSEL hinzufügt (Muster `Em.*.CoKg`).
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **13/13 byte-gleich**,
> Toleranzvergleich **13/13 PASS** (3 777 497 Werte), Laufzeit 00:00:04; auch das Skript ist
> wiederholbar.
>
> ```bash
> python3 Referenzlaeufe/Skripte/pruefprojekt_1046_speicherflotte.py \
>   Referenzlaeufe/Kenndaten_Test.sqlite
>
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046 \
>   --ziel Referenzlaeufe/2026-09-11_R7_Speicherflotte
> ```
>
> Aufbau des Projekts, Herleitung der Größen — auch des Peak-Ziels 16,0 kW — und die drei
> Gegenproben im Wortlaut stehen im `protokoll.txt` der Basis.

> **Nachtrag: Schemastand 74 (Auftrag #178), die Basis bleibt.** Migrationsschritt **74**
> (`SCHRITT_74_SPEICHERAUSLEGUNG_STRICT`) baut `Tab_SpeicherAuslegung` als **STRICT**-Tabelle neu
> auf — `CREATE` unter Hilfsnamen, `INSERT … SELECT` mit **namentlich genannten** Spalten, `DROP`,
> `RENAME`, Index neu, alles in EINER Transaktion
> (`EPOS.Kern/Allgemein/Update/SpeicherAuslegungStrict.cs`); Spalten, Typen, Schlüssel und
> `idx_SpeicherAuslegung` bleiben wortgleich, und `SpeicherAuslegungCtrl.SQL_TABELLE` trägt das
> `STRICT` selbst, damit eine NEUE Datenbank die Tabelle gleich richtig anlegt. Stand der Datei:
> **Schemastand 74**, **70 803 456 Byte**, **119 Tabellen, davon 118 STRICT**, **25 Projekte**;
> die einzige Zeile — der Flottenstand `@Projektflotte` des Projekts 1046 — ist byte-gleich
> übernommen (SHA-256 vorher wie nachher `251c8554…add0d9`). **Der Referenzlauf ist 13/13
> byte-gleich gegen diese Basis** (Toleranzvergleich 13/13 PASS, 3 777 497 Werte): Kein
> Rechenwert ändert sich, die Basis wird nicht neu eingefroren, und **keine der drei
> Einfrierregeln ist berührt** — der Schritt kopiert Zeilen, er schreibt keine.

> **Nachtrag: Schemastand 75 (Auftrag #269), die Basis bleibt.** Migrationsschritt **75**
> (`SCHRITT_75_NUTZUNGSDAUER`) legt die Nutzungsdauertabelle `Tab_Nutzungsdauer` an
> (**STRICT** von der ersten Zeile an), sät ihre **28 Auslieferungszeilen** mit Richtwerten
> und Quellenangabe, hängt die nullbare Verweisspalte `NutzungsdauerID` an
> `Tab_KostenVorlagePosition` und `Tab_ProjektWerte` und ordnet **31 der 53
> Investitionspositionen** über ihren Namen einer Positionsart zu
> (`EPOS.Kern/Allgemein/Update/NutzungsdauerSchema.cs`). Stand der Datei: **Schemastand 75**,
> **70 762 496 Byte**, **120 Tabellen, davon 119 STRICT**, **25 Projekte**.
> **`Tab_ProjektWerte` bekommt die Spalte, aber keinen Wert** — alle 175 Projektzeilen sind
> Feld für Feld unverändert, und die 120 Vorlagenpositionen behalten ihre Altspalten
> wortgleich. **Der Referenzlauf ist 5/5 byte-gleich gegen diese Basis** (Toleranzvergleich
> 5/5 PASS, 1 586 257 Werte): Kein Rechenwert ändert sich, die Basis wird nicht neu
> eingefroren, und **keine der drei Einfrierregeln ist berührt** — der Schritt legt eine
> Tabelle an und füllt sie, er rührt keine gerechnete Größe an.

> **Nachtrag: Schemastand 76 (Auftrag #278), die Basis bleibt.** Migrationsschritt **76**
> (`SCHRITT_76_TRAEGERSATZ_EINDEUTIG`) entdoppelt `energy_project_settings` und legt darüber
> den eindeutigen Index `idx_EnergyProjectSettings_Traeger` auf
> `(ID_Projekt, ID_Energieträger)` — die Regel „ein Preis und ein Emissionssatz je
> Energieträger im Projekt" hält ab hier die Datenbank und nicht mehr allein die
> Anwendungslogik (`EPOS.Kern/Allgemein/Update/ProjektEnergietraegerEindeutig.cs`).
> **Auf der Messlatte gab es nichts zu entdoppeln:** Die 28 Zeilen der Tabelle verteilen
> sich auf 18 Projekte, kein Paar kommt zweimal vor, also entfernt der Schritt keine Zeile.
> Stand der Datei: **Schemastand 76**, **70 766 592 Byte**, **120 Tabellen, davon 119
> STRICT**, **25 Projekte**; `PRAGMA integrity_check` = `ok`, `PRAGMA foreign_key_check`
> bleibt leer. **Der Referenzlauf ist 5/5 byte-gleich gegen diese Basis**
> (Toleranzvergleich 5/5 PASS, 1 586 257 Werte): Kein Rechenwert ändert sich, die Basis
> wird nicht neu eingefroren, und **keine der drei Einfrierregeln ist berührt** — der
> Schritt legt einen Index an, er schreibt keinen Wert.

> **Nachtrag: Schemastand 77 (Auftrag #284), die Basis bleibt.** Migrationsschritt **77**
> (`SCHRITT_77_PUFFER_VOLUMENBEMESSUNG`) stellt die ausgelieferte Investitionsvorlage des
> Pufferspeichers von „je kWh Kapazität" auf die Bemessung je **Liter Gesamtvolumen** um —
> der Pufferspeicher führt keine kWh-Kapazität, die Art blieb dort ohne Bezugsgröße
> (`EPOS.Kern/Allgemein/Update/PufferspeicherBemessungVolumen.cs`). **Genau eine Zeile war
> betroffen:** `Tab_KostenVorlagePosition` 38 („Speicher", Vorlage 6), **Satz `NULL`** — die
> Vorlage gibt die Art vor, keine Zahl. Der Schritt fasst ausschließlich Zeilen **ohne**
> gepflegten Satz an; auf der Messlatte gab es keine mit Satz. `Tab_ProjektWerte` führt die
> Art am Pufferspeicher überhaupt nicht und bleibt unberührt, die Vorlagen der übrigen neun
> Komponenten ebenso (die PV-Position „Batteriespeicher" und die Stromspeicher-Position
> „Speicher" tragen sie weiter). Stand der Datei: **Schemastand 77**, **70 766 592 Byte**,
> **120 Tabellen, davon 119 STRICT**, **25 Projekte**; `PRAGMA integrity_check` = `ok`,
> `PRAGMA foreign_key_check` bleibt leer. **Der Referenzlauf ist 5/5 byte-gleich gegen diese
> Basis** (Toleranzvergleich 5/5 PASS, 1 586 257 Werte): Kein Rechenwert ändert sich, die
> Basis wird nicht neu eingefroren, und **keine der drei Einfrierregeln ist berührt** — der
> Schritt ändert eine Bemessungsart ohne Satz, er schreibt keinen Wert. Die beiden
> Pufferspeicher der Referenzprojekte 1007 und 1046 (Anlagenzeilen 11238 und 14941) sind
> nicht angefasst.

## Was hier liegt

| Pfad | Inhalt |
|---|---|
| `<yyyy-MM-dd>_<Marke>/` | Ein eingefrorener Lauf: je Projekt ein Unterordner `Projekt_<ID>/`, dazu `lauf_protokoll.md` bzw. `protokoll.txt` |
| `<...>/Projekt_<ID>/aggregate.csv` | Alle Skalare des Laufs: `Tab_Ergebnis*`-Zeilen, Restgrößen aus `SimulationControl`, Jahressumme jedes Vektors |
| `<...>/Projekt_<ID>/*.csv` | Die Ganglinien: 8760 Stundenwerte bzw. 35040 Viertelstundenwerte, `Index;Wert` |
| `Arbeitskopie/` | Die Kopie der Datenbank, auf der gerechnet wird. Wird bei jedem `lauf` neu angelegt. Nicht im Git (`Kenndaten.accdb` ist in `.gitignore`) |
| `Kenndaten_Test.sqlite` | Die reduzierte Testdatenbank, gegen die der plattformfreie `EPOS.Referenzlauf` und der SQL-Dialektprüfer laufen. **Versioniert** — eine Änderung daran gehört in einen eigenen Commit |
| `Skripte/` | Was an dieser Testdatenbank gemacht wurde, als Skript und nicht als Erzählung: `pruefprojekt_1045_ost_west.py` (W6‑O‑7) und `pruefprojekt_1046_speicherflotte.py` (SP‑O‑8) |

Der Werkzeugcode liegt in `../Referenzlauf/`.

### Das Prüfprojekt 1045 neu anlegen

`Skripte/pruefprojekt_1045_ost_west.py` legt das zwölfte Projekt der Basis an: Tiefkopie von
Projekt 1040, zwei gepflegte Modulkopien, der Wechselrichter „Muster 2500TL" in Katalog und
Projektkopie, zwei Strangzeilen und die PV-Anlagenzeile auf dem Katalogweg. Der Kopfkommentar
des Skripts nennt jede Zahl und jede Abweichung von Anhang A des Wechselrichterkonzepts.

```bash
cp Referenzlaeufe/Kenndaten_Test.sqlite /tmp/Kenndaten_Test.sicherung     # erst sichern
python3 Referenzlaeufe/Skripte/pruefprojekt_1045_ost_west.py Referenzlaeufe/Kenndaten_Test.sqlite
```

Das Skript **bricht ab**, wenn Projekt 1045 schon steht; für einen zweiten Lauf die Sicherung
zurücklegen — dann vergibt er dieselben Ids. Zum Schluss vergleicht er die Zeilenzahlen je
Tabelle zwischen Vorlage und Kopie; fehlt etwas, fällt es dort auf und nicht erst im
Rechenergebnis. Danach gehören dazu: `python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db
Referenzlaeufe/Kenndaten_Test.sqlite` (0 Fundstellen) und
`dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite
--trocken` (0 Spalten, 0 Tabellen anzulegen).

## Die wichtigste Regel

**Die produktive `Kenndaten.accdb` wird nie beschrieben.**

Die Suite kopiert sie nach `Referenzlaeufe/Arbeitskopie/`, biegt den DB-Pfad der Anwendung
per Reflection auf diesen Ordner um und prüft anschließend über
`DataRepository.GetDBPath()` nach, dass die Anwendung wirklich auf der Kopie arbeitet.
Zeigt der Pfad woanders hin — oder auf eine der bekannten produktiven Ablagen — bricht der
Lauf sofort ab. Auch jeder Kindprozess prüft das für sich noch einmal.

Liegt neben der Quelle eine `Kenndaten.laccdb`, ist die Datenbank gerade geöffnet. Kopiert
wird trotzdem (lesend), aber das Protokoll weist darauf hin: die Kopie kann dann Änderungen
der laufenden Sitzung noch nicht enthalten. Für einen belastbaren Referenzlauf die
Anwendung vorher schließen.

## Bauen

Standardweg:

```powershell
dotnet build Referenzlauf\Referenzlauf.csproj -c Debug -p:Platform=x64
```

`Referenzlauf.csproj` steht **in** `WP-Plan.sln` (der Kopfkommentar der `.csproj` sagt das
Gegenteil und ist überholt). Wer die ganze Solution baut, bekommt das Werkzeug also mit:
`dotnet build WP-Plan.sln -c Debug -p:Platform=x64`.

Alternative über das MSBuild von Visual Studio, falls `dotnet` einmal nicht in Frage kommt:

```powershell
$msb = & "${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe" `
        -latest -prerelease -products * -requires Microsoft.Component.MSBuild `
        -find 'MSBuild\**\Bin\MSBuild.exe' | Where-Object { $_ -notmatch '\\amd64\\' } | Select-Object -First 1
& $msb `
    C:\Waermeplan\WP_Plan\Referenzlauf\Referenzlauf.csproj `
    -p:Configuration=Debug -p:Platform=x64
```

Beim allerersten Mal davor einmal `-t:Restore` mit denselben Parametern.

Ergebnis: `Referenzlauf\bin\x64\Debug\net10.0-windows\Referenzlauf.exe` (alte Protokolle
nennen noch den Frameworkordner vor .NET 10).

## Bedienung

```powershell
$exe = "C:\Waermeplan\WP_Plan\Referenzlauf\bin\x64\Debug\net10.0-windows\Referenzlauf.exe"
```

### `lauf` — Stand einfrieren

```powershell
& $exe lauf                                  # Ziel: Referenzlaeufe\<heute>_B0
& $exe lauf --ziel D:\Temp\NachUmbau         # anderer Zielordner
& $exe lauf --projekte 1010,1023             # feste Projektliste statt Automatik
& $exe lauf --timeout 600                    # Zeitlimit je Projekt in Sekunden (Standard 300)
```

Kopiert die Datenbank, **migriert sie auf den Zielstand des Schemas**, wählt die Projekte,
rechnet und schreibt CSVs plus `lauf_protokoll.md`. Exit-Code 0, wenn alle Projekte
durchgelaufen sind.

Die Migration (Schritt 2b) gehört dazu: Ohne sie rechnet `lauf` auf einer Kopie im Stand der
Quelldatenbank, deren fehlende Spalten und fehlende `Tab_ErgebnisPufferspeicher` nur die
Rückfallebenen im Anwendungscode notdürftig ausgleichen — das Ergebnis wäre mit einem Lauf
auf einer migrierten Datenbank nicht vergleichbar. Die Migration ist idempotent: auf einer
aktuellen Kopie ein No-op.

### `vergleich` — gegen die Referenz prüfen

```powershell
& $exe vergleich <refOrdner> <neuOrdner>
& $exe vergleich <refOrdner> <neuOrdner> --ohne Heizkessel.Quellwaerme,Weiterer.Schluessel
```

Exit-Code 0 = alles PASS, 1 = mindestens ein FAIL. Je Projekt werden die zehn größten
Abweichungen ausgegeben, sortiert nach dem Vielfachen der erlaubten Toleranz.

`--ohne` nimmt **ausdrücklich benannte** Schlüssel vom Vergleich aus und nennt sie in der
Ausgabe. Der Zweck ist eng: Eine neue **Ergebnisspalte** lässt `aggregate.csv` zwangsläufig um
einen Schlüssel wachsen, und diese Meldung verdeckt gegen die eingefrorene Basis die
eigentliche Frage — *sind die Altwerte unverändert?* Dafür ist die Option da, **nicht** um
Abweichungen wegzuschalten; ist die Basis neu gesetzt, laufen die Vergleiche wieder ohne
Ausschluss.

### `pruefen` — Plausibilität eines Laufs

```powershell
& $exe pruefen <ordner>
```

Prüft Rasterlänge (8760 oder 35040 Zeilen), NaN/Inf und Jahressummen größer null dort, wo
dem Projekt ein Modul zugeordnet ist. Ein aktiviertes Gewerk ohne Modul ergibt zwangsläufig
null und wird nur als Hinweis gemeldet.

### `liste` — Projektlandschaft ansehen

```powershell
& $exe liste                                 # legt die Arbeitskopie neu an
& $exe liste C:\Waermeplan\Paket7_Nach\DB_Basis   # liest eine vorhandene Kopie
```

Zeigt alle Projekte mit Ausstattung und die automatische Auswahl samt Begründung, ohne zu
rechnen. Mit Ordnerargument wird **nichts kopiert** — so lässt sich die Auswahl auf einer
eigenen Kopie außerhalb des Repos nachprüfen, ohne die `Arbeitskopie` eines laufenden
Vergleichs zu überschreiben.

## Toleranzen

Für Skalare und für jedes einzelne Vektorelement gilt dieselbe Regel:

| Wertebereich | Toleranz |
|---|---|
| Betrag ≥ 1 | relative Abweichung bis **1e-4** |
| Betrag < 1 | absolute Abweichung bis **0,01** |

Nichtnumerische Werte (Modulnamen, Schalter wie `Sim_Waermepumpe`) müssen exakt
übereinstimmen. Fehlende oder zusätzliche Dateien und Einträge gelten als FAIL.

Volatile Größen sind bewusst nicht Teil des Vergleichs: die Autowert-IDs der
`Tab_Ergebnis*`-Zeilen und der Zeitstempel des Laufs.

## Ablauf vor einer Änderung an der Engine (Paket 1 ff.)

Zwei gleichwertige Wege der Windows-Suite. **Weg B** ist zwingend, wenn parallel gearbeitet
wird oder die Kopie außerhalb des Repos liegen soll.

### Weg A — mit `lauf` (bequem, benutzt `Referenzlaeufe\Arbeitskopie`)

1. **Sauberen Ausgangszustand herstellen.** Anwendung schließen, Arbeitsverzeichnis auf dem
   Stand, gegen den verglichen werden soll.
2. **Änderung umsetzen** und die Anwendung neu bauen (`WP-Plan.sln` **und**
   `Referenzlauf.csproj`).
3. **Neu rechnen und vergleichen.** Die einzige Basis im Arbeitsbaum,
   `2026-09-11_R7_Speicherflotte`, ist plattformfrei gegen `Kenndaten_Test.sqlite` gerechnet:
   Wer auf Windows gegen die produktive Datenbank misst, friert **vor** der Änderung selbst
   einen Stand ein und vergleicht gegen diesen. **`--projekte` ist Pflicht**:
   ```powershell
   & $exe lauf --ziel C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-09-12_VorUmbau `
               --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030
   # ... Änderung umsetzen, neu bauen, dann derselbe Lauf nach 2026-09-12_NachUmbau ...
   & $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-09-12_VorUmbau `
                    C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-09-12_NachUmbau
   ```
   `lauf` kopiert **und migriert** die Arbeitskopie selbst.

### Weg B — eigene Kopie außerhalb des Repos (`migration` + `projekt`)

```powershell
# 1. Eigene, vollständig migrierte Kopie anlegen (schreibt NIE in die produktive DB)
& $exe migration C:\ProgramData\EPOS_PLAN\Kenndaten.accdb C:\Waermeplan\MeinTest\DB

# 2. Auswahl kontrollieren (rein lesend, kopiert nichts)
& $exe liste C:\Waermeplan\MeinTest\DB

# 3. Die NEUN Referenzprojekte einzeln rechnen (feste Liste, nicht die Automatik)
foreach ($id in 1007,1008,1011,1017,1018,1021,1023,1024,1030) {
    & $exe projekt $id "C:\Waermeplan\MeinTest\Lauf\Projekt_$id" C:\Waermeplan\MeinTest\DB
}

# 4. Gegen den eingefrorenen Ausgangsstand vergleichen und plausibilisieren
& $exe vergleich C:\Waermeplan\WP_Plan\Referenzlaeufe\2026-09-12_VorUmbau C:\Waermeplan\MeinTest\Lauf
& $exe pruefen   C:\Waermeplan\MeinTest\Lauf
```

Der Modus `projekt` migriert **nicht** — er erwartet eine fertige Kopie aus Schritt 1.
Ohne Schritt 1 rechnet er auf einem unvollständigen Schema.

> **Schritt 1 ist keine Bequemlichkeit.** Er ist der Grund, warum die Anwendung auf der Kopie
> dieselben Werte rechnet wie auf der gepflegten Datenbank. Beispiel `Extrapolation_erlaubt`:
> Die **Spalte** entsteht schon in Migrationsschritt 2 und wird auch von der stillen
> Rückfallebene `WaermequelleClass.SchemaSicherstellen` angelegt — mit `False`, also
> „Extrapolation verboten"; ihre **Vorbelegung auf WAHR** setzt erst Schritt 7. Der Leser
> fängt das ab (unter Schemastand 7 gilt `False` als Datenlücke und wird als „erlaubt"
> gelesen) — wer die Einstellung wirklich prüfen will, braucht dennoch eine migrierte Kopie.

### Danach

**Abweichungen bewerten.** Jede gemeldete Abweichung ist entweder gewollt — dann im
Umsetzungsprotokoll begründen und den neuen Ordner zur Referenz erklären — oder ein
Fehler.

Wichtig: Beide Läufe müssen von derselben Quelldatenbank ausgehen. Ändern sich zwischendurch
die Projektdaten, vergleicht man Äpfel mit Birnen. Die Quelle steht im Kopf von
`lauf_protokoll.md`.

## Die Projektauswahl

**Für jeden Vergleichslauf gilt die feste Liste. `--projekte` ist Pflicht** — eine einzige
Liste, auf beiden Seiten des Vergleichs dieselbe:

```powershell
# Die feste Liste der Windows-Läufe:
& $exe lauf --projekte 1007,1008,1011,1017,1018,1021,1023,1024,1030,1039,1040,1041,1042
```

Kern der Liste sind die neun Projekte 1007, 1008, 1011, 1017, 1018, 1021, 1023, 1024 und das
Kaskadenprojekt **1030**, dazu **1039** und die drei Konzept-11.1-Projekte **1040 (zwei
Puffer je Kanal), 1041 (Prozesswärme mit eigenem Puffer) und 1042 (Booster-Kette mit
Kombi-Speicher)**. Die weiteren Booster-Varianten **1043** und **1044** gehören **nicht**
zur festen Liste; sie aufzunehmen wäre ein bewusster Basiswechsel, kein Nebenbei-Schritt.
Wer die Liste
wegläßt, bekommt einen Ordner, der sich mit der Basis nicht vergleichen läßt — der
Vergleich meldet dann fehlende und zusätzliche Projekte, nicht Rechenabweichungen.

### Warum nicht die Automatik

Ohne `--projekte` wählt die Suite selbst, deterministisch und aus der Arbeitskopie heraus: erst
**sieben** Pflichtkategorien — Wärmepumpe mit Pufferspeicher, Heizkessel, BHKW, Solarthermie,
der Minimalfall „nur Wärmepumpe", Wärmepumpe mit **Quellspeicher**, **BHKW-Kaskade mit
mehreren Modulen** —, dann auf neun Projekte aufgefüllt (neue Erzeugerkombinationen vor
abweichender Anlagenausstattung). Übergangen werden Projekte ohne Eintrag in
`Tab_Einstellungen` und ohne Klimaregion; die stehen mit Begründung im Protokoll.

**Diese Auswahl ist datengetrieben und wandert mit dem Projektbestand** — das ist der Grund
für die feste Liste: Mit den Beispielprojekten 1026–1029 zieht die Automatik **1012** („nur
Wärmepumpe") und **1026** (Pflichtkategorie Heizkessel) herein und läßt **1008** und **1018**
fallen; eine so entstandene Basis ließe sich mit keiner früheren mehr vergleichen. Die
Automatik sichtet die Projektlandschaft (`liste`), sie friert keine Basis ein.

> **Das Kaskadenprojekt 1030 ist der Anker für Mehrmodul-BHKW.** Es ist das einzige Projekt
> der Referenzmenge mit zwei BHKW-Modulen, gepflegtem KWKG-Satz und gepflegten Energiepreisen
> und deckt damit als einziges die drei Vollbenutzungsstunden-Aggregate aus E2, die bindende
> KWKG-Deckelung und die Positivseite der beiden KWKG-Guards (500-kW-Grenze, Heizöl) ab.
> Die Zahlen dazu standen im Laufprotokoll der Basis `2026-08-19_B6`; die Basis ist gelöscht,
> ihr Protokoll ist nicht gesichert.

> **Projekt 1010 „Kurs EE" gibt es nicht mehr** — es war die Kategorie **„nur Wärmepumpe"**,
> die in der festen Liste damit unbesetzt ist; die Automatik füllt sie mit 1012. Ein
> Nachrücken in die feste Liste wäre ein bewußter Basiswechsel und kein Nebenbei-Schritt.

## Dialoge der Engine

**Seit Paket 8 zeigt die Engine keine MessageBoxen mehr** (Konzept Kapitel 13.4). Grenz- und
Fehlerfälle laufen über den Protokollkanal `SimulationProtokoll`; jeder Eintrag geht zusätzlich auf
die Konsole und steht damit im `lauf_protokoll.md`:

```
Simulation Hinweis:  vollwertig gerechnet, Randbedingung erwähnenswert
Simulation Warnung:  gerechnet, aber mit einer Ersatzannahme
Simulation FEHLER:   Lauf abgebrochen, es wird kein Ergebnis gespeichert
```

Die Rückfrage nach der Extrapolation ist eine **Projekteinstellung** (`Extrapolation_erlaubt`,
Vorbelegung WAHR — genau die Antwort, die in jedem dokumentierten Lauf gegeben wurde): Statt
eines weggeklickten Dialogs steht eine `Simulation Hinweis:`-Zeile im Protokoll, derselbe
Rechenweg, nur sichtbar.

Der **Dialogwächter läuft trotzdem mit**: Er findet Dialogfenster des eigenen Prozesses und
drückt den bejahenden Knopf (Ja vor OK vor Ignorieren). Er hat nichts mehr zu drücken — und ist
genau deshalb wertvoll: Taucht im Lauf-Protokoll doch ein Eintrag auf, ist eine MessageBox in
den Rechenpfad zurückgekommen, und das ist ein Befund.

Der Zähler des Protokolls wertet die Konsolenausgabe der Kindprozesse aus und kennt beide
Schreibweisen — `WARNUNG:` (Suite) und `Simulation Warnung:` (Engine). Hinweise zählt er
bewusst nicht mit: Sie melden einen vollwertig gerechneten Grenzfall.

Bleibt ein Projekt trotzdem hängen, greift das Zeitlimit: Jedes Projekt läuft in einem eigenen
Kindprozess, der nach Ablauf abgeräumt wird; die halbfertige Ausgabe wird gelöscht, das
Projekt im Protokoll als übersprungen vermerkt, die übrigen laufen weiter.

## Aufräumen

Ein Lauf belegt rund 30 MB (neun Projekte). Die CSVs gehören ins Git — sie sind die Referenz —, alte
Laufordner dagegen nicht auf Dauer. Nicht mehr benötigte Ordner löschen, statt sie
anzusammeln. `Arbeitskopie/` bleibt ohnehin außen vor: `Kenndaten.accdb` steht in
`.gitignore`.
