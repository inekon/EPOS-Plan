-- ============================================================================
-- migration.manuell.sql  (MANUELL GEPFLEGTES SKRIPT, V4 PARAMETRISIERT, 26.07.2026)
-- Quelle: Benutzer-DB (alte Struktur)  |  Vorlage: neue Versions-DB
--
-- Die GUI verwendet dieses Skript im Auto-Modus automatisch (Vorrang) und
-- befuellt die Projekt-Platzhalter aus dem KONFLIKTDIALOG:
--   {{PROJEKTE_QUELLE}}        zu uebernehmende Quell-Projekte
--   {{PROJEKTE_ZIEL_LOESCHEN}} zu ersetzende Ziel-Projekte (Konflikt "ueberschreiben")
--   {{PROJEKT_OFFSET}}         ID-Versatz (automatisch bei ID-Kollisionen, sonst 0)
--   {{SUFFIX_ALT}}             Namenszusatz (Modus "alte umbenennen", sonst leer)
--
-- Aufbau:
--   Teil A: Kataloge alt -> *_STAMM. REGEL (26.07.): Zeilen mit ReadOnly=TRUE
--           bleiben aus der VORLAGE erhalten; alle uebrigen Inhalte kommen aus
--           der QUELLE. KEINE Ausnahmen - auch Klimaregion/Klimadaten/Solar
--           werden aus der Quelle uebernommen (STAMM und je Projekt).
--   Teil B: Projektdaten der gewaehlten Projekte in die NEUE Struktur:
--           projektbezogene Komponenten-KOPIEN (ID_Projekt), Namensfeld
--           Bezeichner, umgestellte Verknuepfungen (Gebaeude, Tagesverteilung,
--           Ganglinien, Pufferspeicher u. a.).
--
-- Kopie-ID-Schema: (ID_Projekt+OFFSET)*1000 + alte Katalog-ID.
-- Zuordnungs-IDs (Z_*-Tabellen, Energieanlagen): +10000.
--   (K6, 20.08.2026: "Brennstoff_Projekt" hier gestrichen - die Tabelle
--    Tab_Brennstoff_Projekt ist mit Migrationsschritt 29 entfallen; ihre beiden
--    Skriptabschnitte sind schon seit K1 heraus, :239 und :490.)
-- KONFLIKTREGELN (eindeutige Indizes im Ziel, geprueft 26.07.2026):
--   * Projektname: Konfliktaufloesung ueber den Dialog (Platzhalter oben).
--   * Stromverbraucher-Kopien + Typen: Suffix ' (P<Projekt>)' noetig, weil
--     Bezeichner/Typname im Ziel GLOBAL eindeutig sind.
--   * ALLE Inserts vergeben EXPLIZITE IDs (deterministische Schemata) - die
--     Autowert-Zaehler der Vorlage sind teils veraltet (Konvertierung mit
--     expliziten IDs ohne Reseed) und wuerden sonst Duplikate erzeugen
--     (so geschehen bei Tab_Brauchwassertyp, 26.07.). Reseed erfolgt am Ende.
-- Bei einem Fehler wird die neu erzeugte Ziel-Kopie verworfen (Rollback).
-- ============================================================================

-- --------------------------------------------------------------------------
-- Teil A: Kataloge alt -> *_STAMM (ReadOnly=TRUE der Vorlage bleibt,
--         alles andere aus der Quelle; inkl. Klima ohne Ausnahme)
-- --------------------------------------------------------------------------
DELETE FROM [Tab_Kenndaten_STAMM] WHERE [ID_WP] IN (SELECT w.[ID] FROM [Tab_WP_STAMM] AS w WHERE w.[ReadOnly] = FALSE);
DELETE FROM [Tab_Kenndaten_Kuehlung_STAMM] WHERE [ID_WP] IN (SELECT w.[ID] FROM [Tab_WP_STAMM] AS w WHERE w.[ReadOnly] = FALSE);
DELETE FROM [Tab_WP_STAMM] WHERE [ReadOnly] = FALSE;
DELETE FROM [Tab_DBTagVDaten_STAMM];
DELETE FROM [Tab_DBTagV_STAMM];
DELETE FROM [Tab_StromganglinieDaten_STAMM];
DELETE FROM [Tab_Stromganglinie_STAMM];
DELETE FROM [Tab_SolarganglinieDaten_STAMM];
DELETE FROM [Tab_Solarganglinie_STAMM];
DELETE FROM [Tab_WaermebedarfDaten_STAMM] WHERE [ID_Ganglinie] IN (SELECT w.[ID] FROM [Tab_Waermebedarf_STAMM] AS w WHERE w.[ReadOnly] = FALSE);
DELETE FROM [Tab_Waermebedarf_STAMM] WHERE [ReadOnly] = FALSE;
DELETE FROM [Tab_Brauchwassertyp_STAMM];
DELETE FROM [Tab_Brauchwasser_STAMM] WHERE [ReadOnly] = FALSE;
DELETE FROM [Tab_Prozesstyp_STAMM];
DELETE FROM [Tab_Prozesswaerme_STAMM];
DELETE FROM [Tab_Stromverbrauchertyp_STAMM];
DELETE FROM [Tab_Stromverbraucher_STAMM];
DELETE FROM [Tab_Gebaeude_STAMM];
DELETE FROM [Tab_BHKW_STAMM] WHERE [ReadOnly] = FALSE;
DELETE FROM [Tab_Heizkessel_STAMM];
DELETE FROM [Tab_PV_STAMM];
DELETE FROM [Tab_Solarkollektoren_STAMM];
DELETE FROM [Tab_Stromspeicher_STAMM];
DELETE FROM [Tab_Pufferspeicher_STAMM];
DELETE FROM [Tab_Brennstoff_Stamm];
DELETE FROM [Tab_Klimadaten_STAMM];
DELETE FROM [Tab_Solar_STAMM];
DELETE FROM [Tab_Klimaregion_STAMM];

-- === Tab_Brennstoff_Stamm <- alt Tab_Brennstoff_Stamm ===
INSERT INTO [Tab_Brennstoff_Stamm] ([ID], [ID_Kategorie], [Bezeichner], [Einheit], [PreisEinheit], [Hi], [Hs], [CO2], [SO2], [NOx], [Staub], [PE_Faktor], [Standard_Grundpreis], [Standard_Arbeitspreis], [Standard_Leistungspreis], [ReadOnly])
SELECT [ID], [ID_Kategorie], [Name], [Einheit], [PreisEinheit], [Hi], [Hs], [CO2], [SO2], [NOx], [Staub], [PE_Faktor], [Standard_Grundpreis], [Standard_Arbeitspreis], [Standard_Leistungspreis], FALSE
FROM [{{QUELLE}}].[Tab_Brennstoff_Stamm];

-- === Tab_Gebaeude_STAMM <- alt Tab_Gebaeude ===
INSERT INTO [Tab_Gebaeude_STAMM] ([ID], [Bezeichner], [Typ], [Beschreibung], [Wohnflaeche_gesamt], [Bewohner], [Flaeche_Nutzer], [Interne_Waermegewinne], [Bauweise], [Fensterflaeche_Sued], [Fensterflaeche_Ost_West], [Fensterflaeche_Nord], [Fensterdurchlassgrad], [Raumsolltemperatur_Nachtabsenkung], [Raumsolltemperatur_Tag], [Raumsolltemperatur_Wochenende], [Raumsolltemperatur_Ferien], [Maximaleraumtemperatur], [k_Wert_Außenwand], [k_Wert_Fenster], [k_Wert_Dachflaeche], [k_Wert_Grundflaeche], [k_Wert_Sonstiges], [Flaeche_Außenwand], [gesamte_Fensterflaeche], [Dachflaeche], [Grundflaeche], [Sonstige_Flaechen], [Wohnflaeche], [Raumhoehe], [WBVK_Anschluß_Fenster_Wand], [WBVK_Anschluß_Wand_Dach], [WBVK_Anschluß_Außenwand_Kellerdecke], [Abmessung_Anschluß_Fenster_Wand], [Abmessung_Anschluß_Wand_Dach], [Abmessung_Anschluß_Außenwand_Kellerdecke], [Luftwechselrate], [Wochenende], [Ferien], [Ferienbeginn_1], [Ferienende_1], [Ferienbeginn_2], [Ferienende_2], [Ferienbeginn_3], [Ferienende_3], [Ferienbeginn_4], [Ferienende_4], [WW_Bedarf], [spez_Waermeverbrauch], [Waermebedarf], [Baualtersklasse], [Gebaeudeart], [Wohngebaeude_Nicht_Wohngebaeude], [ReadOnly])
SELECT [ID], [Gebaeudename], [Typ], [Beschreibung], [Wohnflaeche_gesamt], [Bewohner], [Flaeche_Nutzer], [Interne_Waermegewinne], [Bauweise], [Fensterflaeche_Sued], [Fensterflaeche_Ost_West], [Fensterflaeche_Nord], [Fensterdurchlassgrad], [Raumsolltemperatur_Nachtabsenkung], [Raumsolltemperatur_Tag], [Raumsolltemperatur_Wochenende], [Raumsolltemperatur_Ferien], [Maximaleraumtemperatur], [k_Wert_Außenwand], [k_Wert_Fenster], [k_Wert_Dachflaeche], [k_Wert_Grundflaeche], [k_Wert_Sonstiges], [Flaeche_Außenwand], [gesamte_Fensterflaeche], [Dachflaeche], [Grundflaeche], [Sonstige_Flaechen], [Wohnflaeche], [Raumhoehe], [WBVK_Anschluß_Fenster_Wand], [WBVK_Anschluß_Wand_Dach], [WBVK_Anschluß_Außenwand_Kellerdecke], [Abmessung_Anschluß_Fenster_Wand], [Abmessung_Anschluß_Wand_Dach], [Abmessung_Anschluß_Außenwand_Kellerdecke], [Luftwechselrate], [Wochenende], [Ferien], [Ferienbeginn_1], [Ferienende_1], [Ferienbeginn_2], [Ferienende_2], [Ferienbeginn_3], [Ferienende_3], [Ferienbeginn_4], [Ferienende_4], [WW_Bedarf], [spez_Waermeverbrauch], [Waermebedarf], [Baualtersklasse], [Gebaeudeart], [Wohngebaeude_Nicht_Wohngebaeude], FALSE
FROM [{{QUELLE}}].[Tab_Gebaeude];

-- === Tab_BHKW_STAMM <- alt Tab_BHKW   (ReadOnly-Zeilen der Vorlage bleiben: 79) ===
INSERT INTO [Tab_BHKW_STAMM] ([ID], [Bezeichner], [Firma], [Beschreibung], [Ptherm], [Pel], [Brennstoff], [Wirkungsgrad], [Investition_kwel], [Raumbedarf], [Wartungskosten_kwhel], [Nutzungsdauer], [NOX], [SO2], [CO], [CO2], [Staub], [Motortyp], [Grenzleistung], [Kosten_Modul], [Kosten_Montage], [Kosten_Lieferung], [Kosten_Schallschutzhaube], [Kosten_Abgasreinigung], [Vorlauf], [Ruecklauf], [ReadOnly])
SELECT [ID], [Bezeichner], [Firma], [Beschreibung], [Ptherm], [Pel], [Brennstoff], [Wirkungsgrad], [Investition_kwel], [Raumbedarf], [Wartungskosten_kwhel], [Nutzungsdauer], [NOX], [SO2], [CO], [CO2], [Staub], [Motortyp], [Grenzleistung], [Kosten_Modul], [Kosten_Montage], [Kosten_Lieferung], [Kosten_Schallschutzhaube], [Kosten_Abgasreinigung], [Vorlauf], [Rücklauf], FALSE
FROM [{{QUELLE}}].[Tab_BHKW] WHERE [ID] NOT IN (61, 62, 63, 64, 65, 66, 67, 68, 69, 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80, 81, 82, 83, 84, 85, 86, 87, 88, 89, 90, 91, 92, 93, 94, 95, 96, 97, 98, 99, 100, 101, 102, 103, 104, 135, 136, 137, 138, 139, 140, 141, 142, 143, 144, 145, 146, 147, 148, 149, 150, 151, 152, 153, 154, 155, 156, 157, 158, 159, 160, 161, 162, 163, 164, 165, 166, 167, 168, 169);

