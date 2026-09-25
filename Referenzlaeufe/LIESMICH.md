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

## Abgeleitete VDI-Werte im Tww-Testkatalog (Anwenderentscheide ZU19, ZU20 und ZU23)

Der Tww-Katalog der Testdatenbank ist fiktiv (Umsetzungskonzept Zapfprofilgenerator, Kapitel 6 (b))
— mit einer Ausnahme, die der Anwender am 23.09.2026 entschieden hat: **geringfügig abweichende
VDI-Werte dürfen ins Repositorium.** Fünf Nutzungsarten „… (abgeleitet)“ (Wohnen groß, Ein- und
Zweifamilienhaus, Studentenwohnheim, Seniorenheim, Krankenhaus) tragen Bedarfswerte,
Monatsfaktoren, Wochenanteile und Tagesgänge, die aus VDI 6002 Blatt 1 und 2 abgeleitet sind, auf
vier Tagesgangsätzen (das Ein- und Zweifamilienhaus teilt den des großen Wohngebäudes);
Herkunftsart `VERFAHREN` (aus einem Verfahren gerechnet — weder Normwert noch Eigenkonstruktion
noch freie Quelle), Quelle „abgeleitet aus VDI 6002 Blatt n“, Ausgabe `2014-03`.
**Sie gehören zur Auslieferung** (Anwenderentscheid ZU20 vom 25.09.2026): Ihre Träger sind drei
CSV-Dateien des freien Paketteils, und `TwwKataloge` spielt sie in jede Vorlage ein (Abschnitt
„Der freie Paketteil“). In der Testdatenbank stehen sie nach deren Regel mit Status `EIGEN`,
`ReadOnly` 0 und Katalogversion `TEST-1`; ihre Zapfkategorien sind — wie die jeder Nutzungsart des
Testkatalogs — der Vorgabesatz ihrer Gruppe aus dem Paketteil.

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
  c_w = 1,163 Wh/(l·K) auf kWh bei den Bezugstemperaturen 60/12 °C der Zeile um. Aus derselben
  Datei **erzeugt** es die drei Träger des Paketteils
  (`Katalogpaket_frei/Tab_TwwTagesgangsatz_STAMM.csv`, `…Tagesgang…`, `…Nutzungsart…`; Schalter
  `--paketteil-schreiben`) und hält sie bei jedem Lauf dagegen — eine Quelle, drei Ablagen.
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

Die Katalogdaten des Zapfprofilgenerators, die im Repositorium stehen dürfen — Zapfkategorien nach
Jordan/Vajen (IEA SHC Task 26, Modellannahme) in zwei Vorgabesätzen, dreizehn Parameter
(`Zapfprofil.Stochastik.*`, `…Zirkulation.*`, `…Anzeige…`, `…Validierung.*`), das
Ecodesign-Zapfprofil L (Verordnung (EU) Nr. 814/2013 Anhang III) samt 24 Ereignissen und die fünf
aus VDI 6002 **abgeleiteten** Nutzungsarten samt vier Tagesgangsätzen und sechzehn Tagesgängen
(ZU20) — stehen einmal im Repositorium, als sieben CSV-Dateien im Paketformat N2 unter
[`Katalogpaket_frei/`](Katalogpaket_frei/LIESMICH.md) (Aufbau, Regeln und Quellen dort).
`Werkzeuge/Auslieferungsvorlage` spielt den Ordner in jede Vorlage ein (Status `AUSLIEFERUNG`,
`ReadOnly` 1, Herkunftsart `FREI` oder `VERFAHREN`);
[`Skripte/tww_testkatalog_fiktiv.py`](Skripte/tww_testkatalog_fiktiv.py) schreibt dieselben Zeilen
in die Testdatenbank — nach deren Regel mit Status `EIGEN`, `ReadOnly` 0 und Katalogversion
`TEST-1`; die Zapfkategorien als Vorgabesatz ihrer Gruppe an jeder Nutzungsart. Die Wache
`TwwKatalogWacheTests.Die_freien_Zeilen_der_Testdatenbank_gleichen_dem_Paketteil` hält beide
gleich, Wert für Wert und in der Anzahl. Wer eine Datei des Paketteils ändert, lässt das Skript
im selben Schritt auf die Testdatenbank laufen; die drei Träger der abgeleiteten Werte ändert **nur**
das Skript (`--paketteil-schreiben`), nie die Hand.

