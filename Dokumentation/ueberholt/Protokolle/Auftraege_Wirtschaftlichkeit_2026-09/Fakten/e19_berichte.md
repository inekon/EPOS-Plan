# E19 — Bericht Phase 0 (Opus, 25.09.2026 ca. 08:15, Worktree e19 ab b5a2e389, nur gelesen/gemessen)

## Nr. 15 Bilanzjahr und Unternehmensart wirken erst beim nächsten Öffnen — durch die Schalentrennung überholt
Herkunft B4-Protokoll Grenze 2 (`B4_Energieintensitaet_Protokoll.md:53-54`): WinForms-Trägerdialog las beide Werte beim Blockaufbau; der
Konzeptpunkt (`Konzept…konsolidiert.md:2901`) trägt nur den Titel. Heute: Trägerdialog-Hülle liest beide Werte je Öffnung
(`EnergietraegerHuelle.cs:107-109`, `:202` über `KatalogjahrErmitteln` `:2455-2475`; speist Katalogsatz, Schnellwahl `:2488`/`:672-677`,
Projekt-CO₂-Preis `:2628`/`:2714`); Hülle entsteht bei jedem Öffnen neu (Windows `EnergietraegerFenster.cs:38-65` ShowDialog; Kostenseite
`KostenSeiteGaben.cs:82-83`, `:953-963`); aus dem Trägerdialog führt kein Weg in Parameter-/BHKW-Dialog (Unterdialoge Kostenprofil,
Spotpreis, Saisonreihe, Emissionskatalog `EnergietraegerDialog.razor:272-298`; `UnterdialogGeschlossen` `:2438-2442` liest Träger neu,
nicht Jahr — folgenlos). Wirtschaftlichkeitsseite: jeder Unterdialog baut den Satz beim Öffnen neu (`WirtschaftlichkeitSeite.razor:1992-2007`,
`WirtschaftlichkeitSeiteGaben.cs:1595-1630`), nach dem Schließen `Laden` (`:2047-2067`; Emissionsbilanz `:400`/`:1480-1497`, Parameterzeile
`:737-742`); Überlagerungen schließen sich aus (`_offen`); Parameter- und BHKW-Dialog schreiben den ganzen Satz (`BhkwWirtschaftlichkeitHuelle.cs:193-215`).
BHKW-Dialog: E18-Anzeige folgt live (`BhkwWirtschaftlichkeitDialog.razor:397`, `:894-896`); Bilanzjahr aus dem geladenen Satz (dort nicht
pflegbar). Bewusst erst nach Rechenlauf: Kohärenzzeilen/Vorschau (`:2231-2280`, `:2322-2345`), Ergebnisansicht, § 9b-Betrag, Kohärenzfall 3;
Seite meldet „gespeichert — bitte neu berechnen" (`WirtschaftlichkeitSeiteGaben.cs:1636-1665`, `WirtschaftlichkeitSeite.razor:2066`).
Vorschlag: keine Codeänderung, Wache-Test (zweimal `EnergietraegerHuelle.Gaben` auf Kopie, dazwischen Unternehmensart ändern →
Empfehlung wechselt Regelsatz ↔ reduziert). Aufwand ~40 Zeilen; Risiko null.

