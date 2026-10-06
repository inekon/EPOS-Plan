# Konzept Berichtsseite: VALERI-Darstellung und Anordnung

Stand 06.10.2026, Sitzung „EPOS-Plan Berichterstellung". Gegenstand: Seite „Berichte & Kosten › Bericht"
(`EPOS.UI/Seiten/Berichte/BerichtSeite.razor`), Baustein Wirtschaftlichkeit des Berichts und die Anordnung der
Bedienelemente derselben Seite. Kein Rechenweg, kein Schemaschritt, keine neue Referenzbasis — dieses Papier legt
den Entscheid und die Fachvorgabe fest; die Umsetzung läuft als eigene Wellen (Teil C).

**Anlass.** Zwei Anwenderwünsche vom 06.10.2026, wörtlich:

1. „Die Berichte sollen auch mit VALERI-Darstellung (best, worst, expected) erstellt werden können (Auswahl)."
2. „Die Anordnung der Elemente kann optimiert werden (zum Beispiel Dateiauswahl und Ausgabe auf die rechte
   Seite..., prüfe gute Darstellung)."

**Entscheid 06.10.2026:** Der Anwender hat wörtlich „Alle Empfehlungen, Fachvorgabe hier schreiben" verfügt — alle
Fragen beider Entwürfe sind nach Empfehlung entschieden; diese Sitzung (Berichterstellung) schreibt die
Fachvorgabe selbst.

**Quellen.** Fachentwurf VALERI-Darstellung und Layout-Entwurf (Scratchpad dieser Sitzung, nicht im Repository —
ihr Inhalt ist in Teil A und Teil B vollständig übernommen), Mockups
`Mockups/Berichtsseite_Anordnung_A.html`, `Mockups/Berichtsseite_Anordnung_B.html`,
`Mockups/Berichtsseite_Anordnung_C.html` (Bilder
beim Anwender, nicht im Repository — sie zeigen ein Kundenprojekt mit Namen, siehe
Teil B, Nr. 3), Muster
[`Konzept_BerichteKosten_Navigation_EPOS-Plan.md`](../ueberholt/Konzept_BerichteKosten_Navigation_EPOS-Plan.md)
(Aufbau eines Varianten-Konzepts) und
[`E31_Fachvorgabe_Bericht_Szenario_2026-09-29.md`](../ueberholt/Protokolle/Auftraege_Wirtschaftlichkeit_2026-09/E31_Fachvorgabe_Bericht_Szenario_2026-09-29.md)
(Aufbau einer Fachvorgabe).

---

## Teil A — VALERI-Darstellung im Bericht

### 1. Ist-Stand

