

===== Bericht 1 (2026-09-24T06:11:23.002Z) =====

Phase 1 von E10 ist fertig: acht Commits auf Zweig `e10` im Worktree `C:\Waermeplan\EPOS-Plan\.claude\worktrees\e10`, Stand sauber, nichts gepusht. Kern-Filter und Windows-Schale bauen mit 0 Fehlern, der SQL-Prüfer meldet 0 Fundstellen bei 1.767 Texten. Tests habe ich geschrieben, aber nicht laufen lassen; Referenzlauf und Basis kommen erst in Phase 2.

**Schemaschritt: ja, 120 ist angelegt** (`SchemaStand.Zielversion` 119 → 120). `origin` steht nach dem Fetch unverändert auf 48d8836d mit Zielversion 119, die 120 ist dort also frei. Warum ein Schritt statt einer Nachsaat beim Start: Die iOS-Schale migriert nicht, sondern nimmt die Seed-Kopie. So bleibt eine einzige Quelle für Migration, Werkzeug und Testdatenbank. Der Schritt füllt nur leere Zellen und ist wiederholbar. Die Testdatenbank steht auf 120 (LFS 6259b348… → 52c4729d…, 67.784.704 Byte, integrity ok).

## Commits

| Commit | Punkt |
|---|---|
| 62795858 | E10/1 Der Dialog „Nutzungsdauern (AfA)“ zeigt Instandsetzung und Wartung (acht Spalten, Neuzeile, Hülle, KI-Sicht; Maskenwache für den Dialog 8 → 12) |
| bdd4aba9 | E10/2 Schritt 120 sät die Sätze; Migration, Werkzeug, Nachzieh-Liste der Tests, Testdatenbank |
| 056ccd0d | E10/3 Vorbelegung: Vorlagenübernahme, Knopf „Sätze vorbelegen…“ auf der Betriebsseite, Herkunftszeile unter dem Satzfeld |
| 71489e2d | E10/4 Rechenweg: gepflegter Satz vor Tabelle vor 0; ein erfasster Betrag schlägt die Tabelle (I‑2); Herleitung und Formelmappe nennen die Herkunft; Nachweisfassung 9 → 10 |
| b5f9f249 | E10/5 Ein neuer Kesselkatalogeintrag in %/a ohne Betrag übernimmt den Wartungssatz der Tabelle; BHKW bleibt, die Asymmetrie (Item 19) ist dokumentiert |
| 375dc0fa | E10/6 Speicherflotte: Restwert je Einheit linear; ohne eigenes Intervall gelten 10 a aus der Tabelle; `RestwertEuro` der Einheit ist Altfeld (Editor und KI-Sicht gekennzeichnet) |
| 9d8999f3 | E10/7 A8: Kessel- und BHKW-Nutzungsdauer heißen „Nutzungsdauer (Gerätedaten)“, mit Tooltip, KI-Vermerk und Vermerk in der Parameterverwendung |
| 511a6647 | E10/8 Tests |

## Saat-Tafel
Gesät ist die Mitte des Empfehlungsbereichs aus den Betriebsvorlagen (`SchemaKatalog.Schritt39_Vorlagen`), nur an den Standardzeilen, nur Instandsetzung.

| Technik (Standardzeile) | Instandsetzung | Quelle: Vorlage, Position, Bereich |
|---|---|---|
| Heizkessel · Wärmeerzeuger (ID 1) | 2,0 % | Heizkessel, „Instandhaltung Heizkessel“, 1,5–2,5 |
| BHKW · Modul (ID 6) | 6,0 % | BHKW, „Instandhaltung BHKW“, 3–9 |
| Wärmezentrale · Rohrleitungen (ID 23) | 2,0 % | Wärmezentrale, „Instandhaltung Wärmezentrale“, 1,8–2,2 |
| Stromeinspeisung · Netzanschluss (ID 25) | 2,0 % | Stromeinspeisung, „Instandhaltung Stromeinspeisung“, 1,8–2,2 |
| Bauliche Anlagen (ID 26) | 1,25 % | Bauliche Anlagen, „Instandhaltung bauliche Anlagen“, 1,0–1,5 |

- Wärmepumpe, Photovoltaik, Solarthermie, Stromspeicher und Pufferspeicher bleiben leer: Ihre Vorlagen haben keinen Bereich.
- Wartung bleibt überall leer: Keine Vorlage führt sie in % der Investition.

