# Umsetzungskonzept: Gebäudesimulation VDI 6007 in EPOS-Plan — Einbindung, Gebäudedialog, IFC-Import

**Rev. 4 — 17.09.2026 — Umsetzungsentwurf, zur Abnahme durch Philipp**

> **Nachzug 25.09.2026 — Abschluss G4b** ([Protokoll G4b](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G4b_Bauteilimport.md),
> [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.49): Mit **E44** ist G4b vor der
> Feldphase von G4a gebaut und abgeschlossen. Ein importiertes Gebäude kommt auf Wunsch — Schalter „Als
> Zone mit Bauteilen übernehmen“ im Zuordnungsdialog — mit **einer** Zone, Bauteilzeilen und Aufbauten
> samt Schichten in die Projektliste und rechnet über den Bauteilweg; die Rechenregeln stehen in **E45**
> (innere Masse nach Datenlage, U-Wert leer neben vollständigen Schichten, Vorhangfassaden transparent).
> Dazu das Zielfeld Innenflächenfaktor (3.4). Kein Schemaschritt, ergebnisneutral. Nachgezogen in 3.1,
> 3.4, 3.8 und Kapitel 4.

> **Nachzug 25.09.2026 — Abschluss G3** ([Protokoll G3](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G3_Bauteilkatalog.md),
> [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.44–N1.46): Die Stufe G3 ist gebaut und
> abgenommen; Kapitel 4, Zeile G3, nennt Inhalt und erfüllte Abnahme.

> **Nachgezogen 22.09.2026:** Der Klimaspalten-Schritt M4 ist durch Schemaschritt 95 vorweggenommen
> und steht in 1.7 und Kapitel 4 als erbracht, in keiner Summe; die Ergebnisreihen des
> Eingangsbauers (1.4) folgen der Ausgabe von Schritt E der Rechenschritte (F-P2); Schemastand
> 22.09.2026: 100, nächste freie 101. **Entscheid E27** (22.09.2026,
> [Konzept N1.32](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)) ist eingearbeitet: U1, U3,
> U5, U7, U8, U10, U12 und U17 nach Empfehlung entschieden, bei U6 das Verfahren; Q24 als
> Ablösekriterium mit vier Bedingungen, Q25 als vollständige Ablösung nach der Löschliste; A15
> und D1 (gbXML-Import vor IFC-Import) entschieden (1.2, 1.5–1.9, 1.10, 2.7, 2.9, 3.6, 3.8,
> Kapitel 4 bis 6).

> **Nachgezogen 23.09.2026:** **Entscheid E29** ([Konzept N1.34](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md))
> trifft die Endwahl zu U6 — der Zeitbezug der Sonnengeometrie ist der **Stundenanfang**, wie
> Photovoltaik und Solarthermie (1.2, Kapitel 5).

> **Nachgezogen 24.09.2026:** **Entscheid E38** ([Konzept N1.43](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md))
> entscheidet U13, U14 und U15 nach Empfehlung — eines je Lauf, Fensterabzug an der Wandfläche, ψ
> als Vorgabe je Baualtersklasse und Anschlusslängen leer, für den IFC- wie für den gbXML-Import —,
> legt für G4a genau einen iOS-Lauf fest, nur nach ausdrücklicher Rückfrage bei der Abnahme, und
> beauftragt die Stufe G4: zuerst G4c, dann G4a, G4b erst nach G3 und nachdem G4a im Feld war (3.4
> bis 3.8, Kapitel 4 und 5; G4b mit **E44** vor der Feldphase von G4a gebaut, Konzept N1.49). Damit
> ist keine Frage dieses Papiers mehr offen.

> **Nachzug 25.09.2026 — Umsetzung G4** ([Protokoll G4](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-24_G4_Importe.md)):
> G4c (gbXML) und G4a (IFC) sind gebaut und im Gebäudedialog angebunden; G4b ist gebaut (Nachzug G4b
> oben). An
> benannten Stellen weicht der gebaute Stand begründet von Kapitel 3 ab: Der Import legt ein
> **neues** Gebäude an — Zuordnungsdialog → vorbelegter Gebäudeeditor im Modus Neu → Katalogsatz →
> die neue Zeile samt ausstehender Herkunft in der Projektliste → das Speichern der Gebäudeliste
> schreibt Projektkopie und Herkunft in einem Vorgang —, statt über `UebernehmenInsProjekt` eine
> vorhandene Projektzeile zu beschreiben (3.1, 3.2). Klassen, Dialog und Hülle tragen die Namen des
> [Datenaustauschkonzepts](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) und der
> [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) (3.3); der Einstieg ist
> ein Knopf „Importieren (gbXML, IFC)…" im Gebäudedialog, das Profil folgt der Dateiendung; die
> Herkunft liegt im Schemaschritt 138 (S-F). Die Arbeitsentscheide der Umsetzung stehen an ihren
> Stellen in 3.4 und 3.5, Paket, Lizenzseite und iOS in 3.6, die Proben in 3.7, der Stand je Teil in
> 3.8 und Kapitel 4. Offen: die Windows-Sichtabnahme, der eine iOS-Lauf zur Abnahme von G4a nur nach
> Rückfrage (E38) samt der erst dort gemessenen iOS-Größengrenze und dem Trimming-Nachweis, der
> Schemaschritt `Baujahr` (G4a, in Arbeit).

> **Rev. 4 — Prüfung 17.09.2026, E26 eingearbeitet.** Diese Fassung nimmt die Stufe **GA — Altweg
> ablösen** als letzte Stufe ohne Termin wieder auf (Kapitel 4) und führt ihre **Löschliste** in
> Kapitel 6; sie stellt den Vertrag des Vorbereitungsschritts, den zweiteiligen Klimakalender, den
> Fehlerweg des VDI-Moduls, den Rückweg-Test und die Modultrennungswache auf die Festlegungen der
> Prüfung um, ersetzt die festen Schemaschrittnummern durch die Papiernamen **M3** und **M4** und
> zieht Kennzahlen, Ergebnisexport und Aufwandszahlen nach.
>
> **Rev. 3 — Entscheid E20 eingearbeitet (Trennung der Rechenwege, 16.09.2026), dazu der Zusatz
> E21 (Kältebedarf) und E19 (Nutzfläche). Rev. 2 — Korrekturen des Gegenlesens vom 15.09.2026,
> Protokoll: [Gegenlesen](Gebaeudesimulation/2026-09-15_Gegenlesen_Umsetzungskonzept.md)**

Auftrag (Anwender, 15.09.2026, im Wortlaut):

> „Prüfe die Einbindung der VDI 6007 Gebäudesimulation in EPOS-Plan. Nutze vorhandene Daten und
> Möglichkeiten. Prüfe den vorhandenen Gebäudeeditor/Dialog auf Änderungen für die Parameter und
> Eingaben der VDI 6007. Prüfe auch den Import von Daten mit einer IFC-Datei. Erstelle ein Konzept,
> wie mit einem Gebäudeimport umgegangen werden kann."

Grundlage ist das Konzept
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Rev. 3 mit Nachtrag 1) und die dort getroffenen Entscheide **E1** (Stundenmodell ist die Vorgabe
für alle Gebäude), **E2** (Bestandsgewichte im Stundenmodell gestrichen, Dialog auf U·A),
**E3** (xBIM als unverändertes NuGet-Paket), **E4** (GB als eigener Einfrierschritt vor G1),
**E5** (Klimabasis sind die vorliegenden PVGIS-TMY-Reihen), **E7/E8** (Einzonenmodell zuerst, Skalierung bleibt), **E9** (Import **und**
Export von gbXML und IFC: der gbXML-Import wird Pflicht in G4, die beiden Exporte werden Stufe G7;
Konzept N1.13) und **E10** (Druckrundung als Toleranz der Normprüfregel; Konzept N1.15).

Dazu die Entscheide vom 16.09.2026: **E19** (die Bezugsfläche heißt `Nutzflaeche`; Konzept N1.24),
**E20** (die beiden Rechenwege werden vollständig getrennt, VDI 6007 ist die Vorgabe; Konzept N1.25,
[`ADR-006`](ADR-006_Trennung_Altweg_VDI6007.md)), **E23** (der Tagesbilanz-Weg bleibt als
eingefrorener **Bestandsweg** im Produkt wählbar) und der Zusatz **E21** (die Kältebedarfsrechnung
wird der Wärmebedarfsrechnung nachgebildet; 1.1, 1.4).

Dazu der Entscheid vom 17.09.2026: **E26** (Konzept N1.31) hält fest, dass der Altweg ein
**Übergang** ist und der VDI-Weg ihn später vollständig ablöst und eigenständig arbeitet. Die
Stufe **GA — Altweg ablösen** ist damit die letzte Stufe des Plans. Mit dem Entscheid vom
22.09.2026, **E27** ([Konzept N1.32](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)), ist
entschieden: GA wird beauftragbar und fällig, sobald die vier Bedingungen des Ablösekriteriums
**Q24** erfüllt sind (Kapitel 5), und ihr Umfang ist die **vollständige Ablösung** nach der
**Löschliste** in Kapitel 6 (**Q25**). E27 beantwortet zugleich die offenen Fragen dieses Papiers
(Kapitel 5) und die Architekturfragen, auf die es sich stützt; **E28** (22.09.2026, Konzept N1.33)
entscheidet danach U4 und U9 nach Empfehlung, **E38** (24.09.2026, Konzept N1.43) die letzten
drei, U13, U14 und U15, und beauftragt die Stufe G4. Die Festlegungen der Prüfung
vom 17.09.2026 (F-Ü1 … F-D1) stehen im
[Register](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md), Kapitel 8.

Die Befunde, auf denen jede Codeaussage dieses Papiers steht:

| Befund | Gegenstand |
|---|---|
| [`Gebaeudesimulation/2026-09-15_Befund_L_Einbindung_Kern.md`](Gebaeudesimulation/2026-09-15_Befund_L_Einbindung_Kern.md) | Einbindung in den Rechenkern: Naht, Klima, Persistenz, Migration, Referenzlauf, Tests, Merge-Folge |
| [`Gebaeudesimulation/2026-09-15_Befund_M_Gebaeudedialog.md`](Gebaeudesimulation/2026-09-15_Befund_M_Gebaeudedialog.md) | Gebäudeeditor und Bedarfsdialog: Ist-Inventar, Soll-Entwurf, Hülle, Texte, bunit-Fälle |
| [`Gebaeudesimulation/2026-09-15_Befund_N_IFC-Import_Entwurf.md`](Gebaeudesimulation/2026-09-15_Befund_N_IFC-Import_Entwurf.md) | IFC-Import: Importmuster, xBIM-Paket und Lizenz, Klassen, Abbildungsregeln, Sonderfälle, Plattform |
| [`Gebaeudesimulation/2026-09-15_Befund_R_gbXML_Schema_Werkzeuge.md`](Gebaeudesimulation/2026-09-15_Befund_R_gbXML_Schema_Werkzeuge.md) | gbXML: Schema und Versionswert, was die Autorensysteme liefern, LINQ to XML statt `XmlSerializer`, Aufwand von Import und Export (E9) |
| [`Gebaeudesimulation/2026-09-15_Befund_S_IFC-Export_ohne_Geometriekernel.md`](Gebaeudesimulation/2026-09-15_Befund_S_IFC-Export_ohne_Geometriekernel.md) | IFC-Export ohne Geometriekernel, Rückgabe angereicherter Dateien — und die daraus folgende Auflage an **diesen** Import (3.2) |
| [`Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md`](Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md) | Feldzuordnung je Spalte (nur Altweg / beide / nur VDI), Aufrufstellen des Tagesbilanz-Wegs, was der Vorbereitungsschritt liefern muss, Umfang der Verschiebung (E20); sein Kapitel 4 ist die Grundlage der **Löschliste der Stufe GA** (Kapitel 6) |

Dieses Papier sagt, **was gebaut wird, in welcher Reihenfolge und woran es abgenommen ist**. Es
erfindet nichts: Jede Aussage über den Quelltext trägt Datei und Zeile aus den drei Befunden; was
dort nicht belegt ist, steht hier ausdrücklich als offen. Was das Konzept sagt und dieses Papier
korrigiert, ist je Stelle benannt (1.10).

---

## 0. Das Ergebnis in sechs Punkten

1. **Die Einbindung hat genau eine Weiche, und sie sitzt am Eingang** (E20, 16.09.2026).
   `SimulationWaermebedarf.HeizwaermeEinesGebaeudes`
   (`EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:566`) ist der einzige Ort, an dem
   die Wärme eines Gebäudes entsteht; `GebaeudeBedarfCtrl.Rechnen`
   (`EPOS.Kern/Controller/GebaeudeBedarfCtrl.cs:94`) ruft dieselben zwei Methoden (`:112`, `:117`)
   und trägt die Weiche damit automatisch mit. Vor der Weiche steht ein **modellfreier
   Vorbereitungsschritt** (Klimakalender in zwei Teilen, `VerbrauchNeu`, die beiden Flächen der
   Skalierung nach E8; sein Vertrag steht an einer Stelle, Softwarearchitektur 1.3 — F-Ü1);
   hinter ihr stehen **zwei getrennte Module**:
   `Simulation/Altweg/` (die Tagesbilanz, Zeichen für Zeichen verschoben, ohne neue Funktion) und
   `Simulation/Gebaeude/` (VDI 6007), und keines ruft das andere. Die
   Verbrauchs-Rückrechnung nimmt `VerbrauchAltKwh` aus **demselben** Modul — das VDI-Modul liefert
   ihn aus einem Lauf, den Altweg ruft die Fassade wie im Bestand zweimal (F-Ü2) —, statt an einer
   zweiten Stelle zu verzweigen; damit ist der frühere zweite Verzweigungspunkt gegenstandslos
   (A16). Der Puffer
   (`:188`, Watt) und die eine Umrechnung nach kW (`:222`) bleiben unberührt. Der Altweg ist der
   **eingefrorene Bestandsweg des Übergangs** (E23, E26): Er steht neben dem VDI-Weg, samt Weiche,
   Schalter und Spalten, bekommt keine neue Funktion und fällt mit der Stufe **GA — Altweg
   ablösen**, die fällig wird, sobald das Ablösekriterium Q24 erfüllt ist (E27).

2. **Zwei stille Fallen entscheiden über die Reihenfolge der Merges.** Das Modellfeld
   `Fensterflaeche_Ost` trägt heute die Spalte `Fensterflaeche_Ost_West`
   (`EPOS.Kern/Controller/ProjektGebaeudeCtrl.cs:56`, `GebaeudeCtrl.cs:66`); wer die neue Spalte
   `Fensterflaeche_Ost` anlegt, bevor das Feld auf `Fensterflaeche_OstWest` umbenannt ist, verliert
   die Ost-/Westfenster des Tagesmodells ohne jede Meldung. Und
   `GebaeudeStammCtrl.CopyFromStamm` (`EPOS.Kern/Controller/GebaeudeStammCtrl.cs:439`) bildet jedes
   `DBNull` auf `0.0` bzw. `""` ab — das zerstört „NULL = Vorgabe" für die dreizehn nullbaren der
   fünfzehn neuen Spalten (1.6). Beides
   ist behebbar, aber nur in dieser Reihenfolge: **Umbenennung vor Schema, Schema vor Modell.**

3. **Der Gebäudedialog ist fünf Masken, und der Editor, den E2 trifft, ist nicht der, den das
   Konzept nennt.** `EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDialog.razor` (983 Z.) führt alle Größen
   der U·A-Tabelle bereits — verteilt auf vier Gruppen und zwei Reiter; `GebaeudeDialog.razor`
   (828 Z.) ist reiner Wirt ohne ein einziges Fachfeld. Der Umbau ist deshalb eine **Umordnung mit
   zwei neuen Feldern** (Fenster Ost, Fenster West) und einer neuen Gruppe „Modellparameter
   (VDI 6007)" — keine
   neue Maske. Nach E20 folgen **alle** Gebäudemasken der VDI-6007-Struktur; die Felder, die nur
   der Altweg liest, stehen in einem eingeklappten Abschnitt **„Tagesbilanz (Bestandsweg)"** — nach
   [Befund X](Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md) sind das
   **vier Spalten**, davon zwei sichtbare. Gemessener Aufwand: **9,0 PT** allein für die
   Oberfläche (8,0 PT Umbau + 1,0 PT Bestandswegabschnitt), mehr als die Hälfte des im Konzept für
   ganz G1 veranschlagten Rahmens.

4. **Der IFC-Import fügt sich ohne Architekturbruch ein, aber das Paket ist enger zu fassen als
   E3 es beschreibt.** `Xbim.Essentials` zieht `Xbim.Ifc` und damit `Xbim.IO.Esent` (Windows,
   ManagedEsent) nach; in den Kern gehört **allein `Xbim.IO.MemoryModel` 6.1.605**, das
   `Xbim.Common`, `Xbim.Ifc2x3`, `Xbim.Ifc4` und `Xbim.Ifc4x3` von selbst mitbringt. Ein Leser
   bedient alle drei Schemata über `Xbim.Ifc4.Interfaces.IIfc*`; `IfcStore` wird nie gerufen.

5. **Drei Dinge fehlen im Bestand und müssen mitgebaut werden, sonst ist die Stufe nicht
   abnehmbar:** eine Lizenzhinweisseite im Installationspaket (CDDL § 3.1 verlangt den
   Quellenverweis an den Empfänger; `Setup/EPOS-Plan.iss:164`, `:330` kennt nur `Lizenz.rtf`), ein
   Größenlimit für Importdateien (im ganzen Importbestand gibt es keins) und eine Vorrichtung für
   **nicht ausgelieferte** Normprüfdaten — mit der Folge, dass der Nachweis der zwölf Normtestfälle
   ein **lokaler** ist, kein CI-Nachweis.

6. **Reihenfolge:** G0 (Löser, keine Wirkung) → GB (Bestandsbefunde) → M2 Umbenennung → M3 Schema
   → G1 + G2 gemeinsam → G3 → G4 → G5 (der Klimaspalten-Schritt M4 ist durch Schemaschritt 95
   vorweggenommen, 1.7), und als letzte Stufe **GA — Altweg
   ablösen** (E26, 17.09.2026), **fällig nach dem Ablösekriterium Q24 (E27)**: Der
   Tagesbilanz-Weg trägt den Übergang, der VDI-Weg löst ihn später vollständig ab. Was GA
   entfernt, steht als **Löschliste** in Kapitel 6 (Q25, vollständige Ablösung nach E27); ihre
   5–8 PT stecken in keiner Summe dieses Papiers.
   **G1 beginnt mit der Verschiebung des Altwegs** in sein Modul und einem **byte-gleichen**
   Referenzlauf — erst danach wird der VDI-Weg angebunden. **G4 trägt nach E9 zwei Importe:** den
   IFC-Import (G4a/G4b, Kapitel 3) und den **gbXML-Import als Pflichtteil G4c**; die beiden
   Exporte nach gbXML und IFC sind Stufe **G7** und Gegenstand des
   [Datenaustauschkonzepts](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md). G5 bleibt die
   Geometrieableitung, nur bei Bedarf aus der Praxis. **Die Basis wird in diesem Papier zweimal neu
   eingefroren: mit GB und mit G1 + G2**; der nächste Anlass der Einfrierkette, **G6d**, gehört zum
   [Mehrzonenmodell](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md); **GA schließt die Kette ab** (1.8).
   Alles dazwischen muss
   `GESAMT: PASS` melden — sonst ist hinterher nicht
   mehr zu sagen, ob eine Abweichung vom Schema oder vom Modell kommt.

---

## 1. Einbindung in den Kern

### 1.1 Der Weg durch den Bestand und die eine Verzweigung

`SimulationWaermebedarf.Waermebedarf_berechnen(int ID_Projekt, int ID_Klimaregion)`
(`SimulationWaermebedarf.cs:128`) ist der Einstieg; gerufen wird er aus
`SimulationRunner.Simuliere_Intern` (`EPOS.Kern/Allgemein/Simulation/SimulationRunner.cs:184`) und
aus `SimulationLaufCtrl.Bedarf` (`EPOS.Kern/Controller/SimulationLaufCtrl.cs:114-120`). Die
Klimaregion kommt aus `ProjektCtrl.ReadSingle`, eine 0 bricht benannt ab
(`SimulationRunner.cs:163-170`); davor steht die Schemasperre `SchemaStand.SimulationGesperrt`
(`SimulationRunner.cs:137`).

Die Gebäudeschleife und ihre Einheiten:

| Zeile | Was geschieht | Einheit |
|---|---|---|
| `:175`, `:177-178` | `KlimakalenderLesen(ID_Klimaregion)`, `ProjektGebaeudeCtrl.ReadAll(ID_Projekt)` | — |
| `:188` | `double[] Waermebedarf_EinGebaeude = new double[8760]` — **ein** Puffer je Durchlauf | **W** |
| `:197` | `HeizwaermeEinesGebaeudes(ctrl.items[i], i, Waermebedarf_EinGebaeude)`; `false` bricht den Lauf ab | — |
| `:201`, `:204`, `:208` | Addition in `kanalHeizung`, in die unabhängige Energieprobe (`:172`) und in `Waermebedarf_Gebaeude` | W |
| `:212` | `MaxP[i] = Maximaler_Waermebedarf(…)` | W |
| `:222` | `BhkwPlan.WattToKw(kanalHeizung)` — **die eine Umrechnung** | kW |
| `:228`, `:321` | `Waermebedarf_Gebaeude_Gesamt = kanalHeizung.Sum() / 1000`, `BhkwPlan.MonatsSumme(…)` | MWh |
| `:401` | `Waermebedarf_Max` — das `Waermelast_Max` des Projekts | kW |

`HeizwaermeEinesGebaeudes` selbst (`:566`) läuft heute in fünf Schritten: Einheitenzweig
(`:569-576`), **Tagesmodell `Berechnung_Gebaeude_Tageswerte(item, index)` (`:581`)**,
Tagesverteilung lesen (`:584`), Abbruch bei fehlender Verteilung (`:590-595`), `VectorInit(ziel)`
und `BhkwPlan.StdWerte(…)` (`:598-608`). Genau dieser Rumpf wird aufgeteilt.

**Die Weiche sitzt am Eingang** (E20, 16.09.2026). `HeizwaermeEinesGebaeudes` wird zur **Fassade**
und schrumpft auf rund zwanzig Zeilen:

1. **Der modellfreie Vorbereitungsschritt** liefert, was **ohne einen Modellauf feststeht**. Sein
   Vertrag steht an **einer** Stelle —
   [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 1.3, Baustein
   `GebaeudeVorbereitung` —, jedes andere Papier verweist darauf (F-Ü1). Modellfrei sind: der
   **Klimakalender** (Punkt 2), **`VerbrauchNeu` je Einheit** (`:617-636`, reine
   Einheitenumrechnung), **`FlaecheAlt = Wohnflaeche_gesamt`** (die Spalte behält nach E19 ihren
   Datenbanknamen, Konzept N1.24), **`Flaeche_Nutzer`**, **`Einheit`** und
   **`Jahresnutzungsgrad`**. **Bewohnerzahl und Skalierungsfaktor nach E8 entstehen dagegen je
   Modul aus dessen erstem Lauf** — die Fassade führt die Schleife. Den Faktor wendet der
   Vorbereitungsschritt nicht an: im Altweg steckt er im Rückgabewert der Tagesrechnung
   (`BhkwPlan.cs:435`), im VDI-Weg ist er eine Nachmultiplikation (1.5).
2. **Der Klimakalender hat zwei Teile** (F-Ü3). `Klimakalender.Gemeinsam` trägt `WE[365]`,
   `Stundentemperatur[8760]`, `WochentagJan1` und die Monatsgrenzen; `Klimakalender.Altweg` trägt
   `Sol_*`, `A_Temp` und `TagTyp_W/NW` — die isotropen Tagesmittel und die Tagestypen, die **allein
   die Tagesbilanz** braucht (heute `KlimakalenderLesen:513-537` und
   `Stundentemperatur_aus_DB:912-920`, Anweisung für Anweisung). Die Weiche reicht dem VDI-Modul
   **nur** `Gemeinsam`, dem Altweg **beides**; der Altweg-Teil fällt mit der Stufe GA.
3. **Die Weiche** liest den Rechenweg des Gebäudes (`Tab_Gebaeude.Gebaeude_Modell`; NULL = VDI 6007
   nach E1) und ruft **genau ein Modul**.
4. **Zwei getrennte Module.** `EPOS.Kern/Allgemein/Simulation/Altweg/` trägt den Tagesbilanz-Weg —
   Zeichen für Zeichen verschoben, ohne neue Funktion —, `Simulation/Gebaeude/` den VDI-Weg
   (1.4). **Der VDI-Weg ruft nichts aus dem Altweg**, und ein Wächter hält das fest (1.9).

**Weiche und Altwegmodul leben, solange der Altweg lebt** (E23, E26). Der Tagesbilanz-Weg ist der
**eingefrorene Bestandsweg des Übergangs**: VDI 6007 ist die Vorgabe (E1), die Tagesbilanz bleibt
ausdrücklich wählbar, bis die Stufe **GA** sie ablöst (fällig nach dem Ablösekriterium Q24,
E27). **Fassade und
Vorbereitungsschritt bleiben über die Ablösung hinaus** — sie sind die Gliederung der
Gebäudebedarfsrechnung und tragen auch danach Klima, Verbrauch und Flächen. **Weiche,
`IGebaeudeRechenweg` und die Modultrennungswache leben bis GA** und stehen mit ihrem Umfang in der
Löschliste (Kapitel 6).

**Die Verbrauchs-Rückrechnung ist kein zweiter Verzweigungspunkt mehr.** Sie braucht
`VerbrauchAlt` aus **demselben** Modell, das anschließend den Bedarf rechnet
(heute `Bewohner_und_Flaeche_berechnen:613` → `:647` → `:650`). `IGebaeudeRechenweg.Rechnen` liefert
dafür aus **einem** Aufruf die Reihe **und** den unskalierten Jahreswert `VerbrauchAltKwh` (F-Ü2).
Das **VDI-Modul läuft damit einmal**; die Skalierung ist eine Nachmultiplikation in der Fassade
([Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 8.3). Der **Altweg wird im
Verbrauchsfall zweimal gerufen** — in der Reihenfolge des Bestands (`:645` → Lauf 1 → `:652` →
Lauf 2) —, weil dort der Faktor in der Tagesrechnung selbst steckt und der Lauf sonst nicht
byte-gleich bliebe (Befund X 3.4). Beides geschieht **hinter derselben Weiche**; damit ist
Architekturfrage **A16** (Zuschnitt des zweiten Verzweigungspunkts) gegenstandslos, und die
Verhältnisrechnung kann zwei Modelle nicht mehr mischen.

Alles nach der Weiche (`:201-212`, Kanal, Summen, Dauerlinie, Energieprobe) bleibt Zeichen für
Zeichen — genau wie Konzept 4.1 es zusagt. Weil `GebaeudeBedarfCtrl.Rechnen`
(`GebaeudeBedarfCtrl.cs:94`) dieselben zwei Methoden ruft (`:112` `KlimakalenderLesen`, `:117`
`HeizwaermeEinesGebaeudes`, danach `:121` `WattToKw`, `:126-133` Summe/Höchstwert/Monate), trägt
die eine Weiche den Bedarfsdialog automatisch mit. Das ist die Hausregel „Eine Auskunft ruft den
Rechenweg des Laufs — sie schreibt ihn nicht ab"
([`EPOS.Kern/CLAUDE.md`](../../EPOS.Kern/CLAUDE.md)). Die drei öffentlichen Signaturen
(`Waermebedarf_berechnen`, `KlimakalenderLesen`, `HeizwaermeEinesGebaeudes`) bleiben deshalb
unverändert.

**Was verschoben wird, und was es kostet.** Nach
[Befund X](Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md) wandern drei
Methoden aus `SimulationWaermebedarf.cs` (`Berechnung_Gebaeude_Tageswerte:684-888`,
`DBTagesVeteilung:658-682`, der `StdWerte`-Zweig `:597-608`, rund 250 Zeilen) und **vier
Physikfunktionen** aus `BhkwPlan.cs` (`StdWerte:259`, `SolareGewinneC:311`,
`SpezWaermeverlusteC:342`, `TaeglHeizlastWG:364` samt der statischen Vortemperatur `:51`/`:54`,
rund 185 Zeilen). **`BhkwPlan.cs` wandert nicht als Ganzes:** dieselbe Datei trägt die neun
Vektorhelfer, die jeder Bedarfs- und Erzeugerzweig ruft. Aufwand der Trennung: **3–5 PT**
zusätzlich in G1, abgenommen an einem **byte-gleichen** Referenzlauf (1.8, Kapitel 4).

**Die Kältebedarfsrechnung folgt derselben Bauform** (E21, 16.09.2026). Neben
`SimulationWaermebedarf` steht eine Fassade **`SimulationKaeltebedarf`**, die **denselben**
Vorbereitungsschritt liest und die Kühllast je Stunde aus **demselben Lauf** des VDI-Moduls
`Gebaeude/` entgegennimmt (`GebaeudeModellErgebnis.KuehlbedarfKwh`, 1.4) — **keine zweite
Gebäuderechnung**. Einen Altweg gibt es auf der Kälteseite nicht: Ein Gebäude auf dem
Tagesbilanz-Weg liefert Kältebedarf **0** mit benanntem Hinweis, weil die Tagesbilanz keine
Raumtemperatur führt; dieser Hinweis ist ein Altweg-Sonderfall und steht deshalb in der Löschliste
der Stufe GA (Kapitel 6). Kanal, Senken, Erzeuger und Dialoge der Kälteseite stehen im
[Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) (Stufe KU1), nicht hier.

**Zwei Bestandsgrenzen fallen dabei auf.** `HeizwaermebedarfGeb` (`:31`) und `MaxP` (`:56`) sind
`double[100]`; ein Projekt mit mehr als 100 Gebäuden wirft eine `IndexOutOfRangeException` an
`:816`. `MaxP` wird an `:212` geschrieben und **nirgends gelesen** — toter Bestand; dasselbe gilt
für `Anzahl_Bewohner` (`:11`, gesetzt `:577`) und `Wohnflaeche` (`:12`, gesetzt `:578`), die im
ganzen Bestand keinen Leser haben (Befund X 2.3). Die drei Spitzenkennzahlen des Stundenmodells
(Konzept 4.5) gehören deshalb nicht dorthin, sondern in ein eigenes, benanntes Ergebnisobjekt
(1.4, `GebaeudeModellErgebnis`). Frage U9 — mit E28 (22.09.2026) nach Empfehlung entschieden: die
Grenze fällt in GB, ergebnisneutral.

### 1.2 Der Datenfluss vom Klima bis zum Kanal

| # | Stufe | Fundstelle | Was das Stundenmodell davon braucht |
|---|---|---|---|
| 1 | `KlimakalenderLesen(int)` füllt sieben Tagesreihen `Sol_N/_O/_S/_w`, `A_Temp`, `WE`, `TagTyp_W`, `TagTyp_NW` | `SimulationWaermebedarf.cs:513`, `:519-526` | **nichts als Rechengröße**: die Wochenendmaske `WE[365]` bildet der Vorbereitungsschritt aus dem Ortszeit-Kalender (F-Ü8), `WE` aus `:524` ist die Probe dagegen; die Tagesverteilung entfällt |
| 2 | `Stundentemperatur_aus_DB(int)` ruft `ReadOrtszeit` und nimmt **nur** `Außen_Temp` | `:912`, `:915`, `:919` | die ganze Zeilenliste ist zu behalten statt zu verwerfen (`:914-920`) — ein Feld `_solarOrtszeit` |
| 3 | `SolardatenCtrl.ReadOrtszeit(idKlimaregion, idProjekt)` liest 8 760 Zeilen, merkt die UTC-Herkunft je Zeile und sortiert auf Ortszeit um | `EPOS.Kern/Controller/SolardatenCtrl.cs:156`, `:160-162`, `:174-175`, `:181`, `:195-199` | `Außen_Temp`, `Globalstrahlung`, `Direktstrahlung`, `Diffusstrahlung`, `TagUtc`, `StundeUtc` |
| 4 | `SolarZeitbasis` — datenbankfrei, EU-Regel fest verdrahtet, kein `TimeZoneInfo` | `EPOS.Kern/Allgemein/Simulation/SolarZeitbasis.cs:36`, `:135` | nichts Neues: Sonnenstand auf UTC, Bilanz auf Ortszeit — wie `SimulationPV` und `SimulationSolarthermie` |
| 5 | `SolarCalculator.CalculateHourlyHayDavies(…)` — anisotrop, **ohne statische Seitenwirkung** | `EPOS.Kern/Allgemein/SolarPVGISCalculator.cs:455`, Kopf `:446-452` | **die Funktion je Fensterorientierung**; `Tilt = 90`, `Azimuth ∈ {0, −90, 180, 90}` |
| 6 | Azimutkonvention: Grad gegen **Süd**, Ost −90, Nord 180, West 90; Fassadenneigung 90 | `EPOS.Kern/Allgemein/Import/KlimaImportAblauf.cs:135`, `:132` | die Konvention des Eingangsbauers |
| 7 | Erdreichtemperatur nach Kusuda — **liegt fertig im Kern** | `EPOS.Kern/Allgemein/Simulation/ErdreichTemperatur.cs:411`, Vorgabeboden `:47` (`BODENTYP_DEFAULT`), Katalogzeile `:188` | Dämpfung 0,69 und Phasenverzug 21,4 Tage, also die Konzeptzahlen 0,68/22 auf zwei Stellen. **Kein neuer Kusuda-Code** |
| 8 | `Zonenmodell2K` rechnet 8 760 Blockstunden, liefert `HeizlastW[8760]` | neu (1.3) | schreibt in den vorhandenen Puffer `ziel[]` in Watt |

**Was nicht benutzt werden darf:** `CalculateHourly` (`SolarPVGISCalculator.cs:389`) — die isotrope
Bestandsfunktion schreibt drei statische Felder (`:302-304`, gesetzt `:395`, `:399`, `:401`), die
`SimulationSolarthermie` unmittelbar nach dem Aufruf liest. Das ist prozessweiter Zustand und
verstößt gegen „Zustand je Instanz, nichts Statisches" (Konzept 4.1).

**Der Vergleich in 8.2 trägt deshalb zwei Anteile, nicht einen.** Der Bestandsweg nimmt `Sol_*` aus
`Tab_Klimadaten` — gerechnet mit der **isotropen** `CalculateHourly` beim Klimaimport
(`KlimaImportAblauf.cs:318-322`, Leser `SimulationWaermebedarf.cs:513-526`) —, das Stundenmodell
rechnet anisotrop nach Hay-Davies. Wer die Differenz der Jahressummen allein dem Modellwechsel
zuschreibt, schreibt den Himmelsmodellwechsel mit hinein. Die Aufteilung wird in G1 **einmal
gemessen** (ein Lauf mit `CalculateHourlyHayDavies` gegen einen mit den `Sol_*`-Reihen) und im
Protokoll ausgewiesen.

**Ein dritter Nachweis gehört in G1** (E5 / Konzept N1.10): Blatt 3 (Gl. 29–50, Aydinli/Krochmann)
wird **neben** Hay-Davies implementiert und je Orientierung und Neigung auf den dreizehn
Klimaregionen gegengehalten; berichtet werden Jahressumme, Stundenabweichung und der Anteil
innerhalb ± 5 W/m². Das trägt Q20.

**Zwei Zeitfragen, beide mit E27 (22.09.2026, [Konzept N1.32](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md))
entschieden — die erste im Verfahren, die zweite in der Sache:**

- **Stundenanfang oder Stundenmitte.** `KlimaImportAblauf.Rechnen` übergibt `dt.Hour` aus der
  TMY-Zeitmarke (`KlimaImportAblauf.cs:318-322`), PVGIS liefert `20200101:0000`, `:0100`, … — der
  Zeitbezug ist also der **Stundenanfang**. Konzept N1.10 verlangt für Blatt 3 die Stundenmitte;
  der Unterschied ist eine halbe Stunde Stundenwinkel = 7,5° und verschiebt genau die Ost- und
  Westflächen, deren Trennung G1 neu einführt. **Das Verfahren ist entschieden (U6, E27):** In G1
  werden an **einer** Stelle, im Eingangsbauer, beide Zeitbezüge gemessen und ihre Wirkung auf Ost
  und West beziffert. **Die Endwahl ist mit E29 (23.09.2026, Konzept N1.34) gefallen: der
  Stundenanfang** — die Messung ergab bei Stundenmitte Ost −10,1 %, West +10,5 % und für die
  Jahresheizwärme höchstens +0,10 %; Gebäude, PV und Solarthermie rechnen denselben Sonnenstand,
  eine Umstellung gibt es nur für alle drei gemeinsam. `Tab_Solar.Sol_*` bleibt in jedem Fall
  unberührt (Referenzbasis).
- **Wochenendkalender.** Konzept 4.4 leitet den Wochentag des 1. Januar aus
  `SolardatenCtrl.Referenzjahr(idProjekt)` (`SolardatenCtrl.cs:222`) ab. Das Referenzjahr kommt aus
  der aktiven Spotpreisreihe (`:228-232`, sonst `DbWerte.SOLAR_REFERENZJAHR_STANDARD`, `:242`) und
  entscheidet heute **ausschließlich** über die zwei Sommerzeit-Umstelltage. `Tab_Klimadaten.WE`
  dagegen entsteht im Import aus `datum.DayOfWeek` der TMY-Zeitmarke
  (`KlimaImportAblauf.cs:354`) — also aus dem PVGIS-Jahr 2020. **Entschieden ist (U7, E27) der
  Ortszeit-Kalender** (F-Ü8, Konzept 4.4): Der Vorbereitungsschritt bildet `WE[365]` aus dem
  Wochentag des 1. Januar des Referenzjahres, und eine **Probe** hält die Maske gegen
  `Tab_Klimadaten.WE` derselben Klimaregion — weicht sie ab, ist das ein Befund der Probe, kein
  Rechenfehler; führt ein Projekt eine Preisreihe ≠ 2020, benennt die Probe genau das. Der
  Bestandsweg liest `WE` weiter aus `KlimakalenderLesen:524`. Kapitel 5 (U7), Konzeptkorrektur
  1.10.

### 1.3 Stufe G0 — der Löser im Kern, ohne jede Wirkung

Ort: `EPOS.Kern/Allgemein/Simulation/Gebaeude/`. Alle Dateien ohne `DataRepository`, ohne
`SimulationProtokoll`, ohne Statik, durchgehend `double` (Vorbild
`EPOS.Kern/Allgemein/Simulation/PvErweitertesModell.cs`).

**`ErsatzparameterRC.cs`** — ein `record` mit den reduzierten RC-Größen eines Gebäudes: die beiden
Kapazitäten `C_AW_Jk`/`C_IW_Jk` [J/K], die Widerstände `R_Rest_AW_KW`, `R_1_AW_KW`, `R_1_IW_KW`,
`R_conv_AW_KW`, `R_conv_IW_KW`, `R_rad_KW` und `R_ext_KW` (= H_ve + Σψ·L) [K/W], die
Bezugsflächen `A_AW_opak_M2`/`A_IW_M2` und `SummeUA_opak_WK` für θ_eq; dazu
`static ErsatzparameterRC AusKlassenweg(GebaeudeModellEingang e)`. Der Erbauer trägt die harten
Prüfungen aus Konzept 4.8 — `R_Rest_AW > 0` mit benanntem Fehler (kein stiller Rückfall),
`5 ≤ Bauweise/Wohnflaeche ≤ 200 Wh/(m²K)`, U-Werte 0,1…6 W/(m²K), `0 < g ≤ 1`.

**E14 (16.09.2026): die Fenster liegen im AW-Zweig, nicht im Lüftungszweig.** Der Satz führt
dafür `R_1_AF_KW` (= R_AF/6, nach den Wänden parallel geschaltet, Gl. (25)–(28)) und
`R_Rest_AF_KW` (der Rest des Fensterzweigs **einschließlich äußerem Übergang**, so gebildet, dass
R_1,AF + R_Rest,AF + Flächenanteil am inneren Übergang = 1/(U·A) des Fensters ist und das Fenster
mit vollem U·A in Gl. (27) eingeht); `R_ext_KW` trägt allein Lüftung und Wärmebrücken. Der Erbauer
fasst Wände und Fenster nach Gl. (27)/(28) zu einem Paar zusammen; die Grenzfälle (28a)–(28c)
sind eine Schutzregel für widersprüchliche Eingaben und werden im Satz ausgewiesen. Der Fensterpfad des Prototyps
entfällt damit für das Produkt (Konzept N1.19, Rechenschritte A7a).

**`Zonenmodell2K.cs`** — der Löser. **Der Name folgt der Norm:** die Richtlinie sagt „2-K-Modell",
„7R2C" ist nur Kurzform (Konzept N1.3); Konzept 4.1 und 11 nennen noch `Zonenmodell7R2C` und sind
an dieser Stelle zu korrigieren (1.10).

```csharp
internal sealed class Zonenmodell2K
{
    internal Zonenmodell2K(ErsatzparameterRC p);   // baut A, b-Struktur, Phi, Gamma, Psi einmal
    internal double[] Eigenwerte { get; }          // beide reell und negativ (Probe Konzept 10.2)
    internal void Zuruecksetzen(double thetaStart);
    internal Stundenergebnis Schritt(in Stundenrand r);  // eine Blockstunde
}
```

`Stundenrand` (readonly struct) trägt `ThetaOut`, `ThetaEq`, `ThetaSoll`, `ThetaMax`, die drei
Lasten `PhiRadAW`/`PhiRadIW`/`PhiConv` [W], `HeizleistungMaxW` (`NaN` = unbegrenzt) und
`HeizungStrahlungsanteil`; `Stundenergebnis` trägt `HeizleistungW` und `KuehlleistungW` (Blockmittel,
beide ≥ 0), `ThetaAirMittel`, `ThetaOpMittel` sowie die beiden Endzustände `ThetaMAwEnde`,
`ThetaMIwEnde`. Zustand je Instanz sind zwei `double` — mehr nicht. `Zuruecksetzen` ist keine
Bequemlichkeit, sondern Bedingung: Der Löser läuft je Lauf und je Auskunft neu, und der
Bedarfsdialog holt zwei Auskünfte nacheinander (1.5, 2.7).

**`Bauteilreduktion.cs`** (Kettenmatrix, `System.Numerics.Complex`) ist hier nur als leerer Platz
vorgemerkt; sie kommt mit G3.

**Die Prüfgröße ist das Blockmittel der Stunde**, nicht der Momentanwert und nicht ein gleitendes
Mittel (Konzept N1.2); die Prüfregel ist nach **E10** das Band zwischen den beiden Programmspalten
**± 0,15 K bzw. ± 1,5 W** — Toleranz der Richtlinie plus eine halbe Druckstelle, weil die Tabellen
gerundet gedruckt sind (Konzept N1.15) —, Heizlast **positiv**. Offen aus dem Konzept und in G0 zu
lösen: Testfall 11
(Kühldecke, α_kon 5,0 je Bauteil), Testfall 6 (Vorzeichen), α_kon je Bauteil statt global 2,7,
die Grenze `E = 0 ab Z > 170` aus Blatt 1, 6.8.

**G0 ändert keine einzige Zeile des Bestandswegs.** Es gibt keinen Aufrufer; der Referenzlauf kann
sich nicht bewegen. Abnahme: Kern-Filter grün; **elf der zwölf Normtestfälle im Band nach E10**,
Testfall 11 in zwei Umschaltstunden um 3,4 W daneben (3,9 W gegen das Band ohne Druckrundung), bis der eigene Knoten der Kühldecke ihn löst
— mit der Einschränkung aus 1.8. **Dazu misst G0 die Rechenzeit neu** (F-S6): je Gebäude und Jahr,
mit Kappung an θ_kuehl, gesetztem Φ_h,max und Φ_c,max, getrennt ohne und mit Kühlung
([Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 10.5) — die Planungsgröße
des Konzepts (4.8/5.13) ist eine Prototypmessung. Und das **Abnahmekriterium (4) aus Konzept 10.4**
wird in G0 aus der wiederholten Messung mit dem Auslieferungsweg (R_si-Abzug, F_W = 0,9,
Hay-Davies, Ortszeit, a_kon = 0,09) neu bestimmt; bis dahin ist es informativ.

### 1.4 Stufe G1 — die vier neuen Kernklassen

**`GebaeudeModellEingang.cs`** baut aus `ProjektGebaeudeModel` und den Klimareihen die 8 760
Randbedingungen:

```csharp
internal static GebaeudeModellEingang Bauen(
    ProjektGebaeudeModel gebaeude,
    IReadOnlyList<SolardatenModel> solarOrtszeit,  // aus ReadOrtszeit, 8760 Zeilen
    bool[] wochenende,                             // WE[365] aus Klimakalender.Gemeinsam (F-Ü3, F-Ü8)
    double laengengrad, double breitengrad);
```

Ergebnis sind sieben Reihen zu 8 760 Werten — die Ausgabe von Schritt E der
[Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) (F-P2): `ThetaOut`,
`ThetaEq` (U·A-gewichtet), die drei fertigen Lasten `PhiRadAW`, `PhiRadIW` und `PhiConv` [W] sowie
die Sollwertreihen `ThetaSoll` und `ThetaMax` (ab KU1 tritt `ThetaKuehl` daneben) — und die
`ErsatzparameterRC`. **Die Aufteilung liegt im Eingangsbauer, nicht im Löser** (F-P2): Er kennt
die Flächen und den a_kon-Anteil ohnehin, teilt solare und innere Lasten auf die Knoten auf und
reicht dem Löser über `Stundenrand` (1.3) genau die drei Lasten, die dessen Knotenbilanzen
brauchen. Die Zwischengrößen `PhiSolarAW`, `PhiSolarIW`, `PhiSolarLuft` und die inneren Lasten
bleiben im Eingangsbauer und sind dort prüfbar, verlassen ihn aber nicht.

Hier — und nur hier — fällt die Entscheidung über Stundenanfang/Stundenmitte (1.2), hier läuft die
Erdreichtemperatur über
`ErdreichTemperatur.JahresprofilKollektor(thetaOut, 1.0, DbWerte.BODENTYP_SAND_FEUCHT)`
(`ErdreichTemperatur.cs:411`), und hier steht die Azimutzuordnung der vier Fensterrichtungen.

**Wichtig für die Lesereihenfolge:** `item.Bewohner` und `item.Z_AuswahlWohnflaeche` sind
**Ausgabefelder** (`SimulationWaermebedarf.cs:571`, `:641`, `:645`, `:652`, `:653`, Doku-Kopf
`:558-560`). Gefüllt werden sie rund um die Weiche: die **Flächen** vom modellfreien
Vorbereitungsschritt davor, die **Bewohnerzahl** und der Skalierungsfaktor nach E8 vom **Modul aus
dessen erstem Lauf** (F-Ü1, 1.1); der Eingangsbauer liest also **nach** dem Vorbereitungsschritt,
nicht davor. Die Klimareihen bekommt er ebenfalls von dort —
er liest `Tab_Klimadaten` und `Tab_Solar` nicht selbst, und er ruft **nichts** aus `Altweg/`.

**Die Kälteseite hängt an derselben Klasse** (E21, 16.09.2026). `GebaeudeModellErgebnis`
(unten) führt `KuehlbedarfKwh` bereits je Stunde; die Fassade **`SimulationKaeltebedarf`** nimmt
diese Reihe aus **demselben** Lauf entgegen, den die Wärmeseite auslöst — der Löser wird kein
zweites Mal gerufen, und der Vorbereitungsschritt wird kein zweites Mal gelesen. Ein Gebäude auf
dem Altweg liefert Kältebedarf **0** mit benanntem Hinweis (kein Altweg auf der Kälteseite).
Kanal, Senken und Erzeuger regelt das
[Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) in Stufe KU1.

**`GebaeudeModellErgebnis.cs`** — vier Reihen (`HeizlastW[8760]` in Watt, weil sie in den
vorhandenen Watt-Puffer `ziel[]` geschrieben wird und den Kern nicht verlässt; `Raumtemperatur` und
`OperativeTemperatur` in **°C**, `KuehlbedarfKwh` in **kWh** — Einheitenregel 1 trifft die
Energiereihe, Temperaturen sind keine Energiemengen) und **acht Kennzahlen**
(`JahresheizwaermeMwh`, `SpitzeKw`, `SpitzeTagesmittelKw` — Mittel über 24 **Blockstunden** —,
`Spitze95Kw`, `KuehlenergieMwh`, `StundenMitKuehlbedarf`, `MittlereRaumtemperaturHeizzeit` und
`Ueberhitzungsstunden`).

**Was die achte Kennzahl ist und was nicht zu den acht zählt** (F-S4). `Ueberhitzungsstunden` [h]
zählt die Stunden der Nutzungszeit, in denen die operative Temperatur über
`Maximaleraumtemperatur` liegt — auch mit Kühlung gegen diese Grenze, nicht gegen den
Kühlsollwert, und ohne wirksame Kühlung im freien Lauf (Entscheid E32,
[Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.37); im Mehrzonenkonzept (M5) trägt
dieselbe Größe denselben Namen. `MittlereRaumtemperaturHeizzeit` wird über die **Nutzungszeit aller
Stunden** gebildet. `JahresheizwaermeMwh` ist die Summe **nach** der Skalierung.
**`VerbrauchAltKwh` gehört nicht zu den acht**: Es ist der **unskalierte** Jahreswert des ersten
Laufs, den `IGebaeudeRechenweg.Rechnen` zusammen mit der Reihe zurückgibt (F-Ü2, 1.5), und es ist
von `JahresheizwaermeMwh` ausdrücklich getrennt zu führen. **Umgerechnet wird im Kern** (`GebaeudeBedarfCtrl` bzw.
`SimulationErgebnisCtrl`), nie in der Hülle — sonst reißt
`In_Anzeige_und_Huellen_steht_kein_Faktor_1000_auf_einer_Energiemenge`; und eine
`…Kwh`-Jahressumme, die den Kern verlässt, verstößt gegen Einheitenregel 2
([`EPOS.Kern/CLAUDE.md`](../../EPOS.Kern/CLAUDE.md)).

Die **Einheit steht im Namen** — sonst reißt der Namenswächter, sobald die Klasse in
`EinheitenWacheTests.Simulationsklassen` (`EPOS.Kern.Tests/EinheitenWacheTests.cs:201-206`)
aufgenommen ist. Sie **muss** dort aufgenommen werden: die Liste ist fest, eine neue Datei unter
`Simulation/Gebaeude/` sieht der Wächter sonst nicht (1.9). Der Eintrag lautet
`Gebaeude/GebaeudeModellErgebnis.cs` — **mit Unterordner**: `Simulationsdateien()` (`:447-455`)
setzt den Pfad aus `Simulation` + Listeneintrag zusammen und prüft ihn mit
`Assert.True(File.Exists(...))`; ein Eintrag ohne Unterordner macht den Wächter rot.

**Zwei Kernänderungen gehören in denselben Merge.** Erstens: `GebaeudeBedarfCtrl.Rechnen`
(`GebaeudeBedarfCtrl.cs:94`) bekommt einen vierten Parameter `string? modellErzwungen = null`
(null = der Spaltenwert) und `GebaeudeBedarfErgebnis` die sechs neuen Kennzahlen — nur so ist der
Vergleich zweier Wege im Bedarfsdialog (2.7) überhaupt zu holen. Der Parameter wirkt allein auf der
**gelesenen Modellinstanz**, schreibt nichts und ruft dieselbe Weiche wie der Lauf (Hausregel
„Eine Auskunft ruft den Rechenweg des Laufs", `EPOS.Kern/CLAUDE.md:186`); er lebt, solange der
Vergleich alt/neu lebt — also **bis zur Stufe GA** (E23/E26; 2.7, Löschliste Kapitel 6). Zweitens:
`GebaeudeModellEingang` bekommt einen **`Pruefmodus`**-Schalter (UTC-Reihenfolge, isotrope `Sol_*`,
θ_eq ohne Absorptionsterm); ein Test hält den Kern damit gegen die Prototypzahlen aus Konzept 5 auf
1e-6 relativ, und der Unterschied zum Auslieferungsweg wird je Referenzprojekt ausgewiesen
(Konzept 10.4 (2)). **Umgesetzt anders (Schlusswelle G1 + G2):** Der Prüfmodus ist ein
Parametersatz im Test (`EPOS.Kern.Tests/GebaeudePruefmodusTests`), kein Schalter im Eingangsbauer —
seine isotropen `Sol_*`-Spalten sind Altweg-Bezeichner, die die `Modultrennungswache` im Modul
`Gebaeude/` verbietet. Geprüft wird der Löser gegen Rechenschritte 9.1–9.5 auf die Druckstelle;
der Jahreswert gegen den Prototyp ist ohne dessen Klimaadapter nicht nachweisbar.

**Das Ergebnisobjekt je Gebäude gehört nicht der Wärmefassade allein.** Es liegt in einem **je Lauf
gehaltenen Träger neben dem Vorbereitungsergebnis**, und **beide** Fassaden lesen ihn:
`SimulationWaermebedarf` Reihe und Kennzahlen, `SimulationKaeltebedarf` die Kühlreihe — so hat die
Kälteseite einen benannten Weg an die Reihe, ohne ein Feld der Wärmefassade zu greifen (F-S4). Der
Zugriff heißt `GebaeudeErgebnisse` (`List<GebaeudeModellErgebnis>`); in `SimulationWaermebedarf`
tritt er **an die Stelle, an der `MaxP` (`:56`) steht**, das tot ist.

**`GebaeudeModellErgebnis` bleibt `internal`** — die Klasse gehört in den Kern, nicht in seine
öffentliche Fläche. Damit der Referenzlauf-Export sie lesen kann (1.8), bekommt `EPOS.Kern.csproj`
im selben Merge `<InternalsVisibleTo Include="EPOS.Referenzlauf" />` und
`<InternalsVisibleTo Include="Referenzlauf" />`; die Datei führt solche Einträge bereits für
`EPOS_Plan`, `EPOS.Kern.Tests`, `EPOS.iOS`, `EPOS.UI.Daten` und die Werkzeuge
(`EPOS.Kern/EPOS.Kern.csproj:67-93`). Ohne die beiden Einträge übersetzt `Ergebnisexport.cs` weder
im plattformfreien noch im Windows-Werkzeug (F-S4). **Umgesetzt anders (G2, Welle 6):** Die Kernseite
`GebaeudeErgebnisexport` ist öffentlich und liest den Träger intern; der Export braucht
deshalb kein `InternalsVisibleTo`.

### 1.5 Stufe G1 — die Verzweigung, der Vorlauf und die Skalierung

**Die Weiche** in der Fassade `HeizwaermeEinesGebaeudes` (E20, 16.09.2026) — eine Stelle, zwei
Module, kein Modellparameter in einer Bestandsmethode:

```
IGebaeudeRechenweg weg = (Modell(item) == DbWerte.GEBAEUDE_MODELL_TAGESBILANZ)
                       ? _altweg      // Altweg/TagesbilanzRechenweg   - Uebergang, faellt mit GA
                       : _vdi6007;    // Gebaeude/Vdi6007Rechenweg     - NULL faellt hierher (E1)
return weg.Rechnen(item, index, ziel, kalender.Gemeinsam, out double verbrauchAltKwh);
```

**Der vierte Parameter ist der gemeinsame Teil des Kalenders, nicht der ganze** (F-Ü3). Das
VDI-Modul bekommt `WE[365]`, `Stundentemperatur[8760]`, `WochentagJan1` und die Monatsgrenzen —
mehr braucht es aus dieser Quelle nicht. Den **Altweg-Teil** (`Sol_*`, `A_Temp`, `TagTyp_W/NW`)
bekommt allein `_altweg`, und zwar beim Aufbau je Lauf aus dem Vorbereitungsergebnis; so nennt
keine Datei unter `Gebaeude/` den Bezeichner `Klimakalender.Altweg`, und die Modultrennungswache
kann genau das prüfen (1.9). Mit der Stufe GA fällt der Altweg-Teil und mit ihm der Parameter.

Der Vorbereitungsschritt läuft **davor** und ist modellfrei (1.1). **Ein Aufruf liefert beides:**
die Reihe in `ziel[]` und den unskalierten Jahreswert `VerbrauchAltKwh` (F-Ü2) — deshalb läuft das
VDI-Modul auch im Verbrauchsfall nur **einmal**, während die Fassade den Altweg dort wie im Bestand
**zweimal** ruft (1.1).

**Der Fehlerweg ist für beide Module derselbe** (F-Ü6). Der Rückgabewert bleibt `bool`, und **auch
das VDI-Modul wirft keine Ausnahme**: Es legt eine Meldung der Stufe Fehler im Protokollkanal ab
und gibt `false` zurück; der Lauf endet dann an derselben Stelle wie heute (`:197`). Der Altweg
gibt `false` bei fehlender Tagesverteilung (`:590-595`). Reihen und Kennzahlen reisen über den
Ergebnisträger (1.4).

**Die Wirkung davon ist auszusprechen.** `:197` bricht mit `return` die **ganze** Bedarfsrechnung
ab, nicht nur das eine Gebäude: **Ein Altweg-Gebäude ohne Tagesverteilung beendet den gesamten
Lauf — auch für die VDI-Gebäude desselben Projekts.** Das ist das Verhalten des Bestands und bleibt
es bis GA. **Entschieden ist (U17, E27):** Das Gebäude wird künftig benannt abgelehnt, aber **erst
mit GA**, weil die Änderung Altweg-Verhalten ändert und den byte-gleichen Nachweis berührt
(Kapitel 5). **Der Tagesbilanz-Zweig bekommt dabei keine Zeile
neuen Codes:** die Bestandsmethoden wandern Zeichen für Zeichen nach `Altweg/`, und **kein
Parameter „Modellwahl" erreicht sie** — damit ist A16 gegenstandslos.

**Der Vorlauf.** Der Bestand rechnet **15 Tage** (`Tag = 350…364`, `:748-814`, Ergebnis verworfen).
Konzept 4.6 setzt für das Stundenmodell **30 Tage** — die gemessenen Zeitkonstanten der
Referenzgebäude liegen bei 7,5–24,8 h, die langsameren Normtesträume bei bis zu 264 h; der Nachweis
„Vorlauf konvergiert auf unter 0,1 K" gehört als Rechenprobe in G0. Der Bestandsvorlauf bleibt bei
15 Tagen, weil daran die Basis hängt.

**Die Skalierung (E8) bleibt in der Sache, wird aber im VDI-Modul nachgebaut.** Die Rechnung läuft
mit den Katalogdaten des Gebäudes; im Altweg steckt der Faktor im Rückgabewert von
`BhkwPlan.TaeglHeizlastWG` (`BhkwPlan.cs:435`: `return acc * gesamtflaeche / wohnflaeche;`), der
`Z_AuswahlWohnflaeche` und `Nutzflaeche` als 14. und 15. Argument bekommt
(`SimulationWaermebedarf.cs:812-813`) — **keine Nachmultiplikation des Ergebnisvektors, sondern
Teil der Tagesrechnung**. **Das VDI-Modul führt die Verhältnisrechnung selbst**, als
Nachmultiplikation in seinem eigenen Ergebnisschritt
([Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Schritt G/8.3); es ruft
dafür **nichts** aus `Altweg/`, und der Vorbereitungsschritt liefert ihm nur die **beiden Flächen**,
nicht den fertigen Faktor — sonst würde der Altweg zweimal skaliert (1.1).

Die Verbrauchs-Rückrechnung ist eine Verhältnisrechnung: Der Vorbereitungsschritt bildet
`VerbrauchNeu` je Einheit (heute `:617-636`, Ergebnis in kWh, modellfrei) und liefert
**`FlaecheAlt = Wohnflaeche_gesamt`**; die Fassade ruft **das gewählte Modul** und nimmt
`VerbrauchAltKwh` aus dessen Rückgabe (heute `HeizwaermebedarfGeb[index] / 1000`, `:650`), rechnet
`FlaecheNeu = VerbrauchNeu / VerbrauchAlt × FlaecheAlt` (`:651`) und wendet das Verhältnis an — im
VDI-Weg als Nachmultiplikation, im Altweg über den zweiten Aufruf des Bestands.

Daraus zwei harte Folgen:

1. **Im VDI-Weg ist die Skalierung eine Nachmultiplikation, im Altweg ein zweiter Lauf** (F-Ü2).
   Das VDI-Modul rechnet **einmal** und multipliziert seine Reihe anschließend mit
   `FlaecheNeu/FlaecheAlt` ([Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
   8.3) — die Physik ändert das nicht. Der **Altweg** trägt den Faktor dagegen in der Tagesrechnung
   und wird wie im Bestand ein zweites Mal gerufen; **die Zahl seiner Aufrufe darf sich um keinen
   verändern**, weil die statische Vortemperatur `BhkwPlan._prevRoomTemp` (`:51`) über beide Aufrufe
   **und alle Gebäude** trägt und der Lauf sonst nicht byte-gleich ist (Befund X 3.4). **Der Löser
   muss trotzdem zustandsfrei mehrfach hintereinander laufen dürfen** — der Bedarfsdialog holt zwei
   Auskünfte nacheinander (2.7): `Zonenmodell2K.Zuruecksetzen` ist Pflicht, die Rechenprobe „zwei
   Läufe byte-gleich" die Gegenprobe; bei rund 5 ms je Lauf (Prototypmessung Konzept 4.8/5.13,
   Planungsgröße in G0 neu zu messen, F-S6) kostet sie nichts.
2. **`VerbrauchAlt = 0` ist heute ungeschützt** (`:650` → Division durch null → `Infinity`).
   Konzept 4.8 verlangt hier eine benannte Prüfung; sie gehört in das **VDI-Modul** (im Altweg
   bleibt es bei einer Warnung, damit die Basis unberührt bleibt — Q18).

### 1.6 Stufe G1 — Gebäudespalten-Schritt M3, vollständig

> **Papiername statt Schrittnummer (F-S1, 17.09.2026):** Dieses Papier nennt den Schritt bei seinem **Papiernamen M3** — den Gebäudespalten-Schritt für `Tab_Gebaeude` und `Tab_Gebaeude_STAMM`; die Klimaspalten sind **M4** (1.7). Feste Schrittnummern stehen hier nicht mehr: **Die tatsächliche Nummer vergibt der Schritt bei seiner Beauftragung**, und der Zielstand wird dann an `SchemaStand.Zielversion` abgelesen. Am Arbeitsbaum steht er auf **100** (Stand 22.09.2026, `EPOS.Kern/Allgemein/Update/SchemaStand.cs:341`), die nächste freie Nummer ist **101**; der Klimaspalten-Schritt M4 trägt seine Nummer bereits (95, 1.7). **E19 (16.09.2026):** derselbe Schritt benennt `Wohnflaeche` in beiden Gebäudetabellen in `Nutzflaeche` um, und der Sichtneubau liefert die Spalte unter dem neuen Namen (Konzept N1.24). Die Softwarearchitektur führt ihn unter demselben Namen M3.

`SchemaMigration` liegt in der **Schale**
(`WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs`); der Kern kennt nur die Zielzahl
`SchemaStand.Zielversion` (`EPOS.Kern/Allgemein/Update/SchemaStand.cs:341`, Stand 22.09.2026). Das
Rezept steht
im Quelltext selbst (`SchemaMigration.cs:3784-3801`): Nummer ab 62 lückenlos aufsteigend; der
Schrittkörper benutzt **ausschließlich** `SqliteDdl` (`:4328`), `SqliteSpalteAnlegen` (`:4467`),
`SqliteSpalteVorhanden` (`:4444`) und `SqliteTabelleVorhanden` (`:4427`) — nie `Ddl`/`NonQuery`, die
auf `Lauf.Conn` arbeiten, und die ist im SQLite-Zweig `null`; **erst** Konstante, Methode und
`SCHRITTE_SQLITE`-Eintrag, **dann** die Zielversion.

**Eine neue Kernklasse trägt die Definitionen**, nach dem Muster
`EPOS.Kern/Allgemein/Update/ProjektEnergietraegerEindeutig.cs:65` („EINE Quelle für Migration,
Testdatenbank und Nachweis"): **`EPOS.Kern/Allgemein/Update/GebaeudeSchema.cs`** mit der Liste
`Gebaeudespalten` (**30** `SchemaSpalte`-Einträge — **fünfzehn** Spalten je Tabelle, für
`Tab_Gebaeude` und `Tab_Gebaeude_STAMM`: die **zwölf** der Tabelle unten und die drei G2-Spalten
aus 1.7; **ein** Gebäudespalten-Schritt mit **einem** Sichtneubau ist mit E27 entschieden, U5),
`SQL_VIEW_DROP` und
`SQL_VIEW_NEU`. **Der Listenname trägt keine Schrittnummer** (F-S1): Die Nummer steht allein im
Migrationsschritt der Schale.

Die Typangaben stehen in **Access**-Schreibweise und werden erst beim Anlegen übersetzt
(`StilleDb.SqliteSpaltenTyp`, `EPOS.Kern/Allgemein/Simulation/StilleDb.cs:234`):

| Spalte (beide Tabellen) | Typangabe | SQLite daraus | NULL bedeutet |
|---|---|---|---|
| `Gebaeude_Modell` | `TEXT(20)` | `TEXT` | **`VDI6007`** (E1 kehrt die Semantik von Konzept 6.1 um) |
| `Fensterflaeche_Ost` | `DOUBLE` | `REAL` | ½ `Fensterflaeche_Ost_West` |
| `Fensterflaeche_West` | `DOUBLE` | `REAL` | ½ `Fensterflaeche_Ost_West` |
| `Rahmenanteil` | `DOUBLE` | `REAL` | 0,3 |
| `Verschattungsfaktor` | `DOUBLE` | `REAL` | 0,9 |
| `Grundflaeche_Randbedingung` | `TEXT(20)` | `TEXT` | `ERDREICH` |
| `Kellertemperatur` | `DOUBLE` | `REAL` | 10 °C (N1.3) |
| `Masseanteil_Aussen` | `DOUBLE` | `REAL` | 0,3 |
| `Innenflaechenfaktor` | `DOUBLE` | `REAL` | 2,5 |
| `Heizung_Strahlungsanteil` | `DOUBLE` | `REAL` | 0,3 |
| `Heizleistung_Max` | `DOUBLE` | `REAL` (kW) | unbegrenzt |
| `Aussenbauteile_Strahlung` | **`YESNO`** | `INTEGER NOT NULL DEFAULT 0 CHECK ("…" IN (0,1))` | — (Schalter, kein Fachwert) |

Die Boolean-Übersetzung kommt **gratis** aus `StilleDb.cs:239-246` — kein handgeschriebenes
`CHECK`. Und es gibt **keinen DDL-DEFAULT auf einem Fachwert**: NULL ist die Vorgabe
(`WechselrichterSchema.cs:33-38`). `sql/schema/001_grundschema.sql`
bleibt unberührt — sie ist der eingefrorene Zielstand 61 und führt weder `PV_Modell` (Schritt 64)
noch `I_sc_max` (Schritt 70) noch `Tab_Nutzungsdauer` (Schritt 75).

**Der Sichtteil ist das Neue.** In `SchemaMigration.cs` kommt `CREATE VIEW` heute **nur in
Kommentaren** vor (`:1075`, `:1079`, `:1309`, alle zum eingefrorenen Access-Zweig); `DROP VIEW`
kommt repoweit in keiner `.cs`- und keiner `.sql`-Datei vor, und `sql/schema/002_views.sql` wird
von der **Anwendung** nicht ausgeführt — gelesen wird sie allein von `sql/tools/baue_leere_db.py:71`
und `sql/tools/Reduziere-Testdatenbank.probe.py:55`, die den eingefrorenen Stand 61 aufbauen;
danach läuft die Migration und baut die Sicht über `GebaeudeSchema` neu. **M3 ist damit der
erste Sichtneubau des SQLite-Zweigs.**

**Der Schrittkörper steht in der Reihenfolge aus Konzept N1.24: Umbenennung zuerst, dann die neuen
Spalten, zuletzt die Sicht** (F-S2). Die Sicht wird vorweg verworfen, weil sie `Wohnflaeche`
namentlich führt und SQLite kein `ALTER VIEW` kennt:

```csharp
private static bool Schritt_NN_Gebaeudespalten(Lauf l)
{
    // vorweg: die Sicht nennt die Spalte - erst weg damit (kein ALTER VIEW in SQLite)
    if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_DROP, "Sicht " + GebaeudeSchema.VIEW)) return false;

    // 1. Umbenennung zuerst (E19, Konzept N1.24); wiederholbar ueber SqliteSpalteVorhanden
    foreach (string t in GebaeudeSchema.TABELLEN)          // Tab_Gebaeude, Tab_Gebaeude_STAMM
        if (SqliteSpalteVorhanden(t, "Wohnflaeche")
            && !SqliteDdl(l, "ALTER TABLE \"" + t + "\" RENAME COLUMN \"Wohnflaeche\" "
                            + "TO \"Nutzflaeche\"", t + ": Wohnflaeche -> Nutzflaeche"))
            return false;

    // 2. dann die neuen Spalten (fuenfzehn je Tabelle, 30 Eintraege)
    foreach (SchemaSpalte s in GebaeudeSchema.Gebaeudespalten)
        if (!SqliteSpalteAnlegen(l, s.Tabelle, s.Name,
                                 StilleDb.SqliteSpaltenTyp(s.Name, s.TypDefinition))) return false;

    // 3. zuletzt die Sicht neu - aus SQL_VIEW_NEU, der einzigen Quelle der Definition
    if (!SqliteDdl(l, GebaeudeSchema.SQL_VIEW_NEU,  "Sicht " + GebaeudeSchema.VIEW)) return false;

    l.Notiz("M3: ... KEIN Rechenergebnis aendert sich durch diesen Schritt.");
    return true;
}
```

`SQL_VIEW_DROP` ist `DROP VIEW IF EXISTS "Abfrage_Projektgebaeude"` — damit ist der Schritt
wiederholbar, und die Umbenennung prüft je Tabelle, ob `Wohnflaeche` überhaupt noch steht.
**Ab M3 ist `GebaeudeSchema.SQL_VIEW_NEU` die einzige Quelle der Sichtdefinition**, und
`sql/schema/002_views.sql` bleibt der eingefrorene Stand 61 (1.10).
`SQL_VIEW_NEU` ist wörtlich die Definition aus `sql/schema/002_views.sql:90-91`,
ergänzt um die fünfzehn `Tab_Gebaeude.<Spalte>` (U5, E27) **hinter** `Tab_Gebaeude.ID`. Das ist kein
Schönheitsfehler, sondern die Bedingung dafür, dass `row[57] = ID`
(`ProjektGebaeudeCtrl.cs:99`) gültig bleibt und der Indexleser den Schritt überlebt — auch wenn er
im selben Merge auf Namen umgestellt wird.

Der `SCHRITTE_SQLITE`-Eintrag (vor der schließenden Klammer `:4136`, Muster `:4124-4135`) trägt
vier Stücke: Nummer, was der Schritt tut, **was ohne ihn schiefginge**, und die Methode. Das dritte
Stück lautet: *Die neuen Spalten erreichten den Leser nicht — die Sicht hat eine feste Spaltenliste,
und SQLite kennt kein `ALTER VIEW`. Das Gebäudemodell liefe für jedes Gebäude auf die Vorgabewerte,
ohne dass eine Eingabe des Anwenders je ankäme.*

**Die Katalogkopie gehört in denselben Merge.** `GebaeudeStammCtrl` führt **vier fest verdrahtete
Spaltenlisten**: `BuildValueParams` (`:345`, 54 Parameter in Positionsreihenfolge), `Insert`
(`:406`/`:409`), `Overwrite` (`:418`/`:426`) und `CopyFromStamm` (`:439`/`:449`, 55 Spalten). Eine
neue Spalte muss in **allen vieren** mitlaufen. Und `CopyFromStamm` bildet jeden Wert als
`r["X"] == DBNull.Value ? 0.0 : Convert.ToDouble(r["X"])` ab (`:458-470`), bei Texten `""` —
**das macht aus „NULL = Vorgabe" ein hartes 0 im Projekt**; für `Rahmenanteil`,
`Verschattungsfaktor`, `Masseanteil_Aussen`, `Innenflaechenfaktor` und `Heizung_Strahlungsanteil`
ist 0 kein neutraler Wert, und `""` ist bei `Gebaeude_Modell` nicht dasselbe wie NULL. **Die
dreizehn nullbaren der fünfzehn neuen Spalten** (elf aus der Tabelle oben, dazu
`Luftwechsel_Infiltration` und `Luftwechsel_Nutzer` aus 1.7) werden deshalb **NULL-erhaltend**
gebunden; `Aussenbauteile_Strahlung` und `Sommerlueftung` sind Schalter (`NOT NULL DEFAULT 0`,
`StilleDb.cs:239-246`) und werden als 0/1 kopiert — für sie gibt es kein NULL. Der Eingangsbauer leitet die Vorgabe aus
NULL **oder** leerem Text ab. Ein Datenbankfall „Katalogkopie hält NULL" gehört dazu (1.9).

**`Zielversion` zuletzt** — erst Konstante, Methode und Eintrag, dann die Zahl, die der Schritt bei
seiner Beauftragung bekommt. **Ohne Handgriff mitläuft** die
Auslieferungsvorlage: sie kennt keine Spaltenlisten, sondern fragt
`DataRepository.SpaltenVonTabelle` (`Werkzeuge/Auslieferungsvorlage/Projektsicht.cs:95-103`) und
`pragma_table_info` (`…/Prueflauf.cs:193-201`); `Tab_Solar_STAMM` führt keine Spalte `ReadOnly` und
bleibt vollständig (`…/Vorlagenbau.cs:113-116`).

**Ein Prüfpunkt kommt hinzu** (F-Ü9). `Gebaeude_Modell` entsteht auch in `Tab_Gebaeude_STAMM`, weil
der Katalog in das Projekt kopiert wird; **Vorgabe für die Auslieferung ist NULL** — ein
Katalogsatz mit `TAGESBILANZ` brächte den Altweg beim Anwender still zurück.
`Werkzeuge/Auslieferungsvorlage` weist deshalb im Prüfbericht **jede Zeile von
`Tab_Gebaeude_STAMM` mit gesetztem `Gebaeude_Modell` namentlich aus**. Mit der Stufe GA entfällt
der Prüfpunkt zusammen mit der Spalte (Kapitel 6).

### 1.7 Stufe G1 — die drei G2-Spalten, der Klimaspalten-Schritt M4 und `DbWerte`

**Die drei G2-Spalten verschmelzen mit M3** — mit E27 (22.09.2026,
[Konzept N1.32](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)) entschieden (U5). Sie bringen `Luftwechsel_Infiltration`
(`DOUBLE`, NULL = 0,3 1/h), `Luftwechsel_Nutzer` (`DOUBLE`, NULL = 0,4) und `Sommerlueftung`
(`YESNO`) — je beide Tabellen, also sechs Einträge. Getrennt müsste die Sicht dafür ein **zweites
Mal** neu gebaut werden. Da G1 und G2 nach E1 gemeinsam ausgeliefert werden, wären zwei
Sichtneubauten hintereinander nur zwei Gelegenheiten, die Definitionen auseinanderlaufen zu
lassen. Konzept 6.1 hat die Trennung vorgesehen, solange G2 eine eigene Auslieferung war.
**Entschieden: ein Schritt** (15 Spalten je Tabelle, ein Neuaufbau; 1.6).

**Der Klimaspalten-Schritt ist ein eigener Schritt und bereits umgesetzt** — der Papiername ist
**M4** (F-S1); Schemaschritt **95** hat ihn vorweggenommen (Anwenderentscheid vom 19.09.2026,
„alles Relevante für die Gebäudesimulation aufnehmen"; Aufträge KL-3/KL-4), Schritt **97** ergänzt
`Szenario` und `Bezugsjahr` der Klimaregion. Kapitel 4 führt M4 deshalb als erbracht und in keiner
Summe; Quellen, Bedienweg und Zuordnung stehen im
[Konzept Klimadatenquellen](Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md). Er legt **drei** `REAL`-Spalten in `Tab_Solar` und
`Tab_Solar_STAMM` an (beide `STRICT`):

| Spalte | Quelle | Einheit | NULL bedeutet |
|---|---|---|---|
| `Gegenstrahlung` | PVGIS `IR(h)`, TRY `A` | W/m² | Δθ_lw = 0 und α_str,A = 5,0 W/(m²K) (E5) — dieselbe Festlegung wie in den Rechenschritten |
| `Luftfeuchte` | PVGIS `RH`, TRY `RF` | % | nicht verfügbar |
| `Bedeckungsgrad` | TRY `N` | Achtel (0…8) | nicht verfügbar — jede PVGIS-Region, weil PVGIS ihn nicht führt; die Schätzung aus dem Diffusanteil bleibt der Rückfall |

**Der Bedeckungsgrad kommt hinzu, weil TRY ihn echt liefert.** Die Testreferenzjahre führen ihn
als Spalte `N` (Blatt 3 „Sonnenwahrscheinlichkeit"); eine gemessene Bedeckung ist etwas anderes
als eine aus dem Diffusanteil geschätzte. Die Schätzung bleibt bestehen — als das, was sie ist:
der Rückfall bei NULL, nicht der Regelweg.

**`Windgeschwindigkeit` wird nicht angelegt** (F-S3). Die Spalte hätte keinen Leser: Der äußere
Wärmeübergang bleibt bei 25 W/(m²K), und eine windabhängige Formel ist in keiner Stufe dieses
Plans festgelegt. Eine Spalte ohne Leser ist eine Zusage, die niemand einlöst; sie kommt, wenn der
Rechenweg sie braucht, mit derselben Stufe wie ihre Formel. **Und der NULL-Fall der Gegenstrahlung
ist keine Schätzung:** Ohne `IR(h)` bzw. `A` ist Δθ_lw nicht rechenbar, also gilt Δθ_lw = 0 (E5);
eine Schätzung aus dem Bedeckungsgrad wäre möglich, wird aber nicht gerechnet.

**Die Werte lagen bereits in beiden Antworten und wurden weggeworfen.** Die eingefrorene Probe
`Referenzlaeufe/Importproben/pvgis_tmy_stuttgart_72h.json` führt je Stunde `time(UTC)`, `T2m`,
`RH`, `G(h)`, `Gb(n)`, `Gd(h)`, **`IR(h)`**, `WS10m`, `WD10m`, `SP`; eine DWD-TRY-Datei führt in
jeder Datenzeile **`N`**, **`RF`** und **`A`**. `TmyHourlyData` las davon nur `RH` und `WS10m`,
und `SaveTmyData` schrieb auch diese beiden nicht; der TRY-Leser verwarf `N`, `RF` und `A`
benannt. Umgesetzt ist: eine Eigenschaft `[JsonPropertyName("IR(h)")] Gegenstrahlung` und ein
Feld `Bedeckungsgrad` in `TmyHourlyData`, `Humidity` **nullbar** (fehlt `RH`, steht NULL und nicht
0); drei Spalten und drei `DbParam` in `SaveTmyData`; drei nullbare Felder in `SolardatenModel`
samt Leseweg in `SolardatenCtrl.MapDataRowToModel`; die drei Feldindizes im `DwdTryLeser`, dessen
Verworfen-Liste damit auf `p, WR, WG, x, E, IL` schrumpft; die Projektkopie in
`KlimaregionStammCtrl.CopyRegionToProjekt`. **`SolardatenCtrl.Insert` und `WriteDataTable` sind
entfallen** — beide schrieben nur `(ID, ID_Klimaregion, Temperatur)`, beide hatten keinen Aufrufer
und wären an `Tab_Solar.ID_Projekt INTEGER NOT NULL` gescheitert (Aufräumregel der
Wurzel-[`CLAUDE.md`](../../CLAUDE.md)).

**Derselbe Schritt gibt der Klimaregion ihre Herkunft** (Anwenderauftrag 19.09.2026, KL-4):
`Tab_Klimaregion` und `Tab_Klimaregion_STAMM` bekommen `Quelle` (`TEXT`; die sprachneutralen
Schlüssel `PVGIS`, `TRY_DATEI`, `TRY_REGIONAL` in `DbWerte.KLIMA_QUELLE_*`) und `Importdatum`
(`TEXT`, ISO `yyyy-MM-dd`). **NULL heißt bei beiden Altbestand** — eine Region, die vor dem
Schritt angelegt wurde, sagt nicht, woher sie kommt, und wird nicht nachdatiert. Der Freitext
`Details` bleibt unverändert daneben stehen: Er ist der Herkunftsvermerk für den Leser, die zwei
Spalten sind die Angabe für das Programm. Beide wandern mit der Projektkopie.

Vier Gründe für den eigenen Schritt: **andere Wirkung** (die Klimaspalten bleiben in allen
Bestandsregionen NULL, bis der Anwender die Region neu importiert — eine Zusage, die im
Schrittbericht eigens steht), **anderer Mitläufercode**, **anderes Risiko** (M3 baut eine
Sicht neu; die Klimaspalten daranzubinden koppelt ihren Rücklauf an dieses Risiko ohne Gegenwert)
und **keine technische Notwendigkeit dagegen** (`ALTER TABLE … ADD COLUMN` ist in SQLite eine reine
Metadatenänderung; die rund 280 000 Zeilen von `Tab_Solar_STAMM` werden nicht angefasst).

**`DbWerte`** bekommt fünf Persistenzwerte nach dem Muster `PV_MODELL_*`
(`EPOS.Kern/Allgemein/DbWerte.cs:2158-2199`, Konstanten `:2173`/`:2180`): ein Abschnittskopf mit
Konzeptverweis, dann je Wert eine `public const string` mit XML-Doku, die sagt, was NULL bedeutet.

```csharp
public const string GEBAEUDE_MODELL_TAGESBILANZ = "TAGESBILANZ";
public const string GEBAEUDE_MODELL_VDI6007     = "VDI6007";
public const string GRUND_ERDREICH              = "ERDREICH";
public const string GRUND_KELLER                = "KELLER";
public const string GRUND_AUSSENLUFT            = "AUSSENLUFT";
```

**Die XML-Doku muss die durch E1 umgedrehte Semantik tragen: NULL = `VDI6007`.** Konzept 6.1
(Rev. 1) sagt noch das Gegenteil; Nachtrag N1.1 hebt es auf. Persistenzwerte sind eingefroren und
ASCII — wie `KANAL_HEIZUNG = "Heizung"` (`:1260`) und `KANAL_PROZESS = "Prozesswaerme"` (`:1271`,
bewusst ohne Umlaut, weil in SQL verglichen).

### 1.8 Stufe G1 — Ergebnisreihen, Referenzlauf-Export und die Einfrierschritte

**Die Kennzahlen gehen nicht in die Datenbank.** `Tab_ErgebnisEnergiebedarf`
(`sql/schema/001_grundschema.sql:850-862`) trägt elf Spalten und ist **einzeilig je Lauf**; die
Gebäudekennzahlen sind je Gebäude. Der Weg ist derselbe wie bei den Emissionsgrößen („Weg A",
`Referenzlauf/Ergebnisexport.cs:284-287`): **Skalare in `aggregate.csv`**, keine Spalte. Die
„einmalige, tolerante Migration" in `ErgebnisCtrl` (`EPOS.Kern/Controller/ErgebnisCtrl.cs:1117`)
ist ausdrücklich **kein** Muster zum Nachbauen ([`ADR-001_Schema-Ausrollung.md`](ADR-001_Schema-Ausrollung.md)).

**Der Export.** `Ergebnisexport.ProjektAusfuehren` (`Referenzlauf/Ergebnisexport.cs:34`) schreibt
heute unter anderem `waermebedarf.csv` (`:58`), `waermebedarf_gebaeude.csv` (`:59`) und
`stundentemperatur.csv` (`:64`); `Vektor` (`:573-592`) legt je Datei die Summe in `summen` ab.
**Achtung, eine Einheiteninkonsistenz im Bestand:** `wb.Waermebedarf_Gebaeude` steht in **Watt**
(die Addition `SimulationWaermebedarf.cs:208` geschieht vor `WattToKw:222`), während
`waermebedarf.csv` kWh führt. Das ist eingefroren und wird nicht angefasst.

Die drei neuen Reihen kommen unmittelbar nach `:64`, in einem Block, der **nur** läuft, wenn das
Projekt mindestens ein VDI-6007-Gebäude führt — Muster ist der Erdreichblock, der ohne Erdreich
**keinen einzigen Eintrag** erzeugt (`:247-253`, Begründung `:249-251`):

```csharp
// nur fuer VDI-6007-Gebaeude, sonst bleibt der Bestandsordner byte-gleich
dateien += Vektor(zielOrdner, "raumtemperatur_"       + n + ".csv", geb.Raumtemperatur,      summen);
dateien += Vektor(zielOrdner, "operative_temperatur_" + n + ".csv", geb.OperativeTemperatur, summen);
dateien += Vektor(zielOrdner, "kuehlbedarf_"          + n + ".csv", geb.KuehlbedarfKwh,      summen);
```

**`kuehlbedarf_<n>.csv` führt kWh** (Einheitenregel 1 des Kerns: Energiezeitreihen führen kWh);
**`raumtemperatur_<n>.csv` und `operative_temperatur_<n>.csv` führen °C** — Temperaturen sind
keine Energiemengen (F-S4; [Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
8.1). Die Watt-Form der Heizlast bleibt intern. Nur die eingefrorenen Bestandsdateien behalten
ihre Einheit — siehe die Inkonsistenz oben.

Der Index `n` ist der Schleifenindex des Gebäudes — Vorbild `"quellspeicher_" + kennung + "_soc.csv"`
(`:99`). Die Skalare folgen dem Muster `Erdreich[i].…` (`:256-271`) mit Präfix `Geb[i].`:
`ID_Gebaeude`, `Modell`, `JahresheizwaermeMwh`, `SpitzeKw`, `SpitzeTagesmittelKw`, `Spitze95Kw`,
`KuehlenergieMwh`, `StundenMitKuehlbedarf`, `MittlereRaumtemperaturHeizzeit` und
`Ueberhitzungsstunden` — die acht Kennzahlen aus 1.4 (F-S4), Einheit im Namen, Jahressummen in
MWh (Einheitenregel 2).

**Warum der bedingte Block Bedingung ist und nicht Bequemlichkeit.** `Vergleich`
(`Referenzlauf/Vergleich.cs:41`) kennt nur einen **Schlüssel**-Ausschluss innerhalb einer Datei
(`--ohne`, `_ausgenommen` `:61-62`, gefüllt `:76-79`). Eine **Datei**, die nur im neuen Lauf liegt,
bekommt `Schwere = double.MaxValue` (`:183-190`) — also FAIL, ohne Schalter dagegen. Der in N1.1
verlangte Rückweg-Regressionstest funktioniert deshalb **nur**, wenn die drei Reihen für
Tagesbilanz-Gebäude **gar nicht entstehen** — nicht „mit Nullen gefüllt"; Konzept 10.4 ist hier zu
schärfen (1.10). `EPOS.Referenzlauf` und das Windows-Werkzeug teilen sich dabei **eine** Fassung
von `Ergebnisexport.cs` und `Vergleich.cs` (`EPOS.Referenzlauf.csproj:42-51`, `<Compile Include…
Link…>`): eine Änderung wirkt auf beiden Wegen.

**Die Einfrierschritte.** In diesem Papier frieren **GB** und **G1 + G2** neu ein; die weiteren
Anlässe der Kette stehen in den Schwesterpapieren — **G6d** im Mehrzonenmodell, **KU2** im
Kühlkonzept, **AK1–AK3** in der Anlagenkopplung —, und **GA — Altweg ablösen** schließt sie ab
(E26; fällig nach dem Ablösekriterium Q24, E27). Dazu gilt als eigener, begründeter Anlass eine **ergebniswirksame
Fehlerbehebung im Altweg nach der Verschiebung** (Softwarearchitektur 2.8, Systementwurf 8.3).

- **GB** (Stufe vor G1, E4): `_prevRoomTemp` wird von `static` (`EPOS.Kern/Allgemein/BhkwPlan.cs:51`,
  gesetzt `:433`, zurückgesetzt nur über `ResetState()` `:54`) auf Instanzzustand umgestellt — bis dahin
  startete Gebäude 2 seinen Vorlauf mit der Raumtemperatur, die Gebäude 1 am 31.12. hinterlassen hat
  (gemessen in GB: ohne Wirkung auf ein Ergebnis, weil Tag 1 des Jahreslaufs die Vortemperatur
  auf den Nachtsollwert setzt und der Jahreslauf die Vorlaufwerte überschreibt — 1039 bleibt
  byte-gleich). Dazu die Warnungen statt stiller
  Fehlgriffe in der Ferienmaske (`SimulationWaermebedarf.cs:689-733`: Zeitraum 1 läuft ohne `-1`
  (`:703`, `:707`), die Zeiträume 2–4 mit `-1` (`:714`, `:721`, `:728`); Zeitraum 1 ist zudem als
  Jahreswechsel gelesen und senkt bei `Ferienbeginn_1 < Ferienende_1` den ganzen Rest des Jahres
  ab) **und die nicht nachgeführte Ferienabsenkung der Jahresschleife** (`:845-850` bestimmen nur
  `WE_Absenkung` neu; `Ferien_Absenkung` behält den Wert des letzten Vorlauftags aus `:781` und geht
  so in alle 365 Aufrufe von `TaeglHeizlastWG` ein, `:872` — die Ferienabsenkung wirkt damit
  ganzjährig oder gar nicht; Befund X 3.4 Punkt 8), die Korrektur `Bauweise` von Gebäude 10576 auf
  15 200 Wh/K in
  `Referenzlaeufe/Kenndaten_Test.sqlite` und die **vierte Einfrierregel „gesäte Gebäudedaten"**
  (`Tab_Gebaeude(_STAMM)`: `Bauweise`, U-Werte, Flächen, Sollwerte, `Luftwechselrate`,
  `Fensterdurchlassgrad` sowie ab G1 `Gebaeude_Modell`, `Fensterflaeche_Ost/West`, `Rahmenanteil`,
  `Verschattungsfaktor`, `Grundflaeche_Randbedingung`, `Kellertemperatur`, `Masseanteil_Aussen`,
  `Innenflaechenfaktor`, `Heizung_Strahlungsanteil`, `Heizleistung_Max`,
  `Aussenbauteile_Strahlung` und die drei G2-Spalten) in
  [`Referenzlaeufe/LIESMICH.md`](../../Referenzlaeufe/LIESMICH.md) (nach `:100-121`) und im
  Abschnitt „Regressionsnetz" der [`CLAUDE.md`](../../CLAUDE.md).
- **G1 + G2:** alle dreizehn Referenzprojekte rechnen stündlich (Konzept 5.5 erwartete +7 bis
  +33 % Jahresheizwärme ohne den Abzug R_si/A; gemessen beim Einfrieren +25 bis +46 % je Gebäude,
  Basis `2026-09-23_R12_Gebaeudemodell`), dazu drei neue CSV je VDI-Gebäude. Basis vollständig neu — und **ein
  Referenzprojekt mit `Gebaeude_Modell = TAGESBILANZ` wird darin mit eingefroren** (F-Ü7). Das
  ist mit **E27** entschieden (A15, 22.09.2026,
  [Konzept N1.32](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)): **genau ein** solches
  Referenzprojekt, bis GA in der jeweils aktuellen Basis; die Arbeitskopie gegen die GB-Basis nur
  bis zum Merge G1 + G2; der Rückweg-Test rechnet nur dieses Projekt und endet mit GA.
  Die GB-Basis ist bis zu diesem Merge die aktuelle Basis
  und wandert danach mit ihrem Protokoll nach `Dokumentation/ueberholt/Referenzbasen/`, wie jede
  Basis vor ihr; **einen zweiten Basisordner gibt es nicht**
  ([Systementwurf](Systementwurf_Gebaeudesimulation_EPOS-Plan.md) 8.4).
- **G6d** (Mehrzonenmodell, [`Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`](Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 9):
  das Zonenprojekt der Testdatenbank und die Regel „gesäte Zonendaten"; außerhalb dieses Papiers,
  hier nur genannt, damit die Einfrierkette vollständig ist.

**GA — Altweg ablösen ist ein eigener Einfrierschritt** (E26; fällig nach dem Ablösekriterium
Q24, E27). Bis dahin
stehen das Referenzprojekt auf dem Altweg (A15) und der Rückweg-Test in **jeder** Basis, und die
vier Altweg-Spalten bleiben im Schema ([`ADR-006`](ADR-006_Trennung_Altweg_VDI6007.md)). Mit GA
geht das Referenzprojekt auf VDI 6007 über, der Rückweg-Test wird eingestellt, die Altweg-Spalten
fallen, und die Basis wird neu eingefroren — Umfang: Löschliste in Kapitel 6.

**Der erste Schritt von G1 friert nichts ein: er muss byte-gleich sein.** Die Verschiebung des
Altwegs nach `Simulation/Altweg/` samt Fassade und Vorbereitungsschritt ist ergebnisneutral und
wird **vor** der Anbindung des VDI-Wegs mit einem **byte-gleichen** Referenzlauf gegen die GB-Basis
abgenommen (E20; Kapitel 4). Ein Unterschied an dieser Stelle ist ein Fehler der Verschiebung, kein
Einfrieranlass — und er wäre nach der Anbindung nicht mehr von der Modellwirkung zu trennen.

### 1.9 Stufe G1 — Tests, Wächter und die nicht ausgelieferten Normprüfdaten

**Reine Rechenproben** (Vorbild `EPOS.Kern.Tests/PvKoeffizientenTests.cs:15-36`, `:43-44`: keine
Sammlung, keine Datenbank, ein privater Erbauer, `Assert.Equal(erwartet, ist, 9)`): Grenzfälle,
Skalierung, Determinismus, Eigenwerte reell und negativ für alle Klassenparameter der
Testdatenbank, Lastbestimmung hält den Sollwert auf 1e-9 K, Vorlauf konvergiert,
Plausibilitätsprüfungen werfen benannte Fehler.

**Datenbankfälle** mit `[Collection("Testdatenbank")]` und `IClassFixture<TestDatenbank>`
(`EPOS.Kern.Tests/GebaeudeBedarfCtrlTests.cs:30-35`; die Sammlung ist die **eine** serielle,
definiert in `EPOS.Kern.Tests/TestDatenbank.cs:29`, Wächter `DiensteSammlungTests`; Kulturpinnung
über `Kulturvorrichtung.cs:29`, alle vier Werte auf `de-DE`, `:38-50`): der Bedarfsdialog liefert
für ein umgestelltes Gebäude dieselbe Reihe wie der Lauf; der Gebäudespalten-Schritt M3 auf der
Arbeitskopie (Muster `EPOS.Kern.Tests/Migration74Tests.cs:34-40` — Teil 1 prüft Texte ohne
Datenbank, Teil 2 den Umbau); der Namensleser hält alle 58 Bestandsfelder; **die Katalogkopie hält
NULL**; die Ost/West-Summe ist konsistent; der Mischfall Gebäude plus Ganglinie (Projekt 1041)
summiert richtig. Dazu **der Rückweg-Test** (E1/N1.1) in der Fassung des
[Systementwurfs](Systementwurf_Gebaeudesimulation_EPOS-Plan.md) 8.4 (F-Ü7): **Bis zum Merge
G1 + G2** läuft er auf einer gitignorierten Arbeitskopie mit `Gebaeude_Modell = 'TAGESBILANZ'`
gegen die GB-Basis, getragen von einem eigenen Modus des Referenzlaufs; **ab dem Einfrieren von
G1 + G2** ist sein Gegenstand allein das **eine Referenzprojekt auf dem Altweg in der jeweils
aktuellen Basis** — nicht jedes Altweg-Gebäude —, und er **endet mit der Stufe GA**. Bis dahin ist
er der laufende Nachweis, dass der Bestandsweg unverändert rechnet.

**`TestDatenbank` zieht das Schema selbst nach** (`:197-241`, Spalten über `SpalteSicherstellen`,
Tabellen über die Kern-Schemaklasse `:224-239`, zuletzt
`UPDATE Tab_Applikation SET SchemaVersion = …` `:241`). **Der Gebäudespalten-Schritt M3 muss hier
eingetragen werden** — der Klimaspalten-Schritt M4 steht dort bereits als Schritt 95
(`TestDatenbank.cs:350-355`, Stand 22.09.2026) —, sonst laufen die Datenbankfälle auf einer Kopie
ohne die neuen Spalten; die Sicht wird über
`DataRepository.ExecuteNonQuery(GebaeudeSchema.SQL_VIEW_DROP)` **und danach** `…SQL_VIEW_NEU`
nachgezogen — dieselbe Zweierfolge wie in M3, nach dem Muster der
`NutzungsdauerSchema.Anweisungen`-Schleife (`:224-225`). Ein einzelnes `CREATE VIEW` scheitert auf
der Kopie, weil die Sicht dort besteht, und der Fehler verschwindet im `catch`
(`TestDatenbank.cs:243-246`, `Console.WriteLine`).

**Die vier vorhandenen Wächter und was sie sehen:**

| Wächter | Umfang | Wirkung auf `Simulation/Gebaeude/` |
|---|---|---|
| `DoubleWacheTests` (`EPOS.Kern.Tests/DoubleWacheTests.cs:39`) | alle `.cs` unter `Simulation/` **mit Unterordnern** (`SearchOption.AllDirectories`, `:206`) plus `BhkwPlan.cs` (`:203`) | **greift.** Kein `float`, kein `Convert.ToSingle`, kein `MathF.`, kein `f`-Suffix; Ausnahmeliste leer (`:63-64`) und bleibt es. `System.Numerics.Complex` (G3) ist `double`-basiert |
| `EinheitenWacheTests` | Wächter 2 prüft nur die **namentlich gelisteten** Simulationsklassen (`:201-206`); `Simulationsdateien()` (`:447-455`) baut daraus `Simulation` + Eintrag und prüft `File.Exists` mit `Assert`, die Gegenprobe zählt Liste gegen Dateien (`:361`) | **greift nur halb.** Einzutragen ist `Gebaeude/GebaeudeModellErgebnis.cs` — **mit Unterordner**, sonst ist der Wächter rot — im selben Merge, in dem die Klasse entsteht; ohne Eintrag bleibt eine stille Lücke |
| `RechenrandTests` (`EPOS.Kern.Tests/RechenrandTests.cs:29`) | prüft `Rechenrand` selbst (`EPOS.Kern/Allgemein/Simulation/Rechenrand.cs:78`, `:95`) | **greift nicht automatisch**, aber die Regel gilt: Kappung an `Maximaleraumtemperatur`, Grenze `Heizleistung_Max` und die Bisektion des Umschaltzeitpunkts sind Betriebsschwellen und nehmen `SchwelleErreicht`, nicht `>=` |
| `ParallelitaetWacheTests` | vier plattformfreie Projekte, sieben Muster (`:75-84`) | **greift.** Der Löser ist einfädig; ein `Parallel.For` über die Gebäude widerspräche dem Determinismusversprechen. Nicht tun |

**Ein fünfter Wächter kommt hinzu: die `Modultrennungswache`** (E20, 16.09.2026; F-Ü4). Ein Name
in allen Papieren — Klasse `Modultrennungswache`, Test
`EPOS.Kern.Tests/ModultrennungswacheTests.cs` —, Bauform ein reiner Textwächter nach dem Muster von
`DoubleWacheTests.Der_Waechter_sieht_den_Bestand_und_jede_Ausnahme_existiert` (`:137`): Er liest
die `.cs`-Dateien beider Ordner und hält vier Sätze fest:

1. **Keine Datei unter `EPOS.Kern/Allgemein/Simulation/Gebaeude/` nennt einen Bezeichner aus
   `Altweg/` oder eine Altweg-Datenquelle** — weder den Namensraum noch eine der verschobenen
   Methoden (`Berechnung_Gebaeude_Tageswerte`, `DBTagesVeteilung`, `StdWerte`, `SolareGewinneC`,
   `SpezWaermeverlusteC`, `TaeglHeizlastWG`) noch `Sol_N`/`Sol_O`/`Sol_S`/`Sol_W`, `A_Temp`,
   `TagTyp_W`/`TagTyp_NW`, `Tab_DBTagV`, `Abfrage_Tagverteilung`, `Fensterflaeche_Ost_West`, `Typ`
   als Verteilungsschlüssel oder `Klimakalender.Altweg` (1.5).
2. **Keine Datei unter `Altweg/` nennt einen Bezeichner aus `Gebaeude/`** — der Altweg bekommt
   keine neue Funktion (E20).
3. **Die Kältefassade `SimulationKaeltebedarf` nennt `Altweg/` nicht** (E21).
4. **Ausbauprobe, statischer Teil:** Außer der Weiche in `SimulationWaermebedarf`, dem
   Rückweg-Test und der Wache selbst nennt **keine** Datei des Kerns `Altweg/`.

Die Gegenprobe hält Liste gegen Dateien: **Der Ordner `Altweg/` existiert und ist nicht leer**,
sonst prüft der Wächter nichts und ist still grün (dieselbe Gegenprobe, die `DoubleWacheTests` für
seine Ausnahmeliste führt).

**Die Wache lebt bis zur Stufe GA** (E26; fällig nach dem Ablösekriterium Q24, E27) — mit dem Ordner `Altweg/`, den
sie bewacht; Satz 4 ist die Vorstufe des **Gates von GA**, der vollständigen **Ausbauprobe**: Ein
Bau mit umbenanntem Ordner `Altweg/` übersetzt, nachdem die Weiche entfernt ist, und der
Referenzlauf **aller** Projekte ohne Altweg-Gebäude bleibt byte-gleich (Kapitel 6). Dazu
`DokumentationLinkWacheTests` (`:133` Verweise, `:170` „Der_Index_nennt_jedes_Papier") und
`HuellenwegTests` beim Hüllenumzug (2.8).

**Die Normprüfdaten dürfen nicht ausgeliefert werden** (Konzept N1.2: das Ausliefern der Normzahlen
in Testdateien ist eine Vervielfältigung). **Ein Muster für lokal beizustellende, gitignorierte
Prüfdaten gibt es im Repositorium heute nicht** — `.gitignore` kennt `dev/` (`:383`), `.work/`
(`:376`) und `Referenzlaeufe/Arbeitskopie/` (`:377`), aber keinen Ort für Prüfdaten;
`Referenzlaeufe/Importproben/` ist das Gegenteil (eingefroren, versioniert,
`Referenzlaeufe/LIESMICH.md:127-130`), und LFS ist keine Zugriffsbeschränkung. Die Bauform ist
`TestDatenbank` (`EPOS.Kern.Tests/TestDatenbank.cs:98-152`, Suche aufwärts vom Laufordner
`Quelle()` `:280-286`, Kennzeichen `Vorhanden`, jeder Fall beginnt mit
`if (!_db.Vorhanden) return;`). **Mit E27 entschieden (U8, 22.09.2026):** Die Normzahlen liegen
lokal und gitignoriert; gebaut wird:

eine Vorrichtung `Normzahlen` mit `Vorhanden`, die
`Referenzlaeufe/Normzahlen/vdi6007_blatt1_anhang_a1.csv` sucht (ohne Datei **schweigt** jeder Fall);
ein `.gitignore`-Eintrag `Referenzlaeufe/Normzahlen/` mit begründendem Kommentar; ein
**versioniertes** `Referenzlaeufe/Normzahlen/LIESMICH.md`, das sagt, woher die Zahlen kommen
(VDI 6007 Blatt 1:2015-06, Tabellen A1.3…A12.3, Seiten 41–63) und wie sie einzutragen sind —
**ohne eine einzige Zahl**; und ein **versionierter Gegenwächter** für das, was ohne die Zahlen
prüfbar ist (Prüfregel Band ± 0,15 K / ± 1,5 W nach E10, Vorzeichenkonvention, und dass die Vorrichtung
wirklich sucht — Vorbild
`DoubleWacheTests.Der_Waechter_sieht_den_Bestand_und_jede_Ausnahme_existiert`, `:137`).

**Die Folge ist auszusprechen: der Nachweis der zwölf Normtestfälle ist ein lokaler Nachweis, kein
CI-Nachweis.** In `kern.yml` laufen die Normfälle schweigend durch. Das ist eine bewusste Lücke im
Gate und gehört ins Protokoll; der Auszug des Laufs (Abweichung je Fall, ohne Absolutwerte) gehört
als Tabelle in die Dokumentation (U8, Kapitel 5).

### 1.10 Was dieses Papier am Konzept korrigiert

| Stelle im Konzept | Korrektur | Beleg |
|---|---|---|
| 4.1 / 11, Klassenname | `Zonenmodell2K` statt `Zonenmodell7R2C` — die Richtlinie sagt „2-K-Modell" | Konzept N1.3 |
| 4.4, Wochenendkalender | der Ortszeit-Kalender gilt (F-Ü8): Der Vorbereitungsschritt bildet `WE[365]` aus dem Wochentag des 1. Januar des Referenzjahres; der dort verlangte Test ist eine **Probe** gegen `Tab_Klimadaten.WE` derselben Klimaregion (`SimulationWaermebedarf.cs:524`), deren Abweichung ein Befund ist, kein Rechenfehler — U7 mit E27 (22.09.2026) so entschieden (Kapitel 5) | [Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) E8; `KlimaImportAblauf.cs:354`, `SolardatenCtrl.cs:222` |
| 4.4, Erdreich | der Kusuda-Ansatz liegt fertig im Kern; die Zahlen 0,68/22 Tage fallen aus dem Vorgabeboden. Kein neuer Code | `ErdreichTemperatur.cs:411`, `:47`/`:188` |
| 4.6, Vorlauf | der Bestandsvorlauf sind **15** Tage — die Zahl ist belegt | `SimulationWaermebedarf.cs:748` |
| 6.1, NULL-Semantik | `Gebaeude_Modell` NULL = **`VDI6007`**; die Tabelle in 6.1 sagt noch das Gegenteil | Konzept N1.1 |
| 6.1, G2-Spalten | mit dem Gebäudespalten-Schritt M3 verschmelzen, weil G1 und G2 gemeinsam ausgeliefert werden; feste Schrittnummern nennt kein Papier mehr (F-S1) | 1.7, U5 (mit E27 entschieden) |
| 6.2, Sichtdefinition | „das Schemaskript und der Migrationsschritt führen dieselbe Definition" trifft nicht zu: `sql/schema/002_views.sql` wird zur Laufzeit **nicht ausgeführt** und ist eingefroren. Die Definition gehört in `GebaeudeSchema` | von der Anwendung nicht ausgeführt; gelesen allein von `sql/tools/baue_leere_db.py:71` und `sql/tools/Reduziere-Testdatenbank.probe.py:55`; `WechselrichterSchema.cs:33-38` |
| 8.1, Randbedingung | die Randbedingung der Grundfläche steht als **Spalte der U·A-Tabelle**, nicht als zwölftes Feld der Modellgruppe | E2/N1.6, Befund M 5.3 |
| 10.4, neue Reihen | sie dürfen für Tagesbilanz-Gebäude **gar nicht entstehen** — `Vergleich` kennt keinen Dateiausschluss | `Vergleich.cs:183-190` |
| 10.5, Wächter | `EinheitenWacheTests` greift auf einer neuen Datei nur halb; die Liste `Simulationsklassen` ist zu erweitern — mit Unterordner | `EinheitenWacheTests.cs:201-206`, `:447-455` |
| N1.6, Gruppenname | drei Gruppen statt einer „Hülle und Rechenmodell": die U·A-Tabelle trägt die Hülle, die Summen stehen als eigene Gruppe, „Rechenmodell" bleibt für die sieben Parameter | 2.3, 2.5 |
| N1.25, Vorbereitungsschritt | „modellfrei" gilt für Klimakalender, `VerbrauchNeu`, `FlaecheAlt`, `Flaeche_Nutzer`, `Einheit` und `Jahresnutzungsgrad` — **nicht für Bewohnerzahl, Skalierungsfaktor und die Rückrechnung**: `VerbrauchAltKwh` kommt aus dem gewählten Modul (VDI-Modul: ein Lauf, Nachmultiplikation; Altweg: zweiter Aufruf wie im Bestand); der Vertrag steht an einer Stelle, Softwarearchitektur 1.3 (F-Ü1, F-Ü2). E20 ist damit erfüllt (kein Weg ruft den anderen), die Rechenfolge des Bestands bleibt | 1.1, 1.5; Befund X 2.4 |
| 6.1, Spaltenliste | die Tabelle nennt elf Spalten und **lässt `Kellertemperatur` aus**, obwohl Rechenschritte 1.1, 1.8 und 2.4 sie führen | [Rechenschritte](Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 1.1; Befund X 5.4 |
| 6.4, Leser von `Gebaeude_Modell` | nicht mehr „in `HeizwaermeEinesGebaeudes`", sondern **in der Weiche der Fassade** und in der Anzeige; der Eingangsbauer liest die Spalte nicht | E20, 1.1 |

---

## 2. Der Gebäudedialog

### 2.1 Der Ist-Stand in fünf Sätzen

Der „Gebäudedialog" ist **fünf Masken**, nicht eine:

| Datei | Zeilen | Rolle |
|---|---|---|
| `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor` | 828 | **Wirt**: Zweispaltenauswahl (`:62-149`), Detailblock (`:154-195`), vier Überlagerungen (`:202-248`), `SpeichernLeiste` (`:199`) — **kein einziges Fachfeld** |
| `EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDialog.razor` | 983 | **der Editor**, den E2 trifft: ein Katalogsatz auf zwei Reitern |
| `EPOS.UI/Dialoge/Bedarf/GebaeudeBedarfDialog.razor` | 324 | **Auskunft**: drei Kennzahlen (`:60-84`), Ganglinie über `ChartBild` (`:91-93`), Monatsübersicht (`:96-117`) |
| `EPOS.UI/Dialoge/Bedarf/GebaeudeWohnflaecheDialog.razor` | 284 | **Zuordnung**: Bedarfsart, Verbrauch/Wohnfläche, Jahresnutzungsgrad — der Ort der Skalierung (E8) |
| `EPOS.UI/Dialoge/Bedarf/GebaeudetypDialog.razor` | 432 | Tagesverteilungen eines Gebäudetyps |

**Konzept 8.1 nennt `GebaeudeDialog.razor` und `GebaeudeKatalogDialog.razor` gleichrangig — das
trifft nicht zu.** Die neuen Gruppen entstehen **einmal**, im Editor; der Wirt bekommt nur eine
Spalte und zwei leise Kennzahlen (2.7).

**Alle Größen der U·A-Tabelle sind bereits da**, nur verstreut: Reiter 1 führt „Kenngrößen"
(`:90-143`), „Flächen [m²]" (`:145-177`) und „U-Werte [W/m²K]" (`:179-203`); Reiter 2 führt
Raumtemperaturen (`:212-236`), „Wärmebrückenverlustkoeffizienten" (`:238-253`), „Abmessung Anschluß"
(`:255-272`), Ferien (`:274-314`) und „Sonstiges" mit der Luftwechselrate (`:316-335`). Der Umbau
ist damit eine **Umordnung**, keine Neuentwicklung — zwei Felder kommen hinzu (Fenster Ost, Fenster
West), sieben Modellparameter und ein Schalter.

Der Speicherweg ist dreistufig: der Dialog ruft den Delegaten
`Speichern(GebaeudeKatalogDaten, istNeu, bezeichner)` (`GebaeudeKatalogDialog.razor:398-399`,
gerufen `:918-935`), die Hülle
`WindowsFormsApplication1/Views/Gebäude/GebaeudeKatalogHuelle.cs` führt ihn in `Schreiben(...)`
(`:318-337`) aus — die ReadOnly-Sperre sitzt **in der Hülle** (`:313-317`, `:323-326`) — und ruft
`GebaeudeStammCtrl.Insert` bzw. `Overwrite` (`:334`); die Feldabbildung steht in `NachModell`
(`:416-505`). **`EPOS.UI.Daten` führt für Gebäude heute nichts**: der Ordner `Bedarf/` enthält
allein `BedarfErgebnisHuelle.cs`, alle drei Gebäudehüllen liegen in der Windows-Schale.

### 2.2 Sechs Befunde am Bestand, die der Umbau mit erledigt

| # | Befund | Stelle | Folge |
|---|---|---|---|
| M-1 | Der Editor ist der **einzige** Dialog des Hauses ohne `SpeichernLeiste`: drei eigene Knöpfe „Überschreiben", „Speichern unter"/„Speichern", „Beenden" (`:360-369`) und zusätzlich „Werte übernehmen" am Fuß des zweiten Reiters (`:337-340`); geschrieben wird **sofort** (`:894-935`), „Beenden" verwirft nichts und meldet immer `true` (`:950-956`) | gegen `EPOS.UI/CLAUDE.md:48-56` | die zehn neuen Prüfregeln aus Konzept 4.8 hingen sonst an **drei** Schreibstellen |
| M-2 | Reiter 2 führt einen **zweiten, eigenen Stand** (`:549-558`), der erst mit „Übernehmen" in den Satz wandert (`:788-829`) | dito | U und A stehen heute auf Reiter 1, ψ und L auf Reiter 2 — die U·A-Summe wäre bis zum „Übernehmen" falsch |
| M-3 | **Kein einziges Zahlenfeld des Editors hat `Min`/`Max`** (`:123-199`, `:218-232`); keine Prüfung auf `0 < g ≤ 1`, keine auf U-Werte | — | Konzept 4.8 verlangt Bereiche für g, U, Bauweise, Ferientage |
| M-4 | ψ- und L-Gruppe tragen **dieselben drei Beschriftungen** (`:244-249` gegen `:261-268`, beide `LabelWbvk*`) in **verschiedener Reihenfolge** (Fenster/Keller/Dach gegen Fenster/Dach/Keller) | — | E2 führt ψ und L in **einer** Zeile zusammen und räumt beides auf |
| M-5 | Der englische Wert `GEBK_GRP_UWERTE` trägt zwei unsichtbare Steuerzeichen | `EPOS.Kern/MyResource/Resource.en-US.resx:13294` | beim Anfassen der Gruppe bereinigen |
| M-6 | `Fensterflaeche_Ost` heißt im Modell Ost, meint aber Ost **und** West | `GebaeudeKatalogHuelle.cs:364`, `:443`, `:453` | die Umbenennung auf `Fensterflaeche_OstWest` gehört zu G1, Merge M2 (1.6) |

**Die Validierung heute:** `PflichtzahlenStehen()` (`:859-892`) prüft 17 Zahlen des ersten Reiters
und meldet den **ersten** fehlenden mit seinem *Feldnamen* (`:963-979`, Sprung auf Reiter 1 `:887`);
`BeiUebernehmen()` (`:788-829`) prüft auf Reiter 2 allein die vier Ferienregeln über
`Ferienzeit.Pruefen` (`:798-799`) und leitet vier Werte ab (`:804-826`). `BauweiseNachfuehren()`
(`:713-715`) bestimmt die `Bauweise` aus der Bauart, der Rückweg beim Laden läuft über
`BauartAusBauweise` (`:743-744`).

### 2.3 Das Soll — Reiter 1 als Textskizze

```
+-------------------------------------------------------------------------------------------+
|  Gebäude im Katalog bearbeiten: "Mehrfamilienhaus 1969-1978"                          [x]  |
+-------------------------------------------------------------------------------------------+
|  [ Gebäude und Hülle ]   [ Temperaturen und Ferien ]                                       |
+-------------------------------------------------------------------------------------------+
|  Kenngrößen                                                                                |
|    Name             [ Mehrfamilienhaus 1969-1978 ]   Gebäudetyp  [ Wohngebäude       v]    |
|    Beschreibung     [                            ]   Gebäudeart  [ Mehrfamilienhaus  v]    |
|    Baujahr (Klasse) [ E  1969-1978              v]   Verwendung  [ Wohngebäude       v]    |
|    Bauart           [ schwer                    v]   Wohn-/Nutzfläche  [  850 ] m²         |
|    Fläche / Nutzer  [   35 ] m²                      Interne Gewinne   [ 4250 ] W          |
|    Fensterdurchlaßgrad  [ 0,60 ]                     Raumhöhe          [  2,5 ] m          |
|    Luftwechselrate      [ 0,70 ] 1/h                                                       |
+-------------------------------------------------------------------------------------------+
|  Hülle: Transmission je Bauteil                                                            |
|  +--------------------------+----------+----------+---------------+----------+             |
|  | Bauteil                  | U bzw. p | A bzw. L | Randbedingung |    U*A   |             |
|  +--------------------------+----------+----------+---------------+----------+             |
|  | Außenwand                | [ 1,20 ] | [  520 ] | Außenluft     |   624,0  |             |
|  | Fenster                  | [ 2,80 ] |    128   | Außenluft     |   358,4  |  A gerechnet|
|  | Dach                     | [ 0,60 ] | [  310 ] | Außenluft     |   186,0  |             |
|  | Bodenplatte              | [ 1,00 ] | [  310 ] | [ Erdreich v] |   310,0  |  nur hier   |
|  | Sonstiges                | [ 2,00 ] | [    8 ] | Außenluft     |    16,0  |             |
|  | Wärmebrücke Fenster-Wand | [ 0,10 ] | [  240 ] |      -        |    24,0  |             |
|  | Wärmebrücke AW-Keller    | [ 0,15 ] | [   88 ] |      -        |    13,2  |             |
|  | Wärmebrücke Wand-Dach    | [ 0,10 ] | [   88 ] |      -        |     8,8  |             |
|  +--------------------------+----------+----------+---------------+----------+             |
+-------------------------------------------------------------------------------------------+
|  Wärmeleitwerte                                                                            |
|    H_T Transmission :  1 540,4 W/K       H_ve Lüftung :    505,8 W/K                       |
|    H_ges gesamt     :  2 046,2 W/K                                                         |
+-------------------------------------------------------------------------------------------+
|  Fenster nach Orientierung                                                                 |
|    Fensterfläche Nord [   24 ] m²      Fensterfläche Süd  [   52 ] m²                      |
|    Fensterfläche Ost  [   26 ] m²      Fensterfläche West [   26 ] m²                      |
|    Summe Ost + West :    52 m²         gesamte Fensterfläche :  128 m²                     |
+-------------------------------------------------------------------------------------------+
|  Modellparameter (VDI 6007)                                                                |
|    Rahmenanteil         [ Vorgabe 0,3 ]   Verschattungsfaktor  [ Vorgabe 0,9 ]              |
|    Masseanteil außen    [ Vorgabe 0,3 ]   Innenflächenfaktor   [ Vorgabe 2,5 ]              |
|    Strahlungsanteil Hz. [ Vorgabe 0,3 ]   Heizleistungsgrenze  [ unbegrenzt ] kW            |
|    [ ] Außenbauteile mit Strahlung                                                          |
+-------------------------------------------------------------------------------------------+
|  Rechenweg         [ VDI 6007                        v]                                    |
|    VDI 6007: Raumtemperatur, Kühlbedarf und Spitzenlast je Stunde.                          |
|  > Tagesbilanz (Bestandsweg)                                    (eingeklappt, nur Altweg)  |
+-------------------------------------------------------------------------------------------+
|                                         [ Speichern unter... ]  [ Abbrechen ]  [ OK ]      |
+-------------------------------------------------------------------------------------------+
```

**Die Maske folgt allein der VDI-6007-Struktur** (E20, 16.09.2026). Die **Modellparameter stehen
immer** — auch bei einem Gebäude auf dem Altweg —, sind immer bearbeitbar und gelten dort nach der
Umstellung; sie sind der Parametersatz des Gebäudes, nicht der Rechenweg. Der Schalter heißt
**„Rechenweg"** und trägt **für die Dauer des Übergangs** zwei Werte (E23, E26): **„VDI 6007"**
(Vorgabe, Spaltenwert NULL) und **„Tagesbilanz"**; darunter steht eine Herleitungszeile, die in beiden
Stellungen sagt, was gilt — bei „Tagesbilanz" zusätzlich, dass dieser Weg der eingefrorene
Bestandsweg ist und weder Kühllast noch Anlagenkopplung kennt.

Steht der Rechenweg auf **Tagesbilanz**, klappt darunter der Abschnitt
**„Tagesbilanz (Bestandsweg)"** auf. Er trägt allein die Felder, die **nur** der Altweg liest — nach
[Befund X](Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md) 1.5 sind das
vier Spalten, davon zwei sichtbare: **Gebäudetyp** (die Tagesverteilung hängt daran) und
**Fensterfläche Ost/West** (schreibgesperrt, gerechnete Summe aus Ost und West); `Wochenende` und
`Ferien` sind abgeleitete Flags, die die Hülle setzt und die der Dialog nicht zeigt (Befund X 5.1).
Dazu tritt unter „Wärmeleitwerte" die vierte Zeile mit dem gewichteten Wert (2.5). **Der Abschnitt
bleibt bis zur Ablösung** (Stufe GA, fällig nach dem Ablösekriterium Q24; E23, E26, E27) und steht mit dem Schalter in der
Löschliste (Kapitel 6); bei einem Gebäude auf VDI 6007 erscheint er nicht.

**Reiter 2 „Temperaturen und Ferien"** bleibt, verliert aber die Gruppen
„Wärmebrückenverlustkoeffizienten" und „Abmessung Anschluß" (ihre sechs Werte stehen jetzt in der
U·A-Tabelle), den eigenen Stand (M-2) und den Knopf „Werte übernehmen".

### 2.4 Die Feldtabelle

**Die Spalte „Weg"** sagt, welcher Rechenweg die Größe liest — `beide`, `nur VDI`, `nur Altweg`
oder `—` (kein Rechenleser). Sie ist die Regel für den Dialog nach E20: **nur Altweg** gehört in
den eingeklappten Abschnitt „Tagesbilanz (Bestandsweg)" (2.3), alles
andere steht in der Hauptstruktur — die Modellparameter immer sichtbar und bearbeitbar. Die
Zuordnung je Spalte samt Zählung steht in
[Befund X](Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md) 1.2 bis 1.5.

*Gruppe „Kenngrößen"* — unverändert bis auf einen Zugang: die **Luftwechselrate wandert von Reiter 2
nach vorn** (heute `GebaeudeKatalogDialog.razor:322-324`), weil H_ve aus ihr entsteht und in der
Summenzeile derselben Ansicht steht.

| Feld | Einheit | Bindung | Vorgabe | Pflicht | Weg | Prüfregel (Konzept 4.8) |
|---|---|---|---|---|---|---|
| Name | — | `Daten.Name` (Admin: Klappliste) | — | beim Anlegen | — | nicht leer |
| Gebäudeart / Baujahr / Verwendung | — | `Gebaeudeart`, `Baualtersklasse`, `Verwendung` (**Steuerwert**, `GebaeudeKatalogDaten.cs:45-49`) | — | nein | — | — |
| **Gebäudetyp** | — | `Typ` — Schlüssel der Tagesverteilung (`SimulationWaermebedarf.cs:584`, `:601`) | — | nein | **nur Altweg** | Abschnitt „Tagesbilanz (Bestandsweg)" |
| Bauart | — | `Bauart` → `Bauweise` (`:713-715`) | schwer | **ja** (Fußnote) | beide | 5 ≤ Bauweise/Nutzfläche ≤ 200 Wh/(m²K) |
| Nutzfläche (E13) | m² | `WohnflaecheGesamt` (Editorfeld des Bestands; Spalte `Nutzflaeche` ab M3, E19) | — | **ja** | beide | > 0 |
| Fläche / Nutzer | m² | `FlaecheNutzer` | 35 (Hülle, `GebaeudeKatalogHuelle.cs:429-431`) | **ja** | beide | > 0 |
| Interne Wärmegewinne | W | `Waermegewinne` | — | **ja** | beide | ≥ 0 |
| Fensterdurchlaßgrad | — | `Fensterdurchlassgrad` | — | **ja** | beide | 0 < g ≤ 1 |
| Raumhöhe | m | `Raumhoehe` | — | **ja** | beide | > 0 |
| Luftwechselrate | 1/h | `Luftwechselrate` | — | **ja** | beide | > 0 |

*Gruppe „Tagesbilanz (Bestandsweg)"* — eingeklappt, **nur bei einem Gebäude auf dem Altweg**, bis
zur Stufe GA (E23, E26):

| Feld | Einheit | Bindung | Weg | Bemerkung |
|---|---|---|---|---|
| Gebäudetyp | — | `Typ` | nur Altweg | wählt die Tagesverteilung aus `Abfrage_Tagverteilung` |
| Fensterfläche Ost/West | m² | `Fensterflaeche_Ost_West` | nur Altweg | **schreibgesperrt**: gerechnete Summe aus Ost und West (2.6) |
| (`Wochenende`, `Ferien`) | 0/1 | abgeleitete Flags, gesetzt beim Übernehmen des zweiten Reiters (`GebaeudeKatalogDialog.razor:953`, `:955`; Träger `GebaeudeKatalogDaten.cs:139`, `:142`; Stand 22.09.2026) | nur Altweg | **nicht gezeigt** — Ableitungen, keine Eingaben. Die Schreibstellen bleiben bis GA, damit ein Gebäude auf dem Altweg dieselben Werte behält (Befund X 4.3, X1), und stehen in der Löschliste (Kapitel 6) |

**Fußnote zur Bauart.** Bei leerer Auswahl liefert `BauweiseAusBauart` ein **absolutes** 50
(`Gebaeudebauweise.cs:66`), nicht `Wohnfläche × 50`; die neue Prüfregel fiele damit für jedes
Gebäude über 10 m² auf einen unerklärlichen Fehler. Die Bauart ist deshalb Pflichtfeld — oder der
Rückfall wird in GB auf `Wohnfläche × 50` gezogen (ergebnisneutral, weil heute kein
Referenzgebäude ihn trifft).

*Gruppe „Modellparameter (VDI 6007)" und der Schalter „Rechenweg"* — die neuen Felder; **alle
nullbar, der Dialog schreibt `null`, nicht die Vorgabe** (Vorbild
`EPOS.UI/Dialoge/Erzeuger/PvModellFelder.razor:93-97`). **Alle sind immer sichtbar und immer
bearbeitbar** (E20) — auch bei einem Gebäude auf dem Altweg, wo sie nach der Umstellung gelten:

| Feld | Einheit | Bindung (neue Spalte, 1.6) | Vorgabe-Anzeige | Weg | Prüfregel | Sichtbar |
|---|---|---|---|---|---|---|
| **Rechenweg** | — | `Modell` (`Gebaeude_Modell`); **NULL = „VDI 6007"**, zweiter Wert „Tagesbilanz" (E1, E20, E23) | — | Weiche | Wert aus `DbWerte.GEBAEUDE_MODELL_*` | immer, **bis GA** |
| Rahmenanteil | — | `Rahmenanteil` | Vorgabe 0,3 | nur VDI | 0 ≤ x < 1 | **immer** |
| Verschattungsfaktor | — | `Verschattungsfaktor` | Vorgabe 0,9 | nur VDI | 0 < x ≤ 1 | **immer** |
| Masseanteil außen | — | `Masseanteil_Aussen` | Vorgabe 0,3 | nur VDI | 0,05 ≤ x ≤ 0,95 | **immer** |
| Innenflächenfaktor | — | `Innenflaechenfaktor` | Vorgabe 2,5 | nur VDI | 0,5 ≤ x ≤ 10 | **immer** |
| Strahlungsanteil Heizung | — | `Heizung_Strahlungsanteil` | Vorgabe 0,3 | nur VDI | 0 ≤ x ≤ 1 | **immer** |
| Heizleistungsgrenze | kW | `Heizleistung_Max` | Vorgabe: unbegrenzt | nur VDI | > 0 | **immer** |
| Außenbauteile mit Strahlung | — | `Aussenbauteile_Strahlung` (0/1, `NOT NULL DEFAULT 0`) | aus | nur VDI | — | **immer** |

Die DTO-Erweiterung in `EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDaten.cs` sind zwölf Felder —
`string? Modell` (null = VDI6007), `string? GrundflaecheRandbedingung` (null = ERDREICH),
`bool AussenbauteileStrahlung` und neun `double?` — die fünf Zahlenparameter der Gruppe
„Modellparameter" (`Rahmenanteil`, `Verschattungsfaktor`, `MasseanteilAussen`,
`Innenflaechenfaktor`, `HeizungStrahlungsanteil`), die Heizleistungsgrenze, die `Kellertemperatur`
sowie `FensterflaecheOst` und `FensterflaecheWest` (2.9). Alle Zahlen des DTO sind dort ohnehin
`double?`, „weil leer etwas anderes ist als 0" (`:13-17`) — die Semantik passt also schon.

**Hilfe und KI-Anmeldung gehören dazu.** Die neue Gruppe trägt
`<InfoKnopf Schluessel=… Dialogname=… />` — „Jeder Dialog bietet den Hilfe-Assistenten an"
([`EPOS.UI/CLAUDE.md`](../../EPOS.UI/CLAUDE.md), Abschnitt `Dialoge/`) —, und der Wirt meldet die
Feldliste über `KiMaskenanmeldung.Fuer(name, () => Daten, KiHaken())` an; `Pruefen` ist dieselbe
Prüfung wie am OK-Weg.

**Weder verstecken noch sperren — die Modellparameter stehen immer** (E20, 16.09.2026). Der
Bestandsentwurf wollte die sieben Parameterfelder im Tagesbilanz-Weg ausblenden oder sperren und
ließ die Wahl zwischen beidem offen (Frage U2). E20 hebt die Frage auf: Die Felder sind der
**Parametersatz des Gebäudes**, nicht der Rechenweg, sie gelten nach der Umstellung auf VDI 6007,
und ein Dialog, der je nach Wahl Felder verschwinden lässt, ist für den Anwender ein Rätsel und
für die Tests ein Zustandsraum. Bedingt ist allein der **Zugang**: der eingeklappte Abschnitt
„Tagesbilanz (Bestandsweg)" mit den vier Feldern, die nur der Altweg liest (2.3) — dort über `@if`,
weil ein Abschnitt, den es für dieses Gebäude nicht gibt, auch nicht grau dastehen soll. Die
Herleitungszeile unter dem Schalter „Rechenweg" sagt in beiden Stellungen, was gilt.

### 2.5 Die U·A-Tabelle und die Wärmeleitwerte

Eine `<table class="epos-raster">` in einer `.epos-raster-huelle` — dieselbe Bauform, die die
Gebäudedialoge für ihre Listen schon benutzen (`GebaeudeDialog.razor:69-92`, `:110-133`), **nicht**
die virtualisierte `Katalogliste`.

| Spalte | Inhalt | Bauform |
|---|---|---|
| Bauteil | fester Zeilentext | `<th scope="row">` |
| U bzw. ψ | W/(m²K) bzw. W/(mK) | `Zahlenfeld`, `Min="0.1" Max="6"` bei U, `Min="0" Max="2"` bei ψ |
| A bzw. L | m² bzw. m | `Zahlenfeld`, `Min="0"` |
| Randbedingung | Außenluft \| Erdreich \| Keller | `Auswahlfeld`, **nur** in der Zeile „Bodenplatte" wählbar, sonst fester Text; bei „Keller" steht daneben das Feld `Kellertemperatur` (Vorgabe 10 °C) — der Eingangsbauer setzt daraus θ_NR,eq statt eines Reduktionsfaktors (Konzept N1.3) |
| U·A | W/K, **gerechnet, nur Anzeige** | `<td class="epos-zahl">` |
| Herkunft | manuell \| Katalog \| IFC | **ab G4**, vorher nicht gezeichnet |

Die acht Zeilen und ihre Bindungen: Außenwand (`UWertAussenwand`/`FlaecheAussenwand`), Fenster
(`UWertFenster`/**Summe der vier Orientierungen, nur lesbar**), Dach
(`UWertDachflaeche`/`Dachflaeche`), Bodenplatte (`UWertGrundflaeche`/`Grundflaeche`/**Randbedingung**),
Sonstiges (`UWertSonstiges`/`SonstigeFlaechen`) und die drei Wärmebrücken
(`WbvkFensterWand`/`AnschlussFensterWand`, `WbvkAussenwandKeller`/`AnschlussAussenwandKeller`,
`WbvkWandDach`/`AnschlussWandDach`).

**Die Randbedingung der Grundfläche steht genau hier und nirgends sonst** — sie ist eine Spalte,
kein zwölftes Feld der Modellgruppe (Konzept 8.1 zählt sie dort auf; dieses Papier folgt E2/N1.6,
Korrektur in 1.10). **Die Tabelle ist in beiden Rechenmodellen sichtbar**: sie ist die gemeinsame
Zielstruktur von Klassenweg (Konzept 4.3), Bauteilweg (G3) und IFC-Import (G4), und auch im
Tagesbilanz-Weg sind U, A, ψ und L die Eingaben.

**Die Summen** stehen darunter in einer eigenen Gruppe „Wärmeleitwerte" (oder als `<tfoot>`):

| Größe | Rechnung | Einheit |
|---|---|---|
| **H_T** | Σ (U·A)_Bauteile + Σ (ψ·L)_Wärmebrücken | W/K |
| **H_ve** | `Luftwechselrate` · `WohnflaecheGesamt` · `Raumhoehe` · 0,34 Wh/(m³K) | W/K |
| **H_ges** | H_T + H_ve | W/K |
| **H_T gewichtet (Tagesbilanz)** — *nur im Tagesbilanz-Weg* | 0,83·U_w·A_w + U_f·A_f + 0,95·U_d·A_d + 0,45·U_g·A_g + U_s·A_s + 0,83·Σψ·L | W/K |

Der Faktor 0,34 ist der des Stundenmodells (Konzept 4.4); der Bestand rechnet mit
`1,2 · 0,2777…` = 0,3333 Wh/(m³K) (`EPOS.Kern/Allgemein/BhkwPlan.cs:357`, `:359`), die Gewichte
stehen in `SpezWaermeverlusteC` (`BhkwPlan.cs:347-353`). Darunter eine `Herleitungszeile`: „Der
Tagesbilanz-Weg wichtet Außenwand und Wärmebrücken mit 0,83, das Dach mit 0,95 und die Bodenplatte
mit 0,45. Das Stundenmodell rechnet ungewichtet." Damit ist der Unterschied je Gebäude erklärbar,
wie E2 es verlangt.

**Die Rechnung liegt im Kern, nicht im Dialog.** Eine reine Hilfsklasse
`EPOS.Kern/Allgemein/Gebaeudehuellbilanz.cs` nach dem Vorbild
`EPOS.Kern/Allgemein/Gebaeudebauweise.cs` und `Ferienzeit` liefert Zeilen und Summen — **eine
Wahrheit für Dialog und Eingangsbauer** (1.4); der Dialog zeigt sie nur an. Dieselbe Maske hat das
schon einmal begründet: „Die Rechnung selbst steht — wie Ferienzeit und Suchmuster — als reine
Hilfsklasse im Kern; ein Controller wird von dieser Komponente nicht angefasst"
(`GebaeudeKatalogDialog.razor:43-45`). **Die Kernprobe hält Transmission + Wärmebrücken** des
gewichteten Zweigs auf 1e-12 gegen `SpezWaermeverlusteC` (`BhkwPlan.cs:347-353`), mit
`aussenTemp = 0` und nach Division durch 100 (die Rückgabe `:362` trägt den Faktor 100, der
Aufrufer teilt ihn wieder heraus, `SimulationWaermebedarf.cs:774`). Der Lüftungsanteil des Bestands
hängt an der Außentemperatur (`:356-357`) und ist keine Dialogkennzahl — er bleibt außen vor.
Bezugsfläche ist im Lauf `Tab_Gebaeude.Wohnflaeche` (`SimulationWaermebedarf.cs:773`), im Editor
`Wohnflaeche_gesamt`; beim Schreiben werden sie gleichgesetzt (`GebaeudeKatalogHuelle.cs:427`,
`:458`), bei Altzeilen nicht — der Dialog nennt das in der Herleitungszeile. Die Kennzahl **H_T
wandert zusätzlich in `KennzahlenKatalog.cs` und den Bericht** (Konzept N1.6).

**E13 (16.09.2026): die Bezugsfläche heißt Nutzfläche.** Feldbeschriftung, Herleitungszeile,
Prüfregeln und Meldungstexte nennen sie **Nutzfläche** (beheizte Netto-Grundfläche), nicht mehr
Wohnfläche; Pflichtfelder des Imports sind Nutzfläche und Raumhöhe (Konzept N1.17). **E19 (16.09.2026,
Q11a):** die Spalte heißt künftig auch im Schema so — `Wohnflaeche` wird in `Tab_Gebaeude` und
`Tab_Gebaeude_STAMM` im Gebäudespalten-Schritt (1.6, mit dem Sichtneubau) zu **`Nutzflaeche`**
umbenannt, Werte 1:1, alle Leser und Schreiber auf den neuen Namen; `Wohnflaeche_gesamt` und die
Skalierungsspalten der Projektzuordnung (E8) bleiben; der Referenzlauf bleibt byte-gleich (Konzept N1.24).

**Zwei Prüfregeln hängen an der Tabelle** (Konzept 4.8): U-Werte zwischen 0,1 und 6 W/(m²K) — als
`Min`/`Max` am Feld **und** als Meldung beim OK — und `R_Rest,AW > 0`, also mittleres U der opaken
Bauteile unter 4,17 W/(m²K); die zweite kann erst die Hilfsklasse rechnen und meldet benannt beim
Speichern.

### 2.6 Fenster nach Orientierung

| Feld | Einheit | Bindung | Vorgabe | Pflicht | Regel |
|---|---|---|---|---|---|
| Fensterfläche Nord | m² | `FensterflaecheNord` | — | **ja** | ≥ 0 |
| Fensterfläche Süd | m² | `FensterflaecheSued` | — | **ja** | ≥ 0 |
| Fensterfläche Ost | m² | **`FensterflaecheOst` (neu)** | ½ der Summe Ost+West | nein | ≥ 0 |
| Fensterfläche West | m² | **`FensterflaecheWest` (neu)** | ½ der Summe Ost+West | nein | ≥ 0 |
| Summe Ost + West | m² | **gerechnet**, nur Anzeige | — | — | wird nach `Fensterflaeche_OstWest` mitgeschrieben |
| gesamte Fensterfläche | m² | **gerechnet**, nur Anzeige | — | — | = Summe der vier; geht in die U·A-Zeile „Fenster" |

**Eine Wahrheit im Dialog, zwei Leser im Kern** (Konzept 6.1): Der Dialog pflegt Ost und West,
schreibt aber die **Summe** nach `Fensterflaeche_OstWest` mit, damit `SolareGewinneC` auf dem
Tagesbilanz-Weg unverändert rechnet. Sind beide Felder leer, gilt „je die Hälfte der Summe" — das
ist die Vorgabe des **Kerns**, nicht des Dialogs; der Dialog schreibt `null`.

Damit wird die Plausibilitätsregel aus Konzept 4.8 („Summe der Fensterflächen =
`gesamte_Fensterflaeche`") **erfüllbar statt prüfbar**: Die Gesamtfläche ist gerechnet, nicht
eingegeben. Die Prüfung bleibt trotzdem als Wache im Kern — für Bestandsdatensätze und für den
IFC-Import. Heute leitet die Hülle sie aus drei Feldern ab
(`gesamte_Fensterflaeche = Süd + Ost + Nord`, `GebaeudeKatalogHuelle.cs:453-454`).

### 2.7 Der Wirt und der Bedarfsdialog

**`GebaeudeDialog.razor` bekommt drei Dinge:**

1. eine **Spalte „Rechenweg"** in der Projektliste (heute Wahl + Name, `:75-89`) — ohne sie ist E1
   für den Anwender unsichtbar; sie bleibt, **solange es zwei Wege gibt**, also bis zur Stufe GA
   (E23, E26; Löschliste Kapitel 6);
2. **zwei leise Kennzahlen** im Detailblock „Gebäude: Verbrauch" (`:154-195`): H_ges [W/K] und den
   Rechenweg als Text — bei einem Gebäude auf dem Altweg **„Tagesbilanz (Bestandsweg)"** statt des
   Produktausweises nach E10 (E20, E23) —, beide nur lesend wie die fünf vorhandenen Felder;
3. den Knopf **„Aus IFC-Datei übernehmen …"** in der Katalogleiste (`:136-148`) — **ab G4**, und
   nur mit Delegat: „Kein Delegat, kein Knopf" (`EPOS.UI/CLAUDE.md:47`).

**`GebaeudeBedarfDialog.razor` bekommt den Vergleich alt/neu** (Konzept 8.2; E20) — eine
Tabelle mit vier Spalten (Kennzahl | Tagesbilanz | VDI 6007 | Abweichung) über fünf Zeilen:
Wärmebedarf Heizung (MWh/kWh nach Einheitenwahl), Spitzenlast (Stunde), Spitzenlast (Tagesmittel),
95-%-Quantil der Stundenlast, Vollbenutzungsstunden. **Beide Spalten sind Auskünfte über
`GebaeudeBedarfCtrl`** — zwei Aufrufe desselben Controllers, keine zweite Rechnung. Dafür braucht
der Controller den vierten Parameter `modellErzwungen` und sein Ergebnis die sechs neuen
Kennzahlen; beides steht in 1.4 und gehört in den Merge G1+G2. **Der Vergleich bleibt, solange
der Altweg besteht** (E23, E26) — mit ihm `modellErzwungen` und das Feld `Vergleich` des DTO: Er
ist Teil des Übergangs, die Entscheidungshilfe des Anwenders, solange beide Rechenwege wählbar
sind, und fällt mit der Stufe GA (Löschliste Kapitel 6). Die Eigenständigkeit des VDI-Wegs berührt
er nicht: Das VDI-Modul rechnet ohne den Altweg, der Vergleich ist eine Anzeige, die der Controller
aus zwei Auskünften zusammensetzt. Ein Gebäude auf dem Altweg trägt in diesem Dialog — wie im
Bericht — die Zeile **„Tagesbilanz (Bestandsweg)"**.

Dazu ein **zweites Bild „Raumtemperatur"** (Jahresverlauf Luft und operativ mit Sollwertband),
gezeichnet im Kern über `ChartRenderer` und hereingereicht als zweiter Delegat
`BildauftragRaumtemperatur` = `Func<Diagrammbereich?, byte[]?>` nach dem Muster von `Bildauftrag`
(`:126-134`), in einem **eigenen** `ChartBild` mit eigenem `BereichGewaehlt`/`Zurueckgesetzt` —
jedes Bild führt seinen eigenen Ausschnitt, und
[`Doku_Simulationsergebnis_Darstellung.md`](Doku_Simulationsergebnis_Darstellung.md) § 5.1 verlangt
Steuerzeile und Datenzoom an **jeder** Jahresganglinie; die Hülle reicht `Fenster(a, 8760)` durch,
der Kern schneidet über `Zugeschnitten`/`XAchseFenster` zu. Nur bei VDI 6007. Und drei neue Kennzahlen im Block „Kennzahlen"
(`:60-84`): Kühlbedarf (informativ), Stunden mit Kühlbedarf, mittlere Raumtemperatur in der Heizzeit.

```csharp
// GebaeudeBedarfDaten.cs — Erweiterung; KEINE 8 760 Werte im DTO (:15-17)
public string Modelltext { get; init; } = "";
public double? SpitzeStundeKw { get; init; }
public double? SpitzeTagesmittelKw { get; init; }
public double? SpitzeQuantil95Kw { get; init; }
public double? KuehlenergieMwh { get; init; }
public int?    KuehlstundenH { get; init; }
public double? MittlereRaumtemperaturC { get; init; }
public GebaeudeBedarfDaten? Vergleich { get; init; }   // der jeweils andere Weg; null = keiner
```

Alle neuen Felder sind **nullbar**: „Ein Reiter zeichnet nie ein vorbelegtes DTO als Ergebnis"
(`EPOS.UI/CLAUDE.md:82-84`); ohne Wert steht „—" (`GebaeudeBedarfDialog.razor:173`). Umgerechnet
wird weiterhin nur an der Anzeigekante über `Energieeinheit` (`:246-251`), die Leistung bleibt kW
(`:253-258`).

**`GebaeudeWohnflaecheDialog` wird zum Skalierungsdialog in VDI-Struktur** (E20, E19). Er bleibt der
Ort der Skalierung nach E8 — Projektfläche, Art der Angabe, Jahresnutzungsgrad und der Schalter für
die dezentrale Warmwasserbereitung —, aber er trägt die Begriffe des VDI-Wegs statt der alten:
Beschriftung und Hilfetext sprechen von der **Nutzfläche** des Projekts (E19, Spalte
`Nutzflaeche`; die Skalierungsspalten `Z_ProjektGebaeude.Wohnflaeche_Waermebedarf` und
`Einheit_Waermebedarf_Wohnflaeche` behalten dagegen Namen und Bedeutung, Konzept N1.24), und die
Herleitungszeile sagt, dass aus Fläche und Verbrauch der **Skalierungsfaktor** der Gebäuderechnung
entsteht. Die vier Größen dieses Dialogs liest **jeder** Rechenweg (Befund X 1.3); ein
Bestandswegabschnitt gehört hier deshalb nicht hinein. Der Dialog heißt in diesem Papier fortan
**Skalierungsdialog**; der Klassenname folgt, wenn die Hülle umzieht (2.8). `GebaeudetypDialog` ist
von E1/E2 nicht betroffen — seine Tagesverteilungen behalten ihren Rechenleser im Bestandsweg bis
zur Stufe GA (E23, E26); dann entfallen `Tab_DBTagV`/`Tab_DBTagVDaten` samt `_STAMM` und der
Dialog nach der Löschliste (Q25, vollständige Ablösung nach E27; Kapitel 6).

### 2.8 Die Hülle nach `EPOS.UI.Daten`

Konzept 8.3 verlangt den Umzug; er ist ohnehin für iOS fällig. **Der Umzug kommt mit G1** — mit
E27 (22.09.2026, [Konzept N1.32](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)) entschieden
(Softwarearchitektur A10). Vorbild ist die Aufteilung von
`SimulationErgebnisHuelle` in fünf Dateien unter `EPOS.UI.Daten/Simulation/`. Aus den beiden
Windows-Hüllen (`GebaeudeHuelle.cs` 483 Z., `GebaeudeKatalogHuelle.cs` 554 Z.) werden je zwei
Hälften: nach `EPOS.UI.Daten/Bedarf/` wandern `GebaeudeHuelle.cs`, `GebaeudeKatalogHuelle.cs`,
`GebaeudeBedarfHuelle.cs` und die Naht `Gebaeudewege.cs`; in der Schale bleiben zwei
Fensterdateien. **Dort bleibt allein das Fenster**: `BlazorDialogForm<T>`, `ShowDialog`, `Size MASS`
und der `Geschlossen`-Rückruf (`GebaeudeHuelle.cs:51-100`, `GebaeudeKatalogHuelle.cs:43-86`) — genau
die Hälfte, die `EnableWindowsTargeting=false` nicht erlaubt.

**Ohne eine benannte Naht wandert die Hülle nicht.** `GebaeudeKatalogHuelle.Gaben(...)` trägt heute
einen `IWin32Window besitzer` (`:88-89`) und reicht ihn an `BrauchwasserGaben(...)` (`:123`, `:253`)
durch, das `BedarfsProfileHuelle.Gaben(besitzer, …)` ruft (`:279`); dasselbe gilt für
`GebaeudetypHuelle.Gaben()` (`GebaeudeHuelle.cs:154-155`). Beide Zielhüllen liegen in der Schale.
Die Bauform steht bereit — `EPOS.UI.Daten/Katalogwege.cs:23-31`: ein `static Func<…>`-Haken mit
folgenloser Vorbelegung, den Windows in `Program.Main` einhängt und iOS leer lässt („Kein Delegat
ist kein Knopf", `Katalogwege.cs:18-21`):

```csharp
internal static class Gebaeudewege
{
    /// Parametersatz der Brauchwasser-Profilliste des laufenden Projekts; null = kein Knopf.
    internal static Func<List<Z_ProjektBrauchwasserModel>,
                         IReadOnlyDictionary<string, object>> BrauchwasserGaben;

    /// Parametersatz der Gebaeudetypen-Verwaltung; null = kein Knopf.
    internal static Func<IReadOnlyDictionary<string, object>> GebaeudetypGaben;
}
```

Der `IWin32Window` fällt damit aus allen `Gaben`-Signaturen — er wurde ohnehin nur weitergereicht,
nie selbst benutzt (`GebaeudeKatalogHuelle.cs:88-123`). Zwei Wachen prüfen den Umzug:
`EPOS.UI.Tests/ParametersatzTests.cs` (jeder Gaben-Schlüssel trifft ein `[Parameter]`, `:12-38`,
Gegenwache am Gerät `WindowsFormsApplication1/Allgemein/Blazor/Parametersatzwache.cs`) und
`EPOS.UI.Tests/HuellenwegTests.cs` (kein modales Systemfenster im Blazor-Ereignis, `:10-57`). Dazu
die Regel der Wurzel-[`CLAUDE.md`](../../CLAUDE.md): Wer eine Hülle oder Naht der Schale anfasst,
prüft sie mit `-p:EnableWindowsTargeting=true` kompiliert. Der Kern bleibt unberührt —
`GebaeudeStammCtrl`, `ProjektGebaeudeCtrl` und `GebaeudeBedarfCtrl` liegen schon in
`EPOS.Kern/Controller/`.

### 2.9 Ressourcenschlüssel und Glossar

**63 neue Schlüssel** — 46 `GEBK_` (Editor), 4 `GEB_` (Wirt), 13 `GEBB_` (Bedarfsdialog); die
vollständige Liste mit deutschem und englischem Wert steht in Befund M, Abschnitt 5.10. Vier
Bestandsschlüssel werden **frei** und werden gelöscht, nicht umgewidmet: `GEBK_GRP_FLAECHEN`,
`GEBK_GRP_UWERTE`, `GEBK_GRP_WAERMEBRUECKEN`, `GEBK_GRP_ANSCHLUSS`.

Die Regeln dazu: beide `.resx` sind Pflicht, UTF-8 **mit** BOM und CRLF, Einträge alphabetisch;
danach **immer** `python3 Werkzeuge/ResourceDesigner/designer_neu.py schreiben`. Die Namensordnung
ist streng (`GRP_*`, `LBL_*` mit Doppelpunkt, `FELD_*` ohne — für die Pflichtmeldung —, `SP_*`,
`BTN_*`, `MSG_*`, `HINWEIS_*`); `LBL_` und `FELD_` sind **bewusst zwei Schlüssel für dasselbe Feld**
(`GebaeudeKatalogDialog.razor:958-961`). Ab etwa zehn Texten ein **Bündel** statt einzelner
Parameter (`EPOS.UI/CLAUDE.md:22-24`) — die Schwelle ist klar überschritten.

**Das Glossar kennt die Gebäudehülle nicht.** In
[`Glossar_Lokalisierung.md`](Glossar_Lokalisierung.md) fehlen unter anderem Wärmebrücke,
Verschattung, Rahmenanteil, Bauteil, Bodenplatte, Keller, Randbedingung, Transmission,
Lüftungsleitwert, operative Temperatur, Kühlbedarf, Bauweise, Rechenmodell und Tagesbilanz.
**Ein Abschnitt „13. Gebäudehülle und Gebäudemodell" muss vor den en-US-Werten stehen** — sonst
entstehen zwei Übersetzungen desselben Begriffs, genau die Lage, die § 12 für die
`KONFIG_*`-Schlüssel eigens einfrieren musste. Vorschlagsliste: Befund M, Abschnitt 3.3. Frage U4 —
mit E28 (22.09.2026) nach Empfehlung entschieden: der Abschnitt entsteht vor den en-US-Werten, fällig
vor G1.

**Eine Lücke im Standardbaustein:** `EPOS.UI/Standards/Zahlenfeld.razor` kennt `Wert`, `Einheit`,
`Min`/`Max`, `Nachkommastellen`, `Aktiv`, `Feldname`, `FehlerZustand` (`:42-93`), aber **keinen
`Platzhalter`** — nur `Textfeld` hat einen. Entweder eine `Herleitungszeile` je Feld (Muster
`BhkwDialog.razor:397-398`, „0 = Projektvorgabe ({0} %)", eingesetzt `:670-688`) oder ein
`Platzhalter`-Parameter am `Zahlenfeld`. **Mit E27 entschieden (U3, 22.09.2026): der
`Platzhalter` am `Zahlenfeld`** — sauberer, rein additiv; er zieht `StilblattTests` nach sich.

### 2.10 Tests der Oberfläche

Heute prüfen **119 bunit-Fälle in fünf Klassen** (2 384 Zeilen) die Gebäudedialoge:
`GebaeudeDialogTests.cs` (40 Fälle), `GebaeudeKatalogDialogTests.cs` (33),
`GebaeudetypDialogTests.cs` (17), `GebaeudeBedarfDialogTests.cs` (14),
`GebaeudeWohnflaecheDialogTests.cs` (15). Das Muster: Klasse erbt `EposBunitContext`,
`JSInterop.Mode = Loose`, `IHilfeDienst` als Attrappe (`GebaeudeKatalogDialogTests.cs:33-37`),
**Kultur auf de-DE gepinnt** (`:17-19`), ein belegter Satz als statische Fabrik `Satz(name)`
(`:39-72`), eine `Aufbauen(...)`-Methode (`:74-93`), Hilfsgriffe `Knopf` (`:95-96`) und
`ReiterWaehlen` über `button[role=tab]` (`:98-99`).

**Neu: 34 Fälle** (24 Editor, 3 Wirt, 7 Bedarf); die 33 aus Befund M, Abschnitt 5.11, und ein
Zugang aus E20. Die tragenden: die Hülltabelle führt acht Zeilen und jede zeigt U·A; die
Fensterzeile ist nur lesbar; nur die Bodenplatte hat eine Randbedingung; H_T ist die Summe der acht
Zeilen; H_ve kommt aus Luftwechsel, Nutzfläche und Raumhöhe; **der gewichtete Wert trifft
`SpezWaermeverlusteC`** (gegen `BhkwPlan.cs:347-353`); **die Vorgabe ist VDI 6007** (leeres `Modell`
→ der Schalter „Rechenweg" steht auf „VDI 6007", E1); ein leeres Parameterfeld **speichert NULL,
nicht 0,3**; die Summe Ost+West wird gerechnet und mitgeschrieben; OK prüft, speichert und
schließt; Abbrechen schreibt nichts.

**Zwei Fälle ersetzen den Modellzustand des Dialogs** (E20). Der Bestandsentwurf wollte prüfen, dass
die sieben Parameterfelder im Tagesbilanz-Weg verschwinden; die Modellparameter stehen jetzt in
beiden Stellungen, es gibt also **keinen Modellzustand mehr zu prüfen**. An seine Stelle tritt der
**Bestandswegabschnitt**: (1) Bei `Modell = 'TAGESBILANZ'` erscheint der eingeklappte Abschnitt
„Tagesbilanz (Bestandsweg)" mit Gebäudetyp und der schreibgesperrten Fensterfläche Ost/West; bei leerem
`Modell` erscheint er **gar nicht** (kein graues Feld, kein leerer Abschnitt). (2) Beim Umschalten
des Rechenwegs bleiben **alle Modellparameterwerte stehen** und werden auch im Tagesbilanz-Weg
gespeichert — sie sind der Parametersatz, nicht der Rechenweg (2.4).

**Sieben bestehende Fälle sind anzupassen**: die beiden Feldbestandsfälle je Reiter (`:105-125`,
`:138-158`), `Ohne_Uebernehmen_bleibt_der_Satz_unberuehrt` (`:486`) und die Fälle um
`Beenden`/`Ueberschreiben` (`:174-217`, `:388-421`). Dazu die Fälle des Skalierungsdialogs
(`GebaeudeWohnflaecheDialogTests.cs`, 15 Fälle), soweit sie die Beschriftungen prüfen, die auf
„Nutzfläche" ziehen (2.7, E19).

**Die Rasterprobe gilt hier nicht — mit einer Einschränkung.** `Proben/Rasterprobe` misst **allein**
die virtualisierte `Katalogliste` (QuickGrid `Virtualize`) im echten Browser
([`Proben/Rasterprobe/LIESMICH.md`](../../Proben/Rasterprobe/LIESMICH.md)); die Gebäudedialoge
zeichnen schlichte `table.epos-raster`. Wer beim Umbau eine `.epos-raster*`-Stilregel anfasst — etwa
für die rechtsbündige Zahlenspalte oder die Summenzeile —, **zieht sie**; wer nur Markup hinzufügt,
nicht. **`Proben/ChartProben` ist dagegen Pflicht**, sobald das Bild „Raumtemperatur" entsteht
(Konzept 9; Regel in [`EPOS.Kern/CLAUDE.md`](../../EPOS.Kern/CLAUDE.md), Abschnitt „Bericht": ein
neuer Parameter bekommt eine Vorgabe, die das Bild byte-gleich lässt).

Ohne Zutun greifen außerdem `ParametersatzTests` (neue Gaben-Schlüssel), `SchliesskreuzWacheTests`,
`UeberlagerungstitelTests`, `StilblattTests` und `HuellenwegTests`.

### 2.11 Aufwand der Oberfläche

| Teil | Inhalt | Aufwand |
|---|---|---|
| **M-a** | `EPOS.Kern/Allgemein/Gebaeudehuellbilanz.cs` (Zeilen, H_T, H_ve, H_ges, gewichteter Zweig) samt Kernprobe gegen `SpezWaermeverlusteC` | 0,5 PT |
| **M-b** | 12 Felder in `GebaeudeKatalogDaten` (2.4), `AusModell`/`NachModell`, Ost/West-Summenschreibung, Umbenennung `Fensterflaeche_OstWest` | 0,5 PT |
| **M-c** | U·A-Tabelle und Summen; Wegfall der drei alten Gruppen und der zwei Reiter-2-Gruppen | 1,0 PT |
| **M-d** | Fenster Ost/West samt Summenanzeigen | 0,3 PT |
| **M-e** | Gruppe „Modellparameter (VDI 6007)": sieben immer sichtbare Felder, Schalter „Rechenweg", `Kellertemperatur`, Herleitungszeile, `Platzhalter` am `Zahlenfeld`; dazu der **freie Zahlenweg für `Bauweise`** — heute überschreibt `BauweiseNachfuehren()` (`GebaeudeKatalogDialog.razor:713-715`) vor jedem Speichern jeden freien Wert mit `Nutzfläche × 20/50/100` | 0,7 PT |
| **M-f** | **Ein Schreibweg**: `SpeichernLeiste` statt drei Knöpfen, Reiter-2-Stand auflösen, zehn Prüfregeln an einer Stelle | 1,0 PT |
| **M-g** | Wirt: Spalte „Rechenweg", zwei Kennzahlen im Detailblock | 0,3 PT |
| **M-h** | Bedarfsdialog: Vergleichstabelle, sechs Kennzahlen, zweites Bild, DTO | 1,0 PT |
| **M-i** | 63 Schlüssel in zwei `.resx`, Glossarabschnitt 13, `ResourceDesigner` | 0,7 PT |
| **M-j** | 34 neue bunit-Fälle, Anpassung von sieben bestehenden | 1,0 PT |
| **M-k** | Hülle nach `EPOS.UI.Daten`: vier Dateien, `Gebaeudewege`-Naht, `IWin32Window` heraus, Linux-Bau der Schale | 1,0 PT |
| **M-l** | **Bestandswegabschnitt „Tagesbilanz (Bestandsweg)" (E20, E23)**: eingeklappter Block mit zwei sichtbaren Feldern und dem gewichteten Wert der Wärmeleitwerte, Herleitungszeile am Schalter „Rechenweg", Ausweis „Tagesbilanz (Bestandsweg)" in Wirt und Bedarfsdialog, Skalierungsdialog in VDI-Struktur (2.7), die beiden Ersatzfälle aus 2.10 | **1,0 PT** |
| | **Summe** | **9,0 PT** |

**Das ist der Grund, warum der G1-Rahmen des Konzepts (10–16 PT für Trennung, Schemaschritt,
Namensleser, Eingangsbauer, Verzweigung, Dialog, Hülle und Texte zusammen) nicht trägt.** Die
Oberfläche allein ist 9,0 PT; Kapitel 4 rechnet neu und ist die Quelle der verbindlichen Aufwände
(F-S8). **Was der Bestandswegabschnitt kostet, spart die entfallene
Modellzustandslogik zum Teil wieder ein** — die sieben bedingten Felder aus dem Bestandsentwurf
sind sieben unbedingte geworden (2.4).

---

## 3. Der IFC-Gebäudeimport (Stufe G4a)

### 3.1 Ziel und Grenzen

**Ziel:** aus einer IFC-Datei die Felder des Gebäudekatalogs vorbelegen — dieselben Zeilen, die E2
im Dialog sichtbar macht (U, A, U·A je Bauteilgruppe), jede mit **Herkunftskennzeichen** und
**Beleg**. Geschrieben wird in die **Projektzeile `Tab_Gebaeude`** über einen eigenen Delegaten
`UebernehmenInsProjekt(daten, idGebaeude)` des Gebäudedialogs. Der `Speichern`-Delegat des
Katalogeditors (`GebaeudeKatalogDialog.razor:399`) trifft dagegen `Tab_Gebaeude_STAMM`
(`GebaeudeKatalogHuelle.cs:318-337` ruft `GebaeudeStammCtrl.Insert`/`Overwrite`, `TABLE` `:13`,
`TABLE_PROJ` `:14`) und ist damit nur der zweite, **ausdrücklich zu wählende** Weg „als Katalogsatz
ablegen". Die Zielfelder liegen fertig in
`EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDaten.cs:29-122` (7 Flächen, 5 U-Werte, Bauweise,
3 ψ + 3 Längen, Luftwechsel).

**Umgesetzt (Protokoll G4 Abschnitt 6): kein `UebernehmenInsProjekt`, sondern ein neues Gebäude.**
Nach dem OK des Zuordnungsdialogs öffnet der Katalogeditor im Modus Neu, vorbelegt
(`GebaeudeImportHuelle.Vorbelegung` über `NachKatalogdaten`); sein gewöhnlicher Schreibweg legt den
Katalogsatz an. Der Gebäudedialog nimmt die neue Zeile samt ausstehender Herkunft in seine
Projektliste, und das Speichern der Gebäudeliste schreibt Projektkopie und Herkunft in einem Vorgang
(`WizardCtrl.GebaeudeZuordnungAnlegen` → `GebaeudeImportCtrl.SchreibeHerkunft`,
`EPOS.Kern/Controller/WizardCtrl.cs:2654`, `:2685`; scheitert die Herkunft, rollt der ganze Vorgang
zurück). Begründung: Es gibt keinen Editor für Projektkopien, eine abweichende Kopie wäre für den
Anwender unsichtbar, und der Katalogsatz zeigte andere Werte, als das Projekt rechnet. Bricht der
Anwender den Gebäudedialog ab, bleibt der Katalogsatz, und es entstehen weder Projektzeile noch
Herkunft; ein im Katalog schon vergebener Name wird am OK des Zuordnungsdialogs abgelehnt.

**Grenzen, die in G4a bewusst nicht überschritten werden:**

- **Keine Geometrieableitung.** Fehlen die Quantity-Sets, bliebe nur `IfcExtrudedAreaSolid`. Für
  eine prismatische Wand wäre das rechenbar (`SweptArea` × `Depth`, Polygonfläche über die
  Trapezformel), es scheitert aber an `IfcBooleanClippingResult` (Giebelwände), an nicht
  prismatischen Wänden und an `IfcMappedItem` — und genau diese drei kommen in Bestandsmodellen
  regelmäßig vor. **In G4a nicht bauen**: der Leser meldet `IMP_IFC_PROT_KEINE_MENGEN` und lässt
  das Feld leer; die Ableitung bleibt G5.
- **Kein Mehrzonenmodell.** E7 vergibt es an ein eigenes Papier
  (`Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`, in Arbeit); G4a schreibt in `Tab_Gebaeude`, nicht in
  `Tab_Bauteil`. Zonen werden gar nicht gelesen (3.5, Nr. 8). Die Bauteilebene — auf Wunsch eine
  Zone mit Bauteilen und Aufbauten, für IFC und gbXML — bringt G4b (3.8, Konzept N1.49).
- **Kein Ort, kein Klima.** `IIfcSite.RefLatitude/RefLongitude` sind
  `IfcCompoundPlaneAngleMeasure` (`LIST [3:4] OF INTEGER`) und von `IfcUnitAssignment` **nicht**
  betroffen; die Klimaregion wählt der Anwender im Projekt.
- **Kein Menüpunkt** (Konzept 8.4): der Import ist projektbezogen, kein Katalogimport, und
  erscheint als **Überlagerung** im Gebäudedialog — dasselbe Muster wie die Brauchwasserliste im
  Katalogeditor (`GebaeudeKatalogDialog.razor:401-405`). Einen **Hilfeschlüssel** bekommt der
  Dialog sehr wohl, über `IfcImportProfil.HilfeSchluessel` (3.3) — nur keinen Eintrag in
  `Menuetabelle.cs`. (Umgesetzt, A17 erfüllt: **ein** Knopf „Importieren (gbXML, IFC)…" im
  Gebäudedialog, `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor:164`, mit einer Dateiwahl für beide
  Formate — das Profil folgt der Dateiendung (`GebaeudeImportProfil.FuerDatei`); der
  Zuordnungsdialog steht als Überlagerung, beide Profile tragen den Hilfeschlüssel
  `GebaeudeImportProfil.HILFE_ZUORDNUNG`; Protokoll G4 Abschnitt 6.)
- **Vorbedingung:** G1 und G2 müssen fertig sein, sonst importiert man in ein Tagesmodell, das die
  Daten nicht nutzt. G3 muss **nicht** fertig sein.

### 3.2 Der Ablauf

Das Muster steht dreifach im Bestand: `KatalogImportAblauf` (Lesen `:119` / Vorprüfen `:259` /
Ausführen `:321`, Klasse `EPOS.Kern/Allgemein/Import/KatalogImportAblauf.cs:81`),
`KatalogImportProfil.cs:196` (was den Lauf unterscheidet, steht als **Daten**),
`KatalogImportSatz.cs:28`, dazu `KlimaImportAblauf.cs:126` und `GanglinienImportAblauf.cs:173`.
Drei Regeln daraus gelten wörtlich: **der Ablauf zeigt nichts an** („Der Konfliktdialog ist kein
Rückruf, sondern eine Zäsur", `KatalogImportAblauf.cs:69-74`), **ein fehlerhafter Eintrag bricht
den Lauf nicht ab** (`:376-381`; nur `OperationCanceledException` beendet ihn), und **der Zustand
lebt im Ablauf, nicht in der Komponente** (`KatalogImportHuelle.cs:111-142`, `:210-233`).

| # | Schritt | Wer | Meldung / Abbruch |
|---|---|---|---|
| 1 | Datei wählen über `Dienste.Datei.DateiOeffnenAsync` | Hülle | `""` = abgebrochen, nichts geschieht |
| 2 | Größe gegen `IfcImportProfil.MaxBytes`: bei `.ifc`/`.ifcxml` die Dateigröße, bei `.ifczip` die **entpackte** Größe aus dem Zip-Verzeichnis (`ZipArchiveEntry.Length`); fehlt sie, gilt das Zehnfache der Dateigröße | Ablauf | `IMP_IFC_PROT_ZU_GROSS` (Fehler), Lauf endet |
| 3 | `MemoryModel.OpenRead(pfad, fortschritt)` — der zweite Parameter ist ein `ReportProgressDelegate` und optional; die Protokollsenke wird einmalig über `XbimServices.Current.ConfigureServices` belegt, die Meldungen des Laufs bleiben `PruefMeldung` | Ablauf, im Arbeitsfaden | `IMP_IFC_PROT_LESEFEHLER` mit `ex.Message` |
| 4 | `MemoryModel.GetSchemaVersion(pfad)` → `XbimSchemaVersion` | Ablauf | `Unsupported`, `Cobie2X4` **und `Ifc4x1`** → `IMP_IFC_PROT_SCHEMA_UNBEKANNT`; angenommen werden genau `Ifc2X3`, `Ifc4`, `Ifc4x3` — die drei Schemata, die `Xbim.IO.MemoryModel` mitbringt |
| 5 | Einheiten aus `IIfcProject.UnitsInContext.Units` | Ablauf | fehlende Längeneinheit → Warnung, Annahme Meter |
| 6 | Gebäude, Geschosse, Räume (`IIfcBuilding`, `IIfcBuildingStorey`, `IIfcSpace`) | Ablauf | 0 Gebäude → Fehler; 0 Räume → Warnung |
| 7 | Bauteile sammeln und gruppieren; außen/innen entscheiden | Ablauf | je verworfenem Bauteil eine Info mit Grund |
| 8 | **U·A-Zeilen bilden** (E2): A = Σ Bruttoflächen, U flächengewichtet | Ablauf | U außerhalb 0,1…6 → Warnung, Wert bleibt, Herkunft `Ifc` |
| 9 | Fenster in Sektoren N/O/S/W über den Azimut des Wirtsbauteils | Ablauf | Fenster ohne Azimut → Sammelposten, Warnung |
| 10 | Vorgaben je Baualtersklasse füllen | Ablauf | jede Vorgabe trägt Herkunft `Vorgabe` |
| 11 | `IfcImportSatz` bilden: Zielfeld, Wert, Herkunft, Beleg | Ablauf | — |
| 12 | **Zuordnungsdialog**: Tabelle, Haken je Zeile, OK/Abbrechen | Komponente | Abbrechen → `null`, **nichts wird geschrieben** |
| 13 | Plausibilität (Konzept 4.8) auf dem übernommenen Satz | Ablauf | benannte Fehler, Rückkehr in den Dialog |
| 14 | Schreiben über `UebernehmenInsProjekt` in `Tab_Gebaeude` (3.1) und Ablegen der Herkunftsdaten | Hülle | `IfcImportBilanz` mit den Zählern |

Die Schritte 2–11 laufen in **einem** Aufruf (`Lesen`), 13–14 in einem zweiten (`Uebernehmen`) —
dieselbe Zäsur wie `Vorpruefen`/`Ausfuehren`.

**Umgesetzt (Protokoll G4 Abschnitte 1, 3 und 6).** Schritt 3: xBIM reicht einen übergebenen Logger
nicht an den Parser weiter, sondern nimmt ihn aus `XbimServices`; der `IfcLeser` öffnet das Modell
deshalb über den Konstruktor mit eigener Logger-Fabrik und lädt dann — `XbimServices` wird **nicht**
konfiguriert, es entsteht kein globaler Zustand. Die Schritte 2 bis 11 trägt der formatfreie
`GebaeudeImportAblauf` hinter `IGebaeudeLeser`; Schritt 11 bildet den `GebaeudeImportSatz`. Schritt 14
schreibt nicht in eine vorhandene Projektzeile, sondern geht den Weg aus 3.1 (vorbelegter Editor im
Modus Neu, Herkunft beim Speichern der Gebäudeliste).

**Schritt 14 persistiert die Herkunft — sonst ist der Rückweg später verbaut.** Übernommen wird
nicht nur der Satz, sondern auch die Zuordnung **EPOS-Gebäude ↔ `IfcBuilding.GlobalId`** (später je
Zone ↔ `IfcSpace.GlobalId`) samt Name, SHA-256 und Zeitpunkt der Quelldatei, als Herkunftsdaten in
einer eigenen Tabelle. Ohne diese Zeilen ist die **Rückgabe angereicherter Dateien** — der
IFC-Export in seiner Stufe S2 — nicht mehr möglich, weil sich beim zweiten Lauf nicht mehr sagen
lässt, welche EPOS-Zeile zu welcher IFC-Entität gehört; Befund S nennt das ausdrücklich als
Anforderung an **diesen** Import
([`Befund S`](Gebaeudesimulation/2026-09-15_Befund_S_IFC-Export_ohne_Geometriekernel.md),
Abschnitt 6). Der Spaltenvorschlag steht im
[Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md); hier wird kein Datenmodell
ausgeführt. (Umgesetzt: `Tab_Importquelle` und `Tab_Importzuordnung` mit Schemaschritt 138 (S-F),
geschrieben über `GebaeudeImportCtrl.SchreibeHerkunft`; der Einzonenweg schreibt nur die Paarung
Gebäude ↔ `IfcBuilding` bzw. `Building/@id`, die Paarungen je Raum, Fläche und Öffnung gehören zu
G6c. Ein zweiter Import derselben Datei wird über den SHA-256 als „schon importiert" genannt, nicht
gesperrt; Protokoll G4 Abschnitte 4 und 6.)

**Fehlerbilder sind Meldungen, keine Ausnahmen.** Im Importbestand gibt es **keine benannte
Ausnahmeklasse**; das Hausmuster ist `PruefStufe` (`SpeicherEngine/GanglinienPruefung.cs:55`:
Info / Warnung / Fehler) und `PruefMeldung` (`:74`: **Schlüssel + invariant formatierte Werte**, nie
Text — den Text holt erst die Oberfläche). Auch ein Lesefehler wird gefangen und gelegt, nicht
geworfen (`KatalogImportAblauf.cs:185-187`). Die dreizehn Schlüssel `IMP_IFC_PROT_*` samt deutschem
und englischem Wortlaut stehen in Befund N, Abschnitt 4.5; dieses Papier fügt vier hinzu —
`…_SEITE_UNBESTIMMT`, `…_PLATZIERUNGSART`, `…_EIGENSCHAFTSART` und `…_FLAECHENART_GEMISCHT` (3.4),
also **siebzehn**.

### 3.3 Die Klassen

Ort: `EPOS.Kern/Allgemein/Import/Ifc/`, Namensraum wie überall `WindowsFormsApplication1`.

| Datei | Inhalt |
|---|---|
| `IfcSchema.cs`, `IfcHerkunft.cs`, `IfcBauteilart.cs`, `IfcHimmelsrichtung.cs` | `enum IfcSchemaStand { Unbekannt, Ifc2x3, Ifc4, Ifc4x3 }`, `enum IfcHerkunft { Leer, Ifc, Vorgabe, Manuell }`, `enum IfcBauteilart { … }`, `enum IfcSektor { Nord, Ost, Sued, West, Ohne }` — **Werte, keine Anzeigetexte** (Regel `EPOS.Kern/Allgemein/Katalog/ImportKonfliktModell.cs:44-46`) |
| `IfcGebaeudeAbbild.cs` | das Zwischenmodell: was in der Datei steht, noch ohne EPOS-Semantik — `IfcBauteilAbbild` (Kennung, Art, `FlaecheBruttoM2`, `AzimutGrad?`, `NeigungGrad?`, `UWert?`, `GWert?`, `IstAussen?`, Geschoss, Schichten, **`Herkunftsbeleg`**), `IfcSchichtAbbild` (Baustoff, `DickeM`, λ, ρ, c_p), `IfcRaumAbbild`, `IfcGebaeudeAbbild` (mit `TrueNorthGrad?`, `MapConversionVorhanden`, `LaengenFaktorNachMeter`, `FlaechenFaktorNachM2`) |
| `IfcImportProfil.cs` | was den Lauf einstellt (Muster `KatalogImportProfil.cs:196`): `Dateifilter`, **`MaxBytes`**, `SektorBreiteGrad` (90), `UWertMin` (0,1), `UWertMax` (6,0), `HilfeSchluessel` |
| `IfcImportSatz.cs` | je **Zielfeld** eine `IfcFeldzeile` (Zielfeld, Gruppe, `Wert?`, Einheit, `Herkunft`, `Beleg`, `Uebernehmen`); der Satz trägt Schema, Gebäudename, `Baujahr?`, `BaualtersklassenIndex`, Zeilen, Meldungen und `NachKatalogdaten(GebaeudeKatalogDaten grundlage)` |
| `IfcImportAblauf.cs` | `Lesen(pfad, melder, abbruch) → int`, `Zuordnen(gebaeudeIndex, baualtersklasseVorgabe) → IfcImportSatz`, `static Pruefen(satz) → IReadOnlyList<PruefMeldung>` |
| `IfcZuordnungsModell.cs` | die Regeln des Dialogs, **oberflächenfrei** (Muster `ImportKonfliktModell.cs:45`): `HerkunftText`, `KopfText`, `ZeilenText`, `Pruefe` |

**Oberfläche:** `EPOS.UI/Dialoge/Bedarf/IfcZuordnungDialog.razor` mit `IfcZuordnungDaten.cs`
(`IfcZuordnungZeile`: Gruppe, Feld, `IfcWert` als formatierter Text, Beleg, `VorgabeWert`,
`Herkunft` als Anzeigetext aus `IfcZuordnungsModell`, `Uebernehmen`). Die Parameter folgen
`KatalogImportDialog.razor:241-260` (`DateiWaehlen`, `Lesen`, `GebaeudeWechseln`, `Uebernehmen`,
`Meldungstext`, `Fortschrittstext`, `Geschlossen` — `false` = Abbrechen); Fortschritt und Meldungen
über die vorhandenen Bausteine `<Fortschritt …>` (`:121`) und `<Warnbanner …>` (`:99`).

**Die Hülle liegt plattformfrei** in `EPOS.UI.Daten/Bedarf/IfcImportHuelle.cs` (Muster
`EPOS.UI.Daten/Kosten/SpotpreisImportHuelle.cs:32`), nicht in der Windows-Schale — so bekommt iOS
sie ohne zweite Fassung. **Vorbedingung:** Der Schreibweg des Gebäudedialogs muss dann schon
plattformfrei sein. Heute liegt er in der Schale
(`WindowsFormsApplication1/Views/Gebäude/GebaeudeKatalogHuelle.cs:318`), und `EPOS.UI.Daten/Bedarf/`
führt allein `BedarfErgebnisHuelle.cs`; der Umzug ist Teil **M-k** des Dialogumbaus (2.8, 2.11) und
damit mit G1+G2 erledigt, bevor G4a beginnt. Wird G4a wider Erwarten vor dem Umzug gebaut, ist der
IFC-Import auf iOS **benannt abzulehnen** (Muster `IFlottenPlaner`) — nicht still zu übergehen.

**Hilfe und KI-Anmeldung** gelten auch hier: Der Zuordnungsdialog trägt
`<InfoKnopf Schluessel=… Dialogname=… />` (Schlüssel aus `IfcImportProfil.HilfeSchluessel`), und der
Wirt meldet die Feldliste über `KiMaskenanmeldung.Fuer(name, () => Daten, KiHaken())` an; `Pruefen`
ist dieselbe Prüfung wie am OK-Weg. **Der Arbeitsfaden läuft dort über `Kulturweitergabe.Starten`**
(`SpotpreisImportHuelle.cs:61`, `:71`; API `Kulturweitergabe.cs:170`), **nicht** über `Task.Run`
wie in der Windows-Hülle (`KatalogImportHuelle.cs:229`): der Wächter prüft `EPOS.UI.Daten` mit
(`EPOS.Kern.Tests/ParallelitaetWacheTests.cs:52`), die Schale nicht.

**Umgesetzt (Protokoll G4 Abschnitte 1 bis 4 und 6) — die Namen weichen ab.** Was beide Formate
teilen, liegt formatfrei unter `EPOS.Kern/Allgemein/Import/Gebaeude/`: `GebaeudeImportSatz` (statt
`IfcImportSatz`), `GebaeudeZuordnungsModell` (statt `IfcZuordnungsModell`), `GebaeudeImportAblauf`
(statt `IfcImportAblauf`) hinter der Naht `IGebaeudeLeser`, `GebaeudeImportProfil`,
`GebaeudeAggregation` (die gemeinsame Zuordnung nach E2), `GebaeudeVorgaben` (U12), `Importherkunft`
(statt `IfcHerkunft`), das normierte Zwischenmodell `GebaeudeAbbild` und `Baujahrregel`. Der IFC-Leser
ist `Import/Ifc/IfcLeser.cs` mit `IfcAbbildBauer`, `IfcEigenschaften`, `IfcEinheiten`,
`IfcPlatzierung`, `IfcProtokoll`, `IfcGebaeudeAbbild`, `IfcSchemaStand` und `IfcImportProfil`; der
gbXML-Leser `Import/Gbxml/GbxmlLeser.cs`. Der Dialog ist
`EPOS.UI/Dialoge/Import/GebaeudeImportDialog.razor` mit `GebaeudeImportDaten.cs`, die Hülle
`EPOS.UI.Daten/Bedarf/GebaeudeImportHuelle.cs` — die Vorbedingung ist erfüllt, `GebaeudeKatalogHuelle`
liegt plattformfrei in `EPOS.UI.Daten/Bedarf/`. Die Herkunft schreibt `GebaeudeImportCtrl`, die DDL
steht in `ImportzuordnungSchema`. Hilfe und KI: Der Dialog steht wie der Katalogimport als Ausnahme
„Import" in `KiDialogAusnahmen`, der Knopf braucht keine KI-Feldanmeldung; Hilfe über die
Wiki-Quelle „Gebäudeimport".

### 3.4 Abbildungsregeln

Alle Typnamen sind Schnittstellen aus `Xbim.Ifc4.Interfaces`. Pset- und Quantity-Zugriff wird
**selbst geschrieben** über `IIfcObject.IsDefinedBy` →
`IIfcRelDefinesByProperties.RelatingPropertyDefinition.PropertySetDefinitions` (ein SELECT, das
eine **Menge** sein kann — die direkte Wandlung nach `IIfcPropertySet` verlöre ein
`IfcPropertySetDefinitionSet` und fiele bei Typmengen auf `IIfcElementQuantity` herein) → daraus
`IIfcPropertySet.HasProperties` bzw. `IIfcElementQuantity.Quantities`. Findet sich die Eigenschaft
dort nicht, wird der **TYP** gelesen (`IsTypedBy` → `RelatingType.HasPropertySets`) — dort tragen
Autorensysteme regelmäßig `Pset_WallCommon.ThermalTransmittance` und `Pset_DoorWindowGlazingType`.
Vorrang hat immer das Vorkommnis; der Beleg nennt, welche Quelle gegriffen hat. Die bequemen
`GetPropertySingleValue`/`GetElementQuantity` hängen an der IFC4-**Klasse**
`Xbim.Ifc4.Kernel.IfcObject` und wären schemagebunden.

**Der Mengensatz heißt in der Datei meist anders als in der Vorlage.** `Qto_…` ist der Name der
*Vorlage*; als Instanz trägt `IfcElementQuantity.Name` nach bSI-Festlegung `BaseQuantities`. Gesucht
wird deshalb über `IIfcElementQuantity.Name` ∈ { `BaseQuantities`, `Qto_<Klasse>BaseQuantities` }
(Groß-/Kleinschreibung egal), die Größe über `IIfcPhysicalSimpleQuantity.Name`. Welche Schreibweise
die KIT-Datei führt, wird an der Importprobe gemessen (3.7). Die Spalte „IFC-Quelle" unten nennt
jeweils die Vorlage.

| Zielfeld | IFC-Quelle | Regel | Rückfall | Herkunft |
|---|---|---|---|---|
| `WohnflaecheGesamt` | `IIfcSpace` + `Qto_SpaceBaseQuantities.NetFloorArea` | Σ über beheizte Räume | `GrossFloorArea` **für alle Räume gemeinsam oder für keinen** — eine gemischte Netto-/Bruttosumme wird nicht gebildet, sondern gemeldet (`IMP_IFC_PROT_FLAECHENART_GEMISCHT`); sonst leer → **Pflichtfeld** | Ifc / leer |
| `Raumhoehe` | `Qto_SpaceBaseQuantities.Height` | flächengewichtetes Mittel | `NetVolume / NetFloorArea`; sonst 2,5 m | Ifc / Vorgabe |
| (Volumen, nur Prüfgröße) | `…NetVolume` | gegen `Wohnfläche · Raumhöhe` halten | Abweichung > 20 % → Warnung | — |
| `FlaecheAussenwand` | `IIfcWall` + `Qto_WallBaseQuantities.GrossSideArea` | Σ über Wände mit `IsExternal = true`; **`GrossSideArea`, nicht `NetSideArea`** (Bruttomaß außen) | `Length × Height`; sonst leer | Ifc / leer |
| gesamte Fensterfläche | `IIfcWindow` + `Qto_WindowBaseQuantities.Area` | Σ über Fenster in Außenwänden | `Width × Height` | Ifc |
| `FensterflaecheNord/Sued/Ost/West` | Azimut des **Wirtsbauteils** | Sektor mit der nächsten Mitte in **IFC-Konvention** (N = 0°, O = 90°, S = 180°, W = 270°, im Uhrzeigersinn), Breite 90°; vor der Übergabe an den Eingangsbauer wird auf die **EPOS-Konvention gegen Süd** umgerechnet (`az_EPOS = az_IFC − 180`, auf (−180, 180] normiert; `KlimaImportAblauf.cs:135`) — ein Unit-Test hält beide Richtungen gegeneinander | ohne Azimut: gleichmäßig auf vier Sektoren, Warnung | Ifc / Vorgabe |
| `Dachflaeche` | `IIfcRoof` + `Qto_RoofBaseQuantities.GrossArea`; `IIfcSlab` mit `PredefinedType = ROOF` + `Qto_SlabBaseQuantities.GrossArea` | Σ; ist das Dach über `IIfcRelAggregates` aus Slabs zusammengesetzt, zählt **entweder** das Dach **oder** die Slabs | Grundfläche des obersten Geschosses | Ifc / Vorgabe |
| `Grundflaeche` | `IIfcSlab` (`BASESLAB`/`FLOOR`) im untersten Geschoss + `…GrossArea` | Σ | Wohnfläche / Geschosszahl | Ifc / Vorgabe |
| `SonstigeFlaechen` | `IIfcDoor` außen, `IIfcCurtainWall`, `IIfcPlate` außen | Σ | 0 | Ifc / Vorgabe |
| die fünf U-Werte | `Pset_WallCommon.ThermalTransmittance` usw.; gelesen wird `IIfcPropertySingleValue.NominalValue`, ein `IIfcPropertyBoundedValue` (so führt IFC4 `Pset_SpaceThermalLoad.AirExchangeRate`) über `SetPointValue`, sonst `UpperBoundValue`/`LowerBoundValue`, jede andere Eigenschaftsart benannt übergangen (`IMP_IFC_PROT_EIGENSCHAFTSART`) | **flächengewichtet je Gruppe:** `U = Σ(Uᵢ·Aᵢ)/ΣAᵢ`; fehlt er bei > 30 % der Gruppenfläche, gilt die Gruppe als „nicht aus IFC" | Vorgabe je Baualtersklasse | Ifc / Vorgabe |
| `Fensterdurchlassgrad` | `Pset_DoorWindowGlazingType.SolarHeatGainTransmittance` | flächengewichtet | Vorgabe je Klasse (0,75 alt / 0,6 / 0,5 neu) | Ifc / Vorgabe |
| `Bauweise` | `IIfcMaterialLayerSet` über `IIfcRelAssociatesMaterial` + `Pset_MaterialThermal`/`…Common` | raumseitige Schichten bis 10 cm: `C" = Σ ρᵢ·cpᵢ·dᵢ` [J/(m²K)], `/3600` → Wh/(m²K), `Bauweise = C"·Wohnfläche`; die Anzeige rastet über `Gebaeudebauweise.BauartAusBauweise` (`EPOS.Kern/Allgemein/Gebaeudebauweise.cs:42`) an den Schwellen **30 und 75** Wh/(m²K) ein (`:46-47`) — die Stufen 20/50/100 gehören zum Hinweg `BauweiseAusBauart` (`:63-65`). **Achtung:** `BauweiseNachfuehren()` (`GebaeudeKatalogDialog.razor:713-715`) schreibt vor jedem Speichern `Wohnfläche × 20/50/100` zurück und löscht jeden freien Wert; der Import setzt `Bauweise` deshalb erst, wenn der Editor den freien Zahlenweg hat (M-e, 2.11) — sonst nur die Bauart, Herkunft `Vorgabe`, und die Prüfregel „5 ≤ Bauweise/Wohnfläche ≤ 200" (Konzept 4.8) sähe über den Dialog nur 20, 50 oder 100 | ohne Schichten: Bauart „schwer" setzen und `BauweiseNachfuehren()` die Größe rechnen lassen (`Wohnfläche × 50`) — **nie** ein absoluter Wert (`Gebaeudebauweise.cs:66`). (Umgesetzt: Der Import setzt **nur die Bauart**, eingerastet über `Gebaeudebauweise.BauartAusBauweise`; der Editor rechnet die Bauweise im Modus Neu aus Nutzfläche und Bauart nach (`GebaeudeArbeitsstand.Laden`/`Ableiten`), der Wert aus den Schichten steht im Beleg. Die Zeilenangabe zu `BauweiseNachfuehren()` ist veraltet; Protokoll G4 Abschnitte 2 und 5) | Ifc / Vorgabe |
| `Baualtersklasse` | `Pset_BuildingCommon.YearOfConstruction` (**`IfcLabel`, also Text**) | erste vierstellige Zahl 1500…2100 aus dem Text („ca. 1965" → 1965), dann **A** vor 1919, **B** 1919–1948, **C** 1949–1957, **D** 1958–1968, **E** 1969–1978, **F** 1979–1983, **G** 1984–1994, **H** 1995–2000 (`EPOS.Kern/Controller/GebaeudeStammCtrl.cs:112-119`) | ab 2001 ist die Klasse ein **Standard** (I = Niedrigenergie … U = BEG 40), kein Jahr — Feld bleibt, der Anwender wählt | Ifc / manuell |
| `Baujahr` (neue Spalte) | dieselbe Quelle | die gezogene Jahreszahl, `INTEGER`, NULL = unbekannt (Schemaschritt `Baujahr` in Arbeit) | — | Ifc |
| `Grundflaeche_Randbedingung` | Geschoss bzw. Raum unter der Bodenplatte | `IIfcBuildingStorey` mit `Elevation < 0` **oder** ein `IIfcSpace`, dessen Name auf Keller deutet → `KELLER` | `ERDREICH` | Ifc / Vorgabe |
| drei ψ und drei Anschlusslängen | **nicht in IFC** | ψ steht in keinem Standard-Pset; Längen ohne Geometriekernel nicht ableitbar | Vorgabe je Klasse **oder** leer — Frage U15, **mit E38 (24.09.2026) nach Empfehlung entschieden:** ψ als Vorgabe je Klasse, Längen leer | Vorgabe / leer |
| `Luftwechselrate` | `Pset_SpaceThermalLoad.AirExchangeRate` | **nicht benutzen** — im Schema als `IfcPowerMeasure` typisiert (Schemafehler); der Zahlenwert ist nicht verlässlich zu deuten | Vorgabe 0,7 1/h (G2: 0,3 + 0,4) | Vorgabe |
| `Waermegewinne` | `Pset_SpaceOccupancyRequirements` | nur als **Vorschlag** angezeigt, nicht übernommen | Vorgabe **5 W/m² × Nutzfläche** für alle Gebäudearten, änderbar (E43) | Vorgabe |
| Sollwerte | `Pset_SpaceThermalRequirements` (in IFC 4.3 entfallen) | nur lesen, wenn vorhanden | Vorgabe Tag **20 °C**, Nachtabsenkung **18 °C** (höchstens der Tagsollwert), Nachtzeit **22 bis 6 Uhr**, alle änderbar (E43) | Ifc / Vorgabe |
| `Innenflaechenfaktor` (G4b) | Wände und Decken zwischen zwei beheizten Räumen (Raumbegrenzungen) | gemessene Innenfläche beider Seiten ÷ Nutzfläche — zweifach, wenn beide Räume zum Gebäude gehören, sonst nur die eigene Seite; Innentüren abgezogen; dieselbe Messung, mit der der Bauteilvorschlag seinen Innenweg wählt (E45, Konzept N1.49); außerhalb des Bands 1,0 … 5,0 übernommen, gelb, mit Beleg; mit Haken abwählbar, auch ohne Zone | ohne Innenflächen oder ohne Nutzfläche leer — es gilt die Vorgabe 2,5 | Ifc / leer |

**Umgesetzt (Protokoll G4 Abschnitte 3 und 5).** Die Rückfälle für Raumhöhe (2,5 m), Dach-, Grund-
und Sonstige Flächen gelten **nur im IFC-Profil** (`RueckfallRaumhoeheM`, `FlaechenRueckfaelle`,
`EPOS.Kern/Allgemein/Import/Ifc/IfcImportProfil.cs:58`, `:61`) — der gbXML-Weg zählt weiter „leer =
Wert des Gebäudes"; jeder Rückfall trägt Herkunft `Vorgabe` und Beleg, eine Teilsumme bleibt stehen
und wird gelb, und ohne jedes Bodenbauteil gilt auch die Randbedingung „Erdreich" als Vorgabe.
Zusätzlich wird der Mengensatzname `Qto_<Klasse>Quantities` angenommen, Fenster- und Türfläche haben
den dritten Rückfall `OverallWidth × OverallHeight`. Die Bauart aus Schichten nimmt λ und c aus
`Pset_MaterialThermal`, ρ aus `Pset_MaterialCommon` (Werte ≤ 0 als Fehlstelle,
`IMP_IFC_PROT_STOFFWERT_NULL`), die Schichtfolge aus `IfcMaterialLayerSetUsage` und der Raumseite;
ohne sie gilt eine Annahme mit Meldung `IMP_IFC_PROT_SCHICHTFOLGE_ANGENOMMEN`.
Die Baualtersklasse folgt dem Baujahr (A…H) nur, wenn der Anwender keine gewählt hat; die gewählte
steht als erstes Feld des Dialogs. Benannt offen: Steht an einem Dach nur ein U-Wert, aber keine
Fläche, nimmt die Gruppe die Vorgabe der Baualtersklasse statt dieses U-Werts.

**Einheiten.** Vor jeder Zahl steht der Faktor aus `IIfcUnitAssignment.Units`: bei `IIfcSIUnit` mit
`UnitType = LENGTHUNIT` entscheidet `Prefix` (`MILLI` → 0,001, `CENTI` → 0,01, ohne → 1,0). Für
`AREAUNIT` und `VOLUMEUNIT` gilt die **dort erklärte Einheit mit ihrem eigenen Prefix** —
unabhängig von `LENGTHUNIT`; ein fehlender Prefix heißt „ohne Prefix", nicht „wie die Länge". Nur
wenn der Typ ganz **fehlt**, wird er aus der Längeneinheit abgeleitet (Quadrat bzw. dritte Potenz),
und das wird gemeldet. Der Grund ist Praxis: Revit und Archicad erklären regelmäßig
`LENGTHUNIT = MILLI METRE` **und** `AREAUNIT = SQUARE_METRE` ohne Prefix — wer den Längenfaktor
quadriert, rechnet die Flächen um 10⁻⁶ falsch. `IIfcConversionBasedUnit` (Zoll, Fuß) trägt den
Faktor in `ConversionFactor` — im DACH-Raum selten, aber **benannt gemeldet** statt stillschweigend
als 1,0 genommen.

**Die Bemaßungsregel** (Außenbauteile nach Bruttomaß) gilt nach **E6 als Arbeitsannahme ohne
Normzitat**, bis sie in G1 aus VDI 2078 Abschnitt 6 oder DIN EN ISO 13789 belegt ist; aus
VDI 6020:2022 geht nichts in Code, Tests, Wiki oder Auslieferung.

**Azimut ohne Geometriekernel** ist reine Matrixmultiplikation: `IIfcProduct.ObjectPlacement` →
`IIfcLocalPlacement.RelativePlacement` (`IIfcAxis2Placement3D.RefDirection`, `Axis`) → über
`PlacementRelTo` aufwärts bis zum Weltsystem — sofern das Placement ein `IIfcLocalPlacement` ist;
`IIfcGridPlacement` und `IIfcLinearPlacement` werden benannt übergangen
(`IMP_IFC_PROT_PLATZIERUNGSART`), das Bauteil kommt in den Sammelposten ohne Azimut.

Die lokale x-Achse der Wand liegt nach Spezifikation in der Wandachse; senkrecht dazu in der
xy-Ebene liegen aber **zwei** Richtungen, und die Spezifikation legt nicht fest, welche außen ist
(`LayerSetDirection = AXIS2` ordnet nur die Schichten). Ein Vorzeichenfehler vertauscht N↔S. **Die
Seite bestimmt die Raumgrenze:** Die Außennormale zeigt von `IIfcRelSpaceBoundary.RelatingSpace`
weg, geprüft am Vorzeichen des Abstands zwischen Raum- und Wandplatzierung. Gibt es keine
Raumgrenze, bleibt der Azimut unbestimmt: der Leser meldet `IMP_IFC_PROT_SEITE_UNBESTIMMT` und
verteilt die Fensterfläche gleichmäßig.

Zum Schluss dreht `IIfcGeometricRepresentationContext.TrueNorth` (`IIfcDirection`, Vorgabe `[0,1]`)
das Ergebnis — **außer** wenn der Kontext eine `IIfcMapConversion` trägt
(`HasCoordinateOperation`): dann tritt deren Drehung `atan2(XAxisOrdinate, XAxisAbscissa)` an die
Stelle von `TrueNorth`, und `TrueNorth` bleibt unbeachtet (es wird **nicht** addiert). Gelesen wird
der Kontext mit `ContextType = 'Model'`.

### 3.5 Sonderfälle

| # | Fall | Regel in G4a |
|---|---|---|
| 1 | **Mehrere `IfcBuilding`** | ein EPOS-Gebäude je `IfcBuilding`; der Dialog zeigt eine Klappliste, übernommen wird je Lauf **eines**. Zuordnung über `IIfcRelAggregates` bzw. `IIfcRelContainedInSpatialStructure`; was sich keinem zuordnen lässt, kommt in einen Sammelposten mit Warnung. Frage U13, **mit E38 (24.09.2026) nach Empfehlung entschieden** |
| 2 | **Geschosse** | `IIfcBuildingStorey.Elevation` liefert nur Summenbildung und Reihenfolge — **relativ** gelesen, nie als absoluter Wert (drei Höhenbezüge sind optional) |
| 3 | **Unbeheizte Räume** | `Pset_SpaceCommon.IsExternal = true` schließt aus; sonst entscheidet der Name (Treffer auf Keller, Garage, Carport, Dachboden, Speicher, Abstellraum, Technik, Schacht, Aufzug — englisch Basement, Garage, Attic, Shaft, Plant; Groß-/Kleinschreibung egal). **Der Dialog zeigt die Raumliste mit dem Haken**, damit die Regel sichtbar und korrigierbar ist |
| 4 | **Archicad-Raumgrenzen** | die Dateien schreiben trotz IFC4 die **Basisklasse** `IfcRelSpaceBoundary` und tragen das Merkmal nur in `Name='2ndLevel'` / `Description='2a'` — der Leser prüft **beides**. Fehlt `IsExternal`, entscheidet `InternalOrExternalBoundary` der Raumgrenze (`EXTERNAL*` = außen, `EXTERNAL_EARTH` zugleich `Grundflaeche_Randbedingung = ERDREICH` ohne Namensraten); steht dort `NOTDEFINED`, gilt die Zählregel: außen, wenn **genau eine** Raumgrenze mit `PhysicalOrVirtualBoundary = PHYSICAL` auf sie zeigt |
| 5 | **Fehlende Quantities** | Meldung statt Rückfall auf Geometrie (3.1) |
| 6 | **Gedrehte Gebäude** | ohne `TrueNorth` gilt `[0,1]` (Norden = +y) und **der Dialog sagt das** (`IMP_IFC_PROT_KEIN_NORDEN`) — ein falsch genordetes Modell vertauscht die Fenstersektoren, und niemand sieht es an den Zahlen |
| 7 | **IFC2x3 gegen IFC4** | der Leser arbeitet über die `IIfc*`-Schnittstellen und verzweigt an genau **zwei** Stellen: 2nd-Level-Erkennung (Nr. 4) und das in 4.3 entfallene `Pset_SpaceThermalRequirements` |
| 8 | **`IfcZone`** | `RelatedObjects` erlaubt `IfcZone` und `IfcSpatialZone`; wer nicht entschachtelt, zählt Flächen doppelt. In G4a werden Zonen **gar nicht** gelesen — die Räume hängen am Geschoss, das genügt für eine Einzonenrechnung |
| 9 | **Mehrschalige Wände** | zwei Wände mit derselben Achslage und derselben Raumgrenze sind **eine** Wand; ohne Achsauswertung Gruppierung über die gemeinsame Raumgrenze, sonst Warnung (`IMP_IFC_PROT_MEHRSCHALIG`) |
| 10 | **Fensterabzug** | hier kreuzen sich Bruttomaß-Grundsatz und EPOS-Feldstruktur: `A_Wand = Σ GrossSideArea − Σ A_Fenster − Σ A_Außentür`; wird das negativ, greift `NetSideArea`, sonst Warnung und `A_Wand = 0`. Frage U14, **mit E38 (24.09.2026) nach Empfehlung entschieden** |

**Umgesetzt, mit benannten Abweichungen (Protokoll G4 Abschnitte 1, 3 und 5).** Ein Außenbauteil mit
mehreren anliegenden Räumen bekommt den ersten beheizten Raum als Nachbarn; ohne jede Angabe gilt ein
Dach als außen, eine Bodenplatte als erdberührt; die Außenplatte `FLOOR` ist Boden oder Decke nach
der Geschosslage. Nr. 7: Die Stoffwerte aus IFC2X3 sind über die Schnittstellen von xBIM 6.1.605
nicht verlässlich lesbar und werden benannt **nicht gelesen** (`IMP_IFC_PROT_STOFFWERTE_NICHT_GELESEN`);
U-Wert und Bauart dieser Schichten fallen auf die Vorgabe. Ein Lesen über die EXPRESS-Metadaten wäre
eine dritte Schemaverzweigung und ist nicht gebaut. Nr. 9: mehrschalige Wände nur mit Warnung.
Nr. 10: Wird die Nettofläche negativ, greift zuerst `NetSideArea`; ohne sie ist es wie beim
gbXML-Weg ein **Fehler** mit `A_Wand = 0`, nicht nur eine Warnung
(`EPOS.Kern/Allgemein/Import/Gebaeude/GebaeudeAggregation.cs:663`). Die Markierung „rot" sperrt im
Zuordnungsdialog nur bei negativer Nettofläche.

### 3.6 Plattform, Paket und Lizenz

**Das Paket.** Gemessen an den nuspec-Dateien (Abruf 15.09.2026):

| Paket 6.1.605 | Abhängigkeiten | Lizenz |
|---|---|---|
| `Xbim.IO.MemoryModel` | `Xbim.Common`, `Xbim.Ifc2x3`, `Xbim.Ifc4`, `Xbim.Ifc4x3` | CDDL-1.0 |
| `Xbim.Ifc` | zusätzlich **`Xbim.IO.Esent`** | CDDL-1.0 |
| `Xbim.Essentials` (Metapaket) | `Xbim.Common`, **`Xbim.Ifc`**, die drei Schemata, **`Xbim.IO.Esent`**, `Xbim.IO.MemoryModel` | CDDL-1.0 |

Alle tragen net10.0, net8.0 und netstandard2.0/2.1. **Der Kern nimmt genau eine Zeile** in
`Directory.Packages.props` (`<PackageVersion Include="Xbim.IO.MemoryModel" Version="6.1.605" />`)
und in `EPOS.Kern.csproj` nur `<PackageReference Include="Xbim.IO.MemoryModel" />`; die drei
Schemapakete kommen transitiv mit (`CentralPackageTransitivePinningEnabled` steht auf `false`,
`Directory.Packages.props:15`), `Microsoft.Extensions.Logging` ist bereits zentral geführt (`:30`)
und deckt den `ILoggerFactory`, den `MemoryModel` über `XbimServices` zieht. **`Xbim.Essentials` ist
damit nicht die richtige Zeile** — das ist die Präzisierung zu E3; die Klammer des Entscheids
(„`Xbim.Ifc2x3`, `Xbim.Ifc4`, `Xbim.Ifc4x3`, `Xbim.IO.MemoryModel`") trifft genau zu.

**Die Lizenzhinweisseite fehlt und muss entstehen.** `Setup/` führt keine Seite für
Fremdbibliotheken: `Setup/EPOS-Plan.iss:164-165` setzt `LicenseFile={#SetupDir}Lizenz.rtf`, `:330-331`
kopiert dieselbe Datei nach `{app}`. Für Datenlizenzen gibt es das Muster „Beipackzettel neben den
Daten" (`VDI-3805-Daten/Stromspeicher/LIESMICH_bslib.md`,
`VDI-3805-Daten/PV/LIESMICH_CEC_Inverters.md`). **Mit E27 entschieden (U10, 22.09.2026): eine
Lizenzhinweisseite mit der ersten IFC-Stufe (G4-1), für alle ausgelieferten Fremdanteile** — eine neue
`Setup/Vorlage/Lizenzhinweise.txt` mit je Fremdbibliothek Name, Version, Lizenz, Copyright-Vermerk
und **dauerhaftem Verweis auf den Quelltext** — erster Eintrag xBIM (CDDL-1.0,
`github.com/xBimTeam/XbimEssentials` bzw. die NuGet-Quellpakete; **CDDL § 3.1 verlangt, dass dieser
Verweis dem Empfänger mitgeteilt wird**), dazu die schon ausgelieferten Fremdanteile; eine
`Source:`-Zeile in `EPOS-Plan.iss` nach dem Muster `:330-331`; als Pflegeweg eine Zeile je
ausgelieferter `PackageVersion`. **Ohne diese Seite ist der IFC-Import nicht auslieferbar.**
Und: **nie forken, nie patchen** (E3) — dann gibt es nichts offenzulegen.

**iOS — drei Punkte.**

- **Dateifilter:** Nachzutragen sind `[".ifcxml"] = "public.xml"` und
  `[".ifczip"] = "public.zip-archive"` in `EPOS.iOS/Dienste/Dateifilter.cs:25-44` — beide Kennungen
  sind dort schon für `.xml` (`:27`) und `.zip` (`:30`) geführt. Für `.ifc` gibt es keine
  registrierte Typkennung; dort bleibt `public.data` (`:22`) die richtige Antwort — mit Kommentar
  wie im `.lic`-Fall (`:37-40`). Gewählt
  wird über `DateiOeffnenAsync` (`IDateiDienst.cs:107`, iOS-Fassung `IosDateiDienst.cs:146`, ohne
  Oberfläche `KeineDateiwahl.cs:17-20` → `""`); dass der Wähler **asynchron** sein muss, begründet
  `IDateiDienst.cs:74-105` (ein synchroner Wähler pumpt unter Windows eine verschachtelte
  Nachrichtenschleife, und auf iOS geht er vom Hauptfaden gar nicht erst auf).
- **Speicher:** `MemoryModel` hält das ganze Modell im Arbeitsspeicher; Faustzahl 10–20 MB je MB
  STEP-Text, 50 MB Datei wären also 0,5–1 GB — auf einem iPad zu viel. Vorschlag **50 MB Windows /
  20 MB iOS** über eine Eigenschaft von `IfcImportProfil`, die die Hülle je Plattform belegt. **Die
  iOS-Zahl ist geschätzt und in G4-8 zu messen.** Frage U11.
- **Trimming:** `EPOS.iOS.csproj` setzt weder `TrimMode` noch `PublishTrimmed`, und es gibt
  **keinen `TrimmerRootDescriptor`** im Repositorium. Der CI-Lauf baut für den **Simulator**
  (`.github/workflows/ios.yml:123-127`, Begründung `:122`: „Fuer den Rauchtest reicht JIT ohne
  Linker"), und dort wird nie getrimmt; **Gerätebauten** trimmen dagegen immer mit
  `TrimMode=partial`, unabhängig von der Konfiguration, und dabei bleiben die vier xBIM-Assemblies
  unangetastet, weil sie nicht als trimmbar markiert sind. Das Risiko ist benannt:
  `ExpressMetaData` ruft `module.GetTypes()`. Es wird deshalb **nichts** voreingestellt: **G4-8
  misst einen Gerätebau**; erst wenn `ExpressMetaData` dort Typen vermisst, kommt
  `EPOS.iOS/Pruefung/XbimRoots.xml` mit `preserve="all"` für die vier Assemblies dazu, eingebunden
  über `<TrimmerRootDescriptor …>`. Der Nachweis kostet einen macOS-Lauf (**zehnfaches
  Kontingent — beim Anwender zu erfragen**). Frage U16. **Mit E38 (24.09.2026,
  [Konzept N1.43](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)) festgelegt:** genau **ein**
  iOS-Lauf (`ios.yml`) als Trimming- und Gerätenachweis für G4a, ausschließlich nach ausdrücklicher
  Rückfrage beim Anwender zum Zeitpunkt der Abnahme von G4a; sonst keine iOS- oder macOS-Läufe.

**Umgesetzt (Protokoll G4 Abschnitte 3, 5 und 6).** **Paket:** eine Zeile `Xbim.IO.MemoryModel`
6.1.605, `PackageReference` nur am Kern; transitiv allein `Xbim.Common`, die drei Schemapakete und
drei `Microsoft.Extensions`-Pakete — kein Esent, kein Geometriekern; die fünf xBIM-Assemblies sind in
der Ausgabe der Schale zusammen rund 11,1 MB groß, der Kern baut ohne Windows-Bindung. **Lizenzseite:**
`Setup/Vorlage/Lizenzhinweise.txt` für alle ausgelieferten Fremdbibliotheken; `Setup/EPOS-Plan.iss`
legt sie nach `{app}` und bricht ohne sie ab; die Wache `LizenzhinweiseWacheTests` hält jedes direkt
referenzierte Paket der ausgelieferten Projekte auf der Seite. **iOS:** die Dateifilter `.ifcxml`,
`.ifczip` und zusätzlich `.gbxml` stehen in `EPOS.iOS/Dienste/Dateifilter.cs:50-52`; die Grenze
50 MB / 20 MB belegt `IfcImportProfil` über `GrenzeFuerPlattform` (nachgetragen, nachdem die Hülle
unter iOS sonst 50 MB belegt hätte). Offen bleiben die gemessene iOS-Grenze, der Gerätebau zum
Trimming-Nachweis und die Prüfung der Schale — alle im einen iOS-Lauf zur Abnahme von G4a, nur nach
Rückfrage (E38).

### 3.7 Tests und Importprobe

**Wie Importproben organisiert sind:** `Referenzlaeufe/Importproben/` (27 Dateien, rund 300 KB,
gewöhnliche Blobs, **kein** LFS); der Ordner „gehört zum Testbestand und wird nie gelöscht"
(`Referenzlaeufe/LIESMICH.md:128-129`). Eine LIESMICH im Ordner selbst gibt es **nicht** — die
Quellenvermerke stehen in den Fachkonzepten. Tests suchen den Ordner **aufwärts vom Laufordner**
(`EPOS.Kern.Tests/KatalogImportTests.cs:50-68`).

**Vorschlag:** `Referenzlaeufe/Importproben/AC20-FZK-Haus.ifc` (KIT/IAI, 2,5 MB, IFC4/Archicad 20)
aufnehmen — der Ordner wächst um den Faktor 9, bleibt unter 3 MB und braucht kein LFS — und eine
**neue** `LIESMICH_Importproben.md` mit den Quellenvermerken aller Proben anlegen; für die KIT-Datei
im vorgegebenen Wortlaut „Institut für Automation und angewandte Informatik (IAI) / Karlsruher
Institut für Technologie (KIT)" (Nutzung uneingeschränkt, Namensnennung für Veröffentlichungen
vorgeschrieben). RWTH- und bim2sim-Dateien **nicht** aufnehmen (Konzept Q12).

**Erwartete Werte der Importprobe** (beim Bau des Tests an der Datei nachzumessen; hier stehen die
belegten): Schema `XbimSchemaVersion.Ifc4`; **1** `IIfcBuilding`; **8** `IIfcSpace`; **81**
Raumgrenzen, sämtlich als **Basisklasse** mit `Name='2ndLevel'` — der Test prüft ausdrücklich, dass
der Leser sie **nicht** über den Entity-Typ sucht; **33** `ThermalTransmittance`; 7 Vorkommen
`Pset_SpaceThermalRequirements`; Längeneinheit aufgelöst; Herkunft je Zeile (Wohnfläche, Raumhöhe,
Außenwandfläche und U-Werte `Ifc`, Wärmebrücken und Luftwechsel `Vorgabe`).

**Vierzehn Unit-Tests ohne Datei** (`EPOS.Kern.Tests`, keine Testdatenbank) — Liste in Befund N,
Abschnitt 4.6; die tragenden: die Sektorzuordnung trifft die vier Mitten (Grenze festgelegt:
**aufsteigend zum größeren Sektor**), der Azimut dreht mit `TrueNorth`, bei `MapConversion` wird
`TrueNorth` **nicht** addiert, der U-Wert einer Gruppe ist flächengewichtet (10 m²/0,5 und 30 m²/1,5
ergeben 1,25, nicht 1,0), das Baujahr wird aus Text gelesen („ca. 1965" → 1965, „Altbau" → keins),
die Baualtersklasse folgt dem Baujahr, die Fensterfläche wird von der Wandfläche abgezogen
(100 − 15 − 2 = 83), das Größenlimit greift, ein Abbruch wirft `OperationCanceled`. Dazu die
bunit-Fälle in `IfcZuordnungDialogTests.cs`: Abbrechen liefert `false` und ruft `Uebernehmen` nie;
ein abgehakter Haken hält die Zeile aus dem Satz; die Herkunftstexte kommen aus
`IfcZuordnungsModell`, nicht aus der Komponente.

**Umgesetzt (Protokoll G4 Abschnitte 1 bis 3, 5 und 6).** Die KIT-Datei `AC20-FZK-Haus.ifc` ist
**nicht** aufgenommen — sie herunterzuladen und aufzunehmen entscheidet der Anwender; die erwarteten
Werte oben sind deshalb noch nicht an der Datei gemessen. Statt ihrer liegen **zwölf selbst erzeugte
IFC-Proben** (IFC4- und IFC2X3-Haus, `.ifczip`, IFC4X1-Kopf, zwei Gebäude, ohne Mengen,
MapConversion, Entitätenverlust, Schichten, Nullwerte, Rückfälle) und **elf selbst erzeugte
gbXML-Proben** unter `Referenzlaeufe/Importproben/`, alle mit Zeile in der neuen
`LIESMICH_Importproben.md`. Die vierzehn Unit-Tests ohne Datei gehören zu den Fällen in
`IfcImportTests` und `IfcProbenTests` (Befund N 4.6 enthält beim TrueNorth-Beispiel einen
Vorzeichenfehler; der Test prüft richtig), dazu `IfcImportWelle2Tests`; die bunit-Fälle stehen in `GebaeudeImportDialogTests`
und `GebaeudeDialogImportTests` (statt `IfcZuordnungDialogTests`), die Herkunftstexte kommen aus
`GebaeudeZuordnungsModell`. Rundlauf und XSD-Prüfung (Datenaustauschkonzept, Proben 1 bis 3) gehören
zum Export G7, der nicht gebaut ist.

**Abnahme G4a:** Kern-Filter grün und Importprobe bestanden; **Referenzlauf unverändert** (der
Import schreibt nur auf Zuruf des Anwenders — und der Schemaschritt für `Baujahr` ist
ergebnisneutral); Windows-Sichtabnahme (Datei wählen, Zuordnung prüfen, OK, Werte im Katalogeditor);
iOS-Lauf — nach E38 **genau einer**, ausschließlich nach ausdrücklicher Rückfrage beim Anwender zum
Zeitpunkt dieser Abnahme — mit Release-Bau, Trimming-Nachweis und der KIT-Datei im Prüfmodus;
`SqlDialektPruefer` nur, wenn der Schemaschritt dazukommt. (Stand: Kern-Filter grün, die
selbst erzeugten Proben bestanden, Referenzlauf 13/13 byte-gleich; offen die Windows-Sichtabnahme,
der eine iOS-Lauf nach Rückfrage — die KIT-Datei für den Prüfmodus fehlt, solange ihre Aufnahme
nicht entschieden ist — und der Schemaschritt `Baujahr`, in Arbeit.)

### 3.8 Aufwand und Reihenfolge innerhalb G4

| Teil | Inhalt | Aufwand |
|---|---|---|
| **G4-1** | Paketzeile, `EPOS.Kern.csproj`, Lizenzhinweisseite im Setup, Bau auf ubuntu und Windows grün | 0,5–1 PT |
| **G4-2** | Abbild, Profil, Einheitenauflösung, Schemaerkennung, Öffnen, Räume und Bauteile sammeln — **ohne** Zuordnung | 3–4 PT |
| **G4-3** | Azimut aus der Placement-Kette, `TrueNorth`, `MapConversion`; Tests ohne Datei | 2–3 PT |
| **G4-4** | Zuordnung nach E2: U·A-Zeilen, Sektoren, Fensterabzug, Bauweise aus Schichten, Baujahr → Klasse; Satz, Modell, Plausibilität | 3–4 PT |
| **G4-5** | Vorgabetabelle je Baualtersklasse (21 Klassen × 5 U-Werte + g), abgeleitet aus dem eigenen EPOS-Gebäudekatalog (U12, mit E27 entschieden) | 1–2 PT |
| **G4-6** | Dialog + DTO + `IfcImportHuelle`, Knopf in beiden Gebäudedialogen, Texte in beiden `.resx`, `ResourceDesigner` (umgesetzt: `GebaeudeImportDialog` + `GebaeudeImportHuelle`, **ein** Knopf im Gebäudedialog für beide Formate) | 2–3 PT |
| **G4-7** | Importprobe, Quellenvermerk, Unit-Tests, Dialogtests | 1–2 PT |
| **G4-8** | iOS: Dateifilter, Größenlimit **gemessen**, **Gerätebau** zum Trimming-Nachweis (erst danach ein `TrimmerRootDescriptor`, wenn er gebraucht wird), **genau ein** Lauf (`ios.yml`, E38), ausschließlich nach ausdrücklicher Rückfrage beim Anwender zum Zeitpunkt der Abnahme von G4a | 1–2 PT |
| | **Summe G4a** | **13,5–21 PT** |
| **G4b** | Anbindung an `Tab_Zone`, `Tab_Bauteil`, `Tab_Bauteilaufbau` und `Tab_Bauteilschicht` aus G3, für IFC und gbXML: auf Wunsch **eine** Zone, je Bauteil eine Zeile mit Nettofläche, Azimut samt Nordwinkel, Neigung, Randbedingung, U, g, Herkunft `IFC`/`GBXML` und Quellkennung, die Aufbauten samt Schichten als Projektkopien — statt Nachmultiplikation. **Gebaut 25.09.2026** (E44, E45; Konzept N1.49) | +4–6 PT |
| **G4c** | **gbXML-Import** (Pflicht nach E9): Lesemodell und Einheiten, Aggregation auf das Zonenmodell, Zuordnungsdialog, Beispieldateien — LINQ to XML, Versionswert `6.01` (Befund R) | **17–28 PT** — die Zahl führt das [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md), Kapitel 10, seit die Persistenz der Zuordnung (Schemaschritt S-F) dorthin vorgezogen ist |

Reihenfolge G4-1 → G4-2 → G4-3 → G4-4 → G4-5 → G4-6 → G4-7 → G4-8; G4-3 **vor** G4-4, weil die
Sektorzuordnung am Azimut hängt. G4-5 kann parallel laufen, sobald die Zielfelder stehen. G4b setzt
G3 voraus; mit E44 ist es vor der Feldphase von G4a gebaut (Konzept N1.49). Der Schemaschritt für `Baujahr` bekommt die nächste freie
Nummer nach G1/G2 (Stand 22.09.2026: `SchemaStand.Zielversion` 100, nächste freie 101;
`SchemaStand.cs:341`).

**Umsetzungsstand 25.09.2026 (Protokoll G4).** G4-1 bis G4-7 sind gebaut, aus G4-8 die iOS-Dateifilter;
G4c ist gebaut samt Schemaschritt 138 (S-F); beide Wege sind im Gebäudedialog angebunden, der
Referenzlauf ist 13/13 byte-gleich. Offen: die Windows-Sichtabnahme, aus G4-8 die gemessene
iOS-Grenze, der Gerätebau zum Trimming-Nachweis und der eine Lauf nach Rückfrage (E38), der
Schemaschritt `Baujahr` (in Arbeit). **G4b ist gebaut** (25.09.2026, E44,
[Protokoll G4b](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G4b_Bauteilimport.md)): auf Wunsch
eine Zone mit Bauteilen, Aufbauten und Schichten, gerechnet über den Bauteilweg, kein Schemaschritt,
Referenzlauf 14/14 unverändert; offen die Windows-Sichtabnahme.

**Vorbedingung von außen:** Der Schreibweg des Gebäudedialogs liegt zu Beginn von G4a schon
plattformfrei in `EPOS.UI.Daten` — das erledigt M-k im Dialogumbau (2.8, 2.11). Ohne diesen Umzug
ist der IFC-Import auf iOS benannt abzulehnen (3.3).

**Die Reihenfolge von G4a und G4c ist entschieden: der gbXML-Import (G4c) kommt vor dem
IFC-Import (G4a)** — Anwenderfrage **D1** des
[Datenaustauschkonzepts](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md), mit E27 (22.09.2026,
[Konzept N1.32](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)) nach der Empfehlung aus
Befund R entschieden (einfacheres Format, geringerer Aufwand, dieselbe Zielstruktur). Die
Reihenfolge G4-1 … G4-8 **innerhalb** von G4a bleibt davon unberührt. Die beiden **Exporte** nach gbXML und IFC sind nach E9 Stufe **G7** und stehen
ebenfalls in jenem Papier.

**Beauftragt mit E38 (24.09.2026, [Konzept N1.43](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)):**
die Stufe G4 — zuerst **G4c** (gbXML), dann **G4a** (IFC); **G4b** erst nach G3 und nachdem G4a im
Feld war. U13, U14 und U15 gelten für beide Importwege; für G4c gibt es keinen iOS-Lauf, für G4a
genau einen (G4-8). Mit **E44** (25.09.2026, [Konzept N1.49](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md))
ist G4b vor der Feldphase von G4a gebaut, mit den Rechenregeln aus **E45**.

---

## 4. Reihenfolge und Abnahme je Stufe

| Stufe | Inhalt | Abnahme | Aufwand |
|---|---|---|---|
| **G0 — Löser** | `Zonenmodell2K`, `ErsatzparameterRC`, Diskretisierung, ideale Regelung; Normfälle und Rechenproben (1.3, 1.9); Testfall 11 lösen, Testfall 6 klären, α_kon je Bauteil, Band-Prüfregel, Vorlaufkonvergenz. **Keine Datenbank, kein Aufrufer** | Kern-Filter grün; **elf der zwölf Normtestfälle** im Band ± 0,15 K / ± 1,5 W nach E10 (Druckrundung), Testfall 11 in zwei Umschaltstunden um 3,4 W daneben (3,9 W gegen das Band ohne Druckrundung) — **lokal**, in der CI schweigend (1.9); Rechenzeit je Gebäude und Jahr neu gemessen und Abnahmekriterium (4) aus Konzept 10.4 neu bestimmt (F-S6, 1.3). Referenzlauf **unberührt**, weil keine Zeile des Bestandswegs angefasst wird | 2–4 PT |
| **GB — Bestandsbefunde** | `_prevRoomTemp` als Instanzzustand (`BhkwPlan.cs:51`, `:433`), Warnungen statt stiller Fehlgriffe in der Ferienmaske (`SimulationWaermebedarf.cs:689-733`) **und die nicht nachgeführte `Ferien_Absenkung` der Jahresschleife** (`:845-850`, Befund X 3.4), `Bauweise` 10576 auf 15 200 Wh/K, **vierte Einfrierregel** in `Referenzlaeufe/LIESMICH.md` und `CLAUDE.md`; dazu die 100-Gebäude-Grenze (U9, E28). **GB läuft vor der Verschiebung** (E20): Das verschobene Modul soll der geprüfte Stand sein | **1008 ändert sich** (geplant waren 1008 und 1039; 1039 bleibt byte-gleich, siehe 1.8 und Statusdatei) — eigener Einfrierschritt: Lauf, Vergleich, Begründung, grüner CI-Lauf. Diese Basis ist die **letzte reine Bestandsbasis** | 1–2 PT |
| **M2 — Umbenennung** | `Fensterflaeche_Ost` → `Fensterflaeche_OstWest`, 15 Stellen (`GebaeudeModel.cs:20`/`:76`, `ProjektGebaeudeModel.cs:26`/`:88`, `GebaeudeCtrl.cs:66`, `GebaeudeStammCtrl.cs:291`/`:358`, `ProjektGebaeudeCtrl.cs:56`, `SimulationWaermebedarf.cs:752`/`:756`/`:822`/`:826`, `GebaeudeKatalogHuelle.cs:364`/`:443`/`:453`) | gegen die GB-Basis **byte-gleich**. Eigener Merge, weil jede dieser Zeilen in `SolareGewinneC` mündet | 0,5 PT |
| **M3 — Schema** | der Gebäudespalten-Schritt M3 (die drei G2-Spalten verschmolzen, U5; Nummer bei Beauftragung, F-S1), Umbenennung `Wohnflaeche` → `Nutzflaeche` zuerst (E19, F-S2), `GebaeudeSchema.cs`, Sichtneubau, Namensleser statt `row[0…57]` — der Leser und `SQL_VIEW_NEU` schreiben die acht nicht-ASCII-Bezeichner **buchstabengetreu** (`k_Wert_Außenwand`, `Flaeche_Außenwand`, `WBVK_Anschluß_*`, `Abmessung_Anschluß_*`), Umlautregel [`BETRIEB_SQLITE.md`](BETRIEB_SQLITE.md) § 6.1, und der Feldbestandstest prüft sie namentlich; `DbWerte`, Katalogkopie NULL-erhaltend, `TestDatenbank` nachziehen | gegen die GB-Basis **byte-gleich** (Spalten bleiben NULL, kein Leser rechnet damit). Die Probe ist der Sichtneubau: `ProjektGebaeudeCtrl` liefert alle 58 Bestandsfelder unverändert; dazu `python3 Werkzeuge/SqlDialektPruefer/pruefer.py --db Referenzlaeufe/Kenndaten_Test.sqlite` grün | 1,5–2 PT |
| **M4 — Klimaspalten** (**umgesetzt**, durch Schemaschritt 95 vom 19.09.2026 vorweggenommen) | eigener Schritt (1.7): `Gegenstrahlung`, `Luftfeuchte`, `Bedeckungsgrad` in `Tab_Solar(_STAMM)`, `Quelle` und `Importdatum` in `Tab_Klimaregion(_STAMM)`, mit Schritt 97 `Szenario` und `Bezugsjahr`; keine `Windgeschwindigkeit` (F-S3); `TmyHourlyData`, `SaveTmyData`, `SolardatenCtrl`, `DwdTryLeser`; Quellen und Bedienweg im [Konzept Klimadatenquellen](Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md) | erbracht: kein Datenteil, alle Spalten im Bestand NULL, kein Rechenweg liest sie — Referenzlauf **byte-gleich**; Nachweise `KlimaspaltenTests` und `KlimaSzenarioTests` (Konzept Klimadatenquellen Kapitel 11) | **erbracht — in keiner Summe** |
| **G1.0 — Trennung der Rechenwege** (erster Schritt von G1, E20) | Modul `Simulation/Altweg/` anlegen und den Tagesbilanz-Weg **Zeichen für Zeichen** hineinschieben (drei Methoden aus `SimulationWaermebedarf.cs`, vier Physikfunktionen aus `BhkwPlan.cs` samt Vortemperatur; `BhkwPlan.cs` bleibt als Datei, seine neun Vektorhelfer wandern nicht); Fassade und Weiche in `HeizwaermeEinesGebaeudes`, **modellfreier Vorbereitungsschritt**, `IGebaeudeRechenweg`, `Modultrennungswache` (1.9); `BhkwPlanRueckgabeTests` nachziehen | gegen die GB-Basis **byte-gleich** — die Verschiebung ist ergebnisneutral, **kein Einfrieranlass**. Eine Abweichung ist ein Fehler der Verschiebung; nach der Anbindung des VDI-Wegs wäre sie nicht mehr von der Modellwirkung zu trennen. Kern-Filter grün, `SqlDialektPruefer` unberührt | **3–5 PT** |
| **G1 + G2 — das Modell und seine Darstellung** | `GebaeudeModellEingang`, `GebaeudeModellErgebnis`, das Modul `Simulation/Gebaeude/` hinter der Weiche (G1.0), Vorlauf 30 Tage, Plausibilitätsprüfungen, Ergebnisexport; Dialogumbau in VDI-Struktur (Kapitel 2, **9,0 PT** einschließlich Bestandswegabschnitt), Hülle nach `EPOS.UI.Daten`, 63 Texte; Raumtemperatur-Bild, Kühlbedarf, drei Spitzenwerte, Vergleich im Bedarfsdialog, Sommerlüftung und Infiltration/Nutzerlüftung, Wiki-Seite | **alle dreizehn Referenzprojekte ändern sich** (+7 bis +33 %), drei neue CSV je VDI-Gebäude — **Basis vollständig neu einfrieren** (E1/Q14). Dazu der **Rückweg-Test** (F-Ü7): bis zu diesem Merge auf einer **Arbeitskopie** (`Referenzlaeufe/Arbeitskopie/`, gitignoriert), die `Gebaeude_Modell = 'TAGESBILANZ'` setzt, gegen die GB-Basis — die eingefrorene Testdatenbank bleibt unberührt, der Schalter dafür ist ein eigener Modus des Referenzlaufs; **ab dem Einfrieren genau ein Referenzprojekt auf dem Altweg in der neuen Basis** (A15, mit E27 entschieden), das **bis zur Stufe GA** in der jeweils aktuellen Basis steht; der Rückweg-Test rechnet nur dieses Projekt. `ChartProben` grün; Sichtabnahme Windows; Kriterien Konzept 10.4 (2) und (3), (4) in der in G0 neu bestimmten Fassung (F-S6); **Strahlungsweg: VDI 2078 Testbeispiel 7.1/7.2 (Typ 2, ± 0,2 °C / ± 5 W), aus den gedruckten Klimaparametern Anhang A1/B1 ohne TRY** (Konzept N1.9); **Logbuch-Eintrag entworfen, Versionsnummer beim Anwender erfragt**, Wiki-Seite „Gebäudemodell VDI 6007" und die geänderte Seite „Gebäude" im nächsten gebündelten Upload | 16–22 PT |
| **G3 — Bauteilkatalog** | `Tab_Baustoff_STAMM`, `Tab_Bauteil`, `Tab_Bauteilschicht`, Baustoffdialog samt **Zeile in `EPOS.UI/Bausteine/Menuetabelle.cs`** unter Administration (das Menü ist Daten; kein Untermenü mit nur einem Punkt), Bauteilweg mit Kettenmatrix-Reduktion, geneigte Fenster, echte Hülle statt Nachmultiplikation. **Gebaut:** alle acht Tabellen in den Schritten 132–134, die Verwaltungen „Baustoffe" und „Bauteilaufbauten" gemeinsam unter Administration › Gebäude, Zonen- und Bauteildialog im Gebäudedialog, die Übernahme als eine Zone mit Hochrechnung (E40) | Reduktion trifft die Normwerte der Testräume; Bauteilweg = Klassenweg im Grenzfall gleicher U und C. **Erfüllt (25.09.2026):** die Reduktion trifft die Parameter der Testräume in 12 von 12 Testbeispielen relativ ≤ 10⁻³, die Normfälle damit 11 von 12 im Band; Grenzfall bis 4,7·10⁻¹⁶ relativ (15 Testgebäude), im Jahreslauf 1,3·10⁻¹⁴; Referenzlauf 13/13 unverändert ([Protokoll G3](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G3_Bauteilkatalog.md)) | 8–12 PT |
| **G4a — IFC-Import** | Kapitel 3: Paket und Lizenzseite, Leser, Azimut, Zuordnung, Vorgabetabelle, Dialog und Hülle, Importprobe, iOS | Importprobe bestanden; **Referenzlauf unverändert**; Windows-Sichtabnahme; genau ein iOS-Lauf, ausschließlich nach ausdrücklicher Rückfrage bei der Abnahme (E38). **Stand 25.09.2026:** gebaut und im Gebäudedialog angebunden, selbst erzeugte Proben bestanden, Referenzlauf 13/13 byte-gleich; offen die Windows-Sichtabnahme, der eine iOS-Lauf nach Rückfrage und der Schemaschritt `Baujahr` (in Arbeit) (3.8, [Protokoll G4](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-24_G4_Importe.md)) | 13,5–21 PT |
| **G4b — IFC und gbXML auf Bauteilebene** | auf Wunsch **eine** Zone mit je Bauteil einer Zeile in `Tab_Bauteil` (Nettofläche, Azimut samt Nordwinkel, Neigung, Randbedingung, U, g, Herkunft `IFC`/`GBXML`, Quellkennung) und den Aufbauten samt Schichten als Projektkopien; innere Masse nach Datenlage, U-Wert leer neben vollständigen Schichten, Vorhangfassaden transparent (E45). **Gebaut:** Schalter „Als Zone mit Bauteilen übernehmen“ im Zuordnungsdialog, Schreibweg in einem Vorgang samt Herkunft, Zielfeld Innenflächenfaktor (3.4) | nach G3; mit **E44** vor der Feldphase von G4a. Importproben bestanden; **Referenzlauf unverändert**; Windows-Sichtabnahme. **Erfüllt (25.09.2026):** Durchgang über die Datenbank (eine Zone mit 34 Bauteilen, Bauteilweg im Lauf), Referenzlauf 14/14 unverändert gegen R16; offen die Windows-Sichtabnahme ([Protokoll G4b](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G4b_Bauteilimport.md), [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.49) | 4–6 PT |
| **G4c — gbXML-Import** | **Pflicht nach E9** (Konzept N1.13): Lesemodell und Einheiten, Aggregation auf das Zonenmodell, Zuordnungsdialog mit Herkunft je Feld, Beispieldateien; LINQ to XML statt `XmlSerializer`, Versionswert `6.01` | Importprobe bestanden; **Referenzlauf unverändert**; **vor G4a** (D1, mit E27 entschieden; 3.8). **Stand 25.09.2026:** gebaut und im Gebäudedialog angebunden, samt Schemaschritt 138 (S-F); elf selbst erzeugte Proben bestanden, Referenzlauf 13/13 byte-gleich; offen die Windows-Sichtabnahme | **17–28 PT** nach [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md), Kapitel 10 (einschließlich der dorthin vorgezogenen Persistenz der Zuordnung) |
| **G5 — Geometrieableitung** | eigene Auswertung von `IfcExtrudedAreaSolid` und Placement-Kette, Öffnungsabzug | nur bei Bedarf aus der Praxis | 30–60 PT |
| **G7 — Exporte** | gbXML- und IFC-Export (E9) — eigenes Papier: [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md). Dort steht auch der **Gebäudebetrachter (E11)**: ein Zonengeometrie-Modell im Kern, 2D-Grundriss je Geschoss mit G6c, schematische Körper mit G7b (Nachtrag 1; Konzept N1.16) | dort | dort |
| **GA — Altweg ablösen** (letzte Stufe; beauftragbar und fällig, sobald das Ablösekriterium Q24 erfüllt ist — E27) | Modul `Simulation/Altweg/`, Weiche, `IGebaeudeRechenweg`, `Modultrennungswache`, Schalter „Rechenweg", Abschnitt „Tagesbilanz (Bestandsweg)", Spalte „Rechenweg" im Wirt, Vergleich alt/neu samt `modellErzwungen`, Ausweis „Tagesbilanz (Bestandsweg)", Kältebedarf-0-Hinweis, AK-Sonderfall „feste Last", Spalte `Gebaeude_Modell` und die nur vom Altweg gelesenen Spalten (`DROP COLUMN` je Tabelle, Sichtneubau; davor `Fensterflaeche_Ost`/`_West` einmalig füllen), `Tab_DBTagV`/`Tab_DBTagVDaten`, Schreibstellen der Flags `Wochenende`/`Ferien`, Referenzprojekt auf VDI 6007 umstellen, Rückweg-Test einstellen — **vollständige Ablösung nach der Löschliste in Kapitel 6** (Q25, mit E27 entschieden; Quellen Konzept N1.25 Punkt 4, N1.31, Befund X Kapitel 4) | **Ausbauprobe** grün: ein Bau mit umbenanntem Ordner `Altweg/` übersetzt nach Entfernen der Weiche, und der Referenzlauf aller Projekte ohne Altweg-Gebäude bleibt byte-gleich; **eigener, begründeter Einfrierschritt**; Logbuch-Eintrag | **5–8 PT** — in keiner Summe; Ablösekriterium Q24 (Kapitel 5), geprüft mit jeder Abnahme, Stand in der Statusdatei |

**Summen:** G0 + GB 3–6 PT; **G1.0 (Trennung) 3–5 PT**; bis einschließlich G1+G2
**24–35,5 PT**; bis G3 32–47,5 PT; bis G4a 45,5–68,5 PT — ohne M4, das mit Schemaschritt 95
erbracht ist. **Die 5–8 PT der Stufe GA stecken in
keiner Summe** — sie wird fällig, sobald das Ablösekriterium Q24 erfüllt ist (E26, E27). Der gbXML-Import (G4c) kommt
mit **17–28 PT** hinzu — **er steckt in keiner der Summen oben**. Diese Zahl und die Exporte (G7)
rechnet das [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) in Kapitel 10;
sie schließt die Persistenz der Zuordnung ein (Schemaschritt S-F, zwei Tabellen), die dort zu G4c
gehört.

**Warum das über dem Konzeptrahmen liegt.** Konzept 11 veranschlagt G1 mit 10–16 PT (darin die
3–5 PT der Trennung nach E20) und G2 mit 3–5 PT. Allein die Oberfläche ist gemessen 9,0 PT (2.11),
und dazu kommen drei Dinge, die das Konzept in dieser Form nicht kannte: der erste Sichtneubau des
SQLite-Zweigs samt Namensleser (1.6), die NULL-erhaltende Katalogkopie (1.6) und die Vorrichtung
für nicht ausgelieferte Normzahlen (1.9). **Dieses Kapitel ist die Quelle der verbindlichen
Aufwände; Konzept 11 und 12 verweisen darauf** (F-S8). Die Aufwände sind Größenordnungen für
Entwicklung **und Nachweis**; Agentenarbeit verkürzt die Kalenderzeit, nicht die Prüfzeit.

**Was zwischen GB und G1+G2 gilt:** jeder Merge läuft gegen die GB-Basis und muss `GESAMT: PASS`
melden — **G1.0 ausdrücklich byte-gleich**. **In diesem Papier frieren nur GB und G1+G2 neu ein**; die
weiteren Anlässe der Kette bis GA stehen in 1.8. Wer M3 und G1
zusammenlegt, kann hinterher nicht mehr sagen, ob eine Abweichung vom Schema oder vom Modell kommt —
genau die Trennung, die E4 für GB begründet; dasselbe gilt für G1.0 und die Anbindung des VDI-Wegs.

**Drei Handgriffe, die zur Papierführung gehören:** **Dieses Papier und jeder Befund** brauchen je
eine Indexzeile in [`Dokumentation/LIESMICH.md`](../LIESMICH.md), sonst ist
`DokumentationLinkWacheTests.Der_Index_nennt_jedes_Papier` (`:170`) rot — dieses Papier steht dort
auf `:55`, die Befunde A–S auf `:56-74`; jedes weitere Papier zieht seine Zeile nach — **Befund X
(E20) ebenso wie Befund W**. **Je Stufe** kommt
eine Zeile in [`Status_Gebaeudesimulation_VDI6007.md`](Status_Gebaeudesimulation_VDI6007.md) dazu,
der ausführliche Block dazu als Protokoll unter `Dokumentation/ueberholt/Protokolle/`. Und
`EinheitenWacheTests.Simulationsklassen` (`:201-206`) bekommt den Eintrag
`Gebaeude/GebaeudeModellErgebnis.cs` — **mit Unterordner** (1.4, 1.9) — in demselben Merge, in dem
die Klasse entsteht.

---

## 5. Fragen mit Empfehlung

Nur **neue** Fragen; Q1–Q23 des Konzepts sind dort beantwortet oder durch **E1–E26** entschieden
(E9 Austauschformate, E10 Druckrundung, E19 Nutzfläche, E20 Trennung der Rechenwege, E21
Kältebedarf, E26 Altweg als Übergang). **Q24** — wann der VDI-Weg bewährt genug ist, dass die
Stufe GA beauftragt wird — und **Q25** — ihr Umfang — waren mit E26 wieder offen (Konzept N1.31)
und sind mit **E27** (22.09.2026, [Konzept N1.32](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)) **entschieden**. Zu Q24 gilt das
Ablösekriterium: **GA wird beauftragbar und fällig, sobald alle vier Bedingungen erfüllt sind** —
(1) alle Referenz- und Bestandsprojekte des Anwenders sind einmal auf VDI 6007 gerechnet und die
Abweichung zum Altweg ist je Projekt erklärt; (2) eine Feldphase von mindestens einer Heizperiode
ohne offenen Fehler am VDI-Weg; (3) KU1 und, falls beauftragt, AK1 sind abgenommen; (4) die
Ausbauprobe ist grün. Geprüft wird mit jeder Abnahme, der Stand steht in der
[Statusdatei](Status_Gebaeudesimulation_VDI6007.md). Zu Q25 ist der Umfang die **vollständige
Ablösung nach der Löschliste** in Kapitel 6. Beide Fragen führt das Konzept, nicht dieses Papier.

**Stand der Fragen dieses Papiers:** Mit **E27** nach Empfehlung entschieden sind **U1, U3, U5,
U7, U8, U10, U12 und U17**; bei **U6** ist das **Verfahren** entschieden (in G1 beide Zeitbezüge
messen), die Endwahl mit **E29** (23.09.2026): der Stundenanfang. **U11** und **U16**
sind mit E18 entschieden, **U2** ist durch E20 überholt. Mit **E28** (22.09.2026, [Konzept N1.33](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)) sind
**U4** (vor G1) und **U9** (in GB) nach Empfehlung entschieden; vor G0, GB und G1 ist damit keine
Frage dieses Papiers mehr offen. **U13, U14 und U15** — an G4 gebunden — sind mit **E38**
(24.09.2026, [Konzept N1.43](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)) nach Empfehlung
entschieden und gelten für beide Importwege (G4a und G4c); damit ist keine Frage dieses Papiers
mehr offen. Die Tabelle bleibt als Begründung stehen; die rechte Spalte trägt den
Entscheidvermerk.

| Nr. | Frage | Empfehlung und Stand (22.09.2026) |
|---|---|---|
| **U1** | Der Katalogeditor bekommt **einen** Schreibweg (OK/Abbrechen statt „Überschreiben", „Speichern"/„Speichern unter", „Beenden" und „Werte übernehmen", `GebaeudeKatalogDialog.razor:337-340`, `:360-369`). Das ist eine für den Anwender **sichtbare** Änderung | **Ja** — sonst hängen die zehn Prüfregeln aus Konzept 4.8 an drei Schreibstellen und der Reiter-2-Stand macht die U·A-Summe zeitweise falsch. „Speichern unter…" bleibt als nicht schließender Zweitknopf mit `MitSpeichern="true"` (`EPOS.UI/CLAUDE.md:53-56`) — **mit E27 (22.09.2026) nach Empfehlung entschieden** |
| **U2** | ~~Die sieben Modellparameterfelder im Tagesbilanz-Weg **verstecken** oder **gesperrt zeigen**?~~ | **Durch E20 überholt (16.09.2026).** Die Modellparameter stehen **immer** sichtbar und bearbeitbar — sie sind der Parametersatz des Gebäudes, nicht der Rechenweg, und gelten nach der Umstellung. Bedingt ist allein der eingeklappte Abschnitt „Tagesbilanz (Bestandsweg)" mit den vier Feldern, die nur der Altweg liest (2.3, 2.4). Die Frage entfällt, die Nummer bleibt vergeben |
| **U3** | `Platzhalter` am `Zahlenfeld` ergänzen (für „Vorgabe 0,3" im leeren Feld) — ein Eingriff in einen Standardbaustein, den alle Dialoge benutzen | **Ja**, rein additiv (ein `[Parameter] string`, ein `placeholder`-Attribut); zieht `StilblattTests` nach sich. Sonst je Feld eine `Herleitungszeile` — zehn Zeilen statt zehn Platzhalter — **mit E27 (22.09.2026) nach Empfehlung entschieden** |
| **U4** | Ein Abschnitt „13. Gebäudehülle und Gebäudemodell" im [`Glossar_Lokalisierung.md`](Glossar_Lokalisierung.md), **bevor** die 63 en-US-Werte geschrieben werden | **Ja** — das Glossar kennt heute weder Wärmebrücke noch Verschattung, Rahmenanteil, Bodenplatte, Randbedingung oder operative Temperatur. Ohne den Abschnitt entstehen zwei Übersetzungen desselben Begriffs — **mit E28 (22.09.2026) nach Empfehlung entschieden**, fällig vor G1 (Ressourcen des Gebäudedialogs) |
| **U5** | Den Gebäudespalten-Schritt M3 und die drei G2-Spalten zu **einem** Schritt verschmelzen (15 Spalten je Tabelle, ein Sichtneubau)? | **Ja** — E1 liefert G1 und G2 gemeinsam aus; zwei Sichtneubauten hintereinander sind zwei Gelegenheiten, die Definitionen auseinanderlaufen zu lassen. Der Tab_Solar-Schritt bleibt **getrennt** (andere Wirkung, anderer Mitläufercode, anderes Risiko) — **mit E27 (22.09.2026) nach Empfehlung entschieden** |
| **U6** | Zeitbezug der Sonnengeometrie im Gebäudemodell: **Stundenanfang** wie im Bestand (`KlimaImportAblauf.cs:318-322`) oder **Stundenmitte** wie Blatt 3 (Konzept N1.10)? | **In G1 beide Zeitbezüge messen und dann entscheiden** — an der einen Stelle im Eingangsbauer. **Das Verfahren ist mit E27 (22.09.2026) nach Empfehlung entschieden, die Endwahl mit E29 (23.09.2026): Stundenanfang**, wie PV und Solarthermie. Der Unterschied sind 7,5° Stundenwinkel und trifft genau Ost und West. `Tab_Solar.Sol_*` bleibt in jedem Fall unberührt (Referenzbasis) |
| **U7** | Wochenendmaske des Stundenmodells: Ortszeit-Kalender aus dem Wochentag des 1. Januar des Referenzjahres (Konzept 4.4) oder `Tab_Klimadaten.WE` (wie der Bestand)? | **Ortszeit-Kalender** (F-Ü8): Der Vorbereitungsschritt bildet `WE[365]` aus dem Wochentag des 1. Januar des Referenzjahres; eine Probe hält die Maske gegen `Tab_Klimadaten.WE` derselben Klimaregion (`KlimaImportAblauf.cs:354`), eine Abweichung ist ein Befund der Probe — **mit E27 (22.09.2026) nach Empfehlung entschieden** |
| **U8** | Normzahlen als **gitignorierte, lokal beizustellende** Datei (`Referenzlaeufe/Normzahlen/`) mit schweigenden Testfällen — Folge: der Normfallnachweis ist **lokal**, nicht CI | **Ja** — das Ausliefern der Normzahlen wäre eine Vervielfältigung (Konzept N1.2), und LFS ist keine Zugriffsbeschränkung. Die Lücke im Gate gehört ins Protokoll, der Laufauszug (Abweichung je Fall, ohne Absolutwerte) in die Dokumentation — **mit E27 (22.09.2026) nach Empfehlung entschieden** |
| **U9** | Die Grenze von 100 Gebäuden beheben (`HeizwaermebedarfGeb[100]`, `:31`; `MaxP[100]`, `:56`; `IndexOutOfRangeException` an `:816`)? | **Ja, in GB**, wo die Schleife ohnehin angefasst wird: `MaxP` **löschen** (wird nirgends gelesen — wie `Anzahl_Bewohner` `:11` und `Wohnflaeche` `:12`, Befund X 2.3), `HeizwaermebedarfGeb` auf `ctrl.rows` dimensionieren. Ergebnisneutral — und **vor** der Verschiebung nach `Altweg/`, damit das verschobene Modul der geprüfte Stand ist (E20) — **mit E28 (22.09.2026) nach Empfehlung entschieden** |
| **U10** | Eine Lizenzhinweisseite im Installationspaket — und dann gleich für **alle** ausgelieferten Fremdanteile, nicht nur xBIM? | **Ja, mit G4-1 und für alle.** CDDL § 3.1 verlangt den Quellenverweis an den Empfänger; ohne die Seite ist der IFC-Import nicht auslieferbar. Die übrigen Fremdanteile sind ohnehin fällig — darunter **three.js (MIT)** des Gebäudebetrachters (E11, Konzept N1.16) — **mit E27 (22.09.2026) nach Empfehlung entschieden** |
| **U11** | Größenlimit für IFC-Dateien: 50 MB Windows / 20 MB iOS — oder es versuchen und bei Speichermangel abbrechen? | **Benannt ablehnen**, nicht versuchen: `MemoryModel` hält das Modell im Arbeitsspeicher (10–20 MB je MB STEP-Text), ein Speicherabbruch auf dem iPad ist kein Fehlerbild, das man erklären kann. **Die iOS-Zahl ist in G4-8 zu messen**, nicht zu schätzen — **mit E18 (16.09.2026) nach Empfehlung entschieden** |
| **U12** | Woher die Vorgaben je Baualtersklasse? TABULA/IWU hat weder DOI noch Datensatzlizenz | **Eigene Werte aus dem EPOS-Gebäudekatalog ableiten** — die Testdatenbank führt Gebäude je Klasse. Lizenzfrei, hausgemacht, passt zu den übrigen EPOS-Vorgaben. Sonst nur A–H vorbelegen und den Rest leer lassen — **mit E27 (22.09.2026) nach Empfehlung entschieden**; **geändert mit E51** (26.09.2026, [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.58), umgesetzt mit Schemaschritt 149: ohne Katalogsatz gilt statt „leer lassen" der freie Wert nach Stein/Loga (2025, CC BY 4.0) mit Herkunft und Beleg; die Klassen M und A haben eigene Katalogsätze, der freie Wert ruht deshalb |
| **U13** | Mehrere `IfcBuilding` in einer Datei: Klappliste und **ein** Gebäude je Lauf — oder alle auf einmal anlegen? | **Eines je Lauf.** „Alle auf einmal" erzeugt Katalognamen automatisch und zöge die Dublettenlogik des Katalogimports nach; das ist ein eigener Schritt, nicht G4a — **mit E38 (24.09.2026) nach Empfehlung entschieden**, gilt auch für den gbXML-Import (G4c) |
| **U14** | Fensterabzug: Wandfläche = `GrossSideArea` **minus** Fenster und Außentüren — oder die Öffnungen in der Wandfläche belassen, wie `GrossSideArea` sie liefert? | **Abziehen.** Das Modell führt Wand und Fenster getrennt mit je eigenem U-Wert; ohne Abzug zählt die Öffnung zweimal. Wird die Differenz negativ: `NetSideArea`, sonst Warnung und 0 — **mit E38 (24.09.2026) nach Empfehlung entschieden**, gilt auch für den gbXML-Import (G4c; dort A = 0, Zeile rot, `IMP_GBXML_PROT_NETTOFLAECHE_NEGATIV`) |
| **U15** | Die drei ψ-Werte und die drei Anschlusslängen beim IFC-Import als **Vorgabe je Baualtersklasse** setzen oder **leer** lassen? | **ψ als Vorgabe, Längen leer.** IFC liefert weder das eine noch das andere; eine geratene Anschlusslänge sähe aus wie eine gemessene, ein ψ-Vorgabewert ist als Klassenwert erkennbar. Beide tragen Herkunft `Vorgabe` bzw. `Leer` — **mit E38 (24.09.2026) nach Empfehlung entschieden**, gilt auch für den gbXML-Import (G4c) |
| **U16** | Soll `ios.yml` um einen **Gerätebau** ergänzt werden (heute baut der Lauf für den Simulator, `:123-127`, und dort wird nie getrimmt) — oder genügt ein einmaliger Nachweis von Hand? | **Einmaliger Nachweis in G4-8**, nach Rückfrage. Ein dauerhafter Release-Zweig verdoppelt die Laufzeit eines Workflows, der zehnfach zählt; erst wenn xBIM im Feld ist, lohnt die Dauerprüfung — **mit E18 (16.09.2026) nach Empfehlung entschieden**; **mit E38 (24.09.2026) ergänzt:** genau ein iOS-Lauf (`ios.yml`) bei der Abnahme von G4a, ausschließlich nach ausdrücklicher Rückfrage beim Anwender |
| **U17** | Ein Altweg-Gebäude ohne Tagesverteilung bricht heute den **ganzen** Lauf ab (`SimulationWaermebedarf.cs:197`, `:590-595`) — auch für die VDI-Gebäude desselben Projekts (1.5). Lauf abbrechen wie heute oder das Gebäude benannt ablehnen? | **Benannt ablehnen, aber erst mit GA** — bis dahin bleibt das Verhalten des Bestands, weil die Änderung Altweg-Verhalten ändert und den byte-gleichen Nachweis berührt (F-Ü6) — **mit E27 (22.09.2026) nach Empfehlung entschieden** |

---

## 6. Abgrenzung

**Dieses Papier behandelt nicht:**

- **Die Physik selbst.** Rechenweg, Knotenbilanzen, Diskretisierung, Randbedingungen, Validierung
  und die gemessenen Vergleichszahlen stehen im
  [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Kapitel 4, 5 und 10; hier steht nur,
  **wo** sie im Quelltext andockt. Ebenso die Einzelheiten der Stufe G3 (Konzept 6.3) und von G5
  (Begründung in 3.1).
- **Das Mehrzonenmodell.** E7 vergibt es an ein eigenes Papier
  (`Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md`, in Arbeit): Zonenkopplung über θ_NR,eq, Datenmodell
  Zone → Bauteil → Aufbau → Schicht → Baustoff, Zoneneingabe und Zuordnung auf Zonenebene.
- **Bericht und Wiki im Wortlaut.** Was in `KennzahlenKatalog.cs`, `AbweichungsErmittler.cs` und in
  die Wiki-Seite „Gebäudemodell VDI 6007" kommt, steht in Konzept 9; der Logbuch-Eintrag entsteht
  mit dem Upload und trägt die Version, die der Anwender nennt
  ([`Konzept_Hilfesystem_Wikidokumentation.md`](Konzept_Hilfesystem_Wikidokumentation.md), 13.3).
- **Alles, was Konzept 15 ausschließt:** Feuchtebilanz, Kühlung als vierter Kanal (**E12 vom
  16.09.2026 nimmt ihn auf** — die Folgen regelt das Kühlkonzept, Konzept N1.18),
  Bauteilaktivierung, Kopplung von Vorlauftemperatur und Erzeugerfahrplan an die
  Raumtemperatur (seit E22 eigenes Papier [Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md), Stufen AK1 nach G2, AK2 und AK3
  danach — **sie warten nicht auf die Stufe GA** (E23, E26): Sie wirken auf VDI-Gebäude, ein
  Gebäude auf dem Bestandsweg geht bis GA als feste Last ein — ein Altweg-Sonderfall, der in der
  Löschliste unten steht), sommerlicher Wärmeschutz nach DIN 4108-2, Nachweise nach GEG/DIN V 18599,
  Verschattung durch Nachbarbebauung, Wärmerückgewinnung, Nutzungsprofile für Nichtwohngebäude,
  Scan-to-BIM, die Validierung an gemessenen Verbräuchen.
- **Normnachweise, die ohne Datenträger nicht führbar sind.** Nach E5 werden weder die Datenträger
  der Richtlinien noch DWD-Testreferenzjahre beschafft; die Testbeispiele 8–16 der VDI 6020 und
  VDI 2078 sind damit nicht nachrechenbar. EPOS-Plan weist nach **E10** aus: **„Rechenkern nach
  VDI 6007 Blatt 1; elf der zwölf Testbeispiele im Normband einschließlich Druckrundung,
  Testbeispiel 11 in zwei Umschaltstunden um 3,4 W daneben (3,9 W gegen das Band ohne Druckrundung)"** — nicht „validiert nach
  VDI 6020/2078". Der Ausweis wechselt auf **„zwölf von zwölf"**, sobald G0 den Fall 11 löst (der
  eigene Knoten der Kühldecke, 1.3). Nach E6 geht aus VDI 6020:2022 nichts in Code, Tests,
  Testdaten, Wiki, Bericht oder Auslieferung.
- **Der Datenaustausch mit gbXML und IFC in beide Richtungen.** E9 macht den gbXML-Import zur
  Pflicht (G4c) und die beiden Exporte zur Stufe G7; Schema, Abbildung, Herkunftsdaten und
  Reihenfolge stehen im
  [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) und in den Befunden R
  und S. Hier steht allein der **IFC-Import** (Kapitel 3) samt der Auflage, seine Zuordnung zu
  persistieren (3.2). Ebenso wenig behandelt dieses Papier den **Gebäudebetrachter** nach **E11**
  (2D-Grundriss aus den Raumgrenzen, schematische Körper aus dem Zonengeometrie-Modell): Konzept
  N1.16, Datenaustauschkonzept Nachtrag 1, Mehrzonenkonzept 6.7.
- **Die Auslegungsheizlast im Stundenmodell** (Heating Design Period) ist Gegenstand eines späteren
  Papiers. `Waermelast_Max` bleibt unverändert das Maximum des Kanalsummenvektors
  (`SimulationWaermebedarf.cs:401`), die drei Spitzenkennzahlen stehen **zusätzlich** je Gebäude.
- **Jede Weiterentwicklung des Tagesbilanz-Wegs.** Nach **E20**, **E23** und **E26** ist der
  Altweg der **eingefrorene Bestandsweg des Übergangs**: Er wird nach `Simulation/Altweg/`
  verschoben und bekommt danach **für die Dauer des Übergangs keine Änderung außer
  Fehlerbehebung** — keine neue Größe, keinen neuen Ausweis, keine neue Eingabe, keine Kühllast
  (0 mit benanntem Hinweis), keine Anlagenkopplung, keine Zonen. Was an ihm zu beheben ist, gehört
  in die Stufe **GB**, also **vor** die Verschiebung (Kapitel 4); eine ergebniswirksame
  Fehlerbehebung nach der Verschiebung ist ein eigener, begründeter Einfrieranlass (1.8). Eine
  Verbesserung, die beide Wege beträfe, wird allein im VDI-Weg gebaut. Das Risiko „zwei Rechenwege
  nebeneinander" gilt **bis GA**; Modultrennung, `Modultrennungswache` und Ausbauprobe halten seine
  Kosten klein.
- **Die Bauweise der Stufe GA selbst.** Was sie entfernt, führt die Löschliste in 6.1; wie ihr
  Schemaschritt gebaut und die Basis eingefroren wird, folgt den Regeln von 1.6 und 1.8, sobald
  GA beauftragt ist — nach E27 dann, wenn das Ablösekriterium Q24 erfüllt ist (Kapitel 5).

### 6.1 Löschliste der Stufe GA — Altweg ablösen

Die Stufe **GA** ist die letzte des Plans (E26, 17.09.2026). Mit **E27** (22.09.2026,
[Konzept N1.32](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)) ist entschieden: Sie wird
beauftragbar und fällig, sobald die vier Bedingungen des Ablösekriteriums erfüllt sind (**Q24**,
Kapitel 5), und sie ist die **vollständige Ablösung** nach dieser Liste (**Q25**) — keine
Teilablösung, kein Rest des Bestandswegs im Produkt. Ihr Aufwand **5–8 PT** steckt in keiner Summe
(Kapitel 4). Quellen: Konzept N1.25 Punkt 4 und N1.31,
[Befund X](Gebaeudesimulation/2026-09-16_Befund_X_Feldzuordnung_Altweg_VDI6007.md) Kapitel 4
(dort die Erhebung je Stelle). **Regel** ([`ADR-006`](ADR-006_Trennung_Altweg_VDI6007.md),
Entscheidung 6): **Jede Stufe, die einen Altweg-Sonderfall einführt** — Hinweistext,
Ressourcenschlüssel, Sonderweg in Verteilung, Deckung oder Bericht —, **trägt ihn im selben
Auftrag hier ein.**

| Bereich | Was die Stufe GA entfernt oder umstellt | Quelle |
|---|---|---|
| **Datenbank** | zuerst `Fensterflaeche_Ost`/`_West` einmalig aus `Fensterflaeche_Ost_West` füllen (je die Hälfte, wo NULL; F-Ü5); dann je `Tab_Gebaeude` und `Tab_Gebaeude_STAMM` `DROP COLUMN` für `Gebaeude_Modell`, `Fensterflaeche_Ost_West`, `Wochenende`, `Ferien`; im selben Schritt die leserlosen Spalten `WW_Bedarf` und `Waermebedarf`; Sicht `Abfrage_Projektgebaeude` neu aus `GebaeudeSchema.SQL_VIEW_NEU` ohne diese Spalten, der Namensleser zieht mit; `Tab_DBTagV`/`Tab_DBTagVDaten` samt `_STAMM` und `GebaeudetypDialog` (Q25, E27); `DbWerte.GEBAEUDE_MODELL_*`; Testdatenbank auf den Schemastand; der Prüfpunkt der Auslieferungsvorlage zu `Gebaeude_Modell` (F-Ü9) entfällt mit der Spalte | Befund X 4.1; 1.6; Konzept 6.1, 6.4 |
| **Quelltext** | Modul `Simulation/Altweg/` (`TagesbilanzRechenweg`, `TagesbilanzPhysik`, `Tagesbilanzzustand`, rund 435 Zeilen); die Weiche in `HeizwaermeEinesGebaeudes` (`RechenwegWaehlen`, die Felder `_altweg` und `_vdi6007`, der Testzugang `Tagesbilanzweg`, die NULL-Regel `MODELL_OHNE_ANGABE`) und `IGebaeudeRechenweg`; die NULL-Vorgabe von Ost/West aus dem Bestandsfeld (`GebaeudeVorbereitung.FensterflaechenOstWest`, nach dem Füllen der Spalten) — die Fassade wird zum geraden Aufruf des einen Moduls, die Verbrauchs-Rückrechnung zur reinen Nachmultiplikation; `Klimakalender.Altweg` (Klasse `KlimakalenderAltweg`: `Sol_*`, `A_Temp`, `TagTyp_W/NW`) samt dem Aufbau je Lauf in `KlimakalenderLesen` (1.5); das Feld `ProjektGebaeudeModel.Gebaeude_Modell` samt seiner Leserzeile in `ProjektGebaeudeCtrl.ReadAll`; `modellErzwungen` an `GebaeudeBedarfCtrl.Rechnen` samt `GebaeudeBedarfErgebnis.Modell`/`ModellErzwungen` (1.4); die Auskunft `Gebaeuderechenweg` (`OhneAngabe`, `Wirksam`, `IstVdi6007`; G1, Welle 5) und `Gebaeudehuellbilanz.TransmissionGewichtetWK` samt den Gewichtskonstanten; die `Modultrennungswache` (1.9) | Befund X 4.2; 1.1, 1.4, 1.5, 1.9 |
| **Altweg-Sonderfälle der Schwesterstufen** | KU1: der Kältebedarf-0-Hinweis der Kältefassade — Ressourcenschlüssel `SIMENG_KAELTE_BESTANDSWEG`, der Zweig für ein Gebäude ohne Stundenergebnis in `SimulationKaeltebedarf.GebaeudeBuchen` samt Zähler `GebaeudeBestandsweg` und die beiden Bestandsweg-Fälle in `KaeltebedarfTests` (Kühlkonzept F-K18); dazu aus der dritten Welle die Herleitungszeile der Gruppe „Kühlung“ im Gebäudedialog (Ressourcenschlüssel `GEBK_ZEILE_KUEHLUNG_BESTANDSWEG`, Bündelfeld `ZeileKuehlungBestandsweg`), die 0 mit Hinweis im Bedarfsdialog (`GebaeudeBedarfErgebnis.KaelteBestandsweg` und sein Zweig in `GebaeudeBedarfHuelle.Daten` und `Kaelteherleitung`), die zwei bunit-Fälle aus Kühlkonzept 8.6 (`GebaeudeKatalogDialogTests.Ein_Bestandsweg_Gebaeude_zeigt_die_Gruppe_bearbeitbar_mit_Hinweis`, `GebaeudeBedarfDialogTests.Ein_Bestandsweg_Gebaeude_zeigt_Kaeltebedarf_0_mit_Hinweis`), der Bestandsweg-Fall der Herleitung in `KuehlungOberflaecheTests` und der Satz zum Rechenweg Tagesbilanz auf der Wiki-Seite „Kühlung“; Anlagenkopplung (F-A18, 9.5): die Meldung „Altweg-Gebäude ohne Anlagenkopplung“ — Ressourcenschlüssel `SIMENG_AK_ALTWEG_OHNE_KOPPLUNG`, ihr Zweig in `SimulationWaermebedarf.HeizwaermeEinesGebaeudes` und der Fall `AnlagenkopplungDatenbankTests.Ein_Altweg_Gebaeude_rechnet_ohne_Kopplung_und_nennt_es` (angelegt mit AK1); der Ausweis des Rechenwegs samt Zahl der festen Lasten im Bericht (9.4, mit dem Bericht von AK1); der Sonderfall „feste Last“ in Verteilung und Deckung (6.2, mit AK2); Mehrzonenpflege (G6a): die Tagesbilanz-Sperre von „+ Neue Zone …“ und „Duplizieren“ im Zonenreiter — derselbe Grund wie bei der Übernahme (`GEBZ_SPERRE_TAGESBILANZ`), Zweig in `GebaeudeKatalogDialog.NeueZoneSperre`, bunit-Fall `GebaeudeZonenlisteTests.Auf_dem_Tagesbilanz_Weg_ist_Neue_Zone_weich_gesperrt`. Jede weitere Stufe trägt ihren Sonderfall hier nach (Regel oben) | Kühlkonzept, Anlagenkopplung |
| **Oberfläche** | Schalter „Rechenweg" (Klappliste, Herleitungszeile, Ressourcenschlüssel; 2.3); Abschnitt „Tagesbilanz (Bestandsweg)" samt Aufklapplogik und die vierte Zeile der Wärmeleitwerte (2.3, 2.5); Spalte „Rechenweg" und der Rechenweg-Text im Wirt (2.7); Vergleich alt/neu im Bedarfsdialog samt `GebaeudeBedarfDaten.Vergleich` (2.7) — mit ihm die Tabelle `gebb-vergleich`, der zweite Aufruf von `GebaeudeBedarfCtrl.Rechnen` in `GebaeudeBedarfHuelle` und die Schlüssel `GEBB_GRP_VERGLEICH`, `GEBB_SP_KENNZAHL`, `GEBB_SP_TAGESBILANZ`, `GEBB_SP_VDI6007`, `GEBB_SP_ABWEICHUNG` (G2); das nur lesende KI-Feld `rechenweg` des Katalogeditors samt `GebaeudeKatalogKiSicht.Rechenweg` und `KI_DLG_GEBK_RECHENWEG_ERL` (G2); Ausweis „Tagesbilanz (Bestandsweg)" in Bericht und Bedarfsdialog — danach gilt allein der Produktausweis nach E10; die Schreibstellen der Flags `Wochenende`/`Ferien` (seit G1, Welle 5: `GebaeudeKatalogDialog.razor`, Methode `Ableiten`; Träger `GebaeudeKatalogDaten`; Hülle `EPOS.UI.Daten/Bedarf/GebaeudeKatalogHuelle.cs`, `NachModell`); dazu die Ressourcenschlüssel des Übergangs `GEBK_LBL_RECHENWEG`, `GEBK_RECHENWEG_*`, `GEBK_ZEILE_RECHENWEG_*`, `GEBK_GRP_TAGESBILANZ`, `GEBK_LBL_HT_GEWICHTET`, `GEBK_HINWEIS_GEWICHTE`, `GEB_SP_RECHENWEG`, `GEB_LBL_RECHENWEG`, `GEB_RECHENWEG_*`, die Hüllenfunktion `GebaeudeHuelle.Rechenwegtext`, `GebaeudeProjektZeile.Rechenweg`, `GebaeudeStammDetail.Rechenweg` und `GebaeudeBedarfDaten.Modelltext` — sie bleiben bis dahin, damit ein Gebäude auf dem Altweg dieselben Werte behält (X1) | Befund X 4.3; 2.3, 2.4, 2.7 |
| **Tests und Nachweise** | Rückweg-Test einstellen (`GebaeudeRueckwegTests`, dazu die Ausnahme der A15-Zelle in `GebaeudeSchemaTests.Die_Testdatenbank_steht_auf_dem_Schritt`), mit ihm der eigene Modus des Referenzlaufs (F-Ü7); die Fälle zu `SolareGewinneC`, `SpezWaermeverlusteC`, `TaeglHeizlastWG` in `BhkwPlanRueckgabeTests`; die Altweg-Fälle in `GebaeudeBestandsbefundeTests` (Vortemperatur, Ferienmaske, Merkplatz über `Tagesbilanzweg`) und die Weichenfälle in `GebaeudeWeicheTests` (G1.0); `ModultrennungswacheTests`; der Datenbankfall „Tagesbilanz ergibt dieselbe Reihe wie der Lauf"; die bunit-Fälle des Abschnitts und des Schalters (2.10); das Referenzprojekt des Altwegs (A15, Projekt 1040, Gebäude 10645) auf VDI 6007 umstellen — die Zelle `Gebaeude_Modell` zurück auf NULL, `Referenzlaeufe/Skripte/gebaeude_1040_tagesbilanz.py` entfällt; das Werkzeug des Bestandsvergleichs (Bedingung (1) des Ablösekriteriums, Q24/E27) entfernen — `Werkzeuge/Gebaeudevergleich` samt Testprojekt `Werkzeuge/Gebaeudevergleich.Tests`, die zwei Freigaben `InternalsVisibleTo` (`Gebaeudevergleich`, `Gebaeudevergleich.Tests`) in `EPOS.Kern.csproj` und der CI-Schritt „Gebaeudevergleich-Tests“ in `kern.yml`; **Basis neu einfrieren** — GA ist ein eigener, begründeter Einfrierschritt (1.8), Begründung in `Referenzlaeufe/LIESMICH.md`, Logbuch-Eintrag im Wiki | Befund X 4.4; 1.8, 1.9 |
| **Gate** | die **Ausbauprobe**: Ein Bau mit umbenanntem Ordner `Altweg/` übersetzt, nachdem die Weiche entfernt ist, und der Referenzlauf **aller** Projekte ohne Altweg-Gebäude bleibt byte-gleich; ihr statischer Teil läuft ab G1 in der `Modultrennungswache` mit (1.9) | Konzept N1.31; Softwarearchitektur 1.7 |

Der Schemaschritt mit den `DROP COLUMN` je Tabelle und dem Sichtneubau ist der aufwendigste Teil,
das Neu-Einfrieren der teuerste Nachweis (Befund X 4).
- **Die Kälteseite.** E21 legt nur die **Bauform** fest (Fassade `SimulationKaeltebedarf`,
  derselbe Vorbereitungsschritt, dieselbe Gebäuderechnung, kein Altweg auf der Kälteseite; 1.1 und
  1.4). Kanal, Senkenzuordnung, Kälteerzeuger, Dialoge, Schema und Bericht der Kühlung stehen im
  [Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) (Stufen KU0–KU3, E12) und in
  [Befund W](Gebaeudesimulation/2026-09-16_Befund_W_Kuehlung_Bestand.md).
