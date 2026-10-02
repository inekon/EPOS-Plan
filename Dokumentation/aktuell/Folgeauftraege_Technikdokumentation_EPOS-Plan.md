# Folgeaufträge aus der Technikdokumentation

Die Arbeit an der Technikdokumentation im Wiki (Statuszeile #611, Protokoll
[`H14_Technikdokumentation_Protokoll.md`](../ueberholt/Protokolle/Hilfe/H14_Technikdokumentation_Protokoll.md))
hat Befunde in Code, Oberflächentexten und einem Werkzeug aufgedeckt. Sie sind hier als zehn
Aufträge abgelegt. Jeder Auftrag läuft in einer eigenen Sitzung, frühestens ab dem 29.09.2026.

Diese Liste liegt auf dem Zweig `claude/wiki-help-assistant-docs-jllq1r`. Jeder Auftrag steht so
da, dass er zusammen mit dem Vorspann ohne sie auskommt.

## So wird ein Auftrag gestartet

1. Eine neue Sitzung für das Repository `inekon/EPOS-Plan` öffnen, auf dem Arbeitszweig
   `ios_migration_september`.
2. Den Vorspann und den Auftrag einfügen.

**Vorspann (gilt für jeden Auftrag):**

> Repository `inekon/EPOS-Plan`, Arbeitszweig `ios_migration_september`. Es gelten die
> `CLAUDE.md` und die `CLAUDE.md` der betroffenen Projekte: Modellwahl, Git, Gate, Statuszeile
> und Protokoll.
>
> Jeden Befund zuerst am Code bestätigen. Trifft er nicht zu: kurz begründen und nichts ändern.
>
> Wiki-Quellen (`Projekte/Wiki/`, `EPOS.Kern/Allgemein/Hilfe/Berechnung/`) nicht anfassen. Die
> Technikdokumentation liegt auf dem Zweig `claude/wiki-help-assistant-docs-jllq1r` und wird dort
> nachgezogen. Die Statuszeile nennt dafür die Aussage, die sich für das Wiki ändert.
>
> Abnahme, wo nicht anders genannt:
> - `dotnet build WP-Plan.Kern.slnf -c Release`
> - betroffene und volle Tests mit `-- xUnit.ParallelizeTestCollections=false xUnit.MaxParallelThreads=2`
> - die Windows-Schale mit `-p:EnableWindowsTargeting=true`
> - `EPOS.Referenzlauf` gegen die aktuelle Basis
> - nach neuen Ressourcenschlüsseln `python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben`
>
> Texte der Oberfläche stehen in beiden Sprachen. Ändert sich sichtbares Verhalten, einen
> Logbuch-Satz vorschlagen und die Versionsnummer beim Anwender erfragen.

## Übersicht

