# E8b — Formelmappe Stufen 0 bis 3, Anhang-E-Checkliste (U43), Anhang-D-Gegenprobe (Protokoll, 23.09.2026)

Statuszeile #455 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Etappe E8 (Teil b — V‑D) des
Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 5 Zeile E8, § 3.3 A4 und A7, § 3.5 N4); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.11.2 (V‑G10, V‑G12, Anhang D), § 2.11.4 (V‑D) und § 2.11.6 (Stufenplan und Grenze); Mockup
`../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Kategorie 8 (Fuß des Bewertungsblocks, Zone „Bericht und
Ausgabe"), Anhangzeilen U12 und U43; Prüfprotokoll
[`05_Berichte_VALERI_Nutzungsdauer.md`](Analyse_Konzept_Wirtschaftlichkeit_2026-09-19/05_Berichte_VALERI_Nutzungsdauer.md)
§ 3.3 (die ClosedXML-Fragen), § 3.4 (die Wache) und § 7 (Anhang E und D). Entscheide: V‑G10 (R‑V, 18.09.2026, abweichend
von der Empfehlung: der ganze Bericht formelbasiert, soweit ableitbar) und Q18 (R‑Q, Folgeentscheid 22.09.2026: U43 mit
E8) im
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md),
die sechs Fragen dieser Etappe unter R‑E8b. Anlass: der Anwender, „fahre fort" (23.09.2026) — Etappe E8, Teil b; Teil a
(V‑C) lief parallel im Worktree `e8a` und ist #454 ([`E8a_ValERI_Bloecke_Protokoll.md`](E8a_ValERI_Bloecke_Protokoll.md)).
Zweig `e8b` von `591229e1` (`origin`, Schemastand 113, Basis R12), zwei Phasen und ein Nachzug; Phase 1: `4b06bce8`
(E8b/0), `5b6a0125` (E8b/1), `8cac394b` (E8b/2), `238511ed` (E8b/3), `a8b05f9c` (E8b/4), `826b2d4d` (E8b/6), `4408523a`
und `3fa1ec6c` (E8b/5); Phase 2: Merge `d5da3480` (Zweig `e8a` auf `ac51fdb5`, darin die Basis R13), `198dcb07` (E8b/8);
Nachzug: Merge `51ef2b99` (Arbeitszweig `485052c6` = #454, ohne Konflikt). Merge `704356a4` auf dem Hilfszweig `pm5`
über dem Merge #454 (`485052c6`, Basis `origin/ios_migration_september` = `ada7d1ac`; 18 Dateien, +4 472/−81; der Baum
gleicht `51ef2b99`). Opus 5.5 im Worktree `.claude/worktrees/e8b`. Die Punktnummern folgen dem Auftrag (0 = Wache und
ClosedXML-Befund, 1 bis 4 = Stufen 0 bis 3, 5 = U43, 6 = Anhang D); gebaut wurde in der Folge 0, 1, 2, 3, 4, 6, 5.
**Keine Rechenwirkung, kein Schemaschritt** — `SchemaStand.Zielversion` bleibt 113. **Mit #454 und #455 ist E8
abgeschlossen.**

## Befund vor der Welle

- **Der Tabellenbericht war eine Wertfassung** (Analysepapier § 3.3 A4, `05/§ 3.1`): keine einzige Zellformel, 167
  `.Value`-Zuweisungen, fünf Zugriffe auf den Parametersatz, keine seiner Zahlen in einer Zelle — die Annahmen des
  Laufs standen nur als Prosa („i = 3,0 % · T = 20 a · …") in einer Zelle. V‑G10 (18.09.2026): der ganze Bericht
  formelbasiert, soweit ableitbar, in vier Stufen (Konzept § 2.11.6); was dauerhaft Wert bleibt, steht dort als Liste.
- **Vier ClosedXML-Fragen waren offen** (`05/§ 3.3`): ob 0.105.1 zu einer Formel auch ihr Ergebnis ablegt, ob
  `RecalculateAllFormulas` NBW/RMZ/IKV trägt, ob andere Tabellenkalkulationen dieselben Werte zeigen, welche
  Funktionsnamen abzulegen sind.
- **Die Blattstruktur-Wache** (`BerichtBlattstrukturWacheTests`, seit E1 #380 über Excel und Word, auf `591229e1` sechs
  Fälle) hielt Blätter und Ankerzeilen fest, aber nicht die Stellen, die die Stufen umbauen — Kopf und Δ%-Block des
  Vergleichsblatts, Prosazeile des Parameternachweises, Kopf, Jahre 0 und T und Abschluss der Mehrjahrestabellen,
  die Folge der Abschnitte im Kapitel Wirtschaftlichkeit des Wortberichts (`05/§ 3.4`; Analysepapier § 3.5 N4).
- **Anhang-E-Checkliste und Anhang-D-Gegenprobe: nichts gebaut** (Analysepapier § 3.3 A7, `05/§ 7`); Normtext und
  `VALERI_Vorlage_V7.xlsx` liegen unter `Quellen/VALERI/`. Die Fallstudie gehört gegen `KapitalwertRechner.Rechne`,
  nicht gegen die volle Kette; zwei Zeilen der Sensitivitätstafel D.6 tragen Werte des Pumpenbeispiels.
- **Mockup-Anhang:** U12 (Excel-Formelmappe, Stufen 0 bis 3) und U43 (Knopf „Anhang-E-Checkliste…" im Fuß des
  Bewertungsblocks; Q18 nach Empfehlung, Folgeentscheid 22.09.2026: Bau mit E8, sobald V‑C und V‑D den Inhalt liefern)
  offen.

## Gebaut — Phase 1 (E8b/0 bis E8b/6)

- **Wache und Befund (E8b/0, `4b06bce8`).** Die Blattstruktur-Wache pinnt im Excel-Bericht jetzt den ganzen Kopf des
  Vergleichsblatts samt Δ%-Block, die Prosazeile des Parameternachweises, Kopf, Jahr 0, Jahr T, Abschluss- und
  Probezeile beider Mehrjahrestabellen und Wertanker für Barwert, Kumuliert und Abschluss; ein neuer Fall hält den
  Δ%-Block ((Wert − Stamm) / |Stamm| · 100, ohne Abweichungsausweis keine Δ-Zelle). Im Wortbericht pinnt sie die
  Abschnitte des Kapitels Wirtschaftlichkeit in fester Folge — das Verweisziel der Checkliste. Die Anker kommen aus
  dem Messprogramm, das denselben Erzeugungsweg nimmt. Neu `FormelmappeClosedXmlBefundTests` (zwei Fälle): der Befund
  zu ClosedXML als Wache, rot, sobald eine neue Fassung ihn ändert (unten).
- **Stufe 0 — der Parameterblock aus echten Zellen (E8b/1, `5b6a0125`).** Unter der Prosazeile des Parameternachweises
  steht der Block „Parameter der Rechnung (je Szenario)" mit den Spalten Erwartet, Günstig, Ungünstig und „Name in den
  Formeln": Kalkulationszins i, Betrachtungszeitraum T, die Preissteigerungen p_E, p_B und p_I als Dezimalzahl
  (derselbe Ausdruck Prozent ÷ 100, mit dem der Rechenkern sie liest), dazu die Änderungen an Investition, Erträgen
  und Nutzungsdauer (Norm 9 c). Die Spalte Erwartet trägt die Namen `Zins_i`, `Zeitraum_T`, `p_E`, `p_B`, `p_I`, die
  beiden anderen dieselben mit `_Guenstig` bzw. `_Unguenstig` — 15 benannte Bereiche. Darunter ein Hinweis (die
  Formeln rechnen mit der Spalte Erwartet; die Jahreszeilen stehen fest, ein anderer Zeitraum verlangt einen neuen
  Bericht) und die Grenze der Mappe in den drei Sätzen des Konzepts. Neue Klasse `ExcelFormelmappe`; zwölf
  `WIRT_FM_*`. Zellvergleich: alle Zellen gleich, der Block schiebt das Blatt nach unten (Messprogramm: elf Zeilen
  eingeschoben).
- **Stufe 1 — die Mehrjahrestabellen in Formeln (E8b/2, `8cac394b`).** Energie = Jahr 1 × (1 + p_E)^(t−1); die
  CO₂-Abgabe nur im Rückfallzweig ebenso, eine jahresscharfe Reihe bleibt zugelieferter Wert; Betrieb in zwei Termen
  über die Hilfsspalten „Basis Betrieb mit p_B [€/a]" und „Basis Betrieb mit p_E [€/a]", damit Positionen mit
  späterem Startjahr eine Stufe bilden; Netto als Zeilensumme, Barwert = Netto × (1 + i)^−t, Kumuliert als Laufsumme;
  die Abschlusszeile mit nominalem Restwert, seinem Barwert und dem Nettobarwert. Dazu das **Formelregister**
  (`Formelregister`): Jede Formel wird vorher in C# gegengerechnet — weicht sie ab, bleibt der Wert —, ihr Ergebnis
  (die Zahl des Rechenkerns) wird nach dem Speichern als Ergebnis eingetragen, und die Mappe verlangt die
  Neuberechnung beim Öffnen (`fullCalcOnLoad`). Das `Zahlungsbild` bekam drei reine Ausweisfelder
  (`BetriebBasisJeJahr`, `EndenergieBasisJeJahr`, `BehgFortgeschrieben`): nur befüllt, nie gelesen, der Rechenweg
  der Summen Zeichen für Zeichen der von vorher. Bis 272 Formeln je Mappe, Werte gleich.
- **Stufe 2 — die Kennzahlen in Formeln (E8b/3, `238511ed`).** Im Block „Erwartet" der Nettobarwert je Stand über
  `NPV` auf die Nettospalte seiner Tabelle (dazu Jahr 0 und der Barwert des Restwerts), die Kapitalwertdifferenz als
  Zellbezug, die Annuität über `PMT`. Jede Tabelle einer Variante bekommt rechts die **Differenzreihe Variante −
  Referenz** — nominal (im Jahr T samt Restwert-Nominaldifferenz), Barwert, kumuliert und die Hilfsspalte
  „Nulldurchgang in diesem Jahr [a]". Der interne Zinsfuß über `IRR` auf die nominale Reihe nur bei genau einem
  Vorzeichenwechsel (Startwert = der Wert des Laufs, gerundet wie der Rechenkern), die Amortisation über die
  Hilfsspalte; ohne Vorzeichenwechsel bzw. Nulldurchgang steht der Satz der Seite als Text, kein Zellfehler. Ein
  mehrdeutiger Zinsfuß und die Kennzahlen von Günstig und Ungünstig bleiben Werte (E8b‑Q1). Jede Kennzahl ist gegen
  die Methoden des Rechenkerns gegengerechnet (Differenzreihe, Vorzeichenzähler, Zinsfuß, Amortisation).
- **Stufe 3 — Betriebskosten und Δ%-Block (E8b/4, `a8b05f9c`).** Eine bemessene Betriebskostenposition trägt Menge und
  Satz in eigenen Spalten rechts des Betrags (Zahlformat mit Einheit), der Betrag ist ihr Produkt — bei
  Prozentbemessung ÷ 100, ein Erlös negativ; welche Rechnung gilt, sagt `BetriebskostenCtrl.Betrag` selbst, eine
  zweite Liste der Bemessungsarten gibt es nicht. Feste, szenariogepflegte und unvollständige Positionen bleiben
  Werte mit Herleitung. Die Summe ist eine Spaltensumme; der Δ%-Block des Vergleichsblatts ein Zellbezug
  (Wert − Stamm) / |Stamm| · 100. Wachfälle: die bemessene Position 2 % von 3.775 €, die Δ-Zelle als Formel.
- **Anhang-D-Gegenprobe (E8b/6, `826b2d4d`).** `AnhangDFallstudieTests` rechnet die Fallstudie der DIN EN 17463
  (Anhang D, BHKW mit 90 kW thermisch, 18 Jahre) mit ihren eigenen Eingaben gegen `KapitalwertRechner.Rechne`, als
  Differenz zweier Zahlungsbilder (BHKW gegen Kessel und Strombezug). Eingaben aus D.2 bis D.7: 6.697
  Volllaststunden, 90 kW thermisch, 52,2 kW elektrisch, Nutzungsgrade 82 % und 85 % (Gas des BHKW 1.161.358 kWh/a,
  eingesparter Kesselbrennstoff 709.094 kWh/a, Eigenstrom 349.583 kWh/a, ungerundet wie die Norm), Gas 0,06 €/kWh,
  Strom 0,12 €/kWh, Investition 90.000 €, Wartung 3.000 €/a, Zins 6,96 %, Preisänderung Energie 3 %/a, nicht
  energetisch 2 %/a, kein Restwert. Die eine Umrechnung: Die Norm schreibt einen Basiswert zu Preisen des Jahres 0
  mit (1 + p)^t fort, der Rechenkern den Betrag des Jahres 1 mit (1 + p)^(t−1) — übergeben wird Basis × (1 + p).
  Ergebnis und Ausnahmen unten; am Rechenweg nichts geändert, kein STOPP.
- **U43 — die Anhang-E-Checkliste (E8b/5, `4408523a`, Nachtrag `3fa1ec6c`).** Die Checkliste der DIN EN 17463,
  Anhang E, entsteht einmal im Kern (`AnhangECheckliste.Punkte`): 15 Punkte (0.1, 0.2, 1, 2a, 2b, 3a, 3b, 4 bis 11)
  in fünf Gruppen — Gegenstand der Bewertung, A · Aufbau des Modells, B · Berechnung, C · Auswertung,
  D · Berichterstattung —, je Punkt Thema, Anforderung mit eigenen Worten (der Normtext steht nicht im Programm), die
  Stelle im Wort- und im Tabellenbericht und der Stand „erfüllt", „teilweise" oder „offen" mit einer Erläuterung. Den
  Stand leitet die Lage des Laufs ab (`ChecklistenLage`: Kennzahlen gerechnet, nicht monetäre Wirkungen gepflegt,
  Zeitraum abgeglichen, Positionen ohne Nutzungsdauer, Sensitivität, Bandbreite, Vorschlag). **Wortbericht:**
  Abschlussseite nach dem Anhang (`AnhangEChecklisteBaustein`, Schlüssel der Wirtschaftlichkeit), die Spalte
  „Beurteilung 1–5" bleibt für den Prüfer frei. **Mappe:** letztes Blatt „Checkliste Anhang E", die Notenspalte
  nimmt über eine Gültigkeitsprüfung nur ganze Zahlen von 1 bis 5 an, mit Fehlermeldung. **Ergebnisseite:** Knopf
  „Anhang-E-Checkliste…" als eigener Baustein `AnhangEChecklisteKnopf` im Fuß des Bewertungsblocks (eine
  Einbindungszeile in `WirtschaftlichkeitSeite.razor`), eine Überlagerung mit Titel und Kreuz und den Spalten Nr.,
  Thema, Anforderung, Stelle im Bericht, Stand in EPOS. **Nachtrag `3fa1ec6c`** aus dem Messlauf über 13
  Prüfgruppen: Die Bandbreite entsteht auch ohne Rechnung (Zeilen mit „—"), Punkt 9 stand deshalb auf „teilweise",
  obwohl nichts gerechnet war — er verlangt jetzt Zahlen in Ungünstig und Günstig in mindestens einer Zeile; Punkt 4
  ist ohne Lauf offen, weil erst die Mehrjahrestabelle die Zeitpunkte der Zahlungen zeigt. Die Wache zählt jetzt
  sieben Blätter (vorher sechs) und im Wortbericht elf Tabellen (vorher zehn).
- **Ressourcen** (de und en): **103 neu** — `WIRT_FM_*` 20 (Parameterblock zwölf, Hilfs- und Differenzspalten sechs,
  „Menge", „Satz") und `WIRT_AE_*` 83 (Titel, Blatt, Knopf, Hinweis, sechs Spaltenköpfe, Notenfehler, drei Stände,
  fünf Gruppen, „ohne Rechnung", je Punkt Thema, Anforderung, Stelle und die Standtexte); eingefügt nach
  `WIRT_MJ_PROBE`, nicht am Dateiende, damit der Merge mit e8a leichter geht. Designer neu erzeugt, wiederholbar.
  Kein neues SQL im Programmcode.

## Der ClosedXML-Befund und der Entscheid

Gemessen am 23.09.2026 mit ClosedXML 0.105.1 (die Fragen aus `05/§ 3.3`), als Wache festgehalten in
`FormelmappeClosedXmlBefundTests`:

1. **Eine Formel wird ohne zwischengespeichertes Ergebnis abgelegt** — die Zelle trägt `<f>`, aber kein `<v>`, auch
   nach `RecalculateAllFormulas()`: Die Rechnung bleibt im Speicher, die Datei sieht sie nicht. Nur
   `SaveOptions.EvaluateFormulasBeforeSaving` schreibt Ergebnisse — mit 15 Stellen und mit den Lücken der eigenen
   Rechenmaschine.
2. **Die Rechenmaschine kennt `NPV` (NBW) und `IRR` (IKV) nicht** — beide ergeben `#NAME?`, und jede Zelle, die auf
   sie verweist, erbt den Fehler. **`PMT` (RMZ) rechnet sie** (berichtigt mit E8b/8, `198dcb07`): Der
   Phase‑1-Bericht nannte auch `PMT`; der erste Testlauf in Phase 2 zeigte, dass die Fehlerwerte der Annuität in der
   Mappe allein daher kommen, dass ihr `PMT` auf die NBW-Zelle zeigt. Grundrechenarten, `SUM`, Potenzen,
   `IF`/`COUNTIF`, `MIN`, `ROUND` rechnet sie.
