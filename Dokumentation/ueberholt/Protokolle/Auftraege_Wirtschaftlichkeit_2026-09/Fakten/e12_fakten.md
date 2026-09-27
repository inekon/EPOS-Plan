# Faktenblatt E12 — Wiki-Runden

Quellenstand: Worktree `C:\Waermeplan\EPOS-Plan\.claude\worktrees\e12`, Zweig `e12`, Commit
`4a9d74495e8f9302e244dfa6ebd121de00d6b21f` (24.09.2026 10:09:01 +0200) = `origin/ios_migration_september`.
Der Zweig `e12` trägt noch **keinen eigenen Commit** — `git branch -vv` zeigt ihn deckungsgleich mit
`origin/ios_migration_september`, ohne „ahead"/„behind". Nur gelesen, nichts geändert, kein Build, kein
Test, kein Worktree angelegt, kein Zweig gewechselt. Alle Pfade repo-relativ zur Worktree-Wurzel
`C:\Waermeplan\EPOS-Plan\.claude\worktrees\e12` (= `cd .claude/worktrees/e12` von der Repowurzel aus).

Laut dem heute (Stand 24.09.2026) im Worktree stehenden Analysepapier ist **E12 tatsächlich die nächste
und letzte offene Etappe** des Plans E0–E12 — anders als bei E10 (siehe `e10_fakten.md` desselben
Ordners, dort war E10 noch nicht die unmittelbar nächste Etappe): „**Nächste Etappe: E12** (Wiki-Runden);
E11 entfällt — damit ist der Etappenplan E0–E12 bis auf E12 abgearbeitet."
(`Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md:67`, wortgleich
`:556–557`).

---

## 1. E12 laut Analysepapier (§ 5, Zeile E12, wörtlich)

Quelle: `Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md:497`
(Tabellenkopf `:482`: Etappe | Inhalt | Größe | Rechenwirkung | Nachweis | Schema | Wiki | Modell |
Voraussetzung):

> | **E12 Wiki-Runden** | Sammel-Upload 28.09.2026: die 14+1 Sätze der Mockup-Prüfung, die fünf Lücken,
> U17/U23/U36, `help_mapping` (Tarifstruktur, BHKW, PV, acht Anker), Höfingen neutralisiert (A16),
> Hilfesystem 13.2 ergänzt; danach je Etappe die Sätze aus `06/§ 3` | S je Runde | — | Tabuwort-Regex,
> Produktdaten-Wache | — | — | Sonnet | A16, A18 |

Direkt davor die einzige Nachbarzeile (E11, `:496`):

