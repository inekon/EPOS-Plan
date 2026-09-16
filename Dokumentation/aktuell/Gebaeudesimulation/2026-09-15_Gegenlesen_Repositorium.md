# Gegenlesen des Konzepts — Blickwinkel Repositorium und Hausregeln (15.09.2026)

**Protokoll.** Einzelbefund des Workflows ‚konzept-gegenlesen‘ (Modell Opus) im Auftrag des Konzepts [`Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md`](../Konzept_Gebaeudesimulation_VDI6007_EPOS-Plan.md), Sitzung vom 15.09.2026. Wortlaut wie geliefert; Zeilenbelege und Zahlen sind im Konzept nach dem Gegenlesen korrigiert, bei Widerspruch gilt das Konzept. Pfade unter `C:\Users\…pos-spike\` bezeichnen die Prüfwerkzeuge außerhalb des Repositoriums.

---

## Befundliste — Blickwinkel „Repository und Hausregeln"

**1. Namensumstellung ersetzt nicht die Sicht — die neuen Spalten erreichen den Leser gar nicht**
(a) Kap. 2.2: „Neue Spalten müssen hinter `ID` in die Sicht, oder der Leser wird auf Namen umgestellt (wie `GebaeudeCtrl.MapRowToModel`). Dieses Papier wählt die Umstellung (6.2)."
(b) Die beiden Wege sind keine Alternativen: `Abfrage_Projektgebaeude` ist eine Sicht mit **fester Spaltenliste**, also kommen neue `Tab_Gebaeude`-Spalten ohne Neubau der Sicht bei keinem Leser an — auch nicht beim Namensleser, dessen `dt.Columns.Contains(...)` dann schlicht `false` liefert.
(c) `sql/schema/002_views.sql:109` (`CREATE VIEW [Abfrage_Projektgebaeude] AS SELECT Z_ProjektGebaeude.ID_Projekt, … Tab_Gebaeude.ID FROM …`), gelesen über `EPOS.Kern/Controller/ProjektGebaeudeCtrl.cs:29` (`SELECT * FROM Abfrage_Projektgebaeude`); `GebaeudeCtrl.cs:60-70` zeigt den Namensleser. In `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs` gibt es für SQLite **kein** `DROP VIEW`/`CREATE VIEW`-Muster (nur ACE-Kommentare :1075, :1309).
(d) **hoch** — sachlich falsch; Schritt 77 liefe nach Papier durch und das Modell bekäme trotzdem keine Parameter.
(e) Ersatztext für 2.2/6.2: „`ProjektGebaeudeCtrl.ReadAll` liest die Sicht `Abfrage_Projektgebaeude` nach Spaltenindex `row[0]…row[57]` (`Tab_Gebaeude.ID` = Index 57). Schemaschritt 77 muss deshalb **beides** tun: (1) die Sicht `Abfrage_Projektgebaeude` in `sql/schema/002_views.sql` und im Migrationsschritt neu aufbauen (`DROP VIEW` + `CREATE VIEW`, neue Spalten hinter `Tab_Gebaeude.ID`) — SQLite kennt kein `ALTER VIEW`; (2) den Leser auf Namenszugriff umstellen (Muster `GebaeudeCtrl.MapRowToModel`), damit künftige Spalten die Zuordnung nicht mehr still verschieben."

**2. `Wohngebaeude_Nicht_Wohngebaeude` wird im Kern sehr wohl gelesen**
(a) Kap. 2.1: „…die Weiche ist allein `Typ == "Wohngebaeude  VDI 2067"` …, das Feld `Wohngebaeude_Nicht_Wohngebaeude` wird im Kern nirgends gelesen."
(b) Die Spalte ist der **Standardfilter des Gebäudekatalogs** im Kern und wird in drei Kern-Klassen gelesen — falsch ist nur, dass sie im *Rechenweg* wirkt.
(c) `EPOS.Kern/Controller/GebaeudeCtrl.cs:23` (`ReadAll(string szFilter = "Wohngebaeude_Nicht_Wohngebaeude='Wohngebaeude'")`), `GebaeudeStammCtrl.cs:83-84` (`FILTER_WOHNGEBAEUDE`/`FILTER_NICHT_WOHNGEBAEUDE`), `ProjektGebaeudeCtrl.cs:98`, `GebaeudeCtrl.cs:108`.
(d) **hoch** — sachlich falsch; die Aussage trägt in 2.2 und 7.6 die Vorstellung, das Feld sei tot.
(e) „…die Weiche des **Rechenwegs** ist allein `Typ == "Wohngebaeude  VDI 2067"` (zwei Leerzeichen, `SimulationWaermebedarf.cs:601-608`). Das Feld `Wohngebaeude_Nicht_Wohngebaeude` steuert dagegen nur die **Katalogauswahl** (`GebaeudeCtrl.cs:23`, `GebaeudeStammCtrl.cs:83-84`) und geht in keine Rechnung ein."

**3. „0 und 366 = aus" gilt nur für Ferienzeitraum 1 — 2 bis 4 haben keinen oberen Riegel**
(a) Kap. 3.3: „| `Ferienbeginn_n` / `Ferienende_n` | Tag des Jahres 1–365; 0 und 366 = aus | `SimulationWaermebedarf.cs:701-731` |"
(b) Nur Zeitraum 1 prüft `> 0 && <= 365`; die Zeiträume 2–4 prüfen allein `> 0`, und Zeitraum 1 indiziert ohne `−1` und läuft über den Jahreswechsel — ein „genau wie im Bestand" gebautes Modell erbt Versatz und fehlenden Riegel (bei `Ferienende_n = 366` greift die Schleife über `F_Absenkung[364]` hinaus).
(c) `EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:701-732`: `if (item.Ferienbeginn_1 > 0 && item.Ferienbeginn_1 <= 365)` + `for (int Tag = (int)item.Ferienbeginn_1; Tag < 365; …)` und `for (int Tag = 0; Tag < (int)item.Ferienende_1; …)` gegen `if (item.Ferienbeginn_2 > 0 && item.Ferienende_2 > 0)` + `for (int Tag = (int)item.Ferienbeginn_2 - 1; Tag < item.Ferienende_2; …)`.
(d) **hoch** — irreführend; 2.2 („Sollwertfahrplan unverändert nutzbar") und 4.4 („Sollwerte genau wie im Bestand") bauen darauf auf.
(e) „| `Ferienbeginn_n` / `Ferienende_n` | Tag des Jahres; **Zeitraum 1** ist der Jahreswechselblock (Beginn…365 **und** 0…Ende, ohne `−1`-Versatz) und nur er ist mit `≤ 365` geriegelt; **Zeiträume 2–4** laufen `Beginn−1 … Ende` und prüfen nur `> 0`. Das VDI-6007-Modell bildet den Fahrplan **nach Tagesindex 1…365 einheitlich** ab und lehnt Werte > 365 benannt ab (4.8) — es ist damit an dieser Stelle bewusst **nicht** zeichengleich zum Bestand. | `SimulationWaermebedarf.cs:701-732` |"

**4. `SummenvektorAusKanaelen` steht in einer anderen Datei als der Beleg nahelegt**
(a) Kap. 2.1: „(`Kanal.HEIZUNG/BRAUCHWASSER/PROZESS`, `SimulationKanaele.cs:426-472`); `Waermebedarf` ist die Kanalsumme (`SummenvektorAusKanaelen`, `:439`)."
(b) Der nachgestellte Kurzbeleg `:439` löst auf `SimulationKanaele.cs` auf; dort steht an Zeile 439 `public const int ANZAHL = 3;` — die Methode liegt in `SimulationWaermebedarf.cs:439`.
(c) `EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:439` (`private void SummenvektorAusKanaelen()`, Aufrufe :348 und :385) gegen `SimulationKanaele.cs:435-438`.
(d) **mittel** — Belegfehler.
(e) „…(`Kanal.HEIZUNG/BRAUCHWASSER/PROZESS`, `SimulationKanaele.cs:426-472`); `Waermebedarf` ist die Kanalsumme (`SimulationWaermebedarf.SummenvektorAusKanaelen`, `SimulationWaermebedarf.cs:439`)."

**5. Drei Kurzbelege in 2.3 zeigen auf `KlimaImportAblauf.cs`, gehören aber in `SolarPVGISCalculator.cs`**
(a) Kap. 2.3: „Daneben gibt es `Sonnengeometrie` (`:353-384`) und **Hay-Davies** (`CalculateHourlyHayDavies`, `:455-482`); Perez nicht."
(b) `KlimaImportAblauf.cs` hat nur 398 Zeilen; `:353-384` dort ist `Jahreszeitwert`, `:455-482` existiert nicht — beide Methoden (und `GetDailyAverages`, `:494-502`) liegen in `SolarPVGISCalculator.cs`.
(c) `EPOS.Kern/Allgemein/SolarPVGISCalculator.cs:353` (`private static Sonnenstand Sonnengeometrie(...)`), `:455` (`public static double CalculateHourlyHayDavies(...)`), `:484-506` (`GetDailyAverages`, `Sonnenwinkel = group.Max(...)` auf :502); `wc -l EPOS.Kern/Allgemein/Import/KlimaImportAblauf.cs` = 398.
(d) **mittel** — Belegfehler (in 3.3 ist derselbe Beleg mit richtiger Datei genannt, 2.3 widerspricht ihm).
(e) „Daneben gibt es `Sonnengeometrie` (`SolarPVGISCalculator.cs:353-388`) und **Hay-Davies** (`SolarPVGISCalculator.CalculateHourlyHayDavies`, `:455-482`); Perez nicht. `Tab_Klimadaten` (365 Tage) trägt 24-h-Mittel derselben Größen (`SolarPVGISCalculator.GetDailyAverages`, `:484-506`, Mittelwertblock `:494-502`; `Sonnenwinkel` als Tagesmaximum) …"

**6. Die Namensfalle ist im falschen Controller belegt**
(a) Kap. 2.1, Punkt 4: „…das Modellfeld `Fensterflaeche_Ost` (`EPOS.Kern/Model/ProjektGebaeudeModel.cs:26`, gefüllt in `GebaeudeCtrl.cs:66`); der Aufruf `SolareGewinneC(…, item.Fensterflaeche_Ost, …)` (`:826`) meint die Summenfläche."
(b) `GebaeudeCtrl.cs:66` füllt `GebaeudeModel`, nicht das im Lauf benutzte `ProjektGebaeudeModel`; dessen Feld kommt aus `ProjektGebaeudeCtrl.cs:56` (`row[14]`, Spalte 14 der Sicht = `Fensterflaeche_Ost_West`).
(c) `EPOS.Kern/Controller/GebaeudeCtrl.cs:66` (→ `GebaeudeModel`), `EPOS.Kern/Controller/ProjektGebaeudeCtrl.cs:56` (`item.Fensterflaeche_Ost = Convert.ToDouble(row[14])`), `sql/schema/002_views.sql:90` (Index 14 = `Tab_Gebaeude.Fensterflaeche_Ost_West`).
(d) **mittel** — Belegfehler; die Falle steckt an zwei Stellen, das Papier nennt die falsche.
(e) „…das Modellfeld `Fensterflaeche_Ost` (`ProjektGebaeudeModel.cs:26`, gefüllt aus Spaltenindex 14 der Sicht in `ProjektGebaeudeCtrl.cs:56`; dieselbe Verdrehung im Katalogweg `GebaeudeModel`/`GebaeudeCtrl.cs:66`); der Aufruf `SolareGewinneC(…, item.Fensterflaeche_Ost, …)` (`:826`) meint die Summenfläche."

**7. `Aussenbauteile_Strahlung` verletzt die Boolean-Regel aus BETRIEB_SQLITE.md**
(a) Kap. 6.1: „| `Aussenbauteile_Strahlung` | INTEGER `CHECK (… IN (0,1))` | θ_eq mit Absorption und Abstrahlung | 0 |"
(b) Die Hausregel schreibt für neue Boolean-Spalten `INTEGER NOT NULL DEFAULT 0 CHECK (…)` vor; „NULL bedeutet 0" ist damit unvereinbar, und die Regel „kein DDL-DEFAULT auf Fachwerten" trifft auf einen Schalter gar nicht zu (er ist kein Fachwert).
(c) `Dokumentation/aktuell/BETRIEB_SQLITE.md:300` („gibt ihr `INTEGER NOT NULL DEFAULT 0 CHECK (spalte IN (0,1))` — dann bleibt das so"); Bestandsmuster z. B. `sql/schema/001_grundschema.sql:113`, `:2879`.
(d) **mittel** — Hausregelverstoß im Vorschlag.
(e) „| `Aussenbauteile_Strahlung` | INTEGER `NOT NULL DEFAULT 0 CHECK ("Aussenbauteile_Strahlung" IN (0,1))` | θ_eq mit Absorption und Abstrahlung | — (Schalter, kein Fachwert: hier gilt die Boolean-Regel aus BETRIEB_SQLITE.md § 6, nicht die NULL-Vorgaberegel) |"

**8. „Wochenendtage folgen aus dem Wochentag des 1. Januar" nennt das Jahr nicht**
(a) Kap. 4.4: „…das Gebäudemodell rechnet in **einer** Zeitbasis (Ortszeit, wie PV und Solarthermie) und braucht `Tab_Klimadaten` nicht mehr; Wochenendtage folgen aus dem Wochentag des 1. Januar."
(b) Ohne benanntes Bezugsjahr ist die Wochenendmaske unbestimmt (sieben mögliche Ergebnisse) — das Repositorium hat dafür bereits einen Mechanismus, den das Papier nicht nennt.
(c) `EPOS.Kern/Controller/SolardatenCtrl.cs:222` (`public static int Referenzjahr(int idProjekt)`), genutzt in `ReadOrtszeit` `:195`, `:203`; heute steuert es nur die zwei Umstelltage (`SolarZeitbasis.cs`). Bestand liest die Wochenendmarke dagegen aus `Tab_Klimadaten.WE` (`KlimaImportAblauf.cs:354`).
(d) **mittel** — unbelegt/unklar; ergebnisrelevant und prüfbar.
(e) „…und braucht `Tab_Klimadaten` nicht mehr. Die Wochenendtage bildet das Modell aus dem Wochentag des 1. Januar des **Referenzjahres der Zeitbasis** (`SolardatenCtrl.Referenzjahr(idProjekt)`, heute schon die Quelle der Sommerzeit-Umstelltage) — damit stammen Zeitbasis und Wochenkalender aus **einer** Wurzel; ein Test hält die so gebildete Maske gegen `Tab_Klimadaten.WE` derselben Region."

**9. „die Einfrierregel" gibt es nicht — es gibt drei, und keine erfasst `Tab_Gebaeude`**
(a) Kap. 5.11: „…die Korrektur der Testdatenbank verändert das Referenzergebnis von Projekt 1008 und läuft über die Einfrierregel (Q22)." (ebenso 10.4: „Basis neu eingefroren und in `Referenzlaeufe/LIESMICH.md` begründet (Einfrierregel)")
(b) Die Basis kennt genau drei benannte Einfrierregeln — Emissionsfaktoren, PV-Modulkoeffizienten, Flottenstand 1046 —; gesäte **Gebäudedaten** sind in keiner davon enthalten, die Korrektur von `Bauweise` fällt also durch das Netz, statt es zu benutzen.
(c) `Referenzlaeufe/LIESMICH.md:57`, `:79`, `:100` (die drei Regelblöcke); `CLAUDE.md`, Abschnitt „Regressionsnetz", zählt dieselben drei auf.
(d) **mittel** — beruft sich auf eine Regel, die den Fall nicht deckt.
(e) 5.11/10.4/Q22: „…verändert das Referenzergebnis von Projekt 1008. Dafür gibt es heute **keine** Einfrierregel: die drei bestehenden (`Referenzlaeufe/LIESMICH.md:57/79/100`) decken Emissionsfaktoren, PV-Modulkoeffizienten und den Flottenstand 1046. G1 legt deshalb im selben Schritt eine **vierte Einfrierregel „gesäte Gebäudedaten"** an (`Tab_Gebaeude(_STAMM)`: `Bauweise`, U-Werte, Flächen, Sollwerte, `Luftwechselrate`, `Fensterdurchlassgrad`) und trägt sie samt Begründung in `Referenzlaeufe/LIESMICH.md` und in den Abschnitt „Regressionsnetz" der `CLAUDE.md` ein."

**10. „Zehn von zwölf" widerspricht der eigenen Tabelle 5.6**
(a) Kap. 5.6: „In zehn von zwölf Projekten liegt die Prototyp-Spitze auf demselben Index — die erste Stunde nach Ende der Nachtabsenkung…" (gleichlautend 4.5)
(b) Die Tabelle darüber weist nur **ein** Projekt mit abweichendem Index aus (1018, Index 438); alle übrigen elf stehen auf 1 398.
(c) Eigene Tabelle 5.6 gegen die Projektliste in 3.7 (1007/1046, 1008, 1017, 1023/1024, 1039, 1040/1041/1042/1045 = elf Projekte auf Index 1 398; 1018 = eines auf 438).
(d) **mittel** — Zahl gegen eigenen Beleg.
(e) „In elf von zwölf Projekten liegt die Prototyp-Spitze auf demselben Index (nur 1018 weicht ab, Index 438) — die erste Stunde nach Ende der Nachtabsenkung…" (gleiche Korrektur in 4.5).

**11. Der Beleg für „`WW_Bedarf` im Lauf ohne Wirkung" belegt etwas anderes**
(a) Kap. 3.3: „| `WW_Bedarf` | kWh/a, im Lauf ohne Wirkung | `AbweichungsErmittler.cs:128` |"
(b) Die genannte Zeile zeigt nur, dass `WW_Bedarf` ein **Vergleichsmerkmal des Variantenvergleichs** ist; eine Aussage über die Wirkungslosigkeit im Lauf enthält sie nicht (ein Negativbefund braucht eine Suche, keine Fundstelle).
(c) `EPOS.Kern/Allgemein/Bericht/AbweichungsErmittler.cs:128`: `new Merkmal("Gebäude", "Tab_Gebaeude", "WW_Bedarf", "Warmwasserbedarf", "kWh/a", 0),`.
(d) **mittel** — unbelegt.
(e) „| `WW_Bedarf` | kWh/a; im Rechenweg des Laufs nicht gelesen (kein Treffer in `EPOS.Kern/Allgemein/Simulation/`), nur Vergleichsmerkmal des Variantenberichts | `AbweichungsErmittler.cs:128` |"

**12. Die Null-Prüfungs-Liste ist unvollständig — der Verbrauchsweg teilt zweimal weiter ungeschützt**
(a) Kap. 2.1, Punkt 2: „**Keine Nullprüfung.** `:435` teilt durch `wohnflaeche`, `SimulationWaermebedarf.cs:571` durch `Flaeche_Nutzer`; eine 0 ergibt NaN oder Unendlich ohne Meldung."
(b) Im Verbrauchsweg `Bewohner_und_Flaeche_berechnen` stehen zwei weitere ungeschützte Divisionen — darunter die durch `VerbrauchAlt`, also durch ein **Rechenergebnis**, das bei einem Nullbedarf null wird; 4.8 führt dafür keine Pflichtprüfung.
(c) `EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:651` (`double FlaecheNeu = VerbrauchNeu / VerbrauchAlt * FlaecheAlt;`) und `:653` (`item.Bewohner = item.Z_AuswahlWohnflaeche / item.Flaeche_Nutzer;`).
(d) **mittel** — unvollständig; 4.8 („`Wohnflaeche > 0`, `Flaeche_Nutzer > 0`, …") deckt den Verbrauchsweg nicht ab.
(e) „**Keine Nullprüfung.** `:435` teilt durch `wohnflaeche`, `:571` und `:653` durch `Flaeche_Nutzer`, `:651` durch das Rechenergebnis `VerbrauchAlt`; jede 0 ergibt NaN oder Unendlich ohne Meldung." — und in 4.8 ergänzen: „`VerbrauchAlt > 0` vor der Rückrechnung".

**13. Kurzbeleg `:221` für die W→kW-Wandlung zeigt auf die Kommentarzeile**
(a) Kap. 2.1: „Die Gebäudereihe entsteht in Watt und wird einmal nach kW gebracht (`:221`); der Kanal `HEIZUNG` führt kW je Stunde…"
(b) Zeile 221 ist der Kommentar „// deshalb Anweisung für Anweisung dieselbe."; die Wandlung steht auf 222.
(c) `EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:222`: `WPPlan.Core.BhkwPlan.WattToKw(kanalHeizung);`.
(d) **niedrig** — Belegversatz um eine Zeile.
(e) „…und wird einmal nach kW gebracht (`:222`)…"

**14. Kurzbeleg `:426` für die Temperatur-Fortschreibung**
(a) Kap. 2.1: „Fortschreibung `T = e^(−L/C)·T_prev + (1 − e^(−L/C))·(…)/L` (`:426`), Kappung auf `Maximaleraumtemperatur` (`:430`)"
(b) Zeile 426 trägt nur den Hilfswert `a = 1 − exp(−L/C)`; die zitierte Fortschreibung selbst steht auf 429.
(c) `EPOS.Kern/Allgemein/BhkwPlan.cs:426-429` (`double a = 1.0 - Math.Exp(-L / C);` … `tPrev = Math.Exp(-L / C) * tPrev + pGesTerm;`).
(d) **niedrig** — Belegversatz.
(e) „Fortschreibung `T = e^(−L/C)·T_prev + (1 − e^(−L/C))·(…)/L` (`:426-429`), Kappung auf `Maximaleraumtemperatur` (`:430`)"

**15. „86–101 %" ist aus der eigenen Tabelle nicht ableitbar**
(a) Kap. 0, Satz 4 (gleichlautend 5.5): „trifft die Katalogkennzahl kWh/m²a zu 86–101 % (das heutige Modell 48–88 %)."
(b) Aus Tabelle 5.5 folgt 85,7 % (10643: 386,6 gegen 451) bis 100,3 % (10576: 112,8 gegen 112,5) — die untere Schranke ist aufgerundet, die obere abgerundet; 10.4 nennt für dasselbe Kriterium 85–105 %.
(c) Eigene Tabelle 5.5, Spalten „Prototyp kWh/(m²a)" und „Katalog"; Gegenrechnung: 386,6/451 = 0,857, 112,8/112,5 = 1,003.
(d) **niedrig** — Rundung gegen eigene Zahlen.
(e) „trifft die Katalogkennzahl kWh/m²a zu 86–100 % (Spanne 85,7–100,3 %; das heutige Modell 48–88 %)."

**16. Zwei Schreibweisen für denselben Lüftungsfaktor**
(a) Kap. 2.1: „Lüftung `f·(A_Wohn·h)·1,2·n·0,2777` mit `f = 1 + 0,025·θ_a` für θ_a < 0" — gegen Kap. 3.3: „`V·1,2·n·0,2778` = W/K".
(b) Der Code führt `0.2777777777777778`; „0,2777" ist abgeschnitten statt gerundet, und die beiden Stellen des Papiers widersprechen sich.
(c) `EPOS.Kern/Allgemein/BhkwPlan.cs:357` und `:359` (`… * 1.2 * lwr * 0.2777777777777778`).
(d) **niedrig** — Stil/Konsistenz.
(e) In 2.1 wie in 3.3 einheitlich: „`…·1,2·n·0,2778` (im Code `0.2777777777777778`, also 1/3,6)".

**17. „120 bzw. 192 Werte" ist im Code nicht belegt**
(a) Kap. 2.1: „Die Tageslast wird über ein 24-h-Tagesprofil je Tagtyp (`Abfrage_Tagverteilung`, 120 bzw. 192 Werte; `BhkwPlan.StdWerte` `:259-289`) auf Stunden verteilt"
(b) Der Leser legt **immer** ein Feld von 192 Werten an (8 Tagtypen × 24); die Zahl 120 steht weder im Leser noch in der Sicht und ist ohne Datenbankprobe nicht nachvollziehbar.
(c) `EPOS.Kern/Allgemein/Simulation/SimulationWaermebedarf.cs:660` (`double[] tagv = new double[192];`), Abfrage `:670`; Sicht `sql/schema/002_views.sql:109`.
(d) **mittel** — unbelegt.
(e) „Die Tageslast wird über ein 24-h-Tagesprofil je Tagtyp auf Stunden verteilt; der Leser `DBTagesVeteilung` (`SimulationWaermebedarf.cs:658-681`) füllt stets ein Feld von 192 Werten (8 Tagtypen × 24) aus `Abfrage_Tagverteilung`, von denen der Wohngebäudeweg nur die Tagtypen 1 und 2 nutzt (`BhkwPlan.StdWerte` `:259-289`)."

**18. Zeilenspanne von `ProjektGebaeudeCtrl.ReadAll` reicht über die Methode hinaus**
(a) Kap. 2.2: „**Falle für jeden Schemaschritt:** `ProjektGebaeudeCtrl.ReadAll` (`EPOS.Kern/Controller/ProjektGebaeudeCtrl.cs:26-107`) liest die Sicht `Abfrage_Projektgebaeude` **nach Spaltenindex** `row[0]…row[57]`"
(b) Die Methode endet auf Zeile 104; 105–107 sind `#endregion` und Klassen-/Namensraumende (die Datei hat 107 Zeilen).
(c) `EPOS.Kern/Controller/ProjektGebaeudeCtrl.cs:26` (Signatur), `:42-99` (`row[0]`…`row[57]`), `:104` (Methodenende), `wc -l` = 107.
(d) **niedrig** — Belegspanne.
(e) „…(`EPOS.Kern/Controller/ProjektGebaeudeCtrl.cs:26-104`, Zuweisungen `:42-99`)…"