-- === Tab_Heizkessel_STAMM <- alt Tab_Heizkessel   (ohne Quelle, bleibt NULL: Vorlauf, Ruecklauf) ===
INSERT INTO [Tab_Heizkessel_STAMM] ([ID], [Bezeichner], [Firma], [Beschreibung], [Ptherm], [Brennstoff], [Wirkungsgrad_Gas], [Wirkungsgrad_Öl], [Investitionskosten], [Raumbedarf], [Wartungskosten], [Nutzungsdauer], [CO2], [SO2], [NOx], [CO], [Staub], [Betriebsbereitschaftverlust], [Brennwert], [ReadOnly])
SELECT [ID], [Name], [Firma], [Beschreibung], [Ptherm], [Brennstoff], [Wirkungsgrad_Gas], [Wirkungsgrad_Öl], [Investitionskosten], [Raumbedarf], [Wartungskosten], [Nutzungsdauer], [CO2], [SO2], [NOx], [CO], [Staub], [Betriebsbereitschaftverlust], [Brennwert], FALSE
FROM [{{QUELLE}}].[Tab_Heizkessel];

-- === Tab_WP_STAMM <- alt Tab_WP   (ReadOnly-Zeilen der Vorlage bleiben: 8) ===
INSERT INTO [Tab_WP_STAMM] ([ID], [Bezeichner], [Firma], [Beschreibung], [Typ], [Baujahr], [Aufstellung], [Nennleistung], [maxPtherm], [Heizung], [Regelung], [Modulkosten], [Laenge], [Breite], [Hoehe], [Gewicht], [Raum], [Kuehlleistung], [Bauart], [ReadOnly])
SELECT [ID_WP], [WPName], [Firma], [Beschreibung], [Typ], [Baujahr], [Aufstellung], [Nennleistung], [maxPtherm], [Heizung], [Regelung], [Modulkosten], [Laenge], [Breite], [Hoehe], [Gewicht], [Raum], [Kuehlleistung], [Bauart], FALSE
FROM [{{QUELLE}}].[Tab_WP] WHERE [ID_WP] NOT IN (8, 17, 18, 20, 22, 23, 24, 25);

-- === Tab_PV_STAMM <- alt Tab_PV ===
INSERT INTO [Tab_PV_STAMM] ([ID], [Bezeichner], [Firma], [Beschreibung], [Leistung], [Wirkungsgrad], [U_Mpp], [U_Leerlauf], [I_Mpp], [I_Kurzschluss], [alpha_SC], [beta_OC], [gamma_PMP], [T_NOCT], [Laenge], [Breite], [Modulkosten], [ReadOnly])
SELECT [ID], [Modulname], [Firma], [Beschreibung], [Leistung], [Wirkungsgrad], [U_Mpp], [U_Leerlauf], [I_Mpp], [I_Kurzschluss], [alpha_SC], [beta_OC], [gamma_PMP], [T_NOCT], [Laenge], [Breite], [Modulkosten], FALSE
FROM [{{QUELLE}}].[Tab_PV];

-- === Tab_Solarkollektoren_STAMM <- alt Tab_Solarkollektoren ===
INSERT INTO [Tab_Solarkollektoren_STAMM] ([ID], [Bezeichner], [Firma], [Beschreibung], [Kollektortyp], [Modulflaeche], [Aperturflaeche], [h0], [k1], [k2], [Kdir], [Kdfu], [Investitionskosten], [Vorlauf], [Ruecklauf], [ReadOnly])
SELECT [ID], [Kollektorname], [Firma], [Beschreibung], [Kollektortyp], [Modulflaeche], [Aperturflaeche], [h0], [k1], [k2], [Kdir], [Kdfu], [Investitionskosten], [Vorlauf], [Ruecklauf], FALSE
FROM [{{QUELLE}}].[Tab_Solarkollektoren];

-- === Tab_Stromspeicher_STAMM <- alt Tab_Stromspeicher ===
INSERT INTO [Tab_Stromspeicher_STAMM] ([ID], [Bezeichner], [Typ], [Leistung], [Energie], [Degradation], [Ladezustand], [Modulkosten], [ReadOnly])
SELECT [ID], [Bezeichner], [Typ], [Leistung], [Energie], [Degradation], [Ladezustand], [Modulkosten], FALSE
FROM [{{QUELLE}}].[Tab_Stromspeicher];

-- === Tab_Pufferspeicher_STAMM <- alt Tab_Pufferspeicher ===
INSERT INTO [Tab_Pufferspeicher_STAMM] ([ID], [Bezeichner], [Hersteller], [Speichertyp], [Bereitschaftsverluste], [Gesamtvolumen], [Investitionskosten], [ReadOnly])
SELECT [ID], [Bezeichner], [Hersteller], [Speichertyp], [Bereitschaftsverluste], [Gesamtvolumen], [Investitionskosten], FALSE
FROM [{{QUELLE}}].[Tab_Pufferspeicher];

-- === Tab_Stromverbraucher_STAMM <- alt Tab_Stromverbraucher ===
INSERT INTO [Tab_Stromverbraucher_STAMM] ([ID], [Bezeichner], [Typ], [Beschreibung], [Monat_1], [Monat_2], [Monat_3], [Monat_4], [Monat_5], [Monat_6], [Monat_7], [Monat_8], [Monat_9], [Monat_10], [Monat_11], [Monat_12], [ReadOnly])
SELECT [ID], [Bezeichner], [Typ], [Beschreibung], [Monat_1], [Monat_2], [Monat_3], [Monat_4], [Monat_5], [Monat_6], [Monat_7], [Monat_8], [Monat_9], [Monat_10], [Monat_11], [Monat_12], FALSE
FROM [{{QUELLE}}].[Tab_Stromverbraucher];

-- === Tab_Stromverbrauchertyp_STAMM <- alt Tab_Stromverbrauchertyp ===
INSERT INTO [Tab_Stromverbrauchertyp_STAMM] ([ID], [Typname], [Beschreibung], [1], [2], [3], [4], [5], [6], [7], [8], [9], [10], [11], [12], [13], [14], [15], [16], [17], [18], [19], [20], [21], [22], [23], [24], [25], [26], [27], [28], [29], [30], [31], [32], [33], [34], [35], [36], [37], [38], [39], [40], [41], [42], [43], [44], [45], [46], [47], [48], [49], [50], [51], [52], [53], [54], [55], [56], [57], [58], [59], [60], [61], [62], [63], [64], [65], [66], [67], [68], [69], [70], [71], [72], [73], [74], [75], [76], [77], [78], [79], [80], [81], [82], [83], [84], [85], [86], [87], [88], [89], [90], [91], [92], [93], [94], [95], [96], [97], [98], [99], [100], [101], [102], [103], [104], [105], [106], [107], [108], [109], [110], [111], [112], [113], [114], [115], [116], [117], [118], [119], [120], [121], [122], [123], [124], [125], [126], [127], [128], [129], [130], [131], [132], [133], [134], [135], [136], [137], [138], [139], [140], [141], [142], [143], [144], [145], [146], [147], [148], [149], [150], [151], [152], [153], [154], [155], [156], [157], [158], [159], [160], [161], [162], [163], [164], [165], [166], [167], [168], [ReadOnly])
SELECT [ID], [Typname], [Beschreibung], [1], [2], [3], [4], [5], [6], [7], [8], [9], [10], [11], [12], [13], [14], [15], [16], [17], [18], [19], [20], [21], [22], [23], [24], [25], [26], [27], [28], [29], [30], [31], [32], [33], [34], [35], [36], [37], [38], [39], [40], [41], [42], [43], [44], [45], [46], [47], [48], [49], [50], [51], [52], [53], [54], [55], [56], [57], [58], [59], [60], [61], [62], [63], [64], [65], [66], [67], [68], [69], [70], [71], [72], [73], [74], [75], [76], [77], [78], [79], [80], [81], [82], [83], [84], [85], [86], [87], [88], [89], [90], [91], [92], [93], [94], [95], [96], [97], [98], [99], [100], [101], [102], [103], [104], [105], [106], [107], [108], [109], [110], [111], [112], [113], [114], [115], [116], [117], [118], [119], [120], [121], [122], [123], [124], [125], [126], [127], [128], [129], [130], [131], [132], [133], [134], [135], [136], [137], [138], [139], [140], [141], [142], [143], [144], [145], [146], [147], [148], [149], [150], [151], [152], [153], [154], [155], [156], [157], [158], [159], [160], [161], [162], [163], [164], [165], [166], [167], [168], FALSE
FROM [{{QUELLE}}].[Tab_Stromverbrauchertyp];

-- === Tab_Brauchwasser_STAMM <- alt Tab_Brauchwasser   (ReadOnly-Zeilen der Vorlage bleiben: 2) ===
INSERT INTO [Tab_Brauchwasser_STAMM] ([ID], [Bezeichner], [Typ], [Beschreibung], [Monat_1], [Monat_2], [Monat_3], [Monat_4], [Monat_5], [Monat_6], [Monat_7], [Monat_8], [Monat_9], [Monat_10], [Monat_11], [Monat_12], [ReadOnly])
SELECT [ID], [Bezeichner], [Typ], [Beschreibung], [Monat_1], [Monat_2], [Monat_3], [Monat_4], [Monat_5], [Monat_6], [Monat_7], [Monat_8], [Monat_9], [Monat_10], [Monat_11], [Monat_12], FALSE
FROM [{{QUELLE}}].[Tab_Brauchwasser] WHERE [ID] NOT IN (94, 95);