> | **E11 Zahlenprobe A8/B9** — **entfällt** (Anwenderentscheid 22.09.2026: „BHKW-Plan-Mappen: nicht
> relevant") | ~~Referenzmappe festlegen (A17) …~~ — Nachweis der Wirtschaftlichkeitsgrößen über die
> Anker aus E1 und die A/B-Nachweise von E7, E9, E10 | — | keine | — | — | — | — | entfällt |

**Modellwahl „Sonnet":** deckt sich mit der Rahmenaussage vor der Tafel (`:478–480`): „Modell nach
`CLAUDE.md`: Opus 5 für Umsetzung, Tests und Hüllen; Sonnet 5 für Suchen, Listen und Textpflege; Fable
5.1 nur für Konzeptarbeit." E12 ist damit als Such-/Listen-/Textpflegearbeit eingestuft, nicht als
Implementierung.

**Kurzfassung „Wiki, Hilfe und Altanwendung" (Punkt 8 der Einleitung, `:142–147`, wörtlich):**

> 8. **Wiki, Hilfe und Altanwendung:** 34 von 39 Bedienstücken sind beschrieben, drei erledigte Punkte
>    fehlen noch (U17, U23, U36), die Hilfe-Taste der Tarifstruktur zeigt auf die falsche Seite, das
>    Beispiel „Höfingen" ist ein reales Altprojekt und gehört vor jeder Wiki-Verwendung neutralisiert
>    (`06`); die Zahlenprobe gegen die Altanwendung (A8, § 6.3 Nr. 20) ist nicht mehr blockiert, weil die
>    Excel-Mappen auf dem Netzlaufwerk liegen und maschinell lesbar sind — es fehlt allein die in
>    Rechenweg 08 zitierte Referenzmappe (`08`).

**Protokollzeile in der Quelltabelle § 1 (`:158`):**

> | Wiki und Hilfe | Konzeptabschnitte und offene Punkte gegen `Projekte/Wiki/*.wiki`,
> `help_mapping.txt`, Konzept Hilfesystem § 13 | Sonnet 5 | `06` |

---

## 2. Die Entscheide A16 und A18

**Empfehlungstabelle, § 4 (`:421–440`), Zeilen A16/A18 (`:438`, `:440`):**

> | **A16** | „Höfingen" in Konzept, Rechenweg 08 und künftigen Wiki-Texten neutralisieren
> („Beispielprojekt B", gerundete Beträge)? | Ja, vor dem Sammel-Upload 28.09.2026 |
>
> | **A18** | Hilfe: eigene Wiki-Seite „Gesetzliche Parameter" anlegen oder `help_mapping` auf einen
> Abschnitt der Kostenseite umbiegen? | Abschnitt auf der Seite Kosten (Menüort seit #372) |

**Standtabelle (Kopf `:446`), Zeilen A16/A18 (`:463`, `:465`):**

> | **A16** | entschieden („Höfingen" neutralisieren), nicht ausgeführt | E12 |
>
> | **A18** | entschieden (Abschnitt auf der Seite Kosten), nicht ausgeführt | E12 |

**Geltender Entscheidrahmen davor (`:413–419`, Auszug):**

> **Alle zwanzig Entscheide sind gefallen: „entschieden 20.09.2026 nach Empfehlung"** (Anwenderauftrag
> vom 20.09.2026 „fahre fort mit der Umsetzung der Wirtschaftlichkeitsberechnung nach Konzept wie im
> Mockup" mit dem Zusatz „Entscheidung nach Empfehlung"; Statuszeile **#405**). Die Empfehlungsspalte
> unten **ist damit der Entscheid** — sie bleibt im Wortlaut stehen.

A16/A18 zählen nicht zu den drei ausdrücklich noch einmal bestätigten Entscheiden (A3/A4/A5) — sie
gelten über die „nach Empfehlung"-Regel als entschieden, aber **nicht ausgeführt**.

**Entscheidungsregister** (`Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md`,
596 Zeilen), Zeilen A16/A18 (`:88`, `:90`, wörtlich, mit Datums- und Quellenspalte über das Analysepapier
hinaus):

> | **A16** | „Höfingen" in Konzept, Rechenweg 08 und künftigen Wiki-Texten neutralisieren
> („Beispielprojekt B", gerundete Beträge)? | Ja, vor dem Sammel-Upload 28.09.2026 | 20.09.2026, nach
> Empfehlung | § 2.11.3, Rechenweg 08 | entschieden („Höfingen" neutralisieren), nicht ausgeführt · E12 |
>
> | **A18** | Hilfe: eigene Wiki-Seite „Gesetzliche Parameter" anlegen oder `help_mapping` auf einen
> Abschnitt der Kostenseite umbiegen? | Abschnitt auf der Seite Kosten (Menüort seit #372) | 20.09.2026,
> nach Empfehlung | Konzept Hilfesystem; `help_mapping` | entschieden (Abschnitt auf der Seite Kosten),
> nicht ausgeführt · E12 |

**§ 7 „Berichtigungen", Stand-Absatz (`:644–646`, wörtlich):**

> **Noch offen:** die drei `help_mapping`-Zeilen und der Satz im Konzept Hilfesystem § 13.2 (beide E12),
> die Neutralisierung von „Höfingen" (A16, E12) und die Benennung der Referenzmappe (A17, E11).

---

## 3. Weitere Fundstellen im Analysepapier (§ 3.7 „Wiki und Hilfe", W1–W5)

Quelle: `2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md:375–383`, wörtlich:

> ### 3.7 Wiki und Hilfe
>
> | W1 | Keine Seite „Berechnung/Wirtschaftlichkeit" unter `EPOS.Kern/Allgemein/Hilfe/Berechnung/` — nach
> Regel 13.1 folgerichtig (keine Formeln im Wiki), aber eine Entscheidung, keine Lücke | `06/§ 2.2` |
> | W2 | Über die fünf Lücken der Mockup-Prüfung hinaus fehlen drei erledigte Punkte: KWKG-Pauschale als
> Zeile (U17), Satzherkunft-Zeilen (U23), zweite PV-Anlagenwarnung (U36); G7/G8-Hinweiszeilen ohne
> erkennbaren Anker; p_I-Feld ohne eigenen Anker | `06/§ 2.1, § 2.4` |
> | W3 | Hilfe-Zuordnung: `Form_Tarifstruktur.btn_Help` zeigt auf „Kosten", der Inhalt liegt auf
> `Wirtschaftlichkeit#strombezug`; `Form_Gesetzesparameter` zeigt auf eine Seite ohne Repo-Quelle (seit
> #372 unter Administration → Kosten); dazu die bekannten Lücken (BHKW ohne Ziel, PV zu grob, acht
> bereite Anker unverdrahtet) | `06/§ 4` |
> | W4 | „Höfingen" ist mit hoher Wahrscheinlichkeit ein reales Altprojekt (Altmappe
> `Quellen/BHKWPlan/`, „reale Höfingen-Zahlen", nicht runde Beträge) — vor jeder Wiki-Verwendung
> neutralisieren; Regel 13.2 sollte Mockup-Beispiele ausdrücklich einschließen | `06/§ 5` |
> | W5 | Pflegeplan: 19 offene U-Zeilen, V‑C…V‑E und S3 haben je Seite, Anker und Logbuch-Einstufung;
> Sätze auf Vorrat entworfen | `06/§ 3` |

**Rechenweg-08-Fund (`:404–407`, wörtlich):**

> `Rechenweg/08` enthält bereits eine bestandene Gegenprobe („Höfingen", Kapitalwert 65.259 €), aber keine
> der drei Höfingen-Dateien des Bestands trägt diese Zahlen — die Referenzmappe ist zu benennen oder zu
> ersetzen (`08/§ 6.1`).

**§ 6 Schemaschritte — Speicherflotte/E10-Zeile als Formmuster für „kein Buchstabe, kein Schemaschritt"**
(zum Vergleich, nicht E12 selbst; `:622–624`): E12 hat laut Tafelzeile `:497` in der Spalte „Schema" ein
bloßes „—", ebenso in „Rechenwirkung" (—) — E12 legt also keine Datenbankspalte an und ändert keinen
Rechenweg.

**§ 7 Berichtigungen, Tafelzeilen (`:673–675`, wörtlich):**

> | Rechenweg 05 Z. 303; Rechenweg 08 Z. 39; Rechenweg 08 Höfingen-Absatz | K‑1 Schritt 97; Mockup-Knöpfe
> ohne Element kennzeichnen; Referenzmappe benennen | `02/§ 8`, `05/§ 8.1`, `08/§ 6.1` |
> | `help_mapping.txt` Z. 259, 334 | `Form_Tarifstruktur.btn_Help = Wirtschaftlichkeit#strombezug`;
> Gesetzesparameter auf einen Abschnitt der Kostenseite (A18) | `06/§ 4` |
> | Konzept Hilfesystem § 13.2 | Satz zu Mockup-Beispielen; Orts- und Projektnamen wie Produktdaten
> behandeln | `06/§ 6.2` |

Die Zeilennummern „Z. 259, 334" stammen aus der 19.09.2026-Fassung von `help_mapping.txt`; im heute
gelesenen Worktree stehen dieselben zwei Zuordnungen (siehe Abschnitt 11) an anderer Stelle der Datei
(Zeile 265 bzw. 348) — die Datei ist seither gewachsen.

**Mockup-Prüfung, Zeile P9 (`Wirtschaftlichkeit_Kosten/2026-09-19_Pruefung_Mockups_Wirtschaftlichkeit.md:411`,
wörtlich) — nennt das Statuszeilen-Datum, mit dem die `help_mapping`-Anker gesetzt wurden:**

> | **P9 Wiki** — offen, **≈ E12**; die `help_mapping`-Anker sind mit **#405** gesetzt | fünf Lücken,
> Anker, Logbuch 14+1 Sätze, Klimadaten-Zeile — im Sammel-Upload 28.09.2026 | — |

---

## 4. Protokoll 06 (Wiki/Hilfe, Stand 19.09.2026) — zentrale Fundstelle für E12

Datei: `Dokumentation/ueberholt/Protokolle/Reporting/Analyse_Konzept_Wirtschaftlichkeit_2026-09-19/06_Wiki_Hilfe.md`
(216 Zeilen). Abkürzungen im Kopf (`:5–10`): Konzept, ND, VALERI, Hilfesystem, Status, Mockup, `05` (=
`Pruefung_Mockups_2026-09-19/05_Wiki.md`), K/W/V/E/P/S/Sim.wiki, `help_mapping` =
`WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt`.

**§ 0 „Ergebnis in fünf Sätzen" (`:14`, wörtlich, Auszug zu den Lücken und zu Höfingen):**

> … zu den fünf Lücken aus 05/§4 kommen zwei weitere hinzu, die der Mockup-Anhang selbst als „erledigt"
> führt, aber im Wiki noch fehlen (U23 Satzherkunft-Zeilen, U36 zweite PV-Anlagenwarnung — beide bereits
> mit 05/§4.4–4.5 identisch benannt). … „Höfingen" ist über die Altmappe
> `Quellen/BHKWPlan/BHKW_Höfingen_Erneuerung_20kWel.XLS` und die vom Konzept selbst als „reale
> Höfingen-Zahlen" bezeichneten Kennwerte (Konzept:51, 846, 857) mit hoher Wahrscheinlichkeit ein echtes
> Altprojekt und gehört vor jeder Wiki-Veröffentlichung neutralisiert, auch wenn Regel 13.2 wörtlich nur
> Hersteller- und Produktdaten nennt.

Die hier zitierten Konzept-Zeilen (51, 846, 857) sind die Fassung vom 19.09.2026; im heute gelesenen
Konzept (2 976 Zeilen, gewachsen gegenüber der Fassung vom 19.09.) stehen die gleichen drei Belege an
den Zeilen 737, 812/857 (nicht erneut geprüft) und 1119 — siehe Abschnitt 13.

**§ 3.1 „Mockup-Anhang «Umsetzungsstand» — offene U-Zeilen" (`:102–104`, wörtlich):**

> 39 Zeilen U1–U39, davon 19 ohne Klasse `gestrichen` = offen: **U1, U2, U3, U4, U5, U6, U7, U9, U10,
> U11, U12, U13, U14, U15, U22, U25, U27, U32, U39.** Für offene Punkte gilt einheitlich: kein
> Logbuch-Eintrag vor der Umsetzung (Regel 13.4 setzt eine sichtbare Änderung voraus), Sätze unten sind
> **Entwürfe auf Vorrat**; Upload frühestens mit dem Sammelpaket, das auf die jeweilige Umsetzung folgt.

**§ 3.2 „Konzept-interner Klärbedarf" — der U17-Fund (`:130`, wörtlich):**

> § 6.3 Nr. 9c (B7‑3) und Nr. 9g (BK1‑3) beschreiben beide, die KWKG-Pauschale habe „keine Rubrikzeile"
> bzw. der Jahr‑0-Ausweis sei „nicht gebaut" … Das widerspricht dem als **erledigt** geführten U17 …
> **U17 ist real umgesetzt und muss in W.wiki `block-a` nachgezogen werden** (deckt sich mit dem in
> Abschnitt 2.1 genannten U23-Fund); die zwei §6.3-Punkte sind vermutlich nur nicht als erledigt
> nachgetragen — das ist ein Befund für die Konzeptpflege des Anwenders, keine Wiki-Aufgabe.

**§ 3.3 „VALERI-Etappen V‑C bis V‑E" (`:132–140`, Tafel wörtlich, Kopf: Etappe | Inhalt | Wiki-Ziel bei
Umsetzung | Logbuch):**

> | V‑C | ValERI-Ansicht (fünf Blöcke + Cashflow-Chart) | neuer Abschnitt in W.wiki, neue Anker je Block …
> | wesentlich, ein Satz je Block oder gebündelt |
> | V‑D | XLSX-Formelbericht + Anhang-E-Checkliste, Gegenprobe Anhang D | W.wiki `bericht` erweitern |
> wesentlich |
> | V‑E | Vollständige Szenarioabdeckung (Trägerpreise, Erlössätze, Mengenfaktor) | W.wiki `szenarien`
> erweitern, löst U10/U15 ab | wesentlich |
>
> Upload für alle drei: frühestens das Sammelpaket nach der jeweiligen Umsetzung, **nicht vor
> 28.09.2026.**

(V‑C, V‑D und V‑E sind inzwischen mit E8 Teil a/b und E9 Teil a/b gebaut — siehe `e10_fakten.md` und
Abschnitt 1 dieses Blatts; damit gehören ihre Wiki-Ziele heute zum tatsächlichen Sammel-Upload, nicht
mehr zum „bei Umsetzung"-Vorrat.)

**§ 4 „Hilfe-Zuordnung", Einleitung und Schlussvorschlag (`:150–152`, `:168`, wörtlich):**

> Die zwölf von 05/§6 geprüften Dialoge werden hier **nicht wiederholt** (Befund dort:
> BhkwWirtschaftlichkeit ohne Ziel, PhotovoltaikVerguetung zu grob, **acht weitere mit bereitem, aber
> unverdrahtetem Anker**). …
>
> **Vorschlag für den nächsten Sammelauftrag (H2/A4 aus dem Hilfesystem-Konzept):** die neun
> Anker-Ergänzungen aus 05/§6 plus die Korrektur `Form_Tarifstruktur` → `Wirtschaftlichkeit#strombezug`
> in einem Zug — reine Konfigurationsänderung an help_mapping.txt, kein Code-Umbau (Hilfesystem:606–608).

Diese Stelle ist die Quelle der Formulierung **„acht Anker"** in der E12-Zeile des Analysepapiers
(Abschnitt 1): „acht **weitere**" bezieht sich auf acht Dialoge **außer** `BhkwWirtschaftlichkeit` und
`PhotovoltaikVerguetung` (die beiden zuvor genannten Fälle „ohne Ziel"/„zu grob") — die „acht Anker"
sind also **nicht** die Anker von Tarifstruktur/BHKW/PV selbst, sondern eine vierte, davon getrennte
Gruppe. Siehe Abschnitt 11/12 für den heutigen Stand dieser Zuordnungen.

**§ 5.1 „«Höfingen» — reales Kundenprojekt?" (`:182–191`, wörtlich, gekürzt um die Aufzählungszeichen):**

> - Der Anhang „Begleitende Artifacts" des Konzepts führt ein Artifact „ValERI-Bewertung Höfingen"
>   ausdrücklich mit **„reale Höfingen-Zahlen"** (Konzept:51).
> - § 2.9 nennt die Quelle konkret: „Die Altanwendung (**Höfingen-Mappe**, `Tab_kurz_KWKG2020`) …
>   Investition 9.624 €, Betriebskosten 518 €/a, Brennstoff 11.498 €/a" (Konzept:554–557).
> - § 2.11 nennt die Datei wörtlich: „die Altmappe `Quellen\BHKWPlan\BHKW_Höfingen_Erneuerung_20kWel.XLS`"
>   (Konzept:613) — **diese Datei liegt tatsächlich im Repository** …
> - § 2.12 und Rechenweg 08 verwenden die Zahlen als Gegenprobe: „Höfingen: Näherung 65.073 €,
>   jahresscharf 65.259 €" (Konzept:857) …
>
> **Einschätzung:** … Regel 13.2 verbietet wörtlich nur Hersteller- und Produktdaten, nicht Orts- oder
> Projektnamen — trifft den Fall also nicht direkt. … **Empfehlung, dem Anwender vorzulegen — vor jeder
> Wiki-Verwendung „Höfingen" durch einen neutralen Namen ersetzen (z. B. „Beispielprojekt B", passend zum
> bereits neutralen `Beispielprojekt.md`) und die Beträge wie bei Regel 13.2 runden**, auch wenn kein
> Wächter dafür greift (der Produktdaten-Wächter prüft nur Hersteller-/Typnamen, keine Orts- oder
> Projektnamen).

Zu `Beispielprojekt.md` als „bereits neutral" vorgeschlagenem Vorbild siehe die Gegenprobe in
Abschnitt 13 — die Datei nennt „Höfingen" heute selbst noch einmal wörtlich.

**§ 6.2 „Regel für Mockup-Beispiele ergänzen" (`:203–205`, wörtlich):**

> „Mockup" kommt im gesamten Konzept Hilfesystem kein einziges Mal vor (eigener Grep, 0 Treffer) — Regel
> 13.2 nennt als Geltungsbereich ausdrücklich nur `EPOS.Kern/Allgemein/Hilfe/Berechnung/*.wiki` und
> `Projekte/Wiki/*.wiki` (Hilfesystem:791). … Empfehlung an dieser Stelle, konkret formuliert:
> **Abschnitt 13.2 um einen Satz „Dieselbe Regel gilt für Beispieldaten, die aus
> `Dokumentation/aktuell/Mockups/*.html` in eine `*.wiki`-Datei übernommen werden — vor der Übernahme
> neutralisieren, der Wächter prüft Mockups nicht" ergänzen** — …

Der heute gelesene § 13.2 (Abschnitt 6 dieses Blatts) trägt diesen vorgeschlagenen Satz **noch nicht** —
das ist mit „Hilfesystem 13.2 ergänzt" in der E12-Zeile offenbar gemeint und steht noch aus.

---

## 5. Protokoll 05 (Mockup-Prüfung, Stand 19.09.2026) — Ursprung von „fünf Lücken" und „14+1 Sätze"

Datei: `Dokumentation/ueberholt/Protokolle/Reporting/Pruefung_Mockups_2026-09-19/05_Wiki.md` (349 Zeilen).

**§ 1 „Kurzbefund" (`:12–16`, wörtlich, Auszug):**

> Fünf Lücken sind belegt und mit Satzvorschlag versehen: die Doppelpflege-Warnung und die
> U23-Satzzeilen der Erlösrubrik fehlen teilweise, die beiden Anlagenwarnungen des
> PV-Vergütungsdialogs und der Kohärenzhinweis „übernehmen ohne Stammprojekt" fehlen ganz, und die Seite
> „Klimadaten" fehlt in der Bedienungsseiten-Tabelle des Konzepts, obwohl ihr Wiki-Upload für den
> 28.09.2026 bereits vorgemerkt ist.

**§ 8 „Offene Fragen an den Anwender", Punkt 5 — die maßgebliche Definition der „fünf Lücken"
(`:340–342`, wörtlich):**

> 5. **Fünf Wiki-Lücken (Abschnitt 4):** Doppelpflege-Warnung, beide PV-Anlagenwarnungen, Kohärenzhinweis
>    „übernehmen ohne Stammprojekt", die U23-Satzzeilen der Erlösrubrik. Empfehlung: mit dem nächsten
>    Kosten-/Wirtschaftlichkeit-Auftrag nachziehen, Sätze liegen in Abschnitt 4 vor.

Gezählt ergibt das: (1) Doppelpflege-Warnung, (2) U23-Satzzeilen der Erlösrubrik, (3) beide
PV-Anlagenwarnungen (U36, als ein Punkt gezählt), (4) Kohärenzhinweis „übernehmen ohne Stammprojekt",
(5) Klimadaten-Seite in der Bedienungsseiten-Tabelle (aus § 1, oben) — macht fünf. **U23 und die
PV-Anlagenwarnungen (U36) sind damit bereits Bestandteil dieser „fünf Lücken"**, nicht nur der später in
06/§0 genannten „zwei weiteren" (Abschnitt 4 dieses Blatts) — siehe Unklarheiten.

**Herkunftszeilen der einzelnen Lücken, § 4 (Tafeln, wörtlich):**

`:86` (§ 4.1 Kostenverwaltung): „| **Doppelpflege-Warnung** | #347 (`KOH_HILFSENERGIE_DOPPELT`) | K ·
(kein Anker) | **fehlt** | neuer Satz bei `laufgroessen`/`betriebsmengen`: „Bewertet eine Position der
Hilfsenergie dieselbe Menge, die eine Anlage mit eigenem Energieträger bereits einpreist, steht über dem
Raster ein Warnband, das auf die doppelte Pflege hinweist." | mittel |"

`:121–122` (§ 4.4 PV-Vergütung): „| **Beide Anlagenwarnungen** | Mockup U36, #350 | W ·
`pv-verguetung` (kein Unterpunkt) | **fehlt** | neuer Satz: „Nähert sich die Anlage der
Ausschreibungsgrenze oder der Grenze der Stromsteuerbefreiung nach § 9 Abs. 1 Nr. 3 StromStG, warnt der
Dialog rechtzeitig vor der jeweiligen Schwelle." | mittel–hoch | … | Kohärenzhinweis „übernehmen ohne
Stammprojekt" | #360 (`KOH_PV_STAMM_FEHLT`) | W/V · (kein Anker) | **fehlt** | Ergänzung bei V
`pv-verguetung`: „Übernimmt eine Variante die Vergütung, ohne dass ihr Stammprojekt noch besteht oder
eine Verknüpfung führt, weist ein Kohärenzhinweis darauf hin." | gering |"

`:131` (§ 4.5 Ergebnisseite): „| **Erlösrubrik Block A/B mit Satzzeilen** | #352 (U23) | W ·
`block-a`/`block-b` · Z. 41–42 | **teilweise** — Block A/B selbst beschrieben, die Satzzeilen „… ·
Vorschlag/eigener Wert" unter Einspeisung/Eigenverbrauch fehlen | Ergänzung bei `block-a`: „Unter den
Positionen Einspeisung und Eigenstrom steht je eine Zeile mit dem angesetzten Satz und seiner Herkunft —
Vorschlag oder eigener Wert." | gering–mittel |"

**§ 7 „Logbuch" — Ursprung von „14+1 Sätze" (`:267–271`, `:277–296`, `:319–323`, wörtlich):**

> Grundlage: Statuszeilen #343–#366 sowie „Nach #340" bis „Nach #367" (zur Einordnung mitgelesen).
> Sammel-Upload laut „Nach #356 (c)" für den **28.09.2026**; Versionsnummer offen, Vorschlag **1.2.0.2**
> laut „Nach #353 (b)", zuletzt in „Nach #367 (h)" wiederholt — **beim Anwender noch zu bestätigen**.

Es folgt die Tafel „7.1 Vorschlag für den Sammel-Upload" mit **14 nummerierten Sätzen** (# 1–14, je ein
Thema mit Auftragsnummer(n) und Satzvorschlag, z. B. „1 | Kostendialog — Herleitung & Summenfuß (#345) |
…", bis „14 | Hilfsstrom je Anlage & Vorlagenhinweis (#366) | …"), gefolgt vom Satz:

> Alle 14 Sätze gegen die Tabuwörter-Regex geprüft: **0 Treffer.**

Der **„+1"** ist ein separat vorgelegter, noch nicht bestätigter 15. Satz (`:319–323`, wörtlich):

> Zusätzlicher Vorschlag zur Bestätigung (siehe 8.1):
>
> | # | Thema | Vorgeschlagener Satz |
> |---|---|---|
> | 15 (offen) | Rasterkarte — Fußzeilen-Zusatz (#361) | „Die Rasterkarte der Stromspeicher-Größen-Sicht
> zeigt den Zusatz zum Feinraster-Ergebnis jetzt vollständig unter dem Hinweistext." |

Die zugehörige offene Anwenderfrage (`:327–330`, wörtlich): „**#346 (KWKG-Pauschale) und #361
(Fußzeilen-Zusatz):** beide sind sichtbare, aber kleine Änderungen ohne bisherige
Logbuch-Entscheidung … Empfehlung: #346 wie entschieden ohne Eintrag lassen … #361 als Eintrag 15 mit
aufnehmen …"

Diese „14+1 Sätze" sind **Entwürfe für die Wiki-Seite „Update-Logbuch"** (eine Live-Wiki-Seite ohne
Repo-Quelle) zu den Statuszeilen #343–#367 (Kostendialog/Energieträger/BHKW/PV-Themen vom 15.–19.09.2026)
— nicht identisch mit den weit umfangreicheren, später entworfenen Logbuch-Sätzen in
`Wiki_Update_2026-09-26.md` (Abschnitt 9 dieses Blatts), die dieselbe Zeitspanne mit abdecken, aber auf
Version 1.2.0.3 statt 1.2.0.2 zielen und rund sechzig weitere Sätze für 1.2.0.4 hinzufügen.

**§ 6 „Hilfe-Zuordnung" (`:235–265`, gekürzt) — Ursprungstabelle der Dialoge:**

> Alle zwölf Dialoge tragen `<InfoKnopf Schluessel="@HilfeSchluessel" />` mit festem Default … Die
> Auflösung läuft — Stand heute — ausschließlich über die **eine** Datei
> `WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt` …

Tafel (`:246–257`, Kopf: Dialog | Hilfeschlüssel | Ziel in help_mapping.txt | Anker in der Wiki-Quelle
vorhanden? | Befund), die drei für E12 namentlich genannten Fälle wörtlich:

> | BhkwWirtschaftlichkeitDialog | `Form_BhkwWirtschaftlichkeit.btn_Help` | **kein Eintrag in
> help_mapping.txt** | — | **Hilfe-Taste wirkungslos** — `IHilfeDienst.Aufloesen` liefert `null` … |
> | PhotovoltaikVerguetungDialog | `Form_PhotovoltaikVerguetung.btn_Help` | `Kosten` (kein Anker) |
> Inhalt liegt tatsächlich auf **Wirtschaftlichkeit** `pv-verguetung` (Z. 32), nicht auf Kosten | Ziel zu
> grob/falsche Seite — Sprung landet auf der Kosten-Seite, nicht am eigentlichen Vergütungsabschnitt |

Schlusssatz (`:259–265`, wörtlich): „**Welche Dialoge haben keine Hilfe-ID:** keiner der zwölf … **Welche
laufen dennoch ins Leere:** `BhkwWirtschaftlichkeitDialog` … **Welche zeigen auf die falsche Seite:**
`PhotovoltaikVerguetungDialog` … Bei **acht der übrigen neun Dialoge** existiert der passende Anker in
der Wiki-Quelle bereits und müsste in `help_mapping.txt` nur als `#anker`-Zusatz ergänzt werden (reine
Konfigurationsänderung, kein Code-Umbau) — das Format trägt es laut Kopfkommentar der Datei bereits."

---

## 6. Konzept Hilfesystem, §§ 13.1–13.4 „Inhaltsregeln für die Wiki-Seiten" (wörtlich)

Quelle: `Dokumentation/aktuell/Konzept_Hilfesystem_Wikidokumentation.md`. Kopfsatz vor den Unterabschnitten
(`:744–749`):

> ## 13. Inhaltsregeln für die Wiki-Seiten
>
> Die Wiki-Seiten sind Hilfe- und Grundlagentexte für jeden Anwender. Zwei Regeln gelten für alle Seiten
> der Rubrik „Programm Dokumentation" und sinngemäß für alle Hilfe- und Grundlagenseiten. Für Seiten mit
> Repo-Quelle halten die Wächter im Kern beide Regeln, für Seiten ohne Repo-Quelle prüft die
> Orchestrierung vor jedem Upload mit denselben Mustern.

**§ 13.1 (`:751–755`):**

> ### 13.1 Nur der gültige Stand
>
> Eine Fachseite beschreibt die Funktion, wie sie jetzt ist. Was sich geändert hat, steht allein auf der
> Seite „Update-Logbuch" (Datum, Text, nach Version geordnet, neueste oben). Wortlaut, Tabuliste und
> Prüfmuster stehen im Abschnitt „Dokumentation" der Wurzel-`CLAUDE.md`.

**§ 13.2 (`:757–797`, vollständig):**

> ### 13.2 Keine Hersteller- und Produktdaten
>
> **Regel:** Keine Wiki-Seite nennt einen Hersteller, ein Produkt, eine Typbezeichnung oder Kennwerte,
> Preise und Datenblattangaben eines konkreten Produkts. Das gilt für Fließtext, Tabellen, Beispiele,
> Bildunterschriften, Menü- und Fehlertexte, Beispielausgaben des Programms und HTML-Kommentare.
>
> **Warum:** … Ein Produktname im Beispiel wirkt wie eine Empfehlung, altert mit dem Katalog und kann
> Marken- und Nutzungsrechte berühren.
>
> **Was erlaubt bleibt:** Datenquellen, Normen und Formate (VDI 3805, VDI 4640, VDI 6002, DIN, EN, ISO,
> die CEC-Listen …, PVsyst-Formate `.PAN`/`.OND`, bslib, GEMIS, UBA-Emissionsfaktoren, DWD und TRY,
> BDEW-Lastprofile) … Gattungsbegriffe und Technikklassen … Richtwerte ohne Produktbezug … Software- und
> Plattformnamen (Windows, iOS, WebView2, MediaWiki) …
>
> **Wie Beispiele geschrieben werden:** neutrale Namen mit rundem Kennwert — „Wärmepumpe A", „Modul B,
> 400 W", „Speicher 1, 100 kWh" …
>
> **Prüfung:** Der Wächter `EPOS.Kern.Tests/WikiProduktdatenWacheTests` hält die Repo-Quellen
> `EPOS.Kern/Allgemein/Hilfe/Berechnung/*.wiki` und `Projekte/Wiki/*.wiki` gegen die Hersteller- und
> Typnamen der Kataloge in `Referenzlaeufe/Kenndaten_Test.sqlite` … und gegen eine feste Liste bekannter
> Hersteller und Typcode-Muster. Seiten ohne Repo-Quelle … prüft die Orchestrierung vor jedem Upload mit
> derselben Liste.

Diese Fassung von § 13.2 enthält **noch nicht** den in Abschnitt 4 zitierten Vorschlag aus 06/§6.2 (Satz
zu Mockup-Beispielen).

**§ 13.3 (`:799–815`, vollständig):**

> ### 13.3 Veröffentlichung höchstens einmal je Woche
>
> **Regel:** Die Repo-Quellen der Wiki-Seiten werden mit jedem Auftrag fortgeschrieben und von den
> Wächtern geprüft; ins Wiki geladen wird gebündelt, höchstens einmal je Woche, für alle seit dem letzten
> Upload geänderten Seiten. Früher wird nur geladen, wenn eine Änderung wesentlich ist …
>
> **Warum:** Jeder Upload kostet Probe, Nachprobe, Revisionsvermerk und Logbuchpflege …
>
> **Ablauf:** Die Statusdatei führt ausstehende Seiten unter dem Punkt des jeweiligen Auftrags („Upload
> ausstehend: <Seiten>"). Der Wochenupload nimmt alle, prüft je Seite den Live-Stand gegen die Quelle
> (Regel 3 des Abschnitts „Bedienungsseiten mit Repo-Quelle"), lädt, vermerkt die Revisionen in diesem
> Konzept und veröffentlicht die gesammelten Logbuch-Einträge in einem Zug; die Versionsnummer für das
> Logbuch erfragt die Orchestrierung beim Anwender.

**§ 13.4 (`:817–827`, vollständig):**

> ### 13.4 Logbuch-Einträge knapp
>
> **Regel:** Das Update-Logbuch nennt je Version nur die wesentlichen Änderungen, die ein Anwender in der
> Bedienung oder im Ergebnis bemerkt … Ein Eintrag ist **ein Satz** mit Datum („Seit 16.09.2026 …") — ohne
> Nebensatz, Beispiel, Klammer oder Begründung; er sagt, was jetzt anders ist, nicht warum und nicht wie.
> Er nennt keine Dateien, Tabellen, Felder, Schlüssel, Klassen, Testzahlen oder Ursachen. Kleinigkeiten —
> Beschriftungen, Hinweise, Tooltips, Knopfpositionen, interne Umbauten, Behebungen ohne sichtbare
> Bedienänderung — bekommen keinen Eintrag … Mehrere Aufträge derselben Version zum selben Thema ergeben
> einen Eintrag. Richtwert: wenige Einträge je Version.

### Verfahren des Uploads — Werkzeug, Skript oder manuell?

Kein Skript und kein Werkzeug für das **Hochladen** einer Wiki-Seite wurde gefunden (repoweite Suche
nach `MediaWiki|api\.php|requests\.|mwclient` ergab nur Fundstellen in Doku- und Testdateien, keine
Werkzeugdatei). Die einschlägigen Regeln:

- **„Bedienungsseiten mit Repo-Quelle unter `Projekte/Wiki/`" (`:835–873`), Regel 5 (`:871–873`,
  wörtlich):** „**Hochgeladen wird nicht vom Agenten**, sondern von der Orchestrierung. Ein Auftrag
  liefert die Upload-Liste *Seitentitel → Quelldatei* im Abschlussbericht; damit ist nachvollziehbar,
  welche Datei welche Wikiseite ersetzt." — beantwortet „wer löst aus": die Orchestrierung (die
  anleitende Sitzung), nicht ein delegierter Agent.
- Regel 3 (`:864–866`, wörtlich): „**Vor dem Hochladen wird der Live-Stand gelesen und verglichen.** Ist
  er neuer, wird er ZUERST in die Repo-Quelle übernommen; erst dann wird ergänzt."
- „Dokumentationspflege Speicherauslegung – 11.09.2026" (`:831`, wörtlich, einziger Beleg für den
  technischen Weg): „Alle vier Texte wurden **über die MediaWiki-API** nach dem Speichern wieder gelesen
  und verglichen." — belegt nur die *Rücklesung/Kontrolle* über die API, nicht ausdrücklich das Schreiben
  selbst.
- § 6 „Teil C — Die neuen Hilfeseiten der Rubrik" (`:538–541`, wörtlich, zur ursprünglichen Anlage der
  Seiten): „**Anlage der 23 Seiten** am effizientesten per **XML-Import** über `Spezial:Importieren`
  (bewährtes Rezept: Interwiki-Präfix setzen, „Benutzer zuordnen" aktivieren, Zeitstempel jüngste
  Vergangenheit); **alternativ einzeln über das Bearbeitungsformular.**" — beides Browser-Wege in der
  MediaWiki-Oberfläche, kein Skript.
- § 8 „H1 — Katalog auf MediaWiki" (`:596–602`) beschreibt nur den **Lese**-Weg der App:
  `WikiHelpCatalog` lädt `list=allpages` mit `apprefix` von der Basis-URL — das befüllt den
  In-App-Hilfekatalog, ist aber kein Upload-Werkzeug (bestätigt auch
  `WindowsFormsApplication1/CLAUDE.md`, Abschnitt „Aufbau der Schale": „`WikiHelpCatalog` lädt die Rubrik
  „Programm Dokumentation" von `wiki.epos-plan.de`, Basis-URL aus dem Einstellwert `WordPressUrl`").

**Befund:** Das Hochladen selbst ist laut den gelesenen Quellen ein **manueller Browser-Vorgang in der
MediaWiki-Oberfläche von `wiki.epos-plan.de`** (Bearbeitungsformular oder XML-Import), ausgelöst von der
Orchestrierung (nicht von einem Agenten), mit anschließender Kontroll-Rücklesung über die MediaWiki-API.
Ein dediziertes Upload-Skript wurde nicht gefunden.

---

## 7. `CLAUDE.md`, Abschnitt „Wiki" (wörtlich, Zeilen 267–293)

Quelle: `CLAUDE.md:267–293` (identisch in der Repowurzel und im hier gelesenen Worktree):

> - **Wiki (`wiki.epos-plan.de`):** Die Seiten der Rubrik „Programm Dokumentation" — und sinngemäß alle
>   Hilfe- und Grundlagenseiten — beschreiben ausschließlich die Funktion, so wie sie jetzt ist.
>   **Änderungskommentare gehören nur in die Seite „Update-Logbuch"**, dort mit Datum und Text („Seit
>   01.09.2026 gilt …", „… wurde hinzugefügt", „… ist nicht mehr vorhanden"), geordnet nach Version
>   (neueste oben). Tabu auf Fachseiten: „seit …", „bisher/früher/vorher", „Anwenderentscheid", „Befund",
>   Auftrags-, Wellen- und Commit-Kürzel, „in Umsetzung, Stand …" — auch nicht in HTML-Kommentaren. Zu
>   jeder veröffentlichten Funktionsänderung einen Logbuch-Eintrag vorschlagen und die Versionsnummer beim
>   Anwender erfragen; **Einträge knapp: ein Satz je wesentlicher, sichtbarer Änderung, ohne Einzelheiten
>   und Begründung; Kleinigkeiten bekommen keinen Eintrag (Regel: Konzept Hilfesystem 13.4)**.
>   Wiki-Entwürfe vor dem Veröffentlichen mit
>   `seit (dem|der|W)|geändert|Entscheid|Befund|W\d+[a-z]?[‑-][A-Z][‑-]\d+|Stand:? *\d|bisher|früher|vorher|Bis dahin|Migrationsschritt`
>   gegenlesen.
>   **Keine Hersteller- und Produktdaten im Wiki:** kein Herstellername, keine Typbezeichnung, keine
>   Kennwerte, Preise oder Datenblattangaben eines konkreten Produkts — Beispiele tragen neutrale Namen
>   mit runden Werten („Speicher 1, 100 kWh"); Datenquellen, Normen und Formate (VDI 3805, CEC-Liste,
>   PVsyst-Formate) dürfen genannt werden. Der Wächter `EPOS.Kern.Tests/WikiProduktdatenWacheTests` hält
>   die Repo-Quellen gegen die Katalognamen der Testdatenbank; Regel und Prüfung: Konzept Hilfesystem,
>   Abschnitt 13.
>   **Veröffentlichung gebündelt:** Repo-Quellen werden je Auftrag fortgeschrieben und geprüft; ins Wiki
>   geladen wird höchstens einmal je Woche, gesammelt für alle seither geänderten Seiten. Früher nur bei
>   einer wesentlichen Änderung: eine neue oder geänderte Bedienung, die ein Anwender schon in Händen hat,
>   ein neuer Rechenweg oder eine Aussage, die nicht mehr zutrifft. Ausstehende Uploads stehen in der
>   Statusdatei; die Logbuch-Einträge werden mit dem Auftrag entworfen und mit dem Upload veröffentlicht
>   (Regel: Konzept Hilfesystem 13.3). Repo-Quellen der Bedienungsseiten: `Projekte/Wiki/*.wiki`; Konzept
>   und Zuordnung der Hilfe:
>   ``Konzept_Hilfesystem_Wikidokumentation.md`` (Dokumentation/aktuell/Konzept_Hilfesystem_Wikidokumentation.md).

---

## 8. `Dokumentation/aktuell/Wiki_Update_2026-09-26.md` — Struktur

391 Zeilen. Kopf (`:1–9`, wörtlich):

> # Wiki-Update 26.09.2026 — Vorbereitung des Sammel-Uploads
>
> Dieses Papier bereitet den gebündelten Wiki-Upload vom 26.09.2026 vor (Regel: Konzept Hilfesystem
> 13.3). Es listet die Seiten, deren Repo-Quelle seit dem letzten Upload (Version 1.2.0.0, Auftrag #252)
> fortgeschrieben wurde, sammelt die dazu entworfenen Logbuch-Sätze geordnet nach Version und nennt, was
> zum Stichtag noch offen ist. … Die Versionsnummern sind Vorschläge — beim Anwender zu bestätigen (Regel:
> Konzept Hilfesystem 13.3).

**Aufbau (Gliederung):**

- `## 1 Seiten für den Sammel-Upload` (`:11–37`) — Tafel mit Kopf „Wiki-Seite | Repo-Quelle | Was sich
  geändert hat | Quelle" und **elf** Zeilen: Klimadaten, Simulationsergebnisse, Stromspeicher,
  Hilfe-Assistent, **Wirtschaftlichkeit**, **Kosten**, Pufferspeicher, Gebäudemodell VDI 6007, Kühlung,
  Gerätekataloge, Simulation. Zur Seite Wirtschaftlichkeit (`:19`) allein eine einzige, sehr lange
  Tabellenzelle mit rund 40 genannten Ankern und Statuszeilen #372 bis #462.
- `## 2 Logbuch-Einträge für die Wiki-Seite „Update-Logbuch"` (`:39–308`) — zwei Versionsabschnitte:
  - `### Version 1.2.0.4` (`:45–252`) — rund 60 Sätze „Seit 26.09.2026: …", geordnet nach
    Statuszeilen #411 bis #463 (Diagramm-Umstellung, KI-Assistent-Wellen, E4–E10 der
    Wirtschaftlichkeit, Gebäudesimulation/Kühlung), mit Kopfvermerk (`:45`): „Anwenderentscheid
    22.09.2026 („letzte Nummer erhöhen"; das Programm trägt heute 1.2.0.3 in `AssemblyInfo.cs`, die
    Anhebung gehört zur Auslieferung)".
  - `### Version 1.2.0.3` (`:254–308`) — 15 Sätze „Seit 26.09.2026: …" zu den Statuszeilen #358–#403.
- `## 3 Offene Punkte` (`:310–391`) — „Seiten ohne Wiki-Quelle" (`:312–322`), „Was bis zum 26.09.2026
  noch dazukommt" (`:324–391`, u. a. die vollständige Revisionsliste der Wirtschaftlichkeit- und
  Kosten-Seite mit „Tabuwort-Regex 0 Treffer" je Nachtrag) und der Schlusshinweis (`:389–391`, wörtlich):
  „Ältere, mit „Version offen" oder „Version 1.2.0.1"/„1.2.0.2" vorbereitete Logbuch-Sätze aus
  Statuszeilen vor #358 sind für dieses Papier **nicht erneut geprüft**; vor dem Hochladen klären, ob sie
  in einer früheren Runde schon veröffentlicht wurden oder noch offen sind."

**Datum 26.09. gegen 28.09.:** Der Titel und jeder der rund 75 Logbuch-Sätze dieses Papiers tragen das
Datum **26.09.2026** („Seit 26.09.2026: …", durchgehend). Das Analysepapier (Abschnitt 1/3 dieses
Blatts), die Mockup-Prüfung P9 und Protokoll 06/§3.3 nennen dagegen durchgehend **28.09.2026** als
Sammel-Upload-Termin. `Wiki_Update_2026-09-26.md` selbst erklärt den Bezug in einer Randbemerkung
(`:27–30`, wörtlich): „Die Liste folgt der zusammenfassenden Aussage der Statuszeile #413: „Die Seiten
Klimadaten, Simulationsergebnisse, Stromspeicher und Wirtschaftlichkeit sind in den Repo-Quellen
fortgeschrieben — Sammel-Upload 28.09.2026" (das dort genannte Datum ist durch den vorliegenden Auftrag
auf den 26.09.2026 **vorgezogen**)." — die 26.09. ist demnach eine im Datei-eigenen Auftrag getroffene
Vorverlegung; ob sie mit dem Analysepapier (weiterhin 28.09., zuletzt am 24.09. so geschrieben, siehe
Abschnitt 1) abgeglichen ist, sagt keine der beiden Quellen ausdrücklich (siehe Unklarheiten).

---

## 9. Mockup `Dialog_Formel_Zahlenprobe.html` — U17/U23/U36 im Anhang „Umsetzungsstand"

5 358 Zeilen. Der Anhang beginnt bei `:4650` („`<h2>Anhang · Umsetzungsstand</h2><span
class="kat-marke">die einzige Stelle für Nichtgebautes</span>`"), mit Stand-Absatz (`:4659`, wörtlich):
„**Stand 24.09.2026.** Der Anwender hat am 20.09.2026 **alle Entscheidfragen Q1–Q25 der Mockup-Prüfung
und alle Entscheide A1–A20 des Analysepapiers nach der Empfehlung der Papiere entschieden**." — die
Datei ist damit auf demselben Tagesstand wie der hier gelesene Commit.

Die von 06/§0 und dem Analysepapier zitierte Zeilenspanne „Mockup:4344–4746" (19.09.2026-Fassung) trifft
im heute gelesenen Stand **nicht mehr** die U-Zeilen-Tafel — die Datei ist seither gewachsen (der Anhang
beginnt jetzt bei `:4650`, die einzelnen U-Zeilen liegen entsprechend später, siehe unten). Zeilen
4344–4746 der heutigen Fassung liegen noch vor dem Anhang, in der Szenario-Tafel „Was daraus im Lauf
wird" (V‑E).

**U17 (`:4901–4906`, wörtlich):**

> `<tr class="gestrichen"><td class="stand-zelle"><span class="stand erledigt">erledigt</span></td><td
> class="mono">U17</td><td class="num">7</td>`
> KWKG-Pauschale nach § 9 als Zeile im Jahr 0. Der Nachweisumschlag führt für sie keine Skalargröße
> — Fundstelle: Konzept § 6.3 — **erledigt:** Block A trägt hinter dem KWK-Zuschlag die Zeile „KWKG-Pauschale
> (§ 9 KWKG) [€, einmalig im Jahr 0]", nur wenn die Pauschale greift.

**U23 (`:4983–4991`, wörtlich, gekürzt):**

> `<tr class="gestrichen">…erledigt…</tr>` U23 · Spaltenwert „5 · 7" · Vermerk „eigener Wert {0} —
> Vorschlag {1}" an Satzzeile und Erlösrubrik: Der Nachweis je Modul trägt den angesetzten Satz und die
> Herleitung des Vorschlags, vergleicht beide aber nicht — Fundstelle `KwkgModulNachweis` ·
> `WirtschaftlichkeitZeilen` — **erledigt:** Der Nachweis führt den Vorschlag nun auch als ZAHL …; die
> Erlösrubrik trägt unter der Einspeisung und dem Eigenstrom je eine eingezogene Textzeile — „Satz
> Einspeisung · …"

**U36 (`:5119–5127`, wörtlich, gekürzt):**

> `<tr class="gestrichen">…erledigt…</tr>` U36 · Spaltenwert „6" · Die Warnzeile „über 2 MW: Stromsteuer
> auf Eigenverbrauch prüfen." der Gruppe Anlage kann nie erscheinen: Die Prüfung auf 1 MW greift zuerst
> und deckt jede größere Anlage mit ab. Beide Warnungen sollen nebeneinander stehen können, sobald ihre
> Grenze überschritten ist — Fundstelle `PhotovoltaikVerguetungDialog.Anlagenwarnung` ·
> `PVW_WARN_AUSSCHREIBUNG` · `PVW_WARN_STROMSTEUER` — **erledigt:** Zwei unabhängige Schwellen aus zwei
> Gesetzen, jede für sich geprüft; über 2 MW stehen beide Banner nebeneinander (`Anlagenwarnungen`).

Alle drei sind, wie in der E12-Zeile des Analysepapiers vorausgesetzt, im Mockup-Anhang als
**„erledigt"** (durchgestrichen mit Grund) markiert — Zeilenklasse `gestrichen`, Status-Chip „erledigt".
Die Spaltenwerte „7"/„5 · 7"/„6" hinter der U-Nummer sind eine Kategorie-Kennzahl des Mockups (vgl.
Text um `:4679–4680`: „Kategorie 4, Trägerkarte …", „Kategorie 7"); welche Kategorienbezeichnung im
Einzelnen gemeint ist, wurde nicht vertieft geprüft.

Zusätzliche Höfingen-Fundstellen im selben Mockup (außerhalb des Anhangs, siehe auch Abschnitt 12):
`:4469`, `:4642`, `:5291`.

---

## 10. `help_mapping.txt` — aktueller Stand zu Tarifstruktur, BHKW, PV und den „acht Ankern"

Datei: `WindowsFormsApplication1/Allgemein/Hilfe/help_mapping.txt`, 608 Zeilen. Kopf (`:1–18`, wörtlich,
Auszug):

> # help_mapping.txt - Zuordnung Steuerelement -> Hilfeseite im Wiki
> …
> Diese Datei ist die EINZIGE Stelle, an der Hilfe gepflegt wird. Sie wird als eingebettete Ressource
> mitgeliefert (F2). Eine gleichnamige Datei NEBEN DER EXE uebersteuert die eingebettete Fassung
> vollstaendig …
> Seit H3 gilt das woertlich: ein neuer Infobutton braucht KEINE Zeile Programmtext mehr. …
> Kodierung: UTF-8 (mit BOM). …

**Die drei in der E12-Zeile genannten Einzelfälle — heutiger Stand (Zeilen 264–267, 333, 348):**

> ```
> Form_PhotovoltaikVerguetung.btn_Help = Wirtschaftlichkeit#pv-verguetung
> Form_Tarifstruktur.btn_Help = Wirtschaftlichkeit#strombezug
> …
> Form_BhkwWirtschaftlichkeit.btn_Help = Wirtschaftlichkeit#bhkw-wirtschaftlichkeit
> …
> Form_Gesetzesparameter.btn_Help = Gesetzesparameter
> ```

Vergleich mit den 19.09.2026-Befunden (Abschnitt 4/5): `Form_Tarifstruktur` zeigte damals auf `Kosten`
(ohne Anker), `Form_PhotovoltaikVerguetung` auf `Kosten` (ohne Anker, „zu grob"),
`Form_BhkwWirtschaftlichkeit` hatte **keinen Eintrag** („Hilfe-Taste wirkungslos"). **Alle drei sind im
heute gelesenen Stand bereits auf die von 06/§4 vorgeschlagenen Ziele korrigiert** —
`Wirtschaftlichkeit#strombezug`, `Wirtschaftlichkeit#pv-verguetung`, `Wirtschaftlichkeit#bhkw-wirtschaftlichkeit`
— und alle drei Anker existieren nachweislich in `Programm Dokumentation - Wirtschaftlichkeit.wiki`
(Zeilen 111, 110, 99 — siehe Abschnitt 11). Die Mockup-Prüfung nennt dafür ausdrücklich die Statuszeile
**#405** (Abschnitt 3, Zitat „P9"). `Form_Gesetzesparameter` zeigt dagegen unverändert auf die Seite
„Gesetzesparameter" **ohne Repo-Quelle** — A18 (Abschnitt 13) ist hier erkennbar **nicht** ausgeführt.

**Die „acht Anker" (weitere, laut 06/§4 „bereit, aber unverdrahtet") — heutiger Stand der in 05/§6
namentlich genannten Dialoge (Zeilen 244, 246, 248, 260, 312, 324, 350, 169):**

> ```
> Form_Energietraeger.btn_Help = Kosten#energietraegerverwaltung
> Form_KostenKomponente.btn_Help = Kosten#komponentenliste
> Form_Nutzungsdauer.btn_Help = Kosten#nutzungsdauern
> Form_VorlagenUebernahme.btn_Help = Kosten#uebernahme-vorlage
> UcBkKosten.btn_Help = Kosten
> Form_WirtschaftlichkeitParameter.btn_Help = Wirtschaftlichkeit#parameter
> Form_Emissionskatalog.btn_Help = Emissionen
> UcWirtschaftlichkeit.btn_Help       = Wirtschaftlichkeit
> ```

Fünf der 2026-09-19 als „bereit, aber unverdrahtet" befundenen Dialoge (`Form_KostenKomponente`,
`Form_Energietraeger`, `Form_WirtschaftlichkeitParameter`, `Form_Nutzungsdauer`,
`Form_VorlagenUebernahme`) tragen im heute gelesenen Stand bereits einen `#anker`-Zusatz. Für
`Form_WirtschaftlichkeitVerlauf` (KapitalwertVerlaufDialog, laut 05/§6 „selbst wenn verdrahtet würde,
fehlt der Anker auf der Zielseite noch") wurde kein Treffer in `help_mapping.txt` gesucht; der Wiki-Anker
`verlauf` existiert inzwischen auf der Wirtschaftlichkeit-Seite (Abschnitt 11).

---

## 11. Wiki-Anker der drei Seiten Wirtschaftlichkeit, Kosten, Photovoltaik

**`Programm Dokumentation - Wirtschaftlichkeit.wiki`** (132 Zeilen, **67** `{{Anker|…}}`-Tags — die Zahl
deckt sich mit dem in `Wiki_Update_2026-09-26.md` mehrfach genannten „67 Anker eindeutig", z. B. `:363`,
`:368`). Die für E12 einschlägigen Anker, alle vorhanden: `bhkw-wirtschaftlichkeit` (`:99`),
`pv-verguetung` (`:110`), `strombezug` (`:111`), **`gesetzliche-parameter`** (`:112`).

Anker `gesetzliche-parameter` (`:112`, wörtlich, gekürzt): „**Gesetzliche Parameter** – die
jahresscharfen Sätze, mit denen die Wirtschaftlichkeit rechnet – KWK-Zuschlag, EEG-Vergütung, Energie-
und Stromsteuer, CO₂-Preis, Umsatzsteuer und die Bilanzkonvention –, stehen in einem eigenen Katalog.
Gepflegt wird er unter {{Menü|Administration|Kosten|Gesetzliche Parameter…}}. …" Dieser Abschnitt steht
auf der Seite **Wirtschaftlichkeit**, nicht auf der Seite **Kosten** — siehe Abschnitt 13 zu A18.

Alter Bestand laut 06/§1 (19.09.2026): „Wirtschaftlichkeit.wiki | 71 | 34 Tags/Namen" — die Seite ist
seither auf 132 Zeilen und 67 Anker gewachsen (E4–E10 der Wirtschaftlichkeit sind seither gebaut worden,
siehe `e10_fakten.md`).

**`Programm Dokumentation - Kosten.wiki`** (175 Zeilen, **44** `{{Anker|…}}`-Tags, davon zwei mit
Doppelnamen: `{{Anker|energietraegerverwaltung|energietraeger}}` `:68`,
`{{Anker|nutzungsdauern|afa}}` `:130`). Kein Anker zu „gesetzlich"/„Gesetzeskatalog"/„Gesetzesparameter"
gefunden — der einzige Treffer des Worts „gesetzlich" (`:108`) betrifft die KWKG-/Stromsteuer-Umlagen,
nicht den Gesetzeskatalog-Dialog. Alter Bestand laut 06/§1: „Kosten.wiki | 168 | 41 Tags / 43 Namen".

**`Programm Dokumentation - Photovoltaik.wiki`** (49 Zeilen, **9** `{{Anker|…}}`-Tags, davon einer mit
Doppelnamen `{{Anker|wechselrichter|straenge}}` `:17`) — unverändert gegenüber dem 06/§1-Bestand („49 |
9"); die Datei wurde laut `git log` zuletzt am 13.09.2026 geändert (Abschnitt 14).

---

## 12. Die Wachen

**`EPOS.Kern.Tests/WikiProduktdatenWacheTests.cs`** (564 Zeilen). Klassendoku (`:13–64`, gekürzt,
wörtlich):

> Der Wächter über die INHALTE der Wiki-Quellen: **keine Hersteller- und keine Produktdaten** (Konzept
> Hilfesystem, Abschnitt 13.2; Wurzel-`CLAUDE.md`, Abschnitt „Dokumentation"). … **Was der Wächter
> liest.** Die beiden Ablagen der Wiki-Quellen im Repository: `EPOS.Kern/Allgemein/Hilfe/Berechnung/*.wiki`
> … und `Projekte/Wiki/*.wiki` … Seiten OHNE Repo-Quelle erreicht er nicht; die prüft die Orchestrierung
> vor jedem Upload mit derselben Liste. **Drei Quellen der verbotenen Namen.** (1) Die Katalognamen der
> Testdatenbank … (2) Eine feste Liste bekannter Hersteller und Produktlinien … (3) Ein Typcode-Muster …

Fünf `[Fact]`-Testmethoden (`:179–306`): `Keine_Wikiquelle_nennt_Hersteller_oder_Produktdaten`,
`Der_Leser_findet_alle_drei_Arten`, `Erlaubte_Begriffe_treffen_nicht`,
`Die_Katalognamen_kommen_vollzaehlig_und_ohne_Platzhalter`, `Der_Waechter_sieht_beide_Ablagen_der_Wikiquellen`.
Geprüft werden Hersteller-/Produktdaten (Regel 13.2), **nicht** Tabuwörter (13.1) und **nicht**
Anker-/Linkgültigkeit.

**Tabuwort-Regex** — wörtlich aus `CLAUDE.md:278` (siehe Abschnitt 7):

```
seit (dem|der|W)|geändert|Entscheid|Befund|W\d+[a-z]?[‑-][A-Z][‑-]\d+|Stand:? *\d|bisher|früher|vorher|Bis dahin|Migrationsschritt
```

**Eigener Probelauf dieser Regex über `Projekte/Wiki/*.wiki`** (heute, nur gelesen, Trefferzahl je
Datei):

| Datei | Treffer |
|---|---|
| Programm Dokumentation - Hilfe-Assistent.wiki | 3 |
| Programm Dokumentation - Photovoltaik.wiki | 3 |
| Programm Dokumentation - Simulation.wiki | 1 |
| Programm Dokumentation - Simulationsergebnisse.wiki | 1 |
| Programm Dokumentation - Stromspeicher.wiki | 9 |
| **alle zwölf übrigen Dateien (u. a. Kosten, Wirtschaftlichkeit, Varianten, Emissionen, Klimadaten,
| Pufferspeicher, Kühlung, Gerätekataloge, Gebäudemodell VDI 6007, Brauchwasser-Zapfprofil,
| Projekttransfer)** | 0 |

**Summe: 17 Treffer über 5 von 16 Dateien.** Zum Vergleich der 19.09.2026-Befund (05/§5.1, Abschnitt 5
dieses Blatts): „**20 Treffer**" über sechs Dateien, davon Wirtschaftlichkeit.wiki mit sechs Treffern
(„Entscheid…", 6×, als fachlicher Begriff „Vorschlag zur Entscheidung" bewertet). Im heute gelesenen
Stand trägt Wirtschaftlichkeit.wiki **0** Treffer — die Seite wurde seither vollständig neu gefasst
(E4–E10); das deckt sich mit den wiederholten Vermerken „Tabuwort-Regex 0 Treffer" in
`Wiki_Update_2026-09-26.md` (z. B. `:334`, `:344`, `:353`, `:357`, `:360`, `:363`, `:369`, `:373`, `:376`).

**Link-/Ankerprüfung:** Kein Werkzeug gefunden. Eine breite Suche nach Testklassen mit „Wache"/„Test" im
Namen (29 Treffer) enthält keine, die `help_mapping.txt` gegen die `{{Anker|…}}`-Tags der
`*.wiki`-Dateien oder umgekehrt prüft; `DokumentationLinkWacheTests` (CLAUDE.md, Abschnitt
„Dokumentation") prüft nur relative Verweise zwischen `Dokumentation/*.md`-Papieren, nicht
Wiki-Anker/`help_mapping`-Ziele. Die einzigen Ankerprüfungen im Bestand sind die **manuellen** Abgleiche
der Protokolle 05/§6 und 06/§4 (Abschnitt 4/5 dieses Blatts).

---

## 13. A16 „Höfingen neutralisieren" — alle Fundstellen

**`Projekte/Wiki/*.wiki` (alle 16 Dateien): 0 Treffer** — „Höfingen" ist heute in keiner Wiki-Quelle
enthalten.

**`Dokumentation/aktuell`, alle Fundstellen (Grep „Höfingen", ohne die bereits in Abschnitt 4/9 zitierten
Protokoll- und Entscheidzeilen):**

| Datei:Zeile | Fundtext (Kurzform) |
|---|---|
| `Mockups/Dialog_Formel_Zahlenprobe.html:4469` | „Die Höfingen-Mappe ist die externe Gegenprobe" |
| `Mockups/Dialog_Formel_Zahlenprobe.html:4642` | „… schließen die Verlaufsendwerte … Die Höfingen-Mappe wird mit …" |
| `Mockups/Dialog_Formel_Zahlenprobe.html:5291` | „Kapitalwert der Höfingen-Mappe … 65.259 €" |
| `Wirtschaftlichkeit_Kosten/Beispielprojekt.md:192` | „Höfingen `Tab_kurz_KWKG2020` | Mehrinvestition 55.745 · NPV 65.259 € · IZF 20,4 % · 4,33 a | `Rechenweg/08`" |
| `Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md:88` | A16-Zeile (Abschnitt 2) |
| `Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md:144, 382, 404–405, 438, 463, 497, 646, 673` | siehe Abschnitte 1–3 |
| `Wirtschaftlichkeit_Kosten/2026-09-19_Pruefung_Mockups_Wirtschaftlichkeit.md:136` | „Kennzahlen, Bandbreiten, Verlauf, Sicht 2 und Höfingen-Gegenprobe treffen." |
| `Wirtschaftlichkeit_Kosten/LIESMICH.md:39` | „08_Wirtschaftlichkeit_Nutzungsdauer.md — Kapitalwert nach DIN EN 17463, Höfingen-Gegenprobe" |
| `Wirtschaftlichkeit_Kosten/LIESMICH.md:105` | „… Aufschlagsmessung Projekt 1030, Höfingen-Kapitalwert" |
| `Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md:737` | „… als reales Zahlenbeispiel die Altmappe `Quellen\BHKWPlan\BHKW_Höfingen_Erneuerung_20kWel.XLS`, Blatt `Tab_kurz_KWKG2020`." (§ 2.11) |
| `…konsolidiert.md:812` | „Beispielzahlen in den Mockups aus der Höfingen-Mappe (20-kW-BHKW-Erneuerung gegen benannte Vergleichsheizung — Kapitalwert 65.259 €, IZF 20,4 %, Amortisation 4,33 a)." (§ 2.11.3) |
| `…konsolidiert.md:1119` | „(Kaskadenprobe 1042, Mischsatz 300 kW, AW 300 kWp, Höfingen) sind als solche gekennzeichnet." |
| `Wirtschaftlichkeit_Kosten/Rechenweg/08_Wirtschaftlichkeit_Nutzungsdauer.md:15–16` | „Zahlen aus der realen Höfingen-Mappe `BHKW_Höfingen_Erneuerung_20kWel.XLS`, Blatt `Tab_kurz_KWKG2020` — 20-kW-BHKW-Erneuerung gegen benannte Vergleichsheizung." |
| `…08_Wirtschaftlichkeit_Nutzungsdauer.md:108` | Abschnittsüberschrift „## Berechnungserläuterung an der Höfingen-Mappe" |
| `Status_iOS_Migration.md:299, 539–540` | (nicht einzeln gelesen — Statuszeilen-Erwähnungen) |

Die Datei `Quellen/BHKWPlan/BHKW_Höfingen_Erneuerung_20kWel.XLS` liegt laut 06/§5.1 tatsächlich im
Repository (nicht erneut geprüft).

**Bemerkenswert:** `Beispielprojekt.md`, von 06/§5.1 ausdrücklich als Vorbild für einen „bereits
neutralen" Dateinamen genannt („passend zum bereits neutralen `Beispielprojekt.md`"), nennt „Höfingen"
in seiner eigenen Kontrollrechnungs-Tafel (`:192`) selbst noch wörtlich und ungerundet.

---

## 14. A18 „Abschnitt auf der Seite Kosten" — was er enthalten soll

**Wortlaut der Frage und Empfehlung** (Analysepapier `:440`, Abschnitt 2 dieses Blatts): „Hilfe: eigene
Wiki-Seite „Gesetzliche Parameter" anlegen oder `help_mapping` auf einen Abschnitt der Kostenseite
umbiegen? | Abschnitt auf der Seite Kosten (Menüort seit #372)".

**06/§6.1 „Fehlt eine Bedienungsseite …" (`:197–201`, wörtlich, zu „Gesetzesparameter"):**

> **„Gesetzesparameter"** — **echte Lücke.** help_mapping.txt verweist mit `Form_Gesetzesparameter.btn_Help
> = Gesetzesparameter` (help_mapping:334) auf einen eigenständigen Seitentitel, für den es weder in der
> Bedienungsseiten-Tabelle noch unter `Projekte/Wiki/` eine Repo-Quelle gibt … Empfehlung: entweder die
> Seite mit Repo-Quelle anlegen (Hausregel Hilfesystem:855–858) oder das Ziel in help_mapping auf einen
> bestehenden Anker umbiegen (**Kandidat: neuer Abschnitt in K.wiki**, da der Gesetzeskatalog dort
> administrativ neben Nutzungsdauern und Energieträgern sitzen würde).

**Heutiger Befund (Abschnitte 10–11):** `help_mapping.txt:348` zeigt unverändert auf
`Gesetzesparameter` (keine Repo-Quelle, nicht ausgeführt). Ein Abschnitt existiert bereits — aber unter
dem Anker `gesetzliche-parameter` auf **`Programm Dokumentation - Wirtschaftlichkeit.wiki:112`**, nicht
auf `Kosten.wiki` wie von A18 und 06/§6.1 vorgeschlagen. Der Abschnitt selbst beschreibt den
Gesetzeskatalog vollständig (Inhalt, Menüpfad `Administration → Kosten → Gesetzliche Parameter…`,
Suche/Trichter/Sortierung, Jahreszeilen-Regel) — siehe Zitat in Abschnitt 11.

---

## 15. Stand der Wiki-Quellen

**`git log --oneline -15 -- Projekte/Wiki`:**

```
a87ea32d Merge origin/ios_migration_september (E10 #463, Schema 120) nach #465
f07a24ef Papiere: Logbuch-Saetze #463, Wiki-Quellen Kosten und Geraetekataloge
3dd0348a Merge #465: Gebaeudeverwaltung editierbar und fuer den Assistenten steuerbar
e8e9ed4c Papiere #465: Gebaeudeverwaltung editierbar und steuerbar
84eb0328 Papiere #466: Konzept 7.1 (d) erledigt, Wiki Geraetekataloge
48d8836d Merge papiere462: Papiere zu #462 (E9b Szenariopflege in den Dialogen, Ausweis)
14d91785 Papiere: Logbuch-Saetze #462, Wiki-Quelle Wirtschaftlichkeit
f06c8c9e Merge origin/ios_migration_september (b7572d42): E34-Schritt wird 119
84e743fc Merge #458 Stufe 3b: Zahlenreihen im Assistenten (reihe_setzen)
8da525ce Papiere #458 Stufe 3b: Zahlenreihen im Rahmen und im Wiki
5c2e7b1d Merge origin/ios_migration_september (E9a #461, Schema 118) nach #458 Stufe 3a
2e355650 Papiere #458/3a: Konzept Dialogintegration, Wiki Hilfe-Assistent
61e4a556 Papiere: Logbuch-Satz #461, Wiki-Quelle Wirtschaftlichkeit
cce2e963 Merge origin/ios_migration_september (a1df2dbe) nach KU2 Welle 3
58be82c9 Papiere Z3: Nachtrag N12, Protokoll, Statuszeile #453, Uebergabe 10
```

**Letztes Änderungsdatum je Datei** (`git log -1 --format=%ad --date=short`):

| Datum | Datei |
|---|---|
| 2026-09-13 | Programm Dokumentation - Emissionen.wiki |
| 2026-09-13 | Programm Dokumentation - Photovoltaik.wiki |
| 2026-09-19 | Programm Dokumentation - Projekttransfer.wiki |
| 2026-09-19 | Programm Dokumentation - Varianten.wiki |
| 2026-09-20 | Programm Dokumentation - Simulationsergebnisse.wiki |
| 2026-09-20 | Programm Dokumentation - Stromspeicher.wiki |
| 2026-09-21 | Programm Dokumentation - Pufferspeicher.wiki |
| 2026-09-24 | Programm Dokumentation - Brauchwasser-Zapfprofil.wiki |
| 2026-09-24 | Programm Dokumentation - Gebäudemodell VDI 6007.wiki |
| 2026-09-24 | Programm Dokumentation - Gerätekataloge.wiki |
| 2026-09-24 | Programm Dokumentation - Hilfe-Assistent.wiki |
| 2026-09-24 | Programm Dokumentation - Klimadaten.wiki |
| 2026-09-24 | Programm Dokumentation - Kosten.wiki |
| 2026-09-24 | Programm Dokumentation - Kühlung.wiki |
| 2026-09-24 | Programm Dokumentation - Simulation.wiki |
| 2026-09-24 | Programm Dokumentation - Wirtschaftlichkeit.wiki |

**Seit dem letzten Upload (Version 1.2.0.0, Auftrag #252, laut `Wiki_Update_2026-09-26.md:4–5`)**
geänderte Seiten: laut der Tafel in Abschnitt 8 (`Wiki_Update_2026-09-26.md` § 1) sind das genau die elf
dort aufgeführten — Klimadaten, Simulationsergebnisse, Stromspeicher, Hilfe-Assistent, Wirtschaftlichkeit,
Kosten, Pufferspeicher, Gebäudemodell VDI 6007, Kühlung, Gerätekataloge, Simulation; Emissionen,
Photovoltaik, Projekttransfer, Varianten und Simulationsergebnisse (bereits genannt) fehlen entsprechend
in dieser Liste — deckt sich mit den frühesten Änderungsdaten (13./19./20.09.) in der Tafel oben, die vor
dem Auftrag #252-Nachfolgezeitraum liegen könnten (nicht geprüft, ob #252 selbst vor oder nach dem
19.09.2026 lag).

---

## Unklarheiten

1. **Datum 26.09. gegen 28.09.2026.** Das Analysepapier (Stand 24.09.2026, Abschnitt 1/3), die
   Mockup-Prüfung (P9) und Protokoll 06/§3.3 nennen den Sammel-Upload durchgehend für **28.09.2026**.
   `Wiki_Update_2026-09-26.md` ist selbst auf den **26.09.2026** datiert und trägt rund 75 Logbuch-Sätze
   mit diesem Datum; es erklärt die Verschiebung als „durch den vorliegenden Auftrag auf den 26.09.2026
   vorgezogen" (`:27–30`), ohne dass eine Gegenquelle diese Vorverlegung bestätigt oder das Analysepapier
   nachgezogen wäre. Beide Termine liegen, von „heute" (24.09.2026) aus gesehen, noch in der Zukunft.
2. **Wer löst den Upload aus und womit?** Die Konzeptregeln sagen „nicht vom Agenten, sondern von der
   Orchestrierung" (Hilfesystem-Konzept `:871–873`) und beschreiben als Weg ausschließlich
   Browser-Arbeit in der MediaWiki-Oberfläche (Bearbeitungsformular oder XML-Import, `:538–541`) mit
   API-gestützter Rücklesung zur Kontrolle (`:831`). Ein Skript oder Werkzeug für das Schreiben selbst
   wurde nicht gefunden; ob der Schreibvorgang selbst über die MediaWiki-API oder ausschließlich über das
   Bearbeitungsformular im Browser läuft, ist aus den gelesenen Quellen nicht abschließend zu entnehmen.
3. **„Hilfesystem 13.2 ergänzt" — konkreter Inhalt.** 06/§6.2 schlägt einen genauen Satz für § 13.2 vor
   (Mockup-Beispiele einschließen). Der heute gelesene § 13.2 (Abschnitt 6) enthält diesen Satz **nicht**
   — die E12-Zeile „Hilfesystem 13.2 ergänzt" bezieht sich damit erkennbar auf diesen noch ausstehenden
   Nachtrag, aber keine Quelle bestätigt das ausdrücklich als den einzig gemeinten Inhalt.
4. **„14+1 Sätze" — welche Sätze genau, und stehen sie schon in den Quellen?** Die 14+1 Sätze stammen
   wörtlich aus 05/§7 (Stand 19.09.2026, Statuszeilen #343–#367, Zielversion 1.2.0.2) und sind
   **Entwürfe**, nicht veröffentlicht (keine Repo-Quelle für die Live-Seite „Update-Logbuch"). Das später
   entstandene `Wiki_Update_2026-09-26.md` (Abschnitt 8/9) deckt denselben Zeitraum mit einem
   Versionssprung (1.2.0.3 statt 1.2.0.2) und rund 75 statt 15 Sätzen ab und erwähnt die „14+1" nicht
   namentlich. Ob die E12-Zeile des Analysepapiers exakt die 15 Sätze aus 05/§7 meint oder inzwischen den
   größeren Bestand aus `Wiki_Update_2026-09-26.md`, ist nicht entschieden; keine Quelle stellt die
   Verbindung ausdrücklich her.
5. **„Fünf Lücken" und „U17/U23/U36" — Überschneidung.** Die maßgebliche Definition der fünf Lücken
   (05/§8.1 Punkt 5, Abschnitt 5) zählt **U23-Satzzeilen** und **beide PV-Anlagenwarnungen (U36)**
   bereits zu den fünf Lücken. Das Analysepapier (W2, `:380`, und die E12-Zeile `:497`) nennt
   „U17/U23/U36" daneben als **eigenen, zusätzlichen** Punkt neben „die fünf Lücken" — 06/§0 spricht von
   „zwei weiteren" (U23, U36) „zu den fünf Lücken … hinzu". Ob U23/U36 einmal (als Teil der fünf Lücken)
   oder zweimal (fünf Lücken **plus** U17/U23/U36) im E12-Sammel-Upload berücksichtigt werden sollen,
   ist zwischen den Quellen nicht deckungsgleich formuliert.
6. **„Acht Anker" — Arithmetik nicht nachvollzogen.** 06/§4 spricht von „acht **weitere**" (nach Abzug
   von BhkwWirtschaftlichkeit und PhotovoltaikVerguetung von den zwölf geprüften Dialogen); 05/§6 selbst
   schreibt „acht der **übrigen neun** Dialoge" — zwölf minus zwei ergibt rechnerisch zehn, nicht neun.
   Die eigene Auszählung der Tafel in 05/§6 (Abschnitt 5) ergab fünf Zeilen mit der Bewertung „Anker
   bereit, nicht verdrahtet" (KostenKomponente, Energieträger, WirtschaftlichkeitParameter,
   Nutzungsdauer, VorlagenUebernahme). Welche acht Dialoge exakt gemeint sind, lässt sich aus den
   gelesenen Tafeln nicht lückenlos rekonstruieren.
7. **help_mapping-Korrekturen scheinen bereits ausgeführt, E12 gilt aber als nicht begonnen.** Die
   Tarifstruktur-, BHKW- und PV-Korrektur sowie mindestens fünf der „acht Anker" stehen bereits so im
   heute gelesenen `help_mapping.txt` (Abschnitt 10), laut Mockup-Prüfung P9 seit Statuszeile **#405**
   (vor Beginn von E12). Die E12-Zeile führt „`help_mapping` (Tarifstruktur, BHKW, PV, acht Anker)"
   dennoch als Teil ihres eigenen, noch offenen Inhalts. Beides schließt sich nicht zwingend aus (§405
   könnte die Code-Zuordnung vorweggenommen haben, während der eigentliche **Wiki-Upload** dieser Anker
   noch aussteht), aber keine gelesene Quelle bestätigt diese Lesart ausdrücklich.
8. **Zeilennummern der 19.09.2026-Protokolle (05, 06) stimmen nicht mehr mit dem heutigen Stand
   überein.** Analysepapier, Konzept und Mockup sind zwischen dem 19. und 24.09.2026 erheblich
   gewachsen (Konzept z. B. von rund 2 630 auf 2 976 Zeilen, Mockup-Anhang-Beginn von zitiert „:4344" auf
   tatsächlich „:4650"). Alle in 05/06 zitierten Zeilennummern sind entsprechend historisch und wurden in
   diesem Blatt, wo möglich, durch heutige Fundstellen ergänzt oder ersetzt.
9. **Versionsnummern 1.2.0.1–1.2.0.4.** 05/§7 schlug am 19.09.2026 „1.2.0.2" vor;
   `Wiki_Update_2026-09-26.md` geht von einem bereits in `AssemblyInfo.cs` stehenden **1.2.0.3** aus und
   entwirft Sätze für **1.2.0.4**; „1.2.0.1" und „1.2.0.2" werden dort nur als „ältere … Logbuch-Sätze …
   nicht erneut geprüft" erwähnt (`:389–391`) — offen, ob sie in einer früheren Runde bereits
   veröffentlicht wurden.
10. **A18-Zielseite: Kosten oder Wirtschaftlichkeit?** A18 (entschieden) und 06/§6.1 (Kandidatenvorschlag)
    benennen die Seite **Kosten** als Ziel eines neuen Abschnitts „Gesetzliche Parameter". Der heute
    bereits vorhandene, inhaltlich passende Abschnitt (Anker `gesetzliche-parameter`, vollständig
    ausformuliert) steht jedoch auf der Seite **Wirtschaftlichkeit** (`:112`). `help_mapping.txt` verweist
    auf keine der beiden Stellen. Keine Quelle erklärt, ob der bestehende Abschnitt auf
    Wirtschaftlichkeit umgezogen/verlinkt werden soll oder ob A18 einen zusätzlichen eigenen Abschnitt auf
    Kosten verlangt.
11. **`Beispielprojekt.md` als „bereits neutral" vorgeschlagenes Vorbild nennt „Höfingen" selbst.** 06/§5.1
    empfiehlt den Namen „Beispielprojekt B" „passend zum bereits neutralen `Beispielprojekt.md`"; dieselbe
    Datei führt in ihrer eigenen Kontrollrechnungs-Tafel (`:192`) „Höfingen" mit den unveränderten realen
    Zahlen. Ob `Beispielprojekt.md` selbst unter A16 fällt (interne QA-Gegenrechnung, keine
    Anwender-Wiki-Seite) oder ebenfalls neutralisiert werden soll, ist nicht entschieden.
12. **Kein Werkzeug für die Anker-/Link-Prüfung.** Weder für die Gültigkeit der `#anker`-Ziele in
    `help_mapping.txt` gegen die `{{Anker|…}}`-Tags der `*.wiki`-Quellen noch für die Konsistenz mit dem
    Live-Wiki existiert ein automatischer Wächter; die einzigen bekannten Prüfungen sind die manuellen
    Tafeln der Protokolle 05/§6 und 06/§4, beide vom 19.09.2026 und seither nicht wiederholt.