| Nr. | Auftrag | Entscheid des Anwenders | Wiki danach nachziehen |
|---|---|---|---|
| 1 | Kühlkennlinie aus dem Katalog nachholbar machen | – | Kühlung, Gerätekataloge |
| 2 | PV-Dialog: Hinweistexte an den Rechenweg angleichen | nur zur Beschriftung „Standby-Verbrauch“ | Photovoltaik, Wechselrichter |
| 3 | Drei Nebenbefunde im PV-Rechenweg beheben | nur die Frage zum 5-kWh-Speicher | Simulationsergebnisse (Monatsbild) |
| 4 | Solarthermieganglinie: in die Rechnung oder entfernen | ja | Energieerzeuger, Solarthermie, Rechenweg Solarthermie |
| 5 | Kaskaden-Vorwahl: Kessel nicht vor die Wärmepumpe | ja | Wärmepumpe, Heizkessel, BHKW, Solarthermie, Pufferspeicher, Simulation, Kühlung |
| 6 | BHKW-Untergrenze wirkungslos, Einheit und Tippfehler | ja (Punkt 1) | BHKW, Gerätekataloge, Rechenweg BHKW |
| 7 | Wärmepumpe: Modulgrenze, CSV-Rückfall, Meldungstexte | ja (Punkte 1 und 2) | Rechenweg Wärmepumpe, Wärmepumpe |
| 8 | Tww-Einspielskript Python-versionsfest machen — **erledigt** (#592) | – | – |
| 9 | Klartext-Umsetzer des Assistenten: fehlende TeX-Befehle | – | – |
| 10 | Wache gegen harte Umbrüche und Formelzeichen außerhalb von `<math>` | – | – |

## 1. Kühlkennlinie aus dem Katalog nachholbar machen

**Kurz:** Hat eine Wärmepumpe im Projekt schon eine Wärmekennlinie, aber keine Kühlkennlinie,
gibt es vermutlich keinen Weg, die Kühlkennlinie aus dem Katalog zu übernehmen. Ohne sie bleibt
der Kühlbetrieb gesperrt.

**Befund** (noch nicht bestätigt):
- Der Kühlbetrieb ist gesperrt, solange das Projektgerät keine Kühlkennlinie hat
  (`EPOS.Kern/Controller/WPCtrl.cs` um Z. 432–456).
- Die Kühlkennlinie kommt aus dem VDI-3805-Import
  (`EPOS.Kern/Allgemein/Import/KatalogImportSatz.cs` um Z. 529–532).
- Das Protokoll zum Kühlbetrieb sagt, sie „lässt sich aus dem Katalog nachholen“.
- Der Knopf „Kennlinien aus dem Katalog übernehmen“ erscheint aber nur, wenn die
  **Wärme**kennlinie der Projektkopie fehlt: `WaermepumpeKennlinienCtrl.FuerAnlage`
  (`EPOS.Kern/Controller/WaermepumpeKennlinienCtrl.cs` um Z. 72–96) gibt die Projektkopie zurück,
  sobald sie Wärme-Vorläufe hat.

**Aufgabe:**
1. Den Befund am Code prüfen: den Weg des Knopfs in `EPOS.UI/Dialoge/Waermepumpe/`, die
   Kühlkennlinien in `Tab_Kenndaten_Kuehlung` und die Kopie vom Katalog ins Projekt.
2. Trifft er zu: die kleinste saubere Lösung im Kern-Controller mit Razor-Anschluss. Der Knopf
   erscheint auch, wenn nur die Kühlkennlinie fehlt und der Katalogsatz eine hat, und kopiert
   dann nur die Kühlkennlinie.
3. Tests in `EPOS.Kern.Tests` bzw. `EPOS.UI.Tests`.
4. Der Referenzlauf bleibt unverändert. Die Kühlkennlinien der Referenzprojekte 1017 und 1047
   bleiben unangetastet (Einfrierregeln der `CLAUDE.md`).

## 2. PV-Dialog: Hinweistexte an den Rechenweg angleichen

**Kurz:** Zwei Texte im PV-Dialog sagen etwas anderes, als die Rechnung tut.

**Aufgabe:**
1. **Hinweis zum Clipping** (`EPOS.UI/Dialoge/Erzeuger/PvStraengeFelder.razor` um Z. 1612–1614).
   - Die Zeile sagt auch im Rechenmodell „Erweitert“ „… und ohne Clipping“.
   - Im Modell „Erweitert“ rechnen die Anlagenwerte aber mit einer Wirkungsgradkennlinie aus
     drei Stützstellen und regeln ab, sobald eine AC-Nennleistung eingetragen ist
     (`EPOS.Kern/Allgemein/Simulation/SimulationPV.cs` um Z. 343–399,
     `PvErweitertesModell.cs` um Z. 134–149 und 170–186).
   - Im Modell „Einfach“ lassen sich dieselben Werte bearbeiten, wirken aber nicht, weil ein
     fester Wirkungsgrad gilt (`SimulationPV.cs` um Z. 244–245).
   - Der Hinweis soll je Modell stimmen. Mitprüfen: Sollen die wirkungslosen Felder im Modell
     „Einfach“ gesperrt oder als wirkungslos gekennzeichnet werden?
2. **Text `PVS_PFLEGEWEG`** (`EPOS.Kern/MyResource/Resource.resx`, de/en): Er nennt
   Eingabefelder für die Temperaturkoeffizienten α_SC und β_OC. Die Verwaltung der PV-Module hat
   dafür kein Feld (`EPOS.Kern/Allgemein/Katalog/ModulKatalogProfil.cs` um Z. 399–428,
   `ParameterVerwendung.cs` um Z. 569–578). Der Text soll den wirklichen Pflegeweg nennen:
   Import bzw. Katalog.
3. **Nur nach Rückfrage beim Anwender:** Das Katalogfeld „Standby-Verbrauch“ (CEC `Pso`,
   OND `PSeuil`) wirkt in der Rechnung als Einschaltschwelle und Nachtverbrauch
   (`EPOS.Kern/Allgemein/Simulation/PvStrangModell.cs` um Z. 201–202 und 399–406). Wäre eine
   klarere Beschriftung sinnvoll?

**Abnahme:** wie im Vorspann. Der Referenzlauf bleibt unverändert, denn es sind nur Texte.

## 3. Drei Nebenbefunde im PV-Rechenweg beheben

**Aufgabe:**
1. **Zusammengesetzte SQL-Texte** in `EPOS.Kern/Allgemein/Simulation/SimulationControl.cs` um
   Z. 4383 und 4639 sowie in `SimulationPV.cs` um Z. 166, 171 und 175.
   - Die Hausregel verlangt Zugriffe über `DataRepository` mit `?`-Parametern
     (`Dokumentation/aktuell/BETRIEB_SQLITE.md`, Abschnitt 6).
   - Umstellen, danach
     `python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite`.
2. **Interne Kürzel in Anwendertexten** (`EPOS.Kern/MyResource/Resource.resx`, de/en):
   - `SIM_PV_V1_BHKW_GETRENNT` enthält „(Befund V1)“.
   - Die Erläuterung „Fest: …“ der Stromspeicher-Auslegung (um Z. 19432) enthält
     „(Befund vom 11.09.2026)“.
   - Die Kürzel entfernen und die Aussage behalten.
   - Weitere sichtbare Texte nach solchen Kürzeln durchsuchen: `Befund`, `W\d+`, Auftragsnummern.
3. **Monatsbild der Autarkie Analyse**
   (`EPOS.UI.Daten/Simulation/SimulationErgebnisHuelle.Bilder.cs` um Z. 784–797): Es teilt das
   Jahr offenbar in zwölf gleich lange Blöcke. Wenn das zutrifft, auf Kalendermonate umstellen.
   Der Kern rechnet mit 365 Tagen und 12 Monaten ohne Schaltjahr.

**Nur fragen, nicht ändern:** Ohne Stromspeicher im Projekt rechnet die Autarkie Analyse mit
einem angenommenen Speicher von 5 kWh (`EPOS.Kern/Controller/StromspeicherStammCtrl.cs` um Z. 347
und 492–514). Soll das so bleiben?

**Abnahme:**
- wie im Vorspann, dazu `dotnet run --project Proben/ChartProben -c Release`, falls das Bild in
  den Proben steckt;
- Referenzlauf der Projekte 1030, 1007, 1017, 1045, 1046, 1047 und 1049: Punkt 1 darf nichts
  ändern.

## 4. Solarthermieganglinie: in die Rechnung oder entfernen

**Kurz:** Die Kachel Solarthermie bietet „Ganglinie“ an, und ihr Statuspunkt wird grün. Die
Simulation liest die Ganglinie aber nicht.

**Befund:**
- Der Reiter „Energieerzeuger“ der Startseite bietet bei der Kachel Solarthermie die Wahl
  „Profil“ oder „Ganglinie“ (`EPOS.UI/Seiten/Start/ErzeugerReiter.razor` um Z. 8–20).
- „Ganglinie“ ordnet dem Projekt eine Solarthermieganglinie zu
  (`EPOS.Kern/Controller/Z_ProjektSolarganglinieCtrl.cs`). Der Statuspunkt wird dann grün
  (`EPOS.Kern/Controller/KomponentenBestandCtrl.cs` um Z. 200 und 250).
- Außer `KomponentenBestandCtrl` verwendet kein Code die Ganglinie. Der Ertrag kommt immer aus
  Klimadaten und Kollektorfeld (`EPOS.Kern/Allgemein/Simulation/SimulationSolarthermie.cs`).

**Aufgabe:**
1. Den Befund bestätigen, auch für die iOS-Schale und den Bericht.
2. Zwei Wege mit Aufwand, Risiko und Wirkung auf den Referenzlauf darstellen:
   - (a) die Ganglinie als eigener Rechenweg statt des Kollektorfelds, mit Senken, Pufferladung
     und Kaskade wie beim Kollektorfeld;
   - (b) die Wahl „Ganglinie“ entfernen oder deutlich als nicht rechnend kennzeichnen.
3. **Entscheid des Anwenders** einholen, dann umsetzen mit Tests. Das Referenzprojekt 1049
   rechnet ein Kollektorfeld; die Einfrierregeln gelten.
4. Mitprüfen: Der KI-Erklärtext `KI_DLG_PSPV_SCHWELLE_NACHRANG_ERL` beschreibt die
   Nachrang-Schwelle des Pufferspeichers als Startschwelle. Sie ist aber die Obergrenze, bis zu
   der nachrangige Erzeuger laden (`EPOS.Kern/Allgemein/Simulation/Ladeordnung.cs` um Z. 93–104
   und 518–590). In beiden Sprachen richtigstellen.

## 5. Kaskaden-Vorwahl: Kessel nicht vor die Wärmepumpe

**Kurz:** Neue Projekte bekommen die Kaskade BHKW, Heizkessel, Solarthermie, Wärmepumpe
vorgeschlagen. Wer ohne Umordnen speichert, lässt in einer bivalenten Anlage den Kessel vor der
Wärmepumpe decken. Eine Solarthermie hinter BHKW und Kesseln deckt dann kaum etwas.

**Befund:**
- Ist die Kaskade nie gespeichert, füllt die Konfigurationsseite die freien Plätze in der festen
  Folge BHKW, Heizkessel, Solarthermie, Wärmepumpe
  (`EPOS.UI.Daten/Simulation/SimulationKonfigHuelle.cs` um Z. 172–199, `ErzeugerKatalog.cs` um
  Z. 42–48). Erst „Konfiguration speichern“ schreibt sie (um Z. 1919–1976).
- Das widerspricht der Ladeprio-Vorgabe: Solarthermie 10, Wärmepumpe 20, BHKW 30, Kessel 40
  (`EPOS.Kern/Allgemein/Simulation/Ladeordnung.cs` um Z. 25–40).
- Es widerspricht auch dem Nachziehen des Kessels ans Ende bei gespeicherter, nicht gepflegter
  Kaskade (`EPOS.Kern/Controller/KonfigurationCtrl.cs` um Z. 243 und 315–344, `Kaskade.cs` um
  Z. 138–161).
- Der Test `EPOS.Kern.Tests/KuehlbetriebProgrammeinstellungTests.cs` (um Z. 320–330) hält die
  Folge [BHKW, Heizkessel] fest.

**Aufgabe:**
1. Den Befund bestätigen.
2. Dem Anwender zwei Varianten zur Entscheidung vorlegen:
   - (a) Vorwahl in Ladeprio-Folge: Solarthermie, Wärmepumpe, BHKW, Heizkessel;
   - (b) nur den Kessel stets ans Ende.

   Betroffen sind nur nie gespeicherte Kaskaden, gespeicherte und gepflegte nicht. Mit dem
   Referenzlauf nachweisen, dass sich nichts ändert; die Referenzprojekte haben gespeicherte
   Kaskaden.
3. Nach dem Entscheid umsetzen und die Tests anpassen bzw. ergänzen.

**Wiki:** Der Fallstrick „Kessel vor der Wärmepumpe in neuen Projekten“ und die Vorwahl stehen
auf den Seiten Wärmepumpe, Heizkessel, BHKW, Solarthermie, Pufferspeicher, Simulation und
Kühlung.

## 6. BHKW-Untergrenze wirkungslos, Einheit und Tippfehler

**Aufgabe:**
1. **Das Feld „Untere Grenzleistung des ausgewählten Moduls“ wirkt nicht.**
   - Die Untergrenze kommt aus der Katalogkopie, wenn sie größer als 0 ist, sonst aus dem
     projektweiten Wert (`EPOS.Kern/Allgemein/Simulation/SimulationBHKW.cs` um Z. 333–357).
   - Das Anlagenfeld wird geladen (`SimulationControl.cs` um Z. 1749–1751) und danach immer
     überschrieben.
   - „Stromgeführt“ und „ohne Einspeisung“ nehmen für alle Module den projektweiten Wert
     (`SimulationBHKW.cs` um Z. 1775–1793).
   - Dem Anwender vorlegen: das Feld wirksam machen oder entfernen? Soll die Untergrenze von der
     Betriebsart abhängen? Erst nach dem Entscheid ändern; den Referenzlauf beachten.
2. **Einheit:** `EPOS.Kern/Allgemein/Katalog/KatalogBrowserProfil.cs` um Z. 424 zeigt den
   Bereitschaftsverlust des Kessels in „%“. Gerechnet und in der Maske geführt wird er in kW
   (`SimulationSPK.cs` um Z. 53–60, `EPOS.UI/Dialoge/Erzeuger/HeizkesselKatalogDialog.razor` um
   Z. 110). Die Einheit berichtigen.
3. **Tippfehler** in der Ressource `SIMERG_INFO_BHKW`: „im im“ und „solangen“. Beide Sprachen
   prüfen.
4. **Nur berichten:** `Referenzlaeufe/Kenndaten_Test.sqlite` führt BHKW-Grenzleistungen von
   468, 620, 770 und 1 027 %. Eine Änderung der Testdatenbank braucht den Anwender und
   gegebenenfalls eine neu eingefrorene Basis.

## 7. Wärmepumpe: Modulgrenze, CSV-Rückfall, Meldungstexte

**Aufgabe:**
1. **Die Grenze liegt faktisch bei 9 statt 10 Modulen, und der Lauf bricht ohne Meldung ab.**
   - `MAX_WP = 10` (`EPOS.Kern/Allgemein/Simulation/SimulationWaermepumpe.cs` um Z. 11).
   - Abgelehnt wird aber schon ab `wp_list.Count >= 10` (um Z. 995). Die Liste füllt
     `SimulationControl.cs` um Z. 1635–1653 mit allen Wärmepumpen des Projekts.
   - `Vorbereiten_Zweikanalig` gibt dabei `false` ohne Fehlertext zurück.
     `FehlertextAufnehmen` (`SimulationControl.cs` um Z. 310) verwirft den leeren Text, der Lauf
     bricht also ohne benannten Grund ab.
   - Dem Anwender vorlegen: Sollen 9 oder 10 Module gelten? In jedem Fall eine benannte Meldung
     (de/en), mit Test.
2. **Stiller Rückfall der CSV-Quelle:** Fehlt die CSV-Datei einer Wärmequelle oder ist sie
   unbrauchbar, rechnet der Lauf ohne Meldung mit Außenluft
   (`EPOS.Kern/Allgemein/Simulation/WaermequelleClass.cs` um Z. 677–681). Das Quellprofil meldet
   dagegen (um Z. 655–663). Vorschlag: eine Meldung im Simulationsprotokoll. Dem Anwender
   vorlegen.
3. **Veralteter Meldungstext `SIMQ_MSG_LUFT_WASSER`** (`Resource.resx` um Z. 3613–3620): Er nennt
   den Weg „Administration → Wärmepumpe → Wärmepumpentyp ändern, dann die WP im Projekt neu
   auswählen“. Den heutigen Weg aus `EPOS.UI/Bausteine/Menuetabelle.cs` und den Dialogen
   ermitteln und einsetzen, in beiden Sprachen.
4. **Das Etikett „Quellprofil (Monatswerte)“** (`Resource.resx` um Z. 3820) ist zu eng: Das
   Profil nimmt 12, 365 oder 8 760 Werte (`WaermequelleClass.cs` um Z. 646).
5. **Tippfehler „Wärmproduktion“** in `SIMERG_TAB_WP_PRODUKTION` (`Resource.resx` um Z. 14589).

**Abnahme:** wie im Vorspann. Punkt 1 kann im Referenzlauf nur Projekte mit zehn Wärmepumpen
betreffen.

## 8. Tww-Einspielskript Python-versionsfest machen

**Erledigt** mit Statuszeile #592: Das Skript normiert jetzt unabhängig von der Python-Fassung; Testdatenbank, Paketteil und Referenzlauf sind unverändert.

**Kurz:** Der Test `EPOS.Kern.Tests/TwwKatalogWacheTests.cs`,
`Das_Einspielskript_ist_wiederholbar`, ist unter Python 3.11 rot und ab 3.12 grün.

**Befund:**
- `Referenzlaeufe/Skripte/tww_testkatalog_fiktiv.py` vergleicht die eingecheckten Träger-CSVs
  zeilenweise mit seinem Erzeugnis (`traeger_pruefen_oder_schreiben`).
- Unter Python 3.11 weicht die letzte Stelle ab, zum Beispiel in `Tab_TwwNutzungsart_STAMM.csv`:
  `1.122` gegenüber `1.1220000000000003`.
- Grund ist die Normierung über `sum()` (um Z. 244). CPython bis 3.11 summiert naiv, ab 3.12 mit
  Kompensation. Die eingecheckten CSVs entsprechen dem Erzeugnis von 3.12.

**Aufgabe:**
1. Die Normierung mit `math.fsum` oder einer festen Rundung machen.
2. Die eingecheckten CSVs dürfen sich nicht ändern. Ändern sie sich doch, gelten die
   Einfrierregeln, und der Anwender muss zustimmen.
3. Eine Prüfung ergänzen, dass beide Summenwege dasselbe liefern.

**Abnahme:** `--filter "FullyQualifiedName~TwwKatalogWache"` grün unter der vorhandenen
Python-Fassung.

## 9. Klartext-Umsetzer des Assistenten: fehlende TeX-Befehle

**Kurz:** Vorhandene Formeln der Rechenwegseiten benutzen TeX-Befehle, die der Klartext-Umsetzer des
Assistenten nicht kennt. Der Assistent liest sie dann als nacktes Wort, etwa „max“ oder „theta“.

**Befund:**
- In den Formeln stehen `\max`, `\min`, `\theta`, `\cos`, `\sin`, `\ln`, `\chi`, `\circ`, `\dfrac`,
  `\lceil`/`\rceil`/`\lfloor`/`\rfloor`, `\Big`, `\qquad`, `\leq`/`\geq` und `\dot`.
- Die Wache `ErlaubteBefehle` in `EPOS.Kern.Tests/BerechnungsHilfeTests.cs` erlaubt sie.
  `BerechnungsHilfe.LatexKlartext` (`EPOS.Kern/Allgemein/Hilfe/BerechnungsHilfe.cs`, Tabellen
  `BefehlsZeichen` und `BuchstabenZeichen`) setzt sie aber nicht um.
- `\times` ist weder erlaubt noch umgesetzt.

**Aufgabe:**
1. `LatexKlartext` um diese Befehle ergänzen:
   - lesbare Zeichen oder Wörter: `\theta` → θ, `\circ` → °, `\leq`/`\geq` → ≤/≥, `\max` → max,
     `\cos` → cos, `\dot{x}` → ẋ, `\lceil…\rceil` → ⌈…⌉;
   - `\dfrac` wie `\frac`; `\Big` und `\qquad` fallen weg;
   - `\times` → ×, und `\times` in `ErlaubteBefehle` aufnehmen.
2. Tests:
   - je Befehl ein Fall;
   - eine Wache, dass jeder Befehl aller eingebetteten Rechenwegseiten in `LatexKlartext` bekannt ist.

**Abnahme:** wie im Vorspann; der Referenzlauf ist nicht betroffen.

## 10. Wache gegen harte Umbrüche und Formelzeichen außerhalb von `<math>`

**Kurz:** Die Wiki-Quellen waren hart umbrochen, und MediaWiki machte daraus graue `<pre>`-Kästen und
zerrissene Listen. Die Korrektur vom 28.09.2026 hat das behoben (Konzept Hilfesystem 13.5). Eine Wache
soll es künftig abfangen.

**Aufgabe:**
1. Ein Test in `EPOS.Kern.Tests` (Vorbild `WikiProduktdatenWacheTests`) über `Projekte/Wiki/*.wiki`
   und `EPOS.Kern/Allgemein/Hilfe/Berechnung/*.wiki`, ohne `_Bezuege.wiki`. Er prüft:
   - keine eingerückte Folgezeile hinter einer Text-, Listen- oder Tabellenzeile. Ausgenommen sind
     Kommentare, `<math>`, `<pre>`, `<syntaxhighlight>` und `<nowiki>`; eine eingerückte Zeile nach
     einer Leerzeile ist ein gewollter Kasten;
   - keine nicht eingerückte Folgezeile hinter einem Listenpunkt;
   - kein Formelzeichen in HTML-Tiefstellung außerhalb von `<math>`; Einheiten wie kW<sub>el</sub>
     und chemische Formeln sind ausgenommen.
2. Die Regeln der ersten beiden Punkte setzt das Werkzeug `Werkzeuge/WikiUpload/entfalten.py` um;
   die Wache übernimmt dieselbe Abgrenzung.
3. Gegenprobe: Die Wache wird auf einer absichtlich umbrochenen Beispielquelle rot.

**Abnahme:** gefilterte und volle Tests.

## Weitere offene Punkte ohne eigenen Auftrag

- **Wechselrichter:** Die Projektkopie eines Wechselrichters sieht spätere Ergänzungen im
  Katalog nicht. Soll es „aus dem Katalog erneuern“ geben?
- **Pufferspeicher:** Projekte ohne Wärmepumpe sehen keine Jahreswerte des Puffers, denn die
  Speichertabelle steht nur im Reiter Wärmepumpe. Ist das gewollt?
- **iOS, optional:** die Adressen der Grundlagenseiten je Pfadsegment kodieren
  (`Uri.EscapeDataString` in `EPOS.iOS/Dienste/IosHilfeDienst.cs`, Methode `Adresse`). Das ist
  eine Härtung, kein Fehler: iOS ab 17 kodiert die Adresse selbst.

## Nach der Umsetzung

- Jede Sitzung schreibt ihre Statuszeile und ihr Protokoll nach der `CLAUDE.md`.
- Die Dokumentationssitzung zieht die Seiten aus der Spalte „Wiki danach nachziehen“ nach. Die
  Routine vom 02.10.2026 prüft das. Hochgeladen wird mit dem nächsten Wochen-Upload.
- Sind alle zehn Aufträge erledigt, wandert diese Liste nach `Dokumentation/ueberholt/`.