## Stellen
- **Satzermittlung:**
  - Die Regel steht einmal in `NutzungsdauerSatzCtrl.WirksamerSatz`; die Zuordnung Position → Technik steht in `NutzungsdauerSaetze`.
  - Der Rechenweg ruft sie in `WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe` und `LiesBetriebskostenPositionen` (über `WirksamerBetriebssatz`). Die Kostenseite summiert über dieselbe Nachweisliste.
  - Vorbelegung und Anzeige: `KostenVorlagenUebernahmeCtrl.SatzOderTabelle` und `KostenKomponenteHuelle` (`SaetzeVorbelegen`, `Nachziehen`, `KopplungAnwenden`).
- **Flotte:**
  - Engine: `FlottenWirtschaftlichkeit.LinearerRestwert` und `Bewerte` (neues Ergebnisfeld `RestwertEuro`); `FlottenSimulator.PruefeEinheit` prüft das Altfeld nicht mehr.
  - Kern: `SpeicherFlottenStudieCtrl.ErsatzintervallVorgabeJahre` liest die Tabelle; `ErsatzintervallAufloesen` setzt den Wert nur in `Rechnen` ein.
  - Beschafft wird der Wert in `SpeicherAuslegungCtrl.QuellenBeschaffen`; der Kandidatenlauf bleibt ohne Datenbank.
  - Der Editor bekommt den Wert über `SpeicherOptimierungVorgaben`.

## Schlüssel
- **24 neu (de/en):** `ND_SP_INSTANDSETZUNG`, `ND_SP_WARTUNG`, `ND_SAETZE_HINWEIS`, acht `KI_DLG_NUD_*`, vier `ND_SAETZE_VORBELEGEN_*`, `ND_SATZ_HERLEITUNG`, `ND_SATZ_HERKUNFT`, `ND_SATZART_INSTANDSETZUNG`, `ND_SATZART_WARTUNG`, `FLOTTE_ED_ERSATZINTERVALL_VORGABE`/`_TABELLE`/`_OHNE_TABELLE`, `FLOTTE_ED_RESTWERT_ALTFELD`, `KBROW_ND_GERAETEDATEN_HINWEIS`.
- **6 bestehende geändert:** `FLOTTE_ED_RESTWERT_EINHEIT`, `KI_DLG_FLE_RESTWERT_ERL`, `KI_DLG_FLE_ERSATZINTERVALL_ERL`, `HZKK_LBL_NUTZUNGSDAUER`, `BHKWK_LBL_NUTZUNGSDAUER`, `KI_DLG_KBROW_NUTZUNGSDAUER_ERL`.
- resx je 9.024 → 9.048, Designer 9.006 → 9.030, neu erzeugt.

## Tests (geschrieben, gebaut, nicht gelaufen)
- **Kern, neu:**
  - `NutzungsdauerS3Tests`: Saat aus den Konstanten, Zuordnung, Schritt 120 samt Wiederholbarkeit, Werkzeug-Wache (Repo-Datei nur lesend, Werkzeug, Migration, Nachzieh-Liste), Speichern und Wiederherstellen, Hülle, Vorrang samt I‑2, Herkunftszeile, Vorlagenübernahme an Projekt 1006, Knopf „Sätze vorbelegen“ und Rechenweg an 1018 (Probe E7), Formelmappe, Kessel in %/a.
  - `SpeicherFlottenNutzungsdauerTests`: linearer Restwert je Einheit, Altfeld rechnet nicht, Rundung und Vorrang des Intervalls, Vorgabe aus der Batteriezeile, Projekt 1046 vorher/nachher.
  - `NutzungsdauerKennzeichnungTests`: Beschriftung, Vermerk, KI-Feldkarte, Parameterverwendung, de/en.
- **Kern, ergänzt:** ein Integrationsfall in `StromspeicherAuslegungCtrlTests` (der Studienlauf setzt 10 a ein).
- **Kern, angepasst:** `BetriebskostenBasisTests`. Die drei übrigen Zeilen „Instandhaltung …“ der Anlage 11327 bekommen im Beispiel den gepflegten Satz 0, sonst nähmen sie jetzt den Tabellensatz. Alle Zahlen dort bleiben gleich.
- **Engine:** `FlottenWirtschaftlichkeitTests` erwartet −390 statt −350, weil der feste Restwert 40 der Einheit nicht mehr rechnet.
- **UI (bunit):** Nutzungsdauer-Dialog, Kostendialog, VorlagenZeile, Flotten-Editor und `KatalogfelderVermerkTests`; der Editortest nimmt die neue Beschriftung.

