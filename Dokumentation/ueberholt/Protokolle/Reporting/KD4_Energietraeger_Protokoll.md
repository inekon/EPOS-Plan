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

## Befund N5 (17.09.2026) — „Strompreis Details": der Aufschlagsblock wird zur Zerlegung des Arbeitspreises

Anwenderbefund vom 16.09.2026, Anwenderentscheide vom 17.09.2026: **SP-E-2** = (a) (Zerlegung wie
beim Brennstoff), **SP-E-3** (Felder und Schemaschritt), **SP-E-3-Q1** = (a) (Vorschlagswerte vom
Anwender).

### N5.1 Befund

Der Block „Aufschläge auf den Strombezugspreis" trug fünf Sätze (Netzentgelt, Umlagen,
Stromsteuer, Konzessionsabgabe, Vertrieb), die auf den Arbeitspreis **addiert** wurden — in der
Speichersimulation immer, in der Wirtschaftlichkeit nur bei gesetztem Projektschalter
`Aufschlaege_Anwenden`. Damit gab es **zwei Preiswahrheiten für denselben Strombezug**, und welche
galt, hing vom Rechenweg ab. Dazu kamen drei Dinge, die der Anwender benannt hat: Der Bereich war
nicht einklappbar, der „Gesamtaufschlag" stand als eigenes Eingabefeld neben der Summe seiner
Teile, und der Anteil, der **nicht** zu den Aufschlägen gehört — die Beschaffung —, fehlte ganz.

### N5.2 Umsetzung

**Die Bedeutung.** Die Anteile zerlegen ab hier den Arbeitspreis:

```
Arbeitspreis = Beschaffung + Vertrieb + Arbeitspreis Netz
               + Stromsteuer + Konzessionsabgabe + Umlagen
```

Der Arbeitspreis der Trägerkarte ist die eine Wahrheit; der Block wird mit ihm **verglichen**, nie
zu ihm addiert. Je Leser:

| Leser | Vorher | Ab N5 |
|---|---|---|
| Wirtschaftlichkeit (`RechneAufschlaege`) | Summand auf `e.Energie`, nur bei `Aufschlaege_Anwenden` | **entfallen** — die Anteile stecken im Arbeitspreis |
| Speichersimulation, Preisquelle **Fixpreis** | Arbeitspreis + wirksamer Aufschlag | Arbeitspreis, sonst nichts |
| Speichersimulation, **Spot/Profil** | Reihe + wirksamer Aufschlag | Reihe + Σ aktive Anteile **ohne Beschaffung**, nur bei gesetztem Variantenschalter |
| Speicherauslegung, Quelle **Preisprofil** | Profil + wirksamer Aufschlag | Profil + Σ ohne Beschaffung |
| `KohaerenzPruefung` (Stromsteuer) | Schalter + Modus + Aktiv-Flag | allein Aktiv-Flag und Wert des Anteils |

