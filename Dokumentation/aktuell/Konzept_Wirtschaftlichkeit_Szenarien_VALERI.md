# Konzept — Szenarioparameter und VALERI-Abgleich (Wirtschaftlichkeit)

**Anwenderentscheid 09.09.2026** · Etappen **W5‑B‑9** (Szenarioparameter) und
**W5‑B‑10** (VALERI-Abgleich nach DIN EN 17463).

**Stand 24.09.2026** · Codestand `75d45630` · `SchemaStand.Zielversion` = **119**, Schemaschritte
90–119 vergeben (116–118 die Schritte B, C und D der Etappe E9a; E9b ohne Schritt; 119 die Kühlung KU2). Die Etappen W5‑B‑9 bis
W5‑B‑12 sind gebaut; ihre Fortsetzung läuft unter der Reihe **V-A…V-E** des konsolidierten Konzepts (§ 2.11.4) im
Etappenplan **E0–E12** des Analysepapiers. **V-E ist mit E9 gebaut** (Teil a im Kern #461, Teil b in den
Dialogen #462): Günstig und Ungünstig lesen zusätzlich Betrachtungszeitraum, Mengenänderung, Trägerpreise und
Erlössätze je Szenario — NULL heißt „wie Erwartet", eine Vorgabe gibt es für diese Größen nicht (konsolidiertes
Konzept § 2.11.5); gepflegt werden sie in den Zeilen 8 und 9 der Szenariotafel (§ 4) und mit dem ±-Knopf an
Trägerpreisen und Erlössätzen, und an der Stelle des Hinweistexts steht der Ausweis „n von m Parametern
szenariert". **Entscheid A5 vom 20.09.2026 (nach Empfehlung): V-E rechnet die
Degradation nicht ein** — der Entscheid „G3 nicht umsetzen" dieses Papiers gilt, der Widerspruch zum
konsolidierten Konzept ist aufgelöst (§ 7.3, § 9.4).

---

## 1 Ausgangsbefund (08.09.2026)

Die Seite „Wirtschaftlichkeit" bietet die drei Szenarien **Erwartet / Best /
Worst** an (`WirtschaftlichkeitSzenario`), zeigt aber in allen dreien **dieselben
Zahlen**. Zwei Ursachen:

