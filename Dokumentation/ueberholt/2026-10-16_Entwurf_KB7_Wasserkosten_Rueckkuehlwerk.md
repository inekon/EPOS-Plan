# KB-7 — Entwurf: Wasserkosten des Rückkühlwerks in der Kostenbasis

Sitzung Dialoge und Korrekturen (Kostenbasis), Entwurf vom 16.10.2026. Schemaschritt **235** (angemeldet, hängt an
234 KBP-5). Nur Entwurf — kein Code, keine Schemaänderung. Bezüge: Entwurf Rückkühlwerk
[`2026-10-10_Entwurf_Split_VRF_Rueckkuehlwerk.md`](../aktuell/Kälteanlagen/2026-10-10_Entwurf_Split_VRF_Rueckkuehlwerk.md)
(Frage KD‑Q8), Protokolle
[`K-F1`](Protokolle/Kaelteanlagen/2026-10-10_K-F1_Rueckkuehlwerk_Glied.md) und
[`K-F2`](Protokolle/Kaelteanlagen/2026-10-11_K-F2_Rueckkuehlwerk_Rechenweg.md), Übergabe
[`KB-4/KB-5`](../aktuell/Dialoge/2026-10-14_Uebergabe_Dialoge_Kostenbasis_KB4_KB5.md).

## 1 Ausgangslage

**Preisfeld `Tab_Energieanlagen.Wasserpreis_EUR_m3`** (Schritt 214, `REAL`, `CHECK ≥ 0`, leer = keine Kosten) —
Fundstellen in 9 Quelltextdateien samt 3 Ressourcendateien, 8 Testdateien und 3 Dokumenten:

| Ort | Datei:Zeile | Rolle |
|---|---|---|
| Schema 214 | `EPOS.Kern/Allgemein/Update/RueckkuehlwerkSchema.cs:16, 63–64, 189–193, 260–266` | Spalte, CHECK, Nachweiszählung |
| Migration, Stand | `WindowsFormsApplication1/Allgemein/Update/SchemaMigration.cs:5414, 8067`; `EPOS.Kern/Allgemein/Update/SchemaStand.cs:1017` | Schrittaufruf und Beschreibung |
| Werkzeug | `Werkzeuge/Testdatenbankschema/Program.cs:3409` | Testdatenbank-DDL |
| Paketanhebung | `EPOS.Kern/Allgemein/Update/Paketanhebung.cs:416–418` | Stufe 214: Paket kommt ohne Preis an |
| Controller | `EPOS.Kern/Controller/KaeltemaschineAnlageCtrl.cs:50–53, 72, 196, 251–260, 328` | Lesen in die Anlagenliste, `WasserpreisSetzen` (einziger Schreiber; **kein Aufrufer außer Tests**) |
| Rechenkern | `EPOS.Kern/Allgemein/Simulation/Kaelte/Rueckkuehlwerk.cs:159–160, 488–489` | Feld `WasserpreisEurM3`, `WasserkostenEur(Menge, Preis)` |
| Lauf | `EPOS.Kern/Allgemein/Simulation/SimulationControl.Kaelte.cs:546, 591` | Preis an das Glied; Kosten nur im **Hinweis** |
| Ressourcen | `EPOS.Kern/MyResource/Resource.resx:28806, 28809` (+ `en-US`, Designer) | `SIMENG_KAELTE_RKW_WASSER_KOSTEN` („nicht in den Betriebskosten“), `…_OHNE_PREIS` |
| Tests | `RueckkuehlwerkSchemaTests.cs:63, 109–110, 185, 236–246, 299, 310`; `RueckkuehlwerkRechenwegTests.cs:366–370`; `RueckkuehlwerkGleichwertigkeitTests.cs:191`; `FachspaltenEinordnungWacheTests.cs:50–51`; Spaltenzählung `AnlagenfahrplanSchemaTests.cs:149`, `ErdsondenfeldSchemaTests.cs:96`, `FreieKuehlungSoleSchemaTests.cs:157`; `TestDatenbank.cs:1027` | Spalte, Kopie, Setzen, Rechnung |
| Doku | Entwurf Rückkühlwerk Abschnitt Wasserbilanz und KD‑Q8; `Referenzlaeufe/LIESMICH.md:1289`; Protokoll K‑F1 | Beschreibung |

**Wassermenge** (`Tab_ErgebnisKaeltemaschine.Wasser_m3`, bleibt):

