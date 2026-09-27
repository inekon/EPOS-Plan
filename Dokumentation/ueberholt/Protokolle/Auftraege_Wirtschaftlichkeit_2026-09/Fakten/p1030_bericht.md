# Bericht P1030: Sichtprüfung der Betriebskosten des Referenzprojekts 1030 (Konzept § 6.3 Nr. 21)

Stand 26.09.2026. Worktree `.claude/worktrees/p1030` (Zweig `p1030`, HEAD 6324e65d), Testdatenbank
`Referenzlaeufe/Kenndaten_Test.sqlite`. Gelesen wurde nur eine Kopie im Scratchpad, über ein dotnet-Dateiskript
(`Microsoft.Data.Sqlite`, `Mode=ReadOnly`). Kein Code geändert, keine Daten geändert, kein Commit, keine Testläufe.

## Vorbemerkung: R20 enthält keine Wirtschaftlichkeit

Der Referenzlauf R20 schreibt für 1030 **keine Betriebskosten-CSV, keinen Kapitalwert und keine Kostenzeile**. Im
Ordner `Referenzlaeufe/2026-09-26_R20_Zapfprofil/Projekt_1030/` liegen 22 Zeitreihen-CSV und `aggregate.csv` mit 160
Skalaren (`protokoll.txt:104`). Darunter sind nur Energie, Puffer und Emissionen, keine Kosten. Die anderen 13 Projekte
haben ebenfalls keine Kosten-Größe (Suche nach „Betriebskost|Kapitalwert|Wirtschaft|Invest" im ganzen R20-Ordner:
0 Treffer).

Die im Auftrag genannte „Betriebskostenzeile laut R20" gibt es also nicht. Ersatzweise habe ich die Summe gegen drei
andere Werte gehalten:

- den Kernanker `WirtschaftlichkeitAnkerTests.cs:331`: `BetriebskostenJahr` = 20.000,00 €/a, Erwartet. Er rechnet mit
  dem gebuchten Lauf `Tab_Ergebnis.ID = 212`.
- den A/B-Wert aus E16 (Register R‑E16, E16‑Q3, `Entscheidungsregister…md:655`): „p. a. bleibt 20.000 €/a".
- eine Handrechnung nach dem Leseweg `WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe`
  (`EPOS.Kern/Allgemein/Wirtschaftlichkeit/WirtschaftlichkeitCtrl.cs:7125` ff.).

Die Betriebskosten von 1030 hängen **nicht vom Lauf ab**. Keine laufabhängige Position trägt einen Satz, und diese
Positionen fallen deshalb auf ihren erfassten Betrag 0 zurück (siehe unten). R20 und der gebuchte Lauf 212 ergeben
daher dieselben Betriebskosten.

## 1 Positionstabelle

Quelle: `Tab_ProjektWerte` mit `ProjektID = 1030` und `KategorieID = 2`, Spalten `EingegebenerWert`, `Bemessung`,
`Einheitpreis` (= Satz), `Menge`, `ID_Anlage`, `VorlageID`, `IstPflicht`, `StartJahr`, `Wiederholperiode_a`.
Bezeichnung aus `Tab_Kostenfaktor.Bezeichnung`.

Für alle 14 Zeilen gilt:
- `Worstcase` = `Bestcase` = 0, also greift in allen drei Szenarien der Erwartet-Wert (`WirtschaftlichkeitCtrl.cs:7589`).
- `StartJahr` ist leer, die Zahlung beginnt also in Jahr 1.
- `Wiederholperiode_a` ist leer, die Zahlung ist also jährlich.
- Die Spalte „Betrag p. a." enthält den gerechneten Wert (Kernanker/Handrechnung), denn R20 führt keinen.

