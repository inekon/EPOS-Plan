# Konzept: Bauteilaufbau beim Gebäudeimport — Befund und Vorschlag (EPOS-Plan)

> **Anlass:** Anwenderauftrag vom 06.10.2026 — „Die Bauteile und deren Aufbau ist nicht klar“: Je
> Bauteil soll eine Zuordnung zu Baustoffen oder zu den wesentlichen Eigenschaften (U-Wert,
> Wärmekapazität) bestehen, in der Gebäudeansicht soll sichtbar sein, welche Bauteile keine Zuordnung
> haben, fehlende Werte sollen gekennzeichnet aus der Datei (U) bzw. aus plausiblen Annahmen (Kapazität)
> kommen. Zu prüfen: Reicht der Simulation ein vereinfachter Aufbau, oder lassen sich die Bauteile
> vollständig aus IFC und Projektdatei (`.sqproj`) übernehmen?
>
> **Stand:** Befund am Code und an den Dateien, Vorschlag zur Entscheidung. Nichts davon ist gebaut.
> Bezug: [Mehrzonenkonzept](../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) 3.2–3.6 und 6.3–6.5,
> [Datenaustauschkonzept](../Konzept_Datenaustausch_gbXML_IFC_EPOS-Plan.md) 3.6–3.7,
> [Konzept HottCAD-Verbund](2026-10-05_Konzept_HottCAD_Verbund_IFC_Projektdatei_Viewer.md) 3.4 und 5,
> [Befund Projektdatei](2026-10-05_Befund_HottCAD_Projektdatei.md) 3.5 und 6.2.

## 0. Das Ergebnis in sechs Punkten

1. **Die Simulation rechnet aus der Schichtfolge, nicht aus U und einer Kapazität.** Je Bauteil
   bildet `Bauteilreduktion` aus den Schichten (innen → außen; Dicke, λ, ρ, c) die Kettenmatrix nach
   VDI 6007-1 und daraus R₁ und C₁ (Innenbauteil) bzw. C₁,korr (Außenbauteil). U geht nur in Gl. (27)
   ein. Eine Kapazität ohne Lage gibt es im Bauteilweg nicht.
2. **Ein Bauteil ohne Aufbau rechnet masselos, sobald ein anderes Bauteil seiner Gruppe Schichten
   trägt.** Nur eine Gruppe ganz ohne Schichten nimmt die Bauweise des Gebäudes (Klassenweg). Ein
   teilweise belegter Import senkt die Speichermasse deshalb unter die beider Grenzfälle. Das ist die
   Lücke, die der Anwender sieht.
3. **Die Anwenderdateien tragen fast überall den U-Wert (92–100 % der opaken Bauteile), aber
   nirgends eine Wärmekapazität.** Schichtsätze haben nur die Hüllbauteile mit Körper, λ und ρ nur
   drei von sechs Dateien. Wo U aus Schichten und U der Datei beide da sind, liegt der Schichtwert
   bei zwei Dateien im Median 15 bis 19 % höher. Der Import nimmt dann den Schichtwert und lässt den
   U-Wert der Datei fallen.
4. **Ein vereinfachter Aufbau reicht, wenn er die Lage der Dämmung und die raumseitigen Schichten
   erhält.** Folien, Sperren und Kleber (≤ 5 mm) zu streichen ändert U, R₁ und C₁ um höchstens 2,5 %.
   Die Dämmung auf die falsche Seite zu legen ändert C₁,korr um −92 bzw. +226 %. Raumseitige Putze
   zusammenzulegen ändert R₁ um bis zu 34 % (Rechenproben, Kapitel 4).
5. **Vollständig übernehmen lässt sich der Aufbau aus der Projektdatei,** nicht aus der IFC: Die
   `.sqproj` führt je raumbezogener Hüllfläche einen Aufbau (`TcBuildingElementDimension`) mit
   Schichten samt λ, Dichte und **Wärmekapazität** (1 bis 24 Schichten). Der Schlüssel zur IFC ist
   `GId`. EPOS liest davon heute nichts.
6. **Vorschlag:** drei Zuordnungsstufen je Bauteil (A vollständiger Aufbau, B U aus Datei mit
   Ersatzaufbau, C U und Ersatzaufbau aus Annahme), eine Relevanzregel für Schichten, der U-Wert der
   Datei bleibt neben dem Aufbau stehen, ein Farbmodus „Aufbau“ und eine Liste „Bauteile ohne
   vollständige Zuordnung“. Die Projektdatei wird als Quelle für Stufe A angebunden. Fünf Wellen,
   zusammen 9 bis 11 PT, ein Schemaschritt. Die Basis ist nicht berührt, weil kein Referenzprojekt
   importiert wird.


## 1. Was die Simulation je Bauteil braucht

### 1.1 Bauteilweg: Reduktion der Schichtfolge

- **Eingang je Bauteil:** `BauteilEingang` (`EPOS.Kern/Allgemein/Simulation/Gebaeude/BauteilEingang.cs`)
  trägt Fläche, Art, Randbedingung, Neigung, Azimut, U (NaN = aus den Schichten) und die Schichten.
  `GebaeudeZonenabbildung.AlsBauteil`/`AlsSchicht` bildet ihn aus `Tab_Bauteil` und dem Aufbau. Die
  Schicht trägt die **Wertekopie** λ/ρ/c_p aus `Tab_Bauteilschicht`, nie die Werte des Baustoffs, auf
  den sie zeigt.
- **Reduktion:** `Bauteilreduktion.Reduzieren` (Kettenmatrix je Schicht, `Schichtmatrix`, Produkt
  innen → außen) liefert R₁, R₂, R₃, C₁, C₂ und C₁,korr nach Gl. (12)–(17).
  `BezugsperiodeWaehlen` entscheidet je Bauteil 7 d oder 2 d nach (10a)–(10d) und erkennt so
  raumseitig abgedeckte Masse (Mehrzonenkonzept 3.2). `Parallel` fasst die Gruppe komplex mit
  T_RA = 5 d zusammen (Gl. (19)–(22)).
