# Befund AK3 — Restwärmebedarf am Kessel, Zeitprogramm beim Pufferladen, Kessel deckt den Rest nicht

Geplanter Prüflauf, 17.10.2026, Zweig `befund/ak3-kessel-zeitprogramm` (von `ios_migration_september` `8e7ff3b8b`).
Basis `Referenzlaeufe/2026-10-17_R58_Kaeltesummen`. Testdatenbank unverändert, kein Schemaschritt, keine neue
SQL-Anweisung. Rechenweg unverändert; Umsetzung nur in der Anzeige (Befund a). Lokaler Referenzlauf läuft
(SDK 10.0.400): Projekt 1059 gegen R58 PASS.

Versuchsreihe (c): Kopien der Testdatenbank, Projekt 1059 je Variante umgestellt, gerechnet mit
`EPOS.Referenzlauf lauf --projekte 1059`. Skripte und Ergebnisse lagen nur im Scratchpad der Sitzung
(`varianten.py`, `auswerten.py`), nicht im Repositorium.

## (a) Restwärmebedarf am Heizkessel im AK3-Pufferkreis

**Beobachtung (R58, 1059).** `kessel_waermebedarf` 70,13 MWh, in 8 759 von 8 760 Stunden gleich `puffer_entladung`;
Kessel 2,37 MWh, `kessel_restwaerme` 57 kWh, `Sim.Restwaerme` 0. Anwenderprojekt Lauf 2: Restwärmebedarf nach
Kessel 66,50 MWh, davon aus Puffer (andere Erzeuger) 62,58 MWh, ungedeckt also 3,92 MWh.

**Codebelege.**
- Stufeneingang = Kanalstand vor Phase A: `SimulationSPK.Stunde_Start` (`EPOS.Kern/Allgemein/Simulation/SimulationSPK.cs:1629-1648`),
  gerufen für alle Schleifenmitglieder gemeinsam vor Phase A (`Kaskadenschleife.cs:1092-1096`).
- `Tab_ErgebnisHeizkessel.Restwaermebedarf` = Stufeneingang − Eigenanteil (Direktdeckung + zugerechnete
  Speicherentladung): `SimulationRunner.cs:1031-1035`.
- Die Reihe `kessel_restwaerme` steht nach Phase B, vor Laden und Entladen (`SimulationSPK.cs:1763`); ein Kessel ohne
  Direktsenke deckt dort nichts — sie taugt nicht als „ungedeckt“.
- Das Kesselbild teilt den Stufeneingang bereits in drei Teile: Kesselwärme, aus Puffer (andere Erzeuger),
  übrige Erzeuger / ungedeckt (`SimulationErgebnisCtrl.KesselbildReihen`, `EPOS.Kern/Controller/SimulationErgebnisCtrl.cs:742-790`).

**Ursache.** Die Größe ist positionsabhängig, nicht AK3-spezifisch: Ist der Kessel eine Vektorstufe ohne Speicher,
ist sein Stufeneingang der Rest nach den vorangehenden Stufen (R58: 1007, 1017, 1047, 1056 — Kessel-Wärmebedarf
klein). Ist er Mitglied der Kaskadenschleife (lädt einen Puffer), sehen alle Mitglieder denselben Kanalstand vor
Phase A, also den ganzen Bedarf (R58: 1039, 1040, 1042, 1045, 1058, 1059, 1066–1070 — Kessel-Wärmebedarf =
Wärmepumpen-Wärmebedarf). Im AK3-Pufferkreis geht der ganze Bedarf durch den Puffer; der Stufeneingang ist dann die
Pufferanforderung, und „Restwärmebedarf nach Kessel“ enthält die Leistung aller anderen Lader.

**Bewertung.** Anzeige. Die Zahlen sind richtig, die Zeile wird als Unterdeckung gelesen.

