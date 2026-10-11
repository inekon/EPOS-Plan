# Entwurf K-D, K-E, K-F — Split und Multisplit, VRF, Rückkühlwerk als eigenes Glied

**Stand 10.10.2026 · Entwurf zur Entscheidung, nichts gebaut.** Gelesen auf `6e5ef1b1` (Zweig `gs-kd`, Stand der Sitzung
Gebäudesimulation nach #908). Auftrag „KD“ der Sitzung Gebäudesimulation nach **E118** (Anwender, 10.10.2026): Stufenplan
der [Konzeptprüfung Kälteanlagen](2026-10-10_Konzeptpruefung_Kaelteanlagen_Katalog_Import.md) wird ausgeführt; Split und
Multisplit bis 12 kW (Stufe 5 der [Recherche](2026-10-08_Recherche_Kaelteanlagen_Herstellerdaten_Rechenmodelle.md), hier
**K-D**) und VRF (Stufe 7, hier **K-E**) kommen **nur als rechnende Anlagenart**; der Ausschluss von Luftführung und
Kältemitteln im [Kühlkonzept](../Konzept_Kuehlung_Gebaeudesimulation_EPOS-Plan.md) §14 ist für **Direktverdampfer** und für
das **Kältemittel als Stammdatum** aufgehoben; das **Rückkühlwerk als eigenes Glied** (**K-F**) kommt nach FK, E33/K8 wird
fortgeschrieben, die Vorgaben sind die heutigen Festwerte; Absorption bleibt zurückgestellt; der Katalog bekommt das Feld
Geräteart (K-A, Schemaschritt 211, parallel); die Kältefolge wird konfigurierbar (KB-D, Schemaschritt 212).

Grundlagen: Konzeptprüfung (Gerätearten-Matrix, 5.1/5.2), Recherche (Abschnitte „Split, Multisplit und VRF“ und
„Rückkühlung und freie Kühlung“, Stufen 3, 5, 7), Kühlkonzept 5.0–5.5 und 14,
[Anlagenkopplung](../Konzept_Anlagenkopplung_Gebaeudesimulation_EPOS-Plan.md) 7 und 10.5 (Kühlübergabe AK1, Kälteseite im
Kreis AK3-K), [Mehrzonenkonzept](../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 2 und 4,
[Entwurf Kältebereich](../Gebaeudesimulation/2026-10-10_Entwurf_Kaeltebereich.md) (KB-A bis KB-D), E33 im
[Status Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md). Das Papier nennt **keine Normwerte und keine
Herstellerkennwerte**; Normen und Verordnungen stehen nur mit Nummer und Gegenstand. Externe Angaben sind Recherchestand und
kein Rechtsrat.

---

## 0 Das Ergebnis in Punkten

1. **K-D Split/Multisplit:** neue Anlagenart **Typ 14 „Raumklimagerät“**; das Außengerät steht im **einen** Kältekatalog
   `Tab_Kaeltemaschine(_STAMM)` mit Geräteart `SPLIT`/`MULTISPLIT` (aus K-A), sein Kennfeld über **Außenluft × Raumluft**;
   die Inneneinheiten hängen an Gebäude oder Zone. Das Gerät ist **Übergabe und Erzeuger in einem**: Es deckt die Kühllast
   seiner Zone direkt, ohne Kaltwasser, ohne Puffer, ohne Kanal. Seine Leistung bei der Stundenaußentemperatur ist eine
   **Kühlgrenze im Stundenrand der Zone** — reicht sie nicht, wird der Raum wärmer (Überschreitungsstunden), auf allen
   Kopplungsstufen. Strom aus Kennfeld, Teillast und Takt nach dem Muster KM3; sensibel, ohne latente Last.
2. **K-E VRF** baut auf K-D: ein Außengerät, viele Inneneinheiten in mehreren Zonen eines Gebäudes, Kombinationsverhältnis
   und Leitungslänge als Korrekturen aus Katalogtabellen (ohne Pflege: Faktor 1 mit Hinweis), Teillast des Außengeräts aus
   der Summe seiner Zonen. Wärmerückgewinnung (3-Leiter) und Heizbetrieb sind abgegrenzt (Frage 4).
3. **K-F Rückkühlwerk:** eigenes Glied `Tab_Rueckkuehlwerk(_STAMM)` an der Anlagenzeile der Kältemaschine — Trockenkühler,
   adiabat, hybrid, Kühlturm offen und geschlossen; Annäherung fest oder lastabhängig über Trocken- bzw. Feuchtkugel,
   Ventilatorregelung, Wasserverbrauch, Teil-Freikühlung im Reihenbetrieb. **Ohne gewähltes Rückkühlwerk und mit den Vorgaben
   rechnet alles wie heute** (`KaelteFestwerte`) — die Basis bleibt byte-gleich.
4. **Befund Klimadaten:** `Tab_Solar(_STAMM).Luftfeuchte` besteht und wird von PVGIS (RH) und DWD-TRY (RF) gefüllt, in der
   **Testdatenbank aber in keiner Zeile** (0 von 280 320 Katalog- und 0 von 367 920 Projektzeilen). Jede Nasskühlerstunde der
   Referenz läuft heute auf dem Ausweichweg; die Feuchtkugel ist nach Stull ableitbar und gebaut. Einziger Leser der Feuchte
   ist die Rückkühlung (`SimulationControl.Kaelte.cs:396`).
5. **Je Stufe ein neues Referenzprojekt** (Kopien von 1017, 1052, 1055) mit eigener Einfrierregel; bestehende Projekte bleiben
   byte-gleich.
6. **Aufwand:** K-D **17–24 PT**, K-E **7,5–10,5 PT**, K-F **9–12,5 PT** (dazu optional 1–1,5 PT Wasserquelle). Zehn Fragen
   in Abschnitt 7.

---

## 1 Bestand, der die drei Stufen trägt oder hindert

| Ort | Befund | Folge für K-D/K-E/K-F |
|---|---|---|
| `Tab_Typ_Energieanlagen` | Typen 1–4, 10–13; 13 = Kältemaschine | K-D bekommt **Typ 14** (2.1) |
| `Tab_Kaeltemaschine(_STAMM)` | Kaltwasser-Spalten (`Rueckkuehlart` mit CHECK `LUFT/WASSER/TROCKENKUEHLER/NASSKUEHLER`, `Kaltwasser_Vorlauf_Min`, `Hilfsstrom_Rueckkuehlung_kW`), KM3-Spalten (`Teillast_Weg`, Kurve a/b/c, `Teillastkurve_Lastgrad_Min`, `Taktverlustfaktor_Cd`, `Verdichterregelung`, `Kennfeld_Randweg`); K-A ergänzt `Geraeteart` | Außengerät der Split/VRF steht hier; die KM3-Teillast wird wiederverwendet |
| `Tab_Kenndaten_Kaeltemaschine(_STAMM)` | Kennfeld `Rueckkuehltemperatur` × `Kaltwassertemperatur` → `EER`, `Kaelteleistung_kW` | für DX-Geräte mit eigenen Achsen (2.2) |
| `Kaeltekaskade` (`Kaeltekaskade.cs`) | **eine** Stundenschleife über den **Projekt**-Kühlkanal (Summe aller Gebäude und Lastgänge); Erzeuger: freie Kühlung, Kältespeicher, Wärmepumpen, Kältemaschinen | kennt keine Zone — ein Raumklimagerät darf nicht hinein (3.1) |
| `Stundenrand` (`Gebaeude/Stundenrand.cs`) | trägt je Zone `KuehlleistungMaxW` und auf AK3 die Kälteschranke (`MitKaelteverfuegbarkeit`) — die Kühlgrenze wirkt im Gebäudemodell, der Raum wird wärmer | **Andockpunkt** der Gerätegrenze (3.2) |
| `Kuehluebergabe` / `Zonenkuehlung` | Kühlübergabe (Kühldecke, Flächenkühlung, Gebläsekonvektor) am Gebäude und je Zone, Schritt K gespiegelt, ab AK1 | ein Raumklimagerät ist **keine** vierte Art dieser Kaltwasserübergabe (3.3) |
| `Tab_Zone` | Kühleingaben je Zone (`Kuehl_Sollwert`, `Kuehlleistung_Max`, `Kuehlung_Aktiv`, `Kuehl_Uebergabe_*`); Einzonengebäude haben meist **keine** Zonenzeile | Inneneinheit verweist auf Gebäude, wahlweise Zone (2.3) |
| `Kaeltemaschine.Rueckkuehltemperatur` | LUFT +5 K, TROCKENKUEHLER +10 K, WASSER fest 25 °C, NASSKUEHLER Feuchtkugel +5 K bzw. Außen −3 K ohne Feuchte (EPOS-Festwerte, `KaelteFestwerte`) | K-F macht daraus Vorgaben (5) |
| freie Kühlung der Kältemaschine | ganz oder gar nicht: Rückkühlung ≤ Kaltwasser − 3 K, dann bis zur Nennleistung mit Ersatz-EER 15 | K-F ergänzt den Reihenbetrieb (5.5) |
| Referenzprojekte | 1017 (Kühlung, ein Gebäude, `Kuehlleistung_Max` 15 kW), 1052 (drei Zonen, ohne Kühlung), 1055 (Kältemaschine mit Trockenkühler und Kältespeicher, Kopie von 1017) | Vorlagen der neuen Projekte (6) |

---

## 2 K-D — Split und Multisplit bis 12 kW

### 2.1 Anlagenart

**Vorschlag: neuer Typ 14 „Raumklimagerät“** in `Tab_Typ_Energieanlagen`; die Anlagenzeile verweist wie Typ 13 über
`ID_Kaeltemaschine` auf die Projektkopie in `Tab_Kaeltemaschine` und trägt `Kaeltemaschine_Anzahl` (Zahl gleicher
Außengeräte), `Kuehl_ID_Carrier` und `Kuehl_EigenerZaehler` (E34/E35) sowie `Kuehl_Hilfsstromanteil` an der Kopie.

Warum ein eigener Typ und nicht Typ 13 mit Geräteart: Alle Leser des Kaltwasserwegs wählen heute Typ 13
(`KaeltemaschinenVorbereiten`, Kälteschranke, Kältevorlauf, Kältefolge KB-A/KB-D, Schema-Kältebahn). Ein Typ 14 bleibt für
sie unsichtbar, bis er ausdrücklich gelesen wird; ein Split-Gerät könnte nie versehentlich Kaltwasser liefern. Geprüft wird
die Paarung **Typ 13 ⇔ Geräteart `KWS_*`**, **Typ 14 ⇔ `SPLIT`/`MULTISPLIT`/`VRF`** im Anlagencontroller (benannte Ablehnung).

### 2.2 Katalog

**Ein Katalog, nicht zwei** (Frage 6): Das Außengerät (bei Multisplit die Kombination, wie EPREL sie führt) steht in
`Tab_Kaeltemaschine_STAMM` mit `Geraeteart` `SPLIT` oder `MULTISPLIT`. Damit gelten Katalogdialog, Filter nach Geräteart
(K-A), Herkunft, Prüfsumme der Katalogfassung, Import (K-C) und Projektkopie unverändert. Für DX-Geräte gilt:

| Feld | Bedeutung bei `SPLIT`/`MULTISPLIT` |
|---|---|
| `Nennkaelteleistung_kW` | Auslegungskälteleistung des Datenblatts (Pdesignc) |
| `Nenn_EER` | EER im Auslegungspunkt (Punkt A) |
| `Rueckkuehlart` | `LUFT` (Pflicht, CHECK über Geräteart) |
| `Kaltwasser_Vorlauf_Min`, `Hilfsstrom_Rueckkuehlung_kW` | leer (benannt abgelehnt, wenn gesetzt) |
| `Verdichterregelung` | `DREHZAHL` (Inverter) oder `EIN_AUS`; daraus die Vorgabekurve von KM3 |
| `Teillastkurve_Lastgrad_Min`, `Taktverlustfaktor_Cd` | kleinste Dauerleistung als Anteil, Taktkennwert aus dem Datenblatt; leer = Hausvorgabe `Waermepumpentakt.VORGABE_CD` |
| neu `SEER` (K-A legt die saisonale Kennzahl an) | **nur Plausibilisierung** (2.6), kein Rechenwert |
| neu `Kennfeld_Weg` | `KENNFELD` (Volllasttabelle) oder `OEKODESIGN` (nur Punkte A–D), Frage 5 |
| neu `Inneneinheiten_Max` | bei `MULTISPLIT` die zulässige Zahl der Inneneinheiten |
| neu `Bereitschaft_W` | Bereitschafts- und Ausaufnahme (Nebenaufnahmen des Datenblatts), leer = 0 |
| neu `Registriernummer` | EPREL-Kennung (für K-H), nur beschreibend |

**Kennfeld.** Die Kennfeldtabelle bekommt für DX-Geräte eigene, ehrlich benannte Achsen statt umgedeuteter Spalten: neue
Tabelle `Tab_Kenndaten_Raumklimageraet(_STAMM)` mit `Aussentemperatur`, `Raumlufttemperatur`, `Kaelteleistung_kW`
(Volllast), `EER`. Die Rechenklasse nutzt die bilineare Leselogik von `KaeltemaschinenKennlinie` (gleiche Rasterregel,
gleicher Randweg `RANDWERT`/`GUETEGRAD`), nur mit Außenluft auf der Rückkühlachse und Raumluft auf der Kaltwasserachse.

**Teillastpunkte A–D.** Neue Tabelle `Tab_Kenndaten_Teillastpunkte(_STAMM)` je Gerät mit `Punkt` (`A`–`D`),
`Aussentemperatur`, `Leistung_kW` (Pdc), `EER` (EERd). Die Außentemperaturen der Punkte und die Innenbedingung sind die der
Ökodesign-Prüfung (VO 206/2012, EN 14825); EPOS führt sie als Werte des Datensatzes, nicht als Konstanten im Code. Aus den
Punkten entstehen:

- im Weg `OEKODESIGN` der Rechenweg 2.5 b;
- im Weg `KENNFELD` die Teillastkurve von KM3 (Lastgrad der Punkte gegen das Volllastkennfeld), dieselbe Umrechnung, die K-C
  für Kaltwassersätze baut — **einmal** gebaut, für beide Gerätearten.

**Inverter und Mindestteillast:** Unter `Teillastkurve_Lastgrad_Min` taktet das Gerät mit Taktverlust nach dem Hausmuster
(`Kaeltekaskade.Taktverlust`, `Kaeltemaschinenteillast`); Punkt D unterhalb der kleinsten Dauerleistung zeigt im
Datenblatt bereits Takten — der Importleser übernimmt dann den Taktkennwert, nicht den EER von D als Dauerwert.

### 2.3 Inneneinheiten

Neue Projekttabelle `Tab_Inneneinheit` (STRICT):

| Spalte | Typ | Regel |
|---|---|---|
| `ID` | INTEGER PK | |
| `ID_Anlage` | INTEGER NOT NULL | FK → `Tab_Energieanlagen.ID`, `ON DELETE CASCADE`; nur Typ 14 |
| `ID_Gebaeude` | INTEGER NOT NULL | FK → `Tab_Gebaeude.ID`, Kaskade |
| `ID_Zone` | INTEGER | FK → `Tab_Zone.ID`, Kaskade; **leer nur bei Gebäuden ohne Zonenzeile** (Einzonenweg) |
| `Bezeichner` | TEXT | |
| `Bauform` | TEXT | `WAND`, `TRUHE`, `DECKE`, `KASSETTE` — **ohne Kanal** (§14 bleibt für Kanalgeräte) |
| `Nennkaelteleistung_kW` | REAL NOT NULL | > 0 |
| `Anzahl` | INTEGER NOT NULL DEFAULT 1 | ≥ 1 |

**Regeln.** Ein Außengerät versorgt Zonen **eines** Gebäudes (Frage 7). Bei `SPLIT` genau eine Inneneinheit, bei
`MULTISPLIT` bis `Inneneinheiten_Max`. Die **Kombinationsgrenze** Σ Innen-Nennleistung / Außen-Nennleistung prüft der
Controller gegen die Katalogfelder `Kombination_Min_Prozent`/`Kombination_Max_Prozent` (aus K-E, für Multisplit schon
gefüllt, wenn der Hersteller sie nennt; leer = keine Prüfung, Hinweis). Eine Zone mit Inneneinheit darf keine
Kaltwasser-Kühlübergabe tragen (`Kuehl_Uebergabe_Art` leer oder `IDEAL`) — Frage 2.

### 2.4 Rechenweg — Übergabe an die Raumluft

**Das Gerät deckt die Zone, nicht den Kanal.** Die Kühllast einer Zone mit Inneneinheit entsteht wie heute im 2-K-Modell
(VDI 6007) aus dem Sollwert der Stunde; neu ist allein ihre **obere Grenze**:

1. **Vor der Stunde** (kein Iterieren): Leistungsangebot der Zone
   Q̇_Gerät(h) = min( Σ Innen-Nennleistung der Zone , Anteil der Zone × Q̇_Außen(θ_e(h), θ_i,soll(h)) )
   mit Q̇_Außen aus dem Kennfeld (Weg `KENNFELD`) bzw. 2.5 b, bei der Stundenaußentemperatur und dem Kühlsollwert der Stunde
   als Raumluft. Der Anteil der Zone ist ihr Anteil an der Innen-Nennleistung des Außengeräts (Frage 7).
2. **Im Stundenrand der Zone** wirkt min(`Kuehlleistung_Max`, Q̇_Gerät(h)) als Kühlgrenze — derselbe Weg wie die
   Kälteschranke auf AK3 (`MitKaelteverfuegbarkeit`), aber **auf allen Stufen AK0–AK3**, weil die Grenze vom Gerät und nicht
   vom Kreis kommt. Reicht sie nicht, ist das Gebäude im Fall `Kuehlgrenze`; die Zone wird wärmer, die Stunde trägt den
   neuen Grund **`RAUMGERAET`**, die Überschreitungsstunden zählen sie (Frage 1).
3. **Verteilung im Knoten:** konvektiv (Strahlungsanteil 0) wie der Gebläsekonvektor — eine Vorgabe ohne Spalte (Anlagenkopplung
   7.4 Punkt 8). Nur Zonen mit Inneneinheit ändern damit ihre Verteilung; alle anderen bleiben wörtlich.
4. **Nach der Stunde:** Die gelieferte Kälte der Zone ist ihre Kühlreihe; sie wird in `SimulationKaeltebedarf` gesondert
   gebucht (`Kaeltebedarf_Raumgeraete`) und **nicht** an die `Kaeltekaskade` gegeben. Der Projekt-Kühlkanal bleibt die Summe
   (Bedarfsprobe unverändert); die Kaskade bekommt Kühlkanal minus Raumgerätekälte, die Deckungsprobe zählt die Raumgeräte als
   Deckung.
5. **Zonensperre (E104)** gilt unverändert: Am Heiztag der Zone ist ihre Kühlung aus, also auch das Gerät.

**Verhältnis zum freien Lauf und zur Kühlübergabe AK1.** Auf AK0 rechnet die Kühlung heute „ideal bis `Kuehlleistung_Max`“;
das Raumgerät ersetzt diese feste Grenze durch eine stündliche. Auf AK1–AK3 bleibt die Kaltwasser-Kühlübergabe (Schritt K)
für Zonen ohne Raumgerät, wie sie ist; eine Zone mit Raumgerät rechnet ohne Schritt K. Auf AK3 ist sie nicht Teil der
Kälteschranke des Kaltwassers. Damit gibt es **keinen** Restbedarf eines Raumgeräts — die Unterdeckung ist Komfort, nicht
Kanalrest (Frage 1).

**Strom je Stunde und Außengerät:**
Last L(h) = Σ gelieferte Kälte seiner Zonen; Lastgrad x = L / Q̇_Außen(h);
Strom = L / EER(θ_e, θ_i) · g(x) · (1 + Hilfsstromanteil) + Taktverlust (x < Lastgrad_Min) + Bereitschaft in Kühltagstunden
ohne Betrieb. g ist die KM3-Teillastkurve (Weg `KENNFELD`) bzw. die Interpolation 2.5 b. Der Strom geht in
`Stromverbrauch_Kuehlung_stuendlich`, damit in Strombilanz, Eigenverbrauch und Tarif über Kühlträger und eigenen Zähler
(E34, E35) — derselbe Weg wie Typ 13.

**Latente Last:** bleibt ausgeschlossen (K5, E31). Das Kennfeld gilt an der festen Innenbedingung seiner Daten; die
Raumluftachse ist die trockene Raumtemperatur. Kein Wärmeverhältnis (SHR), keine Entfeuchtung; der Satz „sensible Kälte“
steht wie an jeder Kältezahl auch am Raumgerät (Frage 3).

### 2.5 Zwei Datenwege, ein Rechenweg

- **a) `KENNFELD`:** Volllasttabelle Außen × Raum (Herstellertabelle, CSV-Vorlage aus K-C) plus Teillastkurve; der Regelfall
  für Geräte mit Auslegungsprogramm.
- **b) `OEKODESIGN`:** nur A–D, Taktkennwert, Nebenaufnahmen. Kapazität = `Nennkaelteleistung_kW` ohne Temperaturabhängigkeit
  (aus A–D nicht ableitbar, Recherche „harte Grenze“), mit Hinweis im Protokoll und im Bericht; EER der Stunde linear über
  der Außentemperatur zwischen den Punkten, außerhalb Randwert; unter dem Lastgrad von D Taktverlust. Genau, solange die Last
  der Lastgeraden der Prüfung folgt; das sagt der Hinweis.

