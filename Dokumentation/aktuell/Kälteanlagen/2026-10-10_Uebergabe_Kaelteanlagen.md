# Übergabe Kälteanlagen — Sitzung Gebäudesimulation an Sitzung Kälteanlagen (10.10.2026)

Übergabe, kein Regelpapier: Regeln stehen in [`CLAUDE.md`](../../../CLAUDE.md), der dauerhafte Stand in der
[Statusdatei iOS-Migration](../Status_iOS_Migration.md) und der [Statusdatei Gebäudesimulation](../Status_Gebaeudesimulation_VDI6007.md).

## 1. Kopf

| Was | Wert |
|---|---|
| Datum | 10.10.2026 |
| Von / an | Sitzung Gebäudesimulation / Sitzung Kälteanlagen |
| Stand | `origin/ios_migration_september` auf `c7796e016` |
| Arbeitszweig | `ios_migration_september` |
| Geht an Kälteanlagen | Kältemaschine, Kältespeicher, Kühlkurve, Kälteseite der Kopplung, Typkennfelder, Teillast und Takten, Rückkühlung, freie Kühlung, Kältemittel, Wiki-Seiten Kühlung |
| Bleibt bei Gebäudesimulation | Gebäude (VDI 6007, Zonen, Konditionierung), Anlagenkopplung AK1–AK3 als Kreis, Übergabegrenze und Bivalenz (UB) |
| Schnittstelle | Kälteschranke auf Stufe AK3 (Projekte 1058, 1059): Änderungen an der Kälteseite der Kopplung werden mit der Sitzung Gebäudesimulation abgestimmt |

## 2. Gebauter Stand Kälte

| Welle | Inhalt | Statuszeile | Entscheid | Schemaschritt | Referenzprojekt / Basis |
|---|---|---|---|---|---|
| KU3-1 | Kältemaschine als Erzeugertyp: Katalog, Register, Verwaltungsdialog | #713 | E67, E68 | 182 | – |
| KU3-2 | Rechenweg: Rückkühlung, Verdichter, freie Kühlung, Kaskade nach den Wärmepumpen | #714 | E67, E68 | – | – |
| KU3-3 | Kühlung je Zone: Vererbung, Zonendialog, Kältebedarf je Zone im Bericht | #715 | E67, E68 | – | – |
| KU3-4a | Kältemaschine als Anlage (Typ 13, Anzahl), Wirtschaftlichkeit, Ergebnis, Bericht | #716 | E67, E68 | 183 | – |
| KU3-4c | Erzeugerdialog „Kältemaschinen“, Hülle, KI-Sicht | #717 | E67, E68 | – | – |
| KU3-5 | Kältespeicher: Pufferverwendung `Kaelte`, Lade- und Entladeweg im Kältekreis | #718 | E68 | – | – |
| KU3-4d | Kältestromabrechnung, Stempeltrigger, Kältespeicher in Bericht und Navigator | #721 | E67, E68 | 184 | – |
| MZ-Rest | Kältespitze je Zone, Trenndecke | #722 | E67, E68 | 185 | – |
| KU3-4b | Referenzprojekt Kältemaschine mit Kältespeicher | #728 | E67, E68 | – | 1055 / R36 |
| KU3-6 | Freie Kühlung über die Wärmequelle der Wärmepumpe | #735 | E75 | 187 | – |
| AK3-K | Zonensperre und Kälteseite im Kreis | #817 | E103, E104 | 201 `Ak3KSchema` | 1059 / R43 |
| KK | Raumgeführte Kühlkurve auf AK3, Kühlübergabe je Zone ab AK1 | #827 | E105–E107 | 202 `KuehlkurveSchema` | 1061, 1062 / R44 |
| KM1 | Importart „Kältemaschine“ (Copper-Kurven, CSV-Kennfeldvorlage), 34 eingebaute Typkennfelder | #841 | – | – | – |
| KM2 | Typkennfelder in jeder Datenbank | #848 | – | 203 `KaeltemaschinenTypkennfelderSchema` | – |
| KM3-E1 | Teillast- und Taktdaten im Katalog, Import der Teillastkurve | #876 | E116 | 210 `KaeltemaschineTeillastSchema` | – |
| KM3-E2 | Rechenweg Teillast und Takten | #877 | E116 | – | 1063 / R49 |
| KM3-E3 | Gruppe „Teillast und Takten“ im Katalogdialog, Lesewerte, Kachelzeile, Kennzahlen `kaelte.km.*`, Tafel, Katalogfassung 18 | #878 | E116 | – | – |
| KM3-E4 | Wiki-Quellen, Logbuch-Entwurf, Konzepte „wie gebaut“ nach `ueberholt/` | #879 | E116 | – | R49 unverändert |

