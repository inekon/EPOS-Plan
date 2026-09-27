# Faktenblatt E7c — rechenwirksame Lücken der Wirtschaftlichkeit, Teil c

Quellenstand: Worktree `C:\Waermeplan\EPOS-Plan\.claude\worktrees\q2` (435b9810). Nur gelesen,
nichts verändert. Zeilennummern beziehen sich auf die genannten Dateien in diesem Worktree.

---

## 1. K‑1 — Der zweite Fall des § 2 Nr. 16 KWKG (Abwärmeabfuhr)

### Registerzeilen (wörtlich)

`Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md`, Zeile 64 (A2\*):

> | **A2\*** | K‑1: Stromkennzahl und Abwärmeabfuhr je Anlage (Schritt **A**, § 6) — Nutzwärme je Modul aus dem Ergebnismodell oder Aufteilung nach Leistung? | Vor der Umsetzung messen, ob die modulscharfe Nutzwärme vorliegt; sonst Aufteilung nach P_el mit Herleitungszeile | 20.09.2026, nach Empfehlung | § 3.6 (Befund K‑1), § 4; EZ‑5 | entschieden, nicht gebaut; **gemessen mit E7a (#437):** die Wärmeproduktion liegt je Modul vor, der Wärmeüberschuss nur als Projektsumme (in allen BHKW-Basisprojekten 0) — es greift die Aufteilung nach P_el; fünf Teilfragen offen (E7‑Q2, R‑E7), Schemaschritt 103 · E7c |

Zeile 369 (EZ‑5, K‑1):

> | **EZ‑5** (K‑1) | Befund K‑1: „Der zweite Fall des § 2 Nr. 16 fehlt" — Anlagen mit Vorrichtung zur Abwärmeabfuhr | „Kennzeichen und Stromkennzahl je Anlage aufnehmen, Fall 2 rechnen." | 18.09.2026, nach Empfehlung (Statusdatei #332) | § 3.6; § 4 (K‑1); A2 | nicht gebaut — die Messung nach A2 ist mit E7a (#437) erfolgt (Wärmeüberschuss nur als Projektsumme, Aufteilung nach P_el); fünf Teilfragen offen (E7‑Q2, R‑E7), Schemaschritt 103 · E7c |

Zeile 352 (E7‑Q2, mit den fünf Teilantworten):

> | **E7‑Q2** | K‑1 (Schritt A): Gemessen nach A2 liegt die Wärmeproduktion je Modul vor, der Wärmeüberschuss nur als Projektsumme (in allen BHKW-Basisprojekten 0) — es greift die Aufteilung nach P_el. Fünf Teilfragen: (1) die ganze Nutzwärme nach P_el aufteilen oder nur den Überschuss (die Wärme bleibt je Modul)? (2) Kennzeichen gesetzt, σ leer: der Vorschlag P_el/P_th (Mockup „leer = 0,845") oder kein Zuschlag, weil das Feld der Wert ist? (3) `min(Netto, Nutzwärme × σ)` an Stelle von `stromNettoJeAnlage` ändert nur den Anteil — bei einer einzigen Anlage bliebe der Zuschlag gleich; Eigen- und Einspeisemenge proportional kürzen oder zuerst die Einspeisung? (4) Wie rechnet Fall 2 auf dem Ersatzweg, wenn sich Anlagen und Module nicht zuordnen lassen? (5) Ort im Dialog: Gruppe 1b (Analysepapier) oder die Überlagerung „Sätze und Herkunft" (Mockup)? | offen — **Empfehlung:** (1) nur den Überschuss nach P_el aufteilen, (2) der Vorschlag P_el/P_th, (3) zuerst die Einspeisung kürzen, (4) der Ersatzweg nach P_el, (5) die Überlagerung „Sätze und Herkunft" | 23.09.2026 gestellt | § 3.6 (Befund K‑1); EZ‑5; A2 | nicht gebaut — Schemaschritt **103** (101 trägt Nr. 30, 102 den Zapfprofilgenerator) · E7c |

**E7‑Q2 ist die einzige der drei R‑E7-Fragen, die am 23.09.2026 offen geblieben ist** (E7‑Q1 und
E7‑Q3 sind entschieden — siehe Punkt 2 und 3).

### Konzeptbezug

`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` § 3.6, Befund-Block Zeile 1769–1802
(Auszug, wörtlich):

> Zeile 1769–1776: „⚠ **Befund K-1 (neu): Der zweite Fall des § 2 Nr. 16 fehlt.** Verfügt eine Anlage **über eine Vorrichtung zur Abwärmeabfuhr** — beim Notkühler größerer BHKW der Regelfall —, ist KWK-Strom **nicht** die Nettostromerzeugung, sondern `Nutzwärme × Stromkennzahl`. […] EPOS-Plan führt **weder** ein Kennzeichen „Abwärmeabfuhr vorhanden" **noch** eine Stromkennzahl und rechnet immer den ersten Fall. Für Anlagen mit Notkühler fällt der Zuschlag damit **zu hoch** aus."
>
> Zeile 1778–1784: „**Entscheid K-1 (→ Register R‑EZ, EZ‑5): Kennzeichen und Stromkennzahl je Anlage aufnehmen, Fall 2 rechnen.** Neuer Boden seit BK1: Der Zuschlag gehört der Anlage (Schemaschritt 89, § 6.5) — die zwei Felder sind zwei weitere Anlagenspalten neben den neun `KWKG_*`-Spalten von `Tab_Energieanlagen`, kein Umbau; nächster freier Schemaschritt ist **103** (90 BK1a, 91 BK1b, 92 Vergleichsprojekt, 93 Vergütung je Variante, 94 Hilfsstrom-Bemessung, 95 KL-3 Klimaspalten, 96 FK-2 Projekt-Fremdschlüssel, 97–100 außerhalb dieses Feldes, 101 die leere Anlagenart (§ 6.3 Nr. 30), 102 der Zapfprofilgenerator, siehe Kopf; die Nummer wird bei der Umsetzung vergeben)."
>
> Zeile 1784–1790: „Das Kennzeichen `KWKG_Abwaermeabfuhr` (0/1, `CHECK`), die Stromkennzahl als nullbare Zahl mit **Vorschlag am Feld** aus P_el / P_th der Gerätezeile (`Tab_BHKW`, wo σ heute nur für die Katalogliste gerechnet wird) […] **Wo die Fallunterscheidung sitzt:** `WirtschaftlichkeitCtrl.ReiheJeAnlage` bildet je Anlage `stromNettoJeAnlage[i] = max(0, StromVon(Modul[i]) − Hilfsstrom[i])` mit `StromVon` = Klemmenerzeugung (`Stromproduktion`); bei gesetztem Kennzeichen tritt dort `min(Nettostromerzeugung, Nutzwärme × σ)` — die Nutzwärme je Modul aus Wärmeproduktion abzüglich Wärmeüberschuss."
>
> Zeile 1791–1801: „**Gemessen mit E7a (#437, A2):** Die Wärmeproduktion liegt je Modul vor, der Wärmeüberschuss nur als Projektsumme (in allen BHKW-Basisprojekten 0) — es greift die Aufteilung nach P_el; wie genau, fragen fünf Teilfragen (E7‑Q2, → Register R‑E7). […] **Referenzprojekte, gemessen an der Testdatenbank:** BHKW führen 1017, 1018, 1024 und 1030 (und das Nichtbasisprojekt 1031); KWKG-Sätze trägt allein 1030 (8,0 / 4,0 an beiden Anlagen), `Betriebsart` ist überall leer, `Wärmeüberschuss` in der Basis überall 0. […] **kein Basisprojekt ist betroffen, keine neue Basis**."

§ 4 Befunde, Zeile 2085: „| ⚠ **K-1** | **Der zweite Fall des § 2 Nr. 16 KWKG fehlt** (§ 3.6): […] |"

Anhang Kürzel/Etappen, Zeile 2442: „Mockup-Anhang **U1…U49** | … | Umsetzungsstand je Bildstelle; **U1 = Befund K-1**".

### Schemaschritt A (Analysepapier § 6)

`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`, Zeile 526 (Tafel):

> | **A** | 97 | K‑1: `KWKG_Abwaermeabfuhr` (0/1, CHECK), `KWKG_Stromkennzahl` (nullbar) | `Tab_Energieanlagen` | nein | nein | E7c — Nummer **103**, nach E7‑Q2 |

### Mockup, Anhang „Umsetzungsstand", U1 (`Dialog_Formel_Zahlenprobe.html`, Zeile 4569–4583)

> „offen · U1 · 5 · Kennzeichen „Vorrichtung zur Abwärmeabfuhr" (0/1 mit CHECK) und Stromkennzahl σ (nullbar) je Anlage, Fallunterscheidung in der Mengenbildung je Anlage, **ein Schemaschritt** (Nummer bei der Umsetzung: 90–102 sind vergeben — 101 trägt die leere Anlagenart, 102 den Zapfprofilgenerator —, der nächste freie ist **103**). **Vorbedingung:** Die modulscharfe Nutzwärme (Wärmeproduktion abzüglich Wärmeüberschuss) liegt im Ergebnismodell je Modul nicht vor — ohne sie ist Fall 2 nicht rechenbar. Gemessen mit E7a (#437): […] nach A2 greift die Aufteilung nach P_el, fünf Teilfragen dazu sind beim Anwender (E7‑Q2). / Ort im Programm: `WirtschaftlichkeitCtrl.ReiheJeAnlage` · `ErgebnisBHKWModulModel` · repoweit 0 Treffer „Abwärmeabfuhr" / Abnahme: Prüffall: Anlage mit Kennzeichen und σ rechnet `min(Netto, Nutzwärme × σ)`; ohne Kennzeichen unverändert. Kein Referenzprojekt betroffen — keine neue Basis."

Die Überlagerung „Sätze und Herkunft" (Teilfrage 5) ist Mockup-Punkt **U22** (Zeile 4615–4630,
noch offen): sie soll acht Größen (u. a. „KWK-Strom-Fall mit σ") mit Vorschlag/Herkunft/eigenem
Wert zeigen, mit Knopf „Übernehmen"; Codestellen dort genannt: `BhkwWirtschaftlichkeitDialog`,
`KwkgSatzRechner`, `KwkgKontingentRechner`, `SteuerGutschriftRechner`.

### Codestellen (heutiger KWKG-Rechenweg)

- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/KwkgSatzRechner.cs:61` — Klasse `KwkgSatzRechner` (marginale KWKG-Satzstaffel)
- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/KwkgKontingentRechner.cs:62` — Klasse `KwkgKontingentRechner` (Kontingentableitung § 8)
- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:3005` — `ReiheJeAnlage(...)`, bildet die KWKG-Jahresreihe je Anlage
- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:3023-3030` — `stromNettoJeAnlage[i] = Math.Max(0, StromVon(auswahl.Module[i]) - hs)`, heutige Fall-1-Rechnung (Nettostromerzeugung), ohne Fallunterscheidung Abwärmeabfuhr
- `EPOS.Kern/Model/ErgebnisModel.cs:143` — `Waermeueberschuss` nur als Feld von `ErgebnisBHKWModel` (Projektsumme), **nicht** im Modul-Modell
- `EPOS.Kern/Model/ErgebnisModel.cs:187-190` — `ErgebnisBHKWModulModel.Waermeproduktion` — Nutzwärme je Modul liegt hier vor, bestätigt die Registeraussage „liegt je Modul vor"
- `EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitDialog.razor` — der Dialog, in dem Gruppe 1b (Teilfrage 5, Lesart „Analysepapier") läge
- Repoweit kein Treffer für „Abwärmeabfuhr" oder `KWKG_Abwaermeabfuhr` außerhalb der Dokumentation (bestätigt: nicht gebaut)

---

## 2. A20 Förderende (KWKG_REALISIERUNG_JAHRE, Katalogdatum 2030)

### Registerzeilen (wörtlich)

`Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md`, Zeile 82 (A20):

> | **A20** | Katalogpflege ohne Leser: `KWKG_MINDESTALTER_*` (Mindestabstand § 8 Abs. 2) lesen, Förderende 2030 säen, EU‑ETS‑2-Schlüssel anlegen? | Förderende ja (R‑U5), Mindestabstand nur mit Inbetriebnahmedatum der Altanlage, ETS 2 mit dem Preispfad | 20.09.2026, nach Empfehlung | Grundlagen KWKG/Energiesteuer/Stromsteuer; § 3.11, § 5 (R‑U5) | entschieden (Förderende ja; Mindestabstand nur mit Inbetriebnahmedatum, ETS 2 mit dem Preispfad), nicht gebaut; Lesart des Förderendes entschieden 23.09.2026 (E7‑Q3, Lesart b, R‑E7) · E7c |

Zeile 353 (E7‑Q3):

> | **E7‑Q3** | A20, Förderende 2030: kein Zuschlag in Kalenderjahren nach 2030 (Lesart a — Analysepapier und Auftrag; sie widerspricht den Grundlagen, „Eine zeitliche Höchstdauer in Jahren gibt es nicht", und Rechenweg 05, dessen Beispielreihe bis 2037 zahlt), oder 2030 als Katalogdatum für das Ende der Frist zur Inbetriebnahme an Stelle der festen `KWKG_REALISIERUNG_JAHRE = 4` (Lesart b — Grundlagen)? | **entschieden 23.09.2026, nach Empfehlung: Lesart b** — das Katalogdatum 2030 ist das Ende der Frist zur Inbetriebnahme (eine Anlage mit Inbetriebnahme nach 2030 bekommt keinen Zuschlag) an Stelle der festen `KWKG_REALISIERUNG_JAHRE = 4`; die Zuschlagsreihe läuft danach bis zum Ende des Kontingents weiter (keine Höchstdauer in Jahren); Bau in E7c | 23.09.2026 gestellt; **entschieden 23.09.2026, nach Empfehlung** | R‑A (A20); Grundlagen KWKG; § 5 (R‑U5) | nicht gebaut, Lesart b entschieden · E7c |

### Konzeptbezug

`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` § 3.6, Zeile 1706–1711:

> „Deckelstaffel 5.000 (2021) … 3.300 (2026) … 2.500 (ab 2030). Vorgeschaltete Prüfkette: Stichtag ≤ 31.12.2026 · Realisierungsfrist 4 Jahre · Ausschreibung > 500 kW · Heizöl-Neuanlage ab 2025. Die Realisierungsfrist ist eine Konstante (`KWKG_REALISIERUNG_JAHRE = 4`); das Förderende 2030 (A20, R‑U5) ersetzt sie als Katalogdatum für das Ende der Frist zur Inbetriebnahme — der Zuschlag läuft danach bis zum Ende des Kontingents weiter, keine Höchstdauer in Kalenderjahren (entschieden E7‑Q3, Lesart b, 23.09.2026, → Register R‑E7 — Bau E7c)."

Rechenweg `Rechenweg/05_Verguetungen_BHKW.md`, Befundtabelle Zeile 311:

> „| A20 | Förderende 2030 (R‑U5) nicht gebaut: Die Prüfkette führt die Realisierungsfrist als Konstante (4 Jahre), die Jahresreihe oben zahlt bis 2037 | **entschieden 23.09.2026** (E7‑Q3, Lesart b): 2030 = Ende der Inbetriebnahmefrist statt fester vier Jahre, Bau E7c |"

Dieselbe Datei, Jahresreihen-Tafel Zeile 278–280 belegt das Beispiel: Zuschlag läuft bis Jahr 12
(2037), Kalenderjahre 2030–2036 zahlen mit Deckel 2.500 h weiter, Rest wird erst 2037
ausgeschöpft — genau der Beleg, den E7‑Q3 gegen Lesart a anführt.

### Katalog/Code (`grep -rn "REALISIERUNG_JAHRE\|2030"` in EPOS.Kern, gezielt)

- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:207` — `public const int KWKG_REALISIERUNG_JAHRE = 4;`
- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:2610-2611` — `fristende = new DateTime(p.KwkgStichtag.Value.Year + KWKG_REALISIERUNG_JAHRE, 12, 31)`, heutige starre Fristprüfung
- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:4581` — zweite Stelle mit derselben Konstante
- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:4176` — Deckelstaffel-Dictionary `{ 2030, 2500 }` (Jahresdeckel, nicht das Förderende selbst)
- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/GesetzKatalog.cs:1180` — `GESETZ_KWKG_VBH_JAHRESDECKEL` sät den 2500-Wert ab 2030 (Deckelstaffel, nicht die Realisierungsfrist)
- Kein Katalogschlüssel/Kein Code für ein „Förderende"/„Inbetriebnahmefrist 2030" gefunden — bestätigt „nicht gebaut"

---

## 3. Nr. 30 — Kern-Regel (E7‑Q1, Lesart b)

### Registerzeilen (wörtlich)

`Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md`, Zeile 272 (Nr. 30):

> | **Nr. 30** | **Sieben Energieanlagen tragen `KWKG_Anlagenart = ''`** (leere Zeichenkette statt NULL oder eines Steuerwerts) | „ein DML-Schritt setzt die leere Zeichenkette auf NULL; NULL heißt „nicht gepflegt" — der Kern bucht dann keinen KWKG-Zuschlag und meldet es als Kohärenzzeile „Anlagenart fehlt", der Dialog zeigt „bitte wählen"" | 22.09.2026, Anwender, nach Empfehlung | § 6.3 Nr. 30; § 2.2, Gruppe 1 | **teilweise gebaut #437** — Schemaschritt **101** setzt die sieben leeren Zeichenketten auf NULL (Projekte 1032 und 1043, kein BHKW), der Dialog zeigt „(bitte wählen)"; Kern-Regel und Kohärenzzeile entschieden 23.09.2026 (E7‑Q1, Lesart b), Bau E7c |

Zeile 351 (E7‑Q1):

> | **E7‑Q1** | Nr. 30, Kern-Regel und Kohärenzzeile „Anlagenart fehlt": Heißt „NULL ⇒ kein KWKG-Zuschlag", dass jedes BHKW ohne Anlagenart den Zuschlag verliert (Lesart a, wörtlich — trifft jedes BHKW der Testdatenbank, auch 1030 mit gepflegtem Kontingent 30.000 h und Sätzen 8/4 ct: KWKG-Erlös Jahr 1 7.315,96 € → 0, Kapitalwert −21.895.377,28 → −21.954.815,75 € (−59.438,48 €), der Anker 1030 bewegt sich), oder gilt sie nur, wo die Anlagenart gebraucht wird, also das Kontingent nach § 8 abzuleiten ist (Lesart b — so rechnet der Kern schon: 0 mit Grund; neu käme allein die Kohärenzzeile)? | **entschieden 23.09.2026, nach Empfehlung: Lesart b** — ohne Anlagenart entfällt der KWKG-Zuschlag nur dort, wo das Kontingent aus der Anlagenart abgeleitet wird; ein gepflegtes Kontingent (wie bei 1030, 30.000 h) bleibt wirksam, die Kohärenzzeile „Anlagenart fehlt" erscheint nur in diesem Fall; die Anlagenart des BHKW von 1030 wird in der Testdatenbank gepflegt; Bau in E7c | 23.09.2026 gestellt; **entschieden 23.09.2026, nach Empfehlung** | § 6.3 Nr. 30; § 2.2, Gruppe 1; R‑NR (Nr. 30) | Schritt 101 und „(bitte wählen)" gebaut #437; Kern-Regel und Kohärenzzeile entschieden (Lesart b) · E7c |

### Konzeptbezug

`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` § 6.3, Zeile 2324–2333 (Nr. 30, wörtlich):

> „30. **Sieben Energieanlagen trugen `KWKG_Anlagenart = ''`** — **zum Teil erledigt mit E7a (#437), siehe Protokoll:** Schemaschritt 101 setzt die leere Zeichenkette auf NULL (NULL heißt „nicht gepflegt"; die sieben Anlagen der Projekte 1032 und 1043 sind kein BHKW), der Dialog zeigt „(bitte wählen)". Ein geratener Wert würde Kontingent und Satzstaffel setzen, die niemand eingegeben hat. **Entschieden** (E7‑Q1, Lesart b, 23.09.2026, → Register R‑E7): Die Kern-Regel „NULL ⇒ kein KWKG-Zuschlag" und die Kohärenzzeile „Anlagenart fehlt" greifen nur dort, wo das Kontingent nach § 8 abzuleiten ist — nicht wörtlich bei jedem BHKW ohne Anlagenart; das BHKW von 1030 behält mit gepflegtem Kontingent (30.000 h) seinen Zuschlag, seine Anlagenart wird in der Testdatenbank gepflegt. Die Live-Datenbank ist vor dem Ausrollen zu prüfen (der Schritt trifft jede leere Zeichenkette). Entscheid: → Register R‑NR. Bau in E7c."

### Codestellen (Kontingent aus Anlagenart)

- `EPOS.Kern/Allgemein/Update/KwkgAnlagenartLeer.cs:8-30` — Kopfkommentar „DIE LEERE ANLAGENART WIRD NULL – Migrationsschritt 101"; DML-Schritt, ergebnisneutral, betrifft sieben Zeilen der Projekte 1032/1043 (kein BHKW)
- `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs` — führt `SCHRITT_101_KWKG_ANLAGENART_LEER` als Schemaschritt der Windows-Schale (Doppelmechanismus, siehe A10)
- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/KwkgKontingentRechner.cs:73-80` — `Ableiten(string anlagenart, …)`: `if (katalog == null || string.IsNullOrEmpty(anlagenart)) { v.Unvollstaendig = true; v.Herleitung = …WIRT_KWKG_KONTINGENT_OHNE_ART; return v; }` — genau die heutige „0 mit Grund"-Regel, die Lesart b bestätigt
- `EPOS.UI/Dialoge/Wirtschaftlichkeit/BhkwWirtschaftlichkeitDaten.cs:84` — `BhwTexte.T("BHW_W_ART_LEER", "(bitte wählen)")`, der mit #437 gebaute Dialogtext
- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/KohaerenzPruefung.cs:33-47` — Muster einer Kohärenzzeile: Klasse `KohaerenzHinweis` (`Schwere`, `Text`, `Betrag`) und die drei Schweregrade `KohaerenzSchwere.WARNUNG/HINWEIS/BESTAETIGUNG` (Zeile 14-27) — die neue Zeile „Anlagenart fehlt" (E7‑Q1) ist danach zu bauen, existiert im Code noch nicht (kein Treffer für „Anlagenart fehlt")

### BHKW von Projekt 1030 in der Testdatenbank

Ohne dediziertes SQL-Werkzeug im Auftrag verfügbar; gelesen über eine reine Lesezugriffs-Abfrage
(`sqlite3`-Modul von `py`, `mode=ro`, nur `SELECT`) direkt gegen
`Referenzlaeufe/Kenndaten_Test.sqlite`, Tabelle `Tab_Energieanlagen`:

| ID (Anlage) | ID_Projekt | Bezeichner | ID_BHKW | KWKG_Anlagenart | KWKG_Satz_Einspeisung | KWKG_Satz_Eigen | KWKG_Vbh_Kontingent | KWKG_Vbh_Jahresdeckel |
|---|---|---|---|---|---|---|---|---|
| 14920 | 1030 | BHKW EW M 50 S [K] Erdgas | 1018148 | `NULL` | 8,0 | 4,0 | 30000,0 | `NULL` |
| 14921 | 1030 | EC-POWER XRGI 9 | 1018152 | `NULL` | 8,0 | 4,0 | 30000,0 | `NULL` |

Das bestätigt den Registerbefund: Die Anlagenart des BHKW von 1030 ist **heute noch NULL** (die im
Register angekündigte Pflege „die Anlagenart des BHKW von 1030 wird in der Testdatenbank
gepflegt" ist noch **nicht** ausgeführt), während das Kontingent bereits gepflegt ist
(30.000 h an beiden Anlagen) — deshalb bleibt der KWKG-Zuschlag nach Lesart b heute schon
wirksam, ganz ohne Anlagenart.

---

## 4. Schritt E, Schritt F, Schritt G (und Schritt A zum Vergleich)

### Wörtliche Tafelzeilen aus dem Analysepapier § 6 (Zeile 523–535)

> | Schritt | vormals | Inhalt | Tabelle | Doppelpflicht `SpalteSicher` | Einfrierregel | Etappe |
> |---|---|---|---|---|---|---|
> | **101** | — | Nr. 30: leere `KWKG_Anlagenart` → NULL (`SCHRITT_101_KWKG_ANLAGENART_LEER`) — **reines DML**, sieben Zellen der Testdatenbank, kein BHKW | `Tab_Energieanlagen` | nein | nein | E7 Teil a, **gebaut #437** |
> | **A** | 97 | K‑1: `KWKG_Abwaermeabfuhr` (0/1, CHECK), `KWKG_Stromkennzahl` (nullbar) | `Tab_Energieanlagen` | nein | nein | E7c — Nummer **103**, nach E7‑Q2 |
> | **B** | 98 | Szenariorahmen: `Szen_Best/Worst_Zeitraum`, `Szen_Best/Worst_Menge` (nicht `_Dauer`) | `Tab_ProjektWirtschaftlichkeit` | **ja** | nein | E9 |
> | **C** | 99 | Trägerpreise best/worst: `custom_price_work/base/power_best/_worst` | `energy_project_settings` | nein | nein | E9 |
> | **D** | 100 | Erlössätze best/worst: `Einspeiseverguetung(_KWK)_Best/_Worst`; `DvEntgelt_Best/_Worst`, `PpaPreis_Best/_Worst` | `Tab_ProjektWirtschaftlichkeit`, `Tab_ProjektPhotovoltaik` | **ja** (PPV) | nein | E9 |
> | **E** | 101 | `ErsatzFuehren`, `RestwertAnsetzen` (nullbar, CHECK; NULL = wie bisher) | `Tab_ProjektWerte`, `Tab_KostenVorlagePosition` | nein | nein | E7 |
> | **F** | 102 | `Preisbasis` (TEXT, nullbar) mit einmaligem DML aus `ID_Umrechnung` | `energy_project_settings` | nein | nein | E7 |
> | **G** | 103 | U‑1: `Einheit`/`PreisEinheit` der fünf Gase auf `Nm³`, eine `energy_price`-Zeile — **reines DML**, vor dem Vorlagenbau | `Tab_Brennstoff_Stamm`, `energy_price` | nein | nein (Einfrierliste nennt nur CO₂/SO₂/NOx/Staub) | E7 |
> | **(H)** | (104) | optional: geräteeigene Nutzungsdauer entfernen | `Tab_BHKW`, `Tab_Heizkessel` | nein | nein | E10, nach A8 |

Fußnote Zeile 537–539: „Die Spalte „vormals" nennt die Nummer aus der Fassung vom 19.09.2026 […]
**Der Mockup-Anhang nennt bei U1 und U32 denselben Schritt A** (dort als „Schemaschritt 97"
geführt; mit E0c auf „Nummer bei der Umsetzung" geändert)."

### Schritt E — Ersatz/Restwert-Kennzeichen

Registerzeile A6 (Zeile 68):

> | **A6** | Ersatz/Restwert-Kennzeichen je Position (Schritt **E**, § 6) oder je Technik? | **Position**, nullbar, NULL = wie bisher | 20.09.2026, nach Empfehlung | § 2.13 (3), U39 | entschieden (Kennzeichen je **Position**, nullbar), nicht gebaut · E7 |

Mockup U39 (`Dialog_Formel_Zahlenprobe.html`, Zeile 4657–4669):

> „offen · U39 · 1 · 8 · Der Rest von U8: Entkopplung von Ersatz und Restwert (ein Kennzeichen je Position oder Technik — oft ist das eine ohne das andere gewollt), die ungelesenen geräteeigenen Dauerspalten (`Tab_BHKW`, `Tab_Heizkessel`, `Tab_StromspeicherVariante`), der Anschluss der Speicherflotte mit ihrem handgepflegten `ErsatzintervallJahre` und `RestwertEuro`. **Hinweiszeile erledigt mit E5 (#434):** Zeitraumzeile und „k von n Positionen ohne Nutzungsdauer" kommen aus dem Kern (`NutzungsdauerHinweisCtrl`) über die plattformfreie Hülle in `EPOS.UI.Daten` und stehen auf der Seite und in Wort- und Tabellenbericht. / Ort im Programm: `WirtschaftlichkeitSeiteGaben` · `NutzungsdauerCtrl` · Konzept § 2.13 (3) · Konzept Nutzungsdauer Stufe S3."

Konzept § 6.3, Nr. 9h (Zeile 2278–2282): „Nutzungsdauer, Ersatz, Restwert — drei fehlende Stücke
(Mockup-Anhang U39): Entkopplung von Ersatz und Restwert, die ungelesenen geräteeigenen
Nutzungsdauer-Spalten, der Anschluss der Speicherflotte — offen (E7/E10)."

Codestellen: kein Treffer für `ErsatzFuehren`/`RestwertAnsetzen` in `EPOS.Kern`/`EPOS.UI`/
`EPOS.UI.Daten` — bestätigt „nicht gebaut". Verwandte, bereits gebaute Stelle:
`EPOS.Kern/Allgemein/Wirtschaftlichkeit/…` (`NutzungsdauerHinweisCtrl`, laut Mockup-Text) — nur
die Hinweiszeile, nicht das Kennzeichen selbst.

### Schritt F — Preisbasis-Spalte (U32)

Registerzeile ET‑D‑3 (Zeile 182):

> | **ET-D-3** | Was bietet die Preisbasis an? | **(a) genau zwei Einträge** — Abrechnungseinheit und kWh, Faktor = Heizwert — **umgesetzt**. Die Umrechnungsregeln werden zum zugeklappten **Prüfblock** „Einheiten und Umrechnung". **Offener Rest (U32):** Der Kartenzustand fällt weiterhin auf `ID_Umrechnung = -1` zurück | 18.09.2026, Anwender | § 2.5, § 3.5 | umgesetzt; offener Rest U32 (§ 5) |

Mockup U32 (Zeile 4646–4656):

> „offen · U32 · 4 · Die Preisbasis-Kennung des Projekts (`ID_Umrechnung`) ist eine **Regelkennung**. Führt kein Brennstoff eine Regel nach kWh, wird beim Speichern −1 abgelegt, und die gewählte Preisbasis fällt beim nächsten Öffnen auf die Abrechnungseinheit zurück. […] Saubere Lösung: eine eigene kleine Spalte für den Kartenzustand, also ein **eigener Schemaschritt** (Nummer bei der Umsetzung: 90–102 sind vergeben, der nächste freie ist **103**). Kennung im Text: UR-1 Schritt 2. / Ort im Programm: `EnergietraegerPreisCtrl.Preisbasen` · `energy_project_settings`. / Abnahme: Öffnen–Speichern–Öffnen hält die Preisbasis kWh, auch wenn der Brennstoff keine kWh-Regel führt."

Codestellen:

- `EPOS.Kern/Controller/EnergietraegerPreisCtrl.cs:190` — Methode `Preisbasen(string abrechnungseinheit, …)`
- `EPOS.Kern/Controller/EnergietraegerPreisCtrl.cs:116,125` — Rückfall `return -1` ohne passende kWh-Regel
- `EPOS.Kern/Controller/EnergietraegerPreisCtrl.cs:513,645` — `IdUmrechnung = -1` als Vorgabe/Vergleichswert des Kartenzustands

### Schritt G — U‑1 (Einheitenbruch Gase)

Registerzeile U‑1 (Zeile 185):

> | **U-1** | Einheitenbruch `Tab_Brennstoff_Stamm.Einheit` ↔ `energy_conversion` (BK3 § 6 Nr. 4): die Identitätsregel-Ableitung liefert für 9 von 25 Brennstoffen `-1` | **entschieden 30.08.2026 — Weg (a)**: Der Stammtext der fünf Gase (Brennstoffe 1, 2, 3, 14, 25) wird „m³" → „Nm³" gezogen (Muster Schritt 26a; Leitentscheidung L4 auf die Stammseite fortgeschrieben). Die Wege (b) Identitätsregel-Saat und (c) `billing_unit`-Ableitung sind **nicht beauftragt** | 30.08.2026, Weg (a) | § 5 (Einheitenbruch) | nicht gebaut; A9 gibt U‑1 frei (vor dem nächsten Vorlagenbau, E7); Schemaschritt bei der Umsetzung |

Registerzeile A9 (Zeile 71, der Freigabe-Entscheid):

> | **A9** | U‑1 Einheitenbruch (Gase `m³` → `Nm³`) als DML-Schritt **G** (§ 6) freigeben? | Ja, vor dem nächsten Vorlagenbau; die fünf Randfragen ins Register | 20.09.2026, nach Empfehlung | § 5 (U‑1); R‑D | entschieden (U‑1 freigeben, vor dem nächsten Vorlagenbau), nicht gebaut · E7 |

Für Schritt G gibt es **keine eigene U-Ziffer** im Mockup-Anhang „Umsetzungsstand" (kein Treffer
für „Nm³" oder „Einheitenbruch" in `Dialog_Formel_Zahlenprobe.html`) — die Kennung „U‑1" ist hier
ausschließlich die Registerkennung der Familie R‑D, keine Mockup-Anhang-Zeile (siehe
„Unklarheiten").

Codestellen: kein Treffer für `Nm³` oder eine Umbenennungsroutine der Gase in
`Tab_Brennstoff_Stamm` in `EPOS.Kern` — bestätigt „nicht gebaut" (Schritt G ist reines DML).

---

## 5. S‑2 (A3), V‑1/V‑2 (A4), B‑4 Rest, B‑6, Kapitalwert 1024

### S‑2 (A3) — Mischlage § 53/53a neben § 54

Registerzeile A3 (Zeile 65):

> | **A3** | S‑2: Mischlage § 53/53a neben § 54 sperren (§ 54-Betrag verwerfen) oder als Warnung hochstufen? | **Sperre** mit Begründungszeile — solange R‑U1 offen ist, ist die Kombination nie zulässig | 20.09.2026, nach Empfehlung, ausdrücklich bestätigt | § 4 (S‑2), § 7 (B8) | entschieden (**Sperre** mit Begründungszeile), nicht gebaut — Befund S‑2 · E7 |

Konzept § 4, Zeile 2082: „| ⚠ **S-2** | Kein projektweites Doppelentlastungsverbot — Anlage A nach
§ 53 und Anlage B nach § 54 gleichzeitig möglich. |"

Konzept § 7, Zeile 2398 (B8, wörtlich): „Die verbliebenen Befunde: **S-2** (kein projektweites
Doppelentlastungsverbot) und **B-6** (geschluckte Fehler, `catch {}` ⇒ still 0). […] S‑2 ist mit
**A3** entschieden (→ Register R‑A): **Sperre mit Begründungszeile**, nicht Warnung. Beide Punkte
laufen in **E7c** des Etappenplans mit."

Bereits gebaut, aber nur als Hinweis (nicht als Sperre): `KohaerenzPruefung.cs:880` —
Methode `MischlageEnergiesteuer(...)`, „Fall 5 — Mischlage § 53 / § 53a Abs. 5 neben § 54"
(Kopfkommentar Zeile 19: „seit FX5-b auch die Mischlage … (Fall 5)"), aufgerufen aus
`KohaerenzPruefung.cs:201`. Das ist heute ein `HINWEIS`, keine Sperre — die eigentliche A3-Sperre
(§ 54-Betrag verwerfen) ist noch nicht gebaut.

### V‑1/V‑2 (A4)

Registerzeile A4 (Zeile 66):

> | **A4** | V‑2: § 51a mit der Einspeisevergütung bewerten, wenn die Anlage feste Vergütung fährt? | Ja, nach Volltextprüfung; eine Zeile, eigener Testfall | 20.09.2026, nach Empfehlung, ausdrücklich bestätigt | § 3.6 (Photovoltaik / EEG), § 4 (V‑2) | entschieden (§ 51a mit dem anzulegenden Wert, **eigener Testfall**), nicht gebaut; der heutige Weg ist mit `PvErloesRechnerEegTests` **gepinnt** (#380) · E7 |

Konzept § 4, Zeile 2087: „| V-1 · V-2 · V-4 | EV-Rundung (EvMix unrundet, Erlös gerundet) ·
§ 51a bewertet mit AW statt EV · Eigen/Einspeise-Split je Anlage ist benannte Näherung. |"

Zusatzbeleg aus `Status_iOS_Migration.md`, Statuszeile Nach #437 (Zeile 532, Abschnitt g,
wörtlich): „**E7c** nach den Entscheiden E7‑Q1 bis E7‑Q3 — K‑1 (Schritt A = 103), Nr. 30
Kern-Regel und Kohärenzzeile, A20, S‑2 (A3), V‑2/V‑1 (A4), die Schritte E, F, G, B‑4 Rest, B‑6,
Kapitalwert 1024 (−676.036,81 €)." — bestätigt die Zuordnung „V‑1 und V‑2 gehören zu A4" aus dem
Auftrag.

Codestellen: `PvErloesRechnerEegTests` als heutige Pinnung (Anker); kein Treffer für eine
V‑1-EV-Rundungskorrektur oder eine V‑2-AW/EV-Umstellung in `EPOS.Kern/Allgemein/Wirtschaftlichkeit/`
— beide Punkte sind unverändert (nicht gebaut).

### B‑4 Rest

Konzept § 4, Zeile 2065: „| B-4 | Zwei Arten nie frisch: `PROZENT_BRENNSTOFFKOSTEN`,
`PROZENT_STROMKOSTEN` — `EUR_PRO_H` und die beiden `EUR_PRO_KWH_*` sind seit FX2 frisch. |"

Codestelle: `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:6629` —
`IstEndenergieArt(string bem)`, die Methode, die bestimmt, was „frisch" aus dem jüngsten Lauf
bezogen wird; `PROZENT_BRENNSTOFFKOSTEN`/`PROZENT_STROMKOSTEN` stehen **nicht** darin, sondern
separat in `WirtschaftlichkeitCtrl.cs:6651-6655` (`IstEnergiepreisArt`) — bestätigt B‑4, dass
diese zwei Arten weiterhin anders (nicht frisch) behandelt werden.

### B‑6

Konzept § 4, Zeile 2067: „| B-6 | Fehler werden geschluckt (`catch {}` ⇒ still 0). |"

Codestellen (Auswahl, sehr verbreitetes Muster im Feld Wirtschaftlichkeit — Belegzeilen, keine
Vollzählung):

- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:303,337,350,371,386,751,951,…` — zahlreiche `catch { }` ohne Meldung, über die ganze Datei verteilt
- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/GesetzKatalog.cs:823,897,922` — `catch { }` bei der Katalogauflösung
- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/Emissionsquelle.cs:317,328,384,424,463` — `catch { }` in der Emissionsfaktor-Lesekette
- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/EmissionsBilanzRechner.cs:90,130,167,357,397` — `catch { }` in der Bilanzrechnung
- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/KohaerenzPruefung.cs:176-210` — jede der acht
  Kohärenz-Teilprüfungen (`HilfsenergieDoppelpflege`, `PvVerguetungHerkunft`,
  `Co2DoppelansatzBehg`, `StrommixRueckfall`, `Brennstoffseite`, `MischlageEnergiesteuer`,
  `Stromseite`, `ErlaubnisschwelleStrom`, `DoppelzaehlungBefreiung`) läuft in einem eigenen
  `try { … } catch { }` — ein Fehler in einer Teilprüfung bleibt unbemerkt

Konzept § 7 nennt B‑6 ausdrücklich als „Robustheit" (Zeile 2398): „**ja** bei S-2 — jeder Punkt
einzeln mit A/B-Nachweis; B-6 ist Robustheit" — also ohne eigenen A/B-Rechennachweis erwartet.

### Kapitalwert 1024

Konzept § 6.2, Zeile 2216–2217 (Anker-Tafel, wörtlich):

> „| `LiesBetriebskosten(1024)` | **99,00 €/a** | gemessen = Konzept (#380) |
> | Kapitalwert 1024 | **−2.896.359,13 €** | gemessen (#380) — das Konzept führte **−2.220.322,32 €** |"

Zeile 2226–2229: „Der Kapitalwert 1024 liegt mit −2.896.359,13 € um **−676.036,81 €** unter dem
Konzeptwert; die Abweichung ist eingegrenzt, aber nicht nachgerechnet (Kandidaten:
Kesselbrennstoff B‑1/#331, Hilfsstrom #365/#366, Schemaschritte 93–96) und gehört zu **E7**. Bis
dahin gilt der **gemessene** Wert als Anker."

Bestätigung, dass Kapitalwert 1024 zu E7c gehört: `Status_iOS_Migration.md`, Zeile 532 (Abschnitt
g, wörtlich, s. o.): „…, die Schritte E, F, G, B‑4 Rest, B‑6, **Kapitalwert 1024
(−676.036,81 €)**."

Der Anker `Kapitalwert 1024` selbst ist heute **gemessen**, nicht nachgerechnet — E7c soll die
Ursache der Konzept-Abweichung klären (kein offener Programmierpunkt im engeren Sinn, sondern ein
Klärungspunkt gegen die eigene Konzepttafel).

---

## 6. § 6.3 offene Punkte (Nummer + Kurztitel) und § 6-Schrittvergabe

### Konzept § 6.3 — noch offene Punkte (nicht durchgestrichen, Stand des Worktrees)

- **9h** — Nutzungsdauer, Ersatz, Restwert (drei fehlende Stücke, Mockup-Anhang U39)
- **10** — Bezugsgrößen der übrigen KD1-Bemessungsarten (H1-1b)
- **11** — Nachzieh-Migration für Bestandsprojekte (durch Auto-Anlage entschärft, bleibt Option)
- **13** — Pufferkapazität bleibt null (bewusste Grenze)
- **15** — Bilanzjahr und Unternehmensart wirken erst beim nächsten Dialog-Öffnen
- **16** — Rückweg „Parameterdialog zeigt den erfassten Preisanteil" fehlt
- **18** — Engine-Sortierung `ORDER BY Prioritaet` (HB1-O1) — vermutlich überholt, nachzumessen
- **19** — Asymmetrie „Wartung BHKW" gegen „Vollwartung / Wartung Kessel"
- **22** — Sichtabnahmen: Brennstoffblock (B2), Kosten-Seite (BK1), Stromsteuer-Hervorhebung (B4)
- **23** — resx-Sammelnachtrag der Textschlüssel aus B3a, B3b, B4 und der F-Serie
- **24** — Datenpflege: Projekt 1018 Kessel ohne Energieträger, Puffer ohne Temperaturpaar; WP-Kennlinie 1024 ohne HT-Stützstellen
- **30** — Sieben Energieanlagen trugen `KWKG_Anlagenart = ''` (Schemaschritt 101 gebaut, Kern-Regel/Kohärenzzeile noch offen — Bau E7c, siehe Punkt 3)

Punkte mit Rest trotz „erledigt"-Kennzeichnung (nicht in der Hauptliste, aber mit offenem
Nachsatz): **14** (keine Wache Konstante-gegen-Katalog), **17** (Fall 4 ohne Katalogsatz bleibt
still), **21** (Betriebskosten von 1030 ohne Anker).

### Analysepapier § 6 — Schrittvergabe (Zeile 505–514, 523–535, wörtlich geprüft)

Der Auftrag vermutete „101 Nr. 30, 102 Zapfprofilgenerator, 103 E7b, 104 K‑1" — das stimmt **nicht
genau** mit dem Papier überein. Wörtlich (Zeile 511–514):

> „**101** trägt E7 Teil a — die leere `KWKG_Anlagenart` wird NULL (Konzept § 6.3 Nr. 30, #437), nicht K‑1 —, **102** führt die Sitzung des Zapfprofilgenerators für ihre Tabellen (Zweig `z0`, noch nicht zusammengeführt). `SchemaStand.Zielversion` steht auf **101**, der **nächste freie Schritt ist 103** — ihn bekommt K‑1 (Schritt **A**)."

Richtig ist also: **101 = Nr. 30** (gebaut #437), **102 = Zapfprofilgenerator** (Zweig `z0`, noch
nicht zusammengeführt), **103 = K‑1** (Schritt A, noch zu vergeben — „die Nummer wird bei der
Umsetzung vergeben"). Für **E7b** (Q11, Zeitzonentarif/Leistungspreis-Staffel) nennt die
Schemaschritt-Tafel **keinen eigenen Schemaschritt** — E7b steht in § 5 als eigene Etappe, aber
nicht in der A–H-Schritttafel des § 6. Eine Schrittnummer „104" für K‑1 gibt es in den Quellen
nicht; K‑1 hat die Nummer 103.

Die übrigen Buchstaben-Schritte (Nummer erst bei der Umsetzung vergeben, aus dem dann freien
Bereich ab 103, siehe Fußnote Zeile 516–518): **B** Szenariorahmen (E9), **C** Trägerpreise (E9),
**D** Erlössätze (E9), **E** Ersatz/Restwert (E7), **F** Preisbasis (E7), **G** U‑1 (E7),
**(H)** optional geräteeigene Nutzungsdauer (E10).

`Status_iOS_Migration.md` Zeile 532 (Abschnitt d, Befunde, wörtlich) bestätigt dieselbe Vergabe
noch einmal ausdrücklich: „**Schrittvergabe 101/102** — die Zapfprofilgenerator-Sitzung (Zweig
`z0`) hatte 101 ebenfalls vergeben und führt ihren Schritt nach dem Merge von e7 als 102; K‑1
bekommt den nächsten freien, 103 (Konzept § 3.6 hatte 101 für K‑1 genannt, die Vergabe an Nr. 30
folgte dem Auftrag)".

---

## Unklarheiten

1. **V‑1/V‑2-Doppeldeutigkeit.** Es gibt zwei unterschiedliche Bezeichnungssysteme mit „V‑1"/
   „V‑2": (a) die Registerfamilie **R‑V** (Zeile 137–138: V‑1 = Aufklappabschnitte/Umschalter der
   ValERI-Ansicht — bereits **gebaut #434**; V‑2 = XLSX-Formelexport-Umfang, **überholt durch
   V‑G10**), und (b) die **§ 4-Befunde** des Konzepts (Zeile 2087: V‑1 = EV-Rundung, V‑2 = § 51a
   AW/EV) — auf Letztere bezieht sich A4 und damit E7c. Ich habe im Faktenblatt konsequent die
   § 4-Befunde (b) verwendet, weil A4 und die Statuszeile Nach #437 sie so referenzieren; die
   R‑V-Familie (a) ist eine andere Sache und in E7c nicht gemeint. Bitte gegenprüfen, ob das der
   gewünschten Lesart entspricht.
2. **Schritt G / „U‑1" ohne Mockup-Anhang-Zeile.** Der Auftrag nennt „Schritt G U‑1" so, als gäbe
   es eine Mockup-U-Ziffer dazu; im Anhang „Umsetzungsstand" von `Dialog_Formel_Zahlenprobe.html`
   gibt es aber keine eigene U-Zeile für den Einheitenbruch der Gase (kein Treffer für „Nm³" oder
   „Einheitenbruch"). „U‑1" ist hier ausschließlich die Registerkennung der Familie R‑D (Zeile
   185), keine Mockup-Anhang-Zeile wie U1, U32, U39 usw. — beide „U‑1"/„U1" sehen im Text ähnlich
   aus, sind aber unterschiedliche Nummernkreise (Registerkennungen vs. Mockup-Anhang-Ziffern).
3. **Schemaschritt „104"/„E7b" aus dem Auftrag.** Wie unter Punkt 6 dargelegt, nennt keine der
   gelesenen Quellen einen Schemaschritt 104 oder eine Zuordnung „103 = E7b". Falls es dazu eine
   andere, mir nicht vorliegende Quelle gibt, bitte prüfen — ich habe nur den Stand des Worktrees
   auswerten können.
4. **Projekt 1030 / KWKG_Anlagenart in der Testdatenbank.** Kein dediziertes SQL-Werkzeug war Teil
   des Auftrags; ich habe ersatzweise eine reine Lese-Abfrage (Python `sqlite3`-Modul, `mode=ro`,
   nur `SELECT`) direkt gegen `Referenzlaeufe/Kenndaten_Test.sqlite` ausgeführt, weil weder
   `sqlite3`-CLI noch `python3` im PATH verfügbar waren (nur `py`). Das Ergebnis steht unter
   Punkt 3; falls dieser Weg nicht als „lesend" im Sinne des Auftrags gelten soll, bitte die dort
   genannten Werte (Anlagen-ID 14920/14921, `KWKG_Anlagenart` = NULL, Kontingent 30.000 h) separat
   verifizieren.
5. **Live-Datenbank vs. Testdatenbank bei Nr. 30.** Das Konzept warnt ausdrücklich: „Die
   Live-Datenbank ist vor dem Ausrollen zu prüfen (der Schritt trifft jede leere Zeichenkette)."
   Diese Prüfung war nicht Teil meines Auftrags (nur Worktree, kein Zugriff auf eine Live-DB) und
   ist hier nicht enthalten.
6. **Kern-Regel/Kohärenzzeile „Anlagenart fehlt" (E7‑Q1) und Kennzeichen/Stromkennzahl (K‑1,
   E7‑Q2) sind beide noch ungebaut** — ich konnte daher nur die heutigen Bausteine (Muster
   `KohaerenzHinweis`, `KwkgKontingentRechner.Ableiten`, `ReiheJeAnlage`) referenzieren, keine
   Stelle „zeigen", die die künftige Logik schon enthält.
7. **B‑6-Codestellen sind eine Auswahl, keine Vollzählung.** `catch { }` kommt allein in
   `WirtschaftlichkeitCtrl.cs` weit über 30-mal vor; ich habe nur repräsentative Zeilen genannt,
   wie im Auftrag „nur Datei:Zeile und ein Halbsatz" verlangt war.