### 2.6 Plausibilisierung

Beim Speichern eines Katalogsatzes: EER-Folge A→D nicht fallend bei Inverter, Pdc der Punkte nicht über der Nennleistung,
SEER gegen den Datensatz über die Beziehung der Labelverordnung zwischen Jahresverbrauch, Pdesignc und SEER (VO 626/2011),
Kombinationsgrenzen. Nach dem Lauf steht der **Jahres-EER der Rechnung neben dem SEER** des Datenblatts (Bericht, Hinweis bei
großer Abweichung; Schwelle als EPOS-Vorgabe in der Welle festgelegt) — der SEER ist kein Rechenwert.

### 2.7 Platz in der Kältefolge

Raumklimageräte stehen **vor** der Kältefolge und **nicht** in ihr: Sie bedienen nur ihre Zonen, konkurrieren mit keinem
Kaltwassererzeuger um denselben Kanal und bekommen keinen `Kaelte_Rang` (KB-D). Der Kältebereich (KB-B) zeigt sie als eigene
Gruppe „Raumklimageräte“ über der Folge „freie Kühlung → Kältespeicher → Wärmepumpen → Kältemaschinen“.

### 2.8 Ergebnisgrößen, Bericht, Kennzahlen

| Ort | Größe |
|---|---|
| `Tab_ErgebnisKaeltemaschine` (eine Zeile je Außengerät, Typ 14) | vorhandene Spalten (Kälte, Strom, Hilfsstrom, Takt, Starts, Teillaststunden, Lastgrad, Netzbezug, Stromspitze, Träger, Zähler); neu `Stunden_Geraetegrenze`, `Jahres_EER`, `Bereitschaft_MWh` |
| `Tab_ErgebnisZone` / `Tab_ErgebnisGebaeude` | neu `Kaelte_Raumgeraet_MWh`, `Stunden_Geraetegrenze`; Überschreitungsstunden und Kelvinstunden bestehen |
| Kennzahlen | `kaelte.raumgeraet.kaelte`, `.strom`, `.eer`, `.grenze_stunden`, `.takt_stunden`; `DeckungKanalKaelte` mit eigenem Zweig „Raumklimageräte“ |
| Bericht | Erzeugerabschnitt „Raumklimageräte“: Außengerät, Inneneinheiten je Zone, Jahreswerte, Jahres-EER neben SEER, Stunden an der Gerätegrenze; Kältediagramm mit eigener Rolle (Farbe über `Ton(r)`); Satz zur sensiblen Kälte |

