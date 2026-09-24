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

**Anschluss an die Nutzungsdauertabelle (E10, #463).** Die Wirtschaftlichkeit der Flottenstudie
rechnet den Restwert je Einheit linear aus ihrer Nutzungsdauer (Ersatzintervall) auf der
Ersatzkette der Flotte; eine Einheit ohne eigenes Intervall nimmt die Nutzungsdauer der
Standardzeile „Stromspeicher · Batterie" der Nutzungsdauertabelle, und der feste Restwert je Einheit
ist ein Altfeld, das nicht mehr rechnet. Zur Regel gehören damit auch **Ersatzintervall und
Ersatzkosten der Einheiten von `@Projektflotte`** und — sobald eine Einheit ohne eigenes Intervall
rechnet — **die Nutzungsdauer der Zeile „Stromspeicher · Batterie"**. Die Basis führt heute keine
Flottenwirtschaftlichkeit (`aggregate.csv` von 1046 trägt nur die Physik), eine Änderung dort
bewegt sie also nicht; die Zahlen hält `EPOS.Kern.Tests/SpeicherFlottenNutzungsdauerTests`
(1046: Restwert 800 → 7 000 €, Kapitalwert +3 432,79 €). Wer diese Größen ändert, zieht den
Test nach, rechnet den Referenzlauf und friert neu ein, sobald die Basis sich bewegt.

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

**Datenwechsel 23.09.2026 — Basis unverändert.** Der Katalogsatz `Tab_Gebaeude_STAMM` 233
„MFH-H-U-112" und seine Projektkopie `Tab_Gebaeude` 10612 (Projekt 1009) trugen denselben
falschen `Bauweise`-Wert 50 Wh/K wie früher 10576; beide stehen nach derselben Herleitung
(50 Wh/(m²K) × 304 m²) auf 15 200 Wh/K —
[`Skripte/gebaeude_10612_233_bauweise.py`](Skripte/gebaeude_10612_233_bauweise.py), genau zwei
Zellen (Zellvergleich aller Tabellen), `integrity_check` ok. **Die Basis bleibt, weil keine der
beiden Zeilen in einem Referenzprojekt rechnet:** 10612 gehört zu Projekt 1009, und der Lauf
liest die Projektkopien, nicht den Katalog. Belegt durch den Referenzlauf aller dreizehn
Projekte: **PASS und byte-gleich** (außer `protokoll.txt`). Projekt 1009 wird seither nicht
mehr mit `BauweiseUnplausibel` abgelehnt.

## Die Einfrierregel „gesäte Kältedaten“ (Kühlkonzept 10.4)

Dieselbe Klasse von Falle, fünfter Ort, und wie die Gebäudedaten nach ihrem Gegenstand benannt, nicht
durchgezählt (Systementwurf Gebäudesimulation 8.3). Den Kühlkanal eines Referenzprojekts rechnet die
Gebäudesimulation aus den Kühleingaben seiner Gebäude, und nur, wenn der Projektschalter steht;
ohne wirksame Kühlung läuft ein Gebäude frei (E32). Eine einzige geänderte Zelle schaltet damit die
Kälteseite eines Projekts ein oder aus, verschiebt seine Raumtemperatur, seine Heizwärme in der
Übergangszeit und seine Überhitzungsstunden — und lässt Dateien und Schlüssel im Export entstehen
oder verschwinden.

> **Wer gesäte Kältedaten der Testdatenbank ändert, friert im selben Schritt die Basis neu ein und
> begründet den Wechsel hier.**
>
> Betroffen ist jede Änderung am Projektschalter `Tab_Einstellungen.Kuehlbetrieb` eines
> Referenzprojekts, an den Kühleingaben seiner Gebäude (`Kuehlung_Aktiv`, `Kuehl_Sollwert`,
> `Kuehl_Sollwert_Nacht`, `Kuehlleistung_Max` in `Tab_Gebaeude`; in `Tab_Gebaeude_STAMM`, sobald
> ein Katalogsatz in ein Referenzprojekt übernommen wird) und an der Kanalzuordnung „Kühlung“ eines
> Lastgangs eines Referenzprojekts (`Z_ProjektWaermebedarf.Kanal`) — und, sobald ein Kälteerzeuger
> rechnet, an der gesäten Kühlleistung, der Kühlkennlinie samt ihren Vorlauf-Stützstellen und dem
> `Kuehl_Vorlauf` einer Anlage eines Referenzprojekts.
>
> **Rechenwirkung hat allein Projekt 1017:** Projektschalter ein, Gebäude 10599 mit Haken,
> Kühlsollwert 24 °C und Kühlleistungsgrenze 15 kW — vier Zellen aus
> [`Skripte/kuehlung_1017_referenzprojekt.py`](Skripte/kuehlung_1017_referenzprojekt.py). Die
> übrigen zwölf Referenzprojekte stehen auf 0; ihre Gebäude laufen frei.
>
> **Nicht** betroffen sind Kühleingaben von Projekten außerhalb der Referenzliste.

## Abgeleitete VDI-Werte im Tww-Testkatalog (Anwenderentscheid ZU19)

Der Tww-Katalog der Testdatenbank ist fiktiv (Umsetzungskonzept Zapfprofilgenerator, Kapitel 6 (b))
— mit einer Ausnahme, die der Anwender am 23.09.2026 entschieden hat: **geringfügig abweichende
VDI-Werte dürfen ins Repositorium.** Vier Nutzungsarten „… (abgeleitet)“ (Wohnen groß,
Studentenwohnheim, Seniorenheim, Krankenhaus) tragen samt eigenem Tagesgangsatz Bedarfswerte,
Monatsfaktoren, Wochenanteile und Tagesgänge, die aus VDI 6002 Blatt 1 und 2 abgeleitet sind;
Herkunftsart `FIKTIV` (Testdaten nach einer Regel — weder Eigenkonstruktion noch Normwert),
Quelle „VDI 6002 Blatt n (abgeleitet)“. Wie jede `FIKTIV`-Zeile bleiben sie aus der
Auslieferungsvorlage (`TwwKataloge.Bereinigen`); ob abgeleitete Werte je ausgeliefert werden, ist
die offene Frage ZU20. Ihre Zapfkategorien sind — wie die jeder Nutzungsart des Testkatalogs — der
Vorgabesatz des freien Paketteils (Abschnitt „Der freie Paketteil“).

- **Die Regel** steht im Kopf von
  [`Skripte/normzahlen_abgeleitet_bauen.py`](Skripte/normzahlen_abgeleitet_bauen.py): jeder Wert
  v der Datenzeile i wird v · (1 + δ) mit δ zyklisch aus (+0,04; −0,03; +0,05; −0,04; +0,03;
  −0,05), gerundet auf die Stellenzahl der Quelle (mindestens zwei signifikante Ziffern);
  Tagesgänge und Wochenanteile werden auf Summe 1, Monatsfaktoren auf Mittel 1 renormiert; kein
  Wert gleicht seinem Original, jeder liegt höchstens 5,9 % davon entfernt (sonst das nächste δ,
  dann eine Stelle feiner). Deterministisch; ein zweiter Lauf schreibt dieselben Bytes.
- **Das Skript läuft nur lokal** — es liest die gitignorierten Originale unter
  `Normzahlen/vdi6002/` und schreibt die committete Datei
  [`Skripte/tww_katalogwerte_abgeleitet.json`](Skripte/tww_katalogwerte_abgeleitet.json) (497 Werte,
  kein Originalwert). Das Einspielskript
  [`Skripte/tww_testkatalog_fiktiv.py`](Skripte/tww_testkatalog_fiktiv.py) liest nur diese Datei
  und läuft ohne die Originale; die Bedarfswerte rechnet es von Litern bei 60 °C mit
  c_w = 1,163 Wh/(l·K) auf kWh bei den Bezugstemperaturen 60/12 °C der Zeile um.
- **Die Wache** `EPOS.Kern.Tests/TwwKatalogWacheTests.Kein_abgeleiteter_Katalogwert_gleicht_dem_VDI_Original`
  prüft lokal — nur wenn `Normzahlen/vdi6002/` beiliegt, sonst schweigt sie —, dass kein Wert der
  Testdatenbank und der JSON-Datei seinem Original gleicht und jeder innerhalb ±6 % liegt; ihre
  Meldung nennt Abweichungen, nie einen Absolutwert.
- **Nicht abgeleitet** werden VDI 4655 (folgt mit Stufe Z4b unter derselben Regel) und die
  DIN-Profile: A100-Referenzprofil und DIN-4708-Profil bleiben gesperrt (K1/K8).
- **Ergebnisneutral:** Kein Referenzprojekt steht auf dem Zapfprofilgenerator; die Basis bleibt.

## Der freie Paketteil (`Katalogpaket_frei/`)

Die freien Katalogdaten des Zapfprofilgenerators — Zapfkategorien nach Jordan/Vajen (IEA SHC
Task 26, Modellannahme bis Z5), die fünf Parameter `Zapfprofil.Stochastik.*` und das
Ecodesign-Zapfprofil L (Verordnung (EU) Nr. 814/2013 Anhang III) — stehen einmal im Repositorium,
als CSV-Dateien im Paketformat N2 unter [`Katalogpaket_frei/`](Katalogpaket_frei/LIESMICH.md)
(Aufbau, Regeln und Quellen dort). `Werkzeuge/Auslieferungsvorlage` spielt den Ordner in jede
Vorlage ein (Herkunftsart `FREI`, Status `AUSLIEFERUNG`, `ReadOnly` 1);
[`Skripte/tww_testkatalog_fiktiv.py`](Skripte/tww_testkatalog_fiktiv.py) schreibt dieselben Zeilen
in die Testdatenbank — nach deren Regel mit Status `EIGEN`, `ReadOnly` 0 und Katalogversion
`TEST-1`; die Zapfkategorien als Vorgabesatz an jeder Nutzungsart. Die Wache
`TwwKatalogWacheTests.Die_freien_Zeilen_der_Testdatenbank_gleichen_dem_Paketteil` hält beide
gleich, Wert für Wert und in der Anzahl. Wer eine Datei des Paketteils ändert, lässt das Skript
im selben Schritt auf die Testdatenbank laufen.

## Entfernte Basen

**`Referenzlaeufe/Importproben` gehört zum Testbestand und wird nie gelöscht; wer die Ordner der
Basen aufräumt, lässt `2026-09-23_R13_Kuehlung`, `Kenndaten_Test.sqlite`,
`Importproben`, `Katalogpaket_frei`, `Skripte` und `LIESMICH.md` stehen.**

Außer der aktuellen liegt hier keine Basis mehr: Die 24 historischen Referenzbasen (7 731
Dateien, 1 016,7 MB) sind am 11.09.2026 aus dem Arbeitsbaum gefallen (**SYNC‑Q1**: „entfernt
lassen") und seit dem Umschreiben der Git-Geschichte (**AUF‑Q1**, 12.09.2026) auch dort nicht
mehr enthalten; **`2026-09-11_R7_Speicherflotte` ist am 16.09.2026 nach demselben Muster
gefallen, `2026-09-16_R8_Heizkessel_Kaskade` am 18.09.2026,
`2026-09-18_R9_Kesselbrennstoff` am 19.09.2026, `2026-09-19_R10_BhkwWirkungsgrad` am
22.09.2026, `2026-09-22_R11_Bestandsbefunde` und `2026-09-23_R12_Gebaeudemodell` am 23.09.2026** (30 Basen, alle sechs Protokolle gesichert). Kein Test, kein Gate, keine CI liest
eine entfernte Basis. **Die Messdaten sind endgültig weg** (rund 8 000 CSV-Dateien) — eine
alte Zahl steht nur noch im Protokoll.
Erhalten sind die **Protokolle** aller 30 Basen samt der Tabelle Basis → Datum → Zweck →
Protokoll unter
[`Dokumentation/ueberholt/Referenzbasen/`](../Dokumentation/ueberholt/Referenzbasen/LIESMICH.md);
**welche Basis wann von welcher abgelöst wurde und warum**, steht ebendort — bis zum 12.09.2026
in der ausgelagerten
[Basenhistorie](../Dokumentation/ueberholt/Referenzbasen/LIESMICH_Basenhistorie_bis_2026-09-12.md),
danach im Wegweiser desselben Ordners.

## Aktuelle Basis

**`2026-09-23_R13_Kuehlung/`** — **dreizehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024,
1030, 1039, 1040, 1041, 1042, 1045, 1046), **387 CSV**, **2 207 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite` (Schemastand
**113**, LFS-SHA-256 `769143e4…`, Nachträge 114 bis 120 in diesem Abschnitt; die Katalog-Generation 9 aus Auftrag #452 ist enthalten und bewegt
kein Referenzprojekt — ihr Nachtrag steht beim R12-Abschnitt unter `ueberholt/`). Gegen diese Basis hält `.github/workflows/kern.yml` (1030,
1007, 1017, 1045, 1046) jeden Push, `ios.yml` den iZ6-Vergleich für 1030, und
`EPOS.Kern.Tests/GebaeudeRueckwegTests` den Tagesbilanz-Weg an Projekt 1040. Sie ist die
**einzige** Basis im Arbeitsbaum.

> **Nachtrag Schemastand 115 (Zapfprofilgenerator, Stufe Z3, Schemaschritt T2).** Die
> Testdatenbank steht auf Schemastand **115** (LFS-SHA-256 `fbc30835…`): Auf der Fassung mit
> Schemastand 114 (Kühlung, Nachtrag unten) hat Werkzeuge/Testdatenbankschema
> `Tab_TwwZapfkategorie_STAMM` angelegt (Schritt 115), und
> [`Skripte/tww_testkatalog_fiktiv.py`](Skripte/tww_testkatalog_fiktiv.py) hat 82 Katalogzeilen
> ergänzt — vier aus VDI 6002 abgeleitete Nutzungsarten samt Tagesgangsätzen (Herkunftsart `FIKTIV`,
> ZU19, Abschnitt „Abgeleitete VDI-Werte im Tww-Testkatalog“) und aus dem freien Paketteil
> (Abschnitt „Der freie Paketteil“) das Ecodesign-Zapfprofil L mit 24 Ereignissen, die fünf Parameter
> der Stochastik und den Vorgabesatz der Zapfkategorien an jeder der sieben Nutzungsarten (28 Zeilen,
> Herkunftsart `FREI`). Ein zweiter Skriptlauf meldet 0/0; `integrity_check` ok, `foreign_key_check`
> leer. Kein Referenzprojekt steht auf dem Generator: Der Referenzlauf der fünf CI-Projekte gegen
> diese Basis ist **byte-gleich**, die Basis bleibt.

> **Anlass: die vierte und letzte Welle der Stufe KU1 der Kühlung** (vom Anwender am 23.09.2026
> beauftragt). Zwei Änderungen, eine davon an den Daten:
>
> 1. **Entscheid E32 — Gebäude ohne wirksame Kühlung laufen frei**
>    ([Konzept Gebäudesimulation](../Dokumentation/aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
>    N1.37). Ein Gebäude, dessen Kühlung nicht wirksam ist (Projektschalter „Kühlung rechnen" aus,
>    `Kuehlung_Aktiv` = 0 oder kein Kühlsollwert), wird nicht mehr an `Maximaleraumtemperatur`
>    gekappt: Die Raumtemperatur darf darüber steigen, es wird keine Wärme abgeführt, und die
>    Überhitzungsstunden zählen die Stunden darüber im freien Lauf. Eine Kühlreihe gibt es für ein
>    solches Gebäude nicht mehr — der Ergebnisexport schreibt `kuehlbedarf_<n>.csv`,
>    `Geb[n].KuehlenergieMwh` und `Geb[n].StundenMitKuehlbedarf` nur noch bei wirksamer Kühlung.
> 2. **Testdatenbank: das Referenzprojekt mit Kühlung** (Kühlkonzept 10.4, Einfrierregel „gesäte
>    Kältedaten" oben): Projekt **1017** rechnet Kälte — genau vier Zellen über
>    [`Skripte/kuehlung_1017_referenzprojekt.py`](Skripte/kuehlung_1017_referenzprojekt.py):
>    `Tab_Einstellungen.Kuehlbetrieb` 0 → 1, Gebäude 10599 `Kuehlung_Aktiv` 0 → 1,
>    `Kuehl_Sollwert` NULL → 24,0 °C, `Kuehlleistung_Max` NULL → 15,0 kW. Sicherung vorher außerhalb
>    des Repositoriums; der Zellvergleich aller 130 Tabellen (10 496 533 Zellen) zeigt genau diese
>    vier Zellen, `integrity_check` ok, `foreign_key_check` leer, Größe unverändert 67 751 936 Byte,
>    ein zweiter Lauf des Skripts ändert nichts.
>
> **Warum 1017.** Ein Einzelgebäude-Projekt auf dem VDI-Weg (Gebäude „GMH-D-S-118", 744,4 m²,
> Skalierung fast 1) mit genau einer Wärmepumpe — sie bekommt mit KU2 den Kühlbetrieb — und mit PV
> und Stromspeicher, der üblichen Umgebung einer reversiblen Wärmepumpe; und 1017 gehört zu den
> fünf Projekten der CI, die Kühlung ist damit bei jedem Push im Netz. Nicht 1040 (Tagesbilanz-Weg,
> A15), nicht 1045 (die Kühltests schalten die Kühlung an dessen Gebäude auf Arbeitskopien ein und
> aus), nicht 1046 (Flottenstand eingefroren), nicht 1007 (Gebäude wie 1046, Skalierung 4,6).
> **Die Werte:** Der Kühlsollwert ist die Maximaleraumtemperatur des Gebäudes — die Anlage hält die
> Grenze, gegen die die Überhitzungsstunden zählen, und er liegt mehr als 1 K über dem höchsten
> Heizsollwert; die Grenze 15 kW liegt unter der Spitze ohne Grenze (rund 21 kW), sodass die Basis
> auch den Betriebsfall „Kühlgrenze" trägt.
>
> **Elf Projekte bewegen sich, zwei bleiben byte-gleich:** **1030** (kein Gebäude) und **1040**
> (Tagesbilanz-Weg), alle Dateien byte-gleich gegen R12. Die Wärmelast `Waermelast_Max` bleibt in
> allen Projekten gleich.
>
> | Projekt | Gebäudewärme R12 → R13 [MWh/a] | relativ | `Waermebedarf_Gesamt` R12 → R13 [MWh/a] | Überhitzungsstunden je Gebäude R12 → R13 [h] | Dateien R12 → R13 |
> |---|---:|---:|---:|---|---:|
> | 1007, 1046 | 69,07 → 68,97 | −0,145 % | 73,13 → 73,03 | 469 → 600 | 36 → 35, 40 → 39 |
> | 1008 | 104,20 → 104,14 | −0,058 % | 104,20 → 104,14 | 295 → 440; 469 → 600 | 31 → 29 |
> | **1017 (gekühlt)** | 90,19 → 90,19 | −0,0001 % | 90,19 → 90,19 | 305 → 306 | 24 → 25 |
> | 1018 | 68,27 → 68,25 | −0,032 % | 68,27 → 68,25 | 154 → 233 | 25 → 24 |
> | 1023, 1024 | 450,56 → 449,90 | −0,145 % | 510,56 → 509,90 | 628 → 840 | 28 → 27, 29 → 28 |
> | 1039 | 597,72 → 597,02 | −0,117 % | 618,72 → 618,02 | 72 → 102; 264 → 308; 628 → 840 | 34 → 31 |
> | 1041 | 75,98 → 75,94 | −0,050 % | 176,41 → 176,37 | 277 → 331 | 30 → 29 |
> | 1042 | 75,98 → 75,94 | −0,050 % | 105,98 → 105,94 | 277 → 331 | 37 → 36 |
> | 1045 | 75,98 → 75,94 | −0,050 % | 80,98 → 80,94 | 277 → 331 | 33 → 32 |
> | 1040 (Tagesbilanz-Weg) | 59,35 → 59,35 | ±0 | unverändert | — | 30, byte-gleich |
> | 1030 (ohne Gebäude) | — | — | unverändert | — | 22, byte-gleich |
>
> (Gebäudewärme = `Vektor.waermebedarf_gebaeude.Summe`, in MWh umgerechnet.)
>
> **Auffälligkeiten, erklärt:**
>
> - **Die Heizwärme sinkt um höchstens 0,15 %** (E32): Die Wärme, die die Kappung im Sommer
>   abführte, bleibt in den Speichermassen und senkt den Heizbedarf der Übergangszeit ein wenig. Mit
>   ihr sinkt die ungedeckte Restwärme leicht (1023 229,95 → 229,88, 1024 166,36 → 166,32, 1039
>   131,75 → 131,74 MWh/a), und die Erzeugerreihen der betroffenen Projekte verschieben sich im
>   selben Maß.
> - **Die Überhitzungsstunden steigen um 17 bis 51 %:** Im freien Lauf liegt die operative
>   Temperatur in mehr Stunden über der Maximaleraumtemperatur als unter der Kappung, die nur die
>   Raumluft hielt; die Raumluft erreicht bis 33,4 °C (die Referenzgebäude führen keine
>   Sommerlüftung).
> - **1017 bleibt fast bei R12:** gekühlt auf 24 °C ist die frühere Kappung — bis auf die 25
>   Stunden an der Grenze von 15 kW, in denen die Raumluft bis 25,0 °C steigt (Heizwärme
>   −0,0001 %, Überhitzung 305 → 306 h, 525 Werte außerhalb der Toleranz gegen R12). **Neu ist die
>   Kälteseite:** Kältebedarf 2,52 MWh/a in 402 Stunden, Kältelast 14,99 kW (die Grenze des
>   Katalogbaus mal der Skalierung 744/744,4), ungedeckt (`Kaelterestbedarf` 2,52 MWh/a, die Warnung
>   „Kältebedarf ohne Kälteerzeuger" ist gewollt — KU1 hat keinen Kälteerzeuger). Dazu die Kanaldatei
>   `waermebedarf_kuehlung.csv` und sechs der neun Kältespalten (`Waermebedarf_Kuehlung`,
>   `Kaeltebedarf_Gesamt`, `Kaeltelast_Max`, `Kaelterestbedarf`, `Deckung_Kuehlung` von BHKW und
>   Heizkessel mit 0); Wärmepumpe, Solarthermie und Pufferspeicher schreiben in 1017 keine
>   Ergebniszeile bzw. bleiben nach K7 leer.
> - **Dateien und Schlüssel:** 13 Gebäude ohne wirksame Kühlung verlieren `kuehlbedarf_<n>.csv` und
>   je drei Skalare (`Geb[n].KuehlenergieMwh`, `Geb[n].StundenMitKuehlbedarf`,
>   `Vektor.kuehlbedarf_<n>.Summe`); 1017 behält seine Kühlreihe und bekommt eine Datei und sieben
>   Skalare dazu — 399 → 387 CSV, 2 239 → 2 207 Skalare.
>
> **Kein Fehlschlag, kein NaN, keine Ablehnung:** 13/13 Projekte gerechnet; keine Datei führt `NaN`
> oder `Inf`.
>
> **Einfrierregeln:** Mit dieser Einfrierung entsteht die Regel „gesäte Kältedaten" (vier Zellen in
> 1017). E32 ist eine Änderung des Rechenwegs, keine Datenänderung; Emissionsfaktoren,
> PV-Modulkoeffizienten, Flottenstand 1046 und gesäte Gebäudedaten sind nicht berührt.
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **13/13 byte-gleich** (387/387 CSV),
> Laufzeit 4 s für alle dreizehn Projekte; ebenso byte-gleich ein Lauf auf der Testdatenbank vor dem
> Zusammenführen mit dem Zapfprofil-Testkatalog Z2 — der Testkatalog bewegt kein Referenzprojekt.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046 \
>   --ziel Referenzlaeufe/2026-09-23_R13_Kuehlung
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis; die Abnahme der Stufe im
> [Protokoll der Schlusswelle KU1](../Dokumentation/ueberholt/Protokolle/Gebaeudesimulation/2026-09-23_Schlusswelle_KU1.md)
> und im [Status der Gebäudesimulation](../Dokumentation/aktuell/Status_Gebaeudesimulation_VDI6007.md).

> **Nachtrag: Schemastand 114 (Kühlung, Stufe KU2, Welle 1), die Basis bleibt.** Migrationsschritt
> **114** (`SCHRITT_114_KUEHLUNG_ERZEUGER`, KU-S3: `Kuehlbetrieb` (0/1, Vorgabe 0), `Kuehl_Vorlauf` und
> `Kuehl_Hilfsstromanteil` an `Tab_WP` und `Tab_WP_STAMM`, dazu `Tab_Energieanlagen.Kuehl_ID_Carrier` mit
> Beziehung auf `energy_carrier.id` und `ON DELETE SET NULL`; Quelle `KuehlungSchema`), **reines DDL**
> (Kühlkonzept 7.3, Entscheide E15 und E33). Nachgezogen mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite` auf der
> Fassung 113; ein zweiter Lauf legt nichts an. Zellvergleich aller 130 Tabellen gegen die Fassung 113
> (10 496 532 Zellen): `SchemaVersion` 113 → 114 und die sieben neuen Spalten — `Kuehlbetrieb` 0, die
> übrigen NULL —, sonst nichts; die 14 Sichten unverändert. `integrity_check` ok, `foreign_key_check`
> leer, 130 von 130 Tabellen STRICT, Größe unverändert 67 751 936 Byte. **Keine Einfrierregel ist
> berührt:** Die neuen Spalten tragen keinen gesäten Wert, und kein Rechenweg liest sie. Referenzlauf
> aller dreizehn Projekte **13/13 PASS** gegen diese Basis (4 145 687 Werte, 387/387 CSV
> byte-gleich, außer `protokoll.txt`) (LFS-SHA-256 `8a3bebaf…`).

> **Nachtrag: Schemastände 116 bis 118 (Szenarioabdeckung der Wirtschaftlichkeit, Etappe E9a,
> #461), die Basis bleibt.** Migrationsschritte **116** (`SCHRITT_116_SZENARIO_RAHMEN`: vier nullbare
> Spalten `Szen_Best_/Szen_Worst_Zeitraum` und `…_Menge` an `Tab_ProjektWirtschaftlichkeit`), **117**
> (`SCHRITT_117_TRAEGERPREIS_SZENARIO`: sechs nullbare Spalten `custom_price_work/base/power_best/_worst`
> an `energy_project_settings`) und **118** (`SCHRITT_118_ERLOESSATZ_SZENARIO`: acht nullbare Spalten —
> `Einspeiseverguetung(_KWK)_Best/_Worst` an `Tab_ProjektWirtschaftlichkeit`, `DvEntgelt_` und
> `PpaPreis_Best/_Worst` an `Tab_ProjektPhotovoltaik`), **reines DDL**; NULL heißt „wie Erwartet". Mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite` auf der
> Fassung 115 (LFS-SHA-256 `fbc30835…`, 67 780 608 Byte) nachgezogen: 18 Spalten angelegt, Marker 118;
> ein zweiter Lauf legt nichts an. Gegen 115 unterscheiden sich allein die drei Tabellen um die 18
> neuen, leeren Spalten und `SchemaVersion` 115 → 118; `integrity_check` ok, `foreign_key_check` leer,
> Größe 67 784 704 Byte (LFS-SHA-256 `e9748b7f…`). **Keine Einfrierregel ist berührt:** Keine Spalte
> trägt einen gesäten Wert, und der Referenzlauf führt keine Wirtschaftlichkeitsgröße — er bleibt
> byte-gleich.

> **Nachtrag: Schemastand 119 (Kühlung, Stufe KU2, Welle 3), die Basis bleibt.** Migrationsschritt
> **119** (`SCHRITT_119_KAELTESTROM`: `Tab_Energieanlagen.Kuehl_EigenerZaehler` — 0/1 mit `CHECK`, nullbar,
> ohne Vorgabe, NULL = anteilig am Netzbezug, Entscheid E34 — und sieben nullbare Ergebnisspalten der
> Kälteseite an `Tab_ErgebnisWaermepumpe` (`Kaelteproduktion_WP`, `Stromverbrauch_Kuehlung`) und
> `Tab_ErgebnisWaermepumpeModul` (`Kaelteproduktion`, `Stromverbrauch_Kuehlung`, `Kaeltestrom_Netzbezug`,
> `Kuehl_carrier_id`, `Kuehl_EigenerZaehler`); Quelle `KuehlungSchema`), **reines DDL** (Kühlkonzept 6.1–6.4,
> 8.4). Er folgt auf die Schritte 115 (Zapfprofil, Nachtrag oben) und 116 bis 118 (Szenarioabdeckung der
> Wirtschaftlichkeit, E9a) und ist auf deren Fassung **118** nachgezogen, mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`; ein zweiter Lauf
> findet nichts offen. Zellvergleich aller 132 Tabellen (samt `sqlite_sequence`) gegen die Fassung 118
> (10 498 685 Zellen): `SchemaVersion` 118 → 119 und die acht neuen Spalten, alle NULL, sonst nichts; die 14
> Sichten und alle 208 Indizes unverändert. `integrity_check` ok, `foreign_key_check` leer, 131 von 131
> Fachtabellen STRICT, Größe unverändert 67 784 704 Byte. **Keine Einfrierregel ist berührt:** Die neuen
> Spalten tragen keinen gesäten Wert; die Ergebnisspalten schreibt nur ein Lauf mit Kältekaskade, und kein
> Referenzprojekt kühlt. Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese Basis
> (4 145 687 Werte, 387/387 CSV byte-gleich, außer `protokoll.txt`) (LFS-SHA-256 `6259b348…`).

> **Nachtrag E10 (#463): Schemastand 120 und der Anschluss der Speicherflotte an die
> Nutzungsdauertabelle, die Basis bleibt.** Migrationsschritt **120**
> (`SCHRITT_120_NUTZUNGSDAUER_SAETZE`, Nutzungsdauer-Konzept Stufe S3), **reines DML** an
> `Tab_Nutzungsdauer`: Die leeren Satzzellen der Standardzeilen bekommen die Mitte des
> Empfehlungsbereichs derselben Position der Betriebsvorlagen-Saat (Quelle `NutzungsdauerSaetze`) —
> fünf Zellen `Instandsetzung_Prozent`: Heizkessel · Wärmeerzeuger 2,0, BHKW · Modul 6,0,
> Wärmezentrale · Rohrleitungen 2,0, Stromeinspeisung · Netzanschluss 2,0, Bauliche Anlagen 1,25;
> `Wartung_Prozent` bleibt überall leer. Mit `Werkzeuge/Testdatenbankschema` auf der Fassung 119
> nachgezogen; ein zweiter Lauf setzt nichts (0/5). Zellvergleich gegen die Fassung 119: allein
> `SchemaVersion` 119 → 120 und diese fünf Zellen; `integrity_check` ok, `foreign_key_check` leer,
> Größe unverändert 67 784 704 Byte (LFS-SHA-256 `52c4729d…`). **Ergebnisneutral:** Ein Satz der
> Tabelle rechnet erst, wenn der Anwender ihn über „Sätze vorbelegen…" oder die Übernahme einer
> Kostenvorlage in eine Position schreibt; der Rechenweg liest weiter den Satz der Position.
>
> **Mit derselben Welle** rechnet die Wirtschaftlichkeit der Speicherflotten-STUDIE den Restwert je
> Einheit linear aus ihrer Nutzungsdauer auf der Ersatzkette der Flotte (Betrag der letzten
> Beschaffung × Restdauer ÷ Nutzungsdauer); eine Einheit ohne eigenes Ersatzintervall nimmt die
> Nutzungsdauer der Standardzeile „Stromspeicher · Batterie" (10 a), und der feste Restwert je
> Einheit ist ein Altfeld, das nicht mehr rechnet. Der Projektlauf rechnet keine
> Flottenwirtschaftlichkeit, und `aggregate.csv` führt für 1046 nur die Physik der Flotte — die
> Referenz bewegt sich nicht (dritte Einfrierregel, Absatz „Anschluss an die Nutzungsdauertabelle").
> Referenzlauf aller dreizehn Projekte gegen diese Basis: **13/13 PASS** (4 145 687 Werte, 387/387 CSV
> byte-gleich, außer `protokoll.txt`) — deshalb keine neue Basis R14.

> **Die Vorgängerbasis `2026-09-23_R12_Gebaeudemodell`**, die erste Basis auf dem VDI-Weg, ist mit
> dieser Einfrierung aus dem Arbeitsbaum gefallen; ihr Protokoll samt der Begründung zu G1 + G2 und
> den Nachträgen zu den Schemaständen 104 bis 113 und zum Zapfprofil-Testkatalog steht in
> [`Dokumentation/ueberholt/Referenzbasen/`](../Dokumentation/ueberholt/Referenzbasen/LIESMICH.md).
> Gerechnet wird ausschließlich gegen die aktuelle Basis.

## Was hier liegt

| Pfad | Inhalt |
|---|---|
| `<yyyy-MM-dd>_<Marke>/` | Ein eingefrorener Lauf: je Projekt ein Unterordner `Projekt_<ID>/`, dazu `lauf_protokoll.md` bzw. `protokoll.txt` |
| `<...>/Projekt_<ID>/aggregate.csv` | Alle Skalare des Laufs: `Tab_Ergebnis*`-Zeilen, Restgrößen aus `SimulationControl`, Jahressumme jedes Vektors |
| `<...>/Projekt_<ID>/*.csv` | Die Ganglinien: 8760 Stundenwerte bzw. 35040 Viertelstundenwerte, `Index;Wert` |
| `Arbeitskopie/` | Die Kopie der Datenbank, auf der gerechnet wird. Wird bei jedem `lauf` neu angelegt. Nicht im Git (`Kenndaten.accdb` ist in `.gitignore`) |
| `Katalogpaket_frei/` | Der freie Paketteil des Zapfprofilgenerators (CSV im Paketformat N2): Quelle der freien Zeilen der Auslieferungsvorlage und der Testdatenbank |
| `Kenndaten_Test.sqlite` | Die reduzierte Testdatenbank, gegen die der plattformfreie `EPOS.Referenzlauf` und der SQL-Dialektprüfer laufen. **Versioniert** — eine Änderung daran gehört in einen eigenen Commit |
| `Skripte/` | Was an dieser Testdatenbank gemacht wurde, als Skript und nicht als Erzählung: `pruefprojekt_1045_ost_west.py` (W6‑O‑7), `pruefprojekt_1046_speicherflotte.py` (SP‑O‑8), `gebaeude_10576_bauweise.py` (Stufe GB, Befund D), `gebaeude_10612_233_bauweise.py` (dieselbe Korrektur an 1009 und Katalogsatz 233, Basis unverändert), `tww_testkatalog_fiktiv.py` (Testkatalog des Zapfprofilgenerators samt abgeleiteten VDI-Werten und den Zeilen des freien Paketteils, Schemastand 115) und `normzahlen_abgeleitet_bauen.py` (nur lokal: abgeleitete VDI-6002-Werte nach `tww_katalogwerte_abgeleitet.json`, ZU19) |

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
   `2026-09-23_R13_Kuehlung`, ist plattformfrei gegen `Kenndaten_Test.sqlite` gerechnet:
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
