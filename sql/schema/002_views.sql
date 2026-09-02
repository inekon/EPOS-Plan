-- 002_views.sql - EPOS-Plan, Zielschema SQLite (Arbeitspaket S2)
-- Erzeugt von sql/tools/Erzeuge-Schema.ps1, Quellenstand 2026-09-01 15:16
-- 14 Views aus 17 gespeicherten Access-Abfragen.
-- Entfallen (Ergebnis der Schemapflege, kein Migrationsgegenstand):
--   Abfrage_Max_Vorlauf . Abfrage_Min_Vorlauf . Abfrage_MaxMin_Vorlauf
-- Access-Schreibweisen ([eckige Bezeichner], Klammer-Joins) bleiben unveraendert.
-- Hinweis: Abfrage_KenndatenKuehlung_Max baut auf Abfrage_Kuehlung_MaxLast auf;
-- SQLite loest Viewrumpfe erst beim SELECT auf, die alphabetische Reihenfolge stoert nicht.
--
-- Kuriert (Befund B1, Arbeitspaket S7, 02.09.2026):
--   Abfrage_ProjektGebaeudeGanglinie . Abfrage_ProjektStromGanglinie . Abfrage_Tagverteilung
-- Diese drei Abfragen waehlen die Spalte ID BEIDER verbundener Tabellen. SQLite entdoppelt
-- das selbsttaetig zu "ID" und "ID:1"; der zweite Name ist fuer Konsumenten unbrauchbar
-- (er laesst sich in WHERE/ORDER BY nur gequotet ansprechen und traegt keine Bedeutung).
-- Die zweite ID - immer die des Datensatzes der *Daten-Tabelle - heisst deshalb ID_Daten.
-- ALLE uebrigen Ausgabespalten behalten ihren Namen (ID, Bezeichner, Wert, Verteilung,
-- Zeitinterval), damit bestehende Konsumenten unveraendert weiterlaufen.
-- Aufrufer (angepasst im selben Zug): SimulationWaermebedarf.cs:305/:602,
-- SimulationStrombedarf.cs:121, StromTestClass.cs:48 - sie sprachen die Sichtspalten
-- ueber den Namen der zugrunde liegenden TABELLE an (Tab_Waermebedarf.ID usw.). Jet loest
-- das auf, SQLite nicht: eine Sicht hat nur ihre eigenen Ausgabespalten.
-- Der Generator Erzeuge-Schema.ps1 fuehrt die drei Texte als feste Ueberschreibung
-- ($VIEWS_UEBERSETZT), sonst wuerde die naechste Generierung die Kuration verwerfen.

-- uebersetzt, Original IIf (SchemaMigration.cs:6344-6351)
CREATE VIEW [Abfrage_Energietraeger_Effektiv] AS
SELECT s.ID_Projekt, s.[ID_Energieträger] AS carrier_id,
       ec.code, ec.name, ec.billing_unit,
       CASE WHEN s.custom_hi IS NULL OR s.custom_hi = 0
            THEN ec.hi_kwh_per_unit ELSE s.custom_hi END AS eff_hi,
       CASE WHEN s.custom_hs IS NULL OR s.custom_hs = 0
            THEN ec.hs_kwh_per_unit ELSE s.custom_hs END AS eff_hs
FROM energy_project_settings AS s
     INNER JOIN energy_carrier AS ec ON s.[ID_Energieträger] = ec.id;

CREATE VIEW [Abfrage_Gebaeudearten] AS
SELECT Tab_Gebaeude_STAMM.Gebaeudeart, Tab_Gebaeude_STAMM.Wohngebaeude_Nicht_Wohngebaeude
FROM Tab_Gebaeude_STAMM
GROUP BY Tab_Gebaeude_STAMM.Gebaeudeart, Tab_Gebaeude_STAMM.Wohngebaeude_Nicht_Wohngebaeude
HAVING (((Tab_Gebaeude_STAMM.Gebaeudeart) IS NOT NULL));