**Heizfunktion des reversiblen Geräts:** Ein Split- oder VRF-Gerät ist meist eine Luft/Luft-Wärmepumpe. K-D und K-E rechnen
**nur Kühlen**; das Heizen am Raum wäre eine Wärmeübergabe ohne Wasser mit eigener Kopplung an Heizkanal, Wärmekaskade,
Bivalenz und Abtauung. Vorschlag: abgrenzen und als eigene Stufe nach K-E führen (Frage 4).

---

## 3 Begründung der Kopplung (zur Prüfung)

### 3.1 Warum nicht in die Kältekaskade

Die Kaskade deckt den **Projekt**kanal, nach der Wärmekaskade, ohne Zonenbezug. Ein Raumgerät dort hineinzunehmen hieße, je
Stunde den Kanal nach Zonen zu zerlegen, die Reihenfolge je Zone zu führen und die Kälteschranke auf AK3 je Zone zu bilden
— mehr Umbau als Nutzen, und auf AK0 entstünde ein Restbedarf, den ein Raumgerät physikalisch nicht hat (der Raum wird
wärmer).

### 3.2 Warum die Gerätegrenze im Stundenrand

Die Grenze hängt nur an Außentemperatur und Sollwert — beides ist vor der Stunde bekannt. Damit braucht sie keinen
geschlossenen Kreis, wirkt auf jeder Stufe gleich und benutzt eine bestehende, geprüfte Naht (`Stundenrand`, Fall
`Kuehlgrenze`). Die Abweichung, dass die Raumluft der Kennfeldachse der Sollwert und nicht die gerechnete Raumtemperatur
ist, ist benannt: In Grenzstunden ist der Raum wärmer, das Gerät könnte dort etwas mehr — die Rechnung ist auf der sicheren
Seite.

