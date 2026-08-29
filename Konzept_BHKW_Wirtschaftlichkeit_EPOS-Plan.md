# Konzept: BHKW-Wirtschaftlichkeit EPOS-Plan — Kostentransparenz, Steuerlast und Anlagenabgrenzung

**Rev. 1 — 29.08.2026 — zur Abnahme durch Philipp**

Auftrag: Die Wirtschaftlichkeit für BHKW soll in EPOS-Plan gerechnet werden. Maßgeblich sind die
Investitionskosten (liegen im Dialog vor), die Energiekosten mit der nach KWKG reduzierten
**Energiesteuer** des BHKW-Brennstoffs, die **Stromsteuer** auf bezogenen Strom, Hilfsstrom und
eingespeisten Strom, sowie die **Vergütungen** für selbst erzeugten und eingespeisten Strom. Für
**energieintensive Unternehmen** sind Energiesteuer und Stromsteuer reduziert — letztere wirkt damit
auch auf die vermiedenen Stromkosten. Dem Anwender ist **transparent darzustellen**, welche Kosten in
die Rechnung eingehen; Energie- und Stromsteuer sind aus den Energiebezugskosten regelmäßig nicht
ersichtlich. Die Abbildung aus BHKW-Plan dient als Ansatz. **Andere Anlagen (Wärmepumpe usw.) müssen
separat betrachtet werden.**

