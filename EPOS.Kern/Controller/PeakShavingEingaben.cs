using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die 14 Zahlen und zwei Schalter der Lastspitzenkappung — samt ihren vier
    /// Pruefregeln und der Umrechnung in die Engine-Parameter (iU9-W12.6).
    ///
    /// <para><b>Woher sie kommen.</b> <c>Form_PeakShaving.ParameterLesen</c>
    /// (:419-480) las die Felder, prüfte vier Regeln und baute daraus
    /// <see cref="SpeicherParameter"/> und <see cref="PeakShavingParameter"/>. Das
    /// Lesen der Felder ist Sache der Oberflaeche; die Regeln und die Umrechnung
    /// sind Fachaussagen und gehoeren in den Kern — sonst haette iOS sie ein
    /// zweites Mal.</para>
    ///
    /// <para><b>Vier Einheitenumrechnungen, woertlich.</b> Das SoC-Band steht in
    /// PROZENT an der Maske und in kWh in der Engine; der Kapitalzins steht in
    /// Prozent und geht als Faktor hinein. <c>DtH = 0,25</c> ist fest — die Maske
    /// rechnet im Viertelstundenraster —, und <c>DegradationProA = 0</c> mit der
    /// Begruendung des Vorlaeufers (:465-469): Degradation ist an dieser Maske
    /// bewusst kein Feld, weil ihr Einfluss nur ueber einen Lauf je Nutzungsjahr
    /// sauber abzubilden waere und ein still mitgefuehrter Geraetewert das Ergebnis
    /// unsichtbar veraendern wuerde.</para>
    /// </summary>
    public sealed class PeakShavingEingaben
    {
        /// <summary>Das feste Zeitraster der Maske: Viertelstunde.</summary>
        public const double DtHStunden = 0.25;

        /// <summary>Lade- und Entladeleistung P [kW].</summary>
        public double PKw;

        /// <summary>Nutzbare Nennkapazitaet C_nom [kWh].</summary>
        public double KapazitaetKwh;

        /// <summary>Untere Bandgrenze [%].</summary>
        public double SoCMinProzent;

        /// <summary>Obere Bandgrenze [%].</summary>
        public double SoCMaxProzent;

        /// <summary>Start-Ladezustand [%].</summary>
        public double StartSoCProzent;

        /// <summary>Round-Trip-Wirkungsgrad eta_RT [-].</summary>
        public double WirkungsgradRt;

        /// <summary>Zielschwelle P_ziel [kW]; ohne Bedeutung, solange <see cref="Adaptiv"/> gilt.</summary>
        public double ZielschwelleKw;

        /// <summary>Leistungspreis L_P [EUR/(kW*a)].</summary>
        public double LeistungspreisEurProKwA;

        /// <summary>Mittlerer Bezugspreis [ct/kWh].</summary>
        public double BezugspreisMittelCtKwh;

        /// <summary>Kapazitaetsbezogene Investition c_cap [EUR/kWh].</summary>
        public double CCapEurProKwh;

        /// <summary>Leistungsbezogene Investition c_pow [EUR/kW].</summary>
        public double CPowEurProKw;

        /// <summary>Leistungsunabhaengiger Investitionsanteil I_fix [EUR].</summary>
        public double IFixEur;

        /// <summary>Kapitalzins [%].</summary>
        public double KapitalzinsProzent;

        /// <summary>Nutzungsdauer [a].</summary>
        public double NutzungsdauerA;

        /// <summary>Schwelle nachziehen statt fester Zielschwelle.</summary>
        public bool Adaptiv = true;

        /// <summary>Excel-Kompatibilitaetsmodus (eta = 1, SoC_min = 0).</summary>
        public bool Kompatibilitaetsmodus;

        // ==================================================================
        // Die vier Pruefregeln (Vorlaeufer :445-448, woertlich)
        // ==================================================================

        /// <summary>Welches Feld eine Regel beanstandet.</summary>
        public enum Feld
        {
            /// <summary>Kein Fehler.</summary>
            Keines = 0,
            /// <summary>Kapazitaet C_nom.</summary>
            Kapazitaet = 1,
            /// <summary>Obere Bandgrenze SoC_max.</summary>
            SoCMax = 2,
            /// <summary>Wirkungsgrad eta_RT.</summary>
            Wirkungsgrad = 3,
            /// <summary>Nutzungsdauer.</summary>
            Nutzungsdauer = 4
        }

        /// <summary>
        /// Prueft die vier Regeln in der Reihenfolge des Vorlaeufers und liefert die
        /// Meldung des ERSTEN Verstosses; <c>""</c> heisst „in Ordnung".
        /// </summary>
        /// <param name="feld">Das beanstandete Feld — die Oberflaeche hebt es hervor.</param>
        public string Pruefe(out Feld feld)
        {
            if (KapazitaetKwh <= 0.0)
            {
                feld = Feld.Kapazitaet;
                return MyResource.Resource.PEAK_MSG_KAPAZITAET;
            }
            if (SoCMaxProzent <= SoCMinProzent)
            {
                feld = Feld.SoCMax;
                return MyResource.Resource.PEAK_MSG_BAND;
            }
            if (WirkungsgradRt <= 0.0 || WirkungsgradRt > 1.0)
            {
                feld = Feld.Wirkungsgrad;
                return MyResource.Resource.PEAK_MSG_ETA;
            }
            if (NutzungsdauerA <= 0.0)
            {
                feld = Feld.Nutzungsdauer;
                return MyResource.Resource.PEAK_MSG_NUTZUNGSDAUER;
            }

            feld = Feld.Keines;
            return "";
        }

        // ==================================================================
        // Umrechnung in die Engine-Parameter (Vorlaeufer :451-478, woertlich)
        // ==================================================================

        /// <summary>Der Speicherparametersatz der Engine.</summary>
        public SpeicherParameter AlsSpeicherParameter()
        {
            return new SpeicherParameter
            {
                CNomKwh = KapazitaetKwh,
                PKw = PKw,
                SoCMinKwh = KapazitaetKwh * SoCMinProzent / 100.0,
                SoCMaxKwh = KapazitaetKwh * SoCMaxProzent / 100.0,
                RoundTripWirkungsgrad = WirkungsgradRt,
                StartSoCKwh = KapazitaetKwh * StartSoCProzent / 100.0,
                DtH = DtHStunden,
                CCapEurProKwh = CCapEurProKwh,
                CPowEurProKw = CPowEurProKw,
                IFixEur = IFixEur,
                Kapitalzins = KapitalzinsProzent / 100.0,
                NutzungsdauerA = NutzungsdauerA,
                DegradationProA = 0.0
            };
        }

        /// <summary>Der Kappungsparametersatz der Engine.</summary>
        public PeakShavingParameter AlsPeakShavingParameter()
        {
            return new PeakShavingParameter
            {
                PZielKw = Adaptiv ? 0.0 : ZielschwelleKw,
                Adaptiv = Adaptiv,
                LeistungspreisEurProKwA = LeistungspreisEurProKwA,
                BezugspreisMittelCtKwh = BezugspreisMittelCtKwh
            };
        }

        /// <summary>Der Rechenmodus der Engine.</summary>
        public SpeicherModus Modus => Kompatibilitaetsmodus
            ? SpeicherModus.ExcelKompatibilitaet
            : SpeicherModus.Energetisch;

        /// <summary>
        /// Die Vorbelegung aus Geraet und aktiver Variante als Eingabesatz.
        /// <c>Adaptiv</c> steht dabei fest auf <c>true</c> — der Vorlaeufer setzte den
        /// Haken unabhaengig von der Variante (:250).
        /// </summary>
        public static PeakShavingEingaben Aus(PeakShavingVorbelegung v)
        {
            if (v == null) v = new PeakShavingVorbelegung();
            return new PeakShavingEingaben
            {
                PKw = v.PKw,
                KapazitaetKwh = v.KapazitaetKwh,
                SoCMinProzent = v.SoCMinProzent,
                SoCMaxProzent = v.SoCMaxProzent,
                StartSoCProzent = v.StartSoCProzent,
                WirkungsgradRt = v.WirkungsgradRt,
                ZielschwelleKw = 0.0,
                LeistungspreisEurProKwA = v.LeistungspreisEurProKwA,
                BezugspreisMittelCtKwh = v.BezugspreisMittelCtKwh,
                CCapEurProKwh = v.CCapEurProKwh,
                CPowEurProKw = v.CPowEurProKw,
                IFixEur = v.IFixEur,
                KapitalzinsProzent = v.KapitalzinsProzent,
                NutzungsdauerA = v.NutzungsdauerA,
                Adaptiv = true,
                Kompatibilitaetsmodus = v.Kompatibilitaetsmodus
            };
        }
    }
}
