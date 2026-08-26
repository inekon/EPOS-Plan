using System;
using System.Collections.Generic;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// KATALOGPFLEGE der Energieträger (Nachtrag Ä9, Nutzerabnahme 26.08.2026 —
    /// der in KD4 offene Punkt „Trägervarianten + Katalog-Schreibweg"):
    /// Anlegen, Variante, Umbenennen/Gruppe, Löschen von
    /// <c>energy_carrier</c>-Stammzeilen. UI-frei und testbar; der Dialog
    /// (<see cref="Form_Energietraeger"/>) ruft ausschließlich hierher.
    ///
    /// <para><b>Löschen ist geschützt:</b> Ein Träger, den Projekte
    /// (<c>energy_project_settings</c>), Anlagen (<c>Tab_Energieanlagen</c>)
    /// oder die Preishistorie (<c>energy_price</c>) verwenden, wird nicht
    /// gelöscht — der Grund wird benannt statt still verweigert.</para>
    ///
    /// <para><b>IDs per MAX+1</b> (ADR-001, kein AutoWert-Vertrauen); der
    /// technische <c>code</c> folgt dem Namen und wird bei Kollision mit der
    /// neuen ID entschärft.</para>
    /// </summary>
    internal static class EnergietraegerKatalogCtrl
    {
        /// <summary>Alle vorhandenen Gruppen (für die Gruppen-Klappliste; die
        /// Liste ist editierbar — neue Gruppen entstehen durch Eintippen).</summary>
        internal static List<string> Gruppen()
        {
            var liste = new List<string>();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT DISTINCT group_code FROM energy_carrier ORDER BY group_code");
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        if (r[0] != DBNull.Value && Convert.ToString(r[0]).Length > 0)
                            liste.Add(Convert.ToString(r[0]));
            }
            catch { }
            return liste;
        }

        /// <summary>Bezeichnung und Gruppe eines Trägers ändern; der technische
        /// <c>code</c> bleibt stehen (er ist Verweisanker, kein Anzeigename).</summary>
        internal static bool Umbenennen(int carrierId, string name, string gruppe)
        {
            if (carrierId <= 0 || string.IsNullOrWhiteSpace(name)) return false;
            return DataRepository.ExecuteSQL(
                "UPDATE energy_carrier SET [name] = ?, group_code = ? WHERE id = ?",
                new OleDbParameter("@n", name.Trim()),
                new OleDbParameter("@g", string.IsNullOrWhiteSpace(gruppe)
                    ? (object)DBNull.Value : gruppe.Trim()),
                new OleDbParameter("@id", carrierId));
        }

        /// <summary>
        /// Neuer Katalogträger mit neutraler Vorbelegung (Gasmodell, Abrechnung
        /// in kWh mit Faktor 1, keine Preise/Emissionen = nicht gepflegt).
        /// Rückgabe neue ID oder 0.
        /// </summary>
        internal static int Neu(string name, string gruppe)
        {
            if (string.IsNullOrWhiteSpace(name)) return 0;
            int id = NaechsteId();
            bool ok = DataRepository.ExecuteSQL(
                "INSERT INTO energy_carrier " +
                "(id, ID_Brennstoff, [name], code, group_code, pricing_model, billing_unit, " +
                " hi_kwh_per_unit, hs_kwh_per_unit, price_work, price_base, price_power, " +
                " co2, so2, nox, is_active) " +
                "VALUES (?, 0, ?, ?, ?, 'GASEOUS_FUEL', 'kWh', 1, 1, 0, 0, 0, 0, 0, 0, TRUE)",
                new OleDbParameter("@id", id),
                new OleDbParameter("@n", name.Trim()),
                new OleDbParameter("@c", CodeFuer(name, id)),
                new OleDbParameter("@g", string.IsNullOrWhiteSpace(gruppe)
                    ? "Sonstige" : gruppe.Trim()));
            return ok ? id : 0;
        }

        /// <summary>
        /// Variante eines Trägers: vollständige Kopie der Katalogzeile (alle
        /// Spalten außer <c>id</c>; Name/Code mit Varianten-Suffix). Genau dafür
        /// gedacht, je Träger ABWEICHENDE Emissionswerte oder Preise als eigenen
        /// Eintrag zu führen (Nutzerwunsch 26.08.2026).
        /// </summary>
        internal static int Variante(int quellId)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT * FROM energy_carrier WHERE id = ?",
                new OleDbParameter("@id", quellId));
            if (dt == null || dt.Rows.Count == 0) return 0;
            DataRow q = dt.Rows[0];

            int id = NaechsteId();
            string name = VariantenName(Convert.ToString(q["name"]));

            var spalten = new List<string>();
            var werte = new List<OleDbParameter>();
            foreach (DataColumn sp in dt.Columns)
            {
                string s = sp.ColumnName;
                spalten.Add("[" + s + "]");
                object wert;
                if (string.Equals(s, "id", StringComparison.OrdinalIgnoreCase)) wert = id;
                else if (string.Equals(s, "name", StringComparison.OrdinalIgnoreCase)) wert = name;
                else if (string.Equals(s, "code", StringComparison.OrdinalIgnoreCase))
                    wert = CodeFuer(name, id);
                else wert = q[s];
                var p = new OleDbParameter("@" + s, wert ?? DBNull.Value);
                werte.Add(p);
            }
            string sql = "INSERT INTO energy_carrier (" + string.Join(", ", spalten.ToArray()) +
                         ") VALUES (" + string.Join(", ", Fragezeichen(spalten.Count)) + ")";
            return DataRepository.ExecuteSQL(sql, werte.ToArray()) ? id : 0;
        }

        /// <summary>Löschen mit Verwendungsschutz; <paramref name="grund"/> nennt
        /// bei false, was den Träger hält.</summary>
        internal static bool Loeschen(int carrierId, out string grund)
        {
            grund = "";
            if (carrierId <= 0) return false;

            int projekte = Zaehle(
                "SELECT COUNT(*) FROM energy_project_settings WHERE [ID_Energieträger] = ?", carrierId);
            int anlagen = Zaehle(
                "SELECT COUNT(*) FROM Tab_Energieanlagen WHERE ID_Carrier = ?", carrierId);
            if (projekte > 0 || anlagen > 0)
            {
                grund = string.Format(
                    "{0} Projektzuordnung(en), {1} Anlage(n) verweisen auf den Träger.",
                    projekte, anlagen);
                return false;
            }

            // Verwaiste Preishistorie darf mit (sie gehört zum Träger).
            DataRepository.ExecuteSQL(
                "DELETE FROM energy_price WHERE carrier_id = ?",
                new OleDbParameter("@id", carrierId));
            return DataRepository.ExecuteSQL(
                "DELETE FROM energy_carrier WHERE id = ?",
                new OleDbParameter("@id", carrierId));
        }

        // ------------------------------------------ Projektzuordnung (Ä10) ---
        //
        // Der offene KD4-Punkt § 7.2: Katalogträger werden dem Projekt per
        // Zuordnungszeile in energy_project_settings zugeteilt — alle
        // custom_-Felder bleiben NULL, es GELTEN also die Katalogwerte (eine
        // Wahrheit; Projektwerte entstehen erst durch Pflege im Projektkontext).

        /// <summary>Katalogträger, die dem Projekt noch nicht zugeordnet sind.</summary>
        internal static List<EnergyCarrier> NichtZugeordnete(int projektId)
        {
            var zugeordnet = new HashSet<int>();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT [ID_Energieträger] FROM energy_project_settings WHERE ID_Projekt = ?",
                    new OleDbParameter("@p", projektId));
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        if (r[0] != DBNull.Value) zugeordnet.Add(Convert.ToInt32(r[0]));
            }
            catch { }

            var frei = new List<EnergyCarrier>();
            foreach (EnergyCarrier c in Form_Kosten.GetAllCarriers(0))
                if (!zugeordnet.Contains(c.ID)) frei.Add(c);
            return frei;
        }

        /// <summary>Katalogträger ins Projekt übernehmen (idempotent).</summary>
        internal static bool InsProjekt(int projektId, int carrierId)
        {
            if (projektId <= 0 || carrierId <= 0) return false;
            object da = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_project_settings " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new OleDbParameter("@p", projektId),
                new OleDbParameter("@c", carrierId));
            if (da != null && Convert.ToInt32(da) > 0) return true;

            return DataRepository.ExecuteSQL(
                "INSERT INTO energy_project_settings (ID_Projekt, [ID_Energieträger]) " +
                "VALUES (?, ?)",
                new OleDbParameter("@p", projektId),
                new OleDbParameter("@c", carrierId));
        }

        /// <summary>Zuordnung lösen; Anlagen des Projekts halten den Träger.</summary>
        internal static bool AusProjektEntfernen(int projektId, int carrierId, out string grund)
        {
            grund = "";
            object a = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Carrier = ?",
                new OleDbParameter("@p", projektId),
                new OleDbParameter("@c", carrierId));
            int anlagen = (a == null || a == DBNull.Value) ? 0 : Convert.ToInt32(a);
            if (anlagen > 0)
            {
                grund = anlagen + " Anlage(n) des Projekts verwenden den Träger.";
                return false;
            }

            DataRepository.ExecuteSQL(
                "DELETE FROM energy_price WHERE id_projekt = ? AND carrier_id = ?",
                new OleDbParameter("@p", projektId),
                new OleDbParameter("@c", carrierId));
            return DataRepository.ExecuteSQL(
                "DELETE FROM energy_project_settings " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new OleDbParameter("@p", projektId),
                new OleDbParameter("@c", carrierId));
        }

        // ------------------------------------------------------------- Helfer ---

        private static int NaechsteId()
        {
            object o = DataRepository.ExecuteScalar("SELECT MAX(id) FROM energy_carrier");
            return ((o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o)) + 1;
        }

        private static int Zaehle(string sql, int carrierId)
        {
            try
            {
                object o = DataRepository.ExecuteScalar(sql, new OleDbParameter("@id", carrierId));
                return (o == null || o == DBNull.Value) ? 0 : Convert.ToInt32(o);
            }
            catch { return 0; }
        }

        /// <summary>Name „X" → „X Variante", „X Variante 2", … (erste freie Nummer).</summary>
        private static string VariantenName(string basis)
        {
            string kandidat = basis + " Variante";
            for (int n = 2; n < 100; n++)
            {
                object o = DataRepository.ExecuteScalar(
                    "SELECT COUNT(*) FROM energy_carrier WHERE [name] = ?",
                    new OleDbParameter("@n", kandidat));
                if (o == null || Convert.ToInt32(o) == 0) return kandidat;
                kandidat = basis + " Variante " + n;
            }
            return basis + " Variante " + Guid.NewGuid().ToString("N").Substring(0, 4);
        }

        private static string CodeFuer(string name, int id)
        {
            string code = (name ?? "").Trim();
            object o = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM energy_carrier WHERE code = ?",
                new OleDbParameter("@c", code));
            return (o != null && Convert.ToInt32(o) > 0) ? code + " " + id : code;
        }

        private static string[] Fragezeichen(int n)
        {
            var f = new string[n];
            for (int i = 0; i < n; i++) f[i] = "?";
            return f;
        }
    }
}
