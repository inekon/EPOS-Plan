# Faktenblatt E8 — Wirtschaftlichkeit EPOS-Plan (fünf ValERI-Blöcke, Formelmappe, Anhang-Gegenprobe)

Quellenstand: Hauptbaum `C:\Waermeplan\EPOS-Plan`, Stand 591229e1. Nur gelesen, nichts verändert,
kein Build, kein Test. Zeilennummern beziehen sich auf die genannten Dateien in diesem Baum.
Pfade sind relativ zu `C:\Waermeplan\EPOS-Plan`, sofern nicht anders angegeben.

---

## 1. E8 laut Analysepapier

### § 5 Zeile E8 (Umsetzungsplan), wörtlich

`Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`,
Zeile 457 (Zeile der Etappentafel § 5; Spaltenköpfe in Zeile 448: Etappe · Inhalt · Größe ·
Rechenwirkung · Nachweis · Schema · Wiki · Modell · Voraussetzung):

> | **E8 V‑C und V‑D** | fünf ValERI-Blöcke hinter dem Umschalter (die Blöcke 1, 3, 4 und 5 mit E5 vorgezogen, offen Block 2); Formelbericht Stufe 0 (Parameterblock), 1 (Mehrjahrestabelle), 2 (NBW/RMZ/IKV über Differenzreihe), 3 (Betriebskostenblock); Anhang-E-Checkliste (U43); Anhang-D-Gegenprobe gegen `KapitalwertRechner.Rechne`; aus E5 nach Empfehlung zu Frage (4) in Nach #434: Nominalsummen, Differenzspalte und Brückenbild der Gliederung, Tafel „Was daraus im Lauf wird", Fußzeile „Drei Szenarien gerechnet…" | L | keine (Werte bleiben gleich) | Blattstruktur-Wache vorher/nachher, Kern-Fall mit Normsollwerten | — | Wirtschaftlichkeit `bericht`, je Stufe ein Logbuch-Satz | Opus; Fable für die Stufenauslegung und die ClosedXML-Fragen | E1, E5; ClosedXML-Fragen aus `05/§ 3.3` geklärt |

Damit: Größe **L**, **keine** Rechenwirkung, Nachweis über eine Blattstruktur-Wache
(vorher/nachher) und einen Kern-Fall mit Normsollwerten, **kein** Schemaschritt, Wiki-Stichwort
`bericht` mit je Stufe einem Logbuch-Satz, Modell Opus (Fable nur für Stufenauslegung und
ClosedXML-Fragen), Voraussetzung E1 und E5 sowie die geklärten ClosedXML-Fragen aus `05/§ 3.3`.

Kopfzeile des Papiers, Zeile 46: „… ausgeführt. **Nächste Etappe: E8.** Die Schemaschritte sind am
23.09.2026 neu geordnet (§ 6): …" — E8 bekommt keinen neuen Schemaschritt (Schema-Spalte „—").

### Stand der Etappen (23.09.2026) — Einordnung von E8

