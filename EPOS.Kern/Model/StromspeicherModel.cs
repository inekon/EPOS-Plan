namespace WindowsFormsApplication1
{
    
    public class StromspeicherModel
    {
        public int m_ID;
        public string m_szBezeichner;
        public double m_Energie;        // C_nom, nutzbare Nennkapazitaet [kWh]
        public double m_Leistung;       // P, gemeinsame Lade-/Entladeleistung [kW]
        public double m_Degradation;    // d, Kapazitaetsverlust [%/a]
        public double m_Ladezustand;    // Start-SoC [%] (AP0-Entscheid 16.08.2026)
        public string m_szTyp;
        public double m_Modulkosten;    // c_cap, kapazitaetsbezogene Investition [EUR/kWh]

        // --- AP3: Geraetetechnik (Fachkonzept Stromspeicher 5.1) ---
        // Neue Spalten in Tab_Stromspeicher UND Tab_Stromspeicher_STAMM, angelegt von
        // SchemaMigration Schritt 11a. Alle sechs beschreiben das GERAET; die
        // Betriebsfuehrung (SoC-Band, Betriebsart, Berechnungsart, Zins, Nutzungsdauer)
        // steht je Variante in Tab_StromspeicherVariante (StromspeicherVarianteModel).
        public double m_WirkungsgradRT;      // eta_RT, Round-Trip-Wirkungsgrad [-], Vorgabe 0,90
        public int m_ZyklenZugesichert;      // N_zyk, zugesicherte Volladezyklen [-]
        public double m_Verschleisskosten;   // c_ver [EUR/(kWh*Zyklus)]
        public double m_Leistungskosten;     // c_pow, leistungsbezogene Investition [EUR/kW]
        public double m_InvestitionFix;      // I_fix, Festanteil der Investition [EUR]
        public double m_StandbyVerbrauch;    // Standby-/Eigenverbrauch [W]

        /// <summary>
        /// Vorgabewert des Round-Trip-Wirkungsgrads (Fachkonzept 5.2). Bewusst KEIN
        /// DEFAULT in der Datenbank: der wuerde nur neue Zeilen erreichen und alle
        /// Bestandszeilen auf 0 stehen lassen - einen Wert, den die Engine als
        /// unbrauchbar zurueckweist. Die Vorgabe setzt deshalb die Leseseite.
        /// </summary>
        public const double WIRKUNGSGRAD_RT_VORGABE = 0.90;

        /// <summary>
        /// Vorgabewert der Zyklus-Verschleisskosten c_ver [€/(kWh·Zyklus)]
        /// (Fachkonzept 5.4).
        /// </summary>
        /// <remarks>
        /// iU9-W14a.0f (Befund W14-B44): Der Wert stand als <c>C_VER_VORGABE</c> IN der
        /// Maske <c>Form_AdminStromspeicher</c> (Z. 389), waehrend sein Zwilling
        /// <see cref="WIRKUNGSGRAD_RT_VORGABE"/> hier lag - zwei fachliche Vorgaben an
        /// zwei Orten. Jetzt stehen beide beieinander; der Wert selbst ist unveraendert.
        /// </remarks>
        public const double C_VER_VORGABE = 0.025;

        public StromspeicherModel()
        {
            m_ID = 0;
            m_szBezeichner = string.Empty;
            m_Energie = 0;
            m_Leistung = 0;
            m_Degradation = 0.0;
            m_Ladezustand = 0;
            m_szTyp = string.Empty;
            m_Modulkosten = 0;

            m_WirkungsgradRT = 0.0;
            m_ZyklenZugesichert = 0;
            m_Verschleisskosten = 0.0;
            m_Leistungskosten = 0.0;
            m_InvestitionFix = 0.0;
            m_StandbyVerbrauch = 0.0;
        }

    }
}
