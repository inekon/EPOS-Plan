# Konzept — Szenarioparameter und VALERI-Abgleich (Wirtschaftlichkeit)

**Anwenderentscheid 09.09.2026** · Etappen **W5‑B‑9** (Szenarioparameter) und
**W5‑B‑10** (VALERI-Abgleich nach DIN EN 17463).

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
  21, 28): Kein Rechenkern der Simulation liest sie, und die tolerante Vorsorge
  steht unmittelbar vor dem Zugriff in
  `WirtschaftlichkeitCtrl.StelleTabellenSicher`.
* **Ergebnisneutral für Erwartet**, verändernd für Best/Worst — und das ist der
  Zweck des Schritts. Der Referenzlauf ist nicht berührt: Er rechnet
  Simulationen, keine Wirtschaftlichkeit.

---

## 4 Dialog „Parameter…" — Abschnitt „Szenarien"

Drei Spalten, sechs Zeilen:

| | Erwartet | Best | Worst |
|---|---|---|---|
| Kalkulationszins [%] | *Projektwert, nur Anzeige* | Feld | Feld |
| Preissteigerung Energie [%/a] | *Projektwert* | Feld | Feld |
| Preissteigerung Betrieb [%/a] | *Projektwert* | Feld | Feld |
| Investition [%] | 0 | Feld | Feld |
| Erträge [%] | 0 | Feld | Feld |
| Nutzungsdauer [a] | 0 | Feld | Feld |

Die Erwartet-Spalte ist **Anzeige, kein Eingabefeld** — sie wiederholt, was im
Abschnitt „Allgemein" gepflegt wird („Kein Delegat ist kein Knopf"). Der Knopf
**„Vorgaben"** setzt alle zwölf Felder auf NULL zurück; die Herleitungszeile
nennt die geltende Regel und ob der Satz aus Vorgaben oder aus gepflegten Werten
besteht.

---

## 5 Seite „Wirtschaftlichkeit"

Die Szenariowahl bleibt, wie sie ist. Ergänzt wird die **Statuszeile** unter der
Wahl: Sie nennt für das gewählte Szenario den wirksamen Satz und seine Herkunft —
„Vorgaben" oder „gepflegt" —, für Erwartet den unveränderten Projektparametersatz.
Die Kennzahltabelle zeigt je gewählter Version die Zahlen des gewählten Szenarios;
Restwert und Ersatzbeschaffungen stehen seit W5‑B‑10 als eigene Zeilen darin.

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
| Zahlungsströme je Jahr | `Zahlungsbild.NominalReihe`/`BarwertReihe` plus die Einzelreihen (Betrieb, Endenergie-Anteil, Energie, CO₂, Ersatz, Einspeiseerlös, benannte Erlösreihen) seit E7; Mehrjahrestabelle im Bericht |
| Betrachtungszeitraum T | Projektparameter, 1…50 a; der Verlaufsdialog rechnet frei wählbare Horizonte |
| Diskontierungszins | Projektparameter, seit W5‑B‑9 je Szenario |
| Restwert am Ende von T | linear je Position, abgezinst (`RestwertBarwert`) |
| Ersatzinvestition bei n < T | in t = n, 2n, … (`ErsatzJeJahr`) |
| Preisentwicklung | p_E (Energie, CO₂, Endenergie-Topf) und p_B (Betrieb); CO₂-Preispfad und PV-Vergütung jahresscharf |
| Förderungen | Investitionszuschuss (K5, I₀-mindernd), KWKG-Zuschlag und drei Steuergutschriften als jahresscharfe Erlösreihen |
| Sensitivitäten | Zins, Energiepreissteigerung, Investition, Energiekosten, „KWKG-Bonus entfällt" |
| Bandbreite Worst/Erwartet/Best | Zeilenwerte (VALERI-Muster) **und** Parametersatz (W5‑B‑9) |
| Weitere Kennzahlen | Annuität, dynamische Amortisation, interner Zinsfuß, Wärmegestehungskosten |
| Referenzfall | Stammprojekt; Varianten werden als **Differenz** zum Stamm bewertet — genau die VALERI-Sicht „Maßnahme gegen Weiterbetrieb" |
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
| **G2** | **Preisänderung je Kostenart.** Heute zwei Töpfe (p_B, p_E) plus CO₂-Pfad. | VALERI führt eine eigene Preisänderung je Faktor. | Spalte je Zeile + Rechenkern; Datenmodell | **W5‑B‑12** — nur als dritter Satz p_I, nicht je Zeile |
| **G3** | **Degradation je Faktor.** Nur die PV-Ertragsdegradation ist modelliert. | VALERI führt eine Degradation je Nutzen-/Lastenfaktor. | Spalte je Zeile; Datenmodell | **nicht umsetzen** — offenlegen (§ 9.4) |
| **G4** | **Preisindizierung der Ersatzbeschaffung.** Ersatz wird nominal unverändert angesetzt (Vereinfachung W1). | VDI 2067/VALERI setzen Ersatzbeschaffungen üblicherweise preisindiziert an. | Rechenkern; **fachlicher Entscheid** | **W5‑B‑12** — Preissteigerungssatz p_I, Migrationsschritt 72 |
| **G5** | **Startjahr für die Energiekosten.** Die Simulation kennt keine Startjahre je Komponente; die Energiekosten sind die Gesamtrechnung des Laufs (dokumentierte Vereinfachung FK10). | VALERI aktiviert jeden Faktor ab seinem Betriebsjahr. | Simulation; groß | **nicht umsetzen** — offenlegen (§ 9.4) |
| **G6** | **Nicht monetisierbare Wirkungen.** Kein Freitextfeld für Komfort, Versorgungssicherheit, Arbeitssicherheit. | VALERI verlangt eine qualitative Beschreibung im Bewertungsbericht. | Feld + Berichtsbaustein | **W5‑B‑12** — Freitextfeld |
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

