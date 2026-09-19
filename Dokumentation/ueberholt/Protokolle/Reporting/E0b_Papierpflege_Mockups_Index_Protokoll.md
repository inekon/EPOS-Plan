# E0b — Papierpflege Mockups und Index (Protokoll)

**Datum:** 19./20.09.2026 · **Zweig:** `e0b_papierpflege` (Basis `455edfd4`) · **21 Commits**,
alle mit Betreff `E0b: …`, siehe `git log --format='%h %s' 455edfd4..HEAD`. Kein Push, kein
CI-Lauf, kein Branchwechsel.

Grundlage: `Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Pruefung_Mockups_Wirtschaftlichkeit.md`
§ 3.1/§ 3.2/§ 5 Etappe P1 und ihre Protokolle unter
`Dokumentation/ueberholt/Protokolle/Reporting/Pruefung_Mockups_2026-09-19/`. Nur Zeilen und
Teile **ohne Stern** (kein ausstehender Anwenderentscheid); gemischte Tabellenzeilen nur im
ungestirnten Teil geändert.

Quellenkürzel in den Tafeln: `00/Sn` = `00_Sichtpruefung.md` Befund n; `02/x-n` =
`02_Konsistenz_Papiere.md` Zeile x-n; `03/#n` = `03_Mockup_Code.md` Befund n; `01/Bn` =
`01_Nachrechnung.md` Zeile Bn; `06/#n` = `06_Struktur_Darstellung.md` Befund n.

---

## 1 Dialog_Formel_Zahlenprobe.html (Hauptmockup)

9 Commits (`c383d583` … `ae8bc34e`). Zeilen 4849 → 5066, Bytes 391.339 → 410.269 (CRLF, keine
BOM, byte-erhaltend geprüft vor jeder Rundung/Ersetzung).

### 1.1 Struktur, Randbreite, Navigation (Commit `c383d583`)

| Stelle | Änderung | Quelle |
|---|---|---|
| `.rumpf`, `.f-spalten` | `grid-template-columns` auf `minmax(0,1fr)` statt `1fr` (Grid-Blowout behoben) | `00/S1` |
| `.tafel-huelle, .fenster, .chart-karte, .seite` | `max-width:100%; overflow-x:auto` gebündelt ergänzt | `00/S1` |
| `.duo` (Kat. 8) | zwei Ergebnisseiten-Rahmen von nebeneinander auf untereinander gestellt, redundante 860px-Medienregel entfernt | `00/S1` |
| Navigation | zweistufig: 43 neue Anker (Dialog/Grundlage/Erläuterung/Schlüssel/Abnahme je Kategorie, Kat. 7 ohne Dialog, Kat. 8 zusätzlich die vier Fragezonen), `.navunter`-Listen, „nach oben" an allen 11 Abschnitten | `00/S2`, `06/#5` |
| Leseweg-Kasten | Chip-Legende (acht Marker) mit den im Papier selbst verwendeten Definitionen ergänzt | `00/S5` |
| 10 × `<h4>` | zu `<h3>` (kein Sprung h2→h4 mehr), `.merk`-Selektoren mitgezogen | `06/#3` |
| 6 Schriftgrößen | 10/10,5px → 11px | `06/#15` |
| 8 CSS-Variablen | `rgb()` → Hex vereinheitlicht; `rgba()`-Schatten unverändert | `06/#12` |
| 38 Eingabezellen | `title`-Attribut aus dem angrenzenden `<label>` ergänzt | `06/#19` (Teilmenge, siehe § 11) |

### 1.2 Rahmenstile, ⓘ/×, Erläuterungstafeln (Commit `e6cb23a8`)

| Stelle | Änderung | Quelle |
|---|---|---|
| `.s-band` | Hintergrund auf `--marine-hell` gestellt (unterscheidbar von `.f-band`); Unterschied im Leseweg-Kasten benannt | `00/S6` |
| Energieträger, Parameterdialog, Reiter Ertrag/Bonus | ⓘ/× nachgezogen; Fußleiste am Reiter Ertrag/Bonus ergänzt (gleiche Leiste wie Nachbarreiter) | `00/S6`, `00/S7` |
| Kat. 1/2/5 Erläuterungsabsätze | in Tafel „Element → Feldart → Verhalten" (Kat. 1, neue Klasse `.f-erklaertafel`) bzw. Aufzählungen (Kat. 2, Kat. 5) umgesetzt, Inhalt unverändert | `00/S3` |

### 1.3 Anhang Umsetzungsstand (Commit `470c0063`)