- **Was eingeht:** jede Schicht mit Dicke, λ, ρ, c in ihrer **Reihenfolge**. Die Kapazität wirkt nur
  so weit, wie sie thermisch zum Raum hin angekoppelt ist (C₁,korr). Der U-Wert geht nur in
  Gl. (27) ein: Ein eingetragener U-Wert hat dort Vorrang, der gerechnete steht in der Herleitung
  daneben, und ab 10 % Abweichung gibt es einen Hinweis (`BauteilHerleitung.UAbweichungHinweis`,
  `GebaeudeFestwerte.UWERT_ABWEICHUNG_HINWEIS`).
- **Luftschichten** ohne λ sind ruhend nach DIN EN ISO 6946 Tabelle 8 und tragen keine Masse
  (`Schicht.RuhendeLuft`).

### 1.2 Ohne Aufbau

Der Weg je Gruppe steht in `ErsatzparameterRC.AusBauteilweg`
(`EPOS.Kern/Allgemein/Simulation/Gebaeude/ErsatzparameterRC.cs`, Kommentar und Zweige zu
`Gruppenweg`):

| Lage | Was die Simulation nimmt | Quelle der Kapazität |
|---|---|---|
| Gruppe (AW oder IW) **ganz ohne** Bauteil mit Masse | Klassenweg: R₁ = 1/(h_ms·A), h_ms = 9,1 W/(m²K) | `Bauweise` des Gebäudes (20/50/100 Wh/(m²K) × Nutzfläche, `Gebaeudebauweise`), Anteil `Masseanteil_Aussen` (Vorgabe 0,3) bzw. 1 − a |
| Außenbauteil **ohne Schichten** neben Bauteilen mit Schichten | masselos: R₁ = R/6 wie ein Fenster, reell parallel zu den Wänden, volles U·A in Gl. (27) | **keine** |
| Innenbauteil ohne Schichten neben solchen mit Schichten | nur Fläche (Übergänge, Strahlungsaustausch) | **keine** |
| opakes Außenbauteil ohne Schichten **und** ohne U | benannt abgelehnt (`SIMENG_G3_UWERT_FEHLT`, `BauteilEingang.Pruefen`) | — |

`Baujahrregel` liefert nur die Baualtersklasse. Die U-Vorgaben je Klasse und Energiestandard kommen
aus `GebaeudeVorgaben` (Median der Katalogsätze, sonst der freie Wert nach Stein/Loga, E51).
`Waermekapazitaetsrueckfall` greift nur im Import und nur für c einer Schicht.

### 1.3 Folgerung

Der Bauteilweg braucht die Schichtfolge. Für die Speichermasse gibt es zwei geschlossene Grenzfälle,
„alle Bauteile mit Aufbau“ und „keines“. Dazwischen fehlt jede Ersatzmasse. Ein Import, der nur an
einem Teil der Hülle Schichten findet, rechnet die übrigen Hüllflächen ohne Kapazität. Die
Gebäudebauart folgt dagegen nur dann aus den Schichten, wenn **jedes** opake Hüllbauteil einen
vollständigen Aufbau trägt (`GebaeudeAggregation.Bauart`). Die Bauweise des Gebäudes wirkt aber
nur im Klassenweg. Die Masse der Bauteile ohne Aufbau geht damit verloren.


## 2. Was der Import heute baut

### 2.1 Die Kette

`EPOS.Kern/Allgemein/Import/Gebaeude/` mit `Ifc/IfcAbbildBauer.cs` und dem gbXML-Leser:

1. **U-Wert:** `Pset_<Klasse>Common.ThermalTransmittance`, sonst jede Eigenschaft `UValue` eines
   beliebigen Satzes, darunter die CAD-Sätze (`IfcAbbildBauer.UWERT_NAMEN`). Eine Einheit `W/(m K)`
   im Namen wird übergangen, ein Wert ≤ 0 ebenso (`IMP_IFC_PROT_UWERT_*`).
2. **Schichten:** `IfcMaterialLayerSetUsage` → `IfcMaterialLayerSet`, Dicke je Schicht, λ aus
   `Pset_MaterialThermal`, ρ aus `Pset_MaterialCommon`, c aus `SpecificHeatCapacity`
   (`IfcAbbildBauer.Aufbau`, `PSET_STOFF_THERMISCH`). Die Raumseite folgt aus der Nutzung
   (`Schichtfolge`). gbXML: `Construction` → `Layer` → `Material` mit Dicke, λ, ρ, c oder R
   (Datenaustauschkonzept 3.6).
3. **Stoffwertband und Namensabgleich:** Werte gelten nur im Band (λ 0,005–500, ρ 5–8 000,
   c 100–5 000). Fehlt einer, sucht `Baustoffabgleich` über den Materialnamen in der Kette N1–N7:
   Aufbereitung, Katalogname, Synonym, Teilwort, Sonderfall Luft oder Schraffur, gemerkte Zuordnung
   je Projekt. Für c allein greift zuletzt `Waermekapazitaetsrueckfall.TAFEL` (sieben Stoffgruppen
   nach DIN EN ISO 10456).
4. **Aufbau oder U** (`GebaeudeBauteilvorschlag`, Kommentar „Aufbau und U-Wert“, Methode um
   `AufbauFuer`/`UVorgabe`): Ein **vollständiger** Aufbau wird geschrieben, und **U bleibt leer**
   (der Bauteilweg rechnet aus dem Aufbau). Ein U der Datei wird nur verglichen und ab 5 %
   Abweichung gemeldet: „Es rechnet der Aufbau“. Ohne vollständigen Aufbau gilt **nur U**, in dieser
   Folge: Datei, masselose Schichtung, Vorgabe der Baualtersklasse (Herkunft `VORGABE`).
5. **Innenmasse nach Datenlage** (`Innenweg`): Innenbauteile werden nur übernommen, wenn **jede**
   innere Trennfläche Fläche und vollständige Stoffwerte trägt. Sonst bleibt die Innengruppe im
   Klassenweg mit Innenflächenfaktor aus der Datei oder der Vorgabe.