| Ort | Datei:Zeile |
|---|---|
| Schema, Modell | `RueckkuehlwerkSchema.cs:136`; `EPOS.Kern/Model/ErgebnisModel.cs:767` |
| Lauf schreibt | `EPOS.Kern/Allgemein/Simulation/SimulationRunner.cs:793` (nur mit Rückkühlwerk) |
| Ergebnis lesen/schreiben | `EPOS.Kern/Controller/ErgebnisCtrl.cs:437, 1308` |
| Szenario skaliert | `EPOS.Kern/Allgemein/Wirtschaftlichkeit/SzenarioMengen.cs:196–198` |
| Kennzahl, Bericht | `EPOS.Kern/Allgemein/Bericht/KennzahlenKatalog.cs:337, 876` (`kaelte.rk.wasser_m3`); Vorlagenfelder Fassung 20 (`Vorlagenfeldkatalog.Ergebnisse.cs:46`) |

**Nicht betroffen:** Anlagendialog und Hüllen (kein Wasserpreisfeld — die Oberfläche des Rückkühlwerks ist K‑F5 und
offen), KI-Feldkatalog, Wiki. Kopie und Paket tragen die Spalte als gewöhnliche Anlagenspalte mit
(`ProjektDuplizierenCtrl`, `ProjektExportImportCtrl`).

**Träger und Preise heute:**

| Gegenstand | Befund |
|---|---|
| Katalog `energy_carrier` | ohne Projektbezug; `billing_unit` im Bestand nur `kWh`, `Nm³`, `L`, `kg` (27 Träger); **kein Träger „Wasser“**, keine Abrechnungseinheit `m³` (`DbWerte.EINHEIT_KUBIKMETER` ist nach `GaseNormkubikmeter` keine Gas-Einheit mehr, nur `from_unit` des z‑Faktors) |
| Projektebene | `energy_project_settings.custom_price_work/base/power` je `ID_Projekt` und Träger, darunter `energy_carrier.price_*` (`KostenEmissionRechner.cs:1867–1890`), dann Szenariopreise (`*_best`, `*_worst`) |
| Preiskarte | `EnergietraegerPreiskarte`: Arbeitspreis gespeichert je Abrechnungseinheit; ein Träger ohne Heizwert rechnet ohne Division (`mitHeizwert = false`) |
| Preis je Szenario | `KostenEmissionRechner.PreisSatz(idProjekt, carrierId, szenario, …)` (`:2100`) liefert den wirksamen Arbeitspreis **je Abrechnungseinheit** — direkt nutzbar für €/m³ |
| Energiekosten | `KostenEmissionRechner` bucht Verbrauch je `carrier_id` in **MWh** und leitet CO₂ und BEHG ab; unbekannte Träger gelten dort als abgabepflichtig — Wasser darf diesen Weg **nicht** nehmen |
| Betriebskosten | `WirtschaftlichkeitCtrl.LiesBetriebskostenTopfe` (`:7747`) → `BetriebSofort` (Steigerung p_B) und `EndenergieSofort` (p_E); Nachweisliste `e.Betriebskosten` (`:2362`); `EndenergieAufloeser` holt Mengen frisch aus dem Lauf |

## 2 Zielbild

| Entscheid | Wirkung |
|---|---|
| 16.10.2026 | Wasser ist Träger der Trägerkarte mit Preis je m³; Lauf-Menge × Preis = Betriebskosten mit Wirkung im Kapitalwert; `Wasserpreis_EUR_m3` entfällt, Preise wandern in die Karte |
| 13.10.2026 „eine Kostenbasis“ | Preis nur in Energiekosten-Dialog/Trägerkarte, keine zweite Haltung an der Anlage |
| E120 / KD‑Q8 (10.10.2026) | Menge **und** Kosten; ohne Preis Menge ohne Kosten (benannt) |
| KB-4/KB-5 | `Tab_Rueckkuehlwerk.Modulkosten` bleibt |

## 3 Schema 235 (`WasserpreisTraegerkarteSchema`, `SCHRITT = BrennstoffpreiseTraegerkarteSchema`-Muster, Kette `KBP-5.SCHRITT + 1`)

