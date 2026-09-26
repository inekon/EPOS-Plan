# Konzept: Mehrzonenmodell aus IFC — Zonen, Bauteile, Materialdaten (EPOS-Plan)

**Rev. 3 — 17.09.2026 — Prüfung 17.09.2026, E26 eingearbeitet**

> **Nachzug 26.09.2026 — E50** ([Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.57): Der
> Anwender hat die Stufe **G6c** beauftragt und ihre vier Punkte nach Empfehlung entschieden: **M7** —
> Vorgabe beim Import ist die Zonierung je Geschoss (Z4, bei gbXML X2), Rückfall auf eine Zone (Z5), wenn
> die Raumgrenzen fehlen (6.1, 6.5); **M8** — Mindestgröße max(2 m², 2 %) mit Zuschlag zum Nachbarn mit der
> größten gemeinsamen Grenzfläche (6.1); **M12** — 50 Zonen als Vorgabe, der Import warnt mit Rückfrage
> und schlägt das Zusammenlegen auf Geschosse vor (2.9, 6.6); **M13** — die Nachbarschaften werden
> vollständig rekonstruiert, Paarbildung über die Geometrie (6.2). Vor G6c ist kein Punkt mehr offen.
> Nachgezogen in 2.9, 6.1, 6.2, 6.5, 6.6 und Kapitel 10.
>
> **Nachzug 26.09.2026 — Umsetzung G6b** ([Protokoll G6b](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-26_G6b_Mehrzonenrechnung.md),
> [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.55, N1.56): Ein Gebäude im Projekt rechnet mit
> bis zu 50 Zonen, gekoppelt nach ADR-005. **E49** entscheidet M3 (Vorgabe mit Übersteuerung je
> Trennfläche), M5 (eigene Zeilen für unbeheizte Zonen) und M6 (30 Tage mit Probe) nach Empfehlung,
> dazu die Kriterien der Proben 3, 4 und 5b und V0 für die Kopplung. Die Überhitzung zählt gegen
> `Maximaleraumtemperatur`, nicht gegen `Kuehl_Sollwert` (E32). Nachgezogen in 2.9, 7, 8.1, 9 und 10.
>
> **Nachzug 25.09.2026 — Umsetzung G4b** ([Protokoll G4b](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G4b_Bauteilimport.md),
> [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.49): Der Einzonenimport aus IFC und gbXML
> legt auf Wunsch schon **eine** Zone je Gebäude mit Bauteilzeilen und Aufbauten samt Schichten an
> (Regel Z5); mehrere Zonen, `ID_Nachbarzone` und die Zonierungsregeln Z1…Z4 bleiben G6c. Nach **E45**
> lässt der Import bei vollständigen Schichten den U-Wert leer, abweichend vom Vorrang des eingetragenen
> U-Werts in 3.4. Nachgezogen in 1.2, 3.4 und Kapitel 6.
>
> **Nachzug 25.09.2026 — Umsetzung G3** ([Protokoll G3](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-25_G3_Bauteilkatalog.md),
> [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.44–N1.46): Die Schritte **S-A bis S-C**
> sind als Schemaschritte **132 bis 134** mit G3 gebaut, und zwar **alle acht Tabellen** — auch
> `Tab_Bauteilaufbau(_STAMM)` (Softwarearchitektur W1); G6a legt keine Tabelle mehr an. Der
> Baustoffkatalog trägt die Spalte `Hersteller` und neben 65 herstellerneutralen Stoffen 67
> Herstellerprodukte (**E39**); die Projektkopien `Tab_Baustoff` und `Tab_Bauteilaufbau` haben den
> Fremdschlüssel auf `Tab_Projekt`. In G3 liest der Lauf von der Zone nur Nutzfläche (**E40**) und
> Bauteile. Nachgezogen in 3.5, 4.2, 4.4 und Kapitel 9.
>
> **Was Rev. 3 ändert:** `Tab_Zone` und `Tab_Bauteil` entstehen mit **G3** (S-A bis S-C),
> `Tab_Zonenluftstrom` und `ID_Nachbarzone` erst mit **G6b** im neuen Schritt **S-G**; G6a legt keine
> Tabelle an — alle acht, auch `Tab_Bauteilaufbau(_STAMM)`, hat G3 angelegt (4.4, Kapitel 9); die Zonenspaltentabelle führt die Spalten aus
> KU-S1 und AK-S1 (4.2); der Schreibweg ist ein Abgleich über die Ids statt Löschen + Neuanlegen
> (4.1); bei N = 1 entfallen adiabater Vorlauf und Konvergenzprobe (2.4, 2.9, 8.1); die
> Schemaschritte tragen Papiernamen statt fester Nummern (2.6, 4.4); die Summe G6 trägt den
> Vorbehalt X1…X3 (Kapitel 0 und 9); M5 nennt die Kennzahl `Ueberhitzungsstunden`; 2.9 nennt
> den Altweg als Übergang bis zur Ablösung (E26).
>
> **Nachzug 22.09.2026 — E27 (22.09.2026, [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.32):** Der Anwender hat die Punkte
> dieses Papiers aus dem Register entschieden, sämtlich nach Empfehlung: **M2** Raumseitenmaß
> (6.2), **M9** Synonymtabelle in der Auslieferung (3.5), **M10** die große Testdatei ins
> Repositorium, nur mit LFS-Eintrag im selben Schritt (8.2), **M14** Projektkopie und Wertekopie
> beide behalten (4.2); dazu aus Softwarearchitektur und Umsetzungskonzept **A6** (ein Aggregat in
> einer Transaktion, 4.1), **A2** (IFC-Paket am Kern, Naht `IGebaeudeLeser` von Anfang an), **A3**
> (formatfreier Name des Zuordnungsdialogs), **A13** (keine Herkunftsspalten am Gebäude), **A17**
> (kein eigener Maskenschlüssel, Überlagerung im Gebäudedialog), **U10** (Lizenzhinweisseite mit der
> ersten IFC-Stufe) und **U12** (Vorgaben je Baualtersklasse aus dem eigenen EPOS-Gebäudekatalog) —
> diese stehen in 6 und 6.4, soweit sie den Zonenimport berühren. Mit E27 sind auch **D16**
> (gbXML-Zonenbildung) und **Q24** (Fälligkeit der Stufe GA) entschieden (0, 2.8, Kapitel 9).
> M3, M5–M8 und M11–M13 sind nicht Gegenstand von E27 und bleiben mit ihrer Stufe zu entscheiden.

> **Rev. 2 — Korrekturen des Gegenlesens vom 15.09.2026 eingearbeitet, Protokoll:
> [Gegenlesen](Gebaeudesimulation/2026-09-15_Gegenlesen_Mehrzonenkonzept.md)** — dort auch die
> Nachbesserung nach der unabhängigen Prüfung (Probe 3, adiabater Vorlauf in den
> Rechenzeitangaben, Aufwandsverschiebung in Kapitel 9, Kriterium von Probe 1).

Auftrag (Anwender, 15.09.2026, im Wortlaut):

> „Als erstes soll die Gebäudesimulation nach VDI 6007 ein Einzonenmodell analog zum bestehenden
> verwenden. Mit dem Import einer IFC-Datei soll ein Mehrzonenmodell möglich sein. Dazu muss ein
> Konzept erstellt werden, wie die Eingaben für die Zonen und die importierten Materialdaten und
> Flächen erfolgen kann."

Grundlage: das Konzept
[`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Rev. 1 mit Nachtrag 1 und den Entscheiden E1–E11, darunter **E7** — Einzonenmodell zuerst,
Mehrzonen über IFC — und **E8** — die Skalierung bleibt), das
[`Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](Umsetzungskonzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
(Einbindung in den Kern, Stufen G0 und G1) sowie die Befunde
[C](Gebaeudesimulation/2026-09-15_Befund_C_IFC-Recherche.md) (IFC-Recherche),
[N](Gebaeudesimulation/2026-09-15_Befund_N_IFC-Import_Entwurf.md) (Entwurf des Einzonen-Imports),
[O](Gebaeudesimulation/2026-09-15_Befund_O_Zonenkopplung_VDI6007.md) (Zonenkopplung nach
VDI 6007 Blatt 1 und VDI 2078),
[P](Gebaeudesimulation/2026-09-15_Befund_P_IFC_Zonen_Materialien.md) (Zonen, Flächen und
Materialdaten aus IFC, an vier Beispieldateien gemessen) und
[Q](Gebaeudesimulation/2026-09-15_Befund_Q_Muster_Datenmodell_Dialoge.md) (vorhandene Muster für
Datenmodell, Kataloge und Listendialoge). Für die Nähte nach außen kommen
[S](Gebaeudesimulation/2026-09-15_Befund_S_IFC-Export_ohne_Geometriekernel.md) (IFC-Export ohne
Geometriekernel, Kapitel 6) und das
[Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) samt Befund R (gbXML) dazu;
die Kopplungsentscheidung selbst steht in
[ADR-005](ADR-005_Zonenkopplung_Mehrzonenmodell.md).

Normzitate tragen Seite und Gleichungsnummer nach Befund O; Wortlaut und Zahlenreihen der
Richtlinien bleiben draußen. VDI 6020:2022 ist nach Entscheid E6 **nicht** herangezogen — keine
Aussage dieses Papiers stützt sich darauf. Jede Aussage über den Quelltext trägt Datei und Zeile.
Dieses Papier entscheidet nichts; es legt vor — M2, M9, M10 und M14 hat der Anwender mit E27
(22.09.2026) entschieden, M7, M8, M12 und M13 mit E50 (26.09.2026) (Kapitel 10).

---

## 0. Das Ergebnis in sechs Punkten

1. **Das Einzonenmodell bleibt der Regelweg und bleibt unberührt.** Die Stufen G0 bis G2 bauen eine
   Zone je Gebäude aus `Tab_Gebaeude` (Entscheid E7); das Mehrzonenmodell ist die spätere Stufe
   **G6** auf dem Bauteilkatalog (G3) und dem IFC-Import (G4). Ein Gebäude ohne Zonendaten rechnet
   weiter als eine Zone und muss dabei **bitgleich** dasselbe liefern wie die Einzonenrechnung
   **desselben Programmstands** (nach G3); der Ergebniswechsel gegenüber heute ist der
   Einfrierschritt von G3, nicht der von G6 (Befund O, Probe 10).
2. **Die Richtlinie kennt keine Zonenkopplung; sie kennt die Randbedingung.** Blatt 1, 5.3 (S. 7)
   nimmt die Zusammenfassung von Räumen zu Gebäudezonen ausdrücklich aus, **verlangt** aber die
   Aufteilung großer Räume ohne thermischen Ausgleich; VDI 2078, 6.2 (S. 21) wiederholt beides.
   Geregelt ist nur der Nachbarraum: seine Bauteile gehören in die AW-Gruppe (S. 15, Gl. (27),
   S. 17), seine Lufttemperatur geht als θ_NR,eq nach Gl. (40) ein und wird mit U·A nach (41)/(42)
   gewichtet (S. 20) — die Gewichte B_v laufen dabei über **alle** Bauteile der AW-Gruppe und
   summieren sich zu 1 (2.3). **Ein Mehrzonenmodell ist eine EPOS-Erweiterung** — wie Kusuda und
   Hay-Davies; die Produktaussage bleibt „Rechenkern nach VDI 6007 Blatt 1" **je Zone**
   (Wortlaut nach Entscheid E10, Kapitel 7).
3. **Der Rechenweg ist keine zweite Physik, sondern eine zusätzliche Randbedingung.** Je Zone läuft
   das validierte 7R2C-Netz aus Konzept 4.2 in der G3-Fassung weiter — ihr Fensterpfad gilt nach
   **E14** schon ab G1 (Konzept N1.19); die Kopplung sitzt
   ausschließlich in θ_A,eq,gew. Vorgeschlagen wird **Gauß-Seidel innerhalb der Stunde**
   (Vorschlag B, Befund O, 3.2) mit fester Reihenfolge, den Schwellen 0,01 K **und 0,1 W**,
   höchstens 50 Durchläufen und benanntem Fehler; das Regelungsmuster des ersten Durchlaufs einer
   Stunde wird festgehalten und jeder Wechsel gezählt. Vorschlag A ist die Vergleichsrechnung, das
   4×4-Gesamtsystem für N = 2 das exakte Prüforakel; die Entscheidung ist als
   [ADR-005](ADR-005_Zonenkopplung_Mehrzonenmodell.md) festgehalten und am 16.09.2026 angenommen (E17).
4. **Die Zonentopologie steht nicht in der IFC-Datei.** In keiner der vier gemessenen Dateien steht
   eine einzige `IfcZone` (Befund P, § 1.5) — für die zweite Entität der thermischen Zone,
   `IfcSpatialZone` mit `PredefinedType = THERMAL`, steht die Messung aus und ist vor G6c
   nachzuholen (6.1); EPOS-Plan **schlägt** die Zonierung vor (Z1…Z5,
   Vorbelegung Z4 „je Geschoss") und lässt bestätigen. Flächen, Nachbarschaft und Randbedingung
   dagegen sind belegt gewinnbar: 100 % der gemessenen Grenzflächen ergaben einen
   Polygonflächeninhalt ohne Geometriekernel, und `CorrespondingBoundary` war überall dort
   lückenlos gefüllt, wo echte 2nd-Level-Entitäten stehen (§ 2.2, § 2.3).
5. **Die Stoffwerte sind der schwarze Fleck.** Kein einziger opaker Baustoff der vier Dateien trägt
   brauchbare λ, ρ, c; zwei Dateien schreiben `Pset_MaterialThermal` **mit Nullen** (§ 3.4). Deshalb
   ist der **Namensabgleich gegen einen EPOS-Baustoffkatalog der Regelweg, nicht der Notweg** — mit
   Plausibilitätsband, Kette N1…N7, zweisprachiger Synonymtabelle und Anwenderzuordnung.
6. **Das Datenmodell folgt erprobten Hausmustern, zwei Stolperstellen sind namentlich zu bedienen.**
   Zone → Bauteil → Aufbau → Schicht → Baustoff nach Bauform B („geordnete Kindliste",
   `EPOS.Kern/Controller/AnlageStrangCtrl.cs:63`), Baustoff- und Aufbaukatalog als **21. und 22.
   Eintrag** in
   `EPOS.Kern/Allgemein/Katalog/KatalogRegistry.cs:80`. Die Stolperstellen: die Kaskade beim
   Speichern (`AnlageStrangCtrl.cs:33-43`, `sql/schema/001_grundschema.sql:1187`) und der
   entnullende Kopierweg `GebaeudeStammCtrl.CopyFromStamm` (`…/GebaeudeStammCtrl.cs:439`, `:459`),
   der „NULL = Vorgabe" bricht (Befund Q-1). Aufwand für G6: **40–62 PT — ohne die
   gbXML-Zonenregeln X1…X3** (D16, mit E27 entschieden: ja; ihr Zuwachs wird mit der Beauftragung
   von G6c beziffert) (mit dem Grundriss aus dem Nachtrag unten); was zwischen
   den Stufen verschoben ist, steht in Kapitel 9.

**Nachtrag zu Punkt 4 (Entscheid E11, 15.09.2026).** Dieselben Raumgrenzen, aus denen Flächen und
Nachbarschaft entstehen, tragen auch den **Grundriss**. G6c baut daraus ein
**Zonengeometrie-Modell** im Kern (je Zone Polygon, Höhe, Geschoss, Zuordnung der Bauteile zu
Kanten, Boden und Decke) und zeigt es im Zuordnungsdialog als **2D-Grundriss je Geschoss** (6.7):
SVG in einer Razor-Komponente, ein Klick auf einen Raum ordnet die Zone zu. Das sind **6–10 PT**
zusätzlich in G6c, die **Summe G6 steht damit bei 40–62 PT — ohne die gbXML-Zonenregeln X1…X3
(Frage D16)** (Kapitel 9). Dieselbe Geometrie
schreiben später die Exporte G7b und G7e
([Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md), Nachtrag 1); die zweite
Ansicht — schematische Körper — hängt dort an G7b.

---

## 1. Einordnung

### 1.1 Entscheid E7: das Einzonenmodell zuerst

Entscheid E7 (Konzept N1.12) legt fest: Die Stufen G0 bis G2 bauen **eine Zone je Gebäude**, mit
den Eingaben des Bestands (Konzept 4.1 und 4.3, Klassenweg) und der Datenquelle `Tab_Gebaeude`.
Dieses Papier ändert daran nichts; es beschreibt, was **danach** kommt, und zwar so, dass die
frühere Stufe unberührt bleibt. Die Prüfung dafür ist hart und steht in Kapitel 8: alle vierzehn
Referenzprojekte, jedes Gebäude als eine Zone, **bitgleich** zur Einzonenrechnung **desselben
Programmstands** (Befund O, 3.6, Probe 10) — der Ergebniswechsel gegenüber dem heutigen Stand
gehört zum Einfrierschritt von G3. Ohne diese Prüfung würde jede Mehrzonenstufe selbst zu einem
Einfrierschritt.

### 1.2 Wann Mehrzonen greift

**Genau dann, wenn Zonendaten vorliegen.** Ist `Tab_Zone` für ein Gebäude leer, rechnet der
Klassenweg des Einzonenmodells unverändert; trägt sie Zeilen, rechnet der Mehrzonenweg. Die
Erkennung folgt der Schemaprobe mit Gedächtnis aus `AnlageStrangCtrl.cs:102`: eine
`COUNT(*)`-Abfrage, die eine **fehlende** Tabelle von einer **leeren** unterscheidet.

Zwei Wege führen zu Zonendaten — **Eingabe** (der Anwender legt im Gebäudedialog Zonen an, von Hand
oder über „Gebäude als eine Zone übernehmen", Kapitel 5; Stufe G6b) und **Import** (eine IFC-Datei
liefert Räume, Grenzflächen und Schichten, Kapitel 6; Stufe G6c — **eine** Zone je Gebäude samt
Bauteilen und Aufbauten legt schon der Einzonenimport auf Wunsch an, G4b, Konzept N1.49). Ein
drittes Tor gibt es nicht; insbesondere entsteht **keine** Zone stillschweigend aus einem
Einzonengebäude.

**Der Import ist nicht auf IFC beschränkt.** Auch eine gbXML-Datei führt Zonen — `Space` je Raum
und `Zone` als deren Zusammenfassung (Befund R, 5.1) — und speist **dasselbe** Zonenmodell aus
Kapitel 4; Zuordnung, Zonierungsvorschlag und Zuordnungsdialog dieses Papiers gelten dafür
sinngemäß. Welches Format was liefern kann, welche Wege hin und zurück offenstehen und wie die
Formate gegeneinander abzugrenzen sind, steht im
[Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md); dieses Papier beschreibt
den IFC-Weg.

### 1.3 Stufe G6 — Lage in der Reihenfolge

Die Stufenfolge des Konzepts (N1.5: G0 → GB → G1 + G2 gemeinsam → G3 → G4 → G5) bleibt
unangetastet; Entscheid E9 (N1.13) hängt die Exporte als G7 an. Das Mehrzonenmodell tritt als
**G6** dazwischen. **Voraussetzungen:** G0–G2 (G6 rechnet je Zone genau dieses Netz); **G3**, denn
ohne Schichtdaten gibt es keine Trennwandreduktion nach Gl. (11)–(17) — der Klassenweg kennt nur
eine Gesamtkapazität je Gebäude (Befund O, 6.1); **G4** für den Zonenimport, weil Leser, Einheiten,
Azimutkette, Meldungsschlüssel und Lizenzlage dort stehen (Befund N). **G5** (Geometrieableitung,
gbXML) ist unabhängig — G6 braucht sie nicht (Befund P, § 2.3) —, **G7** dagegen baut auf G6 auf,
weil der Export dessen Datenmodell abbildet. Befund O, 6.1 nennt als Lage „G5 oder später"; G6 ist
die Präzisierung.

Die Kopplungsentscheidung selbst — Nachbarraum-Randbedingung statt zweiter Physik, Gauß-Seidel je
Stunde (Empfehlung M1) — ist als
[`ADR-005_Zonenkopplung_Mehrzonenmodell.md`](ADR-005_Zonenkopplung_Mehrzonenmodell.md) festgehalten
und am 16.09.2026 angenommen (E17, damit auch M4); dieses Papier trägt die Begründung, der ADR den
Entscheid.

---

## 2. Rechenweg: N gekoppelte 2-K-Zonen

### 2.1 Der Grundsatz

Je Zone z ∈ 1…N das Netz aus Konzept 4.2 **in der G3-Fassung** (Fenster im AW-Zweig nach
Gl. (25)–(28) — nach **E14** schon der Stand von G1 —, R_rad nach Gl. (29)/(31), die mit G3 kommt);
nur die Bauteile dieses Zweigs stehen im Nenner von B_zj.
Sonst unverändert: zwei Zustände θ_m,AW,z und θ_m,IW,z,
drei algebraische Knoten θ_s,AW,z, θ_s,IW,z, θ_air,z, exakte Diskretisierung über Φ, Γ, Ψ aus den
beiden Eigenwerten. Geändert wird allein die **Bauteilzuordnung** und eine Randbedingung — **keine
zweite Physik** (Befund O, 3.1). Das ist zugleich die Abnahmeregel: Was am Einzonenlöser geprüft
ist, bleibt geprüft; neu zu prüfen ist ausschließlich die Kopplung.

### 2.2 Gruppenbildung AW/IW je Zone

Blatt 1, 6.4, S. 15 fasst alle Außenbauteile **und alle Innenbauteile zu anders temperierten
Nachbarräumen** zu einem asymmetrisch beaufschlagten Bauteil zusammen (Index AW) und alle
symmetrisch beaufschlagten Innenbauteile zu einem adiabaten Bauteil (Index IW). Daraus je Zone:

| Gruppe | Inhalt | Reduktion |
|---|---|---|
| **AW der Zone z** | Außenbauteile der Zone **plus** alle Bauteile zu Zonen anderer Temperatur | einseitig nach Bild 2 (S. 14) mit C_1,korr aus Gl. (17) |
| **IW der Zone z** | Innenbauteile innerhalb der Zone **plus** Trennflächen zu Zonen mit Δϑ < 4 K | symmetrisch auf R_1/C_1 aus Bild 1 (S. 11), ohne Korrektur (S. 14) |

Vier Punkte, die das Einzonenmodell nicht kennt:

1. **Die 4-K-Regel.** VDI 2078, 7.2, S. 46 erlaubt, Innenbauteile zu Räumen mit Δϑ < 4 K wie
   symmetrisch beaufschlagte, adiabate Bauteile zu behandeln. In einem Wohngebäude mit gleichem
   Sollwert ist damit **die Mehrzahl der Trennwände zwischen beheizten Zonen adiabat**; es koppeln
   beheizte gegen unbeheizte Zonen, Zonen mit bewusst unterschiedlichem Sollwert — und jedes Paar,
   dessen **gerechnete** Temperaturen weiter auseinanderlaufen, als die Sollwerte vermuten lassen
   (Nachtabsenkung, Leerstunden, Sommerüberhitzung). Die Zuordnung fällt
   **einmal vor dem Lauf** aus dem größten Betrag der **gerechneten** Zonendifferenz eines Vorlaufs
   mit adiabaten Trennflächen (VDI 2078, 7.2, S. 46 spricht von Raumkonditionen, nicht von
   Sollwerten); Zonen mit deutlich anderen Strahlungsverhältnissen zählen nach Gl. (31), S. 18
   immer zur AW-Gruppe; unbeheizte Zonen gelten immer als anders temperiert. Die getroffene
   Zuordnung steht im Protokoll und im Dialog, und **nach** dem Lauf wird das erreichte Δϑ geprüft
   und eine Überschreitung von 4 K benannt. Eine
   Umschaltung **während** des Laufs wäre eine Strukturänderung des RC-Netzes und ein
   Determinismusrisiko; sie wird ausgeschlossen (Befund O, 1.5).
2. **Die Fallunterscheidung Gl. (29)/(31), S. 18.** Ist die Fläche der zusammengefassten IW kleiner
   als die der AW — einschließlich der Flächen zu Nachbarräumen —, gilt Gl. (31) statt (29) für
   R_α;str;AW/IW; sobald eine Trennwand von IW nach AW wandert, kann genau das eintreten. Das
   Einzonenmodell führt R_rad mit fester Bezugsfläche (Konzept 4.2); hier ist das zu ersetzen.
   **Weil die Umstellung im gemeinsamen Löser sitzt, ist zuerst zu zeigen, dass sie für
   A_IW ≥ A_AW auf Gl. (29) und damit bitgleich auf den heutigen Einzonenwert zurückfällt (neue
   Probe 12a);** trifft das nicht zu, gehört sie mit eigenem Einfrierschritt zu G3, nicht zu G6.
   Wer nur Gl. (29) kennt, rechnet **kleine Zonen mit großen Trennflächen falsch**
   (Befund O, 1.2).
3. **Dieselbe Wand wird zweimal reduziert.** Die Kettenmatrix ist im Allgemeinen nicht
   richtungssymmetrisch (S. 13 zu Gl. (11)); jede Zone baut sie in ihrer eigenen Zählrichtung auf,
   vom eigenen Raum nach außen. **Eine asymmetrisch beaufschlagte Trennwand wird von jeder Seite
   ganz reduziert** (C₁,korr, Gl. (17), S. 14); eine symmetrisch beaufschlagte (4-K-Regel, IW) wird
   wie jedes adiabate Innenbauteil bis zur Mittelebene reduziert und geht mit halber Masse in jede
   Zone ein. Im ersten Fall geht die Wand mit voller Masse in jede der beiden Reduktionen ein, und
   die Bilanz stimmt trotzdem, weil die Kopplung über θ_NR,eq und R_Rest läuft, nicht über eine
   geteilte Kapazität. **Benannte Modellgrenze:** Der Speicherinhalt der asymmetrischen Trennwand
   wird instationär doppelt geführt; die Bilanz schließt im Jahresmittel, nicht in der Stunde
   (Probe 4; Befund O, 1.4).
4. **α_kon je Bauteil.** Für die Trennfläche gelten zwei Werte, der raumseitige α_kon,i der eigenen
   Zone und der α_kon,A;NR der Nachbarseite. **Für den U-Wert der Trennfläche gilt raumseitig
   1/(α_kon,i + α_str), nachbarseitig 1/α_kon,A;NR**, passend zur Vereinfachung in Gl. (40), S. 20;
   Testbeispiel 10 entscheidet die Frage zahlenmäßig und ist dafür abzunehmen. Vorbild ist
   Tabelle A10.1 (S. 58), wo FB1 als einziges Innenbauteil der zwölf Testbeispiele **zwei**
   Übergangskoeffizienten trägt.

**Flächen:** Außenbauteile brutto, Innenbauteile und Trennflächen zu anders temperierten Zonen
netto, Fenster mit Rahmen (VDI 2078, 6.1, S. 18). Die Regel gilt für Gl. (27), (29)/(31) und (42)
gleichermaßen; der Import folgt ihr mit dem Raumseitenmaß (6.2; **M2**, entschieden mit E27).

### 2.3 Nachbarzonen über θ_NR,eq

Blatt 1, S. 19 unten verlangt θ_A,eq getrennt je Außenfläche mit unterschiedlicher Orientierung,
für transparente Flächen **und für Trennwände zu anders temperierten Nachbarräumen**. Gl. (40),
S. 20 bildet θ_NR,eq „in Anlehnung an Gl. (32)" mit zwei Vereinfachungen: θ_A,Lu wird durch die
**Lufttemperatur** θ_NR,Lu des Nachbarraums ersetzt, α_A durch den nur **konvektiven**
Übergangskoeffizienten α_kon,A;NR, weil der langwellige Austausch mit Himmel und Erdboden entfällt.
Ohne strahlende Quellen auf der Nachbarseite — der Regelfall und der des Testbeispiels 10 — ist
θ_NR,eq = θ_NR,Lu. Gl. (41)/(42), S. 20 summieren über opake Außenwände, Außenfenster **und
Nachbarraumflächen** mit B_v = U_v·A_v / Σ(U·A) über alle Bauteile der AW-Gruppe. Daraus die Form,
die den ganzen Rechenweg trägt:

```
θ_A,eq,gew,z(h) = (1 − Σ_j B_zj) · θ̄_A,eq,ext,z(h) + Σ_{j ∈ Nachbarn(z)} B_zj · θ_NR,eq,j(h)

θ̄_A,eq,ext,z(h) = Σ_{v ∈ Außenbauteile z} θ_A,eq,v(h) · B_v / Σ_{v ∈ Außenbauteile z} B_v

B_v   = U_v·A_v / Σ_{v ∈ AW-Gruppe z} U_v·A_v                                (Gl. (42))
B_zj  = Σ_{v ∈ Trennflächen z↔j} B_v
```

**Die Gewichte laufen über alle Bauteile der AW-Gruppe und summieren sich zu 1.** Gl. (41)
summiert θ_A,eq über opake Außenwände, Außenfenster **und** Nachbarraumflächen, jede Fläche mit
ihrem B_v; der Nenner von Gl. (42) ist in allen Fällen dieselbe Summe Σ(U·A) über **alle** p
Bauteile der Gruppe. Eine Form, die θ_ext mit dem Gewicht 1 führt und die Nachbaranteile
obendrauf legt, hätte das Gesamtgewicht 1 + ΣB_zj und triebe die äquivalente Temperatur weit über
jeden der beteiligten Werte hinaus; der Kellerfall des Testbeispiels 10 zeigt den Unterschied
sofort (2.10). θ_A,eq,gew ist eine **affine Funktion der Nachbar-Lufttemperaturen** mit über das
Jahr konstanten Koeffizienten. Die Nachbarzone wirkt **nicht** auf den Luftknoten, sondern über
R_Rest,AW auf den Massenknoten θ_m,AW,z — hinter der Kapazität C_1,AW; das dämpft jede
Kopplungswirkung innerhalb einer Stunde erheblich und ist der physikalische Grund, warum schon
einfache Verfahren hier tragen. **Ausnahme:** ein Zonen-Luftaustausch (2.7) wirkt unmittelbar auf
den Luftknoten und ist die einzige starre Kopplung.

**Zeitliche Kopplung ist von der Richtlinie nicht geregelt.** θ_NR ist dort eine Aktionsgröße:
Blatt 1, 6.2, S. 9 führt „eine von der Raumlufttemperatur abweichende Lufttemperatur in einem
Nebenraum" unter den inneren Wärmequellen; Testbeispiel 10 heißt im Text „Nebenraum ist ein Keller
mit **vorgegebener** Temperatur" (6.7, S. 36) und gibt sie in Tabelle A10.2 (S. 58) als
Stundenprofil vor. Wer sie als Ergebnis einer zweiten Zone einsetzt, verlässt den geregelten
Bereich — er verletzt die Richtlinie nicht, kann sich aber auch nicht auf sie berufen. **Die Wahl
ist eine EPOS-Entscheidung** (Befund O, 1.6).

### 2.4 Der gewählte Kopplungsweg und die Begründung

| Kriterium | **A** Vorstunde | **B** Gauß-Seidel | **C** Gesamtsystem 2N |
|---|---|---|---|
| Genauigkeit | Phasenfehler 1 h auf dem Kopplungspfad | exakt bis 0,01 K und 0,1 W | exakt |
| Stabilität | gut ohne Zonen-Luftaustausch, **fraglich mit** | gut (Diagonaldominanz) | unbedingt |
| Rechenzeit (N = 50, ein Jahr) | ≈ 0,25 s | ≈ 0,8–1,7 s (mit zweitem Vorlauf) | Vorbereitung je Regelungsmuster, danach schnell |
| Determinismus | vollständig | gegeben bei fester Reihenfolge, Schwelle, Höchstzahl | gegeben, Laufzeit datenabhängig |
| Testbarkeit | jede Zone = geprüftes Einzonenmodell | dito, plus Vergleich gegen C | nur als Ganzes |
| Aufwand | klein | mittel | groß |

**Für alle drei Wege gleich:** Vor dem Lauf steht der ungekoppelte Vorlauf mit adiabaten
Trennflächen, aus dem die 4-K-Zuordnung fällt (2.2) — ein Durchlauf je Zone, rund 0,25 s bei
50 Zonen. Er steckt in keiner der drei Spalten, weil er für jeden Kopplungsweg gleich anfällt;
in 2.9 ist er in der Gesamtzeit mitgezählt.

**Ausnahme N = 1.** Ein Gebäude mit genau einer Zone hat keine Trennfläche und keinen Nachbarn:
Der adiabate Vorlauf entfällt, es wird nicht iteriert, und die Zone durchläuft genau denselben
Code in genau derselben Reihenfolge wie die Einzonenrechnung. Das ist die Bedingung, unter der
Probe 10 überhaupt bitgleich sein kann (8.1); ohne diese Ausnahme führten zwei zusätzliche
Vorläufe den Einzonenfall in eine andere Gleitkommafolge.

**Vorschlag: B als Produktweg.** In jeder Stunde wird über die Zonen in fester Reihenfolge
iteriert; jede Zone rechnet ihren Stundenschritt mit den zuletzt bekannten Nachbartemperaturen
**derselben** Stunde. Der Fixpunkt ist die Lösung des gekoppelten Systems, bis auf die
Abbruchschwelle. Das algebraische System der Luftknoten ist strikt diagonaldominant — der
Selbstleitwert einer Zone enthält neben den Kopplungsleitwerten stets auch Lüftung, Fenster,
Wärmebrücken und die beiden Massepfade —, also konvergiert Gauß-Seidel **je Betriebsmuster und für
die vollständige Stundenabbildung, nicht für die Luftknotenalgebra allein**. Wechselt die
Regelungszuordnung zwischen zwei Durchläufen, wird das Muster des ersten Durchlaufs für diese
Stunde festgehalten, zu Ende iteriert und der Wechsel im `SimulationProtokoll` gezählt; Probe 8
prüft eine Pendelstunde ausdrücklich. Wegen der Dämpfung durch C_1,AW reichen in der Regel wenige
Durchläufe (Befund O, 3.2).

**Feste Festlegungen, ohne die B nicht deterministisch ist:** Reihenfolge der Zonen aufsteigend nach
`Tab_Zone.Rang`, bei Gleichstand nach `ID` (fest verdrahtet, nicht aus der Eingabereihenfolge
abgeleitet); Abbruch, wenn sich **alle** Zonen-Stundenmittel um weniger als 0,01 K **und alle
Zonen-Heizlasten um weniger als 0,1 W** ändern — in geregelten Zonen ist die Last das allein
aussagekräftige Maß, weil θ_air dort Vorgabe ist und sich zwischen den Durchläufen nie ändert (ein
Zehntel der Normtoleranz 0,1 K bzw. 1 W, Blatt 1, 6.6, S. 31); höchstens 50 Durchläufe, darüber **benannter
Fehler** im `SimulationProtokoll` und Abbruch für dieses Gebäude — kein stiller Rückfall auf den
letzten Stand (6.8, S. 36; Konzept 4.8).

**Warum nicht A allein:** Der Zonen-Luftaustausch (2.7) ist der Punkt, an dem A kippt — dort
entsteht eine unmittelbare Rückkopplung Luft↔Luft, und ein expliziter Schritt kann bei großen
Volumenströmen aufschwingen; genau dieser Austausch ist die Funktion, die ein Mehrzonenmodell
interessant macht (Treppenhaus, offene Küche). **Warum C nicht ins Produkt:** Der geschlossene Weg
über die Sylvester-Formel aus zwei Eigenwerten (Konzept 4.2) entfällt bei 2N Zuständen, und
schwerer wiegt die Regelung — die ideale Heizung macht θ_air einer geregelten Zone zur vorgegebenen
Größe und die Heizlast zur Reaktionsgröße, in einer freien Zone umgekehrt; welche Zonen geregelt
sind, wechselt stündlich, jede Kombination ist ein anderes A_ges (im schlechtesten Fall 2^N
Matrizen), und die Umschaltsuche per Bisektion (Konzept 4.5) müsste über alle Zonen gleichzeitig
laufen (Befund O, 3.2). **C bleibt als Prüforakel für N = 2** (4×4-System, Gauß-Seidel muss es auf
besser als 0,001 K treffen, Probe 5), **A als Vergleichsrechnung** — ein Schalter in der Probe,
nicht im Dialog; Probe 6 misst, was dieses Papier sonst schätzen müsste.

### 2.5 Unbeheizte Zonen

Eine unbeheizte Zone ist eine Zone **ohne Heizung**: Φ_h ≡ 0, θ_air frei schwingend, kein Sollwert,
keine Kappung. Sie liefert ihre eigene Lufttemperatur als θ_NR,Lu an alle Nachbarzonen. Keine
Sonderphysik, kein Reduktionsfaktor.

**Damit löst sich `KELLER` physikalisch auf.** Konzept 4.4 kennt für die Grundfläche `ERDREICH`
(Kusuda), `KELLER` (Reduktionsfaktor bzw. nach N1.3 θ_NR,eq mit **vorgegebener** Kellertemperatur)
und `AUSSENLUFT`. Mit einer Kellerzone wird `KELLER` zur **Rechnung** statt zur Vorgabe: Die
Kellerlufttemperatur folgt aus der Bilanz von Kellerwänden und Bodenplatte gegen Erdreich,
Kellerdecke gegen die beheizten Zonen, Kellerfenstern und Kellerlüftung. Der Anwender muss keine
Kellertemperatur mehr raten — das ist der stärkste fachliche Gewinn des Mehrzonenmodells
(Befund O, 3.3).

**Erdreich bleibt** und rutscht eine Ebene tiefer an die Kellerbodenplatte. Die Rechenklasse liegt
vor und ist **ohne Änderung nutzbar**:
`EPOS.Kern/Allgemein/Simulation/ErdreichTemperatur.cs:34`, Jahresgang nach Kusuda im Klassenkopf
(`:16-19`), Amplitude und Phase aus einer Sinusregression der zwölf Monatsmittel des
8 760er-Vektors (`:24-27`, ausdrücklich nicht aus den Stundenextrema), Einstieg
`AnalysiereJahresgang(double[] aussentemp)` (`:297`); sie ist datenbank- und oberflächenfrei
(`:29-33`) und heute für die Wärmequelle Erdreich nach VDI 4640 gebaut (`:10`). **Sie ist nicht
anzupassen, sondern zu rufen** — das hält die Fachänderung an einer Stelle.

| Zone | Hüllflächen | Besonderheit |
|---|---|---|
| Keller | Bodenplatte und Kellerwände gegen Erdreich, Kellerdecke gegen beheizte Zonen, ggf. Kellerfenster | großer Speicher, sehr träge — Vorlauf prüfen (2.9) |
| Treppenhaus | Außenwände und Fenster gegen Außenluft, Trennwände und Decken gegen beheizte Zonen | **starke Luftkopplung** über offene Türen und Kamineffekt — der Fall für 2.7 |
| Dachraum | Dachfläche gegen Außenluft (Schalter `Aussenbauteile_Strahlung`, Konzept 4.4), oberste Geschossdecke gegen beheizte Zonen | hohe Lüftungsrate, geringe Masse — schnelle Zeitkonstante, Sommerüberhitzung deutlich |

### 2.6 Sollwerte und Nutzung je Zone

Alles, was Konzept 4.4 heute je Gebäude führt, wird je Zone geführt: die vier Sollwerte,
`Maximaleraumtemperatur`, `Interne_Waermegewinne` mit der 50/50-Aufteilung, `Luftwechselrate`
(G2: Infiltration und Nutzerlüftung), `Heizung_Strahlungsanteil`, `Heizleistung_Max` sowie Fläche,
Höhe und Volumen. Im Bestand hängen Gewinne, die vier Sollwerte, Fläche, Höhe und der Luftwechsel
am Gebäude (`EPOS.Kern/Model/ProjektGebaeudeModel.cs:23`, `:29-33`, `:44-45`, `:52`);
`Heizung_Strahlungsanteil`, `Heizleistung_Max`, `Luftwechsel_Infiltration` und
`Luftwechsel_Nutzer` **entstehen erst mit dem Gebäudespalten-Schritt M3 und dem
Klimaspalten-Schritt M4** (Konzept 6.1) und werden von dort je Zone übersteuerbar. M3 und M4 sind
Papiernamen; ihre Schrittnummern werden erst bei der Beauftragung an `SchemaStand.Zielversion`
abgelesen. **Die Vorgabenkaskade ist
die Bedienregel:** Jedes Zonenfeld darf NULL sein und bedeutet dann „Wert des Gebäudes"; der Dialog
zeigt den geerbten Wert als Vorgabetext (Kapitel 5) — dieselbe Semantik, die Konzept 6.1 festlegt
(„kein DDL-DEFAULT auf Fachwerten; NULL = Vorgabe"), und deshalb ist Befund Q-1 (4.4) ein
Sperrpunkt, nicht eine Fußnote.

**Solare Gewinne je Zone.** Fenster gehören einer Zone; Φ_sol wird je Zone aus **ihren** Fenstern
gebildet und nach Gl. (43)–(46) (S. 21) **innerhalb dieser Zone** auf IW und AW verteilt, mit dem
dort vorgeschriebenen Ausschluss der bestrahlten Fläche. Die Verteilung endet an der Zonengrenze.
Den einen von der Richtlinie vorgesehenen Strahlungsweg über die Grenze — Q̇_str,A,NR in Gl. (40),
S. 20, gespeist aus kurzwelliger Einstrahlung und langwelligem Austausch an der Rückseite der
Innenbauteile (Blatt 1, 6.2, S. 9) — setzt EPOS zu null und benennt das; ein besonntes Treppenhaus
wirkt damit nur über seine Lufttemperatur (Befund O, 3.4 und 5).

### 2.7 Zonen-Luftaustausch

Er ist als Randbedingung vorgesehen — Blatt 1, 6.2, S. 9 nennt „einen Luftaustausch mit
Nebenräumen" unter den inneren Wärmequellen, VDI 2078, 7.1, S. 38 wiederholt die Liste —, aber
**ohne eigene Gleichung**. Der Lüftungspfad des 2-K-Modells ist ein einziger Widerstand nach
Gl. (75), S. 27, und θ_Lue ist dort als Zulufttemperatur „gewichtet nach den Volumenströmen"
erklärt. Er fügt sich also ein, ohne die Modellklasse zu verlassen: V̇_ges,z = Σ_k V̇_k,z;
θ_Lue,z = (Σ_k V̇_k,z · θ_k) / V̇_ges,z mit θ_k = θ_out oder θ_air der Quellzone; R_Lue,z =
1/(c_L·ρ_L·V̇_ges,z) nach Gl. (75). Zwei Auflagen: **Massenbilanz** — der Anwender gibt **Paare**
ein (Zone A ↔ Zone B, V̇), der Gegenstrom entsteht automatisch, und frei eingegebene Einzelströme
werden **benannt abgelehnt**; und **ein Wert für c_L·ρ_L im ganzen Modell**, derselbe wie für die
Lüftung der Zone (Konzept N1.3: 0,34 Wh/(m³K) für Projekte, 1,1953 kJ/(m³K) in den
Blatt-1-Testfällen). Dass der Nachbarzonenstrom in θ_Lue eingeht, ist eine **EPOS-Regel in
Anlehnung an Gl. (75)** und wird wie Kusuda gekennzeichnet: Die Erläuterung zu Gl. (75), S. 27
zählt Infiltration, Fensterlüftung und RLT auf, den Nachbarraumstrom nicht.

Nicht modelliert werden Auftriebs- und Windantrieb, Druckbilanz, offene Türen als variabler
Querschnitt, Kamineffekt. Alle Ströme sind Eingaben — **wer ein Treppenhaus rechnet, rechnet den
Strom, den er selbst eingegeben hat**, und der Dialog muss das sagen (Befund O, 5).

### 2.8 Ergebnis je Zone und Gebäudesumme in den Kanal

Die Anbindung bleibt, wo sie ist. Heute rechnet die Gebäudeschleife in
`EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:190-214`: je Gebäude ein genullter
Einzelpuffer (`:188`), der Aufruf `HeizwaermeEinesGebaeudes(...)` (`:197`), die Addition genau
einmal auf den Heizkanal (`:201`) und einmal auf die Gebäudesumme (`:208`), die Energieprobe
(`:204`), die Spitze in `MaxP[i]` (`:212`); die Umrechnung W → kW steht danach an einer Stelle
(`:222`), die Jahressumme bei `:229`. Der Kanalname ist `DbWerte.KANAL_HEIZUNG`
(`EPOS.Kern/Allgemein/DbWerte.cs:1260`).

**Die Zonenschleife läuft *innerhalb* von `HeizwaermeEinesGebaeudes`**
(`SimulationWaermebedarf.cs:566`), nicht daneben. Diese Methode ist dafür gebaut: Der Kommentar bei
`:192-196` hält fest, dass ihr Rumpf Anweisung für Anweisung derselbe Text ist wie der frühere
Schleifenrumpf, damit der Gebäudedialog genau diese Rechnung fahren kann. Sie füllt den Zielvektor
in Watt (`:562-563`) und meldet `false`, wenn die Rechnung nicht möglich ist (`:590-595`, benannte
Warnung über `SimulationProtokoll`, kein stiller Rückfall).

Damit gilt: **Je Zone** entstehen die vier Reihen aus Konzept 4.6 und die Kennzahlen; sie gehen in
Dialog, Bericht und Diagramm, **nicht** in den Kanal. **Die Gebäudesumme** ist die Summe der
Zonen-Heizlasten, in Watt in den Zielvektor — Kanal, Energieprobe, `MaxP`, Dauerlinie, Deckung und
Skalierung bleiben unverändert, und **kein Aufrufer außerhalb von `HeizwaermeEinesGebaeudes` merkt,
dass es Zonen gibt.** `SimulationWaermebedarf.Waermebedarf_Max` bleibt das Maximum des
Kanalsummenvektors (`:401`); die Ergebnisgröße `Waermelast_Max` wird davon unverändert abgeleitet
(`SimulationRunner.cs:358`, Konzept 4.5). **Die Skalierung** `Z_AuswahlWohnflaeche / Wohnflaeche`
(Konzept 4.7, Entscheid E8) steht im Altweg — dem eingefrorenen Bestandsweg nach E20 und E23, der
nach E26 mit der Stufe GA abgelöst wird (ohne Datum; fällig nach den vier Bedingungen aus Q24,
entschieden mit E27) — in
der Physikfunktion selbst (`EPOS.Kern/Allgemein/BhkwPlan.cs:435`, Argumente `:392`) — die in
Entscheid E8 genannte Fundstelle
in `SimulationWaermebedarf.cs` trifft den Kopfkommentar von `SummenvektorAusKanaelen` und ist dort
zu berichtigen. Im Stundenmodell wird die Skalierung zur Nachmultiplikation und wird auf der
**Gebäudesumme** angewandt, nicht je Zone — sonst verschieben
sich die Zonenanteile gegeneinander. **Die ausgewiesenen Zonenlasten tragen denselben Faktor, damit
sie auf die Gebäudesumme aufgehen; der Faktor wird in Dialog und Bericht benannt, unskaliert
bleiben allein die Temperaturen.** Für Gebäude mit echter Hülle entfällt sie ohnehin, und die
Verbrauchs-Rückrechnung
(`Bewohner_und_Flaeche_berechnen`, `:613-656`) bleibt eine Verhältnisrechnung mit einem
Kataloglauf.

### 2.9 Vorlauf, Determinismus, Rechenzeit

**Vorlauf.** Konzept 4.6 setzt 30 Tage an (die letzten 30 Tage des Jahres, Ergebnisse verworfen).
Die Zeitkonstanten der Referenzgebäude liegen bei 7,5–24,8 h (Konzept 5.7); eine **unbeheizte
Kellerzone** ist deutlich träger, und ihr Anfangszustand wirkt über θ_NR,eq auf jede angrenzende
beheizte Zone. **Vorschlag:** 30 Tage mit Konvergenzprobe — der Vorlauf wird ein zweites Mal
gerechnet, und unterscheidet sich die Endtemperatur irgendeiner Zone um mehr als 0,05 K (die halbe
Druckstelle aus Entscheid E10), wird auf 90 Tage verlängert und das benannt. **Entschieden mit
E49 (A3), umgesetzt mit G6b.**
**Ausnahme N = 1:** Bei genau einer Zone entfallen der adiabate Vorlauf und die Konvergenzprobe;
es bleibt beim Vorlauf der Einzonenrechnung (2.4, Probe 10 in 8.1).

**Determinismus.** Reine 2×2-Arithmetik je Zone, Zustand je Instanz, nichts Statisches
(Konzept 4.1), dazu die drei Festlegungen aus 2.4. Zwei Läufe desselben Modells liefern
byte-gleiche Reihen; ein Lauf mit umgekehrter **Eingabe**reihenfolge der Zonen ebenfalls, weil
intern nach `Rang` sortiert wird (Kapitel 8, Probe 7) — geprüft als eigene Probe in
`EPOS.Kern.Tests` und über den Referenzlauf; `ParallelitaetWacheTests` hält daneben den Kern von
nackter Parallelität frei.

**Rechenzeit.** Rund 5 ms je Zone und Jahr (Konzept 5.13), mal N, mal der Zahl der Durchläufe: bei
drei bis sechs Durchläufen 0,8–1,7 s für 50 Zonen und ein Jahr, einschließlich des zweiten Vorlaufs
aus der Konvergenzprobe und der Umschaltsuche je Zone. **Dazu kommt der ungekoppelte Vorlauf mit
adiabaten Trennflächen, aus dem die 4-K-Zuordnung fällt** (2.2): ein Durchlauf je Zone, rund
0,25 s bei 50 Zonen, für jeden Kopplungsweg gleich — zusammen also rund **1,1–2,0 s** (dieselbe
Zahl in [ADR-005](ADR-005_Zonenkopplung_Mehrzonenmodell.md), Kraft 4). Das liegt
weit über der Planungsgröße 10 ms je Gebäude (Konzept 4.8), ist gegenüber den 4 s des
heutigen Gesamtlaufs aber vertretbar — **und es fällt nur bei Mehrzonengebäuden an**. **Obergrenze 50 Zonen je Gebäude**:
nicht, weil die Physik versagt, sondern weil ein IFC-Import mit 400 Räumen sonst unbemerkt einen
Jahreslauf von Minuten erzeugt (Befund O, 6.4). Der Import **warnt** und schlägt das Zusammenlegen
auf Geschosse vor (6.6), die Rechnung lehnt darüber benannt ab. **Entschieden mit E50 (M12,
26.09.2026): 50 Zonen als Vorgabe**; die Laufzeitmessung aus G6b (50 Zonen in rund 0,55 s je Gebäude und
Jahr, Kapitel 9) stützt die Zahl, eine andere wäre ein eigener Entscheid. Die Institute-Datei liefert 78 **Räume** (Befund P, § 5.1) — Mindestgröße und
Zonenbildung müssen vorher greifen.

### 2.10 Der Prüfstein: Testbeispiel 10

Testbeispiel 10 (Blatt 1, 6.7, S. 36) ist Testbeispiel 5 mit einer einzigen Änderung — der Fußboden
FB1 (17,50 m², Tabelle A10.1, S. 58) ist nicht adiabat, sondern grenzt an einen Keller mit
vorgegebenen 15 °C. Das **verdoppelt den Verlustleitwert**, und die Lufttemperatur liegt am 60. Tag
rund **19 K** unter der des sonst gleichen Testbeispiels 5 (Tabellen A5.3 und A10.3, S. 48/49 und
58/59). Der Fall rechnet **frei schwingend** — die Last ist in allen drei Tagen 0 W —, geprüft wird
also nur die Temperatur, und das ist der schärfere Test: Eine geregelte Zone versteckt Modellfehler
in der Last, eine freie zeigt sie in der Temperatur (Befund O, 2.3). Ein Abstand von 19 K deckt
jede plausible Fehlbedienung des Kopplungspfads auf — falsches Vorzeichen, vergessene Gewichtung in
Gl. (41), verwechselte Bezugsfläche, C_1,korr statt C_1, ein am Luftknoten statt am Massepfad
angehängter Nachbarraum. **Im Mehrzonenmodell wird der Keller als zweite Zone gerechnet**, ideal
auf 15 °C gehalten; damit ist derselbe Testfall Nachweis für G0 und Beleg, dass der Kopplungspfad
die bekannte Lösung nicht verfälscht. **Die Rückkopplung selbst prüft er nicht** — der Keller ist
dort ein Nebenraum mit **vorgegebener** Temperatur (Blatt 1, 6.7, S. 36; A10.2, S. 58) —; dafür
stehen Probe 5 (4×4-Orakel) und ein zweiter Lauf mit **frei schwingendem** Keller. Und das ohne
zusätzliche Normzahlen (die nach Konzept N1.2 ein **nicht ausgeliefertes** Prüfmittel bleiben). Die
vollständige Prüfliste steht in Kapitel 8.

---

## 3. Bauteile und Materialdaten

### 3.1 Der Bauteilweg nach Blatt 1, Gl. (1)–(17)

Der Weg ist geschlossen vorgeschrieben (Blatt 1, 6.3, S. 11–14) und zugleich der Weg, den der
Bauteilkatalog (G3) und der IFC-Import zu bedienen haben:

| Schritt | Gleichung | Seite | Inhalt |
|---|---|---|---|
| 1 | (1), (2) | 11 | Je homogener Schicht eine **Kettenmatrix** im periodischen Fall, eindimensionaler Wärmefluss |
| 2 | (3)–(8) | 11–12 | Die vier komplexen Elemente aus ω_BT, R und C der Schicht |
| 3 | (5)-Kasten, (9) | 12 | **R = s/λ** in m²K/W, **C = c·ρ·s** in J/(m²K), **ω = 2π/(86 400 · T)** mit T in Tagen |
| 4 | (10a)–(10e) | 12–13 | **Bezugsperioden** (3.2) |
| 5 | (11) | 13 | **Gesamtwand** = Produkt der Schichtmatrizen, **beginnend mit den raumzugewandten Schichten**; Reihenfolge darf nicht vertauscht werden, weil die Matrix nicht richtungssymmetrisch ist |
| 6 | (12)–(16) | 13–14 | R₁, R₂, C₁, C₂ aus den Matrixelementen; R₃ = (1/A)·Σ(s_v/λ_v) − R₁ − R₂ |
| 7 | (17) | 14 | **C₁,korr** für einseitige Belastung (Bild 2, S. 14) |

**Aggregation:** Die Richtlinie lässt zwei Wege zu und **schreibt einen vor** — die
Parallelschaltung über die **komplexen** Widerstände (S. 16, begründet mit Vergleichsrechnungen
gegen das Beuken-Modell), weil die getrennte Schaltung von ΣC und Σ1/R Räume mit stark
unterschiedlichen oder abgedeckten Speichermassen schlechter abbildet: Gl. (19) bildet Z₁ je
Bauteil mit ω_RA aus T_RA = 5 Tagen, (22) schaltet alle Z₁ parallel, (20)/(21) holen R₁ und C₁ aus
dem zusammengefassten Z₁ zurück, (23)/(24) sind die Zweierform, bei mehr als zwei Bauteilen mehrfach nacheinander auszuführen.

### 3.2 Bezugsperioden — der Punkt, an dem Innendämmung entschieden wird

Gl. (10a)–(10e), S. 12–13: Je Bauteil gilt **T_BT = 7 Tage** (mit Verweis auf DIN EN ISO 13786);
**Ausnahme 2 Tage** für Bauteile mit raumseitig wärmetechnisch abgedeckten Speichermassen
(Beispiel der Richtlinie: abgehängte Decken). Die Entscheidung fällt **je Bauteil getrennt** über
zwei Kriterien auf R₁;rel = R₁(2 d)/R₁(7 d) und C₁;rel = C₁(2 d)/C₁(7 d): (10a) R₁;rel > 0,99 und
C₁;rel < 0,95; (10b) R₁;rel < 0,95 und C₁;rel < 0,95 und abs(R₁;rel − C₁;rel) > 0,30 → dann (10c)
T_BT = 2, sonst (10d) T_BT = 7. Für die Zusammenfassung zum Raum gilt (10e) **T_RA = 5 Tage**.

> **Der Löser darf nicht pauschal mit 7 Tagen rechnen.** Außen liegende Dämmung lässt die Masse zum
> Raum hin wirksam, innen liegende kann sie abdecken. **Aufgeklebte Innendämmung erfüllt
> (10a)/(10b) in der Regel nicht**; die Kriterien greifen bei raumseitig abgedeckten Speichermassen
> — abgehängte Decken, Vorsatzschalen mit Luftschicht, Doppelböden. Die Prüfung ist trotzdem je
> Bauteil zu rechnen, weil sie nicht am Augenschein hängt. Wer sie überspringt, trifft **Altbauten
> mit raumseitig abgedeckter Masse systematisch falsch** (Befund O, 4.3). Für die Zielgruppe
> Heizungserneuerung im Bestand ist das kein Randfall.

**Luftschichten** kennt die Richtlinie nicht; die Kettenmatrix setzt Wärmeleitung voraus. Der
übliche Weg (DIN EN ISO 6946) ist ein **äquivalenter Wärmewiderstand ohne Kapazität**: R = R_g,
C ≈ 0. Für C → 0 entartet die Matrix zu a₁₁ = a₂₂ = 1, a₁₂ = R, a₂₁ = 0; die Ausdrücke (3)–(8)
enthalten dann 0/0-Formen und sind numerisch abzufangen (Reihenentwicklung statt Division) — das
fällt unter die Vorgabe in 6.8, S. 36. **Stark belüftete** Luftschichten sind keine Schicht,
sondern eine Außenoberfläche; alles dahinter zählt nicht mehr. Weil die Richtlinie schweigt, ist
beides als **EPOS-Regel mit Quelle DIN EN ISO 6946** zu kennzeichnen (wie Kusuda, Konzept N1.3).
**Putz** ist eine normale Schicht: Er deckt die Speichermasse nicht ab, verschiebt aber R₁ und
damit die Oberflächentemperatur merklich.

### 3.3 Fenster

Blatt 1, Gl. (25)–(28), S. 17. Die **Reihenfolge ist vorgeschrieben und ergebnisrelevant**: Die
Parallelschaltung der Fensterwiderstände hat **nach** der der Wände zu erfolgen, „da das Ergebnis
von der Reihenfolge der Berechnung beeinflusst wird"; dabei bleibt **die Wärmekapazität der Wände
unverändert**. Gl. (25): R₁;AFv = R_AFv / 6, in Analogie zu R₁;AWv angesetzt. Gl. (26):
R_AFv = (1/U_AFv − 1/α_Iv − 1/α_Av) · 1/A_AFv — der U-Wert wird um **beide** Übergangswiderstände
bereinigt, weil das Modell sie über R_α;kon und R_α;str selbst führt. Gl. (27):
R_ges,AW = 1 / ( Σ U_AWv·A_AWv + Σ U_AFv·A_AFv ) über alle Außenbauteile **einschließlich der nicht
adiabaten Innenbauteile**. Gl. (28) mit (28a)–(28c): R_Rest,AW als Differenz, geklemmt auf den
äußeren Übergangswiderstand bzw. R₁;AW ≥ 10⁻¹⁰.

Die Kapazität des Außenfensters ist „praktisch null" (S. 14); VDI 2078, 6.1, S. 18 erlaubt
ausdrücklich, transparente Bauteile allein mit dem U-Wert zu veranschlagen. Der Fensterpfad des
Einzonen-Klassenwegs (Konzept 4.2) ist nach N1.5 eine **bewusste Abweichung**; G3 stellt auf den
Normweg um, und das Mehrzonenmodell erbt ihn. **Mit E14 (16.09.2026) ist das erledigt:** der
Einzonen-Klassenweg führt die Fenster schon in G1 nach Gl. (25)–(28); die hier für G3 vorgesehene
Umstellung des Fensterpfads samt ihrer Einfrierfolge **entfällt**, G3 ändert an den Fenstern
nichts mehr — dort wechselt allein R_rad auf Gl. (29)/(31) (Probe 12a, Konzept N1.19).

### 3.4 U-Wert aus Schichten

Liegt ein Schichtsatz mit Stoffwerten vor, folgt der U-Wert aus ihm: U = 1 / (R_si + Σ s_v/λ_v +
R_se), und `Tab_Bauteil.U_Wert` bleibt NULL („aus dem Aufbau gerechnet", Kapitel 4). Liegt beides
vor, gilt der **eingetragene U-Wert** und der gerechnete steht daneben als Herleitung; weicht er um
mehr als 10 % ab, meldet der Dialog es. Der Grund für diesen Vorrang ist gemessen: In den vier
Beispieldateien sind die U-Werte aus `Pset_*Common.ThermalTransmittance` die **belastbarere**
Quelle (FZK-Haus 33 plausible Werte zwischen 0,3 und 2,0 W/(m²K), DigitalHub 359), die Stoffwerte
dagegen praktisch nie gefüllt (Befund P, § 3.4).

**Der Import schreibt anders (E45, [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.49).**
Bringt eine IFC- oder gbXML-Datei zu einem Bauteil einen **vollständigen** Schichtaufbau (Dicke, λ, ρ, c
je Schicht, im Plausibilitätsband), schreibt der Bauteilimport (G4b) den Aufbau und lässt `U_Wert`
**leer** — es rechnen die Schichten; der U-Wert der Datei steht nur zum Vergleich, eine Abweichung über
**5 %** wird gemeldet. Grund: R₁, C₁ und U·A kommen so aus denselben Schichten, und R_Rest kann nicht
negativ werden. Ohne vollständige Stoffwerte trägt die Zeile nur den U-Wert (der Datei, sonst aus einer
masselosen Schichtung, sonst die Vorgabe der Baualtersklasse). Der Vorrang des eingetragenen U-Werts
gilt weiter für Zeilen, die der Anwender pflegt.

**Einheitenfalle.** Gl. (9), S. 12 verlangt C in J/(m²K), also c in J/(kgK). Die Bauteiltabellen
der Richtlinie (z. B. A10.1, S. 58) führen c in **kJ/(kgK)** — Faktor 1 000. IFC liefert
`Pset_MaterialThermal.SpecificHeatCapacity` in SI, also J/(kgK) (Befund P, § 3.2). Der
Baustoffkatalog führt `cp` in J/(kgK); ein Katalogimport aus gedruckten Tabellen braucht die
Umrechnung und einen Plausibilitätsriegel (500 ≤ c ≤ 3 000 J/(kgK) deckt Beton bis Holz ab).

### 3.5 Baustoffkatalog mit gesäten Standardwerten und Quelle

Der Baustoffkatalog ist die Voraussetzung des Mehrzonenimports. **Gebaut mit G3 (Schemaschritte
132 bis 134, 25.09.2026):** `Tab_Baustoff(_STAMM)`, `Tab_Bauteilaufbau(_STAMM)`,
`Tab_Bauteilschicht(_STAMM)`, `Tab_Zone` und `Tab_Bauteil` entstehen **einmal** und in der hier
vorgeschlagenen Form (Bauteil an der Zone, Schicht am Aufbau, `Bezeichner` als Namensspalte);
Kapitel 6.3 des Grundkonzepts ist nachgezogen. `Tab_Zone` entsteht mit G3, weil
`Tab_Bauteil.ID_Zone` NOT NULL auf sie zeigt und der Zonenreiter der Grundform schon dort steht.
G6a legt keine Tabelle mehr an; `Tab_Zonenluftstrom` und `Tab_Bauteil.ID_Nachbarzone` kommen mit
G6b (Schritt S-G, 4.4) — eine zweite Anlage derselben Tabellen wäre ein Umbauschritt und nicht
ergebnisneutral.

**Saat (Schritt 132, E39):** **65 herstellerneutrale Stoffe** nach DIN 4108-4:2020-11 /
DIN EN ISO 10456 (feste Ids 1 bis 65) und **67 Herstellerprodukte** mit den Bemessungswerten aus den
Herstellerunterlagen (feste Ids 1001 bis 1067, Spalte `Hersteller`), nach dem Muster der
Nutzungsdauer-Saat — feste Id je Zeile („sie bleibt über alle Auslieferungen gleich",
`EPOS.Kern/Allgemein/Update/NutzungsdauerSchema.cs:78`), `Quelle` je Zeile (`:191`),
geschrieben über `SaatSchreiben()` (`:448`) mit `?`-Parametern, idempotent; `ReadOnly = 1`, Herkunft
`VORGABE`. Dämmstoffe der herstellerneutralen Saat heißen nach dem Nennwert („λD 0,035"), die Spalte
`Lambda` trägt den Bemessungswert ([Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.44,
N1.46).

> **Zwei Auflagen aus der Auslieferung.** `Werkzeuge/Auslieferungsvorlage` erkennt Kataloge über
> `EndsWith("_STAMM", StringComparison.Ordinal)` (`…/Projektsicht.cs:110-111`) — **ordinal**, und
> `Tab_Brennstoff_Stamm` in gemischter Schreibweise ist der Beleg, dass dieser Fehler schon einmal
> passiert ist (`:44-52`). Wird `--kataloge readonly` gewählt (seit Anwenderentscheid #160‑E‑1a
> nicht mehr die Vorgabe, `Argumente.cs:72`), behält das Werkzeug in `*_STAMM` nur, was
> `ReadOnly = TRUE` trägt (`…/Vorlagenbau.cs:106-112`); ein dadurch **von Zeilen auf null**
> fallender Katalog löst den Wächter aus (`:175`, Abbruchcode 4). **Also genau
> `Tab_Baustoff_STAMM` schreiben und die Saat mit `ReadOnly = 1` setzen**, sonst ist der Katalog in
> der Auslieferung leer. **Zu prüfen ist zusätzlich `Tab_Bauteilschicht_STAMM`**: Sie führt keine
> Spalte `ReadOnly` und würde über die Kaskade ihres Aufbaus mitgerissen (`Vorlagenbau.cs:175`).
> *Erledigt mit G3:* Die Auslieferungsvorlage führt die Kindkataloge ohne `ReadOnly` namentlich und
> prüft sie mit eigenen Proben.

**Herkunftskennzeichen.** Je **Aufbau** und je **Baustoff** eine `Herkunft` ∈ {`GBXML`, `IFC`,
`KATALOG` (über Namensabgleich, mit der getroffenen Stufe als Beleg), `MANUELL`, `VORGABE`} — die
Aufzählung `IfcHerkunft` aus Befund N (4.2), um `KATALOG` erweitert und nach
[Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 1.4/7.3 um `GBXML`.
**`Tab_Bauteilschicht` bekommt keine eigene `Herkunft`** — sie erbt die des Aufbaus, und ihre
Stoffwerte sind ohnehin Kopien zum Zeitpunkt der Zuordnung (Datenaustauschkonzept 7.3). Vorbild
der Spalte ist `Tab_Wechselrichter.Herkunft`. **Die Werte
sind Persistenzwerte, keine Anzeigetexte** — die Regel steht wörtlich in
`EPOS.Kern/Allgemein/Katalog/ImportKonfliktModell.cs:41-46`, begründet in `:36-45` (ein
Sprachwechsel zur Laufzeit hätte die Rückabbildung aus dem Zelltext zerrissen).

**Der Namensabgleich** ist nach Befund P, § 3.6 der Regelweg:

| Stufe | Regel | Beispiel aus den Messdateien |
|---|---|---|
| N1 | **Zahlenschwänze abschneiden** (`\s+\d{5,}$`) — Archicad und Revit hängen eine interne Kennung an | `Leichtbeton 102890359` → `Leichtbeton` |
| N2 | **Normalisieren:** Kleinschreibung, Umlaute auflösen, Bindestriche und Mehrfachleerzeichen vereinheitlichen, Zusätze wie `Verputzt`, `bewehrt`, `generisch` als Marke abtrennen | `Ortbeton - bewehrt Verputzt` → `ortbeton` + Marken |
| N3 | **Genauer Treffer** gegen `Tab_Baustoff_STAMM.Bezeichner` (normalisiert; Kapitel 6.3 des Grundkonzepts ist im selben Schritt von `Name` auf `Bezeichner` anzugleichen) | `stahlbeton` → Stahlbeton |
| N4 | **Synonymtabelle, zweisprachig** | `reinforced concrete` → Stahlbeton, `Luftschicht` → ruhende Luftschicht |
| N5 | **Teilwort mit eindeutigem Treffer** — nur bei genau einem Katalogeintrag als Wortanfang | `Fußbodenaufbau` → kein eindeutiger Treffer |
| N6 | **Sonderfälle ohne Stoff:** `Luftschicht`, `Leer`, `Solid …`, `Radial Gradient Fill …` (eine Schraffur, kein Stoff) | Luftschicht → Ersatzwiderstand nach DIN EN ISO 6946; Schraffur → Schicht verwerfen, Meldung |
| N7 | **Anwenderzuordnung** mit Merkfunktion: eine getroffene Zuordnung IFC-Name → Baustoff wird behalten | alles übrige |

Die Liste ist kurz: gemessen **6 bis 13 Namen je Datei**. Die Synonymtabelle ist eine
**Datentabelle**, kein Anzeigetext, und gehört nicht in die `.resx`. **Entschieden mit E27 (M9):
Sie geht in die Auslieferung** (`_STAMM`) — die Namen der Autorensysteme wiederholen sich
projektübergreifend; je Projekt gepflegte Zuordnungen (N7) ergänzen sie.

**Ein Stoffwert ≤ 0 ist kein Wert.** λ, ρ und c werden nur übernommen, wenn sie im Band liegen: λ
in [0,005; 500] W/(mK), ρ in [5; 8 000] kg/m³, c in [100; 5 000] J/(kgK); alles andere gilt als
„nicht geliefert" und läuft in den Rückfall. Das ist keine Vorsicht, sondern Messung: FZK-Haus und
Institute schreiben `Pset_MaterialThermal` **mit Nullen** — schlimmer als fehlend, weil ein naiver
Leser λ = 0 übernimmt und einen unendlichen Wärmewiderstand rechnet (Befund P, § 3.4).

### 3.6 Was ohne Stoffwerte trotzdem geht

Fehlen die Schichten ganz oder tragen sie keine Stoffe, bleibt das Bauteil **masselos mit U-Wert**
— der Einzonen-Klassenweg (Konzept 4.3), je Bauteil statt je Gebäude; die thermische Masse der Zone
kommt dann aus `Bauweise`, mit der Einrastung über `EPOS.Kern/Allgemein/Gebaeudebauweise.cs:42-48`
(< 30 leicht, > 75 sehr schwer, sonst schwer, bezogen auf Wh/(m²K)) und den Stufenwerten in `:63`
(20 / 50 / 100 × Wohnfläche). **Damit gilt im Mehrzonenmodell dieselbe Zweiteilung wie im
Einzonenmodell** — Bauteilweg mit Schichten, Klassenweg sonst —, nur **je Zone** entschieden; eine
Zone mit `Bauweise` neben einer Nachbarzone mit Schichten ist zulässig, und der Dialog zeigt je
Zone, welcher Weg gilt.

**Nachweis der Reduktion.** Die Richtlinie nennt keine Soll-RC-Werte; sie gibt Schichtaufbauten
(Tabellen A1.1 Typraum S, A3.1 Typraum L) **und** Ergebnisreihen. Der Nachweis ist indirekt:
Reduktion nach Gl. (1)–(17), Aggregation nach (19)–(28), Simulation, Treffen der Ergebnistabellen
im Band. Für das Mehrzonenmodell kommt hinzu: **Testbeispiel 10 prüft FB1 in der AW-Variante**
(Gl. (17), C₁,korr), Testbeispiel 5 dasselbe FB1 in der IW-Variante (R₁/C₁) — zusammen der einzige
normbelegte Nachweis beider Reduktionsarten, deshalb als **Paar** in der Abnahme (Befund O, 4.3).

---

## 4. Datenmodell

Der Vorschlag folgt den Hausmustern aus Befund Q, Kapitel 1, und ist **nicht entschieden**.

### 4.1 Bauform und Begründung

Für Eltern-Kind-Strukturen gibt es drei erprobte Bauformen: (A) Kopf + Wertetabelle
(`Tab_WP`/`Tab_Kenndaten`, `sql/schema/001_grundschema.sql:1310`), (B) geordnete Kindliste
(`Z_AnlageStrang`, `EPOS.Kern/Controller/AnlageStrangCtrl.cs:63`), (C) JSON-Dokument je Projekt
(`Tab_SpeicherAuslegung`, `EPOS.Kern/Controller/SpeicherAuslegungCtrl.cs:32-38`). **Für Zonen,
Bauteile und Schichten passt B**; ihr Klassenkopf (`AnlageStrangCtrl.cs:14-53`) beantwortet die
Fragen, die hier anstehen: **kein zweites `ID_Projekt` am Kind** (`:20-23`, „eine zweite Wahrheit
darüber könnte auseinanderlaufen"); **zwei Lesewege**, je Eltern für den Dialog (`:164`,
`LesenJeAnlage`) und je Projekt über JOIN für den Rechenkern (`:135`, `LesenJeProjekt`); und **die
Kaskadenfalle** (`:33-43`) — `ON DELETE CASCADE` an einem Eltern, dessen Speicherweg Löschen +
Neuanlegen ist, räumt jede Kindliste ab. **Für Zonen gilt das genauso**, weil
`Tab_Gebaeude.ID_ProjektGebaeude` kaskadiert (`sql/schema/001_grundschema.sql:1187`); die Rettung
steht dort, wo das Löschen steht (Vorbild `WizardCtrl.StraengeSichern`), nicht im Controller.

**Der Schreibweg ist nicht Teil der Bauform.** Bauform B gilt hier für Aufbau und Struktur, nicht
für das Speichern. `AnlageStrangCtrl` schreibt als Löschen + Neuanlegen je Eltern in einer
Transaktion (`:210`, Begründung `:25-31`) mit lückenlos neu vergebenen Rängen — und vergibt damit
neue Schlüssel. Genau daran hängen aber die Zuordnungen des Imports: `Tab_Bauteil.ID_Zone`,
`Tab_Bauteil.ID_Aufbau`, `Tab_Bauteil.ID_Nachbarzone` und die Persistenz Zone ↔
`Quellkennung` (Kapitel 6) zeigen auf Ids, die ein zweiter Speichervorgang sonst wegwirft.
**Geschrieben wird deshalb als ein Aggregat je Gebäude durch Abgleich über die Ids** (Entfernen →
Ändern → Anlegen) in **einer** Transaktion — Muster **A6** des
[Registers](Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md), mit E27 (22.09.2026, [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.32)
entschieden: Was in der Oberfläche fehlt, wird
gelöscht;
was vorhanden ist, wird über seine `ID` geändert; was neu ist, wird angelegt. Der `Rang` wird
danach lückenlos neu gesetzt, ohne die Schlüssel anzurühren.

Die DDL-Vorlage ist `EPOS.Kern/Allgemein/Update/AnlageStrangSchema.cs` mit vier Hausregeln:
`STRICT`, `AUTOINCREMENT` am Schlüssel, **Kaskade nur zum Eltern** (der Katalogverweis kaskadiert
nicht) und `CHECK (length(...))` statt einer Typlänge.

### 4.2 Die Tabellen

**`Tab_Zone`** — Bauform B an `Tab_Gebaeude`, **keine** `_STAMM`-Entsprechung (eine Zone ist
Projektware; wiederverwendbar ist der Bauteilaufbau, nicht die Zone).

| Spalte | Typ | NULL | Bedeutung / NULL bedeutet |
|---|---|---|---|
| `ID` | INTEGER PRIMARY KEY AUTOINCREMENT | — | |
| `ID_Gebaeude` | INTEGER NOT NULL | — | FK → `Tab_Gebaeude.ID`, `ON DELETE CASCADE` |
| `Rang` | INTEGER NOT NULL | — | Reihenfolge, lückenlos ab 1; zugleich die Iterationsreihenfolge (2.4) |
| `Bezeichner` | TEXT NOT NULL CHECK (length ≤ 80) | — | Zonenname |
| `Nutzflaeche`, `Raumhoehe`, `Volumen` | REAL | ja | m², m, m³; NULL = aus den Bauteilen bzw. `Tab_Gebaeude.Raumhoehe` bzw. Fläche × Höhe. **E13 (16.09.2026):** die Zonenfläche ist die **Nutzfläche der Zone** (beheizte Netto-Grundfläche) — dieselbe Größe, die das Gebäude nach Konzept N1.17 als Nutzfläche führt |
| `IstBeheizt` | INTEGER NOT NULL DEFAULT 1 CHECK (IN (0,1)) | — | Schalter, kein Fachwert (Boolean-Regel `BETRIEB_SQLITE.md`) |
| `Raumsolltemperatur_Tag`, `_Nachtabsenkung`, `_Wochenende`, `_Ferien`, `Maximaleraumtemperatur`, `Heizung_Strahlungsanteil`, `Heizleistung_Max`, `Luftwechsel_Infiltration`, `Luftwechsel_Nutzer` | REAL | ja | NULL = Wert des Gebäudes |
| `Interne_Waermegewinne`, `Bewohner` | REAL | ja | NULL = anteilig aus dem Gebäude (Flächenschlüssel) |
| `Kuehl_Sollwert`, `Kuehlleistung_Max`, `Kuehl_Sollwert_Nacht` (REAL), `Kuehlung_Aktiv` (INTEGER, `CHECK (IN (0,1))`) | REAL / INTEGER | ja | **Block aus KU-S1**, sofern die Stufe steht: NULL = Wert des Gebäudes ([Kühlkonzept](Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) 7.1) |
| `Uebergabe_Art`, `Uebergabe_Exponent`, `Uebergabe_Leistung_Nenn` | TEXT / REAL / REAL | ja | **Block aus AK-S1**, sofern die Stufe steht: NULL = Wert des Gebäudes ([Anlagenkopplung](Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) 8.1); mehr trägt die Zone nicht |
| `Kuehl_Uebergabe_Art`, `Kuehl_Uebergabe_Exponent`, `Kuehl_Uebergabe_Leistung_Nenn` | TEXT (`CHECK IN` der drei Kühlübergabearten samt `IDEAL`) / REAL / REAL | ja | **Block aus KAK-S1** (E37, Konzept N1.42): NULL = Wert des Gebäudes bzw. Anteil der Zonenfläche; ohne Schalter wie die Heizseite. Anders als die übrigen Blöcke kommt er in einem **eigenen Schritt nach S-C** — Schemaschritt **137**, weil `KAK-S1` (Schritt 135) nach S-C entsteht. Das Aggregat je Gebäude, Projektduplikat und Projekttransfer tragen ihn NULL-erhaltend; gerechnet wird er erst ab G6 |
| `Herkunft`, `Quellkennung` | TEXT | ja | `GBXML`/`IFC`/`KATALOG`/`MANUELL`/`VORGABE` — Großbuchstaben, ASCII, Persistenzwerte in `DbWerte` (Muster `DbWerte.cs:2165-2214`), mit `CHECK (Herkunft IN ('GBXML','IFC','KATALOG','MANUELL','VORGABE'))` (Muster `Tab_Wechselrichter.Herkunft`); `Quellkennung` trägt die `IfcGloballyUniqueId` (Base64-22) oder die gbXML-`id`, `CHECK (length ≤ 64)` — Spaltenname, Wertebereich und Länge nach [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 1.4 (Zeile 2) und 7.3, Frage D9 |

**Die beiden Blöcke aus KU-S1 und AK-S1** stehen hier, weil Kühlkonzept und Anlagenkopplung
dieselben Spalten in der Zone verlangen. Sie werden nicht nachträglich angehängt: Steht die
jeweilige Stufe schon, legt der Schritt, der `Tab_Zone` anlegt (S-C, 4.4), sie gleich mit an;
steht sie noch nicht, bringt sie ihr eigener Schritt (KU-S1 bzw. AK-S1) an beide Tabellen. Mehr
als die hier genannten Spalten trägt die Zone von beiden Stufen nicht. Der dritte Block (KAK-S1, E37)
ist die benannte Ausnahme: ein eigener Schritt nach S-C.

**`Tab_Bauteil`** — Bauform B an `Tab_Zone` (nicht am Gebäude: im Einzonenfall hängt es an der
einen Zone, und die Abfragen bleiben gleich).

| Spalte | Typ | NULL | Bedeutung |
|---|---|---|---|
| `ID`, `Rang`, `Bezeichner` | INTEGER PK AUTOINCREMENT / INTEGER NOT NULL / TEXT NOT NULL CHECK (length ≤ 80) | — | wie bei der Zone; die Namensspalte heißt durchgängig `Bezeichner` (`KatalogRegistry.cs:110` ff.) |
| `ID_Zone` | INTEGER NOT NULL | — | FK → `Tab_Zone.ID`, `ON DELETE CASCADE` |
| `Bauteilart` | TEXT NOT NULL CHECK (IN ('AUSSENWAND','DACH','BODENPLATTE','FENSTER','TUER','INNENWAND','DECKE','VORHANGFASSADE','SONSTIGES')) — `VORHANGFASSADE` trägt U- und g-Wert wie ein Fenster (6.2) | — | Persistenzwerte in `DbWerte`, sprachneutral (Vorbild `DbWerte.cs:2165-2214`) |
| `ID_Aufbau` | INTEGER | ja | FK → `Tab_Bauteilaufbau.ID`, **ohne** Kaskade; NULL = nur U-Wert |
| `Flaeche` | REAL NOT NULL | — | m² |
| `U_Wert` | REAL | ja | W/(m²K); NULL = aus dem Aufbau gerechnet (3.4) |
| `g_Wert`, `Rahmenanteil`, `Verschattungsfaktor` | REAL | ja | nur Fenster; NULL = Vorgabe (0,3 / 0,9) |
| `Neigung` | REAL | ja | °; NULL = nach `Bauteilart` (Dach/Decke 0°, Wand 90°, Boden 180°) |
| `Azimut` | REAL | ja | °, 0° = Nord; **Pflicht nur an Außenluft:** NULL ist zulässig bei Neigung 0° oder 180° und an Erdreich, Zone, unbeheiztem Raum oder innerhalb der Zone — eine Wand an Außenluft ohne Azimut wird benannt abgelehnt, nicht auf Nord vorbelegt ([Leitkonzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.46, Punkt 9) |
| `Randbedingung` | TEXT | ja | `AUSSENLUFT` / `ERDREICH` / `ZONE` / `UNBEHEIZT`; NULL = Außenluft, an Innenwand und Decke NULL = innerhalb der Zone |
| `ID_Nachbarzone` | INTEGER | ja | FK → `Tab_Zone.ID`, **ohne** Kaskade; nur bei `Randbedingung = 'ZONE'` |
| `Psi_L`, `Herkunft`, `Quellkennung` | REAL / TEXT / TEXT | ja | ψ·L in W/K, NULL = keiner; Herkunft und Quellkennung wie bei der Zone |

Der Wert `KELLER` aus Konzept 6.1 fehlt hier mit Absicht: Im Mehrzonenmodell ist ein Keller eine
**unbeheizte Zone**, also `ZONE` (2.5). Der Wert bleibt im Einzonenweg an `Tab_Gebaeude` bestehen.

**`Tab_Bauteilaufbau` / `_STAMM`** — der wiederverwendbare Schichtaufbau: `ID`, `ID_Projekt` (FK →
`Tab_Projekt.ID`, `ON DELETE CASCADE ON UPDATE CASCADE` nach der Hausregel seit Schemaschritt 96; bzw.
`ReadOnly INTEGER NOT NULL DEFAULT 0 CHECK (IN (0,1))` im Stamm), `Bezeichner`, `Beschreibung`,
`Bauteilart`, `Quelle` (Dateiname des Imports), `Herkunft` TEXT mit `CHECK (Herkunft IN
('GBXML','IFC','KATALOG','MANUELL','VORGABE'))` (Datenaustauschkonzept 7.3).
**`Tab_Bauteilschicht` / `_STAMM`** — Bauform A daran, **ohne eigene `Herkunft`** (sie erbt die des
Aufbaus): `ID`,
`ID_Aufbau` (FK, Kaskade), `Reihenfolge` INTEGER NOT NULL (**innen → außen**, ab 1; Zählrichtung
nach Gl. (11), S. 13), `ID_Baustoff` INTEGER, ohne Kaskade, NULL = freie Eingabe — in
`Tab_Bauteilschicht` ein Verweis auf `Tab_Baustoff` (Projektkopie), in `Tab_Bauteilschicht_STAMM`
auf `Tab_Baustoff_STAMM`; die Spalte heißt auf beiden Seiten gleich, ihr `REFERENCES` zeigt je
Seite auf die eigene Ablage, und der Kopierweg setzt sie über die Id-Abbildung um. Dazu
`Dicke` REAL NOT NULL (m), `IstLuftschicht` INTEGER NOT NULL DEFAULT 0 CHECK (IN
(0,1)) sowie `Lambda`, `Rho`, `cp` als REAL NULL — **die Kopie der Stoffwerte zum Zeitpunkt der
Zuordnung**, damit eine spätere Katalogänderung kein gerechnetes Ergebnis rückwirkend verschiebt
(Begründung wie die Kopiersemantik, `KatalogRegistry.cs:86-90`).

**`Tab_Baustoff_STAMM` / `Tab_Baustoff`** — spaltengleich (Regel `WechselrichterSchema.cs:280-284`:
„eine Spalte nur auf einer Seite ist beim `CopyFromStamm` sofort ein Datenverlust"): `ID`,
`Bezeichner` NOT NULL, `Gruppe` TEXT (Mauerwerk, Beton, Dämmstoff, Holz, Putz, …), `Hersteller` TEXT
(≤ 80, **NULL = herstellerneutral**, E39), `Lambda`, `Rho`, `cp` REAL, `Quelle`, `Herkunft` TEXT mit
`CHECK (Herkunft IN ('GBXML','IFC','KATALOG','MANUELL','VORGABE'))` (Datenaustauschkonzept 7.3),
`Quellkennung`; dazu in `_STAMM` `ReadOnly`, in der Projektkopie `ID_Projekt` NOT NULL mit
Fremdschlüssel auf `Tab_Projekt` (`ON DELETE CASCADE ON UPDATE CASCADE`, Hausregel seit
Schemaschritt 96). **`Tab_Zonenluftstrom`** — neu gegenüber Befund Q, weil 2.7 es verlangt:
`ID`, `ID_ZoneA`/`ID_ZoneB` NOT NULL (FK, Kaskade), `Volumenstrom` REAL NOT NULL (m³/h),
`CHECK (ID_ZoneA < ID_ZoneB)` **und** `CREATE UNIQUE INDEX IF NOT EXISTS idx_Zonenluftstrom ON
Tab_Zonenluftstrom(ID_ZoneA, ID_ZoneB)` — der CHECK normiert die Richtung, der Index erzwingt
**eine** Zeile je Paar; erst beides zusammen trägt die Massenbilanz von selbst (Muster
`EPOS.Kern/Controller/SpeicherAuslegungCtrl.cs:37-38`). **Offen (M4):** ob der Zonen-Luftaustausch in G6 überhaupt kommt.

**Keine `Tab_Zonenkopplung`.** Die Kopplung steht als `ID_Nachbarzone` am Bauteil: Eine Innenwand
zwischen zwei Zonen ist genau ein Bauteil mit Fläche, Aufbau und zwei Seiten; eine zweite Tabelle
wäre eine zweite Wahrheit über dieselbe Fläche (`AnlageStrangCtrl.cs:20-23` gilt hier wörtlich).
Die Gegenseite wird beim **Lesen** erzeugt — das Bauteil zählt in Zone A vorwärts, in Zone B mit
gespiegelter Schichtfolge und eigener Reduktion (2.2, Punkt 3) —, und ein Wächter prüft, dass nicht
**beide** Zonen dieselbe Trennfläche führen.

### 4.3 Verträglichkeit mit `Tab_Gebaeude` und dem Einzonenweg

**Ein Gebäude ohne Zone bleibt ein Gebäude ohne Zone:** `Tab_Zone` leer heißt, der Klassenweg aus
Konzept 4.3 rechnet wie bisher; der Leser prüft das über die Schemaprobe mit Gedächtnis
(`AnlageStrangCtrl.cs:102`) — diese Stelle ist die einzige Verzweigung. **`Tab_Gebaeude` bleibt die
führende Ablage der Summen und Vorgaben:** Die fünf `k_Wert_*`/Flächenpaare
(`sql/schema/001_grundschema.sql:1152-1161`) sind der Klassenweg und zugleich die Vorgabe, aus der
eine erste Zone erzeugt wird. **Die U·A-Zeilen aus Entscheid E2 sind die gemeinsame Zielstruktur**
(Konzept N1.6): je Gruppe bzw. je Bauteil U, A, U·A, Randbedingung, darunter H_T, H_ve, H_ges — der
Klassenweg füllt sie je Gruppe, der Bauteilweg je Bauteil, der IFC-Import mit Herkunftskennzeichen,
und eine Zone summiert ihre Bauteile in dieselben Zeilen. **Summenregel:** Sobald Zonen da sind,
sind `Tab_Gebaeude.Wohnflaeche_gesamt` und die Flächenspalten **abgeleitete Anzeigen**, keine
Eingaben — sichtbar zu machen über `EPOS.UI/Bausteine/Herleitungszeile.razor` (im Einsatz
`PufferSpProjektDialog.razor:25-31`), **nicht still zu überschreiben.**

### 4.4 Migrationsschritte

`sql/schema/001_grundschema.sql` ist **nicht der Ort** (`:1-4`, „NICHT VON HAND AENDERN … Ab
S4-Beginn eingefroren"); alle jüngeren Tabellen fehlen dort. Der Weg ist ADR-001
([`ADR-001_Schema-Ausrollung.md`](ADR-001_Schema-Ausrollung.md), Option C): eine `*Schema`-Klasse
im Kern als **eine Quelle** für vier Leser — Migrationsschritt
(`WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs:4893` als Muster, registriert
`:3703`, Konstante `:2762`), `Werkzeuge/Testdatenbankschema/Program.cs:272-302`, Kopierweg und
Nachweis. Die vier Handgriffe in fester Reihenfolge stehen in `SchemaMigration.cs:4879-4882`; Saat
und Zuordnung laufen über den Kern mit `?`-Parametern, und der Zweig läuft vor dem ersten Fenster,
muss also still bleiben (`:4883-4891`).

| Schritt | Inhalt | Ergebnisneutral? |
|---|---|---|
| **S-A** (mit **G3**, Schritt **132**) | `Tab_Baustoff_STAMM` + `Tab_Baustoff` anlegen, Baustoffsaat schreiben (`ReadOnly = 1`), DDL und Saat in `BaustoffSchema.cs` nach Muster `NutzungsdauerSchema.cs:218/257/297` | ja — legt an und sät |
| **S-B** (mit **G3**, Schritt **133**) | `Tab_Bauteilaufbau(_STAMM)` + `Tab_Bauteilschicht(_STAMM)` anlegen, Index `(ID_Aufbau, Reihenfolge)` | ja |
| **S-C** (mit **G3**, Schritt **134**) | `Tab_Zone` + `Tab_Bauteil` anlegen, Indizes `(ID_Gebaeude, Rang)` und `(ID_Zone, Rang)`; `Tab_Zone` und `Tab_Bauteil` führen `Quellkennung` (Länge 64) und `Herkunft` mit `CHECK (Herkunft IN ('GBXML','IFC','KATALOG','MANUELL','VORGABE'))` — **nicht** `IfcGuid` (4.2; [Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 1.4/7.3, Frage D9). Stehen KU-S1 bzw. AK-S1 schon, legt S-C deren Zonenspalten gleich mit an (4.2) | ja, solange kein Rechenweg liest |
| **S-D** | Registerpflege **ohne DDL**: `KatalogRegistry`-Einträge `BAUSTOFF` und `AUFBAU` (mit Datenblock), `SchemaKatalog`-Konstanten (`EPOS.Kern/Allgemein/Update/SchemaKatalog.cs:46`, `:47-51`), `ProjektDuplizierenCtrl.FK_MAP` und `KINDER`, `Seitenschluessel`, `Menuetabelle`, Ressourcen + `ResourceDesigner`, **und die sechs neuen Projekttabellen in `sql/tools/Reduziere-Testdatenbank.sql`** — `Tab_Zone`, `Tab_Bauteil`, `Tab_Zonenluftstrom` über den Unterausdruck auf `Tab_Gebaeude` (sie führen bewusst kein `ID_Projekt`, Vorbild `Tab_DBTagVDaten`), `Tab_Bauteilaufbau`, `Tab_Bauteilschicht`, `Tab_Baustoff` über `ID_Projekt`. Jede Tabelle wird in dem Schritt eingetragen, der sie anlegt — `Tab_Zonenluftstrom` also erst mit S-G | ja |
| **S-G** (mit **G6b**) | `Tab_Zonenluftstrom` anlegen und `Tab_Bauteil.ID_Nachbarzone` ergänzen, Index `(ID_Zone)`; beide haben vor der Zonenrechnung keinen Leser und gehören deshalb dorthin, wo der Rechenweg entsteht — **nicht** zu S-C (Softwarearchitektur 5) | ja, solange kein Rechenweg liest |
| **S-E** | **S-E gehört nicht zu G6**: Der Umbau von `GebaeudeStammCtrl.CopyFromStamm`/`Insert`/`Overwrite` auf die Spaltenlisten-Bauweise (Befund Q-1) läuft als eigener, begründeter Einfrierschritt **mit G1**; in G6a bleibt davon nur das Mitkopieren der Zonen im schon umgebauten Kopierweg; Zonen im Katalog und eine `Tab_Zone_STAMM` gibt es nicht (Softwarearchitektur 2.9) | **mit G1 nein** — in G6a ergebnisneutral, solange kein Projekt Zonen führt |

Die Schritte S-A bis S-C und S-G werden als **nummerierte** Migrationsschritte nach ADR-001
geführt; die Nummern werden vergeben, wenn der Schemastand bei Beauftragung der jeweiligen Stufe
feststeht — **S-A bis S-C tragen seit G3 die Nummern 132 bis 134** (die Nummer steht allein in
`BaustoffSchema.SCHRITT`, S-B und S-C zählen davon weiter); S-D ist Registerpflege ohne DDL und ohne
Nummer, S-G bekommt ihre Nummer mit G6b. Die Gebäudespalten-Schritte tragen bis dahin die Papiernamen **M3** und **M4**; die
Zahlen 77 und 78 sind im Bestand anderweitig vergeben (Softwarearchitektur 2.4; A11, mit E27 entschieden: Nummern erst bei Beauftragung). Der
Zielstand wird an `SchemaStand.Zielversion` abgelesen. Jede Nummer bekommt ihre Konstante,
ihre Registrierung und ihren Zweig in `SchemaMigration.cs`.

**S-A bis S-C fallen nach 3.5 schon mit G3 an** — also auch `Tab_Zone`, weil
`Tab_Bauteil.ID_Zone` NOT NULL auf sie zeigt und der Zonenreiter der Grundform schon in G3 steht —
und zwar bereits in der hier vorgeschlagenen Form (Bauteil an der Zone, Schicht am Aufbau,
`Bezeichner` als Namensspalte). Kommen sie von dort, übernimmt G6a sie unverändert; eine zweite
Anlage derselben Tabellen in anderer Form wäre ein Umbauschritt und nicht ergebnisneutral. **Mit G3
ist auch S-B gebaut** (Softwarearchitektur W1, Konzept N1.46): G6a legt keine Tabelle mehr an;
`Tab_Zonenluftstrom` und `Tab_Bauteil.ID_Nachbarzone` kommen mit S-G und G6b.

> **Befund Q-1 (hoch, Sperrpunkt).** `GebaeudeStammCtrl.CopyFromStamm`
> (`EPOS.Kern/Controller/GebaeudeStammCtrl.cs:439`) ist ein handgeschriebener 55-Spalten-`INSERT`,
> der **jedes NULL zu 0,0 macht** (`:459` ff.), mit `DataRepository.GetMaxID(...) + 1` als
> Schlüsselvergabe (`:446`). Damit bricht „NULL = Vorgabe" aus Konzept 6.1: Ein aus dem Katalog
> übernommenes Gebäude bekäme Rahmenanteil 0 statt 0,3 und Verschattungsfaktor 0 statt 0,9;
> dieselbe Handliste steckt in `Insert` (`:406`) und `Overwrite` (`:418`). Der neue Weg steht
> daneben: `WechselrichterSchema.Fachspalten` (`…/WechselrichterSchema.cs:287`) +
> `WechselrichterCtrl.CopyFromStamm` (`…/WechselrichterCtrl.cs:103`) mit NULL-erhaltendem
> `Spaltenwert`. **Das gehört nach G1, nicht nach G6** — sonst erbt G6 den Fehler und vererbt ihn
> an jede Zone.

**Vier Fallen, die sonst teuer werden** (Befund Q, 6): (1) Die Sicht `Abfrage_Projektgebaeude` hat
eine feste Spaltenliste (`sql/schema/002_views.sql:89-91`, Zeile 90 mit 58 Spalten) — SQLite kennt
kein `ALTER VIEW`, jede neue Gebäudespalte braucht `DROP VIEW` + `CREATE VIEW` im selben Schritt,
und `ProjektGebaeudeCtrl.ReadAll` muss vorher auf **Namenszugriff** umgestellt sein (Konzept 6.2).
(2) **`FK_MAP` ist `OrdinalIgnoreCase`** (`ProjektDuplizierenCtrl.cs:85-137`, die Begründung steht im Quelltext `:113-118`): ein zweiter Eintrag
mit gleichem Schlüssel in anderer Schreibweise ist eine `ArgumentException` beim Laden der Klasse —
sichtbar als „Programm startet nicht"; neu kommen `ID_Zone`, `ID_Aufbau`,
`ID_Baustoff` und `ID_Nachbarzone` hinzu, vorhanden sind `ID_Gebaeude` und `ID_ProjektGebaeude`
(`:98-99`). (3) **`KINDER` braucht Handpflege, dreistufig** (`:152-175`; zweistufiges Vorbild
`Tab_DBTagV`/`Tab_DBTagVDaten`, `:156-157`) — auf die Auto-Erkennung ist kein Verlass: Sie nimmt
die **erste** Spalte mit deklarierter Beziehung, und bei zwei Fremdschlüsseln an der Schicht
(`ID_Aufbau`, `ID_Baustoff`) entschiede die Spaltenreihenfolge; über `ID_Baustoff` gefiltert fielen
alle Schichten weg (`:168-175`). (4) **Bauteilart, Randbedingung und Herkunft sind
Persistenzwerte** (4.2), keine Anzeigetexte.

**Referenzlauf.** Ein Schritt, der nur Tabellen anlegt und sät, ist ergebnisneutral; der Wortlaut
dafür steht in `Referenzlaeufe/LIESMICH.md:236-245`. Das gilt für S-A bis S-D und S-G so lange,
wie **kein
Rechenweg sie liest**. Der Umbau aus S-E berührt den Referenzlauf und bekommt seinen eigenen,
begründeten Einfrierschritt — **mit G1**, nicht mit G6. Nach jeder neuen SQL-Anweisung läuft `Werkzeuge/SqlDialektPruefer`.

---

## 5. Eingaben

### 5.1 Dialogskizze

```
Gebäudedialog (GebaeudeDialog.razor, Zweispaltenauswahl)
└─ Katalogeditor (GebaeudeKatalogDialog.razor, Überlagerung)
   ├─ Reiter "Flächen und U-Werte"      (Bestand)
   ├─ Reiter "Temperaturen, Ferien …"   (Bestand)
   ├─ Reiter "Hülle und Rechenmodell"   (E2/G1: U, A, U·A je Gruppe, H_T/H_ve/H_ges)
   └─ Reiter "Zonen"                    (NEU, G6)
      ├─ Zeilenraster: Zone | Fläche | Volumen | beheizt | H_T | Bauteile | ▸
      ├─ "+ Neue Zone …" / "Gebäude als eine Zone übernehmen" / "Aus IFC-Datei übernehmen …"
      ├─ Zonendialog (Überlagerung)
      │  ├─ Formularraster: Fläche, Höhe, Volumen, beheizt, Solltemperaturen, Luftwechsel,
      │  │   Gewinne — jedes leere Feld zeigt "Vorgabe: <Gebäudewert>"
      │  └─ Zeilenraster Bauteile: Art | Bezeichnung | A | U | Azimut | Randbed. | Nachbar | ▸
      │     └─ Bauteildialog (Überlagerung)
      │        ├─ Formularraster: Art, Fläche, Azimut, Neigung, Randbedingung,
      │        │   Nachbarzone (nur bei ZONE), g/Rahmen/Verschattung (nur Fenster), ψ·L
      │        └─ Aufbau: Katalogwahl (Katalogliste) ODER Schichten
      │           └─ Zeilenraster Schichten: Nr | Baustoff | Dicke | λ | ρ | c | R | Herkunft
      │              Summenfuß: R_ges, U, C_wirk, T_BT (7 d / 2 d nach Gl. (10a)–(10d))
      └─ Luftaustausch (Überlagerung, nur wenn M4 bejaht)
         Zeilenraster: Zone A | Zone B | V̇ [m³/h]   — Paare, kein Einzelstrom
```

Dazu in der Administration **zwei** neue Kataloge — `KatalogBrowserArt.Baustoff` und
`…Aufbau` als fünfte und sechste Ausprägung
(`EPOS.Kern/Allgemein/Katalog/KatalogBrowserProfil.cs:17-29`, heute vier) mit **zwei** Menüpunkten
unter „Administration" (`EPOS.UI/Bausteine/Menuetabelle.cs:175`): der Baustoffkatalog mit der Liste
links (Bezeichner, Gruppe, λ, ρ, c, Quelle) und dem Detailblock rechts über `Katalogfelder`, der
Aufbaukatalog mit seinen Schichten als `Datenblock` am Aufbau (FK `ID_Aufbau`, Sortierung
`Reihenfolge`, Muster Wärmepumpe `KatalogRegistry.cs:118-131`). **Kein neuer Dialogtyp** — ein
Katalog ist ein Eintrag in `KatalogRegistry.cs:80` (heute zwanzig Definitionen ab `:110`), und der
Gebäudekatalog ist dort der kürzeste (`:218-220`, ohne Datenblöcke).

### 5.2 Die Hausmuster, die dabei gelten

| Muster | Fundstelle | Bedeutung hier |
|---|---|---|
| **OK und Abbrechen als `SpeichernLeiste`**, **Überlagerung statt zweitem Fenster**, **Esc kaskadiert** | `EPOS.UI/CLAUDE.md:48`, `:120`, `:45`; `EPOS.UI/Dialoge/Bedarf/GebaeudeDialog.razor:24-28` | vier Ebenen tief, jede eigenständig; der Gebäudedialog ist bereits Wirt von vier Überlagerungen, bei drei Ebenen also drei Esc-Prüfungen |
| **Arbeitsstand im Speicher, geschrieben wird im OK-Weg**; Pflichtprüfung im „Übernehmen" | `EPOS.UI/Dialoge/Simulation/PufferSpProjektDialog.razor:11-18`, `:33-35` | vorläufige Zeilen tragen eine **negative** Id; nur eine positive hat eine Entsprechung in der Datenbank |
| **Eine Liste ist kein Formularblock** | `PufferSpProjektDialog.razor:51-58` | Bauteil- und Schichtlisten stehen **außerhalb** des `Formularraster` |
| **Die Zeile IST eine kleine Maske** | `EPOS.UI/Bausteine/Zeilenraster.razor:1-32` | genau die Bauteilzeile; Abschlusszeile „+ Neue Position …" und Summenfuß sind eingebaut |
| **Gefiltert wird VOR dem Raster** | `EPOS.UI/Bausteine/Katalogliste.razor:17-26` | QuickGrid filtert nicht; der Kern schränkt ein |
| **Der Schlüssel der Zeile ist die Zeilen-Id, nie der Name** | `EPOS.UI/Dialoge/Bedarf/GebaeudeDaten.cs:19-22` | zwei gleichnamige Zonen sind erlaubt |
| **Ein Reiter zeichnet nie ein vorbelegtes DTO als Ergebnis** | `EPOS.UI/CLAUDE.md:82` | der Zonenreiter führt einen eigenen Stand, den erst „Übernehmen" schreibt (`GebaeudeKatalogDialog.razor:549`, `:754-757`) |

**Die Lehre aus `PvStraengeFelder.razor:23-36`** gilt hier wörtlich: „noch kein Strang" war keine
Voraussetzung der Wahl, sondern ihre **Folge** — der einzige Weg zum ersten Strang lag innerhalb
des gesperrten Weges. **Für Zonen heißt das: „Gebäude ohne Zone" darf den Zonenweg nicht sperren**;
der Abschnitt steht auch leer da, mit dem Knopf „Gebäude als eine Zone übernehmen" (`:38-44`).

### 5.3 Plausibilitäts- und Konsistenzprüfungen

Alle Prüfungen laufen **vor dem Schreiben** (Konzept 4.8), als benannte Fehler, ohne stillen
Rückfall. Je Eingabe:

| Ebene | Regel |
|---|---|
| Zone | `Nutzflaeche > 0`, `Raumhoehe > 0`, `Volumen > 0`; Solltemperaturen 5…40 °C; `Maximaleraumtemperatur` ≥ Tagessollwert; Luftwechsel 0…10 1/h; höchstens 50 Zonen je Gebäude — nach E46, mit E50 (M12) als Vorgabe entschieden (2.9); ab zwei Zonen ist die Nutzfläche Pflicht (G6a) |
| Bauteil | `Flaeche > 0`; U-Wert 0,1…6 W/(m²K); 0 < g ≤ 1; Rahmenanteil 0,05…0,6; Azimut 0…360°, Neigung 0…180°; `ID_Nachbarzone` gesetzt **genau dann**, wenn `Randbedingung = 'ZONE'`, und ≠ `ID_Zone` |
| Aufbau/Schicht | `Dicke` 0,001…1,0 m; λ, ρ, c im Band aus 3.5; `Reihenfolge` lückenlos ab 1; mindestens eine Schicht |
| aus 4.8 geerbt | `R_Rest,AW > 0` je Zone (Gl. (28), S. 17 mit den Klemmfällen (28a)–(28c)); `5 ≤ Bauweise/Nutzflaeche ≤ 200 Wh/(m²K)` im Klassenweg |

Neu sind die Prüfungen **zwischen** Zonen, im Dialog wie im Lauf: **Trennflächenbilanz** — jede
Trennfläche wird von **genau einer** Zone geführt, die Gegenseite entsteht beim Lesen (4.2), zwei
Zeilen über dieselbe Fläche sind ein Fehler; **Nachbar existiert** — `ID_Nachbarzone` zeigt auf eine
Zone desselben Gebäudes; **geschlossene Hülle je Zone** — Σ A_v·n_v betragsmäßig nahe null, dazu
die Verankerung von „oben" und „unten" an einer Bodenplatte bzw. einer Erdreichgrenze (6.2), weil
Σ oben ≈ Σ unten gegen eine globale Spiegelung blind ist (Abweichung > 10 % → Warnung; eine Zone
ohne Außenfläche ist zulässig, aber benannt);
**Flächensumme** Σ `Tab_Zone.Nutzflaeche` gegen `Tab_Gebaeude.Nutzflaeche` (E19; > 5 % → Warnung, Hinweis
auf doppelt gezählte Räume); **4-K-Zuordnung** — Anzeige, ob eine Trennfläche als IW oder AW zählt,
mit dem im adiabaten Vorlauf **gerechneten** Δϑ als Beleg (2.2); **Wärmebrücke auf der Zonengrenze** — ψ·L gehört **der Zone,
in der die wärmere Seite liegt**, sonst zählt sie doppelt (Befund O, 5); **Luftstrombilanz** — nur
Paare, Einzelströme werden benannt abgelehnt (2.7).

**Vorgabenanzeige.** Jedes leere Zonenfeld zeigt „Vorgabe: <Gebäudewert>" als Herleitungszeile —
die sichtbare Seite der NULL-Semantik und zugleich die Probe darauf, dass Befund Q-1 behoben ist:
Steht dort eine 0, wo eine 0,3 stehen müsste, ist der Kopierweg noch der alte.

---

## 6. IFC-Import in Zonen

Alles, was Befund N für den Einzonenimport festlegt — Ablauf `Lesen`/`Uebernehmen`,
Meldungsschlüssel `IMP_IFC_PROT_*`, Einheitenauswertung über `IIfcProject.UnitsInContext`,
Azimutkette, Größenlimit (50 MB Windows / 20 MB iOS), Lizenzlage xBIM unter CDDL-1.0 nach
Entscheid E3, iOS-Trimming — gilt unverändert weiter. Hier steht nur, was darüber hinausgeht.

**Was der Einzonenimport schon schreibt (G4b, [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
N1.49).** Mit dem Schalter „Als Zone mit Bauteilen übernehmen“ legt der Import aus IFC und gbXML an der
Projektkopie **eine** Zone je Gebäude an — Regel Z5 dieses Kapitels, Nutzfläche, Volumen und Raumhöhe
der übernommenen beheizten Räume, Quellkennung des Gebäudes —, dazu je Grenzfläche, Fenster und Tür eine
Zeile in `Tab_Bauteil` und je vollständigem Schichtsatz einen Aufbau samt Schichten; die Paarungen stehen
in der Importzuordnung. Grenzflächen gegen unbeheizte oder unbekannte Räume tragen `UNBEHEIZT`
(Kellertemperatur), Grenzflächen zwischen zwei übernommenen beheizten Räumen sind innere Masse: nach
**E45** bei vollständiger Datenlage Zeilen beider Seiten innerhalb der Zone (je Raumbegrenzung), sonst
der Innenflächenfaktor aus der Datei. Kein Namensabgleich nach 6.3 — `ID_Baustoff` bleibt leer, die
Stoffwerte stehen als Kopie an der Schicht. **G6c** bleibt, was darüber hinausgeht: die
Zonierungsregeln Z1…Z4 mit mehreren Zonen, `ID_Nachbarzone` und Randbedingung `ZONE`, der
Namensabgleich N1…N7, die Grundrissansicht; trägt ein Gebäude schon eine Zone, schreibt der Import
keine.

**Was E27 für die Naht des Imports festlegt** (E27 (22.09.2026, [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.32)): Das
IFC-Paket bleibt am Kern, und die Naht `IGebaeudeLeser` wird **von Anfang an** gezogen (**A2**) —
der Zonenimport liest über dieselbe Naht wie der Einzonenimport. Die Lizenzhinweisseite kommt mit
der **ersten** IFC-Stufe ins Installationspaket (**U10**); ohne sie ist auch G6c nicht auslieferbar.
Die Gebäudetabelle bekommt **keine** Herkunftsspalten (**A13**) — die Herkunft des Gebäudes steht
allein in der Importzuordnung; Herkunft je Feld tragen Zone, Bauteil, Aufbau und Baustoff (4.2).

**Der Import merkt sich, woher jede Zeile stammt.** Beim Übernehmen wird die Zuordnung EPOS-Zone ↔
`IfcSpace.GlobalId` und EPOS-Gebäude ↔ `IfcBuilding.GlobalId` **persistiert**, dazu Name, SHA-256
und Zeitpunkt der Quelldatei. Das ist die Voraussetzung dafür, dass EPOS später eine **angereicherte
Datei zurückgeben** kann — der Export der Stufe S2 schreibt Ergebnisse und Kennwerte an dieselben
Entitäten der Ursprungsdatei, ohne Geometrie zu erzeugen
([Befund S](Gebaeudesimulation/2026-09-15_Befund_S_IFC-Export_ohne_Geometriekernel.md),
Abschnitt 6) — und dafür, dass jedes einzelne Feld seine Herkunft behält (3.5). Die Tabelle dazu
definiert das
[Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) (Kapitel 7); hier steht nur
die Anforderung an den Import.

**Umsetzung G6c, Welle A (26.09.2026,**
[Protokoll](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-26_G6c_Zonenimport.md),
[Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) **N1.57).** Die Regeln dieses Kapitels sind im
Kern gebaut, formatfrei und ohne Oberfläche: Z1…Z5 samt Leseregeln und B1…B6 (6.1), die gbXML-Regeln
X1…X4, Polygonflächen ohne Geometriekern, Paarbildung und Gegenprobe (6.2), Mindestgröße und Obergrenze
(6.1, 6.6) und der Bauteilvorschlag für N Zonen mit Trennflächen `ZONE`. Acht Festlegungen der Umsetzung
präzisieren die Regeln (N1.57): Die Gebäudegrundfläche der Mindestgröße ist die Σ der Raumflächen,
Beheizung trennt Zonen, der geometrische Schritt der Paarbildung nimmt den Schwerpunktabstand ≤ Dicke +
1 cm (ohne Dicke 0,6 m) bei entgegengesetzten Normalen, und ohne Raumgrenzen lässt der Kern Z4 mit der
Warnung `GRENZEN_ENTKOPPELT` zu, statt eine vom Anwender eingetragene Trenndecke zu verlangen (6.5). Der
Zuordnungsdialog (6.4) und die Grundrissansicht (6.7) folgen mit den Wellen C und D.

### 6.1 Zonierungsregeln

**In keiner der vier gemessenen Dateien steht eine `IfcZone`** (Befund P, § 1.5) — gezählt wurde
per Textsuche auf `IFCZONE`, und `IFCZONE` ist **kein Teilwort** von `IFCSPATIALZONE`: Die zweite
Entität der thermischen Zone (`IfcSpatialZone` mit `PredefinedType = THERMAL`, ab IFC4) ist damit
nicht gemessen, und die Messung ist vor G6c nachzuholen. Der Leser bildet
deshalb einen **Vorschlag** und legt die Regel offen, nach der er ihn gebildet hat; die erste
Regel, die für die Datei trägt, gewinnt, und der Anwender kann umschalten:

| Rang | Regel | Voraussetzung | Ergebnis an den Messdateien |
|---|---|---|---|
| Z1 | nach `IfcSpatialZone` (`PredefinedType = THERMAL`, Räume über `IfcRelReferencedInSpatialStructure`), sonst nach `IfcZone` (Räume über `IsGroupedBy`) | mindestens eine Zone, Räume eindeutig zugeordnet | `IfcZone` trägt in **keiner** der vier Dateien; für `IfcSpatialZone` steht die Messung aus und ist vor G6c nachzuholen |
| Z2 | nach Klassifikation | alle Räume tragen dieselbe Klassifikationsquelle | FZK-Haus: eine Klasse „000 Allgemeines" für alle sieben Räume → eine Zone |
| Z3 | nach Nutzung (`LongName`) | Nutzungsmuster trifft | FZK-Haus: Schlafen/Bad/Büro/Wohnen/Flur/Küche/Galerie → 4–5 Gruppen |
| Z4 | **nach Geschoss** | Geschosse vorhanden | FZK-Haus 2, DigitalHub 3 Zonen — der robusteste Vorschlag |
| Z5 | eine Zone je Gebäude | immer | der **Einzonen-Rückfall** |

**Vorbelegung: Z4**, sofern mehr als ein Geschoss Räume trägt, sonst Z5 — Z4 ist die einzige Regel,
die in allen vier Messdateien trägt, und sie trifft die Gliederung, die der Rechenkern braucht
(unterschiedliche Randbedingungen an Boden und Dach). **Entschieden mit E50 (M7, 26.09.2026,
[Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.57):** je Geschoss, Rückfall auf die
gröbste Regel Z5, wenn die Raumgrenzen fehlen (6.5); beim gbXML-Import entspricht Z4 die Regel X2, die
Regelkette des [Datenaustauschkonzepts](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 3.3 bleibt.

Drei Leseregeln, alle gemessen: **Der Zonenname kommt aus `LongName`, sonst `Name`, sonst
`ObjectType`, sonst `GlobalId`** — nicht `Name` zuerst, dort steht in beiden Praxisdateien eine
Raumnummer (§ 1.1). **`IfcZone` ist zu entschachteln und Mehrfachzuordnung aufzulösen** — wer die
Räume nicht über den transitiven Abschluss sammelt, zählt Flächen doppelt; ein Raum in mehreren
Zonen gehört im Vorschlag in **keine** (§ 1.5). Und **„Elevation < 0 heißt Keller" ist falsch** —
im DigitalHub liegt das Erdgeschoss bei −0,15 m; die belastbare Regel ist **relativ**:
Untergeschoss ist jedes Geschoss unter dem niedrigsten, dessen Räume Grenzen mit `EXTERNAL` tragen,
ersatzweise unter dem Geschoss mit der kleinsten Elevation ≥ −0,5 m (§ 1.6).

**Beheizt oder unbeheizt** — dieselbe Rangfolge, jede Stufe mit Beleg: B1
`PredefinedType = EXTERNAL`, B2 `Pset_SpaceCommon.IsExternal = TRUE`, B3
`Pset_SpaceThermalRequirements` mit `SpaceTemperatureWinterMin` > 12 °C (nur IFC2x3/IFC4, in 4.3
entfallen) — **der Wert ist vor dem Vergleich über die `THERMODYNAMICTEMPERATUREUNIT` aus
`UnitsInContext` auf °C zu bringen** (KELVIN und DEGREE_CELSIUS sind beide zulässig, und in Kelvin
ist jede Raumtemperatur größer 12); ohne auflösbare Einheit greift B3 nicht, sondern B4
Nutzungsmuster im Namen (`Keller|Garage|Dachboden|Technik|Treppenhaus` und englische
Entsprechungen), B5 Untergeschoss ohne `EXTERNAL`-Grenze, B6 sonst beheizt.

**Mindestgröße.** Eine Zone unter **max(2 m², 2 % der Gebäudegrundfläche)** wird dem Nachbarn mit
der größten gemeinsamen Grenzfläche zugeschlagen; gibt es keinen, bleibt sie stehen und der Dialog
warnt. Der Grund ist rechnerisch: Das 7R2C-Netz kostet je Zone zwei Kapazitäten und einen
Luftknoten; eine 1,5-m²-Abstellkammer bringt keine Aussage, verzerrt aber die Kopplung. **Entschieden
mit E50 (M8, 26.09.2026,** [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) **N1.57):** diese
Mindestgröße samt Zuschlag zum Nachbarn mit der größten gemeinsamen Grenzfläche.

### 6.2 Grenzflächen → Bauteile

**Flächeninhalt.** Über `IfcConnectionSurfaceGeometry` → `IfcCurveBoundedPlane` → Randkurve →
Trapezformel. Gemessen lieferten **81 von 81, 206 von 206 und 2 582 von 2 582** Grenzflächen einen
Wert; `IfcSurfaceOfLinearExtrusion` kam **2-mal von 5 266** vor, `IfcFaceSurface` nirgends
(Befund P, § 2.3) — und `IfcSurfaceOfLinearExtrusion` ist **nicht notwendig gekrümmt**: Ist der
`SweptCurve` eine `IfcPolyline` oder `IfcLine`, ist die Fläche eben und ohne Geometriekernel als
Profillänge × `Depth` zu rechnen; nur ein gekrümmtes Profil wird benannt abgelehnt (Profiltyp der
zwei Fälle noch zu messen). Die Summen 778,2 / 1 358,1 / 22 862,1 m² sind **Bruttosummen aller
Grenzflächen einschließlich der auf ihnen liegenden Öffnungsgrenzen** und dienen als
**Reproduktionsprobe des Lesers**, nicht als Hüllfläche; sie stammen aus einer Textauszählung
(Befund P, § 8) und sind beim ersten Lauf mit xBIM nachzumessen. Abnahmegrundlage sind allein die
lizenzgeklärten Dateien (8.2, M10).

Drei Umsetzungsfallen: Die Randkurve ist mal eine `IfcPolyline`, mal eine `IfcCompositeCurve` →
`IfcCompositeCurveSegment` → `IfcPolyline` (beide Wege nötig, `IfcIndexedPolyCurve` dazu); die
Punkte sind mal 2D, mal 3D (FZK-Haus) — **der Leser projiziert 3D-Punkte auf die beiden Achsen der
`IfcPlane.Position` (`RefDirection` und `Axis × RefDirection`)** statt zwei Koordinaten
herauszugreifen: Das System ist orthonormal, der Polygoninhalt bleibt exakt, und eine senkrechte
Wand mit konstanter x-Koordinate fällt nicht durch; streut der Abstand zur Ebene um mehr als 1 mm,
ist die Berandung nicht eben (Meldung). Und der Ring ist mal geschlossen, mal
nicht — ein roh angehängter letzter Punkt erzeugt eine Dreiecksfläche zu viel.

**Orientierung und Neigung** aus der Normalen der `IfcPlane` über die Placement-Kette **des
`RelatingSpace`** — `SurfaceOnRelatingElement` ist im LCS des Raumes gegeben, nicht im LCS des
Bauteils; `SurfaceOnRelatedElement` liegt im LCS des Bauteils und ist nicht mit der ersten zu
mischen — und `TrueNorth` (außer bei `IfcMapConversion`, dann nur informativ): Azimut = Winkel der
projizierten Normalen, 0° = Nord im Uhrzeigersinn; Neigung = Winkel zur z-Achse, 90° Wand, 0°
Decke, 180° Boden.

**Die Gegenprobe gehört in den Leser und ist zweiteilig**, weil Σ oben ≈ Σ unten gegen eine globale
Spiegelung blind ist — bei gekipptem Vorzeichen tauschen beide Summen nur die Plätze: (1) je Zone
muss Σ A_v·n_v betragsmäßig nahe null sein (geschlossene Hülle); (2) die Zuordnung oben/unten wird
an einem Bauteil mit `PredefinedType = BASESLAB` bzw. an einer Grenze mit `EXTERNAL_EARTH`
verankert — zeigt deren Normale nach oben, ist die Kette für die ganze Datei zu spiegeln. Eine
Bodenplatte, deren Normale nach oben zeigt, ist ein Vorzeichenfehler, kein Dach.

**Nachbarzone.** `CorrespondingBoundary.RelatingSpace` ist ein `IfcSpaceBoundarySelect`, also
`IfcSpace` **oder `IfcExternalSpatialElement`**. Nur der Raumfall führt auf eine Zone; ein
`IfcExternalSpatialElement` ist die Außenwelt, wird nach seinem `PredefinedType` auf
`AUSSENLUFT`/`ERDREICH` abgebildet und **nie als Zone geführt**. Dieselbe Prüfung gilt für
`RelatingSpace` der Grenze selbst. Wo echte
`IfcRelSpaceBoundary2ndLevel`-Entitäten stehen, ist das Attribut **lückenlos** gefüllt: 124/124,
1 472/1 472, 1 724/1 724 (Befund P, § 2.2). **Die Archicad-Basisklasse hat es gar nicht** — sie
trägt neun Attribute und schreibt `Name='2ndLevel'`, `Description='2a'` pauschal auch auf die 41
Außengrenzen, die kein Gegenstück haben können; der Typ ist deshalb **nicht aus dem Text zu
übernehmen**, sondern aus `InternalOrExternalBoundary` und dem gefundenen Gegenstück abzuleiten.
Rekonstruktion: (1) Kandidaten = gleiches `RelatedBuildingElement`, anderes `RelatingSpace`,
`PHYSICAL`; (2) eindeutig bei genau einem Kandidaten — im FZK-Haus tragen 24 Bauteile eine Grenze
und 11 genau zwei, aber 2 drei, 2 vier, 1 sechs und **1 Bauteil 15 Grenzen**, dort scheitert die
Zuordnung bei sechs Bauteilen; (3) sonst Geometrie, **beide Flächen zuvor über die Placement-Kette
ihres jeweiligen Raumes in Weltkoordinaten überführt**: gleicher Flächeninhalt innerhalb 1 % **und**
Schwerpunktabstand kleiner als die Bauteildicke; (4) bleibt es mehrdeutig, grenzt die Fläche gegen
„unbekannt". **Entschieden mit E50 (M13, 26.09.2026, [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
N1.57): die Rekonstruktion wird vollständig gebaut**, Schritte (1) bis (4) samt Paarbildung über die
Geometrie — Mehrzonigkeit auch für Dateien ohne echte Paare, die kleine lizenzfreie Referenzdatei bleibt
nutzbar (Probe 18).

**Randbedingung** kommt direkt aus `InternalOrExternalBoundary` — gemessen im DigitalHub
`.INTERNAL.` 1 538, `.EXTERNAL.` 967, **`.EXTERNAL_EARTH.` 76**:

| IFC | EPOS-Randbedingung |
|---|---|
| `EXTERNAL` | `AUSSENLUFT`, mit Orientierung und Neigung |
| `EXTERNAL_EARTH` | **`ERDREICH`** — je Bauteil statt je Gebäude |
| `INTERNAL`, Gegenstück in derselben Zone | entfällt (innere Masse, zählt in A_IW) |
| `INTERNAL`, Gegenstück in anderer Zone | `ZONE` mit `ID_Nachbarzone` |
| `INTERNAL` ohne Gegenstück | `UNBEHEIZT` als Vorgabe, Zeile rot im Dialog |
| `EXTERNAL_WATER`, `EXTERNAL_FIRE`, `NOTDEFINED` | benannt abgelehnt |

**Bauteilart** aus dem Typ von `RelatedBuildingElement` plus `PredefinedType`: `IfcWall` →
Außen-/Innenwand (die Randbedingung entscheidet, **nicht** `Pset_WallCommon.IsExternal`), `IfcSlab`
`ROOF`/`IfcRoof` → Dach, `BASESLAB` → Bodenplatte, `FLOOR` → Decke (Neigung entscheidet über
oben/unten), `IfcWindow` → Fenster, `IfcDoor` → Tür, `IfcColumn`/`IfcBeam`/`IfcMember` →
Sonstiges (im DigitalHub 150 + 42 Grenzen mit zusammen 198,6 m² — **nicht** zur Außenwand zählen,
sie sind innere Masse); `IfcCurtainWall`/`IfcPlate` → **eigene Bauteilart „Vorhangfassade"** mit
U-Wert und g-Wert wie ein Fenster (im DigitalHub 2 Grenzen / 36,0 m², in den 198,6 m² **nicht**
enthalten), sonst verliert das Modell die solaren Gewinne einer Pfosten-Riegel-Fassade; `VIRTUAL`
→ **keine Bauteilfläche**, aber eine benannte Luftverbindung: Liegen beide Räume in derselben Zone,
entfällt sie; liegen sie in verschiedenen Zonen, wird sie als **Zonen-Luftaustausch nach 2.7**
vorgeschlagen (Fläche als Beleg), nie stillschweigend verworfen — das Zusammenlegen zweier Räume
ist ein Vorschlag im Dialog, keine Leserregel (FZK-Haus: 6 virtuelle Grenzen / 36,6 m²). Der Leser
prüft **jede**
Pflichtangabe gegen `null`: Im FZK-Haus tragen 5 Grenzen kein auflösbares
`RelatedBuildingElement`, obwohl das Attribut ab IFC4 Pflicht ist.

**Öffnungsabzug.** Die Norm ist eindeutig und die Folge unbequem: Die Wandgrenze enthält die
Fensteröffnung, die Fenstergrenze liegt zusätzlich darauf — „both overlap". Gemessen schneiden nur
0 von 81, 1 von 206 und 116 von 2 582 der 2nd-Level-Ebenen ihre Öffnungen geometrisch aus. **Die
Regel entscheidet je Fläche, nicht je Datei:** A_netto = A(OuterBoundary) − Σ A(InnerBoundaries der
Ebene) − Σ A(Kindgrenzen über `InnerBoundaries`/`ParentBoundary`). In den gemessenen Dateien
schließen sich der zweite und der dritte Term erfahrungsgemäß aus; **belegt ist das nicht**. Der
Leser rechnet beide, zieht höchstens einmal ab und **warnt, wenn beide gleichzeitig größer null
sind** — das wäre doppelter Abzug. **Sind beide Terme null, obwohl in der Ebene dieser Wandgrenze
Fenster- oder Türgrenzen liegen**, ist kein Abzug erfolgt: Der Leser prüft geometrisch (Polygon der
Öffnungsgrenze in der Ebene und innerhalb des Wandpolygons) und zieht einmal ab; gelingt das nicht,
greift der Ersatz je Zone — Fehlerbild `IMP_IFC_PROT_OEFFNUNG_OHNE_ABZUG` (W). **Vor G6c zu
messen:** wie viele der 2 582 DigitalHub-Grenzen ein `ParentBoundary` tragen. Fehlt die
Eltern-Kind-Beziehung ganz (Archicad-Basisklasse), bleibt der billige Ersatz, **getrennt nach
Randbedingung**: Von der Summe der Außenwandgrenzen einer Zone wird nur die Summe der Fenster- und
Türgrenzen **mit `EXTERNAL`/`EXTERNAL_EARTH`** abgezogen, von den Innenwandgrenzen nur die inneren;
Zahl und Fläche sind im Dialog zu zeigen (gemessen: FZK-Haus 8 Türgrenzen / 17,5 m², DigitalHub
136 / 307,5 m² — überwiegend innen).

**Raumseitenmaß.** Raumgrenzen sind Raumseitenflächen und damit systematisch kleiner als das
Bruttomaß, das die Bemaßungsregel verlangt — belegt in VDI 2078, 6.1, S. 18 (Innenbauteile netto,
Außenbauteile brutto, anders temperierte Nebenräume netto, Fenster einschließlich Rahmen; damit ist
der Merkposten aus Konzept N1.11 erledigt). **Entschieden mit E27 (M2): Raumseitenmaß
durchhalten und im Dialog benennen** — eine halbe Umrechnung erzeugt eine Hülle, die weder brutto noch netto ist, und die
Umrechnung selbst bräuchte Bauteildicken und die Gehrung an jeder Ecke, also Geometrie. Die
Abweichung ist zu **beziffern** (Kapitel 8, Probe 17); das Bruttomaß bleibt als verworfene
Möglichkeit Teil der Begründung.

### 6.3 Materialien → Aufbauten

Die Kette `IfcMaterialLayerSetUsage` → `IfcMaterialLayerSet` → `IfcMaterialLayer` hängt am Bauteil
über `IfcRelAssociatesMaterial`. **Fehlt sie dort, ist sie am Typ zu suchen:** `IIfcObject.IsTypedBy`
→ `IfcRelDefinesByType.RelatingType` → dessen `HasAssociations`. Am Vorkommen steht die `…Usage`
(mit Lage und Richtung), am Typ der nackte `IfcMaterialLayerSet` (ohne Lage) — beide Wege sind
Pflicht, der Typweg liefert die Schichten, aber keine Seitenzuordnung; er ist der Regelfall in
Revit- und Archicad-Exporten, und ein Leser nur über die Vorkommenszuordnung fiele dort für das
ganze Gebäude in den masselosen Rückfall nach 3.6. Gemessen führen FZK-Haus, FZK-mit-SB und Institute je **4
Schichtsätze mit je einer Schicht** — der Schichtaufbau ist dort gar nicht modelliert, nur die
Gesamtdicke mit einem Sammelnamen; der DigitalHub führt 20 Sätze mit 38 Schichten, im Mittel 1,9
(Befund P, § 3.1). Auch das ist die Konstruktion des Architekten, nicht die des Bauphysikers.

**Welche Schicht ist innen?** Das entscheidet über die thermische Masse, denn nur die raumseitigen
Schichten zählen in C₁. Die Liste läuft von der MlsBase in Richtung `DirectionSense` entlang
`LayerSetDirection` (Wände AXIS2, Platten AXIS3); die MlsBase liegt bei `OffsetFromReferenceLine`,
beide sind **unabhängig**. Ist eine Schicht ein `IfcMaterialLayerWithOffsets`, gelten ihre
`OffsetValues` gegen `ReferenceExtent` des `…Usage` (nach Spezifikation dann Pflichtangabe); für die
Reduktion nach Gl. (1)–(17) wird die **mittlere** Dicke genommen und der Fall im Dialog benannt. **Welche Seite raumseitig ist, sagt der Schichtsatz nicht** — das sagt
erst die Raumgrenze: Ist die Projektion ihrer Normalen auf die Achse gleichgerichtet mit
`DirectionSense`, steht die **letzte** Schicht raumseitig, sonst die erste. Fehlt das `…Usage`,
**gibt die Spezifikation keine Lage an** (`IfcMaterialLayerSet`). EPOS nimmt dann die erste Schicht
als außenliegend an — eine **EPOS-Annahme mit 50 % Irrtumswahrscheinlichkeit je Bauteil**; sie ist
als solche zu benennen, im Dialog vorzulegen und umschaltbar.
**Im Mehrzonenmodell ist die Frage zweiseitig** — dieselbe Liste einmal vorwärts, einmal rückwärts;
dafür ist `CorrespondingBoundary` gut, weil die zweite Grenze die zweite Normale liefert, und die
Probe ist, dass beide entgegengesetzt sind (§ 3.3).

**Stoffwerte** hängen **nicht** über `IsDefinedBy`/`IIfcPropertySet` am Material — das ist der Weg
für Objekte —, sondern über `HasProperties : IEnumerable<IIfcMaterialProperties>` auf
`IIfcMaterialDefinition` (dort deklariert und von `IIfcMaterial` **geerbt**, also ohne Umwandlung
abrufbar); wer dort nach einem
`IfcPropertySet` sucht, findet nie etwas. `Pset_MaterialThermal` trägt vier Eigenschaften
(`SpecificHeatCapacity`, `BoilingPoint`, `FreezingPoint`, `ThermalConductivity`),
`Pset_MaterialCommon` die `MassDensity`; **`ThermalConductivityTemperatureDerivative` gibt es dort
nicht** — sie stammt aus einer fremden Ontologie und wird nicht gelesen (§ 3.2).

**Fenster:** `IfcMaterialConstituentSet` ist für Rahmen und Glas vorgesehen, in der Praxis aber ein
zweiter Weg, mehrschalige Wände zu beschreiben — „Lining"/„Glazing" kommen nicht vor. Brauchbar
sind U_w aus `Pset_WindowCommon.ThermalTransmittance` und, mit Vorbehalt, der g-Wert aus
`Pset_DoorWindowGlazingType.SolarHeatGainTransmittance` — DigitalHub 85 Vorkommen, davon **79 mit
Wert 0.**, also **7 % brauchbar**, FZK-Haus 0 Vorkommen; `GlazingAreaFraction` ist der
**Glasflächenanteil** F_F, nicht der Rahmenanteil (Rahmenanteil = 1 − `GlazingAreaFraction`; Band
für F_F: [0,4; 0,95]) und steht in **keiner** der vier Dateien. **Also bleiben g-Wert und Rahmenanteil Vorgaben**, solange kein
Wert im Band (g in [0,1; 0,9], Rahmenanteil in [0,05; 0,6]) gefunden wird.

**Namensabgleich** nach der Kette N1…N7 aus 3.5, zweisprachig: Die gemessenen Namen sind deutsch,
ein Revit-Export aus englischer Vorlage liefert „Concrete, Cast-in-Place" und „Air".

### 6.4 Der Zuordnungsdialog

Ein Dialog, vier Abschnitte, ein OK. Der Aufbau folgt dem Konfliktdialog
(`EPOS.UI/Dialoge/Import/ImportKonflikteDialog.razor`), die Listen der virtualisierten
`Katalogliste`, weil die Institute-Datei 78 **Räume** und über 2 000 Flächen liefert; unter der
Vorbelegung Z4 werden daraus wenige Zonen, unter einer Regel „je Raum eine Zone" 78. (1) **Kopf** —
Datei, Schema, Gebäudewahl, Zonenregel Z1…Z5, Bilanz (Zonen, beheizte Fläche, Volumen, Σ
Außenfläche, Σ Trennfläche) und Warnbanner mit der schwersten Meldungsstufe. (2) **Zonen** — je
Zone Name, Regel, Räume, Fläche, Volumen, Haken „beheizt", Knöpfe „zusammenlegen"/„trennen";
aufgeklappt die Raumliste mit `LongName`, Geschoss, Fläche, Beheizungsregel B1…B6 und Beleg.
(3) **Flächen je Zone** — Bauteilart, Fläche, Azimut, Neigung, Randbedingung, Nachbarzone, U-Wert,
Aufbau, Herkunft, Beleg, mit den Filtern „nur Fehler", „nur ohne Gegenstück", „nur ohne U-Wert",
„nur ohne Stoffwerte". (4) **Baustoffe** — IFC-Name, Abgleichstufe N1…N7, zugeordneter Baustoff,
λ/ρ/c, Herkunft; **diese Liste ist die Arbeit des Anwenders**, und sie ist kurz: 6 bis 13 Namen je
Datei. (Abschnitt (4) ist mit G4b für den Einzonenimport umgesetzt — Abschnitt „Baustoffe“ im
Gebäudeimport mit eigener Zuordnung, die das Projekt beim Speichern merkt; Leitkonzept N1.49.)

**Name und Ort sind mit E27 entschieden:** Der Dialog trägt einen **formatfreien** Namen (**A3**)
— er zeigt IFC und gbXML, und ein Format im Namen einer Maske, die zwei Formate trägt, wäre eine
Unwahrheit (Datenaustauschkonzept 1.4, Nr. 6). Er bekommt **keinen eigenen Maskenschlüssel und
keine Menüzeile**, sondern erscheint als **Überlagerung im Gebäudedialog** (**A17**).

**Was der Anwender ändern kann:** Regel wählen (der Vorschlag wird neu gebildet, Handeingriffe nach
Rückfrage verworfen), Zone umbenennen, Räume zusammenlegen (Innengrenzen zwischen ihnen entfallen),
Zone trennen (vormals interne Flächen werden zu Trennflächen), Haken „beheizt" (die angrenzenden
Flächen wechseln ihre Randbedingung), „alles in eine Zone" (der Einzonen-Rückfall). Nach jeder
Änderung rechnet der Dialog die Bilanz neu.

**Vier Regeln, die nicht verhandelbar sind:** Nichts wird ohne OK geschrieben; **jede Zahl trägt
ihre Herkunft und ihren Beleg** — eine Zelle ohne Beleg ist eine Vorgabe; kein Anzeigetext ist
Steuerwert (`ImportKonfliktModell.cs:41-46`); die Plausibilitätsprüfungen laufen **vor** dem
Schreiben, dazu neu die geschlossene Hülle je Zone, die Trennflächenbilanz und die Summe der
Zonenflächen gegen die Summe der Raumflächen (5.3).

### 6.5 Rückfälle und Sonderfälle

**Ohne Raumgrenzen** (Allplan 2023 exportiert keine; Duplex Apartment nur 1. Ebene) fällt die
Topologie weg: Bauteile über `IfcRelContainedInSpatialStructure` dem Geschoss zuordnen, Flächen aus
den Quantity-Sets, Außen/Innen aus `Pset_*Common.IsExternal`. **Nachbarschaft gibt es dann nicht** —
zwei Geschosszonen ohne Grenzen wären thermisch entkoppelt, und das ist falsch. Deshalb bietet der
Leser bei fehlenden Grenzen **Z5 als Vorgabe** an und Z4 nur, wenn der Anwender die Trenndecke
selbst einträgt: **Eine stillschweigend entkoppelte Mehrzonenrechnung wäre schlechter als die
Einzonenrechnung.** Das ist der Rückfall aus M7 (entschieden mit E50, 26.09.2026). **Ohne Stoffwerte** gilt 3.6: masselos mit U-Wert, Masse aus `Bauweise`, je
Zone entschieden.

**Weitere Sonderfälle:** Ein Pset ist **nur über den Namen** zu erkennen, nie über die erwartete
Eigenschaftsliste — `Pset_SpaceCommon` im FZK-Haus führt kein `IsExternal`, dafür die fremden
`NaturalVentilation` und `Category`; unbekannte Eigenschaften sind folgenlos zu übergehen.
`Pset_SpaceOccupancyRequirements` liefert nur **Vorschläge** für Nutzungsgruppe und innere Gewinne,
nie eine stille Übernahme (die Belegung ist Planungsvorgabe, keine Betriebsgröße). Mehrere
`IfcBuilding` je Datei sind möglich — ein EPOS-Gebäude je `IfcBuilding`, eines je Lauf; Räume ohne
Gebäudezuordnung kommen in einen Sammelposten mit Warnung, **nicht** stillschweigend zum einzigen
Gebäude. Einheiten immer aus `UnitsInContext` auflösen, auch wenn alle vier Messdateien Meter
führen: Ein Millimetermodell verschiebt die Flächen um 10⁶, und das fällt an keiner Zahl auf.
**IFC2x3** kennt `Pset_MaterialThermal` nicht, sondern `IfcThermalMaterialProperties` — **am
xBIM-Quelltext entschieden:** `HasProperties` liefert in 2x3 die `IfcThermalMaterialProperties` und
`IfcGeneralMaterialProperties` (`Xbim.Ifc2x3/Interfaces/IFC4/IfcMaterial.cs:103`), aber mit
`Name == null` (`Xbim.Ifc2x3/Interfaces/IFC4/IfcMaterialProperties.cs`; einen Namen trägt in 2x3
nur `IfcExtendedMaterialProperties`). Der Leser darf deshalb **nicht** auf
`Name == 'Pset_MaterialThermal'` filtern — er fände dort nie etwas, obwohl die Werte da sind —,
sondern sammelt die Eigenschaften nach **ihrem eigenen Namen** (`ThermalConductivity`,
`SpecificHeatCapacity`, `MassDensity`); der Pset-Name ist Beleg, nicht Schlüssel. **Früh zu
messen** bleibt allein, ob eine reale 2x3-Datei diese Eigenschaften überhaupt füllt.

### 6.6 Fehlerbilder

Alle als `PruefMeldung` mit Schlüsseln in **beiden** `.resx`, danach `ResourceDesigner`; das
Protokoll ist die Meldungsliste in Reihenfolge und wird nicht geschrieben (Befund N, 4.5).

| Bild | Erkennung | Wirkung | Schlüssel |
|---|---|---|---|
| keine Raumgrenzen / nur 1. Ebene | `BoundedBy` leer bzw. keine 2ndLevel-Entität und kein `'2nd'` in Name/Description | Z5 vorgeben, Flächen aus Quantities | `IMP_IFC_PROT_KEINE_GRENZEN`, `…_NUR_1STLEVEL` (W) |
| Fläche ohne Gegenstück | `INTERNAL`, Rekonstruktion mehrdeutig | Randbedingung `UNBEHEIZT`, Zeile rot | `IMP_IFC_PROT_OHNE_GEGENSTUECK` (W) |
| Bilanzlücke | Σ Trennfläche A→B ≠ B→A (> 2 %) | beide zeigen, größere nehmen | `IMP_IFC_PROT_TRENNFLAECHE_UNGLEICH` (W) |
| Überlappung | Innenränder **und** Kindgrenzen auf derselben Fläche | nur einmal abziehen | `IMP_IFC_PROT_UEBERLAPPUNG` (W) |
| Öffnung größer als Wand | A_netto < 0 | A = 0, Zeile rot | `IMP_IFC_PROT_NETTOFLAECHE_NEGATIV` (F) |
| Stoffwert null/außerhalb; Baustoff unbekannt | λ, ρ, c außerhalb des Bandes; Abgleich ohne Treffer | Namensabgleich bzw. Anwenderzuordnung, Zeile gelb | `IMP_BAUTEIL_PROT_STOFFWERT_UNGUELTIG`, `…_BAUSTOFF_UNBEKANNT` (W; formatfrei, weil der Bauteilvorschlag IFC und gbXML trägt — umgesetzt mit G4b, Konzept N1.49) |
| Zone ohne Hülle / zu klein | keine Grenze `EXTERNAL`/`EXTERNAL_EARTH`; Fläche < max(2 m², 2 %) | zulässig, aber benannt; Vorschlag zusammenlegen | `IMP_IFC_PROT_ZONE_OHNE_AUSSEN`, `…_ZONE_ZU_KLEIN` (I) |
| Raum in mehreren Zonen | Mehrfachzuordnung über `IfcZone` | Raum bleibt unzugeordnet | `IMP_IFC_PROT_RAUM_MEHRFACH` (W) |
| Öffnung ohne Abzug | beide Abzugsterme 0, obwohl Öffnungsgrenzen in der Ebene liegen | geometrisch prüfen, einmal abziehen, sonst Ersatz je Zone | `IMP_IFC_PROT_OEFFNUNG_OHNE_ABZUG` (W) |
| nicht auswertbare Grenzfläche | `IfcSurfaceOfLinearExtrusion` **mit gekrümmtem `SweptCurve`**, `IfcFaceSurface`, sonstige | Fläche leer, Zeile rot | `IMP_IFC_PROT_FLAECHE_UNBEKANNT` (W) |
| zu viele Zonen | N > 50 (M12, entschieden mit E50) | **Warnung mit Rückfrage, Vorschlag „auf Geschosse zusammenlegen"** | `IMP_IFC_PROT_ZU_VIELE_ZONEN` (W) |

### 6.7 Grundrissansicht im Zuordnungsdialog (E11)

**Anwenderentscheid E11** (15.09.2026, [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
N1.16): Der Zuordnungsdialog aus 6.4 bekommt eine **zeichnende Fläche** — den Grundriss je Geschoss.
Sie ist keine Zugabe, sondern das Werkzeug für die Arbeit, die dieser Dialog verlangt: 78 Räume in
wenige Zonen zu ordnen, ist an einer Liste mühsam und an einem Bild eine Handbewegung.

**Was die Ansicht zeigt.** Je Geschoss die **Raumgrenzenpolygone** als SVG-Polygone, Farbe je Zone,
unzugeordnete Räume grau, dazu die Geschosswahl. Die Polygone kommen aus den Raumgrenzen (6.2:
`IfcSurfaceOfLinearExtrusion` mit gerader `SweptCurve`, Polygonflächeninhalt ohne Geometriekernel),
das Geschoss aus `IfcBuildingStorey`.

**Was der Klick tut.** Ein Klick auf einen Raum wählt ihn und ordnet ihn der im Kopf gewählten Zone
zu — dieselbe Wirkung wie „zusammenlegen"/„trennen" in der Zonenliste (6.4), nur am Bild; danach
rechnet der Dialog seine Bilanz neu. **Nichts wird ohne OK geschrieben**, und **kein Anzeigetext ist
Steuerwert** — die vier Regeln aus 6.4 gelten unverändert.

**Technik und gemeinsames Fundament.** SVG in einer **Razor-Komponente ohne Bibliothek**
(Arbeitsname `GebaeudeAnsicht.razor`, Umschalter „Grundriss | Körper"); die Komponente liest ein
**Zonengeometrie-Modell** im Kern (Arbeitsname; die [Softwarearchitektur](Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md) 1.3 legt es als `Zonengeometrie` mit `Zonenumriss` fest): je Zone Grundrisspolygon, Höhe, Geschoss und die Zuordnung der
Bauteile zu den Polygonkanten bzw. zu Boden und Decke. **Dasselbe Modell speist den Export** — die
zweite Ansicht mit schematischen Körpern (three.js, lokal ausgeliefert), den gbXML-Export G7b
(`PolyLoop`) und den IFC-Export G7e (`IfcExtrudedAreaSolid`); Einzelheiten im
[Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md), Nachtrag 1. Der
**gbXML-Import** speist dieselbe Ansicht über `Space`/`Zone`.

**Ohne Raumgrenzen** (6.5) gibt es kein Polygon. Dann bildet das Modell je Zone ein **Rechteck** aus
Zonenfläche und dem Seitenverhältnis der Bauteilgruppen (h = V/A, l = A_NS/(2·h), b = A_OW/(2·h))
und reiht die Zonen je Geschoss. **Diese Anordnung ist erfunden, und das steht sichtbar am Bild:**
„schematisch" ist Pflicht in der Oberfläche, nicht nur in der Exportdatei. Jede Zone weist außerdem
aus, ob ihr Polygon aus Raumgrenzen oder aus der Rechteckherleitung stammt — dieselbe
Herkunftsregel, die 6.4 für jede Zahl verlangt.

**Abnahme.** bunit-Test der Komponente (Polygon je Zone mit Zonenfarbe, Klick meldet die Zone an den
Wirt, Kennzeichnung im gerenderten Baum) und die Probe auf **Determinismus der Geometrie**: gleiche
Eingabe, gleiche Polygone, byteweise gleicher Export.

**Aufwand:** 3–5 PT für das Zonengeometrie-Modell und 3–5 PT für die Ansicht, zusammen **6–10 PT**
in G6c (Kapitel 9).

---

## 7. Ergebnis, Bericht, Wiki

**Ergebnis je Zone.** Die vier Reihen aus Konzept 4.6 (`Heizlast`, `Raumtemperatur`,
`OperativeTemperatur`, `Kuehlbedarf`) und die Kennzahlen entstehen je Zone; in den Kanal geht
ausschließlich die Gebäudesumme (2.8). Unbeheizte Zonen tragen keine Heizlast, aber Temperatur und
die Kennzahl `Ueberhitzungsstunden` — im Bedarfsdialog tragen sie eigene Zeilen ohne Energie
(**E49/A2**, umgesetzt mit G6b).

**Bericht.** Vorbild ist die echte Tabelle je Teilobjekt, nicht der Eigenschaftsblock: Die
Speichertemperaturen in `EPOS.Kern/Allgemein/Bericht/Bausteine/BausteineProjekt.cs:105-140` zeigen
die Bauform (Spaltenbreiten als `int[]`, `k.NeueTabelle(w)`, Kopfzeile mit
`WordBerichtGenerator.HEAD_FILL`, eine `TableRow` je Objekt, fehlende Werte als „—"), und `:89-93`
die Regel: **Der Abschnitt entfällt vollständig, wenn kein Objekt einen Wert trägt** — „Eine Tabelle
voller ‚—' wäre keine Aussage, sondern eine Frage." Das Gebäude steht heute als Eigenschaftsblock
(`:35-52`, Daten aus `…/ProjektDetails.cs:39-40`, gefüllt `:72`); die Zonentabelle tritt daneben:
Zone | Fläche | Volumen | H_T [W/K] | H_ve [W/K] | Heizwärme [kWh/a] | Spitze [kW] | beheizt.
**Kennzahlen** (`…/KennzahlenKatalog.cs:204` ff.) kennen **keine Objektlisten** — als Kennzahl je
Projekt taugt nur eine Summe oder ein Extremum; H_T je Gebäude ist mit E2 vorgesehen (N1.6).

**Wiki.** Eine Seite „Mehrzonenmodell" unter `Projekte/Wiki/`, nach den Regeln der
Wurzel-`CLAUDE.md`: nur die Funktion, wie sie ist, keine Änderungsvermerke (die gehören ins
„Update-Logbuch", mit Datum und beim Anwender erfragter Versionsnummer), **keine Hersteller- und
Produktdaten** — Baustoffe nach Norm sind keine Produktdaten, ein Herstellerdämmstoff wäre einer;
der Wächter `EPOS.Kern.Tests/WikiProduktdatenWacheTests` hält die Repo-Quellen gegen die
Katalognamen der Testdatenbank — **`Tab_Baustoff(_STAMM)` wird deshalb NICHT in die
Gerätekatalogliste des Wächters aufgenommen** (`WikiProduktdatenWacheTests.cs:93-94` liest heute
acht Gerätekataloge); sonst fiele jede Wiki-Seite mit dem Wort „Stahlbeton". **Die Produktaussage
lautet im Wortlaut von Entscheid E10 „Rechenkern nach VDI 6007 Blatt 1; elf der zwölf
Testbeispiele im Normband einschließlich Druckrundung, Testbeispiel 11 in zwei Umschaltstunden um
 3,4 W daneben (3,9 W gegen das Band ohne Druckrundung)" — je Zone**, und sie darf **nicht** zu „Mehrzonensimulation nach VDI 6007" werden
(Befund O, 5).

---

## 8. Tests und Abnahme

### 8.1 Rechenweg

| Nr. | Probe | Kriterium |
|---|---|---|
| 1 | **Testbeispiel 10 als Kopplungspfadnachweis** — Keller als zweite Zone, ideal auf 15 °C; dazu ein zweiter Lauf mit **frei schwingendem** Keller | Zone 1 im Normband nach 6.6, S. 31 mit der Druckrundung aus E10 (± 0,15 K, ± 1,5 W) für Tag 1, 10, 60; zugleich **bitgleich** zur Einzonenrechnung mit vorgegebener Nebenraumtemperatur — und das trägt **nur**, weil die Kellerzone im ersten Lauf ideal auf ihrem vorgegebenen Wert gehalten wird und Zone 1 damit Stunde für Stunde dieselben θ_NR in denselben Code bekommt; im zweiten Lauf mit frei schwingendem Keller gilt es nicht mehr. Die Rückkopplung selbst belegt erst der zweite Lauf gegen Probe 5 (2.10) |
| 2 | **Zwei identische Zonen = eine Zone** — dieselbe halbe Zone zweimal, Trennwand adiabat | Zonenreihen paarweise bitgleich zueinander (identischer Code, identische Eingaben); Gebäudesumme gegen die Einzonenrechnung des ganzen Gebäudes: Jahresenergie 1e‑9 relativ, Stundenwerte 1e‑6 K — zwei Halbzonen durchlaufen eine andere Zahl und Reihenfolge von Gleitkommaoperationen. Bricht die Probe, stimmt die Flächenaufteilung, die Halbierung der adiabaten Trennwand (2.2) oder Gl. (29)/(31) nicht |
| 3 | **Adiabate Symmetrie** — zwei Zonen, gleicher Sollwert, Trennfläche einmal als IW (4-K-Regel) und einmal ausdrücklich als AW | Einen Strom „über die Trennfläche" weist das 2-K-Modell nicht aus (Gl. (28), S. 17 fasst die AW-Gruppe zu **einem** R_Rest,AW zusammen); gemessen wird an der Gewichtung und an der Jahresenergie: im AW-Fall weicht der auf die Trennfläche entfallende Summand B_NR·θ_NR,eq der Gl. (41)/(42) je Stunde um weniger als 0,01 K von B_NR·θ_air der eigenen Zone ab — der Antrieb über die Trennfläche verschwindet also —, und die Jahresenergie beider Rechnungen liegt innerhalb 0,1 %. Bleibt ein Rest, stimmt die 4-K-Zuordnung oder die Reduktion nach Gl. (17) nicht |
| 4 | **Energiebilanz über alle Zonen** einschließlich der Zonenströme | Jahresbilanz je Zone aus Lüftungs-, Zonenluft- und AW-Zweigströmen gegen die Änderung der Speicherinhalte, Abweichung < 0,1 % der Jahresenergie; die Zonenluftströme summieren sich **stündlich** zu null. Einen Strom „über die Trennfläche" gibt es im 2-K-Modell nicht (Gl. (28), S. 17 fasst die AW-Gruppe zu **einem** R_Rest,AW zusammen). **Das ist die Probe, die die doppelte Reduktion derselben Trennwand prüft** (2.2) |
| 5 | **Iteration gegen exakte Lösung** — N = 2, Vorschlag B gegen das 4×4-Gesamtsystem | alle Stundenwerte < 0,001 K und < 0,1 W |
| 6 | **Vorstunde gegen Iteration** — A gegen B auf einem Gebäude mit unbeheiztem Keller und einem mit Treppenhaus-Luftaustausch | misst maximale Stundenabweichung und Jahresenergieabweichung je Zone. **Ergebnis ist die Begründung der Wegwahl, nicht deren Voraussetzung** |
| 7 | **Determinismus** — zwei Läufe; zusätzlich umgekehrte Eingabereihenfolge der Zonen | byte-gleiche Reihen (eigene Probe in `EPOS.Kern.Tests` und Referenzlauf) |
| 8 | **Nichtkonvergenz und Pendelstunde** — künstlich stark gekoppelte Zonen, Höchstzahl erreicht; dazu eine Stunde, in der die Regelungszuordnung zwischen zwei Durchläufen umschlägt | **benannter Fehler**, Abbruch, kein stiller Rückfall (6.8, S. 36); in der Pendelstunde wird das Muster des ersten Durchlaufs gehalten und der Wechsel gezählt (2.4) |
| 9 | **Grenzfälle der Bauteilzuordnung** — Zone ohne AW, Zone ohne IW, A_AW > A_IW | 6.8, S. 36/37 verlangt die Behandlung ausdrücklich: ohne zusammengefasste IW darf nicht durch deren Fläche geteilt werden; für einen Raum ohne AW ist die Koeffizientenmatrix mit 10¹² statt 0 zu belegen; der dritte Fall ist der Umschaltpunkt (29) → (31). Dazu die stehende Zusicherung **Σ B_v = 1 je Zone** (Gl. (42), 2.3) |
| 10 | **Referenzlauf** — alle vierzehn Referenzprojekte, jedes Gebäude als **eine** Zone | **bitgleich** zur Einzonenrechnung **desselben Programmstands** (nach G3). Das trägt nur wegen der Ausnahme N = 1 (2.4, 2.9): bei genau einer Zone entfallen adiabater Vorlauf, Iteration und Konvergenzprobe, sodass derselbe Code in derselben Reihenfolge läuft. Ohne diese Probe wird jede Mehrzonenstufe zu einem Einfrierschritt |
| 11 | **Reduktionspaar** — FB1 aus Testbeispiel 5 (IW, R₁/C₁) und Testbeispiel 10 (AW, C₁,korr nach Gl. (17)) | beide Reduktionsarten desselben Bauteils treffen ihre Ergebnistabellen im Band; **als Paar** abzunehmen (3.6) |
| 12 | **Bezugsperiode** — derselbe Aufbau einmal mit und einmal ohne raumseitige Vorsatzschale mit Luftschicht | (10a)/(10b) schalten den Aufbau mit Vorsatzschale und Luftschicht auf T_BT = 2 d, denselben Aufbau ohne sie nicht; aufgeklebte Innendämmung schaltet in der Regel **nicht** (3.2); der Summenfuß des Schichtdialogs zeigt es (5.1) |
| 12a | **R_rad-Umstellung ist im Einzonenfall ergebnisneutral** — ein Gebäude mit A_IW ≥ A_AW, einmal mit fester Bezugsfläche, einmal mit der Fallunterscheidung Gl. (29)/(31) | Gl. (29) greift, und die Reihen sind **bitgleich** zum Einzonenwert. Bricht die Probe, gehört die Umstellung mit eigenem Einfrierschritt zu G3 (2.2, Punkt 2) |

**Ergebnisse G6b (26.09.2026, Konzept N1.55 und N1.56).** Alle Proben laufen in `EPOS.Kern.Tests`:

- **Probe 1** nach A7: Lauf 1 bitgleich zur Einzonenrechnung mit unbeheiztem Rand; Testbeispiel 10
  mit der Trennfläche höchstens 0,0883 K außerhalb des Bands (Maßstab: nicht schlechter als G3).
- **Probe 2** bitgleich, die Gebäudesumme gegen die Einzone bis 1e-14 K.
- **Probe 3** im **Band 3 %** (+1,8 % stationär, +2,4 % im Jahresgang), modellbedingt und benannt;
  der Antrieb über die Trennfläche verschwindet (1e-9 K).
- **Probe 4** auf echten Größen: Bilanz je Zone geschlossen, Luftströme stündlich Σ = 0;
  **(c)** stationäre Erhaltung 5·10⁻¹⁶, Kriterium **< 0,1 %**; **(d)** gegen die wandaufgelöste
  Referenz +0,044 %, Kriterium **gesamt < 0,1 %, Dynamik < 0,01 %** (gemessen höchstens 7·10⁻⁶).
- **Probe 5** nach A8 geteilt: **5a** Abstand zum Fixpunkt 3,5e-5 K, **5b** gegen das 4×4-System
  höchstens 1,5e-4 K, Kriterium **< 0,001 K**.
- **Probe 6** Vorstunde gegen Iteration höchstens 0,019 K, Jahresenergie 2,5e-5 — die Wahl B ist
  gemessen begründet. **Proben 7–11** wie gefordert; **Probe 10** 14/14 byte-gleich in jeder Welle.
- **Probe 12a** bestätigend: R_rad ist schon die Fallunterscheidung Gl. (29)/(31), kein Einfrierschritt.

### 8.2 Import

Die Importprobe braucht eine Testdatei, die **Materialschichten und 2nd-Level-Grenzen führt**.
Gemessen erfüllt das nur `FM_ARC_DigitalHub_with_SB_v1.ifc` (17,6 MB, 59 Räume, 2 582 Grenzen der
2. Ebene, 1 724 Paare, 359 U-Werte, 20 Schichtsätze mit 38 Schichten, drei Geschosse,
`EXTERNAL_EARTH`, Bauteiltypen bis `IfcCurtainWall`). **Die Lizenz ist mit Repositorium, Commit und
Abrufdatum zu belegen:** MIT ist für `github.com/RWTH-E3D/DigitalHub` belegt (Lizenz-API,
`spdx_id: MIT`), die hier gemeinte `…_with_SB`-Fassung stammt nach Konzept 7.8 aus dem E3D-GitLab
und ist damit **nicht** gedeckt; auch die U-Wert-Zahl weicht ab (Konzept 7.8 zählt 719, dieses
Papier 359). **M10 ist mit E27 entschieden:** Die Datei kommt ins Repositorium, **nur**
zusammen mit der Zeile `Referenzlaeufe/Importproben/**/*.ifc filter=lfs diff=lfs merge=lfs -text` in
`.gitattributes` im selben Schritt und dem Vermerk in `Referenzlaeufe/LIESMICH.md`, Abschnitt „Git
LFS". Die Lizenz der `…_with_SB`-Fassung ist nachzufragen und bis dahin nur außerhalb des
Repositoriums zu messen; Lizenzbeleg und abweichende U-Wert-Zahl gehören vor der Aufnahme in
**einen** Vermerk.
Die Auflage ist wie bei den KIT-Dateien Lizenztext und Vermerk.

| Nr. | Probe | Kriterium |
|---|---|---|
| 13 | **Zählproben ohne Datenbank** | Zonen, Grenzen, Paare, U-Werte, Schichten der Messdateien werden wiedergefunden (Befund P, § 5.1) |
| 14 | **Polygonflächen** | die Bruttosummen **778,2 / 1 358,1 / 22 862,1 m²** auf 0,1 m² getroffen — Reproduktion der Textauszählung, keine Hüllfläche (6.2); beim ersten Lauf mit xBIM nachgemessen |
| 15 | **FZK-Haus mit und ohne Space-Boundary-Anreicherung** | dieselbe beheizte Fläche (die vier Zonen der angereicherten Fassung fassen dieselben sieben Räume zusammen) |
| 16 | **Einzonenfall als Sonderfall** | Z5 auf demselben Gebäude reproduziert die Zahlen aus Befund N — **das ist die eigentliche Probe** |
| 17 | **Raumseitenmaß gegen Bruttomaß** | der Abstand ist für das FZK-Haus **beziffert**; die Zahl ist heute nicht bekannt und entscheidet, ob 6.2 Weg 1 tragbar ist |
| 18 | **Archicad-Rekonstruktion** | `AC20-FZK-Haus.ifc` als schwerer Fall: Basisklasse ohne `CorrespondingBoundary`, `IsExternal` fehlt, Stoffwerte auf null, `IfcCompositeCurve`, 3D-Randpunkte — die sechs mehrdeutigen Bauteile landen benannt unter „Flächen ohne Gegenstück" |

**Umsetzungsvermerk G6c, Welle A (26.09.2026,**
[Protokoll](../ueberholt/Protokolle/Gebaeudesimulation/2026-09-26_G6c_Zonenimport.md)**).** Die Proben 13–16
und 18 brauchen FZK-Haus und DigitalHub; beide liegen nicht im Repositorium, die Lizenz ist nach M10 offen,
die Proben sind deshalb nicht gefahren. Ersatzweise halten zwei eigene Importproben unter
`Referenzlaeufe/Importproben/` die Regeln: `ifc4_zonen.ifc` (drei Geschosse, Polygone, Gegenstück,
fehlendes Paar, geschachtelte und mehrfache `IfcZone`, Klassifikation, B3/B5, zu kleiner Raum) und
`gbxml_zonen_viele.xml` (60 Räume, über 50 Zonen); unter Z5 bzw. X4 bleibt der Vorschlag zeilengleich zu
G4b. Die genannten Proben kommen mit den lizenzgeklärten Dateien nach.

### 8.3 Referenzprojekt und Einfrierschritt

**Vorschlag:** Ein Referenzprojekt der Testdatenbank bekommt mit G6 ein Gebäude mit **drei Zonen**
(beheiztes Erdgeschoss, beheiztes Obergeschoss, unbeheizter Keller mit Erdreich-Randbedingung) samt
gesäten Bauteilen, Aufbauten und Schichten. Ohne das ist der Mehrzonenweg im Regressionsnetz
unsichtbar — dasselbe Argument, mit dem E4 die Umstellung der dreizehn Projekte begründet. Die
Basis wird mit G6 **einmal** neu eingefroren, begründet in `Referenzlaeufe/LIESMICH.md` nach Muster
`:236-245`. Zugleich entsteht die **Einfrierregel „gesäte Zonendaten"** (`Tab_Zone`,
`Tab_Bauteil`, `Tab_Bauteilschicht`, `Tab_Baustoff(_STAMM)`) neben der Regel aus E4 — dort und im
Abschnitt „Regressionsnetz" der Wurzel-`CLAUDE.md`. Einfrierregeln werden **benannt**, nicht
durchgezählt; die Zählung geht sonst mit jeder Stufe schief. **Offen (M11):** bestehendes Projekt umstellen
oder ein vierzehntes anlegen.

**Dialogtests** folgen dem Muster der 96 bunit-Klassen unter `EPOS.UI.Tests/Dialoge/`:
`EposBunitContext` als Basis, `JSInterop.Mode = Loose`, `Satz()`-Fabrik mit vollständig belegtem
DTO, `Aufbauen(...)`-Fabrik mit Delegaten, Kultur auf de-DE gepinnt. **Grenze:** „bunit misst weder
Farbe noch Breite noch Höhe" (`EPOS.UI/CLAUDE.md:72`) — dafür ist `Proben/Rasterprobe` zuständig,
zu ziehen, sobald eine neue Spaltenart oder eine geänderte Zeilenhöhe entsteht.

---

## 9. Stufen G6a–G6d

| Stufe | Inhalt | Abnahme | Aufwand |
|---|---|---|---|
| **G6a — Datenmodell und Pflege** | **Mit G3 gebaut (25.09.2026, Schritte 132–134):** alle acht Tabellen samt Registerpflege S-D, Baustoffsaat (65 Stoffe und 67 Herstellerprodukte, E39), Baustoff- und Aufbaukatalog unter Administration › Gebäude, Aufbau- und Schichteditor mit Summenfuß R/U/C und T_BT, Controller, Kopierwege, `FK_MAP`/`KINDER`, der Zonen- und Bauteildialog in der Grundform. **G6a behält** die Register-, Editor- und Kopierarbeit, die mehrere Zonen verlangen, und die Bericht-Zonentabelle; `Tab_Zonenluftstrom` und `Tab_Bauteil.ID_Nachbarzone` kommen mit G6b (S-G) | Migrationstests grün, Auslieferungsvorlage grün (Katalog nicht leer), Referenzlauf **byte-gleich** (kein Referenzprojekt hat Zonen), `SqlDialektPruefer` grün | **10–15 PT** geplant; mit G3 lag der größere Teil vor; mit E46 beauftragt, auf 9,5–12 PT beziffert und in vier Wellen umgesetzt (rund 9 PT, Konzept N1.51) |
| **G6b — Zoneneingabe und Rechenweg** | Schritt **S-G** (`Tab_Zonenluftstrom`, `Tab_Bauteil.ID_Nachbarzone`); Zonenreiter, Zonendialog, Bauteilliste, Bauteildialog, Hülle nach `EPOS.UI.Daten`; die Zonenschleife in `HeizwaermeEinesGebaeudes`; Gruppenbildung AW/IW mit adiabatem Vorlauf für die 4-K-Regel, Gl. (29)/(31), θ_NR,eq nach (40), Gewichtung (41)/(42) mit Σ B_v = 1; Gauß-Seidel mit fester Reihenfolge und den Schwellen 0,01 K / 0,1 W; unbeheizte Zonen; Konsistenzprüfungen; Proben 1–12 samt 12a | Testbeispiel 10 im Normband, Probe 10 **bitgleich zum Stand nach G3** (trägt nur mit der Ausnahme N = 1, 2.4/2.9), Probe 12a ergebnisneutral, Probe 6 gemessen und begründet | **12–18 PT** |
| **G6c — Zonenimport aus IFC** (mit **D16** auch aus gbXML) | Zonierungsregeln Z1…Z5 (samt Messung von `IfcSpatialZone`) und B1…B6; **nach D16 — entschieden mit E27 — zusätzlich die gbXML-Zonenregeln X1…X3** ([Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 3.3, **D16** in 11.1; `X4`, der Einzonen-Rückfall, gehört zu **G4c** und ist hier nicht enthalten) — in den 16–26 PT stecken X1…X3 noch **nicht**, ihr Zuwachs wird mit der Beauftragung von G6c beziffert; Polygonflächen, Normale, Azimut/Neigung; `CorrespondingBoundary` und Rekonstruktion; Persistenz der Zuordnung Zone ↔ `GlobalId` samt Dateikennung (Kapitel 6); Öffnungsabzug je Fläche; Schichtrichtung nach `DirectionSense` und Grenznormale; Namensabgleich N1…N7 mit Synonymtabelle aus der Auslieferung (M9 — vorgezogen: mit G4b umgesetzt bzw. in Arbeit, G3, Nachtrag zu E44 in N1.49; nicht G6a); Zuordnungsdialog mit vier Abschnitten, formatfrei benannt, als Überlagerung im Gebäudedialog (A3, A17); Importprobe auf der Datei mit LFS-Zeile (M10); Meldungen in beiden `.resx`; Importproben; **Zonengeometrie-Modell und 2D-Grundriss je Geschoss im Zuordnungsdialog (E11, 6.7)** | Proben 13–18 und die beiden Proben aus 6.7; iOS-Lauf nach Rückfrage (Trimming, Größenlimit gemessen) | **16–26 PT** |
| **G6d — Referenzprojekt und Einfrieren** | Zonenprojekt in der Testdatenbank säen; Einfrierregel „gesäte Zonendaten" (benannt, nicht durchgezählt); Referenzlauf, Vergleich, Begründung; Wiki-Seite und Logbuch-Eintrag | grüner Kern-Lauf, neue Basis begründet | **2–3 PT** |
| | **Summe G6** | | **40–62 PT — ohne X1…X3 (D16)** |

**G6b umgesetzt (26.09.2026, Konzept N1.55, N1.56):** sechs Wellen W0 bis W5, rund 16 PT gegen
geschätzt 15,5–20,5 PT im Auftrag (die Tabelle nennt 12–18 PT; dazu kamen Netz, `Tab_ErgebnisZone`,
Lösch- und Duplikatregeln, KI-Sicht und Wiki). Die Wiki-Seite „Mehrzonenmodell" und der Logbuch-Satz
gehören nach E46/A1 zu G6b, nicht zu G6d; die Seite liegt als Repo-Quelle, hochgeladen wird mit dem
nächsten Sammel-Upload. Die Messung am echten Gebäude (50 Zonen rund 0,55 s) ist die Grundlage für M12.

Aufwände sind Größenordnungen für Entwicklung und Nachweis; Agentenarbeit verkürzt die
Kalenderzeit, nicht die Prüfzeit. Zum Vergleich: Befund Q beziffert Datenmodell, Pflege und Bericht
allein mit 18–27 PT — darin sind die Teile enthalten, die dieses Papier auf G6a und die
Dialoganteile von G6b verteilt; Konzept 11 nennt für G3 8–12 PT, und das ist der **Rechenweg** der
Bauteilreduktion, nicht das Datenmodell.

**Die Stufen verschieben Arbeit untereinander, nicht die Summe.** G6a gibt `Tab_Zone`,
`Tab_Bauteil`, `Tab_Bauteilschicht` und `Tab_Baustoff(_STAMM)` an G3 ab (3.5, 4.4) und behält
Register-, Katalog- und Editorarbeit sowie `Tab_Bauteilaufbau(_STAMM)` — *gebaut hat G3 davon mehr,
nämlich auch `Tab_Bauteilaufbau(_STAMM)`, beide Kataloge und den Schichteditor (Kopf, Tabelle oben)*;
dafür kommen zu **G6b**
der Schritt S-G, der adiabate Vorlauf der 4-K-Zuordnung (2.2) und Probe 12a
(8.1), zu **G6c** der geometrische Öffnungsrückfall (6.2), die Messung von `IfcSpatialZone` (6.1),
der Typweg der Schichtsätze über `IsTypedBy` (6.3), die Bauteilart `VORHANGFASSADE` (4.2) und die
Persistenz der Zuordnung Zone ↔ `GlobalId` (Kapitel 6). Die Verschiebungen heben sich in der
Größenordnung auf: Die Spannen je Stufe und die Summe **34–52 PT** bleiben **gegenüber Rev. 1**, sind
aber anders belegt. Was der Anwenderentscheid **E11** darüber hinaus auf G6c legt, steht im nächsten
Absatz.

**Was E11 hinzufügt.** Der Gebäudebetrachter (Konzept N1.16, 15.09.2026) legt **6–10 PT** auf G6c:
das **Zonengeometrie-Modell** im Kern (3–5 PT) und den **2D-Grundriss** im Zuordnungsdialog
(3–5 PT, 6.7). Damit steht **G6c bei 16–26 PT** und die **Summe G6 bei 40–62 PT** statt 34–52 PT —
in beiden Zahlen stecken die gbXML-Zonenregeln X1…X3 **nicht**; **D16** ist mit E27 entschieden
(ja), ihr Zuwachs wird mit der Beauftragung von G6c beziffert.
Die zweite Ansicht — schematische Körper, 4–7 PT — hängt an G7b und steht im
[Datenaustauschkonzept](Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) (Nachtrag 1); sie ist hier
**nicht** mitgezählt.

**Reihenfolge:** G6a → G6b → G6c → G6d. G6a vor G6b, weil der Rechenweg die Tabellen liest; G6c
zuletzt, weil ein Import ohne Zonenrechnung in ein Modell schreibt, das die Daten nicht nutzt —
dasselbe Argument, mit dem Konzept 7.9 G4 hinter G2 stellt. **Vorbedingungen:** G3 muss fertig sein
(Kettenmatrix-Reduktion), G4 für G6c (Leser, Einheiten, Azimutkette), und Befund Q-1 muss mit G1
behoben sein.

---

## 10. Fragen mit Empfehlung

**E27 (22.09.2026, [Konzept](Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) N1.32) hat M2, M9, M10 und M14 entschieden**, sämtlich nach
Empfehlung; die Zeilen tragen den Vermerk, Frage und Empfehlung bleiben als Begründung stehen. M1
und M4 sind seit dem 16.09.2026 entschieden (E17); M3, M5 und M6 hat **E49** (26.09.2026, Konzept N1.55)
nach Empfehlung entschieden. **E50 (26.09.2026, Konzept N1.57) hat mit dem Auftrag der Stufe G6c M7, M8,
M12 und M13 entschieden**, alle nach Empfehlung. M11 bleibt mit seiner Stufe zu entscheiden.

| Nr. | Frage | Empfehlung |
|---|---|---|
| **M1** | Kopplungsweg: A (Vorstunde), B (Gauß-Seidel) oder C (Gesamtsystem)? | **B bleibt es auch nach dem Gegenlesen**, mit A als Vergleichsrechnung und C für N = 2 als Prüforakel (2.4): Die berichtigte Gewichtung (Σ B_v = 1, 2.3) dämpft den Kopplungspfad eher, das zusätzliche Abbruchmaß 0,1 W verschärft nur die Schwelle, und das Festhalten des Regelungsmusters je Stunde ist gerade der Punkt, an dem C teuer würde. Die Wahl wird durch Probe 6 **gemessen** bestätigt, nicht vorausgesetzt; festgehalten in [ADR-005](ADR-005_Zonenkopplung_Mehrzonenmodell.md) — **angenommen 16.09.2026 (E17)** |
| **M2** | Raumseitenmaß oder Bruttomaß beim Import? | **Entschieden mit E27 (22.09.2026): Raumseitenmaß durchhalten und im Dialog benennen** (6.2). Das weicht von der Bemaßungsregel des Einzonenmodells ab (VDI 2078, 6.1, S. 18) — deshalb ein Entscheid; Probe 17 beziffert den Abstand |
| **M3** | Gilt die 4-K-Regel als feste Vorgabe oder je Trennfläche übersteuerbar? | **Entschieden mit E49 (26.09.2026, A1): Vorgabe mit Übersteuerung je Trennfläche**, Anzeige des Δϑ als Beleg; gemessen wird es an den **gerechneten Raumkonditionen** eines adiabaten Vorlaufs, nicht an den Sollwerten (VDI 2078, 7.2, S. 46), und die Zuordnung fällt einmal vor dem Lauf, nie während (2.2). Nach dem Lauf wird eine Überschreitung von 4 K benannt |
| **M4** | Kommt der Zonen-Luftaustausch in G6 oder später? | **In G6b**, als Paare mit `CHECK (ID_ZoneA < ID_ZoneB)`. Ohne ihn ist Treppenhaus und offene Küche nicht darstellbar — und er ist der Grund gegen Vorschlag A. Wer ihn streicht, kann A nehmen und spart 2–3 PT — **entschieden 16.09.2026 mit ADR-005 (E17): in G6b** |
| **M5** | Bekommen unbeheizte Zonen eigene Zeilen im Bedarfsdialog? | **Entschieden mit E49 (26.09.2026, A2): ja** — sie tragen keine Heizlast, aber Temperatur und die achte Gebäudekennzahl **`Ueberhitzungsstunden`** [h] (Stunden der Nutzungszeit mit θ_op über `Maximaleraumtemperatur`, auch mit wirksamer Kühlung — nicht über `Kuehl_Sollwert`, E32) — derselbe Name und dieselbe Bildungsregel wie in Rechenschritte 8.2, Umsetzungskonzept 1.4 und Systementwurf F7; ohne Zeile ist die Kellertemperatur unsichtbar, und sie ist der fachliche Gewinn (2.5) |
| **M6** | Vorlauf: 30 Tage mit Konvergenzprobe oder fest 90 Tage? | **Entschieden mit E49 (26.09.2026, A3): 30 Tage mit Probe**, Verlängerung auf 90 Tage benannt, nur ab zwei Zonen (2.9); feste 90 Tage kosten Rechenzeit ohne Aussage bei leichten Gebäuden |
| **M7** | Zonenregel als Vorgabe beim Import: Z4 (je Geschoss) oder stets Z5? | **Entschieden mit E50 (26.09.2026): Z4, Rückfall Z5** — Z4 trägt in allen vier Messdateien; bei fehlenden Grenzen zwingend Z5 (6.5); bei gbXML entspricht Z4 die Regel X2 |
| **M8** | Mindestgröße einer Zone: max(2 m², 2 %)? | **Entschieden mit E50 (26.09.2026): ja**, mit Zuschlag zum Nachbarn mit der größten gemeinsamen Grenzfläche; sonst werden aus der Institute-Datei 78 Zonen (6.1) |
| **M9** | Synonymtabelle: Auslieferung (`_STAMM`) oder Projektgröße? | **Entschieden mit E27 (22.09.2026): Auslieferung** — die Namen der Autorensysteme wiederholen sich projektübergreifend; je Projekt gepflegte Zuordnungen ergänzen sie |
| **M10** | Testdateien im Repositorium: DigitalHub (17,6 MB) als Blob? Lizenz der `…_with_SB`-Fassung nachfragen? | **Entschieden mit E27 (22.09.2026): DigitalHub ja — aber nur mit der Zeile `Referenzlaeufe/Importproben/**/*.ifc filter=lfs diff=lfs merge=lfs -text` in `.gitattributes` im selben Schritt** und einem Vermerk in `Referenzlaeufe/LIESMICH.md`, Abschnitt „Git LFS"; ohne sie liegt ein 17,6-MB-Blob dauerhaft in der Geschichte (er ist die einzige Datei mit Schichten **und** echten 2nd-Level-Paaren). **Lizenz der `…_with_SB`-Fassung nachfragen** — MIT ist für das GitHub-Repositorium belegt, nicht für die E3D-GitLab-Fassung (8.2) —, bis dahin nur außerhalb des Repositoriums messen. Alternative: Test holt die Datei zur Laufzeit und schweigt ohne sie |
| **M11** | Referenzprojekt mit Zonen: bestehendes umstellen oder vierzehntes anlegen? | **Bestehendes umstellen**, im Einfrierschritt G6d — ein vierzehntes Projekt verlängert jeden CI-Lauf dauerhaft |
| **M12** | Obergrenze 50 Zonen je Gebäude — und wie hart? | **Entschieden mit E50 (26.09.2026): ja, 50 als Vorgabe**, aber **im Import als Warnung mit Rückfrage** und dem Vorschlag „auf Geschosse zusammenlegen" (6.6); die Rechnung selbst lehnt darüber benannt ab (2.9). Die Zahl selbst ist eine Setzung aus der Rechenzeit, kein Messergebnis — sie bleibt offen, bis Probe 6 die Laufzeit an einem echten Mehrzonengebäude gemessen hat; gemessen mit G6b: 50 Zonen in rund 0,55 s je Gebäude und Jahr (Kapitel 9) |
| **M13** | Wie weit soll die Archicad-Rekonstruktion gehen? | **Entschieden mit E50 (26.09.2026): vollständig** (Paarbildung über Geometrie, 6.2) — die magere Alternative wäre, Mehrzonigkeit nur bei echten 2nd-Level-Entitäten anzubieten und Archicad auf Z5 zu beschränken; das spart 2–3 PT und schließt die einzige lizenzfreie kleine Referenzdatei aus |
| **M14** | Wird `Tab_Baustoff` (Projektkopie) gebraucht, oder genügt `_STAMM` mit der Wertekopie an der Schicht? | **Entschieden mit E27 (22.09.2026): beides behalten** — die Wertekopie an der Schicht schützt gerechnete Ergebnisse, die Projektkopie erlaubt projekteigene Stoffe; wer die Projektkopie streicht, spart eine Tabelle und verliert den Weg „eigener Stoff ohne Katalogeintrag" |

---

## 11. Risiken

| Risiko | Wirkung | Gegenmaßnahme |
|---|---|---|
| **Befund Q-1 bleibt offen** (`GebaeudeStammCtrl.cs:439`, `:459`) | Jede aus dem Katalog kopierte Zone erbt 0,0 statt der Vorgaben; „NULL = Vorgabe" ist nur im Dialog wahr | Umbau auf `WechselrichterSchema.Fachspalten` + `Spaltenwert` **mit G1**, Schritt S-E als eigener Einfrierschritt (4.4) |
| **Kaskade beim Speichern** räumt die Zonenliste ab (`AnlageStrangCtrl.cs:33-43`, `001_grundschema.sql:1187`) | Zonen verschwinden beim Speichern des Gebäudes, ohne Meldung | Gegenprobe **vor** dem Dialog schreiben; Rettung dort, wo das Löschen steht |
| **Gl. (29)/(31) nicht umgesetzt**; **die beiden Reduktionsfälle verwechselt** — asymmetrische Trennwand halbiert oder symmetrische ganz geführt | Kleine Zonen mit großen Trennflächen rechnen falsch; im ersten Fall fehlt Masse in beiden Zonen (Zeitkonstanten zu klein), im zweiten trägt das Gebäude die doppelte Speichermasse | Probe 9 (Grenzfall A_AW > A_IW), Probe 2 (zwei identische Zonen), Probe 4 (Energiebilanz), Probe 11 (beide Reduktionsarten gegen die Norm) |
| **Benannte Modellgrenze: doppelt geführter Speicherinhalt** der asymmetrisch beaufschlagten Trennwand (2.2) | Die Bilanz schließt im Jahresmittel, nicht in der Stunde; kurzzeitige Vorgänge an schweren Trennwänden werden zu träge abgebildet | Grenze im Papier, im Dialog und im Bericht benennen; Probe 4 misst die Jahresbilanz, Probe 5 den Stundenfehler gegen das 4×4-Orakel |
| **Kopplungsgewichte falsch normiert** — θ_ext mit Gewicht 1 statt (1 − Σ B_zj) | Die äquivalente Temperatur liegt über jedem beteiligten Wert; der Fehler wächst mit der Trennfläche und fällt in einem Wohngebäude mit 4-K-Regel kaum auf | Σ B_v = 1 als stehende Zusicherung (Probe 9); Testbeispiel 10 mit seinen 19 K Abstand (Probe 1) |
| **Nichtkonvergenz bei starkem Luftaustausch** | Lauf bricht ab, wo der Anwender ein Ergebnis erwartet | Probe 8; Meldung nennt Zonen und Volumenstrom; M4 erlaubt, den Luftaustausch zu streichen |
| **Keine oder mit Nullen gefüllte Stoffwerte** (gemessen: null brauchbare von vier Dateien) | Der Bauteilweg läuft leer; λ = 0 ergibt unendlichen Widerstand — schlimmer als eine Vorgabe | Namensabgleich als **Regelweg** und Plausibilitätsband „≤ 0 ist kein Wert" (3.5); Baustoffkatalog vor dem Import |
| **Zonenbildung ohne `IfcZone`** trifft nicht die thermische Gliederung; **stillschweigend entkoppelte Zonen** bei fehlenden Raumgrenzen | Der Anwender bekommt Zonen, die er nicht wollte, im zweiten Fall schlechter als die Einzonenrechnung | Regel offenlegen, Z1…Z5 umschaltbar, Bilanz nach jeder Änderung, „alles in eine Zone" als Rückweg (6.4); bei fehlenden Grenzen **Z5 erzwingen** (6.5) |
| **Strahlungsaustausch endet an der Zonengrenze** (Gl. (29)/(31), (55)–(57), S. 25); der eine von der Richtlinie vorgesehene Weg über die Grenze, Q̇_str,A,NR in Gl. (40), wird zu null gesetzt (2.6) | Eine Zone, die viel Umfassungsfläche verliert, bekommt einen strukturellen Fehler in der operativen Temperatur (Gl. (103), S. 29); ein besonntes Treppenhaus wirkt nur über seine Lufttemperatur | Argument für **große** Zonen; 4-K-Regel und Mindestgröße wirken in dieselbe Richtung; im Dialog benennen |
| **Rechenzeit bei vielen Zonen** | 50 Zonen × 6 Durchläufe ≈ 1,7 s je Gebäude und Jahr, mit zweitem Vorlauf und Umschaltsuche; dazu der ungekoppelte Vorlauf der 4-K-Zuordnung (≈ 0,25 s), zusammen rund 2,0 s | Obergrenze 50 (M12), Mindestgröße (M8); fällt nur bei Mehrzonengebäuden an |
| **Normstatus**; **Lizenz der `…_with_SB`-Fassungen ungeklärt** | „Mehrzonensimulation nach VDI 6007" wäre falsch; der Prüfstand mit Schichten **und** echten 2nd-Level-Entitäten steht auf einer Datei, deren Lizenz nicht belegt ist | Ausweis nach Entscheid E10 **je Zone** (Kapitel 7); M10 (entschieden mit E27): LFS-Zeile im selben Schritt, Lizenz mit Repositorium, Commit und Abrufdatum belegen, bis dahin nur außerhalb messen |

---

## 12. Abgrenzung — was dieses Papier nicht behandelt

- **Das Einzonenmodell selbst** (Löser, Anbindung, die Schritte M3 und M4, Gebäudedialog, Klimaweg),
  **die Bauteilreduktion als Stufe G3** (Kettenmatrix, Normnachweis an den Typräumen S und L) und
  **der Einzonen-IFC-Import G4** (Ablauf, Klassen, Meldungen, Paketverwaltung, iOS-Trimming) — das
  steht im Konzept, im Umsetzungskonzept und in Befund N; dieses Papier setzt es voraus.
- **Der Export nach IFC und gbXML** (E9, Stufe G7) und **die Geometrieableitung G5** — der Export
  bildet das hier vorgeschlagene Datenmodell ab und bekommt ein eigenes Papier; die Rekonstruktion
  fehlender Raumgrenzen aus Bauteilkörpern ist Aufgabe von IFC2SB/bim2sim. **Die schematischen
  Körper des Gebäudebetrachters** (E11) hängen ebenfalls dort an G7b; hier steht allein die
  2D-Grundrissansicht (6.7).
- **Zonierung *innerhalb* eines Raums** (Blatt 1, 5.3, S. 7: große Räume, Atrien, Hallen, vertikale
  Aufteilung). Ein Atrium mit angrenzenden Bereichen ist nach dieser Lesart **ein** Raum mit
  vertikaler Aufteilung; verglaste Zwischenwände, Galerien über zwei Geschosse und Strahlungswege
  über Zonengrenzen sind mit N gekoppelten 2-K-Zonen nicht abbildbar (Befund O, 5).
- **Feuchte** — die latente Wärmelast ist nach Blatt 1, 6.2, S. 10 nicht Gegenstand der Richtlinie;
  kein Feuchtetransport zwischen Zonen, keine Kondensat- oder Schimmelaussage. **Luftströmung als
  Physik** — Auftrieb, Wind, Druckbilanz, Kamineffekt; alle Ströme sind Eingaben (2.7).
- **Kühlung als vierter Kanal** (**E12 vom 16.09.2026 nimmt ihn auf** — die Kühllast je Zone und ihre
  Deckung regelt das Kühlkonzept, Konzept N1.18), Kältemaschinen, Bauteilaktivierung, Nachweise nach
  GEG/DIN V 18599,
  sommerlicher Wärmeschutz nach DIN 4108-2, Nutzungsprofile für Nichtwohngebäude, Validierung an
  gemessenen Verbräuchen — wie in Konzept 15 abgegrenzt.
- **Die Entscheidung, ob und wann G6 beauftragt wird.** Dieses Papier legt vor.
