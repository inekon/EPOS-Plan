# E5 — Ergebnisansicht und V‑A: Umschalter, vier Abschnitte, Bandbreite, Empfehlungskarten, Berichte (Protokoll, 22.09.2026)

Statuszeile #434 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Etappe E5 des Analysepapiers
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
(§ 3.3 A1, A2, A5, § 4 A14, § 5 Zeile E5); Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.11.3/§ 2.11.4 (V‑A), § 2.11.7 (Hinweistext), § 2.13 und § 6.3 Nr. 31; Mockup
`../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Kategorie 8 „Ergebnisansicht" und Anhangzeilen U2, U4, U5, U10,
U39 und U44 — vom Anwender am 22.09.2026 als verbindliche Zielvorgabe abgenommen. Entscheide: K8/V‑1 (18.09.2026, Umschalter
im Kopf der Seite), V‑3 (Kacheln behalten, mit Label), V‑4 mit A14 (Hinweistext in der Konzeptfassung ohne Roadmap-Satz),
Q8, Q16 und Q18 der Mockup-Prüfung, Nr. 31 (22.09.2026: kein Nachziehlauf, Kennzeichnung). Anwender 22.09.2026: „setze das
Konzept für die App epos-plan um". Zweig `e5` von `5334d774`; Teil a: `e8ebef76` (E5/1), `3a45d994` (E5/2), `d22ad34e`
(E5/3), `60ba2761` (E5/4); Teil b: `07cd8ede` (E5/5), `5078c4f1` (E5/6), `d21eb434` (E5/7), `a493448f` (E5/8), `7724ffda`
(E5/8a); dazu die Zusammenführungen des Arbeitszweigs `d1b5e158` (vor Phase 2 von Teil a; `Resource.Designer.cs` mit
`designer_neu.py` neu erzeugt, nicht von Hand gemischt) und `279947c5` (ohne Konflikt); Merge `deba5e57` in
`ios_migration_september`. Beide Teile Opus 5.5 im Worktree `.claude/worktrees/e5`.

## Befund vor der Welle

- **A1 (Analysepapier § 3.3):** Die Seite kannte weder die Bandbreite (U4) noch Empfehlungskarten je Version (U5, nur die
  `Empfehlungszeile`), Gliederung, Brücke, die fünf ValERI-Blöcke oder den Szenario-Hinweistext (U10,
  `WIRT_SZEN_HINWEIS` repoweit 0 Treffer); statt des Umschalters (K8/V‑1) stand ein Aufklappblock mit einem Textfeld.
- **A2 (V‑A):** keine „nachrichtlich"-Kennzeichnung, keine Deklarationszeilen (nominal, Steuern, Restwert, Risiko), keine
  Mehrdeutigkeitswarnung des Zinsfußes — `InternerZinsfuss` brach bei fehlendem Vorzeichenwechsel ab und zählte nicht —,
  keine Steigungsspalte. `Referenzwahl.Deklarationszeile` benennt die Referenz und ist nicht die Normdeklaration.
- **A5:** Die Bandbreite stand seit E2 (#405) mit Spanne und Referenzzeile in Word und Excel, auf der Seite gar nicht.
- **Konzept:** § 2.11.4 führte V‑A als offen mit Etappe E5; nach § 2.13 (3) gehörte das Einsammeln der Positionen für die
  Hinweiszeile „k von n ohne Nutzungsdauer" in einen Kern-Controller (U39) zu E5; § 6.3 Nr. 31 verlangte die Kennzeichnung
  der Ergebniszeilen ohne Nachweis in Ansicht und Bericht.

## Teil a — Kern und Hüllen (E5/1 bis E5/4)

- **V‑A im Kern.** Die Differenzreihe Variante − Referenz (nominal, Restwertdifferenz im letzten Jahr) steht einmal in
  `KapitalwertRechner`; `InternerZinsfuss` und der neue Zähler `Vorzeichenwechsel` lesen sie, Nullwerte unter 1e‑6 €
  zählen nicht. Mehr als ein Wechsel setzt an der Zinsfußzeile die Warnung `WIRT_IZF_MEHRDEUTIG`, keiner führt zu „kein
  Zinsfuß bestimmbar" (`WIRT_IZF_KEIN_WERT`) statt zum Abbruch. Der Nachweisumschlag trägt die Zahl der Wechsel ab Fassung
  7; ältere Fassungen lesen „nicht gezählt" und warnen nicht. „nachrichtlich" setzt `WirtschaftlichkeitZeilen.IstNachrichtlich`
  (nach E5‑Q3 an Amortisation und Zinsfuß), die Deklarationen bildet `ValeriAusweis.Deklarationen()` (nominal, Steuern,
  Restwert, Risiko). Die Sensitivität trägt die Steigung (`SensitivitaetZeile.Steigung` = (KW⁺ − KW⁻)/(2 · Schritt),
  `SteigungEinheit` €/%-Pkt. oder €/%); die Stufe liest sich aus dem Text der gespeicherten Zeile, ein Schemaschritt war
  nicht nötig.
- **Q16 und Reihenfolge.** Eine Zelle ohne Wert zeigt „— ‹Grund›" auf der Seite und im Wortbericht, Excel bleibt leer
  (`WirtZeile.ExcelWert` null); die Kennzahltafel folgt der Reihenfolge des Mockups (Differenz, Annuität, Amortisation,
  Zinsfuß, Gestehungskosten, Nettobarwert absolut). Die Blattstruktur-Wache ist nachgezogen (Nettobarwert Zeile 24 statt 22).
- **Bandbreite und Einstufung.** `WirtschaftlichkeitBandbreite` bildet je Stand außer der Referenz die Kapitalwertdifferenz
  in den drei Szenarien, Spanne, Amortisation und Einstufung (`Urteile` aus `WirtschaftlichkeitEmpfehlung.Einstufungen`) —
  dieselbe Definition wie die Berichtstafel seit E2 (G8). `WirtschaftlichkeitCtrl.BerechneBandbreite` rechnet drei
  vollständige Szenarioläufe ohne Speichern (Muster der Sicht 2). `WirtschaftlichkeitBewertung` bündelt Bandbreite,
  Vorschlagstext mit der Referenz beim Namen, Szenariohinweis, Deklarationen, Nutzungsdauer-Hinweise und die Stände ohne
  Nachweis; `BerichtsDaten.Bewertung` trägt sie in beide Berichte, `FuerBericht` ist der Rückfall ohne Sammler.
- **Nutzungsdauer-Hinweis (Teil von U39).** `NutzungsdauerHinweisCtrl` sammelt die Positionen der Gruppe im Szenario
  Erwartet im Kern statt in der Hülle der Seite: die Zeitraumzeile und je Stand und Technik „k von n betragstragenden
  Positionen ohne Nutzungsdauer", nur wenn der Betrachtungszeitraum über der Vorgabe der Technik liegt und k ≥ 1 ist; der
  Satz kommt aus `ErsatzRestwertTafel.Hinweis`, gleiche Sätze mehrerer Stände stehen einmal. Messung an der Testdatenbank:
  95 von 101 Positionen der Kategorie 1 ohne Dauer, von den 33 betragstragenden 27, daraus sieben Hinweise.
- **Nr. 31.** `WirtschaftlichkeitErgebnis.OhneNachweis` kennzeichnet Zeilen ohne `Nachweis_Json`; sie tragen „Nachweis
  liegt mit der nächsten Rechnung vor" (`WIRT_NACHWEIS_NAECHSTE_RECHNUNG`), ein frisch gebuchter Lauf trägt es nicht, auch
  nicht nach dem Neuladen.
- **Hülle.** `WirtschaftlichkeitSeiteGaben` (plattformfrei in `EPOS.UI.Daten`) liefert Karten, Bandbreite, die
  Empfehlungszeile aus denselben Urteilen, Kacheln mit Label und Grund, Szenariohinweis, Deklarationen, Zeitraumzeile und
  Hinweise aus den Kernmodellen, ohne eigene Regel; ihr eigenes Einsammeln der Nutzungsdauer-Positionen ist entfallen.

## Teil b — Seite, Berichte, bunit (E5/5 bis E5/8a)

- **Entscheide im Kern (E5/5).** E5‑Q2 bis E5‑Q7 umgesetzt (Tafel unten). Neu: `WirtschaftlichkeitCtrl.Berechne(…, out
  sensitivitaet)`, `WirtschaftlichkeitBewertung.Sensitivitaet` und `.Nachweiszeile`, `ValeriAusweis.Annahmen`
  (Annahmentafel), `WirtschaftlichkeitZeilen.KENNZAHLTAFEL`/`IstKennzahl`/`GLIEDERUNGSSUMME`, `Vergleichsauswahl.Darstellung`.
  Für E5‑Q6 nimmt `BerichtsDatenSammler.SammleFuerBericht` die Vergleichssicht und setzt sie vor den Rechenschritt: gebucht
  wird gegen die Gruppenreferenz, in Sicht 2 rechnet derselbe Weg gegen A, ohne zu speichern.
- **Berichte (E5/6).** Wortbericht (`BausteineWirtschaftlichkeit`) und Tabellenbericht (`ExcelBerichtGenerator`) bilden
  Bandbreite, Zeitraumzeile und Sensitivität nicht mehr selbst, sondern lesen `BerichtsDaten.Bewertung`. Word: Zeitraumzeile
  und „k von n", die Deklarationen in einer Zeile, die Nr.-31-Zeile, die Bandbreite mit Spanne und Einstufung, darunter
  Hinweistext und Vorschlagssatz, das Label „nachrichtlich (Anhang C)" am Titel von Amortisation und Zinsfuß, die Warnung
  des mehrdeutigen Zinsfußes und die Sensitivität mit der Spalte „Steigung". Excel: Zeitraumzeile (Zeile 4), je
  Nutzungsdauer-Hinweis eine Zeile, die Bandbreitentafel mit dem Hinweistext, die Steigung numerisch in Spalte 5 und ihre
  Einheit als Text in Spalte 6, der Block „Bewertung nach DIN EN 17463" am Blattende. Die Blattstruktur-Wache zieht nur
  verschobene Zeilen nach (+1, keine Wertänderung).
- **Seite (E5/7).** Kopf mit dem Umschalter „Kennzahlen / ValERI-Bewertung" (zwei Knöpfe mit `aria-pressed` in einer
  Gruppe), Zustand als Sitzungswahl; die Fußleiste ist in beiden Zuständen dieselbe. Die Darstellung „Kennzahlen" gliedert
  in vier Abschnitte: „Lohnt es sich?" (Kacheln mit Label und Warnung, Empfehlungskarten je Version mit Stufe und
  Differenz, Vorschlagssatz und Einstufungsregel, Kennzahltafel im Szenario Erwartet mit Label, Zellwarnung und
  „— ‹Grund›"), „Wie sicher ist das?" (Bandbreite nebeneinander mit Referenzzeile, Spanne und Fußtext, Sensitivitätstafel
  mit Steigung, darunter die Klappliste „Einzelheiten anzeigen für Szenario" mit „Parameter…", die nur die Tafel und die
  Szenariozeile darunter tauscht), „Woraus entsteht die Zahl?" (Gliederung ohne doppelte Kennzahlzeilen, Nettobarwert als
  Summe, Hinweiszeilen) und „Was ist angenommen?" (Annahmentafel, darunter der Hinweistext, Parameternachweis,
  Zeitraumzeile, „k von n", Vereinfachungen, Nr.-31-Zeile, Bewertungsblock mit den Deklarationen als Klappblock und dem
  Freitext, im Fuß „Bericht erzeugen"). Die Darstellung „ValERI-Bewertung" zeigt Block 1 (Maßnahme, Referenz, Zeitraum,
  Zins, Rechnung nominal), Block 3 (Kennzahltafel, Einordnung), Block 4 (Bandbreite, Vorschlag, Hinweistext, Sensitivität)
  und Block 5 (Deklarationen, nicht monetäre Wirkungen, Nr. 31, Berichtsknopf); an der Stelle von Block 2 steht eine
  Hinweiszeile (E8). Köpfe `WIRT_VALERI_BLOCK_1…5`, Normbezug `WIRT_VALERI_NORM_1…5` als Untertitel.
  `WirtschaftlichkeitSeiteTexte` bündelt die 40 Beschriftungen; die Karten färben über die Tokens `--epos-wirt-empf-*`
  (Grün und Gelb der Ampel, Rot = `--epos-stufe-fehler`, `#B00020`), `forced-colors` ist abgesichert.