| Teil | Inhalt |
|---|---|
| a Träger | Fehlt ein Träger mit `pricing_model = 'WATER'`, wird er angelegt: `name`/`code`/`group_code` „Wasser“, `billing_unit` `m³`, `hi`/`hs` leer, Preise 0, Emissionen 0, `ID_Brennstoff` 0, `is_active` 1. Erkennung über `pricing_model`, nie über den Namen (idempotent) |
| b Preisübernahme | Je `ID_Projekt` mit `Wasserpreis_EUR_m3 IS NOT NULL` an einer Kältemaschinen-Anlage: `energy_project_settings`-Zeile (Projekt, Wasserträger) anlegen oder ergänzen, `custom_price_work` = Preis, nur wenn dort leer/0 (gepflegter Wert bleibt). Katalogpreis des Trägers bleibt 0 (Saat bewegt nie ein Ergebnis — Muster Schritt 227) |
| c Mehrere Preise | Tragen Anlagen eines Projekts **verschiedene** Preise: Vorschlag — der Preis der Anlage mit der kleinsten `ID` gewinnt, Migrationshinweis nennt Projekt, Anlagen und verworfene Preise (Frage F2) |
| d `DROP COLUMN` | `Tab_Energieanlagen.Wasserpreis_EUR_m3` (kein Index, kein FK, nur Spalten-CHECK → Prüfung vor dem Drop wie Schritt 227; sonst Tabellenneubau). Schritt 214 legt die Spalte nicht wieder an, wenn 235 gelaufen ist (Muster 175/227) |
| e Register | `SchemaStand` 235, `SchemaMigration`-Aufruf, `Werkzeuge/Testdatenbankschema`, `TestDatenbank`; `RueckkuehlwerkSchema.SPALTEN_ANLAGE` nur noch `ID_Rueckkuehlwerk` für Neubauten; `FachspaltenEinordnungWacheTests` angleichen |
| f Katalogfassung | `energy_carrier` steht in keinem Prüfsummen-Register → keine neue Fassung allein dafür; die Auslieferungsvorlage trägt den Wasserträger mit |
| g Projektpaket | Paket ordnet Träger über `name` zu (`ProjektExportImportCtrl.cs:113`) → der Wasserträger muss im Ziel existieren (Teil a). `Paketanhebung`-Stufe 235: ein älteres Paket mit `Wasserpreis_EUR_m3` übergibt den Preis an die `energy_project_settings` des importierten Projekts (Regel c), dann entfällt die Spalte |
| h Offen()-Zählung | Anlagen mit Spalte vorhanden = offen; Nachweis nach dem Lauf 0 |

## 4 Rechenweg

| Schritt | Regel |
|---|---|
| Menge | Σ `Wasser_m3` der Kältemaschinen-Ergebnisse des Laufs (szenarioskaliert über `SzenarioMengen`) |
| Preis | `KostenEmissionRechner.PreisSatz(idProjekt, WasserTraeger(idProjekt), szenario)` → Arbeitspreis €/m³; neu `WasserTraeger(idProjekt)` analog `StromTraeger` (Projektzuordnung, sonst Katalogträger `WATER`) |
| Kosten | Menge × Preis [€/a] → **Betriebs-Topf** (`BetriebSofort`, Steigerung p_B; Frage F3) und eine Nachweiszeile in `e.Betriebskosten` („Wasser Rückkühlwerk: Menge × Preis“) |
| Genau eine Buchung | nicht in `KostenEmissionRechner` (Energiekosten, CO₂, BEHG), nicht in `EndenergieAufloeser`, nicht in Kohärenzprüfung „Träger mit Verbrauch“; Wasserträger aus `EnergietraegerZulaessigkeit` und `ProjektEnergietraegerCtrl.Verwendete` heraushalten |
| Ohne Preis | Menge > 0 und Preis leer/0: keine Kosten, Hinweis der Wirtschaftlichkeit nennt die Menge (KD‑Q8 „benannt“) |
| Lauf | Simulation liest **keinen** Preis mehr: `Rueckkuehlwerk.WasserpreisEurM3` und `WasserkostenEur` entfallen; ein Hinweis `SIMENG_KAELTE_RKW_WASSER` nennt Menge und Nassstunden („Kosten in der Wirtschaftlichkeit“), beide Sprachen; die zwei alten Schlüssel entfallen, Designer neu ziehen |

## 5 Oberfläche und Bericht

| Ort | Änderung |
|---|---|
| Anlage | nichts zu entfernen (Feld nie gebaut); K‑F5 baut **kein** Preisfeld |
| Energiekosten-Dialog/Trägerkarte | Wasserträger erscheint mit Arbeitspreis €/m³ und Szenariopreisen; ohne Heizwert, ohne Leistungspreis, ohne Emissionsfelder; Preisbasis-Liste nur `m³` |
| Wirtschaftlichkeit | Zeile „Wasser Rückkühlwerk“ unter Betriebskosten (Menge, Preis, €/a) |
| Bericht | Kennzahl `kaelte.rk.wasser_m3` bleibt; neue Kennzahl Wasserkosten €/a aus der Wirtschaftlichkeit (Vorlagenfeld, neue Fassung) — nur wenn F4 = ja |