3. **Die Funktionsnamen:** Abgelegt werden die englischen Namen `NPV`, `PMT`, `IRR`; Excel 16 erkennt und rechnet sie
   (unten), die Anzeige folgt der Sprache von Excel.

**Entscheid** (die Regel des Konzepts § 2.11.6): **EPOS trägt die Werte ein, Excel rechnet beim Öffnen neu.** Die
Formeln schreibt ClosedXML; EPOS rechnet jede Formel vorher in C# nach — stimmt sie nicht, bleibt der Wert stehen
(`Formelregister.Abweichungen`); nach dem Speichern trägt EPOS zu jeder Formelzelle die Zahl des Rechenkerns als
Ergebnis ein (OpenXML SDK, volle Stellenzahl), und die Mappe verlangt die volle Neuberechnung beim Öffnen
(`fullCalcOnLoad`). Ein Betrachter ohne Rechenmaschine zeigt damit die eingetragenen Zahlen statt leerer Zellen.
**Gemessen:** Excel (Microsoft 365, Version 16) rechnet jede Formelzelle auf den gespeicherten Wert; die
OpenXML-Prüfung meldet 0 Fehler. **Nicht prüfbar:** LibreOffice und openpyxl sind auf dem Rechner nicht installiert;
ersatzweise ist das XML so gelesen, wie openpyxl es mit `data_only` lesen würde — Formel und Wert stehen überall.
Ändert eine neue ClosedXML-Fassung eines der beiden Verhalten, wird die Wache rot; dann ist der Entscheid neu zu
prüfen (etwa, ob der Nachtrag der Ergebnisse entfallen kann).