-- === Tab_Brauchwassertyp_STAMM <- alt Tab_Brauchwassertyp ===
INSERT INTO [Tab_Brauchwassertyp_STAMM] ([ID], [Bezeichner], [Beschreibung], [1], [2], [3], [4], [5], [6], [7], [8], [9], [10], [11], [12], [13], [14], [15], [16], [17], [18], [19], [20], [21], [22], [23], [24], [25], [26], [27], [28], [29], [30], [31], [32], [33], [34], [35], [36], [37], [38], [39], [40], [41], [42], [43], [44], [45], [46], [47], [48], [49], [50], [51], [52], [53], [54], [55], [56], [57], [58], [59], [60], [61], [62], [63], [64], [65], [66], [67], [68], [69], [70], [71], [72], [73], [74], [75], [76], [77], [78], [79], [80], [81], [82], [83], [84], [85], [86], [87], [88], [89], [90], [91], [92], [93], [94], [95], [96], [97], [98], [99], [100], [101], [102], [103], [104], [105], [106], [107], [108], [109], [110], [111], [112], [113], [114], [115], [116], [117], [118], [119], [120], [121], [122], [123], [124], [125], [126], [127], [128], [129], [130], [131], [132], [133], [134], [135], [136], [137], [138], [139], [140], [141], [142], [143], [144], [145], [146], [147], [148], [149], [150], [151], [152], [153], [154], [155], [156], [157], [158], [159], [160], [161], [162], [163], [164], [165], [166], [167], [168], [ReadOnly])
SELECT [ID], [Typname], [Beschreibung], [1], [2], [3], [4], [5], [6], [7], [8], [9], [10], [11], [12], [13], [14], [15], [16], [17], [18], [19], [20], [21], [22], [23], [24], [25], [26], [27], [28], [29], [30], [31], [32], [33], [34], [35], [36], [37], [38], [39], [40], [41], [42], [43], [44], [45], [46], [47], [48], [49], [50], [51], [52], [53], [54], [55], [56], [57], [58], [59], [60], [61], [62], [63], [64], [65], [66], [67], [68], [69], [70], [71], [72], [73], [74], [75], [76], [77], [78], [79], [80], [81], [82], [83], [84], [85], [86], [87], [88], [89], [90], [91], [92], [93], [94], [95], [96], [97], [98], [99], [100], [101], [102], [103], [104], [105], [106], [107], [108], [109], [110], [111], [112], [113], [114], [115], [116], [117], [118], [119], [120], [121], [122], [123], [124], [125], [126], [127], [128], [129], [130], [131], [132], [133], [134], [135], [136], [137], [138], [139], [140], [141], [142], [143], [144], [145], [146], [147], [148], [149], [150], [151], [152], [153], [154], [155], [156], [157], [158], [159], [160], [161], [162], [163], [164], [165], [166], [167], [168], FALSE
FROM [{{QUELLE}}].[Tab_Brauchwassertyp];

-- === Tab_Prozesswaerme_STAMM <- alt Tab_Prozesswaerme ===
INSERT INTO [Tab_Prozesswaerme_STAMM] ([ID], [Bezeichner], [Typ], [Beschreibung], [Monat_1], [Monat_2], [Monat_3], [Monat_4], [Monat_5], [Monat_6], [Monat_7], [Monat_8], [Monat_9], [Monat_10], [Monat_11], [Monat_12], [ReadOnly])
SELECT [ID], [Prozessname], [Typ], [Beschreibung], [Monat_1], [Monat_2], [Monat_3], [Monat_4], [Monat_5], [Monat_6], [Monat_7], [Monat_8], [Monat_9], [Monat_10], [Monat_11], [Monat_12], FALSE
FROM [{{QUELLE}}].[Tab_Prozesswaerme];

-- === Tab_Prozesstyp_STAMM <- alt Tab_Prozesstyp ===
INSERT INTO [Tab_Prozesstyp_STAMM] ([ID], [Bezeichner], [Beschreibung], [1], [2], [3], [4], [5], [6], [7], [8], [9], [10], [11], [12], [13], [14], [15], [16], [17], [18], [19], [20], [21], [22], [23], [24], [25], [26], [27], [28], [29], [30], [31], [32], [33], [34], [35], [36], [37], [38], [39], [40], [41], [42], [43], [44], [45], [46], [47], [48], [49], [50], [51], [52], [53], [54], [55], [56], [57], [58], [59], [60], [61], [62], [63], [64], [65], [66], [67], [68], [69], [70], [71], [72], [73], [74], [75], [76], [77], [78], [79], [80], [81], [82], [83], [84], [85], [86], [87], [88], [89], [90], [91], [92], [93], [94], [95], [96], [97], [98], [99], [100], [101], [102], [103], [104], [105], [106], [107], [108], [109], [110], [111], [112], [113], [114], [115], [116], [117], [118], [119], [120], [121], [122], [123], [124], [125], [126], [127], [128], [129], [130], [131], [132], [133], [134], [135], [136], [137], [138], [139], [140], [141], [142], [143], [144], [145], [146], [147], [148], [149], [150], [151], [152], [153], [154], [155], [156], [157], [158], [159], [160], [161], [162], [163], [164], [165], [166], [167], [168], [ReadOnly])
SELECT [ID], [Typname], [Beschreibung], [1], [2], [3], [4], [5], [6], [7], [8], [9], [10], [11], [12], [13], [14], [15], [16], [17], [18], [19], [20], [21], [22], [23], [24], [25], [26], [27], [28], [29], [30], [31], [32], [33], [34], [35], [36], [37], [38], [39], [40], [41], [42], [43], [44], [45], [46], [47], [48], [49], [50], [51], [52], [53], [54], [55], [56], [57], [58], [59], [60], [61], [62], [63], [64], [65], [66], [67], [68], [69], [70], [71], [72], [73], [74], [75], [76], [77], [78], [79], [80], [81], [82], [83], [84], [85], [86], [87], [88], [89], [90], [91], [92], [93], [94], [95], [96], [97], [98], [99], [100], [101], [102], [103], [104], [105], [106], [107], [108], [109], [110], [111], [112], [113], [114], [115], [116], [117], [118], [119], [120], [121], [122], [123], [124], [125], [126], [127], [128], [129], [130], [131], [132], [133], [134], [135], [136], [137], [138], [139], [140], [141], [142], [143], [144], [145], [146], [147], [148], [149], [150], [151], [152], [153], [154], [155], [156], [157], [158], [159], [160], [161], [162], [163], [164], [165], [166], [167], [168], FALSE
FROM [{{QUELLE}}].[Tab_Prozesstyp];

-- === Tab_DBTagV_STAMM <- alt Tab_DBTagV ===
INSERT INTO [Tab_DBTagV_STAMM] ([ID], [Bezeichner], [Beschreibung], [Veraenderbar], [ReadOnly])
SELECT [ID], [Name], [Beschreibung], [Veraenderbar], FALSE
FROM [{{QUELLE}}].[Tab_DBTagV];

-- === Tab_DBTagVDaten_STAMM <- alt Tab_DBTagVDaten ===
INSERT INTO [Tab_DBTagVDaten_STAMM] ([ID], [ID_TagV], [Verteilung], [ReadOnly])
SELECT [ID], [ID_TagV], [Verteilung], FALSE
FROM [{{QUELLE}}].[Tab_DBTagVDaten];

-- === Tab_Waermebedarf_STAMM <- alt Tab_Waermebedarf   (ReadOnly-Zeilen der Vorlage bleiben: 3) ===
INSERT INTO [Tab_Waermebedarf_STAMM] ([ID], [Bezeichner], [ReadOnly])
SELECT [ID], [Bezeichner], FALSE
FROM [{{QUELLE}}].[Tab_Waermebedarf] WHERE [ID] NOT IN (1, 2, 4);

-- === Tab_Stromganglinie_STAMM <- alt Tab_Stromganglinie ===
INSERT INTO [Tab_Stromganglinie_STAMM] ([ID], [Bezeichner], [Zeitinterval], [ReadOnly])
SELECT [ID], [Bezeichner], [Zeitinterval], FALSE
FROM [{{QUELLE}}].[Tab_Stromganglinie];

-- === Tab_Solarganglinie_STAMM <- alt Tab_Solarganglinie ===
INSERT INTO [Tab_Solarganglinie_STAMM] ([ID], [Bezeichner], [Beschreibung], [ReadOnly])
SELECT [ID], [Bezeichner], [Beschreibung], FALSE
FROM [{{QUELLE}}].[Tab_Solarganglinie];

-- === Tab_Kenndaten_STAMM / _Kuehlung_STAMM <- alt (nur Nicht-ReadOnly-WPs; ID +100000) ===
INSERT INTO [Tab_Kenndaten_STAMM] ([ID], [ID_WP], [Vorlauf], [Temperatur], [COP], [Ptherm], [ReadOnly])
SELECT [ID]+100000, [ID_WP], [Vorlauf], [Temperatur], [COP], [Ptherm], FALSE
FROM [{{QUELLE}}].[Tab_Kenndaten] WHERE [ID_WP] NOT IN (8, 17, 18, 20, 22, 23, 24, 25);

INSERT INTO [Tab_Kenndaten_Kuehlung_STAMM] ([ID], [ID_WP], [Vorlauf], [Temperatur], [COP], [Pkuehl], [Last])
SELECT [ID]+100000, [ID_WP], [Vorlauf], [Temperatur], [COP], [Pkuehl], [Last]
FROM [{{QUELLE}}].[Tab_Kenndaten_Kuehlung] WHERE [ID_WP] NOT IN (8, 17, 18, 20, 22, 23, 24, 25);

-- === Klima-STAMM <- alt (Regel ohne Ausnahme; Spalten identisch) ===
INSERT INTO [Tab_Klimaregion_STAMM] ([ID_Klimaregion], [Name], [Longitude], [Latitude], [Details], [ReadOnly])
SELECT [ID_Klimaregion], [Name], [Longitude], [Latitude], [Details], FALSE
FROM [{{QUELLE}}].[Tab_Klimaregion];

INSERT INTO [Tab_Klimadaten_STAMM] ([ID_Klimadaten], [ID_Klimaregion], [Sol_Nord], [Sol_Ost], [Sol_Sued], [Sol_West], [Temperatur], [WE], [TagTyp_W], [TagTyp_NW], [Globalstrahlung], [Direktstrahlung], [Diffusstrahlung], [Sonnenwinkel])
SELECT [ID_Klimadaten], [ID_Klimaregion], [Sol_Nord], [Sol_Ost], [Sol_Sued], [Sol_West], [Temperatur], [WE], [TagTyp_W], [TagTyp_NW], [Globalstrahlung], [Direktstrahlung], [Diffusstrahlung], [Sonnenwinkel]
FROM [{{QUELLE}}].[Tab_Klimadaten];

INSERT INTO [Tab_Solar_STAMM] ([ID], [ID_Klimaregion], [Temperatur], [Sol_Nord], [Sol_Ost], [Sol_Sued], [Sol_West], [Globalstrahlung], [Direktstrahlung], [Diffusstrahlung], [Sonnenwinkel])
SELECT [ID], [ID_Klimaregion], [Temperatur], [Sol_Nord], [Sol_Ost], [Sol_Sued], [Sol_West], [Globalstrahlung], [Direktstrahlung], [Diffusstrahlung], [Sonnenwinkel]
FROM [{{QUELLE}}].[Tab_Solar];

-- === Ganglinien-Daten -> STAMM (alt gruppiert ueber ID_GanglinieDaten) ===
INSERT INTO [Tab_StromganglinieDaten_STAMM] ([ID], [ID_Ganglinie], [Wert], [ReadOnly])
SELECT gd.[ID_GanglinieDaten], g.[ID], gd.[Wert], FALSE
FROM [{{QUELLE}}].[Tab_StromganglinieDaten] AS gd
INNER JOIN [{{QUELLE}}].[Tab_Stromganglinie] AS g ON gd.[ID_Ganglinie] = g.[ID_GanglinieDaten];

INSERT INTO [Tab_SolarganglinieDaten_STAMM] ([ID], [ID_Ganglinie], [Wert], [ReadOnly])
SELECT gd.[ID], g.[ID], gd.[Wert], FALSE
FROM [{{QUELLE}}].[Tab_SolarganglinieDaten] AS gd
INNER JOIN [{{QUELLE}}].[Tab_Solarganglinie] AS g ON gd.[ID_GanglinieDaten] = g.[ID_GanglinieDaten];

