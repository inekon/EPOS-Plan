using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    public class StromganglinieDatenModel
    {
        public int m_ID_GanglinieDaten { get; set; }
        public double m_Wert { get; set; }

        public StromganglinieDatenModel()
        {
            m_ID_GanglinieDaten = 0;
            m_Wert = 0;
        }
    }

    class StromganglinieDatenCtrl : StromganglinieDatenModel
    {
        // Verwendung der bestehenden Liste aus der Altanwendung
        public List<StromganglinieDatenModel> list_GanglinieDaten = new List<StromganglinieDatenModel>();

        // Kompatibilitätseigenschaften für das einheitliche Listen-Schema
        public int rows => list_GanglinieDaten.Count;
        public List<StromganglinieDatenModel> items => list_GanglinieDaten;

        public StromganglinieDatenCtrl()
        {
            // Konstruktor bereinigt - Ressourcen werden lokal verwaltet
        }

        public bool Delete(string szName)
        {
            try
            {
                // Standardkonforme DELETE-Syntax ohne "*" und Typkorrektur über expliziten Parameter
                string sql = "DELETE FROM Tab_StromganglinieDaten WHERE ID_Ganglinie = ?";

                DbParam paramId = new DbParam("@idGang", DbParamTyp.Integer);
                paramId.Wert = m_ID_GanglinieDaten;

                DbParam[] ps = { paramId };

                return DataRepository.ExecuteSQL(sql, ps);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Fehler bei Delete: " + ex.Message);
                return false;
            }
        }

        public bool Insert()
        {
            if (list_GanglinieDaten == null || list_GanglinieDaten.Count == 0) return true;

            try
            {
                // Ein Vorgang bündelt alle Schreibvorgänge in EINER Transaktion
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    string sqlInsert = "INSERT INTO Tab_StromganglinieDaten (ID_Ganglinie, Wert) VALUES (?, ?)";

                    try
                    {
                        foreach (var item in list_GanglinieDaten)
                        {
                            v.Ausfuehren(sqlInsert,
                                new DbParam("@id", DbParamTyp.Integer) { Wert = item.m_ID_GanglinieDaten },
                                new DbParam("@wert", DbParamTyp.Double) { Wert = item.m_Wert });
                        }

                        // Erst jetzt wird alles physisch auf die Platte geschrieben
                        v.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        v.Rollback();
                        Console.WriteLine("Fehler beim Massen-Insert in der Schleife: " + ex.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Allgemeiner Verbindungsfehler bei Massen-Insert: " + ex.Message);
                return false;
            }
        }

        public bool InsertKompletteGanglinie(StromganglinieCtrl kopfCtrl, List<string> roheWerte)
        {
            if (roheWerte == null || roheWerte.Count == 0) return true;

            try
            {
                // 1./2. EIN Vorgang (Verbindung + Transaktion) für beide Tabellen
                using (DbVorgang v = DataRepository.Vorgang())
                {
                    // ======================================================================
                    // VORGANG 1: Der Kopfdatensatz (Tab_Stromganglinie)
                    // ======================================================================

                    // Zuerst die ID ermitteln
                    int neueGanglinieID = 1;
                    int count = Convert.ToInt32(v.Skalar("SELECT COUNT(*) FROM Tab_Stromganglinie") ?? 0);
                    if (count > 0)
                    {
                        neueGanglinieID = Convert.ToInt32(v.Skalar("SELECT MAX(ID) FROM Tab_Stromganglinie") ?? 0) + 1;
                    }

                    // ID an das Kopf-Objekt zurückgeben, damit die UI sie nach dem Erfolg kennt
                    kopfCtrl.m_ID_Ganglinie = neueGanglinieID;

                    // Kopfdatensatz über die geteilte Verbindung einfügen
                    string sqlKopf = "INSERT INTO Tab_Stromganglinie (ID, Bezeichner, Zeitinterval) VALUES (?, ?, ?)";
                    v.Ausfuehren(sqlKopf,
                        new DbParam("@id", DbParamTyp.Integer) { Wert = neueGanglinieID },
                        new DbParam("@bez", DbParamTyp.VarWChar) { Wert = kopfCtrl.m_szBezeichner ?? (object)DBNull.Value },
                        new DbParam("@interval", DbParamTyp.Integer) { Wert = kopfCtrl.m_Zeitinterval });

                    // ======================================================================
                    // VORGANG 2: Die 8760 Datenpunkte (Tab_StromganglinieDaten)
                    // ======================================================================
                    string sqlDaten = "INSERT INTO Tab_StromganglinieDaten (ID_Ganglinie, Wert) VALUES (?, ?)";

                    try
                    {
                        // Die Liste leeren und neu befüllen für interne Kompatibilität
                        this.list_GanglinieDaten.Clear();

                        foreach (string roherWert in roheWerte)
                        {
                            // Konvertierung mit InvariantCulture fängt Punkt/Komma-Probleme ab
                            double parsedWert = double.Parse(roherWert, System.Globalization.CultureInfo.InvariantCulture);

                            v.Ausfuehren(sqlDaten,
                                new DbParam("@idGang", DbParamTyp.Integer) { Wert = neueGanglinieID },
                                new DbParam("@wert", DbParamTyp.Double) { Wert = parsedWert });

                            // Optional: Das lokale Modell befüllen, falls du die Liste 'list_GanglinieDaten' danach noch im Code brauchst
                            StromganglinieDatenModel model = new StromganglinieDatenModel();
                            model.m_ID_GanglinieDaten = neueGanglinieID;
                            model.m_Wert = parsedWert;
                            this.list_GanglinieDaten.Add(model);
                        }

                        // ======================================================================
                        // ERFOLG: Erst jetzt wird ALLES permanent geschrieben
                        // ======================================================================
                        v.Commit();
                        return true;
                    }
                    catch (Exception ex)
                    {
                        // Wird aufgerufen, wenn beim double.Parse ODER beim Schleifen-Insert ein Fehler auftritt
                        v.Rollback();
                        Console.WriteLine("Schleifen-Fehler. Transaktion zurückgerollt: " + ex.Message);
                        return false;
                    }
                }
            }
            catch (Exception ex)
            {
                // Wird aufgerufen, falls die Verbindung fehlschlägt oder der Kopf-Insert abstürzt
                Console.WriteLine("Allgemeiner Transaktions-Fehler: " + ex.Message);
                return false;
            }
        }
    }
}