# Paket B0 — Bestandsfehler: Umsetzungsprotokoll

**Stand:** 14.08.2026 · Bezug: `Konzept_Simulation_QuellenSenken.md` Fassung 12, Kapitel 8
**Build:** grün (Debug/x86, VS-MSBuild — siehe Hinweis unten)
**Zurückgestellt:** Alles mit Schemaänderung/Migration — insbesondere **B0-6b**
(Beziehung `Tab_Projekt → Tab_Pufferspeicher`), die Nachrüstung der
`ID_PUFFER`-Beziehung und jede Altdaten-Bereinigung (ADR-001-Aufgaben 2–6).

Das Konzept verlangt je Fix eine dokumentierte Ergebniswirkung (E9). Da noch keine
Referenzlauf-Suite existiert (Paket B1), ist die Wirkung hier **qualitativ** je Fix
beschrieben: *wo* sich Ergebnisse ändern und *warum* — als Grundlage für die
Referenzläufe, die vor den weiteren Paketen einzufrieren sind.

## Die elf Fixes

| # | Datei(en) | Fix | Ergebniswirkung |
|---|---|---|---|
| **B0-1** | `Z_ProjektPufferSpCtrl.cs` (Insert + ReadAll), `Z_ProjektPufferSpModel.cs`, `Form_Simulation_Config.cs` (btn_Speichern) | `Schwelle_Ein`/`Schwelle_Aus` als `double?` ins Model, in INSERT und ReadAll aufgenommen; beim Speichern werden die alten Schwellen vor dem Delete gesichert (Schlüssel Erzeuger+Puffer) und wieder mitgegeben. **Review-Nacharbeiten:** Fehlen die Schwellen-Spalten (still fehlgeschlagenes ALTER, z. B. gesperrte DB), fällt Insert auf die 7-Spalten-Variante zurück, statt nach dem Delete sämtliche Zuordnungen zu verlieren; Insert-Fehlschläge werden gezählt und als Statusmeldung angezeigt | **Nur Projekte mit Puffer und manuell gesetzten Schwellen ≠ 10/95:** Bisher fielen die Schwellen bei jedem Speichern der Konfiguration still auf 10/95 % zurück — die Simulation rechnete mit den Defaults. Künftig bleiben gesetzte Werte erhalten. Bereits zurückgesetzte Altwerte sind verloren und müssen einmal neu eingegeben werden. Randfälle: exakte Dubletten (gleicher Erzeuger **und** Puffer) teilen sich einen Schwellensatz; der Wechsel des Puffers einer Zeile gilt als neue Zuordnung (Schwellen → Default) |
| **B0-2** | `SimulationSPK.cs:101, :167` | `Restwaerme = (float[])Waermebedarf.Clone()` statt Referenzbindung bei 0 Kesseln; **zusätzlich (Review-Fund):** `Stromverbrauch_stuendlich` wird bei Brennstoffart 13 geklont statt referenziert — gleiche Aliasing-Klasse | **Nur zweiter und weitere Läufe derselben Sitzung:** Bisher konnte `Init()` über das Alias den Projekt-Wärmebedarf auf 0 löschen (Projekte ohne Kessel) bzw. blieb der Strom-Vektor dauerhaft an die Kessel-Ganglinie gebunden (Elektro-Kessel). Erste Läufe: bitidentisch |
| **B0-3** | `SimulationSPK.cs:106` | Kessel-SELECT um `AND ID_Projekt=` ergänzt (+ Apostroph-Escaping). **Review-Nacharbeit:** 0-Treffer-Absicherung — fehlt der Kessel im Projekt, bricht die Kessel-Simulation mit klarer Meldung ab statt mit `ArgumentOutOfRangeException` | **Nur Projekte, deren Kesselname auch in einem anderen Projekt vorkommt:** Leistung, Wirkungsgrade, Brennstoff und Emissionen kommen jetzt sicher aus dem eigenen Projekt statt vom ersten Namenstreffer. Ist der Kessel im Projekt gar nicht (mehr) hinterlegt, lieferte der Lauf bisher falsche Fremddaten — jetzt eine Abbruchmeldung. Eindeutige Namen: identisch |
| **B0-4** | `WErzeugerCtrl.cs:24/:81/:191` | `Ruecklauf` → `Rücklauf` in UPDATE/INSERT/ReadSingle (Spaltenname mit Umlaut, an der DB verifiziert) | **Keine.** Die drei Methoden werden für Energieanlagen nicht aufgerufen — Beseitigung einer stillen Fehlerquelle. `Z_ProjektPufferSp.Ruecklauf` (ohne Umlaut) ist unberührt |
| **B0-5** | `SimulationWaermepumpe.cs:528-548` | Ganglinie `+=` Modul-Beitrag statt `=`; `Heizstab_gesamt` addiert den Beitrag statt den Stundenstand | **Nur bei ≥2 WP-Modulen mit Heizstab in derselben Stunde:** `Heizstab_stuendlich` trägt jetzt die Summe aller Module statt nur des letzten — darüber steigen Viertelstunden-Strombedarf und Reststrom (`SimulationControl.cs:143-144`, `:211`) auf den korrekten Wert. `Heizstab_gesamt` und Ein-Modul-Anlagen: bitidentisch |
| **B0-6a** | `PufferSpCtrl.cs` (neu: `ProjektWaisenEntfernen`), `PufferSpKontextMenuCtrl.cs` (Löschen **und** Neu/Bearbeiten), `Form_Start.cs` | Zentrale Aufräum-Methode löscht Projektkopien in `Tab_Pufferspeicher` ohne zugehörige Puffer-Anlage — aufgerufen nach **allen drei** Löschpfaden (die Review fand zwei zusätzliche: Dialog Hinzufügen/Bearbeiten und Startseite); Zuordnungen räumt die FK-Löschweitergabe ab | **Keine Änderung bestehender Läufe.** Ab dem nächsten Lösch-/Speichervorgang rechnet ein gelöschter Puffer nicht mehr weiter; dabei werden auch **Alt-Waisen des Projekts** mit abgeräumt. Projektübergreifende Bereinigung beim Projektlöschen: Migration (B0-6b), zurückgestellt |
| **B0-7** | `SimulationRunner.cs` (Restbedarf + Deckung), `SimulationWaermepumpe.cs` (Init) | `Restwaermebedarf` aus der `waermerestbedarf_gesamt`-Ganglinie (identisch zur Detailansicht). `Waermebedarfsdeckung` restbedarfsbasiert als **Eigenanteil der WP-Stufe** `(Stufeneingang − Rest) / Gesamtbedarf`, Kappung 0–100 — **Review-Nacharbeit:** Bericht/Wirtschaftlichkeit addieren Erzeugeranteile zu 100 %, eine Differenz gegen den Gesamtbedarf hätte vorgelagerte Erzeuger doppelt gezählt. Zusätzlich setzt `Init()` jetzt `Waermebedarf_gesamt`/`waermerestbedarf_gesamt` zurück (kein veralteter Wert nach Berechnungsabbruch) | **Alle Projekte mit Pufferspeicher oder Heizstab:** Die gespeicherten Werte ändern sich. `Restwaermebedarf` stimmt erstmals mit der Detailansicht überein; die Deckung ebenfalls, sofern die WP an erster Kaskadenposition steht. Steht sie dahinter, zeigt die **Detailansicht** weiterhin die überzeichnete Gesamtdifferenz — bekannter Restpunkt, wird mit dem Kanalmodell (Paket 4) aufgelöst. **Betrifft nachgelagert Bericht und Wirtschaftlichkeit** — größte Ergebniswirkung des Pakets |
| **B0-8** | `Form_PufferSp.cs` (Löschbutton) | Explizite Ja/Nein-Bestätigung mit Klartext „wird aus dem Katalog (Stammdaten) gelöscht" vor `PufferSpStammCtrl.Delete` | **Keine Rechenwirkung.** Verhindert das versehentliche globale Löschen von Katalogdatensätzen aus dem Projektdialog |
| **B0-9** | `Form_KonfigPufferspeicher.cs` | Anzeigename → DB-Bezeichner-Mapping (`ErzeugerDbWert`) + **direkte parametrisierte SQL-Abfrage** statt der gespeicherten Access-Abfragen. **Review-Fund:** Deren Definitionen enden auf ein hartkodiertes `HAVING ID_Projekt=8` — die Vorbelegung war bisher in **jeder** Sprache und für **jedes** Projekt tot | **Keine Wirkung auf gespeicherte Ergebnisse** (reine Dialog-Vorbelegung). Sichtbare Änderung: Vor-/Rücklauf werden **erstmals überhaupt** vorbelegt, auch in deutscher Oberfläche. SQL-Injection-Anfälligkeit beseitigt. Vorbestehender Restpunkt: Der Handler hängt nur an der Puffer-ComboBox — ein Erzeugerwechsel allein aktualisiert nicht |
| **B0-10** | `Form_PufferSp.cs:183`, `Form_PufferSp_Admin.cs:59` | `szFilterVolumen` mit `Gesamtvolumen Like '%'` vorbelegt (Fallback „alle Volumina") | **Keine Rechenwirkung.** Beseitigt den Laufzeit-SQL-Syntaxfehler `… and␣␣order by …` bei unbekannten Filtertexten; Filterverhalten bei den bekannten Literalen unverändert |
| **B0-11** | `Form_Simulation_Config.cs` (btn_Speichern) | Rückwärts-Mapping matcht auch gegen `DbValue`; Mapping-Liste vor die Schleife gezogen | **Deutsche Oberfläche: unverändert** (DisplayName = DbValue trifft weiterhin zuerst). Verhindert, dass nach Sprachwechsel der lokalisierte Anzeigename als `Erzeuger` in `Z_ProjektPufferSp` landet und die Zuordnung still wirkungslos wird |

## Nachtrag 14.08.2026: B0-12 (aus den offenen Review-Funden)

| # | Datei(en) | Fix | Ergebniswirkung |
|---|---|---|---|
| **B0-12** | `SimulationSPK.cs` (Berechnung + Heizkessel_Simulation) | `Anzahl` wird nach dem Einlesen mit Hinweismeldung auf `MAX_SPK` (10) begrenzt; `Gasspitze_Kessel` von `double[5]` auf `MAX_SPK` dimensioniert (gleiche Größe wie alle übrigen Kessel-Arrays) | **Bis 5 Kessel: bitidentisch.** Ab dem 6. Kessel brach der Lauf bisher mit `IndexOutOfRangeException` in der Gasspitzenberechnung ab (kein Ergebnis) — jetzt rechnet er durch; ab dem 11. Kessel liefen alle Kessel-Arrays über — jetzt werden die ersten 10 gerechnet und eine Hinweismeldung angezeigt |

## Nachtrag 14.08.2026: B0-13 (aus der Paket-4-Review)

| # | Datei(en) | Fix | Ergebniswirkung |
|---|---|---|---|
| **B0-13** | `SimulationWaermepumpe.cs` (Modulschleife, Volllast-Zweig) | `WP_Laufzeit`/`Modul_WP_Laufzeit` zählen nur noch, wenn das Modul tatsächlich Wärme liefert (`result[PTHERM] > 0`) — dieselbe Absicherung, die der Teillast-Zweig bereits hatte | **Nur Stunden, in denen `PTHERM` trotz Bedarf auf 0 fällt** (leergefahrener Quellspeicher → Quellbegrenzungs-Faktor 0, oder Sperrzeit): Bisher zählte jede solche Stunde als volle Betriebsstunde — die Paket-4-Review fand im Mehrmodul-Quellspeicher-Szenario 6.691 Betriebsstunden bei 0 kWh Wärme. Betroffen sind ausschließlich die laufzeitbasierten Größen: `Vollbenutzungsstunden` (gespeichertes Ergebnis, Berichts-Kennzahl `eff.wp_vbh`, Projektvergleich, Detailansicht) und die Modul-`Betriebsstunden` (Paket-7-Persistenz) sinken auf die Stunden mit tatsächlicher Lieferung. Wärme, Strom und Restbedarf: bitidentisch (der Zweig addierte in solchen Stunden ohnehin 0). **Hinweis:** Der neue Quellen-/Senken-Pfad aus Paket 4 enthielt dieselbe unbedingte Zählung an zwei Stellen; seit dem Paket-4-Commit tragen beide Pfade den Guard |

**Verifikation B0-13 (14.08.2026):** A/B-Referenzlauf mit Baseline- und Fix-DLL auf
derselben migrierten DB-Kopie, alle neun Referenzprojekte. Die Baseline reproduziert
die eingefrorene Basis `2026-08-14_Paket7` exakt (GESAMT PASS, 2.260.923 Werte —
Quelldatenbank für diese Projekte unverändert). Der Fix ändert **ausschließlich** in
Projekt 1021 (Mehrmodul + Quellspeicher) die zwei laufzeitbasierten Skalare:
`WaermepumpeModul[0].Betriebsstunden` 6.692,41 → 4,41 und
`Waermepumpe.Vollbenutzungsstunden` 3.846,66 → 502,66; Modul 1, sämtliche Wärme-,
Strom- und Restgrößen sowie alle Ganglinien aller Projekte: unverändert (PASS).
Die Basis wurde im Zuge des Paket-4-Commits neu eingefroren (`2026-08-14_Paket4`,
Feature-Flag aus); die zwei 1021-Abweichungen sind dort im `lauf_protokoll.md`
als gewollt begründet.

## Verifikation

- **Build:** `WP-Plan.sln`, Debug/x86 — fehlerfrei (nur vorbestehende Warnungen).
  ⚠ `dotnet build` schlägt in dieser Umgebung mit `MSB4803 ResolveComReference`
  fehl (SDK-10-Preview verträgt keine COM-Referenzen) — gebaut wird über
  `MSBuild.exe` aus VS 2022 (`vswhere` → `MSBuild\Current\Bin\MSBuild.exe`).
  Der Buildbefehl in `CLAUDE.md` ist entsprechend zu ergänzen.
- **Kodierung:** Alle Dateien UTF-8 mit BOM erhalten, Diff ohne Ersatzzeichen
  (`git diff | grep U+FFFD` = 0 Treffer); die Umlaut-Schreibweise `Rücklauf` ist
  im Diff intakt.
- **Adversariale Review (abgeschlossen 14.08.2026):** Drei unabhängige Prüfagenten
  über den Working-Tree-Diff; einer davon hat gegen eine lesende Kopie der
  produktiven `Kenndaten.accdb` verifiziert (Schema, Abfragedefinitionen,
  Testselects). Urteil erste Runde: 6× korrekt, 2× Problem (B0-3, B0-7),
  3× unvollständig (B0-1, B0-6a, B0-9). **Alle fünf Befunde sind nachgearbeitet**
  (siehe Tabellenzeilen), Build danach erneut grün.
- **Referenzläufe (B1): erledigt (14.08.2026).** Acht Realprojekte eingefroren
  (`Referenzlaeufe/2026-08-14_B0/`, 183 CSVs; Selbstvergleich PASS über
  2.033.047 Werte, zweiter unabhängiger Lauf bitidentisch, Negativtest schlägt
  korrekt fehl). Werkzeug: `Referenzlauf/` (Konsolentool, arbeitet ausschließlich
  auf einer DB-Kopie), Bedienung in `Referenzlaeufe/LIESMICH.md`. Die Referenzen
  tragen den Stand **nach B0** — `Restwaermebedarf`/`Waermebedarfsdeckung` also
  bereits die korrigierten Werte. Hinweis: Fünf Projekte beantworten die
  Extrapolations-Rückfrage automatisiert mit „Ja" (dokumentiert im
  Laufprotokoll) — mit der `Extrapolation_erlaubt`-Einstellung aus Konzept 13.4
  wird das später zur regulären Vorab-Einstellung.