INSERT INTO [Tab_WaermebedarfDaten_STAMM] ([ID], [ID_Ganglinie], [Wert], [ReadOnly])
SELECT gd.[ID]+1000000, w.[ID], gd.[Wert], FALSE
FROM [{{QUELLE}}].[Tab_WaermebedarfDaten] AS gd
INNER JOIN [{{QUELLE}}].[Tab_Waermebedarf] AS w ON gd.[ID_GanglinieDaten] = w.[ID_GanglinieDaten] WHERE w.[ID] NOT IN (1, 2, 4);

-- --------------------------------------------------------------------------
-- Teil B: Projektdaten der gewaehlten Projekte in die neue Struktur
-- --------------------------------------------------------------------------

-- B.1  Zu ERSETZENDE Ziel-Projekte samt Daten entfernen (Kinder zuerst;
--      CASCADE-Beziehungen raeumen abhaengige Zeilen automatisch mit ab).
DELETE FROM [Tab_Energieanlagen] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Kenndaten] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Kenndaten_Kuehlung] WHERE [ID_WP] IN (SELECT w.[ID] FROM [Tab_WP] AS w WHERE w.[ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}}));
DELETE FROM [Tab_WP] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Heizkessel] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_BHKW] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_PV] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Solarkollektoren] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Stromspeicher] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Z_ProjektPufferSp] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Pufferspeicher] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Z_Projekt_Stromverbraucher] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Stromverbraucher] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Z_Projekt_Brauchwasser] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Brauchwassertyp] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Brauchwasser] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Z_Projekt_Prozesswaerme] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Prozesstyp] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Prozesswaerme] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Z_ProjektStromganglinie] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Stromganglinie] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Z_ProjektSolarganglinie] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Solarganglinie] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Z_ProjektWaermebedarf] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Waermebedarf] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Z_ProjektGebaeude] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Gebaeude] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
-- Tab_Brennstoff_Projekt: DELETE entfernt 19.08.2026 - Altweg ohne C#-Zugriff
--   (Konzept Kosten/Energietraeger, HF1). Die Tabelle wird in Etappe K6
--   (Migrationsschritt M-E) gedroppt.
DELETE FROM [Tab_ProjektWerte] WHERE [ProjektID] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [energy_price] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [energy_project_settings] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [energy_conversion];
DELETE FROM [Tab_Einstellungen] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Solar] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Klimadaten] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Klimaregion] WHERE [ID_Projekt] IN ({{PROJEKTE_ZIEL_LOESCHEN}});
DELETE FROM [Tab_Projekt] WHERE [ID] IN ({{PROJEKTE_ZIEL_LOESCHEN}});

-- B.2  Gewaehlte Quell-Projekte einfuegen (Name ggf. mit Suffix, ID ggf.
--      mit Versatz - beides aus dem Konfliktdialog).
INSERT INTO [Tab_Projekt] ([ID], [Projektname], [Bearbeiter], [Beschreibung], [Kunde], [Aenderungsdatum], [ID_Klimaregion], [Erstelldatum])
SELECT (q.[ID]+{{PROJEKT_OFFSET}}), q.[Projektname] & '{{SUFFIX_ALT}}', q.[Bearbeiter], q.[Beschreibung], q.[Kunde], q.[Aenderungsdatum], (q.[ID]+{{PROJEKT_OFFSET}})*1000 + q.[ID_Klimaregion], q.[Erstelldatum]
FROM [{{QUELLE}}].[Tab_Projekt] AS q WHERE q.[ID] IN ({{PROJEKTE_QUELLE}});

-- B.2b Klima je Projekt (KEINE Ausnahme mehr): Region-Kopie des im Projekt
--      referenzierten Klimastandorts inkl. Klimadaten und Solar-Zeitreihe.
INSERT INTO [Tab_Klimaregion] ([ID], [ID_Projekt], [Bezeichner], [Longitude], [Latitude], [Details])
SELECT (q.[ID]+{{PROJEKT_OFFSET}})*1000 + r.[ID_Klimaregion], (q.[ID]+{{PROJEKT_OFFSET}}), r.[Name], r.[Longitude], r.[Latitude], r.[Details]
FROM [{{QUELLE}}].[Tab_Projekt] AS q
INNER JOIN [{{QUELLE}}].[Tab_Klimaregion] AS r ON q.[ID_Klimaregion] = r.[ID_Klimaregion]
WHERE q.[ID] IN ({{PROJEKTE_QUELLE}});

INSERT INTO [Tab_Klimadaten] ([ID], [ID_Projekt], [ID_Klimaregion], [Sol_Nord], [Sol_Ost], [Sol_Sued], [Sol_West], [Temperatur], [WE], [TagTyp_W], [TagTyp_NW], [Globalstrahlung], [Direktstrahlung], [Diffusstrahlung], [Sonnenwinkel])
SELECT (q.[ID]+{{PROJEKT_OFFSET}})*100000 + k.[ID_Klimadaten], (q.[ID]+{{PROJEKT_OFFSET}}), (q.[ID]+{{PROJEKT_OFFSET}})*1000 + k.[ID_Klimaregion], k.[Sol_Nord], k.[Sol_Ost], k.[Sol_Sued], k.[Sol_West], k.[Temperatur], k.[WE], k.[TagTyp_W], k.[TagTyp_NW], k.[Globalstrahlung], k.[Direktstrahlung], k.[Diffusstrahlung], k.[Sonnenwinkel]
FROM [{{QUELLE}}].[Tab_Projekt] AS q
INNER JOIN [{{QUELLE}}].[Tab_Klimadaten] AS k ON q.[ID_Klimaregion] = k.[ID_Klimaregion]
WHERE q.[ID] IN ({{PROJEKTE_QUELLE}});

INSERT INTO [Tab_Solar] ([ID], [ID_Projekt], [ID_Klimaregion], [Temperatur], [Sol_Nord], [Sol_Ost], [Sol_Sued], [Sol_West], [Globalstrahlung], [Direktstrahlung], [Diffusstrahlung], [Sonnenwinkel])
SELECT (q.[ID]+{{PROJEKT_OFFSET}})*1000000 + s.[ID], (q.[ID]+{{PROJEKT_OFFSET}}), (q.[ID]+{{PROJEKT_OFFSET}})*1000 + s.[ID_Klimaregion], s.[Temperatur], s.[Sol_Nord], s.[Sol_Ost], s.[Sol_Sued], s.[Sol_West], s.[Globalstrahlung], s.[Direktstrahlung], s.[Diffusstrahlung], s.[Sonnenwinkel]
FROM [{{QUELLE}}].[Tab_Projekt] AS q
INNER JOIN [{{QUELLE}}].[Tab_Solar] AS s ON q.[ID_Klimaregion] = s.[ID_Klimaregion]
WHERE q.[ID] IN ({{PROJEKTE_QUELLE}});

