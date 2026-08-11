using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Zentrale Logik für Projektvarianten (Konzept_Berichtserstellung_EPOS-Plan.md, Kap. 3.3).
    ///
    /// Eine Variante ist ein vollwertiges Kopie-Projekt (ProjektDuplizierenCtrl);
    /// die Seitentabelle Tab_Variante (ID, ID_Projekt, ID_ProjektRef, Variantenname)
    /// verknüpft die Variante (ID_Projekt) mit ihrem Stammprojekt (ID_ProjektRef).
    ///
    /// Diese Klasse bündelt die bislang in Form_Variantentest verstreute Logik, damit
    /// Formular, Menüweg ("Als Variante speichern…") und Berichtsmodul dieselbe
    /// Implementierung nutzen. Kein UI-Bezug (Meldungen laufen über Rückgabewerte).
    /// </summary>
    public class VariantenCtrl
    {
        public const string TAB_VARIANTE = "Tab_Variante";

        /// <summary>Eine Zeile der Vergleichsgruppe (Stamm oder Variante).</summary>
        public class VarianteInfo
        {
            public int IdProjekt;
            public string Projektname = "";
            public string Variantenname = "";   // leer beim Stamm
            public bool IstStamm;
        }

        // ------------------------------------------------------------- Lesen

        /// <summary>Stamm + alle Varianten des Stammprojekts (Stamm als erste Zeile).</summary>
        public List<VarianteInfo> LadeGruppe(int idStamm, string stammName)
        {
            List<VarianteInfo> gruppe = new List<VarianteInfo>();
            gruppe.Add(new VarianteInfo { IdProjekt = idStamm, Projektname = stammName ?? "", IstStamm = true });

            try
            {
                string sql = "SELECT v.ID_Projekt, v.Variantenname, p.Projektname " +
                             "FROM " + TAB_VARIANTE + " v INNER JOIN Tab_Projekt p ON v.ID_Projekt = p.ID " +
                             "WHERE v.ID_ProjektRef = ? ORDER BY v.Variantenname";
                DataTable dt = DataRepository.GetDataTable(sql, new OleDbParameter("?", idStamm));
                foreach (DataRow r in dt.Rows)
                {
                    gruppe.Add(new VarianteInfo
                    {
                        IdProjekt = Convert.ToInt32(r["ID_Projekt"]),
                        Variantenname = r["Variantenname"]?.ToString() ?? "",
                        Projektname = r["Projektname"]?.ToString() ?? "",
                        IstStamm = false
                    });
                }
            }
            catch { /* leere Gruppe genügt dem Aufrufer als Antwort */ }

            return gruppe;
        }

        /// <summary>IDs aller Projekte, die bereits als Stamm dienen (ID_ProjektRef in Tab_Variante).</summary>
        public HashSet<int> LiesStammProjektIds()
        {
            var set = new HashSet<int>();
            try
            {
                DataTable dt = DataRepository.GetDataTable("SELECT DISTINCT ID_ProjektRef FROM " + TAB_VARIANTE);
                foreach (DataRow r in dt.Rows)
                    if (r[0] != DBNull.Value) set.Add(Convert.ToInt32(r[0]));
            }
            catch { }
            return set;
        }

        /// <summary>Liefert ID_ProjektRef, wenn idProjekt eine Variante ist, sonst -1.</summary>
        public int StammRefDerVariante(int idProjekt)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT ID_ProjektRef FROM " + TAB_VARIANTE + " WHERE ID_Projekt = ?",
                    new OleDbParameter("@proj", idProjekt));
                if (o != null) return Convert.ToInt32(o);
            }
            catch { }
            return -1;
        }

        public bool ProjektnameExistiert(string name)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Projekt WHERE Projektname = ?",
                new OleDbParameter("@name", name));
            return o != null && Convert.ToInt32(o) > 0;
        }

        // ------------------------------------------------------------- Anlegen

        /// <summary>
        /// Legt aus einem Stammprojekt eine Variante an: Projekt duplizieren,
        /// Tab_Variante-Verknüpfung eintragen, Energieträger-Einstellungen kopieren.
        /// Rückgabe: neue Projekt-ID der Variante, -1 bei Fehler (fehler beschreibt die Ursache).
        /// </summary>
        public int AnlegenAusStamm(int idStamm, string stammName, string bezeichner, out string fehler)
        {
            fehler = null;
            bezeichner = (bezeichner ?? "").Trim();
            if (idStamm <= 0 || string.IsNullOrWhiteSpace(stammName)) { fehler = "Kein Stammprojekt angegeben."; return -1; }
            if (bezeichner.Length == 0) { fehler = "Bitte einen Bezeichner für die Variante eingeben."; return -1; }

            StelleVariantentabelleSicher();

            // Eindeutigen Projektnamen bilden: "<Stamm> - <Bezeichner>" (ggf. mit Zähler).
            string basisName = stammName + " - " + bezeichner;
            string neuerName = basisName;
            int n = 2;
            while (ProjektnameExistiert(neuerName)) { neuerName = basisName + " (" + n + ")"; n++; }

            try
            {
                int neueId = new ProjektDuplizierenCtrl().Duplizieren(stammName, neuerName);
                if (neueId <= 0) { fehler = "Variante konnte nicht angelegt werden (Duplizieren fehlgeschlagen)."; return -1; }

                int vid = DataRepository.GetMaxID(TAB_VARIANTE, "ID") + 1;
                string ins = "INSERT INTO " + TAB_VARIANTE + " (ID, ID_Projekt, ID_ProjektRef, Variantenname) VALUES (?, ?, ?, ?)";
                DataRepository.ExecuteSQL(ins,
                    new OleDbParameter("@id", vid),
                    new OleDbParameter("@proj", neueId),
                    new OleDbParameter("@ref", idStamm),
                    new OleDbParameter("@name", bezeichner));

                KopiereEnergieEinstellungen(idStamm, neueId);
                return neueId;
            }
            catch (Exception ex)
            {
                fehler = "Fehler beim Anlegen: " + ex.Message;
                return -1;
            }
        }

        /// <summary>
        /// Kopiert projektbezogene Energieträger-Einstellungen (energy_project_settings)
        /// und die Preishistorie (energy_price) vom Stamm auf die Variante. Best effort:
        /// fehlen Kostenmodul/Tabellen, läuft der Anlegevorgang trotzdem weiter.
        /// </summary>
        public void KopiereEnergieEinstellungen(int vonProjekt, int nachProjekt)
        {
            try
            {
                string sqlSettings =
                    "INSERT INTO energy_project_settings " +
                    "(ID_Projekt, ID_Energieträger, custom_price_work, custom_price_power, custom_hi, custom_Hs, " +
                    " custom_price_base, ID_Umrechnung, co2, so2, nox) " +
                    "SELECT ?, ID_Energieträger, custom_price_work, custom_price_power, custom_hi, custom_Hs, " +
                    " custom_price_base, ID_Umrechnung, co2, so2, nox " +
                    "FROM energy_project_settings WHERE ID_Projekt = ?";
                DataRepository.ExecuteSQL(sqlSettings,
                    new OleDbParameter("@neu", nachProjekt),
                    new OleDbParameter("@von", vonProjekt));

                string sqlPrices =
                    "INSERT INTO energy_price " +
                    "(carrier_id, id_projekt, arbeitspreis, heizwert, grundpreis, valid_from, arbeitspreis_unit, leistungspreis) " +
                    "SELECT carrier_id, ?, arbeitspreis, heizwert, grundpreis, valid_from, arbeitspreis_unit, leistungspreis " +
                    "FROM energy_price WHERE id_projekt = ?";
                DataRepository.ExecuteSQL(sqlPrices,
                    new OleDbParameter("@neu", nachProjekt),
                    new OleDbParameter("@von", vonProjekt));
            }
            catch { /* Hinweis obliegt dem Aufrufer; das Anlegen selbst gilt als gelungen */ }
        }

        // ------------------------------------------------------------- Löschen

        /// <summary>
        /// Löscht eine Variante: Verknüpfung, Energieanlagen, Projekt (Detailtabellen
        /// fallen per Löschweitergabe mit weg). Kein Stammprojekt-Löschen über diesen Weg.
        /// </summary>
        public bool LoescheVariante(int idProjekt, string projektname, out string fehler)
        {
            fehler = null;
            if (StammRefDerVariante(idProjekt) <= 0)
            { fehler = "Das Projekt ist keine Variante (Stammprojekte werden hier nicht gelöscht)."; return false; }

            try
            {
                DataRepository.ExecuteSQL("DELETE FROM " + TAB_VARIANTE + " WHERE ID_Projekt = ?",
                    new OleDbParameter("@proj", idProjekt));

                WErzeugerCtrl werz = new WErzeugerCtrl { ID_Projekt = idProjekt };
                werz.Delete();

                new ProjektCtrl().Delete(projektname);
                return true;
            }
            catch (Exception ex)
            {
                fehler = "Fehler beim Löschen: " + ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Entfernt Waisen aus Tab_Variante (Einträge, deren Projekt oder deren Stamm
        /// nicht mehr existiert — die Tabelle hat keine Löschweitergabe, Befund B5).
        /// Rückgabe: Anzahl entfernter Einträge.
        /// </summary>
        public int EntferneWaisen()
        {
            int entfernt = 0;
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT v.ID FROM " + TAB_VARIANTE + " v " +
                    "LEFT JOIN Tab_Projekt p ON v.ID_Projekt = p.ID " +
                    "LEFT JOIN Tab_Projekt s ON v.ID_ProjektRef = s.ID " +
                    "WHERE p.ID IS NULL OR s.ID IS NULL");
                foreach (DataRow r in dt.Rows)
                {
                    DataRepository.ExecuteSQL("DELETE FROM " + TAB_VARIANTE + " WHERE ID = ?",
                        new OleDbParameter("@id", Convert.ToInt32(r["ID"])));
                    entfernt++;
                }
            }
            catch { }
            return entfernt;
        }

        // ------------------------------------------------------------- Schema

        /// <summary>Legt Tab_Variante an, falls sie noch nicht existiert (tolerant).</summary>
        public void StelleVariantentabelleSicher()
        {
            try
            {
                using (OleDbConnection conn = new OleDbConnection(DataRepository.GetConnectionString()))
                {
                    conn.Open();
                    DataTable schema = conn.GetOleDbSchemaTable(OleDbSchemaGuid.Tables,
                        new object[] { null, null, TAB_VARIANTE, "TABLE" });
                    if (schema != null && schema.Rows.Count > 0) return;

                    string ddl = "CREATE TABLE " + TAB_VARIANTE + " (" +
                                 "ID LONG CONSTRAINT PK_Variante PRIMARY KEY, " +
                                 "ID_Projekt LONG CONSTRAINT UQ_VarProj UNIQUE, " +
                                 "ID_ProjektRef LONG, " +
                                 "Variantenname TEXT(255))";
                    using (OleDbCommand cmd = new OleDbCommand(ddl, conn))
                        cmd.ExecuteNonQuery();
                }
            }
            catch { /* best effort — existiert dann ggf. schon */ }
        }
    }
}