6. **Bauart aus Schichten:** raumseitige 10 cm (`SchichtwerteNaht.WIRKSAME_TIEFE_M`), nur wenn
   jedes opake Hüllbauteil einen vollständigen Aufbau trägt, sonst „schwer“.

### 2.2 Was gespeichert wird

- `Tab_Bauteil`: Fläche, U_Wert, `ID_Aufbau`, Neigung, Azimut, Randbedingung, ψ·L, **eine**
  `Herkunft` je Zeile und `Quellkennung`.
- `Tab_Bauteilaufbau` mit `Herkunft`, `Quelle`, `Quellkennung`. Dazu `Tab_Bauteilschicht` mit
  Reihenfolge, Dicke, Luftschicht, Wertekopie λ/ρ/cp und `ID_Baustoff` auf die Projektkopie
  `Tab_Baustoff`.
- Die **Herkunft je Wert** (U, Aufbau, Fläche, Neigung, Azimut, g) lebt nur im Vorschlag
  (`GebaeudeBauteilzeile.HerkunftU`, `HerkunftAufbau` …) und geht beim Schreiben auf die eine
  Zeilenherkunft zurück.
- Testdatenbank: 132 Katalogbaustoffe (`Tab_Baustoff_STAMM`, davon 37 Dämmstoffe, 45 Mauerwerk,
  4 Abdichtungen), 212 Synonyme und **kein Katalogaufbau** (`Tab_Bauteilaufbau_STAMM` leer). Von den
  62 Bauteilen der Referenzprojekte trägt **keines** einen Aufbau.

### 2.3 Quellen × Größen

„ja“ = die Quelle liefert es und EPOS liest es heute; „liefert, nicht gelesen“ = in der Quelle
vorhanden, nicht gelesen.

| Größe | IFC allgemein | IFC aus dem CAD-Programm (Anwenderdateien) | gbXML | Projektdatei `.sqproj` | Katalog | Annahme |
|---|---|---|---|---|---|---|
| U je Bauteil | ja (`ThermalTransmittance`) | ja, 92–100 % der opaken Bauteile (`UValue`, gleichwertig `ThermalTransmittance`) | ja (`U-value`) | liefert, nicht gelesen (`UValue` an Level 3 und Aufbau) | — | ja (Baualtersklasse, Energiestandard) |
| Schichtfolge, Dicke | ja (`IfcMaterialLayerSet`) | ja, nur Hüllbauteile mit Körper, 1–7 Schichten | ja | liefert, nicht gelesen (1–24 Schichten) | Aufbaukatalog leer | **nein** |
| λ | ja | ja in 3 von 6 Dateien | ja | liefert, nicht gelesen | ja (Namensabgleich N3–N7) | — |
| ρ | ja | ja in 3 von 6 Dateien | ja | liefert, nicht gelesen | ja | — |
| c | ja | **in keiner Datei** | ja | liefert, nicht gelesen (`HeatCapacity`) | ja | ja (Tafel ISO 10456, nur Schicht) |
| wirksame Kapazität je Bauteil | — | — | — | liefert, nicht gelesen (`SpecificComponentMass`, `Cp03`, `Cp10` an `BmElement`) | — | Gebäude: `Bauweise` |
| Fläche, Orientierung, Neigung | ja | ja | ja | liefert, nicht gelesen (`NetArea`, `Orientation`, `Slope`) | — | Vorgabe nach Art |
| Randbedingung | ja (Raumbegrenzung, HC-1 zweiseitig) | ja (`HSETU_Bauteilreferenzen`) | ja | liefert, nicht gelesen (`AdjacentType`) | — | — |
| Innenbauteile mit Aufbau | selten | **nein** (Konzept HottCAD-Verbund 3.4) | wenn exportiert | liefert, nicht gelesen | — | Klassenweg |

EPOS liest aus der Projektdatei heute Gebäude, Geschosse, Räume, Zonen, Profile, Ganglinien und
Kalender (SQ-1 bis SQ-3, `SqprojLeser.TABELLEN`), aber **keine Bauteiltabelle**.


## 3. Was die Quellen hergeben

### 3.1 Die sechs IFC-Anwenderdateien unter `Quellen/`

Ausgezählt mit einem Lesewerkzeug außerhalb des Repositoriums. Opak = `IfcWall`,
`IfcWallStandardCase`, `IfcSlab`, `IfcRoof`. Die Dateinamen sind gekürzt.

| Datei | opak | mit U > 0 | mit Schichtsatz | Schichten je Satz (Median / max) | λ, ρ am Material | c | Wände: U(Schichten) / U(Datei) − 1 |
|---|---|---|---|---|---|---|---|
| MFH 1964 | 188 | 180 | 1 | 7 / 7 | nein | nein | — |
| MFH 1984 | 191 | 185 | 67 | 3 / 3 | ja | nein | Median +18,8 % (n = 61) |
| Produktion mit Verwaltung 2026 | 406 | 374 | 70 | 3 / 4 | nein | nein | — |
| Sportheim 1970 | 356 | 356 | 105 | 1 / 3 | ja | nein | Median +14,6 % (n = 92) |
| Verwaltung 2014 | 641 | 640 | 235 | 3 / 6 | nein | nein | — |
| Wohngebäude EH55 2026 | 180 | 174 | 82 | 1 / 3 | ja | nein | Median 0,0 %, max +23 % (n = 75) |

- Wo beide U-Eigenschaften stehen, tragen `UValue` und `ThermalTransmittance` denselben Wert (1 465
  von 1 465 Paaren). Der Beschreibungstext des CAD-Satzes nennt den U-Wert „inklusive der
  Zusatzdämmungen“. Der Schichtsatz ist also der CAD-Aufbau, nicht zwingend der energetische.
- Keine der 428 Schichten mit λ ist 5 mm dünn oder dünner. Die CAD-Exporte führen Folien und Sperren
  gar nicht.
