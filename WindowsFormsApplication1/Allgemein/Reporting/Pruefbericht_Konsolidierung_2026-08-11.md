# Prüf- und Konsolidierungsbericht — Berichtsmodul & Wirtschaftlichkeit EPOS-Plan

**Prüf-Session (Cloud) · 11.08.2026 abends** · Auftrag: Datenstruktur `Kenndaten.accdb` und Code
`C:\Waermeplan\WP_Plan\WindowsFormsApplication1` gegen die Konzepte prüfen; Konzepte auf
Konsistenz prüfen und konsolidieren.

---

## 1. Wichtigste Erkenntnis vorab

Die drei im Chat angehängten Konzeptdateien waren **ältere Stände** (Fassung 2 bzw.
Erstfassung vom 10./11.08. vormittags). Im Repo (`Allgemein\Reporting\`) liegt bereits eine
von einer parallelen Cowork-Session erstellte **Konsolidierung (Fassung 3)**: das Leitkonzept
`Konzept_Berichtserstellung_EPOS-Plan.md` (in dem `Konzept_Variantenbericht.md` aufgegangen
ist) plus `Konzept_Wirtschaftlichkeit.md` als Begleitkonzept — und der Code enthält die
**Umsetzung Phase 1–6** (Berichtsmodul komplett, Wirtschaftlichkeit Stufe W1), zuletzt
geändert um ca. 22:55 Uhr.

Diese Prüf-Session hat deshalb (a) die Konsolidierung **unabhängig am Code nachgeprüft**
(drei Opus-Prüfläufe: Berichtsmodul, Kosten/DB-Schema, Wirtschaftlichkeit W1 inkl.
numerischer Nachrechnung), (b) die verbliebenen **Inkonsistenzen zwischen Konzept und Code
bereinigt** und beide Dokumente als **Fassung 3.1** fortgeschrieben.

## 2. Konsistenzprüfung der Konzepte

**Zwischen den drei hochgeladenen Fassungen** gab es erhebliche Widersprüche
(u. a. `Form_Bericht` vs. `Form_VariantenBericht`, DB- vs. Registry-Persistenz der
Berichtskonfiguration, `Berichtsvorlage.docx` vs. `Vorlage_Bericht3.dotx`,
Blocksplitting-Hochformat vs. Querformat ab 4 Varianten, Neubau `Allgemein/Bericht/` vs.
Erweiterung `ProjektvergleichBericht`). Diese sind durch die Fassung 3 sämtlich
**entschieden** (Kombinations-Architektur, DB-Persistenz, neue Vorlage, Blocksplitting zu 3
im Hochformat) — und der Code folgt der Fassung 3. Die hochgeladenen Altfassungen können als
überholt gelten; der Verweis-Stub `Konzept_Variantenbericht.md` im Repo sagt das bereits.

**In der Fassung 3 selbst** blieben vier Inkonsistenzen zum Code, jetzt in 3.1 bereinigt:

| # | Fassung 3 sagte | Code macht | Bereinigung 3.1 |
|---|---|---|---|
| 1 | Charts über **ScottPlot** (Kap. 2, 6, 8.1) | `ChartRenderer` bewusst mit **System.Drawing/GDI+** | Leitkonzept auf GDI+ umgestellt, ScottPlot als Option vermerkt |
| 2 | Tabelle `Berichtskonfiguration` „Anlage über `UpdateDatabaseFromScript` (TABELLEN/SPALTEN)" (8.4) | selbstanlegende DDL zur Laufzeit (`StelleKonfigTabelleSicher` u. a.); `UpdateDatabaseFromScript` kennt gar keine TABELLEN/SPALTEN-Abschnitte, sondern `SQL=`-Zeilenpräfixe + Registry-Connection | 8.4 korrigiert (auch die entsprechende Angabe in `CLAUDE.md` ist falsch) |
| 3 | Kennzahlenkatalog Kap. 5 in vollem Umfang | 25 Kennzahlen, es fehlen u. a. Wärmebedarf je Zweck, Nutzungsgrade Kessel/BHKW, Speicherverluste | als Befund B10 aufgenommen |
| 4 | Wirtschaftlichkeit: Szenario „Best/**Real**/Worst", Szenarien komplett W2 | DB-Werte `Worst/Erwartet/Best`; Kosten-Szenarien schon in W1, Preis-/Zins-Szenarien fehlen | 5.4/5.5 des Begleitkonzepts präzisiert |

## 3. Code-Verifikation (Kurzfassung)

**Berichtsmodul Phase 1–5: im Kern konzeptkonform umgesetzt.** Bestätigt: unbegrenzte
Variantenzahl, Blocksplitting zu 3 mit wiederholter Stamm-Spalte, Δ aus Rohwerten, alle 4
Ganglinientypen aus In-Memory-Simulation, ClosedXML-Excel (Übersicht/Vergleich/Detail),
DB-Konfigpersistenz (JSON), lesender Datensammler ohne Projektwechsel, Aktualitätsprüfung,
`VariantenCtrl`/`Form_AlsVariante`, `carrier_id`-Nachrüstung, `SixLabors.Fonts` 1.0.1
gepinnt. Alt- und Neubericht **koexistieren** (Button „…(alt)"); Stilllegung nach Praxistest
steht aus.

**Wirtschaftlichkeit W1 (Phase 6): Rechenkern numerisch bestätigt.** Unabhängige
Nachrechnung: Annuität inkl. i=0-Grenzfall, Kapitalwertformel, Restwert linear (letzte
Ersatzgeneration), Ersatzbeschaffung, dynamische Amortisation mit Interpolation — alles
korrekt (u. a. 17 150 € · a(0,35 %; 13,33 a) = 1 319,07 €/a wie im Konzept). Differenzrechnung
gegen den Stamm ist mathematisch sauber; SQL durchgängig parametrisiert; Reiter, Word und
Excel lesen dieselben persistierten Ergebnisse.

**Aber: Korrekturliste K1–K10 vor Produktivsetzung** (Details Begleitkonzept Kap. 8), die
wichtigsten vier:

1. **K1 — Zuschüsse:** eine negative Investitionsposition mit Nutzungsdauer < T wird in
   jedem Ersatzjahr erneut gutgeschrieben (−5 000 €, n=10, T=20, i=3 % → KW +8 720 € statt
   +5 000 €).
2. **K2 — Parameternachweis:** der je Lauf persistierte Parametersatz wird nie zurückgelesen;
   nach Parameteränderung ohne Neuberechnung weist der Bericht falsche Annahmen aus —
   verletzt die Nachvollziehbarkeits-Anforderung der DIN EN 17463.
3. **K3 — Veraltete Simulationen** werden beim Berechnen nicht nachgerechnet (nur rot
   markiert), das Ergebnis gilt danach trotzdem als aktuell — entgegen Konzept Kap. 6 und
   `LIESMICH_Phase1`.
4. **K6 — BHKW-Einspeisung:** vergütet wird nur der PV-Überschuss; `Tab_ErgebnisBHKW` hat
   kein Stromüberschuss-Feld → BHKW-Varianten werden systematisch unterbewertet.

**Neue Querschnittsbefunde** (jetzt B8–B13 im Leitkonzept): doppelte Kopie der
Energie-Einstellungen beim Variantenanlegen; `Tab_Ergebnis*` wird beim Duplizieren
mitkopiert (frische Variante zeigt fremden Simulationsstand); Anhang weist verwendete
Emissionsfaktoren/Preise noch nicht aus; neue Formulare ohne `.resx`-Lokalisierung
(en-Bericht im Wirtschaftlichkeitsteil deutsch); Menü-/Button-Verdrahtung offen
(Designer-Handgriffe). Dazu Kostenmodul-Restbefunde (defekter `Form_KostenAdmin`-Insert,
`valid_from` mit Uhrzeit beim `energy_price`-Ersteintrag, `price_power` nie gelesen,
`KategorieID` über Reiter-Index).

## 4. Datenstruktur Kenndaten.accdb

**Einschränkung dieser Session:** Die Live-DB (`C:\ProgramData\EPOS_PLAN\`) liegt außerhalb
der verbundenen Ordner (Freigabedialog blieb unbeantwortet), und das Z:-Laufwerk lässt sich
über die Dateibrücke nur auflisten, nicht lesen (bekanntes Umlaut-Problem). Die DB-Prüfung
stützt sich daher auf (a) das **vollständig aus dem SQL im Code rekonstruierte Schema**
(≈25 Tabellen + 4 gespeicherte Abfragen — deckungsgleich mit den Konzeptangaben zu
`Tab_ProjektWerte`, `Tab_Ergebnis*`, `energy_*`, `Tab_Variante`, `Berichtskonfiguration`)
und (b) die DB-Nachverifikation der Parallel-Session (Chat-Upload-Kopie: Emissionsfaktoren
g/kWh bestätigt, `energy_carrier.co2` fast durchweg 0 → Faktor-Kette zwingend).

**An der echten DB noch zu klären** (aus Code-Widersprüchen):

- Semantik von `Tab_BHKW.Brennstoff`/`Tab_Heizkessel.Brennstoff` — FK auf
  `energy_carrier.id` oder auf `Tab_Brennstoff_Stamm.ID` (`id_brennstoff`)? Der Code
  verwendet **beide** Lesarten; davon hängt die Korrektheit der `carrier_id`-Befüllung ab.
- Existiert `energy_carrier.price_power`? (wird im UI-Objekt geführt, aber nie gelesen/geschrieben)
- Ist der Unique-Index `unq_price_date` bereits um `ID_Projekt` erweitert (Voraussetzung der
  Duplizier-Logik)? Hat `energy_price` tatsächlich `valid_to` (Leitkonzept Kap. 2) — im Code
  kommt nur `valid_from` vor?
- Heißen die Kosten-Katalogtabellen `Tab_KostenKomponente`/`Tab_KostenKategorie` (Leitkonzept
  Kap. 2) — im Code sichtbar sind nur `Tab_Kostenfaktor`, `Tab_KostenGruppenKatalog`,
  `Tab_Typ_Energieanlagen` und die IDs als Zahlencodes.

→ Sobald der Ordner `C:\ProgramData\EPOS_PLAN` freigegeben ist (oder die .accdb erneut als
Chat-Anhang kommt), lassen sich diese vier Punkte in wenigen Minuten schließen.

## 5. Gelieferte/aktualisierte Dateien

| Datei | Änderung |
|---|---|
| `Allgemein\Reporting\Konzept_Berichtserstellung_EPOS-Plan.md` | **Fassung 3.1**: Chart-Technik (GDI+), Tabellenanlage (selbstanlegend statt UpdateDatabaseFromScript), Befundliste B1–B7 mit Erledigt-Status + neue B8–B13, Phase-6-Zeile mit Prüfvermerk |
| `Allgemein\Reporting\Konzept_Wirtschaftlichkeit.md` | **Fassung 3.1**: Kap. 3/4 an Code-Stand angepasst (Befunde 1–3 behoben, Menge×Preis existiert, drei 3.1-Präzisierungen), neue Rechenkonventionen 5.1a, W1/W2-Abgrenzung 5.4, Szenarionamen 5.5, Umsetzungsstand Kap. 6, **neues Kap. 8** (Verifikation + K1–K10) |
| `Allgemein\Reporting\Pruefbericht_Konsolidierung_2026-08-11.md` | dieser Bericht |

**Empfohlene nächste Schritte:** K1–K4 beheben (Rechenkorrektheit/Nachweis), dann K5–K10 und
B8/B9; DB-Klärpunkte aus Kap. 4 abarbeiten; Menü-/Designer-Handgriffe; Validierung des
Zahlungsgerüsts gegen `goetz_test.XLS` und `VALERI_Vorlage_V7.xlsx`, sobald lesbar.