-- B.3  Komponenten-Kopien je (Projekt, Komponente) aus Tab_Energieanlagen.
--      Kopie-ID = (ID_Projekt+Versatz)*1000 + alte ID.
-- Tab_WP <- alt Tab_WP (Referenzen ueber Tab_Energieanlagen.ID_WP)
INSERT INTO [Tab_WP] ([ID], [Bezeichner], [ID_Projekt], [Firma], [Beschreibung], [Typ], [Baujahr], [Aufstellung], [Nennleistung], [maxPtherm], [Heizung], [Regelung], [Modulkosten], [Laenge], [Breite], [Hoehe], [Gewicht], [Raum], [Kuehlleistung], [Bauart])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + k.[ID_WP], k.[WPName], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), k.[Firma], k.[Beschreibung], k.[Typ], k.[Baujahr], k.[Aufstellung], k.[Nennleistung], k.[maxPtherm], k.[Heizung], k.[Regelung], k.[Modulkosten], k.[Laenge], k.[Breite], k.[Hoehe], k.[Gewicht], k.[Raum], k.[Kuehlleistung], k.[Bauart]
FROM [{{QUELLE}}].[Tab_WP] AS k
INNER JOIN (SELECT DISTINCT ea.[ID_Projekt], ea.[ID_WP] FROM [{{QUELLE}}].[Tab_Energieanlagen] AS ea WHERE ea.[ID_WP] IS NOT NULL AND ea.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x
        ON k.[ID_WP] = x.[ID_WP];

-- Tab_Heizkessel <- alt Tab_Heizkessel (Referenzen ueber Tab_Energieanlagen.ID_Kessel)
INSERT INTO [Tab_Heizkessel] ([ID], [ID_Projekt], [Bezeichner], [Firma], [Beschreibung], [Ptherm], [Brennstoff], [Wirkungsgrad_Gas], [Wirkungsgrad_Öl], [Investitionskosten], [Raumbedarf], [Wartungskosten], [Nutzungsdauer], [CO2], [SO2], [NOx], [CO], [Staub], [Betriebsbereitschaftverlust], [Brennwert])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + k.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), k.[Name], k.[Firma], k.[Beschreibung], k.[Ptherm], k.[Brennstoff], k.[Wirkungsgrad_Gas], k.[Wirkungsgrad_Öl], k.[Investitionskosten], k.[Raumbedarf], k.[Wartungskosten], k.[Nutzungsdauer], k.[CO2], k.[SO2], k.[NOx], k.[CO], k.[Staub], k.[Betriebsbereitschaftverlust], k.[Brennwert]
FROM [{{QUELLE}}].[Tab_Heizkessel] AS k
INNER JOIN (SELECT DISTINCT ea.[ID_Projekt], ea.[ID_Kessel] FROM [{{QUELLE}}].[Tab_Energieanlagen] AS ea WHERE ea.[ID_Kessel] IS NOT NULL AND ea.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x
        ON k.[ID] = x.[ID_Kessel];

-- Tab_BHKW <- alt Tab_BHKW (Referenzen ueber Tab_Energieanlagen.ID_BHKW)
INSERT INTO [Tab_BHKW] ([ID], [ID_Projekt], [Bezeichner], [Firma], [Beschreibung], [Ptherm], [Pel], [Brennstoff], [Wirkungsgrad], [Investition_kwel], [Raumbedarf], [Wartungskosten_kwhel], [Nutzungsdauer], [NOX], [SO2], [CO], [CO2], [Staub], [Motortyp], [Grenzleistung], [Kosten_Modul], [Kosten_Montage], [Kosten_Lieferung], [Kosten_Schallschutzhaube], [Kosten_Abgasreinigung], [Vorlauf], [Ruecklauf])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + k.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), k.[Bezeichner], k.[Firma], k.[Beschreibung], k.[Ptherm], k.[Pel], k.[Brennstoff], k.[Wirkungsgrad], k.[Investition_kwel], k.[Raumbedarf], k.[Wartungskosten_kwhel], k.[Nutzungsdauer], k.[NOX], k.[SO2], k.[CO], k.[CO2], k.[Staub], k.[Motortyp], k.[Grenzleistung], k.[Kosten_Modul], k.[Kosten_Montage], k.[Kosten_Lieferung], k.[Kosten_Schallschutzhaube], k.[Kosten_Abgasreinigung], k.[Vorlauf], k.[Rücklauf]
FROM [{{QUELLE}}].[Tab_BHKW] AS k
INNER JOIN (SELECT DISTINCT ea.[ID_Projekt], ea.[ID_BHKW] FROM [{{QUELLE}}].[Tab_Energieanlagen] AS ea WHERE ea.[ID_BHKW] IS NOT NULL AND ea.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x
        ON k.[ID] = x.[ID_BHKW];

-- Tab_PV <- alt Tab_PV (Referenzen ueber Tab_Energieanlagen.ID_PV)
INSERT INTO [Tab_PV] ([ID], [ID_Projekt], [Bezeichner], [Firma], [Beschreibung], [Leistung], [Wirkungsgrad], [U_Mpp], [U_Leerlauf], [I_Mpp], [I_Kurzschluss], [alpha_SC], [beta_OC], [gamma_PMP], [T_NOCT], [Laenge], [Breite], [Modulkosten])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + k.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), k.[Modulname], k.[Firma], k.[Beschreibung], k.[Leistung], k.[Wirkungsgrad], k.[U_Mpp], k.[U_Leerlauf], k.[I_Mpp], k.[I_Kurzschluss], k.[alpha_SC], k.[beta_OC], k.[gamma_PMP], k.[T_NOCT], k.[Laenge], k.[Breite], k.[Modulkosten]
FROM [{{QUELLE}}].[Tab_PV] AS k
INNER JOIN (SELECT DISTINCT ea.[ID_Projekt], ea.[ID_PV] FROM [{{QUELLE}}].[Tab_Energieanlagen] AS ea WHERE ea.[ID_PV] IS NOT NULL AND ea.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x
        ON k.[ID] = x.[ID_PV];

-- Tab_Solarkollektoren <- alt Tab_Solarkollektoren (Referenzen ueber Tab_Energieanlagen.ID_Solar)
INSERT INTO [Tab_Solarkollektoren] ([ID], [ID_Projekt], [Bezeichner], [Firma], [Beschreibung], [Kollektortyp], [Modulflaeche], [Aperturflaeche], [h0], [k1], [k2], [Kdir], [Kdfu], [Investitionskosten], [Vorlauf], [Ruecklauf])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + k.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), k.[Kollektorname], k.[Firma], k.[Beschreibung], k.[Kollektortyp], k.[Modulflaeche], k.[Aperturflaeche], k.[h0], k.[k1], k.[k2], k.[Kdir], k.[Kdfu], k.[Investitionskosten], k.[Vorlauf], k.[Ruecklauf]
FROM [{{QUELLE}}].[Tab_Solarkollektoren] AS k
INNER JOIN (SELECT DISTINCT ea.[ID_Projekt], ea.[ID_Solar] FROM [{{QUELLE}}].[Tab_Energieanlagen] AS ea WHERE ea.[ID_Solar] IS NOT NULL AND ea.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x
        ON k.[ID] = x.[ID_Solar];

-- Tab_Stromspeicher <- alt Tab_Stromspeicher (Referenzen ueber Tab_Energieanlagen.ID_SP)
INSERT INTO [Tab_Stromspeicher] ([ID], [ID_Projekt], [Bezeichner], [Typ], [Leistung], [Energie], [Degradation], [Ladezustand], [Modulkosten])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + k.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), k.[Bezeichner], k.[Typ], k.[Leistung], k.[Energie], k.[Degradation], k.[Ladezustand], k.[Modulkosten]
FROM [{{QUELLE}}].[Tab_Stromspeicher] AS k
INNER JOIN (SELECT DISTINCT ea.[ID_Projekt], ea.[ID_SP] FROM [{{QUELLE}}].[Tab_Energieanlagen] AS ea WHERE ea.[ID_SP] IS NOT NULL AND ea.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x
        ON k.[ID] = x.[ID_SP];

-- Kenndaten je WP-Kopie.
INSERT INTO [Tab_Kenndaten] ([ID], [ID_Projekt], [ID_WP], [Vorlauf], [Temperatur], [COP], [Ptherm])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000000 + k.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + k.[ID_WP], k.[Vorlauf], k.[Temperatur], k.[COP], k.[Ptherm]
FROM [{{QUELLE}}].[Tab_Kenndaten] AS k
INNER JOIN (SELECT DISTINCT ea.[ID_Projekt], ea.[ID_WP] FROM [{{QUELLE}}].[Tab_Energieanlagen] AS ea WHERE ea.[ID_WP] IS NOT NULL AND ea.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x
        ON k.[ID_WP] = x.[ID_WP];

INSERT INTO [Tab_Kenndaten_Kuehlung] ([ID], [ID_WP], [Vorlauf], [Temperatur], [COP], [Pkuehl], [Last])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000000 + k.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + k.[ID_WP], k.[Vorlauf], k.[Temperatur], k.[COP], k.[Pkuehl], k.[Last]
FROM [{{QUELLE}}].[Tab_Kenndaten_Kuehlung] AS k
INNER JOIN (SELECT DISTINCT ea.[ID_Projekt], ea.[ID_WP] FROM [{{QUELLE}}].[Tab_Energieanlagen] AS ea WHERE ea.[ID_WP] IS NOT NULL AND ea.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x
        ON k.[ID_WP] = x.[ID_WP];

-- Pufferspeicher-Kopien: aus Z_ProjektPufferSp (Name-Verweis) UND Tab_Energieanlagen.ID_PUFFER.
INSERT INTO [Tab_Pufferspeicher] ([ID], [ID_Projekt], [Bezeichner], [Hersteller], [Speichertyp], [Bereitschaftsverluste], [Gesamtvolumen], [Investitionskosten])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + p.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), p.[Bezeichner], p.[Hersteller], p.[Speichertyp], p.[Bereitschaftsverluste], p.[Gesamtvolumen], p.[Investitionskosten]
FROM [{{QUELLE}}].[Tab_Pufferspeicher] AS p
INNER JOIN (SELECT DISTINCT z.[ID_Projekt], p2.[ID] AS PID FROM [{{QUELLE}}].[Z_ProjektPufferSp] AS z INNER JOIN [{{QUELLE}}].[Tab_Pufferspeicher] AS p2 ON z.[Pufferspeicher] = p2.[Bezeichner] WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x
        ON p.[ID] = x.[PID];

INSERT INTO [Tab_Pufferspeicher] ([ID], [ID_Projekt], [Bezeichner], [Hersteller], [Speichertyp], [Bereitschaftsverluste], [Gesamtvolumen], [Investitionskosten])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + p.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), p.[Bezeichner], p.[Hersteller], p.[Speichertyp], p.[Bereitschaftsverluste], p.[Gesamtvolumen], p.[Investitionskosten]
FROM [{{QUELLE}}].[Tab_Pufferspeicher] AS p
INNER JOIN (SELECT DISTINCT ea.[ID_Projekt], ea.[ID_PUFFER] FROM [{{QUELLE}}].[Tab_Energieanlagen] AS ea WHERE ea.[ID_PUFFER] IS NOT NULL AND ea.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})
            AND NOT EXISTS (SELECT 1 FROM [{{QUELLE}}].[Z_ProjektPufferSp] AS z2 INNER JOIN [{{QUELLE}}].[Tab_Pufferspeicher] AS p3 ON z2.[Pufferspeicher] = p3.[Bezeichner]
                            WHERE z2.[ID_Projekt] = ea.[ID_Projekt] AND p3.[ID] = ea.[ID_PUFFER])) AS x
        ON p.[ID] = x.[ID_PUFFER];

