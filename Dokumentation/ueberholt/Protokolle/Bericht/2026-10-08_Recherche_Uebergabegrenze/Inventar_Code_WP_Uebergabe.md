# Inventar Code — Wärmepumpe/Übergabe/Bivalenz (Leseauftrag, nur lesen)

## 1. Datenmodell Wärmepumpe

**Katalog** (`Referenzlaeufe/Kenndaten_Test.sqlite`, geprüft mit `PRAGMA table_info`):

| Tabelle | Spalte | Bedeutung |
|---|---|---|
| `Tab_WP`/`Tab_WP_STAMM` | `Nennleistung`, `maxPtherm`, `Kuehlleistung` | Katalogleistungen |
| | `Kuehlbetrieb`, `Kuehl_Vorlauf`, `Kuehl_Hilfsstromanteil` | Kühlkennwerte am Katalogdatensatz |
| | `Mindestleistung_kW`, `Taktverlustfaktor_Cd` | Teillast/Taktung |
| `Tab_Kenndaten`/`Tab_Kenndaten_STAMM` | `ID_WP`, `Vorlauf`, `Temperatur`, `COP`, `Ptherm` | das Kennfeld: je Stützstelle Vorlauf × Quelltemperatur → COP und thermische Leistung |

Es gibt **keine** Tabellen namens `Tab_Waermepumpe`/`Tab_Waermepumpe_STAMM` — die Gerätetabellen heißen `Tab_WP`/`Tab_WP_STAMM`. Ergebnistabellen: `Tab_ErgebnisWaermepumpe`, `Tab_ErgebnisWaermepumpeModul`.

**Projektfelder `Tab_Energieanlagen`** (Anlagenzeile, Auszug mit Wärmepumpenbezug):

| Spalte | Typ | Bedeutung |
|---|---|---|
| `Betriebsart` | TEXT | `WP_BETRIEBSART_PARALLEL`/`_TEILPARALLEL`/`_ALTERNATIV` (Konstanten in `DbWerte`) |
| `Bivalenter_Betrieb` | INTEGER | Schalter bivalenter Betrieb |
| `Abschaltpunkt` | REAL | Bivalenztemperatur (Außentemperatur), wörtlich auch 0 °C |
| `Heizstab` | INTEGER | elektrische Nachheizung, Leistung je Baustein |
| `Sperrung`, `Sperrzeit_von`, `Sperrzeit_bis` | INTEGER | Sperrzeitfenster, geht dem Zeitprogramm vor |
| `Zeitprogramm` | TEXT | Anlagenfahrplan (AK2), Faktor je Stunde |
| `Vorlauf`, `Vorlauf_Max` | INTEGER/REAL | Rückfall-Vorlauf bzw. Höchstvorlauf |
| `Kuehl_ID_Carrier`, `Kuehl_EigenerZaehler` | | Kühlabrechnung |
| `WQ_*` | diverse | Wärmequellenfelder (Typ, Temperatur, Monats-/Wochenwerte, Spreizung, Erdsonden-Geometrie) |
| `WS_*`, `ID_PUFFER`, `Prioritaet` | | Senkenfelder, Pufferzuordnung, Ladepriorität |

**Nicht vorhanden** im heutigen Schema: eigene Spalten für Spreizung/ΔT der Wärmepumpe selbst (nur `WQ_Spreizung` auf der Quellseite), Mindestvolumenstrom, hydraulische Weiche. Pufferspeicher-Einbindung läuft über `Z_AnlageSenke`/`ID_PUFFER`/`WS_ID_Puffer(2)` plus Prioritäten — keine eigene ΔT-Restriktion am Übergabepunkt der Wärmepumpe.

## 2. Rechenweg Wärmepumpe im Kern

