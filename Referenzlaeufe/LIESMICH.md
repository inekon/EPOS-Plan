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
> und gehalten von `EPOS.Kern.Tests/ZapfprofilReferenzprojektWacheTests`. Die übrigen vierzehn
> Referenzprojekte tragen weder eine Projektzeile noch eine Zone und rechnen unverändert auf dem
> Bestandsweg (gehalten von `ZapfprofilCtrlTests`/`ZapfprofilWeicheTests`).
>
> **Nicht** betroffen sind Projekte außerhalb der Referenzliste (etwa 1006, 1009 — Träger der
> Schreibweg-Tests) und der Rechenweg des Generators selbst — der wird gegen die Basis gehalten
> wie jeder andere.

## Die Einfrierregel „gesäte Solardaten“ (Referenzprojekt 1049)

Achter Ort derselben Falle. Projekt 1049 „Referenzprojekt Solarthermie“ ist das einzige
Referenzprojekt, dessen Kollektorfeld deckt: Es deckt direkt am Heizkreis und lädt vorrangig
den Puffer, den BHKW und Kessel nachrangig laden. Wie viel Solarwärme genutzt wird, hängt am
Feld (Kollektorsatz, Modulanzahl, Neigung, Azimut), am Puffer (Volumen, Temperaturpaar,
Abschaltschwellen) und an der Ladeordnung, an der Arbeitstemperatur des Felds (aus der untersten Zone
des Puffers plus Grädigkeit und halber Spreizung, Welle M2) — und an der Nachrang-Vorgabe: Weil
`Schwelle_Aus_Nachrang` leer ist, laden BHKW und Kessel nur bis 30 %
(`Ladeordnung.SCHWELLE_AUS_NACHRANG_SOLAR_DEFAULT`); eine gepflegte Zahl dort verschiebt
Solarertrag, BHKW-Laufzeit, Kesselwärme, Speicherzustand und Emissionen.

> **Wer gesäte Solardaten des Referenzprojekts 1049 in der Testdatenbank ändert, friert im
> selben Schritt die Basis neu ein und begründet den Wechsel hier.**
>
> Betroffen sind der Kollektorsatz des Projekts (`Tab_Solarkollektoren`: Apertur, Modulfläche, `h0`,
> `k1`, `k2`, `Kdir`, `Kdfu`, `Bezugsflaeche`), die Anlagenzeile des Felds (`Kollektormodulanzahl`,
> `Neigung`, `Azimut` und die Felder des Solarkreises: `Arbeitstemperatur_Weg` = `speicher`,
> `Uebertrager_Graedigkeit_K` 5, `Kollektor_Spreizung_K` 10, `Solarkreisverluste_Prozent` und
> `Pumpenleistung_W` leer, dazu `Hilfsenergie_Anteil` leer) und seine Senken in `Z_AnlageSenke` (Rang 1 Heizkreis, Rang 2 Puffer Heizung mit
> Ladeprio 1), der Puffer „Pufferspeicher 3000 l“ (`Gesamtvolumen`, `Vorlauf`/`Ruecklauf`,
> `Schwelle_Aus`, `Schwelle_Aus_Nachrang` leer, `Bereitschaftsverluste`), die Lade-Prioritäten
> von BHKW (2) und Kessel (3) an diesem Puffer und die Kaskade `Tool_1` bis `Tool_4`
> (Solarthermie → BHKW → Heizkessel). Ebenso betroffen ist das Anlegen oder Entfernen eines
> Referenzprojekts mit Solarthermie.
>
> Die Zeilen legt [`Skripte/referenzprojekt_1049_solarthermie.cs`](Skripte/referenzprojekt_1049_solarthermie.cs)
> an (unten); gehalten wird die Solarbilanz von `EPOS.Kern.Tests/SolarWaermeMonateTests`
> (Direkt- und Speicheranteil, Überschuss, Nachrang-Vorgabe am Lauf). Die Vorlage 1018 bleibt
> unverändert; die übrigen Referenzprojekte führen kein deckendes Kollektorfeld.

## Die Einfrierregel „gesäte Kesseldaten“

Neunter Ort derselben Falle. Ein stillstehender Heizkessel trägt seinen Bereitschaftsverlust nur,
wenn er betriebsbereit ist: an einem Heiztag — Tagesmittel der Außentemperatur unter der
Heizgrenze des Projekts, leer 15 °C — oder in den 24 Stunden nach seiner letzten Laufstunde
(`SimulationSPK.IstBetriebsbereit`); die Vorgabe `Kessel_Betriebsbereitschaft` [h/a] deckelt Lauf-
plus Bereitschaftsstunden. Heizgrenze, Deckel und Bereitschaftsleistung verschieben damit
`Kessel[i].Bereitschaftsstunden`, `.BereitschaftKwh`, den Kesselverbrauch, den
Jahresnutzungsgrad und bei Brennstoffkesseln die Kesselemissionen.

Ein laufender Brennstoffkessel rechnet je Stunde mit dem Wirkungsgrad seiner Laststufe
(`Kesselkennlinie.Eta`, Konzept Kesselkennlinie 4.1): zwischen 30 % und 100 % der Nennleistung
linear von η₃₀ nach η₁₀₀. Ein leeres η₃₀ nimmt die Normvorgabe nach Bauart — `Brennwert` = 1
macht den Brennwertkessel (η₁₀₀ + 0,06), eine Beschreibung mit der VDI-Bauart „Standard…“ den
Standardkessel (η₁₀₀ − 0,03), sonst gilt η₁₀₀. Die gepflegte Kennlinie, der Schalter `Brennwert`
und die Bauart verschieben damit Brennstoff, `Kessel[i].Eta30`, `.EtaBetrieb`, `.TeillastKwh`,
den Jahresnutzungsgrad und die Kesselemissionen. Ein Brennwertkessel mit `Kennlinie_Brennwert` = 1
rechnet zusätzlich mit dem Rücklauf der Stunde (`Kesselkennlinie.EtaBrennwert`): Heizkreis der
Anlagenkopplung, sonst Senkenspeicher, sonst das gepflegte Temperaturpaar an Anlage und Kessel, sonst
50 °C — was davon greift, verschiebt `.RuecklaufMittel`, `.Brennwertstunden`, `.BrennwertWaermeKwh`
und `.BrennwertKwh`. Liegt die Wärme einer Laufstunde unter der Mindestleistung, taktet der Kessel
(`Kesselkennlinie.Taktet`, `.StartsImTakt`): so viele Starts, wie Mindestläufe die Wärme braucht, höchstens
⌊60 / Mindestlaufzeit⌋, und je Start der Anfahrverlust als Brennstoff; leere Felder nehmen die Normvorgaben
(Mindestleistung 30 % der Nennleistung beim Gas-Brennwertkessel, sonst 60 %, Anfahrverlust 0,002 h × Nennleistung,
Mindestlaufzeit 10 min). Mindestleistung, Anfahrverlust und Mindestlaufzeit — und über die Normvorgabe Bauart,
Brennstoff und Nennleistung — verschieben damit `Kessel[i].Starts`, `.Taktstunden`, `.AnfahrKwh`, die drei Taktwerte,
den Brennstoff, die Gasspitze, den Jahresnutzungsgrad und die Kesselemissionen.

