# Konzept: Baualtersklassen und Energiestandard der Gebäude

**Stand 26.09.2026 — freigegeben (Entscheid E47, Konzept Gebäudesimulation N1.52) und umgesetzt (Schemaschritt 148, Protokoll G4 Abschnitt 16); offen ist die Sichtabnahme unter Windows.** Anlass ist die Windows-Sichtabnahme des Gebäudeimports (Protokoll G4,
Abschnitt 15, Punkt 6): „Baualtersklassen stimmen nicht und sollten komplett überarbeitet werden, am besten
mit neuer, üblicher Einteilung." Der Anwender hat am 26.09.2026 entschieden:

1. **Weg 1** — zwei Felder: eine **Baualtersklasse nach Bauzeitraum** in der üblichen Einteilung und ein
   eigenes Feld **Energiestandard**.
2. Neuere Grenzen (Gebäudemodernisierungsgesetz 2026/2027) prüfen und **Wohn- und Nichtwohngebäude**
   unterscheiden.
3. Die **Buchstaben nach IWU** übernehmen und die **Namen im Auslieferungskatalog umbenennen**.

Die Fragen in Abschnitt 8 hat der Anwender am 26.09.2026 **nach Empfehlung** beantwortet (F1–F6); der
Entscheid steht als **E47** im Register der Gebäudesimulation. **F4 ist mit E51** (26.09.2026, Konzept
Gebäudesimulation N1.58) **aufgehoben:** Klassen und Standards ohne Katalogsatz bekommen freie Werte aus
Stein/Loga (2025), die Klassen M und A zudem eigene Katalogsätze (Abschnitt 4); umgesetzt mit
Schemaschritt 149 (26.09.2026).

## 1. Befund heute

**Die Liste.** 21 Klassen A–U, gespeichert als Buchstabe in `Tab_Gebaeude.Baualtersklasse` und
`Tab_Gebaeude_STAMM.Baualtersklasse` (Text), Texte in `GebaeudeStammCtrl.BAUALTERSKLASSEN_DE` und den
Ressourcen `GEB_BAK_A` … `GEB_BAK_U`. Sie stammt aus den gelöschten WinForms-Masken (`Form_Gebaeude`,
`Form_Gebaeude1`) und mischt drei Achsen:

| Achse | Klassen heute |
|---|---|
| Bauzeitraum | A vor 1919, B 1919–1948, C 1949–1957, D 1958–1968, E 1969–1978, F 1979–1983, G 1984–1994, H 1995–2000 |
| Energiestandard | I Niedrigenergiebauweise, J Passivhaus, K EnEv 2007, M EnEV 2009, P EnEV 2014, Q EnEV 2016 |
| Förderstufe | L Eff. 70 (EnEV 2007), N Eff. 70 (EnEV 2009), O Eff. 55 (EnEV 2009), R Eff. 100 (EnEV 2016), S Eff. 155 (EnEV 2016), T BEG 55, U BEG 40 |

**Mängel.** Es fehlen 2001–2006 und alles ab 2020; „vor 1919" fasst alles davor zusammen; „Eff. 155" gibt
es nicht, „EnEv" ist ein Tippfehler. Sechs Klassen (L, O, P, R, T, U) haben keinen Satz im Gebäudekatalog
der Testdatenbank und liefern keine Vorgaben; J, K, M, N, S haben nur 3–5 Sätze. Aus dem Baujahr ermittelt
der Import nur A–H (`Baujahrregel`, bis 2000).

**Wirkung.** Die Klasse geht in **keine Rechnung** ein — weder der VDI-6007-Weg noch der Tagesbilanz-Altweg
liest sie (`GebaeudeModel`, `BaujahrSchema`, Suche in `Allgemein/Simulation/**`). Sie steuert:

- die **Vorgaben des Imports** — U-Werte, g-Wert, ψ als Median der Katalogsätze je Klasse
  (`GebaeudeVorgaben`, Entscheid E27: eigene Werte statt TABULA/IWU), auch für den Bauteilvorschlag (G4b);
