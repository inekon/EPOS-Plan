# Konzept: BHKW-Betriebskosten und -Erlöse (Ausbaustufe W4)

**Stand: 18.08.2026.** Ergänzt [`Konzept_Wirtschaftlichkeit.md`](Konzept_Wirtschaftlichkeit.md)
um die vollständige BHKW-Kosten- und Erlösrechnung. Faktenbasis:
[`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](../../../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md)
(Rechtsstand mit Quellen) und
[`Analyse_Altanwendung_BHKW-Plan.md`](Analyse_Altanwendung_BHKW-Plan.md)
(Rechenwege und Fehler der abzulösenden Excel-Anwendung).

---

## 1 Anlass und Abgrenzung

Abgelöst wird die Excel-Anwendung `BHKW-WP-PLAN.XLSM`: Betriebskosten nach
VDI 2067, Energie- und Stromsteuererstattung, Strombezug, Einspeisung und
Reststrom mit vermiedenen Kosten, KWK-Zuschlag für neue, modernisierte und
nachgerüstete Anlagen — auf aktuellem Rechtsstand und mit pflegbaren Parametern.

**Das ist eine Ausbaustufe, kein Neubau.** Ein erheblicher Teil ist bereits
produktiv (Phasen W1–W3, 9 und 11, siehe [`UMSETZUNGSSTAND.md`](UMSETZUNGSSTAND.md)):

- KWK-Zuschlag mit jahresscharfer Reihe und Deckelung über das
  Vollbenutzungsstunden-Kontingent — `WirtschaftlichkeitCtrl.BaueKwkgReihe:839-940`
- degressive Jahresstaffel als pflegbare Tabelle `Tab_KWKG_Staffel` mit
  Code-Rückfallebene — `:153-186`, Lookup `:966-973`
- Förderfähigkeitsprüfung nach § 6 (Stichtag 31.12.2026, vierjährige
  Realisierungsfrist, 500-kW-Ausschreibungslücke, Heizöl-Ausschluss) — `:851-903`
- Negativpreis-Abschlag nach § 7 Abs. 5
- Kapitalwertrechnung nach DIN EN 17463 mit Barwerten, Preissteigerungen,
  Ersatzbeschaffung, Restwert, internem Zinsfuß und dynamischer Amortisation —
  `KapitalwertRechner.cs:75-209`

**Was fehlt:**

| Lücke | Belegt |
|---|---|
| Energiesteuer- und Stromsteuererstattung | „Energiesteuer" kommt im gesamten Code nur in Konzeptdateien vor; „Stromsteuer" nur als Bezugspreis-Aufschlag |
| ~~Vermiedener Strombezug als Erlöszeile~~ — **behoben mit E5** | steckt nur implizit im kleineren Restbezug; die Bezugsgröße „Bedarf ohne Anlage" wird nirgends geführt |
| ~~Strompreis für eingespeisten BHKW-Strom ohne PV~~ — **behoben mit E5** | Feld „Einspeisevergütung PV" ist ohne PV-Gruppe unsichtbar (`Form_WirtschaftlichkeitParameter.cs:62-66`) — der Strom bekommt dann nur den Zuschlag |
| VDI-2067-Bemessungsarten (% Investition, €/h, €/kWh) | `Tab_ProjektWerte` kennt nur einen Eurobetrag; `Einheit` ist Freitext ohne Rechenwirkung |
| Negative Beträge für Erlöse | `ucKostenItem.cs:104-109` klemmt, `TechnikPlanwertCtrl.Basis:361` verwirft ≤ 0 |
| Aufschläge in der Jahreskostenrechnung — **mit E5 möglich gemacht, Vorgabe AUS** | Netzentgelt, Umlagen, Stromsteuer, Konzession, Vertrieb sind gepflegt, wirken aber nur in der Speichersimulation. Gemessene Wirkung: **+32 bis 34 % Energiekosten, −30 bis 33 % Kapitalwert** — deshalb ein ausdrücklicher Projektschalter statt stiller Übernahme (E5-Protokoll, Abschnitt 4) |

**Ein Rechenfehler im Bestand** ist zu korrigieren: `WirtschaftlichkeitCtrl.cs:848`
setzt die erreichten Vollbenutzungsstunden auf `Betriebsstunden_Gesamt`. Das ist
die **Summe thermischer** Vollbenutzungsstunden über alle Module
(`SimulationBHKW.cs:294-304`, `:353-358`) und kann 8.760 h überschreiten.

> **Richtungskorrektur nach der Messung (Etappe E2, 18.08.2026).** Dieser Absatz
> nahm an, der Zuschlag falle „bei mehreren BHKW systematisch zu hoch" aus. Das
> Gegenteil ist der Fall: `BaueKwkgReihe` normiert mit
> `bonusVoll × verguetet / vbh` — eine zu große Vbh-Zahl senkt diesen Bruch und
> verbraucht zugleich das 30.000-h-Kontingent zu schnell. Gemessen an einer
> präparierten Zweimodul-Kaskade halbierte der Altstand den Zuschlag exakt
> (242,90 statt 485,81 €/a im Jahr 1). Belege in
> [`W4_E2_Vollbenutzungsstunden_Protokoll.md`](W4_E2_Vollbenutzungsstunden_Protokoll.md),
> Abschnitt 4.

---

## 2 Leitentscheidungen

**L1 — Ausbaustufe.** Die bestehende KWKG-Reihe wird erweitert, nicht ersetzt.
Neue jahresscharfe Gutschriftreihen entstehen nach demselben Muster und werden
über `zusatzErloesJeJahr` in `KapitalwertRechner.Rechne:75-143` eingespeist.
Sobald mehr als eine solche Reihe existiert, wird der Parameter auf eine Liste
benannter Reihen umgestellt — eine Signaturänderung mit überschaubarem
Aufruferkreis.

**L2 — Ein Katalog für gesetzliche Parameter.** Neue Tabelle
`Tab_Gesetzesparameter` nach der Bauform von `Tab_KWKG_Staffel` und mit der
Stichtagsregel aus `StromPreisCtrl.cs:329-381` („jüngste Zeile mit
`JahrVon ≤ Jahr`"). Rund 60 Parameter statt je Sachverhalt einer eigenen Tabelle.
`Tab_KWKG_Staffel` wird per Migration überführt und danach nicht mehr gelesen;
die Tabelle bleibt als Altbestand stehen.

*Begründung:* Die Alternative — je Sachverhalt eine Tabelle — führt bei sechs
Themenfeldern (KWKG-Sätze, Vbh-Kontingente, Stromsteuer, Energiesteuer, BEHG,
Umsatzsteuer) zu sechs Pflegemasken mit derselben Stichtagslogik. Ein Katalog
braucht eine.

**L3 — Einheitendisziplin.** Jeder Satz wird in **seiner gesetzlichen Einheit**
gespeichert (€/MWh, €/1.000 l, €/1.000 kg) mit eigenem Einheitenfeld; umgerechnet
wird ausschließlich über die gepflegten Heizwerte. Die Vermischung dieser
Einheiten ist die Ursache des Öl-Fehlers der Altanwendung (Befund 1 und 2 der
Analyse).

**L4 — Steuersatz und Entlastungssatz getrennt.** Nie eine Differenz raten. Der
Dialogwert „−0,5 €/MWh" (Restbelastung) und der Tabellenwert „−20,5 €/MWh"
(Regelsatz) sind verschiedene Größen; künftig stehen Regelsatz (20,50),
Entlastungssatz (20,00) und Sockelbetrag (250 €/a) einzeln im Katalog.

**L5 — Kostenposition erweitern statt eigener Erlöstabelle.**
`Tab_ProjektWerte` bekommt additiv `Kostenart`, `Bemessung`, `IstErloes` — das
Zielbild des Altkonzepts (`Konzept_Wirtschaftlichkeit.md:452-454`) — dazu `Menge`
und `Einheitpreis`, damit die Herleitung („0,041 €/kWh × 72.000 kWh") persistent
ist statt nur Anzeigetext. Die Nichtnegativitäts-Klemme wird für Erlöspositionen
aufgehoben.

**L6 — Vollbenutzungsstunden elektrisch und je Modul.** Für den Zuschlag gilt
`Stromproduktion × 1000 / Pel` je Modul. Zusätzlich werden die vorhandenen
`SimulationBHKW.Laufzeiten[]` persistiert (Muster Wärmepumpe: `ErgebnisModel.cs:92`,
`ErgebnisCtrl.cs:222/235`, `SimulationRunner.cs:362`) — sie sind die
Bemessungsgrundlage für Wartung je Betriebsstunde.

> **Zwei Präzisierungen aus der Umsetzung (E2, 18.08.2026).** (1) Die Spalte für
> `Laufzeiten[]` heißt `VbhThermisch`, **nicht** `Betriebsstunden`: Der Wert ist
> `Wärme / P_therm`, also eine Vollbenutzungsstundenzahl; Taktung bildet der
> Rechenkern nicht ab. Wer eine Wartung „je Betriebsstunde" darauf bemisst (L7,
> Etappe E3), rechnet mit einer Näherung und muss das wissen. (2) **Je Modul sind
> thermische und elektrische Vbh im heutigen Modell identisch** — der Motor
> erzeugt Wärme und Strom stets im festen Verhältnis `P_el / P_therm`. Die
> Korrektur wirkt deshalb ausschließlich über die **Aggregation** (Summe →
> leistungsgewichtet) und über die **Bezugsmenge von Σ P_el**, nicht über die
> Energieart je Modul.

**L7 — Nutzerentscheidungen vom 18.08.2026:**

- **Wartung BHKW:** genau **eine** Angabe gilt — je kWh elektrisch, je
  Betriebsstunde oder Prozent der Investition. Die Auswahl ist sichtbar, die
  übrigen Felder werden gesperrt. Das stille Überschreiben der Altanwendung
  (Befund 6) wird nicht übernommen.
- **KWK-Zuschlagssatz:** wird aus Inbetriebnahmedatum, elektrischer Leistung und
  Einspeisung/Eigennutzung **vorgeschlagen** und bleibt überschreibbar; die
  Herleitung wird angezeigt. Damit behebt sich Befund 14 (Datum ohne Wirkung).
- **Granularität:** Zuschlag, Vollbenutzungsstunden und Kontingent **je
  BHKW-Modul** — erst damit sind die gesetzlichen Leistungsklassen abbildbar.
- **Leistungspreis:** **alle drei** Modelle — monatlicher Leistungspreis,
  vierstufige kW-Staffel (Sommer und Winter getrennt) und Jahreshöchstlast; je
  Tarif wählbar.

**L8 — Netto ist verbindlich**, der Umsatzsteuersatz wird Katalogparameter statt
40-fach hart codierter 1,19. Der KWK-Zuschlag wird **nicht** mit Umsatzsteuer
multipliziert (Befund 4).

**L9 — Rechenlogik ohne Datenbankzugriff.** Neue Rechenklassen als reine
Funktionen über DTOs, Vorbild `SpeicherEngine/Aufschlagsmodell.cs` (headless,
unit-getestet, sprachneutrale Schlüssel, unveränderliche Objekte). Kein neues
Projekt, sondern `Allgemein/Wirtschaftlichkeit/` ohne DB-Bezug; Tests im
vorhandenen Testprojekt.

**L11 — Zwei Faktorensätze, strikt getrennt.** Nachweisfaktoren (GEG bis
31.12.2026, GModG ab 01.01.2027) und reale Bilanzfaktoren (UBA-Strommix) werden
getrennt geführt und dürfen **nie dieselbe Variable belegen**. Grund: Der
GModG-Nachweiswert für Netzstrom beträgt ab 2027 100 g CO₂-Äq/kWh, der reale
Strommix lag 2025 bei 406 g CO₂-Äq/kWh mit Vorkette — Faktor 4. Der Nachweiswert
gehört in den Energieausweis, der reale in Wirtschaftlichkeit und Klimabilanz.
Einzelheiten in [`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](../../../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md),
Abschnitt 7.

