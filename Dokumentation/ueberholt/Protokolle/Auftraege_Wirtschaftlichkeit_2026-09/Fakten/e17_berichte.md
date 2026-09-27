# E17 — Bericht Phase 1 (Opus, 24.09.2026, Worktree e17 ab 105cdb31)

Commits: 1ca8273b E17/1 Tabelle `Tab_ProjektWirkung`, Schemaschritt 126 (vorläufig; Nummer an zwei Stellen `ProjektWirkungSchema.SCHRITT`
und `SchemaStand.Zielversion`, Konstante `SCHRITT_126_NICHT_MONETAERE_WIRKUNGEN` zeigt darauf), Testdatenbank 124 → 126 (LFS 48e500c7,
67.801.088 B; 132 Tabellen STRICT, 210 Indizes; 0 Freitexte übernommen); 3f5f0412 E17/2 Kern (`NichtMonetaereWirkungen.Beurteilung` =
Dauer × stärkste Wirkung 0–9; `ProjektWirkungCtrl` lädt/ersetzt Liste in einem Vorgang; kein Rechenweg liest die Tabelle); c902018c E17/3
Dialog (Baustein `WirkungenListe` im Bewertungsblock statt Freitext: Wahlfelder, Beschreibung, Beurteilung „x von 9"/„nicht beurteilt",
Zeile hinzufügen/entfernen; Altfeld nur lesbar; KI-Sicht `wirkung_anzahl`, `wirkung_kategorie/_beschreibung/_dauer/_organisation/
_mitarbeiter/_umwelt`, `wirkung_beurteilung` lesend, `nicht_monetaer` entfällt; Maskenwache: WirkungenListe 6 Eingabestellen als Baustein
des Wirts WirtschaftlichkeitSeite, Seite 9 → 8; help_mapping `UcWirtschaftlichkeit.btn_Help_Wirkungen` → `Wirtschaftlichkeit#nicht-monetaer`
(Anker vorhanden)); 2512865e E17/3a Aktionsspalte mit Kopf; c5094dd4 E17/4 Word/Excel-Tabelle (Kategorie, Beschreibung, Dauer, drei
Wirkungsgrade, Beurteilung; Quelle `WirtschaftlichkeitBewertung.Wirkungen`; ohne Wirkung entfällt der Block), Anhang-E-Checkliste 2b/3b
erfüllt/teilweise/offen, Blattstruktur-Wache zwei Fälle; 0769c84d E17/5 Tests (Kern: Regel, Persistenz, CHECK, Duplikat, Freitext-Übernahme,
Anker bitgleich, Werkzeug-Wache; bUnit). Schema: FK auf Tab_Projekt (Löschen kaskadiert); Migration übernimmt gepflegten Freitext als
SONSTIG ohne Beurteilung (nur nicht leer, nur Projekte ohne Wirkung, wiederholbar); Migration/Werkzeug/Vorrichtung aus `ProjektWirkungSchema`;
Duplizieren/Projekttransfer nehmen die Tabelle am Schema mit. 40 Schlüssel neu (`WIRT_NM_*`, `WIRT_AE_NM_ERFUELLT`, `KI_DLG_WSE_WIRKUNG_*`),
gefasst `WIRT_AE_NM_TEILWEISE`, `WIRT_AE_2B_STELLE`, `KI_DLG_WSE_WIRKUNG_ERL`. Build 0, SQL-Prüfer 1.809/0, Designer wiederholbar.

Abweichungen/Befunde: Kern-Datenklassen direkt in der Oberfläche (Regel EPOS.UI/CLAUDE.md; Vorbild E8b‑Q4 → E17‑Q4); Freitext zählt nicht
mehr für „benannt"/Checkliste; kein eigener KI-Befehl für Zeilen (über `wirkung_anzahl`); Infoknopf `MitAssistent="false"`; KI-Aktion
Parameter lesen meldet `nicht_monetaere_wirkungen` aus dem Altfeld; `WPAR_NICHT_MONETAER_HINWEIS` bleibt als Schlüssel; Designer-Nachtrag
ZPG_EINGABE_STOCHASTIK_EINHEITSTAGE; Werkzeug baut Sicht von Schritt 122 bei jedem Schreiblauf neu (Bestand); Papiere offen: LIESMICH
124 → 126 (48e500c7), Wiki-Anker `nicht-monetaer` (beschreibt Freitext) und `checkliste` („teilweise").

Fragen (gebaut a): E17‑Q1 Ablage Tabelle (b JSON); E17‑Q2 Skalen Dauer 1–3, Wirkung 0–3, Dauer × max (b Summe); E17‑Q3 Altfeld bleibt
lesbar, Migration übernimmt (b entfällt); **E17‑Q4 neu:** Kern-Datenklasse in der Oberfläche a wie E8b‑Q4 (gebaut), b eigenes UI-DTO.

Erledigt: Register V‑G11, Konzept § 2.11.2, Szenarienkonzept § 10/§ 11.1 (W5‑B‑12: Kategorie und Beurteilung fehlten). Logbuchsatz
(`wirtschaftlichkeit`): „Die nicht monetären Wirkungen der Wirtschaftlichkeit werden als Liste mit Kategorie und Beurteilung nach Dauer und
Wirkung (DIN EN 17463) erfasst und erscheinen im Word- und Excel-Bericht als Tabelle." Abnahme A‑E17‑1: acht Schritte (Liste im
Bewertungsblock, Speichern/Kopf, Altfeld nach Migration, Word/Excel-Tabelle, Checkliste 2b/3b, Kapitalwerte unverändert, Englisch, Assistent).
