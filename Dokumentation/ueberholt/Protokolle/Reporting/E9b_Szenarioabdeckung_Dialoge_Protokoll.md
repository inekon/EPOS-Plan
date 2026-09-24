# E9b — Vollständige Szenarioabdeckung, Teil b: Pflege in den Dialogen, Ausweis statt Hinweistext (Protokoll, 24.09.2026)

Statuszeile #462 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Etappe E9 (Teil b — V‑E) des
Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 5 Zeile E9: „±-Knopf an drei neuen Orten … Hinweistext entfällt"); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.11.4 (V‑E), § 2.11.5 (Tafel, Regeln „Pflege" und Ausweis) und § 2.11.7 (Hinweistext); Szenarienkonzept
[`Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md`](../../../aktuell/Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md)
§ 4 und § 11; Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Kategorie 8 (Zonen „Dialog — Parameter"
und „Was ist angenommen?", Ressourcentafel) und Anhangzeilen U10 und U15. Entscheide: V‑G5 (R‑V, 31.08.2026: vollständige
Abdeckung), V‑4 (R‑V, 18.09.2026: nach der Darstellungsetappe, bis dahin mit Hinweistext), A5 (R‑A, 20.09.2026: ohne
Degradation) und A14 (R‑A, 20.09.2026: Wortlaut des Hinweistexts) im
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md),
die fünf Fragen dieser Welle unter R‑E9b. Anlass: der Anwender, „Fahre fort" (24.09.2026) — der zweite Teil der Etappe
E9: die Pflege der Best/Worst-Paare aus E9a (#461) in den Dialogen, der Wegfall des Hinweistexts und der Ausweis „n von
m Parametern szenariert"; **damit ist E9 (V‑E) abgeschlossen**. Vorgänger:
[`E9a_Szenarioabdeckung_Kern_Protokoll.md`](E9a_Szenarioabdeckung_Kern_Protokoll.md). Zweig `e9b` von `0d296ca0`
(`origin` nach dem Push #461, Schemastand 118), zwei Phasen; Phase 1: `8e3e9357` (E9b/1), `6a7f738e` (E9b/2),
`5841cd7c` (E9b/3), `8ed0f1a5` (E9b/4), `a4bcb41e` (E9b/5), `6d0fef5f` (E9b/6); Nachzug: Merge `f56cdab4` (`origin` =
`b7572d42`, Dialog Design #458 Stufe 3a/3b); Phase 2: `4eeab727` (E9b/7) und `018520f8` (E9b/8); Endstand des Zweigs
`a605c803`. Merge `75d45630` über `origin` = `f06c8c9e` (Kühlung KU2 Welle 3 mit Schemaschritt 119); der
Baum gleicht dem Endstand des Zweigs. Der erste Merge `9fbac8c6` auf dem Hilfszweig `pm9` über `b7572d42` (51 Dateien,
+5.003/−262, Baum = `018520f8`) ist durch das Vorrücken von `origin` überholt. Opus 5.5 im Worktree
`.claude/worktrees/e9b`. **Kein Schemaschritt aus E9b, keine Rechenwirkung ohne Pflege** — nach dem Merge steht
`SchemaStand.Zielversion` auf 119 (die Kühlung KU2); die Rechenwege stehen seit E9a, E9b bringt die Eingabestellen und
den Ausweis.

## Befund vor der Welle

- **Der Kern las die Paare, gepflegt werden konnten sie nicht** (E9a, #461): Betrachtungszeitraum, Mengenänderung,
  Trägerpreise und Erlössätze je Szenario rechneten, sobald sie in der Datenbank standen; eine Eingabestelle führte
  keiner der Dialoge, und die Wiki-Quelle sagte es so.
- **Der ±-Knopf stand nur an den Kostenpositionen** (`CaseEingabeDialog`, Hilfekennung `Form_CaseEingabe`; Befund P5 des
  Analysepapiers: „nicht an Trägerpreisen, Erlösfeldern, Rahmen"). Die Szenariotafel des Parameterdialogs führte sieben
  Zeilen; „Vorgaben" ersetzte beide Szenariosätze als Ganzes — mit den neuen Größen hätte der Knopf eine Pflege gelöscht,
  die an dieser Stelle niemand sieht.
- **Der Hinweistext** `WIRT_SZEN_HINWEIS` (V‑4, gebaut #434) stimmte seit E9a nur noch für Projekte ohne Pflege der neuen
  Größen (Befund 7 aus E9a); Konzept § 2.11.7: „Der Hinweis entfällt mit der Etappe, die ihn überflüssig macht."
- **Ein Szenariopreis ohne Erwartet-Preis** rechnet in Günstig und Ungünstig, Erwartet zeigt die Datenlücke (Befund 3
  aus E9a) — bis E9b ohne Kennzeichen.
- **Punkt 9 der Anhang-E-Checkliste** stand auf „teilweise" mit dem Satz „… Betrachtungszeitraum und Mengen bleiben
  unverändert" (`WIRT_AE_9_TEILWEISE`) — seit E9a falsch.

## Gebaut — Phase 1 (E9b/1 bis E9b/6)

- **Szenariotafel Zeilen 8 und 9 (E9b/1, `8e3e9357`).** Parameterdialog, Abschnitt „Szenarien": Zeile 8
  **Betrachtungszeitraum** — die Erwartet-Spalte zeigt T, Best und Worst sind **Ganzzahlfelder** von 1 bis 50 a wie das
  Feld T (Abweichung 1) —, Zeile 9 **Mengenänderung** — Erwartet „0 %", Best und Worst als Zahlenfeld in Prozent. Ohne
  Vorgabe (E9a‑Q5): leer heißt „wie Erwartet", das Feld zeigt den gepflegten Wert, der Platzhalter den Erwartungswert;
  gespeichert über `SzenarioSatz.Zeitraum` und `.Menge`. Kern: `SzenarioSatz.TafelZuruecksetzen` (neun Größen je Satz,
  die Einspeisevergütungen bleiben) und `Nachweis(p, kultur, nurGepflegt)` — die Herleitungszeilen des Dialogs nennen
  Zeitraum, Menge und Einspeisevergütungen nur, wenn gepflegt; Seite und Bericht unverändert. „Vorgaben" leert alle
  achtzehn Felder der Tafel. KI-Sicht und Feldkarte `best_zeitraum`, `worst_zeitraum`, `best_menge`, `worst_menge`;
  Maskenwache `WirtschaftlichkeitParameterDialog` 26 → 30 im Block „ETAPPE E9b". `WPAR_SZ_HINWEIS` ergänzt: keine Vorgabe,
  ein längerer Zeitraum ist nicht von selbst der günstigere, „Vorgaben" leert achtzehn Felder.
- **±-Knopf an den Trägerpreisen, der `CaseEingabeDialog` als Baustein (E9b/2, `6a7f738e`; E9b‑Q1, Lesart a).** Der
  Dialog wird zum allgemeinen Baustein „Szenariopaar" (Beschriftung, Erwartet-Wert, Einheit, Best/Worst,
  Nachkommastellen, Grenzen, Kohärenzzeilen, Warnung ohne Erwartet-Wert, Eingabe absolut oder in Prozent vom
  Erwartet-Wert); ohne `Szenariopaar="true"` bleibt die Kostenposition Zeichen für Zeichen (Felder, Grenzen, Rundung,
  Hilfeschlüssel `Form_CaseEingabe`) — die `CaseEingabeDialogTests` unverändert grün. Neu `Szenariopaar.cs` (Parametersatz,
  Nullregel, Kurztext, Texte `SZP_*`) und der Knopf-Baustein `SzenarioKnopf` (± mit dem Gegenstand, Kennzeichen ● bei
  gepflegtem Paar, Warnzeichen ⚠, `aria-label`, gesperrt wie das Feld daneben). An der **Trägerkarte im Projekt** je Preis
  (Arbeit, Grund, Leistung) ein ±-Knopf; die Überlagerung im `EnergietraegerDialog`; geschrieben mit der Karte über
  `EnergietraegerPreisCtrl.SzenarioSchreiben`; der Arbeitspreis folgt der Preisbasis wie das Feld daneben (Umschalten auf
  €/kWh zieht die Szenariowerte mit); im Katalog keine Knöpfe. Kohärenz: eine gepflegte Staffel oder Saisonreihe → der
  Szenario-Leistungspreis bleibt ohne Wirkung (E9a‑Q3); kein Erwartet-Preis → Warnzeichen und Zeile, gespeichert wird
  trotzdem (E9b‑Q4, Lesart a). Neue Hilfekennung `Form_WirtschaftlichkeitSzenariowerte.btn_Help` →
  `Wirtschaftlichkeit#szenariowerte`; kein neues Prüfmuster (Abweichung 3). KI: `CaseEingabeKiSicht` verallgemeinert
  (Größe, Erwartet-Wert und Einheit nur lesbar), Katalog `Form_CaseEingabe` 7 → 10 Felder.
- **±-Knopf an den Erlössätzen (E9b/3, `5841cd7c`).** Derselbe Baustein, jeweils dort, wo der Erwartet-Wert gepflegt wird:
  **Einspeisevergütung PV** im Parameterdialog, Gruppe „Strom" (`SzenarioSatz.Einspeiseverguetung`), Kohärenzzeilen nach
  E9a‑Q7 bei aktivem Tarif-Rollenmodell (`WIRT_SZ_ROLLEN_EINSPEISUNG`) oder aktivem PV-Vergütungsdialog
  (`WIRT_SZ_PV_DIALOG_EINSPEISUNG`); **Einspeisevergütung KWK** im Dialog „BHKW-Wirtschaftlichkeit", Gruppe KWK-Zuschlag —
  dort wird ihr Erwartet-Wert seit #325 gepflegt, der Parameterdialog führt kein KWK-Feld (Abweichung 2) —, geschrieben
  erst mit OK des BHKW-Dialogs, Kohärenzzeile bei wirksamem Rollenmodell; **DV-Entgelt** und **PPA-Preis** im
  PV-Vergütungsdialog, Gruppe Vermarktung (`DvEntgeltBest/Worst`, `PpaPreisBest/Worst` am Modell) — die Knöpfe folgen der
  Sperre ihres Feldes (DV-Entgelt nur bei Marktprämie, PPA-Preis nur bei sonstiger Direktvermarktung), Kohärenzzeilen bei
  nicht angewendeter Vergütung (`SZP_PV_INAKTIV`) und anderer Vermarktungsform (`SZP_PV_DV_OHNE_WIRKUNG`,
  `SZP_PV_PPA_OHNE_WIRKUNG`). Esc gehört der Überlagerung, solange sie steht.
- **Der Hinweistext entfällt, der Ausweis kommt (E9b/4, `8ed0f1a5`; E9b‑Q2 und E9b‑Q3, je Lesart a).** Kern
  `SzenarioAbdeckung` mit `Zaehle` (ohne Datenbank) und `Lesen` (Gruppe aus der Datenbank) — die Zählregel im nächsten
  Abschnitt. `WirtschaftlichkeitBewertung`: `Szenariohinweis` → `Szenarioabdeckung`/`Abdeckung`, der U10-Kommentar
  nachgezogen, `ValeriAusweis.Szenariohinweis` entfernt. Der Satz steht auf der Seite unter der Annahmentafel und in
  Block 4 (CSS `epos-wirt-szenarioabdeckung`), im Wort- und im Tabellenbericht an der Stelle des Hinweistexts und in
  Punkt 9 der Anhang-E-Checkliste (`WIRT_AE_9_ABDECKUNG`; der Stand bleibt „teilweise", E9b‑Q5). `WIRT_SZEN_HINWEIS` ist
  aus beiden Ressourcen und dem Designer entfernt; `WIRT_AE_9_TEILWEISE` nennt jetzt „drei vollständige Läufe, je
  Szenario mit eigenem Parametersatz."
- **Nachbesserungen (E9b/5, `a4bcb41e`).** Die Zeile der ±-Knöpfe trägt die eigene CSS-Klasse `epos-szenarioleiste`
  statt `epos-leiste` (Abweichung 4); im Hochkontrast steht das Warnzeichen in der Linkfarbe statt einer festen Farbe; im
  Szenariopaar lehnt der Assistent Nutzungsdauer, Startjahr und Zuschuss mit Grund ab (`KI_DLG_CSE_NUR_KOSTEN`,
  Abweichung 5).
- **Tests (E9b/6, `6d0fef5f`) — 71 neue Testmethoden.** bUnit: `SzenarioKnopfTests` (7), `CaseEingabeSzenariopaarTests`
  (15), `EnergietraegerSzenarioTests` (10), dazu 9 in `WirtschaftlichkeitParameterDialogTests` (neun Zeilen, 18 Felder,
  Zeilen 8/9 leer = wie Erwartet, „Vorgaben", Herleitungszeile, ±-Knopf der Einspeisevergütung PV, KI-Felder), 5 in
  `BhkwWirtschaftlichkeitDialogTests` und 4 in `PhotovoltaikVerguetungDialogTests`. Kern: `SzenarioAbdeckungTests` (15 —
  Zählregel m/n, Vorgabe zählt nicht, Nullregel, DV/PPA je Zeile, Trägerpreise nach der einen Regel, Satz de/en, Punkt 9,
  `TafelZuruecksetzen`, `Nachweis(nurGepflegt)`, „Hinweistext weg, Ausweis da", Lesen am Projekt 1030) und
  `EnergietraegerSzenarioHuelleTests` (6 — öffnen, speichern, öffnen in Nm³ und kWh, Speichern ohne Pflege lässt stehen,
  Katalog ohne, Staffelzeile). Tests, die den Hinweistext prüften, sind auf den Ausweis umgestellt.
- **Ressourcen** (de und en): **38 neu** — Parameterdialog `WPAR_SZ_ZEITRAUM`, `WPAR_SZ_MENGE`; Assistent
  `KI_DLG_WPA_SZ_ZEITRAUM_ERL`, `KI_DLG_WPA_SZ_MENGE_ERL` und sieben `KI_DLG_CSE_*` (Größe, Erwartet-Wert und Einheit je
  Name und Erläuterung, `NUR_KOSTEN`); der Knopf-Dialog 15 × `SZP_*` und `SZP_PV_INAKTIV`, `SZP_PV_DV_OHNE_WIRKUNG`,
  `SZP_PV_PPA_OHNE_WIRKUNG`; die Trägerkarte `ETV_SZ_TITEL`, `ETV_SZ_HINWEIS`, `ETV_SZ_OHNE_ERWARTET`,
  `ETV_SZ_SPEICHERFEHLER`; Ausweis und Bericht `WIRT_ANN_DV_ENTGELT`, `WIRT_ANN_PPA_PREIS`, `WIRT_SZ_ABDECKUNG`,
  `WIRT_SZ_ABDECKUNG_LISTE`, `WIRT_AE_9_ABDECKUNG`. **1 entfallen:** `WIRT_SZEN_HINWEIS`. **2 geändert:**
  `WPAR_SZ_HINWEIS`, `WIRT_AE_9_TEILWEISE`. Je Sprache 9.020 Einträge (8.983 von `origin` + 38 − 1); Designer neu erzeugt
  und wiederholbar.

## Die Zählregel des Ausweises (E9b‑Q2, Lesart a)

`SzenarioAbdeckung.Zaehle` zählt je Vergleichsgruppe. `m` sind die Parameter, `n` die szenarierten — gepflegt heißt:
Best oder Worst weicht um mehr als 1e−9 vom Erwartet-Wert ab. Ein gezählter Wert trägt damit immer eine Abweichung.

| Parameter | zählt in m | szenariert (n), wenn |
|---|---|---|
| die sieben Größen des W5‑B‑9-Satzes (Zins, p_E, p_B, p_I, Investitions-, Ertrags-, Nutzungsdaueränderung) | immer | das Feld eingetragen ist und abweicht — die Vorgabe (leeres Feld) ist keine Pflege, eine eingetragene 0 zählt, wenn sie abweicht |
| Betrachtungszeitraum, Mengenänderung | immer | nach der Nullregel des Kerns (leer oder 0 = wie Erwartet) |
| Einspeisevergütung PV und KWK | immer — die KWK-Vergütung auch ohne BHKW | nach der Nullregel; eine leere KWK-Vergütung von Erwartet gilt als 0 |
| DV-Entgelt und PPA-Preis | je Vergütungszeile eines Standes mit PV-Anlage; eine übernommene Zeile zählt einmal, bei dem Stand, dem sie gehört | nach der Nullregel |
| Arbeits- und Grundpreis | je Träger mit Verbrauch und Stand (die Träger der Anlagen, dazu der Stromträger, wenn der Stand Strom bezieht) | nach `TraegerpreisSzenario.Wirksam` — ein Szenariopreis gleich dem Erwartet-Preis zählt nicht |
| Leistungspreis | beim Stromträger immer, sonst nur, wo ein Leistungspreis gepflegt ist (Erwartet oder je Szenario) — so bleibt n ≤ m | wie Arbeits- und Grundpreis |

Der Satz lautet „n von m Parametern szenariert" (`WIRT_SZ_ABDECKUNG`), mit gepflegten Größen „…: ‹Liste›"
(`WIRT_SZ_ABDECKUNG_LISTE`) in der Reihenfolge der Zählung, bei mehreren Ständen mit dem Stand hinter dem Träger; ohne
Parametersatz steht kein Ausweis. Beispiel eines Testfalls (ein Stand mit Strom und Erdgas E, ohne PV): „2 von 16
Parametern szenariert: Betrachtungszeitraum, Arbeitspreis Erdgas E". Am Projekt 1030 zählt `Lesen` die Grundmenge 11,
Strom mit drei und Erdgas E mit zwei Preisen; m bleibt bei jeder Pflege gleich, ein doppelt genannter Stand zählt einmal.
**Zwei Lesarten zum Mitentscheiden** (E9b‑Q2): n zählt nur gepflegte Werte — ohne Pflege steht „0 von m", obwohl die
Vorgaben der sieben Tafelgrößen Günstig und Ungünstig verschieben; und die Einspeisevergütung KWK zählt auch ohne BHKW.

## Die Orte der Pflege

| Größe | Ort | geschrieben mit | Kohärenzzeile |
|---|---|---|---|
| Zins, p_E, p_B, p_I, Investition, Erträge, Nutzungsdauer | Szenariotafel des Parameterdialogs, Zeilen 1 bis 7 (Vorgaben) | „Speichern" des Parameterdialogs | — |
| Betrachtungszeitraum | Szenariotafel, Zeile 8 (Ganzzahlfeld 1–50 a, Platzhalter T) | „Speichern" des Parameterdialogs | — |
| Mengenänderung | Szenariotafel, Zeile 9 (Prozent, Platzhalter 0) | „Speichern" des Parameterdialogs | — |
| Arbeits-, Grund- und Leistungspreis | Trägerkarte im Projekt, ± je Preis (im Katalog keine Knöpfe) | der Karte | Leistungspreis neben Staffel oder Saisonreihe ohne Wirkung; ⚠ ohne Erwartet-Preis |
| Einspeisevergütung PV | Parameterdialog, Gruppe „Strom", ± am Feld | „Speichern" des Parameterdialogs | Rollenmodell aktiv; PV-Vergütungsdialog aktiv |
| Einspeisevergütung KWK | Dialog „BHKW-Wirtschaftlichkeit", Gruppe KWK-Zuschlag, ± am Feld | OK des BHKW-Dialogs | Rollenmodell aktiv |
| DV-Entgelt, PPA-Preis | PV-Vergütungsdialog, Gruppe Vermarktung — DV-Entgelt nur bei Marktprämie, PPA-Preis nur bei sonstiger Direktvermarktung | dem PV-Vergütungsdialog | Vergütung nicht angewendet; andere Vermarktungsform |

Die „Rahmen-Gruppe" des Konzepts § 2.11.5 (Zins, Preissteigerungen, Betrachtungszeitraum) hat keinen eigenen ±-Knopf; sie
wird über die Szenariotafel gepflegt, Zeilen 1 bis 4 und 8 (Abweichung 6).

## Fragen aus der Welle

Der Phase‑1-Bericht nennt vier Fragen und eine Folgefrage. **Alle fünf sind beim Anwender offen**; gebaut ist jeweils
Lesart a. Sie stehen im Entscheidungsregister als **R‑E9b**.

| Frage | Lesarten | Empfehlung | Stand |
|---|---|---|---|
| **E9b‑Q1** Bauform des ±-Knopfs | (a) der verallgemeinerte `CaseEingabeDialog` mit Knopf-Baustein und gemeinsamem Parametersatz — ein Muster, ein Hilfeschlüssel, die Kostenposition unverändert; (b) eigene Dialoge je Ort | a | offen; gebaut ist a |
| **E9b‑Q2** Zählregel m | (a) die sieben Größen der Tafel, Zeitraum, Menge, Einspeisevergütung PV und KWK; DV-Entgelt und PPA-Preis je Vergütungszeile mit PV-Anlage; je Träger mit Verbrauch Arbeits- und Grundpreis, der Leistungspreis beim Stromträger immer, sonst nur, wo einer gepflegt ist; (b) nur die projektweiten Größen (m = 11). Lesarten: n zählt nur gepflegte Werte („0 von m" ohne Pflege, obwohl die Vorgaben der Tafel die Szenarien verschieben); die Einspeisevergütung KWK zählt auch ohne BHKW | a | offen; gebaut ist a |
| **E9b‑Q3** Ort des Ausweises | (a) unter der Annahmentafel (Seite und Block 4), in Wort- und Tabellenbericht und in Punkt 9 der Anhang-E-Checkliste; (b) im Kopf der Ergebnisseite | a | offen; gebaut ist a |
| **E9b‑Q4** Szenariopreis ohne Erwartet-Preis | (a) Warnzeichen am Knopf, Kohärenzzeile, Warnband im Dialog — gespeichert wird trotzdem; (b) Speichern verweigern. Grundpreis und Erlössätze warnen nie, 0 ist dort ein gültiger Wert | a | offen; gebaut ist a |
| **E9b‑Q5** Punkt 9 der Checkliste | (a) bleibt „teilweise"; (b) „erfüllt", sobald beide Szenarien gerechnet sind — der Grund für „teilweise" war der feste Zeitraum und die festen Mengen, und das löst E9 auf; (c) „erfüllt" nur mit mindestens einer Pflege | b | offen; gebaut ist a |

**Mit dieser Welle erledigt** (unter dem Vorbehalt der Abnahme A‑E9‑1): V‑G5 (R‑V) — alle Parameterklassen aus § 2.11.5
tragen Best/Worst-Werte, gepflegt in den Dialogen; V‑4 (R‑V) — der Zeitpunkt „danach" ist umgesetzt, der Hinweistext aus
#434 ist entfallen; Mockup U15 erledigt, U10 entfallen (der U10-Kommentar in `WirtschaftlichkeitBewertung` nachgezogen);
Konzept § 2.11.7 — „Der Hinweis entfällt mit der Etappe, die ihn überflüssig macht": das ist E9b. Vom Befund P5 des
Analysepapiers ist der Teil „Szenario-±-Knopf" erledigt.

## Nachweis „keine Rechenwirkung ohne Pflege"

- **E9b berührt keinen Rechenweg** — gebaut sind Eingabestellen, der Ausweis und Berichtstexte; die Lesestellen je Größe
  stehen seit E9a, ihre Rechenwirkung je Pflege belegt die A/B-Tafel des E9a-Protokolls (neun Größen, Erwartet
  bitgleich).
- **Anker:** `WirtschaftlichkeitAnkerTests` im vollen Lauf unverändert grün.
- **Referenzlauf** 13/13 PASS gegen `2026-09-23_R13_Kuehlung`, 4.145.687 Werte in der Toleranz, 387/387 CSV byte-gleich —
  die Referenzprojekte tragen keine Pflege.
- **In den Ergebnissen sichtbar ohne Pflege** ist allein der Ausweis: „0 von m Parametern szenariert" an der Stelle des
  Hinweistexts. Die
  Blattstruktur-Wache hält die Excel-Ankerzeile unter dem Fußtext der Bandbreite fest — dieselbe eine Zeile, jetzt mit
  dem Ausweis (E9b/8); die Anker darunter bleiben.
- **Maskenwache** grün mit 84 Eingabestellen: `WirtschaftlichkeitParameterDialog` 30 im Block „ETAPPE E9b"; der
  Knopf-Dialog (`CaseEingabe`) behält 7 Eingaben und bekommt drei nur lesbare KI-Felder; Trägerkarte (21),
  Energieträgerdialog (4), BHKW-Dialog (39) und PV-Dialog (16) unverändert — die ±-Knöpfe sind keine Eingaben.
  **Formularkarte** 124/124.

## Phase 2 und der Merge

- **Merge `f56cdab4`** holt `origin` = `b7572d42` (Dialog Design #458 Stufe 3a/3b: Zapfprofil-Dialoge, Feldtyp
  Zahlenreihe, Aktion `reihe_setzen`). Ohne Konflikte; auf beiden Seiten geändert waren `KiDialoge.cs`,
  `KiDialogTexte.cs`, beide `.resx` und der Designer, `KiDialogkatalogTests` und `KiMaskenabdeckungWacheTests`. Je
  Sprache 9.020 Einträge, keiner doppelt, jede Naht `</data>` geschlossen; der Designer unverändert und wiederholbar; die
  Maskenwache mit 84 Eingabestellen, der Block „ETAPPE E9b" unverändert.
- **E9b/7 (`4eeab727`):** Zwei neue BHKW-Fälle erwarteten fälschlich `null` statt eines leeren KWK-Paars; der
  Parametersatz trägt `SatzBest`/`SatzWorst` von Anfang an mit Vorgaben. Geprüft wird jetzt `EinspeiseverguetungKwk` der
  Sätze — bis zum OK unberührt, nach Abbrechen leer.
- **E9b/8 (`018520f8`):** Die Blattstruktur-Wache erwartet in der Excel-Ankerzeile den Ausweis statt „Was ein Szenario
  heute variiert".
- **Tests auf `018520f8`:** Kern-Filter mit UI 0 Fehler, Windows-Schale 0 Fehler; voller Lauf 0 Fehler, **12.500
  bestanden, 1 übersprungen** (EPOS.Kern 5.685, EPOS.UI 5.860, KiKern 542, SpeicherEngine 386, SpeicherPlanung 27 und
  1 übersprungen); gefiltert die neuen Klassen 58/58 (Kern) und 81/81 (UI), Wachen und Nachbarklassen 281/281 und 428/428;
  Formularkarte 124/124; Maskenwache grün (84 Eingabestellen); Referenzlauf 13/13 gegen R13; SQL-Prüfer 1.763 Texte,
  0 Fundstellen; Designer wiederholbar.
- **Erster Merge `9fbac8c6`** auf dem Hilfszweig `pm9` über `origin` = `b7572d42`: 51 Dateien, +5.003/−262; der Baum
  gleicht `018520f8` byte-genau — überholt, weil `origin` auf `f06c8c9e` vorgerückt ist (Kühlung KU2 Welle 3 mit
  Schemaschritt 119 und neuer Testdatenbank). **Merge `75d45630`** über `origin` = `f06c8c9e`; der Endstand
  des Zweigs `e9b` ist `a605c803`.

## Abweichungen und Befunde

1. **Zeile 8 ist ein Ganzzahlfeld**, kein Zahlenfeld: Der Zeitraum ist eine ganze Zahl von Jahren (1 bis 50 wie das
   Feld T); Zeile 9 zeigt in der Erwartet-Spalte „0 %".
2. **Der ±-Knopf der Einspeisevergütung KWK steht im Dialog „BHKW-Wirtschaftlichkeit"**, nicht im Parameterdialog — dort
   wird ihr Erwartet-Wert seit #325 gepflegt, der Parameterdialog zeigt kein KWK-Feld; geschrieben wird mit OK des
   BHKW-Dialogs.
3. **Kein neues Prüfmuster:** Prüfmuster sind eingefrorene WinForms-Vorgänger, die neue Verwendung hat keinen. Ersatz:
   ein Test, dass jeder Schlüssel des Knopf-Parametersatzes einen `[Parameter]` trifft, und die neue Hilfekennung.
4. **Eigene CSS-Klasse der Knopfzeile** (`epos-szenarioleiste`): Mit `epos-leiste` wären die OK/Abbrechen-Zugriffe der
   BHKW-Tests gekippt, und die Fußleistenwache zählt jede `epos-leiste`; das Maß bleibt dasselbe.
5. **Assistent im Knopf-Dialog:** Nutzungsdauer, Startjahr und Zuschuss lehnt er im Szenariopaar mit Grund ab
   (`KI_DLG_CSE_NUR_KOSTEN`), statt einen Wert still zu verwerfen.
6. **Die „Rahmen-Gruppe" des Konzepts § 2.11.5** wird über die Szenariotafel gepflegt (Zeilen 1 bis 4 und 8), nicht über
   einen eigenen ±-Knopf — so verlangt es der Auftrag; das Konzept ist mit den Papieren zu #462 nachgezogen.

**Zu den Lesarten der Zählregel** siehe oben (E9b‑Q2). **Merge-Vorschau aus Phase 1:** `origin` war zwölf Commits weiter
(#458 Stufe 3a/3b), `git merge-tree` meldete keinen Konflikt — so kam es beim Nachzug `f56cdab4`.

## Zahlen und Abnahme

- **Phase 1** (Worktree `e9b`): Kern-Filter nach jedem Commit 0 Fehler (die Warnungen aus Dateien, die E9b nicht
  anfasst); Windows-Schale 0 Fehler (13 Warnungen, Bestand); Designer neu erzeugt, die Prüfung meldet „unverändert" — kein
  `dotnet test`, kein Referenzlauf.
- **Phase 2** (auf `018520f8`): voller Lauf 0 Fehler, **12.500 bestanden, 1 übersprungen**; gefiltert 58/58 und 81/81,
  281/281 und 428/428; Formularkarte 124/124; Maskenwache 84; Referenzlauf 13/13 gegen R13 (387 CSV byte-gleich,
  4.145.687 Werte); SQL-Prüfer 1.763/0; Designer wiederholbar.
- **Erstes Gate auf `9fbac8c6`** (der erste Merge, Worktree `pm9`, 05:46–05:51, Log `GATE462.log`), grün: Kern-Filter
  0 Fehler; ChartProben 146 Bilder, alle grün, Hashes gleich der Windows-Messlatte (146/146; E9b bringt kein Bild); voller
  Lauf 0 Fehler, **12.500 bestanden, 1 übersprungen** (EPOS.Kern 5.685, EPOS.UI 5.860, KiKern 542, SpeicherEngine 386,
  SpeicherPlanung 27 und 1 übersprungen); Dokumentationswachen 26/26. Referenzlauf 13/13 gegen R13 und SQL-Prüfer
  1.763/0 aus dem Agentenlauf auf dem byte-gleichen Baum `018520f8`.
- **Zweites Gate auf `75d45630`** (über `origin` = `f06c8c9e`): Build 0 Fehler, ChartProben 146/146 gleich der Windows-Messlatte, voller Lauf 12.532 bestanden / 0 Fehler / 1 übersprungen (EPOS.Kern 5.700, EPOS.UI 5.877, KiKern 542, SpeicherEngine 386, SpeicherPlanung 27+1), Dokumentationswachen 26/26 (Log GATE462b).
- **Schema:** E9b bringt keinen Schritt; nach dem Merge `SchemaStand.Zielversion` = 119 (Kühlung KU2 Welle 3,
  `SCHRITT_119_KAELTESTROM`, Entscheid E34 — nicht aus E9b), der nächste freie Schritt ist 120. Testdatenbank: die der
  Kühlung (LFS `6259b348`, Stand 119); E9b ändert sie nicht.

## Abnahme am Gerät (A‑E9‑1, Windows und iPad)

1. **Parameterdialog, Szenariotafel:** neun Zeilen. Zeitraum und Menge sind leer mit Platzhalter T bzw. 0. Einträge
   erscheinen in der Herleitungszeile. „Vorgaben" leert 18 Felder und lässt die Einspeisevergütungen stehen.
2. **± Einspeisevergütung PV:** Erwartet-Zeile im Dialog; nach OK Kennzeichen ● und Kurztext mit den Werten. Die
   Kohärenzzeile erscheint bei aktivem Rollenmodell oder aktivem PV-Vergütungsdialog. Esc schließt nur die Überlagerung.
3. **BHKW-Dialog:** ± Einspeisevergütung KWK; geschrieben wird erst mit OK.
4. **Trägerkarte im Projekt:** ± je Preis. Beim Umschalten auf €/kWh folgen die Szenariowerte. Stromträger mit Staffel:
   Zeile „ohne Wirkung". Ohne Erwartet-Preis: ⚠, gespeichert wird trotzdem. Im Katalog keine Knöpfe.
5. **PV-Dialog:** ± DV-Entgelt nur bei Marktprämie, ± PPA-Preis nur bei sonstiger Direktvermarktung.
6. **Ergebnisseite:** „n von m Parametern szenariert: …" unter der Annahmentafel und in Block 4; Checkliste Punkt 9
   nennt den Ausweis.
7. **Wort- und Excelbericht:** derselbe Satz in der Annahmentafel, kein Hinweistext.
8. **Rechnen:** Ein Projekt mit Pflege zeigt geänderte Kapitalwerte Günstig/Ungünstig bei unverändertem Erwartet; nach
   dem Leeren wieder die alten Werte.

## Logbuch

Mit #462 stehen im Update-Papier (Version 1.2.0.4, Sammel-Upload 26.09.2026) fünf Sätze — die zwei aus E9a
zurückgestellten und drei aus E9b; der Satz zum Rechenkern aus #461 ist vor der Veröffentlichung durch den ersten
ersetzt (ein Thema, ein Eintrag):

- *szenarien (E9a):* „Die Szenarien Günstig und Ungünstig können einen eigenen Betrachtungszeitraum, eine
  Mengenänderung, eigene Energieträgerpreise sowie eigene Einspeisevergütungen, DV-Entgelte und PPA-Preise führen."
- *wirtschaftlichkeit (E9a):* „Bericht und Formelmappe nennen je Szenario Betrachtungszeitraum, Mengenänderung,
  Einspeisevergütungen und gepflegte Energieträgerpreise."
- *szenarien:* „Die Szenariotafel im Dialog ‚Parameter' führt zusätzlich Betrachtungszeitraum und Mengenänderung je
  Szenario."
- *szenarien:* „Arbeits-, Grund- und Leistungspreis der Energieträger, die Einspeisevergütungen sowie DV-Entgelt und
  PPA-Preis tragen einen ±-Knopf für ihre Werte je Szenario."
- *wirtschaftlichkeit:* „Unter der Annahmentafel, im Wort- und im Excelbericht steht statt des Hinweistexts der Ausweis
  ‚n von m Parametern szenariert' mit den gepflegten Größen."

Aus zwei Sätzen zu #434 derselben Version (Sensitivitätstafel; Wort- und Tabellenbericht) ist der Teil zum Hinweistext
gestrichen — beide sind noch nicht veröffentlicht, der Hinweistext erreicht so kein veröffentlichtes Logbuch. Der
letzte Satz oben nennt ihn trotzdem („statt des Hinweistexts"), wie der Bericht ihn formuliert; ob er ohne diesen
Bezug stehen soll, klärt der Anwender mit der Versionsnummer.

## Befunde nebenbei

- **CI-Nachweise zu #461** (Push `0d296ca0`): Kern-Lauf `ios_migration_september` 35945134055, Kern-Lauf `main`
  35945142392 und Windows-Lauf `main` 35945142208 — alle grün; nachgetragen in Nach #461 (h). Der CI-Nachweis zu #462
  steht aus — der Push folgt nach dem Gate.
- **Wiki-Quelle Wirtschaftlichkeit veraltet** (aus dem Phase‑1-Bericht): der Satz „Ein Eingabefeld dafür führt keiner der
  Dialoge", der Punkt zum entfallenen Hinweistext (Anker `szenariohinweis`), die Szenariotafel mit sieben Zeilen,
  Trägerkarte, PV- und BHKW-Dialog ohne ±-Knöpfe — mit den Papieren zu #462 nachgezogen; der Anker heißt jetzt
  `szenarioabdeckung` (er war noch nicht veröffentlicht).
- **Linux-Messlatte der ChartProben:** `Proben/ChartProben/Messlatte_2026-09-20.sha256` im Repository hat 91 Zeilen, die
  Windows-Messlatte des Gates 146 — Nachtrag beim nächsten Linux-Lauf.
- **Testdatenbank ohne PV-Projekt mit vollständigen Preisen** (aus E9a) — eine eigene Datenaufgabe.
- **Ein einmal roter Oberflächentest, nicht aus E9b:** Im ersten vollen Lauf fiel
  `EinstellungenDialogTests.Die_Diagrammrubrik_zeigt_alle_Farbrollen_in_ihren_Gruppen` einmal aus („Wärmepumpe" fehlte
  im Markup). Nicht wiederholbar: allein 44/44, die UI-Suite 5.860/5.860, der zweite volle Lauf grün, `b7572d42` zum
  Vergleich 5.810/5.810. Vermutete Ursache: Die Namensliste `Diagrammfarben._namen` ist `static readonly` und liest die
  Rollennamen beim ersten Zugriff auf den Typ in der gerade gültigen Sprache — läuft zuerst ein Test in Englisch, hält
  sie die englischen Namen fest. E9b berührt den Code nicht; behoben von Dialog Design (Diagrammfarben-Fix 589e306f: Namen je Kultur nachgeschlagen, Test pinnt die Kultur), enthalten im Nachzug 0e819226 — auf dem Endstand grün.

## Offen

- **Die fünf Fragen** E9b‑Q1 bis E9b‑Q5 beim Anwender (Empfehlung a/a/a/a/b; gebaut jeweils a); offen außerdem
  E7c3‑Q1 bis E7c3‑Q8, E8c‑Q1 und E8c‑Q2 und E9a‑Q1 bis E9a‑Q7.
- **Abnahme am Gerät** A‑E9‑1 (acht Schritte oben); A‑E9a‑1 bleibt offen — ihre SQL-Pflege auf einer Kopie lässt sich
  jetzt durch Zeile 8 der Szenariotafel ersetzen.
- **Nächste Etappe: E10** (Analysepapier § 5, voraussichtlich #463) — Nutzungsdauer S3 und Speicherflotte: Instandsetzung
  und Wartung je Technik aus den vorhandenen Spalten, Gerätekataloge, die Speicherflotte an `Tab_Nutzungsdauer` mit
  Neueinfrieren der Basis (A7), geräteeigene Spalten kennzeichnen (A8); rechenwirksam mit A/B und neuer Referenzbasis;
  Schemaschritt bei Bedarf 120. Voraussetzung ist der eigene Entscheid zu ND‑S3. E11 entfällt, E12 (Wiki-Runden)
  folgt.
- **Push** nach der Regel des Anwenders ohne Rückfrage aus dem Hilfszweig des Merges (`75d45630` und die Papiere) auf
  `ios_migration_september` und `main`; der CI-Nachweis kommt mit der nächsten Papierwelle.
- **Papiere mit der Statuszeile:** Register (Kopf, Familientafel, A5, A14, Q18, R‑V mit V‑4, V‑G2, V‑G5, E6‑Q1, neue
  Familie R‑E9b, EZ‑9, EZ‑10), Konzept (Kopf, § 2.11.2, § 2.11.4, § 2.11.5 Tafel und Regeln, § 2.11.7, § 2.13 (5), § 6.1,
  § 6.2, § 7 und Anhang), Szenarienkonzept (Kopf, § 4, § 5, § 11, § 11.1), Protokoll der Entscheidwege (§ 8.21, § 8.22,
  Kopf von § 8), Analysepapier (Kopf, Nachtrag, § 0 Punkt 4, § 3 P5, § 5 mit der Zeile E9 und dem Stand der Etappen),
  Mockup (Zonen „Dialog — Parameter" und „Was ist angenommen?", Ressourcentafel der Kategorie 8, Anhang U10 und U15,
  Stand-Absatz), Logbuch-Sätze und die Wiki-Quelle der Seite Wirtschaftlichkeit, Index Reporting.

**Entscheide 24.09.2026:** alle nach Empfehlung (E9b‑Q5: b, Bau offen), siehe Register R‑E9b