## Nr. 33 Unternehmensart ohne BHKW nicht pflegbar
Sperre `WirtschaftlichkeitSeite.razor:588-592` (`_stand.MitBhkw` aus `WirtschaftlichkeitSeiteGaben.cs:440` ← `ErzeugerDerGruppe`
`WirtschaftlichkeitCtrl.cs:1562-1587`, zählt Tab_BHKW über Stamm und Varianten); verbirgt den ganzen BHKW-Dialog (Gruppe 4 Räumlichkeit,
Hocheffizienz, Modus § 9 Abs. 1 Nr. 3, E18-Anzeige; Überlagerung „Sätze und Herkunft"; Sprung „BHKW-Tarif…"); ohne BHKW gehört fachlich nur
Unternehmensart + E18-Anzeige dazu. iOS dieselbe Lücke (`IosProjektQuelle.BhkwDaten` null ohne Anlagen, `IosProjektQuelle.cs:217`,
`AppWurzel.razor:1214-1224`). Parameterdialog: Gruppe BHKW nur Verweis (`WirtschaftlichkeitParameterDialog.razor:436-442`), Gruppe Strom
(`:387-429`) ohne Unternehmensart; Nebenbefund: Bilanzjahr nur in der Brennstoffgruppe (`:446`, `:485-487`, `HatBrennstoff`). Rechenweg: seit
E5 rechnet der Kern § 9b auch ohne BHKW bei produzierendem Gewerbe oder Land-/Forstwirtschaft (`WirtschaftlichkeitCtrl.cs:4459-4473`; sonst
null) — existiert, über die Oberfläche unerreichbar. Testdatenbank: 15 von 19 Stämmen ohne BHKW; Referenzprojekte mit BHKW nur 1017, 1018,
1024, 1030 (**Nebenbefund: 1017 hat ein BHKW, Vorbehalt in A‑E18‑1 gegenstandslos**); `Tab_ProjektWirtschaftlichkeit` 5 Zeilen (1019/1030
KEIN_PROD_GEWERBE; 1017/1023/1024 NULL; `Bilanz_Jahr` überall NULL); ohne Brennstofferzeuger Stämme 19, 1006, 1032 (kein Referenzprojekt).
Vorschlag (a): Unternehmensart in den Parameterdialog Gruppe Strom nur ohne BHKW (mit BHKW Verweis `BHW_PARAM_VERWEIS`), je Projektlage
eine Pflegestelle, kein Schema. Hülle `WirtschaftlichkeitParameterHuelle.cs:71-89` + `["Stromsteueranteil"]` und Katalog-Delegat; Dialog
`Auswahlfeld` mit `BhkwWahlen.Unternehmensart()` (`BhkwWirtschaftlichkeitDaten.cs:135`) auf `Parameter.Unternehmensart`, darunter
`StromsteueranteilAnzeige.Zeilen(…)` mit `Parameter.BilanzJahr`/Rückfall (erledigt zugleich den Live-Teil von Nr. 15); Schreibweg vorhanden
(`WirtschaftlichkeitCtrl.cs:1275`, `:1430`); Menüs unberührt; Maskenwache `KiMaskenabdeckungWacheTests.cs:155` 34 → 35; KI-Feldkarte
`KiDialoge.cs` ab `:1791` + `WirtschaftlichkeitParameterKiSicht.cs`; Hilfeanker `help_mapping.txt:377` → `Wirtschaftlichkeit#parameter`
bleibt; iOS nicht anfassen. Aufwand 150–250 Zeilen, 1–2 Schlüssel; Risiko null (nur vorhandener Rechenweg erreichbar; Kapitalwert ändert
sich gewollt mit der Art).

## Fragen (Entscheid Orchestrator 25.09.2026 08:20 nach Empfehlung)
E19‑Q1 Nr. 15: a überholt schließen mit Befund + Wache-Test (b defensiv `KatalogjahrErmitteln` in `UnterdialogGeschlossen`). E19‑Q2 Ort:
a Parameterdialog Gruppe Strom nur ohne BHKW (b BHKW-Knopf immer, Dialog reduziert; c immer im Parameterdialog, Gruppe 4 nur Anzeige).
E19‑Q3 E18-Anzeige im Parameterdialog: a ja live (b nein). E19‑Q4 Bilanzjahr ohne Brennstofferzeuger: **b** so lassen, Rückfall 2026 als
benannte Grenze (a immer zeigen). E19‑Q5 KI-Feld unternehmensart: a anmelden mit Sperre bei BHKW + Hinweis (b ohne Sperre; c kein Feld).
E19‑Q6 Texte: a vorhandene BHW-Schlüssel + ein neuer Schlüssel § 9b-Erklärzeile (b eigene WPAR-Schlüssel).
Abnahme A‑E19‑1 (Vorschlag): (1) 1007 ohne BHKW: Parameter… Gruppe Strom zeigt Unternehmensart + E18-Zeilen; (2) produzierendes Gewerbe ohne
Speichern → Kohärenzzeile wechselt, Abbrechen schreibt nichts; (3) Speichern + berechnen → § 9b im Ergebnis; (4) 1030 mit BHKW: keine
Unternehmensart in Gruppe Strom, Verweis nennt BHKW-Dialog; (5) Englisch. Notizen: Hüllen-Kopfnotiz `WirtschaftlichkeitParameterHuelle.cs:33-34`
bleibt korrekt; Nr. 22/23/24 unverändert.

