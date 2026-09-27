# Faktenblatt E10 — Nutzungsdauer S3 und Speicherflotte

Quellenstand: Worktree `C:\Waermeplan\EPOS-Plan\.claude\worktrees\e9b`, Zweig `e9b`, Commit
`018520f833e7a7c50891be14d6fa5593768ea717` (24.09.2026 05:35:43 +0200) — das ist der Zweig der
laufenden Etappe **E9b** (V‑E Teil b), nicht E10 selbst; E10 ist laut allen gelesenen Papieren die
**übernächste**, noch nicht begonnene Etappe (siehe Abschnitt „Statusdatei" und „Unklarheiten" Nr. 1).
Nur gelesen, nichts geändert, kein Build, kein Test, kein Worktree angelegt, kein Zweig gewechselt.
Alle Pfade repo-relativ zur Repowurzel `C:\Waermeplan\EPOS-Plan`.

---

## E10 laut Analysepapier (§ 5, Zeile E10, wörtlich)

Quelle: `Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md:478`
(Tabellenkopf `:465`: Etappe | Inhalt | Größe | Rechenwirkung | Nachweis | Schema | Wiki | Modell |
Voraussetzung):

> **E10 Nutzungsdauer S3 und Speicherflotte** | Instandsetzung/Wartung je Technik aus den vorhandenen
> Spalten, Gerätekataloge; Speicherflotte an `Tab_Nutzungsdauer` mit **Neueinfrieren der Basis**;
> geräteeigene Spalten kennzeichnen (A8) | M + M | **ja** | A/B, neue Referenzbasis mit Begründung in
> `Referenzlaeufe/LIESMICH.md` | (H optional) | Kosten `nutzungsdauern` | Opus | ND‑S3-Entscheid, A7

Die Nachbarzeilen ordnen E10 ein (`:477`, `:479`):

> **E9 V‑E Szenarioabdeckung** — **Teil a (E9a) umgesetzt #461**, Teil b (E9b) folgt | … | Opus |
> A5 entschieden; E5, E7 — … **E9b** die Dialoge (±-Knopf an drei Orten, Zeilen 8 und 9 der
> Szenariotafel), der Hinweistext entfällt, der Ausweis „n von m" (voraussichtlich #462)