- **Anzeige und Filter** — Katalogeditor, Gebäudeverwaltung, Gebäudedialog (die Listenspalte heißt dort
  historisch „Baujahr", `Katalogfilterprofil.SpBaujahr`), Wohnflächendialog;
- den **Bericht** — dort steht bisher der Buchstabe statt des Texts (`BausteineProjekt`);
- den **Assistenten** (`KiDialoge`) und das Werkzeug `Werkzeuge/Gebaeudevergleich`.

**Daten der Testdatenbank.** Katalog (`Tab_Gebaeude_STAMM`, 269 Sätze): A 11, B 13, C 26, D 34, E 21,
F 30, G 38, H 14, I 34, J 3, K 5, M 5, N 3, Q 26, S 4, ohne Klasse 2. Projektkopien (28): A 18, D 2, F 4,
G 2, H 2 — die Referenzprojekte tragen nur A, D, F, G, H. **Die Katalognamen tragen den Buchstaben** als
zweiten Namensteil (`AltenH-C-U-252` = Altenheim, Klasse C, unsaniert, Kennzahl), nicht jeder Name folgt dem
Schema (`Kaufhaus`, `Hotel-KfW 55`).

## 2. Recherche

**Die übliche Einteilung für Wohngebäude** ist die der Deutschen Wohngebäudetypologie des IWU (Loga u. a.
2015, Bild 1 S. 9, Begründung S. 10–11): Die Grenzen folgen historischen Einschnitten, statistischen
Erhebungen und den Wärmeschutzvorschriften.

| IWU | Zeitraum | Grund der Grenze |
|---|---|---|
| A | bis 1859 | vorindustriell |
| B | 1860–1918 | Gründerzeit bis Ende des Ersten Weltkriegs |
| C | 1919–1948 | Zwischenkriegszeit |
| D | 1949–1957 | Nachkriegsbau, DIN 4108 (1952) |
| E | 1958–1968 | Stahlbeton, Großsiedlungen |
| F | 1969–1978 | industrielle Bauweisen, vor der 1. WSchV |
| G | 1979–1983 | 1. WSchV |
| H | 1984–1994 | WSchV 84 |
| I | 1995–2001 | WSchV 95 |
| J | 2002–2009 | EnEV 2002/2004/2007 |
| K | 2010–2015 | EnEV 2009/2014 |
| L | ab 2016 | EnEV-Verschärfung 2016 |

**Nach 2016.** Die IWU-Liste endet offen mit L. Die einzige gefundene Fortschreibung mit Zeiträumen nach 2016
ist das IWU-Typgebäudemodell im Auftrag des BBSR (Stein/Loga 2025, CC BY 4.0): 2016–2020 und 2021–2025, ohne
Buchstaben. Der dena-Gebäudereport 2026 führt die Zensusklasse „2020 und später".

**Gebäudemodernisierungsgesetz (GModG).** Verkündet am 28.07.2026 (BGBl. 2026 Nr. 226); die Heizungsregeln
gelten seit 29.07.2026 (65-%-Regel entfällt), die Umsetzung der EU-Gebäuderichtlinie ab 01.01.2027 (neues
Referenzgebäude, DIN/TS 18599), Nullemissionsgebäude ab 2028 für Behörden und ab 2030 für alle Neubauten.
Die Hüllenanforderung ist seit 2016 praktisch unverändert (GEG 2020 auf EnEV-2016-Niveau; GEG 2023 änderte
nur die Primärenergie; das GModG geht laut Begründung vom gleichen Niveau aus). **Eine eigene Klassengrenze
für die Hülle begründet es nicht;** die Sprünge 2023 und 2030 bildet die Achse Energiestandard ab. Für
Nichtwohngebäude setzt das GModG Mindeststandards, die bei **Baujahr ab 1996** als erfüllt gelten — dafür
braucht es das genaue Baujahr, nicht die Klasse.

**Nichtwohngebäude.** Keine Quelle vergibt Buchstaben; bis 2009 sind die Grenzen dieselben wie bei
Wohngebäuden (amtliche Bekanntmachung zur Datenaufnahme im Nichtwohnbestand 2020: bis 1918 … 1995–2001, ab
2002; ENOB:dataNWG: dieselben Grenzen, dann 2010–2014 und ab 2015; IWU-Typologie der Nichtwohngebäude 2022:
vor 1979, 1979–2009, 2010–2019). Die EnEV-2016-Verschärfung galt auch für Nichtwohngebäude.

**Energiestandards.** Belastbar benannt sind die Förderstufen der BEG (KfW-Merkblätter 261 und 263, Stand
09/2026): Wohngebäude Effizienzhaus 85, 70, 55, 40 und Denkmal; Nichtwohngebäude Effizienzgebäude 70, 55, 40
und Denkmal (kein 85); Effizienzhaus 100 und 115 nur historisch (bis 2022). Dazu Passivhaus bzw. EnerPHit
(Passivhaus Institut) und das Nullemissionsgebäude des GModG. „Niedrigenergiehaus" und „teilsaniert" sind
keine amtlichen Stufen.

**Lizenz** (Befund, keine Rechtsberatung). Jahresgrenzen und Buchstaben sind Fakten; die amtlichen
Bekanntmachungen sind frei (§ 5 UrhG). Übernommen werden **nur die Grenzen**, nicht die Kennwerte (E27
bleibt). Die Quelle der Einteilung wird in Programm und Wiki genannt.

Quellen: IWU, Deutsche Wohngebäudetypologie (2015); Stein/Loga (2025), Zenodo 15488271; BBSR Forschung
kompakt 6/2025; dena-Gebäudereport 2026; GModG-Infoportal des Bundes; KfW-Merkblätter 261 und 263 (09/2026);
Bekanntmachungen BAnz AT 04.12.2020 B1 und B2; IWU Working Paper 2022 (Hörner/Bischof).

## 3. Zielbild

### 3.1 Die Baualtersklasse — ein Zeitraum, für Wohn- und Nichtwohngebäude gleich

| Klasse | Zeitraum | Grenze | Status |
|---|---|---|---|
| A | bis 1859 | IWU | gesichert |
| B | 1860–1918 | IWU | gesichert |
| C | 1919–1948 | IWU | gesichert |
| D | 1949–1957 | IWU | gesichert |
| E | 1958–1968 | IWU | gesichert |
| F | 1969–1978 | IWU | gesichert |
| G | 1979–1983 | IWU | gesichert |
| H | 1984–1994 | IWU | gesichert |
| I | 1995–2001 | IWU | gesichert |
| J | 2002–2009 | IWU | gesichert |
| K | 2010–2015 | IWU | gesichert |
| L | 2016–2020 | IWU (Beginn), Stein/Loga 2025 (Ende) | Ende 2020 ist eigene Festlegung |
| M | ab 2021 | Stein/Loga 2025, Zensus „2020 und später" | Buchstabe M ist eigene Festlegung |

Eine Klasse N „ab 2030" (Nullemissionsgebäude) wird erst angelegt, wenn die Anforderungswerte feststehen.
Wohn- und Nichtwohngebäude nehmen **dieselbe** Liste — bis 2009 ist das amtlich und typologisch belegt, die
Grenze K/L folgt der EnEV 2016, die für beide galt.

**Das Baujahr führt.** Ist das Baujahr gesetzt (Spalte `Baujahr`, Schemaschritt 139), folgt die Klasse aus
ihm — im Editor, in der Gebäudeverwaltung und im Import, für jedes Jahr (die `Baujahrregel` deckt dann A–M
ab). Ohne Baujahr ist die Klasse wählbar. So bleiben das genaue Jahr für die GModG-Grenze 1996 und die
Zensusklassen erhalten.

### 3.2 Der Energiestandard — ein eigenes, freiwilliges Feld

| Code | Anzeige | Nutzung | Status |
|---|---|---|---|
| (leer) | wie Baualtersklasse (unsaniert) | beide | Vorgabe |
| `TEILSANIERT` | teilsaniert | beide | keine amtliche Stufe |
| `SANIERT` | saniert nach den Bauteilanforderungen des GModG | beide | gesichert |
| `NIEDRIGENERGIE` | Niedrigenergiehaus | beide | historischer Begriff |
| `EH115_100` | Effizienzhaus 115/100 (bis 2022) | Wohnen | historisch |
| `EH85` | Effizienzhaus 85 | Wohnen | BEG 09/2026 |
| `EH70` | Effizienzhaus/-gebäude 70 | beide | BEG 09/2026 |
| `EH55` | Effizienzhaus/-gebäude 55 | beide | BEG 09/2026 |
| `EH40` | Effizienzhaus/-gebäude 40 | beide | BEG 09/2026 |
| `DENKMAL` | Effizienzhaus/-gebäude Denkmal | beide | BEG 09/2026 |
| `PASSIVHAUS` | Passivhaus bzw. EnerPHit | beide | Passivhaus Institut |
| `NULLEMISSION` | Nullemissionsgebäude | beide | GModG, Werte offen |

Die Klappliste zeigt je nach `Wohngebaeude_Nicht_Wohngebaeude` nur die passenden Einträge. Gespeichert wird
der sprachneutrale Code (Drei-Schichten-Regel), die Texte kommen aus den Ressourcen. „Gesetzlicher
Mindeststandard zum Baujahr" ist kein eigener Eintrag — das ist die Baualtersklasse selbst.

## 4. Vorgaben für U-Werte, g-Wert und ψ

Die Vorgaben sind Mediane der eigenen Katalogsätze, soweit es sie gibt (E27, geändert mit E51). Die Kette in
`GebaeudeVorgaben.Fuer(klasse, standard)` lautet: **Energiestandard mit Katalogsätzen → Klasse mit
Katalogsätzen → freier Wert**; der Import fragt allein die Klasse. Die Klassenzeile umfasst alle Sätze der
Klasse, gleich welchen Standard und welche Nutzung sie tragen. Hat weder Standard noch Klasse einen
Katalogsatz, gilt nach **E51** (26.09.2026, Konzept Gebäudesimulation N1.58) der **freie Wert** aus Stein, B.;
Loga, T. (2025): *Das Typgebäude-Modell zur energetischen Bewertung des Wohngebäudebestands*, IWU im Auftrag
des BBSR, Zenodo, Record 15488271, CC BY 4.0 — U-Werte und g-Wert des Typgebäudes EZFH aus Anhang A, Tab. 28,
13 Zeilen A–M, ψ leer, weil die Quelle nur einen Wärmebrückenzuschlag führt. Er ist sichtbar mit eigener
Herkunft (`VorgabeFrei`, gespeichert als `VORGABE` mit eigenem Beleg) und eigener Meldung, nie still; die
Quellenangabe steht in der Herleitungszeile, im Wiki und in den Lizenzhinweisen. Ein Wert der Nachbarklasse
wird nicht geliehen (Hausregel „ein geliehener Wert wird nie still gesetzt").

Die Klassen M und A haben eigene Katalogsätze, gesät mit Schemaschritt 149 (`ReadOnly = 1`, Schlüssel ist
der Bezeichner): für M `EFH-GEG-Ref` (Referenzgebäude nach GEG Anlage 1), `EFH-GEG-EH55` (Energiestandard
Effizienzhaus 55, U-Werte des Referenzgebäudes × 0,70) und `KMH-GEG-typ` (kleines Mehrfamilienhaus nach
Stein/Loga), für A `EFH-bis1859-U`, `KMH-bis1859-U` (Urzustand) und `EFH-bis1859-TS` (anteilig modernisiert,
ohne Energiestandard), alle nach Stein/Loga „bis 1918". Sie tragen keine Kennzahl im Namen.

Der Katalog der Testdatenbank hat damit: **A 3**, B 11, C 13, D 26, E 34, F 21, G 30, H 38, I 14, J 42,
K 8, L 30, **M 3** Sätze (Standards: Niedrigenergie 34, Passivhaus 3, Effizienzhaus 70 3,
**Effizienzhaus 55 1**). **Jede Klasse A–M hat Katalogsätze; der freie Rückfall ruht** und ist über die
Lesenaht `GebaeudeVorgaben.KatalogOhne(...)` getestet. K hat nur wenige Sätze. Die Mediane rechnet
`GebaeudeVorgabenTests` aus dem Katalog nach, die Tabelle in `GebaeudeVorgaben` ist daraus geschrieben.

## 5. Umschlüsselung des Bestands

Ein Schemaschritt schlüsselt `Baualtersklasse` in `Tab_Gebaeude` und `Tab_Gebaeude_STAMM` um und setzt die
neue Spalte `Energiestandard`. **Ist `Baujahr` gesetzt, gilt die Klasse aus dem Baujahr**, sonst die Tabelle:

| alt | Text alt | neu Klasse | neu Energiestandard |
|---|---|---|---|
| A | vor 1919 | B | — |
| B | 1919 bis 1948 | C | — |
| C | 1949 bis 1957 | D | — |
| D | 1958 bis 1968 | E | — |
| E | 1969 bis 1978 | F | — |
| F | 1979 bis 1983 | G | — |
| G | 1984 bis 1994 | H | — |
| H | 1995 bis 2000 | I | — |
| I | Niedrigenergiebauweise | J | `NIEDRIGENERGIE` |
| J | Passivhaus | J | `PASSIVHAUS` |
| K | EnEv 2007 | J | — |
| L | Eff. 70 (EnEV 2007) | J | `EH70` |
| M | EnEV 2009 | K | — |
| N | Eff. 70 (EnEV 2009) | K | `EH70` |
| O | Eff. 55 (EnEV 2009) | K | `EH55` |
| P | EnEV 2014 | K | — |
| Q | EnEV 2016 | L | — |
| R | Eff. 100 (EnEV 2016) | L | `EH115_100` |
| S | Eff. 155 (EnEV 2016) | L | — (Stufe gibt es nicht; Protokollzeile) |
| T | BEG 55 | M | `EH55` |
| U | BEG 40 | M | `EH40` |

**Die Buchstaben verschieben sich** (heute D = 1958–1968, künftig E). Der Schritt ist einmalig und
idempotent über einen Marker; er protokolliert jede Zeile, deren Klasse sich nicht eindeutig ergibt (I, J, S
ohne Baujahr).

## 6. Katalognamen

Die Sätze des **Auslieferungskatalogs** (`ReadOnly = 1`), deren zweiter Namensteil der alte Buchstabe ist,
bekommen den neuen (`AltenH-C-U-252` → `AltenH-D-U-252`) — in der Auslieferungsvorlage und über den
Schemaschritt in jeder Anwenderdatenbank. **Eigene Sätze der Anwender behalten ihren Namen**, nur die Klasse
wird umgeschlüsselt. Projektkopien behalten ihren Namen (sie sind Kopien; die Verbindung zum Katalog läuft
über `ID_Gebaeude_Stamm`). Namen ohne Klassenteil bleiben. Kollidiert ein neuer Name mit einem bestehenden,
bleibt der alte und die Migration meldet es.

## 7. Umsetzung

| Welle | Inhalt |
|---|---|
| W1 Kern | Klassenliste A–M mit Texten (de/en) und Quelle; `Baujahrregel` für alle Jahre; Energiestandard (Codes, Texte, Filter je Nutzung); `GebaeudeVorgaben` je Klasse und Standard, neu gerechnet; Schemaschritt (nächste freie Nummer): Spalte `Energiestandard TEXT` mit `CHECK` auf die Codes, Umschlüsselung, Namen des Auslieferungskatalogs, siebter Neubau der Sicht `Abfrage_Projektgebaeude`; Testdatenbank und Auslieferungsvorlage |
| W2 Oberfläche | Katalogeditor und Gebäudeverwaltung (Klasse aus Baujahr, Klappliste Energiestandard), Gebäudedialog (Listenspalte „Baualtersklasse" statt „Baujahr"), Wohnflächendialog, Import (Klasse aus Baujahr für alle Jahre; Vorgaben aus Standard), Bericht mit Klartext statt Buchstabe, Assistent, `Werkzeuge/Gebaeudevergleich` |
| W3 Nachweis | Tests; Referenzlauf gegen die aktuelle Basis — kein Rechenweg liest Klasse oder Energiestandard, beide sind keine Spalten des Gebäudemodells im Sinn der Einfrierregel; die Ergebnisse bleiben byte-gleich, **eine neue Basis wird nicht eingefroren** (Nachtrag zum Schemastand in `Referenzlaeufe/LIESMICH.md`); Wiki „Gebäude" und „Gebäudeimport" (mit Quellenangabe), Glossar, Logbuch-Satz |

**Abstimmung.** G3 vor W1: Bauteilvorschlag und die Tests von G4b rechnen mit Klasse E und ziehen die
U-Vorgaben über `GebaeudeVorgaben`. AK1: siebter Neubau der Sicht. Schema- und Entscheidnummer werden spät
vergeben und vorher angekündigt.

## 8. Fragen an den Anwender

Entschieden am 26.09.2026: **alle Empfehlungen** (E47).

| Nr. | Frage | Empfehlung = Entscheid |
|---|---|---|
| F1 | L 2016–2020 und M ab 2021 (Anschluss an Stein/Loga 2025 und Zensus) — oder L ab 2016 offen wie IWU, bzw. M ab 2023 (GEG 2023, nur Primärenergie)? | **L 2016–2020, M ab 2021** |
| F2 | Das Baujahr führt, die Klasse folgt aus ihm; wählbar ist sie nur ohne Baujahr? | **ja** |
| F3 | Energiestandard mit den zwölf Einträgen aus 3.2, gefiltert nach Wohn-/Nichtwohngebäude? Denkmal als Standard statt als eigenes Merkmal? | **ja, wie 3.2** |
| F4 | Klassen und Standards ohne Katalogsatz (A, M, einige Standards): Vorgabe leer lassen (E27) — oder die unter CC BY 4.0 freien Werte von Stein/Loga 2025 mit Quellenangabe nehmen (Änderung von E27)? | **leer lassen**, Katalog später ergänzen — **aufgehoben mit E51** (26.09.2026, Konzept Gebäudesimulation N1.58): **beides** — freie Werte aus Stein/Loga (2025) nur ohne Katalogsatz, mit Herkunft und Beleg, **und** eigene Katalogsätze für M und A; E27 ist damit geändert (Abschnitt 4) |
| F5 | Umschlüsselung nach Abschnitt 5 (alt A → B, Niedrigenergie/Passivhaus ohne Baujahr → J mit Standard, „Eff. 155" → L ohne Standard)? | **ja** |
| F6 | Energieausweisklasse (Wohnen A+–H, Nichtwohnen A–G ab 2027) als weiteres Feld? | **nein, nicht jetzt** |