> **Wer gesäte Kesseldaten eines Referenzprojekts in der Testdatenbank ändert, friert im selben
> Schritt die Basis neu ein und begründet den Wechsel hier.**
>
> Betroffen sind `Tab_Einstellungen.Kessel_Heizgrenze` und `Kessel_Betriebsbereitschaft` eines
> Referenzprojekts (in der Testdatenbank überall leer bzw. 0) und die Bereitschaftsleistung seines
> Kessels (`Tab_Heizkessel.Betriebsbereitschaftverlust` der Projektkopie; 1007, 1046 und 1008
> 0,05 kW, 1017 und 1047 0,057 kW, 1023 und 1050 0,03 kW, die übrigen 0). Ebenso betroffen sind die
> fünf Kennlinienspalten seines Kessels (`Wirkungsgrad_Teillast30`, `Kennlinie_Brennwert`,
> `Mindestleistung`, `Anfahrverlust_kWh`, `Mindestlaufzeit_min`; gepflegt allein in 1050: η₃₀ 1,05,
> Brennwertkennlinie 1, 3,86 kW, 0,1 kWh, Mindestlaufzeit leer), sein Schalter `Brennwert` und die
> Bauart in `Beschreibung`, die über die Normvorgaben von η₃₀ und Mindestleistung entscheiden (jeder Brennstoffkessel der Referenzprojekte ist Brennwertkessel, seit dem
> Schemaschritt 158 auch in der Projektkopie; die Elektrokessel 1017, 1024 und 1047 tragen das Kennzeichen
> nicht), beim Referenzprojekt 1050 dazu, was den Rücklauf seiner Brennwertkennlinie bestimmt (Temperaturpaar
> `Vorlauf`/`Ruecklauf` an Anlagenzeile und Projektkessel, die Senken des Kessels in `Z_AnlageSenke`, die
> Kopplungsstufe `Tab_Einstellungen.Anlagenkopplung`), sowie das Anlegen oder Entfernen des Referenzprojekts
> 1050. Gehalten werden Regel und Zahlen von `EPOS.Kern.Tests/KesselBereitschaftTests`,
> `EPOS.Kern.Tests/KesselKennlinieTests` und `EPOS.Kern.Tests/KesselBrennwertNachzugTests`; die Zeilen von 1050 legt
> [`Skripte/referenzprojekt_1050_kesselkennlinie.cs`](Skripte/referenzprojekt_1050_kesselkennlinie.cs) an.

## Die Einfrierregel „gesäte BHKW-Grenzleistungen“

Zehnter Ort derselben Falle. Ein BHKW-Modul bleibt in einer Stunde aus, wenn der Wärmeraum (wärmegeführt) bzw. der
Reststrom (stromgeführt, ohne Einspeisung) unter seiner unteren Grenzleistung liegt; darüber moduliert es. Die
Untergrenze kommt aus dem Anlagenfeld der BHKW-Zeile (`Tab_Energieanlagen.Grenzleistung`), sobald es gepflegt ist,
sonst aus dem Katalogwert des Moduls (`Tab_BHKW.Grenzleistung` der Projektkopie), sonst aus dem Projektwert
(`Tab_Einstellungen.Leistungsgrenze`); ein Wert über 100 % ist ungültig und wird übersprungen
(`SimulationBHKW.Grenzfaktor`). Jede der drei Stellen verschiebt damit BHKW-Wärme und -Strom, Betriebsstunden,
Brennstoff, die Kesselwärme dahinter und alle Emissionen.

> **Wer gesäte BHKW-Grenzleistungen eines Referenzprojekts in der Testdatenbank ändert, friert im selben Schritt
> die Basis neu ein und begründet den Wechsel hier.**
>
> Betroffen sind das Anlagenfeld der BHKW-Zeilen (gepflegt: 1017 und 1047 je 30 %, 1018 und 1049 je 35 %), die
> Katalogspalte ihrer Projektmodule (gepflegt allein die zwei Module von 1030 mit 15 %) und der Projektwert (30 %,
> in 1024 10 %). Gehalten wird die Rangfolge von `EPOS.Kern.Tests/BhkwLeistungsgrenzeTests`.

## Die Einfrierregel „gesäte Strom-Viertelstundenfelder“

Elfter Ort derselben Falle. Die Einspeisegrenze eines Projekts (`Tab_Einstellungen.Einspeisegrenze_Wert` und
`Einspeisegrenze_Einheit`) regelt PV-Einspeisung ab und verschiebt Einspeisung, Abregelung, die Eigenverbrauchsquote
des Berichts und im Flottenpfad Ladung, Abregelung und Netzbilanz der Flotte; der Standby eines Stromspeichers
(`Tab_Stromspeicher(_STAMM).Standby_Verbrauch`) verschiebt Restbezug und Einspeisung, seine Selbstentladung
(`Selbstentladung_Prozent_Monat`) Ladezustand, Entladung und Restbezug. In der Testdatenbank sind alle drei bei jedem
Referenzprojekt leer.

> **Wer eines dieser Felder an einem Referenzprojekt oder an der Projektkopie eines seiner Stromspeicher setzt, friert
> im selben Schritt die Basis neu ein und begründet den Wechsel hier.** Gehalten werden die Rechenwege von
> `EPOS.Kern.Tests/StromViertelstundenTests` (auf einer Kopie) und `SpeicherEngine.Tests/SpeichersystemTests`.

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

Die Zählungen der Tww-Katalogtabellen der Testdatenbank (Schemastand 150): 6 Tagesgangsätze
(1 fiktiver, 4 abgeleitete, Hotel), 24 Tagesgänge, 9 Nutzungsarten (3 fiktive, 5 abgeleitete, Hotel),
26 Zapfkategorien (je Nutzungsart der Vorgabesatz ihrer Gruppe), 12 Bedarfstage (3 fiktive, die
neun Ecodesign-Zapfprofile XXS bis 4XL) mit 170 Ereignissen, 96 Parameter (54 fiktive, 42 aus dem
Paketteil), 5 DIN-4708-Werte.
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
Basen aufräumt, lässt `2026-10-02_R33_Viertelstunden`, `Kenndaten_Test.sqlite`,
`Importproben`, `Katalogpaket_frei`, `Katalogpaket_Vorlage_A100`, `Skripte` und `LIESMICH.md`
stehen.**

