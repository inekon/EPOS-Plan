# Entwurf ZZ — Zonenzuordnung beim IFC-Import: Befund, Abgleich mit dem DIN/TS-18599-10-Katalog, Zuordnungsdialog

**Stand:** 17.10.2026 — Analyse und Entwurf zur Entscheidung, nichts gebaut (Prototyp nur zum Messen, nicht
committet). **Anlass:** Auftrag des Anwenders vom 17.10.2026:

> „beim einlesen von einer ifc Datei werden die Zonen nicht übernommen. Prüfe an den beispielen die Zonen. Es sollen
> die Zonen übernommen werden können, wenn Sie zuzuordnen sind z.B. Anhand der Bezeichnung. So sollen die DIN 8599
> Zonen im Katalog den eingelesenen Zonen zugeordnet werden, falls 1. die Zonen keine Beschreibung enthalten und
> 2. Die Zuordnung möglich ist. Fall nicht Eindeutig soll eine Einfach Zuordnung der Zonen der eingelesen Datei zu
> vorhandenen Zonen möglich sein (besser Strukturierte Auswahl vorhandener Zonen und einfache/übersichtliche
> Zuordnung zum Projekt - auch mit Vorschlägen).“

Gemeint sind die Nutzungsprofile der Kategorie „DIN/TS 18599-10“ des Nutzungsprofil-Katalogs
(`Tab_Raumnutzungsprofil`, Welle DA-6, Schemaschritt 231 `RaumnutzungDinWerteSchema`: 43 nummerierte Profile und
zwei Wohnprofile, mit Werten).