**Grundlagen dieses Konzepts:**
- [`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](Grundlagen_KWKG_Energiesteuer_Stromsteuer.md) — Rechtsstand 18./19.08.2026 mit Quellen
- [`Allgemein/Reporting/Konzept_BHKW_Kosten_Erloese.md`](WindowsFormsApplication1/Allgemein/Reporting/Konzept_BHKW_Kosten_Erloese.md) und [`W4_Umsetzungsstand.md`](WindowsFormsApplication1/Allgemein/Reporting/W4_Umsetzungsstand.md) — die abgenommene Ausbaustufe W4
- [`Allgemein/Reporting/Analyse_Altanwendung_BHKW-Plan.md`](WindowsFormsApplication1/Allgemein/Reporting/Analyse_Altanwendung_BHKW-Plan.md) — Rechenwege und 17 Fehler der Excel-Anwendung
- [`Konzept_Kostendialoge_EPOS-Plan.md`](Konzept_Kostendialoge_EPOS-Plan.md) § 6.1 (Reiter „Ertrag/Bonus", umgesetzt KD5) und [`Konzept_Photovoltaik_Wirtschaftlichkeit_EPOS-Plan.md`](Konzept_Photovoltaik_Wirtschaftlichkeit_EPOS-Plan.md) (Muster für einen anlagenbezogenen Wirtschaftlichkeitsdialog)
- zwei Code-Erhebungen vom 29.08.2026 (Rechenkette und Dialoge, mit Datei:Zeile-Belegen am Arbeitsstand `Pufferspeicher`)

**Pfadbasis:** `WindowsFormsApplication1\`, sofern nicht anders angegeben.

---

## 1 Einordnung: Das ist ein Delta, kein Neubau

Die BHKW-Wirtschaftlichkeit ist **überwiegend vorhanden**. Die Ausbaustufe W4 (Etappen E1–E8, plus
L12/L13, abgenommen 19.08.2026) und die Etappen K6/KD5 haben geliefert:

| Bereich | Stand | Fundstelle |
|---|---|---|
| Kapitalwert nach DIN EN 17463 mit benannten Erlösreihen | vollständig | `KapitalwertRechner.cs:67-99` |
| KWK-Zuschlag **je Modul**, marginale Leistungsstaffel § 7, Eigenstrom-Tatbestände § 6 Abs. 3, Vbh-Kontingent § 8, Jahresdeckel, Förderfähigkeit, Negativpreis-Abschlag | vollständig | `KwkgSatzRechner.cs:155-293`, `KwkgKontingentRechner.cs:74-174`, `WirtschaftlichkeitCtrl.cs:2222-2340` |
| Energiesteuer § 53 / § 53a Abs. 5 / § 54, einheitenrichtig (€/MWh · €/1.000 l · €/1.000 kg · €/GJ), Brennwertumrechnung, Nutzungsgradprüfung | vollständig | `SteuerGutschriftRechner.cs:208-448` |
| Stromsteuer: Befreiung § 9 Abs. 1 Nr. 3 (vier Bedingungen je Anlage, CO₂-Grenzwert auf den Energieertrag), Entlastung § 9b mit Sockel 250 €/a | vollständig | `SteuerGutschriftRechner.cs:483-646` |
| Tarif-Rollenmodell mit Differenzmethode „vermiedene Kosten" (Arbeit / Leistung / Summe) | vollständig | `StromTarifRechner.cs:246-267` |
| Gesetzesparameter als Katalog mit Gültig-ab-Jahr und Pflegemaske | vollständig | `GesetzKatalog.cs`, `Form_Gesetzesparameter` |
| Preiszerlegung des **Strom**bezugs in fünf Bestandteile | vorhanden, **Vorgabe AUS** | `ucStromAufschlaege.cs:143-347`, `Tab_ProjektWirtschaftlichkeit.Aufschlaege_Anwenden` |
| Ausweis in Reiter, Word und Excel aus **einer** Zeilendefinition | vollständig | `WirtschaftlichkeitZeilen.cs` |

**Was dieses Konzept ergänzt, ist genau das, was der Auftrag zusätzlich verlangt:** die
Sichtbarkeit der Steuerbestandteile im Energiepreis, die daraus folgende Kohärenz zwischen Belastung
und Entlastung, die Energieintensität als *eine* Wahrheit, den Hilfsstrom als eigene Größe und die
anlagenscharfe Abgrenzung gegenüber Wärmepumpe, Kessel, Solarthermie und Speicher.

**Abgrenzung.** Das Rechenverfahren bleibt (Kapitalwert, Stammprojekt = Unterlassensalternative).
Keine neue Simulation, keine Messdatenhaltung, kein neues Berichtsformat, keine Umsatzsteuer
(EPOS-Plan rechnet netto). Die PV-Vergütung bleibt beim PV-Konzept, die Speichererlöse beim
Stromspeicher-Konzept.

---

## 2 Befunde — was der Auftrag am Bestand trifft

### B1 Der Energiepreis sagt nicht, ob die Steuer darin steckt

Der Arbeitspreis eines Brennstoffs steht als **eine Zahl** in `energy_price.arbeitspreis` bzw.
`energy_project_settings.custom_price_work`. Ob der Anwender einen Bezugspreis **einschließlich**
Energiesteuer erfasst hat (der Regelfall einer Lieferantenrechnung) oder einen Nettopreis, ist
**nirgends erfasst**. Die Entlastung nach § 53/§ 53a wird trotzdem in voller Höhe gutgeschrieben.

Für Strom ist die Zerlegung vorhanden (`ucStromAufschlaege`: Netzentgelt · Umlagen · Stromsteuer ·
Konzession · Vertrieb, je mit Aktiv-Schalter, Live-Summe und rot markiertem „nicht aufgeschlüsseltem
Rest"), aber sie ist **standardmäßig ausgeschaltet** (`Aufschlaege_Anwenden`, Vorgabe AUS) und wirkt
nur auf den Strompreis. **Für Brennstoffe gibt es nichts Vergleichbares** — repoweit kein
Gas-Aufschlagsblock, kein ausgewiesener Energiesteueranteil.

> Das ist wörtlich der Punkt des Auftrags: „Energiesteuer und Stromsteuer sind oft nicht aus den
> Energiebezugskosten ersichtlich."

### B2 Entlastung ohne Belastung — und eine mutmaßliche Doppelzählung

Der Bestand kennt den Fall bereits und **meldet** ihn, statt ihn zu verhindern
(`WirtschaftlichkeitCtrl.cs:1801-1805`): Steht der Aufschlagsschalter auf AUS und ist § 9b aktiv,
wird die Stromsteuer entlastet, ohne dass sie je belastet wurde. Die Entscheidung, das nur zu
melden, war für E4/E5 richtig (Ergebnisneutralität); als Dauerzustand ist sie eine Fehlerquelle.

**Schwerwiegender ist ein zweiter Fall, den der Bestand nicht meldet.** Die Befreiung nach
§ 9 Abs. 1 Nr. 3 StromStG wird als Erlösreihe `STROMSTEUER_BEFREIUNG` mit
`20,50 €/MWh × KwkEigenMWh` gebucht (`SteuerGutschriftRechner.cs:573`). Diese Vorschrift ist aber
**keine Rückerstattung**, sondern der Umstand, dass auf selbst erzeugten und selbst verbrauchten
Strom gar keine Stromsteuer entsteht. Der wirtschaftliche Vorteil steckt bereits vollständig in der
**kleineren Bezugsrechnung** — genau die Größe, die im Kapitalwert als Reststrombetrag ansetzt
(`WirtschaftlichkeitCtrl.cs:1660`). Enthält der Bezugspreis die Stromsteuer, wird sie damit zweimal
gutgeschrieben.

Der Bestand hat denselben Gedanken für die vermiedenen Kosten bereits vollzogen — E5 hält
ausdrücklich fest: „Vermiedene Kosten sind Ausweis, kein Zahlungsstrom. Die Einsparung steckt bereits
in der kleineren Bezugsmenge; sie zusätzlich als Erlös zu buchen wäre eine Doppelzählung."
**Für die Stromsteuerbefreiung ist derselbe Schluss nicht gezogen worden.**

> **Belastbarkeit:** Der Befund ist aus den Rechenwegen hergeleitet, **nicht gemessen**. Er ist der
> erste Prüfschritt der Etappe B1 (§ 8) und braucht eine Zahlenprobe an einem Projekt mit
> Stundenreihen, bevor daraus eine Codeänderung wird. Auf § 53/§ 53a und § 9b trifft er **nicht**
> zu — das sind echte Rückerstattungen auf Mengen, die tatsächlich besteuert bezogen wurden.

### B3 „Energieintensiv" wird an drei Orten geführt und nirgends gekoppelt

| Ort | Wert | Wirkung |
|---|---|---|
| `Tab_ProjektWirtschaftlichkeit.Unternehmensart` | `KEIN_PROD_GEWERBE` / `PROD_GEWERBE` / `LAND_FORSTWIRTSCHAFT` | Voraussetzung § 9b StromStG und § 54 EnergieStG (`SteuerGutschriftRechner.cs:318-324`) |
| `StromAufschlagModel.STROMSTEUER_REDUZIERT = 0,050` ct/kWh | Schnellwahlknopf im Energieträgerdialog | senkt den Strompreis (`ucStromAufschlaege.cs:213-215`) |
| `Tab_Gesetzesparameter.STROMST_REGELSATZ` = 20,50 €/MWh | Katalog | Bemessung der Entlastung |

Der Knopf „reduziert" und die Unternehmensart wissen nichts voneinander: Man kann ein
produzierendes Gewerbe erfassen und den Regelsatz im Preis stehen lassen — oder umgekehrt. Dazu
kommt die bereits als Befund A7 geführte doppelte Wahrheit des Stromsteuersatzes (Konstante im
Modell gegen Katalogzeile). Für die Energiesteuer fehlt die Preisseite ganz (B1).

### B4 Die Steuerwahl gilt je Projekt — § 54 trifft deshalb den falschen Brennstoff

`Energiesteuer_Wahl` und `Aufteilung_Methode` stehen an `Tab_ProjektWirtschaftlichkeit`, also
**einmal je Projekt**. Die Bemessungsmenge stammt ausschließlich aus der BHKW-Anlagenliste
(`WirtschaftlichkeitCtrl.cs:2675-2772`). Für § 53 und § 53a ist das richtig — sie setzen am
Umwandlungsprozess an. **§ 54 EnergieStG entlastet dagegen Heizstoffe** (Erdgas 1,38 €/MWh), also
gerade den Kessel- und Spitzenlastbrennstoff, der in dieser Liste nicht vorkommt. Der Code weist die
Lücke aus (`DbWerte.cs:643-650`, Begründungszeile `STEUER_ENERGIEST_54_BEMESSUNG`), schließt sie aber
nicht. Praktisch heißt das: Ein produzierendes Gewerbe mit BHKW **und** Spitzenlastkessel bekommt für
den Kesselbrennstoff keine Entlastung ausgewiesen, obwohl sie ihm zusteht.

Hinzu kommt: Ein Projekt kann mehrere BHKW mit **verschiedenen Brennstoffen** führen (Erdgas und
Biogas), für die verschiedene Normen günstig sind. Die eine Projektwahl kann das nicht abbilden.

### B5 Hilfsstrom des BHKW existiert nicht

Weder `ErgebnisBHKWModel` noch `ErgebnisBHKWModulModel` führen einen Eigenbedarf. Eine Größe
„Hilfsstrom" gibt es nur für den Heizkessel (`ErgebnisModel.cs:231`), und die geht in die
Wirtschaftlichkeit nicht ein. Wirtschaftlich abgebildet ist Hilfsenergie ausschließlich als
VDI-2067-Betriebskostenposition „Hilfsenergiekosten", bemessen als **Prozentsatz der
Brennstoffkosten** (`BetriebskostenCtrl.cs:213-216`).

Das ist für eine Kostenschätzung vertretbar, für die Steuerrechnung nicht: Der Hilfsstrom ist eine
**Strommenge**, die entweder aus dem Netz bezogen wird (dann stromsteuerpflichtig, § 9b-fähig) oder
aus dem eigenen BHKW stammt (dann nach § 9 Abs. 1 Nr. 3 befreit, aber nicht KWKG-zuschlagsfähig, weil
der Zuschlag an der Nettostromerzeugung hängt). Der Auftrag nennt ihn ausdrücklich.

### B6 Alles außer BHKW und PV wird global bilanziert

| Größe | Zuordnung heute | Beleg |
|---|---|---|
| Brennstoffkosten | **BHKW und Heizkessel gemeinsam** je Energieträger summiert | `KostenEmissionRechner.cs:99-102` |
| Strombezugskosten | eine Reihe `NETZBEZUG`; Wärmepumpe, Speicher, Hilfsantriebe undifferenziert | `WirtschaftlichkeitCtrl.cs`, `StromMatrix.cs:35-56` |
| CO₂ / BEHG | Projektsumme über alle Feuerungen | `WirtschaftlichkeitCtrl.cs:1533` |
| Investition und Betriebskosten | je **eine** Summe; die Komponentenzuordnung ist Freitext und rein beschreibend | `WirtschaftlichkeitCtrl.LiesInvestitionen`, `WirtschaftlichkeitDaten.cs:799-801` |
| Gestehungskosten | annuisierter Kapitalwert ÷ **Gesamt**wärmebedarf | `WirtschaftlichkeitCtrl.cs:4028-4032` |

Anlagenscharf sind nur drei Pfade: KWKG, Steuerprüfung und PV. **Wärmepumpe, Kessel, Solarthermie und
Stromspeicher haben keinen eigenen Rechenpfad** — sie erscheinen lediglich als Erzeugerkennzeichen
zur Dialogsteuerung.

**Die gute Nachricht:** Mengenseitig ist die Trennung längst da. `ErgebnisBHKWModulModel` und
`ErgebnisHeizkesselModulModel` führen `Verbrauch [MWh/a]` **und** `CarrierId` je Modul,
`ErgebnisWaermepumpeModulModel` führt `Stromverbrauch` und `Heizstab` je Modul
(`Model/ErgebnisModel.cs:186-271`). Was fehlt, ist ausschließlich die **Auswertung** dieser Mengen.

### B7 Es gibt keine geschlossene Herleitungsansicht

Für BHKW existiert eine Herleitung nur (a) je Modul im `Form_KwkgModule` („Einspeisung 5,57 ct/kWh —
Tranchen …") und (b) als **verketteter Einzeiler** `SteuerHerkunft` im Ergebnisgrid. Eine Ansicht
„Woraus besteht dieser Erlös, woraus dieser Preis" auf Projektebene gibt es nicht.

Die Altanwendung hatte sie: `Dial_ErloesErg` zeigt vermiedenen Strombezug mit Aufteilung, vermiedene
Kosten getrennt nach Arbeit / Leistung / Summe / spezifisch, Einspeisung nach Winter/Sommer und
HT/NT sowie die Jahresboni auf einer Tafel. Das ist — bei allen 17 Rechenfehlern der Excel-Anwendung
— **das Zielbild, das übernommen gehört**.

---

## 3 Leitentscheidungen

| Nr. | Entscheidung | Begründung |
|---|---|---|
| **BW1** | **Ein Energiepreis wird in Bestandteile zerlegt, nicht nur als Summe geführt.** Für Brennstoffe entsteht ein Bestandteilsblock nach dem Muster von `ucStromAufschlaege`: Energiesteuer · CO₂-Anteil (BEHG) · Netz-/Messentgelt · Vertrieb, je mit Aktiv-Schalter, Live-Summe und ausgewiesenem Rest. | B1. Das Muster ist erprobt, lokalisiert und hat mit der Restzeile bereits die richtige Grundhaltung: Was nicht aufgeschlüsselt ist, wird als solches benannt statt unterstellt. |
| **BW2** | **Kohärenzregel: Eine Steuerentlastung wird nur gebucht, wenn dieselbe Steuer im angesetzten Energiepreis als Belastung enthalten ist.** Die Prüfung ist Teil des Rechenlaufs und erzeugt bei Verstoß eine Warnzeile mit Betrag — nicht bloß einen Hinweistext. | B2. Ohne diese Regel ist jede Steuergutschrift eine Behauptung über einen Preis, den niemand erfasst hat. |
| **BW3** | **§ 9 Abs. 1 Nr. 3 StromStG ist Ausweis, kein Zahlungsstrom** — wie die vermiedenen Kosten seit E5. Der Vorteil steckt in der kleineren Bezugsrechnung. Die Zeile bleibt sichtbar (sie ist fachlich bedeutsam), geht aber nicht mehr in den Kapitalwert ein. Umschaltbar über eine Projektangabe; **Vorgabe = „Ausweis" (BF1 entschieden 30.08.2026)** — die B1-Probe bestätigte die Doppelzählung und maß, dass kein Bestandslauf die Reihe bucht: die Vorgabe ist bestandsneutral. | B2. Ergebnisneutralität hat Vorrang vor der eigenen Herleitung — durch B1 belegt statt nur vermutet. |
| **BW4** | **Energieintensität ist eine Wahrheit.** `Unternehmensart` bleibt das führende Feld; der Schnellwahlknopf im Energieträgerdialog liest es und schlägt den passenden Satz vor, der Katalog liefert die Zahl. Die Konstante `StromAufschlagModel.STROMSTEUER_REGELFALL/_REDUZIERT` wird Rückfallebene, nicht Quelle. | B3 und der offene Befund A7 des Umsetzungsstands. |
| **BW5** | **Die Steuerwahl wandert an die Anlage**, mit Rückfall auf den Projektwert (Muster E6: `Tab_Energieanlagen` führt Stichtag und Inbetriebnahme je Anlage, NULL = Projektwert). § 54 bekommt zusätzlich die **Kesselanlagen** als Bemessungsgrundlage. | B4. Ohne das bleibt § 54 dauerhaft falsch bemessen und Mehrbrennstoffprojekte unabbildbar. |
| **BW6** | **Hilfsenergie ist Strom und wird als Menge geführt** — Anteil am Energieeinsatz der Komponente, alternativ fester Jahresbetrag; je Anlage. Sie mindert die zuschlagsfähige Nettostromerzeugung und geht in die Stromsteuerrechnung ein. Eine Deckungswahl gibt es nicht: Die Befreiung nach § 9 Abs. 1 Nr. 3 ist bilanziell und folgt den Anlagenbedingungen. Einzelheiten in 4.5, **festgelegt 29.08.2026**. | B5, Auftrag. |
| **BW7** | **Anlagenscharfe Zuordnung ohne zweite Rechnung.** Die Energie- und CO₂-Kosten werden je Anlage **aufgeschlüsselt**, nicht neu gerechnet: Die Summe der Anlagenanteile ist zeilengleich der heutige Projektwert. Die Aufteilung folgt den vorhandenen Modulmengen (`Verbrauch` × `CarrierId` je Modul, `Stromverbrauch` je WP-Modul). | B6. So entsteht Transparenz ohne Ergebnisänderung — und ohne eine zweite Wahrheit neben `KostenEmissionRechner`. |
| **BW8** | **Eine Herleitungstafel für alles**, gespeist aus **einer** Definition — der vorhandenen `WirtschaftlichkeitZeilen`-Mechanik, erweitert um Menge, Satz und Einheit je Zeile. Reiter, Word und Excel rendern dieselbe Tafel. | B7. Die dreifach-Doppelung der Kennzahlenliste ist mit E7 gerade erst aufgelöst worden; eine vierte Ausgabe darf nicht danebenstehen. |
| **BW9** | **Ein eigener Dialog „BHKW-Wirtschaftlichkeit"** nach dem Muster `Form_PhotovoltaikVerguetung` (Designer-basiert nach FK1/Ä6, Kopfband, Gruppen mit Herleitungslabel, Live-Vorschau aus dem *einen* Rechenweg). Die beiden BHKW-Gruppen verlassen `Form_WirtschaftlichkeitParameter`. | Der Parameterdialog trägt bereits rund 26 Felder in sechs Gruppen auf 445 px Breite und hat seine Erläuterungen in einem Sammelabsatz am Fuß. Weitere Felder sind dort nicht unterzubringen. |
| **BW10** | **Inhalte aus BHKW-Plan, Darstellung nach EPOS-Plan** — Fortschreibung von L9 des Kostenkonzepts. Übernommen wird das Zielbild `Dial_ErloesErg`; nicht übernommen werden Brutto-/Netto-Mischung, „oder"-Doppelfelder und die drei parallelen Bonus-Begrenzungen. | Auftrag; Befunde 1–17 der Altanwendungsanalyse. |

---

## 4 Rechenmodell

### 4.1 Preiszerlegung und Kohärenzprüfung

Je Energieträger und Projekt gilt künftig:

```
Arbeitspreis_erfasst [ct/kWh]
   = Energie-/Beschaffungsanteil
   + Energiesteuer- bzw. Stromsteueranteil      (aktiv/inaktiv, Wert, Quelle Katalog)
   + CO₂-Anteil (BEHG)                          nur Brennstoffe
   + Netz-/Messentgelt + Konzession + Vertrieb
   + nicht aufgeschlüsselter Rest               (wird benannt, nicht unterstellt)
```

Zwei Modi wie beim Strom: **aufgeschlüsselt** (Summe der Bestandteile ist der Preis) oder
**Gesamtwert** (Preis ist gesetzt, die Bestandteile bleiben sichtbar und lesbar — das
Transparenzmuster aus `ucStromAufschlaege.cs:308-324`).

Daraus die **Kohärenzprüfung** (BW2), die vor jedem Wirtschaftlichkeitslauf läuft:

| Fall | Befund | Folge |
|---|---|---|
| Entlastung gewählt, Steueranteil im Preis **aktiv** | konsistent | Gutschrift wie bisher, Zeile nennt den enthaltenen Satz |
| Entlastung gewählt, Steueranteil **inaktiv oder 0** | Entlastung ohne Belastung | Warnzeile **mit Betrag**: „Die Gutschrift von X €/a setzt voraus, dass der erfasste Preis die Steuer enthält. Im Preis ist sie nicht ausgewiesen." Gutschrift bleibt bestehen (Ergebnisneutralität), wird aber im Bericht markiert |
| Steueranteil aktiv, Entlastung **nicht** gewählt | Belastung ohne Entlastung — der wirtschaftlich ungünstige, aber zulässige Fall | Hinweiszeile mit dem entgangenen Betrag |
| Satz im Preis ≠ Katalogsatz des Jahres | Preis veraltet oder Sondertatbestand | Hinweiszeile mit beiden Sätzen |

Damit ist die Auftragsforderung erfüllt: Der Anwender sieht, **welche Steueranteile in seinen
Energiekosten stecken** und was sie für die Wirtschaftlichkeit bedeuten.

### 4.2 Energiesteuer, anlagenscharf

```
je Anlage a:
   Wahl(a)      = Tab_Energieanlagen.Energiesteuer_Wahl  ?? Projektwert          (BW5)
   Menge(a)     = Brennstoff(a) in der gesetzlichen Einheit des Satzes           (Bestand)
   Gutschrift(a)= Satz(Träger(a), Wahl(a), Jahr) × Menge(a)

   § 53   : Anlagen mit Stromerzeugung (BHKW)     — Bemessung nach Aufteilungsmethode
   § 53a  : Anlagen mit Stromerzeugung (BHKW)     — Gesamteinsatz, Nutzungsgrad ≥ 70 %
   § 54   : BHKW  UND  Heizkessel/Spitzenlast     — produzierendes Gewerbe, Sockel 250 €/a
```

Neu gegenüber dem Bestand sind allein die **Wahl je Anlage** und die **Kesselanlagen bei § 54**. Die
Ausschlussregeln bleiben: § 53 geht § 53a vor, § 53a Abs. 5 und § 54 schließen einander aus — die
Prüfung wird von der Projekt- auf die Anlagenebene gezogen und je Anlage begründet.

Bemessungsmenge der Kessel: `ErgebnisHeizkesselModulModel.Verbrauch` mit `CarrierId`, also dieselbe
Struktur wie beim BHKW. Der Sockelbetrag von 250 €/a wird **einmal je Projekt** abgezogen, nicht je
Anlage — er ist ein Kalenderjahresbetrag des Antragstellers.

### 4.3 Stromsteuer und Hilfsstrom

```
Nettostromerzeugung(a) = Stromproduktion(a) − Hilfsstrom(a)          (BW6, neu)

Hilfsstrom(a)  = Anteil(a) × Brennstoffeinsatz(a)    Anteil am Energieeinsatz, siehe 4.5
                 KEIN KWKG-Zuschlag auf diese Menge — der Zuschlag hängt an der Nettoerzeugung
                 steuerlich Teil des KWK-Eigenverbrauchs, sofern die Anlage die Bedingungen
                 des § 9 Abs. 1 Nr. 3 erfüllt (bilanziell, keine Anwenderwahl)

§ 9 Abs. 1 Nr. 3  : Satz × KWK-Eigenverbrauch × Anteil bestandener Anlagen     (Bestand)
                    → nach BW3 Ausweis statt Zahlungsstrom, umschaltbar
§ 9b              : max(0, Satz × Netzbezug − 250 €/a)                         (Bestand)
Belastungsseite   : Stromsteueranteil × Netzbezug aus der Preiszerlegung       (4.1)
```

Der KWKG-Zuschlag bemisst sich künftig auf die **Nettostromerzeugung**. Bei einem Hilfsstromanteil
von null ist das zeilengleich dem Bestand — die Vorbelegung ist deshalb null, und die Etappe bleibt
für Bestandsprojekte ergebnisneutral.

### 4.4 Vergütungen

Unverändert gegenüber W4/E5/E6, hier nur zur Vollständigkeit der Auftragsdeckung:

```
Zuschlag eingespeist (a) = Mischsatz(§ 7 Abs. 1, marginale Tranchen) × Einspeisemenge(a)
Zuschlag eigengenutzt(a) = Mischsatz(§ 7 Abs. 2) × Eigenmenge(a)
                           NUR bei Tatbestand nach § 6 Abs. 3
Sonderregel              = § 7 Abs. 3a, neue Anlagen ≤ 50 kWel: 16 / 8 ct/kWh, geht vor
Begrenzung je Jahr       = min(Vbh, Jahresdeckel, Restkontingent) × (1 − Negativpreisabschlag)
Pauschale § 9            = 4 ct × 60.000 Vbh × P_el, einmalig, schließt den laufenden Zuschlag aus
Einspeiseerlös           = Einspeisemenge × Einspeisepreis (Tarifstruktur vor Parameterwert)
```

**Eine Ergänzung** ist nötig: Der Eigenstrom-Tatbestand nach § 6 Abs. 3 wird heute nur gegen den
**Projekt**satz geprüft; der Anlagensatz `SatzEigenCt` bleibt unverändert bestehen
(`WirtschaftlichkeitCtrl.cs:2215-2218`). Bei gepflegten Modulsätzen kann so ein Eigenstromzuschlag
entstehen, obwohl kein Tatbestand vorliegt. Die Prüfung wird auf die Anlagenebene gezogen.

### 4.5 Hilfsenergie — eine Definition (festgelegt 29.08.2026)

**Hilfsenergie ist immer Strom für den Betrieb der Komponente.** Bemessen wird sie an der
**Endenergie der betrachteten Anlage** — Brennstoff bei BHKW und Heizkessel, Strom bei der
Wärmepumpe. Es gibt **drei gleichwertige Angabewege**, und die frühere Vorrangregel (Menge vor
Prozentsatz) ist damit gegenstandslos:

```
A  % der Endenergiekosten     Betrag [€/a] = Endenergiekosten(a) [€/a] × Satz / 100
                              Menge [kWh]  = Betrag / Strombezugspreis        (rückgerechnet)

B  % des Endenergiebedarfs    Menge [kWh]  = Endenergiebedarf(a) [kWh] × Satz / 100
                              Betrag [€/a] = Menge × Strombezugspreis

C  fester Jahresbetrag        Betrag [€/a] = Eingabe
                              Menge [kWh]  = Betrag / Strombezugspreis        (rückgerechnet)
```

> **Die Menge ist ein Ergebniswert, kein Eingabewert** (Festlegung 29.08.2026). Endenergiebedarf und
> Endenergiekosten stehen erst nach dem Simulationslauf fest. **Im Dialog wird ausschließlich der
> Satz gepflegt** — oder der absolute Jahresbetrag, und der ist der einzige Weg, der **ohne
> Simulation** funktioniert. Daraus folgt für den Rechenweg: Die Menge wird bei jedem Lesen frisch
> aus dem jüngsten Lauf geholt, nicht aus der Datenbank. Die Spalte `Tab_ProjektWerte.Menge` bleibt
> Ausweisgröße („Stand des Laufs vom …"), sie ist nicht die Rechenwahrheit — sonst rechnet die
> Anwendung nach einer neuen Simulation stillschweigend mit der alten Bezugsgröße weiter.

**Die Endenergie ist je Komponente eine andere Größe — und nicht jede Komponente hat eine:**

| Komponente | Endenergie = | zulässige Wege | Herkunft der Bezugsgröße |
|---|---|---|---|
| BHKW | Brennstoff | A · B · C | `Verbrauch` des Moduls × Trägerpreis |
| Heizkessel | Brennstoff | A · B · C | `Verbrauch` des Moduls × Trägerpreis |
| Wärmepumpe | Strom | A · B · C | `Stromverbrauch` + `Heizstab` × Bezugspreis |
| Solarthermie | keine — die Sonne kostet nichts | **nur C** | — |
| **Pufferspeicher** | keine | **nur C** | — |
| **Stromspeicher** | keine | **nur C** | — |
| **Photovoltaik** | keine — fachlich nicht einschlägig | **nur C**, Feld vorhanden | — |

**Zwei Bemessungsarten statt einer.** Weg A und Weg B unterscheiden sich in der Basis, nicht im
Ergebnisbegriff: `BEMESSUNG_PROZENT_ENDENERGIEKOSTEN` („% der Endenergiekosten") und
`BEMESSUNG_PROZENT_ENDENERGIEBEDARF` („% des Endenergiebedarfs"). Welche Größe die Endenergie einer
Komponente ist, löst der Bezugsgrößen-Auflöser auf; die Herleitungszeile nennt sie im Klartext —
„× 14.760,00 € Endenergiekosten · BHKW 1" bzw. „× 205.000 kWh Brennstoff · BHKW 1".

**Die Erfahrungswerte gelten für Weg A** (Kostenbasis): BHKW 2–4 %, Heizkessel 4–8 %. Probe am
Kessel: 6 % von 14.760 €/a Brennstoffkosten ergeben **885 €/a** — das ist die Größenordnung, die
zu einem Kessel dieser Klasse passt. Auf den Bedarf bezogen wären dieselben 6 % rund
12.300 kWh Strom und damit etwa das Dreifache; **die Prozentwerte der beiden Wege sind deshalb
nicht austauschbar**, der Faktor zwischen ihnen ist das Preisverhältnis Strom zu Brennstoff
(bei 24,60 gegen 7,20 ct/kWh rund 3,4). Der Dialog muss die Basis deshalb sichtbar benennen und
darf sie beim Umschalten der Bemessung **nicht stillschweigend übernehmen**.

**Was damit abgelöst wird.** `BEMESSUNG_PROZENT_BRENNSTOFFKOSTEN` und
`BEMESSUNG_PROZENT_STROMKOSTEN` (heute in allen Betriebsvorlagen) sind die Vorläufer von Weg A,
aber je Energieart getrennt und projektweit bemessen. Sie bleiben für Altdaten gültig und
verschwinden aus den Seeds; „Endenergie" fasst beide zusammen und ist zugleich anlagenscharf.

**Die Menge ist immer verfügbar — auch auf Weg A und C.** Weil Hilfsenergie definitionsgemäß Strom
ist, lässt sich aus jedem Betrag über den Strombezugspreis die Kilowattstundenzahl zurückrechnen.
Damit liefert **jeder** der drei Wege die Größe, die § 9 Abs. 1 Nr. 3 StromStG, § 9b StromStG und
die KWKG-Nettostromerzeugung brauchen (4.3). Die Rückrechnung wird in der Herleitung ausgewiesen,
damit sie nicht als gemessene Größe missverstanden wird.

**Zur steuerlichen Zuordnung.** Die Befreiung des Eigenstroms nach § 9 Abs. 1 Nr. 3 StromStG ist
eine **bilanzielle Größe aus der gesetzlichen Vorgabe**, nicht die Feststellung, dass eine bestimmte
Kilowattstunde physisch aus dem eigenen Modul kam. Sie folgt aus den Anlagenbedingungen, die
`SteuerGutschriftRechner` je Anlage ohnehin prüft (≤ 2 MW, hocheffizient, räumlicher Zusammenhang,
CO₂-Grenzwert). Eine Anwenderwahl „Deckung Netz oder Eigen" bildet das falsch ab und entfällt
deshalb — die frühere Spalte `Hilfsstrom_Deckung` aus 5.2 wird gestrichen.

> **Zwei Fallen bei den Speichern.** Erstens: Die **Umwandlungsverluste** eines Strom- oder
> Pufferspeichers sind **keine** Hilfsenergie — sie stecken bereits im Wirkungsgrad der
> Speicherrechnung. Ein Prozentsatz auf den Durchsatz würde sie doppelt zählen; das ist der
> sachliche Grund, aus dem beide Speicher nur Weg C zulassen. Zweitens ist ihr Hilfsbedarf
> (Klimatisierung, Batteriemanagement, Standby) überwiegend **zeit**abhängig und nicht
> durchsatzabhängig — ein Jahresbetrag bildet ihn richtiger ab als jeder Prozentsatz.

### 4.6 Anlagenscharfe Aufschlüsselung (BW7)

Für jede Anlage entsteht eine Zeile mit Menge, Preis und Betrag — **als Aufteilung der vorhandenen
Summen, nicht als zweite Rechnung**:

| Anlage | Energie | Zuordnungsschlüssel |
|---|---|---|
| BHKW-Modul | Brennstoffkosten, CO₂ | `Verbrauch` × Preis des `CarrierId` |
| Heizkessel-Modul | Brennstoffkosten, CO₂ | ebenso |
| Wärmepumpe (+ Heizstab) | Strombezugskosten | `Stromverbrauch` + `Heizstab` × Bezugspreis |
| Stromspeicher | Verluste als Strombezug | aus der Speicherrechnung |
| Solarthermie, Puffer | — | keine bezugsgebundenen Kosten |

**Prüfregel:** `Σ Anlagenanteile = Projektwert` auf den Cent. Weicht es ab, erscheint eine
Restzeile „nicht zugeordnet" — nach demselben Grundsatz wie die Restzeile der Preiszerlegung.

Investitions- und Betriebskosten werden über die **vorhandene** Komponentenzuordnung
(`Tab_ProjektWerte.KomponentenID`) gruppiert; sie ist heute nur beschreibend und wird damit erstmals
ausgewertet. Eine anlagenscharfe Kapitalwert- oder Gestehungskostenrechnung entsteht dadurch
**nicht** — sie wäre eine andere Aufgabe (Variantenvergleich) und ist ausdrücklich nicht Teil dieses
Konzepts.

---

## 5 Datenmodell

Migrationsschritte fortlaufend **ab 59** (58 = E6-Quellensaat, siehe Wurzel-`CLAUDE.md`). Jeder
Schritt idempotent, kein DDL-DEFAULT auf Fachwerten, Vorbelegung per DML — und jede Vorbelegung ist
der Wert, der **nichts auslöst**.

### 5.1 Preisbestandteile für Brennstoffe (Schritt M-1)

`energy_project_settings`, analog dem vorhandenen Strom-Aufschlagsblock:

| Spalte | Typ | Inhalt | Vorbelegung |
|---|---|---|---|
| `Anteil_Energiesteuer` | DOUBLE | ct/kWh | NULL |
| `Anteil_Energiesteuer_Aktiv` | YESNO | | False |
| `Anteil_CO2` | DOUBLE | ct/kWh (BEHG) | NULL |
| `Anteil_CO2_Aktiv` | YESNO | | False |
| `Anteil_Netzentgelt` · `_Aktiv` | DOUBLE / YESNO | ct/kWh | NULL / False |
| `Anteil_Vertrieb` · `_Aktiv` | DOUBLE / YESNO | ct/kWh | NULL / False |
| `Anteil_Modus` | TEXT(20) | `AUFGESCHLUESSELT` / `GESAMTWERT` | `GESAMTWERT` |

> **Persistenzwert-Fußnote (B2, 30.08.2026):** Gespeichert wird `Gesamtwert` /
> `Aufgeschluesselt` — die Werte der bestehenden Konstanten
> `DbWerte.SP_AUFSCHLAG_MODUS_*` (Regel „keine neuen Persistenzliterale"). Die
> Großschreibung in dieser Tabelle ist Dokumentationsschreibweise.

> **Falle aus E5, die hier greift:** Access legt `YESNO` durchgängig mit `False` an, und der Leseweg
> des Strom-Aufschlagsblocks behandelt `NULL` als „nicht gepflegt" und setzt dann den **vollen
> Vorschlagssatz** — bei Projekt 1030 gemessen 11,746 ct/kWh trotz fünf abgeschalteter Flags. Der
> neue Block darf diese Regel **nicht** übernehmen: `NULL` heißt hier „kein Anteil", und der
> Vorschlagssatz kommt nur auf ausdrückliche Übernahme in das Feld.

### 5.2 Steuerwahl und Hilfsstrom je Anlage (Schritt M-2)

`Tab_Energieanlagen` — Muster E6 (NULL bzw. 0 = Projektwert):

| Spalte | Typ | Inhalt |
|---|---|---|
| `Energiesteuer_Wahl` | TEXT(20) | `KEINE` / `PARAGRAF_53` / `PARAGRAF_53A` / `PARAGRAF_54`; NULL = Projektwert |
| `Aufteilung_Methode` | TEXT(24) | NULL = Projektwert |
| `Hilfsenergie_Anteil` | DOUBLE | % des Energieeinsatzes der Komponente (4.5); 0 = keine Hilfsenergie |

Die Spalte gilt für **jede** Komponente mit Hilfsenergie, nicht nur für BHKW — sie sitzt deshalb an
`Tab_Energieanlagen` und nicht an `Tab_BHKW`. Vorschlagswerte kommen aus dem Gerätekatalog, sofern
gepflegt, **als Vorschlag im Dialog, nicht als stiller Rückfall im Rechenweg**.

`Tab_ErgebnisBHKWModul` und `Tab_ErgebnisHeizkesselModul` bekommen `Hilfsenergie` DOUBLE [MWh/a],
damit die Größe wie jede andere Ergebnisgröße persistiert, im Bericht erscheint und der
Stromsteuerrechnung ohne Zweitrechnung zur Verfügung steht.

### 5.3 Projektangaben (Schritt M-3)

`Tab_ProjektWirtschaftlichkeit`:

| Spalte | Typ | Inhalt | Vorbelegung |
|---|---|---|---|
| `Stromst_Befreiung_Modus` | TEXT(12) | `ERLOES` / `AUSWEIS` (BW3) | `AUSWEIS` (BF1 entschieden 30.08.2026; bestandsneutral, da kein Bestandslauf die Reihe bucht — B1) |
| `Kohaerenz_Pruefung` | YESNO | Warnzeilen nach 4.1 erzeugen | True (nur Ausweis, keine Rechenwirkung) |

**Doppelte Schema-Wahrheit beachten:** `WirtschaftlichkeitCtrl.StelleTabellenSicher()` legt die
Spalten dieser Tabelle zusätzlich selbst an (`:290-295`). Neue Spalten gehören wie bei E4/E5/E6 an
**beide** Stellen.

### 5.4 Was **nicht** angelegt wird

Keine neue Ergebnistabelle für die Herleitungstafel. Menge, Satz und Einheit je Zeile werden bei
jedem Lauf gebildet und über den mit E7 geschaffenen **Rückgabekanal** transportiert — dasselbe
Verfahren wie bei `KwkgModulNachweis` und `KostenPositionNachweis`, die bewusst nicht persistiert
werden (`WirtschaftlichkeitDaten.cs:690-706`).

---

## 6 Dialoge

### 6.1 `Form_BhkwWirtschaftlichkeit` — neuer Dialog (BW9)

**Andockung, zwei Wege auf dasselbe Formular** (Muster PV/F7):
- Knopf **„BHKW…"** in der Fußleiste von `UcWirtschaftlichkeit`, neben „Strombezug…", „BHKW-Tarif…"
  und „Photovoltaik…" (`UcWirtschaftlichkeit.cs:140-207`), sichtbar nur bei `_erzeuger.Bhkw`
- Reiter **„Ertrag/Bonus"** des Komponenten-Kostendialogs (`ucErtragBonus`, KD5) — dort heute reine
  Anzeige; der Reiter öffnet künftig diesen Dialog, wie er es für PV bereits tut

**Hausregeln:** Designer-basiert (FK1/Ä6, Muster `Form_PhotovoltaikVerguetung`), Kopfband `#0F1F3D`
mit weißem 12-pt-Titel, `MyResource` de+en mit Präfix `BHW_*`, Drei-Schichten-Regel,
`InfoKnopf.Anbringen`, `Program.ZahlParsen/ZahlPruefen/ZahlFaerben`, `FensterEinpassung`,
Höhendeckelung + AutoScroll, `SpeichernLeiste` statt schließender Speicherung.

**Gruppen:**

| # | Gruppe | Inhalt |
|---|---|---|
| 1 | **Anlagen** | Tabelle der BHKW-Module: Bezeichner · P_el · Brennstoff · Stichtag · Inbetriebnahme · Anlagenart. Zeile aufklappbar → die Felder des heutigen `Form_KwkgModule`, ergänzt um Steuerwahl und Hilfsstrom (BW5/BW6). Warnzeilen: P_el > 500 kW (Ausschreibung), > 2 MW (Stromsteuerbefreiung entfällt), Heizöl ab IBN 2025 |
| 2 | **KWK-Zuschlag** | Tatbestand § 6 Abs. 3 · Anlagenart · Kostenanteil · Pauschalmodus § 9 · Kontingent- und Deckel-Override. Darunter **Herleitungslabel je Anlage**: „Einspeisung 5,5667 ct/kWh — 50 kW × 8,00 + 50 kW × 6,00 + 150 kW × 5,00 + 50 kW × 4,40" (Bestand `KwkgSatzRechner.Vorschlag`, heute nur im Modulformular) |
| 3 | **Energiesteuer** | Wahl je Anlage mit Rückfall auf den Projektwert · Aufteilungsmethode · Jahresnutzungsgrad. Herleitungslabel: „§ 53a Abs. 5 · Erdgas 4,42 €/MWh · 2.480 MWh (Ho, Faktor 1,1048) = 10.962 €/a". **Kohärenzzeile** nach 4.1 in `Firebrick`, wenn der Preis die Steuer nicht ausweist |
| 4 | **Stromsteuer** | Räumlicher Zusammenhang · Hocheffizienz · Unternehmensart (BW4, führendes Feld) · Modus der § 9-Befreiung (BW3). Herleitungslabel je Vorschrift, Kohärenzzeile wie oben; Sprungknopf in den Energieträgerdialog (Strom) |
| 5 | **Hilfsstrom** | Anteil und Deckung je Modul, resultierende Nettostromerzeugung, Hinweis auf die dadurch gesperrte Betriebskostenposition |
| 6 | **Vorschau** | Live-Block aus **dem einen** Rechenweg (Muster `Form_PhotovoltaikVerguetung.lblVorschau`): Zuschlag p. a. · Energiesteuer p. a. · Stromsteuer p. a. · Einspeiseerlös p. a. · vermiedene Kosten p. a. — keine Zweitrechnung |

**Was `Form_WirtschaftlichkeitParameter` verlässt:** die Gruppen „BHKW — KWKG 2025" (9 Felder +
Button) und „BHKW — Energie- und Stromsteuer" (6 Felder). Dort bleibt eine einzeilige Statuszeile mit
Sprungknopf. Der Dialog schrumpft damit von sechs auf vier Gruppen und ist wieder handhabbar.