**19. Der neue Spaltenname `Rechenmodell` folgt dem PV-Vorbild nicht, auf das sich das Papier beruft**
(a) Kap. 4.1: „**Ein Feld entscheidet:** `Tab_Gebaeude.Rechenmodell` (NULL = Tagesbilanz, Wert `VDI6007`), Persistenzwerte `GEBAEUDE_MODELL_TAGESBILANZ` / `GEBAEUDE_MODELL_VDI6007` in `DbWerte`."
(b) Das ausdrücklich zitierte PV-Muster heißt in der Datenbank `PV_Modell` (Gegenstand-Präfix), und seine Persistenzwerte sind namensgleich mit der Konstante (`PV_MODELL_EINFACH = "PV_MODELL_EINFACH"`) — der Vorschlag weicht in beidem ab, ohne das zu begründen.
(c) `EPOS.Kern/Model/WErzeugerModel.cs:226` (`PV_Modell`), `EPOS.Kern/Allgemein/DbWerte.cs:2173/2180`, `EPOS.Kern/Allgemein/Simulation/SimulationPV.cs:706-709`; im Repository gibt es heute keine Spalte `Rechenmodell` (Suche über `*.sql`/`*.cs` ohne Treffer).
(d) **niedrig** — Konsistenz zum Vorbild.
(e) „**Ein Feld entscheidet:** `Tab_Gebaeude.Gebaeude_Modell` (NULL = Tagesbilanz, Wert `GEBAEUDE_MODELL_VDI6007`), Persistenzwerte `GEBAEUDE_MODELL_TAGESBILANZ = "GEBAEUDE_MODELL_TAGESBILANZ"` / `GEBAEUDE_MODELL_VDI6007 = "GEBAEUDE_MODELL_VDI6007"` in `DbWerte` — Spalten- und Wertform wie beim Vorbild `Tab_Energieanlagen.PV_Modell` / `DbWerte.PV_MODELL_*` (`DbWerte.cs:2173/2180`)."