Mit E31 (#591, Register
[R‑E31](Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)) folgt der Baustein
Wirtschaftlichkeit **einem** gewählten Szenario (Klappliste „Szenario der Wirtschaftlichkeit", Vorgabe Erwartet).
Die Szenarioergebnisse liegen je Stand schon vor (`WirtschaftlichkeitErgebnis` je `Szenario`,
`WirtschaftlichkeitCtrl.Szenariodaten`, drei Verlaufsläufe in `WirtschaftlichkeitVerlaufSzenarien`) — eine
VALERI-Darstellung (Ungünstig, Erwartet, Günstig nebeneinander, wie die Szenarioanalyse nach DIN EN 17463, 7.3,
8.1.3, Abschnitt 9, und die Darstellung „ValERI-Bewertung" der Ergebnisseite, Block 4, es tun) stellt diese Werte
nur anders dar.

**Wortbericht** (`WirtschaftlichkeitBaustein`, `BausteineWirtschaftlichkeit.cs`): Das Szenario kommt einmal aus
`werte.Berichtsszenario(konfig, out ohneSzenario)`, Rückfall auf Erwartet (`WIRT_BER_SZENARIO_RUECKFALL`). Dem
Szenario folgen Aktualitätsprüfung, Kennzahltafel, KWK-Modultafel, Betriebskosten, Bild „Kumulierte Barwerte je
Version", Brücke, Mehrjahresübersicht, Emissionsbilanz und die Rechnungszeilen — jede Methode trägt den Parameter
`szenario` schon. **Alle drei Szenarien zeigen schon heute:** die Szenarienübersicht (Tafel mit ΔKW Ungünstig |
Erwartet | Günstig, Spanne, Amortisation, Einstufung, Spannenbild, Annahmenzeilen je Szenario, Szenarioabdeckung,
Vorschlagstext — der Kern der VALERI-Aussage steht im Wortbericht bereits dreispaltig), das Dreierbild
(`Berichtsbilder.KapitalwertSzenarien`, Farbe = Variante, Strichart = Szenario). Die Sensitivität bleibt auf
Erwartet, Anhang-E-Punkt 9 (`AnhangECheckliste.StandPunkt9`) bleibt bei „Günstig und Ungünstig gerechnet". **Was
fehlt:** die übrigen Kennzahlen (Nettobarwert absolut, Annuität, IZF, Amortisation, Jahreswerte,
Gestehungskosten …), die Positionen und die Jahresreihen von Günstig und Ungünstig.

**Tabellenbericht** (`ExcelBerichtGenerator.BlattWirtschaftlichkeit`, `ExcelFormelmappe`): Das Blatt
„Wirtschaftlichkeit" **ist bereits die VALERI-Form** — Parameterblock je Szenario, drei Kennzahlblöcke „Szenario:
Erwartet / Günstig / Ungünstig", `BandbreitenTafel`, Mehrjahrestabellen je Szenario und Stand mit Kennzahlen als
Formeln, Blatt „Verlauf" mit allen drei Linien. Nur die Betriebskosten (`BlattBetriebskosten`) und die
Sensitivität stehen allein in Erwartet.

**Vorlagenweg** (`Vorlagenfeldkatalog*`, Katalogfassung 14): `{{kapitel.wirtschaftlichkeit}}` folgt der
Konfiguration, würde also auch der VALERI-Darstellung folgen. Szenarienzwillinge gibt es schon
(`stamm.wirtschaft.<zeile>`, `stand.wirtschaft.<zeile>`, `wirtschaft.beste.<zeile>`, `wirtschaft.parameter.<name>`,
`wirtschaft.szenario.<s>.<angabe>`, `tabelle.wirtschaft.kennzahlen(.guenstig|.unguenstig)`,
`stand.bandbreite.(unguenstig|guenstig)`). Ohne Zwilling bleiben `stand.tabelle.mehrjahres`,
`stand.tabelle.betriebskosten`, `stand.tabelle.kwkg_module`, `stand.tabelle.vermiedene_kosten`,
`stand.bild.zahlungsstrom`, `bild.wirtschaft.barwerte_kumuliert`, `bild.wirtschaft.bruecke` — immer Erwartet.

**Bedienung:** `BerichtSeite.razor` zeigt ein `Auswahlfeld` mit `_stand.Szenarien`/`_stand.SzenarioId`, nur
sichtbar bei angehaktem Baustein. Der Setter `BerichtsKonfiguration.Szenario` normiert über
`WirtschaftlichkeitSzenario.Normiere` — ein vierter Schlüssel würde dort still zu Erwartet. Die Ergebnisseite kennt
den Umschalter `WirtschaftlichkeitStand.DARSTELLUNG_VALERI`; `BerichtVorbelegung` trägt ihn heute nicht.

### 2. Varianten der Bedienung

| | V1 vierter Eintrag „Alle drei Szenarien (VALERI)" | V2 eigenes Häkchen „VALERI-Darstellung" | V3 eigener Baustein „ValERI-Bewertung" |
|---|---|---|---|
| Bedienung | eine Klappliste, eine Wahl | zwei Steuerelemente; Kombination VALERI + Leitszenario Ungünstig möglich | neues Häkchen in der Bausteinliste, eigenes Kapitel |
| Ablage | **nicht** in `Szenario` (Normiere); eigenes Feld `BerichtsKonfiguration.Szenariodarstellung` (`EINZELN`/`VALERI`, duldsam, Vorgabe `EINZELN`), `Szenario` bleibt Erwartet | dasselbe Feld als `bool` | neuer `B_VALERI`, `Berichtskapitel`-Eintrag, `Blattbausteine`, `{{kapitel.valeri}}`, Katalogfassung 15 |
| Altbestand | alte Fassung liest das unbekannte Feld nicht → Erwartet einzeln | dito | alter Stand kennt den Baustein nicht |
| KI-Sicht / Wachen | ein Eintrag mehr im Wahlfeld `szenario`; `KiMaskenabdeckungWacheTests`, `KiDialogkatalogTests` unverändert | neues Feld → beide Wachen nachziehen | neues Häkchen → Wachen, Hilfe, Wiki |
| Doppelung | keine | keine | Inhalt doppelt zum Kapitel Wirtschaftlichkeit, wenn beide angehakt |
| Vorbelegung | `BerichtVorbelegung` kann aus `DARSTELLUNG_VALERI` der Ergebnisseite „VALERI" setzen | dito | — |

**Bewertung.** V1 ist für den Anwender am klarsten („welche Szenarien will ich sehen?") und hält das Leitszenario
der übrigen Tafeln fest auf Erwartet — normgerecht und ohne die Mischlage aus § 6.3 Nr. 37. V2 erlaubt eine
fachlich kaum sinnvolle Kombination und kostet ein Feld der KI-Sicht; V3 ist der teuerste Weg und doppelt Inhalt.
**Empfehlung V1**, in einem eigenen Konfigurationsfeld (nicht über `Normiere`).

### 3. Varianten der Darstellung im Kapitel Wirtschaftlichkeit

Maßstab: `WordKontext.MAX_VARIANTEN_JE_BLOCK` = 3, Kennzahltafel ~22 Zeilen, Kopf einzeilig
(`Berichtstabelle.MitKopf`, keine Spaltengruppenköpfe), Mehrjahrestafel bis 13 Spalten.

**D1 — dreispaltig je Tafel.** Kennzahltafel, Mehrjahresübersicht, Brücke, Positionstafeln je Stand mit Ungünstig
| Erwartet | Günstig. Die Kennzahltafel sprengt bei mehreren Ständen die Blockgröße (6 Zahlspalten bei Referenz +
Stand), die Mehrjahrestafel (13 Spalten) lässt sich nicht verdreifachen und zerfällt in drei Tafeln, die Brücke
bräuchte ein gruppiertes Bild (neuer Bildtyp im `ChartRenderer`). Excel bräuchte einen Betriebskostenblock je
Szenario (Umbau `BlattBetriebskosten`, berührt E31 § 5 „Umbau des Excel-Blatts"). Aufwand 7–9 PT, Risiko: neue
ChartProben-Bilder, Messlatten nur bei sauberer Weiche byte-gleich.

**D2 — drei Teilkapitel nacheinander.** Der szenariofolgende Teil (§ 1) läuft dreimal: Ungünstig, Erwartet,
Günstig, je mit eigener Überschrift — die Methoden nehmen `szenario` schon, die Formate tragen den
Szenarionamen schon. Bei drei Ständen grob +10 bis +20 Seiten, der Vergleich der Szenarien verlangt Blättern. Für
Einzelplatzhalter fehlen Zwillinge der sieben Platzhalter ohne Szenario → Katalogfassung 15, zehn Vorlagen neu.
Aufwand 2–3 PT (ohne Zwillinge), 4–5 PT mit. Risiko: Überschriften im Inhaltsverzeichnis dreifach gleichlautend
ohne Szenarioname je Instanz, Anhang-E-Stellen zeigen auf eine von drei Überschriften.

**D3 — Mischform.** Kennzahltafel je Szenario nebeneinander, alles Übrige im Erwartungsfall, Verweis auf die
Formelmappe. Neue Tafel `Berichtstabellen.WirtschaftskennzahlenSzenarien(daten, werte, stand, englisch, kultur)`:
je Stand eine Tafel „Kennzahlen je Szenario — ‹Stand›", Spalten Kennzahl | Ungünstig | Erwartet | Günstig (genau
drei Zahlspalten = Blockgröße, Kopf aus `WIRT_SZ_SP_WORST/ERWARTET/BEST`), Zeilen aus `WirtschaftlichkeitZeilen.
Sichtbare`, Referenzstand mit „(Referenz)" in den Δ-Zeilen; sie tritt an die Stelle der einen Kennzahltafel, danach
wie heute Erwartet. Mehrjahresübersicht, Brücke, Barwertbild, KWK- und Positionstafeln bleiben Erwartet; eine neue
Zeile `WIRT_BER_VALERI_MAPPE` (de/en) verweist auf die Formelmappe für die Jahresreihen. Excel unverändert bis auf
eine Kopfzeile. Vorlagenweg: ein neuer Platzhalter `stand.tabelle.wirtschaft_szenarien` → Katalogfassung 15.
Aufwand 3–4 PT inkl. Bedienung V1, Risiko gering (eine Weiche, keine neuen Bilder).

**Empfehlung D3.** Sie liefert den Mehrwert (alle Kennzahlen dreier Szenarien nebeneinander) an der Stelle, an der
der Leser vergleicht, ohne den Bericht zu verdreifachen, und überlässt die Jahresreihen der Formelmappe, die sie
schon formelbasiert je Szenario trägt.

### 4. Was unverändert bleibt

Alle E31-Regeln für die Einzelwahl (Erwartet/Günstig/Ungünstig), der Rückfall auf Erwartet
(`Berichtsszenario`), die Szenarienübersicht mit Tafel, Spannenbild, Annahmen, Abdeckung, Vorschlag, das
Dreierbild, die Sensitivität (Erwartet), Punkt 9 der Anhang-E-Checkliste, Strommatrix und Emissionsbilanz (Größen
der Simulation), die drei Blöcke des Excel-Blatts, `WirtschaftlichkeitCtrl` und jeder Rechenweg; der Referenzlauf
bleibt ohne Wirkung. Ohne VALERI-Wahl sind Wort- und Tabellenbericht byte-gleich. D3 berührt keinen E31-§-5-Punkt
(keine Sensitivität je Szenario, kein Strichartwechsel, kein wandernder Punkt, kein Umbau des Excel-Blatts außer
der einen Kopfzeile). **Rückfall in VALERI:** Fehlt einem Stand Günstig oder Ungünstig, steht die ganze Tafel
einspaltig (Erwartet) mit Hinweiszeile `WIRT_BER_VALERI_RUECKFALL` (VB‑Q4).

### 5. Entscheidtafel VB‑Q1 bis VB‑Q9

| Kennung | Frage | Lesarten | Empfehlung | Entscheid 06.10.2026 |
|---|---|---|---|---|
| **VB‑Q1** | Bedienung | a V1 vierter Eintrag · b V2 Häkchen · c V3 Baustein | **a** | **a** — vierter Klapplisten-Eintrag „Alle drei Szenarien (VALERI)" |
| **VB‑Q2** | Darstellung | a D3 Mischform · b D2 drei Teilkapitel · c D1 dreispaltig je Tafel | **a** | **a** — D3 Mischform: neue Tafel „Kennzahlen je Szenario" je Stand, Spalten Ungünstig \| Erwartet \| Günstig, alles Übrige bleibt Erwartet |
| **VB‑Q3** | Leitszenario der übrigen Tafeln in VALERI | a fest Erwartet · b wählbar (verlangt V2) | **a** | **a** — Leitszenario fest Erwartet |
| **VB‑Q4** | Rückfall, wenn einem Stand Günstig/Ungünstig fehlt | a ganze Tafel Erwartet mit Hinweiszeile (E31-Regel) · b Strich in der Spalte | **a** | **a** — ganze Tafel Erwartet mit Hinweiszeile |
| **VB‑Q5** | Paarsicht (Sicht 2) | a je Stand A und B eine Tafel · b nur „B gegen A" | **a** | **a** — je Stand A und B eine Tafel |
| **VB‑Q6** | Vorbelegung aus der Ergebnisseite | a Darstellung „ValERI-Bewertung" belegt VALERI vor · b nur das Szenario wie heute | **a** | **a** — Darstellung „ValERI-Bewertung" belegt VALERI vor |
| **VB‑Q7** | Anhang E, Punkte 1, 7, 9 in VALERI | a Stelle nennt die Tafel „Kennzahlen je Szenario", Stand Punkt 9 unverändert · b alles unverändert | **a** | **a** — Punkte 1, 7, 9 nennen die neue Tafel, Stand Punkt 9 unverändert |
| **VB‑Q8** | Vorlagenweg | a Platzhalter `stand.tabelle.wirtschaft_szenarien`, Katalogfassung 15 · b kein neuer Platzhalter | **a** | **a** — Platzhalter `stand.tabelle.wirtschaft_szenarien`, Katalogfassung 15 |
| **VB‑Q9** | Excel | a Kopfzeile „Wortbericht in VALERI-Darstellung" · b keine Änderung | **a** | **a** — Excel-Kopfzeile „Wortbericht in VALERI-Darstellung" |

### 6. Fachvorgabe VB

Nach dem Muster der Fachvorgabe E31. Fachliche Führung, Konzeptabsatz (§ 2.13 (5) der
[Wirtschaftlichkeits-Konzeption](Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md))
und Abnahme liegen bei der Sitzung Wirtschaftlichkeit; Registereintrag → R‑E32.

**Regeln (aus den Entscheiden VB‑Q1 bis VB‑Q9):**

1. **Bedienung.** Die Klappliste „Szenario der Wirtschaftlichkeit" am Baustein erhält einen vierten Eintrag „Alle
   drei Szenarien (VALERI)". Die Wahl liegt **nicht** im Feld `Szenario` (das bleibt über `Normiere` auf Erwartet,
   Günstig oder Ungünstig beschränkt), sondern in einem eigenen Feld `BerichtsKonfiguration.Szenariodarstellung`
   (`EINZELN`/`VALERI`, duldsam, Vorgabe `EINZELN`). Ein fehlendes oder unbekanntes Feld liest sich als `EINZELN`
   — Altbestand und alte Vorlagenpakete bleiben gültig.
2. **Darstellungsregel der Tafel.** Bei `VALERI` tritt die neue Tafel `WirtschaftskennzahlenSzenarien` je Stand an
   die Stelle der einzelnen Kennzahltafel (Spalten Ungünstig | Erwartet | Günstig, drei Zahlspalten =
   Blockgröße, Zeilen aus `WirtschaftlichkeitZeilen.Sichtbare`, Referenzstand mit „(Referenz)" in den Δ-Zeilen).
   Alle übrigen Stellen des Kapitels (Mehrjahresübersicht, Brücke, Barwertbild, KWK- und Positionstafeln,
   Rechnungszeilen) bleiben im Erwartungsfall; eine neue Zeile `WIRT_BER_VALERI_MAPPE` (de/en) verweist auf die
   Formelmappe für die Jahresreihen von Günstig und Ungünstig.
3. **Leitszenario.** Das Leitszenario der übrigen Tafeln in der VALERI-Darstellung ist fest Erwartet, nicht
   wählbar.
4. **Rückfall.** Fehlt einem Stand das Ergebnis von Günstig oder Ungünstig, steht seine ganze Tafel „Kennzahlen je
   Szenario" einspaltig (Erwartet) mit Hinweiszeile `WIRT_BER_VALERI_RUECKFALL` — dieselbe Rückfalllogik wie E31
   § 2 (5).
5. **Paarsicht.** Zeigt der Bericht die Paarsicht (Sicht 2, „A gegen B"), erhält jeder Stand (A und B) seine
   eigene Tafel „Kennzahlen je Szenario", wie die einzelne Kennzahltafel es heute an beiden Ständen tut.
6. **Vorbelegung.** `BerichtVorbelegung` setzt `Szenariodarstellung = VALERI`, wenn die Ergebnisseite in der
   Darstellung „ValERI-Bewertung" steht; sonst bleibt `EINZELN` mit dem gewählten Einzelszenario wie bisher
   (E31‑Regel 1).
7. **Anhang E.** Die Punkte 1 und 7 der Anhang-E-Checkliste nennen in der VALERI-Darstellung die Tafel „Kennzahlen
   je Szenario" statt der einzelnen Kennzahltafel; Punkt 9 (Szenarioabdeckung) bleibt unverändert — er prüft, ob
   Günstig und Ungünstig gerechnet sind, unabhängig von der Darstellung.
8. **Vorlagenweg.** Neuer Platzhalter `stand.tabelle.wirtschaft_szenarien` (Stand-Kontext, Kapitel
   Wirtschaftlichkeit), Katalogfassung **15**. `{{kapitel.wirtschaftlichkeit}}` folgt der Wahl automatisch;
   Einzelplatzhalter bleiben Erwartet (kein Mischfall, weil das Leitszenario Erwartet ist, Regel 3).
9. **Excel.** Bei `VALERI` trägt das Blatt „Wirtschaftlichkeit" eine Kopfzeile „Wortbericht in
   VALERI-Darstellung" (Format analog `WIRT_BER_SZENARIO_WORTBERICHT`); das Blatt selbst bleibt unverändert (es
   trägt die drei Blöcke schon).
10. **Rechenweg unberührt.** Keine Stelle dieser Regeln ändert einen Rechenwert; `WirtschaftlichkeitCtrl`, die
    Simulation und der Referenzlauf bleiben unberührt.

**Prüfungen:**

- Die sechs Bericht-Messlatten (`Bericht_Word_*`, `Bericht_Excel_*`) bleiben byte-gleich (VALERI aus, Vorgabe).
- Neue Messlatte `Bericht_Word_1030_Valeri.txt` (Projekt 1030, Baustein Wirtschaftlichkeit, `Szenariodarstellung =
  VALERI`).
- Proben in `BerichtSzenarioTests`: drei Spalten = Ergebnisse je Szenario, Szenarienübersicht und Sensitivität
  gleich der Erwartet-Ausgabe, Rückfall bei fehlendem Szenario.
- Katalogliste v15 (`BerichtVorlagenMesslatteTests`, `BerichtsvorlageDateiWacheTests`,
  `AuslieferungsvorlagenWacheTests`).
- bunit (`BerichtSeiteTests`): vierter Klapplisteneintrag, Vorbelegung aus `DARSTELLUNG_VALERI`.

**Zuständigkeiten:** Registereintrag **R‑E32** (Entscheidungsregister Wirtschaftlichkeit), Konzeptabsatz § 2.13
(5) der konsolidierten Konzeption; fachliche Führung und Abnahme bei der Sitzung Wirtschaftlichkeit, Bauplatz nach
Verfügbarkeit (Berichterstellung oder Wirtschaftlichkeit).

**Nicht Teil dieses Auftrags:** Sensitivität je Szenario (bleibt Erwartet), Umbau des Excel-Blatts (außer der einen
Kopfzeile), neue Diagrammbilder (ChartProben unverändert), eine dreispaltige Mehrjahrestafel (D1, nicht gewählt).

### 7. Etappenplan VB‑E1 bis VB‑E6

| Etappe | Inhalt | Abnahme |
|---|---|---|
| **VB‑E1** | Konfiguration: Feld `Szenariodarstellung` (duldsam, Vorgabe `EINZELN`), Regel in `WirtschaftsBerichtswerte` (VALERI ⇒ Leitszenario Erwartet, Rückfallliste je Stand) | `BerichtsKonfigurationJsonTests` (fehlend, unbekannt, gültig, Rundlauf); Kern-Filter gebaut |
| **VB‑E2** | Tafel `WirtschaftskennzahlenSzenarien`, Weiche in `SchreibeVergleich`, Überschrift und Hinweise als Ressourcen de/en, Rückfallzeile; Anhang-E-Stelle nach Regel 7 | sechs Messlatten `Bericht_*` byte-gleich; neue Messlatte `Bericht_Word_1030_Valeri.txt`; Proben in `BerichtSzenarioTests`; `BerichtSchreiberOhneDatenbankWacheTests`, `LokalisierungWirtschaftlichkeitWacheTests` |
| **VB‑E3** | Excel-Kopfzeile (nur VALERI) | `Bericht_Excel_*` byte-gleich; Probe Kopfzeile + übriges Blatt eine Zeile tiefer |
| **VB‑E4** | Platzhalter `stand.tabelle.wirtschaft_szenarien`, Katalogfassung 15, Vorlagen und Baukästen neu über `Werkzeuge/Berichtsvorlage` | `BerichtVorlagenMesslatteTests`, `BerichtsvorlageDateiWacheTests`, `AuslieferungsvorlagenWacheTests`, Vorlagenprüfer |
| **VB‑E5** | Bedienung: vierter Eintrag der Klappliste (`BerichtSeiteGaben`), Vorbelegung aus `DARSTELLUNG_VALERI`, KI-Wahlfeld | bUnit `BerichtSeiteTests`, `BerichtsvorlagenHuelleTests`, `HuellenTextschluesselWacheTests`, KI-Wachen; Windows-Schale mit `EnableWindowsTargeting` ohne Fehler |
| **VB‑E6** | Wiki „Wirtschaftlichkeit" (`bericht-szenario`) und „Berichtsvorlagen", Logbuchsatz | Gegenlesen mit dem Muster der Wurzel-`CLAUDE.md`, `WikiProduktdatenWacheTests` |

Je Etappe: Bau des Kern-Filters, Tests mit `--filter` auf die betroffenen Klassen, SQL-Dialekt unberührt; ein Gate
der Orchestrierung nach dem Merge (ChartProben unverändert, Referenzlauf gegen die aktuelle Basis ohne Abweichung).

---

## Teil B — Anordnung der Berichtsseite

### 1. Befund

| Frage | Elemente | heute |
|---|---|---|
| Was wird berichtet? | Variantentabelle mit „Alle"/„Keine", Berichtsbausteine (8 Häkchen, weiche Sperre), Szenario der Wirtschaftlichkeit | Varianten links oben; Bausteine und Szenario rechts unter der Vorlage |
| Wie sieht es aus? | Word-Vorlage mit Schloss, Leiste „Neue Vorlage…" bis „…", Prüfzeile, Originalzeile, bei Excel/Beide die Zeile „Excel-Vorlage" | rechts oben |
| Wohin? | Ausgabe Word/Excel/Beide, Zielordner mit „Durchsuchen…" | unter beiden Spalten, ganz unten links |
| Auslösen | „Erstellen", Fortschritt/„Abbrechen", Erklärzeile „Jeder Bericht rechnet neu …" | „Erstellen" ganz unten links; die Erklärzeile rechts unter dem Szenario |
| Ergebnis | Erfolgszeile mit „Öffnen" (Verfall), Warnungen, Hinweisklappe | **ganz oben**, über beiden Spalten |

**Was stört:**

1. **Leerfläche:** Ausgabe, Zielordner und Erstellen stehen unter dem Spaltenraster; bei zwei Varianten hat die
   linke Spalte nur rund 200 px Inhalt, darunter bleibt die Fläche leer.
2. **Trennung von Konfiguration und Auslösung:** Die Ausgabe (Word/Excel/Beide) steuert die Zeile
   „Excel-Vorlage" oben rechts, steht aber unten links — Ursache und Wirkung liegen diagonal.
3. **Lesereihenfolge:** links oben → rechts oben → rechts unten → zurück nach links unten. Die Erklärzeile des
   Hauptknopfs steht in der anderen Spalte; die Erfolgszeile erscheint oben, weit weg vom Knopf.
4. **Hausregel verletzt:** `EPOS.UI/CLAUDE.md` verlangt für einen Zweispalten-Reiter einen Bedienblock (feste
   Breite, ein Hauptknopf mit leiser Erklärzeile); die Seite hat keinen, die Spalten sind `auto-fit,
   minmax(320px, 1fr)`.

### 2. Varianten (Mockups)

Mockups: `Mockups/Berichtsseite_Anordnung_A.html`, `Mockups/Berichtsseite_Anordnung_B.html`,
`Mockups/Berichtsseite_Anordnung_C.html` — eigene
Vorschauseiten mit eingebettetem Stil, Token wörtlich aus `epos-ui.css`. Die Bilder der ursprünglichen Prüfung
(1.890/1.280/820 px) liegen beim Anwender, nicht im Repository: Sie zeigen ein tatsächliches Projekt mit
Kundennamen; die Mockup-HTML dieses Konzepts tragen stattdessen das Beispielprojekt „Beispielprojekt mit
Wärmepumpe".

**A — Lücke schließen.** Zwei gleiche Spalten wie heute; links unter der Variantentabelle Ausgabe, Zielordner,
Erstellen, Ergebnis; rechts unverändert Vorlage, Bausteine, Szenario. Lesereihenfolge: Varianten → Ausgabe →
**Erstellen** → Vorlage → Bausteine — der Knopf steht vor der Hälfte der Entscheidungen. Unter 900 px: Erstellen
mitten in der Seite, nur mit CSS-`order` behebbar.

**B — Konfiguration links, Ausgabe rechts** (Anwenderhinweis). Links Karte „Varianten" und Karte „Inhalt"
(Bausteine, Szenario); rechts eine Spalte fester Breite mit Karte „Vorlage" und Karte „Ausgabe" (Format,
Zielordner, **Erstellen** + Erklärzeile, Erfolgszeile, Hinweise). Lesereihenfolge: Varianten → Inhalt → Vorlage →
Ausgabe → **Erstellen** → Ergebnis; der Weg endet rechts unten beim Knopf. Unter 900 px dieselbe Folge einspaltig,
Markup-Reihenfolge = Lesefolge.

**C — drei Zonen.** Variantentabelle als Karte über die volle Breite, darunter drei gleiche Karten „Bausteine und
Szenario" | „Vorlage" | „Ausgabe" (mit Erstellen). Lesereihenfolge: oben, dann links nach rechts. Bei 900–1.099 px
zwei Spalten mit „Ausgabe" darunter über die volle Breite; unter 900 px einspaltig.

**Messung (Mockup, Inhalt rollt im Blatt):** Überlauf bei 1.890 × 1.010 px: A 0, B 0, C 0 px. Bei 1.280 × 800 px:
A 17 px, B 82 px, C 121 px — ohne Ergebnis (vor dem Lauf) ist „Erstellen" in A und B sichtbar, in C knapp
darunter.

### 3. Bewertung

| Kriterium | A Lücke schließen | B links/rechts | C drei Zonen |
|---|---|---|---|
| Lesefluss | – Knopf vor Vorlage und Bausteinen | ++ links → rechts, endet beim Knopf | + oben, dann links → rechts |
| Leerfläche | + links gefüllt, rechts wie heute | ++ beide Spalten etwa gleich hoch | + unter den Karten bei 1.890 px gering |
| Nähe Entscheidung ↔ Auslösung | o Ausgabe am Knopf, Vorlage diagonal | ++ Vorlage direkt über Ausgabe und Knopf | + Vorlage neben Ausgabe |
| viele Varianten (10+) | – Tabelle schiebt Erstellen nach unten | + nur die linke Spalte wächst, Knopf bleibt oben rechts | – Tabelle schiebt alle drei Karten nach unten |
| schmales Fenster (< 900 px) | – Erstellen mitten in der Seite | ++ fachliche Folge ohne `order` | + fachliche Folge, Zwischenstufe nötig |
| 1.280 px | + | + (Knopfleiste der Vorlage bricht ab ≈ 430 px um) | o drei Spalten je ≈ 390 px, Vorlagenknöpfe brechen um |
| Hausregel Bedienblock | – keiner | ++ rechte Spalte ist der Bedienblock | o drei gleichrangige Karten |
| Änderungsaufwand Razor/CSS | klein: ≈ 30 Zeilen Markup verschieben, CSS 0 | mittel: Raster neu, 4 Karten, Ergebnisblock verschieben; CSS ≈ 40 Zeilen | mittel: wie B plus Zwischenstufe; CSS ≈ 45 Zeilen |
| Risiko bunit-Tests | 0 von 95 | 1 von 95 | 1 von 95 |

**bunit-Bestand** (`EPOS.UI.Tests/Seiten/BerichtSeite*Tests.cs`, 95 Testmethoden): 16 greifen Knöpfe über die
Position in `.epos-leiste` (Alle, Keine, Erstellen) — halten in allen drei Varianten, solange die DOM-Folge bleibt;
1 prüft, dass die Gruppe „Vorlage" vor der `epos-mehrfachauswahl` steht — bricht in B und C (wird auf die neue
Ordnung umgeschrieben); die übrigen (Liste vor Banner, einspaltiges `Formularraster`, Erfolgszeile über
`.epos-bericht-erfolg`) halten unverändert.

**Empfehlung B.** Sie erfüllt den Anwenderhinweis wörtlich, ist die einzige mit fachlicher Lesefolge in jeder
Breite ohne `order`-Kniff, setzt die Hausregel „Zweispalten-Reiter bekommt einen Bedienblock" um (Vorbild
`epos-simreiter`) und bringt Ursache und Wirkung zusammen: Ausgabe steuert die Excel-Zeile der Vorlage direkt
darüber, das Ergebnis erscheint am Knopf. Kosten: ein umzuschreibender Test, mittlerer Markup-Umbau ohne Rechen-
oder Textänderung.

### 4. Umbruch unter 900 px

Hausschwelle 900 px (wie der Bedienblock der Simulation): Variante B fällt auf eine Spalte, Markup-Reihenfolge =
Lesefolge (Varianten → Inhalt → Vorlage → Ausgabe → Erstellen → Ergebnis), ohne CSS-`order`.

### 5. Entscheidtafel BL‑Q1 bis BL‑Q5

| Kennung | Frage | Lesarten | Empfehlung | Entscheid 06.10.2026 |
|---|---|---|---|---|
| **BL‑Q1** | Variante | a A · b B · c C | **b** | **b** — links Karten „Varianten" und „Inhalt", rechts Spalte fester Breite mit Karten „Vorlage" und „Ausgabe" samt Erstellen, Erklärzeile, Ergebnis |
| **BL‑Q2** | Breite der rechten Spalte | a fest `minmax(420px, 480px)` · b mitwachsend `clamp(420px, 36%, 560px)` | **a** | **a** — feste Breite `minmax(420px, 480px)` |
| **BL‑Q3** | Springende Optionsgruppe „Excel-Vorlage" | a hinnehmen · b in die Karte „Ausgabe" unter die Optionsgruppe · c Platz freihalten | **b** | **b** — Zeile „Excel-Vorlage" in die Karte „Ausgabe" unter die Optionsgruppe |
| **BL‑Q4** | Warnungen nach dem Lauf | a in die Karte „Ausgabe" · b oben über der Seite lassen | **a** | **a** — Warnungen nach dem Lauf in die Karte „Ausgabe", nur Seitenfehler oben |
| **BL‑Q5** | Kartentitel | a „Varianten"/„Inhalt"/„Vorlage"/„Ausgabe" als Kartenköpfe, bisherige Beschriftungen bleiben · b ohne Kartenköpfe | **a** | **a** — Kartentitel wie a, bisherige Beschriftungen bleiben |

### 6. Umsetzungsschritte und Testfolgen (für B)

1. `BerichtSeite.razor`: `epos-seite-spalten` durch ein eigenes Raster ersetzen (`epos-bericht-raster` mit
   `-links`/`-rechts`); links Karte Varianten, Karte Inhalt (Mehrfachauswahl/Inhaltszeile, Szenario,
   Bausteinmeldung); rechts Karte Vorlage (Block `epos-vorlage` unverändert), Karte Ausgabe (Optionsgruppe,
   `Formularraster` mit Dateiwahl, Fortschritt/Status, Leiste mit Erstellen/Abbrechen, Herleitungszeile neben dem
   Knopf, darunter Erfolgszeile, Warnungen, Hinweisklappe). Keine neue `epos-leiste` vor Erstellen; die
   Wurzel-`div` und alle Klassen der Bausteine (`epos-vorlage*`, `epos-bericht-erfolg`) bleiben.
2. `epos-ui.css`: Raster `minmax(0, 1fr) minmax(420px, 480px)`, Umbruch bei 900 px einspaltig, Kartenregel aus
   vorhandenen Token (`--epos-karte-*`), Leiste „Erstellen + Erklärzeile" mit Umbruch; `forced-colors` für die
   Kartenrahmen. `StilblattTests` um die neue Klasse ergänzen.
3. Bei BL‑Q3: `ExcelzeileSichtbar`-Block in die Karte Ausgabe verlegen.
4. Tests: `Die_Gruppe_steht_ueber_den_Bausteinen…` auf „Vorlage-Karte vor Ausgabe-Karte, Bausteine in der linken
   Spalte" umschreiben; neuer bunit-Fall „Erfolgszeile steht in der Karte Ausgabe nach Erstellen"; Lauf `--filter
   BerichtSeite` (95 + 1).
5. Sichtprüfung bei 1.890/1.280/820 px, Wiki-Bild „Bericht" im nächsten Sammel-Upload; Logbuch: „Die
   Berichtsseite ordnet Ausgabe und Erstellen rechts neben der Auswahl an."

---

## Teil C — Status

| Welle | Inhalt | Stand |
|---|---|---|
| Konzept (dieses Papier) | Entscheide VB‑Q1–Q9, BL‑Q1–Q5 nach Empfehlung, Fachvorgabe VB, Mockups, Registereintrag R‑E32 | Statuszeile #761 |
| Layout B | Umsetzung Teil B, Schritte 1–5 | eigene Welle, läuft, Nummern folgen |
| VALERI VB‑E1–E5 | Umsetzung Teil A, Fachvorgabe VB | eigene Welle, läuft, Nummern folgen |
| VB‑E6 | Wiki-Quelle Berichtsvorlagen/Wirtschaftlichkeit, Logbuchsatz | offen, nach VB‑E1–E5, Version beim Anwender erfragen |
