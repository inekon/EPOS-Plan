# Befund M — der Gebäudeeditor und die Änderungen für VDI 6007 (15.09.2026)

**Protokoll.** Befund M eines Erkundungs-Agenten (Modell Opus, nur lesend) im Auftrag des Umsetzungskonzepts [`../Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Sitzung vom 15.09.2026.

Grundlage sind die Entscheide E1 (Stundenmodell ist die Vorgabe für alle Gebäude, Konzept
N1.1) und E2 (Gewichte gestrichen, Dialog auf U·A, Konzept N1.6) sowie die Kapitel 4.4,
4.8, 6.1, 8.1, 8.2, 8.3, 10 und 11 des Konzepts. Jede Aussage über den Quelltext trägt
Datei und Zeile; gelesen wurde der Arbeitsbaum des Zweigs `ios_migration_september`.

---

## 0. Das Ergebnis in sieben Sätzen

1. **Der „Gebäudedialog" ist nicht eine Maske, sondern fünf** — und der Editor, den E2
   trifft, ist `GebaeudeKatalogDialog.razor`, nicht `GebaeudeDialog.razor`.
2. **Alle Größen, die die U·A-Tabelle braucht, sind schon da** — nur über vier Gruppen und
   zwei Reiter verstreut: fünf U-Werte, fünf Flächen, drei ψ und drei L.
3. **Die Fensterfläche ist heute dreiteilig** (Nord, Süd, Ost + West); Ost und West
   getrennt heißt: zwei neue Felder, eine Summenanzeige und eine neue Pflichtregel.
4. **Der Editor bricht als einziger Dialog des Hauses die OK/Abbrechen-Regel** — er
   schreibt sofort über drei Knöpfe und hat zwei Ebenen von „Übernehmen"; das ist beim
   Umbau zu bereinigen, sonst hängt die neue Plausibilitätsprüfung an drei Stellen.
5. **Das Vorbild für den Modellschalter steht im PV-Zweig** (`PvModellFelder.razor`) und
   trägt alles, was 8.1 braucht: Klappliste, bedingte Felder, erklärende Zeile,
   „leer = NULL = Vorgabe".
6. **Für die Rechnung der U·A-Zeilen gibt es ein Hausmuster im Kern** (`Gebaeudebauweise`,
   `Ferienzeit`): eine reine Hilfsklasse, die Dialog UND Kern benutzen — eine Wahrheit.
7. **Die Hülle nach `EPOS.UI.Daten` hängt an zwei fremden Hüllen** (Brauchwasserprofile
   und Gebäudetypen); ohne eine benannte Naht wandert sie nicht.

---

## 1. Ist-Inventar der Dialoge

### 1.1 Fünf Dateien, fünf Rollen

| Datei | Zeilen | Rolle |
|---|---|---|
| `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor` | 828 | **Wirt**: Projektliste ↔ Katalogliste, Filter, Detailblock, vier Überlagerungen. Kein Fachfeld |
| `EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDialog.razor` | 983 | **Der Editor**: ein Katalogsatz auf zwei Reitern — hier greift E2 |
| `EPOS.UI/Dialoge/Bedarf/GebaeudeBedarfDialog.razor` | 324 | **Auskunft**: Wärmebedarf genau eines Gebäudes, drei Kennzahlen, Ganglinie, Monate |
| `EPOS.UI/Dialoge/Bedarf/GebaeudeWohnflaecheDialog.razor` | 284 | **Zuordnung**: Bedarfsart, Verbrauch/Wohnfläche, Jahresnutzungsgrad der Projektzeile |
| `EPOS.UI/Dialoge/Bedarf/GebaeudetypDialog.razor` | 432 | **Tagesverteilungen** eines Gebäudetyps (5 oder 8 Kurven à 24 Stunden) |

Dazu die DTO: `GebaeudeDaten.cs` (72), `GebaeudeKatalogDaten.cs` (155),
`GebaeudeBedarfDaten.cs` (38), `GebaeudetypDaten.cs` (49), `GebaeudeKatalogModus.cs` (40).

### 1.2 `GebaeudeDialog.razor` — der Wirt

**Aufbau.** Kopf mit Titel, `InfoKnopf` und `Schliesskreuz` (`:45-49`), Kontextzeile
(`:51`), `Warnbanner` bei stehender Meldung (`:53-56`). Darunter der Baustein
`Zweispaltenauswahl` (`:62-149`) mit der Projektliste links (`:69-92`), der Katalogliste
rechts (`:110-133`) und der Filtergruppe darüber (`:96-108`: `Optionsgruppe` Verwendung,
`Auswahlfeld` Gebäudeart, `Auswahlfeld` Baujahr, `Textfeld` Suche). Die Katalogleiste
trägt vier Knöpfe (`:136-148`): „Gebäude in DB ändern…", „… neu…", „… löschen",
„Gebäudetyp in DB ändern…" (letzterer nur mit Delegat, `:143`).

**Detailblock** „Gebäude: Verbrauch" (`:154-195`): ein `Formularraster` (`:165-174`) mit
fünf **nur lesenden** Textfeldern (Gebäudename, Gebäudeart, Beschreibung, Wohn-/Nutzfläche,
Art der Angabe), darunter die Leiste „Ändern" und „Simulation…" (`:178-191`). Der
Rechenweg-Hilfeknopf steht als `InfoKnopf` mit zweitem Schlüssel `Form_Gebaeude.Berechnung`
(`:158-161`, Parameter `:432-435`).

**Vier Überlagerungen** statt vier Fenster (`:202-248`): Katalogeditor (`:203-212`),
Wohnflächenangabe (`:215-224`), Gebäudetypen (`:227-236`), Wärmebedarf (`:239-248`);
dazu die Löschrückfrage (`:251-253`).

**Schlussleiste.** `SpeichernLeiste` mit OK und Abbrechen, im Assistentenbetrieb
unterdrückt (`:198-200`). Esc schließt nur, wenn keine Überlagerung offen ist
(Prüfhilfen `:478-487`).

**Bindungen.** Der Dialog bearbeitet `List<GebaeudeProjektZeile>` **an Ort und Stelle**
(`:261`) und meldet jede Änderung über einen einzigen Rückruf `Geaendert` (`:288-298`).
Der Schlüssel ist `IdZ` (`Z_ProjektGebaeude.ID`), nicht die Stamm-Id — begründet in
`GebaeudeDaten.cs:7-11` und `GebaeudeDialog.razor:544-550`.

**Für E2 ist dieser Dialog fast unberührt**: Er zeigt keine U-Werte und keine Flächen.
Betroffen sind nur zwei Stellen — der Detailblock könnte H_ges als leise Kennzahl
mitführen (Vorschlag 5.7), und die Überlagerung „Simulation…" reicht den erweiterten
Bedarfsdialog durch.

### 1.3 `GebaeudeKatalogDialog.razor` — der Editor, den E2 trifft

**Zwei Reiter, ein Feldsatz** (`:83-346`). Der Vorläufer waren zwei WinForms-Masken auf
demselben Objekt (Kopfkommentar `:3-9`).

**Reiter 1 „Flächen und U-Werte"** (`:87-206`), drei Gruppen:

| Gruppe (`Gruppenkopf`) | Felder | Zeilen |
|---|---|---|
| „Kenngrößen" | Name (Textfeld bzw. im Modus Admin Klappliste), Gebäudetyp, Beschreibung (3-zeilig), Gebäudeart, Baujahr, Verwendung, Bauart, Wohn-/Nutzfläche [m²], Fläche/Nutzer [m²], Interne Wärmegewinne [W], Fensterdurchlaßgrad (mit `Herleitungszeile` „(z.B. 0,4)"), Raumhöhe [m] | `:90-143` |
| „Flächen [m²]" | Fensterfläche Nord, Süd, **Ost + West**, Fläche Außenwand, Dachfläche, Grundfläche, sonstige Flächen | `:145-177` |
| „U-Werte [W/m²K]" | Außenwand, Fenster, Dachfläche, Grundfläche, Sonstiges | `:179-203` |

**Reiter 2 „Temperaturen, Ferien, Luftwechsel"** (`:209-343`), sechs Gruppen:
Raumtemperaturen (Soll am Tag, Nachtabsenkung auf, Maximalraumtemperatur,
Wochenendabsenkung, Soll in Ferien — je °C, `:212-236`);
„Wärmebrückenverlustkoeffizienten [W/(mK)]" (Fenster-Wand, Außenwand-Keller, Wand-Dach,
`:238-253`); „Abmessung Anschluß [m]" (dieselben drei, `:255-272`); Ferien Anfang und
Ferien Ende (je vier Paare Tag/Monat über `Ganzzahlfeld` mit `Min`/`Max`, `:274-314`);
„Sonstiges" (Luftwechselrate [1/h] und der Knopf „Brauchwasser…", `:316-335`).

**Einheiten** stehen teils am Feld (`Einheit="m²"`, `:123`; `"W"`, `:129`; `"m"`, `:137`;
`"°C"`, `:218-232`; `"1/h"`, `:322`), teils **nur im Gruppentitel** (Flächen `:433`,
U-Werte `:434`, Wärmebrücken `:436-437`, Anschlussmaße `:438`). Die Flächen- und
U-Wert-Felder tragen deshalb **keine** eigene Einheit — für die U·A-Tabelle ist das zu
vereinheitlichen.

**Validierung.** `PflichtzahlenStehen()` (`:859-892`) prüft **17** Zahlen des ersten
Reiters in fester Reihenfolge und meldet den **ersten** fehlenden mit seinem *Feldnamen*
(nicht seiner Beschriftung — die 17 Namen stehen getrennt, `:963-979`); die Meldung
springt auf den ersten Reiter (`:887`). Der Name ist Pflicht beim Anlegen (`:909-913`).
Auf Reiter 2 prüft `BeiUebernehmen()` (`:788-829`) allein die **vier Ferienregeln** über
`Ferienzeit.Pruefen` (`:798-799`). **Wertebereiche prüft heute niemand** — kein `Min`/`Max`
an einem einzigen Zahlenfeld des Editors, keine Prüfung auf 0 < g ≤ 1, keine auf U-Werte.

**Vier Ableitungen beim Übernehmen** (`:804-826`): Maximaltemperatur < 1 → 24;
`Wochenende` = 1 bei Absenkung > 0; `Ferien` = 1 bei Sollwert > 0; `WwBedarf` = 0;
Winterferienbeginn 0 → 366. Dazu `BauweiseNachfuehren()` (`:713-715`) — die **Bauart**
bestimmt seit Entscheid W9-O-2 die `Bauweise` (`Gebaeudebauweise.BauweiseAusBauart`), der
Rückweg beim Laden über `BauartAusBauweise` (`:743-744`).

**Knöpfe — Abweichung vom Hausmuster.** Der Dialog trägt **keine** `SpeichernLeiste`,
sondern drei eigene Knöpfe „Überschreiben", „Speichern unter"/„Speichern" und „Beenden"
(`:360-369`) und **zusätzlich** einen „Werte übernehmen" am Fuß des zweiten Reiters
(`:337-340`). Geschrieben wird **sofort** (`:894-935`); „Beenden" verwirft nichts und
meldet immer `true` (`:950-956`). Die Hausregel lautet dagegen: „Jeder Dialog trägt OK und
Abbrechen — als `SpeichernLeiste`, nie als eigene Knopfzeile … Geschrieben wird im OK-Weg,
sonst ist Abbrechen eine Behauptung, die nicht stimmt" (`EPOS.UI/CLAUDE.md:48-56`). Der
Editor ist ein bewusst wörtlich übernommener Bestand (Kopfkommentar `:15-27`) — aber mit
G1 kommen zehn neue Felder und zehn neue Prüfregeln dazu, und die müssten sonst an **drei**
Schreibstellen hängen.

**Zwei kleinere Befunde am Bestand.** (a) Die Gruppen „Wärmebrücken" und „Anschlussmaße"
benutzen **dieselben drei Beschriftungen** (`:244-249` und `:261-268` lesen beide
`LabelWbvkFenster`/`LabelWbvkKeller`/`LabelWbvkDach`) — nur der Gruppentitel unterscheidet
sie; die eigenen Texte `LabelAnschluss*` (`:980-982`) stehen bloß als *Feldname*.
(b) Die Reihenfolge weicht ab: Wärmebrücken Fenster/Keller/Dach, Anschlussmaße
Fenster/Dach/Keller. Die U·A-Tabelle von E2 räumt beides auf, weil ψ und L in **einer**
Zeile stehen.

### 1.4 `GebaeudeBedarfDialog.razor` — die Auskunft

Kopf mit bedingtem Titel (`:40-50`), Kontextzeile mit dem Gebäudenamen (`:52`),
Einheitenwahl MWh/kWh über `Auswahlfeld` (`:56-57`), drei Kennzahlen in einem Raster
(`:60-84`: Wärmebedarf Heizung, max. Wärmelast [kW], Vollbenutzungsstunden [h/a]), der
Schalter „sortiert" (`:88`), das Bild über `ChartBild` mit Bild- und Datenzoom (`:91-93`)
und die Monatsübersicht, sobald zwölf Werte da sind (`:96-117`). Schluss: eine
`SpeichernLeiste` **ohne** Abbrechen (`:119`), Esc und Enter schließen (`:322-323`).

**Das Bild kommt als Delegat** (`Bildauftrag`, `:134`) und wird nach Schalterstellung und
Ausschnitt zwischengespeichert (`:276-291`); die Bibliothek ruft keinen Renderer. Ein
Schalterwechsel verwirft den Ausschnitt (`:299-303`).

**Umgerechnet wird nur an der Anzeigekante** über `Energieeinheit` (`:246-251`); die
Leistung bleibt kW (`:253-258`).

### 1.5 `GebaeudeWohnflaecheDialog.razor` und `GebaeudetypDialog.razor`

Die Wohnflächenangabe zeigt vier nur lesende Kopffelder (`:48-62`) und die Eingabegruppe
mit Bedarfsart-Klappliste, abgeleiteter Einheit, Verbrauch und Jahresnutzungsgrad
(`:64-89`), Schluss mit regulärer `SpeichernLeiste` (`:97`). Der Gebäudetypdialog führt
Name, Beschreibung (nur lesbar) und 24 Zahlenfelder je Kurve (`:102-119`). **Beide sind
von E1/E2 nicht betroffen** — die Wohnflächenangabe bleibt der Ort der Skalierung
(Konzept 4.7).

### 1.6 Die DTO

`GebaeudeKatalogDaten` (`GebaeudeKatalogDaten.cs`) trägt heute:

- Kopf: `Name`, `Typ`, `Beschreibung`, `Gebaeudeart`, `Verwendung` (**Steuerwert**, nie
  Anzeigetext, `:45-49`), `Baualtersklasse` (int-Index), `Bauart` (int), `Bauweise`
  (double, `:70`);
- Kenngrößen (5): `WohnflaecheGesamt`, `FlaecheNutzer`, `Waermegewinne`,
  `Fensterdurchlassgrad`, `Raumhoehe` (`:74-78`);
- Flächen (7): `FensterflaecheNord`, `FensterflaecheSued`, **`FensterflaecheOstWest`**,
  `FlaecheAussenwand`, `Dachflaeche`, `Grundflaeche`, `SonstigeFlaechen` (`:82-88`);
- U-Werte (5): `UWertAussenwand`, `UWertFenster`, `UWertDachflaeche`,
  `UWertGrundflaeche`, `UWertSonstiges` (`:92-96`);
- Reiter 2: fünf Temperaturen (`:100-104`), drei ψ (`:108-110`), drei L (`:114-116`),
  `Luftwechselrate` (`:120`), `Ferienbeginn[4]`/`Ferienende[4]` (`:128-131`);
- abgeleitet: `Wochenende`, `Ferien`, `WwBedarf`, `SpezWaermeverbrauch`, `Waermebedarf`
  (`:139-154`).

**Alle Zahlen sind `double?`, weil leer etwas anderes ist als 0** (`:13-17`) — genau die
Semantik, die 6.1 für die neuen Spalten verlangt („NULL = Vorgabe"). Das DTO ist damit
vorbereitet; es fehlen nur die Felder.

`GebaeudeBedarfDaten` trägt fünf Größen (Name, `HeizwaermeMwh`, `MaxLastKw`,
`VollbenutzungsstundenH`, `MonatswerteMwh`) und **keine 8 760 Werte** — Bilder laufen über
Delegaten (`GebaeudeBedarfDaten.cs:15-17`).

`GebaeudeKatalogModus` ist ein Dreiwertetyp (Bearbeiten, Neu, Admin) statt zweier Schalter
(`GebaeudeKatalogModus.cs:12-31`); `GebaeudeKatalogErgebnis(Erfolg, Meldung)` ist das
Schreibergebnis (`:40`).

### 1.7 Bausteine und Standards

Benutzt werden aus `EPOS.UI/Bausteine/`: `Zweispaltenauswahl`, `Gruppenkopf`,
`Formularraster`, `Herleitungszeile`, `Reiter`/`Reiterblatt`, `Ueberlagerung`,
`Warnbanner`, `Rueckfrage`, `SpeichernLeiste`, `InfoKnopf`, `Schliesskreuz`, `Zeilenwahl`,
`Optionsgruppe`, `ChartBild`; aus `EPOS.UI/Standards/`: `Textfeld`, `Zahlenfeld`,
`Ganzzahlfeld`, `Auswahlfeld`, `Schalter`.

**`Formularraster`** ist die Pflichtanordnung jedes Parameterblocks
(`EPOS.UI/Bausteine/Formularraster.razor:22-40`, Hausregel `EPOS.UI/CLAUDE.md:105-107`):
Beschriftung neben dem Feld, Zahlenfelder kurz, Einheit unmittelbar dahinter, zwei
Feldpaare je Zeile. Zwei bunit-Fälle halten das für die Gebäudedialoge fest
(`GebaeudeKatalogDialogTests.cs:621`, `GebaeudeWohnflaecheDialogTests.cs:283`).

**`Zahlenfeld`** (`EPOS.UI/Standards/Zahlenfeld.razor`) kennt `Wert` (`double?`),
`Einheit`, `Min`/`Max`, `Nachkommastellen`, `Aktiv`, `Feldname` und `FehlerZustand`
(`:42-93`). Eine Fehleingabe **färbt** das Feld und meldet nicht (`:1-11`); ein Wert
außerhalb `Min`/`Max` gilt wie eine Fehleingabe und wird nicht nach außen gereicht
(`:10-11`). **Ein `Platzhalter` fehlt** — nur `Textfeld` hat einen (benutzt in
`GebaeudeDialog.razor:105`). Für die Anzeige „Vorgabe 0,3" im leeren Feld ist das die eine
fehlende Kleinigkeit (Vorschlag 5.5 und 2.2).

**`Katalogfelder.razor`** (neu im Arbeitsbaum, ungetrackt, 135 Zeilen) ist **nicht** für
den Gebäudedialog gebaut, aber das nächstliegende Muster für eine *generische* Feldtabelle:
Er zeichnet eine Liste `BrowserFeldwert` in einem `Formularraster` und wählt je Feld die
Bauform nach `BrowserFeldArt` — `Schalter` (`:41-46`), `Zahlenfeld` (`:47-53`),
`Ganzzahlfeld` (`:54-60`), sonst `Textfeld` (`:61-67`). Er kennt **weder Datenbank noch
Controller** (`:20-23`); zwei Sperren greifen: `NurLesen` für die ganze Maske und
`BrowserFeldwert.Editierbar` für das einzelne Feld (`:76-100`). Jede Änderung meldet
`Geaendert`, ein ungültiger Zahlenzustand `FehlerZustand` (`:91-97`); **wann** gespeichert
wird, entscheidet der Wirt (`:22-23`). Herkunft und Zweck stehen im Kopfkommentar
(`:1-29`): Er ist der gemeinsame Baustein von Katalogbrowser und Modulbereich, seit dem
Anwenderentscheid vom 15.09.2026.

> **Folgerung für E2.** Die U·A-Tabelle ist *keine* Anwendung von `Katalogfelder` — sie ist
> eine **Tabelle mit gerechneter Spalte**, kein Feldraster, und ihre Zeilen sind
> Bauteilgruppen, keine Katalogspalten. Aber die zwei Regeln des Bausteins gelten
> unverändert: je Feld seine Bauform, zwei Sperren (Maske und Feld), `Geaendert` und
> `FehlerZustand` nach außen. Ab G3/G4 (Bauteilkatalog, IFC-Herkunft) wird die U·A-Tabelle
> zeilenvariabel — dann lohnt ein eigener Baustein `Huellzeilen.razor` nach genau diesem
> Vorbild.

### 1.8 Wie Speichern läuft

**Der Weg ist dreistufig: Komponente → Hülle → Kern-Controller.**

1. Der Dialog ruft den Delegaten `Speichern(GebaeudeKatalogDaten, istNeu, bezeichner)`
   (`GebaeudeKatalogDialog.razor:398-399`, gerufen in `:918-935`).
2. Die Hülle `WindowsFormsApplication1/Views/Gebäude/GebaeudeKatalogHuelle.cs` hängt ihn
   ein (`:115-116`) und führt ihn in `Schreiben(...)` aus (`:318-337`): ReadOnly-Sperre in
   der **Hülle**, nicht im Controller (`:313-317`, `:323-326`), dann `AusModell`/`NachModell`
   (`:340`, `:416`) und `GebaeudeStammCtrl.Insert` bzw. `Overwrite` (`:334`).
3. Der Kern-Controller ist `EPOS.Kern/Controller/GebaeudeStammCtrl.cs`
   (`Tab_Gebaeude_STAMM`); die Projektseite liegt in
   `EPOS.Kern/Controller/ProjektGebaeudeCtrl.cs` und `GebaeudeCtrl.cs`, das Modell in
   `EPOS.Kern/Model/GebaeudeModel.cs` (55 Felder) bzw. `ProjektGebaeudeModel.cs`.

**Die Feldabbildung** steht in `GebaeudeKatalogHuelle.NachModell` (`:416-505`) und kennt
drei Eigenheiten, die für G1 zu beachten sind:

- `Flaeche_Nutzer == 0 → 35` samt Bewohnerzahl daraus (`:429-431`);
- `gesamte_Fensterflaeche = Süd + Ost + Nord` (`:453-454`) — **die abgeleitete Summe, an
  der die Ost/West-Trennung hängt**;
- `Bauweise` kommt aus dem Dialog, nicht mehr aus der Gebäudeart (`:437-440`).

**`EPOS.UI.Daten` führt für Gebäude heute nichts.** Der Ordner `EPOS.UI.Daten/Bedarf/`
enthält allein `BedarfErgebnisHuelle.cs`; die drei Gebäudehüllen
(`GebaeudeHuelle.cs` 483 Z., `GebaeudeKatalogHuelle.cs` 554 Z.,
`GebaeudeWohnflaecheHuelle.cs` 134 Z.) liegen sämtlich in der Windows-Schale unter
`WindowsFormsApplication1/Views/Gebäude/`.

**Der Parametersatz** entsteht in `GebaeudeHuelle.Gaben(...)`
(`WindowsFormsApplication1/Views/Gebäude/GebaeudeHuelle.cs:110-222`) — 13 Delegaten und
rund 30 Texte; die Fachliste wird nach jeder Änderung **an Ort und Stelle** neu aufgebaut
(`:122-126`). Zwei Wachen hängen daran: `EPOS.UI.Tests/ParametersatzTests.cs` liest den
Quelltext der Hüllen und löst die Gegenseite per Reflexion auf (`:12-38`), die Gegenwache
am Gerät steht in `WindowsFormsApplication1/Allgemein/Blazor/Parametersatzwache.cs`
(`ParametersatzTests.cs:33-36`). **Jeder neue Schlüssel muss also einem `[Parameter]`
entsprechen**, sonst bricht der erste Zeichenlauf.

### 1.9 Wie der Bedarfsdialog rechnet und Bilder liefert

`EPOS.Kern/Controller/GebaeudeBedarfCtrl.cs` (164 Z.) ist die **Auskunft, nicht die
Rechnung** (`:49-59`): `Rechnen(idProjekt, idKlimaregion, idZ)` (`:94-136`) sucht über
`Tab_Gebaeude.ID_ProjektGebaeude` die Zeile (`:99`, `:147-156`), liest sie mit
`ProjektGebaeudeCtrl.ReadAll` (`:102-109`) und ruft **dieselben zwei Methoden wie der
Lauf** — `SimulationWaermebedarf.KlimakalenderLesen` (`:112`) und
`HeizwaermeEinesGebaeudes` (`:117`). Danach `BhkwPlan.WattToKw` (`:121`),
Jahressumme **zeichengleich zum Lauf** (`werte.Sum() / 1000`, `:126-130`), Höchstlast
(`:131`), zwölf Monatswerte über `BhkwPlan.MonatsSumme` (`:132-133`). Das ist die
Hausregel „eine Auskunft ruft den Rechenweg des Laufs — sie schreibt ihn nicht ab"
(`EPOS.Kern/CLAUDE.md`, Abschnitt gleichen Namens).

Das Ergebnis reicht `GebaeudeHuelle.Bedarfsgaben(...)` als eingefrorenes DTO herein
(`GebaeudeHuelle.cs:326-378`); ohne Klimaregion oder ohne gespeicherte Zeile liefert sie
`null`, und die Komponente meldet das (`:329`, `:336`; Text `:216-218`). **Das Bild**
zeichnet `Bedarfsbild(...)` (`:391-416`) über `ChartRenderer.GanglinieNormiert` im Kern —
dieselbe Bauform wie Bild B1 der Ergebnisseite (`:380-386`).

### 1.10 Abweichungen vom Hausmuster — die Liste

| # | Befund | Stelle | Folge für G1 |
|---|---|---|---|
| M-1 | Der Editor hat keine `SpeichernLeiste`; drei Schreibknöpfe plus „Werte übernehmen" | `GebaeudeKatalogDialog.razor:337-340`, `:360-369` gegen `EPOS.UI/CLAUDE.md:48-56` | Die zehn neuen Prüfregeln aus 4.8 brauchen **eine** Stelle; sonst drei |
| M-2 | Reiter 2 führt einen zweiten, eigenen Stand (`:549-558`), der erst mit „Übernehmen" in den Satz wandert (`:788-829`) | dito | Die U·A-Zeilen stehen heute auf **beiden** Reitern (U auf 1, ψ/L auf 2) — die Summe wäre bis zum „Übernehmen" falsch |
| M-3 | Keine Wertebereiche an den Zahlenfeldern des Editors | `:123-199`, `:218-232` | 4.8 verlangt Bereiche für g, U, Bauweise, Ferientage |
| M-4 | ψ und L stehen in zwei Gruppen mit denselben Beschriftungen und verschiedener Reihenfolge | `:244-249` gegen `:261-268` | E2 führt sie in einer Zeile zusammen |
| M-5 | Der englische Wert `GEBK_GRP_UWERTE` trägt zwei unsichtbare Steuerzeichen | `EPOS.Kern/MyResource/Resource.en-US.resx:13294` („U-values ​​[W/m²K]") | beim Anfassen der Gruppe bereinigen |
| M-6 | `Fensterflaeche_Ost` heißt im Modell Ost, meint aber Ost **und** West | `GebaeudeKatalogHuelle.cs:364`, `:443`; Konzept 6.1 („Namensfalle") | Umbenennung in `Fensterflaeche_OstWest` gehört zu G1 |

---

## 2. Das Vorbild: ein Modellschalter im Dialog

### 2.1 `PvModellFelder.razor` — die eine Vorlage

Der PV-Zweig löst genau die Aufgabe von 8.1, und zwar in **104 Zeilen**
(`EPOS.UI/Dialoge/Erzeuger/PvModellFelder.razor`):

- **Die Klappliste** ist ein gewöhnliches `Auswahlfeld` über zwei Einträge, die in
  `OnParametersSet` aus den Texten gebaut werden (`:42-43`, `:80-83`); der Stand ist ein
  `bool` an der Zeile (`Zeile.ModellErweitert`), und `BeiModell` setzt ihn und meldet
  `Geaendert` (`:85-89`).
- **Bedingte Felder** laufen nicht über `@if`, sondern über `Aktiv` am Feld: der
  Wirkungsgrad bleibt im Modell „Einfach" frei und ist in „Erweitert" **gesperrt sichtbar**
  (`:48`). Der Kopfkommentar begründet das ausdrücklich: „Ein gesperrter Knopf ohne
  sichtbaren Grund liest sich als Fehler" (`:13-18`) — die Sperre ist zulässig, **wenn**
  eine Zeile daneben den Grund nennt.
- **Die erklärende Zeile** unter der Klappliste sagt, was die Modellwahl unterscheidet
  *und was sie nicht unterscheidet* (`:56-57`, Prüfhilfe `:72-78`). Sie ist eine
  `epos-herleitung`-Zeile, kein Banner.
- **„Leer = NULL = Vorgabe"** steht an genau einer Stelle je Feld und ist kommentiert:
  „Leer = NULL = 0,95 (Bestand). Ein Wert außerhalb 0..1 ist eine Fehleingabe, die das Feld
  färbt; übernommen wird er nicht." (`:91-97`, dieselbe Form `:99-103`). Der Dialog
  schreibt also **`null`**, nicht die Vorgabe — den Vorgabewert kennt nur der Kern.

Eingebunden wird der Baustein als ein Tag im Anlagenblock
(`EPOS.UI/Dialoge/Erzeuger/PhotovoltaikDialog.razor:171`); die Texte kommen als **ein**
Bündel `PvModellTexte` (`EPOS.UI/Dialoge/Erzeuger/PvModellDaten.cs:22`), wie es die
Hausregel ab etwa zehn Anzeigetexten verlangt (`EPOS.UI/CLAUDE.md:22-24`).

### 2.2 Das Muster „Vorgabewert anzeigen, NULL speichern"

Drei Ausprägungen stehen im Bestand:

1. **Leise Zeile mit eingesetzter Zahl.** `EPOS.UI/Dialoge/Erzeuger/BhkwDialog.razor:397-398`
   führt `HerleitungVorgabe = "0 = Projektvorgabe ({0} %)"` und setzt die Projektvorgabe
   zur Laufzeit ein (`:670-688`). Das ist die genaue Bauform für „Vorgabe 0,3" aus 8.1 —
   nur mit `0` statt leer als Merkzeichen.
2. **Sperren statt Verstecken.** `PvModellFelder.razor:48` (siehe oben).
3. **Rückfallwert im Kern, `null` im DTO.** `PvModellFelder.razor:93-96`; dieselbe
   Semantik verlangt Konzept 6.1 für alle elf neuen Spalten.

**Was fehlt:** Ein `Zahlenfeld` kann heute keinen Platzhalter zeigen — die Vorgabe müsste
also entweder in eine `Herleitungszeile` (Muster 1, funktioniert sofort) oder das
`Zahlenfeld` bekommt einen `Platzhalter`-Parameter wie das `Textfeld`. Der zweite Weg ist
sauberer (eine Vorgabe je Feld statt einer Zeile je Feld), kostet aber einen Eingriff in
`EPOS.UI/Standards/Zahlenfeld.razor` samt `StilblattTests`-Durchlauf. **Empfehlung:**
`Platzhalter` am `Zahlenfeld` ergänzen; er trägt den Text „Vorgabe 0,3", das Feld bleibt
leer, und der Dialog schreibt `null`.

---

## 3. Ressourcen

### 3.1 Wie ein Text entsteht

Zwei Dateien, beide Pflicht: `EPOS.Kern/MyResource/Resource.resx` (neutral, deutsch) und
`EPOS.Kern/MyResource/Resource.en-US.resx`. Beide sind UTF-8 **mit** BOM und CRLF
(Wurzel-`CLAUDE.md`, Abschnitt „Bauen und prüfen"). Ein Eintrag ist ein
`<data name="…" xml:space="preserve"><value>…</value></data>`-Block; die Einträge stehen
**alphabetisch** (Beispiel `Resource.resx:13300-13311`).

Danach **immer** `python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben` —
`Resource.Designer.cs` ist erzeugter, eingecheckter Quelltext und wird nie von Hand
ergänzt (`EPOS.Kern/CLAUDE.md`, „Regeln für Änderungen hier"; Wurzel-`CLAUDE.md`,
Werkzeugtabelle: „Nach jedem neuen Ressourcenschlüssel ziehen").

In der Oberfläche steht der Text **nie** direkt: Die Komponente führt einen
`[Parameter] string` mit deutschem Rückfallwert, die Hülle belegt ihn über
`Text_("SCHLUESSEL", "deutscher Rückfall")` (`GebaeudeHuelle.cs:164-220`, `:475`). Ab etwa
zehn Texten ein **Bündel** statt einzelner Parameter (`EPOS.UI/CLAUDE.md:22-24`) — bei
zehn neuen Feldern und einer achtzeiligen Tabelle ist diese Schwelle klar überschritten.

### 3.2 Die Schlüsselnamen im Gebäudebereich

Vier Präfixe, streng getrennt:

| Präfix | Dialog | Beispiele |
|---|---|---|
| `GEB_` | `GebaeudeDialog` (Wirt) | `GEB_GRP_FILTER`, `GEB_BTN_SIMULATION`, `GEB_MSG_KEIN_BEDARF` |
| `GEBK_` | `GebaeudeKatalogDialog` (Editor) | `GEBK_GRP_UWERTE`, `GEBK_LBL_U_FENSTER`, `GEBK_FELD_U_FENSTER`, `GEBK_MSG_ZAHL` |
| `GEBB_` | `GebaeudeBedarfDialog` | `GEBB_TITEL`, `GEBB_GRP_KENNZAHLEN`, `GEBB_LBL_VOLLBENUTZUNG` |
| `GEBW_` | `GebaeudeWohnflaecheDialog` | `GEBW_LBL_ART_ANGABE` |

Innerhalb des Präfixes die Sorte: `TITEL`, `REITER_*`, `GRP_*` (Gruppentitel), `LBL_*`
(Beschriftung, **mit** Doppelpunkt und Leerzeichen davor im Editor — „Außenwand :"),
`FELD_*` (der Feldname der Pflichtmeldung, **ohne** Doppelpunkt), `SP_*` (Spaltenkopf),
`BTN_*`, `MSG_*`, `HINWEIS_*`, dazu Wertelisten ohne Sortenkürzel (`GEBK_BAUART_LEICHT`,
`GEBK_FERIEN_WINTER`, `GEBK_VERWENDUNG_WOHN`). **`LBL_` und `FELD_` sind bewusst zwei
Schlüssel für dasselbe Feld** (`GebaeudeKatalogDialog.razor:958-961`) — die eine Fassung
steht am Feld, die andere in der Meldung „Bitte {0} als Zahl eingeben."

Der Gebäudebereich führt heute rund **95** `GEBK_`-Schlüssel (`Resource.resx:13180-13460`).

### 3.3 Das Glossar — und seine Lücke

`Dokumentation/aktuell/Glossar_Lokalisierung.md` ist verbindlich: „Ein deutscher
Fachbegriff hat genau **eine** englische Entsprechung … Konsistenz geht vor Eleganz"
(`:8-11`). Aus dem Bestand direkt verwendbar:

| DE | EN | Fundstelle |
|---|---|---|
| Erdreich | ground (**nicht** „soil") | Glossar § 3 |
| Außenluft | outdoor air (**nicht** „ambient air") | Glossar § 3 |
| Wärmedurchgangskoeffizient (U-Wert) | heat transfer coefficient | Glossar § 5 |
| Wärmeleitfähigkeit | thermal conductivity | Glossar § 5 |
| Heizlast | heat load | Glossar § 2 |
| Stundenwerte | hourly values | Glossar § 7 |
| Vorgabewert / Standardwert | default value | Glossar § 9 |
| Plausibilitätsprüfung | plausibility check | Glossar § 9 |
| Ungültiger Wert | Invalid value | Glossar § 8 |
| Pflichtfeld | Mandatory field | Glossar § 8 |
| Außentemperatur | outdoor temperature | Glossar § 7 |

**Das Glossar kennt die Gebäudehülle nicht.** Es fehlen: Wärmebrücke, Verschattung,
Verschattungsfaktor, Rahmenanteil, Bauteil, Außenwand, Bodenplatte, Dach, Keller,
Randbedingung, Transmission, Lüftungsleitwert, Raumtemperatur, operative Temperatur,
Kühlbedarf, Bauweise, Bauart, Rechenmodell, Tagesbilanz. **Vorschlag: ein Abschnitt
„13. Gebäudehülle und Gebäudemodell" im Glossar, bevor die en-US-Werte geschrieben
werden** — sonst entstehen zwei Übersetzungen desselben Begriffs (die Regel, die § 12
für die `KONFIG_*`-Schlüssel eigens einfrieren musste). Vorschlagsliste:

| DE | EN | Quelle |
|---|---|---|
| Wärmebrücke | thermal bridge | EN ISO 10211 |
| Wärmebrückenverlustkoeffizient | linear thermal transmittance | EN ISO 14683 (ψ) |
| U-Wert | U-value | Kurzform zu „heat transfer coefficient", in Spaltenköpfen |
| Verschattung / Verschattungsfaktor | shading / shading factor | ISO 13790 |
| Rahmenanteil | frame fraction | ISO 13790 |
| Gesamtenergiedurchlassgrad (g-Wert) | total solar energy transmittance | EN 410 |
| Bauteil | building component | — |
| Außenwand | external wall | — |
| Bodenplatte | ground floor slab | — |
| Dach | roof | — |
| Keller (unbeheizt) | unheated basement | — |
| Randbedingung | boundary condition | — |
| Transmissionswärmeverlust / H_T | transmission heat loss | EN 12831 |
| Lüftungswärmeverlust / H_ve | ventilation heat loss | EN 12831 |
| Raumtemperatur | indoor temperature | — |
| operative Temperatur | operative temperature | VDI 6007 / EN ISO 7726 |
| Kühlbedarf | cooling demand | — |
| Bauweise / Bauart | thermal mass class | ISO 13790 Tab. 12 |
| Rechenmodell | calculation model | — |
| Tagesbilanz | daily balance | — |

---

## 4. Tests

### 4.1 Was heute geprüft wird

Fünf bunit-Klassen, 2 384 Zeilen, **119 Fälle**:

| Datei | Zeilen | Fälle |
|---|---|---|
| `EPOS.UI.Tests/Dialoge/GebaeudeDialogTests.cs` | 803 | 40 |
| `EPOS.UI.Tests/Dialoge/GebaeudeKatalogDialogTests.cs` | 633 | 33 |
| `EPOS.UI.Tests/Dialoge/GebaeudetypDialogTests.cs` | 347 | 17 |
| `EPOS.UI.Tests/Dialoge/GebaeudeBedarfDialogTests.cs` | 309 | 14 |
| `EPOS.UI.Tests/Dialoge/GebaeudeWohnflaecheDialogTests.cs` | 292 | 15 |

**Das Muster** (`GebaeudeKatalogDialogTests.cs`): Klasse erbt `EposBunitContext`,
`JSInterop.Mode = Loose`, `IHilfeDienst` als Attrappe (`:33-37`); die **Kultur ist auf
de-DE gepinnt**, weil die Erwartungswerte deutsche Beschriftungen sind und der
Windows-Läufer englisch läuft (`:17-19`). Ein vollständig belegter Satz steht als
statische Fabrik `Satz(name)` (`:39-72`), der Aufbau als eine `Aufbauen(...)`-Methode mit
allen Delegaten (`:74-93`); Hilfsgriffe sind `Knopf(cut, text)` (`:95-96`) und
`ReiterWaehlen` über `button[role=tab]` (`:98-99`).

**Die Fallklassen**, an denen sich die neuen Fälle orientieren:

- **Feldbestand je Reiter** — gezählt wird über `input[inputmode=decimal]`, `select`,
  `textarea`, geprüft über `Assert.Contains(text, cut.Markup)`
  (`:105-125`, `:138-158`). Der erste Reiter hat heute **17** Zahlenfelder, **5**
  Klapplisten, **1** `textarea` (`:111-115`).
- **Betriebsarten** — Bearbeiten/Neu/Admin und ihre Knopfsperren (`:174-217`).
- **Listeninhalte** — 21 Baualtersklassen (`:219`), drei Bauarten (`:245`), Steuerwert
  getrennt vom Anzeigetext (`:233`).
- **Ableitungen** — Bauweise aus Bauart × Wohnfläche (`:267`, `:305`), Rundweg beim Laden
  (`:329`), die vier Ferienableitungen (`:422-485`).
- **Meldungen** — fehlende Pflichtzahl nennt ihren Feldnamen (`:350`), leerer Name (`:369`),
  abgelehnte Schreibung bleibt als Warnbanner stehen (`:406`).
- **Ohne Übernehmen bleibt der Satz unberührt** (`:486`).
- **Tastatur und Kreuz** — Esc, Beenden, Kreuz, Überlagerungskreuz, „ohne Titel kein
  Kreuz" (`:528-608`).
- **Anordnung** — die Blöcke stehen im `Formularraster` (`:621`).

Beim Bedarfsdialog dazu: Platzhalter ohne Bildauftrag (`:144`), Neuzeichnen beim Schalter
(`:162`), Bereich geht in den Bildauftrag (`:185`), Schalterwechsel verwirft den Ausschnitt
(`:204`), Einheitenwahl wird gemeldet (`:226`), Höchstlast bleibt kW (`:248`).

### 4.2 Proben und die Rasterprobe-Regel

`Proben/Rasterprobe` misst **allein die virtualisierte `Katalogliste`** (QuickGrid
`Virtualize`) im echten Browser — Zeilenhöhe, Abstandshalter, Rollbehälter,
Sichtbarkeitsmelder (`Proben/Rasterprobe/LIESMICH.md:1-17`). Die Regel der
Wurzel-`CLAUDE.md` lautet wörtlich: **„Vor jeder Änderung an `Raster`, `Katalogliste` oder
den `.epos-raster*`-Regeln ziehen"**.

> **Gilt sie für Eingabedialoge? Nein — mit einer Einschränkung.** Die Gebäudedialoge
> benutzen `Katalogliste`/`Raster` nicht; sie zeichnen ihre Listen als schlichte
> `<table class="epos-raster">` in einer `.epos-raster-huelle`
> (`GebaeudeDialog.razor:69-92`, `:110-133`; `GebaeudeBedarfDialog.razor:62-82`). Die
> **Einschränkung**: Die U·A-Tabelle wird genau so eine `table.epos-raster` sein. Wer beim
> Umbau eine `.epos-raster*`-Regel im Stilblatt anfasst — etwa für die rechtsbündige
> Zahlenspalte oder die Summenzeile —, zieht die Rasterprobe. Wer nur Markup hinzufügt,
> nicht. Für die Sichtabnahme unter Windows bleibt es bei der Regel aus Konzept 11
> (Stufe G2: „ChartProben grün; Sichtabnahme Windows").

`Proben/ChartProben` ist dagegen **Pflicht**, sobald das neue Bild „Raumtemperatur"
entsteht (Konzept 9 und 11; `EPOS.Kern/CLAUDE.md`, Abschnitt „Bericht": ein neuer
Parameter bekommt eine Vorgabe, die das Bild byte-gleich lässt).

---

## 5. Soll-Entwurf

### 5.1 Die Leitgedanken

1. **Eine Wahrheit je Größe.** U, A, ψ, L stehen **nur** in der U·A-Tabelle — nicht
   zusätzlich in „Flächen" und „U-Werte". Die drei alten Gruppen verschwinden in einer.
2. **Die Rechnung liegt im Kern, nicht im Dialog.** Eine reine Hilfsklasse
   `EPOS.Kern/Allgemein/Gebaeudehuellbilanz.cs` nach dem Vorbild
   `EPOS.Kern/Allgemein/Gebaeudebauweise.cs` und `Ferienzeit` liefert Zeilen und Summen;
   der Dialog zeigt sie nur an, der Eingangsbauer des Modells benutzt dieselbe Klasse.
   Begründet ist dieses Vorgehen schon einmal: „Die Rechnung selbst steht — wie Ferienzeit
   und Suchmuster — als reine Hilfsklasse im Kern; ein Controller wird von dieser
   Komponente nicht angefasst" (`GebaeudeKatalogDialog.razor:43-45`).
3. **Der Dialog schreibt `null`, nicht die Vorgabe** — die Vorgabe zeigt er nur an
   (`PvModellFelder.razor:93-96`; Konzept 6.1).
4. **Ein Schreibweg.** Die drei Knöpfe und das „Werte übernehmen" weichen einer
   `SpeichernLeiste`; die Prüfregeln stehen genau einmal im OK-Rückruf
   (`EPOS.UI/CLAUDE.md:48-56`).

### 5.2 Der Editor im Soll — Gruppen und Felder in Reihenfolge

**Reiter 1 „Gebäude und Hülle"**

*Gruppe 1 — „Kenngrößen"* (unverändert bis auf einen Zugang)

| Feld | Einheit | Bindung | Vorgabe | Pflicht | Plausibilitätsregel (Konzept 4.8) | Sichtbar bei |
|---|---|---|---|---|---|---|
| Name | — | `Daten.Name` (Admin: Klappliste) | — | ja beim Anlegen | nicht leer | beide |
| Gebäudetyp | — | `Daten.Typ` | — | nein | — | beide |
| Beschreibung | — | `Daten.Beschreibung`, 3-zeilig | — | nein | — | beide |
| Gebäudeart | — | `Daten.Gebaeudeart` | — | nein | — | beide |
| Baujahr (Klasse) | — | `Daten.Baualtersklasse` | — | nein | — | beide |
| Verwendung | — | `Daten.Verwendung` (Steuerwert) | Wohngebaeude | nein | — | beide |
| Bauart | — | `Daten.Bauart` → `Bauweise` | schwer | nein | 5 ≤ Bauweise/Wohnfläche ≤ 200 Wh/(m²K) | beide |
| Wohn-/Nutzfläche | m² | `WohnflaecheGesamt` | — | **ja** | > 0 | beide |
| Fläche / Nutzer | m² | `FlaecheNutzer` | 35 (Hülle) | **ja** | > 0 | beide |
| Interne Wärmegewinne | W | `Waermegewinne` | — | **ja** | ≥ 0 | beide |
| Fensterdurchlaßgrad (g) | — | `Fensterdurchlassgrad` | — | **ja** | 0 < g ≤ 1 | beide |
| Raumhöhe | m | `Raumhoehe` | — | **ja** | > 0 | beide |
| Luftwechselrate | 1/h | `Luftwechselrate` *(von Reiter 2 hierher)* | — | **ja** | > 0 | beide |

> Die Luftwechselrate wandert nach vorn, weil H_ve aus ihr entsteht und in der Summenzeile
> derselben Ansicht steht. Heute liegt sie in „Sonstiges" auf Reiter 2
> (`GebaeudeKatalogDialog.razor:322-324`).

*Gruppe 2 — „Hülle: Transmission je Bauteil"* — die U·A-Tabelle, siehe 5.3.
*Gruppe 3 — „Wärmeleitwerte"* — die Summen, siehe 5.4.
*Gruppe 4 — „Fenster nach Orientierung"* — siehe 5.5.
*Gruppe 5 — „Rechenmodell"* — siehe 5.6.

**Reiter 2 „Temperaturen und Ferien"** — unverändert: Raumtemperaturen (5 Felder), Ferien
Anfang (4 Paare), Ferien Ende (4 Paare), der Knopf „Brauchwasser…". Die beiden Gruppen
„Wärmebrückenverlustkoeffizienten" und „Abmessung Anschluß" **entfallen** — ihre sechs
Werte stehen jetzt in der U·A-Tabelle. Der eigene Stand des Reiters (M-2) und der Knopf
„Werte übernehmen" entfallen mit; geschrieben wird im OK-Weg. Die Plausibilitätsregel
„Ferientage 1…365" (4.8) tritt neben die vier Ferienregeln des Bestands.

### 5.3 Die U·A-Tabelle

Eine `<table class="epos-raster">` in einer `.epos-raster-huelle`, acht Zeilen
(fünf Bauteile, drei Wärmebrücken), Spalten:

| Spalte | Inhalt | Bauform |
|---|---|---|
| Bauteil | fester Zeilentext | `<th scope="row">` |
| U bzw. ψ | W/(m²K) bzw. W/(mK) | `Zahlenfeld`, `Min="0.1" Max="6"` bei U, `Min="0" Max="2"` bei ψ |
| A bzw. L | m² bzw. m | `Zahlenfeld`, `Min="0"` |
| Randbedingung | Außenluft \| Erdreich \| Keller | `Auswahlfeld`, nur Zeile „Bodenplatte" wählbar, sonst fester Text |
| U·A | W/K, **gerechnet, nur Anzeige** | `<td class="epos-zahl">` |
| Herkunft | manuell \| Katalog \| IFC | **ab G4**, sonst Spalte nicht gezeichnet |

Die Zeilen:

| Zeile | U bzw. ψ | A bzw. L | Randbedingung |
|---|---|---|---|
| Außenwand | `UWertAussenwand` | `FlaecheAussenwand` | Außenluft (fest) |
| Fenster | `UWertFenster` | **Summe** aus Nord + Süd + Ost + West, nur Anzeige | Außenluft (fest) |
| Dach | `UWertDachflaeche` | `Dachflaeche` | Außenluft (fest) |
| Bodenplatte | `UWertGrundflaeche` | `Grundflaeche` | **`GrundflaecheRandbedingung`** — Erdreich (Vorgabe), Keller, Außenluft |
| Sonstiges | `UWertSonstiges` | `SonstigeFlaechen` | Außenluft (fest) |
| Wärmebrücke Fenster–Wand | `WbvkFensterWand` | `AnschlussFensterWand` | — |
| Wärmebrücke Außenwand–Keller | `WbvkAussenwandKeller` | `AnschlussAussenwandKeller` | — |
| Wärmebrücke Wand–Dach | `WbvkWandDach` | `AnschlussWandDach` | — |

**Die Randbedingung der Bodenplatte steht genau hier und nirgends sonst** — sie ist die
Spalte, nicht ein zwölftes Feld der Modellgruppe (gegen die Aufzählung in 8.1, aber im
Sinne von E2: „Je Bauteilgruppe … eine Zeile mit U bzw. ψ, A bzw. L, dem Produkt U·A und
der Randbedingung").

**Die Fensterfläche ist in der Tabelle nur lesbar**, weil sie aus vier Feldern der Gruppe
„Fenster nach Orientierung" entsteht (5.5). Das ist dieselbe Regel wie heute für
`gesamte_Fensterflaeche` (`GebaeudeKatalogHuelle.cs:453-454`) — nur sichtbar gemacht.

**Sichtbarkeit:** in **beiden** Rechenmodellen. Die Tabelle ist die gemeinsame
Zielstruktur (E2), und auch im Tagesbilanz-Weg sind U, A, ψ, L die Eingaben.

**Zwei Plausibilitätsregeln hängen an der Tabelle** (Konzept 4.8): U-Werte zwischen 0,1
und 6 W/(m²K) — als `Min`/`Max` am Feld **und** als Meldung beim OK — und
R_Rest,AW > 0, also mittleres U der opaken Bauteile unter 4,17 W/(m²K); die zweite kann
erst die Hilfsklasse rechnen und meldet benannt beim Speichern.

### 5.4 Die Summen

Drei Zeilen unter der Tabelle, in einer eigenen Gruppe „Wärmeleitwerte" oder als
`<tfoot>`:

| Größe | Rechnung | Einheit |
|---|---|---|
| **H_T** | Σ (U·A)_Bauteile + Σ (ψ·L)_Wärmebrücken | W/K |
| **H_ve** | `Luftwechselrate` · `WohnflaecheGesamt` · `Raumhoehe` · 0,34 Wh/(m³K) | W/K |
| **H_ges** | H_T + H_ve | W/K |

Der Faktor 0,34 ist der des Stundenmodells (Konzept 4.4); der Bestand rechnet mit
0,3333 Wh/(m³K) (`EPOS.Kern/Allgemein/BhkwPlan.cs:357/359`: `1,2 · 0,2777…`).

**Der gewichtete Vergleichswert** erscheint **nur, wenn das Rechenmodell auf Tagesbilanz
steht** — eine vierte Zeile:

| Größe | Rechnung | Fundstelle |
|---|---|---|
| H_T (Tagesbilanz, gewichtet) | 0,83·U_w·A_w + U_f·A_f + 0,95·U_d·A_d + 0,45·U_g·A_g + U_s·A_s + 0,83·Σψ·L | `BhkwPlan.cs:347-353` |
| H_ve (Tagesbilanz) | `Wohnfläche · Raumhöhe · 1,2 · n · 0,2777…` (= 0,3333 Wh/(m³K)) | `BhkwPlan.cs:357/359` |

Darunter eine `Herleitungszeile`: „Der Tagesbilanz-Weg wichtet Außenwand und Wärmebrücken
mit 0,83, das Dach mit 0,95 und die Bodenplatte mit 0,45. Das Stundenmodell rechnet
ungewichtet." Damit ist der Unterschied je Gebäude erklärbar, wie E2 es verlangt
(Konzept N1.6) — **ohne** dass eine Zahl zweimal gerechnet wird: beide Werte kommen aus
derselben Hilfsklasse (5.1, Punkt 2), die den gewichteten Zweig mit denselben Konstanten
führt wie `SpezWaermeverlusteC`.

**Die Kennzahl H_T wandert zusätzlich in `KennzahlenKatalog.cs` und den Bericht**
(Konzept N1.6, letzter Punkt).

### 5.5 Fenster Ost und West getrennt

*Gruppe „Fenster nach Orientierung"*, vier Eingaben und zwei Anzeigen:

| Feld | Einheit | Bindung | Vorgabe | Pflicht | Plausibilitätsregel | Sichtbar |
|---|---|---|---|---|---|---|
| Fensterfläche Nord | m² | `FensterflaecheNord` | — | **ja** | ≥ 0 | beide |
| Fensterfläche Süd | m² | `FensterflaecheSued` | — | **ja** | ≥ 0 | beide |
| Fensterfläche Ost | m² | **`FensterflaecheOst` (neu)** | ½ der Summe Ost+West | nein | ≥ 0 | beide |
| Fensterfläche West | m² | **`FensterflaecheWest` (neu)** | ½ der Summe Ost+West | nein | ≥ 0 | beide |
| Summe Ost + West | m² | **gerechnet**, nur Anzeige | — | — | schreibt `FensterflaecheOstWest` mit | beide |
| gesamte Fensterfläche | m² | **gerechnet**, nur Anzeige | — | — | = Summe der vier; geht in die U·A-Zeile „Fenster" | beide |

**Die Regel „eine Wahrheit im Dialog, zwei Leser im Kern"** (Konzept 6.1): Der Dialog
pflegt Ost und West, schreibt aber die **Summe** nach `Fensterflaeche_OstWest` mit, damit
`SolareGewinneC` auf dem Tagesbilanz-Weg unverändert rechnet. Beide Felder leer heißt: je
die Hälfte der Summe — das ist die Vorgabe des **Kerns**, nicht des Dialogs; der Dialog
schreibt `null`.

**Die Plausibilitätsregel aus 4.8** „Summe der Fensterflächen = `gesamte_Fensterflaeche`"
wird damit **erfüllbar statt prüfbar**: Die Gesamtfläche ist gerechnet, nicht eingegeben.
Die Prüfung bleibt trotzdem als Wache im Kern, für Datensätze aus dem Bestand und aus dem
IFC-Import.

*Ost und West sind in beiden Modellen sichtbar*, weil die Summe in beiden gebraucht wird;
die getrennte Wirkung hat nur das Stundenmodell (Konzept 5.12).

### 5.6 Die Gruppe „Rechenmodell"

| Feld | Einheit | Bindung (neue Spalte 6.1) | Vorgabe-Anzeige | Pflicht | Plausibilitätsregel | Sichtbar |
|---|---|---|---|---|---|---|
| Rechenmodell | — | `Modell` (`Gebaeude_Modell`); **NULL = VDI 6007** (E1) | — | nein | — | immer |
| *(Herleitungszeile)* | — | — | „VDI 6007 (stündlich): Raumtemperatur, Kühlbedarf und Spitzenlast je Stunde." / „Tagesbilanz: der Bestandsweg mit gewichteten Bauteilen, ohne Raumtemperatur." | — | — | immer |
| Rahmenanteil | — | `Rahmenanteil` | Vorgabe 0,3 | nein | 0 ≤ x < 1 | VDI 6007 |
| Verschattungsfaktor | — | `Verschattungsfaktor` | Vorgabe 0,9 | nein | 0 < x ≤ 1 | VDI 6007 |
| Masseanteil außen | — | `Masseanteil_Aussen` | Vorgabe 0,3 | nein | 0,05 ≤ x ≤ 0,95 | VDI 6007 |
| Innenflächenfaktor | — | `Innenflaechenfaktor` | Vorgabe 2,5 | nein | 0,5 ≤ x ≤ 10 | VDI 6007 |
| Strahlungsanteil Heizung | — | `Heizung_Strahlungsanteil` | Vorgabe 0,3 | nein | 0 ≤ x ≤ 1 | VDI 6007 |
| Heizleistungsgrenze | kW | `Heizleistung_Max` | Vorgabe: unbegrenzt | nein | > 0 | VDI 6007 |
| Außenbauteile mit Strahlung | — | `Aussenbauteile_Strahlung` (0/1, `NOT NULL DEFAULT 0`) | aus | — | — | VDI 6007 |

**Zur Sichtbarkeit.** Zwei Wege sind im Haus belegt: verstecken (`@if`) und **sperren mit
Grund** (`PvModellFelder.razor:48` samt Begründung `:13-18`). Empfehlung: **verstecken**,
weil es hier sieben Felder auf einmal sind und ein Block aus sieben grauen Feldern mehr
verwirrt als er erklärt — die Herleitungszeile unter der Klappliste sagt in beiden
Stellungen, was gilt. Die **Werte bleiben beim Umschalten stehen** und werden auch im
Tagesbilanz-Weg gespeichert (sie sind der Modellparametersatz, nicht der Rechenweg).

**Die Randbedingung der Grundfläche steht nicht in dieser Gruppe**, sondern als Spalte der
U·A-Tabelle (5.3) — sonst gäbe es sie zweimal.
**Ost/West stehen nicht in dieser Gruppe**, sondern bei den Fenstern (5.5) — sie wirken
auch im Bestandsweg über die Summe.

**Die DTO-Erweiterung** in `GebaeudeKatalogDaten` (alle nullbar, NULL = Vorgabe):

```
public string? Modell { get; set; }                     // null = VDI6007, sonst "TAGESBILANZ"
public double? FensterflaecheOst { get; set; }
public double? FensterflaecheWest { get; set; }
public double? Rahmenanteil { get; set; }
public double? Verschattungsfaktor { get; set; }
public string? GrundflaecheRandbedingung { get; set; }  // null = ERDREICH
public double? MasseanteilAussen { get; set; }
public double? Innenflaechenfaktor { get; set; }
public double? HeizungStrahlungsanteil { get; set; }
public double? HeizleistungMaxKw { get; set; }
public bool AussenbauteileStrahlung { get; set; }
```

### 5.7 Der Wirt und der Katalogdialog

**Dieselben Änderungen in beiden.** 8.1 nennt `GebaeudeDialog.razor` und
`GebaeudeKatalogDialog.razor`. Tatsächlich hat der Wirt keine Fachfelder (1.2) — die
Gruppen 2 bis 5 entstehen **einmal**, im Editor. Was der Wirt bekommt:

1. **Eine Spalte „Modell" in der Projektliste** (heute Wahl + Name,
   `GebaeudeDialog.razor:75-89`) — der Anwender sieht, welches Gebäude stündlich rechnet.
   Ohne sie ist E1 unsichtbar.
2. **Zwei leise Kennzahlen im Detailblock** „Gebäude: Verbrauch" (`:154-195`): H_ges [W/K]
   und das Rechenmodell als Text, beide nur lesend wie die fünf vorhandenen Felder.
3. Der Knopf **„Aus IFC-Datei übernehmen …"** in der Katalogleiste (`:136-148`) — **ab G4**,
   „Kein Delegat, kein Knopf" (`EPOS.UI/CLAUDE.md:47`).

**Der Katalogdialog ist in allen drei Betriebsarten identisch** (Bearbeiten, Neu, Admin) —
die neuen Gruppen hängen an keiner davon.

### 5.8 Der Bedarfsdialog

*Neue Gruppe „Vergleich der Rechenmodelle"* (Konzept 8.2) — eine Tabelle mit vier Spalten
(Kennzahl | Tagesbilanz | VDI 6007 | Abweichung):

| Zeile | Einheit |
|---|---|
| Wärmebedarf Heizung | MWh / kWh (Einheitenwahl) |
| Spitzenlast (Stunde) | kW |
| Spitzenlast (gleitendes Tagesmittel) | kW |
| 95-%-Quantil der Stundenlast | kW |
| Vollbenutzungsstunden | h/a |

Die drei Spitzenwerte sind die Kennzahlen aus Konzept 4.5; **beide Spalten sind Auskünfte
über `GebaeudeBedarfCtrl`** — zwei Aufrufe desselben Controllers mit erzwungenem Modell,
keine zweite Rechnung (Hausregel `EPOS.Kern/CLAUDE.md`, „Eine Auskunft ruft den Rechenweg
des Laufs").

*Neues Bild „Raumtemperatur"* unter der Wärmelast-Ganglinie: Jahresverlauf Luft und
operativ mit Sollwertband, gezeichnet im Kern (`ChartRenderer`, Konzept 9), hereingereicht
als **zweiter Delegat** `BildauftragRaumtemperatur` nach dem Muster von `Bildauftrag`
(`GebaeudeBedarfDialog.razor:126-134`) und im selben `ChartBild`-Baustein (`:91-93`). Nur
bei VDI 6007. Gegenprobe: `Proben/ChartProben`.

*Neue Kennzahlen* im Block „Kennzahlen" (`:60-84`), nur bei VDI 6007: Kühlbedarf
(informativ) [MWh], Stunden mit Kühlbedarf [h], mittlere Raumtemperatur in der Heizzeit
[°C].

*DTO-Erweiterung* `GebaeudeBedarfDaten`:

```
public string Modelltext { get; init; } = "";          // Anzeigetext des Rechenmodells
public double? SpitzeStundeKw { get; init; }
public double? SpitzeTagesmittelKw { get; init; }
public double? SpitzeQuantil95Kw { get; init; }
public double? KuehlenergieMwh { get; init; }
public int? KuehlstundenH { get; init; }
public double? MittlereRaumtemperaturC { get; init; }
public GebaeudeBedarfDaten? Vergleich { get; init; }   // der jeweils andere Weg; null = keiner
```

Alle neuen Felder **nullbar** — „Ein Reiter zeichnet nie ein vorbelegtes DTO als Ergebnis"
(`EPOS.UI/CLAUDE.md:82-84`); ohne Wert steht „—" (`GebaeudeBedarfDialog.razor:173`).
**Keine 8 760 Werte im DTO** (`GebaeudeBedarfDaten.cs:15-17`) — auch nicht die
Raumtemperatur.

### 5.9 Die Hülle nach `EPOS.UI.Daten`

**Die Klassenskizze.** Aus `GebaeudeHuelle.cs` (483 Z.) und `GebaeudeKatalogHuelle.cs`
(554 Z.) werden je zwei Hälften — Vorbild ist die Aufteilung von
`SimulationErgebnisHuelle` in fünf Dateien unter `EPOS.UI.Daten/Simulation/`:

```
EPOS.UI.Daten/Bedarf/GebaeudeHuelle.cs            // Gaben(...), Katalogzeilen, Stammdetail,
                                                  // Aufnehmen, AusModell, NachModell, Texte
EPOS.UI.Daten/Bedarf/GebaeudeKatalogHuelle.cs     // Gaben(...), Laden, Schreiben,
                                                  // AusModell, NachModell, Texte
EPOS.UI.Daten/Bedarf/GebaeudeBedarfHuelle.cs      // Bedarfsgaben(...), Bedarfsbild(...)
EPOS.UI.Daten/Bedarf/Gebaeudewege.cs              // die NAHT (siehe unten)
WindowsFormsApplication1/Views/Gebäude/GebaeudeFenster.cs         // Oeffnen, Katalogverwaltung
WindowsFormsApplication1/Views/Gebäude/GebaeudeKatalogFenster.cs  // Bearbeiten, Neu, Verwaltung
```

**Was in der Schale bleibt:** allein das Fenster. `BlazorDialogForm<T>`, `ShowDialog`,
`Size MASS` und der `Geschlossen`-Rückruf, der das Formular schließt
(`GebaeudeHuelle.cs:51-100`, `GebaeudeKatalogHuelle.cs:43-86`). Das ist genau die Hälfte,
die `EnableWindowsTargeting=false` nicht erlaubt.

**Die Naht.** `GebaeudeKatalogHuelle.Gaben(...)` trägt heute einen `IWin32Window besitzer`
(`:88-89`) und reicht ihn an `BrauchwasserGaben(...)` (`:123`, `:253`) durch, das wiederum
`BedarfsProfileHuelle.Gaben(besitzer, …)` ruft (`:279`) — und
`WindowsFormsApplication1/Views/Bedarf/BedarfsProfileHuelle.cs` liegt in der Schale.
Dasselbe gilt für `GebaeudetypHuelle.Gaben()` (`GebaeudeHuelle.cs:154-155`,
`WindowsFormsApplication1/Views/Bedarf/GebaeudetypHuelle.cs`). **Ohne eine benannte Naht
wandert die Gebäudehülle also nicht.** Die Bauform steht bereit —
`EPOS.UI.Daten/Katalogwege.cs` (`:23-31`): ein `static Func<…>`-Haken mit folgenloser
Vorbelegung, den Windows in `Program.Main` einhängt und iOS leer lässt; „Kein Delegat ist
kein Knopf" (`Katalogwege.cs:18-21`). Vorschlag:

```
internal static class Gebaeudewege
{
    /// Parametersatz der Brauchwasser-Profilliste des laufenden Projekts; null = kein Knopf.
    internal static Func<List<Z_ProjektBrauchwasserModel>,
                         IReadOnlyDictionary<string, object>> BrauchwasserGaben;

    /// Parametersatz der Gebäudetypen-Verwaltung; null = kein Knopf.
    internal static Func<IReadOnlyDictionary<string, object>> GebaeudetypGaben;
}
```

Der `IWin32Window` fällt damit aus allen `Gaben`-Signaturen — er wurde ohnehin nur
weitergereicht, nie selbst benutzt (`GebaeudeKatalogHuelle.cs:88-123`).

**Zwei Wachen prüfen den Umzug:** `EPOS.UI.Tests/ParametersatzTests.cs` (jeder Schlüssel
trifft ein `[Parameter]`, `:12-38`) und `EPOS.UI.Tests/HuellenwegTests.cs` (kein modales
Systemfenster im Blazor-Ereignis, `:10-57`). Konzept 10.5 nennt zusätzlich die
Hüllenwegwache ausdrücklich für diesen Schritt. Dazu die Regel der Wurzel-`CLAUDE.md`:
Wer eine Hülle oder Naht der Schale anfasst, prüft sie mit
`-p:EnableWindowsTargeting=true` kompiliert, bevor der Auftrag abgenommen wird.

**Der Kern bleibt unberührt:** `GebaeudeStammCtrl`, `ProjektGebaeudeCtrl` und
`GebaeudeBedarfCtrl` liegen schon in `EPOS.Kern/Controller/` — die Hülle referenziert sie,
nicht umgekehrt.

### 5.10 Die neuen Ressourcenschlüssel

**Gruppen, Spalten, Summen (Editor)**

| Schlüssel | DE | EN |
|---|---|---|
| `GEBK_GRP_HUELLE` | Hülle: Transmission je Bauteil | Envelope: transmission per component |
| `GEBK_GRP_LEITWERTE` | Wärmeleitwerte | Heat transfer coefficients |
| `GEBK_GRP_FENSTER_ORIENTIERUNG` | Fenster nach Orientierung | Windows by orientation |
| `GEBK_GRP_RECHENMODELL` | Rechenmodell | Calculation model |
| `GEBK_SP_BAUTEIL` | Bauteil | Component |
| `GEBK_SP_U_PSI` | U bzw. ψ | U or ψ |
| `GEBK_SP_A_L` | A bzw. L | A or L |
| `GEBK_SP_RANDBEDINGUNG` | Randbedingung | Boundary condition |
| `GEBK_SP_UA` | U·A [W/K] | U·A [W/K] |
| `GEBK_SP_HERKUNFT` | Herkunft | Source |
| `GEBK_ZEILE_AUSSENWAND` | Außenwand | External wall |
| `GEBK_ZEILE_FENSTER` | Fenster | Windows |
| `GEBK_ZEILE_DACH` | Dach | Roof |
| `GEBK_ZEILE_BODENPLATTE` | Bodenplatte | Ground floor slab |
| `GEBK_ZEILE_SONSTIGES` | Sonstiges | Other |
| `GEBK_ZEILE_WB_FENSTER` | Wärmebrücke Fenster–Wand | Thermal bridge window–wall |
| `GEBK_ZEILE_WB_KELLER` | Wärmebrücke Außenwand–Keller | Thermal bridge external wall–basement |
| `GEBK_ZEILE_WB_DACH` | Wärmebrücke Wand–Dach | Thermal bridge wall–roof |
| `GEBK_LBL_HT` | H_T Transmission : | H_T transmission: |
| `GEBK_LBL_HVE` | H_ve Lüftung : | H_ve ventilation: |
| `GEBK_LBL_HGES` | H_ges gesamt : | H_total: |
| `GEBK_LBL_HT_GEWICHTET` | H_T gewichtet (Tagesbilanz) : | H_T weighted (daily balance): |
| `GEBK_HINWEIS_GEWICHTE` | Der Tagesbilanz-Weg wichtet Außenwand und Wärmebrücken mit 0,83, das Dach mit 0,95 und die Bodenplatte mit 0,45. Das Stundenmodell rechnet ungewichtet. | The daily balance weights external wall and thermal bridges by 0.83, the roof by 0.95 and the ground floor slab by 0.45. The hourly model calculates unweighted. |

**Randbedingungen** (Anzeigetexte; die DB-Werte `ERDREICH`/`KELLER`/`AUSSENLUFT` bleiben
deutsch in `DbWerte`, Glossar § 10)

| Schlüssel | DE | EN |
|---|---|---|
| `GEBK_RAND_AUSSENLUFT` | Außenluft | Outdoor air |
| `GEBK_RAND_ERDREICH` | Erdreich | Ground |
| `GEBK_RAND_KELLER` | Keller (unbeheizt) | Unheated basement |

**Fenster nach Orientierung**

| Schlüssel | DE | EN |
|---|---|---|
| `GEBK_LBL_FF_OST` | Fensterfläche Ost : | Window area east: |
| `GEBK_LBL_FF_WEST` | Fensterfläche West : | Window area west: |
| `GEBK_LBL_FF_SUMME_OW` | Summe Ost + West : | Total east + west: |
| `GEBK_LBL_FF_GESAMT` | gesamte Fensterfläche : | Total window area: |
| `GEBK_FELD_FF_OST` | Fensterfläche Ost | Window area east |
| `GEBK_FELD_FF_WEST` | Fensterfläche West | Window area west |

**Rechenmodell**

| Schlüssel | DE | EN |
|---|---|---|
| `GEBK_LBL_RECHENMODELL` | Rechenmodell : | Calculation model: |
| `GEBK_MODELL_VDI6007` | VDI 6007 (stündlich) | VDI 6007 (hourly) |
| `GEBK_MODELL_TAGESBILANZ` | Tagesbilanz | Daily balance |
| `GEBK_ZEILE_MODELL_VDI6007` | VDI 6007 (stündlich): Raumtemperatur, Kühlbedarf und Spitzenlast je Stunde. | VDI 6007 (hourly): indoor temperature, cooling demand and peak load per hour. |
| `GEBK_ZEILE_MODELL_TAGESBILANZ` | Tagesbilanz: der Bestandsweg mit gewichteten Bauteilen, ohne Raumtemperatur. | Daily balance: the legacy path with weighted components, without indoor temperature. |
| `GEBK_LBL_RAHMENANTEIL` | Rahmenanteil : | Frame fraction: |
| `GEBK_LBL_VERSCHATTUNG` | Verschattungsfaktor : | Shading factor: |
| `GEBK_LBL_MASSEANTEIL` | Masseanteil außen : | External mass fraction: |
| `GEBK_LBL_INNENFLAECHENFAKTOR` | Innenflächenfaktor : | Internal area factor: |
| `GEBK_LBL_HEIZUNG_STRAHLUNG` | Strahlungsanteil Heizung : | Radiative fraction of heating: |
| `GEBK_LBL_HEIZLEISTUNG_MAX` | Heizleistungsgrenze : | Heating power limit: |
| `GEBK_LBL_AUSSEN_STRAHLUNG` | Außenbauteile mit Strahlung | External components with solar radiation |
| `GEBK_VORGABE` | Vorgabe {0} | Default {0} |
| `GEBK_VORGABE_UNBEGRENZT` | Vorgabe: unbegrenzt | Default: unlimited |

**Plausibilitätsmeldungen (Konzept 4.8)**

| Schlüssel | DE | EN |
|---|---|---|
| `GEBK_MSG_WOHNFLAECHE` | Die Wohn-/Nutzfläche muss größer als 0 sein. | The living/usable area must be greater than 0. |
| `GEBK_MSG_FLAECHE_NUTZER` | Die Fläche je Nutzer muss größer als 0 sein. | The area per occupant must be greater than 0. |
| `GEBK_MSG_RAUMHOEHE` | Die Raumhöhe muss größer als 0 sein. | The room height must be greater than 0. |
| `GEBK_MSG_BAUWEISE` | Die Bauweise muss zwischen 5 und 200 Wh/(m²K) je m² Wohnfläche liegen. | The thermal mass must be between 5 and 200 Wh/(m²K) per m² of living area. |
| `GEBK_MSG_G_WERT` | Der Fensterdurchlaßgrad muss größer als 0 und höchstens 1 sein. | The total solar energy transmittance must be greater than 0 and at most 1. |
| `GEBK_MSG_U_BEREICH` | Der U-Wert {0} muss zwischen 0,1 und 6 W/(m²K) liegen. | The U-value {0} must be between 0.1 and 6 W/(m²K). |
| `GEBK_MSG_FENSTERSUMME` | Die Summe der Fensterflächen muss die gesamte Fensterfläche ergeben. | The window areas must add up to the total window area. |
| `GEBK_MSG_RREST` | Die Bauteile ergeben keinen positiven Restwiderstand; das mittlere U liegt über 4,17 W/(m²K). | The components do not yield a positive residual resistance; the mean U-value exceeds 4.17 W/(m²K). |
| `GEBK_MSG_FERIENTAG` | Ein Ferientag muss zwischen 1 und 365 liegen. | A holiday day must be between 1 and 365. |
| `GEBK_MSG_LUFTWECHSEL` | Die Luftwechselrate muss größer als 0 sein. | The air change rate must be greater than 0. |

**Wirt und Bedarfsdialog**

| Schlüssel | DE | EN |
|---|---|---|
| `GEB_SP_MODELL` | Modell | Model |
| `GEB_LBL_HGES` | Wärmeleitwert H_ges: | Heat transfer coefficient H_total: |
| `GEB_LBL_RECHENMODELL` | Rechenmodell: | Calculation model: |
| `GEB_BTN_IFC` | Aus IFC-Datei übernehmen… | Import from IFC file… |
| `GEBB_GRP_VERGLEICH` | Vergleich der Rechenmodelle | Comparison of calculation models |
| `GEBB_SP_TAGESBILANZ` | Tagesbilanz | Daily balance |
| `GEBB_SP_VDI6007` | VDI 6007 | VDI 6007 |
| `GEBB_SP_ABWEICHUNG` | Abweichung | Deviation |
| `GEBB_LBL_SPITZE_STUNDE` | Spitzenlast (Stunde): | Peak load (hour): |
| `GEBB_LBL_SPITZE_TAGESMITTEL` | Spitzenlast (Tagesmittel): | Peak load (daily mean): |
| `GEBB_LBL_SPITZE_Q95` | 95-%-Quantil der Stundenlast: | 95th percentile of hourly load: |
| `GEBB_LBL_KUEHLENERGIE` | Kühlbedarf (informativ): | Cooling demand (for information): |
| `GEBB_LBL_KUEHLSTUNDEN` | Stunden mit Kühlbedarf: | Hours with cooling demand: |
| `GEBB_LBL_RAUMTEMP_MITTEL` | mittlere Raumtemperatur in der Heizzeit: | Mean indoor temperature during the heating period: |
| `GEBB_BILD_RAUMTEMPERATUR` | Raumtemperatur Jahresverlauf | Indoor temperature over the year |
| `GEBB_LBL_RAUMTEMP_LUFT` | Raumluft | Indoor air |
| `GEBB_LBL_RAUMTEMP_OPERATIV` | operativ | Operative |
| `GEBB_LBL_SOLLBAND` | Sollwertband | Setpoint band |

**Summe: 63 neue Schlüssel** (46 `GEBK_`, 4 `GEB_`, 13 `GEBB_`), je in beiden `.resx`,
danach `Werkzeuge/ResourceDesigner`. Einheiten bleiben sprachneutral (Glossar § 11),
englische Beschriftungen in **Sentence case** (Glossar § 11), der Doppelpunkt wird aus dem
Deutschen übernommen. Vier Bestandsschlüssel werden **frei** (die Gruppentitel
`GEBK_GRP_FLAECHEN`, `GEBK_GRP_UWERTE`, `GEBK_GRP_WAERMEBRUECKEN`, `GEBK_GRP_ANSCHLUSS`) —
sie werden gelöscht, nicht umgewidmet.

### 5.11 Die bunit-Testfälle

*`GebaeudeKatalogDialogTests` — neu (23 Fälle)*

| # | Fall | Prüft |
|---|---|---|
| 1 | `Der_erste_Reiter_traegt_die_neuen_Gruppen` | Gruppentitel Hülle/Leitwerte/Fenster/Rechenmodell stehen |
| 2 | `Die_Huelltabelle_fuehrt_acht_Zeilen` | 8 Zeilen im Hüllraster, Zeilentexte |
| 3 | `Jede_Huellzeile_zeigt_U_mal_A` | drei belegte Zeilen, Produkt gerechnet und angezeigt |
| 4 | `Die_Fensterzeile_ist_nur_lesbar` | die Flächenzelle der Fensterzeile trägt kein Eingabefeld |
| 5 | `Nur_die_Bodenplatte_hat_eine_Randbedingung` | genau ein `select` in der Spalte |
| 6 | `H_T_ist_die_Summe_der_acht_Zeilen` | Summenzeile gegen die Handrechnung |
| 7 | `H_ve_kommt_aus_Luftwechsel_Wohnflaeche_und_Raumhoehe` | der 0,34-Weg |
| 8 | `H_ges_ist_H_T_plus_H_ve` | — |
| 9 | `Der_gewichtete_Wert_steht_nur_im_Tagesbilanz_Weg` | Umschalten blendet die vierte Zeile ein und aus |
| 10 | `Der_gewichtete_Wert_trifft_SpezWaermeverlusteC` | 0,83/0,95/0,45 gegen `BhkwPlan.cs:347-353` |
| 11 | `Die_Modellklappliste_fuehrt_zwei_Eintraege` | Reihenfolge, Texte |
| 12 | `Die_Vorgabe_ist_VDI_6007` | leeres `Modell` → Klappliste steht auf VDI 6007 (E1) |
| 13 | `Die_Modellwahl_blendet_die_sieben_Parameterfelder_ein_und_aus` | — |
| 14 | `Die_Modellzeile_wechselt_mit_der_Wahl` | Prüfhilfe wie `PvModellFelder.Modellzeile` |
| 15 | `Ein_leeres_Parameterfeld_zeigt_seine_Vorgabe` | Platzhalter „Vorgabe 0,3" |
| 16 | `Ein_leeres_Parameterfeld_speichert_NULL` | der `Speichern`-Delegat bekommt `null`, nicht 0,3 |
| 17 | `Ost_und_West_stehen_getrennt` | zwei neue Zahlenfelder |
| 18 | `Die_Summe_Ost_West_wird_gerechnet_und_mitgeschrieben` | `FensterflaecheOstWest` im gespeicherten Satz |
| 19 | `Beide_leer_heisst_NULL_und_nicht_die_Haelfte` | der Dialog rechnet die Vorgabe nicht aus |
| 20 | `Ein_U_Wert_ausserhalb_0_1_bis_6_faerbt_und_wird_nicht_uebernommen` | `Min`/`Max` am Zahlenfeld |
| 21 | `Ein_g_Wert_ueber_1_meldet_beim_OK` | `GEBK_MSG_G_WERT` |
| 22 | `OK_prueft_speichert_und_schliesst` | die eine Schreibstelle (Hausregel) |
| 23 | `Abbrechen_schreibt_nichts` | der Delegat wird nicht gerufen |

Dazu **anzupassen**: `Der_erste_Reiter_traegt_die_Felder_der_Karte_von_Form_Gebaeude1`
(`:105-125`, die Zahl der Zahlenfelder ändert sich),
`Der_zweite_Reiter_traegt_die_Felder_der_Karte_von_Form_Gebaeude2` (`:138-158`, sechs
Felder gehen weg), `Ohne_Uebernehmen_bleibt_der_Satz_unberuehrt` (`:486`, der Knopf
entfällt) und die drei Fälle um `Beenden`/`Ueberschreiben` (`:174-217`, `:388-421`).

*`GebaeudeDialogTests` — neu (3 Fälle)*: Spalte „Modell" in der Projektliste; H_ges und
Rechenmodell im Detailblock; ohne IFC-Delegat kein IFC-Knopf.

*`GebaeudeBedarfDialogTests` — neu (7 Fälle)*: Vergleichstabelle erscheint nur mit
`Vergleich`; fünf Kennzahlzeilen stehen; die drei Spitzenwerte tragen kW; Kühlbedarf und
Kühlstunden nur bei VDI 6007; ohne Raumtemperatur-Delegat kein zweites Bild; das zweite
Bild wird zwischengespeichert; „—" statt einer erfundenen Zahl bei `null`.

*Kernseitig* (Konzept 10.2/10.3, nicht Gegenstand dieses Befundes, aber die Gegenprobe zur
Dialoganzeige): ein Fall in `EPOS.Kern.Tests`, der `Gebaeudehuellbilanz` im gewichteten
Zweig auf 1e-12 gegen `BhkwPlan.SpezWaermeverlusteC` hält.

*Wachen, die ohne Zutun greifen*: `ParametersatzTests` (neue Gaben-Schlüssel),
`SchliesskreuzWacheTests`, `UeberlagerungstitelTests`, `StilblattTests` (falls eine
`.epos-raster*`-Regel fällt), `HuellenwegTests` (beim Hüllenumzug),
`DokumentationLinkWacheTests` (dieser Befund und seine Indexzeile).

### 5.12 Aufwand je Teil

| Teil | Inhalt | Aufwand |
|---|---|---|
| **M-a — Hilfsklasse** | `EPOS.Kern/Allgemein/Gebaeudehuellbilanz.cs` (Zeilen, H_T, H_ve, H_ges, gewichteter Zweig) samt Kernprobe gegen `SpezWaermeverlusteC` | 0,5 PT |
| **M-b — DTO und Hüllenabbildung** | 11 Felder in `GebaeudeKatalogDaten`, `AusModell`/`NachModell`, Ost/West-Summenschreibung, Umbenennung `Fensterflaeche_OstWest` | 0,5 PT |
| **M-c — U·A-Tabelle und Summen** | neue Gruppen 2 und 3, Wegfall der drei alten Gruppen und der zwei Reiter-2-Gruppen | 1,0 PT |
| **M-d — Fenster Ost/West** | Gruppe 4 samt Summenanzeigen | 0,3 PT |
| **M-e — Gruppe Rechenmodell** | Klappliste, sieben bedingte Felder, Schalter, Herleitungszeile, `Platzhalter` am `Zahlenfeld` | 0,7 PT |
| **M-f — Ein Schreibweg** | `SpeichernLeiste` statt drei Knöpfen, Reiter-2-Stand auflösen, zehn Prüfregeln an einer Stelle | 1,0 PT |
| **M-g — Wirt** | Spalte „Modell", zwei Kennzahlen im Detailblock | 0,3 PT |
| **M-h — Bedarfsdialog** | Vergleichstabelle, sechs Kennzahlen, zweites Bild, DTO | 1,0 PT |
| **M-i — Texte** | 63 Schlüssel in zwei `.resx`, Glossarabschnitt 13, `ResourceDesigner` | 0,7 PT |
| **M-j — Tests** | 33 neue bunit-Fälle, Anpassung von sieben bestehenden | 1,0 PT |
| **M-k — Hülle nach `EPOS.UI.Daten`** | vier Dateien, `Gebaeudewege`-Naht, `IWin32Window` heraus, Linux-Bau der Schale | 1,0 PT |
| | **Summe** | **8,0 PT** |

Das liegt im Rahmen von Konzept 11 (G1 „mittel, 6–10 PT" **einschließlich** Schemaschritt,
Namensleser, Eingangsbauer und Verzweigung) — die Oberfläche ist also rund die Hälfte von
G1. Die Sichtabnahme unter Windows und der Wiki-Eintrag kommen aus G2.

---

## 6. Offene Punkte

1. **Die Zieldatei dieses Befundes verweist auf
   `../Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`, das es noch nicht
   gibt.** `EPOS.Kern.Tests/DokumentationLinkWacheTests` prüft jeden relativen Verweis;
   bis das Umsetzungskonzept angelegt und dieser Befund in `Dokumentation/LIESMICH.md`
   eingetragen ist, ist die Wache rot.
2. **M-1 (ein Schreibweg) ist eine sichtbare Änderung für den Anwender** — „Überschreiben"
   und „Beenden" verschwinden zugunsten von OK/Abbrechen plus „Speichern unter…".
   Das ist ein Entscheid, kein Befund; ohne ihn hängen die zehn Prüfregeln aus 4.8 an drei
   Stellen (Vorschlag: „Speichern unter…" bleibt als nicht schließender Zweitknopf mit
   `MitSpeichern="true"` nach `EPOS.UI/CLAUDE.md:53-56`).
3. **Sperren oder verstecken?** 5.6 empfiehlt verstecken, `PvModellFelder` sperrt. Eine
   Hausregel dafür gibt es nicht — nur die Nachbarregel, dass ein gesperrtes Bedienelement
   seinen Grund nennen muss (`EPOS.UI/CLAUDE.md:41-44`).
4. **Der `Platzhalter` am `Zahlenfeld`** ist ein Eingriff in einen Standardbaustein, den
   alle Dialoge benutzen. Er ist rein additiv (ein `[Parameter] string` und ein
   `placeholder`-Attribut), zieht aber `StilblattTests` nach sich.
5. **Das Glossar ist für die Gebäudehülle nicht vorbereitet** (3.3). Der Abschnitt 13
   sollte vor den en-US-Werten stehen, sonst entstehen zwei Übersetzungen desselben
   Begriffs.
6. **`GEBK_GRP_UWERTE` im englischen Katalog trägt zwei unsichtbare Steuerzeichen**
   (`Resource.en-US.resx:13294`) — beim Anfassen der Gruppe mitbereinigen.
7. **Konzept 8.1 zählt die Randbedingung der Grundfläche unter den Parameterfeldern der
   Gruppe „Rechenmodell" auf**, E2 (N1.6) verlangt sie als Spalte der U·A-Tabelle. Dieser
   Befund folgt E2 (5.3); das Konzept wäre an dieser Stelle nachzuziehen.
