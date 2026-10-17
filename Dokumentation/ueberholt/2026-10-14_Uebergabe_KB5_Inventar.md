# Inventar KB-5: Stromspeicher-Kosten auf Position (Übergabe 14.10.2026)

Erhoben am 14.10.2026 auf dem Stand `6b2564da7` (origin) und `kb4` für die Welle KB-5 (Schemaschritt 229). Gehört zu [`2026-10-14_Uebergabe_Dialoge_Kostenbasis_KB4_KB5.md`](2026-10-14_Uebergabe_Dialoge_Kostenbasis_KB4_KB5.md). Fundstellen Datei:Zeile beziehen sich auf diesen Stand.


Stand: Hauptbaum 6b2564da7. Nur gelesen. Vorbild kb4 = GeraetekostenEntfallSchema.cs, KatalogkostensatzCtrl.cs (nur auf kb4, nicht auf main).
Rohtreffer (alle Wörter, 726 Zeilen): scratchpad/kb5/treffer.txt. Zeilennummern = Hauptbaum.

## 0. Vorab-Befunde (wichtig für den Schnitt)

- **Verweisspalten und Saat existieren schon.** `ID_KostenVorlage`/`ID_KostenVorlageInvestition` stehen an `Tab_Stromspeicher(_STAMM)` (Schritte 208/209 Katalog, 226 Projektkopien). `KatalogkostenSaatSchema.cs:202-209` führt das Gewerk "Tab_Stromspeicher_STAMM/Tab_Stromspeicher" (Komponente 5) mit allen vier Feldern. Schritt 232 braucht daher KEIN neues DDL der Verweise und keine neue Saat, nur: Sicherheitslauf der Saat, Nachtrag der Saat für Projektkopien ohne Verweis, DROP COLUMN der 4 Spalten an beiden Tabellen (8 Spalten). KB-4 schloss den Stromspeicher ausdrücklich aus (GeraetekostenEntfallSchema.cs Kommentar Z.60-63 auf kb4).
- **Testdatenbank ist bereits gesät:** Vorlagen 59-63 (Komponente 5, Kategorie 1, "Speicher", EUR_PRO_KWH_KAPAZITAET, Satz 3999,5) hängen an Stamm 1 und den Projektkopien 1007001, 1017062, 1017065, 1017088 (Vaillant 10030745).
- **Bemessungsarten sind zulässig:** KapazitaetsSatz und KW_LEISTUNG (BaugroesseSumme, TechnikPlanwertCtrl.cs:814/857), BETRAG (absolut, passt zu jedem Gewerk), EUR_PRO_KWH_ELEKTRISCH am Stromspeicher = Art "aus dem Lauf" (WirtschaftlichkeitCtrl.cs:8763-8768, EndenergieAufloeser.cs:733 `SpeicherEntladungKwh`) = je ENTLADENER kWh. Komponente 5 steht im Auswahlfilter (KostenVorlagenCtrl.cs:831).
- **Der Auslegungs-Rechenweg liest schon Positionen:** `SpeicherAuslegungCtrl.Modulkosten` (SpeicherAuslegungCtrl.cs:151-195) liest Tab_ProjektWerte (Kategorie 1/2) der aktiven Anlage: KW_LEISTUNG/KW_ELEKTRISCH, KWH_KAPAZITAET, Betrieb je entladener kWh. BETRAG wird NICHT übernommen ("kein übernehmbarer Kostensatz"), `IFixEur = 0` in Rechnung.cs:334.
- **Zwei Rechenwege lesen noch die Spalten:** StromspeicherSimCtrl.LeseParameter und PeakShavingCtrl (s. 2).

## 1. Leser und Schreiber der vier Spalten

