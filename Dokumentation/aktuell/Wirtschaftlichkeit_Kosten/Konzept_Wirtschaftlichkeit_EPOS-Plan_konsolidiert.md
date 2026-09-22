# Konzept: Wirtschaftlichkeit EPOS-Plan — konsolidiert

**Stand 22.09.2026** · Codestand `2cfee66b` · `SchemaStand.Zielversion` = 100 · Schemaschritte 90–100 vergeben, neue ab **101** · konsolidiert aus drei Quelldokumenten; Mockups und Rechenwege im Ordner `Wirtschaftlichkeit_Kosten/`

Die vier zuletzt vergebenen Schritte gehören nicht diesem Feld: **97** Szenario und Bezugsjahr der
Klimaregion (`Schritt97_KlimaSzenario`, KL‑6), **98** BHKW-Gesamtwirkungsgrad als Faktor (reines DML,
BW‑1), **99** die zwei Wirkungsgrade des BHKW (`Schritt99_BhkwWirkungsgradAnteile`, BW‑1), **100** die
Vorgabe 0 der Fremdschlüsselspalten (FK‑1, #426). Wer hier einen Schritt plant, nimmt die nächste
freie Nummer **bei der Umsetzung** — nicht im Papier.

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
> zusammen. **Bei Widerspruch gilt dieses Dokument.** Was sie an Stoff enthielten, der hier bislang
> fehlte, ist mit der Konsolidierung nachgezogen: § 2.12 (Kategorien-Mockups), § 3.4 (Vorrangregel),
> § 3.11 (Emissionsfaktoren und CO₂-Preispfad), § 5 (rechtliche Unsicherheiten), § 6.5 (doppelte
> Wahrheiten).
>
> **Wo die drei Quellen heute liegen:** [`Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md`](../../ueberholt/Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md)
> und [`KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md`](../../ueberholt/KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md)
> stehen in `ueberholt/` und sind damit Geschichte, **nie Regelquelle**;
> [`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md)
> bleibt in `aktuell/`, weil es Faktenbasis ist und kein umgesetztes Konzept. Was aus den beiden
> Geschichtspapieren weiter gilt, gilt, weil es **hier** steht.

> **Stand der Umsetzung: § 6.1.** Was hier als Soll steht, ist gebaut, sofern § 6.1 die Etappe
> führt. Was § 6.1 nicht führt, ist Entwurf zur Abnahme; § 6.3 nennt, was daran offen ist, § 7 die
> Reihenfolge der offenen Etappen.

**Herkunft.** Das Papier ist am 30.08.2026 unter der Arbeitsregel des Anwenders „erst das Konzept,
keine Umsetzung" begonnen worden; damals war von den hier beschriebenen Vorhaben nichts
implementiert. Seither sind die in § 6.1 geführten Etappen gelaufen — die Regel beschreibt die
Entstehung des Papiers, nicht seinen heutigen Geltungsumfang.

| Quelle | Was daraus einfließt |
|---|---|
| Formelkarte `rechenwege_formelkarte.md` (30.08.2026, gegen `2cfb871d`) | § 3 vollständig, § 4 — **nicht erhalten**: lag im Sitzungs-Scratchpad; Belege heute an der Codestelle |
| Feldkarte `b5_feldkarte.md` | § 2 vollständig, § 5 — **nicht erhalten**: lag im Sitzungs-Scratchpad; Belege heute an der Codestelle |
| [`ueberholt/Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md`](../../ueberholt/Konzept_BHKW_Wirtschaftlichkeit_EPOS-Plan.md) | Leitentscheidungen BW1–BW10 |
| [`ueberholt/KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md`](../../ueberholt/KONTEXT_Kosten_Energie_Wirtschaftlichkeit.md) | Datenwelten, Festlegungen L/KL/E/FK |
| [`Grundlagen_KWKG_Energiesteuer_Stromsteuer.md`](../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md) | Rechtsstand, Sätze, Fristen |
| Protokolle B1–B4, BK1, H1–H4b, H21, HB1, W4 E1–E8, K1–K6, KD1–KD6, P1–P6 | § 6 |

## Begleitendes Artifact

| Artifact | Inhalt | entspricht |
|---|---|---|
| [**Dialog, Formel, Zahlenprobe**](https://claude.ai/code/artifact/739d3cca-3b6c-4e2b-af8a-d1a7f73ddc9f) | acht Kostenkategorien, je Dialog-Mockup + Berechnungsgrundlage + durchgerechnete Zahlenprobe an einem Beispielprojekt; Schwerpunkt Vergütungen BHKW (Mengentafel brutto/netto, Mischsatz, Jahresreihe mit Deckel) und PV (AW, Marktprämie, § 51/51a, Kappung); Komponentenkosten BHKW und PV in derselben Dialogform — **die Repo-Datei `../Mockups/Dialog_Formel_Zahlenprobe.html` führt** | § 2.12 |

Fünf weitere Artifacts sind durch Repo-Dateien abgelöst und stehen nur noch im Protokoll: das
B5-Dialogmockup durch `../Mockups/Dialog_Formel_Zahlenprobe.html` Kat. 5 und den gebauten
`BhkwWirtschaftlichkeitDialog`, die Rechenwege durch [`Rechenweg/01…08`](Rechenweg/01_Investitionskosten_BHKW.md),
die Erlösrubrik BHKW durch [`Rechenweg/07_Erloesrubrik.md`](Rechenweg/07_Erloesrubrik.md), die
Pflichtpositionen durch § 2.8 und die ValERI-Bewertung Höfingen durch
[`Rechenweg/08_Wirtschaftlichkeit_Nutzungsdauer.md`](Rechenweg/08_Wirtschaftlichkeit_Nutzungsdauer.md).

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
| Stromsteuer-Befreiung § 9 Abs. 1 Nr. 3 *(Ausweis; Erlös nur auf ausdrückliche Wahl)* | |
| Stromsteuer-Entlastung § 9b | |
| PV-Vergütungsreihe · Restwert | |

## 1.2 Die vier Datenwelten

```
① Positionswelt      Tab_ProjektWerte ← Tab_Kostenfaktor
   Kategorie 1 = Investition, 2 = Betrieb; je Projekt, Komponente und ANLAGE
   Kostenart · Bemessung · Satz (Einheitpreis) · Menge · Betrag (EingegebenerWert) · IstErloes ·
   Nutzungsdauer · StartJahr · IstPflicht · ID_AnlageGeraet · StammID · VorlageID · NutzungsdauerID

② Vorlagenwelt       Tab_KostenVorlage / Tab_KostenVorlagePosition
   Auslieferung „Standard": Struktur und Bemessung, Sätze bewusst LEER

③ Energieträgerwelt  energy_carrier · energy_price · energy_project_settings
   Preise, Heizwerte, Emissionen, Aufschläge, B2-Preisbestandteile

④ Gesetzeswelt       Tab_Gesetzesparameter
   Schluessel · Klasse · JahrVon · Wert · Einheit · Status · Quelle
   Eine Novelle ist eine neue Jahreszeile, kein Überschreiben
```

Dazu die **Projektrahmen-Zeile** `Tab_ProjektWirtschaftlichkeit` — **eine Zeile je Stammprojekt**,
gültig für die ganze Vergleichsgruppe (Befund R-1), und seit Schema 61 die **neun
`KWKG_*`-Spalten** an `Tab_Energieanlagen` samt `Energiesteuer_Wahl`, `Aufteilung_Methode`,
`Hilfsenergie_Anteil` und `ID_Carrier`.

---

# 2 Der Dialograum

**Zu den Namen.** `Form_*` und `Uc*` sind die **eingefrorenen Hilfe- und KI-Kennungen** (Hilfe-
Zuordnung, Wissensbasis) und **keine Klassennamen mehr**: Seit der Schalentrennung liegen die
Dialoge als Razor-Komponenten unter `EPOS.UI/Dialoge/…` mit den Bündeln `…Daten`/`…Texte`, die
Plattformseite als `…Huelle` in der jeweiligen Schale. Dieses Kapitel nennt den gebauten Namen und
die Hilfekennung als Paar.

## 2.1 Welche Anlage hat welche Wirtschaftlichkeitsfelder

| Anlage | Investitionsfelder (Gerätewelt) | Betrieb: Hilfsenergie | Eigener Wirtschaftlichkeitsdialog |
|---|---|---|---|
| **BHKW** | `Kosten_Modul` + Montage + Lieferung + Schallschutzhaube + Abgasreinigung (Summe) · `Wartungskosten_kwhel` · Nutzungsdauer | `PROZENT_ENDENERGIEKOSTEN`, 2–4 % | **ja** — `BhkwWirtschaftlichkeitDialog` (§ 2.2) |
| **Photovoltaik** | `Modulkosten` je Modul × Modulanzahl | `JAHRESBETRAG`, keine Pflicht | **ja** — `PhotovoltaikVerguetungDialog` (§ 2.3) |
| **Heizkessel** | `Investitionskosten` · `Wartungskosten` mit Einheit (€/a \| €/kWh \| %/a) · Nutzungsdauer | `PROZENT_ENDENERGIEKOSTEN`, 4–8 % | nein |
| **Wärmepumpe** | `Modulkosten` (ganze Anlage) | `PROZENT_ENDENERGIEKOSTEN` | nein |
| **Solarthermie** | `Investitionskosten` je Kollektormodul × Anzahl | `JAHRESBETRAG` | nein |
| **Stromspeicher** | `Modulkosten` [€/kWh] + `Leistungskosten` [€/kW] + `Investition_Fix` + Verschleiß | `JAHRESBETRAG` | teilweise — `Tab_StromspeicherVariante` |
| **Pufferspeicher** | `Investitionskosten` | `JAHRESBETRAG` | nein |

**Nur der Stromspeicher** führt heute eigene anlagenbezogene Wirtschaftlichkeitsparameter
(Kapitalzins 3,0 %/a, Nutzungsdauer 20 a, Leistungspreis, Netzladeanteil, Preisquelle, SoC-Band).
Alle übrigen Anlagen erben den Projektrahmen.

## 2.2 `BhkwWirtschaftlichkeitDialog` (Hilfekennung `Form_BhkwWirtschaftlichkeit`)

**Gebaut** (§ 6.1, Etappe B5): `EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitDialog.razor`
mit der Windows-Hülle `WindowsFormsApplication1/Views/Wirtschaftlichkeit/BhkwWirtschaftlichkeitHuelle.cs`.
Der Dialog führt **acht Gruppen** an einem Ort, je Anlage pflegbar, mit Live-Vorschau aus **einem**
Rechenweg: die sechs unten beschriebenen, dazu „**Angaben der gewählten Anlage**" (aus der
Aufklappzeile der Gruppe 1 herausgelöst) und „**Kohärenzprüfung (Energie- und Stromsteuer)**".
Andockung: Knopf in der Fußleiste von `WirtschaftlichkeitSeite.razor` und Reiter „Ertrag/Bonus" der
Komponente — dasselbe Formular, Muster PV.

### Gruppe 1 — Anlagen (Tabelle mit Aufklappzeile)

Tabellenkopf je BHKW-Modul, alles nur lesend:

| Spalte | Quelle |
|---|---|
| Anlage | `Tab_Energieanlagen.Bezeichner` |
| P_el [kW] | `Tab_BHKW.Pel` |
| **Brennstoff** | **kein Leseweg vorhanden — Lücke K4**, kleiner Leser `CarrierId` → Name in B5 |
| Stichtag · Inbetriebnahme · Anlagenart | `KWKG_Stichtag` · `KWKG_Inbetriebnahme` · `KWKG_Anlagenart` |

**Aufklappzeile** — heute die eigene Gruppe „Angaben der gewählten Anlage" des gebauten Dialogs:
8 Bestandsfelder (die Maske `Form_KwkgModule` ist mit BK1 aufgelöst) plus 3 aus B5 plus den
Kostenanteil aus BK1. Durchgängig gilt: **leer oder 0 heißt „kein eigener Wert".** Für die fünf
KWKG-Größen heißt das seit Schemaschritt 89 **nicht mehr „Projektvorgabe", sondern 0** — § 7 und
§ 8 KWKG bemessen sie an der einzelnen Anlage, und der Schritt hat jeder Bestandsanlage den Wert
eingetragen, den ihr der Rückfall zugewiesen hat (Entscheid `BK-E-1` (a)).

| Feld | Typ | Bereich / Optionen | Spalte | Stand |
|---|---|---|---|---|
| Stichtag (Bestellung/Genehmigung) | Datumsfeld mit Kontrollkästchen | Haken aus = Projektwert | `KWKG_Stichtag` | Bestand |
| Inbetriebnahme | Datumsfeld mit Kontrollkästchen | dito | `KWKG_Inbetriebnahme` | Bestand |
| Anlagenart | Baustein `Auswahlfeld` | (nicht erfasst = Neuanlage) · neu § 8 Abs. 1 · modernisiert Abs. 2 · nachgerüstet Abs. 3 | `KWKG_Anlagenart` | Bestand |
| Eigenstrom nach § 6 Abs. 3 | Baustein `Auswahlfeld` | kein Tatbestand · Nr. 1 bis 100 kW · Nr. 2 Kundenanlage · Nr. 3 stromkostenintensiv | `KWKG_Eigenstromfall` | Bestand |
| Satz Einspeisung [ct/kWh] | Numerisch 0–30 | 0 = kein Zuschlag; **Knopf „Vorschlag übernehmen" am Feld** | `KWKG_Satz_Einspeisung` | Bestand, Knopf BK1 |
| Satz Eigenstrom [ct/kWh] | Numerisch 0–30 | 0 = kein Zuschlag; **Knopf am Feld** | `KWKG_Satz_Eigen` | Bestand, Knopf BK1 |
| Vbh-Kontingent [h] | Numerisch 0–200.000 | 0 = nach § 8 aus dieser Anlage abgeleitet; **Knopf am Feld** | `KWKG_Vbh_Kontingent` | Bestand, Knopf BK1 |
| Vbh-Jahresdeckel [h/a] | Numerisch 0–8.760 | 0 = Staffel | `KWKG_Vbh_Jahresdeckel` | Bestand |
| **Anteil Neuherstellungskosten [%]** | Numerisch 0–100 | 0 = nicht gepflegt; wählt mit der Anlagenart die Kontingentstufe § 8 Abs. 2/3 | `KWKG_Kostenanteil` | **neu BK1**, einziger Pflegeort seit BK1b |
| **Energiesteuerentlastung (Anlage)** | Baustein `Auswahlfeld` | (Projektwert) · keine · § 53 · § 53a · § 54 | `Energiesteuer_Wahl` | **neu B5** |
| **Brennstoff auf Strom/Wärme (Anlage)** | Baustein `Auswahlfeld` | (Projektwert) · voller Brennstoff · energetisch | `Aufteilung_Methode` | **neu B5** |
| **Hilfsenergieanteil [% des Endenergiebedarfs]** | Numerisch 0–100 | 0 = keine; Vorschlag BHKW 2–4 %. Bemessen wird am **Endenergiebedarf (Brennstoff)** dieser Anlage — nicht an den Kosten (Schemaschritt 94, Statuszeilen #365/#366) | `Hilfsenergie_Anteil` | **neu B5** |

Warnzeilen der Gruppe: Ausschreibung § 8a bei P_el > 500 kW · Stromsteuerbefreiung entfällt über
2 MW · Heizöl-Ausschluss ab Inbetriebnahme 2025.

### Gruppe 2 — Projektweite KWK-Angaben

Die Gruppe führt **nur, was wirklich projektweit ist** (Entscheid `BK-E-1` (a)):

| Feld | Bedeutung |
|---|---|
| Einspeisevergütung KWK-Strom [€/kWh] | die Vergütung des eingespeisten Stroms; der Zuschlag kommt obendrauf (Auftrag #325) |
| Abschlag Negativstunden [%] | gilt für alle Anlagen gleich |
| Pauschale § 9 KWKG | Σ P_el ≤ 2 kW, einmalig im Jahr 0 |
| Stichtag (Bestellung/Genehmigung, § 6) | Prüfkette der Förderfähigkeit |
| **Förderbeginn (Startjahr der Reihen)** | `KWKG_Inbetriebnahme`; er startet **alle** jahresscharfen Reihen (KWKG, CO₂, Steuern), auch ohne BHKW — deshalb heißt er hier nicht mehr „Inbetriebnahme, Vorgabe je Anlage" |

Fünf Felder — mehr steht hier nicht.

**Herausgenommen und an die Anlage gewandert:** Bonus Eigenstrom · Bonus Einspeisung ·
Vbh-Deckel-Override · Vbh-Kontingent gesamt · Eigenstrom-Tatbestand · Anlagenart § 8 · Anteil
Neuherstellungskosten. Eine leise Zeile unter der Gruppe sagt, wo sie jetzt stehen.

**Der Vorschlag steht am Feld, nicht als Sammelknopf** (Anwenderwunsch 17.09.2026): Unter jedem der
drei Felder Satz Einspeisung, Satz Eigenstrom und Vbh-Kontingent steht die Grundlage im Klartext
und daneben der Knopf „Vorschlag übernehmen". Er schreibt **nur sein eigenes Feld**, nur auf
Knopfdruck und nur in den Arbeitsstand:

```
Einspeisung 5,5667 ct/kWh — § 7 Abs. 1 KWKG 2025, Stichtagsjahr 2026:
                            50 × 8,00 + 50 × 6,00 + 150 × 5,00 + 50 × 4,40 = 1.670 ÷ 300  [Vorschlag übernehmen]
Eigenstrom  2,4167 ct/kWh — § 7 Abs. 2 mit § 6 Abs. 3 Nr. 2, Stichtagsjahr 2026:
                            50 × 4,00 + 50 × 3,00 + 150 × 2,00 + 50 × 1,50 =   725 ÷ 300  [Vorschlag übernehmen]
Kontingent  30.000 Vbh    — § 8 Abs. 1, neue Anlage                                       [Vorschlag übernehmen]
```

*Entscheid Q2 offen (Analyse vom 19.09.2026):* ob der Vorschlag am Feld bleibt oder als
Sammelknopf „Sätze und Herkunft…" mit der Überlagerung U22 zusammengeführt wird.

Fehlt eine Grundlage, ist der Knopf **weich** gesperrt (`aria-disabled`, Grund im `title` — ein
`disabled`-Knopf zeigt seinen Tooltip nie): keine elektrische Nennleistung, kein Tatbestand nach
§ 6 Abs. 3, keine Anlagenart.

### Gruppe 3 — Energiesteuer

Projektebene: Energiesteuerentlastung (keine · § 53 Formular 1131 · § 53a Abs. 5 Formular 1135 ·
§ 54 Formular 1450) · Brennstoff auf Strom/Wärme · Jahresnutzungsgrad [%] (0 = nicht erfasst,
bleibt Projektgröße — K5).

Herleitungslabel am Musterprojekt: `§ 53a Abs. 5 · Erdgas 4,42 €/MWh · 4.797,2 MWh = 21.203,4 €/a`
Kohärenzzeile in Firebrick, wenn der erfasste Brennstoffpreis die Energiesteuer nicht ausweist.

### Gruppe 4 — Stromsteuer

Unternehmensart (führend, BW4) · Räumlicher Zusammenhang 4,5 km · Hocheffizienz nachgewiesen ·
**zwei Sprungknöpfe „Strombezug…" und „BHKW-Tarif…"** · **Feld „Modus § 9 Abs. 1 Nr. 3"**
(ERLOES/AUSWEIS, Vorgabe AUSWEIS) — Spalte `Stromst_Befreiung_Modus`, Schemaschritt 88
(K3 erledigt mit B6, Statuszeile #328). Beide Sprünge speichern nur, wenn der Arbeitsstand vom
geladenen Stand abweicht.

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

## 2.3 `PhotovoltaikVerguetungDialog` (Hilfekennung `Form_PhotovoltaikVerguetung`) — Bestand, zugleich Stilmuster

**Sieben Gruppen untereinander**, einspaltig, in der Reihenfolge des Dialogs; Kopfband mit
Kontrollkästchen „Vergütung anwenden". Das Wunschmaß des Fensters (914 × 724, fest) ist keine
Eigenschaft der Razor-Komponente, sondern eine Konstante der Windows-Hülle
(`PhotovoltaikVerguetungHuelle.FENSTER_BREITE`/`FENSTER_HOEHE`).

| Gruppe (Name im Dialog) | Felder |
|---|---|
| **Anlage** | Installierte Leistung (rechnerisch, fett) · Override [kWp] (0 = keiner) · Inbetriebnahme · **Degradation [%/a]** (0 = keine; mit #348 aus dem Kostenbild hierher verlegt) · `Optionsgruppe` Überschuss-/Volleinspeisung · Warnzeile: > 1 MW Ausschreibung ⇒ AW-Override nötig; > 2 MW Stromsteuer prüfen |
| **Anzulegender Wert** | AW_mix (10 pt fett) · Herleitungszeile · AW-Override (0 = Katalog) · abgeleitete feste EV (AW − 0,40) |
| **Vermarktung** | `Optionsgruppe`: Feste Einspeisevergütung (nur ≤ 100 kW) · Direktvermarktung mit Marktprämie · Sonstige DV/PPA · Keine Vergütung (< 200 kW). Felder: DV-Entgelt [ct/kWh] (0,40) · PPA-Festpreis · PPA-Aufschlag auf Spot. Hinweis zu § 21c/§ 21 |
| **Vergütungsausfall (§ 51 / § 51a)** | Anwenden: Automatisch/Ja/Nein · Statuszeile · iMSys-Einbaujahr (0 = keins) · Ausfallanteil [%] (20,0) · Kontrollkästchen § 51a-Kompensation |
| **Strompreis / Bezugsbewertung** | Kontrollkästchen „Netzbezug stundenscharf aus Preiszeitreihe bewerten" · Stromsteuerhinweis § 9 |
| **60-%-Wirkleistungsbegrenzung (§ 9 Abs. 2)** | Anwenden: `Auswahlfeld` · Statuszeile |
| **Σ Vorschau** | Einspeisung MWh/a · Satz Jahr 1 · Erlös Jahr 1 · Vergütungsausfall · § 51a-Gutschrift · Kennzahlzeile |

Fußleiste: Marktwerte importieren… · Einspeise-Tarif… · Übernehmen · Abbrechen.

## 2.4 `WirtschaftlichkeitParameterDialog` (Hilfekennung `Form_WirtschaftlichkeitParameter`) nach dem Auszug

Bleibt: **Allgemein** (Kalkulationszins · Betrachtungszeitraum · Preissteigerung Energie ·
Preissteigerung Betrieb · **Vergleichsprojekt** je Gruppe — die Referenz der Differenzrechnung nach
§ 2.9, Schemaschritt 92, NULL = Stammprojekt) · **Strom** (Einspeisevergütung PV ·
Einspeisevergütung KWK · Anzeige-Kontrollkästchen Aufschläge) · **BEHG** (CO₂-Preis, 0 = Pfad ·
Herkunftszeile · Katalogknopf · Referenz-Kraftwerkspark · Referenzkessel) · **Bilanzierung**
(Bilanzjahr · Emissionsmethode · Biomasse-Konvention · Nachhaltigkeitsnachweis).

**Dazu der Szenarien-Parametersatz** (Zins, Preissteigerungen Energie und Betrieb, Nutzungsdauer,
Betrachtungszeitraum je Worst/Best) — die Tafel steht in
[`../Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md`](../Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md)
§ 4 und wird hier gepflegt; die Spalten `Szen_Best_*`/`Szen_Worst_*` liegen seit Schritt 71 an
`Tab_ProjektWirtschaftlichkeit` (§ 2.11.5).

Zieht aus: die 11 KWKG-Felder und die 6 Steuerfelder. Danach vier Gruppen statt sechs, plus
Statuszeile und Sprungknopf „BHKW…". Der Dialog war mit 26 Feldern am Kapazitätslimit — das ist der
sachliche Grund für den Auszug.

## 2.5 Preis- und Trägerdialoge

| Block | Inhalt |
|---|---|
| **Strompreis Details** (`StrompreisDetails`) | Beschaffung (Rest-Vorschlag) · Vertrieb 0,200 · Netzentgelt 6,440 · Stromsteuer 2,050 (reduziert 0,050) · Konzessionsabgabe 0,110 · Umlagen 2,946 ct/kWh, wahlweise als KWKG 0,446 / Offshore 0,941 / § 19 StromNEV 1,559 einzeln; je Aktiv-Schalter, Live-Summe, Kohärenzzeile gegen den Arbeitspreis und Knopf „In Arbeitspreis übernehmen"; Schnellwahl katalogbasiert, Unternehmensart hebt den passenden Knopf hervor |
| **Preisbestandteile des Brennstoffs** (`BrennstoffBestandteile`) | Energiesteuer · CO₂ · Netz-/Messentgelt · Vertrieb, je Aktiv-Schalter, Schnellwahl aus dem Katalog; Summe der aktiven Bestandteile, Kohärenzzeile gegen den Arbeitspreis, nicht aufgeschlüsselter Rest (negativ in Warnfarbe) und Knopf „In Arbeitspreis übernehmen". **Kein Modus** — ein leeres Feld heißt „kein Anteil" und bekommt keinen Vorschlagswert; **ohne Preiswirkung**, reine Transparenz und Kohärenzgrundlage. Dieselbe Regel wie bei „Strompreis Details" |
| **Vergütungssätze** | `Tab_ProjektWirtschaftlichkeit.Einspeiseverguetung` bzw. `Einspeiseverguetung_KWK` [€/kWh] je Projekt — die eine Quelle für Wirtschaftlichkeit und Speicherwelt |

### Die Trägerkarte: Einheiten, Preishistorie, Katalogübernahme

**Heizwert und Brennwert sind Stoffwerte je Abrechnungseinheit.** Sie stehen in
`kWh/<Abrechnungseinheit>` (`energy_carrier.billing_unit`: Nm³, L, kg, kWh) und werden **nie** mit
der Preisbasis umgerechnet — ein Brennstoff hat seinen Heizwert, gleichgültig worin man ihn
abrechnet. Gespeichert werden `custom_hi`/`custom_hs` bzw. `hi_kwh_per_unit`/`hs_kwh_per_unit`
immer in dieser Einheit.

**Nur der Arbeitspreis folgt der Preisbasis.** Die Klappliste „Preisbasis" sagt, in welcher
Einheit der Anwender ihn eingeben will; angezeigt wird `Basiswert ÷ Faktor`, gespeichert wird der
Basiswert je Abrechnungseinheit, und die gewählte Basis geht als `ID_Umrechnung` mit. Beim
Öffnen mit gespeicherter Preisbasis wird die Anzeige umgerechnet. Der **Leistungspreis** bleibt in
`€/(kW·a)` bzw. `€/(kW·Monat)`, der **Grundpreis** in `€/a`; beide kennen die Preisbasis nicht.

**Formelzeile und Effektivprüfung rechnen über die Basiswerte** — Arbeitspreis je
Abrechnungseinheit ÷ Heizwert je Abrechnungseinheit — und nennen die Einheiten:
„0,50 €/Nm³ ÷ 10,50 kWh/Nm³ = 0,0476 €/kWh"; bei Preisbasis kWh kommt „Direktabrechnung:
0,0476 €/kWh" dazu. Rechnet der Träger unmittelbar nach kWh ab (Strom, Fernwärme), steht
„Direktabrechnung nach kWh". Die Rechnung selbst liegt einmal im Kern
(`EnergietraegerPreiskarte`); der Riegel `EnergieEinheitenPruefung.ErreichtKwh` fragt über
derselben Abrechnungseinheit.

**Die Preishistorie gehört dem Kontext.** Im Projektkontext zeigt die Tabelle die Zeilen des
Projekts (`energy_price.ID_Projekt`); gelesen wird beim Trägerwechsel und nach jedem
erfolgreichen Speichern. Eine Zeile entsteht, wenn sich ein Wert gegenüber dem gespeicherten
Stand geändert hat, zum Datum aus dem Feld „Gültig ab"; ein zweites Speichern am selben Tag
aktualisiert sie, statt eine zweite anzulegen. Die Spalte „Heizwert" trägt ihre Einheit im Kopf.
Im **Katalogkontext** entsteht **keine** Historienzeile — `energy_price.ID_Projekt` trägt einen
Fremdschlüssel auf `Tab_Projekt.ID`, und das Projekt 0 gibt es nicht; die Karte nennt den Grund
unter der Tabelle, geschrieben wird dort die Katalogzeile selbst.

**Die Katalogübernahme ist eine einmalige Kopie.** Der Knopf „Katalogwerte übernehmen" steht nur
im Projektkontext und holt Arbeits-, Grund- und Leistungspreis, Heiz- und Brennwert sowie die drei
Emissionswerte aus der Katalogzeile in die Felder; die Preisbasis geht dabei auf die
Abrechnungseinheit zurück. Die Karte meldet „Katalogwerte übernommen — noch nicht gespeichert";
geschrieben wird erst mit „Speichern" bzw. „OK", und dabei entsteht die Historienzeile. Das
Projekt folgt dem Katalog danach **nicht** — eine spätere Änderung im Katalog lässt die
Projektwerte, wo sie sind.

### Emissionsanzeige der Energieträgertabelle (Auftrag 30.08.2026) — umgesetzt

> **Stand: umgesetzt.** Die Tabelle trägt eine Emissionsspalte; Kopf und Inhalt folgen
> `Tab_Projekt.Emission_Berechnungsmodus` (`EmissionsAusweis.SpaltenkopfEmission`), der Kurztext
> trägt die Herleitung nach den drei Fällen unten (`EmissionsAusweis.HerleitungEmission`). Ein
> stiller Rückfall CO2E → CO2 findet nicht statt: Fehlt der Artenkatalog, steht in der Spalte der
> reine CO₂-Faktor und der Kurztext sagt es. Nachweis `EPOS.Kern.Tests/EmissionsspalteTests`
> (sieben Fälle, beide Sprachen) und `EPOS.UI.Tests/Seiten/KostenSeiteTests`.

**Ist-Zustand vor der Umsetzung.** Die Tabelle „Energieträger des Projekts" auf der Kostenseite führt **drei feste
Emissionsspalten** — CO₂ [g/kWh], SO₂ [mg/kWh], NOx [mg/kWh] (`EPOS.UI/Seiten/Berichte/KostenSeite.razor` mit `KostenSeiteGaben.cs:658`, aus BK1).
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

## 2.6 Eigene Rubrik „Erlöse und Vorteile" (Auftrag 30.08.2026) — umgesetzt

> **Stand: umgesetzt.** Die Rubrik lebt als Block in `WirtschaftlichkeitZeilen.Kennzahlen` —
> **einer** Definition für Ergebnisreiter, Word, Excel und die Vorschau des BHKW-Dialogs
> (Gruppe 6). Block A trägt die Kennung `WirtZeile.BLOCK_A` und geht in die Summenzeile
> `ERL_A_SUMME`, die genau die Zeilen über ihr summiert; Block B trägt `BLOCK_B` und kommt in
> keine Summe — die Trennung ist kein Flag, sondern zwei verschiedene Wege in die Liste
> (`Erloes()` gegen `Ausweis()`). Nachweis `EPOS.Kern.Tests/ErloesrubrikTests`.
>
> **Drei Abweichungen von der Tabelle unten, jede aus einer Messung:**
>
> - **A1 und A2 stehen in einer Zeile** „KWK-Zuschlag (§ 7 KWKG)" mit zwei Unterzeilen
>   „davon Einspeisung" und „davon Eigenstrom". Die Aufteilung kommt aus dem Modulnachweis
>   `KwkgModulNachweis`; er reist im Nachweisumschlag des Ergebnisses mit, die beiden
>   Unterzeilen erscheinen deshalb auch beim gebuchten Stand.
> - **A3 (Pauschale § 9 KWKG) hat eine eigene Zeile** „KWKG-Pauschale (§ 9 KWKG)" hinter dem
>   KWK-Zuschlag — aber **nicht in der Summe** des Blocks A. Ihr Betrag steht in **€**, nicht in
>   €/a: eine einmalige Zahlung im Jahr 0, die in einer €/a-Summe ein Einheitenfehler wäre, wie
>   der Restwert darunter. Der Titel nennt beides („[€, einmalig im Jahr 0]"). Die Zeile
>   erscheint nur, wenn die Pauschale greift (Σ P_el ≤ 2 kW und Schalter gesetzt); über der
>   Grenze bleibt der Schalter ohne Wirkung, und der Hinweis des Laufs sagt das. Der Betrag reist
>   im Nachweisumschlag mit (`KwkgPauschaleEur`, Fassung 2), steht also auch beim gebuchten
>   Stand. Nachweis `EPOS.Kern.Tests/KwkgPauschaleZeileTests`.
> - **A4 und A5 stehen in einer Zeile** „Energiesteuer-Entlastung (§ 53/§ 53a bzw. § 54
>   EnergieStG)": `SteuerErgebnis.EnergiesteuerEur` ist eine Summe, ihre Trennung wäre eine neue
>   Größe im Rechner und damit ein Umbau, den die Etappe ausschließt.
> - **A10 (Restwert) steht nicht in der Summe des Blocks A.** Er ist ein Barwert über T; in einer
>   €/a-Summe des Jahres 1 wäre er ein Einheitenfehler. Seine Zeile bleibt beim Nettobarwert.
>
> **Die Nullzeile mit Grund** (Anwenderbefund 17.09.2026 „Vergütungen und Reduktionen sind in den
> Ergebnissen nicht dargestellt"): Eine A-Zeile erscheint, sobald das Projekt eine Anlage führt,
> für die die Position gilt — auch bei Betrag 0, dann mit dem Klartext der fehlenden Grundlage
> („0 — kein KWK-Zuschlagssatz gepflegt oder Kontingent erschöpft"). Excel bekommt weiterhin die
> blanke Zahl, damit Filter und Diagramme des Blattes numerisch bleiben.

Die Erlösseite bekommt eine **eigene Rubrik** — im Ergebnisreiter, im BHKW-Dialog als Vorschau und
im Bericht. Sie ist in **zwei Blöcke** geteilt, und diese Teilung ist keine Kosmetik: Block B darf
nicht addiert werden.

### Block A — zahlungswirksam (geht in den Kapitalwert)

| # | Position | Rechtsgrundlage | Menge × Satz | Laufzeitbegrenzung |
|---|---|---|---|---|
| A1 | **KWK-Bonus Einspeisung** | § 7 Abs. 1 KWKG | eingespeister KWK-Strom × Mischsatz (marginale Staffel) | Vbh-Kontingent § 8 · Jahresdeckel · Stichtag 31.12.2026 |
| A2 | **KWK-Bonus Eigenstrom** | § 7 Abs. 2 KWKG | eigengenutzter KWK-Strom × Mischsatz | zusätzlich: **Tatbestand § 6 Abs. 3 zwingend** |
| A3 | KWKG-Pauschale (≤ 2 kWel) | § 9 KWKG | 0,04 €/kWh × 60.000 Vbh × P_el | einmalig im Jahr 0, schließt A1/A2 aus; eigene Zeile in €, **nicht in der Summe** |
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

**Umgesetzt.** Die vermiedene MENGE ist die Bemessungsgröße, nicht der Netzbezug: nur sie
unterscheidet die beiden Seiten der Differenz (`StromErloesErgebnis.VermiedenMengeMWh` =
Bedarf ohne Anlage − Restbezug). Der Entlastungssatz kommt jahresgenau aus dem Gesetzeskatalog
(`GESETZ_STROMST_ENTLASTUNG_9B`), die Prüfung der Unternehmensart aus derselben Funktion, mit der
die Steuerrechnung rechnet (`SteuerGutschriftRechner.ProduzierendesGewerbe`). Die drei Größen
(`VermiedenMengeMWh`, `VermiedenEntlastung9bJahr`, `ProduzierendesGewerbe`) reisen zusammen mit
`BezugsspitzeKW` im **Nachweisumschlag** der Ergebniszeile (`ErgebnisNachweisUmschlag`, Spalte
`Nachweis_Json`); die Korrekturzeilen stehen deshalb auch beim gebuchten Stand. **Der Kapitalwert
ist unberührt:** Es wird keine Reihe angehängt und keine verändert.

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
Segoe UI 9 pt, Gruppentitel fett, Eckenradius 6 · **Fußknöpfe mindestens 88 × 44** (Zielgröße der
Berührfläche; die alte Angabe 110 × 30 war ein WinForms-Maß) · `InfoKnopf` 28 × 28 ·
`SpeichernLeiste` (nicht schließender Speichern-Knopf mit Statuszeile) · Razor-Komponenten **ohne
Designer**: Texte kommen über `[Parameter]`-Vorgaben und die `*Texte`-Bündel und werden in der
Hülle mit `Resource.*` bzw. `T(schlüssel, rückfall)` belegt.

**Die Fußleiste der Wirtschaftlichkeitsseite** (`EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor`)
führt **fünf Knöpfe** — Photovoltaik, BHKW, Strombezug, Verlauf…, Berechnen —, nach dem Wegfall von
„Verlauf…" vier. **Lücke K8 („kein Platz für einen achten Knopf") ist damit gegenstandslos.**
**Entschieden 18.09.2026 (K8, nach Empfehlung):** kein weiterer Knopf, sondern ein Umschalter
„Kennzahlen / ValERI-Bewertung" im Kopf der Seite — links die heutige Kennzahltafel, rechts die
Abschnitte der Ergebnisansicht (§ 2.13); das bestätigt zugleich V-1 (§ 2.11.4). Der Knopf
„Verlauf…" entfällt mit der Ergebnisansicht, weil der Verlauf dort steht.

*Entscheid Q9 offen (Analyse vom 19.09.2026):* Kopfband und Fehlerfarbe des Hausstils sowie die
Frage, ob die Bauformregeln als eigener Abschnitt „Hausstil Dialoge" geführt werden.

## 2.8 Betriebskosten-Raster der Kostenverwaltung — Entwurf B (übernommen 31.08.2026)

*Aus dem Artifact [Pflichtpositionen je Komponente](https://claude.ai/code/artifact/236c8a8a-a2e0-47f4-a099-aa1456de883a),
Entwurf B, auf Anwenderentscheid in dieses Konzept übernommen. Es ist die Spezifikation des offenen
Punkts „Live-Frisch-Anzeige der Bezugsgröße samt Herleitungszeile im Kostendialog"
(§ 6.3 Nr. 2, H21-Grenze 1).*

Das Raster der Kostenverwaltung (`KostenKomponenteDialog`, Hilfekennung `Form_KostenKomponente`)
gliedert sich in eine **Optionsgruppe „Betriebskosten / Investitionskosten"** und **zwei Reiter**
„Kosten Invest/Betrieb" (`KDLG_TAB_KOSTEN`) und „Ertrag/Bonus" (`KDLG_TAB_ERTRAG`). Sein Rasterkopf
führt die Spalten **Aktionen · Position · Bemessung · Satz · Betrag netto [€] · Nutzungsdauer [a] ·
Worst/Best**; Kostenart und Runde stehen im Zeileneditor bzw. in der Herleitungszeile. Gegenüber dem
Stand vor dieser Beschreibung ändert sich das Raster in **vier Punkten**:

**1. Pflichtzeilen stehen oben, hinterlegt, mit Schloss statt Papierkorb.**
Die Zeilen mit `IstPflicht` (Wartung, Instandhaltung der eigenen Komponente, Hilfsenergie) tragen
in der Aktionsspalte ein Schloss-Symbol; der Löschversuch führt zum Dialog „Satz auf 0 setzen"
(Löschsperre aus H3 — hier ihr Anzeige-Teil). Unter der Positionsbezeichnung steht der Vermerk
„Pflicht nach VDI 2067" bzw. der Empfehlungsbereich („Pflicht · üblich 3,0–9,0 %").

**2. Unter dem Betrag steht die Herleitung im Klartext** — Menge **und** Quelle, anlagenscharf;
sie nennt dort keine Runde (U28, umgesetzt mit #345):

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
- **Worst/Best** bleibt je Zeile über den ±-Knopf (`CaseEingabeDialog`, Hilfekennung
  `Form_CaseEingabe`), Speichern über die nicht schließende `SpeichernLeiste` mit Statuszeile
  („✓ Gespeichert 14:18").
- **Vier Rasterknöpfe:** „+ Position hinzufügen" · „Aus Vorlage übernehmen…" · „Positionskatalog…" ·
  „Nutzungsdauern vorbelegen…"; darunter die Dialogleiste „Abbrechen · Speichern · OK".

**Einordnung und Grenzen:** Der Umsetzungsstand je Punkt steht in § 6.1 und § 6.3. Der Klick auf einen
Betrag öffnet die vollständige Herleitung (Entwurf C im Artifact); die Mengenherkunft folgt den
Rechenwegen aus § 3.4. Die Herleitungszeile zeigt am Kessel die Menge des Laufs (Befund **B-1**);
beim Elektrokessel steht dort sein **Stromeinsatz** samt Betrag zum Arbeitspreis des Stromträgers
und der Herkunft „Strom · Netzbezug (im Reststrombedarf des Projekts bepreist)" — die Menge ist
sichtbar, bezahlt wird sie genau einmal, nämlich im Netzbezug (Regel **E1**).

## 2.9 Wählbares Vergleichsprojekt — die Referenz der Differenzrechnung (Anforderung 31.08.2026) — umgesetzt

> **Stand: umgesetzt.** Die Referenz ist je Vergleichsgruppe wählbar und steht in
> `Tab_ProjektWirtschaftlichkeit.ID_Referenzprojekt` (Schemaschritt 92, nullbar; NULL = Stamm).
> `WirtschaftlichkeitCtrl.Berechne` und `BerechneVerlauf` nehmen sie als **Parameter**; alle
> Differenzkennzahlen rechnen gegen sie, die Referenz selbst bekommt keine. Ihre Auflösung samt
> Randfällen und Nachweistexten steht einmal in `Referenzwahl`; die Zeilendefinition kennzeichnet
> die Referenzspalte (`WirtZeile.IdReferenz`), und die Vergleichsgruppen-Liste führt die Wahl.
> Referenz = Stamm rechnet bitgleich zum Bestand. Nachweis
> `EPOS.Kern.Tests/ReferenzprojektTests` und `EPOS.UI.Tests/Seiten/WirtschaftlichkeitSichtTests`.

**Anforderung des Anwenders:** Die Differenzrechnung soll nicht fest gegen das Stammprojekt laufen —
**die Referenz (das Vergleichsprojekt) soll wählbar sein.**

**Ist-Zustand vor der Umsetzung — die Referenz war hart verdrahtet.** Das Stammprojekt war überall die
Unterlassensalternative: `Kapitalwertdifferenz = KW(Variante) − KW(Stamm)`; Annuität, dynamische
Amortisation und IZF rechneten ausschließlich auf dieser Differenz; der Stamm war als Referenz
**nicht abwählbar**, und die Nachweiszeile sagte fest „Referenz: Stammprojekt". Eine Auswahl
existierte nirgends.

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
| Anzeige | Referenzspalte der Vergleichstabelle markiert; in der Variantenliste (eine `Optionsgruppe` je Zeile, kein ListView) ist die **gewählte** Referenz nicht abwählbar (heute: der Stamm); Nachweiszeile nennt die Referenz beim Namen („Referenz: ‹Variantenname›") statt der festen Formel |
| Bericht | Word und Excel übernehmen die Benennung aus derselben Zeilendefinition (`WirtschaftlichkeitZeilen`) — keine dritte Wahrheit |
| ValERI-Sicht | die gewählte Referenz **ist** die Unterlassensalternative im Sinne der Norm; der ValERI-Bewertungsbericht weist sie als solche aus |

**Randfälle, ausdrücklich geregelt:**

- Gewählte Referenz **ohne Simulationsergebnis** → der Sammler rechnet sie nach (Bestandsverhalten
  für jede Variante); scheitert das, Abbruch mit Fehlgrund — nie stiller Rückfall auf den Stamm.
- Gewählte Referenz **gelöscht** oder nicht mehr in der Gruppe → Rückfall auf Stamm **mit
  Warnzeile**, die den Rückfall benennt; die Spalte wird nicht still bereinigt.
- Referenz = Stamm (Vorgabe) → Verhalten byte-gleich zum Bestand; das ist das Abnahmekriterium der
  Etappe.

**Einordnung:** Umgesetzt als eigene, kleine Etappe — ergebnisneutral in der Vorgabe, erste
Rechenwirkung erst bei ausdrücklicher Wahl einer anderen Referenz. Sie steht **vor** der
ValERI-Berichtsetappe, weil deren Bewertungsbericht die Unterlassensalternative benennen muss.

## 2.10 Integrationsort der ValERI-Darstellung (Anwendervorgabe 31.08.2026)

**Die ValERI-Blöcke werden Bestandteil der Seite „Berichte && Kosten → Wirtschaftlichkeit"
(`EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor`, Hilfekennung `UcWirtschaftlichkeit`) — kein separater Dialog.** Die Seite trägt bereits heute den Titel
„Wirtschaftlichkeit — Kapitalwertmethode (DIN EN 17463)" und die passende Grundausstattung:
Vergleichsgruppen-Liste (mit der Referenzwahl je Gruppe, § 2.9), Szenariowahl, vier
Kennzahl-Kacheln, Vergleichstabelle (Zeilen × Projekte, `<table class="epos-raster epos-matrix">`), Parameternachweis und Fußknöpfe.

**Andockvorschlag** (Einzelheiten in den ValERI-Mockups):

| Element | Ort auf der Seite |
|---|---|
| Referenzwahl (§ 2.9) | in der Vergleichsgruppen-Liste, ein Optionsfeld je Zeile; die gewählte Referenz ist nicht abwählbar |
| Die fünf ValERI-Blöcke (Investition · Betrieb · Erlöse · Energie · Wirtschaftlichkeit über Nutzungsdauer) | unterhalb der Vergleichstabelle als auf-/zuklappbare Abschnitte **oder** als zweite Ansicht der Seite (Umschalter „Kennzahlen / ValERI-Bewertung") — Entscheidung am Mockup |
| Kumulierter diskontierter Cashflow | inline in den Block „Wirtschaftlichkeit über Nutzungsdauer"; der vorhandene Verlauf-Dialog bleibt als Vollbild-Absprung |
| ValERI-Bewertungsbericht (Anhang E der Norm) | als Baustein der **Bericht**-Seite (Word/Excel), gespeist aus derselben Zeilendefinition |

Damit bleibt die Regel „eine Wahrheit je Größe": Die ValERI-Ansicht **rendert** die vorhandenen
Ergebnisse (`WirtschaftlichkeitErgebnis`, `WirtschaftlichkeitZeilen`, Verlaufsreihe) — sie rechnet
nichts Eigenes.

## 2.11 ValERI (DIN EN 17463) — Integration und Darstellung (Auftrag 31.08.2026)

*Quellen: der Normtext DIN EN 17463:2021-12 (vom Anwender bereitgestellt, vollständig ausgewertet —
alle Anforderungen hier paraphrasiert, Abschnittsnummern der Norm in Klammern) und als reales
Zahlenbeispiel die Altmappe `Quellen\BHKWPlan\BHKW_Höfingen_Erneuerung_20kWel.XLS`, Blatt
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

**Vorsicht bei den Nummern:** Dieses Papier zählt `V-G1…V-G12`, das Szenarienkonzept zählt `G1…G11`
— dieselben Ziffern meinen Verschiedenes. Weil dieses Papier die führende Fassung ist, gelten hier
die `V-G`-Nummern; die Tafel übersetzt:

| hier | Szenarienkonzept | Gegenstand dort |
|---|---|---|
| V-G1 Preisschwankungsraten | **G2** | Preisänderung je Kostenart — umgesetzt |
| V-G2 Degradation | **G3** | Degradation — **Entscheid A5 vom 20.09.2026 (nach Empfehlung)**: Der Entscheid „G3 nicht umsetzen" gilt, V-E (§ 2.11.4) wird **ohne** Degradation geplant; die Vereinfachung bleibt offengelegt |
| V-G6 Sensitivität mit Steigung €/% | **G6** | nicht monetisierbare Wirkungen — **andere Sache**, trotz gleicher Ziffer |
| V-G10 Formelbericht | **G10** | Herleitung Eigen/Einspeisung — **andere Sache** |
| V-G11 nicht monetisierbare Wirkungen | **G11** | investitionsgekoppelte Betriebskosten — **andere Sache**; der Freitext ist mit W5‑B‑12/G6 gebaut |

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
| V-G10 | **Bericht** mit Pflichtinhalten a)–d) + **editierbarer XLSX mit Formeln** nach Anhang-A-Raster (9) | Excel-Export existiert (ClosedXML), aber als **Werte** — der Generator schreibt keine einzige Formel, und keine Zahl des Parametersatzes erreicht eine Zelle (gemessen 18.09.2026) | **größte Einzellücke mit hartem Muss**. **Entschieden 18.09.2026, abweichend von der Empfehlung: der ganze Bericht formelbasiert**, soweit ableitbar — Stufenplan und die Liste dessen, was dauerhaft Wert bleibt, in § 2.11.6; das ValERI-Blatt (Parameterblock mit absoluten Bezügen, Periodenspalten, Gesamt-/Barwert-/NPV-Zeile je Szenario) ist darin Stufe 0 und 1 |
| V-G11 | **Nicht monetarisierbare Wirkungen**: erfassen, kategorisieren (Energiefluss / finanziell / sonstig), beurteilen nach Dauer × Wirkung auf Organisation/Mitarbeiter/Umwelt (6.1, 8.2) | **Freitext umgesetzt** (W5‑B‑12/G6 des Szenarienkonzepts) | es fehlen **Kategorie und Beurteilung** nach Dauer × Wirkung; fließt nie in den NPV, immer in den Bericht |
| V-G12 | **Anhang-E-Checkliste** (15 Punkte, Note 1–5) | fehlt | als Abschlussseite des Berichts; zugleich interne Abnahmecheckliste der Etappe |

**Anhang D der Norm ist eine BHKW-Fallstudie** (90 kW_th, 18 Jahre, NPV 64.480 €, Worst −202.802 €,
Best +598.320 €) — sie dient der Etappe als **externe Gegenprobe**: EPOS muss mit denselben
Eingaben dieselben Zahlen treffen. *(Vorsicht: Zwei Zeilen der Sensitivitätstabelle D.6 tragen im
Normtext versehentlich Werte des Pumpenbeispiels — als Prüfreferenz ungeeignet, dokumentiert.)*

### 2.11.3 Die fünf Darstellungsblöcke

Andockung nach § 2.10 in der Wirtschaftlichkeitsseite (`WirtschaftlichkeitSeite.razor`), Referenz nach § 2.9. Alle Blöcke rendern
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

Die Spalte „entspricht / bereits geliefert durch" löst die zweite Etappenreihe des
Szenarienkonzepts (`W5‑B‑9` … `W5‑B‑12`) gegen diese auf — beide Reihen meinen teilweise dieselbe
Arbeit.

| Etappe | Inhalt | entspricht / bereits geliefert durch | Wirkung | Stand im Etappenplan E0–E12 |
|---|---|---|---|---|
| **V-A** | Ausweis: „nachrichtlich"-Kennzeichnung der Kacheln, IZF-Mehrdeutigkeitswarnung, Deklarationszeilen, Steigungsspalte der Sensitivität | offen | keine | **E5** (mit der Ergebnisansicht) |
| **V-B** | Referenzwahl (§ 2.9) — umgesetzt | Etappe **VG**, Statuszeile **#358**, Schemaschritt 92 | keine in der Vorgabe | gebaut |
| **V-C** | ValERI-Ansicht (fünf Blöcke + Cashflow-Chart) in der Wirtschaftlichkeitsseite | offen | Ausweis | **E8** |
| **V-D** | XLSX-Formelbericht nach Anhang-A-Raster + Berichtsinhalte a)–d) + Anhang-E-Checkliste; **Gegenprobe an der Anhang-D-Fallstudie** | deckt sich mit **V-G10** (Entscheid 18.09.2026, § 2.11.6) | Ausgabe | **E8** |
| **V-E** | Vollständige Szenarioabdeckung nach § 2.11.5 (V-G5, Umfang entschieden 31.08.2026), Risiko (V-G7), n-jährliche Zeitpunkte (V-G3) — **ohne Degradation (V-G2), A5** | Szenarioabdeckung und Freitext teils geliefert durch **W5‑B‑9** und **W5‑B‑12** (Migrationsschritte 71, 72) | **ja** — je Pflege, mit A/B-Nachweis; NULL = wie Erwartet hält die Etappe bis zur ersten Pflege ergebnisneutral | **E9** |

*Die Spalte „Stand" verweist auf den Etappenplan E0–E12 des Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md) § 5;
gebaut sind daraus E0 (#379), E1 (#380) und E2 (#405).*

**Entscheid A5 (20.09.2026, nach Empfehlung): V-E rechnet die Degradation nicht ein.** V-E nannte die
**Degradation (V-G2)** als Teil der Etappe; das Szenarienkonzept führt dieselbe Sache als `G3` mit dem
Entscheid vom 09.09.2026 „nicht umsetzen". Der Entscheid vom 20.09.2026 löst den Widerspruch zugunsten
des Szenarienkonzepts auf: **V-E wird ohne Degradation geplant**, die Vereinfachung bleibt offengelegt
(Szenarienkonzept § 9.4). Die Nummerierung `V-G2` ↔ `G3` bleibt, wie sie ist — die Übersetzungstafel
steht am Anfang von § 2.11.2.

| Nr. | Entscheidungsfrage | Empfehlung |
|---|---|---|
| **V-1** | Fünf Blöcke als Aufklappabschnitte unter der Vergleichstabelle oder als zweite Ansicht mit Umschalter „Kennzahlen / ValERI-Bewertung"? | Umschalter — die Seite ist schon voll. **Entschieden 18.09.2026 nach Empfehlung** (zusammen mit K8, § 2.7): Umschalter im Kopf der Seite |
| **V-2** | XLSX-Formelexport: nur das ValERI-Blatt oder den ganzen Bericht formelbasiert? | Empfehlung war: nur das ValERI-Blatt (drei Szenariotabellen + Parameterblock), der übrige Bericht bleibt Werte. **Gekippt 18.09.2026 durch den Entscheid zu V-G10: der ganze Bericht, soweit ableitbar** — was ableitbar ist und was Wert bleibt, steht in § 2.11.6 |
| **V-3** | IZF/Amortisation von den Kacheln nehmen oder mit „nachrichtlich"-Label behalten? | behalten mit Label — Anwender kennen die Größen, die Norm verlangt nur die richtige Einordnung |
| **V-4** | Szenario-Parametersätze (V-G5) sofort oder nach V-A–V-D? | **Umfang entschieden** (§ 2.11.5). **Zeitpunkt entschieden 18.09.2026 nach Empfehlung: danach**, einzige Etappe mit Rechenwirkung, eigener A/B-Nachweis — **mit Hinweistext** bis dahin (§ 2.11.7) |

### 2.11.5 Vollständige Szenarioabdeckung (Entscheidung 31.08.2026)

**Anwenderentscheid:** Alle Parameter — Investitionskosten, Energiekosten, Betriebskosten,
Erlöse, dazu Rahmen und Mengen — werden mit Best- und Worst-Case-Werten versehen.

**Das Prinzip ist eine Fortschreibung, kein Neubau:** Die Kostenpositionen haben das Muster schon —
`BestCase`/`WorstCase` je Zeile, VALERI-Vorrang (0/NULL = wie Erwartet), Pflege über
`CaseEingabeDialog` (±-Knopf; Hilfekennung `Form_CaseEingabe`). **Dasselbe Muster wandert an jeden Ort, an dem ein Erwartet-Wert
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
| **Rahmen** | `Tab_ProjektWirtschaftlichkeit`: `Zinssatz`, `Betrachtungszeitraum`, `Preissteigerung_Energie`, `Preissteigerung_Betrieb` | je Größe `_Best`/`_Worst` (8 Spalten), NULL/0 = wie Erwartet | **6 von 8 vorhanden** seit Schritt 71 (`Szen_Best/Worst_Zins`, `_Preis_E`, `_Preis_B`); neu sind allein Best/Worst des **Betrachtungszeitraums**. Namensvorsicht: `Szen_*_Dauer` ist die **Nutzungsdaueränderung**, nicht der Betrachtungszeitraum |
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
- **Pflege**: der vorhandene ±-Knopf (`CaseEingabeDialog`) als einheitliches Muster auch an
  Trägerpreisen, Erlösfeldern und der Rahmen-Gruppe; die ValERI-Ansicht zeigt je Szenario, welche
  Parameter gepflegte Abweichungen tragen („12 von 31 Parametern szenariert").
- **Ausweis im Bericht** (Norm 9c): Die Kalkulationstabelle je Szenario nennt die
  Parametereinstellungen vollständig — die Szenariospalten der Rahmenzeile erscheinen im
  Parameterblock des XLSX-Blatts.

### 2.11.6 Formelbericht — Stufenplan und Grenze (Entscheid 18.09.2026 zu V-G10)

**Der Anwender hat abweichend von der Empfehlung entschieden: der ganze Bericht formelbasiert,
nicht nur das ValERI-Blatt.** Das kippt V-2. Damit das Konzept nichts Unmögliches verspricht, ist
die Grenze am Generator gemessen (`ExcelBerichtGenerator`, eine Klasse, 1 412 Zeilen (gemessen 19.09.2026), vier
Blattarten; adversarisch gegengelesen):

- Der Generator schreibt heute **keine einzige Formel** — `.Value` mit Zahl oder Text, Zahlenformat,
  Füllfarbe, Autofilter, Freeze; keine benannten Bereiche, keine Excel-Tabellen, keine Diagramme,
  keine Bilder. Er greift **nur fünfmal** auf den Parametersatz zu (Nachweis, `SatzFuer`,
  `NichtMonetaer`, Betrachtungszeitraum als Rechenargument, `IdKraftwerkspark`) — **keine Zahl des
  Parametersatzes erreicht eine Zelle**; die Annahmen stehen als ein zusammengesetzter Text in einer
  einzigen grauen Zelle.
- Vier Annahmen der ersten Messung hat die Gegenlesung widerlegt, und sie begrenzen den Plan: Die
  Spalte *Betrieb* ist **keine** einfache Fortschreibung (zwei verschieden eskalierte Töpfe mit p_B
  und p_E, deren Basen bei Positionen mit späterem Startjahr springen); die Sensitivitätstafel hat
  **fünf** Zeilen und ist **nicht** als Mehrfachoperation darstellbar (jede Zeile ist die Differenz
  zweier neu gerechneter Zahlungsbilder, die fünfte streicht die KWKG-Reihe ganz, der
  Investitionsausschlag koppelt in die abgeleiteten Betriebskosten); die Kosten-Kennzahlen sind
  **nicht** Menge × Preis (Grundpreise je Träger und ein Leistungspreisanteil aus der
  **Viertelstunden**-Bezugsspitze — feiner als das Stundenraster); die Spalte *Herleitung* bleibt
  nötig (fester Betrag, szenariogepflegter Wert, fehlende Menge oder fehlender Satz).

**Stufenplan** — jede Stufe ein eigener Schritt mit Gegenprobe (die Mappe muss vor und nach der
Stufe dieselben Werte zeigen; die Formelfassung wird gegen die Wertfassung gehalten):

| Stufe | Inhalt | Warum in dieser Reihenfolge |
|---|---|---|
| **0** | **Parameterblock aus echten Zellen**: Kalkulationszins, Betrachtungszeitraum, die drei Preissteigerungen (Energie, Betrieb, Investition/Ersatz), je Szenario ein Satz; absolute Bezüge darauf | Voraussetzung für alles Weitere — solange die Annahmen Prosa in einer Zelle sind, lässt sich keine Formel verankern |
| **1** | **Mehrjahrestabelle** des Wirtschaftlichkeitsblatts (das Raster steht: Jahre als Zeilen, Zahlungspositionen als Spalten): *Energie* als Fortschreibung Jahr 1 × (1+p_E)^(t−1); *Netto* als Zeilensumme; *Barwert* als Netto × (1+i)^−t; *Kumuliert* als Laufsumme; *Betrieb* als zwei Terme (Betriebs-Topf mit p_B, Endenergie-Topf mit p_E) mit Stufenlogik oder Hilfsspalte für Positionen mit späterem Startjahr; *BEHG* nur im Rückfallzweig als Fortschreibung, mit jahresscharfer CO₂-Reihe bleibt sie zugelieferter Preispfad | größter Nutzen: genau diese Größen variiert der Anwender im Gespräch, und die Tabelle zieht mit |
| **2** | **Ergebniskennzahlen**: Nettobarwert und Annuität über NBW/RMZ auf die Spalten der Stufe 1; interner Zinsfuß und Amortisation über eine **Differenzreihe Variante − Referenz** (samt Restwert-Nominaldifferenz im letzten Jahr), die das Blatt heute nicht führt und je Variante bekommt; benannter Leerwert bei fehlendem Vorzeichenwechsel als Text, nicht als Zellfehler | die Kennzahlen hängen an Stufe 1 und an einer Reihe, die erst entstehen muss |
| **3** | **Betriebskostenblock**: Menge und Satz in eigene Spalten, Betrag als Produkt — nur für bemessene Positionen; die Spalte Herleitung bleibt für feste, szenariogepflegte und unvollständige Positionen. **Delta-Block** des Vergleichsblatts als Zellbezug (Wert − Referenz) / |Referenz| | kleine Blöcke gleicher Mechanik; kosmetisch |

**Dauerhaft Werte bleiben**, weil sie am Stundenlauf, an Katalog- und Datenbankzugriff oder an
Text hängen:

- Blatt *Übersicht* (Stammdaten, Variantenliste, Gewerke-Matrix);
- die **Kennzahlspalten des Vergleichsblatts** — auch die Gruppe *Kosten*: Energiekosten p. a. und
  Stromkosten Netzbezug enthalten Grundpreise je Träger und den Leistungspreisanteil aus der
  Viertelstunden-Bezugsspitze;
- alle **Detailblätter** samt Monatswerten (Aggregat der Stundenreihen; die Brennstoffmenge wird
  dort heute als Text geschrieben);
- die **Strommengen-Matrix** nach Tarifzonen (Zonenzuordnung je Stunde, stundenweises Minimum für
  den KWK-Eigenstrom), die Bezugsspitze, die **Emissionsbilanz**, der **KWK-Modulblock**;
- die **Sensitivitätstafel** (fünf Zeilen, jede eine Differenz zweier Zahlungsbilder);
- die Textbausteine (Parameter- und Tarifnachweis, Bilanzkonvention, Veraltet-Warnungen,
  Empfehlungssatz, nicht monetäre Wirkungen) und die Gesetzeslogik mit Katalogzugriff
  (KWKG-Kontingente und ihr Auslaufen, Befreiungs- und Entlastungstatbestände, BEHG-Pflichtigkeit,
  Preisstände).

**Drei Sätze, die der Formelbericht trägt:** Er rechnet die Bewertung bereits feststehender
Jahresmengen nach — jede Änderung an Anlagengröße, Bedarf oder Fahrweise verlangt einen neuen
Stundenlauf, den keine Zellformel liefert. Die Gesetzeslogik und die Nachweise, die den Zahlen ihre
Gültigkeit geben, sind nicht abbildbar. Und was der Anwender in der Mappe umstellt, kommt nie ins
Projekt zurück: Die Formelmappe ist ein nachvollziehbarer Nachweis, keine zweite Eingabeoberfläche.

Vor Stufe 0 zu klären: ob die eingesetzte ClosedXML-Fassung Formeln mit zwischengespeichertem
Ergebnis ablegt oder Excel beim Öffnen rechnen muss, und ob eine Formelmappe in anderen
Tabellenkalkulationen dieselben Werte zeigt. Im Bestand deckt kein Test den Excel- und den Word-Generator ab —
die Stufen brauchen zuerst eine Wache über beide Blattstrukturen.

### 2.11.7 Hinweistext bis zur vollständigen Szenarioabdeckung (Entscheid 18.09.2026 zu V-4)

Die vollständigen Parametersätze je Szenario (§ 2.11.5) kommen **nach** der Darstellungsetappe. Bis
dahin sagt ein Hinweis unter der Annahmentafel der Wirtschaftlichkeitsseite, was ein Szenario heute
variiert und was nicht. **Gemessen** an `WirtschaftlichkeitParameter.FuerSzenario` und der Eingabe
(`LiesInvestitionen`, Einspeiseerlös, PV-Reihe): Best und Worst ersetzen Zins, p_E, p_B und p_I und
wirken in der Eingabe auf Investition, Erträge und Nutzungsdauer ungepflegter Positionen; der
Betrachtungszeitraum, die Trägerpreise, die Erlössätze, die Mengen und die gesetzlichen Sätze
bleiben in allen drei Szenarien gleich. (Die Spalte „EPOS heute" der Zeile V-G5 beschreibt den
Stand vor der Etappe W5‑B‑9; seitdem variieren Zins und Preisraten sehr wohl.)

**Wortlaut** (beide Sprachen als Ressource, sobald die Ansicht gebaut wird):

> **Was ein Szenario heute variiert — und was nicht.** Ungünstig und Günstig verändern gegenüber
> Erwartet den Kalkulationszins, die drei Preissteigerungsraten (Energie, Betrieb, Investition und
> Ersatz), die Investition der Positionen ohne eigenen Szenariowert (±10 %), die Erträge aus
> Einspeisung und Photovoltaik-Vergütung (±10 %) und die Nutzungsdauer der Positionen ohne eigenen
> Szenariowert (±2 a). In allen drei Szenarien gleich bleiben der Betrachtungszeitraum, die
> Energiepreise je Träger, die Erlössätze (Einspeisevergütung, Direktvermarktung), die Mengen aus
> der Simulation und alle gesetzlichen Sätze. Die vollständigen Parametersätze je Szenario — auch
> Zeitraum, Trägerpreise, Erlössätze und ein Mengenfaktor — kommen nach dieser Darstellung; bis
> dahin steht dieser Hinweis unter der Tafel.

Die Vorgabewerte im Text (±10 %, ±2 a) sind die Vorgaben aus § 2.1 des Szenarienkonzepts; trägt
das Projekt gepflegte Sätze, nennt der Hinweis die gepflegten Werte. Der Hinweis entfällt mit der
Etappe, die ihn überflüssig macht.

*Entscheid A14 offen (Analyse vom 19.09.2026):* Das Mockup führt einen abweichenden Schlusssatz;
welcher Wortlaut die Ressource `WIRT_SZEN_HINWEIS` trägt, ist nicht entschieden.

## 2.12 Kategorien-Mockups mit Rechenweg (Auftrag 02.09.2026)

*Ausgelagert in den Ordner [`Wirtschaftlichkeit_Kosten/`](LIESMICH.md):
`Beispielprojekt.md` (die eine Zahlenquelle), `../Mockups/Dialog_Formel_Zahlenprobe.html` — **das
eine konsolidierte Mockup**: alle acht Kategorien mit Dialog, Berechnungsgrundlage,
Berechnungserläuterung, Beschriftungen und Abnahmezeile, die Ergebnisseite in Kategorie 8 und zwei
Anhänge (Umsetzungsstand, Herkunft der Zahlen); zugleich Artifact
[Dialog, Formel, Zahlenprobe, Ergebnis](https://claude.ai/code/artifact/739d3cca-3b6c-4e2b-af8a-d1a7f73ddc9f)
— **die Repo-Datei führt**, das Artifact trägt einen älteren Zahlenstand.
Dazu `Rechenweg/01…08` — je Kategorie Dialog → Berechnungsgrundlage → Berechnungserläuterung →
Befunde. Der Auftrag: je Kostenkategorie ein Mockup mit Berechnungsgrundlage und
Berechnungserläuterung, Schwerpunkt Vergütungen BHKW und PV. Dieser Abschnitt ist die Kurzfassung;
bei Abweichung gilt der Ordner für die Zahlen und dieses Dokument für die Regeln.*

**Die Dialogform der Komponentenkosten ist abgenommen** (Anwender, 02.09.2026): Kopfband,
**Optionsgruppe „Betriebskosten / Investitionskosten"** und **zwei Reiter** „Kosten Invest/Betrieb"
und „Ertrag/Bonus" (`KDLG_TAB_KOSTEN`/`KDLG_TAB_ERTRAG`), Raster mit **Aktionen · Position ·
Bemessung · Satz · Betrag netto [€] · Nutzungsdauer [a] · Worst/Best** — Kostenart und Runde stehen
im Zeileneditor bzw. in der Herleitungszeile unter dem Betrag —, Warnband, Fußleiste mit vier
Rasterknöpfen (§ 2.8). Für **PV** dieselbe Form mit eigenen
Anordnungen: Spalte *Nutzungsdauer* im Investitionsraster, Herleitung der kWp-Menge aus
Modulanzahl × Modulleistung (Befund I-1 sichtbar), Gruppe *Ersatz und Restwert* mit Barwerten,
Kennzahl €/kWp, Betriebsseite ohne Endenergie-Bemessung, Gruppe *Ertrag und Degradation* (V-G2).

**Beispielprojekt** (durchgängig): BHKW 300 kW_el, Erdgas, η 38 / 45 / 83 %, 5.500 h/a → Brennstoff
4.342,1 MWh/a, Strom brutto 1.650,0, netto 1.563,2 MWh/a · PV 300 kWp (750 × 400 Wp), 285,0 MWh/a ·
Reststrombezug 250 MWh/a · produzierendes Gewerbe · i = 3 %, T = 20 a. Belegzahlen des Bestands
(Kaskadenprobe 1042, Mischsatz 300 kW, AW 300 kWp, Höfingen) sind als solche gekennzeichnet.

| # | Kategorie | Rechenweg | Kernaussage der Zahlenprobe |
|---|---|---|---|
| 1 | Investitionskosten BHKW | `01` | Kaskadenfaktor auf die Hauptposition 1,155; I₀ = 240.772,40 − 6.000 = **234.772,40 €** (300 kW) |
| 2 | Betriebskosten BHKW | `02` | Hilfsenergie 2 % × 312.631 € Endenergiekosten = 6.252,62 €/a (21.710 kWh Strom); Instandhaltung 1,50 % × 240.772,40 = 3.611,59 €/a; Summe 57.164,21 €/a; die Kesselmenge kommt aus der Modulzeile |
| 3 | Kosten der Photovoltaik | `03` | 192.150 € = 640,50 €/kWp; Wechselrichter-Ersatz Jahr 12 (24.000 €, Barwert 16.833), Restwert 39.800 € (Barwert 22.036); Degradation 0,5 %/a → Jahr 20: 259,1 MWh |
| 4 | Energiekosten | `04` | **umgesetzt** (ET-D): Preisbestandteile 0,0638 + 0,1371 + 0,1180 + 0,4371 = 0,7560 €/m³ — im Dialog in der Abrechnungseinheit; BEHG 872,3 t × 65 € = 56.700 €/a; N3 +32 % |
| 5 | **Vergütungen BHKW** | `05` | **Mengentafel** brutto 1.650 → netto 1.563,2 (§ 9 Nr. 3 bleibt brutto); Mischsatz 5,5667 / 2,4167 ct; 2026 vergütet 60 % = 32.022 €; **Reihe endet nach zwölf Jahren** (291.111 €); § 53a 21.203 €/a |
| 6 | **Vergütungen PV** | `06` | AW 6,04 ct; 159,6 von 199,5 MWh vergütet (§ 51: 20 % abgeregelt); Spot 7.182,00 + Prämie 2.457,84 − DV 638,40 = 9.001,44 €/a; § 51a 1.095,52 € im Jahr 20; Reihe nominal 150.118 €, Barwert 113.800 € |
| 7 | Erlösrubrik | `07` | Block A 91.727,0 €/a; vermieden brutto 339.753,6 − entgangene § 9b 23.594,0 = effektiv 316.159,6 €/a |
| 8 | Wirtschaftlichkeit über Nutzungsdauer | `08` | Musterprojekt: Kapitalwertdifferenz V1 +1.660.205 · V2 +182.491 · V3 +1.842.695 € (Bandbreite V3 1.636.035 … 2.048.635 €); Höfingen: Näherung 65.073 €, jahresscharf 65.259 €; IZF/Amortisation nachrichtlich |

**Drei Darstellungen, die über den bisherigen Stand hinausgehen und in die Umsetzung gehören:**

1. **Mengentafel im BHKW-Dialog** — Brutto, Hilfsstrom, Netto, Eigen/Einspeisung und der brutto
   bleibende § 9-Eigenverbrauch untereinander, mit der Spalte „verwendet von". Sie macht das
   Netting (§ 3.6) erklärbar, statt es zu verstecken.
2. **Jahresreihe des Zuschlags als Diagramm** — Deckelstaffel und Kontingent auf einen Blick; die
   Vorschau „Zuschlag p. a." allein suggeriert eine Dauerförderung.
3. **Ersatz- und Restwerttafel je Komponente** — Wechselrichtertausch und Modulrestwert sind der
   Regelfall, das BHKW-Modul wird im Jahr 15 ersetzt. Die Nutzungsdauer-Spalte und die Gruppe
   „Ersatz und Restwert" gehören **technikneutral** ins Investitionsraster jeder Komponente, weil
   beide an der einzelnen Position hängen; die Abweichung V-G4 wird dort deklariert, und eine
   Hinweiszeile mit Platzhaltern nennt die Positionen ohne Nutzungsdauer.

**Fachliche Klarstellungen, die beim Durchrechnen entstanden sind:**

- Der Emissionsfaktor Erdgas in der BEHG-Reihe ist der EBeV-Wert **200,9 g/kWh (H_i)** (§ 3.11);
  CO₂-Preisbestandteil im Arbeitspreis und BEHG-Reihe beschreiben denselben Betrag — **im
  Kapitalwert darf nur einer von beiden stehen**; die Kohärenzprüfung (§ 3.9) bekommt dafür eine
  Zeile.
- Die BEHG-Abgabe entsteht allein aus dem Brennstoff (Kessel, BHKW): Die Variante „Photovoltaik" der
  Kategorie 4 behält den Gas-Brennwertkessel und trägt dessen Abgabe weiter (26.857 €/a wie das
  Stammprojekt); die Photovoltaik selbst trägt keine CO₂-Kosten (§ 3.5).
- Die vermiedene Strommenge der Differenzmethode ist die **physisch** vermiedene (netto
  Eigenverbrauch BHKW + PV-Eigenverbrauch), nicht die brutto bemessene § 9-Menge.
- Der KWKG-Eigenstromsatz braucht einen Tatbestand des § 6 Abs. 3; ohne ihn ist der Satz 0 und
  Bonus_voll halbiert.

**Namensvorsicht:** „Befund K-1" (§ 3.6, Abwärmeabfuhr) und „Entscheidung K1" (§ 5, Deckung je
Modul) sind verschiedene Dinge — im Ordner steht der Befund mit Bindestrich. Dasselbe gilt für
„B-1": In § 3.8 und im Rechenweg `05`/`07` bezeichnet es die Stromsteuer-Erlösreihe (erledigt mit
B6), in der Befundtabelle § 4 und in den Mockups die Kessel-Endenergie (Auftrag #331). Ebenso
kollidieren:

- **U-1 gegen U1** — „U-1" ist der Einheitenbruch der Gase (§ 5), „U1" die erste Zeile des
  Mockup-Anhangs „Umsetzungsstand" und damit der Befund **K-1** (Abwärmeabfuhr, Stromkennzahl σ);
- **V-1 gegen V-1** — in § 2.11.4 die entschiedene ValERI-Frage „Umschalter", in der Befundtabelle
  § 4 der Befund zur EV-Rundung (EvMix unrundet gegen gerundeten Erlös);
- **V-Gn gegen Gn** — die Lückennummern dieses Papiers (`V-G1…V-G12`, § 2.11.2) und die des
  Szenarienkonzepts (`G1…G11`) meinen bei gleicher Ziffer Verschiedenes; die Übersetzungstafel
  steht am Anfang von § 2.11.2.

## 2.13 Ergebnisansicht (Anwenderdurchsicht 18.09.2026)

*Die Ergebnisansicht steht in Kategorie 8 des einen Mockups
`../Mockups/Dialog_Formel_Zahlenprobe.html`: Kopf mit Umschalter, dann die
vier Fragen „Lohnt es sich · Wie sicher ist das · Woraus entsteht die Zahl · Was ist angenommen",
die Kapitalwertformel, die Gegenprobe und die Bericht-Ausgaben. Woraus die einzelnen Beträge
entstehen, sagen die Kategorien 1 bis 7; was noch nicht gebaut ist, steht im Anhang
„Umsetzungsstand". Der Anwender hat die Ansicht am 18.09.2026 durchgesehen; die fünf Punkte, dazu (6) als Verweis, und ihre
Messung am Bestand:*

**(1) Die laufenden Energiekosten — „Verbrauchskosten (BHKW)".** Der Begriff kommt im Bestand nicht
vor. Die Größe heißt **Energiekosten** (`EnergiekostenJahr`): eigene Kennzahlenzeile nach den
Betriebskosten und vor den Stromkosten nach Tarif, eigener Topf im Kapitalwert mit eigener
Preissteigerung. Die anlagenscharfe Aufschlüsselung `EnergiekostenJeAnlage` (B7) **wird
gezeichnet**: `WirtschaftlichkeitZeilen.Kennzahlen` legt je Anlage der Gruppe eine eingerückte
Unterzeile „davon ‹Anlage› [€/a]" und „davon Netzbezug Strom [€/a]" in den Zeilenkatalog, den die
Windows-Gabe der Seite, Word und Excel über `Sichtbare` gemeinsam ziehen. Zwei Stücke davon sind
**verwaist, kein Fehler**: der Überschriftsschlüssel `WIRT_ENK_KOPF` („Energiekosten je Anlage
[€/a]") wird außer in der erzeugten Ressourcenklasse `Resource.Designer.cs` nirgends gelesen, und `AnlageHerleitung` („Menge × Preis =
Betrag") hat als einzigen Aufrufer einen Test — beides kann die Ergebnisansicht ohne neue Schlüssel
verwenden. **Die Nachweise je Anlage bleiben erhalten:** Sie reisen im Nachweisumschlag der
Ergebniszeile mit (`ErgebnisNachweisUmschlag`, Spalte `Nachweis_Json`), die davon-Zeilen stehen
deshalb auch nach dem Neuladen einer gespeicherten Rechnung; nur eine ohne Umschlag gebuchte
Zeile lädt wie zuvor, also ohne sie. Das Mockup führt die Zeile unter ihrem Namen an ihrer
Stelle, in der Tafel der laufenden Kosten und in der BHKW-Ansicht
(„Energiekosten — davon Blockheizkraftwerk" statt „Brennstoffkosten"); Photovoltaik hat keine
Zeile (keine Endenergie, Hilfsenergie als Jahresbetrag in den Betriebskosten).

**(2) „Zuschuss BAFA" → „Zuschuss".** Neutral, ohne Programm- und Behördennamen; beide Mockups
geprüft.

**(3) Ersatz und Restwert je Komponente, mit Hinweis.** Gemessen und der Prämisse
widersprechend: Ersatz und Restwert gibt es **schon heute für jede Technik gleich** — es gibt keinen
PV-eigenen Pfad. Sie hängen an der einzelnen **Investitions-Kostenposition**, und der einzige
Schalter ist die Nutzungsdauer: n ≥ 1 schaltet Ersatz **und** Restwert zugleich ein, n < 1 beides
aus („still wie T"). `Tab_Nutzungsdauer` liefert Vorgaben für zehn Techniken, aber nur beim
**Anlegen** einer Position; in der Testdatenbank tragen **95 von 101** Investitionspositionen keine
Dauer, 27 davon tragen einen Betrag (Messung 19.09.2026). Das Mockup zeigt Ersatz und Restwert deshalb je Komponente als **Zusammenfassung ihrer
Positionen** (Betrag, Dauer, Ersatzjahr, Restwert, Herkunft der Dauer) und entwirft den fehlenden
**Hinweis**: „Betrachtungszeitraum über der Nutzungsdauer-Vorgabe der Technik, k von n Positionen
ohne Nutzungsdauer (Position, Betrag): kein Ersatz, kein Restwert gerechnet — ist ein Ersatz fällig,
ist der Kapitalwert zu günstig ausgewiesen." Er gehört auf die Seite **und** in den Bericht, als
Prüfauftrag, nicht als Fehlermeldung. Rechenwerk braucht er keines: T steht in
`Tab_ProjektWirtschaftlichkeit.Betrachtungszeitraum`, die Positionen samt n liefert
`LiesInvestitionen` im Szenario Erwartet, `NutzungsdauerAbgleich.Hinweis` bildet kürzeste und
längste Dauer bereits; zu ergänzen ist allein die Zahl der betragstragenden Positionen ohne Dauer
gegen `NutzungsdauerCtrl.Vorgabe`.

**Erledigt mit #357** (Nutzungsdauern Stufe S2, U8 und U30): die **Nachpflege des Bestands** — der
Knopf „Nutzungsdauern vorbelegen…" steht als vierter der Rasterleiste
(`KostenKomponenteDialog.razor:310`); offen bleibt allein sein gesperrter Zwilling auf der
Betriebsseite. Ebenso der **Pflegeort für die Positionsart** — `NutzungsdauerID` ist seither eine
Klappliste im Zeileneditor (`KostenProjektPositionenCtrl.NutzungsdauerArtZuordnen`).

**Was der späteren Umsetzung fehlt** (drei Stücke, im Mockup-Anhang als **U39** geführt):

1. die **Entkopplung** von Ersatz und Restwert — ein Kennzeichen je Position oder je Technik
   („Ersatz führen", „Restwert ansetzen"), weil ein Anwender oft das eine ohne das andere will;
2. die **geräteeigenen Nutzungsdauer-Spalten** (`Tab_BHKW`, `Tab_Heizkessel`,
   `Tab_StromspeicherVariante`) — eine zweite Wahrheit, die kein Wirtschaftlichkeitsrechner liest;
3. der **Anschluss der Speicherflotte**, die ihren Ersatz über die gleichnamigen **Felder des
   Flottenstands** (`ErsatzintervallJahre`, `RestwertEuro` als JSON in `Tab_SpeicherAuslegung`) führt
   — nicht über Spalten; der Anschluss berührt deshalb die **Einfrierregel** des Projekts 1046.

Dazu: Die Hinweiszeile hat weiterhin nur `WirtschaftlichkeitSeiteGaben` als **einzigen** Schreiber,
und die Textbildung liegt bereits im Kern (`NutzungsdauerAbgleich.Hinweis`); zu tun ist das Einsammeln
der Positionen, nicht der Text. **Stand: Die Hülle selbst liegt seit E3 plattformfrei (umgesetzt
#431)** unter `EPOS.UI.Daten/Wirtschaftlichkeit/` — den Ordner `Wirtschaftlichkeit`, den es dort noch
nicht gab, gibt es damit jetzt. **Offen bleibt allein das Einsammeln der Positionen in einem
Kern-Controller (U39) — das gehört zu E5.**

**Entscheid A1 (20.09.2026, nach Empfehlung): der Umzug kommt vor der Ergebnisansicht.** Rechenaufruf
und Datenseite werden als eigene Welle **E3 Plattform** aus der Windows-Schale geholt, nicht erst mit
der Ergebnisansicht — sonst entsteht jedes Stück dieser Ansicht ein zweites Mal nur für Windows.
**Das Muster liegt seit #428 (KI‑F8) vor:** Vier Hüllen (`KlimadatenHuelle`, `ProjektKopieHuelle`,
`PeakShavingHuelle`, `StromganglinieAdminHuelle`) sind plattformfrei nach `EPOS.UI.Daten` gewandert,
während Windows je Hülle einen **Fenster-Adapter** behielt (`KlimadatenFenster`, `ProjektKopieFenster`,
`PeakShavingFenster`, `StromganglinieAdminFenster`); die Wurzel öffnet dieselben Masken auf iOS über
Nähte in `IProjektQuelle`. Nach diesem Muster sind die Hüllen dieses Papiers umgezogen — allen voran
`KostenKomponenteHuelle` (Q14, E3 Schritt 5). **Stand: umgesetzt #431** (Merge `2cfee66b`).

**(4) Erlöse und Vorteile je Komponente.** B7 hat die Rubrik nach der Achse **zahlungswirksam /
Ausweis** gebaut (Block A mit Summe, Block B ohne); der Anwender verlangt die Gliederung nach
**Komponente**. Entscheid des Entwurfs, im Mockup sichtbar begründet: **A/B bleibt die äußere
Ordnung** — sie entscheidet, was in die Summe geht, und genau das darf keine Gliederung verwischen
(stünde die Komponente außen, lägen beim BHKW Zuschlag und vermiedene Stromkosten untereinander) —,
**die Komponente gliedert innen**: Blockheizkraftwerk, Photovoltaik, Kessel, zuletzt **„projektweit"**
für Positionen ohne Anlagenbezug (die § 9b-Entlastung hängt am Restbezug, nicht an einer Anlage).
Jeder Komponentenblock trägt seine Zwischensumme, Block A darunter die Gesamtsumme; Block B wird
nicht summiert, weil sich seine Beträge überschneiden (die Befreiung steckt im Arbeitspreis der
vermiedenen Kosten). Für die Umsetzung: Die Zeilen des Katalogs brauchen einen Anlagenbezug
(Schlüssel je Anlage wie bei den Energiekosten-Unterzeilen), und die vermiedenen Stromkosten sind
heute **projektweit** aus `VermiedenMengeMWh` gebildet — eine Aufteilung je Anlage braucht einen
**Verteilschlüssel je Anlage**: die Strommatrix trennt nur nach **Tarifzone** (`StromMatrix.Zone`),
nicht nach Anlage, und der Kern verteilt heute nach dem Netto-Stromanteil (Befund V-4); der
Leistungsanteil bleibt projektweit.

**(5) Der Verlauf mit allen drei Szenarien.** Gemessen: Der Knopf „Verlauf…" öffnet
`KapitalwertVerlaufDialog`, der **ein** Szenario je Lauf rechnet, immer bei Erwartet beginnt (die
Seite reicht ihre Szenariowahl nicht durch) und zwei Bilder zeigt (Differenz zur Referenz; kumulierte
Barwerte je Projekt absolut). Der Renderer (`ChartRenderer.KapitalwertVerlauf`) kennt keine
Szenarien, zeichnet aber beliebig viele Reihen auf eine Jahresachse und führt eine Legende mit
Name, Farbe und **Strichart je Reihe** (`Reihe.Gestrichelt` wird gelesen, `Segment.Gestrichelt`
zeichnet das Legendenfeld gestrichelt statt gefüllt); er kann **nicht**: ein Flächenband, mehr als
etwa zwei Legendenzeilen im festen Maß 1240 × 620, mehr als acht unterscheidbare Farben. **Entscheid des Entwurfs:** eigener Abschnitt bei „Wie sicher
ist das?" — die Bandbreite zeigt die Spanne am Ende, der Verlauf über die Zeit; der Knopf entfällt;
**Farbe = Variante, Strichart = Szenario** (ein Band ist bei mehreren Varianten unlesbar und vom
Renderer nicht zeichenbar); die Legende zweigeteilt (Varianten + 3 Einträge statt Varianten × 3);
der Nulldurchgang je Szenario markiert. **Das zweite Bild** (Versionen absolut) bleibt nicht auf der
Seite: Alle Versionen liegen tief im Negativen und nahezu parallel, entschieden wird über den
Abstand; der absolute Vergleich steht in der Kennzahltafel (Nettobarwert absolut) und in der
Mehrjahresübersicht des Berichts. **Als Bild steht er an genau einem Ort: im Wortbericht**, unter
dem Titel „Kumulierte Barwerte je Version", mit **Legende je Version** (Name und Farbe) und
gestrichelter Stammlinie — sie ist die Bezugsgröße und keine Version und muss auch im
Schwarz-Weiß-Ausdruck davon zu trennen sein. Auf der Seite bleibt allein das Differenzbild.
Nachweis: `Proben/ChartProben` (Bild `kapitalwert_absolut_legende`, Gegenproben
`kapitalwert_verlauf_gestrichelt_wirkt` und `kapitalwert_verlauf_legende_nennt_die_version`).
**Was die Umsetzung braucht:**

- einen Rechenaufruf für die Dreierreihe — `BerechneVerlauf` nimmt einen Szenario-String und
  `WirtschaftlichkeitVerlauf` trägt genau einen: entweder drei Läufe der bestehenden Methode (die
  Berichtsdaten werden ohnehin nur einmal gesammelt, der Mehraufwand ist die Zahlungsbildrechnung)
  oder ein Sammelmodell mit drei Szenarien;
- eine Reihenbildung, die Variante und Szenario zugleich unterscheidet — heute vergibt
  `VerlaufsReihen` Farben nach laufendem Index, und die Reihennamen tragen nur den Projektnamen
  (dasselbe Projekt in drei Szenarien bekäme drei beliebige Farben und dreimal denselben Namen).
  Die Palette hat **acht** Farben (`ChartRenderer.cs:553–562`); mit „Farbe = Variante" reicht sie
  bis acht Varianten;
- Platz für die zweigeteilte Legende und ein passendes Bildmaß (das Lesen von `Gestrichelt` in
  `KapitalwertVerlauf` steht). **`Reihe.Gestrichelt` ist ein `bool`** (`ChartRenderer.cs:106`) und
  trägt damit **zwei** Stricharten — drei Szenarien brauchen eine dritte;
- im Tabellenbericht je Szenario eine Spaltengruppe (die heutige Tabelle „Jahr, je Projekt eine
  Spalte, dann die Δ-Spalten" ist dafür nicht vorbereitet), im Wortbericht ein zusätzliches Bild;
- **plattformfrei**: Rechen- und Zeichenlogik der Ansicht gehören nach `EPOS.UI.Daten` — **Stand:
  umgesetzt #431**, der Ordner `Wirtschaftlichkeit` besteht, `KapitalwertVerlaufHuelle` liegt darin
  und sammelt, rechnet und zeichnet bereits plattformfrei über den Renderer des Kerns; offen bleibt
  allein die Dreiszenarien-Erweiterung dieses Punkts (E6).

**(6) Vergleichssicht — alle Varianten gegen die Referenz oder zwei Stände.** Die Anforderung vom
18.09.2026 zur Tafel „Gliederung des Kapitalwerts" steht als eigener Abschnitt in § 2.15, weil sie
in die Differenzrechnung greift und die Gliederung von § 2.9 braucht (Ist, Soll-Tafel, Randfälle,
Abnahme, Etappe); das Mockup zeigt beide Sichten in Kategorie 8.

**Weitere Festlegungen des Mockups:** Umschalter „Kennzahlen / ValERI-Bewertung" im Kopf der Seite
(K8/V-1); Hinweistext zu den Szenarien unter der Annahmentafel (§ 2.11.7); der Kopfabschnitt „Was
sich gegenüber der heutigen Seite ändert", der alle fünf Punkte führt, steht bislang nur im
abzulösenden Mockup `../Mockups/Ergebnis_Bandbreite_Herkunft.html`. Die drei Entscheide, die den
Zuschnitt änderten: K-3 ist mit B6 erledigt (Statuszeile #328, anderer Rechner), B-1 (Kessel) behebt
Auftrag #331, die Erlösrubrik steht seit B7.

---

## 2.14 Erfassungsgruppen auf der Kostenseite (Anwenderentscheid K-WZ-1)

Die Kostenseite (Berichte & Kosten → Kosten) listet zuerst die Anlagen des Projekts und hängt
darunter an, was **keiner Anlage** zugeordnet ist. Dort stehen zwei verschiedene Dinge, und sie
sehen jetzt auch verschieden aus:

| Zeile | Was sie ist | Wie sie erscheint |
|---|---|---|
| Anlagenfähige Gruppe ohne gültige Zuordnung | Ihre Anlage ist gelöscht oder der Verweis leer — sie **hatte** eine | gelb, „{0} — ohne Anlagenzuordnung", Papierkorb |
| **Erfassungsgruppe** (Wärmezentrale, Bauliche Anlagen, Stromeinspeisung) | Sie **ist** keine Anlage und kann keiner zugeordnet werden — sie sammelt Kosten, die zu keinem Gerät gehören | „{0} — Erfassungsgruppe (ohne Anlage)", Papierkorb |

**Die Unterscheidung gehört in den Kern:** `KostenVorlagenCtrl.IstErfassungsgruppe` — das
Gegenstück zu `IstWaehlbar` (Ä7). Stünde sie in der Oberfläche, hätte jede Schale ihre eigene.
Beide Zeilenarten rechnen in der Wirtschaftlichkeit mit, beide lassen sich über denselben
Papierkorb nach Rückfrage löschen und in der Kostenverwaltung bearbeiten.

**Nicht in der Statuszeile.** Die Meldung „Kostenpositionen ohne verbaute Anlage: …" gilt weiter
nur den gelben Zeilen — eine Erfassungsgruppe hat nie eine Anlage gehabt, und die Meldung wäre
eine Fehlanzeige.

**Die Altzeilen.** Die frühere Kostenmaske hinterließ je Erfassungsgruppe eine
**Hauptkomponentenzeile** in `Tab_ProjektWerte` (`Tab_Kostenfaktor.IsMainComponent = 1`, Gruppe
„Allgemein", Wert 0,00). Kein heutiger Rechenweg legt sie an. **Schemaschritt 90** entfernt sie —
aber nur, wenn die Gruppe **nirgends** eine Position mit Wert führt; sonst bleibt sie vollständig
stehen, denn dort ist die Hauptkomponentenzeile die Überschrift ihrer Positionen.

## 2.15 Vergleichssicht der Ergebnisansicht — alle Varianten gegen die Referenz oder zwei Stände (Anforderung 18.09.2026) — umgesetzt

> **Stand: umgesetzt.** Sicht, A und B liegen als Sitzungswahl in `Vergleichsauswahl.Sicht`
> (`Vergleichssicht`); Sicht 2 übergibt A als Referenz des Rechenlaufs, ohne
> `ID_Referenzprojekt` zu schreiben, und rechnet **ohne zu persistieren** — der gespeicherte Lauf
> bleibt der gegen die Unterlassensalternative der Gruppe. Die Ergebnisansicht trägt die
> Optionsgruppe mit den Klapplisten A und B, dem Tauschknopf und der Erklärzeile; der Verlauf
> zeichnet in Sicht 2 die eine Kurve B − A; Word und Excel folgen der Sicht und tragen die
> Deklarationszeile (`Referenzwahl.Deklarationszeile`). Nachweis
> `EPOS.Kern.Tests/VergleichssichtTests`, `EPOS.Kern.Tests/ReferenzprojektTests` und
> `EPOS.UI.Tests/Seiten/WirtschaftlichkeitSichtTests`.

**Anforderung des Anwenders, im Wortlaut:** „Es soll die Optionen geben, entweder Stamm mit allen
Varianten (wie bisher) oder zwischen zwei Varianten (oder Stamm mit einer Variante)." Gemeint ist die
Ergebnisansicht der Wirtschaftlichkeit (Berichte & Kosten → Wirtschaftlichkeit) mit ihren
Vergleichstafeln — Kennzahltafel, Empfehlung, Bandbreite, Verlauf, Gliederung des Kapitalwerts und
Brücke.

**Warum ein eigener Abschnitt und kein sechster Punkt in § 2.13:** Die fünf Punkte der
Anwenderdurchsicht sind Darstellungsbefunde je Größe. Die Vergleichssicht greift dagegen in die
Differenzrechnung ein und ist das Schwesterstück von § 2.9 — sie braucht dieselbe Gliederung
(Ist, Soll-Tafel, Randfälle, Abnahme, Einordnung als Etappe). § 2.13 verweist unter (6) hierher.

**Ist-Zustand vor der Umsetzung:** Die Ergebnisansicht stellte alle angehakten Stände der
Vergleichsgruppe nebeneinander — eine Spalte je Stand — und rechnete die Differenzkennzahlen jeder
Variante (Kapitalwertdifferenz, Annuität, dynamische Amortisation, interner Zinsfuß) gegen den
Stamm. `KapitalwertRechner.AmortisationDifferenz` und `InternerZinsfuss` nehmen zwei
Zahlungsbilder; `WirtschaftlichkeitZeilen.Kennzahlen` zeichnete die Stammspalte mit dem
Platzhalter „Referenz". Welche Stände in der Ansicht stehen, entscheiden die Häkchen der
Vergleichsgruppen-Liste (`Vergleichsauswahl` — eine Sitzungswahl für Übersicht, Kosten und
Wirtschaftlichkeit). Eine Wahl zweier Stände gegeneinander gab es nicht. Das Mockup zeigt diesen
Aufbau in Kategorie 8 als Sicht 1.

**Soll — zwei Sichten:**

| Tafel | Sicht 1 — alle Varianten gegen die Referenz | Sicht 2 — zwei Stände |
|---|---|---|
| Spalten | eine je angehaktem Stand, Referenzspalte zuerst | A \| B \| Differenz B − A |
| Referenz der Differenzkennzahlen | die Referenz der Gruppe (§ 2.9; Vorgabe Stamm) | A — für diese Sicht |
| Vorzeichen | Variante − Referenz | B − A; ein Tausch von A und B dreht das Vorzeichen |
| Kennzahltafel und Empfehlung | je Variante eine Spalte und eine Karte, dazu der Vorschlag zur Wahl | eine Spalte „B gegen A" und eine Karte; kein Vorschlag zur Wahl — die Einstufung gilt für B gegenüber A |
| Bandbreite | je Variante die drei Szenarien | Zeile A „Referenz", Zeile B mit den drei Szenariowerten B − A |
| Verlauf | Differenzkurve je Variante (Farbe) und Szenario (Strichart) | die Differenzkurve B − A in drei Stricharten; ihr Nulldurchgang ist die dynamische Amortisation von B gegenüber A |
| Gliederung und Brücke | je Stand eine Spalte, Differenzspalte gegen die Referenz | A \| B \| B − A; die Brücke läuft von A nach B |
| ValERI-Umschalter | die fünf Blöcke gegen die Referenz der Gruppe | die fünf Blöcke gegen A, mit Deklarationszeile |
| Kennzahlenzeile (Nachweis) | „Referenz: ‹Referenz der Gruppe›" | „Referenz dieser Sicht: ‹A› · Referenz der Gruppe: ‹Referenz›" |

**Wie § 2.9 und die Paarwahl zusammenspielen — Festlegung:** Sicht 2 setzt **A als Referenz dieser
Sicht**, ohne die gespeicherte Gruppenreferenz `ID_Referenzprojekt` anzufassen. Die
Differenzrechnung nach § 2.9 bekommt ihre Referenz als Parameter des Aufrufs; die Gruppenreferenz
liefert den Vorgabewert, Sicht 2 übergibt A. Die Alternative — die Paarwahl als reine Anzeige über
der Referenzrechnung, B − A = (B − Referenz) − (A − Referenz) — trägt nur für die linearen Größen:
Kapitalwertdifferenz, Annuität, Bandbreite, Gliederung, Brücke und Verlaufskurve sind Differenzen
und lassen sich exakt aus den Werten der Sicht 1 bilden. Dynamische Amortisation und interner
Zinsfuß sind es nicht: Sie hängen an der Jahresreihe der Differenz B − A und brauchen den
Rechenlauf mit A als Gegenstück (`AmortisationDifferenz(bildB, bildA)`,
`InternerZinsfuss(bildB, bildA)`). Ein zweiter Rechenweg für die Paarwahl wäre eine zweite
Wahrheit; deshalb rechnet Sicht 2 mit derselben Methode wie § 2.9. Die Gruppenreferenz bleibt die
Unterlassensalternative der Norm (§ 2.11); die Paarwahl ist ein Erkundungswerkzeug und schreibt
sie nicht um.

**Bedienung:** Über den Vergleichstafeln steht eine Optionsgruppe „Alle Varianten gegen die
Referenz | Zwei Stände" mit zwei Klapplisten A und B. Die Listen führen die angehakten Stände der
Vergleichsgruppe (Stamm eingeschlossen) und sind in Sicht 1 gesperrt. A ≠ B ist ohne Meldung
gesichert: Die Liste B führt A nicht, die Liste A führt B nicht. Vorbelegung beim Wechsel in
Sicht 2: A = Referenz der Gruppe, B = die erste andere Variante in der Reihenfolge der
Vergleichsgruppen-Liste. Unter der Optionsgruppe nennt eine Erklärzeile die geltende Referenz — in
Sicht 1 die der Gruppe, in Sicht 2 A und, wenn sie davon abweicht, dazu die Gruppenreferenz. Die
Szenariowahl gilt für beide Sichten gleich. Die Beschriftung der Sicht 1 lautet „gegen die
Referenz" und nicht „gegen den Stamm", weil die Referenz mit § 2.9 auch eine Variante sein kann;
solange die Vorgabe gilt, nennt die Erklärzeile den Stamm.

**Persistenz — Sitzung, nicht Datenbank:** Sicht, A und B liegen in der Sitzung, an derselben
Stelle wie die Häkchen der Vergleichsgruppen-Liste (`Vergleichsauswahl`), und werden von der
Wirtschaftlichkeitsseite und vom Bericht gelesen; Übersicht und Kosten kennen sie nicht. Beim
Wechsel der Vergleichsgruppe und beim Neustart gilt Sicht 1. Begründung: Eine gespeicherte Paarwahl
wäre eine zweite Referenzangabe neben `ID_Referenzprojekt` — zwei Spalten, die dasselbe meinen
können und sich widersprechen dürfen. Wer einen Paarvergleich dauerhaft will, wählt A als
Gruppenreferenz (§ 2.9); dann zeigt Sicht 1 alle Stände gegen A, und der Bericht ist aus der
Datenbank reproduzierbar.

**Bericht (Word und Excel):** Der Bericht folgt der Sicht — so wie er den Häkchen folgt
(`BerichtSeiteGaben` liest `GewaehlteVarianten`). In Sicht 2 druckt er A | B | Differenz mit A als
Referenz und die Deklarationszeile „Referenz dieser Bewertung: ‹A› · Unterlassensalternative der
Gruppe: ‹Referenz›". Beides entsteht aus der einen Zeilendefinition:
`WirtschaftlichkeitZeilen.Kennzahlen` bekommt die Menge [A, B] und A als Referenz, so wie es heute
die Menge aller Stände und den Stamm bekommt — keine dritte Wahrheit, kein zweiter Zeilenkatalog.
Der ValERI-Bewertungsbericht (Anhang E) trägt in Sicht 2 die Deklaration, dass der Vergleich zwei
Maßnahmen gegeneinander stellt; die Norm lässt das zu (8.1.2: Wahl unter Alternativen über den
höheren Kapitalwert), verlangt aber die Benennung. Die Ausgabe „Verlauf nach Excel…" schreibt die
gezeichneten Reihen, in Sicht 2 also die Reihe B − A.

**Randfälle, ausdrücklich geregelt:**

- **Gruppe mit nur dem Stamm:** Sicht 2 ist gesperrt (Option ausgegraut, Werkzeugtipp „mindestens
  zwei Stände"); Sicht 1 zeigt den Stamm allein.
- **Nur eine Variante:** Sicht 2 ist möglich; die Listen führen je einen Eintrag (Stamm und
  Variante), die Tafeln zeigen dasselbe wie Sicht 1 in der Spaltenordnung A | B | Differenz — und
  mit A = Variante das umgekehrte Vorzeichen.
- **Stand ohne Simulationsergebnis:** wie in § 2.9 — der Sammler rechnet ihn nach; scheitert das,
  bleibt die Tafel leer und die Statuszeile nennt den Fehlgrund; nie ein stiller Rückfall auf Sicht 1.
- **Gelöschte oder abgehakte Variante als A oder B:** Rückfall auf Sicht 1 mit Warnzeile, die den
  Rückfall benennt; da die Wahl in der Sitzung liegt, bleibt nichts zu bereinigen.
- **Gruppenreferenz wird in Sicht 2 gewechselt (§ 2.9):** A und B bleiben, wie gewählt; die
  Erklärzeile zeigt die neue Gruppenreferenz; erst der nächste Wechsel in Sicht 2 belegt A neu vor.
- **A = Gruppenreferenz:** Die Kennzahlen von B sind dieselben wie in Sicht 1 — ein Prüffall, kein
  Sonderweg.

**Abnahme:**

- Sicht 1 ist byte-gleich zum Bestand: Referenzlauf der fünf Projekte, Word- und Excel-Bericht
  unverändert.
- Sicht 2 mit A = Gruppenreferenz: alle Kennzahlen von B stimmen mit der Spalte B der Sicht 1
  überein (Test).
- Sicht 2 mit A ≠ Gruppenreferenz: Kapitalwertdifferenz(B − A) = Kapitalwertdifferenz₁(B) −
  Kapitalwertdifferenz₁(A) auf 0,01 €; die Gliederung geht je Bestandteil auf; ein Tausch von A
  und B dreht das Vorzeichen von Kapitalwertdifferenz und Annuität (Tests).
- bunit: beide Zustände der Optionsgruppe, Listen ohne den jeweils anderen Stand, Sperre bei nur
  einem Stand, Erklärzeile mit beiden Referenzen.

**Was die Umsetzung braucht:**

1. die Referenz als Parameter der Differenzrechnung in `WirtschaftlichkeitCtrl.Berechne` — dort
   ist der Stamm fest verdrahtet (`if (v.IstStamm) { … stammBild = bild; … }`); das ist zugleich
   der Kern der Etappe § 2.9;
2. Sicht, A und B in `Vergleichsauswahl` neben den Häkchen — plattformfrei, mit Vorbelegung und
   Rückfallregel;
3. `WirtschaftlichkeitZeilen.Kennzahlen` mit Menge und Referenz statt `IstStamm` als
   Referenzkennzeichen; der Platzhalter `StammAnzeige` wird zum Referenzplatzhalter;
4. die Optionsgruppe mit zwei Klapplisten und Erklärzeile in `WirtschaftlichkeitSeite.razor`,
   Texte in `MyResource.Resource.*` (beide Sprachen);
5. Bericht: Menge und Referenz aus der Sitzungswahl, dazu die Deklarationszeile;
6. Verlauf und Brücke lesen die Referenz aus derselben Wahl — `BerechneVerlauf` rechnet die
   Differenz zum Stamm und braucht denselben Parameter.

**Fragen mit Empfehlung — entschieden (Anwenderentscheid 18.09.2026: „VG‑Q1 bis VG‑Q7: Empfehlung"):**

| Frage | Empfehlung |
|---|---|
| **VG‑Q1** Setzt Sicht 2 A als Referenz des Rechenlaufs, oder ist die Paarwahl eine Anzeige über der Referenzrechnung? | **A als Referenz dieser Sicht**, ohne die Gruppenreferenz zu schreiben — Amortisation und Zinsfuß sind nicht linear, und es gibt nur einen Rechenweg für Differenzkennzahlen |
| **VG‑Q2** Beschriftung der Sicht 1: „gegen den Stamm" oder „gegen die Referenz"? | **„gegen die Referenz"**; die Erklärzeile nennt den Namen. „Stamm" wäre falsch, sobald § 2.9 eine Variante wählt |
| **VG‑Q3** Persistenz der Paarwahl? | **Sitzung**, in `Vergleichsauswahl` neben den Häkchen; keine Spalte |
| **VG‑Q4** Folgt der Bericht der Sicht, oder druckt er immer alle Stände? | **Er folgt der Sicht**, mit Deklarationszeile — so wie er den Häkchen folgt; wer alle Stände will, wählt Sicht 1 vor dem Druck |
| **VG‑Q5** Verlauf in Sicht 2: eine Differenzkurve B − A oder die zwei Kurven A und B gegen die Gruppenreferenz? | **eine Differenzkurve B − A** — ihr Nulldurchgang ist die Amortisation des Paars; mit A = Gruppenreferenz wäre die A-Kurve die Nulllinie |
| **VG‑Q6** ValERI-Bewertung in Sicht 2 erlaubt? | **ja**, mit der Deklaration „Vergleich zweier Maßnahmen · Unterlassensalternative der Gruppe: ‹Referenz›"; der Kapitalwert von B gegenüber A ist die Differenz zweier Kapitalwerte gegen dieselbe Unterlassensalternative |
| **VG‑Q7** Tauschknopf ⇄ zwischen den Listen? | **ja, klein** — er spart zwei Listenwahlen und macht die Vorzeichenregel sichtbar; kein Muss |

**Einordnung:** Eigene kleine Etappe **nach § 2.9**, deren Referenzparameter sie voraussetzt;
ergebnisneutral in der Vorgabe (Sicht 1), erste Wirkung erst mit der Wahl der Sicht 2. Mockup:
Kategorie 8 in `../Mockups/Dialog_Formel_Zahlenprobe.html#sicht2`, Umsetzungsstand U37.

---

## 2.16 Vergütung je Variante — eigene Werte oder vom Stammprojekt übernommen (Anforderung 18.09.2026) — umgesetzt

> **Stand: umgesetzt.** Schemaschritt 93 legt `Tab_ProjektPhotovoltaik.Uebernahme_Stamm` an
> (nullbares 0/1, `SchemaKatalog.Schritt93_VerguetungJeVariante`) und leitet die Wahl
> ergebnisneutral aus dem Bestand ab (`PvVerguetungJeVariante`, zwei DML).
> `ProjektPhotovoltaikCtrl.LiesAufgeloest` löst je Stand auf — Stamm, Variante mit eigener
> Zeile, sonst die Zeile des Stamms —, und `WirtschaftlichkeitCtrl.RechnePvVerguetung` liest
> sie; der flache Satz `Einspeiseverguetung` bleibt je Gruppe. Der Kopierlauf lässt die
> PV-Zeile aus, `ProjektCtrl.Delete` gibt den übernehmenden Varianten vor dem Löschen die
> Stammwerte. Der Reiter Ertrag/Bonus trägt die Optionsgruppe samt Erklärzeile und die
> Klappliste „Projekt:" im Admin-Kontext, der Vergütungsdialog die Herkunftszeile und den
> Knopf „eigene Werte". Die Herkunft reist im `ErgebnisNachweisUmschlag` (Fassung 3) und
> steht als Zeile `PV_HERKUNFT` in Block A. Nachweis
> `EPOS.Kern.Tests/PvVerguetungJeVarianteTests`, `EPOS.UI.Tests/Dialoge/ErtragBonusTests` und
> `EPOS.UI.Tests/Dialoge/PhotovoltaikVerguetungDialogTests`; Referenzlauf byte-gleich.

**Anforderung des Anwenders, im Wortlaut:** „Die Vergütung kann in der Variante unterschiedlich vom
Stamm sein. Die Option der Übernahme soll es geben, aber eigene Vergütung in den Varianten muss
möglich sein." Gemeint ist die PV-Vergütung, die der Reiter „Ertrag/Bonus" des Kostendialogs
(`Rechenweg/03_Kosten_Photovoltaik.md`) und der Knopf „Photovoltaik…" der Wirtschaftlichkeitsseite
im Vergütungsdialog (§ 2.3, `Rechenweg/06_Verguetungen_PV.md`) öffnen.

**Warum ein eigener Abschnitt:** Wie § 2.9 (Referenz) und § 2.15 (Vergleichssicht) greift die
Anforderung in die Frage ein, was je Gruppe und was je Stand gilt (Regel R‑1: Rahmenparameter je
Stammprojekt). § 2.9 wählt die Referenz je Gruppe, § 2.15 die Sicht je Sitzung; dieser Abschnitt löst
eine Größe, die als gruppenweit galt, auf die Stände auf. § 2.13 bekommt keinen neuen Punkt: Die
Vergütung ist keine Darstellungsfrage der Ergebnisansicht.

**Ist (18.09.2026) — zwei Ablageorte, ein Leseweg je Stand:**

- *Der flache Einspeisesatz* `Einspeiseverguetung` [€/kWh] (und `Einspeiseverguetung_KWK`) steht in
  der Rahmenzeile `Tab_ProjektWirtschaftlichkeit` — eine Zeile je Stammprojekt (`ID_Projekt` =
  Stamm; `WirtschaftlichkeitParameter.IdStamm`, `WirtschaftlichkeitCtrl.LadeParameter(idStamm)`;
  Schemaschritt 84 hat ihn aus der Trägerkarte hierher gezogen, `Allgemein/Update/VerguetungUmzug.cs`).
  `Berechne(daten, p)` rechnet jede Variante mit demselben `p`: `Erlös = PV-Überschuss × 1000 ×
  p.Einspeiseverguetung` (Flat-Pfad). Er gilt je Gruppe — wie Zins und Zeitraum (R‑1).
- *Die Dialogangaben* — Vermarktungsform, anzulegender Wert (Override), Einspeiseart,
  Inbetriebnahme, Degradation, DV-Entgelt, PPA, § 51/§ 51a, 60-%-Begrenzung, Marktwerte — stehen in
  `Tab_ProjektPhotovoltaik` (Schemaschritt 41; eindeutiger Index auf `ID_Projekt`, keine
  Löschweitergabe), im Katalog als „eine Zeile je Stammprojekt" beschrieben. Geschrieben wird sie über
  `ProjektPhotovoltaikCtrl.Speichern` mit der Id, die die Hülle hereingibt:
  `PhotovoltaikVerguetungHuelle.Gaben(idStamm)` von der Wirtschaftlichkeitsseite
  (`WirtschaftlichkeitSeiteGaben`, Stamm der Gruppe) und vom Reiter Ertrag/Bonus mit dem Eintrag der
  Klappliste „Stammprojekt:" — die alle Projekte aus `Tab_Projekt` führt, Varianten eingeschlossen
  (`KostenVorlagenUebernahmeCtrl.Projekte`), ohne Vorwahl des geöffneten Projekts
  (`ErtragBonusGaben.Bauen(komponente)` kennt keine Projekt-Id).
- *Der Rechenweg liest je Stand:* `WirtschaftlichkeitCtrl.RechnePvVerguetung(v, p, e)` holt
  `ProjektPhotovoltaikCtrl.Lies(v.IdProjekt)` — die Zeile des **jeweiligen** Projekts, Stamm wie
  Variante —, gibt sie an `PvErloesRechner.Rechne` und ersetzt den PV-Anteil des flachen Erlöses durch
  die Reihe `PV_VERGUETUNG`; fehlt die Zeile oder ist sie inaktiv, bleibt der Flat-Pfad.
  `SkaliereErtraege` skaliert danach je Stand (Best/Worst).
- *Folge:* Die „eine Vergütungswahrheit" ist eine Wahrheit **je Projekt**, nicht je Gruppe. Eine
  Variante bekommt die Dialogangaben nur, wenn sie eine eigene Zeile hat — und die hat sie genau
  dann, wenn sie **nach** der Pflege des Stamms angelegt wurde: `VariantenCtrl.AnlegenAusStamm`
  kopiert über `ProjektDuplizierenCtrl` jede Tabelle mit `ID_Projekt` (ausgenommen allein
  `Berichtskonfiguration`), also auch `Tab_ProjektPhotovoltaik` und `Tab_ProjektWirtschaftlichkeit`.
  Diese Kopie ist ein eingefrorener Stand des Anlegetags; spätere Änderungen am Stamm erreichen sie
  nicht. Eine Variante, die vor der Pflege angelegt wurde, hat keine Zeile und rechnet mit dem
  flachen Satz. Beides ist von außen nicht zu erkennen: Reiter, Dialog und Bericht sagen
  „stammprojektbezogen". Die Testdatenbank führt keine Zeile in `Tab_ProjektPhotovoltaik`; in
  `Tab_ProjektWirtschaftlichkeit` tragen die Varianten 1023 und 1024 (Stamm 1019) Kopien, die kein
  Rechenweg liest.
- *BHKW-Vergütung, zum Vergleich:* Die KWKG-Größen (Satz Eigen/Einspeisung, Vbh-Kontingent,
  Jahresdeckel, Anlagenart, Eigenstromfall, Stichtag, Inbetriebnahme, Kostenanteil) stehen an der
  Anlage (`Tab_Energieanlagen.KWKG_*`); Anlagen gehören dem Projekt (`BhkwAnlagen(v.IdProjekt)`), die
  Variante hat ihre eigenen — beim Anlegen kopiert, seither eigenständig. Der BHKW-Dialog zeigt die
  Anlagen der ganzen Gruppe (`KwkgAnlagenCtrl.LadeGruppe(idStamm)`). Nur der flache KWK-Einspeisesatz
  und die projektweiten KWKG-Angaben (Stichtag, Abschlag bei negativen Preisen, Pauschale) liegen in
  der Rahmenzeile je Gruppe. Das BHKW erfüllt die Anforderung für den Zuschlag also von selbst, weil
  er anlagenscharf ist; die Regel „Vergütung je Stand, Rahmensatz je Gruppe" ist dort schon Praxis.

**Soll:**

| Aspekt | Festlegung |
|---|---|
| Auswahl | je **Variante** eine Wahl: **„vom Stammprojekt übernehmen"** (Vorgabe) oder **„eigene Vergütung"**; der Stamm führt immer eigene Werte |
| Vorgabe | **übernehmen** — ergebnisneutral für jede neue Variante; für den Bestand leitet ein Schemaschritt die Wahl aus den Daten ab (Randfälle) |
| Persistenz | eine Spalte `Tab_ProjektPhotovoltaik.Uebernahme_Stamm` (INTEGER, `CHECK (Uebernahme_Stamm IN (0,1))`, nullbar): 1 = übernommen, 0 = eigene Werte; **keine Zeile** = übernommen (Vorgabe jeder neuen Variante). Die Zeile der Variante bleibt bei „übernehmen" stehen — sie ist der Rückweg zu den eigenen Werten. Kein DDL-DEFAULT (Hausregel der Tabelle); die Spalte steht an beiden DDL-Orten (SchemaMigration **und** `StelleTabellenSicher`) |
| Warum nicht „NULL je Spalte = übernommen" | die Spalten der Tabelle bedeuten mit NULL schon „nicht gepflegt / Rückfall" (DV-Entgelt, Ausfallanteil, Marktwert); ein zweiter NULL-Sinn wäre eine Zweideutigkeit je Feld. Und eine Vergütung ist ein Block: Vermarktungsform, anzulegender Wert, Inbetriebnahme und § 51 gehören zusammen — Felder aus zwei Projekten zu mischen ergäbe eine Vergütung, die niemand eingegeben hat (VV‑Q2) |
| Auflösung | eine Methode `ProjektPhotovoltaikCtrl.LiesAufgeloest(idProjekt)` → (Modell, Herkunft): Stamm → eigene Zeile; Variante mit `Uebernahme_Stamm = 0` → eigene Zeile; sonst → Zeile des Stamms (`VariantenCtrl.StammRefDerVariante`), Herkunft „übernommen von ‹Stamm›". Fehlt auch dem Stamm die Zeile, gilt der Flat-Pfad wie heute |
| Rechenwirkung | `RechnePvVerguetung` liest die aufgelöste Zeile statt `Lies(v.IdProjekt)`; `PvErloesRechner`, `SkaliereErtraege` und der Flat-Pfad bleiben unverändert; der Stamm rechnet wie heute. Der flache Satz `Einspeiseverguetung` bleibt in der Rahmenzeile je Gruppe (VV‑Q5) |
| Anzeige — Reiter Ertrag/Bonus | Optionsgruppe „Vergütung: ○ vom Stammprojekt übernehmen ● eigene Vergütung" mit Erklärzeile („übernommen von ‹Stamm› · anzulegender Wert ‹AW› ct/kWh, ‹Vermarktungsform›" bzw. „eigene Werte dieser Variante"); beim Stamm statt der Optionsgruppe die Zeile „Stammprojekt — ‹n› Varianten übernehmen diese Vergütung"; die Klappliste „Stammprojekt:" bleibt für den Admin-Kontext (Kostendialog ohne Projekt, VV‑Q7); die Knöpfe „PV-Vergütungsdialog öffnen…" und „Gesetzesparameter…" bleiben |
| Anzeige — Vergütungsdialog | öffnet für die Variante; bei „übernommen" mit Hinweiszeile „übernommen von ‹Stamm› — Felder gesperrt" und Knopf **„eigene Werte"**, der die Stammwerte in die Zeile der Variante kopiert und `Uebernahme_Stamm = 0` setzt (VV‑Q6); „Übernehmen" schreibt die Zeile des geöffneten Projekts |
| Bericht | Block A9 (`WirtschaftlichkeitZeilen`, Zeilen PV_FORM/PV_AW) bekommt eine Nachweiszeile „PV-Vergütung: Herkunft" je Spalte — „eigene Werte" / „übernommen von ‹Stamm›"; die Herkunft reist im Nachweisumschlag der Ergebniszeile (`ErgebnisNachweisUmschlag`), keine neue Ergebnisspalte; Word und Excel lesen dieselbe Zeilendefinition |
| ValERI | die Vergütung ist Bestandteil der Maßnahme; ob eigene oder übernommene Werte, ändert an der Bewertung nichts — keine Änderung |
| Kohärenz (§ 3.9) | Warnzeilen ohne Rechenwirkung: „Variante ‹x› führt eine eigene, inaktive PV-Vergütung; der Stamm eine aktive" — die Spur der Bestandsableitung (VV‑Q4); „Variante ‹x› übernimmt, der Stamm ist nicht angewendet"; „Variante ‹x› führt ‚übernehmen', hat aber kein Stammprojekt — keine Vergütungszeile, Flat-Pfad" (Verknüpfung ins Leere oder Wahl ohne Verknüpfung) |

**Randfälle, ausdrücklich geregelt:**

- Stamm ändert seine Vergütung → alle übernehmenden Varianten folgen im nächsten Lauf (sie lesen
  die Stammzeile), eigene bleiben unberührt.
- Variante wird Referenz (§ 2.9) → die Herkunft ihrer Vergütung ändert sich nicht; die
  Referenzrechnung nimmt die aufgelöste Zeile wie jede andere.
- Variante aus dem Stamm angelegt (`VariantenCtrl.AnlegenAusStamm`) → sie erbt „übernehmen": Der
  Kopierlauf lässt `Tab_ProjektPhotovoltaik` aus (feste Ausnahme wie `Berichtskonfiguration`) —
  keine eingefrorene Kopie mehr. Variante aus einer anderen Variante angelegt (Quelle ≠ Stamm) →
  dieselbe Regel, Vorgabe übernehmen.
- Bestand (Schemaschritt): Variante mit eigener Zeile → `Uebernahme_Stamm = 0` (ihre Kopie rechnet
  weiter wie heute); Variante ohne Zeile, Stamm ohne aktive Zeile → nichts zu tun (übernehmen,
  Flat-Pfad beiderseits); Variante ohne Zeile, Stamm mit aktiver Zeile → eigene Zeile `Aktiv = 0,
  Uebernahme_Stamm = 0` — ergebnisneutral, mit Kohärenzhinweis (VV‑Q4).
- Projekttransfer (`ProjektExportImportCtrl`) → **umgesetzt.** Der Transfer nimmt
  `Tab_ProjektPhotovoltaik` mit (`Transferplan` = Kopierplan des Duplizierers plus diese eine
  Tabelle; die Ausnahmeliste des Duplizierers bleibt, sie gilt dem Anlegen einer Variante in
  DERSELBEN Datenbank). Ein Variantenbaum bringt damit Stamm und Wahl mit, und es gibt nichts
  beizulegen. Eine **einzeln** transferierte Variante mit „übernehmen" findet im Ziel keinen
  Stamm; ihr Paket trägt deshalb die geltende Zeile des Stamms als eigenen Abschnitt
  (`pvVerguetungStamm` im Manifest, `pvstamm/<i>.json`) — nicht als Zeile der Tabelle des
  Stamms, denn der Stamm reist nicht. Scheitert die Verknüpfung am Ziel, macht der Import die
  Beilage zu den **eigenen** Werten der Variante (`Uebernahme_Stamm = 0`, umgeschlüsselt auf die
  neue Projekt-Id) und nennt es in der Importmeldung; steht die Verknüpfung (Stamm im Paket oder
  am Ziel), bleibt die Beilage liegen und die Variante übernimmt wie zuvor. Fehlt dem Stamm eine
  aktive Zeile, gibt es keine Beilage: Die Variante bleibt ohne Zeile, die Importmeldung nennt den
  Flat-Pfad, und die Kohärenzprüfung wiederholt ihn bei jedem Lauf. Ein Paket ohne den Abschnitt
  lädt unverändert — er ist kein Pflichtteil des Manifests.
- Löschen des Stamms (`ProjektCtrl.Delete` löst die Verknüpfungen in `Tab_Variante`) → jede
  übernehmende Variante erhält zuvor die Stammwerte als eigene Zeile (`Uebernahme_Stamm = 0`); nie
  stiller Verlust der Vergütung. Löschen einer Variante → ihre Zeile fällt mit (heute bleibt sie
  verwaist, weil die Tabelle keine Löschweitergabe hat — im selben Schritt nachrüsten).
- Zwei Varianten, beide „übernehmen", der Stamm inaktiv → beide rechnen Flat wie heute; der
  Kohärenzhinweis nennt es.

**Abnahmekriterium:** Alle Bestandsvarianten auf „übernehmen" (Testdatenbank: keine Zeile in
`Tab_ProjektPhotovoltaik`, weder beim Stamm noch bei einer Variante) → Referenzlauf **byte-gleich**;
die Wahl „eigene Vergütung" mit einer Zeile, die der Stammzeile wertgleich ist → dieselben Zahlen wie
„übernehmen" (Tests auf `WirtschaftlichkeitErgebnis.PvAnzulegenderWert` und `EinspeiseerloesPvJahr`);
Stammänderung → die übernehmende Variante folgt, die eigene nicht (Test).

**Was die Umsetzung braucht:**

1. Schemaschritt **93**: Spalte `Uebernahme_Stamm` an beiden DDL-Orten, Ableitung der Wahl aus
   dem Bestand (Randfälle), Löschweitergabe `Tab_Projekt → Tab_ProjektPhotovoltaik` — sie steht
   als Vorarbeit in `ProjektCtrl.Delete`, siehe Einordnung;
2. `ProjektPhotovoltaikCtrl.LiesAufgeloest` und `Speichern` mit der Spalte;
   `ProjektDuplizierenCtrl`: `Tab_ProjektPhotovoltaik` in die feste Ausnahmeliste; `ProjektCtrl.Delete`
   und `VariantenCtrl.LoescheVariante`: Übernahme in eigene Zeilen vor dem Lösen;
3. `WirtschaftlichkeitCtrl.RechnePvVerguetung` auf die Auflösung; Herkunft im Nachweisumschlag;
   Zeile in `WirtschaftlichkeitZeilen` (Word, Excel); `KohaerenzPruefung`;
4. Dialoge: Optionsgruppe und Erklärzeile in `ErtragBonus.razor`, Projekt-Id in
   `ErtragBonusGaben.Bauen` (die Vorwahl ist das geöffnete Projekt, nicht das erste der Liste),
   Hinweiszeile und Knopf „eigene Werte" in `PhotovoltaikVerguetungDialog.razor`; die Hülle
   (`PhotovoltaikVerguetungHuelle`) öffnet für das gewählte Projekt; auf iOS war der Dialog **aus
   zwei Gründen** nicht erreichbar. **Stand: umgesetzt #431** — beide sind gefallen:
   `PhotovoltaikVerguetungHuelle` liegt jetzt plattformfrei in `EPOS.UI.Daten/Wirtschaftlichkeit/`,
   und `IosProjektQuelle.BerichteKostenGaben` liefert nicht mehr `null` (erreichbar als Überlagerung
   der Wirtschaftlichkeitsseite über `BERICHTE_KOSTEN`); ein bestätigender `ios.yml`-Lauf steht noch
   aus;
5. Ressourcen (beide Sprachen): Optionsgruppe, Erklärzeilen, Hinweiszeile, Knopf, Nachweiszeile,
   Kohärenztext;
6. Tests: Auflösung (Stamm; Variante eigene; Variante übernommen; Stamm ohne Zeile), Kopierlauf ohne
   PV-Zeile, Löschweg, Ableitung aus dem Bestand, bunit für beide Zustände der Optionsgruppe,
   Referenzlauf;
7. Wiki: `Programm Dokumentation - Wirtschaftlichkeit.wiki` (Vergütungsdialog, Herkunftszeile) und
   `Programm Dokumentation - Varianten.wiki` (was eine Variante vom Stamm übernimmt); Logbuch-Eintrag
   mit der Veröffentlichung.

**Fragen mit Empfehlung — entschieden am 18.09.2026 („VV‑Q1 bis VV‑Q7: Empfehlung"):**

| Frage | Empfehlung |
|---|---|
| **VV‑Q1** Gilt dieselbe Regel für die BHKW-Vergütung? | **Ja, sinngemäß — und dort ist sie schon erfüllt:** der KWKG-Zuschlag ist anlagenscharf, also je Stand; der flache KWK-Einspeisesatz bleibt wie der flache PV-Satz in der Rahmenzeile je Gruppe. Keine Änderung am BHKW in dieser Etappe. Einheitliche Regel: *Vergütung folgt der Anlage bzw. dem Stand, Rahmensätze folgen der Gruppe* |
| **VV‑Q2** Einzelwerte übernehmbar oder nur der ganze Block? | **nur der Block** — die Felder bedingen einander (die Vermarktungsform bestimmt, welche Felder gelten; § 51 hängt an Inbetriebnahme und Leistung); ein Mischsatz ist keine gepflegte Vergütung |
| **VV‑Q3** Anzeige der Wahl im Kostendialog (Reiter Ertrag/Bonus) oder nur im Vergütungsdialog? | **an beiden Orten, gewählt an einem:** die Optionsgruppe sitzt im Reiter (dort ist die Variante im Blick, dort fragt der Anwender); der Vergütungsdialog zeigt die Herkunft als Hinweiszeile und bietet nur den Weg „eigene Werte". Der Reiter bleibt Anzeige plus Wahl, kein zweiter Rechenweg |
| **VV‑Q4** Bestandsvarianten ohne eigene Zeile bei aktiver Stammzeile: ergebnisneutral (eigene, inaktive Zeile) oder übernehmen (Rechenwirkung)? | **ergebnisneutral**, mit Kohärenzhinweis; der Anwender schaltet je Variante mit einem Klick auf „übernehmen". Ein Schemaschritt, der Zahlen ändert, ohne dass jemand gewählt hat, verletzte die Regel „Vorgabe ergebnisneutral" |
| **VV‑Q5** Auch der flache Satz `Einspeiseverguetung` der Rahmenzeile je Variante? | **nein** — er ist ein Rahmenparameter wie Zins und Zeitraum (R‑1) und der Rückfall, wenn kein Dialog aktiv ist; wer je Variante vergüten will, tut es im Dialog |
| **VV‑Q6** Was lädt „eigene Werte" vor: die Stammwerte oder die Vorbelegung des Controllers? | **die Stammwerte** — der Anwender will eine Abweichung von einer bekannten Basis, keinen leeren Satz; die Zeile trägt `GeaendertAm` |
| **VV‑Q7** Klappliste „Stammprojekt:" im Reiter: umbenennen? | **„Projekt:"** — sie führt alle Projekte; im Projektmodus des Kostendialogs ist das geöffnete Projekt vorgewählt, und die Liste entfällt |

**Einordnung:** Eigene kleine Etappe, unabhängig von § 2.9 und § 2.15 — sie ändert weder Referenz
noch Sicht, nur den Leseweg einer Größe je Stand; ergebnisneutral in der Vorgabe. Mockup:
Kategorie 3 (Reiter Ertrag/Bonus) unter `../Mockups/Dialog_Formel_Zahlenprobe.html#pvkosten` und
Kategorie 6 (Kopfzeile) unter `../Mockups/Dialog_Formel_Zahlenprobe.html#pv`; Umsetzungsstand U38.

**Drei Stellen, an denen die Umsetzung von der Soll-Tafel abweicht — benannt, nicht still:**

- **Die Löschweitergabe `Tab_Projekt → Tab_ProjektPhotovoltaik` ist kein Schemateil.** SQLite
  kann einer bestehenden Tabelle keinen Fremdschlüssel anhängen; ein Tabellenneubau allein
  dafür wäre eine zweite Wahrheit neben `ProjektCtrl.Delete`, dem einen Weg, durch den jedes
  Projektlöschen läuft (auch `VariantenCtrl.LoescheVariante` endet dort). Und die Übernahme
  der Stammwerte, die der Randfall verlangt, könnte ein `ON DELETE CASCADE` ohnehin nicht
  leisten. Beides steht deshalb in `ProjektCtrl.PvVerguetungAufloesen` — erst die Übernahme,
  dann das Löschen der eigenen Zeile.
- **NULL an einer vorhandenen Zeile liest sich als „eigene Werte", nicht als „übernommen".**
  Eine Zeile, die da ist, hat gegolten; das ist der Bestand vor dem Schritt, und dieselbe
  Antwort gibt eine Datenbank, der die Spalte noch fehlt. „Übernommen" bleibt der Zustand
  **ohne** Zeile oder mit ausdrücklichem Kennzeichen 1. Das DML des Schritts schreibt dieses
  NULL einmalig als 0 fest; die Lesart bleibt als tolerante Rückfallebene.
- **Der Transfer hat einen eigenen Plan.** `Tab_ProjektPhotovoltaik` steht in der
  Ausnahmeliste des Duplizierers, und dieselbe Liste filtert `ErmittlePlan` — sie allein
  ließe die Zeile in keinem Transferpaket mitreisen, auch nicht die eines Stammprojekts. Die Liste bleibt, wie
  sie ist: Sie gilt dem ANLEGEN einer Variante, bei dem Stamm und Variante in derselben
  Datenbank liegen und die Wahl „übernehmen" genügt. Der Transfer trägt das Projekt in eine
  andere Datenbank, in der es nichts zu übernehmen gibt, und nimmt die Tabelle deshalb über
  `ProjektExportImportCtrl.Transferplan` zusätzlich mit — Export, Import und das Löschen beim
  Überschreiben lesen denselben Plan.

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
A_t [€] = Betrieb_t     × (1 + p_B)^(t−1)
        + Energie_1     × (1 + p_E)^(t−1)
        + Endenergie_1  × (1 + p_E)^(t−1)     ← eigener Topf (Energiekosten je Anlage, B7)
        + CO2_t
        + Ersatz_t      = A₀ × (1 + p_I)^t    ← Preisindizierung der Ersatzbeschaffung (W5‑B‑12)

E_t [€] = Einspeiseerlös_1                    ← nominal KONSTANT, keine Steigerung
        + Σ über alle benannten Erlösreihen: Reihe.Wert(t)
```

Die Preissteigerung der Ersatzbeschaffung **p_I** und die Nullsemantik stehen in
[`../Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md`](../Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md)
§ 10; der Endenergie-Topf wird nur angesetzt, wenn er von 0 verschieden ist, und trägt dieselbe Rate
p_E wie der Energietopf.

Rahmengrößen aus `Tab_ProjektWirtschaftlichkeit`, **eine Zeile je Stammprojekt**:

| Größe | Spalte | Vorgabe |
|---|---|---|
| Kalkulationszins i | `Zinssatz` [%] | 3,0 |
| Betrachtungszeitraum T | `Betrachtungszeitraum` [a] | 20 |
| Preissteigerung Energie p_E | `Preissteigerung_Energie` [%/a] | 0,0 |
| Preissteigerung Betrieb p_B | `Preissteigerung_Betrieb` [%/a] | 0,0 |
| Preissteigerung Investition/Ersatz p_I | `Preissteigerung_Investition` [%/a] (Schritt 72) | NULL = 0,0 |
| Einspeisevergütung PV | `Einspeiseverguetung` [€/kWh] | 0,0 |
| Einspeisevergütung KWK | `Einspeiseverguetung_KWK` [€/kWh] | NULL = aus |
| CO₂-Preis-Override | `CO2_Preis` [€/t] | 0 = Katalogpfad |

**Drei Preissteigerungsreihen** (p_B, p_E, p_I) — keine je Träger, keine je Position.

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

Lesepunkt: `Tab_ProjektWerte` mit `KategorieID = 1`, **ohne `ORDER BY`**; die Runde 3 friert ihre Basiszeilen vorher ein und ist deshalb reihenfolgeunabhängig (Befund I-3 erledigt).

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
| `EUR_PRO_KWP` | Σ (Modulanzahl × Modulleistung)/1000 × Satz | `PhotovoltaikCtrl.KwpSumme` |
| `EUR_PRO_KWH_KAPAZITAET` | Σ Energie × Satz | `Tab_Stromspeicher.Energie` |
| `EUR_PRO_M2_KOLLEKTOR` | Σ (Aperturfläche × Modulanzahl) × Satz | Solarthermie |
| `EUR_PRO_KW_LEISTUNG` am Pufferspeicher | Σ Gesamtvolumen × Satz [€/Ltr.] | `Tab_Pufferspeicher.Gesamtvolumen` |

Art ↔ Gewerk wird gekreuzt geprüft: falsches Paar ⇒ **null, keine Fantasiezahl**. Der
Pufferspeicher bemisst sich allein an seinem **Volumen**; eine kWh-Kapazität führt er nicht
(ohne Temperaturpaar keine belastbare kWh), und `EUR_PRO_KWH_KAPAZITAET` liefert dort null.

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

**Der eine Rechenweg** — fehlt Menge **oder** Satz, ist die Ableitung nicht rechenbar; dann gilt
der **erfasste Betrag** (Anwenderentscheid I-2, 30.08.2026). Eine ermittelte Menge 0 rechnet weiter
zu 0.

| Gruppe | Bemessungen | Formel |
|---|---|---|
| A absolut | `BETRAG`, `JAHRESBETRAG` | Betrag = EingegebenerWert |
| B Prozent | alle `PROZENT_*` | Betrag = Menge × Satz / 100 |
| C Produkt | alle `EUR_PRO_*` | Betrag = Menge × Satz |

**Vorrangordnung der Bezugsmenge** (frisch vor Konserve, H2-1):

1. Szenariowert gepflegt → keine Ableitung
2. `BETRAG`/leer → gespeicherter Wert
3. **Endenergie-Arten** (`PROZENT_ENDENERGIEKOSTEN`/`_BEDARF`): Menge **immer frisch** aus dem
   jüngsten Lauf; Auflöser null ⇒ **erfasster Betrag** (I-2) — die Konserve greift nie
4. **Rückfall-ermittelbare Arten** (10 Stück): frisch versuchen, Konserve nur bei null;
   `EUR_PRO_H` und die beiden `EUR_PRO_KWH_*` sind seit FX2 frisch
5. **Nur Konserve** bleiben `PROZENT_BRENNSTOFFKOSTEN` und `PROZENT_STROMKOSTEN` (Rest von
   Befund B-4)

**Endenergie je Komponente** (`EndenergieAufloeser`, „jüngster Lauf" = höchste `Tab_Ergebnis.ID`):

| Komponente | Endenergie | Formel |
|---|---|---|
| BHKW | Brennstoff | Bedarf = Σ Verbrauch × 1000; Kosten = Bedarf × Arbeitspreis(CarrierId) |
| Heizkessel (Brennstoff) | Brennstoff | ebenso; `Verbrauch` trägt den Brennstoffeinsatz des Laufs (**B-1**) |
| Heizkessel (**Elektrokessel**, Gerät mit `Brennstoff` = 13) | Strom | Bedarf = Σ (Waerme_Gas + Waerme_Oel) × 1000 — seine Nutzwärme, denn der Rechenkern führt ihn mit Nutzungsgrad 1; Kosten = Bedarf × Strompreis. Seine Modulzeile bleibt bei `Verbrauch` = 0, weil die Energie im Netzbezug steht (Regel **E1**) |
| Wärmepumpe | Strom | Bedarf = Σ (Stromverbrauch + Heizstab) × 1000; Kosten = Bedarf × Strompreis |
| PV · Solarthermie · Speicher | keine | null — nur Jahresbetrag zulässig |

**Zwei Welten am selben Gewerk, nie eine Summe:** Trägt ein Projekt Brennstoff- **und**
Elektrokessel, weist der Auflöser sie getrennt aus — anlagenscharf gilt die Welt dieses Kessels,
als Komponentensumme die Brennstoffwelt und nur bei reinen Elektrokesselprojekten die Stromwelt.
Brennstoff und Netzbezugsstrom sind zwei Energieformen mit zwei Preisen; eine gemeinsame Zahl wäre
für beide Bemessungen keine Basis, sondern eine Vermengung.

Preis = `PreisArbeit / EffHi`, **ohne** Grund- und Leistungspreis.

**Der Strompreis einer Anlage ist der ihres eigenen Trägers** (Anwenderentscheid
19.09.2026). Trägt eine Anlage, die selbst Strom bezieht — Wärmepumpe, Heizstab,
Elektrokessel —, einen eigenen `ELECTRICITY`-Träger (`Tab_Energieanlagen.ID_Carrier`, dem
Projekt zugeordnet), bewerten **beide** Wege ihre Endenergie mit dessen Arbeitspreis; sonst
gilt der Stromträger des Projekts (Rangfolge `StromTraegerDerAnlagen`). Eine Anlage mit
Brennstoffträger bewertet ihren Hilfsstrom weiter mit dem Projekt-Stromträger — ihr eigener
Träger ist Brennstoff und wäre für eine Strommenge der falsche Preis. Regel **E1** bleibt
davon unberührt: Der Strom des Elektrokessels ist weiterhin nur SICHTBAR und wird genau
einmal bezahlt, im Netzbezug.

**Weg B braucht keine zweite Formel:** Der Auflöser übergibt den **bewerteten** Bedarf
(kWh × Strompreis der Anlage), weil `Menge × Satz/100 × Preis` dasselbe ist wie `(Menge × Preis) × Satz/100`.
Die Sätze von A und B sind **nicht austauschbar** — Faktor ≈ 3,4, das Preisverhältnis Strom zu
Brennstoff.

**Erlöse:** `IstErloes && wert > 0 → wert = −wert`, an drei Stellen identisch geklemmt.

**Vorrangregel Prozent vor Absolut** (aus KONTEXT § 5.3): Eine gepflegte Satzangabe schlägt den
Absolutbetrag; das unterlegene Feld wird **gesperrt, nicht geleert** (KL4) — anders als in der
Altanwendung, die beim Speichern die Absolutfelder leerte (stiller Datenverlust, Altbefund 6). Die
„oder"-Doppelfelder der Wartung (€/kWh_el neben €/h, dort tatsächlich **addiert**, Altbefund 7)
gibt es nicht mehr: **eine** Position mit sichtbarer Bemessungswahl.

**Basis „% der Investition" auf der Betriebsseite** (`InvestSummeFuer`): Summe der
**Investitionskaskade** (`InvestKaskade.Summen`), stufig Anlage → Komponente → Projekt, **vor**
Zuschussabzug — die abgeleiteten Beträge sind seit W5‑B‑8 enthalten (Befund B-5 erledigt).

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

**Leistungspreis der Brennstoffträger** — die einzige η-Division der Kostenkette; Basis ist die
vorgehaltene Anschlussleistung aus den Gerätedaten:

```
kw = BHKW:  (P_el + P_therm) / η_gesamt        Kessel:  P_therm / η
     (η > 1,5 gilt als Prozentangabe ÷ 100; außerhalb (0;1,5] wird die Anlage übersprungen)
Saisonreihe (12 Monatssätze) vor konstantem Satz
Modus JAHR:  Satz × kw          Modus MONAT:  Satz × kw × 12
kw ≤ 0 ⇒ kein Leistungspreis
```

**Netzbezug Strom:** `StromkostenNetz = Stromrestbedarf × 1000 × Preis + Grundpreis +
Leistungsanteil`. Der Leistungsanteil des Stromträgers bemisst sich an der **gemessenen
Bezugsspitze**, nicht an einer Anlagenleistung — ein Stromanschluss wird nach der
registrierten Leistung abgerechnet:

```
Spitze = Maximum der VIERTELSTUNDENreihe des Netzbezugs (dieselbe Reihe, die der Speicher
         kappt); das Stundenmittel (StromMatrix.MaxBezugKW) glättet die Spitze und bleibt
         der Tarifstruktur vorbehalten
Modus JAHR:  Satz × Jahresspitze       Modus MONAT:  Σ₁₂ (Monatsspitze × Satz)
Saisonreihe vor konstantem Satz:       Σ₁₂ (Monatssatz × Monatsspitze)
Satz 0 / nicht gepflegt ⇒ kein Anteil; ohne Zeitreihen ⇒ kein Anteil, der Träger wird benannt
```

Im Tarifmodus ersetzt der Zonen- oder Rollenbetrag den **ganzen** Flat-Anteil samt
Leistungsanteil.

**Kein Aufschlag — die Anteile zerlegen den Arbeitspreis.** Die Preisanteile der
Trägerkarte („Strompreis Details": Beschaffung, Vertrieb, Netzentgelt, Stromsteuer,
Konzessionsabgabe, Umlagen; „Preisbestandteile" beim Brennstoff: Energiesteuer,
CO₂-Bestandteil, Netz- und Messentgelt, Beschaffung und Vertrieb) sagen, WORAUS der
Arbeitspreis besteht; sie kommen nicht auf ihn. Es gibt genau eine Preiswahrheit, und das
ist der Arbeitspreis des Trägers:

```
Σ aktive Anteile   =  Arbeitspreis        (Kohärenzzeile, nie Summand)
Σ ohne Beschaffung →  Spot-/Profilreihe    (dort IST die Reihe die Beschaffung)
```

Ein Anteil, der nicht gepflegt ist, ist 0 und inaktiv; die Vorschlagswerte stehen im Feld
und werden erst auf Knopfdruck übernommen.

**Die Anzeigekante der Anteile.** Gerechnet, gespeichert und geprüft werden die Anteile in
**ct/kWh** — eine Größe, die für jeden Träger dieselbe Bedeutung hat. **Angezeigt und
eingegeben** werden sie in der **Abrechnungseinheit** des Trägers (€/m³, €/l, €/t): Wer
einen Gaspreis pflegt, pflegt ihn je Kubikmeter. Die Einheit wechselt **genau einmal**, an
der Anzeigekante, über den Heizwert:

```
€ je Abrechnungseinheit = ct/kWh ÷ 100 × H_i        (EnergietraegerPreiskarte.AnteilJeEinheit)
ct/kWh                  = € je Einheit × 100 ÷ H_i  (…AnteilCtKwh)
```

Ohne Heizwert gibt es keinen Weg dorthin — dann bleibt ct/kWh stehen, und eine leise Zeile
nennt den Grund. Die **Kohärenzzeile prüft**: Σ der aktiven Anteile gegen den Arbeitspreis,
Toleranz **0,0001 €/kWh**; darüber steht sie auf „≠" und nennt den Abstand. Je Zeile steht
die Herleitung darunter — die Energiesteuer mit ihrem **brennwertbezogenen** Katalogsatz
(„5,50 €/MWh (H_s)"), der CO₂-Bestandteil als Preis × CO₂-Masse je Abrechnungseinheit
(„65 €/t × 2,109 kg/m³", aus 200,9 g/kWh × 10,5 kWh/m³ ÷ 1000).

**Die Preisbasis ist von den Umrechnungsregeln entkoppelt** (Befund `UR-1`). Sie bietet
genau zwei Einträge: die **Abrechnungseinheit** (Faktor 1) und die **Kilowattstunde**, und
deren Faktor ist der **Heizwert** — nicht der `factor` einer `energy_conversion`-Zeile. Die
Regeln prüfen weiterhin die Einheitenkette (`EnergieEinheitenPruefung`) und stellen die
`ID_Umrechnung` der Projektzeile; gerechnet wird mit H_i und H_s.

**CO₂ / BEHG** als eigene Reihe:

```
behgBasisT [t/a] = CO2Brennstoff + (ohne Nachhaltigkeitsnachweis) BiogenBehgMenge × BehgOhneNachweis/1000
BEHG_t [€]       = behgBasisT × CO2-Preis(Kalenderjahr)
```

Preis: Override `CO2_Preis > 0` (dann mit p_E fortgeschrieben), sonst Katalogpfad
(2021–25: 25/30/30/45/55 · 2026/27: 65 · ab 2028: 80 als Prognose).
⚠ **Bedeutungsumkehr seit K6: 0 heißt „Pfad", nicht mehr „aus".**

Die Abgabe entsteht allein aus dem Brennstoff der Anlagen — Kessel wie BHKW —, nie aus der
Photovoltaik: Eine Variante, die neben der Photovoltaik den Kessel behält, trägt dessen Abgabe
weiter (Kategorie 4 des Mockups: 2.056,7 MWh × 200,9 g/kWh × 65 €/t = 26.857 €/a, Stammprojekt wie
Variante „Photovoltaik"); die Photovoltaik selbst trägt keine CO₂-Kosten.

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

**Die Rückfallkette Anlage → Projekt ist mit Schemaschritt 89 entfallen** (Entscheid `BK-E-1` (a)).
Gerechnet wird ausschließlich, was an der Anlage steht; `NULL` heißt dort jetzt 0 und nicht mehr
„Projektwert":

```
Satz_Einspeisung(A) = A.KWKG_Satz_Einspeisung ?? 0
Satz_Eigen(A)       = A.KWKG_Satz_Eigen ?? 0,  geprüft gegen A.KWKG_Eigenstromfall
Kontingent(A)       = A.KWKG_Vbh_Kontingent > 0 ? dieser : § 8 aus A.KWKG_Anlagenart und A.KWKG_Kostenanteil
Deckel(A)           = A.KWKG_Vbh_Jahresdeckel > 0 ? dieser : Staffel § 8 Abs. 4
```

Möglich wird das durch den **Datenschritt 89**: Er trägt in jede BHKW-Anlagenzeile, die an der
betreffenden Stelle leer ist, den Projektwert nach — genau den, den der Rückfall ihr zugewiesen
hat (Sätze, Kontingent, Jahresdeckel, Anlagenart, **Tatbestand**, Kostenanteil, Stichtag,
Inbetriebnahme). Der Schritt ist damit ergebnisneutral und idempotent.

**Die Strenge des Eigenstromsatzes wandert mit.** Bis BK1 galt an der Anlage: gepflegter Satz ohne
Tatbestand ⇒ 0. Diese Strenge stand auf der Annahme, ein Anlagensatz sei eine ausdrückliche
Eingabe, die es im Bestand nirgends gibt — mit Schritt 89 ist sie hinfällig, weil jede
Bestandsanlage einen Satz bekommt, den niemand an ihr eingegeben hat. Es gilt deshalb je Anlage
genau die Regel, die K6 am Projekt eingeführt hat: **leerer Tatbestand ⇒ Satz bleibt, Meldung
„ungeprüft"; nur die ausdrückliche Wahl `KEINER` nimmt ihn weg.** Hätte die alte Strenge Bestand,
nähme Schritt 89 jedem Bestandsprojekt ohne gepflegten Tatbestand den Eigenverbrauchszuschlag —
eine Rechenwirkung, die nirgends entschieden wurde.

**Vbh-Kontingent § 8, je Anlage:** Override > 0 gewinnt; sonst aus **ihrer** Anlagenart und
**ihrem** Kostenanteil — neu 30.000 h · modernisiert ab 50 %/25 % → 30.000/15.000 · nachgerüstet ab
50/25/10 % → 30.000/15.000/10.000; darunter 0 mit Fehlgrund. Der Ersatzweg leitet das Kontingent
**ebenfalls je Anlage** ab und mischt es leistungsgewichtet (BK1a); er greift, wenn sich Anlagen-
und Ergebniszeilen nicht zuordnen lassen.

**Der Aktivierungsschalter fragt die Anlagen.** „Ist der KWK-Zuschlag dieser Gruppe aktiv?" heißt
seit BK1: führt **irgendeine** BHKW-Anlage der Gruppe einen Satz > 0? Die Regel steht **einmal** in
`KwkgAktivierung` und wird von allen sechs Stellen dort geholt (Rechenkern, Word-Baustein,
Excel-Erzeuger, Nachweiszeile, Kapitalwert-Verlaufshülle, Wirtschaftlichkeitsseite).

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

#### Der Ersatzweg: eine leistungsgewichtete virtuelle Gesamtanlage (BK1a)

Lassen sich Anlagen- und Ergebnismodulzeilen nicht paaren (kein Modulsatz, oder Namen und Anzahl
passen nicht zusammen), fehlt die Zuordnung **Menge → Anlage** — die **Anlage** fehlt nicht. Der
Ersatzweg bildet deshalb aus den BHKW-Anlagen **eine** virtuelle Gesamtanlage. Gewicht ist die
elektrische Nennleistung, `g_i = P_el,i`, `G = Σ g_i`:

```
SatzEigen   = Σ g_i × SatzEigen(a_i)                              / G     (Tatbestand je Anlage)
SatzEinsp   = Σ g_i × SatzEinsp(a_i)                              / G
Kontingent  = Σ g_i × (Kontingent(a_i) > 0 ? Kontingent(a_i)
                       : ableiten(Anlagenart(a_i), Kostenanteil(a_i))) / G
Deckel(t)   = Σ g_i × (Deckel(a_i) > 0 ? Deckel(a_i)
                       : Staffel(Beginn(a_i) + t − 1))               / G     JE JAHR neu
```

**Der Jahresdeckel wird je Jahr gemischt**, nicht einmal gebildet: Anlagen mit verschiedenem
Förderbeginn (`Inbetriebnahme(a_i).Jahr`, sonst der Projekt-Förderbeginn) stehen im selben
Kalenderjahr auf verschiedenen Stufen der Staffel des § 8 Abs. 4; ein einmal gebildeter Mittelwert
hätte den Verlauf eingeebnet.

**Alles Übrige bleibt die Rechnung von vorher:** `Bonus_voll`, der Negativpreis-Abschlag, der
Fallback ohne Stundenreihen, die projektweiten Vollbenutzungsstunden und die Jahresschleife. Nur
die vier Eingangsgrößen wechseln die Herkunft — vom Projekt zu den Anlagen. Bei einer Anlage, und
bei mehreren mit gleichen Werten, ist das Ergebnis deshalb dieselbe Zahl wie vorher.

**`G ≤ 0`** — keine Anlage führt eine elektrische Nennleistung: Dann wäre jede gewichtete Größe
still 0 und der Zuschlag ohne Grund 0. Stattdessen wird **arithmetisch gemittelt** und der Ersatz
benannt. Die Hinweise des Weges entstehen je Anlage und werden **lokal** ordinal entdoppelt — N
gleichartige Anlagen ergäben sonst N wortgleiche Zeilen.

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
>
> **Entschieden 18.09.2026, nach Empfehlung: Kennzeichen und Stromkennzahl je Anlage aufnehmen,
> Fall 2 rechnen.** Neuer Boden seit BK1: Der Zuschlag gehört der Anlage (Schemaschritt 89,
> § 6.5) — die zwei Felder sind zwei weitere Anlagenspalten neben den neun `KWKG_*`-Spalten von
> `Tab_Energieanlagen`, kein Umbau; nächster freier Schemaschritt ist **97** (90 BK1a,
> 91 BK1b, 92 Vergleichsprojekt, 93 Vergütung je Variante, 94 Hilfsstrom-Bemessung, 95 KL-3 Klimaspalten, 96 FK-2 Projekt-Fremdschlüssel). Das Kennzeichen
> `KWKG_Abwaermeabfuhr` (0/1, `CHECK`), die Stromkennzahl als nullbare Zahl mit **Vorschlag am
> Feld** aus P_el / P_th der Gerätezeile (`Tab_BHKW`, wo σ heute nur für die Katalogliste gerechnet
> wird) — dasselbe Muster wie die Vorschlagszeilen aus BK1. **Wo die Fallunterscheidung sitzt:**
> `WirtschaftlichkeitCtrl.ReiheJeAnlage` bildet je Anlage `stromNettoJeAnlage[i] = max(0,
> StromVon(Modul[i]) − Hilfsstrom[i])` mit `StromVon` = Klemmenerzeugung (`Stromproduktion`);
> bei gesetztem Kennzeichen tritt dort `min(Nettostromerzeugung, Nutzwärme × σ)` — die Nutzwärme je
> Modul aus Wärmeproduktion abzüglich Wärmeüberschuss, ob modulscharf im Ergebnismodell, ist vor
> der Umsetzung zu prüfen. Der Torwächter `BaueKwkgReihe` (`v.Ergebnis.BHKW.Stromproduktion`
> als Summe) bleibt. **Referenzprojekte, gemessen an der Testdatenbank:** BHKW führen 1017, 1018,
> 1024 und 1030 (und das Nichtbasisprojekt 1031); KWKG-Sätze trägt allein 1030 (8,0 / 4,0 an beiden
> Anlagen), `Betriebsart` ist überall leer, `Wärmeüberschuss` in der Basis überall 0. Das
> Kennzeichen wäre nach dem Schemaschritt nirgends gesetzt, und die Referenzbasis vergleicht nur
> Simulationsgrößen — **kein Basisprojekt ist betroffen, keine neue Basis**; nötig würde sie nur,
> wenn die Umsetzung an der Simulation oder an einer in `aggregate.csv` landenden Spalte der
> Testdatenbank etwas änderte.

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

✅ **Befund B-1 — erledigt mit B6.** § 9 Abs. 1 Nr. 3 wurde als **Erlösreihe** gebucht und
derselbe Betrag zusätzlich ausgewiesen. Die Vorschrift ist aber keine Rückerstattung: Auf selbst
erzeugten und selbst verbrauchten Strom entsteht gar keine Stromsteuer, der Vorteil steckt bereits
in der kleineren Bezugsrechnung.

**Der Modus** steht in `Tab_ProjektWirtschaftlichkeit.Stromst_Befreiung_Modus` (Schemaschritt 88,
TEXT, `AUSWEIS`/`ERLOES`, NULL = AUSWEIS) und wird im Dialog „BHKW-Wirtschaftlichkeit" gepflegt:

* **AUSWEIS (Vorgabe)** — der Betrag wird gerechnet und in der Vergleichstabelle gezeigt
  („Stromsteuer-Befreiung [€/a] (Ausweis, nicht im Kapitalwert)"), geht aber in keine Zahlungsreihe.
* **ERLOES** — jahresscharfe Erlösreihe wie zuvor, dazu die Kohärenzwarnung zur Doppelzählung.
  Richtig ist das nur, wenn der angesetzte Bezugspreis die Stromsteuer auf den Eigenverbrauch
  enthält.

Der Modus wandert mit ins Ergebnis, damit ein gespeicherter Lauf auch nach einer späteren
Umstellung sagen kann, wie *er* gerechnet hat. **Gemessen** an Projekt 1030 (432,30 MWh
KWK-Eigenverbrauch, 3 %, 20 a): Ausweisbetrag 8.862,15 €/a in beiden Modi, Kapitalwert als Erlös
−21.763.530,86 €, als Ausweis −21.895.377,28 € — die Differenz von 131.846,41 € ist genau der
Rentenbarwert der flachen Reihe. **Im Bestand bucht kein gespeicherter Lauf die Reihe** (die
Befreiung setzt Stundenreihen voraus), der Referenzlauf bleibt unverändert.

## 3.9 Kohärenzprüfung — Warnzeilen ohne Rechenwirkung

| Fall | Bedingung | Schwere |
|---|---|---|
| 1 konsistent | Wahl gesetzt, Anteil bei **jedem** beteiligten Träger ausgewiesen | **Bestätigung** (grün, ohne Betrag) |
| 2 **Entlastung ohne Belastung** | Gutschrift gebucht, Preis weist die Steuer nicht aus | Warnung **mit Betrag** |
| 3 Belastung ohne Entlastung | Anteil ausgewiesen, keine Wahl bzw. kein § 9b bei produzierendem Gewerbe | Hinweis |
| 4 Satz ≠ Katalogsatz | Toleranz 0,005 ct/kWh | Hinweis (beide Sätze) |
| 4a **Einheit nicht vergleichbar** | Katalogsatz je 1.000 kg bzw. je 1.000 l, Projekt rechnet in der anderen Einheit — ohne Dichte keine Brücke | Hinweis (ohne Betrag) |
| **Doppelzählung § 9 Abs. 1 Nr. 3** | Modus `ERLOES` bucht einen Betrag | Warnung **mit Betrag** |
| Doppelpflege Hilfsenergie | Anlagenanteil > 0 **und** aktive Kostenposition derselben Anlage | Warnung |
| **CO₂-Bestandteil im Arbeitspreis und BEHG-Reihe gleichzeitig aktiv** | der Träger weist einen CO₂-Anteil im Arbeitspreis aus **und** die BEHG-Reihe rechnet denselben Brennstoff | Warnung **mit Betrag** — **Soll, nicht gebaut** (Etappe E2) |
| Strommix-Rückfall | kein Stromträger, Netzbezug > 0 | **Laufhinweis** ohne Wertangabe, kein `KohaerenzHinweis` (`WirtschaftlichkeitCtrl.cs:5465`) |

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

Aus der Abnahmeliste der Formelkarte (die Datei ist nicht erhalten, s. Quelltabelle — die Belege stehen heute an der Codestelle). ⚠ = wirkt oder kann wirken.

## Investitionsseite

| Nr. | Befund |
|---|---|
| ✔ **I-1** | `EUR_PRO_KWP` summierte `PV_Leistung` — dieselbe Spalte heißt andernorts ausdrücklich Modulanzahl. **Erledigt mit FX1/FX2:** `PhotovoltaikCtrl.KwpSumme` rechnet Modulanzahl × Modulleistung ÷ 1000. |
| ✔ **I-2** | Abgeleitete Bemessung ohne Satz ⇒ 0 €, nicht der erfasste Betrag. **Entschieden 30.08.2026 (Anwender):** Ist die Ableitung nicht rechenbar, gilt der erfasste Betrag; eine ermittelte Menge 0 rechnet zu 0. |
| ✔ **I-3** | Runde 3 war reihenfolgeabhängig: Zwei `PROZENT_INVESTITION`-Zeilen — die zweite rechnete die erste ein; ohne `ORDER BY` entschied früher die Datenbank. **Erledigt mit FX2:** Die Runde 3 friert ihre Basiszeilen vorher ein und ist reihenfolgeunabhängig. |
| I-4 | `BaugroesseSumme` entdoppelt nicht (bei PV/Solar gewollt). |
| ✔ **I-5** | Vergleichsstrenge uneinheitlich: ZUSCHUSS ohne, `PROZENT_*` mit Groß-/Kleinschreibung. **Erledigt mit E2:** Steuerwerte sind eingefrorene ASCII-Schlüssel und werden im ganzen Kern **zeichengenau** verglichen — 78 von 79 Stellen taten das bereits, und der Betriebskostenpfad stellt dieselbe Frage als SQL-Gleichheit (SQLite vergleicht TEXT zeichengenau). Die eine tolerante Stelle (`IstZuschuss`) und ihr Gegenstück in `SpeicherAuslegungCtrl` sind nachgezogen; auf den Katalogschreibweisen des Bestands ändert das nichts. |
| I-6 | Nicht migrierte Datenbank: keine Kaskade, keine Zuschusserkennung. |

## Betriebsseite

| Nr. | Befund |
|---|---|
| ✔ **B-1** | **Die Kessel-Modulspalte `Verbrauch` blieb leer** — der Rechenkern führte den je Kessel gerechneten Brennstoffeinsatz nur auf der Anlagenzeile je Brennstoffart, und genau die Modulspalte liest die Kostenkette. Endenergie-Positionen am Kessel fielen damit auf `null` (nicht auf 0 €). Die Steuerseite umging es über den Jahresnutzungsgrad, Kosten- und Betriebsseite nicht. **Erledigt:** Die Modulzeile trägt Verbrauch, Wärmeproduktion und Brennstoff des Laufs; Modulverbrauch und Anlagensumme sind dieselbe Größe. Ausgenommen der Elektrokessel — er bucht auf den Stromzähler und steht im Netzbezug, seine Modulzeile führt bewusst 0. Seine Endenergie ist damit nicht verloren: Der Auflöser weist sie als **Stromeinsatz** aus (Regel **E1**), ohne sie ein zweites Mal zu bepreisen. |
| B-2 | Asymmetrie der Rückfälle: Endenergie-Arten unbedingt frisch, Rückfall-Arten bedingt. |
| B-3 | „Jüngster Lauf" ist die höchste ID, nicht der Zeitstempel. |
| B-4 | Zwei Arten nie frisch: `PROZENT_BRENNSTOFFKOSTEN`, `PROZENT_STROMKOSTEN` — `EUR_PRO_H` und die beiden `EUR_PRO_KWH_*` sind seit FX2 frisch. |
| ✔ **B-5** | `InvestSummeFuer` summierte `EingegebenerWert`, abgeleitete Beträge fehlten. **Erledigt mit W5‑B‑8:** Basis ist die Investitionskaskade (`InvestKaskade.Summen`). |
| B-6 | Fehler werden geschluckt (`catch {}` ⇒ still 0). |
| ✔ **B-7** | `MengenEinheit` beschriftet die neuen Arten mit „€". **Erledigt mit E2:** Die Bezugsmenge kommt aus dem `BemessungKatalog` wie der Satz — eine Leistung heißt „kW", ein Volumen am Pufferspeicher „Ltr.", eine Menge je Jahr „kWh/a". Regel: Trägt der Betriebssatz „·a", ist die Bezugsgröße ein Bestand und bleibt ohne Jahr; eine prozentuale Art bemisst sich an einem Betrag. |

## Energiekosten

| Nr. | Befund |
|---|---|
| ✔ **N1** | Kesselbrennstoff fehlte in Kosten, CO₂ und BEHG **ohne Meldung** — Folge von B-1; `kostenVollstaendig` blieb true. **Erledigt mit B-1:** Der Brennstoff steht jetzt in allen drei Größen. Trägt der Kessel keinen Energieträger, greift die allgemeine Regel „keine stillen Teilsummen": Energiekosten und CO₂ bleiben `null` mit dem benannten Grund „gehört zu keinem Energieträger" samt Ausweg — statt wie bisher vollständig auszusehen und den Kessel zu übergehen. |
| N2 | 0 beim Grundpreis gültig, 0 beim Arbeitspreis „ungepflegt" — verschiedene Regeln, beide gewollt. |
| ✔ **N3** | Ungepflegte Anteilsspalten wirkten als 11,746 ct/kWh, nicht als 0 — erledigt: Ein ungepflegter Anteil ist inaktiv und trägt 0 bei. |

## Steuern und Vergütungen

| Nr. | Befund |
|---|---|
| ⚠ **S-2** | Kein projektweites Doppelentlastungsverbot — Anlage A nach § 53 und Anlage B nach § 54 gleichzeitig möglich. |
| S-1 · ✔ **S-3** · S-4 · **S-5** | Überholte Zeilennummern älterer Protokolle · ~~§ 9-Meldung nennt Kessel „(0 kW)"~~ **erledigt mit E2** (`SteuerAnlage.Klartext` nennt an einer Anlage ohne Stromerzeugung keine elektrische Leistung) · €/GJ ohne Ho-Umrechnung (für Kohle konsistent, bleibt offen) · S-5: Der **Radius 4,5 km** bleibt Meldungstext (die Geometrie fehlt im Datenmodell); die **Erlaubnisschwelle 1.000 kW** hat mit E2 einen Leser — eine Hinweiszeile der Kohärenzprüfung ohne Rechenwerk. |
| ✔ **S-6** | `STROMST_REDUZIERT_SATZ` ungesät. **Erledigt:** Der Satz ist gesät (`GesetzKatalog.cs:1156`, Generation 7) und wird gelesen (`EnergietraegerHuelle.cs:659`); die Konstante in `StrompreisZerlegungModel` ist nur noch wertgleiche Rückfallebene. |
| ⚠ **K-1** | **Der zweite Fall des § 2 Nr. 16 KWKG fehlt** (§ 3.6): Bei Anlagen mit Vorrichtung zur Abwärmeabfuhr ist KWK-Strom `Nutzwärme × Stromkennzahl`, nicht die Nettostromerzeugung. Weder Kennzeichen noch Stromkennzahl sind im Datenmodell vorhanden; der Zuschlag fällt für solche Anlagen zu hoch aus. |
| ✔ **V-3** | Die **PV-Reihe** hatte in der Mehrjahrestabelle keine eigene Spalte — sie wirkte nur in „Netto". **Erledigt mit E2:** `Mehrjahresbild.Baue` nimmt `ErloesReihe.PV_VERGUETUNG` als Spalte auf (`WIRT_REIHE_PV`). Damit stimmt die Selbstprüfung „Summe der Positionsspalten = Netto nominal" auch dort, wo ein Projekt den Vergütungsdialog führt; die **KWKG-Pauschale** hat ihre Spalte seit U17, und die Zeile 0 geht ebenfalls auf. |
| V-1 · V-2 · V-4 | EV-Rundung (EvMix unrundet, Erlös gerundet) · § 51a bewertet mit AW statt EV · Eigen/Einspeise-Split je Anlage ist benannte Näherung. |
| R-1 · R-2 · R-3 | Rahmenparameter je Stammprojekt, nicht je Variante · Hilfsenergie steigt mit p_B statt p_E (bei gleichen Sätzen null) · ohne bestimmbare Energiekosten kein Kapitalwert (Absicht). |

---

# 5 Offene Entscheidungen K1–K11

| # | Frage | Stand |
|---|---|---|
| K1 | Feld „Deckung je Modul" | **entschieden: kein Feld** — die Befreiung ist bilanziell |
| **K2** | Hilfsenergie-Basis je Anlage: **Weg B — „% des Endenergiebedarfs" der Anlage**, bewertet mit dem **eigenen Trägerpreis der Anlage**; Wege A und C nur in der Kostenposition | **erledigt mit #365/#366** (Schemaschritt 94): Vorlage „Standard" und Dialog benennen die Basis, Feldbeschriftung „Hilfsenergieanteil [% des Endenergiebedarfs]" (§ 2.2) |
| **K3** | Modusfeld § 9 Nr. 3 — Spalte kommt erst mit B6 | **erledigt mit B6** (Statuszeile #328, anderer Rechner): Schemaschritt 88, Feld offen, Vorgabe AUSWEIS — der Entscheid vom 18.09.2026 („Ausweis") ist damit umgesetzt, keine zweite Baustelle |
| K4 | Tabellenspalte „Brennstoff" ohne Leseweg | kleiner Leser `CarrierId` → Name in B5 |
| K5 | Jahresnutzungsgrad bleibt Projektgröße | als Projektfeld zeigen |
| K6 | WP-Hilfsenergie: Spalte gilt formal für alle, Leser nur BHKW und Kessel | B5 zeigt das Feld nur bei BHKW |
| **K7** | Schreibweg der drei B3-Spalten fehlt (`KwkgAnlagenCtrl.Speichere` = 8 Spalten) | **erledigt**: `Speichere(g, mitSteuerangaben)` schreibt 8 + 4 Spalten (`KwkgAnlagenCtrl.cs:283–299`) |
| **K8** | Fußleiste voll — ein achter Knopf läge bei x = −50. **Die Frage ist gegenstandslos:** die Razor-Fußleiste führt fünf Knöpfe (§ 2.7) | **entschieden 18.09.2026** (nach Empfehlung, = V-1): Umschalter „Kennzahlen / ValERI-Bewertung" im Kopf der Seite statt eines weiteren Knopfes; der Knopf „Verlauf…" entfällt mit der Ergebnisansicht (§ 2.7, § 2.13) |
| K10 | Hilfsenergie-Bemessung doppelt: Seed gegen Altkatalog | **erledigt**: Altarten nur noch zur Anzeige, abgelöst von `PROZENT_ENDENERGIEKOSTEN` |
| K11 | `Views\Wirtschaftlichkeit` unlokalisiert (63 Literale) | **erledigt mit B6**: 0 nackte Anzeigetexte, eigene Wache |

Dazu die Entscheidungen zur Darstellung (30.08.2026):

| # | Frage | Entscheidung |
|---|---|---|
| **D-1** | Emissionsspalte der Energieträgertabelle | **eine** Spalte, Kopf und Inhalt nach `Emission_Berechnungsmodus`; SO₂/NOx entfallen aus dieser Übersicht (§ 2.5) |
| **E-1** | Modus `CO2E`, wenn außer CO₂ nichts gepflegt ist bzw. der Wert schon ein Äquivalent ist | **Wert zeigen, Umstand im Tooltip benennen** — drei Herleitungsfälle, kein stiller Rückfall auf „CO₂" (§ 2.5) |
| **D-2** | Erlösdarstellung | eigene Rubrik in zwei Blöcken, getrennte Summen; Block B (Ausweis) wird nicht addiert (§ 2.6) |
| **D-3** | Referenz der Differenzrechnung | **wählbares Vergleichsprojekt** je Gruppe (Stamm oder Variante), Vorgabe Stamm = ergebnisneutral; `ID_Referenzprojekt` an der Rahmenzeile; die Referenz ist die Unterlassensalternative der DIN EN 17463 (§ 2.9, Anforderung 31.08.2026) |

Zum Energieträger-Dialog kommen die Entscheide vom 18.09.2026 („Der Dialog im Mockup ist sehr
übersichtlich und besser als der vorhandene Dialog Energieträgerverwaltung … und verbessert
werden wie im Mockup"):

| # | Frage | Entscheidung |
|---|---|---|
| **ET-D-1** | In welcher Einheit stehen die Preisbestandteile? | **(a) in der Abrechnungseinheit** (€/m³, €/l, €/t) an der Anzeigekante; gerechnet, gespeichert und geprüft wird weiter in ct/kWh. Ohne Heizwert bleibt ct/kWh mit Hinweis (§ 3.5) |
| **ET-D-2** | Was zeigt der Emissionsblock der Trägerkarte? | **(a) die Arten DIESES Trägers** samt Bilanzierungsmethode als Klappliste, Summenzeile und Fußnote — **kein Primärenergiefaktor, keine Trägerübersicht**. Der Modus ist Projektsache und im Katalogkontext nur lesbar (Entscheide D-1/E-1 bleiben) |
| **ET-D-3** | Was bietet die Preisbasis an? | **(a) genau zwei Einträge** — Abrechnungseinheit und kWh, Faktor = Heizwert — **umgesetzt**. Die Umrechnungsregeln werden zum zugeklappten **Prüfblock** „Einheiten und Umrechnung". **Offener Rest (U32):** Der Kartenzustand fällt weiterhin auf `ID_Umrechnung = -1` zurück |
| **UR-1** | Die Preisbasis rechnete mit dem `factor` einer Umrechnungsregel statt mit dem Heizwert (Anwenderfoto: 0,07 €/kWh eingegeben, 0,04 €/Nm³ gespeichert, Formelzeile 0,0033 €/kWh) | **behoben mit ET-D**: `EnergietraegerPreisCtrl.Preisbasen` liefert den Heizwert als Faktor; `Umrechnungen` liest nur noch aktive Regeln. **Bestandsprojekte werden nicht stillschweigend umgerechnet** — erkennbar an der Formelzeile der Trägerkarte, die den Preis je kWh nennt; wer einen falsch gespeicherten Arbeitspreis hat, gibt ihn neu ein |
| **E1** | Welchen Energieträger bekommt ein **Elektroheizkessel** („Für Elektroheizkessel muss Strom als Energieträger auswählbar und zuzuordnen sein")? | **Er gehört zur elektrischen Welt wie Wärmepumpe und Heizstab.** Ein Heizkessel, dessen Gerät `Tab_Heizkessel.Brennstoff` = 13 führt, lässt nur die Stromfamilie zu, erscheint in der Komponentenliste der Energieträgerverwaltung mit dem **projektweiten Stromträger** als Vorgabe und ist dort wie eine Wärmepumpe zuzuordnen. Nicht über den Brennstoffweg: Der Katalog führt mehrere Träger auf Brennstoff 13, und die Auswahl unter ihnen könnte einen anderen treffen als die Wärmepumpe desselben Projekts. **Keinen eigenen Stromtarif je Verbraucher** (18.09.2026): Es gibt **einen Stromträger je Projekt**. Welcher es ist, wählt die Zuordnung an den Anlagen in der Rangfolge Wärmepumpe → Heizstab → Elektrokessel → Speicher → PV (`ProjektEnergietraegerCtrl.StromTraegerDerAnlagen`); ohne Wahl gilt die Vorgabe des Projekts. Bepreist wird der Netzbezug einmal — ein Heizstromtarif je Anlage entsteht daraus nicht |

**Elektrokessel und Energieträgerzuordnung — die drei Aussagen zusammen.** (1) *Zulassung:* Die
Zulässigkeit hängt am **Gerät**, nicht am Wort „Heizkessel"; Brennstoff 13 bedeutet Kategorie
`ELECTRICITY`, also Gruppe „Strom". (2) *Vorgabe:* Ohne eigenen Trägerverweis an der Anlagenzeile
gilt der Stromträger des Projekts — dieselbe Auflösung, die Wärmepumpe, Photovoltaik,
Stromspeicher und Heizstab nutzen; ein Projekt, dessen einzige elektrische Anlage ein
Elektrokessel ist, braucht und bekommt deshalb ebenfalls einen Stromträger. (3) *Bilanz:* Die
Zuordnung ist eine **Anzeige- und Zuordnungsfrage**, keine Buchung — der Strom des Elektrokessels
bleibt im Reststrombedarf und wird dort einmal bepreist; seine Modulzeile führt weiterhin
`Verbrauch` = 0.

**Keine Doppelbepreisung auf der Betriebsseite** (geprüft am Bestand): Keine Bemessungsart des
Betriebskosten-Rasters multipliziert eine Laufmenge mit einem **Trägerpreis**. Die `EUR_PRO_*`-Arten
rechnen `Menge × Einheitpreis`, und der Einheitpreis ist ein gepflegter Satz; die vier
preisgestützten `PROZENT_*`-Arten (`ENDENERGIEKOSTEN`, `ENDENERGIEBEDARF`, `BRENNSTOFFKOSTEN`,
`STROMKOSTEN`) bilden einen **Anteil** einer Energiekostensumme, keine zweite Vollbepreisung. Eine
Kohärenzprüfung „Strom eines elektrischen Verbrauchers doppelt bepreist" hat deshalb keinen
Gegenstand und ist nicht gebaut.

Aus dem Energieträger-Umfeld kommt eine weitere Entscheidung desselben Tages hinzu. Sie betrifft
die Wirtschaftlichkeitsrechnung nicht, wohl aber den gemeinsamen Schema-Nummernraum:

| # | Frage | Entscheidung |
|---|---|---|
| **U-1** | Einheitenbruch `Tab_Brennstoff_Stamm.Einheit` ↔ `energy_conversion` (BK3 § 6 Nr. 4): die Identitätsregel-Ableitung liefert für 9 von 25 Brennstoffen `-1` | **entschieden 30.08.2026 — Weg (a)**: Der Stammtext der fünf Gase (Brennstoffe 1, 2, 3, 14, 25) wird „m³" → „Nm³" gezogen (Muster Schritt 26a; Leitentscheidung L4 auf die Stammseite fortgeschrieben). Die Wege (b) Identitätsregel-Saat und (c) `billing_unit`-Ableitung sind **nicht beauftragt** |

**Der Schemaschritt ist noch nicht vergeben.** Der einst genannte **Schritt 62 ist anderweitig
belegt** (`Schritt_62_KlimaWaisen`); U-1 steht aus und bekommt seine Nummer **bei der Umsetzung**
(nächster freier Schritt am 22.09.2026: **101** — 90–100 sind vergeben). Gemessen am
19.09.2026: Die fünf Gase führen in `Tab_Brennstoff_Stamm.Einheit` unverändert `m³`;
`energy_carrier.billing_unit` steht dagegen seit Schritt 26a auf `Nm³`. **Die Umsetzung ist nicht
freigegeben** und gehört auf den Pufferspeicher-Strang.

**Die Randfragen des Einheitenbruch-Konzepts sind hiermit hierher übernommen** und gelten von hier
aus: Waisenheilung (`energy_project_settings` Zeile 10076, Projekt 1039), kg-/rm-Abrechnung der
Brennstoffe 4/5/12, Brennstoff 24 „Sonstige", der Fremdkörper Regel 67 und ein Prüfschritt in
`EnergieEinheitenPruefung`. Das Quellpapier
[`ueberholt/Konzept_Einheitenbruch_Energietraeger_EPOS-Plan.md`](../../ueberholt/Konzept_Einheitenbruch_Energietraeger_EPOS-Plan.md)
ist Geschichte (Bezugsstand Access, `SchemaVersion 61`) und **keine Regelquelle**. Nicht zu
verwechseln mit [`Konzept_Einheiten_EPOS-Plan.md`](../Konzept_Einheiten_EPOS-Plan.md) (kWh gegen
MWh) — ein anderes Papier.

**Rechtliche Unsicherheiten** (aus Grundlagen § 6, weiterhin offen — vor produktivem Einsatz mit
dem Hauptzollamt bzw. am Volltext zu klären; keine Entscheidung des Anwenders, sondern des Rechts):

| # | Punkt | Stand im Konzept |
|---|---|---|
| R-U1 | § 53 neben § 53a: der Wortlaut „vorbehaltlich" spricht für anteilige Anwendung, Kommentarliteratur und Dienstvorschrift für ein Entweder-oder | als **Auswahl** modelliert (`KEINE` / `PARAGRAF_53` / `PARAGRAF_53A`), nie Kombination; entschärft, weil § 53 den gesamten BHKW-Brennstoff erfasst (§ 3.7) |
| R-U2 | § 53a Abs. 3, Erdgassatz 4,96 €/MWh für das produzierende Gewerbe über dem allgemeinen 4,42 €/MWh | nicht umgesetzt; in § 2.6 (Klarstellung 2) als Kesselseite eingeordnet |
| R-U3 | Ausschluss fossiler flüssiger Brennstoffe aus der KWKG-Förderung — nur Sekundärquelle | als Prüfkette „Heizöl-Neuanlage ab 2025" umgesetzt (§ 3.6) |
| R-U4 | EuGH-Urteil 09.07.2026 zur Beihilfeeigenschaft des KWKG | kein Primärbeleg |
| R-U5 | keine Nachfolgeregelung nach 2030 | Förderzeitraum als Datumsparameter im Katalog, nicht als Konstante |
| **R-U6** | **Auslösedauer des 45-Euro-Mechanismus im ETS 2** — zwei oder drei aufeinanderfolgende Monate; die Sekundärquellen widersprechen sich (Grundlagen § 10 Nr. 1) | nicht übernommen; der CO₂-Preispfad (§ 3.11) baut auf dieser Größe auf und führt sie als Stützstelle mit Status, nicht als Konstante |
| **R-U7** | **Exakter Wortlaut des § 10 Abs. 3 BEHG** (Bezugspreis 2027), nur sekundär gesichert; die Zuordnung „Berechtigungen" gegen „Emissionszertifikate" ist auch fachlich strittig (Grundlagen § 10 Nr. 2) | entschärft, falls das Dritte Änderungsgesetz den Korridor festschreibt; bis dahin Katalogstatus VORLÄUFIG |
| **R-U8** | **Zahlenreihe des Projektionsberichts 2026**, nur sekundär belegt (Grundlagen § 10 Nr. 3) | vor Verwendung als Vorbelegung des Preispfads (§ 3.11) zu prüfen |
| **R-U9** | **Enddatum der Versteigerungsphase 2026** — Restmengenrechnung gegen Sekundärquelle 09.09.2026 (Grundlagen § 10 Nr. 4) | betrifft den Übergang Festpreis → Versteigerung im Preispfad (§ 3.11) |

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
| **B7** | Erlösrubrik in Reiter, Word, Excel und BHKW-Vorschau; Energiekosten je Anlage; eine Emissionsspalte nach Modus; eine Sichtbarkeitsregel für alle drei Ausgaben | keine auf den Kapitalwert — Referenzlauf der fünf CI-Projekte PASS |
| **BK1** (Entscheid `BK-E-1` (a)) | KWK-Zuschlag gehört der Anlage: Schemaschritt 89 (`KWKG_Kostenanteil` + Datenschritt), Rückfall Anlage → Projekt entfällt, Kontingent je Anlage nach § 8, Vorschlagsknöpfe am Feld, Gruppe 2 auf die vier projektweiten Angaben eingedampft, **ein** Aktivierungsschalter statt sechs Kopien | keine — Datenschritt ergebnisneutral, gemessen an Projekt 1030 (Zuschlag Jahr 1 7.315,96 €, Kapitalwert −21.895.377,28 € vorher wie nachher); Referenzlauf der fünf CI-Projekte PASS |
| **BK1a** | Aufräumen nach der Anlagenwahrheit: Schemaschritt 90 entfernt die sechs KWKG-Projektspalten; Ersatzweg über eine leistungsgewichtete virtuelle Gesamtanlage (Gewicht `g_i = P_el,i`), Gruppe 2 auf die projektweiten Angaben eingedampft | keine — Datenschritt ergebnisneutral |
| **BK1b** | Die Projektspalte `KWKG_Kostenanteil` fällt (Schemaschritt 91); der Anteil der Neuherstellungskosten steht nur noch an der Anlage | keine — einziger Pflegeort wandert, kein Rechenleser betroffen |
| **VG** (§ 2.9, § 2.15) | Wählbares Vergleichsprojekt je Gruppe (Schemaschritt 92, `ID_Referenzprojekt`, NULL = Stamm) und die Vergleichssicht der Ergebnisansicht: alle Varianten gegen die Referenz oder zwei Stände A und B mit A als Referenz dieser Sicht. Die Referenz ist Parameter von `Berechne` und `BerechneVerlauf`, ihre Auflösung samt Randfällen steht einmal in `Referenzwahl`; Sicht, A und B liegen in `Vergleichsauswahl`, der Paarlauf persistiert nicht | keine in der Vorgabe — Referenz = Stamm rechnet bitgleich, Referenzlauf der fünf CI-Projekte PASS |
| **B7P** | Nachweise eines Wirtschaftlichkeitslaufs werden persistiert: `ErgebnisNachweisUmschlag` legt vier Listen und vier Skalare als JSON mit Präfix `nw1:` in `Tab_ErgebnisWirtschaftlichkeit.Nachweis_Json` (über `SpalteSicher`, ohne Schemaschritt), Längenwächter 4 MiB, toleranter Leseweg | keine — die davon-Zeilen stehen auch nach dem Neuladen |
| **B5** (#286) | Der BHKW-Wirtschaftlichkeitsdialog als Razor-Komponente (`BhkwWirtschaftlichkeitDialog.razor`, acht Gruppen, § 2.2): Auszug aus dem Parameterdialog, Schreibweg der Anlagenspalten (K7), Brennstoff-Leser (K4), Live-Herleitung; mit #286 auf die einheitliche Speichern-/Abbrechen-Leiste umgebaut, danach mit #330, #342, #352 weiter gepflegt | keine — solange niemand die neuen Felder pflegt |
| **B6** (#328, anderer Rechner) | § 9 Abs. 1 Nr. 3 StromStG als **Ausweis** statt als Erlös (Befund B-1, Entscheidung K3, § 3.8), Kohärenz-Nachträge, Lokalisierung (K11). **Schemaschritt 88** legt `Tab_ProjektWirtschaftlichkeit.Stromst_Befreiung_Modus` an (TEXT, `AUSWEIS`/`ERLOES`, kein DML, NULL = AUSWEIS); der Modus wandert mit ins Ergebnis | **ja, gewollt** — Projekt 1030: Ausweisbetrag 8.862,15 €/a in beiden Modi, Kapitalwert von −21.763.530,86 € (Erlös) auf −21.895.377,28 € (Ausweis) |
| **B-1 / Kesselbrennstoff** (#331, dieser Rechner) | Die Kessel-Modulspalte `Verbrauch` trägt den Brennstoffeinsatz des Laufs; Endenergie-Positionen am Kessel fallen nicht mehr auf `null` (§ 4, Befunde B-1/N1) | **ja, gewollt** — neue Referenzbasis `2026-09-18_R9_Kesselbrennstoff` mit **#333** eingefroren (§ 6.2) |
| **VV** (§ 2.16, #359) | Vergütung je Variante: eigene Werte oder vom gewählten Projekt übernommen. **Schemaschritt 93** an `Tab_ProjektPhotovoltaik`; der Kopierlauf fror die Vergütung bisher nur zufällig ein | keine in der Vorgabe — Übernahme rechnet wie bisher |
| **Hilfsstrom am Endenergiebedarf** (#365/#366) | **Schemaschritt 94** (reines DML): Die drei Hilfsstrom-Positionen der Standardvorlagen BHKW, Heizkessel und Wärmepumpe wechseln von „% der Endenergiekosten" auf „% des **Endenergiebedarfs**"; Weg B bewertet den Endenergiebedarf einer Anlage mit dem **Preis ihres eigenen Stromträgers** (§ 2.2 Gruppe 1, § 5 K2) | **ja, gewollt** — die Hilfsenergiekosten werden überhaupt erst berechnet |
| **Nutzungsdauern S2** (#357) | Knopf „Nutzungsdauern vorbelegen…" als vierter der Rasterleiste (U8) und die Tafel „Ersatz und Restwert" im Kostendialog (U30); Positionsart als Klappliste im Zeileneditor (§ 2.13 (3)) | keine, solange keine Dauer gepflegt wird; danach **ja** — Ersatz und Restwert entstehen |
| **Übernahme aus der Kostenverwaltung** (#363) | „Aus Vorlage übernehmen…" öffnet den **Katalogblock** der Administration (Komponente · Kategorie · Variante · Positionsvorschau mit Spalte „Ziel") statt der bisherigen Klappliste | keine — reine Auswahlseite |
| **Bezugsgrößen** (#364) | Betriebskosten: Bezugsgrößen aus dem gespeicherten Lauf, jeder Fehlgrund benannt; Grundlage der Live-Frisch-Anzeige (§ 2.8 Punkt 3, § 6.3 B5-Kernaufgabe 2) | **ja** — zuvor ungerechnete Hilfsenergiekosten entstehen |
| **E0 Papierpflege** (#379) | Papierpflege ohne Entscheid: Kopfzeile, Geltungsblock, Quelltabelle und Artifacts dieses Papiers; § 2.2/§ 2.3/§ 2.4/§ 2.7/§ 2.8/§ 2.12 auf den Razor-Stand; § 3.1 Formelkarte mit Endenergie-Topf und p_I; § 6.1 um acht Etappenzeilen, § 6.3 um neun Erledigte bereinigt, § 6.4/§ 6.5/§ 7 berichtigt; neuer Anhang „Kürzel und Etappen"; dazu Szenarienkonzept, Nutzungsdauer-Konzept, Rechenwege 04/05/08, Mockups und Index (E0b). **Nicht ausgeführt:** der Schnitt in drei Papiere (A13) | **keine** — kein Code berührt |
| **E1 Nachweisfundament** (#380) | Das Nachweisfundament dieses Feldes: `WirtschaftlichkeitAnkerTests` (9 Anker, § 6.2), `SteuerGutschriftRechnerTests` (39), `EegSatzRechnerTests` (49), `PvErloesRechnerEegTests` (23, Befund V‑2 gepinnt), `BerichtBlattstrukturWacheTests` (5), `WirtZeileFormatWacheTests` (4); **Kaskadenrunde 2** (`InvestKaskade`) in zwei Phasen wie Runde 3, damit reihenfolgeunabhängig | **keine** — A/B über 25 Projekte × 3 Szenarien zeilenweise identisch; Referenzlauf unverändert |
| **DL‑2e Knopfleisten** (#390) | Die Fußleisten der beiden Kostendialoge dieses Papiers auf den Hausstil: `KostenKomponenteDialog` und `EnergietraegerDialog` tragen die `SpeichernLeiste` mit Status · Speichern · Abbrechen · OK; die vier Rasterknöpfe des Reiters „Kosten" (§ 2.8) bleiben Blattleiste; „Bezeichnung speichern" der Energieträgerkarte entfällt zugunsten **eines** Schreibwegs | keine — reine Bedienung |
| **E2 Kleine Kernkorrekturen** (#405, 20.09.2026) | Ausweis und Bedienung ohne Rechenwirkung: CO₂-Doppelansatz und Strommix-Rückfall als Kohärenzzeilen (§ 3.9), Kohärenzzeilen im **einen** Zeilenkatalog und damit in Rubrik, Wort- und Excelbericht; Erlaubnisschwelle StromStG mit Leser; PV-Reihe als eigene Spalte der Mehrjahrestabelle; Bezugsmenge aus dem `BemessungKatalog`; zeichengenauer Steuerwertvergleich; Kapitalwertdifferenz über dem Nettobarwert; Bandbreite mit Spalte „Spanne" und Referenzzeile in Wort- und Excelbericht, Empfehlungssatz und Δ-Fußzeile mit der gewählten Referenz, Zeitraumhinweis auch im Excel-Blatt; dazu die Dialogkorrekturen der Mockup-Prüfung (Löschrückfragen mit Vorgabe „Nein", Gesetzesparameter im PV-Zweig, Sprungknopf auf den OK-Weg, OK-Weg nur bei Änderung, Hausschlüssel der Standardknöpfe, `help_mapping`-Anker) | **keine** — die vier Ankertests unverändert, Referenzlauf der fünf CI-Projekte PASS |
| **E3 Plattform** (#431, Merge `2cfee66b`) | Acht Schritte: vier nahtlose Hüllen, PV-Dateiwahl über `Dienste.Datei`, `KostenSeiteGaben`/`WirtschaftlichkeitSeiteGaben` und der Rechenaufruf (`BerichtsDatenSammler`) nach `EPOS.Kern`/`EPOS.UI.Daten` verschoben, `KostenKomponenteHuelle` und `GesetzeskatalogHuelle` mit Fenster-Adapter, Tarif-Sprünge als Überlagerung statt Zweitfenster, `IosProjektQuelle.BerichteKostenGaben` beliefert alle vier Seiten, Whitelist 18 → 21 Schlüssel | **keine** — Referenzlauf 13/13 Projekte, 3 882 737 Werte innerhalb der Toleranz (Teil a und Teil b) |

## 6.2 Regressionsanker

Die Anker stehen seit **E1 (#380)** als Testklasse im Kern: `WirtschaftlichkeitAnkerTests` (9 Fälle)
fährt den Weg `LadeParameter → ErgebnisCtrl.Load → KostenEmissionRechner.Berechne →
WirtschaftlichkeitCtrl.Berechne` und hält damit die Größen dieses Papiers fest, ohne den
Berichtssammler (`BerichtsDatenSammler`, seit E3 in `EPOS.Kern/Allgemein/Bericht/`, umgesetzt #431)
nachzubauen. Dazu kommen die Rechnerklassen
`SteuerGutschriftRechnerTests` (39), `EegSatzRechnerTests` (49), `PvErloesRechnerEegTests` (23), die
Blattwache `BerichtBlattstrukturWacheTests` (5) und die Formatwache `WirtZeileFormatWacheTests` (4).

| Anker | Wert | Herkunft |
|---|---|---|
| `LiesBetriebskosten(1024)` | **99,00 €/a** | gemessen = Konzept (#380) |
| Kapitalwert 1024 | **−2.896.359,13 €** | gemessen (#380) — das Konzept führte **−2.220.322,32 €** |
| Kapitalwert 1030 | **−21.895.377,28 €** | gemessen (#380) |
| `LiesInvestitionen` 1018 / 1024 / 1042 | 45.312,50 · 12.001,00 · 13.000,00 | unverändert |
| Kaskadenregression 1042 | **±0,00 €** | gemessen (#380) — das Konzept führte **+20.927,61 €** |
| Referenzbasis | `Referenzlaeufe\2026-09-19_R10_BhkwWirkungsgrad` | |

**Zwei Abweichungen zum bisherigen Konzepttext, beide als Befund festgehalten (#380):** Die
Kaskadenprobe 1042 ergibt ±0,00 € statt +20.927,61 € — die drei Prozentzeilen des Projekts tragen im
heutigen Datenstand keinen Einheitpreis. Der Kapitalwert 1024 liegt mit −2.896.359,13 € um
**−676.036,81 €** unter dem Konzeptwert; die Abweichung ist eingegrenzt, aber nicht nachgerechnet
(Kandidaten: Kesselbrennstoff B‑1/#331, Hilfsstrom #365/#366, Schemaschritte 93–96) und gehört zu
**E7**. Bis dahin gilt der **gemessene** Wert als Anker.

1030 ist auf der **Investitionsseite verankert** (410.000,00 €, `InvestKaskadeTests.cs:281`) und seit
#380 auch im Kapitalwert; **die Betriebskosten von 1030 tragen weiterhin keinen Anker.** Die Projekte
1007, 1017, 1045 und 1046 führen in der Testdatenbank keinen gebuchten Ergebnisstand und keine
Kategorie‑1-Zeilen — ihre absoluten Anker fallen an, sobald die nächste Basis einen führt (#380).

## 6.3 Offene Punkte

*Die Nummern bleiben, wie sie vergeben wurden — auch die Sprünge (9 doppelt; 9a…9g, 9l, 9m, dann
9h…9k). Ein erledigter oder überholter Punkt wird als solcher gekennzeichnet, nicht entfernt und
nicht neu nummeriert, damit Verweise aus Protokollen und Statuszeilen weiter treffen.*

**B5-Kernaufgaben — alle drei erledigt**

1. ~~Schreibweg der drei B3a-Anlagenspalten — `KwkgAnlagenCtrl.Speichere` von 8 auf 11 Spalten (K7)~~
   — **erledigt**: `Speichere(g, mitSteuerangaben)` schreibt 8 + 4 Spalten (`KwkgAnlagenCtrl.cs:283–299`)
2. ~~Live-Frisch-Anzeige der Bezugsgröße mit Herleitungszeile im Kostendialog~~ — **erledigt mit
   U31 (#347) und #364**; spezifiziert bleibt sie in § 2.8 (Entwurf B, übernommen 31.08.2026)
3. ~~Erste Kostenposition mit Anlagenbezug entsteht erst hier~~ — **erledigt mit B5 (#286 ff.)**

**Nach B6 — alle sechs erledigt**

4. ~~§ 9 Nr. 3 als Ausweis (`Stromst_Befreiung_Modus`, Vorgabe AUSWEIS) — K3~~ — Schemaschritt 88, § 3.8
5. ~~Doppelmeldung § 9b in `RechneAufschlaege` streichen~~ — `RechneAufschlaege` ist mit dem Umbau der Aufschläge entfallen; die § 9b-Hinweise der Kohärenzprüfung schließen einander aus (Fall 2 kehrt zurück, bevor Fall 3 geprüft wird)
6. ~~Hinweiszeile für Träger mit 1.000-kg-Satz bei Literabrechnung (`density` leer)~~ — Fall 4a in § 3.9
7. ~~Positive Nennung im Kohärenzfall 1~~ — Bestätigungszeile in § 3.9
8. ~~Lokalisierung `Views\Wirtschaftlichkeit` (63 Literale) und der Auflöser-Texte — K11~~ — nach dem Razor-Port blieben 7; alle überführt, bewacht von `LokalisierungWirtschaftlichkeitWacheTests`
9. ~~Altkatalog-Bemessung `PROZENT_BRENNSTOFFKOSTEN` nachziehen — K10~~ — war schon erledigt: Der Seed führt sie nur noch zur Anzeige von Bestandsdaten (`FuerBetrieb = false`), abgelöst von `PROZENT_ENDENERGIEKOSTEN`; die Eskalation zieht gleich

**Nach B7 — was die Etappe offen lässt**

9a. **B7-1: A4 und A5 stehen in einer Zeile.** `SteuerErgebnis.EnergiesteuerEur` führt § 53/§ 53a
    und § 54 als Summe; die Rubrik kann sie deshalb nur gemeinsam ausweisen. Für die Trennung
    braucht der Rechner zwei Rückgabegrößen — ein Eingriff, den B7 ausdrücklich ausschließt.
9b. ~~**B7-2: `KwkgModulNachweis` und die Energiekosten je Anlage werden nicht persistiert.**~~ —
    erledigt mit B7P (Anwenderentscheid B7-E-1): Modulnachweis, Energiekosten je Anlage,
    Betriebskostenpositionen (E3) und Kohärenzzeilen reisen als JSON-Umschlag in der Spalte
    `Nachweis_Json` von `Tab_ErgebnisWirtschaftlichkeit` mit, zusammen mit den vier Skalaren
    `VermiedenMengeMWh`, `VermiedenEntlastung9bJahr`, `ProduzierendesGewerbe` und
    `BezugsspitzeKW`. Ohne Schemaschritt: Die Ergebnistabelle wächst wie ihre zwanzig
    Vorgängerspalten über `SpalteSicher`, die Zielversion bleibt 89. Eine Nachweistabelle je
    Ergebnis wäre vier Schemata, vier Schreib- und vier Lesewege für Daten, aus denen nichts
    gerechnet wird. Ein fehlender oder unlesbarer Umschlag kostet nur die Unterzeilen und setzt
    genau einen Kohärenzhinweis — nie eine Ergebniszeile.
9c. ~~**B7-3: Die KWKG-Pauschale (§ 9 KWKG, A3) hat keine Rubrikzeile.**~~ — **erledigt mit U17
    (#346)**: Die Zeile ist gebaut (`WirtschaftlichkeitZeilen.cs:405–412`), die Spalte ebenso
    (`:1209`), bewacht von `KwkgPauschaleZeileTests`; sie steht als Jahr-0-Ausweis, nicht in einer
    €/a-Spalte. Siehe § 2.6 (A3).
9d. **B7-4: Der Grund einer Nullzeile ist aus den Ergebnisdaten abgeleitet, nicht vom Rechner
    durchgereicht.** Die `STEUER_*`-Begründungen und der KWKG-Ausstieg stehen im Hinweisfeld des
    Laufs, aber als ein mit „ | " verbundener Text über alle Positionen; ihn einer einzelnen
    Position zuzuordnen hieße, einen Parser zu erfinden. Die Rubrik nennt deshalb die
    **Bedingung** der Position („nur produzierendes Gewerbe; abzüglich Sockelbetrag 250 €/a"),
    nicht die Diagnose des Laufs — die steht unverändert in der Hinweiszeile darunter. Eine
    saubere Lösung führte die Begründungen je Position im `SteuerErgebnis`.

**Nach BK1 — was die Etappe offen lässt**

9e. ~~**BK1-1: Sechs Projektspalten bleiben ungelesen stehen.**~~ **Erledigt mit BK1a**
    (Anwenderentscheid `BK1-1` „Empfehlung" vom 18.09.2026): **Schemaschritt 90** entfernt
    `KWKG_Bonus`, `KWKG_Bonus_Einspeisung`, `KWKG_Vbh_Kontingent`, `KWKG_Vbh_Jahresdeckel`,
    `KWKG_Tatbestand` und `KWKG_Anlagenart` aus `Tab_ProjektWirtschaftlichkeit`. Der Datenschritt
    bleibt nachvollziehbar — er steht als Quelle in `KwkAnlagenwahrheit`, und Schritt 89 läuft vor
    Schritt 90.
9f. ~~**BK1-2: Der projektweite Ersatzweg rechnet weiter mit den Projektsätzen.**~~ **Erledigt mit
    BK1a:** Der Ersatzweg bildet aus den Anlagen eine **leistungsgewichtete virtuelle
    Gesamtanlage** (§ 3.6) und liest damit dieselbe Quelle wie der Regelweg. Der benannte
    Widerspruch zum Aktivierungsschalter ist damit fort: Ein Projekt mit Anlagensätzen bekommt
    auch auf diesem Weg seinen Zuschlag.
9g. ~~**BK1-3: Ein Jahr-0-Ausweis der KWKG-Pauschale in der Erlösrubrik**~~ — **erledigt mit U17
    (#346)**, dieselbe Sache wie 9c.
9l. ~~**BK1-4: `KWKG_Kostenanteil` (Projekt) hat keinen Rechenleser mehr, das Feld bleibt.**~~
    **Erledigt mit BK1b (Schemaschritt 91):** Anwenderentscheid 18.09.2026 „BK1-4: (a)
    Entfernen“. Eine gepflegte Angabe ohne Wirkung neben derselben Größe mit Wirkung war der
    Rest der doppelten Wahrheit; § 8 Abs. 2/3 KWKG leitet das Kontingent aus dem Kostenanteil
    **der Anlage** ab. Die Projektspalte `Tab_ProjektWirtschaftlichkeit.KWKG_Kostenanteil` und
    ihr Dialogfeld in Gruppe 2 sind entfallen; das Anlagenfeld in Gruppe 1b samt
    Vorschlagsknopf bleibt.
9m. **BK1-Q2 (a) — abgenommen, hier als Ausnahme festgehalten:** Ein Projekt, dessen
    Vbh-Kontingent an Projekt UND Anlagen leer ist, rechnete auf dem Ersatzweg still mit dem
    Feldvorgabewert 30 000 h. Seit BK1a leitet `KontingentDerAnlage` daraus 0 h mit Begründung ab
    — dieselbe Antwort, die der Regelweg seit BK1 gibt. Wissentlich abgenommen.

**Aus der Anwenderdurchsicht der Ergebnisansicht (§ 2.13)**

9h. **Nutzungsdauer, Ersatz, Restwert — drei fehlende Stücke (Mockup-Anhang U39):** Entkopplung
    von Ersatz und Restwert, die ungelesenen geräteeigenen Nutzungsdauer-Spalten, der Anschluss
    der Speicherflotte; dazu der Hinweis „T über Vorgabe, Position ohne Dauer" auf Seite und
    Bericht und die plattformfreie Hülle der Zeitraumzeile. **Erledigt mit #357:** die Nachpflege
    des Bestands (Knopf „Nutzungsdauern vorbelegen…") und der Pflegeort der Positionsart
    (Klappliste im Zeileneditor) — s. § 2.13 (3).
9i. **Erlösrubrik nach Komponente innen:** Anlagenbezug je Katalogzeile, Block „projektweit",
    Eigenverbrauchsmengen je Anlage für die vermiedenen Kosten.
9j. **Verlauf mit drei Szenarien:** Dreierreihe statt eines Szenarios je Lauf, Reihenbildung
    Variante × Szenario, Spaltengruppen je Szenario im Tabellenbericht, plattformfreie Rechen-
    und Zeichenlogik. `Gestrichelt` liest das Verlaufsbild.
9k. ~~**Persistenz der Nachweise je Anlage** (Energiekosten-Unterzeilen nach dem Neuladen) — Teil
    von B7-2.~~ — erledigt mit B7P zusammen mit 9b: Der Nachweisumschlag der Ergebniszeile
    trägt die Unterzeilen mit.

**Fachlich und technisch**

10. Bezugsgrößen der übrigen KD1-Bemessungsarten (H1-1b)
11. Nachzieh-Migration für Bestandsprojekte — durch die Auto-Anlage entschärft, bleibt Option
12. ~~`InvestSummeFuer` auf die abgeleitete Kaskadensumme umbauen (B-5)~~ — **erledigt mit
    W5‑B‑8**: Basis ist `InvestKaskade.Summen` (§ 3.4)
13. Pufferkapazität bleibt null — bewusste Grenze
14. ~~Reduzierter Stromsteuersatz bleibt Konstante bis zur Katalog-Nachpflege~~ — **überholt**:
    Der Katalog führt `STROMST_REDUZIERT_SATZ` (`GesetzKatalog.cs:1156`); die Konstante in
    `StrompreisZerlegungModel` ist nur noch wertgleiche **Rückfallebene** (`:82–89`). Offen bleibt
    allein, dass **keine Wache Konstante gegen Katalog** hält (§ 6.5)
15. Bilanzjahr und Unternehmensart wirken erst beim nächsten Dialog-Öffnen
16. Rückweg „Parameterdialog zeigt den erfassten Preisanteil" fehlt
17. ~~Kohärenzzeilen nicht persistiert~~ — erledigt mit B7P, sie reisen im Nachweisumschlag mit ·
    Fall 4 ohne Katalogsatz bleibt still
18. ~~Engine-Sortierung `ORDER BY Prioritaet` (HB1-O1)~~ — **vermutlich überholt**:
    `SimulationControl.cs:1512, 3372, 4117` sortieren `ORDER BY Prioritaet, ID`; ob dies die
    gemeinte Stelle ist, ist nicht gegengeprüft — nachmessen, dann streichen
19. Asymmetrie „Wartung BHKW" gegen „Vollwartung / Wartung Kessel"

**Aus Etappe E1 (#380) — geschlossen**

*Ohne eigene Nummer, weil der Punkt erst im Umsetzungsplan des Analysepapiers entstand:* **R4
Kaskadenrunde 2.** Runde 2 der Drei-Runden-Kaskade (§ 3.2) hing an der Reihenfolge der Zeilen.
`InvestKaskade` fährt sie seit #380 in zwei Phasen wie Runde 3 und ist damit
reihenfolgeunabhängig; der A/B-Nachweis über 25 Projekte × 3 Szenarien ist zeilenweise identisch
(`InvestKaskadeTests`, 25 Fälle).

**Aus Etappe E2 (#405, 20.09.2026) — geschlossen**

25. ~~**CO₂ doppelt gebucht** (R5): aktiver CO₂-Bestandteil im Arbeitspreis und gebuchte
    BEHG-Reihe nebeneinander blieben unbemerkt.~~ — **erledigt**: neuer Fall der Kohärenzprüfung,
    WARNUNG mit dem doppelt gebuchten Jahresbetrag (§ 3.9). Der Rechenweg bleibt, wie er ist.
26. ~~**Kohärenzzeilen erreichen nur die Seite** (R6); der Strommix-Rückfall war ein Laufhinweis
    ohne seinen Wert.~~ — **erledigt**: Die Zeilen stehen im **einen** Zeilenkatalog
    (`WirtschaftlichkeitZeilen`) und damit in Rubrik, Wort- und Excelbericht; der Rückfall nennt
    seine 435 g CO₂/kWh.
27. ~~**B-7** (`MengenEinheit` beschriftet jede Bezugsmenge mit „€") · **I-5** (uneinheitliche
    Vergleichsstrenge) · **S-3** („(0 kW)" am Kessel) · **S-5** (Erlaubnisschwelle ohne Leser) ·
    **V-3** (PV-Reihe ohne Spalte).~~ — **alle fünf erledigt**, je ohne Rechenwirkung; Einzelheiten
    in der Befundtafel des § 4.
28. ~~**G7** (Zeitraumhinweis fehlt im Excel-Blatt) · **G8** (Bandbreite ohne Spalte „Spanne" und
    ohne Referenzzeile) · **G9** (`WIRT_EMPF_KEINE` nennt „Stammprojekt" statt der gewählten
    Referenz).~~ — **alle drei erledigt**; Word und Excel führen dieselbe Bandbreitentafel, und
    Empfehlungssatz wie Δ-Fußzeile nennen die Referenz beim Namen.
29. **Hi/Ho am CO₂-Grenzwert** (R11) — **entschieden 22.09.2026 (Anwender): „es gilt immer der
    Brennwert."** Der Katalog führt zum Erdgas einen heizwert- und einen brennwertbezogenen
    EBeV-Faktor (200,9 bzw. 181,4 g/kWh) samt Umrechnung; gelesen wird heute der Schlüssel der
    Anlage, die beiden Ho-Zeilen haben keinen Leser. Der Grenzwert 270 g/kWh des § 2 StromStG wird
    **brennwertbezogen** geprüft: Der Zähler nimmt den Ho-Faktor (bei heizwertbezogenem Katalogwert
    die Umrechnung Hi → Ho), sonst fiele er rund 10 % zu hoch aus und die Befreiung entfiele in
    Grenzfällen zu Unrecht. Umsetzung mit **E7** (Rechenwirkung: der gebuchte Befreiungsbetrag und
    damit der Kapitalwert ändern sich in Grenzfällen; A/B-Nachweis, die Pinnung in
    `KleinkorrekturenE2Tests` wird auf den Brennwert umgestellt). Die Frage entstand erst mit E2
    und war vom Entscheid „nach Empfehlung" des 20.09.2026 nicht gedeckt.

**Aus der Papierpflege E0 (#379) — Sachpunkte der Datenaufnahme**

*Beide Punkte sind neue Anwenderfragen ohne Empfehlung; der Entscheid „nach Empfehlung" vom
20.09.2026 deckt sie **nicht**.*

30. **Sieben Energieanlagen tragen `KWKG_Anlagenart = ''`** (leere Zeichenkette statt NULL oder
    eines Steuerwerts). Die Anlagenart entscheidet über Kontingent und Satzstaffel; eine leere
    Zeichenkette ist weder „nicht gepflegt" noch eine Wahl. **Entschieden 22.09.2026 (Anwender,
    nach Empfehlung): ein DML-Schritt setzt die leere Zeichenkette auf NULL; NULL heißt „nicht
    gepflegt" — der Kern bucht dann keinen KWKG-Zuschlag und meldet es als Kohärenzzeile
    „Anlagenart fehlt", der Dialog zeigt „bitte wählen". Ein geratener Wert würde Kontingent und
    Satzstaffel setzen, die niemand eingegeben hat. Umsetzung mit E7** (Schemaschritt, Nummer bei
    der Umsetzung; Projekte 1032 und 1043 der Testdatenbank, Live-Datenbank vorher prüfen).
31. **`Nachweis_Json` ist in 0 von 78 Ergebniszeilen belegt.** Die Persistenz der Nachweise
    (B7P, Punkt 9b) ist gebaut, aber kein Bestandsergebnis trägt den Umschlag: Er entsteht erst
    beim nächsten Rechenlauf. **Entschieden 22.09.2026 (Anwender, nach Empfehlung): kein
    Nachziehlauf.** Ergebniszeilen ohne Nachweis werden in Ansicht und Bericht als „Nachweis liegt
    mit der nächsten Rechnung vor" gekennzeichnet; der Umschlag entsteht beim nächsten Rechenlauf.
    Ein Nachziehlauf würde 78 Bestandsergebnisse mit den heutigen Rechenwegen neu rechnen und
    Zahlen ändern, die der Anwender bereits gesehen hat. Umsetzung der Kennzeichnung mit E5
    (Ergebnisansicht) und dem Berichtsbaustein.

**Nachweis und Betrieb**

20. ~~Zahlenprobe gegen die Altanwendung (A8, ≡ B9)~~ — **entfällt, Anwenderentscheid 22.09.2026:
    „BHKW-Plan-Mappen: nicht relevant."** Die Inventarisierung der Mappen bleibt als Geschichte in
    [`ueberholt/Protokolle/Reporting/Analyse_Altanwendung_BHKW-Plan.md`](../../ueberholt/Protokolle/Reporting/Analyse_Altanwendung_BHKW-Plan.md);
    Entscheid A17 (Referenzmappe, `_kap`) ist damit gegenstandslos, Etappe E11 des Analysepapiers
    entfällt. Der Nachweis der Wirtschaftlichkeitsgrößen läuft über die Anker aus E1 (§ 6.2) und
    die A/B-Nachweise der rechenwirksamen Etappen (E7, E9, E10)
21. ~~Basiswechsel der Referenzläufe entscheiden~~ — **erledigt mit #333** (neue Basis
    `2026-09-18_R9_Kesselbrennstoff`, § 6.2; heute `2026-09-19_R10_BhkwWirkungsgrad`).
    ~~**Offen bleibt**: Kapitalwert und Betriebskosten von 1030 verankern.~~ — **Entscheid A11
    (20.09.2026 nach Empfehlung): Ankertests zuerst**, die Erweiterung des Referenzlaufs ist eine
    Frage für die nächste Basis. Mit **E1 (#380)** gebaut: Der Kapitalwert 1030 ist verankert
    (−21.895.377,28 €); **offen bleiben allein die Betriebskosten von 1030**
22. Sichtabnahmen: Brennstoffblock (B2), Kosten-Seite (BK1), Stromsteuer-Hervorhebung (B4)
23. resx-Sammelnachtrag der Textschlüssel aus B3a, B3b, B4 und der F-Serie
24. Datenpflege: Projekt 1018 Kessel ohne Energieträger, Puffer ohne Temperaturpaar;
    WP-Kennlinie 1024 ohne HT-Stützstellen

## 6.4 Fallstricke zur Wiederverwendung

Es gilt heute nur noch, was hier ohne Einschränkung steht:

- `SetzeBetrag` ist ein Upsert — ein Harness braucht unbenutzte StammIDs.
- Visual Studio regeneriert `Resource.Designer.cs`; Handeinträge erzeugen CS0102.

**Nur noch für den Migrationslauf** (`Referenzlauf/Migrationslauf.cs`, die einzige Stelle mit
`Microsoft.ACE.OLEDB`; die Datenhaltung ist SQLite):

- ACE: `UPDATE … WHERE x IN (SELECT …)` trifft stillschweigend 0 Zeilen — kein Fehler, keine
  Warnung. Fremdschlüssel vorher einzeln auflösen und die Zeilenzahl protokollieren.
- ACE: Ein falscher Spaltenname meldet sich als fehlender Parameter, nicht als unbekannte Spalte.
- Zwei gemischte ACE-Verbindungen sehen Fremdschreibungen verzögert.

**Überholt und deshalb gestrichen:** „Keine `.cs` unterhalb von `WindowsFormsApplication1\`
(CS0017); Harnesse nach `dev\`" — den Ordner `dev\` gibt es nicht. „Build nur über das MSBuild von
Visual Studio, x64 — `dotnet build` scheitert an COM" — `CLAUDE.md` nennt
`dotnet build WP-Plan.sln -c Debug -p:Platform=x64` ausdrücklich als den Weg (SDK 10.0.400).

## 6.5 Doppelte Wahrheiten

> **Aufgelöst mit BK1 (Entscheid `BK-E-1` (a), Schemaschritt 89): der KWK-Zuschlag.** Er stand
> zweimal da — je Anlage (`Tab_Energieanlagen.KWKG_*`, Schritt 22) und je Projekt
> (`Tab_ProjektWirtschaftlichkeit.KWKG_*`, Schritt 28) —, und dazwischen lag eine Rückfallkette.
> Der Anwender pflegte damit Felder, deren Wirkung davon abhing, ob ein zweites Feld anderswo leer
> war; eine Kaskade aus zwei verschieden alten Modulen war gar nicht abbildbar. Die Wahrheit ist
> jetzt die Anlage.
>
> **Vollständig aufgelöst mit BK1a (Schemaschritt 90).** Die sechs Projektspalten `KWKG_Bonus`,
> `KWKG_Bonus_Einspeisung`, `KWKG_Vbh_Kontingent`, `KWKG_Vbh_Jahresdeckel`, `KWKG_Tatbestand` und
> `KWKG_Anlagenart` sind entfernt; der letzte Leser — der projektweite Ersatzweg — rechnet seither
> mit einer leistungsgewichteten virtuellen Gesamtanlage aus den Anlagen (§ 3.6). **Ein Vorbehalt
> bleibt und ist abgenommen** (`BK1-Q2` a): Wo weder Projekt noch Anlage ein Vbh-Kontingent
> führen, galten bisher still 30 000 h; jetzt gilt 0 h mit Begründung. **Ein Feld bleibt ohne
> Leser stehen** (`BK1-Q1` c, offener Punkt `BK1-4`): `KWKG_Kostenanteil` des Projekts samt seinem
> Dialogfeld.
>
> **Damit zu Ende gebracht mit BK1b (Schemaschritt 91).** Auch dieses letzte Feld ist fort:
> Projektspalte und Dialogfeld in Gruppe 2 entfallen, der Kostenanteil steht nur noch an der
> Anlage (Gruppe 1b, mit Vorschlagsknopf am Feld). `Tab_ProjektWirtschaftlichkeit` führt von den
> ursprünglich elf KWKG-Projektspalten noch vier: `KWKG_Abschlag_Negativ`, `KWKG_Pauschalmodus`,
> `KWKG_Stichtag` und `KWKG_Inbetriebnahme` — alle vier gelten wirklich projektweit.

*Aus KONTEXT § 9 — jede benannt und begründet. Neue Spalten und Novellen müssen beide Orte treffen.*

| Doppelung | Stand |
|---|---|
| Stromsteuersatz an zwei Orten — Katalog `STROMST_REGELSATZ` und `STROMST_REDUZIERT_SATZ` gegen die `const double` in `StrompreisZerlegungModel` | wertgleich, **gekoppelt ist nichts**: Die vorhandene Wache (`StrompreisZerlegungTests`) prüft Modell gegen Konstante, **nicht** Konstante gegen Katalog — eine gepflegte Novelle erreicht die Modellkonstante nicht, und eine Wache dafür fehlt. Der reduzierte Satz ist gesät; die Konstante ist ausdrücklich nur noch Rückfallebene (§ 6.3 Nr. 14) |
| „Energieintensiv" an drei Orten — Unternehmensart, Schnellwahl im Trägerdialog, Katalogsatz | seit B4 liest die Schnellwahl den Katalog und die Unternehmensart hebt den passenden Knopf hervor; gekoppelt ist weiterhin nichts |
| BHKW-Einspeisevergütung an **drei** Orten | aktiver Tarif (`Tab_ProjektTarif.Einsp_Arbeit`) · Projektparameter (`Einspeiseverguetung_KWK`) · Anlage (`KWKG_Satz_Einspeisung`); Vorrang eindeutig (aktiver Tarif schlägt Parameterwert). Der vierte Ort (`energy_project_settings.Verguetung_BHKW`) ist mit Schritt 84/85 entfallen |
| Zwei Migrationsmechanismen — `SchemaMigration` gegen Selbst-DDL in `WirtschaftlichkeitCtrl` | **fünf** Tabellen mit eigenem `CREATE TABLE` (`Tab_ProjektWirtschaftlichkeit`, `Tab_ErgebnisWirtschaftlichkeit`, `Tab_ErgebnisWirtSensitivitaet`, `Tab_ProjektTarif`, `Tab_ErgebnisStromMatrix`), dazu 55 `SpalteSicher` auf sechs Tabellen; die `CREATE` tragen weder `STRICT` noch Fremdschlüssel (ADR-001 Option B). Neue Spalten gehören an beide Stellen |
| Zwei Lesewege auf die Kostenposition | der direkte Zugriff ist der Normalfall; daneben die gespeicherte **Sicht** `Abfrage_Kostenfaktoren` (`sql/schema/002_views.sql`) — kein Access-Artefakt mehr, sie liegt im Repo, kennt die neuen Spalten nicht und ließe sich erweitern; beim Tabellenumbau wird sie eigens behandelt (`ProjektWerteLoeschschutz`) |
| ~~Komponenten-IDs hart verdrahtet gegen dynamisch gelesen (`Form_Kosten` gegen `UcBkKosten`)~~ | **gegenstandslos** — beide Klassen gibt es nicht mehr: Die Unterscheidung liegt im Kern (`KostenVorlagenCtrl.IstErfassungsgruppe`), die Oberfläche in `EPOS.UI/Dialoge/Kosten/` |
| Vorrang Projekt vor Katalog in **zwei** Implementierungen | `KostenEmissionRechner`, `StromPreisCtrl`; dazu die Sicht `Abfrage_Energietraeger_Effektiv` (`sql/schema/002_views.sql:26`) |
| ~~Die gespeicherte Access-Abfrage kennt die neuen Spalten nicht~~ | **überholt**: Access ist abgelöst; die gespeicherten Abfragen sind Altbestand des eingefrorenen Access-Zweigs |
| ~~Kennzahlenliste dreifach~~ | aufgelöst mit E7 — `WirtschaftlichkeitZeilen` führt sie einmal |

---

# 7 Vorgeschlagene Reihenfolge

**B5, B6 und B7 sind gelaufen und stehen mit ihrer Ergebniswirkung in § 6.1.** Offen sind nur noch
die beiden Etappen dieser Tafel:

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **B8** | Die verbliebenen Befunde: **S-2** (kein projektweites Doppelentlastungsverbot) und **B-6** (geschluckte Fehler, `catch {}` ⇒ still 0). Der **PV-Teil von V-3** und **I-5** sind mit **E2 (#405)** erledigt. S‑2 ist mit **A3** entschieden (20.09.2026 nach Empfehlung): **Sperre mit Begründungszeile**, nicht Warnung. Beide Punkte laufen in **E7** des Etappenplans mit | **ja** bei S-2 — jeder Punkt einzeln mit A/B-Nachweis; B-6 ist Robustheit |
| **B9** ≡ A8 | Zahlenprobe gegen die Altanwendung — **entfällt (Anwenderentscheid 22.09.2026: „BHKW-Plan-Mappen: nicht relevant")**. Die Inventur der Mappen ([`Analyse_Altanwendung_BHKW-Plan.md`](../../ueberholt/Protokolle/Reporting/Analyse_Altanwendung_BHKW-Plan.md)) und die neun Abweichungen der Altanwendung (§ 5 der [Grundlagen](../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md)) bleiben als Geschichte stehen; der Nachweis der Wirtschaftlichkeitsgrößen läuft über die Anker aus E1 (§ 6.2) und die A/B-Nachweise der rechenwirksamen Etappen | entfällt |

Zur Einordnung: **I-1, I-2, I-3, B-1/N1, N3, B-5 und S-6 sind erledigt** (§ 4); die frühere
Reihenfolgebegründung („B5 bleibt ergebnisneutral, die erste gewollte Ergebnisänderung kommt mit
B6") ist mit B5, B6, B7, BK1, BK1a, BK1b, VG, VV und der Hilfsstrom-Umstellung überholt. Die
Reihenfolge der **heute** offenen Etappen — V-A…V-E (§ 2.11.4), U39 (§ 2.13 (3)),
Erlösrubrik-Ausbau (§ 6.3 9a/9d/9i), ND-S3, B8, B9 — steht im Etappenplan E0–E12 des
Analysepapiers [`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md) § 5.
Davon sind **E0 (#379), E1 (#380), E2 (#405) und E3 (#431)** gebaut; die nächste Etappe ist **E4
Erlösrubrik und Steuerzeilen**.

**Wiederaufnahme 22.09.2026.** Die Umsetzung war am 20.09.2026 zurückgestellt (Statusdatei, Block
„Nach #405" (f)); der Anwender hat sie am 22.09.2026 mit dem Auftrag wieder aufgenommen, das Mockup
`../Mockups/Dialog_Formel_Zahlenprobe.html` umzusetzen.

## Entscheide vor der nächsten Codeetappe

Kennungen nach dem Analysepapier vom 19.09.2026, § 4. **Anwenderentscheid 20.09.2026: alle Entscheide
A1–A20 gelten nach der Empfehlung der Papiere.** Damit blockiert keiner dieser Entscheide noch eine
Etappe; ausgeführt sind sie damit nicht.

| # | Entscheid (20.09.2026, nach Empfehlung) | Stand |
|---|---|---|
| **A1** | Rechenaufruf und Datenseite kommen **vor** der Ergebnisansicht aus der Windows-Schale — als eigene Welle **E3 Plattform**, in der gemessenen Reihenfolge; sonst entsteht jedes Stück der Ergebnisansicht ein zweites Mal nur für Windows (betrifft § 2.13 (5), die plattformfreie Hülle) | entschieden **und gebaut** — E3 ist umgesetzt (**#431**) |
| **A2** | Befund **K-1**: Vor der Umsetzung wird gemessen, ob die modulscharfe Nutzwärme vorliegt; sonst Aufteilung nach P_el mit Herleitungszeile (§ 3.6, § 4) | entschieden, nicht gebaut — E7 |
| **A5** | **Degradation: V-E rechnet sie nicht ein.** Der Entscheid „G3 nicht umsetzen" des Szenarienkonzepts (§ 2.11.2, dort V‑G2) gilt; V-E (§ 2.11.4) wird **ohne Degradation** geplant | entschieden — der Widerspruch zwischen beiden Papieren ist aufgelöst |
| **A11** | Nachweis der Wirtschaftlichkeitsgrößen: **Ankertests zuerst**; die Erweiterung des Referenzlaufs ist eine Frage für die nächste Basis (§ 6.2, § 6.3 Nr. 21) | entschieden **und gebaut** mit E1 (#380) |
| **A13** | Schnitt dieses Papiers in drei Papiere (gültiger Stand · Entscheidungsregister · Protokoll der Entscheidwege) — **ja**, vor der ersten Codeetappe; Papierpflege ohne Entscheid zuerst | entschieden, **nicht ausgeführt** — E0 (#379) hat nur die Pflege gemacht, der Schnitt steht aus |

Die übrigen Entscheide A3, A4, A6–A10, A12, A14–A20 stehen mit ihrer Empfehlung und ihrer Etappe im
Analysepapier § 4 und § 5; sie gelten seit dem 20.09.2026 ebenso nach Empfehlung. **Nicht** vom
Entscheid gedeckt sind die Punkte 29, 30 und 31 des § 6.3 — sie entstanden erst mit E0 und E2 und
tragen keine Empfehlung.

---

# Anhang — Kürzel und Etappen

Fünf Kürzelräume laufen nebeneinander: die Etappen dieses Papiers, die des Szenarienkonzepts, die
des Nutzungsdauer-Konzepts, die Statusnummern der Datei `Status_iOS_Migration.md` und die
U-Nummern des Mockup-Anhangs „Umsetzungsstand". Diese Tafel löst sie gegeneinander auf.

| Konsolidiertes Konzept | Szenarienkonzept | Nutzungsdauer | Statuszeile | Gegenstand |
|---|---|---|---|---|
| W4 E1–E8, L12/L13 | — | — | vor #300 | Gesetzeskatalog, Tarif-Rollenmodell |
| K1–K6 · KD1–KD6 · P1–P6 · H1–H4b, H21 | — | — | vor #300 | Alttabellen, Kostendialoge, PV, Pflichtpositionen |
| B1 · B2 · B3a · B3b · B4 | — | — | vor #300 | Zahlenprobe, Schema 60/61, Hilfsstrom, Stromsteuer |
| **B5** | — | — | **#286 ff.** | `BhkwWirtschaftlichkeitDialog` (§ 2.2) |
| **B6** | — | — | **#328** (anderer Rechner) | § 9 Nr. 3 als Ausweis, Schemaschritt 88 |
| **B7** | — | — | **#329** | Erlösrubrik, Energiekosten je Anlage, Emissionsspalte |
| **B7P** | — | — | **#331** (anderer Rechner) | Nachweisumschlag `Nachweis_Json` |
| Befund **B-1** | — | — | **#331** (dieser Rechner) | Kessel-Modulspalte `Verbrauch`; neue Basis **#333** |
| **BK1** | — | — | **#330** | KWK-Zuschlag an der Anlage, Schemaschritt 89 |
| **BK1a** | — | — | **#335** | Schritt 90, virtuelle Gesamtanlage |
| **BK1b** | — | — | **#336** | Schritt 91, `KWKG_Kostenanteil` entfällt |
| **VG** (§ 2.9, § 2.15) ≡ **V-B** | W5‑B‑11 | — | **#358** | Vergleichsprojekt, Schritt 92 |
| **VV** (§ 2.16) | — | — | **#359** | Vergütung je Variante, Schritt 93 |
| *(namenlos)* | — | — | **#365/#366** | Hilfsenergie am Endenergiebedarf, Schritt 94 |
| **B8** | — | — | offen (in **E7**) | Befunde S-2 (≡ A3) und B-6; V-3-Rest und I-5 mit **#405** erledigt (§ 7) |
| **B9** ≡ A8 | — | — | entfällt (Anwender 22.09.2026) | Zahlenprobe gegen die Altanwendung — BHKW-Plan-Mappen nicht relevant, E11 entfällt |
| **V-A…V-E** (§ 2.11.4) | W5‑B‑9…W5‑B‑12 | — | keine im Bereich #300–#428 | ValERI: W5‑B‑9/10/11/12 gebaut (Schritte 71, 72); V-A = **E5**, V-C/V-D = **E8**, V-E = **E9** |
| § 2.13 Punkte (1)–(6) | — | — | #332, #346, #354, **#405** | Ergebnisansicht; mit #405 Kennzahl-Reihenfolge und die Dialogkorrekturen |
| § 6.3 Nr. 9h | — | **S2-Rest / U39** | **#357** | Nutzungsdauer, Ersatz, Restwert |
| — | — | **S1 · S2 · S3** | S1 vor #300, S2 = #357, S3 offen (**E10**) | AfA-Tabelle |
| Mockup-Anhang **U1…U45** | — | — | #342 ff. | Umsetzungsstand je Bildstelle; **U1 = Befund K-1** |

**Die Etappenreihe E0–E12** (Analysepapier § 5) ordnet alles Offene dieses Papiers:

| Etappe | Gegenstand | Statuszeile |
|---|---|---|
| **E0** Papierpflege | Kopf, § 6.1/§ 6.3/§ 6.4/§ 6.5/§ 7 und die Nebenkonzepte; **A13-Schnitt nicht ausgeführt** | **#379**; Nachpflege auf den Stand vom 22.09.2026 mit **E0c** |
| **E1** Nachweisfundament | Ankertests, fünf neue Testklassen, Kaskadenrunde 2 (R4) | **#380** |
| **E2** Kleine Kernkorrekturen | R5, R6, V-3, B-7, I-5, S-3, S-5, G7/G8/G9, Formel `N4`, P3 der Mockup-Prüfung | **#405** |
| **E3** Plattform | acht Schritte: vier nahtlose Hüllen, `Dienste.Datei`, die beiden Gaben, Rechenaufruf, `KostenKomponenteHuelle` mit Fenster-Adapter, PV/Tarif/Katalog/Verlauf, Sprünge, `BerichteKostenGaben` und Whitelist | **umgesetzt #431** (Merge `2cfee66b`) |
| **E4** Erlösrubrik und Steuerzeilen | U7 (zwei Beträge, zwei Zeilen), 9d (Gründe je Position), U6 (Anlagenfeld, Komponentenblöcke, Zwischensummen, Block „projektweit") | offen — **nächste Etappe** |
| **E5** … **E12** | Ergebnisansicht und V-A · Verlauf · rechenwirksame Lücken (B8) · V-C/V-D · V-E · ND-S3 · Zahlenprobe A8/B9 · Wiki | offen |

Daneben laufen **W‑E2** (die Statuszeilen-Schreibweise für E2, #405) und **DL‑2** (Knopfleisten aller
Dialoge; die beiden Dialoge dieses Papiers mit **DL‑2e**, #390). **KI‑F2 … KI‑F8** (#419–#425, #427,
#428) geben die Masken für den Hilfe-Assistenten frei — davon berührt **KI‑F4** (#423) die Kosten- und
Wirtschaftlichkeitsmasken; **KI‑F8** (#428) liefert das Hüllen- und Adaptermuster für E3.

**Zwei Fallen bei den Kennungen.** (1) Die Statusnummern **#302, #304, #328 und #331** sind je
**doppelt** vergeben (zwei Rechner); jede Zeile der Statusdatei trägt den Zusatz, ein Verweis
„#331" allein ist mehrdeutig. (2) Die Kürzelkollisionen K-1/K1, B-1/B-1, U-1/U1, V-1/V-1 und
V-Gn/Gn sind unter „Namensvorsicht" am Ende von § 2.12 aufgelöst.