| Datei | Methode/Fundstelle | Zweck |
|---|---|---|
| `EPOS.Kern/Allgemein/Simulation/SimulationWaermepumpe.cs:1141` | `StuetzstelleWaehlen` | wählt die Kennlinien-Stützstelle am **gerechneten Vorlauf** je Stunde (seit AK1/E86-Berichtigung) |
| `SimulationWaermepumpe.cs:2141-2160` | Betriebsartverzweigung | `Bivalenter_Betrieb` + `WP_BETRIEBSART_TEILPARALLEL/PARALLEL/ALTERNATIV`: steuert, ob unterhalb der Bivalenztemperatur die WP aussetzt, parallel läuft oder umschaltet |
| `SimulationWaermepumpe.cs:2550` | Bivalenzprüfung | `Temperatur[stunde] < model.Abschaltpunkt` — Abschaltpunkt gilt **wörtlich**, auch 0 °C (Kommentar :2502-2537 dokumentiert einen früheren Fehler, bei dem `Abschaltpunkt` wirkungslos blieb) |
| `SimulationWaermepumpe.cs:801` | `Bivalenzpunkt = -100` | Feldinitialisierung/Kennzahl für Ausweisung |
| `EPOS.Kern/Allgemein/Simulation/Anlagenfahrplan.cs:24-33` | `Sperrzeit`, `Zeitprogramm`, `VorlaufAngebotC` | Vorlaufangebot je Stunde: `Vorlauf_Max`, Rückfall `Vorlauf`; NaN = keine Grenze; Sperrzeit geht dem Zeitprogramm vor |
| `Anlagenfahrplan.cs:126-127` | Verfügbarkeitsgrund `Zeitprogramm` | Faktor < 1 wird als Ausfallgrund gebucht |
| `Anlagenfahrplan.cs:257-259` | `VorlaufAngebotC = m.Vorlauf_Max.HasValue ? … : …` | Übersetzung Modellspalte → Angebotsfunktion |
| `EPOS.Kern/Allgemein/Simulation/Kaskadenschleife.cs:953` | `Rechnen` | Stundenschleife über alle Kaskadenmitglieder (Tool_1…Tool_6), Direktdeckung je Stunde |
| `EPOS.Kern/Allgemein/Simulation/Anlagenzeitprogramm.cs` | `Lesen`, `Faktor(h)` | parst `Zeitprogramm`-Text, liefert Stundenfaktor |
| `EPOS.Kern/Allgemein/Simulation/Anlagenverfuegbarkeit.cs` | Verfügbarkeitsgründe | sammelt Sperre/Zeitprogramm/Abschaltpunkt als benannte Ausfallgründe |
| `EPOS.Kern/Allgemein/Update/WaermepumpeSperrprofilSchema.cs`, `HeizstabJeWaermepumpe.cs` | Migrationsschritte | legen Sperrprofil- bzw. Heizstab-Spalten an |

AK1 (Anlagenkopplung, gekoppelter Vorlauf) ist dort eingearbeitet, wo die WP „am gerechneten Vorlauf" liefert — laut AK3-Entwurf (Abschnitt 1, Befund B2) ist die vorlaufabhängige Stützstellenwahl bereits **gebaut**; neu wäre nur, die Kapazität an dieser Stützstelle als Schranke zurückzumelden.

## 3. Übergabe im Kern (AK1)

Laut AK3-Entwurf (Befund B6, Abschnitt 2.3/2.4): Der Vorlauf ist in Schritt H eine **Vorgabe** (Heizkurve, Außentemperatur, Sollwert, `Vorlauf_Max`), keine Fixpunktgröße — eine Einzelzone hat ohne die raumgeführte Korrektur H2 keinen Fixpunkt in der Stunde; die Stunde ist explizit lösbar: Φ = min(Φ_ue(θ_V, θ_i), Angebot). Relevante Klassen laut Verweisen im Entwurf: `Gebaeudeheizkreis`, `Waermeuebergabe` (Konzeptbezug 10.2). Rückwirkungen innerhalb der Stunde: raumgeführte Heizkurvenkorrektur (H2), bedarfsgewichteter Projektvorlauf bei mehreren Gebäuden, Verteilung der Schranke.