**Umsetzung (Commit `9cbecfbcc`).** Neue Zeile im Kessel-Reiter unter „davon aus Puffer (andere Erzeuger)“:
„davon übrige Erzeuger / ungedeckt“ / „of which other generators / uncovered“ (`SIMERG_LBL_DAVON_UEBRIGE_UNGEDECKT`),
Wert `HeizkesselErgebnis.UebrigeUngedecktMwh` = max(0, Restwärmebedarf nach Kessel − davon aus Puffer), dieselbe
Aufteilung wie die Reihe des Kesselbildes. Datenbankspalte und Referenz-CSV unverändert. Nachweis: Bau des
Kern-Filters grün, `ErzeugerReiterTests` (89) und die Ressourcentests grün.

**Offen.** Die Beschriftung „Restwärmebedarf (Stufeneingang)“ könnte für Schleifenmitglieder „Bedarf am
Schleifeneingang“ heißen; das ist eine Sprachentscheidung für alle Erzeugerreiter (WP, BHKW, Solar führen dieselbe
Logik) und nicht Teil dieses Befunds.

## (b) Zeitprogramm greift beim Pufferladen in AK3 nicht

**Beobachtung (R58, 1059).** Zeitprogramm an Kessel 23956 und BHKW 23957: Faktor 0 in den Stunden 0–5 jedes Tages.
Kessel 1 390 von 1 450 Laufstunden in den Stunden 0–5 (1,75 von 2,37 MWh), BHKW 948 von 984 (12,5 von 13,2 MWh);
Wärmepumpe (Sperrung 0–6 Uhr) in den Stunden 0–5 null. Dieselbe Saat tragen die AK3-Projekte 1058, 1061, 1062.
Projekt 1056 (AK1, Profilweg) zeigt in den Stunden 0–5 weder Kessel noch BHKW noch WP: Dort kappt der Fahrplan den
Gebäudebedarf selbst, die Erzeuger sehen nachts keinen Bedarf.

**Codebelege.**
- Das Zeitprogramm wirkt nur im Anlagenfahrplan bzw. im Stundenangebot, also in der gebäudeseitigen Schranke:
  `Anlagenfahrplan.cs:118-131` (Sperrstunde geht vor, sonst Faktor), `Stundenangebot.cs:180`; im AK3-Kreis zählen
  Kessel und BHKW als `FesteKapazitaet(z.Fahrplan)` (`SimulationControl.Ak3.cs:130-137`).
- Die Kaskadenschleife kennt es nicht: Der Kessel beginnt jede Stunde mit voller Nennleistung
  (`SimulationSPK.cs:1633`, `_restLeistung[i] = Kessel_Leistung_Spk[i]`), das Modul liest den Kessel nur über den
  Bezeichner (`SimulationSPK.cs:538-555`) und hat weder Anlagen-ID noch Zeitprogramm; das BHKW ebenso
  (`SimulationBHKW.Stunde_Start`, `SimulationBHKW.cs:1656-1679`). In `Kaskadenschleife.cs`, `Ladeordnung.cs`,
  `SimulationSPK.cs`, `SimulationBHKW.cs` steht kein Bezug auf `Zeitprogramm`.
- Die Sperrzeit der Wärmepumpe wirkt dagegen in ihrem eigenen Modul — daher ist sie nachts korrekt null.

**Ursache.** Im AK3-Kreis entnimmt das Gebäude nachts aus dem Puffer (die Schranke rechnet Kessel und BHKW mit 0,
den Speicherinhalt mit); die Kaskadenschleife lädt den Puffer danach mit Kessel und BHKW nach, ohne das
Zeitprogramm zu sehen. Schranke und Kaskade laufen auseinander. In AK1/AK2 bleibt der Fehler verdeckt, weil der
Fahrplan dort schon den Bedarf kappt; mit einem Puffer, den Kessel oder BHKW in Sperrstunden laden, träte er auch
dort auf.

**Bewertung.** Rechenweg.

