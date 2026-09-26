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

## Die Einfrierregel „gesäte Zapfprofil-Eingaben“ (Umsetzungskonzept Zapfprofilgenerator 3.4, ZU7)

Siebter Ort derselben Falle. Ein Referenzprojekt, das über die Weiche (3.4) auf den
Zapfprofilgenerator umgestellt ist, rechnet sein Brauchwasser nicht mehr aus der eingefrorenen
Jahresreihe der Testdatenbank, sondern aus der Bilanz des Generators — Kalender, Tagesgang,
Kaltwasser-Jahresgang und Zirkulationsmethode der benutzten Katalogzeile, dazu Seed und
Realisierungszahl der Zone. Eine einzige geänderte Zelle der Projektzeile, der Zone oder der
benutzten `Tab_Tww*_STAMM`-Zeile verschiebt die Stundenreihe `waermebedarf_brauchwasser.csv` und
mit ihr die Deckung der Erzeuger, den Speicherzustand, Netzbezug, Kosten und CO₂.

> **Wer gesäte Zapfprofil-Eingaben eines Referenzprojekts in der Testdatenbank ändert, friert im
> selben Schritt die Basis neu ein und begründet den Wechsel hier.**
>
> Betroffen ist jede Änderung an `Tab_TwwProjekt` (`Weg`, Seed, Realisierungen, Perzentil,
> Zirkulationsmethode, Speicherart, Personen-Automatik, Typtage-Schalter) und an den Zonen
> (`Tab_TwwZone`: Bezugsmenge, Niveau, Topologie, Zirkulation, Tagesbedarf-Automatik, gebundene
> Nutzungsart) eines Referenzprojekts, dazu die Katalogzeilen, die eine solche Zone benutzt
> (`Tab_TwwNutzungsart_STAMM` samt ihrem Tagesgangsatz `Tab_TwwTagesgang_STAMM`) und die drei
> Kaltwasser-Parameter der Bilanz (`ZapfParameter.KALTWASSER_*`). Ebenso betroffen ist das
> Umstellen eines Referenzprojekts auf den Generator oder zurück auf den Bestandsweg.
>
> **Rechenwirkung hat allein Projekt 1045 „Prüfprojekt Ost/West Stränge"**: eine Projektzeile mit
> `Weg = GENERATOR` und eine Zone der Nutzungsart „Wohnen groß (abgeleitet)" am Gebäude 10651 —
> zwei Zeilen samt der Begründung der Werte in
> [`Skripte/referenzprojekt_zapfprofil.py`](Skripte/referenzprojekt_zapfprofil.py), wiederholbar
> und gehalten von `EPOS.Kern.Tests/ZapfprofilReferenzprojektWacheTests`. Die übrigen dreizehn
> Referenzprojekte tragen weder eine Projektzeile noch eine Zone und rechnen unverändert auf dem
> Bestandsweg (gehalten von `ZapfprofilCtrlTests`/`ZapfprofilWeicheTests`).
>
> **Nicht** betroffen sind Projekte außerhalb der Referenzliste (etwa 1006, 1009 — Träger der
> Schreibweg-Tests) und der Rechenweg des Generators selbst — der wird gegen die Basis gehalten
> wie jeder andere.

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
(`Zapfprofil.Stochastik.*`, `…Zirkulation.*`, `…Anzeige…`, `…Validierung.*`), die neun
Ecodesign-Zapfprofile XXS bis 4XL (Verordnung (EU) Nr. 814/2013 Anhang III) samt 161 Ereignissen
und die fünf aus VDI 6002 **abgeleiteten** Nutzungsarten samt vier Tagesgangsätzen und sechzehn
Tagesgängen
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