**Der Modus ist ganz entfallen.** „Kein Aufschlag" / „Gesamtwert" / „aufgeschlüsselt" sagten
zusammen nur, ob und wie viel gerechnet wird. Seit ein ungepflegter Anteil **inaktiv** ist und 0
beiträgt (Leseregel „NULL heißt kein Anteil", dieselbe wie beim Brennstoff), sagt „ein aktiver
Anteil vorhanden" dasselbe — die einfachere Wahrheit. `AufschlagsModus`, `WirksamCtKwh`,
`OverrideCtKwh` und `NichtAufgeschluesselterRestCtKwh` sind aus der Engine verschwunden; geblieben
sind `SummeAktivCtKwh` und das neue `SummeAktivOhneCtKwh(schluessel)`. Die Spalten
`Aufschlag_Modus` und `Aufschlag_Override` bleiben ungelesen im Schema stehen, damit eine ältere
Programmfassung auf derselben Datei nicht auf einen fehlenden Namen läuft.

**Die Maske** (`EPOS.UI/Dialoge/Kosten/StrompreisDetails.razor`, aus `StromAufschlaege.razor`
hervorgegangen): ein aufklappbarer Bereich „Strompreis Details" mit dem Hausmuster ▸/▾
(`.epos-modulparameter-knopf`, `aria-expanded`), **Vorgabe zugeklappt**, die Summe der aktiven
Anteile steht auch eingeklappt im Kopf. Drei Gruppen mit Zwischenüberschrift: *Beschaffung und
Vertrieb*, *Netzentgelte*, *Steuern, Abgaben und Umlagen*. Die Umlagen stehen **einmal** —
entweder als Summenfeld oder, über die Merkspalte „Umlagen aufschlüsseln", als KWKG-,
Offshore- und § 19-StromNEV-Umlage; beides nebeneinander wäre eine Doppelzählung. Unten die
Summe, die **Kohärenzzeile** gegen den Arbeitspreis der Karte, die Restzeile und der Knopf
**„In Arbeitspreis übernehmen"**. Der Knopf *meldet* nur (Callback `InArbeitspreis`); eingetragen
wird der Wert vom Wirt, gespeichert mit „Speichern" — dasselbe Muster wie beim Brennstoffblock.
Die **Beschaffung** wird, solange sie leer ist, als **Rest** vorgeschlagen (Arbeitspreis − Σ
übrige aktive Anteile) — als Knopf, nicht als stiller Feldwert. Das Feld „Gesamtaufschlag" und
der Schalter „Aufschläge in der Wirtschaftlichkeit berücksichtigen" sind weg.

**Schemaschritt 83** (`SchemaKatalog.Schritt83_Strompreisdetails`,
`SchemaMigration.Schritt_83_Strompreisdetails`, Faltung in `StrompreisZerlegung`): neun Spalten an
`energy_project_settings` — `Aufschlag_Beschaffung`, `Aufschlag_KWKG`, `Aufschlag_Offshore`,
`Aufschlag_StromNEV19` je mit `_Aktiv`, dazu `Aufschlag_UmlagenEinzeln` (0/1, `NOT NULL DEFAULT 0`).
Die fünf Bestandsspalten werden nur umbenannt und umgruppiert.

**Die Faltung.** Wer bisher wirksam aufschlug, rechnete ab hier ohne den Aufschlag. Der Schritt
setzt deshalb `Beschaffung := bisheriger wirksamer Arbeitspreis` und hebt den Arbeitspreis um den
bisher wirksamen Aufschlag. Geschrieben wird, **wo die Vorrangkette liest**: in
`energy_project_settings.custom_price_work` **und** in jede Preisversion des (Projekt, Träger) mit
einem Arbeitspreis > 0 — `StromPreisCtrl.ArbeitspreisCtKwh` nimmt zuerst die Historie, eine
Faltung allein in die Projekteinstellung käme bei jedem gepflegten Projekt nie an. Eine Version
mit Arbeitspreis 0 bleibt unberührt (0 heißt „nicht gepflegt", die Kette überspringt sie). Ein
**Gesamtwert** lässt sich nicht in Anteile zerlegen: Er wandert vollständig in den Arbeitspreis
und in die Beschaffung, die Anteilsfelder bleiben als Vorschlag stehen und werden inaktiv, und die
Zeile bekommt eine **Protokollzeile** mit Projekt, Träger und altem Aufschlag — eine erfundene
Zuordnung wäre schlimmer als eine benannte Lücke. Zeilen mit gepflegten Werten **ohne** wirksamen
Aufschlag werden stillgelegt (Aktiv-Schalter auf 0), nicht geleert: Ohne Modus rechneten sie sonst
ab hier mit, obwohl sie es nie taten. Idempotent ist der Schritt über eine gepflegte Beschaffung —
sie gibt es erst ab Schritt 83.

In der Testdatenbank wurden fünf Zeilen gefaltet:

| Projekt / Träger | Arbeitspreis vorher | Aufschlag | Arbeitspreis nachher | Beschaffung |
|---|---|---|---|---|
| 1017 / 54 | 38,000 ct/kWh | 11,746 | 49,746 | 38,000 |
| 1017 / 58 | 32,000 | 11,746 | 43,746 | 32,000 |
| 1019 / 60 | 35,000 | 11,746 | 46,746 | 35,000 |
| 1023 / 60 | 35,000 | 11,746 | 46,746 | 35,000 |
| 1024 / 60 | 35,000 | 11,746 | 46,746 | 35,000 |

Danach ist Σ der aktiven Anteile der neue Arbeitspreis und Σ ohne Beschaffung der alte Aufschlag —
**die Preisreihe jedes Laufs bleibt, wie sie war**. Genau das weist der byte-gleiche Referenzlauf
nach.

**Katalogwerte (SP-E-3-Q1).** Saatgeneration **7** des Gesetzeskatalogs, Stichjahr **2026**,
Quelle „Angabe des Anwenders vom 17.09.2026, Umlagen 2026 laut Veröffentlichung der
Übertragungsnetzbetreiber":

| Anteil | 2025 (Vorjahr, nur hier) | **2026 (Katalogwert)** | Entwicklung |
|---|---|---|---|
| KWKG-Umlage | 0,277 | **0,446** | +61,0 % |
| Offshore-Netzumlage | 0,816 | **0,941** | +15,3 % |
| § 19 StromNEV-Umlage | 1,558 | **1,559** | +0,1 % |
| Summe der Umlagen | 2,651 | **2,946** | +11,1 % |

Die Summe 2,946 ist genau der bisherige Vorschlagswert `UMLAGEN_VORGABE` — die namenlose Klammer
„0,446 + 1,559 + 0,941" des Fachkonzepts hat damit ihre Namen. Dazu eingesät: der reduzierte
Stromsteuersatz `STROMST_REDUZIERT_SATZ` mit **0,50 EUR/MWh** (= 0,050 ct/kWh), **angegeben** und
nicht als Differenz aus Regelsatz und § 9b-Entlastung geraten (Leitentscheidung L4) — damit ist
Restpunkt **S-6** erledigt. Netzentgelt Arbeit 6,440, Stromsteuer 2,050, Konzessionsabgabe 0,110
und Vertrieb 0,200 ct/kWh bleiben, wie sie waren. Restpunkt **C5** (Konstanten im Modell gegen
Katalog) ist über eine Wache erledigt, die beide wertgleich hält; die Konstanten sind damit
ausdrücklich Rückfallebene, nicht Quelle.

### N5.3 Die E5-Restpunkte und „Nach #266"

| Restpunkt | Stand |
|---|---|
| **Vorgabeverhalten** (E5) | **erledigt** — es gibt keinen Modus mehr; ein ungepflegter Anteil ist inaktiv und trägt 0 bei |
| **Aktiv-Flags kein verlässliches Aus** (E5) | **erledigt** — der Aktiv-Schalter ist ab hier die einzige Aussage; ein nie gepflegter Wert steht als Vorschlag im Feld, aber ohne Haken |
| **Doppelzählung Zeile/Energiekosten** (E5) | **erledigt** — die Zeile `WIRT_ZEILE_AUFSCHLAG` ist entfallen, weil es keinen zweiten Summanden mehr gibt |
| **„Nach #266"** (Bestandsprojekte ohne Modus rechnen 0 statt 11,746) | **erledigt** — 0 ist ab hier die richtige und die einzige Antwort: Ein Projekt, an dem niemand etwas eingestellt hat, hat keine Anteile |

### N5.4 Prüffälle

- `EPOS.Kern.Tests/StrompreisZerlegungTests` (11 Fälle): Leseregel (frisches Modell, nie gepflegte
  Zeile, fehlende Zeile), Katalog gegen Rückfallebene, Umlagen einmal, Schreiben/Lesen samt
  Merkspalte, die gefaltete Zeile, und drei Fälle des Schemaschritts — Faltung „aufgeschlüsselt"
  (Arbeitspreis + 11,746, Beschaffung := alter Preis, Wiederholbarkeit), Faltung „Gesamtwert"
  (vollständig in den Arbeitspreis, Anteile inaktiv, Protokollzeile) und die Stilllegung gepflegter
  Werte ohne wirksamen Aufschlag.
- `SpeicherEngine.Tests/PreisModellTests` (Abschnitt Aufschlagssatz, umgeschrieben): Σ = Arbeitspreis,
  Σ ohne Beschaffung = 11,746, die drei Einzelumlagen wertgleich dem Summenfeld, inaktive Anteile,
  unbekannter Schlüssel, ungepflegter Satz.
- `EPOS.UI.Tests/Dialoge/PreisbloeckeTests` (Strom, 11 Fälle): zugeklappt mit Summe im Kopf,
  aufgeklappt drei Gruppen und sechs Anteile, Beschriftungen **deutsch und englisch**, der
  Umlagenschalter tauscht Summenfeld gegen drei Einzelposten, Wertänderung, Stromsteuer-Schnellwahl,
  empfohlener Satz, Rest-Vorschlag erst auf Klick, Summe/Kohärenz/Rest aus der Hülle, der
  Übernahmeknopf meldet und schreibt nichts selbst.

**Gegenproben** (gefahren und zurückgebaut): Übernahmeknopf ausgehängt → 1 rot; Faltung ausgehängt
→ 1 rot.

### N5.5 Gate

Kern-Filter 0 Fehler / 5 Warnungen (keine neue, Schranke 7), Windows-Schale 0 Fehler / 5 Warnungen,
`EPOS.Kern.Tests` 3 153/3 153, `EPOS.UI.Tests` 4 546/4 546, SpeicherEngine 368/368, KiKern 499/499,
SpeicherPlanung 27/28 (1 übersprungen), `Resource.Designer.cs` neu erzeugt (6 296 → 6 294 Einträge,
zweiter Lauf +0), SqlDialektPrüfer 1 468 Texte / 0 Fundstellen, ChartProben 64 Bilder /
0 Verstöße, Referenzlauf **5/5 PASS und byte-gleich vor UND nach dem Datenschritt** (143 CSV)
gegen `2026-09-16_R8_Heizkessel_Kaskade`. Schemastand **82 → 83**, **keine neue Referenzbasis**,
kein iOS- und kein CI-Lauf.

Eine Geldgröße hat sich mit Absicht verschoben und ist im Prüffall festgehalten:
`EnergiekostenGrundTests` rechnet für Projekt 1024 jetzt 188 167,18 € statt 142 696,06 € — der
Aufschlag, der dort bisher nur die Speichersimulation erreichte, steht seit der Faltung im
Arbeitspreis und damit in jeder Rechnung, die ihn liest. Die Simulationsergebnisse sind davon
unberührt.

### N5.6 Abnahmepunkte auf Windows

| Nr. | Was zu prüfen ist |
|---|---|
| `A-SP-W23-1` | Trägerkarte Strom: Der Bereich heißt „Strompreis Details", ist zugeklappt und nennt schon zugeklappt die Summe der Anteile; ▸/▾ klappt ihn auf |
| `A-SP-W23-2` | Drei Gruppen mit ihren Überschriften, sechs Anteile, kein Feld „Gesamtaufschlag", kein Modusumschalter |
| `A-SP-W23-3` | „Umlagen aufschlüsseln" tauscht das Summenfeld gegen KWKG, Offshore und § 19 StromNEV; die Summe bleibt gleich, wenn alle drei aktiv sind |
| `A-SP-W23-4` | Beschaffung leer: Der Knopf „Rest: …" trägt Arbeitspreis − Σ übrige Anteile ein und schaltet den Anteil aktiv; ohne Klick ändert sich nichts |
| `A-SP-W23-5` | „In Arbeitspreis übernehmen" setzt den Arbeitspreis der Karte auf die Summe; gespeichert wird erst mit „Speichern" |
| `A-SP-W23-6` | Wirtschaftlichkeit: Der Schalter „Aufschläge … berücksichtigen" ist weg, die Berichtszeile „Aufschläge auf den Strombezug" erscheint in Word und Excel nicht mehr |
| `A-SP-W23-7` | Migration einer Bestandsdatenbank auf Stand 83: Der Arbeitspreis eines Projekts, das aufgeschlagen hat, steht um den Aufschlag höher, die Beschaffung trägt den alten Preis, und die Speichersimulation liefert dieselben Zahlen wie vorher |

### N5.7 Logbuch-Entwurf (Version beim Anwender offen)

> Der Stromträger hat statt der Aufschläge einen einklappbaren Bereich „Strompreis Details", der
> den Arbeitspreis in Beschaffung, Vertrieb, Netzentgelt, Stromsteuer, Konzessionsabgabe und
> Umlagen zerlegt.
>
> Der ermittelte Preis lässt sich mit einem Knopf in den Arbeitspreis übernehmen.
>
> Der Schalter „Aufschläge in der Wirtschaftlichkeit berücksichtigen" ist nicht mehr vorhanden —
> die Anteile stecken im Arbeitspreis und rechnen damit überall mit.

Wiki-Quellen `Programm Dokumentation - Kosten.wiki` und `Programm Dokumentation -
Wirtschaftlichkeit.wiki` sind fortgeschrieben; der Upload läuft gebündelt.

## Befund N6 (17.09.2026) — die Einspeisevergütung verlässt die Trägerkarte

> Anwenderbefund 17.09.2026 (Kosten → Energieträger, Stromträger): „Die ‚Vergütung für
> eingespeisten Strom' (v_pv, v_bhkw) ist auf der Trägerkarte überflüssig und an der falschen
> Stelle." Anwenderentscheid **SP-E-5 (a)**.

### N6.1 Befund

Der Block trug zwei Sätze, `Verguetung_PV` und `Verguetung_BHKW`, an
`energy_project_settings` je (Projekt, Träger). **Gelesen hat ihn genau ein Rechenweg: die
Speicherwelt** (`StromPreisCtrl.BaueVerguetungen`, von dort in Speicherauslegung,
Flottenstudie und Stromspeichersimulation). Die **Wirtschaftlichkeit hat ihn nie angesehen** —
sie rechnet seit jeher mit `Tab_ProjektWirtschaftlichkeit.Einspeiseverguetung` und
`Einspeiseverguetung_KWK`. Es gab damit zwei Wahrheiten über denselben eingespeisten Strom,
und welche galt, hing vom Rechenweg ab.

Dazu ein stiller Ersatzwert: Die Modellfelder standen auf 5,0 ct/kWh, und diese Vorgabe kam
auch dann zum Zug, wenn das Projekt **gar keine** Zeile in `energy_project_settings` hatte.
Die Speicherprojekte 1007 und 1046 der Testdatenbank haben keine — sie rechneten mit einer
Zahl, die nirgends steht. Denselben stillen Rückfall trugen
`StromspeicherSimCtrl.StandardParameter` (`VerguetungCtKwh = 5,0`) und über ihn der Rückfall
der Flottenstudie (`?? v.Basis.VerguetungCtKwh`).

### N6.2 Umsetzung

**Die Trägerkarte.** Die Gruppe „Vergütung für eingespeisten Strom" ist aus
`StrompreisDetails.razor`, aus dem Bearbeitungsstand (`StromAufschlaegeStand`), aus der Hülle
(`EnergietraegerHuelle`) und aus den Ressourcen entfernt (`PREIS_GRUPPE_VERGUETUNG`,
`PREIS_LABEL_VERGUETUNG_PV`, `PREIS_LABEL_VERGUETUNG_BHKW` in beiden Sprachen). Die
Modellfelder `Verguetung_PV`/`_BHKW` und die beiden Vorgabekonstanten sind fort;
`StromAufschlagCtrl` liest und schreibt die Spalten nicht mehr. **Die Spalten selbst bleiben
stehen** — kein DROP: Eine ältere Programmfassung auf derselben Datei soll nicht auf einen
fehlenden Namen laufen.

**Die neue Quellenkette** steht an genau einer Stelle,
`StromPreisCtrl.VerguetungenBauen` (aufgerufen aus `BaueVerguetungen` und aus `Baue`):

| Größe | Quelle |
|---|---|
| `v_pv` | PV-Vergütungsdialog, wenn aktiv (Entscheid F7, **unverändert**); sonst `Einspeiseverguetung` |
| `v_bhkw` | `Einspeiseverguetung_KWK`, wenn gepflegt; sonst `Einspeiseverguetung` |

**Einheiten.** Die Parameter führen €/kWh (so steht es an
`WirtschaftlichkeitParameter.Einspeiseverguetung` und so rechnet `WirtschaftlichkeitCtrl`
damit), die Speicherwelt ct/kWh. Umgerechnet wird an dieser einen Naht, Faktor 100.

**Kein stiller Rückfall mehr.** Ohne gepflegten Wert rechnet der Lauf mit 0 und sagt es:
`PREIS_HINWEIS_KEINE_VERGUETUNG_PV` bzw. `…_BHKW`, beide Sprachen, über `HinweisErgaenzen`
ins Simulationsprotokoll. Eine gepflegte **0** in `Einspeiseverguetung` gilt als „nicht
gepflegt" — die REAL-Spalte trägt keinen DDL-Default, aber jeder von der Maske angelegte Satz
steht dort auf 0, und eine Einspeisung ohne Erlös ist an dieser Zahl von einem nie
angefassten Feld nicht zu unterscheiden. Beim KWK-Satz ist die Frage nicht zu stellen: Seine
Spalte ist nullbar, und eine gepflegte 0 heißt dort seit jeher „kein eigener KWK-Satz".
`StromspeicherSimCtrl.VERGUETUNG_PV_CT_KWH`/`_BHKW_CT_KWH` (je 5,0) sind durch **eine**
Konstante `VERGUETUNG_OHNE_PFLEGE_CT_KWH = 0,0` ersetzt; damit erfindet auch der Weg über
`StandardParameter` — Flottenstudie und Dashboard-Kachel — keine Vergütung mehr.

**Sichtbarkeit.** Die Maske der Wirtschaftlichkeitsparameter trägt unter „Strom — Einspeisung
und Bezug" eine Herleitungszeile (`WPAR_EINSP_HINWEIS`, beide Sprachen): Die beiden Sätze
bewerten den eingespeisten Strom in der Wirtschaftlichkeit **und** stellen den Verkaufspreis
von Stromspeicher und Speicherflotte; ohne KWK-Satz gilt für BHKW-Strom der PV-Satz; ist gar
nichts gepflegt, rechnet die Speicherwelt mit 0 und weist das aus; der PV-Vergütungsdialog hat
für `v_pv` Vorrang. Am PV-Vergütungsdialog selbst ist nichts geändert.

**Schemaschritt 84** (`VerguetungUmzug`, eine Quelle für Migration,
`Werkzeuge/Testdatenbankschema` und `EPOS.Kern.Tests`): **reiner Datenschritt, keine Spalte.**
Für jedes Projekt mit gepflegtem Kartenwert am Stromträger gilt
`Einspeiseverguetung := Verguetung_PV / 100`, wenn dort nichts (oder 0) steht, und
`Einspeiseverguetung_KWK := Verguetung_BHKW / 100`, wenn die Spalte NULL ist. **Gepflegte
Parameter gewinnen.** Hat ein Projekt keinen Parametersatz, entsteht einer, der **nur** die
Vergütung trägt — jede andere Spalte bleibt NULL und damit beim Vorgabewert der Klasse; eine
abgeschriebene Vorgabeliste wäre die zweite Stelle für dieselben Zahlen. Führt ein Projekt
mehrere Stromträger mit **verschiedenen** Kartenwerten, gewinnt der erste in der Reihenfolge
der Träger-Id und das Protokoll sagt es; ein Mittelwert wäre eine erfundene Zahl. Der Schritt
ist wiederholbar: Nach dem Lauf liefert `ZaehlungUmzug()` 0.

**Testdatenbank auf Stand 84** — vier Projekte, alle mit Kartenwert 5,0/5,0 ct/kWh:

| Projekt | vorher | nachher |
|---|---|---|
| 1017 (Träger 54 und 58) | kein Parametersatz | Satz angelegt, 0,05 / 0,05 €/kWh |
| 1019 (Träger 60) | Satz mit `Einspeiseverguetung = 0`, KWK NULL | 0,05 / 0,05 €/kWh |
| 1023 (Träger 60) | kein Parametersatz | Satz angelegt, 0,05 / 0,05 €/kWh |
| 1024 (Träger 60) | kein Parametersatz | Satz angelegt, 0,05 / 0,05 €/kWh |
| 1030 | Satz mit 0, KWK NULL, **kein Kartenwert** | unverändert 0 / NULL |

Die Kartenspalten stehen unverändert da. Die Dateigröße bleibt gleich (70 770 688 Byte).

### N6.3 Prüffälle (`EPOS.Kern.Tests/VerguetungUmzugTests`, 9 Fälle)

| Fall | Was er festhält |
|---|---|
| `Ohne_Parametersatz_legt_der_Schritt_einen_an` | 1017 ohne Satz → 0,05/0,05 €/kWh; Zins, Betrachtungszeitraum und Vbh-Kontingent bleiben beim Vorgabewert; zweiter Lauf zählt 0 und schreibt nichts |
| `Eine_gepflegte_Null_gilt_als_nicht_gepflegt` | 1019 mit 0/NULL → 0,05/0,05 €/kWh, Satz „ergaenzt" |
| `Ein_gepflegter_Parameter_gewinnt` | 0,0912/0,1234 €/kWh überleben den Schritt unverändert |
| `Ohne_Kartenwert_bleibt_der_Parametersatz_wie_er_ist` | 1030 bleibt bei 0 / NULL |
| `Die_Verguetung_kommt_aus_den_Parametern_in_ct_je_kWh` | 0,05 €/kWh → `v_pv` = `v_bhkw` = 5,0 ct/kWh, kein Hinweis |
| `Der_KWK_Satz_fuehrt_v_bhkw_sonst_gilt_der_allgemeine` | 0,08/0,12 → 8,0/12,0; 0,08/NULL → 8,0/8,0 |
| `Ohne_gepflegte_Verguetung_null_und_benannter_Hinweis` (2×) | 0/NULL → 0,0/0,0 **und** der Hinweistext, `de-DE` und `en-US` |
| `Der_Rueckfallwert_der_Speicherparameter_ist_null` | `VERGUETUNG_OHNE_PFLEGE_CT_KWH` und `StandardParameter(…).VerguetungCtKwh` stehen auf 0 |

Dazu in `EPOS.UI.Tests/PreisbloeckeTests`: `Die_Verguetungsgruppe_steht_nicht_mehr_auf_der_Traegerkarte`
(weder Gruppentitel noch `v_pv`/`v_bhkw` im Markup, zugeklappt wie aufgeklappt; die drei
Preisblöcke stehen unverändert), die beiden Feldzählungen von 2 → 0 und 8 → 6 nachgezogen.

**Gegenprobe** gefahren und zurückgebaut: Datenschritt ausgehängt (das Schreiben in
`VerguetungUmzug.Umziehen` entfernt) → `Ohne_Parametersatz_legt_der_Schritt_einen_an` und
`Eine_gepflegte_Null_gilt_als_nicht_gepflegt` **rot**, die übrigen sieben grün; wieder
eingehängt → 9/9 grün.

**Was die Gegenprobe NICHT zeigt, und das ist der wichtigste Befund dieses Auftrags:** Der
**Referenzlauf taugt hier nicht als Wächter**. Gemessen wurde es vor jeder Änderung — beide
Vergütungen fest auf 0 verdrahtet, Lauf über 1030, 1007, 1017, 1045, 1046: **kein einziges
Byte** der 143 CSV ändert sich. Dasselbe Bild liefert der Lauf gegen die **nicht** migrierte
Datenbank (Stand 83) mit dem neuen Code. Die fünf CI-Projekte hängen an `v_pv`/`v_bhkw`
überhaupt nicht: Ihre Speicherbetriebsarten treffen keine Entscheidung am Verkaufspreis, und
die Basis führt keinen Geldskalar. Byte-Gleichheit ist damit hier **kein** Nachweis, dass der
Umzug gelungen ist — das sind die Prüffälle oben.

### N6.4 Gate

Kern-Filter 0 Fehler / 5 Warnungen (keine neue, Schranke 7), Windows-Schale
(`-p:EnableWindowsTargeting=true`) 0 Fehler / 5 Warnungen (Bestand), `EPOS.Kern.Tests`
3 162/3 162 (+9), `EPOS.UI.Tests` 4 546/4 546, SpeicherEngine 368/368, KiKern 499/499,
SpeicherPlanung 27/28 (1 übersprungen), `Resource.Designer.cs` neu erzeugt (6 294 → 6 294
Einträge, drei Schlüssel raus und drei herein, zweiter Lauf +0), SqlDialektPrüfer 1 473 Texte
/ 0 Fundstellen, ChartProben 64 Bilder / 0 Verstöße, Referenzlauf **5/5 PASS und byte-gleich
vor UND nach dem Datenschritt** (143 CSV, 1 656 417 Werte) gegen
`2026-09-16_R8_Heizkessel_Kaskade`. Schemastand **83 → 84**, **keine neue Referenzbasis**,
kein iOS- und kein CI-Lauf.

Keine Geldgröße verschiebt sich: Die Wirtschaftlichkeit las die Kartenwerte nie, und die
Speicherwelt liest dieselbe Zahl von der neuen Stelle.

### N6.5 Abnahmepunkte auf Windows

| Nr. | Was zu prüfen ist |
|---|---|
| `A-SP-E5-1` | Trägerkarte Strom: Die Gruppe „Vergütung für eingespeisten Strom" ist weg; „Strompreis Details" steht unverändert mit drei Gruppen und sechs Anteilen da |
| `A-SP-E5-2` | Wirtschaftlichkeits-Parameter, Gruppe „Strom — Einspeisung und Bezug": Unter den beiden Feldern steht die Hinweiszeile, dass sie auch den Verkaufspreis der Speicherwelt stellen — deutsch und englisch |
| `A-SP-E5-3` | Einspeisevergütung PV auf 0,08 €/kWh, KWK leer: Die Stromspeichersimulation rechnet mit v_pv = v_bhkw = 8 ct/kWh; mit KWK 0,12 €/kWh rechnet v_bhkw mit 12 ct/kWh |
| `A-SP-E5-4` | Beide Felder auf 0 bzw. leer: Die Simulation rechnet mit 0 und schreibt den Hinweis „… ist keine Einspeisevergütung gepflegt …" ins Protokoll |
| `A-SP-E5-5` | PV-Vergütungsdialog aktiv: Er führt v_pv weiterhin (Protokollzeile „PV-Vergütungsdialog führt die Einspeisevergütung"); v_bhkw kommt weiter aus den Parametern |
| `A-SP-E5-6` | Migration einer Bestandsdatenbank auf Stand 84: Ein Projekt mit gepflegtem Kartenwert 5 ct/kWh trägt danach 0,05 €/kWh in den Parametern; ein Projekt mit schon gepflegtem Parameter behält seinen Wert; der zweite Migrationslauf ändert nichts |

### N6.6 Logbuch-Entwurf (Version beim Anwender offen)

> Die Vergütung für eingespeisten Strom wird nur noch bei den Wirtschaftlichkeits-Parametern
> gepflegt; von dort rechnen auch Stromspeicher und Speicherflotte mit ihr.


## Aufräumen nach N4–N6 (17.09.2026) — die Zerlegung heißt so, die toten Spalten fallen weg

> Auftrag des Orchestrators (`AR-SP`), gebündelte Nebenbefunde der Aufträge #313 und #314;
> Regel „Was seine Aufgabe erfüllt hat, wird entfernt" (CLAUDE.md, Aufräumen).

### AR.1 Befund

Die Welle #312–#314 hat zwei Reste hinterlassen.

**Erstens Namen, die etwas anderes sagen als der Gegenstand.** Seit SP‑E‑2 **zerlegen** die
Preisanteile den Arbeitspreis, statt auf ihn zu kommen — die Typen hießen aber weiter
„Aufschlag": `Aufschlagssatz`, `Aufschlagskomponente`, `StromAufschlagCtrl`,
`StromAufschlagModel`, `StromAufschlaegeStand`, `AlsAufschlagssatz`. Ein Name, der das
Gegenteil dessen behauptet, was die Klasse tut, ist die teuerste Sorte Altlast: Der nächste
Leser glaubt ihm.

**Zweitens fünf Spalten ohne Leser.** Schritt 83 hat den Modus abgeschafft und den
Gesamtwert in den Arbeitspreis gefaltet, Schritt 84 die Vergütung umgezogen, und beide haben
ihre Quellspalten mit Absicht stehen lassen — damit eine ältere Programmfassung auf derselben
Datei nicht auf einen fehlenden Namen läuft. Diese Schonfrist ist mit der Auslieferung der
Welle vorbei. Was bleibt, ist eine zweite, tote Wahrheit:

| Spalte | Wer las sie | Seit wann nicht mehr |
|---|---|---|
| `energy_project_settings.Aufschlag_Modus` | der Modus der Zerlegung | Schritt 83 — es gibt keinen Modus mehr |
| `energy_project_settings.Aufschlag_Override` | der Gesamtwert | Schritt 83 — in den Arbeitspreis gefaltet |
| `energy_project_settings.Verguetung_PV` | die Speicherwelt | Schritt 84 — in die Parameter umgezogen |
| `energy_project_settings.Verguetung_BHKW` | die Speicherwelt | Schritt 84 — in die Parameter umgezogen |
| `Tab_ProjektWirtschaftlichkeit.Aufschlaege_Anwenden` | `RechneAufschlaege` | Schritt 83 — es gibt nichts an- oder abzuschalten |

Dazu drei kleinere Reste: überholte Kommentare, die von einem „Standardwert" der Vergütung
sprechen (der Rückfall ist seit N6 **0**), Papiere unter `Dokumentation/aktuell/`, die
`AufschlagBetrag`/`WirksamCtKwh` noch als Rechenweg führen, und ein Variantenschalter, dessen
Beschriftung („Aufschläge des Kostenmoduls anwenden") nicht mehr sagt, was er tut.

### AR.2 Umbenennung

| alt | neu |
|---|---|
| `SpeicherEngine/Aufschlagsmodell.cs` | `SpeicherEngine/Preiszerlegung.cs` (`git mv`) |
| `Aufschlagskomponente` | `Preisanteil` |
| `Aufschlagssatz` | `Preiszerlegung` |
| `EPOS.Kern/Controller/StromAufschlagCtrl.cs` | `EPOS.Kern/Controller/StrompreisZerlegungCtrl.cs` (`git mv`) |
| `StromAufschlagCtrl` | `StrompreisZerlegungCtrl` |
| `EPOS.Kern/Model/StromAufschlagModel.cs` | `EPOS.Kern/Model/StrompreisZerlegungModel.cs` (`git mv`) |
| `StromAufschlagModel` | `StrompreisZerlegungModel` |
| `AlsAufschlagssatz` | `AlsPreiszerlegung` |
| `StromAufschlaegeStand` | `StrompreisDetailsStand` |

27 Dateien, **reine Bezeichnerersetzung** — kein Rechenweg, keine Signatur, keine
Sichtbarkeit geändert. Die Umbenennung fasst `EPOS.UI.Daten/Kosten/EnergietraegerHuelle.cs`
mit an; anders ist ein Typname nicht durchgängig zu wechseln, und mehr als die
Bezeichnerersetzung steht dort nicht.

**Was „Aufschlag" bleibt, und warum.** Bei der Spot- und Profilreihe wird tatsächlich
aufgeschlagen: Die Reihe **ist** die Beschaffung, die übrigen aktiven Anteile kommen darauf.
Deshalb behalten `AufschlagCtKwh` am Preisergebnis, `MitAufschlag` in `PreisModell`,
`profilAufschlagCtKwh` in der Speicherauslegung, `Netzladeaufschlag`, `PpaSpotAufschlag` und
`Tarifaufschlag` ihr Wort. Ebenso bleiben die **Spaltennamen** `Aufschlag_*` (kein Umbenennen
im Schema) und die Ressourcenschlüssel `PREIS_*` — ihre Bedeutung stimmt. Und die
Kommentare der Migrationsklasse `StrompreisZerlegung` (Schritt 83) sprechen weiter vom
„Aufschlag": Sie beschreiben den Bestand, den der Schritt vorfindet.

### AR.3 Schemaschritt 85 — `StrompreisAltspalten`

Fünf `ALTER TABLE … DROP COLUMN`, **kein DML**, **kein Tabellenneubau**.

**Warum DROP COLUMN reicht.** SQLite kann das seit 3.35. Es verweigert den Dienst, wenn die
Spalte Primärschlüssel ist, unter einem Index oder einer UNIQUE-Bedingung steht, in einer
**Tabellen**-CHECK-Bedingung, einer generierten Spalte, einem Trigger oder einer Sicht
vorkommt. Gemessen an der Testdatenbank trifft nichts davon zu: Die drei Indizes über
`energy_project_settings` liegen auf `ID_Energieträger`, `ID_Projekt` und `ID_Umrechnung`,
der eindeutige auf `(ID_Projekt, ID_Energieträger)`; `Tab_ProjektWirtschaftlichkeit` führt
einen eindeutigen Index auf `ID_Projekt`. Die CHECK-Bedingungen von `Aufschlag_Modus`
(Textlänge ≤ 50) und `Aufschlaege_Anwenden` (`IN (0,1)`) sind **Spalten**bedingungen und
fallen mit ihrer Spalte — an einer STRICT-Kopie mit denselben Bauformen gegengeprüft. Beide
Tabellen sind STRICT; das ändert daran nichts.

**Eine Quelle, drei Leser** — wortgleich zum Muster von `HeizstabJeWaermepumpe` (Schritt 79,
dem einzigen anderen Entfernungsschritt des SQLite-Zweigs): `StrompreisAltspalten` im Kern
hält Namen und Anweisungen, `SchemaMigration.Schritt_85_StrompreisAltspalten`,
`Werkzeuge/Testdatenbankschema` und `EPOS.Kern.Tests/TestDatenbank` bedienen sich daraus.
Der Spaltenname reist als **Argument** in die Bauweise, weil die Anweisung einen Namen nennt,
den es nach dem Schritt nicht mehr gibt — sonst hielte ihr der `SqlDialektPruefer` die
migrierte Messlatte entgegen.

**Die Konstanten sind aus `SchemaKatalog` ausgezogen.** Der Katalog beschreibt die Spalten,
die es **gibt**; die fünf Namen hält ab hier der Schritt, der sie wegnimmt. `Schritt12_Preismodell`,
`StrompreisZerlegung` (83) und `VerguetungUmzug` (84) lesen sie von dort — die Kette legt sie
an, liest sie, und Schritt 85 nimmt sie weg.

**`sql/schema/*` bleibt unberührt.** Diese Dateien beschreiben den eingefrorenen Quellstand 61
(„NICHT VON HAND AENDERN"), nicht den Zielstand; `WP_Heizstab` steht dort seit Schritt 79
ebenso weiter drin. Geprüft, was die Nachbarn taten — sie haben sie nicht angefasst.

**Idempotenz.** `StrompreisAltspalten.Anweisungen` gibt nur Anweisungen für Spalten heraus,
die noch stehen. Nach dem Lauf ist `Offen()` = 0, und ein zweiter Lauf fasst nichts an.

### AR.4 Kommentar- und Papierstellen

| Stelle | was |
|---|---|
| `SpeicherEngine/SpeicherParameter.cs`, `SpeicherEingang.cs` | „Standardwert" → **Rückfallwert**, ohne gepflegte Vergütung **0**, Quelle Wirtschaftlichkeitsparameter (`StromPreisCtrl.VerguetungenBauen`) |
| `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitDaten.cs`, `WirtschaftlichkeitCtrl.cs` | „Spalte bleibt ungelesen stehen" → mit Schritt 85 entfallen |
| `EPOS.Kern/Model/StromspeicherVarianteModel.cs`, `SchemaKatalog` | der Variantenschalter gilt nur für Spot und Kostenprofil; beim Fixpreis wirkt er nicht |
| `Dokumentation/aktuell/.../Rechenweg/04_Energiekosten.md` | Formelblock ohne `AufschlagBetrag`; Befund **N3 erledigt** |
| `Dokumentation/aktuell/.../Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` | Aufschlagsblock → Zerlegung; Vergütungssätze auf die Parameter umgeschrieben; Trägerdialog-Zeile auf „Strompreis Details"; N3 erledigt; Doppelwahrheit Stromsteuer auf den neuen Typnamen |
| `Dokumentation/aktuell/Konzept_Stromspeicher_EPOS-Plan.md` | `Aufschlag_Modus`/`_Override` **entfallen** statt „bleiben stehen"; `StrompreisZerlegungModel`; „Aufschlagskomponenten" → „Preisanteile" |
| `Referenzlaeufe/LIESMICH.md` | Schemastand **85**, Schritt 85 benannt und als ergebnisneutral begründet |

`VERGUETUNG_PV_VORGABE` gibt es nicht mehr; `AufschlagsModus` steht nur noch in der
Statusdatei (dort als Geschichte richtig), `ucStromAufschlaege` nur noch als Verweis auf die
abgelöste WinForms-Maske. `Dokumentation/ueberholt/` ist Geschichte und bleibt unberührt.

### AR.5 Variantenschalter

`Tab_StromspeicherVariante.Aufschlag_Anwenden` gilt seit SP‑E‑2 nur noch für die Preisquellen
**Spotmarkt** und **Kostenprofil** (`StromPreisCtrl`: `reiheIstBeschaffung && v.Aufschlag_Anwenden`).
Die Beschriftung heißt deshalb jetzt, was sie tut:

| | alt | neu |
|---|---|---|
| `PREIS_PARAM_CHK_AUFSCHLAG` (de) | Aufschlaege des Kostenmoduls anwenden | **Anteile auf Spot-/Profilpreis aufschlagen** |
| `PREIS_PARAM_CHK_AUFSCHLAG` (en) | Apply the surcharges from the cost module | **Add price components to the spot/profile price** |

`PREIS_PARAM_HINWEIS_AUFSCHLAG` ist **entfallen**: Der `Schalter`-Baustein kennt keinen
Hinweistext, der Schlüssel wurde von niemandem gelesen. Die Spalte selbst bleibt — sie trägt
eine Anwenderangabe. Konzept Stromspeicher 4.1 a/b nennt den Schalter bereits richtig; in den
Wiki-Quellen kommt er nicht vor.

### AR.6 Nachweis

**Schemafall** (Arbeitskopie der Testdatenbank, 84 → 85):

| | vorher | nachher |
|---|---|---|
| Schemaversion | 84 | **85** |
| Tabellen | 120 | 120 |
| Spalten `energy_project_settings` | 44 | **40** (weg: Aufschlag_Modus, Aufschlag_Override, Verguetung_PV, Verguetung_BHKW; neu: keine) |
| Spalten `Tab_ProjektWirtschaftlichkeit` | 51 | **50** (weg: Aufschlaege_Anwenden) |
| Zeilen, alle 120 Tabellen | 1 602 377 | 1 602 377, **keine Tabelle abweichend** |
| SHA-256 über alle übrigen Felder beider Tabellen | — | **gleich** |

Der **zweite Lauf** liefert einen byte-identischen Schnappschuss: nichts angefasst.

**Prüfstände.** 4 neue Fälle (`StrompreisAltspaltenTests`: der Schritt entfernt genau die
fünf, ohne Zeile oder Nachbarspalte anzufassen; auf dem Zielstand nichts zu tun; fünf
DROP COLUMN in fester Reihenfolge ohne DML; Zielversion 85). Die Nachweise der Schritte 83
und 84 stellen die Altspalten auf ihrer **eigenen** Arbeitskopie wieder her
(`TestDatenbank.AltspaltenStrompreisWiederherstellen`) — sie prüfen den Bestandsstand, und zu
dem gehörten die Spalten. Keine Zahl eines bestehenden Falls hat sich geändert; kein Fall ist
verloren gegangen (`EPOS.Kern.Tests` 3 162 → 3 166, alle anderen Projekte gleich).

**Gate** (beide Kulturen, normal und `LC_ALL=en_US.UTF-8`):

| | Ergebnis |
|---|---|
| Kern-Filter (Release) | 0 Fehler / **5 Warnungen** (Ausgangslage 5, Schranke 7) |
| Windows-Schale (`EnableWindowsTargeting=true`) | 0 Fehler / 5 Warnungen (Bestand) |
| `EPOS.Kern.Tests` | 3 166 / 3 166 |
| `EPOS.UI.Tests` | 4 546 / 4 546 |
| SpeicherEngine / KiKern / SpeicherPlanung | 368 / 499 / 27 von 28 (1 übersprungen) |
| `Werkzeuge/Formularkarte.Tests` | 122 / 122 |
| `SqlDialektPruefer` | 1 474 Texte, **0 Fundstellen** |
| ChartProben | 64 Bilder, 0 Verstöße |
| `Resource.Designer.cs` | 6 294 → **6 293** Einträge, zweiter Lauf **+0** |
| Referenzlauf gegen `2026-09-16_R8_Heizkessel_Kaskade` | **5/5 PASS**, 143 CSV, 1 656 417 Werte — und **byte-gleich vor und nach** dem Schemaschritt |

Keine neue Referenzbasis. Kein Push, kein CI- und kein iOS-Lauf.

### AR.7 Abnahmepunkte

| | |
|---|---|
| `A-AR-SP-1` | Stromspeicher → Parameter, Preisquelle **Spotmarkt** oder **Kostenprofil**: Der Schalter heißt „Anteile auf Spot-/Profilpreis aufschlagen" (englisch „Add price components to the spot/profile price"); ein- und ausgeschaltet ändert sich der ausgewiesene Bezugspreis um die Summe der aktiven Anteile ohne Beschaffung |
| `A-AR-SP-2` | Dieselbe Seite mit Preisquelle **Fixpreis**: Der Schalter ändert am Bezugspreis nichts — dort zerlegen die Anteile den Arbeitspreis |
| `A-AR-SP-3` | Kosten → Energieträger, Stromträger: „Strompreis Details" öffnet, rechnet und übernimmt wie zuvor; Speichern und Wiederöffnen halten jeden Wert |
| `A-AR-SP-4` | Eine Bestandsdatenbank vom Stand 84 einmal starten: Die Migration meldet Schritt 85 mit fünf entfernten Spalten, ein zweiter Start meldet nichts mehr, und alle Zahlen des Projekts stehen unverändert |

### AR.8 Logbuch-Entwurf (Version beim Anwender offen)

> Der Schalter der Speichervariante heißt jetzt „Anteile auf Spot-/Profilpreis aufschlagen"
> und wirkt nur bei den Preisquellen Spotmarkt und Kostenprofil.

## Befund N7 (17.09.2026) — eine Zerlegung für beide Träger: Brennstoff ohne Modus

Anwenderentscheid 17.09.2026 auf die Fachfrage `Anteil_Modus`: **(a) Modus abschaffen**,
wie beim Strom. Dazu zwei Reste aus den Aufräumwellen.

### N7.1 Befund

`BrennstoffBestandteile.razor` führte eine Radiogruppe „Gesamtwert (Arbeitspreis gilt)" /
„aufgeschlüsselt (Summe ist der Preis)". **Sie hat nie gerechnet.** Der Kommentar des
Controllers sagte es bereits: „welcher Modus gewählt ist, entscheidet allein die Anzeige";
der Engine-Satz kennt seit SP-E-2 keinen Modus mehr, und in beiden Stellungen war
`SummeAktivCtKwh` die Zahl, die zählt. Wirksam waren genau drei Unterschiede, alle in der
Maske:

| Stellung | Summenzeile | Restzeile | Knopf |
|---|---|---|---|
| Gesamtwert | „Summe der aktiven Bestandteile: …" | „Nicht aufgeschlüsselter Rest: …", negativ in Warnfarbe | **gesperrt** |
| aufgeschlüsselt | „Preis aus den Bestandteilen: …" | Satz über den Modus statt einer Zahl | bedienbar |

Der gesperrte Knopf war dabei die Falle: Wer die Summe in den Arbeitspreis übernehmen
wollte, musste erst einen Modus wählen, der nichts bewirkt.

Die Spalte `energy_project_settings.Anteil_Modus` (Schritt 60) trug den Wert. Gelesen wurde
er in `BrennstoffBestandteilCtrl.Read`, geschrieben in `Update`, übersetzt in
`EnergietraegerHuelle.AusBrennstoffModell`/`InBrennstoffModell`.

### N7.2 Umsetzung — der Modus fällt

- **Maske** (`EPOS.UI/Dialoge/Kosten/BrennstoffBestandteile.razor`): Radiogruppe fort, die
  Parameter `ModusGesamtwert`/`ModusAufgeschluesselt` fort, der Knopf „In Arbeitspreis
  übernehmen" ist immer bedienbar und MELDET nur — eingetragen wird der Wert vom Wirt.
  Summenzeile, Kohärenzzeile und Restzeile stehen wie beim Strom, der negative Rest in
  Warnfarbe.
- **Stand** (`BrennstoffBestandteileStand`): `Aufgeschluesselt` entfallen.
- **Modell** (`BrennstoffBestandteilModel`): Feld `Modus` entfallen.
- **Controller** (`BrennstoffBestandteilCtrl`): `Read` liest `Anteil_Modus` nicht mehr,
  `Update` schreibt sie nicht mehr. **Die Spalte bleibt im Schema stehen** — kein
  Schemaschritt in dieser Aufgabe; sie ist als Aufräumkandidat vermerkt.
- **Hülle** (`EnergietraegerHuelle`): `AusBrennstoffModell`/`InBrennstoffModell` ohne Modus;
  die vier Textschlüssel des Modus sind aus `BestandteilTexte` entfallen.
- **Ressourcen:** `BB_MODUS_GESAMTWERT`, `BB_MODUS_AUFGESCHLUESSELT`,
  `BB_PREIS_AUS_BESTANDTEILEN` und `BB_REST_HINWEIS_MODUS` sind aus beiden Sprachen und dem
  erzeugten Designer entfernt.

`DbWerte.SP_AUFSCHLAG_MODUS_*` **bleiben**: Schemaschritt 83 (`StrompreisZerlegung`) liest
den Bestandsstand der Strom-Altspalte `Aufschlag_Modus` und braucht die drei Werte für die
Faltung. Der Kopfkommentar der Gruppe in `DbWerte` sagt das jetzt auch — er sprach vom
„Modus des Aufschlagsblocks" als wäre er in Betrieb.

### N7.3 Ein Bauplan — was doppelt war

Gemessen wurde vorher, was die beiden Controller wirklich zweimal führten:

| Doppelt | Stelle |
|---|---|
| Vorsorge (Schema je Tabelle lesen, `ALTER TABLE ADD COLUMN`, Protokoll statt Dialog) | je ~45 Zeilen in beiden `StelleSpaltenSicher` |
| SET-Fragment `[x] = ?, [x_Aktiv] = ?, ` | `Feld(spalte)`, wortgleich in beiden |
| `null` → `DBNull` als Zahlenparameter | `Wert(name, wert)` im Brennstoff, an neun Stellen von Hand im Strom |
| Lesen eines (Wert, Aktiv)-Paares | `Komponente` / `Bestandteil` — **fachlich verschieden**, aber mit derselben Aktiv-Spalten-Wache |
| Summe und Rest gegen den Arbeitspreis | `Ansicht()` der Hülle, zweimal dieselben drei Zeilen |

Zusammengezogen in `EPOS.Kern/Controller/Preisanteile.cs` (statische Klasse, keine
Basisklasse — die Controller teilen keinen Zustand):

- `SpaltenSicherstellen(herkunft, spalten)` — eine Vorsorge für beide, Schema je Tabelle
  einmal gelesen, ohne Dialog.
- `Paar(...)` und `PaarNullbar(...)` — die zwei Spielarten des Lesens. Sie bleiben zwei,
  weil sie fachlich zwei sind: Beim Strom lässt NULL den Vorschlag des Modells stehen
  (inaktiv), beim Brennstoff bleibt NULL `null` („kein Anteil erfasst", E5-Falle). Die
  gemeinsame Aktiv-Spalten-Wache steht einmal.
- `SetzPaar(spalte)`, `Wert(name, wert)`, `Aktiv(name, wert)` — die Schreibseite.
- `SummeCtKwh(satz)` und `RestCtKwh(arbeitspreis, satz)` — **die eine Stelle**, aus der
  beide Träger Summe und Rest beziehen.

Die Hülle hat dazu `Preisblock(satz, arbeitspreis, vorlageSumme, vorlageRest)`: Beide
Blöcke bauen ihre `PreisblockAnzeige` jetzt aus einem Aufruf, mit ihren eigenen Texten.

**Nicht** zusammengezogen wurden Spaltennamen, Komponentenlisten und der Zuschnitt des
Satzes (Umlagen einzeln oder als Summe) — das ist je Träger verschieden. Die Razor-Blöcke
bleiben zwei: verschiedene Gruppen, verschiedene Texte, beim Strom einklappbar.

### N7.4 Rest 1 — der Hinweis nennt Knöpfe, die es gibt

`BK_KOSTEN_ANLAGE_OHNE_POSITIONEN` nannte „Kosten bearbeiten…" — einen Knopf, den kein
Erzeugerdialog mehr führt. Neu in beiden Sprachen: „Investitionskosten…" und
„Betriebskosten…" (wortgleich mit `KDLG_KNOPF_INVEST`/`KDLG_KNOPF_BETRIEB`).
`KostenSeiteGaben.cs` trägt denselben Text als Rückfall und einen Kommentar, der auf die
beiden Schlüssel zeigt.

### N7.5 Rest 2 — die Dialogseite heißt wie die Sache

Reine Bezeichnerersetzung nach dem Muster des Aufräumschritts: `Stand.Aufschlaege` →
`Stand.Zerlegung`, `AufschlagAnzeige` → `ZerlegungAnzeige`, `AufschlagTexte` →
`ZerlegungTexte`, `_aufschlagModell` → `_zerlegungModell`. Betroffen:
`EnergietraegerDaten.cs`, `EnergietraegerEinstellungen.razor`, `EnergietraegerDialog.razor`,
`EnergietraegerHuelle.cs` und ein Prüffall. Die Spaltennamen `Aufschlag_*` und die
Ressourcenschlüssel `PREIS_*` bleiben, wo sie sind.

### N7.6 Prüffälle

`EPOS.Kern.Tests/PreisanteileTests` (10 Fälle):

| Fall | Was er hält |
|---|---|
| `Summe_und_Rest_des_Brennstoffs_kommen_aus_dem_gemeinsamen_Bauplan` | 1,65 ct/kWh aus zwei aktiven Anteilen, Rest 4,79 gegen 6,44; ein negativer Rest wird nicht abgeschnitten |
| `Summe_und_Rest_des_Stroms_kommen_aus_demselben_Bauplan` | derselbe Weg für den Strom |
| `Gleiche_Betraege_ergeben_bei_beiden_Traegern_dieselbe_Summe_und_denselben_Rest` | die Zusage des Bauplans — und der Hebel der Gegenprobe |
| `Das_SET_Fragment_ist_fuer_beide_Traeger_dasselbe` | `[Anteil_CO2] = ?, [Anteil_CO2_Aktiv] = ?, ` und dasselbe für `Aufschlag_Netzentgelt` |
| `Ein_nicht_erfasster_Anteil_wird_DBNull_und_nicht_null_Komma_null` | `null` → `DBNull`, eine erfasste 0 bleibt 0 |
| `Das_Brennstoffmodell_fuehrt_kein_Modusfeld_mehr` | weder Feld noch Eigenschaft `Modus` |
| `Die_Brennstoffanteile_gehen_ohne_Modus_hin_und_zurueck` | Schreiben und Lesen an der Testdatenbank (Projekt 1030, Gas 63); `null` bleibt `null`, ein aktiver Schalter ohne Wert bleibt aktiv |
| `Ein_Bestandssatz_mit_altem_Modus_liest_sich_gleich` | eine Zeile mit `Anteil_Modus = 'Aufgeschluesselt'` liest sich wie eine mit `'Gesamtwert'`, und der Schreibweg lässt die Spalte stehen |
| `Der_Hinweis_ohne_Positionen_nennt_die_heutigen_Knoepfe` (2 Kulturen) | Rest 1, beide Sprachen, ohne den alten Knopfnamen |

`EPOS.UI.Tests/Dialoge/PreisbloeckeTests` — vier neue, zwei ersetzte Fälle:
`Der_Brennstoff_Block_kennt_keinen_Modus_mehr` (keine Optionsgruppe, kein Radio, keiner der
beiden Modustexte im Markup), `Der_Knopf_meldet_nur_und_schreibt_nichts` (nicht mehr
gesperrt), `Summe_Kohaerenz_und_Rest_stehen_wie_beim_Strom`,
`Ein_positiver_Rest_steht_ohne_Warnfarbe`, und
`Der_Brennstoff_Block_traegt_seinen_Titel_in_beiden_Sprachen` (de-DE / en-US aus derselben
Ressourcenzeile). Entfallen ist
`In_Arbeitspreis_uebernehmen_geht_nur_im_aufgeschluesselten_Modus`;
`In_beiden_Modi_bleiben_die_Komponentenfelder_schreibbar` heißt jetzt
`Die_Komponentenfelder_bleiben_schreibbar`.

**Gegenprobe:** `Preisanteile.RestCtKwh` versuchsweise um 0,001 ct/kWh falsch rechnen
lassen. Ergebnis: **drei Fälle rot, darunter je einer je Träger**
(`Summe_und_Rest_des_Brennstoffs_…`, `Summe_und_Rest_des_Stroms_…`,
`Gleiche_Betraege_…`) — der gemeinsame Bauplan ist damit belegt, nicht behauptet.
Zurückgebaut, danach wieder 10/10.

### N7.7 Gate

| Prüfung | Ergebnis |
|---|---|
| Kern-Filter `WP-Plan.Kern.slnf` Release | 0 Fehler / **5 Warnungen** (keine neue, Schranke 7) |
| Windows-Schale (`EnableWindowsTargeting=true`) | 0 Fehler / 5 Warnungen (Bestand) |
| `EPOS.Kern.Tests` | 3 177 / 3 177 (+10) |
| `EPOS.UI.Tests` | 4 557 / 4 557 (+5 neu, 1 entfallen) |
| `SpeicherEngine.Tests` / `KiKern.Tests` / `SpeicherPlanung.Tests` | 368/368 · 499/499 · 27/28 (1 übersprungen) |
| Kulturen | beide — normal und `LC_ALL=en_US.UTF-8` |
| `Resource.Designer.cs` | neu erzeugt, 6 291 → 6 287 Einträge, zweiter Lauf +0 |
| `SqlDialektPruefer` | 1 474 Texte, **0 Fundstellen** |
| Referenzlauf 1030/1007/1017/1045/1046 gegen `2026-09-16_R8_Heizkessel_Kaskade` | **5/5 PASS**, 143 CSV, 1 656 417 Werte, **byte-gleich** (`diff -rq` je Projekt: 0 Unterschiede) |

Kein Rechenweg, kein Schemaschritt, keine neue Referenzbasis.

### N7.8 Abnahmepunkte auf Windows

| Nr. | Was zu sehen ist |
|---|---|
| `A-ZE1-1` | Energieträgerdialog, Brennstoffträger: Der Block „Preisbestandteile des Brennstoffs" führt **keine Wahl** „Gesamtwert / aufgeschlüsselt" mehr — vier Zeilen mit Schalter und Feld, darüber die Schnellwahl |
| `A-ZE1-2` | Unter den Zeilen stehen Summe der aktiven Bestandteile, Arbeitspreis der Trägerkarte und der nicht aufgeschlüsselte Rest; ein negativer Rest steht in Warnfarbe |
| `A-ZE1-3` | „In Arbeitspreis übernehmen" ist **immer** bedienbar; ein Klick trägt die Summe in das Arbeitspreisfeld der Karte, gespeichert wird erst mit „Speichern" |
| `A-ZE1-4` | Ein Projekt, dessen Brennstoffträger bisher auf „aufgeschlüsselt" stand, zeigt dieselben Werte wie vorher; die Anteile sind unverändert |
| `A-ZE1-5` | Kosten-Seite des Berichts: Eine Anlage ohne eigene Positionen trägt den Hinweis mit „Investitionskosten…" und „Betriebskosten…" — in beiden Sprachen, ohne „Kosten bearbeiten…" |
| `A-ZE1-6` | Strom-Block „Strompreis Details" unverändert: dieselbe Summe, dieselbe Kohärenzzeile, derselbe Rest-Vorschlag für die Beschaffung |

### N7.9 Logbuch-Entwurf (bestehende Version 1.2.0.2)

> Die Preisbestandteile eines Brennstoffträgers kommen ohne die Wahl „Gesamtwert /
> aufgeschlüsselt" aus: Ausgewiesen werden die Summe der eingeschalteten Bestandteile und
> ihr Abstand zum Arbeitspreis, und „In Arbeitspreis übernehmen" ist immer bedienbar.

---

## Nachtrag US-1 (17.09.2026) — die drei Umlagen-Katalogzeilen kommen in die Datenbank

Erledigt den Nebenbefund (2) aus dem Aufräumen nach N4–N6 und aus N7: Die drei
`UMLAGEN`-Zeilen, die `GesetzKatalog` mit der Saatgeneration 7 anlegt, standen in keiner
Datenbank.

### US1.1 Befund — gemessen, nicht vermutet

`GesetzKatalog.Vorbelegung` sät die Klasse `UMLAGEN` mit einer Quelle aus Zeilenname und
gemeinsamem Nachsatz. Mit dem längsten Namen ergab das mehr Zeichen, als
`Tab_Gesetzesparameter` zulässt:

| Zeile | Quelle vorher | Länge | Schranke |
|---|---|---|---|
| `UMLAGE_KWKG` | „KWKG-Umlage — Angabe des Anwenders vom 17.09.2026, Umlagen 2026 laut Veröffentlichung der Übertragungsnetzbetreiber" | 115 | 120 |
| `UMLAGE_OFFSHORE` | „Offshore-Netzumlage — …" | **123** | 120 |
| `UMLAGE_STROMNEV19` | „§ 19 StromNEV-Umlage — …" | **124** | 120 |

Das `CHECK (length("Quelle") <= 120)` (`sql/schema/001_grundschema.sql`) wies die zweite
Zeile ab. Die Saat schreibt über `StilleDb`; die schluckt jeden Fehler, gibt `-1` zurück und
schreibt „SQLite Error 19" auf eine Konsole, die im Auslieferungsbetrieb niemand liest.
`Einfuegen` machte daraus eine Ausnahme ohne Schlüssel und ohne Grund, und
`StelleKatalogSicher` fing sie in einem **leeren `catch`** ab. Folge: Die Schleife brach bei
der zweiten Zeile ab, die Markerzeile stieg **nicht** auf 7, und in
`Referenzlaeufe/Kenndaten_Test.sqlite` stand keine einzige `UMLAGEN`-Zeile
(`SELECT COUNT(*) … WHERE Klasse='UMLAGEN'` = 0). Gerechnet wurde weiterhin richtig — die
Konstanten des `StrompreisZerlegungModel` sind die wertgleiche Rückfallebene —, aber Katalog
und Schnellwahl führten die Sätze nicht.

### US1.2 Die Quelle wird kürzer, der Inhalt bleibt

Der gemeinsame Nachsatz lautet jetzt „Angabe des Anwenders vom 17.09.2026; Umlagen 2026 laut
den Übertragungsnetzbetreibern" (77 → 85 Zeichen des Nachsatzes selbst). Inhalt unverändert:
Anwenderangabe, Datum, Stichjahr, Herkunft.

| Zeile | Quelle nachher | Länge |
|---|---|---|
| `UMLAGE_KWKG` | „KWKG-Umlage — Angabe des Anwenders vom 17.09.2026; Umlagen 2026 laut den Übertragungsnetzbetreibern" | 99 |
| `UMLAGE_OFFSHORE` | „Offshore-Netzumlage — …" | 107 |
| `UMLAGE_STROMNEV19` | „§ 19 StromNEV-Umlage — …" | 108 |

### US1.3 Die Wache — damit es nicht wiederkommt

`EPOS.Kern.Tests/GesetzkatalogSaatWacheTests`, drei Fälle:

1. **`JedeSaatzeileBleibtInDenSchrankenDesSchemas`** — liest die
   `CHECK (length("…") <= n)` des Blocks `CREATE TABLE "Tab_Gesetzesparameter"` **aus
   `sql/schema/001_grundschema.sql`** (Schluessel 60, Klasse 40, Einheit 20, Status 12,
   Quelle 120) und misst **jede** der 225 Saatzeilen gegen sie. Die Liste der gefundenen
   Spalten wird selbst geprüft, damit ein umformuliertes Schema die Wache nicht still
   abschaltet.
2. **`DieNachsaatBringtDieDreiUmlagenzeilenInDieDatenbank`** — räumt auf einer eigenen
   Arbeitskopie die Generation 7 ab, setzt die Markerzeile auf 6 zurück, ruft
   `StelleKatalogSicher` und findet die drei Zeilen mit **0,446 / 0,941 / 1,559 ct/kWh**,
   Stichjahr 2026, wieder. Der zweite Lauf legt nichts mehr an (Idempotenz).
3. **`EinSaatfehlerStehtMitSchluesselUndGrundInDenWarnungen`** — stellt den Fehlschlag mit
   einem Trigger auf der Arbeitskopie her und verlangt Schlüssel, Klasse, Stichjahr und
   Grund in der Warnung.

**Gegenproben** gefahren und zurückgebaut: (a) den alten Quelltext wieder eingesetzt → Fall 1
rot mit „`UMLAGE_OFFSHORE` … Quelle hat 123 Zeichen, erlaubt sind 120" und „`UMLAGE_STROMNEV19`
… 124"; (b) den Erwartungswert der KWKG-Umlage auf 0,447 gestellt → Fall 2 rot.

### US1.4 Der stille Fehler trägt einen Namen

Drei Stellen, keine mehr:

* `StilleDb.LetzterSchreibfehler` — der Grund der zuletzt verschluckten schreibenden
  Anweisung. Nur der Schreibweg führt ihn; die lesenden Methoden brauchen ihn nicht, ihre
  Rückgabe `null` ist selbst die Aussage.
* `GesetzKatalog.Einfuegen` nennt in seiner Ausnahme **Schlüssel, Klasse, Stichjahr und
  Grund**; `StelleKatalogSicher` sammelt sie in `GesetzKatalog.SaatWarnungen` statt sie
  wortlos zu verschlucken. **Am Ablauf ändert sich nichts** — der Abbruch bleibt, die
  Markerzeile steigt bei einem Fehlschlag weiterhin nicht, der nächste Start versucht es
  wieder.
* `SchemaMigration.Ausfuehren` ruft nach den Schritten `GesetzKatalog.StelleKatalogSicher`
  und schreibt Zahl und Warnungen in den Fehlerbericht — also in die Protokolldatei neben der
  Datenbank. Der Ausgang der Migration hängt nicht daran: Der Katalog hat seine wertgleiche
  Rückfallebene, ein Fehlschlag ist eine **Warnung**.

### US1.5 Der Weg der Nachsaat in die Testdatenbank

`Werkzeuge/Testdatenbankschema` zieht die Datei auf den Schemastand nach und **sät jetzt auch
den Gesetzeskatalog nach** — kein Schemaschritt und deshalb ohne Nummer. Grund: Der Katalog
sät sich generationsweise selbst nach, aber nur, wenn ihn jemand aufruft; die Testdatenbank
startet nie ein Programm und stand deshalb auf der Generation ihres letzten
Anwendungsstarts (6).

Lauf auf `Referenzlaeufe/Kenndaten_Test.sqlite`: 0 Spalten, 0 Tabellen, **4 Zeilen
nachgesät, Generation jetzt 7**, keine Warnung. Gemessen:

| Größe | vorher | nachher |
|---|---|---|
| Zeilen in `Tab_Gesetzesparameter` | 222 | 226 |
| Klassen ohne `SYSTEM` | 9 | 10 (neu `UMLAGEN`) |
| `STROMSTEUER` | 7 | 8 (`STROMST_REDUZIERT_SATZ`) |
| `UMLAGEN` | 0 | 3 |
| Markerzeile `KATALOG_GENERATION` | 6 | 7 |

Die eingefrorenen Zahlen in `EPOS.Kern.Tests/KatalogpflegeTests` sind mitgezogen.

### US1.6 Gate

| Prüfung | Ergebnis |
|---|---|
| `dotnet build WP-Plan.Kern.slnf -c Release` | 0 Fehler, 5 Warnungen (Bestand, Schranke 7) |
| `dotnet build WindowsFormsApplication1 … -p:EnableWindowsTargeting=true` | 0 Fehler, 5 Warnungen (Bestand) |
| `EPOS.Kern.Tests` | 3 181/3 181 |
| `EPOS.UI.Tests` | 4 557/4 557 |
| SpeicherEngine / KiKern / SpeicherPlanung | 368/368, 499/499, 27/28 (1 übersprungen) |
| Beide Kulturen | normal und `LC_ALL=en_US.UTF-8` — gleiches Bild |
| `SqlDialektPruefer` | 1 474 Texte, **0 Fundstellen** |
| Referenzlauf 1030/1007/1017/1045/1046 gegen `2026-09-16_R8_Heizkessel_Kaskade` | **5/5 PASS**, 143 CSV, 1 656 417 Werte, **byte-gleich** |

Kein Rechenweg, kein Schemaschritt, keine neue Referenzbasis. Dass die Nachsaat den
Referenzlauf nicht berührt, ist damit **gemessen** und nicht behauptet: Zu jedem der vier
Schlüssel führt der Kern eine wertgleiche Code-Rückfallebene
(`StrompreisZerlegungModel.STROMSTEUER_REDUZIERT` 0,050 ct/kWh = 0,50 EUR/MWh; die drei
Umlagen stecken wertgleich im gefalteten Arbeitspreis des Schemaschritts 83).

### US1.7 Abnahmepunkte auf Windows

| Nr. | Was zu sehen ist |
|---|---|
| `A-US1-1` | Katalogpflege, Bereich **`UMLAGEN`**: drei Zeilen — KWKG-Umlage 0,446, Offshore-Netzumlage 0,941, § 19 StromNEV-Umlage 1,559 ct/kWh, alle ab 2026, Status `GESICHERT` |
| `A-US1-2` | Jede der drei Zeilen führt ihre Quelle vollständig, ohne abgeschnittenen Text |
| `A-US1-3` | Bereich `STROMSTEUER` führt zusätzlich `STROMST_REDUZIERT_SATZ` (0,50 EUR/MWh, ab 2026) |
| `A-US1-4` | Strompreis-Block, Schnellwahl „Stromsteuer energieintensiver Unternehmen": Die Herkunftszeile nennt **„Katalog: 0,5 EUR/MWh (ab 2026, …)"** statt der Rückfallebene. Die drei Umlagen haben bisher **keinen** Leser im Preisblock — sie sind Katalogzeilen zum Pflegen und Nachschlagen |
| `A-US1-5` | Protokolldatei neben der Datenbank: nach dem ersten Start einer Bestandsdatenbank steht dort „Gesetzeskatalog: 4 Zeile(n) nachgesät (Generation 7)." und **keine** Warnung |

### US1.8 Logbuch

Kein Eintrag. Der Anwender sah bisher keine falsche Angabe — die Umlagen rechneten
wertgleich aus der Rückfallebene; es fehlten Katalogzeilen, also eine hausinterne Sache.

### US1.9 Nebenbefunde (gemessen, nicht angefasst)

1. **Ein Saatfehler erzeugt bei jedem Start Dubletten.** Bricht die Nachsaatschleife ab,
   bleibt die Markerzeile auf der alten Generation — die Zeilen, die **vor** der
   fehlgeschlagenen schon angelegt wurden, legt der nächste Start ein zweites Mal an, mit
   neuer `ID`. In der Saatgeneration 7 betraf das `STROMST_REDUZIERT_SATZ` und
   `UMLAGE_KWKG`: Eine Bestandsdatenbank, die seit dem 17.09.2026 mehrfach gestartet wurde,
   kann jede dieser beiden Zeilen mehrfach führen — sichtbar in der Katalogpflege.
   **Die Ursache ist mit diesem Nachtrag weg** (die Saat läuft durch), die bereits
   entstandenen Dubletten sind es nicht. Ein Entdoppelungsschritt nach dem Muster von
   Schritt 76 (`ProjektEnergietraegerEindeutig`) wäre der Weg; er ist ein Schemaschritt und
   gehörte nicht in diesen Auftrag.
2. **Ein Kommentar nennt die falsche Generation.**
   `StrompreisZerlegungModel.STROMSTEUER_REDUZIERT` sagt, der Katalogschlüssel
   `GESETZ_STROMST_REDUZIERT` sei „mit der Saatgeneration 5 eingesät"; im Quelltext trägt
   die Zeile die Generation **7**. Reiner Kommentarfehler ohne Wirkung.