### 1a. Schema, SQL, Register
- EPOS.Kern/Allgemein/Update/SchemaKatalog.cs:991-993 (Tab_Stromspeicher DOUBLE-Spalten), :999-1001 (Tab_Stromspeicher_STAMM) - Spaltenliste, die Migration legt sie an
- sql/schema/001_grundschema.sql:2288-2293 (STAMM), :2305-2311 (Projekt); sql/schema/inventar.json:29812-29877, 30027-30105 (Modulkosten/Verschleisskosten/Leistungskosten/Investition_Fix) - erzeugte Schema-Inventare, nach DROP neu zu erzeugen (Werkzeuge/Testdatenbankschema)
- EPOS.Kern/Allgemein/Katalog/Katalogfassung.cs:437-439 (Spaltenliste Tab_Stromspeicher_STAMM der Katalogfassung/Prüfsumme) - KB-4-Vorbild entfernt die Spalten aus der Liste; ändert die Spaltenmenge der Fassung
- EPOS.Kern/Allgemein/Katalog/KatalogRegistry.cs:303-310 (STROMSPEICHER, AusschlussSpalten = alle vier) - nach DROP leeres Array (KB-4-Vorbild `new string[0]`)
- EPOS.Kern/Allgemein/Katalog/ParameterVerwendung.cs:828 (Modulkosten), :836-841 (Verschleisskosten, Leistungskosten, Investition_Fix; Verweisfelder "StromspeicherSimCtrl.LeseParameter", "TechnikPlanwertCtrl.BasenFuellen") - Einträge entfallen; Wachen ParameterVerwendungTests prüfen Vollständigkeit
- EPOS.Kern/Allgemein/Katalog/ModulKatalogProfil.cs:241-245 (Feldschlüssel), :302 (Kostenfelder-Liste), :374 (Modulkosten €/kWh), :384-392 (Verschleiss, Leistungskosten, InvestitionFix) - Katalogmaske des Stromspeichers
- EPOS.Kern/Allgemein/Katalog/KatalogBrowserProfil.cs: kein Stromspeicher-Spaltenbezug gefunden (nur Kommentar :137); Katalogliste/Spaltenfilter trotzdem prüfen (KatalogspaltenfilterTests.cs:716 Stromspeicher-Spalten)
- Schemaregister/Fachspalten: AnlagenFachspalten.cs, KomponentenUebernahmeCtrl.cs, ProjektDuplizierenCtrl.cs, WizardCtrl.cs enthalten KEINE der vier Spaltennamen (Grep leer) - kopieren die Zeilen vermutlich per SELECT * / Spaltenliste aus Schema; Gegenprobe im Test (Kopierwachen) nötig

