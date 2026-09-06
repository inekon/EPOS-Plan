using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// EIN Gerät der CEC-Wechselrichterliste (NREL/SAM) — die Rohwerte einer Zeile
    /// und ihre Übersetzung in einen Katalogsatz (Konzept Wechselrichter 5.1,
    /// Anwenderentscheid <b>W6‑E‑2</b> vom 06.09.2026, Stufe S1.5).
    ///
    /// <para>Zwilling zu <see cref="PVModule"/> auf der Modulseite: Der Dienst liest
    /// die Datei, dieser Satz trägt ihre Werte, und
    /// <see cref="NachModell"/> macht daraus einen
    /// <see cref="WechselrichterModel"/>.</para>
    ///
    /// <para><b>Der Herstellername steht im Gerätenamen</b> — die Liste führt keine
    /// eigene Spalte dafür (anders als die Modulliste mit <c>Manufacturer</c>). Er ist
    /// der Text vor dem ersten Doppelpunkt, genau wie beim Modulimport.</para>
    /// </summary>
    public sealed class CecWechselrichter
    {
        /// <summary>Der vollständige Gerätename aus der Spalte <c>Name</c>.</summary>
        public string Name = "";

        /// <summary>Wechselstrom-Nennspannung [V] (Spalte <c>Vac</c>) — nur Ausweis.</summary>
        public string Vac = "";

        /// <summary>Einschaltschwelle [W] (Spalte <c>Pso</c>).</summary>
        public double Pso;

        /// <summary>AC-Nennwirkleistung [W] (Spalte <c>Paco</c>).</summary>
        public double Paco;

        /// <summary>DC-Leistung bei AC-Nennleistung [W] (Spalte <c>Pdco</c>).</summary>
        public double Pdco;

        /// <summary>Bezugsspannung [V] (Spalte <c>Vdco</c>).</summary>
        public double Vdco;

        /// <summary>Sandia-Koeffizient C0 [1/W].</summary>
        public double C0;

        /// <summary>Sandia-Koeffizient C1 [1/V].</summary>
        public double C1;

        /// <summary>Sandia-Koeffizient C2 [1/V].</summary>
        public double C2;

        /// <summary>Sandia-Koeffizient C3 [1/V].</summary>
        public double C3;

        /// <summary>Nachtverbrauch [W] (Spalte <c>Pnt</c>).</summary>
        public double Pnt;

        /// <summary>Maximale DC-Eingangsspannung [V] (Spalte <c>Vdcmax</c>).</summary>
        public double Vdcmax;

        /// <summary>Maximaler DC-Strom je MPPT [A] (Spalte <c>Idcmax</c>).</summary>
        public double Idcmax;

        /// <summary>Untere Grenze des MPP-Fensters [V] (Spalte <c>Mppt_low</c>).</summary>
        public double MpptLow;

        /// <summary>Obere Grenze des MPP-Fensters [V] (Spalte <c>Mppt_high</c>).</summary>
        public double MpptHigh;

        /// <summary>Listungsdatum der CEC (Spalte <c>CEC_Date</c>); leer, wenn nicht gepflegt.</summary>
        public string CecDatum = "";

        /// <summary>Der Hersteller — der Text vor dem ersten Doppelpunkt des Namens.</summary>
        public string Hersteller => HerstellerAus(Name);

        /// <summary>
        /// Der Hersteller aus einem CEC-Gerätenamen; leer, wenn kein Doppelpunkt
        /// darin steht.
        /// </summary>
        public static string HerstellerAus(string name)
        {
            if (string.IsNullOrEmpty(name)) return "";
            int i = name.IndexOf(':');
            return i > 0 ? name.Substring(0, i).Trim() : "";
        }

        /// <summary>
        /// Die sechs Stützstellen dieses Geräts — gerechnet aus den Sandia-Werten bei
        /// <c>U_dc = U_dco</c> (<see cref="WechselrichterKennlinie.AusSandia"/>).
        /// </summary>
        public double?[] Stuetzstellen()
        {
            return WechselrichterKennlinie.AusSandia(Paco, Pdco, Pso, C0);
        }

        /// <summary>
        /// Der Katalogsatz zu diesem Gerät — die Feldzuordnung aus Konzept 5.1.
        /// </summary>
        /// <param name="ziel">
        /// Optional ein vorhandenes Modell (bzw. ein Stamm-Controller), das gefüllt
        /// werden soll; <c>null</c> legt ein neues an. Dasselbe Muster wie
        /// <c>UnifiedModule.NachModell</c>.
        /// </param>
        /// <remarks>
        /// <para><b>Was die Liste NICHT führt und deshalb NULL bleibt</b> (Konzept 5.1,
        /// „Zwei Punkte zur Ehrlichkeit"): <c>Anzahl_Mppt</c> und
        /// <c>Straenge_Je_Mppt</c> — die Prüfungen P4/P5 rechnen dann auf EINEM MPPT,
        /// dem konservativen Fall, und melden es. Ebenso <c>S_AC_Max</c>: <c>Paco</c>
        /// ist Wirkleistung, nicht Scheinleistung. Dazu <c>P_DC_Max</c>,
        /// <c>U_Start</c> und <c>Kosten</c>.</para>
        ///
        /// <para><b><c>Eta_Max</c> ist das Maximum der sechs Stützstellen</b>, nicht der
        /// wahre Scheitel: Die CEC-Liste führt keinen Maximalwirkungsgrad, und der
        /// Scheitel der Modellparabel liegt zwischen zwei Stützstellen — der Ausweis ist
        /// damit eine untere Schranke. Das ist ehrlicher als eine gerechnete Zahl, die
        /// in keinem Datenblatt steht.</para>
        /// </remarks>
        public WechselrichterModel NachModell(WechselrichterModel ziel = null)
        {
            WechselrichterModel m = ziel ?? new WechselrichterModel();

            m.m_szName = Name ?? "";
            m.m_szFirma = Hersteller;
            m.m_szBeschreibung = Beschreibung();

            m.m_P_AC_Nenn = Paco > 0 ? (double?)(Paco / 1000.0) : null;   // W -> kW
            m.m_S_AC_Max = null;
            m.m_P_DC_Max = null;
            m.m_U_Mpp_Min = MpptLow > 0 ? (double?)MpptLow : null;
            m.m_U_Mpp_Max = MpptHigh > 0 ? (double?)MpptHigh : null;
            m.m_U_Dc_Max = Vdcmax > 0 ? (double?)Vdcmax : null;
            m.m_U_Start = null;
            m.m_I_Dc_Max = Idcmax > 0 ? (double?)Idcmax : null;
            m.m_Anzahl_Mppt = null;
            m.m_Straenge_Je_Mppt = null;

            double?[] etas = Stuetzstellen();
            m.m_Eta05 = etas[0];
            m.m_Eta10 = etas[1];
            m.m_Eta20 = etas[2];
            m.m_Eta30 = etas[3];
            m.m_Eta50 = etas[4];
            m.m_Eta100 = etas[5];
            m.m_Eta_Euro = WechselrichterKennlinie.EuroWirkungsgrad(etas);
            m.m_Eta_Max = Hoechster(etas);

            m.m_P_Standby = Pso != 0.0 ? (double?)Pso : null;
            m.m_P_Nacht = Pnt != 0.0 ? (double?)Pnt : null;
            m.m_Kosten = null;

            m.m_Sandia_Pdco = Pdco != 0.0 ? (double?)Pdco : null;
            m.m_Sandia_Vdco = Vdco != 0.0 ? (double?)Vdco : null;
            m.m_Sandia_Pso = Pso != 0.0 ? (double?)Pso : null;
            m.m_Sandia_C0 = C0 != 0.0 ? (double?)C0 : null;
            m.m_Sandia_C1 = C1 != 0.0 ? (double?)C1 : null;
            m.m_Sandia_C2 = C2 != 0.0 ? (double?)C2 : null;
            m.m_Sandia_C3 = C3 != 0.0 ? (double?)C3 : null;

            m.m_Herkunft = DbWerte.WR_HERKUNFT_CEC;
            return m;
        }

        /// <summary>
        /// Der Beschreibungstext des Katalogsatzes — Herkunft, Listungsdatum und
        /// AC-Nennspannung.
        /// </summary>
        /// <remarks>
        /// Konzept 5.1 nennt dafür eine Spalte <c>CEC_Type</c>. <b>Die gibt es in der
        /// Liste vom 06.09.2026 nicht</b>; ihre Kopfzeile führt stattdessen
        /// <c>CEC_Date</c> und <c>CEC_hybrid</c>. Genommen wird deshalb, was da ist —
        /// und zwar ohne Anzeigetext: Der Kern kennt keine Sprache.
        /// </remarks>
        private string Beschreibung()
        {
            var teile = new List<string> { DbWerte.WR_HERKUNFT_CEC };
            if (!string.IsNullOrWhiteSpace(CecDatum)) teile.Add(CecDatum.Trim());
            if (!string.IsNullOrWhiteSpace(Vac)) teile.Add("Vac " + Vac.Trim() + " V");
            return string.Join(" - ", teile);
        }

        /// <summary>Der größte vorhandene Wert der sechs Stützstellen; <c>null</c>, wenn keine da ist.</summary>
        private static double? Hoechster(double?[] etas)
        {
            double? max = null;
            foreach (double? e in etas)
                if (e.HasValue && (!max.HasValue || e.Value > max.Value)) max = e;
            return max;
        }

        /// <summary>
        /// Die Werte, die die DUBLETTENPRÜFUNG vergleicht — Spaltenname der
        /// Stammtabelle auf Wert, genau die <c>ImportSpalten</c> der
        /// <see cref="KatalogRegistry"/>-Definition „WECHSELRICHTER".
        /// </summary>
        /// <remarks>
        /// Bauart wörtlich <c>UnifiedModule.Vergleichswerte</c>. Die
        /// <c>Sandia_*</c>-Spalten stehen bewusst NICHT darin: Zwei Katalogsätze, die
        /// sich nur in <c>C3</c> unterscheiden, rechnen in EPOS-Plan identisch
        /// (Konzept 3.3.2) — sie als verschieden zu melden wäre falscher Alarm
        /// (Konzept 5.4).
        /// </remarks>
        public IDictionary<string, object> Vergleichswerte(string name)
        {
            WechselrichterModel m = NachModell();
            var werte = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
            {
                ["Bezeichner"] = name ?? m.m_szName,
                ["Firma"] = m.m_szFirma
            };

            Nimm(werte, WechselrichterSchema.SPALTE_P_AC_NENN, m.m_P_AC_Nenn);
            Nimm(werte, WechselrichterSchema.SPALTE_S_AC_MAX, m.m_S_AC_Max);
            Nimm(werte, WechselrichterSchema.SPALTE_P_DC_MAX, m.m_P_DC_Max);
            Nimm(werte, WechselrichterSchema.SPALTE_U_MPP_MIN, m.m_U_Mpp_Min);
            Nimm(werte, WechselrichterSchema.SPALTE_U_MPP_MAX, m.m_U_Mpp_Max);
            Nimm(werte, WechselrichterSchema.SPALTE_U_DC_MAX, m.m_U_Dc_Max);
            Nimm(werte, WechselrichterSchema.SPALTE_U_START, m.m_U_Start);
            Nimm(werte, WechselrichterSchema.SPALTE_I_DC_MAX, m.m_I_Dc_Max);
            Nimm(werte, WechselrichterSchema.SPALTE_ANZAHL_MPPT, m.m_Anzahl_Mppt);
            Nimm(werte, WechselrichterSchema.SPALTE_STRAENGE_JE_MPPT, m.m_Straenge_Je_Mppt);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA05, m.m_Eta05);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA10, m.m_Eta10);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA20, m.m_Eta20);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA30, m.m_Eta30);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA50, m.m_Eta50);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA100, m.m_Eta100);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA_EURO, m.m_Eta_Euro);
            Nimm(werte, WechselrichterSchema.SPALTE_ETA_MAX, m.m_Eta_Max);
            Nimm(werte, WechselrichterSchema.SPALTE_P_STANDBY, m.m_P_Standby);
            Nimm(werte, WechselrichterSchema.SPALTE_P_NACHT, m.m_P_Nacht);
            werte[WechselrichterSchema.SPALTE_HERKUNFT] = m.m_Herkunft;

            return werte;
        }

        private static void Nimm(IDictionary<string, object> werte, string spalte, double? wert)
        {
            werte[spalte] = wert.HasValue ? (object)wert.Value : null;
        }

        private static void Nimm(IDictionary<string, object> werte, string spalte, int? wert)
        {
            werte[spalte] = wert.HasValue ? (object)wert.Value : null;
        }

        /// <summary>Der Gerätename — für Listen und Meldungen.</summary>
        public override string ToString()
        {
            return Name ?? "";
        }

        /// <summary>Eine Zahl der Anzeige, kulturabhängig formatiert.</summary>
        internal static string Anzeige(double? wert, int stellen)
        {
            return wert.HasValue
                ? wert.Value.ToString("F" + stellen.ToString(CultureInfo.InvariantCulture),
                                      CultureInfo.CurrentCulture)
                : ParameterVerwendung.LEER;
        }
    }
}