**Bezugspapiere:** [Konzept Nutzungsprofile](../Konzept_Nutzungsprofile_EPOS-Plan.md) (4.4 Einbau, 5.4 Zuordnung,
6.2 Zugänge), [Konzept DA — Datenaustausch der Simulationsdaten](2026-10-12_Konzept_Datenaustausch_Simulationsdaten.md)
(4.3 Prüfung und Zuordnung des DIN-Profils je Zone, E135; Welle DA-5 noch offen),
[Mehrzonenkonzept](../Konzept_Mehrzonenmodell_IFC_EPOS-Plan.md) (6.1 Zonierungsregeln, 6.4 Zuordnungsdialog),
[Entwurf GD — Gebäudedialog Mehrzonen](2026-10-11_Entwurf_Gebaeudedialog_Mehrzonen.md),
[Entwurf MA — Geführter Modellaufbau](2026-10-12_Entwurf_Gefuehrter_Modellaufbau.md). Statuszeilen N7 (#1153) und
N8 (#1161) zu den Zonenkalendern.


## 0. Das Ergebnis in sechs Punkten

1. **Die Beispieldateien tragen keine Zonen.** Keine der sieben `Quellen/*.ifc` enthält `IfcZone` oder
   `IfcSpatialZone`, keine Raumgrenzen (`IfcRelSpaceBoundary`), keine Klassifikation. Die zwei `IfcRelAssignsToGroup`
   hängen ein `IfcSystem` („FieldNet“) mit `IfcEnergyConversionDevice`-Gliedern — Haustechnik, keine Zonen. Die
   Zonenregel Z1 („nach den Zonen der Datei“) hat deshalb nichts zu lesen; Z2 und Z3 brauchen Raumgrenzen und stehen
   auch nicht zur Wahl. Wählbar sind nur Z4 (je Geschoss), Z6 (Raumtemperatur und Nutzung) und Z5 (eine Zone);
   Vorgabe ist Z4 — Zonen heißen „EG“, „OG1“, nicht nach der Nutzung.
2. **Was die Dateien tragen, ist die Raumart je Raum:** `HSETU_RaumAllgemein.RoomType` mit Aufzählungswerten
   (`mrtOffice`, `mrtWC`, `mrtHall`, …) an **jedem** Raum (353 von 353), dazu `HeatingType` und `InsideTemperature`,
   der Raumname („Büroraum 002“) und die Netto-Grundfläche (`HSETU_RaumQuantities.Area`). Beschreibung, `LongName`
   und eine DIN-Nummer fehlen überall. Der Leser liest `RoomType` schon als `AbbildRaum.Raumtyp`.
3. **Kein Weg führt heute auf ein DIN/TS-18599-10-Profil.** Ein Profil bekommt eine Zone nur unter Z6 — über die
   Nutzungsklasse des Raumtyps und die Zuordnung `IFC_KLASSE`, die auf **EPOS-Muster** zeigt (NP-F13), nie auf ein
   DIN-Profil; `Sanitaer` und `Sonstige` ergeben „keine“. Unter Z4 und Z5 bleibt die Zone ohne Nutzung — oder sie
   bekommt über den flächengrößten Raum (ein Flur, ein Treppenraum) das Muster „Verkehr“, weil die
   gbXML-Raumtyptafel auf den IFC-Raumtyp angewandt wird (Befund B4).
4. **Der Abgleich ist tragfähig.** Ein Prototyp (Raumart → Kandidaten, Raumname schärft, Fläche entscheidet Büro-
   und Lagergröße, Gebäudeart entscheidet Wohnen) ordnet **344 von 353 Räumen eindeutig** zu (97 %), 9 mehrdeutig
   (alle Werkstatt/Produktion → Hallen 22/23/24), 0 ohne Kandidaten. Je Datei 90 bis 100 % eindeutig.
5. **Vorschlag:** eine neue Zonenregel **Z7 „nach Nutzungsprofil“** (Räume gleichen Profils und gleicher Beheizung
   bilden eine Zone, ohne Raumgrenzen wählbar), eine **Profilzuordnung je Raum** mit fester Rangfolge (eigene
   Angabe der Datei → Raumart → Name → Fläche → Gebäudeart), automatisch nur eindeutig und nur ohne eigene
   Nutzungsangabe, sonst Vorschlag; eine **Überlagerung „Nutzungsprofile zuordnen“** im Importdialog mit den
   Katalogprofilen nach Gruppen der Norm links und den gleich benannten Räumen gebündelt rechts (Sammelzuordnung,
   Begründung je Vorschlag, „alle Vorschläge übernehmen“).
6. **Ein Schemaschritt (244 empfohlen, nicht angemeldet):** Saat der Zuordnungszeilen `HOTTCAD_RAUMTYP` → DIN-Profil
   (die Art existiert, Zeilen gibt es keine) und Umstellen der ausgelieferten `DIN_NUMMER`-Zeilen vom EPOS-Muster auf
   das DIN-Profil gleicher Nummer, weil die DIN-Profile seit DA-6 Werte tragen (NP-F13 ist damit überholt). Keine
   DDL-Änderung, kein Eingriff in Rechenweg oder Basis.


## 1. Befund an den Beispieldateien

### 1.1 Zählungen je Datei

Gelesen mit einem Textzähler über die STEP-Dateien und nachgestellt mit dem Importweg des Kerns
(`GebaeudeImportAblauf.Lesen` → `GebaeudeZonierung.Waehlbar` → `Zonenplan.Vorschlag`; Diagnose im Testprojekt, nicht
committet):

| Datei | Schema | Räume | Geschosse | `IfcZone` / `IfcSpatialZone` | Raumgrenzen | `RoomType` belegt | `LongName` / Beschreibung | Regeln wählbar (Vorgabe) |
|---|---|---:|---:|---:|---:|---:|---:|---|
| `MFH-Klein-unsaniert-1964.ifc` | IFC2X3 | 30 | 5 | 0 | 0 | 30 | 0 | Z4, Z6, Z5 (Z4) |
| `MFH_mittel_1984.ifc` | IFC4 | 29 | 4 | 0 | 0 | 29 | 0 | Z4, Z6, Z5 (Z4) |
| `Produktion_groß_mit_Verwaltung_EG55-2026.ifc` | IFC2X3 | 49 | 2 | 0 | 0 | 49 | 0 | Z4, Z6, Z5 (Z4) |
| `Sportheim_1970_unsaniert.ifc` | IFC4 | 56 | 3 | 0 | 0 | 56 | 0 | Z4, Z6, Z5 (Z4) |
| `Sportheim_Bestand-1997_test_ifc4.ifc` | IFC2X3 | 56 | 3 | 0 | 0 | 56 | 0 | Z4, Z6, Z5 (Z4) |
| `Verwaltung_mit_Montage-2969_vollsaniert_2014.ifc` | IFC2X3 | 113 | 7 | 0 | 0 | 113 | 0 | Z4, Z6, Z5 (Z4) |
| `WG-EH55_Poroton-GModG-2026.ifc` | IFC4 | 20 | 3 | 0 | 0 | 20 | 0 | Z4, Z6, Z5 (Z4) |

Alle Räume tragen `ObjectType` = `TModelRoom` (ein Wert, keine Nutzungsinformation). `IfcRelAssignsToGroup` steht in
zwei Dateien (Produktion, Verwaltung) je einmal und hängt ein `IfcSystem` namens „FieldNet“ mit 1 107 bzw. 140
`IfcEnergyConversionDevice` — kein Raum darunter. Die beiden IFC4-Dateien tragen stattdessen ein
`IfcDistributionSystem` ohne Zuweisung.

### 1.2 Wo Nutzungsangaben stehen

Je Raum dieselben Eigenschaftssätze (Namen aus den Dateien, Unicode aufgelöst):

| Satz | Eigenschaften | Nutzen für die Zonierung |
|---|---|---|
| `HSETU_RaumAllgemein` | `RoomType` (Aufzählung, Präfix `mrt`), `HeatingType` (`bhtHeated`, `bhtSeparatelyHeated`, `bhtUnHeated`), `InsideTemperature (°C)`, `RatedMinAirExchangeRate (1/h)`, `RoomVentilationType`, `LevelOfBasicFloor (m)` | **die Raumart** — die einzige Nutzungsangabe; Beheizung und Sollwert werden heute schon gelesen |
| `HSETU_RaumQuantities` | `Area` (Netto-Grundfläche), `UsefulArea`, `Volume`, `ClearHeight`, `Height`, `Perimeter`, `LivingArea` | die Fläche je Raum — heute als `FlaecheM2` gelesen |
| `HSETU_RaumHeizung`, `…Radiator`, `…Fußboden`, `…Wand` | Heizlast, Anteile je Übergabe | keine Nutzung |
| `HSETU_RaumMappingNachDIN12831` | Auslegungslasten (Transmission, Lüftung, Zusatz), `Info` (Text) | keine Nutzung — Heizlast nach DIN EN 12831, kein Raumtyp der Norm |
| `HSETU_RaumMappingNachDIN1946-6` | Luftvolumenströme, `CombustionAirSupply.AirSupplyType` | keine Nutzung |
| `HSETU_BauteilAllgemein`, `HSETU_EcoCad`, `HSETU_RaumWandflächen`, `HSETU_RaumDeckenflächen` | Kennungen, Zeitstempel, Rahmenanteil, Flächenregeln | keine Nutzung |

Die Werte von `RoomType` über alle sieben Dateien (28 verschiedene Codes): `Hall` 56, `Office` 53, `Store` 33,
`WC` 35, `Stairway` 17, `Living` 27, `AdjoiningRoom` 21, `Connection` 9, `Workshop` 9, `Conference` 7, `Bath` 9,
`Sleeping` 6, `Kitchen` 5, `Fitness` 5, `Locker` 4, `Eating` 4, `Restaurant` 3, `Basement` 3, `HallWay` 2,
`Child` 2, `StorageRoom` 2, je 1 `Roof`, `Guests`, `Examination`, `Ante`, `Shower`, `Sauna`, `CentralHeating`.
Die Raumnamen sind deutsche Bezeichnungen mit angehängter Nummer („Büroraum 002“, „WC-Raum 003“, „Flur 4“); je Datei
7 bis 32 Namensstämme, bei 113 Räumen 17 Stämme — die Verwaltung besteht zu 43 Räumen aus „Büroraum“.

### 1.3 Die Anwenderdatei

`Quellen/Sportheim_1970_unsaniert.sqproj` liegt unversioniert neben der IFC (`.gitignore`). Sie trägt die
DIN-V-18599-Zonen des Planers (Typ 5) und Simulationszonen (Typ 6); der Weg „IFC + Projektdatei“ bietet dafür das
Wahlfeld „DIN-V-18599-Zonen | Simulationszonen“ (E87, F1) und zoniert nach X1 mit den Profilen der Datei
(`SqprojZonen`, `SqprojRaumnutzung`, Rangstufe 1 des Konzepts DA 4.3). **Mit Projektdatei kommen die Zonen also an;
ohne sie — der Auftrag — nicht.** Der Entwurf lässt diesen Weg unverändert und ergänzt den Weg „nur IFC“.


## 2. Ursache (a)

Der Satz „die Zonen werden nicht übernommen“ hat drei Ursachen, jede mit Beleg:

**B1 — Die Datei trägt keine Zonen, die Regel Z1 liest nur Zonen.** `IfcAbbildBauer.Zonen()`
(`EPOS.Kern/Allgemein/Import/Ifc/IfcAbbildBauer.cs:1736–1812`) liest `IfcSpatialZone` mit `THERMAL`, sonst `IfcZone`
über `IfcRelAssignsToGroup` (Zeile 1777: `if (!(rel.RelatingGroup is IIfcZone z)) continue;` — das `IfcSystem` der
Beispieldateien fällt hier heraus). Ohne Treffer bleibt `ZahlZonen` 0 und kein Raum trägt eine `ZonenKennung`.
`GebaeudeZonierung.Waehlbar` (`EPOS.Kern/Allgemein/Import/Gebaeude/GebaeudeZonierung.cs:430–457`) bietet Z1 nur mit
Raumgrenzen **und** Zonenkennung (Zeile 439), Z2 und Z3 nur mit Raumgrenzen (Zeilen 440–446); die Dateien haben keine
(`ZahlGrenzen` = 0 in allen sieben). Übrig bleiben Z4, Z6, Z5 mit Vorgabe Z4 (Zeilen 447–456). Nachgestellt an allen
sieben Dateien (Diagnose: `Regeln=[Z4,Z6,Z5] Vorgabe=Z4 grenzen=False`).

**B2 — Die Vorgabe Z4 bildet Geschosszonen ohne Nutzung.** `Zonenplan.NutzungMitQuelle`
(`EPOS.Kern/Allgemein/Import/Gebaeude/Zonenplan.cs:415–432`) gibt nur unter Z6 ein Profil zurück
(`!string.Equals(regel, IfcImportProfil.ZONENREGEL_Z6, …) … return (null, null)`); `Bedarf(regel)` (Zeile 435) liest
die Zuordnung nur für Z6. Unter Z4 entstehen Zonen „EG“, „OG1“, „Keller“ ohne Nutzung (Diagnose Produktion:
`REGEL Z4: Zonen=3 mitProfil=0`; MFH-Klein: `Zonen=7 mitProfil=0`).

**B3 — Auch unter Z6 entsteht kein DIN-Profil.** `GebaeudeZonierung.Nutzungsklasse`
(`GebaeudeZonierung.cs:1304–1315`) bildet den Raumtyp über `RAUMTYP_KLASSE` (Zeilen 1266–1278) auf **elf
Nutzungsklassen** ab; `Raumnutzungsvorbelegung.Herleitung` (`Raumnutzungsvorbelegung.cs:195–206`) sucht zuerst eine
Zeile `HOTTCAD_RAUMTYP` — **die Testdatenbank trägt keine** (Zuordnung: 18 `DIN_NUMMER`, 9 `IFC_KLASSE`, 0
`HOTTCAD_RAUMTYP`) — und fällt auf `IFC_KLASSE` zurück. Alle 27 ausgelieferten Zuordnungszeilen zeigen auf
**EPOS-Muster** (Konzept Nutzungsprofile 5.4, NP-F13: „Die DIN-Nummern zeigen ausgeliefert auf EPOS-Muster, nicht auf
die leeren DIN-Profile“); `Sanitaer` und `Sonstige` haben keine Zeile und ergeben „keine“. Z6 gruppiert zudem nach der
gerundeten Raumsolltemperatur und nennt das Profil der flächengrößten Klasse der Gruppe (Zeilen 420–431): Im
Sportheim wird „20 °C – Gastronomie, Büro, Küche“ (26 Räume) zu **Gastronomie**, in der Verwaltung „20 °C – Büro,
Sanitär, Gastronomie“ (63 Räume) zu **Büro**, im Produktionsbau „20 °C – Sonstige, Büro, Gastronomie“ (23 Räume) zu
**keine** (Sonstige ist flächengrößt). Nachgestellt an allen sieben Dateien.

**B4 — Nebenbefund: Z4-Zonen bekommen „Verkehr“ über die gbXML-Raumtyptafel.** `Zonenplan.DinNummerAnwenden`
(`Zonenplan.cs:336–347`) nimmt die DIN-Nummer des flächengrößten Raums (`DateiDinNummer`, Zeilen 349–353:
`r.Konditionierung?.DinNummer ?? GbxmlRaumtyptafel.DinNummer(x.R.Raumtyp)`). Die gbXML-Tafel
(`EPOS.Kern/Allgemein/Import/Gbxml/GbxmlRaumtyptafel.cs:51`, `["Stairway"] = 19`) trifft den IFC-Raumtyp `Stairway`:
Ein Geschoss, dessen flächengrößter Raum der Treppenraum oder Flur ist, bekommt **DIN 19 Verkehrsflächen → Muster
Verkehr** (Diagnose Verwaltung: sechs Geschosszonen `Profil=Verkehr | Quelle=DIN_NUMMER:19`; Sportheim: drei). Das
ist fachlich falsch (die Geschosse sind Büro- bzw. Gaststättengeschosse) und zeigt, dass eine Zone mit gemischter
Nutzung nicht über **einen** Raum ihr Profil bekommen darf.

**Zusammengefasst:** Ohne `IfcZone` fehlt die Zonenquelle, die Vorgabe Z4 kennt keine Nutzung, und die Zuordnung führt
auch dort, wo sie greift (Z6), auf EPOS-Muster statt auf den DIN/TS-18599-10-Katalog — die Raumart `RoomType`, die
jede Datei an jedem Raum trägt, wird für die Zonenbildung und für den Katalog nicht genutzt.


## 3. Katalog (Stand Testdatenbank)

- **Tabellen** (`RaumnutzungSchema`, Schritt 189; Werte DA-6, Schritt 231): `Tab_Raumnutzungskatalog` (Kategorie;
  zwei Zeilen: „EPOS-Muster“ `EPOS_MUSTER`, „DIN/TS 18599-10“ `DIN_V_18599_10`), `Tab_Raumnutzungsprofil` (`Nummer`
  TEXT, `Bezeichner`, 25 Kennwerte, `Werte_Herkunft/Quelle/Datum`), `Tab_Raumnutzungszeile`, `Tab_Raumnutzungsstunden`,
  `Tab_Raumnutzungszuordnung` (`Art` ∈ {`DIN_NUMMER`, `IFC_KLASSE`, `HOTTCAD_RAUMTYP`}, `Schluessel`, `ID_Profil`
  nullbar = „keine“, `ReadOnly`).
- **DIN/TS 18599-10:** 45 Profile — Nummern 1 bis 43 nach Tabelle 6 der Fassung 2025-10 (Einzelbüro, Gruppenbüro,
  Großraumbüro, Besprechung, Schalterhalle, Einzelhandel ×2, Klassenzimmer, Hörsaal, Bettenzimmer, Hotelzimmer,
  Kantine, Restaurant, Küche ×2, WC und Sanitärräume, Sonstige Aufenthaltsräume, Nebenflächen, Verkehrsflächen,
  Lager/Technik/Archiv, Rechenzentrum, Hallen ×3, Theater ×3, Messe, Museum, Bibliothek ×3, Turnhalle, Parkhaus ×2,
  Sauna, Fitness, Labor, Untersuchung, Spezialpflege, Pflegeflure, Praxen, Lagerhallen) und zwei Wohnprofile **ohne
  Nummer** („Wohngebäude – Einfamilienhaus“, „Wohngebäude – Mehrfamilienhaus“). **Eine Gruppenspalte gibt es nicht**;
  die Norm selbst führt Tabelle 6 ohne Gruppenüberschriften — eine Gliederung nach Gruppen ist eine Festlegung der
  Oberfläche (Abschnitt 6).
- **EPOS-Muster:** 9 (Wohnen, Büro, Schule, Sonstige, Sport, Gastronomie, Lager, Verkehr, Technik).
- **Zone → Profil:** `Tab_Zone.Nutzungsprofil` (TEXT, der Profil**name**; `RaumnutzungSchema.SPALTE_ZONE_NUTZUNGSPROFIL`),
  gesetzt von `ZonenplanCtrl.NutzungUebernehmen` (`EPOS.Kern/Controller/ZonenplanCtrl.cs:35–70`) samt Kalenderkopien
  je Größe über den Generator (`RaumnutzungCtrl.ProfilUebernehmen`); keine Fremdschlüssel-Id (NP-F23: ein Text ohne
  Katalogtreffer bleibt wählbar). In der Testdatenbank tragen 0 von 11 Zonen ein Profil (Referenzprojekte, nicht
  importiert). Der Zonenplan merkt sich die Herkunft als `Profilquelle(Art, Schluessel)` (Herleitungszeile im
  Zonenbaum, NP-F16).


## 4. Abgleich: Prototyp und Trefferquote

Prototyp (Python über STEP-Text und Testdatenbank, nur zum Messen; gelöscht): je Raum **Kandidatenmenge** aus der
Raumart, geschärft durch den Namensstamm, entschieden durch Fläche und Gebäudeart; **eindeutig** = genau ein Kandidat.

**Rangfolge des Prototyps**

| Stufe | Quelle | Regel | Beispiel |
|---|---|---|---|
| 0 | eigene Nutzungsangabe der Datei | `EPOS_Zone.DinNummer`/`.Nutzung`, Projektdatei-Profil, `LongName`/Beschreibung mit Katalogtreffer → **gesetzt, nie überschrieben** (Bedingung 1 des Anwenders) | in den sieben Dateien nirgends vorhanden |
| 1 | Raumart `RoomType` | Code → Kandidaten (Tafel, 28 Codes): `Conference` → 4; `WC`, `Bath`, `Shower`, `Locker` → 16; `Hall`, `HallWay`, `Stairway`, `Ante` → 19; `Store`, `StorageRoom`, `Basement`, `Roof`, `CentralHeating`, `Connection` → 20 (Store auch 43); `AdjoiningRoom` → 18; `Restaurant` → 13; `Eating` → 12/13; `Kitchen` → 14/15; `Fitness` → 37; `Sauna` → 36; `Examination` → 39; `Office` → 1/2/3; `Workshop` → 22/23/24; `Living`, `Child` → Wohnen/17; `Sleeping`, `Guests` → Wohnen/11 | `mrtOffice` → {1, 2, 3} |
| 2 | Raumname (Stamm ohne Nummer, klein) | Stichwort schärft die Kandidaten (Teilmenge) oder setzt sie, wenn die Raumart fehlt: „sitzung/besprech“ → 4, „empfang“ → 5, „kantine/essen“ → 12, „restaurant“ → 13, „kühlraum/getränke“ → 15, „küche“ → 14, „lagerhalle“ → 43, „produktion/werkstatt/montage“ → 22–24, „kegelbahn/ballraum“ → 33, „personalzimmer/gäste“ → 11, „massage/behandlung“ → 39, … | „Kühlraum“ [Store] → 15 statt 20 |
| 3 | Fläche | Büro: ≤ 15 m² → 1, ≤ 60 m² → 2, sonst 3; Lager: ≥ 200 m² → 43, sonst 20 | „Büroraum 014“ 22 m² → Gruppenbüro |
| 4 | Gebäudeart | Wohnanteil (Fläche der beheizten Räume mit Wohn-Raumart) ≥ 50 % → **Wohngebäude**: Wohnräume → Wohnprofil (MFH); sonst Nichtwohngebäude: `Living` → 17, `Sleeping`/`Guests` → 11 | Sportheim: Wohnanteil 27 % → „Wohnraum“ → Sonstige Aufenthaltsräume |

**Ergebnis je Datei** (Räume gesamt / eindeutig / mehrdeutig / keine Kandidaten):

| Datei | Räume | eindeutig | mehrdeutig | keine | Gebäudeart (Wohnanteil) | Häufigste Treffer |
|---|---:|---:|---:|---:|---|---|
| `MFH-Klein-unsaniert-1964.ifc` | 30 | 30 (100 %) | 0 | 0 | Wohnen (79 %) | Wohngebäude MFH 16, Verkehrsflächen 6, WC/Sanitär 3, Lager/Technik 3, Küche 2 |
| `MFH_mittel_1984.ifc` | 29 | 29 (100 %) | 0 | 0 | Wohnen (54 %) | Verkehrsflächen 9, Lager/Technik 7, Wohngebäude MFH 5, WC/Sanitär 4, Hotelzimmer 1, Fitness 1, Untersuchung 1 |
| `Produktion_groß_mit_Verwaltung_EG55-2026.ifc` | 49 | 44 (90 %) | 5 | 0 | Nichtwohnen (1 %) | Verkehrsflächen 14, WC/Sanitär 9, Lager/Technik 7, Großraumbüro 3, Gruppenbüro 2, Besprechung 3, Kantine 3, Lagerhallen 2 |
| `Sportheim_1970_unsaniert.ifc` | 56 | 56 (100 %) | 0 | 0 | Nichtwohnen (27 %) | Verkehrsflächen 15, Lager/Technik 13, WC/Sanitär 8, Sonstige Aufenthalt 4, Küche 3, Restaurant 3, Einzel-/Gruppenbüro 2+2, Hotelzimmer 2, Fitness 2 |
| `Sportheim_Bestand-1997_test_ifc4.ifc` | 56 | 56 (100 %) | 0 | 0 | wie oben | wie oben |
| `Verwaltung_mit_Montage-2969_vollsaniert_2014.ifc` | 113 | 109 (96 %) | 4 | 0 | Nichtwohnen (2 %) | Gruppenbüro 26, Verkehrsflächen 21, WC/Sanitär 18, Nebenflächen 18, Einzelbüro 14, Lager/Technik 7, Großraumbüro 3 |
| `WG-EH55_Poroton-GModG-2026.ifc` | 20 | 20 (100 %) | 0 | 0 | Wohnen (74 %) | Wohngebäude MFH 8, WC/Sanitär 6, Lager/Technik 2, Fitness 1, Sauna 1, Gruppenbüro 1 |
| **Summe** | **353** | **344 (97 %)** | **9** | **0** | | |

Alle neun mehrdeutigen Räume sind Werkstatt-, Betriebs- und Produktionsräume (`Workshop`) zwischen den drei
Hallenprofilen 22/23/24 (schwere, mittelschwere, leichte Arbeit) — das entscheidet kein Merkmal der Datei, nur der
Anwender. Ohne Stufe 3 (Fläche) wären 53 Büros und 2 Lager mehrdeutig; ohne Stufe 4 (Gebäudeart) 36 Wohnräume.

**Grenzen der Messung:** (1) Die Schwellen 15/60 m² und 200 m² sind Festlegungen des Prototyps, nicht der Norm (die
Norm zählt Arbeitsplätze); (2) im Wohngebäude ordnet der Prototyp Bad, WC, Flur und Küche noch Nichtwohnprofilen zu —
die Norm (Tabelle 5) rechnet ein Wohngebäude als **eine** Zone, der Entwurf fasst dort alle beheizten Räume im
Wohnprofil zusammen (Abschnitt 5.4); (3) die Beispiele stammen alle aus demselben CAD-Export — fremde IFC-Dateien
tragen andere Raumtyp-Vokabulare (`Pset_SpaceCommon.Category`, `IfcSpaceType.PredefinedType`, Klassifikation), für
die die Rangfolge offen bleibt (Stufe 2 trägt dann allein).

**Bedingung des Anwenders**, festgehalten: Automatisch zugeordnet wird **nur**, wenn (1) die Zone bzw. der Raum keine
eigene Beschreibung oder Nutzungsangabe trägt (Stufe 0 leer) **und** (2) genau ein Kandidat bleibt. Alles andere ist
ein **Vorschlag** mit Begründung, den der Anwender im Dialog annimmt oder ändert.


## 5. Entwurf (b): Zonenbildung aus IFC ohne `IfcZone`

### 5.1 Quelle und Regel Z7

Neue Zonenregel **Z7 „nach Nutzungsprofil“** (`IfcImportProfil.ZONENREGEL_Z7`): Räume mit **demselben zugeordneten
Profil** (Abschnitt 5.3, nach Rangfolge) und **derselben Beheizung** bilden eine Zone; der Zonenname ist der
Profilname (bei Bedarf „ 2“, „ 3“ wie `FreierName`); unbeheizte Räume bilden wie bei Z6 eine eigene, frei schwingende
Zone je Profil oder — Vorgabe — **eine** unbeheizte Zone („unbeheizt“), weil die Norm für unbeheizte Räume kein Profil
kennt. Räume ohne Kandidaten gehen in eine Zone „ohne Nutzung“ und stehen im Zonenbaum als **nicht zugeordnet**
(Sperre am OK wie heute, `Zonenplan.NichtZugeordnet`). Z7 braucht **keine Raumgrenzen** (wie Z6; die Bauteile kommen
aus dem Körperweg bzw. Grundriss) und steht zur Wahl, sobald mindestens zwei verschiedene Profile und weniger Profile
als Räume vorliegen. Die Obergrenze der Zonenzahl und die Mindestgröße gelten wie bei allen Regeln; mehrdeutige Räume
nehmen für die Zonenbildung den **ersten Vorschlag** (Abschnitt 5.3) und bleiben als „Vorschlag“ markiert.

Warum nicht Z6 erweitern: Z6 gruppiert nach gerundeter Solltemperatur und benennt nur; dass „20 °C“ Büro, Sanitär
und Gastronomie zusammenfasst, ist bei Z6 gewollt (Temperaturzonen). Z7 ist die Nutzungszonierung der DIN/TS
18599-10; beide bleiben wählbar. Z3 („nach der Nutzung“, Raumname) bleibt als Regel mit Raumgrenzen bestehen, wird
aber durch Z7 in der Sache abgelöst — Empfehlung: Z3 nur noch anbieten, wenn Z7 nicht wählbar ist.

### 5.2 Vorgabe, Vorrang, Verhältnis zu Einzonenweg, X1 und Zonenkalendern

- **Vorgabe (M7 erweitert):** Trägt die Datei keine Zonen (Z1 nicht wählbar), aber an allen beheizten Räumen eine
  Raumart oder einen Namen mit Treffer, wird **Z7 die Vorgabe**; sonst bleibt es bei Z4/Z5. Das ist eine Änderung der
  Vorgabe gegenüber heute und braucht den Entscheid des Anwenders (Frage F1). Die Vorgabe ändert keine gespeicherten
  Pläne: `ZonenplanCtrl.GespeichertJeGebaeude` legt bei Wiederimport weiter den gespeicherten Plan vor.
- **Vorrang:** (1) Zonen der Projektdatei (X1, Weg „IFC + Projektdatei“ oder „nur Projektdatei“) vor (2) Zonen der
  IFC-Datei (Z1) vor (3) Z7 aus der Raumart vor (4) Z4/Z5. Die Projektdatei trägt die Zonierung des Planers und ihre
  Profile (Rangstufe 1 des Konzepts DA 4.3); Z7 ist der Weg ohne sie.
- **Einzonenweg Z5:** Das Gebäude bekommt das Profil mit der **größten beheizten Fläche** (nicht das des
  flächengrößten Raums — Befund B4), über die bestehende Vorgaberegel 2.5 (`Einzonenvorschlag`, NP-F16, E130 F5),
  die um die Rangfolge 5.3 erweitert wird; der Haken wählt es ab. Die Einzonenwarnung zu nicht übernommenen
  Zeitplänen (N8, `IMP_BAUTEIL_PROT_EINZONE_ZEITPLAENE`) bleibt.
- **Z4-Zonen:** `DinNummerAnwenden` nimmt statt des flächengrößten Raums die **flächengewichtete Mehrheit** der
  Raumprofile der Zone (Schwelle ≥ 50 % der beheizten Fläche, sonst „keine“ mit Vorschlag) — behebt B4 auch für Z4
  und Z6.
- **Regel X1 (gbXML, Projektdatei):** unverändert (N8, E161). Der gbXML-Raumtyp läuft schon über
  `GbxmlRaumtyptafel`; die gbXML-Tafel wird **nicht mehr auf IFC-Raumtypen** angewandt (`DateiDinNummer` prüft das
  Format) — B4.
- **Zonenkalender (N7/N8):** Z7-Zonen tragen keinen Dateikalender (die Beispieldateien haben kein `EPOS_Kalender_*`);
  die Kalender entstehen wie heute aus dem Profil über den Generator (`ZonenplanCtrl.NutzungUebernehmen`). Trägt ein
  Raum einen Dateikalender (EPOS-Export, gbXML), gilt **Datei vor Profil** (`GebaeudeZonierung.Dateikonditionierung`,
  N7, E138 F2) — Z7 ruft dieselbe Stelle wie die anderen Regeln.
- **Kein Eingriff in den Rechenweg:** Der Import schreibt Daten wie heute (`GebaeudeZonenCtrl.VorschlagSchreiben`);
  Referenzprojekte werden nicht berührt, der Referenzlauf bleibt byte-gleich.

### 5.3 Profilzuordnung je Raum (c)

Neue Kernklasse `Raumprofilzuordnung` (ohne Datenbank; die Zuordnungszeilen kommen als `Raumnutzungsvorbelegung`
herein) mit `Zuordnen(AbbildRaum, Gebaeudeart) → Raumprofilbefund(Kandidaten, Vorschlag, Eindeutig, Begruendung)`:

1. **Stufe 0 — eigene Angabe der Datei** (hat immer Vorrang, wird nie überschrieben): `EPOS_Zone.DinNummer` → Zeile
   `DIN_NUMMER` (bestehender Weg), `EPOS_Zone.Nutzung` → `AusText`, Projektdatei-Profil; **neu:** `LongName` und
   `Description` des Raums mit Katalogtreffer (Profilname oder Nummer). Quelle `DATEI`.
2. **Stufe 1 — Raumart:** `Raumtyp` ohne Präfix → Zeile `HOTTCAD_RAUMTYP` (Saat, Abschnitt 7) → **das** Profil des
   Codes; der Index `idx_Raumnutzungszuordnung_Schluessel` ist eindeutig über (`Art`, `Schluessel`), also höchstens
   eine Zeile je Code. Die **Kandidatenmengen** mehrdeutiger Codes (`Office` → 1/2/3, `Workshop` → 22/23/24,
   `Eating` → 12/13, `Kitchen` → 14/15, `Store` → 20/43) stehen in der Codetafel des Kerns (`Raumarttafel`); eine
   Zuordnungszeile für einen solchen Code setzt den Anwenderwunsch und macht ihn eindeutig (eine Zeile hier geht der
   Tafel vor, Konzept Nutzungsprofile 5.4). Ohne Zeile und ohne Tafeleintrag: die bestehende `RAUMTYP_KLASSE` →
   `IFC_KLASSE` (Rückfall auf EPOS-Muster, wie heute).
3. **Stufe 2 — Raumname:** Stichworttafel im Code (`Raumnamentafel`, sprachneutral deutsch und englisch, nach dem
   Muster von `NAME_KLASSE`), schärft die Kandidaten oder setzt sie. Kein Schema: Eine Zuordnungsart `RAUMNAME`
   bräuchte den Umbau der `CHECK`-Klausel von `Art`.
4. **Stufe 3 — Fläche:** Büro 1/2/3 und Lager 20/43 über Schwellen in `Raumprofilfestwerte` (15 m², 60 m², 200 m²),
   Begründung „Fläche 22 m²“.
5. **Stufe 4 — Gebäudeart:** `Gebaeudeverwendung` liefert heute schon Wohnen/Nichtwohnen aus der Datei bzw. aus den
   Raumtypen nach Fläche (`GebaeudeImportAblauf.Verwendung`, Zeilen 684–700). Im Wohngebäude gehen **alle beheizten
   Räume** mit Wohn-Raumart samt Bad, WC, Küche, Wohnungsflur in das Wohnprofil (EFH bei einer Wohnung bzw. ≤ 2
   Wohneinheiten, sonst MFH — Frage F4); Treppenraum, Keller, Technik behalten 19/20. Im Nichtwohngebäude fallen die
   Wohnkandidaten weg.
6. **Ergebnis:** `Eindeutig` = genau ein Kandidat; `Vorschlag` = erster Kandidat in Normreihenfolge (Werkstatt →
   23 mittelschwer als Mitte; Frage F5); `Begruendung` = Kette der Stufen („Raumart Office · Fläche 22 m² →
   Gruppenbüro“). Die Zone übernimmt automatisch nur eindeutige Profile ohne eigene Angabe; alles andere steht als
   Vorschlag im Zonenbaum (Herleitungszeile mit Marke „Vorschlag“, Quelle-Arten neu: `RAUMTYP`, `RAUMNAME`, `FLAECHE`,
   `GEBAEUDEART`, `VORSCHLAG`).
7. **Protokoll:** eine Meldung je Gebäude `IMP_IFC_PROT_PROFILE_ZUGEORDNET` („{0} Räume eindeutig, {1} mit Vorschlag,
   {2} ohne Nutzung“) als Info, mit Beispielen der offenen Räume; je Zone die Herleitungszeile; im Prüfbericht der
   Modellprüfung die Ampel aus Konzept DA 4.3 (grün DIN-Profil mit Werten, gelb Vorschlag oder Muster, rot ohne
   Profil an beheizter Zone).


## 6. Zuordnungsdialog (d)

**Ort:** im bestehenden Gebäudeimport-Dialog (`EPOS.UI/Dialoge/Import/GebaeudeImportDialog.razor`), Abschnitt
„Zonenbaum“ (Zeilen 511–733), als **Überlagerung „Nutzungsprofile zuordnen…“** nach dem Muster der
Katalog-Überlagerung aus NP3b (Zeile 528). Knopf im Kopf des Zonenbaums neben der Regel-Klappliste (Zeile 401) und als
Verweis in der Zahl „{n} Räume mit Vorschlag / ohne Nutzung“ der Bilanz. Heute wählt der Anwender je Zone in einem
`<select>` mit zwei `optgroup`-Gruppen (EPOS-Muster, DIN/TS 18599-10 mit 45 Einträgen flach, Zeilen 596–622) — bei
100 Räumen und einer Zone je Profil geht das, bei Z4-Geschossen oder gemischten Zonen nicht; die Räume selbst sind
heute nicht zuzuordnen.

**Aufbau der Überlagerung (zwei Spalten, Standards des Hausblatts):**

- **Links — der Katalog, strukturiert:** Baum oder Akkordeon „Kategorie → Gruppe → Profil“. Die Gruppen der DIN-Profile
  sind eine Codetafel `DinProfilgruppen` (Nummer → Gruppenschlüssel, Ressource in beiden Sprachen; kein Schema):
  Büro und Verwaltung (1–5), Handel (6–7), Bildung (8–9, 30–32), Beherbergung und Pflege (10–11, 40–42),
  Gastronomie und Küche (12–15), Neben-, Verkehrs- und Lagerflächen (16–21), Hallen und Produktion (22–24, 43),
  Veranstaltung und Kultur (25–29), Sport und Wellness (33, 36, 37), Parken (34–35), Labor und Gesundheit (38–39),
  Wohnen (die zwei Wohnprofile). Suchfeld über Nummer und Name; EPOS-Muster als eigene Kategorie darunter; je Profil
  Kennwerte als Tooltip (Sollwert, Nutzungszeit) — dieselben Daten wie das Blatt „Nutzungsprofile“ (NP3a).
- **Rechts — die Räume der Datei, gebündelt:** eine Zeile je **Bündel** gleicher Raumart **und** gleichen
  Namensstamms („Büroraum ×43 · Office · 12–61 m²“), aufklappbar auf die Einzelräume (Geschoss, Fläche, Zone); Spalten:
  Bündel, Räume, Fläche, **Vorschlag** (Profil mit Marke eindeutig/mehrdeutig), **Begründung** (Kette der Stufen),
  **Zuordnung** (Klappliste mit denselben Gruppen wie links oder Übernahme der links gewählten Zeile per Knopf
  „→ zuordnen“). Filter „nur offene“, „nur Vorschläge“, „nur beheizte“. Sortierung: offene zuerst, dann nach Fläche.
  Die Verwaltung mit 113 Räumen wird so zu **17 Zeilen**, der Produktionsbau zu 13, das Sportheim zu 32.
- **Sammelzuordnung:** eine Wahl am Bündel gilt für alle Räume des Bündels; aufgeklappt kann ein Einzelraum
  abweichen („Büroraum 101“ → Großraumbüro), der Bündelwert zeigt dann „gemischt“. Knöpfe „Alle Vorschläge
  übernehmen“ (nur mehrdeutige mit Vorschlag; eindeutige sind schon gesetzt), „Vorschläge zurücknehmen“ (setzt die
  nicht von Hand gewählten zurück) und „Zuordnung aufheben“ für ein Bündel.
- **Wirkung:** Jede Änderung bildet die Z7-Zonierung neu (Räume wandern in die Zone ihres Profils); unter Z4/Z5/Z6
  ändert sie nur das Zonenprofil nach der flächengewichteten Mehrheit (5.2) und markiert gemischte Zonen. Die
  Herleitungszeile unter jeder Zone zeigt Quelle und Begründung (NP-F16) — „Raumart Office, Fläche 22 m², 26 Räume“.
  Schritte von Hand gelten als `Handgeaendert`; die Rückfrage vor einem Regelwechsel bleibt (Zeile 957).
- **OK-Sperre:** wie heute die Zahl der nicht zugeordneten Räume; neu dazu der Hinweis „{n} Räume mit Vorschlag“
  (keine Sperre — ein Vorschlag ist ein gültiges Profil, Frage F6).
- **Einzonenweg:** kein Dialog nötig; der `Einzonenvorschlag` nennt das Profil der größten Fläche mit Begründung
  und dem Haken wie heute.
- **Hülle:** `GebaeudeImportZonen` bekommt `Profilzuordnungsdaten(Zonenplan)` (Bündel, Vorschläge, Begründungen,
  Gruppen) und `GebaeudeImportHuelle` die Operationen `ProfilZuordnen(bündel|raum, schluessel)`,
  `VorschlaegeUebernehmen`, `VorschlaegeZuruecknehmen`; die Datenbankseite bleibt im Kern
  (`Raumnutzungsvorbelegung`, `ZonenplanCtrl`). Texte in `MyResource.Resource.*` in beiden Sprachen; Infoknopf auf
  die Hilfeseite „Gebäudeimport“.


## 7. Persistenz (e)

- **Gespeichert wird wie heute:** `Tab_Zone.Nutzungsprofil` (Profilname) samt Kalenderkopien über den Generator;
  die Raumpaarungen in `Tab_Importzuordnung` (Herkunftsablage, Schritt 138) zeigen Raum → Zone, der gespeicherte Plan
  findet Zone und Profil wieder (`Zonenplan.AusGespeichert`, `ZonenplanCtrl.Gespeichert`). `Tab_Importquelle.Zonenregel`
  ist TEXT ohne `CHECK` — `Z7` passt ohne Schemaänderung. Eine Profilwahl **je Raum** muss nicht gespeichert werden:
  Beim Wiederimport bildet der gespeicherte Plan die Zonen, und das Profil der Zone steht an der Zone.
- **Ein Schemaschritt ist nötig — als Saat, nicht als DDL:** `Tab_Raumnutzungszuordnung` kennt die Art
  `HOTTCAD_RAUMTYP` schon (CHECK-Klausel), trägt aber keine Zeile. Nach ADR-001 kommt Saat nur über `SchemaMigration`.
  Empfohlen **Schritt 244 `RaumnutzungRaumtypZuordnungSchema`** (nicht angemeldet, nur empfohlen; die nächste freie
  Nummer ist 244 laut Statuskopf):
  1. Saat von rund 23 Zeilen `HOTTCAD_RAUMTYP` (`ReadOnly` = 1) → DIN-Profil nach der Tafel in 5.3 — nur die
     eindeutigen Codes; der Index ist eindeutig über (`Art`, `Schluessel`), die mehrdeutigen Codes (`Office`,
     `Workshop`, `Eating`, `Kitchen`, `Store`) bekommen **keine** Zeile, ihre Kandidaten stehen in der Codetafel des
     Kerns, und eine Zeile des Anwenders macht sie eindeutig — Frage F5;
  2. Umstellen der 18 ausgelieferten `DIN_NUMMER`-Zeilen vom EPOS-Muster auf das **DIN-Profil gleicher Nummer**
     (NP-F13 ist mit DA-6 überholt, weil die DIN-Profile Werte tragen); 70 und 71 → „Wohngebäude – Mehrfamilienhaus“
     bzw. „– Einfamilienhaus“; Zeilen des Anwenders (`ReadOnly` = 0) bleiben unverändert — Frage F2;
  3. `IFC_KLASSE`-Zeilen bleiben als Rückfall ohne Raumart.
  Wiederholbar, mit Wache (`RaumnutzungSaatWacheTests` erweitert: Zahl der Zeilen je Art, Ziel je Zeile), Testdatenbank
  gehoben, Auslieferungsvorlage und `Katalogpaket.json` nachgezogen. Keine Spalte, keine Tabelle, kein Eingriff in
  Referenzprojekte oder Rechenweg; der Referenzlauf bleibt byte-gleich.
- **Ohne Schemaschritt** ginge nur die Codetafel (Raumart → DIN-Nummer im Kern) — dann aber ohne Pflegbarkeit durch
  den Anwender (Konzept Nutzungsprofile 5.4: „eine Zeile hier geht ihr vor“) und ohne Umstellung der
  `DIN_NUMMER`-Zeilen; nicht empfohlen.


## 8. Tests und Abnahme (f)

| Prüfung | Was | Wo |
|---|---|---|
| Rangfolge je Raum | Stufe 0 schlägt alles; Raumart → Kandidaten; Name schärft; Fläche entscheidet Büro/Lager; Gebäudeart Wohnen/Nichtwohnen; eindeutig/mehrdeutig/keine; Begründungskette | `EPOS.Kern.Tests/RaumprofilzuordnungTests` (synthetische `AbbildRaum`, ohne Datenbank) |
| Zuordnung mit Katalog | `HOTTCAD_RAUMTYP`-Zeile geht der Codetafel vor; Zeile „keine“ wirkt; Zeile des Anwenders überlebt die Saat | `RaumnutzungsvorbelegungTests`, `RaumnutzungSaatWacheTests` (Zahlen je Art) |
| Regel Z7 | wählbar ab zwei Profilen; Zonen je Profil und Beheizung; Namen; unbeheizte Zone; „ohne Nutzung“ nicht zugeordnet; Obergrenze; Rückfrage bei Regelwechsel | `GebaeudeZonierungTests`, `ZonenplanProfilTests` |
| Vorgabe | Z7 Vorgabe nur ohne `IfcZone` und mit Raumart an allen beheizten Räumen; sonst Z4/Z5 unverändert (Gegenprobe mit den IFC-Proben des Testprojekts) | `GebaeudeZonierungTests` |
| Z4/Z6/Z5 Profil | flächengewichtete Mehrheit statt flächengrößter Raum; gbXML-Tafel nicht mehr auf IFC-Raumtypen (B4) | `ZonenplanProfilTests`, `GbxmlRaumtyptafelTests` |
| Quellenprobe | die sieben Beispieldateien: je Datei Zahl eindeutig/mehrdeutig/keine wie Tabelle 4 (±0), Z7-Zonenzahl; übersprungen, wenn eine Datei fehlt | `EPOS.Kern.Tests/ZonenzuordnungQuellenprobeTests` (Muster `MehrzonenQuellenprobeTests`) |
| Schreibweg | Z7-Plan → `VorschlagSchreiben` → `Tab_Zone.Nutzungsprofil` und Kalender je Zone; Wiederimport findet den Plan | `ZonenplanDatenbankTests`, `GebaeudeImportZonenplanHuelleTests` |
| Hülle | Bündel, Vorschläge, Begründungen, Gruppen; Sammelzuordnung; „Alle Vorschläge übernehmen/zurücknehmen“ | `EPOS.Kern.Tests/GebaeudeImportProfilzuordnungHuelleTests` |
| Dialog | Überlagerung öffnet, Gruppenbaum, Filter, Sammelzuordnung ändert die Zonierung; beide Sprachen | `EPOS.UI.Tests` (bunit) |
| Wachen | `DokumentationLinkWacheTests`, `RepositoryOrdnungWacheTests`, `WikiProduktdatenWacheTests` (Wiki-Quelle), Ressourcen-Designer | Gate |
| Nachweis | Kern-Filter grün, SQL-Dialekt-Prüfer, Windows-Schale kompiliert (Hülle berührt), Referenzlauf byte-gleich gegen R59, Sichtabnahme unter Windows an der Verwaltung (113 Räume) über EPOS Lokal | Gate der Orchestrierung |

Abnahmekriterien: An den sieben Beispielen ohne Projektdatei entstehen mit Z7 Nutzungszonen mit DIN-Profilen; die
Verwaltung braucht höchstens **vier** Handgriffe (Werkstatt-Bündel), das Sportheim **keinen**; kein Referenzprojekt
ändert sich.


## 9. Umsetzungsschritte mit Modellempfehlung (g)

| Welle | Inhalt | Modell | Aufwand |
|---|---|---|---|
| **ZZ-1 Kern — Zuordnung und Saat** | `Raumprofilzuordnung` mit Rangfolge, Codetafel Raumart → Kandidaten, `Raumnamentafel`, `Raumprofilfestwerte`; Schemaschritt 244 (Saat `HOTTCAD_RAUMTYP`, Umstellung `DIN_NUMMER`), Wache, Testdatenbank, Auslieferungsvorlage; `DateiDinNummer` nach Format; flächengewichtete Mehrheit | **Fable** (Schemakette mit Hebung der Testdatenbank) oder Opus für Zuordnung + Fable für die Kette | 2–2,5 PT |
| **ZZ-2 Kern — Regel Z7 und Vorgabe** | `ZONENREGEL_Z7`, `Waehlbar`, Gruppierung, Namen, Bilanz, Vorgabe nach F1, Einzonenvorschlag nach größter Fläche, Meldung `IMP_IFC_PROT_PROFILE_ZUGEORDNET`, Quellenprobe | Opus | 1,5–2 PT |
| **ZZ-3 Hülle** | `GebaeudeImportZonen.Profilzuordnungsdaten`, Operationen in `GebaeudeImportHuelle`, `DinProfilgruppen`, Hüllentests; Windows-Schale kompiliert | Opus | 1 PT |
| **ZZ-4 Dialog und Ressourcen** | Überlagerung „Nutzungsprofile zuordnen…“, Knopf und Bilanz im Zonenbaum, Marken in der Herleitungszeile, bunit-Tests; Texte in beiden Sprachen, `designer_neu.py` | Opus (Dialog), Sonnet (Ressourcen) | 1,5 PT |
| **ZZ-5 Papiere, Wiki, Logbuch** | Konzept Nutzungsprofile 5.4 und Konzept DA 4.3 fortschreiben (NP-F13 ablösen), Mehrzonenkonzept 6.1 um Z7, Wiki-Quelle „Gebäudeimport“ (Abschnitt Zonenregeln und Nutzungsprofile, ohne Hersteller- und Produktdaten), Logbuch-Entwurf (Versionsnummer beim Anwender erfragen), Statuszeile, Protokoll, dieses Papier nach `ueberholt/` | Sonnet | 0,5 PT |
| Prüfungen | BOM/CRLF, Konfliktmarker, Zählungen | Haiku | — |

Reihenfolge ZZ-1 → ZZ-2 → ZZ-3 → ZZ-4 → ZZ-5; ZZ-3 kann parallel zu ZZ-2 beginnen (getrennte Dateien). Jede Welle
im eigenen Worktree, baut den Kern-Filter und testet mit `--filter`; das Gate fährt die Orchestrierung nach dem
Merge. Rund **6,5–7,5 PT**. Berührungen: Welle DA-5 (Profilprüfung, E135) geht in ZZ-1/ZZ-2 auf; der Entwurf GD
(Platzierung nicht zugeordneter Räume) und MA (Prüfgruppe 3 der Modellprüfung) bleiben unverändert.


## 10. Offene Fragen an den Anwender (h)

| Nr. | Frage | Optionen | Empfehlung |
|---|---|---|---|
| **F1** | Soll Z7 „nach Nutzungsprofil“ die **Vorgabe** für IFC-Dateien ohne Zonen werden, wenn alle beheizten Räume eine Raumart tragen? | (a) ja, Z7 Vorgabe; (b) Z4 bleibt Vorgabe, Z7 nur wählbar; (c) Z7 Vorgabe nur, wenn alle Räume eindeutig sind | **(a)** — der Auftrag will Nutzungszonen; Geschosszonen ohne Nutzung sind das heutige Fehlbild. Mehrdeutige Räume nehmen den Vorschlag und bleiben markiert |
| **F2** | Sollen die ausgelieferten `DIN_NUMMER`-Zeilen vom EPOS-Muster auf das **DIN-Profil gleicher Nummer** umgestellt werden (NP-F13 ablösen)? | (a) ja, ReadOnly-Zeilen umstellen, Anwenderzeilen bleiben; (b) nein, nur neue `HOTTCAD_RAUMTYP`-Zeilen auf DIN-Profile | **(a)** — die DIN-Profile tragen seit DA-6 Werte; sonst bekommen Projektdatei-Zonen weiter Muster, IFC-Zonen DIN-Profile |
| **F3** | Büro nach Fläche: Schwellen **15 m²** (Einzel-), **60 m²** (Gruppen-), darüber Großraumbüro; Lager **200 m²** → Lagerhalle? | (a) so; (b) andere Werte; (c) Büro immer mehrdeutig, Vorschlag Gruppenbüro | **(a)** als Festwerte des Kerns, in der Begründung sichtbar; die Norm zählt Arbeitsplätze, die die Datei nicht trägt |
| **F4** | Wohngebäude: Wohnanteil ≥ 50 % der beheizten Fläche → alle Wohnräume samt Bad, WC, Küche, Wohnungsflur in **ein** Wohnprofil; EFH bis zwei Wohneinheiten, sonst MFH? | (a) so; (b) Wohnprofil nur für Wohn-/Schlafräume, Bad/WC → 16; (c) Grenze EFH/MFH nach Fläche | **(a)** — Tabelle 5 der Norm rechnet das Wohngebäude als eine Zone; die Wohneinheit kommt aus `LivingArea`/Wohnungstabelle oder bleibt Vorschlag MFH |
| **F5** | Werkstatt/Produktion (`Workshop`) zwischen Hallen 22/23/24: welcher Vorschlag? | (a) 23 mittelschwere Arbeit; (b) 24 leichte Arbeit; (c) kein Vorschlag, immer offen | **(a)** als Vorschlag, nie automatisch; der Anwender entscheidet im Bündel mit einem Griff |
| **F6** | Darf der Import mit **Vorschlägen** (mehrdeutig, nicht bestätigt) abgeschlossen werden? | (a) ja, Hinweis in der Bilanz, Ampel gelb; (b) Sperre am OK bis alle bestätigt sind | **(a)** — Leitziel MA „in wenig Zeit zum Modell“; die Ampel der Modellprüfung hält es sichtbar |
| **F7** | Soll die Rangfolge auch im Weg **mit Projektdatei** greifen, wo ein Raum keiner DIN-Zone angehört (Entwurf GD: „Raum ohne DIN-Zone sperrt Plan“)? | (a) ja, als Vorschlag für die offenen Räume; (b) nein, Projektdatei allein | **(a)** — derselbe Code, kein zweiter Weg; die Zonen der Projektdatei bleiben unberührt |
| **F8** | Logbuch-Eintrag und Wiki-Seite „Gebäudeimport“: Versionsnummer? | — | Eintrag: „IFC-Dateien ohne Zonen werden nach Nutzungsprofil zoniert; Raumarten werden den Profilen der DIN/TS 18599-10 zugeordnet, mit Vorschlägen und Sammelzuordnung im Importdialog.“ |


## 11. Register

- **Belege:** `IfcAbbildBauer.cs:1736–1812` (Zonen), `GebaeudeZonierung.cs:430–457` (Wählbarkeit), `:1266–1278`
  (`RAUMTYP_KLASSE`), `:1304–1315` (`Nutzungsklasse`), `Zonenplan.cs:336–353` (`DinNummerAnwenden`, `DateiDinNummer`),
  `:415–432` (`NutzungMitQuelle`), `Raumnutzungsvorbelegung.cs:195–228` (`Herleitung`, `Aufloesen`),
  `GbxmlRaumtyptafel.cs:51`, `ZonenplanCtrl.cs:35–70`, `GebaeudeZonenCtrl.cs:1334–1345`,
  `GebaeudeImportDialog.razor:394–408, 511–733, 596–622`, `GebaeudeImportZonen.cs:180–222`.
- **Nicht committet:** Textzähler und Abgleich-Prototyp (Python, Scratchpad), Diagnosetest
  `ZzDiagnoseZonenIfcTests` (gelöscht).
- **Abgrenzung:** keine Änderung an Rechenweg, Referenzprojekten, Basis R59, Projektdatei-Weg (X1, E87), gbXML-Weg;
  keine Hersteller- oder Produktdaten; keine Werte aus der Anwenderdatei.