### 1b. Controller (SQL SELECT/INSERT/UPDATE) und Modell
- EPOS.Kern/Controller/StromspeicherStammCtrl.cs (28 Zeilen Cost-Spalten): INSERT :82-100 und :202-220, UPDATE :127-144, Reader :329-338, Kopie :825-829, Anzeige/Katalogliste :869-875, Plausibilität :941-945, Mehrfachbearbeitung-UPDATE :1043-1060 (`Wert("Verschleisskosten"…)`), Kommentar :1077
- EPOS.Kern/Controller/StromspeicherCtrl.cs (Projektkopie): Reader :48-49/103-104/164-165 (Modulkosten), :194-216 (Verschleiss, Leistung, Fix), INSERT-Kopie aus Stamm :285-305
- EPOS.Kern/Model/StromspeicherModel.cs:22-32,72-77 (m_Modulkosten, m_Verschleisskosten, m_Leistungskosten, m_InvestitionFix; Vorgabewert c_ver :51)
- EPOS.Kern/Controller/SpeicherFlottenStudieCtrl.cs:260-262 (Doku), :334 (`kosten`), :351-354 (EinheitAusKatalog -> FlottenEinheit), :266-271 (Verschleiss bewusst NICHT abgebildet)
- EPOS.Kern/Controller/SpeicherFlottenStudieCtrl.Uebernahme.cs:92-108 (SQL_GERAET_NEU/SQL_GERAET_AENDERN: schreibt Modulkosten, Leistungskosten, Investition_Fix in Tab_Stromspeicher beim Übernehmen einer Flotteneinheit)
- EPOS.Kern/Controller/PeakShavingCtrl.cs:348-350 (v.CCap/CPow/IFix aus Modulkosten/Leistungskosten/Investition_Fix)
- EPOS.Kern/Controller/TechnikPlanwertCtrl.cs:461-472 (BasenFuellen Stromspeicher: cCap*Energie + cPow*Leistung + fix), Kommentare :431-440, :1362
- EPOS.Kern/Controller/StromspeicherSimCtrl.cs:1362-1366, 1458, 1472-1476, 1514-1534, 1571-1577 (s. 2)
- EPOS.Kern/Allgemein/Import/Stromspeicher/StromspeicherImportSatz.cs:47-48,134-135 (Import setzt die Spalten NICHT; nur Kommentar) - Import bleibt kostenfrei, Kommentare nachziehen
- Reine Ergebnis-/Engine-Namen, KEINE Katalogspalte (nicht anfassen): ErgebnisCtrl.cs:998/1040/1672/2235, ErgebnisModel.cs:1321, SchemaMigration.cs:17234 (Ergebnistabelle), SpeicherKennzahlenBlock.cs:322, SpeicherEngine/* (CVerEurProKwhZyklus, VerschleisskostenEurProA), StromTarifRechner.Leistungskosten (Tarif), FlottenRechnung.LeistungskostenEuro, SpeicherFlottenAnzeigeCtrl.cs:381

### 1c. Hüllen, Masken, KI, Ressourcen
- WindowsFormsApplication1/Views/Stromspeicher/StromspeicherAdminHuelle.cs:159-164 (Katalogmaske -> Modell, Modul-/Verschleiss-/Leistungskosten; Investition_Fix hier nicht), StromspeicherHuelle.cs:178 (Kommentar), :387 (Anzeigezeile Modulkosten im Detail)
- EPOS.UI/Dialoge/Erzeuger/ModulKatalogKiSicht.cs:160-164 (Modulkosten), :211-222 (Verschleisskosten, Leistungskosten), InvestitionFix-Feld (Z.~225); EPOS.UI/Dialoge/Erzeuger/StromspeicherSteckbrief.cs:33 (Modulkosten im Steckbrief)
- EPOS.Kern/Allgemein/KI/Dialoge/KiDialoge.cs:10216-10300 (modulkosten, verschleisskosten, leistungskosten, investitionfix) und :5747 (Kaeltemaschine, fremd); KiDialogTexte.cs:2973 (Einheit Verschleiss), :3090-3095
- Ressourcen (Resource.resx, beide Sprachen + Designer): SP_LABEL_MODULKOSTEN(:5618), SP_LABEL_VERSCHLEISSKOSTEN(:4078), SP_LABEL_LEISTUNGSKOSTEN(:4081), SP_LABEL_INVESTITION_FIX(:4084), SP_EINHEIT_ZYKLUSKOSTEN(:4090), KI_DLG_MODK_LEISTUNGSKOSTEN_ERL(:25275), KI_DLG_MODK_VERSCHLEISS_ERL(:25272), KI_DLG_MODK_INVEST_FIX_ERL(:25278); MODK_LBL_MODULKOSTEN wird auch von WP/Kältemaschine geteilt (nicht löschen). Danach `Werkzeuge/ResourceDesigner designer_neu.py schreiben`
- EPOS.UI: Razor nur indirekt (generische Katalogmaske über ModulKatalogProfil; StromspeicherAuslegungSeite.razor:224/SpeicherAuslegungEditor.razor = SpeicherKostensaetze, kein Spaltenzugriff); EPOS.UI.Daten: keine Treffer (außer fremden Kaeltemaschine/WP). EPOS.iOS: keine Treffer.
- Werkzeuge/ Referenzlauf/ Skripte: keine Zugriffe auf die vier Spalten (Referenzlaeufe/Skripte kopieren nur Tab_Stromspeicher als Ganzes per ID_SP-Map); Werkzeuge/Testdatenbankschema/Program.cs und Auslieferungsvorlage(.Tests/Werkzeuglauf.cs:101) nennen nur Tabellennamen -> Auslieferungsvorlage prüfen (Spaltenmenge des Kataloges).
- Embedded Hilfe: EPOS.Kern/Allgemein/Hilfe/Berechnung/Stromspeicher.wiki:44-46, 71, 107-111, 311-313, 473-475

## 2. Rechenwege (wo die vier Werte eingehen)

| Stelle | Was | Folgerung KB-5 |
|---|---|---|
| StromspeicherSimCtrl.LeseParameter (:1361-1580) | SELECT a.ID, sp.* je Anlage; summiert Investition_Fix; energiegewichtet Modulkosten (c_cap) und Verschleisskosten (c_ver); leistungsgewichtet Leistungskosten (c_pow); Rückfall c_ver = C_VER_STANDARD 0,025 (:122,:1534); Ergebnis in SpeicherParameter CCap/CPow/IFix/CVer (:1571-1577); InvestitionEur = CCap*CNom + CPow*P + IFix (SpeicherParameter.cs:214) | Auf Sätze aus der Satzvorlage der Projektkopie umstellen. Weil `sp.*` gelesen wird, scheitert der Lauf nach DROP nicht, rechnet aber leise 0. Gewichtung über mehrere Anlagen (Aggregationsrückfall) bleibt zu erhalten |
| SpeicherParameter.CVerEurProKwhZyklus (SpeicherEngine) | K_ver = Vollzyklen * C_nom * c_ver (Dauernutzung.cs:392, PeakShaving.cs:398, Arbitrage.cs:410); ArbitrageOptionen.cs:182 (Grenzverschleiß ct/kWh) | Einheit EUR je (kWh Nennkapazität * Vollzyklus). Engine bleibt; nur die Quelle ändert sich |
| PeakShavingCtrl.cs:348-350 | eigener Leser derselben drei Spalten (v.CCap/CPow/IFix) | zweiter Leseweg, auf denselben Satzleser umstellen |
| SpeicherAuslegungCtrl.cs:140-142 | Vorbelegung der DirekteKosten aus `LeseParameter` (basis.CPow/CCap), Herkunft "Gerätedaten" | folgt automatisch LeseParameter |
| SpeicherAuslegungCtrl.Rechnung.cs:332-334 | `laufBasis with { CCap = kosten.InvestEurProKwh, CPow = ..., IFix = 0.0 }`; Quelle Katalog/Kostenmodul wählbar (Investitionsquelle/Betriebsquelle); SpeicherAuslegungCtrl.Modulkosten(…) liest Tab_ProjektWerte | schon Position-basiert; BETRAG (Fix) fehlt dort |
| TechnikPlanwertCtrl.BasenFuellen Stromspeicher (:461-472) | Planwert = cCap*Energie + cPow*Leistung + fix, BASIS_SPEZIFISCH; Verschleiß bewusst nicht (Doppelzählung, :1362) | Quelle ersetzen: Planwert aus Satzvorlage (wie KB-4 für die anderen Gewerke) |
| WirtschaftlichkeitCtrl / InvestKaskade / KostenPositionCtrl | liest nur Tab_ProjektWerte (Positionen); keine Spalten (Grep leer für Verschleiss/Modulkosten). KostenPositionCtrl.cs:595 nutzt BASIS_SPEZIFISCH | unverändert; Kapitalwert nimmt die Positionen. Verschleiß-Jahreskosten (ErgebnisModel.Verschleisskosten, "eigene Betriebskostenzeile 5.4") laufen separat aus SimCtrl |
| PvBatteriespeicherBemessung | im Quelltext nicht gefunden, nur Tests (EPOS.Kern.Tests/PvBatteriespeicherBemessungTests.cs, BemessungsauswahlJeGewerkTests.cs); keine Kostenspalte gelesen - bitte beim Bau nachsehen | |
| Zulässige Bemessung Komponente 5 heute | Investition: EUR_PRO_KWH_KAPAZITAET, EUR_PRO_KW_LEISTUNG, BETRAG (+ übrige Gewerk-unabhängige Arten); Betrieb: EUR_PRO_KWH_ELEKTRISCH (= entladene kWh aus dem Lauf, BASISGRUND_LAUF), JAHRESBETRAG, EUR_PRO_KWH_KAPAZITAET (BetriebEurProKwhJahr), EUR_PRO_KW_LEISTUNG (BetriebEurProKwJahr). Konzept Wirtschaftlichkeit 2.14a Tabelle Z.1524-1529 | Saat nutzt genau diese |

## 3. Flotte

- Felder in `Tab_SpeicherAuslegung.Daten` (gz1:base64 JSON, FlottenEinheit): InvestitionEuro, InvestitionEuroProKWh, InvestitionEuroProKw, JaehrlicheFixeOpexEuro, JaehrlicheOpexEuroProKWhKapazitaet, JaehrlicheOpexEuroProKw, DurchsatzkostenEuroProKWhEntladung, GrenzverschleissEuroProKWhEntladung, ErsatzkostenEuro, EigeneKosten (SpeicherEngine/FlottenModel.cs:252).
- Schreiber aus Katalogspalten: SpeicherFlottenStudieCtrl.EinheitAusKatalog (:334-354): Modulkosten -> InvestitionEuroProKWh, Leistungskosten -> InvestitionEuroProKw, Investition_Fix -> InvestitionEuro, EigeneKosten = (eine der drei > 0). Verschleiss: nicht abgebildet (Begründung :266-271: Einheit €/(kWh*Zyklus) auf Nennkapazität gegen je abgegebene AC-kWh).
- Rückschreiber: SpeicherFlottenStudieCtrl.Uebernahme.cs:92-108 (Flotte -> Tab_Stromspeicher: drei Investitionssätze).
- Leser: SpeicherFlottenStudieCtrl.Konfiguration (:560-590): EigeneKosten=false -> Sätze aus `a.VerwendeteKosten` (SpeicherAuslegungCtrl.Modulkosten = Tab_ProjektWerte); EigeneKosten=true -> die gespeicherten Eigenwerte. SpeicherAuslegungCtrl.Rechnung.cs:295-298 `SpezifischeSaetzeGebraucht`. FlottenPlausibilitaet.cs:339.
- Projekt 1046: Stand `@Projektflotte` (Tab_SpeicherAuslegung ID 1, Daten 1724 Byte komprimiert, entpackt 3391 Zeichen): Einheit A "flotte-1046-a" EigeneKosten=true, AnlageId=null, 350 €/kWh, 200 €/kW, Fix 2000 €, Opex 2,5/5/120, Durchsatz 0,01, Grenzverschleiß 0,02, Ersatz 4000; DirekteKosten "Prüfprojekt 1046 (SP-O-8)". Alle Eingaben stehen im JSON, nicht in den Katalogspalten -> der DROP lässt den Flottenstand unverändert, wenn EigeneKosten=true bleibt und kein Pfad die Spalten liest. Die Projektanlagen von 1046 (Tab_Stromspeicher ID 1017062 Vaillant mit Modulkosten 3999,5, 1017063 BYD ohne Kosten) gehen NICHT in den Flottenlauf ein (AnlageId null).

## 4. Testdatenbank (Referenzlaeufe/Kenndaten_Test.sqlite, nur Lesen)

- Tab_Stromspeicher_STAMM: 5 Zeilen, 22 Spalten; Tab_Stromspeicher: 24 Zeilen, 20 Spalten. Werte >0: nur Modulkosten. Leistungskosten, Investition_Fix, Verschleisskosten: überall NULL/0.
  - Stamm: 1 Zeile (ID 1 "Vaillant 10030745", Modulkosten 3999,5; ID_KostenVorlageInvestition 59). Projektkopien mit Modulkosten > 0: 1007001 (Projekt 1007, Vorlage 60), 1017062 (Projekt 1046, 61), 1017065 (Projekt 1051, 62), 1017088 (Projekt 1065, 63); alle übrigen 20 Kopien (BYD HVS+/B-Box) haben 0.0 und keine Vorlage.
- Anlagen mit Stromspeicher (Tab_Energieanlagen ID_SP>0): 31 Zeilen in den Projekten 1007, 1017, 1026, 1028, 1029, 1046, 1047, 1051, 1055-1065, 1072. Die Vaillant-Anlagen: 10357 (1007), 14938 (1046), 17016 (1051), 24236 (1065).
- Tab_KostenKomponente 5 = Stromspeicher (11 = Kältemaschine). Tab_KostenVorlage: Standardvorlagen ID 7 (Invest) und 17 (Betrieb) Komponente 5 (ReadOnly) plus die gesäten 59-63 (je eine Position "Speicher", KAPITALGEBUNDEN, EUR_PRO_KWH_KAPAZITAET, Satz 3999,5). Keine Betriebsvorlage gesät (kein Verschleißwert).
- Tab_ProjektWerte: für die vier Vaillant-Anlagen KEINE Zeilen (die Position ist nur Vorlage, nicht angewandt). Rechnung nimmt heute den Spaltenwert 3999,5 als c_cap (SimCtrl, nur wo die Anlage die aktive Variante ist: 1007, 1046, 1051, 1065 haben die Vaillant-Anlage in der Variante; Aktivheit nicht geprüft).
- **Auffälligkeit:** 3999,5 als €/kWh ist fachlich unplausibel (bei 7,9 kWh = 31,6 k€ Planwert); sieht nach einem absoluten Preis in € aus. Der Saatsatz EUR_PRO_KWH_KAPAZITAET erhält die Zahl unverändert (ergebnisneutral).

## 5. Tests

Halten Spalten oder Rechenwege (anzupassen/neu):
- EPOS.Kern.Tests: StromspeicherRueckwegTests.cs (:277, :422-431 setzt Kostenspalten), StromspeicherMehrfachbearbeitungTests.cs (:35, :64, :92), StromspeicherUebernahmeTests.cs (:507, :591, :613), StromspeicherImportTests.cs (:151), SpeicherFlottenUebernahmeTests.cs (:151), SpeicherFlottenQuellenTests.cs (:100 INSERT mit Kostenspalten), SpeicherFlottenAnzeigeCtrlTests.cs, SpeicherKennzahlenBlockTests.cs, ParameterVerwendungTests.cs (Spaltenvollständigkeit, 13 Treffer), KatalogVerwaltungTests.cs, KatalogkostenSaatSchemaTests.cs (Gewerk Stromspeicher), KatalogkostenabgleichTests.cs, KatalogspaltenfilterTests.cs:716, SpeicherAuslegungProfilTests.cs:101, BemessungsauswahlJeGewerkTests.cs, PvBatteriespeicherBemessungTests.cs, InvestKaskadeTests.cs, TestDatenbank.cs (Seed), EinRasterFuerAlleLeserTests.cs / StromViertelstundenTests / StromspeicherAuslegungCtrlTests / LastspitzenkappungTests (Tab_Stromspeicher-Seeds prüfen)
- EPOS.UI.Tests: Dialoge/StromspeicherDialogTests.cs (:70, :298, :625-637 Feldanzahl "Sieben nur lesbare Anzeigefelder"), Dialoge/ModulKatalogDialogTests.cs (:184-202), Dialoge/Hilfe/KiFeldSetzenTests.cs (Stromspeicher-KI-Sicht), SpeicherFlottenErgebnisBetriebTests/ErgaenzungenTests (nur FlottenRechnung-Name, unberührt)
- SpeicherEngine.Tests/QuellenmatrixTests.cs (Kennzahlen aus SpeicherParameter, Engine, unberührt)
- Wachen mit Spaltenzahlen: keine eigene Wache "Tab_Stromspeicher hat N Spalten" gefunden. Indirekt: KatalogfassungsTests/Paketanhebung (Spaltenmenge der Fassung), ParameterVerwendungTests, Schemastand-/Migrationstests (SchemaStand.cs trägt die Schrittzahl), GeraetekostenEntfallSchemaTests (kb4) als Vorbild, RepositoryOrdnungWacheTests/DokumentationLinkWacheTests, WikiProduktdatenWacheTests (Stromspeicher-Katalognamen in Wiki-Quellen).

## 6. Wiki- und Konzeptstellen

- Projekte/Wiki/Programm Dokumentation - Stromspeicher.wiki:150 ("Modulkosten - der Betrag, der in die Wirtschaftlichkeit eingeht"), :298 (drei Investitionssätze bei Flottenübernahme), :353 (Kostenmodell)
- Projekte/Wiki/Programm Dokumentation - Gerätekataloge.wiki:184 (Speicherdaten-Felder; Kosten-Satz prüfen), Projekte/Wiki/Programm Dokumentation - Kosten.wiki (Satzvorlage/Katalogkosten-Abschnitt ergänzen). Update-Logbuch-Eintrag vorschlagen, Version beim Anwender erfragen; Upload gebündelt.
- EPOS.Kern/Allgemein/Hilfe/Berechnung/Stromspeicher.wiki:44-46, 71, 107-111, 311-313, 473-475 (Quelle "Speicherkatalog" für c_cap, c_pow, c_ver, I_fix)
- Dokumentation/aktuell: Konzept_Wirtschaftlichkeit_EPOS-Plan_konsolidiert.md :165 (Gewerktabelle), :1524-1529 (Saat-Tabelle 2.14a, "Wie gebaut KB-3"; KB-5 hier fortschreiben), Entscheidungsregister_Wirtschaftlichkeit :660 (E16-Q4), Konzept_Stromspeicher_EPOS-Plan.md :458-486, :1035, :1252, Konzept_Stromspeicher_Dialoge_EPOS-Plan.md :254 (Modulkosten -> InvestitionEuroProKWh nur mit EigeneKosten), :1212, Konzept_Stromspeicherimport_EPOS-Plan.md :286, :312, :397, :576, :712 (Kosten bleiben 0), Konzept_Katalogfilter_EPOS-Plan.md :291, :542, VDI-3805-Daten/Stromspeicher/LIESMICH_bslib.md:63, Doku_Mehrspeicher: keine Spaltentreffer.

## 7. Einfrierregeln und Basen

- Berührt (CLAUDE.md "Einfrierregeln"): (a) Flottenstand @Projektflotte 1046 - nur wenn die Flotte aus Spalten befüllt würde (heute EigeneKosten=true, nicht betroffen); (b) "Stromfelder der Referenzprojekte": Einspeisegrenze, Standby_Verbrauch, Selbstentladung_Prozent_Monat an Projektkopien der Speicher - die vier Kostenspalten stehen dort NICHT, aber die Projektkopie-Zeilen der Stromspeicher werden verändert (neue Verweise, Spalten entfallen); (c) "Bedarfsdaten/Kostendaten": Satzvorlagen 59-63 sind neu seit 13.10.2026; Anlegen oder Ändern von Katalogkosten ist nicht ausdrücklich als Einfrierregel geführt. Maßgeblich ist, ob Rechenergebnis der Projekte 1007, 1046, 1051, 1065 (alle in der CI-Auswahl, alle mit Vaillant-Anlage und Modulkosten 3999,5) gleich bleibt.
- Basis: Referenzlaeufe/2026-10-14_R55_KaeltebedarfDezentral (37 Projekte, LIESMICH.md:841; Schemastand 227 für R54; CLAUDE.md nennt noch R54/36 - Stand der Datei hinkt). Nächste R56 (vom Auftraggeber genannt; in LIESMICH.md keine R56-Vorbelegung gefunden). Stromspeicher-Kosten gehen in die CSVs der Projekte mit aktiver Speichervariante ein (Wirtschaftlichkeit/Kennzahlen). Ist die Umstellung wertgleich (Satz 3999,5 EUR_PRO_KWH_KAPAZITAET), bleibt die Basis; jede Korrektur des Werts oder eine Verschleißbetriebsposition erfordert R56 (Vergleich vor/nach entscheidet).
- Schemaschritt-Zeile: Status_iOS_Migration.md Kopf zeigt "232 ... Welle KB-5 ... StromspeicherKostenSchema" angemeldet.

## 8. Gewerk -> Dateien -> Aufwand

| Gewerk | Dateien | Aufwand |
|---|---|---|
| Schema (232) | neu EPOS.Kern/Allgemein/Update/StromspeicherKostenSchema.cs (Sicherheitslauf Saat, DROP COLUMN 8 Spalten, Bericht), SchemaStand.cs, ggf. Paketanhebung.cs, SchemaKatalog.cs (Spalten raus), sql/schema/001_grundschema.sql + inventar.json neu erzeugen | mittel (Vorbild GeraetekostenEntfallSchema; Saat-Gewerk existiert) |
| Satzleser | KatalogkostensatzCtrl (aus kb4 übernehmen, für 4 Positionen je Satz erweitern: Einheit()/Satz() liefern nur eine Zahl) | mittel |
| Rechenwege | StromspeicherSimCtrl.LeseParameter, PeakShavingCtrl, TechnikPlanwertCtrl.BasenFuellen, SpeicherAuslegungCtrl (+ BETRAG als Fix annehmen) | groß (Gewichtung, Einheit Verschleiß, Ergebnisgleichheit) |
| Flotte | SpeicherFlottenStudieCtrl (EinheitAusKatalog, Uebernahme SQL, Konfiguration) | mittel |
| Controller/Modell/Katalog | StromspeicherStammCtrl, StromspeicherCtrl, StromspeicherModel, ModulKatalogProfil, KatalogRegistry, Katalogfassung, ParameterVerwendung, KatalogBrowserProfil(Prüfung) | groß (rund 60 Stellen) |
| Hüllen/UI/KI/Ressourcen | StromspeicherAdminHuelle, StromspeicherHuelle, ModulKatalogKiSicht, StromspeicherSteckbrief, KiDialoge, KiDialogTexte, Resource.resx (de/en) + Designer | mittel |
| Tests | ca. 20 Dateien (Abschnitt 5) + neue StromspeicherKostenSchemaTests | groß |
| Doku/Wiki | Konzept Wirtschaftlichkeit 2.14a/165, Stromspeicher-Konzepte, Wiki-Quellen Stromspeicher/Gerätekataloge/Kosten, Hilfe Stromspeicher.wiki, Logbuch-Entwurf | mittel |
| Basis R56 | Referenzlauf 37 Projekte, Vergleich gegen R55, LIESMICH | klein bis mittel (nur wenn Werte ändern) |

## 9. Offene Fachfragen

1. **Verschleißkosten: Einheit und Bemessung.** Katalog/Engine: €/(kWh Nennkapazität * Vollzyklus), Rückfall 0,025 (SimCtrl :122), K_ver = Vollzyklen * C_nom * c_ver. Die Saat (KatalogkostenSaatSchema.cs:208) macht daraus EUR_PRO_KWH_ELEKTRISCH = je ENTLADENER kWh - andere Größe (Faktor nutzbares Band * Entladewirkungsgrad, Flottenkommentar :266-271). Welche Bemessung gilt für die Betriebsposition, und wird c_ver weiter aus der Position gelesen (Umrechnung nötig) oder bleibt der Rückfall 0,025? In der Testdatenbank ist Verschleiß überall 0, die Frage berührt die Basis nicht, wohl aber Neuanlagen.
2. **Doppelzählung Verschleiß:** SimCtrl bucht K_ver als eigene Betriebskostenzeile (ErgebnisModel 5.4); eine zusätzlich angewandte Betriebsposition "Verschleiß je entladener kWh" (Auslegung: BetriebEurProKwhEntladen) würde dasselbe zweimal rechnen (TechnikPlanwertCtrl.cs:1362 warnt). Entscheid: Verschleißposition nur für die Auslegung/Flotte (DurchsatzkostenEuroProKWhEntladung) oder auch für die Einzelanlage?
3. **Investition_Fix (BETRAG)** wird von SpeicherAuslegungCtrl.Modulkosten nicht übernommen (IFix = 0). Soll BETRAG der Position in die Auslegung/Flotte (Zeile InvestitionEuro) eingehen? Betrifft Konsistenz zwischen Einzelanlage (IFix in InvestitionEur) und Auslegungsrechnung.
4. **Testwert 3999,5** (€/kWh) der Vaillant-Anlagen: ergebnisneutral übernehmen oder korrigieren (dann R56 begründet)? Und wirken die Vorlagen auf die Projektanlagen (Tab_ProjektWerte leer) - muss die Rechnung weiter aus der Katalog-Satzvorlage der Projektkopie lesen (Satz x Bezug) statt aus Tab_ProjektWerte?
5. **Flotte: aus Satzvorlage befüllen oder Eingabe bleibt?** EinheitAusKatalog setzt EigeneKosten und die drei Sätze aus Spalten; Vorschlag Befund: aus Satzvorlage der Anlage/Katalogsatzes lesen (KatalogkostensatzCtrl), EigeneKosten = Vorlage vorhanden; Rückschreiben (Uebernahme) dann in die Satzvorlage der Projektkopie oder weglassen. Muss entschieden werden (SQL_GERAET_NEU/AENDERN schreiben die Spalten).
6. **Mehrfachbearbeitung/Plausibilität im Katalog** (StromspeicherStammCtrl :941-945, :1043-1060): Die Masken pflegen Kosten künftig nur in der Kostenverwaltung; Katalogmaske zeigt nur Lesewert "Gerätekosten" (KB-4-Muster). Auch für den Detail-Steckbrief (StromspeicherHuelle :387)?
7. **Katalogfassung:** Die Spaltenliste (Katalogfassung.cs:437) ändert sich -> neue Fassung/Prüfsumme und Katalogpaket (Werkzeuge/Auslieferungsvorlage --katalogfassung)? In KB-4 wurde so verfahren (Paketanhebung.cs). Bestätigen.
8. **Einheitentext** `SP_EINHEIT_ZYKLUSKOSTEN` und Stromspeicher.wiki (c_ver "Speicherkatalog; Rückfall 0,025") nach der Entscheidung zu Frage 1 anpassen.
9. Basis- und CLAUDE.md-Stand: CLAUDE.md nennt noch R54 mit 36 Projekten, im Ordner liegt R55 mit 37 (LIESMICH.md:841) - Gegenlesen vor R56.