### 3.3 Warum keine vierte Kühlübergabeart

Die Kühlübergabe von AK1 rechnet einen **Wasserkreis** (Vorlauf, Rücklauf, Exponent, Taupunktgrenze, gespiegelter
Schritt H). Ein Direktverdampfer hat davon nichts; ein neuer Wert `RAUMGERAET` in `Kuehl_Uebergabe_Art` würde jeden Leser
dieser Spalte zur Ausnahme zwingen. Die Inneneinheit ist deshalb ein **eigener Verweis**, und die Zone ist entweder
Kaltwasser- oder Raumgerätezone.

---

## 4 K-E — VRF

**Aufbau auf K-D:** dasselbe Typ-14-Gerät mit Geräteart `VRF`, dieselben Inneneinheiten, dieselbe Stundenrandgrenze. Neu:

| Baustein | Inhalt |
|---|---|
| Außengerät | Nennkälteleistung über 12 kW möglich; Kennfeld Außen × Raum wie K-D (Daten aus Auslegungsprogramm, Eurovent nur mit Vertrag) |
| **Kombinationsverhältnis** | CR = Σ Innen-Nennleistung / Außen-Nennleistung; Katalogtabelle `Tab_Kenndaten_Korrektur(_STAMM)` mit `Art` = `KOMBINATION`, Stützstelle CR, `Faktor_Leistung`, `Faktor_EER`; ohne Zeilen Faktor 1 mit Hinweis; Grenzen `Kombination_Min/Max_Prozent` |
| **Leitungslänge** | an der Anlagenzeile `Leitung_Aequivalent_m`, `Leitung_Hoehe_m`; Korrekturtabelle `Art` = `LEITUNG` (Stützstelle Länge, Faktor Leistung) und `HOEHE`; ohne Zeilen Faktor 1 mit Hinweis |
| **mehrere Zonen** | Inneneinheiten in beliebig vielen Zonen **eines** Gebäudes; Aufteilung der Außenleistung nach Innen-Nennleistung (Frage 7) |
| **Teillast** | Lastgrad des Außengeräts aus der Summe seiner Zonen nach der Stunde (2.4); Teillastkurve und Mindestlast des Außengeräts |
| Wärmerückgewinnung | **nicht** in K-E: 2-Leiter ist reines Kühlen; 3-Leiter (gleichzeitig heizen und kühlen) braucht die Heizseite — Frage 4 |

