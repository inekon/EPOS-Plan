# Entwurf MA — Geführter Modellaufbau: Einzonen- und Mehrzonenmodell, Prüfung, Vorschläge, Profile, Export

**Stand 12.10.2026 · Konzeptprüfung und Entwurf, nichts gebaut.** Gelesen auf `97f72a6` (Zweig `ios_migration_september`,
Basis R52, Schema 221). Auftrag der Sitzung „Gebäudesimulation“. Fortsetzung des
[Entwurfs GD](2026-10-11_Entwurf_Gebaeudedialog_Mehrzonen.md) (E130, Wellen GD-1 bis GD-5 gebaut) und des
[Entwurfs BK](2026-10-12_Entwurf_Bauteilkatalog_Hottgenroth.md) (E132 bis E134).

**Leitziel des Anwenders (Wortlaut, 12.10.2026):** „Ziel ist, in möglichst wenig Zeit aus einer importierten Datei ein
vollständiges Modell zur Gebäudesimulation zu erhalten. Detailänderungen sollen dann im Nachgang ebenfalls einfach möglich
sein.“ Dieses Leitziel ist der Maßstab des ganzen Papiers. Jede Frage und jede Welle wird an zwei Größen gemessen: an der
**Zeit bis zum ersten Lauf** und an der **Zahl der Pflichteingaben**.

**Wortlaut des Auftrags (12.10.2026), mit den Ergänzungen des Anwenders vom selben Tag:**

> „Es soll folgendes Vorgehen geprüft werden und im Konzept berücksichtigt werden:
> Es gibt entweder ein Einzonenmodell für ein Gebäude oder ein Mehrzonenmodell.
> Das Einzonenmodell soll einfach vom Benutzer parametrisiert werden können (Bauteilgruppen mit U-Werten, Luftwechsel,
> innere Lasten etc.).
> Das Mehrzonenmodell wird i.d.R. eingelesen mit einer IFC- oder gbXML-Datei. Der Nutzer soll dabei durch die folgenden
> Schritte möglichst einfach geführt werden:
> Es soll dabei die Geometrie auf Vollständigkeit geprüft werden (alle Bauteile korrekt eingelesen, jeder Raum/jede Zone ist
> begrenzt durch Bauteile, jeder Raumtyp/jede Nutzungszone hat mindestens eine Konditionierung — Heizung, Kühlung, Lüftung,
> … — evtl. mit Zeitprofilen, …); alle für die Simulation notwendigen Angaben werden geprüft und korrekt zugeordnet.
> Fehlende Angaben (Bauteile, Konditionierung, …) werden dargestellt (übersichtlich, nicht lange Listen) und es wird dann eine
> sinnvolle Zuordnung vorgeschlagen (z. B. wenn Außenwandbauteile fehlen, dann wird die Außenwand mit dem Aufbau „bestehende
> Außenwand“ ergänzt). Wenn Bauteilaufbauten nicht vorhanden sind (z. B. nur U-Werte), sollen auf Basis der Bezeichnung des
> Baumaterials Ergänzungen und Vorschläge gemacht werden zu den für die Simulation notwendigen Angaben (wie Wärmekapazität des
> Bauteils …). Möglich wäre, bei großen Lücken baujahrestypische Vorgaben auswählen zu können (Baumaterialien, …).
> Der 3D-Viewer und die Flächendarstellung sollen dabei zur Kontrolle und Unterstützung dienen.
> Wichtig ist insbesondere, die Zuordnung von Zeitprofilen mit Kalender möglichst einfach zu gestalten. So sollten bei einem
> nach DIN 18599 erstellten Gebäude die Profile der Zonen/Nutzungstypen zugeordnet werden (oder andere vorgegebene Profile) —
> mit einem Vorschlag (z. B. Kalender für Heizung mit Ferien/Urlaub/Saison, Soll-Temperatur; analog für Kühlen, Lüften,
> interne Lasten, …).
> Es soll möglichst einfach das Gebäudemodell nach dem Import ergänzt/vervollständigt werden.
> Beim Export (IFC, gbXML) sollen dann alle Daten, auch die zugefügten, mit exportiert werden. Empfehlungen, welches Format
> besser für den Export ist, sollen gegeben werden (um möglichst wenig Informationen zu verlieren). Dabei fehlen ggf. die
> Bauteilaufbauten.“

**Ausgangslage aus einem früheren Befund (Bild des Anwenders):** Der IFC-Standard sieht alles nur optional vor. Zonen stehen als
`IfcZone`, ab IFC4 als `IfcSpatialZone` mit Typ `THERMAL`. Die Nutzung steht in `ObjectType`/`LongName` am Raum, in
`Pset_SpaceCommon.Category` und in `Pset_SpaceOccupancyRequirements`. Solltemperaturen stehen in
`Pset_SpaceThermalRequirements`, Lasten in `Pset_SpaceThermalLoad`/`Pset_SpaceThermalDesign`. Zeitprofile sind im Schema
möglich, werden in der Praxis aber kaum exportiert. Die sechs IFC-Dateien des Anwenders unter `Quellen/` stammen aus demselben
CAD-Programm. Keine trägt Zonen oder eine der Standardgruppen. Jeder Raum trägt herstellereigene Eigenschaftssätze:
Raumtemperatur `InsideTemperature` (alle Räume, 5 Werte), Raumtyp `RoomType` (15 Nutzungsarten), Beheizungsart `HeatingType`
(2), Lüftungsart `RoomVentilationType` (4), Mindestluftwechsel `RatedMinAirExchangeRate` (3), Raumheizlast nach
DIN EN 12831 (an 44 von 56 Räumen), Lüftung nach DIN 1946-6 (an 2). Auf Gebäudeebene steht eine Gebäudenutzung. Hier stehen
nur Struktur und Zählungen, keine Werte.

## 0 Das Ergebnis in Punkten

- **Leitziel:** Aus einer importierten Datei soll in möglichst wenig Zeit ein vollständiges, rechenfähiges Modell werden, und
  Details sollen sich danach einfach ändern lassen. Daraus folgt der Kern dieses Entwurfs: **eine Prüfung mit fünf Gruppen und
  einem Rechenstatus** („vollständig“, „rechnet mit Vorgaben“, „nicht rechenfähig“), dazu **ein Knopf „Alle Vorschläge
  übernehmen“**. Danach kann der Anwender rechnen. Im Regelfall braucht der Weg von der Datei zum ersten Lauf damit nur
  Datei wählen → Zonierung bestätigen → „Alle Vorschläge übernehmen“ → OK. Pflichteingaben gibt es keine, solange eine
  Klimaregion im Projekt steht.