---

**Geprüft und bestätigt:** `_prevRoomTemp` als statisches Feld (`BhkwPlan.cs:51`) samt `ResetState()` (`:54`), das ausschließlich in `EPOS.Kern.Tests/BhkwPlanRueckgabeTests.cs:91/109/112` und nirgends in der Produktion gerufen wird; `day == 1` (`:398`), Fortschreibung (`:433`), Rückgabe mit Division durch `wohnflaeche` (`:435`); `SolareGewinneC` (`:311-318`, roher g-Wert `:316`) und `SpezWaermeverlusteC` (`:342-362`, Gewichte `:347-353`, Lüftungsfaktor `:355-359`) samt zitierter Formeln; `TaeglHeizlastWG` (`:388-436`) mit Heizlast `:418`, Solarfenster 9–14 h (`:420`, `:428`), Kappung `:430`; `StdWerte` (`:259-289`); `Waermebedarf_berechnen` (`:128`), `HeizwaermeEinesGebaeudes` (`:566-611`), Typweiche mit zwei Leerzeichen (`:601-608`), `Berechnung_Gebaeude_Tageswerte` (`:684-888`), Vorlauf Tage 350–364 (`:748-814`), Ferienschwelle 0,9 (`:689-699`), Wochenendschwelle > 5 (`:777-780`), `Bewohner_und_Flaeche_berechnen` (`:613-656`, `:641-648`), Ortszeit-Kommentar (`:903-911`), `SolareGewinneC`-Aufruf mit `Fensterflaeche_Ost` (`:826`); dass `waermebedarf_gebaeude.csv` in W steht (`WattToKw` trifft nur `kanalHeizung`, `:222`); `SchemaStand.Zielversion = 76` (`SchemaStand.cs:93`); `Referenzlauf/Ergebnisexport.cs:58-67` mit `waermebedarf_gebaeude.csv` auf `:59`; `Referenzlauf/Vergleich.cs:43-44` (1e-4 / 0,01); `DbWerte.cs:2173/2180` und `:1260-1271` (kein vierter Kanal); `Gebaeudebauweise.cs:22-67`, Nullprüfung `:44`, Rückfall `50` `:66`; `001_grundschema.sql:1131-1188`, `Fensterflaeche_Ost_West` auf `:1144`, `Tab_Solar` `:2153-2182`, `Tab_ErgebnisEnergiebedarf` `:850-863`, `Z_ProjektGebaeude` `:2873-2881`; `ProjektGebaeudeModel.cs:26`; Index 57 der Sicht = `Tab_Gebaeude.ID` und damit 58 Bestandsfelder; `GebaeudeStammCtrl.CopyFromStamm` (`:439`); `SimulationLaufCtrl.ErgebnisSpeichern` (`:210`); `SimulationErgebnisCtrl.Bedarf` (`:813-828`); `GebaeudeBedarfCtrl.Rechnen` (`:94-136`) ruft denselben Weg wie der Lauf (Regel `EPOS.Kern/CLAUDE.md:186`); `SimulationPV.cs:700-709` als Umschaltmuster und `PvErweitertesModell.cs` als Vorbild der reinen Rechenklasse; `KlimadatenCtrl.cs:37`, `SolardatenCtrl.ReadOrtszeit` (`:156-208`), `SolarPVGISCalculator.cs:100`, `KlimaImportAblauf.cs:132-136` und `:305-338` (vier Fassaden über `CalculateHourly`), `:354` (`WE`), `:355` (`TagTyp_W`); `Directory.Packages.props:20` (Microsoft.Data.Sqlite 10.0.11); `WindowsFormsApplication1/Views/Gebäude/GebaeudeHuelle.cs:322-350` und dass `EPOS.UI.Daten/Bedarf/` nur `BedarfErgebnisHuelle.cs` führt; `GebaeudeDialog.razor:153` (Gruppe „Verbrauch"), `GebaeudeBedarfDialog.razor:117`; `IDateiDienst`/`KeineDateiwahl` in `EPOS.Kern/Allgemein/Dienste/` samt Vorbelegung `Dienste.cs:45`; `EPOS.UI/Bausteine/Menuetabelle.cs`; die 4 s des Gesamtlaufs über dreizehn Projekte (`Referenzlaeufe/2026-09-11_R7_Speicherflotte/protokoll.txt:8`, `:390`) und Projekt 1007 „Laurentiuskirche"; kein Gradtagzahl-/Normaußentemperatur-Begriff im Kern; `Referenzlaeufe/Importproben/` existiert bereits als Ablage; **keine** Namenskollision für `Zonenmodell7R2C`, `ErsatzparameterRC`, `Bauteilreduktion`, `Zonenrandbedingungen`, `Zonenergebnis`, `GebaeudeModellEingang`, `GebaeudeModellErgebnis`, `IfcImportAblauf`, `IfcImportSatz`, `Tab_Bauteil`, `Tab_Bauteilschicht`, `Tab_Baustoff_STAMM` und alle elf Spaltennamen aus 6.1 (einschließlich `Fensterflaeche_West`); Hausregeln der Ablage: Papier liegt in `Dokumentation/aktuell/`, Indexzeile vorhanden (`Dokumentation/LIESMICH.md:50`), alle fünf relativen Verweise lösen auf, keine Commit-Kennung im Text, Kopfzeile im Muster der Schwesterpapiere, deutsche Bezeichner, UTF-8 **ohne** BOM mit durchgehend CRLF (1 356 Zeilenenden, kein einzelnes LF oder CR); Gliederung und Aufbau decken sich mit `Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md` und `Konzept_Stromspeicherimport_EPOS-Plan.md` (Auftrag im Wortlaut, „Ergebnis in einem Satz", Stufenplan, Fragen mit Empfehlung, Größenordnungen); Schemaschritt-Reihenfolge Konstante → Methode → `SCHRITTE`-Eintrag → `Zielversion` deckt sich mit `SchemaMigration.cs:62/72/79`, `SchemaKatalog.cs` führt das Muster `Schritt70_*` (`:1543`, `:1559`); die NULL-als-Vorgabe-Regel aus dem PV-Zweig ist belegt (`Konzept_Photovoltaik_Ertragsmodell_EPOS-Plan.md`, N2.2); die Zerlegung in 5.10 ist additiv in sich stimmig (16,1+10,9−9,1 = 17,9 usw.), ebenso die Skalierungsfaktoren in 3.6 und die kWh/(m²a) in 3.7.