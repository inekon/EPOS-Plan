# E17 — Nicht monetarisierbare Wirkungen: Kategorie und Beurteilung nach DIN EN 17463, Schemaschritt 127 (Protokoll, 24.09.2026)

Statuszeile #479 in [`Status_iOS_Migration.md`](../../../aktuell/Status_iOS_Migration.md); Auftrag des Anwenders vom
24.09.2026 („V‑G11 … kleiner Dialog-und-Bericht-Auftrag ohne Rechenwirkung"). Konzept
[`Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md)
§ 2.11.2 (Lücke V‑G11, Checkliste V‑G12 Punkte 2b und 3b), § 2.11.4 (V‑E), § 2.11.6;
[Entscheidungsregister](../../../aktuell/Wirtschaftlichkeit_Kosten/Entscheidungsregister_Wirtschaftlichkeit_EPOS-Plan.md)
R‑V (V‑G11) und R‑E17 (neu); Szenarienkonzept
[`Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md`](../../../aktuell/Konzept_Wirtschaftlichkeit_Szenarien_VALERI.md)
§ 10 und § 11.1 (W5‑B‑12); Analysepapier
[`2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md`](../../../aktuell/Wirtschaftlichkeit_Kosten/2026-09-19_Analyse_Konzept_Umsetzung_Wirtschaftlichkeit.md)
§ 5 und § 6; Mockup `../../../aktuell/Mockups/Dialog_Formel_Zahlenprobe.html`, Zonen „Was ist angenommen?"
(Klappblock „Bewertung nach DIN EN 17463"), „ValERI-Bewertung — die fünf Blöcke", „Bericht und Ausgabe" und
Ressourcentafel der Kategorie 8. Vorgänger: [`E15_Risikomodul_Protokoll.md`](E15_Risikomodul_Protokoll.md). Zweig
`e17` von `105cdb31` (`origin` nach dem Push von #474), Opus 5.5 im Worktree `.claude/worktrees/e17`, zwei Phasen:
`1ca8273b` (E17/1), `3f5f0412` (E17/2), `c902018c` (E17/3), `2512865e` (E17/3a), `c5094dd4` (E17/4), `0769c84d`
(E17/5) in Phase 1; in Phase 2 die Zusammenführung `311780cd` mit `origin` = `c99c4c7a` (#478 E15 samt Papieren und
#482), `4ede85a5` (E17/6), `db909d37` (E17/7) und `079de7d7` (E17/8). Erster Merge `0462f92e` („Merge e17: Nicht
monetarisierbare Wirkungen - Kategorie und Beurteilung, Tab_ProjektWirkung, Schritt 127 (#479)") über `c99c4c7a`, der
Baum byte-gleich mit `079de7d7`. Nach dem Push von Dialog Design (#483, #485 mit Schemaschritt 126, `origin` =
`58502233`) der Nachzug `86ebd491` und `f545f86b` (E17/9); End-Merge `52614c33`.
Basis `2026-09-24_R14_Kaelteerzeuger`. **Schemaschritt 127, ohne Rechenwirkung** — kein Rechenweg liest die
Wirkungen, die Basis bleibt.

## Befund vor der Welle

Die Norm verlangt, die Wirkungen einer Maßnahme, die sich nicht in Euro fassen lassen, zu erfassen, zu
kategorisieren — Energiefluss, finanziell, sonstig (DIN EN 17463, 6.1) — und nach Dauer und Wirkung auf Organisation,
Mitarbeiter und Umwelt zu beurteilen (8.2). EPOS führte dafür seit W5‑B‑12 (Szenarienkonzept § 10, Migrationsschritt
72) ein Freitextfeld `Tab_ProjektWirtschaftlichkeit.Nicht_Monetaer`; die Lücke V‑G11 stand im Konzept (§ 2.11.2) auf
„Freitext umgesetzt — es fehlen Kategorie und Beurteilung", die Punkte 2b und 3b der Anhang-E-Checkliste standen mit
gepflegtem Text höchstens auf „teilweise".

## Gebaut

- **E17/1 — Schemaschritt 127** (in Phase 1 vorläufig 126; `SCHRITT_127_NICHT_MONETAERE_WIRKUNGEN`, Methode
  `Schritt_127_NichtMonetaereWirkungen`, Quelle `ProjektWirkungSchema` für Migration, Werkzeug
  `Werkzeuge/Testdatenbankschema` und Testvorrichtung): die Tabelle `Tab_ProjektWirkung`, **STRICT**, zehn Spalten —
  `ID` (Schlüssel), `ID_Projekt` mit Fremdschlüssel auf `Tab_Projekt` (ON DELETE/UPDATE CASCADE), `Sortierung`,
  `Kategorie` TEXT mit CHECK `ENERGIEFLUSS`/`FINANZIELL`/`SONSTIG`, `Beschreibung` TEXT, `Dauer` INTEGER 1–3,
  `Wirkung_Organisation`, `Wirkung_Mitarbeiter`, `Wirkung_Umwelt` INTEGER je 0–3; NULL heißt „nicht beurteilt" —,
  dazu der Index `idx_ProjektWirkung_Projekt`. Die **Übernahme des Freitexts** (E17‑Q3 a): ein gepflegter Text wird
  eine Wirkung SONSTIG, Sortierung 1, ohne Beurteilung — nur nicht leere Texte, nur Projekte ohne eigene Wirkung, nur
  bestehende Projekte; wiederholbar. Das Freitextfeld bleibt stehen (Altfeld). Die Zahl steht an zwei Stellen aus
  einer Quelle (`ProjektWirkungSchema.SCHRITT`, `SchemaStand.Zielversion`); Duplizieren und Projekttransfer erkennen
  die Tabelle an `ID_Projekt` und kopieren sie ohne Codeänderung. Testdatenbank in Phase 1 124 → 126 (LFS
  `48e500c7…`).
- **E17/2 — Kern:** `ProjektWirkung` (eine Zeile) und `NichtMonetaereWirkungen` mit der Beurteilungsregel 8.2 als
  **einer** Funktion: Dauer (1 kurz … 3 lang) × stärkste der drei Wirkungen (0 keine … 3 stark), 0 bis 9; ohne Dauer,
  ohne jeden Wirkungsgrad oder außerhalb der Skala `null` = nicht beurteilt (E17‑Q2 a). Dazu `Benannt`, `Beurteilt`,
  `Kurztext`, die Anzeigetexte der Kategorien, Dauer- und Wirkungsstufen, `Pruefen` und `IstLeer`.
  `ProjektWirkungCtrl` lädt je Projekt (Sortierung, ID) mit Ladefehler und ersetzt die Liste in **einem** Vorgang —
  leere Zeilen fallen weg, ein Prüfbefund bricht vor dem Schreiben ab, ein Speicherfehler ist benannt. Kein Rechenweg
  liest die Klassen.
- **E17/3, E17/3a — Dialog:** Im Bewertungsblock der Wirtschaftlichkeitsseite steht an der Stelle des Freitexts der
  Baustein `WirkungenListe`: je Wirkung Kategorie, Beschreibung, Dauer und die drei Wirkungsgrade als Wahlfelder (leer
  = nicht beurteilt), die Beurteilung als Anzeige („6 von 9" bzw. „nicht beurteilt", aus dem Kern), „Wirkung
  hinzufügen" und je Zeile „Wirkung entfernen" in einer Aktionsspalte mit dem Kopf „Entfernen" (Hausregel EPOS.UI).
  Geschrieben wird auf Zuruf über `WirkungSpeichern` (`ProjektWirkungCtrl.Speichern` am Stammprojekt über die Hülle);
  Lade- und Speichergründe gehen wie gehabt in die Statuszeile. Das Freitextfeld steht, wenn gepflegt, als
  „Altfeld Freitext (nur lesbar, nicht mehr gepflegt)" darunter (E17‑Q3 a). Ausweis im Kopf, Zeile in Block 5 und
  die Deklaration „benannt" folgen der Liste. Infoknopf `UcWirtschaftlichkeit.btn_Help_Wirkungen` →
  `Wirtschaftlichkeit#nicht-monetaer` (`help_mapping`; der Anker besteht). **KI-Sicht:** `nicht_monetaer` entfällt,
  an seine Stelle treten `wirkung_anzahl` (legt Zeilen an bzw. nimmt sie vom Ende) und je Zeile
  `wirkung_kategorie`, `wirkung_beschreibung`, `wirkung_dauer`, `wirkung_organisation`, `wirkung_mitarbeiter`,
  `wirkung_umwelt` sowie `wirkung_beurteilung` (nur lesen); Zeilenkennzeichen „Wirkung n: …". **Maskenwache:**
  `WirkungenListe` als Baustein des Wirts `WirtschaftlichkeitSeite` mit 6 Eingabestellen, die Seite 9 → 8.
- **E17/4 — Bericht:** Wort- und Tabellenbericht zeigen in dem Abschnitt, der den Freitext trug (nach der
  Szenarienübersicht bzw. unter dem Vorschlag), die Tabelle der Wirkungen: Kategorie, Beschreibung, Dauer, Wirkung auf
  Organisation, Mitarbeiter und Umwelt, Beurteilung (Word „6 von 9" bzw. „nicht beurteilt", Excel die Zahl als Wert,
  keine Formel), mit einem Hinweis zu Skalen und Regel; ohne benannte Wirkung entfällt der Block. Quelle ist
  `WirtschaftlichkeitBewertung.Wirkungen` (Stammprojekt, in `FuerBericht` gelesen). **Anhang-E-Checkliste:**
  `ChecklistenLage.NichtMonetaerBeurteilt`; die Punkte 2b und 3b stehen auf „erfüllt", sobald mindestens eine Wirkung
  beurteilt ist, auf „teilweise" bei nur beschriebenen, sonst auf „offen" — im Bericht und auf der Seite
  (`AnhangEChecklisteKnopf.Lage`); ohne Wirkungsliste an der Bewertung bleibt der Freitext die Quelle (Rückfall für
  Aufrufer ohne Lauf). Blattstruktur-Wache: zwei Fälle (Word-Abschnitt samt Tabelle, Excel-Tafel samt Checkliste
  2b/3b) an der Prüfgruppe 1040/1041/1042 mit zwei Wirkungen am Stamm.
- **E17/5 — Tests:** `NichtMonetaereWirkungenTests` (Kern) — die Beurteilungsregel als Tafel (stärkste statt Summe,
  nicht beurteilt ohne Dauer/Wirkung oder außerhalb der Skala), Kurztext, Benannt, Beurteilt, Prüfen, Texte de/en,
  Kategorien gleich dem CHECK der Tabelle; an der Testdatenbank Tabelle STRICT mit Fremdschlüssel und Index, Speichern
  und Laden (Reihenfolge, leere Zeilen, Ersetzen), Prüfbefund ohne Schreiben, CHECK, Projektduplikat trägt die
  Wirkungen, Übernahme des Freitexts (Leerraum nicht, wiederholbar, Altfeld bleibt), **Anker „keine Rechenwirkung"**
  (Kapitalwert, Differenz, Annuität und Amortisation bitgleich mit und ohne Wirkungen), Werkzeug-Wache der einen
  Quelle. `WirkungenListeTests` (bUnit): Leerzustand, Feldbestand und Köpfe, Wahl setzt Wert und Beurteilung,
  Hinzufügen/Entfernen, nur lesbar; der Assistent legt über `wirkung_anzahl` eine Zeile an und setzt Beschreibung,
  Dauer, Umwelt und Kategorie, die Beurteilung nur lesbar.
- **Phase 2:** Zusammenführung `311780cd` mit `c99c4c7a` (#477, #478 mit Schemaschritt 125 und Testdatenbank
  `6c4c32f9`, #480 bis #482). Der Schritt der Wirkungsliste wird **127**, weil 126 dem Dialog Design zugesagt ist:
  `ProjektWirkungSchema.SCHRITT` und `SchemaStand.Zielversion` 127, Konstante und Schrittfunktion umbenannt, die
  Migration überspringt die Lücke 126. Konflikte, beide Seiten zusammengeführt: Schemastand, Migration, Werkzeug und
  Nachziehliste der Testvorrichtung (125 vor 127); die Deklarationen in `WirtschaftlichkeitBewertung` und Hülle — E15
  reicht den Parametersatz für die Risikozeile, E17 den Kurztext der Wirkungsliste: `Deklarationen(Kurztext(Wirkungen),
  p)`; beide resx mit beiden Anhängen, keine Dublette, Designer neu (9.998). Ohne Konflikt: `help_mapping`,
  KI-Dialoge, Maskenwache (Parameterdialog 34, Seite 8, `WirkungenListe` 6), Bericht, Formelmappe, Blattstruktur-Wache,
  Checkliste. **E17/6** (`4ede85a5`): Testdatenbank aus der Fassung 125 auf **127** gezogen — `Tab_ProjektWirkung`
  samt Index, 0 Freitexte übernommen; 132 Tabellen, alle STRICT, 14 Sichten, 210 Indizes; `integrity_check` ok,
  `foreign_key_check` leer, ein zweiter Lauf meldet den Schritt als stehend; 67.801.088 Byte, LFS `c99a1eae…`.
  **E17/7** (`db909d37`): Die Probe „Altfeld bleibt stehen" liest die Spalte roh, der Leseweg `LadeParameter` trimmt.
  **E17/8** (`079de7d7`): Die Stilregeln der Wirkungsliste stehen vor dem Block FORMULARRASTER (`FormularrasterTests`),
  der Infoknopf der Liste trägt den Assistenten wie jeder andere (`InfoknopfSchluesselWacheTests`) — das hebt die
  Phase‑1-Abweichung `MitAssistent="false"` auf.
- **Nachzug auf #485:** `86ebd491` führt `origin` = `58502233` (#483 und #485, Schemaschritt 126 — die Reparatur der
  Gebäude-Katalogsätze) zusammen; Schritt 127 steht jetzt nach 126, die Lücke ist geschlossen. **E17/9**
  (`f545f86b`): Testdatenbank aus der Fassung 126 (#485, LFS `0fe67575…`, 67.788.800 Byte) auf **127** — nur
  `Tab_ProjektWirkung` samt Index neu, 0 Freitexte übernommen, Schritt 126 fand nichts offen; 132 Tabellen, alle
  STRICT, 14 Sichten, 210 Indizes; `integrity_check` ok, `foreign_key_check` leer; **67.796.992 Byte, LFS
  `87e49ed1…`**; ein zweiter Lauf meldet den Schritt als stehend.

## Schlüssel

Je Sprache 9.961 → 10.001 Einträge, **40 neu**: die Kategorien `WIRT_NM_KAT_ENERGIEFLUSS`, `WIRT_NM_KAT_FINANZIELL`,
`WIRT_NM_KAT_SONSTIG`; die Stufen `WIRT_NM_DAUER_1…3` (kurz, mittel, lang) und `WIRT_NM_WIRKUNG_0…3` (keine, gering,
mittel, stark); `WIRT_NM_NICHT_BEURTEILT`, `WIRT_NM_BEURTEILUNG_WERT` („{0} von {1}"); die Prüftexte
`WIRT_NM_FEHLER_KATEGORIE`, `WIRT_NM_FEHLER_BESCHREIBUNG`, `WIRT_NM_FEHLER_DAUER`, `WIRT_NM_FEHLER_WIRKUNG`,
`WIRT_NM_FEHLER_PROJEKT`; die Spaltenköpfe `WIRT_NM_SP_KATEGORIE`, `WIRT_NM_SP_BESCHREIBUNG`, `WIRT_NM_SP_DAUER`,
`WIRT_NM_SP_ORGANISATION`, `WIRT_NM_SP_MITARBEITER`, `WIRT_NM_SP_UMWELT`, `WIRT_NM_SP_BEURTEILUNG`,
`WIRT_NM_SP_ENTFERNEN`; `WIRT_NM_LEER`, `WIRT_NM_ZEILE_NEU`, `WIRT_NM_ZEILE_LOESCHEN`, `WIRT_NM_FELD`,
`WIRT_NM_KENNZEICHEN`, `WIRT_NM_LISTE_HINWEIS`, `WIRT_NM_ALTFELD`, `WIRT_NM_TABELLE_HINWEIS`; `WIRT_AE_NM_ERFUELLT`;
die Namen und Erläuterungen des Assistenten `KI_DLG_WSE_WIRKUNG_ANZAHL_NAME`, `KI_DLG_WSE_WIRKUNG_ANZAHL_ERL`,
`KI_DLG_WSE_WIRKUNG_KATEGORIE_ERL`, `KI_DLG_WSE_WIRKUNG_DAUER_ERL`, `KI_DLG_WSE_WIRKUNG_GRAD_ERL`,
`KI_DLG_WSE_WIRKUNG_BEURTEILUNG_ERL`. **Neu gefasst:** `WIRT_AE_NM_TEILWEISE` („beschrieben; noch keine Wirkung nach
Dauer und Wirkung beurteilt (8.2)."), `WIRT_AE_2B_STELLE` (die Tabelle bzw. Tafel) und `KI_DLG_WSE_WIRKUNG_ERL` (die
Beschreibung einer Wirkung). Der Schlüssel `WPAR_NICHT_MONETAER_HINWEIS` bleibt stehen, ohne Leser in der Seite.
Der Designer führt 9.998 Eigenschaften (9.958 vor E17), wiederholbar.

## Fragen aus der Welle

Gebaut ist jeweils Lesart a (die Empfehlung); offen beim Anwender (→ Register R‑E17).

| Frage | Lesarten | Empfehlung |
|---|---|---|
| **E17‑Q1** Ablage der Wirkungen | (a) eine eigene Tabelle `Tab_ProjektWirkung` je Projekt, eine Zeile je Wirkung, Beziehung über die ID; (b) eine JSON-Spalte an `Tab_ProjektWirtschaftlichkeit` | a |
| **E17‑Q2** Skalen und Regel der Beurteilung | (a) Dauer 1–3, Wirkung je Bereich 0–3, Beurteilung = Dauer × stärkste der drei Wirkungen (0 bis 9); (b) Dauer × Summe der drei Wirkungen | a |
| **E17‑Q3** Der Freitext `Nicht_Monetaer` | (a) bleibt als Altfeld lesbar, die Migration übernimmt einen gepflegten Text als Wirkung „sonstig" ohne Beurteilung; (b) entfällt | a |
| **E17‑Q4** Kern-Datenklassen in der Oberfläche (neu) | (a) so lassen — `WirkungenListe` und die Seite nehmen `ProjektWirkung` und `NichtMonetaereWirkungen` direkt, wie E8b‑Q4; (b) ein eigenes DTO der Oberfläche, die Hülle übersetzt | a |

## Abweichungen und Befunde

1. **Kern-Datenklassen in der Oberfläche:** Die Regel in `EPOS.UI/CLAUDE.md` sagt „keine Fachklassen des Kerns";
   `WirkungenListe` und `WirtschaftlichkeitSeite` nehmen `ProjektWirkung` und `NichtMonetaereWirkungen` direkt — eine
   Quelle für Regel und Texte, ohne Datenbank, wie bei E8b‑Q4. Als Frage E17‑Q4 gestellt.
2. **Der Freitext zählt nicht mehr:** Für „benannt", die Deklaration und die Punkte 2b/3b zählt allein die Liste;
   der Freitext ist nur noch Rückfall für Aufrufer ohne Lauf. Ein Projekt, dessen Text die Migration übernommen hat,
   steht auf „teilweise", bis eine Wirkung beurteilt ist.
3. **KI-Aktion „Parameter lesen" meldet das Altfeld:** Sie gibt `nicht_monetaere_wirkungen` weiter aus dem Freitext
   aus, nicht aus der Liste; einen eigenen KI-Befehl für Zeilen gibt es nicht — der Assistent arbeitet über
   `wirkung_anzahl` und die Zeilenfelder der Seite.
4. **Werkzeug baut die Sicht von Schritt 122 bei jedem Schreiblauf neu** (Bestand, nicht aus E17):
   `Werkzeuge/Testdatenbankschema` legt die Sicht aus Schritt 122 bei jedem schreibenden Lauf neu an.
5. **Designer-Nachtrag:** Der Designer-Lauf der Phase 1 trug den bis dahin fehlenden Schlüssel
   `ZPG_EINGABE_STOCHASTIK_EINHEITSTAGE` des Zapfprofilgenerators nach (Bestand aus Z4; nach dem Nachzug stand er
   schon auf `origin`).
6. **Lücke 126 bis zum Push des Dialog Design:** Bis zum Nachzug stand `SchemaStand.Zielversion` auf 127 ohne den
   Schritt 126; die Migration übersprang die Lücke (erster Merge `0462f92e`). Mit dem Nachzug `86ebd491` auf #485 ist
   126 da, die Testdatenbank folgt der Reihe 125 → 126 → 127 (E17/9); erledigt.
7. **Infoknopf:** In Phase 1 trug der Infoknopf der Liste `MitAssistent="false"`; E17/8 hebt das auf.

## Nachweis

- **Keine Rechenwirkung:** Kein Rechenweg liest die Tabelle; der Anker „keine Rechenwirkung" in
  `NichtMonetaereWirkungenTests` hält Kapitalwert, Differenz, Annuität und Amortisation mit und ohne Wirkungen
  bitgleich, `WirtschaftlichkeitAnkerTests` unverändert grün, kein Anker neu gesetzt; Referenzlauf 13/13 gegen
  `2026-09-24_R14_Kaelteerzeuger` PASS, 394/394 CSV byte-gleich, 4.207.049 Werte — die Basis bleibt.
- **Maskenwache:** `WirkungenListe` als Baustein des Wirts `WirtschaftlichkeitSeite` (6 Eingabestellen), die Seite
  9 → 8; der Parameterdialog bleibt bei 34.
- **Ressourcen:** je Sprache 10.001, keine Dublette, de/en deckungsgleich; Designer 9.998, wiederholbar.
- **Phase 1** (Worktree `e17`): Build 0 Fehler, SQL-Prüfer 1.809/0, Designer wiederholbar; kein `dotnet test`.
- **Phase 2** (auf `079de7d7`, derselbe Baum wie `0462f92e`): Builds 0 Fehler; gefiltert Kern 27/27 (Wirkungen), UI
  311/311; voller Lauf EPOS.Kern.Tests 6.072 / 0 Fehler, EPOS.UI.Tests 6.009 / 0, KiKern.Tests 549,
  SpeicherEngine.Tests 386, SpeicherPlanung.Tests 27 und 1 übersprungen; Referenzlauf 13/13 gegen R14 (oben);
  SQL-Prüfer 1.811/0; Designer wiederholbar (9.998).
- **Nach dem Nachzug** (auf `f545f86b`, derselbe Baum wie `52614c33`): gefiltert Kern 137/137, UI 357/357;
  Referenzlauf 13/13 gegen R14; SQL-Prüfer 1.821/0.
- **Gate auf `0462f92e`:** Kern-Filter 0 Fehler, ChartProben 151/151 gleich der Windows-Messlatte; der Testlauf abgebrochen, weil der Baum mit `079de7d7` (voller Lauf in Phase 2) übereinstimmt und das Gate auf dem End-Merge folgte (`GATE479.log`).
- **Gate auf `52614c33`:** Kern-Filter 0 Fehler, ChartProben 151/151 gleich der Windows-Messlatte, voller Lauf 13.050 bestanden / 0 Fehler / 1 übersprungen (EPOS.Kern 6.076, EPOS.UI 6.012, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 und 1 übersprungen), Dokumentationswachen 29/29 (`GATE479b.log`, 24.09.2026 20:29–20:34 Uhr).
- **CI:** steht aus (Beobachtung nach dem Push).

## Abnahme am Gerät (A‑E17‑1, Windows)

1. **Liste im Bewertungsblock:** Wirtschaftlichkeit öffnen, „Bewertung nach DIN EN 17463" aufklappen: An der Stelle
   des Freitexts steht die Liste „Nicht monetäre Wirkungen" mit Infoknopf; „Wirkung hinzufügen" legt eine Zeile an,
   Kategorie, Beschreibung, Dauer und die drei Wirkungen lassen sich wählen, die Beurteilung zeigt z. B. „6 von 9"
   (Dauer mittel, stärkste Wirkung stark) bzw. „nicht beurteilt".
2. **Speichern und Kopf:** „Speichern", Seite neu laden: Die Liste steht wie gepflegt; zugeklappt nennt der Kopf des
   Blocks die Beschreibungen, die Deklaration heißt „nicht monetäre Wirkungen benannt".
3. **Altfeld nach der Migration:** Ein Projekt mit gepflegtem Freitext (vor Schritt 127) öffnen: Der Text steht als
   Wirkung „Sonstig" ohne Beurteilung in der Liste und darunter als „Altfeld Freitext (nur lesbar, nicht mehr
   gepflegt)".
4. **Word- und Excel-Bericht:** Bericht erzeugen: Der Abschnitt „Nicht monetäre Wirkungen" trägt die Tabelle mit
   Kategorie, Beschreibung, Dauer, drei Wirkungen und Beurteilung (Word „x von 9", Excel die Zahl); ohne Wirkung
   entfällt er.
5. **Checkliste 2b/3b:** „Anhang-E-Checkliste…": mit einer beurteilten Wirkung „erfüllt", mit nur beschriebenen
   „teilweise", ohne Wirkung „offen" — gleich in Überlagerung, Abschlussseite und Blatt „Checkliste Anhang E".
6. **Kapitalwerte unverändert:** Rechnen mit und ohne Wirkungen: alle Kennzahlen gleich.
7. **Englisch:** Oberfläche auf Englisch: Köpfe, Wahltexte, Beurteilung und Hinweise englisch.
8. **Assistent:** Den Hilfe-Assistenten bitten, eine Wirkung „Versorgungssicherheit" mit Dauer lang anzulegen: Er
   setzt Zahl und Felder, die Beurteilung liest er nur; gespeichert wird erst mit „Speichern".

## Logbuch

Im Update-Papier (Version 1.2.0.4, Sammel-Upload 26.09.2026), Stichwort `wirtschaftlichkeit`: „Die nicht monetären
Wirkungen der Wirtschaftlichkeit werden als Liste mit Kategorie und Beurteilung nach Dauer und Wirkung (DIN EN 17463)
erfasst und erscheinen im Word- und Excel-Bericht als Tabelle."

## Papiere mit der Statuszeile

Register (Kopf, Familientafel mit R‑V und der neuen Familie R‑E17, R‑V mit dem Vermerk und der Zeile V‑G11 „gebaut
#479 (Schritt 127)", R‑E8b mit dem Verweis bei E8b‑Q4, R‑E17 mit E17‑Q1…Q4, EZ‑9, EZ‑10), Konzept (Kopf und
Schrittabsatz mit Schritt 127, § 2.11.2 Zuordnungstafel, V‑G11 und V‑G12 Punkte 2b/3b, § 2.11.4 V‑E und Fußnote,
§ 2.11.6 „Dauerhaft Werte bleiben", § 6.1, § 6.2, § 7 und Anhang), Szenarienkonzept (§ 7.3 G6, § 10.2, § 10.5 „Vom
Freitext zur Liste", § 11.1 W5‑B‑12), Analysepapier (Kopf, Nachtrag #479, § 5 Zeile E17 und Stand der Etappen, § 6
Schritt 127), Protokoll der Entscheidwege (Kopf, Kopf von § 8, § 8.31, § 8.32), `Referenzlaeufe/LIESMICH.md` (Nachtrag
Schemastand 127), Mockup (Klappblock „Bewertung nach DIN EN 17463" mit der Wirkungsliste, Block 5, Zone „Bericht und
Ausgabe" mit der Tabelle, Ressourcentafel der Kategorie 8, U43, Stand-Absatz), Update-Papier und die Wiki-Quelle
Wirtschaftlichkeit (Anker `nicht-monetaer`, `checkliste`, Word- und Excel-Bericht), Index (Reporting 130 → 131).

## Offen

- **Fragen E17‑Q1…Q4** beim Anwender (gebaut jeweils a).
- **Abnahme am Gerät** A‑E17‑1 (acht Schritte oben).
- **Gate auf `0462f92e`** und auf dem End-Merge `52614c33`, dazu **CI** (Nachtrag).
- Der **Wiki-Sammel-Upload** am 26.09.2026 (Version 1.2.0.4, freigegeben).
- Aus der Gap-Tafel des Konzepts bleibt **V‑G3** (Wiederholperiode je Kostenposition, E16, #484).
