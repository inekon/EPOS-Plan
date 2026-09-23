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
`protokoll.txt` der Basis `2026-09-11_R7_Speicherflotte`
([`Dokumentation/ueberholt/Referenzbasen/`](../Dokumentation/ueberholt/Referenzbasen/LIESMICH.md)).

## Die Einfrierregel „gesäte Gebäudedaten“ (Q22, Entscheid E4)

Dieselbe Klasse von Falle, vierter Ort. Die Regel trägt bewusst keine Ordnungszahl: Die
Einfrierregeln der Gebäudesimulation werden nach ihrem Gegenstand benannt, nicht
durchgezählt (Softwarearchitektur Gebäudesimulation, W18). Den Heizkanal jedes Referenzprojekts mit Gebäude
rechnet die Gebäudesimulation (`SimulationWaermebedarf.HeizwaermeEinesGebaeudes`: VDI 6007,
bei 1040 der Tagesbilanz-Weg), und sie liest die Gebäudezeile Spalte für Spalte. Eine einzige geänderte Zahl verschiebt damit den
Wärmebedarf und mit ihm Wärmepumpe, Kessel, Puffer, Emissionen und Wirtschaftlichkeit des
Projekts — die Korrektur der `Bauweise` von Gebäude 10576 hat den Wärmebedarf von Projekt 1008
um 41 % bewegt (Basis `2026-09-22_R11_Bestandsbefunde`).

> **Wer gesäte Gebäudedaten der Testdatenbank ändert, friert im selben Schritt die Basis neu
> ein und begründet den Wechsel hier.**
>
> Betroffen ist jede Änderung an `Tab_Gebaeude` und `Tab_Gebaeude_STAMM` in den Spalten
> `Bauweise`, den U-Werten (`k_Wert_*`), den Flächen (Außenwand, Fenster je Richtung und
> gesamt, Dach, Grundfläche, Sonstige, `Wohnflaeche`, `Wohnflaeche_gesamt`,
> `Flaeche_Nutzer`) samt Wärmebrücken (`WBVK_*`, `Abmessung_*`), den Sollwerten
> (`Raumsolltemperatur_*`, `Maximaleraumtemperatur`, Wochenend- und Ferienfahrplan),
> `Raumhoehe`, `Interne_Waermegewinne`, `Luftwechselrate`, `Fensterdurchlassgrad` und `Typ`
> (er wählt die Tagesverteilung) — und **ab G1** zusätzlich an `Gebaeude_Modell`,
> `Fensterflaeche_Ost/West`, `Rahmenanteil`, `Verschattungsfaktor`,
> `Grundflaeche_Randbedingung`, `Kellertemperatur`, `Masseanteil_Aussen`,
> `Innenflaechenfaktor`, `Heizung_Strahlungsanteil`, `Heizleistung_Max`,
> `Aussenbauteile_Strahlung` und den drei G2-Spalten. Ebenso betroffen sind die
> Gebäudezuordnungen `Z_ProjektGebaeude` der Referenzprojekte (Fläche bzw. Verbrauch,
> Einheit, Jahresnutzungsgrad), die Tagesverteilungen ihrer Gebäude und das Anlegen oder
> Entfernen eines Gebäudes in einem Referenzprojekt.
>
> **Rechenwirkung hat die Projektkopie in `Tab_Gebaeude`** — der Lauf liest sie über
> `Abfrage_Projektgebaeude`: fünfzehn Gebäudezeilen in zwölf der dreizehn Referenzprojekte
> (1030 hat kein Gebäude). `Tab_Gebaeude_STAMM` erreicht den Lauf erst, wenn ein
> Katalogsatz in ein Referenzprojekt übernommen wird; er steht in der Regel, damit eine
> Katalogpflege nicht unbemerkt in ein neu angelegtes Referenzgebäude wandert.
>
> **Nicht** betroffen sind Gebäudezeilen von Projekten außerhalb der Referenzliste.