> **E11 Zahlenprobe A8/B9** — **entfällt** (Anwenderentscheid 22.09.2026: „BHKW-Plan-Mappen: nicht
> relevant") | … Nachweis der Wirtschaftlichkeitsgrößen über die Anker aus E1 und die A/B-Nachweise
> von E7, E9, E10 | … | entfällt

E10 ist damit die einzige der drei Nachbarzeilen mit eigenem Rechenwirkungs-„ja" außer E9; E11 entfällt
vollständig, E12 (Wiki-Runden) folgt danach.

**Größenangabe „M + M":** zwei Halbtages- bis Eintagesstücke — vermutlich Instandsetzung/Wartung
einerseits, Speicherflotten-Anschluss samt Neueinfrieren andererseits (keine Quelle trennt das
ausdrücklich, siehe Unklarheiten).

---

## Die Entscheide A7 und A8 (Voraussetzung von E10)

Quelle: `2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md` § 4 „Entscheide, die die Umsetzung
braucht" (`:392`). Fragetabelle (Kopf `:404`: # | Frage | Empfehlung), Zeilen `:412–413`:

> | **A7** | Speicherflotte an `Tab_Nutzungsdauer` anschließen (Basis neu einfrieren) — jetzt oder mit
> ND‑S3? | Mit ND‑S3, als eigener Auftrag mit Neueinfrieren |
> | **A8** | Geräteeigene Nutzungsdauer-Spalten (`Tab_BHKW`, `Tab_Heizkessel`) abkündigen? | **Nicht
> jetzt** — kennzeichnen; die Speichervariante sollte die Positionsarten 20/21 lesen |

Standtabelle (Kopf `:429`: # | Umsetzungsstand | Etappe), Zeilen `:437–438`:

> | **A7** | entschieden (Speicherflotte **mit ND‑S3**, eigener Auftrag mit Neueinfrieren), nicht
> gebaut | E10 |
> | **A8** | entschieden (geräteeigene Spalten **nicht jetzt**, nur kennzeichnen), nicht ausgeführt |
> E10 |

Vor der Fragetabelle steht der geltende Entscheidrahmen (`:395–401`, Auszug):

> Alle zwanzig Entscheide sind gefallen: „entschieden 20.09.2026 nach Empfehlung" (Anwenderauftrag vom
> 20.09.2026 „fahre fort mit der Umsetzung der Wirtschaftlichkeitsberechnung nach Konzept wie im
> Mockup" mit dem Zusatz „Entscheidung nach Empfehlung"; Statuszeile **#405**). Die Empfehlungsspalte
> unten **ist damit der Entscheid** — sie bleibt im Wortlaut stehen.

A7/A8 zählen nicht zu den drei von A3/A4/A5 ausdrücklich noch einmal bestätigten Entscheiden — sie
gelten über die allgemeine „nach Empfehlung"-Regel als entschieden, aber **nicht gebaut**.

---

## § 6 Schemaschritte — (H) und die Speicherflotten-Zeile

Quelle: `2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md` § 6 „Schemaschritte dieser
Etappen" (`:548`), Tafelkopf `:583` (Schritt | vormals | Inhalt | Tabelle | Doppelpflicht
`SpalteSicher` | Einfrierregel | Etappe), Zeilen `:594–595`:

> | **(H)** | (104) | optional: geräteeigene Nutzungsdauer entfernen | `Tab_BHKW`, `Tab_Heizkessel` |
> nein | nein | E10, nach A8 |
> | — | — | Speicherflotte an `Tab_Nutzungsdauer` | JSON in `Tab_SpeicherAuslegung` | — | **ja**
> (Projekt 1046) | E10, eigener Auftrag |

Zwei getrennte Zeilen, nur eine trägt einen Buchstaben (H); die Speicherflotten-Zeile hat **keine**
Schemaschritt-Kennung, weil sie keine eigene Spalte anlegt — sie verknüpft nur bestehende JSON-Felder
mit der neuen Tabelle. **(H) ist als „optional" markiert**, die Speicherflotten-Zeile nicht; ihre
Einfrierregel-Spalte trägt „**ja** (Projekt 1046)" — die einzige Einfrierregel-Markierung, die die
ganze Tafel (Schritte 102 bis 118, `:585–595`) außer bei dieser Zeile trägt.

Direkt vor der Tafel steht die allgemeine Regel zu den geplanten Schritten (`:576–578`):

> Damit keine Nummer zweimal vergeben wird, führt dieses Papier die geplanten Schritte fortan mit
> **Buchstaben**. Jeder bekommt seine Nummer **bei der Umsetzung**, aus dem dann freien Bereich (nach
> der Vergabe vom 24.09.2026 ab 119), und der Umsetzende misst sie an `SchemaStand.Zielversion` neu —
> nicht an diesem Papier.

`(H)` war „vormals (104)" — also eine ältere, inzwischen umnummerierte Planung; die Speicherflotten-
Zeile hatte nie eine Nummer, auch keine „vormals"-Nummer.

---

## Weitere Fundstellen zu ND‑S3, Speicherflotte, Neueinfrieren, Instandsetzung, Wartung, Gerätekatalog (Analysepapier)

**§ 2.4 „Nebenkonzepte"** (Tafelzeilen `:236–237`):

> | Nutzungsdauer | S3 Instandsetzung/Wartung, Gerätekataloge | offen, Spalten liegen
> (`Instandsetzung_Prozent`, `Wartung_Prozent`) | `05/§ 2.1` |
> | Nutzungsdauer | fünf Stücke § 2.13 (3) | 2 und 3 gebaut; 1 Entkopplung, 4 geräteeigene Spalten, 5
> Speicherflotte offen; Hinweis „k von n ohne Dauer" halb (Kostendialog ja, Seite/Bericht nein) |
> `05/§ 2.3` |

**§ 3.1 „Rechenkern", Einleitungspunkt 5** (`:102–109`, Auszug):

> nötig sind sieben ergebnisneutrale DDL-Schritte (Schritte **A–G**, § 6; … B, C und D sind 116, 117
> und 118, gebaut #461) … Nur der Anschluss der **Speicherflotte** berührt die Einfrierregel der
> Referenzbasis.

**§ 3.4 „Datenmodell und Schema", Befund D4** (`:327`):

> | D4 | Speicherflotte: `ErsatzintervallJahre` und `RestwertEuro` sind keine Spalten, sondern
> JSON-Felder des Flottenstands in `Tab_SpeicherAuslegung` — ihr Anschluss ändert den Flottenstand
> des Projekts 1046 und berührt die **Einfrierregel** | `03/§ 3.6` |

**§ 5 „Reihenfolge und Begründung"** nennt E10 nicht ausdrücklich (nur E0–E3, E7, E8, E9, `:526–531`);
E10 steht dort **nicht** in der Begründungskette — die Reihenfolge nach E9 ergibt sich nur aus der
Tabellenposition und aus der Voraussetzungsspalte „ND‑S3-Entscheid, A7" der E10-Zeile selbst.

**§ 7 „Berichtigungen"**, Zeile zu Konzept § 6.2 (`:632`):

> | Konzept Z. 712 (§ 2.11.5), Z. 928, Z. 966–968 | „6 von 8 Rahmenspalten vorhanden (Schritt 71)";
> „95 von 101 Positionen ohne Dauer"; **Speicherflotte als JSON-Felder mit Einfrierfolge** | `03/§ 6.2` |

Kein Treffer im Analysepapier für „Gerätekatalog" als eigenes Wort außerhalb der E10-Zeile selbst
(„Gerätekataloge" in der E10-Inhaltsspalte, `:478`) und der A7/A8-Zeilen.

---

## Nutzungsdauer-Konzept (`Konzept_Nutzungsdauer_AfA_EPOS-Plan.md`, 284 Zeilen)

Kopf (`:1–7`, wörtlich):

> # Konzept: Nutzungsdauer je Technik und Positionsart aus einer AfA-Tabelle
>
> Stand 23.09.2026 — Anwenderentscheid 14.09.2026: ND-Q1 bis ND-Q8 nach Empfehlung. **Die Stufen S1
> und S2 sind umgesetzt**; S3 (Instandsetzung, Wartung, Gerätekataloge) steht aus und braucht einen
> eigenen Entscheid (Abschnitte 3 und 6). Codestand `41764ab0`, `SchemaStand.Zielversion` = **113**
> (neue Schritte ab 114). **ND‑S3 ist die Etappe E10** des Etappenplans E0–E12 im Analysepapier …

(Der Kopf trägt den Codestand `41764ab0`/Zielversion 113 aus einer älteren Fassung — im gelesenen
Worktree `e9b` steht `SchemaStand.Zielversion` inzwischen auf 118, siehe Abschnitt „Codestellen"; das
Papier selbst wurde seither nicht auf diesen Stand nachgezogen.)

**Tabelle `Tab_Nutzungsdauer`, § 2.2 (`:100–116`):** elf Spalten — `ID`, `KomponentenID` (FK
`Tab_KostenKomponente`, NULL = technikübergreifend), `Positionsart`, `IstStandard`, `Nutzungsdauer_a`,
`AfA_steuerlich_a` (nur Anzeige, ND-Q1), **`Instandsetzung_Prozent`, `Wartung_Prozent` (REAL, NULL
erlaubt) — „VDI 2067 Blatt 1, Tabelle A2 — Stufe S3 (ND-Q6)"** (`:110`), `Quelle`, `ReadOnly`,
`Sortierung`; eindeutig ist (`KomponentenID`, `Positionsart`).

**§ 3 „Stufenplan" (`:224–232`, wörtlich):**

> | Stufe | Umfang | Nachweis |
> |---|---|---|
> | **S1 Tabelle und Verwaltung** — **umgesetzt** | … | Kern-/bunit-Tests, SqlDialektPruefer,
> Referenzlauf 13/13 byte-gleich |
> | **S2 Vorbelegung** — **umgesetzt** | … | bunit-Fälle je Weg, Kern-Fall gegen die
> Kapitalwertrechnung, Referenzlauf byte-gleich |
> | **S3 Instandsetzung und Wartung** | Spalten in der Tabelle sichtbar, `BetriebskostenCtrl` liest
> Sätze je Technik statt Konstanten; Vorbelegung der Gerätekataloge | eigener Entscheid, Referenzlauf
> mit Abweichungen nur in Betriebskosten → neue Basis |
>
> S3 nur nach Entscheid; es ist die Etappe **E10** des Etappenplans E0–E12.

**„Offen aus S2"-Absatz (`:241–253`, wörtlich, Kernsatz):**

> Ebenso offen: die geräteeigenen Dauerspalten und der Anschluss der Speicherflotte (Mockup-Anhang
> U39). Dazu sind zwei Entscheide gefallen (**20.09.2026, nach Empfehlung**): **A7** — die
> Speicherflotte wird **mit ND‑S3** an `Tab_Nutzungsdauer` angeschlossen, als eigener Auftrag mit
> Neueinfrieren der Referenzbasis (Projekt 1046); **A8** — die geräteeigenen Nutzungsdauer-Spalten
> (`Tab_BHKW`, `Tab_Heizkessel`) werden **nicht jetzt** abgekündigt, sondern nur **gekennzeichnet**;
> die Speichervariante sollte die Positionsarten 20/21 lesen. Ein Schemaschritt dafür (vormals „104")
> bekommt seine Nummer erst bei der Umsetzung.

**§ 4 „Fragen mit Empfehlung", ND-Q6 und ND-Q7 (`:268–269`, wörtlich):**

> | **ND-Q6** Spalten Instandsetzung/Wartung | (a) mit S1 anlegen, Anzeige ab S3, (b) erst mit S3 | (a)
> — ein Schema-Schritt statt zwei |
> | **ND-Q7** Gerätekataloge (BHKW, Kessel, Stromspeicher) | (a) unberührt, (b) Vorbelegung aus der
> Tabelle | (a) in S1/S2; (b) mit S3 prüfen |

**§ 6 „Umsetzung", Zeile S3 (`:283`):** `| S3 | Instandsetzung/Wartung, Gerätekataloge | eigener
Entscheid |` — als einzige der drei Umsetzungszeilen ohne „umgesetzt"-Marke.

**§ 5 „Nicht enthalten" (`:274–275`):** „Die Betriebskostenprozentsätze nach VDI 2067 bleiben bis S3
Konstanten" — deckt sich mit dem Befund unten (Abschnitt „Codestellen"), dass `BetriebskostenCtrl` die
neuen Spalten heute nicht liest.

---

## Register-Entscheide mit E10-Bezug

Datei: `Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md` (553 Zeilen).

**R‑A, A7 und A8 (`:77–78`, wörtlich, mit den „gemessen #452"-Zusätzen, die über das Analysepapier
hinausgehen):**

> | **A7** | Speicherflotte an `Tab_Nutzungsdauer` anschließen (Basis neu einfrieren) — jetzt oder mit
> ND‑S3? | Mit ND‑S3, als eigener Auftrag mit Neueinfrieren | 20.09.2026, nach Empfehlung | § 2.13
> (3); Nutzungsdauer-Konzept (S3) | entschieden (Speicherflotte **mit ND‑S3**, eigener Auftrag mit
> Neueinfrieren), nicht gebaut · E10; **gemessen #452** (§ 6.3 Nr. 9h: Flotte 1046 zwei Einheiten,
> Ersatzintervall 10 a, Restwert 500/300 €; ein linearer Restwert aus der Nutzungsdauer erst mit
> ND‑S3 und neu eingefrorener Basis) |
> | **A8** | Geräteeigene Nutzungsdauer-Spalten (`Tab_BHKW`, `Tab_Heizkessel`) abkündigen? | **Nicht
> jetzt** — kennzeichnen; die Speichervariante sollte die Positionsarten 20/21 lesen | 20.09.2026,
> nach Empfehlung | § 2.13 (3) | entschieden (geräteeigene Spalten **nicht jetzt**, nur
> kennzeichnen), nicht ausgeführt · E10; **gemessen #452** (§ 6.3 Nr. 9h: `Tab_BHKW` 3 von 6
> gepflegt, Stamm 44 von 79; `Tab_Heizkessel` 1 von 22, Stamm 0 von 63 — beide ohne Leser in der
> Wirtschaftlichkeit; Vorschlag für ND‑S3: nur als „Gerätedaten" kennzeichnen) |

**R‑ND — ND‑Q1…Q8 (`:260–273`, vollständig, wörtlich):**

> Quelle: `Nutzungsdauer-Konzept` (../Konzept_Nutzungsdauer_AfA_EPOS-Plan.md) § 4 („Entscheid
> 14.09.2026: alle acht Fragen nach Empfehlung").
>
> | **ND-Q1** | Quelle der Vorbelegung … | (c): VDI 2067 rechnet, die AfA-Spalte informiert | …
> | umgesetzt (S1) |
> | **ND-Q2** | Schlüssel der Zuordnung … | (a) — nur so bekommen Abgasanlage und MSR andere Werte
> als der Kessel | … | umgesetzt (S1) |
> | **ND-Q3** | Ort in der Administration … | ja, dritter Eintrag neben Kostenvorlagen und
> Energieträgern | … | umgesetzt (S1) |
> | **ND-Q4** | Bestehende Positionen … | (b) — nichts ändert eine gerechnete Wirtschaftlichkeit ohne
> Zutun | … | umgesetzt #357 (S2: Knopf „Nutzungsdauern vorbelegen…") |
> | **ND-Q5** | Auslieferungszeilen … | (b) — die Tabelle ist die Tabelle des Anwenders | … |
> umgesetzt (S1) |
> | **ND-Q6** | Spalten Instandsetzung/Wartung … | (a) — ein Schema-Schritt statt zwei | … | Spalten
> mit S1 angelegt; Anzeige mit S3 (E10) |
> | **ND-Q7** | Gerätekataloge (BHKW, Kessel, Stromspeicher) … | (a) in S1/S2; (b) mit S3 prüfen | … |
> (a) in S1/S2 gilt; (b) mit S3 zu prüfen (E10) |
> | **ND-Q8** | Startwerte … | vom Anwender zu bestätigen — Normwerte werden nicht erfunden … | … |
> umgesetzt (Saat als Richtwert) |

**Nr. 20 (R‑NR, `:290`)** nennt E10 als eine der drei rechenwirksamen Etappen, deren A/B-Nachweise den
entfallenden Zahlenprobe-Nachweis (E11) ersetzen: „… die A/B-Nachweise der rechenwirksamen Etappen
(E7, E9, **E10**)".

Keine weitere Registerfamilie (R‑V, R‑K, R‑D, R‑BK, R‑VG, R‑VV, R‑E4…R‑E9a, R‑EZ) nennt E10, ND‑S3
oder Speicherflotte; R‑EZ (Einzelentscheide) hat keinen Bezug.

---

## Konsolidiertes Konzept zu E10

Datei: `Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md` (2 904 Zeilen).

**§ 2.13 (3)-Umfeld, „Was der späteren Umsetzung fehlt" (`:1204–1210`, wörtlich, Mockup-Anhang U39):**

> 1. die **geräteeigenen Nutzungsdauer-Spalten** (`Tab_BHKW`, `Tab_Heizkessel`,
>    `Tab_StromspeicherVariante`) — eine zweite Wahrheit, die kein Wirtschaftlichkeitsrechner liest;
> 2. der **Anschluss der Speicherflotte**, die ihren Ersatz über die gleichnamigen **Felder des
>    Flottenstands** (`ErsatzintervallJahre`, `RestwertEuro` als JSON in `Tab_SpeicherAuslegung`)
>    führt — nicht über Spalten; der Anschluss berührt deshalb die **Einfrierregel** des Projekts
>    1046.

**§ 6.3 „Offene Punkte", Nr. 9h — die ausführlichste Einzelfundstelle im ganzen Bestand (`:2681–2688`,
wörtlich):**

> 9h. **Nutzungsdauer, Ersatz, Restwert — zwei fehlende Stücke (Mockup-Anhang U39):** die ungelesenen
>     geräteeigenen Nutzungsdauer-Spalten und der Anschluss der Speicherflotte — offen mit ND‑S3 (E10;
>     A7 und A8 binden beides daran). **Gemessen mit E7c3 (#452):** `Tab_BHKW` führt eine Nutzungsdauer
>     in 3 von 6 Zeilen der Testdatenbank (10 a), im Stamm in 44 von 79; `Tab_Heizkessel` in 1 von 22
>     (20 a), im Stamm in 0 von 63 — beide ohne Leser in der Wirtschaftlichkeit; `Tab_StromspeicherVariante`
>     in 13 von 13 (20 a), sie rechnet in der Speicherwirtschaftlichkeit und im Peak-Shaving. Die
>     Nutzungsdauer-Tabelle führt abweichend BHKW-Modul 15 a und Batterie 10 a; die Flotte von 1046 zwei
>     Einheiten mit Ersatzintervall 10 a und Restwert 500 bzw. 300 €. **Vorschlag für ND‑S3:** die
>     Gerätespalten nur als „Gerätedaten" kennzeichnen (A8); für Speichervariante und Flottenintervall
>     die Tabelle nur als Vorgabe neuer Einträge nehmen — das bewegt nichts; ein linearer Restwert aus
>     der Nutzungsdauer erst mit ND‑S3 und einer neu eingefrorenen Basis für 1046.

Wichtig: `Tab_StromspeicherVariante` unterscheidet sich von `Tab_BHKW`/`Tab_Heizkessel` — ihre
Nutzungsdauer **wird bereits gelesen** (Speicherwirtschaftlichkeit, Peak-Shaving), nur `Tab_BHKW` und
`Tab_Heizkessel` sind „ohne Leser in der Wirtschaftlichkeit". Der „Vorschlag für ND‑S3" im Zitat ist
ausdrücklich ein Vorschlag der E7c3-Messung, kein Anwenderentscheid.

**§ 6.3, Item 19 — Asymmetrie (`:2714`):** `19. Asymmetrie „Wartung BHKW" gegen „Vollwartung /
Wartung Kessel"` — ohne Streichung (also offen), ohne weitere Erläuterung an dieser Stelle; kein
anderer Fundort im gelesenen Bestand erklärt den Unterschied näher (siehe Unklarheiten).

**Anhang „Kürzel und Etappen" — drei Tafelzeilen (wörtlich):**

`:2861` (Familientafel):

> | § 6.3 Nr. 9h | — | **S2-Rest / U39** | **#357**, **#434** (Hinweiszeile), **#446** (Entkopplung,
> Schritt 111), **#452** (gemessen) | Nutzungsdauer, Ersatz, Restwert; der Rest mit ND‑S3 |

`:2871`:

> | — | — | **S1 · S2 · S3** | S1 vor #300, S2 = #357, S3 offen (**E10**) | AfA-Tabelle |

`:2872` (Mockup-Anhang-Zeile, Auszug):

> … U39 teilweise (Nr. 9h gemessen #452, der Rest mit ND‑S3) …

**Etappentafel, letzte Zeile (`:2894`, wörtlich) — zeigt, dass E10 laut diesem Papier NICHT die
unmittelbar nächste Etappe ist:**

> | **E9** Teil b … **E12** | V-E Teil b (±-Knopf an drei Orten, Zeilen 8 und 9 der Szenariotafel,
> Hinweistext entfällt, Ausweis „n von m") · **ND-S3** · Wiki (E11 entfällt) | **nächste Etappe: E9b**
> (voraussichtlich #462); offen die acht Fragen aus E7c3 (→ Register R‑E7c3), die zwei aus E8c (→
> Register R‑E8c) und die sieben aus E9a (→ Register R‑E9a); E8b entschieden und gebaut (→ Register
> R‑E8b) |

**§ 7 „Vorgeschlagene Reihenfolge" (`:2794`, Überschrift; Zitat `:2802–2805`):** bestätigt denselben
Stand — „**E9** (V‑E, die Szenarioabdeckung nach § 2.11.5) läuft in zwei Wellen: **E9 Teil a (#461) ist
gebaut** … **als Nächstes kommt E9b** (voraussichtlich #462) mit dem ±-Knopf an den drei neuen Orten
und den Zeilen 8 und 9 der Szenariotafel, mit ihr entfällt der Hinweistext (§ 2.11.7) zugunsten des
Ausweises „n von m Parametern szenariert"." E10 wird in diesem Abschnitt nicht als eigener nächster
Schritt benannt, sondern nur in der Etappentafel am Ende (`:2894`) als Teil der Restgruppe „E9 Teil b
… E12" mitgeführt.

---

## Statusdatei `Status_iOS_Migration.md` (600 Zeilen)

**Nach #429 (E0c, Papierpflege-Nachzug), `:349`, Auszug zum Nutzungsdauer-Konzept:** „**Nutzungsdauer:**
Zielversion 100, ND‑S3 = E10, die Hülle der Kostenverwaltung als E3 Schritt 5 mit dem
#428-Muster, A6/A7/A8 entschieden."

**Nach #264 (Stufenplan-Ankündigung), `:440`:**

> Umsetzung nach Stufenplan: S1 (#269: Tabelle, Saat, Verwaltung, Kern-Vorbelegung), S2 (#270: Hülle
> nach `EPOS.UI.Daten`, Kostendialoge mit Herkunftsanzeige), S3 (Rechenwirkung
> Instandsetzung/Wartung, neue Referenzbasis) auf Zuruf; Konzept wandert nach `ueberholt/`, sobald S3
> abgeschlossen ist.

**Nach #269 (S1 umgesetzt), `:444`:**

> Stufe S2 (#270): Knopf „Nutzungsdauern vorbelegen", Positionsart im Zeileneditor, Herleitungszeile
> in der Kostenverwaltung, Hülle nach `EPOS.UI.Daten`; S3: `Instandsetzung_Prozent`/`Wartung_Prozent`
> sind angelegt, aber weder sichtbar noch gelesen; 22 Vorlagenpositionen ohne zuordenbaren Namen
> bleiben ohne `NutzungsdauerID` (Technik-Standard greift); iOS kennt die Rubrik Kostenverwaltung
> insgesamt nicht (wie Energieträger).

**Nach #357 (Nutzungsdauer S2/U30), `:514`:**

> (a) Die Hülle der Kostenverwaltung bleibt in der Windows-Schale (Konzept § 6 B sah den Umzug nach
> `EPOS.UI.Daten` vor; 1 107 Zeilen mit WinForms-Naht) — eigener Auftrag. (b) U39 (Rest von U8):
> Entkopplung Ersatz/Restwert, geräteeigene Dauerspalten, Speicherflotte, plattformfreie
> Hinweiszeile der Ergebnisseite (Anwenderentscheid).

**Nach #461 (E9a, aktuellster Eintrag mit Bezug zur Reihenfolge), `:562`, Punkt (g), wörtlich:**

> **Nächste Etappe: E9b** (V‑E, Teil b; Statusnummer voraussichtlich #462; Auftrag
> `E9b_Auftrag_2026-09-24.md`, Zweig `e9b` ab `origin` nach dem Push dieser Welle, Schemastand 118) —
> die Pflege in den Dialogen: der ±-Knopf an drei Orten … Offen beim Anwender: E7c3‑Q1 bis E7c3‑Q8,
> E8c‑Q1/Q2 und E9a‑Q1 bis E9a‑Q7.

Der Auftrag zu E9b bestätigt ausdrücklich, dass der Zweig `e9b` (der hier gelesene Worktree) genau die
Etappe **vor** E10 bearbeitet; E10 selbst hat in der Statusdatei **keine eigene Statuszeile** — kein
Treffer für „E10" in der ganzen Datei außerhalb des oben zitierten Nutzungsdauer-Satzes nach #429 und
der (indirekten) Erwähnung in den Umsetzungsplan-Zitaten.

---

## `Referenzlaeufe/LIESMICH.md` — Neueinfrieren und aktuelle Basis R13

**Die allgemeine Regel (Kopf, `:5–8`, wörtlich):**

> Vor jedem Umbau an der Engine wird der aktuelle Stand als CSV eingefroren; nach dem Umbau läuft
> derselbe Satz Projekte erneut und wird mit Toleranz gegen den eingefrorenen Stand verglichen. Was
> sich dabei ändert, ist entweder gewollt — dann wird die Referenz neu gesetzt — oder ein Fehler.

**Die für E10 einschlägige Einfrierregel — „Die dritte Einfrierregel: die Flottenparameter des
Projekts 1046" (`:100–118`, wörtlich, gekürzt):**

> Projekt **1046 „Prüfprojekt Speicherflotte"** führt den einzigen aktivierten Stand `@Projektflotte`
> in `Tab_SpeicherAuslegung`; er schaltet im gewöhnlichen Projektlauf den **Flottenpfad** ein …
>
> **Wer den Stand `@Projektflotte` des Projekts 1046 ändert, friert im selben Schritt die Basis neu
> ein und begründet den Wechsel hier.**
>
> Betroffen ist jede Änderung an Einheitenzahl, Kapazität, Lade-/Entladeleistung,
> Richtungswirkungsgraden, SoC-Grenzen, Start-SoC, Peak-Reserve, Hilfsverbrauch, Betriebsziel,
> Verteilung, Peak-Ziel, Netzladung, Batterieexport, Energie-Ausgleichswert oder Lebensdauerkurve —
> und ebenso jede Änderung an den Projektzeilen von 1046 selbst.
>
> **Nicht** betroffen sind Auslegungs- und Arbeitsstände unter einem ANDEREN Bezeichner als
> `@Projektflotte`: Sie erreichen den Projektlauf nicht.

Ein Anschluss von `ErsatzintervallJahre`/`RestwertEuro` an `Tab_Nutzungsdauer`, der den Rechenweg der
Speicherflotte ändert (z. B. einen linearen Restwert einführt, der heute nicht gerechnet wird — siehe
Rechenweg unten), fiele der Aufzählung nach nicht wortwörtlich unter eine der genannten Größen
(„Lebensdauerkurve" o. ä.); ob der bestehende Wortlaut diesen Fall schon deckt oder ergänzt werden
müsste, ist offen (siehe Unklarheiten).

**`lauf` — Stand einfrieren (`:509–524`, Auszug):** Das Werkzeug `EPOS.Referenzlauf`/`Referenzlauf`
kopiert die Datenbank, migriert sie auf den Zielstand des Schemas, rechnet die Projekte und schreibt
CSV plus `lauf_protokoll.md`; „Exit-Code 0, wenn alle Projekte durchgelaufen sind."

**Aktuelle Basis (`:283–292`, wörtlich, Kern):**

> **`2026-09-23_R13_Kuehlung/`** — **dreizehn Projekte** (1007, 1008, 1017, 1018, 1023, 1024, 1030,
> 1039, 1040, 1041, 1042, 1045, 1046), **387 CSV**, **2 207 Skalare**, gerechnet mit dem
> plattformfreien `EPOS.Referenzlauf` auf Windows gegen `Kenndaten_Test.sqlite` (Schemastand **113**,
> LFS-SHA-256 `769143e4…`, Nachträge 114 und 115 unten; …). … Sie ist die **einzige** Basis im
> Arbeitsbaum.

Der Abschnitt „Aktuelle Basis" trägt einen Nachtrag bis Schemastand 115 (Zapfprofil-Stufe Z3, `:294`);
**kein Nachtrag für die Schemaschritte 116–118 (E9a) ist eingetragen** — die Datei ist an dieser
Stelle drei Schritte hinter dem tatsächlich ausgecheckten Datenbankstand (siehe Abschnitt
„Codestellen": `SchemaStand.Zielversion = 118`, LFS-Objekt `e9748b7f…`). Projekt **1046** gehört zu
den 13 Basisprojekten von R13.

---

## Rechenweg (`Rechenweg/08_Wirtschaftlichkeit_Nutzungsdauer.md`, 200 Zeilen)

Trotz des Dateinamens „…_Nutzungsdauer.md" behandelt dieses Papier **nicht** in erster Linie die
Nutzungsdauer-Tabelle oder Instandsetzung/Wartung, sondern den gesamten Rechenweg der
ValERI-Kapitalwertsicht (Dialog `UcWirtschaftlichkeit`, Umschalter „Kennzahlen / ValERI-Bewertung").
Eine Volltextsuche nach „Instandsetzung" oder „Wartungssatz je Technik" ergibt **keinen Treffer**; das
einzige Vorkommen von „Wartung" ist die Fließtext-Randnotiz „Wartung des Motors gegen den Kessel"
(`:120`) in der Höfingen-Beispieltabelle — keine Prozentsatz-Tafel.

**„Berechnungsgrundlage", Abschnitt Nutzungsdauer/Ersatz/Restwert (`:71–80`, wörtlich):**

> Nutzungsdauer, Ersatz, Restwert, Startjahr
>   n = Nutzungsdauer, falls ≥ 1 ; sonst n = T   (dann kein Ersatz, kein Restwert)
>   start = StartJahr, falls > 1 ; sonst 0 ; start = 0 → Betrag in I₀ ; start ≥ 2 → Zahlung im
>   Jahr start, abgezinst, NICHT indexiert ; start > T → keine Zahlung, nur Ausweis
>   Ersatz:   t_j = round(start + k·n)  für k = 1,2,… solange 1 ≤ t_j < T
>   Restwert: Alter = T − letzte Beschaffung ; Restdauer = n − Alter ; RW_T = Betrag × Restdauer / n
>   Kennzeichen je Position (Schemaschritt 111, Konzept § 2.13 (3)) — leer und „ja" rechnen wie oben:
>     ErsatzFuehren = nein    ⇒ keine Ersatzkette ; die letzte Beschaffung bleibt die erste
>     RestwertAnsetzen = nein ⇒ RW_T = 0 ; entkoppelt — eine nicht ersetzte Position trägt ihren
>                               Restwert aus der ersten Beschaffung weiter

Das ist der Rechenweg, den die **Positionen** von `Tab_ProjektWerte`/`Tab_KostenVorlagePosition`
bereits heute nutzen (Kern: `KapitalwertRechner`). Ob und wie die **Speicherflotte**
(`ErsatzintervallJahre`/`RestwertEuro` in `Tab_SpeicherAuslegung`, gerechnet in
`SpeicherEngine/FlottenWirtschaftlichkeit.cs`, nicht in `KapitalwertRechner`) denselben oder einen
eigenen Restwert-Rechenweg bekäme, sagt dieses Papier nicht (siehe Unklarheiten).

**Betriebskosten-Nennung (`:24`, Berechnungsgrundlage-Formel):** `A_t = Betrieb_t × (1 + p_B)^(t−1)
+ …` — Betriebskosten laufen als eine einzige, bereits bemessene Jahresreihe ein; keine Aufschlüsselung
nach Instandsetzung/Wartung/Technik an dieser Stelle.

---

## Codestellen

**Schema — `EPOS.Kern/Allgemein/Update/NutzungsdauerSchema.cs`** (Namensraum
`WindowsFormsApplication1`, wie im ganzen Kern üblich):

- `:8` — Kopfkommentar: „Die NUTZUNGSDAUERTABELLE - **Migrationsschritt 75**, Stufe S1 des Konzepts".
- `:146` — `TABELLE = "Tab_Nutzungsdauer"`.
- `:158/:161/:165/:168` — Spaltenkonstanten `SPALTE_NUTZUNGSDAUER = "Nutzungsdauer_a"`,
  `SPALTE_AFA = "AfA_steuerlich_a"`, **`SPALTE_INSTANDSETZUNG = "Instandsetzung_Prozent"`**,
  **`SPALTE_WARTUNG = "Wartung_Prozent"`** — Kommentar bei beiden: „VDI 2067 Blatt 1, Tabelle A2 —
  Stufe S3".
- `:218–233` — `SQL_CREATE`: elf Spalten, `UNIQUE (KomponentenID, Positionsart)`, `FOREIGN KEY` auf
  `Tab_KostenKomponente`, **STRICT**; `Instandsetzung_Prozent`/`Wartung_Prozent` beide `REAL` ohne
  `NOT NULL`.
- `:297–351` — `Saat`: 28 Auslieferungszeilen (zehn Techniken plus zwei technikübergreifende), keine
  Zeile trägt einen Instandsetzung- oder Wartungswert (beide Spalten bleiben in der Saat leer — die
  Konstruktorsignatur der Saatzeile, `:64–66`, kennt diese zwei Felder gar nicht).
- `:360–408` — `Zuordnungen`: 31 der 53 Investitionspositionen bekommen eine feste Positionsart-Zeile.

**Verwaltung — `EPOS.Kern/Controller/NutzungsdauerCtrl.cs`:** `NutzungsdauerZeile` (`:12`),
`NutzungsdauerVorgabe` (`:52`), `NutzungsdauerCtrl` (`:86`, statisch). `Instandsetzung_Prozent`/
`Wartung_Prozent` werden dort **gelesen und geschrieben** — im Ladeweg der Administrationstabelle
(`:156–157`, Teil einer `SELECT`-Spaltenliste) und im Speicherweg (`:412–413`, Teil eines
`UPDATE`-Satzes) — also editierbar im Dialog „Nutzungsdauern (AfA)", aber (siehe unten) nirgends von
einem Rechenweg gelesen.

**Kein Leser außerhalb von Schema und Verwaltung:** `grep -rn "Instandsetzung_Prozent\|Wartung_Prozent"
--include=*.cs` (Repo-weit, ohne `bin`/`obj`) trifft **ausschließlich**
`NutzungsdauerSchema.cs` und `NutzungsdauerCtrl.cs` — keine Wirtschaftlichkeits- oder
Betriebskostenklasse liest die beiden Spalten.

**`EPOS.Kern/Controller/BetriebskostenCtrl.cs`** (`internal static class BetriebskostenCtrl`, `:41`):
„Der EINE Rechenweg der Betriebskostenpositionen nach VDI 2067" — `Betrag(bemessung, eingegeben, menge,
satz, istErloes)` rechnet bei den Bemessungsarten `PROZENT_INVESTITION`, `PROZENT_BRENNSTOFFKOSTEN`,
`PROZENT_ERZEUGERKOSTEN`, `PROZENT_STROMKOSTEN`, `PROZENT_ENDENERGIEKOSTEN`,
`PROZENT_ENDENERGIEBEDARF` einheitlich `menge × satz / 100`; `satz` kommt als Parameter herein (keine
eigene Konstante je Technik im Controller selbst) — die Nutzungsdauer-Konzept-Aussage „liest Sätze je
Technik statt Konstanten" (§ 3, S3-Zeile) bezieht sich damit vermutlich auf die **Vorgabe/Vorbelegung**
des Satzes in der Kostenvorlage, nicht auf `BetriebskostenCtrl` selbst (nicht abschließend geklärt,
siehe Unklarheiten).

**Weitere Nutzungsdauer-Klassen:**
- `EPOS.Kern/Controller/NutzungsdauerHinweisCtrl.cs`: `NutzungsdauerHinweise` (`:70`),
  `NutzungsdauerHinweisCtrl` (`:118`, internal) — der „k von n ohne Dauer"-Hinweis (§ 2.13 (3) des
  konsolidierten Konzepts).
- `EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitEmpfehlung.cs:373` —
  `public static class NutzungsdauerAbgleich`.

**Geräteeigene Nutzungsdauer-Spalten (A8) — Modelle:**
- `EPOS.Kern/Model/BHKWModel.cs:35` und `BHKWStammModel.cs:38` — `public int m_Nutzungsdauer;`
  (**Typ `int`**).
- `EPOS.Kern/Model/HeizkesselModel.cs:33` — `public double Nutzungsdauer;` (**Typ `double`**).
- `EPOS.Kern/Model/StromspeicherVarianteModel.cs:132` — `public double Nutzungsdauer =
  NUTZUNGSDAUER_VORGABE;` — diese Variante hat laut Konzept § 6.3 Nr. 9h (oben) tatsächlich einen
  Leser in der Speicherwirtschaftlichkeit.

**Speicherflotte / `Tab_SpeicherAuslegung` — `ErsatzintervallJahre`/`RestwertEuro`:**
- `SpeicherEngine/FlottenModel.cs:541/:544` (Klasse `FlottenEinheit`, Felder `ErsatzintervallJahre`
  [int], `RestwertEuro` [double]) und `:1394` (zweites `RestwertEuro`, andere Klasse — vermutlich
  Gesamtflotte).
- `SpeicherEngine/FlottenWirtschaftlichkeit.cs:54–55/:120–121/:140/:196` — Ersatzkosten aus
  `jahr % ErsatzintervallJahre == 0`, Restwert als Summe `RestwertEuro` je Einheit plus Flottenwert;
  **kein linearer Restwert nach Nutzungsdauer wie in `KapitalwertRechner`** — die Speicherflotte
  rechnet ihren Restwert heute als festen, direkt eingegebenen Betrag, nicht linear aus einer
  Nutzungsdauer hergeleitet.
- `EPOS.Kern/Controller/SpeicherFlottenStudieCtrl.cs:548–549` — `s.ErsatzintervallJahre = 0;
  s.RestwertEuro = 0;` (eine Rückstell-/Vorgabestelle).
- `EPOS.UI/Seiten/Strom/StromspeicherKiSicht.cs:609–610/:1046–1047/:1053–1054` — KI-Sicht-Felder für
  beide Größen, editierbar im Flotten-Editor.

**`Tab_SpeicherAuslegung` selbst** trägt laut PRAGMA (unten) **keine** `ErsatzintervallJahre`/
`RestwertEuro`-Spalten — sie liegen ausschließlich als Teil des JSON-Blobs in der Spalte `Daten`
(bestätigt Konzeptbefund D4 und § 6.3 Nr. 9h oben).

**Schema-Stand:**
- `EPOS.Kern/Allgemein/Update/SchemaStand.cs:460` — `public const int Zielversion = 118;`
- Höchster definierter Schemaschritt im Code:
  `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs:8872–8880` —
  `SCHRITT_118_ERLOESSATZ_SZENARIO` / `SchemaKatalog.Schritt118_ErloessatzSzenario` („reines DDL an
  zwei Tabellen"); kein `SCHRITT_119` oder höher im Bestand — **E10 hat noch keine reservierte
  Schemaschritt-Nummer im Code**, deckungsgleich mit der Papierlage („Nummer erst bei der Umsetzung").

**Tests:** `EPOS.Kern.Tests/NutzungsdauerTests.cs` — `public class NutzungsdauerTests :
IClassFixture<TestDatenbank>`, 15 `[Fact]`-Methoden (Zeilen 43–315, keine mit Instandsetzung/Wartung
im sichtbaren Methodenkopf; Methodennamen wurden nicht einzeln gelesen). Weitere Dateien mit
„Nutzungsdauer" im Namen oder Text: `ErsatzRestwertKennzeichenTests.cs`, `ErsatzRestwertTafelTests.cs`,
`ProjektWerteSchemaWacheTests.cs`, `SzenarioParameterTests.cs` (Nutzungsdaueränderung als
Szenariogröße), `SpeicherFlottenProjektKostenTests.cs`, `SpeichervarianteSicherstellenTests.cs` — keine
eigene Testklasse für Instandsetzung/Wartung-Sätze gefunden (konsistent mit „S3 noch nicht gebaut").

---

## Heutige Spalten der vier Tabellen

Quelle: `PRAGMA table_info(...)` gegen `Referenzlaeufe/Kenndaten_Test.sqlite`, schreibgeschützt
(`py -c` mit dem `sqlite3`-Modul, `sqlite3.connect('file:...?mode=ro', uri=True)`). Datei-Prüfung
vorab: 67 784 704 Byte, `git lfs ls-files` nennt `e9748b7f48 * Referenzlaeufe/Kenndaten_Test.sqlite` —
eine real ausgecheckte LFS-Datei (nicht nur ein Zeiger), Größe deckt sich exakt mit der in
`Status_iOS_Migration.md` (Nach #461) genannten Zahl für Schemastand 118.

### `Tab_Nutzungsdauer` (11 Spalten, ID 0–10)

`ID`, `KomponentenID`, `Positionsart` (NOT NULL), `IstStandard` (NOT NULL, Vorgabe 0),
`Nutzungsdauer_a`, `AfA_steuerlich_a`, **`Instandsetzung_Prozent`, `Wartung_Prozent`** (beide REAL,
nullbar), `Quelle`, `ReadOnly` (NOT NULL, Vorgabe 0), `Sortierung` — deckungsgleich mit
`NutzungsdauerSchema.SQL_CREATE`.

### `Tab_SpeicherAuslegung` (6 Spalten, ID 0–5)

`ID`, `ID_Projekt` (NOT NULL), `ID_Energieanlage`, `Bezeichner` (NOT NULL), `Daten` (NOT NULL, TEXT —
der JSON-Blob, der u. a. `ErsatzintervallJahre`/`RestwertEuro` je Flotteneinheit trägt), `Stand` (NOT
NULL, TEXT). Keine eigenen Spalten für Ersatzintervall/Restwert — beide stecken ausschließlich im
JSON von `Daten`.

### `Tab_BHKW` (29 Spalten, ID 0–28)

Relevant: `Wartungskosten_kwhel` (REAL) — **kein** „Instandsetzung"-Feld; `Nutzungsdauer` (**INTEGER**,
Spalte 12) — geräteeigen, ohne erkennbaren Bezug zu `Tab_Nutzungsdauer`.

### `Tab_Heizkessel` (23 Spalten, ID 0–22)

Relevant: `Wartungskosten` (REAL) und **zusätzlich** `Wartungskosten_Einheit` (TEXT) — entspricht der
Konzepttafel § 2.1 „Wartungskosten mit Einheit (€/a | €/kWh | %/a)"; `Nutzungsdauer` (**REAL**, Spalte
12) — anders typisiert als bei `Tab_BHKW` (dort INTEGER). Eine Spalte `8` ist in der PRAGMA-Ausgabe als
`Wirkungsgrad_<Kodierfehler>l` sichtbar (vermutlich „Wirkungsgrad_Öl", Umlaut in der Werkzeugausgabe
nicht sauber dekodiert) — reiner Lesebefund der Abfrage, keine Bewertung der Datenbank selbst.

**Zusammengefasst zur A8-Frage:** Weder `Tab_BHKW` noch `Tab_Heizkessel` haben eine
`Instandsetzung`-Spalte; beide haben eine `Wartung(skosten)`-Spalte unterschiedlicher Bauart
(mit/ohne Einheit-Spalte, unterschiedlicher Nutzungsdauer-Typ) — das deckt sich mit dem offenen Item 19
des konsolidierten Konzepts („Asymmetrie „Wartung BHKW" gegen „Vollwartung / Wartung Kessel"").

---

## Schemaschritt-Nummern

Im gelesenen Worktree (`e9b`, Commit `018520f8`): `SchemaStand.cs:460` → `Zielversion = 118`. Höchster
Schritt im Code: **118** (`SCHRITT_118_ERLOESSATZ_SZENARIO`, E9a, gebaut #461). Der nächste freie
Schritt ist laut Analysepapier § 6 (`:572`: „`SchemaStand.Zielversion` steht auf **118**; der nächste
freie Schritt ist 119.") **119**; E9b (die laufende Etappe dieses Worktrees) braucht
laut Statuszeile #461 (g) „kein Schemaschritt" — der nächste tatsächlich vergebene Schritt bliebe damit
119, sofern zwischen diesem Faktenstand und dem Beginn von E10 keine andere Etappe (z. B. eine
Kühlungs- oder Zapfprofil-Folgewelle, wie es bei 114/115 der Fall war) ihn beansprucht. Die einzige für
E10 selbst vorgemerkte Schemaschritt-Kennung ist der Buchstabe **(H)** „vormals (104)" für die
geräteeigenen Spalten (optional); der Speicherflotten-Teil von E10 bekommt laut Analysepapier
`:595` **gar keine** eigene Schemaschritt-Zeile (kein Buchstabe, kein „vormals").

`Referenzlaeufe/LIESMICH.md` „Aktuelle Basis" nennt nur Nachträge bis Schemastand 115 (siehe oben) —
die Lücke zu 118 ist im gelesenen Papierbestand nicht durch einen eigenen LIESMICH-Nachtrag
geschlossen, wohl aber durch die Statuszeile #461 und den tatsächlichen Datenbank-Footprint
(LFS `e9748b7f…`).

---

## Unklarheiten

1. **E10 ist nicht die unmittelbar nächste Etappe.** Der Auftrag bezeichnet E10 als „nächste Etappe";
   alle gelesenen, aktuell gültigen Papiere (konsolidiertes Konzept `:2894`, Statuszeile #461 (g)) und
   der gelesene Zweig selbst (`e9b`) zeigen **E9b** (voraussichtlich #462) als die tatsächlich nächste
   Etappe, danach erst „E9 Teil b … E12" mit ND‑S3 (E10) als einem Teil dieser Restgruppe. Ob „nächste
   Etappe" im Auftrag „nächste noch nicht begonnene Rechenwirkungs-Etappe nach E9b" meint oder ob der
   Auftraggeber E9b bereits als abgeschlossen ansieht, ist aus dem Auftragstext allein nicht zu
   entscheiden.
2. **Umfang „Instandsetzung/Wartung je Technik aus den vorhandenen Spalten" bleibt vage.** Keine
   gelesene Quelle sagt ausdrücklich, ob „vorhandene Spalten" `Tab_Nutzungsdauer.Instandsetzung_Prozent`/
   `Wartung_Prozent` meint (die es laut Schema und PRAGMA bereits gibt, aber ungelesen sind) oder auch
   die bestehenden `Wartungskosten(_Einheit)`-Felder von `Tab_BHKW`/`Tab_Heizkessel`/Kostenvorlage
   einbezieht. Die Nutzungsdauer-Konzept-Aussage „`BetriebskostenCtrl` liest Sätze je Technik statt
   Konstanten" (§ 3) ließ sich im Code nicht eindeutig verorten: `BetriebskostenCtrl.Betrag` nimmt den
   Satz als Parameter entgegen und enthält selbst keine Technik-Konstanten — welche Stelle heute
   tatsächlich „Konstanten" statt „Sätze je Technik" liefert (eine Vorgabe in der Kostenvorlage? ein
   Dialog-Defaultwert?), wurde nicht gefunden.
3. **Was „Neueinfrieren der Basis" für E10 konkret verlangt, ist nicht ausbuchstabiert.** Die
   Einfrierregel in `LIESMICH.md` (§ „dritte Einfrierregel") zählt Größen des Flottenstands
   `@Projektflotte` auf, nennt aber nicht ausdrücklich „Ersatzintervall/Restwert-Anschluss an
   `Tab_Nutzungsdauer`" oder „linearer Restwert" als auslösenden Fall — ob der bestehende Wortlaut
   diesen Fall bereits deckt oder bei E10 ergänzt werden müsste, sagt keine Quelle. Ebenso offen: ob
   „Neueinfrieren" nur Projekt 1046 betrifft (wie die Flottenregel) oder — falls
   Instandsetzung/Wartung-Sätze aus `Tab_Nutzungsdauer` in die Betriebskosten einfließen — weitere der
   13 Basisprojekte mit BHKW/Heizkessel/Speicher-Betriebskosten die Basis verschieben.
4. **Ob (H) (geräteeigene Nutzungsdauer entfernen) überhaupt gebaut werden soll, ist nicht
   entschieden — nur ihre Bedingung.** A8 entscheidet „nicht jetzt, nur kennzeichnen"; die Tafelzeile
   `:594` führt (H) trotzdem als „optional" mit eigenem Buchstaben und Voraussetzung „E10, nach A8".
   Ob „kennzeichnen" (A8-Wortlaut) und „entfernen" (H-Wortlaut) dieselbe Maßnahme meinen oder zwei
   verschiedene, nacheinander mögliche Schritte, ist zwischen A8, Nr. 9h und der (H)-Zeile nicht
   deckungsgleich formuliert.
5. **Was Projekt 1046 in E10 „tut", bleibt auf die Flottenparameter-Regel gestützt, nicht auf eine
   E10-eigene Aussage.** Alle Fundstellen (Analysepapier D4, Konzept Nr. 9h, `LIESMICH.md`) beschreiben
   den heutigen Zustand von 1046 (zwei Einheiten, Ersatzintervall 10 a, Restwert 500/300 €) und die
   allgemeine Einfrierregel; keine Quelle beschreibt den Ziel-Rechenweg nach dem Anschluss (bleibt der
   heutige, direkt eingegebene Restwert bestehen und wird nur ergänzt, oder ersetzt ein aus
   `Tab_Nutzungsdauer` hergeleiteter linearer Restwert den heutigen Wert?).
6. **Größenangabe „M + M" ohne Zuordnung.** Keine Quelle sagt ausdrücklich, welches der beiden „M"
   welchem Teilstück (Instandsetzung/Wartung vs. Speicherflotte) zuzuordnen ist oder ob beide Teile
   unabhängig oder in einer Welle umgesetzt werden sollen.
7. **`Referenzlaeufe/LIESMICH.md` ist gegenüber dem Codestand um drei Schemaschritte zurück.** Der
   Abschnitt „Aktuelle Basis" dokumentiert Nachträge nur bis Schemastand 115; die tatsächlich
   ausgecheckte Testdatenbank steht (laut `SchemaStand.Zielversion`, LFS-Objekt-Hash und Statuszeile
   #461) auf 118. Ob das für die E10-Planung relevant ist (etwa weil E10 selbst die nächste Basis
   einfriert und dabei den 116–118-Nachtrag mit nachziehen müsste), sagt keine Quelle ausdrücklich.
8. **Item 19 des konsolidierten Konzepts („Asymmetrie „Wartung BHKW" gegen „Vollwartung / Wartung
   Kessel"") bleibt unerläutert.** Der Fund (PRAGMA: `Tab_BHKW.Wartungskosten_kwhel` ohne Einheit-Spalte
   gegen `Tab_Heizkessel.Wartungskosten` **mit** `Wartungskosten_Einheit`) liefert einen möglichen
   technischen Hintergrund, aber keine gelesene Quelle stellt diesen Zusammenhang ausdrücklich her oder
   sagt, ob E10 diese Asymmetrie beheben soll.
9. **`NutzungsdauerTests.cs`-Methodennamen wurden nicht einzeln gelesen** (nur Zahl und Zeilen der
   `[Fact]`-Attribute) — ob eine der 15 vorhandenen Testmethoden bereits S3-Verhalten vorwegnimmt oder
   ausschließlich S1/S2 deckt, ist damit nicht abschließend gesichert, nur plausibel gemacht durch den
   fehlenden Spaltenzugriff repoweit (Abschnitt „Codestellen").
