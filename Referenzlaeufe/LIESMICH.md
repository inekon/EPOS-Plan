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

## Entfernte Basen

**`Referenzlaeufe/Importproben` gehört zum Testbestand und wird nie gelöscht; wer die Ordner der
Basen aufräumt, lässt `2026-09-18_R9_Kesselbrennstoff`, `Kenndaten_Test.sqlite`,
`Importproben`, `Skripte` und `LIESMICH.md` stehen.**

Außer der aktuellen liegt hier keine Basis mehr: Die 24 historischen Referenzbasen (7 731
Dateien, 1 016,7 MB) sind am 11.09.2026 aus dem Arbeitsbaum gefallen (**SYNC‑Q1**: „entfernt
lassen") und seit dem Umschreiben der Git-Geschichte (**AUF‑Q1**, 12.09.2026) auch dort nicht
mehr enthalten; **`2026-09-11_R7_Speicherflotte` ist am 16.09.2026 nach demselben Muster
gefallen, `2026-09-16_R8_Heizkessel_Kaskade` am 18.09.2026** (26 Basen, beide Protokolle
gesichert). Kein Test, kein Gate, keine CI liest eine entfernte Basis. **Die Messdaten sind
endgültig weg** (rund 8 000 CSV-Dateien) — eine alte Zahl steht nur noch im Protokoll.
Erhalten sind die **Protokolle** aller 26 Basen samt der Tabelle Basis → Datum → Zweck →
Protokoll unter
[`Dokumentation/ueberholt/Referenzbasen/`](../Dokumentation/ueberholt/Referenzbasen/LIESMICH.md);
**welche Basis wann von welcher abgelöst wurde und warum**, steht ebendort — bis zum 12.09.2026
in der ausgelagerten
[Basenhistorie](../Dokumentation/ueberholt/Referenzbasen/LIESMICH_Basenhistorie_bis_2026-09-12.md),
danach im Wegweiser desselben Ordners.

## Aktuelle Basis

**`2026-09-18_R9_Kesselbrennstoff/`** — **dreizehn Projekte** (1007, 1008, 1017, 1018, 1023,
1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046), **357 CSV**, **2 057 Skalare**, gerechnet mit
dem plattformfreien `EPOS.Referenzlauf` auf Linux gegen `Kenndaten_Test.sqlite`
(**Schemastand 91** — die Basis ist unter Stand 89 eingefroren; die Testdatenbank steht auf
Stand 91, und die Schritte 90 und 91 sind ergebnisneutral, gemessen).
Schritt 90 räumt hinter Schritt 89 auf und hat **zwei Teile**. Der **DDL-Teil** entfernt aus
`Tab_ProjektWirtschaftlichkeit` die sechs KWKG-Spalten `KWKG_Bonus`,
`KWKG_Bonus_Einspeisung`, `KWKG_Vbh_Kontingent`, `KWKG_Vbh_Jahresdeckel`,
`KWKG_Tatbestand` und `KWKG_Anlagenart`; seit Schritt 89 und dem Umbau des Ersatzwegs auf
eine leistungsgewichtete virtuelle Gesamtanlage liest sie kein Rechenweg mehr.
`KWKG_Kostenanteil` bleibt samt Dialogfeld stehen (Anwenderentscheid), ebenso Stichtag,
Inbetriebnahme, Pauschalmodus und Abschlag Negativstunden. **Ergebnisneutral, gemessen
auf dem Ersatzweg** (Projekt 1030, Modulzuordnung absichtlich verstellt): Der KWK-Zuschlag
im Jahr 1 beträgt vor und nach dem Umbau **7.315,948722 €**, der Kapitalwert
**−21.895.377,339395 €**, und die volle Reihe t = 1…20 ist zahlengleich. **Eine Ausnahme
ist abgenommen:** Leert man das Vbh-Kontingent an Projekt UND Anlagen, rechnete der
Ersatzweg bisher still mit dem Feldvorgabewert 30.000 h und lieferte dieselben
7.315,948722 €; jetzt leitet er 0 h mit Begründung ab — 0,00 € Zuschlag und
−21.954.815,753214 € Kapitalwert. Der **DML-Teil** entfernt aus `Tab_ProjektWerte` die
Nullzeilen der drei nicht anlagenfähigen Erfassungsgruppen (Wärmezentrale, Bauliche
Anlagen, Stromeinspeisung): Hauptkomponentenzeilen der früheren Kostenmaske, Gruppe
„Allgemein", ausnahmslos 0,00. Getroffen hat er **elf Zeilen** — 1018 eine, 1019 sechs,
1031 eine, 1032 drei (Kategorien 1 und 2); eine Gruppe mit irgendeiner Position mit Wert
bleibt vollständig stehen. Keines der vier Projekte ist CI- oder Basisprojekt, und die
Basis führt keine Kostengröße — der Lauf ist auch nach Schritt 90 für alle fünf
CI-Projekte byte-gleich.
**Schritt 91** nimmt die siebte und letzte KWKG-Projektspalte: `KWKG_Kostenanteil` aus
`Tab_ProjektWirtschaftlichkeit`, samt ihrem Dialogfeld in Gruppe 2 des
BHKW-Wirtschaftlichkeitsdialogs. § 8 Abs. 2/3 KWKG leitet das Vbh-Kontingent aus dem
Kostenanteil **der Anlage** ab (`Tab_Energieanlagen.KWKG_Kostenanteil`, Schritt 89); der
Projektwert hatte seit dem Umbau des Ersatzwegs keinen Rechenleser mehr. **Kein DML** —
Schritt 89 hat den Wert einmalig in jede BHKW-Anlagenzeile übertragen, die dort leer war.
`Tab_ProjektWirtschaftlichkeit` führt danach noch vier KWKG-Spalten: `KWKG_Stichtag`,
`KWKG_Inbetriebnahme`, `KWKG_Abschlag_Negativ` und `KWKG_Pauschalmodus`.
**Ergebnisneutral, gemessen:** Projekt 1030 rechnet den KWK-Zuschlag im Jahr 1 unverändert
mit **7.315,948722 €** und den Kapitalwert mit **−21.895.377,339395 €**; die Basis führt
keine KWKG-Projektgröße — der Lauf ist auch nach Schritt 91 für alle fünf CI-Projekte
byte-gleich.
**Ohne eigenen Schritt** trägt die Testdatenbank zusätzlich die Spalte
`Nachweis_Json` an `Tab_ErgebnisWirtschaftlichkeit` — eine **Konservenspalte**: Diese
Ergebnistabelle ist keine Schematabelle, sie entsteht und wächst erst beim ersten
Programmlauf über `WirtschaftlichkeitCtrl.SpalteSicher`. Der SQL-Dialektprüfer löst das
INSERT des Ergebnisses aber gegen genau diese Datei auf und meldete ohne die Spalte eine
Fundstelle, die in der Anwendung keine ist; angelegt wird sie deshalb von
`Werkzeuge/Testdatenbankschema` (leer, kein Wert, ohne eigene `Zielversion`), und der Lauf
ist auch danach für alle fünf CI-Projekte byte-gleich. Gegen
diese Basis hält `.github/workflows/kern.yml` (1030, 1007, 1017,
1045, **1046**) jeden Push, `ios.yml` den iZ6-Vergleich für 1030; das Gate der Orchestrierung
zieht getrennt nach. Sie ist die **einzige** Basis im Arbeitsbaum.

> **Anlass: der Anwenderentscheid vom 18.09.2026, „Ja, neue Basis einfrieren".** Auftrag #331
> hat zum Befund **`B-1`** der Wirtschaftlichkeit den Brennstoffverbrauch des Heizkessels aus
> dem Simulationslauf in die **Modulzeile** nachgezogen: Die drei Spalten
> `HeizkesselModul[0].Verbrauch`, `.Waermeproduktion` und `.Brennstoff` trägt jetzt der Lauf,
> statt sie leer zu lassen. Herleitung, Rückfälle, Warnung und die benannte Ausnahme des
> Elektrokessels stehen im Protokoll
> [`B-1_Kesselbrennstoff_Modulzeile_Protokoll.md`](../Dokumentation/ueberholt/Protokolle/Reporting/B-1_Kesselbrennstoff_Modulzeile_Protokoll.md).
>
> **37 Abweichungen von 3 882 737 Werten — alle in denselben drei Spalten, keine unerklärte.**
> Elf Projekte tragen drei davon, die beiden Elektrokesselprojekte **1017** und **1024** nur die
> letzten beiden: Ihr Verbrauch bleibt 0, weil der Elektrokessel die benannte Ausnahme des
> Rechenwegs ist. 11 × 3 + 2 × 2 = 37.
>
> | Projekt | `…Brennstoff` | `…Verbrauch` [MWh/a] | `…Waermeproduktion` [MWh/a] |
> |---|---|---:|---:|
> | 1007, 1046 | → Gas | 0 → **15,47** | 0 → **6,13** |
> | 1008 | → Gas | 0 → **12,02** | 0 → **2,78** |
> | 1017 | → Strom | 0 (Ausnahme) | 0 → **8,91** |
> | 1018 | → Gas | 0 → **16,76** | 0 → **16,76** |
> | 1023 | → Gas | 0 → **78,64** | 0 → **66,61** |
> | 1024 | → Strom | 0 (Ausnahme) | 0 → **47,44** |
> | 1030 | → Gas | 0 → **5 403,10** | 0 → **5 403,10** |
> | 1039 | → Gas | 0 → **225,04** | 0 → **220,54** |
> | 1040, 1045 | → Gas | 0 → **16,19** | 0 → **16,19** |
> | 1041 | → Gas | 0 → **133,33** | 0 → **133,33** |
> | 1042 | → Gas | 0 → **13,81** | 0 → **13,53** |
>
> **Was byte-gleich geblieben ist:** die **344 Ganglinien-CSV** — keine Zeitreihe hat sich
> bewegt — und in den Skalaren jede andere Größe: Anlagensummen, Emissionen, Puffer,
> Speicherflotte, Wirtschaftlichkeit. Je `aggregate.csv` sind genau drei bzw. zwei Zeilen
> anders; keine kommt hinzu, keine fällt weg, die Skalarzahl bleibt Projekt für Projekt
> dieselbe.
>
> **Die Gegenprobe hält in allen dreizehn Projekten:**
> `HeizkesselModul[0].Verbrauch` = `Heizkessel.Gasverbrauch` und
> `HeizkesselModul[0].Waermeproduktion` = `Heizkessel.Waermeproduktion`. Der Modulwert ist
> nicht neu gerechnet, sondern der Anlagenwert des Laufs an der Stelle, an der die
> Wirtschaftlichkeit ihn liest.
>
> **Weder die Testdatenbank noch das Schema sind dafür angefasst worden**, und keine der drei
> Einfrierregeln ist berührt.
>
> **Determinismus geprüft:** zweiter Lauf desselben Standes **13/13 byte-gleich**,
> Laufzeit 00:00:06.
>
> ```bash
> dotnet run --project EPOS.Referenzlauf -c Release -- lauf \
>   --quelle Referenzlaeufe/Kenndaten_Test.sqlite \
>   --projekte 1007,1008,1017,1018,1023,1024,1030,1039,1040,1041,1042,1045,1046 \
>   --ziel Referenzlaeufe/2026-09-18_R9_Kesselbrennstoff
> ```
>
> Ablauf, Warnungen und Ausstattung je Projekt sowie der vollständige Vergleich R8 → R9 stehen
> im `protokoll.txt` der Basis.

> **Die Vorgängerbasis `2026-09-16_R8_Heizkessel_Kaskade`** ist mit dieser Einfrierung aus dem
> Arbeitsbaum gefallen; ihr Protokoll samt der Begründung zu `HK‑E‑1`, den Nachträgen zu den
> Schemaständen 83 bis 89 und dem Beleg zur Netzladung der Preissteuerung steht in
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
   `2026-09-18_R9_Kesselbrennstoff`, ist plattformfrei gegen `Kenndaten_Test.sqlite` gerechnet:
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