### 6.2 `ucBrennstoffBestandteile` — Preiszerlegung für Brennstoffe (BW1)

Neues Steuerelement in `ucFuelSettings`, eingeblendet bei `pricing_model` GAS/FUEL — **exakte
Nachbildung** von `ucStromAufschlaege` (Modusumschalter, Komponentenzeilen mit Aktiv-Schalter und
Einheit, Live-Summenzeile, rot markierter Rest, Override-Modus mit lesbaren Komponenten):

- Energiesteuer [ct/kWh] mit **Schnellwahl aus dem Katalog** — „Regelsatz § 2" / „nach § 53a Abs. 5" /
  „nach § 54", jeweils mit dem Jahressatz beschriftet und über die Einheitenkette (€/MWh, €/1.000 l,
  €/1.000 kg) in ct/kWh umgerechnet
- CO₂-Anteil [ct/kWh] mit Schnellwahl aus dem CO₂-Preispfad × Emissionsfaktor des Trägers
  — **heizwertbezogen** (B2, 30.08.2026): Der Arbeitspreis des Dialogs ist `Preis ÷ Hi`,
  die Zerlegung muss dieselbe Basis tragen, sonst wäre die Restzeile schief. Für Erdgas
  sind das 1,31 ct/kWh bei 65 €/t; die 1,18 im Beispiel der Herleitungstafel (6.3)
  waren brennwertbezogen