1. **Es gab nur Zeilenwerte, und die sind fast überall leer.**
   `Tab_ProjektWerte.BestCase` / `WorstCase` (und `…_Nutzungsdauer`) stehen bei
   nahezu jeder Position auf 0. `WirtschaftlichkeitCtrl.Szenariowert` fällt dann
   nach dem VALERI-Muster („0/leer = kein Szenariowert gepflegt") auf den
   Erwartungswert zurück — und damit rechnen alle drei Szenarien dieselbe Zeile.
2. **Es gab keine Szenarioparameter auf Projektebene.** Zins, Preissteigerungen,
   Investitions- und Ertragsunsicherheit galten für alle drei Szenarien gleich.
   Genau das ist aber die Ebene, auf der VALERI seine Bandbreite aufspannt.

Hinzu kam: Fehlen die Arbeitspreise, sind die Energiekosten „nicht bestimmbar" —
dann bleibt der Kapitalwert in **allen** Szenarien leer. Das ist keine
Szenariofrage, sondern ein Datenmangel der Kostenmaske; die Fehlgrundzeile sagt
das bereits.

---

## 2 Der Parametersatz je Szenario (Etappe W5‑B‑9)

Neben den Zeilenwerten bekommt jedes Projekt **je Szenario einen
Parametersatz** — `SzenarioSatz`, abgelegt an
`Tab_ProjektWirtschaftlichkeit` (Migrationsschritt **71**).

| Größe | Feld | Einheit | Wirkung |
|---|---|---|---|
| Kalkulationszins | `Zinssatz` | % | Diskontierung, Annuität, Gestehungskosten |
| Preissteigerung Energie | `PreissteigerungEnergie` | %/a | p_E: Energiekosten, CO₂-Abgabe, Endenergie-Topf |
| Preissteigerung Betrieb | `PreissteigerungBetrieb` | %/a | p_B: Betriebskosten-Topf |
| Investitionsänderung | `InvestitionAenderung` | % | Investitionspositionen **ohne** gepflegten Szenariowert |
| Ertragsänderung | `ErtragAenderung` | % | Einspeiseerlöse und die PV-Vergütungsreihe |
| Nutzungsdaueränderung | `NutzungsdauerAenderung` | a | Nutzungsdauer **ohne** gepflegten Szenariowert → Ersatz und Restwert |

**Vorzeichen einheitlich:** `+` heißt *mehr / länger*. Eine höhere Investition
ist ungünstig, ein höherer Ertrag günstig, eine längere Nutzungsdauer günstig —
die Vorgaben tragen dem Rechnung.

**Seit Etappe W5‑B‑12** kommt als **siebte** Größe die Preissteigerung der
kapitalgebundenen Kosten p_I hinzu (`PreissteigerungInvestition`, %/a; sie
indiziert Ersatzbeschaffungen und Restwert-Preisbasis). Ihre Nullsemantik und ihre
Vorgaberegel stehen in § 10.2 und § 10.4 — sie sind eine Spur anders als bei den
sechs Größen hier, weil ihr Erwartungswert selbst einen Rückfall hat.

### 2.1 Vorbelegung (Vorgaben)

**Erwartet** bekommt **keinen** Parametersatz. Es rechnet unverändert mit den
Projektparametern — Zeichen für Zeichen der Rechenweg von vor dieser Etappe.
Das ist die Zusage, an der die Regressionsprobe hängt (§ 6).

Für Best und Worst gilt, solange nichts gepflegt ist (Spalte NULL):

| Größe | Best | Worst |
|---|---|---|
| Kalkulationszins | i − 1 %‑Pkt (nie < 0) | i + 1 %‑Pkt |
| Preissteigerung Energie | p_E − 1 %‑Pkt | p_E + 1 %‑Pkt |
| Preissteigerung Betrieb | p_B − 1 %‑Pkt | p_B + 1 %‑Pkt |
| Investitionsänderung | − 10 % | + 10 % |
| Ertragsänderung | + 10 % | − 10 % |
| Nutzungsdaueränderung | + 2 a | − 2 a |

Die Vorgaben sind **Vorschlag, nicht Gesetz**: Der Parameterdialog zeigt sie im
Reiter „Szenarien" mit den drei Spalten *Erwartet | Best | Worst*, jedes Feld ist
überschreibbar, und der Knopf „Vorgaben" stellt sie wieder her. Ein Feld, das der
Anwender nie angefasst hat, bleibt NULL — die Vorgabe zieht dann bei einer
geänderten Projektangabe automatisch mit (i wechselt von 3 auf 4 % → Best folgt
auf 3 %).

### 2.2 Vorrangregel — gepflegte Zeilenwerte schlagen den Parametersatz

> **Ein gepflegter Szenariowert je Zeile hat Vorrang. Der Parametersatz greift
> genau dort, wo keiner gepflegt ist.**

Das ist die Fortschreibung des VALERI-Musters, das die Zeilenwerte schon tragen,
und es verhindert die einzige echte Falle dieser Etappe: **Doppelzählung**. Wer
für eine Position 55.000 € als Worst-Case erfasst hat, meint diese Zahl — nicht
diese Zahl plus 10 %.

Umgesetzt wird das **je Zeile**, nicht als Summenkorrektur:
`InvestKaskade.Zeile` merkt sich seit dieser Etappe in `WertGepflegt` und
`DauerGepflegt`, ob der Szenariowert aus der Best-/Worst-Spalte kam oder auf den
Erwartungswert zurückgefallen ist. `WirtschaftlichkeitCtrl.LiesInvestitionen`
skaliert nur die zurückgefallenen Zeilen.

**Nicht skaliert werden:**

* **Zuschusszeilen** (Kostenart `ZUSCHUSS`, K5). Eine bewilligte Förderzusage über
  einen festen Betrag ändert sich nicht, weil die Anlage 10 % mehr kostet —
  dieselbe Begründung, mit der schon die Sensitivität sie ausnimmt. Ihre eigenen
  Best-/Worst-Spalten gelten weiterhin.
* **Betriebskostenzeilen.** Für die Betriebsseite ist p_B der pauschale Hebel; eine
  zweite, multiplikative Unsicherheit auf denselben Zeilen wäre Doppelzählung.
  Die investitionsgekoppelten Zeilen („x % der Investitionssumme") folgen dem
  Investitionsausschlag nicht über den Szenariosatz, sondern über ihre
  **Bemessung** — seit Etappe W5‑B‑11 aus der Kaskade **mit** Ausschlag, nach
  derselben Vorrangregel (§ 9.5; bis dahin aus der unskalierten Kaskade, das
  war die dokumentierte Grenze G11).
* **Gesetzliche Erlösreihen** (KWKG-Zuschlag, Energiesteuer, Stromsteuer). Sie
  hängen an Sätzen und Kontingenten des Gesetzeskatalogs, nicht an einer
  Ertragserwartung. Für das Regulierungsrisiko gibt es die eigene
  Sensitivitätszeile „KWKG-Bonus entfällt".

### 2.3 Nutzungsdauer

Die Nutzungsdauer wird **addiert**, nicht skaliert (`+2 a` / `−2 a`), weil das
Datenfeld eine Jahreszahl ist und die Szenariospalten
`BestCase_Nutzungsdauer` / `WorstCase_Nutzungsdauer` ebenfalls Jahre führen. Die
Untergrenze ist **1 a**: Alles darunter bedeutet im `KapitalwertRechner` „keine
Nutzungsdauer gepflegt" (n = T, kein Ersatz, kein Restwert), und eine Änderung
darf diese Bedeutung nicht versehentlich auslösen. Zeilen, die bereits ohne
Nutzungsdauer stehen (n < 1), bleiben unangetastet.

### 2.4 Was der Kern damit tut

`WirtschaftlichkeitCtrl.Berechne` bildet je Szenario **einen
Parametersatz-Abzug** (`WirtschaftlichkeitParameter.FuerSzenario`):

* **Erwartet** → derselbe Satz (identische Referenz), also der unveränderte Lauf.
* **Best/Worst** → eine flache Kopie mit ersetztem `Zinssatz`,
  `PreissteigerungEnergie` und `PreissteigerungBetrieb`. Alles Weitere
  (Betrachtungszeitraum, Einspeisevergütung, KWKG, Steuern, Bilanzierung) bleibt
  unverändert — es sind Rechtsstände und Preise, keine Szenariogrößen.
  **Mit E9 Teil a (#461)** ersetzt die Kopie zusätzlich den Betrachtungszeitraum und die
  Einspeisevergütungen PV und KWK, wo das Szenario eigene trägt; Mengenänderung, Trägerpreise,
  DV-Entgelt und PPA-Preis je Szenario liest der Kern an je einer eigenen Stelle (konsolidiertes
  Konzept § 2.11.5, „Regeln des Kerns"). KWKG, Steuern und Bilanzierung bleiben unverändert.

Investitions-, Ertrags- und Nutzungsdaueränderung wirken **in der Eingabe**
(`BaueEingabe` → `LiesInvestitionen`, Einspeiseerlös, PV-Vergütungsreihe), nicht
im Rechenkern: Der `KapitalwertRechner` bleibt von dieser Etappe unberührt.

Persistiert wird je Ergebniszeile der **wirksame** Satz — `Zinssatz`,
`Preissteigerung_Energie`, `Preissteigerung_Betrieb` in
`Tab_ErgebnisWirtschaftlichkeit` tragen ab dieser Etappe die Szenariowerte statt
dreimal die Projektwerte. Bericht und Seite lesen damit dieselbe Annahme, mit der
gerechnet wurde.

---

## 3 Ablage und Migrationsschritt 71

Zwölf **nullbare** `DOUBLE`-Spalten an `Tab_ProjektWirtschaftlichkeit`
(`SchemaKatalog.Schritt71_Szenarioparameter`) — sechs je Szenario:

```
Szen_Best_Zins    Szen_Best_Preis_E    Szen_Best_Preis_B
Szen_Best_Invest  Szen_Best_Ertrag     Szen_Best_Dauer
Szen_Worst_Zins   Szen_Worst_Preis_E   Szen_Worst_Preis_B
Szen_Worst_Invest Szen_Worst_Ertrag    Szen_Worst_Dauer
```

* **NULL = Vorgabe.** Kein DML, kein DDL-DEFAULT — dasselbe Muster wie Schritt 70.
* **Erwartet bekommt keine Spalten.** Es *ist* der Projektparametersatz; eigene
  Spalten wären eine zweite Wahrheit für dieselbe Zahl.
* Die Rückfallebene `SchemaKatalog.Alle` führt die Spalten **nicht** — dieselbe
  Begründung wie bei den übrigen `Tab_ProjektWirtschaftlichkeit`-Schritten (20,
  21, 28): Kein Rechenkern der Simulation liest sie; der Schemaschritt ist ihr einziger
  DDL-Ort.
* **Ergebnisneutral für Erwartet**, verändernd für Best/Worst — und das ist der
  Zweck des Schritts. Der Referenzlauf ist nicht berührt: Er rechnet
  Simulationen, keine Wirtschaftlichkeit.

---

## 4 Dialog „Parameter…" — Abschnitt „Szenarien"

Drei Spalten, **neun Zeilen** (seit W5‑B‑12 mit p_I, seit E9b, #462, mit Betrachtungszeitraum und
Mengenänderung):

| | Erwartet | Best | Worst |
|---|---|---|---|
| Kalkulationszins [%] | *Projektwert, nur Anzeige* | Feld | Feld |
| Preissteigerung Energie [%/a] | *Projektwert* | Feld | Feld |
| Preissteigerung Betrieb [%/a] | *Projektwert* | Feld | Feld |
| Preissteigerung Investition [%/a] | *wirksames p_I (§ 10.2)* | Feld | Feld |
| Investition [%] | 0 | Feld | Feld |
| Erträge [%] | 0 | Feld | Feld |
| Nutzungsdauer [a] | 0 | Feld | Feld |
| Betrachtungszeitraum [a] | *Projektwert T* | Feld (ganze Jahre, 1–50; leer = wie Erwartet) | Feld |
| Mengenänderung [%] | 0 % | Feld (leer = wie Erwartet) | Feld |

Die p_I-Zeile steht **bei den beiden anderen Preissteigerungen** und nicht am Ende
der Tabelle — dieselbe Reihenfolge, in der `SzenarioSatz.Nachweis` die Größen
aufzählt (i · p_E · p_B · p_I · Investition · Erträge · Nutzungsdauer). Mit E9 Teil a (#461)
nennt die Nachweiszeile dazu den Betrachtungszeitraum T hinter i und am Ende die
Einspeisevergütung, die Einspeisevergütung KWK, wo eine geführt wird, und die Mengenänderung nur,
wenn sie gepflegt ist; die Herleitungszeilen des Dialogs nennen Zeitraum, Mengenänderung und
Einspeisevergütungen nur, wenn das Szenario sie pflegt (#462). Die Erwartet-Zelle der p_I-Zeile
zeigt das **wirksame** p_I, nicht das gepflegte: Bei leerem Feld ist
das p_B, und genau um diesen Wert spannen sich die Vorgaben daneben.

**Die Zeilen 8 und 9 (E9b, #462) haben keine Vorgabe:** Ein leeres Feld heißt „wie Erwartet"; das
Feld zeigt den gepflegten Wert, der Platzhalter den Wert, mit dem dann gerechnet wird (T bzw. 0). Der
Zeitraum ist eine ganze Zahl von Jahren wie das Feld T; ein längerer Zeitraum ist nicht von selbst der
günstigere — überwiegen die Kosten, senkt er den Kapitalwert.

Die Erwartet-Spalte ist **Anzeige, kein Eingabefeld** — sie wiederholt, was im
Abschnitt „Allgemein" gepflegt wird („Kein Delegat ist kein Knopf"). Der Knopf
**„Vorgaben"** setzt alle **achtzehn** Felder der Tafel auf NULL zurück und lässt die
Einspeisevergütungen je Szenario stehen; die Herleitungszeile
nennt die geltende Regel und ob der Satz aus Vorgaben oder aus gepflegten Werten
besteht.

**±-Knöpfe (E9b, #462).** Die übrigen neuen Größen pflegt der ±-Knopf dort, wo ihr Erwartet-Wert
steht: die Einspeisevergütung PV in der Gruppe „Strom" dieses Dialogs, die Einspeisevergütung KWK im
Dialog „BHKW-Wirtschaftlichkeit", Arbeits-, Grund- und Leistungspreis an der Trägerkarte im Projekt,
DV-Entgelt und PPA-Preis im PV-Vergütungsdialog — derselbe Baustein wie an den Kostenpositionen
(`CaseEingabeDialog`; konsolidiertes Konzept § 2.11.5, Regel „Pflege").

---

## 5 Seite „Wirtschaftlichkeit"

Die Szenariowahl bleibt, wie sie ist. Ergänzt wird die **Statuszeile** unter der
Wahl: Sie nennt für das gewählte Szenario den wirksamen Satz und seine Herkunft —
„Vorgaben" oder „gepflegt" —, für Erwartet den unveränderten Projektparametersatz.
Die Kennzahltabelle zeigt je gewählter Version die Zahlen des gewählten Szenarios;
Restwert und Ersatzbeschaffungen stehen seit W5‑B‑10 als eigene Zeilen darin. Was die
Ergebnisansicht daran ändert (Umschalter, Verlauf mit drei Szenarien, der Ausweis „n von m
Parametern szenariert" an der Stelle des Hinweistexts), steht in § 11 — gebaut.

**Strombedarf ohne Verwendung — die Gruppenregel.** Je Stand gilt: Führt ein Stand einen
Netzbezug, aber keinen Erzeuger, der Strom verwendet (keine Wärmepumpe, Photovoltaik, kein
Stromspeicher, Heizstab, Elektrokessel, BHKW, keine Anlage mit Hilfsenergie-Anteil), bleibt
dieser Netzbezug in Energiekosten und Emissionen außen vor
(`ProjektEnergietraegerCtrl.StromOhneVerwendung`). **Im Vergleich gilt die Regel je
Vergleichsgruppe:** Verwendet irgendein Stand der Gruppe — Referenz oder Variante — Strom
(`ProjektEnergietraegerCtrl.GruppeVerwendetStrom`), bepreisen und bewerten alle Stände der Gruppe
ihren Netzbezug; ein Stand ohne zugeordneten Stromträger nimmt dafür den Auslieferungsträger des
Katalogs. Verwendet kein Stand Strom, bleibt es bei der Regel je Stand. Sonst stünde der
unbepreiste Netzbezug des einen Standes neben dem bepreisten Reststrom des anderen, und eine
Variante, die Strom einspart, erschiene mit Mehrkosten. Die Regel wirkt auf einer Kopie des
Standes (`WirtschaftlichkeitCtrl.StromGruppenregel`, `Szenariodaten`) — in allen drei Szenarien, in
Sensitivität, Bandbreite, Einstufung, Verlauf und Excel-Verlauf gleich. Die Einzelbetrachtung eines
Standes (Kostenseite, Übersicht) bleibt je Stand. Das Hinweisband nennt den Fall: „Strombedarf
ohne Verwendung im Stand „X“: Im Vergleich mit „Y“ wird der Netzbezug von … MWh/a bepreist und
bewertet (Gruppenregel).“

---

## 6 Nachweise

* **Erwartet bleibt zahlengleich.** `SzenarioParameterTests` rechnet die
  Kapitalwerte aller Projekte der Testdatenbank einmal mit und einmal ohne
  Szenariosätze und vergleicht sie **bitgenau** — die Vorgaben dürfen den
  Erwartungsfall nicht anfassen.
* **Best und Worst unterscheiden sich** und liegen in der richtigen Reihenfolge
  (KW_Best ≥ KW_Erwartet ≥ KW_Worst), sobald Vorgaben oder gepflegte Werte gelten.
* **Vorrang** — eine Position mit gepflegtem Worst-Case-Betrag trägt genau diesen
  Betrag, nicht Betrag × 1,1.
* **Migration 71** — Spalten vorhanden, idempotent, Zielstand 71.

---

## 7 VALERI-Abgleich nach DIN EN 17463 (Etappe W5‑B‑10)

DIN EN 17463 („Valuation of Energy Related Investments", VALERI) verlangt für
jede energiebezogene Investition eine **Kapitalwertrechnung über den gesamten
Zahlungsstrom**, ausgewiesen je Jahr, mit Bandbreite (Worst/Erwartet/Best) und
offengelegten Annahmen.

### 7.1 Was EPOS-Plan bereits abbildet

| VALERI-Anforderung | In EPOS-Plan |
|---|---|
| Nettobarwert (Kapitalwert) | `KapitalwertRechner.Rechne` — KW = −I₀ + Σ (E_t − A_t)/(1+i)^t + RW_T/(1+i)^T |
| Zahlungsströme je Jahr | `Zahlungsbild.NominalReihe`/`BarwertReihe` plus die Einzelreihen (Betrieb, Endenergie-Anteil, Energie, CO₂, Ersatz, Einspeiseerlös, benannte Erlösreihen) seit W4 E7; Mehrjahrestabelle im Bericht, Verlaufstabelle je Szenario im Blatt „Verlauf" des Tabellenberichts |
| Betrachtungszeitraum T | Projektparameter, 1…50 a; der Verlauf der Wirtschaftlichkeitsseite rechnet alle drei Szenarien über einen frei wählbaren Zeitraum (2 bis 60 a) |
| Diskontierungszins | Projektparameter, seit W5‑B‑9 je Szenario |
| Restwert am Ende von T | linear je Position, abgezinst (`RestwertBarwert`) |
| Ersatzinvestition bei n < T | in t = n, 2n, … (`ErsatzJeJahr`) |
| Preisentwicklung | p_E (Energie, CO₂, Endenergie-Topf) und p_B (Betrieb); CO₂-Preispfad und PV-Vergütung jahresscharf |
| Förderungen | Investitionszuschuss (K5, I₀-mindernd), KWKG-Zuschlag und drei Steuergutschriften als jahresscharfe Erlösreihen |
| Sensitivitäten | Zins, Energiepreissteigerung, Investition, Energiekosten, „KWKG-Bonus entfällt" |
| Bandbreite Worst/Erwartet/Best | Zeilenwerte (VALERI-Muster) **und** Parametersatz (W5‑B‑9); angezeigt als Ungünstig/Erwartet/Günstig — auf der Seite nebeneinander mit Spannenbild, im Verlauf je Szenario eine Strichart |
| Weitere Kennzahlen | Annuität, dynamische Amortisation, interner Zinsfuß, Wärmegestehungskosten |
| Referenzfall | die **wählbare Referenz je Vergleichsgruppe** (Stamm oder Variante, `ID_Referenzprojekt`, seit #358; Vorgabe = Stammprojekt); Varianten werden als **Differenz** zu ihr bewertet — genau die VALERI-Sicht „Maßnahme gegen Weiterbetrieb" |
| Offenlegung der Annahmen | Parameternachweis (`WirtschaftlichkeitParameter.Nachweis`), Herkunft der Steuersätze, Hinweiszeilen bei jeder Vereinfachung |

### 7.2 Umgesetzt mit dieser Etappe (ohne neues Datenmodell)

* **V1 — Restwert und Ersatzbeschaffungen als eigener Ausweis.** Beide steckten
  bisher nur in `BarwertAusgaben` bzw. in einer einzelnen Kennzahl. Der Barwert
  der Ersatzbeschaffungen wird jetzt getrennt geführt
  (`Tab_ErgebnisWirtschaftlichkeit.ErsatzBarwert`) und erscheint zusammen mit dem
  Restwert als eigene Zeile der Vergleichstabelle. VALERI verlangt beides als
  ausgewiesene Position, nicht als Saldo.
* **V2 — die Annahmen der Bandbreite stehen in der Nachweiszeile.** Der
  Szenariosatz und seine Herkunft (Vorgabe/gepflegt) sind Teil des
  Parameternachweises und damit Teil jedes Berichts, der ihn führt.
* **V3 — je Ergebniszeile der wirksame Satz.** Zins und beide Preissteigerungen
  werden je Szenario persistiert (§ 2.4), nicht dreimal der Erwartungswert.

### 7.3 Offen — Entscheidungsbedarf des Anwenders

| Nr. | Lücke | Was VALERI verlangt | Aufwand | **Entscheid 09.09.2026** |
|---|---|---|---|---|
| **G1** | **Endjahr je Position.** EPOS kennt seit KD6 ein Startjahr je Kostenzeile, aber kein Endjahr. | VALERI führt je Faktor Start- **und** Endjahr (`99` = ganze Betriebszeit). | Spalte + Rechenweg; Datenmodell | **nicht umsetzen** — als Vereinfachung offenlegen (§ 9.4) |
| **G2** | **Preisänderung je Kostenart.** Heute zwei Töpfe (p_B, p_E) plus CO₂-Pfad. | VALERI führt eine eigene Preisänderung je Faktor. | Spalte je Zeile + Rechenkern; Datenmodell | **umgesetzt W5‑B‑12** — nur als dritter Satz p_I, nicht je Zeile (§ 10) |
| **G3** | **Degradation je Faktor.** Nur die PV-Ertragsdegradation ist modelliert. | VALERI führt eine Degradation je Nutzen-/Lastenfaktor. | Spalte je Zeile; Datenmodell | **nicht umsetzen** — offenlegen (§ 9.4). **Bestätigt mit Entscheid A5 vom 20.09.2026 (nach Empfehlung):** Das konsolidierte Konzept führte die Degradation als `V-G2` in der Etappe **V-E** (§ 2.11.4); der Widerspruch ist zugunsten dieses Papiers **aufgelöst — V-E wird ohne Degradation geplant**, § 2.11.2 und § 2.11.4 des Konzepts sind nachgezogen |
| **G4** | **Preisindizierung der Ersatzbeschaffung.** Ersatz wird nominal unverändert angesetzt (Vereinfachung W1). | VDI 2067/VALERI setzen Ersatzbeschaffungen üblicherweise preisindiziert an. | Rechenkern; **fachlicher Entscheid** | **umgesetzt W5‑B‑12** — Preissteigerungssatz p_I, Migrationsschritt 72 (§ 10) |
| **G5** | **Startjahr für die Energiekosten.** Die Simulation kennt keine Startjahre je Komponente; die Energiekosten sind die Gesamtrechnung des Laufs (dokumentierte Vereinfachung FK10). | VALERI aktiviert jeden Faktor ab seinem Betriebsjahr. | Simulation; groß | **nicht umsetzen** — offenlegen (§ 9.4) |
| **G6** | **Nicht monetisierbare Wirkungen.** Kein Freitextfeld für Komfort, Versorgungssicherheit, Arbeitssicherheit. | VALERI verlangt eine qualitative Beschreibung im Bewertungsbericht. | Feld + Berichtsbaustein | **umgesetzt W5‑B‑12** — Freitextfeld (§ 10.5); **abgelöst #479** (E17, V‑G11 des konsolidierten Konzepts) durch die Liste mit Kategorie und Beurteilung, der Freitext bleibt als Altfeld lesbar |
| **G7** | **Betrachtungszeitraum aus der Nutzungsdauer.** T ist frei wählbar und wird nicht gegen die längste Nutzungsdauer geprüft. | VALERI verlangt die Begründung des Zeitraums. | Prüfzeile; klein | **W5‑B‑11 umgesetzt** (§ 9.3) |
| **G8** | **Berichtsausgabe der Bandbreite.** Der Word-/Excel-Bericht führt heute den Erwartungsfall. | VALERI-Bericht weist alle drei Szenarien nebeneinander aus. | Berichtsbaustein; **kein klarer Anker** — offen gelassen | **W5‑B‑11 umgesetzt** (§ 9.2) |
| **G9** | **Kapitalwert je Version absolut.** Wird geführt, aber die Entscheidungsempfehlung („Vorschlag zur Entscheidung") fehlt als Text. | VALERI-Bericht formuliert eine Empfehlung. | Textbaustein; klein | **W5‑B‑11 umgesetzt** (§ 9.1) |
| **G10** | **Aufteilung Eigennutzung/Einspeisung als Annahme.** EPOS leitet sie aus der Simulation ab — fachlich **besser** als VALERI, aber die Herleitung steht nicht im Bericht. | VALERI erfasst sie als Annahme. | Ausweis; klein | **W5‑B‑11 umgesetzt** (§ 9.4) |
| **G11** | **Investitionsgekoppelte Betriebskosten im Szenario.** „x % der Investitionssumme" folgt dem Szenario-Investitionsausschlag **nicht** (anders als in der Sensitivität, FX5‑a). | Konsequenz wäre, den Ausschlag auch dort mitzuziehen. | klein; **fachlicher Entscheid** | **W5‑B‑11 umgesetzt** — Ausschlag wird mitgezogen (§ 9.5) |

**Bewusst nicht übernommen:** Die VALERI-Vorlage rechnet ihre Worst-/Best-Spalten
über feste Formelfaktoren im Tabellenblatt (z. B. `=E44*1,3`). EPOS-Plan trennt
statt dessen **gepflegter Zeilenwert** von **pauschalem Parametersatz** (§ 2.2) —
dieselbe Wirkung, aber nachvollziehbar, wo die Zahl herkommt.

---

## 8 Grenzen der Etappen W5‑B‑9/10

* Der `KapitalwertRechner` ist in **W5‑B‑9/10/11 unverändert**. Alle
  Szenariowirkungen entstehen in der Eingabe bzw. im Parametersatz. (Erst
  Etappe **W5‑B‑12** fasst ihn an — für die Preisindizierung der
  Ersatzbeschaffung, G4.)
* Die Kostenseite und der Dialog „Kostenverwaltung" zeigen weiterhin die
  **gepflegten** Positionswerte ohne pauschalen Ausschlag — dort geht es um
  erfasste Zahlen, nicht um eine Bandbreite. Das gilt auch nach W5‑B‑11: Der
  Ausschlag auf die Bemessungsbasis wirkt in der Rechnung, nicht in der Anzeige.
* Die Sensitivitätsanalyse bleibt am Szenario **Erwartet** und rechnet mit ihren
  eigenen Ausschlägen (± 1 %‑Pkt, ± 10 %). Sie beantwortet eine andere Frage als
  die Szenarien: „Wie empfindlich ist das Ergebnis?" statt „Wie sieht ein
  ungünstiger Verlauf aus?". Ihre eigene Korrektur der investitionsgekoppelten
  Betriebskosten (FX5‑a, additiv in `RechneBild`) und die Basisskalierung aus
  W5‑B‑11 treffen deshalb **nie zusammen**: Auf Erwartet ist der Szenariosatz
  null, im Szenario läuft keine Sensitivität.

---

## 9 Etappe W5‑B‑11 — Umsetzung der Entscheidungen

Der Anwender hat die Gap-Liste § 7.3 am **09.09.2026** entschieden. Diese Etappe
setzt alles um, was **ohne neue Spalte** auskommt (G11, G8, G9, G7, G10) und legt
die drei bewusst nicht umgesetzten Lücken offen (G1, G3, G5). G4, G2 und G6 gehören
zur Etappe **W5‑B‑12** (Preissteigerungssatz p_I und Freitextfeld,
Migrationsschritt 72).

### 9.1 G9 — die Empfehlungsregel

Maßstab ist die **Kapitalwertdifferenz zur gewählten Referenz** (`KapitalwertDiff`), nicht der
absolute Kapitalwert: Die Referenz ist die Unterlassensalternative (§ 7.1, Referenzfall). Seit
#358 ist sie je Vergleichsgruppe wählbar; ohne Wahl gilt das Stammprojekt. Die Ressource
`WIRT_EMPF_KEINE` nennt die **gewählte Referenz** beim Namen (parametrierter Text „gegenüber {0}") —
**umgesetzt #405** (G9-Referenztext, zusammen mit der Δ-Fußzeile).

| Stufe | Bedingung |
|---|---|
| **empfohlen** | ΔKW > 0 in Worst, Erwartet und Best |
| **bedingt empfohlen** | ΔKW > 0 in Erwartet, aber ≤ 0 in Worst (oder in Best) |
| **nicht empfohlen** | ΔKW ≤ 0 in Erwartet |
| Zusatz **„Bandbreite nicht berechnet"** | Best oder Worst fehlt → Urteil allein nach Erwartet |

**Gesamtvorschlag:** die höchste Erwartet-Differenz unter den *empfohlenen*, sonst
unter den *bedingt empfohlenen*, sonst der Satz „Keine Variante ist gegenüber der
Referenz wirtschaftlich; Weiterbetrieb (Referenzfall)." Ohne Variante mit
Erwartet-Ergebnis bleibt der Text **leer** — ein Vorschlag ohne Zahlen wäre eine
Behauptung.

Die Regel steht **im Kern** (`WirtschaftlichkeitEmpfehlung`), damit Seite,
Word-Bericht und Excel-Blatt denselben Satz zeigen; die Texte sind Ressourcen
(`WIRT_EMPF_*`, deutsch und englisch).

### 9.2 G8 — die Bandbreite im Bericht

Die Szenarientabelle führte schon immer die Differenz, hieß aber „KW Worst". Sie
heißt jetzt **„ΔKW Worst/Erwartet/Best [€]"**, bekommt eine Fußzeile zur Bedeutung
von Δ und eine Spalte **„Einstufung"** (§ 9.1). Darunter stehen die **Annahmen**
von Best und Worst — der wirksame Parametersatz mit seiner Herkunft
(`SzenarioSatz.Nachweis`, „Vorgaben" / „gepflegte Werte") — und der Vorschlag zur
Entscheidung. Excel führt dieselbe Annahmenzeile unter jeder Blocküberschrift und
den Vorschlag unter den drei Blöcken.

### 9.3 G7 — Betrachtungszeitraum gegen die Nutzungsdauern

Aus T und den Investitionspositionen des Erwartet-Laufs entsteht eine Hinweiszeile:
kürzeste und längste **gepflegte** Nutzungsdauer (n < 1 heißt „wie T" und zählt
nicht), dazu „Restwert am Ende angesetzt" (T < längste), „Ersatzbeschaffung im
Jahr n" (T > kürzeste, gerundet wie im Rechenkern) oder „deckungsgleich". Ohne
gepflegte Dauer: „kein Ersatz, kein Restwert". **Kein Blocker, reiner Ausweis.**

### 9.4 G10 und die Vereinfachungen G1/G3/G5

Zwei Sätze im Nachweisblock (Seite und Word-Bericht):

* **G10** — Eigenverbrauchsquote und Einspeiseanteil sind aus der
  **Stundensimulation abgeleitet**, nicht als Annahme gesetzt (nur mit
  Photovoltaik in der Gruppe). Kein neuer Rechenweg.
* **G1/G3/G5** — „Vereinfachungen (VALERI): kein Endjahr je Kostenposition; keine
  Degradation außer beim PV-Ertrag; Energiekosten als Gesamtrechnung des
  Simulationslaufs ab Jahr 1 (FK10)." Eine Vereinfachung, die im Bericht steht,
  ist eine Annahme; eine, die nicht dasteht, ist ein Fehler.

### 9.5 G11 — die Basisregel der Prozentzeilen

> **Im Szenario ist die Bemessungsbasis der Zeilen „x % der Investitionssumme" die
> Kaskade MIT pauschalem Ausschlag auf die nicht gepflegten Investitionszeilen.**

Skaliert wird **je Zeile** (`InvestKaskade.BetragImSzenario`) — dieselbe Methode,
die auch `LiesInvestitionen` fragt, und damit dieselbe **Vorrangregel** wie § 2.2:
Eine gepflegte Zeile mit 7.000 € im Worst-Fall bleibt Basis 7.000 €. Eine pauschale
Multiplikation der Summe wäre die Doppelzählung, die § 2.2 ausschließt.

`satz == null` (Erwartet, jede Anzeige) betritt den Zweig gar nicht: **Der
Erwartungsfall bleibt bitgleich.** Best und Worst ändern sich für jedes Projekt mit
solchen Zeilen — Worst wird ungünstiger, Best günstiger. Genau das war der Zweck
des Entscheids; die Zahlen des Belegs stehen im Protokoll
(`iU9_W5_Blazor_Port_Protokoll.md`, Abschnitt W5‑B‑11).

---

## 10 Preisindizierung der Ersatzbeschaffung (p_I) — Etappe W5‑B‑12

### 10.1 Rechenregel

`KapitalwertRechner.Rechne` nimmt den **Preisänderungssatz der kapitalgebundenen
Kosten** p_I [%/a] entgegen (Parameter `preisstInvestProzent`, am Ende der Signatur,
Vorgabe 0). VDI 2067 Blatt 1 schreibt die kapitalgebundenen Kosten mit einem eigenen
Faktor fort; bis hierher trug EPOS-Plan den heutigen Betrag unverändert in jedes
Ersatzjahr (Vereinfachung W1, Lücke G4).

| Größe | Regel |
|---|---|
| Erstbeschaffung in t₀ | **nominal** — der eingegebene Betrag |
| verschobene Erstbeschaffung (KD6, StartJahr ≥ 2) | **nominal** — der Betrag gilt zum Zahlungszeitpunkt |
| Ersatzbeschaffung im Jahr tj | `A(tj) = A₀ · (1 + p_I)^tj` — Exponent ist das ABSOLUTE Jahr |
| Restwert zum Zeitpunkt T | Preisbasis der **letzten** Beschaffung × rest/n, abgezinst wie bisher |
| p_I = 0 | Rechenweg **bitgleich** zu vor dieser Etappe (Indexfaktor wird nicht gebildet) |

**Ein Satz für alle Positionen, nicht einer je Zeile.** Damit ist zugleich entschieden,
wie weit Lücke **G2** reicht: genau bis zu diesem dritten Topf neben p_B und p_E.

### 10.2 Nullsemantik

| Feld | NULL heißt |
|---|---|
| `Preissteigerung_Investition` (Erwartet) | **wie p_B** (`Preissteigerung_Betrieb`) — nicht „0 %“ |
| `Szen_Best_Preis_I` / `Szen_Worst_Preis_I` | Vorgabe = **Erwartet‑p_I ∓ 1 %‑Punkt** (Best −, Worst +) |
| `Nicht_Monetaer` | nichts erfasst (Berichtszeile entfällt); seit #479 Altfeld — die Wirkungen stehen in `Tab_ProjektWirkung` (§ 10.5) |

Eine 0 als Vorbelegung hätte behauptet, Investitionsgüter würden nie teurer — eine
Aussage, die niemand getroffen hat. Der einzige gepflegte Satz im Haus, der eine
allgemeine Kostensteigerung ausdrückt, ist p_B; deshalb der Rückfall dorthin.

> **Die Vorgaberegel je Szenario, ausgeschrieben** (präzisiert in Teil b):
> wirksames p_I(Szenario) =
> `satz.PreissteigerungInvestition ?? (p_I_erwartet + Richtung × 1 %‑Punkt)` mit
> `p_I_erwartet = p.PreissteigerungInvestition ?? p.PreissteigerungBetrieb`.

Der Bezugswert ist also das **Erwartet‑p_I**, nicht das p_B des Szenarios — dieselbe
∓1‑%‑Punkt-Regel wie bei p_E und p_B, angewandt auf den Erwartungswert der **eigenen**
Größe. Daraus folgen drei Fälle:

* **Regelfall** (p_I ungepflegt, Szenario‑p_B ungepflegt): Die Vorgabe ist genau das
  wirksame p_B des Szenarios. Bis Teil a stand hier nur dieser Satz — er ist die
  Wirkung, nicht die Regel.
* **Erwartet‑p_I gepflegt:** Die Bandbreite spannt sich um diesen Wert (p_I = 5 %/a →
  Best 4, Worst 6), unabhängig von p_B.
* **Nur das Szenario‑p_B gepflegt:** p_I folgt ihm **nicht**. Sonst zöge eine
  Betriebskostenannahme still die Ersatzbeschaffung mit — und niemand hätte das
  angegeben.

### 10.3 Ablage und Migrationsschritt 72

Vier **nullbare** Spalten an `Tab_ProjektWirtschaftlichkeit`
(`SchemaKatalog.Schritt72_ValeriErgaenzung`):

```
Preissteigerung_Investition   DOUBLE → REAL
Szen_Best_Preis_I             DOUBLE → REAL
Szen_Worst_Preis_I            DOUBLE → REAL
Nicht_Monetaer                MEMO   → TEXT (ohne Längenprüfung, Freitext, G6)
```

* **Kein DML, kein DDL-DEFAULT** — dasselbe Muster wie Schritt 70 und 71.
* **`MEMO` statt `TEXT(n)`:** Ein Fließtext, dessen Länge niemand vorhersagen kann; eine
  Längenprüfung schnitte ihn ab. `StilleDb.SqliteSpaltenTyp` übersetzt MEMO nach TEXT
  ohne CHECK — dieselbe Wahl wie bei `WQ_Wochenwerte`.
* **Am Projekt, nicht an der Variante:** Nicht monetäre Wirkungen beschreiben die
  Maßnahme als Ganzes.
* **Die Rückfallebene `SchemaKatalog.Alle` führt die Spalten nicht** — wortgleiche
  Begründung wie bei den Schritten 20, 21, 28 und 71: Kein Rechenkern der Simulation
  liest sie, p_I erreicht den `KapitalwertRechner` als PARAMETER. Der Schemaschritt ist
  ihr einziger DDL-Ort.
* **Ergebnisneutral als Schritt.** Er legt Spalten an. Erst wenn der Parametersatz sie
  liest (Teil b), rechnen Bestandsprojekte mit Ersatzbeschaffung mit p_I = p_B; ihre
  Kapitalwerte sinken dann leicht — gewollt, denn der bisherige Ausweis war der zu
  günstige. Projekte ohne Ersatzbeschaffung bleiben zahlengleich. Der Referenzlauf ist
  nicht berührt.
* **Zielstand dieser Etappe:** `SchemaStand.Zielversion = 72`. Systemimmanent weist
  `ProjektExportImportCtrl` damit `.wpx`-Pakete auf Stand 71 ab. Der **heutige** Schemastand ist
  ein anderer (24.09.2026: Zielversion 118, neue Schritte ab 119) — die 72 beziffert, womit diese
  Etappe abgeschlossen wurde, nicht den Stand des Programms.

### 10.4 Parametersatz und Dialog (Teil b)

`WirtschaftlichkeitParameter` führt seit Teil b `PreissteigerungInvestition`
(`double?`) und `NichtMonetaer` (`string`); die Nullsemantik steht an **einer**
Stelle: `PreisInvestWirksam => PreissteigerungInvestition ?? PreissteigerungBetrieb`.
`SzenarioSatz` bekommt dieselbe Größe als **siebte**; `NurVorgaben`, `Kopie`,
`Vorgabe(szenario)` und `Nachweis(p, kultur)` führen sie mit.

`FuerSzenario` ersetzt p_I wie Zins, p_E und p_B — und zwar als **gepflegten** Wert in
der Kopie. Bliebe das Feld dort `null`, fiele die Kopie über `PreisInvestWirksam` auf
ihr eigenes, bereits ersetztes p_B zurück; das Szenario rechnete an seinem eigenen Satz
vorbei. In den Rechenkern kommt der Satz an **einer** Stelle:
`WirtschaftlichkeitCtrl.RechneBild` übergibt `p.PreisInvestWirksam` als letzten
Parameter an `KapitalwertRechner.Rechne`. Das ist der einzige Aufrufer — Hauptlauf,
Verlauf und Sensitivität gehen alle dort durch.

Gelesen und geschrieben werden die vier Spalten im vorhandenen Weg (`LadeParameter`,
`LiesSatz`, UPDATE und INSERT von `SpeichereParameter`); der Freitext geht als
`LongVarWChar` (MEMO) und nicht als `VarWChar` — sonst schnitte ihn der Access-Rückweg
bei 255 Zeichen ab. `StelleTabellenSicher` legt weder die Tabelle an noch zieht sie
diese Spalten nach; Schemaschritt 72 ist ihr einziger DDL-Ort.

> **Grenze, bewusst gezogen:** `Tab_ErgebnisWirtschaftlichkeit` bekommt **keine**
> p_I-Spalte. Die Annahmenzeile der Berichte entsteht aus dem **Parametersatz**, nicht
> aus der Ergebniszeile; eine Ergebnisspalte wäre eine zweite Wahrheit für dieselbe
> Zahl. Das unterscheidet p_I bewusst von Zins, p_E und p_B (§ 2.4, V3): Die drei
> werden je Ergebniszeile persistiert, weil sie dort schon Spalten hatten.

Im **Dialog** steht p_I unter „Allgemein" neben p_B — als einziges Zahlenfeld des
Blocks **ohne** Rückfall auf den alten Wert, denn ein leeres Feld ist hier eine Aussage.
Die Herleitungszeile darunter nennt immer den wirksamen Wert und seine Herkunft
(„wie Betrieb" / „gepflegt"); ohne sie bliebe offen, ob leer „0 %/a" oder „wie p_B"
heißt. In der Szenariotabelle kommt die siebte Zeile dazu (§ 4).

Das Freitextfeld **„Nicht monetäre Wirkungen"** steht in einem eigenen Abschnitt
„Bewertung nach DIN EN 17463" und nicht unter „Allgemein": Dort stehen Rechengrößen —
dieser Text rechnet nichts, er steht neben der Zahl, so wie die Norm es verlangt.
Gebaut ist er aus dem Hausbaustein `Textfeld` (mehrzeilig); ein zweiter Baustein wäre
ein zweiter Ort für dieselben Regeln, und ein eigener CSS-Block war nicht nötig.

### 10.5 Bericht, Seite und Freitext (Teil b)

* **`WIRT_SZ_QUELLEN`** (Hinweis über der Szenarientabelle) nennt p_I als dritte
  Quelle samt Nullsemantik — deutsch und englisch.
* **Annahmenzeilen.** `SzenarioSatz.Nachweis` führt „· p_I = x,x %/a",
  `WirtschaftlichkeitParameter.Nachweis` führt „Investition/Ersatz x,x %/a (gepflegt |
  wie Betrieb)". Dialog, Seite, Word und Excel wachsen dadurch aus **einer** Quelle mit.
* **Der Satz „Ersatzbeschaffungen nominal konstant"** im Parameternachweis des
  Word-Berichts war bis Teil a richtig und ist es jetzt nur noch bei p_I = 0. Er heißt
  deshalb „Ersatzbeschaffungen preisindiziert mit p_I (VDI 2067)" bzw. „… nominal
  konstant (p_I = 0)". Eine Annahme, die im Bericht steht, ist eine Annahme; eine, die
  falsch dasteht, ist ein Fehler.
* **G6 — der Freitext erscheint dreimal, jedes Mal nur wenn gepflegt:** im Word-Bericht
  als Überschrift 2 + Absatz unmittelbar **nach dem Vorschlagssatz** (erst die Zahl mit
  ihrer Bandbreite und der Empfehlung, dann das, was die Zahl nicht fassen kann), in
  Excel als Zelle unter der Vorschlagszelle, auf der Seite im **Kopf des
  Bewertungsblocks**, der ihn auch pflegt (`WirtschaftlichkeitStand.NichtMonetaer`).
  Er hängt am **Stand** und nicht an der Ansicht: Der Text beschreibt die Maßnahme,
  nicht ein Szenario und nicht eine Vergleichswahl.
* **Auf der Seite steht er je Zustand an genau EINER Stelle.** Zugeklappt weist der Kopf
  des Blocks ihn neben seinem Titel aus, mit dem vollen Text im `title`; aufgeklappt
  trägt der Kopf nur seinen Titel, weil der Text dann im Feld darunter steht. Gekürzt
  wird dabei im Browser (`.epos-modulparameter-ausweis`, `text-overflow`) und nicht im
  Quelltext — eine zweite Kürzungsregel wäre eine zweite Wahrheit. **Die Berichte sind
  davon unberührt:** Word und Excel lesen `WirtschaftlichkeitParameter.NichtMonetaer`
  selbst und formulieren mit `WIRT_NM_TITEL` bzw. `WIRT_NM_ZEILE` ihre eigene Ausgabe.
* **Ohne gepflegten Text entfällt der ganze Block**, Überschrift eingeschlossen. Eine
  leere Überschrift wäre keine Aussage, sondern eine Lücke mit Titel.
* **Vom Freitext zur Liste (#479, E17 — V‑G11 des konsolidierten Konzepts).** Der Freitext
  ist durch eine Liste der Wirkungen abgelöst: Schemaschritt 127 legt `Tab_ProjektWirkung`
  an — je Wirkung Kategorie (Energiefluss, finanziell, sonstig; DIN EN 17463, 6.1),
  Beschreibung, Dauer (kurz, mittel, lang) und die Wirkung auf Organisation, Mitarbeiter
  und Umwelt (keine bis stark); die Beurteilung nach 8.2 ist Dauer × stärkste Wirkung
  (0 bis 9), eine Anzeige, nicht gespeichert. Ein gepflegter Freitext wird dabei eine
  Wirkung „sonstig" ohne Beurteilung; das Feld `Nicht_Monetaer` bleibt als Altfeld lesbar
  unter der Liste, keine Maske schreibt es mehr. Auf der Seite steht an seiner Stelle der
  Baustein `WirkungenListe` im Bewertungsblock; Word und Excel zeigen in dem Abschnitt, der
  den Freitext trug, die **Tabelle** der Wirkungen (Quelle `WirtschaftlichkeitBewertung.Wirkungen`),
  ohne Wirkung entfällt der Block. Die Regeln oben — am Stand, nicht an der Ansicht, eine
  Stelle je Zustand — gelten für die Liste weiter. Keine Rechenwirkung.

### 10.6 Wirkung auf den Bestand

Mit Teil b rechnen Bestandsprojekte **mit Ersatzbeschaffung und p_B ≠ 0** erstmals mit
p_I = p_B; ihre Kapitalwerte sinken leicht. Das ist gewollt — der bisherige Ausweis war
der zu günstige. **Projekte ohne Ersatzbeschaffung und alle Projekte mit p_B = 0 bleiben
bitgleich**; wer den alten Ausweis behalten will, trägt p_I ausdrücklich mit 0 ein. Der
Referenzlauf ist nicht berührt: Er rechnet Simulationen, keine Wirtschaftlichkeit.

---

## 11 Entscheide vom 18.09.2026 mit Wirkung auf dieses Papier

Drei Entscheide zur Ergebnisansicht (konsolidiertes Konzept § 2.11.4, § 2.11.6, § 2.11.7,
§ 2.13) berühren die Etappen dieses Papiers; das Papier beschreibt weiterhin den gebauten Stand
W5‑B‑9 bis W5‑B‑12, den Stand der drei Entscheide nennt die Tafel.

**Stand 24.09.2026: V-4 ist erledigt, K8/V-1 sind gebaut — der Umschalter mit E5 (#434), der
Verlauf mit drei Szenarien und der Wegfall des Knopfes „Verlauf…" mit E6 (#436), der Verlauf auch in
Block 4 der Darstellung „ValERI-Bewertung" mit E8 Teil a (#454); V-G10 ist mit E8 Teil b (#455) gebaut; die
Szenarioabdeckung selbst (V-E) ist mit E9 gebaut (Teil a im Kern #461, Teil b in den Dialogen #462), der
Hinweistext ist entfallen** —
eingeordnet im Etappenplan **E0–E12** des
Analysepapiers ([`Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md) § 5):

| Entscheid | Etappe | Bemerkung |
|---|---|---|
| **V-4** Hinweistext bis zur vollständigen Szenarioabdeckung | Hinweistext **gebaut #434** (E5, U10); die Abdeckung im Kern **gebaut #461** (E9 Teil a), die Pflege in den Dialogen **gebaut #462** (E9 Teil b) — der Hinweistext ist **entfallen #462** | `WIRT_SZEN_HINWEIS` in beiden Sprachen, Wortlaut nach **A14** (Konzeptfassung ohne Roadmap-Satz); die Zahlen im Text sind die wirksamen — ohne Pflege die Vorgaben aus § 2.1, sonst die gepflegten Werte; unter der Annahmentafel und in Wort- und Tabellenbericht; nach #461 stimmte er nur noch für Projekte ohne Pflege von Zeitraum, Menge, Trägerpreisen und Erlössätzen; mit #462 steht an seiner Stelle der Ausweis „n von m Parametern szenariert" |
| **K8 / V-1** Umschalter „Kennzahlen / ValERI-Bewertung" | Umschalter **gebaut #434** (E5); der Verlauf mit drei Szenarien und der Wegfall des Knopfes „Verlauf…" **gebaut #436** (E6); derselbe Verlauf in Block 4 **gebaut #454** (E8 Teil a, E6‑Q1) | Der Verlauf steht als Abschnitt in „Wie sicher ist das?" und rechnet je Version alle drei Szenarien über einen frei wählbaren Zeitraum (§ 7.1); die Fußleiste trägt höchstens vier Knöpfe |
| **V-G10** Der ganze Bericht formelbasiert, soweit ableitbar | **gebaut #455** (E8 Teil b, V-D; V-C gebaut #454) | Stufenplan 0–3 im konsolidierten Konzept § 2.11.6, alle vier Stufen gebaut: der Parameterblock je Szenario aus echten Zellen, die Mehrjahrestabellen und die Kennzahlen des Erwartungsfalls in Formeln; die Blöcke Günstig und Ungünstig bleiben Werte (E8b‑Q1) |

**Mit #405 (E2) ist aus diesem Umkreis erledigt:** die Bandbreite im Bericht mit Spalte „Spanne" und
Referenzzeile (G8, § 9.2) in Word **und** Excel, der Zeitraumhinweis im Excel-Blatt (G7, § 9.3) und
der Empfehlungssatz mit der gewählten Referenz (G9, § 9.1). Der Excel-Bericht ist mit E8 Teil b (#455)
eine **Formelmappe**; die Annahmenzeile je Blocküberschrift und die Bandbreite als drei Blöcke (G8) stehen darin
unverändert.

* **V-4 — vollständige Szenarioabdeckung erst nach der Darstellungsetappe, mit
  Hinweistext.** Der Parametersatz aus § 2 bleibt, wie er ist: Best und Worst ersetzen
  Zins, p_E, p_B und p_I (`FuerSzenario`) und wirken in der Eingabe auf Investition,
  Erträge und Nutzungsdauer ungepflegter Positionen; Betrachtungszeitraum, Trägerpreise,
  Erlössätze, Mengen und gesetzliche Sätze bleiben in allen drei Szenarien gleich. Bis die
  vollständigen Sätze kommen (Rahmen, Trägerpreise, Erlössätze, Mengenfaktor), sagt ein
  Hinweis unter der Annahmentafel der Seite genau das; Wortlaut im konsolidierten Konzept
  § 2.11.7. Die Statuszeile aus § 5 bleibt daneben bestehen. **Gebaut #434** als
  `WIRT_SZEN_HINWEIS`. **Mit E9 Teil a (#461)** liest der Kern die vollständigen Sätze (Rahmen,
  Trägerpreise, Erlössätze, Mengenfaktor); ohne Pflege bleibt alles wie oben beschrieben. **Mit E9
  Teil b (#462)** kommt die Pflege in die Dialoge (Zeilen 8 und 9 der Szenariotafel, ±-Knopf an
  Trägerpreisen und Erlössätzen), und der Hinweis ist entfallen; an seiner Stelle steht der Ausweis
  „n von m Parametern szenariert".
* **K8 / V-1 — Umschalter „Kennzahlen / ValERI-Bewertung" im Kopf der Seite** statt
  eines weiteren Knopfes. Die Ergebnisansicht bringt den kumulierten Barwert der Differenz
  mit allen drei Szenarien in einem Bild (Farbe = Variante, Strichart = Szenario) auf die
  Seite; der Knopf „Verlauf…" entfällt damit. **Gebaut: der Umschalter mit #434, das
  Verlaufsbild und der Wegfall des Knopfes mit #436** — der Verlauf rechnet je Version alle drei
  Szenarien mit frei wählbarem Horizont (§ 7.1).
* **V-G10 — der ganze Bericht formelbasiert, soweit ableitbar** (abweichend von der
  Empfehlung „nur das ValERI-Blatt"; kippt V-2). Was formelfähig ist und was dauerhaft Wert
  bleibt, steht als Stufenplan im konsolidierten Konzept § 2.11.6; Stufe 0 ist ein
  Parameterblock aus echten Zellen — genau die Größen aus § 2 und § 4 dieses Papiers, je
  Szenario ein Satz: Kalkulationszins, Betrachtungszeitraum, p_E, p_B und p_I, dazu die
  Änderungen an Investition, Erträgen und Nutzungsdauer; die Spalte Erwartet trägt die Namen,
  auf die die Formeln der Mappe zeigen. **Gebaut #455** (E8 Teil b) — alle vier Stufen. **Die
  Stufen 0 bis 3 zählt § 2.11.6 des konsolidierten Konzepts; hier ist nur Stufe 0 beschrieben.**

### 11.1 Zuordnung der Etappen W5‑B‑9…W5‑B‑12 zu V-A…V-E

Das konsolidierte Konzept führt für dieselbe Arbeit die Reihe **V-A…V-E** (§ 2.11.4). Damit
niemand zweimal baut:

| hier | konsolidiertes Konzept | Stand |
|---|---|---|
| **W5‑B‑9** Parametersatz je Szenario (§ 2, Migrationsschritt 71) | Teil von **V-E** (vollständige Szenarioabdeckung) | gebaut — V-E für Rahmen (Betrachtungszeitraum), Trägerpreise, Erlössätze und Mengenfaktor **gebaut #461/#462** (E9: Teil a im Kern mit den Schemaschritten 116 bis 118, Teil b die Pflege in den Dialogen und der Ausweis „n von m Parametern szenariert" an der Stelle des Hinweistexts) |
| **W5‑B‑10** VALERI-Abgleich (§ 7) | Grundlage der Gap-Tafel **V-G1…V-G12** (§ 2.11.2) | gebaut |
| **W5‑B‑11** Umsetzung der Entscheidungen (§ 9) | **V-B** ≡ Etappe „VG" der Statuszeile **#358** (wählbare Referenz, Schemaschritt 92) | gebaut |
| **W5‑B‑12** Preisindizierung p_I und Freitext (§ 10, Migrationsschritt 72) | Teil von **V-E** (p_I) und **V-G11** (Freitext) | gebaut — V-G11 **vollständig mit #479** (E17, Schemaschritt 127): die Liste mit Kategorie und Beurteilung löst den Freitext ab (§ 10.5) |
| — | **V-A** Ausweis („nachrichtlich", Zinsfuß-Warnung, Deklarationen, Steigung) | gebaut — E5, **#434** |
| — | **V-C** ValERI-Ansicht, **V-D** XLSX-Formelbericht | V-C gebaut — E8 Teil a, **#454**: die fünf Blöcke vollständig (die Blöcke 1, 3, 4 und 5 mit #434 vorgezogen, das Cashflow-Bild mit #436 als Verlauf unter „Wie sicher ist das?", mit #454 Block 2 samt Zahlungsstrombild und Block 4 mit Spannenbild und Verlauf); V-D gebaut — E8 Teil b, **#455**: die Formelmappe in den Stufen 0 bis 3, die Anhang-E-Checkliste und die Anhang-D-Gegenprobe |

**Nummernvorsicht:** Die Lückennummern `G1…G11` dieses Papiers und `V-G1…V-G12` des
konsolidierten Konzepts meinen bei gleicher Ziffer Verschiedenes (G2 Preisänderung je Kostenart
gegen V-G2 Degradation; G6 nicht monetisierbare Wirkungen gegen V-G6 Sensitivität; G11
investitionsgekoppelte Betriebskosten gegen V-G11 nicht monetisierbare Wirkungen). Die
Übersetzungstafel steht am Anfang von § 2.11.2 des konsolidierten Konzepts, das als führende
Fassung die `V-G`-Nummern setzt.
