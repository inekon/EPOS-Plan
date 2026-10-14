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
        /// Mindestleistung 0 … 1 000 kW und höchstens die Nennleistung (wenn gepflegt), C_d 0 … 1.
        /// </summary>
        public static string WpVerstoss(double? mindestleistungKw, double? cd, double nennleistungKw)
        {
            if (mindestleistungKw.HasValue)
            {
                double bis = nennleistungKw > 0 && nennleistungKw < ErzeugerTeillastSchema.WP_MINDESTLEISTUNG_MAX_KW
                    ? nennleistungKw : ErzeugerTeillastSchema.WP_MINDESTLEISTUNG_MAX_KW;
                string g = Bereich(Text("WPS_FELD_MINDESTLEISTUNG", "Mindestleistung"), mindestleistungKw.Value, 0, bis);
                if (g != null) return g;
            }
            if (cd.HasValue)
            {
                string g = Bereich(Text("WPS_FELD_TAKTVERLUST_CD", "Teillastkoeffizient C_d"), cd.Value, 0,
                                   ErzeugerTeillastSchema.CD_MAX);
                if (g != null) return g;
            }
            return null;
        }

        // =============================================================================
        //  BHKW (BH1, BH2)
        // =============================================================================

        /// <summary>Die vier Teillastfelder eines BHKW-Satzes — leer heißt „nicht gepflegt".</summary>
        public readonly record struct BhkwFelder(double? EtaEl50, double? EtaTh50, double? AnfahrverlustKwh,
                                                 int? MindestlaufzeitMin);

        /// <summary>Liest die vier Felder aus einer Zeile von <c>Tab_BHKW</c> oder <c>Tab_BHKW_STAMM</c>.</summary>
        public static BhkwFelder BhkwAusZeile(DataRow zeile)
        {
            if (zeile == null) return default;
            double? laufzeit = Zahl(zeile, ErzeugerTeillastSchema.SPALTE_BHKW_MINDESTLAUFZEIT);
            return new BhkwFelder(
                Zahl(zeile, ErzeugerTeillastSchema.SPALTE_BHKW_ETA_EL50),
                Zahl(zeile, ErzeugerTeillastSchema.SPALTE_BHKW_ETA_TH50),
                Zahl(zeile, ErzeugerTeillastSchema.SPALTE_BHKW_ANFAHRVERLUST),
                laufzeit.HasValue ? (int?)Convert.ToInt32(laufzeit.Value) : null);
        }

        /// <summary>Liest die vier Felder in eine Projektkopie.</summary>
        public static void BhkwAusZeile(BHKWModel ziel, DataRow zeile)
        {
            if (ziel == null || zeile == null) return;
            Setzen(ziel, BhkwAusZeile(zeile));
        }

        /// <summary>Liest die vier Felder in einen Katalogsatz.</summary>
        public static void BhkwAusZeile(BHKWStammModel ziel, DataRow zeile)
        {
            if (ziel == null || zeile == null) return;
            Setzen(ziel, BhkwAusZeile(zeile));
        }

        /// <summary>Die vier Felder einer Projektkopie.</summary>
        public static BhkwFelder Bhkw(BHKWModel m)
            => m == null ? default
                         : new BhkwFelder(m.m_Wirkungsgrad_el_Teillast50, m.m_Wirkungsgrad_th_Teillast50,
                                          m.m_Anfahrverlust_kWh, m.m_Mindestlaufzeit_min);

        /// <summary>Die vier Felder eines Katalogsatzes.</summary>
        public static BhkwFelder Bhkw(BHKWStammModel m)
            => m == null ? default
                         : new BhkwFelder(m.m_Wirkungsgrad_el_Teillast50, m.m_Wirkungsgrad_th_Teillast50,
                                          m.m_Anfahrverlust_kWh, m.m_Mindestlaufzeit_min);

        /// <summary>Setzt die vier Felder an einer Projektkopie.</summary>
        public static void Setzen(BHKWModel ziel, BhkwFelder f)
        {
            if (ziel == null) return;
            ziel.m_Wirkungsgrad_el_Teillast50 = f.EtaEl50;
            ziel.m_Wirkungsgrad_th_Teillast50 = f.EtaTh50;
            ziel.m_Anfahrverlust_kWh = f.AnfahrverlustKwh;
            ziel.m_Mindestlaufzeit_min = f.MindestlaufzeitMin;
        }

        /// <summary>Setzt die vier Felder an einem Katalogsatz.</summary>
        public static void Setzen(BHKWStammModel ziel, BhkwFelder f)
        {
            if (ziel == null) return;
            ziel.m_Wirkungsgrad_el_Teillast50 = f.EtaEl50;
            ziel.m_Wirkungsgrad_th_Teillast50 = f.EtaTh50;
            ziel.m_Anfahrverlust_kWh = f.AnfahrverlustKwh;
            ziel.m_Mindestlaufzeit_min = f.MindestlaufzeitMin;
        }

        /// <summary>Überträgt die vier Felder von einem Modell auf ein anderes.</summary>
        public static void BhkwUebertragen(BHKWModel von, BHKWModel nach) => Setzen(nach, Bhkw(von));

        /// <summary>Überträgt die vier Felder von einem Katalogsatz auf einen anderen.</summary>
        public static void BhkwUebertragen(BHKWStammModel von, BHKWStammModel nach) => Setzen(nach, Bhkw(von));

        /// <summary>
        /// Schreibt die vier Felder an den Satz, dessen <paramref name="schluesselspalte"/>
        /// (<c>ID</c> oder <c>Bezeichner</c>) den Wert <paramref name="schluessel"/> trägt, in
        /// <paramref name="tabelle"/> (<see cref="ErzeugerTeillastSchema.TAB_BHKW"/> oder
        /// <see cref="ErzeugerTeillastSchema.TAB_BHKW_STAMM"/>); ohne die Spalten nichts.
        /// </summary>
        public static void BhkwSchreiben(string tabelle, string schluesselspalte, object schluessel, BhkwFelder f,
                                         DbVorgang vorgang = null)
        {
            if (schluessel == null || !Gueltig(tabelle) ||
                (schluesselspalte != "ID" && schluesselspalte != "Bezeichner") ||
                !ErzeugerTeillastSchema.BhkwSpaltenVorhanden(tabelle)) return;
            string sql = "UPDATE \"" + tabelle + "\" SET \"" + ErzeugerTeillastSchema.SPALTE_BHKW_ETA_EL50 + "\" = ?, \"" +
                         ErzeugerTeillastSchema.SPALTE_BHKW_ETA_TH50 + "\" = ?, \"" +
                         ErzeugerTeillastSchema.SPALTE_BHKW_ANFAHRVERLUST + "\" = ?, \"" +
                         ErzeugerTeillastSchema.SPALTE_BHKW_MINDESTLAUFZEIT + "\" = ? WHERE \"" + schluesselspalte + "\" = ?";
            DbParam[] ps =
            {
                new DbParam("@el50", Wert(f.EtaEl50)),
                new DbParam("@th50", Wert(f.EtaTh50)),
                new DbParam("@anf", Wert(f.AnfahrverlustKwh)),
                new DbParam("@lauf", Wert(f.MindestlaufzeitMin)),
                new DbParam("@key", schluessel)
            };
            if (vorgang != null) vorgang.Ausfuehren(sql, ps);
            else DataRepository.ExecuteNonQuery(sql, ps);
        }

        /// <summary>Schreibt die vier Felder an die Projektkopie <paramref name="id"/>.</summary>
        public static void BhkwSchreiben(string tabelle, int id, BHKWModel m, DbVorgang vorgang = null)
        {
            if (m == null || id <= 0) return;
            BhkwSchreiben(tabelle, "ID", id, Bhkw(m), vorgang);
        }

        /// <summary>Kopiert die vier Felder aus einer Katalogzeile an die Projektkopie <paramref name="neueId"/>.</summary>
        public static void BhkwKopieren(DataRow stamm, int neueId)
        {
            if (stamm == null || neueId <= 0) return;
            BhkwSchreiben(ErzeugerTeillastSchema.TAB_BHKW, "ID", neueId, BhkwAusZeile(stamm));
        }

        /// <summary>
        /// Die Plausibilität der vier Felder — <c>null</c>, wenn alles passt, sonst der erste Grund: die
        /// Wirkungsgrade bei 50 % Last als Faktor 0 … 1, Anfahrverlust 0 … 100 kWh, Mindestlaufzeit 0 … 60 min.
        /// </summary>
        public static string BhkwVerstoss(BhkwFelder f)
        {
            if (f.EtaEl50.HasValue)
            {
                string g = Bereich(Text("BHKWK_FELD_ETA_EL50", "elektrischer Wirkungsgrad bei 50 % Last"),
                                   f.EtaEl50.Value, 0, ErzeugerTeillastSchema.ETA50_MAX);
                if (g != null) return g;
            }
            if (f.EtaTh50.HasValue)
            {
                string g = Bereich(Text("BHKWK_FELD_ETA_TH50", "thermischer Wirkungsgrad bei 50 % Last"),
                                   f.EtaTh50.Value, 0, ErzeugerTeillastSchema.ETA50_MAX);
                if (g != null) return g;
            }
            if (f.AnfahrverlustKwh.HasValue)
            {
                string g = Bereich(Text("BHKWK_FELD_ANFAHRVERLUST", "Anfahrverlust je Start"),
                                   f.AnfahrverlustKwh.Value, 0, ErzeugerTeillastSchema.ANFAHRVERLUST_MAX_KWH);
                if (g != null) return g;
            }
            if (f.MindestlaufzeitMin.HasValue)
            {
                string g = Bereich(Text("BHKWK_FELD_MINDESTLAUFZEIT", "Mindestlaufzeit"),
                                   f.MindestlaufzeitMin.Value, 0, ErzeugerTeillastSchema.MINDESTLAUFZEIT_MAX_MIN);
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