**Vorschlag (nicht eingebaut).** Der Verfügbarkeitsfaktor gehört je Stunde auf die Abgabeleistung jedes Erzeugers in
der Kaskadenschleife: Kessel `_restLeistung[i] = Kessel_Leistung_Spk[i] · f(h)` in `Stunde_Start`; BHKW: Fahrweise
und Ladeauftrag erhalten die Höchstleistung `P_th · f(h)`, bei `f = 0` läuft die Maschine nicht; Wärmepumpe: Faktor
neben der Sperrzeit (Sperrzeit geht vor). Quelle der Faktoren: `Anlagenfahrplan.ErzeugerAusProjekt` (dort sind sie je
Anlage schon gelesen und geprüft); die Module brauchen dafür die Anlagen-ID statt nur des Bezeichners.
Nicht eingebaut, weil (1) drei Module betroffen sind, (2) ein Faktor zwischen 0 und 1 beim BHKW die
Modulationsgrenze berührt — laut `EPOS.Kern/CLAUDE.md` eine der drei Schwellen, deren Änderung einen Entscheid
braucht —, und (3) die Änderung die Basis für 1058, 1059, 1061, 1062 (und ggf. 1056) neu einfriert; ein Einfrieren
auf einem Nebenzweig kollidierte mit der Basisfolge auf `ios_migration_september`.
Testfall: Projekt 1059 rechnen; Summe von `kessel_leistung` und `bhkw_waerme` über alle Stunden `h % 24 < 6` muss 0
sein (heute 1 746 bzw. 12 527 kWh); dazu eine Kernprobe mit einem Kessel, Faktor 0,5 in einer Stunde und
Pufferladung: Abgabe ≤ 0,5 · Ptherm.

**Offen.** Entscheid zum Teilfaktor beim BHKW (Leistung kappen oder Laufzeitanteil); Meldung, wenn ein Erzeuger
mit Zeitprogramm in AK3 einen Puffer lädt, bis der Rechenweg folgt (heute nur Wertprüfungen
`SIMENG_AK2_ZEITPROGRAMM_*`).

## (c) Kessel deckt den Rest nicht, obwohl die Leistung reicht

### Versuche (Projekt 1059, BHKW aus der Kaskade, WP ohne Sperre, Kessel ohne Zeitprogramm, Ladeprio beider 0 = Vorgabe, Puffer 1 000 l 55/50, Schwellen 5/95/Nachrang 10/Reserve 10, Lade-/Entladeleistung 0, Aufheizoptimierung 1)

| Variante | WP MWh | Kessel MWh | Rest MWh (Stunden) | Puffer-SOC Mittel / Q_max kWh | Lauf |
|---|---|---|---|---|---|
| U0 ohne Übergabe | 72,66 | 1,55 (57 h) | 1,24 (246) | 0,54 / 5,8 | ok |
| U0b ohne Übergabe, Kessel Ladeprio 1 | 0,04 | 74,39 (8 760 h) | 2,47 (435) | 4,94 / 5,8 | ok |
| U8 ohne Übergabe, Puffer 70/50 | 72,83 | 1,66 | 0,97 (223) | 2,22 / 23,3 | ok |
| U1 mit Übergabe (Radiator) | — | — | — | — | **Abbruch** Stunde 468 |
| U2 mit, Kessel Ladeprio 1 | — | — | — | — | **Abbruch** Stunde 468 |
| U4 mit, Aufheizoptimierung 0 | — | — | — | — | **Abbruch** Stunde 468 |
| U5 / U6 mit, Puffer 55/45 bzw. 50/35 | — | — | — | — | **Abbruch** Stunde 468 |
| U3 mit, Puffer 70/50 | 68,59 | 1,56 (57 h) | 0,00 | 2,29 / 23,3 | ok |
| U7 mit, Puffer 10 000 l 55/50 | 68,57 | 1,63 | 0,00 | 5,75 / 58 | ok |

### (c1) Ohne Übergabe