Weitere Kältezeilen der Statusdatei (Oberfläche und Nachzüge, jeweils ohne eigenen Rechenweg): #724 (Kältegang nach Bedarfsrechnung), #758 und #777 (Kachel „Kühlung und Kälteanlagen“), #852 (Kältemaschine als Stromverbraucher der Wirtschaftlichkeit), #881 (Katalogdialog Kältemaschinen, Kachel bei leerem Katalog), #882 (Langtext am Spaltenkopf), #884 (Kachel „Kühlung“ unter Windows), #892 und #902 (Anlagenschema mit Bahn „Kälte“, Doppelklick), #898 (Dialog Wärmequelle Erdreich), #901 (Wiki-Einzelsätze).

**Papiere**

| Papier | Pfad |
|---|---|
| Recherche (Stufen 1–7) | [`2026-10-08_Recherche_Kaelteanlagen_Herstellerdaten_Rechenmodelle.md`](2026-10-08_Recherche_Kaelteanlagen_Herstellerdaten_Rechenmodelle.md) |
| Umsetzung KM1 | [`2026-10-09_Umsetzung_KM1_Typkennfelder.md`](2026-10-09_Umsetzung_KM1_Typkennfelder.md) |
| Fachkonzept KM3 (mit „Umsetzung — wie gebaut“) | [`Konzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md`](../../ueberholt/Konzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md) |
| Umsetzungskonzept KM3 (mit „Umsetzung — wie gebaut“) | [`Umsetzungskonzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md`](../../ueberholt/Umsetzungskonzept_Kaeltemaschine_Teillast_Takten_EPOS-Plan.md) |
| Protokolle KM3 | [E1](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-09_KM3-E1_Schema_Katalog_Import.md), [E2](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-09_KM3-E2_Rechenweg_1063_R49.md), [E3](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-09_KM3-E3_Dialoge_Bericht_Vorlagen.md), [E4](../../ueberholt/Protokolle/Gebaeudesimulation/2026-10-10_KM3-E4_Wiki_Logbuch_Konzepte.md) |
| Wiki-Quellen | `Projekte/Wiki/Programm Dokumentation - Kühlung.wiki`, `Projekte/Wiki/Grundlagen - Kühlung.wiki` (je Abschnitt „Teillast und Takten der Kältemaschine“) |

## 3. Code-Landkarte Kälte

