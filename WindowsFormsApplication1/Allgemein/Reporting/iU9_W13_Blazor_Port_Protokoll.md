# iU9 — Welle 13: Katalog-Importe VDI 3805, Wärmebedarf-Ganglinien, CEC/PAN

**Stand 04.09.2026.** Vermessung: `iU9_W13_Vermessung.md` (1 815 Zeilen, Stand
`81a04ec`) mit den Befunden W13‑B1 … W13‑B55. Basis dieser Welle: `08c489a`
(`ios_migration` nach W12).

Vorbilder für Form und Tiefe: `iU9_W12_Blazor_Port_Protokoll.md`,
`iU9_W11b_Blazor_Port_Protokoll.md`.

---

## 1. Auftrag und Ergebnis

**Sechs WinForms-Masken — 2 396 Zeilen `.cs`, 2 621 Zeilen Designer, 32
`MessageBox` und vier indirekte über `Program.ZahlPruefen` — sind DREI
Razor-Komponenten.** Jede WinForms-Fassung ist gelöscht (Regel M1), alle sechs
im selben Commit wie ihr Nachfolger.

| Nr | Komponente (`EPOS.UI/`) | ersetzt | Windows-Hülle |
|---|---|---|---|
| W13.1 | `Dialoge/Import/KatalogImportDialog` — **vier Ausprägungen** | `Form_Heizkessel_einlesen` (500 + 323), `Form_PufferSp_einlesen` (383 + 270), `Form_SolarKollektoren_einlesen` (264 + 418), `Form_WP_einlesen` (424 + 385) | `Views/Import/KatalogImportHuelle.cs` (eine für alle vier) |
| W13.2 | `Dialoge/Bedarf/WaermebedarfAdminDialog` | `Form_AdminWaermeeinlesen` (167 + 148) | `Views/Wärmebedarf/WaermebedarfAdminHuelle.cs` |
| W13.3 | `Dialoge/Photovoltaik/PvModulImportDialog` | `Form_CECImport` / Klasse `Main_PV_Test` (658 + 1 077) | `Views/Photovoltaik/PvModulImportHuelle.cs` |
| W13.4 | — (Aufräumen) | `Views/Import/ImportKonflikteHuelle.cs` (W12, Lebensdauer eine Welle) | — |

**Der rote Faden ist der vierfach abgeschriebene Bauplan.** Die vier
VDI-3805-Einlesemasken tragen dreizehn Bausteine wortgleich — dieselben
Kommentare, bis hin zum falschen Handlernamen
`Liste_WP_SelectedIndexChanged` in DREI von vier (Befund W13‑B15). Sie sind
**eine** Komponente mit vier Ausprägungen geworden; was sie trennt, sind sieben
Werte und steht als `KatalogImportProfil` im Kern.

**Der Nachweis der Welle ist der bitgleiche Import.** Dafür gab es für die fünf
Parser, für `DublettenPruefung` und für `VdiAuswahlFilter` keinen einzigen Test
(Befund W13‑B1). Die zwanzig Proben und ihre eingefrorenen Erwartungswerte sind
deshalb der **erste** Schritt der Welle, nicht der letzte.

### Commits (7, auf `08c489a`)

| Commit | Schritt |
|---|---|
| `0711916` | W13.0i — zwanzig Importproben und der eingefrorene Nachweis der fünf Parser |
| `5c52e85` | W13.0l — die Mehrfachmarkierung im Raster (die eine Bausteinlücke) |
| `9ea6d81` | W13.0a–h — der Katalogimport als EIN Kern-Ablauf mit vier Ausprägungen |
| `30dae47` | W13.1 — die vier VDI-3805-Katalogimporte als EINE Komponente |
| `eb83c00` | W13.2 — die Wärmebedarfs-Ganglinienverwaltung, und die Sprungbrücke fällt |
| `5ba5215` | W13.0j/0k/3/4 — der PV-Modulimport, und die letzte Maske der Welle fällt |
| `56578aa` | W13.6 — Formularkarte nachgezogen: 38 Masken werden 32 |

W13.5 (Ressourcen) ist in den Schritten selbst gelaufen — jede Komponente
brachte ihre Texte mit; W13.7 (Protokoll, drei CLAUDE.md, H7-Protokoll) steht im
Abschlusscommit dieser Datei.

---

## 2. Die drei Entscheidungen der Welle

### 2.1 EINE Komponente mit vier Ausprägungen — nicht vier Komponenten

Der Bestand **ist** bereits eine Komponente mit vier Ausprägungen, nur vierfach
abgeschrieben. Vier Razor-Komponenten zu bauen hieße, denselben Fehler ein
zweites Mal zu machen — diesmal in einer Technik, die Parametrisierung billig
macht. Das Muster ist im Haus erprobt und benannt: W8 hat zehn Masken auf vier
Komponenten gebracht, W9 acht auf fünf; die Ausprägung ist dort wie hier ein
**Aufzählungstyp im Kern** (`BedarfsArt` damals, `KatalogImportArt` jetzt), weil
ihn beide Seiten brauchen — der Ablauf wählt danach Parser und Schreibweg, die
Komponente ihre Filtergröße und ihre Detailfelder.

Die sieben echten Unterschiede sind **Werte**, kein Verhalten: Katalogschlüssel,
Unterordner, Dateifilter, Filtergröße samt Vorbelegung, Detailfeldliste,
Vergleichswerte, Schreibweg. Sie stehen in `KatalogImportProfil`.

**Der Feldkartenabgleich läuft je AUSPRÄGUNG** (§ 4), nicht je Komponente — vier
Karten, vier Feldbestandsfälle. Der Nachweis verliert nichts.

### 2.2 Angleichen, nicht bitgleich nachbauen

§ 7.2 der Vermessung zählt fünf Stellen, an denen die vier Vierlinge
auseinanderlaufen, ohne dass ein Grund erkennbar wäre. Eine gemeinsame
Komponente **kann** sie nicht alle bitgleich nachbauen — sie müsste je
Ausprägung einen Sonderzweig führen, und genau das soll die Zusammenlegung
beenden.

Deshalb: **angeglichen**, je Angleichung eine A‑Zeile (§ 5) **und** ein Punkt in
der Windows-Abnahme (§ 10). Der Referenzlauf sieht davon nichts — er rechnet
einen bestehenden Projektstand nach und kennt keinen Katalogimport
(Risiko R‑W13‑6).

**Bitgleich geblieben sind:** alle Filtervorbelegungen (10/200, 0/1000, 0/5,
0/100) und Nachkommastellen (1/0/2/0), sämtliche Meldungstexte, die Reihenfolge
Vorprüfung → Konfliktdialog → Ausführung → EINE Sammelmeldung, die Regeltabelle
des Konfliktdialogs (W12) und **die Parserergebnisse Zeichen für Zeichen**
(§ 8.3).

### 2.3 Die Sprungbrücke fällt für ein Ziel, das selbst Blazor wird

`WaermebedarfExternDialog` (W9.4) sprang über `Sprungziel.WaermebedarfExternAdmin`
in ein WinForms-Fenster. Ist das Ziel selbst Blazor, wären zwei WebViews
übereinander Risiko R2. Der Dialog bekommt jetzt den **Parametersatz** der
Verwaltung und zeigt sie als `Ueberlagerung` im selben Fenster — dasselbe
Muster, das W4 für die neun Unterdialoge der Kostenseite gefahren hat.
`Sprungziel.WaermebedarfExternAdmin` und der `case` in `Sprungbruecke` sind
gelöscht; die Sprungziele sind von zehn auf **neun** gefallen.

---

## 3. Bauweise

### 3.1 Der Katalogimport als ein Kern-Ablauf

`KatalogImportAblauf` (W13.0b) hat vier Schritte: `Lesen`, `Anzeigeindex`,
`Vorpruefen`, `Ausfuehren`. Sie standen viermal wortgleich im Formularcode
(`btn_VDI3805_Click`, `FuelleListe`, der Vorprüfblock von
`btn_Uebernehmen_Click`, `FuehreAus`) — je Maske rund 200 Zeilen, in denen sich
nur Feldnamen und der Katalogschlüssel unterschieden.

**Er zeigt nichts an** — dieselbe Regel wie `GanglinienImportAblauf` aus W12.
Der Konfliktdialog ist hier aber **kein Rückruf**, sondern eine Zäsur: Der Wirt
ruft `Vorpruefen`, zeigt bei Bedarf seine Überlagerung und ruft dann
`Ausfuehren`. So bleibt der Fadenwechsel auf zwei klare Stellen beschränkt
statt mitten in einer Schleife zu sitzen.

### 3.2 Was in den Kern gezogen ist