**Mechanismus.**
1. Mit `Heizkreis_Aktiv = 0` ist das Gebäude nicht gekoppelt: `Waermeuebergabe.KopplungWirksamFuer` verlangt
   Heizkreis und eine nicht ideale Übergabeart (`Gebaeude/Waermeuebergabe.cs:243-246`), `Ak3Vorbereiten` endet ohne
   gekoppeltes Gebäude still (`SimulationWaermebedarf.Ak3.cs:64-75`), der AK2-Fahrplan ebenso
   (`SimulationWaermebedarf.cs:1391`). Das Gebäude geht als unbegrenzter VDI-6007-Idealbedarf samt Aufheizspitzen in
   die Kaskade. Das Protokoll meldet trotzdem „Anlagenkopplung ‚geschlossener Kreis‘: Gebäude und Kaskade rechnen je
   Stunde im geschlossenen Kreis“ — bei aktiver Kälteseite läuft der Kreis nur für die Kühlung
   (`Ak3_Durchlaeufe_Mittel` 1).
2. Die Lücke entsteht in den kältesten Stunden mit Aufheizspitze (Rest je Tagesstunde: 6 Uhr 620 kWh, 7 Uhr 325, 8 Uhr
   119 kWh, sonst fast null). Dort läuft die WP an ihrer Grenze (h441: −9,6 °C, Bedarf 59,8 kWh, WP 37,7 = Höchstwert bei
   dieser Temperatur), der Kessel liefert **0** — in allen 246 Reststunden, obwohl 28 kW frei sind. Er läuft nur in
   den 57 Stunden unter dem Abschaltpunkt −10 °C, in denen die WP aus ist. Spiegelbildlich U0b: Kessel vorrangig an
   seiner Grenze 28 kW, die nachrangige WP liefert in den Reststunden 0.
3. Nach dem Code müsste der zweite Lader die Lücke über den Durchlass füllen: `ladefaehig` ist bei Puffer über der
   Nachrangschwelle 0, aber `durchlass = min(Σ absehbar, Entnahmefaehigkeit())` (`Kaskadenschleife.cs:770-779`), mit
   `Entladeleistung_Max = 0` unbegrenzt (`SimulationPufferspeicher.cs:1050-1054`), `absehbar` = offener Bedarf nach
   Phase B (`Kaskadenschleife.cs:1213-1214`); `Zweikanalig_Laden` (`SimulationSPK.cs:1779-1804`) lädt
   `min(MaxAbgabe, ladefaehig + durchlass)`. Die gemessenen Reihen widersprechen dem: Die Stelle, an der der zweite
   Lader leer ausgeht (verbrauchter oder nicht gebuchter Durchlass, Ladeauftrag nicht erzeugt, Phase-B-Deckung der WP
   über den Puffer), ist **nicht abschließend gefunden**.

**Bewertung.** Konfiguration (keine Übergabe unter AK3, Wache fehlt) **und** Rechenweg (nachrangiger Lader füllt
die Lücke des vorrangigen nicht, reproduzierbar).

**Vorschlag.**
- Wache/Warnung: „Anlagenkopplung AK3, aber Gebäude ‚…‘ ohne Heizkreis/Übergabe — es rechnet ungekoppelt als feste
  Last“ (heute nur `SIMENG_AK_UEBERGABEART_IDEAL` bei aktivem Heizkreis mit idealer Übergabe); den Hinweis
  „geschlossener Kreis“ nur für tatsächlich gekoppelte Gebäude ausgeben.
- Rechenweg: Testfall U0 (Variante oben, Projekt 1059): Erwartung in jeder Stunde mit `restwaerme > 0` und
  `kessel_leistung < 0,98 · Ptherm` — keine solche Stunde. Heute 246 Stunden, 1,24 MWh. Spiegelprobe U0b für die WP.
  Ursache an der Durchlassbuchung zwischen erstem und zweitem Ladeauftrag am selben Puffer suchen
  (`DurchlassBuchen`, Reihenfolge der Ladephase, Reservierung `sp.Reserviert`).

### (c2) Mit Übergabe

**Befund.** Mit dem 1 000-l-Puffer (55/50, 55/45, 50/35) bricht jeder Lauf in Stunde 468 ab: „Der Kreis findet in
Jahresstunde 468 keine stabile Lösung … letzter Stand 20 Durchläufe, Vorlauf 50,4 °C, Schranke 28,6 kW … Der Lauf
bricht ab statt zu nähern“. Mit 10 000 l (55/50) oder mit 1 000 l bei 70/50 läuft er durch, Rest 0; der Kessel liefert
dann nur in den 57 Stunden unter −10 °C. Kesselvorrang und Aufheizoptimierung ändern am Abbruch nichts.
Ein Nachstellen der Anwenderzahl 3,9 MWh ungedeckt war damit nicht möglich.