## Review-Funde außerhalb des Paketumfangs (offen, vorbestehend)

- ~~**`SimulationSPK`: fehlende Array-Grenzen.**~~ **Behoben als B0-12** (siehe
  Nachtrag oben).
- **`Form_Heizkessel.cs:445` / `Form_Heizkessel_Admin.cs:82`:** gleiche Filterlücke
  wie B0-10 (`szFilterLeistung` ohne Fallback); die BHKW-Formulare haben den
  Fallback bereits.
- **`MyResource/Resource.en-US.resx`:** Schlüssel `KONFIG_STROMSPEICHER` doppelt
  (einmal `ResXNullRef`, einmal String) — Risiko für die Satellitenassembly.
- **`Form_PufferSp`:** Katalog-Löschung per `WHERE Bezeichner=?` trifft bei
  Namensdubletten im Katalog mehrere Zeilen.
- **Konzept 13.7 (Abschnitt 4):** Wiedergabe der Erzeuger-Abfragen um das
  `HAVING ID_Projekt=8` ergänzt (Nachtrag eingearbeitet).

## Nachtrag 14.08.2026 — Alt-Spaltenname `ID_GanglinieDaten` (Ganglinien)

Gleiche Fehlerklasse wie die stillen SQL-Fehler B1-F1/B1-F2: Sechs Controller und eine
Simulationsabfrage verwendeten den Alt-Spaltennamen `ID_GanglinieDaten`, den es im
aktuellen Schema nicht mehr gibt (Kopftabellen `Tab_Waermebedarf`/`Tab_Stromganglinie`/
`Tab_Solarganglinie` führen `ID`, Datentabellen `ID_Ganglinie`; per Schemadump gegen die
DB-Arbeitskopie verifiziert, alle `ID`-Spalten AutoNumber).