Die Zählungen der Tww-Katalogtabellen der Testdatenbank (Schemastand 142): 5 Tagesgangsätze
(1 fiktiver, 4 abgeleitete), 20 Tagesgänge, 8 Nutzungsarten (3 fiktive, 5 abgeleitete),
24 Zapfkategorien (je Nutzungsart der Vorgabesatz ihrer Gruppe), 4 Bedarfstage (3 fiktive, das
Ecodesign-Zapfprofil) mit 33 Ereignissen, 85 Parameter (72 fiktive, 13 freie), 5 DIN-4708-Werte.
Keine Zeile trägt Status `AUSLIEFERUNG` oder `IMPORT`.

## Paketvorlage der A100-Typen (`Katalogpaket_Vorlage_A100/`)

Die Nichtwohn-Nutzungsarten des Beiblatts A100 der DIN EN 12831-3 (Hotels, Krankenhäuser,
Sportstätten, Schulen, Bürogebäude) kommen **nicht** aus dem Repositorium, sondern als eigenes
Katalogpaket des Anwenders (Anwenderentscheid ZU24). Im Repositorium liegt allein die Vorlage:
[`Katalogpaket_Vorlage_A100/`](Katalogpaket_Vorlage_A100/LIESMICH.md) mit den vier Dateien des
Importformats, vollständigen Kopfzeilen und je einer Beispielzeile aus **Platzhaltern** — keine
Normzahl. Die Anleitung, die Wertemengen, die Summenregeln, die Ablehnungsgründe und eine Liste
empfohlener Typnamen (nur Namen) stehen in ihrer `LIESMICH.md`; die **gefüllte** Datei gehört nie
ins Repositorium. Zwei Fälle in `EPOS.Kern.Tests/TwwKatalogimportTests` halten die Vorlage: Sie
spielt ohne Ablehnung ein, und jede ihrer Zahlen ist ein Platzhalter.

## Entfernte Basen

**`Referenzlaeufe/Importproben` gehört zum Testbestand und wird nie gelöscht; wer die Ordner der
Basen aufräumt, lässt `2026-09-25_R16_Anlagenprio`, `Kenndaten_Test.sqlite`,
`Importproben`, `Katalogpaket_frei`, `Katalogpaket_Vorlage_A100`, `Skripte` und `LIESMICH.md`
stehen.**