- Drei Dateien liefern nur Materialnamen und Dicken. Ihre Schichten werden nur über den
  Namensabgleich vollständig. Auch in den drei anderen Dateien kommt c immer aus Katalog oder Tafel.
- Die Prüfproben unter `Referenzlaeufe/Importproben/` (`ifc4_schichten*.ifc`,
  `ifc2x3_schichten.ifc`, `ifc4_schichtdicken_mm.ifc`, `gbxml_rwert_schicht.xml`,
  `gbxml_ohne_konstruktionen.xml`) sind synthetisch und decken die Lesewege ab, nicht die Datenlage
  der Anwender.

### 3.2 Projektdatei

Nach [Befund Projektdatei](2026-10-05_Befund_HottCAD_Projektdatei.md) 3.5 und 6.2, belegt an sechs
gerechneten Dateien:

- **Level-3-Zeilen von `BmElement`** sind raumbezogene Hüllflächen mit `UValue`, `NetArea`,
  `GrossArea`, `Orientation`, `AdjacentType`, über `BmElementReference` an genau einen Raum gebunden.
  `RepositoryElementUUID` zeigt auf das CAD-Objekt (Level 2). Dessen `GId` trifft die IFC-`GlobalId`
  (75 von 75 Wänden beim Wohngebäude EH55, Befund 6.6).
- **`CatalogDimUUID` → `TcBuildingElementDimension`** (U, R, Dicke, Rahmenanteil, g) trifft bei 74 bis
  100 % der Hüllflächen. **`TcBuildingElementDimensionLayer`** trägt `LayerType`, `MaterialType`,
  `Thickness`, `ThermalConductivity`, `Density`, `HeatCapacity`, Emissivität und
  Diffusionswiderstand. Alle Aufbauten haben U > 0, alle Schichten λ und Dicke > 0.
- `BmElement` trägt zusätzlich `SpecificComponentMass`, `Cp03`, `Cp10` und die Zusatzdämmung innen
  und außen (`AddIns…`).
- **Offen** für einen Leser: die Codes von `LayerType` und `MaterialType`, die Einheit von
  `HeatCapacity` (J/(kg·K) oder kJ/(kg·K), Einheitenfalle wie Mehrzonenkonzept 3.4), die Richtung
  der Schichtfolge und ob die `AddIns…`-Dämmung im Aufbau steckt oder dazukommt. Die Sportheim-Datei
  ist ein Zwischenstand ohne Wände in Tabellen. Binärströme bleiben ausgeschlossen (Konzept
  HottCAD-Verbund 9). Die Aufbauten liegen relational und sind ohne sie lesbar.


## 4. Kernfrage: vereinfachter Aufbau oder vollständige Schichtfolge?

### 4.1 Rechenproben

Die Reduktion ist dieselbe wie `Bauteilreduktion.Reduzieren` (7 d, je m², ohne Übergänge im
Kettenprodukt), nachgerechnet außerhalb des Kerns. Die Stoffwerte sind Tafelwerte, keine
Produktdaten. Die Abweichung ist jeweils gegen „voll“ der Zeile davor gerechnet.

| Aufbau (innen → außen) | U [W/(m²K)] | R₁ [m²K/W] | C₁,korr [kJ/(m²K)] | ΔU | ΔR₁ | ΔC₁ |
|---|---|---|---|---|---|---|
| **W1** Gipsputz 15, Kalksandstein 175, Kleber 5, EPS 160, Oberputz 7 — voll | 0,202 | 0,081 | 338 | | | |
| W1 ohne Kleber (≤ 5 mm) | 0,202 | 0,080 | 330 | +0,1 % | −2,0 % | −2,5 % |
| W1 Ersatz: Innenputz, Kalksandstein, Dämmung mit dem R aller äußeren Schichten | 0,202 | 0,080 | 330 | 0,0 % | −2,0 % | −2,5 % |
| W1 Ersatz: Putz und Stein zu einer Schicht zusammengelegt | 0,202 | 0,068 | 329 | 0,0 % | −16,6 % | −2,7 % |
| W1 Schichtfolge umgekehrt (Dämmung innen) | 0,202 | 0,539 | 26 | 0,0 % | +564 % | **−92 %** |
| **W2** Holzrahmen: Gipskarton 12,5, PE-Folie 0,2, OSB 15, Gefach 160, Holzfaser 60, Putz 8 — voll | 0,191 | 0,155 | 37 | | | |
| W2 ohne Folie | 0,191 | 0,157 | 37 | 0,0 % | +1,3 % | −0,9 % |
| W2 Ersatz: Gipskarton, OSB, Dämmung mit dem R der äußeren Schichten | 0,191 | 0,103 | 34 | 0,0 % | −33,5 % | −9,2 % |
| **W3i** Altbau 365 Vollziegel mit Innendämmung 60 und Gipskarton — voll | 0,398 | 1,197 | 162 | | | |
| W3i ohne Folie | 0,398 | 1,207 | 165 | 0,0 % | +0,9 % | +1,4 % |
| W3i Dämmung außen statt innen gedacht | 0,398 | 0,158 | 528 | 0,0 % | −87 % | **+226 %** |
| **D1** Flachdach: Putz 10, Stahlbeton 200, Bitumen 4, EPS 200, Bitumen 5 — voll | 0,165 | 0,047 | 495 | | | |
| D1 ohne beide Bitumenlagen | 0,167 | 0,047 | 491 | +0,9 % | −0,6 % | −0,9 % |
| **W4** monolithischer Dämmziegel 365, Innen- und Außenputz — voll | 0,231 | 0,494 | 112 | | | |
| W4 Ersatz: nur der Ziegel, Putze ins R | 0,231 | 0,664 | 115 | 0,0 % | +34 % | +3,1 % |
| **I1** Innenwand Kalksandstein 115, beidseitig 15 Putz (C₁ symmetrisch) — voll | — | 0,039 | 122 | | | |
| I1 ohne Putz | — | 0,019 | 104 | — | −51 % | −15 % |