-- B.4  Tab_Energieanlagen mit remappten Komponenten-Referenzen (Zeilen-ID +10000).
INSERT INTO [Tab_Energieanlagen] ([ID], [ID_Projekt], [Bezeichner], [ID_Type], [ID_WP], [Betriebsart], [Sperrung], [Sperrzeit_von], [Sperrzeit_bis], [Vorlauf], [Rücklauf], [Bivalenter_Betrieb], [Abschaltpunkt], [Nutzungszeit], [ID_SP], [ID_PV], [ID_Solar], [Heizstab], [Volumen], [rendeMix], [Solaranteil], [ID_Kessel], [ID_BHKW], [Grenzleistung], [Kollektormodulanzahl], [PV_Leistung], [Neigung], [Azimut], [ID_PUFFER])
SELECT ea.[ID]+10000, (ea.[ID_Projekt]+{{PROJEKT_OFFSET}}), ea.[Bezeichner], ea.[ID_Type], IIf(ea.[ID_WP] IS NULL, NULL, (ea.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + ea.[ID_WP]), ea.[Betriebsart], ea.[Sperrung], ea.[Sperrzeit_von], ea.[Sperrzeit_bis], ea.[Vorlauf], ea.[Rücklauf], ea.[Bivalenter_Betrieb], ea.[Abschaltpunkt], ea.[Nutzungszeit], IIf(ea.[ID_SP] IS NULL, NULL, (ea.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + ea.[ID_SP]), IIf(ea.[ID_PV] IS NULL, NULL, (ea.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + ea.[ID_PV]), IIf(ea.[ID_Solar] IS NULL, NULL, (ea.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + ea.[ID_Solar]), ea.[Heizstab], ea.[Volumen], ea.[rendeMix], ea.[Solaranteil], IIf(ea.[ID_Kessel] IS NULL, NULL, (ea.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + ea.[ID_Kessel]), IIf(ea.[ID_BHKW] IS NULL, NULL, (ea.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + ea.[ID_BHKW]), ea.[Grenzleistung], ea.[Kollektormodulanzahl], ea.[PV_Leistung], ea.[Neigung], ea.[Azimut], IIf(ea.[ID_PUFFER] IS NULL, NULL, (ea.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + ea.[ID_PUFFER])
FROM [{{QUELLE}}].[Tab_Energieanlagen] AS ea WHERE ea.[ID_Projekt] IN ({{PROJEKTE_QUELLE}});

-- B.5  Pufferspeicher-Zuordnungen (neu: ID_Pufferspeicher via Name-Lookup).
INSERT INTO [Z_ProjektPufferSp] ([ID], [ID_Projekt], [ID_Pufferspeicher], [Pufferspeicher], [Erzeuger], [Vorlauf], [Ruecklauf], [Prioritaet])
SELECT z.[ID]+10000, (z.[ID_Projekt]+{{PROJEKT_OFFSET}}), (z.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + p.[ID], z.[Pufferspeicher], z.[Erzeuger], z.[Vorlauf], z.[Ruecklauf], z.[Prioritaet]
FROM [{{QUELLE}}].[Z_ProjektPufferSp] AS z
INNER JOIN [{{QUELLE}}].[Tab_Pufferspeicher] AS p ON z.[Pufferspeicher] = p.[Bezeichner]
WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}});

-- B.6  Stromverbraucher: Kopien, Typ-Kopien, Zuordnungen. Bezeichner/Typname
--      sind im Ziel GLOBAL eindeutig -> Suffix ' (P<Projekt>)'.
INSERT INTO [Tab_Stromverbraucher] ([ID], [ID_Projekt], [Bezeichner], [Typ], [Beschreibung], [Monat_1], [Monat_2], [Monat_3], [Monat_4], [Monat_5], [Monat_6], [Monat_7], [Monat_8], [Monat_9], [Monat_10], [Monat_11], [Monat_12])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + v.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), v.[Bezeichner] & ' (P' & (x.[ID_Projekt]+{{PROJEKT_OFFSET}}) & ')', v.[Typ] & ' (P' & (x.[ID_Projekt]+{{PROJEKT_OFFSET}}) & ')', v.[Beschreibung], v.[Monat_1], v.[Monat_2], v.[Monat_3], v.[Monat_4], v.[Monat_5], v.[Monat_6], v.[Monat_7], v.[Monat_8], v.[Monat_9], v.[Monat_10], v.[Monat_11], v.[Monat_12]
FROM [{{QUELLE}}].[Tab_Stromverbraucher] AS v
INNER JOIN (SELECT DISTINCT z.[ID_Projekt], z.[ID_Stromverbraucher] FROM [{{QUELLE}}].[Z_Projekt_Stromverbraucher] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x
        ON v.[ID] = x.[ID_Stromverbraucher];

INSERT INTO [Tab_Stromverbrauchertyp] ([ID], [ID_Stromverbraucher], [Typname], [Beschreibung], [1], [2], [3], [4], [5], [6], [7], [8], [9], [10], [11], [12], [13], [14], [15], [16], [17], [18], [19], [20], [21], [22], [23], [24], [25], [26], [27], [28], [29], [30], [31], [32], [33], [34], [35], [36], [37], [38], [39], [40], [41], [42], [43], [44], [45], [46], [47], [48], [49], [50], [51], [52], [53], [54], [55], [56], [57], [58], [59], [60], [61], [62], [63], [64], [65], [66], [67], [68], [69], [70], [71], [72], [73], [74], [75], [76], [77], [78], [79], [80], [81], [82], [83], [84], [85], [86], [87], [88], [89], [90], [91], [92], [93], [94], [95], [96], [97], [98], [99], [100], [101], [102], [103], [104], [105], [106], [107], [108], [109], [110], [111], [112], [113], [114], [115], [116], [117], [118], [119], [120], [121], [122], [123], [124], [125], [126], [127], [128], [129], [130], [131], [132], [133], [134], [135], [136], [137], [138], [139], [140], [141], [142], [143], [144], [145], [146], [147], [148], [149], [150], [151], [152], [153], [154], [155], [156], [157], [158], [159], [160], [161], [162], [163], [164], [165], [166], [167], [168])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + v.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + v.[ID], t.[Typname] & ' (P' & (x.[ID_Projekt]+{{PROJEKT_OFFSET}}) & ')', t.[Beschreibung], t.[1], t.[2], t.[3], t.[4], t.[5], t.[6], t.[7], t.[8], t.[9], t.[10], t.[11], t.[12], t.[13], t.[14], t.[15], t.[16], t.[17], t.[18], t.[19], t.[20], t.[21], t.[22], t.[23], t.[24], t.[25], t.[26], t.[27], t.[28], t.[29], t.[30], t.[31], t.[32], t.[33], t.[34], t.[35], t.[36], t.[37], t.[38], t.[39], t.[40], t.[41], t.[42], t.[43], t.[44], t.[45], t.[46], t.[47], t.[48], t.[49], t.[50], t.[51], t.[52], t.[53], t.[54], t.[55], t.[56], t.[57], t.[58], t.[59], t.[60], t.[61], t.[62], t.[63], t.[64], t.[65], t.[66], t.[67], t.[68], t.[69], t.[70], t.[71], t.[72], t.[73], t.[74], t.[75], t.[76], t.[77], t.[78], t.[79], t.[80], t.[81], t.[82], t.[83], t.[84], t.[85], t.[86], t.[87], t.[88], t.[89], t.[90], t.[91], t.[92], t.[93], t.[94], t.[95], t.[96], t.[97], t.[98], t.[99], t.[100], t.[101], t.[102], t.[103], t.[104], t.[105], t.[106], t.[107], t.[108], t.[109], t.[110], t.[111], t.[112], t.[113], t.[114], t.[115], t.[116], t.[117], t.[118], t.[119], t.[120], t.[121], t.[122], t.[123], t.[124], t.[125], t.[126], t.[127], t.[128], t.[129], t.[130], t.[131], t.[132], t.[133], t.[134], t.[135], t.[136], t.[137], t.[138], t.[139], t.[140], t.[141], t.[142], t.[143], t.[144], t.[145], t.[146], t.[147], t.[148], t.[149], t.[150], t.[151], t.[152], t.[153], t.[154], t.[155], t.[156], t.[157], t.[158], t.[159], t.[160], t.[161], t.[162], t.[163], t.[164], t.[165], t.[166], t.[167], t.[168]
FROM ([{{QUELLE}}].[Tab_Stromverbraucher] AS v
INNER JOIN (SELECT DISTINCT z.[ID_Projekt], z.[ID_Stromverbraucher] FROM [{{QUELLE}}].[Z_Projekt_Stromverbraucher] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x ON v.[ID] = x.[ID_Stromverbraucher])
INNER JOIN [{{QUELLE}}].[Tab_Stromverbrauchertyp] AS t ON t.[Typname] = v.[Typ];

INSERT INTO [Z_Projekt_Stromverbraucher] ([ID], [ID_Projekt], [ID_Stromverbraucher], [Bezeichner], [Summe])
SELECT z.[ID]+10000, (z.[ID_Projekt]+{{PROJEKT_OFFSET}}), (z.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + z.[ID_Stromverbraucher], z.[Bezeichner], z.[Summe]
FROM [{{QUELLE}}].[Z_Projekt_Stromverbraucher] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}});

-- B.7  Brauchwasser: Kopien, Typ-Kopien (mit ID_Projekt), Zuordnungen.
INSERT INTO [Tab_Brauchwasser] ([ID], [ID_Projekt], [Bezeichner], [Typ], [Beschreibung], [Monat_1], [Monat_2], [Monat_3], [Monat_4], [Monat_5], [Monat_6], [Monat_7], [Monat_8], [Monat_9], [Monat_10], [Monat_11], [Monat_12])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + w.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), w.[Bezeichner], w.[Typ], w.[Beschreibung], w.[Monat_1], w.[Monat_2], w.[Monat_3], w.[Monat_4], w.[Monat_5], w.[Monat_6], w.[Monat_7], w.[Monat_8], w.[Monat_9], w.[Monat_10], w.[Monat_11], w.[Monat_12]
FROM [{{QUELLE}}].[Tab_Brauchwasser] AS w
INNER JOIN (SELECT DISTINCT z.[ID_Projekt], z.[ID_Brauchwasser] FROM [{{QUELLE}}].[Z_Projekt_Brauchwasser] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x
        ON w.[ID] = x.[ID_Brauchwasser];

INSERT INTO [Tab_Brauchwassertyp] ([ID], [ID_Brauchwasser], [ID_Projekt], [Typname], [Beschreibung], [1], [2], [3], [4], [5], [6], [7], [8], [9], [10], [11], [12], [13], [14], [15], [16], [17], [18], [19], [20], [21], [22], [23], [24], [25], [26], [27], [28], [29], [30], [31], [32], [33], [34], [35], [36], [37], [38], [39], [40], [41], [42], [43], [44], [45], [46], [47], [48], [49], [50], [51], [52], [53], [54], [55], [56], [57], [58], [59], [60], [61], [62], [63], [64], [65], [66], [67], [68], [69], [70], [71], [72], [73], [74], [75], [76], [77], [78], [79], [80], [81], [82], [83], [84], [85], [86], [87], [88], [89], [90], [91], [92], [93], [94], [95], [96], [97], [98], [99], [100], [101], [102], [103], [104], [105], [106], [107], [108], [109], [110], [111], [112], [113], [114], [115], [116], [117], [118], [119], [120], [121], [122], [123], [124], [125], [126], [127], [128], [129], [130], [131], [132], [133], [134], [135], [136], [137], [138], [139], [140], [141], [142], [143], [144], [145], [146], [147], [148], [149], [150], [151], [152], [153], [154], [155], [156], [157], [158], [159], [160], [161], [162], [163], [164], [165], [166], [167], [168])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + w.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + w.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), t.[Typname], t.[Beschreibung], t.[1], t.[2], t.[3], t.[4], t.[5], t.[6], t.[7], t.[8], t.[9], t.[10], t.[11], t.[12], t.[13], t.[14], t.[15], t.[16], t.[17], t.[18], t.[19], t.[20], t.[21], t.[22], t.[23], t.[24], t.[25], t.[26], t.[27], t.[28], t.[29], t.[30], t.[31], t.[32], t.[33], t.[34], t.[35], t.[36], t.[37], t.[38], t.[39], t.[40], t.[41], t.[42], t.[43], t.[44], t.[45], t.[46], t.[47], t.[48], t.[49], t.[50], t.[51], t.[52], t.[53], t.[54], t.[55], t.[56], t.[57], t.[58], t.[59], t.[60], t.[61], t.[62], t.[63], t.[64], t.[65], t.[66], t.[67], t.[68], t.[69], t.[70], t.[71], t.[72], t.[73], t.[74], t.[75], t.[76], t.[77], t.[78], t.[79], t.[80], t.[81], t.[82], t.[83], t.[84], t.[85], t.[86], t.[87], t.[88], t.[89], t.[90], t.[91], t.[92], t.[93], t.[94], t.[95], t.[96], t.[97], t.[98], t.[99], t.[100], t.[101], t.[102], t.[103], t.[104], t.[105], t.[106], t.[107], t.[108], t.[109], t.[110], t.[111], t.[112], t.[113], t.[114], t.[115], t.[116], t.[117], t.[118], t.[119], t.[120], t.[121], t.[122], t.[123], t.[124], t.[125], t.[126], t.[127], t.[128], t.[129], t.[130], t.[131], t.[132], t.[133], t.[134], t.[135], t.[136], t.[137], t.[138], t.[139], t.[140], t.[141], t.[142], t.[143], t.[144], t.[145], t.[146], t.[147], t.[148], t.[149], t.[150], t.[151], t.[152], t.[153], t.[154], t.[155], t.[156], t.[157], t.[158], t.[159], t.[160], t.[161], t.[162], t.[163], t.[164], t.[165], t.[166], t.[167], t.[168]
FROM ([{{QUELLE}}].[Tab_Brauchwasser] AS w
INNER JOIN (SELECT DISTINCT z.[ID_Projekt], z.[ID_Brauchwasser] FROM [{{QUELLE}}].[Z_Projekt_Brauchwasser] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x ON w.[ID] = x.[ID_Brauchwasser])
INNER JOIN [{{QUELLE}}].[Tab_Brauchwassertyp] AS t ON t.[Typname] = w.[Typ];

INSERT INTO [Z_Projekt_Brauchwasser] ([ID], [ID_Projekt], [ID_Brauchwasser], [Bezeichner], [Summe])
SELECT z.[ID]+10000, (z.[ID_Projekt]+{{PROJEKT_OFFSET}}), (z.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + z.[ID_Brauchwasser], z.[Bezeichner], z.[Summe]
FROM [{{QUELLE}}].[Z_Projekt_Brauchwasser] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}});

-- B.8  Prozesswaerme: analog (derzeit 0 Zuordnungen in alt; greift kuenftig).
INSERT INTO [Tab_Prozesswaerme] ([ID], [ID_Projekt], [Bezeichner], [Typ], [Beschreibung], [Monat_1], [Monat_2], [Monat_3], [Monat_4], [Monat_5], [Monat_6], [Monat_7], [Monat_8], [Monat_9], [Monat_10], [Monat_11], [Monat_12], [ReadOnly])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + w.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), w.[Prozessname], w.[Typ], w.[Beschreibung], w.[Monat_1], w.[Monat_2], w.[Monat_3], w.[Monat_4], w.[Monat_5], w.[Monat_6], w.[Monat_7], w.[Monat_8], w.[Monat_9], w.[Monat_10], w.[Monat_11], w.[Monat_12], FALSE
FROM [{{QUELLE}}].[Tab_Prozesswaerme] AS w
INNER JOIN (SELECT DISTINCT z.[ID_Projekt], z.[ID_Prozesswaerme] FROM [{{QUELLE}}].[Z_Projekt_Prozesswaerme] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x
        ON w.[ID] = x.[ID_Prozesswaerme];

INSERT INTO [Tab_Prozesstyp] ([ID], [ID_Prozesswaerme], [ID_Projekt], [Typname], [Beschreibung], [1], [2], [3], [4], [5], [6], [7], [8], [9], [10], [11], [12], [13], [14], [15], [16], [17], [18], [19], [20], [21], [22], [23], [24], [25], [26], [27], [28], [29], [30], [31], [32], [33], [34], [35], [36], [37], [38], [39], [40], [41], [42], [43], [44], [45], [46], [47], [48], [49], [50], [51], [52], [53], [54], [55], [56], [57], [58], [59], [60], [61], [62], [63], [64], [65], [66], [67], [68], [69], [70], [71], [72], [73], [74], [75], [76], [77], [78], [79], [80], [81], [82], [83], [84], [85], [86], [87], [88], [89], [90], [91], [92], [93], [94], [95], [96], [97], [98], [99], [100], [101], [102], [103], [104], [105], [106], [107], [108], [109], [110], [111], [112], [113], [114], [115], [116], [117], [118], [119], [120], [121], [122], [123], [124], [125], [126], [127], [128], [129], [130], [131], [132], [133], [134], [135], [136], [137], [138], [139], [140], [141], [142], [143], [144], [145], [146], [147], [148], [149], [150], [151], [152], [153], [154], [155], [156], [157], [158], [159], [160], [161], [162], [163], [164], [165], [166], [167], [168], [ReadOnly])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + w.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + w.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), t.[Typname], t.[Beschreibung], t.[1], t.[2], t.[3], t.[4], t.[5], t.[6], t.[7], t.[8], t.[9], t.[10], t.[11], t.[12], t.[13], t.[14], t.[15], t.[16], t.[17], t.[18], t.[19], t.[20], t.[21], t.[22], t.[23], t.[24], t.[25], t.[26], t.[27], t.[28], t.[29], t.[30], t.[31], t.[32], t.[33], t.[34], t.[35], t.[36], t.[37], t.[38], t.[39], t.[40], t.[41], t.[42], t.[43], t.[44], t.[45], t.[46], t.[47], t.[48], t.[49], t.[50], t.[51], t.[52], t.[53], t.[54], t.[55], t.[56], t.[57], t.[58], t.[59], t.[60], t.[61], t.[62], t.[63], t.[64], t.[65], t.[66], t.[67], t.[68], t.[69], t.[70], t.[71], t.[72], t.[73], t.[74], t.[75], t.[76], t.[77], t.[78], t.[79], t.[80], t.[81], t.[82], t.[83], t.[84], t.[85], t.[86], t.[87], t.[88], t.[89], t.[90], t.[91], t.[92], t.[93], t.[94], t.[95], t.[96], t.[97], t.[98], t.[99], t.[100], t.[101], t.[102], t.[103], t.[104], t.[105], t.[106], t.[107], t.[108], t.[109], t.[110], t.[111], t.[112], t.[113], t.[114], t.[115], t.[116], t.[117], t.[118], t.[119], t.[120], t.[121], t.[122], t.[123], t.[124], t.[125], t.[126], t.[127], t.[128], t.[129], t.[130], t.[131], t.[132], t.[133], t.[134], t.[135], t.[136], t.[137], t.[138], t.[139], t.[140], t.[141], t.[142], t.[143], t.[144], t.[145], t.[146], t.[147], t.[148], t.[149], t.[150], t.[151], t.[152], t.[153], t.[154], t.[155], t.[156], t.[157], t.[158], t.[159], t.[160], t.[161], t.[162], t.[163], t.[164], t.[165], t.[166], t.[167], t.[168], FALSE
FROM ([{{QUELLE}}].[Tab_Prozesswaerme] AS w
INNER JOIN (SELECT DISTINCT z.[ID_Projekt], z.[ID_Prozesswaerme] FROM [{{QUELLE}}].[Z_Projekt_Prozesswaerme] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x ON w.[ID] = x.[ID_Prozesswaerme])
INNER JOIN [{{QUELLE}}].[Tab_Prozesstyp] AS t ON t.[Typname] = w.[Typ];

INSERT INTO [Z_Projekt_Prozesswaerme] ([ID], [ID_Projekt], [ID_Prozesswaerme], [Bezeichner], [Summe])
SELECT z.[ID]+10000, (z.[ID_Projekt]+{{PROJEKT_OFFSET}}), (z.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + z.[ID_Prozesswaerme], z.[Bezeichner], z.[Summe]
FROM [{{QUELLE}}].[Z_Projekt_Prozesswaerme] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}});