**Nebenbefund Referenzlauf.** Trotz `Simulation FEHLER` meldet `EPOS.Referenzlauf` „OK — Erfolgreich: 1 von 1“ und
schreibt CSV aus dem abgebrochenen Lauf (Restwärme = ganzer Bedarf, Erzeugerergebnisse 0); auch ein Ergebniskopf
wird angelegt („Simulation beendet, Ergebnis-Kopf-ID …“). Vorschlag: Lauf mit Fehlermeldung im Protokoll als FAIL
werten (`EPOS.Referenzlauf`, Zählung `erfolge`).

**Puffervorlauf und Kesselvorlauf.** Das Vorlaufangebot an den Kreis bilden nur die Erzeuger (`Vorlauf_Max`, sonst
`Vorlauf`; `Anlagenfahrplan.cs:259-260`, `Stundenangebot.cs:603-606`), der Speicher liefert nur kWh
(`Stundenangebot.cs:471-500`); `VL_eff` des Puffers deckelt den Kreis nicht. Der Kessel-Vorlauf 70 °C ist also nicht
wirkungslos, der Puffer-Vorlauf 55 °C wirkt auf die Schranke nicht. Er wirkt über `Q_max` (Spreizung × Volumen): Ein
5-K-Puffer mit 1 000 l hält 5,8 kWh — weniger als ein Zehntel der Stundenlast — und nach den Versuchen hängt
Durchlauf oder Abbruch des Kreises an genau dieser Größe.

**Bivalenz (Parallelbetrieb).** Bestätigt: kein Kesselvorbehalt unterhalb des Bivalenzpunkts. Der Parallelzweig
lässt die WP weiterlaufen (`SimulationWaermepumpe.cs:2317-2320`), eine Abschaltmaske gibt es nur für Teilparallel- und
Alternativbetrieb (`Anlagenfahrplan.cs:298-303`); der Bivalenzpunkt wird gesammelt (`Kaskadenschleife.cs:1242-1245`)
und nur berichtet (`SimulationWaermepumpe.cs:2664-2665`).

**Bewertung.** Rechenweg (Kreis konvergiert mit kleinem Puffer nicht) mit Konfigurationsanteil (Puffer zu klein
für die Kopplung). Die 3,9 MWh im Anwenderprojekt stammen nach (c1) wahrscheinlich aus denselben
Spitzenstunden, in denen der nachrangige Kessel keinen Durchlass erhält, zuzüglich der Komfortlücke der
Übergabeleistung.

**Vorschlag.** Konvergenz des Kreises mit kleinem Puffer als Testfall U1 (Projekt 1059, Variante oben) festhalten;
Warnung bei `Q_max` des Kopplungspuffers unter der Stundenleistung der Erzeuger.

## Empfehlung an den Anwender (Projekt 1113)

- Am Gebäude „Übergabe rechnen“ setzen (Heizkreis aktiv, Übergabeart), sonst rechnet AK3 die Heizseite nicht gekoppelt.
- Puffer vergrößern oder mit größerer Spreizung führen (z. B. Vorlauf 70 / Rücklauf 50 oder mehr Volumen); 1 000 l bei
  5 K sind für 66 kW zu wenig.
- Bis zur Behebung von (c1) den Kessel nicht allein als nachrangigen Puffer-Lader führen: Er füllt die Lücke der
  Wärmepumpe in Spitzenstunden nicht. Probeweise den Kessel zusätzlich mit einer Direktsenke Heizkreis eintragen.
- Kessel-Reiter: Maßgeblich für die Unterdeckung ist die neue Zeile „davon übrige Erzeuger / ungedeckt“ bzw. die
  Restwärme der Übersicht, nicht „Restwärmebedarf nach Kessel“.
