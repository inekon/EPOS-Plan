# Protokoll: Stufe G4b — Bauteilimport, ein importiertes Gebäude als Zone mit Bauteilen (25.09.2026)

**Auftrag** (Anwender, 25.09.2026, im Wortlaut „fahre mit G4b fort“): Stufe G4b der Gebäudesimulation —
ein Gebäude aus IFC oder gbXML kommt auf Wunsch mit **einer** Zone, echten Bauteilzeilen und Aufbauten
samt Schichten ins Projekt und rechnet dann über den Bauteilweg. Gebaut in zwei Wellen (A, B), die
Papiere als Welle C. Dazu zwei Anwenderentscheide: **E44** (G4b jetzt, abweichend von E38) und **E45**
(drei Rechenregeln des Bauteilimports). Maßgeblich:
[Konzept](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 4.3, 4.7, 6.3, 7.6 und
N1.49 (Entscheide und Festlegungen der Umsetzung),
[Datenaustauschkonzept](../../../aktuell/Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 3.4 bis 3.7,
[Mehrzonenkonzept](../../../aktuell/Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 3.4 und 6,
[Umsetzungskonzept](../../../aktuell/Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 3.4 und
3.8, [Rechenschritte](../../../aktuell/Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 1.1 und
A2. Der Stand je Stufe steht in der [Statusdatei](../../../aktuell/Status_Gebaeudesimulation_VDI6007.md),
Zeile G4b; der Einzonenimport (G4c, G4a), auf dem diese Stufe aufsetzt, im
[Protokoll G4](2026-09-24_G4_Importe.md).

## 1 Die Wellen im Überblick

| Welle | Datum | Inhalt | Commits (Merge) |
|---|---|---|---|
| A | 25.09.2026 | Bauteilvorschlag im Kern (eine Zone, Bauteilzeilen, Aufbauten, Schichten), Schreibweg in einem Vorgang | `f2c0ccdf` (Vorschlag), `aa0e5b73` (Schreibweg) (`0b6cbdb1`) |
| B | 25.09.2026 | Einordnung der Hülle an einer Stelle, Vorhangfassade transparent, Texte der Meldungen, Zielfeld Innenflächenfaktor, Folgevorgaben, Abschnitt „Bauteile (echte Hülle)“ im Importdialog samt Schreibweg, Wiki-Quelle „Gebäudeimport“ | `98f64d8e`, `4124b583`, `7a24b08f`, `9a494f16`, `cc548fa6`, `60081339`, `c522fe4d` (`fc49ad88`) |
| C | 25.09.2026 | Papiere: dieses Protokoll, Konzept N1.49, Nachzüge in Statusdatei, Umsetzungs-, Datenaustausch- und Mehrzonenkonzept, Softwarearchitektur, Rechenschritten, Logbuch und Index | dieser Nachzug |

Kein Schemaschritt: `Tab_Zone`, `Tab_Bauteil`, `Tab_Bauteilaufbau(_STAMM)` und `Tab_Bauteilschicht(_STAMM)`
stehen seit G3 (Schritte 132–134), `Tab_Importzuordnung` seit G4c (Schritt 138), die Spalte
`Tab_Gebaeude.Innenflaechenfaktor` seit dem Gebäudespalten-Schritt M3.

## 2 Die Entscheide

**E44 (Anwender, 25.09.2026, „fahre mit G4b fort“).** G4b wird jetzt gebaut. Das weicht ab von E38
(Konzept N1.43): Dort sollte G4b erst nach G3 **und** nachdem G4a im Feld war folgen. G3 ist
abgeschlossen (Protokoll [G3](2026-09-25_G3_Bauteilkatalog.md)), G4a gebaut und angebunden; seine
Feldphase steht noch aus. G4b ist in der Sitzung von G3 gebaut worden.

**E45 (Anwender, 25.09.2026) — drei Rechenregeln des Bauteilimports.**

1. **Innere Masse nach Datenlage.** „Vollständig“ heißt: Jede innere Trennfläche zwischen übernommenen
   beheizten Räumen hat eine Fläche und einen vollständigen Aufbau (Dicke, λ, ρ, cp), und die
   Innenfläche beider Seiten liegt im Band **1,0 … 5,0 × Nutzfläche** (das Band steht um die Vorgabe
   f_IW = 2,5; DIN EN ISO 13790 nennt 2,5 bis 3,5). Dann werden die Trennflächen Bauteilzeilen
   `INNENWAND`/`DECKE` mit leerer Randbedingung (innerhalb der Zone), und beide Seiten zählen — IFC je
   Raumbegrenzung, gbXML als zwei Zeilen, weil eine unsymmetrische Decke von beiden Seiten verschiedene
   Masse hat. Sonst gibt es keine Innenzeilen: Der Innenflächenfaktor kommt aus der Datei (gemessene
   Innenfläche beider Seiten ÷ Nutzfläche) in die Spalte `Tab_Gebaeude.Innenflaechenfaktor`, die Masse
   aus der Bauweise. Ohne Innenflächen in der Datei gilt die Vorgabe 2,5; außerhalb des Bands wird der
   Faktor übernommen und gewarnt. Der Dialog nennt den gewählten Weg samt Grund.
2. **U-Wert neben vollständigen Schichten.** Die Schichten rechnen, `U_Wert` bleibt leer; weicht der
   U-Wert der Datei um mehr als 5 % ab, wird das gemeldet. Das weicht für den Import vom Vorrang des
   eingetragenen U-Werts im Mehrzonenkonzept 3.4 ab. Grund: R₁, C₁ und U·A kommen so aus denselben
   Schichten, und R_Rest kann nicht negativ werden.
3. **Vorhangfassaden transparent.** Eine Zeile `VORHANGFASSADE`; U und g aus der Datei, sonst U aus der
   Fenstervorgabe der Baualtersklasse (Herkunft `VORGABE`); g, Rahmenanteil und Verschattung bleiben
   leer, dann gelten die Werte des Gebäudes bzw. die Vorgaben. In den Summenfeldern stehen sie weiter
   unter „Sonstige“.

## 3 Welle A — der Bauteilvorschlag und sein Schreibweg

**Vorschlag im Kern** (`f2c0ccdf`; `EPOS.Kern/Allgemein/Import/Gebaeude/GebaeudeBauteilvorschlag.cs`,
ohne Datenbank): Aus dem formatfreien Abbild eines Gebäudes und der Raumauswahl des Zuordnungsdialogs
entsteht **eine** Zone (Nutzfläche, Volumen und Raumhöhe der übernommenen beheizten Räume, wie die
Zonenregel X4 des Einzonenwegs) mit Bauteilzeilen, Aufbauten und Schichten (innen → außen, Stoffwerte
als Kopie), Herkunft je Wert und benannten Meldungen `IMP_BAUTEIL_PROT_*`.
`GebaeudeHuelleneinordnung` ordnet jedes Bauteil nach den Regeln der Zuordnung (Seite, Summenfeld,
Fensterabzug U14, Trennflächenprobe); der Vorschlag hält jede Gruppe gegen das Summenfeld der Zuordnung
und lehnt bei einer Abweichung benannt ab. Gegen unbeheizte oder unbekannte Nachbarn steht die
Randbedingung `UNBEHEIZT`, die Fläche zählt im Summenfeld des Einzonenwegs. U-Wert und Schichten nach
E45/2; ohne vollständige Stoffwerte kein Aufbau, nur der U-Wert — der der Datei, sonst der aus einer
masselosen Schichtung, sonst die Vorgabe der Baualtersklasse. Die innere Masse nach E45/1. Der
Vorschlag rechnet sich zur Probe einmal über `GebaeudeZonenabbildung.AlsZonensatz` und
`ErsatzparameterRC.AusBauteilweg` durch; was sich nicht abbilden lässt, wird benannt abgelehnt, nie
mit einer Ausnahme. Neue Importprobe `gbxml_innenflaechen_teilweise.xml` (eine Ständerwand nur mit
R-Wert) samt Zeile in
[`LIESMICH_Importproben.md`](../../../../Referenzlaeufe/Importproben/LIESMICH_Importproben.md).

**Schreibweg** (`aa0e5b73`): `GebaeudeZonenCtrl.VorschlagSchreiben` schreibt einen Vorschlag für eine
vorhandene Projektkopie in **einem** Vorgang — Aufbauten samt Schichten als Projektkopien (ein im
Projekt schon vergebener Name wird ergänzt, `BauteilaufbauCtrl.ProjektaufbauEinfuegen` und
`FreierName`), die Zone und die Bauteile mit abgebildeter `ID_Aufbau`; nur über `DataRepository` mit
`?`-Parametern, auf Wunsch im Vorgang des Aufrufers (Sicherungspunkt). Benannt abgelehnt ohne
Schreiben: ein abgelehnter Vorschlag, ein Prüfbefund, ein fehlendes Gebäude, keine Projektkopie, eine
schon vorhandene Zone. Die Rückgabe trägt die Paarungen Raum → Zone, Fläche → Bauteil und Konstruktion
→ Aufbau mit Ziel-Id für `Tab_Importzuordnung`.

Tests: `GebaeudeBauteilvorschlagTests` (am Stand `fc49ad88` 33 Testmethoden),
`GebaeudeBauteilvorschlagDatenbankTests` (4: Rücklesen über `GebaeudeZonenanschluss`, Bauteilweg im
Lauf, Rücklauf mitten im Vorgang, Herkunft im selben Vorgang, Auskunft Bauteilweg gegen Klassenweg).

## 4 Welle B — Einordnung, Vorhangfassade, Zielfeld und Dialog

- **Einordnung an einer Stelle** (`98f64d8e`): `GebaeudeAggregation` bezieht die Einordnung je Bauteil
  (Seite, Summenfeld, Boden und Decke gegen unbeheizt, Gegenprobe der Trennflächen, Fensterabzug) aus
  `GebaeudeHuelleneinordnung`; die zweite Fassung der Regel entfällt. Die Einordnung führt die beidseitig
  beschriebenen Trennflächen und die Messung der Innenfläche (`InnenflaecheM2`: je Fläche innerer Masse
  die Nettofläche, zweifach, wenn beide Räume zum Gebäude gehören, sonst einfach). Alle 340 Import- und
  Aggregationstests unverändert grün.
- **Vorhangfassade transparent** (`4124b583`, E45/3): Art `VORHANGFASSADE`, rechnet im Fensterzweig mit
  Sonneneintrag; an Außenluft braucht sie einen Azimut, an Erdreich rechnet sie an Außenluft; ohne
  Klasse und ohne U benannt abgelehnt. Die Summenprobe hält die Gruppe Sonstige samt Vorhangfassaden
  gegen das Summenfeld. Neue Probe `ifc4_vorhangfassade.ifc` aus dem IFC-Probenerzeuger samt Zeile in
  `LIESMICH_Importproben.md`.
- **Texte** (`7a24b08f`): die 29 Meldungskennungen `IMP_BAUTEIL_PROT_*` deutsch und englisch in beiden
  `.resx`, `Resource.Designer.cs` mit `Werkzeuge/ResourceDesigner` neu geschrieben; die Wache
  „jeder Schlüssel in beiden Sprachen“ kennt den Präfix samt Mindestzahl.
- **Zielfeld Innenflächenfaktor** (`9a494f16`): `GebaeudeZielfelder.INNENFLAECHENFAKTOR` (Gruppe
  Kenngrößen) für `Tab_Gebaeude.Innenflaechenfaktor` — dieselbe Messung, mit der der Vorschlag seinen
  Innenweg wählt; außerhalb des Bands 1 … 5 gelb mit Beleg; ohne Innenflächen oder ohne Nutzfläche leer
  und ohne Haken (die Spalte bleibt NULL, es gilt 2,5). Der Wert geht über `NachKatalogdaten` in das
  Editorfeld, über den Katalogsatz und `CopyFromStamm` in die Projektkopie.
- **Folgevorgaben** (`cc548fa6`): Die inneren Gewinne folgen einer Handänderung der Nutzfläche
  (5 W/m² × A), der Nachtsollwert einem von Hand gesenkten Tagsollwert (höchstens der Tagwert) — nur für
  Zeilen mit Herkunft Vorgabe. Beides gehört zum Gebäudeimport der Stufe G4 (E43) und wird im
  Protokoll G4 geführt.
- **Abschnitt und Schreibweg** (`60081339`): Der Zuordnungsdialog trägt den Abschnitt „Bauteile (echte
  Hülle)“ mit dem Schalter „Als Zone mit Bauteilen übernehmen“ — vorbelegt ein, wenn der Vorschlag sich
  bilden lässt, sonst aus und gesperrt mit dem Grund daneben —, den Bauteilzeilen als schlichte Tabelle
  (Bezeichner, Art, Fläche, U-Wert oder „aus Schichten“, Azimut, Neigung, Randbedingung, Herkunft), der
  Zeile zur inneren Masse und den Meldungen des Vorschlags. `GebaeudeImportHuelle` bildet den Vorschlag
  mit jeder Zuordnung neu (Klasse, Gebäude, Raumhaken, Handwerte). Die ausstehende Herkunft trägt ihn
  nur mit Schalter (`GebaeudeImportHerkunft.Vorschlag`); `WizardCtrl.GebaeudeZuordnungAnlegen` schreibt
  nach `CopyFromStamm` `VorschlagSchreiben` und `GebaeudeImportCtrl.SchreibeHerkunft` (Paarungen des
  Vorschlags und des Gebäudes) im selben Vorgang — scheitert ein Teil, bleibt nichts. Ohne Schalter
  bleibt es beim Summenweg ohne Zone. Plattformfrei in `EPOS.UI.Daten`, derselbe Weg für Windows und
  iOS; der Wirt der Rasterprobe zeigt die Wahl auf der Seite `/gebaeudeimport`.
- **Wiki-Quelle** (`c522fe4d`): „Programm Dokumentation - Gebäudeimport“ mit dem Abschnitt „Bauteile
  (echte Hülle)“ — Schalter samt Sperre mit Grund, Liste, Schichtaufbauten und U-Werte, Randbedingung
  unbeheizt, Vorhangfassaden transparent, innere Masse, Neubildung bei Neuzuordnung; die Zeile
  Innenflächenfaktor der Zuordnung, Zone und Aufbauten an der Projektkopie, die Herkunft je Zone,
  Bauteil und Aufbau, die Grenze „eine Zone je Gebäude“. Gegengelesen mit dem Verbotsmuster, keine
  Treffer; nicht hochgeladen.

Tests: `GebaeudeImportBauteileTests` (11 Testmethoden: Zielfeld, Leer- und Bandfall, Gleichheit mit
dem Innenweg, Hülle, Schalter), `GebaeudeImportBauteileDatenbankTests` (4, Durchgang über die
Datenbank), Fälle in `GebaeudeImportDialogTests` (bunit).

## 5 Festlegungen der Umsetzung

Benannt, nicht entschieden; die nummerierte Liste steht im
[Konzept](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.49.

- Flächen gegen unbeheizte oder unbekannte Räume bekommen `UNBEHEIZT` (Kellertemperatur) statt
  Außenluft wie im Einzonenweg; das Summenfeld bleibt. Grund: Innenwände ohne Azimut würden an
  Außenluft nach N1.46 Nr. 9 abgelehnt.
- Erdberührte Wände und Decken behalten ihre Art mit `ERDREICH` und zählen zur Grundfläche.
- Fenster in erdberührten Wänden rechnen an Außenluft, weil G3 transparente Bauteile an Erdreich ablehnt.
- Die Herkunft einer Zeile wird `VORGABE`, sobald Fläche oder U eine Vorgabe ist.
- Die Quellkennung der Zone ist die Kennung des Gebäudes.
- Ein g ≤ 0 oder > 1 bleibt leer.
- Innentüren werden von der Innenwand abgezogen und bekommen keine Zeile.
- Das Zielfeld Innenflächenfaktor trägt den gemessenen Wert auch ohne Schalter (Summenweg), mit
  Herkunft ausgewiesen und abwählbar, gelb außerhalb des Bands.
- Die Zone behält die Nutzfläche der Datei; eine Handänderung der Nutzfläche am Gebäude rechnet über den
  Flächenschlüssel (E40).
- Nicht umgesetzt: der Namensabgleich mit `Tab_Baustoff` (`ID_Baustoff` bleibt leer, die Stoffwerte sind
  kopiert). Eine IFC-Geschossdecke hat keine Neigung; das wirkt nur auf den Übergangswiderstand.
- Mehrere Zonen (G6c) bleiben benannt abgelehnt; trägt das Gebäude schon eine Zone, wird nichts geschrieben.

## 6 Nachweise

**Durchgang über die Datenbank** (`GebaeudeImportBauteileDatenbankTests`, Probe `gbxml_haus_si.xml`):
Der Schreibweg legt an der Projektkopie **eine Zone mit 34 Bauteilen** an (Nutzfläche 120 m², Herkunft
`GBXML`), `Tab_Importzuordnung` bekommt **1 + 3 + 34 + 6 Paarungen** (Gebäude, Räume → Zone, Flächen →
Bauteile, Konstruktionen → Aufbauten), die Projektkopie trägt den Innenflächenfaktor 145/120, und der
Lauf rechnet die Zone über den Bauteilweg mit **A_IW = 145 m²**. Ohne Schalter entsteht keine Zone; ein
Fehler im Vorgang schreibt nichts.

**Auskunft Jahresheizwärme, Klassenweg gegen Bauteilweg** (Projekt 1045, dieselbe Gebäudezeile):

| Probe | Klassenweg [MWh] | Bauteilweg [MWh] | Verhältnis | Innere Masse | A_IW |
|---|---|---|---|---|---|
| `gbxml_haus_si.xml` | 10,930 | 10,643 | 0,974 | Innenbauteile übernommen | 145 m² |
| `ifc4_haus.ifc` | 10,766 | 10,820 | 1,005 | Faktor aus der Datei | 201,6 m² |
| `gbxml_innenflaechen_teilweise.xml` | 5,339 | 5,356 | 1,003 | Faktor aus der Datei | 66 m² |

**Der Dialog bei den Proben:**

- `gbxml_haus_si.xml`: Schalter ein, 34 Zeilen, 6 Aufbauten, Innenbauteile aus der Datei (145 m²).
- `ifc4_haus.ifc`: ein, 17 Zeilen, keine Aufbauten, Innenflächenfaktor 1,551 aus der Datei.
- `gbxml_ohne_konstruktionen.xml` ohne Klasse: aus und gesperrt, weil 8 Bauteile weder U noch Schichten
  tragen.
- Dieselbe Probe mit Klasse E: ein, 8 Zeilen mit U der Klasse.

**Welle A** (vor dem Merge): Kern 7 344 und UI 6 375 grün; Referenzlauf **14/14 PASS** gegen
`2026-09-25_R16_Anlagenprio`, 432/432 CSV byte-gleich; SQL-Dialekt-Prüfer 0 Funde. Auf dem Merge
`0b6cbdb1`: Kern 7 412, UI 6 390. Kern-Lauf der CI grün: 36175600275 auf `ba798d8a` (enthält `0b6cbdb1`).

**Welle B** (vor dem Merge): Kern 7 446, UI 6 396, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27;
Referenzlauf **14/14 PASS** gegen R16; SQL-Dialekt-Prüfer 1 927 Texte, 0 Funde; Auslieferungsvorlage
36/36.

**Ergebnisneutral.** Kein Referenzprojekt ist importiert, und kein Schemaschritt kam dazu; der
Referenzlauf ist in beiden Wellen unverändert
([Referenzläufe](../../../../Referenzlaeufe/LIESMICH.md)).

## 7 Koordination

- **Nummern.** E44, E45 und der Konzept-Nachtrag N1.49 gehören G4b, mit den parallelen Sitzungen
  abgestimmt und vor dem Eintrag auf `origin/ios_migration_september` als frei geprüft; G6a beginnt bei
  E46 und N1.50.
- **Folgevorgaben.** Die zwei Folgevorgaben aus `cc548fa6` gehören zum Gebäudeimport (G4, E43); die
  Sitzung G4 führt sie in ihrem Protokoll.
- **Register.** E44 und E45 berühren keinen Registerpunkt; das Register zählt weiter 8 offene Punkte
  (M3, M5–M8, M11–M13). M7 (Zonenregel beim Import) und M13 bleiben vor G6c fällig — G4b bildet stets
  eine Zone je Gebäude.

## 8 Abnahme

- **Gate auf dem Merge `fc49ad88`** (gegen die Basis `2026-09-25_R18_PvAusweis`): Kern-Filter und
  Windows-Schale je 0 Fehler; Kern 7 507 grün (einer übersprungen), UI 6 402, KiKern 549, SpeicherEngine
  386, SpeicherPlanung 27 (einer übersprungen); Referenzlauf der vierzehn Projekte **14/14 PASS**
  (4 610 207 Werte); SQL-Dialekt-Prüfer 1 939 Texte, 0 Fundstellen. Nach dem nächsten Merge von origin
  (`4c568dbb`, dabei die `.resx` als Vereinigung beider Blöcke samt neu erzeugtem Designer) dasselbe
  Gate noch einmal: Kern 7 574 (einer übersprungen), UI 6 432, KiKern 549, SpeicherEngine 386,
  SpeicherPlanung 27 (einer übersprungen); Referenzlauf 14/14 PASS gegen R18; SQL-Dialekt-Prüfer 0
  Fundstellen.
- **Wachen:** `DokumentationLinkWacheTests`, `RepositoryOrdnungWacheTests` und
  `WikiProduktdatenWacheTests` grün nach dem Nachzug der Papiere (Welle C).

## 9 Offen

- die **Windows-Sichtabnahme** des Abschnitts „Bauteile (echte Hülle)“ (Anwender);
- der **Wiki-Upload** der Seite „Gebäudeimport“ mit dem Sammel-Upload der Version 1.2.0.4 am
  26.09.2026 durch die Orchestrierung; Logbuch-Satz entworfen
  ([Update-Papier](../../../aktuell/Wiki_Update_2026-09-26.md));
- Azimute im Dialog mit bis zu drei Nachkommastellen (Kleinigkeit);
- der Namensabgleich der Baustoffe (`ID_Baustoff`) — mit dem Nachtrag unten vorgezogen, Welle 1 fertig;
- mehrere Zonen (G6c).

## 10 Nachtrag: Namensabgleich N1…N7, Welle 1 (26.09.2026)

**Auftrag.** Der Anwender hat am 26.09.2026 entschieden, den Namensabgleich aus G6c vorzuziehen
(Nachtrag zu E44 in [Konzept N1.49](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)).
Anlass: IFC-Dateien liefern fast nie brauchbare Stoffwerte; das IFC-Probenhaus bekam 17 Bauteile, aber
keinen Aufbau. Die Synonymtabelle (Register M9) kam aus G6a mit, abgestimmt mit der Sitzung G6a; die
Sitzung G4 gab die Importdateien frei.

**Commits** (Opus-Agent im eigenen Worktree): `1f9d8329` Schritt 145, jetzt **146** (Synonymtabelle,
gemerkte Zuordnungen, Registerpflege, Testdatenbank), `6c82c054` Abgleich im Kern samt Datenbankseite und
Texten, `83ca38b4` Einbau in den Bauteilvorschlag und `VorschlagSchreiben`, `9c287ec9` Tests und die
Probe `ifc4_haus_materialnamen.ifc`, `9a4153a1` Nachtrag in `Referenzlaeufe/LIESMICH.md`; dazu zwei
Merges von origin (`11fbbfc3`, `efe0dea3`) und der Merge in den Arbeitszweig.

**Gebaut.**
- `Tab_Baustoffsynonym_STAMM` (STRICT; normalisierter Materialname eindeutig, Sprache `de`/`en`,
  Verweis auf `Tab_Baustoff_STAMM` mit Löschweitergabe, `ReadOnly`, Quelle) mit 212 Synonymen, 116
  deutsch und 96 englisch, auf 51 der 65 herstellerneutralen Stoffe; die übrigen trifft der Abgleich
  über ihren eigenen Namen. Keine Hersteller- oder Markennamen; jede Zeile mit Quelle.
- `Tab_Baustoffzuordnung` (STRICT; Projekt, Materialname, Stammbaustoff, Zeitpunkt; eindeutig je
  Projekt und Name) für die eigene Zuordnung N7. Sie trägt ein eigenes `ID_Projekt` und ist deshalb vom
  Planwächter der Kopierlisten ausgenommen; das Projektduplikat versetzt `ID_Baustoff` nicht
  (`FK_OVERRIDE`, der Verweis zeigt auf den Katalog).
- `Baustoffabgleich` (Kern, mit Lesenaht) und `BaustoffabgleichCtrl` (`Abgleich()`, `Merken`,
  `Vergessen`); der Bauteilvorschlag nimmt den Abgleich optional und liefert die Liste der
  Materialnamen der Datei mit Stufe, Baustoff und Zahl der Schichten.

**Festlegungen** (im Konzept N1.49 zusammengefasst): Reihenfolge N7 → N6 → N3 → N4 → N5;
Herstellerzeilen nur bei genauem Namen, die neutrale Zeile geht vor; Marken in N2 `verputzt`,
`bewehrt`, `generisch`, `generic` und Maßangaben; ein Synonym trifft auch als Wortanfang, das längste
gewinnt, eine Zahl im Rest wählt die Rohdichtestufe derselben Stoffreihe; N5 erst ab fünf Zeichen,
mehrdeutig heißt ohne Treffer; Band je Stoffwert, fehlt λ, gilt d/R, wenn die Datei einen R-Wert trägt;
die Luftschicht steht als Namensliste im Code (die Synonymtabelle verlangt einen Baustoff);
Gegenprobe-Schwelle 50 % (λ streut innerhalb einer Stoffreihe etwa um diesen Betrag); Herkunft
`KATALOG` nur für Aufbauten mit mindestens einem Katalogwert; ohne übergebenen Abgleich bleibt das
Verhalten unverändert; die Summenfelder des Einzonenwegs nutzen den Abgleich nicht.

**Wirkung an den Proben** (Aufbauten ohne → mit Abgleich): `ifc4_haus_materialnamen.ifc` 0 → 6 (20
Namen: 16 Treffer, 3 Sonderfälle, 1 ohne Treffer; mit gemerkter Zuordnung 7), `ifc4_schichten_nullwerte.ifc`
0 → 4, `ifc2x3_schichten.ifc` 0 → 2, `ifc4_schichten.ifc` und `gbxml_haus_si.xml` unverändert (nur
Gegenprobe), `ifc4_haus.ifc` ohne Materialien. **Auskunft** (Projekt 1045, `ifc4_haus_materialnamen.ifc`):
Klassenweg 10,766 MWh, Bauteilweg ohne Abgleich 10,820 MWh, mit Abgleich 8,910 MWh (0,82) — vor allem,
weil nach E45/2 die Schichten den U-Wert tragen (Wand 0,23 statt 0,4 der Datei); die Abweichungen sind
gemeldet.

**Abnahme** (Merge in den Arbeitszweig, gegen `2026-09-25_R19_BhkwNetzbezug`): Kern-Filter,
Windows-Schale und Referenzlauf je 0 Fehler; Kern 7 678 grün (einer übersprungen), UI 6 453, KiKern
549, SpeicherEngine 386, SpeicherPlanung 27 (einer übersprungen); Referenzlauf 14/14 **PASS**
(4 610 207 Werte); SQL-Dialekt-Prüfer 1 960 Texte, 0 Fundstellen; Auslieferungsvorlage 38/38;
Testdatenbank 145 mit `integrity_check` ok und leerem `foreign_key_check`.

**Umnummeriert:** Der Schemaschritt heißt **146** (umnummeriert, weil #522 die 145 zuerst belegte:
Zapfprofilgenerator T5 „Konstruktor“, `edcfb4a1`). Geändert hat sich allein die Zahl bei
`BaustoffabgleichSchema.SCHRITT`; die Testdatenbank ist die Fassung von origin (Schemastand 145) mit
dem nachgezogenen Schritt 146 (`Referenzlaeufe/LIESMICH.md`).

**Offen:** Welle 2 — der Abschnitt „Baustoffe“ im Importdialog (Treffer je Name, eigene Zuordnung, die
das Projekt beim Speichern merkt), die Saat-Lesenaht für den Wirt der Rasterprobe, die Wiki-Quelle.
Erledigt mit Abschnitt 11.

## 11 Nachtrag: Namensabgleich, Welle 2 — der Abschnitt „Baustoffe“ (26.09.2026)

**Commits** (Opus-Agent im eigenen Worktree; zweimal durch einen Neustart des Programms unterbrochen,
der Zwischenstand lag jeweils committet): `d0912bbc` Kern (die Zuordnungen des Dialogs reisen mit der
Projektzeile, `WizardCtrl.GebaeudeZuordnungAnlegen` merkt oder vergisst sie als ersten Schritt im
Vorgang), `d8d630c1` Abschnitt „Baustoffe“ (Hülle, Datenobjekte, Dialog, CSS, 32 Texte in beiden
Sprachen), `4dfff11e` Tests, `2d58d962` Wirt der Rasterprobe, `08f6f33b` Wiki-Quelle „Gebäudeimport“,
`24601a58` Merge des Arbeitszweigs mit Schritt 146.

**Gebaut.** Im Importdialog unter „Bauteile (echte Hülle)“ der Abschnitt „Baustoffe“: je Materialname
der Datei die Zahl der Schichten, die Stufe (genauer Name, Synonym, Wortanfang, Luftschicht, verworfen,
eigene Zuordnung, ohne Treffer), der zugeordnete Baustoff und die Herkunft der Werte; eine Klappliste der
Katalogbaustoffe nach Gruppe (herstellerneutrale zuerst), „Zuordnung entfernen“ bei einer eigenen
Zuordnung; Namen ohne Treffer gelb, oben eine Zusammenfassung; jede Änderung bildet den Bauteilvorschlag
neu. Die Hülle nimmt den Abgleich des Projekts einmal je Dialog aus der Datenbank, ohne Projekt Katalog
und Synonyme aus der Saat.

**Festlegungen.** (1) Gemerkt wird auch ohne Bauteilschalter — die Zuordnung beschreibt die Namen der
Datei und gilt für das Projekt; Abbrechen verwirft alles. (2) Keine Verwaltung der Synonyme,
`KatalogRegistry` unberührt. (3) Schlüssel ist der normalisierte Name, `null` heißt „gemerkte
Zuordnung entfernen“; in die Herkunft kommen nur wirksame Zuordnungen zu sichtbaren Namen. (4) Ein
weiterer Import derselben Gebäudeliste sieht die noch ungespeicherten Zuordnungen früherer Importe wie
gemerkte. (5) Die Zuordnungen werden als erster Schritt geschrieben, damit ein späterer Fehler sie
zurückrollt. (6) Die Stufe N7 heißt in der Anzeige „eigene Zuordnung“. (7) Die Klappliste ist ein
eigenes `select` mit Gruppen; `Auswahlfeld` bleibt unverändert. (8) Eine neue Datei oder ein anderes
Gebäude verwirft die ungespeicherten Zuordnungen. (9) Kein Schemaschritt.

**Was der Abschnitt zeigt.** `ifc4_haus_materialnamen.ifc`: „16 von 20 zugeordnet, 1 ohne Treffer“
(Fußbodenaufbau gelb; Air als Luftschicht, zwei Schraffuren verworfen), mit Fußbodenaufbau →
Zementestrich 7 statt 6 Aufbauten und „17 von 20 zugeordnet, 0 ohne Treffer“. `gbxml_haus_si.xml`: „10
Materialnamen, alle Stoffwerte aus der Datei; 8 davon im Katalog gefunden (Gegenprobe)“.
`ifc2x3_schichten.ifc`: „3 von 4 zugeordnet, 1 ohne Treffer“ (Mauerwerk mehrdeutig), mit eigener
Zuordnung 4 statt 2 Aufbauten. Im Wirt der Rasterprobe ohne Datenbank im Browser geprüft (Klappliste,
gelbe Zeile, Neubildung, OK, Übernahme mit der Zuordnung).

**Abnahme** (auf `24601a58`, gegen `2026-09-25_R19_BhkwNetzbezug`): Kern-Filter, Windows-Schale, Wirt
und Referenzlauf je 0 Fehler; Kern 7 840 grün (einer übersprungen), UI 6 464, KiKern 549,
SpeicherEngine 386, SpeicherPlanung 27 (einer übersprungen); Referenzlauf 14/14 **PASS** (4 610 207
Werte); SQL-Dialekt-Prüfer 1 967 Texte, 0 Fundstellen; Auslieferungsvorlage 38/38.

**Offen:** die Windows-Sichtabnahme; der Wiki-Upload der Seite „Gebäudeimport“ mit dem Sammel-Upload
1.2.0.4 (der Logbuch-Satz von G4b nennt den Namensabgleich mit, Regel 13.4); eine Ansicht der gemerkten
Zuordnungen eines Projekts außerhalb des Importdialogs (**erledigt**, Abschnitt 12); die Klappliste
trägt je Zeile den ganzen Katalog (bei Dateien mit sehr vielen Materialnamen in der Anwendung nicht gemessen). ~~Wird eine Importzeile vor
dem Speichern wieder entfernt, gelten ihre Zuordnungen bis zum erneuten Öffnen des Dialogs als
vorgemerkt.~~ **Trifft nicht zu** — die Vormerkung wird je Import aus den verbliebenen Zeilen gebildet
(Abschnitt 12).

## 12 Nachtrag: Nacharbeiten (26.09.2026)

Auftrag des Anwenders vom 26.09.2026: „Kleine Nacharbeiten an G4b, dann G6c …“ (Leitkonzept N1.57). Drei
kleine Punkte, zwei davon aus „Offen“ in Abschnitt 11; kein Schemaschritt, ergebnisneutral.

1. **Azimut der Bauteilliste** im Importdialog auf eine Nachkommastelle gerundet — nur die Anzeige,
   das Bauteil behält den Wert der Datei; was auf 360° rundet, zeigt 0°.
2. **Vorgemerkte Zuordnungen entfernter Importzeilen** — kein Fehler: `GebaeudeHuelle.Vorgemerkt` bildet
   die Vormerkung je Klick auf „Importieren…“ aus den Zeilen, die dann noch in der Liste stehen; eine vor dem
   Speichern entfernte Importzeile nimmt ihre Zuordnungen mit, und das Speichern der Liste merkt keine.
   Jetzt mit einem Test über die Testdatenbank gehalten, der Kommentar nennt es. Der Satz in Abschnitt 11
   „Offen“ trifft damit nicht zu.
3. **Ansicht der gemerkten Zuordnungen:** Knopf „Baustoff-Zuordnungen…“ in der Fußleiste des
   Gebäudedialogs; der Dialog zeigt je Zuordnung des Projekts Materialnamen, Baustoff und Zeitpunkt und
   entfernt einzelne (`BaustoffabgleichCtrl.GemerkteJeProjekt`, `BaustoffzuordnungenHuelle`,
   `BaustoffzuordnungenDialog`, Texte in beiden Sprachen, Hilfeanker `baustoffzuordnungen`, Hilfepräfix
   im Bereich Gebäude des Assistenten); Wiki-Quellen „Gebäude“ und „Gebäudeimport“ nachgezogen. Kein neuer
   Logbuch-Satz: Die Ansicht gehört zum Gebäudeimport derselben Version 1.2.0.4 (Regel 13.4).

**Commits:** `395ebb00` Azimut, `8a273e6f` Vormerkung mit Test, `a4c7c76e` Ansicht der Zuordnungen,
`19693c5a` Wiki-Quellen, `d5f762a7` Hilfepräfix im KI-Kontext; `02f7513e` Merge in den Arbeitszweig,
`c6f3c907` Merge mit origin (G6b W5, Basis R20), `f2217656` Papiere.

**Gate** (auf `f2217656`, nach dem Merge mit origin, gegen `2026-09-26_R20_Zapfprofil`): Kern-Filter,
Windows-Schale, Wirt der Rasterprobe und Referenzlauf je 0 Fehler; Kern 8 125 grün (einer
übersprungen), UI 6 559, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 (einer übersprungen);
Referenzlauf 14/14 **PASS** (4 610 207 Werte, 432 CSV byte-gleich); SQL-Dialekt-Prüfer 1 991 Texte,
0 Fundstellen; Auslieferungsvorlage 38/38.

**Offen:** die Windows-Sichtabnahme (Anwender) samt Knopf „Baustoff-Zuordnungen…“; der Wiki-Upload der
Seiten „Gebäudeimport“ und „Gebäude“ mit dem Sammel-Upload 1.2.0.4.