-- B.9  Ganglinien (Strom/Solar/Waermebedarf): Kopien + Daten + Zuordnungen.
INSERT INTO [Tab_Stromganglinie] ([ID], [ID_Projekt], [Bezeichner], [Zeitinterval])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + g.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), g.[Bezeichner], g.[Zeitinterval]
FROM [{{QUELLE}}].[Tab_Stromganglinie] AS g
INNER JOIN (SELECT DISTINCT z.[ID_Projekt], z.[ID_Ganglinie] FROM [{{QUELLE}}].[Z_ProjektStromganglinie] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x ON g.[ID] = x.[ID_Ganglinie];

INSERT INTO [Tab_StromganglinieDaten] ([ID], [ID_Ganglinie], [Wert])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000000 + gd.[ID_GanglinieDaten], (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + g.[ID], gd.[Wert]
FROM ([{{QUELLE}}].[Tab_Stromganglinie] AS g
INNER JOIN (SELECT DISTINCT z.[ID_Projekt], z.[ID_Ganglinie] FROM [{{QUELLE}}].[Z_ProjektStromganglinie] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x ON g.[ID] = x.[ID_Ganglinie])
INNER JOIN [{{QUELLE}}].[Tab_StromganglinieDaten] AS gd ON gd.[ID_Ganglinie] = g.[ID_GanglinieDaten];

INSERT INTO [Z_ProjektStromganglinie] ([ID], [ID_Projekt], [ID_Ganglinie], [Bezeichner])
SELECT z.[ID_Z]+10000, (z.[ID_Projekt]+{{PROJEKT_OFFSET}}), (z.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + z.[ID_Ganglinie], z.[Bezeichner]
FROM [{{QUELLE}}].[Z_ProjektStromganglinie] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}});

INSERT INTO [Tab_Solarganglinie] ([ID], [ID_Projekt], [Bezeichner], [Beschreibung])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + g.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), g.[Bezeichner], g.[Beschreibung]
FROM [{{QUELLE}}].[Tab_Solarganglinie] AS g
INNER JOIN (SELECT DISTINCT z.[ID_Projekt], z.[ID_Ganglinie] FROM [{{QUELLE}}].[Z_ProjektSolarganglinie] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x ON g.[ID] = x.[ID_Ganglinie];

INSERT INTO [Tab_SolarganglinieDaten] ([ID], [ID_Ganglinie], [Wert])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000000 + gd.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + g.[ID], gd.[Wert]
FROM ([{{QUELLE}}].[Tab_Solarganglinie] AS g
INNER JOIN (SELECT DISTINCT z.[ID_Projekt], z.[ID_Ganglinie] FROM [{{QUELLE}}].[Z_ProjektSolarganglinie] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x ON g.[ID] = x.[ID_Ganglinie])
INNER JOIN [{{QUELLE}}].[Tab_SolarganglinieDaten] AS gd ON gd.[ID_GanglinieDaten] = g.[ID_GanglinieDaten];

INSERT INTO [Z_ProjektSolarganglinie] ([ID], [ID_Projekt], [ID_Ganglinie], [Bezeichner])
SELECT z.[ID_Z]+10000, (z.[ID_Projekt]+{{PROJEKT_OFFSET}}), (z.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + z.[ID_Ganglinie], z.[Bezeichner]
FROM [{{QUELLE}}].[Z_ProjektSolarganglinie] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}});

INSERT INTO [Tab_Waermebedarf] ([ID], [ID_Projekt], [Bezeichner], [ReadOnly])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + g.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}}), g.[Bezeichner], FALSE
FROM [{{QUELLE}}].[Tab_Waermebedarf] AS g
INNER JOIN (SELECT DISTINCT z.[ID_Projekt], z.[ID_Ganglinie] FROM [{{QUELLE}}].[Z_ProjektWaermebedarf] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x ON g.[ID] = x.[ID_Ganglinie];

INSERT INTO [Tab_WaermebedarfDaten] ([ID], [ID_Ganglinie], [Wert])
SELECT (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000000 + gd.[ID], (x.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + g.[ID], gd.[Wert]
FROM ([{{QUELLE}}].[Tab_Waermebedarf] AS g
INNER JOIN (SELECT DISTINCT z.[ID_Projekt], z.[ID_Ganglinie] FROM [{{QUELLE}}].[Z_ProjektWaermebedarf] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}})) AS x ON g.[ID] = x.[ID_Ganglinie])
INNER JOIN [{{QUELLE}}].[Tab_WaermebedarfDaten] AS gd ON gd.[ID_GanglinieDaten] = g.[ID_GanglinieDaten];

INSERT INTO [Z_ProjektWaermebedarf] ([ID_Z], [ID_Projekt], [ID_Ganglinie], [Bezeichner])
SELECT z.[ID_Z]+10000, (z.[ID_Projekt]+{{PROJEKT_OFFSET}}), (z.[ID_Projekt]+{{PROJEKT_OFFSET}})*1000 + z.[ID_Ganglinie], z.[Bezeichner]
FROM [{{QUELLE}}].[Z_ProjektWaermebedarf] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}});

-- B.10 Gebaeude-Kette: Zuordnungen (+10000), Gebaeude-Kopien (ID = Zuordnungs-ID),
--      Tagesverteilung je Gebaeude aus dem alten DBTagV-Katalog (Typ = Name).
--      Gebaeude-Typen ohne Katalogeintrag (z. B. 'MFH_Niedrigenergie') erhalten
--      KEINE Tagesverteilung -> im Programm nachpflegen.
INSERT INTO [Z_ProjektGebaeude] ([ID], [ID_Projekt], [Wohnflaeche_Waermebedarf], [Einheit_Waermebedarf_Wohnflaeche], [Jahresnutzungsgrad], [dezWarmwasserbereitung])
SELECT z.[ID]+10000, (z.[ID_Projekt]+{{PROJEKT_OFFSET}}), z.[Wohnflaeche_Waermebedarf], z.[Einheit_Waermebedarf_Wohnflaeche], z.[Jahresnutzungsgrad], z.[dezWarmwasserbereitung]
FROM [{{QUELLE}}].[Z_ProjektGebaeude] AS z WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}});

