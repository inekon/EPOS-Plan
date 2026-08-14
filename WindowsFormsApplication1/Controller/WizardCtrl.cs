using System;
using System.Collections.Generic;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    class WizardCtrl
    {
        public WizardParent parentform;
        public bool speichern;
        public string Projektname;
        public string Klimazone;

        public WizardCtrl()
        {
            speichern = false;
            Projektname = "";
            Klimazone = "";
        }

        private object GetIdForType(WErzeugerModel item, int targetType, object value)
        {
            return (item.ID_Type == targetType) ? value : DBNull.Value;
        }

        public bool Del_Projekt_Waermeerzeuger(int projektID)
        {
            return DataRepository.ExecuteSQL("DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
        }

        public bool Del_Projekt_Waermeerzeuger(int projektID, int nType)
        {
            return DataRepository.ExecuteSQL("DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID), new OleDbParameter("@type", nType) });
        }

        public bool Del_Projekt_ID_Waermeerzeuger(int projektID, int ID_Waermeerzeuger)
        {
            return DataRepository.ExecuteSQL("DELETE FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID), new OleDbParameter("@id", ID_Waermeerzeuger) });
        }

        public bool Del_Projekt_ZuordungGebäude(int projektID)
        {
            // Tagesverteilungen der Projekt-Gebaeude entfernen (Detail vor Kopf).
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_DBTagVDaten WHERE ID_TagV IN " +
                "(SELECT ID FROM Tab_DBTagV WHERE ID_Gebaeude IN " +
                "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?))",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_DBTagV WHERE ID_Gebaeude IN " +
                "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ?)",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
            // Erst die Projekt-Gebaeudekopien (Kind: FK ID_ProjektGebaeude -> Z_ProjektGebaeude.ID), dann die Zuordnung.
            DataRepository.ExecuteSQL("DELETE FROM Tab_Gebaeude WHERE ID_Projekt = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektGebaeude WHERE ID_Projekt = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
        }

        public bool Del_Projekt_ZuordungGebäude(int projektID, int ID)
        {
            // ID = Z_ProjektGebaeude.ID; zugehoerige Gebaeude-Kopie via ID_ProjektGebaeude.
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_DBTagVDaten WHERE ID_TagV IN " +
                "(SELECT ID FROM Tab_DBTagV WHERE ID_Gebaeude IN " +
                "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ? AND ID_ProjektGebaeude = ?))",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID), new OleDbParameter("@idpg", ID) });
            DataRepository.ExecuteSQL(
                "DELETE FROM Tab_DBTagV WHERE ID_Gebaeude IN " +
                "(SELECT ID FROM Tab_Gebaeude WHERE ID_Projekt = ? AND ID_ProjektGebaeude = ?)",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID), new OleDbParameter("@idpg", ID) });
            DataRepository.ExecuteSQL("DELETE FROM Tab_Gebaeude WHERE ID_Projekt = ? AND ID_ProjektGebaeude = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID), new OleDbParameter("@idpg", ID) });
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektGebaeude WHERE ID_Projekt = ? AND ID = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID), new OleDbParameter("@id", ID) });
        }

        public bool Del_WaermebedarfExtern(int projektID)
        {
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektWaermebedarf WHERE ID_Projekt = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
        }

        public bool Del_Projekt_Prozess(int projektID, int ID = 0)
        {
            string sql = (ID > 0) ? "DELETE FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ? AND ID = ?"
                                  : "DELETE FROM Z_Projekt_Prozesswaerme WHERE ID_Projekt = ?";

            List<OleDbParameter> ps = new List<OleDbParameter> { new OleDbParameter("@pID", projektID) };
            if (ID > 0) ps.Add(new OleDbParameter("@id", ID));

            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        public bool Del_Stromganglinie(int projektID)
        {
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektStromganglinie WHERE ID_Projekt = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
        }

        public bool Del_Solarganglinie(int projektID)
        {
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektSolarganglinie WHERE ID_Projekt = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
        }

        public bool Del_Projekt_Stromverbraucher(int projektID, int ID = 0)
        {
            string sql = (ID > 0) ? "DELETE FROM Z_Projekt_Stromverbraucher WHERE ID_Projekt = ? AND ID = ?"
                                  : "DELETE FROM Z_Projekt_Stromverbraucher WHERE ID_Projekt = ?";

            List<OleDbParameter> ps = new List<OleDbParameter> { new OleDbParameter("@pID", projektID) };
            if (ID > 0) ps.Add(new OleDbParameter("@id", ID));

            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        public bool Del_Projekt_Brauchwasser(int projektID, int ID = 0)
        {
            string sql = (ID > 0) ? "DELETE FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ? AND ID = ?"
                                  : "DELETE FROM Z_Projekt_Brauchwasser WHERE ID_Projekt = ?";

            List<OleDbParameter> ps = new List<OleDbParameter> { new OleDbParameter("@pID", projektID) };
            if (ID > 0) ps.Add(new OleDbParameter("@id", ID));

            return DataRepository.ExecuteSQL(sql, ps.ToArray());
        }

        public bool Add_WP_Waermeerzeuger(int projektID, List<WErzeugerModel> list)
        {
            try
            {
                foreach (var item in list)
                {
                    // Stammdatensatz der jeweiligen Energieanlage bei Bedarf ins Projekt kopieren
                    // (Dispatch ueber ID_Type / Tab_Typ_Energieanlagen). Idempotent (dedup per Bezeichner + Projekt).
                    // Weitere Typen (Heizkessel, PV, Stromspeicher, Solar, Pufferspeicher) hier analog ergaenzen,
                    // sobald deren CopyFromStamm vorhanden ist.
                    if (CheckType(item, WizardItemClass.WP_TYP, WizardItemClass.REF_WP_TYP))
                    {
                        int idWp = new WPCtrl().CopyFromStamm(item.Bezeichner, projektID);
                        if (idWp > 0) item.ID_WP = idWp;
                    }
                    else if (item.ID_Type == WizardItemClass.BHKW_TYP)
                    {
                        int idBhkw = new BHKWCtrl().CopyFromStamm(item.Bezeichner, projektID);
                        if (idBhkw > 0) item.ID_BHKW = idBhkw;
                    }
                    else if (CheckType(item, WizardItemClass.KESSEL_TYP, WizardItemClass.REF_KESSEL_TYP))
                    {
                        int idKessel = new HeizkesselCtrl().CopyFromStamm(item.Bezeichner, projektID);
                        if (idKessel > 0) item.ID_Kessel = idKessel;
                    }
                    else if (CheckType(item, WizardItemClass.SP_TYP, WizardItemClass.REF_SP_TYP))
                    {
                        int idSp = new StromspeicherCtrl().CopyFromStamm(item.Bezeichner, projektID);
                        if (idSp > 0) item.ID_SP = idSp;
                    }
                    else if (item.ID_Type == WizardItemClass.PUFFER_TYP)
                    {
                        int idPuf = new PufferSpCtrl().CopyFromStamm(item.Bezeichner, projektID);
                        // Seit Schritt 4 der SchemaMigration hat ID_PUFFER eine erzwungene
                        // Beziehung auf Tab_Pufferspeicher.ID. Scheitert die Auflösung, darf
                        // die alte ID nicht stehen bleiben - Form_PufferSp schreibt dort die
                        // STAMM-ID (Konzept 2.3), und die verletzt die Beziehung. 0 bedeutet
                        // "kein Puffer" und wird unten als NULL geschrieben.
                        item.ID_PUFFER = (idPuf > 0) ? idPuf : 0;
                    }
                    else if (CheckType(item, WizardItemClass.PV_TYP, WizardItemClass.REF_PV_TYP))
                    {
                        int idPv = new PhotovoltaikCtrl().CopyFromStamm(item.Bezeichner, projektID);
                        if (idPv > 0) item.ID_PV = idPv;
                    }
                    else if (CheckType(item, WizardItemClass.SOLAR_TYP, WizardItemClass.REF_SOLAR_TYP))
                    {
                        int idSol = new SolarkollektorenCtrl().CopyFromStamm(item.Bezeichner, projektID);
                        if (idSol > 0) item.ID_Solar = idSol;
                    }

                    // SQL mit allen Feldern aus dem Original
                    string sql = @"INSERT INTO Tab_Energieanlagen 
                        (ID_Projekt, Bezeichner, Betriebsart, Sperrung, Sperrzeit_von, Sperrzeit_bis, 
                         Vorlauf, Rücklauf, Bivalenter_Betrieb, Abschaltpunkt, Nutzungszeit, Grenzleistung, 
                         Kollektormodulanzahl, PV_Leistung, Neigung, Azimut, ID_Type, 
                         ID_WP, ID_Solar, ID_PV, ID_SP, ID_KESSEL, ID_BHKW, ID_PUFFER, 
                         Heizstab, Volumen, rendeMix, Solaranteil)
                        VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";

                    // Parameter exakt in der Reihenfolge des SQL-Strings
                    OleDbParameter[] ps = {
                        new OleDbParameter("@pID", projektID),
                        new OleDbParameter("@bez", item.Bezeichner ?? (object)DBNull.Value),
                        new OleDbParameter("@art", item.Betriebsart ?? (object)DBNull.Value),
                        new OleDbParameter("@sperr", item.Sperrung),
                        new OleDbParameter("@svon", item.Sperrzeit_von),
                        new OleDbParameter("@sbis", item.Sperrzeit_bis),
                        new OleDbParameter("@vor", item.Vorlauf),
                        new OleDbParameter("@rueck", item.Ruecklauf),
                        new OleDbParameter("@biv", item.Bivalenter_Betrieb),
                        new OleDbParameter("@ab", item.Abschaltpunkt),
                        new OleDbParameter("@nutz", item.Nutzungszeit),
                        new OleDbParameter("@grenz", item.Grenzleistung),
                        new OleDbParameter("@koll", item.Kollektormodulanzahl),
                        new OleDbParameter("@pvleist", item.PV_Leistung),
                        new OleDbParameter("@neig", item.m_Neigung),
                        new OleDbParameter("@azim", item.m_Azimut),
                        new OleDbParameter("@type", item.ID_Type),
                
                        // Fremdschlüssel-Logik (IDs nur setzen, wenn der Typ passt)
                        new OleDbParameter("@wp", CheckType(item, WizardItemClass.WP_TYP, WizardItemClass.REF_WP_TYP) ? item.ID_WP : (object)DBNull.Value),
                        new OleDbParameter("@sol", CheckType(item, WizardItemClass.SOLAR_TYP, WizardItemClass.REF_SOLAR_TYP) ? item.ID_Solar : (object)DBNull.Value),
                        new OleDbParameter("@pv", CheckType(item, WizardItemClass.PV_TYP, WizardItemClass.REF_PV_TYP) ? item.ID_PV : (object)DBNull.Value),
                        new OleDbParameter("@sp", CheckType(item, WizardItemClass.SP_TYP, WizardItemClass.REF_SP_TYP) ? item.ID_SP : (object)DBNull.Value),
                        new OleDbParameter("@kes", CheckType(item, WizardItemClass.KESSEL_TYP, WizardItemClass.REF_KESSEL_TYP) ? item.ID_Kessel : (object)DBNull.Value),
                        new OleDbParameter("@bhkw", (item.ID_Type == WizardItemClass.BHKW_TYP) ? item.ID_BHKW : (object)DBNull.Value),
                        new OleDbParameter("@puf", (item.ID_Type == WizardItemClass.PUFFER_TYP && item.ID_PUFFER > 0) ? item.ID_PUFFER : (object)DBNull.Value),

                        new OleDbParameter("@stab", item.Heizstab),
                        new OleDbParameter("@vol", item.Volumen),
                        new OleDbParameter("@mix", item.rendeMix),
                        new OleDbParameter("@solan", item.Solaranteil)
                    };

                    if (!DataRepository.ExecuteSQL(sql, ps)) return false;
                }

                Console.WriteLine("Daten erfolgreich aktualisiert.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Fehler beim Aktualisieren der Daten: " + ex.Message);
                return false;
            }
        }

        // Kleine Hilfsfunktion für die Typprüfung (kommt mit in die Ctrl)
        private bool CheckType(WErzeugerModel item, int typ, int refTyp)
        {
            return item.ID_Type == typ || item.ID_Type == refTyp;
        }
        
        public bool Add_Projekt_ZuordungGebäude(int projektID, List<Z_ProjGebModel> list)
        {
            GebaeudeStammCtrl ctrlStamm = new GebaeudeStammCtrl();
            foreach (var item in list)
            {
                // 1) Projekt-Zuordnung (Z_ProjektGebaeude) mit eigener ID anlegen.
                int zID = DataRepository.GetMaxID("Z_ProjektGebaeude") + 1;
                string sqlZ = "INSERT INTO Z_ProjektGebaeude (ID, ID_Projekt, Wohnflaeche_Waermebedarf, " +
                    "Einheit_Waermebedarf_Wohnflaeche, Jahresnutzungsgrad, dezWarmwasserbereitung) VALUES (?,?,?,?,?,?)";
                OleDbParameter[] psZ = {
                    new OleDbParameter("@id", OleDbType.Integer) { Value = zID },
                    new OleDbParameter("@pid", OleDbType.Integer) { Value = projektID },
                    new OleDbParameter("@fl", OleDbType.Double) { Value = item.Wohnflaeche },
                    new OleDbParameter("@Einheit", OleDbType.VarWChar) { Value = (object)(item.Einheit ?? "") },
                    new OleDbParameter("@jng", OleDbType.Double) { Value = item.Jahresnutzungsgrad },
                    new OleDbParameter("@dez", OleDbType.Boolean) { Value = item.DezentralWarmwasser }
                };
                if (!DataRepository.ExecuteSQL(sqlZ, psZ)) return false;

                // 2) Gebaeude-Stammdatensatz in die Projekt-Tabelle Tab_Gebaeude kopieren
                //    (setzt ID_Projekt und die Verknuepfung ID_ProjektGebaeude = zID).
                if (ctrlStamm.CopyFromStamm(item.Gebaeudename, projektID, zID) <= 0) return false;
            }
            return true;
        }

        public bool Add_Projekt(ref int projektID, ProjektModel model)
        {
            string sql = "INSERT INTO Tab_Projekt (Projektname, Bearbeiter, Beschreibung, Kunde, Aenderungsdatum, ID_Klimaregion, Erstelldatum) VALUES (?,?,?,?,?,?,?)";

            OleDbParameter[] ps = {
                new OleDbParameter("@name", model.m_szProjektname),
                new OleDbParameter("@bearb", model.m_szBearbeiter),
                new OleDbParameter("@besch", model.m_szBeschreibung),
                new OleDbParameter("@kunde", model.m_szKunde),
                new OleDbParameter("@date", OleDbType.Date) { Value = model.m_Aenderungsdatum },
                new OleDbParameter("@klima", model.m_ID_Klimaregion),
                new OleDbParameter("@edate", OleDbType.Date) { Value = model.m_Erstelldatum }
            };

            // Aufruf deiner neuen, zentralen Methode
            int generierteId = DataRepository.ExecuteInsertAndGetId(sql, ps);

            // Wenn die ID größer als 0 ist, war das Einfügen erfolgreich
            if (generierteId > 0)
            {
                projektID = generierteId; // Über ref-Parameter an den Aufrufer zurückgeben

                // Klimadaten-Kopie fuer das Projekt anlegen (falls noetig) und
                // Tab_Projekt.ID_Klimaregion auf die Projekt-Kopie setzen (gefuehrt wird nur der Name).
                KlimaregionStammCtrl.ApplyRegionByNameToProjekt(model.m_szKlimaregion, projektID);

                return true;
            }
            else
            {
                return false;
            }
        }

        public bool Update_Projekt(int projektID, ProjektModel model)
        {
            // Klimadaten-Kopie fuer das Projekt anlegen (falls noetig); liefert die Projekt-Region-ID.
            int projRegId = KlimaregionStammCtrl.ApplyRegionByNameToProjekt(model.m_szKlimaregion, projektID);
            if (projRegId > 0) model.m_ID_Klimaregion = projRegId;

            string sql = "UPDATE Tab_Projekt SET Projektname=?, Bearbeiter=?, ID_Klimaregion=?, Aenderungsdatum=?, Kunde=?, Beschreibung=? WHERE ID=?";
            OleDbParameter[] ps = {
                new OleDbParameter("@name", model.m_szProjektname),
                new OleDbParameter("@bearb", model.m_szBearbeiter),
                new OleDbParameter("@klima", model.m_ID_Klimaregion),
                new OleDbParameter("@date", OleDbType.Date) { Value = DateTime.Now },
                new OleDbParameter("@kunde", model.m_szKunde),
                new OleDbParameter("@besch", model.m_szBeschreibung),
                new OleDbParameter("@id", projektID)
            };
            return DataRepository.ExecuteSQL(sql, ps);
        }

        public bool Add_SP(int projektID, List<StromspeicherModel> list)
        {
            foreach (var item in list)
            {
                string sql = @"INSERT INTO Tab_Energieanlagen 
                               (ID_Projekt, Bezeichner, ID_Type, ID_SP) 
                               VALUES (?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@bez", item.m_szBezeichner ?? ""),
                    new OleDbParameter("@type", 4), // Typ 4 Stromspeicher
                    new OleDbParameter("@spID", item.m_ID)
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }

        public bool Add_WaermebedarfExtern(int projektID, List<Z_ProjWaermebedarfModel> list)
        {
            int nextID = DataRepository.GetMaxID("Z_ProjektWaermebedarf", "ID_Z") + 1;

            foreach (var item in list)
            {
                // Stamm-Ganglinie (+ Daten) bei Bedarf ins Projekt kopieren und die Projekt-Ganglinie-ID verwenden.
                int projGanglinieId = WaermebedarfStammCtrl.ApplyGanglinieToProjekt(item.m_szBezeichner, projektID);
                if (projGanglinieId <= 0) projGanglinieId = item.m_ID_Ganglinie;

                string sql = "INSERT INTO Z_ProjektWaermebedarf (ID_Z, ID_Projekt, ID_Ganglinie, Bezeichner) VALUES (?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", nextID++),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@gID", projGanglinieId),
                    new OleDbParameter("@bez", item.m_szBezeichner ?? "")
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }

        public bool Add_Projekt_Prozess(int projektID, List<Z_ProjektProzesswaermeModel> list)
        {
            int nextID = DataRepository.GetMaxID("Z_Projekt_Prozesswaerme", "ID") + 1;

            foreach (var item in list)
            {
                // Stamm-Prozess (+ Typ-Profil) bei Bedarf ins Projekt kopieren und die Projekt-ID verwenden.
                int projPwId = ProzesswaermeStammCtrl.CopyFromStamm(item.szProzessname, projektID);
                if (projPwId > 0) item.ID_Prozesswaerme = projPwId;

                string sql = "INSERT INTO Z_Projekt_Prozesswaerme (ID, ID_Projekt, ID_Prozesswaerme, Bezeichner, Summe) VALUES (?, ?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", nextID++),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@pwID", item.ID_Prozesswaerme),
                    new OleDbParameter("@bez", item.szProzessname ?? ""),
                    new OleDbParameter("@sum", item.Summe)
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }

        public bool Add_Projekt_Stromverbraucher(int projektID, List<Z_ProjektStromverbraucherModel> list)
        {
            int nextID = DataRepository.GetMaxID("Z_Projekt_Stromverbraucher", "ID") + 1;

            foreach (var item in list)
            {
                // Stamm-Stromverbraucher (+ Typ-Profil) bei Bedarf ins Projekt kopieren und die Projekt-ID verwenden.
                int projSvId = StromverbraucherStammCtrl.CopyFromStamm(item.m_szVerbraucher, projektID);
                if (projSvId > 0) item.m_ID_Stromverbraucher = projSvId;

                string sql = "INSERT INTO Z_Projekt_Stromverbraucher (ID, ID_Projekt, ID_Stromverbraucher, Bezeichner, Summe) VALUES (?, ?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", nextID++),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@svID", item.m_ID_Stromverbraucher),
                    new OleDbParameter("@bez", item.m_szVerbraucher ?? ""),
                    new OleDbParameter("@sum", item.m_Summe)
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }

        public bool Add_Stromganglinie(int projektID, List<Z_ProjektStromganglinieModel> list)
        {
            foreach (var item in list)
            {
                // Stamm-Ganglinie (+ Daten) bei Bedarf ins Projekt kopieren und die Projekt-Ganglinie-ID verwenden.
                int projGanglinieId = StromganglinieStammCtrl.ApplyGanglinieToProjekt(item.m_szStromganglinie, projektID);
                if (projGanglinieId <= 0) projGanglinieId = item.m_ID_Stromganglinie;

                string sql = "INSERT INTO Z_ProjektStromganglinie (ID_Projekt, ID_Ganglinie, Bezeichner) VALUES (?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@gID", projGanglinieId),
                    new OleDbParameter("@bez", item.m_szStromganglinie ?? "")
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }

        public bool Add_Solarganglinie(int projektID, List<Z_ProjektSolarganglinieModel> list)
        {
            int nextID = DataRepository.GetMaxID("Z_ProjektSolarganglinie", "ID") + 1;

            foreach (var item in list)
            {
                // Stamm-Ganglinie (+ Daten) bei Bedarf ins Projekt kopieren und die Projekt-Ganglinie-ID verwenden.
                int projGanglinieId = SolarganglinieStammCtrl.ApplyGanglinieToProjekt(item.m_szSolarganglinie, projektID);
                if (projGanglinieId <= 0) projGanglinieId = item.m_ID_Solarganglinie;

                string sql = "INSERT INTO Z_ProjektSolarganglinie (ID, ID_Projekt, ID_Ganglinie, Bezeichner) VALUES (?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", nextID++),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@gID", projGanglinieId),
                    new OleDbParameter("@bez", item.m_szSolarganglinie ?? "")
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }

        public bool Add_Projekt_Brauchwasser(int projektID, List<Z_ProjektBrauchwasserModel> list)
        {
            int nextID = DataRepository.GetMaxID("Z_Projekt_Brauchwasser", "ID") + 1;

            foreach (var item in list)
            {
                // Stamm-Brauchwasser (+ Typ-Profil) bei Bedarf ins Projekt kopieren und die Projekt-ID verwenden.
                int projBwId = BrauchwasserStammCtrl.CopyFromStamm(item.szBezeichner, projektID);
                if (projBwId > 0) item.ID_Brauchwasser = projBwId;

                string sql = "INSERT INTO Z_Projekt_Brauchwasser (ID, ID_Projekt, ID_Brauchwasser, Bezeichner, Summe) VALUES (?, ?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", nextID++),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@bwID", item.ID_Brauchwasser),
                    new OleDbParameter("@bez", item.szBezeichner ?? ""),
                    new OleDbParameter("@sum", item.Summe)
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }
 
    }
}
