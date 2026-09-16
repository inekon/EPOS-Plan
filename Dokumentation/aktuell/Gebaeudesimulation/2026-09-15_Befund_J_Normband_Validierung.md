# Befund J — Prototyp gegen das Normband der VDI 6007 Blatt 1, alle zwölf Testfälle (15.09.2026)

**Protokoll.** Ergebnis des Workflows „normtabellen-validieren" (Modell Opus; zwei unabhängige Leser der Tabellen A1.3–A12.3, Abgleich mit Drittlesung und AixLib-Gegenprobe, Bandvergleich, Kritik) im Auftrag des Konzepts [`../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Sitzung vom 15.09.2026. Die Normzahlen selbst liegen nur außerhalb des Repositoriums (`epos-spike\vdi\norm\`, Entscheid zu Q2: nicht ausliefern); dieser Befund zitiert einzelne Werte als Beleg. Wortlaut der drei Teilberichte wie geliefert.

**Korrekturen aus der Kritik (Abschnitt 3), die vor dem Lesen des Validierungsberichts gelten:** Es sind **30 von 36 Prüfungen bestanden und 6 nicht** (nicht 31/5); fünf der sechs Fehlschläge sind Haarrisse. Die Abweichung des Falls 5 gegen die VDI-6020-Spalte beträgt 0,558 K (nicht 0,458). Die Fälle 8 und 9 führen zwei Orientierungen mit zusammen 14 m² Fensterfläche; hConInt ist 2,24 nur in den Fällen 1–7. Die Einspalten-/Normband-Gegenüberstellung vergleicht unterschiedliche Grundgesamtheiten (72 gegen 216 Zellen je Fall); die Aussage „das Normband ist schärfer" hält gleichbasig. Die Nutzungsprofil-Tabellen der Textfassung sind um eine Zeile versetzt und dürfen nicht zitiert werden.

---

## 1. Abgleich der Lesungen (Stufe Abgleich)