- Netz-/Messentgelt, Vertrieb — frei
- Restzeile und Effektivpreiszeile wie beim Strom

Die Schnellwahlknöpfe lesen den **Katalog**, nicht Konstanten — damit ist die von A7 benannte
doppelte Wahrheit für die neue Seite von vornherein vermieden; die Strom-Seite zieht in Etappe B4
nach (BW4).

### 6.3 Die Herleitungstafel (BW8, B7)

**Neue Seite „Herleitung"** in der senkrechten Navigation von `UcBerichteKosten` (heute Übersicht ·
Kosten · Wirtschaftlichkeit · Bericht) — kein eigener Dialog, weil die Tafel zum Ergebnis gehört und
nicht zur Eingabe.

Aufbau, nach dem Vorbild `Dial_ErloesErg` der Altanwendung, aber in EPOS-Plan-Muster:

```
▸ Energiekosten                                    Menge        Satz         Betrag
    BHKW 1 · Erdgas E                          2.480 MWh   7,20 ct/kWh    178.560 €
      davon Energiesteuer (§ 2: 5,50 €/MWh)                 0,61 ct/kWh     15.128 €
      davon CO₂ (BEHG, 65 €/t)                              1,18 ct/kWh     29.264 €
    Spitzenkessel 1 · Erdgas E                    620 MWh   7,20 ct/kWh     44.640 €
    Wärmepumpe (Strom)                            180 MWh  24,60 ct/kWh     44.280 €
      davon Stromsteuer (20,50 €/MWh)                       2,05 ct/kWh      3.690 €
    nicht zugeordnet                                    —            —           0 €
▸ Erlöse und Gutschriften
    KWK-Zuschlag, eingespeist                     820 MWh  5,5667 ct/kWh    45.647 €
      Herleitung: 50 kW × 8,00 + 50 kW × 6,00 + 150 kW × 5,00 + 50 kW × 4,40
    Energiesteuer § 53a Abs. 5                  2.480 MWh   4,42 €/MWh      10.962 €
    Stromsteuer § 9b (nach Sockel 250 €/a)        180 MWh  20,00 €/MWh       3.350 €
    Stromsteuer § 9 Abs. 1 Nr. 3       [Ausweis]  420 MWh  20,50 €/MWh       8.610 €
▸ Vermiedene Stromkosten (Ausweis)
    Arbeit / Leistung / Summe / spezifisch
▸ Prüfhinweise
    ⚠ Die Gutschrift § 53a setzt voraus, dass der Gaspreis die Energiesteuer enthält.
      Im Preis ist sie nicht ausgewiesen (Energieträgerdialog → Erdgas E).
```