Die Zählungen der Tww-Katalogtabellen der Testdatenbank (Schemastand 148): 5 Tagesgangsätze
(1 fiktiver, 4 abgeleitete), 20 Tagesgänge, 8 Nutzungsarten (3 fiktive, 5 abgeleitete),
24 Zapfkategorien (je Nutzungsart der Vorgabesatz ihrer Gruppe), 12 Bedarfstage (3 fiktive, die
neun Ecodesign-Zapfprofile XXS bis 4XL) mit 170 Ereignissen, 85 Parameter (72 fiktive, 13 freie),
5 DIN-4708-Werte.
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
Basen aufräumt, lässt `2026-09-26_R21_BhkwDeckung`, `Kenndaten_Test.sqlite`,
`Importproben`, `Katalogpaket_frei`, `Katalogpaket_Vorlage_A100`, `Skripte` und `LIESMICH.md`
stehen.**

Außer der aktuellen liegt hier keine Basis mehr: Die 24 historischen Referenzbasen (7 731
Dateien, 1 016,7 MB) sind am 11.09.2026 aus dem Arbeitsbaum gefallen (**SYNC‑Q1**: „entfernt
lassen") und seit dem Umschreiben der Git-Geschichte (**AUF‑Q1**, 12.09.2026) auch dort nicht
mehr enthalten; **`2026-09-11_R7_Speicherflotte` ist am 16.09.2026 nach demselben Muster
gefallen, `2026-09-16_R8_Heizkessel_Kaskade` am 18.09.2026,
`2026-09-18_R9_Kesselbrennstoff` am 19.09.2026, `2026-09-19_R10_BhkwWirkungsgrad` am
22.09.2026, `2026-09-22_R11_Bestandsbefunde` und `2026-09-23_R12_Gebaeudemodell` am 23.09.2026,
`2026-09-23_R13_Kuehlung` am 24.09.2026, `2026-09-24_R14_Kaelteerzeuger`, `2026-09-25_R15_Anlagenkopplung`, `2026-09-25_R16_Anlagenprio`, `2026-09-25_R17_Datenpflege`, `2026-09-25_R18_PvAusweis` am 25.09.2026, `2026-09-25_R19_BhkwNetzbezug` und `2026-09-26_R20_Zapfprofil` am
26.09.2026** (38 Basen,
alle vierzehn Protokolle gesichert). Kein Test, kein Gate, keine CI liest
eine entfernte Basis. **Die Messdaten sind endgültig weg** (rund 8 000 CSV-Dateien) — eine
alte Zahl steht nur noch im Protokoll.
Erhalten sind die **Protokolle** aller 38 Basen samt der Tabelle Basis → Datum → Zweck →
Protokoll unter
[`Dokumentation/ueberholt/Referenzbasen/`](../Dokumentation/ueberholt/Referenzbasen/LIESMICH.md);
**welche Basis wann von welcher abgelöst wurde und warum**, steht ebendort — bis zum 12.09.2026
in der ausgelagerten
[Basenhistorie](../Dokumentation/ueberholt/Referenzbasen/LIESMICH_Basenhistorie_bis_2026-09-12.md),
danach im Wegweiser desselben Ordners.

## Aktuelle Basis

**`2026-09-26_R21_BhkwDeckung/`** — **vierzehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024,
1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047), **432 CSV**, **2 447 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite` (Schemastand **149**,
70 688 768 Byte, LFS-SHA-256 `217a519b…` — die Fassung `22e67400…` der Speicherauslegung (#543) mit der
Datenpflege der Betriebskosten von 1030 und 1026, Nachtrag unten). Gegen diese Basis hält
`.github/workflows/kern.yml` (1030, 1007, 1017, 1045, 1046, 1047) jeden Push, `ios.yml` den
iZ6-Vergleich für 1030, `EPOS.Kern.Tests/GebaeudeRueckwegTests` den Tagesbilanz-Weg an Projekt 1040 und
`EPOS.Kern.Tests/ZapfprofilReferenzprojektWacheTests` die Generator-Bilanz von Projekt 1045. Sie ist die
**einzige** Basis im Arbeitsbaum.

> **Anlass: der Stromdeckungsgrad des BHKW ist sein Eigenverbrauch am Bedarf aller Verbraucher**
> (Welle E30, Statusnummer #548; Befund N10 aus E29, Anwenderentscheid 26.09.2026 „nach Empfehlung
> korrigieren", E30‑Q7 a). `BHKW.Strombedarfsdeckung` rechnete bis R20 die ganze Erzeugung samt
> Einspeisung am Projekt-Strombedarf; jetzt gilt `(Erzeugung − KWK-Einspeisung) ÷ Σ Strombedarf der
> Verbraucher` (Projektlast, Wärmepumpe, Heizstab, Elektrokessel, Kälte), geklemmt auf 0…100 — eine
> Formel für Lauf, BHKW-Reiter und Übersicht (`SimulationErgebnisCtrl.BhkwStromdeckungProzent`).
>
> **A/B gegen R20** (14 Projekte): **11/14 PASS**, **429/432 CSV byte-gleich**; FAIL allein je ein
> Skalar in drei `aggregate.csv`, alle Zeitreihen byte-gleich:
>
> | Projekt, Datei, Größe | R20 | R21 | Grund |
> |---|---|---|---|
> | 1017 `aggregate.csv` `BHKW.Strombedarfsdeckung` | 5,48 | 5,31 | Nenner mit Wärmepumpe, Elektrokessel und Kälte: 36,80 ÷ 692,68 MWh |
> | 1024 `aggregate.csv` `BHKW.Strombedarfsdeckung` | 26,22 | 20,94 | Nenner mit Wärmepumpe, Heizstab und Elektrokessel: 95,70 ÷ 456,98 MWh |
> | 1047 `aggregate.csv` `BHKW.Strombedarfsdeckung` | 5,34 | 5,30 | Nenner mit Wärmepumpe, Elektrokessel und Kälte: 35,87 ÷ 677,04 MWh |
>
> 1030 bleibt gerundet 9,02 (Eigenverbrauch 431,91 statt 432,31 MWh, 0,39 MWh Einspeisung; 9,025 → 9,017),
> 1018 bleibt 0 (kein Strombedarf, die ganze Erzeugung wird eingespeist).
>
> **Kein Fehlschlag, keine Ablehnung:** 14/14 Projekte gerechnet. **Determinismus:** Der Einfrierlauf
> ist mit dem A/B-Lauf 432/432 CSV byte-gleich.
>
> ```bash
> dotnet run Referenzlaeufe/Skripte/datenpflege_1030_1026_betriebskosten.cs -- Referenzlaeufe/Kenndaten_Test.sqlite
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047 \
>   --ziel Referenzlaeufe/2026-09-26_R21_BhkwDeckung
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis.