| Datei (neu bzw. erweitert) | Herkunft | Grund |
|---|---|---|
| `Allgemein/Import/KatalogImportProfil.cs` | § 7.2 der Vermessung | die Ausprägung als DATEN, nicht als Sonderzweig (**B3**) |
| `Allgemein/Import/KatalogImportAblauf.cs` | vier × `FuelleListe`/`FuehreAus`/Vorprüfblock | **B3**, **B14** |
| `Allgemein/Import/KatalogImportSatz.cs` | vier × `FuelleModellwerte` bzw. `InitDatensatzUpdate` | Fachrechnung im Formularcode (**B17**, **B30**–**B32**) |
| `Allgemein/Import/GanglinienTextDatei.cs` | `ToolsClass.OpenText` | ein Parser mit DIALOG darin (**B11**) |
| `Allgemein/ZahlText.NachDouble`/`NachInt` | `Program.convertTxt2Double`/`-Int` | `Program.*` ist im Kern verboten (iU5-Wächter) |
| `WaermepumpenImport.KennlinienZu` | `Form_WP_einlesen.SammleKennlinien` :359‑404 | **Parsercode im Formular** (**B34**) |
| `WaermepumpenImport.Meldungen` | — | ein Kanal für das, was beim Lesen auffällt (**B35**) |
| `HeizkesselStammCtrl.ImportUebernehmen` | `Form_Heizkessel_einlesen.Insert(model, v)` :403‑444 | der einzige Schreibweg der Welle, der nicht im Kern lag (**B16**) |
| `PufferSpStammCtrl.ImportUebernehmen`, `SolarkollektorenStammCtrl.ImportUebernehmen` | `UebernehmeEintrag` je Maske | Prüfung und Schreiben geklammert (Konzept 6.3) |
| `WPStammCtrl.ImportMitKennlinien` | `Form_WP_einlesen.UebernehmeEintrag` :308‑354 | drei Tabellen in EINER Transaktion statt Aufräumklammer (**B33**) |
| `UnifiedModule` (Quellenweiche, `NachModell`, `Vergleichswerte`) | `Form_CECImport.ShowDetail`/`InitDatensatzUpdate` | dreizehn Ternäre und die Modellabbildung im Anzeigecode |
| `PanModule.PtcGeschaetzt` | `ShowDetail` :431‑437 | Fachwissen im Anzeigecode (**B43**) |
| `PVModule.Bifazial` | `UnifiedModule` :54, `AddPVModul` :112 | deutsche ANZEIGETEXTE im Kern (**B50**) |
| `Masken.PvImport` | — | die einzige Maske der Welle ohne Maskenschlüssel (**B55**) |

**Gelöscht im Kern:** `CECDataService.Filter(...)` und `BuildWildcardMatcher` —
die dritte Platzhaltersuche des Bestands, ohne Aufrufer (**B41**). Mit ihnen
fällt der Steuerwert `"(alle)"` aus dem Kern (**B39**).

**Nicht gelöscht:** `EPOS.Kern/Allgemein/ToolsClass.cs`. Sie hat einen zweiten
Nutzer — `Form_Solarganglinie_Admin` — bis Welle 14b. Nur der Aufruf aus dem
Wärmebedarf ist abgelöst.

### 3.3 Lesen und Schreiben laufen nebenher

Die größte Probendatei des Bestands hat **92 376 Zeilen / 8,3 MB**
(`PART22_Wolf…20250521`), die CEC-Modulliste **20 746 Zeilen**, und drei
Netz-URLs mit je 45 Sekunden Zeitgrenze sind im schlechtesten Fall über zwei
Minuten. **In einer WebView ist der Renderfaden der Bedienfaden.** Alle drei
Wege laufen deshalb in `Task.Run` und melden über `IProgress`; alle drei lassen
sich abbrechen:

| Weg | Melder | Abbruch |
|---|---|---|
| VDI-Datei lesen (`KatalogImportHuelle.Lesen`) | `ImportFortschritt` | ja |
| Übernahme ausführen (`…Ausfuehren`) | `ImportFortschritt` je Eintrag | ja |
| CEC-Netzabruf (`PvModulImportHuelle.CecLaden`) | `CecFortschritt` | ja (**B38**, R‑W13‑3) |
| Wärmebedarfsdatei (`WaermebedarfAdminHuelle.Einlesen`) | `ImportFortschritt` | nein — 8 760 Zeilen sind schnell gelesen, und die Transaktion bricht man besser nicht |

Der Baustein `Fortschritt` hängt daran, samt Abbrechen-Knopf — und dort, wo es
keinen Abbruch gibt, ohne Knopf (Regel des Bausteins, iU9‑W11a.7).

### 3.4 Die Bausteinlücke: Mehrfachmarkierung im Raster

`Mehrfachauswahl.razor` rendert **jede** Zeile als Schalter — für 190
Katalogsätze noch tragbar, für 20 746 CEC-Zeilen nicht. `Raster` kennt keine
Markierung, `Zeilenwahl` war einwertig. Deshalb vor W13.1 (Risiko R‑W13‑4):

- **`Zeilenwahl`** bekommt `Mehrfach` (Kontrollkästchen statt Optionsknopf,
  `role="checkbox"`) und `Tastenwahl`, das `Strg`/`Umschalt` mitmeldet.
- **`Zeilenmarkierung`** (neu, `EPOS.UI/Bausteine/`) trägt die REGEL: Klick
  wählt eine, `Strg` nimmt dazu oder weg, `Umschalt` wählt den Bereich ab dem
  Anker — der bleibt stehen, damit ein zweiter Umschalt-Klick den Bereich auch
  wieder verkleinert. `AufAnzahlBegrenzen` wirft nach einem Filterwechsel
  hinaus, was hinter der neuen Liste liegt; `QuellIndizes` bildet auf die
  Importliste ab (Zwilling von `VdiAuswahlFilter.QuellIndizes`).
- **`Raster`** bekommt `Virtualisiert` und `Zeilenhoehe`. Ein `IQueryable`
  allein virtualisiert **nichts** — QuickGrid zeichnet ohne `Virtualize` jede
  Zeile. Die Hülle trägt dann `epos-raster-huelle--hoch`: feste Höhe, stehender
  Spaltenkopf; ohne einen Behälter zum Rollen gäbe es nichts auszulassen.

Beide Importkomponenten virtualisieren **ab 120 Zeilen** — darunter kostet es
mehr, als es bringt, und der feste Behälter stünde halb leer.

---

## 4. Feldkarten-Abgleich

Die Feldkarte wurde für jede der fünf Masken mit Designer **neu gezogen**
(`dotnet run --project Werkzeuge/Formularkarte -- <Designer.cs>`).
`Form_CECImport` hat eine leere `.resx`; ihr Abgleich läuft gegen Designer und
Quelltext.