Zeile 506–509 (Prosa unmittelbar nach der Etappentafel, Abschnitt „Stand der Etappen am
23.09.2026"):

> „**E4 — umgesetzt #432, E5 — umgesetzt #434, E6 — umgesetzt #436, E7 Teil a — umgesetzt #437, E7 Teil b — umgesetzt #439, E7 Teil c1 — umgesetzt #440, E7 Teil c2 — umgesetzt #446, E7 Teil c3 — umgesetzt #452** (Einzelheiten in der Tafel oben, in den Statuszeilen und ihren Protokollen) — **E7 ist abgeschlossen**; der **A13-Schnitt** ist mit #435 ausgeführt. **Nächste Etappe:** **E8** (die fünf ValERI-Blöcke samt Block 2 und Zahlungsstrombild U42, die Formelmappe Stufen 0 bis 3, die Anhang-E-Checkliste U43, die Anhang-D-Gegenprobe, Nominalsummen, Brückenbild, „Was daraus im Lauf wird" und Fußzeile U41/U46–U48, E6‑Q1; vorab die ClosedXML-Fragen aus `05/§ 3.3`); die acht Fragen aus E7c3 sind offen. **E8 bis E12** bleiben offen, E11 entfällt."

Zeile 518–520 (Begründung der Etappenreihenfolge, letzter Satz von § 5):

> „E8 ist die einzige Etappe mit belastbarer Beschreibung im Konzept (§ 2.11.6)."

**Wichtig:** Diese Prosa (Stand 23.09.2026) zählt **E6‑Q1** ausdrücklich zu E8. Die Tabellenzeile
selbst (oben, Zeile 457, sprachlich auf dem Stand vor der E6‑Q1-Entscheidung vom 23.09.2026)
erwähnt E6‑Q1 dagegen **nicht** — siehe „Unklarheiten" unten.

### Was § 3 zu den E8-Punkten sagt

§ 2.4 Nebenkonzepte, Zeile 212 und 214 (Tafel VALERI-Stand):

> | VALERI | V‑C Ansicht, fünf Blöcke, Umschalter | fehlt | `04/§ 6` |
> | VALERI | V‑D Formelbericht, Anhang E, Anhang D | fehlt; 0 Formeln repoweit; Normtext und Vorlage liegen unter `Quellen/VALERI/` | `05/§ 3, § 7` |

§ 3.3 Ergebnisansicht, ValERI, Verlauf, Bericht, Zeile 293 (Befund A1):

> „Der Stand der Seite kennt weder Bandbreite (U4) noch Empfehlungskarten je Version (U5, nur `Empfehlungszeile`), Gliederung, Brücke, die fünf ValERI-Blöcke oder den Szenario-Hinweistext (U10, `WIRT_SZEN_HINWEIS` repoweit 0 Treffer); statt des Umschalters (K8/V‑1) ein Aufklappblock mit einem Textfeld. **Stand 22.09.2026: gebaut #434** — Umschalter (K8/V‑1) mit vier Abschnitten, Bandbreite (U4), Empfehlungskarten je Version (U5), Hinweistext `WIRT_SZEN_HINWEIS` (U10), Gliederung ohne doppelte Kennzahlzeilen, die Blöcke 1, 3, 4 und 5 als Darstellung „ValERI-Bewertung"; **Rest:** Brückenbild, Block 2 und damit die fünf Blöcke vollständig (E8); der Verlauf mit drei Szenarien ist **gebaut #436** (A3)"

§ 3.3, Zeile 294 (Befund A2):

> „V‑A: keine „nachrichtlich"-Kennzeichnung, keine Deklarationszeilen (nominal, Steuern, Restwert, Risiko), keine IZF-Mehrdeutigkeitswarnung […]. **Stand 22.09.2026: gebaut #434** — […] V‑A ist damit vollständig; aus demselben Normumkreis bleiben offen die Sensitivität mit T-Variation, Endzahlungen und Diagramm (Konzept V‑G6) und **die Formelmappe (V‑D, E8)**"

§ 3.3, Zeile 296 (Befund A4, Messung des Formelberichts):

> „Formelbericht: Messung des Konzepts bestätigt — **0 Formeln, 167 `.Value`, fünf Parameterzugriffe, keine Zahl des Parametersatzes erreicht eine Zelle**; 1 412 statt 1 380 Zeilen; **auch der Word-Generator ist ungedeckt**; vor Stufe 0 offen: ob ClosedXML 0.105.1 Formeln mit zwischengespeichertem Wert ablegt, ob `RecalculateAllFormulas` NBW/RMZ/IKV trägt, ob andere Tabellenkalkulationen dieselben Werte zeigen"

§ 3.3, Zeile 299 (Befund A7, Anhang-E/-D-Stand):

> „Anhang-E-Checkliste und Anhang-D-Gegenprobe: **nichts gebaut**; Normtext und `VALERI_Vorlage_V7.xlsx` liegen unter `Quellen/VALERI/`; die Fallstudie gehört als Prüfvorrichtung gegen `KapitalwertRechner.Rechne`, nicht gegen die volle Kette; **zwei Zeilen der Sensitivitätstafel D.6 tragen im Normtext Werte des Pumpenbeispiels und dürfen nicht in die Vorrichtung**"

§ 3.5 Nachweis und Tests, Zeile 320 (Befund N4):

> „`ExcelBerichtGenerator.Erzeuge` und `WordBerichtGenerator.Erzeuge` werden von keinem Test gerufen; **ohne Wache über die Blattstruktur ist keine Stufe des Formelberichts abnehmbar**"

### Was § 4 zu den E8-Punkten sagt

**Negativer Befund:** Keine der zwanzig Entscheidfragen A1–A20 in § 4 („Entscheide, die die
Umsetzung braucht", Zeilen 374–441) ist E8 zugeordnet. Die zweite Tafel dieses Paragrafen
(„Umsetzungsstand / Etappe", Zeilen 417–437) weist jedem A-Punkt eine Etappe zu — E3, achtmal E7
(A2, A3, A4, A6, A9, A20 u. a.), E9, E10, E11 (entfällt), E12 oder „eigener Auftrag"; **E8 kommt in
dieser Spalte kein einziges Mal vor.** § 4 des Analysepapiers sagt damit inhaltlich **nichts** zu
den E8-Punkten. Die für E8 einschlägigen Entscheide stehen stattdessen im Entscheidungsregister
(Familien R‑Q [Q18], R‑E5 [E5b‑4-Folgeentscheid 22.09.2026] und R‑E6 [E6‑Q1, 23.09.2026]), nicht in
§ 4 des Analysepapiers.

---

## 2. Fünf Blöcke

### Konzept § 2.11.3 „Die fünf Darstellungsblöcke" (Zeile 782–792)

`Dokumentation/aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`,
Zeile 782–792:

> „### 2.11.3 Die fünf Darstellungsblöcke
>
> Andockung nach § 2.10 in der Wirtschaftlichkeitsseite (`WirtschaftlichkeitSeite.razor`), Referenz nach § 2.9. Alle Blöcke rendern vorhandene Größen; Beispielzahlen in den Mockups aus der Höfingen-Mappe (20-kW-BHKW-Erneuerung gegen benannte Vergleichsheizung — Kapitalwert 65.259 €, IZF 20,4 %, Amortisation 4,33 a).
>
> | Block | Inhalt | Normbezug |
> |---|---|---|
> | **Investitionskosten** | Anlage · Referenz · Differenz; Zuschusszeile; je Szenario | 6.1, 7.3 |
> | **Betriebskosten** | Positionsliste mit den Normattributen Zeitpunkt · Preisrate · Degradation · Quelle | 6.3.1 |
> | **Erlöse** | die Rubrik aus § 2.6 — in der Differenzsicht sind vermiedene Bezüge reguläre Differenz-Cashflows (die Referenzkosten laufen als Gegenposition), Block-B-Kennzeichnung bleibt für die Absolutsicht | 6.1 |
> | **Energiekosten** | je Träger, Anlage gegen Referenz, BEHG mit Preispfad, Preisraten-Ausweis | 6.3.2 |
> | **Wirtschaftlichkeit über Nutzungsdauer** | kumulierter diskontierter Cashflow (drei Szenarien), NPV-Regel, Kennzahlen mit „nachrichtlich"-Kennzeichnung, Sensitivitätstafel mit Steigung €/%, Deklarationszeilen (nominal · Steuern · Restwert · Risiko) | 7, 8, Anhang A |"

**Achtung Doppelbelegung des Begriffs** (siehe „Unklarheiten"): Diese fünf Blöcke
(Investitionskosten/Betriebskosten/Erlöse/Energiekosten/Wirtschaftlichkeit über Nutzungsdauer)
sind **nicht namensgleich** mit den fünf nummerierten Darstellungsblöcken „1 Gegenstand und
Rahmen … 5 Deklarationen", die § 2.11.4, das Mockup und der Code tragen (unten). § 2.11.3 selbst
verweist nicht auf die Nummerierung 1–5, die im gebauten Code steht.

### Konzept § 2.11.4 „Etappen und Entscheidungen" — Zeilen V-C und V-D (Zeile 796–810)

Zeile 796–801 (Tafelkopf):

> „### 2.11.4 Etappen und Entscheidungen
>
> Die Spalte „entspricht / bereits geliefert durch" löst die zweite Etappenreihe des Szenarienkonzepts (`W5‑B‑9` … `W5‑B‑12`) gegen diese auf — beide Reihen meinen teilweise dieselbe Arbeit."

Zeile 806 (V-C):

> | **V-C** | ValERI-Ansicht (fünf Blöcke + Cashflow-Chart) in der Wirtschaftlichkeitsseite | teilweise vorgezogen mit **#434**: die Darstellung „ValERI-Bewertung" hinter dem Umschalter mit den Blöcken 1, 3, 4 und 5; das Cashflow-Bild steht mit **#436** als Verlauf mit drei Szenarien unter „Wie sicher ist das?" der Darstellung „Kennzahlen" (ob auch in Block 4: Frage E6‑Q1, → Register R‑E6); offen Block 2 (Zahlungsreihen, an seiner Stelle eine Hinweiszeile) | Ausweis | **E8** |

Zeile 807 (V-D):

> | **V-D** | XLSX-Formelbericht nach Anhang-A-Raster + Berichtsinhalte a)–d) + Anhang-E-Checkliste; **Gegenprobe an der Anhang-D-Fallstudie** | deckt sich mit **V-G10** (Entscheid 18.09.2026, § 2.11.6) | Ausgabe | **E8** |

Zeile 810 (Fußnote der Tafel):

> „*Die Spalte „Stand" verweist auf den Etappenplan E0–E12 des Analysepapiers […] § 5; gebaut sind daraus E0 (#379), E1 (#380), E2 (#405), E3 (#431), E4 (#432), E5 (#434) und E6 (#436).*"

### Mockup: Zone „ValERI-Bewertung — die fünf Blöcke" (Zeile 4469–4494)

`Dokumentation/aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Zeile 4469–4494, wörtlich:

> „ValERI-Bewertung — die fünf Blöcke
>
> | Block | Inhalt | Normbezug |
> |---|---|---|
> | 1 · Gegenstand und Rahmen | Maßnahme, Referenz (wählbares Vergleichsprojekt), Betrachtungszeitraum, Kalkulationszins, Rechnung nominal | 6.1 · 7.3 |
> | 2 · Zahlungsreihen | Investition, Betriebskosten, Energiekosten, Erlöse, Ersatz, Restwert — je Jahr und als Barwert, in der Gliederung oben | 6.1 bis 6.4 |
> | 3 · Kennzahlen | Kapitalwert als einziges Maß; Annuität, interner Zinsfuß und dynamische Amortisation nachrichtlich | 7 · Anhang C |
> | 4 · Unsicherheit | drei Szenarien mit vollständigem Lauf, Bandbreite, Verlauf, Sensitivitätstafel | 7.3 · 8.1.3 |
> | 5 · Deklarationen | nominal gerechnet · Energie- und Stromsteuerentlastungen berücksichtigt · Ertragsteuern nicht · keine Abschreibungen als Zahlung · Restwert linear (dokumentierte Abweichung von 6.4) · Risikozuschlag nicht angesetzt · nicht monetäre Wirkungen benannt | 7 · 9 · Anhang E |"

Direkt darunter, Zeile 4491–4494 (Erläuterung):

> „Die **Sensitivitätstafel** beziffert je Größe, wie stark der Kapitalwert auf eine Änderung reagiert; jede Zeile ist die Differenz zweier vollständig neu gerechneter Zahlungsbilder, keine Ableitung. Die **Anhang-E-Checkliste** hakt die Punkte der Norm ab, die das Papier belegen muss, und nennt zu jedem die Stelle im Bericht."

Diese Mockup-Tafel entspricht **wörtlich** der im Code gebauten Nummerierung (Block1…Block5,
s. u.) — und nennt in Block 4 ausdrücklich „Verlauf" als Inhalt, was der Grund für die Frage
E6‑Q1 ist (Abschnitt 10 unten).

### Code: heutiger Stand der fünf Blöcke

`EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor`, Zeile 21–22 (Kopfkommentar der Datei):

> „- "ValERI-Bewertung": die Bloecke 1, 3, 4, 5 der Norm; an der Stelle von
>   Block 2 (Zahlungsreihen, E8) EINE benannte Hinweiszeile."

Zeile 444–451 (Kommentar vor der ValERI-Darstellung):

> „@* ============== Darstellung "ValERI-Bewertung" (V-1, V-C) ==============
>
>    IN DIESER ETAPPE NUR, WAS DATEN HAT: Block 1 (Gegenstand und Rahmen aus
>    dem Parametersatz), Block 3 (Kennzahlen), Block 4 (Unsicherheit) und
>    Block 5 (Deklarationen). Block 2 - die Zahlungsreihen je Jahr samt
>    Zahlungsstrombild - kommt mit E8; an seiner Stelle steht EINE benannte
>    Hinweiszeile, kein leerer Rahmen. […] *@"

Zeile 452–459: Block 1 — `<Gruppenkopf Titel="@Texte.Block1" Summe="@Texte.Norm1">`, zeichnet die
Rahmentafel (`_stand.Ansicht.Rahmen`), sofern Zeilen vorhanden sind, plus eine `Herleitungszeile`
mit `@Texte.RechnungNominal` — gebaut.

Zeile 462–469: Block 2 — `<Gruppenkopf Titel="@Texte.Block2" Summe="@Texte.Norm2">` enthält nur
einen `div.epos-wirt-block-luecke` mit `<Herleitungszeile Text="@Texte.Block2Hinweis" />` — **kein
Datenblock**, nur die im Kopfkommentar angekündigte Hinweiszeile (E8-Lücke).

Zeile 470–475: Block 3 — `<Gruppenkopf Titel="@Texte.Block3" Summe="@Texte.Norm3">` zeichnet
`@Kennzahlteil` plus eine `Herleitungszeile` mit `@Texte.KennzahlHinweis` — gebaut.

Zeile 477–491: Block 4 — `<Gruppenkopf Titel="@Texte.Block4" Summe="@Texte.Norm4">` zeichnet
`@Bandbreitenteil`, bedingt eine Empfehlungs-`Herleitungszeile`, bedingt den
Szenariohinweis, dann `@Sensitivitaetsteil`. **Weder `@Spannenteil` noch
`<KapitalwertVerlaufAbschnitt>` stehen hier** — das ist genau die Lücke, die E6‑Q1 stellt (Abschnitt
10 unten).

Zeile 496–515: Block 5 — `<Gruppenkopf Titel="@Texte.Block5" Summe="@Texte.Norm5">` zeichnet die
Deklarationszeilen (`_stand.Deklarationen`), die Zeile „nicht monetäre Wirkungen", die
Nachweiszeile und `@BerichtKnopfteil` — gebaut.

`EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeiteTexte.cs`, Zeile 96–119 (Textschlüssel der Blöcke):

```
96:  public string Block1 { get; set; } = T("WIRT_VALERI_BLOCK_1", "1 · Gegenstand und Rahmen");
98:  public string Block2 { get; set; } = T("WIRT_VALERI_BLOCK_2", "2 · Zahlungsreihen");
100: public string Block3 { get; set; } = T("WIRT_VALERI_BLOCK_3", "3 · Kennzahlen");
102: public string Block4 { get; set; } = T("WIRT_VALERI_BLOCK_4", "4 · Unsicherheit");
104: public string Block5 { get; set; } = T("WIRT_VALERI_BLOCK_5", "5 · Deklarationen");
107: public string Norm1 { get; set; } = T("WIRT_VALERI_NORM_1", "DIN EN 17463 · 6.1 · 7.3");
109: public string Norm2 { get; set; } = T("WIRT_VALERI_NORM_2", "DIN EN 17463 · 6.1 bis 6.4");
111: public string Norm3 { get; set; } = T("WIRT_VALERI_NORM_3", "DIN EN 17463 · 7 · Anhang C");
113: public string Norm4 { get; set; } = T("WIRT_VALERI_NORM_4", "DIN EN 17463 · 7.3 · 8.1.3");
115: public string Norm5 { get; set; } = T("WIRT_VALERI_NORM_5", "DIN EN 17463 · 7 · 9 · Anhang E");
118: public string Block2Hinweis { get; set; } = T("WIRT_VALERI_BLOCK_2_HINWEIS", …
```

Diese Konstanten (Titel- und Normtext je Block) stimmen exakt mit der Mockup-Tafel oben überein —
der Code hat also bereits das Gerüst für alle fünf Blöcke, nur Block 2 hat noch keine Daten.

Mockup-Anhang, Zeile 4558 (Ressourcenschlüssel-Tafel, Zeile zur Darstellung „ValERI-Bewertung"):

> „Darstellung „ValERI-Bewertung": Blockköpfe „1 · Gegenstand und Rahmen" bis „5 · Deklarationen", Normbezug „DIN EN 17463 · …", Zeile „Maßnahme", Einordnung unter der Kennzahltafel, Hinweiszeile an der Stelle von Block 2 | `WIRT_VALERI_BLOCK_1 … WIRT_VALERI_BLOCK_5 · WIRT_VALERI_NORM_1 … WIRT_VALERI_NORM_5 · WIRT_VALERI_MASSNAHME · WIRT_VALERI_KZ_HINWEIS · WIRT_VALERI_BLOCK_2_HINWEIS` | Darstellung „ValERI-Bewertung""

---

## 3. Formelmappe 0–3

### Konzept § 2.11.6 „Formelbericht — Stufenplan und Grenze" (Zeile 859–899)

`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`, Zeile 859–868 (Kopf und Stufentafel):

> „### 2.11.6 Formelbericht — Stufenplan und Grenze
>
> **Der ganze Bericht wird formelbasiert, nicht nur das ValERI-Blatt** (V-G10, das kippt V-2 — → Register R‑V). Die Grenze ist am Generator gemessen und gegengelesen; die Messung steht im Protokoll § 3.11 und begrenzt den Stufenplan unten.
>
> **Stufenplan** — jede Stufe ein eigener Schritt mit Gegenprobe (die Mappe muss vor und nach der Stufe dieselben Werte zeigen; die Formelfassung wird gegen die Wertfassung gehalten):
>
> | Stufe | Inhalt | Warum in dieser Reihenfolge |
> |---|---|---|
> | **0** | **Parameterblock aus echten Zellen**: Kalkulationszins, Betrachtungszeitraum, die drei Preissteigerungen (Energie, Betrieb, Investition/Ersatz), je Szenario ein Satz; absolute Bezüge darauf | Voraussetzung für alles Weitere — solange die Annahmen Prosa in einer Zelle sind, lässt sich keine Formel verankern |
> | **1** | **Mehrjahrestabelle** des Wirtschaftlichkeitsblatts (das Raster steht: Jahre als Zeilen, Zahlungspositionen als Spalten): *Energie* als Fortschreibung Jahr 1 × (1+p_E)^(t−1); *Netto* als Zeilensumme; *Barwert* als Netto × (1+i)^−t; *Kumuliert* als Laufsumme; *Betrieb* als zwei Terme (Betriebs-Topf mit p_B, Endenergie-Topf mit p_E) mit Stufenlogik oder Hilfsspalte für Positionen mit späterem Startjahr; *BEHG* nur im Rückfallzweig als Fortschreibung, mit jahresscharfer CO₂-Reihe bleibt sie zugelieferter Preispfad | größter Nutzen: genau diese Größen variiert der Anwender im Gespräch, und die Tabelle zieht mit |
> | **2** | **Ergebniskennzahlen**: Nettobarwert und Annuität über NBW/RMZ auf die Spalten der Stufe 1; interner Zinsfuß und Amortisation über eine **Differenzreihe Variante − Referenz** (samt Restwert-Nominaldifferenz im letzten Jahr), die das Blatt heute nicht führt und je Variante bekommt; benannter Leerwert bei fehlendem Vorzeichenwechsel als Text, nicht als Zellfehler | die Kennzahlen hängen an Stufe 1 und an einer Reihe, die erst entstehen muss |
> | **3** | **Betriebskostenblock**: Menge und Satz in eigene Spalten, Betrag als Produkt — nur für bemessene Positionen; die Spalte Herleitung bleibt für feste, szenariogepflegte und unvollständige Positionen. **Delta-Block** des Vergleichsblatts als Zellbezug (Wert − Referenz) / |Referenz| | kleine Blöcke gleicher Mechanik; kosmetisch |"

Zeile 870–878 (was dauerhaft Werte bleibt, nicht formelbasiert wird):

> „**Dauerhaft Werte bleiben**, weil sie am Stundenlauf, an Katalog- und Datenbankzugriff oder an Text hängen: Blatt *Übersicht* […]; die **Kennzahlspalten des Vergleichsblatts** […]; alle **Detailblätter** […]; die **Strommengen-Matrix** […], die Bezugsspitze, die **Emissionsbilanz**, der **KWK-Modulblock**; die **Sensitivitätstafel** (fünf Zeilen, jede eine Differenz zweier Zahlungsbilder); die Textbausteine […] und die Gesetzeslogik mit Katalogzugriff […]."

Zeile 880–885 (Grenze des Formelberichts, drei Sätze):

> „**Drei Sätze, die der Formelbericht trägt:** Er rechnet die Bewertung bereits feststehender Jahresmengen nach — jede Änderung an Anlagengröße, Bedarf oder Fahrweise verlangt einen neuen Stundenlauf, den keine Zellformel liefert. Die Gesetzeslogik und die Nachweise, die den Zahlen ihre Gültigkeit geben, sind nicht abbildbar. Und was der Anwender in der Mappe umstellt, kommt nie ins Projekt zurück: Die Formelmappe ist ein nachvollziehbarer Nachweis, keine zweite Eingabeoberfläche."

Zeile 887–890 (offene ClosedXML-Fragen vor Stufe 0):

> „Vor Stufe 0 zu klären: ob die eingesetzte ClosedXML-Fassung Formeln mit zwischengespeichertem Ergebnis ablegt oder Excel beim Öffnen rechnen muss, und ob eine Formelmappe in anderen Tabellenkalkulationen dieselben Werte zeigt. Im Bestand deckt kein Test den Excel- und den Word-Generator ab — die Stufen brauchen zuerst eine Wache über beide Blattstrukturen."

### Konzept Gap-Tabelle V-G10 (Zeile 777)

`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`, Zeile 777 (§ 2.11.2, Gap-Tabelle V-G):

> | V-G10 | **Bericht** mit Pflichtinhalten a)–d) + **editierbarer XLSX mit Formeln** nach Anhang-A-Raster (9) | Excel-Export existiert (ClosedXML), aber als **Werte** — der Generator schreibt keine einzige Formel, und keine Zahl des Parametersatzes erreicht eine Zelle (gemessen 18.09.2026) | **größte Einzellücke mit hartem Muss**. **Entschieden 18.09.2026, abweichend von der Empfehlung: der ganze Bericht formelbasiert**, soweit ableitbar — Stufenplan und die Liste dessen, was dauerhaft Wert bleibt, in § 2.11.6; das ValERI-Blatt (Parameterblock mit absoluten Bezügen, Periodenspalten, Gesamt-/Barwert-/NPV-Zeile je Szenario) ist darin Stufe 0 und 1 |

### Analysepapier: Befund und Testlücke

Bereits oben unter Punkt 1 zitiert: § 3.3 Zeile 296 (A4, Messung „0 Formeln, 167 `.Value`", auch
der Word-Generator ungedeckt) und § 3.5 Zeile 320 (N4, keine Testwache über
`ExcelBerichtGenerator.Erzeuge`/`WordBerichtGenerator.Erzeuge`).

Entscheidungsregister, Zeile 147 (V-G10, wörtlich):

> | **V-G10** | **Bericht** mit Pflichtinhalten a)–d) + **editierbarer XLSX mit Formeln** nach Anhang-A-Raster (9) | **größte Einzellücke mit hartem Muss**. **Entschieden 18.09.2026, abweichend von der Empfehlung: der ganze Bericht formelbasiert**, soweit ableitbar — Stufenplan und die Liste dessen, was dauerhaft Wert bleibt, in § 2.11.6; das ValERI-Blatt (Parameterblock mit absoluten Bezügen, Periodenspalten, Gesamt-/Barwert-/NPV-Zeile je Szenario) ist darin Stufe 0 und 1 | 18.09.2026, Anwender, abweichend von der Empfehlung (Statusdatei #332) | § 2.11.6 | offen — V‑D (E8), Stufenplan 0 bis 3 |

### Mockup: Zone „Bericht und Ausgabe", Zeile Excel-Formelmappe (Zeile 4512–4527)

> „Excel-Formelmappe | Parameterblock aus echten Zellen; Mehrjahrestabelle mit Energie als Fortschreibung, Netto als Zeilensumme, Barwert als Netto × Abzinsungsfaktor, Kumuliert als Laufsumme; Nettobarwert und Annuität über NBW/RMZ, interner Zinsfuß über eine Differenzreihe; bemessene Betriebskostenzeilen als Menge × Satz, Delta-Block als Zellbezug | Umfang in vier Stufen 0 bis 3 — jede setzt die vorige voraus"

und Zeile 4528–4531 („Was dauerhaft Werte bleibt", unmittelbar folgende Zeile derselben Tafel):

> „Was dauerhaft **Werte** bleibt | Übersicht, Detailblätter mit Monatswerten, Strommengen-Matrix, Emissionsbilanz, KWK-Modulblock, Sensitivitätstafel, Kosten-Kennzahlen mit dem Leistungspreis aus der Viertelstunden-Bezugsspitze, Text- und Gesetzesnachweise | eine Formelmappe rechnet die Bewertung feststehender Jahresmengen nach; sie ersetzt keinen Stundenlauf und schreibt nichts ins Projekt zurück"

### Code: heutiger Stand (kein Formelbericht, nur Wertbericht)

`EPOS.Kern/Allgemein/Bericht/ExcelBerichtGenerator.cs`, Zeile 5: `using ClosedXML.Excel;` — die
Bücherei ist eingebunden. Zeile 124–126, 160: `XLWorkbook`/`IXLWorksheet` werden für eine
Probe-Mappe und den eigentlichen Bericht angelegt. Zeile 356: `BlattWirtschaftlichkeit(XLWorkbook
wb, BerichtsDaten daten)` schreibt das Blatt „Wirtschaftlichkeit" — **als Werte**, keine Formel.
Weitere Blätter derselben Datei: Zeile 194 `BlattUebersicht`, Zeile 270 `BlattVergleich`.

`EPOS.Kern/Allgemein/Bericht/VerlaufExcel.cs` (302 Zeilen) — schreibt das Blatt „Verlauf" mit
ClosedXML, ebenfalls als Werte (siehe Statuszeile #436: „`VerlaufExcel` schreibt das Blatt
„Verlauf" (je Jahr eine Zeile, je Stand und Szenario eine Spalte …)").

Repoweite Suche (Hauptbaum, ohne `.claude/worktrees`) nach der ClosedXML-Formel-API ergibt **0
Treffer**:

```
grep -rn "FormulaA1\|\.Formula =\|\.Formula=" --include=*.cs EPOS.Kern EPOS.UI   →  keine Treffer
```

Das bestätigt unabhängig vom Analysepapier („0 Formeln repoweit"), dass im Hauptbaum tatsächlich
keine einzige Excel-Zellformel geschrieben wird.

`EPOS.Kern/Allgemein/Bericht/EmissionsAusweis.cs`, Zeile 184 und 197: `SummenFormel(...)` — das ist
**keine** Excel-Formel, sondern eine **Textzeile**, die eine Rechenvorschrift in Prosa
zusammensetzt (Kommentar Zeile 195: „Formel sagte nichts, was die Zahl nicht schon sagt"); der
einzige Treffer für „Formel" im Bericht-Ordner außerhalb dieser Textbausteinklasse.

`EPOS.Kern/Allgemein/Bericht/Bausteine/BausteineWirtschaftlichkeit.cs` (1221 Zeilen) —
`WirtschaftlichkeitBaustein : IBerichtsBaustein` (Zeile 20) schreibt den Word-Baustein
(`SchreibeWord`, Zeile 25); Kommentar Zeile 304–305: „Excel-Bericht führt den Verlauf als ZAHLEN
statt als Bild (Blatt „Wirtschaftlichkeit" und Blatt „Verlauf")." Kein `*Formel*`-benannter
Baustein vorhanden.

`EPOS.Kern/Allgemein/Wirtschaftlichkeit/` — Suche nach `*Herleitung*`/`*Nachweis*`/`*Formel*`
ergibt genau zwei Treffer im Kern-Wirtschaftlichkeitsordner: `ErgebnisNachweisUmschlag.cs`
(Nachweis-Umschlag, Fassung 8, s. u.) und — außerhalb dieses Ordners, in
`EPOS.Kern/Controller/KostenHerleitung.cs` — eine Herleitungsklasse der Kostenseite, nicht der
Wirtschaftlichkeit. **Keine `*Formel*`-benannte Klasse existiert**, konsistent mit dem Befund „0
Formeln repoweit".

---

## 4. U43

### Mockup-Anhang, Zeile 4741–4747 (Tabellenzeile U43)

`Dialog_Formel_Zahlenprobe.html`, Zeile 4741–4747, wörtlich:

> „offen · entschieden 22.09.2026, mit E8 | U43 | 8 | Knopf „Anhang-E-Checkliste…" im Fuß des Bewertungsblocks — die Checkliste ist eigene Arbeit und im Code nicht vorhanden | keine Fundstelle | **Entscheid ausstehend:** ob dieser Knopf auf die Ergebnisseite gehört, ist offen — Anhangzeile angelegt, Bau erst nach dem Entscheid. **Frage Q18 entschieden 20.09.2026 nach Empfehlung**, und die Empfehlung lautete genau so: erst die Anhangzeile anlegen, dann entscheiden. Folgeentscheid 22.09.2026: Bau mit E8, sobald V‑C und V‑D den Inhalt der Checkliste liefern"

Hinweis: Die „Entscheid ausstehend"-Formulierung in derselben Zelle ist der **ältere** Satz (vor
dem Folgeentscheid vom 22.09.2026) und durch den letzten Satz derselben Zelle überholt — der
Stand-Chip davor („entschieden 22.09.2026, mit E8") ist die aktuelle Fassung.

### Entscheidungsregister, Zeile 121 (Q18, wörtlich)

`Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md`, Zeile 121:

> | **Q18** | „Bericht erzeugen" und „Anhang‑E‑Checkliste…" auf der Wirtschaftlichkeitsseite? | Zweiter Einstieg in den Bericht vertretbar; die Checkliste ist eigene Arbeit — **Anhangzeile anlegen, dann entscheiden**. **Anwenderentscheid 22.09.2026 (nach Empfehlung): U44 „Bericht erzeugen" wird mit E5 gebaut (ruft den bestehenden Berichtsweg), U43 „Anhang‑E‑Checkliste…" mit E8, wenn V‑C und V‑D den Inhalt liefern** (Nummern nach dem Anhang des Mockups: U43 Checkliste, U44 Bericht; berichtigt 22.09.2026) | 20.09.2026, nach Empfehlung; Rest 22.09.2026, nach Empfehlung | Mockup-Anhang U43, U44 | U44 gebaut #434; U43 mit E8 |

### Konzept Gap-Tabelle V-G12 (Zeile 780, unmittelbar unter V-G11 derselben Tafel)

> | V-G12 | **Anhang-E-Checkliste** (15 Punkte, Note 1–5) | fehlt | als Abschlussseite des Berichts; zugleich interne Abnahmecheckliste der Etappe |

### Umsetzungsstand-Einleitung, Zeile 4592–4595 (Mockup, Anhang-Kopf)

> „… und der Folgeentscheid gefallen: U44 „Bericht erzeugen" ist mit E5 gebaut, U43 „Anhang-E-Checkliste…" kommt mit E8 und trägt deshalb den Chip „offen"."

### Abhängigkeit innerhalb E8

U43 hängt laut Register-Wortlaut **inhaltlich von V‑C und V‑D ab** („sobald V‑C und V‑D den Inhalt
der Checkliste liefern") — die Checkliste ist also nicht unabhängig von den fünf Blöcken und der
Formelmappe zu bauen, sondern setzt deren Inhalte voraus.

---

## 5. Anhang D

### Konzept, Zeile 777–780 (Definition „Anhang D der Norm")

`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`, Zeile 777–780, wörtlich:

> „**Anhang D der Norm ist eine BHKW-Fallstudie** (90 kW_th, 18 Jahre, NPV 64.480 €, Worst −202.802 €, Best +598.320 €) — sie dient der Etappe als **externe Gegenprobe**: EPOS muss mit denselben Eingaben dieselben Zahlen treffen. *(Vorsicht: Zwei Zeilen der Sensitivitätstabelle D.6 tragen im Normtext versehentlich Werte des Pumpenbeispiels — als Prüfreferenz ungeeignet, dokumentiert.)*"

### Analysepapier § 3.3, Zeile 299 (Befund A7, bereits oben zitiert)

> „Anhang-E-Checkliste und Anhang-D-Gegenprobe: **nichts gebaut**; Normtext und `VALERI_Vorlage_V7.xlsx` liegen unter `Quellen/VALERI/`; die Fallstudie gehört als Prüfvorrichtung gegen `KapitalwertRechner.Rechne`, nicht gegen die volle Kette; zwei Zeilen der Sensitivitätstafel D.6 tragen im Normtext Werte des Pumpenbeispiels und dürfen nicht in die Vorrichtung"

### Analysepapier § 5, Zeile 457 (E8-Zeile, bereits oben zitiert)

Nennt „Anhang-D-Gegenprobe gegen `KapitalwertRechner.Rechne`" ausdrücklich als Teil von E8.

### Konzept § 2.11.4, Zeile 807 (V-D, bereits oben zitiert)

Nennt „**Gegenprobe an der Anhang-D-Fallstudie**" als Teil von V-D/E8.

### Abgrenzung zu einer ähnlich klingenden, aber anderen „Zahlenprobe"

Das Analysepapier enthält in § 3.8 (Zeile 350–374, Überschrift „Zahlenprobe gegen die
Altanwendung (A8, § 6.3 Nr. 20, § 7 B9)") eine **andere** Prüfung: einen Zahlenvergleich gegen die
alte WinForms-/Excel-Anwendung `BHKW-WP-PLAN.XLSM`. Zeile 351–352:

> „**Anwenderentscheid 22.09.2026: „BHKW-Plan-Mappen: nicht relevant."** Die Zahlenprobe gegen die Altanwendung entfällt damit (Etappe E11, § 7 B9, Entscheid A17) …"

Diese Zahlenprobe ist **entfallen** und gehört zu **E11** (das selbst entfällt), **nicht zu E8**.
Sie ist nicht zu verwechseln mit der Anhang-D-Gegenprobe (Prüfung gegen die ValERI-Norm-Fallstudie,
Teil von E8) — auch wenn der Mockup-Dateiname `Dialog_Formel_Zahlenprobe.html` (Titel-Tag, Zeile 6:
„Dialog, Formel, Zahlenprobe, Ergebnis") denselben Wortstamm „Zahlenprobe" trägt.

### Code: Ziel der Gegenprobe

`EPOS.Kern/Allgemein/Wirtschaftlichkeit/KapitalwertRechner.cs`, Zeile 110: `public static class
KapitalwertRechner`; Zeile 610: `public static Zahlungsbild Rechne(List<InvestPosition>
investitionen, …)` — das ist die von A7 und der E8-Zeile genannte Zielmethode der
Anhang-D-Gegenprobe („gegen die volle Kette" soll die Fallstudie ausdrücklich **nicht** laufen,
sondern nur gegen `Rechne`).

---

## 6. U41

### Mockup-Anhang, Zeile 4727–4733 (Tabellenzeile U41)

`Dialog_Formel_Zahlenprobe.html`, Zeile 4727–4733, wörtlich:

> „offen · mit E8 | U41 | 8 | Brückenbild „Von der Investition zur Kapitalwertdifferenz" (Wasserfalldarstellung) auf der Ergebnisseite — im Code nirgends vorhanden | keine Fundstelle — vorgesehen: `ChartRenderer.cs`, `WirtschaftlichkeitSeite.razor` | **Entschieden 22.09.2026 nach Empfehlung (E5b‑4): Bau mit E8**, dann Bildprobe wie bei den anderen Diagrammen"

### Entscheidungsregister, Zeile 319 (E5b‑4, wörtlich)

> | **E5b‑4** | Etappe der nicht gebauten Mockup-Teile: Spannen-Balkenbild mit E6; Nominalsummen, Brückenbild, Tafel „Was daraus im Lauf wird" und Fußzeile „Drei Szenarien gerechnet…" mit E8? (Empfehlung: so.) | so — das Spannen-Balkenbild mit E6; Nominalsummen, Brückenbild, Tafel „Was daraus im Lauf wird" und Fußzeile mit E8 | 22.09.2026, nach Empfehlung | Mockup, Kategorie 8; Analysepapier § 5 (E6, E8) | Spannen-Balkenbild gebaut #436 (`ChartRenderer.KapitalwertSpanne`, auf der Seite unter der Bandbreitentafel und im Wortbericht); Nominalsummen und Differenzspalte, Brückenbild, Tafel und Fußzeile offen — E8 (Mockup-Anhang U41, U46–U48) |

Das Brückenbild ist damit das **einzige** der vier E5b‑4-Reststücke, das noch **keine**
Codestelle hat (weder `ChartRenderer.cs` noch `WirtschaftlichkeitSeite.razor` enthalten heute eine
Brücken-/Wasserfalldarstellung — geprüft per Suche nach „Brücke" im Bericht-Ordner: keine
Zeichenmethode gefunden, nur die oben zitierten Kommentare, die das Fehlen selbst benennen, z. B.
Analysepapier Zeile 293: „Rest: Brückenbild, Block 2 …").

---

## 7. U46

### Mockup-Anhang, Zeile 4755–4763 (Tabellenzeile U46)

`Dialog_Formel_Zahlenprobe.html`, Zeile 4755–4763, wörtlich:

> „offen · mit E8 | U46 | 8 | Gliederung „Woraus entsteht die Zahl?" mit der **Nominalsumme** je Bestandteil neben dem Barwert und der Spalte „Differenz ‹Version› − Referenz", die in der Kapitalwertdifferenz aufgeht. Heute führt die Gliederung Betriebs- und Energiekosten als Werte des ersten Jahres [€/a], Ersatz und Restwert als Barwert, keine Nominalsumme und keine Differenzspalte | `WirtschaftlichkeitSeite.razor` (Gliederung) · `WirtschaftlichkeitZeilen` | **Entschieden 22.09.2026 nach Empfehlung (E5b‑4): Bau mit E8.** bunit: je Bestandteil Barwert und Nominalsumme; die Differenzspalte ergibt in der Summe die Kapitalwertdifferenz"

Betroffener Abschnitt im Code: „3 · Woraus entsteht die Zahl?" der Darstellung „Kennzahlen"
(`WirtschaftlichkeitSeite.razor`, Kommentarzeile 299–302: „------------------------------------- 3
· Woraus entsteht die Zahl? *@ … Die Gliederungstafeln der Zeilendefinition (Investition, Betrieb,
…)"); die dort gezeichnete Gliederung stammt aus der Klasse `WirtschaftlichkeitZeilen`
(`EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitZeilen.cs`, nicht einzeln geöffnet, aber
oben als Sichtbarkeitsregel-Quelle in Analysepapier A6/N-Fund `WirtschaftlichkeitZeilen.Sichtbare`
bestätigt).

---

## 8. U47

### Mockup-Anhang, Zeile 4764–4771 (Tabellenzeile U47)

`Dialog_Formel_Zahlenprobe.html`, Zeile 4764–4771, wörtlich:

> „offen · mit E8 | U47 | 8 | Tafel „Was daraus im Lauf wird" unter dem Hinweistext in „Was ist angenommen?": je Szenario die Wirkung auf eine Variante — Investition I₀, die Jahre der fälligen Ersatzbeschaffungen, der Restwert am Ende | `WirtschaftlichkeitSeite.razor` („Was ist angenommen?") · `WirtschaftlichkeitBewertung` | **Entschieden 22.09.2026 nach Empfehlung (E5b‑4): Bau mit E8.** Prüffall: die drei Spalten gleichen den Szenarioläufen der Bandbreite"

Betroffener Abschnitt im Code: „4 · Was ist angenommen?" der Darstellung „Kennzahlen"
(`WirtschaftlichkeitSeite.razor`, Kommentarzeile 314–332: „4 · Was ist angenommen? … U10: der
Hinweistext UNMITTELBAR unter der Annahmentafel …"); die Tafel „Was daraus im Lauf wird" käme
**unter** diesem Hinweistext hinzu. Zielklasse laut Mockup:
`EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitBewertung.cs` (im Verzeichnis vorhanden,
nicht einzeln geöffnet).

---

## 9. U48

### Mockup-Anhang, Zeile 4772–4777 (Tabellenzeile U48)

`Dialog_Formel_Zahlenprobe.html`, Zeile 4772–4777, wörtlich:

> „offen · mit E8 | U48 | 8 | Fußzeile „Drei Szenarien gerechnet · Annahmen aus Vorgaben, nichts gepflegt" im Fuß des Bewertungsblocks — sie nennt, wie viele Szenarien gerechnet sind und woher ihre Annahmen kommen | `WirtschaftlichkeitSeite.razor` (Fuß von „Was ist angenommen?") | **Entschieden 22.09.2026 nach Empfehlung (E5b‑4): Bau mit E8.** bunit: „Vorgaben" ohne Pflege, „gepflegt" nach einer Pflege im Parameterdialog"

Diese Fußzeile ist die letzte der vier E5b‑4-Reststücke (neben U41, U46, U47) und steht laut
Mockup-Fundstelle am Fuß desselben Abschnitts „Was ist angenommen?" wie U47.

---

## 10. E6‑Q1

### Entscheidungsregister, Zeile 47 und Zeile 334 (R‑E6, wörtlich)

Zeile 47 (Übersichtstafel der Familien):

> | R‑E6 | E6‑Q1, E6‑Q2 — die Fragen aus E6 (entschieden 23.09.2026) | Protokoll E6; Statusdatei Nach #436 (a) | 2 |

Zeile 328 und 334 (Familie R‑E6, wörtlich):

> „23.09.2026 entschieden, E6‑Q1 mit „ja", E6‑Q2 mit „so lassen". Die zwei übrigen Fragen der Etappe …"
>
> | **E6‑Q1** | Block 4 „Unsicherheit" der Darstellung „ValERI-Bewertung" zeigt Bandbreite, Vorschlag, Hinweistext und Sensitivität, aber weder den Verlauf noch das Spannenbild — die Tafel der fünf Blöcke im Mockup nennt dort den Verlauf. Verlauf und Spannenbild auch in Block 4? | **ja** — dieselben Bausteine wie unter „Wie sicher ist das?" | 23.09.2026 gestellt; **entschieden 23.09.2026 (Anwender: ja)** | § 2.13 (5); § 2.11.4 (V‑C); Mockup, Kategorie 8 und Anhang U49 | Umsetzung mit E8 (Block 4 vollständig) |

### Protokoll E6, Zeile 148 und 210 (wörtlich)

`Dokumentation/ueberholt/Protokolle/Reporting/E6_Verlauf_Szenarien_Protokoll.md`, Zeile 148
(Fragentafel):

> | **E6‑Q1** Block 4 „Unsicherheit" der Darstellung „ValERI-Bewertung" zeigt Bandbreite, Vorschlag, Hinweistext und Sensitivität, aber weder den Verlauf noch das Spannenbild — die Tafel der fünf Blöcke im Mockup nennt dort den Verlauf. Beide auch in Block 4? | offen — Empfehlung: ja, dieselben Bausteine |

Zeile 210–211 (Abschnitt „Offen"):

> „**Zwei Anwenderfragen:** E6‑Q1 (Verlauf und Spannenbild auch in Block 4 der ValERI-Ansicht; Empfehlung: ja, dieselben Bausteine) und E6‑Q2 (Spannenbild mit Vorzeichenfarben und Achse in vollen Euro; Empfehlung: so lassen) — Register R‑E6."

Zeile 225 und 229 (E8-Stücke, nur redaktionell verwandt, nicht E6‑Q1 selbst):

> „**E8-Stücke aus E5b‑4:** Nominalsummen und Differenzspalte der Gliederung, Brückenbild, Tafel „Was daraus im Lauf wird", Fußzeile „Drei Szenarien gerechnet…" — Mockup-Anhang U41 und U46 bis U48."
>
> „… Mockup-Nachzug (U3, U13, Rest von U2, U46 bis U48 für die E8-Stücke, **U49 für E6‑Q1**, Schlüsseltafel) …"

### Status_iOS_Migration.md, Zeile 559 — Block „Nach #436" (a) (wörtlich, Auszug)

> „**Nach #436 (E6 Verlauf mit drei Szenarien):** (a) **Zwei offene Anwenderfragen aus der Etappe** (im Entscheidungsregister als R‑E6): **E6‑Q1** Block 4 „Unsicherheit" der Darstellung „ValERI-Bewertung" zeigt Bandbreite, Vorschlag, Hinweistext und Sensitivität, aber weder den Verlauf noch das Spannenbild — die Tafel der fünf Blöcke im Mockup nennt dort den Verlauf. Beide auch in Block 4? (Empfehlung: ja, dieselben Bausteine.) **Anwender 23.09.2026: ja** — Umsetzung mit E8 (Block 4 vollständig). **E6‑Q2** […] **Gebaut sind E5b‑2, E5b‑3 und der E6-Teil von E5b‑4** (Register: Umsetzungsstand #436; Nominalsummen und Differenzspalte der Gliederung, Brückenbild, Tafel „Was daraus im Lauf wird" und Fußzeile bleiben bei E8) …"

Derselbe Absatz, Teil (e) (Mockup-Nachzug), nennt zusätzlich:

> „… U41 (Brückenbild) „offen · mit E8"; neue Zeilen U46 (Nominalsummen und Differenzspalte der Gliederung), U47 (Tafel „Was daraus im Lauf wird") und U48 (Fußzeile „Drei Szenarien gerechnet…"), alle „offen · mit E8", und **U49 (Verlauf und Spannenbild in Block 4, „Entscheid ausstehend", E6‑Q1)** …"

### Mockup-Anhang, Zeile 4778–4786 (Tabellenzeile U49, wörtlich)

`Dialog_Formel_Zahlenprobe.html`, Zeile 4778–4786:

> „offen · entschieden 23.09.2026, mit E8 | U49 | 8 | Verlauf mit drei Szenarien und Spannenbild auch in Block 4 „Unsicherheit" der Darstellung „ValERI-Bewertung" — die Tafel der fünf Blöcke nennt dort den Verlauf; gebaut zeigt Block 4 Bandbreite, Vorschlag, Hinweistext und Sensitivität | `WirtschaftlichkeitSeite.razor` (Block 4) · `KapitalwertVerlaufAbschnitt` | **Frage E6‑Q1 — entschieden 23.09.2026: ja, Bau mit E8** (Statusdatei „Nach #436"; Empfehlung: ja, dieselben Bausteine wie unter „Wie sicher ist das?"). bunit: Block 4 zeigt Spannenbild und Verlauf"

### Code: Vergleich „Wie sicher ist das?" (gebaut) gegen Block 4 (Lücke)

`EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor`, Zeile 254–266 (Abschnitt „2 · Wie sicher
ist das?" der Darstellung „Kennzahlen", **bereits vollständig mit E6 gebaut**):

```
254: @* ------------------------------------------- 2 · Wie sicher ist das? *@
255: <Gruppenkopf Titel="@Texte.AbsSicher">
256:     <KindInhalt>
257:         @Bandbreitenteil
258:         @Spannenteil
259:
260:         @* ETAPPE E6 (Konzept § 2.13 (5), U3): DER VERLAUF MIT ALLEN DREI
...
264:         <KapitalwertVerlaufAbschnitt Dienste="@Verlauf" Fassung="@_verlaufFassung"
265:                                      Gesperrt="@_laeuft" Gerechnet="VerlaufGerechnet" />
266:
267:         @Sensitivitaetsteil
```

Gegenüber Zeile 477–491 (Block 4, **Lücke**, bereits oben unter Punkt 2 zitiert): dort stehen
`@Bandbreitenteil`, die Empfehlungs- und Szenariohinweis-Zeilen und `@Sensitivitaetsteil` — **aber
weder `@Spannenteil` noch `<KapitalwertVerlaufAbschnitt>`**. Genau diese zwei fehlenden Bausteine
sind es, die E6‑Q1 („ja, dieselben Bausteine") für Block 4 verlangt.

`RenderFragment`-Definitionen derselben Datei: Zeile 1357 `Bandbreitenteil`, Zeile 1376
`Spannenteil` — beide stehen als wiederverwendbare Fragmente bereits zur Verfügung und könnten
unverändert auch in Block 4 eingesetzt werden; `<KapitalwertVerlaufAbschnitt>` ist eine eigene
Razor-Komponente (`EPOS.UI/Seiten/Berichte/KapitalwertVerlaufAbschnitt.razor`, 262 Zeilen).

`EPOS.Kern/Allgemein/Bericht/ChartRenderer.cs`, Zeile 1671: `public static byte[]
KapitalwertSpanne(IReadOnlyList<Spannenbalken> balken, string referenz, …)` — der mit E6 (#436)
gebaute Spannenbild-Renderer, Zeile 1707 `KapitalwertSpanneModell(...)`.

`EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitVerlaufSzenarien.cs`, Zeile 26: `public
sealed class WirtschaftlichkeitVerlaufSzenarien` — hält die drei Szenarioläufe je Version
(`Laeufe`, Zeile 46; `Lauf(string szenario)`, Zeile 50; `Nulldurchgang`, Zeile 103); das ist die
Datengrundlage, die `KapitalwertVerlaufAbschnitt` zeichnet und die mit E6‑Q1 zusätzlich in Block 4
gebraucht wird.

---

## 11. Codestellen

### EPOS.UI/Seiten/Berichte/ — die Ergebnisseite

- `WirtschaftlichkeitSeite.razor:1` — Kopfkommentar, beschreibt die ganze Seite (Umschalter,
  Darstellungen „Kennzahlen"/„ValERI-Bewertung").
- `WirtschaftlichkeitSeite.razor:21–22` — Kommentar nennt Block 2/E8 ausdrücklich als offene
  Hinweiszeile.
- `WirtschaftlichkeitSeite.razor:226–332` — die vier Abschnitte der Darstellung „Kennzahlen" (1
  Lohnt es sich? … 4 Was ist angenommen?, alle mit E5/E6 gebaut).
- `WirtschaftlichkeitSeite.razor:254–293` — Abschnitt „Wie sicher ist das?" mit Bandbreite,
  Spannenbild, Verlauf (`KapitalwertVerlaufAbschnitt`), Sensitivität, Szenariozeile — Vorbild für
  Block 4 nach E6‑Q1.
- `WirtschaftlichkeitSeite.razor:444–520` — die Darstellung „ValERI-Bewertung" mit den fünf
  `Gruppenkopf`-Blöcken (Block1 Zeile 452, Block2 Zeile 462, Block3 Zeile 470, Block4 Zeile 477,
  Block5 Zeile 496); Block 2 nur Hinweiszeile, Block 4 ohne Spannenteil/Verlauf.
- `WirtschaftlichkeitSeite.razor:1357` — `Bandbreitenteil`-Fragment.
- `WirtschaftlichkeitSeite.razor:1376` — `Spannenteil`-Fragment.
- `WirtschaftlichkeitSeiteTexte.cs:96–119` — Textschlüssel `Block1`…`Block5`, `Norm1`…`Norm5`,
  `Block2Hinweis` (`WIRT_VALERI_BLOCK_*`, `WIRT_VALERI_NORM_*`, `WIRT_VALERI_BLOCK_2_HINWEIS`).
- `WirtschaftlichkeitSeiteKiSicht.cs` — vorhanden, nicht einzeln geöffnet (KI-Sicht/Hilfe-Maske der
  Seite).
- `WirtschaftlichkeitDaten.cs` (Ordner Berichte) — Datenhülle der Seite, nicht einzeln geöffnet.
- `KapitalwertVerlaufAbschnitt.razor:105–144` — Parameter `Dienste`, `Texte`, `Fassung`,
  `Gesperrt`, `Gerechnet`; das ist die Verlauf-Komponente, die E6‑Q1 zusätzlich in Block 4
  einsetzen soll (262 Zeilen gesamt).
- `VerlaufDaten.cs`, `VerlaufTexte.cs` (Ordner Berichte) — Daten- und Textklassen des
  Verlauf-Abschnitts, nicht einzeln geöffnet.

### EPOS.UI/Bausteine/ — wiederverwendete Formel-/Herleitungs- und Blockkomponenten

- `Herleitungszeile.razor` (26 Zeilen) — die Komponente für jede „Formel-/Herleitungszeile" der
  Seite (`<Herleitungszeile Text="…" />`), in allen fünf ValERI-Blöcken sowie in „Wie sicher ist
  das?" verwendet.
- `Gruppenkopf.razor` (39 Zeilen) — die Blockkopf-Komponente (`Titel`, `Summe`, `KindInhalt`), aus
  der jeder der fünf ValERI-Blöcke und jeder der vier Kennzahlen-Abschnitte besteht.

### EPOS.UI/Dialoge/Wirtschaftlichkeit/ — Befund

Der Ordner enthält ausschließlich **Eingabedialoge** (BHKW-, Gesetzeskatalog-, Photovoltaik-,
Tarifstruktur- und Parameterdialog, je mit `*Daten.cs`/`*KiSicht.cs`/`*Texte.cs` bzw. `.razor`,
17 Dateien insgesamt) — **keine** dieser Dateien trägt einen erkennbaren Bezug zu E8 (fünf Blöcke,
Formelmappe, Anhang-D/-E); sie pflegen Projektparameter, nicht die Ergebnis-/Berichtsdarstellung.
Für E8 einschlägiger Code liegt nach dem heutigen Repo-Stand ausschließlich in
`EPOS.UI/Seiten/Berichte/`, `EPOS.UI/Bausteine/`, `EPOS.Kern/Allgemein/Wirtschaftlichkeit/` und
`EPOS.Kern/Allgemein/Bericht/`.

### EPOS.Kern/Allgemein/Wirtschaftlichkeit/ — Herleitung, Nachweis, Formel

- `ErgebnisNachweisUmschlag.cs:43` — `public sealed class ErgebnisNachweisUmschlag`; Zeile 53:
  `FASSUNG = 8` (aktuelle Nachweisfassung); Zeile 72–171: trägt u. a. `KwkgModule`,
  `EnergiekostenJeAnlage`, `Betriebskosten`, `KohaerenzHinweise`, `IrrVorzeichenwechsel` — der
  Nachweis-Umschlag, den Formelmappe und Anhang-D-Gegenprobe gegenlesen müssten.
- `KapitalwertRechner.cs:110` — `public static class KapitalwertRechner`; Zeile 610: `public
  static Zahlungsbild Rechne(...)` — Zielmethode der Anhang-D-Gegenprobe.
- `WirtschaftlichkeitVerlaufSzenarien.cs:26` — `public sealed class
  WirtschaftlichkeitVerlaufSzenarien`; hält die Szenarioläufe für Verlauf (E6, mit E6‑Q1 auch für
  Block 4).
- Kein `*Formel*`-benannter Rechner im Ordner (30 Dateien geprüft) — konsistent mit „0 Formeln
  repoweit".
- Übrige Dateien des Ordners (`WirtschaftlichkeitCtrl.cs`, `WirtschaftlichkeitBewertung.cs`,
  `WirtschaftlichkeitZeilen.cs`, `WirtschaftlichkeitDaten.cs`, `WirtschaftlichkeitEmpfehlung.cs` u. a.)
  — vorhanden, nicht einzeln geöffnet; laut Mockup-Fundstellen sind `WirtschaftlichkeitZeilen`
  (U46, Gliederung) und `WirtschaftlichkeitBewertung` (U47, Tafel „Was daraus im Lauf wird") die
  für zwei der E8-Reststücke genannten Zielklassen.

### EPOS.Kern/Allgemein/Bericht/ — Word-/Excel-Bausteine

- `ExcelBerichtGenerator.cs:5` — `using ClosedXML.Excel;`; Zeile 194 `BlattUebersicht`, Zeile 270
  `BlattVergleich`, Zeile 356 `BlattWirtschaftlichkeit(XLWorkbook wb, BerichtsDaten daten)` —
  schreibt das Blatt „Wirtschaftlichkeit" ausschließlich als Werte (1714 Zeilen gesamt).
- `VerlaufExcel.cs` (302 Zeilen) — schreibt das Blatt „Verlauf", ebenfalls als Werte.
- `Bausteine/BausteineWirtschaftlichkeit.cs:20` — `public class WirtschaftlichkeitBaustein :
  IBerichtsBaustein`; Zeile 25 `SchreibeWord(...)` — der Word-Baustein der Wirtschaftlichkeit (1221
  Zeilen gesamt); Zeile 304–305 Kommentar zum Werte-statt-Formel-Ansatz von Excel.
- `WordBerichtGenerator.cs` (492 Zeilen) — vorhanden, laut Befund N4 (Analysepapier § 3.5)
  ungetestet, „auch der Word-Generator ist ungedeckt" (A4).
- `ChartRenderer.cs:1669–1707` — `KapitalwertSpanne`/`KapitalwertSpanneModell` (Spannenbild, mit
  E6 gebaut, für E6‑Q1 in Block 4 wiederzuverwenden); kein Brückenbild („Von der Investition zur
  Kapitalwertdifferenz") und kein Zahlungsstrombild (U42) im Ordner `Zeichnung/` oder in
  `ChartRenderer.cs` gefunden (repoweite Suche nach „Brücke"/„Wasserfall"/„Zahlungsstrom" im
  Bericht-Ordner ohne Treffer auf eine Zeichenmethode).
- `EmissionsAusweis.cs:184,197` — `SummenFormel(...)`, eine **Textzeile**, kein
  Excel-Formel-Mechanismus (einziger „Formel"-Treffer außerhalb der oben genannten Klassen).
- Repoweite Suche `grep -rn "FormulaA1\|\.Formula =\|\.Formula=" --include=*.cs EPOS.Kern
  EPOS.UI` (Hauptbaum ohne `.claude/worktrees`) — **0 Treffer**.

---

## Unklarheiten

1. **E6‑Q1 fehlt in der § 5-Tabellenzeile von E8.** Die wörtliche Tabellenzeile „E8 V‑C und V‑D"
   im Analysepapier (Zeile 457) zählt Block 2, Formelmappe 0–3, U43, Anhang-D-Gegenprobe, U41 und
   U46–U48 auf, nennt E6‑Q1/U49 (Verlauf und Spannenbild in Block 4) aber **nicht**. Erst die
   Prosa direkt darunter („Stand der Etappen am 23.09.2026", Zeile 506–509) sowie der letzte Satz
   von § 5 (Zeile 518–520, „E8 ist die einzige Etappe mit belastbarer Beschreibung … (§ 2.11.6)")
   und alle anderen geprüften Quellen (Konzept § 7/Anhang Zeile 2711, Entscheidungsregister Zeile
   334, Status Zeile 559, Mockup U49 Zeile 4778) zählen E6‑Q1 ausdrücklich zu E8. In der Sache
   scheint die Zuordnung eindeutig „ja" — aber die maßgebliche Etappentafel selbst (§ 5) ist an
   dieser Stelle nicht nachgezogen; ob das nur redaktionell offen ist oder ob E6‑Q1 wegen dieser
   Lücke förmlich noch nicht als E8-Bestandteil der Tafel gilt, habe ich nicht klären können.

2. **Zwei verschiedene „fünf Blöcke" unter demselben Namen.** Konzept § 2.11.3 („Die fünf
   Darstellungsblöcke", Zeile 782–792) nennt die Blöcke Investitionskosten / Betriebskosten /
   Erlöse / Energiekosten / Wirtschaftlichkeit über Nutzungsdauer. Das Mockup (Zeile 4469–4494),
   der Code (`WirtschaftlichkeitSeiteTexte.cs:96–104`, `WirtschaftlichkeitSeite.razor:452–515`)
   und § 2.11.4/Entscheidungsregister/Status/Protokoll verwenden dagegen ausnahmslos die
   Nummerierung „1 Gegenstand und Rahmen … 5 Deklarationen". Beide Fünfergruppen behandeln zwar
   dieselbe ValERI-Bewertung, sind aber inhaltlich nicht deckungsgleich benannt, und das Konzept
   selbst stellt keine Brücke zwischen § 2.11.3 und § 2.11.4 her. Für E8 (Block 2, Block 4) ist
   erkennbar nur die zweite (nummerierte) Fassung gemeint — welchen Zweck § 2.11.3 daneben noch
   erfüllt bzw. ob es ein nicht bereinigter älterer Entwurfsstand ist, geht aus den gelesenen
   Papieren nicht hervor.

3. **U42 (Zahlungsstrombild) ist nicht wie U41/U46–U49 mit „mit E8" markiert.** Analysepapier und
   Konzept (Zeile 507–509, Zeile 2635, Zeile 2711) zählen „Zahlungsstrombild U42" verbal zu E8
   dazu. Die eigene Mockup-Zeile U42 (Zeile 4734–4739) trägt aber nur den Stand-Chip **„offen"**
   (ohne den Zusatz „· mit E8", den U41/U43/U46/U47/U48/U49 alle tragen) und ihre Abnahme-Spalte
   sagt ausdrücklich „**Konzeptentscheid nötig**, dann Bildprobe wie bei den anderen Diagrammen" —
   im Unterschied zu U41/U46–U48, die alle „Entschieden 22.09.2026 … Bau mit E8" vermerken. Ob U42
   damit tatsächlich schon entschieden mit E8 gebaut werden soll oder ob dafür (wie die
   Mockup-eigene, als „die einzige Stelle für Nichtgebautes" bezeichnete Anhangtafel nahelegt)
   noch ein gesonderter Konzeptentscheid aussteht, ist zwischen den Quellen nicht deckungsgleich.

4. **U43-Zelle enthält einen überholten Satz neben dem aktuellen.** Die Mockup-Zeile U43 (Zeile
   4741–4747) führt in dessen „Abnahme"-Spalte zuerst „Entscheid ausstehend: ob dieser Knopf auf
   die Ergebnisseite gehört, ist offen" und erst danach den späteren „Folgeentscheid 22.09.2026:
   Bau mit E8". Beim wörtlichen Zitieren bleibt der ältere Satz stehen; inhaltlich gilt nach dem
   Stand-Chip und dem Register (Zeile 121) eindeutig der Folgeentscheid, nicht der ältere Satz.

5. **Kein Codefund für Brückenbild (U41) und Zahlungsstrombild (U42).** Für beide Bilder nennen
   die Papiere „keine Fundstelle — vorgesehen: `ChartRenderer.cs`, `WirtschaftlichkeitSeite.razor`"
   (Mockup Zeile 4730–4731, 4737–4738). Eine repoweite Stichwortsuche nach „Brücke", „Wasserfall"
   und „Zahlungsstrom" im Ordner `EPOS.Kern/Allgemein/Bericht/` ergab keine Zeichenmethode — das
   bestätigt die Quellen, schließt aber nicht aus, dass ein Vorentwurf unter anderem Namen
   existiert; das wurde im Rahmen dieses nur lesenden Auftrags nicht durch eine Volltextsuche über
   den ganzen Bericht-Ordner mit weiteren Synonymen abgesichert.