> **Nachtrag Datenpflege der Betriebskosten 1030/1026 (E30/1, Befunde B3 und B5 der Sichtprüfung 1030)
> ohne Rechenwirkung auf die Basis.** [`Skripte/datenpflege_1030_1026_betriebskosten.cs`](Skripte/datenpflege_1030_1026_betriebskosten.cs)
> (dotnet-Dateiskript, wiederholbar: Vorzustand zellgenau, eine Transaktion in einer Arbeitsdatei,
> zweiter Lauf 0) zieht die Wartung von 1030 in die Pflichtzeilen (101600588 „Wartung BHKW" 18 000 €/a,
> 101600585 „Vollwartung / Wartung Kessel" 2 000 €/a, beide als fester Jahresbetrag), löscht die zwei
> Altzeilen ohne Vorlage (101600097, 101600098) und stellt fünf Hilfsenergie-Pflichtzeilen (1030:
> 101600587, 101600590, 101600593; 1026: 101600570, 101600576) von „% der Endenergiekosten" auf „% des
> Endenergiebedarfs" (ohne Satz). Gezogen auf `22e67400…` (Speicherauslegung #543): 9 Zeilen,
> `integrity_check` ok, `foreign_key_check` leer → `217a519b…`, 70 688 768 Byte. **Keine Einfrierregel
> ist berührt**, der Referenzlauf rechnet keine Wirtschaftlichkeit; 1030 behält 20 000 €/a
> Betriebskosten, die Kapitalwert-Anker sind bitgleich (`EPOS.Kern.Tests/DatenpflegeBetriebskosten1030Tests`).

> **Schemaschritt S-G (147, Zonenkopplung) ohne neue Basis.** Der Schritt legt `Tab_Bauteil.ID_Nachbarzone`
> und `Tab_Bauteil.Trennflaeche_Zuordnung` an, dazu `Tab_Zonenluftstrom` und `Tab_ErgebnisZone` (STRICT).
> Die Testdatenbank wurde aus der origin-Fassung (Schemastand 146) mit `Werkzeuge/Testdatenbankschema`
> nachgezogen — zwei Spalten, zwei Tabellen, fünf Indizes; ein zweiter Lauf 0/0. Zellvergleich über
> 10 893 413 Zellen: einzige Abweichung `Tab_Applikation.SchemaVersion` 146 → 147, die neuen Spalten
> leer, die neuen Tabellen leer; `integrity_check` ok, `foreign_key_check` leer, STRICT 149 von 150;
> 70 664 192 Byte, LFS-SHA-256 `40c9cf26626e4c13461dc64d3c9f57cd6c79eeeac00453ee54b989b48f2efb0f`.
> Referenzlauf 14/14 PASS, 432/432 CSV byte-gleich gegen R19. Kein Referenzprojekt trägt Zonen, keine
> Einfrierregel ist berührt; mit der Freischaltung (G6b W5) bleibt der Lauf gegen R20 14/14 PASS und 432/432 CSV byte-gleich.

> **Schemaschritt 149 (Katalogsätze M/A) ohne Neufreigabe.** Entscheid E51 sät sechs Sätze in
> `Tab_Gebaeude_STAMM` (`ReadOnly = 1`, Schlüssel ist der Bezeichner): `EFH-GEG-Ref`, `EFH-GEG-EH55`,
> `KMH-GEG-typ` (Klasse M) und `EFH-bis1859-U`, `KMH-bis1859-U`, `EFH-bis1859-TS` (Klasse A); Quelle
> `GebaeudeSaatSchema`. Die Testdatenbank wurde aus der Fassung `22e67400…` (Schemastand 148) mit
> `Werkzeuge/Testdatenbankschema` nachgezogen — sechs Zeilen, ein zweiter Lauf 0/0. Tabellenvergleich:
> einzige Abweichungen `Tab_Applikation.SchemaVersion` 148 → 149 und die sechs neuen Zeilen
> (`Tab_Gebaeude_STAMM` 269 → 275); `integrity_check` ok, `foreign_key_check` leer; 70 688 768 Byte,
> LFS-SHA-256 `4c8ed3982a35c561a12b084b11a26c13d489c6c028fc9c07872b76ed3585e093`. **Die Einfrierregel
> „gesäte Gebäudedaten" ist nicht berührt:** Sie hält die Gebäude der Referenzprojekte samt ihrer
> Zuordnungen; die sechs Sätze führt kein Referenzprojekt (keine Zuordnung, kein `ID_Gebaeude_Stamm`),
> und kein Rechenweg liest den Katalog, die Klasse oder die Vorgaben des Imports. Referenzlauf 14/14
> PASS, 432/432 CSV byte-gleich gegen R20.
> Beim Merge mit E30 (#548) wurde diese Fassung um die Datenpflege 1030/1026 ergänzt (wiederholbares Skript
> `Referenzlaeufe/Skripte/datenpflege_1030_1026_betriebskosten.cs`, 9 Zeilen, zweiter Lauf 0/0): 70 688 768 Byte,
> LFS-SHA-256 `217a519b136cdc99d941252575af24ea3e201d23b8415b48b384e495a857313b`; Referenzlauf 14/14 PASS, 432/432 CSV byte-gleich gegen R21.

> **Die Vorgängerbasis `2026-09-26_R20_Zapfprofil`** ist mit dieser Einfrierung aus dem Arbeitsbaum
> gefallen; ihr Protokoll steht in
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
| `Skripte/` | Was an dieser Testdatenbank gemacht wurde, als Skript und nicht als Erzählung: `pruefprojekt_1045_ost_west.py` (W6‑O‑7), `pruefprojekt_1046_speicherflotte.py` (SP‑O‑8), `anlagenkopplung_1047_referenzprojekt.py` (Referenzprojekt der Anlagenkopplung, Kopie von 1017), `pruefprojekt_1048_pv_preise.cs` (dotnet-Dateiskript: Prüfprojekt 1048 „PV mit Preisen“, ohne Referenzrolle), `gebaeude_10576_bauweise.py` (Stufe GB, Befund D), `gebaeude_10612_233_bauweise.py` (dieselbe Korrektur an 1009 und Katalogsatz 233, Basis unverändert), `tww_testkatalog_fiktiv.py` (Testkatalog des Zapfprofilgenerators samt abgeleiteten VDI-Werten und den Zeilen des freien Paketteils, Schemastand 115) `normzahlen_abgeleitet_bauen.py` (nur lokal: abgeleitete VDI-6002-Werte nach `tww_katalogwerte_abgeleitet.json` und abgeleitete VDI-4655-Werte nach `vdi4655_abgeleitet.json`, ZU19; `--norm vdi6002|vdi4655|beide`) und `referenzprojekt_zapfprofil.py` (stellt Projekt 1045 auf den Zapfprofilgenerator um, ZU7; die Einfrierregel „gesäte Zapfprofil-Eingaben" oben) |

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

### Das Prüfprojekt 1048 „PV mit Preisen“ (ohne Referenzrolle)

Projekt **1048 „Prüfprojekt PV mit Preisen“** ist das einzige PV-Projekt der Testdatenbank mit einem
vollständigen Preissatz: Kopie von 1040 auf dem Kopierweg des Programms, Gebäude nach VDI 6007
(der Tagesbilanz-Weg bleibt allein bei 1040), 40 Module = 10,40 kWp, Strom 0,30 €/kWh (Günstig 0,26 /
Ungünstig 0,36) und 120 €/a (100 / 150), Erdgas 0,80 €/Nm³ (0,70 / 0,95) und 150 €/a, Parametersatz
mit Einspeisevergütung PV 0,08 €/kWh (0,10 / 0,06), PV-Investition 1.200 €/kWp (Kategorie 1),
Wartung 150 €/a (120 / 180) und Instandhaltung 1 % der Investition (Kategorie 2); Flat-Tarif, keine
Vergütungszeile, keine gespeicherten Ergebnisse. Alle Werte sind neutrale Prüfwerte.

**1048 ist kein Referenzprojekt:** Es steht in keiner Basis und in keiner Projektliste der CI, für
seine Zeilen gilt keine Einfrierregel, und eine Änderung an 1048 bewegt keine Basis. Es hält die
PV-Erlösseite der Wirtschaftlichkeit in `EPOS.Kern.Tests/PvPreisProjektTests` (Einspeiseerlös je
Szenario, Szenarien C und D, vermiedene Kosten, Formelmappe, Kapitalwert-Anker).

```bash
dotnet run Referenzlaeufe/Skripte/pruefprojekt_1048_pv_preise.cs -- Referenzlaeufe/Kenndaten_Test.sqlite [--trocken]
```

Das Skript ist wiederholbar: Steht 1048 mit allen Zielzellen, ändert es nichts (Rückgabe 0); weicht
etwas ab, bricht es ab, ohne die Datei zu ändern (Rückgabe 2). Es schreibt in eine Arbeitsdatei, prüft
die Zielzellen, die Unversehrtheit von 1040, `integrity_check` und `foreign_key_check` und ersetzt erst
dann die Datenbank. Die Zeilen-IDs vergibt der Kopierweg; nach einer Neufassung der Testdatenbank ohne
1048 wird das Skript auf der neuen Fassung erneut gezogen.

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
   `2026-09-25_R19_BhkwNetzbezug`, ist plattformfrei gegen `Kenndaten_Test.sqlite` gerechnet:
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