| Maske | Kartenzeilen | in der Komponente |
|---|---|---|
| `Form_Heizkessel_einlesen` | **17** | Dateiwahl, 2 Zahlenfelder + Suchfeld, Raster mit Wahlspalte, **7** Detailfelder, Speichern, OK |
| `Form_PufferSp_einlesen` | **14** | dieselbe Komponente, **5** Detailfelder, Volumenfilter |
| `Form_SolarKollektoren_einlesen` | **11 + 11** (Gruppenrahmen „Eigenschaften") | dieselbe Komponente, **11** Detailfelder, Aperturfilter |
| `Form_WP_einlesen` | **34** | dieselbe Komponente, **10** Detailfelder, Leistungsfilter, Hinweis „* 0=modulierend" |
| `Form_AdminWaermeeinlesen` | **11** | Katalograster, Ordnerfeld, Dateiwahl, „Inhalt anzeigen…", „Datei in DB Einlesen…", „DB Ganglinie Löschen", „Beenden" |
| `Form_CECImport` | **75** (gegen den Designer) | zwei Quellenknöpfe, Statuszeile, 2 Auswahlfelder + Suche, 4 Zahlenfelder, Zurücksetzen, **10** Gitterspalten, 3 `Reiterblatt` mit **21** Textfeldern, Übernehmen, Schließen |

**Kein Feld ist verlorengegangen.** Was ersatzlos entfällt, steht in § 5.

**Ein Feld ist DAZUGEKOMMEN:** Das Solar-Beschreibungsfeld stand im Designer
(`label6` „Beschreibung:", `textBox_Beschreibung`), wurde von `ZeigeDetails`
aber nie befüllt (**B25**) — obwohl der Parser es liest und
`InitDatensatzUpdate` es speichert. Jetzt zeigt es, was gespeichert wird.

---

## 5. Abweichungen (A‑Zeilen)

Jede Zeile hat einen Punkt in der Windows-Abnahme (§ 10), weil der Referenzlauf
Katalogimporte nie sieht (R‑W13‑6).

| Nr | Abweichung | Begründung |
|---|---|---|
| **A‑1** | Der WP-Katalogordner heißt `VDI_Waermepumpe`; `VDI` bleibt Rückfall beim Lesen | Die drei anderen tragen ihr Gewerk im Namen; nur die Wärmepumpe hieß schlicht `VDI` (**B28**). Wer seine Kataloge dort liegen hat, findet sie weiter. |
| **A‑2** | Ein unbekannter `450`-Aufstellungsindex ist eine **Warnung**, kein Abbruch | `_Aufstellung[Int32.Parse(…) − 1]` warf und riss den GANZEN Dateiimport mit: aus 129 Wärmepumpen wurde wegen EINES Satzes nichts (**B35**). Der Satz behält jetzt die zuletzt gelesene Aufstellung. |
| **A‑3** | Die leere Auswahl meldet sich — **ein** Text für alle vier | Die Wärmepumpe brach wortlos ab (**B29**), die drei anderen meldeten je einen eigenen Text. |
| **A‑4** | Der Bezeichner kommt in allen vier aus dem **Feld**, nicht aus der Liste | Solar und WP lasen den Listeneintrag; eine Handkorrektur lief ins Leere (**B26**). In allen vier Designern ist `textBox_Name` das einzige Feld ohne `Enabled = false` — es ist also genau das Feld, das der Anwender ändern DARF. |
| **A‑5** | Das Solar-Beschreibungsfeld wird befüllt | Es stand im Designer und `ZeigeDetails` setzte es nie (**B25**). |
| **A‑6** | Solar bekommt Dublettenprüfung und Konfliktdialog | Es war die einzige der vier ohne — sie stand auf dem Stand VOR Paket D2 (**B24**). Die elf `ImportSpalten` und `SolarkollektorenStammCtrl.UpdateImport` lagen bereit. |
| **A‑7** | Alle vier schließen nach erfolgreicher Übernahme und melden den Rückgabewert | Die Wärmepumpe tat beides nie (**B4b**); ihr `MitOk` lieferte immer `false`. |
| **A‑8** | Die zweite Geometrie der englischen `.resx` entfällt ersatzlos | `Form_WP_einlesen.en-US.resx` trug 13 `.Size`, 14 `.Location` und eine eigene `ClientSize` (**B36**). In Blazor fließt das Layout. |
| **A‑9** | Der Wärmebedarf bekommt eine ECHTE Dublettenprüfung | Die einzige Prüfung war `listBox_Extern.FindString(…)` — in der ANZEIGE, still, ohne Datenbankfrage (**B2**). |
| **A‑10** | Der Hinweis nennt den **Punkt** als Dezimaltrennzeichen | Die Maske schrieb „Dezimaltrennzeichen ','"; `WaermebedarfStammCtrl.ImportGanglinie` parst mit `InvariantCulture` — ein Komma hätte die Datei abgelehnt (**B56**, neu). |
| **A‑11** | Vor dem Löschen eines Katalogeintrags wird gefragt | Der Vorläufer löschte ohne jede Sicherheitsabfrage. Dieselbe A‑Zeile wie W12‑A‑5. |
| **A‑12** | Die Projektzuordnungssperre läuft über `HatProjektzuordnung` | Sie steht seit W9.0d im Kern; die Maske rief sie nie und baute inline-SQL aus dem Anwendertext (**B8**). Die Meldung steht als Ressource (**B7**). |
| **A‑13** | Der Fehlschlag der Originalablage ist eine Warnung | `catch { }` verschluckte ihn (**B9**). Wer glaubt, sein Original sei gesichert, und es ist nicht so, merkt es sonst erst, wenn er es braucht. |
| **A‑14** | Der Dateipfad ist ein **Parameter**, kein Feld | `filebasename` war ein Feld: Brach der Anwender die Dateiwahl ab, lief „Einlesen" mit der Datei des VORIGEN Laufs weiter (**B10**). |
| **A‑15** | Der erfolgreiche Wärmebedarfs-Import meldet sich | Der Vorläufer meldete gar nichts — der Anwender sah nur die neue Zeile. |
| **A‑16** | Die zwei U+200B der englischen Beschriftung sind nicht übernommen | Rückstand einer Maschinenübersetzung (**B12**). |
| **A‑17** | Die Rückmeldungen des CEC-Dienstes sind **Schlüssel**, keine Sätze | Der Kern kennt keine Anzeigetexte. |
| **A‑18** | `Bifacial` ist ein Wahrheitswert; der Text entsteht in der Oberfläche | „Ja"/„Nein" bzw. „Ja (0,70)" standen als deutsche Anzeigetexte im Kern (**B50**). |
| **A‑19** | Die PTC-Näherung steht im Modell (`PanModule.PtcGeschaetzt`) | Sie stand in `ShowDetail` — Fachwissen im Anzeigecode (**B43**). Die Zahl ist unverändert. |
| **A‑20** | Die PAN-Sitzungsliste lebt so lange wie der Dialog | Sie war `static` und überlebte Maskenschluss, Projektwechsel und Prozessleben (**B46**). Das SAMMELN mehrerer Dateien einer Sitzung bleibt Absicht. |
| **A‑21** | Der PAN-Dateiname reist mit | `ParsePan(inhalt)` ließ `SourceFile` leer (**B45**). |
| **A‑22** | `"(alle)"` ist der Listenplatz 0 und kein Anzeigetext, gegen den verglichen wird | Fünf Fundstellen verglichen gegen die Zeichenkette (**B39**); eine Übersetzung hätte den Filter still zerrissen. |
| **A‑23** | Die Suche läuft über `Suchmuster` aus dem Kern | `GetFilterRegex` war die dritte Platzhaltersuche des Bestands (**B41**). |
| **A‑24** | Das CEC-Baujahr kommt aus der Kopfzeile | `DateTime.Parse(fields[26], …)` griff auf einen FESTEN Spaltenindex zu, obwohl jedes andere Feld über die Kopfzeile aufgelöst wird (**B48**). |
| **A‑25** | Der PV-Modulimport ist vollständig lokalisiert | Er war die am wenigsten übersetzte Maske des Bestands: leere `.resx`, keine Satelliten, rund 69 deutsche Literale (**B54**). Dabei ist der Klammerfehler „γ_r – Leistungs-TK [%/°C:" behoben (**B53**). |
| **A‑26** | Der CEC-Netzabruf meldet seinen Fortschritt und lässt sich abbrechen | `LoadDataAsync` nahm den Melder seit jeher entgegen; die Maske übergab ihn nicht (**B38**). |
| **A‑27** | `ZeigeDetails` im Einzelfall folgt dem PufferSp-Verhalten | Heizkessel und WP zogen die Detailfelder auch im Einzelfall nach; PufferSp nicht, „damit eine Handkorrektur auch den Konfliktweg übersteht". Das ist das dokumentierte Verhalten — die Komponente zieht die Felder nur bei einem Klick nach. |

### Wörtlich trotz Befund

| Nr | Verhalten | warum es bleibt |
|---|---|---|
| **B23** | `PufferSpImport` und `WaermepumpenImport` haben **keinen Nachlaufblock**: Der letzte Satz einer Datei fällt weg, wenn nach ihm kein Fremdsatz mehr kommt | Das ist heute so, und die Proben halten es fest (`pufferspeicher_vaillant.vdi` liefert 9 von 10, `waermepumpen_hoval_ohne_abschluss.vdi` 1 von 3). Es zu beheben wäre eine Verhaltensänderung am Parser, die der Referenzlauf nicht sieht — **Anwenderfrage** (offener Punkt W13‑O‑4). |
| **B30** | `Baujahr` und `maxPtherm` gehen als Vergleichswerte in den Kandidaten, werden aber nie gesetzt | Sie vergleichen den Vorgabewert gegen den Bestand. Wörtlich behalten; die Zahl der Vergleichsspalten ist die des Katalogs. |
| **B32** | Die Kühlleistung wird nur gesetzt, **wenn** eine elektrische Zuheizung angegeben ist | Zwei fachlich unabhängige Größen hängen aneinander. **Anwenderfrage** (W13‑O‑5). |
| **B40** | Zwei Leistungsbegriffe im PV-Import: Die Maske filtert und zeigt `I_mp · V_mp`, `PVModule.Efficiency` rechnet mit `STC` | Bei den meisten Modulen liegen beide dicht beieinander. Welcher gemeint ist, ist eine **Anwenderfrage** (W13‑O‑3). |
| **B44** | PAN-Module kommen **ohne** Temperaturkoeffizienten in den Katalog | `muISC` und `muVocSpec` stehen in der Datei und standen im Vorläufer auskommentiert daneben. Ob sie umgerechnet übernommen werden sollen, ist eine **Anwenderfrage** (W13‑O‑2). |
| **B51** | Zwei Menüpunkte öffnen dieselbe Maske | Der Vorläufer öffnete sie im SELBEN Zustand; jetzt setzt das Argument die Quelle. Ob ein Menüpunkt genügt, ist eine **Anwenderfrage** (W13‑O‑6, R‑W13‑12). |
| — | Trägt der Ablageordner schon eine gleichnamige Datei, wird DIESE gelesen | Bestandsverhalten (`Form_AdminWaermeeinlesen`:133); derselbe offene Punkt wie W12‑O‑2 beim Lastgang (W13‑O‑1). |

### Ersatzlos entfallen

Die vier `_anzeigeIndex`/`_listeWirdGefuellt`-Paare, die drei `FuehreAus`, die
drei Vorprüfblöcke, die vier `ZeigeDetails`, die drei `Program.ZahlPruefen` auf
**gesperrten** Feldern (**B21**), die vier toten Zeitmessungen in den Parsern
(**B18**), `m_ID_Projekt`/`m_szProjekt`/`result`/`DateiListe` der
Wärmebedarfsmaske (**B5**), `btn_Abbrechen_Click` ohne Knopf (**B6**), der leere
`listBox_Extern_SelectedIndexChanged`, der 38-zeilige „NOTEBOOK-FIX"-Block, die
sechs verwaisten `Label2`-Einträge der Satellitendateien (**B20**), die vier
doppelt vergebenen `DataGridView`-Spaltennamen (**B42**), `HeaderGradientPanel`
samt `LinearGradientBrush`, `MakeSmoothButton`/`MakeSmoothRounded` über
`GraphicsPath`, `MakeSmooth` per Reflexion und die Gitterkopf-Färbung.

---

## 6. Texte (W13.5)

**145 neue Schlüssel in beiden Sprachen** (3 935 → 4 080):

| Gruppe | Zahl | Wofür |
|---|---|---|
| `IMP_LADE_*` | 5 | die Sammelmeldung, die hartkodiert deutsch im KERN stand (**B19**) |
| `IMP_KAT_*` | 57 | Titel, Knöpfe, Filterzeile, 23 Feldbeschriftungen, 9 Einheiten, 4 Meldungen, 6 Protokolltexte der vier Ausprägungen |
| `IMP_TXT_*` | 4 | die Meldungen der Ganglinien-Textdatei |
| `WBAD_*` | 19 | die Wärmebedarfsverwaltung |
| `PVIMP_*` | 47 | der PV-Modulimport — **vollständig neu**, er hatte gar keine |
| `CEC_*` / `PAN_*` | 13 | die Rückmeldungen der beiden Dienste |

**Die Filterzeile ist erstmals englisch** (**B22**): `lbl_Filter`,
`lbl_*Von` und `lbl_*Bis` standen in KEINER Satellitendatei — auf Englisch
blieb sie deutsch. **Die Solar-Texte sind aus den Zwillingen abgeleitet**
(**B27**, R‑W13‑8): Die Maske hatte weder `de-DE` noch `en-US`; „Firma:" wird
„Company:" wie bei den drei anderen, die kollektorspezifischen (h0, a1, a2,
Kdir, Kdfu) sind neu.

**Beide `.resx` haben denselben Schlüsselsatz** — 4 080 Einträge, 0 nur in einer
Sprache (geprüft mit einem Mengenvergleich über beide Dateien).

Die verwaisten Einträge (**B20**) und die U+200B-Rückstände (**B12**) sind nicht
übernommen.

---

## 7. Formularkarte (W13.6)

| Zählung | vor W13 | nach W13 |
|---|---|---|
| Designer-Dateien (Repo) | 39 | **33** |
| Masken (Repo) | 38 | **32** |
| davon lokalisiert | 25 | **21** |
| erreichbar „ja" | 37 von 38 | **31 von 32** |
| „nein" / „verwaist" / „unklar" | 0 / 0 / 1 | 0 / 0 / **1** |

Die eine „unklar"-Maske bleibt `Form_PufferSp_Bearbeiten` (Welle 14a).
Nur VIER der sechs Masken waren lokalisiert.

**Der Umlaut-Anker ist umgehängt.**
`RazorSchreiberTests.UmlauteImOrdnernamenWerdenUmschrieben` hing an
`Form_WP_einlesen.designer.cs`. Die Datei ist nach
`Werkzeuge/Formularkarte.Tests/Pruefmuster/Wärmepumpe/` **verschoben** statt
gelöscht — genau wie W7 mit `Wizard_WPItem` und W4 mit `ucVorlagenZeile`. Das
ist die stabile Lösung: Der Umlaut-Ordner IST die Prüfsache, nicht die Maske;
ein eingefrorenes Muster kann keine Welle mehr wegnehmen. Die Umschreibregel
(ä → ae) bleibt in Kraft, solange `EPOS.UI/Dialoge/Waermepumpe/` so heißt.

**`help_mapping.txt` bleibt unverändert.** Seine sechs Zeilen sind die Adresse
eines **Hilfetextes**, nicht einer Klasse; die Razor-Komponenten führen sie als
`HilfeSchluessel` weiter — dieselbe Praxis wie seit W12. `HilfeKontext` dagegen
bildet **Klassennamen** ab und ist umbenannt: `KatalogImportDialog` (für alle
vier Ausprägungen — der Bereich hinge sonst am Klassennamen und käme viermal
gleich heraus), `WaermebedarfAdminDialog`, `PvModulImportDialog`. Mit
`Main_PV_Test` fällt auch der **tote** Eintrag `Form_CECImport`, den der
Dateiname erzeugt hatte (**B37**).

`H7_InfoButtons_Protokoll.md` ist nachgezogen: die sechs Zeilen sind als
abgelöst gekennzeichnet, und die CP1252-Angabe für drei von ihnen ist als seit
iU1‑P1.12 überholt vermerkt (**B13**).

---

## 8. Nachweise

### 8.1 Build

```
dotnet build WP-Plan.sln -c Release -p:Platform=x64
→ Build succeeded. 0 Fehler, 12 Warnungen
```

Die zwölf sind die bekannten: 6 × WFO1000, 2 × CS0108, 2 × CS0109,
1 × WFO0003, 1 × CA2255. **Keine neue.**

### 8.2 Tests

```
dotnet test WP-Plan.Kern.slnf -c Release
→ EPOS.Kern.Tests       566 gruen
  EPOS.UI.Tests       1 619 gruen
  SpeicherEngine.Tests  337 gruen
  KiKern.Tests          450 gruen
  = 2 972 gruen, 0 rot

dasselbe mit LANG=en_US.UTF-8
→ 2 972 gruen, 0 rot
```

Basis vor der Welle: 2 807, also **165 Fälle mehr**:

| Datei | Fälle | Gegenstand |
|---|---|---|
| `EPOS.Kern.Tests/KatalogImportTests.cs` | 53 | die fünf Parser gegen eingefrorene Werte, die vier Sonderfälle, die Nachlauf-Löcher, `VdiAuswahlFilter`, `DublettenPruefung` gegen die Testdatenbank, die Aufräumarbeiten an CEC und PAN |
| `EPOS.Kern.Tests/KatalogImportAblaufTests.cs` | 33 | dieselben Proben durch den NEUEN Kern-Ablauf; Profil, Filter, Vorprüfung, Ausführung, Sammelmeldung |
| `EPOS.UI.Tests/Dialoge/KatalogImportDialogTests.cs` | 29 | W13.1 — vier Feldbestandsfälle, einer je Ausprägung |
| `EPOS.UI.Tests/Dialoge/WaermebedarfAdminDialogTests.cs` | 17 | W13.2 |
| `EPOS.UI.Tests/Dialoge/PvModulImportDialogTests.cs` | 20 | W13.3 |
| `EPOS.UI.Tests/Bausteine/ZeilenmarkierungTests.cs` | 8 | die Markierungsregel (W13.0l) |
| `EPOS.UI.Tests/Bausteine/ZeilenwahlTests.cs` | +4 | der Mehrfachmodus |
| `EPOS.UI.Tests/Standards/RasterTests.cs` | +2 | die Virtualisierung |
| übrige | −1 | `SprungzielTests`: zehn Ziele werden neun |

**Jede neue Testklasse mit deutschen Text-Zusicherungen pinnt die Sprache
SELBST im Konstruktor** (Regel seit W8, nachgeschärft nach dem roten
Windows-Lauf 33839255709): Kultur, UI-Kultur und
`DefaultThreadCurrentCulture`/`-UICulture` auf `de-DE`. Sich darauf zu
verlassen, dass eine andere Klasse den Prozessstandard gesetzt hat, war genau
die Ursache. In `EPOS.Kern.Tests` pinnen die beiden `LadeMeldung`-Fälle ihre
Sprache im Rumpf und stellen sie im `finally` zurück.

`TestDatenbank` wird nur lesend je Klasse geteilt (Regel seit W11a).

### 8.3 Die Import-Proben — der eigentliche Nachweis

```
Referenzlaeufe/Importproben/            (20 Dateien, 188 KB)
  heizkessel_vaillant.vdi                5 Saetze; 3 mit Wirkungsgrad in Spalte 26,
                                         2 mit Rueckfall auf 710.01 Spalte 6
  heizkessel_buderus.vdi                 3 Saetze mit Emissionswerten (CO2 14, NOx 95,
                                         CO 15) und Oel-Brennstoffindex 9
  pufferspeicher_vaillant.vdi            Trinkwasserabschnitt (gefiltert) + 10 Bloecke
                                         -> 9 Saetze; der zehnte faellt weg (B23)
  pufferspeicher_weishaupt.vdi           Solarspeicher (Typ 1) neben Pufferspeicher (2)
  solarkollektoren_vaillant.vdi          3 Saetze: Flach-, Roehrenkollektor, leere Bruttoflaeche
  solarkollektoren_gegenprobe.vdi        alle vier Bauarten + Bezugsflaechen-Rueckfall
  waermepumpen_hoval.vdi                 3 Saetze; checkDaten trennt Voll- von Teillast
  waermepumpen_hoval_ohne_abschluss.vdi  3 Bloecke -> 1 Satz (B23, Blatt 22)
  waermepumpen_gegenprobe_aufstellung.vdi 450er-Index 7 ausserhalb der Tabelle
  cec_module_50.csv                      50 Module, Kopf-, Einheiten- und [0]-Zeile
  cec_module_gegenprobe.csv              #-Kommentar, Leerzeile, Komma im Anfuehrungsfeld
  pan_{jinko,lg,panasonic,trina}.pan     die vier .pan-Dateien des Bestands
  waermebedarf_8760.txt                  8 760 Stundenwerte, ein Wert je Zeile
  waermebedarf_gegenprobe_{semikolon,komma,leerzeile}.txt
  ganglinie_mit_kopfzeile.txt            fuer den Kopfzeilenschalter (W14b)
```

**Die Erwartungswerte sind aus dem Bestand vom 04.09.2026 eingefroren** — VOR
dem Umbau, auf die letzte Stelle: Satzzahl, Namen, Kennwerte, Rohzeilen der
Kennlinien Zeichen für Zeichen. `KatalogImportAblaufTests` fährt **dieselben**
Proben durch den neuen Ablauf und erwartet **dieselben** Zahlen. Beides grün.

Die Proben liegen als `-text` in `.gitattributes`: Die VDI-Ausschnitte tragen
die Kodierung des Originals (Windows-1252) und CRLF; `text=auto` würde sie beim
nächsten Auschecken umschreiben und damit genau das ändern, was hier bewiesen
wird. Dieselbe Begründung wie bei den Ganglinien-Proben aus W12.

**Zwei eingefrorene Fälle sind mit einer A‑Zeile umgestellt worden** — und
nennen im Kommentar, was vorher dastand: der Absturz beim Aufstellungsindex
(A‑2) und die Rückmeldung des CEC-Dienstes (A‑17). Das ist der Sinn des
Einfrierens: Eine Verhaltensänderung fällt auf, statt durchzurutschen.

**Drei Befunde sind beim Einfrieren neu aufgefallen:**

| Nr | Befund |
|---|---|
| **W13‑B56** | `WaermebedarfStammCtrl.ImportGanglinie` parst die Werte mit `InvariantCulture`, die Maskenbeschriftung nannte aber das **Komma** als Dezimaltrennzeichen. Eine Datei mit Komma wäre an `double.Parse` gescheitert und hätte „Fehler beim Speichern" ergeben — ohne dass jemand die Beschriftung verdächtigt hätte. Behoben mit A‑10. |
| **W13‑B57** | Die Trina-PAN-Probe führt `BifacialityFactor` 0,70, aber keinen `Bifacial`-Schlüssel; das Modul gilt damit als einseitig. Wörtlich behalten und im Test festgehalten. |
| **W13‑B58** | `VDI-3805-Daten/PV/CEC Modules.csv` ist eine **Semikolon**-Fassung mit Dezimalkomma und lässt sich von `CECDataService` gar nicht lesen — sein Zerleger trennt an Kommas. Nur `CEC Modules_UTC.csv` hat das Format, das der Dienst auch aus dem Netz holt; die Probe stammt von dort. |

### 8.4 SQL-Dialektprüfer

```
python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite
→ 1 241 SQL-Texte geprueft: 0 Fundstellen, 173 dynamisch, 1 068 in Ordnung
```

1 231 → 1 241: Die vier transaktionalen Importwege (W13.0e) bringen ihre
`COUNT(*)`-, `MAX(ID)`- und `INSERT`-Anweisungen mit; das inline-SQL der Masken
ist dafür weg.

### 8.5 ChartProben

```
dotnet run --project Proben/ChartProben -c Release
→ 30 Bilder geprueft, 0 Verstoesse. ERGEBNIS: alle gruen.
```

**Unverändert 30** — die Welle fasst den Renderer nicht an.

### 8.6 Referenzlauf

```
dotnet run --project EPOS.Referenzlauf -c Release -- \
  lauf --quelle Referenzlaeufe/Kenndaten_Test.sqlite --projekte 1030,1007,1017 \
  --ziel artifacts/reflauf/w13
→ Erfolgreich: 3 von 3

dotnet run --project EPOS.Referenzlauf -c Release --no-build -- \
  vergleich <Basis 1030/1007/1017> artifacts/reflauf/w13
→ Projekt_1007: PASS (29 Dateien, 324 219 Werte)
  Projekt_1017: PASS (21 Dateien, 254 154 Werte)
  Projekt_1030: PASS (22 Dateien, 236 670 Werte)
  GESAMT: PASS (815 043 Werte innerhalb der Toleranz)

diff -rq je Projekt gegen Referenzlaeufe/2026-08-30_B3-Kaskade
→ BYTE-GLEICH: Projekt_1030, Projekt_1007, Projekt_1017
```

**Byte-gleich, nicht nur innerhalb der Toleranz.** Die Welle fasst den Rechenweg
nicht an — und sie könnte es auch nicht beweisen, wenn sie es täte: Der
Referenzlauf sieht keinen Katalogimport. Dafür sind die Import-Proben da.

### 8.7 Formularkarte

```
dotnet test Werkzeuge/Formularkarte.Tests -c Release
→ 123 gruen

dotnet run --project Werkzeuge/Formularkarte -- --alle WindowsFormsApplication1
→ 32 Masken, 21 lokalisiert, 31 erreichbar, 0 nein, 0 verwaist, 1 unklar
```

### 8.8 Keine Typverwendung ist übrig

`git grep` über `*.cs` und `*.razor` nach den sechs Klassennamen, nach
`Main_PV_Test`, nach `ImportKonflikteHuelle` und nach
`Sprungziel.WaermebedarfExternAdmin` findet **nur noch Kommentare,
Maskenschlüssel, Hilfeadressen und das eingefrorene Prüfmuster** — keinen
Aufruf, kein `new`, keine Typreferenz.

Die `Masken.*`-Konstanten behalten ihre Werte (`"Form_Heizkessel_einlesen"` …):
Sie sind sprachneutrale ASCII-**Schlüssel** und nicht der Name einer Klasse —
dieselbe Praxis wie seit W12.

---

## 9. Grenzen

- **`ToolsClass` bleibt stehen.** `Form_Solarganglinie_Admin` ist ihr zweiter
  Nutzer bis Welle 14b; erst dann kann sie fallen. Nur der Aufruf aus dem
  Wärmebedarf ist abgelöst — `GanglinienTextDatei` trägt den Kopfzeilenschalter
  schon jetzt, damit W14b sie ohne zweiten Bau übernehmen kann (Risiko R‑W14‑8).
- **Der PAN-Sonderweg älterer PVsyst-Fassungen ist ungeprüft.** Die vier
  `.pan`-Proben tragen alle einen `PVObject_Commercial`-Block; der Zweig für
  Dateien ohne ihn (`PanDataService`:104‑105) hat keinen Zeugen.
- **Der CEC-Netzabruf ist nicht geprüft.** Der Test geht über `LoadFromFile`;
  drei URLs und ein 30-Tage-Zwischenspeicher gehören in die Windows-Abnahme.
- **Keine Bildschirmabnahme.** Diese Umgebung hat kein Windows; alle
  Oberflächenaussagen stützen sich auf bunit und die Feldkarte. Die Liste in
  § 10 ist deshalb offen.

---

## 10. Abnahmeliste Windows (iZ5)

Je Ausprägung derselbe Weg, mit der jeweiligen Hersteller-Probe aus
`VDI-3805-Daten/`:

- [ ] Menü → **Heizkessel einlesen** → `PART03_Vaillant`: Filter 10…200 kW
      vorbelegt, Suchfeld über Name UND Firma, mehrere Sätze mit `Strg` und
      `Umschalt` markieren, Bezeichner von Hand ändern (**A‑4**), „Speichern DB",
      Sammelmeldung, Dialog schließt.
- [ ] Menü → **Pufferspeicher einlesen** → `PART20_Bosch` (190 Sätze):
      Filter 0…1000 l, Fortschritt beim Lesen, Konfliktdialog beim zweiten Lauf
      (Umbenennen / Überschreiben / Auslassen), Sammelmeldung zählt richtig.
- [ ] Menü → **Solarkollektoren einlesen** → `PART19_Junkers_Bosch`:
      **Dublettenprüfung und Konfliktdialog sind NEU** (**A‑6**); das
      Beschreibungsfeld ist befüllt (**A‑5**); eine Handkorrektur am Bezeichner
      wirkt (**A‑4**).
- [ ] Menü → **Wärmepumpen einlesen** → `PART22_Wolf` (8,3 MB, 92 376 Zeilen):
      Der Fortschritt läuft, **Abbrechen wirkt**, die Maske bleibt bedienbar.
      Leere Auswahl meldet sich (**A‑3**), nach Erfolg schließt der Dialog
      (**A‑7**). Katalogordner ist `VDI_Waermepumpe`, ein vorhandener `VDI`
      wird als Rückfall gefunden (**A‑1**).
- [ ] Eine WP-Datei mit unbekanntem `450`-Index: **Warnung statt Abbruch**, die
      übrigen Sätze stehen (**A‑2**).
- [ ] Menü → **Wärmebedarf Ganglinie**: Liste, „DB Ganglinie Löschen" mit
      Rückfrage (**A‑11**), Projektzuordnungssperre (**A‑12**),
      ReadOnly-Sperre; „Datei Auswählen…" legt die Originaldatei ab, ein
      Fehlschlag wird gemeldet (**A‑13**); „Inhalt anzeigen…" öffnet mit der
      Systemanwendung; „Datei in DB Einlesen…" mit einer 8 760-Zeilen-Datei
      meldet Erfolg (**A‑15**), ein zweites Mal erscheint der **Konfliktdialog**
      (**A‑9**).
- [ ] Dieselbe Verwaltung **aus dem externen Wärmebedarf**: „Bearbeiten…"
      (bis W9‑O‑9 „Einlesen/Bearbeiten..")
      zeigt sie als **Überlagerung** im selben Fenster, nicht als zweites
      Fenster; nach dem Schließen ist der Katalog frisch.
- [ ] Menü → **PV-Import CEC**: Netzabruf mit laufendem Balken und
      **Abbrechen** (**A‑26**); danach 20 746 Zeilen im Gitter, flüssig zu
      rollen; Hersteller- und Technologiefilter, Platzhaltersuche
      („Trina*", „*410*"), vier Zahlenfelder, „Zurücksetzen"; ein Modul wählen,
      drei Reiter durchsehen, „Auswahl übernehmen".
- [ ] Menü → **PV-Import PAN**: Der Dialog macht **im PAN-Modus** auf
      (**B51**); eine `.pan`-Datei laden, dann eine zweite — beide stehen in der
      Liste; Maske schließen und neu öffnen: die Liste ist **leer** (**A‑20**);
      im Reiter „Elektrisch" stehen „-" für α und β, PTC ist geschätzt (**A‑19**).
- [ ] **Die zwölf Punkte** der Abnahme-Prüfliste
      `Konzept_Dublettenpruefung_Import_EPOS-Plan.md:469–490` für jeden der fünf
      Kataloge — 1 bis 7 sind automatisiert (§ 8.2), 8 bis 12 nicht.
- [ ] de **und** en, 125 %, Esc je Ebene (Konfliktdialog vor Dialog).

---

## 11. Offene Punkte

| Nr | Punkt |
|---|---|
| **W13‑O‑1** | Trägt der Ablageordner `%LOCALAPPDATA%\…\Waermebedarf` schon eine gleichnamige Datei, wird **diese** gelesen und nicht die soeben gewählte (Bestandsverhalten). Eine zweite Datei gleichen Namens mit anderem Inhalt geht damit still verloren — derselbe offene Punkt wie W12‑O‑2 beim Lastgang. |
| **W13‑O‑2** | **Anwenderfrage: PAN ohne Temperaturkoeffizienten (Befund B44).** Ein PAN-Modul kommt mit `alpha_SC = 0`, `beta_OC = 0` und `T_NOCT = 0` in den Katalog, obwohl die Datei `muISC` (mA/K) und `muVocSpec` (mV/K) führt und beide im Vorläufer auskommentiert danebenstanden. Sollen sie umgerechnet übernommen werden — und mit welchem Faktor? |
| **W13‑O‑3** | **Anwenderfrage: zwei Leistungsbegriffe (B40).** Der PV-Import filtert und zeigt `I_mp · V_mp`, `PVModule.Efficiency` rechnet mit `STC`. Welcher ist der gemeinte? |
| **W13‑O‑4** | **Anwenderfrage: die fehlenden Nachlaufblöcke (B23).** `PufferSpImport` und `WaermepumpenImport` verlieren den letzten Satz einer Datei, wenn nach ihm kein Fremdsatz mehr kommt. Heizkessel und Solar haben den Block; soll er auch dort hinein? |
| **W13‑O‑5** | **Anwenderfrage: Kühlleistung an Zuheizung gekoppelt (B32).** Ohne elektrische Zuheizung bleibt auch die Kühlleistung 0 — zwei fachlich unabhängige Größen. |
| **W13‑O‑6** | **Anwenderfrage: zwei PV-Menüpunkte (B51, R‑W13‑12).** „CEC laden" und „PAN laden" öffnen dieselbe Maske; sie kann beide Quellen. Genügt ein Menüpunkt? |
| **W13‑O‑7** | Die Windows-Abnahme (§ 10) steht aus. |

---

## 12. Geänderte und neue Dateien

**Neu in `EPOS.Kern`:** `Allgemein/Import/KatalogImportProfil.cs`,
`Allgemein/Import/KatalogImportSatz.cs`,
`Allgemein/Import/KatalogImportAblauf.cs`,
`Allgemein/Import/GanglinienTextDatei.cs`.

**Neu in `EPOS.UI`:** `Bausteine/Zeilenmarkierung.cs`,
`Dialoge/Import/KatalogImportDialog.razor`, `Dialoge/Import/KatalogImportDaten.cs`,
`Dialoge/Bedarf/WaermebedarfAdminDialog.razor`,
`Dialoge/Bedarf/WaermebedarfAdminDaten.cs`,
`Dialoge/Photovoltaik/PvModulImportDialog.razor`,
`Dialoge/Photovoltaik/PvModulImportDaten.cs`.

**Neu in `WindowsFormsApplication1`:** `Views/Import/KatalogImportHuelle.cs`,
`Views/Wärmebedarf/WaermebedarfAdminHuelle.cs`,
`Views/Photovoltaik/PvModulImportHuelle.cs`.

**Neu unter `Referenzlaeufe/`:** `Importproben/` (20 Dateien).

**Gelöscht (22 Dateien):** die sechs Masken mit ihren fünf verbleibenden
Designern und zehn `.resx`, dazu `Views/Import/ImportKonflikteHuelle.cs`.
**Verschoben:** `Form_WP_einlesen.designer.cs` nach
`Werkzeuge/Formularkarte.Tests/Pruefmuster/Wärmepumpe/`.

**Geändert:** `EPOS.Kern/Allgemein/ZahlText.cs`,
`Allgemein/Katalog/KatalogRegistry.cs`,
`Allgemein/Import/VDI 3805/{VdiAuswahlFilter,WaermepumpenImport}.cs`,
`Allgemein/Import/CEC/{CECDataService,PVModule,UnifiedModule}.cs`,
`Allgemein/Import/Pan/{PanDataService,PanModule}.cs`,
`Allgemein/Dienste/Masken.cs`,
`Controller/{HeizkesselStammCtrl,PufferSpStammCtrl,SolarkollektorenStammCtrl,WPStammCtrl}.cs`,
die drei `MyResource`-Dateien, `EPOS.UI/Bausteine/Zeilenwahl.razor`,
`EPOS.UI/Standards/Raster.razor`, `EPOS.UI/wwwroot/epos-ui.css`,
`EPOS.UI/Dialoge/Bedarf/WaermebedarfExternDialog.razor`,
`EPOS.UI/Dialoge/Allgemein/Sprungziel.cs`, `Dienste/WinFormsNavigation.cs`,
`Controller/MenueCtrl.cs`, `MDIMainForm.cs`, `Allgemein/Blazor/Sprungbruecke.cs`,
`Allgemein/KI/HilfeKontext.cs`,
`Views/Wärmebedarf/WaermebedarfExternHuelle.cs`,
`Allgemein/Hilfe/H7_InfoButtons_Protokoll.md`,
`Werkzeuge/Formularkarte.Tests/{RazorSchreiberTests,StapelTests,ErreichbarkeitTests}.cs`,
`Werkzeuge/Formularkarte/Erreichbarkeit_2026-09-03.md`, `.gitattributes`.

---

## 13 — Windows-Abnahme 05.09.2026 (Befund W13‑B‑1)

### 13.1 Beobachtung

Der Anwender meldete zum Weg **Administration → Daten & Import → VDI-3805-Import**:

> „Admin: VDI-3805-Datei-Import: **Absturz bei Datei laden**, **teilweise Absturz auch bei
> Dateiauswahl-Dialog**."

Zwei Beobachtungen in einem Satz, und das Wort **teilweise** ist der Hinweis, der die
Untersuchung geführt hat: Ein Fehler, der mal auftritt und mal nicht, ist selten ein
Rechenfehler und fast immer eine Frage der Zeitlage.

### 13.2 Was der Kern NICHT ist

Der erste Verdacht lag beim Parser: Eine Ausnahme aus `KatalogImportAblauf.Lesen` käme im
Wirt als unbehandelte Ausnahme eines Blazor-Ereignisses an, und der WinForms-`BlazorWebView`
(10.0.100) führt kein `UnhandledException`-Ereignis — sie beendete den Prozess.

**Der Verdacht trifft nicht zu, und das ist jetzt bewiesen.** Neun Fälle in
`EPOS.Kern.Tests/KatalogImportAblaufTests` (Abschnitt 2b) fahren alle vier Ausprägungen
gegen sechs Bauarten von „kaputt" — ein leeres Blatt, Rohbytes, ein abgeschnittener Satz,
das falsche Trennzeichen, ein Verzeichnis statt einer Datei und ein Pfad, den es nicht
gibt — dazu je eine mitten im Satz abgeschnittene ECHTE Probe:

| Stelle | Verhalten |
|---|---|
| `Lesen` | fängt **jeden** Fehlschlag außer dem Abbruch und macht eine `IMP_KAT_PROT_LESEFEHLER`-Meldung mit dem Wortlaut der Ausnahme daraus |
| `Vorpruefen` | überspringt `null`, leere Listen und Indizes außerhalb des Bestands |
| `Ausfuehren` | zählt einen fehlerhaften Eintrag als Fehler und läuft weiter (Kommentar `Form_Heizkessel_einlesen:298`) |

Damit liegt die Ursache **im Wirt**, nicht im Ablauf.

### 13.3 Ursache 1 (sicher) — der Dateiwähler im WebView2-Rückruf

Der Weg zum Wähler lautete:

```
Kachel bzw. Menuepunkt          (Blazor-Ereignis der ERSTEN WebView2)
  -> BlazorDialogForm<KatalogImportDialog>.ShowDialog()
    -> Dateiwahl.BeiKlick        (Blazor-Ereignis der ZWEITEN WebView2)
      -> KatalogImportHuelle.DateiWaehlen
        -> Task.FromResult(Dienste.Datei.DateiOeffnen(...))
          -> OpenFileDialog.ShowDialog()
```

Die letzte Zeile öffnet ihre **verschachtelte Nachrichtenschleife INNERHALB des
`WebMessageReceived`-Rückrufs** der WebView2, in der die Komponente gerade ihr Ereignis
abarbeitet. `Task.FromResult` ist dabei kein Nebenschauplatz, sondern der Kern der Sache:
Es sieht aus wie ein Task, ist aber schon erfüllt, wenn es entsteht — der Wähler läuft
also **vollständig synchron im Ereignis**.

Das ist wortgleich das Muster von **W16b‑B‑1** (§ 12.1 des W16b-Protokolls), nur eine
Ebene tiefer: Dort baute sich eine ZWEITE WebView2 im Rückruf der ersten auf, hier pumpt
ein modales Fenster im Rückruf **derselben** WebView2, die den Dialog zeigt. Während die
Schleife pumpt, liefert die WebView2 weitere Nachrichten aus, und Blazor beginnt einen
Zeichenlauf, während einer läuft. Ob das gutgeht, hängt daran, was in diesem Augenblick
sonst noch unterwegs ist — **daher „teilweise"**.

**Behebung.** `IDateiDienst` und `IDialogDienst` bekommen **wartbare Zwillinge** mit
Standardfassung — `DateiOeffnenAsync`, `DateiSpeichernAsync`, `OrdnerWaehlenAsync`,
`MeldungAsync`, `WarnungAsync`, `FrageAsync`. Die synchronen Formen bleiben unangetastet;
`KeineDateiwahl`, `StilleDialoge` und jeder Prüfstand brechen durch die Erweiterung nicht.
Die Windows-Fassungen fahren ihr Fenster über
`Allgemein/Blazor/Blazornachlauf.cs` eine **geposteten Nachricht später** hoch, aus der
gewöhnlichen Schleife von `Application.Run` heraus.

`Blazornachlauf` ist der Bruder von `Blazorsprung` für den Fall **mit Rückgabewert**: Der
Sprung liefert nichts zurück und kommt mit `BeginInvoke` und einem Riegel aus; ein
Dateiwähler MUSS einen Pfad liefern, also gibt der Nachlauf einen `Task`, den der Aufrufer
`await`et. Einen Riegel hat er bewusst nicht — zwei Wähler nebeneinander gibt es ohnehin
nicht, und ein Riegel würde einen Wartenden mit einem leeren Ergebnis abspeisen.

**Was NICHT geändert werden musste:** `Dateiwahl.razor` und `KatalogImportDialog` — beide
`await`eten ihren Delegaten von jeher. Die ganze Änderung liegt in den elf Hüllen und in
den zwei Dienstfassungen.

**Auf iOS ist derselbe Befund ein anderer Fehler.** Ein Blazor-Ereignis LÄUFT dort auf dem
Hauptfaden, und `IosDateiDienst.AufDemHauptfaden` liefert von dort `default`, um einen
Selbstblock zu vermeiden — der Wähler ging also gar nicht erst auf. `DateiOeffnenAsync`
gibt den Task des Wählers weiter, statt auf ihn zu blocken; damit ist der Aufruf vom
Hauptfaden der Normalfall und nicht mehr der verbotene.

### 13.4 Ursache 2 (sicher) — es gab kein Netz

Für „Absturz **bei Datei laden**" gilt: Was auch immer nach der Dateiwahl wirft — im
Ereignis, in einer Lebenszyklusmethode, in einem `Progress`-Rückruf —, es ging bis hierher
**ungefangen** an den Renderer, und von dort kam es am Bedienfaden wieder heraus. Der
WinForms-`BlazorWebView` (10.0.100) führt kein `UnhandledException`-Ereignis; genau diese
Lücke nennt `WebViewWache` im eigenen Klassenkopf als ihre Grenze:

> „Was danach kommt — eine Ausnahme beim Zeichnen der Komponente — sieht diese Wache
> nicht; sie schweigt dann auch."

**Behebung: `EPOS.UI/Bausteine/Fehlerschranke.razor`** — eine `ErrorBoundary` auf
`ErrorBoundaryBase`. Wirft eine Kindkomponente, zeigt sie statt der Maske einen lesbaren,
**markierbaren** Kasten mit Typ, Wortlaut und innerster Ausnahme und zwei Knöpfen
(„Weiter" / „Schließen", beide stellen den Inhalt wieder her); derselbe Satz geht nach
`Debug` und `Trace`. Bewusst **nicht** die fertige `ErrorBoundary` aus
`Microsoft.AspNetCore.Components.Web`: Die spritzt einen `IErrorBoundaryLogger` ein und
zeigt im Fehlerfall eine leere `<div class="blazor-error-boundary">` — also wieder eine
stumme Fläche, und das ist der Befund selbst.

**`EPOS.UI/Bausteine/Wurzel<T>`** ist das fehlende Zwischenglied. Eine `ErrorBoundary`
fängt ihre NACHFAHREN, muss also **über** `T` stehen — eine Wurzelkomponente hat aber
nichts über sich, `RootComponents.Add<T>("#app", …)` hängt sie unmittelbar an den
Renderer. Die drei Hüllen mounten seither `Wurzel<T>` statt `T`:
`BlazorDialogForm`, `BlazorSeite` und `EPOS.iOS/HauptSeite` (dort
`Wurzel<AppWurzel>`, Regel: **jede Wurzel steht in der Fehlerschranke**).

Der Parametersatz geht dabei **unverändert** durch (`Wurzel.Gaben` mit
`CaptureUnmatchedValues`), und beide Parametersatz-Wachen sehen weiterhin `T`:
`Parametersatzwache.Pruefen` läuft im Konstruktor der Hülle mit dem UNVERPACKTEN Typ, und
`ParametersatzTests` liest den Quelltext der Hüllen, in dem weiterhin
`new BlazorDialogForm<T>` steht. `Wurzel` zeichnet nichts Eigenes — ohne Wurf ist im DOM
kein Unterschied zu sehen; ein Wirt, der Maße verschöbe, hätte sechzig Dialoge verschoben.

### 13.5 Was der Importweg NICHT braucht

`Meldung`/`Frage` kommen im Katalogimport **gar nicht vor** — er meldet über sein
`Warnbanner` und fragt über die Überlagerung `ImportKonflikteDialog` (Hausregel A‑8 aus
W11b). Die wartbaren Formen von `IDialogDienst` sind deshalb für die Hüllen da, die
AUSSERHALB einer Komponente melden; im Importweg gibt es nichts umzustellen. Das ist der
Grund, warum die Fehlerschranke hier den zweiten Teil des Befunds trägt und nicht eine
Umstellung von `MessageBox` auf Bausteine.

### 13.6 Geänderte und neue Dateien

**Neu:** `WindowsFormsApplication1/Allgemein/Blazor/Blazornachlauf.cs`,
`EPOS.UI/Bausteine/Fehlerschranke.razor`, `EPOS.UI/Bausteine/Wurzel.cs`,
`EPOS.UI.Tests/Bausteine/FehlerschrankeTests.cs`.

**Geändert:** `EPOS.Kern/Allgemein/Dienste/{IDateiDienst,IDialogDienst}.cs`,
`EPOS.Kern/MyResource/*` (vier Schlüssel `FEHLERSCHRANKE_*`),
`WindowsFormsApplication1/Dienste/{WindowsDateiDienst,WindowsDialogDienst}.cs`,
`WindowsFormsApplication1/Allgemein/Blazor/{BlazorDialogForm,BlazorSeite}.cs`
(je die Zeile `RootComponents.Add`), elf Hüllen unter `Views/**`,
`EPOS.iOS/{HauptSeite.cs,Dienste/IosDateiDienst.cs}`, `EPOS.UI/wwwroot/epos-ui.css`,
`EPOS.Kern.Tests/KatalogImportAblaufTests.cs`,
`EPOS.UI.Tests/Dialoge/KatalogImportDialogTests.cs`.

Die elf umgestellten Hüllen: `Import/KatalogImportHuelle`,
`Photovoltaik/PvModulImportHuelle`, `Kosten/SpotpreisImportHuelle`,
`Wärmebedarf/WaermebedarfAdminHuelle`, `Stromverbraucher/StromganglinieAdminHuelle`,
`Stromspeicher/PeakShavingHuelle`, `Solarthermie/SolarganglinieAdminHuelle`,
`Simulation/QuellprofilHuelle`, `Simulation/SimulationKonfigHuelle`,
`Help/LizenzHuelle` (zweimal: öffnen und speichern).

### 13.7 Abnahmepunkte für den Anwender

| # | Was | Erwartung |
|---|---|---|
| B1 | Administration → **Heizkessel einlesen** → „Datei VDI 3805 …" | Der Dateiwähler **geht auf** und die Maske dahinter bleibt stehen. Kein Absturz, kein Einfrieren — auch nicht beim zweiten und dritten Mal in derselben Sitzung |
| B2 | Eine Hersteller-Probe wählen (`PART03_Vaillant`) | Die Liste füllt sich, der Filter 10…200 kW steht, „Speichern DB" schreibt und meldet |
| B3 | Eine **kaputte** Datei wählen (irgendeine `.txt` in `.vdi` umbenannt) | Ein **Warnbanner** „Die Datei konnte nicht gelesen werden: …" — kein Absturz, kein leeres Fenster. Die Maske bleibt bedienbar |
| B4 | Dasselbe für Pufferspeicher, Solarkollektoren, Wärmepumpen | Gleiches Verhalten; bei `PART22_Wolf` (8,3 MB) läuft der Fortschritt und **Abbrechen** wirkt |
| B5 | Bleibt trotzdem etwas stehen oder erscheint ein **Fehlerkasten** mit rotem Rand | Den Wortlaut abfotografieren — er nennt Typ, Meldung und innerste Ausnahme; die Anwendung läuft weiter |
| B6 | „Datei Auswählen…" in der Wärmebedarfs- und der Solarganglinien-Verwaltung, PV-Import PAN, Spotpreis-Import, Lizenz „Datei wählen"/„Speichern unter…" | Derselbe Wähler, dasselbe Verhalten — die elf Stellen hängen am selben Dienst |
| B7 | de **und** en | Der Fehlerkasten spricht die Oberflächensprache |

### 13.8 Wenn es am Gerät weiter abstürzt — Diagnoseanleitung

1. **Erscheint ein Fehlerkasten** (roter Rahmen, „In dieser Ansicht ist ein Fehler
   aufgetreten")? Dann hat die Fehlerschranke gefangen: Der Wortlaut IST die Antwort,
   und die Anwendung lebt noch. Bitte abschreiben oder ablichten.
2. **Stürzt der Prozess trotzdem ab** (Fenster weg, kein Kasten), dann liegt es nicht an
   einer Komponentenausnahme — die fängt die Schranke jetzt. Übrig bleiben: der
   Bedienfaden selbst, die WebView2 und der Fadenwechsel. Nachsehen in der
   **Ereignisanzeige → Windows-Protokolle → Anwendung** auf `.NET Runtime`- oder
   `Application Error`-Einträge zur Zeit des Klicks.
3. **Das Ablaufprotokoll mitlesen**: `DebugView` von Sysinternals starten (Optionen:
   „Capture Win32"), dann EPOS-Plan bedienen. Die drei Köpfe sind
   `[Fehlerschranke]`, `[Blazornachlauf]` und `[Blazorsprung]`, dazu `[WebView]` von der
   Wache. Steht `[Blazornachlauf] Der nachgelagerte Aufruf liess sich nicht einreihen`,
   hat der Nachlauf kein Wirtsfenster gefunden und **unmittelbar** ausgeführt — dann ist
   der alte Weg noch aktiv und die Ursache steht wieder in 13.3.
4. **Friert die Maske ein, statt abzustürzen**, ist es die Nachrichtenschleife: Ein
   Aufrufer wartet BLOCKIEREND auf einen `…Async` (`.Result`, `.Wait()`) und hält damit
   genau den Faden an, den der Nachlauf braucht. Suchen mit
   `git grep -nE '(DateiOeffnen|DateiSpeichern|OrdnerWaehlen|Meldung|Warnung|Frage)Async[^;]*\.(Result|Wait\(\)|GetAwaiter)'`.
5. **Gegenprobe ohne die Verzögerung**: In `WindowsDateiDienst.DateiOeffnenAsync` statt
   `Blazornachlauf.Nachgelagert(…)` `Task.FromResult(DateiOeffnen(…))` einsetzen und neu
   bauen. Kommt der Absturz zurück, war die Wiedereintritts-These richtig.

### 13.9 Grenzen

- **Keine Bildschirmabnahme.** Diese Umgebung hat kein Windows; alle Aussagen zur
  Zeitlage der WebView2 stützen sich auf die Bauart des Weges und auf den gleichlautenden
  Befund W16b‑B‑1. Dass der Wähler HINTER dem Ereignis hochfährt, ist am Quelltext
  ablesbar; dass genau das den Absturz behebt, sagt erst das Gerät (Abnahmepunkt B1).
- **Die iOS-Fassung ist ungebaut.** `EPOS.iOS` steht bewusst nicht im Solution-Filter;
  `IosDateiDienst.DateiOeffnenAsync` und `Wurzel<AppWurzel>` in `HauptSeite` sind hier
  nicht übersetzt worden. Der Nachweis ist der CI-Job `ios.yml`.
- **Die Fehlerschranke fängt keine Ausnahme, die NICHT durch den Renderer läuft.** Ein
  `async void`, ein unbeobachteter `Task` oder ein Wurf auf einem Arbeitsfaden ohne
  `await` geht weiterhin am Netz vorbei. Im Katalogimport gibt es keine solche Stelle
  (jedes `Task.Run` wird `await`et), aber die Regel gilt für neue Wege.

## Windows-Abnahme 05.09.2026 — Formularraster, Paket P3 (iU8‑E‑2)

**Der Wortlaut** (Anwender, 05.09.2026): „Darstellung der Dialoge kompakter und
übersichtlicher — Parameterblöcke rechts. Genauso für andere Dialoge prüfen."
Aufgabe #90 hat daraus die hausweite Regel gemacht (Bausteine
`Formularraster`/`Formulargruppe`, Regel in `epos-ui.css`, Bestandsaufnahme aller
92 Dateien im Protokoll `iU9_W14a`); Paket **P3** hängt Bedarf, Simulation und
Projekt ein. **Kein Feld umbenannt, kein Text geändert, keine Regel je Dialog** —
ein Dialog stellt nur seinen vorhandenen Feldlauf in den Raster.

| Datei | Felder | Raster | Einspaltig | Klasse‑B‑Entscheid |
|---|---|---|---|---|
| `Dialoge/Photovoltaik/PvModulImportDialog.razor` | 21 von 28 | 3 | nein | **Klasse B, teilweise umgestellt.** Die **drei Detailreiter** (Übersicht, Elektrisch, Thermisch) sind Formularblöcke — der handgebaute Kasten `epos-pvimport-details` entfällt samt seiner Regel im Stilblatt; dieselbe Anordnung wie im Modulkatalog, in den die Zeile übernommen wird. **Nicht** umgestellt: die **zwei Filterleisten** über dem Gitter (Hersteller, Technologie, Suche, vier Grenzwerte, Rücksetzknopf) — eine Filterleiste ist eine Leiste über einer Tabelle und kein Parameterblock; sie bleibt `epos-pvimport-filter` (flex, umbrechend). Das Gitter mit 20 746 Zeilen ohnehin nicht. |

**Drei bestehende Proben zogen nach.** `PvModulImportDialogTests` griff die
Detailfelder über `.epos-pvimport-details`; der Anker heißt jetzt
`.epos-formularraster`. Neu dazu:
`Die_Detailfelder_stehen_im_Formularraster` — er prüft ausdrücklich die **Grenze**,
dass in der Filterleiste **kein** Raster steht.

> Diese Datei gehört zu Welle W13; der Dialog stand in der Pakettabelle von
> Aufgabe #91 unter **P3**, deshalb steht der Nachtrag hier und nicht in einem
> W8–W12‑Protokoll.

**Eine Zeile Stilblatt kam dazu** — der Unterblock „Formularraster — Paket P3" in
`epos-ui.css`: Eine `Herleitungszeile` als Rasterkind spannt über **alle** Spalten.
Sie gehört zu dem Feld ÜBER ihr („Vorgabe 0,6", „aus dem Kesselwirkungsgrad");
als gewöhnliches Rasterkind fiele sie im zweispaltigen Raster **neben** ein fremdes
Feld und läse sich wie dessen Erläuterung. Sonst kein CSS, keine Inline‑Stile.