### 4.2 Antwort

- **Dünne Schichten (Folien, Sperren, Kleber, ≤ 5 mm)** ändern U, R₁ und C₁ um höchstens 2,5 %.
  Sie sind für die Simulation nicht relevant.
- **Die Lage der Dämmung ist die wichtigste Eigenschaft des Aufbaus.** Bei gleichem U und gleicher
  Gesamtkapazität ändert sie C₁,korr um eine Größenordnung. „U plus flächenbezogene Kapazität“ ohne
  Lage reicht deshalb **nicht**.
- **Raumseitige Schichten bis zur ersten Dämmschicht bestimmen R₁ und C₁.** Wer sie zusammenlegt oder
  weglässt, verschiebt R₁ um 17 bis 51 % (W1, W4, I1). Sie bleiben Einzelschichten.
- **Außen der Hauptdämmung** wirken die Schichten fast nur über ihren Widerstand. Bei massiven
  Bauteilen genügt es, sie in die Dämmschicht einzurechnen (W1: −2 %). Bei leichten Bauteilen kostet
  das bis zu einem Drittel von R₁ (W2), dort bleiben sie besser einzeln.
- **Ein Ersatzaufbau aus drei bis vier Schichten** (raumseitige Bekleidung, speichernde Schicht,
  Dämmschicht mit U-treuem Widerstand, gegebenenfalls äußere Schale) trifft U exakt und C₁ bei
  massiven Bauteilen auf wenige Prozent. Bei leichten Bauteilen liegt er auf rund 10 bis 35 %. Er ist
  eine brauchbare **Annahme für Bauteile ohne Schichten** (Stufen B und C), kein Ersatz für einen
  vorhandenen vollständigen Aufbau.
- **Vollständige Übernahme:** Aus der IFC geht sie nicht. Es fehlen c überall, λ und ρ in der Hälfte
  der Dateien, Innenaufbauten ganz, und die Schichtsätze geben den energetischen Aufbau nicht
  sicher wieder (U-Abweichung im Median bis +19 %). Aus der Projektdatei geht sie für die Hülle: Sie
  führt den energetischen Aufbau je Hüllfläche mit allen vier Stoffwerten. Offen sind nur die vier
  Punkte aus 3.2.


## 5. Vorschlag

### 5.1 Relevanzregel für Schichten

Eine Schicht wird beim Import **weggelassen**, wenn sie eine der folgenden Bedingungen erfüllt:

- (a) Dicke ≤ 5 mm **und** Anteil < 2 % an Σ d/λ **und** Anteil < 2 % an Σ ρ·c·d;
- (b) Stoffgruppe „Abdichtungen“ oder N6-Sonderfall „kein Stoff“ (Folie, Sperre, Schraffur),
  unabhängig von der Dicke, solange Bedingung (a) für R gilt.

Weggelassene Schichten zählen im Protokoll, nicht im Aufbau. **Begründung:** Die Proben in 4.1
zeigen ≤ 2,5 % Wirkung. Die Schwelle 2 % liegt unter dem Hinweisband von 10 % für U. Eine
Dämmschicht oder eine speichernde Schicht kann die Regel nicht treffen, weil sie über einem der
beiden Anteile liegt.

**Fehlt ein Wert einer relevanten Schicht** und füllt ihn der Abgleich nicht, verwirft der Import
heute den ganzen Aufbau. Künftig gilt:

- **Fehlt nur c:** wie heute über die Tafel.
- **Fehlt λ oder ρ einer nicht dämmenden Schicht:** Stoffgruppenwert aus einer erweiterten Tafel
  (Herkunft „Annahme“).
- **Fehlt λ einer Dämmschicht:** Der Aufbau bleibt, der U-Wert der Datei bestimmt den Widerstand der
  Dämmschicht (U-Abgleich wie im Ersatzaufbau, 5.3). Ohne U der Datei fällt das Bauteil auf Stufe C.

### 5.2 Zuordnungsstufen je Bauteil

| Stufe | Bedeutung | U in Gl. (27) | Schichten für R₁/C₁ | Kennzeichen |
|---|---|---|---|---|
| **A** | vollständiger relevanter Aufbau aus Datei, Projektdatei oder Katalog | **U der Datei**, wenn vorhanden, sonst aus den Schichten | der Aufbau | Herkunft je Wert: U (Datei oder Schichten), Aufbau (IFC — auch aus der Projektdatei —, GBXML, KATALOG, MANUELL) |
| **B** | U aus der Datei, kein vollständiger Aufbau | U der Datei | **Ersatzaufbau** (5.3), Dämmschicht auf U abgeglichen | U „aus Datei“, Aufbau „Annahme (Typaufbau …)“ |
| **C** | nur Geometrie | Vorgabe der Baualtersklasse bzw. des Energiestandards | Ersatzaufbau auf diese U abgeglichen | U „Vorgabe Klasse X“, Aufbau „Annahme“ |
| — | transparent (Fenster, Vorhangfassade) | U und g wie heute | keine | nicht bewertet |

**Drei Änderungen am Bestand:**