INSERT INTO [Tab_Gebaeude] ([ID], [ID_ProjektGebaeude], [ID_Projekt], [Gebaeudename], [Typ], [Beschreibung], [Wohnflaeche_gesamt], [Bewohner], [Flaeche_Nutzer], [Interne_Waermegewinne], [Bauweise], [Fensterflaeche_Sued], [Fensterflaeche_Ost_West], [Fensterflaeche_Nord], [Fensterdurchlassgrad], [Raumsolltemperatur_Nachtabsenkung], [Raumsolltemperatur_Tag], [Raumsolltemperatur_Wochenende], [Raumsolltemperatur_Ferien], [Maximaleraumtemperatur], [k_Wert_Außenwand], [k_Wert_Fenster], [k_Wert_Dachflaeche], [k_Wert_Grundflaeche], [k_Wert_Sonstiges], [Flaeche_Außenwand], [gesamte_Fensterflaeche], [Dachflaeche], [Grundflaeche], [Sonstige_Flaechen], [Wohnflaeche], [Raumhoehe], [WBVK_Anschluß_Fenster_Wand], [WBVK_Anschluß_Wand_Dach], [WBVK_Anschluß_Außenwand_Kellerdecke], [Abmessung_Anschluß_Fenster_Wand], [Abmessung_Anschluß_Wand_Dach], [Abmessung_Anschluß_Außenwand_Kellerdecke], [Luftwechselrate], [Wochenende], [Ferien], [Ferienbeginn_1], [Ferienende_1], [Ferienbeginn_2], [Ferienende_2], [Ferienbeginn_3], [Ferienende_3], [Ferienbeginn_4], [Ferienende_4], [WW_Bedarf], [spez_Waermeverbrauch], [Waermebedarf], [Baualtersklasse], [Gebaeudeart], [Wohngebaeude_Nicht_Wohngebaeude])
SELECT z.[ID]+10000, z.[ID]+10000, (z.[ID_Projekt]+{{PROJEKT_OFFSET}}), g.[Gebaeudename], g.[Typ], g.[Beschreibung], g.[Wohnflaeche_gesamt], g.[Bewohner], g.[Flaeche_Nutzer], g.[Interne_Waermegewinne], g.[Bauweise], g.[Fensterflaeche_Sued], g.[Fensterflaeche_Ost_West], g.[Fensterflaeche_Nord], g.[Fensterdurchlassgrad], g.[Raumsolltemperatur_Nachtabsenkung], g.[Raumsolltemperatur_Tag], g.[Raumsolltemperatur_Wochenende], g.[Raumsolltemperatur_Ferien], g.[Maximaleraumtemperatur], g.[k_Wert_Außenwand], g.[k_Wert_Fenster], g.[k_Wert_Dachflaeche], g.[k_Wert_Grundflaeche], g.[k_Wert_Sonstiges], g.[Flaeche_Außenwand], g.[gesamte_Fensterflaeche], g.[Dachflaeche], g.[Grundflaeche], g.[Sonstige_Flaechen], g.[Wohnflaeche], g.[Raumhoehe], g.[WBVK_Anschluß_Fenster_Wand], g.[WBVK_Anschluß_Wand_Dach], g.[WBVK_Anschluß_Außenwand_Kellerdecke], g.[Abmessung_Anschluß_Fenster_Wand], g.[Abmessung_Anschluß_Wand_Dach], g.[Abmessung_Anschluß_Außenwand_Kellerdecke], g.[Luftwechselrate], g.[Wochenende], g.[Ferien], g.[Ferienbeginn_1], g.[Ferienende_1], g.[Ferienbeginn_2], g.[Ferienende_2], g.[Ferienbeginn_3], g.[Ferienende_3], g.[Ferienbeginn_4], g.[Ferienende_4], g.[WW_Bedarf], g.[spez_Waermeverbrauch], g.[Waermebedarf], g.[Baualtersklasse], g.[Gebaeudeart], g.[Wohngebaeude_Nicht_Wohngebaeude]
FROM [{{QUELLE}}].[Tab_Gebaeude] AS g
INNER JOIN [{{QUELLE}}].[Z_ProjektGebaeude] AS z ON g.[ID] = z.[ID_Gebaeude]
WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}});

INSERT INTO [Tab_DBTagV] ([ID], [ID_Gebaeude], [Bezeichner], [Beschreibung])
SELECT z.[ID]+10000, z.[ID]+10000, d.[Name], d.[Beschreibung]
FROM ([{{QUELLE}}].[Z_ProjektGebaeude] AS z
INNER JOIN [{{QUELLE}}].[Tab_Gebaeude] AS g ON g.[ID] = z.[ID_Gebaeude])
INNER JOIN [{{QUELLE}}].[Tab_DBTagV] AS d ON g.[Typ] = d.[Name]
WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}});

INSERT INTO [Tab_DBTagVDaten] ([ID], [ID_TagV], [Verteilung])
SELECT (z.[ID]+10000)*10000 + dd.[ID], z.[ID]+10000, dd.[Verteilung]
FROM (([{{QUELLE}}].[Z_ProjektGebaeude] AS z
INNER JOIN [{{QUELLE}}].[Tab_Gebaeude] AS g ON g.[ID] = z.[ID_Gebaeude])
INNER JOIN [{{QUELLE}}].[Tab_DBTagV] AS d ON g.[Typ] = d.[Name])
INNER JOIN [{{QUELLE}}].[Tab_DBTagVDaten] AS dd ON dd.[ID_TagV] = d.[ID]
WHERE z.[ID_Projekt] IN ({{PROJEKTE_QUELLE}});

-- B.11 Uebrige Projekttabellen.
-- Tab_Brennstoff_Projekt: INSERT...SELECT entfernt 19.08.2026 - Altweg ohne
--   C#-Zugriff (Konzept Kosten/Energietraeger, HF1). Die Tabelle wird in
--   Etappe K6 (Migrationsschritt M-E) gedroppt.

-- KategorieID 3 (Energiekosten) ist stillgelegt (Konzept Kosten/Energietraeger,
-- HF1/L1, 19.08.2026): EPOS-Plan schreibt keine Kategorie-3-Zeilen mehr. Die
-- Altzeilen werden per Migrationsschritt geloescht (Entscheidung E3, Etappe K6);
-- dieser Import bleibt vorerst UNVERAENDERT (kopiert KategorieID wie vorgefunden),
-- damit Migrationen von Alt-Datenbanken nicht brechen.
INSERT INTO [Tab_ProjektWerte] ([ID], [ProjektID], [StammID], [KomponentenID], [KategorieID], [EingegebenerWert], [Worstcase], [Bestcase], [Nutzungsdauer], [Worstcase_Nutzungsdauer], [Bestcase_Nutzungsdauer], [Einheit], [Gruppe])
SELECT (q.[ProjektID]+{{PROJEKT_OFFSET}})*100000 + q.[ID], (q.[ProjektID]+{{PROJEKT_OFFSET}}), q.[StammID], q.[KomponentenID], q.[KategorieID], q.[EingegebenerWert], q.[Worstcase], q.[Bestcase], q.[Nutzungsdauer], q.[Worstcase_Nutzungsdauer], q.[Bestcase_Nutzungsdauer], q.[Einheit], q.[Gruppe]
FROM [{{QUELLE}}].[Tab_ProjektWerte] AS q WHERE q.[ProjektID] IN ({{PROJEKTE_QUELLE}});

-- energy_conversion: global, Quelle gewinnt komplett (IDs bleiben, werden
-- von energy_project_settings.ID_Umrechnung referenziert).
INSERT INTO [energy_conversion] ([ID], [id_brennstoff], [from_unit], [to_unit], [factor], [user_edited])
SELECT q.[ID], q.[id_brennstoff], q.[from_unit], q.[to_unit], q.[factor], q.[user_edited]
FROM [{{QUELLE}}].[energy_conversion] AS q;

INSERT INTO [energy_price] ([id], [ID_Projekt], [carrier_id], [valid_from], [valid_to], [grundpreis], [arbeitspreis], [arbeitspreis_unit], [Heizwert], [leistungspreis], [notes])
SELECT q.[id]+10000, (q.[ID_Projekt]+{{PROJEKT_OFFSET}}), q.[carrier_id], q.[valid_from], q.[valid_to], q.[grundpreis], q.[arbeitspreis], q.[arbeitspreis_unit], q.[Heizwert], q.[leistungspreis], q.[notes]
FROM [{{QUELLE}}].[energy_price] AS q WHERE q.[ID_Projekt] IN ({{PROJEKTE_QUELLE}});

INSERT INTO [energy_project_settings] ([ID], [ID_Projekt], [ID_Energieträger], [ID_Umrechnung], [custom_hi], [custom_hs], [custom_price_work], [custom_price_base], [custom_price_power], [co2], [so2], [nox])
SELECT q.[ID]+10000, (q.[ID_Projekt]+{{PROJEKT_OFFSET}}), q.[ID_Energieträger], q.[ID_Umrechnung], q.[custom_hi], q.[custom_hs], q.[custom_price_work], q.[custom_price_base], q.[custom_price_power], q.[co2], q.[so2], q.[nox]
FROM [{{QUELLE}}].[energy_project_settings] AS q WHERE q.[ID_Projekt] IN ({{PROJEKTE_QUELLE}});

INSERT INTO [Tab_Einstellungen] ([ID], [ID_Projekt], [BHKW_Grenzleistung], [Netzverluste], [NetzverlusteEinheit], [WP_Heizstab], [Kessel_Betriebsbereitschaft], [Tool_1], [Tool_2], [Tool_3], [Tool_4], [Tool_5], [Tool_6], [Ladefuellstand_Min], [Ladefuellstand_Max], [Ladeleistung_Max], [Ladefuellstand_Min_Auswahl], [Ladefuellstand_Max_Auswahl], [Ladeleistung_Max_Auswahl], [Ladeschwellwert], [Betriebsart], [Leistungsgrenze], [Pendelspeicher])
SELECT q.[ID]+10000, (q.[ID_Projekt]+{{PROJEKT_OFFSET}}), q.[BHKW_Grenzleistung], q.[Netzverluste], q.[NetzverlusteEinheit], q.[WP_Heizstab], q.[Kessel_Betriebsbereitschaft], q.[Tool_1], q.[Tool_2], q.[Tool_3], q.[Tool_4], q.[Tool_5], q.[Tool_6], q.[Ladefuellstand_Min], q.[Ladefuellstand_Max], q.[Ladeleistung_Max], q.[Ladefuellstand_Min_Auswahl], q.[Ladefuellstand_Max_Auswahl], q.[Ladeleistung_Max_Auswahl], q.[Ladeschwellwert], q.[Betriebsart], q.[Leistungsgrenze], q.[Pendelspeicher]
FROM [{{QUELLE}}].[Tab_Einstellungen] AS q WHERE q.[ID_Projekt] IN ({{PROJEKTE_QUELLE}});

-- NICHT migriert (bewusst): Tab_Applikation (Einzel-Zeilen-Statustabelle;
-- Uebernahme ergaebe doppelte ID=1 und Fehler 3022 beim Projekt-Oeffnen),
-- Tab_Typ_Energieanlagen (neue Typenliste deckt alle verwendeten Typen ab),
-- Ergebnis-Tabellen (neu, leer).