Außer der aktuellen liegt hier keine Basis mehr: Die 24 historischen Referenzbasen (7 731
Dateien, 1 016,7 MB) sind am 11.09.2026 aus dem Arbeitsbaum gefallen (**SYNC‑Q1**: „entfernt
lassen") und seit dem Umschreiben der Git-Geschichte (**AUF‑Q1**, 12.09.2026) auch dort nicht
mehr enthalten; **`2026-09-11_R7_Speicherflotte` ist am 16.09.2026 nach demselben Muster
gefallen, `2026-09-16_R8_Heizkessel_Kaskade` am 18.09.2026,
`2026-09-18_R9_Kesselbrennstoff` am 19.09.2026, `2026-09-19_R10_BhkwWirkungsgrad` am
22.09.2026, `2026-09-22_R11_Bestandsbefunde` und `2026-09-23_R12_Gebaeudemodell` am 23.09.2026,
`2026-09-23_R13_Kuehlung` am 24.09.2026, `2026-09-24_R14_Kaelteerzeuger`, `2026-09-25_R15_Anlagenkopplung`, `2026-09-25_R16_Anlagenprio`, `2026-09-25_R17_Datenpflege`, `2026-09-25_R18_PvAusweis` am 25.09.2026, `2026-09-25_R19_BhkwNetzbezug`, `2026-09-26_R20_Zapfprofil`, `2026-09-26_R21_BhkwDeckung` und
`2026-09-26_R22_Solarthermie` am 26.09.2026, `2026-09-26_R23_KesselBereitschaft`, `2026-09-27_R24_Heizgrenze` und `2026-09-29_R25_Plattformrand` am 29.09.2026,
`2026-09-29_R26_Kesselrest`, `2026-09-30_R27_Kesselteillast`, `2026-09-30_R28_Kesselbrennwert` und
`2026-09-30_R29_Kesseltakten` am 30.09.2026, `2026-09-30_R30_Stromverbraucher`,
`2026-10-02_R31_Rechenwegbefunde` und `2026-10-02_R32_Solarthermie` am 02.10.2026**
(50 Basen, alle Protokolle gesichert). Kein Test, kein Gate, keine CI liest
eine entfernte Basis. **Die Messdaten sind endgültig weg** (rund 8 000 CSV-Dateien) — eine
alte Zahl steht nur noch im Protokoll.
Erhalten sind die **Protokolle** aller 50 Basen samt der Tabelle Basis → Datum → Zweck →
Protokoll unter
[`Dokumentation/ueberholt/Referenzbasen/`](../Dokumentation/ueberholt/Referenzbasen/LIESMICH.md);
**welche Basis wann von welcher abgelöst wurde und warum**, steht ebendort — bis zum 12.09.2026
in der ausgelagerten
[Basenhistorie](../Dokumentation/ueberholt/Referenzbasen/LIESMICH_Basenhistorie_bis_2026-09-12.md),
danach im Wegweiser desselben Ordners.

## Aktuelle Basis

**`2026-10-02_R33_Viertelstunden/`** — **sechzehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039,
1040, 1041, 1042, 1045, 1046, 1047, 1049, 1050), **487 CSV**, **3 082 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` **auf Linux** (x64, Kultur de-DE, eingefroren am 02.10.2026) gegen
`Kenndaten_Test.sqlite` (Schemastand **176**, 81 494 016 Byte, LFS-SHA-256
`4db1fadcfce4ba499bf99abbb4c83d9c19b3cbad2ac6b71a456cc2e24a5f968b`; eingefroren auf der Fassung `2b0dc246…`, Nachtrag „Testdatenbank“ unten). Die
Schemaschritte 166 (Netzverluste je Kanal, Zirkulation, Betriebskalender) und 167 (Teillastfelder von Wärmepumpe
und BHKW) legen nur leere Felder an und wirken nicht auf die Basis; Schemaschritt 169 (Pufferspeicher-Auslegung,
Nachtrag unten) legt zwei Tabellen samt Saat an, die kein Rechenweg liest; Schemaschritt 170 (Empfehlungsspannen der
Hilfsenergie von BHKW und Heizkessel in den Auslieferungsvorlagen auf Weg B, Nachtrag „Schemaschritt 170“ unten)
ändert nur einen Hinweis am Satzfeld der Kostenvorlagen, den der Referenzlauf nicht liest; Schemaschritt 171
(Pufferoptionen und thermische Desinfektion) legt nur leere Felder an, ebenso Schemaschritt 174 (Aufschlag und
manuelle Aufheizzeit der Aufheizoptimierung, Nachtrag unten). Gegen diese Basis hält
`.github/workflows/kern.yml` (1030, 1007, 1017, 1045, 1046, 1047, 1049) jeden Push und rechnet dieselben Projekte
ein zweites Mal gestört (Abschnitt „Der Plattformnachweis“), `ios.yml` den iZ6-Vergleich für 1030,
`EPOS.Kern.Tests/GebaeudeRueckwegTests` den Tagesbilanz-Weg an Projekt 1040,
`EPOS.Kern.Tests/ZapfprofilReferenzprojektWacheTests` die Generator-Bilanz von Projekt 1045,
`EPOS.Kern.Tests/SolarWaermeMonateTests` die Solarbilanz von Projekt 1049 samt der Anker (genutzte Solarwärme,
Überschuss, mittlere Arbeitstemperatur des Felds), `EPOS.Kern.Tests/StromViertelstundenTests` die PV-Bilanz der
Projekte 1045 und 1046 (Erzeugung, Einspeisung, Restbezug),
`EPOS.Kern.Tests/PlattformrandTests` die Betriebsstunden der Wärmepumpe am Quellspeicher von Projekt 1042 und
die Kesselstunden von Projekt 1024, `EPOS.Kern.Tests/KesselKennlinieTests` die Teillastkennlinie an 1023 und 1007,
das Takten mit den Normvorgaben an 1023 sowie Brennwertkennlinie und Takten des Referenzprojekts 1050,
`EPOS.Kern.Tests/KesselBrennwertNachzugTests` das Brennwertkennzeichen der Projektkessel,
`EPOS.Kern.Tests/StromverbraucherZuordnungTests` die Stromverbraucher-Zuordnung über die ID an 1017, 1043 und
1046 und `EPOS.Kern.Tests/BhkwLeistungsgrenzeTests` die Rangfolge der BHKW-Untergrenze (Anlagenfeld, Katalog,
Projekt) in allen drei Betriebsarten. 1050 steht nicht in der CI-Auswahl; `Werkzeuge/Gate/gate_linux.sh` rechnet
alle sechzehn. Sie ist die **einzige** Basis im Arbeitsbaum.

> **Anlass: Welle M5 „Strom in Viertelstunden“ der
> [Entscheidungsvorlage Modellgrenzen](../Dokumentation/aktuell/Entscheidungsvorlage_Modellgrenzen_Rechenwege.md)**
> (SB1 (a), PV3, SP1; Wellenplan vom Anwender freigegeben). Gebaut sind vier Teile, die Zahlen der Basis verschiebt
> allein der erste:
>
> - **SB1 (a) PV-Bilanz auf 35 040 Viertelstunden:** Der Stundenertrag der Photovoltaik wird energieerhaltend nach
>   dem Sonnenstand auf die vier Viertel verteilt (`P_q = P_h · 4 · cos θ_z,q / Σ cos θ_z`, Sonnenstand in der
>   Mitte jeder Viertelstunde auf der UTC-Herkunft der Klimazeile; ohne Sonne gleichmäßig). Direktverbrauch
>   `min(P_q, Last_q)`, Überschuss und Reststrom entstehen je Viertel aus **einer** Auflösung; die Stundenreihen
>   (`pv_produktion.csv`, `pv_ueberschuss.csv`, `pv_reststrom.csv`) sind die Mittel ihrer vier Viertel. Stromspeicher
>   und Flotte rechnen mit der glatten Reihe.
> - **Schemaschritt 168** (`StromViertelstundenSchema`): an `Tab_Einstellungen` die nullbaren Felder
>   `Einspeisegrenze_Wert` und `Einspeisegrenze_Einheit`, an `Tab_Stromspeicher(_STAMM)`
>   `Selbstentladung_Prozent_Monat`. Kein Referenzprojekt setzt sie.
> - **PV3 Einspeisegrenze:** Abregelung über der Grenze nach der Speicherladung; die Flotte liest sie als weiche
>   Grenze. Ohne Grenze kein Schlüssel `Photovoltaik.AbregelungMwh`: byte-gleich.
> - **SP1 Standby und Selbstentladung:** Standby aus `Standby_Verbrauch` (in der Testdatenbank überall leer),
>   Selbstentladung leer. Neu ist allein der Skalar `Stromspeicher.EigenverbrauchSystemMwh` von **1046** (0,438):
>   Im Flottenpfad ist der Eigenverbrauch des Speichersystems der Hilfsverbrauch der Einheiten, den die Flotte
>   von 1046 schon rechnete.
>
> **A/B gegen R32** (beide auf Linux): **16/16 PASS**, 459/487 CSV byte-gleich. Abgewichen sind genau die vier
> Projekte mit PV-Ertrag — 1007 (7 Dateien), 1040 (5), 1045 (5), 1046 (11): `aggregate.csv`, `pv_produktion.csv`,
> `pv_ueberschuss.csv`, `pv_reststrom.csv`, `reststrom_viertelstunde.csv`, mit Speicher dazu
> `pv_speicherfuellstand.csv` und `ssp_gespeichert_viertelstunde.csv`, bei 1046 die vier Flottenreihen.
> `pv_produktion_theoretisch.csv` und `pv_strombedarf.csv` bleiben byte-gleich. 1041 und 1042 führen eine
> PV-Anlage ohne Ertrag und bleiben wie alle Projekte ohne PV byte-gleich.
>
> | Größe (kWh/a) | 1007 (Speicher) | 1040 | 1045 | 1046 (Flotte) |
> |---|---|---|---|---|
> | Erzeugung der Module | 6 014,3 → 6 014,3 | 6 713,5 → 6 713,5 | 3 545,5 → 3 545,5 | 6 014,3 → 6 014,3 |
> | Direktverbrauch | 5 081,0 → 5 076,2 | 4 440,7 → 4 436,6 | 2 763,8 → 2 762,1 | 5 081,0 → 5 076,2 |
> | Überschuss vor Speicher | 933,3 → 938,1 | 2 272,7 → 2 276,9 | 781,6 → 783,4 | 933,3 → 938,1 |
> | Einspeisung | 330 → 330 (gerundet) | 2 272,7 → 2 276,9 | 781,6 → 783,4 | 894,9 → 899,9 |
> | Eigenverbrauch (Erzeugung − Einspeisung) | 5 684 → 5 684 (gerundet) | 4 440,7 → 4 436,6 | 2 763,8 → 2 762,1 | 5 119,4 → 5 114,4 |
> | Restbezug | 62 784,0 → 62 786,1 | 22 986,3 → 22 990,5 | 28 745,2 → 28 747,0 | 63 896,1 → 63 901,1 |
> | Autarkie % (Bedarf − Restbezug)/Bedarf | 8,237 → 8,234 | 16,191 → 16,176 | 8,772 → 8,766 | 6,611 → 6,604 |
>
> **Plausibel:** Die Lastgänge der Referenzprojekte sind Stundenprofile, gleichmäßig auf die Viertel gespreizt; die
> PV-Reihe folgt jetzt dem Sonnenstand innerhalb der Stunde. In Viertelstunden, in denen die PV über dem Mittel
> liegt, entsteht Überschuss, den das Stundenmittel verdeckt hatte — der Direktverbrauch sinkt um 0,05 bis 0,1 %,
> Einspeisung und Restbezug steigen um denselben Betrag. Die Erzeugung bleibt Stunde für Stunde gleich
> (Energieerhaltung). Größer wird der Unterschied bei Lastgängen mit echten Viertelstundenspitzen
> (Gewerbelast, Ganglinien mit 35 040 Werten).
>
> **Kein Fehlschlag, keine Ablehnung:** 16/16 Projekte gerechnet. **Determinismus:** Ein zweiter Lauf ist mit dem
> Einfrierlauf 487/487 CSV byte-gleich. **Plattformnachweis:** gestört gegen die Basis 16/16 PASS, 480/487 CSV
> byte-gleich. Die sieben CI-Projekte gegen die Basis: GESAMT PASS.
>
> ```bash
> dotnet build EPOS.Referenzlauf/EPOS.Referenzlauf.csproj -c Release
> dotnet run --project EPOS.Referenzlauf -c Release --no-build -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046,1047,1049,1050 \
>   --ziel Referenzlaeufe/2026-10-02_R33_Viertelstunden
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis; die Regeln im
> [Konzept Simulationsablauf](../Dokumentation/aktuell/Konzept_Simulationsablauf_EPOS-Plan.md), Abschnitt 19.

