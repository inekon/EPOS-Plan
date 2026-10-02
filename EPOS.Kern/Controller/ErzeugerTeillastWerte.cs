using System;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Teillastfelder von Wärmepumpe und BHKW an der Datenbankgrenze</b> (Welle M4, Schemaschritt
    /// <see cref="ErzeugerTeillastSchema.SCHRITT"/>) — Lesen, Schreiben, Kopieren und Prüfen an EINER Stelle
    /// für Katalog und Projektkopie.
    /// </summary>
    /// <remarks>
    /// <para><b>Leer ist nicht 0.</b> Alle Felder sind nullbar; <c>null</c> heißt „nicht gepflegt", und das
    /// Gerät rechnet dann wie zuvor. Deshalb schreibt diese Klasse <c>DBNull</c> und nie eine 0 an die
    /// Stelle eines leeren Feldes.</para>
    /// <para><b>Ein eigener Schreibschritt.</b> Die Felder gehen mit einer eigenen <c>UPDATE</c>-Anweisung
    /// nach dem Schreiben des Satzes in die Datenbank — nur, wenn die Spalten stehen. Eine noch nicht
    /// migrierte Datenbank (vor Schritt <see cref="ErzeugerTeillastSchema.SCHRITT"/>) speichert damit
    /// alles Übrige wie zuvor.</para>
    /// </remarks>
    public static class ErzeugerTeillastWerte
    {
        // =============================================================================
        //  Wärmepumpe (WP1)
        // =============================================================================

        /// <summary>Liest Mindestleistung und C_d aus einer Zeile von <c>Tab_WP</c> oder <c>Tab_WP_STAMM</c>.</summary>
        public static void WpAusZeile(WPModel ziel, DataRow zeile)
        {
            if (ziel == null || zeile == null) return;
            ziel.MindestleistungKw = Zahl(zeile, ErzeugerTeillastSchema.SPALTE_WP_MINDESTLEISTUNG);
            ziel.TaktverlustfaktorCd = Zahl(zeile, ErzeugerTeillastSchema.SPALTE_WP_CD);
        }

        /// <summary>Überträgt die zwei Felder von einem Modell auf ein anderes.</summary>
        public static void WpUebertragen(WPModel von, WPModel nach)
        {
            if (von == null || nach == null) return;
            nach.MindestleistungKw = von.MindestleistungKw;
            nach.TaktverlustfaktorCd = von.TaktverlustfaktorCd;
        }

        /// <summary>
        /// Schreibt die zwei Felder an den Satz <paramref name="id"/> von <paramref name="tabelle"/>
        /// (<see cref="ErzeugerTeillastSchema.TAB_WP"/> oder <see cref="ErzeugerTeillastSchema.TAB_WP_STAMM"/>);
        /// ohne die Spalten nichts. Mit <paramref name="vorgang"/> in dessen Transaktion.
        /// </summary>
        public static void WpSchreiben(string tabelle, int id, double? mindestleistungKw, double? cd,
                                       DbVorgang vorgang = null)
        {
            if (id <= 0 || !Gueltig(tabelle) || !ErzeugerTeillastSchema.WpSpaltenVorhanden(tabelle)) return;
            string sql = "UPDATE \"" + tabelle + "\" SET \"" + ErzeugerTeillastSchema.SPALTE_WP_MINDESTLEISTUNG +
                         "\" = ?, \"" + ErzeugerTeillastSchema.SPALTE_WP_CD + "\" = ? WHERE ID = ?";
            DbParam[] ps =
            {
                new DbParam("@pmin", Wert(mindestleistungKw)),
                new DbParam("@cd", Wert(cd)),
                new DbParam("@id", id)
            };
            if (vorgang != null) vorgang.Ausfuehren(sql, ps);
            else DataRepository.ExecuteNonQuery(sql, ps);
        }

        /// <summary>Kopiert die zwei Felder aus einer Katalogzeile an die Projektkopie <paramref name="neueId"/>.</summary>
        public static void WpKopieren(DataRow stamm, int neueId, DbVorgang vorgang = null)
        {
            if (stamm == null) return;
            WpSchreiben(ErzeugerTeillastSchema.TAB_WP, neueId,
                        Zahl(stamm, ErzeugerTeillastSchema.SPALTE_WP_MINDESTLEISTUNG),
                        Zahl(stamm, ErzeugerTeillastSchema.SPALTE_WP_CD), vorgang);
        }

        /// <summary>
        /// Die Plausibilität der zwei Felder — <c>null</c>, wenn alles passt, sonst der erste Grund:
        /// Mindestleistung 0 … 1 000 kW und höchstens die maximale Heizleistung (wenn gepflegt), C_d 0 … 1.
        /// </summary>
        public static string WpVerstoss(double? mindestleistungKw, double? cd, double maxPthermKw)
        {
            if (mindestleistungKw.HasValue)
            {
                double bis = maxPthermKw > 0 && maxPthermKw < ErzeugerTeillastSchema.WP_MINDESTLEISTUNG_MAX_KW
                    ? maxPthermKw : ErzeugerTeillastSchema.WP_MINDESTLEISTUNG_MAX_KW;
                string g = Bereich(Text("WPS_LBL_MINDESTLEISTUNG_KURZ", "Mindestleistung"), mindestleistungKw.Value, 0, bis);
                if (g != null) return g;
            }
            if (cd.HasValue)
            {
                string g = Bereich(Text("WPS_LBL_TAKTVERLUST_CD_KURZ", "Teillastkoeffizient C_d"), cd.Value, 0,
                                   ErzeugerTeillastSchema.CD_MAX);
                if (g != null) return g;
            }
            return null;
        }

        // =============================================================================
        //  BHKW (BH1, BH2)
        // =============================================================================

        /// <summary>Liest die vier Felder aus einer Zeile von <c>Tab_BHKW</c> oder <c>Tab_BHKW_STAMM</c>.</summary>
        public static void BhkwAusZeile(BHKWModel ziel, DataRow zeile)
        {
            if (ziel == null || zeile == null) return;
            ziel.m_Wirkungsgrad_el_Teillast50 = Zahl(zeile, ErzeugerTeillastSchema.SPALTE_BHKW_ETA_EL50);
            ziel.m_Wirkungsgrad_th_Teillast50 = Zahl(zeile, ErzeugerTeillastSchema.SPALTE_BHKW_ETA_TH50);
            ziel.m_Anfahrverlust_kWh = Zahl(zeile, ErzeugerTeillastSchema.SPALTE_BHKW_ANFAHRVERLUST);
            double? laufzeit = Zahl(zeile, ErzeugerTeillastSchema.SPALTE_BHKW_MINDESTLAUFZEIT);
            ziel.m_Mindestlaufzeit_min = laufzeit.HasValue ? (int?)Convert.ToInt32(laufzeit.Value) : null;
        }

        /// <summary>Überträgt die vier Felder von einem Modell auf ein anderes.</summary>
        public static void BhkwUebertragen(BHKWModel von, BHKWModel nach)
        {
            if (von == null || nach == null) return;
            nach.m_Wirkungsgrad_el_Teillast50 = von.m_Wirkungsgrad_el_Teillast50;
            nach.m_Wirkungsgrad_th_Teillast50 = von.m_Wirkungsgrad_th_Teillast50;
            nach.m_Anfahrverlust_kWh = von.m_Anfahrverlust_kWh;
            nach.m_Mindestlaufzeit_min = von.m_Mindestlaufzeit_min;
        }

        /// <summary>
        /// Schreibt die vier Felder an den Satz <paramref name="id"/> von <paramref name="tabelle"/>
        /// (<see cref="ErzeugerTeillastSchema.TAB_BHKW"/> oder <see cref="ErzeugerTeillastSchema.TAB_BHKW_STAMM"/>);
        /// ohne die Spalten nichts.
        /// </summary>
        public static void BhkwSchreiben(string tabelle, int id, BHKWModel m, DbVorgang vorgang = null)
        {
            if (m == null || id <= 0 || !Gueltig(tabelle) || !ErzeugerTeillastSchema.BhkwSpaltenVorhanden(tabelle)) return;
            string sql = "UPDATE \"" + tabelle + "\" SET \"" + ErzeugerTeillastSchema.SPALTE_BHKW_ETA_EL50 + "\" = ?, \"" +
                         ErzeugerTeillastSchema.SPALTE_BHKW_ETA_TH50 + "\" = ?, \"" +
                         ErzeugerTeillastSchema.SPALTE_BHKW_ANFAHRVERLUST + "\" = ?, \"" +
                         ErzeugerTeillastSchema.SPALTE_BHKW_MINDESTLAUFZEIT + "\" = ? WHERE ID = ?";
            DbParam[] ps =
            {
                new DbParam("@el50", Wert(m.m_Wirkungsgrad_el_Teillast50)),
                new DbParam("@th50", Wert(m.m_Wirkungsgrad_th_Teillast50)),
                new DbParam("@anf", Wert(m.m_Anfahrverlust_kWh)),
                new DbParam("@lauf", Wert(m.m_Mindestlaufzeit_min)),
                new DbParam("@id", id)
            };
            if (vorgang != null) vorgang.Ausfuehren(sql, ps);
            else DataRepository.ExecuteNonQuery(sql, ps);
        }

        /// <summary>Kopiert die vier Felder aus einer Katalogzeile an die Projektkopie <paramref name="neueId"/>.</summary>
        public static void BhkwKopieren(DataRow stamm, int neueId)
        {
            if (stamm == null) return;
            var m = new BHKWModel();
            BhkwAusZeile(m, stamm);
            BhkwSchreiben(ErzeugerTeillastSchema.TAB_BHKW, neueId, m);
        }

        /// <summary>
        /// Die Plausibilität der vier Felder — <c>null</c>, wenn alles passt, sonst der erste Grund: die
        /// Wirkungsgrade bei 50 % Last als Faktor 0 … 1, Anfahrverlust 0 … 100 kWh, Mindestlaufzeit 0 … 60 min.
        /// </summary>
        public static string BhkwVerstoss(BHKWModel m)
        {
            if (m == null) return null;
            if (m.m_Wirkungsgrad_el_Teillast50.HasValue)
            {
                string g = Bereich(Text("BHKW_LBL_ETA_EL50_KURZ", "Elektrischer Wirkungsgrad bei 50 % Last"),
                                   m.m_Wirkungsgrad_el_Teillast50.Value, 0, ErzeugerTeillastSchema.ETA50_MAX);
                if (g != null) return g;
            }
            if (m.m_Wirkungsgrad_th_Teillast50.HasValue)
            {
                string g = Bereich(Text("BHKW_LBL_ETA_TH50_KURZ", "Thermischer Wirkungsgrad bei 50 % Last"),
                                   m.m_Wirkungsgrad_th_Teillast50.Value, 0, ErzeugerTeillastSchema.ETA50_MAX);
                if (g != null) return g;
            }
            if (m.m_Anfahrverlust_kWh.HasValue)
            {
                string g = Bereich(Text("BHKW_LBL_ANFAHRVERLUST_KURZ", "Anfahrverlust je Start"),
                                   m.m_Anfahrverlust_kWh.Value, 0, ErzeugerTeillastSchema.ANFAHRVERLUST_MAX_KWH);
                if (g != null) return g;
            }
            if (m.m_Mindestlaufzeit_min.HasValue)
            {
                string g = Bereich(Text("BHKW_LBL_MINDESTLAUFZEIT_KURZ", "Mindestlaufzeit"),
                                   m.m_Mindestlaufzeit_min.Value, 0, ErzeugerTeillastSchema.MINDESTLAUFZEIT_MAX_MIN);
                if (g != null) return g;
            }
            return null;
        }

        // =============================================================================
        //  Hilfen
        // =============================================================================

        /// <summary>Ein nullbarer Wert als Parameterwert: <c>null</c> wird <c>DBNull</c>.</summary>
        public static object Wert(double? wert) => wert.HasValue ? (object)wert.Value : DBNull.Value;

        /// <summary>Ein nullbarer Wert als Parameterwert: <c>null</c> wird <c>DBNull</c>.</summary>
        public static object Wert(int? wert) => wert.HasValue ? (object)wert.Value : DBNull.Value;

        /// <summary>Nur die vier Tabellen des Schritts sind Schreibziele.</summary>
        private static bool Gueltig(string tabelle)
            => Array.IndexOf(ErzeugerTeillastSchema.TABELLEN, tabelle) >= 0;

        private static string Bereich(string feld, double wert, double von, double bis)
        {
            if (wert >= von && wert <= bis) return null;
            return string.Format(
                Text("KBROW_MSG_WERT_BEREICH", "„{0}“ muss zwischen {1} und {2} liegen."),
                feld, von.ToString(CultureInfo.CurrentCulture), bis.ToString(CultureInfo.CurrentCulture));
        }

        private static double? Zahl(DataRow zeile, string spalte)
        {
            if (!zeile.Table.Columns.Contains(spalte)) return null;
            object v = zeile[spalte];
            if (v == null || v == DBNull.Value) return null;
            return Convert.ToDouble(v, CultureInfo.InvariantCulture);
        }

        private static string Text(string schluessel, string rueckfall)
        {
            string t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t;
        }
    }
}
