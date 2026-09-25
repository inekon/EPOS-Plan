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
> **Rechenwirkung hat allein Projekt 1017:** Projektschalter ein, Gebäude 10599 mit Haken,
> Kühlsollwert 24 °C und Kühlleistungsgrenze 15 kW — vier Zellen aus
> [`Skripte/kuehlung_1017_referenzprojekt.py`](Skripte/kuehlung_1017_referenzprojekt.py); dazu
> der Kälteerzeuger — die Wärmepumpe (Anlage 10211, Projektgerät 1017033) auf Kaskadenplatz 3, im
> Kühlbetrieb mit Kühl-Vorlauf 18 °C und Hilfsstromanteil 5 %, ohne Kühlträger und
> Abrechnungsart, mit einer gesäten Kühlkennlinie aus zehn Zeilen (Vorlauf 7 und 18 °C, 20 bis 40 °C,
> Laststufe 100) — vier Zellen und zehn Zeilen aus
> [`Skripte/kaelteerzeuger_1017_referenzprojekt.py`](Skripte/kaelteerzeuger_1017_referenzprojekt.py).
> Die übrigen zwölf Referenzprojekte stehen auf 0; ihre Gebäude laufen frei, keine ihrer
> Wärmepumpen kühlt.
>
> **Nicht** betroffen sind Kühleingaben und Kälteerzeuger von Projekten außerhalb der Referenzliste.

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
Basen aufräumt, lässt `2026-09-24_R14_Kaelteerzeuger`, `Kenndaten_Test.sqlite`,
`Importproben`, `Katalogpaket_frei`, `Skripte` und `LIESMICH.md` stehen.**

Außer der aktuellen liegt hier keine Basis mehr: Die 24 historischen Referenzbasen (7 731
Dateien, 1 016,7 MB) sind am 11.09.2026 aus dem Arbeitsbaum gefallen (**SYNC‑Q1**: „entfernt
lassen") und seit dem Umschreiben der Git-Geschichte (**AUF‑Q1**, 12.09.2026) auch dort nicht
mehr enthalten; **`2026-09-11_R7_Speicherflotte` ist am 16.09.2026 nach demselben Muster
gefallen, `2026-09-16_R8_Heizkessel_Kaskade` am 18.09.2026,
`2026-09-18_R9_Kesselbrennstoff` am 19.09.2026, `2026-09-19_R10_BhkwWirkungsgrad` am
22.09.2026, `2026-09-22_R11_Bestandsbefunde` und `2026-09-23_R12_Gebaeudemodell` am 23.09.2026,
`2026-09-23_R13_Kuehlung` am 24.09.2026** (31 Basen, alle sieben Protokolle gesichert). Kein Test, kein Gate, keine CI liest
eine entfernte Basis. **Die Messdaten sind endgültig weg** (rund 8 000 CSV-Dateien) — eine
alte Zahl steht nur noch im Protokoll.
Erhalten sind die **Protokolle** aller 31 Basen samt der Tabelle Basis → Datum → Zweck →
Protokoll unter
[`Dokumentation/ueberholt/Referenzbasen/`](../Dokumentation/ueberholt/Referenzbasen/LIESMICH.md);
**welche Basis wann von welcher abgelöst wurde und warum**, steht ebendort — bis zum 12.09.2026
in der ausgelagerten
[Basenhistorie](../Dokumentation/ueberholt/Referenzbasen/LIESMICH_Basenhistorie_bis_2026-09-12.md),
danach im Wegweiser desselben Ordners.

## Aktuelle Basis

**`2026-09-24_R14_Kaelteerzeuger/`** — **dreizehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024,
1030, 1039, 1040, 1041, 1042, 1045, 1046), **394 CSV**, **2 249 Skalare**, gerechnet mit dem
plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite` (eingefroren auf
Schemastand **119**, LFS-SHA-256 `63cc2d64…`; heute Schemastand **141**, LFS-SHA-256 `a427aa72…`,
Nachträge unten). Gegen diese Basis hält `.github/workflows/kern.yml` (1030, 1007, 1017, 1045,
1046) jeden Push, `ios.yml` den iZ6-Vergleich für 1030, und `EPOS.Kern.Tests/GebaeudeRueckwegTests` den
Tagesbilanz-Weg an Projekt 1040. Sie ist die **einzige** Basis im Arbeitsbaum.

> **Anlass: die vierte und letzte Welle der Stufe KU2 der Kühlung** (vom Anwender am 24.09.2026 samt
> dem Einfrieren beauftragt). Eine Änderung an den Daten, zwei an der Wirtschaftlichkeit:
>
> 1. **Testdatenbank: das Referenzprojekt mit Kälteerzeuger** (Kühlkonzept 10.4, Einfrierregel „gesäte
>    Kältedaten" oben): Die Wärmepumpe von 1017 kühlt — vier Zellen und zehn Zeilen über
>    [`Skripte/kaelteerzeuger_1017_referenzprojekt.py`](Skripte/kaelteerzeuger_1017_referenzprojekt.py):
>    `Tab_Einstellungen.Tool_3` leer → „Wärmepumpe" (Kaskadenplatz 3), Projektgerät 1017033
>    `Kuehlbetrieb` 0 → 1, `Kuehl_Vorlauf` NULL → 18, `Kuehl_Hilfsstromanteil` NULL → 0,05, dazu die
>    zehn Zeilen der gesäten Kühlkennlinie in `Tab_Kenndaten_Kuehlung` (die Tabelle war leer; SQLite
>    führt dazu die Zeile der Tabelle in `sqlite_sequence`). Sicherung vorher außerhalb des
>    Repositoriums; der Zellvergleich aller 132 Tabellen (10 498 993 Zellen) zeigt genau diese vier
>    Zellen, zehn Zeilen und die Sequenzzeile, die 14 Sichten und 208 Indizes gleich;
>    `integrity_check` ok, `foreign_key_check` leer, Größe unverändert 67 784 704 Byte, ein zweiter Lauf
>    des Skripts ändert nichts.
> 2. **E35 und die Reste von E34** — Grund- und Leistungspreis eines eigenen Zählers, der Preis des
>    vermiedenen Bezugs, die Bemessungsmenge nach § 9b StromStG, das Mengenszenario — ändern Kosten,
>    nicht die Simulation; der Referenzlauf führt keine Kosten, und kein Referenzprojekt trägt einen
>    Kühlträger. Vor der Datenänderung waren alle dreizehn Projekte gegen R13 byte-gleich.
>
> **Warum so** — Kaskadenplatz 3 hinter BHKW und Elektrokessel (ohne Platz rechnet die Maschine nicht,
> auf Platz 3 bleibt die Wärmeseite fast unverändert), Kühl-Vorlauf 18 °C (Flächenkühlung, sensible
> Kälte), Hilfsstromanteil 5 % (die Basis trägt den Zuschlag), kein Kühlträger (der Referenzfall ohne
> den Sonderweg aus E34), eine gesäte Kühlkennlinie aus runden, erfundenen Werten (der Katalogsatz des
> Geräts trägt keine), die Nennkühlleistung bleibt leer: Kühlkonzept 10.4.
>
> **Allein 1017 bewegt sich, zwölf Projekte bleiben byte-gleich** (alle Dateien):
>
> | 1017 | R13 | R14 |
> |---|---:|---:|
> | Kältebedarf [MWh/a] | 2,52 | 2,52 |
> | Kältedeckung durch die Wärmepumpe [MWh/a] | — | 2,48 (98,4 %) |
> | ungedeckte Kälte `Kaelterestbedarf` [MWh/a] | 2,52 | 0,04 |
> | Kältestrom, davon Hilfsstrom [MWh/a] | — | 0,55; 0,03 |
> | Jahresarbeitszahl Kälte (EER-Jahreswert) | — | 4,52 |
> | Kühltage | — | 43 |
> | Wärme der Wärmepumpe auf Platz 3 [MWh/a] | — | 0,04 |
> | Restwärme [MWh/a] | 0,14 | 0,10 |
> | Netzbezug `Stromrestbedarf` [MWh/a] | 655,31 | 655,88 |
>
> Der Kältestrom kommt ganz aus dem Netz (0,5487 von 0,5487 MWh/a): In den Kühlstunden deckt die
> Eigenerzeugung schon den übrigen Strombedarf nicht — Stromspeicher, BHKW und Elektrokessel bleiben
> Zeichen für Zeichen, wie sie waren; 1017 führt keine Photovoltaik. Kosten und CO₂ führt der
> Referenzlauf nicht; gerechnet (`ReferenzprojektKaelteerzeugerTests`) steigen die Stromkosten des
> Anschlusses um 283,55 €/a, der Kältestrom trägt 273,60 €/a und 0,24 t/a CO₂.
>
> **Dateien und Schlüssel:** 1017 bekommt die sieben Dateien der Wärmepumpe (`heizstab.csv`,
> `wp_produktion.csv`, `wp_quellentemperatur.csv`, `wp_restwaerme.csv`, `wp_strom.csv`,
> `wp_waermebedarf.csv`, `wp_warmwasserbedarf.csv`) und 42 Skalare (`Kaelte.*`, `Kaelte[0].*`,
> `Waermepumpe.*`, `WaermepumpeModul[0].*`, sieben Vektorsummen) — 387 → 394 CSV, 2 207 → 2 249
> Skalare. Gegen R13 meldet der Vergleich 1 689 Abweichungen, alle in 1017: die neuen Dateien und
> Schlüssel, die geänderten Skalare und die Reihen `reststrom_viertelstunde.csv` (1 660 geänderte
> Viertelstunden) und `restwaerme.csv` (19 geänderte Stunden).
>
> **Kein Fehlschlag, kein NaN, keine Ablehnung:** 13/13 Projekte gerechnet.
>
> **Einfrierregeln:** Die Regel „gesäte Kältedaten" umfasst jetzt die Kälteerzeugung. Emissionsfaktoren,
> PV-Modulkoeffizienten, Flottenstand 1046 und gesäte Gebäudedaten sind nicht berührt.
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **13/13 byte-gleich** (394/394 CSV) und
> **GESAMT: PASS** gegen diese Basis (4 207 049 Werte); ebenso byte-gleich ein Lauf vor dem Zusammenführen
> mit der Szenariopflege E9b (#462) — sie bewegt kein Referenzprojekt.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046 \
>   --ziel Referenzlaeufe/2026-09-24_R14_Kaelteerzeuger
> ```
>
> Ablauf und Ausstattung je Projekt stehen im `protokoll.txt` der Basis; die Abnahme der Stufe im
> [Protokoll der Schlusswelle KU2](../Dokumentation/ueberholt/Protokolle/Gebaeudesimulation/2026-09-24_Schlusswelle_KU2.md)
> und im [Status der Gebäudesimulation](../Dokumentation/aktuell/Status_Gebaeudesimulation_VDI6007.md).