**In Phase 2 erwarte ich rote Tests.** Leere Instandhaltungszeilen in Bestandsprojekten rechnen jetzt mit. Deshalb fallen voraussichtlich die 1030-Anker −21.895.377,28 € in `WirtschaftlichkeitAnkerTests`, `KwkgAnlagenartFehltTests` und `KwkgErsatzwegGewichtetTests`, dazu weitere Fälle an 1018, 1026 und 1030. Ich messe sie in Phase 2 und setze sie mit Begründung neu.

## Vorab-Zahlen
- **1018:** +5.097,66 €/a (45.312,50 € × 11,25 %).
- **1030:** +37.200 €/a. Das sind 17.700 € für Anlage 14920, noch einmal 17.700 € für Anlage 14921 und 1.800 € für den Kessel. Anlage 14921 hat keine eigene Investition; die Basis fällt deshalb nach H4a auf die ganze Komponente zurück, die Instandhaltung des BHKW steht so doppelt. Daneben stehen die Sammelposten „BHKW“ 18.000 €/a und „Heizkessel“ 2.000 €/a, auch hier droht Doppelzählung.
- **1026:** Nur der Kessel ist betroffen, seine Investition steht bei 0; die Basis messe ich in Phase 2.
- **1019:** keine Änderung.
- **1046 (Flotte):** Der Restwert steigt von 800 € auf 7.000 € nominal, der Kapitalwert um 3.432,79 €. Mit Zahlungsstrom 0 sind das −27.358,66 € gegenüber −30.791,45 €. Alternative b (fester Restwert hat Vorrang) ergäbe für 1046 keine Änderung.
- **R14:** Ich erwarte sie byte-gleich zu R13, weil die Basis keine Wirtschaftlichkeitsgrößen führt. Die Wirkung zeigen A/B-Tafel und Anker.

## Abweichungen vom Auftrag
1. **Zuordnung über den Positionsnamen:** Eine Kostenart „Instandsetzung/Wartung“ gibt es nicht, deshalb ordne ich über den Positionsschlüssel (`DbWerte.VDI_POS_*`) zu. Eine technikfremde Position wie „Instandhaltung Wärmezentrale“ in der BHKW-Vorlage nimmt den Satz der Technik aus ihrem Namen.
2. **Knopf:** Auf der Betriebsseite heißt derselbe Knopf „Sätze vorbelegen…“. Er nennt die Sätze nicht zusätzlich im Knopf „Nutzungsdauern vorbelegen…“ mit.
3. **I‑2:** Auch ein erfasster Betrag schlägt die Tabelle.
4. **Flotte:**
   - Der zusätzliche Restwert der Studie (Flottenebene) rechnet weiter; nur das Feld je Einheit ist Altfeld.
   - Das Tabellenintervall gilt nur im Studienlauf; der Projektlauf rechnet keine Flottenwirtschaftlichkeit.
   - Ein übernommener Stand speichert das eingesetzte Intervall als Zahl.
   - Ein Intervall 0 heißt nicht mehr „kein Ersatz“.
   - Einheiten ohne eigene Kosten: Ersatz kostet 0, Restwert gibt es nur bei einer Laufzeit unter der Nutzungsdauer.
   - Die Flotte ersetzt auch im letzten Jahr; der Restwert steht dann in voller Höhe daneben, netto wie bei den Positionen.
5. **A8:**
   - Die KI-Feldkarte trägt den Vermerk, bleibt aber setzbar wie der Katalog.
   - Der Tooltip läuft über einen neuen Parameter `Titel` an Zahlen-, Ganzzahl- und Textfeld.
   - Der zweite Halbsatz von A8 („Speichervariante liest 20/21“) ist nicht gebaut, weil `StromspeicherVarianteModel` laut Auftrag unverändert bleibt.

