# LIESMICH — Konzeptdokumente Berichtserstellung & Wirtschaftlichkeit

Stand 12.08.2026 · Ordner `Allgemein\Reporting\` · konsolidiert (Cowork-Sessions 11.–12.08.2026)

## Welche Datei gilt wofür

| Datei | Rolle |
|---|---|
| **`Konzept_Berichtserstellung_EPOS-Plan.md`** | **Leitkonzept** (Fassung 3.1, konsolidiert und code-geprüft): Variantenvergleichs-Bericht Word + Excel, Dialog `Form_Bericht`, Architektur `Allgemein/Bericht/`, Kennzahlenkatalog, Diagramme/Ganglinien, Verifikation + Befundstatus, Phasenplan mit Ist-Stand |
| **`Konzept_Wirtschaftlichkeit.md`** | Begleitkonzept Wirtschaftlichkeit (Fassung 2.1): Kapitalwertmethode nach DIN EN 17463 (ValERI), Analyse des Alt-Verfahrens (BHKW-Plan-Excel), **Datenvertrag (Kap. 5.8)**, DB-Zusätze, UI-Reiter „Wirtschaftlichkeit" |
| **`Pruefbericht_Berichtsmodul_2026-08-12.md`** | Unabhängige Prüfung (12.08.2026): Code der Phasen 1–5 gegen das Leitkonzept, Befundstatus B1–B7, neue Befunde N1–N27, Konsistenzabgleich der Konzepte, offene DB-Checkliste für `Kenndaten.accdb` |
| **`LIESMICH.md`** | dieser Index + Archivnotizen |

**Löschbare Alt-Dateien** (inhaltlich hier bzw. im Leitkonzept aufgegangen, nur
noch Einzeilen-Verweise; die Dateibrücke kann nicht löschen — bitte von Hand
entfernen): `Konzept_Variantenbericht.md`, `LIESMICH_Geruest.md`.
Ältere Vollstände (u. a. `Konzept_Berichterstellung.md` Fassung 2, Feldmapping,
`Konzept_Variantenbericht.md` Fassung 2) sind im Claude-Projekt
„EPOS-Plan Energieplanungssoftware" archiviert.

## Entscheidungslage (Kurzfassung, Runden 1–5, 10.–11.08.2026)

Unbegrenzt viele Varianten je Bericht · Kombinations-Architektur (neue Struktur
`Allgemein/Bericht/` unter Weiterverwendung des lauffähigen
`ProjektvergleichBericht`-Codes) · voller Berichtsumfang sofort (4 Kennzahlgruppen,
Balkendiagramme, 4 Ganglinientypen aus In-Memory-Simulation) · Word über OpenXML
mit neuer `Berichtsvorlage.docx` · Excel über ClosedXML (Übersicht + Vergleich +
Detailblatt je Variante) · Dialog `Form_Bericht` mit DB-gespeicherter
Berichtskonfiguration je Stamm · Berichtssprache = UI-Sprache (de/en) ·
Wirtschaftlichkeit per Kapitalwertmethode DIN EN 17463 mit eigenem UI-Reiter ·
Menüweg „Als Variante speichern…" für den Variantenbezeichner im Stammprojekt.

Details: Leitkonzept Kap. 1 (Entscheidungen) und Kap. 11 (Verifikation + Codebefunde).

## Umsetzungs- und Prüfstand (12.08.2026)

Phasen 1–5 sind implementiert (`Allgemein\Bericht\LIESMICH_Phase1.md`). Das
unabhängige Code-Review vom 12.08.2026 (Prüfbericht) bestätigt Struktur und
Konzepttreue, meldet aber kritische Restpunkte **N1–N5** (u. a.:
Spitzenkessel-Brennstoff fehlt in Kosten/CO₂; Preis 0 maskiert den
Katalogpreis) — die Kennzahlgruppen Emissionen/Kosten sind bis zu deren
Behebung nicht belastbar. Die unabhängige **DB-Nachprüfung an `Kenndaten.accdb`
steht aus** (Zugriff auf `C:\ProgramData\EPOS_PLAN` war in der Prüfsession
nicht erteilt) — Checkliste im Prüfbericht Kap. 6.

## Archivnotiz: Reporting-Gerüst (zurückgestellter PDF-Ausbaupfad)

`Reporting_Geruest.zip` enthält den Code-Kern des früheren Ansatzes
„format-neutrales Dokumentmodell mit drei Renderern (PDF/DOCX/XLSX)":
`Dokument/ReportDocument.cs` (Dokumentmodell + Fluent-API), `Werteformat.cs`
(zentrale Formatierung, `null` → „—"), `ReportContext.cs` (referenziert
`ErgebnisModel`), `IReportBaustein.cs` + Registry (Reflection),
`Bausteine/WaermepumpeBaustein.cs` (Beispielkapitel ~60 Zeilen),
`Renderer/IReportRenderer.cs`. Zum Kompilieren fehlten seinerzeit `Texte.cs`,
`ReportTheme` und der `ContextBuilder`. Tragende Entwurfsideen (Bausteine kennen
kein Ausgabeformat; Tabellen führen Rohwerte; `Sim_*`-Flags statt Nullwerte;
stabile Baustein-Schlüssel) sind in die konsolidierte Architektur des
Leitkonzepts eingeflossen (`IBerichtsBaustein`, `KennzahlenKatalog`,
Rohwert-Regel). Das Zip bleibt als Referenz liegen, falls der PDF-Weg später
reaktiviert wird — dann gilt wieder: erst Spike QuestPDF/MigraDoc auf x86.