**L12 — Methodenwechsel 2027 abbilden, nicht nur Zahlen tauschen.** Zum
01.01.2027 entfällt der Verdrängungsstrommix (2,8 beziehungsweise
860 g CO₂-Äq/kWh) **ersatzlos**; die Stromgutschriftmethode für eingespeisten
KWK-Strom wird abgeschafft und durch eine Bewertung nach DIN EN 15316-4-5
ersetzt. Beide Rechenwege müssen parallel vorliegen und über dasselbe
Gültig-ab-Datum umgeschaltet werden. Wer nach 2027 dennoch eine Gutschrift
rechnen will, trifft eine methodische Wahl — sie wird zum Auswahlparameter und
im Bericht ausgewiesen. Für BHKW-Projekte ist das die folgenreichste Änderung
des gesamten Vorhabens.

**L13 — Bilanzierungskonvention für Biomasse ausweisen.** Ob biogenes
Verbrennungs-CO₂ mit null angesetzt wird, hängt vom Regelwerk ab und
widerspricht sich zwischen BEHG, GModG, UBA-Emissionsbilanz und UBA-CO₂-Rechner
(dort 365 g/kWh). Die gewählte Konvention wird Einstellung mit Ausweis im
Bericht, keine stille Annahme im Code. Beim BEHG kommt hinzu, dass der Nullansatz
**einen Nachhaltigkeitsnachweis voraussetzt** — ohne ihn gilt der volle fossile
Standardwert.