| Bereich | Dateien |
|---|---|
| Schema (`EPOS.Kern/Allgemein/Update/`) | `KuehlungSchema.cs`, `KaeltemaschineSchema.cs`, `KaeltemaschineAnlageSchema.cs`, `KaeltestromabrechnungSchema.cs`, `ZonenKaeltespitzeSchema.cs`, `FreieKuehlungSoleSchema.cs`, `Ak3KSchema.cs`, `KuehlkurveSchema.cs`, `KaeltemaschinenTypkennfelderSchema.cs`, `KaeltemaschineTeillastSchema.cs`, `KuehluebergabeSchema.cs` |
| Kern Simulation Kälte | `EPOS.Kern/Allgemein/Simulation/Kaelte/KaelteFestwerte.cs`, `Kaeltemaschine.cs`, `KaeltemaschinenRand.cs`, `Kaeltemaschinenteillast.cs`; `EPOS.Kern/Allgemein/Simulation/Kaelteangebot.cs`, `Kaeltekaskade.cs`, `SimulationControl.Kaelte.cs`, `SimulationKaeltebedarf.cs`, `Kuehlkennlinie.cs`, `KuehlRaumeinfluss.cs`, `Waermepumpentakt.cs`, `Ak3Kernstufe.cs` |
| Kern Gebäudeseite der Kühlung | `EPOS.Kern/Allgemein/Simulation/Gebaeude/Kuehlkurve.cs`, `Kuehluebergabe.cs`, `Zonenkuehluebergabevorgaben.cs`, `GebaeudeModellEingang.Kuehlkurve.cs`, `GebaeudeModellEingang.Zonenkuehlung.cs` |
| Import und Katalog | `EPOS.Kern/Allgemein/Import/KaeltemaschineImportLeser.cs`, `KaeltemaschineTeillastkurve.cs`, `KaeltemaschinenKennfeld.cs`; `EPOS.Kern/Allgemein/Katalog/KaeltemaschinenTypkennfelder.cs`, `KaeltemaschinenTypkennfelder.json`; `Quellen/Kaeltemaschine_Kennfeldvorlage.csv`, `Quellen/LIZENZ_Kaeltemaschinen_Typkennfelder.txt` |
| Controller, Modell, Wirtschaftlichkeit | `EPOS.Kern/Controller/KaeltemaschineStammCtrl.cs`, `KaeltemaschineAnlageCtrl.cs`, `KaeltemaschineTeillastDialogrechnung.cs`, `KaeltemaschineParameteruebersicht.cs`, `KenndatenKuehlungCtrl.cs`, `SimulationErgebnisCtrl.cs`; `EPOS.Kern/Model/KaeltemaschineModel.cs`, `KenndatenKuehlungModel.cs`, `KuehlkurveKennzahlen.cs`, `Ak3KKennzahlen.cs`; `EPOS.Kern/Allgemein/Wirtschaftlichkeit/Kaeltestromabrechnung.cs` |
| Bericht | `EPOS.Kern/Allgemein/Bericht/KaeltemaschineTeillastBerichtswerte.cs`, `KaelteProduktionBild.cs`; Tafel `BER_TAFEL_KM_TEILLAST` und Kennzahlen `kaelte.km.*` in `EPOS.Kern/MyResource/Resource.resx`; Katalogfassung 18 in `EPOS.Kern/Allgemein/Bericht/Vorlagen/Vorlagenfeldkatalog.cs` |
| Oberfläche | `EPOS.UI/Dialoge/Erzeuger/KaeltemaschineKatalogDialog.razor`, `KaeltemaschineKatalogDaten.cs`, `KaeltemaschineKatalogTexte.cs`, `KaeltemaschineKatalogKiSicht.cs`, `KaeltemaschineAnlageDialog.razor`, `KaeltemaschineAnlageDaten.cs`, `KaeltemaschineAnlageTexte.cs`, `KaeltemaschineAnlageKiSicht.cs`; `EPOS.UI/Seiten/Simulation/KaeltegangReiter.razor`; `EPOS.UI/Seiten/Start/KuehlungKachelDaten.cs`; `EPOS.UI/Dialoge/Bedarf/GebaeudeKuehluebergabeFelder.razor`, `KuehluebergabeTexte.cs`; `EPOS.UI/Dialoge/Waermepumpe/WaermepumpeKuehlKiWege.cs` |
| Hüllen | `EPOS.UI.Daten/Erzeuger/KaeltemaschineKatalogHuelle.cs`, `KaeltemaschineAnlageHuelle.cs`, `KaeltemaschineKennlinienbild.cs`, `KaeltemaschineTeillastbild.cs`, `KuehlungKachelBau.cs`; `EPOS.UI.Daten/Simulation/SimulationErgebnisHuelle.Anzeige.cs` (Kältekachel), `WaermepumpeKuehlGabenBau.cs`; `WindowsFormsApplication1/Views/Kaeltemaschine/KaeltemaschineKatalogFensterHuelle.cs` |
| Tests Kern (Auswahl nach Namen `*Kaelte*`, `*Kuehl*`, `*Teillast*`) | `EPOS.Kern.Tests/Kaeltemaschine*Tests.cs` (u. a. `KaeltemaschineTeillastTests.cs`, `KaeltemaschineTeillastLaufTests.cs`, `KaeltemaschineTeillastkurveTests.cs`, `KaeltemaschineTeillastSchemaTests.cs`, `KaeltemaschineTeillastDialogrechnungTests.cs`, `KaeltemaschineTeillastBerichtTests.cs`, `KaeltemaschinenTypkennfelderSchemaTests.cs`), `Kaeltespeicher*Tests.cs`, `Kuehlkurve*Tests.cs`, `Ak3Kaelte*Tests.cs`, `Ak3K*Tests.cs`, `AnlagenkopplungKaelte*Tests.cs`, `FreieKuehlungSole*Tests.cs`, `Kuehlung*Tests.cs`, `KaeltegangLaufTests.cs`, `KaeltebedarfTests.cs` |
| Tests Oberfläche | `EPOS.UI.Tests/Dialoge/KaeltemaschineKatalogDialogTests.cs`, `KaeltemaschineKatalogTeillastTests.cs`, `KaeltemaschineKatalogImportKnopfTests.cs`, `KaeltemaschineAnlageDialogTests.cs`; `EPOS.UI.Tests/Seiten/ErzeugerReiterKuehlungTests.cs`, `SimulationKonfigKaeltebahnTests.cs`; `EPOS.UI.Tests/Bausteine/SchemaKaeltebahnTests.cs` |
| Referenzprojekte 1017, 1047, 1055, 1058, 1059, 1061, 1062, 1063 | Skripte `Referenzlaeufe/Skripte/kaelteerzeuger_1017_referenzprojekt.py`, `kuehlung_1017_referenzprojekt.py`, `anlagenkopplung_1047_referenzprojekt.py`, `referenzprojekt_1055_kaeltemaschine.py`, `referenzprojekt_1058_ak3.py`, `referenzprojekt_1059_ak3k.py`, `referenzprojekt_1061_kk.py`, `referenzprojekt_1062_kkz.cs`, `referenzprojekt_1063_kaeltemaschine_teillast.cs`; Wachen `EPOS.Kern.Tests/KaeltemaschineReferenzprojektWacheTests.cs`, `KaeltemaschineTeillastReferenzprojektWacheTests.cs`, `Ak3KReferenzprojektWacheTests.cs`, `KuehlkurveReferenzprojektWacheTests.cs`, `ZonenKuehlkurveReferenzprojektWacheTests.cs`, `ReferenzprojektKaelteerzeugerTests.cs` |
| Einfrierregeln | `CLAUDE.md`, Abschnitt „Regressionsnetz“: gesäte Kältedaten, Kältemaschinendaten, Kühlkurvendaten, Teillastdaten |

