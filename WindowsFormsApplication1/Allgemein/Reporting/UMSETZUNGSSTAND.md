# Umsetzungsstand Berichtserstellung + Wirtschaftlichkeit

**Stand: 12.08.2026 (nach Phase 11)** · Bezug: `Konzept_Berichtserstellung_EPOS-Plan.md`,
`Konzept_Wirtschaftlichkeit.md` (Fassung 7). Diese Datei beschreibt nur den
**aktuellen** Stand; die Phasen-Historie steht in `Allgemein\Bericht\LIESMICH_Phase1.md`.

## 1. Was die App heute kann

### Berichtserstellung (Phasen 1–5)
- **Varianten-Verwaltung**: Vergleichsgruppe = Stammprojekt + Varianten
  (`Tab_Variante`, `VariantenCtrl`); Anlegen aus dem Stamm, Waisen-Prüfung.
- **Word-Vergleichsbericht** (OpenXML, Vorlage `Berichtsvorlage.docx` mit
  INEKON-CI, frei anpassbar): Deckblatt, Inhaltsverzeichnis, Projektbeschreibung
  inkl. Gebäude, Komponenten-Matrix + Kenndaten + Abweichungen je Variante
  (3 Stufen), Ergebnisse je Variante, Variantenvergleich mit Δ-Tabellen und
  Blocksplitting, Anhang (Simulationsstände, Methodik).
- **Diagramme** (GDI+, PNG): Deckungs-Kuchen, Vergleichs-Balken, 4 Ganglinientypen
  (Wärme-Jahresverlauf, Jahresdauerlinie, Strombilanz je Monat, Speicherverlauf);
  Daten aus frischer In-Memory-Simulation je Projekt (keine gemischten Stände).
- **Excel-Bericht** (ClosedXML): Kennzahlen-, Vergleichs- und Detailblätter.
- **Konfigurierbare Bausteine** je Bericht (`Berichtskonfiguration`, JSON in DB);
  zweisprachig (de/en) über `BerichtTexte`, Zahlenformate kulturabhängig.
- **Emissionen**: CO₂/SO₂/NOx je Energieträger über die Kette
  `energy_project_settings → Tab_Brennstoff_Stamm → energy_carrier`
  (Projektwert vor Stammdaten vor Fallback); Einheiten verifiziert
  (CO₂ g/kWh, SO₂/NOx mg/kWh).

### Wirtschaftlichkeit (Phasen 6–10)
- **W1 — Kapitalwertmethode DIN EN 17463**: KW, Annuität, Barwerte je
  Vergleichsgruppe; Stamm = Referenz; Ersatzbeschaffung (ganzjährig gerundet),
  Restwert linear; Szenarien Erwartung/Best/Worst aus `Tab_ProjektWerte`;
  fehlende Preise erscheinen als Begründung, nie als 0.
- **W2 — Erweiterungen**: Differenz-Kennzahlen (KW-Diff, dynamische Amortisation
  mit Interpolation, IRR per Bisektion), Sensitivitäten (Zins ±1 pp, Preisst.
  Energie ±1 pp, Invest ±10 %, Energiekosten ±10 %) + Novellen-Zeile
  „KWKG-Bonus entfällt", BEHG-CO₂-Abgabe (nur fossile Kategorien).
- **W3 — Tarifmodell + Emissionsbilanz**: HT/NT × Winter/Sommer-Strommatrix aus
  den Stundenreihen (Referenzjahr 2026), zweistufige Leistungspreis-Staffel,
  Tarifersatz nur bei vollständig gepflegten Preisen (sonst Flat + Hinweis);
  Emissionsbilanz gekoppelt/getrennt gegen wählbaren Referenz-Kraftwerkspark
  (`Tab_Kraftwerkspark`, vorbefüllt) inkl. CO₂-Vermeidung — nur bei aktuellem
  Rechenstand (`ErgebnisAktuell`).
- **KWKG 2025** (Konzept Kap. 8): degressive Vbh-Staffel in `Tab_KWKG_Staffel`
  (2020: 5 000 → ab 2030: 2 500; Kontingent 30 000 Vbh, pflegbar), Deckel-Override
  (0 = Staffel), Fristenlogik § 6 (Stichtag ≤ 31.12.2026, Realisierung bis
  31.12. des 4. Folgejahres), Guards > 500 kW und Heizöl-Neuanlagen,
  Negativpreis-Abschlag (kontingentschonend), Nachweiszeile in Reiter/Word/Excel.