**L10 — HT/NT entfällt** (Nutzervorgabe). Die Vier-Preis-Struktur bleibt intern
erhalten, wird aber mit demselben Durchschnittspreis belegt — genau das tut die
Altanwendung in `Durchschitt_eintragen` bereits. Die Leistungspreise bleiben
vollständig erhalten, denn der Leistungsanteil der vermiedenen Kosten ist
regelmäßig **negativ** und damit ergebnisrelevant.

---

## 3 Datenmodell

Additiv, Migration ab Schemastand 18. Muster für neue Tabellen:
`SchemaMigration.SQL_CREATE_SPVARIANTE:1177-1196` mit `SpVarianteTabelle:1321-1334`
(Tabelle hart, Index und Fremdschlüssel weich).

| Tabelle | Änderung |
|---|---|
| `Tab_Gesetzesparameter` (neu) | `ID LONG PK, Schluessel TEXT(60), Klasse TEXT(40), JahrVon LONG, Wert DOUBLE, Einheit TEXT(20), Quelle TEXT(120)` |
| `Tab_ProjektWerte` | `Kostenart TEXT(20)`, `Bemessung TEXT(20)`, `IstErloes YESNO`, `Menge DOUBLE`, `Einheitpreis DOUBLE` |
| `Tab_ErgebnisBHKW` | `VbhElektrisch DOUBLE` — leistungsgewichtet über alle Module (E2, Schritt 18) |
| `Tab_ErgebnisBHKWModul` | `VbhThermisch DOUBLE`, `VbhElektrisch DOUBLE` (E2, Schritt 18) |
| `Tab_ErgebnisWirtschaftlichkeit` | `KWKGVbhElektrisch DOUBLE` — Bemessungsgrundlage der Deckelung, über `SpalteSicher` |
| `Tab_BHKW`, `Tab_BHKW_STAMM` | `Wartungsbemessung TEXT(20)` — analog Kessel (Schritt 15) |
| `Tab_ProjektTarif` | `Leistungsmodell TEXT(20)` (`MONATLICH` / `STAFFEL` / `JAHRESHOECHSTLAST`), vier **kumulierte Obergrenzen** in kW mit Sommer- und Winterpreis, monatlicher Leistungspreis, Grundpreis, `GueltigAb` — *umgesetzt mit E5 als **36 Spalten**: je Rolle (Bezug, Reststrom) ein Arbeitspreis, ein Grundpreis, `…_Leistungsmodell TEXT(24)`, ein Monatspreis und vier Staffelstufen; dazu `Tarif_Modus TEXT(12)` (`ZONEN` / `ROLLEN`), `Tarif_GueltigAb` und für die Einspeisung Arbeits- und Grundpreis. Die Einspeiserolle führt **keine** Leistungsstaffel — Begründung im E5-Protokoll, Abschnitt 2.2* |
| `Tab_Kraftwerkspark` | `CO`, `Staub`, `GueltigAb`, `Quelle`, `ReadOnly` und vor allem **`Bezugsbasis TEXT(12)`** (`BRENNSTOFF` / `STROM`) |
| `Tab_ProjektWirtschaftlichkeit` | Steuerparameter je Projekt: Unternehmensart, Nutzungsgrad, Hocheffizienz, räumlicher Zusammenhang, Wahl § 53 / § 53a |

