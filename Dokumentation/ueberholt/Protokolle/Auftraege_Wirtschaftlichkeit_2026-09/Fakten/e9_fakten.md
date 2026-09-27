# Faktenblatt E9 — Vollständige Szenarioabdeckung (V‑E)

Quellenstand: Worktree `C:\Waermeplan\EPOS-Plan\.claude\worktrees\q4`, Commit `fbe93de6` (23.09.2026
23:33:22, Merge „papiere455"). Nur gelesen, nichts geändert. Zusätzlicher Live-Abgleich gegen
`origin/ios_migration_september` am 24.09.2026 (siehe Abschnitt „Schemaschritt-Nummern" — der
Worktree-Stand ist inzwischen **acht Commits hinter origin** zurück).

---

## E9 laut Analysepapier (§ 5, Zeile E9, wörtlich)

Quelle: `Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md:467`
(Tabelle „Umsetzungsplan", Spalten Etappe | Inhalt | Größe | Rechenwirkung | Nachweis | Schema | Wiki |
Modell | Voraussetzung):

> **E9 V‑E Szenarioabdeckung** | Schritte **B–D** (Zeitraum und Mengenfaktor, Trägerpreise, Erlössätze),
> ±-Knopf an drei neuen Orten, Kern liest die Paare, Hinweistext entfällt; **ohne Degradation** (A5) | L
> | **ja**, je Pflege (NULL = wie Erwartet) | A/B je Projekt, Referenzlauf byte-gleich,
> `SzenarioParameterTests` je Größe | B, C, D (Nummern bei der Umsetzung; 114 und 115 sind zugesagt,
> voraussichtlich ab 116) | Wirtschaftlichkeit `szenarien`; wesentlich | Opus | A5 entschieden; E5, E7

Voraussetzung ist damit **A5** (Degradation entschieden, siehe unten) sowie **E5** (Ergebnisansicht,
umgesetzt #434) und **E7** (rechenwirksame Lücken, umgesetzt #437–#452) — beide sind laut Statustafel
bereits gebaut.

**Einordnung in § 5 „Reihenfolge und Begründung"** (Zeile 526–530):

> E7 und E9 sind die beiden Wellen mit Rechenwirkung; sie brauchen die Anker aus E1 und je einen
> A/B-Nachweis, der Referenzlauf sieht sie nicht.

**Relevante Befunde aus § 3 (Rechenkern/Architektur):**

- **R13** (`2026-09-19_Analyse...md:248`): „Vollständige Szenarien § 2.11.5: gemessen fehlt alles außer
  den Positionsspalten; das heutige Modell (pauschale Prozent- und Jahresausschläge je Zeile) ist ein
  anderes als die Best/Worst-Werte je Parameter des Konzepts" — Umfang **L, Schema**.
- **P5** (`2026-09-19_Analyse...md:258`): „… Szenario-±-Knopf nur an Kostenpositionen (ein Wirt), nicht
  an Trägerpreisen, Erlösfeldern, Rahmen" — Umfang „M, für ± L mit Schema". Das ist die Bauform, die E9
  auf die drei neuen Orte ausdehnt.

**A5 (Degradation), Stand je Entscheid** (`2026-09-19_Analyse...md:426`):

> **A5** | entschieden: **V‑E ohne Degradation** — der Widerspruch zum Szenarienkonzept (G3) ist
> aufgelöst; beide Papiere sind nachgezogen | E9

---

## Schritt B — Szenariorahmen (Betrachtungszeitraum, Mengenfaktor)

Quelle: `Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md:569`
(Tabelle § 6 „Schemaschritte dieser Etappen", Spalten Schritt | vormals | Inhalt | Tabelle |
Doppelpflicht `SpalteSicher` | Einfrierregel | Etappe):

> **B** | 98 | Szenariorahmen: `Szen_Best/Worst_Zeitraum`, `Szen_Best/Worst_Menge` (nicht `_Dauer`) |
> `Tab_ProjektWirtschaftlichkeit` | **ja** | nein | E9

Namensvorsicht ausdrücklich im Papier vermerkt (unmittelbar vor der Tabelle, § 6): `Szen_*_Dauer` ist
bereits vergeben für die **Nutzungsdaueränderung** (Teil des heute gebauten W5‑B‑9-Parametersatzes),
nicht für den Betrachtungszeitraum — Schritt B braucht deshalb einen eigenen Spaltennamen
(`Szen_Best/Worst_Zeitraum`) statt einer Wiederverwendung.

**Im konsolidierten Konzept** deckt sich das mit § 2.11.5, Zeile „Rahmen" (siehe unten): 6 von 8
Rahmen-Spalten sind seit Schritt 71 vorhanden (Zins, Preis_E, Preis_B je Best/Worst), **neu sind allein
Best/Worst des Betrachtungszeitraums**; die Zeile „Mengen" (Mengenfaktor je Szenario) ist komplett neu
und steht in der Konzepttafel als eigene Parameterklasse, nicht als Teil der Rahmen-Zeile — das
Analysepapier führt beides unter einem Schemaschritt **B**, das konsolidierte Konzept trennt sie
inhaltlich in zwei Zeilen der Tafel § 2.11.5.

---

## Schritt C — Trägerpreise best/worst

Quelle: `Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md:570`:

> **C** | 99 | Trägerpreise best/worst: `custom_price_work/base/power_best/_worst` |
> `energy_project_settings` | nein | nein | E9

Kein `SpalteSicher`-Doppelpflicht-Vermerk, keine Einfrierregel. Betrifft die Trägerkarte
(Preis-/Energieträgerdialog), Felder `custom_price_work`, `custom_price_base`, `custom_price_power` —
heute je Projekt und Energieträger eine Zeile in `energy_project_settings` (siehe Tabellenspalten
unten); neu wären je zwei weitere Spalten (`_best`/`_worst`) je Feld, also sechs neue Spalten.

---

## Schritt D — Erlössätze best/worst

Quelle: `Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md:571`:

> **D** | 100 | Erlössätze best/worst: `Einspeiseverguetung(_KWK)_Best/_Worst`; `DvEntgelt_Best/_Worst`,
> `PpaPreis_Best/_Worst` | `Tab_ProjektWirtschaftlichkeit`, `Tab_ProjektPhotovoltaik` | **ja** (PPV) |
> nein | E9

Doppelpflicht `SpalteSicher` ausdrücklich nur für **PPV** (`Tab_ProjektPhotovoltaik`) vermerkt, nicht
für `Tab_ProjektWirtschaftlichkeit`. Betroffene Felder heute: `Einspeiseverguetung` und
`Einspeiseverguetung_KWK` in `Tab_ProjektWirtschaftlichkeit`; `DvEntgelt` und `PpaPreis` in
`Tab_ProjektPhotovoltaik` (dort außerdem `PpaSpotAufschlag`, das die Tafel **nicht** nennt — siehe
Unklarheiten).

**Allgemeine Regel zu B/C/D** (unmittelbar vor der Schemaschritt-Tafel,
`2026-09-19_Analyse...md`, § 6): „Alle Schritte außer **F** (DDL mit einmaligem DML) und **G** (reines
DML) sind reines DDL ohne DML, ergebnisneutral bis zur ersten Pflege" — B, C und D sind damit alle drei
**reines DDL** (nur neue nullbare Spalten, kein Datenumbau, kein einmaliger Lauf über Bestandsdaten).

---

## Konsolidiertes Konzept zu E9

Datei: `Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` (2 797 Zeilen).

### § 2.11.4 „Etappen und Entscheidungen" — Zeile V‑E (Zeile 830)

> | **V-E** | Vollständige Szenarioabdeckung nach § 2.11.5 (V-G5, Umfang entschieden 31.08.2026),
> Risiko (V-G7), n-jährliche Zeitpunkte (V-G3) — **ohne Degradation (V-G2), A5** | Szenarioabdeckung
> und Freitext teils geliefert durch **W5‑B‑9** und **W5‑B‑12** (Migrationsschritte 71, 72) | **ja** —
> je Pflege, mit A/B-Nachweis; NULL = wie Erwartet hält die Etappe bis zur ersten Pflege
> ergebnisneutral | **E9** |

### § 2.11.2 Gap-Tabelle — V‑G5 (Zeile ~zwischen 762 und 776, Auszug der Zeile V‑G5)

> V-G5 | **Szenarien = gleichzeitige Variation aller Einstellparameter** — auch r, T, Preisraten,
> Mengen (7.3) | Best/Worst variieren nur die Kosten-/Betragsspalten; r, T, p sind je Szenario fix |
> **entschieden 31.08.2026: vollständige Abdeckung** — alle Parameter (Investition, Energiekosten,
> Betriebskosten, Erlöse, Rahmen, Mengen) erhalten Best/Worst-Werte; Modell in § 2.11.5

### § 2.11.5 „Vollständige Szenarioabdeckung" (Zeile 840 ff.) — die zentrale Tafel

> **Umfang (V-G5, → Register R‑V):** Alle Parameter — Investitionskosten, Energiekosten,
> Betriebskosten, Erlöse, dazu Rahmen und Mengen — werden mit Best- und Worst-Case-Werten versehen.

Parametertafel (wörtlich):

| Parameterklasse | Ort des Erwartet-Werts | Best/Worst | Stand |
|---|---|---|---|
| Investitionskosten je Position | `Tab_ProjektWerte` Kat. 1 | `BestCase`/`WorstCase` (+ Nutzungsdauern) | **vorhanden** |
| Betriebskosten je Position | `Tab_ProjektWerte` Kat. 2 | dito | **vorhanden** |
| Energiepreise je Träger | `energy_project_settings.custom_price_work` (+ Grundpreis) | neue Spalten `custom_price_work_best/_worst` (Grund-/Leistungspreis analog, nullable) | **neu** |
| Erlössätze (Marktgrößen) | `Einspeiseverguetung`, `Einspeiseverguetung_KWK`, PPA-/DV-Preise des PV-Dialogs | je Feld ein Best/Worst-Paar an derselben Tabelle | **neu** |
| Rahmen | `Tab_ProjektWirtschaftlichkeit`: `Zinssatz`, `Betrachtungszeitraum`, `Preissteigerung_Energie`, `Preissteigerung_Betrieb` | je Größe `_Best`/`_Worst` (8 Spalten), NULL/0 = wie Erwartet | **6 von 8 vorhanden** seit Schritt 71; neu sind allein Best/Worst des Betrachtungszeitraums. Namensvorsicht: `Szen_*_Dauer` ist die Nutzungsdaueränderung, nicht der Betrachtungszeitraum |
| Mengen (Simulationsergebnis) | Stromerzeugung, Wärme, Einspeisung … | ein Mengenfaktor [%] je Szenario an der Rahmenzeile | **neu** |

Ausdrücklich **nicht** szenariert: gesetzliche Sätze (KWKG-Zuschläge, Energie-/Stromsteuersätze,
BEHG-Festpreise) — deren Zukunft bildet der Katalogpfad ab (GESICHERT/PROGNOSE), nicht ein Worst-Case.

Regeln (wörtlich zusammengefasst):

- „NULL/0 heißt **wie Erwartet**" — Abnahmekriterium der Etappe.
- VALERI-Vorrang unverändert: `szenarioGepflegt ⇔ |Wert − Erwartet| > 1e−9`.
- Sensitivität bleibt getrennt (ceteris paribus) von den Szenarien (alles gleichzeitig).
- Pflege: der vorhandene ±-Knopf (`CaseEingabeDialog`) als einheitliches Muster auch an
  Trägerpreisen, Erlösfeldern und der Rahmen-Gruppe; „12 von 31 Parametern szenariert" als
  Beispieltext des Ausweises.
- Ausweis im Bericht (Norm 9c): Kalkulationstabelle je Szenario nennt die Parametereinstellungen
  vollständig.

### § 2.11.7 „Hinweistext bis zur vollständigen Szenarioabdeckung"

Der heute aktive Hinweistext (`WIRT_SZEN_HINWEIS`, gebaut #434) endet mit dem Satz, den E9
überflüssig macht:

> Die vollständigen Parametersätze je Szenario — auch Zeitraum, Trägerpreise, Erlössätze und ein
> Mengenfaktor — kommen nach dieser Darstellung; bis dahin steht dieser Hinweis unter der Tafel.

„Der Hinweis entfällt mit der Etappe, die ihn überflüssig macht" — das ist E9 (Analysepapier §5-Zeile:
„Hinweistext entfällt").

### § 7 „Vorgeschlagene Reihenfolge" und Anhang — E9-Nennungen

`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md:2706`:

> Als Nächstes kommt **E9** (V‑E, die Szenarioabdeckung nach § 2.11.5): die Schemaschritte B
> (Betrachtungszeitraum und Mengenfaktor je Szenario), C (Trägerpreise best/worst) und D (Erlössätze
> best/worst) mit ihren Nummern bei der Umsetzung, der ±-Knopf an den neuen Orten, der Kern liest die
> Paare, der Hinweistext (§ 2.11.7) entfällt; ohne Degradation (A5), rechenwirksam je Pflege.

Anhang-Tafel, Zeile V‑A…V‑E (`:2756`):

> **V-A…V-E** (§ 2.11.4) | W5‑B‑9…W5‑B‑12 | — | V-A = **#434**, V-C = **#454**, V-D = **#455** | ValERI:
> W5‑B‑9/10/11/12 gebaut (Schritte 71, 72); V-A = **E5** (gebaut), V-C = **E8** Teil a (gebaut #454; …),
> V-D = **E8** Teil b (gebaut #455), **V-E = E9**

Anhang-Etappentafel, letzte Zeile (`:2787`):

> **E9** … **E12** | V-E (Schritte B, C, D) · ND-S3 · Wiki (E11 entfällt) | **nächste Etappe: E9** (offen
> die acht Fragen aus E7c3 und die sechs aus E8b, → Register R‑E7c3, R‑E8b)

---

## Szenarienkonzept — W5‑B‑9 … W5‑B‑12 und ihr Verhältnis zu E9

**Datei liegt nicht in `Wirtschaftlichkeit_Kosten/`, sondern eine Ebene höher:**
`Dokumentation/aktuell/Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md` (618 Zeilen; siehe
Unklarheiten). Kopf (Zeile 1–9):

> # Konzept — Szenarioparameter und VALERI-Abgleich (Wirtschaftlichkeit)
> **Anwenderentscheid 09.09.2026** · Etappen **W5‑B‑9** (Szenarioparameter) und **W5‑B‑10**
> (VALERI-Abgleich nach DIN EN 17463).
> **Stand 23.09.2026** · Codestand `704356a4` · `SchemaStand.Zielversion` = **113** … Die Etappen
> W5‑B‑9 bis W5‑B‑12 sind gebaut; ihre Fortsetzung läuft unter der Reihe **V-A…V-E** des
> konsolidierten Konzepts (§ 2.11.4) im Etappenplan **E0–E12** des Analysepapiers. **Entscheid A5 vom
> 20.09.2026 (nach Empfehlung): V-E rechnet die Degradation nicht ein**

### § 2 „Der Parametersatz je Szenario (Etappe W5‑B‑9)" — was heute schon variiert

Sechs Größen (plus eine siebte seit W5‑B‑12), an `Tab_ProjektWirtschaftlichkeit` (Migrationsschritt 71
für die ersten sechs × 2, Migrationsschritt 72 für die siebte × 2):

| Größe | Feld (Klasse `SzenarioSatz`) | Einheit | Vorgabe Best | Vorgabe Worst |
|---|---|---|---|---|
| Kalkulationszins | `Zinssatz` | % | i − 1 %-Pkt (nie < 0) | i + 1 %-Pkt |
| Preissteigerung Energie | `PreissteigerungEnergie` | %/a | p_E − 1 %-Pkt | p_E + 1 %-Pkt |
| Preissteigerung Betrieb | `PreissteigerungBetrieb` | %/a | p_B − 1 %-Pkt | p_B + 1 %-Pkt |
| Investitionsänderung | `InvestitionAenderung` | % | − 10 % | + 10 % |
| Ertragsänderung | `ErtragAenderung` | % | + 10 % | − 10 % |
| Nutzungsdaueränderung | `NutzungsdauerAenderung` | a | + 2 a | − 2 a |
| *(seit W5‑B‑12)* Preissteigerung Investition/Ersatz p_I | `PreissteigerungInvestition` | %/a | Erwartet‑p_I − 1 %-Pkt | Erwartet‑p_I + 1 %-Pkt |

„**Erwartet** bekommt **keinen** Parametersatz" — rechnet zahlengleich mit den Projektparametern
(Regressionsanker, § 6). Vorrangregel (§ 2.2): „Ein gepflegter Szenariowert je Zeile hat Vorrang. Der
Parametersatz greift genau dort, wo keiner gepflegt ist." Nicht skaliert werden Zuschusszeilen
(`ZUSCHUSS`), Betriebskostenzeilen (p_B ist dort der Hebel) und gesetzliche Erlösreihen
(KWKG/Energiesteuer/Stromsteuer).

### § 4 „Dialog „Parameter…" — Abschnitt „Szenarien"" — heutiger Dialog

> Drei Spalten, **sieben Zeilen** (seit W5‑B‑12 mit p_I) … Die Erwartet-Spalte ist **Anzeige, kein
> Eingabefeld** … Der Knopf **„Vorgaben"** setzt alle **vierzehn** Felder auf NULL zurück.

Das ist der Baustein, den E9 laut Analysepapier-Zeile „±-Knopf an drei neuen Orten" **nicht**
wiederverwendet, sondern um den bereits vorhandenen `CaseEingabeDialog`-Baustein (Kosten) ergänzt (vgl.
§ 2.11.5-Regel „Pflege" oben und Befund P5).

### § 11.1 „Zuordnung der Etappen W5‑B‑9…W5‑B‑12 zu V-A…V-E" (Zeile 607–617, wörtlich)

> | hier | konsolidiertes Konzept | Stand |
> |---|---|---|
> | **W5‑B‑9** Parametersatz je Szenario (§ 2, Migrationsschritt 71) | Teil von **V-E** (vollständige
> Szenarioabdeckung) | gebaut — V-E bleibt für Rahmen, Trägerpreise, Erlössätze, Mengenfaktor offen |
> | **W5‑B‑10** VALERI-Abgleich (§ 7) | Grundlage der Gap-Tafel **V-G1…V-G12** (§ 2.11.2) | gebaut |
> | **W5‑B‑11** Umsetzung der Entscheidungen (§ 9) | **V-B** ≡ Etappe „VG" der Statuszeile **#358**
> (wählbare Referenz, Schemaschritt 92) | gebaut |
> | **W5‑B‑12** Preisindizierung p_I und Freitext (§ 10, Migrationsschritt 72) | Teil von **V-E** (p_I)
> und **V-G11** (Freitext) | gebaut — von V-G11 fehlen Kategorie und Beurteilung |

Damit deckt E9 aus der Reihe W5‑B‑9…12 **ausschließlich den nicht gebauten Rest von V‑E** ab: Rahmen
(Zeitraum, Mengenfaktor), Trägerpreise, Erlössätze — nicht W5‑B‑10/11/12 selbst (die sind bereits
gebaut) und nicht das Freitextfeld aus V‑G11 (Kategorie/Beurteilung bleiben offen, gehören aber nicht
zur E9-Zeile des Analysepapiers).

---

## Register-Entscheide mit E9-Bezug

Datei: `Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md`. Alle Zeilen,
die „E9" nennen (Volltextsuche, fünf Treffer):

**R‑A, Zeile 73 (A5):**

> | **A5\*** | Degradation: V‑E des Konzepts rechnet sie ein, G3 des Szenarienkonzepts lehnt sie ab |
> Entscheid neu stellen; bis dahin V‑E ohne Degradation planen | 20.09.2026, nach Empfehlung,
> ausdrücklich bestätigt | § 2.11.2 (V‑G2), § 2.11.4 (V‑E); Szenarienkonzept § 9.4 | entschieden:
> **V‑E ohne Degradation** — der Widerspruch zum Szenarienkonzept (G3) ist aufgelöst; beide Papiere
> sind nachgezogen · **E9** |

**R‑V, Zeile 148 (V‑4):**

> | **V-4** | Szenario-Parametersätze (V-G5) sofort oder nach V-A–V-D? | **Umfang entschieden**
> (§ 2.11.5). **Zeitpunkt entschieden 18.09.2026 nach Empfehlung: danach**, einzige Etappe mit
> Rechenwirkung, eigener A/B-Nachweis — **mit Hinweistext** bis dahin (§ 2.11.7); der Hinweistext ist
> **gebaut #434** | Umfang 31.08.2026 (V‑G5); Zeitpunkt 18.09.2026, nach Empfehlung | § 2.11.5,
> § 2.11.7 | Hinweistext gebaut #434; die Szenarioabdeckung selbst offen (**V‑E, E9**) |

**R‑V, Zeile 149 (V‑G2):**

> | **V-G2** | **Degradation** je Position [%/a] **mit Quellenangabe** (6.3.1/6.3.3) | Degradation —
> **Entscheid A5 vom 20.09.2026 (nach Empfehlung)**: Der Entscheid „G3 nicht umsetzen" gilt, V-E
> (§ 2.11.4) wird **ohne** Degradation geplant; die Vereinfachung bleibt offengelegt | 20.09.2026, nach
> Empfehlung (A5) | § 2.11.2, § 2.11.4 | entschieden; V‑E wird ohne Degradation geplant (**E9**) |

**R‑V, Zeile 150 (V‑G5):**

> | **V-G5** | **Szenarien = gleichzeitige Variation aller Einstellparameter** — auch r, T, Preisraten,
> Mengen (7.3) | **entschieden 31.08.2026: vollständige Abdeckung** — alle Parameter (Investition,
> Energiekosten, Betriebskosten, Erlöse, Rahmen, Mengen) erhalten Best/Worst-Werte; Modell in § 2.11.5
> … | 31.08.2026, Anwender | § 2.11.5 | offen — **V‑E (E9)** |

**Zeile 280 (Nr. 20, indirekter E9-Bezug über den Nachweisweg):**

> | **Nr. 20** | Zahlenprobe gegen die Altanwendung (A8, ≡ B9) | „BHKW-Plan-Mappen: nicht relevant." —
> die Zahlenprobe entfällt … | 22.09.2026, Anwender | § 6.3 Nr. 20; § 7 (B9); § 6.2 | entfällt — der
> Nachweis läuft über die Anker aus E1 und die A/B-Nachweise der rechenwirksamen Etappen (**E7, E9**,
> E10) |

Kein Register-Eintrag entscheidet die Schritte B/C/D selbst oder das V‑G7-Risikomodul inhaltlich neu —
alle vier direkten E9-Zeilen betreffen **Umfang** (V‑G5, entschieden) und **Degradation** (A5/V‑G2,
entschieden); Zeitpunkt/Reihenfolge (V‑4) ist entschieden, die Ausführung selbst steht aus.

---

## Mockup `Dialog_Formel_Zahlenprobe.html`

Datei: `Dokumentation/aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`. Keine eigene, gesondert
beschriftete „Parameterdialog der Szenarien"-Sektion im Mockup selbst (das ist die Ergebnisseiten-
Kategorie 8 plus der Anhang „Umsetzungsstand"); der Parameterdialog mit dem Reiter „Szenarien" (drei
Spalten, sieben Zeilen) ist im **Szenarienkonzept § 4** beschrieben (siehe oben), nicht im HTML-Mockup
selbst — einziger Codeverweis im Mockup ist die Ressourcentafel-Zeile (`WirtschaftlichkeitParameterDialog`
als Fundort von `WPAR_PREIS_I`).

**U15 — die maßgebliche Zeile des Anhangs „Umsetzungsstand"** (Kategorie 8):

> Stand: offen | Nr.: `U15` | Abschnitt: 8 | Was fehlt: Vollständige Szenarioabdeckung — Trägerpreise,
> Erlössätze und ein Mengenfaktor je Szenario; bis dahin gilt der Hinweistext unter der Annahmentafel |
> Ort im Programm: Konzept § 2.11.5 | Abnahme: A/B-Nachweis: je Szenario ein vollständiger
> Parametersatz, Kapitalwerte gegenübergestellt

**Zeile ~4668** (Fließtext direkt vor der U-Tafel, Abschluss des E8-Absatzes):

> E8 ist damit abgeschlossen; die nächste Etappe ist **E9** (U15, die vollständige Szenarioabdeckung).

**Zeile ~4710** (U27, PV-Dialog-Vorschau — Randbemerkung zu Feldern ohne Szenario-Bezug):

> Marktwert-Override und Marktwertentwicklung des Modells haben im Dialog kein Feld — sie gehören zur
> Szenarioabdeckung (U15)

`MarktwertJahresmittel`/`MarktwertEntwicklung` (siehe Tabellenspalten `Tab_ProjektPhotovoltaik` unten)
sind damit laut Mockup-Anhang ausdrücklich **nicht** von der Analysepapier-Zeile D erfasst
(die nennt nur `DvEntgelt`/`PpaPreis`), werden aber vom Mockup-Anhang trotzdem der Szenarioabdeckung
(U15) zugerechnet — ein Unterschied im Umfang zwischen Analysepapier-Schritt D und Mockup-U15 (siehe
Unklarheiten).

---

## Statusdatei `Status_iOS_Migration.md`

**Nach #455, Punkt (g)** (Zeile 552, Auszug):

> (g) **Nächste Etappe: E9** (V‑E, Analysepapier § 5) — die Schemaschritte B (Szenariorahmen:
> Betrachtungszeitraum und Mengenfaktor je Szenario an `Tab_ProjektWirtschaftlichkeit`), C
> (Trägerpreise best/worst an `energy_project_settings`) und D (Erlössätze best/worst an
> `Tab_ProjektWirtschaftlichkeit` und `Tab_ProjektPhotovoltaik`), der ±-Knopf an drei neuen Orten, der
> Kern liest die Paare, der Hinweistext entfällt; ohne Degradation (A5); rechenwirksam je Pflege (NULL
> = wie Erwartet), A/B je Projekt und Referenzlauf. **Nummern geprüft (23.09.2026, nach `git fetch`):**
> `origin/ios_migration_september` = `ada7d1ac` und alle lokalen Zweige außer `z3` stehen auf
> `SchemaStand.Zielversion` = 113; `z3` (Zapfprofil-Stufe Z3, #453) trägt 114, 115 ist der
> Nachbarsitzung „Dialog Design" zugesagt — die Schritte B bis D nehmen voraussichtlich 116 ff. und
> messen bei der Umsetzung neu; die Statusnummer nimmt E9 hinter #456 bis #459 („Dialog Design"),
> voraussichtlich #460. Offen beim Anwender: E7c3‑Q1 bis E7c3‑Q8 und E8b‑Q1 bis E8b‑Q6; nach dem
> Entscheid die zwei kleinen Aufträge zu E8b‑Q2 und E8b‑Q3.

**Nach #436, Randnotiz** (Zeile 569, Auszug zu einem konkreten Code-/Ressourcenfund):

> die Spalten der Szenariotafel im Parameterdialog heißen „Best"/„Worst" (`WPAR_SZ_SPALTE_BEST`,
> `_WORST`)

**Nach #434, Randnotiz** (Zeile 571, Auszug): bestätigt Szenarionamen „Ungünstig"/„Günstig" (E5‑Q2),
Spannenberechnung aus größtem/kleinstem Szenariowert (E5‑Q4) — Hintergrund zur heutigen
Ergebnisdarstellung der drei Szenarien, kein direkter E9-Bauinhalt.

---

## Codestellen

**Heutiger Szenario-Parametersatz** — `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitDaten.cs`:

- `:49` — `public class SzenarioSatz` — die sieben nullbaren Felder (Zinssatz,
  PreissteigerungEnergie, PreissteigerungBetrieb, InvestitionAenderung, ErtragAenderung,
  NutzungsdauerAenderung, PreissteigerungInvestition), Doku-Kommentar nennt „sechs Größen" (die siebte
  kam erst mit W5‑B‑12 dazu).
- `:242` — `public class WirtschaftlichkeitParameter` — Parametersatz eines Rechenlaufs, eine Zeile je
  Stammprojekt (`Tab_ProjektWirtschaftlichkeit`).
- `:493`/`:496` — `public SzenarioSatz SatzBest`/`SatzWorst` — Vorgabe-Instanzen je Projekt.
- `:504` — `public SzenarioSatz SatzFuer(string szenario)` — liefert `null` für ERWARTET (Zusage:
  Erwartet rechnet unverändert).
- `:526` — `public WirtschaftlichkeitParameter FuerSzenario(string szenario)` — für Best/Worst eine
  flache Kopie mit ersetztem Zins/Preissteigerungen; „Alles Weitere (Betrachtungszeitraum,
  Einspeisevergütung, KWKG, Steuern, Bilanzierung) bleibt unverändert" — genau die Lücke, die E9
  schließt.

**Bewertung/Bandbreite** (verbraucht die drei Szenarien, definiert sie nicht) —
`EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitBewertung.cs`: Zeilen 39/42/45 (ΔKW je
Szenario Worst/Erwartet/Best), Zeile 89/97 „Drei vollständige Läufe" — dieser Rechner liest fertige
Ergebniszeilen je Szenario, enthält aber keine Definition, welche Größen variieren.

**Parameterdialog** — `EPOS.UI/Dialoge/Wirtschaftlichkeit/WirtschaftlichkeitParameterDialog.razor`:

- `:121` — `<Gruppenkopf Titel="@_t.GSzenarien" Symbol="⚖">` — Beginn des Abschnitts „Szenarien".
- `:125–133` — Tabellenkopf mit den vier Spalten Größe/Erwartet/Best/Worst.
- `:135` (Zins), `:187` (PreisI), `:203` (Invest), `:217` (Ertrag), `:231` (Dauer) — je eine
  Tabellenzeile mit editierbarem `Zahlenfeld` in den Best-/Worst-Spalten, Erwartet-Spalte ist reine
  Anzeige (`epos-matrix-zelle`, kein Eingabefeld).
- `:248` — Knopf „Vorgaben" (`VorgabenKlick`) setzt alle vierzehn Felder zurück.
- `:250`/`:251` — Herleitungszeilen `SzenarioZeileBest`/`SzenarioZeileWorst`.

Diese Tabelle ist **nicht** der `CaseEingabeDialog`-±-Knopf, sondern ein eigenes, direktes
Bearbeitungsraster nur für die sieben Rahmengrößen — bestätigt den Befund P5 (± bisher nur an
Kostenpositionen, hier eine dritte, wieder andere Bauform für den Rahmen).

**±-Knopf-Baustein (heute nur an Kostenpositionen)**:
`EPOS.UI/Dialoge/Kosten/CaseEingabeDialog.razor`, `CaseEingabeErgebnis.cs`, `CaseEingabeKiSicht.cs` —
Hilfekennung `Form_CaseEingabe`, Prüfmuster unter
`Werkzeuge/Formularkarte.Tests/Pruefmuster/Kosten/Form_CaseEingabe.cs`. Liegt im Ordner `Dialoge/Kosten`,
nicht `Dialoge/Wirtschaftlichkeit` — für E9 „±-Knopf an drei neuen Orten" ist damit zu klären, ob dieser
Baustein an Trägerpreisen/Erlösfeldern/Rahmen wiederverwendet oder nachgebaut wird (kein Fund einer
bereits vorhandenen Wirtschaftlichkeits-Variante).

**Schema-Update-Muster** — `EPOS.Kern/Allgemein/Update/`:

- `SchemaStand.cs:428` — `public const int Zielversion = 113;` (Stand des Worktrees/Commit `fbe93de6`).
- `SchemaKatalog.cs:4536–4554` — Muster für **reines DDL, mehrere neue nullbare Spalten in einem
  Block**: die zwölf Szenario-Spalten aus Migrationsschritt 71 stehen als `SchemaSpalte(...)`-Einträge
  mit begleitenden `SPALTE_PW_SZEN_*`-Konstanten (`:4569` ff.) — kein eigenes DML, keine eigene
  Migrationsklasse. Ebenso `SchemaKatalog.cs:4114/4125` (`SPALTE_EA_KWKG_ABWAERMEABFUHR`,
  `SPALTE_EA_KWKG_STROMKENNZAHL`, Schritt 105/A) und `:4166/4175`
  (`SPALTE_PW_ERSATZ_FUEHREN`/`RESTWERT_ANSETZEN`, Schritt 111/E) — beide ebenfalls reine
  Spaltendeklarationen ohne eigene `.cs`-Datei. **Schritte B, C, D dürften nach diesem Muster laufen**
  (reine `SchemaSpalte`-Deklarationen), nicht nach dem Muster von Schritt 113.
- `GaseNormkubikmeter.cs` (Schritt 113 = G, „reines DML", 213 Zeilen) — Gegenbeispiel für einen Schritt
  MIT eigener Migrationsklasse, weil er Bestandsdaten per `UPDATE` ändert (Stammtext „m³" → „Nm³");
  Dokublock (`:6–33`) beschreibt Befund, Umfang, Ergebnisneutralität nach demselben Muster wie die
  Konzeptpapiere. Für B/C/D (reines DDL ohne DML) ist vermutlich **keine** vergleichbare eigene Klasse
  nötig.
- „Katalog-Generation 9": kein Bezug zu einem Schema-Update-Skript, sondern zum **Gesetzeskatalog**
  (`EPOS.Kern/Allgemein/Wirtschaftlichkeit/GesetzKatalog.cs:1068`, `:1449` — Kommentare zu
  abgekündigten KWKG-Zeilen/Nachpflege der Generation 9); betrifft E7 Teil c3 (#452), nicht E9.

---

## Heutige Spalten der drei Tabellen

Quelle: `PRAGMA table_info(...)` gegen `Referenzlaeufe/Kenndaten_Test.sqlite` (read-only, `mode=ro`,
`py -c` mit `sqlite3`-Modul). Nur die für E9 relevanten Spalten hervorgehoben.

### `Tab_ProjektWirtschaftlichkeit` (45 Spalten, ID 0–44)

Rahmen/Erwartet: `Zinssatz`, `Betrachtungszeitraum`, `Preissteigerung_Energie`,
`Preissteigerung_Betrieb`, `Preissteigerung_Investition` (Migrationsschritt 72),
`Einspeiseverguetung`, `Einspeiseverguetung_KWK`.

Bereits vorhandene Szenario-Spalten (Migrationsschritt 71, zwölf Spalten): `Szen_Best_Zins`,
`Szen_Best_Preis_E`, `Szen_Best_Preis_B`, `Szen_Best_Invest`, `Szen_Best_Ertrag`, `Szen_Best_Dauer`,
`Szen_Worst_Zins`, `Szen_Worst_Preis_E`, `Szen_Worst_Preis_B`, `Szen_Worst_Invest`,
`Szen_Worst_Ertrag`, `Szen_Worst_Dauer`; dazu (Migrationsschritt 72): `Szen_Best_Preis_I`,
`Szen_Worst_Preis_I`.

**Fehlen heute** (durch PRAGMA bestätigt nicht vorhanden): `Szen_Best_Zeitraum`,
`Szen_Worst_Zeitraum`, ein Mengenfaktor je Szenario, `Einspeiseverguetung_Best/_Worst`,
`Einspeiseverguetung_KWK_Best/_Worst` — das ist genau der Umfang der Schritte B (Zeitraum/Menge-Teil)
und D (Erlössätze-Teil an dieser Tabelle).

### `energy_project_settings` (44 Spalten, ID 0–43)

Erwartet-Felder vorhanden: `custom_price_work`, `custom_price_base`, `custom_price_power` (dazu
`custom_hi`, `custom_hs`, `co2`, `so2`, `nox`, diverse `Aufschlag_*`/`Anteil_*`, `Preisbasis` seit
Schritt 112, `Leistungspreis_Staffelgrenze/_Staffel1/_Staffel2` seit Schritt 104).

**Fehlen heute:** `custom_price_work_best/_worst`, `custom_price_base_best/_worst`,
`custom_price_power_best/_worst` — genau Schritt C, sechs neue Spalten.

### `Tab_ProjektPhotovoltaik` (22 Spalten, ID 0–21)

Erwartet-Felder vorhanden: `DvEntgelt`, `PpaPreis`, `PpaSpotAufschlag`, `MarktwertJahresmittel`,
`MarktwertEntwicklung`, `Degradation` (Einzelwert, keine Szenario-Spalte — konsistent mit A5/V‑G2 „ohne
Degradation").

**Fehlen heute:** `DvEntgelt_Best/_Worst`, `PpaPreis_Best/_Worst` — der PV-Teil von Schritt D.
`PpaSpotAufschlag`, `MarktwertJahresmittel`, `MarktwertEntwicklung` nennt weder die Analysepapier-Zeile
D noch die § 2.11.5-Tafel als Szenariofeld, obwohl das Mockup (U27, oben) `MarktwertEntwicklung`
ausdrücklich der Szenarioabdeckung (U15) zurechnet.

---

## Schemaschritt-Nummern

**Im Worktree (Commit `fbe93de6`, Stand 23.09.2026 23:33):** `SchemaStand.cs:428` →
`Zielversion = 113`. Deckt sich mit allen gelesenen Papieren (Analysepapier, konsolidiertes Konzept,
Szenarienkonzept, Statusdatei): „nächster freier Schritt 114", zugesagt an `z3` (Zapfprofil-Stufe Z3,
#453) und an die Nachbarsitzung „Dialog Design" (115); E9 (B–D) rechnet deshalb überall mit
„voraussichtlich ab 116".

**Live-Abgleich `git show origin/ios_migration_september:.../SchemaStand.cs | grep Zielversion` am
24.09.2026:**

```
public const int Zielversion = 114;
```

`origin/ios_migration_september` steht auf Commit `aab9896e` (24.09.2026 00:57) — **acht Commits
weiter** als der Worktree-Stand `fbe93de6` (`git log --oneline fbe93de6..origin/ios_migration_september`
zeigt ausschließlich Commits zu „Kuehlung KU2 W1/W2", u. a. `24074b3a Kuehlung KU2 W1: Schritt 114
(KU-S3), Kuehlbetrieb am Erzeuger`). **Schritt 114 ist damit inzwischen von der Kühlung (KU2, Welle 1)
belegt — nicht vom Zapfprofil Z3**, wie es die am 23.09.2026 geschriebenen Papiere und die Statuszeile
noch annehmen. Der lokale Zweig `z3` selbst führt ebenfalls `Zielversion = 114` und ist nicht auf dem
neuen origin-Stand aufgebaut (`git merge-base --is-ancestor origin/... z3` → nein) — er wird beim
nächsten Zusammenführen neu nummerieren müssen.

Eine Stichprobe (`git grep` über `origin/ios_migration_september`, Kern-Ordner) nach möglichen
E9-Spaltennamen (`Szen_Best_Zeitraum`, `Szen_Best_Menge`, `custom_price_work_best`,
`Einspeiseverguetung_Best`, `DvEntgelt_Best`, `PpaPreis_Best` u. ä.) und nach E9-Commits
(`git log --all --grep=E9` u. ä.) ergab **keinen Treffer** — E9 ist nach heutigem Stand nirgends
begonnen.

**Für die E9-Planung folgt:** Die in allen Papieren wiederholte Annahme „114 und 115 sind zugesagt, B–D
also ab 116" ist mit dem Origin-Stand vom 24.09.2026 überholt, weil **114 bereits vergeben ist** (an
KU2, nicht an Z3) und **Z3 selbst noch umnummerieren muss**. Der tatsächlich nächste freie Schritt am
24.09.2026 ist nicht mit letzter Sicherheit zu bestimmen, ohne auch den Stand von „Dialog Design" (115)
und den weiteren Verlauf von KU2 zu prüfen — beides lag außerhalb des Auftrags dieses Faktenblatts.

---

## Unklarheiten

1. **Schemaschritt-Kollision 114 (neu, 24.09.2026):** Origin hat seit der Worktree-Erstellung acht
   Commits erhalten; `Zielversion` steht dort auf 114, vergeben an „Kuehlung KU2 W1: Schritt 114
   (KU-S3)" — nicht an das in allen gelesenen Wirtschaftlichkeits-Papieren genannte „Zapfprofil Z3".
   Der lokale Zweig `z3` führt ebenfalls 114, ist aber nicht auf dem neuen origin-Stand aufgebaut. Wer
   E9 konkret umsetzt, muss die Schemaschritt-Nummern B/C/D neu gegen den dann aktuellen
   `SchemaStand.Zielversion` messen — die Zahl „116" aus den Papieren ist nicht mehr verlässlich.
2. **Ordnerzuordnung der Quellpapiere:** Das Szenarienkonzept (`Konzept_Wirtschaftlichkeit_
   Szenarien_VALERI.md`) liegt in `Dokumentation/aktuell/`, nicht — wie der Auftrag annahm — in
   `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/`.
3. **§-Nummern des konsolidierten Konzepts weichen vom Auftrag ab:** § 2.13 dort heißt „Ergebnisansicht"
   (nicht „Szenarien" — die Szenariotafel steht in § 2.11.5); § 6 heißt „Umsetzungsstand" und enthält
   eine Rückschau-Tafel bereits gebauter Schritte, aber **keine** eigene Schemaschritt-Definitionstafel
   für B/C/D — diese steht ausschließlich im Analysepapier § 6 (Zeilen 569–571). § 7
   „Vorgeschlagene Reihenfolge" und der Anhang „Kürzel und Etappen" tragen die dort gesuchten
   E9-Etappenzeilen.
4. **Umfang von Schritt D vs. Mockup U15/PV-Dialog:** Das Analysepapier nennt für
   `Tab_ProjektPhotovoltaik` nur `DvEntgelt_Best/_Worst` und `PpaPreis_Best/_Worst`; weder diese Zeile
   noch die Konzepttafel § 2.11.5 erwähnen `PpaSpotAufschlag`, `MarktwertJahresmittel` oder
   `MarktwertEntwicklung`. Das Mockup (U27) ordnet `MarktwertEntwicklung`/„Marktwert-Override" jedoch
   ausdrücklich der Szenarioabdeckung (U15) zu. Ob diese Felder zu Schritt D gehören, ist aus den
   gelesenen Quellen nicht eindeutig zu entscheiden.
5. **±-Knopf-Wiederverwendung:** Weder Analysepapier noch Konzept noch Code legen fest, ob die „drei
   neuen Orte" des ±-Knopfs den bestehenden `CaseEingabeDialog` (heute `EPOS.UI/Dialoge/Kosten/`,
   Hilfekennung `Form_CaseEingabe`) technisch wiederverwenden oder ob Trägerpreise/Erlösfelder/Rahmen
   je eine eigene, angepasste Form bekommen (die heutige Rahmen-Tabelle in
   `WirtschaftlichkeitParameterDialog.razor` ist bereits eine dritte, wieder andere Bauform ohne
   ±-Knopf). Befund P5 stellt nur die Lücke fest, keine Lösung.
6. **Schritt B — eine Nummer oder zwei?** Das Analysepapier führt Zeitraum **und** Mengenfaktor unter
   einer einzigen Schemaschritt-Kennung „B"; § 2.11.5 des Konzepts trennt „Rahmen" (Zeitraum) und
   „Mengen" (Mengenfaktor) als zwei Zeilen einer Tafel, ohne eine eigene Schemaschritt-Zuordnung zu
   nennen. Ob der Mengenfaktor an `Tab_ProjektWirtschaftlichkeit` (wie der Zeitraum) oder an anderer
   Stelle abgelegt wird, sagt keine der Quellen ausdrücklich.
7. **Risiko (V‑G7) und n-jährliche Zeitpunkte (V‑G3):** Die V-E-Zeile in § 2.11.4 nennt neben der
   Szenarioabdeckung auch „Risiko (V-G7)" und „n-jährliche Zeitpunkte (V-G3)" als Teil von V‑E; die
   Analysepapier-Zeile E9 und die Schemaschritt-Tafel (B/C/D) erwähnen beides nicht. Ob Risiko/V‑G3
   Teil des E9-Umsetzungsauftrags sind oder separat/offen bleiben, ist zwischen den beiden Papieren
   nicht eindeutig aufgelöst.
8. **`SzenarioParameterTests` als Nachweis:** Sowohl Analysepapier- als auch Statuszeile nennen diese
   Testklasse als Nachweis „je Größe" für E9; im Worktree-Code wurde keine Datei dieses Namens gesucht
   (außerhalb des beauftragten Suchraums) — ob sie bereits als Gerüst existiert oder komplett neu
   entsteht, ist offen.