Bau freigegeben 25.09.2026 08:20: zuerst Merge origin a0bbc633 (Z5 #495 Schritt 140), dann E19/1 Wache Nr. 15, E19/2 Hülle/Dialog/Texte,
E19/3 KI-Feldkarte/KI-Sicht/Maskenwache 35, E19/4 Tests.

# E19 — Bericht Phase 1 (Opus, 25.09.2026 ca. 08:40, e19 = bfa2c9e5 ab a0bbc633 [ff auf Z5 #495]; Merge #498 = 31a0b085 auf pm20 ab f55a4cd5 [#496, Schritt 141])

Commits: a8832acd E19/1 Wache Nr. 15 `EPOS.Kern.Tests/KatalogjahrJeOeffnungTests.cs` (1030, Stromträger 60: vorher Regelsatz „(ab 2026"; nach
Speichern produzierendes Gewerbe + Bilanzjahr 2025 empfiehlt die neue Öffnung den reduzierten Satz „… im Jahr 2025"; kein Code am
Trägerdialog); c87354a0 E19/2 Hülle `WirtschaftlichkeitParameterHuelle.cs` liefert `Stromsteueranteil` (`StrompreisZerlegungCtrl.StromsteuerErfasst`)
und `Katalog` (`GesetzKatalog.WertMitHerkunft`); `WirtschaftlichkeitParameterDialog.razor` Gruppe Strom nur bei `!HatBhkw`: Auswahlfeld
Unternehmensart (`BhkwWahlen.Unternehmensart()`, Beschriftung `BHW_S_UNTERNEHMENSART`), darunter `StromsteueranteilAnzeige.Zeilen(…)` live mit
`Parameter.Unternehmensart`/`Parameter.BilanzJahr` (Rückfall Bilanzkonvention), Erklärzeile; mit BHKW nur `BHW_PARAM_VERWEIS`; neue
`[Parameter]` `Stromsteueranteil`, `Katalog`; **ein neuer Schlüssel** `WPAR_UA_9B_HINWEIS` (de/en, nach `WPAR_TITEL`; Text
`WirtschaftlichkeitParameterDaten.Unternehmensart9b`), Designer 11.007 → 11.008 wiederholbar; 14d55922 E19/3 `KiDialoge.cs` Feld
`unternehmensart` (Wahl, `BhwUaName`/`BhwUaErl`), Maske Form_WirtschaftlichkeitParameter 35 Felder; `WirtschaftlichkeitParameterKiSicht.cs`
`Unternehmensart`, `UnternehmensartWahl`, `UnternehmensartSetzen` (mit BHKW `InvalidOperationException` mit `BHW_PARAM_VERWEIS`);
`KiMaskenabdeckungWacheTests` 34 → 35; bfa2c9e5 E19/4 Tests: `WirtschaftlichkeitParameterDialogTests` +6 bunit (sichtbar ohne BHKW; mit BHKW
nur Verweis; Wahl/Bilanzjahr drehen Anzeige live ohne Speichern; Abbrechen schreibt nichts; OK schreibt mit Parametersatz; KI setzt nur ohne
BHKW; Klapplisten-Zählung 1 → 2 bzw. 4 → 5); neu `EPOS.Kern.Tests/UnternehmensartOhneBhkwTests.cs` (Hülle liefert Anteil + Katalog, jeder
Schlüssel trifft einen `[Parameter]`; § 9b ohne BHKW: **1041** [statt 1007, das kein gespeichertes Ergebnis hat] rechnet mit produzierendem
Gewerbe Entlastung > 0, mit KEIN_PROD_GEWERBE 0). iOS unberührt.

Prüfungen: Kern-Filter Release 0 Fehler, Windows-Schale x64 0 Fehler; kein SQL berührt; keine Tabellenänderung; gefiltert Kern 155/155
(UnternehmensartOhneBhkwTests 2/2), UI 425/425 (6 E19-Fälle); voller Lauf 14.023 bestanden / 0 Fehler / 2 übersprungen (EPOS.Kern 6.818 + 1
übersprungen, EPOS.UI 6.243, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27 + 1); Testhost-Regel eingehalten (zweimal gewartet);
Referenzlauf 13/13 gegen R14 PASS, 4.207.049 Werte, 394/394 CSV byte-gleich; Anker unberührt. Aufgeräumt: -shm/-wal im Worktree gelöscht.
Merge #498 = 31a0b085 auf pm20 ab f55a4cd5 (#496 Schritt 141, Testdatenbank a427aa72): ort ohne Konflikt, Designer wiederholbar (+0).

Erledigt-Gründe: § 6.3 Nr. 15 überholt (E19‑Q1 a): Hülle der Energieträgerverwaltung liest Bilanzjahr und Unternehmensart je Öffnung
(`EnergietraegerHuelle.Gaben`; jede Öffnung neue Hülle: Windows modales `EnergietraegerFenster`, Kostenseite `KostenSeiteGaben.TraegerGaben`);
aus dem Trägerdialog kein Weg in Parameter-/BHKW-Dialog; Wirtschaftlichkeitsseite frischt nach jedem Unterdialog auf; Anzeige des
Stromsteueranteils folgt im BHKW- und jetzt im Parameterdialog live; Kohärenzzeilen/Vorschau/Ergebnis bewusst erst nach Rechenlauf; Wache
`KatalogjahrJeOeffnungTests`. § 6.3 Nr. 33 erledigt (E19‑Q2/Q3/Q5/Q6 a): ohne BHKW pflegt der Parameterdialog Gruppe Strom die
Unternehmensart samt Anzeige; mit BHKW bleibt der BHKW-Dialog einzige Pflegestelle, KI lehnt benannt ab; kein Schema, kein Kern-Umbau; § 9b
ohne BHKW (seit E5) erreichbar; benannte Grenze (Q4 b): Bilanzjahr bleibt in der Brennstoffgruppe, ohne Kessel/BHKW Rückfall 2026 (nur
Katalogjahr/Anzeige, nicht Rechnung). Nebenbefund 1017 hat BHKW → A‑E18‑1-Vorbehalt gegenstandslos. Offen (nicht beauftragt): Kommentar
„Zwoelf Felder gehoeren dem Parametersatz" `KiDialoge.cs:1777` veraltet (17 Parametersatz-, 18 Szenariofelder).
Logbuchsatz (`wirtschaftlichkeit`): „Ohne BHKW wird die Unternehmensart nach Stromsteuergesetz in den Wirtschaftlichkeits-Parametern
(Gruppe Strom) gepflegt; darunter steht der im Strompreis erfasste Stromsteueranteil." Wiki-Anker `parameter` nachziehen (Papiere).
Abnahme A‑E19‑1 (Windows): (1) 1041 oder 1007 ohne BHKW → Parameter…: Gruppe Strom zeigt Unternehmensart, Stromsteueranteil-Zeilen,
§ 9b-Erklärzeile; (2) produzierendes Gewerbe wählen ohne Speichern → Kohärenzzeile wechselt sofort, Abbrechen schreibt nichts; (3) Speichern
→ Berechnen → § 9b-Entlastung im Ergebnis; (4) 1030 mit BHKW: keine Unternehmensart in Gruppe Strom, Gruppe BHKW verweist; (5) Englisch.

**Anwenderentscheide 25.09.2026 zu § 6.3 (mit dieser Welle nachzutragen):** Nr. 10 „Wärmepumpe beides" nur bei Investitionskosten nach kW
elektrisch und kW thermisch (kWh-Bemessung der WP bleibt thermisch; Strom-kWh sind Energiekosten) → kleine Bauwelle folgt; Nr. 11 nicht
nachziehen (schließen); Nr. 13 nur Volumen (Grenze bestätigt, schließen); Nr. 19/E10‑Q6 belassen (a, €/kWh el. beim BHKW ist die übliche
Vertragsform); Nr. 18 nach Empfehlung: Messwelle zuerst (läuft, Worktree mess18), dann Entscheid.