1. **Stufe A behält den U-Wert der Datei** in `Tab_Bauteil.U_Wert` neben `ID_Aufbau`. Der Kern
   kann das schon (eingetragener U-Wert hat Vorrang, R_Rest gleicht ab). Die Abweichungsmeldung des
   Imports bleibt bei 5 %, die Herleitung zeigt sie ab 10 %. Gebaut (#785) ist die Protokollmeldung der U-Abweichung ab 10 % statt 5 %. Damit gilt der energetische U-Wert des
   CAD-Programms, und die Schichten liefern die Dynamik.
2. **Stufe B und C tragen einen Ersatzaufbau statt masselos zu rechnen.** Damit verschwindet die
   Lücke aus 1.3 für importierte Gebäude, ohne den Kern zu ändern: Der Ersatzaufbau ist Daten, keine
   Rechenregel.
3. **Innenbauteile** (die IFC liefert keine): Die Innengruppe bleibt im Klassenweg mit der Bauweise,
   solange nicht **alle** Innenflächen Stufe A haben (heutige Regel `Innenweg`). Mit der Projektdatei
   kommen Innenaufbauten in Stufe A, und die Regel bleibt unverändert.

**Bausteine, die tragen:** `Baustoffabgleich` (N1–N7) und die Anwenderzuordnung im Abschnitt
„Baustoffe“ für Stufe A. `Waermekapazitaetsrueckfall` für c. `GebaeudeVorgaben` und
`Baujahrregel` für U in Stufe C. `SchichtwerteNaht`/`Bauteilreduktion` für U und Kapazität. Die
Herkunft je Wert in `GebaeudeBauteilzeile`.

**Was fehlt:**

- die Relevanzregel;
- der Ersatzaufbau samt Typkatalog;
- das gespeicherte Kennzeichen „Ersatz“ (heute ist ein Aufbau mit Herkunft `VORGABE` nicht von
  einem gepflegten zu unterscheiden);
- die Stufe als abgeleitete Größe (`Bauteilzuordnungsstufe` im Kern, aus `Tab_Bauteil.Herkunft`,
  `ID_Aufbau`, `Tab_Bauteilaufbau.Herkunft` und dem Kennzeichen);
- die Anzeige.

### 5.3 Ersatzaufbau aus Typaufbauten

- **Typkatalog** in `Tab_Bauteilaufbau_STAMM`/`Tab_Bauteilschicht_STAMM` (heute leer), herstellerneutral
  aus den Katalogbaustoffen. Rund 14 Typen = Bauteilart × Bauart:
  - Außenwand: massiv außen gedämmt, monolithisch, massiv ungedämmt, massiv innen gedämmt,
    Holzleichtbau;
  - Dach: Stahlbeton gedämmt, Sparrendach;
  - Decke bzw. Boden gegen unbeheizt oder Erdreich: Stahlbeton mit Dämmung unten bzw. oben;
  - Innenwand: massiv, leicht;
  - Innendecke: Stahlbeton mit Estrich, Holzbalken.
- **Wahl des Typs:**
  - Bauart des Gebäudes (leicht/schwer/sehr schwer, `Gebaeudebauweise`) und Baualtersklasse:
    bis Klasse F ungedämmt bzw. schwach gedämmt, ab G gedämmt;
  - bei Bauteilen mit Schichtsatz ohne Stoffwerte der Typ, dessen Stoffgruppen die Materialnamen
    treffen (Namensabgleich auf Gruppen);
  - überschreibbar je Aufbau.
- **Abgleich auf U:** Nur die Dicke der Dämmschicht des Typs wird so gesetzt, dass U (mit R_si/R_se
  nach DIN EN ISO 6946 wie `SchichtwerteNaht`) den Ziel-U trifft. Liegt der Ziel-U über dem U des
  Typs ohne Dämmschicht, entfällt sie, und die speichernde Schicht wird über λ angeglichen. Der
  Typ „massiv ungedämmt“ dient dann als Rückfall.
- **Kennzeichen:** Herkunft `VORGABE` am Aufbau, Spalte `Tab_Bauteilaufbau.Typaufbau` mit dem
  sprachneutralen Code des Typs (`TEXT`, `CHECK` auf die Codes, nullbar, NULL = echter Aufbau) —
  eine Kopie, kein ID-Verweis auf den Katalog, damit Katalogpflege den Aufbau nicht ändert.
  Schemaschritt, siehe 5.6.
- **Gebaut (BA-2, #786) mit Abweichungen:** neun statt rund 14 Typen (die Innentypen entfallen nach E95-5); der Namensabgleich von Schichtsätzen auf Typgruppen und der U-Abgleich der Dämmschicht bei fehlendem λ (5.1) sind nicht gebaut.
- **Ein Ersatzaufbau je Kombination aus Typ und Ziel-U je Projekt**, nicht je Bauteil. Hunderte
  Wandstücke mit gleichem U teilen einen Aufbau.

### 5.4 Sichtbarkeit

**(a) Farbmodus „Aufbau“ in `EPOS.UI/Bausteine/GebaeudeAnsicht.razor`** — der dritte Modus neben
„Zonen“ und „Randbedingung“ (HC-2), nach demselben Bauplan:

- Farbtafel als Konstante: A grün, B gelb, C orange, transparent grau. R0-Flächen (zwischen Räumen
  einer Zone) bleiben halbtransparent.
- **3D:** Raum- und Bauteilkörper je Dreieck in der Farbe der Stufe ihres Bauteils. Die Zuordnung
  Dreieck → Bauteil gibt es seit HC-1 (Klick meldet Raum, Gruppe und Bauteil). Die Stufe kommt im
  Import aus dem Vorschlag, im Gebäudedialog über `Quellkennung` aus den gespeicherten Zeilen
  (HC-4 „Datei erneut lesen“).
- **2D:** Grundrisskanten in der Stufe ihrer senkrechten Bauteile. Decken und Böden stehen nur in
  der Legende.
- **Legende:** Fläche und Zahl je Stufe, getrennt nach AW und IW, Schalter je Stufe, Klick auf einen
  Eintrag hebt die Bauteile hervor. Der Zustand lebt im Dialog, gespeichert wird nichts.
- **Bauteilsteckbrief (E98):** Klick auf ein Bauteil oder eine Raumfläche zeigt Stufe, Fläche, Orientierung, Randbedingung, U mit Herkunft, R₁/C₁, flächenbezogene Wärmekapazität, Schichtliste mit Herkunft, Ersatzaufbau und Fensterwerte. Nur Anzeige, gespeichert wird nichts.
- Wählbar nur, wenn ein Bauteilvorschlag vorliegt. Ohne Vorschlag ist der Modus grau mit Grund, wie
  „Randbedingung“ ohne Klassifikation.

**(b) Liste „Bauteilaufbauten“** im Gebäudeimport (`EPOS.UI/Dialoge/Import/GebaeudeImportDialog.razor`,
neben dem Abschnitt „Baustoffe“):

- **Je Aufbau, nicht je Bauteil:** Stufe, Bauteilart, Zahl und Fläche der Bauteile, U der Datei,
  U aus den Schichten, C₁,korr je m², was fehlt (etwa „λ fehlt: <Materialname>“, „keine Schichten“).
- **Filter** „nur B und C“. Ausgeklappt zeigt die Zeile die relevanten Schichten mit Herkunft je
  Wert und die weggelassenen.
- **Handlungen je Zeile:**
  - Baustoff zuordnen: springt in den Abschnitt „Baustoffe“ auf den Namen;
  - Typaufbau wählen;
  - Katalogaufbau übernehmen: Wahl wie im `BauteilDialog`.

**(c) Im Gebäudedialog:** Die Bauteiltabelle im `ZonenDialog` bekommt eine Spalte „Aufbau“ mit
Stufe und Herkunft und den Filter „ohne vollständige Zuordnung“. Ein Klick öffnet den vorhandenen
`BauteilDialog` (Aufbau aus Projekt oder Katalog, U-Wert leer = aus Aufbau, Hinweis bei
Abweichung). Die Zeile „Bauteilweg/Klassenweg“ der Zone (`GEBZ_ZEILE_*`) nennt dazu die
Flächenanteile der drei Stufen.

### 5.5 Übernahme aus der Projektdatei

- **Lesen:**
  - `SqprojLeser` um die Bauteiltabellen erweitern: `BmElement` (Level 2 und 3), `BmElementReference`,
    `TcBuildingElementDimension`, `TcBuildingElementDimensionLayer`;
  - nur lesend, wie SQ-1, ohne Binärströme;
  - Prüfung der Fassung über `XmTables` wie heute;
  - die Tabellen bleiben optional: Fehlen sie, bleibt es beim Stand SQ-1 bis SQ-3, benannt im
    Protokoll.
- **Zuordnen:** Level-3-Hüllfläche → Level-2-CAD-Objekt → `GId` → IFC-`GlobalId` des Bauteils (Weg
  wie der Raumabgleich in `SqprojRaumabgleich`). Ein IFC-Wandstück ohne eigenes `GId` (abgeleitetes
  Stück, Befund 6.6) erbt über den Raum und die Rolle (`BmElementReference.ReferenceType`) den Aufbau
  der passenden Level-3-Zeile. Bleibt eine Zuordnung mehrdeutig, wird sie nicht geraten, und das
  Bauteil bleibt auf seiner IFC-Stufe.
- **Rangfolge**, wenn beide Quellen etwas liefern: Projektdatei vor IFC-Schichtsatz für Aufbau und
  Stoffwerte, weil sie c trägt und den energetischen Aufbau beschreibt. U der Datei bleibt wie in
  Stufe A. Die Projektdatei liefert auch Innenaufbauten.
- **Rangfolge nach E98, beim Weg „IFC + Projektdatei“ nach der Standprüfung (E108):** Schlägt die Prüfung an, wählt der Anwender im Dialog die Aufbauten der Projektdatei oder der IFC; sonst gilt der IFC-Stand: Aufbau der Projektdatei, wenn U auf 1 % gleich ist, sonst Katalogaufbau der Projektdatei mit dem U der IFC, sonst IFC-Schichten, sonst Ersatzaufbau. Der Raumabgleich läuft über die HottCAD-Eigenschaft `GUID`.
- **Stoffe:** Projektkopie `Tab_Baustoff` mit Herkunft `IFC` und `Quelle` „Projektdatei“ (die Projektdatei ergänzt den IFC-Import; kein neuer Herkunftswert, kein `CHECK` zu ändern). Materialnamen aus der
  Datei (Herstellerprodukte) bleiben Projektdaten und kommen nie in Katalog, Wiki oder Repositorium.
- **Vorab Diagnose** (0,5 PT) an den lokalen Projektdateien neben den IFC-Dateien:
  - Codes von `LayerType` und `MaterialType`;
  - Einheit von `HeatCapacity`;
  - Richtung der Schichtfolge;
  - Rolle von `AddIns…`;
  - Deckung der `GId`-Zuordnung je Bauteilart;
  - Abgleich `UValue` des Aufbaus gegen das U der IFC.

  Ergebnis als Nachtrag zum Befund Projektdatei.
- **Risiken:** Schemaänderungen der Projektdatei zwischen Fassungen (Prüfung über `XmTables`, wie
  heute). Die Einheitenfalle bei c (Band 100–5 000 J/(kg·K) fängt den Faktor 1 000). Mehrfach
  verwendete Aufbauten mit Raumbezug: ein EPOS-Aufbau je `TcBuildingElementDimension`.

### 5.6 Wirkung auf Rechnung, Basis und Schema

- **Rechnung importierter Gebäude:**
  - Stufe A mit U der Datei statt U aus Schichten ändert Gl. (27) um die Abweichung aus 3.1 (bis
    rund 19 %).
  - Stufe B und C mit Ersatzaufbau statt masselos erhöhen C_AW. Die Größe hängt am Flächenanteil
    ohne Aufbau.
  - Beides gilt nur für **neu importierte** Gebäude. Bestehende Projekte bleiben, bis „Datei erneut
    lesen“ samt Übernahme oder ein neuer Import sie anfasst.
- **Basis und Einfrierregeln:** nicht berührt.
  - Kein Referenzprojekt ist importiert.
  - Die 62 Bauteile der Referenzprojekte tragen keinen Aufbau.
  - Der Kern ändert keine Rechenregel.
  - Die Saat der Typaufbauten in `Tab_Bauteilaufbau_STAMM` ist Katalog, nicht „gesäte Gebäudedaten“
    eines Referenzprojekts. Sie hebt die Katalogfassung (`Katalogfassung`, Auslieferungsvorlage).
- **Schemaschritt: ja, einer** — Spalte `Tab_Bauteilaufbau.Typaufbau TEXT` (nullbar, `CHECK` auf
  die Typcodes, Kopie statt ID-Verweis) und die Saat der Typaufbauten
  samt Schichten. Die Nummer wird bei Beauftragung von BA-2 in der Statusdatei angemeldet. Die
  Stufe selbst wird abgeleitet, nicht gespeichert. Die Relevanzregel, das Behalten des Datei-U,
  die Ansicht und der Projektdateileser brauchen kein Schema.


## 6. Wellenzuschnitt

| Welle | Inhalt | PT | Schema | hängt an |
|---|---|---|---|---|
| **BA-1** Kern: Relevanzregel und Datei-U (**gebaut #785**, [Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-06_BA-1_Bauteilaufbau_Kern.md)) | Relevanzregel 5.1 in `GebaeudeBauteilvorschlag`; U der Datei neben dem Aufbau speichern; Stufe A/B/C als abgeleitete Größe (`Bauteilzuordnungsstufe`) mit Herkunft je Wert im Vorschlag; Protokollzeilen; Tests mit den Proben in 4.1 als Sollwerten | 1–1,5 | nein | — |
| **BA-2** Kern: Typaufbauten und Ersatzaufbau (**gebaut #786**, [Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-06_BA-2_Typaufbauten_Ersatzaufbau.md)) | Typkatalog (rund 14 Aufbauten) als Saat, Spalte `Typaufbau`, Wahl und U-Abgleich 5.3, Stufen B/C schreiben Ersatzaufbauten; erweiterte Stofftafel für λ/ρ; Katalogfassung; SQL-Dialekt-Prüfer | 2–3 | **ja** | BA-1 |
| **BA-3** Oberfläche (**gebaut #793**, [Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-07_BA-3_Oberflaeche_Bauteilaufbau.md)) | Farbmodus „Aufbau“ (3D, 2D, Legende), Liste „Bauteilaufbauten“ im Import, mit Bauteilsteckbrief (E98), Spalte und Filter im `ZonenDialog`, Texte in beiden Sprachen; Rasterprobe unberührt, bunit | 2 | nein | BA-1 (BA-2 für Typwahl) |
| **BA-4** Projektdatei: Aufbauten (BA-4a Befund, **BA-4b gebaut #792**, [Protokoll](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-07_BA-4b_Aufbauten_Projektdatei.md)) | Diagnose (0,5 PT, Nachtrag Befund), Leser der vier Tabellen, Zuordnung über `GId`, Rangfolge 5.5, Proben mit synthetischer Projektdatei (keine Anwenderdatei im Repositorium) | 3–4 | nein | BA-1 |
| **BA-5** Wiki und Logbuch | Seite Gebäudeimport um Stufen und Farbmodus, Logbuch-Entwurf | 0,5 | nein | BA-3 |

Summe 9 bis 11 PT. **Reihenfolge:** BA-1 → BA-2 → BA-3. BA-4 kann nach BA-1 parallel laufen. Kein
iOS-Lauf nötig (keine Änderung an Hülle oder `Dienste.*`). Der Kern-Lauf auf ubuntu ist der
Nachweis.


## 7. Fragen an den Anwender — entschieden (E95, 06.10.2026)

Alle Empfehlungen sind übernommen; die Empfehlung ist damit der Entscheid. Reihenfolge: BA-1 nach HC-5.

1. **Gilt in Stufe A der U-Wert der Datei vor dem U aus den Schichten?** — Empfehlung: **ja.** Die
   Schichten liefern R₁ und C₁, der U-Wert der Datei die Transmission. Abweichungen über 10 % stehen
   als Hinweis am Bauteil.
2. **Relevanzschwellen: ≤ 5 mm und je < 2 % an R und C?** — Empfehlung: **ja.** Folien und
   Abdichtungen werden immer weggelassen, solange sie unter 2 % von R bleiben.
3. **Ersatzaufbau statt masselos für Bauteile ohne vollständigen Aufbau?** — Empfehlung: **ja**
   (Stufen B und C, als Daten im Import). Die Alternative wäre, eine Gruppe in den Klassenweg zu
   schicken, sobald ihr ein Aufbau fehlt. Sie wirft vorhandene Schichten weg.
4. **Typwahl aus Bauart und Baualtersklasse, überschreibbar je Aufbau?** — Empfehlung: **ja.**
   Vorgabe „massiv außen gedämmt“ ab Klasse G, „massiv ungedämmt“ bis F, „Holzleichtbau“ bei
   Bauart leicht.
5. **Innenbauteile ohne Projektdatei: Klassenweg mit der Bauweise behalten?** — Empfehlung: **ja.**
   Ersatzaufbauten für Innenwände erst, wenn die Innenflächen sicher gemessen sind (heutige Regel
   `Innenweg`).
6. **Aufbauten aus der Projektdatei übernehmen (BA-4), mit Vorrang vor den IFC-Schichtsätzen?** —
   Empfehlung: **ja, nach der Diagnose.** Sie ist die einzige Quelle mit c und mit Innenaufbauten.
7. **Ein Schemaschritt für das Kennzeichen `Typaufbau` und die Typsaat?** — Empfehlung: **ja.** Ohne
   ihn ist ein Ersatzaufbau von einem gepflegten nicht zu unterscheiden.
8. **Bestehende importierte Gebäude nachziehen?** — Empfehlung: **nein, nur auf Zuruf** über „Datei
   erneut lesen“ samt Übernahme. Die Rechnung gespeicherter Projekte ändert sich nicht still.


## 8. Abgrenzung

Kein neues Paket, kein Lesen der Binärströme der Projektdatei, kein Schreiben der `.sqproj`. Keine
Hersteller- oder Produktnamen aus Anwenderdateien in Katalog, Wiki oder Repositorium. Keine
Anwenderdatei im Repositorium (die Zählungen in 3.1 sind außerhalb entstanden). Keine Änderung an
`Bauteilreduktion` oder `ErsatzparameterRC`: Die Regel für masselose Bauteile in gemischten Gruppen
bleibt, der Import sorgt dafür, dass sie bei importierten Gebäuden nicht mehr greift.