> **Nachtrag: Schemastände 120 und 121 (Zusammenführung mit E10 #463 und #468), die Basis bleibt.** Die
> Testdatenbank ist die Fassung von origin mit Schemastand **121** (LFS-SHA-256 `00fbbb8b…`; die
> Schritte 120 und 121 stehen mit ihren Nachträgen beim R13-Abschnitt unter `ueberholt/`), auf die
> [`Skripte/kaelteerzeuger_1017_referenzprojekt.py`](Skripte/kaelteerzeuger_1017_referenzprojekt.py) erneut
> angewandt ist; `kuehlung_1017_referenzprojekt.py` und `gebaeude_10612_233_bauweise.py` finden nichts zu
> tun, ein zweiter Lauf des Skripts ändert nichts. Zellvergleich aller 132 Tabellen gegen die Fassung von
> origin (10 499 019 Zellen): genau die vier Zellen, zehn Zeilen und die Sequenzzeile der vierten Welle;
> 14 Sichten und 209 Indizes gleich, `integrity_check` ok, `foreign_key_check` leer, 67 792 896 Byte
> (LFS-SHA-256 `9acda529…`). Referenzlauf aller dreizehn Projekte gegen diese Basis: **13/13 PASS**
> (4 207 049 Werte, 394/394 CSV byte-gleich, außer `protokoll.txt`).

> **Nachtrag: Schemastände 122 und 123 (Anlagenkopplung, Stufe AK1 Welle 1), die Basis bleibt.** Die
> Testdatenbank steht über `Werkzeuge/Testdatenbankschema` auf Schemastand **123**: Schritt 122
> (`AK-S1`) legt die dreizehn Spalten der Wärmeübergabe an `Tab_Gebaeude` und `Tab_Gebaeude_STAMM`
> und baut `Abfrage_Projektgebaeude` ein drittes Mal neu (90 Spalten), dazu
> `Tab_Einstellungen.Anlagenkopplung` (Wertliste AUS/AK1/AK2/AK3, NULL = aus); Schritt 123 (`AK-S3`,
> Wärmeteil) die drei nullbaren Ergebnisspalten `Vorlauf_Mittel`, `Ruecklauf_Mittel` und
> `Uebergabe_Begrenzt_Stunden` an `Tab_ErgebnisEnergiebedarf`. Reines DDL: Sicherung vorher außerhalb
> des Repositoriums; der Zellvergleich aller 132 Tabellen gegen die Fassung 121 (10 499 091 Zellen)
> zeigt allein `SchemaVersion` 121 → 123, die 30 neuen Spalten sind leer (die vier Schalter 0), und
> nur die DDL der vier Tabellen und der Sicht hat sich geändert; 14 Sichten und 209 Indizes, 131 von
> 132 Tabellen STRICT, `integrity_check` ok, `foreign_key_check` leer, 67 792 896 Byte (LFS-SHA-256
> `1ba28e23…`). Ein zweiter Lauf des Werkzeugs legt nichts an; die Migration der Schale ergibt aus der
> Fassung 121 dieselbe DDL und dieselben Zellen. Kein Rechenweg liest die Spalten, und die drei
> Ergebnisspalten gehen erst mit einem Wert in `aggregate.csv` (`Referenzlauf/Ergebnisexport.cs`).
> Referenzlauf aller dreizehn Projekte gegen diese Basis: **13/13 PASS** (4 207 049 Werte, 394/394 CSV
> byte-gleich, außer `protokoll.txt`). Einfrierregeln sind nicht berührt; die Regel „gesäte
> Auslegungsdaten der Übergabe" (Anlagenkopplung 11.4) entsteht erst mit dem Referenzprojekt der
> Kopplung.

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

> **Nachtrag: Schemastand 124 (Zapfprofilgenerator, Stufe Z4, Schemaschritt T3) und drei Setzungen des
> freien Paketteils, die Basis bleibt.** Migrationsschritt **124** (`SCHRITT_124_ZAPFPROFIL_LAUFANGABEN`;
> Quelle `TwwSchema.SpaltenT3`, die Wertemengen stehen je einmal in `TwwSchema` für DDL und Schreibweg): an
> `Tab_TwwProjekt` die Laufangaben der Auslegung `Erzeugerart` (1, 2), `Uebertrager_Werkstoff` (1, 2),
> `Personen_Auto` (0/1, Vorgabe 1), `Personen_Manuell` (≥ 0) und `Fuellstand_Bezug` (1 bis 4), an
> `Tab_TwwBedarfstag_STAMM` die `Bezugsart` (1 bis 7, nullbar) — alle mit `CHECK`, sonst nullbar.
> Nachgezogen auf der Fassung von origin mit Schemastand **123** (Nachtrag Anlagenkopplung oben,
> `1ba28e23…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite` (sechs Spalten
> angelegt; ein zweiter Lauf legt nichts an), danach
> [`Skripte/tww_testkatalog_fiktiv.py`](Skripte/tww_testkatalog_fiktiv.py) mit `--stochastik`: Es führt am
> Ecodesign-Zapfprofil L des freien Paketteils die Bezugsart 2 (Wohneinheiten) nach und spielt aus
> [`Katalogpaket_frei/Tab_TwwParameter_STAMM.csv`](Katalogpaket_frei/LIESMICH.md) die drei Setzungen
> `Zapfprofil.Zirkulation.Hinweisverhaeltnis` (1,5), `Zapfprofil.Anzeigetemperatur` (45 °C) und
> `Zapfprofil.Stundenschwelle` (0,1 kW) nach der Regel der Testdatenbank ein — Setzungen von INEKON zur
> Bestätigung (ZU21) —, zusammen 3 angelegt, 1 nachgeführt; ein zweiter Lauf meldet 0/0. Zellvergleich
> aller 132 Tabellen gegen die Fassung 123 (10 503 133 Zellen): `SchemaVersion` 123 → 124, die sechs neuen
> Spalten — `Tab_TwwProjekt` ohne Zeile, die Bezugsart allein am Ecodesign-Tag gesetzt, an den drei
> fiktiven Tagen NULL —, die drei Zeilen in `Tab_TwwParameter_STAMM` (samt `sqlite_sequence`), sonst
> nichts; die 14 Sichten und alle 209 Indizes unverändert. `integrity_check` ok, `foreign_key_check` leer,
> Größe unverändert 67 792 896 Byte (LFS-SHA-256 `1d971b1a…`). **Keine Einfrierregel ist berührt:**
> Kein Referenzprojekt steht auf dem Generator. Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen
> diese Basis (4 207 049 Werte, 394/394 CSV byte-gleich, außer `protokoll.txt`).

> **Nachtrag E15 (#478): Schemastand 125 (Risikomodul der Wirtschaftlichkeit), die Basis bleibt.**
> Migrationsschritt **125** (`SCHRITT_125_RISIKOMODUL`; Quelle `SchemaKatalog.RisikomodulSpalten`), **reines DDL**
> an `Tab_ProjektWirtschaftlichkeit`: `Risiko_Art` (TEXT(10); leer = kein Risiko, `ZINS`, `ABZUG`),
> `Risiko_Zinszuschlag` [%-Punkte], `Risiko_Verlust` [€ je Periode] und `Risiko_Wahrscheinlichkeit` [%], nullbar,
> ohne Vorgabe (DIN EN 17463, 6.5 und Anhang F; Konzept Wirtschaftlichkeit § 2.11.2, V‑G7). Nachgezogen auf der
> Fassung **124** (Nachtrag oben, `1d971b1a…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`: vier Spalten angelegt,
> keine Tabelle, Marker 125. `integrity_check` ok, `foreign_key_check` leer, Größe unverändert 67 792 896 Byte
> (LFS-SHA-256 `6c4c32f9…`); die vier Spalten sind in allen fünf Parameterzeilen leer. **Ergebnisneutral:** Leer
> heißt „kein Risiko", und kein Referenzprojekt trägt eines; die Wirtschaftlichkeit geht ohnehin nicht in
> `aggregate.csv`. **Keine Einfrierregel ist berührt.** Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen
> diese Basis (4 207 049 Werte, 394/394 CSV byte-gleich, außer `protokoll.txt`).

> **Nachtrag: Schemastand 126 (Reparatur der Gebäude-Katalogsätze, Welle #485), die Basis bleibt.**
> Migrationsschritt **126** (`SCHRITT_GEBAEUDE_KATALOGREPARATUR`; die Nummer steht allein bei
> `GebaeudeKatalogReparatur.SCHRITT`, dort auch Anweisungen und Schadensbilder), **reines DML** an
> `Tab_Gebaeude_STAMM`, je Satz nach Bezeichner UND Schadensbild (Konzept Administrationsdialoge 7.1 (a)):
> `Krankenhaus_92-EnEV2016` U-Wert Fenster 0,09 → 1,3, Fensterfläche Nord 10 000 → 250 m², gesamte
> Fensterfläche 11 645,9 → 1 895,9 m² (Süd + Ost/West + Nord wie im Editor); „Fläche je Nutzer" leer →
> Wohnfläche ÷ Bewohner bei `EFH-BZ2` (40,0), `KrankenH-F-U-400` (50,0, Bewohner 360 → 360,24 wie die
> Geschwister), `KMEH-M-U-54` (31,78), `Z-EFH-A-S-126` (28,71); die acht Testreste 275–282
> (`Z2-EFH-A-S*`, `EFH-BZ2 XXX`) gelöscht — nur, weil keine Projektkopie sie über
> `ID_Gebaeude_Stamm` oder den Namen führt (ein benutzter Rest bliebe und stünde im Protokoll).
> Nachgezogen auf der Fassung 125 (Nachtrag E15 oben, `6c4c32f9…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite` (offen
> vorher 14, danach 0; ein zweiter Lauf findet nichts). Zellvergleich aller Tabellen gegen die Fassung 125
> (10 503 021 Zellen): `SchemaVersion` 125 → 126, die acht Zellen der fünf berichtigten Sätze und die acht
> gelöschten Zeilen, sonst nichts; 355 Schemaobjekte unverändert, `integrity_check` ok,
> `foreign_key_check` leer, 67 788 800 Byte (LFS-SHA-256 `0fe67575…`). Der Katalog zählt 269 Sätze,
> und jeder besteht die Prüfung des Gebäudeeditors (Wächter
> `GebaeudeKatalogverweisTests.Nach_der_Reparatur_besteht_jeder_Katalogsatz_die_Editorpruefung`).
> **Keine Einfrierregel ist berührt:** Keiner der dreizehn Sätze ist einem Referenzprojekt zugeordnet,
> und Projektkopien bleiben unberührt. Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese
> Basis (4 207 049 Werte, 394/394 CSV byte-gleich, außer `protokoll.txt`).

> **Nachtrag E17 (#479): Schemastand 127 (nicht monetarisierbare Wirkungen der Wirtschaftlichkeit), die Basis bleibt.**
> Migrationsschritt **127** (`SCHRITT_127_NICHT_MONETAERE_WIRKUNGEN`; Quelle `ProjektWirkungSchema` für Migration,
> Werkzeug und Testvorrichtung) legt die Tabelle `Tab_ProjektWirkung` an (STRICT; `ID`, `ID_Projekt` mit
> Fremdschlüssel auf `Tab_Projekt` ON DELETE/UPDATE CASCADE, `Sortierung`, `Kategorie` mit CHECK
> `ENERGIEFLUSS`/`FINANZIELL`/`SONSTIG`, `Beschreibung`, `Dauer` 1–3, `Wirkung_Organisation`, `Wirkung_Mitarbeiter`,
> `Wirkung_Umwelt` je 0–3, NULL = nicht beurteilt) samt Index `idx_ProjektWirkung_Projekt` und übernimmt einen
> gepflegten Freitext `Tab_ProjektWirtschaftlichkeit.Nicht_Monetaer` als eine Wirkung SONSTIG ohne Beurteilung
> (DIN EN 17463, 6.1 und 8.2; Konzept Wirtschaftlichkeit § 2.11.2, V‑G11). Der Schritt steht nach **126** (Reparatur
> der Gebäude-Katalogsätze, #485, Nachtrag Schemastand 126). Nachgezogen auf der Fassung **126** (`0fe67575…`,
> 67 788 800 Byte) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`: nur Tabelle und Index
> neu, **0 Freitexte übernommen** (kein Referenzprojekt pflegt einen), Schritt 126 fand nichts offen, Marker 127;
> 132 Tabellen, alle STRICT, 14 Sichten, 210 Indizes; `integrity_check` ok, `foreign_key_check` leer, ein zweiter Lauf
> meldet den Schritt als stehend. Größe 67 796 992 Byte (LFS-SHA-256 `87e49ed1…`). **Ergebnisneutral:** Kein Rechenweg liest die Tabelle,
> und die Wirtschaftlichkeit geht ohnehin nicht in `aggregate.csv`. **Keine Einfrierregel ist berührt.** Referenzlauf
> aller dreizehn Projekte **13/13 PASS** gegen diese Basis (4 207 049 Werte, 394/394 CSV byte-gleich, außer
> `protokoll.txt`).

> **Nachtrag AK1 Welle 3: Schemastand 128 (Heizkreis je Gebäude im Ergebnis, Anlagenkopplung), die Basis bleibt.**
> Migrationsschritt **128** (`SCHRITT_128_ERGEBNIS_HEIZKREIS`; die Nummer steht allein bei
> `ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS`, Quelle `ErgebnisGebaeudeSchema.SpaltenHeizkreis` für Migration, Werkzeug
> und Testvorrichtung) hängt vier nullbare Spalten an `Tab_ErgebnisGebaeude` (Muster E30; Konzept Anlagenkopplung 8.3,
> 9.4): `Uebergabe_Art` (CHECK `RADIATOR`/`FLAECHE`/`KONVEKTOR`; NULL = nicht gekoppelt gerechnet), `VorlaufMittel_C`,
> `RuecklaufMittel_C` und `UebergabeBegrenzt_H` (0 … 8 760) — **reines DDL**. Vergeben beim Merge mit origin
> (125 Risikomodul, 126 Katalogreparatur, 127 Wirkungen waren belegt); er steht nach **127**. Nachgezogen auf der
> Fassung **127** (Nachtrag E17 oben, `87e49ed1…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`: vier Spalten neu, alle
> leer, Marker 128. Zellvergleich aller 133 Tabellen gegen die Fassung 127 (11 973 380 Zellen): allein `SchemaVersion`
> 127 → 128 und der Tabellentext von `Tab_ErgebnisGebaeude`; `integrity_check` ok, `foreign_key_check` leer, 133 Tabellen
> (132 STRICT), 14 Sichten, 210 Indizes. Größe 67 796 992 Byte (LFS-SHA-256 `81209c50…`). **Ergebnisneutral:** Kein
> Referenzprojekt rechnet gekoppelt, und der Referenzlauf exportiert die Tabelle nicht. **Keine Einfrierregel ist
> berührt.** Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese Basis (4 207 049 Werte, 394/394 CSV
> byte-gleich, außer `protokoll.txt`).

> **Nachtrag E16 (#484): Schemastand 129 (Wiederholperiode je Kostenposition der Wirtschaftlichkeit), die Basis bleibt.**
> Migrationsschritt **129** (`SCHRITT_WIEDERHOLPERIODE`; die Nummer steht allein bei `WiederholperiodeSchema.SCHRITT`,
> der Quelle für Migration, Werkzeug und Testvorrichtung) hängt die nullbare Spalte `Wiederholperiode_a` (INTEGER, ohne
> Vorgabe; leer, 0 und 1 = jährlich) an `Tab_ProjektWerte` und an `Tab_KostenVorlagePosition` (DIN EN 17463, 6.3.1
> „alle n Jahre"; Konzept Wirtschaftlichkeit § 2.11.2, V‑G3) — **reines DDL**. In Phase 1 vorläufig 128; nach dem Push
> des Schritts 128 (Nachtrag AK1 Welle 3 oben) steht er als **129** nach 128, der Zwischenstand 127 → 128 der Welle
> (`f4a6ee8b…`) ist überholt. Nachgezogen auf der Fassung **128** (`81209c50…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`: zwei Spalten neu, keine
> Tabelle, Schritt 128 fand nichts offen, Marker 129; 133 Tabellen (132 STRICT), 14 Sichten, 210 Indizes;
> `integrity_check` ok, `foreign_key_check` leer. Größe 67 796 992 Byte, unverändert (LFS-SHA-256 `4c546a7c…`).
> **Ergebnisneutral:** Keine Zeile pflegt `Wiederholperiode_a`, leer rechnet jährlich wie vor dem Schritt, und die
> Wirtschaftlichkeit geht ohnehin nicht in `aggregate.csv`. **Keine Einfrierregel ist berührt.** Referenzlauf aller
> dreizehn Projekte **13/13 PASS** gegen diese Basis (4 207 049 Werte, 394/394 CSV byte-gleich, außer `protokoll.txt`).

> **Nachtrag #493: Schemastand 130 (Anschlusslängen im Gebäudekatalog), die Basis bleibt.**
> Migrationsschritt **130** (`SCHRITT_GEBAEUDE_ANSCHLUSSLAENGEN`; die Nummer steht allein bei
> `GebaeudeAnschlusslaengenReparatur.SCHRITT`, der Quelle für Migration, Werkzeug und Testvorrichtung; der Schritt der
> Zapfprofil-Sitzung folgt ihm) berichtigt nach Anwenderentscheid vom 24.09.2026 („Ersetzt durch plausible Werte“)
> 20 Zellen an sieben Sätzen von `Tab_Gebaeude_STAMM` — **reines DML, je Satz, Spalte und Schadensbild** (Bezeichner und
> unplausibler Wert ± 0,05), nichts gelöscht, Projektkopien unberührt:
>
> | Satz | Spalte | vorher | nachher | Herleitung |
> |---|---|---|---|---|
> | 79 `Krankenhaus_92-EnEV2016` | `Abmessung_Anschluß_Fenster_Wand` | 1 800 | 4 812,0 m | Laibung je m² Fenster des Ausgangssatzes 78 `KrankenH_NE` 7 655,75 / 3 016,3 = 2,5381 m/m² × 1 895,9 m² |
> | 79 | `Flaeche_Außenwand` | 12 094 | 13 214,4 m² | Hüllfläche der Geometrie von 78: 12 094 + 3 016,3 = 15 110,3 m² minus Fenster 1 895,9 m² (Ost/West bleibt 400) |
> | 80–83 `KrankenH-F-*`, 37 `gr_Hotel-G-134` | `Abmessung_Anschluß_Fenster_Wand` | 243,7 | 7 879,0 m | Tausch mit der Dachkante trägt: 7 879 / 3 062,3 = 2,573 m/m² (78: 2,538); 243,7 m wären 0,08 m/m² |
> | dieselben | `Abmessung_Anschluß_Wand_Dach` | 7 879 | 313,8 m | Umfang der Grundfläche 1 469 m² wie bei 78 (Wand–Dach = Keller = 313,8). Der Tausch (243,7 m) trägt hier nicht: Hülle 13 156,3 m² / 243,7 m = 54,0 m = 4,40 m je Geschoss (12,26 Geschosse, Raumhöhe 2,55 m); mit 313,8 m 3,42 m je Geschoss (78: 3,54 m bei 3,0 m) |
> | dieselben | `Abmessung_Anschluß_Außenwand_Kellerdecke` | 1 392,8 | 313,8 m | Umfang wie bei 78; 1 392,8 m wären das 4,4-Fache |
> | 77 `Kaufhaus` | `Abmessung_Anschluß_Fenster_Wand` | 243,7 | 5 820,8 m | Abwandlung der F-Sätze (Fenster 2 262,36 m²); Tausch gäbe 3,48 m/m², daher Verhältnis der F-Quelle 2,5729 m/m² × 2 262,36 m² |
> | 77 | `Abmessung_Anschluß_Wand_Dach`, `…_Außenwand_Kellerdecke` | 7 879 / 1 392,78 | 313,8 / 313,8 m | Umfang der Grundfläche 1 469 m² wie bei 78 und den F-Sätzen |
>
> Nachgezogen auf der Fassung **129** (Nachtrag E16 oben, `4c546a7c…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`: offen vorher
> 20, berichtigt 20, offen danach 0, Marker 130. Zellvergleich aller 133 Tabellen gegen die Fassung 129: allein
> `SchemaVersion` 129 → 130 und die 20 Zellen der Tabelle; Schema unverändert; `integrity_check` ok, `foreign_key_check`
> leer, 133 Tabellen (132 STRICT), 14 Sichten, 210 Indizes. Größe 67 796 992 Byte (LFS-SHA-256 `f8fe1b76…`).
> **Ergebnisneutral:** Keinen der sieben Sätze führt ein Projekt der Testdatenbank (weder über `ID_Gebaeude_Stamm` noch
> über den Namen); die dreizehn Referenzprojekte führen die Sätze 125, 129, 142–146, 233, 56. **Keine Einfrierregel ist
> berührt.** Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese Basis (4 207 049 Werte).

> **Nachtrag: Schemastand 131 (Zapfprofilgenerator, Stufe Z4b, Schemaschritt T3 „Typtage"), die Basis
> bleibt.** Migrationsschritt **131** (`SCHRITT_131_ZAPFPROFIL_TYPTAGE`; Quelle
> `TwwSchema.AnweisungenT3Typtage`): die Tabelle `Tab_TwwTyptag_IMPORT` — STRICT, elf Spalten (`ID`,
> `Art`, `Klimazone`, `Gebaeudeart`, `Typtag`, `Aufloesung_min`, `Zeilenindex`, `Wert`, `Quelle`,
> `Ausgabe`, `Datum_Import`), natürlicher Schlüssel (`Art`, `Klimazone`, `Gebaeudeart`, `Typtag`,
> `Zeilenindex`), **kein `Status` und kein `ReadOnly`**, kein Fremdschlüssel. Sie nimmt die Typtage auf,
> die der **lizenzierte Anwender** selbst einspielt; das Repositorium bringt keine Zeile mit (Konzept
> Kapitel 6), und die Auslieferungsvorlage leert sie. Im SELBEN Schritt stehen die drei Projektspalten
> der Wahl an `Tab_TwwProjekt` — `Typtage_Aktiv` (0/1, Vorgabe 0), `Typtage_Klimazone` und
> `Typtage_Gebaeudeart` (beide NULL = keine Wahl), Quelle `TwwSchema.SpaltenT3Typtage`. Die Nummer war in
> der Arbeit 125; beim Zusammenführen mit origin waren 125 bis 129 belegt (Risikomodul, Gebaeude-
> Katalogreparatur, Wirkungen, Heizkreis, Wiederholperiode) und mit der Welle #493 auch 130
> (Anschlusslängen im Gebäudekatalog); der Schritt steht jetzt nach **130**.
> Nachgezogen auf der Fassung von origin mit Schemastand **130** (Nachtrag #493 oben,
> `f8fe1b76…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite` (eine
> Tabelle und drei Spalten angelegt; ein zweiter Lauf legt nichts an), danach
> `tww_testkatalog_fiktiv.py --stochastik` (0 angelegt, 0 nachgeführt — der Testkatalog stand schon
> vollständig da; zweiter Lauf 0/0). **Kein DML:** Die Tabelle ist und bleibt LEER, `Typtage_Aktiv`
> steht auf 0 und beide Angaben auf NULL. Zellvergleich aller Tabellen gegen die Fassung 130
> (10 502 791 Zellen): allein `SchemaVersion` 130 → 131, die drei neuen Spalten und der Tabellentext von
> `Tab_TwwProjekt`; `integrity_check` ok, `foreign_key_check` leer, 134 Tabellen (133 STRICT) statt 133
> (132), 14 Sichten und 211 Indizes unverändert. Größe 67 805 184 Byte (LFS-SHA-256 `a4a88c33…`).
> **Keine Einfrierregel ist berührt:** Kein Referenzprojekt steht auf dem Generator, und ohne
> eingespielte Typtage ist der Typtagweg benannt nicht verfügbar.
> Referenzlauf der fünf CI-Projekte (1030, 1007, 1017, 1045, 1046) **5/5 PASS** gegen diese Basis,
> 160/160 CSV **byte-gleich**.

> **Nachtrag AK1 Welle 4 (E37): Schemastände 135 bis 137 (Kälteseite der Anlagenkopplung), die Basis
> bleibt.** Drei Migrationsschritte, die Nummern stehen allein bei `KuehluebergabeSchema` (Quelle für
> Migration, Werkzeug und Testvorrichtung): **135** (`SCHRITT_KUEHLUEBERGABE`, KAK-S1) legt die acht
> Spalten der Kühlübergabe an `Tab_Gebaeude` und `Tab_Gebaeude_STAMM` — `Kuehluebergabe_Aktiv` (0/1,
> Vorgabe 0), `Kuehl_Uebergabe_Art`, `Kuehl_Uebergabe_Exponent`, `Kuehl_Uebergabe_Leistung_Nenn`,
> `Kuehl_Auslegung_Vorlauf`, `Kuehl_Auslegung_Ruecklauf`, `Kuehl_Auslegung_Raumtemperatur`,
> `Kuehl_Vorlaufgrenze`, sonst alle NULL — und baut die Sicht `Abfrage_Projektgebaeude` zum vierten Mal
> neu (98 Spalten, die 90 davor an ihren Stellen); **136** (`SCHRITT_KUEHLUEBERGABE_ERGEBNIS`, KAK-S3)
> hängt `Kuehl_Vorlauf_Mittel`, `Kuehl_Ruecklauf_Mittel` und `Kuehl_Uebergabe_Begrenzt_Stunden` an
> `Tab_ErgebnisEnergiebedarf` und `Kuehl_Uebergabe_Art` (CHECK der drei Arten), `KuehlVorlaufMittel_C`,
> `KuehlRuecklaufMittel_C`, `KuehlUebergabeBegrenzt_H` und `KuehlVorlaufgrenze_H` (0 … 8 760) an
> `Tab_ErgebnisGebaeude`; **137** (`SCHRITT_KUEHLUEBERGABE_ZONE`) hängt `Kuehl_Uebergabe_Art` (CHECK samt
> `IDEAL`), `Kuehl_Uebergabe_Exponent` und `Kuehl_Uebergabe_Leistung_Nenn` an `Tab_Zone` — **reines
> DDL**. Sie stehen nach S-C (134, Stufe G3). Die drei Energiebedarf-Spalten gehen erst mit einem Wert in
> `aggregate.csv` (`SpaltenNurMitWert`). Nachgezogen auf der Fassung von origin mit Schemastand **134**
> (G3 Welle B, `9d3c006a…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -- Referenzlaeufe/Kenndaten_Test.sqlite`: 27 Spalten
> neu, Marker 137; ein zweiter Lauf legt nichts an. Zellvergleich aller Tabellen gegen die Fassung 134
> (10 504 069 Zellen): allein `SchemaVersion` 134 → 137, die 27 neuen Spalten (der Schalter 0, alles
> andere NULL) und die Sicht; `integrity_check` ok, `foreign_key_check` leer, 141 Tabellen (alle STRICT),
> 14 Sichten, 215 Indizes. Größe 67 887 104 Byte (LFS-SHA-256 `834aa718…`). **Ergebnisneutral:** Kein
> Referenzprojekt rechnet gekoppelt. **Keine Einfrierregel ist berührt**; die Einfrierregel der
> Auslegungsdaten der Wärme- und Kühlübergabe samt `Kuehluebergabe_Aktiv` kommt mit der fünften Welle von
> AK1. Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese Basis (4 207 049 Werte, 394/394
> CSV byte-gleich, außer `protokoll.txt`).

> **Nachtrag G4c Welle 3: Schemastand 138 (Herkunftsablage der Gebäudeimporte, Schritt S-F), die Basis bleibt.**
> Migrationsschritt **138** (`SCHRITT_IMPORTZUORDNUNG`; die Nummer steht allein bei `ImportzuordnungSchema.SCHRITT`,
> der Quelle für Migration, Werkzeug und Testvorrichtung) folgt den Mehrzonenschritten 132–134 der Stufe G3 und der
> Kälteseite 135–137 (AK1 Welle 4) und legt zwei leere STRICT-Tabellen an — **reines DDL, keine Saat**:
> `Tab_Importquelle` (eine Zeile je Importlauf: Gebäude mit Kaskade, Format `IFC`/`GBXML`, Dateiname ohne Pfad,
> SHA-256, Größe, Schemastand, Zeitpunkt, Programmfassung, Zonenregel, Zahl fehlender Entitäten) und
> `Tab_Importzuordnung` (eine Zeile je Paarung: Quelle mit Kaskade, fünf nullbare Zielverweise auf Gebäude, Zone,
> Bauteil, Aufbau und Baustoff, genau einer gesetzt, ebenfalls mit Kaskade — Begründung der Abweichung von
> Softwarearchitektur 2.2 im Kopf von `ImportzuordnungSchema` —, Quellkennung bis 64 und Quelltyp bis 40 Zeichen)
> samt den Indizes über `ID_Importquelle` und `Quellkennung`. In der Arbeit trug der Schritt die Nummer 135; beim
> Zusammenführen mit origin waren 135 bis 137 von der Kälteseite belegt.
> Nachgezogen auf der Fassung von origin mit Schemastand **137** (Nachtrag AK1 Welle 4 oben, `834aa718…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite` (zwei Tabellen
> angelegt; ein zweiter Lauf legt nichts an). Zellvergleich aller 141 Tabellen gegen die Fassung 137 (11 977 604 Zellen):
> allein `SchemaVersion` 137 → 138; neu sind die zwei leeren Tabellen und die zwei Indizes, kein bestehender Tabellen-,
> Sicht- oder Indextext ist geändert; `integrity_check` ok, `foreign_key_check` leer, 143 Tabellen (alle STRICT) statt
> 141, 14 Sichten, 217 Indizes statt 215. Größe 67 903 488 Byte (LFS-SHA-256 `3f5c892d…`). **Ergebnisneutral, keine
> Einfrierregel berührt:** Kein Rechenweg liest die Tabellen, und kein Referenzprojekt führt einen Import.
> Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese Basis (4 207 049 Werte, 394/394 CSV byte-gleich,
> außer `protokoll.txt`).

> **Nachtrag G4a Welle 3: Schemastand 139 (Baujahr des Gebäudes), die Basis bleibt.** Migrationsschritt **139**
> (`SCHRITT_BAUJAHR`; die Nummer steht allein bei `BaujahrSchema.SCHRITT`, der Quelle für Migration, Werkzeug und
> Testvorrichtung, die Definitionen bei `GebaeudeSchema`) folgt auf S-F (138) und legt an `Tab_Gebaeude` und
> `Tab_Gebaeude_STAMM` spaltengleich die Spalte `Baujahr INTEGER` mit
> `CHECK (Baujahr IS NULL OR Baujahr BETWEEN 1500 AND 2100)` an und baut die Sicht `Abfrage_Projektgebaeude` zum
> fünften Mal neu (99 Spalten, die 98 von KAK-S1 an ihren Stellen, `Baujahr` an Stelle 98) — **reines DDL, keine
> Saat**. Nachgezogen auf der Fassung von origin mit Schemastand **138** (Nachtrag G4c Welle 3 oben, `3f5c892d…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite` (zwei Spalten
> angelegt; ein zweiter Lauf legt nichts an). Zellvergleich aller 143 Tabellen gegen die Fassung 138 (10 506 686 Zellen
> der gemeinsamen Spalten): allein `SchemaVersion` 138 → 139; neu ist die Spalte `Baujahr` in beiden Gebäudetabellen,
> in allen 26 bzw. 269 Zeilen NULL, geändert allein der Text der Sicht; `integrity_check` ok, `foreign_key_check` leer,
> 143 Tabellen (alle STRICT), 14 Sichten, 217 Indizes. Größe 67 903 488 Byte (LFS-SHA-256 `f700e81e…`).
> **Ergebnisneutral, keine Einfrierregel berührt:** Die Spalte bleibt in jeder Zeile NULL, es ist also nichts gesät,
> und kein Rechenweg liest sie — weder der Eingangsbauer des Gebäudemodells noch der Tagesbilanz-Weg noch eine Vorgabe
> (die Vorgaben hängen an der Baualtersklasse, nicht am Jahr); sie gehört damit nicht zu den Spalten des Gebäudemodells,
> die die Einfrierregel „gesäte Gebäudedaten“ nennt. Referenzlauf aller dreizehn Projekte **13/13 PASS** gegen diese
> Basis (4 207 049 Werte, 394/394 CSV byte-gleich, außer `protokoll.txt`).

> **Nachtrag Stufe Z5: Schemastand 140 (Zapfprofilgenerator, Schemaschritt T4 „Messreihen"), die
> Basis bleibt.** Ein Migrationsschritt, die Nummer steht allein bei
> `TwwSchema.SCHRITT_T4_MESSREIHEN` (Quelle für Migration, Werkzeug und Testvorrichtung; sie folgt
> lückenlos auf das Baujahr des Gebäudes, 139): **140** (`SCHRITT_140_ZAPFPROFIL_MESSREIHEN`) legt
> `Tab_TwwMessreihe` an — STRICT, zehn Spalten, eine Zeile je Wert, natürlicher Schlüssel
> (`ID_Projekt`, `Bezeichnung`, `Zeilenindex`), `ID_Projekt` mit `ON DELETE CASCADE`, kein `Status`
> und kein `ReadOnly` — samt ihrem Index auf `ID_Projekt`. **Reines DDL;** die Tabelle entsteht LEER
> und bleibt es: Gemessene Reihen gehören dem Objekt (Konzept Kapitel 9 K5), das Repositorium bringt
> keine mit, und ohne eingespielte Messreihe ist der Vergleich benannt nicht verfügbar.
> In der Arbeit trug der Schritt zuerst die Nummer 138, dann 139; beim Zusammenführen mit origin
> war 138 vom Schritt S-F der Gebäudeimporte und 139 vom Baujahr der Stufe G4a belegt — wer zuerst
> schiebt, hält die Nummer.
> Nachgezogen auf der Fassung von origin mit Schemastand **139** (Nachtrag G4a Welle 3 oben,
> `f700e81e…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`:
> 1 Tabelle und 1 Index neu, Marker 140; ein zweiter Lauf legt nichts an. Danach der fiktive
> Testkatalog der Stufen Z0 bis Z5
> (`py Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py Referenzlaeufe/Kenndaten_Test.sqlite --stochastik`):
> 14 Zeilen neu, 28 nachgeführt — fünf Tagesgangsätze, 20 Tagesgänge, acht Nutzungsarten, vier
> Bedarfstage mit 33 Ereignissen, 85 Parameter, fünf DIN-4708-Werte und 24 Zapfkategorien, alle
> `FIKTIV` oder „(abgeleitet)"; kein Normwert, kein Herstellerwert, keine Projektzeile. Ein zweiter
> Lauf schreibt nichts (0 neu, 0 nachgeführt).
> `integrity_check` ok, `foreign_key_check` leer, 144 Tabellen (alle STRICT), 14 Sichten,
> 205 Indizes (219 samt den von SQLite angelegten). Größe 67 923 968 Byte
> (LFS-SHA-256 `5de448e8…`). Zellvergleich aller 143 gemeinsamen Tabellen gegen die Fassung von
> origin (10 506 805 Zellen): abweichend allein `Tab_Applikation.SchemaVersion` (139 → 140) und die
> Katalogzeilen des Skripts (`Tab_TwwNutzungsart_STAMM` 8 statt 7, `Tab_TwwParameter_STAMM` 85 statt
> 80, `Tab_TwwZapfkategorie_STAMM` 24 statt 28 — die Stufe Z5 führt die Nichtwohnen-Nutzungsarten
> mit zwei statt vier Kategorien); `Tab_TwwMessreihe` steht mit 0 Zeilen. **Ergebnisneutral:** Kein
> Referenzprojekt führt eine Messreihe, und kein Rechenweg der dreizehn liest den Tww-Katalog.
> **Keine Einfrierregel ist berührt.**

> **Nachtrag #496: Schemastand 141 (Folgeberichtigung im Gebäudekatalog), die Basis bleibt.**
> Migrationsschritt **141** (`SCHRITT_GEBAEUDE_FOLGEREPARATUR`; die Nummer steht allein bei
> `GebaeudeAnschlusslaengenFolgereparatur.SCHRITT`, der Quelle für Migration, Werkzeug und Testvorrichtung; er folgt
> auf die Messreihen, 140) berichtigt nach Anwenderauftrag vom 25.09.2026 („setze um: weiteren Scan-Kandidaten mit
> vertauschten Anschlusslängen, die Außenwand des Kaufhauses“) 39 Zellen an zwanzig Sätzen von `Tab_Gebaeude_STAMM` —
> **reines DML** in der Bauart von #493 (Bezeichner und unplausibler Wert ± 0,05 je Spalte, dieselben Anweisungen),
> nichts gelöscht, Projektkopien unberührt. **Regel der Herleitung:** (1) führt ein Satz gleicher Geometrie
> (Ausgangssatz, nicht die gerundete EnEV-Abwandlung) den Umfang, gilt er; (2) sonst der Tausch von Laibung und
> Dachkante, wenn er für beide Spalten trägt (Laibung im Band des Katalogs 1,2 … 3,4 m je m² Fenster, Median 2,57;
> Dachkante nicht unter der Quadratkante 4·√Grundfläche und nahe dem Umfang U = (Außenwand + Fenster) /
> (Nutzfläche / Grundfläche × Raumhöhe)); (3) sonst Laibung = Verhältnis der Quelle × Fensterfläche, Dachkante =
> Umfang aus der eigenen Geometrie. ΔH_T = Σ ψ·ΔL bzw. U_AW·ΔA je Satz.
>
> | Satz | Spalte | vorher | nachher | Herleitung | ΔH_T |
> |---|---|---|---|---|---|
> | 2, 4, 5, 7, 9, 10 `AltenH-C-*`, `Pflegeheim-C-*`; 92 `Schule-C-U-202` | `Abmessung_Anschluß_Fenster_Wand` / `…_Wand_Dach` | 185 / 985 | 985 / 185 m | Regel (2), Tausch: Laibung 1,81 m/m² (185 m wären 0,34); Umfang der Geometrie (2 132 + 545) / (3 020 / 540 × 2,55) = 187,7 m neben 185 m, Quadratkante 93,0 m | +70,4 W/K (ψ 0,228 / 0,14) |
> | 94 `Schule-NE1`, 96 `Schule-NE-66` | dieselben | 185 / 985 | 985 / 185 m | dieselbe Geometrie wie die Heime | −48,0 W/K (ψ 0,04 / 0,10) |
> | 17 `Hallenbad-652`, 20 `Hallenbad-Sauna-750` | dieselben | 185 / 985 | 985 / 185 m | Regel (2): Laibung 2,35 m/m² (420 m² Fenster); Dachkante zwischen Quadratkante 132,7 m und Umfang der Geometrie 206,3 m (1 100 m², 78,5 × 14,0 m) | +70,4 W/K |
> | 54 `Hotel-F-228` | `Abmessung_Anschluß_Wand_Dach` | 5 380,75 | 116,16 m | Regel (1): `Kaufhalle_NE` (76) hat dieselbe Geometrie (Wand 834, Fenster 243,4, Dach 473,7, Grund 480,8, Nutzfläche 1 138 m²) und führt Wand–Dach = Keller = 116,16 m (48,08 × 10,0 m; Hülle 1 077,4 m² / 116,16 m = 9,28 m = drei Geschosse zu 3,09 m); 5 380,8 m wären das 61,8-Fache der Quadratkante | −1 316,1 W/K (ψ 0,25) |
> | 69 `ml_Hotel-F-228`, 71 `ml-Hotel-F-228` | `Abmessung_Anschluß_Wand_Dach` | 5 380,8 | 116,16 m | wie 54 | −1 316,2 W/K |
> | 54, 69, 71 | `Abmessung_Anschluß_Außenwand_Kellerdecke` | 40 | 116,16 m | wie 54 — der Ausgangssatz führt die Kellerkante als Umfang; 40 m wären weniger als die halbe Quadratkante. Laibung 515,2 m (2,12 m/m²) bleibt | +38,1 W/K (ψ 0,50); je Satz −1 278,1 W/K |
> | 42 `Hotel_G_96`, 72 `ml-Hotel-G-096`, 134 `GMH-G-U-97` | `Abmessung_Anschluß_Fenster_Wand` / `…_Wand_Dach` | 86,6 / 295,5 | 295,5 / 86,6 m | Regel (2): Laibung 1,24 m/m² (unterer Rand des Katalogs wie `GMH KfW 55` mit 1,20; 86,6 m wären 0,36); Umfang der Geometrie (433 + 237,6) / (1 263 / 431,2 × 2,61) = 87,7 m, Quadratkante 83,1 m | +31,3 W/K (ψ 0,22 / 0,07) |
> | 84 `GMH-BZ_T`, 85 `GMH-J-015` | `Abmessung_Anschluß_Fenster_Wand` | 86,6 | 382,6 m | Regel (3): dieselben Längen auf fremder Geometrie, der Tausch trägt nicht (0,96 m/m²; 86,6 m lägen unter der Quadratkante 88,1 m) — Verhältnis von 42 nach dem Tausch 295,5 / 237,6 = 1,2437 m/m² × 307,6 m² | +65,1 W/K |
> | 84, 85 | `Abmessung_Anschluß_Wand_Dach` | 295,5 | 122,3 m | Umfang der Geometrie (633 + 307,6) / (1 430 / 485,2 × 2,61) = 122,28 m (51,8 × 9,4 m) | −12,1 W/K; je Satz +53,0 W/K |
> | 77 `Kaufhaus` | `Flaeche_Außenwand` | 10 093,99 | 1 820,9 m² | die 10 094 m² stammen aus den F-Sätzen (Nutzfläche 18 012 m², zwölf Geschosse); eigene Geometrie: 4 201 / 1 468,97 = 2,86 Geschosse × 4,55 m = 13,01 m, Hülle 313,8 m × 13,01 m = 4 083,2 m² minus Fenster 2 262,36 m² (Fensteranteil 55 %). Wand–Dach = Keller = 313,8 m aus #493 bleiben: einziger belegter Umfang dieser Grundfläche (`KrankenH_NE`), trägt die Fensterfläche (mindestens 173,9 m Fassade); die 10 094 m² ergäben 949,6 m Umfang — eine Grundfläche von 3 m Tiefe | −4 963,9 W/K (U 0,6) |
>
> **Nicht geändert, berichtet.** Kellerkanten: 0 m bei den Heimen, Schulen und Hallenbädern (die
> EnEV-Abwandlungen 3, 8 führen 140 m bei 200 m Dachkante, nicht den Umfang; ψ der C-Sätze 0), 14,6 m bei 42, 72,
> 134, 84, 85 (kein Ausgangssatz führt sie als Umfang; Vorschlag: der Umfang 86,6 bzw. 122,3 m; bei ψ 0,65/0,67
> +46,8 bis +48,2 bzw. +72,2 W/K) — der Katalog führt die Kellerkante systematisch klein. Dazu die Scan-Gruppen, Entscheidung
> beim Anwender:
>
> | Satz | Befund | Vorschlag Laibung | Herleitung des Vorschlags |
> |---|---|---|---|
> | 6 `Pflegeheim-122-EnEV2016` | 0 m bei 545 m² Fenster (Kanten 40 / 30 m) | 540 m | EnEV-Abwandlung 8 gleicher Geometrie: 540 / 545 = 0,99 m/m² (nach dem C-Verhältnis 1,81 wären es 985 m) |
> | 15 `Industriehalle-320` | alle drei Längen 0 | 16 000 m | Satz 14 `Industrie_ne_81` gleicher Geometrie: 2,5 m/m² × 6 400 m²; Kanten dort 7 337,4 m |
> | 43 `Hotel_H_BZ`, 64 `kl_Hotel-H-086` | alle drei Längen 0 | 391,5 m | Sätze 65, 73 gleicher Geometrie: 2,5 m/m² × 156,6 m²; Kanten dort 71,0 m |
> | 117 `Verw_H_75` | alle drei Längen leer | 391,5 m | Satz 118 `Verw_I_33` gleicher Geometrie; Kanten dort 70,98 m |
> | 105 `Büro1-F-U-89`, 107 `Bürogebäude_F_72` | alle drei Längen leer, auch ψ | 1 462,1 m | Verwaltung F (115 `Verw_F_147`): 2,901 m/m² × 504 m²; Umfang der Geometrie 103,4 m |
> | 106 `Bürogebäude KfW 55` | alle drei Längen 0 | 2 875 m | Verhältnis der NE-/I-Sätze 2,5 m/m² × 1 150 m²; Umfang der Geometrie 264,1 m |
> | 207 `KMH-G-U-120` | Laibung 0 (Kanten 250,68 / 28 m) | 238,3 m | KMH G (206, 209): 268,6 / 112,02 = 2,398 m/m² × 99,37 m² |
> | 23 `Hallenbad-Umkl-140-EnEV2016` | 50 m (0,12 m/m²), gerundet | 865,1 m | Hallenbad-Umkleide 24: 142 / 70,4 = 2,017 m/m² × 428,9 m² |
> | 34 `gr_Hotel-80-EnEV2016` | 600 m (0,25 m/m²), gerundet | 6 164,4 m | F-Quelle gleicher Geometrie 2,5729 m/m² × 2 395,9 m² |
> | 108 `Bürogebäude_gross-30-EnEV2016` | 330 m (0,18 m/m²), gerundet | 4 460 m | Verhältnis der NE-/I-Sätze 2,5 m/m² × 1 784 m² |
>
> „Nur Dachkante“ (35, 39–41, 47–49, 52, 55, 58, 61, 63, 66, 67, 112–114, 127, 128, 130, 144–146, 151, 169, 173,
> 189–191, 195–197, 205, 206, 209–213, 274; 4,7- bis 8-fache Quadratkante, vermutlich geneigte Dächer) bleibt
> unberührt, darunter die eingefrorenen Referenzsätze 145 und 146. Nebenbefunde ohne Scan-Eintrag: Laibung 0,64 /
> 0,46 / 0,32 m/m² bei 46 `Hotel-72-EnEV2016`, 57 `Hotel-KfW 55` und 120 `Verwaltung_40-EnEV2016`; Dachkante
> 7 337,4 m (9,6-fache Quadratkante) bei 14 `Industrie_ne_81`.
>
> Nachgezogen auf der Fassung von origin mit Schemastand **140** (Nachtrag Stufe Z5 oben, `5de448e8…`) mit
> `dotnet run --project Werkzeuge/Testdatenbankschema -c Release -- Referenzlaeufe/Kenndaten_Test.sqlite`: offen vorher
> 39, berichtigt 39, offen danach 0, Marker 141. Zellvergleich aller 144 Tabellen gegen die Fassung 140
> (10 506 856 Zellen): allein `SchemaVersion` 140 → 141 und die 39 Zellen der Tabelle; Schema unverändert;
> `integrity_check` ok, `foreign_key_check` leer, 144 Tabellen (alle STRICT), 14 Sichten, 205 Indizes (219 samt den
> von SQLite angelegten). Größe 67 915 776 Byte (LFS-SHA-256 `a427aa72…`). **Ergebnisneutral:** Keinen der zwanzig
> Sätze führt ein Projekt der Testdatenbank (weder über `ID_Gebaeude_Stamm` noch über den Namen); die dreizehn
> Referenzprojekte führen die Sätze 125, 129, 142–146, 233, 56. **Keine Einfrierregel ist berührt.** Referenzlauf
> aller dreizehn Projekte **13/13 PASS gegen diese Basis (4 207 049 Werte, 394/394 CSV byte-gleich, außer `protokoll.txt`)**.

> **Die Vorgängerbasis `2026-09-23_R13_Kuehlung`**, die erste Basis mit Kühlung, ist mit dieser
> Einfrierung aus dem Arbeitsbaum gefallen; ihr Protokoll samt der Begründung zu E32 und dem
> Referenzprojekt mit Kühlung und den Nachträgen zu den Schemaständen 114 bis 121 steht in
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
| `Skripte/` | Was an dieser Testdatenbank gemacht wurde, als Skript und nicht als Erzählung: `pruefprojekt_1045_ost_west.py` (W6‑O‑7), `pruefprojekt_1046_speicherflotte.py` (SP‑O‑8), `gebaeude_10576_bauweise.py` (Stufe GB, Befund D), `gebaeude_10612_233_bauweise.py` (dieselbe Korrektur an 1009 und Katalogsatz 233, Basis unverändert), `tww_testkatalog_fiktiv.py` (Testkatalog des Zapfprofilgenerators samt abgeleiteten VDI-Werten und den Zeilen des freien Paketteils, Schemastand 115) und `normzahlen_abgeleitet_bauen.py` (nur lokal: abgeleitete VDI-6002-Werte nach `tww_katalogwerte_abgeleitet.json` und abgeleitete VDI-4655-Werte nach `vdi4655_abgeleitet.json`, ZU19; `--norm vdi6002|vdi4655|beide`) |

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
   `2026-09-24_R14_Kaelteerzeuger`, ist plattformfrei gegen `Kenndaten_Test.sqlite` gerechnet:
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
