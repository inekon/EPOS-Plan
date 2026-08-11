# LIESMICH — Konzeptdokumente Berichtserstellung & Wirtschaftlichkeit

Stand 11.08.2026 · Ordner `Allgemein\Reporting\` · konsolidiert auf **3 Dateien** (Cowork-Session)

## Welche Datei gilt wofür

| Datei | Rolle |
|---|---|
| **`Konzept_Berichtserstellung_EPOS-Plan.md`** | **Leitkonzept** (Fassung 3, konsolidiert): Variantenvergleichs-Bericht Word + Excel, Dialog `Form_Bericht`, Architektur `Allgemein/Bericht/`, Kennzahlenkatalog, Diagramme/Ganglinien, Code-/DB-Verifikation, Codebefunde, Phasenplan |
| **`Konzept_Wirtschaftlichkeit.md`** | Begleitkonzept Wirtschaftlichkeit (Fassung 2): Kapitalwertmethode nach DIN EN 17463 (ValERI), Analyse des Alt-Verfahrens (BHKW-Plan-Excel), Datenvertrag, DB-Zusätze, UI-Reiter „Wirtschaftlichkeit" |
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
