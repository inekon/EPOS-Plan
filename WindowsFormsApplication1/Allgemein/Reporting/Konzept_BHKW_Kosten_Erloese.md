# Konzept: BHKW-Betriebskosten und -Erlöse (Ausbaustufe W4)

**Stand: 19.08.2026 — nach der Abnahme (Etappe E8) auf den tatsächlichen Umsetzungsstand
berichtigt.** Ergänzt [`Konzept_Wirtschaftlichkeit.md`](Konzept_Wirtschaftlichkeit.md)
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

> **Berichtigung nach der Umsetzung (E5, bestätigt bei der Abnahme E8).** „HT/NT entfällt" gilt
> nur für das **neue Rollenmodell** — dort führt jede Rolle genau **einen** Durchschnitts-
> Arbeitspreis. Das **Zonenmodell der Stufe W3 bleibt vollständig erhalten und ist die
> Vorbelegung** (`Tab_ProjektTarif.Tarif_Modus = ZONEN`); dort gelten HT und NT unverändert
> weiter. Beide Modelle stehen nebeneinander, `Tarif_Modus` entscheidet — und genau daran hängt
> die Ergebnisneutralität der Etappe E5 für Bestandsprojekte. Ein Bestandsprojekt rechnet nach
> E5 also **nicht** mit einem Durchschnittspreis, sondern wie vorher. Begründung im
> [`W4_E5_Tarife_Strombezug_Protokoll.md`](W4_E5_Tarife_Strombezug_Protokoll.md), Abschnitt 2.2.

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
| ~~`Tab_BHKW`, `Tab_BHKW_STAMM`~~ | ~~`Wartungsbemessung TEXT(20)` — analog Kessel (Schritt 15)~~ — **nicht gebaut, und das ist richtig so** (festgestellt bei der Abnahme E8). Die Bemessung der Wartung sitzt seit E3 an der **Kostenposition** (`Tab_ProjektWerte.Bemessung`, Migrationsschritt 19) statt am Gerätekatalog. Das ist der bessere Ort: Die Bemessung ist eine Eigenschaft der erfassten Kostenzeile eines Projekts, nicht des Geräts — ein Gerät kann in zwei Projekten unterschiedlich abgerechnet werden. L7 („genau **eine** Angabe gilt") ist damit vollständig erfüllt, ohne dass Projekt- **und** `_STAMM`-Tabelle eine Spalte brauchen |
| `Tab_ProjektTarif` | `Leistungsmodell TEXT(20)` (`MONATLICH` / `STAFFEL` / `JAHRESHOECHSTLAST`), vier **kumulierte Obergrenzen** in kW mit Sommer- und Winterpreis, monatlicher Leistungspreis, Grundpreis, `GueltigAb` — *umgesetzt mit E5 als **36 Spalten**: je Rolle (Bezug, Reststrom) ein Arbeitspreis, ein Grundpreis, `…_Leistungsmodell TEXT(24)`, ein Monatspreis und vier Staffelstufen; dazu `Tarif_Modus TEXT(12)` (`ZONEN` / `ROLLEN`), `Tarif_GueltigAb` und für die Einspeisung Arbeits- und Grundpreis. Die Einspeiserolle führt **keine** Leistungsstaffel — Begründung im E5-Protokoll, Abschnitt 2.2* |
| `Tab_Kraftwerkspark` | `CO`, `Staub`, `GueltigAb`, `Quelle`, `ReadOnly` und vor allem **`Bezugsbasis TEXT(12)`** (`BRENNSTOFF` / `STROM`) — **mit W4 NICHT gebaut** (Abnahme E8, Befund A5). Der Definitionsbruch des Altkatalogs besteht damit fort; der Punkt ist seit E8 in der Liste offener Punkte des Umsetzungsstands geführt (Nr. 9) |
| `Tab_ProjektWirtschaftlichkeit` | Steuerparameter je Projekt: Unternehmensart, Nutzungsgrad, Hocheffizienz, räumlicher Zusammenhang, Wahl § 53 / § 53a |
| `Tab_Energieanlagen` | *E6, Schritt 22:* `KWKG_Stichtag DATETIME`, `KWKG_Inbetriebnahme DATETIME`, `KWKG_Anlagenart TEXT(24)`, `KWKG_Eigenstromfall TEXT(24)`, `KWKG_Satz_Einspeisung DOUBLE`, `KWKG_Satz_Eigen DOUBLE`, `KWKG_Vbh_Kontingent DOUBLE`, `KWKG_Vbh_Jahresdeckel DOUBLE` — **alle NULL-fähig, NULL = Projektwert**. Kein DML, kein `_STAMM`-Gegenstück (die Tabelle hat keines) |

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
> zoll.de führt 1131 und 1131_25, für § 53a das Formular 1135.
>
> **Nachtrag der Abnahme E8:** Der Satz „Abschnitt 5 dieses Konzepts nennt es noch" war beim
> Schreiben schon nicht mehr zutreffend — im gesamten Konzept kommt „1131a" **nur in dieser
> Berichtigung** vor. Die Angabe ist damit vollständig bereinigt; berichtigt ist auch das
> Grundlagendokument. Wer nach der Fundstelle suchte, suchte vergebens.

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

> **Berichtigung nach der Umsetzung (Etappe E6, 19.08.2026).** „Leistungs**klasse**" ist die
> falsche Vorstellung. § 7 Abs. 1 und 2 KWKG überschreiben ihre Wertetabelle mit
> *Leistungsanteil* und meinen **marginale Tranchen**: Eine 300-kW-Anlage bekommt 50 kW zu
> 8,00, 50 kW zu 6,00, 150 kW zu 5,00 und 50 kW zu 4,40 ct/kWh — leistungsgewichtet
> **5,5667 ct/kWh** statt der 4,40 ct/kWh einer Klassensuche, also **21 % mehr**. Zweitens ist
> der Zuschlag auf **selbst genutzten** Strom nicht das Spiegelbild der Einspeisung: Er besteht
> nach Abs. 2 nur in den drei Tatbeständen des § 6 Abs. 3, und § 7 Abs. 3a (neue Anlagen bis
> 50 kW: 16 bzw. 8 ct/kWh) geht Abs. 1 **und** 2 vor. Beides steht in
> [`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](../../../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md),
> Abschnitt 1.3, und ist mit E6 als Staffel umgesetzt (`KwkgSatzRechner`).
>
> **Und eine Präzisierung zur Wirkung:** Solange **jedes** Modul über dem Jahresdeckel liegt,
> ist die Summe der Modulreihen algebraisch die projektweite Reihe — die alte Rechnung war
> dann nicht falsch, sondern zufällig richtig. Die Wirkung entsteht erst bei ungleicher
> Deckelung, verschiedenen Inbetriebnahmejahren, verschiedenen Kontingenten oder beim Ausfall
> einer einzelnen Anlage (E6-Protokoll, Abschnitte 4.2 und 5).

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

> **Stand nach der Abnahme (E8): dieser letzte Punkt ist nur zur Hälfte erfüllt.** Erfüllt sind
> `Form_Gesetzesparameter` (48 `MyResource`-Zugriffe, 0 deutsche Literale) und
> `Form_Betriebskosten` (41 / 0). **Nicht erfüllt** sind die drei Dialoge der Etappen E4 bis E6:
> `Form_WirtschaftlichkeitParameter` (0 / 23), `Form_Tarifstruktur` (0 / 30) und
> `Form_KwkgModule` (0 / 10) greifen kein einziges Mal auf `MyResource` zu. Die
> Ressourcenschlüssel, die E6 und E7 angelegt haben, bedienen den **Bericht**, nicht die Dialoge.
> Kein Etappenprotokoll begründet die Abweichung; sie ist seit E8 als offener Punkt 11 im
> Umsetzungsstand geführt. Die Persistenz- und Steuerwertseite der Drei-Schichten-Regel ist
> dagegen eingehalten — bei der Abnahme geprüft und ohne Befund.

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

> **Stand nach der Abnahme (E8): von den drei genannten Wertegruppen ist eine offen.** Die
> KWKG-Stichtage und -Grenzen kommen seit E1/E2 aus dem Katalog (mit Code-Rückfallebene), die
> Umsatzsteuer ebenfalls. **Die Stromsteuersätze in `Model/StromAufschlagModel.cs:25-70` sind
> weiterhin `const double`** — und kein toter Bestand: Der Aufschlagsblock der E5 rechnet mit
> ihnen (`STROMSTEUER_REGELFALL = 2.050` ct/kWh als Teil der Vorschlagssumme 11,746 ct/kWh),
> während die Steuerrechnung der E4 denselben Satz aus dem Katalog liest
> (`STROMST_REGELSATZ = 20,50 €/MWh`). **Derselbe Satz an zwei Orten**, heute wertgleich, ohne
> Kopplung: Eine im Katalog gepflegte Novelle erreicht den Aufschlagsblock nicht. Seit E8 als
> doppelte Wahrheit im Umsetzungsstand geführt. Die BEHG-Pflichtigkeit steht weiterhin als
> Zahlenliterale im `KostenEmissionRechner` (aus dem E2-Nachtrag 2 bekannt).

---

## 7 Etappen

| # | Inhalt | Ergebnis |
|---|---|---|
| **E1** | `Tab_Gesetzesparameter`, Seed, Admin-Maske, Lesefassade | Parameter pflegbar, noch ohne Rechenwirkung |
| **E2** | Vbh-Korrektur (L6), Betriebsstunden je Modul persistieren | Zuschlag bei Kaskaden korrekt |
| **E3** | Kostenposition erweitern (L5), Betriebskosten-Dialog VDI 2067 | Betriebskosten vollständig erfassbar |
| **E4** | Energiesteuer- und Stromsteuergutschrift | Steuern in Kapitalwert und Bericht — **umgesetzt 19.08.2026**, ergebnisneutral für Bestandsprojekte ([`W4_E4_Steuergutschriften_Protokoll.md`](W4_E4_Steuergutschriften_Protokoll.md)) |
| **E5** | Tarife mit drei Leistungspreismodellen, vermiedener Strombezug | Erlösseite vollständig — **umgesetzt 19.08.2026**, ergebnisneutral für Bestandsprojekte ([`W4_E5_Tarife_Strombezug_Protokoll.md`](W4_E5_Tarife_Strombezug_Protokoll.md)). Die Aufschläge sind gemessen (+32 bis 34 % Energiekosten, −30 bis 33 % Kapitalwert) und hinter einen Projektschalter gelegt, Vorgabe AUS |
| **E6** | KWK-Zuschlag je Modul mit Katalogvorschlag | gesetzliche Leistungsklassen abgebildet — **umgesetzt 19.08.2026** (Migrationsschritt 22, [`W4_E6_Zuschlag_je_Modul_Protokoll.md`](W4_E6_Zuschlag_je_Modul_Protokoll.md)). Ergebnisneutral für Einmodulprojekte; bei Mehrmodulanlagen ändert sich das Ergebnis **nur**, wenn die Module den Jahresdeckel unterschiedlich treffen oder sich in Datum, Satz oder Kontingent unterscheiden |
| **E7** | Bericht (Word und Excel), Mehrjahrestabelle | Ausgabe — **umgesetzt 19.08.2026** ([`W4_E7_Bericht_Mehrjahrestabelle_Protokoll.md`](W4_E7_Bericht_Mehrjahrestabelle_Protokoll.md)); rein additiv, 864 von 864 Wirtschaftlichkeitswerten unverändert |
| **E8** | Abnahme, neue Referenzbasis, Protokoll | **abgeschlossen 19.08.2026** ([`W4_E8_Abnahme_Protokoll.md`](W4_E8_Abnahme_Protokoll.md)). Basis **`2026-08-19_B6`** eingefroren, 216/216 byte-gleich gegen B5; die vier Prüflücken gemessen; **acht Befunde A1–A8 dokumentiert**, keine Codezeile geändert |

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

  > **Stand nach der Abnahme (E8): NICHT ERFÜLLT.** Keine der sieben Etappen hat diese Probe
  > gerechnet; die Suche nach jeder der sechs Zahlen über alle sieben Etappenprotokolle liefert
  > **null Treffer**, und keine Etappe begründet den Verzicht. Damit fehlt der einzige Nachweis,
  > dass die neue Kette dieselbe Aufgabe löst wie die Anwendung, die sie ablöst — die
  > Handrechnungen der Etappen prüfen jede Formel gegen ihre eigene Herleitung, nicht gegen das
  > Vorbild. **Der Punkt bleibt nach W4 offen** (Umsetzungsstand, offener Punkt 7).
- **Referenzlauf** Flag AUS 8/8 byte-identisch je Etappe außer E2. — *Erfüllt; ab Basis B5 sind es
  **neun** Projekte und 216 Dateien. E3 bis E8 je 216/216 byte-gleich; für E2 lief der A/B-Nachweis
  mit Wirkungsbeleg wie vorgesehen.*
- **Reflection-Harness** für Dialoge und Persistenz, deutsch und englisch,
  Dialogwächter gegen unerwartete Meldungen. — *Erfüllt in E1, E3 und E8 (dort 40 Proben, 0
  Fehlschläge, 0 unerwartete Dialoge).* **Aber:** Jeder dieser Harnische war ein Wegwerfwerkzeug.
  **Dauerhafte Tests für die neuen Rechenklassen gibt es nicht**, obwohl L9 sie verlangt — siehe
  Umsetzungsstand, offener Punkt 10.
- Build 0 Fehler, exakt 6 Bestandswarnungen. — *Erfüllt in jeder Etappe und bei der Abnahme.*

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
   offen** (Etappe E6 oder später). — **Stand nach E8: weder E6 noch eine andere Etappe hat ihn
   angefasst.** `Bezugsbasis` ist im gesamten Code unbekannt (0 Treffer), und der Punkt war bis zur
   Abnahme in keiner Offene-Punkte-Liste geführt. Seit E8 im Umsetzungsstand als offener Punkt 9.
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

   > **Bilanz nach der Abnahme (E8):** Aufgelöst ist **eine** — die Kennzahlenliste (es waren
   > zuletzt 22 Zeilen in **drei** Kopien, seit E7 führt `WirtschaftlichkeitZeilen.cs` sie
   > einmal). Die Einspeisevergütung steht jetzt an **vier** Orten statt drei (E5 hat einen
   > hinzugefügt, um einen Bestandsmangel zu beheben), die Vorrangregel unverändert an drei.
   > **Neu entstanden sind zwei:** die zwei Lesewege auf die Kostenposition (E3) und der
   > Stromsteuersatz an zwei Orten (Katalog gegen `StromAufschlagModel`, benannt mit E8). Die
   > Migrationsdoppelung ist von drei auf vier Tabellen gewachsen. Vollständige Liste im
   > [`W4_Umsetzungsstand.md`](W4_Umsetzungsstand.md), Abschnitt 6.

7. **Nach der Abnahme E8 zusätzlich offen** (Einzelheiten im
   [`W4_E8_Abnahme_Protokoll.md`](W4_E8_Abnahme_Protokoll.md), Abschnitt 5.2): die **Zahlenprobe
   gegen die Altanwendung** (A8, siehe Abschnitt 8); **L12** — der Methodenwechsel zum 01.01.2027
   liegt nur als Katalogdatenseite vor, **keine Codezeile liest die 2027er-Schlüssel** (A3);
   **L13** — die Bilanzierungskonvention für Biomasse ist nicht umgesetzt (A4); **keine Tests**
   für die neuen Rechenklassen, obwohl L9 sie verlangt (A1); die **Lokalisierung** der drei neuen
   Dialoge (A6) samt der doppelt beschrifteten Zeile „Hinweis" im Ergebnisreiter (B1).
