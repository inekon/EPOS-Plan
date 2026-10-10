# Protokoll KB-D — pflegbare Kältefolge, Schemaschritt 212

**10.10.2026 · Sitzung Gebäudesimulation · Zweig `gs-kbd`.** Grundlage:
[Entwurf Kältebereich](../../../aktuell/Gebaeudesimulation/2026-10-10_Entwurf_Kaeltebereich.md), Abschnitt 3.4 und Welle KB-D;
Anwenderentscheid E117 F1: Die Reihenfolge der Kälteerzeuger ist konfigurierbar, Vorgabe freie Kühlung, Kältespeicher,
Wärmepumpen, Kältemaschinen. Vorlauf: [Protokoll KB-A](2026-10-10_KB-A_Kaeltefolge_Projektkopie.md) (eine Quelle der Folge).
`SimulationKonfigSeite.razor` bleibt unberührt; die Knöpfe ergänzt eine Folgewelle nach dem Merge von KB-B.

## Ergebnis

1. **Speicherort: `Tab_Energieanlagen.Kaelte_Rang`** (INTEGER, `CHECK (Kaelte_Rang IS NULL OR Kaelte_Rang >= 1)`, NULL =
   Vorgabefolge), Schemaschritt 212 `KaelteRangSchema` (`KaelteKatalogfelderSchema.SCHRITT + 1`, reines DDL, wiederholbar).
   **Begründung:** Die Kältefolge ordnet Anlagenzeilen (`Kaeltekaskade.Erzeuger` je Anlage, Wärmepumpe wie Kältemaschine);
   der Rang lebt und stirbt mit der Zeile — Löschen braucht keinen Aufräumweg, Projekt duplizieren und die
   Fachspaltenübertragung (Assistent, Komponentenübernahme) tragen ihn mit, Waisen gibt es nicht. **Keine Projektspalte
   für die Stufenfolge:** Der Entwurf hält die Stufen fest (3.4: „Die freie Kühlung bleibt vorn“; die Kältespeicher ordnet
   ihre Entladepriorität und laden aus den Erzeugern, eine Stufe dahinter hätte keinen Rechenweg). Pflegbar ist, was der
   Entscheid und der Entwurf meinen: die Folge der Erzeuger, Wärmepumpe vor oder hinter Kältemaschine und die
   Kältemaschinen untereinander. Kein Eindeutigkeitsindex: Die Übertragung zwischen Projekten könnte sonst scheitern; die
   Eindeutigkeit hält der Schreibweg, ein gleicher Rang ordnet sich stabil nach der Vorgabe.
2. **Kern:** `Kaeltefolge.ErzeugerOrdnen` nimmt den Rang als vorderen Schlüssel — Erzeuger mit Rang vorn, aufsteigend, ohne
   Rang dahinter in der Vorgabefolge (Wärmepumpen nach Modulfolge, Kältemaschinen nach Anlagen-ID). Ohne jeden Rang ist
   die Folge Zeichen für Zeichen die bisherige. `Kaeltefolge.RaengeLesen` liest die Ränge (leer ohne Spalte, etwa auf einem
   älteren iOS-Stand). Lauf (`KaelteerzeugerVorbereiten`), Schema (`SchemaModell.KaelteBahnAnlegen`: je Anlage ein
   Bauschritt, über dieselbe Ordnung) und Leser (`KaeltefolgeStand.Gepflegt`, `KaelteerzeugerEintrag.KaelteRang`) ordnen
   darüber. Schreibweg `EPOS.Kern/Controller/KaeltefolgeCtrl.cs`: `FolgeSetzen` (Ränge 1…n in einem Vorgang, jede andere
   Anlage des Projekts ohne Rang), `Verschieben` (ein Platz nach vorn/hinten, schreibt die ganze Folge), `VorgabeSetzen`,
   `Pruefen` (Schema vorhanden, Projekt, jeder Kälteerzeuger genau einmal, keine andere Anlage — „kühlfähig“ heißt: in
   `Kaeltefolge.Lesen` geführt). Rückgabe `null` oder der Grund in der Oberflächensprache.
