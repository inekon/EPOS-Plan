# Protokoll: Stufe G3 — Bauteilkatalog, Bauteilweg und Zonen (24./25.09.2026)

**Auftrag** (vom Anwender am 24.09.2026 beauftragt): Stufe G3 der Gebäudesimulation — Bauteilkatalog
mit Schichtaufbau und Normnachweis der Reduktion, gebaut in sechs Wellen (A, B, W, D1, C, D2). Dazu
zwei Anwenderentscheide: **E39** (24.09.2026, Herstellerprodukte im Baustoffkatalog) und **E40**
(25.09.2026, „Hochrechnen" bei der Übernahme als eine Zone). Maßgeblich:
[Konzept](../../../aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md) 4.3, 4.7, 6.3 und N1.44 bis
N1.46 (Festlegungen der Umsetzung), [Mehrzonenkonzept](../../../aktuell/Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md)
3, 4 und 5.3, [Softwarearchitektur](../../../aktuell/Softwarearchitektur_Gebaeudesimulation_EPOS-Plan.md)
2.2, 3.1, 3.2 und 5 (W1), [Rechenschritte](../../../aktuell/Rechenschritte_Gebaeudesimulation_VDI6007_EPOS-Plan.md)
3 und 10.3, [Register](../../../aktuell/Offene_Entscheide_Gebaeudesimulation_EPOS-Plan.md) A1, A6 und
A14. Der Stand je Stufe steht in der
[Statusdatei](../../../aktuell/Status_Gebaeudesimulation_VDI6007.md), Zeile G3.

## 1 Die Wellen im Überblick

| Welle | Datum | Inhalt | Commits (Merge) |
|---|---|---|---|
| A, Teil 1 | 24.09.2026 | `Bauteilreduktion` im Kern | `7f501dd9` (`474014e7`) |
| A, Teil 2 | 24.09.2026 | `ErsatzparameterRC.AusBauteilweg`, Klimaweg für beliebige Flächen, lokaler Normnachweis, Tabellenskript | `aa45b8e6`, `6ba1e0c3` (`83c208c2`); Papiere `f5584ac3` |
| B | 24./25.09.2026 | Schemaschritte 132–134, Saat (65 + 67, E39), Registerpflege S-D, Controller, Kaskadenmessung A1 | `2b7d6774`, `9e286e89`, `f84c105d`, `d71b2efe`, `005bef86`, Umnummerierungen `6a2cb5b9`, `c1466489`, Wache `5a0a87b4`, Zusammenführungen `8d3767da`, `689fa45f` (`86378073`) |
| W | 25.09.2026 | Bauteilweg im Lauf, geneigte Fenster, echte Hülle, „Gebäude als eine Zone übernehmen" im Kern | `bf5e5346` (`d122c8e6`) |
| D1 | 25.09.2026 | Datenbankleser der Zonen für den Lauf | `b29d7946` (`a2254820`) |
| C | 25.09.2026 | Verwaltungen „Baustoffe" und „Bauteilaufbauten" (beide Schalen) | `04a59f1b`, `64ddb99d`, `e3676871`, `747d9ea5` (`d6dc2201`) |
| D2 | 25.09.2026 | Zonen und Bauteile im Gebäudedialog, Übernahme mit Hochrechnung (E40) | `bdf4464a`, `937c4e70`, `98152e11`, `60182f31`, `7f6a6a88`, dazu `726761cd` (`0c00f6ef`) |

Die Statuszeile ist mit `ca3dfb4e`, `12c647c1`, `729d2cff` und `b5a2e389` je Welle fortgeschrieben
worden; dieses Protokoll schließt die Stufe ab.

## 2 Welle A — der Bauteilweg im Kern

**Teil 1 — `Bauteilreduktion`** (`EPOS.Kern/Allgemein/Simulation/Gebaeude/`, ohne Datenbank):
Kettenmatrix je Schicht nach VDI 6007 Blatt 1 Gl. (1)–(11), als Abweichung von der Einheitsmatrix
geführt, ohne Division durch R oder k — numerisch stabil für R → 0 und C → 0; Ersatzgrößen R₁, R₂,
R₃, C₁, C₂ und C₁,korr nach (12)–(17); Bezugsperiode je Bauteil nach (10a)–(10d); komplexe
Parallelschaltung zum Raum nach (19)–(24) mit T_RA = 5 d; U-Wert, Wärmedurchlasswiderstand und
flächenbezogene Kapazität aus Schichten mit den Übergangswiderständen und ruhenden Luftschichten nach
DIN EN ISO 6946 — die eine Stelle im Kern, die auch der Import (G4) nutzt; Plausibilitätsband der
Stoffwerte (Mehrzonenkonzept 3.5), fünf benannte Fehlergründe, Meldungen `SIMENG_G3_*` in beiden
Sprachen. `BauteilreduktionTests`: neun Rechenproben.

**Teil 2 — der zweite Fabrikweg** `ErsatzparameterRC.AusBauteilweg`: Gruppen AW (Außenluft,
Erdreich, unbeheizt; einseitig mit C₁,korr) und IW (innerhalb der Zone; symmetrisch über den
vollständigen Aufbau), Fenster und Vorhangfassade nach (25)/(26) nach den Wänden, (27)–(28c) über den
vorhandenen Erbauer; trägt eine Gruppe keine Schichten, rechnet sie den Klassenweg aus den
Bauteilsummen; Herleitung je Bauteil (Bezugsperiode, U gerechnet und wirksam, Hinweis über 10 %).
`GebaeudeKlimaweg`: Einstrahlung auf beliebig geneigte und gedrehte Flächen, bitgleich zu den vier
Fassaden für N/O/S/W. `ErsatzparameterBauteilwegTests`: neun Proben, eine davon an den 15 Gebäuden der
Testdatenbank — **Grenzfall Bauteilweg = Klassenweg bis 4,7·10⁻¹⁶ relativ**.

**Normnachweis, lokal** (`BauteilreduktionNormTests`, in der CI schweigend):
`Referenzlaeufe/Skripte/vdi6007_bauteiltabellen.py` liest die Bauteiltabellen der zwölf Testbeispiele
aus der lokalen Normkopie und schreibt sie nach `Referenzlaeufe/Normzahlen/vdi6007/` (gitignoriert;
das Skript enthält keine Normzahl). Ergebnis: Die Reduktion trifft die Parameter der
Validierungsmodelle der AixLib in **12 von 12** Testbeispielen relativ **≤ 10⁻³**, und die Normfälle
mit diesen Parametern bringen dasselbe Bandergebnis wie mit denen der AixLib — **11 von 12 im Band**,
Fall 11 mit seiner benannten Grenze. Drei benannte Stellen, keine an den Formeln: FB1 in
Testbeispiel 1 (Innengruppe rund 3·10⁻⁴, Eingangsdaten der AixLib), ein Druckfehler der Richtlinie in
Testbeispiel 4 (Decke DE2; der Nachweis nimmt das Bauteil aus Testbeispiel 3) und Testbeispiel 10 (mit
den α-Werten der Tabelle höchstens 0,088 K außerhalb des Bands in einzelnen Stunden; die AixLib rechnet
den konvektiven Übergang der Außengruppe um 15,7 % kräftiger) — Rechenschritte 10.3 und 11, Zeilen 20
und 21. Ohne Aufrufer im Lauf, Referenzlauf 13/13 unverändert.

## 3 Welle B — Schemaschritte 132 bis 134, Saat, Controller

**Schemaschritte** (Quelle je Schritt die Schema-Klasse für Migration, Werkzeug `Testdatenbankschema`,
Testvorrichtung und Nachweis; alle Tabellen STRICT; die Nummer steht allein in
`BaustoffSchema.SCHRITT`, die übrigen zählen davon weiter):

| Schritt | Papiername | Inhalt |
|---|---|---|
| **132** | S-A (`BaustoffSchema`) | `Tab_Baustoff_STAMM` und `Tab_Baustoff`, spaltengleich (je elf Spalten, `ReadOnly` nur im Stamm, `ID_Projekt` nur in der Kopie), Spalte `Hersteller` (NULL = herstellerneutral); Saat von 65 herstellerneutralen Stoffen (DIN 4108-4:2020-11, DIN EN ISO 10456; Ids 1–65) und 67 Herstellerprodukten (Ids 1001–1067), `ReadOnly = 1`, Herkunft `VORGABE`, Quelle je Zeile; AUTOINCREMENT-Folge auf die Saatgrenze 10 000 |
| **133** | S-B (`BauteilaufbauSchema`) | `Tab_Bauteilaufbau(_STAMM)` und `Tab_Bauteilschicht(_STAMM)`; `ID_Baustoff` zeigt je Seite auf die eigene Ablage (W11), die Schicht ohne `ReadOnly` und `Herkunft` (L1), Index `(ID_Aufbau, Reihenfolge)` |
| **134** | S-C (`ZonenSchema`) | `Tab_Zone` (samt den Blöcken aus KU-S1 und AK-S1, alle nullbar) und `Tab_Bauteil` (neun Bauteilarten, vier Randbedingungen, ohne `ID_Nachbarzone` und `IstAussen`), Indizes `(ID_Gebaeude, Rang)` und `(ID_Zone, Rang)` |

**Nummern.** Die Schritte liefen in der Arbeit zuerst als 130 bis 132 (`2b7d6774`); origin vergab 129
(Wiederholperiode, E16) und 130 (Anschlusslängen, #493), also 131 bis 133 (`6a2cb5b9`, Zusammenführung
`8d3767da`); danach vergab origin 131 an die Typtage des Zapfprofils (Z4b), also **132 bis 134**
(`c1466489`, Zusammenführung `689fa45f`). Die Wache der Anschlusslängen (#493) prüft seither
„Zielstand ≥ Schritt" (`5a0a87b4`).

**Testdatenbank.** Die Fassung von origin mit Schemastand 131 (133 Tabellen, 67 805 184 Byte), auf die
`Werkzeuge/Testdatenbankschema` die Schritte 132 bis 134 angewendet hat: **141 Tabellen (alle STRICT),
215 Indizes, 25 Projekte, 132 Saatzeilen, 67 883 008 Byte** (LFS-SHA-256 `9d3c006a…`). Die
Auslieferungsvorlage zählt die STRICT-Tabellen mit und führt die Kindkataloge ohne `ReadOnly`
namentlich samt Proben. Nachgemessen am Stand nach D2 (Schemastand 139, LFS-SHA-256 `f700e81e…`):
`Tab_Baustoff_STAMM` 132 Zeilen, 65 ohne und 67 mit Hersteller, alle `ReadOnly = 1`; die Aufbau-,
Schicht-, Zonen- und Bauteiltabellen sind leer — es gibt keine Saat von Aufbauten, und kein
Referenzprojekt hat eine Zone.

**E39 — die Herstellersaat** (`9e286e89`): 67 Zeilen aus der Recherche vom 24.09.2026, Beleg als
Kommentar neben der Saatzeile, Beleg und Bemerkung nicht in der Datenbank; natürlicher Schlüssel
(Hersteller, Bezeichner). `WikiProduktdatenWacheTests` hält die Wiki-Quellen auch gegen die
Herstellerzeilen (nur Zeilen **mit** Hersteller — Normnamen bleiben erlaubt). Benannte Lücken: ein
großer Dämmstoffhersteller fehlt (Zugangsprüfung auf seiner Seite), Rohdichten teils aus
Umweltproduktdeklarationen.

**Fremdschlüssel der Projektkopien** (`005bef86`): `ProjektFremdschluesselTests` verlangt für jede
Tabelle mit Projektspalte den Fremdschlüssel der Hausregel seit Schemaschritt 96; `Tab_Baustoff` und
`Tab_Bauteilaufbau` tragen ihn von Anfang an (`ON DELETE CASCADE ON UPDATE CASCADE`, Vorbilder
`Tab_Wechselrichter` und `Tab_PV`). Softwarearchitektur 2.2 („ohne Fremdschlüssel") beschrieb den
Stand davor und ist nachgezogen. Ein gelöschtes Projekt nimmt seine Baustoffe, Aufbauten und
Schichten mit; das Reduzierskript zählt 21 Kaskaden.

**Registerpflege S-D** (`f84c105d`, ohne DDL): `KatalogRegistry` `BAUSTOFF` (Hersteller im
natürlichen Schlüssel, Verwendungsprüfung über die Schichten) und `BAUTEILAUFBAU` (Datenblock Schichten
nach Reihenfolge); Katalogfilterprofile; `ProjektDuplizierenCtrl` — `FK_MAP` um `ID_Aufbau` und
`ID_Baustoff`, `ID_Zone` des Bauteils über `FK_OVERRIDE` (der Name steht schon für `Tab_TwwZone`),
`KINDER` dreistufig Gebäude → Zone → Bauteil und Aufbau → Schicht; `Reduziere-Testdatenbank.sql`
samt Probe.

**Controller** (`d71b2efe`): `BaustoffCtrl` (Katalog und Projektkopie, Schutz der
Auslieferungssätze, Verwendungsprüfung, Duplizieren, Schloss, Kopie aus dem Stamm NULL-erhaltend),
`BauteilaufbauCtrl` (Aufbau samt Schichten als Aggregat in einer Transaktion, Reihenfolge lückenlos,
Kopie aus dem Stamm mit Id-Abbildung `ID_Baustoff`), `GebaeudeZonenCtrl` (Lesen je Gebäude und je
Projekt mit je einer Abfrage für Zonen und Bauteile, Speichern als Aggregat mit Abgleich über die Ids
nach A6, Prüfung mit benannter Azimutpflicht). **Kaskadenmessung A1:** Kein gewöhnlicher Speicherweg
löscht `Tab_Gebaeude` — die Gebäudeliste wird abgeglichen; Zonen fallen nur beim Entfernen oder
Tauschen des Gebäudes und beim Löschen des Projekts. **Eine Rettung ist nicht nötig**; die Probe hält
Löschen (Zonen weg) und Speichern (Zonen unverändert) fest.

Tests: `GebaeudeG3SchemaTests` (14 Testmethoden), `GebaeudeG3CtrlTests` (18). Ergebnisneutral —
kein Rechenweg liest die Tabellen; Referenzlauf 13/13 byte-gleich.

## 4 Welle W — der Bauteilweg im Lauf

Ein Gebäude mit **genau einer Zone** rechnet über seine Bauteile (Datenlage, A14), ohne Zone
bitgleich den Klassenweg, zwei Zonen benannt abgelehnt (`MehrereZonen`, G6). `GebaeudeModellEingang`
nimmt die Parameter aus `AusBauteilweg`; solare Gewinne **je Fensterbauteil mit Azimut und Neigung**
(geneigte und waagerechte Fenster), θ_eq je Bauteil U·A-gewichtet nach (41), Erdreich und Kellertemperatur;
der unbeheizte Nachbarraum rechnet mit der Kellertemperatur des Gebäudes bis zum Entscheid M3 des
Registers und G6b; die Auslegung der Anlagenkopplung (AK1) kommt aus den Bauteilen. **Echte Hülle
statt Nachmultiplikation** (Konzept 4.7): Mit Zone gilt der Faktor 1, eine Verbrauchs- oder
Flächenangabe steht einmal benannt im Protokoll; ein Altweg-Gebäude mit Zone bekommt einen Hinweis.
Kernfunktion `GebaeudeZonenuebernahme.AlsEineZone` (Wand und Sonstiges geviertelt, Dach, Bodenplatte,
Fenster, ohne Schichten).

Nachweise (`GebaeudeBauteilwegLaufTests`, 19 Testmethoden): **Grenzfall im Jahreslauf** —
übernommene Zone = Klassenweg an fünf Gebäuden der Testdatenbank mit je acht Varianten, Jahresheizwärme
bis **1,3·10⁻¹⁴ relativ**, synthetisch neun Fälle; ein **Dachfenster Süd 45°** bekommt 1 851 statt
1 476 kWh/(m²a); Schichten im Lauf, Auskunft = Lauf, zwei Zonen benannt abgelehnt.

## 5 Welle D1 — der Datenbankleser

`GebaeudeZonenanschluss` liest je Projekt selbst: `GebaeudeZonenCtrl.LesenJeProjekt` und, nur wenn ein
Bauteil einen Aufbau trägt, `BauteilaufbauCtrl.LesenJeProjekt` — **höchstens vier Abfragen, nie je Zone
oder Bauteil**, rund 0,1 % Laufzeit. Ein älterer Schemastand ohne `Tab_Zone` heißt „keine Zonen"
(Schemaprobe mit Gedächtnis je Datenbankpfad nach dem Muster `AnlageStrangCtrl`, verworfen von
Schritt S-C). `GebaeudeZonenabbildung` bildet Zeile ↔ Kern an einer Stelle ab; die Regel
**„leere Randbedingung an Innenwand und Decke = innerhalb der Zone, sonst Außenluft"** steht allein in
`RandAusZeile`. Eine unlesbare Zone lässt das Lesen heil und wirft ihren Fehler im Lauf mit dem
Gebäude davor. Nachweise über echte Zeilen: Rundlauf Zeile → Kern → Zeile verlustfrei, Auskunft = Lauf
über die Datenbank, das Projektduplikat rechnet bitgleich, feste Abfragezahl, ohne `Tab_Zone`
(`GebaeudeZonenabbildungTests`, sieben Testmethoden, dazu Fälle in den Lauftests).

## 6 Welle C — die Verwaltungen „Baustoffe" und „Bauteilaufbauten"

**Kern** (`04a59f1b`): `BauteilaufbauCtrl.Kennwerte` — der Summenfuß eines Aufbaus über den Bauteilweg
(R je Schicht, R, U mit den Übergängen aus der Neigung der Bauteilart an Außenluft, Σ ρ·c·d, T_BT
nach (10a)–(10d) samt C₁ je m²), nie eine Ausnahme, die erste Lücke als Grund;
`BauteilaufbauCtrl.EingabePruefen` nach Mehrzonenkonzept 5.3; Duplizieren samt Schichten; Filter
„nur herstellerneutral"; KI-Masken beider Kataloge. **Oberfläche** (`64ddb99d`): `BaustoffKatalogDialog`
im Gerüst der Verwaltungen (sieben Spalten, Filter Gruppe/Hersteller/„nur herstellerneutral",
Auslieferungssätze mit Schloss, Löschen mit Sperrgrund) und `BauteilaufbauDialog` mit Schichtenraster
(Baustoff per Suchauswahl mit Wertekopie, Dicke in mm, λ, ρ, c_p überschreibbar, ruhende Luftschicht,
Pfeile, Summenfuß aus der Hülle), ein Arbeitsstand, ein Speicherweg; Hüllen plattformfrei in
`EPOS.UI.Daten`. **Menü und Navigation** (`e3676871`): beide Kataloge gemeinsam unter
**Administration › Gebäude** nach „Gebäudetypen" (63 Punkte, 49 Handlungen), freie Ansichten der
Wurzel auf beiden Schalen; Hilfezuordnung auf die neue Wiki-Seite. **Wiki** (`747d9ea5`): Repo-Quelle
„Programm Dokumentation - Baustoffe und Bauteilaufbauten" (Anker `baustoffe`, `bauteilaufbauten`,
`schichten`, `summen`, `speichern`; neutrale Beispiele), Zeile im Konzept Hilfesystem, Glossar.

Tests: `BauteilaufbauVerwaltungTests` (15 Testmethoden), `BauteilkatalogHuellenTests` (6),
`BaustoffKatalogDialogTests` (16), `BauteilaufbauDialogTests` (17); KI-Katalog 83 Masken. Referenzlauf
13/13 unverändert (kein Referenzprojekt hat eine Zone).

## 7 Welle D2 — Zonen und Bauteile im Gebäudedialog, Übernahme mit Hochrechnung (E40)

**Kern** (`bdf4464a`, `937c4e70`): `GebaeudeZonensatz` trägt die Nutzfläche der Zone (NULL = die des
Gebäudes), die Abbildung liest und schreibt `Tab_Zone.Nutzflaeche`; **Flächenschlüssel** im
Eingangsbauer — Luftvolumen, Speichermasse der Bauweise, innere Gewinne und f_IW·A_f folgen der
Zonenfläche, der Anteil 1 rechnet bitgleich; Leistungsgrenzen bleiben (benannt), die übrigen
Zonenspalten liest G3 nicht. `AlsEineZone(g, faktor)` rechnet Flächen und ψ·L mit dem Faktor hoch;
`GebaeudeBedarfCtrl.Hochrechnungsfaktor` fragt die Fassade; `GebaeudeZonenCtrl.Uebernahme` bildet
den Vorschlag, `BauteilPruefen` hält die Regeln 5.3 des Bauteildialogs genau einmal;
`Gebaeudehuellbilanz.Zonenzeilen` die Summenregel der Hülle aus den Bauteilen.

**Oberfläche** (`98152e11`): Knopf **„Hülle und Zonen…"** im Gebäudedialog öffnet den Gebäudeeditor in
der **Betriebsart Projekt** (weich gesperrt ohne Projektkopie); Herleitungszeile „Rechenweg der
Hülle" in beiden Stellungen; Knopf „Gebäude als eine Zone übernehmen" — frei nur im Projekt ohne Zone
auf dem VDI-Weg, sonst weich gesperrt mit Grund — mit Rückfrage (Faktor, Nutzfläche alt und neu,
Angabe, Leistungsgrenzen); mit Zone Hülle und Leitwerte als abgeleitete Anzeige; Reiter „Zonen" (leer
bedienbar, Öffnen, Entfernen mit Rückfrage); `ZonenDialog` und `BauteilDialog` als Überlagerungen,
Aufbau aus Projekt oder Katalog (ein Katalogaufbau wird erst im OK-Weg kopiert); OK-Weg in drei
benannten Schritten (Projektkopie, Katalogaufbauten, Zonen); „Speichern unter" mit Zone fragt vorher;
Skalierungsangabe mit Zone: Herleitungszeile, Angabe weich gesperrt. KI-Masken Zone und Bauteil,
Hilfezuordnung, Texte beider Sprachen.

**Nachweis der Hochrechnung** (`GebaeudeHochrechnungTests`, 15 Testmethoden): über die Datenbank an
**1007 (Faktor 4,59), 1008, 1018 und 1017** ohne Kühlgrenze und Verbrauchsangabe — das Ergebnis vor
und nach der Übernahme gleich, relativ **≤ 10⁻⁹**; Faktor 1, Flächenschlüssel, Prüfregeln, das
Projektduplikat hängt `ID_Zone` um. Oberflächentests (`60182f31`): `ZonenDialogTests` (14),
`BauteilDialogTests` (13 Testmethoden, 15 Fälle), `GebaeudeZonenTests` (22); KI-Dialogkatalog
85 Masken, Maskenabdeckungswache um die Eingabestellen.

**Wiki** (`7f6a6a88`): „Gebäude" mit dem Abschnitt „Gebäude im Projekt: Hülle und Zonen" (Anker
`huelle-und-zonen`, `zone-uebernehmen`, `zonen`, `bauteile`, `zone-katalog`), „Gebäudemodell VDI 6007"
mit dem Abschnitt „Bauteilweg" (Anker `bauteilweg`), Glossar mit sieben Begriffen; Gegenlese-Muster
ohne Treffer, nicht hochgeladen.

## 8 Koordination mit den Sitzungen AK1 und G4

- **Schemanummern.** Nach G3 (132–134) vergab AK1 Welle 4 die Schritte **135 bis 137** (Kälteseite der
  Kopplung); 137 hängt drei Spalten der Kühlübergabe an `Tab_Zone` — `GebaeudeZonenCtrl` liest und
  schreibt sie NULL-erhaltend (AK1 W4-6). Nach dem Zusammenführen fragte jedes Lesen der Zonen vier
  Schemaabfragen; die Spaltenprobe ist seither je Datenbankpfad gemerkt und wird von Schritt 137
  verworfen (`726761cd`). G4c Welle 3 vergab **138** (S-F, in der Arbeit 135), G4a Welle 3 **139**
  (`Baujahr`).
- **Importzuordnung.** `Tab_Importzuordnung` (S-F) verweist mit Kaskade auf Zone, Bauteil, Aufbau und
  Baustoff; die Probe „ohne `Tab_Zone`" räumt deshalb zuerst die Importzuordnung ab (`726761cd`).
- **Eine Stelle für U aus Schichten.** Der gbXML-Import (G4c Welle 1) rechnet U-Werte aus Schichten
  über `Bauteilreduktion`; der Import benutzt die eine Bauteilart der Stufe G3 (`80fed675`).
- **Kaskadenrettung entfällt.** Die Messung A1 dieser Stufe hat G4 die geplante Rettung in
  `WizardCtrl` erspart (Softwarearchitektur, Nachzug G4).
- **Register-Zählung.** E39 und E40 berühren keinen Registerpunkt; das Register zählt weiter 8 offene
  Punkte (M3, M5–M8, M11–M13).

## 9 Abnahme

- **Bau und Tests** auf dem Stand nach dem Merge der Welle D2 (Nachzug der Papiere, 25.09.2026):
  Kern-Filter 0 Fehler; Test-Gate grün — Kern 6 748 (ein Fall übersprungen), UI 6 247, KiKern 549,
  SpeicherEngine 386, SpeicherPlanung 27 (einer übersprungen). Die G3-Testklassen zählen zusammen
  15 Dateien mit 196 Testmethoden (Abschnitte 2 bis 7).
- **Normnachweis** lokal: 12 von 12 Testbeispielen relativ ≤ 10⁻³, 11 von 12 im Band; benannt FB1,
  Testbeispiel 4, Testbeispiel 10 (Abschnitt 2).
- **Grenzfälle:** Bauteilweg = Klassenweg an 15 Gebäuden bis 4,7·10⁻¹⁶, im Jahreslauf bis 1,3·10⁻¹⁴
  relativ; Hochrechnung bei der Übernahme ≤ 10⁻⁹ relativ.
- **Referenzlauf:** in jeder Welle 13/13 unverändert gegen `2026-09-24_R14_Kaelteerzeuger` — kein
  Referenzprojekt hat eine Zone, die Schritte 132–134 sind ergebnisneutral
  ([Referenzläufe](../../../../Referenzlaeufe/LIESMICH.md), Nachtrag Schemastände 132 bis 134).
  Nachgerechnet auf dem Stand nach D2 (25.09.2026): die fünf CI-Projekte 1030, 1007, 1017, 1045 und
  1046 **5/5 PASS** gegen R14 (1 805 429 Werte), 160/160 CSV byte-gleich, außer `protokoll.txt`.
- **Wachen:** `DokumentationLinkWacheTests`, `RepositoryOrdnungWacheTests`, `WikiProduktdatenWacheTests`
  grün nach dem Nachzug der Papiere.

## 10 Offen

- **Katalogseite auf `Katalogliste`** (Softwarearchitektur 3.2, Regel 5): Die Rasterprobe braucht
  Playwright und Chromium, die auf dem Arbeitsrechner fehlen; den Download gibt der Anwender frei.
- **Wiki-Upload** der Seiten „Baustoffe und Bauteilaufbauten" (neu), „Gebäude" und „Gebäudemodell
  VDI 6007" mit dem Sammel-Upload der Version 1.2.0.4, Logbuch-Sätze entworfen
  ([Update-Papier](../../../aktuell/Wiki_Update_2026-09-26.md)).
- **Folgestufen:** G4b (Bauteile aus IFC, nach G3 und nachdem G4a im Feld war); G6 — mehrere Zonen,
  unbeheizte Zonen (`IstBeheizt = 0` rechnet in G3 wie beheizt), die Temperaturregel des unbeheizten
  Nachbarraums (Register M3), die übrigen Zonenspalten, der konvektive Übergang einer Trennfläche in der
  Außengruppe (Mehrzonenkonzept 2.2, Punkt 4).