Außer der aktuellen liegt hier keine Basis mehr: Die 24 historischen Referenzbasen (7 731
Dateien, 1 016,7 MB) sind am 11.09.2026 aus dem Arbeitsbaum gefallen (**SYNC‑Q1**: „entfernt
lassen") und seit dem Umschreiben der Git-Geschichte (**AUF‑Q1**, 12.09.2026) auch dort nicht
mehr enthalten; **`2026-09-11_R7_Speicherflotte` ist am 16.09.2026 nach demselben Muster
gefallen, `2026-09-16_R8_Heizkessel_Kaskade` am 18.09.2026,
`2026-09-18_R9_Kesselbrennstoff` am 19.09.2026, `2026-09-19_R10_BhkwWirkungsgrad` am
22.09.2026, `2026-09-22_R11_Bestandsbefunde` und `2026-09-23_R12_Gebaeudemodell` am 23.09.2026,
`2026-09-23_R13_Kuehlung` am 24.09.2026, `2026-09-24_R14_Kaelteerzeuger` und `2026-09-25_R15_Anlagenkopplung` am 25.09.2026** (33 Basen,
alle neun Protokolle gesichert). Kein Test, kein Gate, keine CI liest
eine entfernte Basis. **Die Messdaten sind endgültig weg** (rund 8 000 CSV-Dateien) — eine
alte Zahl steht nur noch im Protokoll.
Erhalten sind die **Protokolle** aller 33 Basen samt der Tabelle Basis → Datum → Zweck →
Protokoll unter
[`Dokumentation/ueberholt/Referenzbasen/`](../Dokumentation/ueberholt/Referenzbasen/LIESMICH.md);
**welche Basis wann von welcher abgelöst wurde und warum**, steht ebendort — bis zum 12.09.2026
in der ausgelagerten
[Basenhistorie](../Dokumentation/ueberholt/Referenzbasen/LIESMICH_Basenhistorie_bis_2026-09-12.md),
danach im Wegweiser desselben Ordners.

## Aktuelle Basis

**`2026-09-25_R16_Anlagenprio/`** — **vierzehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024,
1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047), **432 CSV**, **2 447 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite` (Schemastand
**144**, LFS-SHA-256 `9a71b714…` — R16 wurde auf der Fassung `1360e2be…` mit Schemastand 142 eingefroren
(dieselbe Datei, auf der R15 zuletzt gehalten wurde; Herleitung samt Projekt 1047 im Abschnitt der Basis R15
unter [`Dokumentation/ueberholt/Referenzbasen/`](../Dokumentation/ueberholt/Referenzbasen/LIESMICH.md)); danach
änderten #504 nur 93 Zellen des Tww-Testkatalogs, Schemaschritt 143 nur zwei Quelltexte des
Baustoffkatalogs und Schemaschritt 144 nur das Schema (zwei leere Spalten der Nachtzeit), alle ohne
Referenzwirkung (Nachträge unten). Gegen
diese Basis hält `.github/workflows/kern.yml` (1030, 1007, 1017, 1045, 1046, 1047) jeden Push, `ios.yml`
den iZ6-Vergleich für 1030, und `EPOS.Kern.Tests/GebaeudeRueckwegTests` den Tagesbilanz-Weg an Projekt
1040. Sie ist die **einzige** Basis im Arbeitsbaum.

> **Anlass: der Anwenderentscheid vom 25.09.2026 zu Konzept Wirtschaftlichkeit § 6.3 Nr. 18** — der
> Rechenweg ordnet die Anlagen nach derselben Regel wie Hydraulikbild und Erzeugerkarten
> (`Ladeordnung.SqlAnlagenprio`, Regel „99“: gepflegte Priorität zuerst, eine Anlage ohne Priorität —
> NULL oder 0 — hinten, bei Gleichstand die ID). Eine Änderung am Rechenweg, keine an den Daten, kein
> Schemaschritt:
>
> 1. **Die fünf Rechenweg-Leser**, die bis dahin `ORDER BY Prioritaet, ID` lasen und damit die
>    ungepflegte Anlage VOR die gepflegte stellten: `SimulationControl.WP_Liste_Laden`,
>    `…QuellbezuegeAufbauen`, `…SenkenPufferDerAnlagen`, `WaermesenkeClass.SenkenLaden`,
>    `…SenkenlistenLaden`.
> 2. **Die drei Modul-Lader**, die ganz ohne `ORDER BY` in der Zeilenfolge der Datenbank luden:
>    `SimulationControl.SPK_Liste_Laden`, `…Solar_Liste_Laden`, `…BHKW_Liste_Laden`.
>
> **Allein 1042 bewegt sich, und nur im Index der Module** — alle Werte und alle Zeitreihen bleiben
> Zeichen für Zeichen gleich, 1047 und die übrigen zwölf Projekte sind in allen Dateien byte-gleich zu R15.
> Die beiden Wärmepumpen von 1042 tauschen die Plätze in `aggregate.csv` (10 Werte):
>
> | 1042, `aggregate.csv` | R15 `WaermepumpeModul[0]` | R15 `[1]` | R16 `WaermepumpeModul[0]` | R16 `[1]` |
> |---|---|---|---|---|
> | `.Modul` (Anlage, Priorität) | CS7800iLW 16 (14818, keine) | CS6800iAW MB + AW 10 OR-T (14817, 1) | CS6800iAW MB + AW 10 OR-T (14817, 1) | CS7800iLW 16 (14818, keine) |
> | `.Leistung` [kW] | 15 | 11 | 11 | 15 |
> | `.Waermeproduktion` [MWh/a] | 20,84 | 71,45 | 71,45 | 20,84 |
> | `.Stromverbrauch` [MWh/a] | 7,06 | 26,29 | 26,29 | 7,06 |
> | `.Betriebsstunden` [h] | 2 073,4 | 5 995,29 | 5 995,29 | 2 073,4 |
>
> **A/B der beiden Teile** (gemessen vor dem Zusammenführen mit AK1 Welle 5 an den dreizehn Projekten
> gegen R14, danach an allen vierzehn gegen R15):
>
> | Stand | Vergleich | Ergebnis |
> |---|---|---|
> | Teil 1 allein | gegen R14 (13 Projekte) | 12/13 PASS und byte-gleich; 1042 FAIL mit den **10 Werten** oben, die übrigen 35 Dateien byte-gleich |
> | Teil 1 + 2 | gegen Teil 1 allein (13 Projekte) | **394/394 CSV byte-gleich** — Teil 2 ändert in keinem Projekt etwas, nicht einmal einen Index |
> | Teil 1 + 2 | gegen R15 (14 Projekte, Schemastand 142) | 13/14 PASS, 431/432 CSV byte-gleich; allein 1042 `aggregate.csv` mit denselben **10 Werten**; 1047 PASS und byte-gleich (38 Dateien) |
>
> **Warum ohne Rechenwirkung:** Die Deckungsreihenfolge legt die Kaskade über den Typ fest (`Tool_1..4`),
> Anlagen finden ihre Senken über die Anlagen-ID und Puffer über `Z_AnlageSenke.Ladeprio`; die Rechenfolge
> der Wärmepumpen von 1042 steht in `ModulEbenen` (getrennte Senken: 14817 Heizkreis und Puffer 1054196,
> 14818 nur Brauchwasserpuffer 1054202). Die Reihenfolge der Senken- und Pufferlisten ändert sich außerdem
> in 1030 (BHKW ohne Priorität hinter BHKW und Kessel mit Priorität) und in 1040, 1041, 1045 (Kessel ohne
> Priorität hinter der Wärmepumpe mit Priorität 1) — ohne Wirkung auf eine Zahl. Teil 2 greift in keinem
> Projekt: Keines führt zwei Kessel oder zwei Kollektorfelder, und die beiden BHKW von 1030 stehen nach der
> Regel wie nach der Zeilenfolge (14920 mit Priorität 1 vor 14921 ohne). Rechnerisch wirkt die Regel erst
> bei zwei Anlagen gleichen Typs auf derselben Rechenebene und Senke, deren Priorität von der ID abweicht.
>
> **Kein Fehlschlag, keine Ablehnung:** 14/14 Projekte gerechnet; NaN nur in den gewollten Lücken der
> Vorlauf- und Rücklaufreihen von 1047 (wie in R15).
>
> **Einfrierregeln:** nicht berührt — keine gesäten Daten geändert, die Testdatenbank ist byte-gleich.
>
> **Determinismus geprüft:** zwei Läufe desselben Standes nacheinander **14/14 byte-gleich** (432/432 CSV)
> und untereinander **GESAMT: PASS** (4 610 207 Werte); der Einfrierlauf ist mit beiden byte-gleich.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047 \
>   --ziel Referenzlaeufe/2026-09-25_R16_Anlagenprio
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis. Nachweis im Test:
> `EPOS.Kern.Tests/AnlagenprioRechenwegTests` (die acht Leser nutzen die Regel; in 1042 steht die
> Wärmepumpe ohne Priorität hinter der mit Priorität 1).

> **Nachtrag #504: die abgeleiteten VDI-6002-Typen im Tww-Testkatalog, die Basis bleibt.**
> **Kein Schemaschritt** — der Folgeposten der Anwenderentscheide ZU20 und ZU24 vom 25.09.2026 setzt
> allein Werte: Quelle, Version und Herkunftsart `VERFAHREN` der fünf Nutzungsarten „… (abgeleitet)“
> und ihrer Tagesgänge (Abschnitt „Abgeleitete VDI-Werte im Tww-Testkatalog“ oben). Nachgezogen auf der
> Fassung von origin mit Schemastand **142** (AK1 Welle 5 oben, `1360e2be…`) mit
> `py Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py Referenzlaeufe/Kenndaten_Test.sqlite --stochastik`:
> 0 Zeilen angelegt, 21 nachgeführt; ein zweiter Lauf schreibt nichts (0 neu, 0 nachgeführt).
> Zellvergleich aller 144 Tabellen gegen die Fassung von origin (10 645 525 Zellen): abweichend allein
> **93 Zellen** — 45 in `Tab_TwwNutzungsart_STAMM` und 48 in `Tab_TwwTagesgang_STAMM` —, Zeilenzahlen
> unverändert, Schema gleich, `sqlite_sequence` gleich (88 Zeilen). `integrity_check` ok,
> `foreign_key_check` leer, 144 Tabellen (alle STRICT), 14 Sichten, 219 Indizes samt den von SQLite
> angelegten, keine `AUSLIEFERUNG`-Zeile. Größe 68 747 264 Byte (LFS-SHA-256 `22eeb75c…`).
> **Ergebnisneutral:** Keines der vierzehn Referenzprojekte führt eine Tww-Zone, und kein Rechenweg
> der vierzehn liest den Tww-Katalog. **Keine Einfrierregel ist berührt.** Referenzlauf der sechs
> Projekte der CI (1030, 1007, 1017, 1045, 1046, 1047) gegen diese Basis: **6/6 PASS** (198 Dateien,
> 2 208 587 Werte).

> **Nachtrag Schemastand 143 (Herkunft der Rohdichte in der Baustoffsaat, E39), die Basis bleibt.**
> Migrationsschritt **143** (`SCHRITT_BAUSTOFF_QUELLEN`; die Nummer steht allein bei
> `BaustoffQuellenBerichtigung.SCHRITT`, der Quelle für Migration, Werkzeug und Testvorrichtung; er folgt
> auf die dritte Berichtigung der Anschlusslängen, 142) setzt die Regel aus E39 in bestehenden Datenbanken
> durch (Konzept Gebäudesimulation N1.44, „Benannte Lücken“): Stammt die Rohdichte einer Herstellerzeile
> aus einer Umweltproduktdeklaration, dann nennt die Quelle das. **Reines DML** an der Spalte `Quelle`
> zweier Saatzeilen — in `Tab_Baustoff_STAMM` über die Saat-Id, in der Projektkopie `Tab_Baustoff` über
> Hersteller und Bezeichner (den Schlüssel von `BaustoffCtrl.CopyFromStamm`), jeweils nur, wo der alte
> Saattext wortgleich steht; eine vom Anwender geänderte Quelle bleibt. Die Saat trägt die neuen Texte,
> eine neue Datenbank bekommt sie gleich.
>
> | Id | Quelle vorher | angefügt |
> |---|---|---|
> | 1041 | `Kingspan, Produktblatt Kooltherm K5 WDVS-Dämmplatte (DE), Version 15, 07/2026` | `; Rohdichte aus FDES 120 mm` |
> | 1066 | `Baumit, Produktdatenblatt DämmPutz DP 85, 18.09.2025` | `; Rohdichte Mindestwert A2-s1,d0 nach VDPM-EPD` |
>
> Nachgezogen auf der Fassung von origin mit Schemastand **142** (Nachtrag #504 oben, `22eeb75c…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`:
> offen vorher 2, berichtigt 2 Katalogzeilen und 0 Projektkopien (die Testdatenbank führt keine
> Projektkopie eines Baustoffs), offen danach 0, Marker 143; ein Trockenlauf danach findet nichts offen.
> Zellvergleich aller Tabellen samt `sqlite_sequence` gegen die Fassung 142: allein `SchemaVersion`
> 142 → 143 und die zwei Quellzellen; Schema gleich, Zeilenzahlen unverändert. `integrity_check` ok,
> `foreign_key_check` leer, 144 Tabellen (alle STRICT), 14 Sichten, 219 Indizes samt den von SQLite
> angelegten. Größe 68 714 496 Byte (das Werkzeug verdichtet mit `VACUUM`; LFS-SHA-256 `76dd9e48…`).
> **Ergebnisneutral:** Kein Rechenweg liest den Baustoffkatalog, und kein Referenzprojekt hat eine Zone.
> **Keine Einfrierregel ist berührt** — der Baustoffkatalog gehört nicht zu den gesäten Gebäudedaten.
> Referenzlauf aller vierzehn Projekte gegen diese Basis: **14/14 PASS** (4 610 207 Werte), 432/432 CSV
> byte-gleich.

> **Nachtrag Schemastand 144 (Nachtzeit je Gebäude, E43), die Basis bleibt.** Migrationsschritt
> **144** (`SCHRITT_NACHTZEIT`; die Nummer steht allein bei `NachtzeitSchema.SCHRITT`, der Quelle für
> Migration, Werkzeug und Testvorrichtung; er folgt auf die Herkunft der Rohdichte, 143) legt an
> `Tab_Gebaeude` und `Tab_Gebaeude_STAMM` je zwei nullbare Spalten `Nachtabsenkung_Beginn` und
> `Nachtabsenkung_Ende` an (`INTEGER`, `CHECK … IS NULL OR … BETWEEN 0 AND 23`, Stunde des Tages; die
> Nacht ist [Beginn, Ende), zyklisch über Mitternacht) und baut die Sicht `Abfrage_Projektgebaeude` zum
> sechsten Mal neu — mit allen Spalten der fünf früheren Durchgänge samt Kühlübergabe und Baujahr, 101
> Spalten, als letzter Sichtneubau. **Reines DDL, keine Saat:** Beide Spalten stehen überall auf NULL,
> und NULL heißt die Vorgabe 22 bis 6 Uhr, abgeleitet aus den Stunden des Tagsollwerts und bitgleich mit
> dem Fahrplan davor. Der Tagesbilanz-Weg (Projekt 1040) liest die Spalten nicht.
>
> Nachgezogen auf der Fassung von origin mit Schemastand **143** (Nachtrag oben, `76dd9e48…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`:
> 4 von 4 Spalten angelegt, Sicht mit 101 Spalten, Marker 144; ein zweiter Lauf legt nichts an.
> Zellvergleich aller Tabellen gegen die Fassung 143: allein `SchemaVersion` 143 → 144, die vier neuen
> Spalten überall NULL und die Schematexte von `Tab_Gebaeude`, `Tab_Gebaeude_STAMM` und
> `Abfrage_Projektgebaeude`; Zeilenzahlen unverändert. `integrity_check` ok, `foreign_key_check` leer,
> 144 Tabellen (alle STRICT), 14 Sichten, 219 Indizes samt den von SQLite angelegten. Größe 68 714 496
> Byte (LFS-SHA-256 `9a71b714…`). **Keine Einfrierregel ist berührt** — die Spalten sind leer, keine
> gesäte Gebäudeangabe ändert sich. Referenzlauf aller vierzehn Projekte gegen diese Basis: **14/14
> PASS** (4 610 207 Werte), 432/432 CSV byte-gleich.

> **Die Vorgängerbasis `2026-09-25_R15_Anlagenkopplung`**, die erste Basis mit Anlagenkopplung, ist mit
> dieser Einfrierung aus dem Arbeitsbaum gefallen; ihr Protokoll samt der Begründung zum Referenzprojekt
> 1047 und dem Nachtrag zu Schemastand 142 steht in
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
   `2026-09-25_R16_Anlagenprio`, ist plattformfrei gegen `Kenndaten_Test.sqlite` gerechnet:
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
