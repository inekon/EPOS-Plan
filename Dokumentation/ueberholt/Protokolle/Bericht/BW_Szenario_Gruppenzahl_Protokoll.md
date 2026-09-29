# Bericht — Szenario der Wirtschaftlichkeit und Gruppenzahl im Kostenkapitel (Protokoll)

Zwei Anwenderentscheide vom 27.09.2026 zum Bericht der Wirtschaftlichkeit: **E1** (Nach #582) — der
Wirtschaftlichkeitsbericht folgt dem gewählten Szenario, gebaut nach der Fachvorgabe E31 der Sitzung
„EPOS Plan Wirtschaftlichkeit“ (`Auftraege_Wirtschaftlichkeit_2026-09/E31_Fachvorgabe_Bericht_Szenario_2026-09-29.md`
neben diesem Ordner; vom Anwender am 29.09.2026 bestätigt); **E2** (Nach #555 b) — das Kostenkapitel eines
Berichts mit Ständen einer Vergleichsgruppe weist Kosten und Emissionen nach der Gruppenregel aus. Der gültige Stand
steht in der [Statusdatei](../../../aktuell/Status_iOS_Migration.md) und in den Wiki-Quellen „Wirtschaftlichkeit“ und
„Berichtsvorlagen“; hier steht, wie es geworden ist. Ein Opus-5.5-Agent im Worktree (Zweig
`worktree-agent-a9855eee25a29d61d` ab `903f926`), erst E2, dann E1, je Entscheid ein Commit. Kein Schemaschritt, kein
Rechenweg berührt, keine Referenzbasis neu eingefroren, `EPOS.iOS/` nicht berührt.

| Teil | Gegenstand | Commit |
|---|---|---|
| E2 | Gruppenregel im Berichtslauf, Hinweissatz unter den Tafeln Kosten und Emissionen, Blatt „Vergleich“ | `41c30e3` |
| E1 | Szenario des Wirtschaftlichkeitsberichts nach E31: Konfiguration, Baustein, Mappe, Anhang E, Berichtsseite, Hülle, KI-Feld, Wiki | `5ce61c9` |
| Papiere | dieses Protokoll, Indexzeile | dieser Commit |

## 1 Befund

- **E1.** Die Seite „Wirtschaftlichkeit“ führt ein Szenario der Einzelheiten und gibt es mit „Zum Bericht ›“ als
  `BerichtVorbelegung.SzenarioId/SzenarioText` weiter; die Berichtsseite nannte es nur in der leisen Zeile. Der Baustein
  Wirtschaftlichkeit wählte an neun Stellen fest `WirtschaftlichkeitSzenario.ERWARTET`, die Mappe an einer (Aktualität).
- **E2.** Der Vergleich der Wirtschaftlichkeit bepreist und bewertet den Netzbezug eines Stands ohne stromverwendenden
  Erzeuger nach der Gruppenregel (#555), sobald ein anderer Stand der Gruppe Strom verwendet
  (`WirtschaftlichkeitCtrl.StromGruppenregel`). Das Kostenkapitel des Berichts rechnete jeden Stand für sich — dieselbe
  Version stand im Bericht mit anderen Energiekosten und Emissionen als im Vergleich.

## 2 Entscheidungsgründe

- **E2 im Sammler, einmal für alle Schreiber.** Die Regel greift in `BerichtsDatenSammler.SammleFuerBericht` nach dem
  Sammeln und vor der Wirtschaftlichkeit; Word, Excel und Platzhalter lesen dieselben Zahlen, die Schreiber bleiben
  ohne Datenbank. Die Einzelbetrachtung der App (Kostenseite, Übersicht, Sammler der Wirtschaftlichkeitsseite) bleibt
  je Stand. Ein Bericht mit einem Stand und eine Gruppe ohne Stromverwendung behalten die Einzelzahl. Der Hinweis ist
  der vorhandene Satz `WIRT_HINWEIS_STROM_GRUPPENREGEL`.
- **E1 nach E31, nicht nach dem ersten Entwurf.** Ein erster, ungecommitteter Stand rechnete für ein anderes Szenario
  einen zweiten Lauf samt Sensitivität, hob die Katalogfassung auf 11 (Platzhalter ohne Anhang im Szenario des
  Berichts, Zwillinge `.erwartet`), erzeugte die zehn Vorlagen neu und stellte das Excel-Blatt um. E31 schließt
  Sensitivität je Szenario, Umbau des Excel-Blatts und jeden Rechenweg aus; der Stand wurde deshalb auf die Vorgabe
  zurückgebaut: kein neuer Platzhalter, Katalogfassung 10, Vorlagen unverändert, `WirtschaftlichkeitCtrl` unverändert.
- **Die Wahl liegt in der Konfiguration, der Schreiber liest sie dort.** `BerichtsKonfiguration.Szenario` (JSON je
  Stammprojekt) trägt den Schlüssel aus `WirtschaftlichkeitSzenario`; `SchreibeWord(k, daten, konfig)` und
  `Erzeuge(daten, konfig, ziel)` behalten ihre Signatur. Der Wertesatz fragt die Aktualität für alle drei Szenarien
  vorab, damit der Schreiber in jedem Szenario ohne Datenbank bleibt.
- **Vorgabe Erwartet bleibt Byte für Byte.** Überschrift „Kennzahlen im Szenario „…““ und die Hinweise von Verlauf,
  Mehrjahresübersicht und Betriebskosten sind Formate, die im Erwartungsfall den bisherigen Wortlaut ergeben; die
  Kopfzeile der Mappe steht nur außerhalb von Erwartet. Die sechs Messlatten `Bericht_*` bleiben unverändert.
- **Was Bandbreite ist, bleibt Bandbreite.** Szenarienübersicht mit Tafel, Spannenbild, Annahmenzeilen,
  Szenarioabdeckung und Vorschlag, das Dreierbild des Verlaufs, Punkt 9 der Anhang-E-Checkliste und die Sensitivität
  (Überschrift „Szenario „Erwartet““) bleiben. Strommatrix und Emissionsbilanz sind Größen der Simulation.
- **Rückfall als Ganzes.** Fehlt einem Stand das Ergebnis des gewählten Szenarios, steht der ganze Baustein in Erwartet
  mit einer Hinweiszeile, damit keine Tafel zwei Szenarien mischt; dieselbe Regel nennt der Mappe ihre Kopfzeile.
- **Anhang E, Punkte 1 und 7.** Ihre Stelle nennt die Überschrift der Kennzahltafel. Im Wortbericht nennt sie deshalb
  das Szenario des Berichts; Seite, Mappe und Platzhalter bleiben bei Erwartet. E31 nennt die Punkte nicht, die Stelle
  zeigte sonst auf eine Überschrift, die es im Bericht nicht gibt.

## 3 Umsetzung

### 3.1 E2 — Gruppenzahl im Kostenkapitel

- `BerichtsDatenSammler.StromGruppenregelAnwenden`: ab zwei Ständen die Regel der Wirtschaftlichkeit; für jeden
  betroffenen Stand `StromImVergleichBepreisen` samt Verwender setzen, Kosten, Emissionen (`KostenEmissionRechner`) und
  Kennzahlen neu rechnen, neu entstandene Warnungen (Strommix-Vorgabewert, Leistungspreis ohne Bezugsspitze) melden.
- `VariantenDaten.StromGruppenregelHinweis`, `BerichtsDaten.StromGruppenregelHinweise`: der Satz in der
  Berichtssprache, unter den Tafeln Kosten und Emissionen (`tabelle.vergleich.*`, Variantenvergleich), einmal im Kapitel
  und unter dem Blatt „Vergleich“ der Mappe.

### 3.2 E1 — Konfiguration und Wertesatz

- `BerichtsKonfiguration.Szenario`: Schlüssel, duldsam gelesen, normiert über `WirtschaftlichkeitSzenario.Normiere`;
  fehlend, leer, unbekannt oder anderer Art heißt Erwartet. Gespeichert wird der Schlüssel, auch Erwartet.
- `WirtschaftsBerichtswerte.Berichtsszenario(konfig, out ohneErgebnis)` — die eine Regel samt Rückfall für Baustein,
  Mappe und Anhang E; `ImSzenario(id, szenario)`; `Ermittle` fragt die Aktualität je Stand in allen drei Szenarien.
- Szenarioparameter mit Vorgabe Erwartet an `Berichtsbilder.BarwerteKumuliert`, `Bruecke`/`BrueckeDaten`,
  `Zahlungsstrom`, `Mehrjahrestafel`, `Berichtstabellen.Betriebskosten`, `KwkgModule`, `VermiedeneKosten` und
  `Zahlungsgliederungen.Leitversion` (eine Auswahl, keine Rechnung) — der Platzhalterkatalog ruft sie ohne und bleibt
  bei Erwartet.

### 3.3 E1 — Baustein, Mappe, Anhang E

- Baustein Wirtschaftlichkeit: die neun Stellen — Aktualität, Kennzahltafel mit Überschrift, KWK-Modultafel,
  Betriebskosten, Bild der kumulierten Barwerte (samt Bezug des Verlaufsabschnitts), Brücke mit Leitversion,
  Mehrjahresübersicht mit Zahlungsstrom und vermiedenen Kosten, Bezug der Emissionsbilanz, Rechnungszeilen — im
  Szenario des Berichts; Rückfallzeile `WIRT_BER_SZENARIO_RUECKFALL`.
- Mappe: drei Spaltengruppen unverändert; die Aktualitätsprüfung im Szenario des Wortberichts; außerhalb von Erwartet
  die Kopfzeile `WIRT_BER_SZENARIO_WORTBERICHT` unter dem Titel, fixiert samt Titel und Parameterzeile.
- Anhang E: `ChecklistenLage.Szenario`, `AusBericht(daten, kapitelstellen, szenario)`; die Wortausgabe übergibt das
  Szenario des Berichts.

### 3.4 E1 — Berichtsseite, Hülle, Assistent

- Berichtsseite: Klappliste „Szenario der Wirtschaftlichkeit:“ (`Auswahlfeld` mit `WIRT_SZEN_*`) unter der
  Bausteinliste, nur mit angehaktem Baustein Wirtschaftlichkeit; die Vorbelegung setzt sie, eine unbekannte Nummer
  lässt die gemerkte; die leise Zeile bleibt (`BK_BER_VORBELEGT` sagt jetzt „im gewählten Szenario“); der Auftrag trägt
  `SzenarioId`.
- `BerichtSeiteGaben`: Nummern in der Folge der Wirtschaftlichkeitsseite (0 Erwartet, 1 Günstig, 2 Ungünstig), die
  Persistenzwerte kennt allein die Hülle; `AusAuftrag` legt den Schlüssel in die Konfiguration, die gespeichert und
  Sammler und Schreibern gereicht wird.
- KI-Assistent: Wahlfeld `szenario` der Maske Berichtsseite (`BerichtSeiteKiSicht.Szenario`).
- Neue Ressourcen (beide Sprachen): `WIRT_BER_KENNZAHLEN_SZENARIO`, `WIRT_BER_SZENARIO_RUECKFALL`,
  `WIRT_BER_SZENARIO_WORTBERICHT`, `BK_BER_LBL_SZENARIO`, `KI_DLG_BKB_SZENARIO_ERL`; als Format mit Szenarionamen:
  `WIRT_MJ_HINWEIS`, `WIRT_BK_HINWEIS`, `WIRT_VERL_WORT_HINWEIS`, `WIRT_AE_1/7_STELLE(_WORT)`; Wortlaut: `BK_BER_VORBELEGT`.

### 3.5 Wiki-Quellen

„Wirtschaftlichkeit“: Abschnitte `bericht-gruppenregel` (E2) und `bericht-szenario` (E1), dazu `szenariofuss`,
`bericht-erzeugen`, `bericht`, `bericht-excel`, `bericht-betriebskosten`. „Berichtsvorlagen“: `erstellen`.
Gegengelesen mit dem Muster der Wurzel-`CLAUDE.md`.

## 4 Tests und Abnahme

- **Messlatten byte-gleich:** `Bericht_Word_1030.txt`, `Bericht_Word_1030_Vorlage.txt`, `Bericht_Excel_1030.txt`,
  `Bericht_Word_Gruppe.txt`, `Bericht_Word_Gruppe_Vorlage.txt`, `Bericht_Excel_Gruppe.txt` unverändert, die Fälle
  `Messlatte_Word_und_Excel` und `Messlatte_Vorlagenweg_mit_der_Standardvorlage` grün.
- **Neue Proben (E31 Abschnitt 3):** (a) `BerichtsKonfigurationJsonTests.Das_Szenario_liest_sich_duldsam_und_uebersteht_den_Rundlauf`;
  (b) `BerichtSzenarioTests.Der_Wortbericht_im_Szenario_Unguenstig_zeigt_dessen_Kennzahlen` für 1030 und die Gruppe —
  Überschrift, Kennzahltafel gleich der eines Erwartet-Berichts mit getauschten Ergebnissen und ungleich der
  Erwartet-Tafel, Szenarienübersicht und Sensitivität gleich, Anhang-E-Stelle, Schreiben ohne Datenbank, nichts
  nachgeholt; (c) `…Ohne_Ergebnis_des_Szenarios_faellt_der_Baustein_auf_Erwartet_zurueck`; (d)
  `…Die_Mappe_nennt_das_Szenario_und_behaelt_ihre_drei_Spaltengruppen` — Kopfzeile, fixierte Zeilen, das übrige Blatt
  Zelle für Zelle eine Zeile tiefer; (e) bUnit `BerichtSeiteTests` (Klappliste nur am angehakten Baustein,
  Vorbelegung, „Erstellen“ und Neuaufbau samt KI-Sicht) und der Hüllentest
  `BerichtsvorlagenHuelleTests.Erstellen_reicht_das_Szenario_in_der_Konfiguration_und_merkt_es`. E2:
  `StromGruppenregelTests` mit drei Fällen.
- **Nachgezogen:** Eingabebilanz der Berichtsseite (`KiMaskenabdeckungWacheTests`, 7 Eingabestellen) und Feldzahl der
  Maske Berichtsseite (`KiDialogkatalogTests`, 8 Felder).
- **Läufe:** E2 — Kerntests der Klassen um Bericht und Wirtschaftlichkeit 566 bestanden. E1 — Kern-Filter gebaut ohne
  Fehler; Kerntests der Klassen um Vorlagen, Bericht, Anhang E, Excel, KI-Dialoge, Wirtschaftlichkeit, Auslieferung,
  Lokalisierung (`LokalisierungWirtschaftlichkeitWacheTests`), Hüllentexte (`HuellenTextschluesselWacheTests`),
  Gruppenregel und die Wachen der Dokumentation, der Ordnung und des Wikis 1.119 bestanden, auf dem Endstand der
  Papiere Wachen und neue Proben 46 bestanden; `EPOS.UI.Tests` vollständig 6.844 bestanden; Windows-Schale
  (`EnableWindowsTargeting`) ohne Fehler; ChartProben 221 Bilder ohne Verstoß; SQL-Dialekt-Prüfer 2.043 Texte ohne
  Fundstelle; Referenzlauf 1030, 1007, 1017, 1045, 1046, 1047, 1049 gegen R23 GESAMT: PASS (2.497.873 Werte).

## 5 Offen

- **Unter Windows zu sehen:** die Klappliste an der Bausteinliste, Word-Bericht 1030 in Ungünstig gegen die
  Einzelheiten der Ergebnisseite im selben Szenario (Abnahme der Wirtschaftlichkeit nach E31), die Kopfzeile der Mappe.
- Der Vorlagenweg: Das Kapitel `{{kapitel.wirtschaftlichkeit}}` folgt der Wahl, die Einzelwerte, Tabellen und Bilder
  der Wirtschaftlichkeit im Platzhalterkatalog bleiben bei Erwartet (E31 schließt neue Platzhalter aus). Eine Vorlage,
  die beides mischt, zeigt in Günstig oder Ungünstig zwei Szenarien — bei Bedarf eigene Etappe.
- Die Sensitivität je Szenario, ein Strichartwechsel im Dreierbild und ein wandernder Punkt im Spannenbild stehen nach
  E31 Abschnitt 5 außerhalb dieses Auftrags.
- Den Konzeptabsatz E31 trägt die Wirtschaftlichkeit nach dem Push nach.
- Aus #555 bleiben die Punkte c und d (Leistungspreis und Stromsteuer bei der Gruppenregel).