Maßstab ist die **Kapitalwertdifferenz zum Stamm** (`KapitalwertDiff`), nicht der
absolute Kapitalwert: Der Stamm ist die Unterlassensalternative (§ 7.1,
Referenzfall).

| Stufe | Bedingung |
|---|---|
| **empfohlen** | ΔKW > 0 in Worst, Erwartet und Best |
| **bedingt empfohlen** | ΔKW > 0 in Erwartet, aber ≤ 0 in Worst (oder in Best) |
| **nicht empfohlen** | ΔKW ≤ 0 in Erwartet |
| Zusatz **„Bandbreite nicht berechnet"** | Best oder Worst fehlt → Urteil allein nach Erwartet |

**Gesamtvorschlag:** die höchste Erwartet-Differenz unter den *empfohlenen*, sonst
unter den *bedingt empfohlenen*, sonst der Satz „Keine Variante ist gegenüber dem
Stammprojekt wirtschaftlich; Weiterbetrieb (Referenzfall)." Ohne Variante mit
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
| `Szen_Best_Preis_I` / `Szen_Worst_Preis_I` | Vorgabe = das **wirksame p_B** desselben Szenarios |
| `Nicht_Monetaer` | nichts erfasst (Berichtszeile entfällt) |

Eine 0 als Vorbelegung hätte behauptet, Investitionsgüter würden nie teurer — eine
Aussage, die niemand getroffen hat. Der einzige gepflegte Satz im Haus, der eine
allgemeine Kostensteigerung ausdrückt, ist p_B; deshalb der Rückfall dorthin.

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
  liest sie, p_I erreicht den `KapitalwertRechner` als PARAMETER. Die tolerante Vorsorge
  steht in `WirtschaftlichkeitCtrl.StelleTabellenSicher`.
* **Ergebnisneutral als Schritt.** Er legt Spalten an. Erst wenn der Parametersatz sie
  liest (Teil b), rechnen Bestandsprojekte mit Ersatzbeschaffung mit p_I = p_B; ihre
  Kapitalwerte sinken dann leicht — gewollt, denn der bisherige Ausweis war der zu
  günstige. Projekte ohne Ersatzbeschaffung bleiben zahlengleich. Der Referenzlauf ist
  nicht berührt.
* **Zielstand:** `SchemaStand.Zielversion = 72`. Systemimmanent weist
  `ProjektExportImportCtrl` damit `.wpx`-Pakete auf Stand 71 ab.