Fertig. Abgleich, Konsolidierung und AixLib-Zuordnung liegen unter `C:\Users\Dirk\AppData\Local\Temp\epos-spike\vdi\norm\`.

## Abgleich der beiden Lesungen — Ergebnis

**Die beiden unabhängigen Lesungen sind in allen 6192 verglichenen Zellen zeichengleich. Null Abweichungen, also kein einziger Entscheid zu fällen.**

| Größe | Zahl |
|---|---|
| verglichene Zellen A gegen B | 6192 |
| Abweichungen | **0** |
| Reihen nur in einem Satz vorhanden | 0 |
| Reihen in beiden Sätzen übereinstimmend `null` (keine VDI-6020-Spalte) | 66 |
| Formfehler im konsolidierten Satz (Reihenlänge 24, Wertebereich, Lasten ganzzahlig) | 0 |
| zusätzliche Drittlesung (eigener Parser) | 6192 Zellen, 0 Abweichungen |
| zusätzlicher AixLib-Abgleich | 864 Zellen, 0 Abweichungen |

Aufbau der 6192: 12 Fälle × 3 Größen × 3 Tage × 2 Programme × 24 Stunden = 5184, dazu der VDI-6020-Block der Fälle 1–7 (Luft und Last × 3 Tage × 24) = 1008.

Der Vergleicher wurde vorab gegen drei absichtlich eingespielte Abweichungen geprüft (Temperatur-, Last- und VDI-6020-Zelle); alle drei wurden gemeldet. Der Nullbefund ist kein Blindlauf.

## Scheinbare Abweichungen aus den beiden Leseberichten

Nur die Berichtstexte weichen ab, die Daten nicht.

| Punkt | Leser A | Leser B | Klärung |
|---|---|---|---|
| Temperaturband Fall 1 | 22,0 … 56,2 | 22,0 … 56,6 | A nennt das Band ohne, B mit VDI-6020-Spalte |
| Temperaturband Fall 5 | 21,9 … 47,6 | 21,7 … 47,9 | dito |
| Seite 55, zweiter Block | „Duplikat wertgleich" | „eine weitere Tabelle" | nachgeschlagen: **A hat recht** — der Ergebnisblock steht zweimal, mit unterschiedlicher Einheitenzeile, aber in allen 24 Zeilen zeichengleich. Nur Seite 55 betroffen, die übrigen 23 Seiten tragen den Block einmal. |
| Ablage der VDI-6020-Werte | zwei Lesarten abgelegt | eine Lesart | konsolidiert auf B (folgt der Zielvorgabe wörtlich); A-interne Doppelablage geprüft, zellgleich |

## Nachgeschlagene Stellen (Textfassung, Seitenmarken)

| Stelle | Befund | Entscheid |
|---|---|---|
| S. 51, Std. 19, Last | dieselbe Zeile setzt den Betrag einmal mit, einmal ohne Tausenderpunkt (`-1.004` neben `-1004`) | Punkt = Tausendertrenner, −1004 W in allen vier Spalten |
| S. 52, Std. 24, Luft P2 | dort `22` statt `22,0`, während P1 in derselben Zeile `22,0` setzt | Satzfehler der Vorlage, 22,0 degC |
| S. 55 | Ergebnisblock doppelt, zeilenweise wertgleich | erster Block |
| S. 58, Überschrift | Extrakt „Tabelle A.10+A88.3" | dieselbe Seite trägt „Tabelle A10.3 / Table A10.3" und „Testbeispiel 10"; Zuordnung bestätigt, Verstümmelung ist Extraktionsartefakt |
| „Empf.Temp." | steht in jedem Ergebnisblock zwischen „Lufttemp." und „Heiz-/Kühllast" | = operative Temperatur |
| VDI-6020-Block | Gruppenzeile „1.Tag 10.Tag 60.Tag 1.Tag 10.Tag 60.Tag", darunter 3× „Lufttemp." und 3× „Heiz-/Kühllast", keine „Empf.Temp." | erste drei Spalten Luft, letzte drei Last, operativ `null` |
| Seitenaufteilung | gerade Seite: Wetterdaten + Ergebnisse 1. Tag P1/P2; ungerade Seite: 10. Tag, 60. Tag, ggf. VDI 6020 | beide Leser haben das erkannt; `quelle`/`seite` nennen weisungsgemäß die ungerade Seite, `seiten` beide |

**Drittlesung:** Weil zwei übereinstimmende Lesungen auch gemeinsam falsch sein können, habe ich mit `vdi/drittlesung.pl` einen dritten, eigenständigen Parser der Textfassung geschrieben (eigene Spaltenherleitung aus der Kopfzeile, eigene Zahlenerkennung). Er reproduziert alle 6192 Zellen ohne Abweichung. Die Falle aus Seite 51 trat dabei erneut auf: ein naiver Ausdruck zerlegt `-1004` in `-100` und `4`, sobald man Tausenderpunkte zulässt — die Zeile fällt dann mit 20 statt 18 Feldern heraus. Beide Leser hatten das unabhängig richtig behandelt.

## AixLib-Zuordnung je Fall

Die AixLib-Reihen liegen als 73 Stützstellen `[Zeit in s, Wert]` vor: t=0 Startpunkt, dann Std. 1–24 des 1. Tages (3600…86400), des 10. (781200…864000) und des 60. Tages (5101200…5184000). **Die Werte stehen dort bereits in degC bzw. W**; die Umrechnung nach K macht AixLib erst über `offset = 273.15` im CombiTimeTable — ein Aufschlag von 273,15 auf die abgelegten Zahlen wäre doppelt gemoppelt. Verglichen wurde deshalb direkt in degC.

| Fall | Referenzgröße | passt zu | max. Abweichung | Vorzeichen gedreht |
|---|---|---|---|---|
| 1–5 | Luft (TAir) | Programm 1 | 0,0 K | nein |
| 6 | Last | Programm 1 | 0 W | **ja** |
| 7 | Last | Programm 1 | 0 W | nein |
| 8–10 | Luft (TAir) | Programm 1 | 0,0 K | nein |
| 11 | Last | **Programm 2** | 0 W | nein |
| 12 | Luft (TAir) | Programm 1 | 0,0 K | nein |

Die Zuordnung ist nicht bloß „innerhalb der Toleranz", sondern in allen 12 × 3 × 24 = 864 Zellen zeichengleich. Trennscharf ist sie dort, wo P1 und P2 auseinanderlaufen: in der jeweiligen Referenzgröße 29 Zellen; AixLib folgt in 28 davon Programm 1 und im Fall 11 in der einen abweichenden Zelle (60. Tag, Std. 10: −121 W gegen −122 W) Programm 2. Über alle drei Größen zählt der Satz 62 Zellen mit P1 ≠ P2, jede Differenz genau 1 W bzw. 0,1 K — Rundung, kein Modellunterschied.

**Vorzeichen.** Die gedruckte Konvention „Heizlast positiv, Kühllast negativ" ist durch den Sollwertverlauf belegt (Sollsprung 22 → 27 degC um 6 Uhr, zurück um 18 Uhr; daher tagsüber positiv, abends negativ). Nur Fall 6 ist in AixLib gedreht, und das ist eine Eigenheit der Verdrahtung, kein Lesefehler:

- **Fall 6:** `TestCase6.mo` führt `heatFlowSensor.Q_flow` unmittelbar auf den Mittelwertbildner; der Sensor zählt den Strom aus der Zone heraus positiv. Zahlenbeleg: 1. Tag, Std. 7 steht in der Norm mit +764 W, AixLib führt −764 W.
- **Fall 7:** `Modelica.Blocks.Math.Gain gainMea(k=-1)` zwischen Sensor und Mittelwertbildner stellt die gedruckte Konvention wieder her → nicht gedreht.
- **Fall 11:** `Modelica.Blocks.Math.Add add(k1=1, k2=-1)` über Kühl- und Heizsensor bewirkt dasselbe → nicht gedreht.

Wer die konsolidierten Lastreihen gegen AixLib prüft, muss also **nur im Fall 6 mit −1 multiplizieren**.

## Restzweifel

1. **Gemeinsame Wurzel der Textextraktion.** Leser A, meine Drittlesung und die Textfassung selbst stammen aus PdfPig, Leser B ebenfalls (nur über Wortkoordinaten statt Fließtext). Ein Fehler in der Glyphendekodierung träfe alle drei gleich. Für Lufttemperatur und Last entkräftet der AixLib-Abgleich das aus einer anderen Quelle heraus (BSD-lizenzierte Modelica-Modelle), 864 Zellen zeichengleich.
2. **Nicht extern belegte Zellen.** AixLib deckt nur je eine Programmspalte der Lufttemperatur (Fälle 1–5, 8–10, 12) bzw. der Last (6, 7, 11) ab. Ohne externen Beleg bleiben: operative Temperatur aller 12 Fälle (1728 Zellen), VDI-6020-Spalten (1008), Lufttemperatur der Fälle 6, 7, 11 (432) und die durchgehend mit 0 W besetzten Lastspalten der neun Temperaturfälle (1296). Dafür tragen drei übereinstimmende Lesungen plus Formprüfungen — nicht mehr.
3. **Die VDI-6020-Spalte ist kein zweiter Beleg derselben Zahl.** In den Fällen 3 und 4 weicht sie tagsüber systematisch ab (Fall 3, 1. Tag, Std. 7: 30,2 degC in Programm 1 gegen 28,1 degC in der VDI-6020-Spalte, beides so gedruckt) — Modellunterschied, taugt nicht zur Fehlersuche an der Lesung.
4. **Seite 55.** Die Wahl des ersten der beiden wertgleichen Blöcke ist belanglos, solange sie wertgleich sind (zeilenweise geprüft). Ein späterer PDF-Stand wäre erneut zu prüfen.
5. **Seitenzahlen.** `quelle`/`seite` nennen weisungsgemäß die Fortsetzungsseite, obwohl der 1. Tag auf der Seite davor steht; wer die Angabe wörtlich nimmt, findet dort nur zwei Drittel der Zahlen. `seiten` hält beide fest.

## Dateien

- Konsolidierter Satz: `C:\Users\Dirk\AppData\Local\Temp\epos-spike\vdi\norm\testfall_1.json` … `testfall_12.json` (Fälle 1–7 je 576 Werte, Fälle 8–12 je 432, zusammen 6192)
- Protokoll: `C:\Users\Dirk\AppData\Local\Temp\epos-spike\vdi\norm\ABGLEICH.md`
- Arbeitsdateien: `...\vdi\norm\_abweichungen.json` (leer), `...\vdi\norm\_aixlib.json`
- Werkzeuge (Perl 5.38, JSON::PP): `...\vdi\vergleich.pl`, `...\vdi\drittlesung.pl`, `...\vdi\konsolidieren.pl`, `...\vdi\pruefe_norm.pl`, `...\vdi\aixlib_abgleich.pl`

Schema des konsolidierten Satzes (Ablage von Leser B, folgt der Zielvorgabe wörtlich): je Größe `tag1`/`tag10`/`tag60` mit `p1`/`p2`, dazu `vdi6020` als Geschwisterschlüssel mit denselben Tagen (Fälle 1–7, bei `operativ` stets `null`; Fälle 8–12 durchgehend `null`, auf den Seiten 55/57/59/61/63 kommt „6020" nicht vor). Zusätzlich `fall`, `quelle`, `seite`, `seiten`, `anzahl_werte`, `hinweis`.

Ich habe ausschließlich unter `C:\Users\Dirk\AppData\Local\Temp\epos-spike` geschrieben; im Repository nichts geändert oder angelegt. Hinweis am Rande: im Repository sind während meiner Laufzeit (20:25 Uhr) zwei unversionierte Dateien `Dokumentation/aktuell/Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md` und `Dokumentation/aktuell/Status_Gebaeudesimulation_VDI6007.md` aufgetaucht — nicht von mir, vermutlich aus einem parallel laufenden Auftrag.

---

## 2. Validierung gegen das Normband (Stufe Validierung)

# Validierung gegen das Normband (VDI 6007 Blatt 1, Tabellen A1.3–A12.3)

## Kurzfassung

2592 Zellen geprüft (12 Fälle × 3 Größen × 3 Tage × 24 Stunden). **39 Zellen liegen außerhalb des Bands, 2553 innerhalb.** Von 36 Prüfungen (Fall × Größe) bestehen 31, fünf nicht: Fall 6 Last, Fall 9 Luft und operativ, Fall 10 Luft und operativ, Fall 11 Last. Vier der fünf Fehlschläge sind Haarrisse (Überschreitung ≤ 0,06 K bzw. ≤ 0,5 W); substanziell ist allein Fall 11 mit 3,9 W in genau zwei Stunden.

## Aufbau

| Baustein | Ort | Zweck |
|---|---|---|
| `NormVergleich\normband.pl` | Perl 5.38, JSON::PP | Bandvergleich, schreibt alle Ergebnisdateien |
| `NormVergleich\ProtoMittel\` | Kopie des 7R2C-Prototyps | liefert zusätzlich **Stundenmittel** der Oberflächentemperaturen |
| `NormVergleich\variante_endwerte.pl` | Gegenprobe | operative Temperatur aus Endwerten statt Stundenmitteln |

Der validierte Prototyp unter `...\epos-spike\Prototyp` wurde **nicht angefasst**. Grund für die Kopie: die CSV des Prototyps führt `theta_sAW_K`/`theta_sIW_K` nur als **Endwerte je Schritt**, die operative Temperatur braucht aber Stundenmittel. In der Kopie akkumuliert `Simulation.Advance` die Oberflächen analog zu Luft und Last (`Surfaces` ist affin, also ist das Mittel der Oberfläche gleich der Oberfläche des Mittels) und schreibt zwei zusätzliche Spalten `theta_sAW_mittel_K`, `theta_sIW_mittel_K`.

**Regressionsnachweis:** Die Spalten 1–10 (Zeit, Stunde, Luftmittel, Luftendwert, Q, Massen- und Oberflächenendwerte) und alle Randbedingungsspalten sind in allen 12 Fällen **bytegleich** mit dem validierten Lauf. Die Kopie ändert nur, was sie zusätzlich ausgibt.

Dass die Mittelwerte nötig waren, belegt die Gegenprobe: baut man die operative Temperatur aus den Endwerten, fallen **8 der 12 Fälle** durch (Fall 4: 0,082 K, Fall 9: 0,053 K); mit Stundenmitteln sind es nur die Fälle 9 und 10 mit 0,006 bzw. 0,060 K.

## Prüfvorschrift

Band je Stunde: **[min(P1,P2) − tol, max(P1,P2) + tol]**, tol = 0,1 K für Temperaturen, 1 W für Lasten. Stunde n = Mittel über die Stunde; Tag 1 Std. 1 = erster Schritt, Tag 10 = Schritte 217–240, Tag 60 = Schritte 1417–1440. Der Stundenindex jeder gelesenen CSV-Zeile wird gegen die erwartete Schrittnummer geprüft (harter Abbruch bei Verschiebung).

**Vorzeichen der Last.** Die Norm führt Heizlast positiv. Der Prototyp rechnet intern durchgehend „positiv = an die Zone" und multipliziert beim Schreiben mit `PowerSign`, das **nur im Fall 6** −1 ist (Nachbildung der AixLib-Verdrahtung, wo `heatFlowSensor.Q_flow` ohne `gainMea(k=-1)` auf den Mittelwertbildner geht). Der Vergleicher dreht das für Fall 6 zurück. Beleg: Tag 1, Std. 7 steht in der Norm mit +764 W, die Prototyp-CSV führt dort −765,48 W. Die Fälle 7 und 11 brauchen keine Umrechnung.

**Operative Temperatur.** θ_op = 0,5·θ_air + 0,5·(A_AW·θ_s,AW + A_IW·θ_s,IW)/(A_AW + A_IW), Flächen aus `referenz\testfall_<n>.json`. In allen 12 Fällen ist `AWin` = 0, die Fensterflächen (`ATransparent`, 7 m² in den Fällen 5, 8, 9, 10, 12) tragen im Zwei-Kapazitäten-Modell keinen eigenen Oberflächenknoten und gehen deshalb nicht in die Flächenwichtung ein. Dass die Formel trägt, zeigt das Ergebnis selbst: 10 von 12 Fällen liegen vollständig im Band, mit Reserven um 0,05 K.

## Ergebnistabelle

Vollständig in `C:\Users\Dirk\AppData\Local\Temp\epos-spike\NormVergleich\out\normband.csv` (2592 Zeilen, je Zelle Band, Prototypwert, Überschreitung, Randabstand, VDI-6020-Spalte). Zusammenfassung:

| Fall | Größe | Einh. | max. Überschreitung | Std. außerhalb | bestanden | Reserve |
|---|---|---|---|---|---|---|
| 1 | Luft | degC | 0 | 0/72 | ja | 0,0447 |
| 1 | operativ | degC | 0 | 0/72 | ja | 0,0480 |
| 1 | Last | W | 0 | 0/72 | ja | 1,0000 |
| 2 | Luft | degC | 0 | 0/72 | ja | 0,0484 |
| 2 | operativ | degC | 0 | 0/72 | ja | 0,0513 |
| 2 | Last | W | 0 | 0/72 | ja | 1,0000 |
| 3 | Luft | degC | 0 | 0/72 | ja | 0,0410 |
| 3 | operativ | degC | 0 | 0/72 | ja | 0,0480 |
| 3 | Last | W | 0 | 0/72 | ja | 1,0000 |
| 4 | Luft | degC | 0 | 0/72 | ja | 0,0442 |
| 4 | operativ | degC | 0 | 0/72 | ja | 0,0471 |
| 4 | Last | W | 0 | 0/72 | ja | 1,0000 |
| 5 | Luft | degC | 0 | 0/72 | ja | 0,0421 |
| 5 | operativ | degC | 0 | 0/72 | ja | 0,0506 |
| 5 | Last | W | 0 | 0/72 | ja | 1,0000 |
| 6 | Luft | degC | 0 | 0/72 | ja | 0,1000 |
| 6 | operativ | degC | 0 | 0/72 | ja | 0,0502 |
| **6** | **Last** | **W** | **0,4986** | **14/72** | **nein** | **−0,4986** |
| 7 | Luft | degC | 0 | 0/72 | ja | 0,0464 |
| 7 | operativ | degC | 0 | 0/72 | ja | 0,0473 |
| 7 | Last | W | 0 | 0/72 | ja | 0,3611 |
| 8 | Luft | degC | 0 | 0/72 | ja | 0,0496 |
| 8 | operativ | degC | 0 | 0/72 | ja | 0,0484 |
| 8 | Last | W | 0 | 0/72 | ja | 1,0000 |
| **9** | **Luft** | **degC** | **0,0112** | **1/72** | **nein** | **−0,0112** |
| **9** | **operativ** | **degC** | **0,0063** | **1/72** | **nein** | **−0,0063** |
| 9 | Last | W | 0 | 0/72 | ja | 1,0000 |
| **10** | **Luft** | **degC** | **0,0240** | **8/72** | **nein** | **−0,0240** |
| **10** | **operativ** | **degC** | **0,0598** | **11/72** | **nein** | **−0,0598** |
| 10 | Last | W | 0 | 0/72 | ja | 1,0000 |
| 11 | Luft | degC | 0 | 0/72 | ja | 0,0522 |
| 11 | operativ | degC | 0 | 0/72 | ja | 0,0483 |
| **11** | **Last** | **W** | **3,9183** | **4/72** | **nein** | **−3,9183** |
| 12 | Luft | degC | 0 | 0/72 | ja | 0,0462 |
| 12 | operativ | degC | 0 | 0/72 | ja | 0,0506 |
| 12 | Last | W | 0 | 0/72 | ja | 1,0000 |

Zwei Zeilen sind nur scheinbar Prüfungen: die **Last der neun Temperaturfälle** ist in Norm und Prototyp durchgehend 0 W (Reserve 1,0000 = Bandmitte), und die **Luft im Fall 6** ist durch die ideale, unbegrenzte Regelung exakt der Sollwert (Reserve 0,1000 = Bandmitte, Abweichung 0,000 K). Substanz haben die Lastprüfungen der Fälle 6, 7, 11 und die Temperaturprüfungen aller zwölf.

## Alle 39 Stunden außerhalb des Bands

`out\normband_ausserhalb.csv`. Temperaturen in degC, Lasten in W.

| Fall | Größe | Tag | Std. | Band | Prototyp | Überschr. |
|---|---|---|---|---|---|---|
| 6 | Last | 1 | 7 | [763, 765] | 765,484 | 0,484 |
| 6 | Last | 1 | 8 | [695, 697] | 697,451 | 0,451 |
| 6 | Last | 1 | 11 | [510, 512] | 512,133 | 0,133 |
| 6 | Last | 1 | 19 | [−639, −637] | −639,499 | 0,499 |
| 6 | Last | 1 | 23 | [−534, −532] | −534,320 | 0,320 |
| 6 | Last | 10 | 7 | [162, 164] | 164,003 | 0,003 |
| 6 | Last | 10 | 11 | [1, 3] | 3,079 | 0,079 |
| 6 | Last | 10 | 20 | [−961, −959] | −961,379 | 0,379 |
| 6 | Last | 10 | 21 | [−920, −918] | −920,204 | 0,204 |
| 6 | Last | 10 | 22 | [−881, −879] | −881,088 | 0,088 |
| 6 | Last | 60 | 11 | [1, 3] | 3,012 | 0,012 |
| 6 | Last | 60 | 20 | [−961, −959] | −961,425 | 0,425 |
| 6 | Last | 60 | 21 | [−920, −918] | −920,249 | 0,249 |
| 6 | Last | 60 | 22 | [−881, −879] | −881,131 | 0,131 |
| 9 | Luft | 60 | 18 | [42,2 , 42,4] | 42,4112 | 0,0112 |
| 9 | operativ | 60 | 6 | [40,2 , 40,4] | 40,4063 | 0,0063 |
| 10 | Luft | 1 | 12 | [20,4 , 20,6] | 20,3760 | 0,0240 |
| 10 | Luft | 1 | 16 | [21,4 , 21,6] | 21,3841 | 0,0159 |
| 10 | Luft | 10 | 20 | [25,2 , 25,4] | 25,4153 | 0,0153 |
| 10 | Luft | 60 | 3 | [25,1 , 25,3] | 25,3231 | 0,0231 |
| 10 | Luft | 60 | 4 | [25,0 , 25,2] | 25,2038 | 0,0038 |
| 10 | Luft | 60 | 5 | [25,0 , 25,2] | 25,2154 | 0,0154 |
| 10 | Luft | 60 | 7 | [25,2 , 25,4] | 25,4086 | 0,0086 |
| 10 | Luft | 60 | 19 | [26,0 , 26,2] | 26,2126 | 0,0126 |
| 10 | operativ | 10 | 6 | [24,3 , 24,5] | 24,5043 | 0,0043 |
| 10 | operativ | 10 | 20 | [25,2 , 25,4] | 25,4154 | 0,0154 |
| 10 | operativ | 60 | 1 | [25,3 , 25,5] | 25,5598 | 0,0598 |
| 10 | operativ | 60 | 2 | [25,2 , 25,4] | 25,4432 | 0,0432 |
| 10 | operativ | 60 | 3 | [25,1 , 25,3] | 25,3232 | 0,0232 |
| 10 | operativ | 60 | 4 | [25,0 , 25,2] | 25,2040 | 0,0040 |
| 10 | operativ | 60 | 6 | [25,0 , 25,2] | 25,2319 | 0,0319 |
| 10 | operativ | 60 | 7 | [25,1 , 25,3] | 25,3187 | 0,0187 |
| 10 | operativ | 60 | 14 | [26,6 , 26,8] | 26,8001 | 0,0001 |
| 10 | operativ | 60 | 18 | [26,1 , 26,3] | 26,3166 | 0,0166 |
| 10 | operativ | 60 | 20 | [25,8 , 26,0] | 26,0409 | 0,0409 |
| 11 | Last | 10 | 7 | [125, 127] | 127,034 | 0,034 |
| **11** | **Last** | **10** | **10** | **[−122, −120]** | **−125,918** | **3,918** |
| 11 | Last | 10 | 11 | [−392, −390] | −389,9995 | 0,0005 |
| **11** | **Last** | **60** | **10** | **[−123, −120]** | **−126,266** | **3,266** |

Die Zeile Fall 10 / operativ / Tag 60 / Std. 14 überschreitet um **0,0001 K** — sie ist als Fehlschlag nicht belastbar. Fall 11 / Tag 10 / Std. 11 überschreitet um 0,0005 W, ebenso wenig.

## Vergleich mit der früheren Einspaltenbewertung

Bisher wurde gegen **eine** Tabellenspalte mit der AixLib-Schwelle 0,15 K bzw. 1,5 W geprüft (Fall 11 gegen Programm 2, alle übrigen gegen Programm 1 — die Zuordnung aus dem Abgleichsbericht).

| Fall | Einspaltig: max abs. Abw. | Schwelle | über Schwelle | Normband: Std. außerhalb | Urteil kippt? |
|---|---|---|---|---|---|
| 1 | 0,0553 K | 0,15 | 0/72 | 0 | nein |
| 2 | 0,0516 K | 0,15 | 0/72 | 0 | nein |
| 3 | 0,0590 K | 0,15 | 0/72 | 0 | nein |
| 4 | 0,0558 K | 0,15 | 0/72 | 0 | nein |
| 5 | 0,0579 K | 0,15 | 0/72 | 0 | nein |
| **6** | **1,4986 W** | **1,5** | **0/72** | **14** | **ja — bestanden → durchgefallen** |
| 7 | 0,6389 W | 1,5 | 0/72 | 0 | nein |
| 8 | 0,0504 K | 0,15 | 0/72 | 0 | nein |
| **9** | **0,1363 K** | **0,15** | **0/72** | **1 (Luft)** | **ja** |
| **10** | **0,1430 K** | **0,15** | **0/72** | **8 (Luft)** | **ja** |
| **11** | **4,9183 W** | **1,5** | **2/72** | **4** | nein — beide durchgefallen |
| 12 | 0,0538 K | 0,15 | 0/72 | 0 | nein |

Der Befund ist eindeutig: **das Normband ist die schärfere Prüfung**, obwohl es zwei Spalten zulässt. Die Bandbreite ist in fast allen Stunden null (P1 = P2), sodass effektiv ±0,1 K gegen ±0,15 K und ±1 W gegen ±1,5 W steht. Die Fälle 6, 9 und 10 lagen mit 1,4986 W, 0,1363 K und 0,1430 K jeweils **knapp unter** der alten Schwelle — ein Reservepolster von 0,1 % bzw. 5 %. Fall 11 fällt in beiden Bewertungen durch; das Band findet dort 4 statt 2 Stunden, weil zwei weitere Stunden zwischen 1,0 und 1,5 W liegen.

Umgekehrt entschärft das Band den Fall 11 Tag 60 Std. 10: dort weichen P1 (−121) und P2 (−122) voneinander ab, das Band ist 3 W breit, und die Überschreitung sinkt von 4,27 W (gegen P2) auf 3,27 W. Das ist die **einzige** Stunde in Fall 11, in der die beiden Normprogramme selbst uneins sind — und ausgerechnet die kritische.

## Vergleich gegen die VDI-6020-Spalte (nur zur Information)

Die Spalte gibt es in den Fällen 1–7, nie für die operative Temperatur. Verglichen wurde gegen ein Einspaltenband ±tol. Sie ist **kein zweiter Beleg derselben Zahl**, sondern ein anderes Rechenverfahren:

| Fall | Größe | max. Abweichung Prototyp ↔ 6020 | Stunden außerhalb ±tol | Beispielstelle |
|---|---|---|---|---|
| 1 | Luft | 0,471 K | 55/72 | Tag 60, Std. 14: 6020 = 56,2 degC, Prototyp 55,73 |
| 2 | Luft | 0,520 K | 51/72 | Tag 10, Std. 6 |
| 3 | Luft | 2,198 K | 64/72 | Tag 10, Std. 7: 6020 = 48,7 degC, Prototyp 50,90 |
| 4 | Luft | 1,925 K | 61/72 | Tag 10, Std. 7 |
| 5 | Luft | 0,458 K | 50/72 | Tag 1, Std. 16 |
| 6 | Last | 66,45 W | 66/72 | Tag 1, Std. 8: 6020 = 631 W, Prototyp 697,45 |
| 7 | Last | 67,65 W | 34/72 | Tag 1, Std. 12 |
| 1–7 | Last (Temp.fälle) | 0 W | 0/72 | beide durchgehend 0 |

Der Prototyp folgt durchgehend VDI 6007, nicht VDI 6020 — erwartungsgemäß, denn er bildet das 7R2C-Modell der 6007 nach. Geprüft und verworfen: die 6020-Lastreihe des Falls 6 ist **nicht** um eine Stunde gegen 6007 verschoben (6020 Std. 7 = 701 W gegen 6007 Std. 7 = 764 W; die Ähnlichkeit von 6020 Std. 8 = 631 W zu 6007 Std. 9 = 632 W ist Zufall). Die konsolidierte Lesung hat hier also kein Ausrichtungsproblem.

## Ursachenanalyse

### Fall 11 — Kühldecke, die einzige substanzielle Abweichung

Beide Ausreißer liegen in **Stunde 10** der Tage 10 und 60, also genau in der Stunde, in der das Modell von Heizen (Q am Luftknoten) auf Kühlen (Q an der Innenwandoberfläche) umschaltet. In allen Stunden mit stabilem Betriebszustand stimmt der Prototyp bis auf ≤ 1 W: Std. 9 liefert 28,96 gegen 28 W, Std. 11 −390,00 gegen −391 W, die Sättigungsstunden exakt −500 W.

Die eingebaute Diagnose (`--tc11-diag`) gibt den Umschaltzeitpunkt aus: **t+478,6 s** in Stunde 226 (Tag 10) und **t+474,2 s** in Stunde 1426 (Tag 60), jeweils gefolgt von einer Kühlleistung, die bei rund −6 W beginnt und bis zum Stundenende auf die Größenordnung −390 W anwächst. Um das Stundenmittel von −125,92 auf die gedruckten −121 W zu heben, fehlen 4,92 W·h; bei einem Endwert um −390 W entspricht das einer **um rund 45 s späteren Umschaltung**. Der Prototyp schaltet also etwa eine Dreiviertelminute zu früh, knapp 10 % des Zeitpunkts — nicht mehr.

Zur Zuordnung des Kühlanteils: der Prototyp führt die Kühlung korrekt am Innenwand-Oberflächenknoten (`ControlIntSurface`, Regel `CoolAtIntSurface`), nicht an der Luft; deshalb stimmen Std. 11 und alle Sättigungsstunden. Was er **nicht** kann, ist die Kühldecke von den übrigen Innenflächen trennen: der Datensatz führt für Fall 11 einen einzigen Innenknoten mit `hConInt` = 3,0 W/(m²K) über A_IW = 75,5 m² (gegen 2,24 in den Fällen 1–9) — eine flächengewichtete Mischung aus dem erhöhten α_kon = 5,0 der Decke und dem Normalwert der übrigen Flächen. Solange Heiz- und Kühlzweig getrennt stationär laufen, ist diese Mischung ausreichend; im Umschaltmoment hängt der Zeitpunkt aber davon ab, wie schnell die gekühlte Fläche selbst reagiert — und genau dafür fehlt dem Ein-Knoten-Modell die Auflösung. Das ist die wahrscheinlichste Ursache der 45 s.

Der Hinweis, dass hier ein Artefakt liegt, kommt aus der Referenz selbst: `TestCase11.mo` ersetzt die Messgröße 120 s nach jedem Wechsel durch eine vierte Tabellenspalte (an dieser Stelle −391 W). Die nachgebildete Korrektur (`--tc11-window`) ändert am Bandergebnis **nichts** (max. Überschreitung bleibt 3,9183 W bei 4 Stunden), weil der Prototyp den Wechsel nicht an der Stundengrenze erkennt, an der die Ersatzspalte greift. Der Referenzimplementierung ist das Problem also bekannt; sie löst es durch Ausblenden, nicht durch ein besseres Modell.

### Fall 6 — Vorzeichen und Betrag

**Vorzeichen:** kein Fehler, nur eine Konvention. Der Prototyp rechnet „positiv = an die Zone" und dreht beim Schreiben nur im Fall 6 (Nachbildung der AixLib-Verdrahtung ohne `gainMea(k=-1)`). Nach Rückdrehung passt das Vorzeichen in allen 72 Stunden; keine einzige Abweichung ist ein Vorzeichenfehler.

**Betrag:** eine systematische Restabweichung zwischen 0,0 und 1,50 W, also bis 0,2 % der Spitzenlast. Sie ist **phasenstarr an das Sollwertprofil gekoppelt**: in den Stunden 7–18 (Sollwert 27 degC) liegt der Prototyp durchweg höher (bis +1,484 W), in den Stunden 19–6 (Sollwert 22 degC) durchweg tiefer (bis −1,499 W). Das ist kein Rauschen und kein Rundungsartefakt der gedruckten Ganzzahlen.

Drei Hypothesen wurden gerechnet und verworfen:

| Experiment | Wirkung auf Fall 6 (max. Überschr. / Std. außerhalb) | Nebenwirkung | Befund |
|---|---|---|---|
| Referenz (unverändert) | 0,4986 W / 14 | — | — |
| `--tc6-exact` (exakte statt gerundeter RC-Werte des TC6-Datensatzes) | 0,4651 W / 12 | keine | erklärt es nicht |
| `--radscale 1,02` / `0,98` (Strahlungsleitwert ±2 %) | 0,5117 / 15 bzw. 0,6223 / 14 | **zerstört die Fälle 1 und 2** (5 bzw. 6 Std. außerhalb) | ausgeschlossen |
| `--fradext 0,127` / `0,118` (Strahlungssplit AW/IW statt 0,1221) | 0,4502 / 12 bzw. 0,8378 / 23 | Fälle 1, 2 bleiben im Band | mildert, erklärt es nicht |

Keine plausible Parametervariation räumt die Abweichung ab. Sie ist ein echter, kleiner Modellrest. Zur Einordnung: das Zimmer koppelt mit G_c,AW + G_c,IW ≈ 197,5 W/K an die Luft; die 0,05 K, mit denen der Prototyp die Temperaturfälle trifft, entsprächen einem Lastfehler von rund 10 W. Der Fall 6 wird also mit 1,5 W **deutlich genauer** reproduziert als die Temperaturfälle — er fällt nur durch, weil das Band mit 1 W schmaler ist als die alte Schwelle von 1,5 W.

### Fälle 9 und 10 — an der Druckauflösung

Fall 9 (langwelliger Austausch mit dem Himmel) überschreitet in **einer** Stunde um 0,0112 K, die operative Temperatur in einer um 0,0063 K. Fall 10 (nicht adiabater Fußboden, Nachbarraum über äquivalente Außentemperatur) überschreitet in 8 bzw. 11 Stunden um maximal 0,024 bzw. 0,060 K. Beide sind die schwächsten Temperaturfälle auch in der Einspaltenbewertung (0,1363 und 0,1430 K), was zu ihren Sonderpfaden passt: der Himmelstemperaturansatz 65,99·H_Sky^0,25 im Fall 9 und die Einfaltung des Bodens in die äquivalente Außentemperatur (wfGro, T_Gro) im Fall 10.

Ein Vorbehalt zur Bewertung: die Norm druckt Temperaturen auf 0,1 K. Ein gedruckter Wert 25,4 steht für ein wahres Intervall [25,35 … 25,45]. Nimmt man die Druckrundung zur Toleranz hinzu (effektiv ±0,15 K um den wahren Wert), **verschwinden sämtliche Überschreitungen der Fälle 9 und 10** — die größte ist 0,060 K. Ob die 0,1 K aus 6.6 auf den gedruckten oder den wahren Wert anzuwenden sind, ist eine Auslegungsfrage, die das Urteil über zwei der fünf Fehlschläge umdreht. Im Bericht ist strikt nach Aufgabenstellung gegen den gedruckten Wert gerechnet.

## Laufzeit

| Schritt | Zeit |
|---|---|
| Prototyplauf, 12 Fälle × 1440 Schritte (Prozessstart inbegriffen) | 1,32 s |
| davon reine Integration je Fall, hochgerechnet auf 8760 Schritte | 6,2 ms (Fall 12) bis 120,4 ms (Fall 7) |
| Bandvergleich (12 Normsätze, 12 CSV à 1440 Zeilen, 2592 Zellen, vier Ausgabedateien) | 0,28 s |

Die teuersten Fälle sind 6 und 7 (114 bzw. 120 ms je 8760 Schritte) — dort kostet die Bisektion auf den Modewechsel; die reinen Freilauffälle liegen bei 6 ms.

## Dateien

| Datei | Inhalt |
|---|---|
| `C:\Users\Dirk\AppData\Local\Temp\epos-spike\NormVergleich\out\normband.csv` | **vollständige Ergebnistabelle**, 2592 Zeilen |
| `...\NormVergleich\out\normband_zusammenfassung.csv` | 36 Zeilen Fall × Größe |
| `...\NormVergleich\out\normband_ausserhalb.csv` | die 39 Stunden außerhalb |
| `...\NormVergleich\out\normband_vdi6020.csv` | Vergleich gegen die VDI-6020-Spalte |
| `...\NormVergleich\out\konsole.txt` | Konsolenprotokoll des Laufs |
| `...\NormVergleich\out\proto\testfall_1..12.csv` | Prototyplauf mit Oberflächen-Stundenmitteln |
| `...\NormVergleich\normband.pl` | Vergleichsprogramm (Optionen `--proto`, `--nur`, `--kurz`) |
| `...\NormVergleich\variante_endwerte.pl` | Gegenprobe operative Temperatur aus Endwerten |
| `...\NormVergleich\ProtoMittel\` | Prototypkopie (Optionen zusätzlich: `--tc11-diag`, `--radscale`, `--fradext`) |

Geschrieben wurde ausschließlich unter `C:\Users\Dirk\AppData\Local\Temp\epos-spike`; im Repository wurde nichts geändert oder angelegt, der validierte Prototyp unter `...\epos-spike\Prototyp` ist unberührt.

## Restzweifel

1. **Das Normband hängt an der konsolidierten Lesung.** Fällt eine gelesene Zahl falsch aus, verschiebt sich das Band um genau diesen Betrag. Für Luft und Last der referenzierten Programmspalte stützt der AixLib-Abgleich (864 Zellen zeichengleich) die Lesung aus zweiter Quelle; die **operative Temperatur aller 12 Fälle (1728 Zellen) hat keinen externen Beleg**. Dass der Prototyp sie in 10 von 12 Fällen vollständig trifft, ist ein starkes indirektes Indiz, aber kein Nachweis.
2. **Die Bandbreite ist fast überall null.** In 2592 Zellen weichen P1 und P2 nur in wenigen Dutzend voneinander ab (im Fall 11 in einer einzigen der 72 Laststunden). Die Zweispaltigkeit bringt also kaum Spielraum; praktisch prüft das Band gegen eine Spalte mit engerer Toleranz. Wer sich vom Band eine Entschärfung versprochen hat, bekommt das Gegenteil.
3. **Operative Temperatur ohne Fensterknoten.** In den Fällen mit `ATransparent` = 7 m² bleibt die Fensterinnenfläche in der Flächenwichtung außen vor, weil das Zwei-Kapazitäten-Modell dort keinen Oberflächenknoten führt. Für diese Fälle ist der Vergleich der operativen Temperatur streng genommen ein Vergleich des Modells mit sich selbst plus Norm, nicht eine saubere Prüfung der Strahlungstemperatur. Er fällt trotzdem positiv aus (Fälle 5, 8, 12 vollständig im Band).
4. **Fall 11 ist nicht repariert, nur lokalisiert.** Die 45 s Umschaltversatz sind belegt, die Zuordnung zur fehlenden Auflösung der Kühldecke ist die plausibelste, aber nicht bewiesene Erklärung. Ein Nachweis bräuchte ein Modell mit getrenntem Deckenknoten.
5. **Die `--tc6-exact`-Frage bleibt offen.** Der TC6-Datensatz führt gerundete RC-Werte, die Fälle 1, 2 und 7 die genauen. Der Unterschied ist für das Bandergebnis fast belanglos (14 gegen 12 Stunden), aber er zeigt, dass der Datensatz selbst nicht in sich konsistent ist.

---

## 3. Kritik — Nachprüfung an den Rohdaten (Stufe Kritik)

# Kritik der beiden Berichte — Nachprüfung an den Rohdaten

Alle Prüfskripte liegen unter `C:\Users\Dirk\AppData\Local\Temp\epos-spike\Kritik\` (`q2.pl`, `p1.pl`, `op2.pl`, `gp.pl`, `ax.pl`, `ar.pl`). Im Repository wurde nichts geändert oder angelegt.

**Gesamturteil:** Beide Berichte halten der Nachprüfung in der Sache stand. Die Lesung der Norm, die Tag/Stunden-Zuordnung, die Bandrechnung, die AixLib-Zuordnung und die Vorzeichenbehandlung sind in jeder gezogenen Stichprobe korrekt. Es gibt jedoch **einen harten Zählfehler in der Kurzfassung des Validierungsberichts** und vier kleinere Ungenauigkeiten.

---

## 1) Stichprobe Normwerte gegen die Textfassung — 30 Werte, 30 Treffer

Gezogen über acht Fälle und sieben Seiten, bewusst auf die heiklen Stellen (Tausenderpunkt, Doppelblock, VDI-6020-Spalte, P1≠P2-Zellen).

| Fall | Größe / Tag / Spalte | Std. | JSON | Textfassung | Seite |
|---|---|---|---|---|---|
| 1 | luft tag10 p1 | 1 | 37,7 | 37,7 | 41 |
| 1 | luft tag60 p1 | 17 | 56,1 | 56,1 | 41 |
| 1 | operativ tag10 p1 | 14 | 41,6 | 41,6 | 41 |
| 1 | luft tag60 **vdi6020** | 18 | 56,6 | 56,6 | 41 |
| 3 | luft tag10 p1 | 7 | 50,9 | 50,9 | 45 |
| 3 | luft tag1 **vdi6020** | 7 | 28,1 | 28,1 | 45 |
| 6 | last tag1 p1 | 7 | 764 | 764 | 50 |
| 6 | last tag10 p1 | 19 | −1004 | `-1.004` | 51 |
| 6 | last tag10 p2 | 19 | −1004 | `-1004` | 51 |
| 6 | last tag60 p1 / p2 | 9 | 78 / 79 | 78 / 79 | 51 |
| 7 | last tag60 p2 | 18 | −409 | −409 | 53 |
| 8 | luft tag10 p1 | 16 | 41,3 | 41,3 | 55 |
| 9 | luft tag10 p2 | 3 | 37,2 | 37,2 | 57 |
| 10 | luft tag60 p1 | 1 | 25,5 | 25,5 | 59 |
| 11 | last tag10 p1 | 10 | −121 | −121 | 61 |
| 11 | last tag60 p2 | 10 | −122 | −122 | 61 |
| 12 | operativ tag10 p2 | 24 | 30,5 | 30,5 | 63 |

(vollständige Liste: `Kritik/q2.pl`, 30/30 zeichengleich; weitere 13 Werte ohne Abweichung nicht abgedruckt)

Ebenfalls bestätigt:
- **Seite 51**: der Tausenderpunkt steht tatsächlich nur in P1 (`-1.004` neben `-1004`) — die Entscheidung „−1004 in allen vier Spalten" ist richtig.
- **Seite 55**: der Ergebnisblock steht zweimal, Einheitenzeile `in °C` bzw. `°C`, in allen 24 Zeilen zeichengleich. Der Abgleichsbericht hat recht, Leser B hatte unrecht.
- **6020-Block**: auf den Seiten 55/57/59/61/63 kommt „6020" null mal vor; die JSON führen dort durchgehend `null`. Struktur und Zählung stimmen: 7 × 576 + 5 × 432 = 6192.
- **AixLib unabhängig nachgerechnet** (`Kritik/ax.pl`, aus `referenz/testfall_*.json`, 12 × 72 = 864 Zellen): Fälle 1–5, 8–10, 12 Luft mit +1 exakt auf P1 (0,0000); Fall 6 Last **nur mit −1** exakt auf P1; Fall 7 Last mit +1 auf P1; Fall 11 Last mit +1 exakt auf **P2** (P1 um 1 W daneben). Die AixLib-Tabelle des Abgleichsberichts ist damit vollständig reproduziert, inklusive Vorzeichen-Sonderfall 6. Beleg: `referenz/testfall_6.json` führt bei t = 25200 s den Wert **−764**, die Norm druckt **+764**.

---

## 2) Stichprobe Prototypwerte gegen `normband.csv` — 22 Werte, 22 Treffer

Gelesen aus `Prototyp\out\testfall_<n>.csv` (nicht aus der Kopie), Schrittnummer = 216 + Std. für Tag 10, 1416 + Std. für Tag 60.

| Fall | Größe | Tag/Std. | Schritt | Wert in `Prototyp\out` | Spalte `prototyp` | Band aus P1/P2 | innerhalb |
|---|---|---|---|---|---|---|---|
| 6 | last | 1 / 7 | 7 | −765,4843 | **+**765,4843 | [763 … 765] aus 764/764 | nein |
| 6 | last | 10 / 19 | 235 | +1004,7823 | **−**1004,7823 | [−1005 … −1003] | ja |
| 6 | last | 10 / 11 | 227 | −3,0792 | +3,0792 | [1 … 3] aus 2/2 | nein |
| 11 | last | 10 / 10 | 226 | −125,9183 | −125,9183 | [−122 … −120] aus −121/−121 | nein |
| 11 | last | 60 / 10 | 1426 | −126,2660 | −126,2660 | [−123 … −120] aus −121/−122 | nein |
| 9 | luft | 60 / 18 | 1434 | 42,411223 | 42,4112 | [42,2 … 42,4] | nein |
| 10 | luft | 1 / 12 | 12 | 20,376048 | 20,3760 | [20,4 … 20,6] | nein |
| 1 | luft | 10 / 18 | 234 | 44,695305 | 44,6953 | [44,6 … 44,8] | ja |
| 7 | last | 60 / 18 | 1434 | −408,6312 | −408,6312 | [−410 … −407] aus −408/−409 | ja |

Weitere fünf Zeilen (3, 8, 12, 6/10/11, 11/10/11) ebenfalls deckungsgleich.

**Bandrechnung:** in allen 14 Fällen exakt `[min(P1,P2) − tol, max(P1,P2) + tol]`, tol = 0,1 K / 1 W. Das ist die wörtliche Umsetzung von Abschnitt 6.6 der Richtlinie („im Bereich der Ergebnisse von Programm 1 und Programm 2 … ±0,1 °C" bzw. „±1 W") — Formulierung im Text bestätigt.

**Vorzeichen:** die Rückdrehung greift ausschließlich in Fall 6; Fälle 7 und 11 werden unverändert übernommen. Korrekt.

**Operative Temperatur unabhängig nachgerechnet** (`Kritik/op2.pl`) aus `theta_sAW_mittel_K` / `theta_sIW_mittel_K` und `AExt`/`AInt`: acht Stichproben, alle auf vier Nachkommastellen identisch mit der Spalte `prototyp` (z. B. Fall 10, Tag 60, Std. 1 = 25,5598; Fall 9, Tag 60, Std. 6 = 40,4063). Auch die **Gegenprobe aus Endwerten** ist reproduzierbar: 8 von 12 Fällen fallen durch, Fall 4 mit 0,0824 K, Fall 9 mit 0,0528 K — exakt die im Bericht genannten 0,082 und 0,053.

**Regressionsnachweis bestätigt:** Spalten 1–10 von `Prototyp\out\testfall_<n>.csv` und `NormVergleich\out\proto\testfall_<n>.csv` sind in allen 12 Fällen md5-gleich.

---

## 3) Definition der „n-ten Stunde" — konsistent

| Quelle | Aussage | Beleg |
|---|---|---|
| Norm S. 38 | „der Ausgabe von z. B. ‚11. Stunde' [ist] der Ausgabe ‚10:00 bis 11:00 Uhr' gleichwertig", Ausgabe als **Mittelwert** der Stunde | Textfassung S. 38 |
| Prototyp | `tEnd = (k+1)·Dt`, Spalte `stunde = k+1`, `MeanTAir`/`MeanQ` = Mittel über den Schritt | `Prototyp\Program.cs` Z. 93–107 |
| AixLib | Stützstelle t = n·3600 trägt den Wert der n-ten Stunde (t = 3600 → Std. 1) | `referenz\testfall_6.json`, t = 25200 → −764 = Std. 7 |
| Vergleicher | Tag 1 Std. 1 = Schritt 1, Tag 10 = 217–240, Tag 60 = 1417–1440 | nachgerechnet, s. Tabelle oben |

Alle vier Ebenen stimmen überein. Zusätzlicher Beleg: der Prototyp legt die 1000 W Strahlungslast des Falls 6 in die Schritte 7–18 (`QRadAW` = 122,093 / `QRadIW` = 877,907), und genau dort druckt die Norm Lasten ≠ 0.

**Ein Vorbehalt, den beide Berichte nicht erwähnen:** Die *extrahierten Nutzungsprofile* (Tabellen A.6.2, A.11.2) sind in der Textfassung um **eine Zeile gegen den Ergebnisblock versetzt**. Sie zeigen den Sollwertsprung 22 → 27 in der Zeile „5 bis 6" und die 1000 W ab „6 bis 7" bis „16 bis 17" (11 statt 12 Stunden). Die Ergebnisspalten und der Prototyp verlangen dagegen zwingend 27 °C in den Stunden 7–18, also ab 6 Uhr. Die Aussage des Abgleichsberichts „Sollsprung 22 → 27 um 6 Uhr, zurück um 18 Uhr" ist **inhaltlich richtig, aber aus dem Ergebnisblock erschlossen, nicht aus der gedruckten Profiltabelle** — die ist an dieser Stelle ein Extraktionsartefakt. Ein Konzeptpapier darf die Profiltabelle aus der Textfassung nicht zitieren.

---

## 4) Widersprüche und Fehler

### 4.1 Harter Zählfehler (Validierungsbericht, Kurzfassung)

> „Von 36 Prüfungen (Fall × Größe) bestehen 31, fünf nicht: Fall 6 Last, Fall 9 Luft und operativ, Fall 10 Luft und operativ, Fall 11 Last."

Die eigene Aufzählung nennt **sechs** Prüfungen. `normband_zusammenfassung.csv` zählt:

| | Anzahl |
|---|---|
| bestanden = ja | **30** |
| bestanden = nein | **6** (6/last, 9/luft, 9/operativ, 10/luft, 10/operativ, 11/last) |

Richtig ist also **30 bestanden, 6 durchgefallen**, und der Folgesatz muss lauten „**fünf der sechs** Fehlschläge sind Haarrisse". Betroffene Fälle: 4 von 12.

### 4.2 Falscher Zahlenwert in der VDI-6020-Tabelle (Validierungsbericht)

Die Spalte „max. Abweichung Prototyp ↔ 6020" mischt zwei Größen. In `normband_vdi6020.csv` steht die *Überschreitung* des ±tol-Bandes; der Bericht addiert für die Fälle 1–4, 6, 7 korrekt die Toleranz auf, für **Fall 5 nicht**:

| Fall | Datei (Überschreitung) | Bericht | nachgerechnetes max \|Proto − 6020\| |
|---|---|---|---|
| 1 | 0,3714 | 0,471 | **0,4714** ✓ |
| 3 | 2,0978 | 2,198 | **2,1978** ✓ |
| **5** | 0,4579 | **0,458** ✗ | **0,5579** |
| 6 | 65,4505 | 66,45 | **66,4505** ✓ |
| 7 (Last) | 66,6483 | 67,65 | **67,6483** ✓ |

Zusätzlich fehlt in der Tabelle die Zeile **Fall 7 / Luft: 0,4352 K, 33/72 Stunden außerhalb** — sie steht in der Ergebnisdatei, aber nicht im Bericht.

### 4.3 „durchweg" ist zu stark (Validierungsbericht, Ursachenanalyse Fall 6)

> „in den Stunden 7–18 … durchweg höher …, in den Stunden 19–6 … durchweg tiefer"

Ein Gegenbeispiel: **Tag 1, Std. 15 → −0,0246 W** (Sollwert 27, Prototyp *tiefer*). Dazu sechs Stunden (Tag 1, Std. 1–6) mit Differenz exakt 0,0000. Das Muster trägt in 65 von 72 Stunden, nicht in 72. Die Extremwerte des Berichts (+1,484 W bei Std. 7, −1,499 W bei Std. 19) sind korrekt.

### 4.4 Zwei Parameterangaben ungenau (Validierungsbericht)

| Behauptung | tatsächlich (`referenz\testfall_*.json`) |
|---|---|
| „`ATransparent`, 7 m² in den Fällen 5, 8, 9, 10, 12" | Fälle 8 und 9 führen **zwei Orientierungen mit je 7 m², zusammen 14 m²** |
| „`hConInt` = 3,0 … (gegen 2,24 in den Fällen 1–9)" | 2,24 gilt für 1–7; Fälle 8/9 = **2,12**, Fall 10 = **2,398** |

Der erste Punkt ist nicht kosmetisch: in Fall 9 — einem der Fehlschläge — bleibt doppelt so viel Fensterfläche aus der Flächenwichtung der operativen Temperatur heraus wie angegeben. Der Restzweifel 3 des Berichts wiegt damit etwas schwerer als dort beschrieben.

### 4.5 Unfairer Vergleichsmaßstab (Validierungsbericht, Einspaltentabelle)

Die Spalte „Einspaltig" bewertet nur die **Referenzgröße** (72 Zellen je Fall), die Spalte „Normband" alle drei Größen (216 Zellen je Fall). Ich habe beide Werte reproduziert: die zwölf Zahlen 0,0553 / 0,0516 / 0,0590 / 0,0558 / 0,0579 / 1,4986 / 0,6389 / 0,0504 / 0,1363 / 0,1430 / 4,9183 / 0,0538 entstehen **genau dann**, wenn man auf die Referenzgröße einschränkt. Bezieht man die operative Temperatur ein, ergäbe sich für Fall 10 bereits einspaltig **0,1680 K in 2 von 216 Zellen**, also ein Fehlschlag auch nach altem Maßstab.

Die Schlussfolgerung „das Normband ist die schärfere Prüfung" habe ich gleichbasig nachgeprüft (Band nur auf die Referenzgröße angewandt) — sie hält: Fall 6 Last 14/72 gegen 0/72, Fall 9 Luft 1/72 gegen 0/72, Fall 10 Luft 8/72 gegen 0/72, Fall 11 Last 4/72 gegen 2/72.

### 4.6 Zwischen den beiden Berichten: keine sachlichen Widersprüche

Alle querverweisenden Zahlen reproduzieren:

| Aussage | Abgleich | Validierung | nachgeprüft |
|---|---|---|---|
| Fall 6, Tag 1, Std. 7: Norm +764 W, AixLib/Prototyp −764 bzw. −765,48 | ✓ | ✓ | ✓ (Norm S. 50; `referenz\testfall_6.json` t = 25200) |
| Fall 11 folgt Programm 2 | ✓ | ✓ (Einspaltenbewertung gegen P2) | ✓ (P2 zeichengleich, P1 um 1 W daneben) |
| 62 Zellen mit P1 ≠ P2, jede Differenz genau 1 W bzw. 0,1 K | ✓ | „wenige Dutzend" | ✓ (62: 6 Last, 23 Luft, 33 operativ; max 1 W / 0,1 K) |
| davon 29 trennscharf in der Referenzgröße | ✓ | — | ✓ (29, einzeln aufgelistet) |
| Fall 11: genau eine Laststunde mit P1 ≠ P2 (Tag 60, Std. 10) | ✓ | ✓ | ✓ (einzige Zelle in Fall 11 überhaupt) |
| Fall 3, Tag 10, Std. 7: 6020 = 48,7 °C, Prototyp 50,90, Norm P1 50,9 | (Std. 7 Tag 1) | ✓ | ✓ |

Eine einzige Reibung, kein Widerspruch: der Abgleichsbericht führt in Restzweifel 2 die „Lufttemperatur der Fälle 6, 7, 11 (432 Zellen)" als extern unbelegt. In Fall 6 ist die Luft durch die unbegrenzte ideale Regelung identisch mit dem Sollwert, in Fall 11 in den geregelten Stunden ebenfalls — diese Zellen sind trivial und tragen kein Leserisiko. Der Zweifel ist an dieser Stelle größer formuliert als er ist.

---

## 5) Was ein Konzeptpapier übernehmen darf

### Belastbar, mit Zahl (jede Zeile hier nachgerechnet)

1. **Prüfvorschrift.** VDI 6007 Blatt 1, Abschnitt 6.6 verlangt Ergebnisse im Bereich von Programm 1 und Programm 2 **±0,1 °C** für Raumluft- und operative Temperatur, **±1 W** für Heiz- und Kühllasten. Das Band ist `[min(P1,P2) − tol, max(P1,P2) + tol]`.
2. **Stundendefinition.** Die n-te Stunde ist das Stundenmittel von (n−1):00 bis n:00 Uhr (Norm S. 38: 11. Stunde = 10:00–11:00). Prototyp-CSV, AixLib-Zeitbasis und Vergleicher folgen dieser Definition.
3. **Prüfumfang.** **2592 Zellen** (12 Fälle × 3 Größen × 3 Tage × 24 Stunden), davon **2553 innerhalb**, **39 außerhalb** des Bands.
4. **Ergebnis je Prüfung: 30 von 36 bestanden, 6 nicht** — Fall 6 Last, Fall 9 Luft, Fall 9 operativ, Fall 10 Luft, Fall 10 operativ, Fall 11 Last. **Nicht die im Validierungsbericht genannten 31/5.**
5. **Betroffene Fälle: 4 von 12** (6, 9, 10, 11); **8 von 12 Fällen liegen vollständig im Band.**
6. **Größenordnung der Fehlschläge.** Fall 6 Last max. **0,4986 W** in 14 Stunden; Fall 9 max. **0,0112 K** (Luft) bzw. **0,0063 K** (operativ) in je 1 Stunde; Fall 10 max. **0,0240 K** (Luft, 8 Stunden) bzw. **0,0598 K** (operativ, 11 Stunden); Fall 11 Last max. **3,9183 W** in 4 Stunden.
7. **Fall 11 ist der einzige substanzielle Befund**, lokalisiert in **Stunde 10 der Tage 10 und 60**, der Umschaltstunde Heizen → Kühlen.
8. **Einzige Stunde, in der die beiden Normprogramme in Fall 11 selbst uneins sind:** Tag 60, Stunde 10 (−121 W gegen −122 W).
9. **Vorzeichenkonvention.** Die Norm führt Heizlast positiv. Ein Vergleich gegen die AixLib-Referenz erfordert **nur in Fall 6** die Multiplikation mit −1; Belegwert: Norm +764 W, AixLib −764 W (Tag 1, Stunde 10 Uhr … 11 Uhr — in der Zählung der Norm die 7. Stunde).
10. **Externer Beleg der Lesung.** Die AixLib-Reihen decken **864 Zellen** (12 × 3 Tage × 24 Stunden, je eine Programmspalte) zeichengleich ab; Zuordnung Programm 1 für die Fälle 1–10 und 12, **Programm 2 für Fall 11**.
11. **Konsolidierter Normdatensatz: 6192 Zellen**, aus zwei unabhängigen Lesungen plus Drittlesung ohne Abweichung; Fälle 1–7 je 576, Fälle 8–12 je 432 Werte.
12. **Der Prototyp rechnet VDI 6007, nicht VDI 6020.** Gegen die VDI-6020-Spalte weicht er in Fall 3 um bis zu **2,198 K** ab (Tag 10, Stunde 7: 6020 = 48,7 °C gegen 50,90 °C), in Fall 6 um bis zu **66,45 W**. Das ist ein Verfahrensunterschied, kein Fehler.
13. **Laufzeit.** 12 Fälle × 1440 Schritte in **1,32 s** inklusive Prozessstart; hochgerechnet auf 8760 Schritte **6,2 ms** (Fall 12) bis **120,4 ms** (Fall 7) je Fall.

### Nicht übernehmen

1. **„31 von 36 bestanden" / „vier der fünf Fehlschläge"** — falsch, es sind 30/36 und fünf von sechs (Punkt 4 oben).
2. **„Fall 5, Abweichung gegen VDI 6020: 0,458 K"** — richtig sind **0,5579 K**; die Tabelle mischt Bandüberschreitung und Absolutabweichung.
3. **„In Fall 6 liegt der Prototyp in den Stunden 7–18 durchweg höher und 19–6 durchweg tiefer"** — ein Gegenbeispiel (Tag 1, Std. 15: −0,0246 W) und sechs Nullstunden. Als „phasenstarr in 65 von 72 Stunden" formulierbar, nicht als ausnahmslose Regel.
4. **„ATransparent = 7 m² in den Fällen 5, 8, 9, 10, 12"** und **„hConInt = 2,24 in den Fällen 1–9"** — in den Fällen 8/9 sind es 14 m² bzw. 2,12, in Fall 10 hConInt = 2,398.
5. **Die Einspalten-/Normband-Gegenüberstellung als Zahlenpaar** — die beiden Spalten haben unterschiedliche Grundgesamtheiten (72 gegen 216 Zellen). Die *Aussage* „das Normband ist schärfer" ist gleichbasig nachgeprüft und darf stehen; die Zahlenpaare nicht ohne den Hinweis auf die Grundgesamtheit.
6. **„Fall 10 / operativ / Tag 60 / Std. 14 überschreitet"** (0,0001 K) und **„Fall 11 / Tag 10 / Std. 11"** (0,0005 W) — als Fehlschläge nicht belastbar; der Validierungsbericht sagt das selbst, ein Konzeptpapier darf sie nicht mitzählen.
7. **Die Ursachenerklärung zu Fall 11** („rund 45 s zu frühe Umschaltung wegen fehlender Auflösung der Kühldecke") — plausibel und diagnostisch belegt, aber unbewiesen. Nur als Hypothese zitieren.
8. **Die Rundungsdiskussion** („nimmt man die Druckrundung hinzu, verschwinden die Fehlschläge der Fälle 9 und 10") — eine Auslegungsfrage zu Abschnitt 6.6, keine Feststellung. Wenn erwähnt, dann als offene Frage mit beiden Lesarten.
9. **Zahlen aus den gedruckten Nutzungsprofil-Tabellen der Textfassung** (Sollwert- und Lastzeitpunkte) — die Extraktion ist dort um eine Zeile versetzt (Abschnitt 3 oben). Die Zeitprofile nur aus den Ergebnisspalten oder aus `referenz\testfall_*.json` belegen.
10. **„Die operative Temperatur ist extern belegt"** — sie ist es nicht. 1728 Zellen hängen allein an den drei übereinstimmenden Lesungen; AixLib deckt sie nicht ab. Dieser Restzweifel beider Berichte ist korrekt und gehört ins Konzeptpapier übernommen, nicht weggelassen.