| Stelle | Änderung | Quelle |
|---|---|---|
| Tafelkopf | neue erste Spalte „Stand" mit Chip offen/erledigt/Entscheid ausstehend | `00/S4` |
| 39 vorhandene Zeilen | eingeordnet (19 offen, 20 erledigt nach `.gestrichen`) und neu sortiert — offene zuerst, erledigte danach; U-Kennungen (Zeilenidentität, vielfach im Fließtext referenziert) unverändert | `00/S4` |
| Zeile U40 | neu angelegt, sofort als erledigt gestrichen: Vorlage „Standard" auf Bemessungsart „% des Endenergiebedarfs", Schemaschritt 94, #365/#366 — schließt die bis dahin hängende Referenz „umgesetzt U40" in Kategorie 2 an | `02/c-1`, `03/#28` |
| Zeilen U41–U45 | neu, offen: Brückenbild, Zahlungsstrombild (Kat. 8, Konzeptentscheid nötig); Knöpfe „Anhang-E-Checkliste…" und „Bericht erzeugen" (Kat. 8, Chip „Entscheid ausstehend" statt „offen", ohne externe Kennung im Text); Fußzeile der Überlagerung „Schnellwahl aus Katalog" (Kat. 4) | `03/#83, #84, #86` |
| U1, U32 | Markertext „Schemaschritt 92"/„92 und aufwärts" → „Schemaschritt 97 (90–96 vergeben)" (die Prüfung selbst nannte noch 95, seither weitere Schritte vergeben) | `02/b-2`, `03/#43` |

### 1.4 Kleinere Nachzüge (Commit `ae8bc34e`)

