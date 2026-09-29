# E20 — Bericht Phase 0 (Opus, 25.09.2026 ca. 09:10, Worktree e20 ab cbed6dba, nur gelesen/gemessen)

Kern: **Tab_WP hat keine Spalte für die elektrische Leistung** (`Nennleistung` INTEGER thermisch, beim VDI-3805-Import abgeschnitten
`KatalogImportSatz.cs:486`; `maxPtherm` immer 0; `Heizung` kW Heizstab; `Kuehlleistung`, `Kuehlbetrieb`). Nenn-COP aus VDI 3805 (Satz 700 Sp. 31,
`WaermepumpenImport.cs:145`) nur im Einlesedialog, nicht gespeichert. P_el = Ptherm ÷ COP aus der Kennlinie `Tab_Kenndaten` (Vorlauf, Temperatur,
COP, Ptherm) — dritter gerechneter Zweig neben PV (`KwpSumme`) und Solarthermie (`KollektorfeldKw`); kein Schemaschritt.
Messung: 29 Anlagenzeilen mit WP (19 L/W, 10 S/W), alle mit Kennlinie Vorlauf 35, kein COP ≤ 0. Katalog _STAMM 49 Geräte (34 L/W, 14 S/W,
1 W/W, 2 ohne Typ): Normpunkt je Typ (A2/W35, B0/W35, W10/W35) bei allen echten Geräten vorhanden; Ausnahmen T 800-2 und 352.AHT (W35 nur
−5/0/5/10 bzw. 0/5/10/15 °C → lineare Interpolation); LS 16-B R nur COP 0, test7 ohne Kennlinie → null mit Grund GERAET. `Nennleistung` als
Zähler unbrauchbar (Verhältnis zu Ptherm am Normpunkt 0,26–1,9: CS5800i AW 12 M 12 vs 4,3; CS3400i AWS 10 E 3 vs 3,8; L 28 I-2 25 vs 13,4).
Maximum über die Kennlinie bis Faktor 2 über Normpunkt (T 800-2 17,06 statt 8,8 kW bei 65 °C) = eher Anschlussleistung. Kühlbetrieb (nur 1017)
P_el 2,7–4,2 kW < Heiz-Normpunkt 7,91 kW. Beispiele: 1024 CS6800iAW 10 A2/W35 11,6 ÷ 2,90 = 4,00 kW; 1017 WPE-I 59 B0/W35 35,6 ÷ 4,50 = 7,91 kW.
Kreuzung Art ↔ Gewerk: Landkarte `TechnikPlanwertCtrl.cs` `Geraetespalte` (:773–836; EUR_PRO_KW_ELEKTRISCH nur BHKW Pel und Stromspeicher
Leistung :814–819), `BaugroesseSumme` (:669–734), `KenntBaugroesse` (:878–885), `BaugroessenName` (:848–870), `BaugroesseHerleitung` (:1007–1022),
`IstPvLeistungsart` (:893–898) als Muster; Grund/Filter `WirtschaftlichkeitCtrl.BasisGrund` (:8260–8335, Gerätearten :8328–8331), `BasisGrundFuerZeile`
(:8347), `FrischeBasis` (:8440, liest KategorieID nicht); Kategorie 1 `InvestKaskade.InvestBetrag` (:431–458, BaugroesseSumme :447); Kategorie 2
`RueckfallMenge` (:8121–8177, :8163); Dialogfilter `BemessungKatalog.PasstZuGewerk` (`KostenVorlagenCtrl.cs:829`, ohne Kategorie), `Auswahl`
(:864–878, kennt invest, gibt es nicht weiter), Aufrufer `KostenKomponenteHuelle.BemessungenBauen` (:895, `_invest`); Dialogzeilen
`KostenProjektPositionenCtrl.cs` (:290, :320, :337 ff., :489–492, `BasisNachziehen` :531, KategorieId :99/:245); Herleitung
`KostenHerleitung.HerkunftText` (:253), Ressource `KDLG_GR_PEL` vorhanden; KI-Sicht liest Bemessungsliste der Hülle (`KiDialoge.cs:6602`); Berichte
nur Betriebskosten mit BemessungText (`BausteineWirtschaftlichkeit.cs:757`, `ExcelBerichtGenerator.cs:1650`), kein Investitionsraster im
Bericht, Anhang D unberührt; Wache `PufferspeicherVolumenbemessungTests.Die_Zuordnung_kommt_aus_EINER_Landkarte` (:135–162); Tests mit
WP × kW el. = unpassend: `BetriebskostenBaugroesseTests:375`, `BetriebskostenBemessungsmatrixTests:370`, `BemessungsauswahlJeGewerkTests:69–90`.
Vorschlag: (1) Kern `IstWpElektrischeLeistung(komponentenID, bemessung, investition)`, `WaermepumpePelKw(projektID, idAnlage)` (Σ Anlagenzeilen,
Kennlinie Projektgerät, Vorlauf 35, Normtemperatur nach Typ, sonst Interpolation; null bei fehlendem Punkt/Typ/COP ≤ 0), `WaermepumpePelHerleitung`
(„11,60 kW ÷ COP 2,90 (A2/W35) = 4,00 kW"), Überladungen mit `bool investition` Vorgabe false; (2) Kategoriebindung in BasisGrund/
BasisGrundFuerZeile/PasstZuGewerk/Auswahl, InvestKaskade true, FrischeBasis liest KategorieID, KostenProjektPositionenCtrl reicht Kategorie,
RueckfallMenge false (Betrieb bleibt GEWERK); (3) 1–2 Ressourcen `KDLG_HERLEITUNG_WP_PEL` (+ Summenform); (4) Tests; (5) A/B 1024 Satz
1.000 €/kW → 4.000 €. Aufwand 3,5–4,5 h; Risiko null (keine Zeile in Tab_ProjektWerte/Vorlagen mit KomponentenID 1 trägt EUR_PRO_KW_ELEKTRISCH;
an der WP nur EUR_PRO_KW_HEIZLEISTUNG in 1040 und einer Vorlage).

Fragen (Entscheid Orchestrator 25.09.2026 nach Empfehlung, alle a): E20‑Q1 P_el = Ptherm ÷ COP am Normpunkt je Typ, bei W35 interpoliert
(b Maximum, c Auslegungspunkt, d Nennleistung ÷ COP); Q2 Normpunkt L/W A2/W35 (b A7/W35 EN 14511; 1024: a 4,00, b 4,23 kW); Q3 Heizstab nicht
einrechnen; Q4 nur Heiz-Normpunkt; Q5 Schalter `investition` in der Landkarte, Betrieb bleibt GEWERK (b überall freischalten, Betriebsauswahl
sperren); **Q6 offen für den Anwender:** Betriebsraster der WP bietet heute „je kWh elektrisch" (Grund LAUF) — a in E20 nicht anfassen, als
Rest von Nr. 10 benennen (gebaut); b aus der Betriebsauswahl nehmen, Bestandszeilen über `benutzt` schützen; Q7 Beschriftung „je kW elektrisch"
unverändert; Q8 Grund GERAET wiederverwenden. Hinweise für Papiere: Wiki Kosten Tafel (:30–46) und Absatz :58 WP-Zeile „je kW elektrisch (nur
Investition)"; Konzept § 3.2 Tafel (:1788–1797) Zeile WP × EUR_PRO_KW_ELEKTRISCH.
Bau freigegeben 25.09.2026 09:15: E20/1 Kern, E20/2 Kategoriebindung, E20/3 Texte, E20/4 Tests, E20/5 A/B.

# E20 — Bericht Phase 1 (Opus, 25.09.2026 ca. 09:50, e20 = a414b768 ab cbed6dba)

Commits: 13d8a9a4 E20/1 Kern-Lesekette `TechnikPlanwertCtrl.cs`: `WaermepumpePelKw(projekt, idAnlage)` = Ptherm ÷ COP am Normpunkt der Kennlinie
`Tab_Kenndaten` bei Vorlauf 35 (Typ L/W A2, S/W B0, W/W W10 über `DbWerte.WP_BAUART_*`; fehlende Stützstelle linear interpoliert je Größe, nie
extrapoliert; COP ≤ 0, Ptherm ≤ 0, unbekannte Bauart → null; Heizstab/Kühlkennlinie zählen nicht), neu `IstWpElektrischeLeistung`,
`WpNormQuellentemperatur`, `WaermepumpeNormpunkt`, Klasse `WpNormpunkt`; `BaugroesseSumme`/`KenntBaugroesse` Überladung `bool investition`
(Vorgabe false), `BaugroessenName` → `KDLG_GR_PEL`. e10300d7 E20/2 Kategoriebindung: `WirtschaftlichkeitCtrl.BasisGrund(bem, komp,
elektrokessel, investition)`, beide `BasisGrundFuerZeile` mit `investition`, `RueckfallMenge(…, investition = false)`, `FrischeBasis` und
`MengeAusweisen` lesen KategorieID; `KostenVorlagenCtrl.PasstZuGewerk(persistenz, komp, invest)`, `BemessungKatalog.Auswahl` filtert je Raster;
`InvestKaskade.cs` true; `KostenProjektPositionenCtrl.cs` reicht Kategorie durch; Betriebsschleifen false (GEWERK). f50ac248 E20/3
`WaermepumpePelHerleitung` („11,60 kW ÷ COP 2,90 (A2/W35) = 4,00 kW", interpoliert mit „≈", mehrere WPs als Summe), `BaugroesseHerleitung`
Überladung, **zwei Ressourcen** `KDLG_HERLEITUNG_WP_PEL` „{0} kW ÷ COP {1} ({2}) = {3} kW", `KDLG_HERLEITUNG_WP_PEL_SUMME` „Σ P_el am Normpunkt von
{0} Wärmepumpen = {1} kW" (hinter `KDLG_HERLEITUNG_SOLAR_KW_FLAECHE`), Designer 11.008 → 11.010 wiederholbar. a414b768 E20/4 Tests:
`WaermepumpeElektrischeLeistungTests` 13 Fälle (Kopien 1024/1017/1006/1009: A2/W35 4,00 kW; B0/W35 mit Kühlbetrieb 7,91 kW; Interpolation T 800-2
A0/A5 8,75 kW; ohne umschließende Stützstelle null + Grund GERAET; COP 0/Bauart fehlt null; ohne Raster/Betrieb null; Kaskade 4.000 € Herkunft
ANLAGE, Betriebszeile 0/GEWERK; Dialogzeilen, Herleitung, Summenform, Name P_el); `PufferspeicherVolumenbemessungTests` Kreuztafel je Raster +
Fall „nur je kW elektrisch an der WP unterscheidet sich"; `BemessungsauswahlJeGewerkTests` Investitionsliste WP mit EUR_PRO_KW_ELEKTRISCH,
`PruefeFehlende` je Raster; `BetriebskostenBaugroesseTests` neuer Fall „passt nur im Investitionsraster".
Prüfungen: Kern-Filter Release 0, Windows-Schale x64 0; Designer +0; SQL-Prüfer 1.923/0; gefiltert 181/181 (10 Klassen); voller Lauf 14.042 / 0 /
2 (Kern 6.837+1, UI 6.243, KiKern 549, SpeicherEngine 386, SpeicherPlanung 27+1); Testhost-Regel eingehalten; Referenzlauf 13/13 gegen R14 PASS,
4.207.049 Werte, 394/394 byte-gleich; Anker unberührt; keine Zeile WP × EUR_PRO_KW_ELEKTRISCH in der Testdatenbank.
A/B 1024 (Arbeitskopie, Anlage 11262, Satz 1.000 €/kW; IDs 202000001 Kat. 1, 202000002 Kat. 2): Kaskade Kat. 1 null/0,00 € → 4,00 kW/4.000,00 €
Herkunft ANLAGE; Investitionssumme 12.001,00 → 16.001,00 €; Dialog Kat. 1 Grund GEWERK → Herleitung „11,60 kW ÷ COP 2,90 (A2/W35) = 4,00 kW";
Kat. 2 unverändert 0,00 €/GEWERK; Auswahl WP Investition nein → ja, Betrieb nein → nein.
Erledigt-Gründe: § 6.3 Nr. 10 Anwenderregel umgesetzt (Investition je kW thermisch und je kW elektrisch am Normpunkt; Betrieb thermisch); Rest
E20‑Q6 (Betriebsraster bietet „je kWh elektrisch", Anwenderfrage). Nachzug Konzept § 3.2 Tafel: Zeile „EUR_PRO_KW_ELEKTRISCH an der WP (nur
Kategorie 1) | Σ Ptherm ÷ COP am Normpunkt × Satz | Tab_Kenndaten bei W35 (A2/B0/W10 je Tab_WP.Typ, interpoliert)", Satz „Art ↔ Gewerk gekreuzt
geprüft" + „je Raster". Wiki Kosten: Tafelzeile „Wärmepumpe | je kW elektrisch (nur Investitionskosten) | elektrische Leistungsaufnahme am
Normpunkt (A2/W35, B0/W35, W10/W35)", Absatz :58 „auf der Betriebskostenseite nicht an der Wärmepumpe". Logbuchsatz (`kosten`): „Die
Investitionskosten der Wärmepumpe lassen sich auch je kW elektrisch bemessen; Bezugsgröße ist die elektrische Leistungsaufnahme am Normpunkt der
Kennlinie." Abnahme A‑E20‑1: (1) 1024 Kostenverwaltung WP Investition: „je kW elektrisch" wählbar, 1.000 €/kW → 4.000,00 €, Herleitung
„P_el der Anlage · 11,60 kW ÷ COP 2,90 (A2/W35) = 4,00 kW"; (2) Betriebskosten: nicht in der Auswahl; (3) 1017 (S/W, Kühlbetrieb) „35,60 kW ÷ COP
4,50 (B0/W35) = 7,91 kW"; (4) Englisch; (5) nach Speichern/Öffnen unverändert, Kapitalwert enthält 4.000 €.