## Fragen (gebaut ist jeweils a)
- **E10‑Q1 Umfang S3:** a Tabellensätze rechnen mit, gepflegte Sätze haben Vorrang; b nur Vorbelegung. Empfehlung a. Vorher bitte die 1030-Befunde oben ansehen (H4a-Rückfall, Sammelposten).
- **E10‑Q2 Gerätekataloge:** a unverändert, nur neue Einträge vorbelegen; b Tabelle überschreibt die Katalogwerte. Empfehlung a.
- **E10‑Q3 Restwert der Flotte:** a linear, Altfeld gekennzeichnet; b fester Restwert hat Vorrang. Empfehlung a (1046: +3.432,79 € gegenüber ±0).
- **E10‑Q4 A8:** a nur kennzeichnen; b Schemaschritt entfernt die Spalten. Empfehlung a.
- **E10‑Q5 Basis:** a eine R14 nach der ganzen Welle; b je Teil eine. Empfehlung a (erwartet byte-gleich zu R13).
- **E10‑Q6 Asymmetrie Wartung:** a dokumentieren; b Kessel-Einheit auch fürs BHKW. Empfehlung a.
- **E10‑Q7, neu, vom Bau erzwungen – Saatwert aus dem Bereich:** a Mitte; b Untergrenze; c Obergrenze; d keine Saat, nur die Spanne zeigen. Empfehlung a.

## Erledigt-Gründe
- **A7:** Die Flotte hängt an der Tabelle (Intervall-Vorgabe, linearer Restwert); das Neueinfrieren folgt in Phase 2.
- **A8:** Kennzeichnung gebaut; der Halbsatz zur Speichervariante bleibt offen.
- **ND‑Q6:** Die Satzspalten werden gezeigt, gesät und gelesen.
- **ND‑Q7:** Nur neue Kesseleinträge in %/a werden vorbelegt.
- **Nr. 9h:** Beide Stücke sind erledigt. Das Flottenintervall gilt, wie der Auftrag es will, zur Rechenzeit und nicht nur als Vorgabe für neue Einträge.
- **U39:** Stück 1 ist für BHKW und Kessel erledigt, die Speichervariante bleibt, weil sie rechnet; Stück 2 ist erledigt.

## Logbuchsätze (Version beim Anwender erfragen)
- `nutzungsdauern`: „Der Dialog Nutzungsdauern (AfA) zeigt je Technik die Sätze für Instandsetzung und Wartung in % der Investition je Jahr (VDI 2067 Blatt 1, Tabelle A2).“
- `kosten`: „Betriebskostenpositionen ‚Instandhaltung …‘ und ‚Wartung …‘ mit ‚% der Investition‘ ohne eigenen Satz rechnen mit dem Satz der Nutzungsdauertabelle; die Kostenverwaltung zeigt die Herkunft und belegt die Sätze auf Knopfdruck vor.“
- `kosten`: „Die Speicherflotte rechnet den Restwert je Einheit linear aus ihrer Nutzungsdauer; ohne eigenes Ersatzintervall gilt die Nutzungsdauer der Nutzungsdauertabelle.“
- `nutzungsdauern`: „Die Nutzungsdauer in den Katalogen von Heizkessel und BHKW ist als Gerätedaten gekennzeichnet; gerechnet wird mit der Nutzungsdauertabelle.“

Phase 2 (Tests, A/B, R14, LIESMICH, Gate-Pfad) startet erst mit „Tests freigegeben“.

===== Bericht 2 (2026-09-24T06:37:34.600Z) =====

E10 ist fertig: der Umbau E10/9 ist eingebaut, der Merge gemacht, alle Tests grün und der Referenzlauf byte-gleich zu R13. Deshalb gibt es keine R14. Die Anker blieben unverändert, kein Anker-Test musste neu gesetzt werden. Gearbeitet habe ich nur im Worktree `C:\Waermeplan\EPOS-Plan\.claude\worktrees\e10` (Zweig `e10`). Kein Push, kein CI-Lauf, Arbeitsbaum sauber.

## Commits seit dem Phase-1-Bericht

