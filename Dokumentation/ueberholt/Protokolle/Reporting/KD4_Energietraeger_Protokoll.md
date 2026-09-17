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