Kennfeld, Ergebnisse, Bericht und Kennzahlen wie K-D, dazu `Kombinationsverhaeltnis` und die wirksamen Korrekturfaktoren im
Ergebnis und im Bericht.

---

## 5 K-F — Rückkühlwerk als eigenes Glied

### 5.1 Fortschreibung E33/K8 (Wortlaut zur Bestätigung)

> **K8, fortgeschrieben mit E118 (10.10.2026):** Die Rückkühlung einer Kältemaschine ist ein **eigenes Glied** — ein
> Rückkühlwerk mit Katalog (`Tab_Rueckkuehlwerk(_STAMM)`), gewählt an der Anlagenzeile der Kältemaschine. Es ist **kein
> Erzeuger** der Anlagenliste und hat **keinen Platz in der Kältefolge**. Ohne gewähltes Rückkühlwerk rechnet die
> Rückkühlart der Kältemaschine mit den Festwerten (`KaelteFestwerte`); die Vorgaben jedes Rückkühlwerks sind dieselben
> Festwerte. Die freie Kühlung bleibt ein Betriebsfall — am Rückkühlwerk wahlweise parallel (ganz oder gar nicht, Vorgabe)
> oder in Reihe (Teil-Freikühlung). Bei der reversiblen Wärmepumpe bleibt die Rückkühlung Teil von Maschine und Kennlinie,
> die Nachtlüftung bleibt Gebäudemaßnahme.

### 5.2 Katalog und Anlagenzeile

`Tab_Rueckkuehlwerk_STAMM` / `Tab_Rueckkuehlwerk` (STRICT, Register in `Katalogfassung`, Projektkopie über `ID_Stamm` wie
die Kältemaschine):

| Spalte | Regel |
|---|---|
| `Bauart` | `TROCKEN`, `ADIABAT`, `HYBRID`, `KUEHLTURM_OFFEN`, `KUEHLTURM_GESCHLOSSEN` |
| `Nennleistung_kW` | abzuführende Wärme im Nennpunkt (EN 1048 für Trockenkühler) |
| `Annaeherung_Nenn_K` | Grädigkeit im Nennpunkt; Bezug Trockenkugel (`TROCKEN`) bzw. Feuchtkugel (nass); **leer = Festwert der passenden Rückkühlart** |
| `Annaeherung_Weg` | `FEST` (heute) oder `LASTABHAENGIG` |
| `Ventilator_Nenn_kW` | leer = Ventilatorstrom wie heute über `Hilfsstrom_Rueckkuehlung_kW` |
| `Ventilator_Regelung` | `EIN_AUS`, `STUFEN`, `DREHZAHL`; `Ventilator_Stufen` (INTEGER ≥ 2) bei `STUFEN` |
| `Befeuchtung_Wirkungsgrad`, `Befeuchtung_Ab_C` | adiabat/hybrid: Vorkühlung der Luft zur Feuchtkugel hin, ab welcher Außentemperatur |
| `Verdunstung_Faktor`, `Eindickung`, `Drift_Anteil` | Wasserbilanz nass; leer = Vorgaben mit Quelle (in K-F2 festgelegt) |
| `Freikuehlung_Schaltung` | `PARALLEL` (Vorgabe, heute) oder `REIHE` |
| `Modulkosten`, Kostenvorlage | wie die übrigen Kataloge |

An `Tab_Energieanlagen` (Typ 13): `ID_Rueckkuehlwerk` (Projektkopie, `SET NULL`). Ist es gesetzt, gilt seine Bauart statt
`Rueckkuehlart`; der Controller prüft die Verträglichkeit (LUFT-Maschine: kein Rückkühlwerk).

### 5.3 Rechenweg

- **Annäherung `FEST`:** Rückkühltemperatur = Bezugstemperatur + `Annaeherung_Nenn_K` — mit den Vorgaben exakt die heutige
  Rechnung (`Kaeltemaschine.Rueckkuehltemperatur`).
- **Annäherung `LASTABHAENGIG`:** ΔT = ΔT_Nenn · Q̇_RK / Q̇_Nenn, Q̇_RK = Kälte + Verdichterstrom der Stunde. Gegen die
  Zirkularität (Rückkühltemperatur ↔ EER) **ein** Schritt: Q̇_RK mit dem EER bei der festen Annäherung geschätzt, dann die
  Stunde mit der lastabhängigen gerechnet — deterministisch, kein Iterieren.
- **Bezugstemperatur:** trocken = Außentemperatur; nass und Kühlturm = Feuchtkugel nach Stull aus `Luftfeuchte`
  (gebaut); adiabat = Lufteintritt nach Befeuchtung oberhalb `Befeuchtung_Ab_C`, darunter trocken; hybrid = trocken bis zur
  Schaltgrenze, darüber adiabat. Ohne Feuchte im Klima: heutiger Ausweichweg mit Hinweis, im Dialog schon beim Wählen einer
  nassen Bauart.
- **Ventilator:** `EIN_AUS` volle Leistung im Laufanteil; `STUFEN` nächsthöhere Stufe des nötigen Luftanteils;
  `DREHZAHL` hält die Nenn-Annäherung, Luftanteil ≈ Lastanteil, Leistung nach dem Ähnlichkeitsgesetz (Luftanteil hoch drei)
  mit kleinster Drehzahl als Katalogfeld. Mit gesetztem `Ventilator_Nenn_kW` entfällt `Hilfsstrom_Rueckkuehlung_kW`
  (Hinweis, keine Doppelzählung).
- **Wasserverbrauch:** Verdunstung = nass abgeführte Wärme / Verdampfungsenthalpie des Wassers × `Verdunstung_Faktor`,
  Abflutung = Verdunstung / (`Eindickung` − 1), Drift als Anteil — in m³ je Stunde und Jahr. Kosten: Frage 8.
- **Hinweis zur 42. BImSchV** (Verdunstungskühlanlagen) bei `KUEHLTURM_*`, `HYBRID`, `ADIABAT` — nur Hinweis, keine Prüfung.

### 5.4 Klimadaten

`Luftfeuchte` [%] steht je Stunde in `Tab_Solar` (Projektkopie) und `Tab_Solar_STAMM`; PVGIS-TMY und DWD-TRY füllen sie
beim Import ([Klimadatenkonzept](../Konzept_Klimadatenquellen_TMY_TRY_EPOS-Plan.md)), NULL heißt „nicht geliefert“. Die
Testdatenbank führt **keine** Feuchte. Folge: Das Referenzprojekt von K-F sät die Feuchte in die **Projektkopie seines
Klimas** (8 760 Zeilen aus einer offen lizenzierten Quelle nach Klimadatenkonzept, Lizenz im Saatskript) — die Projektkopie
liest nur dieses Projekt, und Feuchte liest nur die Rückkühlung; andere Projekte bleiben byte-gleich.

### 5.5 Teil-Freikühlung am Rückkühler