> **Testdatenbank — Schemaschritt 168.** Eingefroren ist R33 auf der Fassung `2b0dc246…` (aus `7debfd8a…`, Schemastand
> 165, gehoben um die Spalten der Einspeisegrenze und der Selbstentladung). Die gültige Fassung kommt aus
> `58d9ba47…` (Schemastand 166, Netzverluste je Kanal, Zirkulation, Betriebskalender) und ist mit
> `Werkzeuge/Testdatenbankschema` auf **168** gezogen: Schritt 167 legt die leeren Teillastspalten an `Tab_WP(_STAMM)`
> und `Tab_BHKW(_STAMM)` an, Schritt 168 zwei leere Spalten an `Tab_Einstellungen`, je eine leere
> Spalte an `Tab_Stromspeicher_STAMM` und `Tab_Stromspeicher`; keine Datenänderung, kein Stempel gesetzt.
> `integrity_check` ok, `foreign_key_check` leer. Neue Fassung **81 195 008 Byte, LFS-SHA-256
> `6e5d24aa5da7dafdad6ed1c1b4dabb3eeb13744f100cee8fc201b7f16b9aceae`**. Die sechzehn Projekte rechnen auf ihr gegen R33
> GESAMT PASS mit 487/487 CSV byte-gleich; keine Einfrierregel ist berührt.