Zeilen mit dem Vermerk `[Ausweis]` gehen **nicht** in den Kapitalwert — das ist die sichtbare Form
der Entscheidung BW3 und der E5-Regel für die vermiedenen Kosten. Dieselbe Definition speist Word
und Excel; die Mehrjahrestabelle (`WirtschaftlichkeitZeilen.Mehrjahresbild`) bleibt daneben
bestehen und bekommt die neuen Spalten.

### 6.4 Lokalisierung

Der Ordner `Views\Wirtschaftlichkeit` ist **nicht lokalisiert** — dort liegt eine einzige `.resx`
mit vier Standardeinträgen, die Texte stehen als deutsche Literale in `TexteSetzen()`. Das ist der
offene Punkt 11 des Umsetzungsstands (63 Anzeigetexte in drei Dialogen).

**Neue Texte gehen ausnahmslos über `MyResource` (de + en), Präfix `BHW_*`.** Die Altlast wird in
Etappe B6 zusammen mit den 63 Bestandstexten erledigt — nicht vorher, weil sie sonst zweimal
angefasst wird. Für optionale Schlüssel gilt das vorhandene Rückfallmuster `T(schluessel, rueckfall)`
(`UcWirtschaftlichkeit.cs:641-647`).

---

## 7 Abgrenzung zu den anderen Anlagen