`REIHE`: Liegt die Fluidtemperatur des Rückkühlers (Bezug + Annäherung) unter dem Kaltwasserrücklauf minus dem Mindestabstand
(`FREIE_KUEHLUNG_ABSTAND_K`), kühlt er den Rücklauf vor:
Q̇_FK = min(Kälte der Stunde, ṁ·c_p·(θ_R − θ_FK), Rückkühlerleistung), den Rest deckt der Verdichter am Kaltwasservorlauf.
Rücklauf: auf AK3 der gerechnete Kühlrücklauf der Stunde, sonst Vorlauf + Auslegungsspreizung der Kühlübergabe
(`Kuehl_Auslegung_Ruecklauf` − `_Vorlauf`, ohne Kühlübergabe EPOS-Vorgabe). Strom der freien Kühlung aus dem Ventilator
(5.3) statt Ersatz-EER, sobald `Ventilator_Nenn_kW` gesetzt ist; sonst Ersatz-EER wie heute. Vorbild für den Mischbetrieb ist
die freie Kühlung über die Wärmequelle (`Kaeltekaskade.cs`, Weg KU3-6). `PARALLEL` rechnet wie heute.

### 5.6 Byte-Gleichheit

Drei Schalter halten die Basis: kein Rückkühlwerk gewählt → heutiger Weg; Rückkühlwerk mit leeren Feldern → Festwerte;
`PARALLEL` → heutige freie Kühlung. **Gleichwertigkeitsprobe:** eine Kopie von 1055 mit einem Rückkühlwerk
`TROCKEN`/`FEST`/leer/`PARALLEL` rechnet byte-gleich zu 1055 (Datenbanktest, nicht in der Basis).

### 5.7 Optional: Wasser aus einer Quelle

Rückkühlart `WASSER` rechnet heute fest 25 °C. Die Anlagenzeile Typ 13 trägt die `WQ_*`-Spalten der Wärmepumpenquelle
bereits (gleiche Tabelle); K-F6 liest sie für Kältemaschinen (Konstant, Monatswerte, Profil, CSV), leer = Festwert.

### 5.8 Ergebnisse

`Tab_ErgebnisKaeltemaschine` neu: `Ventilatorstrom_MWh`, `Wasser_m3`, `Stunden_Nass`, `TeilFreikuehlung_MWh`,
`TeilFreikuehlung_Stunden`, `Rueckkuehltemperatur_Mittel`; Kennzahlen `kaelte.rk.ventilator`, `.wasser_m3`, `.teilfrei`,
`.teilfrei_stunden`; Bericht: Zeile Rückkühlwerk im Erzeugerabschnitt Kältemaschine.

---

## 6 Referenzprojekte, Basis, Einfrierregeln

Nummern und Basisnamen werden bei der Anmeldung vergeben (Vorschlag ab 1065).

| Stufe | Projekt (Vorlage) | Saat | Was es hält | Basis |
|---|---|---|---|---|
| K-D | „Referenzprojekt Raumklimagerät“, Kopie von **1017** auf dem Kopierweg | Kühlbetrieb der Wärmepumpe 0 (sie heizt nur); ein `MULTISPLIT`-Außengerät (neutral benannt, Leistung unter der Kühlgrenze 15 kW, sodass Grenzstunden entstehen), zwei Inneneinheiten im Gebäude (Einzonenweg), Kennfeld und Punkte A–D, Kühlträger Projekt | Gerätegrenze im Stundenrand, Strom mit Teillast und Takt, Bereitschaft, Buchung neben der Kaskade | neue Basis; 1017 und alle übrigen byte-gleich |
| K-E | „Referenzprojekt VRF“, Kopie von **1052** | Kühlbetrieb an, Kühleingaben an Zone 1 und 2, `VRF`-Außengerät mit drei Inneneinheiten in zwei Zonen, CR über 1, Leitungs- und Kombinationskorrektur | Aufteilung über Zonen, Korrekturen, Teillast aus der Summe | neue Basis |
| K-F | „Referenzprojekt Rückkühlwerk“, Kopie von **1055** | Rückkühlwerk `HYBRID`, `LASTABHAENGIG`, `DREHZAHL`, `REIHE`; Feuchte in der Klimakopie | Feuchtkugel, Ventilator, Wasser, Teil-Freikühlung | neue Basis; 1055 byte-gleich (Gleichwertigkeitsprobe 5.6) |

