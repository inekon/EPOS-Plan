# KD4 — Energieträgerverwaltung und Leistungspreis-Rechenwirkung (Protokoll)

Etappe KD4 des Konzepts Kostendialoge (Rev. 1.2, § 7, Entscheidungen FK6/FK6a),
umgesetzt 25.08.2026 auf Branch `kostenformulare`.

## Umgesetzt

**Migrationsschritt 40** (`SCHRITT_40_LEISTUNGSPREISREIHE`, Zielstand 40):
`Tab_Preisreihe.ID_Energietraeger` (LONG, nullable) — saisonale Leistungspreis-
Reihen je Träger nach dem Preisreihen-Muster (12 Monatswerte, Auflösung „Monat",
Einheit „EUR/kW/Monat"); NULL = Spotreihe (Bestand). Neue `DbWerte`:
`PREISREIHE_AUFLOESUNG_MONAT`, `PREISREIHE_EINHEIT_EUR_KW_MONAT`. Idempotent
(Zweitlauf „bereits erledigt"); `SQL_CREATE_PREISREIHE` bringt die Spalte bei
Neuanlagen gleich mit.

**Rechenwirkung (FK6)** in `KostenEmissionRechner`:

- `TraegerInfo.PreisLeistung`/`LeistungsModus`/`ReihenSummeJeKW`; Effektivregel
  Projekt (`custom_price_power`) vor Katalog (`price_power`), 0 = nicht gepflegt
  (Befund-D5-Regel); Modus aus `energy_carrier.price_power_modus`.
- Anteil je Nicht-Strom-Träger: JAHR = Satz × kW; MONAT = Satz × kW × 12;
  **Reihe (FK6a) gilt vor dem konstanten Satz**: Σ(12 Monatssätze) × kW
  (Projektreihe vor Stammreihe, jüngstes Jahr —
  `PreisreiheCtrl.ReadTraegerReihe`). Der Stromträger bleibt außen vor: sein
  Leistungspreis ist die Tarifstruktur (Schritt 21, keine zweite Wahrheit).
- Ausweis: `VariantenDaten.EnergieLeistungsanteil` (in den Energiekosten
  ENTHALTEN, getrennt ausgewiesen; null = kein Träger gepflegt).
- **Basis = vorgehaltene Anschlussleistung aus den GERÄTEDATEN**
  (`AnschlussleistungKW`: BHKW (Pel+Ptherm)/η, Kessel Ptherm/η;
  η-Normierung > 1,5 ⇒ ÷100, außerhalb (0;1,5] zählt die Anlage nicht) —
  dokumentierte Abweichung vom § 7.1-Wortlaut „Höchstlast aus dem
  Simulationslauf": Der Gas-Leistungspreis bepreist die vorgehaltene
  Anschlussleistung, und Ergebnis-Zeitreihen werden nicht persistiert.

**Spot-Schutz:** `PreisreiheCtrl.ReadVerfuegbare` filtert auf Spotreihen
(`ID_Energietraeger IS NULL`, Auflösung Stunde/Viertelstunde) — die
Stichtagsregel der Simulation kann keine Monats-/Trägerreihe küren.

**UI:**

- `ucFuelSettings`: Modus-Klappliste (Jahres-/Monatsleistungspreis; schreibt
  `price_power_modus` — der Modus ist Katalogsache je Träger, auch im
  Projektkontext; dokumentierte Zwischenlösung), Knopf „Saisonale Sätze…",
  Statuszeile „Saisonreihe X gepflegt — gilt vor dem Satz"; Einheitentext
  €/(kW·a) bzw. €/(kW·Monat) statt €/⟨Brennstoffeinheit⟩; Strom: Feld
  gesperrt/unsichtbar (has_powerprice=false im Bestand) + Tarifstruktur-Hinweis.
- `Form_LeistungspreisReihe` (NEU, Designer-fähig, App-Design): 12 Monatssätze +
  Jahr; je (Träger, Ebene, Jahr) eine Reihe, „Übernehmen" ersetzt das gleiche
  Jahr; Projektkontext erzeugt die vorgehende Projektreihe.
- `Form_Energietraeger` (NEU, Designer-fähig, App-Design): Trägerliste +
  `ucFuelSettings`; beim Stromträger die beiden K4-Karten „Kostenprofil"
  (nur Projektkontext) und „Spotmarktpreise"; speichert das offene Control bei
  Trägerwechsel/Schließen (Bestandsverhalten des Energie-Reiters).
- Menü Administration → Kosten → **„Energieträgerverwaltung…"** (Katalogkontext;
  Preisfelder dort nur lesend — Katalogpreis-Pflege folgt mit den
  Trägervarianten; Modus, Stammreihen und Spot-Stammimport sind pflegbar).
- **Ä1**: `Form_Kosten` führt nur noch die Reiter Investitions-/Betriebskosten —
  Energie- und Kostenprofil-Reiter programmatisch entfernt (Designer-Datei
  unberührt); Übergangsknopf „Energieträger…" unten rechts bis die
  KD6-Projekteinstiege (§ 3.2) stehen.

## Nachweise (kd4-Smoke, DB-Kopie, 21/21 PASS)

- A: `AnschlussleistungKW(1030, 63)` = 2967,27 kW gegen Handrechnung
  (50+81)/0,94 + (250+290)/0,86 + 2200/1; unbekannter Träger ⇒ 0.
- B: alles NULL ⇒ neutral (kein Anteil, `EnergieLeistungsanteil` null);
  Katalog 25 wirkt; MONAT wird gelesen; Projekt 30 vor Katalog 25; Projekt 0 ⇒
  Rückfall auf Katalog. Handrechnung: 25×2967,27 = 74 181,72 €/a (JAHR) bzw.
  ×12 = 890 180,60 €/a (MONAT).
- C: Stammreihe Σ36 wirkt; Projektreihe Σ48 geht vor (48×2967,27 =
  142 428,90 €/a).
- D: Träger-/Monatsreihen bleiben der Spot-Auswahl unsichtbar.
- E: Gas zeigt Modus+Saison-Knopf; Strom gesperrt mit Tarifstruktur-Hinweis;
  beide Dialoge bauen sich in beiden Kontexten auf; Reihendialog lädt die
  Projektreihe. Regressionen: kd2 23/23, kd3 20/20; Layout-Sweep 119 Formulare,
  0 Befunde. Alle Testdaten wurden zurückgesetzt.

## Bewusst offen

- **Emissionsquellen-Ausnahme je Träger (KL8, § 7.3)** — wartet auf E1/E2 des
  Emissionsfaktoren-Konzepts (zur Abnahme).
- **Trägervarianten „Speichern unter…" + Katalogpreis-Schreibweg** (§ 7.1)
  und die Träger-Übernahme Stamm→Projekt (§ 7.2/§ 8.5).
- Projekt-Einstiege § 3.2 (Anlagendialog „Energiekosten…", Berichte & Kosten)
  — Etappe KD6; bis dahin Übergangsknopf in `Form_Kosten`.
- E2E-Lauf über `KostenEmissionRechner.Berechne` mit Simulationsergebnis: der
  Anteilsblock (8 Zeilen) ist über die Bausteintests + Handrechnung belegt.

---

## Windows-Abnahme 08.09.2026 — EMK‑B‑1: Emissionsfaktor-Katalog, Werte je Träger

**Wortlaut:** „Bei Auswahl Energieträger Strom sind in der Auswahl der Quelle keine für
Energieträger Strom, sondern nur fossile Energieträger. Es sollten Emissionswerte für Strom zur
Auswahl sein → sind ganz unten. Es sollten bei Auswahl elektrische Energie nur diese zur Auswahl
sein, ebenfalls bei fossilen Energieträgern nur jeweils für den ausgewählten. Löschen und
Bearbeiten funktioniert nicht."

**Befund.** `EmissionskatalogCtrl.Werte(art, träger)` lieferte die eigenen Zeilen des Trägers UND
alle trägerlosen Vorlagen (`carrier_id IS NULL` — die Brennstoffvorlagen „BAFA EEW — Klärschlamm,
Klärgas, Deponiegas, Biodiesel", „EBeV — Erdgas brennwertbezogen", „GModG — Biogas …"), sortiert
nach Quelle: Die sechs Stromwerte (BAFA 435, UBA 379/387/442, GModG 100, GEMIS) standen zwischen
und unter den Vorlagen. „Bearbeiten"/„Löschen" waren für AUSGELIEFERTE Werte gesperrt (Konzept:
unveränderlich) — der Knopf schwieg, statt es zu sagen.

**Änderung.** Mit Trägerkontext liefert `Werte` NUR die Zeilen dieses Trägers (geltender Wert
zuerst); die trägerlosen Vorlagen erscheinen nur noch, wenn der Träger selbst keine Werte
führt (Rückfall), und im Verwaltungsmodus ohne Träger wie bisher. Die Knöpfe „Bearbeiten…" und
„Löschen" sind bei markiertem Wert frei; bei einem ausgelieferten Wert erklärt der Klick
(„Ausgelieferte Katalogwerte sind unveränderlich — … Legen Sie einen eigenen Wert an"), eigene
Werte lassen sich ändern und löschen. Hinweistext `EMK_HINWEIS_TRAEGER` angepasst (de/en).
Nachweis: `EmissionskatalogCtrlTests` (neu, 3), `EmissionskatalogDialogTests` angepasst (1).
Sandbox: Kern **2062/2062**, UI **3278/3278**.

---

## Befund N4 (17.09.2026) — der Leistungspreis des Stromträgers rechnet mit

**Wortlaut des Anwenderbefunds (16.09.2026, Berichte & Kosten → Wirtschaftlichkeit):** „Die
Energiekosten sind nicht vollständig. Es wird nur der Arbeitspreis genommen. Der Strompreis setzt
sich aus Arbeitspreis, Leistungspreis und Grundpreis zusammen. Die Effekte der Lastspitzenkappung
gehen so im Vergleich verloren."

**Anwenderentscheid 17.09.2026:** `SP-E-1` = (a) — der Leistungspreis rechnet im **Regelweg** mit;
`SP-E-1-Q1` — die Bezugsgröße ist die **Viertelstundenspitze**.

### N4.1 Befund

Die oben festgehaltene Ausnahme („der Stromträger bleibt außen vor: sein Leistungspreis ist die
Tarifstruktur") trug weiter, als sie gedacht war. Ein Projekt **ohne** aktive Tarifstruktur — der
Normalfall — bezahlte vom Strom nur Arbeits- und Grundpreis. Der Leistungspreis der Trägerkarte
(`custom_price_power` vor `price_power`, Modus `price_power_modus`, Saisonreihe) war gepflegt,
sichtbar und wirkungslos. Ein Leistungspreis traf eine Spitze **nur** im Tarifweg, und der rechnet
mit `StromMatrix.MaxBezugKW` — einem **Stundenmittel**-Maximum.

Die Folge ist die, die der Anwender beschreibt: Eine Speichervariante kappt die Bezugsspitze, aber
kaum die Arbeit. Ohne Leistungspreis unterscheiden sich Stamm und Variante in den Energiekosten
deshalb um fast nichts — der wirtschaftliche Kern der Lastspitzenkappung stand in keiner Zahl des
Vergleichs.

### N4.2 Umsetzung

**Der Rechenweg.** Ohne aktive Tarifstruktur gilt

```
Stromanteil = Arbeit × Arbeitspreis + Grundpreis + Leistungsanteil
Leistungsanteil   Modus JAHR:  Satz [€/(kW·a)]     × Jahresspitze
                  Modus MONAT: Σ₁₂ (Monatsspitze × Satz [€/(kW·Monat)])
                  Saisonreihe geht vor: Σ₁₂ (Monatssatz × Monatsspitze)
```

**Woher die Spitze kommt — Stufe (1) der Rangfolge: die vorhandene Reihe.** Gemessen wurde zuerst,
ob eine persistierte Spitze taugt. Ergebnis: **keine.** `Tab_ErgebnisEnergiebedarf.Strombedarf_Max`
ist die Spitze des **Bedarfs** vor jeder Erzeugung, nicht die des Netzbezugs;
`Tab_ErgebnisStromMatrix.MaxBezugKW` ist ein Stundenmittel und entsteht erst im
Wirtschaftlichkeitslauf selbst. Eine **neue Spalte** (Stufe 3, Schemaschritt 83) schied doppelt
aus: Der Ergebnisexport liest `Tab_Ergebnis*` mit `SELECT *`, eine neue Spalte in
`Tab_ErgebnisEnergiebedarf` fiele als neuer Schlüssel in jede `aggregate.csv` und verlangte eine
neue Referenzbasis — und die Hausregel dieses Protokolls, dass **Ergebnis-Zeitreihen nicht
persistiert werden**, gilt für ihre Ableitungen mit.

Die Spitze entsteht deshalb dort, wo die Reihe ohnehin liegt: `ZeitreihenExtraktor` bildet sie beim
Einsammeln aus `SimulationControl.Rest_Strombedarf_viertelstuendlich` — **derselben Reihe, die der
Speicher kappt** (bei aktivierter Flotte die Flottennetzbilanz, sonst der Rest nach Abzug der
Entladung) — und legt sie als Skalar an den `ZeitreihenSatz` (`Netzbezugsspitze`: Jahresspitze und
zwölf Monatsspitzen, feste Monatsgrenzen 31/28/31/… = 365 Tage). Keine vierzehnte Reihe zu 35 040
Werten, keine Datenbankspalte, kein Schemaschritt.

**Damit die Reihe da ist, wenn sie gebraucht wird**, fragen die drei Sammelwege der Schale
(Wirtschaftlichkeitsreiter, Kapitalwertverlauf, Berichtslauf) jetzt zusätzlich
`KostenEmissionRechner.StromLeistungspreisGepflegt(idProjekt)` — dieselbe Vorbedingung, die
Tarifstruktur und KWKG-Bonus längst stellen.

**Was sich NICHT ändert.** Der Brennstoffzweig rechnet unverändert auf die vorgehaltene
Anschlussleistung aus den Gerätedaten (N4 ändert nur, was für den **Stromträger** gilt, und das
war zuvor: nichts). Tarifstruktur und Rollenmodell ersetzen weiterhin den **ganzen** Stromanteil —
der Leistungsanteil steckt ausdrücklich in `StromkostenNetz` und fällt dort mit heraus, damit es
keine zweite Wahrheit gibt. `Ertrag_Leistungspreis` der Stromspeicherrechnung (fest 0, „AP7")
bleibt unberührt.

**Ohne Spitze kein Anteil — aber kein Schweigen.** Führt ein Lauf keine Zeitreihen, wird der
gepflegte Leistungspreis benannt (`VariantenDaten.LeistungspreisOhneSpitze`, Warnung des
Berichtssammlers) statt still zu entfallen. Dasselbe Muster wie beim Strommix-Rückfall.

**Sichtbar gemacht.** Die Vergleichstabelle der Wirtschaftlichkeit führt die Herleitungszeile
**„Bezugsspitze Strom [kW]"** (`WIRT_ZEILE_BEZUGSSPITZE_STROM`, beide Sprachen) zwischen den
Stromkosten und dem Rest. Sie ist keine Geldzeile: An ihr liest der Anwender, was die Kappung
bewirkt hat. Sie erscheint nur, wenn irgendein Lauf der Gruppe eine Spitze führt, und wird — wie
`KwkgModule` und `Betriebskosten` — **nicht persistiert**: Eine gespeicherte Spitze beschriebe
einen anderen Lauf.

### N4.3 Prüffälle (`EPOS.Kern.Tests/StromLeistungspreisTests`, 11 Fälle)

Grundlage ist Projekt 1026 „Beispiel WP WG 1": Netzbezug 19,08 MWh/a, kein Brennstoffverbrauch,
Arbeitspreis 0,35 €/kWh ⇒ Arbeitsanteil **6 678,00 €/a**. Jede Zahl ist von Hand hergeleitet.

| Fall | Rechnung | Erwartung |
|---|---|---|
| Modus JAHR | 6 678,00 + 60 €/(kW·a) × 40 kW | **9 078,00 €/a**, Leistungsanteil 2 400,00 |
| Modus MONAT | 6 678,00 + 5 €/(kW·Mon) × 12 × 30 kW | **8 478,00 €/a**, Leistungsanteil 1 800,00 |
| Leistungspreis 0 | 6 678,00 | unverändert, `EnergieLeistungsanteil` null |
| ohne Zeitreihen | 6 678,00 | kein Anteil, Träger benannt |
| **Stamm gegen Speichervariante** | (40 − 25) kW × 60 €/(kW·a) | **900,00 €/a** Differenz bei gleicher Arbeit |
| Tarifweg | `Energiekosten − StromkostenNetz` | mit und ohne Leistungspreis **derselbe Rest** |
| Viertelstunde gegen Stundenmittel | 100 kW eine Viertelstunde je Stunde | Spitze 100 kW, Stundenmittel 25 kW |
| Monatsgrenzen | Spitze am 1. März (Viertelstunde 5 664) | dritter Monatswert, alle anderen 0 |
| fremdes Raster | 1 000 Werte | keine Spitze (kein Fantasiewert) |
| Vergleichszeile | Menge mit / ohne Spitze | Zeile erscheint / entfällt |
| Zeilentitel | `WIRT_ZEILE_BEZUGSSPITZE_STROM` | in beiden Ressourcendateien, beide mit „kW" |

**Gegenproben (gefahren und zurückgebaut).** (1) Den neuen Summanden `stromKosten += anteilStrom`
ausgehängt → **4 Fälle rot** (JAHR, MONAT, Kappung, Tarifweg), die übrigen 7 grün. (2) Die
Sichtbarkeitsprüfung der Vergleichszeile auf `false` gesetzt → **1 Fall rot**. Beides
zurückgebaut, danach wieder 11/11.

`EnergiekostenGrundTests` bleibt **ohne Zahländerung** grün: In den Projekten 1026 und 1024 trägt
kein Stromträger einen Leistungspreis (0 = nicht gepflegt), und ohne Zeitreihen gibt es ohnehin
keine Spitze.

### N4.4 Gate

Kern-Filter 0 Fehler / 5 Warnungen (keine neue, Schranke 7), Windows-Schale 0 Fehler / 5 Warnungen
(Bestand), `EPOS.Kern.Tests` 3 141/3 141 (+11), `EPOS.UI.Tests` 4 546/4 546, SpeicherEngine
370/370, KiKern 499/499, SpeicherPlanung 27/28 (1 übersprungen) — **beide Kulturen** —,
`Resource.Designer.cs` neu erzeugt (6 294 → 6 295 Einträge, +345 Zeichen, zweiter Lauf +0),
SqlDialektPrüfer 1 460 Texte / 0 Fundstellen, ChartProben 64 Bilder / 0 Verstöße, Referenzlauf
**5/5 PASS, byte-gleich** gegen `2026-09-16_R8_Heizkessel_Kaskade` (1 656 417 Werte). Kein
Schemaschritt, keine neue Referenzbasis, kein iOS- und kein CI-Lauf.

### N4.5 Abnahmepunkte auf Windows

Projekt mit Speichervariante und gepflegtem Leistungspreis auf der Trägerkarte des Stromträgers:

| Nr. | Was zu prüfen ist |
|---|---|
| `A-SP-W1-1` | Trägerkarte Strom: Leistungspreis und Modus (Jahres-/Monatsleistungspreis) pflegen; Wirtschaftlichkeit rechnen — die Energiekosten steigen um Satz × Bezugsspitze |
| `A-SP-W1-2` | Vergleichstabelle zeigt die Zeile „Bezugsspitze Strom [kW]" — in Stamm- und Variantenspalte, in beiden Sprachen |
| `A-SP-W1-3` | Speichervariante gegen Stamm: kleinere Bezugsspitze, niedrigere Energiekosten; die Differenz entspricht Δ Spitze × Satz |
| `A-SP-W1-4` | Leistungspreis auf 0: Energiekosten und Kapitalwert stehen wieder auf dem Stand ohne Leistungspreis |
| `A-SP-W1-5` | Tarifstruktur aktivieren: die Zeile „Strombezugskosten nach Tarif" ersetzt den Stromanteil; der Leistungsanteil des Regelwegs taucht nicht zusätzlich auf |
| `A-SP-W1-6` | Bericht (Word und Excel) führt dieselbe Zeile und dieselben Zahlen wie der Reiter |

### N4.6 Logbuch-Entwurf (Version beim Anwender offen)

> Der Leistungspreis des Stromträgers geht jetzt in die Energiekosten ein — mit der
> Viertelstunden-Bezugsspitze als Grundlage —, und die Wirtschaftlichkeit weist die Bezugsspitze
> als eigene Zeile aus.

Wiki-Quellen `Programm Dokumentation - Kosten.wiki` und `Programm Dokumentation -
Wirtschaftlichkeit.wiki` sind fortgeschrieben; der Upload läuft gebündelt.