> **Nachtrag — Schemaschritt 169 (Pufferspeicher-Auslegung), Basis unverändert.**
> `PufferAuslegungSchema` (Nummer `StromViertelstundenSchema.SCHRITT + 1`): die Tabelle `Tab_PufferAuslegung` (STRICT,
> leer; `ID_Projekt` mit `ON DELETE CASCADE`, `ID_Pufferspeicher` mit `ON DELETE SET NULL`) und die Vorgabetabelle
> `Tab_PufferAuslegungParameter_STAMM` (STRICT) mit 148 gesäten Vorgabewerten `Pufferauslegung.*` aus
> `PufferAuslegungVorgaben`. Die Testdatenbank ist aus der Fassung `6e5d24aa…` (168) mit `Werkzeuge/Testdatenbankschema`
> auf **169** gezogen. Neue Fassung **81 240 064 Byte, LFS-SHA-256
> `5fdc093e38eb755c4d3fc598c57022180409763cdc7a05e6a069d20dd3fb6d29`**. **Die Basis bleibt:** Kein Rechenweg liest die
> neuen Tabellen; die Auslegung rechnet und schreibt nur auf Zuruf. Keine Einfrierregel ist berührt.

> **Nachtrag — Schemaschritt 170 (Katalogempfehlung der Hilfsenergie auf Weg B), Basis unverändert.**
> `HilfsenergieEmpfehlungNachzug` (Nummer `PufferAuslegungSchema.SCHRITT + 1`), reines DML: In den
> Auslieferungsvorlagen (`Tab_KostenVorlage.ReadOnly = 1`, Kategorie Betrieb) trägt die Pflichtzeile
> „Hilfsenergiekosten“ des BHKW (`Tab_KostenVorlagePosition.ID` 62) die Empfehlung 0,5–1,5 % statt 2–4 %, die
> Pflichtzeile „Hilfsenergiekosten (Strom)“ des Heizkessels (ID 68) 1–2 % statt 4–8 % — beide rechnen als Anteil des
> Endenergiebedarfs (Weg B), die alten Spannen galten für Weg A. Projektzeilen (`Tab_ProjektWerte`) führen keine
> Empfehlung und bleiben unberührt. Die Testdatenbank ist aus der Fassung `5fdc093e…` (169) mit
> `Werkzeuge/Testdatenbankschema` auf **170** gezogen (`--trocken` danach 0 offen, `integrity_check` ok,
> `foreign_key_check` leer). Neue Fassung **81 240 064 Byte, LFS-SHA-256
> `bd624ace4a02fb3f146c688018594af03020ed67023cf351afed14d09b4b70c2`**. **Die Basis R33 bleibt:** Der Referenzlauf liest
> keine Kostenvorlage. Keine Einfrierregel ist berührt.

> **Nachtrag — Schemaschritt 171 (Pufferoptionen und thermische Desinfektion), Basis unverändert.**
> `PufferOptionenSchema` (Nummer `HilfsenergieEmpfehlungNachzug.SCHRITT + 1`): an `Tab_Pufferspeicher` (Projektkopie) die
> nullbaren Spalten `Bereitschaft_Weg` ('tag'/'temperatur'), `Aufstellraum_Temperatur_C`, `Schicht_Anteile`,
> `Frischwassermodul`, `FWM_Graedigkeit_K`; an `Tab_Einstellungen` `Desinfektion_Aktiv`, `_Intervall_Tage`, `_Stunde`,
> `_Zieltemperatur_C`, `_Volumen_l`, reines DDL. Die Testdatenbank ist aus der Fassung `bd624ace…` (170) mit
> `Werkzeuge/Testdatenbankschema` auf **171** gezogen, alle neuen Zellen leer (`integrity_check` ok,
> `foreign_key_check` leer). Neue Fassung **81 235 968 Byte, LFS-SHA-256
> `4055798699076f008b90417d47efe726a71d203cd8c4f2d70b2c67580890c8a6`**. **Die Basis bleibt:** Die Projekte der CI-Auswahl
> rechnen auf ihr gegen R33 GESAMT PASS, alle CSV byte-gleich. Keine Einfrierregel ist berührt.