CREATE VIEW [Abfrage_Gebaeudetypen] AS
SELECT Tab_Gebaeude_STAMM.Typ
FROM Tab_Gebaeude_STAMM
GROUP BY Tab_Gebaeude_STAMM.Typ
ORDER BY Tab_Gebaeude_STAMM.Typ;

CREATE VIEW [Abfrage_KenndatenKuehlung_Max] AS
SELECT Tab_Kenndaten_Kuehlung.ID, Tab_Kenndaten_Kuehlung.ID_WP, Tab_Kenndaten_Kuehlung.Vorlauf, Tab_Kenndaten_Kuehlung.Temperatur, Tab_Kenndaten_Kuehlung.COP, Tab_Kenndaten_Kuehlung.Pkuehl, Abfrage_Kuehlung_MaxLast.MaxvonLast AS [Last]
FROM Abfrage_Kuehlung_MaxLast INNER JOIN Tab_Kenndaten_Kuehlung ON (Abfrage_Kuehlung_MaxLast.ID_WP = Tab_Kenndaten_Kuehlung.ID_WP) AND (Abfrage_Kuehlung_MaxLast.MaxvonLast = Tab_Kenndaten_Kuehlung.Last)
ORDER BY Tab_Kenndaten_Kuehlung.ID_WP, Tab_Kenndaten_Kuehlung.Vorlauf, Tab_Kenndaten_Kuehlung.Temperatur;

-- uebersetzt, Original IIf/PROCEDURE (ACE laesst kein ORDER BY in Views zu, SQLite schon).
-- Der IIf-Ausdruck steht im ORDER BY ausgeschrieben, damit der Text dem Original entspricht.
CREATE VIEW [Abfrage_Kostenfaktoren] AS
SELECT w.ID, w.ProjektID, w.StammID, w.KategorieID,
       CASE w.KategorieID WHEN 1 THEN 'Investitionskosten'
                          WHEN 2 THEN 'Betriebskosten'
                          WHEN 3 THEN 'Energiekosten' ELSE '' END AS KategorieName,
       k.Komponente, f.Bezeichnung, w.Gruppe, w.EingegebenerWert, w.WorstCase,
       w.BestCase, w.Nutzungsdauer, w.WorstCase_Nutzungsdauer,
       w.BestCase_Nutzungsdauer, w.Einheit, f.IsMainComponent
FROM (Tab_ProjektWerte AS w
      INNER JOIN Tab_Kostenfaktor AS f ON w.StammID = f.StammID)
     INNER JOIN Tab_KostenKomponente AS k ON w.KomponentenID = k.ID
ORDER BY f.IsMainComponent,
         CASE w.KategorieID WHEN 1 THEN 'Investitionskosten'
                            WHEN 2 THEN 'Betriebskosten'
                            WHEN 3 THEN 'Energiekosten' ELSE '' END,
         k.Komponente, w.Gruppe, f.Bezeichnung;

CREATE VIEW [Abfrage_Kuehlung_MaxLast] AS
SELECT Tab_Kenndaten_Kuehlung.ID_WP, Max(Tab_Kenndaten_Kuehlung.Last) AS MaxvonLast
FROM Tab_Kenndaten_Kuehlung
GROUP BY Tab_Kenndaten_Kuehlung.ID_WP;

CREATE VIEW [Abfrage_Monatsstrom] AS
SELECT Z_Projekt_Stromverbraucher.ID_Projekt, Tab_Stromverbraucher.Bezeichner, Tab_Stromverbraucher.Typ, Tab_Stromverbraucher.Monat_1, Tab_Stromverbraucher.Monat_2, Tab_Stromverbraucher.Monat_3, Tab_Stromverbraucher.Monat_4, Tab_Stromverbraucher.Monat_5, Tab_Stromverbraucher.Monat_6, Tab_Stromverbraucher.Monat_7, Tab_Stromverbraucher.Monat_8, Tab_Stromverbraucher.Monat_9, Tab_Stromverbraucher.Monat_10, Tab_Stromverbraucher.Monat_11, Tab_Stromverbraucher.Monat_12
FROM Tab_Stromverbraucher INNER JOIN Z_Projekt_Stromverbraucher ON Tab_Stromverbraucher.ID = Z_Projekt_Stromverbraucher.ID_Stromverbraucher;

