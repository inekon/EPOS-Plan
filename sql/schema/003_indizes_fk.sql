-- 003_indizes_fk.sql - EPOS-Plan, Zielschema SQLite (Arbeitspaket S2)
-- Erzeugt von sql/tools/Erzeuge-Schema.ps1, Quellenstand 2026-09-01 15:16
--
-- ACHTUNG zum Dateinamen: hier stehen NUR INDIZES. Die Fremdschluessel stehen in
-- 001_grundschema.sql, weil SQLite eine FOREIGN-KEY-Klausel nach dem CREATE TABLE
-- nicht nachruesten kann (kein ALTER TABLE ADD CONSTRAINT). Der Dateiname bleibt aus
-- dem Konzept erhalten.
--
-- Uebersprungen: Primaerindizes und Indizes, deren Spaltenliste dem Primaerschluessel
-- entspricht (SQLite legt die selbst an). Entdoppelung: Spaltenliste.
-- Access-Indexnamen sind nur je Tabelle eindeutig, SQLite-weit global - bei Kollision
-- wird auf Tabelle_Indexname umbenannt.

-- Berichtskonfiguration
CREATE UNIQUE INDEX "UQ_BerichtKonfigProj" ON "Berichtskonfiguration" ("ProjektID");
-- emissionsart
CREATE UNIQUE INDEX "idx_emissionsart_kuerzel" ON "emissionsart" ("kuerzel");
-- emissionswert
CREATE INDEX "FK_emissionswert_art" ON "emissionswert" ("emissionsart_id");
CREATE INDEX "idx_emissionswert" ON "emissionswert" ("emissionsart_id", "carrier_id");
CREATE INDEX "idx_emissionswert_aktiv" ON "emissionswert" ("carrier_id", "ist_aktiv");
-- energy_carrier
CREATE INDEX "energy_carrier_code" ON "energy_carrier" ("code");
CREATE INDEX "ID_Brennstoff" ON "energy_carrier" ("ID_Brennstoff");
CREATE UNIQUE INDEX "name" ON "energy_carrier" ("name");
-- energy_conversion
CREATE INDEX "carrier_id" ON "energy_conversion" ("id_brennstoff");
-- energy_price
CREATE INDEX "energy_carrierenergy_price" ON "energy_price" ("carrier_id");
CREATE INDEX "energy_price_ID_Projekt" ON "energy_price" ("ID_Projekt");
CREATE UNIQUE INDEX "unq_price_date" ON "energy_price" ("carrier_id", "valid_from", "ID_Projekt");
CREATE INDEX "valid_from" ON "energy_price" ("valid_from");
-- energy_project_settings
CREATE INDEX "energy_carrierenergy_project_settings" ON "energy_project_settings" ("ID_Energieträger");
CREATE INDEX "energy_project_settings_ID_Projekt" ON "energy_project_settings" ("ID_Projekt");
CREATE INDEX "ID_Umrechnung" ON "energy_project_settings" ("ID_Umrechnung");
-- Tab_Applikation
CREATE INDEX "Tab_Applikation_ID_Projekt" ON "Tab_Applikation" ("ID_Projekt");
CREATE INDEX "Tab_ApplikationID" ON "Tab_Applikation" ("ID");
CREATE INDEX "Tab_ApplikationProjektname" ON "Tab_Applikation" ("Projektname");
-- Tab_BHKW
CREATE INDEX "Tab_BHKW_Bezeichner" ON "Tab_BHKW" ("Bezeichner");
CREATE INDEX "Tab_BHKW_ID_Projekt" ON "Tab_BHKW" ("ID_Projekt");
-- Tab_BHKW_STAMM
CREATE UNIQUE INDEX "Tab_BHKW_STAMM_Bezeichner" ON "Tab_BHKW_STAMM" ("Bezeichner");
-- Tab_Brauchwasser
CREATE INDEX "Tab_Brauchwasser_Bezeichner" ON "Tab_Brauchwasser" ("Bezeichner");
CREATE INDEX "Tab_Brauchwasser_ID_Projekt" ON "Tab_Brauchwasser" ("ID_Projekt");
-- Tab_Brauchwasser_STAMM
CREATE UNIQUE INDEX "Tab_Brauchwasser_STAMM_Bezeichner" ON "Tab_Brauchwasser_STAMM" ("Bezeichner");
-- Tab_Brauchwassertyp
CREATE UNIQUE INDEX "Tab_Brauchwassertyp_ID" ON "Tab_Brauchwassertyp" ("ID");
CREATE INDEX "Tab_Brauchwassertyp_ID_Brauchwasser" ON "Tab_Brauchwassertyp" ("ID_Brauchwasser");
CREATE INDEX "Tab_Brauchwassertyp_ID_Projekt" ON "Tab_Brauchwassertyp" ("ID_Projekt");
CREATE INDEX "Tab_Brauchwassertyp_Typname" ON "Tab_Brauchwassertyp" ("Typname");
-- Tab_Brauchwassertyp_STAMM
CREATE UNIQUE INDEX "Tab_Brauchwassertyp_STAMM_Typname" ON "Tab_Brauchwassertyp_STAMM" ("Bezeichner");
-- Tab_Brennstoff_Stamm
CREATE UNIQUE INDEX "Tab_Brennstoff_Stamm_GruppenName" ON "Tab_Brennstoff_Stamm" ("Bezeichner");
CREATE INDEX "Tab_Brennstoff_Stamm_KategorieID" ON "Tab_Brennstoff_Stamm" ("ID_Kategorie");
-- Tab_BrennstoffKategorien
CREATE INDEX "Tab_BrennstoffKategorien_Code" ON "Tab_BrennstoffKategorien" ("Code");
CREATE UNIQUE INDEX "Gruppe" ON "Tab_BrennstoffKategorien" ("Gruppe");
-- Tab_DBTagV
CREATE INDEX "Tab_DBTagV_Bezeichner" ON "Tab_DBTagV" ("Bezeichner");
CREATE INDEX "ID_Gebaeude" ON "Tab_DBTagV" ("ID_Gebaeude");
-- Tab_DBTagV_STAMM
CREATE UNIQUE INDEX "UX_Tab_DBTagV_STAMM_Bezeichner" ON "Tab_DBTagV_STAMM" ("Bezeichner");
-- Tab_DBTagVDaten
CREATE INDEX "Tab_DBTagVDaten_ID_TagV" ON "Tab_DBTagVDaten" ("ID_TagV");
-- Tab_DBTagVDaten_STAMM
CREATE INDEX "Tab_DBTagVDaten_STAMM_ID_TagV" ON "Tab_DBTagVDaten_STAMM" ("ID_TagV");
-- Tab_Einstellungen
CREATE UNIQUE INDEX "Tab_Einstellungen_ID_Projekt" ON "Tab_Einstellungen" ("ID_Projekt");
-- Tab_Energieanlagen
CREATE INDEX "FK_Anlage_Quellprofil" ON "Tab_Energieanlagen" ("WQ_ID_Quellprofil");
CREATE INDEX "FK_Energieanlagen_ID_Puffer" ON "Tab_Energieanlagen" ("ID_PUFFER");
CREATE INDEX "FK_Energieanlagen_WQ_Puffer" ON "Tab_Energieanlagen" ("WQ_ID_Puffer");
CREATE INDEX "FK_Energieanlagen_WS_Puffer" ON "Tab_Energieanlagen" ("WS_ID_Puffer");
CREATE INDEX "FK_Energieanlagen_WS_Puffer2" ON "Tab_Energieanlagen" ("WS_ID_Puffer2");
CREATE INDEX "ID_BHKW" ON "Tab_Energieanlagen" ("ID_BHKW");
CREATE INDEX "ID_Kessel" ON "Tab_Energieanlagen" ("ID_Kessel");
CREATE INDEX "Tab_Energieanlagen_ID_Projekt" ON "Tab_Energieanlagen" ("ID_Projekt");
CREATE INDEX "ID_PV" ON "Tab_Energieanlagen" ("ID_PV");
CREATE INDEX "ID_Solar" ON "Tab_Energieanlagen" ("ID_Solar");
CREATE INDEX "ID_SP" ON "Tab_Energieanlagen" ("ID_SP");
CREATE INDEX "ID_Type" ON "Tab_Energieanlagen" ("ID_Type");
CREATE INDEX "Tab_Energieanlagen_ID_WP" ON "Tab_Energieanlagen" ("ID_WP");
CREATE UNIQUE INDEX "idx_Anlage_ID_BHKW" ON "Tab_Energieanlagen" ("ID_Projekt", "ID_BHKW");
CREATE UNIQUE INDEX "idx_Anlage_ID_Kessel" ON "Tab_Energieanlagen" ("ID_Projekt", "ID_Kessel");
CREATE UNIQUE INDEX "idx_Anlage_ID_PUFFER" ON "Tab_Energieanlagen" ("ID_Projekt", "ID_PUFFER");
CREATE UNIQUE INDEX "idx_Anlage_ID_WP" ON "Tab_Energieanlagen" ("ID_Projekt", "ID_WP");
-- Tab_ErgebnisBHKW
CREATE INDEX "Rel_Ergebnis_BHKW" ON "Tab_ErgebnisBHKW" ("ID_Ergebnis");
-- Tab_ErgebnisBHKWModul
CREATE INDEX "Rel_BHKW_Modul" ON "Tab_ErgebnisBHKWModul" ("ID_ErgebnisBHKW");
-- Tab_ErgebnisEnergiebedarf
CREATE INDEX "Rel_Ergebnis_Energiebedarf" ON "Tab_ErgebnisEnergiebedarf" ("ID_Ergebnis");
-- Tab_ErgebnisHeizkessel
CREATE INDEX "Rel_Ergebnis_Heizkessel" ON "Tab_ErgebnisHeizkessel" ("ID_Ergebnis");
-- Tab_ErgebnisHeizkesselModul
CREATE INDEX "Rel_Heizkessel_Modul" ON "Tab_ErgebnisHeizkesselModul" ("ID_ErgebnisHeizkessel");
-- Tab_ErgebnisPhotovoltaik
CREATE INDEX "Rel_Ergebnis_Photovoltaik" ON "Tab_ErgebnisPhotovoltaik" ("ID_Ergebnis");
-- Tab_ErgebnisPhotovoltaikModul
CREATE INDEX "Rel_Photovoltaik_Modul" ON "Tab_ErgebnisPhotovoltaikModul" ("ID_ErgebnisPhotovoltaik");
-- Tab_ErgebnisPufferspeicher
CREATE INDEX "FK_ErgPuffer" ON "Tab_ErgebnisPufferspeicher" ("ID_Ergebnis");
-- Tab_ErgebnisSolarthermie
CREATE INDEX "Rel_Ergebnis_Solarthermie" ON "Tab_ErgebnisSolarthermie" ("ID_Ergebnis");
-- Tab_ErgebnisSolarthermieModul
CREATE INDEX "Rel_Solarthermie_Modul" ON "Tab_ErgebnisSolarthermieModul" ("ID_ErgebnisSolarthermie");
-- Tab_ErgebnisStromspeicher
CREATE INDEX "FK_ErgStromspeicher" ON "Tab_ErgebnisStromspeicher" ("ID_Ergebnis");
-- Tab_ErgebnisWaermepumpe
CREATE INDEX "Rel_Ergebnis_Waermepumpe" ON "Tab_ErgebnisWaermepumpe" ("ID_Ergebnis");
-- Tab_ErgebnisWaermepumpeModul
CREATE INDEX "Rel_Waermepumpe_Modul" ON "Tab_ErgebnisWaermepumpeModul" ("ID_ErgebnisWaermepumpe");
-- Tab_Gebaeude
CREATE INDEX "Tab_Gebaeude_Gebaeudename" ON "Tab_Gebaeude" ("Gebaeudename");
CREATE INDEX "Tab_Gebaeude_ID_Projekt" ON "Tab_Gebaeude" ("ID_ProjektGebaeude");
CREATE INDEX "ID_Projekt1" ON "Tab_Gebaeude" ("ID_Projekt");
CREATE INDEX "Tab_Gebaeude_Typ" ON "Tab_Gebaeude" ("Typ");
-- Tab_Gebaeude_STAMM
CREATE UNIQUE INDEX "Tab_Gebaeude_STAMM_Gebaeudename" ON "Tab_Gebaeude_STAMM" ("Bezeichner");
-- Tab_Heizkessel
CREATE INDEX "Tab_Heizkessel_Bezeichner" ON "Tab_Heizkessel" ("Bezeichner");
CREATE INDEX "Tab_Heizkessel_ID_Projekt" ON "Tab_Heizkessel" ("ID_Projekt");
-- Tab_Heizkessel_STAMM
CREATE UNIQUE INDEX "UX_Tab_Heizkessel_STAMM_Bezeichner" ON "Tab_Heizkessel_STAMM" ("Bezeichner");
-- Tab_Kenndaten
CREATE INDEX "Tab_Kenndaten_ID_Projekt" ON "Tab_Kenndaten" ("ID_Projekt");
CREATE INDEX "Tab_Kenndaten_ID_WP" ON "Tab_Kenndaten" ("ID_WP");
-- Tab_Kenndaten_Kuehlung
CREATE INDEX "Tab_Kenndaten_Kuehlung_ID_WP" ON "Tab_Kenndaten_Kuehlung" ("ID_WP");
-- Tab_Kenndaten_Kuehlung_STAMM
CREATE INDEX "Tab_Kenndaten_Kuehlung_STAMM_ID_Projekt" ON "Tab_Kenndaten_Kuehlung_STAMM" ("ID_Projekt");
CREATE INDEX "Tab_Kenndaten_Kuehlung_STAMM_ID_WP" ON "Tab_Kenndaten_Kuehlung_STAMM" ("ID_WP");
-- Tab_Kenndaten_STAMM
CREATE INDEX "Tab_Kenndaten_STAMM_ID_WP" ON "Tab_Kenndaten_STAMM" ("ID_WP");
-- Tab_Klimadaten
CREATE INDEX "Tab_Klimadaten_ID" ON "Tab_Klimadaten" ("ID_Klimaregion");
CREATE INDEX "Tab_Klimadaten_ID_Projekt" ON "Tab_Klimadaten" ("ID_Projekt");
-- Tab_Klimadaten_STAMM
CREATE INDEX "Tab_Klimadaten_STAMM_ID" ON "Tab_Klimadaten_STAMM" ("ID_Klimaregion");
-- Tab_Klimaregion
CREATE INDEX "Tab_Klimaregion_Bezeichner" ON "Tab_Klimaregion" ("Bezeichner");
CREATE INDEX "Tab_Klimaregion_ID_Projekt" ON "Tab_Klimaregion" ("ID_Projekt");
-- Tab_Klimaregion_STAMM
CREATE UNIQUE INDEX "UX_Tab_Klimaregion_STAMM_Name" ON "Tab_Klimaregion_STAMM" ("Name");
-- Tab_KostenGruppenKatalog
CREATE UNIQUE INDEX "Tab_KostenGruppenKatalog_GruppenName" ON "Tab_KostenGruppenKatalog" ("GruppenName");
-- Tab_Kostenprofil
CREATE INDEX "idx_Kostenprofil" ON "Tab_Kostenprofil" ("ID_Projekt");
-- Tab_KostenVorlage
CREATE INDEX "idx_KostenVorlage" ON "Tab_KostenVorlage" ("KomponentenID", "KategorieID");
-- Tab_KostenVorlagePosition
CREATE INDEX "FK_KostenVorlagePos" ON "Tab_KostenVorlagePosition" ("VorlageID");
-- Tab_Preisreihe
CREATE INDEX "idx_Preisreihe" ON "Tab_Preisreihe" ("ID_Projekt");
-- Tab_PreisreiheDaten
CREATE INDEX "FK_PreisreiheDaten" ON "Tab_PreisreiheDaten" ("ID_Preisreihe");
-- Tab_Projekt
CREATE INDEX "Tab_Projekt_ID_Klimaregion" ON "Tab_Projekt" ("ID_Klimaregion");
CREATE UNIQUE INDEX "Projektname" ON "Tab_Projekt" ("Projektname");
-- Tab_ProjektPhotovoltaik
CREATE UNIQUE INDEX "idx_ProjektPhotovoltaik" ON "Tab_ProjektPhotovoltaik" ("ID_Projekt");
-- Tab_ProjektTarif
CREATE UNIQUE INDEX "Tab_ProjektTab_ProjektTarif" ON "Tab_ProjektTarif" ("ID_Projekt");
-- Tab_ProjektWerte
CREATE INDEX "Tab_ProjektWerte_KategorieID" ON "Tab_ProjektWerte" ("KategorieID");
CREATE INDEX "KomponentenID" ON "Tab_ProjektWerte" ("KomponentenID");
CREATE INDEX "ProjektID" ON "Tab_ProjektWerte" ("ProjektID");
CREATE INDEX "Tab_KostenfaktorTab_ProjektWerte" ON "Tab_ProjektWerte" ("StammID");
CREATE INDEX "Tab_KostenGruppenKatalogTab_ProjektWerte" ON "Tab_ProjektWerte" ("Gruppe");
-- Tab_ProjektWirtschaftlichkeit
CREATE UNIQUE INDEX "Tab_ProjektTab_ProjektWirtschaftlichkeit" ON "Tab_ProjektWirtschaftlichkeit" ("ID_Projekt");
-- Tab_Prozesstyp
CREATE INDEX "Tab_Prozesstyp_ID_Projekt" ON "Tab_Prozesstyp" ("ID_Projekt");
CREATE INDEX "Tab_Prozesstyp_ID_Prozesswaerme" ON "Tab_Prozesstyp" ("ID_Prozesswaerme");
CREATE INDEX "Tab_Prozesstyp_Typname" ON "Tab_Prozesstyp" ("Typname");
-- Tab_Prozesstyp_STAMM
CREATE UNIQUE INDEX "Tab_Prozesstyp_STAMM_Typname" ON "Tab_Prozesstyp_STAMM" ("Bezeichner");
-- Tab_Prozesswaerme
CREATE INDEX "Tab_Prozesswaerme_Bezeichner" ON "Tab_Prozesswaerme" ("Bezeichner");
CREATE INDEX "Tab_Prozesswaerme_ID_Projekt" ON "Tab_Prozesswaerme" ("ID_Projekt");
CREATE INDEX "Tab_ProzesstypTab_Prozesswaerme" ON "Tab_Prozesswaerme" ("Typ");
-- Tab_Prozesswaerme_STAMM
CREATE UNIQUE INDEX "Prozessname" ON "Tab_Prozesswaerme_STAMM" ("Bezeichner");
CREATE INDEX "Tab_Prozesswaerme_STAMM_Typ" ON "Tab_Prozesswaerme_STAMM" ("Typ");
-- Tab_Pufferspeicher
CREATE INDEX "Tab_Pufferspeicher_Bezeichner" ON "Tab_Pufferspeicher" ("Bezeichner");
CREATE INDEX "FK_Pufferspeicher_Projekt" ON "Tab_Pufferspeicher" ("ID_Projekt");
-- Tab_Pufferspeicher_STAMM
CREATE UNIQUE INDEX "Tab_Pufferspeicher_STAMM_Bezeichner" ON "Tab_Pufferspeicher_STAMM" ("Bezeichner");
-- Tab_PV
CREATE INDEX "Tab_PV_Bezeichner" ON "Tab_PV" ("Bezeichner");
CREATE INDEX "Tab_PV_ID_Projekt" ON "Tab_PV" ("ID_Projekt");
-- Tab_PV_STAMM
CREATE UNIQUE INDEX "UX_Tab_PV_STAMM_Bezeichner" ON "Tab_PV_STAMM" ("Bezeichner");
-- Tab_Quellprofil
CREATE INDEX "idx_Quellprofil" ON "Tab_Quellprofil" ("ID_Projekt");
-- Tab_QuellprofilDaten
CREATE INDEX "FK_QuellprofilDaten_Kopf" ON "Tab_QuellprofilDaten" ("ID_Quellprofil");
CREATE INDEX "idx_QuellprofilDaten" ON "Tab_QuellprofilDaten" ("ID_Quellprofil", "Index");
-- Tab_Solar
CREATE INDEX "Tab_Solar_ID_Klimaregion" ON "Tab_Solar" ("ID_Klimaregion");
CREATE INDEX "Tab_Solar_ID_Projekt" ON "Tab_Solar" ("ID_Projekt");
-- Tab_Solar_STAMM
CREATE INDEX "Tab_Solar_STAMM_ID_Klimaregion" ON "Tab_Solar_STAMM" ("ID_Klimaregion");
-- Tab_Solarganglinie
CREATE INDEX "Tab_Solarganglinie_ID_Projekt" ON "Tab_Solarganglinie" ("ID_Projekt");
-- Tab_Solarganglinie_STAMM
CREATE UNIQUE INDEX "UX_Tab_Solarganglinie_STAMM_Bezeichner" ON "Tab_Solarganglinie_STAMM" ("Bezeichner");
-- Tab_SolarganglinieDaten
CREATE INDEX "Tab_SolarganglinieDaten_ID_GanglinieDaten" ON "Tab_SolarganglinieDaten" ("ID_Ganglinie");
-- Tab_SolarganglinieDaten_STAMM
CREATE INDEX "Tab_SolarganglinieDaten_STAMM_ID_GanglinieDaten" ON "Tab_SolarganglinieDaten_STAMM" ("ID_Ganglinie");
-- Tab_Solarkollektoren
CREATE INDEX "Tab_Solarkollektoren_Bezeichner" ON "Tab_Solarkollektoren" ("Bezeichner");
CREATE INDEX "Tab_Solarkollektoren_ID_Projekt" ON "Tab_Solarkollektoren" ("ID_Projekt");
-- Tab_Solarkollektoren_STAMM
CREATE UNIQUE INDEX "Kollektorname" ON "Tab_Solarkollektoren_STAMM" ("Bezeichner");
-- Tab_Stromganglinie
CREATE INDEX "Tab_Stromganglinie_ID_Projekt" ON "Tab_Stromganglinie" ("ID_Projekt");
-- Tab_Stromganglinie_STAMM
CREATE UNIQUE INDEX "UX_Tab_Stromganglinie_STAMM_Bezeichner" ON "Tab_Stromganglinie_STAMM" ("Bezeichner");
-- Tab_StromganglinieDaten
CREATE INDEX "Tab_StromganglinieDaten_ID_GanglinieDaten" ON "Tab_StromganglinieDaten" ("ID_Ganglinie");
-- Tab_StromganglinieDaten_STAMM
CREATE INDEX "Tab_StromganglinieDaten_STAMM_ID_GanglinieDaten" ON "Tab_StromganglinieDaten_STAMM" ("ID_Ganglinie");
-- Tab_Stromspeicher
CREATE INDEX "Tab_Stromspeicher_Bezeichner" ON "Tab_Stromspeicher" ("Bezeichner");
CREATE INDEX "Tab_Stromspeicher_ID_Projekt" ON "Tab_Stromspeicher" ("ID_Projekt");
-- Tab_Stromspeicher_STAMM
CREATE UNIQUE INDEX "Tab_Stromspeicher_STAMM_Bezeichner" ON "Tab_Stromspeicher_STAMM" ("Bezeichner");
-- Tab_StromspeicherVariante
CREATE INDEX "FK_SpVariante_Anlage" ON "Tab_StromspeicherVariante" ("ID_Energieanlage");
-- Tab_Stromverbraucher
CREATE INDEX "Tab_Stromverbraucher_Bezeichner" ON "Tab_Stromverbraucher" ("Bezeichner");
CREATE INDEX "Tab_Stromverbraucher_ID_Projekt" ON "Tab_Stromverbraucher" ("ID_Projekt");
-- Tab_Stromverbraucher_STAMM
CREATE UNIQUE INDEX "Tab_Stromverbraucher_STAMM_Bezeichner" ON "Tab_Stromverbraucher_STAMM" ("Bezeichner");
-- Tab_Stromverbrauchertyp
CREATE INDEX "Tab_Stromverbrauchertyp_ID_Projekt" ON "Tab_Stromverbrauchertyp" ("ID_Projekt");
CREATE INDEX "Tab_Stromverbrauchertyp_ID_Stromverbraucher" ON "Tab_Stromverbrauchertyp" ("ID_Stromverbraucher");
CREATE INDEX "Tab_Stromverbrauchertyp_Typname" ON "Tab_Stromverbrauchertyp" ("Typname");
-- Tab_Stromverbrauchertyp_STAMM
CREATE UNIQUE INDEX "Tab_Stromverbrauchertyp_STAMM_Typname" ON "Tab_Stromverbrauchertyp_STAMM" ("Typname");
-- Tab_Typ_Energieanlagen
CREATE UNIQUE INDEX "Tab_Typ_Energieanlagen_Bezeichner" ON "Tab_Typ_Energieanlagen" ("Bezeichner");
-- Tab_Variante
CREATE UNIQUE INDEX "UQ_VarProj" ON "Tab_Variante" ("ID_Projekt");
-- Tab_Waermebedarf
CREATE INDEX "Tab_Waermebedarf_ID_Projekt" ON "Tab_Waermebedarf" ("ID_Projekt");
-- Tab_Waermebedarf_STAMM
CREATE UNIQUE INDEX "UX_Tab_Waermebedarf_STAMM_Bezeichner" ON "Tab_Waermebedarf_STAMM" ("Bezeichner");
-- Tab_WaermebedarfDaten
CREATE INDEX "Tab_WaermebedarfDaten_ID_GanglinieDaten" ON "Tab_WaermebedarfDaten" ("ID_Ganglinie");
-- Tab_WaermebedarfDaten_STAMM
CREATE INDEX "Tab_WaermebedarfDaten_STAMM_ID_GanglinieDaten" ON "Tab_WaermebedarfDaten_STAMM" ("ID_Ganglinie");
-- Tab_WP
CREATE INDEX "Tab_WP_Bezeichner" ON "Tab_WP" ("Bezeichner");
CREATE INDEX "Tab_WP_ID_Projekt" ON "Tab_WP" ("ID_Projekt");
-- Tab_WP_STAMM
CREATE UNIQUE INDEX "UX_Tab_WP_STAMM_Bezeichner" ON "Tab_WP_STAMM" ("Bezeichner");
-- Z_AnlagePufferVerbund
CREATE INDEX "FK_Verbund_Anlage" ON "Z_AnlagePufferVerbund" ("ID_Anlage");
CREATE INDEX "FK_Verbund_Puffer" ON "Z_AnlagePufferVerbund" ("ID_Puffer");
-- Z_AnlageSenke
CREATE INDEX "FK_AnlageSenke_Anlage" ON "Z_AnlageSenke" ("ID_Anlage");
CREATE INDEX "FK_AnlageSenke_Puffer" ON "Z_AnlageSenke" ("ID_Puffer");
CREATE INDEX "idx_AnlageSenke" ON "Z_AnlageSenke" ("ID_Anlage", "Rang");
-- Z_Projekt_Brauchwasser
CREATE INDEX "Z_Projekt_Brauchwasser_ID_Brauchwasser" ON "Z_Projekt_Brauchwasser" ("ID_Brauchwasser");
CREATE INDEX "Z_Projekt_Brauchwasser_ID_Projekt" ON "Z_Projekt_Brauchwasser" ("ID_Projekt");
-- Z_Projekt_Prozesswaerme
CREATE INDEX "Z_Projekt_Prozesswaerme_ID_Projekt" ON "Z_Projekt_Prozesswaerme" ("ID_Projekt");
CREATE INDEX "Z_Projekt_Prozesswaerme_ID_Prozesswaerme" ON "Z_Projekt_Prozesswaerme" ("ID_Prozesswaerme");
-- Z_Projekt_Stromverbraucher
CREATE INDEX "Z_Projekt_Stromverbraucher_ID_Projekt" ON "Z_Projekt_Stromverbraucher" ("ID_Projekt");
CREATE INDEX "Z_Projekt_Stromverbraucher_ID_Stromverbraucher" ON "Z_Projekt_Stromverbraucher" ("ID_Stromverbraucher");
-- Z_ProjektGebaeude
CREATE INDEX "Z_ProjektGebaeude_ID_Projekt" ON "Z_ProjektGebaeude" ("ID_Projekt");
-- Z_ProjektPufferSp
CREATE INDEX "Z_ProjektPufferSp_ID_Projekt" ON "Z_ProjektPufferSp" ("ID_Projekt");
CREATE INDEX "ID_Pufferspeicher" ON "Z_ProjektPufferSp" ("ID_Pufferspeicher");
CREATE INDEX "Pufferspeicher" ON "Z_ProjektPufferSp" ("Pufferspeicher");
-- Z_ProjektSolarganglinie
CREATE INDEX "Z_ProjektSolarganglinie_ID_Ganglinie" ON "Z_ProjektSolarganglinie" ("ID_Ganglinie");
CREATE INDEX "Z_ProjektSolarganglinie_ID_Projekt" ON "Z_ProjektSolarganglinie" ("ID_Projekt");
-- Z_ProjektStromganglinie
CREATE INDEX "Z_ProjektStromganglinie_ID_Ganglinie" ON "Z_ProjektStromganglinie" ("ID_Ganglinie");
CREATE INDEX "Z_ProjektStromganglinie_ID_Projekt" ON "Z_ProjektStromganglinie" ("ID_Projekt");
-- Z_ProjektWaermebedarf
CREATE INDEX "Z_ProjektWaermebedarf_ID_Ganglinie" ON "Z_ProjektWaermebedarf" ("ID_Ganglinie");
CREATE INDEX "Z_ProjektWaermebedarf_ID_Projekt" ON "Z_ProjektWaermebedarf" ("ID_Projekt");
