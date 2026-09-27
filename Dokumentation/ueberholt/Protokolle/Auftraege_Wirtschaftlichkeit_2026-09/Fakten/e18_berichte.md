# E18 — Bericht Phase 0 (Opus, 24.09.2026 ca. 23:30, Worktree e18 ab fbc4e536, nur gelesen/gemessen)

## 1 Wache Konstante gegen Katalog (§ 6.3 Nr. 14 / § 6.5)
Konstanten `StrompreisZerlegungModel.cs:78` `STROMSTEUER_REGELFALL = 2.050`, `:89` `STROMSTEUER_REDUZIERT = 0.050`; Umlagen Z. 42/48/54; Feld
`Stromsteuer` (Z. 144) nur Vorschlag, gerechnet erst mit Aktiv-Schalter. Saat `GesetzKatalog.cs:1475` `STROMST_REGELSATZ` 2026 20,50 EUR/MWh,
`:1495` `STROMST_REDUZIERT_SATZ` 2026 0,50 EUR/MWh (Generation 7). Testdatenbank (Generation 9): ID 50 20,5; ID 224 0,5; Umlagen IDs 225–227
0,446/0,941/1,559 ct/kWh; alles wertgleich, keine Zeile nach 2026. Konstanten greifen nur als Rückfall (`EnergietraegerHuelle.cs:672–677`,
Schnellwahl ~2500) für Jahre vor 2026 oder gelöschte Zeile (`GesetzKatalog.cs:244`). **§ 6.5 stimmt so nicht:**
`StrompreisZerlegungTests.Die_Katalogwerte_und_die_Rueckfallebene_sind_wertgleich` (Z. 78–98, seit 17.09.) prüft Konstante gegen Saat, aber
nur `JahrVon == 2026`; es fehlt (a) Konstante gegen Testdatenbank-Katalog, (b) Robustheit gegen spätere Jahreszeile. Vierter Ort: Ressourcen
`PREIS_STROMSTEUER_REGELFALL/_REDUZIERT` (`Resource.resx:4614/4617`, en 4598/4601, „2,05"/„0,05") ohne Leser seit B4. `GesetzKatalogTests`
gibt es nicht; Muster `GesetzkatalogSaatWacheTests`, `StrompreisZerlegungTests`. Vorschlag: neue Wache in `StrompreisZerlegungTests`
(`[Collection("Testdatenbank")]`), älteste Zeile je Schlüssel, Einheit umrechnen (EUR/MWh ÷ 10, ct/kWh × 1, unbekannt = rot), Meldung nennt
drei Orte (Modell Z. 78/89, `GesetzKatalog.Vorbelegung` 1475/1495 + Generation, Testdatenbank-Nachsaat); Saattest auf „älteste Zeile";
tote Ressourcen streichen. Aufwand ~1 h; Risiko Anker/Referenzlauf null.

## 2 Nr. 18 Engine-Sortierung `ORDER BY Prioritaet` (HB1-O1) — offen, nicht streichen
Ursprung `HB1_Hydraulikbild_Sortierung_Protokoll.md` (0d4be91ca, 30.08.), HB1-O1: fünf `ORDER BY Prioritaet`-Stellen des Rechenwegs tragen
die Schieflage ungepflegt (NULL/0) vorn; HB1 stellte nur vier Anzeige-Leser auf `Ladeordnung.SqlAnlagenprio` (99er-Regel,
`Ladeordnung.cs:51–80`) um. Heute unverändert `ORDER BY Prioritaet, ID`: `SimulationControl.cs:1542` (`WP_Liste_Laden`), `:3410`
(`QuellbezuegeAufbauen`), `:4184` (`SenkenPufferDerAnlagen`), `WaermesenkeClass.cs:774` (`SenkenLaden`), `:912` (`SenkenlistenLaden`). Die
„drei Stellen" in § 6.3 Nr. 18 sind die gemeinten, unvollständig (WaermesenkeClass fehlt); SQLite sortiert NULL wie ACE zuerst. Messung:
48 von 60 Wärmeerzeugern (Typ 1/2/10/11) mit `Prioritaet` NULL, keiner 0; 99er-Regel änderte die Reihenfolge in 5 von 13 Referenzprojekten
(1030, 1040, 1041, 1042, 1045); in 1042 dreht sich die Modulreihenfolge der Wärmepumpen (14818 NULL vor 14817 Prio 1). Umbau = Rechenweg,
kein bitgleicher Referenzlauf → nicht in E18. Befundsatz für Nr. 18: „offen, weil die fünf Rechenweg-Leser weiterhin ungepflegt vor gepflegt
sortieren; Umbau ändert die Reihenfolge in 5/13 Referenzprojekten." Optional Verweiskommentar HB1-O1 an den fünf Stellen.