> **Nachtrag — Schemaschritt 172 (Katalogfassung, Katalogabgleich, Erdreichprüfung im Ergebnis), Basis unverändert.**
> `KatalogfassungSchema` (Nummer `PufferOptionenSchema.SCHRITT + 1`): an den Stufe-1-Katalogen (`Tab_WP_STAMM` samt
> `Tab_Kenndaten_STAMM`/`Tab_Kenndaten_Kuehlung_STAMM`, `Tab_Heizkessel_STAMM`, `Tab_BHKW_STAMM`, `Tab_PV_STAMM`,
> `Tab_Brauchwasser_STAMM`, `Tab_Brauchwassertyp_STAMM`, `Tab_Prozesswaerme_STAMM`, `Tab_Prozesstyp_STAMM`) die Spalten
> `Katalog_Schluessel` (Teilindex eindeutig), `Katalog_Pruefsumme`, `Katalog_Ausgelaufen`; `Tab_Applikation.Katalogfassung`;
> die STRICT-Tabellen `Tab_Katalogabgleich` und `Tab_ErgebnisErdreich`. Die Saat setzt nur Schlüssel und Prüfsumme der
> Sätze mit `ReadOnly = 1` (keine Fachwerte); Projektkopien unberührt. Die Testdatenbank ist aus der Fassung `40557986…`
> (171) mit `Werkzeuge/Testdatenbankschema` auf **172** gezogen (36 Spalten, 2 Tabellen; `integrity_check` ok). Neue Fassung
> **81 293 312 Byte, LFS-SHA-256 `8edc80c49b95d9841d044580dc61a011915f3b330c587de66a41c86c9dab2bfd`**. **Die Basis bleibt:** Die sechzehn
> Projekte rechnen auf einer so gehobenen Kopie gegen R33 GESAMT PASS mit 487/487 CSV byte-gleich; der Katalogabgleich selbst
> läuft auf der Testdatenbank nie (Einfrierregeln nennen Katalogzeilen der Referenzprojekte). Keine Einfrierregel ist berührt.

> **Nachtrag — Schemaschritt 173 (Katalogfassung Stufe 2), Basis unverändert.** `KatalogfassungStufe2Schema` (Nummer
> `KatalogfassungSchema.SCHRITT + 1`): die drei Katalogspalten `Katalog_Schluessel`, `Katalog_Pruefsumme`, `Katalog_Ausgelaufen`
> an 16 weiteren Kopftabellen (Baustoffe, Bauteilaufbauten, Brennstoffe, Tagesverteilungen, Gebäude, Konditionierungsvorlagen,
> Pufferspeicher, Pufferauslegungs-Vorgaben, Solarkollektoren, Solar-, Strom- und Wärmebedarfsganglinien, Stromspeicher,
> Stromverbraucher und -typen, Wechselrichter); Klima und Zapfprofilkatalog benannt ausgenommen. Die Saat setzt nur Schlüssel
> und Prüfsumme der Sätze mit `ReadOnly = 1` (BST 132, GEB 6, KV 14, PAP 148, WBGL 3, WR 1), keine Fachwerte; Projektkopien
> unberührt. Die Testdatenbank ist aus der Fassung `8edc80c4…` (172) mit `Werkzeuge/Testdatenbankschema` auf **173** gezogen
> (`integrity_check` ok). Neue Fassung **81 412 096 Byte, LFS-SHA-256 `d65e7ef5c67f1ac8302bf17faa350da792535655c67118ddd39679549b249f2b`**.
> **Die Basis bleibt:** Die sechzehn Projekte rechnen auf einer so gehobenen Kopie gegen R33 GESAMT PASS mit 487/487 CSV
> byte-gleich. Keine Einfrierregel ist berührt.

> **Nachtrag — Schemaschritt 174 (KP-S4: Aufschlag und manuelle Aufheizzeit der Aufheizoptimierung), Basis unverändert.**
> `AufheizManuellSchema` (Nummer `KatalogfassungStufe2Schema.SCHRITT + 1`): an `Tab_Einstellungen` `Aufheiz_Aufschlag_H` (0 … 24) und
> `Aufheiz_Aufschlag_Prozent` (0 … 100), an `Tab_Gebaeude` (nicht am Katalog) `Aufheizzeit_Manuell_H` (1 … 47) samt dem
> achten Neubau der Sicht `Abfrage_Projektgebaeude` (103 Spalten), an `Tab_ErgebnisGebaeude` `Aufheiz_Art` (TAEGLICH, FEST,
> MANUELL), `Auslegungsheizlast_Kw` (> 0) und `Aufheizzuschlag_Kw` (≥ 0), an `Tab_ErgebnisZone` `Aufheiz_Art`; alle nullbar
> mit Prüfklausel. Der Zustand `GEKOPPELT` der Zone kommt per kleinem Neubau allein von `Tab_ErgebnisZone` (STRICT, beide
> Fremdschlüssel und Indizes erhalten, `foreign_key_check` leer); `Tab_ErgebnisGebaeude` wird nicht neu gebaut. Kein DML an
> Bestandsdaten. Die Testdatenbank ist aus der Fassung `d65e7ef5…` (173) mit `Werkzeuge/Testdatenbankschema` auf **174**
> gezogen (7 Spalten, ein Neubau ohne Zeilen, Sicht; `integrity_check` ok). Neue Fassung **81 412 096 Byte, LFS-SHA-256
> `e85c3bdbd33d618886712fcb0aacb124b44c1a6c123222a33921813d3edf0673`**. **Die Basis bleibt:** Aufschlag und manuelle
> Aufheizzeit stehen überall leer, `Aufheizoptimierung` = 0 hält jedes Projekt auf „aus“, und der Referenzlauf liest
> weder `Tab_ErgebnisGebaeude` noch `Tab_ErgebnisZone`; die sechzehn Projekte rechnen auf ihr gegen R33 GESAMT PASS mit
> 487/487 CSV byte-gleich. Keine Einfrierregel ist berührt.

> **Nachtrag — Schemaschritt 175 (Projektkopien der Brennstoffe und Pufferauslegungs-Vorgaben), Basis unverändert.**
> `ProjektkopienKatalogeSchema` (Nummer `AufheizManuellSchema.SCHRITT + 1`): `Tab_Brennstoff` (STRICT; je Projekt und
> Brennstoffart die 14 Fachspalten des Stamms samt `Katalogfassung_Herkunft`, `ID_Projekt` mit `ON DELETE CASCADE`) und
> `Tab_PufferAuslegungParameter` (STRICT; je Projekt und `Schluessel`). Die Saat kopiert wertgleich: je Projekt jeden
> Stamm-Brennstoff (29 Projekte × 25 Arten = 725 Zeilen), je Projekt mit Pufferauslegung jede Vorgabe (hier 0). Der Kern
> liest die Brennstoffwerte eines Projekts seither über `ProjektBrennstoffe.Sicht` aus der Kopie, der Katalogabgleich fasst
> die Kopien nie an. Konditionierungsvorlagen brauchen keine Kopie, ihr Inhalt liegt schon am Gebäude. Die Testdatenbank ist
> aus der Fassung `e85c3bdb…` (174) mit `Werkzeuge/Testdatenbankschema` auf **175** gezogen (2 Tabellen, 725 Zeilen;
> `integrity_check` ok). Neue Fassung **81 494 016 Byte, LFS-SHA-256
> `551a288deae7562b316a798f0c7b669e3b453ce8d22ad01fe6cebbd733c73951`**. **Die Basis bleibt:** Die Kopien tragen dieselben
> Werte wie der Stamm; die sechzehn Projekte rechnen auf einer so gehobenen Kopie gegen R33 GESAMT PASS mit 487/487 CSV
> byte-gleich. Berührt ist die Einfrierregel „gesäte Bedarfsdaten“ nur im Wortlaut (`Tab_Brennstoff` der Referenzprojekte
> gehört seither dazu, siehe `CLAUDE.md`), nicht im Wert.