**ACE-Regeln, die im Bestand teuer gelernt wurden:** `YESNO` belegt
Bestandszeilen mit `False`, `DOUBLE` bleibt NULL — Vorbelegung immer als eigener
DML-Schritt. Kein DDL-`DEFAULT` auf Fachwerten. Neue Spalten **immer in Projekt-
und `_STAMM`-Tabelle**, sonst Datenverlust beim Übernehmen aus dem Katalog. Kein
AutoWert, sondern `MAX(ID)+1` — deshalb `ON DELETE CASCADE` an Anhangstabellen.

---

## 4 Rechenkette

### 4.1 Betriebskosten (VDI 2067)

Bezugsgröße einheitlich **netto**; die Altanwendung mischt netto und brutto
(Befund 5) und wird darin nicht übernommen.

| Position | Bemessung | Bezug |
|---|---|---|
| Vollwartung BHKW | €/kWh el **oder** €/h **oder** % Investition (genau eine) | Stromproduktion bzw. Betriebsstunden je Modul |
| Instandhaltung BHKW / Kessel / Wärmezentrale / bauliche Anlagen / Stromeinspeisung | % der Investition | jeweilige Investitionsgruppe |
| Personal · Steuern, Versicherung, Verwaltung | % der Investition | Investitionssumme |
| **Hilfsenergie** | **% der Brennstoffkosten** | Summe Brennstoffkosten |
| Reserveleistung · Sonstiges | €/a absolut | — |

