#nullable enable

using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Gerätegrenzen von Wärmepumpe und BHKW an der Datenbankgrenze</b> (Schemaschritt
    /// <see cref="UebergabegrenzeSchema"/>; Umsetzungskonzept Übergabegrenze 4.3, 6.4) — Lesen, Schreiben, Kopieren und
    /// Prüfen an EINER Stelle für Katalog (<c>Tab_WP_STAMM</c>, <c>Tab_BHKW_STAMM</c>) und Projektkopie (<c>Tab_WP</c>,
    /// <c>Tab_BHKW</c>), nach dem Muster von <see cref="ErzeugerTeillastWerte"/>.
    /// </summary>
    /// <remarks>
    /// <para><b>Leer ist nicht 0.</b> Alle Felder sind nullbar; <c>null</c> heißt „nicht gepflegt", der Lauf nimmt dann
    /// die Vorgabe der Kältemittelklasse (<see cref="Geraetegrenzen"/>) bzw. beim BHKW keine Grenze.</para>
    /// <para><b>Ein eigener Schreibschritt</b> nach dem Schreiben des Satzes, nur wenn die Spalten stehen.</para>
    /// <para><b>Bereiche.</b> Die Spalten tragen <c>CHECK</c>-Bereiche (<see cref="UebergabegrenzeSchema.WP_SPALTEN"/>);
    /// das Stammblatt zeigt einen Wert außerhalb als Hinweis, der Speicherweg nennt ihn benannt, bevor die Datenbank
    /// ihn abweist.</para>
    /// </remarks>
    public static class GeraetegrenzWerte
    {
        /// <summary>Ein Bereich einer Gerätespalte [von; bis] — dieselben Zahlen wie der <c>CHECK</c> des Schemas.</summary>
        public readonly record struct Bereich(string Spalte, double Von, double Bis)
        {
            /// <summary>Liegt <paramref name="wert"/> im Bereich? Leer liegt immer darin.</summary>
            public bool Enthaelt(double? wert) => !wert.HasValue || (wert.Value >= Von && wert.Value <= Bis);
        }

        /// <summary>Die sieben Zahlspalten der Wärmepumpe mit ihrem Bereich, in der Folge des Schemas.</summary>
        public static readonly IReadOnlyList<Bereich> WP_BEREICHE = new[]
        {
            new Bereich(UebergabegrenzeSchema.SPALTE_SPREIZUNG_AUSLEGUNG, 3, 8),
            new Bereich(UebergabegrenzeSchema.SPALTE_SPREIZUNG_MAX, 5, 40),
            new Bereich(UebergabegrenzeSchema.SPALTE_SPREIZUNG_MIN, 0, 8),
            new Bereich(UebergabegrenzeSchema.SPALTE_MINDESTVOLUMENSTROM, 20, 100),
            new Bereich(UebergabegrenzeSchema.SPALTE_RUECKLAUF_MAX, 20, 70),
            new Bereich(UebergabegrenzeSchema.SPALTE_RUECKLAUF_BEZUG, 20, 40),
            new Bereich(UebergabegrenzeSchema.SPALTE_RUECKLAUF_ABWERTUNG, 0, 5),
        };

        /// <summary>Der Bereich der BHKW-Abschaltgrenze <c>Ruecklauf_Max</c> [°C].</summary>
        public static readonly Bereich BHKW_BEREICH = new Bereich(UebergabegrenzeSchema.SPALTE_RUECKLAUF_MAX, 40, 90);

        /// <summary>Die Vorgabe der BHKW-Abschaltgrenze bei Neuanlage [°C] (Fachkonzept U‑3).</summary>
        public const double BHKW_RUECKLAUF_MAX_VORGABE_C = Bivalenzvorgaben.RUECKLAUF_GRENZE_BHKW_C;

        /// <summary>Der Kältemittelcode, bei dem Bezugsrücklauf und Abwertung wirken (R744).</summary>
        public const string KAELTEMITTEL_R744 = Bivalenzvorgaben.KM_R744;

        // =============================================================================
        //  Wärmepumpe
        // =============================================================================

        /// <summary>Stehen alle acht Spalten in <paramref name="tabelle"/>?</summary>
        internal static bool WpSpaltenVorhanden(string tabelle)
        {
            if (tabelle != UebergabegrenzeSchema.TAB_WP && tabelle != UebergabegrenzeSchema.TAB_WP_STAMM) return false;
            foreach ((string spalte, string _) in UebergabegrenzeSchema.WP_SPALTEN)
                if (!DataRepository.SpalteVorhanden(tabelle, spalte)) return false;
            return true;
        }

        /// <summary>Die acht Spalten aus einer Zeile von <c>Tab_WP</c> oder <c>Tab_WP_STAMM</c>; fehlende Spalten leer.</summary>
        internal static Geraetespalten WpAusZeile(DataRow? zeile)
        {
            if (zeile == null) return Geraetespalten.Leer;
            string? km = Text(zeile, UebergabegrenzeSchema.SPALTE_KAELTEMITTEL);
            return new Geraetespalten(
                Zahl(zeile, UebergabegrenzeSchema.SPALTE_SPREIZUNG_AUSLEGUNG),
                Zahl(zeile, UebergabegrenzeSchema.SPALTE_SPREIZUNG_MAX),
                Zahl(zeile, UebergabegrenzeSchema.SPALTE_SPREIZUNG_MIN),
                Zahl(zeile, UebergabegrenzeSchema.SPALTE_MINDESTVOLUMENSTROM),
                Zahl(zeile, UebergabegrenzeSchema.SPALTE_RUECKLAUF_MAX),
                Zahl(zeile, UebergabegrenzeSchema.SPALTE_RUECKLAUF_BEZUG),
                Zahl(zeile, UebergabegrenzeSchema.SPALTE_RUECKLAUF_ABWERTUNG),
                km);
        }

        /// <summary>Die acht Spalten des Satzes <paramref name="id"/> von <paramref name="tabelle"/>; <c>null</c> ohne Satz oder Spalten.</summary>
        internal static Geraetespalten? WpLesen(string tabelle, int id)
        {
            if (id <= 0 || !WpSpaltenVorhanden(tabelle)) return null;
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM \"" + tabelle + "\" WHERE ID = ?", new DbParam("@id", id));
            return dt == null || dt.Rows.Count == 0 ? null : WpAusZeile(dt.Rows[0]);
        }

        /// <summary>
        /// Schreibt die acht Spalten an den Satz <paramref name="id"/> von <paramref name="tabelle"/>; <c>null</c> = alle leer.
        /// Ohne die Spalten nichts. Mit <paramref name="vorgang"/> in dessen Transaktion.
        /// </summary>
        internal static void WpSchreiben(string tabelle, int id, Geraetespalten? s, DbVorgang? vorgang = null)
        {
            if (id <= 0 || !WpSpaltenVorhanden(tabelle)) return;
            Geraetespalten w = s ?? Geraetespalten.Leer;
            string sql = "UPDATE \"" + tabelle + "\" SET \"" + UebergabegrenzeSchema.SPALTE_SPREIZUNG_AUSLEGUNG + "\" = ?, \"" +
                         UebergabegrenzeSchema.SPALTE_SPREIZUNG_MAX + "\" = ?, \"" + UebergabegrenzeSchema.SPALTE_SPREIZUNG_MIN +
                         "\" = ?, \"" + UebergabegrenzeSchema.SPALTE_MINDESTVOLUMENSTROM + "\" = ?, \"" +
                         UebergabegrenzeSchema.SPALTE_RUECKLAUF_MAX + "\" = ?, \"" + UebergabegrenzeSchema.SPALTE_RUECKLAUF_BEZUG +
                         "\" = ?, \"" + UebergabegrenzeSchema.SPALTE_RUECKLAUF_ABWERTUNG + "\" = ?, \"" +
                         UebergabegrenzeSchema.SPALTE_KAELTEMITTEL + "\" = ? WHERE ID = ?";
            DbParam[] ps =
            {
                new DbParam("@sa", ErzeugerTeillastWerte.Wert(w.SpreizungAuslegungK)),
                new DbParam("@smax", ErzeugerTeillastWerte.Wert(w.SpreizungMaxK)),
                new DbParam("@smin", ErzeugerTeillastWerte.Wert(w.SpreizungMinK)),
                new DbParam("@mv", ErzeugerTeillastWerte.Wert(w.MindestvolumenstromProzent)),
                new DbParam("@rmax", ErzeugerTeillastWerte.Wert(w.RuecklaufMaxC)),
                new DbParam("@rbez", ErzeugerTeillastWerte.Wert(w.RuecklaufBezugC)),
                new DbParam("@rabw", ErzeugerTeillastWerte.Wert(w.RuecklaufAbwertungProzentJeK)),
                ProjektPuffer.Par("@km", DbParamTyp.VarWChar,
                    string.IsNullOrWhiteSpace(w.Kaeltemittel) ? null : w.Kaeltemittel!.Trim()),
                new DbParam("@id", id)
            };
            if (vorgang != null) vorgang.Ausfuehren(sql, ps);
            else DataRepository.ExecuteNonQuery(sql, ps);
        }

        /// <summary>Kopiert die acht Spalten aus einer gelesenen Zeile (Katalog oder Projekt) an <paramref name="nachId"/>.</summary>
        internal static void WpKopieren(DataRow? von, string nachTabelle, int nachId, DbVorgang? vorgang = null)
        {
            if (von == null || nachId <= 0) return;
            if (!von.Table.Columns.Contains(UebergabegrenzeSchema.SPALTE_KAELTEMITTEL)) return;
            WpSchreiben(nachTabelle, nachId, WpAusZeile(von), vorgang);
        }

        /// <summary>
        /// Die Bereiche der sieben Zahlspalten — <c>null</c>, wenn alles passt, sonst der erste Grund mit Feldname und
        /// Bereich (derselbe Wortlaut wie die Teillastfelder).
        /// </summary>
        internal static string? WpVerstoss(Geraetespalten? s)
        {
            if (s == null) return null;
            double?[] werte =
            {
                s.SpreizungAuslegungK, s.SpreizungMaxK, s.SpreizungMinK, s.MindestvolumenstromProzent,
                s.RuecklaufMaxC, s.RuecklaufBezugC, s.RuecklaufAbwertungProzentJeK,
            };
            for (int i = 0; i < WP_BEREICHE.Count; i++)
                if (!WP_BEREICHE[i].Enthaelt(werte[i]))
                    return BereichText(WpFeldname(WP_BEREICHE[i].Spalte), WP_BEREICHE[i]);
            return null;
        }

        /// <summary>Der Feldname einer Gerätespalte aus <c>MyResource</c> (die Beschriftungen des Stammblatts).</summary>
        public static string WpFeldname(string spalte) => spalte switch
        {
            UebergabegrenzeSchema.SPALTE_SPREIZUNG_AUSLEGUNG => Text("WPS_LBL_SPREIZUNG_AUSLEGUNG", "Auslegungsspreizung"),
            UebergabegrenzeSchema.SPALTE_SPREIZUNG_MAX => Text("WPS_LBL_SPREIZUNG_MAX", "Größte Spreizung"),
            UebergabegrenzeSchema.SPALTE_SPREIZUNG_MIN => Text("WPS_LBL_SPREIZUNG_MIN", "Kleinste Spreizung"),
            UebergabegrenzeSchema.SPALTE_MINDESTVOLUMENSTROM => Text("WPS_LBL_MINDESTVOLUMENSTROM", "Mindestvolumenstrom"),
            UebergabegrenzeSchema.SPALTE_RUECKLAUF_MAX => Text("WPS_LBL_RUECKLAUF_MAX", "Größter Rücklauf"),
            UebergabegrenzeSchema.SPALTE_RUECKLAUF_BEZUG => Text("WPS_LBL_RUECKLAUF_BEZUG", "Bezugsrücklauf"),
            UebergabegrenzeSchema.SPALTE_RUECKLAUF_ABWERTUNG => Text("WPS_LBL_RUECKLAUF_ABWERTUNG", "Abwertung je Kelvin Rücklauf"),
            _ => spalte,
        };

        // =============================================================================
        //  BHKW
        // =============================================================================

        /// <summary>Die Abschaltgrenze aus einer Zeile von <c>Tab_BHKW</c> oder <c>Tab_BHKW_STAMM</c>; ohne Spalte leer.</summary>
        public static double? BhkwAusZeile(DataRow? zeile)
            => zeile == null ? null : Zahl(zeile, UebergabegrenzeSchema.SPALTE_RUECKLAUF_MAX);

        /// <summary>
        /// Schreibt die Abschaltgrenze an den Satz (<paramref name="schluesselspalte"/> <c>ID</c> oder <c>Bezeichner</c>);
        /// ohne die Spalte nichts.
        /// </summary>
        public static void BhkwSchreiben(string tabelle, string schluesselspalte, object? schluessel, double? ruecklaufMaxC,
                                         DbVorgang? vorgang = null)
        {
            if (schluessel == null ||
                (tabelle != UebergabegrenzeSchema.TAB_BHKW && tabelle != UebergabegrenzeSchema.TAB_BHKW_STAMM) ||
                (schluesselspalte != "ID" && schluesselspalte != "Bezeichner") ||
                !DataRepository.SpalteVorhanden(tabelle, UebergabegrenzeSchema.SPALTE_RUECKLAUF_MAX)) return;
            string sql = "UPDATE \"" + tabelle + "\" SET \"" + UebergabegrenzeSchema.SPALTE_RUECKLAUF_MAX + "\" = ? WHERE \"" +
                         schluesselspalte + "\" = ?";
            DbParam[] ps = { new DbParam("@rmax", ErzeugerTeillastWerte.Wert(ruecklaufMaxC)), new DbParam("@key", schluessel) };
            if (vorgang != null) vorgang.Ausfuehren(sql, ps);
            else DataRepository.ExecuteNonQuery(sql, ps);
        }

        /// <summary>Kopiert die Abschaltgrenze aus einer Katalogzeile an die Projektkopie <paramref name="neueId"/>.</summary>
        public static void BhkwKopieren(DataRow? stamm, int neueId)
        {
            if (stamm == null || neueId <= 0 || !stamm.Table.Columns.Contains(UebergabegrenzeSchema.SPALTE_RUECKLAUF_MAX)) return;
            BhkwSchreiben(UebergabegrenzeSchema.TAB_BHKW, "ID", neueId, BhkwAusZeile(stamm));
        }

        /// <summary>Der Bereich der Abschaltgrenze — <c>null</c>, wenn er passt, sonst der Grund.</summary>
        public static string? BhkwVerstoss(double? ruecklaufMaxC)
            => BHKW_BEREICH.Enthaelt(ruecklaufMaxC) ? null
               : BereichText(Text("BHKWK_LBL_RUECKLAUF_MAX", "Höchster Rücklauf (Abschaltgrenze)"), BHKW_BEREICH);

        /// <summary>
        /// Prüfregel des BHKW-Stammblatts (U‑3, <see cref="Ruecklaufgrenze.BhkwAuslegungHinweis"/>): Hinweis, wenn der
        /// Auslegungsrücklauf die Abschaltgrenze erreicht oder übersteigt.
        /// </summary>
        public static bool BhkwAuslegungHinweis(int? auslegungRuecklaufC, double? ruecklaufMaxC)
            => auslegungRuecklaufC.HasValue && Ruecklaufgrenze.BhkwAuslegungHinweis(auslegungRuecklaufC.Value, ruecklaufMaxC);

        // =============================================================================
        //  Hilfen
        // =============================================================================

        private static string BereichText(string feld, Bereich b)
            => string.Format(Text("KBROW_MSG_WERT_BEREICH", "„{0}“ muss zwischen {1} und {2} liegen."),
                             feld, b.Von.ToString(CultureInfo.CurrentCulture), b.Bis.ToString(CultureInfo.CurrentCulture));

        private static double? Zahl(DataRow zeile, string spalte)
        {
            if (!zeile.Table.Columns.Contains(spalte)) return null;
            object v = zeile[spalte];
            if (v == null || v == DBNull.Value) return null;
            double d = Convert.ToDouble(v, CultureInfo.InvariantCulture);
            return double.IsNaN(d) ? (double?)null : d;
        }

        private static string? Text(DataRow zeile, string spalte)
        {
            if (!zeile.Table.Columns.Contains(spalte)) return null;
            object v = zeile[spalte];
            string? t = v == null || v == DBNull.Value ? null : Convert.ToString(v, CultureInfo.InvariantCulture);
            return string.IsNullOrWhiteSpace(t) ? null : t!.Trim();
        }

        private static string Text(string schluessel, string rueckfall)
        {
            string? t = null;
            try { t = MyResource.Resource.ResourceManager.GetString(schluessel); }
            catch { }
            return string.IsNullOrEmpty(t) ? rueckfall : t!;
        }
    }
}
