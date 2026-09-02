# Konzept: Wirtschaftlichkeit EPOS-Plan — konsolidiert

**Stand 02.09.2026** · Codestand `922228a` (Branch `ios_migration`) · `SchemaMigration.ZIEL_VERSION` = 61 · Schemaschritt 62 vergeben (U-1), neue ab 63 · konsolidiert aus drei Quelldokumenten; Mockups und Rechenwege im Ordner `Wirtschaftlichkeit_Kosten/`

Dieses Dokument führt zusammen, was heute auf Formelkarte, Feldkarte, sechs Konzepte und
gut zwanzig Etappenprotokolle verteilt liegt. Es beantwortet die beiden Fragen, die vor der
Umsetzung zu klären sind:

1. **Wie sehen die Dialoge und Felder aus** — für alle Anlagen, für BHKW und Photovoltaik im Detail
   (§ 2).
2. **Wie wird gerechnet** — Investition, Betrieb, Energie, Vergütungen, Reduktionen, Energiesteuer,
   Stromsteuer, Zeile für Zeile mit den Formeln (§ 3).

## Geltung und Abgrenzung

> **Dieses Dokument ändert nichts am Code.** Seit der Konsolidierung vom **02.09.2026** ist es die
> **führende Fassung** des Wirtschaftlichkeitskonzepts: Es führt
> `Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` (Etappenkonzept, letzter Stand 30.08.2026 mit B4),
> `KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md` und `Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`
> zusammen. Die drei Quellen bleiben als Historie und Herleitung stehen; **bei Widerspruch gilt
> dieses Dokument.** Was sie an Stoff enthielten, der hier bislang fehlte, ist mit der Konsolidierung
> nachgezogen: § 2.12 (Kategorien-Mockups), § 3.4 (Vorrangregel), § 3.11 (Emissionsfaktoren und
> CO₂-Preispfad), § 5 (rechtliche Unsicherheiten), § 6.5 (doppelte Wahrheiten).

> **Arbeitsregel (Anwender, 30.08.2026): erst das Konzept, keine Umsetzung.** Sämtliche hier
> beschriebenen Vorhaben — die Erlösrubrik (§ 2.6), die Emissionsspalte (§ 2.5), die Befunde aus
> § 4 — sind **zur Abnahme gedacht und ausdrücklich nicht implementiert**. Wo dieses Dokument
> „Soll" sagt, beschreibt es einen Entwurf, keinen Codestand. Was tatsächlich umgesetzt ist, steht
> in § 6.1.

| Quelle | Was daraus einfließt |
|---|---|
| Formelkarte `rechenwege_formelkarte.md` (30.08.2026, gegen `b2ad3e3`) | § 3 vollständig, § 4 |
| Feldkarte `b5_feldkarte.md` | § 2 vollständig, § 5 |
| `Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md` | Leitentscheidungen BW1–BW10 |
| `KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md` | Datenwelten, Festlegungen L/KL/E/FK |
| `Grundlagen_KWKG_Energiesteuer_Stromsteuer.md` | Rechtsstand, Sätze, Fristen |
| Protokolle B1–B4, BK1, H1–H4b, H21, HB1, W4 E1–E8, K1–K6, KD1–KD6, P1–P6 | § 6 |

## Begleitende Artifacts

Die visuellen Fassungen. Sie sind zum Ansehen und Prüfen gedacht; **maßgeblich ist dieses
Dokument** — bei Abweichung gilt der Text hier.

