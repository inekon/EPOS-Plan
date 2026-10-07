using System;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die gepflegten Kennzahlen des Erdsondenfeldes einer Anlage (<see cref="ErdsondenfeldSchema"/>) — so, wie sie in
    /// der Anlagenzeile stehen. <c>null</c> heißt „Normvorgabe" (<see cref="Sondenfeldgeometrie.Norm"/>); der
    /// Bohrlochdurchmesser steht in mm wie in der Spalte.
    /// </summary>
    public sealed class ErdsondenfeldEingabe
    {
        /// <summary>Sondenabstand [m], &gt; 0; null = Vorgabe.</summary>
        public double? AbstandM { get; set; }

        /// <summary>Bohrlochdurchmesser [mm], &gt; 0; null = Vorgabe.</summary>
        public double? BohrlochdurchmesserMm { get; set; }

        /// <summary>Bohrlochwiderstand [m·K/W], &gt; 0; null = Vorgabe.</summary>
        public double? Bohrlochwiderstand { get; set; }

        /// <summary>Kopfüberdeckung [m], ≥ 0; null = Vorgabe.</summary>
        public double? KopfueberdeckungM { get; set; }

        /// <summary>Betrachtungsjahr, ≥ 1; null = Vorgabe.</summary>
        public int? Betrachtungsjahr { get; set; }

        /// <summary>Anordnung (<see cref="ErdsondenfeldSchema.ANORDNUNGEN"/>); null = Vorgabe.</summary>
        public Sondenanordnung? Anordnung { get; set; }

        /// <summary>
        /// Hält jeder gepflegte Wert die Prüfklausel seiner Spalte? Abstand, Durchmesser und Widerstand &gt; 0,
        /// Kopfüberdeckung ≥ 0, Betrachtungsjahr ≥ 1; jeder Wert endlich. Ohne Datenbank — auch für den Dialog.
        /// </summary>
        public bool Zulaessig()
        {
            return Positiv(AbstandM) && Positiv(BohrlochdurchmesserMm) && Positiv(Bohrlochwiderstand)
                   && (!KopfueberdeckungM.HasValue || (double.IsFinite(KopfueberdeckungM.Value) && KopfueberdeckungM.Value >= 0))
                   && (!Betrachtungsjahr.HasValue || Betrachtungsjahr.Value >= 1)
                   && (!Anordnung.HasValue || Enum.IsDefined(typeof(Sondenanordnung), Anordnung.Value));
        }

        private static bool Positiv(double? x) => !x.HasValue || (double.IsFinite(x.Value) && x.Value > 0);
    }

    /// <summary>
    /// <b>Lesen und Schreiben des Erdsondenfeldes je Anlage</b> (Konzept Simulationsablauf 23.3) — der Datenweg des
    /// Erdreichdialogs; die Oberfläche fasst keine Datenbank an. Gerechnet wird über
    /// <see cref="WaermequelleClass.SondenfeldgeometrieDerAnlage"/>, das dieselben Spalten liest.
    /// </summary>
    public static class ErdsondenfeldCtrl
    {
        /// <summary>Die Vorgabewerte, die eine leere Spalte meint (VDI 4640 Blatt 2, Tabelle B2).</summary>
        public static Sondenfeldgeometrie Vorgabe => Sondenfeldgeometrie.Norm;

        /// <summary>Der Vorgabewert des Bohrlochdurchmessers [mm] (aus dem Radius der Norm).</summary>
        public static double VorgabeBohrlochdurchmesserMm => Sondenfeldgeometrie.NormBohrlochdurchmesserMm;

        /// <summary>
        /// Die gepflegten Werte der Anlage; jede leere Spalte und eine Datenbank vor dem Schritt ergeben <c>null</c>.
        /// </summary>
        public static ErdsondenfeldEingabe Lesen(int idAnlage)
        {
            var e = new ErdsondenfeldEingabe();
            if (idAnlage <= 0 || !ErdsondenfeldSchema.Vollstaendig()) return e;
            e.AbstandM = Zahl(WaermequelleClass.WertLesenStill(idAnlage, ErdsondenfeldSchema.SPALTE_ABSTAND));
            e.BohrlochdurchmesserMm = Zahl(WaermequelleClass.WertLesenStill(idAnlage, ErdsondenfeldSchema.SPALTE_BOHRLOCHDURCHMESSER));
            e.Bohrlochwiderstand = Zahl(WaermequelleClass.WertLesenStill(idAnlage, ErdsondenfeldSchema.SPALTE_BOHRLOCHWIDERSTAND));
            e.KopfueberdeckungM = Zahl(WaermequelleClass.WertLesenStill(idAnlage, ErdsondenfeldSchema.SPALTE_KOPFUEBERDECKUNG));
            double? jahr = Zahl(WaermequelleClass.WertLesenStill(idAnlage, ErdsondenfeldSchema.SPALTE_BETRACHTUNGSJAHR));
            e.Betrachtungsjahr = jahr.HasValue ? (int)Math.Round(jahr.Value) : (int?)null;
            e.Anordnung = AnordnungAusText(WaermequelleClass.WertLesenStill(idAnlage, ErdsondenfeldSchema.SPALTE_ANORDNUNG) as string);
            return e;
        }

        /// <summary>
        /// Hält jeder gepflegte Wert die Prüfklausel seiner Spalte? Abstand, Durchmesser und Widerstand &gt; 0,
        /// Kopfüberdeckung ≥ 0, Betrachtungsjahr ≥ 1; jeder Wert endlich.
        /// </summary>
        public static bool Zulaessig(ErdsondenfeldEingabe e) => e == null || e.Zulaessig();

        /// <summary>
        /// Schreibt die sechs Werte der Anlage in EINER Anweisung; <c>null</c> schreibt NULL (= Vorgabe). Eine
        /// Datenbank vor dem Schritt und ein unzulässiger Wert (<see cref="Zulaessig"/>) schreiben nichts
        /// (Rückgabe <c>false</c>).
        /// </summary>
        public static bool Schreiben(int idAnlage, ErdsondenfeldEingabe e)
        {
            if (idAnlage <= 0 || e == null || !Zulaessig(e) || !ErdsondenfeldSchema.Vollstaendig()) return false;
            string sql = "UPDATE Tab_Energieanlagen SET " +
                         "\"" + ErdsondenfeldSchema.SPALTE_ABSTAND + "\" = ?, " +
                         "\"" + ErdsondenfeldSchema.SPALTE_BOHRLOCHDURCHMESSER + "\" = ?, " +
                         "\"" + ErdsondenfeldSchema.SPALTE_BOHRLOCHWIDERSTAND + "\" = ?, " +
                         "\"" + ErdsondenfeldSchema.SPALTE_KOPFUEBERDECKUNG + "\" = ?, " +
                         "\"" + ErdsondenfeldSchema.SPALTE_BETRACHTUNGSJAHR + "\" = ?, " +
                         "\"" + ErdsondenfeldSchema.SPALTE_ANORDNUNG + "\" = ? WHERE ID = ?";
            return DataRepository.ExecuteSQL(sql,
                Par("@abstand", DbParamTyp.Double, e.AbstandM),
                Par("@durchmesser", DbParamTyp.Double, e.BohrlochdurchmesserMm),
                Par("@widerstand", DbParamTyp.Double, e.Bohrlochwiderstand),
                Par("@kopf", DbParamTyp.Double, e.KopfueberdeckungM),
                Par("@jahr", DbParamTyp.Integer, e.Betrachtungsjahr),
                Par("@anordnung", DbParamTyp.VarWChar, e.Anordnung?.ToString()),
                new DbParam("@id", idAnlage));
        }

        /// <summary>Die Anordnung zu einem Spaltentext; leer oder unbekannt = <c>null</c> (Vorgabe).</summary>
        public static Sondenanordnung? AnordnungAusText(string text) => Sondenfeldgeometrie.AnordnungAusText(text);

        private static DbParam Par(string name, DbParamTyp typ, object wert)
            => new DbParam(name, typ) { Wert = wert ?? DBNull.Value };

        private static double? Zahl(object o)
        {
            if (o == null || o is DBNull) return null;
            try { return Convert.ToDouble(o, CultureInfo.InvariantCulture); }
            catch { return null; }
        }
    }
}