Der Auftrag verlangt ausdrücklich, dass Wärmepumpe und andere Anlagen **separat betrachtet** werden.
Dieses Konzept leistet das auf drei Ebenen — und benennt, was es bewusst **nicht** leistet.

| Ebene | Was geschieht |
|---|---|
| **Mengen** | bereits getrennt (Brennstoff je BHKW-/Kesselmodul mit Träger, Strom je WP-Modul) — wird ausgewertet statt nur persistiert (BW7) |
| **Energiekosten und CO₂** | je Anlage **aufgeschlüsselt**, Summe zeilengleich dem Projektwert; Restzeile bei Abweichung |
| **Steuern** | § 53/§ 53a nur für stromerzeugende Anlagen; § 54 auch für Kessel; § 9b auf den gesamten Netzbezug einschließlich Wärmepumpe und Speicher — das ist keine BHKW-Größe und war schon mit E5 richtig vom BHKW gelöst |
| **Investition und Betriebskosten** | nach Komponente gruppiert dargestellt (vorhandene `KomponentenID`) |
| **Nicht geleistet** | keine anlagenscharfe Kapitalwert- oder Gestehungskostenrechnung, keine Aufteilung der Wärmegestehung auf Erzeuger, keine eigene Amortisation je Anlage. Der Kapitalwert bleibt eine Projektgröße gegen die Unterlassensalternative — das ist die Methode nach DIN EN 17463 und wird nicht angetastet |

