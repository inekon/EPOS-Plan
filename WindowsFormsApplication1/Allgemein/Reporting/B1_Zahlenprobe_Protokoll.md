# B1 — Zahlenprobe: Stromsteuer-Doppelzählung und Altanwendungsvergleich (Protokoll)

Etappe B1 des [`Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md`](../../../Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md)
(§ 8: „Zahlenprobe zuerst"), durchgeführt am **29.08.2026** auf Branch `Pufferspeicher`.
**Reine Messung — keine Codezeile geändert.** Commit ohne Push (Etappenregel).

---

## 1 Teil A — Doppelzählung § 9 Abs. 1 Nr. 3 StromStG (Befund B2): BESTÄTIGT

### 1.1 Die Kette (Fundstellen)

- Buchung: `r.StromsteuerBefreiungEur = Regelsatz(20,50 €/MWh) × KwkEigenMWh × Anteil`
  (`SteuerGutschriftRechner.cs:573`) → Erlösreihe `STROMSTEUER_BEFREIUNG` im Kapitalwert
  (`WirtschaftlichkeitCtrl.cs:2641`).
- Bezugsseite: Das Projekt zahlt nur den **Rest**bezug (`:1660` Tarifmodell bzw.
  `KostenEmissionRechner` Flat) — der volle Arbeitspreis der Eigenmenge, samt einer darin
  enthaltenen Stromsteuer, ist bereits „vermieden".

### 1.2 Messung (Harness `..\dev\b1zahlenprobe\`, gitignored)

| Ebene | Ergebnis |
|---|---|
| **Bestand** (Produktiv, lesend) | `Tab_ErgebnisWirtschaftlichkeit`: **kein einziger** gespeicherter Lauf bucht `StromsteuerBefreiung > 0` — die E4-Bedingungen (Stundenreihen, Anlagenprüfung) griffen bisher nie zusammen. **Die Doppelzählung ist heute in keinem Bestandsergebnis wirksam.** |
| **Realer Stundenlauf** (Projekt 1024, frisch gegen die DB-Kopie simuliert; Matrix aus den Laufreihen) | Bedarf 365,0 · Restbezug 387,3 · **KWK-Eigen 73,7 MWh**. Buchung nach `:573` (Anteil 1): 20,50 × 73,7 = **1.510,84 €/a**. Stromsteueranteil derselben Menge in der kleineren Bezugsrechnung: 2,05 ct/kWh × 73.700 kWh = **1.510,84 €/a** — **derselbe Betrag auf beiden Pfaden.** (Projekt 1018 als Gegenprobe: ohne Strombedarfsreihe kein Eigenverbrauch, beide Pfade 0.) |
| **Synthetik** (100/62/38 MWh, Preis 24,60 ct inkl. 2,05 ct Steuer) | Ersparnis 9.348 € enthält 779 € Steueranteil der Eigenmenge; die Befreiungsreihe bucht **zusätzlich** 779 € → 1.558 € = **das Doppelte** des gesetzlichen Vorteils. |

### 1.3 Schluss und Empfehlung (BF1)

Die Doppelzählung ist **strukturell und zahlenhaft bestätigt** — sie tritt genau dann
ein, wenn der erfasste Strompreis die Stromsteuer enthält (Regelfall
Lieferantenrechnung; ob sie enthalten ist, ist im Bestand nicht erfasst — genau
Befund B1 des Konzepts, dessen Antwort die Preiszerlegung der Etappe B2 ist).

**Empfehlung zu BF1:** BW3 umsetzen — § 9 Abs. 1 Nr. 3 wird **Ausweis statt
Zahlungsstrom** (Umschalter `Stromst_Befreiung_Modus`, M-3). Wichtige Zusatzerkenntnis
dieser Messung: Da **kein** Bestandslauf die Reihe bucht, wäre sogar die Vorgabe
`AUSWEIS` für den gesamten Bestand ergebnisneutral — die Konzept-Vorsicht „Vorgabe =
Bestandsverhalten" kostet nichts, die schärfere Vorgabe aber auch nicht.
**Entscheidung liegt beim Anwender** (BF1); die Umsetzung gehört zu B6/M-3, nicht
hierher.

> **Nachtrag 30.08.2026 — BF1 entschieden: Vorgabe „Ausweis"** (Anwenderentscheid).
> BW3 wird in B6/M-3 mit Vorgabe `AUSWEIS` umgesetzt; der Projekt-Umschalter
> (`Stromst_Befreiung_Modus`) bleibt für Preise ohne Steueranteil.

---

## 2 Teil B — Zahlenprobe gegen die Altanwendung (Befund A8): BLOCKIERT

Die sechs Sollzahlen des Erlös-Screenshots sind dokumentiert (Bedarf 100, Restbezug 62,
Einspeisung 34, Eigenverbrauch 38 MWh; vermiedene Kosten 3.657 / −341 / 3.316 €;
Einspeiseerlös 1.028 €; Zuschläge 5.488 / 3.059 €) — die zugehörigen **Eingabewerte**
(Arbeits- und Leistungspreise je Zone, HT/NT-Zeiten, Anlagenleistung,
Inbetriebnahme/KWKG-Variante) sind es **nicht**, und die Alt-Excel selbst liegt weder im
Repo noch im Projektumfeld (`C:\Waermeplan`, rekursiv gesucht). Eine Reproduktion ohne
diese Eingaben wäre geraten, keine Probe.

**Was stattdessen belegt ist:** Die Differenzmethode der neuen Kette ist **formelgleich**
mit der Altanwendung — `Einsparung = Bezug(Bedarf) − Reststrom(Restbezug)` je Arbeit und
Leistung (Analyse § 2.3 ↔ `StromTarifRechner.Rechne:265-267`), und der negative
Leistungsanteil (−341 €) ist im Rollenmodell darstellbar (Reststrom teurer als Bezug).

**Zulieferbitte an den Anwender:** die BHKW-Plan-Excel (oder ein Screenshot der
Eingabemaske `Dial_ErloesEing` samt Tarifblatt zum Beispielfall). Damit ist die Probe
ein reiner Harness-Nachmittag; der `StromTarifRechner` ist als pure Funktion ohne
Datenbank direkt fütterbar.

---

## 3 Nebenbefunde des Messlaufs

1. Der `SimulationRunner` läuft **headless** sauber durch (Reflection-Harness, Kopie) —
   gute Nachricht für die B7-Testbarkeit.
2. Projekt 1018 führt eine Kessel-Anlage ohne Energieträger („Vitocrossal 200 …",
   `ID_Carrier` leer) und einen Puffer ohne Temperaturpaar — beides von den
   Lauf-Warnungen korrekt gemeldet (Anwender-Datenpflege, kein Codebefund).
3. Ein Lauf-Hinweis meldet an der WP-Kennlinie von 1024 fehlende
   Hochtemperatur-Stützstellen (957 h über 20 °C) — bekanntes Warnmuster.

## 4 Dateien

Nur dieses Protokoll. Harness `..\dev\b1zahlenprobe\` gehört nicht zum Lieferumfang;
die Simulation lief ausschließlich gegen die Scratchpad-Kopie, geschrieben wurde
nirgends.