> **Nachtrag — Schemaschritt 176 (Konditionierungsnutzung an der Kalenderkopie), Basis unverändert.**
> `KonditionierungNutzungSchema` (Nummer `ProjektkopienKatalogeSchema.SCHRITT + 1`): Spalte `Nutzung` (nullbar, Prüfklausel
> auf die vier Nutzungen) an `Tab_Konditionierungskalender`. „Vorlage übernehmen“ und der Paketimport setzen sie aus der
> Vorlage, die Saat füllt bestehende Kalender einmalig aus der Herkunft in `Bemerkung`; die Pufferauslegung liest ihre
> Vorbelegung seither aus der Kopie statt aus dem Stamm. Die Testdatenbank ist aus der Fassung `551a288d…` (175) mit
> `Werkzeuge/Testdatenbankschema` auf **176** gezogen (eine Spalte, 0 Kalender gesät: keiner der 10 Kalender trägt eine
> Bemerkung; `integrity_check` ok). Neue Fassung **81 494 016 Byte, LFS-SHA-256
> `4db1fadcfce4ba499bf99abbb4c83d9c19b3cbad2ac6b71a456cc2e24a5f968b`**. **Die Basis bleibt:** Kein Rechenweg liest die Spalte; die sechzehn Projekte
> rechnen auf einer so gehobenen Kopie gegen R33 GESAMT PASS mit 487/487 CSV byte-gleich. Keine Einfrierregel ist berührt.

### Die Vorgängerbasis R32 `2026-10-02_R32_Solarthermie`

Sechzehn Projekte, 487 CSV, 3 081 Skalare, auf Linux eingefroren gegen die Testdatenbank `486d5b0c…`, getragen
bis zur Fassung `7debfd8a…` (Schemastand 165); mit R33 aus dem Arbeitsbaum gefallen, Protokoll und Anlass
(Welle M2 Solarthermie, Arbeitstemperatur des Kollektorfelds von 1049 aus dem Speicher) unter
[`Dokumentation/ueberholt/Referenzbasen/`](../Dokumentation/ueberholt/Referenzbasen/LIESMICH.md). Zwischen R32 und
R33 hat die Testdatenbank die Schemaschritte 166 bis 168 bekommen (leere Felder); der Wechsel ist allein der Rechenweg der
PV-Bilanz (SB1 a).

## Was hier liegt