- **Fast alle Bausteine stehen schon. Es fehlt die Zusammenfassung.** Gebaut sind:
  - Zonierung mit Raumplatzierung (G6c, GD-3);
  - Bauteilvorschlag mit Ersatzaufbau aus neun Typaufbauten (G4b, BA-2);
  - Befund je Bauteil im Farbmodus „Befund“ (G5-B1) und Zuordnungsstufe A/B/C im Farbmodus „Aufbau“ (BA-3);
  - Vorbesetzung jeder Zone mit einem Nutzungsprofil (GD-5) und der Generator der fünf Konditionierungskalender (NP1);
  - Mehrzonenübernahme aus der gespeicherten Quelle (GD-4);
  - Baualtersklassen A–M mit Vorgaben aus dem Katalog (E47, U12);
  - Exporte gbXML und IFC (G7a–G7e).

  Was fehlt, ist eine **Prüfung über das ganze Modell**. Heute meldet jeder Schritt einzeln, als schwerste Meldung, Banner oder
  Liste. Auch die Übernahme aller Vorschläge in einem Vorgang fehlt.
- **Drei echte Lücken:**
  1. Es gibt **keine Prüfung „Zone von Bauteilen umschlossen / Hülle geschlossen“**. Das Mehrzonenkonzept 5.3 fordert sie, im
     Kern ist sie nicht gebaut.
  2. Eine Zone ohne Profil **erbt still** die Kalender des Gebäudes. GD-5 zeigt das zwar als „geerbt“, aber nicht als Befund.
  3. **Ein Wiederimport verliert Handergänzungen:** GD-4 ersetzt Zonen, Bauteile und Zonenkalender in einem Vorgang.
- **Vorschläge:** Fehlende Bauteile und Aufbauten werden in einer festen Rangfolge vorgeschlagen:
  1. Aufbau des Projekts gleicher Bauteilart („bestehende Außenwand“);
  2. geeigneter Katalogaufbau (BK) nach Bauteilart, Baustoffname, Stoffgruppe und Nähe zum U-Wert;
  3. Typaufbau nach Bauart und Baualtersklasse.

  Die Wärmekapazität kommt dabei immer aus den Schichten. Für große Lücken gibt es baujahrestypische Vorgaben **aus dem
  Bestand** (Baualtersklasse, Katalogbauten, Typaufbauten). Normtabellen kommen nicht ins Repositorium (E131; ausgenommen die
  DIN/TS-18599-10-Profile nach E135).
- **Profile:** Die Vorschlagsregel je Zone lautet **DIN-Nummer → Raumtyp der Quelle → Gebäudeart → EPOS-Muster**, DIN-Profile mit Werten (E135). Quelle sind
  der ganze Katalog der Nutzungsprofile (DIN, EPOS-Muster, eigene) und die Zuordnung. Ferien und Heizperiode kommen aus dem
  Projekt. Alle Zonen stehen auf **einem Blatt**, mit Mehrfachwahl.
- **Ansicht:** Ein Klick auf eine Ampel stellt die Gebäudeansicht auf den passenden Farbmodus und hebt die betroffenen Körper
  hervor. Der Vorschlag lässt sich dort je Legendeneintrag übernehmen oder verwerfen. Das baut auf AB, AB2 und der geplanten
  Welle AB4 auf.
- **Export:** Für die Rückgabe ans CAD-Programm wird **IFC** empfohlen (G7d, Anreicherung der Originaldatei). Für die Übergabe an
  andere Simulationswerkzeuge wird **gbXML** empfohlen: Es trägt Schichtfolgen samt Stoffwerten vollständig, aber heute **keine
  Zeitpläne** (benannter Verlust „Kalender“). Vorschlag: gbXML schreibt `Schedule`, IFC schreibt zusätzlich die Standardsätze
  (`Pset_SpaceOccupancyRequirements`, `Pset_SpaceThermalLoad`). `EPOS_*`-Sätze bleiben die Rückfallebene.
- **Wellen MA-1 bis MA-8, zusammen rund 18,5–26,5 PT.** Schon **MA-1** bringt den schnellen Weg: Prüfung, „Alle Vorschläge
  übernehmen“ und Profile. Rechenweg und Referenzbasis bleiben unberührt. Voraussichtlich ist kein Schemaschritt nötig.

## 1 Bestand

### 1.1 Was vom gewünschten Vorgehen schon steht