**Einfrierregel-Vorschläge** (je ein Abschnitt in [`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md) und
eine Zeile in der Liste der [`CLAUDE.md`](../../../CLAUDE.md)):

- **„gesäte Raumklimagerätedaten“:** Anlagenzeile Typ 14 (`ID_Kaeltemaschine`, Anzahl, Kühlträger, Zähler), Projektkopie
  des Außengeräts mit Geräteart, Nennwerten, `Kennfeld_Weg`, Teillast- und Taktfeldern, Bereitschaft, die Zeilen in
  `Tab_Kenndaten_Raumklimageraet` und `Tab_Kenndaten_Teillastpunkte`, die Inneneinheiten (`Tab_Inneneinheit`) samt
  Zonenbezug, bei VRF Korrekturzeilen und Leitungsfelder; dazu das Anlegen oder Entfernen eines Referenzprojekts mit
  Raumklimagerät. Wachen `RaumklimageraetReferenzprojektWacheTests`, `VrfReferenzprojektWacheTests`.
- **„gesäte Rückkühlwerkdaten“:** `ID_Rueckkuehlwerk` und die Projektkopie in `Tab_Rueckkuehlwerk` (jede Spalte), die
  `Luftfeuchte` der Klimakopie des Referenzprojekts, bei K-F6 die `WQ_*`-Felder einer Kältemaschine; Anlegen oder Entfernen.
  Wache `RueckkuehlwerkReferenzprojektWacheTests`.

Die CI-Liste der sieben Projekte bleibt; die neuen Projekte laufen in der vollständigen Basis wie 1055/1064.

---

## 7 Fragen an den Anwender

Kurzzeichen **KD-Q**; jede Frage mit Empfehlung.

| Nr. | Frage | Empfehlung |
|---|---|---|
| KD-Q1 | Reicht die Leistung eines Raumklimageräts nicht, wird dann der **Raum wärmer** (Gerätegrenze im Stundenrand, Überschreitungsstunden, auf allen Stufen) — oder bleibt ein **Kälte-Restbedarf** wie beim Kaltwasser auf AK0–AK2? | **Raum wird wärmer.** Ein Raumgerät hat keinen Nachbarn, der den Rest deckt; der Restbedarf wäre eine Zahl ohne Anlage. Gleiche Naht wie `Kuehlleistung_Max` |
| KD-Q2 | Ist eine Zone mit Inneneinheit **ausschließlich** Raumgerätezone (keine Kaltwasser-Kühlübergabe, keine Kälte aus der Kaskade)? | **Ja in K-D/K-E.** Mischbetrieb (Split als Spitze neben Kühldecke) erst auf Wunsch als eigene Welle |
| KD-Q3 | Latente Last weiter ausgeschlossen: Kennfeld an der festen Innenbedingung seiner Daten, Raumluftachse trocken, kein Wärmeverhältnis, keine Entfeuchtung? | **Ja** — K5/E31 bleiben, der Satz „sensible Kälte“ steht auch am Raumgerät |
| KD-Q4 | **Heizfunktion** reversibler Split-/VRF-Geräte (Luft/Luft-Wärmepumpe) und VRF-Wärmerückgewinnung (3-Leiter): jetzt mitbauen oder abgrenzen? | **Abgrenzen**, als eigene Stufe nach K-E (Heizen am Raum ohne Wasser berührt Heizkanal, Wärmekaskade, Bivalenz, Abtauung); Bericht und Wiki sagen „rechnet nur Kühlen“ |
| KD-Q5 | Rechenweg **nur aus A–D** (`OEKODESIGN`, Kapazität ohne Temperaturabhängigkeit, mit Hinweis) neben dem Kennfeldweg zulassen? | **Ja** — das Ökodesign-Datenblatt ist die häufigste freie Quelle; der Hinweis benennt die Grenze |
| KD-Q6 | Anlagenart: neuer **Typ 14 „Raumklimagerät“** mit dem Außengerät im gemeinsamen Kältekatalog (Geräteart aus K-A) — oder eigene Katalogtabellen? | **Typ 14 + gemeinsamer Katalog**; eigene Kennfeld-, Punkte- und Inneneinheitentabellen |
| KD-Q7 | Multisplit/VRF: Außengerät nur für Zonen **eines** Gebäudes, Leistung auf die Zonen **fest nach Innen-Nennleistung** aufgeteilt? | **Ja.** Deterministisch, ohne Iteration; eine Aufteilung nach dem Bedarf der Stunde nur auf Wunsch (zweiter Durchgang) |
| KD-Q8 | Wasserverbrauch des Rückkühlwerks nur als Menge oder auch als Kosten (Wasserpreis je m³ an der Anlage, eigene Zeile der Betriebskosten)? | **Menge und Kosten**, Preis optional (leer = keine Kosten, Hinweis) |
| KD-Q9 | Teil-Freikühlung im **Reihenbetrieb** als wählbare Schaltung des Rückkühlwerks, Vorgabe **parallel** (heute)? | **Ja** — Vorgabe parallel hält die Basis; der Reihenbetrieb bringt die Stunden, die die Recherche als Hebel nennt |
| KD-Q10 | Reihenfolge: **K-F1** (Glied mit Festwerten, byte-gleich) gleich nach K-A und FK, dann K-D, K-E, die übrigen K-F-Wellen parallel zu K-D? Rückkühlart `WASSER` an die Quelle (K-F6) mitnehmen? | **Ja**; K-F6 als optionale Welle auf Wunsch |

---

## 8 Oberfläche

| Ort | K-D / K-E | K-F |
|---|---|---|
| **Katalogdialog Kältemaschinen** (Stufe 5 der [Katalogauswahl](../Konzept_Projektdialoge_Katalogauswahl_EPOS-Plan.md), Sitzung „Dialoge und Korrekturen“) | Filter Geräteart (K-A); Stammblatt je Geräteart mit eigener Gruppe „Raumklimagerät“ (Kennfeld-Weg, A–D-Tabelle, Kombinationsgrenzen, Inneneinheiten max., Bereitschaft, Registriernummer); Kennfeld als Diagramm über Außenluft | eigener Katalog „Rückkühlwerke“ in der Kachel, gleicher Baustein (`Zweispaltenauswahl`) |
| **Kälte-Kachel** (Auswahldialog nach KB) | Übernahme eines DX-Satzes legt Anlagenzeile Typ 14 an und öffnet die Inneneinheiten | Rückkühlwerk wird in der Konfiguration der Kältemaschine gewählt, nicht als Anlage |
| **Simulationskonfiguration, Bereich „Kälte“** (KB-B dieser Sitzung) | Gruppe „Raumklimageräte“ über der Rechenfolge; Kachel mit Außengerät, Zahl der Inneneinheiten, Zonen, Kühlträger; „Konfigurieren…“ öffnet `RaumklimageraetKonfiguration` (Anzahl, Hilfsstrom, Träger, Zähler, bei VRF Leitungsfelder) | `KaeltemaschineKonfiguration` bekommt die Gruppe „Rückkühlung“: Rückkühlwerk wählen, Schaltung der freien Kühlung, Hinweis bei fehlender Feuchte |
| **Gebäudedialog / Zonenreiter** | Gruppe „Kühlung“: Liste der Inneneinheiten dieses Gebäudes bzw. dieser Zone (Anlegen, Leistung, Bauform), Sperre der Kaltwasser-Kühlübergabe mit Grund, Summe gegen die Kühllast | — |
| **Schema (Kältebahn)** | Raumklimageräte als eigenes Symbol an der Gebäudekachel, nicht auf der Kaltwasserbahn | Rückkühlwerk als Glied an der Kältemaschine (heute schon als „Rückkühlung“ gezeichnet) |
| **Bedarfsdialog, Bericht** | Zeilen „Raumklimageräte“ und „Stunden an der Gerätegrenze“; Erzeugerabschnitt (2.8) | Rückkühlwerkzeile, Wasser, Teil-Freikühlung |
| KI-Sicht, Feldkarten, Ressourcen | neue Feldkarten der Komponenten, Texte in `MyResource.Resource.*` (beide Sprachen), `designer_neu.py` | ebenso |

---

## 9 Schema je Stufe

Nummern werden bei der Anmeldung vergeben; 211 (K-A, `Geraeteart`) und 212 (KB-D, `Kaelte_Rang`) sind belegt und
vorausgesetzt. Alle Tabellen STRICT, Boolesche Spalten mit `CHECK (… IN (0,1))`, Beziehungen über IDs, Ausrollung nach
[ADR-001](../ADR-001_Schema-Ausrollung.md).

| Stufe | Schritt | Inhalt |
|---|---|---|
| K-D | S-KD1 | `Tab_Typ_Energieanlagen` Zeile 14; an `Tab_Kaeltemaschine(_STAMM)`: `Kennfeld_Weg`, `Inneneinheiten_Max`, `Bereitschaft_W`, `Registriernummer`, `Kombination_Min_Prozent`, `Kombination_Max_Prozent` (Multisplit); CHECK `Rueckkuehlart = 'LUFT'` bei DX-Geräteart; neu `Tab_Kenndaten_Raumklimageraet(_STAMM)`, `Tab_Kenndaten_Teillastpunkte(_STAMM)`, `Tab_Inneneinheit`; Register in `Katalogfassung`; Ergebnisspalten (2.8) |
| K-E | S-KE1 | `Tab_Kenndaten_Korrektur(_STAMM)` (`Art` `KOMBINATION`/`LEITUNG`/`HOEHE`, Stützstelle, `Faktor_Leistung`, `Faktor_EER`); an `Tab_Energieanlagen`: `Leitung_Aequivalent_m`, `Leitung_Hoehe_m`; Ergebnisspalte `Kombinationsverhaeltnis` |
| K-F | S-KF1 | `Tab_Rueckkuehlwerk(_STAMM)` (5.2), `Tab_Energieanlagen.ID_Rueckkuehlwerk`, Register; Ergebnisspalten (5.8); ggf. Wasserpreis (Frage 8) |

Jeder Schritt: Testdatenbank über `Werkzeuge/Testdatenbankschema`, `SqlDialektPruefer`, Schematests; Katalogsätze sind
`_STAMM` und berühren keine Einfrierregel.

---

## 10 Wellenplan

| Welle | Inhalt | Aufwand | Abnahme |
|---|---|---|---|
| **K-F1** | Schema S-KF1, Katalog und Projektkopie Rückkühlwerk, Klasse `Rueckkuehlwerk` mit `FEST` und Vorgaben = `KaelteFestwerte`, Anlagenzeile, E33/K8 im Status fortgeschrieben, Kühlkonzept 5.4 nachgezogen | 2,5–3,5 PT | Kern-Filter grün; `--filter "FullyQualifiedName~Kaelte\|FullyQualifiedName~Rueckkuehl\|FullyQualifiedName~Schema"`; **Gleichwertigkeitsprobe 1055 byte-gleich**; `SqlDialektPruefer`; Referenzlauf der sieben Projekte PASS |
| **K-F2** | lastabhängige Annäherung, Ventilatorregelung, Feuchtkugel/adiabat/hybrid/Kühlturm, Wasserbilanz, Ergebnisse und Kennzahlen | 2–3 PT | Rechenproben ohne Datenbank je Bauart (Grenzfälle: Feuchte fehlt, Teillast 0, Nennpunkt trifft Festwert); Referenzlauf PASS |
| **K-F3** | Teil-Freikühlung `REIHE` (Rücklauf aus AK3 bzw. Spreizung) | 1,5–2 PT | Rechenprobe Mischstunde, Energieprobe Kälte; Referenzlauf PASS |
| **K-F4** | Referenzprojekt Rückkühlwerk mit Feuchtesaat, Einfrierregel, neue Basis | 1–1,5 PT | Wache, Referenzlauf aller Projekte gegen die neue Basis, Begründung in `Referenzlaeufe/LIESMICH.md` |
| **K-F5** | Oberfläche (Katalog, Gruppe „Rückkühlung“), Bericht, Wiki-Quellen, Logbuch-Entwurf | 2–2,5 PT | bunit `~Kaeltemaschine\|~SimulationKonfig`; Schale auf Linux (0 Fehler); `designer_neu.py`; Wiki-Gegenlese; `WikiProduktdatenWacheTests` |
| *K-F6 (optional)* | Rückkühlart `WASSER` an die `WQ_*`-Quelle | 1–1,5 PT | Rechenprobe, Referenzlauf PASS |
| **K-D1** | Schema S-KD1, Modelle, Controller (Paarung Typ/Geräteart, Kombinationsgrenze, Exklusivität der Zone), Katalogfassung | 3–4 PT | Schematests, Controllertests, `SqlDialektPruefer`, Referenzlauf byte-gleich |
| **K-D2** | Rechenklasse `Raumklimageraet` (Kennfeld Außen × Raum, Weg `OEKODESIGN`, Teillast und Takt über KM3, Bereitschaft); Umrechnung A–D gemeinsam mit K-C | 3–4 PT | Rechenproben ohne Datenbank; Validierung an einem frei zugänglichen Prüffallsatz für Splitgeräte nach Lizenzprüfung (Recherche, Abschnitt Split); Plausibilisierung 2.6 |
| **K-D3** | Kopplung: Gerätegrenze im Stundenrand (alle Stufen, Grund `RAUMGERAET`), Konvektivanteil, Buchung neben der Kaskade, Deckungs- und Bedarfsprobe, Strom über Kühlträger und Zähler, Ergebnisse, Kennzahlen, Referenzprojekt Raumklimagerät, Einfrierregel, neue Basis | 5–7 PT | Bedarfs- und Deckungsprobe Kälte ohne Fehler; Komfortkennzahlen; `ModultrennungswacheTests`; Referenzlauf: alle bisherigen Projekte byte-gleich, neues Projekt in der neuen Basis |
| **K-D4** | Oberfläche: Katalogdialog (mit der Sitzung „Dialoge und Korrekturen“ abgestimmt), Kälte-Kachel, Bereich „Kälte“, Inneneinheiten im Gebäude-/Zonendialog, Schema, KI-Sicht, Ressourcen | 4–6 PT | bunit der betroffenen Komponenten; Schale auf Linux; Rasterprobe bei Katalogliste; `fensterprobe.mjs`; `designer_neu.py` |
| **K-D5** | Bericht, Wirtschaftlichkeit (Kostenkomponente, Investition Außen- und Inneneinheiten), Wiki-Quellen, Logbuch-Entwurf | 2–3 PT | `BerichtSchreiberOhneDatenbankWacheTests`; ChartProben; Wiki-Gegenlese; `WikiProduktdatenWacheTests` |
| **K-E1** | Schema S-KE1, Korrekturtabellen, Leitungsfelder | 1–1,5 PT | Schematests, `SqlDialektPruefer` |
| **K-E2** | Kern: Kombinationsverhältnis, Leitungs- und Höhenkorrektur, Aufteilung über Zonen, Teillast des Außengeräts | 3–4 PT | Rechenproben (CR unter, gleich, über 1; ohne Korrekturzeilen Faktor 1 mit Hinweis) |
| **K-E3** | Referenzprojekt VRF (Kopie 1052), Einfrierregel, neue Basis | 1,5–2 PT | Wache, Referenzlauf |
| **K-E4** | Oberfläche (VRF-Felder, Inneneinheiten in mehreren Zonen), Bericht, Wiki | 2–3 PT | wie K-D4/K-D5 |

**Summen:** K-F 9–12,5 PT (+1–1,5 optional), K-D 17–24 PT, K-E 7,5–10,5 PT — zusammen **33,5–47 PT**, im Rahmen der
Konzeptprüfung (K-D 18–26, K-E 8–12, K-F 8–12). **Voraussetzungen:** K-A (Schritt 211) gemergt; K-C für den gemeinsamen
A–D-Leser; KB-B für den Bereich „Kälte“; FK (R51) vor K-F1. Ein iOS-Lauf ist für keine Welle begründet (keine Änderung an
der iOS-Hülle); der Nachweis ist der grüne Kern-Lauf.

---

## 11 Abgrenzung

Nicht Gegenstand: Kanalgeräte und Rooftops (RLT, §14), latente Last und Entfeuchtung (K5), Heizen am Raum und
VRF-Wärmerückgewinnung (Frage 4), direkte Treibhauswirkung des Kältemittels (Füllmenge, Leckage) — das Kältemittel bleibt
beschreibendes Stammdatum mit GWP aus K-A, eine Warnung nach den Stichtagen der F-Gas-Verordnung (VO (EU) 2024/573) ist
Ausbau von K-A —, Absorption (zurückgestellt), Kältenetzverluste, der EPREL-Abruf (K-H). Normen nur als Einordnung:
EN 14825 (Teillast und saisonale Kennzahlen), EN 14511 (Volllastprüfung), EN 1048 (Trockenkühler), VO 206/2012
(Ökodesign Raumklimageräte), VO 626/2011 (Energieverbrauchskennzeichnung), VO 2016/2281 (Kaltwassersätze und Luftkühler
über 12 kW), VDI 2078 (Kühllast, nur Einordnung), 42. BImSchV (Verdunstungskühlanlagen).