| Pfad | Inhalt |
|---|---|
| `<yyyy-MM-dd>_<Marke>/` | Ein eingefrorener Lauf: je Projekt ein Unterordner `Projekt_<ID>/`, dazu `lauf_protokoll.md` bzw. `protokoll.txt` |
| `<...>/Projekt_<ID>/aggregate.csv` | Alle Skalare des Laufs: `Tab_Ergebnis*`-Zeilen, Restgrößen aus `SimulationControl`, Jahressumme jedes Vektors |
| `<...>/Projekt_<ID>/*.csv` | Die Ganglinien: 8760 Stundenwerte bzw. 35040 Viertelstundenwerte, `Index;Wert` |
| `Arbeitskopie/` | Die Kopie der Datenbank, auf der gerechnet wird. Wird bei jedem `lauf` neu angelegt. Nicht im Git (`Kenndaten.accdb` ist in `.gitignore`) |
| `Katalogpaket_frei/` | Der freie Paketteil des Zapfprofilgenerators (CSV im Paketformat N2): Quelle der freien Zeilen der Auslieferungsvorlage und der Testdatenbank |
| `Kenndaten_Test.sqlite` | Die reduzierte Testdatenbank, gegen die der plattformfreie `EPOS.Referenzlauf` und der SQL-Dialektprüfer laufen. **Versioniert** — eine Änderung daran gehört in einen eigenen Commit |
| `Skripte/` | Was an dieser Testdatenbank gemacht wurde, als Skript und nicht als Erzählung: `pruefprojekt_1045_ost_west.py` (W6‑O‑7), `pruefprojekt_1046_speicherflotte.py` (SP‑O‑8), `anlagenkopplung_1047_referenzprojekt.py` (Referenzprojekt der Anlagenkopplung, Kopie von 1017), `pruefprojekt_1048_pv_preise.cs` (dotnet-Dateiskript: Prüfprojekt 1048 „PV mit Preisen“, ohne Referenzrolle), `referenzprojekt_1049_solarthermie.cs` (dotnet-Dateiskript: Referenzprojekt 1049 „Solarthermie“, Kopie von 1018; die Einfrierregel „gesäte Solardaten“ oben), `referenzprojekt_1050_kesselkennlinie.cs` (dotnet-Dateiskript: Referenzprojekt 1050 „Kesselkennlinie“, Kopie von 1023; die Einfrierregel „gesäte Kesseldaten“ oben), `gebaeude_10576_bauweise.py` (Stufe GB, Befund D), `gebaeude_10612_233_bauweise.py` (dieselbe Korrektur an 1009 und Katalogsatz 233, Basis unverändert), `tww_testkatalog_fiktiv.py` (Testkatalog des Zapfprofilgenerators samt abgeleiteten VDI-Werten und den Zeilen des freien Paketteils, Schemastand 115) `normzahlen_abgeleitet_bauen.py` (nur lokal: abgeleitete VDI-6002-Werte nach `tww_katalogwerte_abgeleitet.json` und abgeleitete VDI-4655-Werte nach `vdi4655_abgeleitet.json`, ZU19; `--norm vdi6002|vdi4655|beide`) und `referenzprojekt_zapfprofil.py` (stellt Projekt 1045 auf den Zapfprofilgenerator um, ZU7; die Einfrierregel „gesäte Zapfprofil-Eingaben" oben) |

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

### Das Referenzprojekt 1049 „Solarthermie“

Projekt **1049 „Referenzprojekt Solarthermie“** ist das einzige Referenzprojekt mit einem deckenden
Kollektorfeld: Kopie von 1018 auf dem Kopierweg des Programms, Kaskade Solarthermie → BHKW →
Heizkessel, 35 Flachkollektoren 35° Süd mit Senke Heizkreis (direkt) und Puffer Heizung (Ladeprio 1),
der gemeinsame Puffer auf 3.000 l, 60/35 °C, `Schwelle_Aus` 95 %, `Schwelle_Aus_Nachrang` leer;
BHKW (Ladeprio 2) und Kessel (3) laden nachrangig bis zur Vorgabe 30 %. Das Feld bildet seine
Arbeitstemperatur aus der untersten Zone dieses Puffers (Grädigkeit 5 K, Spreizung 10 K; Basis R32). Es steht in der Basis und in
der CI-Liste, für seine Zeilen gilt die Einfrierregel „gesäte Solardaten“ oben.

```bash
dotnet run Referenzlaeufe/Skripte/referenzprojekt_1049_solarthermie.cs -- Referenzlaeufe/Kenndaten_Test.sqlite [--trocken]
```

Das Skript ist wiederholbar wie das von 1048: Steht 1049 mit allen Zielzellen, ändert es nichts
(Rückgabe 0); weicht etwas ab, bricht es ab, ohne die Datei zu ändern (Rückgabe 2). Es schreibt in eine
Arbeitsdatei, prüft die Zielzellen, die Unversehrtheit von 1018, `integrity_check` und
`foreign_key_check` und ersetzt erst dann die Datenbank. Es setzt voraus, dass die Kopie auf 1049 fällt
(1048 ist die höchste Projekt-ID).

### Das Referenzprojekt 1050 „Kesselkennlinie“

Projekt **1050 „Referenzprojekt Kesselkennlinie“** ist das einzige Referenzprojekt, dessen Heizkessel eine
gepflegte Kennlinie trägt: Kopie von 1023 auf dem Kopierweg des Programms (zwei Wärmepumpen auf einen Puffer,
danach ein Gas-Brennwertkessel 19,3 kW am Heizkreis), am Projektkessel die neutralen Werte aus Konzept
Kesselkennlinie 4.3 — η₁₀₀ (`Wirkungsgrad_Gas`) 0,97, η₃₀ 1,05, `Kennlinie_Brennwert` 1, `Mindestleistung`
3,86 kW (20 % der Nennleistung), `Anfahrverlust_kWh` 0,1, `Mindestlaufzeit_min` leer (Normvorgabe). Es steht in
der Basis, **nicht** in der CI-Liste (Entscheid F4); für seine Zeilen gilt die Einfrierregel „gesäte
Kesseldaten“ oben. Es rechnet die Teillastkurve aus dem gepflegten η₃₀ und als einziges Referenzprojekt die
Brennwertkennlinie: ohne Anlagenkopplung, Senkenspeicher und gepflegtes Paar mit dem Rückfall-Rücklauf 50 °C, also
in jeder Laufstunde mit η = 0,97 + 0,08 · 7/27 (η₃₀,tr = 1,05 − 0,08 = η₁₀₀, die trockene Kurve ist flach). Als
einziges Referenzprojekt taktet es mit gepflegten Werten: Mindestleistung 3,86 kW, Anfahrverlust 0,1 kWh je Start,
Mindestlaufzeit 10 min als Normvorgabe (503 Taktstunden, 2 035 Starts, 203,5 kWh/a Anfahrverlust in R31).

```bash
dotnet run Referenzlaeufe/Skripte/referenzprojekt_1050_kesselkennlinie.cs -- Referenzlaeufe/Kenndaten_Test.sqlite [--trocken]
```

Das Skript ist wiederholbar wie das von 1049: Steht 1050 mit allen Zielzellen, ändert es nichts (Rückgabe 0);
weicht etwas ab, bricht es ab, ohne die Datei zu ändern (Rückgabe 2). Es schreibt in eine Arbeitsdatei, prüft die
Zielzellen, die Unversehrtheit von 1023, `integrity_check` und `foreign_key_check` und ersetzt erst dann die
Datenbank. Es setzt voraus, dass die Kopie auf 1050 fällt (1049 ist die höchste Projekt-ID); nach einer Neufassung
der Testdatenbank ohne 1050 wird es auf der neuen Fassung erneut gezogen.

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

## Der Plattformnachweis: `lauf … --stoerung ulp`

Nur der plattformfreie `EPOS.Referenzlauf` kennt den Schalter. Er verschiebt die Ergebnisse von
`Math.Exp`, `Sin`, `Cos`, `Asin` und `Acos` an der Naht `Plattformrundung` (Gebäudematrix, Sonnenstand,
Erdreich, Kollektor, Tagesbilanz) deterministisch um ±1 ulp — etwa jedes sechzehnte Ergebnis, nach dem
Bitmuster des Arguments, so wie eine andere C-Bibliothek rundet. Der gestörte Lauf muss mit dem
ungestörten **innerhalb der Toleranz** übereinstimmen: `kern.yml` rechnet ihn für die sieben CI-Projekte,
`Werkzeuge/Gate/gate_linux.sh` (Schritt 6) für alle sechzehn. Er wird **nie eingefroren**; sein
`protokoll.txt` trägt die Zeile `Stoerung:`.

```bash
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
  --projekte 1030,1007,1017,1045,1046,1047,1049 --ziel <ordner>_stoerung --stoerung ulp
dotnet run --project EPOS.Referenzlauf -c Release --no-build -- vergleich <ordner> <ordner>_stoerung
```

Stand mit R31 (Zahlenrand an Phase G, Quellspeicher und Kessellauf; Teil- und Brennwertkennlinie des Kessels
als lineare Arithmetik, Brennwertbetrieb und Takten am Zahlenrand, die Jahressumme der Stromprofile als
Skalierung, die BHKW-Untergrenze als Vergleich am Zahlenrand): alle sechzehn Projekte GESAMT PASS, 480/487 CSV byte-gleich, die übrigen sieben nur mit
Rechenresten von höchstens 10⁻⁸ (Heizstab 1007/1046, Kessel 1024, Quellpuffer 1042, BHKW-Restwärme 1018,
Wärmepumpe 1045). **Gegenprobe:** Mit dem blanken
Vergleich an diesen drei Stellen fallen 1008, 1018, 1023, 1024, 1039 und 1042 durch — der Nachweis sieht
genau die Kanten, die der Rand geschlossen hat. Ohne den Schalter rechnet die Naht bitgleich `Math.*`
(`EPOS.Kern.Tests/PlattformrundungTests`); ein Lauf ohne Schalter ist mit R31 487/487 CSV byte-gleich.

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