| Commit | Inhalt |
|---|---|
| 2c41c642 | E10/9 Umbau: Die Tabellensätze wirken nur noch über die ausdrückliche Vorbelegung |
| f9eb2615 | Merge `origin/ios_migration_september` (502fea3c, #466). resx liefen ohne Konflikt zusammen, der Designer blieb gleich und ist wiederholbar erzeugbar |
| b8cb7287 | E10/10: Nachtrag in `Referenzlaeufe/LIESMICH.md` |

Davor liegen E10/1–E10/8 aus Phase 1. `origin` steht weiter auf 502fea3c mit Zielversion 119; Schritt 120 bleibt bei E10.

## E10/9 – was sich geändert hat
- **Rechenweg:** `LiesBetriebskostenTopfe` und die Beträge der Nachweisliste rechnen wieder genau wie vor E10: mit dem gepflegten Satz, sonst dem erfassten Betrag, sonst 0.
- **Herkunft:** Die Nachweisliste nennt „Satz aus Nutzungsdauertabelle“ nur noch als Ausweis, und zwar wenn der gepflegte Satz genau dem Tabellensatz entspricht. Herleitung und Formelmappe zeigen das; die Nachweisfassung 10 bleibt.
- **`WirksamerSatz`:** ist jetzt die Kernfunktion für Vorbelegung und Anzeige; der Betrags-Parameter ist entfallen.
- **Herkunftszeile im Dialog:** erscheint nur bei einem Satz gleich dem Tabellensatz. Ein leeres Feld rechnet mit nichts und bekommt keine Zeile.
- **Knopf „Sätze vorbelegen…“:** schreibt den Satz erst mit „Speichern“. Eine Zeile mit erfasstem Betrag, aber ohne Satz zählt als belegt und löst die Rückfrage aus.
- **Vorlagenübernahme:** Die vom Anwender ausgelöste Übernahme schreibt den Tabellensatz. **Abweichung:** Die automatische Pflichtanlage des Wizards (`PflichtpositionenSicherstellen`) schreibt keinen Satz. Sie läuft beim Speichern der Erzeuger ohne Zutun des Anwenders und wäre sonst derselbe ungewollte Eingriff.
- **Texte** de/en angepasst: `ND_SAETZE_HINWEIS`, `KI_DLG_NUD_INST_ERL`, `KI_DLG_NUD_WART_ERL`, `ND_SAETZE_VORBELEGEN_KEINE`, `ND_SAETZE_VORBELEGEN_FRAGE`.
- **Tests:** `NutzungsdauerS3Tests` angepasst: Die Tabelle rechnet nicht selbst; Vorbelegen plus Speichern rechnet; die Pflichtanlage schreibt keinen Satz; Herkunft nur bei Gleichheit. `BetriebskostenBasisTests` ist wieder auf dem Stand vor E10.

## Tests
- Gefilterte Läufe: Kern 229, UI 604 und SpeicherEngine 386 Fälle, alle grün.
- Voller Lauf `WP-Plan.Kern.slnf`: 12.580 bestanden, 0 Fehler, 1 übersprungen.

| Projekt | Bestanden |
|---|---|
| EPOS.Kern.Tests | 5.733 |
| EPOS.UI.Tests | 5.892 |
| KiKern.Tests | 542 |
| SpeicherEngine.Tests | 386 |
| SpeicherPlanung.Tests | 27 (+1 übersprungen) |

- `FlottenWirtschaftlichkeitTests` erwartet −390 statt −350; das ist die gewollte Folge von E10‑Q3 a.
- Vor jedem `dotnet test` habe ich in einem eigenen Aufruf nach `testhost` gesehen. Einmal lief ein fremder Prozess; nach 60 s Warten war er weg.

## A/B-Tafel Teil A – Knopf „Sätze vorbelegen…“ auf einer Arbeitskopie
Ohne Knopf ist alles unverändert: 1030 rechnet −31.141.242,71 €, genau der für #440 dokumentierte frische Wert.

| Projekt | vorbelegte Zeilen | Betriebskosten p. a. ohne → mit Knopf | Kapitalwert Erwartet ohne → mit Knopf (Δ) | Δ Günstig / Ungünstig |
|---|---|---|---|---|
| 1018 | 4 (BHKW 6 %, Wärmezentrale 2 %, bauliche Anlagen 1,25 %, Stromeinspeisung 2 % auf 45.312,50 €) | 0,00 → 5.097,66 € | nicht rechenbar* | – |
| 1030 | 3 (Kessel 2 % auf 90.000 €, BHKW 14920 6 % auf 295.000 €, BHKW 14921 6 % auf 295.000 €) | 20.000,00 → 57.200,00 € | −31.141.242,71 → −31.771.854,72 € (−630.612,02 €) | −572.367,27 / −687.883,88 € |
| 1026 | 1 (Kessel 2 % auf 6.775,50 €) | 0,00 → 135,51 € | nicht rechenbar* | – |

\* Bei 1018 und 1026 fehlen vorher wie nachher Energieträger-Zuordnungen, deshalb kein Kapitalwert.

**Datenbefunde für den Anwender (nicht behoben):**
- **1030 zählt doppelt.** Die Instandhaltung der Anlage 14921 fällt mangels eigener Investition nach der H4a-Stufung auf die Summe der ganzen BHKW-Komponente (295.000 €) zurück. Die Instandhaltung des BHKW steht damit zweimal da. Zusätzlich stehen die Sammelposten „BHKW“ 18.000 €/a und „Heizkessel“ 2.000 €/a neben den Einzelpositionen.
- **1026 rechnet auf fremder Basis.** Der Kessel hat die Investition 0; die Instandhaltung fällt deshalb auf die Projektsumme aus Solarthermie und Puffer (6.775,50 €) zurück.

## A/B-Tafel Teil B – Speicherflotte 1046
Studie mit den EPOS-Reihen des Projekts, 20 Jahre, 3 %, Investition 22.150 €, Ersatz in Jahr 10 und Jahr 20 je 7.000 €.

| Größe | vorher (fester Restwert, zugleich Alternative b) | nachher (linear, E10‑Q3 a) | Δ |
|---|---|---|---|
| Restwert nominal | 800,00 € | 7.000,00 € | +6.200,00 € |
| Kapitalwert | −38.487,24 € | −35.054,45 € | +3.432,79 € |

## Referenzlauf und LIESMICH
- `vergleich` gegen `2026-09-23_R13_Kuehlung`: 13/13 PASS, 4.145.687 Werte, 387 von 387 CSV byte-gleich (nur `protokoll.txt` weicht ab, wie erwartet).
- Der Gate-Pfad bleibt `Referenzlaeufe/2026-09-23_R13_Kuehlung`.
- Nachtrag in `Referenzlaeufe/LIESMICH.md`:
  - Kopf der Basis: „Nachträge 114 bis 120“.
  - Neuer Nachtrag zu den Schritten 116–118 (E9a, 18 leere Spalten, LFS `e9748b7f…`). Er fehlte bisher (Unklarheit 7).
  - Nachtrag „E10 (#463)“: Schritt 120 mit den fünf Sätzen und LFS `52c4729d…`, ergebnisneutral, weil ein Satz erst über die Vorbelegung rechnet; dazu der Flottenanschluss ohne Wirkung auf die Referenz; 13/13 byte-gleich, daher keine R14.
  - Dritte Einfrierregel um den Absatz „Anschluss an die Nutzungsdauertabelle“ ergänzt. Dazu gehören jetzt Ersatzintervall und Ersatzkosten der Einheiten von `@Projektflotte` sowie die Nutzungsdauer der Zeile Stromspeicher · Batterie. Den Nachweis führt `SpeicherFlottenNutzungsdauerTests`.

## Weitere Prüfungen
- SQL-Prüfer: 1.766 Texte, 0 Fundstellen.
- Designer wiederholbar (zweiter Lauf +0).
- Maskenwache und die Dokumentations- und Ordnungswachen grün (33 Fälle).
- Kern-Filter und Windows-Schale bauen jeweils mit 0 Fehlern.

## E10‑Q1 neu gefasst
- **a (gebaut):** Die Tabellensätze wirken nur, wenn sie ausdrücklich in die Position geschrieben werden: über die vom Anwender ausgelöste Vorlagenübernahme oder den Knopf „Sätze vorbelegen…“. Ohne dieses Zutun ändert sich keine gerechnete Wirtschaftlichkeit (ND‑Q4).
- **b (zuerst gebaut, verworfen):** Leere Positionen rechnen zur Rechenzeit automatisch mit dem Tabellensatz. Begründung für das Verwerfen: 1030 bekäme ohne Zutun +37.200 €/a und einen um 630.612 € schlechteren Kapitalwert, davon 17.700 €/a doppelt gezählt.
- Die Fragen E10‑Q2 bis E10‑Q7 bleiben wie im Phase-1-Bericht.
- E10‑Q5 hat sich erledigt: Die Basis ist byte-gleich, eine R14 entfällt.

**Logbuchsatz Kosten, angepasst:** „Betriebskostenpositionen ‚Instandhaltung …‘ und ‚Wartung …‘ mit ‚% der Investition‘ bekommen den Satz der Nutzungsdauertabelle über ‚Sätze vorbelegen…‘ oder die Übernahme einer Kostenvorlage; die Herkunft steht am Satz.“ Die übrigen Logbuchsätze bleiben wie im Phase-1-Bericht.

Das Harness-Protokoll der A/B-Tafeln liegt unter `C:\Users\Dirk\AppData\Local\Temp\claude\C--Waermeplan\b08323f3-360f-41d1-97bf-333e60c170b3\scratchpad\e10\ab\ab_e10_lauf_final.txt`.