# Konzept: Wirtschaftlichkeit EPOS-Plan — gültiger Stand (konsolidiert)

**Stand 25.09.2026** · Codestand `80a7b9fb` · `SchemaStand.Zielversion` = 144 · Schemaschritte 90–144 vergeben (116–118 die Schritte B, C und D der Etappe E9a; E9b ohne Schritt; 119 die Kühlung KU2; 120 die Sätze der Nutzungsdauertabelle, Etappe E10; 121–124 anderen Feldern; E13 und E14 ohne Schritt; 125 das Risikomodul, Etappe E15; 126 die Reparatur der Gebäude-Katalogsätze (#485); 127 die nicht monetarisierbaren Wirkungen, Etappe E17; 128 der Heizkreis je Gebäude im Ergebnis (Anlagenkopplung AK1, Welle 3); 129 die Wiederholperiode je Kostenposition, Etappe E16; 130 die Anschlusslängen im Gebäudekatalog (#493); E18 ohne Schritt; 131 die Zapfprofil-Stufe Z4b (#486); 132–139 den Cloud-Sitzungen G3, G4 und AK1; 140 die Messreihen der Zapfprofil-Stufe Z5 (#495); 141 die Folgeberichtigung der Anschlusslängen im Gebäudekatalog (#496); 142 die dritte Reparatur der Anschlusslängen im Gebäudekatalog (#505); 143 die Quellenberichtigung der Baustoffe (Cloud-Sitzung G3, E39); 144 die Nachtzeit je Gebäude (Cloud-Sitzung G4, E43); E19, E20, E21, E22, E23, E24, E25, E26 und E27 ohne Schritt) · Gesetzeskatalog Generation 9 (Nachpflege ohne Schemaschritt, § 3.6) · Referenzbasis `2026-09-25_R19_BhkwNetzbezug` · konsolidiert aus drei Quelldokumenten; Mockups und Rechenwege im Ordner `Wirtschaftlichkeit_Kosten/`

Die Schritte 97 bis 101, 103, 107 bis 110, 114, 115, 119, 121 bis 124, 128 und 130 bis 141 gehören nicht diesem Feld: **97**
Szenario und Bezugsjahr der Klimaregion (`Schritt97_KlimaSzenario`, KL‑6), **98** BHKW-Gesamtwirkungsgrad als Faktor (reines DML,
BW‑1), **99** die zwei Wirkungsgrade des BHKW (`Schritt99_BhkwWirkungsgradAnteile`, BW‑1), **100** die
Vorgabe 0 der Fremdschlüsselspalten (FK‑1, #426), **101** die Gebäudespalten der Gebäudesimulation
(`SCHRITT_101_GEBAEUDESPALTEN`), **103** Katalog, Zonen und Projekt des Zapfprofilgenerators
(`SCHRITT_103_ZAPFPROFIL_KATALOG`, #438), **107** die Ergebnistabelle je Gebäude der Gebäudesimulation
(`SCHRITT_107_ERGEBNIS_GEBAEUDE`, Entscheid E30), **108** bis **110** die Kühlung der Gebäudesimulation, Stufe
KU1 (108 KU-S1 `SCHRITT_108_KUEHLUNG_GEBAEUDE`, 109 KU-S2 `SCHRITT_109_KUEHLUNG_PROJEKTEINSTELLUNG`, 110 KU-S4
`SCHRITT_110_KUEHLUNG_ERGEBNIS`), **114** der Kühlbetrieb am Erzeuger, Stufe KU2 (KU-S3
`SCHRITT_114_KUEHLUNG_ERZEUGER`), **115** die Zapfkategorien des Zapfprofilgenerators, Stufe Z3 (T2,
`SCHRITT_115_ZAPFKATEGORIEN`, `Tab_TwwZapfkategorie_STAMM`, #453), **119** die Abrechnungsart des Kältestroms und
die Kälteseite der Wärmepumpenergebnisse, Stufe KU2 Welle 3 (`SCHRITT_119_KAELTESTROM`, Entscheid E34), **121** der
Katalogverweis des Projektgebäudes (#468), **122** und **123** die Wärmeübergabe und die Ergebnisspalten der
Anlagenkopplung, Stufe AK1 (AK-S1, AK-S3), **124** die Laufangaben der Zapfprofil-Auslegung, Stufe Z4 (T3, #464), **128** der Heizkreis je Gebäude im Ergebnis,
Stufe AK1 Welle 3 (`ErgebnisGebaeudeSchema.SCHRITT_HEIZKREIS`, vier Spalten an `Tab_ErgebnisGebaeude`), **130** die
Berichtigung der Anschlusslängen im Gebäudekatalog (#493, `GebaeudeAnschlusslaengenReparatur.SCHRITT`, reines DML), **131** die
Typtage der Zapfprofil-Stufe Z4b (`SCHRITT_131_ZAPFPROFIL_TYPTAGE`, #486), **132** bis **139** die Cloud-Sitzungen G3, G4
und AK1 (Baustoffkatalog, Bauteilaufbau, Zonen, Kühlübergabe mit Ergebnis und Zone, Importzuordnung, Baujahr), **140**
die Messreihen der Zapfprofil-Stufe Z5 (`TwwSchema.SCHRITT_T4_MESSREIHEN`, #495) und **141** die Folgeberichtigung der
Anschlusslängen im Gebäudekatalog (#496, `GebaeudeAnschlusslaengenFolgereparatur.SCHRITT`, reines DML). An Tabellen
dieses Feldes,
aber nicht aus seinem Etappenplan:
**106** — fremde Ergebnisverweise der Wirtschaftlichkeit werden NULL, eine Datenbereinigung der Welle #444
(`SCHRITT_106_WIRTSCHAFTLICHKEIT_FREMDVERWEIS`). Diesem Feld gehören **102** — die leere `KWKG_Anlagenart` wird
NULL (`SCHRITT_102_KWKG_ANLAGENART_LEER`, § 6.3 Nr. 30, #437) —, **104** — der Zeitzonentarif wird
abgelöst, die Leistungspreis-Staffel zieht an den Stromträger (`SCHRITT_104_ZEITZONENTARIF_ABLOESUNG`,
§ 3.5, #439) —, **105** — Kennzeichen „Vorrichtung zur Abwärmeabfuhr" und Stromkennzahl je Anlage
(`SCHRITT_105_KWKG_ABWAERMEABFUHR`, § 3.6, K‑1, #440) — und die drei Schritte der Etappe E7c2 (#446): **111** —
Ersatz und Restwert je Position (`SCHRITT_111_ERSATZ_RESTWERT_KENNZEICHEN`, § 2.13 (3), § 3.1) —, **112** — die
Preisbasis der Trägerkarte als eigener Kartenzustand (`SCHRITT_112_PREISBASIS`, § 2.5) — und **113** — der
Stammtext der Gase auf Nm³, des Brennstoffs 24 auf kWh (`SCHRITT_113_GASE_NM3`, § 5) —, dazu die drei Schritte der
Etappe E9a (#461, § 2.11.5): **116** — Betrachtungszeitraum und Mengenänderung je Szenario
(`SCHRITT_116_SZENARIO_RAHMEN`) —, **117** — die Trägerpreise best/worst (`SCHRITT_117_TRAEGERPREIS_SZENARIO`) — und
**118** — die Erlössätze best/worst (`SCHRITT_118_ERLOESSATZ_SZENARIO`) —, und der Schritt der Etappe E10 (#463): **120** —
die Instandsetzungssätze der Standardzeilen der Nutzungsdauertabelle, reines DML (`SCHRITT_120_NUTZUNGSDAUER_SAETZE`,
§ 3.4) —, und der Schritt der Etappe E15 (#478): **125** — das Risikomodul V‑G7, die Spalten `Risiko_Art`,
`Risiko_Zinszuschlag`, `Risiko_Verlust` und `Risiko_Wahrscheinlichkeit` an `Tab_ProjektWirtschaftlichkeit`, reines DDL
(`SCHRITT_125_RISIKOMODUL`, § 2.11.2) —, und der Schritt der Etappe E17 (#479): **127** — die nicht monetarisierbaren
Wirkungen V‑G11, die Tabelle `Tab_ProjektWirkung` (STRICT, Fremdschlüssel auf `Tab_Projekt` mit Weitergabe) samt Index
und der Übernahme eines gepflegten Freitexts `Nicht_Monetaer` als eine Wirkung „sonstig" ohne Beurteilung
(`SCHRITT_127_NICHT_MONETAERE_WIRKUNGEN`, `ProjektWirkungSchema`, § 2.11.2) —, und der Schritt der Etappe E16 (#484):
**129** — die Wiederholperiode je Kostenposition V‑G3, die Spalte `Wiederholperiode_a` (INTEGER, nullbar; leer, 0 und 1
heißen jährlich) an `Tab_ProjektWerte` und `Tab_KostenVorlagePosition`, reines DDL (`WiederholperiodeSchema.SCHRITT`,
`SCHRITT_WIEDERHOLPERIODE`, § 2.11.2, § 2.13 (3)); **126**, die Reparatur der
Gebäude-Katalogsätze (#485), **128**, der Heizkreis je Gebäude der Anlagenkopplung, **130**, die Anschlusslängen im
Gebäudekatalog (#493), und **131** bis **143** (die Zapfprofil-Stufen Z4b und Z5, die Cloud-Sitzungen G3, G4 und AK1, die
Folgeberichtigung #496, die dritte Reparatur #505, die Quellenberichtigung der Baustoffe der Cloud-Sitzung G3) gehören nicht diesem Feld; die Etappen E18 (#492), E19 (#498), E20 (#502), E21 (#506), E22 (#503), E23 (#510), E24 (#514), E26 (#518), E25 (#519) und E27 (#521) kommen ohne Schritt aus. Wer hier einen Schritt plant, nimmt die nächste freie Nummer **bei der Umsetzung** — nicht im Papier.

Dieses Dokument führt zusammen, was heute auf Formelkarte, Feldkarte, sechs Konzepte und
gut zwanzig Etappenprotokolle verteilt liegt. Es beantwortet die beiden Fragen, die vor der
Umsetzung zu klären sind:

1. **Wie sehen die Dialoge und Felder aus** — für alle Anlagen, für BHKW und Photovoltaik im Detail
   (§ 2).
2. **Wie wird gerechnet** — Investition, Betrieb, Energie, Vergütungen, Reduktionen, Energiesteuer,
   Stromsteuer, Zeile für Zeile mit den Formeln (§ 3).

## Geltung und Abgrenzung

> **Dieses Dokument ändert nichts am Code.** Es ist die **führende Fassung** des
> Wirtschaftlichkeitskonzepts und beschreibt den **gültigen Stand**: die Dialoge und Felder (§ 2), die
> Rechenwege (§ 3), die Befunde (§ 4), was aus den Entscheiden als Regel gilt (§ 5 und an Ort und
> Stelle), den Umsetzungsstand und das Offene (§ 6, § 7). **Bei Widerspruch gilt dieses Dokument.**
>
> **Zwei Schwesterpapiere tragen, was hier nicht steht.** Das
> [Entscheidungsregister](Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md) führt jeden
> Entscheid dieses Feldes mit Frage, Wortlaut, Datum und Weg, Ort der Regel und Umsetzungsstand; es
> ist die eine Stelle, an der künftige Entscheide eingetragen werden. Das
> [Protokoll der Entscheidwege](../../ueberholt/Protokolle/Reporting/Konzept_Wirtschaftlichkeit_Entscheidwege_Protokoll.md)
> führt die Geschichte — Entstehung und Quellen, die ausführlichen Etappenzeilen, die erledigten
> Punkte mit ihren Gründen, die Messungen vor der Umsetzung und die Wege der Entscheide; es ist nie
> Regelquelle. Ein Vermerk „→ Register R‑…" oder „→ Protokoll § …" zeigt, wo der Wortlaut dazu steht.
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

*Herkunft des Papiers, sein Geltungsblock vor dem Schnitt und die Tafel der drei Ursprungsdokumente:
→ Protokoll § 2.1 bis § 2.3.*

## Begleitendes Artifact

| Artifact | Inhalt | entspricht |
|---|---|---|
| [**Dialog, Formel, Zahlenprobe**](https://claude.ai/artifact/1WeFMXrpxrCSu1jUtvTRCw) | acht Kostenkategorien, je Dialog-Mockup + Berechnungsgrundlage + durchgerechnete Zahlenprobe an einem Beispielprojekt; Schwerpunkt Vergütungen BHKW (Mengentafel brutto/netto, Mischsatz, Jahresreihe mit Deckel) und PV (AW, Marktprämie, § 51/51a, Kappung); Komponentenkosten BHKW und PV in derselben Dialogform — **die Repo-Datei `../Mockups/Dialog_Formel_Zahlenprobe.html` führt** | § 2.12 |

*Die fünf abgelösten Artifacts: → Protokoll § 2.4.*

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
| **BHKW** | `Kosten_Modul` + Montage + Lieferung + Schallschutzhaube + Abgasreinigung (Summe) · `Wartungskosten_kwhel` · Nutzungsdauer (Gerätedaten, nicht rechenwirksam — A8, #463) | `PROZENT_ENDENERGIEKOSTEN`, 2–4 % | **ja** — `BhkwWirtschaftlichkeitDialog` (§ 2.2) |
| **Photovoltaik** | `Modulkosten` je Modul × Modulanzahl | `JAHRESBETRAG`, keine Pflicht | **ja** — `PhotovoltaikVerguetungDialog` (§ 2.3) |
| **Heizkessel** | `Investitionskosten` · `Wartungskosten` mit Einheit (€/a \| €/kWh \| %/a; ein neuer Eintrag in %/a übernimmt den Wartungssatz der Nutzungsdauertabelle, #463) · Nutzungsdauer (Gerätedaten, nicht rechenwirksam — A8, #463) | `PROZENT_ENDENERGIEKOSTEN`, 4–8 % | nein |
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
| Anlagenart | Baustein `Auswahlfeld` | (bitte wählen) = nicht gepflegt, NULL · neu § 8 Abs. 1 · modernisiert Abs. 2 · nachgerüstet Abs. 3; ohne Anlagenart leitet § 8 kein Kontingent ab (0 h mit Grund) — der Zuschlag entfällt und die Kohärenzzeile „Anlagenart fehlt" erscheint nur dort, wo das Kontingent aus der Anlagenart abzuleiten ist (E7‑Q1, Lesart b, § 6.3 Nr. 30, § 3.9) | `KWKG_Anlagenart` | Bestand; „(bitte wählen)" und Schritt 102 #437; Kohärenzzeile #440 |
| Eigenstrom nach § 6 Abs. 3 | Baustein `Auswahlfeld` | kein Tatbestand · Nr. 1 bis 100 kW · Nr. 2 Kundenanlage · Nr. 3 stromkostenintensiv | `KWKG_Eigenstromfall` | Bestand |
| Satz Einspeisung [ct/kWh] | Numerisch 0–30 | 0 = kein Zuschlag; **Knopf „Vorschlag übernehmen" am Feld** | `KWKG_Satz_Einspeisung` | Bestand, Knopf BK1 |
| Satz Eigenstrom [ct/kWh] | Numerisch 0–30 | 0 = kein Zuschlag; **Knopf am Feld** | `KWKG_Satz_Eigen` | Bestand, Knopf BK1 |
| Vbh-Kontingent [h] | Numerisch 0–200.000 | 0 = nach § 8 aus dieser Anlage abgeleitet; **Knopf am Feld** | `KWKG_Vbh_Kontingent` | Bestand, Knopf BK1 |
| Vbh-Jahresdeckel [h/a] | Numerisch 0–8.760 | 0 = Staffel | `KWKG_Vbh_Jahresdeckel` | Bestand |
| **Anteil Neuherstellungskosten [%]** | Numerisch 0–100 | 0 = nicht gepflegt; wählt mit der Anlagenart die Kontingentstufe § 8 Abs. 2/3 | `KWKG_Kostenanteil` | **neu BK1**, einziger Pflegeort seit BK1b |
| **Energiesteuerentlastung (Anlage)** | Baustein `Auswahlfeld` | (Projektwert) · keine · § 53 · § 53a · § 54 | `Energiesteuer_Wahl` | **neu B5** |
| **Brennstoff auf Strom/Wärme (Anlage)** | Baustein `Auswahlfeld` | (Projektwert) · voller Brennstoff · energetisch | `Aufteilung_Methode` | **neu B5** |
| **Hilfsenergieanteil [% des Endenergiebedarfs]** | Numerisch 0–100 | 0 = keine; Vorschlag BHKW 2–4 %. Bemessen wird am **Endenergiebedarf (Brennstoff)** dieser Anlage — nicht an den Kosten (Schemaschritt 94, Statuszeilen #365/#366) | `Hilfsenergie_Anteil` | **neu B5** |
| **KWK-Strom (§ 2 Nr. 16 KWKG)** | Anzeigezeile; die Wahl steht in der Überlagerung „Sätze und Herkunft" | Fall 1 — Nettostromerzeugung (0, Vorgabe) · Fall 2 — Vorrichtung zur Abwärmeabfuhr (1); Rechenweg § 3.6 | `KWKG_Abwaermeabfuhr` | **neu #440** (Schritt 105) |
| **Stromkennzahl σ** | Numerisch 0–5, drei Nachkommastellen, in der Überlagerung | leer = Vorschlag P_el ÷ P_th der Gerätezeile; ohne P_el oder P_th kein Vorschlag — bei Fall 2 dann kein Zuschlag der Anlage und die Kohärenzzeile „Stromkennzahl fehlt" (§ 3.9); ein eigener Wert gilt dauerhaft, „Vorschlag übernehmen" leert das Feld; gelesen nur bei Fall 2 | `KWKG_Stromkennzahl` | **neu #440** (Schritt 105) |

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

**Der Vorschlag steht am Feld, nicht als Sammelknopf** (→ Register R‑BK, BK-E-1): Unter jedem der
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

**Vorschlag am Feld und Überlagerung gelten beide** (→ Register R‑Q, Q2): Die Grundlagenzeile mit
dem Knopf „Vorschlag übernehmen" bleibt am Feld; die Überlagerung „Sätze und Herkunft…"
(Mockup-Anhang U22) ergänzt je Größe die Wahl und die Herleitung.

**Die Überlagerung „Sätze und Herkunft" trägt alle Wahlen und Sätze** (Mockup-Anhang U22; → Register
R‑Q, Q2, R‑E7, E7‑Q2 (5), und R‑E7c1, E7c1‑Q7; umgesetzt #440, #446 und #452). Die Gruppe „Angaben der gewählten
Anlage" zeigt die Zeile „KWK-Strom (§ 2 Nr. 16 KWKG): Fall 1 — Nettostromerzeugung." bzw. „Fall 2 — Vorrichtung
zur Abwärmeabfuhr, Stromkennzahl σ … (Herkunft)" und den Knopf „Sätze und Herkunft…"; die Gruppe Energiesteuer
trägt den zweiten Knopf „Wahl und Herkunft…" — beide öffnen dieselbe Überlagerung „Sätze und Herkunft —
‹Anlage›". Jede Wahl steht dort als **eine Zeile** — Wahlknopf, Text und dahinter ihre Wirkung („→ 30.000 Vbh",
„→ 5,50 €/MWh · 26.383,5 €/a"; Baustein `Optionsgruppe.Wirkungen`, ein Klick auf Text oder Wirkung wählt mit;
Mockup-Anhang U22, umgesetzt #452). Sie führt drei Gruppen:

1. **KWK-Zuschlag — diese Anlage:** die Anlagenart (§ 8) mit der Wirkung je Wahl, dem Kontingent aus
   `KwkgKontingentRechner`; der Tatbestand des § 6 Abs. 3 mit der Wirkung je Wahl, dem Eigenstromsatz aus
   `KwkgSatzRechner`; die Wahl Fall 1 / Fall 2 samt Wirkung je Fall; die Tafel Größe · Vorschlag · Herkunft ·
   eigener Wert (leer = Vorschlag) · gilt mit den Zeilen Stromkennzahl σ (Vorschlag P_el ÷ P_th der Gerätezeile),
   Satz Einspeisung, Satz Eigenstrom, Vbh-Kontingent und Jahresdeckel — „Vorschlag übernehmen" leert das Feld
   und ist ohne Grundlage weich gesperrt —; darunter „Wirkung Jahr 1" mit Menge × Satz × Deckelanteil aus dem
   gebuchten Lauf (ohne Lauf der Hinweis, dass die Wirkung nach „Berechnen" im Reiter steht).
2. **Energiesteuer:** Geltung „Projektvorgabe für alle Anlagen" oder „nur diese Anlage", die Entlastung (keine,
   § 53, § 53a Abs. 5, § 54) und die Aufteilung (nur § 53) je mit Satz und Betrag im ersten Jahr aus der Vorschau des
   Laufs (§ 3.7), die Herkunft des Satzes und die Positionen der Anlage aus dem gebuchten Lauf.
3. **Stromsteuer — Projekt:** Unternehmensart und Modus § 9 Abs. 1 Nr. 3 je mit ihrer Wirkung, Hocheffizienz und
   räumlicher Zusammenhang, dazu Entlastung (§ 9b) und Befreiung aus dem gebuchten Lauf.

**Leer heißt Vorschlag.** Ein leeres Feld „eigener Wert" lässt den Vorschlag gelten: Bei den Sätzen schreibt
„Übernehmen" den Vorschlag in das Feld des Formulars, bei Kontingent und Deckel bleibt das Feld leer, und der
Lauf leitet selbst ab (§ 8 Abs. 1 bis 3, Staffel des § 8 Abs. 4). Ein eigener Wert gilt dauerhaft, auch wenn
der Katalog später einen anderen Vorschlag liefert. Ein leeres Satzfeld des Formulars (0 = kein Zuschlag)
erscheint in der Überlagerung als eigener Wert 0, damit „Übernehmen" nicht still den Vorschlag schreibt. Die
Überlagerung hält Kopien von Anlagen- und Projektstand als Zwischenstand; „Übernehmen" legt nur Geändertes auf
den Arbeitsstand, „Abbrechen" ändert nichts, geschrieben wird im OK-Weg des Dialogs (Anlagenspalten über
`KwkgAnlagenCtrl.Speichere`). Die Klapplisten und die Knöpfe „Vorschlag übernehmen" des Formulars bleiben daneben
stehen (Q2: Vorschlag am Feld und Überlagerung gelten beide). „Wirkung Jahr 1" und die KWKG-Reihe des Laufs
rechnen denselben Ausdruck (`KwkgJahresbetrag`). Satz und Betrag der Energiesteuer zeigt die Überlagerung für
**jede** Wahl aus der Vorschau des Laufs (§ 3.7; entschieden E7c2‑Q8, Lesart b, → Register R‑E7c2; umgesetzt #452)
— § 54 mit dem Sockel, den die Wahl auslöst, ohne Position der Grund, bei § 53 die Aufteilung mit ihrem Betrag;
trägt der gebuchte Stand keine Vorschau (Nachweisumschlag vor Fassung 8), stehen der Text der Vorschrift und der
Hinweis auf den nächsten Lauf. Dass die Klapplisten des Formulars neben den Zeilen der Überlagerung bleiben, ist
gebaut und als Frage offen (E7c3‑Q7, → Register R‑E7c3).

Fehlt eine Grundlage, ist der Knopf **weich** gesperrt (`aria-disabled`, Grund im `title` — ein
`disabled`-Knopf zeigt seinen Tooltip nie): keine elektrische Nennleistung, kein Tatbestand nach
§ 6 Abs. 3, keine Anlagenart.

### Gruppe 3 — Energiesteuer

Projektebene: Energiesteuerentlastung (keine · § 53 Formular 1131 · § 53a Abs. 5 Formular 1135 ·
§ 54 Formular 1450) · Brennstoff auf Strom/Wärme · Jahresnutzungsgrad [%] (0 = nicht erfasst,
bleibt Projektgröße — K5).

Herleitungslabel am Musterprojekt: `§ 53a Abs. 5 · Erdgas 4,42 €/MWh · 4.797,2 MWh = 21.203,4 €/a`
Kohärenzzeile in Firebrick, wenn der erfasste Brennstoffpreis die Energiesteuer nicht ausweist. Der Knopf
„Wahl und Herkunft…" öffnet die Überlagerung „Sätze und Herkunft" (Gruppe 2 oben; umgesetzt #446). Stehen im
Projekt § 53 / § 53a Abs. 5 an einer Anlage mit Stromerzeugung und § 54 an einer anderen nebeneinander, ist die
Mischlage gesperrt: Der § 54-Betrag ist 0, und die Kohärenzprüfung warnt (§ 3.7, § 3.9; umgesetzt #446).

### Gruppe 4 — Stromsteuer

Unternehmensart (führend, BW4) · Räumlicher Zusammenhang 4,5 km · Hocheffizienz nachgewiesen ·
**Sprungknopf „BHKW-Tarif…"** (in die Tarifstruktur, Rollenmodell; § 3.5) · **Feld „Modus § 9 Abs. 1 Nr. 3"**
(ERLOES/AUSWEIS, Vorgabe AUSWEIS) — Spalte `Stromst_Befreiung_Modus`, Schemaschritt 88
(K3, → Register R‑K). Der Sprung speichert nur, wenn der Arbeitsstand vom
geladenen Stand abweicht. Einen Sprung „Strombezug…" gibt es nicht (Q11, → Register R‑Q): Den
Zeitzonentarif gibt es nicht, die Leistungspreis-Staffel steht beim Stromträger (§ 2.5).

**Erfasster Stromsteueranteil (gebaut #492, E18; § 6.3 Nr. 16).** Unter den Feldern der Unternehmensart — in Gruppe 4
und in der Überlagerung „Sätze und Herkunft" (Gruppe „Stromsteuer (StromStG) — Projekt") — steht der im Strompreis
erfasste Stromsteueranteil des Stromträgers (`energy_project_settings.Aufschlag_Stromsteuer` mit Aktiv-Schalter;
Leseweg `StrompreisZerlegungCtrl.StromsteuerErfasst`, Anzeige `StromsteueranteilAnzeige`). **Zeile 1, Herleitung:**
Träger, Wert in ct/kWh, aktiv oder abgeschaltet und der Abgleich gegen Regelsatz und reduzierten Satz des Bilanzjahres
aus dem Katalog (ohne Bilanzjahr die Bilanzkonvention, ohne Katalogzeile die Rückfallebene; Toleranz 0,005 ct/kWh wie
Kohärenzfall 4, § 3.9). **Zeile 2, Kohärenz** (nur bei aktivem Anteil): „passt zur Unternehmensart" oder der Vorschlag
des anderen Satzes mit dem Verweis auf „Strompreis Details" — dieselbe Regel wie die Hervorhebung der Schnellwahl
(§ 2.5), ein Hinweis ohne Sperre. Die Zeilen folgen der gewählten, auch ungespeicherten Unternehmensart; ohne erfassten
Anteil, ohne Stromträger oder bei nicht lesbarem Wert sagt die Zeile das. **Nur Anzeige:** Gepflegt wird der Anteil
allein in „Strompreis Details" (§ 6.5); Entscheide E18‑Q4 und E18‑Q5 (→ Register R‑E18). Führt die Vergleichsgruppe
kein BHKW, pflegt der Parameterdialog die Unternehmensart in der Gruppe Strom mit derselben Anzeige (§ 2.4; gebaut
#498, E19; § 6.3 Nr. 33).

### Gruppe 5 — Hilfsstrom

Anteil je Modul (dasselbe Feld wie 1.17, zweitgezeigt) · Mengenkette nur lesend aus B3b:

```
Stromerzeugung brutto → − Hilfsstrom → = Nettostromerzeugung → davon Eigen / Einspeisung
```

Bei einer Anlage mit Fall 2 des § 2 Nr. 16 zeigt die Mengenkette dazu die Herleitung des Laufs — σ mit
Herkunft, Nutzwärme, KWK-Strom und Kürzung (§ 3.6; umgesetzt #440).

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

**Unternehmensart ohne BHKW (gebaut #498, E19; § 6.3 Nr. 33).** Führt die Vergleichsgruppe kein BHKW, steht in der
Gruppe **Strom** unter der Einspeisevergütung PV die Unternehmensart nach StromStG — ein Auswahlfeld mit denselben
Wahlen und Texten wie Gruppe 4 des Dialogs „BHKW-Wirtschaftlichkeit“ (`BhkwWahlen.Unternehmensart()`,
`BHW_S_UNTERNEHMENSART`) —, darunter die Zeilen des erfassten Stromsteueranteils (derselbe Baustein
`StromsteueranteilAnzeige` wie in § 2.2 Gruppe 4; Gabe `Stromsteueranteil` und `Katalog` der
`WirtschaftlichkeitParameterHuelle`), live mit der gewählten, auch ungespeicherten Unternehmensart und dem Bilanzjahr
des Arbeitsstands, und eine Erklärzeile zu § 9b (`WPAR_UA_9B_HINWEIS`). Führt sie ein BHKW, steht hier nichts davon: Die
Gruppe BHKW verweist auf den Dialog „BHKW-Wirtschaftlichkeit“, die Pflegestelle in dieser Lage. Je Projektlage gibt es
damit genau **eine** Pflegestelle derselben Spalte (`Tab_ProjektWirtschaftlichkeit.Unternehmensart`); das KI-Feld
`unternehmensart` lehnt mit BHKW benannt ab. **Grenze (E19‑Q4 b):** Das Bilanzjahr steht nur in der Gruppe Brennstoff,
die der Dialog nur mit Brennstofferzeuger zeigt; ohne Kessel und BHKW nimmt die Anzeige den Rückfall 2026 der
Bilanzkonvention — nur Katalogjahr und Anzeige, nicht die Rechnung. Entscheide E19‑Q2, Q3, Q5 und Q6 (→ Register
R‑E19).

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

**Nur der Arbeitspreis folgt der Preisbasis.** Die Klappliste „Preisbasis" steht direkt unter dem
Arbeitspreis und bietet „€/‹Abrechnungseinheit›" und — sobald ein Heizwert im Feld steht — „€/kWh"
(ET‑D‑4, → Register R‑D); sie sagt, in welcher Einheit der Anwender ihn eingeben will. Angezeigt wird
`Basiswert ÷ Faktor`, gespeichert wird der Basiswert je Abrechnungseinheit. Ohne Heizwert nennt eine leise
Zeile, warum „€/kWh" fehlt; rechnet der Träger ohnehin nach kWh ab, gibt es keine Liste. **Die gewählte Basis ist ein eigener Kartenzustand** (ET‑D‑3, Mockup-Anhang
U32; umgesetzt #446): Sie steht als Einheitentext („kWh" oder die Abrechnungseinheit) in
`energy_project_settings.Preisbasis` (Schemaschritt 112), und beim Öffnen wird die Anzeige in dieser Basis
umgerechnet — auch wenn der Brennstoff keine Umrechnungsregel nach kWh führt. `ID_Umrechnung` geht weiter mit,
ist aber nur noch die Regel der Einheitenprüfung, nicht mehr der Zustand der Karte; einen Rückfall auf −1 gibt
es für die Karte nicht. Leer heißt Abrechnungseinheit, ohne Herleitungszeile — so beginnen neue Zuordnungen aus
Wizard, Katalog und Variantenträger (E7c2‑Q3, → Register R‑E7c2). Führt eine Datenbank vor Schritt 112 die
Spalte nicht, zeigt die Karte die Abrechnungseinheit und nennt den Grund, statt still zurückzufallen. Die
Versionskopie trägt die Basis mit. Der **Leistungspreis** bleibt in
`€/(kW·a)` bzw. `€/(kW·Monat)`, der **Grundpreis** in `€/a`; beide kennen die Preisbasis nicht.

**Formelzeile und Effektivprüfung rechnen über die Basiswerte** — Arbeitspreis je
Abrechnungseinheit ÷ Heizwert je Abrechnungseinheit — und nennen die Einheiten:
„0,50 €/Nm³ ÷ 10,50 kWh/Nm³ = 0,0476 €/kWh"; bei Preisbasis kWh läuft die Zeile in Eingaberichtung:
„0,0476 €/kWh × 10,50 kWh/Nm³ = 0,5000 €/Nm³ (gespeichert je Nm³)". Rechnet der Träger unmittelbar nach kWh ab (Strom, Fernwärme), steht
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
Emissionswerte aus der Katalogzeile in die Felder; die gewählte Preisbasis bleibt stehen, und der
Katalogpreis je Abrechnungseinheit erscheint in ihr. Die Karte meldet „Katalogwerte übernommen — noch nicht gespeichert";
geschrieben wird erst mit „Speichern" bzw. „OK", und dabei entsteht die Historienzeile. Das
Projekt folgt dem Katalog danach **nicht** — eine spätere Änderung im Katalog lässt die
Projektwerte, wo sie sind.

**Die Leistungspreis-Staffel steht beim Stromträger** (Q11, → Register R‑Q, R‑E7b; umgesetzt #439). Im
Projektkontext führt die Trägerkarte des Stromträgers im Block „Preis und Heizwert" die Gruppe
„Leistungspreis-Staffel (auf die Jahres-Bezugsspitze)": Staffelgrenze [kW], Preis bis zur Grenze und Preis über
der Grenze [€/(kW·a)], darunter eine Erklärzeile. Gespeichert wird an der Projektübersteuerung
(`energy_project_settings.Leistungspreis_Staffelgrenze`, `…_Staffel1`, `…_Staffel2`, Schemaschritt 104),
geschrieben über `EnergietraegerPreisCtrl`; der Katalog führt keine Staffel. Ein geleertes Feld heißt „nicht
gepflegt" (NULL) — anders als die Preise darüber lässt sich die Staffel so wieder abschalten. Gepflegt ist sie,
sobald einer der beiden Preise größer als 0 ist; gerechnet wird sie im Leistungsanteil des Netzbezugs (§ 3.5).

### Emissionsanzeige der Energieträgertabelle

> **Stand: umgesetzt.** Die Tabelle trägt eine Emissionsspalte; Kopf und Inhalt folgen
> `Tab_Projekt.Emission_Berechnungsmodus` (`EmissionsAusweis.SpaltenkopfEmission`), der Kurztext
> trägt die Herleitung nach den drei Fällen unten (`EmissionsAusweis.HerleitungEmission`). Ein
> stiller Rückfall CO2E → CO2 findet nicht statt: Fehlt der Artenkatalog, steht in der Spalte der
> reine CO₂-Faktor und der Kurztext sagt es. Nachweis `EPOS.Kern.Tests/EmissionsspalteTests`
> (sieben Fälle, beide Sprachen) und `EPOS.UI.Tests/Seiten/KostenSeiteTests`.

*Ist-Zustand vor der Umsetzung: → Protokoll § 3.3.*

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

**Der Tooltip trägt die Herleitung** (E-1, → Register R‑D). Im Modus `CO2E` kann derselbe
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

## 2.6 Eigene Rubrik „Erlöse und Vorteile"

> **Stand: umgesetzt.** Die Rubrik lebt als Block in `WirtschaftlichkeitZeilen.Kennzahlen` —
> **einer** Definition für Ergebnisreiter, Word, Excel und die Vorschau des BHKW-Dialogs
> (Gruppe 6). Block A trägt die Kennung `WirtZeile.BLOCK_A` und geht in die Summenzeile
> `ERL_A_SUMME`, die genau die Zeilen über ihr summiert; Block B trägt `BLOCK_B` und kommt in
> keine Summe — die Trennung ist kein Flag, sondern zwei verschiedene Wege in die Liste
> (`Erloes()` gegen `Ausweis()`). Nachweis `EPOS.Kern.Tests/ErloesrubrikTests`.
>
> **Abweichungen und Klarstellungen zur Tabelle unten, jede aus einer Messung:**
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
> - **A4 und A5 stehen wie in der Tabelle in zwei Zeilen** (umgesetzt #432, U7): § 53/§ 53a
>   beim Blockheizkraftwerk (`ERL_A_ENERGIESTEUER`) und § 54 beim Kessel
>   (`ERL_A_ENERGIESTEUER_54`), je mit Herleitungszeile. `SteuerErgebnis` führt beide als
>   getrennte Beträge samt Sockel, `EnergiesteuerEur` ist nur ihre Summe; ein Stand, der ohne
>   diese Aufteilung gebucht ist, fällt benannt auf die eine Gesamtzeile zurück.
> - **A10 (Restwert) steht nicht in der Summe des Blocks A.** Er ist ein Barwert über T; in einer
>   €/a-Summe des Jahres 1 wäre er ein Einheitenfehler. Seine Zeile bleibt beim Nettobarwert.
>
> **Die Nullzeile mit Grund** (Anlass: → Protokoll § 3.3): Eine A-Zeile erscheint, sobald das Projekt eine Anlage führt,
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
| A7 | Stromsteuer-Befreiung Eigenverbrauch | § 9 Abs. 1 Nr. 3 StromStG | KWK-Eigenverbrauch × 20,50 €/MWh | ≤ 2 MW · hocheffizient · 4,5 km · CO₂ < 270 g/kWh brennwertbezogen (§ 3.8) — **wandert nach BF1 in Block B** |
| A8 | Einspeiseerlös Strom | Tarif bzw. Projektwert | Einspeisemenge × Preis | nominal konstant |
| A9 | PV-Vergütung | EEG | eigene Reihe (`PvErloesRechner`) | 20 Jahre + Inbetriebnahmemonate |
| A10 | Restwert | DIN EN 17463 | Betrag × Restdauer / n | Ende des Betrachtungszeitraums |

### Block B — Ausweis, **nicht addieren**

| # | Position | Warum kein Zahlungsstrom |
|---|---|---|
| B1 | **Vermiedene Stromkosten** — Arbeit · Leistung · Summe | Die Einsparung steckt bereits in der kleineren Bezugsrechnung; in den Kapitalwert geht der **Reststrom**betrag. Zusätzliches Buchen wäre Doppelzählung (E5). Der Leistungsanteil ist regelmäßig **negativ** |
| B2 | PV: vermiedener Bezug, Kappungs- und Ausfallmengen | dito bzw. Mengenausweis. Im Rollentarif trägt der PV-Anteil an B1 den vermiedenen Bezug der Photovoltaik; die Zeile „PV: vermiedener Bezug" zum Flat-Preis steht nur, wo die Aufteilung keinen PV-Anteil führt (umgesetzt #437); ihre Menge ist der Eigenverbrauch = Erzeugung der Module − Einspeisung (§ 3.6, E26 #518) |

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
Bedarf ohne jede Eigenerzeugung − Restbezug; sie führt KWK- und PV-Eigenverbrauch, und die Korrektur
greift auf beide — § 3.6, umgesetzt #437). Der Entlastungssatz kommt jahresgenau aus dem Gesetzeskatalog
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
Warnung amber `#C88A00` auf `#FFF6E0` · Fehlerzeile `#B00020` (Token `--epos-stufe-fehler`) · Hinweise DimGray ·
Segoe UI 9 pt, Gruppentitel fett, Eckenradius 6 · **Fußknöpfe mindestens 88 × 44** (Zielgröße der
Berührfläche; die alte Angabe 110 × 30 war ein WinForms-Maß) · `InfoKnopf` 28 × 28 ·
`SpeichernLeiste` (nicht schließender Speichern-Knopf mit Statuszeile) · Razor-Komponenten **ohne
Designer**: Texte kommen über `[Parameter]`-Vorgaben und die `*Texte`-Bündel und werden in der
Hülle mit `Resource.*` bzw. `T(schlüssel, rückfall)` belegt.

**Die Fußleiste der Wirtschaftlichkeitsseite** (`EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor`)
führt **höchstens drei Knöpfe** — Photovoltaik und BHKW je nach Ausstattung der Gruppe, dazu
Berechnen; „Parameter…" steht in der Zeile der Szenariowahl. Einen Knopf „Strombezug…" gibt es nicht (Q11,
umgesetzt #439); die Tarifstruktur im Rollenmodell öffnet sich aus dem BHKW- und dem PV-Dialog. **Lücke K8 („kein Platz für einen
achten Knopf") ist damit gegenstandslos.** **Entscheid K8 (→ Register R‑K):** kein weiterer Knopf,
sondern ein Umschalter „Kennzahlen / ValERI-Bewertung" im Kopf der Seite (umgesetzt #434) — die
Darstellung „Kennzahlen" mit den vier Abschnitten der Ergebnisansicht (§ 2.13), die Darstellung
„ValERI-Bewertung" mit den Blöcken der Norm; das bestätigt zugleich V-1 (§ 2.11.4). Einen Knopf
„Verlauf…" gibt es nicht: Der Verlauf steht als Abschnitt in „Wie sicher ist das?" (§ 2.13 (5),
umgesetzt #436).

*Kopfband, Fehlerfarbe und die Frage eines eigenen Abschnitts „Hausstil Dialoge": → Register R‑Q (Q8, Q9).*

## 2.8 Betriebskosten-Raster der Kostenverwaltung — Entwurf B

*Herkunft: Entwurf B des Artifacts „Pflichtpositionen je Komponente", in dieses Konzept übernommen
(→ Register R‑EZ, EZ‑3; → Protokoll § 2.6).*

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
  „Nutzungsdauern vorbelegen…"; darunter die Dialogleiste „Abbrechen · Speichern · OK". Auf der Betriebsseite heißt
  der vierte **„Sätze vorbelegen…"** (Nutzungsdauer S3, #463, § 3.4): Er trägt die Instandsetzungs- und Wartungssätze
  der Nutzungsdauertabelle in leere Positionen „Instandhaltung …"/„Wartung …" mit „% der Investition" ein — bei
  Positionen, die schon einen anderen Satz oder einen erfassten Betrag tragen, erst nach der Rückfrage mit ihrer
  Anzahl — und schreibt wie jede Eingabe erst mit „Speichern"/„OK". Gleicht ein Satz dem der Tabelle, steht unter
  dem Satzfeld die Herkunft („2 % · Satz aus Nutzungsdauertabelle: Heizkessel · Wärmeerzeuger (Instandsetzung)").

**Einordnung und Grenzen:** Der Umsetzungsstand je Punkt steht in § 6.1 und § 6.3. Der Klick auf einen
Betrag öffnet die vollständige Herleitung (Entwurf C im Artifact); die Mengenherkunft folgt den
Rechenwegen aus § 3.4. Die Herleitungszeile zeigt am Kessel die Menge des Laufs (Befund **B-1**);
beim Elektrokessel steht dort sein **Stromeinsatz** samt Betrag zum Arbeitspreis des Stromträgers
und der Herkunft „Strom · Netzbezug (im Reststrombedarf des Projekts bepreist)" — die Menge ist
sichtbar, bezahlt wird sie genau einmal, nämlich im Netzbezug (Regel **E1**).

## 2.9 Wählbares Vergleichsprojekt — die Referenz der Differenzrechnung

> **Stand: umgesetzt.** Die Referenz ist je Vergleichsgruppe wählbar und steht in
> `Tab_ProjektWirtschaftlichkeit.ID_Referenzprojekt` (Schemaschritt 92, nullbar; NULL = Stamm).
> `WirtschaftlichkeitCtrl.Berechne` und `BerechneVerlauf` nehmen sie als **Parameter**; alle
> Differenzkennzahlen rechnen gegen sie, die Referenz selbst bekommt keine. Ihre Auflösung samt
> Randfällen und Nachweistexten steht einmal in `Referenzwahl`; die Zeilendefinition kennzeichnet
> die Referenzspalte (`WirtZeile.IdReferenz`), und die Vergleichsgruppen-Liste führt die Wahl.
> Referenz = Stamm rechnet bitgleich zum Bestand. Nachweis
> `EPOS.Kern.Tests/ReferenzprojektTests` und `EPOS.UI.Tests/Seiten/WirtschaftlichkeitSichtTests`.

*Anforderung, Ist-Zustand vor der Umsetzung, fachliche Begründung und Einordnung der Etappe:
→ Protokoll § 4.1; Entscheid D-3: → Register R‑D.*

**Soll:**

| Aspekt | Festlegung |
|---|---|
| Auswahl | je Vergleichsgruppe **ein** Referenzprojekt: Stamm **oder** eine beliebige Variante der Gruppe |
| Vorgabe | **Stamm** — damit ist die Umstellung für jede Bestandsrechnung ergebnisneutral |
| Persistenz | `Tab_ProjektWirtschaftlichkeit.ID_Referenzprojekt` (LONG, nullable; NULL = Stamm). Die Spalte gehört an die Rahmenzeile, weil die Referenz wie Zins und Zeitraum **je Gruppe** gilt (R-1) — ihr einziger DDL-Ort ist der Schemaschritt (SchemaMigration) |
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

## 2.10 Integrationsort der ValERI-Darstellung

*Vorgabe des Anwenders zum Integrationsort: → Register R‑EZ (EZ‑2).*

**Die ValERI-Blöcke werden Bestandteil der Seite „Berichte && Kosten → Wirtschaftlichkeit"
(`EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor`, Hilfekennung `UcWirtschaftlichkeit`) — kein separater Dialog.** Die Seite trägt bereits heute den Titel
„Wirtschaftlichkeit — Kapitalwertmethode (DIN EN 17463)" und die passende Grundausstattung:
Vergleichsgruppen-Liste (mit der Referenzwahl je Gruppe, § 2.9), Szenariowahl, vier
Kennzahl-Kacheln, Vergleichstabelle (Zeilen × Projekte, `<table class="epos-raster epos-matrix">`), Parameternachweis und Fußknöpfe.

**Andockvorschlag** (Einzelheiten in den ValERI-Mockups):

| Element | Ort auf der Seite |
|---|---|
| Referenzwahl (§ 2.9) | in der Vergleichsgruppen-Liste, ein Optionsfeld je Zeile; die gewählte Referenz ist nicht abwählbar |
| Die fünf ValERI-Blöcke (Investition · Betrieb · Erlöse · Energie · Wirtschaftlichkeit über Nutzungsdauer) | als zweite Ansicht der Seite hinter dem Umschalter „Kennzahlen / ValERI-Bewertung" — **V‑1 entschieden** (→ Register R‑V), umgesetzt #434 mit den Blöcken 1, 3, 4 und 5, vollständig #454 mit Block 2 „Zahlungsreihen" samt Zahlungsstrombild und Block 4 mit Spannenbild und Verlauf (die Nummerierung der Blöcke: Fußnote in § 2.11.3) |
| Kumulierter diskontierter Cashflow | als Abschnitt „Verlauf" in „Wie sicher ist das?" der Darstellung „Kennzahlen", mit allen drei Szenarien (§ 2.13 (5), umgesetzt #436); einen eigenen Verlaufsdialog gibt es nicht. Block 4 der Darstellung „ValERI-Bewertung" zeigt denselben Abschnitt samt Spannenbild (E6‑Q1, → Register R‑E6; umgesetzt #454) |
| ValERI-Bewertungsbericht (Anhang E der Norm) | als Baustein der **Bericht**-Seite (Word/Excel), gespeist aus derselben Zeilendefinition; die **Anhang-E-Checkliste** als Abschlussseite des Wortberichts, als letztes Blatt der Mappe und hinter dem Knopf „Anhang-E-Checkliste…" im Fuß des Bewertungsblocks (V‑G12, U43; umgesetzt #455) |

Damit bleibt die Regel „eine Wahrheit je Größe": Die ValERI-Ansicht **rendert** die vorhandenen
Ergebnisse (`WirtschaftlichkeitErgebnis`, `WirtschaftlichkeitZeilen`, Verlaufsreihe) — sie rechnet
nichts Eigenes.

## 2.11 ValERI (DIN EN 17463) — Integration und Darstellung

*Quellen: der Normtext DIN EN 17463:2021-12 (vom Anwender bereitgestellt, vollständig ausgewertet —
alle Anforderungen hier paraphrasiert, Abschnittsnummern der Norm in Klammern) und als reales
Zahlenbeispiel (im Folgenden „Beispielprojekt B") die Altmappe
`Quellen\BHKWPlan\BHKW_Höfingen_Erneuerung_20kWel.XLS`, Blatt
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
| V-G11 nicht monetisierbare Wirkungen | **G11** | investitionsgekoppelte Betriebskosten — **andere Sache**; der Freitext ist mit W5‑B‑12/G6 gebaut, die Liste mit Kategorie und Beurteilung mit E17 (#479) |

| # | Normanforderung | EPOS heute | Lücke / Behandlung |
|---|---|---|---|
| V-G1 | ≥ 2 differenzierte **Preisschwankungsraten**, nominal (6.3.2) | p_E und p_B vorhanden, nominal ✓ | dem Grunde nach erfüllt; keine Raten je Träger/Position (bekannt, R-2) — Deklaration genügt, Ausbau optional |
| V-G2 | **Degradation** je Position [%/a] **mit Quellenangabe** (6.3.1/6.3.3) | fehlt vollständig | neue optionale Positionsattribute; Vorgabe 0 %/a = ergebnisneutral |
| V-G3 | **Zeitpunktattribut** je Cashflow: Periode 0 · jährlich · alle n Jahre · einmalig in k (6.3.1) | **gebaut #484** (E16, Schemaschritt 129); vorher teilweise (StartJahr, Ersatz über Nutzungsdauer) | „alle n Jahre" (z. B. Dichtheitsprüfung alle 2 a) als kleiner Ausbau der Bemessung. **Stand: gebaut #484** (E16, → Register R‑V, R‑E16) — alle vier Zeitpunktarten sind abgebildet: Periode 0 (Investition ohne Startjahr → I₀), jährlich (Betriebsposition, Vorgabe), **alle n Jahre** und einmalig in k (Startjahr k ≥ 2; die Ersatzkette über die Nutzungsdauer). Die Spalte `Wiederholperiode_a` an `Tab_ProjektWerte` und `Tab_KostenVorlagePosition` (leer, 0, 1 = jährlich); eine Betriebsposition mit n ≥ 2 zahlt in den Jahren s, s + n, s + 2n … ≤ T, s = Startjahr, ohne Startjahr das Jahr 1 (`KapitalwertRechner.ZahltImJahr`, E16‑Q1 a), mit dem Betrag des ersten Jahres, fortgeschrieben mit p_B bzw. p_E ihres Topfes; nur Betriebspositionen, eine Investition „alle n Jahre" ist die Ersatzkette (E16‑Q2 a); die Betriebskosten p. a. bleiben die Zahl des ersten Jahres (E16‑Q3 a). Gepflegt im Zeileneditor „Zahlung alle: [n] Jahre" der Betriebsseite und in den Kostenvorlagen (§ 2.13 (3)); Ausweis „alle n Jahre ab Jahr X" in der Herleitungsspalte der Betriebskostentabelle (§ 3.4), Hilfsspalte je Topf in der Formelmappe (§ 2.11.6), Nachweisumschlag Fassung 11. Ohne Pflege ergebnisneutral (Anker, Referenzlauf 13/13); A/B-Nachweis an 1030; vier Fragen → R‑E16 |
| V-G4 | **Kein Restwertverfahren** — Endzahlungen (Demontage, Veräußerung) gehören als explizite Cashflows in die Endperiode (6.4) | Restwert linear | **dokumentierte Abweichung**: Restwert bleibt als Schätzer des Veräußerungswerts, wird aber im Bericht als Modellannahme deklariert; Endzahlungs-Positionen sind über StartJahr bereits abbildbar |
| V-G5 | **Szenarien = gleichzeitige Variation aller Einstellparameter** — auch r, T, Preisraten, Mengen (7.3) | Best/Worst variieren nur die Kosten-/Betragsspalten; r, T, p sind je Szenario fix | **entschieden 31.08.2026: vollständige Abdeckung** — alle Parameter (Investition, Energiekosten, Betriebskosten, Erlöse, Rahmen, Mengen) erhalten Best/Worst-Werte; Modell in § 2.11.5. **Stand: gebaut #461/#462** (V‑E, E9 Teil a im Kern, Teil b in den Dialogen) — die Schemaschritte 116 bis 118 und die Regeln des Kerns in § 2.11.5: Betrachtungszeitraum, Mengenänderung, Trägerpreise und Erlössätze je Szenario, NULL/0 = wie Erwartet; gepflegt über die Szenariotafel und den ±-Knopf, ausgewiesen als „n von m Parametern szenariert" |
| V-G6 | **Sensitivität**: die 7 Regelparameter, Ausweis mit **Steigung €/%** und Liniendiagramm (7.2, 8.1.3) | 5 Fälle vorhanden (Zins, p_E, Invest, Energie, KWKG-Wegfall) | fehlt: T-Variation, Endzahlungen; Ausgabeformat um Steigungsspalte + Diagramm ergänzen. **Stand: Steigungsspalte gebaut #434** (V‑A; Seite, Wort- und Tabellenbericht); T-Variation, Endzahlungen und Diagramm offen |
| V-G7 | **Risiko**: Zinszuschlag **oder** Abzug `R_loss × p_loss` auf die Periodennettosumme, nur t > 0 (6.5, Anhang F) | **gebaut #478** (E15, Schemaschritt 125) | optionales Risikomodul; Anhang F bevorzugt den Zahlungsstromabzug; Vorgabe aus. **Stand: gebaut #478** (E15, → Register R‑V, R‑E15) — `Risiko_Art` (leer = aus, `ZINS`, `ABZUG`), `Risiko_Zinszuschlag` [%-Punkte], `Risiko_Verlust` [€ je Periode] und `Risiko_Wahrscheinlichkeit` [%] an `Tab_ProjektWirtschaftlichkeit`, gepflegt in der Gruppe „Risiko (DIN EN 17463, 6.5)" des Parameterdialogs; eine Stelle der Regeln (`RisikoModul`): **Zinszuschlag** — i + Zuschlag in allen drei Szenarien (`FuerSzenario`), daraus Kapitalwert, Annuität, Amortisation, Zinsfuß, Verlauf und Sensitivität (E15‑Q1 a); **Zahlungsstromabzug** — R_loss × p_loss / 100 mindert die Nettozahlung jeder Periode ab Jahr 1, nicht Jahr 0 und nicht den Restwert (`KapitalwertRechner.Rechne`), als eigener Bestandteil RISIKO (E15‑Q2 a), für jeden Stand außer der Referenz des Laufs, ein Einzelstand trägt ihn selbst (E15‑Q4 a). **Lesart der Norm:** Anhang F, Tabelle F.2 rechnet R_loss als Prozent des Nettorückflusses (ded_t = P_t × R_loss × p_loss); gebaut ist R_loss als **Betrag in € je Periode**, weil die Nettozahlungen eines Standes meist negativ sind — die Prozentlesart auf der Differenzreihe steht als E15‑Q4 c zur Entscheidung. Ausweis nur bei Pflege (E15‑Q3 a, § 2.11.5); ohne Pflege bitgleich (Anker, Referenzlauf 13/13). PV-Vergütungsdialog und `KostenKomponenteHuelle` rechnen ihre Vorschau ohne Zuschlag; die Ergebniszeile speichert bei Zinszuschlag den gerechneten Zins i + Zuschlag |
| V-G8 | **IZF/Amortisation nur nachrichtlich** (Anhang C) | Kacheln zeigen beide gleichrangig neben dem Kapitalwert | Kacheln behalten, aber als „nachrichtlich (Anhang C)" gekennzeichnet; **IZF-Mehrdeutigkeitswarnung** bei > 1 Vorzeichenwechsel der Differenzreihe — bei EPOS-Projekten durch Ersatzjahre und KWKG-Auslauf der Regelfall, nicht die Ausnahme. **Stand: gebaut #434** (V‑A) — Label an Amortisation und Zinsfuß, die Annuität ohne (E5‑Q3, Statusdatei „Nach #434"); Warnung bei mehr als einem Vorzeichenwechsel, ohne Wechsel „kein Zinsfuß bestimmbar" |
| V-G9 | **Steuerdeklaration Pflicht**: „Steuern berücksichtigt: ja/nein"; Abschreibungen nie als Cashflow, nur als Steuerschild (7.1.2) | Steuer-**Gutschriften** ja (Energie-/Stromsteuer), **Ertragsteuern** nein; keine AfA ✓ | zweiteilige Deklarationszeile: „Energie-/Stromsteuerentlastungen: berücksichtigt · Ertragsteuern: nicht berücksichtigt". **Stand: gebaut #434** (V‑A) — `WIRT_DEKL_STEUERN` in der Deklarationsliste auf Seite und in beiden Berichten |
| V-G10 | **Bericht** mit Pflichtinhalten a)–d) + **editierbarer XLSX mit Formeln** nach Anhang-A-Raster (9) | Excel-Export existiert (ClosedXML), aber als **Werte** — der Generator schreibt keine einzige Formel, und keine Zahl des Parametersatzes erreicht eine Zelle (gemessen 18.09.2026) | **größte Einzellücke mit hartem Muss**. **Entschieden 18.09.2026, abweichend von der Empfehlung: der ganze Bericht formelbasiert**, soweit ableitbar — Stufenplan und die Liste dessen, was dauerhaft Wert bleibt, in § 2.11.6; das ValERI-Blatt (Parameterblock mit absoluten Bezügen, Periodenspalten, Gesamt-/Barwert-/NPV-Zeile je Szenario) ist darin Stufe 0 und 1. **Stand: gebaut #455** (V‑D, E8 Teil b) — die Stufen 0 bis 3 nach § 2.11.6: Parameterblock aus echten Zellen mit Namen, Mehrjahrestabellen und die Kennzahlen des Szenarios Erwartet in Formeln, bemessene Betriebskosten als Menge × Satz, der Δ%-Block als Zellbezug; EPOS trägt zu jeder Formel den Wert ein, Excel rechnet beim Öffnen neu. **Ergänzt #477** (E14): Stufe 1 und 2 rechnen alle drei Szenarien formelbasiert, je Szenario auf seine Spalte im Parameterblock |
| V-G11 | **Nicht monetarisierbare Wirkungen**: erfassen, kategorisieren (Energiefluss / finanziell / sonstig), beurteilen nach Dauer × Wirkung auf Organisation/Mitarbeiter/Umwelt (6.1, 8.2) | **gebaut #479** (E17, Schemaschritt 127); vorher Freitext (W5‑B‑12/G6 des Szenarienkonzepts) | Kategorie und Beurteilung nach Dauer × Wirkung; fließt nie in den NPV, immer in den Bericht. **Stand: gebaut #479** (E17, → Register R‑V, R‑E17) — die Liste `Tab_ProjektWirkung` je Projekt (E17‑Q1 a): je Wirkung Kategorie (`ENERGIEFLUSS`, `FINANZIELL`, `SONSTIG`), Beschreibung, Dauer 1–3 (kurz, mittel, lang) und die Wirkung auf Organisation, Mitarbeiter und Umwelt je 0–3 (keine bis stark), NULL = nicht beurteilt; die **Beurteilung** ist Dauer × stärkste der drei Wirkungen, 0 bis 9, eine Anzeige an einer Stelle (`NichtMonetaereWirkungen.Beurteilung`, E17‑Q2 a), nicht gespeichert. Gepflegt im Baustein `WirkungenListe` des Bewertungsblocks an der Stelle des Freitexts; der Freitext `Nicht_Monetaer` bleibt als Altfeld lesbar, Schritt 127 übernimmt einen gepflegten Text als eine Wirkung „sonstig" ohne Beurteilung (E17‑Q3 a). Wort- und Tabellenbericht zeigen die Tabelle (Kategorie, Beschreibung, Dauer, drei Wirkungsgrade, Beurteilung; ohne Wirkung entfällt der Block), die Punkte 2b und 3b der Anhang-E-Checkliste folgen der Liste (V‑G12). Keine Rechenwirkung: kein Rechenweg liest die Tabelle (Anker bitgleich, Referenzlauf 13/13); vier Fragen → R‑E17 |
| V-G12 | **Anhang-E-Checkliste** (15 Punkte, Note 1–5) | fehlt | als Abschlussseite des Berichts; zugleich interne Abnahmecheckliste der Etappe. **Stand: gebaut #455** (V‑D, U43) — 15 Punkte in fünf Gruppen mit Anforderung, Stelle im Bericht und Stand aus dem Lauf (`AnhangECheckliste`); Abschlussseite des Wortberichts, letztes Blatt der Mappe mit der Notenspalte 1–5, Knopf „Anhang-E-Checkliste…" auf der Ergebnisseite. **Punkt 9 (Szenarioanalyse), gebaut #474** (E9b‑Q5 b, → Register R‑E9b): „erfüllt", sobald Günstig und Ungünstig mit Kapitalwert gerechnet sind — auch ohne gepflegten Parameter; „teilweise" bei nur Erwartet oder nur einem Szenario; „offen" ohne Lauf; der Ausweis „n von m Parametern szenariert" steht als Beleg im Punkt (§ 2.11.5). **Punkte 2b und 3b (nicht monetarisierbare Wirkungen), gebaut #479** (E17, V‑G11): „erfüllt", sobald mindestens eine Wirkung nach Dauer und Wirkung beurteilt ist; „teilweise" bei nur beschriebenen Wirkungen; „offen" ohne Wirkung — der Freitext zählt dafür nicht mehr, er ist nur noch Rückfall für Aufrufer ohne Lauf |

**Anhang D der Norm ist eine BHKW-Fallstudie** (90 kW_th, 18 Jahre, NPV 64.480 €, Worst −202.802 €,
Best +598.320 €) — sie dient der Etappe als **externe Gegenprobe**: EPOS muss mit denselben
Eingaben dieselben Zahlen treffen. *(Vorsicht: Zwei Zeilen der Sensitivitätstabelle D.6 tragen im
Normtext versehentlich Werte des Pumpenbeispiels — als Prüfreferenz ungeeignet, dokumentiert.)*
**Die Gegenprobe steht als Kern-Fall** (`AnhangDFallstudieTests`, umgesetzt #455) — gegen `KapitalwertRechner.Rechne`,
nicht gegen die volle Kette, weil die Norm Jahresmengen liefert und keinen Stundenlauf. Der Kapitalwert als Differenz
zweier Zahlungsbilder (BHKW gegen Kessel und Strombezug) trifft **64.479,51 €, −202.801,57 € und 598.319,65 €**
(Toleranz ±1 €, die Norm rundet auf ganze Euro); die Jahreswerte der Tafel D.5 stimmen, sechs Zeilen der Tafel D.6
treffen auf ±0,5 € samt Steigung (Energiepreisschwankung 472, Preisschwankung nicht energetisch −57, Laufzeit 1.121,
Kalkulationszins −889, CAPEX −900, OPEX −354 € je Prozent der Änderung). Die Norm schreibt einen Basiswert zu Preisen
des Jahres 0 mit (1 + p)^t fort, der Rechenkern den Betrag des Jahres 1 mit (1 + p)^(t−1); übergeben wird deshalb
Basis × (1 + p). **Ausgenommen** sind neben den zwei Zeilen des Pumpenbeispiels die D.6-Zeile „Gasverbrauch BHKW" —
sie trifft die Änderung des ganzen Energie-Nettostroms, nicht die des Gasverbrauchs —; Tafel D.7 nennt im
wahrscheinlichsten Fall 348.583 statt 349.583 kWh/a Strom, ein Tippfehler (nur die zweite Menge ergibt die 64.480 €).
Der Rechenweg ist unverändert.

### 2.11.3 Die fünf Darstellungsblöcke

Andockung nach § 2.10 in der Wirtschaftlichkeitsseite (`WirtschaftlichkeitSeite.razor`), Referenz nach § 2.9. Alle Blöcke rendern
vorhandene Größen; Beispielzahlen in den Mockups aus der Mappe „Beispielprojekt B" (20-kW-BHKW-Erneuerung
gegen benannte Vergleichsheizung — Kapitalwert 65.259 €, IZF 20,4 %, Amortisation 4,33 a).

| Block | Inhalt | Normbezug |
|---|---|---|
| **Investitionskosten** | Anlage · Referenz · Differenz; Zuschusszeile; je Szenario | 6.1, 7.3 |
| **Betriebskosten** | Positionsliste mit den Normattributen Zeitpunkt · Preisrate · Degradation · Quelle | 6.3.1 |
| **Erlöse** | die Rubrik aus § 2.6 — in der Differenzsicht sind vermiedene Bezüge reguläre Differenz-Cashflows (die Referenzkosten laufen als Gegenposition), Block-B-Kennzeichnung bleibt für die Absolutsicht | 6.1 |
| **Energiekosten** | je Träger, Anlage gegen Referenz, BEHG mit Preispfad, Preisraten-Ausweis | 6.3.2 |
| **Wirtschaftlichkeit über Nutzungsdauer** | kumulierter diskontierter Cashflow (drei Szenarien), NPV-Regel, Kennzahlen mit „nachrichtlich"-Kennzeichnung, Sensitivitätstafel mit Steigung €/%, Deklarationszeilen (nominal · Steuern · Restwert · Risiko) | 7, 8, Anhang A |

*Zur Nummerierung: Die Tafel ordnet den Inhalt nach den Kostenarten der Norm; die Seite, das Mockup und § 2.11.4
zählen dieselben fünf Blöcke nach dem Aufbau der Bewertung (`WIRT_VALERI_BLOCK_1` … `_5`): **1 · Gegenstand und
Rahmen** (Maßnahme, Referenz nach § 2.9, Betrachtungszeitraum, Kalkulationszins, Rechnung nominal), **2 ·
Zahlungsreihen** (die Zeilen Investitionskosten, Betriebskosten, Erlöse und Energiekosten der Tafel, dazu Ersatz und
Restwert — je Jahr und als Barwert, mit dem Zahlungsstrombild), **3 · Kennzahlen**, **4 · Unsicherheit** und **5 ·
Deklarationen** (zusammen die Zeile „Wirtschaftlichkeit über Nutzungsdauer": Kennzahlen und NPV-Regel in Block 3,
drei Szenarien, Bandbreite, Spannenbild, Verlauf und Sensitivität in Block 4, die Deklarationszeilen in Block 5).
Es sind zwei Ordnungen desselben Inhalts, nicht zwei Blocksätze; gebaut ist die Nummerierung 1 bis 5 (#434, #454).*

### 2.11.4 Etappen und Entscheidungen

Die Spalte „entspricht / bereits geliefert durch" löst die zweite Etappenreihe des
Szenarienkonzepts (`W5‑B‑9` … `W5‑B‑12`) gegen diese auf — beide Reihen meinen teilweise dieselbe
Arbeit.

| Etappe | Inhalt | entspricht / bereits geliefert durch | Wirkung | Stand im Etappenplan E0–E12 |
|---|---|---|---|---|
| **V-A** | Ausweis: „nachrichtlich"-Kennzeichnung der Kacheln, IZF-Mehrdeutigkeitswarnung, Deklarationszeilen, Steigungsspalte der Sensitivität | gebaut **#434** (Merge `deba5e57`): `WirtschaftlichkeitZeilen.IstNachrichtlich` (Amortisation und Zinsfuß, E5‑Q3), `KapitalwertRechner.Vorzeichenwechsel` mit Nachweisumschlag Fassung 7, `ValeriAusweis.Deklarationen()`, `SensitivitaetZeile.Steigung` — auf der Seite und in beiden Berichten | keine | **E5** (mit der Ergebnisansicht) — gebaut |
| **V-B** | Referenzwahl (§ 2.9) — umgesetzt | Etappe **VG**, Statuszeile **#358**, Schemaschritt 92 | keine in der Vorgabe | gebaut |
| **V-C** | ValERI-Ansicht (fünf Blöcke + Cashflow-Chart) in der Wirtschaftlichkeitsseite | vorgezogen mit **#434** (die Darstellung „ValERI-Bewertung" hinter dem Umschalter mit den Blöcken 1, 3, 4 und 5) und **#436** (das Cashflow-Bild als Verlauf mit drei Szenarien unter „Wie sicher ist das?"); **vollständig mit #454** (E8 Teil a): **Block 2 „Zahlungsreihen"** je Stand und Szenario — Jahrestafel der sechs Bestandteile Investition, Betriebskosten, Energiekosten, Erlöse, Ersatzbeschaffungen und Restwert mit Netto, Barwert und „Summe nominal", darunter das **Zahlungsstrombild** (gestapelte Jahresbalken der Positionsspalten der Mehrjahrestafel, Ausgaben nach unten, Ersatzjahre markiert, ohne Restwert; E8a‑Q1, → Register R‑E8a), Vorgabe die Leitversion im Erwartungsfall (größte Kapitalwertdifferenz, in Sicht 2 der Stand B; E8a‑Q2); **Block 4** mit Spannenbild und Verlauf aus denselben Bausteinen wie „Wie sicher ist das?" (E6‑Q1, → Register R‑E6); in „Woraus entsteht die Zahl?" die **Gliederung des Kapitalwerts** mit Barwert und Nominalsumme je Bestandteil und der Spalte „Differenz ‹Leitversion› − ‹Referenz›", die in der Kapitalwertdifferenz aufgeht (U46), darunter das **Brückenbild** „Von der Investition zur Kapitalwertdifferenz" (U41, auch im Wortbericht); in „Was ist angenommen?" die Tafel **„Was daraus im Lauf wird"** (je Szenario I₀, die Jahre der fälligen Ersatzbeschaffungen und der Restwert am Ende, nominal — U47) und die **Fußzeile** „Drei Szenarien gerechnet · Annahmen aus Vorgaben, nichts gepflegt" (U48; mit #455 links in der Reihe der Knöpfe „Anhang-E-Checkliste…" und „Bericht erzeugen", E8a‑Q4). Alles ist Ausgabe: `Zahlungsgliederung` ordnet das Zahlungsbild des Laufs, die sechs Barwerte ergeben den Kapitalwert, gezeigt wird nur, was zum gespeicherten Ergebnis passt; die Jahresreihen stehen nach einem Lauf in der Sitzung, gespeicherte Ergebnisse tragen keine (E8a‑Q3) | Ausweis | **E8** Teil a — gebaut |
| **V-D** | XLSX-Formelbericht nach Anhang-A-Raster + Berichtsinhalte a)–d) + Anhang-E-Checkliste; **Gegenprobe an der Anhang-D-Fallstudie** | deckt sich mit **V-G10** (Entscheid 18.09.2026, § 2.11.6); **gebaut mit #455** (E8 Teil b): die **Formelmappe** in den Stufen 0 bis 3 (§ 2.11.6) samt der Blattstruktur-Wache über Excel- und Wortbericht und der Wache über den ClosedXML-Befund; die **Anhang-E-Checkliste** als Abschlussseite beider Berichte und hinter dem Knopf „Anhang-E-Checkliste…" im Fuß des Bewertungsblocks (V‑G12, U43); die **Gegenprobe an der Anhang-D-Fallstudie** (§ 2.11.2). Alles ist Ausgabe: die Werte der Mappe gleich denen der Wertfassung (13 Prüfgruppen), die Anker unverändert. **Ergänzt #477** (E14, nach dem Befund 1 aus E9a): Stufe 1 und 2 auch für Günstig und Ungünstig — je Stand und Szenario eine Mehrjahrestabelle bis zum längsten Zeitraum mit Schutzformel jenseits von T_s, Kennzahlen, Zinsfuß und Bandbreite als Formeln auf die Spalte des Szenarios im Parameterblock; Wertfassung = Formelfassung in 16 Prüfgruppen, Anker und Referenzlauf unverändert; E14‑Q2 a löst E8b‑Q1 a ab (→ Register R‑E14, R‑E8b) | Ausgabe | **E8** Teil b — gebaut; ergänzt #477 (E14) |
| **V-E** | Vollständige Szenarioabdeckung nach § 2.11.5 (V-G5, Umfang entschieden 31.08.2026), Risiko (V-G7), n-jährliche Zeitpunkte (V-G3) — **ohne Degradation (V-G2), A5** | Szenarioabdeckung und Freitext teils geliefert durch **W5‑B‑9** und **W5‑B‑12** (Migrationsschritte 71, 72); **Teil a gebaut #461** (E9a): die Schemaschritte 116 (Betrachtungszeitraum und Mengenänderung je Szenario), 117 (Trägerpreise best/worst) und 118 (Erlössätze best/worst), der Kern liest die Paare je Größe an einer Stelle (§ 2.11.5, „Regeln des Kerns"), Nachweiszeile, Parameterblock und Verlauf je Szenario; `SzenarioParameterTests`, A/B-Nachweis über neun Größen mit Erwartet bitgleich; **Teil b gebaut #462** (E9b): die Zeilen 8 (Betrachtungszeitraum) und 9 (Mengenänderung) der Szenariotafel, der ±-Knopf an Trägerpreisen und Erlössätzen (ein verallgemeinerter `CaseEingabeDialog`), der Ausweis „n von m Parametern szenariert" an der Stelle des Hinweistexts (§ 2.11.7); `SzenarioAbdeckungTests` und die Dialogproben, ohne Pflege keine Rechenwirkung. Risiko (V-G7) und n-jährliche Zeitpunkte (V-G3) baut E9 nicht (E9a‑Q6, → Register R‑E9a). **Risiko (V‑G7) gebaut #478** mit dem eigenen Auftrag E15 (Schemaschritt 125, § 2.11.2, → Register R‑V, R‑E15): Zinszuschlag oder Zahlungsstromabzug, Vorgabe aus, A/B-Nachweis an 1030, 1019 und 1024; **n-jährliche Zeitpunkte (V‑G3) gebaut #484** mit dem eigenen Auftrag E16 (Schemaschritt 129, § 2.11.2, → Register R‑V, R‑E16): die Wiederholperiode je Betriebsposition, leer = jährlich, A/B-Nachweis an 1030. Den Freitext aus W5‑B‑12 löst **E17 (#479)** ab: die nicht monetarisierbaren Wirkungen (V‑G11) als Liste mit Kategorie und Beurteilung (Schemaschritt 127, § 2.11.2, → Register R‑E17), ohne Rechenwirkung. Mit V‑G3 (E16, #484) ist die Gap-Tafel § 2.11.2 geschlossen — keine Lücke offen | **ja** — je Pflege, mit A/B-Nachweis; NULL = wie Erwartet hält die Etappe bis zur ersten Pflege ergebnisneutral; ebenso das Risiko (Vorgabe aus) und die Wiederholperiode (leer = jährlich) | **E9** — gebaut #461 (Kern) und #462 (Dialoge, Ausweis); E9 abgeschlossen; Risiko gebaut #478 (E15), V‑G3 gebaut #484 (E16) |

*Die Spalte „Stand" verweist auf den Etappenplan E0–E12 des Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md) § 5;
gebaut sind daraus E0 (#379), E1 (#380), E2 (#405), E3 (#431), E4 (#432), E5 (#434), E6 (#436), E7 (#437, #439,
#440, #446, #452), E8 (#454, #455; die Nachbesserung E8c #460) und E9 (#461, #462); außerhalb des Plans die kleine
Bauwelle E13 (#474), die Formelmappe je Szenario E14 (#477, ergänzt V‑D), das Risikomodul E15 (#478, V‑G7 aus
V‑E, Schemaschritt 125), die nicht monetarisierbaren Wirkungen E17 (#479, V‑G11, Schemaschritt 127) und die
Wiederholperiode je Kostenposition E16 (#484, V‑G3 aus V‑E, Schemaschritt 129).*

*Entscheid A5 (Degradation) und die Entscheidungsfragen V-1 bis V-4: → Register R‑A (A5) und R‑V;
der Entscheidweg zu A5: → Protokoll § 5.5.*

### 2.11.5 Vollständige Szenarioabdeckung

**Umfang (V-G5, → Register R‑V):** Alle Parameter — Investitionskosten, Energiekosten, Betriebskosten,
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
| **Energiepreise** je Träger | `energy_project_settings.custom_price_work` (+ Grundpreis) | neue Spalten `custom_price_work_best/_worst` (Grund-/Leistungspreis analog, nullable) | **gebaut #461/#462** — Spalten aus Schritt 117: `custom_price_work_best/_worst`, `custom_price_base_best/_worst`, `custom_price_power_best/_worst` (Kern #461); gepflegt mit dem ±-Knopf je Preis an der Trägerkarte im Projekt (#462) |
| **Erlössätze** (Marktgrößen) | `Einspeiseverguetung`, `Einspeiseverguetung_KWK`, PPA-/DV-Preise des PV-Dialogs | je Feld ein Best/Worst-Paar an derselben Tabelle | **gebaut #461/#462** — Spalten aus Schritt 118: `Einspeiseverguetung_Best/_Worst`, `Einspeiseverguetung_KWK_Best/_Worst` an `Tab_ProjektWirtschaftlichkeit`, `DvEntgelt_Best/_Worst`, `PpaPreis_Best/_Worst` an `Tab_ProjektPhotovoltaik` (Kern #461); nicht szenariert sind der Spot-Aufschlag des PPA und die Marktwertfelder (E9a‑Q1); gepflegt mit dem ±-Knopf am Feld — Einspeisevergütung PV im Parameterdialog, KWK im Dialog „BHKW-Wirtschaftlichkeit", DV-Entgelt und PPA-Preis im PV-Vergütungsdialog (#462) |
| **Rahmen** | `Tab_ProjektWirtschaftlichkeit`: `Zinssatz`, `Betrachtungszeitraum`, `Preissteigerung_Energie`, `Preissteigerung_Betrieb` | je Größe `_Best`/`_Worst` (8 Spalten), NULL/0 = wie Erwartet | **8 von 8, gebaut #461/#462:** 6 seit Schritt 71 (`Szen_Best/Worst_Zins`, `_Preis_E`, `_Preis_B`), gepflegt in den Zeilen 1 bis 3 der Szenariotafel (p_I in Zeile 4); der **Betrachtungszeitraum** aus Schritt 116: `Szen_Best/Worst_Zeitraum` (ganze Jahre, Kern #461), gepflegt in Zeile 8 (#462). Namensvorsicht: `Szen_*_Dauer` ist die **Nutzungsdaueränderung**, nicht der Betrachtungszeitraum |
| **Mengen** (Simulationsergebnis) | Stromerzeugung, Wärme, Einspeisung … | **ein Mengenfaktor [%] je Szenario** an der Rahmenzeile (wirkt multiplikativ auf die Energie- und Erlösmengen) — die Simulation selbst wird nicht dreifach gerechnet | **gebaut #461/#462** — Spalten aus Schritt 116: `Szen_Best/Worst_Menge` (%, Kern #461), gepflegt in Zeile 9 der Szenariotafel (#462) |

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
- **Pflege** (umgesetzt #462, E9b): der vorhandene ±-Knopf (`CaseEingabeDialog`) als einheitliches Muster
  auch an Trägerpreisen und Erlösfeldern — der Dialog ist dafür ein allgemeiner Baustein (Beschriftung,
  Erwartet-Wert, Einheit, Best/Worst, Nachkommastellen, Grenzen, Kohärenzzeilen), die Kostenposition bleibt,
  wie sie war (E9b‑Q1, → Register R‑E9b). Der Knopf steht je Preis (Arbeit, Grund, Leistung) an der
  Trägerkarte im Projekt, an der Einspeisevergütung PV im Parameterdialog, an der Einspeisevergütung KWK im
  Dialog „BHKW-Wirtschaftlichkeit" — dort wird ihr Erwartet-Wert gepflegt — und an DV-Entgelt und PPA-Preis
  des PV-Vergütungsdialogs; ein Kennzeichen ● zeigt ein gepflegtes Paar, ein Warnzeichen ⚠ einen
  Szenariopreis ohne Erwartet-Preis — gespeichert wird trotzdem (E9b‑Q4); im Katalog trägt die Trägerkarte
  keine Knöpfe. Die **Rahmen-Gruppe** (Zins, Preissteigerungen, Betrachtungszeitraum) und die
  **Mengenänderung** pflegt die Szenariotafel des Parameterdialogs: neun Zeilen, die Zeilen 8
  (Betrachtungszeitraum, ganze Jahre) und 9 (Mengenänderung, %) ohne Vorgabe; „Vorgaben" leert alle
  achtzehn Felder und lässt die Einspeisevergütungen stehen. Einen eigenen ±-Knopf hat die Rahmen-Gruppe
  nicht — die Tafel trägt sie schon.
- **Ausweis „n von m Parametern szenariert"** (umgesetzt #462, E9b‑Q2 und E9b‑Q3, → Register R‑E9b): unter
  der Annahmentafel der Seite und in Block 4, im Wort- und im Tabellenbericht an der Stelle des Hinweistexts
  (§ 2.11.7) und in Punkt 9 der Anhang-E-Checkliste; gepflegte Größen folgen als Liste („2 von 16 Parametern
  szenariert: Betrachtungszeitraum, Arbeitspreis Erdgas E"), ohne Parametersatz steht kein Ausweis.
  **Zählregel** (`SzenarioAbdeckung`): m = die sieben Größen des W5‑B‑9-Satzes, Betrachtungszeitraum,
  Mengenänderung, Einspeisevergütung PV und KWK, je Vergütungszeile eines Standes mit PV-Anlage DV-Entgelt
  und PPA-Preis, je Träger mit Verbrauch und Stand Arbeits- und Grundpreis, der Leistungspreis beim
  Stromträger immer, sonst nur, wo einer gepflegt ist; n = die Parameter, deren Best- oder Worst-Wert um
  mehr als 1e−9 vom Erwartet-Wert abweicht — bei den sieben Größen zählt nur ein eingetragenes Feld, die
  Vorgabe nicht. **Lesart:** Ohne Pflege steht „0 von m", obwohl die Vorgaben der Tafel Günstig und
  Ungünstig verschieben; die Einspeisevergütung KWK zählt auch ohne BHKW (beides zum Mitentscheiden,
  E9b‑Q2). In Punkt 9 der Anhang-E-Checkliste ist der Ausweis Beleg, nicht Bedingung: Der Punkt steht auf
  „erfüllt", sobald Günstig und Ungünstig gerechnet sind (E9b‑Q5 b, gebaut #474; § 2.11.2, V‑G12).
- **Ausweis im Bericht** (Norm 9c): Die Kalkulationstabelle je Szenario nennt die
  Parametereinstellungen vollständig — die Szenariospalten der Rahmenzeile erscheinen im
  Parameterblock des XLSX-Blatts. **Umgesetzt #455 und #461:** Die Nachweiszeile je Szenario
  (`SzenarioSatz.Nachweis` — Szenariozeile der Seite, Herleitungszeile des Parameterdialogs,
  Annahmenzeilen der Berichte) nennt Zins, Betrachtungszeitraum, die drei Preissteigerungen, die
  Änderungen an Investition, Erträgen und Nutzungsdauer und die Einspeisevergütung immer, die
  Einspeisevergütung KWK, wo Projekt oder Szenario eine führt, die Mengenänderung nur, wenn sie
  gepflegt ist; gepflegte Trägerpreise stehen je Stand in einer eigenen Zeile. Der Parameterblock der
  Formelmappe (§ 2.11.6, Stufe 0) führt dazu Mengenänderung und Einspeisevergütung PV und KWK je
  Szenario, den Betrachtungszeitraum je Szenario und je gepflegtem Trägerpreis eine Zeile je Stand,
  Träger und Preisart; die Annahmentafel der Seite nennt den Betrachtungszeitraum je Szenario und die
  Zeilen Mengenänderung und Einspeisevergütungen nur bei Pflege. **Risiko (umgesetzt #478, V‑G7, E15‑Q3 a):** Ist
  ein Risiko gepflegt, nennt die Nachweiszeile — für das Projekt und je Szenario — „Risiko (DIN EN 17463, 6.5)" mit
  dem Zuschlag bzw. mit R_loss × p_loss je Periode ab Jahr 1; die Annahmentafel führt die Zeile „Risiko (DIN EN 17463,
  6.5)", die Deklaration heißt „Risiko angesetzt (6.5): …" statt „Risikozuschlag nicht angesetzt (6.5 optional)",
  Punkt 6 der Anhang-E-Checkliste nennt es, und der Parameterblock der Formelmappe trägt die Risikozeilen (§ 2.11.6,
  Stufe 0). Das Risiko ist kein Szenariowert: Es gilt in allen drei Szenarien gleich (E15‑Q1 a) und zählt nicht im
  Ausweis „n von m Parametern szenariert". Ohne Pflege steht nichts davon.

**Regeln des Kerns (umgesetzt #461, E9a — je Größe genau eine Lesestelle):**

- **Keine Vorgaben für die neuen Größen:** Betrachtungszeitraum, Mengenänderung, Trägerpreise und
  Erlössätze haben keine Richtungsvorgabe — leer oder 0 heißt „wie Erwartet" (anders als die sieben
  Größen des W5‑B‑9-Satzes, deren leeres Feld der Vorgabe folgt, Szenarienkonzept § 2.1). Gepflegt ist
  ein Wert, wenn er sich um mehr als 1e−9 vom Erwartungswert unterscheidet (`SzenarioSatz.Gepflegt`) —
  dieselbe Regel für Trägerpreise und PV-Erlössätze (E9a‑Q5, → Register R‑E9a). Ohne Pflege bekommt
  jedes Szenario dieselbe Referenz wie Erwartet und rechnet bitgleich.
- **Zeitraum je Szenario:** Ein gepflegter Betrachtungszeitraum T_s (ganze Jahre ≥ 1) rechnet das
  Szenario genau wie einen Lauf mit diesem Zeitraum — Horizont, Restwert am Ende von T_s,
  Ersatzbeschaffungen innerhalb von T_s, die PV-, KWKG- und CO₂-Reihen; Verlauf und Zahlungsgliederung
  laufen je Szenario über T_s (E9a‑Q4). Lesestelle: `WirtschaftlichkeitParameter.FuerSzenario`, der
  Verlauf über `BerechneVerlaufSzenarienJeZeitraum`.
- **Mengenfaktor:** f = 1 + Menge/100 (nie negativ) auf das Mengengerüst des Simulationsergebnisses der
  Variante — Erzeugung von Strom und Wärme, Einspeisung, Bezug, Brennstoffeinsatz, Hilfsenergie,
  Vollbenutzungsstunden, die Stundenreihen und die Bezugsspitze —, bevor Preise, Sätze und Kontingente
  sie treffen. Leistungen aus Gerätedaten, Prozente, Festbeträge (Grundpreis, Pauschalen, Zuschuss,
  Investition) und gesetzliche Sätze bleiben; ein Deckel begrenzt auch das Szenario; die Simulation
  wird nicht erneut gerechnet. Die Ertragsänderung wirkt daneben eigenständig auf die Erlöszeilen der
  Eingabe — beide multiplizieren sich (+10 % und +10 % ergeben +21 %) (E9a‑Q2). Lesestelle:
  `SzenarioMengen` (in `WirtschaftlichkeitCtrl.Szenariodaten` und im `EndenergieAufloeser`).
- **Trägerpreis-Ersatz:** Ein gepflegter Szenariopreis ersetzt den wirksamen Erwartet-Preis des Trägers
  als Ganzes (nach der Rückfallkette Projektwert → Preisstand → Katalog) — Arbeits-, Grund- und
  Leistungspreis je für sich; die Preisanteile der Zerlegung bleiben prozentual. Eine gepflegte
  Leistungspreis-Staffel oder saisonale Leistungspreisreihe gilt auch im Szenario; ein
  Szenario-Leistungspreis bleibt dann ohne Wirkung, und eine Kohärenzzeile sagt es (E9a‑Q3).
  Lesestelle: `TraegerpreisSzenario.Wirksam`, angewandt in `KostenEmissionRechner.LadeTraeger` und
  `WirtschaftlichkeitCtrl.StromArbeitspreisEurJeKwh`.
- **Erlössätze:** Einspeisevergütung PV und KWK je Szenario in `FuerSzenario`, DV-Entgelt und PPA-Preis
  in `ProjektPhotovoltaikCtrl.FuerSzenario` (PV-Vergütungsrechnung); der Spot-Aufschlag des PPA und die
  Marktwertfelder sind nicht szenariert (E9a‑Q1).
- **Rollenmodell:** Ist die Tarifstruktur im Rollenmodell aktiv, bleiben die Rollenpreise (Bezug,
  Reststrom, Einspeisung) in allen Szenarien die Erwartet-Preise; ein Szenario-Strompreis und eine
  Szenario-Einspeisevergütung bleiben ohne Wirkung, je eine Kohärenzzeile nennt das — ebenso die flache
  Einspeisevergütung PV neben einem aktiven PV-Vergütungsdialog, der mit DV-Entgelt und PPA-Preis je
  Szenario rechnet (E9a‑Q7). Kohärenzzeilen dieser Art sind Hinweise ohne Rechenwirkung.
- **Nachweis:** A/B je Größe mit Erwartet bitgleich, Referenzlauf byte-gleich (keine Pflege in den
  Referenzprojekten), `SzenarioParameterTests` (§ 6.2); die Tafel steht im Protokoll E9a.

### 2.11.6 Formelbericht — Stufenplan und Grenze

**Der ganze Bericht wird formelbasiert, nicht nur das ValERI-Blatt** (V-G10, das kippt V-2 —
→ Register R‑V). Die Grenze ist am Generator gemessen und gegengelesen; die Messung steht im
Protokoll § 3.11 und begrenzt den Stufenplan unten. **Gebaut sind alle vier Stufen mit #455** (E8 Teil b, V‑D; die
Klasse `ExcelFormelmappe` im Tabellenbericht); **mit #477** (E14) rechnen die Stufen 1 und 2 **alle drei Szenarien
formelbasiert**, jedes aus seinem eigenen Parametersatz — was die Mappe trägt, steht unter der Tafel.

**Stufenplan** — jede Stufe ein eigener Schritt mit Gegenprobe (die Mappe muss vor und nach der
Stufe dieselben Werte zeigen; die Formelfassung wird gegen die Wertfassung gehalten):

| Stufe | Inhalt | Warum in dieser Reihenfolge |
|---|---|---|
| **0** | **Parameterblock aus echten Zellen**: Kalkulationszins, Betrachtungszeitraum, die drei Preissteigerungen (Energie, Betrieb, Investition/Ersatz), je Szenario ein Satz; absolute Bezüge darauf | Voraussetzung für alles Weitere — solange die Annahmen Prosa in einer Zelle sind, lässt sich keine Formel verankern |
| **1** | **Mehrjahrestabelle** des Wirtschaftlichkeitsblatts (das Raster steht: Jahre als Zeilen, Zahlungspositionen als Spalten): *Energie* als Fortschreibung Jahr 1 × (1+p_E)^(t−1); *Netto* als Zeilensumme; *Barwert* als Netto × (1+i)^−t; *Kumuliert* als Laufsumme; *Betrieb* als zwei Terme (Betriebs-Topf mit p_B, Endenergie-Topf mit p_E) mit Stufenlogik oder Hilfsspalte für Positionen mit späterem Startjahr; *BEHG* nur im Rückfallzweig als Fortschreibung, mit jahresscharfer CO₂-Reihe bleibt sie zugelieferter Preispfad | größter Nutzen: genau diese Größen variiert der Anwender im Gespräch, und die Tabelle zieht mit |
| **2** | **Ergebniskennzahlen**: Nettobarwert und Annuität über NBW/RMZ auf die Spalten der Stufe 1; interner Zinsfuß und Amortisation über eine **Differenzreihe Variante − Referenz** (samt Restwert-Nominaldifferenz im letzten Jahr), die das Blatt heute nicht führt und je Variante bekommt; benannter Leerwert bei fehlendem Vorzeichenwechsel als Text, nicht als Zellfehler | die Kennzahlen hängen an Stufe 1 und an einer Reihe, die erst entstehen muss |
| **3** | **Betriebskostenblock**: Menge und Satz in eigene Spalten, Betrag als Produkt — nur für bemessene Positionen; die Spalte Herleitung bleibt für feste, szenariogepflegte und unvollständige Positionen. **Delta-Block** des Vergleichsblatts als Zellbezug (Wert − Referenz) / |Referenz| | kleine Blöcke gleicher Mechanik; kosmetisch |

**Was die Mappe trägt** (umgesetzt #455, je Szenario ergänzt #477):

- **Stufe 0** — unter der Prosazeile des Parameternachweises der Block „Parameter der Rechnung (je Szenario)" mit den
  Spalten Erwartet, Günstig und Ungünstig: Kalkulationszins, Betrachtungszeitraum und die Preissteigerungen p_E, p_B,
  p_I als Dezimalzahl (derselbe Ausdruck Prozent ÷ 100, mit dem der Rechenkern sie liest), dazu die Änderungen an
  Investition, Erträgen und Nutzungsdauer (Norm 9 c). Die Spalte Erwartet trägt die Namen `Zins_i`, `Zeitraum_T`,
  `p_E`, `p_B`, `p_I`, die beiden anderen dieselben mit `_Guenstig` bzw. `_Unguenstig`; darunter ein Hinweis und die
  drei Sätze der Grenze. Die Formeln rechnen je Szenario mit seiner Spalte (#477): Wer dort einen Satz ändert, sieht
  Mehrjahrestabellen, Kennzahlen und Bandbreite dieses Szenarios mitziehen. Die Zeile „Betrachtungszeitraum T_s je
  Szenario [a]" nennt den Zeitraum jedes Szenarios; die Jahreszeilen stehen fest und reichen bis zum längsten der drei
  Zeiträume, Jahre nach T_s eines Szenarios bleiben leer — ein längerer Betrachtungszeitraum verlangt einen neuen
  Bericht. **Risikozeilen (#478, V‑G7):** Nur bei gepflegtem Risiko; beim Zinszuschlag stehen „Kalkulationszins"
  (`Zins_Basis`) und „Risikozuschlag auf den Zins (6.5)" (`Risiko_Zuschlag`) als Werte, der Kalkulationszins mit
  Risikozuschlag `Zins_i` ist ihre Summe als Formel; beim Zahlungsstromabzug stehen „Rückflusseinbuße R_loss [€ je
  Periode]" (`Risiko_Verlust`) und „Eintrittswahrscheinlichkeit p_loss" (`Risiko_p`) als Werte, „Risikoabzug je
  Periode ab Jahr 1 [€]" (`Risiko_Abzug`) als ihr Produkt — je Szenario mit dem Anhang des Szenarios. Ohne Risiko ist
  Stufe 0 unverändert.
- **Stufe 1** — die Mehrjahrestabellen wie in der Tafel; die Hilfsspalten „Basis Betrieb mit p_B" und „Basis Betrieb
  mit p_E" tragen die Basis beider Töpfe je Jahr samt den Stufen der Positionen mit späterem Startjahr (aus den
  Ausweisfeldern `BetriebBasisJeJahr` und `EndenergieBasisJeJahr` des Zahlungsbilds); **Positionen „alle n Jahre"** (#484, V‑G3) stehen je Topf in einer
  weiteren Hilfsspalte „Positionen alle n Jahre mit p_B [€/a]" bzw. „… mit p_E [€/a]" mit
  `IF(AND(Jahr>=s,MOD(Jahr-s,n)=0),Betrag,0)` je Position, die Basisspalte trägt den jährlichen Rest, die Betriebszelle
  rechnet `(Basis+Wiederholt)*(1+p)^(Jahr-1)` — ohne Periode steht keine Spalte da; die CO₂-Abgabe ist nur im
  Rückfallzweig eine Formel (`BehgFortgeschrieben`); die Abschlusszeile trägt den nominalen Restwert, seinen Barwert
  und den Nettobarwert. **Je Szenario (#477, E14‑Q1 a, → Register R‑E14):** Unter den Tabellen von Erwartet steht je
  Szenario Günstig und Ungünstig ein Block „Mehrjahresübersicht der Zahlungsströme — Szenario „…" (T = n a)" mit
  Hinweiszeile und je Stand einer Tabelle. Die Eingangswerte kommen aus dem Lauf des Szenarios
  (`BerechneVerlaufSzenarienJeZeitraum`), die Formeln rechnen mit seiner Spalte im Parameterblock (`p.FuerSzenario(s)`);
  die Energiespalte ist der Kernwert des ersten Jahres aus dem Szenariolauf, fortgeschrieben mit dessen p_E —
  Menge × Preis je Träger ist dort nicht zerlegt, Grund- und Leistungspreise bleiben Werte. Die Tabellen reichen bis zum
  längsten Zeitraum der drei Szenarien; jenseits von T_s hält die Schutzformel `IF(A<=Zeitraum_T_s,…,"")` die Zeile
  leer, der Restwert steht am Ende von T_s. Der Nachweisblock „vermiedene Kosten" steht nur bei Erwartet.
  **Risiko (#478, V‑G7):** Beim Zahlungsstromabzug trägt jede Tabelle eines Standes mit Abzug — in jedem Szenario —
  die Spalte „Risikoabzug" als `=-Risiko_Abzug` (mit dem Namen des Szenarios) ab Jahr 1, im Jahr 0 und in der
  Restwertzeile nicht; Netto, Barwert und Kumuliert schließen sie ein. Beim Zinszuschlag rechnen Stufe 1 und 2 mit dem
  gerechneten Zins `Zins_i`. In der Gliederung des Kapitalwerts ist das Risiko der Bestandteil RISIKO „Risikoabzug
  (Anhang F)" vor dem Restwert (E15‑Q2 a).
- **Stufe 2** — im Block „Erwartet" der Nettobarwert je Stand über `NPV` (dazu Jahr 0 und der Barwert des
  Restwerts), die Kapitalwertdifferenz als Zellbezug, die Annuität über `PMT`; rechts neben jeder Tabelle einer
  Variante die Differenzreihe Variante − Referenz (nominal, im Jahr T samt Restwert-Nominaldifferenz, Barwert,
  kumuliert, Nulldurchgang je Jahr); der Zinsfuß über `IRR` nur bei genau einem Vorzeichenwechsel, die Amortisation
  über die Hilfsspalte, ohne Wechsel bzw. Nulldurchgang der Satz der Seite als Text. **Dasselbe für die Blöcke
  Günstig und Ungünstig** auf ihre eigenen Tabellen (#477, E14‑Q2 a, → Register R‑E14; E8b‑Q1 ist abgelöst). Zwei
  Hilfsspalten je Differenzreihe tragen das Vorzeichen (über Nullwerte ≤ 1E‑6 € fortgeschrieben) und die Zahl der
  Wechsel bis zum Jahr — die Regel von `KapitalwertRechner.Vorzeichenwechsel`; der Zinsfuß ist
  `ROUND(IRR(…)*100,2)` bei genau einem Wechsel, ohne Wechsel steht „kein Zinsfuß bestimmbar", bei mehr als einem
  „nicht eindeutig" — als Text in allen drei Szenarien (E14‑Q3 a). Die Bandbreitentafel führt die
  Kapitalwertdifferenzen der drei Szenarien als Zellbezug, die Spanne als `MAX-MIN`.
- **Stufe 3** — Menge und Satz einer bemessenen Position rechts des Betrags, der Betrag ihr Produkt (bei
  Prozentbemessung ÷ 100, ein Erlös negativ); welche Rechnung gilt, sagt der eine Rechenweg selbst:
  `BetriebskostenCtrl.Bemessungsfaktor` — 1 für eine Art „Satz je Einheit", 0,01 für eine Prozentart, sonst fest —,
  für jede der 16 bemessenen Arten; dieselbe Frage stellt die Herleitungsspalte (umgesetzt #460, § 3.4). Die Spalte
  „Bemessung" nennt jede Art mit dem Text des Bemessungskatalogs. Die Summe als Spaltensumme; der Δ%-Block als
  (Wert − Stamm) / |Stamm| · 100. Stufe 3 gilt für das Szenario Erwartet.
- **Keine Formel ohne Gegenrechnung:** Jede Formel wird vor dem Schreiben in C# nachgerechnet; weicht ihr Ergebnis
  vom Wert ab, bleibt die Zelle ein Wert (`Formelregister`). Die Mappe zeigt damit nie eine Formel, die etwas anderes
  rechnet als der Bericht sagt; in allen 13 Prüfgruppen gleicht die Formelfassung der Wertfassung (mit #460 über 15
  Prüfgruppen nachgemessen: Beträge, Mengen, Sätze und Formeln unverändert, nur die Texte der Spalte „Bemessung"; mit
  #477 über 16 Prüfgruppen für alle drei Szenarien — Excel 16 und die ClosedXML-Nachrechnung abweichend 0, die Zahl
  der Formeln je Mappe drei- bis vierfach, z. B. hyb1042 429 → 1.541).

**Dauerhaft Werte bleiben**, weil sie am Stundenlauf, an Katalog- und Datenbankzugriff oder an
Text hängen:

- Blatt *Übersicht* (Stammdaten, Variantenliste, Gewerke-Matrix);
- die **Kennzahlspalten des Vergleichsblatts** — auch die Gruppe *Kosten*: Energiekosten p. a. und
  Stromkosten Netzbezug enthalten Grundpreise je Träger und den Leistungspreisanteil aus der
  Viertelstunden-Bezugsspitze;
- alle **Detailblätter** samt Monatswerten (Aggregat der Stundenreihen; die Brennstoffmenge wird
  dort heute als Text geschrieben);
- die **Strommengen-Matrix** als Jahreszeile ohne Tarifzonen (Jahressummen aus den Stundenreihen,
  stundenweises Minimum für den KWK-Eigenstrom, höchste Stundenlast), die Bezugsspitze, die **Emissionsbilanz**, der **KWK-Modulblock**;
- die **Sensitivitätstafel** (fünf Zeilen, jede eine Differenz zweier Zahlungsbilder);
- die Textbausteine (Parameter- und Tarifnachweis, Bilanzkonvention, Veraltet-Warnungen,
  Empfehlungssatz, nicht monetäre Wirkungen — mit #479 die Tafel der Wirkungen, die Beurteilung als Zahl, keine
  Formel) und die Gesetzeslogik mit Katalogzugriff
  (KWKG-Kontingente und ihr Auslaufen, Befreiungs- und Entlastungstatbestände, BEHG-Pflichtigkeit,
  Preisstände).

Die Kennzahlen der Szenarien Günstig und Ungünstig gehören nicht dazu: Sie rechnen mit #477 in Formeln wie die des
Szenarios Erwartet (E14‑Q2 a).

**Drei Sätze, die der Formelbericht trägt:** Er rechnet die Bewertung bereits feststehender
Jahresmengen nach — jede Änderung an Anlagengröße, Bedarf oder Fahrweise verlangt einen neuen
Stundenlauf, den keine Zellformel liefert. Die Gesetzeslogik und die Nachweise, die den Zahlen ihre
Gültigkeit geben, sind nicht abbildbar. Und was der Anwender in der Mappe umstellt, kommt nie ins
Projekt zurück: Die Formelmappe ist ein nachvollziehbarer Nachweis, keine zweite Eingabeoberfläche.

**EPOS trägt die Werte ein, Excel rechnet neu** — in allen drei Szenarien. Die eingesetzte Fassung ClosedXML 0.105.1 legt eine Formel ohne
ihr Ergebnis ab — auch nach `RecalculateAllFormulas` —, und ihre Rechenmaschine kennt `NPV` und `IRR` nicht (`PMT`
rechnet sie; eine Annuität, deren `PMT` auf den Nettobarwert zeigt, erbt dessen Fehler). Deshalb schreibt ClosedXML
nur die Formeln; nach dem Speichern trägt EPOS zu jeder Formelzelle die Zahl des Rechenkerns als Ergebnis ein, in
voller Stellenzahl, und die Mappe verlangt die volle Neuberechnung beim Öffnen (`fullCalcOnLoad`). Excel (gemessen:
Microsoft 365, Version 16) rechnet damit jede Formelzelle auf den eingetragenen Wert; ein Betrachter ohne eigene
Rechenmaschine zeigt die eingetragenen Zahlen statt leerer Zellen. Andere Tabellenkalkulationen sind nicht gemessen;
Formel und Wert stehen in jeder Zelle der Datei. Zwei Wachen halten das fest: `FormelmappeClosedXmlBefundTests` wird
rot, sobald eine neue ClosedXML-Fassung eines der beiden Verhalten ändert — dann ist die Regel neu zu prüfen —, und
`BerichtBlattstrukturWacheTests` hält die Blattstruktur des Excel- und des Wortberichts samt jeder Stufe fest.

### 2.11.7 Hinweistext bis zur vollständigen Szenarioabdeckung

**Entfallen mit E9b (#462).** Der Hinweistext ist aus der Seite, aus Block 4, aus Wort- und Tabellenbericht
und aus den Ressourcen entfernt; an seiner Stelle steht der Ausweis „n von m Parametern szenariert" (§ 2.11.5,
Ausweis). Der Hinweis entfällt mit der Etappe, die ihn überflüssig macht — das ist E9b. Was folgt, ist Rückschau.

Die vollständigen Parametersätze je Szenario (§ 2.11.5) kamen **nach** der Darstellungsetappe. Bis
dahin sagte ein Hinweis unter der Annahmentafel der Wirtschaftlichkeitsseite, was ein Szenario
variierte und was nicht (V-4, → Register R‑V; die Messung dazu: → Protokoll § 3.11).

**Wortlaut** (beide Sprachen als Ressource — **umgesetzt #434 als `WIRT_SZEN_HINWEIS`**, ohne den letzten Satz, siehe A14 unten; **entfallen #462**):

> **Was ein Szenario heute variiert — und was nicht.** Ungünstig und Günstig verändern gegenüber
> Erwartet den Kalkulationszins, die drei Preissteigerungsraten (Energie, Betrieb, Investition und
> Ersatz), die Investition der Positionen ohne eigenen Szenariowert (±10 %), die Erträge aus
> Einspeisung und Photovoltaik-Vergütung (±10 %) und die Nutzungsdauer der Positionen ohne eigenen
> Szenariowert (±2 a). In allen drei Szenarien gleich bleiben der Betrachtungszeitraum, die
> Energiepreise je Träger, die Erlössätze (Einspeisevergütung, Direktvermarktung), die Mengen aus
> der Simulation und alle gesetzlichen Sätze. Die vollständigen Parametersätze je Szenario — auch
> Zeitraum, Trägerpreise, Erlössätze und ein Mengenfaktor — kommen nach dieser Darstellung; bis
> dahin steht dieser Hinweis unter der Tafel.

Die Vorgabewerte im Text (±10 %, ±2 a) waren die Vorgaben aus § 2.1 des Szenarienkonzepts; trug
das Projekt gepflegte Sätze, nannte der Hinweis die gepflegten Werte. Der Hinweis entfällt mit der
Etappe, die ihn überflüssig macht — das ist E9b (#462).

*Entscheid A14 (→ Register R‑A):* Die Ressource
`WIRT_SZEN_HINWEIS` trug die Konzeptfassung **ohne den Roadmap-Satz** (den letzten Satz des Wortlauts
oben); das Mockup führte einen abweichenden Schlusssatz. **Umgesetzt #434:** Die Zahlen im Text waren die
wirksamen — ohne Pflege die Vorgaben (±10 %, ±10 %, ±2 a), mit gepflegtem Satz die gepflegten Werte,
ungünstig vor günstig (`WirtschaftlichkeitEmpfehlung.Szenariohinweis`); der Hinweis stand unter der
Annahmentafel, in Block 4 der Darstellung „ValERI-Bewertung" und in Wort- und Tabellenbericht.

**Zwischen E9a und E9b (#461):** Mit E9 Teil a rechnete der Kern Betrachtungszeitraum, Mengenänderung,
Trägerpreise und Erlössätze je Szenario (§ 2.11.5). Der Hinweistext blieb stehen, bis die Pflege in den
Dialogen kam; er stimmte nur noch für Projekte ohne Pflege dieser Größen — eine Pflege zeigten die
Annahmentafel, die Nachweiszeile je Szenario und die Kohärenzzeilen. **Mit E9b (#462) ist er entfallen;** an
seiner Stelle steht der Ausweis „n von m Parametern szenariert" (§ 2.11.5, Ausweis).

## 2.12 Kategorien-Mockups mit Rechenweg

*Ausgelagert in den Ordner [`Wirtschaftlichkeit_Kosten/`](LIESMICH.md):
`Beispielprojekt.md` (die eine Zahlenquelle), `../Mockups/Dialog_Formel_Zahlenprobe.html` — **das
eine konsolidierte Mockup**: alle acht Kategorien mit Dialog, Berechnungsgrundlage,
Berechnungserläuterung, Beschriftungen und Abnahmezeile, die Ergebnisseite in Kategorie 8 und zwei
Anhänge (Umsetzungsstand, Herkunft der Zahlen); zugleich Artifact
[Dialog, Formel, Zahlenprobe, Ergebnis](https://claude.ai/artifact/1WeFMXrpxrCSu1jUtvTRCw)
— **die Repo-Datei führt** (Q22, → Register R‑Q).
Dazu `Rechenweg/01…08` — je Kategorie Dialog → Berechnungsgrundlage → Berechnungserläuterung →
Befunde. Der Auftrag: je Kostenkategorie ein Mockup mit Berechnungsgrundlage und
Berechnungserläuterung, Schwerpunkt Vergütungen BHKW und PV. Dieser Abschnitt ist die Kurzfassung;
bei Abweichung gilt der Ordner für die Zahlen und dieses Dokument für die Regeln.*

*Das Mockup ist vom Anwender abgenommen; es ist die verbindliche Zielvorgabe der Etappen E4 ff.
(→ Register R‑EZ, EZ‑9).*

**Die Dialogform der Komponentenkosten ist abgenommen** (→ Register R‑EZ, EZ‑4): Kopfband,
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
(Kaskadenprobe 1042, Mischsatz 300 kW, AW 300 kWp, Beispielprojekt B) sind als solche gekennzeichnet.

| # | Kategorie | Rechenweg | Kernaussage der Zahlenprobe |
|---|---|---|---|
| 1 | Investitionskosten BHKW | `01` | Kaskadenfaktor auf die Hauptposition 1,155; I₀ = 240.772,40 − 6.000 = **234.772,40 €** (300 kW) |
| 2 | Betriebskosten BHKW | `02` | Hilfsenergie 2 % × 312.631 € Endenergiekosten = 6.252,62 €/a (21.710 kWh Strom); Instandhaltung 1,50 % × 240.772,40 = 3.611,59 €/a; Summe 57.164,21 €/a; die Kesselmenge kommt aus der Modulzeile |
| 3 | Kosten der Photovoltaik | `03` | 192.150 € = 640,50 €/kWp; Wechselrichter-Ersatz Jahr 12 (24.000 €, Barwert 16.833), Restwert 39.800 € (Barwert 22.036); Degradation 0,5 %/a → Jahr 20: 259,1 MWh |
| 4 | Energiekosten | `04` | **umgesetzt** (ET-D): Preisbestandteile 0,0638 + 0,1371 + 0,1180 + 0,4371 = 0,7560 €/m³ — im Dialog in der Abrechnungseinheit; BEHG 872,3 t × 65 € = 56.700 €/a; N3 +32 % |
| 5 | **Vergütungen BHKW** | `05` | **Mengentafel** brutto 1.650 → netto 1.563,2 (§ 9 Nr. 3 bleibt brutto); Mischsatz 5,5667 / 2,4167 ct; 2026 vergütet 60 % = 32.022 €; **Reihe endet nach zwölf Jahren** (291.111 €); § 53a 21.203 €/a |
| 6 | **Vergütungen PV** | `06` | AW 6,04 ct; 159,6 von 199,5 MWh vergütet (§ 51: 20 % abgeregelt); Spot 7.182,00 + Prämie 2.457,84 − DV 638,40 = 9.001,44 €/a; § 51a 1.095,52 € im Jahr 20; Reihe nominal 150.118 €, Barwert 113.800 € |
| 7 | Erlösrubrik | `07` | Block A 91.727,0 €/a; vermieden brutto 339.753,6 − entgangene § 9b 23.594,0 = effektiv 316.159,6 €/a |
| 8 | Wirtschaftlichkeit über Nutzungsdauer | `08` | Musterprojekt: Kapitalwertdifferenz V1 +1.660.205 · V2 +182.491 · V3 +1.842.695 € (Bandbreite V3 1.636.035 … 2.048.635 €); Beispielprojekt B: Näherung 65.073 €, jahresscharf 65.259 €; IZF/Amortisation nachrichtlich |

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

## 2.13 Ergebnisansicht

*Die Ergebnisansicht steht in Kategorie 8 des einen Mockups
`../Mockups/Dialog_Formel_Zahlenprobe.html`: Kopf mit Umschalter, dann die
vier Fragen „Lohnt es sich · Wie sicher ist das · Woraus entsteht die Zahl · Was ist angenommen",
die Kapitalwertformel, die Gegenprobe und die Bericht-Ausgaben. Woraus die einzelnen Beträge
entstehen, sagen die Kategorien 1 bis 7; was noch nicht gebaut ist, steht im Anhang
„Umsetzungsstand". Die Punkte (1) bis (5) stammen aus einer Durchsicht des Anwenders, (6) ist ein
Verweis; Anlass und Messung am Bestand: → Protokoll § 3.7.*

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

*Erledigt mit #357 sind die Nachpflege des Bestands und der Pflegeort der Positionsart: → Protokoll
§ 3.10.*

**Ersatz und Restwert je Position — entkoppelt** (A6, → Register R‑A; Schritt E, Schemaschritt 111;
umgesetzt #446). Jede Investitionsposition des Projekts und jede Vorlagenposition trägt zwei nullbare
Kennzeichen, `ErsatzFuehren` und `RestwertAnsetzen` (`Tab_ProjektWerte`, `Tab_KostenVorlagePosition`,
`CHECK` 0/1). Leer heißt wie bisher — ersetzt wird, sobald die Nutzungsdauer vor dem Ende des
Betrachtungszeitraums abläuft, und der Restwert steht linear im letzten Jahr; „ja" rechnet genauso;
„nein" schaltet das eine ohne das andere ab (Rechnung § 3.1). Gepflegt werden beide im Zeileneditor
„Position bearbeiten" der Investitionsseite als Klapplisten „Ersatzbeschaffung führen:" und „Restwert
ansetzen:" (leer — wie bisher · ja · nein) mit einer Herleitungszeile; die Tafel „Ersatz und Restwert"
nennt eine Abwahl als Grund („— nein (Kennzeichen der Position)"). Vorlagenübernahme, Projektkopie und
„Speichern unter" tragen die Kennzeichen mit; der Hilfe-Assistent kennt beide Felder. Führt eine Datenbank
die Spalten nicht, stehen die Klapplisten nicht im Editor.

**Betriebspositionen „alle n Jahre"** (V‑G3, → Register R‑V, R‑E16; Schemaschritt 129; umgesetzt #484). Jede
Betriebsposition des Projekts und jede Vorlagenposition trägt die nullbare Wiederholperiode `Wiederholperiode_a`
(`Tab_ProjektWerte`, `Tab_KostenVorlagePosition`); leer, 0 und 1 heißen jährlich. Mit n ≥ 2 zahlt die Position in den
Jahren s, s + n, s + 2n … bis zum Ende des Betrachtungszeitraums (Rechnung § 3.1); **das Startjahr ist der Beginn der
Folge**, ohne Startjahr das Jahr 1. Nutzungsdauer, Ersatz und Restwert bleiben unberührt — sie gehören der
Investitionsseite, und eine Investition „alle n Jahre" ist die Ersatzkette (E16‑Q2 a). Gepflegt wird die Periode im
Zeileneditor „Position bearbeiten" der Betriebsseite als Ganzzahlfeld „Zahlung alle: [n] Jahre" (1 … 99, Vorgabe 1) mit
der Zeile „1 = jährlich. Ab 2 zahlt die Position im Startjahr und danach alle n Jahre (DIN EN 17463, 6.3.1)." und bei
n ≥ 2 der Herleitung „alle n Jahre ab Jahr X"; die Kostenvorlagen führen dasselbe Feld, „Aus Vorlage übernehmen…" (aus
einer Vorlage wie aus einer anderen Anlage) trägt die Periode mit, der Hilfe-Assistent kennt das Feld
`wiederholperiode`. Auf der Investitionsseite und in einer Datenbank ohne die Spalte steht das Feld nicht.

**Die zwei Stücke aus U39 — gebaut mit E10 (#463):**

1. die **geräteeigenen Nutzungsdauer-Spalten** (`Tab_BHKW`, `Tab_Heizkessel`) sind als **Gerätedaten
   gekennzeichnet** (A8, → Register R‑A): Katalog und Aufklapper „Alle Daten anzeigen" beschriften sie „Nutzungsdauer
   (Gerätedaten):", ein Tooltip sagt „nicht rechenwirksam; maßgeblich ist die Nutzungsdauertabelle", der
   Hilfe-Assistent und die Parameterverwendung tragen denselben Vermerk; entfernt ist keine Spalte (E10‑Q4, → Register
   R‑E10). `Tab_StromspeicherVariante` führt ihre Nutzungsdauer weiter selbst, denn sie rechnet in
   Speicherwirtschaftlichkeit und Peak-Shaving; der Halbsatz aus A8, sie solle die Positionsarten 20/21 lesen, ist
   **erledigt mit #474** als Vorgabe neuer Einträge: Eine neu angelegte Speichervariante (neue Speicheranlage im
   Anlagendialog, „Speichervariante anlegen" des Hilfe-Assistenten) nimmt die Nutzungsdauer der Standardzeile
   „Stromspeicher · Batterie", ohne brauchbaren Wert 20 a; bestehende Varianten und der Rückfall des Rechenwegs bleiben;
2. die **Speicherflotte** hängt an der Tabelle (A7): Die Wirtschaftlichkeit der Flottenstudie rechnet den **Restwert
   je Einheit linear** aus ihrer Nutzungsdauer — dem Ersatzintervall — auf der Ersatzkette der Flotte
   (`FlottenWirtschaftlichkeit.LinearerRestwert`: Betrag der letzten Beschaffung × Restdauer ÷ n; ersetzt wird in
   jedem Jahr, dessen Nummer durch n teilbar ist, auch im letzten — dann steht der volle Betrag als Restwert
   daneben); eine Einheit ohne eigenes Intervall nimmt die Nutzungsdauer der Standardzeile „Stromspeicher · Batterie"
   (heute 10 a); der feste Restwert je Einheit (`RestwertEuro` im JSON des Flottenstands in `Tab_SpeicherAuslegung`)
   ist ein **Altfeld** — gespeichert, im Flotten-Editor gekennzeichnet, nicht mehr gelesen; der zusätzliche Restwert
   der Studie bleibt (E10‑Q3, → Register R‑E10). Der Projektlauf rechnet keine Flottenwirtschaftlichkeit; der
   Referenzlauf blieb byte-gleich, und die dritte Einfrierregel in `Referenzlaeufe/LIESMICH.md` nennt den Anschluss.

Dazu: Die Hinweiszeile hatte nur `WirtschaftlichkeitSeiteGaben` als **einzigen** Schreiber,
und die Textbildung lag bereits im Kern (`NutzungsdauerAbgleich.Hinweis`); zu tun war das Einsammeln
der Positionen, nicht der Text. **Stand: Die Hülle selbst liegt seit E3 plattformfrei (umgesetzt
#431)** unter `EPOS.UI.Daten/Wirtschaftlichkeit/` — den Ordner `Wirtschaftlichkeit`, den es dort noch
nicht gab, gibt es damit jetzt. **Das Einsammeln der Positionen im Kern ist umgesetzt #434**
(`NutzungsdauerHinweisCtrl`: Zeitraumzeile und je Stand und Technik „k von n", der Satz aus
`ErsatzRestwertTafel.Hinweis`); die Hinweiszeile steht auf der Seite und in Wort- und Tabellenbericht,
die Hülle sammelt nicht mehr selbst. Die Entkopplung von Ersatz und Restwert ist umgesetzt #446, die zwei Stücke
oben #463 — U39 ist damit erledigt; der Halbsatz aus A8 zur Speichervariante ist mit #474 gebaut.

*Entscheid A1 (Umzug vor der Ergebnisansicht) und sein Weg: → Register R‑A (A1), → Protokoll § 5.5.*

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
**Verteilschlüssel je Anlage**: die Strommatrix führt je Projekt Jahressummen (eine Jahreszeile, keine
Tarifzonen), nicht Mengen je Anlage, und der Kern verteilt nach dem Eigenverbrauch je Anlage (Befund V-4); der
Leistungsanteil bleibt projektweit. **Stand: umgesetzt #432** — Anlagenbezug `WirtZeile.Komponente`,
Verteilschlüssel `VermiedenAnlageNachweis.Verteile()` (Näherung ausgewiesen), Leistungsanteil projektweit
(Q15); **mit #437** ist die Bezugsgröße der vermiedenen Menge der Bedarf ohne jede Eigenerzeugung
(Entscheid U6‑Q1, § 6.3 Nr. 32), und der Schlüssel nimmt Blockheizkraftwerk und Photovoltaik brutto aus
der Strommatrix (§ 3.6).

**(5) Der Verlauf mit allen drei Szenarien — umgesetzt #436 (E6).** Der Verlauf steht als eigener
Abschnitt in „Wie sicher ist das?" zwischen Bandbreite und Sensitivität — die Bandbreite zeigt die
Spanne am Ende, der Verlauf, wie sie entsteht; einen Knopf „Verlauf…" gibt es nicht (K8). Drei
Kernaussagen:

- **Dreierreihe.** `WirtschaftlichkeitCtrl.BerechneVerlaufSzenarien` rechnet je Version die drei
  Szenarien als drei vollständige Läufe über `BerechneVerlauf`, mit derselben Referenz und ohne zu
  speichern; `WirtschaftlichkeitVerlaufSzenarien` trägt je Stand und Szenario die kumulierte
  Differenzlinie, die Restwert-Differenz und den Nulldurchgang (`KapitalwertRechner.Nulldurchgang`,
  dieselbe Regel wie `AmortisationDifferenz`). Keine Rechenwirkung.
- **Farbe = Variante, Strichart = Szenario.** `ChartRenderer.VerlaufsReihenSzenarien` gibt jedem
  Stand die Farbe seines Platzes in der Gruppe — die Palette hat acht Farben, mehr als acht gewählte
  Stände werden benannt abgelehnt — und jedem Szenario eine Strichart der Aufzählung
  `ChartRenderer.Strichart`: Erwartet durchgezogen und kräftiger, Ungünstig gestrichelt, Günstig
  gepunktet. Ein Flächenband gibt es nicht, es wäre bei mehreren Varianten unlesbar; der
  Nulldurchgang jeder Linie ist eine Marke auf der Nulllinie.
- **Legende zweigeteilt.** Erst die Stände mit ihrer Farbe, dann die drei Szenarien mit ihrer
  Strichart und die Marke — Stände plus drei Einträge statt Stände mal drei. Das Bild misst
  1240 × 620 für zwei Legendenzeilen und wächst je weitere Zeile um 30 px; die Zeichenfläche bleibt
  1090 × 400.

Bedienung und Ausgabe: Zeitraum, „Aktualisieren", „Verlauf nach Excel…" (Blatt „Verlauf" über
`Dienste.Datei`), je Stand und Szenario ein Haken, der nur neu zeichnet; die Legende schaltet nicht.
Die Hülle des Abschnitts ist `KapitalwertVerlaufHuelle`, plattformfrei in `EPOS.UI.Daten`. Der
Tabellenbericht führt je Szenario eine Spaltengruppe und das Blatt „Verlauf", im Wortbericht steht
das Dreierbild. **Das zweite Bild** (Versionen absolut) bleibt nicht auf der Seite: Alle Versionen
liegen tief im Negativen und nahezu parallel, entschieden wird über den Abstand; der absolute
Vergleich steht in der Kennzahltafel (Nettobarwert absolut) und in der Mehrjahresübersicht des
Berichts. **Als Bild steht er an genau einem Ort: im Wortbericht**, unter dem Titel „Kumulierte
Barwerte je Version", im Erwartungsfall, mit **Legende je Version** (Name und Farbe) und
gestrichelter Stammlinie — sie ist die Bezugsgröße und keine Version und muss auch im
Schwarz-Weiß-Ausdruck davon zu trennen sein. Nachweis: `Proben/ChartProben` (Bilder
`kapitalwert_szenarien…` und `kapitalwert_absolut_legende`, Gegenproben `strichart_gepunktet_wirkt`,
`kapitalwert_szenarien_legende_zweigeteilt_wirkt`, `kapitalwert_szenarien_nulldurchgang_wirkt`,
`kapitalwert_verlauf_gestrichelt_wirkt` und `kapitalwert_verlauf_legende_nennt_die_version`),
`VerlaufSzenarienTests`, `KapitalwertVerlaufAbschnittTests`. **Block 4 der Darstellung „ValERI-Bewertung"**
zeigt Spannenbild und Verlauf mit denselben Bausteinen — dem Fragment des Spannenbilds und
`KapitalwertVerlaufAbschnitt` mit derselben Datenseite und Fassung — in der Folge Bandbreite, Spannenbild,
Vorschlag, Ausweis der Szenarioabdeckung (an der Stelle des Hinweistexts, #462), Verlauf, Sensitivität; je
Darstellung steht genau ein Verlauf (E6‑Q1, → Register R‑E6; umgesetzt #454).

*Die Messung vor der Umsetzung und die Liste dessen, was die Umsetzung brauchte: → Protokoll § 8.1.*

**(6) Vergleichssicht — alle Varianten gegen die Referenz oder zwei Stände.** Die Anforderung vom
18.09.2026 zur Tafel „Gliederung des Kapitalwerts" steht als eigener Abschnitt in § 2.15, weil sie
in die Differenzrechnung greift und die Gliederung von § 2.9 braucht (Ist, Soll-Tafel, Randfälle,
Abnahme, Etappe); das Mockup zeigt beide Sichten in Kategorie 8.

*Weitere Festlegungen des Mockups zur Ergebnisansicht und die Entscheide, die ihren Zuschnitt
änderten: → Protokoll § 3.7.*

---

## 2.14 Erfassungsgruppen auf der Kostenseite

*Entscheid K-WZ-1: → Register R‑BK.*

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

## 2.15 Vergleichssicht der Ergebnisansicht — alle Varianten gegen die Referenz oder zwei Stände

> **Stand: umgesetzt.** Sicht, A und B liegen als Sitzungswahl in `Vergleichsauswahl.Sicht`
> (`Vergleichssicht`); Sicht 2 übergibt A als Referenz des Rechenlaufs, ohne
> `ID_Referenzprojekt` zu schreiben, und rechnet **ohne zu persistieren** — der gespeicherte Lauf
> bleibt der gegen die Unterlassensalternative der Gruppe. Die Ergebnisansicht trägt die
> Optionsgruppe mit den Klapplisten A und B, dem Tauschknopf und der Erklärzeile; der Verlauf
> zeichnet in Sicht 2 die eine Kurve B − A; Word und Excel folgen der Sicht und tragen die
> Deklarationszeile (`Referenzwahl.Deklarationszeile`). Nachweis
> `EPOS.Kern.Tests/VergleichssichtTests`, `EPOS.Kern.Tests/ReferenzprojektTests` und
> `EPOS.UI.Tests/Seiten/WirtschaftlichkeitSichtTests`.

*Anforderung im Wortlaut, Abgrenzung zu § 2.13, Ist-Zustand vor der Umsetzung, Abnahme,
Umsetzungsliste und Einordnung der Etappe: → Protokoll § 4.1.*

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

*Fragen VG-Q1 bis VG-Q7: → Register R‑VG.*

Mockup: Kategorie 8 in `../Mockups/Dialog_Formel_Zahlenprobe.html#sicht2`, Umsetzungsstand U37.

---

## 2.16 Vergütung je Variante — eigene Werte oder vom Stammprojekt übernommen

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

*Anforderung im Wortlaut, Abgrenzung, Ist vor der Umsetzung (Stand 18.09.2026), Abnahmekriterium,
Umsetzungsliste — darin als noch ausstehend vermerkt ein bestätigender `ios.yml`-Lauf — und
Einordnung der Etappe: → Protokoll § 4.2.*

**Soll:**

| Aspekt | Festlegung |
|---|---|
| Auswahl | je **Variante** eine Wahl: **„vom Stammprojekt übernehmen"** (Vorgabe) oder **„eigene Vergütung"**; der Stamm führt immer eigene Werte |
| Vorgabe | **übernehmen** — ergebnisneutral für jede neue Variante; für den Bestand leitet ein Schemaschritt die Wahl aus den Daten ab (Randfälle) |
| Persistenz | eine Spalte `Tab_ProjektPhotovoltaik.Uebernahme_Stamm` (INTEGER, `CHECK (Uebernahme_Stamm IN (0,1))`, nullbar): 1 = übernommen, 0 = eigene Werte; **keine Zeile** = übernommen (Vorgabe jeder neuen Variante). Die Zeile der Variante bleibt bei „übernehmen" stehen — sie ist der Rückweg zu den eigenen Werten. Kein DDL-DEFAULT (Hausregel der Tabelle); ihr einziger DDL-Ort ist der Schemaschritt (SchemaMigration) |
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

*Fragen VV-Q1 bis VV-Q7: → Register R‑VV.*

Mockup: Kategorie 3 (Reiter Ertrag/Bonus) unter `../Mockups/Dialog_Formel_Zahlenprobe.html#pvkosten` und
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
        + Σ_w Betrag_w × (1 + p_w)^(t−1) · [t zahlt]   ← Positionen „alle n Jahre" (V‑G3, #484), p_w = p_B bzw. p_E
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
          nur wenn ErsatzFuehren ≠ nein   (leer/ja = wie hier; nein ⇒ keine Kette, letzte Beschaffung = start)
Restwert: Alter = T − letzte Beschaffung ;  Restdauer = n − Alter
          RW_T [€] = Betrag × Restdauer / n     (nur bei Restdauer > 0, linear)
          nur wenn RestwertAnsetzen ≠ nein  (leer/ja = wie hier; nein ⇒ RW_T = 0)

Wiederholperiode einer Betriebsposition (Schemaschritt 129, § 2.13 (3)):
n = Wiederholperiode_a ; leer, 0, 1 ⇒ jährlich (in Betrieb_t bzw. Endenergie_1)
n ≥ 2 ⇒ Zahlung in t = s, s+n, s+2n, … ≤ T ; s = StartJahr, falls > 1 ; sonst 1
        (KapitalwertRechner.ZahltImJahr) ; Betrag = Preisstand Jahr 1, fortgeschrieben mit p_B bzw. p_E
        Betriebskosten p. a. = Zahl des ersten Jahres: die Position zählt nur mit s ≤ 1
```

Die zwei Kennzeichen je Position (Schemaschritt 111, § 2.13 (3); umgesetzt #446) sind entkoppelt: Eine nicht
ersetzte Position trägt ihren Restwert aus der ersten Beschaffung weiter, solange er nicht abgewählt ist.

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
| `EUR_PRO_KW_ELEKTRISCH` an der Wärmepumpe, **nur Kategorie 1**¹ | Σ (Ptherm ÷ COP am Normpunkt) × Satz | `Tab_Kenndaten` bei W35 (A2/B0/W10 je `Tab_WP.Typ`, interpoliert) |
| `EUR_PRO_KWH_ELEKTRISCH` an der Wärmepumpe, Kategorie 2² | gesperrt (GEWERK), Bestandszeile rechnet aus dem Lauf, Herleitung „Altbestand“ | Strommenge der Wärmepumpe aus dem jüngsten Lauf |
| `EUR_PRO_KWH_THERMISCH` an der Wärmepumpe, Kategorie 2² | gesperrt (GEWERK), Bestandszeile rechnet aus dem Lauf, Herleitung „Altbestand“ | Wärmemenge der Wärmepumpe aus dem jüngsten Lauf |
| `EUR_PRO_KWP` | Σ (Modulanzahl × Modulleistung)/1000 × Satz | `PhotovoltaikCtrl.KwpSumme` |
| `EUR_PRO_KWH_KAPAZITAET` | Σ Energie × Satz | `Tab_Stromspeicher.Energie` |
| `EUR_PRO_M2_KOLLEKTOR` | Σ (Aperturfläche × Modulanzahl) × Satz | Solarthermie |
| `EUR_PRO_KW_LEISTUNG` am Pufferspeicher | Σ Gesamtvolumen × Satz [€/Ltr.] | `Tab_Pufferspeicher.Gesamtvolumen` |

Art ↔ Gewerk wird gekreuzt geprüft: falsches Paar ⇒ **null, keine Fantasiezahl**. Der
Pufferspeicher bemisst sich allein an seinem **Volumen**; eine kWh-Kapazität führt er nicht
(ohne Temperaturpaar keine belastbare kWh), und `EUR_PRO_KWH_KAPAZITAET` liefert dort null.
Geprüft wird **je Raster** (Schalter `investition` der Landkarte `TechnikPlanwertCtrl.Geraetespalte`): Allein
`EUR_PRO_KW_ELEKTRISCH` an der Wärmepumpe antwortet im Investitions- und im Betriebsraster verschieden — in der
Kaskade (Kategorie 1) rechnet die Zeile, im Betriebsraster (Kategorie 2) bleibt sie GEWERK und steht nicht in der
Auswahl. Die beiden kWh-Arten `EUR_PRO_KWH_ELEKTRISCH` und `EUR_PRO_KWH_THERMISCH` antworten an der Wärmepumpe in
beiden Rastern GEWERK (Fußnote ²).

¹ **Normpunktregel** (gebaut #502, E20; § 6.3 Nr. 10; → Register R‑E20). `Tab_WP` führt keine elektrische Leistung
(`Nennleistung` ist thermisch). P_el = Ptherm ÷ COP am Normpunkt der Kennlinie `Tab_Kenndaten` bei Vorlauf 35 —
Luft/Wasser A2, Sole/Wasser B0, Wasser/Wasser W10 nach `Tab_WP.Typ`; fehlt die Stützstelle, wird je Größe linear
interpoliert (Herleitung mit „≈“), nie extrapoliert; COP ≤ 0, Ptherm ≤ 0, unbekannte Bauart oder keine umschließende
Stützstelle ⇒ null mit Grund GERAET; Heizstab und Kühlkennlinie zählen nicht; mehrere Wärmepumpen einer Anlage
summieren. Herleitung am Betrag: „11,60 kW ÷ COP 2,90 (A2/W35) = 4,00 kW“ (`KDLG_HERLEITUNG_WP_PEL`, Summenform
`KDLG_HERLEITUNG_WP_PEL_SUMME`).

² **Betrieb der Wärmepumpe ohne kWh** (gebaut #510, E23; Anwenderentscheid E20‑Q6 b vom 25.09.2026, erweitert: „bei
Wärmepumpe fixer Jahresbetrag (oder % von Investitionskosten), nicht nach kWh/a — weder Strom noch Wärme“; § 6.3
Nr. 10; → Register R‑E20, R‑E23). Die Landkarte `WirtschaftlichkeitCtrl.BasisGrund` führt die Wärmepumpe nicht in den
Listen der beiden kWh-Arten; `BemessungKatalog.Auswahl`, die KI-Wahlliste und die Kreuztafel folgen. Eine Bestandszeile
bleibt über `benutzt` wählbar und rechnet aus dem Lauf weiter (`RueckfallMenge`, `FrischeBasis` unverändert); die
Herleitung hängt „Altbestand — Betriebskosten der Wärmepumpe werden nicht je kWh bemessen“ an
(`KDLG_HERL_ALTBESTAND_WP_KWH`, nur im Projektmodus; ohne Bezugsgröße steht der Vermerk allein, der Grund lautet „passt
nicht zu diesem Gewerk“), ebenso beim Altwert `EUR_PRO_KWH`. Wählbar bleiben an der Wärmepumpe im Betrieb fester
Jahresbetrag, % der Investition, % der Endenergiekosten, % des Endenergiebedarfs, je kW Leistung und je kW
Heizleistung (E23‑Q6 und E23‑Q7 entschieden 25.09.2026, nach Empfehlung a).

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
der **erfasste Betrag** (Entscheid I-2, → Register R‑EZ, EZ‑1). Eine ermittelte Menge 0 rechnet weiter
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
5. **Projektweite Arten** `PROZENT_BRENNSTOFFKOSTEN` und `PROZENT_STROMKOSTEN` (B‑4, umgesetzt #446): Menge
   frisch aus dem jüngsten Lauf — die Brennstoffkosten Σ Verbrauch × Arbeitspreis aller Brennstoffmodule (BHKW
   und Brennstoffkessel; der Elektrokessel bleibt in der Stromwelt), derselbe Weg wie Weg A, bzw. die
   Stromkosten Netzbezug × Arbeitspreis des Projekt-Stromträgers. Bezugsgröße sind die **Arbeitskosten**, ohne
   Grund- und Leistungspreis (E7c2‑Q2, → Register R‑E7c2). Die Konserve gilt nur, wo frisch nichts ermittelbar
   ist; der Grund nennt dann Lauf, Menge oder Preis (`EndenergieAufloeser.GrundOhneProjektkosten`)

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

**Der Strompreis einer Anlage ist der ihres eigenen Trägers** (→ Register R‑EZ, EZ‑6).
Trägt eine Anlage, die selbst Strom bezieht — Wärmepumpe, Heizstab,
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

**Hilfsenergiekosten aus dem Anlagenanteil** (E30, #548; → Register R‑E30). Ist an einer Anlage ein Hilfsenergieanteil
angegeben (`Tab_Energieanlagen.Hilfsenergie_Anteil`, % des Brennstoffs, § 3.6), werden die Hilfsenergiekosten daraus
ermittelt: Trägt eine Anlage mit Brennstoff — BHKW oder Brennstoffkessel — einen Anteil > 0 und eine Brennstoffmenge im
jüngsten Lauf, rechnet ihre Pflichtzeile „Hilfsenergiekosten“ **ohne eigenen Satz und Betrag** den Betrag Hilfsstrom ×
Arbeitspreis des Projekt-Stromträgers — rechnerisch Weg B (% des Endenergiebedarfs) mit dem Anteil als Satz, gleich
welche Bemessung die Zeile gespeichert hat, und dieselbe Menge, die die KWKG-Nettomenge mindert
(`HilfsstromRechner.MengeMWh`); gerechnet im Endenergie-Topf p_E über den Auflöser, mit Mengenfaktor und Strompreis des
Szenarios, ohne Grund- und Leistungspreis (E30‑Q1 a, Q4 a). Fehlt die Pflichtzeile, entsteht die abgeleitete Zeile
„Hilfsenergiekosten (aus dem Anlagenanteil)“. Die Herleitung nennt „Satz aus dem Hilfsenergieanteil der Anlage“; Töpfe
und Nachweisliste lesen denselben Plan (`HilfsenergieAusAnteil`), ihre Summe ist die Probe. **Vorrang (E30‑Q2 a):** Trägt
die Position selbst einen Satz oder Betrag, gilt die Position; der Anteil wirkt dann nur auf die KWKG-Nettomenge, und die
Kohärenzprüfung sagt es (`KOH_HILFSENERGIE_DOPPELT`: die Kosten rechnet die Kostenposition, der Anteil wird nicht
zusätzlich bepreist). **Ausnahmen (E30‑Q3 a):** der Elektrokessel (Regel E1 — sein Strom steht im Netzbezug), die
Wärmepumpe und der Kälte-Hilfsstrom (`Tab_WP.Kuehl_Hilfsstromanteil` steckt in der Simulation und damit im Netzbezug).
**Keine Doppelzählung:** Der Hilfsstrom von BHKW und Brennstoffkessel steht nicht in `STROMBEDARF_GESAMT` (die
Strommatrix bleibt brutto, § 3.6), die Kosten treten also neben die Energiekosten, nicht in sie; KWKG-Netting und Kosten
sind zwei Größen — nur im Einspeisefall überschätzt der Bezugspreis den entgangenen Einspeiseerlös (benannte Näherung).
Ohne Anteil ist der Plan leer, und alles rechnet bitgleich; kein Referenzprojekt trägt einen Anteil (E30‑Q6 a). Emission
und Stromsteuer des Hilfsstroms sind nicht angesetzt (E30‑Q5, benannt), und die Katalogempfehlung „Hilfsenergiekosten
(Strom)“ 4–8 % am Kessel stammt aus Weg A und ist für Weg B zu hoch (E30‑Q12, eigener Katalogauftrag).

**Erlöse:** `IstErloes && wert > 0 → wert = −wert`, an drei Stellen identisch geklemmt.

**Vorrangregel Prozent vor Absolut** (aus KONTEXT § 5.3): Eine gepflegte Satzangabe schlägt den
Absolutbetrag; das unterlegene Feld wird **gesperrt, nicht geleert** (KL4) — anders als in der
Altanwendung, die beim Speichern die Absolutfelder leerte (stiller Datenverlust, Altbefund 6). Die
„oder"-Doppelfelder der Wartung (€/kWh_el neben €/h, dort tatsächlich **addiert**, Altbefund 7)
gibt es nicht mehr: **eine** Position mit sichtbarer Bemessungswahl.

**Basis „% der Investition" auf der Betriebsseite** (`InvestSummeFuer`): Summe der
**Investitionskaskade** (`InvestKaskade.Summen`), stufig Anlage → Komponente → Projekt, **vor**
Zuschussabzug — die abgeleiteten Beträge sind seit W5‑B‑8 enthalten (Befund B-5 erledigt).

**Sätze der Nutzungsdauertabelle** (Nutzungsdauer-Konzept Stufe S3, umgesetzt #463; E10‑Q1, → Register R‑E10). Die
Tabelle `Tab_Nutzungsdauer` führt je Technik und Positionsart einen Instandsetzungs- und einen Wartungssatz in % der
Investition je Jahr (VDI 2067 Blatt 1, Tabelle A2), gepflegt im Dialog „Nutzungsdauern (AfA)". Schemaschritt 120 sät an
den Standardzeilen die Mitte des Empfehlungsbereichs der Betriebsvorlagen — Heizkessel 2,0 %, BHKW 6,0 %, Wärmezentrale
2,0 %, Stromeinspeisung 2,0 %, Bauliche Anlagen 1,25 %; Wartung bleibt leer, weil keine Vorlage sie in % der Investition
führt (E10‑Q7). **Die Tabelle rechnet nicht selbst:** Der Rechenweg nimmt den Satz der Position, wie er gepflegt ist,
sonst den erfassten Betrag, sonst 0 — nichts ändert eine gerechnete Wirtschaftlichkeit ohne Zutun (ND‑Q4, → Register
R‑ND). Ein Satz der Tabelle wirkt allein, wenn er ausdrücklich in eine Position „Instandhaltung …"/„Wartung …" mit
„% der Investition" geschrieben wird: mit der vom Anwender ausgelösten Übernahme einer Kostenvorlage (für eine neue
Position, deren Vorlage keinen Satz trägt) oder mit „Sätze vorbelegen…" und „Speichern" (§ 2.8); die automatische
Anlage der Pflichtpositionen schreibt keinen. Zugeordnet wird über den Positionsschlüssel (`NutzungsdauerSaetze`: die
Technik aus dem Namen, sonst die der Komponente; die Zeile der Positionsart, sonst die Standardzeile), die Regel steht
einmal in `NutzungsdauerSatzCtrl.WirksamerSatz`. **Herkunft:** Gleicht ein gepflegter Satz dem der Tabelle, nennen die
Herkunftszeile des Kostendialogs, die Herleitung der Berichte und die Formelmappe (Stufe 3) „Satz aus
Nutzungsdauertabelle"; der Nachweisumschlag trägt sie ab Fassung 10 (`KostenPositionNachweis.SatzHerkunft`) — ein
Ausweis, keine Rechengröße.

**Die Betriebskostentabelle der Berichte** — „Betriebskosten nach Kostenarten" in Wort- und Tabellenbericht, je
Position Bezeichnung, Gruppe, Bemessung, Herleitung und Betrag (umgesetzt #460; E8b‑Q2 und E8b‑Q3, → Register R‑E8b;
offen E8c‑Q1 und E8c‑Q2, → Register R‑E8c):

- **Bemessung:** Jede Position nennt ihre Art mit dem Text des `BemessungKatalog` (Ressourcen `BM_*`, alle 18
  Steuerwerte, beide Sprachen) — derselbe Text wie in Kostendialog und Kostenseite, in der Beschriftung des Gewerks
  („je Liter" am Pufferspeicher, „je kW elektr. Leistung" am BHKW). „fester Betrag" steht nur bei `BETRAG` und bei
  leerem oder unbekanntem Steuerwert, weil der Rechenweg beide wie `BETRAG` rechnet.
- **Herleitung:** Menge × Satz nur an einer bemessenen Position — bemessen ist, wofür
  `BetriebskostenCtrl.Bemessungsfaktor` einen Faktor liefert (1 je Einheit, 0,01 bei Prozent); feste Beträge, feste
  Jahresbeträge, szenariogepflegte und unvollständige Positionen tragen keine. Die Formelmappe (§ 2.11.6, Stufe 3)
  fragt denselben Faktor.
- **Startjahr:** Eine Position mit Startjahr ≥ 2 (KD6) trägt in der Herleitungsspalte „ab Jahr X". Sie steht in der
  Summe der Tabelle, zahlt aber erst ab ihrem Jahr (§ 3.1) und gehört nicht zu den angesetzten Betriebskosten p. a.;
  der Hinweistext über der Tabelle sagt das in einem Satz.
- **Alle n Jahre:** Eine Position mit Wiederholperiode n ≥ 2 (#484, V‑G3) trägt in der Herleitungsspalte „alle n Jahre
  ab Jahr X" (ohne Startjahr X = 1; mit „ab Jahr X" in einem Text). Sie zahlt nur in ihren Zahlungsjahren (§ 3.1) und
  gehört zu den angesetzten Betriebskosten p. a., wenn sie im ersten Jahr zahlt (E16‑Q3 a) — die Probe der Gliederung
  geht damit auf; die Periode reist je Position im Nachweisumschlag (Fassung 11, nur bei n ≥ 2 geschrieben).
- **Probe der Gliederung:** Gegen die angesetzten Betriebskosten p. a. — die Jahr‑1-Zahl der Rechnung — wird die
  Summe der **Positionen des ersten Jahres** gehalten; weichen beide um mehr als 0,50 € ab, steht der Hinweis
  „Gliederung unvollständig" (`WIRT_BK_ABWEICHUNG`). Er trifft eine echte Lücke — eine Position der Rechnung, die in
  keinem Block der Tabelle steht, oder eine abgebrochene Nachweisliste —, nicht ein Startjahr. Wort- und
  Tabellenbericht rufen dieselbe Probe (`WirtschaftlichkeitZeilen.GliederungAbweichung`).
- **Nachweis:** Das Startjahr reist je Position im Nachweisumschlag (Fassung 9, nur bei ≥ 2 geschrieben). Ein älterer
  Umschlag liest seine Positionen als „ab dem ersten Jahr", die Probe vergleicht dort alle Positionen; das betrifft nur
  den Rückfall des Berichts auf den gespeicherten Stand, denn der Bericht rechnet frisch.

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
         kappt); das Stundenmittel (StromMatrix.MaxBezugKW) glättet die Spitze und bemisst
         allein die Leistungspreismodelle des Rollentarifs
Staffel vor Saisonreihe vor konstantem Satz — genau einer rechnet, nie eine Summe:
  Staffel:     min(S, G) × P₁ + max(0, S − G) × P₂    S = Jahresspitze, G = max(0, Grenze),
               P₁, P₂ in €/(kW·a) — gleich welcher Modus am Träger steht
  Modus JAHR:  Satz × Jahresspitze       Modus MONAT:  Σ₁₂ (Monatsspitze × Satz)
  Saisonreihe:                           Σ₁₂ (Monatssatz × Monatsspitze)
Satz 0 / nicht gepflegt ⇒ kein Anteil; ohne Zeitreihen ⇒ kein Anteil, der Träger wird benannt
```

**Die zweistufige Leistungspreis-Staffel** (Q11, → Register R‑Q, R‑E7b; umgesetzt #439) steht beim
Stromträger in der Kostenverwaltung (§ 2.5) — an der Projektübersteuerung, nicht im Katalog; gepflegt ist sie,
sobald einer der beiden Preise größer als 0 ist. Eine gepflegte Staffel **ersetzt** Leistungspreis und
Saisonreihe des Trägers, sie addiert sich nicht (E7b‑Q3); bemessen wird sie an der Viertelstundenspitze des
Jahres wie jeder Leistungspreis des Stromträgers (E7b‑Q2). Probe an 1030 mit 1.500 kW / 60 / 90 €/(kW·a) bei
2.011 kW Spitze: 1.500 × 60 + 511 × 90 = 135.990 €/a. Die Speicherauslegung bewertet eine Kappung mit dem Preis
der Stufe, in der die Spitze liegt.

**Kein Zeitzonentarif** (Q11, „kein HT/NT"). Einen Tarif nach Hoch- und Niedertarif, Winter und Sommer gibt es
nicht: Den Netzbezug bepreist der Stromträger. Die Strommatrix (`StromMatrix`) führt nur Mengen und Lasten —
je Projekt eine Jahreszeile in `Tab_ErgebnisStromMatrix` (Spalte `Zone` = „Jahr"). Im **Rollentarif**
(Tarifmodus `ROLLEN`, die Differenzmethode § 3.6) ersetzt der Reststrombetrag den **ganzen** Flat-Anteil samt
Leistungsanteil — auch eine Staffel des Trägers; das Rollenmodell bleibt (E7b‑Q1). Ein Tarifsatz, der nicht im
Rollenmodell steht, rechnet nicht: Schemaschritt 104 löscht ihn samt der mit ihm gespeicherten Läufe (E7b‑Q4),
und steht er noch in einer Datenbank vor dem Schritt, nennt ein Hinweis am Ergebnis den Wegfall
(`WIRT_HINWEIS_ZEITZONENTARIF`).

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
eingegeben** werden sie in der **Abrechnungseinheit** des Trägers (€/Nm³, €/l, €/t): Wer
einen Gaspreis pflegt, pflegt ihn je Normkubikmeter. Die Einheit wechselt **genau einmal**, an
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
`ID_Umrechnung` der Projektzeile; gerechnet wird mit H_i und H_s. Welche der beiden Basen die Karte zeigt, steht
als eigener Kartenzustand in `energy_project_settings.Preisbasis` (Schemaschritt 112, § 2.5; umgesetzt #446) —
eine Eingabehilfe ohne Rechenwirkung: Gerechnet wird unverändert mit dem Basiswert je Abrechnungseinheit.

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
Hinweis). Bilanz und BEHG-Reihe rechnen mit dem heizwertbezogenen Faktor auf die heizwertbezogene
Menge; **die Grenzwertprüfung des § 9 Abs. 1 Nr. 3 StromStG nimmt den brennwertbezogenen Faktor**
(§ 3.8, → Register R‑NR, Nr. 29).

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
50/25/10 % → 30.000/15.000/10.000; darunter 0 mit Fehlgrund. **Ohne Anlagenart** leitet § 8 kein
Kontingent ab: 0 h mit Grund, der Zuschlag der Anlage entfällt, und die Kohärenzprüfung meldet
„Anlagenart fehlt" (§ 3.9) — nur dann; ein gepflegtes Kontingent bleibt auch ohne Anlagenart wirksam
(§ 6.3 Nr. 30; → Register R‑E7, E7‑Q1 Lesart b; umgesetzt #440). Der Ersatzweg leitet das Kontingent
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
Fall 2(A): Eigen/Einsp(A) um Netto(A) − KWK-Strom(A) gekürzt, zuerst Einsp   (§ 2 Nr. 16, unten)
Bonus_voll = Eigen × 10 × SatzEigen + Einsp × 10 × SatzEinsp        [€/a bei ct/kWh]
Vbh(A)     = W_a ÷ P_Nenn = erzeugte Arbeit brutto(A) ÷ P_el,Nenn(A)   in Fall 1 und Fall 2 (unten)

je Jahr:  Vergütet = min(Vbh, Deckel(Jahr), Restkontingent) × (1 − Abschlag)
          Reihe[t] += Bonus_voll × Vergütet / Vbh
          Rest     −= Vergütet
```

Deckelstaffel 5.000 (2021) … 3.300 (2026) … 2.500 (ab 2030). Vorgeschaltete Prüfkette: Stichtag
≤ 31.12.2026 · Inbetriebnahme bis zum Ende der Frist zur Inbetriebnahme · Ausschreibung > 500 kW ·
Heizöl-Neuanlage ab 2025.

**Das Ende der Frist zur Inbetriebnahme ist ein Katalogdatum** (A20, R‑U5; → Register R‑E7, E7‑Q3
Lesart b; umgesetzt #440): `KWKG_INBETRIEBNAHME_FRISTENDE` = 2030, gemeint der 31.12.2030
(Gesetzeskatalog, Generation 8, Quelle KWKG 2025 § 6), nachgeschlagen mit dem Inbetriebnahmejahr —
geprüft je Anlage und im Projektblock, **auch ohne Stichtag** (entschieden E7c1‑Q3, nach Empfehlung). Eine Anlage mit
Inbetriebnahme danach bekommt keinen Zuschlag; die Herleitung nennt Fristende und Herkunft. Fehlt der
Katalogwert, rechnet der Zuschlag ohne die Frist, mit der Zeile „ungeprüft" — keine stille Vorgabe
(entschieden E7c1‑Q4, nach Empfehlung). Die Zuschlagsreihe läuft danach bis zum Ende des Kontingents, **eine Höchstdauer in
Kalenderjahren gibt es nicht** (Inbetriebnahme 01.10.2026: Jahr 12 = 2037).

**Die zwei Katalogzeilen der alten Frist sind abgekündigt** (E7c1‑Q8, → Register R‑E7c1; umgesetzt #452).
`KWKG_REALISIERUNGSFRIST` (4 Jahre) und `KWKG_STICHTAG_DAUERBETRIEB` (2026) liest kein Code; sie tragen den vierten
Status ABGEKUENDIGT neben GESICHERT, VORLAEUFIG und PROGNOSE — die Vorbelegung sät sie mit diesem Status, gelöscht wird
nichts, Wert, Stichjahr und Quelle bleiben. In bestehende Datenbanken bringt ihn die **Katalog-Generation 9**: Sie
sät keine Zeile, sondern pflegt bestehende nach — in jeder Datenbank mit einem Saatstand darunter genau einmal, beim
Start oder beim ersten Katalogzugriff, ohne Schemaschritt (`GesetzKatalog.Nachpflege`); scheitert ein Schritt, steigt
der Marker nicht, und der Grund steht in den Saatwarnungen. Dieselbe Generation setzt beim Brennstoff 24 „Sonstige"
H_i = H_s = 1,0 (§ 5). Ob Nachpflege und vierter Status so bleiben, ist offen gestellt (E7c3‑Q2, E7c3‑Q3, → Register
R‑E7c3; gebaut ist jeweils Lesart a).

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
CO₂-Grenzwert und die Vollbenutzungsstunden bleiben **brutto** — auch bei einer Anlage im zweiten Fall des § 2
Nr. 16 (Vbh = W_a ÷ P_Nenn, unten).

**Rechtskette.** Der Nachweis (dass die Fundstelle erst nachgetragen wurde: → Protokoll § 2.5):

| Norm | Wortlaut |
|---|---|
| **§ 7 Abs. 1 KWKG** | „Der Zuschlag für **KWK-Strom**, der in ein Netz der allgemeinen Versorgung eingespeist wird …" |
| **§ 7 Abs. 2 KWKG** | „Der Zuschlag für **KWK-Strom**, der nicht in ein Netz der allgemeinen Versorgung eingespeist wird …" |
| **§ 2 Nr. 16 KWKG** | „**KWK-Strom** ist das rechnerische Produkt aus Nutzwärme und Stromkennzahl der KWK-Anlage; bei Anlagen, **die nicht über Vorrichtungen zur Abwärmeabfuhr verfügen, ist die gesamte Nettostromerzeugung KWK-Strom**" |
| **§ 2 Nr. 20 KWKG** | „**Nettostromerzeugung** ist die an den Generatorklemmen gemessene Stromerzeugung einer Anlage **abzüglich des Stromverbrauchs der Stromerzeugungsanlage oder von deren Neben- und Hilfsanlagen**" |

Damit ist die Kette geschlossen: § 7 zahlt auf KWK-Strom → bei Anlagen ohne Abwärmeabfuhr ist das
die Nettostromerzeugung → und die zieht den Hilfsstrom ab. **Das Netting ist richtig**, und der
Begriff „Nettostromerzeugung" ist der des Gesetzes, keine Erfindung des Konzepts.

**Der zweite Fall des § 2 Nr. 16 — Anlagen mit Vorrichtung zur Abwärmeabfuhr** (Befund K‑1, § 4;
→ Register R‑EZ, EZ‑5, und R‑E7, E7‑Q2; umgesetzt #440). Verfügt eine Anlage über eine Vorrichtung zur
Abwärmeabfuhr — beim Notkühler größerer BHKW der Regelfall —, ist ihr KWK-Strom nicht die
Nettostromerzeugung, sondern `Nutzwärme × Stromkennzahl`, höchstens die Nettostromerzeugung. Das
Kennzeichen `KWKG_Abwaermeabfuhr` (0/1, `CHECK`, Vorgabe 0 = Fall 1) und die Stromkennzahl
`KWKG_Stromkennzahl` (nullbar) sind zwei Anlagenspalten von `Tab_Energieanlagen` neben den neun
`KWKG_*`-Spalten (Schemaschritt 105, `SCHRITT_105_KWKG_ABWAERMEABFUHR`); gepflegt werden sie in der
Überlagerung „Sätze und Herkunft" des BHKW-Dialogs (§ 2.2). Gerechnet wird in `KwkStromRechner`, eingebaut
in `WirtschaftlichkeitCtrl.ReiheJeAnlage` und den Ersatzweg:

```
σ(A)          = KWKG_Stromkennzahl(A)                  gepflegt (> 0)
              sonst P_el(A) ÷ P_th(A) der Gerätezeile   berechnet (Tab_BHKW)
              sonst keine                               kein Ersatzwert, keine Vorgabe
Nutzwärme(A)  = max(0, Wärme(A) − Überschuss × P_el(A) / Σ P_el)
KWK-Strom(A)  = min(Netto(A), Nutzwärme(A) × σ(A))      ohne σ: 0 — kein Zuschlag der Anlage
Kürzung(A)    = Netto(A) − KWK-Strom(A)                 zuerst von Einsp(A), dann von Eigen(A)
```

Die fünf Teilantworten zu E7‑Q2 sind die Regel: (1) Die Wärmeproduktion bleibt je Modul
(`ErgebnisBHKWModulModel.Waermeproduktion`); nur der Wärmeüberschuss, den das Ergebnismodell als
Projektsumme führt, wird nach P_el verteilt. (2) σ ist eine Geräteeigenschaft — der gepflegte Wert, sonst
P_el ÷ P_th der Gerätezeile; geht beides nicht, gibt es **keinen Ersatzwert**: Der Zuschlag der Anlage ist
0, die Herleitung sagt es, und die Kohärenzprüfung meldet „Stromkennzahl fehlt" (§ 3.9; Auflage des
Anwenders). (3) Der Anteil am Split bleibt physikalisch (nach Netto); gekürzt wird die zugeteilte Menge der
Anlage, **zuerst die Einspeisung**. (4) Auf dem Ersatzweg werden Nutzwärme und Nettostromerzeugung des
Projekts nach P_el auf die Anlagen verteilt (führt keine Anlage P_el, zu gleichen Teilen), jede Anlage
rechnet mit ihrem σ, die Summe der Kürzungen geht zuerst von der Einspeisung ab; die Zeile „Ersatzweg" nennt
es. (5) Gepflegt wird in der Überlagerung „Sätze und Herkunft" (§ 2.2).

Die Herleitung je Anlage nennt Fall, σ mit Herkunft, Nutzwärme (Wärmeproduktion − Anteil am Überschuss),
KWK-Strom und Kürzung, davon Einspeisung und Eigenverbrauch; `KwkgModulNachweis` führt dazu sieben nullbare
Felder ohne eigene Fassung (entschieden E7c1‑Q6, nach Empfehlung); die Fassung des Nachweisumschlags ist 9 — 8 mit
der Energiesteuer-Vorschau (§ 3.7), 9 mit dem Startjahr je Betriebskostenposition (§ 3.4, #460).

**Die Vollbenutzungsstunden zählen in beiden Fällen brutto: Vbh = W_a ÷ P_Nenn** (Definition des Anwenders vom
23.09.2026, die E7c1‑Q2 Lesart b präzisiert, → Register R‑E7c1; umgesetzt #452). W_a ist die jährlich erzeugte
Arbeit — hier die elektrische Arbeit des Moduls brutto an den Generatorklemmen —, P_Nenn die installierte elektrische
Nennleistung; die Kennzahl misst die Auslastung einer modulierend oder getaktet laufenden Anlage, hochgerechnet auf
volle Stunden. Kontingentverbrauch, Jahresdeckel und Kontingentreihe zählen diese Stunden in Fall 1 und Fall 2
gleich, auch auf dem Ersatzweg (erzeugte Arbeit der Gesamtanlage ÷ Σ P_el); der KWK-Strom bestimmt allein die
bezahlte Menge. In Fall 2 nennt eine Hinweiszeile die Formel mit ihren Zahlen — „Vbh = erzeugte Arbeit ÷ P_Nenn =
… MWh ÷ … kW = … h/a (brutto an den Klemmen, wie in Fall 1)" — und den KWK-Strom als bezahlte Menge
(`WIRT_KWKG_FALL2_VBH`, `WIRT_KWKG_FALL2_VBH_ERSATZ`). Ohne gespeicherte Modulzahl nimmt der Rückfall
`VbhDerAnlage` ebenso die Bruttoerzeugung. Bindet der Jahresdeckel, gilt der Deckelanteil von Fall 1, und eine
Kürzung des Falls 2 mindert die bezahlte Menge voll (E7c2‑Q7 ist damit erledigt, → Register R‑E7c2). Eine Kürzung
unter 0,01 MWh rechnet die Formel ohne Toleranz; die Herleitung nennt ihren Grund — die Rundung von σ bzw. der
Mengen auf 0,01 MWh (`WIRT_KWKG_FALL2_RUNDUNG`; entschieden E7c1‑Q1, nach Empfehlung, mit Hinweis; umgesetzt
#446). Mit berechnetem σ und ohne Wärmeüberschuss trifft `Nutzwärme × σ` etwa die Bruttoerzeugung und liegt über
der Nettostromerzeugung — eine Kürzung entsteht dann erst mit Wärmeüberschuss oder einem gepflegten kleineren σ
(Beispielprojekt: 1.953,9 × 0,845 = 1.651,2 > 1.563,2 MWh), und ohne Kürzung rechnet Fall 2 genau wie Fall 1
(Beispielprojekt, von Hand gerechnet: Jahr 1 32.022,2 € — Rechenweg 05). Der Torwächter `BaueKwkgReihe`
(`v.Ergebnis.BHKW.Stromproduktion` als Summe) bleibt. **Referenzprojekte:** Kein Projekt der Testdatenbank trägt
das Kennzeichen; die dreizehn Basisprojekte sind gemessen unverändert, keine neue Basis (Proben an 1030 in den
Protokollen E7c1 bis E7c3, etwa σ 0,5: KWKG Jahr 1 7.315,96 € in Fall 1, 6.137,94 € in Fall 2 bei 7.475,69 h/a).

### Einspeiseerlös

`PV_Überschuss × 10 × EV + KWK_Einspeisung × 10 × EV_KWK` (KWK-Teil nur bei gepflegtem Satz) →
Rollentarif (ein Einspeisepreis für beide Mengen) → PV-Dialog ersetzt den PV-Anteil durch seine Reihe.
**Nominal konstant.**

### Photovoltaik / EEG

```
Degression:  Faktor 0,99^n  (Halbjahresstichtage 1.2./1.8. ab 01.02.2024 bis Inbetriebnahme)
AW_mix    =  round( Σ Anteil_k × AW_Klasse_k / Σ Anteil_k , 2)
             marginale Klassen 10 / 40 / 100 / 400 / 1000 kWp
EV_mix    =  max(0, AW_ungerundet − 0,40)   nur ≤ 100 kW; UNGERUNDET (Mix bzw. AW-Override)
Erlös EV  =  round(Arbeit × EV_mix / 100 ; 2)          gerundet wird allein der Erlös, auf Cent
Ausfallvergütung = AW × (1 − 20 %)          nur > 100 kW

§ 51 je Jahr (AUTO):  IBN < 25.02.2025 → nein ;  ≥ 100 kWp → ja ;
                      sonst ab dem Jahr nach dem iMSys-Einbau
Ausfallanteil a:      Pauschale 20 %  oder stundenscharf  Σ Einsp(Spot<0) / Σ Einsp
60-%-Kappung:         Verlust = Σ max(0, Einsp_h − 0,6 × kWp)
Marktprämie:          Erlös = Spot€ + Arbeit × max(0, AW − Jahresmarktwert)/100
                             − Arbeit × DV/100
§ 51a:                im letzten Vergütungsjahr  Ausfallarbeit_J1 × 0,5 × Satz/100   (ungerundet)
                      Satz = EV_mix bei fester Vergütung, sonst AW (Direktvermarktung)
```

Belege: 8,60 × 0,99⁵ → 8,10 ct/kWh ab 08/2026 (16/16 BNetzA-Werte exakt) · 300 kWp → 6,04 ct/kWh ·
Marktprämie Jahr 1 = 13.536,00 € · § 51a = 1.812,00 €.

**V‑1 und V‑2** (A4, → Register R‑A; umgesetzt #446). Bei fester Einspeisevergütung rechnet der EV-Mix ungerundet
— der ungerundete Mix des anzulegenden Werts (bzw. der Override) abzüglich des Abschlags —, und gerundet wird allein
der Erlös, auf Cent; die Herleitung nennt den ungerundeten Satz (`PvErloesErgebnis.EvCt`). § 51a bewertet die
Ausfallarbeit bei fester Vergütung mit der Einspeisevergütung, die die Anlage in der Verlängerung tatsächlich
bekäme, in der Direktvermarktung weiter mit dem anzulegenden Wert; der § 51a-Betrag bleibt ungerundet (E7c2‑Q6).
Der Satz der Speicherbewertung (`VpvCtKwh`) nimmt bei fester Vergütung denselben ungerundeten Satz wie die
Erlösreihe (E7c2‑Q5, Lesart b, → Register R‑E7c2; umgesetzt #452) — Rechner 100 kWp 6,03 → 6,032 ct/kWh, 750 kWp
5,52 → 5,518933 ct/kWh; der Override bleibt, die Marktprämie rechnet weiter mit dem gerundeten anzulegenden Wert.
Proben mit fester Vergütung: 1040 Erlös Jahr 1 139,832 → 139,83 €, § 51a 18,39 →
17,48 €; der Rechner mit 100 kWp Jahr 1 3.207,96 → 3.209,02 €, § 51a 427,60 → 401,13 €. Kein Basisprojekt führt den
PV-Vergütungsdialog; die Pinnung der Marktprämie (#380) bleibt gleich.

### Vermiedene Stromkosten — Ausweis, kein Zahlungsstrom

```
Bezug     = Rollenkosten(Bezugstarif,    Bedarf OHNE JEDE EIGENERZEUGUNG)
Reststrom = Rollenkosten(Reststromtarif, Restbezug MIT Anlage)
Vermieden = Bezug − Reststrom       je Arbeit / Leistung / Gesamt
Menge     = Bedarf ohne jede Eigenerzeugung − Restbezug      (KWK- und PV-Eigenverbrauch)
Schlüssel = Eigenverbrauch je Anlage, brutto aus der Strommatrix
            BHKW  KwkEigenGesamtMWh   min(BHKW, Bedarf nach PV) — der KWK-Split bleibt
            PV    PvEigenGesamtMWh    PV-Eigennutzung, soweit sie Bedarf deckt
            Menge, Arbeit und § 9b-Korrektur anteilig; der Leistungsanteil bleibt projektweit
```

Der **Leistungsanteil ist regelmäßig negativ** — das ist die Kernaussage, kein Fehler. In den
Kapitalwert geht der **Reststrom**betrag; die Differenz zusätzlich zu buchen wäre Doppelzählung
(E5, fünffach belegt).

**Ohne jede Eigenerzeugung** (§ 6.3 Nr. 32, → Register R‑NR; umgesetzt #437). Bedarf und Lastbild
der Bezugsseite stehen **vor** Abzug der PV-Eigennutzung (`StromMatrix.BedarfGesamtMWh`,
`LastBedarf`), die vermiedene Menge führt KWK- und PV-Eigenverbrauch, und die § 9b-Korrektur (§ 2.6)
greift auf beide. Der Hilfsstrom berührt diese Menge nicht, er mindert allein die KWKG-Mengen; ohne
Speicher bekommt jede Anlage damit genau ihren Eigenverbrauch, bei mehr als einer Anlage nennt die
Herleitung die Aufteilung „Näherung" (V‑4). Im Rollentarif ersetzt der PV-Anteil die Ausweiszeile
„PV: vermiedener Bezug" zum Flat-Preis; im Flat-Tarif bleibt sie. Der Kapitalwert bleibt unberührt —
er rechnet mit dem tatsächlichen Restbezug; gespeichert ändert sich allein der Strommatrix-Bedarf
(`Tab_ErgebnisStromMatrix.Bedarf`) der Projekte mit Photovoltaik. Probe am Beispielprojekt:
1.179,7 = 1.094,2 + 85,5 MWh, wirksam 293.245,6 + 22.914,0 = 316.159,6 €/a.

**Bedarf aller Verbraucher** (E26, #518; → Register R‑E26). Bedarf ohne jede Eigenerzeugung ist der
Strombedarf aller Verbraucher des Anschlusses (Projektbedarf, Wärmepumpe, Heizstab, Elektrokessel,
Kältestrom der Stufenrechnung; Reihe `STROMBEDARF_GESAMT`); auch der KWK-Split (Eigenstrom =
min(BHKW-Strom, Bedarf nach PV-Eigennutzung)) misst sich daran, weil die Simulation das BHKW den Strom
der Wärmepumpe decken lässt (E26, Entscheid E26‑Q3). Die Reihe ist der Rest nach der Kaskade plus
BHKW-Strom (`SimulationControl.Strombedarf_Verbraucher_viertelstuendlich`); der Kältestrom mit eigenem
Zähler gehört nicht dazu (E34). `StromMatrix.Baue` nimmt sie für Bedarf, PV-Eigenverbrauch, Lastbild und
KWK-Split und fällt auf den Projektbedarf `STROMBEDARF` zurück, wo sie fehlt. An den Referenzen bleiben
KWK-Split und Kapitalwert gleich; in einem Projekt, dessen BHKW-Strom zwischen Projekt- und Bruttobedarf
liegt, kann der Kapitalwert über KWKG-Zuschlag, Stromsteuer und Einspeiseerlös wandern.

**PV-Stromproduktion des Ausweises** (E26, E26‑Q1, E26‑Q4). `Ergebnis.Photovoltaik.Stromproduktion` ist
die Erzeugung der Module nach Wechselrichter und Clipping (Summe der Modulzeilen); der Eigenverbrauch des
Ausweises ist Erzeugung − Einspeisung, einschließlich der Speicherladung. Die Zeile „PV: vermiedener
Bezug“ rechnet mit diesem Eigenverbrauch und ist damit nie negativ. Die Zeitreihe `pv_produktion.csv`
bleibt der direkt genutzte Anteil.

**Netzbezug nie negativ** (E27, #521; → Register R‑E27). Der Netzbezug ist nie negativ: Ein BHKW-Überschuss,
den keine spätere Stufe (Verbraucher derselben Viertelstunde, Photovoltaik, Stromspeicher) aufnimmt, steht allein im
KWK-Split als Einspeisung; der Reststrom wird am Laufende bei 0 geklemmt, der Reststrombedarf der BHKW-Zeile je
Stunde (E27, Entscheide E27‑Q1/Q4). Die Kaskade zieht den BHKW-Strom weiter ungeklemmt ab, damit spätere Verbraucher
derselben Viertelstunde und die Photovoltaik den Überschuss sehen; die Klemme (`SimulationControl.NetzbezugGeklemmt`)
setzt vor `ReststromMwh` nur Werte unter 0 auf 0 und greift nicht bei der Speicherflotte, deren Netzbilanz den Rest
ersetzt. Die Reststromkosten des Rollentarifs werden damit keine Gutschrift mehr neben dem Einspeiseerlös des
KWK-Splits, und der Netzbezug trägt keine CO₂-Gutschrift mehr. Seit E28 (#535; → Register R‑E28) gilt dieselbe Regel
für die Stufeneingänge: Der PV-Modus der Wärmepumpe reagiert nur auf PV-Überschuss — ein negativer Bedarf zählt je
Stunde als 0 (`SimulationControl.PvUeberschussVorab`), ein BHKW-Überschuss ist nie PV-Überschuss —, und der
Strom-Stufeneingang der Kessel- und der PV-Zeile wird je Stunde bei 0 geklemmt (`NetzbezugGeklemmt`), einheitlich mit
dem Reststrombedarf der BHKW-Zeile (E28‑Q1…Q3); die Stundenrechnung des Kessels liest diese Reihe nicht, und das BHKW der
Speicherstufe bekommt weiter den ungeklemmten Eingang.

**BHKW-Einspeisung und Gesamtbedarf im Ausweis** (E29, #536; → Register R‑E29). Die BHKW-Einspeisung ist eine
Ausweisgröße und gleich dem KWK-Split, je Stunde Σ max(0, BHKW − max(0, Strombedarf aller Verbraucher −
PV-Eigenverbrauch)) (`SimulationControl.BhkwEinspeisungStuendlich`; mit Speicherflotte die BHKW-Einspeisung der
Flottenbilanz): Der BHKW-Reiter zeigt sie als Zeile „Stromeinspeisung“ nach der Stromproduktion, die Diagnosereihe
`BHKW_UEBERSCHUSS` und der Excel-Monatsblock führen sie auch ohne PV und Flotte; Strombilanz-Diagramm und Excel-Spalte
„Strombedarf“ messen den Strombedarf aller Verbraucher (`STROMBEDARF_GESAMT`, Rückfall `STROMBEDARF`), die Übersicht
„Strombedarf mit Eigenverbrauch“ zählt den Kältestrom der Stufenrechnung mit, und der PV-Deckungsgrad teilt durch den je
Stunde geklemmten Bedarf (E29‑Q1…Q10). Kapitalwert und Strommatrix bleiben unberührt. Die Stromdeckung des BHKW ist
seit E30 (#548; → Register R‑E30, E30‑Q7 a) sein Eigenverbrauch am Strombedarf aller Verbraucher, (Erzeugung −
BHKW-Einspeisung) ÷ Σ `Strombedarf_Verbraucher`, auf 0 bis 100 % geklemmt — eine Formel
(`SimulationErgebnisCtrl.BhkwStromdeckungProzent`) für `BHKW.Strombedarfsdeckung` des Laufs (persistiert, `aggregate.csv`,
Word-Torte), den BHKW-Reiter und Stromring und Stromtabelle der Übersicht, die beim BHKW den Eigenverbrauch zeigen; die
Einspeisung deckt keinen Bedarf (1018 im Ring nicht mehr über 100 %, 1024 26,22 → 20,94 %; Basis R21).

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
       GESPERRT neben § 53/§ 53a (Mischlage, unten): netto = 0, Begründung an der Zeile
```

**Die Mischlage ist gesperrt** (Befund S‑2, A3, → Register R‑A; umgesetzt #446). Stehen im Projekt § 53 oder
§ 53a Abs. 5 und § 54 nebeneinander, wird der § 54-Betrag verworfen: 0 €, der Sockel entfällt, die
§ 54-Posten verlassen den Nachweis, und die § 54-Zeile trägt die Begründung (`STEUER_ENERGIEST_54_MISCHLAGE`);
die Entlastung nach § 53 / § 53a bleibt. Solange R‑U1 offen ist (§ 5), ist die Kombination nie zulässig.
Geprüft wird an der wirksamen Wahl je Anlage mit Brennstoffeinsatz (Anlagenwert, sonst Projektwert), in einer
Prüfung für Sperre und Kohärenzzeile (`SteuerGutschriftRechner.Mischlage`); auf der § 53-Seite zählt nur eine
Anlage mit Stromerzeugung — ein Kessel mit § 53-Wahl rechnet ohnehin 0 und begründet keine zweite
Entlastungswelt (E7c2‑Q1, → Register R‑E7c2). Die Kohärenzprüfung meldet die Sperre als Warnung (§ 3.9). Probe
an 1030 (produzierendes Gewerbe, BHKW § 53, Kessel § 54): § 54 Jahr 1 7.987,41 → 0 €; kein Basisprojekt trägt
eine Mischlage.

**Die Vorschau je Wahl** (E7c2‑Q8, Lesart b, → Register R‑E7c2; umgesetzt #452). Der Lauf rechnet für jede Anlage
mit Brennstoff die Energiesteuer jeder wählbaren Entlastung vor — keine, § 53 voll und energetisch, § 53a Abs. 5,
§ 54 — mit Satz, Menge und Wirkung im ersten Jahr (`SteuerGutschriftRechner.Vorschau`, Zeilen
`EnergiesteuerVorschauZeile`). Ein zweiter Rechenweg ist das nicht: Es ist dieselbe Rechnung wie oben, auf einer
Kopie der Steuereingabe, in der allein die Wahl dieser Anlage gesetzt ist; die Wirkung ist die Entlastung des
Projekts mit der Wahl minus die mit „keine" für diese Anlage — Sockel und Mischlage wirken wie im Lauf, ihr Grund
steht an der Zeile, die Eingabe des Laufs bleibt unberührt. Bei „Projektvorgabe für alle Anlagen" steht die
Vorschau je Anlage (E7c3‑Q8, offen, → Register R‑E7c3). Sie reist im Nachweisumschlag (Fassung 8; eine ältere
Fassung liest sich mit leerer Vorschau) und steht damit auch am gebuchten Stand; die Überlagerung „Sätze und
Herkunft" zeigt sie in der Zeile jeder Wahl (§ 2.2). Handprobe am Beispielprojekt (Rechenweg 05,
`EnergiesteuerVorschauTests`): § 53 voll 26.383,46 €, § 53 energetisch (Anteil 0,458) 12.079,17 €, § 53a Abs. 5
21.202,71 €, § 54 nach dem Sockel 6.369,85 €.

**Einheitenkette** — hier ist die Altanwendung um den Faktor 10 gescheitert:

| Einheit | Formel | Bedingung |
|---|---|---|
| €/MWh | MWh_Hi × (eff_hs / eff_hi) → Brennwertmenge (Erdgas 11,6/10,5 = 1,1048) | Hs und Hi > 0; sonst konservativ Hi + Hinweis |
| €/1.000 l bzw. kg | MWh × 1000 / eff_hi / 1000 | Hi > 0 **und** passende Abrechnungseinheit — **keine geratene Dichte** |
| €/GJ | MWh × 3,6 (Hi) | — |

Sätze: Erdgas 5,50 / 4,42 / 1,38 €/MWh · Heizöl EL 61,35 / 40,35 / 15,34 €/1.000 l · Sockel
250 €/a. Handproben 11/11 auf vier Nachkommastellen getroffen. Dieselbe Umrechnung H_i/H_s der Werte
des Trägers (Projektwert vor Katalogwert) stellt den brennwertbezogenen CO₂-Faktor der
Grenzwertprüfung des § 9 Abs. 1 Nr. 3 StromStG, wo der Katalog keinen führt (§ 3.8).

## 3.8 Stromsteuer

```
§ 9 Abs. 1 Nr. 3   Betrag = Regelsatz(Jahr) × KwkEigen [MWh/a] × Anteil
                   Anteil = Σ Strom(a, bestanden) / Σ Strom(a, alle)
                   Regelsatz 20,50 €/MWh (ab 2026)

   Vier Bedingungen: Hocheffizienz · räumlicher Zusammenhang 4,5 km (Anwenderangaben)
                     P_el ≤ 2 MW je Anlage
                     CO₂ < 270 g/kWh Energieertrag = Faktor_Ho × Brennstoff/(Strom+Wärme)
   BRENNWERTBEZOGEN:  Faktor_Ho = Katalogwert H_s, wo der Katalog einen führt
                        (Erdgas EF_BILANZ_EBEV_ERDGAS_HO 181,4 statt _HI 200,9 g/kWh)
                      sonst heizwertbezogener Katalogwert × H_i/H_s des Trägers
                      ohne gepflegten Brennwert der Hi-Faktor — konservativ, mit Begründung
                      Brennstoff bleibt die heizwertbezogene Menge des Rechenkerns
   KwkEigen nur mit Stundenreihen — sonst 0 mit Begründung
   Beleg: Beispielprojekt Erdgas 181,4 × 4.342,1 / (1.650,0 + 1.953,9) = 218,6 g/kWh → Befreiung
          Grenzfall, Energieertrag 72 % des Brennstoffs: Hi 279,0 → Ho 251,9 g/kWh,
          Befreiung 0,00 → 8.200,00 €/a
          Heizöl EL im Beispiel 266,4 × 1,2048 = 321,0 g/kWh heizwertbezogen, auch
          brennwertbezogen über 270 (mit H_i/H_s 0,9052: 290,5) → keine Befreiung

§ 9b               Betrag = max(0, 20,00 €/MWh × Netzbezug [MWh/a] − 250 €/a)
                   Bedingung produzierendes Gewerbe; hängt an keiner KWK-Anlage
```

Die Mengen beider Vorschriften sind **disjunkt** (Eigenverbrauch gegen Netzbezug) — untereinander
keine Doppelzählung.

**§ 9b ist ohne BHKW erreichbar** (gebaut #498, E19; § 6.3 Nr. 33). Die Bedingung liest die Unternehmensart des
Projekts (`Tab_ProjektWirtschaftlichkeit.Unternehmensart`): produzierendes Gewerbe oder Land- und Forstwirtschaft, sonst
null (`WirtschaftlichkeitCtrl.BaueSteuerEingabe`, seit E5 ohne Bezug auf eine KWK-Anlage). Gepflegt wird sie mit BHKW im
Dialog „BHKW-Wirtschaftlichkeit“ (§ 2.2 Gruppe 4), ohne BHKW im Parameterdialog, Gruppe Strom (§ 2.4). Nachweis:
`UnternehmensartOhneBhkwTests` — Projekt 1041 ohne BHKW rechnet mit produzierendem Gewerbe eine Entlastung > 0, mit
`KEIN_PROD_GEWERBE` 0.

**Der CO₂-Grenzwert ist brennwertbezogen** (§ 6.3 Nr. 29, → Register R‑NR; umgesetzt #437,
`SteuerGutschriftRechner.Co2JeEnergieertrag`). Allein der Faktor wechselt die Bezugsgröße; ein
heizwertbezogener Zähler fiele rund 10 % zu hoch aus, und die Befreiung entfiele in Grenzfällen zu
Unrecht. Die Herleitung nennt den Wert je Anlage mit dem Faktor, aus dem er entstand — in der
Begründung, wenn die Anlage über dem Grenzwert liegt, sonst in der Herkunft der Befreiung („BHKW 1:
218,6 g/kWh (EBeV 181,4 g/kWh, brennwertbezogen)"); ohne gepflegten Brennwert sagt eine Begründung,
dass heizwertbezogen geprüft wurde. Bilanz und BEHG-Reihe lesen diesen Wert nicht (§ 3.5).

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
| 5 **Mischlage § 53/§ 53a neben § 54 — gesperrt** | im Projekt § 53 oder § 53a Abs. 5 an einer Anlage mit Stromerzeugung und § 54 an einer anderen, beide mit Brennstoffeinsatz — dieselbe Prüfung, mit der der Rechner den § 54-Betrag verwirft (§ 3.7) | **Warnung**; nennt beide Seiten je mit Norm und Herkunft der Wahl und die Sperre (`KOH_FALL5_MISCHLAGE_SPERRE`, umgesetzt #446; entschieden A3, E7c2‑Q1) |
| **Doppelzählung § 9 Abs. 1 Nr. 3** | Modus `ERLOES` bucht einen Betrag | Warnung **mit Betrag** |
| Doppelpflege Hilfsenergie | Anlagenanteil > 0 **und** aktive Kostenposition derselben Anlage | Warnung |
| **CO₂-Bestandteil im Arbeitspreis und BEHG-Reihe gleichzeitig aktiv** | der Träger weist einen CO₂-Anteil im Arbeitspreis aus **und** die BEHG-Reihe rechnet denselben Brennstoff | Warnung **mit Betrag** — dem gebuchten Jahresbetrag der CO₂-Abgabe; der Rechenweg bleibt (Fall `Co2DoppelansatzBehg`, umgesetzt #405) |
| Strommix-Rückfall | kein Stromträger, Netzbezug > 0 | Hinweis **mit Wert** — der Strommix-Vorgabewert in g CO₂/kWh, mit dem der Netzbezug gerechnet ist (`KOH_CO2_STROMMIX_RUECKFALL`, umgesetzt #405) |
| **Stromkennzahl fehlt** | Kennzeichen „Vorrichtung zur Abwärmeabfuhr" gesetzt, aber weder eine gepflegte Stromkennzahl noch P_el und P_th der Gerätezeile — der Zuschlag der Anlage ist 0 (§ 3.6, zweiter Fall des § 2 Nr. 16) | Hinweis (ohne Betrag), eine Zeile mit allen betroffenen Anlagen (`KOH_KWKG_STROMKENNZAHL_FEHLT`, umgesetzt #440; Schwere: entschieden E7c1‑Q5, nach Empfehlung) |
| **Anlagenart fehlt** | weder ein Vbh-Kontingent gepflegt noch eine Anlagenart erfasst — § 8 leitet kein Kontingent ab, der Zuschlag der Anlage ist 0 (§ 3.6, § 6.3 Nr. 30); ein gepflegtes Kontingent löst die Zeile nicht aus | Hinweis (ohne Betrag), eine Zeile mit allen betroffenen Anlagen (`KOH_KWKG_ANLAGENART_FEHLT`, umgesetzt #440; Schwere: entschieden E7c1‑Q5, nach Empfehlung) |
| **Prüfung nicht ausführbar** | eine Teilprüfung dieser Tafel scheitert an einem Lesefehler (etwa einer nicht lesbaren Tabelle) | **Warnung** an ihrer Stelle: „Prüfung „X" nicht ausführbar: ‹Grund›" (`KOH_PRUEFUNG_NICHT_AUSFUEHRBAR` mit zehn Prüfungsnamen `KOH_TP_*`; umgesetzt #452, B‑6) |
| **Rechenstufe nicht ausführbar** | eine Rechenstufe der Wirtschaftlichkeit scheitert — Parameter, Tarif, Anlagen, elektrische Leistung, Energieträger, Heizölprüfung, Betriebskosten, Gesetzeskatalog, Kohärenzprüfung, KWKG-Satzherleitung, Speichern oder Laden der Ergebnisse; die Stufe rechnet mit ihrem benannten Rückfall weiter | **Warnung** an jeder Ergebniszeile des Projekts, je Grund einmal: „Rechenstufe „X" nicht ausführbar: ‹Grund›" (`WIRT_STUFE_NICHT_AUSFUEHRBAR` mit zwölf Stufennamen; umgesetzt #452, B‑6); scheitert das Speichern, gilt der Kapitalwert, gespeichert ist er nicht |

**Der Grund statt der stillen Null** (Befund B‑6, § 4; umgesetzt #452 für `KohaerenzPruefung`,
`EmissionsBilanzRechner`, `Emissionsquelle`, `GesetzKatalog` und `WirtschaftlichkeitCtrl`). `DataRepository` wirft
bei einem Abfragefehler nie — es meldet ihn selbst und liefert eine leere Tabelle bzw. null. Diese fünf Dateien
lesen deshalb über den strengen Weg `StilleDb.TabelleStreng`/`ScalarStreng` (26 Lesestellen), der den Fehler
weiterreicht (E7c3‑Q4, → Register R‑E7c3). Die Emissionsbilanz nennt eine gescheiterte Stufe in ihrem Hinweis
(„Emissionsbilanz — Stufe „X" nicht ausführbar: ‹Grund›"), die Herkunft eines Emissionsfaktors einen Lesefehler
(„… — Brennstoffkatalog nicht lesbar: ‹Grund›"). Gründe ohne eigene Zeile tragen die Eigenschaften `Lesefehler`
(Parametersatz, Tarif, Emissionsfaktoren, Gesetzeskatalog), `LetzterFehler` (Pflegewege des Gesetzeskatalogs),
`Katalogfehler` (Kraftwerksparks), `Ladefehler`, `Speicherfehler` und `Vorsorgewarnung` (Wirtschaftlichkeit). Die
letzten drei zeigt die Oberfläche (E7c3‑Q6 a, gebaut #474, → Register R‑E7c3): `Fehlergrund.Anzeigezeilen` bildet je
Grund eine Zeile — Laden, Speichern, Vorsorge —, derselbe Grund aus zwei Quellen ist eine Zeile, der Text ist der des
Kerns ohne Stapel und ohne „neu berechnen"-Hinweis. Die **Statuszeile der Ergebnisseite** nennt beim Laden den
`Ladefehler` von `LadeErgebnisse`/`LadeSensitivitaet` und die `Vorsorgewarnung`, einen Speicherfehler der Referenzwahl
einmal beim folgenden Laden und den der nicht monetären Wirkungen beim Schreiben; der **Dialog BHKW-Wirtschaftlichkeit**
zeigt Lade- und Vorsorgegrund als Warnband (es entfällt, wenn eine Kohärenzzeile denselben Grund nennt) und hängt einen
Speicherfehler an die Fehlermeldung von „OK". Einen Datenbankfehler beim Schreiben meldet `DataRepository` selbst,
einmal; `Speicherfehler` trägt nur Fehler außerhalb der Datenbankanweisung, und nach einem gescheiterten UPDATE folgt
kein INSERT mehr. `StelleTabellenSicher` setzt die `Vorsorgewarnung` zu Beginn zurück. Ein nicht lesbarer Tarif gilt
als nicht aktiv. Ohne
Fehler steht keine dieser Zeilen; die dreizehn Basisprojekte sind Zeile für Zeile unverändert.

## 3.10 Rechenreihenfolge

Die Ordnung ist zwingend — Prozentbezüge und fortgeschriebene Restkontingente hängen daran.

```
 1. Investition Runde 1     direkte Arten, VALERI-Vorrang, Menge frisch vor Konserve
 2. Runde 2                 % der Erzeugerkosten (Basis: Hauptpositionen der Komponente)
 3. Runde 3                 % der Investition, stufig Anlage → Komponente → Projekt
 4. Zuschussabzug           NACH der Positionsschleife — Ersatz und Restwert bleiben brutto
 5. Simulationslauf         Endenergie aus dem jüngsten Lauf (höchste Tab_Ergebnis.ID)
 6. Energiekosten           vor der Betriebsseite und vor dem Kapitalwert (R-3)
 7. Betriebskosten          InvestSummeFuer greift auf Kategorie 1 zu; % der Brennstoff- und
                            Stromkosten aus den Arbeitskosten des Laufs (B‑4)
 8. CO₂ / BEHG              nach den Trägermengen, Preis je Kalenderjahr
 9. Vergütungen             Prüfkette (Fristende der Inbetriebnahme aus dem Katalog) → Satz (marginal)
                            → Hilfsstrom-Netting → Anteile → Fall 2: Kürzung auf den KWK-Strom,
                            zuerst an der Einspeisung → Bonus_voll
                            → Jahresreihe mit Vbh (brutto, W_a ÷ P_Nenn)/Deckel/Restkontingent
                              (Rest −= Vergütet)
10. Steuern                 je Betrachtungsjahr, anlagenscharf, § 54-Sockel einmal je Lauf;
                            Mischlage § 53/§ 53a neben § 54 ⇒ § 54 = 0 (S‑2);
                            Vorschau je Anlage und Wahl auf einer Kopie der Eingabe
11. Kapitalwert             A_t/E_t, Abzinsung, Ersatz und Restwert je Kennzeichen der Position,
                            Index-0-Einmalzahlung; danach Kennzahlen und Sensitivität
12. Kohärenzprüfung         zuletzt, liest gebuchte Jahr-1-Werte; gescheiterte Stufen als Warnzeile (B‑6)
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
10 % zu viel CO₂ (Umrechnung 3,2508 GJ/MWh). Die Grenzwertprüfung des § 9 Abs. 1 Nr. 3 StromStG
liest den brennwertbezogenen Faktor (§ 3.8); die Katalogzeile der Umrechnung
(`EF_BILANZ_EBEV_UMRECHNUNG_HO`) hat keinen Leser, umgerechnet wird über H_i/H_s des Trägers. Für
Träger ohne gesetzliche Festlegung gilt das
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
| ✔ **I-2** | Abgeleitete Bemessung ohne Satz ⇒ 0 €, nicht der erfasste Betrag. **Entscheid (→ Register R‑EZ, EZ‑1):** Ist die Ableitung nicht rechenbar, gilt der erfasste Betrag; eine ermittelte Menge 0 rechnet zu 0. |
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
| ✔ **B-4** | Zwei Arten nie frisch: `PROZENT_BRENNSTOFFKOSTEN`, `PROZENT_STROMKOSTEN` — `EUR_PRO_H` und die beiden `EUR_PRO_KWH_*` sind seit FX2 frisch. **Erledigt mit E7c2 (#446):** Beide beziehen ihre Bezugsgröße frisch aus dem jüngsten Lauf — die projektweiten Arbeitskosten des Brennstoffs bzw. des Netzbezugs (E7c2‑Q2, § 3.4); die Konserve nur, wo frisch nichts ermittelbar ist, mit Grund. |
| ✔ **B-5** | `InvestSummeFuer` summierte `EingegebenerWert`, abgeleitete Beträge fehlten. **Erledigt mit W5‑B‑8:** Basis ist die Investitionskaskade (`InvestKaskade.Summen`). |
| **B-6** | Fehler werden geschluckt (`catch {}` ⇒ still 0). **Für die fünf Prioritätsdateien erledigt mit E7c3 (#452):** In `KohaerenzPruefung`, `EmissionsBilanzRechner`, `Emissionsquelle`, `GesetzKatalog` und `WirtschaftlichkeitCtrl` sind alle 100 leeren Fänge benannt; 26 Lesestellen lesen über den strengen Weg `StilleDb.TabelleStreng`/`ScalarStreng`, weil `DataRepository` nie wirft; ein Fehler steht als Warnzeile an der Ergebniszeile oder als Grund in einer Eigenschaft (§ 3.9). **Offen:** 29 leere `catch` in 13 weiteren Dateien und fünf stille Lesestellen im Engine-Modus (E7c3‑Q5, → Register R‑E7c3). |
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
| ✔ **S-2** | Kein projektweites Doppelentlastungsverbot — Anlage A nach § 53 und Anlage B nach § 54 gleichzeitig möglich. **Erledigt mit E7c2 (#446):** Die Mischlage ist gesperrt (A3) — der § 54-Betrag wird verworfen, mit Begründung an der Zeile und einer Warnung der Kohärenzprüfung (§ 3.7, § 3.9). |
| S-1 · ✔ **S-3** · S-4 · **S-5** | Überholte Zeilennummern älterer Protokolle · ~~§ 9-Meldung nennt Kessel „(0 kW)"~~ **erledigt mit E2** (`SteuerAnlage.Klartext` nennt an einer Anlage ohne Stromerzeugung keine elektrische Leistung) · €/GJ ohne Ho-Umrechnung (für Kohle konsistent, bleibt offen) · S-5: Der **Radius 4,5 km** bleibt Meldungstext (die Geometrie fehlt im Datenmodell); die **Erlaubnisschwelle 1.000 kW** hat mit E2 einen Leser — eine Hinweiszeile der Kohärenzprüfung ohne Rechenwerk. |
| ✔ **S-6** | `STROMST_REDUZIERT_SATZ` ungesät. **Erledigt:** Der Satz ist gesät (`GesetzKatalog.cs:1156`, Generation 7) und wird gelesen (`EnergietraegerHuelle.cs:659`); die Konstante in `StrompreisZerlegungModel` ist nur noch wertgleiche Rückfallebene. |
| ✔ **K-1** | Der zweite Fall des § 2 Nr. 16 KWKG fehlte (§ 3.6): Bei Anlagen mit Vorrichtung zur Abwärmeabfuhr ist KWK-Strom `Nutzwärme × Stromkennzahl`, nicht die Nettostromerzeugung. **Erledigt mit E7c1 (#440):** Kennzeichen `KWKG_Abwaermeabfuhr` und Stromkennzahl `KWKG_Stromkennzahl` je Anlage (Schemaschritt 105), Fall 2 `min(Netto, Nutzwärme × σ)` auf Regel- und Ersatzweg mit Herleitung je Anlage, gepflegt in der Überlagerung „Sätze und Herkunft"; kein Basisprojekt betroffen. |
| ✔ **V-3** | Die **PV-Reihe** hatte in der Mehrjahrestabelle keine eigene Spalte — sie wirkte nur in „Netto". **Erledigt mit E2:** `Mehrjahresbild.Baue` nimmt `ErloesReihe.PV_VERGUETUNG` als Spalte auf (`WIRT_REIHE_PV`). Damit stimmt die Selbstprüfung „Summe der Positionsspalten = Netto nominal" auch dort, wo ein Projekt den Vergütungsdialog führt; die **KWKG-Pauschale** hat ihre Spalte seit U17, und die Zeile 0 geht ebenfalls auf. |
| ✔ **V-1** · ✔ **V-2** · V-4 | EV-Rundung (EvMix unrundet, Erlös gerundet) · § 51a bewertet mit AW statt EV · Eigen/Einspeise-Split je Anlage ist benannte Näherung. **V‑1 und V‑2 erledigt mit E7c2 (#446):** Der EV-Mix rechnet ungerundet, gerundet wird allein der Erlös; § 51a bewertet bei fester Vergütung mit der Einspeisevergütung (A4, § 3.6). V‑4 bleibt die benannte Näherung. |
| R-1 · R-2 · R-3 | Rahmenparameter je Stammprojekt, nicht je Variante · Hilfsenergie steigt mit p_B statt p_E (bei gleichen Sätzen null) · ohne bestimmbare Energiekosten kein Kapitalwert (Absicht). |

---

# 5 Entscheidungen K1–K11 und rechtliche Unsicherheiten

**Die Entscheide dieses Abschnitts stehen mit Frage, Wortlaut, Datum und Stand im Register:** K1–K11
unter R‑K; die Entscheidungen zur Darstellung (D-1, E-1, D-2, D-3), zum Energieträger-Dialog (ET-D-1
bis ET-D-3, UR-1), zum Elektroheizkessel (E1) und zum Einheitenbruch der Gase (U-1) unter R‑D. Was
aus ihnen als Regel gilt, steht an dem Ort, den das Register in der Spalte „Ort der Regel" nennt;
hier bleiben die Zuordnung des Elektrokessels, die Prüfung auf Doppelbepreisung, der Stand des
Einheitenbruchs und die rechtlichen Unsicherheiten. Der Rest zu ET-D-3 (U32) ist erledigt mit E7c2
(#446): Die Preisbasis steht als eigener Kartenzustand in `energy_project_settings.Preisbasis`
(Schemaschritt 112, § 2.5).

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

Der Einheitenbruch der Gase (U-1, → Register R‑D) betrifft die Wirtschaftlichkeitsrechnung nicht; behoben
ist er mit einem eigenen Schemaschritt:

**Der Einheitenbruch ist behoben** (U‑1 Weg (a), freigegeben mit A9, → Register R‑A; umgesetzt #446).
Schemaschritt **113** (`SCHRITT_113_GASE_NM3`, reines DML nach dem Muster von Schritt 26a) zieht
`Tab_Brennstoff_Stamm.Einheit` der fünf Gase (Brennstoffe 1, 2, 3, 14, 25) von „m³" auf „Nm³" und
`PreisEinheit` auf „€/Nm³", dazu jede Preiszeile ihrer Träger, die noch „m³" führt; damit gleicht der
Stammtext `energy_carrier.billing_unit`, und die nächste Zuordnung eines Gasträgers findet ihre
Identitätsregel. Ergebnisneutral: Kein Rechenweg liest den Stammtext. Der Vorlagenbau läuft danach ohne
Auffälligkeit. Der Brennstoff 24 „Sonstige" führt im selben Schritt „kWh" und „€/kWh" (E7c2‑Q4, Anwender,
→ Register R‑E7c2); eine Preisumrechnung nimmt der Schritt nicht vor, Träger und Preise des Brennstoffs bleiben und
werden im Migrationsprotokoll gezählt. Sein Stamm trägt H_i = H_s = 1,0 wie Strom (13) und Fernwärme (23) —
nachgepflegt von der Katalog-Generation 9, nur wo beide noch 0 oder leer waren (§ 3.6; umgesetzt #452).

**Die Randfragen des Einheitenbruch-Konzepts sind hiermit hierher übernommen** und gelten von hier
aus: Waisenheilung (`energy_project_settings` Zeile 10076, Projekt 1039), kg-/rm-Abrechnung der
Brennstoffe 4/5/12, Brennstoff 24 „Sonstige" (die Einheit ist mit Schritt 113 entschieden, H_i = H_s = 1,0 wie
bei Strom und Fernwärme mit der Katalog-Generation 9, #452), der Fremdkörper Regel 67 und ein Prüfschritt in
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
| R-U5 | keine Nachfolgeregelung nach 2030 | Förderzeitraum als Datumsparameter im Katalog, nicht als Konstante — **umgesetzt #440**: das Ende der Frist zur Inbetriebnahme steht als `KWKG_INBETRIEBNAHME_FRISTENDE` (31.12.2030) im Gesetzeskatalog, `KWKG_REALISIERUNG_JAHRE` ist entfallen (§ 3.6); die zwei Katalogzeilen der alten Frist tragen den Status ABGEKUENDIGT (Katalog-Generation 9, #452) |
| **R-U6** | **Auslösedauer des 45-Euro-Mechanismus im ETS 2** — zwei oder drei aufeinanderfolgende Monate; die Sekundärquellen widersprechen sich (Grundlagen § 10 Nr. 1) | nicht übernommen; der CO₂-Preispfad (§ 3.11) baut auf dieser Größe auf und führt sie als Stützstelle mit Status, nicht als Konstante |
| **R-U7** | **Exakter Wortlaut des § 10 Abs. 3 BEHG** (Bezugspreis 2027), nur sekundär gesichert; die Zuordnung „Berechtigungen" gegen „Emissionszertifikate" ist auch fachlich strittig (Grundlagen § 10 Nr. 2) | entschärft, falls das Dritte Änderungsgesetz den Korridor festschreibt; bis dahin Katalogstatus VORLÄUFIG |
| **R-U8** | **Zahlenreihe des Projektionsberichts 2026**, nur sekundär belegt (Grundlagen § 10 Nr. 3) | vor Verwendung als Vorbelegung des Preispfads (§ 3.11) zu prüfen |
| **R-U9** | **Enddatum der Versteigerungsphase 2026** — Restmengenrechnung gegen Sekundärquelle 09.09.2026 (Grundlagen § 10 Nr. 4) | betrifft den Übergang Festpreis → Versteigerung im Preispfad (§ 3.11) |

---

# 6 Umsetzungsstand

## 6.1 Abgeschlossene Etappen

*Kurztafel: je Etappe ein Satz und die Statusnummer. Die ausführlichen Zeilen — Inhalt und
Ergebniswirkung im Wortlaut — stehen im Protokoll der Entscheidwege (§ 1 bis § 6 dort, je Etappe);
was an einer Etappe offen blieb, steht in § 6.3.*

| Etappe | Inhalt | Statusnummer |
|---|---|---|
| **W4 E1–E8, L12/L13** | Gesetzeskatalog, Vollbenutzungsstunden, Bemessungsarten, Steuergutschriften, Tarif-Rollenmodell, KWKG je Modul und Bericht. | vor #300 |
| **K1–K6** | Alttabellen, kWh-Konsistenz, Einheiten-Seeds, Kostenprofil, Komponenten und Zuschuss, KWKG und Steuern einheitenrichtig. | vor #300 |
| **KD1–KD6** | Vorlagentabellen, Komponenten-Kostendialog, Übernahme, Energieträgerverwaltung und Ertrag/Bonus. | vor #300 |
| **P1–P6** | EEG-Satzrechner, Monatsmarktwerte, § 51/§ 51a, PV-Dialog und Kennzahlen. | vor #300 |
| **H1–H4b, H21** | Pflichtpositionen, Endenergie-Bemessung, Bezugsgrößen-Auflöser, Drei-Runden-Kaskade und Mengenausweis „frisch vor Konserve". | vor #300 |
| **B1** | Zahlenprobe, die die Doppelzählung § 9 Nr. 3 bestätigt. | vor #300 |
| **B2** | Schema 60: Preisbestandteile des Brennstoffs und Kohärenzprüfung. | vor #300 |
| **B3a** | Schema 61: Steuerwahl je Anlage, § 54 auf Kesselbrennstoff. | vor #300 |
| **B3b** | `HilfsstromRechner`, Netting nur in der KWKG-Reihe. | vor #300 |
| **B4** | Stromsteuer-Schnellwahl katalogbasiert, die Unternehmensart hebt hervor. | vor #300 |
| **BK1/BK2** | Trägerzuordnung über `code`, Wizard-Automatik und Emissionsspalten — mit gewollter Wirkung auf die CO₂-Bilanz. | ohne Statuszeile |
| **HB1** | Anzeigesortierung; das Hydraulikbild liest `Z_AnlageSenke`. | ohne Statuszeile |
| **B7** | Erlösrubrik in Reiter, Word, Excel und BHKW-Vorschau, Energiekosten je Anlage, eine Emissionsspalte nach Modus. | #329 |
| **BK1** | Der KWK-Zuschlag gehört der Anlage (Schemaschritt 89). | #330 |
| **BK1a** | Ersatzweg über eine leistungsgewichtete virtuelle Gesamtanlage; Schemaschritt 90 entfernt die sechs KWKG-Projektspalten. | #335 |
| **BK1b** | Die Projektspalte `KWKG_Kostenanteil` fällt (Schemaschritt 91). | #336 |
| **VG** (§ 2.9, § 2.15) | Wählbares Vergleichsprojekt je Gruppe (Schemaschritt 92) und die Vergleichssicht mit zwei Ständen. | #358 |
| **B7P** | Die Nachweise eines Wirtschaftlichkeitslaufs reisen als Umschlag in `Nachweis_Json`. | #331 (anderer Rechner) |
| **B5** | Der BHKW-Wirtschaftlichkeitsdialog als Razor-Komponente mit acht Gruppen (§ 2.2). | #286 ff. |
| **B6** | § 9 Abs. 1 Nr. 3 StromStG als Ausweis statt als Erlös (Schemaschritt 88) — mit gewollter Wirkung auf den Kapitalwert. | #328 (anderer Rechner) |
| **B-1 / Kesselbrennstoff** | Die Kessel-Modulspalte `Verbrauch` trägt den Brennstoffeinsatz des Laufs — mit gewollter Wirkung und neuer Referenzbasis. | #331 (dieser Rechner), Basis #333 |
| **VV** (§ 2.16) | Vergütung je Variante: eigene Werte oder übernommen (Schemaschritt 93). | #359 |
| **Hilfsstrom am Endenergiebedarf** | Die Hilfsstrom-Positionen der Standardvorlagen bemessen sich am Endenergiebedarf (Schemaschritt 94) — mit gewollter Wirkung. | #365/#366 |
| **Nutzungsdauern S2** | Knopf „Nutzungsdauern vorbelegen…", Tafel „Ersatz und Restwert" und Positionsart als Klappliste. | #357 |
| **Übernahme aus der Kostenverwaltung** | „Aus Vorlage übernehmen…" öffnet den Katalogblock der Administration. | #363 |
| **Bezugsgrößen** | Bezugsgrößen der Betriebskosten aus dem gespeicherten Lauf, jeder Fehlgrund benannt — die Hilfsenergiekosten entstehen. | #364 |
| **E0 Papierpflege** | Papierpflege des Konzepts und der Nebenpapiere ohne Entscheid. | #379 |
| **E1 Nachweisfundament** | Ankertests und fünf Testklassen; Kaskadenrunde 2 reihenfolgeunabhängig. | #380 |
| **DL‑2e Knopfleisten** | Die Fußleisten der beiden Kostendialoge auf dem Hausstil. | #390 |
| **E2 Kleine Kernkorrekturen** | Ausweis- und Bedienkorrekturen ohne Rechenwirkung, Kohärenzzeilen im einen Zeilenkatalog. | #405 |
| **E3 Plattform** | Datenseite und Rechenaufruf der Wirtschaftlichkeit plattformfrei; iOS liefert „Berichte und Kosten". | #431 |
| **E4 Erlösrubrik und Steuerzeilen** | Energiesteuer in zwei Beträgen, Gründe je Position, Erlösrubrik nach Komponente innen. | #432 |
| **E5 Ergebnisansicht und V‑A** | Umschalter „Kennzahlen / ValERI-Bewertung" mit vier Abschnitten, Bandbreite, Empfehlungskarten, V‑A im Kern. | #434 |
| **A13 Konzeptschnitt** | Dieses Konzept in drei Papiere geschnitten: gültiger Stand, Entscheidungsregister, Protokoll der Entscheidwege. | #435 |
| **E6 Verlauf mit drei Szenarien** | Der Kapitalwertverlauf aller drei Szenarien als Abschnitt der Seite (Farbe = Variante, Strichart = Szenario), „Verlauf nach Excel…", Berichte mit einer Spaltengruppe je Szenario, Spannenbild der Bandbreite; der Knopf „Verlauf…" entfällt. | #436 |
| **E7a Rechenwirksame Lücken, Teil a** | CO₂-Grenzwert des § 9 Abs. 1 Nr. 3 StromStG brennwertbezogen mit Herleitung je Anlage (Nr. 29), Schemaschritt 102 und „(bitte wählen)" für die leere Anlagenart (Nr. 30, ohne die Kern-Regel), vermiedene Menge ohne jede Eigenerzeugung mit beiden Anlagen im Schlüssel (Nr. 32) — die dreizehn Basisprojekte wirtschaftlich unverändert. | #437 |
| **E7b Zeitzonentarif und Leistungspreis-Staffel** (Q11) | Kein Zeitzonentarif: die Strommatrix ohne Tarifzonen mit einer Jahreszeile je Projekt; Schemaschritt 104 übernimmt die Staffel an den Stromträger, löscht die Zonensätze und verwirft ihre gespeicherten Läufe; die zweistufige Leistungspreis-Staffel in der Kostenverwaltung, bemessen an der Viertelstundenspitze, mit Vorrang vor Leistungspreis und Saisonreihe; der Tarifdialog nur noch im Rollenmodell, der Knopf „Strombezug…" entfällt — die dreizehn Basisprojekte unverändert (kein Tarifsatz im Bestand). | #439 |
| **E7c1 K‑1 Fall 2, Förderende 2030, Anlagenart-Kohärenz** | Schemaschritt 105 (Kennzeichen „Vorrichtung zur Abwärmeabfuhr" und Stromkennzahl je Anlage) und der zweite Fall des § 2 Nr. 16 KWKG — `min(Netto, Nutzwärme × σ)` auf Regel- und Ersatzweg, σ gepflegt oder P_el ÷ P_th, Kürzung zuerst an der Einspeisung, gepflegt in der Überlagerung „Sätze und Herkunft"; das Ende der Frist zur Inbetriebnahme als Katalogdatum 31.12.2030 statt der festen vier Jahre, die Reihe bis zum Kontingentende; die Kohärenzzeilen „Stromkennzahl fehlt" und „Anlagenart fehlt" — die dreizehn Basisprojekte unverändert (kein Kennzeichen im Bestand, 1030 vor dem Fristende). | #440 |
| **E7c2 Schritte E, F, G, S‑2, V‑1/V‑2, B‑4 und die E7c1-Reste** | Schemaschritt 111 (Ersatz und Restwert je Position, entkoppelt, im Zeileneditor gepflegt), 112 (die Preisbasis als eigener Kartenzustand) und 113 (der Stammtext der Gase auf Nm³, Brennstoff 24 auf kWh); die Mischlage § 53/§ 53a neben § 54 gesperrt, mit Warnung; der EV-Mix ungerundet und § 51a bei fester Vergütung mit der Einspeisevergütung; die zwei Prozentarten der Brennstoff- und Stromkosten frisch aus dem Lauf; in Fall 2 die Vollbenutzungsstunden aus dem KWK-Strom (mit E7c3 zurückgebaut) und der Rundungsgrund in der Herleitung; die Überlagerung „Sätze und Herkunft" vollständig, KI-Feldkatalog und Berichtsspalten zu Fall 2 — die dreizehn Basisprojekte unverändert (9.195 von 9.195 Werten). | #446 |
| **E7c3 Vbh nach Definition, B‑6, Kapitalwert 1024, Vorschau** | Die Vollbenutzungsstunden als erzeugte Arbeit ÷ Nennleistung in beiden Fällen des § 2 Nr. 16 (E7c2/7 zurückgebaut); B‑6 in den fünf Prioritätsdateien mit strengem Leseweg und Warnzeilen; der Kapitalwert 1024 als Datenstand nachgerechnet, der Anker bleibt; die Energiesteuer-Vorschau je Wahl im Kern (Nachweisfassung 8) und die Wahlen der Überlagerung als Anzeigezeilen; `VpvCtKwh` ungerundet; Katalog-Generation 9 (Brennstoff 24 H_i = H_s = 1,0, zwei KWKG-Zeilen abgekündigt) — die dreizehn Basisprojekte unverändert (9.519 von 9.519 Werten). | #452 |
| **E8a ValERI-Ansicht vollständig** (V‑C) | Die fünf Blöcke der Darstellung „ValERI-Bewertung" vollständig: Block 2 „Zahlungsreihen" je Stand und Szenario mit dem Zahlungsstrombild, Block 4 mit Spannenbild und Verlauf (E6‑Q1); in „Woraus entsteht die Zahl?" die Gliederung mit Nominalsumme und Differenzspalte und das Brückenbild zur Kapitalwertdifferenz, Brücke und Zahlungsstrom auch im Wortbericht; in „Was ist angenommen?" die Tafel „Was daraus im Lauf wird" und die Fußzeile zur Herkunft der Annahmen — keine Rechenwirkung, kein Schemaschritt. | #454 |
| **E8b Formelmappe, Anhang E und Anhang D** (V‑D) | Der Tabellenbericht als Formelmappe in den Stufen 0 bis 3 — Parameterblock aus echten Zellen mit Namen, Mehrjahrestabellen und Kennzahlen des Erwartungsfalls in Formeln, bemessene Betriebskosten als Menge × Satz, der Δ%-Block als Zellbezug; EPOS trägt die Werte ein, Excel rechnet beim Öffnen neu; die Anhang-E-Checkliste als Abschlussseite beider Berichte und hinter einem Knopf der Ergebnisseite; die Gegenprobe an der Fallstudie des Anhangs D gegen den Rechenkern; die Fußzeile in der Knopfreihe — keine Rechenwirkung, kein Schemaschritt; E8 ist damit abgeschlossen. | #455 |
| **E8c Bemessungstexte und Gliederungsprobe** (E8b‑Q2, E8b‑Q3) | Die Betriebskostentabelle beider Berichte nennt jede Bemessungsart mit dem Text des Bemessungskatalogs, Herleitung und Formelmappe fragen denselben Faktor; die Probe der Gliederung hält nur die Positionen des ersten Jahres gegen die angesetzten Betriebskosten, eine Position mit späterem Startjahr trägt „ab Jahr X" (Nachweisfassung 9); der Kommentar zu U42 berichtigt — keine Rechenwirkung, kein Schemaschritt. | #460 |
| **E9a Szenarioabdeckung, Teil a** (V‑E, § 2.11.5) | Schemaschritt 116 (`Szen_Best/Worst_Zeitraum` in ganzen Jahren und `Szen_Best/Worst_Menge` in Prozent an `Tab_ProjektWirtschaftlichkeit`), 117 (`custom_price_work/base/power_best/_worst` an `energy_project_settings`) und 118 (`Einspeiseverguetung_Best/_Worst` und `Einspeiseverguetung_KWK_Best/_Worst` an `Tab_ProjektWirtschaftlichkeit`, `DvEntgelt_Best/_Worst` und `PpaPreis_Best/_Worst` an `Tab_ProjektPhotovoltaik`), reines DDL, 18 nullbare Spalten; der Kern liest die Paare — Zeitraum, Mengenfaktor, Trägerpreise, Einspeisevergütungen, DV-Entgelt und PPA-Preis je Szenario an je einer Stelle; Nachweiszeile, Parameterblock und Verlauf je Szenario — rechenwirksam je Pflege, ohne Pflege bitgleich (Anker, Referenzlauf 13/13); die Dialoge mit E9b. | #461 |
| **E9b Szenarioabdeckung, Teil b** (V‑E, § 2.11.5, § 2.11.7) | Die Pflege in den Dialogen: die Zeilen 8 (Betrachtungszeitraum) und 9 (Mengenänderung) der Szenariotafel, „Vorgaben" leert 18 Felder; der ±-Knopf — der `CaseEingabeDialog` als allgemeiner Baustein mit dem Knopf `SzenarioKnopf` — an Arbeits-, Grund- und Leistungspreis der Trägerkarte, an der Einspeisevergütung PV (Parameterdialog) und KWK (Dialog „BHKW-Wirtschaftlichkeit"), an DV-Entgelt und PPA-Preis (PV-Vergütungsdialog), mit Kohärenzzeilen und Warnzeichen ohne Erwartet-Preis; der Hinweistext `WIRT_SZEN_HINWEIS` entfällt, an seiner Stelle der Ausweis „n von m Parametern szenariert" (`SzenarioAbdeckung`) auf der Seite, in beiden Berichten und in Punkt 9 der Anhang-E-Checkliste — kein Schemaschritt, ohne Pflege keine Rechenwirkung (Anker, Referenzlauf 13/13); E9 ist damit abgeschlossen. | #462 |
| **E10 Nutzungsdauer S3 und Speicherflotte** (A7, A8, § 2.13 (3), § 3.4) | Schemaschritt 120 sät die Instandsetzungssätze der Nutzungsdauertabelle (fünf Standardzeilen, die Mitte der Vorlagenbereiche), der Dialog „Nutzungsdauern (AfA)" zeigt Instandsetzung und Wartung; die Sätze wirken nur über die ausdrückliche Vorbelegung — Übernahme einer Kostenvorlage oder „Sätze vorbelegen…" —, die Herkunft steht am Satz, in Herleitung und Formelmappe (Nachweisfassung 10); ein neuer Kesseleintrag in %/a übernimmt den Wartungssatz; die Flottenstudie rechnet den Restwert je Einheit linear aus der Nutzungsdauer, ohne eigenes Intervall aus der Batteriezeile, der feste Restwert der Einheit ist Altfeld; die Nutzungsdauer von Kessel und BHKW ist als Gerätedaten gekennzeichnet — ohne Zutun keine Rechenwirkung auf die Projektwirtschaftlichkeit (Anker unverändert, Referenzlauf 13/13 byte-gleich, keine neue Basis). | #463 |
| **E13 Checkliste Punkt 9, Fehlergründe, A8-Halbsatz** (E9b‑Q5, E7c3‑Q6, A8, § 2.11.2, § 3.9) | Punkt 9 der Anhang-E-Checkliste „erfüllt", sobald Günstig und Ungünstig gerechnet sind; Lade-, Speicher- und Vorsorgegrund des Kerns in der Statuszeile der Ergebnisseite und im Dialog BHKW-Wirtschaftlichkeit, je Grund einmal, einen Datenbankfehler meldet die Anwendung selbst; eine neue Speichervariante nimmt die Nutzungsdauer der Zeile „Stromspeicher · Batterie"; die Hilfe von Zeileneditor und Saisonreihe des Leistungspreises auf ihren Abschnitt der Seite Kosten — ohne Rechenwirkung (Anker unverändert, Referenzlauf 13/13 gegen R14 byte-gleich), kein Schemaschritt. | #474 |
| **E14 Formelmappe je Szenario** (V‑D, § 2.11.6; Befund 1 aus E9a) | Die Formelmappe rechnet die Stufen 1 und 2 für alle drei Szenarien: je Stand und Szenario eine Mehrjahrestabelle aus der Spalte des Szenarios im Parameterblock, bis zum längsten Zeitraum, jenseits von T_s leer über eine Schutzformel; Kennzahlen Günstig und Ungünstig, Zinsfuß („nicht eindeutig" als Text) und Bandbreite als Formeln; Punkt 11 der Anhang-E-Checkliste nennt alle drei Szenarien — ohne Rechenwirkung (Anker unverändert, Referenzlauf 13/13 gegen R14 byte-gleich, Wertfassung = Formelfassung in 16 Prüfgruppen), kein Schemaschritt; drei Fragen entschieden 24.09.2026, nach Empfehlung (→ Register R‑E14). | #477 |
| **E15 Risikomodul** (V‑G7 aus V‑E, § 2.11.2, § 2.11.5, § 2.11.6) | Schemaschritt 125 (`Risiko_Art` TEXT(10) leer/`ZINS`/`ABZUG`, `Risiko_Zinszuschlag` [%-Punkte], `Risiko_Verlust` [€ je Periode], `Risiko_Wahrscheinlichkeit` [%] an `Tab_ProjektWirtschaftlichkeit`, reines DDL, Doppelpflicht mit `SpalteSicher`); das Risiko nach DIN EN 17463, 6.5 und Anhang F wahlweise als Zinszuschlag in allen drei Szenarien oder als Zahlungsstromabzug R_loss × p_loss je Periode ab Jahr 1 für jeden Stand außer der Referenz, eine Stelle der Regeln (`RisikoModul`); die Gruppe „Risiko (DIN EN 17463, 6.5)" im Parameterdialog, Ausweis nur bei Pflege in Nachweiszeile, Annahmentafel, Deklaration, Checkliste Punkt 6, Gliederung (Bestandteil RISIKO), Mehrjahrestabelle und Formelmappe — Vorgabe aus, ohne Pflege bitgleich (Anker unverändert, Referenzlauf 13/13 gegen R14 byte-gleich), Testdatenbank 125; vier Fragen entschieden 24.09.2026, nach Empfehlung; Lesart c zu E15‑Q4 nicht beauftragt (→ Register R‑E15). | #478 |
| **E17 Nicht monetarisierbare Wirkungen** (V‑G11, § 2.11.2; V‑G12 Punkte 2b und 3b) | Schemaschritt 127 (nach 126, der Reparatur der Gebäude-Katalogsätze #485): die Tabelle `Tab_ProjektWirkung` (STRICT; `ID`, `ID_Projekt` mit Fremdschlüssel auf `Tab_Projekt`, Löschen und Ändern weitergegeben, `Sortierung`, `Kategorie` TEXT mit CHECK `ENERGIEFLUSS`/`FINANZIELL`/`SONSTIG`, `Beschreibung`, `Dauer` 1–3, `Wirkung_Organisation`, `Wirkung_Mitarbeiter`, `Wirkung_Umwelt` je 0–3, NULL = nicht beurteilt) mit dem Index `idx_ProjektWirkung_Projekt`; die Migration übernimmt einen gepflegten Freitext `Nicht_Monetaer` als eine Wirkung SONSTIG ohne Beurteilung — nur nicht leere Texte, nur Projekte ohne Wirkung, wiederholbar —, das Freitextfeld bleibt als Altfeld lesbar; Migration, Werkzeug und Testvorrichtung aus einer Quelle (`ProjektWirkungSchema`), Duplizieren und Projekttransfer nehmen die Tabelle am Schema mit. Die Beurteilung nach 8.2 (Dauer × stärkste Wirkung, 0 bis 9) als Anzeige an einer Stelle (`NichtMonetaereWirkungen`), `ProjektWirkungCtrl` lädt und ersetzt die Liste in einem Vorgang; der Baustein `WirkungenListe` im Bewertungsblock statt des Freitexts, die Tabelle in Wort- und Tabellenbericht, die Punkte 2b und 3b der Anhang-E-Checkliste — keine Rechenwirkung (Anker bitgleich, Referenzlauf 13/13 gegen R14 byte-gleich), Testdatenbank 127 (0 Freitexte übernommen); vier Fragen entschieden 24.09.2026, nach Empfehlung (→ Register R‑E17). | #479 |
| **E16 Wiederholperiode je Kostenposition** (V‑G3 aus V‑E, § 2.11.2, § 2.13 (3), § 3.1, § 3.4) | Schemaschritt 129 (nach 128, dem Heizkreis der Anlagenkopplung AK1, Welle 3): die Spalte `Wiederholperiode_a` (INTEGER, nullbar; leer, 0, 1 = jährlich) an `Tab_ProjektWerte` und `Tab_KostenVorlagePosition`, reines DDL, die Zahl allein in `WiederholperiodeSchema.SCHRITT`; eine Betriebsposition mit n ≥ 2 zahlt in s, s + n, … ≤ T (`KapitalwertRechner.ZahltImJahr`, eigene Liste `Wiederholposten` neben den Töpfen), fortgeschrieben mit p_B bzw. p_E; das Ganzzahlfeld „Zahlung alle: [n] Jahre" im Zeileneditor der Betriebsseite und in den Kostenvorlagen, mitgenommen von der Vorlagenübernahme; „alle n Jahre ab Jahr X" in der Betriebskostentabelle beider Berichte, Hilfsspalte je Topf in der Formelmappe, Nachweisumschlag Fassung 11 — ohne Pflege bitgleich (Anker unverändert, Referenzlauf 13/13 gegen R14 byte-gleich), A/B-Nachweis an 1030 gleich der Handrechnung, Testdatenbank 129; vier Fragen offen (→ Register R‑E16). | #484 |
| **E18 Restpunkte Stromsteuer** (§ 6.3 Nr. 14, 16, 18; § 6.5) | Die Wache hält die Rückfallebene der Stromsteuer (`StrompreisZerlegungModel`) gegen die älteste Katalogzeile in Saat und Testdatenbank, zwei tote Ressourcen gestrichen; der Dialog „BHKW-Wirtschaftlichkeit" zeigt unter der Unternehmensart den erfassten Stromsteueranteil mit Satzabgleich und Kohärenzzeile, nur Anzeige (§ 2.2, Gruppe 4); die fünf Rechenweg-Sortierungen tragen den Vermerk HB1-O1, Nr. 18 bleibt offen — keine Rechenwirkung (Anker unverändert, Referenzlauf 13/13 gegen R14 byte-gleich), kein Schemaschritt; sechs Fragen entschieden 24.09.2026, nach Empfehlung, die siebte ist der Restpunkt Nr. 33 (→ Register R‑E18). | #492 |
| **E19 Restpunkte Unternehmensart** (§ 6.3 Nr. 15, 33; § 2.4, § 3.8) | Nr. 15 ist durch die Schalentrennung überholt — die Hülle der Energieträgerverwaltung liest Bilanzjahr und Unternehmensart je Öffnung, die Wache `KatalogjahrJeOeffnungTests` hält es fest; ohne BHKW pflegt der Parameterdialog in der Gruppe Strom die Unternehmensart samt Anzeige des erfassten Stromsteueranteils und § 9b-Erklärzeile, mit BHKW bleibt der Dialog „BHKW-Wirtschaftlichkeit“ die Pflegestelle (§ 2.4); KI-Feld `unternehmensart` mit Sperre bei BHKW, Maskenwache 35; § 9b ohne BHKW erreichbar (§ 3.8) — keine Rechenwirkung (Anker unverändert, Referenzlauf 13/13 gegen R14 byte-gleich), kein Schemaschritt; sechs Fragen entschieden 25.09.2026, nach Empfehlung, Q4 b (→ Register R‑E19). | #498 |
| **E20 Wärmepumpe je kW elektrisch** (§ 6.3 Nr. 10; § 3.2) | Die Investitionskosten der Wärmepumpe lassen sich auch „je kW elektrisch“ bemessen; Bezugsgröße P_el = Ptherm ÷ COP am Normpunkt der Kennlinie (A2/W35, B0/W35, W10/W35, bei W35 interpoliert), ein gerechneter Zweig der Landkarte mit dem Schalter `investition` — nur Kategorie 1, die Betriebsseite bleibt unverändert (§ 3.2); im Bestand ohne Rechenwirkung (Anker unverändert, Referenzlauf 13/13 gegen R14 byte-gleich; A/B an 1024: 1.000 €/kW → 4.000 €), kein Schemaschritt; sieben Fragen entschieden 25.09.2026, nach Empfehlung a, E20‑Q6 offen beim Anwender (→ Register R‑E20). | #502 |
| **E22 Rechenweg-Sortierung nach der Regel „99“** (§ 6.3 Nr. 18; HB1-O1) | Die acht Rechenweg-Leser der Anlagen — die fünf Leser der Wärmepumpen-, Senken- und Pufferlisten und die drei Modul-Lader für Kessel, Solarthermie und BHKW — sortieren nach `Ladeordnung.SqlAnlagenprio` wie Hydraulikbild und Erzeugerkarten: gepflegte Priorität zuerst, eine Anlage ohne Priorität hinten; Wache `AnlagenprioRechenwegTests` — keine Rechenwirkung (Anker unverändert, alle Werte und Zeitreihen gleich), allein in 1042 tauschen die beiden Wärmepumpen ihren Modulindex, deshalb die neue Basis `2026-09-25_R16_Anlagenprio` (vierzehn Projekte); kein Schemaschritt; E22‑Q1 entschieden 25.09.2026, nach Empfehlung a (→ Register R‑E22). | #503 |
| **E23 Betriebskosten der Wärmepumpe ohne kWh** (§ 6.3 Nr. 10; § 3.2; E20‑Q6) | Die Betriebskosten der Wärmepumpe werden nicht je kWh bemessen: „je kWh elektrisch“ und „je kWh thermisch“ antworten an der Wärmepumpe GEWERK und stehen nicht in der Auswahl; eine Bestandszeile rechnet aus dem Lauf weiter und trägt an der Herleitung den Vermerk „Altbestand“ (§ 3.2, Fußnote ²); wählbar bleiben fester Jahresbetrag, Prozentbemessungen und je kW — im Bestand ohne Rechenwirkung (keine Zeile an der Wärmepumpe trägt eine kWh-Art; Anker unverändert, Referenzlauf 14/14 gegen R16 byte-gleich), kein Schemaschritt; sieben Fragen entschieden 25.09.2026, nach Empfehlung a (→ Register R‑E23). | #510 |
| **E26 PV-Ausweis und Strommatrix-Bedarf** (§ 3.6; § 6.3 Nr. 32, 34; Befunde N1, N3 aus E25) | Die Stromproduktion der Photovoltaik ist die Erzeugung der Module (Summe der Modulzeilen), der Eigenverbrauch des Ausweises Erzeugung − Einspeisung; der Bedarf der Strommatrix zählt alle Verbraucher des Anschlusses (Reihe `STROMBEDARF_GESAMT`), auch für den KWK-Split — „PV: vermiedener Bezug“ nicht mehr negativ, im Rollentarif vermiedene Menge und Kosten der Wärmepumpen-Projekte positiv (1040 −4.496 → +1.332 €/a); Kapitalwert an allen Ankern bitgleich; neue Basis `2026-09-25_R18_PvAusweis`, einzige Wirkung `Photovoltaik.Stromproduktion` in vier `aggregate.csv`; kein Schemaschritt; sieben Fragen entschieden 25.09.2026, nach Empfehlung (→ Register R‑E26). | #518 |
| **E27 Netzbezug nie negativ** (§ 3.6; § 6.3 Nr. 34, 36; Befund N5 aus E26) | Ein Stromüberschuss des BHKW, den keine spätere Stufe aufnimmt, steht allein im KWK-Split als Einspeisung; der Reststrom wird am Laufende bei 0 geklemmt (`SimulationControl.NetzbezugGeklemmt`, nicht bei der Speicherflotte), der Reststrombedarf der BHKW-Zeile je Stunde — 1018 Netzbezug −27,46 → 0 MWh, im Rollentarif keine Gutschrift der Reststromkosten mehr (−8.237,25 → 0 €/a), CO₂ +11,95 t/a; 1030 Kapitalwert Erwartet −31.141.242,71 → −31.142.971,06 € (Anker neu, E27‑Q2 a); neue Basis `2026-09-25_R19_BhkwNetzbezug`, Wirkung allein in 1018 und 1030 (4/432 CSV); kein Schemaschritt; acht Fragen entschieden 25.09.2026 (Q1, Q2 Anwender, Q3…Q8 nach Empfehlung, → Register R‑E27). | #521 |
| **E28 Prüfwelle N7: Strom-Stufeneingang geklemmt** (§ 3.6; § 6.3 Nr. 36; Befund N7 aus E27, Nebenbefund N8) | Der PV-Modus der Wärmepumpe reagiert nur auf PV-Überschuss (`SimulationControl.PvUeberschussVorab`: ein negativer Bedarf zählt je Stunde als 0, ein BHKW-Überschuss ist nie PV-Überschuss); der Strom-Stufeneingang der Kesselzeile (an allen drei Wegen) und der PV-Zeile wird je Stunde bei 0 geklemmt (`NetzbezugGeklemmt`), einheitlich mit E27‑Q4 — beide Stellen latent (keine Wärmepumpe im PV-Modus, kein Projekt mit BHKW und Photovoltaik), Kapitalwert 0 €, Referenzlauf 14/14 gegen R20 byte-gleich, keine Neueinfrierung, kein Anker wandert; Wache `StromStufeneingangKlemmeTests` (13 Fälle); kein Schemaschritt; fünf Fragen entschieden 26.09.2026 nach Empfehlung, Anlass Anwender „Prüfwelle ausführen“ (→ Register R‑E28). | #535 |
| **E29 Anzeige-Welle Stromausweis** (§ 3.6; § 6.3 Nr. 34, 36; E27‑Q3 b, E26‑Q6, N6; Befund N9) | Die BHKW-Einspeisung (= KWK-Split, `SimulationControl.BhkwEinspeisungStuendlich`) steht als Zeile „Stromeinspeisung“ im BHKW-Reiter (1018 27,46 MWh/a, 1030 0,39) und als Diagnosereihe `BHKW_UEBERSCHUSS` auch ohne PV und Flotte, der Excel-Monatsblock führt die Spalte „BHKW-Einspeisung“ (Messlatte `Bericht_Excel_1030` begründet neu); Strombilanz-Linie und Excel „Strombedarf“ lesen `STROMBEDARF_GESAMT ?? STROMBEDARF` (1040 8,0 → 27,4 MWh/a); die Übersicht zählt den Kältestrom der Stufenrechnung mit (N6); der PV-Deckungsgrad teilt durch den je Stunde geklemmten Bedarf; der Stromgang zeigt beim Heizkessel den Kesselstrom (N9: 1017 635,2 → 20,12 MWh, 1030 4.790,09 → 0) — reiner Ausweis, Referenzlauf 14/14 gegen R20 byte-gleich, keine Neueinfrierung, kein Anker wandert; Wache `BhkwEinspeisungAusweisTests` (21 Fälle); kein Schemaschritt; zwölf Fragen entschieden 26.09.2026 nach Empfehlung, Anlass Anwender (E27‑Q3 b, E26‑Q6/N6) (→ Register R‑E29). | #536 |
| **E30 Hilfsenergiekosten, BHKW-Stromdeckung, Datenpflege 1030/1026, Anker 1030** (§ 3.4, § 3.6; § 6.2; § 6.3 Nr. 21, 36; Befunde B3, B4, B5, B8 der Sichtprüfung P1030, N10 aus E29) | Trägt eine BHKW- oder Brennstoffkessel-Anlage einen Hilfsenergieanteil, rechnet ihre Pflichtzeile „Hilfsenergiekosten“ ohne eigenen Satz nach Weg B mit dem Anteil als Satz, sonst eine abgeleitete Zeile; eine gepflegte Position hat Vorrang (§ 3.4, `HilfsenergieAusAnteil`); die Stromdeckung des BHKW ist sein Eigenverbrauch am Strombedarf aller Verbraucher an allen fünf Stellen (N10); die Wartung von 1030 steht als fester Jahresbetrag in den Pflichtzeilen, fünf Hilfsenergiezeilen 1030/1026 auf „% des Endenergiebedarfs“ (Skript `datenpflege_1030_1026_betriebskosten.cs`, ergebnisneutral); der Kernanker 1030 heißt „gespeicherter Altlauf 212“, die Differenz −9.247.593,78 € zum Berichtsweg ist zerlegt — kein Kapitalwert-Anker bewegt, Messlatten Word/Excel 1030 begründet neu (zwei Positionen weniger), neue Basis `2026-09-26_R21_BhkwDeckung` (A/B gegen R20 429/432 byte-gleich, allein `BHKW.Strombedarfsdeckung` in 1017, 1024, 1047), Testdatenbank `40df1bf2`; kein Schemaschritt; zwölf Fragen entschieden 26.09.2026 nach Empfehlung, Anlass Anwender (B3, B4, B5, B8, N10) (→ Register R‑E30). | #548 |

## 6.2 Regressionsanker

Die Anker stehen seit **E1 (#380)** als Testklasse im Kern: `WirtschaftlichkeitAnkerTests` (10 Fälle)
fährt den Weg `LadeParameter → ErgebnisCtrl.Load → KostenEmissionRechner.Berechne →
WirtschaftlichkeitCtrl.Berechne` und hält damit die Größen dieses Papiers fest, ohne den
Berichtssammler (`BerichtsDatenSammler`, seit E3 in `EPOS.Kern/Allgemein/Bericht/`, umgesetzt #431)
nachzubauen. Dazu kommen die Rechnerklassen
`SteuerGutschriftRechnerTests` (39), `EegSatzRechnerTests` (49), `PvErloesRechnerEegTests` (23), die
Blattwache `BerichtBlattstrukturWacheTests` (13, über Excel- und Wortbericht samt den Stufen der Formelmappe), die
Formatwache `WirtZeileFormatWacheTests` (4), die Befundwache `FormelmappeClosedXmlBefundTests` (2), die Gegenprobe an
der Norm `AnhangDFallstudieTests` (10: die drei Sollwerte der Fallstudie, die Tafel D.5 und sechs Zeilen der Tafel
D.6 gegen `KapitalwertRechner.Rechne`, § 2.11.2) und die zwei Klassen der Betriebskostentabelle (§ 3.4, #460):
`BemessungstexteAlleArtenTests` (21, mit dem Wächter über alle Konstanten `DbWerte.BEMESSUNG_*` gegen den
Bemessungskatalog) und `BetriebskostenStartjahrGliederungTests` (12, die Probe mit den Positionen des ersten Jahres).
Die Szenarioabdeckung (§ 2.11.5, #461) hält `SzenarioParameterTests` (39: die 17 Fälle des W5‑B‑9-Satzes und 22 je
neuer Größe — Nullsemantik, 1e−9-Regel, Wirkungsrichtung, Speicherweg über drei Controller, auf 1030 der Zeitraum je
Szenario Zahl für Zahl wie ein Lauf mit diesem Zeitraum und die Menge ±10 %, die Staffel-Kohärenz, das Rollenmodell
und die Werkzeug-Wache über die 18 Spalten der Testdatenbank); E9a bewegt keinen Anker. Den Ausweis der
Szenarioabdeckung (#462) hält `SzenarioAbdeckungTests` (15: die Zählregel m und n, die Vorgabe zählt nicht, die
Nullregel, DV-Entgelt und PPA-Preis je Vergütungszeile, die Trägerpreise nach der einen Regel, der Satz in beiden
Sprachen, Punkt 9 der Checkliste, „Hinweistext weg, Ausweis da", die Zählung am Projekt 1030), die Trägerkarte
`EnergietraegerSzenarioHuelleTests` (6, Speichern und Laden in Nm³ und kWh); E9b bewegt keinen Anker. Die
Nutzungsdauer S3 (#463) halten `NutzungsdauerS3Tests` (16: Saat aus den Vorlagen, Zuordnung, Schritt 120 und seine
Wiederholbarkeit, Werkzeug-Wache, Vorrang des gepflegten Satzes, Herkunft nur bei Gleichheit, Vorlagenübernahme,
„Sätze vorbelegen" an 1018, Formelmappe, Kessel in %/a — die Tabelle rechnet nicht selbst),
`SpeicherFlottenNutzungsdauerTests` (9: linearer Restwert je Einheit, Altfeld, Intervall-Vorgabe aus der Batteriezeile,
1046 vorher/nachher) und `NutzungsdauerKennzeichnungTests` (4); E10 bewegt keinen Anker. E13 (#474) ergänzt
`AnhangEChecklisteTests` (Punkt 9 offen, teilweise, erfüllt; das Blatt der Mappe mit demselben Stand), `RobustheitB6Tests`
(+4 und der Datenbankfehler der Referenzwahl einmal) und `SpeichervarianteSicherstellenTests` (die Nutzungsdauer der
Tabelle für eine neue Variante); E13 bewegt keinen Anker. E14 (#477) ergänzt `BerichtBlattstrukturWacheTests` um
`Excel_E14_Kennzahlen_Guenstig_und_Unguenstig_rechnen_in_Formeln` und `Excel_E14_Zeitraum_je_Szenario_mit_Schutzformel`
(Zeiträume 25/20/15 a) samt den Ankerzeilen der Szenariotabellen und `AnhangEChecklisteTests` um
`Punkt_11_nennt_alle_drei_Szenarien_formelbasiert`; E14 bewegt keinen Anker. Das Risikomodul (#478) hält
`RisikoModulTests` (25: Schema 125 aus einer Quelle, Werkzeug-Wache der Testdatenbank, Normierung der Art, Vorgabe aus
bitgleich, Zuschlag gleich dem Lauf mit i + 1 in allen Szenarien, Abzug nur ab Jahr 1 und nicht auf den Restwert,
Differenzreihe risikobereinigt, der Abzug trifft die Variante und nicht die Referenz, Verlauf, Gliederung und
Mehrjahrestabelle, Speicherweg, Ausweis nur bei Pflege, Formelmappe), dazu fünf Dialogproben in
`WirtschaftlichkeitParameterDialogTests`; E15 bewegt keinen Anker. Die nicht monetarisierbaren Wirkungen (#479) hält
`NichtMonetaereWirkungenTests` (die Beurteilungsregel 8.2 als Tafel, Kurztext, Prüfung, Texte de/en, Kategorien gleich
dem CHECK der Tabelle; an der Testdatenbank Tabelle STRICT mit Fremdschlüssel und Index, Speichern und Laden, CHECK,
Projektduplikat, die Übernahme des Freitexts wiederholbar mit stehendem Altfeld, Werkzeug-Wache der einen Quelle und
der Anker „keine Rechenwirkung": Kapitalwert, Differenz, Annuität und Amortisation bitgleich mit und ohne Wirkungen),
dazu `WirkungenListeTests` (bUnit) und zwei Fälle der `BerichtBlattstrukturWacheTests` (Word-Abschnitt samt Tabelle,
Excel-Tafel samt Checkliste 2b/3b); E17 bewegt keinen Anker. Die Wiederholperiode (#484) hält `WiederholperiodeTests`
(31 Fälle: der Schritt an beiden Tabellen aus einer Quelle, die Zahlungsjahre als Tafel, die Normierung, jährlich
bitgleich, alle zwei Jahre und Startjahr 3 gleich der Handrechnung, der Rand des Zeitraums, Schreiben und Lesen,
Vorlagenübernahme, Umschlag, Herleitung, Gliederung und Berichte, Kapitalwert des Laufs, Formelmappe mit und ohne
Periode), dazu fünf Dialogproben in `VorlagenPositionDialogTests`; E16 bewegt keinen Anker — ohne Periode läuft der
Kern Zeichen für Zeichen den Weg von vorher.

| Anker | Wert | Herkunft |
|---|---|---|
| `LiesBetriebskosten(1024)` | **99,00 €/a** | gemessen = Konzept (#380) |
| Kapitalwert 1024 | **−2.896.359,13 €** | gemessen (#380), nachgerechnet #452: Datenstand — Schemaschritt 83 (#313) faltete 11,746 ct/kWh Strompreisanteile in den Arbeitspreis (−676.495,37 €), dazu +458,56 € aus der Übernahme Access → SQLite am 02.09.2026; der frühere Konzeptwert −2.220.322,32 € ist ersetzt (E7c3‑Q1, gebaut ist Lesart a) |
| Kapitalwert 1024 mit dem Strompreis vor Schritt 83 (35,000 ct/kWh) | **−2.219.863,76 €** | gemessen (#452, `Kapitalwert_1024_mit_dem_Strompreis_vor_Schritt_83`) — bitgleich mit B5 und FX1 bis FX4 |
| Kapitalwert 1030 (Kernweg über den **gespeicherten Altlauf 212** `Tab_Ergebnis.ID = 212` vom 30.08.2026 — vor B‑1 ohne Kesselbrennstoff, BHKW-Brennstoff vor R10) | **−21.895.377,28 €** | gemessen (#380), `WirtschaftlichkeitAnkerTests.Kapitalwert_des_gespeicherten_Altlaufs_212_ist_absolut_gepinnt` (`:323`, Wert `:338`); ein Pin des Altlaufs, nicht der fachliche Wert — die Differenz zum Berichtsweg zerlegt `KapitalwertAnkerZerlegungTests` (E30, #548; E30‑Q11 b, → Register R‑E30) |
| Kapitalwert 1030 (Weg über die Berichtsdaten, `BerichtsDatenSammler.Sammle`), Erwartet — **der fachliche Anker** | **−31.142.971,06 €** | gemessen (#521, E27‑Q2 a), `PvAusweisStromMatrixTests.cs:243`; die Differenz zum Altlauf 212 ist mit E30 (#548) geklärt (Befund B8): Energiekosten +447.807,10 €/a (Kesselbrennstoff 0 → 5.403,1 MWh +432.248 €/a, BHKW-Brennstoff 1.048,27 → 1.241,55 MWh +15.462 €/a, E27-Klemme +97,50 €/a) und CO₂-Abgabe +73.872,08 €/a, × 17,7267, KWKG +54,25 € → ΔKW **−9.247.593,78 €** (bis auf < 5 €); Lauf 212 auf einer Kopie neu gebucht ergibt −31.143.024,30 € (Rest KWKG-Split ohne Zeitreihen). Für 1024 gilt dasselbe (Lauf 199) |
| Betriebskosten 1030 (`BetriebskostenJahr`, Erwartet = Best = Worst) | **20.000,00 €/a** = 18.000,00 (Wartung BHKW-Kaskade) + 2.000,00 (Wartung Kessel) | gemessen (#380), `WirtschaftlichkeitAnkerTests.cs:341`; Sichtprüfung P1030 (26.09.2026): plausibel, Abweichung 0,00 €, laufunabhängig (§ 6.3 Nr. 21); seit der Datenpflege E30 (#548) als fester Jahresbetrag in den Pflichtzeilen `101600588`/`101600585`, gehalten in drei Szenarien von `DatenpflegeBetriebskosten1030Tests` |
| `LiesInvestitionen` 1018 / 1024 / 1042 | 45.312,50 · 12.001,00 · 13.000,00 | unverändert |
| Kaskadenregression 1042 | **±0,00 €** | gemessen (#380) — das Konzept führte **+20.927,61 €** |
| Vermiedene Kosten des Beispielprojekts über den Kernweg (Matrix, Tarifrechner, Verteilschlüssel) | **316.159,6 €/a** = 293.245,6 + 22.914,0 | gemessen (#437, `VermiedeneMengeOhneEigenerzeugungTests`) — vorher 293.245,6 €/a, allein das Blockheizkraftwerk; die übrigen Anker bewegt E7a nicht |
| Fallstudie DIN EN 17463, Anhang D (Rechenkern, BHKW gegen Kessel und Strombezug) | **64.479,51 €**; Worst **−202.801,57 €**, Best **598.319,65 €** | gemessen #455 (`AnhangDFallstudieTests`) — die Norm nennt 64.480 €, −202.802 € und 598.320 € (Toleranz ±1 €, § 2.11.2) |
| Referenzbasis | `Referenzlaeufe/2026-09-26_R22_Solarthermie` | Aufbau, Herleitung und Schemastand: [`Referenzlaeufe/LIESMICH.md`](../../../Referenzlaeufe/LIESMICH.md) |

**Zwei Abweichungen zum bisherigen Konzepttext, beide als Befund festgehalten (#380):** Die
Kaskadenprobe 1042 ergibt ±0,00 € statt +20.927,61 € — die drei Prozentzeilen des Projekts tragen im
heutigen Datenstand keinen Einheitpreis. Der Kapitalwert 1024 liegt mit −2.896.359,13 € um
**−676.036,81 €** unter dem früheren Konzeptwert; **nachgerechnet mit E7c3 (#452), kein Codefehler, beide Anteile
sind Datenstand:** −676.495,37 € aus Schemaschritt 83 (#313, 17.09.2026) — er hat die aktiven Strompreisanteile des
Stromträgers 60 (Modus „aufgeschlüsselt", 11,746 ct/kWh) in den Arbeitspreis gefaltet, 35,000 → 46,746 ct/kWh:
387,12 MWh Netzbezug × 0,11746 €/kWh = 45.471,12 €/a × 14,877475 (3 %, 20 a) —, dazu +458,56 € aus der Übernahme
Access → SQLite am 02.09.2026 (FX1). Die drei Kandidaten Kesselbrennstoff B‑1/#331, Hilfsstrom #365/#366 und
Schemaschritte 93–96 tragen 0,00 € bei: Mit 35,000 ct/kWh rechnet der Kern bitgleich −2.219.863,76 €, den Wert von
B5 und FX1 bis FX4. Der **gemessene** Wert bleibt der Anker (E7c3‑Q1, → Register R‑E7c3); die Etappen E7c1 (#440),
E7c2 (#446) und E7c3 (#452) haben keinen Anker bewegt.

1030 ist auf der **Investitionsseite verankert** (410.000,00 €, `InvestKaskadeTests.cs:359`) und seit
#380 auch im Kapitalwert und in den **Betriebskosten** (20.000,00 €/a, `WirtschaftlichkeitAnkerTests.cs:331`; der
frühere Satz „die Betriebskosten von 1030 tragen weiterhin keinen Anker“ ist mit der Sichtprüfung P1030 vom 26.09.2026
berichtigt, Befund B1). Den Kapitalwert von 1030 halten zwei Anker auf zwei Wegen (Tafel oben): der Pin des gespeicherten
Altlaufs 212 und der fachliche Anker über die Berichtsdaten; die Differenz ist mit E30 (#548) zerlegt (B8,
`KapitalwertAnkerZerlegungTests`), die Läufe 212 und 199 bleiben als Vorrichtungen weiterer Tests ungebucht (E30‑Q11 b). Die Projekte
1007, 1017, 1045, 1046 und 1047 führen in der Testdatenbank keinen gebuchten Ergebnisstand und keine
Kategorie‑1-Zeilen — ihre absoluten Anker fallen an, sobald die nächste Basis einen führt (#380).

## 6.3 Offene Punkte

*Die Nummern bleiben, wie sie vergeben wurden — auch die Sprünge (9 doppelt; 9a…9g, 9l, 9m, dann
9h…9k). Ein erledigter oder überholter Punkt wird als solcher gekennzeichnet, nicht entfernt und
nicht neu nummeriert, damit Verweise aus Protokollen und Statuszeilen weiter treffen. Erledigte Punkte stehen hier als Einzeiler; ihr Wortlaut mit dem Erledigt-Grund steht im Protokoll der Entscheidwege (Fundstellen dort in § 0.5).*

**B5-Kernaufgaben — alle drei erledigt**

1. ~~Schreibweg der drei B3a-Anlagenspalten — `KwkgAnlagenCtrl.Speichere` von 8 auf 11 Spalten (K7)~~ — erledigt, siehe Protokoll
2. ~~Live-Frisch-Anzeige der Bezugsgröße mit Herleitungszeile im Kostendialog~~ — erledigt mit U31 (#347) und #364, siehe Protokoll
3. ~~Erste Kostenposition mit Anlagenbezug entsteht erst hier~~ — erledigt mit B5 (#286 ff.), siehe Protokoll

**Nach B6 — alle sechs erledigt**

4. ~~§ 9 Nr. 3 als Ausweis (`Stromst_Befreiung_Modus`, Vorgabe AUSWEIS) — K3~~ — erledigt mit B6 (Schemaschritt 88, § 3.8), siehe Protokoll
5. ~~Doppelmeldung § 9b in `RechneAufschlaege` streichen~~ — erledigt (`RechneAufschlaege` entfallen), siehe Protokoll
6. ~~Hinweiszeile für Träger mit 1.000-kg-Satz bei Literabrechnung (`density` leer)~~ — erledigt (Fall 4a in § 3.9), siehe Protokoll
7. ~~Positive Nennung im Kohärenzfall 1~~ — erledigt (Bestätigungszeile in § 3.9), siehe Protokoll
8. ~~Lokalisierung `Views\Wirtschaftlichkeit` (63 Literale) und der Auflöser-Texte — K11~~ — erledigt (K11), siehe Protokoll
9. ~~Altkatalog-Bemessung `PROZENT_BRENNSTOFFKOSTEN` nachziehen — K10~~ — war schon erledigt (K10), siehe Protokoll

**Nach B7 — was die Etappe offen lässt**

9a. ~~**B7-1: A4 und A5 stehen in einer Zeile.**~~ — erledigt mit E4/1 (#432, U7), siehe Protokoll
9b. ~~**B7-2: `KwkgModulNachweis` und die Energiekosten je Anlage werden nicht persistiert.**~~ — erledigt mit B7P, siehe Protokoll
9c. ~~**B7-3: Die KWKG-Pauschale (§ 9 KWKG, A3) hat keine Rubrikzeile.**~~ — erledigt mit U17 (#346), siehe Protokoll
9d. ~~**B7-4: Der Grund einer Nullzeile ist aus den Ergebnisdaten abgeleitet, nicht vom Rechner
    durchgereicht.**~~ — erledigt mit E4/2 (#432), siehe Protokoll

**Nach BK1 — was die Etappe offen lässt**

9e. ~~**BK1-1: Sechs Projektspalten bleiben ungelesen stehen.**~~ — erledigt mit BK1a (Schemaschritt 90), siehe Protokoll
9f. ~~**BK1-2: Der projektweite Ersatzweg rechnet weiter mit den Projektsätzen.**~~ — erledigt mit BK1a, siehe Protokoll
9g. ~~**BK1-3: Ein Jahr-0-Ausweis der KWKG-Pauschale in der Erlösrubrik**~~ — erledigt mit U17 (#346), dieselbe Sache wie 9c, siehe Protokoll
9l. ~~**BK1-4: `KWKG_Kostenanteil` (Projekt) hat keinen Rechenleser mehr, das Feld bleibt.**~~ — erledigt mit BK1b (Schemaschritt 91), siehe Protokoll
9m. **BK1-Q2 (a) — abgenommen, hier als Ausnahme festgehalten:** Ein Projekt, dessen
    Vbh-Kontingent an Projekt UND Anlagen leer ist, rechnete auf dem Ersatzweg still mit dem
    Feldvorgabewert 30 000 h. Seit BK1a leitet `KontingentDerAnlage` daraus 0 h mit Begründung ab
    — dieselbe Antwort, die der Regelweg seit BK1 gibt. Wissentlich abgenommen.

**Aus der Anwenderdurchsicht der Ergebnisansicht (§ 2.13)**

9h. ~~**Nutzungsdauer, Ersatz, Restwert — zwei fehlende Stücke (Mockup-Anhang U39)**~~ — erledigt mit E10 (#463),
    siehe Protokoll; die Regel steht in § 2.13 (3); der Halbsatz aus A8 zur Speichervariante (Positionsarten 20/21)
    ist erledigt mit E13 (#474)
9i. ~~**Erlösrubrik nach Komponente innen:** Anlagenbezug je Katalogzeile, Block „projektweit",
    Eigenverbrauchsmengen je Anlage für die vermiedenen Kosten.~~ — erledigt mit E4/3 (#432, U6; Q15, A12), siehe Protokoll; offen bleibt Nr. 32
9j. ~~**Verlauf mit drei Szenarien**~~ — erledigt mit E6 (#436), siehe Protokoll
9k. ~~**Persistenz der Nachweise je Anlage** (Energiekosten-Unterzeilen nach dem Neuladen) — Teil
    von B7-2.~~ — erledigt mit B7P zusammen mit 9b, siehe Protokoll

**Fachlich und technisch**

10. ~~Bezugsgrößen der übrigen KD1-Bemessungsarten (H1-1b)~~ — erledigt mit E20 (#502) und E23 (#510): Investition der Wärmepumpe je kW thermisch und je kW elektrisch (§ 3.2, Fußnote ¹), Betrieb nicht je kWh — fester Jahresbetrag, Prozentbemessungen, je kW (§ 3.2, Fußnote ²; Anwender 25.09.2026, → Register R‑Rest, R‑E20, R‑E23), siehe Protokoll
11. ~~Nachzieh-Migration für Bestandsprojekte — durch die Auto-Anlage entschärft, bleibt Option~~ — geschlossen: nicht nachziehen (Anwender 25.09.2026, → Register R‑Rest), siehe Protokoll
12. ~~`InvestSummeFuer` auf die abgeleitete Kaskadensumme umbauen (B-5)~~ — erledigt mit W5‑B‑8, siehe Protokoll
13. ~~Pufferkapazität bleibt null — bewusste Grenze~~ — geschlossen: nur Volumen, die Grenze bestätigt (Anwender 25.09.2026, → Register R‑Rest), siehe Protokoll
14. ~~Reduzierter Stromsteuersatz bleibt Konstante bis zur Katalog-Nachpflege~~ — überholt; die Wache Konstante gegen Katalog erledigt mit E18 (#492), siehe Protokoll
15. ~~Bilanzjahr und Unternehmensart wirken erst beim nächsten Dialog-Öffnen~~ — überholt durch die Schalentrennung, Wache mit E19 (#498), siehe Protokoll
16. ~~Rückweg „Parameterdialog zeigt den erfassten Preisanteil" fehlt~~ — erledigt mit E18 (#492), siehe Protokoll
17. ~~Kohärenzzeilen nicht persistiert~~ — erledigt mit B7P, siehe Protokoll · Fall 4 ohne Katalogsatz bleibt still
18. ~~Engine-Sortierung `ORDER BY Prioritaet` (HB1-O1)~~ — erledigt mit E22 (#503): Rechenweg, Hydraulikbild und Erzeugerkarten folgen derselben Regel `Ladeordnung.SqlAnlagenprio` (Regel „99“), neue Basis R16 (Anwender 25.09.2026, → Register R‑Rest, R‑E22), siehe Protokoll
19. Asymmetrie „Wartung BHKW" gegen „Vollwartung / Wartung Kessel" — **dokumentiert mit E10 (#463)** (E10‑Q6, Lesart a,
    → Register R‑E10): Der Kessel führt seine Wartung je Katalogeintrag in €/a, €/kWh oder %/a, und ein neuer Eintrag
    in %/a übernimmt den Wartungssatz der Nutzungsdauertabelle; das BHKW führt sie fest in €/kWh el
    (`Wartungskosten_kwhel`) und bekommt keine Vorbelegung. Behoben ist die Asymmetrie nicht. **Belassen** —
    E10‑Q6 a bestätigt am 25.09.2026 (€/kWh el. ist beim BHKW die übliche Vertragsform; → Register R‑Rest)

**Aus Etappe E1 (#380) — geschlossen**

*Ohne eigene Nummer:* **R4 Kaskadenrunde 2** — erledigt mit E1 (#380), siehe Protokoll.

**Aus Etappe E2 (#405, 20.09.2026) — geschlossen**

25. ~~**CO₂ doppelt gebucht** (R5): aktiver CO₂-Bestandteil im Arbeitspreis und gebuchte
    BEHG-Reihe nebeneinander blieben unbemerkt.~~ — erledigt, siehe Protokoll
26. ~~**Kohärenzzeilen erreichen nur die Seite** (R6); der Strommix-Rückfall war ein Laufhinweis
    ohne seinen Wert.~~ — erledigt, siehe Protokoll
27. ~~**B-7** (`MengenEinheit` beschriftet jede Bezugsmenge mit „€") · **I-5** (uneinheitliche
    Vergleichsstrenge) · **S-3** („(0 kW)" am Kessel) · **S-5** (Erlaubnisschwelle ohne Leser) ·
    **V-3** (PV-Reihe ohne Spalte).~~ — alle fünf erledigt mit E2, siehe Protokoll
28. ~~**G7** (Zeitraumhinweis fehlt im Excel-Blatt) · **G8** (Bandbreite ohne Spalte „Spanne" und
    ohne Referenzzeile) · **G9** (`WIRT_EMPF_KEINE` nennt „Stammprojekt" statt der gewählten
    Referenz).~~ — alle drei erledigt mit E2, siehe Protokoll
29. ~~**Hi/Ho am CO₂-Grenzwert** (R11)~~ — erledigt mit E7a (#437), siehe Protokoll; die Regel steht in § 3.8, der Entscheid („es gilt immer der Brennwert"): → Register R‑NR

**Aus der Mockup-Prüfung (Q11) — geschlossen**

*Ohne eigene Nummer:* ~~**Q11 — Zeitzonentarif HT/NT und Leistungspreis-Staffel**~~ — erledigt mit E7b (#439), siehe
Protokoll; die Regel steht in § 3.5 und § 2.5, die Entscheide: → Register R‑Q (Q11) und R‑E7b.

**Aus der Papierpflege E0 (#379) — Sachpunkte der Datenaufnahme, alle drei erledigt**

30. ~~**Sieben Energieanlagen trugen `KWKG_Anlagenart = ''`**~~ — erledigt mit E7a (#437) und E7c1 (#440), siehe Protokoll; die Regel steht in § 3.6 (Vbh-Kontingent) und § 3.9, die Entscheide: → Register R‑NR und R‑E7 (E7‑Q1); vor dem Ausrollen die Live-Datenbank prüfen (Schritt 102 trifft jede leere Zeichenkette)
31. ~~**`Nachweis_Json` ist in 0 von 78 Ergebniszeilen belegt.**~~ — erledigt mit E5 (#434), siehe Protokoll; Entscheid (kein Nachziehlauf): → Register R‑NR
32. ~~**Die vermiedene Bezugsmenge führt keinen PV-Eigenverbrauch** (Befund aus E4/3, Frage U6‑Q1)~~ — erledigt mit E7a (#437), siehe Protokoll; die Regel („ohne jede Eigenerzeugung") steht in § 3.6, der Entscheid: → Register R‑NR; seit E26 (#518) zählt der Bedarf alle Verbraucher des Anschlusses (Nr. 34)

**Aus Etappe E18 (#492) — geschlossen**

33. ~~**Unternehmensart ohne BHKW nicht pflegbar** (E18‑Q7, → Register R‑E18)~~ — erledigt mit E19 (#498): ohne BHKW im
    Parameterdialog, Gruppe Strom, samt Anzeige (§ 2.4); Grenze: das Bilanzjahr nur mit Brennstofferzeuger, sonst der
    Rückfall 2026 (E19‑Q4 b, → Register R‑E19), siehe Protokoll

**Aus Etappe E26 (#518) — erledigt, Restpunkte benannt**

34. ~~**PV-Ausweis: Stromproduktion und Bedarf der Strommatrix** (Befunde N1 und N3 aus E25)~~ — erledigt mit E26
    (#518): Stromproduktion = Erzeugung der Module, Eigenverbrauch = Erzeugung − Einspeisung, Bedarf der Strommatrix
    aller Verbraucher (`STROMBEDARF_GESAMT`) auch für den KWK-Split (§ 3.6), Basis `2026-09-25_R18_PvAusweis`
    (→ Register R‑E26), siehe Protokoll; benannt bleiben **Q6** (Strombilanz-Diagramm und Excel-Spalte
    „Strombedarf“ zeigen weiter den Projektbedarf), **N5** (1018 negativer Netzbezug, der Rest nach der Kaskade
    ohne Klemme — kapitalwertwirksam, Empfehlung eigene Welle E27) und **N6** (die Übersicht „Strombedarf mit
    Eigenverbrauch“ ohne Kältestrom); **N5 erledigt mit E27 (#521)**, Nr. 36; **Q6 und N6 erledigt mit E29 (#536)**
    (Anwenderentscheid 26.09.2026 „in einer kleinen Welle nachziehen“): Strombilanz-Linie und Excel „Strombedarf“ am
    Gesamtbedarf, die Übersicht mit dem Kältestrom der Stufenrechnung (§ 3.6, → Register R‑E29), siehe Protokoll

**Aus Etappe E25 (#519) — erledigt**

35. ~~**Kein Projekt der Testdatenbank führt eine PV-Anlage mit vollständigem Preissatz** (Befund aus E9a; E21‑Q9,
    bis dahin ohne eigene Nummer unter Nr. 24 geführt)~~ — erledigt mit E25 (#519): Prüfprojekt 1048 „PV mit Preisen“
    ohne Referenzrolle (Kopie von 1040, VDI 6007, 10,40 kWp, Strom- und Erdgaspreis, Parametersatz mit
    Einspeisevergütung, PV-Kostenpositionen), gehalten von `PvPreisProjektTests`; die Basis R18 bleibt (→ Register
    R‑E25, R‑E21), siehe Protokoll; benannt bleiben die an 1048 nicht angelegte Vergütungszeile und Tarifstruktur
    (DV-Entgelt, PPA und Rollentarif nur im Test) und die Wiederholung des Skripts nach jeder Neufassung der
    Testdatenbank ohne 1048

**Aus Etappe E27 (#521) — erledigt, Restpunkte benannt**

36. ~~**Negativer Netzbezug bei BHKW-Überschuss** (Befund N5 aus E26, Nr. 34)~~ — erledigt mit E27 (#521): der
    Reststrom wird am Laufende bei 0 geklemmt (`SimulationControl.NetzbezugGeklemmt`, nicht bei der Speicherflotte),
    der Reststrombedarf der BHKW-Zeile je Stunde; ein BHKW-Überschuss steht allein im KWK-Split als Einspeisung
    (§ 3.6) — keine Gutschrift der Reststromkosten im Rollentarif mehr, Kapitalwert-Anker 1030 neu gesetzt, Basis
    `2026-09-25_R19_BhkwNetzbezug` (→ Register R‑E27), siehe Protokoll; benannt bleiben **Q3 b** (eine eigene
    Ausweisgröße „BHKW-Einspeisung“ mit Diagnosereihe und Zeile im BHKW-Reiter), **N7** (der Vorab-Überschuss für
    den PV-Modus der Wärmepumpe liest den negativen Rest nach dem BHKW als PV-Überschuss; die Kessel-Vektorstufe
    hinter dem BHKW bekommt einen negativen Stromeingang — Empfehlung Prüfwelle E28 nach Anwenderentscheid) und
    **Q6** (gespeicherte Altergebnisse 1018 und 1031 heilen beim nächsten Lauf); **N7 geprüft und behoben mit E28
    (#535):** beide Stellen latent (keine Wärmepumpe im PV-Modus, kein Projekt mit BHKW und Photovoltaik, R20
    byte-gleich, Kapitalwert 0 €) — der PV-Modus reagiert nur auf PV-Überschuss, der Strom-Stufeneingang der
    Kesselzeile je Stunde geklemmt, N8 (Strombedarf der PV-Zeile) mit (§ 3.6, → Register R‑E28), siehe Protokoll;
    **Q3 b erledigt mit E29 (#536)** (Anwenderentscheid 26.09.2026): die BHKW-Einspeisung (= KWK-Split) als Zeile
    „Stromeinspeisung“ im BHKW-Reiter, als Diagnosereihe und im Excel-Monatsblock, dazu der PV-Deckungsgrad am geklemmten
    Bedarf und der Kesselstrom im Stromgang (N9) (§ 3.6, → Register R‑E29), siehe Protokoll; mit E29 neu benannt
    **N10** (Stromring, Stromtabelle und Word-Deckungstorte zählen beim BHKW die ganze Produktion samt Einspeisung als
    Deckung, `BHKW.Strombedarfsdeckung` teilt durch den Projekt- statt den Gesamtbedarf — bewegt R20; Anwenderentscheid
    26.09.2026: korrigieren; **N10 erledigt mit E30 (#548):** die Stromdeckung des BHKW ist sein Eigenverbrauch am
    Strombedarf aller Verbraucher an allen fünf Stellen, Basis R21 (§ 3.6, → Register R‑E30),
    siehe Protokoll) und **N11** (das Strombilanz-Diagramm stapelt die
    Flotten-Netzeinspeisung in der Deckung statt im Nebenbalken, 1046 0,895 MWh/a — **offen**, Anwenderentscheid)

**Nachweis und Betrieb**

20. ~~Zahlenprobe gegen die Altanwendung (A8, ≡ B9)~~ — entfällt (→ Register R‑NR), siehe Protokoll
21. ~~Basiswechsel der Referenzläufe entscheiden~~ — erledigt mit #333 und E1 (#380), siehe Protokoll (heute gilt die Basis `2026-09-26_R22_Solarthermie`); ~~**offen bleiben allein die Betriebskosten von 1030**~~ — Sichtprüfung 26.09.2026 (P1030): 20.000 €/a plausibel, 1030 ist Regressionsprojekt ohne vollständige VDI‑2067-Positionen; ~~Befunde B3/B5 (Doppelanlage der Wartung, Bemessungsart Hilfsenergie), B4 (Hilfsstrom fehlt), B8 (zwei Kapitalwert-Anker)~~ — **endgültig geschlossen mit E30 (#548)** (Anwenderentscheide 26.09.2026): B3/B5 Datenpflege (Wartung als fester Jahresbetrag in den Pflichtzeilen, Hilfsenergiezeilen auf „% des Endenergiebedarfs“, ergebnisneutral), B4 Hilfsenergiekosten aus dem Anlagenanteil (§ 3.4), B8 Anker geklärt (§ 6.2) (→ Register R‑Rest, R‑E30), siehe Protokoll
22. ~~Sichtabnahmen: Brennstoffblock (B2), Kosten-Seite (BK1), Stromsteuer-Hervorhebung (B4)~~ — abgenommen vom Anwender 26.09.2026 (→ Register R‑Rest)
23. ~~resx-Sammelnachtrag der Textschlüssel aus B3a, B3b, B4 und der F-Serie~~ — erledigt mit E21 (#506), siehe Protokoll
24. ~~Datenpflege: Projekt 1018 Kessel ohne Energieträger, Puffer ohne Temperaturpaar; WP-Kennlinie 1024 ohne HT-Stützstellen~~ — erledigt mit E24 (#514): 1018 und 1023 gepflegt (Kessel mit Energieträger „Erdgas E“, 1023 dazu Erdgas-Projektzeile und Preisstand), Basis `2026-09-25_R17_Datenpflege`; benannt bleiben der 1018-Puffer ohne Temperaturpaar, 1024 (kein Datenfehler), 1030 (Anker) und 1026 (Prüffall), siehe Protokoll

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

*Gestrichene Fallstricke: → Protokoll § 7.2.*

## 6.5 Doppelte Wahrheiten

> **Aufgelöst: der KWK-Zuschlag.** Die Wahrheit ist die Anlage (BK1, BK1a, BK1b; Entscheide
> → Register R‑BK, Weg → Protokoll § 3.9). `Tab_ProjektWirtschaftlichkeit` führt von den ursprünglich
> elf KWKG-Projektspalten noch vier: `KWKG_Abschlag_Negativ`, `KWKG_Pauschalmodus`, `KWKG_Stichtag`
> und `KWKG_Inbetriebnahme` — alle vier gelten wirklich projektweit.

*Aus KONTEXT § 9 — jede benannt und begründet. Neue Spalten und Novellen müssen beide Orte treffen.*

| Doppelung | Stand |
|---|---|
| Stromsteuersatz an zwei Orten — Katalog `STROMST_REGELSATZ` und `STROMST_REDUZIERT_SATZ` gegen die `const double` in `StrompreisZerlegungModel` | wertgleich und **gekoppelt durch zwei Wachen** (E18, #492): `StrompreisZerlegungTests` hält die Konstanten gegen die älteste Zeile je Schlüssel in der Saat (`GesetzKatalog.Vorbelegung`) und im Katalog der Testdatenbank (`Tab_Gesetzesparameter`: `STROMST_REGELSATZ`, `STROMST_REDUZIERT_SATZ`, dazu die drei Umlagen); rot nennt die Meldung die drei nachzuziehenden Orte. Eine Novelle ist eine spätere Jahreszeile und lässt die Rückfallebene stehen; die Konstante greift nur für Jahre vor 2026 oder ohne Katalogzeile (§ 6.3 Nr. 14) |
| „Energieintensiv" an drei Orten — Unternehmensart, Schnellwahl im Trägerdialog, Katalogsatz | seit B4 liest die Schnellwahl den Katalog und die Unternehmensart hebt den passenden Knopf hervor; umgekehrt zeigt der Dialog „BHKW-Wirtschaftlichkeit" den erfassten Stromsteueranteil gegen die gewählte Unternehmensart (E18, #492, § 2.2 Gruppe 4) — ein Hinweis ohne Sperre; ohne BHKW steht die Unternehmensart mit derselben Anzeige im Parameterdialog, Gruppe Strom (E19, #498, § 2.4) — je Projektlage eine Pflegestelle; gekoppelt ist weiterhin nichts |
| BHKW-Einspeisevergütung an **drei** Orten | aktiver Tarif (`Tab_ProjektTarif.Einsp_Arbeit`) · Projektparameter (`Einspeiseverguetung_KWK`) · Anlage (`KWKG_Satz_Einspeisung`); Vorrang eindeutig (aktiver Tarif schlägt Parameterwert). Der vierte Ort (`energy_project_settings.Verguetung_BHKW`) ist mit Schritt 84/85 entfallen |
| Zwei Migrationsmechanismen — `SchemaMigration` gegen Selbst-DDL in `WirtschaftlichkeitCtrl` | aufgelöst mit #501 (Cloud-Sitzung, 25.09.2026: „Ad-hoc-DDL der fünf Tabellen entfernt“) bis auf drei Ergebnisspalten: die fünf Tabellen stehen im Grundschema (STRICT, Fremdschlüssel auf `Tab_Projekt`), jede Eingabespalte allein in ihrem Schemaschritt; `StelleTabellenSicher` zieht nur noch `StromsteuerBefreiungModus`, `ErsatzBarwert` und `Nachweis_Json` nach, bis ein Schemaschritt sie führt (Wache `WirtschaftlichkeitCtrlTabellenTests`). Neue Eingabespalten gehören allein in den Schemaschritt |
| Zwei Lesewege auf die Kostenposition | der direkte Zugriff ist der Normalfall; daneben die gespeicherte **Sicht** `Abfrage_Kostenfaktoren` (`sql/schema/002_views.sql`) — kein Access-Artefakt mehr, sie liegt im Repo, kennt die neuen Spalten nicht und ließe sich erweitern; beim Tabellenumbau wird sie eigens behandelt (`ProjektWerteLoeschschutz`) |
| ~~Komponenten-IDs hart verdrahtet gegen dynamisch gelesen (`Form_Kosten` gegen `UcBkKosten`)~~ | **gegenstandslos** — beide Klassen gibt es nicht mehr: Die Unterscheidung liegt im Kern (`KostenVorlagenCtrl.IstErfassungsgruppe`), die Oberfläche in `EPOS.UI/Dialoge/Kosten/` |
| Vorrang Projekt vor Katalog in **zwei** Implementierungen | `KostenEmissionRechner`, `StromPreisCtrl`; dazu die Sicht `Abfrage_Energietraeger_Effektiv` (`sql/schema/002_views.sql:26`) |
| Nutzungsdauer an zwei Orten — `Tab_Nutzungsdauer` gegen die Gerätespalten `Tab_BHKW.Nutzungsdauer`, `Tab_Heizkessel.Nutzungsdauer` und `Tab_StromspeicherVariante.Nutzungsdauer`; dazu der feste Restwert je Flotteneinheit | **benannt, nicht gekoppelt** (A8, #463): Die Spalten von BHKW und Kessel heißen „Nutzungsdauer (Gerätedaten)" und rechnen nicht; die Speichervariante rechnet mit ihrer eigenen Spalte, eine neue Variante bekommt sie aus der Zeile „Stromspeicher · Batterie" (Halbsatz aus A8, #474); `RestwertEuro` der Flotteneinheit ist Altfeld, gerechnet wird linear aus dem Ersatzintervall (§ 2.13 (3)) |
| ~~Die gespeicherte Access-Abfrage kennt die neuen Spalten nicht~~ | **überholt**: Access ist abgelöst; die gespeicherten Abfragen sind Altbestand des eingefrorenen Access-Zweigs |
| ~~Kennzahlenliste dreifach~~ | aufgelöst mit W4 E7 (vor #300; nicht E7 des Etappenplans) — `WirtschaftlichkeitZeilen` führt sie einmal |

---

# 7 Vorgeschlagene Reihenfolge

Die Reihenfolge der offenen Etappen steht im Etappenplan E0–E12 des Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md) § 5.
Gebaut sind daraus **E0 (#379), E1 (#380), E2 (#405), E3 (#431), E4 (#432), E5 (#434), E6
(#436), E7 Teil a (#437), E7 Teil b (#439), E7 Teil c1 (#440), E7 Teil c2 (#446), E7 Teil c3 (#452), E8 Teil
a (#454), E8 Teil b (#455), E9 Teil a (#461), E9 Teil b (#462) und E10 (#463)** — **E7, E8, E9 und E10 sind damit abgeschlossen**: von E8 die ValERI-Ansicht (V‑C) mit allen
fünf Blöcken samt Block 2 und dem Zahlungsstrombild U42, der Gliederung mit Nominalsumme und Differenzspalte, dem
Brückenbild, „Was daraus im Lauf wird", der Fußzeile und E6‑Q1 (§ 2.11.4), und V‑D — die Formelmappe in den Stufen 0
bis 3 (§ 2.11.6), die Anhang-E-Checkliste (U43) und die Anhang-D-Gegenprobe (§ 2.11.2), dazu die Fußzeile in der
Knopfreihe (E8a‑Q4); aus E8b ist die Nachbesserung **E8c (#460)** gebaut — die zwei kleinen Aufträge zu E8b‑Q2 und
E8b‑Q3, die Betriebskostentabelle der Berichte (§ 3.4); der Schnitt dieses Papiers (A13) ist mit **#435** ausgeführt.
**E9** (V‑E, die Szenarioabdeckung nach § 2.11.5) ist in zwei Wellen gebaut: **E9 Teil a (#461)** — die
Schemaschritte 116 (B, Betrachtungszeitraum und Mengenfaktor je Szenario), 117 (C, Trägerpreise best/worst) und 118
(D, Erlössätze best/worst) und der Kern, der die Paare liest, ohne Degradation (A5), rechenwirksam je Pflege — und
**E9 Teil b (#462)** — der ±-Knopf an Trägerpreisen und Erlössätzen, die Zeilen 8 und 9 der Szenariotafel und der
Ausweis „n von m Parametern szenariert" an der Stelle des Hinweistexts (§ 2.11.7). **E10** (Nutzungsdauer S3 und
Speicherflotte, #463) ist gebaut: die Instandsetzungs- und Wartungssätze der Nutzungsdauertabelle mit Schemaschritt
120, wirksam nur über die ausdrückliche Vorbelegung (§ 3.4, § 2.8), die Speicherflotte an der Tabelle und die
Kennzeichnung der geräteeigenen Spalten (§ 2.13 (3)); den eigenen Entscheid zu ND‑S3 vertreten die Fragen aus E10.
**E12** (Wiki-Runden) ist mit **#470** vorbereitet — der Sammel-Upload ist für den 26.09.2026 freigegeben (Version
1.2.0.4); E11 entfällt — damit ist der Etappenplan E0–E12 bis auf den Upload
abgearbeitet. **Alle Fragen der Etappen bis E10 sind entschieden:** die aus E7c3 (→ Register R‑E7c3), E8b (→ Register R‑E8b), E8c
(→ Register R‑E8c), E9a (→ Register R‑E9a), E9b (→ Register R‑E9b) und E10 (→ Register R‑E10; E10‑Q5 erledigt), zuletzt
am 24.09.2026 nach Empfehlung. Die zwei daraus offenen Bauten sind als kleine Bauwelle **E13 (#474)** gebaut —
Punkt 9 der Anhang-E-Checkliste (E9b‑Q5 b, § 2.11.2) und die Anzeige der drei Kerneigenschaften `Ladefehler`,
`Speicherfehler`, `Vorsorgewarnung` (E7c3‑Q6 a, § 3.9) —, dazu der Halbsatz aus A8 zur Speichervariante (§ 2.13 (3)).
Aus E7c3 bleibt der Rest von B‑6 (E7c3‑Q5 a: eine eigene kleine Etappe, nicht gebaut). Nach dem Befund 1 aus E9a ist
**E14 (#477)** gebaut — die Formelmappe rechnet die Stufen 1 und 2 für alle drei Szenarien (§ 2.11.6, V‑D ergänzt); ihre
drei Fragen sind am 24.09.2026 entschieden, nach Empfehlung, jeweils a (→ Register R‑E14). Die Lücke V‑G7 ist mit
**E15 (#478)** gebaut — das Risikomodul mit Schemaschritt 125, Zinszuschlag oder Zahlungsstromabzug, Vorgabe aus
(§ 2.11.2); ihre vier Fragen sind am 24.09.2026 entschieden, nach Empfehlung, jeweils a (→ Register R‑E15). Die Lücke
V‑G11 ist mit **E17 (#479)** gebaut — die nicht monetarisierbaren Wirkungen als Liste mit Kategorie und Beurteilung,
Schemaschritt 127, ohne Rechenwirkung (§ 2.11.2); ihre vier Fragen sind am 24.09.2026 entschieden, nach Empfehlung,
jeweils a (→ Register R‑E17). Die Lücke V‑G3 ist mit **E16 (#484)** gebaut — die Wiederholperiode je Betriebsposition
„alle n Jahre", Schemaschritt 129, ohne Pflege ergebnisneutral (§ 2.11.2); ihre vier Fragen sind offen, gebaut ist
jeweils die Empfehlung a (→ Register R‑E16). Damit ist die Gap-Tafel des § 2.11.2 geschlossen. Aus § 6.3 erledigt die
kleine Welle **E18 (#492)** Nr. 14 (die Wache Konstante gegen Katalog, § 6.5) und Nr. 16 (der erfasste
Stromsteueranteil im Dialog „BHKW-Wirtschaftlichkeit", § 2.2), ohne Schemaschritt und ohne Rechenwirkung; Nr. 18 ist
nachgemessen und bleibt offen, neu ist Nr. 33 (Unternehmensart ohne BHKW); sechs Fragen entschieden 24.09.2026, nach
Empfehlung (→ Register R‑E18). Die kleine Welle **E19 (#498)** schließt Nr. 15 als überholt (mit Wache) und erledigt
Nr. 33 — die Unternehmensart ohne BHKW im Parameterdialog (§ 2.4) —, ohne Schemaschritt und ohne Rechenwirkung; sechs
Fragen entschieden 25.09.2026, nach Empfehlung, Q4 b (→ Register R‑E19). Zu Nr. 10, 11, 13, 18 und 19 hat der Anwender
am 25.09.2026 entschieden (→ Register R‑Rest): Nr. 11 und 13 sind geschlossen, Nr. 19 bleibt dokumentiert, Nr. 10 ist
präzisiert — eine kleine Bauwelle folgt —, Nr. 18 ist gemessen (ohne Rechenwirkung) und wartet auf den Entscheid, ob
der Umbau mit der nächsten Neueinfrierung der Referenzbasis gebündelt wird. Die kleine Welle **E20 (#502)** baut
Nr. 10 — die Investitionskosten der Wärmepumpe auch je kW elektrisch, P_el am Normpunkt der Kennlinie (§ 3.2) —, ohne
Schemaschritt und im Bestand ohne Rechenwirkung; sieben Fragen entschieden 25.09.2026, nach Empfehlung, E20‑Q6 offen
beim Anwender (→ Register R‑E20). Die kleine Welle **E22 (#503)** erledigt Nr. 18 nach dem Anwenderentscheid vom
25.09.2026 („Nr. 18: so umsetzen“): Der Rechenweg ordnet die Anlagen nach der Regel des Hydraulikbilds, ohne
Schemaschritt und ohne Rechenwirkung; allein die Modulreihenfolge der Wärmepumpen in 1042 wechselt, deshalb die neue
Basis R16; E22‑Q1 entschieden 25.09.2026, nach Empfehlung a (→ Register R‑E22). Die kleine Welle **E23 (#510)** schließt
Nr. 10 nach dem Anwenderentscheid E20‑Q6 vom 25.09.2026 (b, erweitert auf Strom und Wärme): Die Betriebskosten der
Wärmepumpe werden nicht je kWh bemessen, eine Bestandszeile rechnet weiter und trägt den Vermerk „Altbestand“ (§ 3.2,
Fußnote ²) — ohne Schemaschritt und im Bestand ohne Rechenwirkung; sieben Fragen entschieden 25.09.2026, nach
Empfehlung a (→ Register R‑E23). Die Datenpflege **E24 (#514)** erledigt Nr. 24 nach dem Anwenderentscheid vom
25.09.2026 („nehme die Empfehlungen vor: für Später“): Die Kessel der Referenzprojekte 1018 und 1023 tragen den
Energieträger „Erdgas E“, 1023 rechnet mit der neuen Erdgas-Projektzeile in einer frischen Wirtschaftlichkeit erstmals
Energiekosten und Kapitalwert — ohne Schemaschritt; in der Simulation wechselt allein die Trägerkennung der Kessel in
zwei `aggregate.csv`, deshalb die neue Basis R17; sechs Fragen entschieden 25.09.2026, nach Empfehlung (→ Register
R‑E24). Die Welle **E26 (#518)** berichtigt nach dem Anwenderentscheid vom 25.09.2026 („Befunde aus E25:
Empfehlung/bearbeiten“) den PV-Ausweis und erledigt Nr. 34: Die Stromproduktion der Photovoltaik ist die Erzeugung
der Module, der Bedarf der Strommatrix zählt alle Verbraucher des Anschlusses (§ 3.6) — ohne Schemaschritt, der
Kapitalwert an allen Ankern gleich; allein `Photovoltaik.Stromproduktion` in vier `aggregate.csv` wechselt, deshalb die
neue Basis R18; sieben Fragen entschieden 25.09.2026, nach Empfehlung (→ Register R‑E26); benannt bleiben Q6, N5 und
N6 (Nr. 34). Die Welle **E25 (#519)** erledigt nach dem Anwenderentscheid vom 25.09.2026 („nehme die Empfehlungen
vor: für Später“, E21‑Q9 a) Nr. 35: Die Testdatenbank führt mit dem Prüfprojekt 1048 „PV mit Preisen“ erstmals eine
PV-Anlage mit vollständigem Preissatz — ohne Referenzrolle, ohne Schemaschritt und ohne neue Basis (R18 bleibt,
Referenzlauf 14/14 byte-gleich); `PvPreisProjektTests` hält Einspeiseerlös, Szenarien C und D, vermiedene Kosten,
Formelmappe und Kapitalwert-Anker; zehn Fragen entschieden 25.09.2026, alle a, nach Empfehlung (→ Register R‑E25).
Die Welle **E27 (#521)** erledigt nach dem Anwenderentscheid vom 25.09.2026 („E27: nach Empfehlung bauen“) den
Befund N5 aus Nr. 34 als Nr. 36: Der Netzbezug ist nie negativ — ein BHKW-Überschuss steht allein im KWK-Split als
Einspeisung, der Reststrom wird am Laufende bei 0 geklemmt (§ 3.6) —, ohne Schemaschritt; `aggregate.csv` und
`reststrom_viertelstunde.csv` von 1018 und 1030 wechseln, deshalb die neue Basis R19, und der Kapitalwert-Anker von
1030 ist neu gesetzt; acht Fragen entschieden 25.09.2026, E27‑Q1 und Q2 vom Anwender, Q3…Q8 nach Empfehlung
(→ Register R‑E27); benannt bleiben Q3 b, N7 und Q6 (Nr. 36).
Aus der
früheren Etappenreihe B5–B9 dieses Papiers ist nur noch B8 offen, und von B8 allein der Rest von B‑6; B9 entfällt:

| Etappe | Inhalt | Ergebniswirkung |
|---|---|---|
| **B8** | Die verbliebenen Befunde: **S-2** (kein projektweites Doppelentlastungsverbot) und **B-6** (geschluckte Fehler, `catch {}` ⇒ still 0). Der **PV-Teil von V-3** und **I-5** sind mit **E2 (#405)** erledigt. S‑2 ist mit **A3** entschieden (→ Register R‑A): **Sperre mit Begründungszeile**, nicht Warnung — **erledigt mit E7c2 (#446)** (§ 3.7, § 3.9). B‑6 ist für die fünf Prioritätsdateien **erledigt mit E7c3 (#452)** (§ 3.9, § 4); der Rest ist offen (E7c3‑Q5) | **ja** bei S-2 (gebaut, im Bestand ohne Wirkung); B-6 ist Robustheit |
| **B9** ≡ A8 | Zahlenprobe gegen die Altanwendung — **entfällt** (→ Register R‑NR, Nr. 20). Die Inventur der Mappen ([`Analyse_Altanwendung_BHKW-Plan.md`](../../ueberholt/Protokolle/Reporting/Analyse_Altanwendung_BHKW-Plan.md)) und die neun Abweichungen der Altanwendung (§ 5 der [Grundlagen](../Grundlagen_KWKG_Energiesteuer_Stromsteuer.md)) bleiben als Geschichte stehen; der Nachweis der Wirtschaftlichkeitsgrößen läuft über die Anker aus E1 (§ 6.2) und die A/B-Nachweise der rechenwirksamen Etappen | entfällt |

*Die Reihenfolge vor dem Schnitt samt ihrer Einordnung: → Protokoll § 7.3; die Wiederaufnahme vom
22.09.2026: → Protokoll § 6.1.*

## Entscheide vor der nächsten Codeetappe

Die geltende Fassung aller Entscheide — A1–A20, Q1–Q25 und die übrigen Familien — führt das
[Entscheidungsregister](Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md) (R‑A, R‑Q, …); dort
werden auch künftige Entscheide eingetragen. Die Tafel, die hier bis zum Schnitt stand (A1, A2, A5,
A11, A13), steht im Wortlaut im Protokoll § 5.5.

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
| **B8** | — | — | S-2 **#446**, B-6 **#452** (fünf Dateien; der Rest offen, E7c3‑Q5) | Befunde S-2 (≡ A3, erledigt) und B-6; V-3-Rest und I-5 mit **#405** erledigt (§ 7) |
| **B9** ≡ A8 | — | — | entfällt (Anwender 22.09.2026) | Zahlenprobe gegen die Altanwendung — BHKW-Plan-Mappen nicht relevant, E11 entfällt |
| **V-A…V-E** (§ 2.11.4) | W5‑B‑9…W5‑B‑12 | — | V-A = **#434**, V-C = **#454**, V-D = **#455**, V-E = **#461**/**#462** | ValERI: W5‑B‑9/10/11/12 gebaut (Schritte 71, 72); V-A = **E5** (gebaut), V-C = **E8** Teil a (gebaut #454; die Blöcke 1, 3, 4, 5 mit #434 vorgezogen), V-D = **E8** Teil b (gebaut #455), V-E = **E9** (Teil a im Kern gebaut #461, Teil b in den Dialogen gebaut #462); aus V-E das Risiko V‑G7 = **E15** (gebaut #478), V‑G3 = **E16** (gebaut #484); der Freitext aus W5‑B‑12 abgelöst durch V‑G11 = **E17** (gebaut #479) |
| § 2.13 Punkte (1)–(6) | — | — | #332, #346, #354, **#405**, **#434**, **#436**, **#454** | Ergebnisansicht; mit #405 Kennzahl-Reihenfolge und die Dialogkorrekturen; mit #434 Umschalter, vier Abschnitte, Karten, Bandbreite, Hinweistext und die Hinweiszeile aus (3); mit #436 der Verlauf mit drei Szenarien (5) und das Spannenbild; mit #454 beide auch in Block 4 (E6‑Q1) |
| § 6.3 Nr. 9h | — | **S2-Rest / U39 · S3** | **#357**, **#434** (Hinweiszeile), **#446** (Entkopplung, Schritt 111), **#452** (gemessen), **#463** (Gerätespalten gekennzeichnet, Speicherflotte angeschlossen) | Nutzungsdauer, Ersatz, Restwert; erledigt mit ND‑S3 (E10) |
| § 6.3 Nr. 29, 30, 32 | — | — | **#437** (Nr. 30 zum Teil, der Rest **#440**) | E7 Teil a: CO₂-Grenzwert brennwertbezogen, Schemaschritt 102 und „(bitte wählen)", vermiedene Menge ohne jede Eigenerzeugung |
| Q11 (§ 3.5, § 2.5) | — | — | **#439** | E7 Teil b: kein Zeitzonentarif, Schemaschritt 104, Leistungspreis-Staffel am Stromträger, Tarifdialog im Rollenmodell |
| Befund **K‑1** (§ 3.6) · A20 · § 6.3 Nr. 30 Kern-Regel | — | — | **#440** | E7 Teil c1: Schemaschritt 105 und der zweite Fall des § 2 Nr. 16, das Fristende der Inbetriebnahme 31.12.2030 als Katalogdatum, die Kohärenzzeilen „Stromkennzahl fehlt" und „Anlagenart fehlt" |
| Befunde **S‑2**, **V‑1**, **V‑2**, **B‑4** (§ 4) · Schritte **E**, **F**, **G** (A6, ET‑D‑3/U32, U‑1/A9) · E7c1‑Q1, Q2 b, Q7 | — | — | **#446** | E7 Teil c2: Schemaschritte 111, 112, 113, die Sperre der Mischlage, EV-Mix und § 51a, die Prozentarten frisch, Vbh aus dem KWK-Strom (zurückgebaut #452), die Überlagerung „Sätze und Herkunft" vollständig |
| Befund **B‑6** (§ 4) und § 6.2 Kapitalwert 1024 · E7c2‑Q5 b, Q8 b, Q4-Rest · E7c1‑Q2 b präzisiert, E7c1‑Q8 · U22 Anzeigezeilen · § 6.3 Nr. 9h gemessen | — | — | **#452** | E7 Teil c3: Vbh = W_a ÷ P_Nenn in beiden Fällen, B‑6 in den fünf Prioritätsdateien, Kapitalwert 1024 als Datenstand, die Energiesteuer-Vorschau je Wahl, die Wahlen als Anzeigezeilen, `VpvCtKwh` ungerundet, Katalog-Generation 9 |
| **V-C** (§ 2.11.4) · E6‑Q1 · E5b‑4 (U41, U46–U48) · U42 (E8a‑Q1) | — | — | **#454** | E8 Teil a: Block 2 mit Zahlungsstrombild, Block 4 mit Spannenbild und Verlauf, Gliederung mit Nominalsumme und Differenzspalte, Brückenbild, „Was daraus im Lauf wird", Fußzeile |
| **V-D** (§ 2.11.4) · V‑G10 · V‑G12 · Anhang D (§ 2.11.2) · Q18 (U43) · U12 · E8a‑Q4 | — | — | **#455** | E8 Teil b: Formelmappe Stufen 0 bis 3 mit Blattstruktur- und ClosedXML-Wache, Anhang-E-Checkliste in beiden Berichten und hinter dem Knopf der Ergebnisseite, Anhang-D-Gegenprobe, Fußzeile in der Knopfreihe |
| E8b‑Q2 · E8b‑Q3 (§ 3.4) · Kommentar zu U42 | — | — | **#460** | E8c: Bemessungstexte aus dem Bemessungskatalog, `Bemessungsfaktor` für Herleitung und Formelmappe, Gliederungsprobe mit den Positionen des ersten Jahres, „ab Jahr X", Nachweisfassung 9 |
| **V-E** Teil a (§ 2.11.4, § 2.11.5) · V‑G5 im Kern · Schritte **B**, **C**, **D** · E9a‑Q1…Q7 | Fortsetzung von W5‑B‑9 | — | **#461** | E9 Teil a: Schemaschritte 116, 117 und 118, der Kern liest Zeitraum, Mengenfaktor, Trägerpreise, Einspeisevergütungen, DV-Entgelt und PPA-Preis je Szenario; Nachweiszeile, Parameterblock und Verlauf je Szenario; Testdatenbank 118 |
| **V-E** Teil b (§ 2.11.5, § 2.11.7) · V‑G5 in den Dialogen · V‑4 · A14 (Hinweistext entfallen) · U10, U15 · E9b‑Q1…Q5 | Fortsetzung von W5‑B‑9 | — | **#462** | E9 Teil b: Zeilen 8 und 9 der Szenariotafel, ±-Knopf an Trägerpreisen und Erlössätzen (`CaseEingabeDialog` als Baustein), Ausweis „n von m Parametern szenariert" an der Stelle des Hinweistexts, Punkt 9 der Checkliste nennt ihn |
| A7 · A8 · § 3.4 · § 6.3 Nr. 9h und Nr. 19 · U39 · ND‑Q6 · ND‑Q7 · E10‑Q1…Q7 | — | **S3** | **#463** | E10: Schemaschritt 120 (Sätze der Nutzungsdauertabelle), die Vorbelegung über Kostenvorlage und „Sätze vorbelegen…" mit der Herkunft am Satz (Nachweisfassung 10), neue Kesseleinträge in %/a, die Speicherflotte mit linearem Restwert je Einheit, die Kennzeichnung „Nutzungsdauer (Gerätedaten)" |
| E9b‑Q5 b · V‑G12 Punkt 9 (U43) · E7c3‑Q6 a (§ 3.9) · A8-Halbsatz · zwei Hilfe-Anker aus E12 | — | — | **#474** | E13: Punkt 9 „erfüllt" mit beiden Szenarien, Lade-, Speicher- und Vorsorgegrund in Statuszeile und BHKW-Dialog, eine neue Speichervariante mit der Nutzungsdauer der Tabelle, `Form_VorlagenPosition` und `Form_LeistungspreisReihe` auf ihren Abschnitt der Seite Kosten |
| **V-D** ergänzt (§ 2.11.4, § 2.11.6) · V‑G10 · E8b‑Q1 abgelöst · Befund 1 aus E9a · V‑G12 Punkt 11 (U43) · U12 · E14‑Q1…Q3 | — | — | **#477** | E14: Stufe 1 und 2 der Formelmappe je Szenario — Mehrjahrestabellen Günstig und Ungünstig bis zum längsten Zeitraum mit Schutzformel, Kennzahlen, Zinsfuß und Bandbreite als Formeln, Punkt 11 „alle drei Szenarien formelbasiert" |
| **V‑G7** (§ 2.11.2) · V‑E (Risiko, § 2.11.4) · E9a‑Q6-Vermerk · V‑G12 Punkt 6 (U43) · E15‑Q1…Q4 | — | — | **#478** | E15: Schemaschritt 125, das Risikomodul — Zinszuschlag in allen drei Szenarien oder Zahlungsstromabzug R_loss × p_loss je Periode ab Jahr 1 für jeden Stand außer der Referenz; Gruppe „Risiko" im Parameterdialog, Ausweis nur bei Pflege, Bestandteil RISIKO, Risikozeilen der Formelmappe |
| **V‑G11** (§ 2.11.2) · W5‑B‑12 (Freitext abgelöst) · V‑G12 Punkte 2b und 3b (U43) · E17‑Q1…Q4 | Fortsetzung von W5‑B‑12 | — | **#479** | E17: Schemaschritt 127, die Tabelle `Tab_ProjektWirkung` mit Kategorie, Beschreibung, Dauer und drei Wirkungsgraden, Beurteilung Dauer × stärkste Wirkung (0 bis 9) als Anzeige; `WirkungenListe` im Bewertungsblock, Freitext als Altfeld, Tabelle in beiden Berichten, Checkliste 2b/3b erfüllt/teilweise/offen |
| **V‑G3** (§ 2.11.2) · V‑E (n-jährliche Zeitpunkte, § 2.11.4) · E9a‑Q6-Vermerk · § 2.13 (3) · E16‑Q1…Q4 | — | — | **#484** | E16: Schemaschritt 129, die Spalte `Wiederholperiode_a` an Projekt- und Vorlagenpositionen; Zahlung in s, s + n, … ≤ T (`ZahltImJahr`), nur Betriebspositionen; Feld „Zahlung alle: [n] Jahre" im Zeileneditor, „alle n Jahre ab Jahr X" in der Betriebskostentabelle, Hilfsspalte je Topf in der Formelmappe |
| § 6.3 Nr. 14, 16, 18 und 33 · § 6.5 (Stromsteuersatz) · B4 § 4 Grenzen 1 und 3 · HB1-O1 · E18‑Q1…Q7 | — | — | **#492** | E18: Wache der Stromsteuer-Rückfallebene gegen den Katalog (Saat und Testdatenbank, älteste Zeile), der erfasste Stromsteueranteil unter der Unternehmensart im Dialog „BHKW-Wirtschaftlichkeit" (Gruppe 4 und Überlagerung), HB1-O1 an den fünf Rechenweg-Sortierungen vermerkt; kein Schemaschritt |
| § 6.3 Nr. 15 und 33 · § 2.4 (Gruppe Strom) · § 3.8 (§ 9b) · B4 § 4 Grenze 2 · E18‑Q7 · E19‑Q1…Q6 | — | — | **#498** | E19: Nr. 15 überholt, Wache `KatalogjahrJeOeffnungTests`; die Unternehmensart ohne BHKW im Parameterdialog, Gruppe Strom, mit der Anzeige des erfassten Stromsteueranteils und der § 9b-Erklärzeile, KI-Feld mit Sperre; kein Schemaschritt |
| § 6.3 Nr. 10 · § 3.2 (Tafel der Runde 1) · H1-1b · H4a · H4b · R‑Rest Nr. 10 · E20‑Q1…Q8 | — | — | **#502** | E20: „je kW elektrisch“ an der Wärmepumpe nur bei den Investitionskosten, P_el = Ptherm ÷ COP am Normpunkt der Kennlinie, Schalter `investition` der Landkarte, Herleitung mit zwei neuen Schlüsseln; E20‑Q6 offen; kein Schemaschritt |
| § 6.3 Nr. 18 · HB1-O1 · R‑Rest Nr. 18 · E18‑Q3 · E22‑Q1 | — | — | **#503** | E22: die acht Rechenweg-Leser nach `Ladeordnung.SqlAnlagenprio` (Regel „99“), die Vermerke HB1-O1 entfernt, Wache `AnlagenprioRechenwegTests`; neue Basis `2026-09-25_R16_Anlagenprio`; kein Schemaschritt |
| § 6.3 Nr. 10 · § 3.2 (Tafel der Runde 1, Fußnote ²) · R‑Rest Nr. 10 · E20‑Q6 · E23‑Q1…Q7 | — | — | **#510** | E23: „je kWh elektrisch“ und „je kWh thermisch“ im Betriebsraster der Wärmepumpe gesperrt (GEWERK), eine Bestandszeile rechnet weiter mit dem Herleitungsvermerk „Altbestand“ (ein neuer Schlüssel); E23‑Q6 und Q7 entschieden a; kein Schemaschritt |
| § 3.6 · § 2.6 (Block B, Zeile B2) · § 6.3 Nr. 32 und Nr. 34 · E26‑Q1…Q7 | — | — | **#518** | E26: `Photovoltaik.Stromproduktion` = Erzeugung der Module, Eigenverbrauch des Ausweises = Erzeugung − Einspeisung, Reihe `STROMBEDARF_GESAMT` als Bedarf der Strommatrix und des KWK-Splits; neue Basis `2026-09-25_R18_PvAusweis`; kein Schemaschritt |
| § 3.6 · § 6.3 Nr. 34 (N5) und Nr. 36 · E26‑Q7 · E27‑Q1…Q8 | — | — | **#521** | E27: Klemme des Reststroms am Laufende (`SimulationControl.NetzbezugGeklemmt`, nicht bei der Speicherflotte), `BHKW.Reststrombedarf` je Stunde geklemmt, ein BHKW-Überschuss allein als Einspeisung im KWK-Split; Kapitalwert-Anker 1030 neu; neue Basis `2026-09-25_R19_BhkwNetzbezug`; kein Schemaschritt |
| — | — | **S1 · S2 · S3** | S1 vor #300, S2 = #357, S3 = **#463** (**E10**) | AfA-Tabelle |
| Mockup-Anhang **U1…U49** | — | — | #342 ff. | Umsetzungsstand je Bildstelle; **U1 = Befund K-1** (erledigt #440); U22 erledigt #446 und #452 (Anzeigezeilen), U32 erledigt #446, U39 erledigt #446 und #463 (Entkopplung; Gerätespalten und Speicherflotte), U41, U42 und U46 bis U49 erledigt #454, U12 und U43 erledigt #455 (Punkt 9 „erfüllt" #474; Günstig und Ungünstig in Formeln, Punkt 11 #477; Punkt 6 mit dem Risiko und die Risikozeilen der Mappe #478; Punkte 2b und 3b mit der Wirkungsliste #479), U15 erledigt #461/#462, U10 entfallen #462 |

**Die Etappenreihe E0–E12** (Analysepapier § 5) ordnet alles Offene dieses Papiers:

| Etappe | Gegenstand | Statuszeile |
|---|---|---|
| **E0** Papierpflege | Kopf, § 6.1/§ 6.3/§ 6.4/§ 6.5/§ 7 und die Nebenkonzepte; der **A13-Schnitt** folgte mit **#435** | **#379**; Nachpflege auf den Stand vom 22.09.2026 mit **E0c** |
| **E1** Nachweisfundament | Ankertests, fünf neue Testklassen, Kaskadenrunde 2 (R4) | **#380** |
| **E2** Kleine Kernkorrekturen | R5, R6, V-3, B-7, I-5, S-3, S-5, G7/G8/G9, Formel `N4`, P3 der Mockup-Prüfung | **#405** |
| **E3** Plattform | acht Schritte: vier nahtlose Hüllen, `Dienste.Datei`, die beiden Gaben, Rechenaufruf, `KostenKomponenteHuelle` mit Fenster-Adapter, PV/Tarif/Katalog/Verlauf, Sprünge, `BerichteKostenGaben` und Whitelist | **umgesetzt #431** (Merge `2cfee66b`) |
| **E4** Erlösrubrik und Steuerzeilen | U7 (zwei Beträge, zwei Zeilen), 9d (Gründe je Position), U6 (Anlagenfeld, Komponentenblöcke, Zwischensummen, Block „projektweit") | **#432** |
| **E5** Ergebnisansicht und V‑A | Umschalter und vier Abschnitte (U2), Bandbreite nebeneinander (U4), Empfehlungskarten (U5), Hinweistext (U10), „Bericht erzeugen" (U44), V‑A, Hinweiszeile aus U39, Kennzeichnung Nr. 31 | **#434** (Merge `deba5e57`) |
| **E6** Verlauf mit drei Szenarien | dritte Strichart, Dreierreihe, Verlauf als Abschnitt der Seite (U3), „Verlauf nach Excel…" und Berichte (U13), Wegfall von „Verlauf…" (Rest von U2), Spannenbild, Vorschlagssatz für den Stamm, „Bericht erzeugen" ohne Merken | **#436** (Merge `57b15a7c`) |
| **E7** Teil a — rechenwirksame Lücken | Nr. 29 (CO₂-Grenzwert brennwertbezogen), Nr. 30 ohne die Kern-Regel (Schemaschritt 102, „(bitte wählen)"), Nr. 32 (vermiedene Menge ohne jede Eigenerzeugung, Schlüssel brutto) | **#437** (Merge `befec9dc`) |
| **E7** Teil b — Q11 | kein Zeitzonentarif (Strommatrix mit einer Jahreszeile), Schemaschritt 104 (Staffelspalten, Zonensätze gelöscht, ihre Läufe verworfen), zweistufige Leistungspreis-Staffel am Stromträger, Tarifdialog im Rollenmodell, Einstieg „Strombezug…" entfällt | **#439** (Merge `954d4dcc`) |
| **E7** Teil c1 — K‑1, A20, Nr. 30 | Schemaschritt 105 und der zweite Fall des § 2 Nr. 16 KWKG (Regel- und Ersatzweg, Überlagerung „Sätze und Herkunft" mit den zwei Feldern), das Fristende der Inbetriebnahme 31.12.2030 als Katalogdatum (Generation 8), die Kohärenzzeilen „Anlagenart fehlt" und „Stromkennzahl fehlt", Testdatenbank 105 mit der Anlagenart der 1030-BHKW | **#440** (Merge `ea8e2a12`) |
| **E7** Teil c2 — Schritte E, F, G, S‑2, V‑1/V‑2, B‑4, E7c1-Reste | Schemaschritte 111 (Ersatz und Restwert je Position), 112 (Preisbasis als Kartenzustand) und 113 (Gase Nm³, Brennstoff 24 kWh), die Sperre der Mischlage § 53/§ 53a neben § 54 mit Warnung, EV-Mix ungerundet und § 51a mit der Einspeisevergütung, die Prozentarten der Brennstoff- und Stromkosten frisch, in Fall 2 die Vollbenutzungsstunden aus dem KWK-Strom (zurückgebaut #452) und der Rundungsgrund, der Rest der Überlagerung „Sätze und Herkunft" (U22), KI-Feldkatalog und Berichtsspalten zu Fall 2, Testdatenbank 113 | **#446** (Merge `41764ab0`) |
| **E7** Teil c3 — Reste | Vbh = W_a ÷ P_Nenn in Fall 1 und Fall 2 (Rückbau von E7c2/7), `VpvCtKwh` ungerundet (E7c2‑Q5 b), Katalog-Generation 9 (Brennstoff 24 H_i = H_s = 1,0, zwei KWKG-Zeilen abgekündigt, E7c1‑Q8), Kapitalwert 1024 als Datenstand, B‑6 in den fünf Prioritätsdateien, die Energiesteuer-Vorschau je Wahl (E7c2‑Q8 b, Nachweisfassung 8), die Wahlen der Überlagerung als Anzeigezeilen (U22), Nr. 9h gemessen, Testdatenbank auf Generation 9 | **#452** (Merge `9c7a0023`, Nachtrag `387c2d9f`) |
| **E8** Teil a — V‑C | die fünf Blöcke vollständig: Block 2 mit dem Zahlungsstrombild (U42), Block 4 mit Spannenbild und Verlauf (E6‑Q1, U49); die Gliederung mit Nominalsumme und Differenzspalte (U46), das Brückenbild (U41), „Was daraus im Lauf wird" (U47), die Fußzeile (U48) | **#454** (Merge `485052c6`) |
| **E8** Teil b — V‑D | die Formelmappe Stufen 0 bis 3 (U12) samt Blattstruktur- und ClosedXML-Wache, die Anhang-E-Checkliste (U43), die Anhang-D-Gegenprobe gegen `KapitalwertRechner.Rechne`, die Fußzeile in der Knopfreihe (E8a‑Q4) — E8 abgeschlossen | **#455** (Merge `704356a4`) |
| **E8c** — E8b‑Q2/Q3 | die Bemessungstexte aller Arten aus dem Bemessungskatalog, die Gliederungsprobe mit den Positionen des ersten Jahres („ab Jahr X", Nachweisfassung 9), der Kommentar zu U42 | **#460** (Merge `9ab55946`) |
| **E9** Teil a — V‑E im Kern | die Schemaschritte 116 (Szenariorahmen), 117 (Trägerpreise best/worst) und 118 (Erlössätze best/worst), der Kern liest die Paare, `SzenarioParameterTests`, A/B über neun Größen, Testdatenbank 118 | **#461** (Merge `62613292`) |
| **E9** Teil b — V‑E in den Dialogen | die Zeilen 8 und 9 der Szenariotafel, der ±-Knopf an Trägerpreisen und Erlössätzen (`CaseEingabeDialog` als Baustein), der Ausweis „n von m Parametern szenariert" statt des Hinweistexts, `SzenarioAbdeckungTests` — E9 abgeschlossen | **#462** (Merge `75d45630`) |
| **E10** — Nutzungsdauer S3 und Speicherflotte | Schemaschritt 120 (Sätze der Nutzungsdauertabelle), die Sätze wirksam nur über die ausdrückliche Vorbelegung, die Herkunft am Satz (Nachweisfassung 10), neue Kesseleinträge in %/a, die Speicherflotte an der Tabelle (linearer Restwert je Einheit, `RestwertEuro` der Einheit Altfeld), die Kennzeichnung A8, `NutzungsdauerS3Tests`, Testdatenbank 120, Referenzlauf byte-gleich ohne neue Basis — E10 abgeschlossen | **#463** (Merge `94521f2e`) |
| **E11** … **E12** | Wiki-Runden (E11 entfällt) | **E12 vorbereitet #470**, Sammel-Upload 26.09.2026 (Version 1.2.0.4); die acht Fragen aus E7c3 (→ Register R‑E7c3), die zwei aus E8c (→ Register R‑E8c), die sieben aus E9a (→ Register R‑E9a), die fünf aus E9b (→ Register R‑E9b) und sechs aus E10 (→ Register R‑E10) **entschieden 24.09.2026, nach Empfehlung; gebaut mit E13 (#474): E9b‑Q5 b, E7c3‑Q6 a**; E8b entschieden und gebaut (→ Register R‑E8b; E8b‑Q1 abgelöst durch E14‑Q2 a, #477) — **alle Fragen bis E10 entschieden** |
| **E13** — kleine Bauwelle | Punkt 9 der Anhang-E-Checkliste „erfüllt" mit beiden Szenarien (E9b‑Q5 b), Lade-, Speicher- und Vorsorgegrund in der Oberfläche (E7c3‑Q6 a), der Halbsatz aus A8 zur Speichervariante, zwei Hilfe-Anker der Seite Kosten; ohne Rechenwirkung, kein Schemaschritt | **#474** (Merge `c71addf5`) |
| **E14** — Formelmappe je Szenario | Stufe 1 und 2 der Formelmappe für Günstig und Ungünstig aus dem eigenen Parametersatz (Befund 1 aus E9a): Mehrjahrestabellen bis zum längsten Zeitraum mit Schutzformel jenseits von T_s, Kennzahlen, Zinsfuß und Bandbreite als Formeln, Punkt 11 der Checkliste; Wertfassung = Formelfassung in 16 Prüfgruppen; ohne Rechenwirkung, kein Schemaschritt; E14‑Q1…Q3 entschieden 24.09.2026, nach Empfehlung (→ Register R‑E14) | **#477** (Merge `b9c660b9`) |
| **E15** — Risikomodul (V‑G7) | Schemaschritt 125 (vier Spalten an `Tab_ProjektWirtschaftlichkeit`), Zinszuschlag oder Zahlungsstromabzug nach DIN EN 17463, 6.5 und Anhang F, Vorgabe aus; R_loss als Betrag in € je Periode (die Prozentlesart der Tabelle F.2 als E15‑Q4 c, spätere Erweiterung, nicht beauftragt); Gruppe „Risiko" im Parameterdialog, Ausweis nur bei Pflege, Formelmappe mit Risikozeilen; ohne Pflege bitgleich, Testdatenbank 125; E15‑Q1…Q4 entschieden 24.09.2026, nach Empfehlung (→ Register R‑E15) | **#478** (Merge `dedfc760`; erster Merge `d176b378`) |
| **E17** — Nicht monetarisierbare Wirkungen (V‑G11) | Schemaschritt 127 (`Tab_ProjektWirkung`, Freitext als SONSTIG übernommen, Altfeld lesbar), Kategorie und Beurteilung nach DIN EN 17463, 6.1 und 8.2 (Dauer × stärkste Wirkung, 0 bis 9); `WirkungenListe` im Bewertungsblock, Tabelle in Wort- und Tabellenbericht, Checkliste 2b/3b; ohne Rechenwirkung, Testdatenbank 127; E17‑Q1…Q4 entschieden 24.09.2026, nach Empfehlung (→ Register R‑E17) | **#479** (Merge `52614c33`; erster Merge `0462f92e`) |
| **E16** — Wiederholperiode je Kostenposition (V‑G3) | Schemaschritt 129 (`Wiederholperiode_a` an `Tab_ProjektWerte` und `Tab_KostenVorlagePosition`), Betriebspositionen „alle n Jahre" nach DIN EN 17463, 6.3.1 — Zahlung in s, s + n, … ≤ T; Feld „Zahlung alle: [n] Jahre" im Zeileneditor und in den Kostenvorlagen, Ausweis in Betriebskostentabelle, Formelmappe und Nachweisumschlag; ohne Pflege bitgleich, A/B an 1030, Testdatenbank 129; E16‑Q1…Q4 offen (→ Register R‑E16) | **#484** (Merge `ae7b0ed0`) |
| **E18** — Restpunkte Stromsteuer (§ 6.3 Nr. 14, 16, 18) | Wache der Stromsteuer-Rückfallebene gegen Saat und Katalog der Testdatenbank (älteste Zeile), zwei tote Ressourcen gestrichen; Anzeige des erfassten Stromsteueranteils mit Satzabgleich und Kohärenzzeile im Dialog „BHKW-Wirtschaftlichkeit"; Nr. 18 nachgemessen, offen, im Code vermerkt; ohne Rechenwirkung, kein Schemaschritt; E18‑Q1…Q6 entschieden 24.09.2026, nach Empfehlung, Q7 als Restpunkt Nr. 33 (→ Register R‑E18) | **#492** (Merge `e79bffb1`) |
| **E19** — Restpunkte Unternehmensart (§ 6.3 Nr. 15, 33) | Nr. 15 durch die Schalentrennung überholt, Wache `KatalogjahrJeOeffnungTests`; die Unternehmensart ohne BHKW im Parameterdialog, Gruppe Strom, mit der Anzeige des erfassten Stromsteueranteils und der § 9b-Erklärzeile (§ 9b ohne BHKW erreichbar); KI-Feld mit Sperre, Maskenwache 35; ohne Rechenwirkung, kein Schemaschritt; E19‑Q1…Q6 entschieden 25.09.2026, nach Empfehlung, Q4 b (→ Register R‑E19) | **#498** (Merge `31a0b085`) |
| **E20** — Wärmepumpe je kW elektrisch (§ 6.3 Nr. 10) | die Investitionskosten der Wärmepumpe auch „je kW elektrisch“, P_el = Ptherm ÷ COP am Normpunkt der Kennlinie (A2/W35, B0/W35, W10/W35, bei W35 interpoliert), nur Kategorie 1, die Betriebsseite unverändert; im Bestand ohne Rechenwirkung, kein Schemaschritt; E20‑Q1…Q5, Q7, Q8 entschieden 25.09.2026, nach Empfehlung a, E20‑Q6 offen beim Anwender (→ Register R‑E20) | **#502** (Merge `49ea20e0`) |
| **E22** — Rechenweg-Sortierung nach der Regel „99“ (§ 6.3 Nr. 18) | die fünf Rechenweg-Leser und die drei Modul-Lader nach `Ladeordnung.SqlAnlagenprio` wie Hydraulikbild und Erzeugerkarten, gepflegte Priorität zuerst, ungepflegt hinten; ohne Rechenwirkung, allein der Modulindex der Wärmepumpen in 1042, neue Basis R16 (vierzehn Projekte); kein Schemaschritt; E22‑Q1 entschieden 25.09.2026, nach Empfehlung a (→ Register R‑E22) | **#503** (Merge `76f8661d`) |
| **E23** — Betriebskosten der Wärmepumpe ohne kWh (§ 6.3 Nr. 10, E20‑Q6) | „je kWh elektrisch“ und „je kWh thermisch“ im Betriebsraster der Wärmepumpe gesperrt, Bestandszeilen rechnen weiter mit dem Vermerk „Altbestand“; wählbar bleiben fester Jahresbetrag, Prozentbemessungen und je kW; im Bestand ohne Rechenwirkung, kein Schemaschritt; E23‑Q1…Q7 entschieden 25.09.2026, nach Empfehlung a (→ Register R‑E23) | **#510** (Merge `f7823b8e`) |
| **E26** — PV-Ausweis und Strommatrix-Bedarf (Befunde N1, N3 aus E25; § 6.3 Nr. 34) | die Stromproduktion der Photovoltaik als Erzeugung der Module, der Eigenverbrauch des Ausweises als Erzeugung − Einspeisung, der Bedarf der Strommatrix aller Verbraucher (`STROMBEDARF_GESAMT`) auch für den KWK-Split; Kapitalwert an den Ankern gleich, neue Basis R18 (vierzehn Projekte), kein Schemaschritt; E26‑Q1…Q7 entschieden 25.09.2026, nach Empfehlung (→ Register R‑E26); Restpunkte Q6, N5, N6 | **#518** (Merge `025a8707`) |
| **E27** — Netzbezug nie negativ (Befund N5 aus E26; § 6.3 Nr. 36) | der Reststrom am Laufende bei 0 geklemmt (nicht bei der Speicherflotte), der Reststrombedarf der BHKW-Zeile je Stunde, ein BHKW-Überschuss allein als Einspeisung im KWK-Split; keine Gutschrift der Reststromkosten im Rollentarif mehr; Kapitalwert-Anker 1030 neu, neue Basis R19 (vierzehn Projekte), kein Schemaschritt; E27‑Q1 und Q2 vom Anwender, Q3…Q8 entschieden 25.09.2026, nach Empfehlung (→ Register R‑E27); Restpunkte Q3 b, N7, Q6 | **#521** (Merge `80a7b9fb`) |

Daneben laufen **W‑E2** (die Statuszeilen-Schreibweise für E2, #405) und **DL‑2** (Knopfleisten aller
Dialoge; die beiden Dialoge dieses Papiers mit **DL‑2e**, #390). **KI‑F2 … KI‑F8** (#419–#425, #427,
#428) geben die Masken für den Hilfe-Assistenten frei — davon berührt **KI‑F4** (#423) die Kosten- und
Wirtschaftlichkeitsmasken; **KI‑F8** (#428) liefert das Hüllen- und Adaptermuster für E3.

**Zwei Fallen bei den Kennungen.** (1) Die Statusnummern **#302, #304, #328 und #331** sind je
**doppelt** vergeben (zwei Rechner); jede Zeile der Statusdatei trägt den Zusatz, ein Verweis
„#331" allein ist mehrdeutig. (2) Die Kürzelkollisionen K-1/K1, B-1/B-1, U-1/U1, V-1/V-1 und
V-Gn/Gn sind unter „Namensvorsicht" am Ende von § 2.12 aufgelöst.