3. **Hülle/DTO:** `KaeltebereichDaten.FolgeGepflegt`, `FolgeHinweis`; je `KaelteerzeugerZeile` `KaelteRang`,
   `NachVornMoeglich`, `NachHintenMoeglich`. `SimulationKonfigDienste.KaelteVerschieben(idAnlage, richtung)` und
   `KaelteVorgabefolge()`, belegt in `SimulationKonfigHuelle` mit den Kernwegen. **Die Pfeile schreiben sofort** wie der
   Kühlbetrieb der Wärmepumpe im selben Bereich, nicht in den Arbeitsstand von „Konfiguration speichern“ (Abweichung von
   Entwurf 3.4): Der Kältebereich hat keinen Arbeitsstand, und ein Rang im Arbeitsstand müsste Laden und Anzeige doppelt
   führen. **KI-Sicht:** `SimulationKiSicht.Kaeltefolge` (lesbar) und `KaeltefolgeGepflegt` (nur „aus“ setzbar = Vorgabefolge),
   Feldkarte in `KiDialoge` (`kaeltefolge`, `kaeltefolge_gepflegt`). 14 Ressourcen je Sprache, `designer_neu.py schreiben`.
4. **Testdatenbank 211 → 212** mit `Werkzeuge/Testdatenbankschema`: nur `Tab_Energieanlagen` (Spalte) und
   `Tab_Applikation.SchemaVersion` geändert, kein Rang gesetzt; `quick_check` ok, `foreign_key_check` leer,
   SQL-Dialektprüfer 0 Fundstellen. **95 596 544 Byte, LFS-SHA-256
   `5b7a63e21a9a402df3ae6925e7a23fbcf4e405c197aa96f63aebf2fc5cbd91aa`.**

## Nachweis

- Kern-Filter Release 0 Fehler; Windows-Schale auf Linux 0 Fehler.
- Tests: `KaelteRangSchemaTests` (Nummer, Kette, Paketanhebung Ddl, Prüfklausel, Testkopie ohne Rang, Rundreise),
  `KaeltefolgePflegbarTests` (Ordnung stabil und Vorgabe gleich der bisherigen; Referenzprojekte ohne Rang; an 1017 mit zwei
  Kältemaschinen: gepflegte Folge Süd, Nord, Wärmepumpe → Lauf in dieser Folge, Süd deckt mehr, die Wärmepumpe weniger,
  Schema zeichnet dieselbe Kette, Vorgabe stellt das Ergebnis wieder her; Prüfregeln; Duplizieren nimmt den Rang mit,
  Löschen lässt die Folge stehen; Hülle mit Pfeilen, Hinweis, KI-Sicht), `KaeltefolgeTests`,
  `FachspaltenEinordnungWacheTests` (`Kaelte_Rang` als Fachspalte), Spaltenlagen in `FreieKuehlungSoleSchemaTests`,
  `ErdsondenfeldSchemaTests`, `AnlagenfahrplanSchemaTests` nachgezogen.
- **Referenzlauf** der Projekte 1017, 1047, 1055, 1058, 1059, 1061, 1062, 1063, 1064 gegen
  `Referenzlaeufe/2026-10-10_R51_FreieKuehlung`: 9 × PASS, **330 von 330 Dateien byte-gleich**. Keine neue Basis; eine
  Einfrierregel braucht es erst, wenn ein Referenzprojekt einen Rang bekommt.

## Offen für die Folgewelle (nach KB-B)

An der Seite je Erzeugerkachel zwei Pfeile (`KaelteVerschieben(z.IdAnlage, -1/+1)`, sichtbar nach `NachVornMoeglich`/
`NachHintenMoeglich`, nur mit Delegat), ein Knopf „Vorgabefolge“ (`KaelteVorgabefolge`, aktiv bei `FolgeGepflegt`), die
Herleitungszeile aus `FolgeHinweis`, nach jedem Schreiben `Laden` und ein zurückgegebener Grund als Meldung. Texte für
Pfeile und Knopf samt Tooltips legt die Folgewelle an (beide Sprachen); die Rollbereich- und Fensterproben laufen nach.
