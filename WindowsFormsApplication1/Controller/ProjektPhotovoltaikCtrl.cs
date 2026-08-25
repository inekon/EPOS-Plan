using System;
using System.Data;
using System.Data.OleDb;

namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Zugriff auf Tab_ProjektPhotovoltaik (PV-Konzept Paragraf 6.1, Etappe P3)
    // und die Marktwert-Solar-Rueckfallketten (Paragraf 6.3, Nachtrag N2).
    // Durchgaengig DataRepository mit ?-Parametern; MAX+1-Hausmuster.
    // ---------------------------------------------------------------------------
    public class ProjektPhotovoltaikCtrl
    {
        public const string TABELLE = "Tab_ProjektPhotovoltaik";

        // =====================================================================
        // Lesen
        // =====================================================================

        /// <summary>Die PV-Verguetungszeile des Projekts; null = keine gepflegt.</summary>
        public ProjektPhotovoltaikModel Lies(int idProjekt)
        {
            try
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT * FROM [" + TABELLE + "] WHERE ID_Projekt = ?",
                    new OleDbParameter("@p", idProjekt));
                if (dt == null || dt.Rows.Count == 0) return null;
                return AusZeile(dt.Rows[0]);
            }
            catch { return null; }
        }

        /// <summary>
        /// Wie <see cref="Lies"/>, aber nie null: Ohne Zeile kommt ein VORBELEGTES
        /// Modell zurueck (Aktiv=false, Ueberschusseinspeisung, DV-Entgelt 0,40 -
        /// N5, Ausfallanteil 20 % - F5, IBN 1.1. des laufenden Jahres) - bewusst
        /// OHNE Schreiben; erst Speichern legt die Zeile an (kein DDL-DEFAULT).
        /// </summary>
        public ProjektPhotovoltaikModel LiesOderVorbelegt(int idProjekt)
        {
            ProjektPhotovoltaikModel m = Lies(idProjekt);
            if (m != null) return m;

            return new ProjektPhotovoltaikModel
            {
                ID = 0,
                ID_Projekt = idProjekt,
                Aktiv = false,
                Vermarktungsform = DbWerte.PV_VERMARKTUNG_EV,
                Einspeiseart = DbWerte.PV_EINSPEISEART_UEBERSCHUSS,
                Inbetriebnahme = new DateTime(DateTime.Now.Year, 1, 1),
                DvEntgelt = 0.40,
                AusfallanteilProzent = 20.0,
                Par51_Anwenden = DbWerte.PV_SCHALTER_AUTO,
                Par51a_Kompensieren = true,
                Kappung60_Anwenden = DbWerte.PV_SCHALTER_AUTO,
                MarktwertEntwicklung = 0,
                BezugAusPreisreihe = false
            };
        }

        // =====================================================================
        // Schreiben
        // =====================================================================

        /// <summary>Upsert der Projektzeile (eine je Projekt). true bei Erfolg.</summary>
        public bool Speichern(ProjektPhotovoltaikModel m)
        {
            if (m == null || m.ID_Projekt <= 0) return false;
            try
            {
                m.GeaendertAm = DateTime.Now;

                int rows = (int)DataRepository.ExecuteNonQuery(
                    "UPDATE [" + TABELLE + "] SET Aktiv = ?, Vermarktungsform = ?, " +
                    "Einspeiseart = ?, Inbetriebnahme = ?, KwpOverride = ?, AwOverride = ?, " +
                    "DvEntgelt = ?, PpaPreis = ?, PpaSpotAufschlag = ?, Par51_Anwenden = ?, " +
                    "IMSys_Einbaujahr = ?, AusfallanteilProzent = ?, Par51a_Kompensieren = ?, " +
                    "Kappung60_Anwenden = ?, MarktwertJahresmittel = ?, MarktwertEntwicklung = ?, " +
                    "BezugAusPreisreihe = ?, GeaendertAm = ? WHERE ID_Projekt = ?",
                    Parameter(m, projektAnsEnde: true));
                if (rows > 0) return true;

                object max = DataRepository.ExecuteScalar("SELECT MAX(ID) FROM [" + TABELLE + "]");
                m.ID = (max == null || max == DBNull.Value ? 0 : Convert.ToInt32(max)) + 1;

                return DataRepository.ExecuteSQL(
                    "INSERT INTO [" + TABELLE + "] (ID, ID_Projekt, Aktiv, Vermarktungsform, " +
                    "Einspeiseart, Inbetriebnahme, KwpOverride, AwOverride, DvEntgelt, PpaPreis, " +
                    "PpaSpotAufschlag, Par51_Anwenden, IMSys_Einbaujahr, AusfallanteilProzent, " +
                    "Par51a_Kompensieren, Kappung60_Anwenden, MarktwertJahresmittel, " +
                    "MarktwertEntwicklung, BezugAusPreisreihe, GeaendertAm) " +
                    "VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
                    ParameterInsert(m));
            }
            catch (Exception ex)
            {
                DataRepository.FehlerMelden("Die PV-Vergütungsangaben konnten nicht gespeichert werden: " + ex.Message);
                return false;
            }
        }

        // =====================================================================
        // Marktwerte (Paragraf 6.3 / N2)
        // =====================================================================

        /// <summary>
        /// Monatsmarktwert Solar [ct/kWh] aus der Stammreihe „Marktwert Solar" des
        /// Jahres (Tab_Preisreihe, Aufloesung Monat). null = Jahr oder Monat nicht
        /// gepflegt — dann greift die Rueckfallkette des Aufrufers
        /// (<see cref="Jahresmarktwert"/>, Fortschreibung in Etappe P4).
        /// </summary>
        public double? MarktwertMonatCt(int jahr, int monat)
        {
            if (monat < 1 || monat > 12) return null;
            try
            {
                object id = DataRepository.ExecuteScalar(
                    "SELECT MAX(ID) FROM [Tab_Preisreihe] " +
                    "WHERE Bezeichner = ? AND Jahr = ? AND ID_Projekt IS NULL",
                    new OleDbParameter("@b", DbWerte.PV_MARKTWERT_BEZEICHNER),
                    new OleDbParameter("@j", jahr));
                if (id == null || id == DBNull.Value) return null;

                double[] werte = new PreisreiheCtrl().ReadWerte(Convert.ToInt32(id));
                if (werte == null || werte.Length < monat) return null;
                return werte[monat - 1];
            }
            catch { return null; }
        }

        /// <summary>
        /// Jahresmarktwert Solar [ct/kWh] fuer die Marktpraemie (N2):
        /// Projekt-Override (<c>MarktwertJahresmittel</c>) vor dem AMTLICHEN
        /// Katalogwert des EXAKTEN Jahres (<c>EEG_JAHRESMARKTWERT_SOLAR</c>).
        /// null = beides fehlt (z. B. kuenftige Jahre) — die Fortschreibung ueber
        /// <c>MarktwertEntwicklung</c> ist Sache der Erloesbildung (P4), nicht
        /// dieses Lesers: Das Stichtagsmuster des Katalogs wuerde sonst still den
        /// VORJAHRESwert liefern und die Szenariofortschreibung verdecken.
        /// </summary>
        public double? Jahresmarktwert(int jahr, ProjektPhotovoltaikModel projekt)
        {
            if (projekt != null && projekt.MarktwertJahresmittel.HasValue &&
                projekt.MarktwertJahresmittel.Value > 0)
                return projekt.MarktwertJahresmittel.Value;

            try
            {
                GesetzParameter p = new GesetzKatalog()
                    .WertMitHerkunft(DbWerte.GESETZ_EEG_JAHRESMARKTWERT_SOLAR, jahr);
                if (p != null && p.JahrVon == jahr && p.Wert.HasValue) return p.Wert.Value;
            }
            catch { }
            return null;
        }

        // =====================================================================
        // Helfer
        // =====================================================================

        private static ProjektPhotovoltaikModel AusZeile(DataRow r)
        {
            var m = new ProjektPhotovoltaikModel
            {
                ID = Ganz(r, "ID") ?? 0,
                ID_Projekt = Ganz(r, "ID_Projekt") ?? 0,
                Aktiv = Wahr(r, "Aktiv"),
                Vermarktungsform = Text(r, "Vermarktungsform", DbWerte.PV_VERMARKTUNG_EV),
                Einspeiseart = Text(r, "Einspeiseart", DbWerte.PV_EINSPEISEART_UEBERSCHUSS),
                KwpOverride = Zahl(r, "KwpOverride"),
                AwOverride = Zahl(r, "AwOverride"),
                DvEntgelt = Zahl(r, "DvEntgelt"),
                PpaPreis = Zahl(r, "PpaPreis"),
                PpaSpotAufschlag = Zahl(r, "PpaSpotAufschlag"),
                Par51_Anwenden = Text(r, "Par51_Anwenden", DbWerte.PV_SCHALTER_AUTO),
                IMSys_Einbaujahr = Ganz(r, "IMSys_Einbaujahr"),
                AusfallanteilProzent = Zahl(r, "AusfallanteilProzent"),
                Par51a_Kompensieren = Wahr(r, "Par51a_Kompensieren"),
                Kappung60_Anwenden = Text(r, "Kappung60_Anwenden", DbWerte.PV_SCHALTER_AUTO),
                MarktwertJahresmittel = Zahl(r, "MarktwertJahresmittel"),
                MarktwertEntwicklung = Zahl(r, "MarktwertEntwicklung") ?? 0,
                BezugAusPreisreihe = Wahr(r, "BezugAusPreisreihe")
            };
            object ibn = r["Inbetriebnahme"];
            m.Inbetriebnahme = (ibn == null || ibn == DBNull.Value)
                ? DateTime.MinValue : Convert.ToDateTime(ibn);
            object g = r["GeaendertAm"];
            m.GeaendertAm = (g == null || g == DBNull.Value)
                ? (DateTime?)null : Convert.ToDateTime(g);
            return m;
        }

        private static OleDbParameter[] Parameter(ProjektPhotovoltaikModel m, bool projektAnsEnde)
        {
            var p = new System.Collections.Generic.List<OleDbParameter>
            {
                new OleDbParameter("@akt", m.Aktiv),
                new OleDbParameter("@ver", m.Vermarktungsform ?? DbWerte.PV_VERMARKTUNG_EV),
                new OleDbParameter("@art", m.Einspeiseart ?? DbWerte.PV_EINSPEISEART_UEBERSCHUSS),
                new OleDbParameter("@ibn", OleDbType.Date)
                    { Value = m.Inbetriebnahme == DateTime.MinValue ? (object)DBNull.Value : m.Inbetriebnahme },
                D("@kwp", m.KwpOverride), D("@aw", m.AwOverride), D("@dv", m.DvEntgelt),
                D("@ppa", m.PpaPreis), D("@spot", m.PpaSpotAufschlag),
                new OleDbParameter("@p51", m.Par51_Anwenden ?? DbWerte.PV_SCHALTER_AUTO),
                G("@ims", m.IMSys_Einbaujahr), D("@ausf", m.AusfallanteilProzent),
                new OleDbParameter("@p51a", m.Par51a_Kompensieren),
                new OleDbParameter("@kap", m.Kappung60_Anwenden ?? DbWerte.PV_SCHALTER_AUTO),
                D("@jw", m.MarktwertJahresmittel),
                new OleDbParameter("@mw", m.MarktwertEntwicklung),
                new OleDbParameter("@bez", m.BezugAusPreisreihe),
                new OleDbParameter("@ga", OleDbType.Date) { Value = m.GeaendertAm ?? (object)DBNull.Value }
            };
            if (projektAnsEnde) p.Add(new OleDbParameter("@pid", m.ID_Projekt));
            return p.ToArray();
        }

        private static OleDbParameter[] ParameterInsert(ProjektPhotovoltaikModel m)
        {
            var kopf = new System.Collections.Generic.List<OleDbParameter>
            {
                new OleDbParameter("@id", m.ID),
                new OleDbParameter("@pid", m.ID_Projekt)
            };
            kopf.AddRange(Parameter(m, projektAnsEnde: false));
            return kopf.ToArray();
        }

        private static OleDbParameter D(string name, double? wert)
        {
            return new OleDbParameter(name, OleDbType.Double)
            { Value = wert.HasValue ? (object)wert.Value : DBNull.Value };
        }

        private static OleDbParameter G(string name, int? wert)
        {
            return new OleDbParameter(name, OleDbType.Integer)
            { Value = wert.HasValue ? (object)wert.Value : DBNull.Value };
        }

        private static double? Zahl(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte)) return null;
            object v = r[spalte];
            return (v == null || v == DBNull.Value) ? (double?)null : Convert.ToDouble(v);
        }

        private static int? Ganz(DataRow r, string spalte)
        {
            double? z = Zahl(r, spalte);
            return z.HasValue ? (int?)Convert.ToInt32(z.Value) : null;
        }

        private static bool Wahr(DataRow r, string spalte)
        {
            if (!r.Table.Columns.Contains(spalte)) return false;
            object v = r[spalte];
            return v != null && v != DBNull.Value && Convert.ToBoolean(v);
        }

        private static string Text(DataRow r, string spalte, string rueckfall)
        {
            if (!r.Table.Columns.Contains(spalte)) return rueckfall;
            object v = r[spalte];
            string s = (v == null || v == DBNull.Value) ? null : Convert.ToString(v);
            return string.IsNullOrEmpty(s) ? rueckfall : s;
        }
    }
}
