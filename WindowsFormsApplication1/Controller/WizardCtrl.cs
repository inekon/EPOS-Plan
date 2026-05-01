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
            return DataRepository.ExecuteSQL("DELETE FROM Z_ProjektGebaeude WHERE ID_Projekt = ?",
                new OleDbParameter[] { new OleDbParameter("@pID", projektID) });
        }

        public bool Del_Projekt_ZuordungGebäude(int projektID, int ID)
        {
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
                // Start-ID ermitteln
                int nextID = DataRepository.GetMaxID("Tab_Energieanlagen", "ID") + 1;

                foreach (var item in list)
                {
                    // SQL mit allen Feldern aus dem Original
                    string sql = @"INSERT INTO Tab_Energieanlagen 
                        (ID, ID_Projekt, Bezeichner, Betriebsart, Sperrung, Sperrzeit_von, Sperrzeit_bis, 
                         Vorlauf, Rücklauf, Bivalenter_Betrieb, Abschaltpunkt, Nutzungszeit, Grenzleistung, 
                         Kollektormodulanzahl, PV_Leistung, Neigung, Azimut, ID_Type, 
                         ID_WP, ID_Solar, ID_PV, ID_SP, ID_KESSEL, ID_BHKW, ID_PUFFER, 
                         Heizstab, Volumen, rendeMix, Solaranteil) 
                        VALUES (?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?,?)";

                    // Parameter exakt in der Reihenfolge des SQL-Strings
                    OleDbParameter[] ps = {
                        new OleDbParameter("@id", nextID++),
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
                        new OleDbParameter("@puf", (item.ID_Type == WizardItemClass.PUFFER_TYP) ? item.ID_PUFFER : (object)DBNull.Value),

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
            int nextID = DataRepository.GetMaxID("Z_ProjektGebaeude", "ID") + 1;
            foreach (var item in list)
            {
                string sql = "INSERT INTO Z_ProjektGebaeude (ID, ID_Projekt, ID_Gebaeude, Wohnflaeche_Waermebedarf, " +
                    "Einheit_Waermebedarf_Wohnflaeche, Jahresnutzungsgrad) VALUES (?,?,?,?,?,?)";
                OleDbParameter[] ps = {
                    new OleDbParameter("@id", nextID++),
                    new OleDbParameter("@pid", projektID),
                    new OleDbParameter("@gid", item.ID_Gebaeude),
                    new OleDbParameter("@fl", item.Wohnflaeche),
                    new OleDbParameter("@Einheit",item.Einheit),
                    new OleDbParameter("@jng",item.Jahresnutzungsgrad)
                };
                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }

        public bool Add_Projekt(int projektID, ProjektModel model)
        {
            string sql = "INSERT INTO Tab_Projekt (ID, Projektname, Bearbeiter, Beschreibung, Kunde, Aenderungsdatum, ID_Klimaregion, Erstelldatum) VALUES (?,?,?,?,?,?,?,?)";
            OleDbParameter[] ps = {
                new OleDbParameter("@id", projektID),
                new OleDbParameter("@name", model.m_szProjektname),
                new OleDbParameter("@bearb", model.m_szBearbeiter),
                new OleDbParameter("@besch", model.m_szBeschreibung),
                new OleDbParameter("@kunde", model.m_szKunde),
                new OleDbParameter("@date", OleDbType.Date) { Value = model.m_Aenderungsdatum },
                new OleDbParameter("@klima", model.m_ID_Klimaregion),
                new OleDbParameter("@edate", OleDbType.Date) { Value = model.m_Erstelldatum }
            };
            return DataRepository.ExecuteSQL(sql, ps);
        }

        public bool Update_Projekt(int projektID, ProjektModel model)
        {
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
            // Start-ID für diesen Block holen
            int nextID = DataRepository.GetMaxID("Tab_Energieanlagen", "ID") + 1;

            foreach (var item in list)
            {
                string sql = @"INSERT INTO Tab_Energieanlagen 
                               (ID, ID_Projekt, Bezeichner, ID_Type, ID_SP) 
                               VALUES (?, ?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", nextID++),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@bez", item.m_szBezeichner ?? ""),
                    new OleDbParameter("@type", 4), // Typ 4 scheint Stromspeicher zu sein
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
                string sql = "INSERT INTO Z_ProjektWaermebedarf (ID_Z, ID_Projekt, ID_Ganglinie, Bezeichner) VALUES (?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", nextID++),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@gID", item.m_ID_Ganglinie),
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
            int nextID = DataRepository.GetMaxID("Z_ProjektStromganglinie", "ID_Z") + 1;

            foreach (var item in list)
            {
                string sql = "INSERT INTO Z_ProjektStromganglinie (ID_Z, ID_Projekt, ID_Ganglinie, Bezeichner) VALUES (?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", nextID++),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@gID", item.m_ID_Stromganglinie),
                    new OleDbParameter("@bez", item.m_szStromganglinie ?? "")
                };

                if (!DataRepository.ExecuteSQL(sql, ps)) return false;
            }
            return true;
        }

        public bool Add_Solarganglinie(int projektID, List<Z_ProjektSolarganglinieModel> list)
        {
            int nextID = DataRepository.GetMaxID("Z_ProjektSolarganglinie", "ID_Z") + 1;

            foreach (var item in list)
            {
                string sql = "INSERT INTO Z_ProjektSolarganglinie (ID_Z, ID_Projekt, ID_Ganglinie, Bezeichner) VALUES (?, ?, ?, ?)";

                OleDbParameter[] ps = {
                    new OleDbParameter("@id", nextID++),
                    new OleDbParameter("@pID", projektID),
                    new OleDbParameter("@gID", item.m_ID_Solarganglinie),
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