- **Kategorisierter Parameterdialog** (Phase 10): „Allgemein" immer sichtbar
  (Zins, T, Preissteigerungen); Erzeuger-Gruppen nur, wenn der Typ in der
  Vergleichsgruppe vorkommt — „Photovoltaik" (Einspeisevergütung), „BHKW — KWKG
  2025", „Brennstoff — BEHG und Emissionsbilanz" (CO₂-Preis, Kraftwerkspark).
  Werte ausgeblendeter Gruppen bleiben beim Speichern erhalten.
- **Referenzkessel aus der DB** (Phase 11): Wirkungsgrad + Brennstoff der
  getrennten Referenz kommen aus dem größten Heizkessel des Stammprojekts
  (`Tab_Heizkessel`; Öl → Wirkungsgrad_Öl, sonst _Gas; Plausibilitätsband
  50–115 %, sonst gespeicherte Vorgabe) — nicht mehr im Dialog gepflegt,
  dort nur noch Info-Anzeige; Quelle wird im Bericht ausgewiesen.
- **Kapitalwert-Verlauf** (Phase 11): Dialog „Verlauf…" mit zwei
  Liniendiagrammen — Differenz zur Stamm-Referenz (Nulldurchgang = dynamische
  Amortisation) und absolute kumulierte Barwerte; **Zeitraum frei wählbar
  (2–60 a, auch > T** — Neuberechnung mit verändertem Horizont, gespeicherte
  Ergebnisse unverändert**)**, Szenario wählbar; im Word-Bericht als Diagramme,
  im Excel-Blatt als Jahresreihen-Block (mit Konsistenz-Gate bei fehlenden
  Stundenreihen). Reihen ohne Restwert; Restwert-Barwerte werden ausgewiesen.
- **Ausgabe**: Reiter im Wirtschaftlichkeits-Dialog, Word-Baustein, Excel-Blatt
  (Szenarioblöcke, Sensitivität, Strommatrix, Emissionsbilanz; Zeilen nur, wenn
  die Funktion aktiv ist).

### Kostenmodul-Befunde (behoben)
B4 Kostenfaktoren anleg-/löschbar · B5 `energy_price.leistungspreis` wird
befüllt · B6 Formular-Schließen ohne Speichern-Button verliert keine Werte und
belebt gelöschte Träger nicht wieder.

## 2. Datenbank (legt die App beim ersten Start selbst an)

`Berichtskonfiguration`, `Tab_Variante`, `Tab_ProjektWirtschaftlichkeit`,
`Tab_ErgebnisWirtschaftlichkeit`, `Tab_ErgebnisWirtSensitivitaet`,
`Tab_ProjektTarif`, `Tab_ErgebnisStromMatrix`, `Tab_Kraftwerkspark`
(vorbefüllt), `Tab_KWKG_Staffel` (vorbefüllt, 8 Zeilen) sowie zusätzliche
Spalten in Bestandstabellen (`carrier_id`, `Waermeproduktion`, KWKG-Spalten)
per additivem Schema-Upgrade — bestehende Daten bleiben unberührt.

## 3. Verifikation

Rechenkern numerisch verifiziert (Python-Nachbau, 40 + 31 Prüfungen bestanden,
Details in `PRUEFBERICHT_Rechenkern.md` inkl. 13-Punkte-Abnahme-Checkliste);
jede Phase mit Opus-Code-Review (Phase 11 mit zweitem Verifikationslauf),
alle Befunde behoben — darunter zwei erst in Phase 11 entdeckte
Compile-Fehler aus Altphasen (typografische Anführungszeichen in
`EmissionsBilanzRechner.cs` und `BausteineVergleich.cs`, beide korrigiert).
Der Visual-Studio-Build ist noch nicht gelaufen (in der Cloud-Umgebung kein
Compiler).

## 4. Offene Schritte (Nutzerseite)

1. **Build**: `dotnet restore` / VS-Build (x86); neue Pakete: ClosedXML 0.105.1,
   SixLabors.Fonts 1.0.1 (gepinnt). Compilerfehler einfach zurückmelden.
2. **Zwei Designer-Handgriffe** (Snippets im LIESMICH): Form_Start-Button
   „Wirtschaftlichkeit", MDI-Menüeinträge „Als Variante speichern…" /
   „Varianten/Bericht".
3. **Funktionstests** nach `PRUEFBERICHT_Rechenkern.md` Kap. 2 + 3 sowie
   Testschritte Phase 10 im LIESMICH.

## 5. Bewusst offener Scope

Preisszenarien aus der `energy_price`-Historie · positionsbezogene
Zins-Overrides je Gewerk · exakte Spotpreis-Stundenrechnung für den
Negativpreis-Abschlag (§ 7 Abs. 5 KWKG) · getrennte KWK-Strommengen aus der
Simulation statt min-Regel · Gegenprüfung an `VALERI_Vorlage_V7.xlsx`
(Datei bei Bedarf anhängen).
