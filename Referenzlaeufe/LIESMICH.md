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
> `Abfrage_Projektgebaeude`: sechzehn Gebäudezeilen in dreizehn der vierzehn Referenzprojekte
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
ohne wirksame Kühlung läuft ein Gebäude frei (E32). Gedeckt wird er von den Wärmepumpen im
Kühlbetrieb, über ihre Kühlkennlinie. Eine einzige geänderte Zelle schaltet damit die Kälteseite
eines Projekts ein oder aus, verschiebt seine Raumtemperatur, seine Heizwärme in der Übergangszeit
und seine Überhitzungsstunden, seine Kältedeckung, seinen Kältestrom und mit ihm Netzbezug, Kosten
und CO₂ — und lässt Dateien und Schlüssel im Export entstehen oder verschwinden.

> **Wer gesäte Kältedaten der Testdatenbank ändert, friert im selben Schritt die Basis neu ein und
> begründet den Wechsel hier.**
>
> Betroffen ist jede Änderung am Projektschalter `Tab_Einstellungen.Kuehlbetrieb` eines
> Referenzprojekts, an den Kühleingaben seiner Gebäude (`Kuehlung_Aktiv`, `Kuehl_Sollwert`,
> `Kuehl_Sollwert_Nacht`, `Kuehlleistung_Max` in `Tab_Gebaeude`; in `Tab_Gebaeude_STAMM`, sobald
> ein Katalogsatz in ein Referenzprojekt übernommen wird) und an der Kanalzuordnung „Kühlung“ eines
> Lastgangs eines Referenzprojekts (`Z_ProjektWaermebedarf.Kanal`) — und an seiner
> **Kälteerzeugung**: am Kaskadenplatz einer Wärmepumpe (`Tab_Einstellungen.Tool_1` bis `Tool_4`),
> an ihrem Kühlbetrieb, Kühl-Vorlauf und Hilfsstromanteil (`Kuehlbetrieb`, `Kuehl_Vorlauf`,
> `Kuehl_Hilfsstromanteil` in `Tab_WP`), an Kühlträger und Abrechnungsart ihrer Anlagenzeile
> (`Kuehl_ID_Carrier`, `Kuehl_EigenerZaehler` in `Tab_Energieanlagen`) und an der Kühlkennlinie
> des Projektgeräts samt Vorlauf-Stützstellen, Temperaturen, EER, Kälteleistung und Laststufe
> (`Tab_Kenndaten_Kuehlung`); in `Tab_WP_STAMM` und `Tab_Kenndaten_Kuehlung_STAMM`, sobald ein
> Katalogsatz in ein Referenzprojekt übernommen wird.
>
> **Rechenwirkung haben Projekt 1017 und seine Kopie 1047:** Projektschalter ein, Gebäude 10599 mit Haken,
> Kühlsollwert 24 °C und Kühlleistungsgrenze 15 kW — vier Zellen aus
> [`Skripte/kuehlung_1017_referenzprojekt.py`](Skripte/kuehlung_1017_referenzprojekt.py); dazu
> der Kälteerzeuger — die Wärmepumpe (Anlage 10211, Projektgerät 1017033) auf Kaskadenplatz 3, im
> Kühlbetrieb mit Kühl-Vorlauf 18 °C und Hilfsstromanteil 5 %, ohne Kühlträger und
> Abrechnungsart, mit einer gesäten Kühlkennlinie aus zehn Zeilen (Vorlauf 7 und 18 °C, 20 bis 40 °C,
> Laststufe 100) — vier Zellen und zehn Zeilen aus
> [`Skripte/kaelteerzeuger_1017_referenzprojekt.py`](Skripte/kaelteerzeuger_1017_referenzprojekt.py).
> Dieselben Kältedaten trägt Projekt 1047, die Kopie von 1017 aus
> [`Skripte/anlagenkopplung_1047_referenzprojekt.py`](Skripte/anlagenkopplung_1047_referenzprojekt.py)
> (Gebäude 10653, Wärmepumpe als Anlage 14946 mit Projektgerät 1672046 und den zehn Zeilen der
> Kühlkennlinie als eigene Kopie); sie gelten dort ebenso als gesät — mit einem Unterschied: In 1047
> steht die Wärmepumpe auf **Kaskadenplatz 2**, vor dem Elektrokessel (Regel „gesäte Auslegungsdaten der
> Übergabe" unten).
> Die übrigen zwölf Referenzprojekte stehen auf 0; ihre Gebäude laufen frei, keine ihrer
> Wärmepumpen kühlt.
>
> **Nicht** betroffen sind Kühleingaben und Kälteerzeuger von Projekten außerhalb der Referenzliste.

## Die Einfrierregel „gesäte Auslegungsdaten der Übergabe“ (Anlagenkopplung 11.4)

Dieselbe Klasse von Falle, sechster Ort, und wie Gebäude- und Kältedaten nach ihrem Gegenstand benannt,
nicht durchgezählt (Anlagenkopplung 11.4). Ein gekoppeltes Gebäude rechnet seine Heizwärme nicht mehr
ideal, sondern über die Wärmeübergabe (Schritt H) und seine Kälte über die Kühlübergabe (Schritt K) —
aus der Kopplungsstufe des Projekts und den Übergabespalten des Gebäudes, und nur, wenn beide stehen.
Eine einzige geänderte Zelle schaltet die Kopplung eines Referenzprojekts ein oder aus, verschiebt
Auslegungspunkt, Heizkurve oder Regelband, mit ihnen Heizwärme, Aufheizspitze, Raumtemperatur,
Kältebedarf und Überhitzungsstunden, die Deckung der Erzeuger, Netzbezug, Kosten und CO₂ — und lässt die
Reihen `vorlauf_<n>.csv`, `ruecklauf_<n>.csv`, `uebergabe_<n>.csv` und ihre Kühl-Gegenstücke im Export
entstehen oder verschwinden.

> **Wer gesäte Auslegungsdaten der Übergabe in der Testdatenbank ändert, friert im selben Schritt die
> Basis neu ein und begründet den Wechsel hier.**
>
> Betroffen ist jede Änderung an der Kopplungsstufe `Tab_Einstellungen.Anlagenkopplung` eines
> Referenzprojekts und an den Übergabespalten seiner Gebäude in `Tab_Gebaeude` (in
> `Tab_Gebaeude_STAMM`, sobald ein Katalogsatz in ein Referenzprojekt übernommen wird): der Heizkreis
> aus `AK-S1` — `Heizkreis_Aktiv`, `Uebergabe_Art`, `Uebergabe_Exponent`, `Uebergabe_Leistung_Nenn`,
> der Auslegungspunkt (`Auslegung_Vorlauf`, `Auslegung_Ruecklauf`, `Auslegung_Raumtemperatur`,
> `Auslegung_Aussentemperatur`), die Heizkurve (`Heizkurve_Aktiv`, `Heizkurve_Niveau`,
> `Heizkurve_Steilheit`), `Regler_Proportionalband` und `Sollwertprofil` — und die Kühlübergabe aus
> `KAK-S1` — `Kuehluebergabe_Aktiv`, `Kuehl_Uebergabe_Art`, `Kuehl_Uebergabe_Exponent`,
> `Kuehl_Uebergabe_Leistung_Nenn`, `Kuehl_Auslegung_Vorlauf`, `Kuehl_Auslegung_Ruecklauf`,
> `Kuehl_Auslegung_Raumtemperatur` und `Kuehl_Vorlaufgrenze`; sobald eine Zone rechnet, ebenso deren
> Übergabespalten in `Tab_Zone`. Betroffen ist außerdem die **Kaskade** eines gekoppelten
> Referenzprojekts (`Tab_Einstellungen.Tool_1` bis `Tool_4`): Sie entscheidet, ob die Wärmepumpe Wärme
> liefert und damit, ob die Kennlinienwahl am gerechneten Vorlauf (Anlagenkopplung 6.1) auf ein Ergebnis
> wirkt; für den Platz der Wärmepumpe gilt zugleich die Regel „gesäte Kältedaten". Ebenso betroffen ist
> das Anlegen oder Entfernen eines gekoppelten Referenzprojekts.
>
> **Rechenwirkung hat allein Projekt 1047 „Referenz Anlagenkopplung AK1"**, die Kopie von 1017:
> Kopplungsstufe „AK1", Gebäude 10653 mit Heizkreis (Radiator, Heizkurve gefahren) und Kühlübergabe
> (Kühldecke), alle übrigen Übergabespalten leer, also die EPOS-Vorgaben der Art, und die Kaskade BHKW,
> Wärmepumpe, Elektrokessel (in 1017: BHKW, Elektrokessel, Wärmepumpe) — neun Zellen samt der Kopie aus
> [`Skripte/anlagenkopplung_1047_referenzprojekt.py`](Skripte/anlagenkopplung_1047_referenzprojekt.py).
> Eine leere Spalte ist hier eine Setzung wie eine gefüllte: Wer eine Vorgabe von Hand einträgt, ändert
> die Basis. Die übrigen dreizehn Referenzprojekte stehen ohne Kopplungsstufe und ohne Haken; ihre
> Gebäude rechnen ideal.
>
> **Nicht** betroffen sind die Übergabespalten von Projekten außerhalb der Referenzliste und die
> Vorgabewerte der Art im Kern — die sind Rechenweg und werden gegen die Basis gehalten wie jeder
> andere.

## Abgeleitete VDI-Werte im Tww-Testkatalog (Anwenderentscheide ZU19 und ZU23)

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
  Wert gleicht seinem Original — außer einer Null, die multiplikativ nicht abzuleiten ist und
  unverändert bleibt —, jeder liegt höchstens 5,9 % davon entfernt (sonst das nächste δ, dann eine
  Stelle feiner). Deterministisch; ein zweiter Lauf schreibt dieselben Bytes.
- **Das Skript läuft nur lokal** — es liest die gitignorierten Originale unter
  `Normzahlen/vdi6002/` und schreibt die committete Datei
  [`Skripte/tww_katalogwerte_abgeleitet.json`](Skripte/tww_katalogwerte_abgeleitet.json) (497 Werte,
  kein Originalwert). Das Einspielskript
  [`Skripte/tww_testkatalog_fiktiv.py`](Skripte/tww_testkatalog_fiktiv.py) liest nur diese Datei
  und läuft ohne die Originale; die Bedarfswerte rechnet es von Litern bei 60 °C mit
  c_w = 1,163 Wh/(l·K) auf kWh bei den Bezugstemperaturen 60/12 °C der Zeile um.
- **Die Wache** `EPOS.Kern.Tests/TwwKatalogWacheTests.Kein_abgeleiteter_Katalogwert_gleicht_dem_VDI_Original`
  prüft lokal — nur wenn `Normzahlen/vdi6002/` beiliegt, sonst schweigt sie —, dass kein Wert der
  Testdatenbank und der JSON-Datei seinem Original gleicht — eine Null der Quelle bleibt Null und
  wird nur darauf geprüft — und jeder innerhalb ±6 % liegt; ihre
  Meldung nennt Abweichungen, nie einen Absolutwert.
- **VDI 4655 läuft unter derselben Regel** (Anwenderentscheid ZU23, 24.09.2026). `--norm vdi4655`
  liest die gitignorierten Originale unter `Normzahlen/vdi4655/` und schreibt
  [`Skripte/vdi4655_abgeleitet.json`](Skripte/vdi4655_abgeleitet.json): Typtagkategorien,
  Klimazonen, Typtage je Zone, Faktoren F_TWE,TT, Kennwerte und der Abschnitt `papierwerte` mit
  allem, was allein das Grundlagenpapier braucht. Zwei Zusätze zur Regel: **ganze Zahlen** (Typtage
  je Zone) weichen um mindestens einen und höchstens max(2; 6 %) Tag(e) ab und kommen je Zone
  wieder auf 365; die **Faktoren** sind Schwankungen um einen Jahresmittelwert und werden
  ausdrücklich **nicht** renormiert — die Prüfsumme des Originals gilt für sie nicht mehr. Codes,
  Zonennamen und Gebäudebezeichnungen der Ausgabe sind neutral (TT01…, variante_1…, nur die
  Zonennummer). Kein abgeleiteter Wert gleicht einem kennzeichnenden Originalwert, auch nicht dem
  einer anderen Zelle.
- **Auch das Grundlagenpapier trägt abgeleitete Werte** (ZU23):
  [`Grundlagen_5_VDI-4655_Auswertung.md`](../Dokumentation/aktuell/Grundlagen_5_VDI-4655_Auswertung.md)
  führt keinen Zahlenwert der Richtlinie mehr — Tabellen, Grenzwerte, Jahresbedarfe und die
  Beispielrechnung stehen abgeleitet, ein Hinweisabsatz am Anfang sagt das. Fundstellen
  (Abschnitt, Tabelle, Seite) und Geltungsangaben (Zahl der Zonen und Typtagkategorien, Personen-
  und Wohneinheitengrenzen, Zeitauflösungen, Bezugskalenderjahr) bleiben unverändert; für die
  Rechnung zählt allein das vom Anwender eingespielte Paket.
- **Nicht abgeleitet** werden die DIN-Profile: A100-Referenzprofil und DIN-4708-Profil bleiben
  gesperrt (K1/K8).
- **Ergebnisneutral:** Kein Referenzprojekt steht auf dem Zapfprofilgenerator; die Basis bleibt.

## Der freie Paketteil (`Katalogpaket_frei/`)

Die freien Katalogdaten des Zapfprofilgenerators — Zapfkategorien nach Jordan/Vajen (IEA SHC
Task 26, Modellannahme), die fünf Parameter `Zapfprofil.Stochastik.*` und das
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
Basen aufräumt, lässt `2026-09-25_R15_Anlagenkopplung`, `Kenndaten_Test.sqlite`,
`Importproben`, `Katalogpaket_frei`, `Skripte` und `LIESMICH.md` stehen.**

Außer der aktuellen liegt hier keine Basis mehr: Die 24 historischen Referenzbasen (7 731
Dateien, 1 016,7 MB) sind am 11.09.2026 aus dem Arbeitsbaum gefallen (**SYNC‑Q1**: „entfernt
lassen") und seit dem Umschreiben der Git-Geschichte (**AUF‑Q1**, 12.09.2026) auch dort nicht
mehr enthalten; **`2026-09-11_R7_Speicherflotte` ist am 16.09.2026 nach demselben Muster
gefallen, `2026-09-16_R8_Heizkessel_Kaskade` am 18.09.2026,
`2026-09-18_R9_Kesselbrennstoff` am 19.09.2026, `2026-09-19_R10_BhkwWirkungsgrad` am
22.09.2026, `2026-09-22_R11_Bestandsbefunde` und `2026-09-23_R12_Gebaeudemodell` am 23.09.2026,
`2026-09-23_R13_Kuehlung` am 24.09.2026, `2026-09-24_R14_Kaelteerzeuger` am 25.09.2026** (32 Basen,
alle acht Protokolle gesichert). Kein Test, kein Gate, keine CI liest
eine entfernte Basis. **Die Messdaten sind endgültig weg** (rund 8 000 CSV-Dateien) — eine
alte Zahl steht nur noch im Protokoll.
Erhalten sind die **Protokolle** aller 32 Basen samt der Tabelle Basis → Datum → Zweck →
Protokoll unter
[`Dokumentation/ueberholt/Referenzbasen/`](../Dokumentation/ueberholt/Referenzbasen/LIESMICH.md);
**welche Basis wann von welcher abgelöst wurde und warum**, steht ebendort — bis zum 12.09.2026
in der ausgelagerten
[Basenhistorie](../Dokumentation/ueberholt/Referenzbasen/LIESMICH_Basenhistorie_bis_2026-09-12.md),
danach im Wegweiser desselben Ordners.

## Aktuelle Basis

**`2026-09-25_R15_Anlagenkopplung/`** — **vierzehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024,
1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047), **432 CSV**, **2 447 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite` (eingefroren auf
Schemastand **141**, LFS-SHA-256 `b48add6a…`). Gegen diese Basis hält `.github/workflows/kern.yml` (1030,
1007, 1017, 1045, 1046, 1047) jeden Push, `ios.yml` den iZ6-Vergleich für 1030, und
`EPOS.Kern.Tests/GebaeudeRueckwegTests` den Tagesbilanz-Weg an Projekt 1040. Sie ist die **einzige**
Basis im Arbeitsbaum.

> **Anlass: die fünfte und letzte Welle der Stufe AK1 der Anlagenkopplung** (vom Anwender am 25.09.2026
> samt dem Einfrieren und der Aufnahme des neuen Projekts in die CI beauftragt). Eine Änderung, allein an
> den Daten — **das Referenzprojekt mit Kopplung** (Anlagenkopplung 11.4, Einfrierregel „gesäte
> Auslegungsdaten der Übergabe" oben): Projekt **1047 „Referenz Anlagenkopplung AK1"** ist eine Kopie von
> 1017 auf dem Kopierweg des Programms (`ProjektDuplizierenCtrl`, im Skript Schritt für Schritt
> nachgebildet) mit neun gesetzten Zellen, über
> [`Skripte/anlagenkopplung_1047_referenzprojekt.py`](Skripte/anlagenkopplung_1047_referenzprojekt.py):
> `Tab_Einstellungen.Anlagenkopplung` NULL → „AK1", die Kaskade `Tool_2` „Heizkessel" → „Wärmepumpe" und
> `Tool_3` „Wärmepumpe" → „Heizkessel" (die Wärmepumpe vor dem Elektrokessel, Anwenderentscheid vom
> 25.09.2026; der Tausch der Platzinhalte wie der Pfeil „nach vorn" der Simulationskonfiguration,
> `Kaskade_Gepflegt` bleibt 0 wie überall in der Testdatenbank), am Projektgebäude 10653 (der Kopie von 10599)
> `Heizkreis_Aktiv` 0 → 1, `Uebergabe_Art` NULL → „RADIATOR", `Heizkurve_Aktiv` 0 → 1,
> `Kuehluebergabe_Aktiv` 0 → 1 und `Kuehl_Uebergabe_Art` NULL → „KUEHLDECKE", dazu die Beschreibung des
> Projekts. Alle übrigen Übergabespalten bleiben NULL — es rechnen die EPOS-Vorgaben der Art: Radiator
> 55/45 °C mit n = 1,3, Heizkurve aus dem Auslegungspunkt (Niveau 0 K, Steilheit 1,0),
> Auslegungs-Außentemperatur aus dem kältesten Tagesmittel, Nennleistung aus der Auslegungsheizlast,
> Proportionalband 1,0 K, Sollwerte des Gebäudes; Kühldecke 16/19 °C mit n = 1,1, Vorlaufgrenze 16 °C,
> Nennleistung aus dem Auslegungstag (19. Juni, 20,41 kW am Katalog-Gebäude). **1017 bleibt unverändert
> und ungekoppelt.** Die Testdatenbank ist die Fassung von origin mit Schemastand 141 (`a427aa72…`, die
> Schritte 140 und 141 stehen mit ihren Nachträgen beim R14-Abschnitt unter `ueberholt/`), auf die das
> Skript angewandt ist; Sicherung vorher außerhalb des Repositoriums. Der Zellvergleich aller Tabellen
> gegen diese Fassung (10 507 032 Zellen) zeigt 9 365 neue Zeilen in 24 Tabellen und 21 geänderte Zeilen
> in `sqlite_sequence`, keine geänderte oder entfernte Zeile sonst, das Schema gleich. Die Kopie gleicht
> Zelle für Zelle einem vom Programm duplizierten Projekt (Prüfstand außerhalb des Repositoriums) bis auf
> die neun Zellen. `integrity_check` ok, `foreign_key_check` leer, 68 747 264 Byte; ein zweiter Lauf des
> Skripts ändert nichts (byte-gleich).
>
> **Warum so** — eine Kopie statt 1017 selbst, damit das Kühlreferenzprojekt bleibt, wie es ist, und das
> Paar 1017/1047 zugleich „ideal gegen gekoppelt" zeigt; 1017 als Vorlage, weil nur dort beide Seiten der
> Kopplung zu rechnen haben (Einzelgebäude nach VDI 6007, Kühlung mit Kälteerzeuger); Radiator mit
> gefahrener Heizkurve und Kühldecke mit allen Vorgaben, damit die Basis die hergeleiteten Wege der Art
> trägt und keine Zahl von Hand (Anlagenkopplung 8.4, 11.4); die Wärmepumpe vor dem Elektrokessel, damit
> sie Wärme liefert und die Kennlinienwahl am gerechneten Vorlauf (Anlagenkopplung 6.1) auf ein Ergebnis
> der Basis wirkt — auf Platz 3 deckten BHKW und Elektrokessel die gekappte Last ganz.
>
> **Die dreizehn alten Projekte bleiben byte-gleich** — Nullnachweis vor dem Einfrieren, auf dem Stand
> mit 1047 in der Testdatenbank: 13/13 PASS gegen R14 (4 207 049 Werte), 394/394 CSV byte-gleich, nur
> `protokoll.txt` anders; nach der Kaskade von 1047 und den Schritten 140 und 141 ebenso byte-gleich zur
> ersten Einfrierung dieser Basis. **1047 kommt mit 38 Dateien und 198 Skalaren dazu**: gegen 1017 die sechs Reihen
> des Heiz- und des Kältekreises (`vorlauf_0.csv`, `ruecklauf_0.csv`, `uebergabe_0.csv`,
> `kuehlvorlauf_0.csv`, `kuehlruecklauf_0.csv`, `kuehluebergabe_0.csv`) und die Skalare
> `Energiebedarf.Vorlauf_Mittel`, `Ruecklauf_Mittel`, `Uebergabe_Begrenzt_Stunden` samt ihren
> Kühl-Gegenstücken und den Vektorsummen der sechs Reihen — 394 → 432 CSV, 2 249 → 2 447 Skalare.
>
> | | 1017 (ideal) | 1047 (gekoppelt) |
> |---|---:|---:|
> | Heizwärme [MWh/a] | 90,19 | 82,75 (−8,3 %) |
> | Heizlastspitze [kW] | 63,16 | 45,64 |
> | mittlere Raumtemperatur der Heizzeit [°C] | 20,72 | 20,08 |
> | Vorlauf / Rücklauf, bedarfsgewichtet [°C] | — | 36,98 / 33,56 |
> | Stunden mit begrenzter Wärmeübergabe [h] | — | 1 108,2 (bis 2,4 K unter dem Sollwert) |
> | Kältebedarf [MWh/a] | 2,52 | 2,33 (−7,5 %) |
> | Kühlvorlauf / Kühlrücklauf, bedarfsgewichtet [°C] | — | 18,00 / 18,81 |
> | Stunden mit begrenzter Kühlübergabe [h] | — | 0 |
> | Stunden mit Kühlbedarf; Überhitzungsstunden [h] | 402; 306 | 423; 327 |
> | Kaskade der Wärmeerzeuger | BHKW, Elektrokessel, WP | BHKW, WP, Elektrokessel |
> | Wärmedeckung BHKW / Wärmepumpe / Elektrokessel [%] | 77,5 / 0,05 / 22,3 | 82,4 / 16,4 / 1,2 |
> | Wärme der Wärmepumpe [MWh/a] | 0,04 | 13,60 |
> | Strom der Wärmepumpe Heizseite / Kühlseite [MWh/a] | 0,02 / 0,55 | 3,54 / 0,51 |
> | Jahresarbeitszahl der Wärmepumpe im Heizbetrieb | 2,49 | 3,84 |
> | Wärme des Elektrokessels [MWh/a] | 20,12 | 1,00 |
> | Restwärme [MWh/a] | 0,10 | 0 |
> | Kältedeckung durch die Wärmepumpe | 98,4 % | 98,8 % |
> | Netzbezug `Stromrestbedarf` [MWh/a] | 655,88 | 641,18 |
>
> **Die Abweichung ist die Kopplung, kein Fehler** (Anlagenkopplung 3.5, 4.4, 7.1). Die Wärmeübergabe
> ist nach den Vorgaben auf die stationäre Auslegungsheizlast bemessen; nach der Absenkung reicht sie in
> 1 108 Stunden nicht, die Aufheizspitze wird gekappt, und der P-Regler hält den Raum mit seinem Band von
> 1 K im Mittel etwas unter dem Sollwert — beides senkt die Heizwärme um 8,3 % (Heizwärme, Spitze und
> begrenzte Stunden wie in der Probe der zweiten Welle mit dem Heizkreis allein). Auf der Kälteseite hebt
> das Band die Raumluft bis zu 1 K über den Kühlsollwert, bevor die Kühldecke voll liefert: Der
> Kältebedarf sinkt um 7,5 %, die Stunden mit Kühlbedarf steigen von 402 auf 423 und die
> Überhitzungsstunden von 306 auf 327 (die Probe der vierten Welle mit der Kühldecke allein: 2,36 MWh/a
> und 330 Überhitzungsstunden; mit dem Heizkreis dazu liegt der Raum in der Heizzeit tiefer). Das ist
> gewollt; die Sollwerte von 1017 bleiben.
>
> **Die Wärmepumpe auf Platz 2 wählt ihre Kennlinie am gerechneten Vorlauf.** Hinter dem BHKW übernimmt
> sie 13,60 MWh/a (16,4 %), der Elektrokessel nur noch 1,00 MWh/a; der Netzbezug sinkt gegen 1017 um
> 14,7 MWh/a. Der Lauf nennt die Stunden je Stützstelle des Heizkreises — 35 °C 3 858 h, 45 °C 1 782 h,
> 55 °C 122 h, dazu 2 309 Stunden unter 35 °C (dort gilt die unterste Kennlinie) —, und die
> Jahresarbeitszahl im Heizbetrieb ist 13,60 / 3,54 = **3,84**. **Gegenprobe ohne Kopplung** (nur an einer
> Arbeitskopie außerhalb des Repositoriums, 1047 mit `Anlagenkopplung` NULL, sonst gleich): Die Wärmepumpe
> rechnet dann durchgehend an der Kennlinie des Anlagenvorlaufs 55 °C und kommt auf eine Jahresarbeitszahl
> von 19,04 / 6,25 = **3,05** — bei höherem Heizbedarf (90,19 MWh/a, ideal) und mehr Betriebsstunden
> (575 statt 396 h). Die Kennlinienwahl am gerechneten Vorlauf wirkt also, und die Basis hält sie. Die
> Kälteseite bleibt am festen Kühl-Vorlauf 18 °C der Maschine (E37, A3): EER-Jahreswert 4,52 wie in 1017,
> Kältestrom 0,51 MWh/a, alles aus dem Netz; die Kaskade ändert daran nichts.
>
> **Rechenzeit (E36):** Das beidseitig gekoppelte Gebäude von 1047 rechnet in 50 bis 62 ms je Jahr
> (Heizwärme eines Gebäudes samt Eingang, das Beste aus fünf Läufen nach dem Anlauf, zwei Messungen),
> das ungekoppelte von 1017 in 22 ms — unter der Grenze von 100 ms.
>
> **Kein Fehlschlag, keine Ablehnung:** 14/14 Projekte gerechnet. NaN steht nur, wo es gewollt ist:
> `vorlauf_0.csv` und `ruecklauf_0.csv` von 1047 tragen in den 1 063 Stunden ohne Heizbetrieb NaN (die
> Lücke der Reihe, Anlagenkopplung 8.3). Der Vergleich nimmt NaN gegen NaN als gleich; `pruefen` nennt die
> Lücken dieser vier Reihenmuster als Hinweis (`Referenzlauf/Plausibilitaet.cs`, benannte Ausnahme) und
> meldet die Basis **plausibel**.
>
> **Einfrierregeln:** Die Regel „gesäte Auslegungsdaten der Übergabe" entsteht mit diesem Projekt und umfasst
> die Kaskade des gekoppelten Projekts; für den Platz der Wärmepumpe gilt zugleich „gesäte Kältedaten". 1047 ist
> zugleich ein Referenzprojekt mit Gebäude-, Kälte- und Kälteerzeugerdaten und mit eigenen Zeilen in
> `energy_project_settings` — die Regeln „gesäte Gebäudedaten", „gesäte Kältedaten" und die der
> Emissionsfaktoren gelten für seine Zeilen wie für die von 1017. PV-Modulkoeffizienten und Flottenstand
> 1046 sind nicht berührt.
>
> **Zweimal eingefroren, am selben Tag und vor der Veröffentlichung:** zuerst mit der Wärmepumpe auf Platz 3
> (Schemastand 139), dann mit der Kaskade oben auf Schemastand 141. Zwischen beiden bewegt sich allein 1047
> (neun Dateien: `aggregate.csv`, die vier Reihen der Wärmepumpe `wp_produktion`, `wp_strom`,
> `wp_waermebedarf`, `wp_restwaerme`, drei des Kessels und `reststrom_viertelstunde.csv`); die dreizehn
> übrigen Projekte sind byte-gleich.
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **14/14 byte-gleich** (432/432 CSV) und
> **GESAMT: PASS** gegen diese Basis (4 610 207 Werte). Der Lauf der sechs CI-Projekte mit der
> Kommandozeile aus `kern.yml`: **6/6 PASS** (2 208 587 Werte), 198/198 CSV byte-gleich.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047 \
>   --ziel Referenzlaeufe/2026-09-25_R15_Anlagenkopplung
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis; die Abnahme der Stufe im
> [Protokoll der fünften Welle AK1](../Dokumentation/ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_AK1_Welle5_Referenzprojekt.md)
> und im [Status der Gebäudesimulation](../Dokumentation/aktuell/Status_Gebaeudesimulation_VDI6007.md).

> **Die Vorgängerbasis `2026-09-24_R14_Kaelteerzeuger`**, die erste Basis mit Kälteerzeuger, ist mit dieser
> Einfrierung aus dem Arbeitsbaum gefallen; ihr Protokoll samt der Begründung zum Kälteerzeuger von 1017
> und den Nachträgen zu den Schemaständen 119 bis 141 steht in
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
| `Skripte/` | Was an dieser Testdatenbank gemacht wurde, als Skript und nicht als Erzählung: `pruefprojekt_1045_ost_west.py` (W6‑O‑7), `pruefprojekt_1046_speicherflotte.py` (SP‑O‑8), `anlagenkopplung_1047_referenzprojekt.py` (Referenzprojekt der Anlagenkopplung, Kopie von 1017), `gebaeude_10576_bauweise.py` (Stufe GB, Befund D), `gebaeude_10612_233_bauweise.py` (dieselbe Korrektur an 1009 und Katalogsatz 233, Basis unverändert), `tww_testkatalog_fiktiv.py` (Testkatalog des Zapfprofilgenerators samt abgeleiteten VDI-Werten und den Zeilen des freien Paketteils, Schemastand 115) und `normzahlen_abgeleitet_bauen.py` (nur lokal: abgeleitete VDI-6002-Werte nach `tww_katalogwerte_abgeleitet.json` und abgeleitete VDI-4655-Werte nach `vdi4655_abgeleitet.json`, ZU19; `--norm vdi6002|vdi4655|beide`) |

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
   `2026-09-25_R15_Anlagenkopplung`, ist plattformfrei gegen `Kenndaten_Test.sqlite` gerechnet:
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
