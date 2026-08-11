# Berichtsmodul — Stand Phase 1 + 2 (11.08.2026)

Umsetzung nach `Allgemein\Reporting\Konzept_Berichtserstellung_EPOS-Plan.md`, Kap. 12.

## Phase 2 (neu): erster vollwertiger Word-Vergleichsbericht

| Datei | Inhalt |
|---|---|
| `Allgemein/Bericht/Vorlagen/Berichtsvorlage.docx` | **Rahmen-/Stylevorlage**: Styles Title, Subtitle, Heading1–3, Normal, Hinweis, Beschriftung; Kopfzeile mit INEKON-Logo, Fußzeile mit Datum- und Seitenfeldern. In Word frei anpassbar (CI ohne Codeänderung) |
| `Allgemein/Bericht/IBerichtsBaustein.cs` | Baustein-Schnittstelle (Konzept Kap. 8.3) |
| `Allgemein/Bericht/WordBerichtGenerator.cs` | OpenXML-Generator + `WordKontext` (Style-Absätze, Tabellenbau, Blocksplitting max. 3 Varianten je Block mit wiederholter Stamm-Spalte, Δ aus Rohwerten, TOC-Feld, UpdateFieldsOnOpen). Fällt die Vorlage aus, werden Ersatz-Styles programmatisch erzeugt |
| `Allgemein/Bericht/ProjektDetails.cs` | lesende Detail-Daten je Projekt (Klimaregion, Gebäude, Tab_Energieanlagen, Komponenten je Gewerk) — Spalten gegen Kenndaten.accdb verifiziert |
| `Allgemein/Bericht/AbweichungsErmittler.cs` | deklarative Feldliste (~50 Merkmale) für Kenndaten-Tabellen **und** Abweichungserkennung Variante↔Stamm (3 Stufen: Bestand, Komponente, Auslegung) |
| `Allgemein/Bericht/Bausteine/BausteineStandard.cs` | Deckblatt, Inhaltsverzeichnis (TOC), Anhang (Simulationsstände, Methodik, Hinweise) |
| `Allgemein/Bericht/Bausteine/BausteineProjekt.cs` | Projektbeschreibung (Stamm inkl. Gebäude), Komponenten & Varianten (Matrix, Kenndaten je Gewerk, Abweichungstabellen) |
| `Allgemein/Bericht/Bausteine/BausteineVergleich.cs` | Ergebnisse je Variante (Kern-Kennzahlen), Variantenvergleich (Gruppen-Tabellen, kompakte Δ%-Tabelle ab 2 Varianten, Erzeuger-Einzellisten, Brennstoffmengen) |

Geändert: `BerichtsDaten.cs` (+`Details`), `BerichtsDatenSammler.cs` (+Details,
+Abweichungen), `Controller/BerichtCtrl.cs` (+`ErzeugeWord` mit Dateinamen
`<Projekt>_Bericht_<JJJJ-MM-TT>.docx`, ohne stilles Überschreiben),
`Views/Bericht/Form_Bericht.cs` (Word-Erzeugung + „Bericht öffnen?"),
`WindowsFormsApplication1.csproj` (Vorlage wird nach `bin\…\Vorlagen\` kopiert).

**Testschritte Phase 2:** bauen (x86) → Varianten-Dialog → „Bericht erstellen…" →
Varianten anhaken → Erstellen. Ergebnis: .docx im Zielordner; beim Öffnen fragt
Word einmal nach der Feldaktualisierung (Inhaltsverzeichnis) — mit Ja bestätigen.
Prüfen: Deckblatt/Logo, Inhaltsverzeichnis, Projektbeschreibung mit Gebäuden,
Komponenten-Matrix + Kenndaten + Abweichungen je Variante, Vergleichstabellen
(bei > 3 Varianten Blocksplitting, bei genau 1 Variante Δ-Spalte), Anhang.

**Bewusst offen:** Diagramme (Kuchen/Balken/4 Ganglinien) → Phase 3
(`ChartRenderer`, In-Memory-Simulation); Excel → Phase 4; Emissions-/Kosten-
Kennzahlen, en-Lokalisierung, Menüpunkte → Phase 5; Wirtschaftlichkeit → Phase 6.
Der alte Direktbericht (`ProjektvergleichBericht`) bleibt bis Phase 3 erreichbar
(Button „…(alt)") und wird dann abgelöst.

---

## Phase 1 (Fundament, unverändert gültig)

Neue Dateien: `Controller/VariantenCtrl.cs` (Variantenlogik inkl. `AnlegenAusStamm`
für den Menüweg, Waisen-Prüfung), `Controller/BerichtCtrl.cs` (Konfig-Persistenz
in DB-Tabelle `Berichtskonfiguration`, JSON), `Allgemein/Bericht/BerichtsDaten.cs`
(DTOs), `BerichtsKonfiguration.cs` (Baustein-Katalog), `KennzahlenKatalog.cs`
(4 Gruppen), `BerichtsDatenSammler.cs` (lesender Sammler + Statusprüfung +
headless Simulation), `Views/Bericht/Form_Bericht.cs` (Dialog).

Geändert: `Form_Variantentest` (+`.Designer`) — Delegation an `VariantenCtrl`,
Button „Bericht erstellen…"; `ErgebnisCtrl` — Befunde B1 (`carrier_id` in beiden
Modultabellen inkl. Befüllung), B2 (`Delete(int)` funktionsfähig), B3
(Kesselmodul-`Waermeproduktion` persistiert); `ErgebnisModel` (+`CarrierId`).

Nach einer Simulation prüfen: `Tab_ErgebnisBHKWModul`/`Tab_ErgebnisHeizkesselModul`
haben befüllte `carrier_id`; Tabelle `Berichtskonfiguration` entsteht beim ersten
Öffnen des Berichtsdialogs.