CREATE VIEW [Abfrage_Monatswaerme_Brauchwasser] AS
SELECT Z_Projekt_Brauchwasser.ID_Projekt, Tab_Brauchwasser.Bezeichner, Tab_Brauchwasser.Typ, Tab_Brauchwasser.Monat_1, Tab_Brauchwasser.Monat_2, Tab_Brauchwasser.Monat_3, Tab_Brauchwasser.Monat_4, Tab_Brauchwasser.Monat_5, Tab_Brauchwasser.Monat_6, Tab_Brauchwasser.Monat_7, Tab_Brauchwasser.Monat_8, Tab_Brauchwasser.Monat_9, Tab_Brauchwasser.Monat_10, Tab_Brauchwasser.Monat_11, Tab_Brauchwasser.Monat_12
FROM Tab_Brauchwasser INNER JOIN Z_Projekt_Brauchwasser ON Tab_Brauchwasser.ID = Z_Projekt_Brauchwasser.ID_Brauchwasser;

CREATE VIEW [Abfrage_Monatswaerme_Prozesse] AS
SELECT Z_Projekt_Prozesswaerme.ID_Projekt, Z_Projekt_Prozesswaerme.Bezeichner, Tab_Prozesswaerme.Typ, Tab_Prozesswaerme.Monat_1, Tab_Prozesswaerme.Monat_2, Tab_Prozesswaerme.Monat_3, Tab_Prozesswaerme.Monat_4, Tab_Prozesswaerme.Monat_5, Tab_Prozesswaerme.Monat_6, Tab_Prozesswaerme.Monat_7, Tab_Prozesswaerme.Monat_8, Tab_Prozesswaerme.Monat_9, Tab_Prozesswaerme.Monat_10, Tab_Prozesswaerme.Monat_11, Tab_Prozesswaerme.Monat_12
FROM Tab_Prozesswaerme INNER JOIN Z_Projekt_Prozesswaerme ON Tab_Prozesswaerme.ID = Z_Projekt_Prozesswaerme.ID_Prozesswaerme;

CREATE VIEW [Abfrage_Projektgebaeude] AS
SELECT Z_ProjektGebaeude.ID_Projekt, Z_ProjektGebaeude.Wohnflaeche_Waermebedarf, Z_ProjektGebaeude.Einheit_Waermebedarf_Wohnflaeche, Z_ProjektGebaeude.Jahresnutzungsgrad, Z_ProjektGebaeude.dezWarmwasserbereitung, Tab_Gebaeude.Gebaeudename, Tab_Gebaeude.Typ, Tab_Gebaeude.Beschreibung, Tab_Gebaeude.Wohnflaeche_gesamt, Tab_Gebaeude.Bewohner, Tab_Gebaeude.Flaeche_Nutzer, Tab_Gebaeude.Interne_Waermegewinne, Tab_Gebaeude.Bauweise, Tab_Gebaeude.Fensterflaeche_Sued, Tab_Gebaeude.Fensterflaeche_Ost_West, Tab_Gebaeude.Fensterflaeche_Nord, Tab_Gebaeude.Fensterdurchlassgrad, Tab_Gebaeude.Raumsolltemperatur_Nachtabsenkung, Tab_Gebaeude.Raumsolltemperatur_Tag, Tab_Gebaeude.Raumsolltemperatur_Wochenende, Tab_Gebaeude.Raumsolltemperatur_Ferien, Tab_Gebaeude.Maximaleraumtemperatur, Tab_Gebaeude.k_Wert_Außenwand, Tab_Gebaeude.k_Wert_Fenster, Tab_Gebaeude.k_Wert_Dachflaeche, Tab_Gebaeude.k_Wert_Grundflaeche, Tab_Gebaeude.k_Wert_Sonstiges, Tab_Gebaeude.Flaeche_Außenwand, Tab_Gebaeude.gesamte_Fensterflaeche, Tab_Gebaeude.Dachflaeche, Tab_Gebaeude.Grundflaeche, Tab_Gebaeude.Sonstige_Flaechen, Tab_Gebaeude.Wohnflaeche, Tab_Gebaeude.Raumhoehe, Tab_Gebaeude.WBVK_Anschluß_Fenster_Wand, Tab_Gebaeude.WBVK_Anschluß_Wand_Dach, Tab_Gebaeude.WBVK_Anschluß_Außenwand_Kellerdecke, Tab_Gebaeude.Abmessung_Anschluß_Fenster_Wand, Tab_Gebaeude.Abmessung_Anschluß_Wand_Dach, Tab_Gebaeude.Abmessung_Anschluß_Außenwand_Kellerdecke, Tab_Gebaeude.Luftwechselrate, Tab_Gebaeude.Wochenende, Tab_Gebaeude.Ferien, Tab_Gebaeude.Ferienbeginn_1, Tab_Gebaeude.Ferienende_1, Tab_Gebaeude.Ferienbeginn_2, Tab_Gebaeude.Ferienende_2, Tab_Gebaeude.Ferienbeginn_3, Tab_Gebaeude.Ferienende_3, Tab_Gebaeude.Ferienbeginn_4, Tab_Gebaeude.Ferienende_4, Tab_Gebaeude.WW_Bedarf, Tab_Gebaeude.spez_Waermeverbrauch, Tab_Gebaeude.Waermebedarf, Tab_Gebaeude.Baualtersklasse, Tab_Gebaeude.Gebaeudeart, Tab_Gebaeude.Wohngebaeude_Nicht_Wohngebaeude, Tab_Gebaeude.ID
FROM Z_ProjektGebaeude INNER JOIN Tab_Gebaeude ON Z_ProjektGebaeude.ID = Tab_Gebaeude.ID_ProjektGebaeude;