## 4. Fachliche Festlegungen, die gelten

| Festlegung | Inhalt |
|---|---|
| Takten | Die Mindestteillast löst das Takten aus (Nennleistung × Mindestteillast); der Mehrstrom läuft über `Waermepumpentakt` mit C_d, Vorgabe 0,9. |
| „Kurve gültig ab Lastgrad“ | `Teillastkurve_Lastgrad_Min` ist nur die Gültigkeitsgrenze der Teillastkurve; leer heißt Mindestteillast. |
| Plausibilität | 0,3 ≤ x/E(x) ≤ 2,0 ab max(x_u; 0,1). |
| Kennfeldrand | `RANDWERT` (Vorgabe) oder `GUETEGRAD` (Carnot-Gütegrad, höchstens 10 K über den Rand, Deckel 15). |
| Folgeschaltung | n = ⌈Last/Q_av⌉. |
| Taktstunden | Genau ein Taktstunden-Zähler. |
| Vorgabekurven | Je Verdichterregelung aus den Typkennfeldern. |
| Einheit der Kachel | Der Taktstrom kommt im Kern in kWh (`SimulationErgebnisCtrl.KaelterzeugerZeile.TaktstromKwh`); Regel W8‑O‑5c: kein Faktor 1000 in den Hüllen. |
| Dialogbegriffe | Gruppe „Teillast und Takten“; Feld „Teillastrechnung“ (wie bisher / linear / Kurve); „Kennfeldrand“ (Randwert / Gütegrad); „Taktverlustfaktor C_d“; Kachel „Teillast und Takten der Kältemaschinen“. |
| Wiki | Die Typkennfelder heißen nur „Typkennfeld je Verdichterregelung“ (keine Katalognamen; `WikiProduktdatenWacheTests`). |

## 5. Offene Punkte, die übergehen