- **Hülle und Berichtsweg (E5/8).** Kacheln und Kennzahltafel stehen im Erwartungsfall, unabhängig von der Klappliste; Nr. 31
  erscheint als eine Nachweiszeile statt als Tafelzeile. „Bericht erzeugen" (U44, Q18) reicht über `BerichteKostenHuelle`
  den bestehenden Berichtsweg `BerichtSeiteGaben.ErzeugeFuerVergleich` samt Abbruch hinein: Rückfrage, Fortschritt,
  danach „öffnen?"; ohne Rahmen kein Knopf, kein Primärknopf (Q8: genau einer, „Berechnen"); der Baustein
  Wirtschaftlichkeit ist im Lauf immer dabei.
- **Prüfdaten (E5/8a).** Zwei Tests auf gültige Prüfdaten gestellt, ohne Code: Der E5‑Q7-Fall rechnet mit den Ids 901–903
  (der Vertrag von `Einstufungen(alle, idReferenz)` kennt nur Ids > 0; 0 und kleiner heißt „Stamm ist Referenz"), die
  Sensitivitätsprobe der Hülle mit der Gruppe Wöhler (1040–1042 bilden in der Testdatenbank keine Gruppe).

## Entscheide des Anwenders (22.09.2026) zu den sieben Fragen aus Teil a

Die Fragen heißen hier E5‑Q1 bis E5‑Q7; Code und Tests nennen sie Q1–Q7. Sie sind nicht die Fragen Q1–Q25 der
Mockup-Prüfung, von denen diese Etappe Q8, Q16 und Q18 berührt.

| Frage | Entscheid | Umsetzung |
|---|---|---|
| **E5‑Q1** Vorschlagssatz neben den Karten | behalten (Satz aus #405, Referenz beim Namen) — nach Empfehlung | unter den Karten, in Block 4 und in beiden Berichten |
| **E5‑Q2** Szenarionamen | „Ungünstig"/„Günstig", en „Unfavourable"/„Favourable" — nach Empfehlung | `WIRT_SZEN_WORST`, `WIRT_SZEN_BEST` geändert |
| **E5‑Q3** „nachrichtlich" an der Annuität | offen; Erläuterung an den Anwender: die Annuität ist der Kapitalwert mal Annuitätenfaktor, gleichwertig, in der Tradition der VDI 2067; Empfehlung: ohne Label behalten | vorerst nach Empfehlung — Label nur an Amortisation und Zinsfuß; Frage (1) unter „Offen" |
| **E5‑Q4** Spanne und Einstufung | Spanne als Betrag aus größtem und kleinstem Szenariowert, Einstufung über den schlechtesten und den besten Wert — nach Empfehlung | `BandbreitenZeile.Spanne`, `VariantenEmpfehlung.Schlechtester`/`Bester`; `WIRT_SZ_DELTA_FUSS` erklärt die Spanne |
| **E5‑Q5** Risikodeklaration ohne gepflegten Text | „nicht monetäre Wirkungen: keine benannt" — nach Empfehlung | `WIRT_DEKL_RISIKO_OHNE_NM` |
| **E5‑Q6** Sicht 2 im Bericht rechnete gegen die Gruppenreferenz, die Tafeln nannten A | in Teil b beheben | der Sammler setzt die Sicht vor dem Rechnen |
| **E5‑Q7** Stamm, wenn eine Variante die Referenz ist | Stammkarte — nach Empfehlung | `WirtschaftlichkeitEmpfehlung.Einstufungen(alle, idReferenz)`; die Bandbreite nimmt diese Überladung |

## Abweichungen vom Mockup

1. **Zwei Darstellungen.** Die vier Fragen stehen in der Darstellung „Kennzahlen", die fünf Blöcke als eigene Darstellung
   „ValERI-Bewertung" hinter dem Umschalter (V‑1/K8).
2. **Kacheln zusätzlich über den Karten** — Entscheid V‑3: IZF und Amortisation bleiben mit Label auf den Kacheln.
3. **Kennzahltafel** ohne Spalte „Bedeutung", die Referenz als eigene Spalte, das Label als Marke am Titel: Seite, Word und
   Excel lesen eine Zeilendefinition.
4. **Annuität ohne „nachrichtlich"** (E5‑Q3, vorerst nach Empfehlung).
5. **Bandbreite** ohne den Untertitel „Vorgaben ungepflegt"; der Fußtext erklärt ΔKW und Spanne; die Referenzzeile zeigt
   „—" statt „Unterlassensalternative".
6. **Die Klappliste steht unter der Sensitivitätstafel**; sie steuert nur die Tafel und die Szenariozeile darunter.
7. **Nur die Deklarationen klappen.**
8. **Hinweistext** in der Konzeptfassung (A14): der Titel sagt „heute", der Schlusssatz des Mockups zu den gepflegten Werten
   fehlt — der Text setzt die wirksamen Werte selbst ein (ohne Pflege ±10 %, ±10 %, ±2 a).
9. **„Verlauf…" bleibt** in der Fußleiste bis E6; der Knopf entfällt erst mit dem Verlauf der drei Szenarien auf der Seite.
10. **Nicht gebaut:** Spannen-Balkenbild und Verlauf mit drei Szenarien (E6); Nominalsummen, Differenzspalte und Brückenbild
    in der Gliederung, Tafel „Was daraus im Lauf wird", Fußzeile „Drei Szenarien gerechnet…", Anhang-E-Checkliste (E8, U43)
    — die Etappenzuordnung ist Frage (4).

## Zahlen und Abnahme

- **Teil a (Phase 2):** Kern-Filter (Release) 0 Fehler, Windows-Schale 0 Fehler, SQL-Dialekt-Prüfer 1 567 Texte ohne
  Fundstelle, Designer 7 527 Einträge, wiederholbar; gefiltert Kern 12 Klassen 236/236, bunit Wirtschaftlichkeit 187/187;
  voller Lauf 10 541 Tests (10 540 bestanden, 1 übersprungen: `OracleJahrPlantMitHorizont192UndNeuplanung96`, Bestand);
  Referenzlauf 13/13 PASS über 3 882 737 Werte, 357/357 CSV byte-gleich gegen `2026-09-19_R10_BhkwWirkungsgrad`. Zeugen in
  `ErgebnisansichtTests`: Einstufungsregel je Szenariolage (Theorie über acht Lagen), Bandbreite zahlengleich zu drei
  Einzelläufen und zur Bandbreitentafel des Excel-Berichts, `BerechneBandbreite` schreibt nicht (Fingerabdruck der Gruppe
  1040–1042), Vorzeichenwechsel-Zähler für 0 bis 3 Wechsel samt Nullen und Bitresten, Deklarationen, Q16, U10, U39 an der
  Testdatenbank, Nr. 31, Hülle und Steigung (1041, Zinssatz −4.141,66 €/%-Pkt.).
- **Teil b:** nach E5/8a gefiltert EPOS.UI.Tests 97/97, EPOS.Kern.Tests 185/185. Zeugen: bunit
  `WirtschaftlichkeitErgebnisansichtTests` (19 Fälle: beide Umschalterzustände, vier Köpfe, Karten samt Stammkarte, Tafel mit
  Label, Grund und Warnung, Bandbreite, Klappliste nur für die Tafel darunter, Gliederung, Sensitivität, Hinweistext unter
  der Annahmentafel, Deklarationen, Nutzungsdauer und Nr. 31, Berichtsknopf über die Naht, ValERI-Blöcke, ohne Gaben);
  `WirtschaftlichkeitSeiteTests` und `WirtschaftlichkeitSichtTests` auf die neue Anordnung nachgezogen; Kern
  `ErgebnisansichtEntscheideTests` (15 Fälle: E5‑Q2, Q4, Q5, Q6 an der Prüfgruppe 1040–1042, Q7, Annahmentafel,
  Nachweiszeile, Sensitivität, Schlüssel in beiden Sprachen, Wort- und Tabellenbericht), `ErgebnisansichtTests` (E5‑Q3 und
  Q5 nachgezogen, Hüllentests), `VergleichsauswahlTests` (Darstellung), `WirtschaftlichkeitHuellenPlattformTests`
  (Berichtsknopf im Rahmen), `BerichtBlattstrukturWacheTests`.
- **Gate #434** auf `deba5e57`: Kern-Filter (Release) 0 Fehler, ChartProben alle grün, 91 Bilder, Hashes gleich der
  Windows-Messlatte, Tests 10 624 grün, 1 übersprungen (KiKern 524, SpeicherEngine 386, SpeicherPlanung 27 und 1
  übersprungen, UI 5 237, Kern 4 450), Dokumentationswachen 24/24, Designer 7 567 Einträge unverändert; Windows-Schale auf
  dem Merge-Stand im Worktree 0 Fehler.
- **Ressourcen:** 52 neue Schlüssel de/en. Teil a (12): `WIRT_SZEN_HINWEIS`, `WIRT_DEKL_NOMINAL`, `WIRT_DEKL_STEUERN`,
  `WIRT_DEKL_RESTWERT`, `WIRT_DEKL_RISIKO`, `WIRT_KZ_NACHRICHTLICH`, `WIRT_IZF_MEHRDEUTIG`, `WIRT_IZF_KEIN_WERT`,
  `WIRT_GRUND_KEINE_AMORTISATION`, `WIRT_NACHWEIS_NAECHSTE_RECHNUNG`, `WIRT_SENS_EINHEIT_PUNKT`, `WIRT_SENS_EINHEIT_PROZENT`.
  Teil b (40): `WIRT_UMSCH_KENNZAHLEN`, `_VALERI`, `_TITEL`; `WIRT_ABS_LOHNT`, `_SICHER`, `_WORAUS`, `_ANNAHMEN`;
  `WIRT_BTN_BERICHT`; `WIRT_LBL_EINZELHEITEN`; `WIRT_EMPF_REGEL`, `WIRT_EMPF_KARTE_UNTER`; `WIRT_KZ_TAFEL_TITEL`;
  `WIRT_BB_TITEL`; `WIRT_SENS_TITEL`, `WIRT_SENS_SP_PARAMETER`, `_MINUS`, `_BASIS`, `_PLUS`, `_STEIGUNG`, `_EINHEIT`;
  `WIRT_ANN_TITEL`, `WIRT_ANN_SP_HERKUNFT`, `WIRT_ANN_ZEITRAUM`, `WIRT_ANN_VORGABE`, `WIRT_ANN_GEPFLEGT`,
  `WIRT_ANN_PROJEKTWERT`; `WIRT_DEKL_RISIKO_OHNE_NM`; `WIRT_VALERI_BLOCK_1` bis `_5`, `WIRT_VALERI_BLOCK_2_HINWEIS`,
  `WIRT_VALERI_NORM_1` bis `_5`, `WIRT_VALERI_MASSNAHME`, `WIRT_VALERI_KZ_HINWEIS`. Geändert: `WIRT_SZEN_WORST`
  („Ungünstig"), `WIRT_SZEN_BEST` („Günstig"), `WIRT_SZ_DELTA_FUSS` (erklärt die Spanne). Gestrichen: `WIRT_KACHEL_KEINE`.
  Designer 7 567 Einträge, wiederholbar. Kein Schemaschritt; Nachweisumschlag Fassung 7.
- **Umfang des Merges:** 34 Dateien, 7 372 Zeilen hinzu, 488 entfernt (gegen den ersten Elternteil `58119cc8`); weder
  `EPOS.iOS` noch die Windows-Schale noch eine Wiki-Quelle berührt.

## Abnahme am Gerät (A‑E5‑1, Windows und iPad)

Wirtschaftlichkeitsseite einer Vergleichsgruppe mit gerechnetem Stand, zwölf Punkte: (1) Der Umschalter „Kennzahlen /
ValERI-Bewertung" steht im Kopf, die Wahl hält für die Sitzung. (2) Vier Abschnittsköpfe; die Fußleiste ist in beiden
Darstellungen dieselbe, „Verlauf…" steht noch darin. (3) Je Version eine Empfehlungskarte; ist eine Variante die Referenz,
auch die Stammkarte. (4) Die Kennzahltafel trägt „nachrichtlich (Anhang C)" an Amortisation und Zinsfuß, „— ‹Grund›" in
Zellen ohne Wert und das Warnzeichen beim mehrdeutigen Zinsfuß. (5) Die Bandbreite zeigt Ungünstig · Erwartet · Günstig
nebeneinander; die Klappliste darunter ändert nur die Tafel darunter. (6) Die Sensitivitätstafel nennt Steigung und
Einheit. (7) Unter „Was ist angenommen?" stehen Hinweistext, Zeitraumzeile, „k von n", Nr.-31-Zeile und Deklarationen.
(8) „Bericht erzeugen" liefert Word und Excel, auch in Sicht 2. (9) Die ValERI-Darstellung zeigt die Blöcke 1, 3, 4 und 5
und an der Stelle von Block 2 die Hinweiszeile. (10) Englisch. (11) Schmales Fenster und Hochkontrast. (12) iPad.

## Offen

- **Vier Anwenderfragen aus Teil b:** (1) E5‑Q3 endgültig — Label nur an Amortisation und Zinsfuß? (Empfehlung: ja.)
  (2) Ein eigener Vorschlagssatz für den Stamm, wenn eine Variante die Referenz ist? (Empfehlung: ja, mit neuem Schlüssel.)
  (3) „Bericht erzeugen" fügt den Baustein Wirtschaftlichkeit der gemerkten Gruppenkonfiguration hinzu — dauerhaft oder
  nur für diesen Lauf? (Empfehlung: nur für diesen Lauf.) (4) Etappe der nicht gebauten Mockup-Teile: Spannen-Balkenbild
  mit E6; Nominalsummen, Brückenbild, Tafel „Was daraus im Lauf wird" und Fußzeile „Drei Szenarien gerechnet…" mit E8?
  (Empfehlung: so.)
- **Mockup-Anhang:** Die nicht gebauten Teile ohne eigene Anhangzeile (Spannen-Balkenbild, Nominalsummen und
  Differenzspalte der Gliederung, Tafel „Was daraus im Lauf wird", Fußzeile „Drei Szenarien gerechnet…") bekommen ihre
  Zeilen mit dem Entscheid zu Frage (4).
- **U39-Rest:** Entkopplung von Ersatz und Restwert, geräteeigene Dauerspalten, Anschluss der Speicherflotte (E7/E10).
- **Referenzexport ohne Wirtschaftlichkeit:** `Referenzlauf/Ergebnisexport.cs` liest `Tab_ErgebnisWirtschaftlichkeit`
  nicht; die Erweiterung ist eine Frage der nächsten Basis (A11, E7).
- **Wiki:** Die Quelle `Projekte/Wiki/Programm Dokumentation - Wirtschaftlichkeit.wiki` beschreibt die Seite noch ohne
  Umschalter und vier Abschnitte (Szenarien „Best"/„Worst", kein „Bericht erzeugen") — Nachzug vor dem Sammel-Upload
  26.09.2026. Die Logbuch-Sätze (Version 1.2.0.4) stehen im Update-Papier (#434).
- **Mockup-Nachzug:** Anhangzeilen U2, U4, U5, U10, U44 auf „erledigt", die Hinweiszeile in U39, die Schlüsseltafel der
  Kategorie 8 und die Fehlerfarbe nach Q9 — mit den Papieren dieser Statuszeile.