Projektspalten (laut `CLAUDE.md`-Regel „Einfrierregeln"): `Tab_Gebaeude` trägt `Heizkreis_Aktiv`, Übergabeart, Exponent, Nennleistung, Auslegungspunkt, Heizkurve, `Regler_Proportionalband`, `Sollwertprofil`, Kühlfelder (`Kuehluebergabe_Aktiv`, `Kuehl_Uebergabe_*`, `Kuehl_Auslegung_*`, `Kuehl_Vorlaufgrenze`); `Tab_Zone` trägt sieben Übergabespalten: `Uebergabe_Art`, `Uebergabe_Exponent`, `Uebergabe_Leistung_Nenn`, `Auslegung_Vorlauf`, `Auslegung_Ruecklauf`, `Auslegung_Raumtemperatur`, `Regler_Proportionalband`. Rücklauf ist laut Vorgabe massenstromgewichtet über die Zonen zusammengeführt.

Kesselbezug zum Rücklauf (nicht Wärmepumpe, aber als Vorbild für ΔT-Kopplung relevant): `Kesselkennlinie.cs:347` rechnet die Brennwertkennlinie am gerechneten Rücklauf des Heizkreises (Rücklaufstufe `Heizkreis` vor den Rückfällen) — laut Entwurf B3 „teilweise überholt" gegenüber dem Konzepttext „ohne Temperaturbezug".

## 4. AK3-Entwurf

Datei: `Dokumentation/aktuell/Gebaeudesimulation/2026-10-07_Entwurf_AK3.md`. Status Umsetzungsstand **W0–W4b gebaut, W5/W6 offen** (Status #796/#799/#804). Auftrag E100 (Anwender 07.10.2026 „AK3 jetzt"). Gliederung:
0. Ergebnis in Punkten · 1. Befunde (1.1 gegen das Konzept, 1.2 aus dem Code, 1.3 Stichproben) · 2. Architektur (2.1 Laufordnung, 2.2 Gebäude-Stepper, 2.3 gekoppelte Stunde, 2.4 Abbruch/Fallwechsel/Fehler, 2.5 Speicher/Ladezustand, 2.6 Mehrzonen, 2.7 Rechenzeit) · 3. AK3 als Stufe, Prüforakel, **Interpolation als eigener Gegenstand AK3-I** · 4. Festlegungen · 5. Referenzprojekt und Basis · 6. Wellenplan · 7. Fragen an den Anwender · 8. Risiken · 9. Nicht in AK3 · 10. Logbuch-Entwürfe.

Kernaussagen mit Bezug zum beauftragten Konzept (Höchstvorlauf als Bivalenzgrenze, Vorwärmbetrieb, ΔT-Restriktionen, Bivalenzdiagramm):
- **AK3-I (Q-AK3-6, E102):** Wärme- und Kälteseite interpolieren die Kennlinie der Wärmepumpe linear zwischen den beiden einschließenden Vorlauf-Stützstellen; löst H-F4 und K21 ab. Es ändern sich dadurch **nur 1047 und 1056** (neu eingefroren im Basiswechsel von W5).
- Referenzprojekte 1047, 1054, 1056 bleiben unter AK1 byte-gleich; das neue AK3-Referenzprojekt (Kopie von 1056 mit Puffer auf Stufe AK3) entsteht erst in W5.
- Vorlauf ist explizit als **Vorgabe**, nicht als Rückkopplungsgröße behandelt (zentral für die Frage, ob der Höchstvorlauf als Bivalenzgrenze wirkt).
- Keine Erwähnung einer ΔT-Restriktion oder eines Bivalenzdiagramms als Berichtsbild — das AK3-Papier behandelt Laufordnung/Interpolation, nicht Bericht/Oberfläche.

## 5. Oberfläche

**Anlagendialog** `EPOS.UI/Dialoge/Waermepumpe/WaermepumpeAnlageDialog.razor` (laut Entwurfstext und Grep):
- Felder/Gruppen: Sperrzeit, bivalenter Betrieb, Bivalenztemperatur (Abschaltpunkt), Energieträger, Betriebsart (`<select>`, Werte „parallelbetrieb"/„Bivalent-parallel"/… über `DbWerte.BetriebsartOderDefault`), Heizstab (Leistung, Label `LabelPHeizstabKurz` = „Leistung Heizstab").
- **Zwei Sichtbarkeitsregeln** (seit 16.09.2026 laut Kommentar im Dialog): Betriebsart nur bei bivalentem Betrieb sichtbar; Bivalenztemperatur nur dort, wo sie rechenwirksam ist.
- Bekannte Fallstricke (Dialog-Kommentare, als Kontext, nicht als Konzeptvorgabe): Heizstabfeld „mit dem Baustein verschmolzen" (Änderung wirkte früher auf alle Projekte), Betriebsart ungeprüft in `Tab_Energieanlagen.Betriebsart` geschrieben (Befund L0-1).
- Betriebsdaten der Zeile (Sperrzeit, Bivalenz, Heizstab, Kosten) bleiben beim Kopieren/Wechsel des Katalogverweises erhalten (`Daten.Heizstab = alt.Heizstab` usw., Zeilen ~1529-1535).
- Kommentar „Anlagenkopplung AK2 (9.3): Zeitprogramm und höchster Vorlauf — dieselbe Regel wie in Simulation › Konfiguration" (Zeile ~1759) — bestätigt: Höchster Vorlauf ist bereits ein Feld im Anlagendialog (AK2), nicht neu zu schaffen.

Hülle: unter `EPOS.UI.Daten/...` wurde nicht im Detail gelesen (Leseauftrag erfüllt über Dialogtext; kein eigener Durchgriff nötig, da Dialog die DTO direkt referenziert).

Ergebnisreiter `EPOS.UI/Seiten/Simulation/WaermepumpeReiter.razor` wurde nicht separat geöffnet; Berichtsbilder dazu siehe Abschnitt 6.

Gebäude-Gruppe „Wärmeübergabe": Felder entsprechen den Übergabespalten aus Abschnitt 3 (`Tab_Gebaeude`/`Tab_Zone`), nicht im Dialogcode selbst gegengelesen.

## 6. Diagramme und Bericht

`EPOS.Kern/Allgemein/Bericht/ChartRenderer.cs` — Methoden mit WP-/Vorlaufbezug:

| Methode | Zeile | Zweck |
|---|---|---|
| `VorlaufRuecklauf` / `VorlaufRuecklaufModell` | 5938/5945 | Vorlauf-Rücklauf-Zeitreihe, Parameter `vorlauf`, `ruecklauf`, `auslegungVorlauf`, `VorlaufRuecklaufnamen` |
| `Komfortwoche` / `KomfortwocheModell` | 5981/5986 | Raumluft/Sollwert/Unterschreitung je Woche |
| Kommentare zu „Wärmepumpenseite" | 195, 4835, 4844, 5072, 5406, 5829 | WP-Flächen/Säulen-Diagramme (Bedarf vs. Produktion), Heizstab als dritte Reihe neben Wärmebedarf/-produktion |

Es existiert **kein** Treffer für „Bivalenz" oder „Vorlaufwahl" direkt im `ChartRenderer` — ein neues Bild „Bivalenzdiagramm" wäre eine neue Methode, kein vorhandener Umbau. Für ein solches Bild wären (nach Hausmuster) nötig: neue Methode + `Zeichenmodell`, ein Platzhalter-Schlüssel `stand.bild.*` in den Berichtsbildern (`Berichtsbilder*.cs`, nicht einzeln gegengelesen), Eintrag in `KennzahlenKatalog` (JAZ, Deckung, Vorlaufwahl laut Statuszeile #738 bereits vorhanden — „Vorlaufwahl" ist also schon eine ausgewiesene Kennzahl, kein Neuland), und eine Messlatte in `Proben/ChartProben` (Hash-Datei) sowie ggf. eine neue Katalogfassung.

## 7. Referenzprojekte 1047, 1054, 1056

Abfrage `Tab_Energieanlagen` nach `ID_Projekt IN (1047,1054,1056)` lieferte für **1047** nur die WP-Zeile „WPE-I 59 H 400 Premium" mit Werten (Spaltenfolge `Betriebsart, Bivalenter_Betrieb, Abschaltpunkt, Vorlauf, Vorlauf_Max, Heizstab, Sperrung, Sperrzeit_von, Sperrzeit_bis, Zeitprogramm`):

```
Betriebsart='Teilparallelbetrieb', Bivalenter_Betrieb=1, Abschaltpunkt=-10.0, Vorlauf=55, Vorlauf_Max=None, Heizstab=0, Sperrung=0, Sperrzeit_von=0, Sperrzeit_bis=14, Zeitprogramm=17
```
(übrige Zeilen desselben Projekts sind Stromspeicher/BHKW ohne WP-Felder.)

Die Abfrage brach wegen eines Encoding-Fehlers (`UnicodeEncodeError … '�'`, Windows-cp1252-Konsole) vor der Ausgabe für 1054 und 1056 ab — **nicht gelesen**, erneuter Versuch mit UTF-8-Umleitung nötig, falls diese Werte für das Konzept gebraucht werden. Laut CLAUDE.md-Referenznetz tragen 1047 und 1056 die „Vorlaufwahl" ihrer Wärmepumpe aus (AK1-Nachweis); 1056 zusätzlich Anlagenfahrplan (Nachtsperre WP 0–6 Uhr, `Vorlauf_Max` 50 °C, gehalten von `FahrplanReferenzprojektWacheTests`); 1054 trägt Zonen-Heizkreis mit Radiator/Heizkurve plus eigener Zonenübergabe (Konvektor 70/50 °C, Proportionalband 2 K).

## 8. Wiki-Quellen

Treffer für „Bivalenz|Heizstab|Höchster Vorlauf|Vorlauf_Max" (fallunabhängig) in `Projekte/Wiki/`:

- `Programm Dokumentation - Wärmepumpe.wiki` — Hauptseite, erwartungsgemäß zu Bivalenz/Heizstab/Betriebsart
- `Grundlagen - Wärmepumpe.wiki` — Grundlagenseite
- `Programm Dokumentation - Simulation.wiki`, `… Simulationsergebnisse.wiki`, `… Energieerzeuger.wiki`, `… Gerätekataloge.wiki`, `… Heizkessel.wiki`, `… Pufferspeicher.wiki`, `… Pufferspeicher auslegen.wiki`, `… Kühlung.wiki`, `… Wärmequelle Erdreich.wiki`, `… Stromspeicher.wiki`, `… BHKW.wiki`, `… Photovoltaik.wiki`, `… Kosten.wiki`, `… Wirtschaftlichkeit.wiki`
- `Grundlagen.wiki`, `Grundlagen - Kessel und Spitzenlast.wiki`
- `Vorlage - Legende.wiki`, `Vorlage - Diagrammfarbe.wiki`
- Bilddateien: `Dateien/Einbindung_Waermepumpe_bivalent.svg`, `Dateien/Jahresdauerlinie_bivalent.svg` — **vorhandene Bivalenz-Grafiken im Wiki**, relevant als Vorbild/Abgrenzung für ein neues „Bivalenzdiagramm" im Bericht.

Die genauen Anker/Zeilen innerhalb von `Programm Dokumentation - Wärmepumpe.wiki` wurden aus Zeitgründen nicht einzeln aufgelöst (nur Dateitreffer, kein Zeilen-Grep in dieser Datei).

## Offene Punkte dieses Leseauftrags

- Werte für 1054/1056 in `Tab_Energieanlagen` nicht gelesen (Encoding-Abbruch) — bei Bedarf mit `PYTHONIOENCODING=utf-8` wiederholen.
- `EPOS.UI.Daten`-Hülle der Wärmepumpe und `WaermepumpeReiter.razor` nicht im Einzelnen geöffnet, nur über Querverweise erschlossen.
- `Berichtsbilder*.cs`-Schlüssel und `KennzahlenKatalog`-Einträge nicht einzeln aufgelistet, nur als Fundort benannt.
- Keine Zeilenanker innerhalb der Wärmepumpe-Wiki-Seite ermittelt.