Empfehlungsbereiche der VDI 2067 werden als Hinweis angezeigt (Werte in der
Analyse, Abschnitt 2.6).

Vorrang: Prozentangabe schlägt Absolutangabe — aber die Absolutfelder werden
**nicht mehr still geleert**, sondern gesperrt und sichtbar als „durch
Prozentangabe ersetzt" gekennzeichnet.

### 4.2 Steuern als Gutschrift

```
Energiesteuer = Satz(Träger, Jahr) × Menge in gesetzlicher Einheit
                § 53  → voller Satz auf den GESAMTEN BHKW-Brennstoff (siehe Korrektur)
                § 53a → Teilsatz (Erdgas 4,42 €/MWh) auf den Gesamteinsatz
Stromsteuer   = Befreiung § 9 Abs. 1 Nr. 3 auf den Eigenverbrauch
                (≤ 2 MW JE ANLAGE, hocheffizient, < 270 g CO₂/kWh Energieertrag,
                 4,5 km Umkreis)
              + Entlastung § 9b auf den Netzbezug (20,00 €/MWh, abzüglich 250 €/a)
```

> **Korrektur nach der Recherche (Etappe E4, 19.08.2026).** Dieser Abschnitt lautete
> „§ 53 → voller Satz, **nur auf den Stromanteil** des Brennstoffs". Das ist falsch:
> § 53 Abs. 2 Satz 1 EnergieStG stellt darauf ab, ob das Energieerzeugnis „unmittelbar am
> Energieumwandlungsprozess" teilnimmt — beim Motor-BHKW also der **gesamte** Brennstoff;
> die Dienstvorschrift Energieerzeugung sagt zum Schaubild des § 53 Abs. 1 ausdrücklich
> „Wärme – genutzt oder ungenutzt – wird nicht betrachtet". Der „Anteil" des Abs. 1
> Satz 2 betrifft die **mechanische** Energie an der Welle (Generator neben Verdichter).
> Abzugrenzen ist **BHKW gegen Kessel**, nicht Strom gegen Wärme. Belege in
> [`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](../../../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md),
> Abschnitt 3.5, und in
> [`W4_E4_Steuergutschriften_Protokoll.md`](W4_E4_Steuergutschriften_Protokoll.md),
> Abschnitt 2. Die Aufteilungsmethode bleibt eine Projektangabe, ihr Vorgabewert ist das
> belegte Verfahren.
>
> **Zwei weitere Präzisierungen aus derselben Etappe.** (1) Der CO₂-Grenzwert bezieht sich
> auf den **Energieertrag** (Strom + Wärme), nicht auf den Brennstoff — die reinen
> Brennstofffaktoren liegen bei Erdgas **und** Heizöl EL unter 270 g/kWh; erst der
> Kehrwert des Nutzungsgrades trennt sie. (2) Ein Formular **„1131a" existiert nicht**;
> zoll.de führt 1131 und 1131_25, für § 53a das Formular 1135. Abschnitt 5 dieses
> Konzepts nennt es noch.

Die Bedingungen werden geprüft und begründet ausgewiesen, nicht stillschweigend
angenommen — insbesondere der CO₂-Grenzwert, an dem Heizöl-BHKW in der Regel scheitern.

Die Wahl zwischen § 53 und § 53a ist rechtlich ungeklärt (Grundlagen, Abschnitt 6) und
wird als **einstellbare Option** modelliert.

### 4.3 Strom und Erlöse

```
Bezugskosten ohne BHKW = Arbeit(Bedarf, Bezugstarif)      + Leistung(Bedarf, Modell)
Reststromkosten        = Arbeit(Restbezug, Reststromtarif)+ Leistung(Restbezug, Modell)
Vermiedene Kosten      = Bezugskosten ohne BHKW − Reststromkosten
Einspeiseerlös         = Einspeisemenge × Einspeisepreis
KWK-Zuschlag je Modul  = min(Δ Vbh, Jahresdeckel, Restkontingent) × Pel × Satz
```

Der Leistungsanteil der vermiedenen Kosten ist regelmäßig negativ, weil der
Reststrom-Leistungspreis über dem Bezugs-Leistungspreis liegt. Das ist kein
Fehler, sondern der Kern der Aussage — es wird als eigene Zeile ausgewiesen.

Der Zuschlagssatz wird je Modul aus dem Katalog vorgeschlagen (Leistungsklasse,
Inbetriebnahmejahr, eingespeist oder eigengenutzt) und bleibt überschreibbar.
Jahresdeckel (2026: 3.300 h) und Kontingent (30.000 h) gelten je Modul.

Die drei Begrenzungen der Altanwendung (Befund 15) werden auf **eine** reduziert:
Kontingent und Jahresdeckel in der jahresscharfen Reihe. Die Einzeljahresanzeige
zeigt denselben Wert wie das erste Jahr dieser Reihe.

---

## 5 Eingabe

- **Betriebskosten-Dialog BHKW** nach Vorbild `Dial_BetriebKost`: drei Spalten
  (Prozent, absolut netto, brutto abgeleitet), Auswahl der Wartungsbemessung,
  Empfehlungsbereiche als Hinweis.
- **Steuer- und KWKG-Block** im Wirtschaftlichkeitsdialog: Sätze aus dem Katalog
  vorbelegt mit Herkunftsanzeige („KWKG § 7 Abs. 3a, gültig ab 2020"),
  Unternehmensart, Nutzungsgrad, Hocheffizienzkriterium; die Anlagenart als
  Auswahl (neu, modernisiert, nachgerüstet mit Kostenschwelle) statt freier
  Vbh-Eingabe.
- **Tarife**: Bezug, Einspeisung und Reststrom je als Durchschnittspreis plus
  Leistungspreismodell.
- Anzeigetexte ausschließlich über `MyResource` (beide `.resx` plus Designer,
  Nachtrag im Lokalisierungskatalog); DB-Werte deutsch und eingefroren in
  `DbWerte.cs`; Steuerwerte sprachneutral.

## 6 Administration

Neue Maske „Gesetzliche Parameter" nach dem Muster von `Form_KostenAdmin`: Liste
je Klasse (KWKG, Stromsteuer, Energiesteuer, BEHG, Umsatzsteuer) mit Schlüssel,
Gültig-ab-Jahr, Wert, Einheit und Quelle. **Eine Novelle ist eine neue
Jahreszeile, kein Ändern der alten** — so bleibt jede Altrechnung
nachvollziehbar. Der Seed läuft über das `StelleKatalogSicher`-Muster
(`WirtschaftlichkeitCtrl.cs:151-186`), damit Bestandsinstallationen die Werte
ohne Migration erhalten.

Damit werden folgende heute hart codierten Werte pflegbar: Stromsteuersätze in
`StromAufschlagModel.cs:25-70`, KWKG-Stichtage und -Grenzen in
`WirtschaftlichkeitCtrl.cs:45-47`, BEHG-Pflichtigkeit in
`KostenEmissionRechner.cs:210-238`.

---

## 7 Etappen

| # | Inhalt | Ergebnis |
|---|---|---|
| **E1** | `Tab_Gesetzesparameter`, Seed, Admin-Maske, Lesefassade | Parameter pflegbar, noch ohne Rechenwirkung |
| **E2** | Vbh-Korrektur (L6), Betriebsstunden je Modul persistieren | Zuschlag bei Kaskaden korrekt |
| **E3** | Kostenposition erweitern (L5), Betriebskosten-Dialog VDI 2067 | Betriebskosten vollständig erfassbar |
| **E4** | Energiesteuer- und Stromsteuergutschrift | Steuern in Kapitalwert und Bericht — **umgesetzt 19.08.2026**, ergebnisneutral für Bestandsprojekte ([`W4_E4_Steuergutschriften_Protokoll.md`](W4_E4_Steuergutschriften_Protokoll.md)) |
| **E5** | Tarife mit drei Leistungspreismodellen, vermiedener Strombezug | Erlösseite vollständig — **umgesetzt 19.08.2026**, ergebnisneutral für Bestandsprojekte ([`W4_E5_Tarife_Strombezug_Protokoll.md`](W4_E5_Tarife_Strombezug_Protokoll.md)). Die Aufschläge sind gemessen (+32 bis 34 % Energiekosten, −30 bis 33 % Kapitalwert) und hinter einen Projektschalter gelegt, Vorgabe AUS |
| **E6** | KWK-Zuschlag je Modul mit Katalogvorschlag | gesetzliche Leistungsklassen abgebildet |
| **E7** | Bericht (Word und Excel), Mehrjahrestabelle | Ausgabe |
| **E8** | Abnahme, neue Referenzbasis, Protokoll | eingefroren |

**E2 ändert Ergebnisse bewusst.** Wie bei K-3 gilt: A/B-Nachweis gegen HEAD,
Wirkungsbeleg, neuer Basis-Freeze. Alle übrigen Etappen sind ergebnisneutral für
Bestandsprojekte ohne die neuen Angaben.

## 8 Verifikation

- **Zahlenprobe gegen die Altanwendung.** Das Beispiel aus dem Erlös-Screenshot
  (Bedarf 100 MWh, Restbezug 62, Einspeisung 34, Eigenverbrauch 38; vermiedene
  Kosten 3.657 / −341 / 3.316 €; Einspeiseerlös 1.028 €; Zuschlag 5.488 und
  3.059 €) muss die neue Kette reproduzieren. Abweichungen ausschließlich dort,
  wo ein Befund aus Abschnitt 5 der Analyse bewusst nicht übernommen wird — jede
  einzeln begründet.
- **Referenzlauf** Flag AUS 8/8 byte-identisch je Etappe außer E2.
- **Reflection-Harness** für Dialoge und Persistenz, deutsch und englisch,
  Dialogwächter gegen unerwartete Meldungen.
- Build 0 Fehler, exakt 6 Bestandswarnungen.

## 9 Offene Punkte

1. ~~`DB-TARIF.XLS` und `bhkwplan.py` fehlen~~ — **am 18.08.2026 nachgereicht und
   ausgewertet** (Analyse, Abschnitte 7 und 8). Ergebnis: Die Werte beider
   Kataloge sind veraltet und werden **nicht übernommen**; ausgewertet wurde die
   Struktur. Die Mengenlogik ist bestätigt, und die Methodenfrage ist
   entschieden — den Ergebnisdialog füllt die **Differenzmethode**, Abschnitt 4.3
   dieses Konzepts ist damit belegt.
2. **Zwei Strukturkorrekturen** gegenüber dem Altkatalog sind in Abschnitt 3
   eingearbeitet und beim Nachbau zu beachten: Leistungsstaffelgrenzen werden als
   **kumulierte Obergrenze** geführt (der Altkatalog speichert Stufen*breiten*),
   und das Leistungsmodell wird eine **sichtbare Auswahl** statt der versteckten
   Schalterlogik „Sommerpreis = 0". *Beide sind mit E5 umgesetzt, zusammen mit der
   dritten und vierten Falle (geführte vierte Stufe, Feld `Tarif_GueltigAb`) —
   E5-Protokoll, Abschnitt 2.3.* Beim Kraftwerkspark verhindert das neue Feld
   `Bezugsbasis`, dass Faktoren je kWh Brennstoff und je kWh Strom in derselben
   Spalte landen — genau dieser Definitionsbruch steckt im Altkatalog; **er ist noch
   offen** (Etappe E6 oder später).
3. **§ 53 neben § 53a** rechtlich ungeklärt — als Option modelliert.
4. **Kategorie 3 „Energiekosten"** in `Tab_ProjektWerte` ist pflegbar, wird aber
   von keiner Rechnung gelesen; dort erfasste Beträge fallen still aus jeder
   Auswertung. Entfernen oder als Override mit sichtbarem Vorrang definieren.
5. **Ausschluss fossiler flüssiger Brennstoffe** aus der KWKG-Förderung nur aus
   Sekundärquelle belegt.
6. **Doppelte Wahrheiten im Bestand**, die mit dieser Ausbaustufe aufzulösen sind:
   BHKW-Einspeisevergütung an drei Orten (`energy_project_settings.Verguetung_BHKW`,
   `Tab_ProjektTarif.Einsp_*`, `WirtschaftlichkeitParameter.Einspeiseverguetung`);
   Vorrangregel Projekt vor Katalog in drei Implementierungen; 14 Kennzahlzeilen
   doppelt in Word- und Excel-Generator.