| Stelle | Änderung | Quelle |
|---|---|---|
| Kat. 5 Ressourcentafel | `WIRT_KWKG_KONTINGENT_LEER` (geplant) + Sammelbegründung „Kontingent leer" → drei vorhandene Schlüssel `WIRT_KWKG_KONTINGENT_OHNE_ART/_ANTEIL_FEHLT/_ZU_KLEIN` (gegen `Resource.resx` geprüft), kein `geplant`-Marker mehr | `02/c-2`, `03/§6.3` |
| Kat. 3 Ressourcentafel | Klappliste „Stammprojekt:" → „Projekt:" (der Dialog selbst nennt schon „Projekt:") | `02/d-17`, `03/#35` |
| Kat. 1 Fließtext | veraltete Feldliste der Übernahme-Überlagerung ersetzt durch den seit #363 gebauten Katalogblock (Komponente · Kategorie · Variante · Positionsvorschau mit Spalte „Ziel") | `02/d-16`, `03/#14` |

### 1.5 Kategorie 1 Raster (Commit `75e30336`, `03/#1–#9, #12, #13, #15, #16`)

| Stelle | Änderung | Quelle |
|---|---|---|
| Herleitungszeilen | „von …" → „× …" (#1); „· Hauptposition ·" aus Runde-1-Zeile gestrichen (#2); „Satz = Betrag" ohne Zusatz, auch an der Zuschusszeile (#3, #4); „Stufe: Anlage BHKW 1" → „Stufe Anlage" (#6); „Hauptpositionen der Komponente" → „Hauptpositionen" (#7) | `03/#1–#4, #6, #7` |
| Satz zum Werkzeugtipp | richtiggestellt: Raster-Zeile und Tipp sind zwei eigene Formulierungen, nicht wortgleich | `03/#5` |
| Tafel „Ersatz und Restwert" | auf die eine gezeichnete Komponente reduziert, Photovoltaik-Zeilen und variantenweite Zeile „Variante 3 – beide Anlagen" entfernt | `03/#8` |
| Absatz zur Tafel | Futur/Soll-Form → Ist-Form | `03/#9` |
| Zeileneditor | Feld „Positionsart:" (Klappliste) samt Erklärzeile ergänzt, Ressourcentafelzeile erweitert | `03/#12` |
| Worst/Best-Satz | Optionsgruppe „Eingabeart", Umrechnungszeile, Zuschuss-Schalter ergänzt | `03/#13` |
| Summenfuß | vierte Zeile „spezifisch {0} €/kWp" (nur Photovoltaik) als Querverweis erwähnt | `03/#15` |
| Nutzungsdauer-Zellen | Herleitungszeile („15 a · Vorgabe der Technik" / „20 a · gepflegt") bei den drei besetzten Zellen eingezeichnet | `03/#16` |

### 1.6 Kategorie 2 Betriebskosten (Commit `5ccccdf9`, `03/#19, #21–25, #27`)

| Stelle | Änderung | Quelle |
|---|---|---|
| Drei Pflichtzeilen | „von …" → „× …", Anlagenname entfernt, „aus dem Lauf" → „Lauf", „Investition brutto" → „Investitionssumme" | `03/#19` |
| Abzeichen „Pflicht nach VDI 2067" | entfernt (keine Ressource, kein Code); Schloss-Symbol zeigt die Pflicht | `03/#21` |
| Empfehlungszeile | vom Positionsfeld unter das Satzfeld verschoben, Wortlaut auf `KDLG_EMPF_ZEILE` gezogen (Tooltip behält eigenen Wortlaut) | `03/#22, #23` |
| Doppelpflege-Warnung | vor Laufstand und Raster gezogen (vorher danach) | `03/#24` |
| Gruppe „Endenergie je Komponente" | von eigenem Seitenabschnitt nach der Fußleiste in `.f-gruppe` innerhalb des Fensters verschoben, vor die Fußleiste; Spaltenzahl (Q17, gestirnt) unangetastet | `03/#25` |
| Neunter Grund ohne Bezugsgröße | „für den Stromträger des Projekts ist kein Arbeitspreis gepflegt" in Prosa und Ressourcentafel ergänzt (`KDLG_BASIS_GRUND_STROMPREIS`, #365) | `03/#27` |

Ausgelassen (gestirnt/Entscheid offen): #18 (Spaltenfrage Q17), #20 (Entscheid zur rückgerechneten
Menge 21.710 kWh), #26 (Ziel „C" — Codeänderung).

### 1.7 Kategorie 3 Photovoltaik (Commit `463b55bb`, `03/#30–#33, #36`)

| Stelle | Änderung | Quelle |
|---|---|---|
| Zwei Pflichtzeilen | 🗑️ → 🔒 mit dem Werkzeugtipp aus Kategorie 2 | `03/#30` |
| PV-Betriebsraster | „vier Arten" → „fünf Arten", „je kW elektrisch" ergänzt | `03/#31` |
| Fließtext + Knopfleisten | „drei Knöpfe" → „vier Knöpfe"; vierter Rasterknopf „Nutzungsdauern vorbelegen…" in beiden Reiterblättern ergänzt | `03/#32` |
| Erklärzeile | „eigene Werte dieser Variante — Variante Photovoltaik" → ohne Variantenname (Ressource kennt kein `{0}`) | `03/#33` |
| Optionsgruppe/Klappliste Projekt | Hinweis „Entweder/oder, nie beides" ergänzt | `03/#36` |

### 1.8 Kategorie 5 BHKW (Commit `f427db23`, `03/#44, #45, #47–49, #52, #53, #56, #57`; `01/B3, B5, B6`)

| Stelle | Änderung | Quelle |
|---|---|---|
| Wahlfelder-Zählung | „sechs" → „acht" (Aufteilung zählt anlagenbezogen/projektweit doppelt), an zwei Stellen | `03/#44` |
| Feldzählung | „elf editierbare Felder" → „neun Zahlenfelder"; U22-Abnahme „elf" → „dreizehn" | `03/#45` |
| Herleitungen Einspeisung/Eigenstrom | auf vollen Ressourcentext `WIRT_KWKG_HERLEITUNG_TRANCHEN` gezogen (Tranchen einzeln, „Stand" statt „Stichtagsjahr"); `NormEigen` „mit" → „i.V.m." | `03/#47, #48` |
| Kontingent-Herleitung | auf `WIRT_KWKG_KONTINGENT_NEU` gezogen | `03/#49` |
| `BHW_G6`/`BHW_V_STAND` | Zusatz „Jahr 1 (2026)" gestrichen bzw. Wortlaut „Stand: {0} — …" gezogen | `03/#52, #53` |
| Kohärenzprosa | fünf → alle neun `KOH_FALL*`-Fälle genannt | `03/#56` |
| Gruppe Hilfsstrom | bedingte Hinweiszeile für Heizkessel ergänzt (`BHW_H_KESSEL`) | `03/#57` |
| „12.082 €/a" | → „12.079 €/a" (nachgerechnet 12.079,17) | `01/B5` |
| Fall-2-Menge „1.651,3 MWh" | → „1.651,2 MWh" (nachgerechnet 1.651,22), drei Stellen | `01/B6` |
| Energiesteuerwahl Kessel | § 54 als anlagenscharf ausgewiesen, ohne `Beispielprojekt.md` anzufassen | `01/B3` |

Ausgelassen: #46, #51 (Ziel „K (U22)"), #50 (Anzeigezeilen-Entscheid, gestirnt), #54, #55 (Ziel
„K (U6)", nicht im Zitierbereich).

### 1.9 Kategorie 6/7 (Commit `d7bd6f12`, `03/#61, #29, #66, #68`; `01/B12`)

| Stelle | Änderung | Quelle |
|---|---|---|
| Kat. 6 Herkunftszeile | Variantenname gestrichen (`PVW_HERKUNFT_EIGEN` kennt kein `{0}`) | `03/#61` |
| Kat. 3 Knopf „Gesetzesparameter…" (PV-Zweig) | Hinweis „geplant im PV-Zweig" ergänzt (Knopf wird im Code nie gezeichnet) | `03/#29` |
| Kat. 7 Block B | um „brutto" und „abzüglich entgangener Entlastung Netzbezug" je Komponente erweitert, Endbeträge unverändert (293.245,6 / 22.914,0) | `03/#66` |
| Blockköpfe | auf Ressourcentexte `WIRT_ERL_KOPF_A/_B` gezogen (weicht bewusst vom unveränderten zweiten Mockup ab) | `03/#68` |
| § 53a-/§ 54-Zeilen | Marker „Vorschlag U7" an beiden Zeilen (nicht nur einer) | `01/B12` |

Ausgelassen: Fußhinweis-Entscheid (#69, gestirnt), Leistungsanteil Q4 (B2, gestirnt).

### 1.10 Kategorie 8 (Commit `80e67df8`, `03/#91–#95, #86`; `01/B8, B11`; `02/d-18`)

| Stelle | Änderung | Quelle |
|---|---|---|
| Parameterdialog | Klappliste „Vergleichsprojekt" entfernt (steht als Optionsspalte auf der Ergebnisseite, Konzept § 2.9) | `03/#91` |
| Parameterdialog | zwei Gruppen „Rahmen"/„Preissteigerungen" → eine Gruppe „Allgemein" (`WPAR_G_ALLGEMEIN`) | `03/#92` |
| Parameterdialog | Hinweis „Ausschnitt" ergänzt (Dialog führt tatsächlich sechs Gruppen/20 Felder) | `03/#93` |
| Parameterdialog-Fuß | „Abbrechen / Übernehmen" → „Abbrechen / Speichern", erfundene Statuszeile gestrichen | `03/#94` |
| Fußleisten-Knopf | „BHKW…" → „BHKW-Wirtschaftlichkeit…", drei Stellen | `03/#95` |
| Ergebnisseite `.duo` | zwei Fußleisten zusammengeführt, Satz „dieselbe wie im linken Zustand" ergänzt | `03/#86` |
| Wärmegestehungskosten | Formel „(−Kapitalwert × Annuitätsfaktor a) ÷ (Bezugsmenge × 1000)" und Bezugsmenge „1.953,9 MWh/a" ergänzt | `01/B11` |
| Nominalsummen Erlöse | 624.592 → 624.594, 740.510 → 740.512, drei Stellen | `01/B8` |

Ausgelassen: Q20 (Parameterdialog vollständig zeichnen, gestirnt), Hinweistext A14 (gestirnt).

### 1.11 Kategorie 4 (Commit `b8a45e98`, `03/#37, #39, #40`; `00/S12`; `01/B9`)

| Stelle | Änderung | Quelle |
|---|---|---|
| Trägerliste | links neben der Trägerkarte eingezeichnet (Filterfeld, zwei Gruppenköpfe, zwei Knöpfe), neuer Rahmen `.f-traeger-liste`, `.f-mit-liste`-Flex bei < 820px untereinander; Fußleiste um Statuszeile ergänzt | `00/S12` |
| Nachweisumschlag | „Fassung ist 1"/„vier Skalare" → „Fassung 3"/„sieben Skalare", drei fehlende Skalare benannt | `03/#37` |
| Schnellwahl-Überlagerung | fünf → drei Spalten (Satz · Herkunft mit Katalogwert/Jahr · in der Abrechnungseinheit) | `03/#39` |
| Emissionssumme | aus Tabellenzeile gelöst, eigene Textzeile unter dem Raster | `03/#40` |
| Formelzeile Trägerkarte | „0,76 €/m³" → „0,7560 €/m³" (deckungsgleich mit dem Eingabefeld, passend zu Kern-Änderung R5) | `01/B9` |

### 1.12 Messungen Hauptmockup

| Messung | vorher | nachher |
|---|---|---|
| Zeilen / Bytes | 4849 / 391.339 | 5066 / 410.269 |
| Seitenbreite bei 1440px Fenster | 1625 px (Überlauf 185px, waagerechter Rollbalken) | 1425 px — **kein** Seiten-Rollbalken; 183 Elemente ragen über 1440px hinaus, alle 183 in einem Vorfahren mit `overflow-x:auto` enthalten, dessen eigener rechter Rand innerhalb 1440px bleibt (JS-Messung, `getBoundingClientRect`) |
| Seitenhöhe bei 1440px Fenster | 69.438 px | 80.249 px (mehr Inhalt: 43 Navigationsanker, 6 neue Anhangzeilen, Trägerliste, Erläuterungstafeln) |
| Parse-Fehler (HtmlAgilityPack) | — | 0 |
| Doppelte IDs | — | 0 |
| Tote interne Anker | — | 0 |
| Tabellen mit ungleicher Zellenzahl | 0 von 53 | 0 von 54 |

---

## 2 Wechselrichter_Mockup_2026-09-06.html (Commit `1e2b2180`)

Zeilen 1520 → 1513, Bytes 79.408 → 78.827 (CRLF, keine BOM).

| Stelle | Änderung | Quelle |
|---|---|---|
| Absatz Z. ~414–423 | „Nachgetragen mit Stufe S2 …" mit Wellenkürzel W6-E-3 und S3-Vorbehalt entfernt | Auftrag |
| SVG Z. ~1149 | feste `width`/`height` (1320×400) durch `style="max-width:100%;height:auto"` ersetzt, `viewBox` bleibt; eine `@media`-Regel (max-width:900px) ergänzt — die Datei hatte zuvor keine | `06/#1` |
| Fußleiste | „OK · Abbrechen" → „Abbrechen · OK" | Auftrag |
| Herstellerzeile „Ablytek 6MN6A275" | neutralisiert zu „Modul A, 275 Wp" (Name-Feld, Fließtext, SVG-Bildunterschrift); Feld „Hersteller: Ablytek" → „Hersteller A" — an derselben Stelle belassen (Q21 offen) | Auftrag |

Übersprungen: Kennwert 275,19 W in Feldern/Diagrammrechnung nicht gerundet (nicht Teil des
Auftrags für diese Datei).

Wache: 0 Parse-Fehler, 0 doppelte IDs, 0 tote Anker, 0 von 4 Tabellen mit ungleicher Zellenzahl.

---

## 3 stromspeicher-optimierung-v2.html (Commit `f01cef87`)

Zeilen 1172 → 1191, Bytes 67.401 → 68.405 (CRLF, keine BOM).

| Stelle | Änderung | Quelle |
|---|---|---|
| Vier h2-Abschnitte | eigene id (`abschnitt-optimierung`, `-ergebnisse-optimierung`, `-ergebnisse-simulation`, `-was-aendert-sich`) | `06/#5` |
| Kopf | neue `<nav class="mockup-abschnitte">` mit vier Ankern ergänzt; vorhandene `<nav class="epos-ablaufleiste">` (Prozessleiste) unverändert | `06/#5, #6` |
| Herstellerzeile „Shenzhen Growatt New Energy Co., Ltd.: WIT-M+APX ESS" / „Growatt WIT-M+APX ESS" | neutralisiert zu „Speicher 1, 100 kWh", sieben Stellen (HTML-Kommentar, Karten, `data-nur`-Kurzformen, `aria-label`) — an derselben Stelle belassen | Auftrag |

Wache: 0 Parse-Fehler, 0 doppelte IDs, 0 tote Anker, 0 Tabellen (Datei führt keine).

---

## 4 Entwurf_Hydraulikuebersicht_Konfiguration.html (Commit `fea2a974`)

Zeilen 169 → 170, Bytes 15.670 → 15.718 (CRLF, keine BOM).

| Stelle | Änderung | Quelle |
|---|---|---|
| `<head>` | `<meta name="viewport" …>` ergänzt — einzige der sechs Mockup-Dateien ohne sie | `06/#2` |
| Anlagentext „CS6800iAW MB + AW 10" | neutralisiert zu „Wärmepumpe A" — an derselben Stelle belassen | Auftrag |

Wache: 0 Parse-Fehler, 0 doppelte IDs, 0 tote Anker, 0 Tabellen (Datei führt keine).

---

## 5 Katalogfilter_Vorschlag.html (Commit `a9d1dd57`)

Zeilen 1740 → 1774, Bytes 130.535 → 130.111 (CRLF, keine BOM).

| Stelle | Änderung | Quelle |
|---|---|---|
| Herstellerdaten | 10 Hersteller → „Kesselhersteller 1–3" / „Wärmepumpenhersteller 1–3" / „Modulhersteller 1–4"; 60 Typcodes (23 Heizkessel, 15 Wärmepumpen, 22 PV-Module) → „Kessel 1–24" / „Wärmepumpe 1–15" / „Modul 1–22", je Tabelle durchnummeriert; erfasst Zellen, `title`-/`value`-Attribute, HTML-Kommentare, gekürzte „…"-Anzeigetexte (Nachlauf gegen eine Liste bekannter Marken/Typcode-Muster: 0 Treffer) | `Konzept_Hilfesystem_Wikidokumentation.md` §13.2 |
| Vier `<h1>` | drei Dialogtitel zu `<h2>` gestellt, ein `<h1>` (Seitentitel) bleibt | `06/#4` |
| Fußleisten | zwei ohne Abbrechen ergänzt, eine verdrehte auf „Abbrechen · OK" gestellt | Auftrag |
| „?"-Infoknopf | auf ⓘ gestellt, drei Stellen | Auftrag |
| Sieben h2-Abschnitte | eigene id; neue `<nav class="mockup-abschnitte">`, die vor dem Sprung in ein verstecktes Reiterblatt automatisch dessen Reiter aktiviert (`blattZeigen`) | `06/#5, #21` |

Übersprungen: Datenblattwerte (Zahlenspalten) nicht gerundet — bei ~60 Zeilen mit
Querbezug zu den „Kenndaten"-Karten wäre das ein eigener Auftrag; die identifizierenden
Namen/Typcodes (der eigentliche Wiki-Regel-Konflikt) sind vollständig neutralisiert.
Verschiebung der Datei nach `ueberholt/` nicht vorgenommen (Q21 offen). Test-Gegenprobe
`EPOS.Kern.Tests/DokumentationLinkWacheTests.cs` (Code) nicht angefasst.

Wache: 0 Parse-Fehler, 0 doppelte IDs, 0 tote Anker, 0 von 6 Tabellen mit ungleicher Zellenzahl.

---

## 6 Kopfblöcke der Nebenkonzepte

Belege: `02_Konsistenz_Papiere.md` f-6…f-8, j-2, j-5; jede Aussage stichprobenhaft gegen das
eigene Kapitel des Papiers geprüft (grep nach den genannten Kapiteln/Commit-Hashes), nicht
gegen den Code — reine Innenwiderspruch-Korrektur.

| Datei · Stelle | Änderung | Quelle | Commit |
|---|---|---|---|
| `Konzept_Katalogfilter_EPOS-Plan.md` Z. 8–12 | „Stufen S2 und S3 stehen aus" widersprach Kapitel 9 desselben Papiers („Stufe S2 ist umgesetzt", „Stufe S3 ist umgesetzt", mit Commits/Zweignamen) → „ALLE DREI STUFEN SIND UMGESETZT" mit allen drei Zweignamen | `f-6`, `j-2` | `7b480b5d` |
| `Konzept_Wechselrichter_EPOS-Plan.md` Z. 26–27 | „S2 und S3 sind es nicht" widersprach Z. 3 und Z. 38 desselben Papiers (Commits `40fc542`/`c02cd99`/`d88243e`) → „sind die Stufen S1, S2 und S3 umgesetzt" | `f-7` | `685b10ed` |
| `Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md` Nachtrag 6 | „Nichts davon ist umgesetzt — zehn Entscheidungsfragen … liegen beim Anwender" berichtigt (alle zehn laut `Konzept_Wechselrichter_EPOS-Plan.md` entschieden, S1–S3 umgesetzt); „Migrationsschritte ab 65" → „ab 97" | `f-8` | `a7918152` |
| `Konzept_Stromspeicher_Dialoge_EPOS-Plan.md` § 8.4 Z. 982 | „wird auf diesen Stand nachgezogen" → „ist auf diesen Stand nachgezogen" (Mockup-Kopf selbst vermerkt „NACHGEZOGEN AUF …" zweimal) | `j-5` | `7f8ea90d` |

Alle vier: CRLF, keine BOM, byte-erhaltend.

---

## 7 Konzept_Hilfesystem_Wikidokumentation.md (Commit `edc3c6fa`)

Zeilen 971 → 972, Bytes 69.706 → 69.806 (CRLF, keine BOM, +1 Zeile).

Wiki-Quelle `Projekte/Wiki/Programm Dokumentation - Klimadaten.wiki` existiert (vorher
geprüft) — Zeile `Programm Dokumentation/Klimadaten` fehlte in der Tabelle „Bedienungsseiten
mit Repo-Quelle" (elf statt zwölf Zeilen) und wurde ergänzt. Sonst nichts an der Datei
angefasst.

---

## 8 Dokumentation/LIESMICH.md (Commit `6c466a07`)

Zeilen 300 → 300, Bytes 58.591 → 58.591 (CRLF, keine BOM, reine Datumsänderung).

Elf Zeilen des Ordners `aktuell/Wirtschaftlichkeit_Kosten/` (LIESMICH, Konzept, Beispielprojekt,
Rechenweg 01–08) trugen alle das Datum 2026-09-02. Auf das jeweils letzte Änderungsdatum
gesetzt (`git log -1 --format=%ad --date=short -- <Datei>`):

| Datei | Datum vorher | Datum nachher |
|---|---|---|
| LIESMICH.md | 2026-09-02 | 2026-09-19 |
| Konzept_…_konsolidiert.md | 2026-09-02 | 2026-09-19 |
| Beispielprojekt.md | 2026-09-02 | 2026-09-18 |
| Rechenweg/01 | 2026-09-02 | 2026-09-18 |
| Rechenweg/02 | 2026-09-02 | 2026-09-19 |
| Rechenweg/03 | 2026-09-02 | 2026-09-18 |
| Rechenweg/04 | 2026-09-02 | 2026-09-18 |
| Rechenweg/05 | 2026-09-02 | 2026-09-18 |
| Rechenweg/06 | 2026-09-02 | 2026-09-19 |
| Rechenweg/07 | 2026-09-02 | 2026-09-18 |
| Rechenweg/08 | 2026-09-02 | 2026-09-19 |

Per Diff geprüft: genau elf Zeilen geändert (11 Einfügungen/11 Löschungen), keine andere
Zeile der Datei angefasst.

---

## 9 Wache

`dotnet test EPOS.Kern.Tests/EPOS.Kern.Tests.csproj -c Release --filter
"FullyQualifiedName~DokumentationLinkWacheTests|FullyQualifiedName~RepositoryOrdnungWacheTests"
--logger "console;verbosity=minimal" -nologo`

**Bestanden: Fehler 0, erfolgreich 19, übersprungen 0, gesamt 19** (erster Bau ~1 Minute
Restore/Build, danach 1s Testlauf).

HtmlAgilityPack-Strukturprüfung (eigenes dotnet-Dateiskript, Muster wie
`06_Struktur_Darstellung.md`: Parse-Fehler, doppelte IDs, tote interne Anker, Tabellen mit
ungleicher Zellenzahl je Zeile inkl. `colspan`) über alle sechs Mockup-Dateien inkl. des
unveränderten zweiten Mockups `Ergebnis_Bandbreite_Herkunft.html` als Gegenprobe: **0 Treffer
in jeder der sechs Kategorien, in jeder Datei.** `git diff --stat` bestätigt zusätzlich, dass
`Ergebnis_Bandbreite_Herkunft.html` unverändert blieb.

---

## 10 Tabuwort-Regex auf neu geschriebene Mockup-Texte

Regex `seit (dem|der|W)|geändert|Entscheid|Befund|W\d+[a-z]?[‑-][A-Z][‑-]\d+|Stand:? *\d|bisher|
früher|vorher|Bis dahin|Migrationsschritt`, angewendet auf die von dieser Sitzung neu
hinzugefügten Zeilen (`git diff 455edfd4 -- <Datei> | grep '^+'`), nur gezählt und gemeldet,
nichts geändert:

| Datei | Treffer | Einordnung |
|---|---|---|
| Dialog_Formel_Zahlenprobe.html | 13 | 8 in der Anhangtafel Umsetzungsstand (per Definition kein Wiki-Text — internes Statusregister); 4 als wortgetreue Wiedergabe von Ressourcentexten der Anwendung („Stand {0}", „Stand: {0}" aus `WIRT_KWKG_HERLEITUNG_TRANCHEN`/`WIRT_KWKG_KONTINGENT_NEU`/`BHW_V_STAND`); 1 („Entscheidungsgröße") als Normbegriff, nicht als Meta-Aussage über den Papierstand |
| Wechselrichter_Mockup_2026-09-06.html | 0 | — |
| stromspeicher-optimierung-v2.html | 0 | — |
| Entwurf_Hydraulikuebersicht_Konfiguration.html | 0 | — |
| Katalogfilter_Vorschlag.html | 1 | „Befund D‑2" — vorhandener Text einer Zeile, in der nur der Typcode „eloBLOCK VE 10" → „Kessel 24" ersetzt wurde; „Befund" selbst stammt nicht aus dieser Sitzung |

Keine der 14 Fundstellen ist als Wiki-Vorlage vorgesehen (die Mockups gehen nicht direkt ins
Wiki; Wiki-Text entsteht in einem eigenen Auftrag nach Konzept Hilfesystem § 13.3/13.4).

---

## 11 Übersprungenes (mit Entscheidkennung)

| Was | Wo | Grund |
|---|---|---|
| Spaltenfrage vier/sechs Spalten „Endenergie je Komponente" | Hauptmockup Kat. 2 | Q17, Entscheid offen |
| Rückgerechnete Menge „21.710 kWh" | Hauptmockup Kat. 2 | Entscheid offen (Kern verwirft die Menge ausdrücklich) |
| Fußleistenregel (OK/Abbrechen/Speichern einheitlich) | Hauptmockup Kat. 1–8 | Q8, Entscheid offen |
| Anzeigezeile „Anteil Neuherstellungskosten" | Hauptmockup Kat. 5 | Entscheid offen (streichen oder bauen) |
| Leistungsanteil −4.180,0 € Herleitung | Hauptmockup Kat. 7 | Q4, Entscheid offen |
| Fußhinweis „Block B wird nicht summiert" als eigene Zeile | Hauptmockup Kat. 7 | Entscheid offen (Zeile bauen oder streichen) |
| Parameterdialog vollständig zeichnen | Hauptmockup Kat. 8 | Q20, Entscheid offen |
| Hinweistext A14 | Hauptmockup Kat. 8 | Entscheid offen |
| Google-Schrift einbetten/Systemschriften | Hauptmockup Kopf | Q22, Entscheid offen |
| Ablösung des zweiten Mockups | `Ergebnis_Bandbreite_Herkunft.html` | Q1, Entscheid offen — Datei nicht angefasst |
| Verschiebung nach `ueberholt/` | Katalogfilter_Vorschlag, Wechselrichter_Mockup, stromspeicher-optimierung-v2, Entwurf_Hydraulikuebersicht | Q21, Entscheid offen |
| Datenblattwerte (Zahlenspalten) runden | Katalogfilter_Vorschlag.html, drei Katalogtabellen | Umfang (≈60 Zeilen mit Querbezug zu Detailkarten) sprengt diesen Auftrag; Namen/Typcodes sind vollständig neutralisiert, die Zahlen bleiben anonym ohne Produktbezug |
| 82 Rasterzellen ohne `title` (Spaltenkopf statt Zeilenlabel) | Hauptmockup, diverse Kategorien | Zuordnung je Tabelle wäre ein eigener Auftrag; die 38 Zellen mit eindeutigem `<label>` sind versehen |
| „Pflicht nach VDI 2067"-Abzeichen an den PV-Pflichtzeilen | Hauptmockup Kat. 3 | Nicht im zitierten Befundbereich (`03/#30–#33, #36`) für Kat. 3; dieselbe Inkonsistenz wie `03/#21` (Kat. 2), dort behoben |

---

## 12 Commits

```
c383d583 E0b: Hauptmockup — Randbreite, zweistufige Navigation, Struktur
e6cb23a8 E0b: Hauptmockup — Rahmenstile, ⓘ/×, Erläuterungstafeln
470c0063 E0b: Hauptmockup — Anhang Umsetzungsstand: Stand-Spalte, U40-U45
ae8bc34e E0b: Hauptmockup — KWKG-Schlüssel, Projekt-Klappliste, Übernahmetext
75e30336 E0b: Hauptmockup — Kategorie 1 Raster an Code und Ressourcen gezogen
5ccccdf9 E0b: Hauptmockup — Kategorie 2 Betriebskosten an Code gezogen
463b55bb E0b: Hauptmockup — Kategorie 3 Photovoltaik an Code gezogen
f427db23 E0b: Hauptmockup — Kategorie 5 BHKW: Zählungen, Ressourcentexte, Zahlen
d7bd6f12 E0b: Hauptmockup — Kategorie 6/7: Herkunftszeile, Block B, U7-Marker
80e67df8 E0b: Hauptmockup — Kategorie 8: Parameterdialog, Fußleisten, Formel
b8a45e98 E0b: Hauptmockup — Kategorie 4: Trägerliste, Fassung 3, Schnellwahl
1e2b2180 E0b: Wechselrichter-Mockup — Absatz, SVG, Fußleiste, Herstellerdaten
f01cef87 E0b: Stromspeicher-Mockup — Abschnittsnavigation, Herstellerdaten
fea2a974 E0b: Hydraulikuebersicht-Entwurf — viewport-Meta, Typcode
a9d1dd57 E0b: Katalogfilter-Mockup — Herstellerdaten, h1, Fußleisten, Navigation
7b480b5d E0b: Konzept Katalogfilter — Kopfblock auf Kapitel 9 gezogen
685b10ed E0b: Konzept Wechselrichter — Kopfblock auf Kapitel 8 gezogen
a7918152 E0b: Konzept PV-Ertragsmodell — Nachtrag 6 auf den heutigen Stand
7f8ea90d E0b: Konzept Stromspeicher-Dialoge — Mockup-Satz in Vergangenheitsform
edc3c6fa E0b: Konzept Hilfesystem — fehlende Klimadaten-Zeile ergänzt
6c466a07 E0b: LIESMICH — Datumsspalte der Wirtschaftlichkeit_Kosten-Zeilen
```

`git status` am Ende dieser Sitzung: sauber (keine offenen Änderungen).