## 3 Nr. 16 Rückweg „Parameterdialog zeigt den erfassten Preisanteil"
Herkunft `B4_Energieintensitaet_Protokoll.md` § 4 Grenze 3; Hinweg Unternehmensart → Hervorhebung Schnellwahl im Trägerdialog
(`EnergietraegerHuelle.cs:2482` `ReduzierterSatzEmpfohlen`, :2540ff). Fehlt: der erfasste Stromsteueranteil des Stromträgers
(`energy_project_settings.Aufschlag_Stromsteuer` + `_Aktiv`, gepflegt in „Strompreis Details", `StrompreisZerlegungCtrl`) dort, wo die
Unternehmensart gepflegt wird — das ist heute der Dialog „BHKW-Wirtschaftlichkeit" (`BhkwWirtschaftlichkeitDialog.razor:365–375` Gruppe 4
„Stromsteuer", Überlagerung Z. 675–683, `UnternehmensartWirkungen` Z. 1992), nicht mehr der Parameterdialog
(`WirtschaftlichkeitParameterDialog.razor:387–429` Gruppe Strom nur Einspeisevergütung PV). Nebenbefund: Unternehmensart nur mit BHKW
erreichbar (`WirtschaftlichkeitSeite.razor:588`, `_stand.MitBhkw`). Lesewege: `StrompreisZerlegungCtrl.StromCarrierId` (Z. 118), `Read`;
roher NULL-Wert nur privat `KohaerenzPruefung.StromsteuerRoh` (Z. 1347), Stromseite/Fall 3/4 Z. 1133–1293. Testdatenbank: 1017 zwei
Stromträger (54, 58) je 2,05 aktiv; 1023/1024 je 2,05 aktiv; 1030 NULL/inaktiv, KEIN_PROD_GEWERBE. Vorschlag: Kern
`StrompreisZerlegungCtrl.StromsteuerErfasst(idProjekt)` (Record: Träger-ID/-Name, Wert ct/kWh roh, Aktiv, lesbar), rohes Lesen aus
`KohaerenzPruefung` verschoben; Hülle `BhkwWirtschaftlichkeitHuelle.cs:84–127` Gabe „Stromsteueranteil"; Dialog Herleitungs-/Kohärenzzeile
unter der Unternehmensart (Gruppe 4 und Überlagerung), folgt live der Unternehmensart, gleicht über `Katalog`-Delegat und Jahr
(`Parameter.BilanzJahr` > 0 sonst `BilanzKonvention.BILANZJAHR_RUECKFALL`, wie `EnergietraegerHuelle.cs:2455`) gegen Regelsatz/reduziert,
„Abweichend" ohne Sperre; `BhkwWirtschaftlichkeitKiSicht.cs` mitführen; ~9 Schlüssel `BHW_S_STANTEIL_*` im Bündel
`BhkwWirtschaftlichkeitTexte`. Aufwand 3–4 h; Risiko null.

## Fragen (Empfehlung/Entscheid Orchestrator 24.09.2026: jeweils a)
E18‑Q1 Wache gegen älteste Zeile (a) / jüngste (b). E18‑Q2 tote Ressourcen streichen (a) / lassen (b). E18‑Q3 Nr. 18 offen lassen mit
Befundsatz, Umbau nur als eigene Etappe mit Neueinfrierung (a) / in E18 umbauen (b) / als Grenze schließen (c). E18‑Q4 Anzeige im Dialog
BHKW-Wirtschaftlichkeit Gruppe 4 + Überlagerung (a) / zusätzlich Parameterdialog (b) / nur Parameterdialog (c). E18‑Q5 Zeile mit Wert, Aktiv,
Satzabgleich, Vorschlag, Kohärenz ohne Sperre (a) / nur Wert (b). E18‑Q6 roher Leseweg nach `StrompreisZerlegungCtrl` verschoben (a) / Kopie
(b). **E18‑Q7 (Notiz, neuer Restpunkt):** Unternehmensart ohne BHKW nicht pflegbar → § 9b bei Projekten ohne BHKW.

Bau freigegeben 24.09.2026 ~23:35: E18/1 Wache + Ressourcen, E18/2 Verweiskommentare HB1-O1, E18/3 Kern, E18/4 Hülle/Dialog/Texte/KI-Sicht,
E18/5 Tests.

# E18 — Bericht Phase 1 (Opus, 24.09.2026 ca. 23:55, e18 = 29fad34d ab fbc4e536; Merge #492 = e79bffb1 auf pm19 ab 247e2091 [#493, Schritt 130])

Commits: 52e223e2 E18/1 Wache Rückfallebene gegen Katalog + zwei tote Stromsteuer-Ressourcen gestrichen; 52b94866 E18/2 Verweiskommentar
„HB1-O1, offen: ungepflegt vor gepflegt" an den fünf Rechenweg-Sortierungen (nur Kommentare); 50ef787d E18/3 `StromsteuerErfasst` im Kern,
`StromsteuerRoh` wortgleich aus `KohaerenzPruefung` nach `StrompreisZerlegungCtrl` (public), Kohärenz Fall 4 ruft ihn; 5c05ea6b E18/4 Dialog
„BHKW-Wirtschaftlichkeit" zeigt den erfassten Stromsteueranteil; 29fad34d E18/5 Tests.

E18/1: Saattest vergleicht älteste Saatzeile je Schlüssel statt `JahrVon == 2026`; neue Wache
`Die_Rueckfallebene_steht_wertgleich_im_Katalog_der_Testdatenbank` (älteste Zeile `Tab_Gesetzesparameter` gegen `StrompreisZerlegungModel`:
STROMST_REGELSATZ, STROMST_REDUZIERT_SATZ, drei Umlagen; EUR/MWh ÷ 10, ct/kWh × 1, andere Einheit rot; Meldung nennt Modellkonstante,
`GesetzKatalog.Vorbelegung` + Generation, Testdatenbank). `PREIS_STROMSTEUER_REGELFALL/_REDUZIERT` gestrichen (Designer 10.006 → 10.004).
Gegenprobe: Kopie mit STROMST_REGELSATZ 2026 = 21 EUR/MWh → rot „… 21 EUR/MWh = 2.1 ct/kWh, die Rückfallebene trägt 2.05 ct/kWh" + drei Orte;
Kopie gelöscht.
E18/3: `StromsteuerErfasst(idProjekt)` → `StromsteueranteilStand` (Träger-ID/-Name, roher Wert oder null, Aktiv, Lesbar, Grund); wirft nie.
E18/4: neu `EPOS.UI/Dialoge/Wirtschaftlichkeit/StromsteueranteilAnzeige.cs`; Anzeige unter der Unternehmensart in Gruppe 4 „Stromsteuer" und
in der Überlagerung „Sätze und Herkunft", folgt live der (ungespeicherten) Unternehmensart; Zeile 1 Herleitung: Wert, aktiv/abgeschaltet,
Abgleich Regelsatz/reduziert/weder (Satzquelle Katalog des Bilanzjahres, sonst Bilanzkonvention 2026, sonst Rückfallebene; Toleranz 0,005
ct/kWh wie Kohärenz Fall 4); Zeile 2 nur bei aktivem Anteil: Kohärenz Ok/Abweichend wie Schnellwahl des Trägerdialogs, keine Sperre. Gabe
`Stromsteueranteil` in `BhkwWirtschaftlichkeitHuelle`, Record `BhkwDialogDaten` (optionaler Parameter), `AppWurzel.razor`,
`EPOS.iOS/Dienste/IosProjektQuelle.cs` (iOS-Zeile lokal nicht kompilierbar, kein iOS-Lauf). KI-Sicht unverändert (Anzeige-/Kohärenzzeilen
sind nach KiDialoge-Regel kein Feld). 14 Schlüssel `BHW_S_STANTEIL_*` (ERFASST, AKTIV, INAKTIV, KEINER, KEIN_TRAEGER, NICHT_LESBAR, REGEL,
REDUZIERT, ABWEICHEND, SATZ_REGEL, SATZ_REDUZIERT, PASST, VORSCHLAG, PFLEGE), Designer 10.004 → 10.018, wiederholbar.
E18/5: `StromsteuerErfasstTests` 4 Fälle (1017 2,05 aktiv mit Träger; 1030 null nicht 2,05; 1018 und Projekt 0 ohne Stromträger; roher
Leseweg); `BhkwWirtschaftlichkeitDialogTests` +8 bunit (ohne Gabe keine Zeile; Regelsatz Ok; Wechsel produzierendes Gewerbe live ohne
Schreiben; Satzabgleich Bilanzjahr 2027; fremder Satz; abgeschaltet ohne Kohärenzzeile; nicht erfasst/kein Träger/nicht lesbar; Englisch).

Prüfungen: Kern-Filter Release 0 Fehler, Windows-Schale Debug x64 0 Fehler; SQL-Prüfer 1.825/0; gefiltert Kern 126/126, UI 94/94 bzw.
500/500 (nach Korrektur am Englisch-Test); voller Lauf Kern 6.124, UI 6.028, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27+1 = 13.114
grün / 0 rot; Referenzlauf 13/13 gegen R14 PASS, 4.207.049 Werte, alle CSV byte-gleich; Anker unberührt. Testhost-Regel: einmal Prüfung im
selben Aufruf (UI-Filterlauf), fremder Testhost lief mit — Regelverstoß, Ergebnis grün.
Merge #492 auf pm19 ab 247e2091 (#493 Schritt 130, Testdatenbank f8fe1b76): ort ohne Konflikt; Designer 10.024 wiederholbar (+0).

Erledigt-Gründe: § 6.3 Nr. 14 erledigt mit E18/1 (Wache Saat + Testdatenbank, älteste Zeile, Umlagen; tote Ressourcen weg); § 6.5 Zeile
Stromsteuersatz neu „gekoppelt durch zwei Wachen (Saat und Testdatenbank, älteste Zeile) — eine Novelle ist eine spätere Jahreszeile und
lässt die Rückfallebene stehen" (bisherige Aussage „Wache prüft Modell gegen Konstante" war unzutreffend); § 6.3 Nr. 16 erledigt mit
E18/3–5 (Pflegestelle der Unternehmensart ist seit dem Auszug der Steuerfelder der Dialog BHKW-Wirtschaftlichkeit; Anzeige mit Satzabgleich,
Pflege nur in „Strompreis Details"); § 6.3 Nr. 18 neuer Wortlaut: offen, fünf Rechenweg-Leser (SimulationControl ×3, WaermesenkeClass ×2)
sortieren ungepflegt vor gepflegt, 99er-Regel änderte 5/13 Referenzprojekte (1030, 1040, 1041, 1042, 1045; in 1042 Modulreihenfolge der
Wärmepumpen), Umbau = eigene Etappe mit neuem Referenzlauf, Stelle im Code mit HB1-O1 vermerkt. Neuer Restpunkt (E18‑Q7): Unternehmensart
nur mit BHKW pflegbar (`WirtschaftlichkeitSeite.razor:588`) → § 9b und der Rückweg für Projekte ohne BHKW unerreichbar.
Logbuchsatz (`wirtschaftlichkeit`): „Der Dialog ‚BHKW-Wirtschaftlichkeit' zeigt unter der Unternehmensart den im Strompreis erfassten
Stromsteueranteil und ob er zur gewählten Unternehmensart passt." Abnahme A‑E18‑1: (1) 1024 oder ein Projekt mit BHKW und Anteil 2,05 aktiv:
Gruppe Stromsteuer nennt Träger, 2,050 ct/kWh aktiv, Regelsatz 2026, grüne Kohärenzzeile; (2) Unternehmensart auf produzierendes Gewerbe
ohne Speichern → Hinweis reduzierter Satz mit Verweis „Strompreis Details", Abbrechen schreibt nichts; (3) 1030: „kein Stromsteueranteil
erfasst" mit Pflegehinweis; (4) englische Oberfläche. (1017 hat evtl. kein BHKW → Dialog nicht erreichbar, siehe Q7.)