| # | ID | Position (Gruppe) | Anlage | Bemessung | Satz | Bezug (Menge) | Betrag p. a. | Herkunft | Periode |
|---|---|---|---|---|---|---|---|---|---|
| 1 | 101600097 | „BHKW" (Gruppe „Wartung BHKW") | 14920 EW M 50 S; die Gruppe gilt für die ganze Kaskade | `BETRAG` | — | — | **18.000,00 €** | Pflege (Altbestand, keine Vorlage; der Katalog `Tab_BHKW.Wartungskosten_kwhel` ist 0) | jährlich |
| 2 | 101600098 | „Heizkessel" (Gruppe „Wartung Kessel") | 11334 Vitocrossal 200 CM2, 2.200 kW | `BETRAG` | — | — | **2.000,00 €** | Katalog: gleich `Tab_Heizkessel.Wartungskosten` 2.000 €/a (ID 1018330) | jährlich |
| 3 | 101600588 | Wartung BHKW (Pflicht) | 14920 | `EUR_PRO_KWH_ELEKTRISCH` | leer | leer (frisch wären es 373,8 MWh el) | 0,00 € | Vorbelegung Vorlage 11 (Position 54, ohne Satz) | jährlich |
| 4 | 101600589 | Instandhaltung BHKW (Pflicht) | 14920 | `PROZENT_INVESTITION` | leer | leer | 0,00 € | Vorlage 11 (Position 55, Empfehlung 3–9 %) | jährlich |
| 5 | 101600590 | Hilfsenergiekosten (Pflicht) | 14920 | `PROZENT_ENDENERGIEKOSTEN` | leer | frisch aus dem Lauf | 0,00 € | Vorlage 11 (Position 62, dort aber `PROZENT_ENDENERGIEBEDARF`, 2–4 %) | jährlich |
| 6 | 101600591 | Wartung BHKW (Pflicht) | 14921 XRGI 9 | `EUR_PRO_KWH_ELEKTRISCH` | leer | leer (frisch wären es 58,5 MWh el) | 0,00 € | Vorlage 11 | jährlich |
| 7 | 101600592 | Instandhaltung BHKW (Pflicht) | 14921 | `PROZENT_INVESTITION` | leer | leer | 0,00 € | Vorlage 11 | jährlich |
| 8 | 101600593 | Hilfsenergiekosten (Pflicht) | 14921 | `PROZENT_ENDENERGIEKOSTEN` | leer | frisch | 0,00 € | Vorlage 11 (Bemessung weicht ab wie bei Nr. 5) | jährlich |
| 9 | 101600585 | Vollwartung / Wartung Kessel (Pflicht) | 11334 | `EUR_PRO_KWH_THERMISCH` | leer | leer (frisch wären es 5.403 MWh th) | 0,00 € | Vorlage 12 (Position 65) | jährlich |
| 10 | 101600586 | Instandhaltung Heizkessel (Pflicht) | 11334 | `PROZENT_INVESTITION` | leer | leer | 0,00 € | Vorlage 12 (Position 66, 1,5–2,5 %) | jährlich |
| 11 | 101600587 | Hilfsenergiekosten (Strom) (Pflicht) | 11334 | `PROZENT_ENDENERGIEKOSTEN` | leer | frisch | 0,00 € | Vorlage 12 (Position 68, dort `PROZENT_ENDENERGIEBEDARF`, 4–8 %) | jährlich |
| 12 | 101600582 | Wartung / Sichtprüfung Speicher (Pflicht) | 11331 Puffer 20 m³ | `JAHRESBETRAG` | — | — | 0,00 € | Vorlage 15 (Position 91) | jährlich |
| 13 | 101600583 | Instandhaltung Pufferspeicher (Pflicht) | 11331 | `PROZENT_INVESTITION` | leer | leer | 0,00 € | Vorlage 15 (Position 92, ohne Empfehlung) | jährlich |
| 14 | 101600584 | Hilfsenergiekosten (Speicherladepumpe) (Pflicht) | 11331 | `JAHRESBETRAG` | — | — | 0,00 € | Vorlage 15 (Position 95) | jährlich |
| | | **Summe** | | | | | **20.000,00 €/a** | | |

Die Investitionen der Kategorie 1 bilden die Basis für Prozentbemessungen:
- BHKW 295.000 € an Anlage 14920, Gruppe „Investition BHKW-Kaskade". Anlage 14921 hat keine eigene Investition.
- Kessel 90.000 €.
- Puffer 25.000 €.
- Sonstiges 0 €.
- Zusammen 410.000 € (`InvestKaskadeTests.cs:359`). Konzept § 6.2 Zeile 2924 nennt noch `:281`; diese Stelle ist
  veraltet.

