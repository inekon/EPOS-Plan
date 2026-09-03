using System;
using System.Collections.Generic;
using System.Data;

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
                new DbParam("@n", name.Trim()),
                new DbParam("@g", string.IsNullOrWhiteSpace(gruppe)
                    ? (object)DBNull.Value : gruppe.Trim()),
                new DbParam("@id", carrierId));
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
                new DbParam("@id", id),
                new DbParam("@n", name.Trim()),
                new DbParam("@c", CodeFuer(name, id)),
                new DbParam("@g", string.IsNullOrWhiteSpace(gruppe)
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
                new DbParam("@id", quellId));
            if (dt == null || dt.Rows.Count == 0) return 0;
            DataRow q = dt.Rows[0];

            int id = NaechsteId();
            string name = VariantenName(Convert.ToString(q["name"]));

            var spalten = new List<string>();
            var werte = new List<DbParam>();
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
                var p = new DbParam("@" + s, wert ?? DBNull.Value);
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
                new DbParam("@id", carrierId));
            return DataRepository.ExecuteSQL(
                "DELETE FROM energy_carrier WHERE id = ?",
                new DbParam("@id", carrierId));
        }

        // ------------------------------------------ Projektzuordnung (Ä10) ---
        //
        // Der offene KD4-Punkt § 7.2: Katalogträger werden dem Projekt per
        // Zuordnungszeile in energy_project_settings zugeteilt — die Zeile nennt nur
        // Projekt und Träger, es GELTEN also die Katalogwerte (eine Wahrheit;
        // Projektwerte entstehen erst durch Pflege im Projektkontext).
        //
        // DIESES VERSPRECHEN LÖST SEIT ETAPPE BK3 DAS INSERT SELBST EIN. Befund BK2:
        // Nicht genannte Spalten landen in Access nicht als NULL, sondern auf ihrem
        // SPALTENDEFAULT — und der ist hier bei allen neun Wertspalten 0. Die
        // Kurzfassung „nur Projekt und Träger nennen" schrieb also neun Nullen statt
        // neun Leerwerten und hielt damit genau das Gegenteil dessen, was darüber
        // steht: custom_hi = 0 gilt dem Kosten-Dialog als GEPFLEGTER Heizwert (Feld
        // 0,00 statt Katalogwert, kWh unerreichbar, Speichern abgelehnt),
        // custom_price_base = 0 schattet den Katalog-Grundpreis still, und
        // ID_Umrechnung = 0 zeigt auf keine Regel (die Autowert-IDs beginnen bei 1).
        // Die acht Wertspalten werden deshalb ausdrücklich als DBNull geschrieben.
        //
        // ID_UMRECHNUNG IST KEINE WERTKOPIE. Sie benennt die RECHENEINHEIT der
        // Zeile — die Identitätsregel des Brennstoffs (from_unit = to_unit) —, nicht
        // einen Preis oder Heizwert; sie sagt, WORIN gerechnet wird, und bleibt damit
        // Ä10-konform. Ermittelt wird sie über dieselbe Regel wie im Wizard-Weg
        // (WizardCtrl.ConvIdErmitteln); gibt es keine, bleibt es bei -1 — dem
        // „keine Regel"-Wert, den auch die Wizard-Zuordnung einträgt.
        //
        // FÜR DIE EMISSIONEN bleibt der BK2-Befund gültig: co2/so2/nox waren als 0
        // schon folgenlos, weil die Lesekette des EmissionsFaktorLaders nur einen
        // Wert GRÖSSER als 0 als „gepflegt" zählt und eine 0 auf die aktive
        // Katalogzeile durchfällt. NULL sagt dasselbe nun auch in der Datenbank.
        // BESTANDSZEILEN BLEIBEN UNANGETASTET — kein Heilungsschritt.

        /// <summary>Katalogträger, die dem Projekt noch nicht zugeordnet sind.</summary>
        internal static List<EnergyCarrier> NichtZugeordnete(int projektId)
        {
            var zugeordnet = new HashSet<int>();
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT [ID_Energieträger] FROM energy_project_settings WHERE ID_Projekt = ?",
                    new DbParam("@p", projektId));
                if (dt != null)
                    foreach (DataRow r in dt.Rows)
                        if (r[0] != DBNull.Value) zugeordnet.Add(Convert.ToInt32(r[0]));
            }
            catch { }

            var frei = new List<EnergyCarrier>();
            foreach (EnergyCarrier c in KostenSummenCtrl.GetAllCarriers(0))
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
                new DbParam("@p", projektId),
                new DbParam("@c", carrierId));
            if (da != null && Convert.ToInt32(da) > 0) return true;

            return DataRepository.ExecuteSQL(
                "INSERT INTO energy_project_settings " +
                "(ID_Projekt, [ID_Energieträger], custom_hi, custom_Hs, custom_price_work, " +
                " custom_price_base, custom_price_power, co2, so2, nox, ID_Umrechnung) " +
                "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                new DbParam("@p", projektId),
                new DbParam("@c", carrierId),
                new DbParam("@hi", DbParamTyp.Double) { Wert = DBNull.Value },
                new DbParam("@hs", DbParamTyp.Double) { Wert = DBNull.Value },
                new DbParam("@pw", DbParamTyp.Double) { Wert = DBNull.Value },
                new DbParam("@pb", DbParamTyp.Double) { Wert = DBNull.Value },
                new DbParam("@pp", DbParamTyp.Double) { Wert = DBNull.Value },
                new DbParam("@co2", DbParamTyp.Double) { Wert = DBNull.Value },
                new DbParam("@so2", DbParamTyp.Double) { Wert = DBNull.Value },
                new DbParam("@nox", DbParamTyp.Double) { Wert = DBNull.Value },
                new DbParam("@conv", DbParamTyp.Integer)
                { Wert = UmrechnungFuer(carrierId) });
        }

        /// <summary>
        /// Recheneinheit der neuen Zuordnungszeile: die Identitätsregel des
        /// Brennstoffs (<c>from_unit = to_unit =</c> Abrechnungseinheit aus
        /// <c>Tab_Brennstoff_Stamm</c>) — dieselben zwei Nachschlagewerke und
        /// dieselbe Ableitung wie in <c>WizardCtrl.TraegerSatzAnlegen</c>.
        /// Fehlt der Brennstoffbezug oder die Einheit, gilt -1 („keine Regel").
        /// </summary>
        private static int UmrechnungFuer(int carrierId)
        {
            try
            {
                object oBrennstoff = DataRepository.ExecuteScalar(
                    "SELECT ID_Brennstoff FROM energy_carrier WHERE id = ?",
                    new DbParam("@cid", carrierId));
                if (oBrennstoff == null || oBrennstoff == DBNull.Value) return -1;
                int idBrennstoff = Convert.ToInt32(oBrennstoff);
                if (idBrennstoff <= 0) return -1;

                object oEinheit = DataRepository.GetValueById(
                    "Tab_Brennstoff_Stamm", "Einheit", idBrennstoff);
                // BEWUSST OHNE Trim: TraegerSatzAnlegen trimmt ebenfalls nicht. Eine
                // Einheit mit Randleerzeichen fände hier sonst eine Regel, die der
                // Wizard-Weg verfehlt — und beide Wege sollen dieselbe Zeile schreiben.
                string einheit = (oEinheit != null && oEinheit != DBNull.Value)
                    ? oEinheit.ToString() : "";
                if (einheit.Length == 0) return -1;

                return WizardCtrl.ConvIdErmitteln(idBrennstoff, einheit);
            }
            catch { return -1; }
        }

        /// <summary>Zuordnung lösen; Anlagen des Projekts halten den Träger.</summary>
        internal static bool AusProjektEntfernen(int projektId, int carrierId, out string grund)
        {
            grund = "";
            object a = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Energieanlagen " +
                "WHERE ID_Projekt = ? AND ID_Carrier = ?",
                new DbParam("@p", projektId),
                new DbParam("@c", carrierId));
            int anlagen = (a == null || a == DBNull.Value) ? 0 : Convert.ToInt32(a);
            if (anlagen > 0)
            {
                grund = anlagen + " Anlage(n) des Projekts verwenden den Träger.";
                return false;
            }

            DataRepository.ExecuteSQL(
                "DELETE FROM energy_price WHERE id_projekt = ? AND carrier_id = ?",
                new DbParam("@p", projektId),
                new DbParam("@c", carrierId));
            return DataRepository.ExecuteSQL(
                "DELETE FROM energy_project_settings " +
                "WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", projektId),
                new DbParam("@c", carrierId));
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
                object o = DataRepository.ExecuteScalar(sql, new DbParam("@id", carrierId));
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
                    new DbParam("@n", kandidat));
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
                new DbParam("@c", code));
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