Der Grund für die letzte Zeile: Eine anlagenscharfe Amortisation setzt eine Aufteilung der
**gemeinsam** genutzten Infrastruktur (Wärmezentrale, bauliche Anlagen, Pufferspeicher, Netz) voraus,
die es fachlich nicht eindeutig gibt. Wer BHKW und Wärmepumpe wirtschaftlich vergleichen will, rechnet
sie als **Varianten** — dafür gibt es den Variantenvergleich, und er ist der richtige Ort.

---

## 8 Etappen

| # | Inhalt | Ergebniswirkung | Abnahme |
|---|---|---|---|
| **B1** | **Zahlenprobe zuerst.** Befund B2 (Doppelzählung § 9 Nr. 3) an einem Projekt mit Stundenreihen rechnen; zugleich die seit E8 offene Zahlenprobe gegen die Altanwendung (Bedarf 100 MWh, Restbezug 62, Einspeisung 34, Eigenverbrauch 38) nachholen | keine (Messung) | Handrechnung gegen Referenzprojekt; jede Abweichung gegen die 17 Altbefunde bewertet |
| **B2** | M-1 Preisbestandteile Brennstoff + `ucBrennstoffBestandteile` + Kohärenzprüfung als **reine Warnzeile** — **umgesetzt 30.08.2026** (Schema-Schritt 60, `B2_Preisbestandteile_Protokoll.md`) | **keine** — Vorbelegung `GESAMTWERT`, alle Flags aus | Referenzläufe byte-gleich gegen B6; Warnzeilen plausibel |
| **B3** | M-2 Steuerwahl und Hilfsstrom je Anlage; § 54 auf Kesselbrennstoff; Eigenstrom-Tatbestand je Anlage | **ja, gewollt** bei gepflegten Angaben; ohne Pflege keine | A/B-Nachweis mit Zahlen, Handrechnung je Vorschrift, präparierte Kopie für den Kesselfall |
| **B4** | BW4: Energieintensität als eine Wahrheit; Katalog löst die Konstanten in `StromAufschlagModel` ab (Befund A7) | keine (wertgleich) | Gleichstand Katalog ↔ Aufschlagsblock gemessen |
| **B5** | `Form_BhkwWirtschaftlichkeit`; Auszug der beiden Gruppen aus `Form_WirtschaftlichkeitParameter`; Andockung an `UcWirtschaftlichkeit` und `ucErtragBonus` | keine (Eingabe) | Roundtrip aller Felder; Layout-Sweep; Sichtprüfung Philipp |
| **B6** | Herleitungstafel (Reiter, Word, Excel aus einer Definition); M-3; Umschaltung BW3 nach dem Ergebnis von B1; Lokalisierung der Wirtschaftlichkeitsdialoge (offener Punkt 11) | je nach B1-Ergebnis; sonst Ausgabe | Reiter ↔ Word ↔ Excel zeichengleich gemessen; `Σ Anlagenanteile = Projektwert` auf den Cent |
| **B7** | Referenzprojekt für die dauerhafte Regression (Vorschlag „1031" aus dem E8-Protokoll, Abschnitt 7) und **Tests** für `SteuerGutschriftRechner`, `KwkgSatzRechner`, `StromTarifRechner` — der seit E8 offene Befund A1 | keine | Testprojekt grün; Regressionslauf über die Wirtschaftlichkeit existiert erstmals |

Jede Etappe: eigener Commit-Block mit Protokoll-`.md` im Hausmuster, Build über das MSBuild von
Visual Studio (x64), Referenzlaufvergleich, `<<<<<<<`-Sweep nach Parallelsitzungen, kein Push.
**B2 und B4 sind zwingend ergebnisneutral**; die erste gewollte Ergebnisänderung kommt mit B3.

> **Reihenfolge-Begründung:** B1 steht vorn, weil die Entscheidung BW3 an seinem Ergebnis hängt und
> weil die Altanwendungsprobe seit der Abnahme E8 als „gewichtigster offener Punkt der Ausbaustufe"
> geführt wird. Ein Konzept, das darüber hinweggeht, baut auf einer ungeprüften Kette weiter.

---

## 9 Entscheidungsfragen

| Nr. | Frage | Empfehlung |
|---|---|---|
| ~~**BF1**~~ | ~~§ 9 Abs. 1 Nr. 3 als Ausweis statt Erlös (BW3)~~ | **entschieden 30.08.2026: Vorgabe „Ausweis"** — B1 hat die Doppelzählung bestätigt (1.510,84 €/a auf beiden Pfaden identisch) und gemessen, dass **kein** Bestandslauf die Reihe bucht: die Vorgabe AUSWEIS ist bestandsneutral. Umschalter je Projekt (`Stromst_Befreiung_Modus`) bleibt für Preise ohne Steueranteil; Umsetzung in B6/M-3 |
| **BF2** | Kohärenzprüfung: nur warnen, oder die Gutschrift bei fehlendem Steueranteil im Preis auf 0 setzen? | **nur warnen** — eine stille Rechenänderung an einer Steuergröße ist schlimmer als eine sichtbare Lücke |
| **BF3** | Aufschläge (Strom) künftig **Vorgabe EIN**? Gemessene Wirkung: +32 bis 34 % Energiekosten, −30 bis 33 % Kapitalwert. Seit E5 offen | **ja, für neue Projekte** — Bestandsprojekte behalten ihren Schalterstand. Ohne das bleibt der Strompreis unvollständig und jede Stromsteuerentlastung unbelegt |
| **BF4** | Hilfsenergie: Vorbelegung aus dem Gerätekatalog, sofern gepflegt — oder immer 0? | **immer 0**, Katalogwert nur als Vorschlagsknopf im Dialog (Ergebnisneutralität, Muster E6) |
| ~~**BF4a**~~ | ~~Bezugsbasis der Erfahrungswerte~~ | **entschieden 29.08.2026: Endenergiekosten der betrachteten Anlage** (Weg A). Alternativ als Anteil am Endenergiebedarf in kWh, Kosten daraus über den Strompreis (Weg B). Siehe 4.5 |
| ~~**BF4b**~~ | ~~Bezugsgröße beim Stromspeicher~~ | **entschieden 29.08.2026: nur absolute Größe** — ebenso Pufferspeicher; Photovoltaik nicht einschlägig, Feld bleibt als Absolutgröße. Siehe 4.5 |
| **BF5** | § 54 auf Kesselbrennstoff: auch für Kessel **ohne** BHKW im Projekt? | **ja** — § 54 hängt an keiner KWK-Anlage, genau wie § 9b (E5-Entscheidung) |
| **BF6** | Steuerwahl je Anlage: auch für § 53/§ 53a, oder nur für § 54? | **für alle** — Mehrbrennstoffprojekte sind sonst nicht abbildbar |
| **BF7** | Herleitungstafel als eigene Seite in „Berichte && Kosten" oder als Reiter in `UcWirtschaftlichkeit`? | **eigene Seite** — `UcWirtschaftlichkeit` trägt bereits ListView, Kacheln, Grid und neun Knöpfe |
| **BF8** | Anlagenscharfe **Betriebskosten** über `KomponentenID` gruppieren — oder bleibt es bei der Projektsumme? | **gruppieren, nur als Anzeige**; die Rechnung bleibt unberührt |
| **BF9** | Sollen die Preisbestandteile auch die Simulation erreichen (`StromPreisCtrl`, Speicherrechnung), oder nur die Wirtschaftlichkeit? | **zunächst nur Wirtschaftlichkeit** — die Speicherrechnung hat ihre eigene Preiskette, und ein gleichzeitiger Umbau beider wäre nicht mehr prüfbar |
| **BF10** | Reicht die Näherung „Eigen/Einspeise-Split projektweit, stromproportional auf Module verteilt" (Bestand) weiterhin, wenn der Hilfsstrom dazukommt? | **ja**, aber im Herleitungstext als Näherung benennen |

---

## 10 Offene Punkte und Risiken

1. **Die Doppelzählung B2 ist hergeleitet, nicht gemessen.** Bis zur Probe B1 ist sie ein Verdacht
   mit klarer Beweisführung — kein Befund. Sie darf keine Codeänderung auslösen, bevor sie gerechnet
   ist.
2. **§ 53 neben § 53a bleibt rechtlich ungeklärt** (Grundlagen, Abschnitt 6 Punkt 1) und vor
   produktivem Einsatz mit dem Hauptzollamt zu klären. Die Anlagenwahl aus BW5 vergrößert die
   Frage nicht, sie verschiebt sie nur auf eine feinere Ebene.
3. **Ohne Stundenreihen keine Stromsteuerbefreiung** (E4-Entscheidung, bewusst). Mit dem Hilfsstrom
   kommt eine weitere Größe hinzu, die ohne Reihen nicht sauber zuzuordnen ist — der Dialog muss das
   sagen, statt zu schätzen.
4. **`energy_carrier.density` ist im gesamten Bestand leer** — je Liter abgerechnete Träger mit
   einem Satz je 1.000 kg bleiben ohne Gutschrift. Das trifft die Preiszerlegung genauso wie die
   Entlastung; die Schnellwahl muss dann leer bleiben und den Grund nennen.
5. **Energiesteuersätze vor 2024/2026 sind nicht eingesät** (erkennbare Lücke statt geratener Wert).
   Für Projekte mit früherem Bilanzjahr liefert die Schnellwahl nichts.
6. **Zwei Migrationsmechanismen** — `SchemaMigration` und das Selbst-DDL in `WirtschaftlichkeitCtrl`
   laufen für `Tab_ProjektWirtschaftlichkeit` beide. M-3 muss an beiden Stellen stehen.
7. **Die 6.000-Vbh-Stufe § 8 Abs. 2** und der Mindestabstand zur Inbetriebnahme der Altanlage sind
   weiterhin nicht abgebildet (Datenmodell führt sie nicht) — unverändert aus W4 übernommen.
8. **Kategorie 3 „Energiekosten"** in `Tab_ProjektWerte` ist weiterhin pflegbar und wird von keiner
   Rechnung gelesen. Mit der Preiszerlegung wird der Widerspruch sichtbarer, nicht kleiner — er
   gehört entschieden (offener Punkt 2 des Umsetzungsstands).