| Punkt | Inhalt | Wer |
|---|---|---|
| (a) Sichtabnahme KM3 unter Windows | Katalogdialog Gruppe „Teillast und Takten“ mit „Kurve aus Typkennfeld…“ und „Teillastpunkte prüfen…“; Lesewerte und Folgeschaltungshinweis im Anlagendialog; Kachel im Reiter Kältegang; Berichtstafel; Projekt 1063. Befunde gehen an die Sitzung Kälteanlagen. | Anwender, dann Sitzung Kälteanlagen |
| (b) Wiki und Logbuch | Die Seiten Kühlung und Grundlagen Kühlung sind am 10.10.2026 hochgeladen (#889, Revisionen 736 und 754); die zwei Logbuchsätze stehen in `Dokumentation/aktuell/Wiki_Update_2026-09-26.md` im Block „Version offen (Vorschlag 1.2.1)“ und sind veröffentlicht, die Versionsnummer ist beim Anwender zu bestätigen. Wortlaut: „Seit 10.10.2026: Die Kältemaschine rechnet auf Wunsch ihr Teillastverhalten und das Takten bei kleiner Last; Katalogdialog und Anlagendialog führen die zugehörigen Felder. (#879)“ und „Seit 10.10.2026: Der Reiter „Kältegang“ der Simulationsergebnisse zeigt die Kachel „Teillast und Takten der Kältemaschinen“ mit Taktstrom, Starts, Teillastanteil, mittlerem Lastgrad und Jahres-EER ohne Hilfsstrom, der Bericht eine Tafel dazu. (#879)“ | Anwender (Version) |
| (c) Weitere Stufen der Recherche | Stufe 3 (Rückkühlung parametrieren, Teil-Freikühlung), Stufe 4 (Kältemittel als Stammdatum), Stufen 5–7 (Split/Multisplit, Kühlkennfelder der Wärmepumpe, R744/passive Erdkühlung/VRF): Abschnitte „Empfehlung für EPOS-Plan“ der [Recherche](2026-10-08_Recherche_Kaelteanlagen_Herstellerdaten_Rechenmodelle.md). | Sitzung Kälteanlagen |
| (d) Angemeldete Kopfzeilen | Siehe unten. | Sitzung Kälteanlagen |

**Kopfzeilen in `Status_iOS_Migration.md`** (Wortlaut):

- Referenzbasis: „R51 — angemeldet 10.10.2026 17:40 UTC für FK Referenzprojekt 1064 freie Kühlung (Sitzung Gebäudesimulation)“
- Schemaschritt: „211 — Sitzung Gebäudesimulation, Welle K-A Katalogfelder der Kälteerzeuger (Geräteart, GWP, saisonale Kennzahl; E118), 10.10.2026 18:38 UTC“ (hängt an 210)
- Schemaschritt: „212 — Sitzung Gebäudesimulation, Welle KB-D pflegbare Kältefolge `Kaelte_Rang` (E117), 10.10.2026 18:38 UTC“ (hängt an 211)

Sie stammen nach Gegenstand von der Sitzung Kälteanlagen. Bitte: diese Anmeldungen und künftige Statuszeilen unter dem Sitzungsnamen „Sitzung Kälteanlagen“ führen, damit sich beide Sitzungen in Kopf- und Statuszeilen unterscheiden.

## 6. Arbeitsregeln und Lehren der KM3-Welle

- Eigener Sitzungszweig für den Kern-Lauf (Muster `claude/<sitzung>`); Push auf `ios_migration_september` erst nach grünem Lauf.
- CI-Vermerk als eigener Commit mit `[skip ci]`; „[skip ci]“ nie in Merge-Rümpfen zitieren.
- Statusnummern, Entscheidnummern (zuletzt E118 vergeben; messen mit `git grep -o -E '\bE1[0-9]{2}\b'`) und Schemaschritt (213 frei) unmittelbar vor Merge und Push gegen origin messen.
- Schemaschritt vor dem Bau per Kopfzeilen-Commit anmelden; Kette `SCHRITT = <Vorgänger>.SCHRITT + 1`; Lückentests.
- Eine Rechenwegänderung an der Kälte friert die Basis neu ein (Einfrierregeln Kältedaten, Kältemaschinendaten, Kühlkurvendaten, Teillastdaten); aktuelle Basis R50, R51 angemeldet.
- Referenzlauf plattformfrei mit `EPOS.Referenzlauf`.
- Testdatenbank liegt in LFS (`git lfs fetch` und `git lfs checkout`; ein Zeiger hat rund 130 Byte).
- Tests laufen unter en-US; deutsche Texte mit `Kulturvorrichtung` pinnen.
- Die Windows-Messliste der ChartProben ist eine lokale Datei auf dem Anwenderrechner, nicht die versionierte Messlatte `Proben/ChartProben/Messlatte_2026-10-10.sha256`.
- Keine Normwerte und keine Normtexte im Repositorium; keine Herstellerdaten im Wiki.

## 7. Übergebene Scratch-Stände

Keine. Alle Stände sind auf origin; Worktrees und Hilfszweige der Sitzung Gebäudesimulation sind entfernt.