Die Korrektur von 10576 entsteht wiederholbar aus
[`Skripte/gebaeude_10576_bauweise.py`](Skripte/gebaeude_10576_bauweise.py), der Rechenweg
`TAGESBILANZ` des Referenzprojekts 1040 (Gebäude 10645, A15) aus
[`Skripte/gebaeude_1040_tagesbilanz.py`](Skripte/gebaeude_1040_tagesbilanz.py). Diese Zelle
fällt mit der Stufe GA der Gebäudesimulation; bis dahin ist 1040 das einzige Referenzprojekt
auf dem Tagesbilanz-Weg.

## Entfernte Basen

**`Referenzlaeufe/Importproben` gehört zum Testbestand und wird nie gelöscht; wer die Ordner der
Basen aufräumt, lässt `2026-09-23_R12_Gebaeudemodell`, `Kenndaten_Test.sqlite`,
`Importproben`, `Skripte` und `LIESMICH.md` stehen.**

Außer der aktuellen liegt hier keine Basis mehr: Die 24 historischen Referenzbasen (7 731
Dateien, 1 016,7 MB) sind am 11.09.2026 aus dem Arbeitsbaum gefallen (**SYNC‑Q1**: „entfernt
lassen") und seit dem Umschreiben der Git-Geschichte (**AUF‑Q1**, 12.09.2026) auch dort nicht
mehr enthalten; **`2026-09-11_R7_Speicherflotte` ist am 16.09.2026 nach demselben Muster
gefallen, `2026-09-16_R8_Heizkessel_Kaskade` am 18.09.2026,
`2026-09-18_R9_Kesselbrennstoff` am 19.09.2026 und `2026-09-19_R10_BhkwWirkungsgrad` am
22.09.2026 und `2026-09-22_R11_Bestandsbefunde` am 23.09.2026** (29 Basen, alle fünf Protokolle gesichert). Kein Test, kein Gate, keine CI liest
eine entfernte Basis. **Die Messdaten sind endgültig weg** (rund 8 000 CSV-Dateien) — eine
alte Zahl steht nur noch im Protokoll.
Erhalten sind die **Protokolle** aller 29 Basen samt der Tabelle Basis → Datum → Zweck →
Protokoll unter
[`Dokumentation/ueberholt/Referenzbasen/`](../Dokumentation/ueberholt/Referenzbasen/LIESMICH.md);
**welche Basis wann von welcher abgelöst wurde und warum**, steht ebendort — bis zum 12.09.2026
in der ausgelagerten
[Basenhistorie](../Dokumentation/ueberholt/Referenzbasen/LIESMICH_Basenhistorie_bis_2026-09-12.md),
danach im Wegweiser desselben Ordners.

## Aktuelle Basis

**`2026-09-23_R12_Gebaeudemodell/`** — **dreizehn Projekte** (1007, 1008, 1017, 1018, 1023,
1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046), **399 CSV**, **2 239 Skalare**, gerechnet mit
dem plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite`
(Schemastand **103**). Gegen diese Basis hält `.github/workflows/kern.yml` (1030, 1007, 1017,
1045, 1046) jeden Push, `ios.yml` den iZ6-Vergleich für 1030, und
`EPOS.Kern.Tests/GebaeudeRueckwegTests` den Tagesbilanz-Weg an Projekt 1040. Sie ist die
**einzige** Basis im Arbeitsbaum.

> **Anlass: die Schlusswelle G1 + G2 der Gebäudesimulation** (Entscheide E1/Q14, A15 mit E27;
> vom Anwender am 23.09.2026 beauftragt). **VDI 6007 ist das Vorgabemodell aller Gebäude:** Ein
> Gebäude ohne Angabe (`Gebaeude_Modell` NULL) rechnet stündlich nach dem 2-K-Modell
> (`EPOS.Kern/Allgemein/Simulation/Gebaeude/`), nicht mehr auf dem Tagesbilanz-Weg. Zwei
> Änderungen, eine davon an den Daten:
>
> 1. **Die NULL-Regel der Weiche** (`SimulationWaermebedarf.MODELL_OHNE_ANGABE`) steht auf
>    `VDI6007`. Der Ergebnisexport schreibt je Gebäude des VDI-Wegs drei neue Reihen —
>    `raumtemperatur_<n>.csv` und `operative_temperatur_<n>.csv` in °C, `kuehlbedarf_<n>.csv` in
>    kWh (n = Merkplatz des Gebäudes im Lauf) — und die Skalare `Geb[n].ID_Gebaeude`,
>    `Geb[n].Modell`, `…JahresheizwaermeMwh`, `…SpitzeKw`, `…SpitzeTagesmittelKw`,
>    `…Spitze95Kw`, `…KuehlenergieMwh`, `…StundenMitKuehlbedarf`,
>    `…MittlereRaumtemperaturHeizzeit`, `…Ueberhitzungsstunden`. Ein Gebäude auf dem
>    Tagesbilanz-Weg erzeugt keinen dieser Einträge.
> 2. **Testdatenbank (A15):** `Tab_Gebaeude.Gebaeude_Modell` von Gebäude **10645** (Projekt
>    **1040**, einziges Gebäude) von NULL auf **`TAGESBILANZ`** — genau diese eine Zelle, über
>    [`Skripte/gebaeude_1040_tagesbilanz.py`](Skripte/gebaeude_1040_tagesbilanz.py). Der
>    Zellvergleich aller Tabellen vor und nach dem Skript (11 964 203 Zellen) zeigt genau diese
>    eine Abweichung; `integrity_check` ok, Schema und Schemastand unverändert.
>
> **Warum 1040 auf dem Altweg bleibt.** Bis zur Stufe GA („Altweg ablösen“) steht genau ein
> Referenzprojekt auf dem Tagesbilanz-Weg und hält den Rückweg-Test (Systementwurf
> Gebäudesimulation 8.4). 1040 hat ein einziges Gebäude, liegt nicht in den fünf Projekten der
> CI (deren Gebäude sollen den Vorgabeweg zeigen), und dasselbe Katalogobjekt „EFH-A-U-347s“
> steht in 1041, 1042 und 1045 auf dem VDI-Weg — die Basis führt beide Wege am selben Gebäude.
>
> **Elf Projekte bewegen sich, zwei bleiben byte-gleich:** **1040** (Altweg, alle 30 Dateien
> byte-gleich gegen R11) und **1030** (kein Gebäude; alle 22 Dateien byte-gleich). Die übrigen
> elf bekommen je Gebäude drei neue Dateien (42 Dateien für 14 Gebäudezeilen) und 13 neue
> Skalare; kein Schlüssel und keine Datei fällt weg.
>
> | Projekt | Gebäudewärme R11 [MWh/a] | R12 [MWh/a] | relativ | `Waermebedarf_Gesamt` R11 → R12 [MWh/a] | `Waermelast_Max` R11 → R12 [kW] |
> |---|---:|---:|---:|---:|---:|
> | 1007, 1046 | 53,07 | 69,07 | +30,2 % | 57,13 → 73,13 | 36,41 → 44,60 |
> | 1008 | 77,32 | 104,20 | +34,8 % | 77,32 → 104,20 | 51,37 → 72,69 |
> | 1017 | 62,96 | 90,19 | +43,2 % | 62,96 → 90,19 | 35,95 → 63,16 |
> | 1018 | 46,88 | 68,27 | +45,6 % | 46,88 → 68,27 | 34,10 → 37,36 |
> | 1023, 1024 | 329,80 | 450,56 | +36,6 % | 389,80 → 510,56 | 204,08 → 311,78 |
> | 1039 | 445,62 | 597,72 | +34,1 % | 466,62 → 618,72 | 257,71 → 382,10 |
> | 1041 | 59,35 | 75,98 | +28,0 % | 159,78 → 176,41 | 78,53 → 77,33 |
> | 1042 | 59,35 | 75,98 | +28,0 % | 89,35 → 105,98 | 45,38 → 44,77 |
> | 1045 | 59,35 | 75,98 | +28,0 % | 64,35 → 80,98 | 38,61 → 40,01 |
> | 1040 (Altweg) | 59,35 | 59,35 | ±0 | unverändert | unverändert |
> | 1030 (ohne Gebäude) | — | — | — | unverändert | unverändert |
>
> (Gebäudewärme = `Vektor.waermebedarf_gebaeude.Summe`; die Datei führt Watt je Stunde, die
> Summe ist also Wh — hier in MWh umgerechnet.)
>
> **Die Größenordnung ist die erwartete.** Konzept Gebäudesimulation 5.5 hatte mit dem
> Prototyp +7 bis +33 % gemessen, **ohne** den Abzug R_si/A in R_Rest,AW; der Abzug hebt die
> Jahresheizwärme um rund 12 % (Rechenschritte 9.6). Die Stufe G1 hatte im Speicher +25 bis
> +46 % gemessen — dieselben Zahlen wie hier je Gebäude (+24,9 % bei 10643 bis +45,6 % bei
> 10632). Die Jahresheizwärme trifft die Katalogkennzahl `spez_Waermeverbrauch` zu 95,8 bis
> 109,4 % (Tagesbilanz-Weg: 48 bis 88 %).
>
> **Auffälligkeiten, erklärt:**
>
> - **Die Spitzenlast steigt stärker als die Arbeit** (1017 +76 %, 1023/1024 +53 %, 1039
>   +48 %): Der ideale Heizer des VDI-Wegs deckt den Sprung vom Nacht- auf den Tagsollwert in
>   einer Stunde (Konzept 5.6: die Prototyp-Spitze lag bei 1017 schon +57 % über der des
>   Tagesmodells, dazu der R_si-Abzug). Bei 1041 und 1042 bestimmt der Prozess- bzw.
>   Brauchwasserkanal die Projektspitze; sie sinkt dort leicht (−1,5 % bzw. −1,3 %).
> - **Mehr ungedeckte Restwärme** in den Projekten mit knapp ausgelegten Erzeugern (1023
>   125 → 230 MWh/a, 1024 79 → 166, 1039 55 → 132, 1042 4,2 → 9,8, 1008 0,31 → 1,62) und
>   erstmals ein kleiner Rest in 1007/1046 (22 kWh in sechs Morgenstunden, höchstens 8,2 kW)
>   und 1017 (0,14 MWh/a in 34 Stunden) — dieselbe Morgenspitze trifft die festen Leistungen
>   der Erzeuger; der Erzeugerweg ist nicht angefasst.
> - **1018: Der Kessel liefert weniger, obwohl der Bedarf um 46 % steigt** (16,8 → 11,1
>   MWh/a). Das BHKW läuft 1 894 statt 1 016 Stunden und deckt 84,6 statt 66,1 %: Der VDI-Weg
>   hat eine physikalische Nachtgrundlast, wo das Tagesprofil die Nacht kappte, und ein
>   wärmegeführtes BHKW findet damit mehr Laufzeit (Stromproduktion 14,7 → 27,5 MWh/a).
> - **1041:** Die Wärmepumpe deckt dort den Prozesskanal; den Mehrbedarf des Gebäudes trägt
>   der Kessel (+12,5 %), der Strom der Wärmepumpe bleibt gleich.
>
> **Kein Fehlschlag, kein NaN, keine Ablehnung:** 13/13 Projekte gerechnet; keine Datei führt
> `NaN` oder `Inf`. Die Plausibilitätsprüfung des Klassenwegs lehnt kein Referenzgebäude ab;
> abgelehnt wird allein das Gebäude von Projekt 1009, das kein Referenzprojekt ist
> (`BauweiseUnplausibel`, 50 Wh/K auf 304 m²).
>
> **Einfrierregeln:** Berührt ist die Regel „gesäte Gebäudedaten“ (`Gebaeude_Modell` von
> 10645) — sie ist Teil dieses Einfrierens. Emissionsfaktoren, PV-Modulkoeffizienten und
> Flottenstand 1046 sind nicht berührt.
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **13/13 byte-gleich** (399/399
> CSV), Laufzeit 4 bis 5 s für alle dreizehn Projekte.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046 \
>   --ziel Referenzlaeufe/2026-09-23_R12_Gebaeudemodell
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis; die Abnahme der Stufe
> (Kriterien, Vergleichsläufe) im
> [Status der Gebäudesimulation](../Dokumentation/aktuell/Status_Gebaeudesimulation_VDI6007.md).

> **Nachträge nach der Einfrierung von R12 (Merge vom 23.09.2026):** Die Schritte 104 und 105 der Etappen E7b und E7c1
> wurden gegen die damalige Basis R11 nachgewiesen und beim Zusammenführen mit R12 erneut gerechnet:
> **Nachweis gegen R12 (Merge a1985884, 23.09.2026):** Referenzlauf aller dreizehn Projekte auf Schemastand 105 mit dem Testdaten-UPDATE gegen `2026-09-23_R12_Gebaeudemodell` — **13/13 PASS**, 4 250 839 Werte innerhalb der Toleranz, 399 von 399 CSV byte-gleich; voller Testlauf des Kern-Filters auf dem gemergten Stand 11 091 grün, 1 übersprungen. Die Basis R12 bleibt.

> **Nachtrag: Schemastand 104 (Auftrag #439, Etappe E7b), die Basis bleibt.** Migrationsschritt
> **104** (`SCHRITT_104_ZEITZONENTARIF_ABLOESUNG`, Quellen `SchemaKatalog.Schritt104_LeistungspreisStaffel`
> und `EPOS.Kern/Allgemein/Update/ZeitzonentarifAbloesung.cs`; Konzept Wirtschaftlichkeit § 3.5,
> Register Q11) legt drei REAL-Spalten `Leistungspreis_Staffelgrenze`, `Leistungspreis_Staffel1` und
> `Leistungspreis_Staffel2` an `energy_project_settings` an (DDL) und führt den Datenteil in einer
> Transaktion: Er übernimmt die Staffel aktiver Zonensätze an den Stromträger (in der Testdatenbank
> keiner), löscht die Zonensätze (keiner) und verwirft ihre gespeicherten Läufe (0 Zeilen); die acht
> Zonenzeilen der Strommatrix in den Projekten 1018 und 1031 werden zu je einer Jahreszeile
> zusammengefasst (Spalte `Zone` = „Jahr", gleiche Summe). `Tab_Applikation` trägt 104; die Größe
> bleibt 67 727 360 Byte. **Keine Einfrierregel ist berührt**, und der Referenzlauf ist **13/13
> byte-gleich** gegen diese Basis (357/357 CSV, auf 103 wie 104 gerechnet). Nachgezogen mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`
> (Commit E7b/11 `bfbfbbb9`, LFS-SHA-256 `044e44db…`).

> **Nachtrag: Schemastand 105 (Auftrag #440, Etappe E7c1), die Basis bleibt.** Migrationsschritt
> **105** (`SCHRITT_105_KWKG_ABWAERMEABFUHR`, Quelle `SchemaKatalog.Schritt105_KwkgAbwaermeabfuhr`; Konzept
> Wirtschaftlichkeit § 3.6, Befund K‑1) legt an `Tab_Energieanlagen` zwei Spalten an — `KWKG_Abwaermeabfuhr`
> (INTEGER NOT NULL DEFAULT 0, `CHECK` 0/1) und `KWKG_Stromkennzahl` (REAL, nullbar) —, **reines DDL**; die
> Tabelle bleibt `STRICT`, jede Bestandsanlage steht auf 0 (Fall 1) bzw. NULL. Dazu sät das Werkzeug die
> Katalog-Generation 8 nach: **eine Zeile** `KWKG_INBETRIEBNAHME_FRISTENDE` (2030 — Ende der Frist zur
> Inbetriebnahme, Quelle KWKG 2025 § 6) in `Tab_Gesetzesparameter`. Mit demselben Commit das
> **Testdaten-UPDATE** nach Entscheid E7‑Q1 (Lesart b): Die Anlagen 14920 und 14921 des Projekts 1030 tragen
> `KWKG_Anlagenart` 'NEUANLAGE' statt NULL (nur die Neuanlage erreicht 30.000 Vbh ohne Kostenanteil); ihr
> Kontingent 30.000 h bleibt gepflegt, **kein Anker bewegt sich**. `Tab_Applikation` trägt 105; die Größe
> bleibt 67 727 360 Byte, `quick_check` ok. **Keine Einfrierregel ist berührt**, und der Referenzlauf ist
> **13/13 byte-gleich** gegen diese Basis (357/357 CSV, 3 882 737 Werte, mit und ohne das UPDATE gerechnet).
> Nachgezogen mit `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`,
> das UPDATE als SQL außerhalb des Repos (Commit E7c1/9 `ccf9f22f`, LFS-SHA-256 `66aa52b0…`).

> **Die Vorgängerbasis `2026-09-22_R11_Bestandsbefunde`**, die letzte Basis allein auf dem
> Tagesbilanz-Weg, ist mit dieser Einfrierung aus dem Arbeitsbaum gefallen; ihr Protokoll samt
> der Begründung zur Stufe GB und den Nachträgen zu den Schemaständen 101 bis 103 steht in
> [`Dokumentation/ueberholt/Referenzbasen/`](../Dokumentation/ueberholt/Referenzbasen/LIESMICH.md).
> Gerechnet wird ausschließlich gegen die aktuelle Basis.

## Was hier liegt

| Pfad | Inhalt |
|---|---|
| `<yyyy-MM-dd>_<Marke>/` | Ein eingefrorener Lauf: je Projekt ein Unterordner `Projekt_<ID>/`, dazu `lauf_protokoll.md` bzw. `protokoll.txt` |
| `<...>/Projekt_<ID>/aggregate.csv` | Alle Skalare des Laufs: `Tab_Ergebnis*`-Zeilen, Restgrößen aus `SimulationControl`, Jahressumme jedes Vektors |
| `<...>/Projekt_<ID>/*.csv` | Die Ganglinien: 8760 Stundenwerte bzw. 35040 Viertelstundenwerte, `Index;Wert` |
| `Arbeitskopie/` | Die Kopie der Datenbank, auf der gerechnet wird. Wird bei jedem `lauf` neu angelegt. Nicht im Git (`Kenndaten.accdb` ist in `.gitignore`) |
| `Kenndaten_Test.sqlite` | Die reduzierte Testdatenbank, gegen die der plattformfreie `EPOS.Referenzlauf` und der SQL-Dialektprüfer laufen. **Versioniert** — eine Änderung daran gehört in einen eigenen Commit |
| `Skripte/` | Was an dieser Testdatenbank gemacht wurde, als Skript und nicht als Erzählung: `pruefprojekt_1045_ost_west.py` (W6‑O‑7), `pruefprojekt_1046_speicherflotte.py` (SP‑O‑8), `gebaeude_10576_bauweise.py` (Stufe GB, Befund D) und `tww_testkatalog_fiktiv.py` (fiktiver Katalog des Zapfprofilgenerators, Schemastand 103) |

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
   `2026-09-23_R12_Gebaeudemodell`, ist plattformfrei gegen `Kenndaten_Test.sqlite` gerechnet:
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
