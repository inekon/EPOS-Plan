# KONTEXT: Kosten, Energieträger und Wirtschaftlichkeit — konsolidierter Stand

**Stand: 29.08.2026.** Dieses Dokument führt zusammen, was bisher auf vier Dokumente verteilt war,
und ergänzt die Festlegungen vom 29.08.2026. Es ist der **Einstiegspunkt**: Wer wissen will, wie
Kosten, Energieträger, Steuern und Wirtschaftlichkeit in EPOS-Plan zusammenhängen, liest zuerst
hier und geht erst dann in die Fachdokumente.

## Verhältnis zu den Quelldokumenten

| Dokument | Rolle danach | Was davon überholt ist |
|---|---|---|
| [`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](Grundlagen_KWKG_Energiesteuer_Stromsteuer.md) | **bleibt Faktenbasis** — Rechtsstand mit Quellen, Zahlenwerte, Recherchelücken. Nicht zusammengefasst, nicht ersetzt | nichts; § 3.5 (Brennstoffaufteilung) hat die frühere Annahme bereits berichtigt |
| [`Bestandsaufnahme_Kosten-Energie-Dialogstruktur.md`](Bestandsaufnahme_Kosten-Energie-Dialogstruktur.md) | **Archiv** — Ist-Analyse vom 19.08.2026, historisch wertvoll | Technik-Steckbrief (§ 1), Rechenkern, Fehlanzeigen (§ 6) und tote Enden (§ 7) sind überwiegend erledigt — Einzelheiten in § 9 unten |
| [`Konzept_Kosten_Energietraeger_EPOS-Plan.md`](Konzept_Kosten_Energietraeger_EPOS-Plan.md) | **erledigt** — Etappen K1–K6 umgesetzt, Entscheidungen E1–E8 getroffen | die Etappentabelle (§ 10) und die offenen Punkte; die Leitentscheidungen L1–L9 gelten weiter |
| [`Konzept_Kostendialoge_EPOS-Plan.md`](Konzept_Kostendialoge_EPOS-Plan.md) | **überwiegend erledigt** — KD1–KD6 umgesetzt | FK-Fragen sind entschieden; § 6.3 zur Hilfsenergie ist durch die Festlegung vom 29.08.2026 überholt |
| [`Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md`](Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md) | **offen** — Delta-Konzept, Etappen B1–B7 nicht begonnen | — |
| [`Konzept_Photovoltaik_Wirtschaftlichkeit_EPOS-Plan.md`](Konzept_Photovoltaik_Wirtschaftlichkeit_EPOS-Plan.md) | **erledigt** — P1–P6 umgesetzt | offen nur die Sichtprüfung |
| `Allgemein/Reporting/W4_*`, `K*_*`, `KD*_*`, `PV_*` | **Protokolle** — Beweisführung je Etappe, nie zusammenfassen | — |

**Pfadbasis:** `WindowsFormsApplication1\`, sofern nicht anders angegeben.

---

## 1 Steckbrief

| Aspekt | Stand 29.08.2026 |
|---|---|
| Rahmen | `net8.0-windows`, C#, WinForms + WPF, Build **x64** (seit 22.08.2026; davor x86) |
| Datenhaltung | eine Access-Datei `Kenndaten.accdb` — Kataloge **und** Projektdaten, keine Projektdatei |
| Datenzugriff | `DataRepository` (OLE DB, `?`-Parameter) — Standard; `RecordSet` ist Altbestand. **ODBC vollständig entfernt** |
| Rechenkern | vollständig verwaltet in `Allgemein/BhkwPlan.cs`. **Keine native DLL, kein COM-Server** |
| Schemastand | `SchemaMigration.ZIEL_VERSION` = 58; neue Schritte ab **59** |
| Rechenverfahren | **Kapitalwertmethode nach DIN EN 17463 / ValERI**; VDI 2067 liefert ausschließlich Kostengliederung und Betriebskostenkatalog, **nicht** die Annuitätenmethode |
| Bezugsbasis | durchgehend **netto**. Umsatzsteuer nur als Anzeige, Satz aus `Tab_Gesetzesparameter` |

Die Bestandsaufnahme vom 19.08.2026 nennt hier noch x86, ODBC-DSN „TEST" und den COM-Rechenkern —
alle drei sind überholt.

---

## 2 Die drei Kostenwelten

Kosten liegen in **drei** getrennten Datenwelten. Das ist gewachsen, aber inzwischen bewusst so
geordnet:

```
① Positionswelt (deutsch)          Tab_ProjektWerte  ← Tab_Kostenfaktor (Positionslexikon)
   Investition und Betrieb            KategorieID 1 = Investition, 2 = Betrieb
   je Projekt, je Komponente,         Kostenart · Bemessung · Satz · Menge · Betrag
   seit Schritt 45/46 je ANLAGE       IstErloes · Nutzungsdauer · VorlageID · StartJahr
                                      ID_Anlage · ID_AnlageGeraet

② Vorlagenwelt (Stammdaten)        Tab_KostenVorlage / Tab_KostenVorlagePosition
   Auslieferung = Name „Standard“,     Struktur und Bemessung je Komponente,
   IstStandard + ReadOnly              Sätze bewusst LEER
   Benutzervarianten frei

③ Energieträgerwelt (englisch)     energy_carrier · energy_price · energy_project_settings
   Preise, Heizwerte, Emissionen       pricing_model · energy_conversion
   stichtagsversioniert                Abfrage_Energietraeger_Effektiv (eff_hi/eff_hs)
```

**Drei Regeln halten das zusammen:**

- **Energiekosten leben ausschließlich in ③** (Entscheidung FK3). Die Kategorie „Energie" in ①
  ist stillgelegt; „Brennstoffkosten" und „Stromkosten" sind keine Betriebskostenpositionen.
- **② liefert Struktur, nicht Preise.** Die Vorlage sät Bezeichnung, Kostenart, Bemessung und
  Empfehlungsbereich — nie einen Satz. Erfundene Preise würden sonst unbemerkt mitrechnen.
- **Die Übernahme ② → ① materialisiert und koppelt nicht.** Nach der Übernahme sind Projektwerte
  eigenständig; `VorlageID` ist Herkunftsvermerk für die Anzeige, spätere Vorlagenänderungen wirken
  nie automatisch ins Projekt.

**Gesetzeswerte** liegen in einer vierten, eigenen Welt: `Tab_Gesetzesparameter`
(`Schluessel · Klasse · JahrVon · Wert · Einheit · Status · Quelle`) mit Pflegemaske
`Form_Gesetzesparameter`. **Eine Novelle ist eine neue Jahreszeile, kein Überschreiben** — nur so
bleiben Altrechnungen reproduzierbar. Sätze stehen **in der Einheit des Gesetzes** (€/MWh,
€/1.000 l, €/1.000 kg, €/GJ); die Umrechnung in €/kWh macht die Rechenkette über gepflegte
Heizwerte. Genau daran ist die Altanwendung mit dem Faktor 10 beim Heizöl gescheitert.

---

## 3 Umsetzungsstand

### 3.1 Vier abgeschlossene Stränge

| Strang | Inhalt | Stand |
|---|---|---|
| **K1–K6** (Kosten/Energieträger) | Alttabellen entfernt, kWh-Konsistenzprüfer, Einheiten-Seeds, Reiter Kostenprofil, Komponenten und Zuschuss, KWKG/Steuern einheitenrichtig | **abgeschlossen** 20.08.2026 |
| **W4 E1–E8 + L12/L13** (BHKW-Kosten und -Erlöse) | Gesetzeskatalog, Vbh elektrisch, VDI-Bemessungsarten, Steuergutschriften, Tarif-Rollenmodell, KWKG je Modul, Bericht, Abnahme; Methodenwechsel 2027 und Biomassekonvention | **abgenommen** 19.08.2026, unter zwei Vorbehalten (§ 8) |
| **KD1–KD6** (Kostendialoge) | Vorlagentabellen und Seeds, Komponenten-Kostendialog, Übernahme-Mechanik, Energieträgerverwaltung, Reiter Ertrag/Bonus | **abgeschlossen** 26.08.2026 |
| **P1–P6** (PV-Wirtschaftlichkeit) | EEG-Satzrechner, Monatsmarktwerte, Erlösbildung, § 51/§ 51a, Dialog, Kennzahlen | **abgeschlossen** 26.08.2026, offen nur die Sichtprüfung |

### 3.2 Was die Wirtschaftlichkeit heute rechnet

| Größe | Verfahren | Klasse |
|---|---|---|
| Kapitalwert, Annuität, IRR, dynamische Amortisation, Restwert | DIN EN 17463, benannte Erlösreihen | `KapitalwertRechner` |
| KWK-Zuschlag | **je Modul**, marginale Tranchenstaffel § 7, Eigenstrom-Tatbestände § 6 Abs. 3, Vbh-Kontingent § 8, Jahresdeckel, Förderfähigkeit, Negativpreisabschlag | `KwkgSatzRechner`, `KwkgKontingentRechner` |
| Energiesteuer | § 53 · § 53a Abs. 5 · § 54, einheitenrichtig, Brennwertumrechnung, Nutzungsgradprüfung | `SteuerGutschriftRechner` |
| Stromsteuer | Befreiung § 9 Abs. 1 Nr. 3 (vier Bedingungen je Anlage, CO₂-Grenzwert auf den **Energieertrag**), Entlastung § 9b mit Sockel 250 €/a | `SteuerGutschriftRechner` |
| Strom | Zonenmodell (HT/NT) **oder** Rollenmodell (Bezug / Reststrom / Einspeisung) mit Differenzmethode | `StromTarifRechner`, `StromMatrix` |
| PV-Vergütung | anzulegender Wert, Marktprämie, § 51/§ 51a, Kappung | `PvErloesRechner`, `EegSatzRechner` |
| Emissionen | drei Rechenwege, Methodenwechsel 2027, Biomassekonvention | `EmissionsBilanzRechner`, `BilanzKonvention` |
| Ausweis | Reiter, Word und Excel aus **einer** Zeilendefinition | `WirtschaftlichkeitZeilen` |

---

## 4 Festlegungen — konsolidiert

Die tragenden Entscheidungen aller Stränge an einer Stelle. Sie gelten weiter, auch wo das
Ursprungsdokument als erledigt gilt.

### 4.1 Datenhaltung und Struktur

| Nr. | Festlegung |
|---|---|
| **L1** | `energy_*` ist die führende Preis- und Trägerwelt; Kategorie 3 „Energie" in `Tab_ProjektWerte` ist stillgelegt |
| **L2** | Gesetzliche Parameter leben im Katalog, nicht im Code — mit Gültig-ab-Jahr; eine Novelle ist eine neue Zeile |
| **L3** | Einheitendisziplin: Sätze in der gesetzlichen Einheit, Umrechnung nur über gepflegte Heizwerte |
| **L8** | Dieselbe Regel für alle Gesetzeswerte — Steuersätze, Sockelbeträge, KWKG-Staffeln, CO₂-Preispfad |
| **KL2** | Vorlagen sind Stammdaten mit Struktur; `Tab_Kostenfaktor` bleibt flaches Positionslexikon |
| **KL3** | Projektwelt bleibt `Tab_ProjektWerte` — die Übernahme materialisiert, es entsteht keine dritte Kostenwahrheit |
| **L11** | **Zwei Faktorensätze, strikt getrennt:** Nachweiswerte (GEG/GModG Anlage 9) und reale Bilanzwerte (UBA-Strommix) dürfen nie dieselbe Variable belegen |

### 4.2 Rechnen und Bewerten

| Nr. | Festlegung |
|---|---|
| **KL4** | **Satz hat Vorrang, Betrag ist Ableitung.** Das unterlegene Feld wird gesperrt, **nie geleert** |
| **KL5** | Netto ist Rechenwahrheit; Brutto ist Anzeige |
| **KL9** | Keine neue Szenariomechanik — Worst/Best über die drei Szenariospalten |
| **L7** | Zuschuss ist eine eigene Positionsart: mindert I₀ einmalig, ohne Ersatzbeschaffung, ohne Restwert |
| **E5-1** | **Vermiedene Kosten sind Ausweis, kein Zahlungsstrom** — die Einsparung steckt in der kleineren Bezugsmenge. In den Kapitalwert geht der Reststrombetrag |
| **E5-2** | Der negative Leistungsanteil der vermiedenen Kosten ist die Kernaussage, kein Sonderfall — Sichtbarkeitsbedingungen prüfen auf „ungleich 0" |
| **E4-1** | § 53 EnergieStG erfasst beim Motor-BHKW den **gesamten** Brennstoff. Abzugrenzen ist **BHKW gegen Kessel**, nicht Strom gegen Wärme |
| **E4-2** | Der CO₂-Grenzwert des § 2 StromStG bezieht sich auf den **Energieertrag** (Strom + Wärme), nicht auf den Brennstoff |
| **E6-1** | „Leistungsanteil" in § 7 KWKG heißt **marginale Tranche**, nicht Klasse — eine Klassensuche liefert 21 % zu wenig |
| **E6-2** | Einspeisung und Eigennutzung sind nicht symmetrisch: Eigenstromzuschlag nur bei einem Tatbestand des § 6 Abs. 3 |
| **L12** | Methodenwechsel 01.01.2027: Der Verdrängungsstrommix entfällt ersatzlos. Umgeschaltet über die Projektangabe `Bilanz_Jahr`, Rückfall 2026 — **nie über die Systemuhr** |
| **L13** | Bilanzierungskonvention Biomasse und Nachhaltigkeitsnachweis sind zwei getrennte, ausgewiesene Einstellungen |

### 4.3 Oberfläche

| Nr. | Festlegung |
|---|---|
| **KL1** | Ein Formular, drei Kontexte — Admin-Stamm, Projekt, Aufruf aus dem Anlagendialog |
| **FK1 / Ä6** | **Neue Dialoge sind Designer-basiert.** Die programmatische Schule (`Form_WirtschaftlichkeitParameter`, `Form_Tarifstruktur`) wird nicht fortgeführt |
| **FK5** | Der Reiter „Ertrag/Bonus" wird bei Komponenten ohne laufende Erträge **entfernt**, nicht geleert |
| **FK7** | Der Strompreis-Teil der Einspeisung bleibt in der Tarifstruktur; der Reiter verlinkt dorthin — eine Wahrheit je Größe |
| **KL6** | Ertrag/Bonus zeigt vorhandene Wahrheiten, schafft keine neuen |
| **L9 / BW10** | **Inhalte aus BHKW-Plan, Darstellung nach EPOS-Plan.** Keine flachen Listen, keine „oder"-Doppelfelder, keine Brutto-Netto-Mischung |
| — | **Drei-Schichten-Regel:** Persistenzwerte deutsch und eingefroren in `DbWerte.cs` · Schlüssel sprachneutral und ASCII · Anzeige nur über `MyResource` |

### 4.4 Vorgehen

| Nr. | Festlegung |
|---|---|
| **L5** | Initialbefüllung ist **ergebnisneutral**: Seeds per DML-Migrationsschritt, kein DDL-Vorgabewert auf Fachwerten, `user_edited` wird nie überschrieben |
| — | Jede Vorbelegung ist der Wert, der **nichts auslöst** (`KEINE`, `KEIN_PROD_GEWERBE`, Anteil 0, Schalter aus) |
| — | Jeder Migrationsschritt ist idempotent; Zweitlauf = 0 Änderungen |
| — | Ergebniswirksame Etappen brauchen einen **A/B-Nachweis mit Zahlen** gegen den Vorgängerstand |
| — | Katalog-Nachsaat läuft **generationsweise** über die Markerzeile `KATALOG_GENERATION` — eine bewusst gelöschte Zeile kommt nicht zurück |

---

## 5 Betriebskosten nach VDI 2067

### 5.1 Positionen und Empfehlungsbereiche

Übernommen aus den Dialogen der Altanwendung (`Dial_BetriebKost` für das BHKW,
„Eingabe Betriebskosten pro Jahr für die getrennte Erzeugung" für den Referenzfall Heizkessel).
Die Bereiche standen dort in den Beschriftungen und sind die einzige Quelle dafür — ein
Richtwertkatalog existiert in der Altanwendung nicht.

| Position | Bemessung | Empfehlung | BHKW | Kessel |
|---|---|---|---|---|
| Vollwartung / Wartung BHKW | je kWh elektrisch **oder** je Betriebsstunde | — | ✓ | — |
| Instandhaltung BHKW | % der Investition | **3,0 – 9,0 %** | ✓ | — |
| Instandhaltung Heizkessel | % der Investition | **1,5 – 2,5 %** | ✓ | ✓ |
| Instandhaltung Wärmezentrale | % der Investition | **1,8 – 2,2 %** | ✓ | ✓ |
| Instandhaltung bauliche Anlagen | % der Investition | **1,0 – 1,5 %** | ✓ | ✓ |
| Instandhaltung Stromeinspeisung | % der Investition | **1,8 – 2,2 %** | ✓ | — |
| Personalkosten | % der Investition | **1,0 – 4,0 %** | ✓ | ✓ |
| Steuern, Versicherungs- und Verwaltungskosten | % der Investition | **0,8 – 2,0 %** | ✓ | ✓ |
| Hilfsenergiekosten | % des Energieeinsatzes (§ 6) | *kein Bereich im Dialog* | ✓ | ✓ |
| Reserveleistungskosten | fester Jahresbetrag | — | ✓ | — |
| frei benennbare Position | fester Jahresbetrag | — | ✓ | ✓ |

### 5.2 Drei Abweichungen der heutigen Seeds von dieser Quelle

Der Abgleich der Auslieferungsvorlagen (`SchemaKatalog.cs:2153-2177`) gegen die Dialoge ergibt:

| Befund | Heute | Soll nach Quelle |
|---|---|---|
| **Instandhaltung Heizkessel** in **beiden** Vorlagen (BHKW und Heizkessel) | `fester Jahresbetrag`, ohne Empfehlungsbereich | `% der Investition`, **1,5 – 2,5 %** |
| **Instandhaltung Wärmezentrale** fehlt in der **Heizkessel**-Vorlage | nicht vorhanden | `% der Investition`, **1,8 – 2,2 %** — der Dialog der getrennten Erzeugung führt sie |
| **Wartung Kessel** ist in der Heizkessel-Vorlage vorhanden | `je kWh thermisch` | im Altdialog nicht vorhanden — **Ergänzung von EPOS-Plan**, fachlich richtig, hier nur zur Klarstellung vermerkt |

Die übrigen sieben Bereiche stimmen bereits überein.

### 5.3 Die Vorrangregel

Prozentangabe schlägt Absolutangabe. Der Unterschied zur Altanwendung ist wesentlich: Dort wurden
die Absolutfelder beim Speichern **geleert** (stiller Datenverlust, Altbefund 6). In EPOS-Plan
wird das unterlegene Feld **gesperrt und nicht geleert** (KL4) — der Bannertext des Dialogs sagt
das ausdrücklich.

Ebenso nicht übernommen: die „oder"-Doppelfelder der Wartung. Dort standen €/kWh<sub>el</sub> und
€/h nebeneinander und wurden tatsächlich **addiert** (Altbefund 7). In EPOS-Plan ist es **eine**
Position mit sichtbarer Bemessungswahl.

---

## 6 Hilfsenergie — Definition vom 29.08.2026

**Hilfsenergie ist immer Strom für den Betrieb der Komponente.** Bemessen wird sie an der
**Endenergie der betrachteten Anlage** — Brennstoff bei BHKW und Heizkessel, Strom bei der
Wärmepumpe. Drei gleichwertige Angabewege:

```
A  % der Endenergiekosten     Betrag [€/a] = Endenergiekosten(a) × Satz / 100
B  % des Endenergiebedarfs    Menge [kWh]  = Endenergiebedarf(a) × Satz / 100
                              Betrag [€/a] = Menge × Strombezugspreis
C  fester Jahresbetrag        Betrag [€/a] = Eingabe
```

| Komponente | Endenergie = | zulässig | Erfahrungswert (Weg A) |
|---|---|---|---|
| BHKW | Brennstoff | A · B · C | **2 – 4 %** |
| Heizkessel | Brennstoff | A · B · C | **4 – 8 %** |
| Wärmepumpe | Strom | A · B · C | deutlich geringer als BHKW |
| Solarthermie | keine — die Sonne kostet nichts | **nur C** | — |
| Pufferspeicher | keine | **nur C** | — |
| Stromspeicher | keine | **nur C** | — |
| Photovoltaik | nicht einschlägig | **nur C**, Feld vorhanden | — |

**Die Erfahrungswerte gelten für Weg A.** Probe am Kessel: 6 % von 14.760 €/a Brennstoffkosten
ergeben **885 €/a**. Auf den Bedarf bezogen wären dieselben 6 % rund 12.300 kWh Strom, also etwa
das Dreifache — **die Prozentsätze beider Wege sind nicht austauschbar**. Der Faktor zwischen ihnen
ist das Preisverhältnis Strom zu Brennstoff (bei 24,60 gegen 7,20 ct/kWh rund 3,4). Der Dialog muss
die Basis benennen und darf den Satz beim Umschalten der Bemessung **nicht stillschweigend
übernehmen**.

**Was das ablöst.** `% der Brennstoffkosten` und `% der Stromkosten` sind die Vorläufer von Weg A,
aber je Energieart getrennt und projektweit bemessen. Sie bleiben für Altdaten gültig und
verschwinden aus den Seeds; „Endenergie" fasst beide zusammen und ist zugleich anlagenscharf.

**Warum das zugleich die Steuerseite löst.** Weil Hilfsenergie definitionsgemäß Strom ist, lässt
sich aus jedem Betrag über den Strombezugspreis die Kilowattstundenzahl zurückrechnen. Jeder der
drei Wege liefert damit die Größe, die § 9 Abs. 1 Nr. 3 StromStG, § 9b StromStG und die
KWKG-Nettostromerzeugung brauchen. Ein zweites Feld „Hilfsstrom" ist überflüssig; die Rückrechnung
wird in der Herleitung ausgewiesen, damit sie nicht als gemessene Größe missverstanden wird.

**Keine Deckungswahl.** Die Befreiung des Eigenstroms nach § 9 Abs. 1 Nr. 3 StromStG ist eine
**bilanzielle Größe aus der gesetzlichen Vorgabe** — nicht die Feststellung, dass eine bestimmte
Kilowattstunde physisch aus dem eigenen Modul kam. Sie folgt aus den Anlagenbedingungen, die
ohnehin je Anlage geprüft werden (≤ 2 MW, hocheffizient, räumlicher Zusammenhang 4,5 km,
CO₂ < 270 g/kWh Energieertrag). Eine Anwenderwahl „Netz oder Eigen" bildet das falsch ab.

> **Warum die Speicher nur Weg C haben.** Erstens sind die **Umwandlungsverluste** eines Strom-
> oder Pufferspeichers **keine** Hilfsenergie — sie stecken bereits im Wirkungsgrad der
> Speicherrechnung; ein Prozentsatz auf den Durchsatz würde sie doppelt zählen. Zweitens ist ihr
> Hilfsbedarf (Klimatisierung, Batteriemanagement, Standby) überwiegend **zeit**abhängig und nicht
> durchsatzabhängig — ein Jahresbetrag bildet ihn richtiger ab als jeder Prozentsatz.

---

## 7 Wie eine Kostenposition entsteht — und was daran offen ist

### 7.1 Heutiger Weg

```
Auslieferungsvorlage (Seed, Schritt 39)
      │  Knopf „Aus Vorlage übernehmen…“   ← EINZIGER Auslöser
      ▼
Tab_ProjektWerte  (je Anlage, NurAnlegen-Regel: nie überschreiben)
      │
      ▼
Betrag = f(Bemessung, Satz, Menge)        ← Menge fehlt bei zehn Bemessungsarten
```

**Ein Projekt, in dem niemand den Knopf gedrückt hat, hat null Kostenpositionen.** Die
Anlagen-INSERTs berühren Kosten ausschließlich zur Ankerpflege.

### 7.2 Zwei Befunde vom 29.08.2026

**Befund 1 — übernommene Positionen rechnen 0 €/a.** Die Übernahme schreibt den Satz nach
`Einheitpreis` und lässt `Menge` auf `NULL`. Die Betragsformel bricht dort ab. Eine
Bezugsgrößen-Ermittlung existiert nur für sechs alte Schlüssel und wird allein vom abgelösten
`Form_Betriebskosten` bedient — die zehn Bemessungsarten der Etappe KD1 haben **keine**. Betroffen
sind unter anderem `je kWh elektrisch` (Vollwartung) und `% der Stromkosten`.

**Befund 2 — Namenskollision.** Die Vorlage sagt „Vollwartung / Wartung BHKW", der Altkatalog
„Wartung BHKW". Verschiedene Bezeichnung heißt verschiedene `StammID` — also zwei Zeilen für
dieselbe VDI-Position, je nach Entstehungsweg.

### 7.3 Vorgesehene Änderung

Pflichtpositionen sollen beim **Einrichten der Komponente** entstehen statt beim Öffnen des
Kostendialogs. Drei Bausteine in dieser Reihenfolge:

1. **Pflichtmerkmal** — Spalte `IstPflicht` an Vorlagenposition und Projektwert; Löschen gesperrt,
   „Satz auf 0 setzen" als Ausweg.
2. **Bezugsgrößen je Anlage** — der Auflöser für alle Bemessungsarten, anlagenscharf aus den
   Modulzeilen statt über Projektsummen. **Blockierend**: Ohne ihn rechnet jede Pflichtposition 0.
3. **Anlage bei Anlagenanlage** — nach dem Anlagen-INSERT, Muster `NurAnlegen`.

Die Auslieferungsvorlage behält alle Positionen; ihr wird nur die **Zuständigkeit für das
Entstehen** genommen. Eine Benutzervariante darf Pflichtpositionen ergänzen und bewerten, aber
nicht abwählen.

Mockups und Rechenwege dazu liegen als Artifact vor; die Einzelheiten stehen im
BHKW-Wirtschaftlichkeitskonzept.

---

## 8 Offene Punkte — konsolidiert

**Fachlich zu entscheiden**

| Nr. | Punkt | Herkunft |
|---|---|---|
| O1 | **Aufschläge Strom als Vorgabe EIN?** Gemessen +32 bis 34 % Energiekosten, −30 bis 33 % Kapitalwert | E5 |
| O2 | **§ 53 neben § 53a EnergieStG** — rechtlich ungeklärt, vor produktivem Einsatz mit dem Hauptzollamt klären | E4 |
| O3 | **Kategorie 3 „Energiekosten"** — entfernen oder als Override mit sichtbarem Vorrang definieren | K-Strang |
| O4 | **Anlagenscharfe Investitionssumme** für „% der Investition" statt projektweit — ergebniswirksam | 29.08.2026 |

**Bekannte Lücken im Rechenweg**

| Nr. | Punkt | Wirkung |
|---|---|---|
| O5 | **Bezugsgrößen für zehn Bemessungsarten fehlen** | jede so bemessene Position rechnet 0 €/a |
| O6 | **§ 54 bemisst auf den BHKW-Brennstoff**, entlastet aber Heizstoffe | Kesselbrennstoff bleibt ohne Entlastung |
| O7 | **Steuerwahl gilt je Projekt**, nicht je Anlage | Mehrbrennstoffprojekte nicht abbildbar |
| O8 | **Ohne Stundenreihen keine Stromsteuerbefreiung** | bewusst — „alles ist Eigenverbrauch" trägt gegenüber dem Hauptzollamt nicht |
| O9 | **`energy_carrier.density` im gesamten Bestand leer** | je Liter abgerechnete Träger mit Satz je 1.000 kg bekommen keine Gutschrift |
| O10 | **Energiesteuersätze vor 2024/2026 nicht eingesät** | erkennbare Lücke statt geratener Wert |
| O11 | **6.000-Vbh-Stufe § 8 Abs. 2** und Mindestabstand zur Alt-Inbetriebnahme | Datenmodell führt sie nicht |
| O12 | **Preissteigerung der Hilfsenergie** folgt der Betriebskosten-, nicht der Energiepreisreihe | null, solange beide gleich gepflegt sind |

**Prüf- und Nachweislücken**

| Nr. | Punkt |
|---|---|
| O13 | **Zahlenprobe gegen die Altanwendung fehlt** — seit der Abnahme E8 der gewichtigste offene Punkt |
| O14 | **Keine dauerhaften Tests** für `SteuerGutschriftRechner`, `KwkgSatzRechner`, `StromTarifRechner`, `KapitalwertRechner`; kein Regressionslauf über die Wirtschaftlichkeit |
| O15 | **Lokalisierung** der Wirtschaftlichkeitsdialoge — 63 deutsche Literale in drei Masken |
| O16 | **`Tab_Kraftwerkspark` ohne `Bezugsbasis`** — Faktoren je kWh Brennstoff und je kWh Strom stehen in derselben Spalte |

---

## 9 Doppelte Wahrheiten

Jede ist benannt und begründet — keine ist damit weniger als das, was sie ist.

| Doppelung | Stand |
|---|---|
| **Stromsteuersatz an zwei Orten** — Katalog `STROMST_REGELSATZ` gegen `const double` in `StromAufschlagModel` | wertgleich, aber ohne Kopplung: eine gepflegte Novelle erreicht den Aufschlagsblock nicht |
| **„Energieintensiv" an drei Orten** — Unternehmensart, Schnellwahlknopf im Trägerdialog, Katalogsatz | nicht gekoppelt; man kann produzierendes Gewerbe erfassen und den Regelsatz im Preis stehen lassen |
| **BHKW-Einspeisevergütung an vier Orten** | Vorrangregel eindeutig (aktiver Tarif schlägt Parameterwert), aber drei Felder zu viel |
| **Zwei Migrationsmechanismen** — `SchemaMigration` gegen Selbst-DDL in `WirtschaftlichkeitCtrl` | betrifft vier Tabellen; neue Spalten gehören an beide Stellen |
| **Zwei Lesewege auf die Kostenposition** — gespeicherte Access-Abfrage kennt die neuen Spalten nicht | zweiter direkter Zugriff ist inzwischen der Normalfall |
| **Komponenten-IDs an zwei Orten** — hart verdrahtet gegen dynamisch gelesen | betrifft `Form_Kosten` gegen `UcBkKosten` |
| **Vorrangregel Projekt vor Katalog** in drei Implementierungen | `KostenEmissionRechner`, `StromPreisCtrl`, eine Access-Abfrage |
| ~~Kennzahlenliste dreifach~~ | **aufgelöst** mit E7: `WirtschaftlichkeitZeilen` führt sie einmal |

---

## 10 Fallstricke beim Arbeiten

- **93 von 372 `.cs`-Dateien sind nicht UTF-8.** Vorhandene Kodierung beim Bearbeiten erhalten,
  sonst zerschießt der Diff die Datei. Vor Zeichen-Fixes einen Byte-Beweis führen.
- **Designer- und `.resx`-Dateien nicht von Hand editieren.** Visual Studio regeneriert
  `Resource.Designer.cs` selbst — parallele Handeinträge erzeugen Duplikate (CS0102).
- **`.accdb` ist in `.gitignore`.** Datenbankänderungen landen nie in einem Commit und müssen
  separat gesichert werden. Vor jedem Schreibzugriff prüfen, ob `Kenndaten.laccdb` existiert.
- **Läuft die Anwendung, ist `bin\` gesperrt** — Verifikationsbuilds mit `-p:OutDir=` umleiten.
- **Wegwerf-Harnesse nur unter `..\dev\`.** Eine `.cs`-Datei unterhalb von
  `WindowsFormsApplication1\` bricht den Build sofort.
- **`dotnet build` scheitert an den COM-Referenzen** — bauen nur über das MSBuild von Visual
  Studio, Plattform x64.
- **Die Sync-Automatik committet mit `git add -A`** — nach Parallelsitzungen repoweit auf
  Konfliktmarker prüfen.

---

## 11 Was aus den Quelldokumenten nicht mehr gilt

Damit niemand veraltete Aussagen weiterträgt:

| Aussage | Quelle | Richtig |
|---|---|---|
| „PlatformTarget x86" | Bestandsaufnahme § 1 | **x64** seit 22.08.2026 |
| „ODBC-DSN TEST, zwei Datenzugriffsschichten" | Bestandsaufnahme § 1 | ODBC vollständig entfernt |
| „Rechenkern `bhkwplan.dll` über COM" | Bestandsaufnahme § 1 | verwaltet in `Allgemein/BhkwPlan.cs` |
| „Keine Förderung/Zuschüsse abbildbar" | Bestandsaufnahme § 6.2 | Positionsart `zuschuss` seit K5 |
| „Energiesteuer kommt im Code nur in Konzeptdateien vor" | W4-Konzept § 1 | seit E4 vollständig gerechnet |
| „Vermiedener Strombezug ist keine Erlöszeile" | W4-Konzept § 1 | seit E5 als Differenzmethode ausgewiesen |
| „§ 53 entlastet nur den Stromanteil des Brennstoffs" | W4-Konzept § 4.2, Grundlagen § 3.2 (vor 19.08.) | **gesamter** BHKW-Brennstoff; Grundlagen § 3.5 berichtigt das |
| „Formular 1131a / Betriebserklärung 1131az" | Grundlagen § 4, W4-Konzept | **existiert nicht** — 1131 bzw. 1135 |
| „60.000 Vbh für Anlagen bis 50 kWel" | Altanwendung | § 8 Abs. 1: **30.000 Vbh** für alle neuen Anlagen |
| „Energiesteuer Öl 61,35 €/MWh" | Altanwendung | 61,35 € je **1.000 Liter** — Faktor rund 10 |
| „Stromsteuer −0,50 €/MWh als Erstattung" | Altanwendung | 0,50 €/MWh ist die **Rest**belastung; die Entlastung beträgt 20,00 €/MWh |
| „Hilfsenergie = % der Brennstoffkosten" | KD1-Seeds, W4-Konzept § 4.1 | **% der Endenergiekosten der Anlage**, alternativ % des Endenergiebedarfs oder fester Jahresbetrag (§ 6) |
| „Hilfsenergie überall prozentual bemessbar" | KD1-Seeds | Puffer-, Stromspeicher und PV: **nur fester Jahresbetrag** (§ 6) |
| „Instandhaltung Heizkessel = fester Jahresbetrag" | KD1-Seeds | **% der Investition, 1,5 – 2,5 %** (§ 5.2) |