## 6 Wiki und Hilfe

Fachseiten Energiekosten/Trägerkarte: Träger „Wasser“, Einheit m³, Wirkung als Betriebskosten. Wirtschaftlichkeitsseite:
Zeile Wasserkosten. Logbuch-Entwurf (ein Satz): „Die Wasserkosten des Rückkühlwerks werden mit dem Preis des Trägers
Wasser als Betriebskosten gerechnet.“ Upload gebündelt.

## 7 Tests und Referenzprojekte

| Prüfung | Inhalt |
|---|---|
| Schema 235 | Träger angelegt (einmal), Preisübernahme, Regel c mit Hinweis, gepflegter Kartenpreis bleibt, Spalte fort, zweiter Lauf ohne Wirkung, 214 nach 235 legt nichts an |
| Paket | Altpaket mit Preis → Kartenpreis im Ziel |
| Wirtschaftlichkeit | Menge × Preis im Betriebs-Topf, Kapitalwert ändert sich um den Barwert; ohne Preis 0 + Hinweis; Szenariopreise; Energiekosten und CO₂ unverändert |
| Wachen | Spaltenzählungen der drei Schema-Tests, `FachspaltenEinordnungWacheTests`, SQL-Dialekt-Prüfer |

**Befund Testdatenbank:** 0 Anlagenzeilen mit `ID_Rueckkuehlwerk` oder `Wasserpreis_EUR_m3`, `Tab_Rueckkuehlwerk(_STAMM)`
leer, `Wasser_m3` in keiner Ergebniszeile. Kein Referenzprojekt rechnet ein Rückkühlwerk → Kapitalwert der
Referenzprojekte unverändert, **keine neue Basis (R56) nötig**; der Referenzlauf bleibt der Nachweis. Der neue
Wasserträger ist eine Katalogzeile ohne Zuordnung zu einem Referenzprojekt und berührt keine Einfrierregel.

## 8 Etappen

| Etappe | Inhalt | Werkzeugaufrufe | Modell |
|---|---|---|---|
| KB-7a | Schema 235 samt Paketanhebung, Register, Werkzeug, Testdatenbank, Schematests | ≈ 100 | `opus` |
| KB-7b | Rechenweg: `WasserTraeger`, Buchung im Betriebs-Topf, Nachweiszeile, Abgrenzung (Emission, Zulässigkeit), Lauf-Hinweis, Ressourcen beider Sprachen, Tests | ≈ 120 | `opus` |
| KB-7c | Trägerkarte für Einheit m³ ohne Heizwert (Hülle, Dialog), Wirtschaftlichkeitszeile, ggf. Kennzahl; bunit-Tests, Schale kompilieren | ≈ 100 | `opus` |
| KB-7d | Wiki-Quellen, Logbuch-Entwurf, Statuszeile, Protokoll, Entwurf nach `ueberholt/` | ≈ 40 | `sonnet` |

KB-7a vor KB-7b; KB-7c nach KB-7b; KB-7d zuletzt. Gate einmal nach dem Merge.

## 9 Entscheide des Anwenders (16.10.2026)

| Nr. | Frage | Entscheid (Anwender 16.10.2026, jeweils nach Empfehlung) |
|---|---|---|
| F1 | Abrechnungseinheit `m³` neu in der Preiskarte zulassen (nur für den Wasserträger, ohne Heizwert)? | **Ja** — Preis je m³ (Entscheid „Kostenbasis übernimmt“), nur am Träger `WATER`; Gasträger bleiben bei `Nm³` |
| F2 | Ein Wasserpreis je Projekt oder je Anlage (eigener Trägerverweis an der Anlage wie `Kuehl_ID_Carrier`)? | **Je Projekt**; abweichende Altpreise nach Regel 3c mit Hinweis |
| F3 | Preissteigerung der Wasserkosten: Betrieb (p_B) oder Energie (p_E)? | **p_B** — Wasser ist Betriebsmittel, keine Endenergie |
| F4 | Wasserkosten als eigene Berichtskennzahl? | **Ja**, neben `kaelte.rk.wasser_m3` |
| F5 | Abwasser (Absalzung) als zweiter Preis, und Grundpreis des Wasserträgers ansetzen? | **Nein**: ein Arbeitspreis, der Abwasseranteil wird im Preis je m³ mitgeführt; Grundpreis-Feld am Wasserträger ausblenden |