-- kuriert (Befund B1, S7): zweite ID als ID_Daten benannt - siehe Kopfhinweis.
CREATE VIEW [Abfrage_ProjektGebaeudeGanglinie] AS
SELECT Tab_Waermebedarf.ID, Tab_WaermebedarfDaten.ID AS ID_Daten, Tab_WaermebedarfDaten.Wert
FROM Tab_Waermebedarf INNER JOIN Tab_WaermebedarfDaten ON Tab_Waermebedarf.ID = Tab_WaermebedarfDaten.ID_Ganglinie;

-- kuriert (Befund B1, S7): zweite ID als ID_Daten benannt - siehe Kopfhinweis.
CREATE VIEW [Abfrage_ProjektStromGanglinie] AS
SELECT Tab_Stromganglinie.ID, Tab_StromganglinieDaten.ID AS ID_Daten, Tab_StromganglinieDaten.Wert, Tab_Stromganglinie.Zeitinterval
FROM Tab_Stromganglinie INNER JOIN Tab_StromganglinieDaten ON Tab_Stromganglinie.ID = Tab_StromganglinieDaten.ID_Ganglinie;

CREATE VIEW [Abfrage_SST] AS
SELECT Tab_WP.Bezeichner, Tab_Kenndaten.Vorlauf, Tab_Kenndaten.Temperatur, Tab_Kenndaten.COP, Tab_Kenndaten.Ptherm
FROM Tab_WP INNER JOIN Tab_Kenndaten ON Tab_WP.ID = Tab_Kenndaten.ID_WP
ORDER BY Tab_WP.Bezeichner, Tab_Kenndaten.Vorlauf, Tab_Kenndaten.Temperatur DESC;

-- kuriert (Befund B1, S7): zweite ID als ID_Daten benannt - siehe Kopfhinweis.
CREATE VIEW [Abfrage_Tagverteilung] AS
SELECT Tab_DBTagV.ID, Tab_DBTagV.Bezeichner, Tab_DBTagVDaten.ID AS ID_Daten, Tab_DBTagVDaten.Verteilung
FROM Tab_DBTagV INNER JOIN Tab_DBTagVDaten ON Tab_DBTagV.ID = Tab_DBTagVDaten.ID_TagV
ORDER BY Tab_DBTagVDaten.ID;