## Phase 2 und der Nachzug

- **Merge `d5da3480`** holt den Zweig `e8a` auf `ac51fdb5` (E8a/1 bis E8a/9 samt Zahlungsstrombild, die Stände des
  Arbeitszweigs bis `2edc081e`: Kühlung KU1 Welle 4, Referenzbasis R13, Testdatenbank auf Katalog-Generation 9,
  LFS `769143e4…`). Konflikt nur in `EPOS.Kern/MyResource/Resource.Designer.cs` — aus den zusammengeführten `.resx` neu
  erzeugt, ein zweiter Lauf ändert nichts; beide `.resx` ohne Konflikt, **je 8.600 Schlüssel**, keine Doppel.
  `WirtschaftlichkeitSeite.razor` und `WirtschaftlichkeitErgebnisansichtTests` gingen ohne Konflikt durch,
  Einbindungs- und Selektorzeile blieben erhalten. **Nachzüge im Merge:** **E8a‑Q4** — die Fußzeile U48 steht links in
  derselben Reihe wie „Anhang-E-Checkliste…" und „Bericht erzeugen": die Seite hat dafür das Fragment `Fussreihe`
  statt `BerichtKnopfteil` (in „Was ist angenommen?" mit der Fußzeile, in Block 5 ohne), die Stilregel
  `.epos-wirt-abschnitt-fuss` bekam Umbruch und Abstand, der U48-Test prüft die Reihe. **Blattstruktur-Wache** — der
  neue Abschnitt „Von der Investition zur Kapitalwertdifferenz" (`WIRT_BR_TITEL`) steht in der Kapitelfolge; das
  Zahlungsstrombild ist ein Bild, keine Tabelle, die Tabellenzahl bleibt 11.
- **E8b/8 `198dcb07`** — der ClosedXML-Befund berichtigt (oben): Befundfall und Kommentar der Formelmappe
  nachgezogen, der Entscheid bleibt.
- **Merge `51ef2b99`** holt den Arbeitszweig `485052c6` (#454: Merge e8a, darin `7a32b6f3` Protokoll #450, `cd2ac9ec`
  #457, `ada7d1ac` #456) — ohne Konflikt; beide `.resx` **je 8.620 Schlüssel**, gültiges XML, keine Doppel,
  Designer-Prüflauf unverändert und wiederholbar.
- **Merge `704356a4`** auf dem Hilfszweig `pm5` über `485052c6` (18 Dateien, +4 472/−81; der Baum gleicht `51ef2b99`).

## Fragen aus der Etappe

Der Phase‑1-Bericht nennt sechs Fragen. **Alle sechs sind beim Anwender offen**; gebaut ist jeweils Lesart a, Q5 und
Q6 haben sich mit dem Nachzug und der Phase 2 erledigt. Sie stehen im Entscheidungsregister als **R‑E8b**.

| Frage | Lesarten | Empfehlung | Stand |
|---|---|---|---|
| **E8b‑Q1** Kennzahlen Günstig und Ungünstig in der Formelmappe | (a) sie bleiben Werte — für diese Szenarien gibt es keine Mehrjahrestabelle; (b) je Stand zwei weitere Tabellen, damit auch sie in Formeln rechnen | a | offen; gebaut ist a (ebenso bleibt ein mehrdeutiger Zinsfuß ein Wert) |
| **E8b‑Q2** „fester Betrag" neben einer Menge-×-Satz-Formel | Schon vorher falsch: `WirtschaftlichkeitZeilen.BemessungText` kennt nur 4 von 17 Bemessungsarten und nennt die übrigen „fester Betrag" — (a) so lassen; (b) den Text im Kern für alle Bemessungsarten richtigstellen | eigener kleiner Auftrag im Kern | offen; nicht gebaut |
| **E8b‑Q3** Warnung „Gliederung unvollständig" bei Positionen mit späterem Startjahr | Auch das war vorher so (Prüfgruppe hybtest: 2.400 gegen 1.800 €, die Differenz ist genau die Wartung ab Jahr 6) — (a) die Warnung stehen lassen; (b) nur Positionen vergleichen, die im Jahr 1 laufen, oder das Startjahr in der Warnung nennen | b, als eigener Auftrag | offen; gebaut ist a (unverändert) |
| **E8b‑Q4** Der Knopf ruft eine reine Kernfunktion direkt auf | Die UI-Regel sagt „keine Fachklassen des Kerns" — (a) so lassen: ohne Datenbank, eine Quelle für Seite und Berichte; (b) die Hülle füllt die Punkte in den Seitenstand (berührt e8a-Dateien, erst nach dem Merge) | a | offen; gebaut ist a |
| **E8b‑Q5** Die Merge-Stellen mit e8a | keine Lesarten — die Arbeitsliste für den Nachzug: `.resx` und Designer, die Einbindungszeile der Seite, eine Selektorzeile der Ergebnisansicht-Tests, der Abschnitt `WIRT_BR_TITEL` in der Abschnittsliste der Wache samt Tabellenzahl | beim Nachzug erledigen | **erledigt beim Nachzug** (`d5da3480`): nur der Designer im Konflikt, neu erzeugt; die Wache kennt den Abschnitt, die Tabellenzahl bleibt 11 |
| **E8b‑Q6** Referenzbasis | Der Arbeitszweig stand auf R13, e8b auf R12 — (a) Phase 2 gegen R12 (E8b ändert nicht am Rechenweg), R13 im Gate nach dem Merge; (b) vorher nachziehen und gegen R13 | a | **erledigt:** Phase 2 lief nach dem Nachzug von e8a gegen R13, ebenso das Gate — 13/13 PASS |

## Nachweis „keine Rechenwirkung", Zellvergleich und Anhang D

- **Rechenwerte:** Die Formelmappe rechnet nichts Neues — sie schreibt eine Formel nur, wo sie die Zahl der
  Wertfassung wiedergibt (Gegenrechnung im Formelregister), und trägt diese Zahl als Ergebnis ein. Die drei neuen
  Felder des `Zahlungsbild` werden nur befüllt. Die Ankertests laufen im vollen Lauf unverändert grün; keine
  Simulationsgröße ist berührt; Referenzlauf 13/13 PASS gegen `2026-09-23_R13_Kuehlung`, 4 145 687 Werte in der
  Toleranz, 387/387 CSV byte-gleich (Phase 2 und Gate).
- **Zellvergleich je Stufe** (Messprogramm, Wertfassung gegen Formelfassung; Stufen 0 bis 3 über zehn Prüfgruppen,
  u. a. 1019/1023/1024, 1030, 1046, die Mischgruppen 1040 und 1042, 1030 mit gepflegten Sätzen): nach jeder Stufe jede
  Zelle gleich. **Endstand über 13 Prüfgruppen:** Formeln je Mappe z. B. synth 256, 1019/1023/1024 320, hyb1042 429,
  hybbk 449, prep1030 134 — 1046 hat keine Wirtschaftlichkeit, also 0; jede Zelle der Wertfassung gleich; Excel 16
  rechnet jede Formelzelle auf den gespeicherten Wert; ClosedXML rechnet gleich, außer bei `NPV`/`IRR` und den Zellen,
  die darauf verweisen; OpenXML-Prüfung 0 Fehler; im Wortbericht kommt allein die Checkliste hinzu.
- **Zellvergleich nach dem Merge mit e8a** (Stand vorher gegen nachher): gleich in neun Prüfgruppen (synth, synthk,
  1030, prep1030, hyb1040, hyb1042, hybbk, hybleer, hybtest); nur Werte verschieden in vier Gruppen mit simulierten
  Daten, in allen Blättern einschließlich Stamm und Varianten (1019/1023/1024 771 Zellen, 1026/1027/1029 155, 1018/1031
  97, 1046 52) — Ursache ist der neue Rechenstand (R13 Kühlung, Katalog-Generation 9); keine Zeile verloren, die Zahl
  der Formeln gleich; Excel 16 rechnet in allen 13 Gruppen jede Formelzelle auf den gespeicherten Wert; im Wortbericht
  kommt nur der Abschnitt des Brückenbilds hinzu.
- **Anhang D** (gegen `KapitalwertRechner.Rechne`, Toleranz ±1 € — die Norm rundet auf ganze Euro):

| Fall | Rechenkern | Norm |
|---|---|---|
| wahrscheinlichster Fall | 64.479,51 € | 64.480 € |
| Worst Case | −202.801,57 € | −202.802 € |
| Best Case | 598.319,65 € | 598.320 € |

  Die Jahreswerte der Tafel D.5 stimmen auf ganze Euro (Jahre 1 und 18: Wartung 3.060 / 4.285 €, Gas des BHKW
  71.772 / 118.628 €, Netto 12.199 / 20.935 €, Barwert 11.405 / 6.236 €; die Einsparung der Referenz im Jahr 1
  43.822 + 43.209 €). Sechs Zeilen der Sensitivitätstafel D.6 treffen auf ±0,5 €, samt Steigung in € je Prozent der
  Änderung: Energiepreisschwankung 472, Preisschwankung nicht energetisch −57, Laufzeit T 1.121, Kalkulationszins
  −889, CAPEX −900, OPEX −354. **Ausgenommen und dokumentiert:** die zwei D.6-Zeilen „Reduzierter Energieverbrauch
  des Heizkessels" und „Stromerzeugung zur Eigennutzung" (Werte des Pumpenbeispiels, Grundeinstellung 239.603 € statt
  64.480 €); die Zeile „Gasverbrauch BHKW" (−30.484 € bei −50 %, +159.443 € bei +50 %, Steigung 1.899 €/Δ%) trifft die
  Änderung des ganzen Energie-Nettostroms, nicht die des Gasverbrauchs (dann +511.160 € bzw. −382.201 €); Tafel D.7
  nennt im wahrscheinlichsten Fall 348.583 kWh/a Strom statt 349.583 kWh/a — ein Tippfehler, nur die zweite Menge ergibt
  die 64.480 € derselben Spalte.

## Abweichungen vom Mockup und vom Auftrag

- **Der Zwischenbericht nach Stufe 1** steht erst im Phase‑1-Bericht (ein Unteragent kann nicht anhalten); nach
  `8cac394b` waren Stufe 0 und 1 fertig, der Zellvergleich gleich, der ClosedXML-Befund und der Entscheid lagen vor —
  kein Grund für einen Stopp. Punkt 5 hat zwei Commits.
- **Das `Zahlungsbild` hat drei reine Ausweisfelder bekommen** (E8b/2), genannt wegen der Regel „keine
  Rechneranpassung ohne Rückmeldung"; der Rechenweg ist unverändert.
- **Die Wache legt drei Zeilen in `Tab_ProjektWerte` der Arbeitskopie an** (nach bestehendem Testmuster); neues SQL
  im Programmcode gibt es nicht.
- **Kennzahlen Günstig und Ungünstig und ein mehrdeutiger Zinsfuß bleiben Werte** (E8b‑Q1).
- **Der Knopf ruft die Kernfunktion direkt** (E8b‑Q4) und steht in beiden Darstellungen — in „Was ist angenommen?"
  und in Block 5 der Darstellung „ValERI-Bewertung", weil beide denselben Fuß tragen; die Überlagerung führt keine
  Notenspalte, die Beurteilung 1–5 vergibt der Prüfer im Bericht.
- **Einen eigenen Knopf „XLSX mit Formeln exportieren…"** (Rechenweg 08) gibt es nicht: Die Formelmappe ist der
  Tabellenbericht selbst, erzeugt über „Bericht erzeugen" oder die Seite „Bericht".
- **Phase 2 lief gegen R13** statt, wie beauftragt, gegen R12 (E8b‑Q6) — nach dem Nachzug von e8a.
- **Die Testdatenbank:** Den in der Startnachricht genannten Stand `171d8acb` gibt es in der Geschichte von e8b nicht;
  gerechnet ist mit LFS `769143e4…` aus `6ebb26b6` (Katalog-Generation 9) — genau die Prüfsumme, die
  `Referenzlaeufe/LIESMICH.md` für R13 nennt; die Arbeitskopie stimmt mit ihr überein, die WAL-Datei ist leer.

## Zahlen und Abnahme

- **Phase 1** (Worktree `e8b`): Kern-Filter und Windows-Schale 0 Fehler, die UI-Tests bauen; Designer unverändert und
  wiederholbar; SQL-Prüfer 0 Fundstellen — kein `dotnet test`, kein Referenzlauf.
- **Phase 2** (nach dem Merge `d5da3480`, Stand `198dcb07`; erst gestartet, als kein fremder Testprozess mehr lief):
  voller Lauf `WP-Plan.Kern.slnf` 0 Fehler, **11 899 bestanden, 1 übersprungen** (EPOS.Kern 5 357, EPOS.UI 5 605,
  KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen); die vier neuen bzw. geänderten Kern-Klassen
  32/32, die betroffenen bunit-Klassen 123/123; Blattstruktur-Wache vorher (auf `4b06bce8`) 8/8, nachher (auf
  `198dcb07`) 13/13; Referenzlauf 13/13 PASS gegen R13, 4 145 687 Werte in der Toleranz; Kern-Filter und
  Windows-Schale 0 Fehler; Designer-Prüflauf unverändert; SQL-Prüfer 1 737 Texte, 0 Fundstellen.
- **Nach dem Nachzug** (`51ef2b99`): gefiltert Kern 154/154, UI 1 133/1 133.
- **Gate auf dem Gesamtstand** (Baum `51ef2b99` = Merge `704356a4`, e8a und e8b über `ada7d1ac`, 22:32–22:38), grün:
  Kern-Filter (Release) 0 Fehler; ChartProben 161 Bilder, 0 Verstöße (E8b bringt kein Bild); Tests Kern-Filter
  0 Fehler, **11 942 bestanden, 1 übersprungen** (EPOS.Kern 5 366, EPOS.UI 5 639, KiKern 524, SpeicherEngine 386,
  SpeicherPlanung 27 und 1 übersprungen); Dokumentationswachen 26/26; Referenzlauf 13/13 PASS gegen
  `2026-09-23_R13_Kuehlung`, 4 145 687 Werte in der Toleranz, 387/387 CSV byte-gleich; SQL-Prüfer 1 737 Texte,
  0 Fundstellen; Windows-Schale 0 Fehler.
- **Tests** (neu oder erweitert): `BerichtBlattstrukturWacheTests` (6 → 13 Fälle; neu Stufe 0 mit den Namen, Stufe 1,
  Stufe 2 über NBW/RMZ und die Differenzreihe, der benannte Leerwert, Stufe 3, der Δ%-Block gegen den Stamm und die
  Abschnittsfolge der Wirtschaftlichkeit im Wortbericht samt `WIRT_BR_TITEL`; sechs → sieben Blätter, zehn → elf
  Tabellen, die Ankerzeilen erweitert), `FormelmappeClosedXmlBefundTests` (neu, zwei Fälle),
  `AnhangDFallstudieTests` (neu, zehn Fälle: drei Sollwerte, Tafel D.5, sechs Zeilen D.6), `AnhangEChecklisteTests`
  (neu, sieben Fälle: Punkte, Stand je Lage, Blatt und Notenprüfung), bunit `AnhangEChecklisteKnopfTests` (neu, fünf
  Fälle); `WirtschaftlichkeitSeiteTests` (der Selektor des Berichtknopfs) und `WirtschaftlichkeitErgebnisansichtTests`
  (die Fußzeile in der Reihe) nachgezogen.
- **Ressourcen** (de und en): **103 neu** (`WIRT_FM_*` 20, `WIRT_AE_*` 83), keiner geändert, keiner gestrichen; je
  Sprache 8 620 Einträge über `485052c6`.
- **Kein Schemaschritt;** `SchemaStand.Zielversion` = 113. Die Testdatenbank ist unverändert (LFS `769143e4…`, die
  Fassung zur Basis R13, Katalog-Generation 9).

## Abnahme am Gerät (A‑E8b‑1, Windows und iPad)

(1) Excel-Bericht der Vergleichsgruppe öffnen (Excel rechnet beim Öffnen neu): Blatt „Wirtschaftlichkeit" mit dem
Parameterblock „Parameter der Rechnung (je Szenario)" unter dem Parameternachweis, die Namen `Zins_i`, `Zeitraum_T`,
`p_E`, `p_B`, `p_I` im Namens-Manager; einen Satz der Spalte Erwartet ändern — Mehrjahrestabelle und Kennzahlen des
Szenarios Erwartet ziehen mit. (2) Die Mehrjahrestabellen in Formeln — Energie, Betrieb über die beiden Hilfsspalten,
Netto, Barwert, Kumuliert, Abschluss; rechts die Differenzreihe je Variante. (3) Im Block „Erwartet" Nettobarwert,
Annuität, interner Zinsfuß und Amortisation als Formeln (NBW, RMZ, IKV); ohne Vorzeichenwechsel der Satz als Text.
(4) „Betriebskosten nach Kostenarten": bemessene Positionen mit Menge und Satz, Betrag als Produkt, Summe als
Spaltensumme; der Δ%-Block des Vergleichsblatts als Formel. (5) Letztes Blatt „Checkliste Anhang E": 15 Punkte in fünf
Gruppen, Stand je Punkt, die Notenspalte nimmt nur 1 bis 5. (6) Wortbericht: die Abschlussseite „Checkliste für den
Bewertungsbericht (DIN EN 17463, Anhang E)" nach dem Anhang. (7) Ergebnisseite: Knopf „Anhang-E-Checkliste…" im Fuß von
„Was ist angenommen?" und in Block 5, links daneben die Fußzeile (U48), rechts „Bericht erzeugen"; die Überlagerung vor
und nach „Berechnen" (ohne Lauf stehen die Punkte, die Zahlen brauchen, auf „offen"). (8) Die Werte der Mappe mit
der Wertfassung vergleichen — dieselben Zahlen wie auf der Seite. (9) Englisch.

## Befunde nebenbei

- **ClosedXML 0.105.1** legt keine Ergebniswerte ab und kennt `NPV` und `IRR` nicht; `PMT` rechnet es (berichtigt
  mit E8b/8). Die Regel „EPOS trägt die Werte ein, Excel rechnet neu" steht im Konzept § 2.11.6; die Wache meldet
  jede Änderung der Bibliothek.
- **Die Norm hat drei Fehler in der Fallstudie:** zwei D.6-Zeilen mit Werten des Pumpenbeispiels (bekannt), die
  D.6-Zeile „Gasverbrauch BHKW" mit der Wirkung des ganzen Energie-Nettostroms (neu) und der Tippfehler 348.583 statt
  349.583 kWh/a in D.7 (neu). Alle drei sind in der Vorrichtung ausgenommen bzw. vermerkt.
- **`WirtschaftlichkeitZeilen.BemessungText` kennt nur 4 von 17 Bemessungsarten** und nennt die übrigen „fester
  Betrag" — in der Mappe steht dieser Text jetzt neben einer Menge-×-Satz-Formel (E8b‑Q2, eigener kleiner Auftrag).
- **Die Warnung „Gliederung unvollständig"** schlägt bei Positionen mit späterem Startjahr an, obwohl die Rechnung
  stimmt (hybtest: 2.400 gegen 1.800 €, die Differenz ist die Wartung ab Jahr 6; E8b‑Q3).
- **Die Knopf-Regel:** Der Baustein `AnhangEChecklisteKnopf` ruft `AnhangECheckliste.Punkte` direkt — gegen den
  Wortlaut „keine Fachklassen des Kerns" in der Oberfläche, aber ohne Datenbank und aus einer Quelle (E8b‑Q4).
- **Die Merge-Nachricht `704356a4`** sagt „E8b-Q1..Q6 nach Empfehlung"; die Fragen sind beim Anwender offen —
  maßgeblich ist das Register (R‑E8b).
- **Testregel:** Beim Vorher-Lauf der Blattstruktur-Wache lief ein fremder Testprozess; er war gesehen, aber nicht
  abgewartet. Der Lauf war grün; alle späteren Läufe starteten erst ohne fremden Testprozess.
- **Ein Codekommentar bleibt überholt:** Der Kopfkommentar der Darstellung „ValERI-Bewertung" in
  `EPOS.UI/Seiten/Berichte/WirtschaftlichkeitSeite.razor` sagt weiter „Das Zahlungsstrombild (U42) ist nicht gebaut"
  (Befund aus #454); E8b hat die Datei an anderer Stelle berührt, den Satz nicht.
- **Rechenweg 08** führte die Knöpfe „Anhang-E-Checkliste…" und „XLSX mit Formeln exportieren…" als Mockup-Knöpfe
  ohne Element und die Gap-Kurztafel auf dem Stand vor #434; mit diesen Papieren sind der Knopfsatz, V‑G10, V‑G12 und
  die Anhang-D-Gegenprobe nachgezogen, die übrigen Zeilen der Kurztafel (V‑G6, V‑G8, V‑G9, V‑G11) nicht.

## Offen

- **Die sechs Fragen** E8b‑Q1 bis E8b‑Q6 beim Anwender (Q5 und Q6 erledigt, ein Entscheid ist für sie nicht nötig);
  nach dem Entscheid die zwei eigenen kleinen Aufträge zu Q2 (`BemessungText`) und Q3 (Warnung bei
  Startjahr-Positionen).
- **Abnahme am Gerät** A‑E8b‑1 (neun Punkte oben); dabei, wo vorhanden, die Mappe in LibreOffice öffnen.
- **Nächste Etappe: E9** (V‑E, Szenarioabdeckung — Analysepapier § 5): die Schemaschritte B (Szenariorahmen:
  Betrachtungszeitraum und Mengenfaktor je Szenario), C (Trägerpreise best/worst) und D (Erlössätze best/worst), der
  ±-Knopf an drei neuen Orten, der Kern liest die Paare, der Hinweistext entfällt; ohne Degradation (A5); rechenwirksam
  je Pflege, NULL = wie Erwartet. Die Nummern der Schritte bei der Umsetzung: 114 belegt die Zapfprofil-Stufe Z3 auf
  ihrem Zweig, 115 ist der Nachbarsitzung „Dialog Design" zugesagt — voraussichtlich ab 116, vorher an
  `SchemaStand.Zielversion` messen.
- **Push** nach der Regel des Anwenders ohne Rückfrage aus dem Hilfszweig `pm5` in einem Zug mit #454 (`485052c6`,
  `704356a4` und die Papiere zu #454 und #455) auf `ios_migration_september` und `main`; der CI-Nachweis kommt mit der
  nächsten Papierwelle.
- **Papiere mit der Statuszeile:** Register (Q18, V‑2, V‑G10, V‑G12, A7, EZ‑9, EZ‑10, neue Familie R‑E8b), Konzept
  (Kopf, § 2.10, § 2.11.2 mit V‑G10, V‑G12 und Anhang D, § 2.11.4 V‑C und V‑D, § 2.11.6, § 6.1, § 6.2, § 7 und
  Anhang), Protokoll der Entscheidwege (§ 8.15, § 8.16, Kopf), Analysepapier (Kopf, Nachtrag, § 2.4, § 3.3 A4 und A7,
  § 3.5 N4, § 5 mit „E8 abgeschlossen", § 6), Szenarienkonzept (Kopf, § 11, § 11.1), Rechenweg 08, Prüfprotokoll
  `05/§ 3.3` (Nachtrag mit Verweis auf dieses Protokoll), Mockup (Anhang U12 und U43 erledigt, Zone „Bericht und
  Ausgabe", Ressourcentafel der Kategorie 8, Stand-Absatz), Logbuch-Sätze und die Wiki-Quelle der Seite
  Wirtschaftlichkeit, Index Reporting.

**Entscheide 23.09.2026:** alle nach Empfehlung, siehe Register R‑E8b