| Artifact | Inhalt | entspricht |
|---|---|---|
| [**B5-Dialogmockup**](https://claude.ai/code/artifact/e928091e-44ef-41b9-ae31-d6bb7535cf1f) | alle Anlagen, BHKW und PV im Detail, Entscheidungen K1–K11 | § 2 |
| [**Rechenwege der Wirtschaftlichkeit**](https://claude.ai/code/artifact/588f6e21-b9b4-4e4f-b732-56aeca3d5882) | Formelkarte aller Ketten mit Prüfliste | § 3, § 4 |
| [**Erlösrubrik BHKW**](https://claude.ai/code/artifact/d924b2ec-9c41-4e84-a670-88e83bae7ff9) | Entwurf der eigenen Erlösrubrik, zwei Blöcke mit getrennten Summen; die beiden geprüften Befunde zur Stromsteuer-Reduktion und zur Unternehmensart | § 2.6 |
| [**Pflichtpositionen je Komponente**](https://claude.ai/code/artifact/236c8a8a-a2e0-47f4-a099-aa1456de883a) |  Kostendialoge, Bezugsgrößen und die Hilfsenergie-Definition; **Entwurf B ist als § 2.8 übernommen** | § 3.4, Etappe H1 |
| [**ValERI-Bewertung Höfingen**](https://claude.ai/code/artifact/f8968739-df34-4892-9d7d-9c07e6858a4f) | die fünf ValERI-Blöcke in der Wirtschaftlichkeitsseite, reale Höfingen-Zahlen, Cashflow über Nutzungsdauer, Sensitivität mit Steigung €/%, XLSX-Formelblatt nach Anhang A | § 2.9–2.11 |
| [**Dialog, Formel, Zahlenprobe**](https://claude.ai/code/artifact/739d3cca-3b6c-4e2b-af8a-d1a7f73ddc9f) | acht Kostenkategorien, je Dialog-Mockup + Berechnungsgrundlage + durchgerechnete Zahlenprobe an einem Beispielprojekt; Schwerpunkt Vergütungen BHKW (Mengentafel brutto/netto, Mischsatz, Jahresreihe mit Deckel) und PV (AW, Marktprämie, § 51/51a, Kappung); Komponentenkosten BHKW und PV in derselben Dialogform | § 2.12 |

---

# 1 Landkarte

## 1.1 Was mit Kapitalwert wirkt und was nur ausgewiesen wird

Diese Trennung ist die wichtigste des ganzen Felds — sie entscheidet, ob eine Zahl das Ergebnis
verändert oder nur erklärt.

| Wirkt auf den Kapitalwert | Nur Ausweis |
|---|---|
| Investition (brutto → I₀ nach Zuschuss) · Zuschuss | Vermiedene Stromkosten (Arbeit/Leistung/Gesamt) |
| Betriebskosten (mit p_B) | PV: vermiedener Bezug, Kappungs- und Ausfallmengen |
| Energiekosten inkl. Tarif und Aufschläge (mit p_E) | Vollbenutzungsstunden, Satzherkunft |
| CO₂/BEHG | sämtliche Kohärenz- und Warnzeilen |
| Einspeiseerlös (nominal konstant) | B2-Preisbestandteile der Energieträger |
| KWKG-Zuschlag · KWKG-Pauschale § 9 | Strommix-Rückfallhinweis |
| Energiesteuer-Gutschrift | |
| Stromsteuer-Befreiung § 9 Abs. 1 Nr. 3 *(heute Erlös — siehe B-1)* | |
| Stromsteuer-Entlastung § 9b | |
| PV-Vergütungsreihe · Restwert | |

## 1.2 Die vier Datenwelten

```
① Positionswelt      Tab_ProjektWerte ← Tab_Kostenfaktor
   Kategorie 1 = Investition, 2 = Betrieb; je Projekt, Komponente und ANLAGE
   Kostenart · Bemessung · Satz · Menge · Betrag · IstErloes · Nutzungsdauer · StartJahr · IstPflicht

② Vorlagenwelt       Tab_KostenVorlage / Tab_KostenVorlagePosition
   Auslieferung „Standard": Struktur und Bemessung, Sätze bewusst LEER

③ Energieträgerwelt  energy_carrier · energy_price · energy_project_settings
   Preise, Heizwerte, Emissionen, Aufschläge, B2-Preisbestandteile

④ Gesetzeswelt       Tab_Gesetzesparameter
   Schluessel · Klasse · JahrVon · Wert · Einheit · Status · Quelle
   Eine Novelle ist eine neue Jahreszeile, kein Überschreiben
```

Dazu die **Projektrahmen-Zeile** `Tab_ProjektWirtschaftlichkeit` — **eine Zeile je Stammprojekt**,
gültig für die ganze Vergleichsgruppe (Befund R-1), und seit Schema 61 die
**Wirtschaftlichkeitsspalten an `Tab_Energieanlagen`** (KWKG_*, `Energiesteuer_Wahl`,
`Aufteilung_Methode`, `Hilfsenergie_Anteil`).

---

# 2 Der Dialograum

## 2.1 Welche Anlage hat welche Wirtschaftlichkeitsfelder

| Anlage | Investitionsfelder (Gerätewelt) | Betrieb: Hilfsenergie | Eigener Wirtschaftlichkeitsdialog |
|---|---|---|---|
| **BHKW** | `Kosten_Modul` + Montage + Lieferung + Schallschutzhaube + Abgasreinigung (Summe) · `Wartungskosten_kwhel` · Nutzungsdauer | `PROZENT_ENDENERGIEKOSTEN`, 2–4 % | **ja** — `Form_BhkwWirtschaftlichkeit` (neu, § 2.2) |
| **Photovoltaik** | `Modulkosten` je Modul × Modulanzahl | `JAHRESBETRAG`, keine Pflicht | **ja** — `Form_PhotovoltaikVerguetung` (§ 2.3) |
| **Heizkessel** | `Investitionskosten` · `Wartungskosten` mit Einheit (€/a \| €/kWh \| %/a) · Nutzungsdauer | `PROZENT_ENDENERGIEKOSTEN`, 4–8 % | nein |
| **Wärmepumpe** | `Modulkosten` (ganze Anlage) | `PROZENT_ENDENERGIEKOSTEN` | nein |
| **Solarthermie** | `Investitionskosten` je Kollektormodul × Anzahl | `JAHRESBETRAG` | nein |
| **Stromspeicher** | `Modulkosten` [€/kWh] + `Leistungskosten` [€/kW] + `Investition_Fix` + Verschleiß | `JAHRESBETRAG` | teilweise — `Tab_StromspeicherVariante` |
| **Pufferspeicher** | `Investitionskosten` | `JAHRESBETRAG` | nein |

**Nur der Stromspeicher** führt heute eigene anlagenbezogene Wirtschaftlichkeitsparameter
(Kapitalzins 3,0 %/a, Nutzungsdauer 20 a, Leistungspreis, Netzladeanteil, Preisquelle, SoC-Band).
Alle übrigen Anlagen erben den Projektrahmen.

## 2.2 `Form_BhkwWirtschaftlichkeit` — neu (BW9)

Sechs Gruppen an einem Ort, je Anlage pflegbar, mit Live-Vorschau aus **einem** Rechenweg.
Andockung: Knopf in der Fußleiste von `UcWirtschaftlichkeit` und Reiter „Ertrag/Bonus" der
Komponente — dasselbe Formular, Muster PV.

### Gruppe 1 — Anlagen (Tabelle mit Aufklappzeile)

Tabellenkopf je BHKW-Modul, alles nur lesend:

| Spalte | Quelle |
|---|---|
| Anlage | `Tab_Energieanlagen.Bezeichner` |
| P_el [kW] | `Tab_BHKW.Pel` |
| **Brennstoff** | **kein Leseweg vorhanden — Lücke K4**, kleiner Leser `CarrierId` → Name in B5 |
| Stichtag · Inbetriebnahme · Anlagenart | `KWKG_Stichtag` · `KWKG_Inbetriebnahme` · `KWKG_Anlagenart` |

**Aufklappzeile** — 8 Bestandsfelder (heute in `Form_KwkgModule`) plus 3 neue.
Durchgängig gilt: **leer oder 0 heißt „kein eigener Wert → Projektvorgabe".**

| Feld | Typ | Bereich / Optionen | Spalte | Stand |
|---|---|---|---|---|
| Stichtag (Bestellung/Genehmigung) | DateTimePicker mit Haken | Haken aus = Projektwert | `KWKG_Stichtag` | Bestand |
| Inbetriebnahme | DateTimePicker mit Haken | dito | `KWKG_Inbetriebnahme` | Bestand |
| Anlagenart | ComboBox | (nicht erfasst = Neuanlage) · neu § 8 Abs. 1 · modernisiert Abs. 2 · nachgerüstet Abs. 3 | `KWKG_Anlagenart` | Bestand |
| Eigenstrom nach § 6 Abs. 3 | ComboBox | kein Tatbestand · Nr. 1 bis 100 kW · Nr. 2 Kundenanlage · Nr. 3 stromkostenintensiv | `KWKG_Eigenstromfall` | Bestand |
| Satz Einspeisung [ct/kWh] | Numerisch 0–30 | 0 = Projektsatz | `KWKG_Satz_Einspeisung` | Bestand |
| Satz Eigenstrom [ct/kWh] | Numerisch 0–30 | 0 = Projektsatz | `KWKG_Satz_Eigen` | Bestand |
| Vbh-Kontingent [h] | Numerisch 0–200.000 | 0 = Projektwert | `KWKG_Vbh_Kontingent` | Bestand |
| Vbh-Jahresdeckel [h/a] | Numerisch 0–8.760 | 0 = Staffel | `KWKG_Vbh_Jahresdeckel` | Bestand |
| **Energiesteuerentlastung (Anlage)** | ComboBox | (Projektwert) · keine · § 53 · § 53a · § 54 | `Energiesteuer_Wahl` | **neu B5** |
| **Brennstoff auf Strom/Wärme (Anlage)** | ComboBox | (Projektwert) · voller Brennstoff · energetisch | `Aufteilung_Methode` | **neu B5** |
| **Hilfsenergieanteil [%]** | Numerisch 0–100 | 0 = keine; Vorschlag BHKW 2–4 % | `Hilfsenergie_Anteil` | **neu B5** |

Warnzeilen der Gruppe: Ausschreibung § 8a bei P_el > 500 kW · Stromsteuerbefreiung entfällt über
2 MW · Heizöl-Ausschluss ab Inbetriebnahme 2025.

### Gruppe 2 — KWK-Zuschlag, Projektebene

Zieht vollständig aus `Form_WirtschaftlichkeitParameter` um: Bonus Eigenstrom · Bonus Einspeisung ·
Vbh-Deckel-Override · Vbh-Kontingent gesamt · Abschlag Negativstunden [%] · Eigenstrom-Tatbestand ·
Anlagenart · Anteil Neuherstellungskosten [%] · Pauschale § 9 (bis 2 kWel, einmalig) · Stichtag- und
Inbetriebnahme-Vorgabe je Anlage.

Dazu ein **Herleitungslabel je Anlage**:

```
Einspeisung 5,57 ct/kWh — 50 kW × 8,00 + 50 kW × 6,00 + 150 kW × 5,00 + 50 kW × 4,40
Eigenstrom  0,00 ct/kWh — kein Tatbestand nach § 6 Abs. 3
```

Knopf „Vorschlag in die Satzfelder übernehmen" — schreibt **nur auf Knopfdruck**, nie automatisch.

### Gruppe 3 — Energiesteuer

Projektebene: Energiesteuerentlastung (keine · § 53 Formular 1131 · § 53a Abs. 5 Formular 1135 ·
§ 54 Formular 1450) · Brennstoff auf Strom/Wärme · Jahresnutzungsgrad [%] (0 = nicht erfasst,
bleibt Projektgröße — K5).

Herleitungslabel: `§ 53a Abs. 5 · Erdgas 4,42 €/MWh · 2.480 MWh (Ho, Faktor 1,1048) = 10.962 €/a`
Kohärenzzeile in Firebrick, wenn der erfasste Brennstoffpreis die Energiesteuer nicht ausweist.

### Gruppe 4 — Stromsteuer

Unternehmensart (führend, BW4) · Räumlicher Zusammenhang 4,5 km · Hocheffizienz nachgewiesen ·
Sprungknopf „Strombezug…". **Feld „Modus § 9 Abs. 1 Nr. 3" (ERLOES/AUSWEIS) fehlt** — die Spalte
`Stromst_Befreiung_Modus` entsteht erst mit B6 (**Lücke K3**).

### Gruppe 5 — Hilfsstrom

Anteil je Modul (dasselbe Feld wie 1.17, zweitgezeigt) · Mengenkette nur lesend aus B3b:

```
Stromerzeugung brutto → − Hilfsstrom → = Nettostromerzeugung → davon Eigen / Einspeisung
```

Doppelpflege-Warnzeile, wenn Anlagen-Anteil **und** Kostenposition an derselben Anlage gepflegt
sind. **Ein Feld „Deckung je Modul" gibt es nicht** (K1, gestrichen — die Befreiung ist bilanziell).

### Gruppe 6 — Vorschau

Live aus **dem einen** Rechenweg, keine Zweitrechnung: Zuschlag p. a. · Energiesteuer p. a. ·
Stromsteuer p. a. · Einspeiseerlös p. a. · Vermiedene Kosten p. a. (als Ausweis gekennzeichnet) ·
Prüfhinweise.

## 2.3 `Form_PhotovoltaikVerguetung` — Bestand, zugleich Stilmuster

914 × 724, festes Fenster, Kopfband `#0F1F3D` mit CheckBox „Vergütung anwenden", zwei Spalten.

| Gruppe | Felder |
|---|---|
| **Anlage** | Installierte Leistung (rechnerisch, fett) · Override [kWp] (0 = keiner) · Inbetriebnahme · Radio Überschuss-/Volleinspeisung · Warnzeile: > 1 MW Ausschreibung ⇒ AW-Override nötig; > 2 MW Stromsteuer prüfen |
| **Vermarktung** | Radios: Feste Einspeisevergütung (nur ≤ 100 kW) · Direktvermarktung mit Marktprämie · Sonstige DV/PPA · Keine Vergütung (< 200 kW). Felder: DV-Entgelt [ct/kWh] (0,40) · PPA-Festpreis · PPA-Aufschlag auf Spot. Hinweis zu § 21c/§ 21 |
| **Anzulegender Wert** | AW_mix (10 pt fett) · Herleitungszeile · AW-Override (0 = Katalog) · abgeleitete feste EV (AW − 0,40) |
| **§ 51 / § 51a** | Anwenden: Automatisch/Ja/Nein · Statuszeile · iMSys-Einbaujahr (0 = keins) · Ausfallanteil [%] (20,0) · CheckBox § 51a-Kompensation |
| **Bezugsbewertung** | CheckBox „Netzbezug stundenscharf aus Preiszeitreihe bewerten" · Stromsteuerhinweis § 9 |
| **60-%-Begrenzung** | Anwenden: Combo · Statuszeile |
| **Vorschau** | Einspeisung MWh/a · Satz Jahr 1 · Erlös Jahr 1 · Vergütungsausfall · § 51a-Gutschrift · Kennzahlzeile |

Fußleiste: Marktwerte importieren… · Einspeise-Tarif… · Übernehmen · Abbrechen.

## 2.4 `Form_WirtschaftlichkeitParameter` nach dem Auszug

Bleibt: **Allgemein** (Kalkulationszins · Betrachtungszeitraum · Preissteigerung Energie ·
Preissteigerung Betrieb) · **Strom** (Einspeisevergütung PV · Einspeisevergütung KWK · Anzeige-
CheckBox Aufschläge) · **BEHG** (CO₂-Preis, 0 = Pfad · Herkunftszeile · Katalogknopf ·
Referenz-Kraftwerkspark · Referenzkessel) · **Bilanzierung** (Bilanzjahr · Emissionsmethode ·
Biomasse-Konvention · Nachhaltigkeitsnachweis).

Zieht aus: die 11 KWKG-Felder und die 6 Steuerfelder. Danach vier Gruppen statt sechs, plus
Statuszeile und Sprungknopf „BHKW…". Der Dialog war mit 26 Feldern am Kapazitätslimit — das ist der
sachliche Grund für den Auszug.

## 2.5 Preis- und Trägerdialoge

| Block | Inhalt |
|---|---|
| **Strom-Aufschläge** (`ucStromAufschlaege`) | Netzentgelt 6,440 · Umlagen 2,946 · Stromsteuer 2,050 (reduziert 0,050) · Konzession 0,110 · Vertrieb 0,200 ct/kWh; je Aktiv-Schalter, Live-Summe, rot markierter Rest; Schnellwahl seit B4 katalogbasiert, Unternehmensart hebt den passenden Knopf hervor |
| **Brennstoff-Bestandteile** (`ucBrennstoffBestandteile`, B2) | Energiesteuer · CO₂ · Netz-/Messentgelt · Vertrieb, Schnellwahl aus dem Katalog, „In Arbeitspreis übernehmen"; **ohne Preiswirkung** — reine Transparenz und Kohärenzgrundlage |
| **Vergütungssätze** | `Verguetung_PV` 5,0 · `Verguetung_BHKW` 5,0 ct/kWh je Projekt und Träger |

### Emissionsanzeige der Energieträgertabelle (Auftrag 30.08.2026)

**Ist-Zustand.** Die Tabelle „Energieträger des Projekts" auf der Kostenseite führt **drei feste
Emissionsspalten** — CO₂ [g/kWh], SO₂ [mg/kWh], NOx [mg/kWh] (`UcBkKosten.cs:771-772`, aus BK1).
Sie stehen unabhängig davon da, was das Projekt rechnet.

**Soll.** Die Tabelle zeigt **eine** Emissionsspalte, und ihr Kopf wie ihr Inhalt richten sich nach
der bereits vorhandenen Nutzervorgabe:

| `Tab_Projekt.Emission_Berechnungsmodus` | Spaltenkopf | Inhalt |
|---|---|---|
| `CO2` *(Vorbelegung)* | **CO₂ [g/kWh]** | Faktor der Emissionsart CO₂ allein |
| `CO2E` | **CO₂-Äquivalent [g/kWh]** | Summe der gewählten Arten, je mit ihrem Äquivalenzfaktor gewichtet (GWP100) |

**SO₂, NOx und die übrigen Arten entfallen aus dieser Übersicht.** Sie bleiben vollständig
erhalten — im Katalog `emissionsart`/`emissionswert`, im Energieträgerdialog als Detailpflege und
in der Emissionsbilanz. Genommen wird ihnen nur der Platz in einer Tabelle, die den Anwender über
**Kosten** informiert und in der drei Schadstoffspalten mehr verdecken als zeigen.

**Warum das ohne neue Datenhaltung geht.** Die Steuergröße existiert bereits doppelt und aus gutem
Grund: `Tab_Applikation.Emission_Berechnungsmodus` ist die globale Vorgabe für **neue** Projekte,
`Tab_Projekt.Emission_Berechnungsmodus` der Modus, in dem **dieses** Projekt rechnet. Ein Projekt
trägt seine Rechenmethode dauerhaft in sich — es rechnet auch nach Jahren im Modus seiner
Entstehung, gleichgültig wie die Vorgabe inzwischen steht (Hausregel Reproduzierbarkeit). Gelesen
wird der Modus schon heute von `EmissionenCtrl` und `EmissionsAusweis`; die Anzeige muss ihm nur
folgen.

**Der Tooltip trägt die Herleitung — Entscheidung E-1 (30.08.2026).** Im Modus `CO2E` kann derselbe
Zahlenwert auf drei verschiedene Weisen zustande kommen. Die Spalte zeigt **immer den Wert**, der
Tooltip sagt, wie er entstanden ist. **Ein stiller Rückfall auf „CO₂" findet nicht statt** — sonst
wichen Spaltenkopf und Bedeutung voneinander ab.

| Fall | Spalte | Tooltip nennt |
|---|---|---|
| **Regelfall** — mehrere Arten gepflegt | gewichtete Summe | die Aufschlüsselung: „CO₂ 240,0 + CH₄ 0,50 × 28 + N₂O 0,010 × 265 = 256,7 g/kWh (GWP100)" |
| **Nur CO₂ gepflegt** | = CO₂-Faktor | „Für diesen Energieträger ist außer CO₂ keine weitere Emissionsart hinterlegt — der Äquivalentwert entspricht deshalb dem CO₂-Faktor." |
| **Wert ist bereits ein Äquivalent** (`ist_co2e`, Konzept-Emissionsarten F3) | unverändert übernommen | „Der hinterlegte Wert ist bereits ein CO₂-Äquivalent (Quelle: …) und wird nicht aufsummiert." |

Der dritte Fall ist der heikelste: Ohne Hinweis liest sich die fehlende Aufsummierung wie ein
Fehler. Im Modus `CO2` entfällt der Tooltip bis auf die Quellenangabe — dort gibt es nichts
herzuleiten.

## 2.6 Eigene Rubrik „Erlöse und Vorteile" (Auftrag 30.08.2026)

Die Erlösseite bekommt eine **eigene Rubrik** — im Ergebnisreiter, im BHKW-Dialog als Vorschau und
im Bericht. Sie ist in **zwei Blöcke** geteilt, und diese Teilung ist keine Kosmetik: Block B darf
nicht addiert werden.

### Block A — zahlungswirksam (geht in den Kapitalwert)

| # | Position | Rechtsgrundlage | Menge × Satz | Laufzeitbegrenzung |
|---|---|---|---|---|
| A1 | **KWK-Bonus Einspeisung** | § 7 Abs. 1 KWKG | eingespeister KWK-Strom × Mischsatz (marginale Staffel) | Vbh-Kontingent § 8 · Jahresdeckel · Stichtag 31.12.2026 |
| A2 | **KWK-Bonus Eigenstrom** | § 7 Abs. 2 KWKG | eigengenutzter KWK-Strom × Mischsatz | zusätzlich: **Tatbestand § 6 Abs. 3 zwingend** |
| A3 | KWKG-Pauschale (≤ 2 kWel) | § 9 KWKG | 0,04 €/kWh × 60.000 Vbh × P_el | einmalig, schließt A1/A2 aus |
| A4 | **Energiesteuer BHKW-Brennstoff** | § 53 **oder** § 53a Abs. 5 EnergieStG | Brennstoffmenge × Satz in gesetzlicher Einheit | dauerhaft, jährlicher Antrag |
| A5 | Energiesteuer Kesselbrennstoff | § 54 EnergieStG | Heizstoffmenge × Teilsatz − 250 €/a | **nur produzierendes Gewerbe** |
| A6 | Stromsteuer-Entlastung Netzbezug | § 9b StromStG | Netzbezug × 20,00 €/MWh − 250 €/a | **nur produzierendes Gewerbe** |
| A7 | Stromsteuer-Befreiung Eigenverbrauch | § 9 Abs. 1 Nr. 3 StromStG | KWK-Eigenverbrauch × 20,50 €/MWh | ≤ 2 MW · hocheffizient · 4,5 km · CO₂ < 270 g/kWh — **wandert nach BF1 in Block B** |
| A8 | Einspeiseerlös Strom | Tarif bzw. Projektwert | Einspeisemenge × Preis | nominal konstant |
| A9 | PV-Vergütung | EEG | eigene Reihe (`PvErloesRechner`) | 20 Jahre + Inbetriebnahmemonate |
| A10 | Restwert | DIN EN 17463 | Betrag × Restdauer / n | Ende des Betrachtungszeitraums |

### Block B — Ausweis, **nicht addieren**

| # | Position | Warum kein Zahlungsstrom |
|---|---|---|
| B1 | **Vermiedene Stromkosten** — Arbeit · Leistung · Summe | Die Einsparung steckt bereits in der kleineren Bezugsrechnung; in den Kapitalwert geht der **Reststrom**betrag. Zusätzliches Buchen wäre Doppelzählung (E5). Der Leistungsanteil ist regelmäßig **negativ** |
| B2 | PV: vermiedener Bezug, Kappungs- und Ausfallmengen | dito bzw. Mengenausweis |

Die Rubrik kennzeichnet Block B sichtbar, etwa mit dem Vermerk `[Ausweis]` je Zeile und einer
Summenzeile, die **nur Block A** summiert.

### Zwei fachliche Klarstellungen zum Auftrag

**(1) Vermiedene Stromkosten und die Stromsteuer-Reduktion — der Punkt trifft eine echte Lücke.**
Die Differenzmethode rechnet beide Seiten mit demselben Arbeitspreis, und der enthält die
Stromsteuer mit **20,50 €/MWh**. Ein Unternehmen des produzierenden Gewerbes bekommt davon nach
§ 9b **20,00 €/MWh** zurück — die tatsächlich vermiedene Stromsteuer beträgt also nur
**0,50 €/MWh**, nicht 20,50.

Im **Kapitalwert** ist das heute richtig erfasst, weil die § 9b-Reihe auf den kleineren Netzbezug
rechnet und damit automatisch kleiner ausfällt. Im **Ausweis** fehlt es: Die vermiedenen Kosten
erscheinen um **2,00 ct/kWh zu hoch**.

```
Vermieden_effektiv = Vermieden_brutto − Entlastungssatz(§ 9b) × vermiedene Menge
                     (nur bei produzierendem Gewerbe / Land- und Forstwirtschaft)
```

Vorschlag: Die Rubrik zeigt beide Zeilen — „vermiedene Kosten brutto" und darunter „abzüglich
entgangener § 9b-Entlastung", mit dem effektiven Betrag als Ergebnis. So bleibt nachvollziehbar,
warum der Vorteil kleiner ist als der Bezugspreis vermuten lässt.

**(2) Die Energiesteuer des BHKW-Brennstoffs hängt _nicht_ an der Unternehmensart.** Geprüft am
Gesetzestext und am Code:

| Vorschrift | betrifft | produzierendes Gewerbe nötig? |
|---|---|---|
| § 53 EnergieStG (Stromerzeugung) | BHKW-Brennstoff | **nein** |
| § 53a Abs. 5 (Gasturbinen und Verbrennungsmotoren) | BHKW-Brennstoff | **nein** — der Absatz differenziert nicht nach Unternehmensart |
| § 53a Abs. 3 | „von einem Unternehmen des Produzierenden Gewerbes … **verheizt**" — also die Kesselseite, nicht die Motorverstromung | ja, aber **nicht umgesetzt** und in den Grundlagen als ungeklärt geführt |
| § 54 EnergieStG | Heizstoffe (Kessel, Spitzenlast) | **ja** |
| § 9b StromStG | Netzbezug Strom | **ja** |

Im Code prüft `ProduzierendesGewerbe` genau zwei Stellen: § 54 und § 9b. Für A4 ist die
Unternehmensart also ohne Wirkung — sie wirkt auf **A5 und A6**, und über den Preisanteil auf
**B1**. Die Rubrik sollte das je Zeile anzeigen, damit niemand eine Reduktion an der falschen
Stelle erwartet.

## 2.7 Hausstil (verbindlich für neue Dialoge)

Kopfband `#0F1F3D`, Titel weiß Segoe UI 12 bold · Vorschau- und Kennzahlstreifen `#1A3261` ·
Warnung amber `#C88A00` auf `#FFF6E0` · Fehlerzeile Firebrick `#B22222` · Hinweise DimGray ·
Segoe UI 9 pt, Gruppentitel fett, Eckenradius 6 · Fußknöpfe 110 × 30 · `InfoKnopf` 28 × 28 ·
`SpeichernLeiste` (nicht schließender Speichern-Knopf mit Statuszeile) · Designer-basiert
(FK1/Ä6), Texte über `MyResource` mit GetString-Rückfall.

**Die Fußleiste von `UcWirtschaftlichkeit` ist voll** — sieben Knöpfe, ein achter läge bei
x = −50 (**Lücke K8**).

## 2.8 Betriebskosten-Raster der Kostenverwaltung — Entwurf B (übernommen 31.08.2026)

*Aus dem Artifact [Pflichtpositionen je Komponente](https://claude.ai/code/artifact/236c8a8a-a2e0-47f4-a099-aa1456de883a),
Entwurf B, auf Anwenderentscheid in dieses Konzept übernommen. Es ist die Spezifikation des offenen
Punkts „Live-Frisch-Anzeige der Bezugsgröße samt Herleitungszeile im Kostendialog"
(§ 6.3 Nr. 2, H21-Grenze 1).*

Das Raster der Kostenverwaltung (`Form_KostenKomponente`, Betriebskosten-Seite) ändert sich
gegenüber dem heutigen Stand in **vier Punkten**:

**1. Pflichtzeilen stehen oben, hinterlegt, mit Schloss statt Papierkorb.**
Die Zeilen mit `IstPflicht` (Wartung, Instandhaltung der eigenen Komponente, Hilfsenergie) tragen
in der Aktionsspalte ein Schloss-Symbol; der Löschversuch führt zum Dialog „Satz auf 0 setzen"
(Löschsperre aus H3 — hier ihr Anzeige-Teil). Unter der Positionsbezeichnung steht der Vermerk
„Pflicht nach VDI 2067" bzw. der Empfehlungsbereich („Pflicht · üblich 3,0–9,0 %").

**2. Unter dem Satz steht die Herleitung im Klartext** — Menge **und** Quelle, anlagenscharf:

| Bemessung | Herleitungszeile (Beispiel) |
|---|---|
| je kWh elektrisch | `× 60.000 kWh el · BHKW 1` |
| % der Investition | `× 48.000,00 € · Investition BHKW 1` |
| % der Investition (Rückfall) | `× 66.500,00 € · Investition gesamt` — die Rückfallstufe wird benannt, nie verschwiegen |
| % der Endenergiekosten | `× 14.760,00 € Endenergiekosten · BHKW 1 → 1.200 kWh Strom` — die rückgerechnete Strommenge wird als solche ausgewiesen |

**3. Der Betrag rechnet sofort — aus dem einen Rechenweg, mit frischer Menge.**
Anzeige und Rechnung laufen über `BetriebskostenCtrl.Betrag` mit der Menge aus dem
`EndenergieAufloeser` (Vorrang „frisch vor Konserve", H21). Der Dialog zeigt damit **nicht mehr den
Konservenstand** aus `Tab_ProjektWerte.Menge`, sondern denselben Wert, den der nächste
Wirtschaftlichkeitslauf ansetzen wird. Unter dem Betrag steht „berechnet"; absolute Positionen
zeigen stattdessen das Kettensymbol am gesperrten Satzfeld.

**4. Der Fuß summiert je Anlage** — netto führend, brutto nachrichtlich (Umsatzsteuersatz aus dem
Gesetzeskatalog, KL5: Brutto ist reine Anzeige):

```
Betriebskosten BHKW 1        brutto 8.358,08 €/a        7.023,60
```

**Rahmen der Seite:**

- **Banner** oben: „Alle Beträge und Bezugsgrößen sind NETTO. Eine gepflegte Satzangabe hat
  Vorrang — das Betragsfeld wird dann gesperrt, aber nicht geleert. **Mengen stammen aus dem
  Simulationslauf vom ‹Datum, Uhrzeit›.**" — der Laufzeitpunkt ist sichtbar, damit eine gealterte
  Bezugsgröße erkennbar ist.
- **Ohne Simulationslauf** zeigen mengenbasierte Zeilen einen **Strich statt einer 0** samt
  Warnzeile („Stromproduktion unbekannt — Simulation noch nicht gelaufen"); investitionsbasierte
  Sätze rechnen sofort. Fußhinweis: „n von m Pflichtpositionen rechnen noch nicht — Simulation
  ausführen." (Entwurf E im selben Artifact.)
- **Worst/Best** bleibt je Zeile über den ±-Knopf (`Form_CaseEingabe`), Speichern über die
  nicht schließende `SpeichernLeiste` mit Statuszeile („✓ Gespeichert 14:18").
- Fußknöpfe: „Aus Vorlage übernehmen…" · „+ Position hinzufügen" · „Speichern".

**Einordnung und Grenzen:** Nur Konzept, keine Umsetzung (Arbeitsregel). Der Klick auf einen
Betrag öffnet die vollständige Herleitung (Entwurf C im Artifact); die Mengenherkunft folgt den
Rechenwegen aus § 3.4 — insbesondere gilt Befund **B-1** (Kessel-Endenergie strukturell 0) bis zu
seiner Behebung auch hier: Die Herleitungszeile würde am Kessel „× 0 kWh" zeigen und macht den
Befund damit erstmals sichtbar statt ihn zu verstecken.

## 2.9 Wählbares Vergleichsprojekt — die Referenz der Differenzrechnung (Anforderung 31.08.2026)

**Anforderung des Anwenders:** Die Differenzrechnung soll nicht fest gegen das Stammprojekt laufen —
**die Referenz (das Vergleichsprojekt) soll wählbar sein.**

**Ist-Zustand — die Referenz ist hart verdrahtet.** Das Stammprojekt ist überall die
Unterlassensalternative: `Kapitalwertdifferenz = KW(Variante) − KW(Stamm)`; Annuität, dynamische
Amortisation und IZF rechnen ausschließlich auf dieser Differenz; in `UcWirtschaftlichkeit` ist der
Stamm als Referenz **nicht abwählbar**, und die Nachweiszeile sagt fest „Referenz: Stammprojekt".
Eine Auswahl existiert nirgends.

**Warum die Anforderung fachlich richtig ist:** Die Altanwendung (Höfingen-Mappe,
`Tab_kurz_KWKG2020`) rechnet durchgehend gegen eine ausdrücklich benannte **Vergleichsheizung** —
Investition 9.624 €, Betriebskosten 518 €/a, Brennstoff 11.498 €/a sind dort eigene Größen der
Referenz, und jede Erlöszeile („Einnahmen Wärme = Betriebskosten des Vergleichssystems") ist eine
Differenz dagegen. Auch DIN EN 17463 verlangt den Vergleich gegen die **Unterlassensalternative** —
und welche Alternative das ist, ist eine fachliche Entscheidung je Bewertung, keine Strukturvorgabe
der Software. Wer zwei Ausbauvarianten gegeneinander stellen will (statt jede gegen den Stamm),
braucht die freie Wahl.

**Soll:**

| Aspekt | Festlegung |
|---|---|
| Auswahl | je Vergleichsgruppe **ein** Referenzprojekt: Stamm **oder** eine beliebige Variante der Gruppe |
| Vorgabe | **Stamm** — damit ist die Umstellung für jede Bestandsrechnung ergebnisneutral |
| Persistenz | `Tab_ProjektWirtschaftlichkeit.ID_Referenzprojekt` (LONG, nullable; NULL = Stamm). Die Spalte gehört an die Rahmenzeile, weil die Referenz wie Zins und Zeitraum **je Gruppe** gilt (R-1) — und wie bei jeder Spalte dieser Tabelle an **beide** DDL-Orte (SchemaMigration **und** `StelleTabellenSicher`) |
| Rechenwirkung | alle Differenzkennzahlen (KW-Differenz, Annuität, Amortisation, IZF, Sensitivität der Differenz) rechnen gegen die gewählte Referenz; die Referenz selbst erhält **keine** Differenzkennzahlen |
| Anzeige | Referenzspalte im Kennzahlengrid markiert; ListView: die **gewählte** Referenz ist nicht abwählbar (heute: der Stamm); Nachweiszeile nennt die Referenz beim Namen („Referenz: ‹Variantenname›") statt der festen Formel |
| Bericht | Word und Excel übernehmen die Benennung aus derselben Zeilendefinition (`WirtschaftlichkeitZeilen`) — keine dritte Wahrheit |
| ValERI-Sicht | die gewählte Referenz **ist** die Unterlassensalternative im Sinne der Norm; der ValERI-Bewertungsbericht weist sie als solche aus |

**Randfälle, ausdrücklich geregelt:**

- Gewählte Referenz **ohne Simulationsergebnis** → der Sammler rechnet sie nach (Bestandsverhalten
  für jede Variante); scheitert das, Abbruch mit Fehlgrund — nie stiller Rückfall auf den Stamm.
- Gewählte Referenz **gelöscht** oder nicht mehr in der Gruppe → Rückfall auf Stamm **mit
  Warnzeile**, die den Rückfall benennt; die Spalte wird nicht still bereinigt.
- Referenz = Stamm (Vorgabe) → Verhalten byte-gleich zum Bestand; das ist das Abnahmekriterium der
  Etappe.

**Einordnung:** Nur Konzept (Arbeitsregel). Umsetzung als eigene, kleine Etappe — ergebnisneutral
in der Vorgabe, erste Rechenwirkung erst bei ausdrücklicher Wahl einer anderen Referenz. Sie gehört
**vor** die ValERI-Berichtsetappe, weil deren Bewertungsbericht die Unterlassensalternative benennen
muss.

## 2.10 Integrationsort der ValERI-Darstellung (Anwendervorgabe 31.08.2026)

**Die ValERI-Blöcke werden Bestandteil der Seite „Berichte && Kosten → Wirtschaftlichkeit"
(`UcWirtschaftlichkeit`) — kein separater Dialog.** Die Seite trägt bereits heute den Titel
„Wirtschaftlichkeit — Kapitalwertmethode (DIN EN 17463)" und die passende Grundausstattung:
Vergleichsgruppen-Liste (mit dem Vermerk „Referenz: Stamm, fest gewählt" — der Ist-Zustand aus
§ 2.9), Szenariowahl, vier Kennzahl-Kacheln, Kennzahlengrid (Zeilen × Projekte), Parameternachweis
und Fußknöpfe.

**Andockvorschlag** (Einzelheiten in den ValERI-Mockups):

| Element | Ort auf der Seite |
|---|---|
| Referenzwahl (§ 2.9) | in der Vergleichsgruppen-Liste — die Beschriftung „fest gewählt" wird zur Auswahl; die gewählte Referenz ist nicht abwählbar |
| Die fünf ValERI-Blöcke (Investition · Betrieb · Erlöse · Energie · Wirtschaftlichkeit über Nutzungsdauer) | unterhalb des Kennzahlengrids als auf-/zuklappbare Abschnitte **oder** als zweite Ansicht der Seite (Umschalter „Kennzahlen / ValERI-Bewertung") — Entscheidung am Mockup |
| Kumulierter diskontierter Cashflow | inline in den Block „Wirtschaftlichkeit über Nutzungsdauer"; der vorhandene Verlauf-Dialog bleibt als Vollbild-Absprung |
| ValERI-Bewertungsbericht (Anhang E der Norm) | als Baustein der **Bericht**-Seite (Word/Excel), gespeist aus derselben Zeilendefinition |

Damit bleibt die Regel „eine Wahrheit je Größe": Die ValERI-Ansicht **rendert** die vorhandenen
Ergebnisse (`WirtschaftlichkeitErgebnis`, `WirtschaftlichkeitZeilen`, Verlaufsreihe) — sie rechnet
nichts Eigenes.

## 2.11 ValERI (DIN EN 17463) — Integration und Darstellung (Auftrag 31.08.2026)

*Quellen: der Normtext DIN EN 17463:2021-12 (vom Anwender bereitgestellt, vollständig ausgewertet —
alle Anforderungen hier paraphrasiert, Abschnittsnummern der Norm in Klammern) und als reales
Zahlenbeispiel die Altmappe `BHKWPlan\BHKW_Höfingen_Erneuerung_20kWel.XLS`, Blatt
`Tab_kurz_KWKG2020`. Mockups: siehe Artifact-Tabelle am Dokumentanfang.*

### 2.11.1 Die Kernaussage

**EPOS-Plan rechnet bereits nach dieser Norm** — der `KapitalwertRechner` trägt sie im Namen, und
NPV, drei Szenarien, Sensitivität und dynamische Amortisation existieren. Die Integration ist
deshalb **keine Rechenreform, sondern eine Vervollständigungs- und Darstellungsaufgabe**: Die Norm
verlangt vor allem Dinge *um* die Rechnung herum — Deklarationen, ein Berichtsformat, konsistente
Szenarien, und die richtige Gewichtung der Kennzahlen.

Vier Normaussagen tragen alles Weitere:

1. **Der Kapitalwert ist das einzige Entscheidungskriterium** (Anhang C). IZF und dynamische
   Amortisation sind dort ausdrücklich als Entscheidungsgrundlage verworfen — der IZF wegen
   Mehrdeutigkeit bei mehr als einem Vorzeichenwechsel, die Amortisation, weil sie alles nach dem
   Break-Even ausblendet. Jeder **NPV > 0 gilt als vorteilhaft** (8.1.2); ein negativer Worst-Case
   ist **kein Ausschlusskriterium**, sondern beziffert das Risiko (8.1.3).
2. **Nur Nominalrechnung** (6.3.2, Anhang G) — Realwerte sind unzulässig, weil sich im Zins nur
   eine Inflationsrate unterbringen lässt, das Modell aber mehrere Preisraten braucht. EPOS rechnet
   nominal: ✓.
3. **Der Vergleich läuft gegen eine Alternative** — der NPV ist Wertbeitrag *gegenüber* der
   Unterlassensalternative (8.1.2). Das ist die gewählte Referenz aus § 2.9.
4. **Der Bericht ist Pflichtteil des Verfahrens** (Abschnitt 9), inklusive einer harten
   Muss-Anforderung: Aushändigung als **editierbare Tabellenkalkulationsdatei** mit sichtbarer
   Rechenlogik (Raster nach Anhang A). Ein Werte-Export genügt nicht.

### 2.11.2 Abgleich Norm ↔ EPOS-Plan (Gap-Tabelle V-G)

| # | Normanforderung | EPOS heute | Lücke / Behandlung |
|---|---|---|---|
| V-G1 | ≥ 2 differenzierte **Preisschwankungsraten**, nominal (6.3.2) | p_E und p_B vorhanden, nominal ✓ | dem Grunde nach erfüllt; keine Raten je Träger/Position (bekannt, R-2) — Deklaration genügt, Ausbau optional |
| V-G2 | **Degradation** je Position [%/a] **mit Quellenangabe** (6.3.1/6.3.3) | fehlt vollständig | neue optionale Positionsattribute; Vorgabe 0 %/a = ergebnisneutral |
| V-G3 | **Zeitpunktattribut** je Cashflow: Periode 0 · jährlich · alle n Jahre · einmalig in k (6.3.1) | teilweise (StartJahr, Ersatz über Nutzungsdauer) | „alle n Jahre" fehlt (z. B. Dichtheitsprüfung alle 2 a) — kleiner Ausbau der Bemessung |
| V-G4 | **Kein Restwertverfahren** — Endzahlungen (Demontage, Veräußerung) gehören als explizite Cashflows in die Endperiode (6.4) | Restwert linear | **dokumentierte Abweichung**: Restwert bleibt als Schätzer des Veräußerungswerts, wird aber im Bericht als Modellannahme deklariert; Endzahlungs-Positionen sind über StartJahr bereits abbildbar |
| V-G5 | **Szenarien = gleichzeitige Variation aller Einstellparameter** — auch r, T, Preisraten, Mengen (7.3) | Best/Worst variieren nur die Kosten-/Betragsspalten; r, T, p sind je Szenario fix | **entschieden 31.08.2026: vollständige Abdeckung** — alle Parameter (Investition, Energiekosten, Betriebskosten, Erlöse, Rahmen, Mengen) erhalten Best/Worst-Werte; Modell in § 2.11.5 |
| V-G6 | **Sensitivität**: die 7 Regelparameter, Ausweis mit **Steigung €/%** und Liniendiagramm (7.2, 8.1.3) | 5 Fälle vorhanden (Zins, p_E, Invest, Energie, KWKG-Wegfall) | fehlt: T-Variation, Endzahlungen; Ausgabeformat um Steigungsspalte + Diagramm ergänzen |
| V-G7 | **Risiko**: Zinszuschlag **oder** Abzug `R_loss × p_loss` auf die Periodennettosumme, nur t > 0 (6.5, Anhang F) | fehlt | optionales Risikomodul; Anhang F bevorzugt den Zahlungsstromabzug; Vorgabe aus |
| V-G8 | **IZF/Amortisation nur nachrichtlich** (Anhang C) | Kacheln zeigen beide gleichrangig neben dem Kapitalwert | Kacheln behalten, aber als „nachrichtlich (Anhang C)" gekennzeichnet; **IZF-Mehrdeutigkeitswarnung** bei > 1 Vorzeichenwechsel der Differenzreihe — bei EPOS-Projekten durch Ersatzjahre und KWKG-Auslauf der Regelfall, nicht die Ausnahme |
| V-G9 | **Steuerdeklaration Pflicht**: „Steuern berücksichtigt: ja/nein"; Abschreibungen nie als Cashflow, nur als Steuerschild (7.1.2) | Steuer-**Gutschriften** ja (Energie-/Stromsteuer), **Ertragsteuern** nein; keine AfA ✓ | zweiteilige Deklarationszeile: „Energie-/Stromsteuerentlastungen: berücksichtigt · Ertragsteuern: nicht berücksichtigt" |
| V-G10 | **Bericht** mit Pflichtinhalten a)–d) + **editierbarer XLSX mit Formeln** nach Anhang-A-Raster (9) | Excel-Export existiert (ClosedXML), aber als **Werte** | **größte Einzellücke mit hartem Muss**: ein ValERI-Blatt mit Parameterblock (absolute Bezüge), Periodenspalten, Periodenschalter, Gesamt-/Barwert-/NPV-Zeile — je Szenario eines |
| V-G11 | **Nicht monetarisierbare Wirkungen**: erfassen, kategorisieren (Energiefluss / finanziell / sonstig), beurteilen nach Dauer × Wirkung auf Organisation/Mitarbeiter/Umwelt (6.1, 8.2) | fehlt | neue kleine Datenklasse je Projekt (Freitext + Kategorie + Beurteilung); fließt nie in den NPV, immer in den Bericht |
| V-G12 | **Anhang-E-Checkliste** (15 Punkte, Note 1–5) | fehlt | als Abschlussseite des Berichts; zugleich interne Abnahmecheckliste der Etappe |

**Anhang D der Norm ist eine BHKW-Fallstudie** (90 kW_th, 18 Jahre, NPV 64.480 €, Worst −202.802 €,
Best +598.320 €) — sie dient der Etappe als **externe Gegenprobe**: EPOS muss mit denselben
Eingaben dieselben Zahlen treffen. *(Vorsicht: Zwei Zeilen der Sensitivitätstabelle D.6 tragen im
Normtext versehentlich Werte des Pumpenbeispiels — als Prüfreferenz ungeeignet, dokumentiert.)*

### 2.11.3 Die fünf Darstellungsblöcke

Andockung nach § 2.10 in `UcWirtschaftlichkeit`, Referenz nach § 2.9. Alle Blöcke rendern
vorhandene Größen; Beispielzahlen in den Mockups aus der Höfingen-Mappe (20-kW-BHKW-Erneuerung
gegen benannte Vergleichsheizung — Kapitalwert 65.259 €, IZF 20,4 %, Amortisation 4,33 a).

| Block | Inhalt | Normbezug |
|---|---|---|
| **Investitionskosten** | Anlage · Referenz · Differenz; Zuschusszeile; je Szenario | 6.1, 7.3 |
| **Betriebskosten** | Positionsliste mit den Normattributen Zeitpunkt · Preisrate · Degradation · Quelle | 6.3.1 |
| **Erlöse** | die Rubrik aus § 2.6 — in der Differenzsicht sind vermiedene Bezüge reguläre Differenz-Cashflows (die Referenzkosten laufen als Gegenposition), Block-B-Kennzeichnung bleibt für die Absolutsicht | 6.1 |
| **Energiekosten** | je Träger, Anlage gegen Referenz, BEHG mit Preispfad, Preisraten-Ausweis | 6.3.2 |
| **Wirtschaftlichkeit über Nutzungsdauer** | kumulierter diskontierter Cashflow (drei Szenarien), NPV-Regel, Kennzahlen mit „nachrichtlich"-Kennzeichnung, Sensitivitätstafel mit Steigung €/%, Deklarationszeilen (nominal · Steuern · Restwert · Risiko) | 7, 8, Anhang A |

### 2.11.4 Etappen und Entscheidungen

| Etappe | Inhalt | Wirkung |
|---|---|---|
| **V-A** | Ausweis: „nachrichtlich"-Kennzeichnung der Kacheln, IZF-Mehrdeutigkeitswarnung, Deklarationszeilen, Steigungsspalte der Sensitivität | keine |
| **V-B** | Referenzwahl (§ 2.9) | keine in der Vorgabe |
| **V-C** | ValERI-Ansicht (fünf Blöcke + Cashflow-Chart) in `UcWirtschaftlichkeit` | Ausweis |
| **V-D** | XLSX-Formelbericht nach Anhang-A-Raster + Berichtsinhalte a)–d) + Anhang-E-Checkliste; **Gegenprobe an der Anhang-D-Fallstudie** | Ausgabe |
| **V-E** | Vollständige Szenarioabdeckung nach § 2.11.5 (V-G5, Umfang entschieden 31.08.2026), Risiko (V-G7), Degradation (V-G2), n-jährliche Zeitpunkte (V-G3) | **ja** — je Pflege, mit A/B-Nachweis; NULL = wie Erwartet hält die Etappe bis zur ersten Pflege ergebnisneutral |

| Nr. | Entscheidungsfrage | Empfehlung |
|---|---|---|
| **V-1** | Fünf Blöcke als Aufklappabschnitte unter dem Grid oder als zweite Ansicht mit Umschalter „Kennzahlen / ValERI-Bewertung"? | Umschalter — die Seite ist schon voll |
| **V-2** | XLSX-Formelexport: nur das ValERI-Blatt oder den ganzen Bericht formelbasiert? | nur das ValERI-Blatt (drei Szenariotabellen + Parameterblock); der übrige Bericht bleibt Werte |
| **V-3** | IZF/Amortisation von den Kacheln nehmen oder mit „nachrichtlich"-Label behalten? | behalten mit Label — Anwender kennen die Größen, die Norm verlangt nur die richtige Einordnung |
| **V-4** | Szenario-Parametersätze (V-G5) sofort oder nach V-A–V-D? | **Umfang entschieden** (§ 2.11.5), Zeitpunkt offen — Empfehlung: danach, einzige Etappe mit Rechenwirkung, eigener A/B-Nachweis |

### 2.11.5 Vollständige Szenarioabdeckung (Entscheidung 31.08.2026)

**Anwenderentscheid:** Alle Parameter — Investitionskosten, Energiekosten, Betriebskosten,
Erlöse, dazu Rahmen und Mengen — werden mit Best- und Worst-Case-Werten versehen.

**Das Prinzip ist eine Fortschreibung, kein Neubau:** Die Kostenpositionen haben das Muster schon —
`BestCase`/`WorstCase` je Zeile, VALERI-Vorrang (0/NULL = wie Erwartet), Pflege über
`Form_CaseEingabe` (±-Knopf). **Dasselbe Muster wandert an jeden Ort, an dem ein Erwartet-Wert
gepflegt wird.** Der Anwender trägt je Parameter die beiden Werte selbst ein (wie die Norm es in
ihren Szenariotabellen vormacht — je Parameter ein realistischer Extremwert je Richtung); die
Software wendet im Szenario Best **alle** Best-Werte gleichzeitig an, im Worst alle Worst-Werte.
Eine automatische „Richtungslogik" gibt es nicht und braucht es nicht.

| Parameterklasse | Ort des Erwartet-Werts | Best/Worst | Stand |
|---|---|---|---|
| **Investitionskosten** je Position | `Tab_ProjektWerte` Kat. 1 | `BestCase`/`WorstCase` (+ Nutzungsdauern) | **vorhanden** (E3/H4b, Kaskadenwirkung gemessen) |
| **Betriebskosten** je Position | `Tab_ProjektWerte` Kat. 2 | dito | **vorhanden** |
| **Energiepreise** je Träger | `energy_project_settings.custom_price_work` (+ Grundpreis) | neue Spalten `custom_price_work_best/_worst` (Grund-/Leistungspreis analog, nullable) | **neu** |
| **Erlössätze** (Marktgrößen) | `Einspeiseverguetung`, `Einspeiseverguetung_KWK`, PPA-/DV-Preise des PV-Dialogs | je Feld ein Best/Worst-Paar an derselben Tabelle | **neu** |
| **Rahmen** | `Tab_ProjektWirtschaftlichkeit`: `Zinssatz`, `Betrachtungszeitraum`, `Preissteigerung_Energie`, `Preissteigerung_Betrieb` | je Größe `_Best`/`_Worst` (8 Spalten), NULL/0 = wie Erwartet | **neu** |
| **Mengen** (Simulationsergebnis) | Stromerzeugung, Wärme, Einspeisung … | **ein Mengenfaktor [%] je Szenario** an der Rahmenzeile (wirkt multiplikativ auf die Energie- und Erlösmengen) — die Simulation selbst wird nicht dreifach gerechnet | **neu** |

**Ausdrücklich nicht szenariert werden gesetzliche Sätze** — KWKG-Zuschläge, Energie- und
Stromsteuersätze, BEHG-Festpreise sind Rechtsgrößen, keine Unsicherheitsparameter; ihre Zukunft
bildet der Katalogpfad ab (Status GESICHERT/PROGNOSE), nicht ein Worst-Case. Unsicher sind Mengen,
Preise, Laufzeit und Zins — genau die stehen oben.

**Regeln (aus dem Bestand fortgeschrieben):**

- **NULL/0 heißt „wie Erwartet"** — jede neue Spalte ist damit ergebnisneutral, bis sie gepflegt
  wird. Das ist zugleich das Abnahmekriterium der Etappe.
- Der **VALERI-Vorrang** gilt unverändert: Ein gepflegter Szenariowert verdrängt die Ableitung
  (`szenarioGepflegt ⇔ |Wert − Erwartet| > 1e−9`), an allen Lesestellen identisch.
- Die **Sensitivität** (7.2) bleibt davon getrennt: Sie variiert einzeln ceteris paribus; die
  Szenarien variieren alles gleichzeitig (7.3). Beide nutzen dieselben Erwartet-Werte als Basis.
- **Pflege**: der vorhandene ±-Knopf (`Form_CaseEingabe`) als einheitliches Muster auch an
  Trägerpreisen, Erlösfeldern und der Rahmen-Gruppe; die ValERI-Ansicht zeigt je Szenario, welche
  Parameter gepflegte Abweichungen tragen („12 von 31 Parametern szenariert").
- **Ausweis im Bericht** (Norm 9c): Die Kalkulationstabelle je Szenario nennt die
  Parametereinstellungen vollständig — die Szenariospalten der Rahmenzeile erscheinen im
  Parameterblock des XLSX-Blatts.

## 2.12 Kategorien-Mockups mit Rechenweg (Auftrag 02.09.2026)

*Ausgelagert in den Ordner [`Wirtschaftlichkeit_Kosten/`](Wirtschaftlichkeit_Kosten/LIESMICH.md):
`Beispielprojekt.md` (die eine Zahlenquelle), `Mockups/Dialog_Formel_Zahlenprobe.html` (alle acht
Kategorien als Seite, zugleich Artifact
[Dialog, Formel, Zahlenprobe](https://claude.ai/code/artifact/739d3cca-3b6c-4e2b-af8a-d1a7f73ddc9f))
und `Rechenweg/01…08` — je Kategorie Dialog → Berechnungsgrundlage → Berechnungserläuterung →
Befunde. Der Auftrag: je Kostenkategorie ein Mockup mit Berechnungsgrundlage und
Berechnungserläuterung, Schwerpunkt Vergütungen BHKW und PV. Dieser Abschnitt ist die Kurzfassung;
bei Abweichung gilt der Ordner für die Zahlen und dieses Dokument für die Regeln.*

**Die Dialogform der Komponentenkosten ist abgenommen** (Anwender, 02.09.2026): Kopfband, Reiter
Investition / Betrieb / Ertrag, Raster mit Position · Kostenart · Bemessung · Satz · Menge mit
Herleitungszeile · Betrag · Runde, Warnband, Fußleiste. Für **PV** dieselbe Form mit eigenen
Anordnungen: Spalte *Nutzungsdauer* im Investitionsraster, Herleitung der kWp-Menge aus
Modulanzahl × Modulleistung (Befund I-1 sichtbar), Gruppe *Ersatz und Restwert* mit Barwerten,
Kennzahl €/kWp, Betriebsseite ohne Endenergie-Bemessung, Gruppe *Ertrag und Degradation* (V-G2).

**Beispielprojekt** (durchgängig): BHKW 300 kW_el, Erdgas, η 38 / 45 / 83 %, 5.500 h/a → Brennstoff
4.342,1 MWh/a, Strom brutto 1.650,0, netto 1.563,2 MWh/a · PV 300 kWp (750 × 400 Wp), 285,0 MWh/a ·
Reststrombezug 250 MWh/a · produzierendes Gewerbe · i = 3 %, T = 20 a. Belegzahlen des Bestands
(Kaskadenprobe 1042, Mischsatz 300 kW, AW 300 kWp, Höfingen) sind als solche gekennzeichnet.

| # | Kategorie | Rechenweg | Kernaussage der Zahlenprobe |
|---|---|---|---|
| 1 | Investitionskosten BHKW | `01` | Kaskadenfaktor 1,155; I₀ = 33.927,61 − 6.000 = 27.927,61 € |
| 2 | Betriebskosten BHKW | `02` | Hilfsenergie 2 % × 312.631 € Endenergiekosten = 6.252,62 €/a (21.710 kWh Strom); B-1 am Kessel sichtbar als „× 0 kWh" |
| 3 | Kosten der Photovoltaik | `03` | 192.150 € = 640,50 €/kWp; Wechselrichter-Ersatz Jahr 12 (24.000 €, Barwert 16.833), Restwert 39.800 € (Barwert 22.036); Degradation 0,5 %/a → Jahr 20: 259,1 MWh |
| 4 | Energiekosten | `04` | Preisbestandteile 0,0638 + 0,1371 + 0,1180 + 0,4371 = 0,7560 €/m³; BEHG 872,3 t × 65 € = 56.700 €/a; N3 +32 % |
| 5 | **Vergütungen BHKW** | `05` | **Mengentafel** brutto 1.650 → netto 1.563,2 (§ 9 Nr. 3 bleibt brutto); Mischsatz 5,5667 / 2,4167 ct; 2026 vergütet 60 % = 31.531 €; **Reihe endet nach zwölf Jahren** (286.644 €); § 53a 21.203 €/a |
| 6 | **Vergütungen PV** | `06` | AW 6,04 ct; Spot 8.977,50 + Prämie 3.072,30 − § 51 614,46 − DV 798,00 − Kappung 241,00 = 10.396,34 €/a; § 51a 1.204,98 € |
| 7 | Erlösrubrik | `07` | Block A 91.330,5 €/a; vermieden brutto 339.753,6 − entgangene § 9b 23.594,0 = effektiv 316.159,6 €/a |
| 8 | Wirtschaftlichkeit über Nutzungsdauer | `08` | Höfingen: Näherung 65.073 €, jahresscharf 65.259 €; IZF/Amortisation nachrichtlich |

**Drei Darstellungen, die über den bisherigen Stand hinausgehen und in die Umsetzung gehören:**

1. **Mengentafel im BHKW-Dialog** — Brutto, Hilfsstrom, Netto, Eigen/Einspeisung und der brutto
   bleibende § 9-Eigenverbrauch untereinander, mit der Spalte „verwendet von". Sie macht das
   Netting (§ 3.6) erklärbar, statt es zu verstecken.
2. **Jahresreihe des Zuschlags als Diagramm** — Deckelstaffel und Kontingent auf einen Blick; die
   Vorschau „Zuschlag p. a." allein suggeriert eine Dauerförderung.
3. **Ersatz- und Restwerttafel bei PV** — Wechselrichtertausch und Modulrestwert sind der
   Regelfall; die Nutzungsdauer-Spalte gehört bei PV ins Investitionsraster, die Abweichung V-G4
   wird dort deklariert.

**Fachliche Klarstellungen, die beim Durchrechnen entstanden sind:**

- Der Emissionsfaktor Erdgas in der BEHG-Reihe ist der EBeV-Wert **200,9 g/kWh (H_i)** (§ 3.11);
  CO₂-Preisbestandteil im Arbeitspreis und BEHG-Reihe beschreiben denselben Betrag — **im
  Kapitalwert darf nur einer von beiden stehen**; die Kohärenzprüfung (§ 3.9) bekommt dafür eine
  Zeile.
- Die vermiedene Strommenge der Differenzmethode ist die **physisch** vermiedene (netto
  Eigenverbrauch BHKW + PV-Eigenverbrauch), nicht die brutto bemessene § 9-Menge.
- Der KWKG-Eigenstromsatz braucht einen Tatbestand des § 6 Abs. 3; ohne ihn ist der Satz 0 und
  Bonus_voll halbiert.

**Namensvorsicht:** „Befund K-1" (§ 3.6, Abwärmeabfuhr) und „Entscheidung K1" (§ 5, Deckung je
Modul) sind verschiedene Dinge — im Ordner steht der Befund mit Bindestrich.

---

# 3 Die Rechenwege

Alle Formeln in der Fassung der Formelkarte vom 30.08.2026 gegen `b2ad3e3`.
Lesehilfe: `??` heißt „Wert links, wenn gepflegt; sonst Wert rechts".

## 3.1 Rahmen — Kapitalwert nach DIN EN 17463

```
KW [€] = − I₀
         + Σ_{t=1..T}  ( E_t − A_t ) / (1 + i)^t
         + RW_T / (1 + i)^T
         + Einmalzahlung_t0
```

```
A_t [€] = Betrieb_t   × (1 + p_B)^(t−1)
        + Energie_1   × (1 + p_E)^(t−1)
        + CO2_t
        + Ersatz_t

E_t [€] = Einspeiseerlös_1                    ← nominal KONSTANT, keine Steigerung
        + Σ über alle benannten Erlösreihen: Reihe.Wert(t)
```

Rahmengrößen aus `Tab_ProjektWirtschaftlichkeit`, **eine Zeile je Stammprojekt**:

| Größe | Spalte | Vorgabe |
|---|---|---|
| Kalkulationszins i | `Zinssatz` [%] | 3,0 |
| Betrachtungszeitraum T | `Betrachtungszeitraum` [a] | 20 |
| Preissteigerung Energie p_E | `Preissteigerung_Energie` [%/a] | 0,0 |
| Preissteigerung Betrieb p_B | `Preissteigerung_Betrieb` [%/a] | 0,0 |
| Einspeisevergütung PV | `Einspeiseverguetung` [€/kWh] | 0,0 |
| Einspeisevergütung KWK | `Einspeiseverguetung_KWK` [€/kWh] | NULL = aus |
| CO₂-Preis-Override | `CO2_Preis` [€/t] | 0 = Katalogpfad |

**Genau zwei Preissteigerungsreihen** — keine je Träger, keine je Position.

**Sechs benannte Erlösreihen**, jahresscharf: `KWKG_ZUSCHLAG` · `KWKG_PAUSCHALE` (Index 0 =
Einmalzahlung) · `ENERGIESTEUER_GUTSCHRIFT` · `STROMSTEUER_BEFREIUNG` · `STROMSTEUER_ENTLASTUNG` ·
`PV_VERGUETUNG`. Index 0 wird nicht abgezinst und mindert I₀ nicht.

**Nutzungsdauer, Ersatz, Restwert, Startjahr:**

```
n [a] = Nutzungsdauer, falls ≥ 1 ; sonst n = T        (dann kein Ersatz, kein Restwert)
start = StartJahr, falls > 1 ; sonst 0

start = 0    → Betrag in I₀
start ≥ 2    → Zahlung im Jahr start, abgezinst, NICHT indexiert
start > T    → keine Zahlung, nur Ausweis

Ersatz:   t_j = round(start + k·n)  für k = 1,2,…  solange 1 ≤ t_j < T
Restwert: Alter = T − letzte Beschaffung ;  Restdauer = n − Alter
          RW_T [€] = Betrag × Restdauer / n     (nur bei Restdauer > 0, linear)
```

**Kennzahlen:**

```
Annuitätenfaktor a(i,n) = i·(1+i)^n / ((1+i)^n − 1) ;  a = 1/n bei i ≈ 0
```

| Kennzahl | Formel | gilt für |
|---|---|---|
| Kapitalwertdifferenz | KW(Variante) − KW(Stamm) | Varianten |
| Annuität | KW-Differenz × a(i, T) | Varianten |
| Dynamische Amortisation | erstes t mit kumuliertem Barwert ≥ 0, linear interpoliert, **ohne Restwert** | Varianten |
| Interner Zinsfuß | Nullstelle KW(r), Bisektion −99 %…1000 %, 200 Schritte | Varianten |
| Wärmegestehungskosten | (−KW × a(i,T)) / (Wärmebedarf [MWh/a] × 1000) [€/kWh] | je Projekt |

**Szenarien (VALERI-Vorrang):**

```
Szenariowert = Szenariospalte, falls ≠ 0 ; sonst EingegebenerWert
szenarioGepflegt ⇔ | Szenariowert − EingegebenerWert | > 1e−9
```

Ist ein Szenariowert gepflegt, wird **jede Ableitung übersprungen** und der Wert roh angesetzt — an
allen drei Lesestellen identisch.

**Sensitivität** (nur „Erwartet"): Zins ± 1 %-Punkt · Energiepreissteigerung ± 1 %-Punkt ·
Investition der Variante ± 10 % (Zuschuss wird **nicht** mitskaliert) · Energiekosten inkl. CO₂
± 10 % · „KWKG-Bonus entfällt" (nur die KWKG-Reihen, Steuergutschriften bleiben).

## 3.2 Investitionskosten — Drei-Runden-Kaskade

Lesepunkt: `Tab_ProjektWerte` mit `KategorieID = 1`, **ohne ORDER BY** (Befund I-3).

**Runde 1 — direkte Arten.** Vorrangordnung:

1. Bemessung leer oder `BETRAG` → Betrag = Szenariowert
2. **VALERI**: gepflegter Szenariowert verdrängt jede Ableitung
3. **Menge frisch vor Konserve** (H2-1): Kaskadenbasis → `BaugroesseSumme` (Gerätewelt) →
   `Tab_ProjektWerte.Menge`
4. Rechnung über den **einen** Rechenweg `BetriebskostenCtrl.Betrag`

| Bemessung | Formel | Mengenquelle |
|---|---|---|
| `BETRAG` / leer, `JAHRESBETRAG` | Betrag = EingegebenerWert | — |
| `EUR_PRO_KW_HEIZLEISTUNG` | Σ Nennleistung × Satz | `Tab_WP.Nennleistung` |
| `EUR_PRO_KW_LEISTUNG` | Σ P_therm × Satz | `Tab_Heizkessel.Ptherm` |
| `EUR_PRO_KW_ELEKTRISCH` | Σ P_el × Satz | `Tab_BHKW.Pel` |
| `EUR_PRO_KWP` | Σ PV_Leistung × Satz | `Tab_Energieanlagen.PV_Leistung` ⚠ **I-1** |
| `EUR_PRO_KWH_KAPAZITAET` | Σ Energie × Satz | `Tab_Stromspeicher.Energie` |
| `EUR_PRO_M2_KOLLEKTOR` | Σ (Aperturfläche × Modulanzahl) × Satz | Solarthermie |

Art ↔ Gewerk wird gekreuzt geprüft: falsches Paar ⇒ **null, keine Fantasiezahl**. Pufferspeicher
liefert immer null (ohne Temperaturpaar keine belastbare kWh).

**Runde 2 — `PROZENT_ERZEUGERKOSTEN`:**

```
Basis  [€] = Σ Betrag der Runde-1-Zeilen mit IsMainComponent = TRUE
             UND Kostenart ≠ ZUSCHUSS UND gleicher KomponentenID
Betrag [€] = Basis × Satz [%] / 100
```

**Runde 3 — `PROZENT_INVESTITION`, stufig:**

```
1. Anlage     (ID_Anlage > 0, Summe ≠ 0)
2. Komponente (KomponentenID > 0, Summe ≠ 0)
3. Projekt    (alle)
4. Basis 0 → null → Rückfall auf die Mengenkette
Betrag [€] = Basis × Satz [%] / 100
```

Beleg der Kaskadenwirkung (Projekt 1042): A = 26 × 653,60 = 16.993,60 · B = 5 % = 849,68 ·
C = 10 % × (A + B + 13.000) = 3.084,33 → Delta exakt **+20.927,61 €**. Als Faktor:
1,155 = 1 + 0,05 + 0,10 × 1,05.

## 3.3 Reduktionen — Zuschüsse

```
I₀_brutto        = Σ Betrag aller Nicht-Zuschuss-Positionen mit start = 0
Zuschuss         = min( Σ Zuschusszeilen , I₀_brutto )      ← Klemme
Zuschussüberhang = Σ Zuschusszeilen − Zuschuss              ← nur Ausweis + Hinweis
I₀               = I₀_brutto − Zuschuss
```

Kennzeichen `Kostenart = "ZUSCHUSS"` (getrimmt, ohne Groß-/Kleinschreibung), Erfassung **positiv**,
`Zuschuss = Σ |Betrag|`.

**Der Abzug steht nach der Positionsschleife.** Ersatzreihe und Restwert entstehen deshalb aus den
**Bruttobeträgen**; `Ergebnis.Investition` bleibt brutto, nur I₀ ist netto. Zuschusszeilen erzeugen
keine Ersatzbeschaffung, keinen Restwert und stehen in keiner Kaskadenbasis.

## 3.4 Betriebskosten

**Der eine Rechenweg** — Sperre zuerst: fehlt Menge **oder** Satz ⇒ **Betrag = 0**, nicht der
gespeicherte Wert.

| Gruppe | Bemessungen | Formel |
|---|---|---|
| A absolut | `BETRAG`, `JAHRESBETRAG` | Betrag = EingegebenerWert |
| B Prozent | alle `PROZENT_*` | Betrag = Menge × Satz / 100 |
| C Produkt | alle `EUR_PRO_*` | Betrag = Menge × Satz |

**Vorrangordnung der Bezugsmenge** (frisch vor Konserve, H2-1):

1. Szenariowert gepflegt → keine Ableitung
2. `BETRAG`/leer → gespeicherter Wert
3. **Endenergie-Arten** (`PROZENT_ENDENERGIEKOSTEN`/`_BEDARF`): Menge **immer frisch** aus dem
   jüngsten Lauf; Auflöser null ⇒ Betrag 0 — die Konserve greift nie
4. **Rückfall-ermittelbare Arten** (9 Stück): frisch versuchen, Konserve nur bei null
5. **Übrige Arten** (`EUR_PRO_H`, `EUR_PRO_KWH`, `PROZENT_BRENNSTOFF-`/`STROMKOSTEN`): nur
   Konserve (Befund B-4)

**Endenergie je Komponente** (`EndenergieAufloeser`, „jüngster Lauf" = höchste `Tab_Ergebnis.ID`):

| Komponente | Endenergie | Formel |
|---|---|---|
| BHKW | Brennstoff | Bedarf = Σ Verbrauch × 1000; Kosten = Bedarf × Arbeitspreis(CarrierId) |
| Heizkessel | Brennstoff | ebenso ⚠ **B-1**: `Verbrauch` ist im Bestand 0 |
| Wärmepumpe | Strom | Bedarf = Σ (Stromverbrauch + Heizstab) × 1000; Kosten = Bedarf × Strompreis |
| PV · Solarthermie · Speicher | keine | null — nur Jahresbetrag zulässig |

Preis = `PreisArbeit / EffHi`, **ohne** Grund- und Leistungspreis.

**Weg B braucht keine zweite Formel:** Der Auflöser übergibt den **bewerteten** Bedarf
(kWh × Strompreis), weil `Menge × Satz/100 × Preis` dasselbe ist wie `(Menge × Preis) × Satz/100`.
Die Sätze von A und B sind **nicht austauschbar** — Faktor ≈ 3,4, das Preisverhältnis Strom zu
Brennstoff.

**Erlöse:** `IstErloes && wert > 0 → wert = −wert`, an drei Stellen identisch geklemmt.

**Vorrangregel Prozent vor Absolut** (aus KONTEXT § 5.3): Eine gepflegte Satzangabe schlägt den
Absolutbetrag; das unterlegene Feld wird **gesperrt, nicht geleert** (KL4) — anders als in der
Altanwendung, die beim Speichern die Absolutfelder leerte (stiller Datenverlust, Altbefund 6). Die
„oder"-Doppelfelder der Wartung (€/kWh_el neben €/h, dort tatsächlich **addiert**, Altbefund 7)
gibt es nicht mehr: **eine** Position mit sichtbarer Bemessungswahl.

**Basis „% der Investition" auf der Betriebsseite** (`InvestSummeFuer`): `SUM(EingegebenerWert)`
Kategorie 1 ohne Zuschuss, stufig Anlage → Komponente → Projekt, **vor** Zuschussabzug — abgeleitete
Beträge fehlen dort (Befund B-5).

## 3.5 Energiekosten und CO₂

**Mengen:** `verbrauchJeTraeger[carrier] += Verbrauch [MWh/a]` je BHKW- und Kesselmodul; Menge ≤ 0
wird verworfen; `carrier ≤ 0` bei Menge > 0 ⇒ `kostenVollstaendig = false`.

**Arbeitspreis:**

```
[A] eff_hi > 0:  Menge [Einheit/a] = MWh × 1000 / eff_hi ;  Arbeit = Menge × PreisArbeit [€/Einheit]
[B] sonst:       Arbeit = MWh × 1000 × PreisArbeit [€/kWh]
```

`eff_hi` ist **kein Wirkungsgrad**, sondern der Heizwert in kWh je Abrechnungseinheit. **In der
Kostenkette wird nie durch η geteilt** — der Verbrauch ist bereits Endenergie.
Vorrang nur für Werte > 0: `custom_price_work` → `price_work` → null ⇒ Energiekosten = null.

**Grundpreis:** einmal p. a. je Träger; `custom_price_base` gilt auch bei 0 (nur NULL fällt durch);
wird nur addiert, wenn ein Arbeitspreis existiert.

**Leistungspreis** — die einzige η-Division der Kostenkette:

```
kw = BHKW:  (P_el + P_therm) / η_gesamt        Kessel:  P_therm / η
     (η > 1,5 gilt als Prozentangabe ÷ 100; außerhalb (0;1,5] wird die Anlage übersprungen)
Saisonreihe (12 Monatssätze) vor konstantem Satz
Modus JAHR:  Satz × kw          Modus MONAT:  Satz × kw × 12
Stromträger ausgenommen; kw ≤ 0 ⇒ kein Leistungspreis
```

**Netzbezug Strom:** `StromkostenNetz = Stromrestbedarf × 1000 × Preis + Grundpreis`. Im Tarifmodus
ersetzt der Zonen- oder Rollenbetrag den Flat-Anteil; danach **immer** `+ AufschlagBetrag`.

**Aufschlagsblock** (Schalter `Aufschlaege_Anwenden`, Vorgabe AUS):

```
AufschlagBetrag = NetzbezugMWh × 1000 × WirksamCtKwh / 100
Regelfall: 6,440 + 2,946 + 2,050 + 0,110 + 0,200 = 11,746 ct/kWh
```

⚠ **Befund N3:** Ungepflegte (NULL-)Spalten lesen sich als **Vorschlagswerte**, nicht als 0 — ein
ungepflegter Stromträger liefert 11,746 ct/kWh. Gemessen an Projekt 1030: **+360.603 €/a (+32 %),
Kapitalwert −29,8 %.**

**CO₂ / BEHG** als eigene Reihe:

```
behgBasisT [t/a] = CO2Brennstoff + (ohne Nachhaltigkeitsnachweis) BiogenBehgMenge × BehgOhneNachweis/1000
BEHG_t [€]       = behgBasisT × CO2-Preis(Kalenderjahr)
```

Preis: Override `CO2_Preis > 0` (dann mit p_E fortgeschrieben), sonst Katalogpfad
(2021–25: 25/30/30/45/55 · 2026/27: 65 · ab 2028: 80 als Prognose).
⚠ **Bedeutungsumkehr seit K6: 0 heißt „Pfad", nicht mehr „aus".**

**Emissionsfaktor-Kette** (eine für alle Rechner): PROJEKT → KATALOG → STAMM → CARRIER → null.
CO₂ in g/kWh, SO₂/NOₓ in mg/kWh. Strommix-Rückfall 435 g/kWh bei fehlendem Stromträger (mit
Hinweis).

## 3.6 Vergütungen

### KWKG-Zuschlag

**Die Staffel ist marginal, nicht klassenweise:**

```
Satz [ct/kWh] = Σ_k Breite_k × Satz_k / P_el
Breite_k      = min(Obergrenze_k, P_el) − Obergrenze_(k−1)
```

Beleg 300 kW: (50 × 8,00 + 50 × 6,00 + 150 × 5,00 + 50 × 4,40) / 300 = **5,5667 ct/kWh**. Eine
Klassenlogik hätte 4,40 geliefert — **21 % zu wenig**.

§ 7 Abs. 3a geht Abs. 1 und 2 vor: Neuanlage ≤ 50 kW → 16,00 / 8,00 ct/kWh.
Staffel Abs. 1: 8,00 / 6,00 / 5,00 / 4,40 / 3,40 (nachgerüstet 3,10).
Abs. 2 (Eigenstrom) nur in den drei Tatbeständen des § 6 Abs. 3; `KEINER` ⇒ Satz 0.

Sätze je Anlage: `Satz_Einspeisung(A) = Anlagensatz ?? Projektsatz`. Beim Eigenstromsatz ist die
Anlagenebene **strenger**: Ist der Anlagensatz gepflegt, wird ein Tatbestand verlangt — fehlt er,
Satz 0 mit Meldung.

**Vbh-Kontingent § 8:** Override > 0 gewinnt; sonst neu 30.000 h · modernisiert ab 50 %/25 % →
30.000/15.000 · nachgerüstet ab 50/25/10 % → 30.000/15.000/10.000; darunter 0 mit Fehlgrund.

**Jahresreihe:**

```
Netto(A)   = max(0, Brutto(A) − Hilfsstrom(A)) ;  Anteil = Netto(A) / Σ Netto
Eigen/Einsp(A) = Projektmengen_netto × Anteil      (ohne Stundenreihen: alles Eigen)
Bonus_voll = Eigen × 10 × SatzEigen + Einsp × 10 × SatzEinsp        [€/a bei ct/kWh]

je Jahr:  Vergütet = min(Vbh, Deckel(Jahr), Restkontingent) × (1 − Abschlag)
          Reihe[t] += Bonus_voll × Vergütet / Vbh
          Rest     −= Vergütet
```

Deckelstaffel 5.000 (2021) … 3.300 (2026) … 2.500 (ab 2030). Vorgeschaltete Prüfkette: Stichtag
≤ 31.12.2026 · Realisierungsfrist 4 Jahre · Ausschreibung > 500 kW · Heizöl-Neuanlage ab 2025.

**Pauschale § 9** (≤ 2 kW): `0,04 × 60.000 × P_el`, einmalig in Index 0.

### Hilfsstrom und die Bemessungsgrundlage des Zuschlags

```
Hilfsstrom(A) = Hilfsenergie_Anteil [%] / 100 × Brennstoff(A) [MWh/a]
Eigen zuerst: Eigen' = max(0, E − H) ;  Einsp' = max(0, F − max(0, H − E))
```

Das Netting wirkt **nur** auf die KWKG-Zuschlagsmengen. `StromMatrix`, § 9 Abs. 1 Nr. 3, der
CO₂-Grenzwert und die Vollbenutzungsstunden bleiben **brutto**.

**Rechtskette — nachgetragen 30.08.2026.** Bis dahin stand die Regel „der Zuschlag bemisst sich auf
die Nettostromerzeugung" **ohne Fundstelle** im Konzept (§ 4.3) und war so implementiert. Der
Nachweis:

| Norm | Wortlaut |
|---|---|
| **§ 7 Abs. 1 KWKG** | „Der Zuschlag für **KWK-Strom**, der in ein Netz der allgemeinen Versorgung eingespeist wird …" |
| **§ 7 Abs. 2 KWKG** | „Der Zuschlag für **KWK-Strom**, der nicht in ein Netz der allgemeinen Versorgung eingespeist wird …" |
| **§ 2 Nr. 16 KWKG** | „**KWK-Strom** ist das rechnerische Produkt aus Nutzwärme und Stromkennzahl der KWK-Anlage; bei Anlagen, **die nicht über Vorrichtungen zur Abwärmeabfuhr verfügen, ist die gesamte Nettostromerzeugung KWK-Strom**" |
| **§ 2 Nr. 20 KWKG** | „**Nettostromerzeugung** ist die an den Generatorklemmen gemessene Stromerzeugung einer Anlage **abzüglich des Stromverbrauchs der Stromerzeugungsanlage oder von deren Neben- und Hilfsanlagen**" |

Damit ist die Kette geschlossen: § 7 zahlt auf KWK-Strom → bei Anlagen ohne Abwärmeabfuhr ist das
die Nettostromerzeugung → und die zieht den Hilfsstrom ab. **Das Netting ist richtig**, und der
Begriff „Nettostromerzeugung" ist der des Gesetzes, keine Erfindung des Konzepts.

> ⚠ **Befund K-1 (neu): Der zweite Fall des § 2 Nr. 16 fehlt.** Verfügt eine Anlage **über eine
> Vorrichtung zur Abwärmeabfuhr** — beim Notkühler größerer BHKW der Regelfall —, ist KWK-Strom
> **nicht** die Nettostromerzeugung, sondern `Nutzwärme × Stromkennzahl`. Das ist eine völlig
> andere Größe: Sie hängt an der genutzten Wärme, nicht an der Stromerzeugung, und kann bei
> Wärmeüberschuss deutlich darunter liegen. EPOS-Plan führt **weder** ein Kennzeichen
> „Abwärmeabfuhr vorhanden" **noch** eine Stromkennzahl und rechnet immer den ersten Fall.
> Für Anlagen mit Notkühler fällt der Zuschlag damit **zu hoch** aus. Zu entscheiden, nicht
> stillschweigend zu lassen.

### Einspeiseerlös

`PV_Überschuss × 10 × EV + KWK_Einspeisung × 10 × EV_KWK` (KWK-Teil nur bei gepflegtem Satz) →
Zonentarif → Rollentarif → PV-Dialog ersetzt den PV-Anteil durch seine Reihe. **Nominal konstant.**

### Photovoltaik / EEG

```
Degression:  Faktor 0,99^n  (Halbjahresstichtage 1.2./1.8. ab 01.02.2024 bis Inbetriebnahme)
AW_mix    =  round( Σ Anteil_k × AW_Klasse_k / Σ Anteil_k , 2)
             marginale Klassen 10 / 40 / 100 / 400 / 1000 kWp
EV_mix    =  max(0, AW_mix − 0,40)          nur ≤ 100 kW
Ausfallvergütung = AW × (1 − 20 %)          nur > 100 kW

§ 51 je Jahr (AUTO):  IBN < 25.02.2025 → nein ;  ≥ 100 kWp → ja ;
                      sonst ab dem Jahr nach dem iMSys-Einbau
Ausfallanteil a:      Pauschale 20 %  oder stundenscharf  Σ Einsp(Spot<0) / Σ Einsp
60-%-Kappung:         Verlust = Σ max(0, Einsp_h − 0,6 × kWp)
Marktprämie:          Erlös = Spot€ + Arbeit × max(0, AW − Jahresmarktwert)/100
                             − Arbeit × DV/100
§ 51a:                im letzten Vergütungsjahr  Ausfallarbeit_J1 × 0,5 × AW/100
```

Belege: 8,60 × 0,99⁵ → 8,10 ct/kWh ab 08/2026 (16/16 BNetzA-Werte exakt) · 300 kWp → 6,04 ct/kWh ·
Marktprämie Jahr 1 = 13.536,00 € · § 51a = 1.812,00 €.

### Vermiedene Stromkosten — Ausweis, kein Zahlungsstrom

```
Bezug     = Rollenkosten(Bezugstarif,    Bedarf OHNE Anlage)
Reststrom = Rollenkosten(Reststromtarif, Restbezug MIT Anlage)
Vermieden = Bezug − Reststrom       je Arbeit / Leistung / Gesamt
```

Der **Leistungsanteil ist regelmäßig negativ** — das ist die Kernaussage, kein Fehler. In den
Kapitalwert geht der **Reststrom**betrag; die Differenz zusätzlich zu buchen wäre Doppelzählung
(E5, fünffach belegt).

## 3.7 Energiesteuer — anlagenscharf

Je Betrachtungsjahr ein Rechnerlauf (Kalenderjahr = Förderbeginn + t − 1). Katalog: jüngste Zeile
mit `JahrVon ≤ Jahr`; fehlt der Satz ⇒ 0 € mit Begründung, **nie geraten**.
`Wahl(a) = Anlagenwert ?? Projektwert`, je Anlage genau **eine** Wahl.

```
§ 53   Gutschrift = Satz_voll(Träger, Jahr) × Menge
       VOLLER_BRENNSTOFF (Vorgabe) : Menge = Brennstoff(a) UNGETEILT      § 53 Abs. 2
       ENERGETISCH                 : Brennstoff × Strom/(Strom + Wärme)
                                     — kein Rechtsverfahren, bewusste Untergrenze
       nur Stromerzeuger; Kessel mit § 53/53a ⇒ 0 € + Begründung
       Wahlwirkung gemessen: Faktor 2,27

§ 53a  Gutschrift = Teilsatz × Brennstoff(a)          immer Gesamteinsatz
       Nutzungsgradschwelle 70 % (Projektgröße); ungepflegt oder unterschritten
       ⇒ 0 € + Begründung, kein Abbruch

§ 54   netto = max(0, Σ_{Wahl=54} Teilsatz_54 × Menge(a) − 250 €)
       Bedingung produzierendes Gewerbe oder Land-/Forstwirtschaft
       Sockel EINMAL je Lauf, bezogen auf den § 54-Teil
       Kessel-Bemessung: Verbrauch > 0 ?? (Waerme_Gas + Waerme_Oel) ÷ (Nutzungsgrad/100)
```

**Einheitenkette** — hier ist die Altanwendung um den Faktor 10 gescheitert:

| Einheit | Formel | Bedingung |
|---|---|---|
| €/MWh | MWh_Hi × (eff_hs / eff_hi) → Brennwertmenge (Erdgas 11,6/10,5 = 1,1048) | Hs und Hi > 0; sonst konservativ Hi + Hinweis |
| €/1.000 l bzw. kg | MWh × 1000 / eff_hi / 1000 | Hi > 0 **und** passende Abrechnungseinheit — **keine geratene Dichte** |
| €/GJ | MWh × 3,6 (Hi) | — |

Sätze: Erdgas 5,50 / 4,42 / 1,38 €/MWh · Heizöl EL 61,35 / 40,35 / 15,34 €/1.000 l · Sockel
250 €/a. Handproben 11/11 auf vier Nachkommastellen getroffen.

## 3.8 Stromsteuer

```
§ 9 Abs. 1 Nr. 3   Betrag = Regelsatz(Jahr) × KwkEigen [MWh/a] × Anteil
                   Anteil = Σ Strom(a, bestanden) / Σ Strom(a, alle)
                   Regelsatz 20,50 €/MWh (ab 2026)

   Vier Bedingungen: Hocheffizienz · räumlicher Zusammenhang 4,5 km (Anwenderangaben)
                     P_el ≤ 2 MW je Anlage
                     CO₂ < 270 g/kWh Energieertrag = Faktor_EBeV × Brennstoff/(Strom+Wärme)
   KwkEigen nur mit Stundenreihen — sonst 0 mit Begründung
   Beleg: Heizöl 303,1 g/kWh → keine Befreiung ; Erdgas 228,6 → Befreiung

§ 9b               Betrag = max(0, 20,00 €/MWh × Netzbezug [MWh/a] − 250 €/a)
                   Bedingung produzierendes Gewerbe; hängt an keiner KWK-Anlage
```

Die Mengen beider Vorschriften sind **disjunkt** (Eigenverbrauch gegen Netzbezug) — untereinander
keine Doppelzählung.

⚠ **Befund B-1 — die offene Frage dieses Felds.** § 9 Abs. 1 Nr. 3 wird heute als **Erlösreihe**
gebucht. Die Vorschrift ist aber keine Rückerstattung: Auf selbst erzeugten und selbst verbrauchten
Strom entsteht gar keine Stromsteuer. Der Vorteil steckt bereits in der kleineren Bezugsrechnung.
Gemessen an Projekt 1024: **1.510,84 €/a auf beiden Pfaden**; synthetisch 779 → 1.558 = das
Doppelte. **Im Bestand bucht kein gespeicherter Lauf die Reihe** — heute also nirgends wirksam.
Die Umstellung auf „Ausweis" (`Stromst_Befreiung_Modus`, Vorgabe AUSWEIS) ist mit B6 entschieden.

## 3.9 Kohärenzprüfung — Warnzeilen ohne Rechenwirkung

| Fall | Bedingung | Schwere |
|---|---|---|
| 1 konsistent | Wahl und Preisanteil aktiv | keine Zeile |
| 2 **Entlastung ohne Belastung** | Gutschrift gebucht, Preis weist die Steuer nicht aus | Warnung **mit Betrag** |
| 3 Belastung ohne Entlastung | Anteil ausgewiesen, keine Wahl bzw. kein § 9b bei produzierendem Gewerbe | Hinweis |
| 4 Satz ≠ Katalogsatz | Toleranz 0,005 ct/kWh | Hinweis (beide Sätze) |
| Doppelpflege Hilfsenergie | Anlagenanteil > 0 **und** aktive Kostenposition derselben Anlage | Warnung |
| Strommix-Rückfall | kein Stromträger, Netzbezug > 0 | Hinweis (435 g/kWh) |

## 3.10 Rechenreihenfolge

Die Ordnung ist zwingend — Prozentbezüge und fortgeschriebene Restkontingente hängen daran.

```
 1. Investition Runde 1     direkte Arten, VALERI-Vorrang, Menge frisch vor Konserve
 2. Runde 2                 % der Erzeugerkosten (Basis: Hauptpositionen der Komponente)
 3. Runde 3                 % der Investition, stufig Anlage → Komponente → Projekt
 4. Zuschussabzug           NACH der Positionsschleife — Ersatz und Restwert bleiben brutto
 5. Simulationslauf         Endenergie aus dem jüngsten Lauf (höchste Tab_Ergebnis.ID)
 6. Energiekosten           vor der Betriebsseite und vor dem Kapitalwert (R-3)
 7. Betriebskosten          InvestSummeFuer greift auf Kategorie 1 zu
 8. CO₂ / BEHG              nach den Trägermengen, Preis je Kalenderjahr
 9. Vergütungen             Satz (marginal) → Hilfsstrom-Netting → Anteile → Bonus_voll
                            → Jahresreihe mit Vbh/Deckel/Restkontingent  (Rest −= Vergütet)
10. Steuern                 je Betrachtungsjahr, anlagenscharf, § 54-Sockel einmal je Lauf
11. Kapitalwert             A_t/E_t, Abzinsung, Restwert, Index-0-Einmalzahlung
                            danach Kennzahlen und Sensitivität
12. Kohärenzprüfung         zuletzt, liest gebuchte Jahr-1-Werte
```

## 3.11 Emissionsfaktoren, Primärenergie und CO₂-Preispfad

*Aus `Grundlagen_KWKG_Energiesteuer_Stromsteuer.md` § 7 und § 8 (Rechtsstand 18.08.2026) — bislang
nicht im konsolidierten Dokument. Rechenwirkung über die BEHG-Reihe (§ 3.5) und den
`EmissionsBilanzRechner`; Anzeige über die Emissionsspalte der Trägertabelle (§ 2.5).*

**Zwei Faktorensätze, die im Code nie dieselbe Variable belegen dürfen:**

| Satz | Zweck | Quelle | Strom netzbezogen |
|---|---|---|---|
| **Nachweis** | Energieausweis, gesetzliche Nachweisführung | GEG/GModG Anlage 9, stichtagsabhängig | 560 g CO₂e/kWh bis 2026, **100** ab 01.01.2027 — politisch gesetzt, nicht physikalisch |
| **Reale Bilanz** | Wirtschaftlichkeit, CO₂-Kosten, Klimabilanz | UBA-Strommix (CLIMATE CHANGE 16/2026), jährlich im März, jüngstes Jahr geschätzt und im Folgejahr revidiert | 2025: 344 direkt · 352 ohne · **406 mit Vorkette** |

EPOS-Plan rechnet Wirtschaftlichkeit und Emissionsbilanz mit der **realen Bilanz**; der
Strommix-Rückfall 435 g/kWh (§ 3.5) ist der BAFA-EEW-Wert (Version 3.4, 01.06.2026). Die
Emissionsspalte zeigt den im Projekt gewählten Satz und benennt im Tooltip, ob eine Vorkette
enthalten ist (E-1). Schadstoffe außer CO₂ sind amtlich nur bis Datenjahr 2021 verfügbar — mit
Datenstand kennzeichnen.

**Stichtag 01.01.2027 (GModG, BGBl. 2026 I Nr. 226):** neue Anlagen 4 und 9 — Strom PEF 1,8 → 1,5,
Holz 0,2 → 0,7, Strom 560 → 100 g, Verdrängungsstrommix KWK (2,8 · 860 g) **entfällt ersatzlos**;
KWK-Wärme wird stattdessen nach DIN EN 15316-4-5 bewertet. Das ist ein **Methodenwechsel**, kein
Parameterwechsel: Beide Faktorensätze müssen mit Gültig-ab-Datum parallel vorliegen, und ein 2026
gerechneter Vergleich muss 2029 dieselben Zahlen liefern. Eine KWK-Stromgutschrift ab 2027 ist eine
**methodische Wahl** (UBA-Substitutionsfaktoren sind für erneuerbaren Strom hergeleitet) und gehört
als Auswahlparameter in den Bericht. *Korrektur zu den Altwerten der Excel-Anwendung: „Nahwärme
2,8" war der Verdrängungsstrommix, „Bio-Erdgas 0,5" gehört auf 0,3 (§ 22 Abs. 1 Satz 2).*

**Brennstoff-Emissionsfaktoren, rechtsverbindlich für die CO₂-Bepreisung** (EBeV 2030, Anlage 2
Teil 4, g CO₂/kWh H_i): Erdgas **200,9** (brennwertbezogen **181,4**) · Heizöl EL 266,4 · Heizöl S
286,9 · Flüssiggas 235,8 · Pflanzenöl und Biodiesel 266,4. **Hi/Ho-Falle:** Erdgas wird
brennwertbezogen abgerechnet — der Heizwertfaktor auf die Abrechnungsmenge angewandt liefert rund
10 % zu viel CO₂ (Umrechnung 3,2508 GJ/MWh). Für Träger ohne gesetzliche Festlegung gilt das
BAFA-Infoblatt (Biogas 152 · Pellets 36 · Holz 27 · Fernwärme 280 · Strom 435 g/kWh).

**Biomasse-Nullregel** (§ 8 EBeV 2030): null nur mit anerkanntem Nachhaltigkeitsnachweis, sonst
voller fossiler Standardwert — das ist die `BehgOhneNachweis`-Logik in § 3.5. Die Konvention für
biogenes CO₂ widerspricht sich zwischen EBeV, GEG, UBA-Bilanz, BAFA und UBA-CO₂-Rechner; sie gehört
deshalb als **ausgewiesene Einstellung** in den Bericht (`Biomasse-Konvention`, § 2.4), nie als
stille Annahme in den Code.

**CO₂-Preispfad:** 2021–2025 Festpreise 25 / 30 / 30 / 45 / 55 €/t · **2026: 65 €/t** — alle
sieben Versteigerungen 07–08/2026 endeten am Korridorhöchstpreis, deshalb kein Korridormittel ·
2027: Korridor 55–65 €/t politisch gesetzt, Gesetz im Verfahren (Kabinett 12.08.2026, Bundestag und
Bundesrat stehen aus) · **EU-ETS 2 ab 2028, nicht 2027** (VO (EU) 2026/667 vom 11.03.2026; erste
Abgabepflicht 31.05.2029). Ab 2028 gibt es keinen Rechtspreis mehr — Vorbelegung 80 €/t konservativ,
95 → 125 €/t mittel (Projektionsbericht 2026, nur sekundär belegt), Ø 150 €/t hoch (Agora). Der
Pfad gehört als editierbare Stützstellenreihe mit Status GESICHERT / VORLÄUFIG / PROGNOSE in den
Katalog; der Katalogpfad in § 3.5 („2026/27: 65 · ab 2028: 80") bildet die konservative Vorbelegung
ab. Die EEX-Auktionsdatei `nEHS_Auction_Reporting.csv` eignet sich zum automatisierten Nachpflegen.

---

# 4 Befunde

Aus der Abnahmeliste der Formelkarte. ⚠ = wirkt oder kann wirken.

## Investitionsseite

| Nr. | Befund |
|---|---|
| ⚠ **I-1** | `EUR_PRO_KWP` summiert `PV_Leistung` — dieselbe Spalte heißt andernorts ausdrücklich Modulanzahl. Ein €/kWp-Satz würde faktisch mit der Modulzahl multipliziert (~Faktor 2,5 bei 400-Wp-Modulen). |
| ⚠ **I-2** | Abgeleitete Bemessung ohne Satz ⇒ 0 €, nicht der erfasste Betrag. |
| ⚠ **I-3** | Runde 3 ist reihenfolgeabhängig: Zwei `PROZENT_INVESTITION`-Zeilen — die zweite rechnet die erste ein; ohne ORDER BY entscheidet ACE. |
| I-4 | `BaugroesseSumme` entdoppelt nicht (bei PV/Solar gewollt). |
| I-5 | Vergleichsstrenge uneinheitlich: ZUSCHUSS ohne, `PROZENT_*` mit Groß-/Kleinschreibung. |
| I-6 | Nicht migrierte Datenbank: keine Kaskade, keine Zuschusserkennung. |

## Betriebsseite

| Nr. | Befund |
|---|---|
| ⚠ **B-1** | **Kessel-Endenergie ist strukturell 0** — der Rechenkern setzt `Verbrauch` nie. Endenergie-Positionen am Kessel liefern 0 € und überschreiben die Konserve. Die Steuerseite umgeht das seit B3a, Kosten- und Betriebsseite nicht. |
| B-2 | Asymmetrie der Rückfälle: Endenergie-Arten unbedingt frisch, Rückfall-Arten bedingt. |
| B-3 | „Jüngster Lauf" ist die höchste ID, nicht der Zeitstempel. |
| B-4 | Vier Arten nie frisch: `EUR_PRO_H`, `EUR_PRO_KWH`, `PROZENT_BRENNSTOFFKOSTEN`, `PROZENT_STROMKOSTEN`. |
| B-5 | `InvestSummeFuer` summiert `EingegebenerWert` — abgeleitete Beträge fehlen. |
| B-6 | Fehler werden geschluckt (`catch {}` ⇒ still 0). |
| B-7 | `MengenEinheit` beschriftet die neuen Arten mit „€". |

## Energiekosten

| Nr. | Befund |
|---|---|
| ⚠ **N1** | Kesselbrennstoff fehlt in Kosten, CO₂ und BEHG **ohne Meldung** — Folge von B-1; `kostenVollstaendig` bleibt true. |
| N2 | 0 beim Grundpreis gültig, 0 beim Arbeitspreis „ungepflegt" — verschiedene Regeln, beide gewollt. |
| ⚠ **N3** | Ungepflegte Aufschlagsspalten wirken als 11,746 ct/kWh, nicht als 0. |

## Steuern und Vergütungen

| Nr. | Befund |
|---|---|
| ⚠ **S-2** | Kein projektweites Doppelentlastungsverbot — Anlage A nach § 53 und Anlage B nach § 54 gleichzeitig möglich. |
| S-1 · S-3 · S-4 · S-5 · S-6 | Überholte Zeilennummern älterer Protokolle · § 9-Meldung nennt Kessel „(0 kW)" · €/GJ ohne Ho-Umrechnung (für Kohle konsistent) · Radius 4,5 km nur Meldungstext, Erlaubnisschwelle 1.000 kW nirgends gelesen · `STROMST_REDUZIERT_SATZ` ungesät. |
| ⚠ **K-1** | **Der zweite Fall des § 2 Nr. 16 KWKG fehlt** (§ 3.6): Bei Anlagen mit Vorrichtung zur Abwärmeabfuhr ist KWK-Strom `Nutzwärme × Stromkennzahl`, nicht die Nettostromerzeugung. Weder Kennzeichen noch Stromkennzahl sind im Datenmodell vorhanden; der Zuschlag fällt für solche Anlagen zu hoch aus. |
| ⚠ **V-3** | PV-Reihe und KWKG-Pauschale haben in der Mehrjahrestabelle **keine eigene Spalte** — sie wirken nur in „Netto". |
| V-1 · V-2 · V-4 | EV-Rundung (EvMix unrundet, Erlös gerundet) · § 51a bewertet mit AW statt EV · Eigen/Einspeise-Split je Anlage ist benannte Näherung. |
| R-1 · R-2 · R-3 | Rahmenparameter je Stammprojekt, nicht je Variante · Hilfsenergie steigt mit p_B statt p_E (bei gleichen Sätzen null) · ohne bestimmbare Energiekosten kein Kapitalwert (Absicht). |

---

# 5 Offene Entscheidungen K1–K11

| # | Frage | Stand |
|---|---|---|
| K1 | Feld „Deckung je Modul" | **entschieden: kein Feld** — die Befreiung ist bilanziell |
| K2 | Hilfsenergie-Basis je Anlage rechnet fest Weg B (% des Bedarfs); Wege A und C nur in der Kostenposition | Dialog benennt die Basis klar; vierte Spalte nur bei Bedarf |
| **K3** | Modusfeld § 9 Nr. 3 — Spalte kommt erst mit B6 | **offen**: im Mockup ausgrauen **oder** Schritt 62 vorziehen |
| K4 | Tabellenspalte „Brennstoff" ohne Leseweg | kleiner Leser `CarrierId` → Name in B5 |
| K5 | Jahresnutzungsgrad bleibt Projektgröße | als Projektfeld zeigen |
| K6 | WP-Hilfsenergie: Spalte gilt formal für alle, Leser nur BHKW und Kessel | B5 zeigt das Feld nur bei BHKW |
| K7 | Schreibweg der drei B3-Spalten fehlt (`KwkgAnlagenCtrl.Speichere` = 8 Spalten) | **B5-Kernaufgabe**: auf 11 Spalten erweitern |
| **K8** | Fußleiste voll — ein achter Knopf läge bei x = −50 | **offen**: Zweitreihe · Aufklappmenü · Knopf ersetzen |
| K9 | § 6.1 zählt „9 Felder", real 11 | Konzeptkorrektur |
| K10 | Hilfsenergie-Bemessung doppelt: Seed gegen Altkatalog | in B5/B6 nachziehen |
| K11 | `Views\Wirtschaftlichkeit` unlokalisiert (63 Literale) | neue Texte `BHW_*` de + en; Altlast nach B6 |

Dazu die Entscheidungen zur Darstellung (30.08.2026):

| # | Frage | Entscheidung |
|---|---|---|
| **D-1** | Emissionsspalte der Energieträgertabelle | **eine** Spalte, Kopf und Inhalt nach `Emission_Berechnungsmodus`; SO₂/NOx entfallen aus dieser Übersicht (§ 2.5) |
| **E-1** | Modus `CO2E`, wenn außer CO₂ nichts gepflegt ist bzw. der Wert schon ein Äquivalent ist | **Wert zeigen, Umstand im Tooltip benennen** — drei Herleitungsfälle, kein stiller Rückfall auf „CO₂" (§ 2.5) |
| **D-2** | Erlösdarstellung | eigene Rubrik in zwei Blöcken, getrennte Summen; Block B (Ausweis) wird nicht addiert (§ 2.6) |
| **D-3** | Referenz der Differenzrechnung | **wählbares Vergleichsprojekt** je Gruppe (Stamm oder Variante), Vorgabe Stamm = ergebnisneutral; `ID_Referenzprojekt` an der Rahmenzeile; die Referenz ist die Unterlassensalternative der DIN EN 17463 (§ 2.9, Anforderung 31.08.2026) |

Aus dem Energieträger-Umfeld kommt eine weitere Entscheidung desselben Tages hinzu. Sie betrifft
die Wirtschaftlichkeitsrechnung nicht, wohl aber den gemeinsamen Schema-Nummernraum:

| # | Frage | Entscheidung |
|---|---|---|
| **U-1** | Einheitenbruch `Tab_Brennstoff_Stamm.Einheit` ↔ `energy_conversion` (BK3 § 6 Nr. 4): die Identitätsregel-Ableitung liefert für 9 von 25 Brennstoffen `-1` | **entschieden 30.08.2026 — Weg (a)**: Der Stammtext der fünf Gase (Brennstoffe 1, 2, 3, 14, 25) wird „m³" → „Nm³" gezogen, als **Schemaschritt 62** (Muster Schritt 26a; Leitentscheidung L4 auf die Stammseite fortgeschrieben). Die Wege (b) Identitätsregel-Saat und (c) `billing_unit`-Ableitung sind **nicht beauftragt** |

**Folge für den Schema-Nummernraum: 62 ist damit vergeben — neue Schritte anderer Etappen ab 63.**
Das schreibt die bisherige Regel „ab 62" (KOORDINATION § 4 Nr. 2) fort. Betroffen sind K3 oben sowie
§ 7 (Zeile B6 und „Voraussetzungen"): Dort steht für M-3 / § 9 Nr. 3 noch „Schritt 62" — diese
Nennung ist auf **63** nachzuziehen. Reine Nummernfrage, keine fachliche Änderung an B6.

Offen bleiben die Randfragen des Einheitenbruch-Konzepts: Waisenheilung (`energy_project_settings`
Zeile 10076, Projekt 1039), kg-/rm-Abrechnung der Brennstoffe 4/5/12, Brennstoff 24 „Sonstige", der
Fremdkörper Regel 67 und ein Prüfschritt in `EnergieEinheitenPruefung`. **Die Umsetzung ist noch
nicht freigegeben** (Konzept-vor-Code) und gehört auf den Pufferspeicher-Strang. Quelle:
`Konzept_Einheitenbruch_Energietraeger_EPOS-Plan.md` — liegt derzeit **nur** auf Zweig
`claude/lucid-cori-a9a425` (`37bd068` Konzept, `8e34222` Entscheidungseintrag), noch nicht gemergt.

**Rechtliche Unsicherheiten** (aus Grundlagen § 6, weiterhin offen — vor produktivem Einsatz mit
dem Hauptzollamt bzw. am Volltext zu klären; keine Entscheidung des Anwenders, sondern des Rechts):

| # | Punkt | Stand im Konzept |
|---|---|---|
| R-U1 | § 53 neben § 53a: der Wortlaut „vorbehaltlich" spricht für anteilige Anwendung, Kommentarliteratur und Dienstvorschrift für ein Entweder-oder | als **Auswahl** modelliert (`KEINE` / `PARAGRAF_53` / `PARAGRAF_53A`), nie Kombination; entschärft, weil § 53 den gesamten BHKW-Brennstoff erfasst (§ 3.7) |
| R-U2 | § 53a Abs. 3, Erdgassatz 4,96 €/MWh für das produzierende Gewerbe über dem allgemeinen 4,42 €/MWh | nicht umgesetzt; in § 2.6 (Klarstellung 2) als Kesselseite eingeordnet |
| R-U3 | Ausschluss fossiler flüssiger Brennstoffe aus der KWKG-Förderung — nur Sekundärquelle | als Prüfkette „Heizöl-Neuanlage ab 2025" umgesetzt (§ 3.6) |
| R-U4 | EuGH-Urteil 09.07.2026 zur Beihilfeeigenschaft des KWKG | kein Primärbeleg |
| R-U5 | keine Nachfolgeregelung nach 2030 | Förderzeitraum als Datumsparameter im Katalog, nicht als Konstante |

---

# 6 Umsetzungsstand

## 6.1 Abgeschlossene Etappen

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **W4 E1–E8, L12/L13** | Gesetzeskatalog, Vbh elektrisch, VDI-Bemessungsarten, Steuergutschriften, Tarif-Rollenmodell, KWKG je Modul, Bericht; Methodenwechsel 2027, Biomassekonvention | abgenommen 19.08.2026 |
| **K1–K6** | Alttabellen, kWh-Konsistenz, Einheiten-Seeds, Kostenprofil, Komponenten und Zuschuss, KWKG/Steuern einheitenrichtig | abgeschlossen 20.08.2026 |
| **KD1–KD6** | Vorlagentabellen, Komponenten-Kostendialog, Übernahme, Energieträgerverwaltung, Ertrag/Bonus | abgeschlossen 26.08.2026 |
| **P1–P6** | EEG-Satzrechner, Monatsmarktwerte, § 51/§ 51a, PV-Dialog, Kennzahlen | abgeschlossen 26.08.2026 |
| **H1–H4b, H21** | Pflichtpositionen und Endenergie-Bemessung, Bezugsgrößen-Auflöser, Investitionsraster als Drei-Runden-Kaskade, Mengen-Ausweis „frisch vor Konserve" | durchgängig ergebnisneutral |
| **B1** | Zahlenprobe: Doppelzählung § 9 Nr. 3 bestätigt (1.510,84 €/a doppelt) | reine Messung |
| **B2** | Schema 60, Preisbestandteile Brennstoff, Kohärenzprüfung (4 Fälle) | keine — 332/332 CSV byte-gleich |
| **B3a** | Schema 61, Steuerwahl je Anlage, § 54 auf Kesselbrennstoff | keine — Anker exakt |
| **B3b** | `HilfsstromRechner`, Netting nur in der KWKG-Reihe, Eigenstrom-Tatbestand je Anlage | keine bei Anteil NULL; **ja, sobald gepflegt** |
| **B4** | Stromsteuer-Schnellwahl katalogbasiert, Unternehmensart hebt hervor | keine — wertgleich |
| **BK1/BK2** | Trägerzuordnung über `code`, Wizard-Automatik, Emissionsspalten | **ja, gewollt** — CO₂-Bilanz ändert sich |
| **HB1** | Anzeigesortierung, Hydraulikbild liest `Z_AnlageSenke` | keine — 90 Dateien SHA256-gleich |

## 6.2 Regressionsanker

| Anker | Wert |
|---|---|
| `LiesBetriebskosten(1024)` | **99,00 €/a** |
| Kapitalwert 1024 | **−2.220.322,32 €** |
| `LiesInvestitionen` 1018 / 1024 / 1042 | 45.312,50 · 12.001,00 · 13.000,00 |
| Kaskadenregression 1042 | +20.927,61 |
| Referenzbasis | `Referenzlaeufe\2026-08-30_B3-Kaskade` |

Die 1030-Anker sind durch den Kaskaden-Umbau **überholt** und müssen neu gesetzt werden.

## 6.3 Offene Punkte

**B5-Kernaufgaben**

1. Schreibweg der drei B3a-Anlagenspalten — `KwkgAnlagenCtrl.Speichere` von 8 auf 11 Spalten (K7)
2. Live-Frisch-Anzeige der Bezugsgröße mit Herleitungszeile im Kostendialog — **spezifiziert in § 2.8 (Entwurf B, übernommen 31.08.2026)**
3. Erste Kostenposition mit Anlagenbezug entsteht erst hier

**Nach B6**

4. § 9 Nr. 3 als Ausweis (`Stromst_Befreiung_Modus`, Vorgabe AUSWEIS) — K3
5. Doppelmeldung § 9b in `RechneAufschlaege` streichen
6. Hinweiszeile für Träger mit 1.000-kg-Satz bei Literabrechnung (`density` leer)
7. Positive Nennung im Kohärenzfall 1
8. Lokalisierung `Views\Wirtschaftlichkeit` (63 Literale) und der Auflöser-Texte — K11
9. Altkatalog-Bemessung `PROZENT_BRENNSTOFFKOSTEN` nachziehen — K10

**Fachlich und technisch**

10. Bezugsgrößen der übrigen KD1-Bemessungsarten (H1-1b)
11. Nachzieh-Migration für Bestandsprojekte — durch die Auto-Anlage entschärft, bleibt Option
12. `InvestSummeFuer` auf die abgeleitete Kaskadensumme umbauen (B-5)
13. Pufferkapazität bleibt null — bewusste Grenze
14. Reduzierter Stromsteuersatz bleibt Konstante bis zur Katalog-Nachpflege
15. Bilanzjahr und Unternehmensart wirken erst beim nächsten Dialog-Öffnen
16. Rückweg „Parameterdialog zeigt den erfassten Preisanteil" fehlt
17. Kohärenzzeilen nicht persistiert · Fall 4 ohne Katalogsatz bleibt still
18. Engine-Sortierung `ORDER BY Prioritaet` (HB1-O1) — nur mit vollem Referenzlauf
19. Asymmetrie „Wartung BHKW" gegen „Vollwartung / Wartung Kessel"

**Nachweis und Betrieb**

20. **Zahlenprobe gegen die Altanwendung (A8) — blockiert, wartet auf Zulieferung der
    BHKW-Plan-Excel.** Seit der Abnahme E8 der gewichtigste offene Punkt
21. Basiswechsel der Referenzläufe entscheiden; 1030 neu verankern
22. Sichtabnahmen: Brennstoffblock (B2), Kosten-Seite (BK1), Stromsteuer-Hervorhebung (B4)
23. resx-Sammelnachtrag der Textschlüssel aus B3a, B3b, B4 und der F-Serie
24. Datenpflege: Projekt 1018 Kessel ohne Energieträger, Puffer ohne Temperaturpaar;
    WP-Kennlinie 1024 ohne HT-Stützstellen

## 6.4 Fallstricke zur Wiederverwendung

- **ACE: `UPDATE … WHERE x IN (SELECT …)` trifft stillschweigend 0 Zeilen** — kein Fehler, keine
  Warnung. Fremdschlüssel vorher einzeln auflösen und die Zeilenzahl protokollieren.
- **ACE: Ein falscher Spaltenname meldet sich als fehlender Parameter**, nicht als unbekannte
  Spalte.
- Zwei gemischte ACE-Verbindungen sehen Fremdschreibungen verzögert.
- `SetzeBetrag` ist ein Upsert — ein Harness braucht unbenutzte StammIDs.
- Visual Studio regeneriert `Resource.Designer.cs`; Handeinträge erzeugen CS0102.
- Keine `.cs` unterhalb von `WindowsFormsApplication1\` (CS0017); Harnesse nach `dev\`.
- Build nur über das MSBuild von Visual Studio, x64 — `dotnet build` scheitert an COM.

## 6.5 Doppelte Wahrheiten

*Aus KONTEXT § 9 — jede benannt und begründet. Neue Spalten und Novellen müssen beide Orte treffen.*

| Doppelung | Stand |
|---|---|
| Stromsteuersatz an zwei Orten — Katalog `STROMST_REGELSATZ` gegen `const double` in `StromAufschlagModel` | wertgleich, ohne Kopplung: eine gepflegte Novelle erreicht den Aufschlagsblock nicht |
| „Energieintensiv" an drei Orten — Unternehmensart, Schnellwahl im Trägerdialog, Katalogsatz | seit B4 liest die Schnellwahl den Katalog und die Unternehmensart hebt den passenden Knopf hervor; gekoppelt ist weiterhin nichts |
| BHKW-Einspeisevergütung an vier Orten | Vorrang eindeutig (aktiver Tarif schlägt Parameterwert), drei Felder zu viel |
| Zwei Migrationsmechanismen — `SchemaMigration` gegen Selbst-DDL in `WirtschaftlichkeitCtrl` | vier Tabellen; neue Spalten gehören an beide Stellen |
| Zwei Lesewege auf die Kostenposition — die gespeicherte Access-Abfrage kennt die neuen Spalten nicht | der direkte Zugriff ist der Normalfall |
| Komponenten-IDs hart verdrahtet gegen dynamisch gelesen | `Form_Kosten` gegen `UcBkKosten` |
| Vorrang Projekt vor Katalog in drei Implementierungen | `KostenEmissionRechner`, `StromPreisCtrl`, eine Access-Abfrage |
| ~~Kennzahlenliste dreifach~~ | aufgelöst mit E7 — `WirtschaftlichkeitZeilen` führt sie einmal |

---

# 7 Vorgeschlagene Reihenfolge

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **B5** | `Form_BhkwWirtschaftlichkeit` mit sechs Gruppen; Auszug aus dem Parameterdialog; Schreibweg der drei Anlagenspalten (K7); Brennstoff-Leser (K4); Live-Herleitung | keine — solange niemand die neuen Felder pflegt |
| **B6** | § 9 Nr. 3 als Ausweis (M-3, Schritt 62); Kohärenz-Nachträge; Lokalisierung | **ja** — der Moduswechsel ändert den Kapitalwert |
| **B7** | Anlagenscharfe Aufschlüsselung der Energiekosten; **Erlösrubrik** (§ 2.6) in Reiter, Word und Excel; **Emissionsspalte nach Modus** (§ 2.5) | Ausweis — bis auf die Korrektur der vermiedenen Kosten um die § 9b-Entlastung |
| **B8** | Befunde abarbeiten: I-1 (kWp), I-3 (ORDER BY), B-1/N1 (Kessel-Verbrauch), N3 (Aufschlags-NULL), V-3 (Berichtsspalten), S-2 | **ja** — jeder einzeln mit A/B-Nachweis |
| **B9** | Zahlenprobe gegen die Altanwendung (A8), sobald die Excel vorliegt | Nachweis |

Die Reihenfolge ist bewusst so gewählt: **B5 bleibt ergebnisneutral** und macht nur pflegbar, was
bisher nur im Datenmodell stand. Die erste gewollte Ergebnisänderung kommt mit B6 — und trifft
dann eine Größe, die im Bestand nachweislich nirgends gebucht ist.

## Voraussetzungen vor der Umsetzung

Zwei Entscheidungen sollten vor B5 fallen, weil sie den Dialog selbst betreffen:

- **K3** — Modusfeld § 9 Nr. 3 im Mockup ausgrauen oder Schema-Schritt 62 vorziehen
- **K8** — wie der achte Knopf in die volle Fußleiste kommt

Beide sind reine Gestaltungsentscheidungen ohne Rechenwirkung.