| Wunsch | Stand | Beleg |
|---|---|---|
| Einzonenmodell einfach parametrisieren | **steht**: Klassenweg „U-Wert-Gruppen und Bauweise“ im Gebäudeeditor. Kopf zweigeteilt in „Beschreibung und Einordnung“ und „Rechengrößen“ mit Herkunftsmarken. Luftwechsel, innere Gewinne, Sollwerte, Kalender und Katalogbauten mit Baualtersklasse | GD-1 (#1015, E130 F1/F2/F6), `EPOS.UI/Dialoge/Bedarf/GebaeudeKatalogDialog.razor` |
| Baujahrestypische Vorgaben | **teilweise**: 13 Baualtersklassen A–M (E47). Vorgaben je Klasse als Median der Katalogsätze, sonst als freier Wert (U12, E51; `GebaeudeVorgaben.Baualtersvorgabe`). Typaufbauten nach Bauart × Klasse (E95 Nr. 4, BA-2 #786). **Genutzt werden sie nur im Import.** Im Gebäudeeditor ist die Klasse ein Feld ohne „Vorgaben übernehmen“ | `EPOS.Kern/Allgemein/Import/Gebaeude/GebaeudeVorgaben.cs`, `Baujahrregel.cs`, `EPOS.Kern/Allgemein/Update/TypaufbauSaat.cs`; `GebaeudeImportHuelle` ist der einzige Aufrufer der Vorgaben |
| Mehrzonenmodell aus IFC/gbXML | **steht**: Zonierungsregeln Z1–Z5/X1–X4, Zonenbaum, Bauteilvorschlag für N Zonen, Trennflächen | G6c (umgesetzt), Mehrzonenkonzept 6.1–6.4 |
| … und aus der Projektdatei des CAD-Programms | **steht**: Wahl DIN-V-18599-Zonen oder Simulationszonen (E87 F1), Gebäudewerte der Datei (GD-2 #1016), Räume ohne Zone werden platziert (GD-3 #1019) | [HottCAD-Verbund](2026-10-05_Konzept_HottCAD_Verbund_IFC_Projektdatei_Viewer.md) 5 |
| Mehrzonenmodell nachträglich | **steht**: „Als Mehrzonenmodell übernehmen…“ aus der gespeicherten Quelle mit Prüfsumme, ersetzt in einem Vorgang | GD-4 (#1026, E130 F7), `MehrzonenuebernahmeCtrl` |
| Bauteile korrekt eingelesen | **steht je Bauteil**: Befund mit vier Stufen und sieben Gründen (ohne Befund; Körper unlesbar; ohne Eigenschaften, d. h. kein U oder keine Fläche; ohne Bauteil) | `EPOS.Kern/Allgemein/Import/Gebaeude/Bauteilbefund.cs`, `EPOS.UI/Dialoge/Bedarf/GebaeudeBefundDaten.cs` (G5-B1) |
| Bauteilart erkannt | **steht**: Einordnung der Hülle an einer Stelle, Randbedingung zweiseitig, Flächengruppen R0–R7 | G4b Welle B (`GebaeudeHuelleneinordnung`), HC-1 |
| Raum/Zone von Bauteilen begrenzt, Hülle geschlossen | **fehlt.** Gebaut sind die Regeln zwischen den Zonen (Nachbar, Trennflächenbilanz, ψ·L) und die Gegenprobe der Trennflächen ab 2 %. Die Prüfung „geschlossene Hülle je Zone“ (Σ A·n ≈ 0, Verankerung oben/unten) aus dem Mehrzonenkonzept 5.3 ist nicht gebaut. Flächen ohne Bauteil zeigt nur die Ansicht | `GebaeudeZonenCtrl.Pruefen` → `Zonenkopplungsregeln`; Mehrzonenkonzept 5.3 |
| Aufbau fehlt, nur U-Wert | **steht für U**: Stufen A/B/C mit Ersatzaufbau, Dämmschicht auf den U-Wert abgeglichen (E95 Nr. 3/4). **Fehlt**: der Namensabgleich von Schichtsätzen auf Typgruppen (BA-2 nicht gebaut). Der Vorschlag eines **Katalogaufbaus** aus BK (Katalog seit BK-3 #1031 gefüllt) fehlt ebenfalls | [Bauteilaufbau](2026-10-06_Konzept_Bauteilaufbau_Import.md) 5.2/5.3 |
| Wärmekapazität aus dem Baustoffnamen | **steht je Schicht**: `Baustoffabgleich` (Namenskette N1–N7), `Waermekapazitaetsrueckfall`, Baustoffwahl an der Schicht (AB3 #1014) | `EPOS.Kern/Allgemein/Import/Gebaeude/` |
| Konditionierung je Zone | **steht als Vorbesetzung**: Rangfolge je Größe Ganglinie vor Datei vor Profil vor Erbe. Profil der DIN-Nummer, sonst Profil der Gebäudeart. Spalte „Nutzung/Profil“, Aktion „Profil/Kalender zuweisen…“ | GD-5 (#1020, E130 F5), `Zonenvorbesetzung` |
| Profile nach DIN 18599 | **steht**: Katalog mit Kategorien DIN (ohne Werte, Normwerte vom Anwender einlesbar), EPOS-Muster und eigenen Profilen. Generator der fünf Kalender. Zuordnung DIN-Nummer / IFC-Nutzungsklasse / Raumtyp | NP1–NP7 (E90, E129, E131, #1022), [Nutzungsprofile](../Konzept_Nutzungsprofile_EPOS-Plan.md) 4.3, 5.4 |
| Ferien, Saison, Sollwert im Heizkalender | **steht im Kalender**: Ferien und Heizperiode des Projekts, Perioden im Gemeinjahr (KS #1002). Der Generator legt die Ferienzeiträume **des Ziels** an (P12) | [Konditionierungsprofile](../Konzept_Konditionierungsprofile_EPOS-Plan.md), Nutzungsprofile 4.3 |
| 3D-Viewer und Flächendarstellung | **steht**: Farbmodi Zonen, Randbedingung (R0–R7), Aufbau (A/B/C), Befund. Legende mit Zahl und Fläche, Steckbrief je Bauteil (E98). Zuordnung über die Legende (AB2 #1013). **Geplant**: Ein-/Ausblenden je Legendeneintrag und Bauteilgruppe (AB4) | `EPOS.UI/Bausteine/GebaeudeAnsicht.razor`, [Übergabe](2026-10-12_Uebergabe_Gebaeudesimulation_Kontowechsel.md) 4 Nr. 2a |
| Ergänzen nach dem Import | **teilweise**: Gebäudeeditor, Reiter „Zonen“, Bauteildialog, „Datei erneut lesen“ (nur Ansicht, HC-4). **Fehlt**: Mehrfachwahl „Profil zuweisen“ (offen aus GD-5) und Bewahren der Ergänzungen beim Wiederimport | #1020 „Offen“, #1026 |
| Export aller Daten | **teilweise**, siehe 1.4 | G7a–G7e, `GebaeudeExportVerluste` |

### 1.2 Was der Import heute als Befund meldet

`GebaeudeBefundDaten` trägt **nur Bauteilbefunde**. Es gibt vier Stufen (ohne Befund, Körper unlesbar, ohne Eigenschaften,
ohne Bauteil) mit sieben Gründen (`Bauteilbefundgrund`). Der Farbmodus „Befund“ färbt danach je Dreieck. Die Legende nennt Zahl
und Fläche je Stufe, der Steckbrief nennt den Grund. Zonen, Nutzung, Kalender, Klima und Orientierung kommen im Befund nicht vor.
Dazu sagen die übrigen Teile des Importdialogs etwas, und zwar jeweils für sich:

- die schwerste Meldung der Zonierung als Banner;
- die Liste der nicht zugeordneten Räume (`Zonenplan.Abschlusspruefung` sperrt OK);
- die Tabelle der Meldungen des Bauteilvorschlags;
- die Liste „Bauteilaufbauten“ mit Stufe je Aufbau;
- die Prüfung der Projektdatei (E108).

**Rund 50 Meldungsschlüssel** der Zonierung, des Plans und des Bauteilvorschlags stehen so nebeneinander. Gerade das will der
Anwender nicht: lange Listen statt einer Übersicht.

### 1.3 Was die Quelldateien für Nutzung und Konditionierung hergeben

Der IFC-Leser liest heute schon `RoomType` (über die Tafel `RAUMTYP_KLASSE` auf elf Nutzungsklassen), `HeatingType`,
`InsideTemperature` und `Pset_SpaceThermalRequirements` (`IfcAbbildBauer`). Zonen liest er aus `IfcSpatialZone` mit Typ
`THERMAL`, sonst aus `IfcZone`. **Nicht gelesen** werden `RoomVentilationType`, `RatedMinAirExchangeRate`,
`Pset_SpaceOccupancyRequirements` und `Pset_SpaceThermalLoad`. Der gbXML-Leser übergeht die `Schedule`-Kette (Datenaustausch 3.4,
13). Die Projektdatei liefert Profilnummer, Ganglinien und Kalender, soweit sie sie trägt (E87 F2).

### 1.4 Was der Export heute trägt

| Daten | gbXML (G7a/G7b) | IFC (G7c–G7e) |
|---|---|---|
| Zonen, Fläche, Volumen, beheizt | `Space`/`Zone`, rundläufig | `IfcSpace`, `IfcZone`, rundläufig |
| Bauteile mit Fläche, U, g, Orientierung, Randbedingung | `Surface`/`Opening`, rundläufig (9 × 5 × 3 Zellen gemessen) | Bauteile mit `Pset_*Common`, rundläufig |
| Schichtfolge mit λ, ρ, c | `Construction`/`Layer`/`Material`, ein Aufbau je Übergangsfall | `IfcMaterialLayerSet` mit `Pset_MaterialThermal` |
| Bauteil nur mit U-Wert | Ersatzschichtung mit wörtlichem Vorbehalt („trifft U-Wert und Gesamtwärmekapazität, nicht die Lage der Masse“) | U am Bauteil; die Schichten fehlen |
| Heizsollwert Tag | `Zone/DesignHeatT` | `Pset_SpaceThermalRequirements`, `EPOS_Zone` |
| Konditionierungskalender (fünf Größen, Perioden, Ferien als Periode) | **benannter Verlust „Kalender“** | `EPOS_Kalender_<Größe>` (Woche mit 168 Zellen, Perioden als Text), **nur von EPOS lesbar** |
| Nutzungsprofil | fehlt (`spaceType` ohne Wohnwert, Datenaustausch 5.2) | `EPOS_Zone.Nutzung` |
| Innere Lasten, Personen | `PeopleNumber`, `EquipPowerPerArea` als Mittelwert ohne Zeitplan | Kalender Geräte/Personen mit `Nennwert_W` in `EPOS_Kalender_*` |
| Nutzerluftwechsel, Nacht- und Wochenendsollwerte | Verlust | `EPOS_Zone` (Teile), sonst Verlust |
| Ferien, Wochenendtage, Feiertagsland des Gebäudes | Verlust | Verlust |

Beleg: `EPOS.Kern/Allgemein/Export/Gebaeude/GebaeudeExportVerluste.cs` (Einstufung je Feld) und
`EPOS.Kern/Allgemein/Export/Ifc/IfcKonditionierungssatz.cs`. Der Export nennt die Verluste vor dem Schreiben.

### 1.5 Entscheide, die gelten

E47 (Baualtersklassen A–M), E80 (kein Klimaimport), E87 (Projektdatei, R0–R7), E95 (Bauteilaufbau: U der Datei vor U aus
Schichten, Ersatzaufbau statt masselos, Typwahl aus Bauart und Klasse, Bestand nur auf Zuruf), E101 (G5), E108 (Projektstand
von IFC und Projektdatei prüfen), E129/E131 (DIN-Profile ohne Normwerte; Normwerte nur aus Eingaben des Anwenders, nie im
Repositorium), E130 (GD F1–F8), E132–E134 (Bauteilkatalog). Alle stehen in
[Status Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md), Abschnitt 1.

### 1.6 Widersprüche und Spannungen

- **Wiederimport und Ergänzungen:** Laut Wortlaut soll das Modell „möglichst einfach … ergänzt/vervollständigt“ werden. GD-4
  ersetzt bei der Übernahme aber Zonen, Bauteile und Zonenkalender. Was der Anwender ergänzt hat, geht dabei verloren
  (#1026). Dasselbe gilt für E95 Nr. 8 („Bestand nur auf Zuruf“). Siehe 2.6 und F7.
- **„Alle Daten exportieren“ gegen die Formate:** gbXML trägt keine Kalender, Standard-IFC kaum welche. Ohne Verlust geht es nur
  mit Erweiterungen (gbXML `Schedule`) oder mit `EPOS_*`-Sätzen. Siehe 2.7 und F8.
- **Baujahrestypische Baumaterialien gegen Normwerte:** Baujahrestypische U-Werte und Aufbauten aus Normtabellen dürfen nicht
  ins Repositorium (E131). Der Bestand (Katalogmedian, Typaufbauten aus Katalogbaustoffen) reicht dafür aus. Siehe F4.

## 2 Zielbild

**Maßstab (Leitziel):** Nach dem Import kommt der Anwender **mit wenigen Klicks** zu einem rechenfähigen Modell. Das heißt:
„Alle Vorschläge übernehmen“, dann rechnen. Jede Detailänderung geht danach im Gebäudeeditor oder in der Ansicht, ohne den
Import zu wiederholen. Ein späterer Wiederimport verliert sie nicht.

### 2.1 Zwei Wege

- **Einzonenmodell:** Der heutige Klassenweg ist schon der einfache Weg: sechs Bauteilgruppen mit Fläche und U-Wert, Bauweise,
  Luftwechsel, innere Gewinne, Sollwerte und Kalender. Einfacher wird er durch zwei Dinge:
  1. **„Vorgaben der Baualtersklasse übernehmen“** im Gebäudeeditor. Gebäudeart, Baualtersklasse bzw. Baujahr und Bauart
     setzen die U-Werte der sechs Gruppen und die Bauweise aus `GebaeudeVorgaben` (Katalogmedian), sichtbar mit Herkunft
     „Vorgabe Klasse X“. So arbeitet der Import heute schon.
  2. **Profil der Gebäudeart** als Vorgabe der Kalender (GD-5 gilt schon im Einzonenweg der Projektdatei, E130 F5).

  Der kürzeste Weg bleibt, einen Katalogbau zu wählen (Gebäudeart × Baualtersklasse).
- **Mehrzonenmodell:** Quellen sind IFC, gbXML und die Projektdatei des CAD-Programms, allein oder zusammen mit der IFC. Der
  Import endet nicht mehr mit einer Reihe einzelner Meldungen, sondern mit der **Modellprüfung** (2.2). Derselbe Ablauf steht im
  Gebäudeeditor für jedes Gebäude mit Zonen bereit, auch für ein von Hand angelegtes.

### 2.2 Modellprüfung: fünf Prüfgruppen, ein Rechenstatus

Ein Kernbaustein `Modellpruefung` liest ein Gebäude (Import-Vorschlag oder gespeichertes Gebäude). Er liefert je Prüfgruppe
einen Stand, eine Zählung und einen Vorschlag. Er **ruft** die vorhandenen Prüfungen und rechnet nichts nach.

| Gruppe | Prüft | Quelle im Bestand | Vorschlag |
|---|---|---|---|
| **1 Geometrie und Bauteile** | jedes Bauteil mit Fläche und erkannter Bauteilart; jede Zone von Bauteilen umschlossen (**neu:** Hülle geschlossen, Σ A·n je Zone, Verankerung oben/unten, Mehrzonenkonzept 5.3); Flächen ohne Bauteil; Trennflächenbilanz | `Bauteilbefund`, `Flaechenklassifikation`, `Zonenkopplungsregeln` | fehlendes Hüllbauteil aus der Körperfläche (Gruppe R1–R7) ergänzen, Aufbau nach 2.3 |
| **2 Aufbauten und Stoffwerte** | Zuordnungsstufe A/B/C je Bauteil; Schicht ohne λ, ρ oder c; Kapazität je Zone | Stufen BA, `Baustoffabgleich`, `Waermekapazitaetsrueckfall` | Rangfolge 2.3 |
| **3 Nutzung und Konditionierung** | jede beheizte Zone mit Profil **und** mindestens Heizkalender; Kühl- und Lüftungskalender, wo die Nutzung sie trägt; unbeheizte Zonen ohne Heiz- und Kühlkalender | `Zonenvorbesetzung`, Herleitung „geerbt“ | Profil nach 2.4 |
| **4 Ort und Lage** | Klimaregion des Projekts gesetzt; Nordrichtung bestätigt (G5-N); Feiertagsland und Ferien des Projekts | Projekt, `Nordrichtung` | Klimaregion des Projekts, Vorgabe-Feiertagsland |
| **5 Rechenfähigkeit** | die Prüfung des Schreibwegs (`GebaeudeZonenCtrl.Pruefen`): Bänder, R_Rest > 0, höchstens 50 Zonen | vorhanden | keiner, nur der Grund |

**Rechenstatus** des Gebäudes, als Kopf über den Gruppen:

- **vollständig** (grün): Keine Gruppe hat einen offenen Punkt.
- **rechnet mit Vorgaben** (gelb): Das Modell ist rechenfähig, trägt aber Annahmen, etwa Ersatzaufbauten (Stufe B/C), geerbte
  Kalender oder Profile aus der Gebäudeart. Die Zählung nennt, wie viel Fläche und wie viele Zonen betroffen sind.
- **nicht rechenfähig** (rot): Gruppe 5 lehnt ab, oder ein Bauteil ist ohne U-Wert bzw. ohne Fläche (Befund rot).

**Darstellung** (gegen lange Listen): eine Zeile je Gruppe mit Ampel, Zählung („7 Bauteile ohne Aufbau, 112 m²“, „3 von 12
Zonen erben Kalender“) und dem Knopf **„Vorschlag übernehmen“**. Darüber steht **„Alle Vorschläge übernehmen“**. Einzelheiten
erscheinen erst auf Klick: als Klappgruppe (`Klappgruppe.razor`) mit höchstens einer Zeile je **Art** des Befunds, nicht je
Bauteil, und je Art einer Gesamtzahl. Die Bauteile selbst zeigt die Ansicht (2.5). Jede Übernahme ist ein Schritt mit
Rücknahme, wie „Zurücknehmen“ der Raumplatzierung (GD-3). Die Prüfung steht an drei Stellen:

- im Importdialog als letzter Abschnitt vor OK;
- im Gebäudeeditor als Kopf des Reiters „Zonen“;
- als Auskunft für den KI-Assistenten (Sicht wie `GebaeudeImportKiSicht`).

### 2.3 Vorschlagsregeln für Bauteile und Aufbauten

**Fehlendes Bauteil.** Eine Hüllfläche ohne Bauteil (Rückfall `FLAECHE_OHNE_BAUTEIL`, HC-1) wird ein Bauteil der Gruppe ihrer
Fläche. Ein Raum ohne Körper bekommt kein Bauteil, der Befund wird benannt.

**Aufbau eines Bauteils ohne vollständigen Aufbau**, in dieser Rangfolge:

1. **Aufbau des Projekts gleicher Bauteilart und Randbedingung**, der im Gebäude schon am meisten Fläche trägt. Das ist die
   „bestehende Außenwand“ des Wortlauts. Liegt ein U-Wert der Datei vor, gilt er weiter vor dem Aufbau (E95 Nr. 1).
2. **Aufbau des Katalogs** (BK, ohne Kennzeichen „meist nicht geeignet“): gleiche Bauteilart; Schichtnamen oder Materialname der
   Datei treffen über `Baustoffabgleich` dieselbe Stoffgruppe; U am nächsten zum U der Datei. Bei Gleichstand gilt die kleinere
   U-Abweichung, dann die Katalogfolge.
3. **Typaufbau** nach Bauart und Baualtersklasse, Dämmschicht auf U abgeglichen (BA-2, heute die einzige Stufe).

Der Vorschlag nennt seine Herkunft („Projektaufbau …“, „Katalog …“, „Typaufbau …“). Die Wärmekapazität kommt immer aus den
Schichten des gewählten Aufbaus, eine Kapazität ohne Lage gibt es nicht (Bauteilaufbau 0 Nr. 1). Hat ein Schichtsatz der Datei
Namen, aber kein λ, ρ oder c, schlägt der Namensabgleich je Schicht den Baustoff vor (AB3). Die noch fehlende Regel „Schichtsatz
→ Typgruppe“ (BA-2) ist Teil von MA-4.

**Große Lücken, baujahrestypisch:** Fehlen U-Werte für viele Bauteile (Stufe C), wählt der Anwender in der Gruppe 2 einmal
Baualtersklasse und Bauart. Daraus folgen U-Vorgabe und Typaufbau für alle Bauteile der Stufe C, mit Rücknahme. Die Werte
kommen aus dem Katalog (Median der Katalogbauten der Klasse) bzw. aus den Typaufbauten der Katalogbaustoffe. Normwerte kommen
höchstens als Eingabe des Anwenders in dessen Datenbank (Regel E131/NP7), nie ins Repositorium.

### 2.4 Zeitprofile und Kalender

**Vorschlagsregel je Zone**, in fester Folge (GD-5 erweitert):

1. **DIN-Nummer** der Zone (Projektdatei, `PdProfileUsage`), über die Zuordnung `DIN_NUMMER`;
2. **Raumtyp der Quelle**: Raumtyp oder Nutzungsklasse der Räume der Zone (IFC `RoomType` → Nutzungsklasse,
   `HOTTCAD_RAUMTYP`, `IFC_KLASSE`; bei gemischten Räumen die Klasse mit der größten Fläche);
3. **Gebäudeart** (`Zonenvorbesetzung.KENNUNG_JE_GEBAEUDEART`);
4. **EPOS-Muster** der Nutzungsklasse „Sonstige“ bzw. „Wohnen“ als letzter Rückfall, damit keine beheizte Zone ohne Profil
   bleibt.

**Quelle der Profile** ist der ganze Katalog: DIN-Profile mit Werten (E135; bis zur Saat mit vom Anwender eingelesenen Normwerten), EPOS-Muster und eigene
Profile. Die Zuordnung (`Tab_Raumnutzungszuordnung`) bestimmt das Profil. Wer eine DIN-Nummer auf sein eigenes Profil umstellt,
bekommt es auch hier (NP-F19). Eine Größe ohne Kennwert im Profil erbt sichtbar vom Gebäude und erscheint in Gruppe 3 als
„rechnet mit Vorgaben“.

**Was der Generator daraus anlegt** (NP 4.3): Heizen mit Sollwert im Betriebsfenster und außerhalb, Kühlen, Lüftung, Geräte
und Personen. **Ferien und Saison** kommen nicht aus dem Profil, sondern aus dem Ziel: Ferienzeiträume und Heizperiode des
**Projekts**, im Gemeinjahr (KS). Fehlen sie, schlägt Gruppe 4 die Vorgabe vor (keine Ferien, Heizperiode nach der Vorgabe des
Projekts) und nennt das.

**Ein Blatt für alle Zonen** („Nutzung und Kalender“): Es ist eine Zeile je Zone mit Profil, Herkunft der Vorgabe und
Kurzbild der Woche je Größe. Mehrfachwahl wendet „Profil zuweisen…“ auf alle gewählten Zonen an (offen aus GD-5). Ein Knopf
übernimmt den Vorschlag für alle Zonen. Das Blatt nimmt die Bausteine der Kalenderkarte und des Nutzungsprofilblatts (NP3).

### 2.5 Ansicht und Flächendarstellung als Kontrolle

Die Prüfgruppen sind an die Gebäudeansicht (`GebaeudeAnsicht.razor`) gekoppelt:

| Prüfgruppe | Farbmodus | Hervorhebung | Übernahme am Körper |
|---|---|---|---|
| 1 Geometrie und Bauteile | „Befund“ bzw. „Randbedingung“ (R0–R7) | Flächen ohne Bauteil, Körper unlesbar, offene Hülle je Zone | Legendeneintrag „ohne Bauteil“: „Bauteil ergänzen“ / „verwerfen“ |
| 2 Aufbauten | „Aufbau“ (A/B/C) | Bauteile der Stufe B/C | je Stufe oder je Aufbau: „Vorschlag übernehmen“ / „verwerfen“; Steckbrief zeigt Vorschlag und Kapazität |
| 3 Nutzung | „Zonen“ | Zonen ohne Profil oder mit geerbtem Kalender | Zuordnung über die Legende wie AB2, Ziel ist ein Profil statt einer Zone |

**Heute geht:** Farbmodi Befund und Aufbau mit Legende (Zahl, Fläche) und Steckbrief; Zuordnung der Räume zu Zonen über die
Legende (AB2); im Gebäudedialog die Ansicht nach „Datei erneut lesen“ (HC-4). **Es fehlt:** der Sprung von der Ampel in den
Farbmodus mit Hervorhebung; Übernehmen/Verwerfen an der Legende; Ein-/Ausblenden je Legendeneintrag und Bauteilgruppe (AB4,
geplant), damit verdeckte Innenflächen prüfbar werden; ein Modus „Nutzung“ für Profile. Die Ansicht bleibt Anzeige und
Bedienung. Gespeichert wird nur, was die Übernahme schreibt (HC-4: keine Persistenz der Geometrie).

### 2.6 Ergänzen nach dem Import, ohne Verlust beim Wiederimport

- **Wege im Gebäudeeditor:** Prüfkopf im Reiter „Zonen“ (2.2), Blatt „Nutzung und Kalender“ (2.4), Bauteiltabelle mit Spalte
  „Aufbau“ und Filter „ohne vollständige Zuordnung“ (Bauteilaufbau 5.4 c). Mehrfachwahl von Zonen und Bauteilen für „Profil
  zuweisen“, „Aufbau zuweisen“ und „Randbedingung setzen“.
- **Wege in der Ansicht:** Zuordnung über die Legende (AB2) und Übernehmen/Verwerfen (2.5).
- **Wiederimport bewahrt Ergänzungen:** `MehrzonenuebernahmeCtrl` ersetzt heute alles. Neu ist eine Zusammenführung:
  - Zonen und Bauteile mit `Quellkennung` (GUID der Datei) werden aus der Datei erneuert.
  - Felder, die der Anwender gesetzt hat (Herkunft `MANUELL` am Satz bzw. Herkunft „Hand“ je Feld nach GD-1), bleiben.
  - Ebenso bleiben von Hand angelegte Sätze ohne Quellkennung, zugewiesene Profile und Kalender sowie gewählte Aufbauten.
  - Was in der neuen Datei fehlt, wird benannt und nicht still gelöscht.

  Die Prüfsumme (GD-4) entscheidet weiter, ob es dieselbe Datei ist. Bei einer anderen Prüfsumme läuft dieselbe Zusammenführung
  mit Vorschau der Unterschiede. Die Spalten `Herkunft` (`MANUELL`, `KATALOG`, `IFC`, `GBXML`, `VORGABE`, `SQPROJ`) und
  `Quellkennung` an Zone und Bauteil tragen das schon. Ob die Kalender der Zone eine Herkunft brauchen, ist in MA-5 zu prüfen.

### 2.7 Export: Empfehlung und verlustfreie Ergänzung

**Empfehlung je Zweck** (steht so im Exportdialog, ein Satz je Format):

- **Rückgabe ans CAD-Programm: IFC**, als Anreicherung der Originaldatei (G7d, nur IFC4). Geometrie und Struktur des CAD bleiben,
  EPOS ergänzt U-Werte, Aufbauten, Zonen und Konditionierung.
- **Übergabe an andere Simulationswerkzeuge: gbXML.** Schichtfolgen mit λ, ρ, c und Fläche, Orientierung und Randbedingung je
  Fläche sind dort eindeutig und werden von verbreiteten Werkzeugen gelesen. Sobald `Schedule` geschrieben wird (unten), trägt
  gbXML auch die Zeitpläne.
- **Austausch zwischen EPOS-Installationen:** Projektexport (`ProjektExportImportCtrl`), weder gbXML noch IFC.

**Verlustfrei ergänzen:**

- **gbXML:** Jeder Kalender wird `Schedule` → `YearSchedule` → `WeekSchedule` → `DaySchedule`, verwiesen über `scheduleIdRef`
  an `Space` (Personen, Geräte, Lüftung) und an den Sollwerten. Ferien und Heizperiode werden Jahresabschnitte. Das Profil wird
  `Space/Name` bzw. `Description`. Der Leser liest dieselben Zeitpläne zurück (Rundlauf in einer Probe). Dafür zu prüfen ist:
  welche Zeitplantypen und Einheiten das Schema 6.01 für Sollwerte erlaubt; ob ein Sollwertplan als Wert- oder als
  Anteilsplan steht.
- **IFC:** Die Standardsätze werden zusätzlich geschrieben: `Pset_SpaceOccupancyRequirements` (Personen, Fläche je Person),
  `Pset_SpaceThermalLoad` (Personen, Geräte, Beleuchtung als Nennwert) und `Pset_SpaceThermalRequirements` (Kühlsollwert,
  Luftwechsel). Kalender bleiben in `EPOS_Kalender_*`, weil das Standard-IFC keinen gebräuchlichen Zeitplan trägt
  (`IfcTimeSeries` wird kaum gelesen). Der Leser liest die Standardsätze auch aus fremden Dateien. Damit wäre die Ausgangslage
  (Nutzung, Lasten) für Dateien anderer Werkzeuge abgedeckt.
- **Aufbauten:** gbXML schreibt sie heute vollständig, IFC als `IfcMaterialLayerSet`. Ein Bauteil, das nur einen U-Wert trägt,
  bekommt nach 2.3 einen Aufbau, **bevor** exportiert wird. Damit entfällt die Ersatzschichtung im Regelfall. Der Exportdialog
  verweist auf Prüfgruppe 2, wenn noch Bauteile ohne Aufbau stehen.
- **`EPOS_*`-Sätze** sind die Rückfallebene für alles, was kein Standard trägt (Nacht- und Wochenendsollwerte, Übergabe,
  Vorheizzeit). Ihr Präfix bleibt.

## 3 Fragen an den Anwender

Jede Empfehlung ist am Leitziel gemessen: an der Zeit bis zum ersten Lauf und an der Zahl der Pflichteingaben.

1. **F1 Darstellung der Prüfung:** (a) eine Ampel je Prüfgruppe (fünf Gruppen) mit Zählung und Rechenstatus oben,
   Einzelheiten auf Klick; (b) eine Liste aller Befunde. **Empfehlung: (a)**. Fünf Zeilen mit Zählung sind in Sekunden gelesen,
   und Einzelheiten zeigt die Ansicht am Körper.
2. **F2 Übernahme der Vorschläge:** (a) „Alle Vorschläge übernehmen“ als Hauptknopf und zusätzlich je Gruppe, jede Übernahme
   mit Rücknahme; (b) nur je Gruppe; (c) nur einzeln. **Empfehlung: (a)**. Ein Klick führt vom Import zum rechenfähigen Modell,
   und die Rücknahme je Gruppe hält Details offen.
3. **F3 Quelle der Aufbauvorschläge:** (a) Rangfolge Projektaufbau gleicher Bauteilart → Katalogaufbau (BK, geeignet, Stoffgruppe
   und U-Nähe) → Typaufbau; (b) nur Katalog; (c) nur Typaufbau wie heute. **Empfehlung: (a)**. Ein Aufbau, den das Projekt schon
   trägt, ist der wahrscheinlichste und braucht keine Rückfrage.
4. **F4 Baujahrestypische Vorgaben:** (a) aus dem Bestand: Baualtersklasse und Bauart setzen U-Vorgabe (Katalogmedian) und
   Typaufbau, als Wahl in Prüfgruppe 2 und als „Vorgaben der Baualtersklasse übernehmen“ im Einzonenweg; (b) ein eigener Katalog
   baujahrestypischer Aufbauten mit Schemaschritt; (c) nur als Sitzungseingabe ohne Speicherung. **Empfehlung: (a)**. Sie kommt
   ohne Schemaschritt und ohne Normtabellen aus (E131) und braucht nur eine Eingabe (die Klasse).
5. **F5 Ferien und Saison:** (a) je Projekt (Ferien, Heizperiode), Zone nur bei Abweichung; (b) je Zone. **Empfehlung: (a)**.
   Einmal je Projekt eingeben statt je Zone, und der Generator setzt sie in jeden Zonenkalender.
6. **F6 Zone ohne eigenes Profil:** (a) Profil nach der Regel 2.4 bis zum EPOS-Muster, Status „rechnet mit Vorgaben“ (gelb);
   (b) Zone erbt die Gebäudekalender wie heute, Status gelb; (c) Status rot, bis ein Profil gewählt ist. **Empfehlung: (a)**,
   mit DIN-Profilen mit Werten (E135). Keine
   Pflichteingabe, und die Zone rechnet mit einer zur Nutzung passenden Woche statt mit der des Gebäudes.
7. **F7 Wiederimport:** (a) Ergänzungen des Anwenders bewahren (Zusammenführung über Quellkennung und Herkunft, Vorschau der
   Unterschiede); (b) ersetzen wie heute (GD-4), mit Warnung vor dem Verlust. **Empfehlung: (a)**. Sonst kostet jede
   Planänderung im CAD die ganze Nacharbeit erneut.
8. **F8 Export:** (a) Empfehlung je Zweck (IFC zurück ans CAD, gbXML an Simulationswerkzeuge) und gbXML um `Schedule` sowie IFC
   um die Standardsätze erweitern; (b) nur die Empfehlung, Formate wie heute; (c) nur IFC mit `EPOS_*`-Sätzen. **Empfehlung:
   (a)**. Die ergänzten Kalender gehen sonst bei jeder Übergabe an ein anderes Werkzeug verloren. Die Erweiterung liegt aber
   hinter dem schnellen Weg (MA-8). **Entschieden (a), E136, vorgezogen — siehe [Konzept Datenaustausch](2026-10-12_Konzept_Datenaustausch_Simulationsdaten.md).**

## 4 Wellenplan

| Welle | Inhalt | Aufwand | Leitziel | Abnahme |
|---|---|---|---|---|
| **MA-1** Schneller Weg | Kern `Modellpruefung` mit fünf Gruppen und Rechenstatus (ruft Bestand); Vorschläge aus dem Bestand (Ersatzaufbau, Profilregel 2.4 mit Raumtyp der Quelle und EPOS-Muster, Projektferien); „Alle Vorschläge übernehmen“ und je Gruppe, mit Rücknahme; Ampelleiste im Importdialog vor OK und im Reiter „Zonen“; Klick öffnet die Ansicht im passenden Farbmodus | 3–4,5 PT | **trägt das Leitziel**: Import → ein Klick → rechenfähig, 0 Pflichteingaben | Kern-Tests je Gruppe und Status (gelb/rot/grün) an Importproben; bunit Ampelleiste und Übernahme; Durchlauf an einer Probe ohne Handschritt bis OK; Referenzlauf byte-gleich; `fensterprobe.mjs` |
| **MA-2** Blatt „Nutzung und Kalender“ | alle Zonen auf einem Blatt, Mehrfachwahl „Profil zuweisen…“, Vorschlag für alle, Kurzbild je Größe | 2–3 PT | Details nachher: eine Handlung für n Zonen | bunit Mehrfachwahl; Kern-Test Zuweisung an n Zonen in einem Vorgang |
| **MA-3** Hülle geschlossen | Prüfung Σ A·n je Zone und Verankerung oben/unten (Mehrzonenkonzept 5.3), fehlendes Hüllbauteil aus der Körperfläche | 2–3 PT | rot wird gelb statt Handarbeit | Kern-Tests an Proben mit offener Hülle (Gegenprobe geschlossen); Importproben unverändert |
| **MA-4** Aufbauvorschläge vertieft | Rangfolge 2.3 mit Projektaufbau und Katalogaufbau (BK), Namensabgleich Schichtsatz → Typgruppe (Rest BA-2), Baualtersklasse für Stufe C in einem Schritt | 2,5–3,5 PT | bessere Vorgabe ohne Mehrklicks | Kern-Tests der Rangfolge; Test „nur U, Name trifft Mauerwerk“ → Katalogaufbau; nach BK-3b |
| **MA-5** Ansicht als Werkzeug | Sprung Ampel → Farbmodus mit Hervorhebung, Übernehmen/Verwerfen an der Legende, Modus „Nutzung“ | 2–3 PT | Kontrolle am Körper statt in Listen | `diagrammprobe`/Rasterprobe nach LIESMICH; bunit; nach AB4 |
| **MA-6** Wiederimport bewahrt | Zusammenführung in `MehrzonenuebernahmeCtrl` über Quellkennung und Herkunft, Vorschau der Unterschiede | 2–3 PT | Ergänzungen überleben die CAD-Änderung | Kern-Test: Hand-Profil, Hand-Aufbau und Hand-Zone überstehen den Wiederimport; Rückweg bei Fehler; ggf. Schemaschritt (vor dem Bau anmelden) |
| **MA-7** Einzonen-Vorgaben | „Vorgaben der Baualtersklasse übernehmen“ im Gebäudeeditor (U der sechs Gruppen, Bauweise), Profil der Gebäudeart | 1,5–2 PT | Einzonenmodell in einer Minute | Hülltests; Referenzlauf byte-gleich (bestehende Gebäude werden nie beschrieben) |
| **MA-8** Export ergänzter Daten — **geht in DA-Wellen des [Konzept Datenaustausch](2026-10-12_Konzept_Datenaustausch_Simulationsdaten.md) auf** (E136) | gbXML `Schedule`, IFC Standardsätze schreiben und lesen, Empfehlung im Exportdialog, Verweis auf Gruppe 2 | 3–4 PT | nach dem ersten Lauf, für die Weitergabe | Rundlaufproben gbXML und IFC (Kalender, Lasten); Schemaprobe gbXML 6.01; Verlustliste nachgeführt |
| Wiki/Logbuch | Seiten „Gebäudeimport“, „Gebäude“, „Nutzungsprofile“; Logbuch-Entwurf | 0,5 PT | — | Gegenlesemuster der Wurzel-`CLAUDE.md` ohne Treffer |

Summe rund 18,5–26,5 PT, davon MA-1 bis MA-3 (der schnelle Weg samt Nachbesserung) 7–10,5 PT.

**Reihenfolge nach dem Leitziel:** MA-1 sofort. Es hängt nur am Bestand und an GD-5 und berührt den Importdialog nach GD-4.
Danach MA-2 und MA-3 parallel (verschiedene Dateien), MA-7 jederzeit. MA-4 nach BK-3b, MA-5 nach AB4, MA-6 nach F7, MA-8 geht in
den DA-Wellen auf (E136). Jede Welle läuft in einem eigenen Zweig, baut den Kern-Filter und testet mit `--filter` auf die betroffenen Klassen. Das
Gate fährt die Orchestrierung nach dem Merge.

**Rechenweg, Basis, Schema:** Keine Welle ändert eine Leseregel des Rechenwegs. Die Prüfung ruft, die Vorschläge schreiben Daten
wie der Import heute. Bestehende Gebäude und Referenzprojekte werden nie beschrieben, der Referenzlauf bleibt byte-gleich zu
R52. Ein Schemaschritt ist höchstens in MA-6 möglich (Herkunft der Zonenkalender). Er wird vor dem Bau angemeldet.

## 5 Abgrenzung

- **Keine Normtabellen** (U-Werte, Profile, Aufbauten nach Baujahr aus Normen) im Repositorium oder in der Auslieferung. Normwerte
  kommen nur als Eingabe des Anwenders in dessen Datenbank (E131, NP7); ausgenommen die DIN/TS-18599-10-Profile, die nach
  E135 mit Werten aus der Lieferung des Anwenders ausgeliefert werden.
- **Kein Klimaimport** (E80). Die Klimaregion kommt aus dem Projekt.
- **Keine Geometriebearbeitung** in der Ansicht (keine Flächen zeichnen oder verschieben). Ergänzt werden nur Bauteile auf
  vorhandenen Körperflächen. Kein Geometriekernel (E3).
- **Kein IFC2x3- oder IFC4.3-Schreibpfad** für die Anreicherung (G7d bleibt IFC4, E3).
- **Keine Anlagentechnik im Export** (`AirLoop`, `HydronicLoop`, IFC-Verteilungselemente; Datenaustausch 13).
- **Keine Hersteller- und Produktdaten** im Papier, im Wiki oder in Proben. Werte und Dateinamen der Anwenderdateien unter
  `Quellen/` nur als Struktur und Zählung.
- **Kein stilles Überschreiben:** Kein Vorschlag wird ohne Handlung des Anwenders gespeichert. Auch „Alle Vorschläge übernehmen“
  ist eine Handlung mit Rücknahme. Bestehende Gebäude werden nur auf Zuruf erneuert (E95 Nr. 8).
- **Keine Änderung am Rechenweg** und keine neue Referenzbasis.
- **Nicht hier:** Lesen von `RoomVentilationType` und `RatedMinAirExchangeRate` als Lüftungskalender. Das wäre ein Nachzug am
  IFC-Leser, nach Entscheid des Anwenders, weil die Codes herstellereigen sind.