## 2 Fachliche Plausibilität je Position

Grundlage ist Konzept § 3.4 (`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md:1866`):
- Einheitlicher Rechenweg: Fehlt Menge oder Satz, gilt der erfasste Betrag (`BetriebskostenCtrl.cs:77`).
- Sätze der Nutzungsdauertabelle (Zeile 1938): Die Tabelle rechnet nicht selbst (E10‑Q1 a, ND‑Q4).
- Saaten in `Tab_Nutzungsdauer`: BHKW Modul 6 %, Heizkessel 2 %, Wärmezentrale 2 %. Für Pufferspeicher gibt es
  keinen Satz. Kein Eintrag hat einen Wartungssatz.

Die Branchenrichtwerte für die BHKW-Wartung (ASUE-Größenordnung) sind meine fachliche Einschätzung. Das Repo führt sie
nicht.

- **Nr. 1, Wartung BHKW-Kaskade 18.000 €/a: in Ordnung, am oberen Rand.**
  - 18.000 € ÷ 432,3 MWh el (R20 `BHKW.Stromproduktion`) ergibt 4,16 ct/kWh el.
  - Für ein 50-kW-Modul sind 2,5–3,5 ct/kWh üblich, für ein 9-kW-Modul 5–7 ct/kWh. Gewichtet ergibt das etwa
    3,4 ct/kWh bzw. rund 14.700 €/a.
  - 18.000 € sind zugleich 6,1 % der BHKW-Investition. Das entspricht der Saat „Instandsetzung BHKW 6 %" der
    Nutzungsdauertabelle und liegt in der Vorlagen-Empfehlung 3–9 %.
  - Als Vollwartungsvertrag (Wartung mit Instandsetzung) ist der Betrag stimmig. Deshalb ist es folgerichtig, dass
    „Instandhaltung BHKW" (Nr. 4 und 7) leer bleibt.
  - Unschärfe: Der Betrag hängt nur an 14920, obwohl er die Kaskade meint (Gruppe „Wartung BHKW", wie die Investition
    „Investition BHKW-Kaskade"). Anlagenscharfe Ausweise zeigen für 14921 deshalb keine Wartung.
  - Die Bezeichnung lautet „BHKW" (Kostenfaktor 83, ein Investitions-Stammfaktor) statt „Wartung BHKW". Die
    Betriebskostentabelle der Berichte zeigt die Zeile als „BHKW · fester Betrag".
- **Nr. 2, Wartung Kessel 2.000 €/a: in Ordnung.** Der Betrag entspricht dem Katalogwert des Kessels. Er macht 2,2 %
  der Investition von 90.000 € aus und passt für Wartung und Inspektion eines 2,2-MW-Gaskessels nach VDI 2067
  (Größenordnung 1,5–2 %).
- **Nr. 10, Instandhaltung Heizkessel 0 €: Unschärfe (Lücke).** Die Saat der Nutzungsdauertabelle wäre 2 % ×
  90.000 € = 1.800 €/a, die Vorlagen-Empfehlung 1,5–2,5 %. Nr. 2 deckt nur Wartung und Inspektion ab. Die Lücke ist
  nach E10‑Q1 a gewollt: kein Satz ohne Zutun.
- **Nr. 12 und 13, Puffer 0 €: Unschärfe (gering).**
  - Die Nutzungsdauertabelle hat für den Pufferspeicher keinen Satz (Zeile 22, Komponente 6), die Vorlage 15 auch
    keine Empfehlung.
  - Fachlich wären 1–2 % von 25.000 € angemessen, also 250–500 €/a.
- **Nr. 5, 8, 11 und 14, Hilfsenergie 0 €: Unschärfe mit spürbarem Gewicht.**
  - Weder die Betriebskosten noch die Energiekosten führen Hilfsstrom. `Tab_ErgebnisBHKWModul.Hilfsenergie` und
    `Tab_ErgebnisHeizkesselModul.Hilfsenergie` sind 0, ebenso `aggregate.csv:81,89,121`.
    `Tab_Energieanlagen.Hilfsenergie_Anteil` ist leer.
  - Größenordnung, wenn man 0,5–1 % der Wärme als Hilfsstrom zum Strompreis von 0,25 €/kWh ansetzt:
    - Kessel: 5.403 MWh × 0,5–1 % × 0,25 €/kWh = 6.800–13.500 €/a.
    - BHKW: rund 1.000–2.000 €/a.
  - Dazu kommt ein Befund in den Daten: Die fünf Hilfsenergiezeilen von BHKW und Kessel tragen
    `PROZENT_ENDENERGIEKOSTEN` (Weg A). Ihre Vorlagen 11 und 12 führen `PROZENT_ENDENERGIEBEDARF` (Weg B) samt
    Empfehlung 2–4 % bzw. 4–8 %.
  - Nach § 3.4 sind die Sätze beider Wege nicht austauschbar. Bei 1030 liegt das Verhältnis bei etwa 0,25 zu
    0,08 €/kWh, also Faktor ≈ 3.
  - Übernähme jemand die Empfehlung der Vorlage in diese Zeilen, wäre der Betrag um diesen Faktor zu klein. Umgekehrt
    ergibt die Vorlagen-Empfehlung 4–8 % in Weg B am Kessel 54.000–108.000 €/a. Das ist fachlich deutlich zu hoch,
    ist aber eine Frage des Katalogs und nicht von 1030.
- **Nr. 3, 6 und 9, Pflichtpositionen Wartung ohne Satz, 0 €: in Ordnung, solange sie leer bleiben.** Sie decken
  dasselbe ab wie Nr. 1 und 2.
  - Wer später einen Satz pflegt, etwa 3 ct/kWh el an Nr. 3 und 6 oder den Kesselwert an Nr. 9, bucht die Wartung
    **doppelt**: rund +13.000 €/a beim BHKW bzw. einen €/kWh-Satz zusätzlich zu den 2.000 € beim Kessel.
  - Eine Doppelbuchung liegt heute nicht vor. Die Doppelstruktur aus Altzeile und Pflichtzeile ist aber angelegt.
- **Nr. 4 und 7, Instandhaltung BHKW 0 €: in Ordnung**, siehe Nr. 1.
  - Das Register E10‑Q1 (`Entscheidungsregister…md:597`) hat die Gegenprobe schon gemessen: Lesart b hätte
    +37.200 €/a ergeben, nämlich 17.700 (14920) + 17.700 (14921) + 1.800 (Kessel).
  - Davon wären 17.700 €/a doppelt gezählt, weil 14921 über die Stufung H4a die Kaskaden-Investition von 14920
    erbt, und dazu doppelt zur Vollwartung in Nr. 1.
  - Wer an 1030 „Sätze vorbelegen…" auslöst, erzeugt genau diese Doppelung.
- **Fehlende Positionen** (in den Vorlagen optional, bei 1030 nicht angelegt): Personal bzw. Bedienung (1–4 % der
  Investition), Steuern, Versicherung und Verwaltung (0,8–2 %), Schornsteinfeger bzw. Messung, Wasserbehandlung.
  - Zusammen wären das nach den Vorlagen rund 7.000–25.000 €/a auf 410.000 €.
  - Für ein Regressionsprojekt ist das keine Pflicht. Für eine „fachlich abgenommene" Betriebskostenseite nach
    VDI 2067 sind sie aber eine erkennbare Lücke.
- **Wiederholperiode und Startjahr:** Keine Zeile ist gepflegt, also zahlt jede jährlich ab Jahr 1. Das ist
  sachgerecht, denn keine Position ist ihrer Natur nach n-jährlich. Die Kesselmessung wäre ein Kandidat, ist aber nicht
  angelegt.
- **Szenarien:** Best und Worst sind 0, also gleich Erwartet. Die Preissteigerung des Betriebs beträgt 1,5 %/a und ist
  ohne Szenariopaar gepflegt (`Tab_ProjektWirtschaftlichkeit`: `Szen_*_Preis_B` leer). Die Betriebskosten haben
  deshalb keine Bandbreite. Das ist in Ordnung und entspricht der Nullsemantik von W5‑B‑9.

## 3 Rechenprobe

| Größe | Wert |
|---|---|
| Summe der Positionen (Tabelle oben) | 18.000,00 + 2.000,00 + 12 × 0,00 = **20.000,00 €/a** |
| Kernanker `BetriebskostenJahr` 1030, Erwartet (`WirtschaftlichkeitAnkerTests.cs:331`) | **20.000,00 €/a** |
| A/B aus E16 (R‑E16, E16‑Q3) | 20.000 €/a |
| Best und Worst (Nullsemantik `WirtschaftlichkeitCtrl.cs:7589`) | je 20.000,00 €/a |
| Abweichung | **0,00 €** |

Der Rechenweg im Einzelnen:
- Nr. 1 und 2 laufen als `BETRAG` im Bestandsweg.
- Nr. 12 und 14 sind `JAHRESBETRAG` mit dem Wert 0.
- Die übrigen zehn Zeilen haben keinen Satz. `BetriebskostenCtrl.Betrag` nimmt für sie nach I‑2 den erfassten Wert 0
  (`BetriebskostenCtrl.cs:77`), auch wo die Menge frisch ermittelbar wäre.
- Der Endenergie-Topf (p_E) bleibt deshalb bei 0. Die ganzen 20.000 € laufen im Betriebs-Topf mit p_B = 1,5 %/a.
- Die Gliederungsprobe (§ 3.4, Zeile 1974) geht auf, weil es keine Startjahr- oder Periodenposition gibt.

Wirkung auf den Kapitalwert (Handrechnung): i = 3 %, T = 20 a, p_B = 1,5 % ergibt einen Barwertfaktor von rund
16,95. Der Barwert der Betriebskosten liegt damit bei etwa **339.000 €**, also rund 1,1 % des Kapitalwerts von
−31,14 Mio. € (Anker E27, `PvAusweisStromMatrixTests.cs:243`). Die Energiekosten von rund 1,18 Mio. €/a dominieren den
Kapitalwert. Die Betriebskosten sind für den Anker praktisch zweitrangig.

## 4 Befundliste

| # | Befund | Schwere | Empfehlung | Aufwand |
|---|---|---|---|---|
| B1 | R20 führt für 1030 (und alle Projekte) keine Wirtschaftlichkeit. Die im Auftrag und in § 6.3 Nr. 21 gedachte Probe „Betriebskosten gegen R20" ist so nicht möglich. Die Betriebskosten sind stattdessen im Kern verankert (20.000,00 €/a, `WirtschaftlichkeitAnkerTests.cs:331`). | Unschärfe | Konzeptvermerk: § 6.2 (Zeile 2924 f., „die Betriebskosten von 1030 tragen weiterhin keinen Anker") und § 6.3 Nr. 21 berichtigen. Der Kernanker 20.000,00 €/a existiert. Nr. 21 nach dieser Sichtprüfung als abgenommen schließen, mit Verweis auf diesen Bericht. Den Anker in die Tabelle § 6.2 aufnehmen. | 15 min (Papier) |
| B2 | Summe 20.000,00 €/a gleich Anker, Abweichung 0. Die Beträge von BHKW-Vollwartung (4,2 ct/kWh el bzw. 6,1 % der Investition) und Kesselwartung (Katalogwert 2.000 €/a) sind fachlich plausibel. | in Ordnung | nichts | — |
| B3 | Doppelstruktur Altzeile/Pflichtzeile: Wartung BHKW (Nr. 1 gegen Nr. 3 und 6) und Kessel (Nr. 2 gegen Nr. 9). Heute ohne Doppelbuchung, nach Pflege eines Satzes oder nach „Sätze vorbelegen…" doppelt (bis +37.200 €/a, davon 17.700 €/a Kaskaden-Doppelung über 14921, vgl. E10‑Q1). | Unschärfe | Datenpflege (Anwenderentscheid, weil die Werte eingefroren sind): die 18.000 € und die 2.000 € in die Pflichtzeilen Nr. 3 und Nr. 9 als `JAHRESBETRAG` umziehen und die Altzeilen löschen, oder die Pflichtzeilen als „in Nr. 1/2 enthalten" vermerken. Ergebnisneutral, wenn richtig gemacht. Alternativ nur Konzeptvermerk „1030 nicht mit ‚Sätze vorbelegen' behandeln". | Vermerk 10 min; Pflege 30–45 min samt Anker- und Referenzprobe |
| B4 | Hilfsenergie ist weder in den Betriebs- noch in den Energiekosten angesetzt (alle Hilfsenergie-Größen 0). Geschätzt fehlen 8.000–15.000 €/a. | Unschärfe (fachlich die größte Lücke) | Konzeptvermerk bei 1030 („Hilfsenergie bewusst 0 – Regressionsprojekt"). Eine Pflege würde den Anker verschieben und bräuchte einen Anwenderentscheid. | 10 min (Vermerk) |
| B5 | Bemessung der fünf Hilfsenergie-Pflichtzeilen `PROZENT_ENDENERGIEKOSTEN` (Weg A), ihre Vorlagen 11 und 12 führen `PROZENT_ENDENERGIEBEDARF` (Weg B) mit Empfehlungen, die für Weg B gelten. Ein aus der Empfehlung übernommener Satz wäre um Faktor ≈ 3 falsch. Dasselbe gilt für 1026 (StammID 147). Die Empfehlung 4–8 % für den Kessel in Weg B ist selbst fachlich hoch. | Unschärfe | Datenpflege: Bemessung der Pflichtzeilen auf die der Vorlage angleichen (ohne Satz ergebnisneutral). Konzeptvermerk bzw. Katalogprüfung der Empfehlung „Hilfsenergiekosten (Strom)" 4–8 %. | 20 min Pflege + Probe; Katalogprüfung eigener kleiner Auftrag |
| B6 | Instandhaltung Kessel und Puffer 0 € (Saat 2 % ergäbe 1.800 €/a, Puffer rund 250–500 €/a). Optionale VDI-2067-Posten (Bedienung, Versicherung und Verwaltung, Messung) fehlen. | Unschärfe | nichts am Referenzprojekt, weil das nach E10‑Q1 a und ND‑Q4 gewollt ist. Konzeptvermerk: „1030 ist ein Regressionsprojekt, kein vollständiges VDI-2067-Beispiel". | im Vermerk B1 enthalten |
| B7 | Die 18.000 € hängen nur an Anlage 14920 und meinen die Kaskade. Bezeichnung „BHKW" (Investitions-Stammfaktor 83) statt „Wartung BHKW". | Unschärfe (Ausweis) | mit B3 erledigen; sonst nichts | — |
| B8 | Randbefund außerhalb der Betriebskosten: § 6.2 führt als Kapitalwert-Anker 1030 nur −21.895.377,28 € (Kern mit gebuchtem Lauf 212, `WirtschaftlichkeitAnkerTests.cs:328`). Seit E27 steht daneben −31.142.971,06 € (Weg über `BerichtsDatenSammler.Sammle`, `PvAusweisStromMatrixTests.cs:243`). Die Ursache der Differenz von rund 9,2 Mio. € habe ich nicht untersucht. Der gebuchte Lauf 212 weicht im BHKW-Brennstoff von R20 ab (1.048,27 gegen 1.241,55 MWh). Die Betriebskosten sind davon nicht berührt. | Unschärfe (Papier) | Konzeptvermerk: beide Anker mit ihrem Weg in die Tabelle § 6.2 aufnehmen | 10 min |

Es gibt **keinen Fehler** im Sinne einer falschen Rechnung oder falscher Zahlen. Die Betriebskosten von 1030 können
fachlich als „plausibel, bewusst unvollständig" abgenommen werden (B2), mit den Vermerken B1, B4 und B6.

## 5 Aufwand

- Diese Sichtprüfung: rund 45 min (lesen, 6 Leseabfragen auf der Kopie, Handrechnung).
- Empfohlene Nacharbeit, nur Papier (B1, B4, B6, B8): rund 45 min in einem Papierauftrag.
- Optionale Datenpflege (B3, B5): rund 1 h mit Anker- und Referenzprobe. Dafür braucht es einen Anwenderentscheid,
  weil 1030 eingefroren ist und CI-Projekt ist.