- **UI-Pfade (anlegen/importieren/löschen):** `WaermebedarfCtrl` (MAX/INSERT),
  `WaermebedarfDatenCtrl` (DELETE/INSERT), `SolarganglinieCtrl` (MAX/INSERT),
  `SolarganglinieDatenCtrl` (DELETE/INSERT), `StromganglinieDatenCtrl` (DELETE) —
  Muster: `ID_GanglinieDaten` → `ID` (Kopftabelle) bzw. `ID_Ganglinie` (Datentabelle).
  Zusätzlich in allen drei Kopf-Controllern das tote ReadAll/ReadSingle-Mapping ersetzt:
  `m_ID_Ganglinie` wurde über einen Fallback auf nicht existierende Spalten gelesen und
  blieb still 0 — jetzt direkt aus `ID`.
- **Simulationspfad:** `SimulationStrombedarf.cs:89` sortierte über
  `Tab_StromganglinieDaten.ID_GanglinieDaten`; den Access-Fehler („Für mindestens einen
  erforderlichen Parameter …") schluckt `RecordSet.Open`, projektzugeordnete
  Stromganglinien gingen dadurch still mit 0 in den Strombedarf ein. Jetzt
  `ORDER BY Tab_StromganglinieDaten.ID`.

**Verifikation:** Schemadump und Testselects per 32-bit-PowerShell/ACE gegen die
DB-Arbeitskopie; Reflection-Testaufruf aller drei Gewerke gegen eine Wegwerf-Kopie
(anlegen → Daten schreiben → Daten löschen → Kopf löschen, 24 Checks OK); Referenzlauf
mit getauschter App-DLL über die acht B1-Projekte: sechs Projekte PASS (bitidentisch),
nur 1008 und 1011 — die einzigen mit zugeordneter Stromganglinie — zeigen die gewollte
Reparaturwirkung (der importierte Lastgang geht erstmals in
`strombedarf_viertelstunde`/`reststrom` ein). Lauf: `Referenzlaeufe/2026-08-14_GanglinienFix`.
Nach Übernahme ist die Referenz für 1008/1011 neu zu setzen.

## Auslieferungshinweis

Das Konzept sieht die B0-Fixes **einzeln ausgeliefert** vor (E9). Die Änderungen
liegen uncommittet im Working Tree; empfohlene Commit-Aufteilung:

1. B0-2 + B0-3 + B0-5 (Engine, ergebnisneutral für Standardfälle)
2. B0-7 (Engine, dokumentierte Ergebnisänderung — eigener Commit)
3. B0-1 + B0-11 (Speicherpfad Zuordnungen)
4. B0-6a + B0-8 (Puffer-Lebenszyklus UI)
5. B0-9 + B0-10 (Lokalisierungs-Vorgriff)
6. B0-4 (Aufräumarbeit)
