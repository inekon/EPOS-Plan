namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die <b>Festwerte des Gebäudemodells</b> (Stufe G1; Rechenschritte 1.3, Vorgaben bei
    /// NULL aus Rechenschritte 1.1). Sie stehen im Quelltext, nicht in der Datenbank, und sind
    /// Teil des Rechenwegs; jede Zahl trägt ihre Herkunft. Eine Zahl, die hier nicht steht,
    /// setzt das Modell nicht — auch nicht als Rückfall.
    /// </summary>
    internal static class GebaeudeFestwerte
    {
        // ---- Übergänge und Klassenweg (Rechenschritte 1.3, Schritt A) ------------------

        /// <summary>Innerer Strahlungsübergang α_str,i [W/(m²K)] — Gl. (30).</summary>
        internal const double ALPHA_STR_INNEN = 5.0;

        /// <summary>Innerer konvektiver Übergang der Wände α_kon,i [W/(m²K)] — Testräume der Richtlinie.</summary>
        internal const double ALPHA_KON_INNEN = 2.7;

        /// <summary>Konvektiver Anteil des äußeren Übergangs α_kon,A [W/(m²K)] (Rechenschritte E5).</summary>
        internal const double ALPHA_KON_AUSSEN = 20.0;

        /// <summary>
        /// Strahlungsanteil des äußeren Übergangs α_str,A [W/(m²K)], wenn die Gegenstrahlung
        /// fehlt (NULL-Regel E5, Rechenschritte 1.2) — und in Stufe G1 stets.
        /// </summary>
        internal const double ALPHA_STR_AUSSEN_RUECKFALL = 5.0;

        /// <summary>Äußerer Übergang gesamt α_A = α_kon,A + α_str,A [W/(m²K)] — Gl. (38), benannte Festlegung.</summary>
        internal const double ALPHA_AUSSEN = ALPHA_KON_AUSSEN + ALPHA_STR_AUSSEN_RUECKFALL;

        /// <summary>Massen-Oberflächen-Koeffizient h_ms [W/(m²K)] — EPOS-Klassenweg.</summary>
        internal const double H_MS = 9.1;

        /// <summary>Innerer Wärmeübergangswiderstand R_si [m²K/W], im U-Wert enthalten.</summary>
        internal const double R_SI = 0.13;

        /// <summary>Wärmekapazität der Luft c·ρ [Wh/(m³K)].</summary>
        internal const double C_RHO_LUFT = 0.34;

        /// <summary>Umrechnung Wh → J.</summary>
        internal const double SEKUNDEN_JE_STUNDE = 3600.0;

        // ---- Klimaweg (Rechenschritte 1.3, Schritt E) ----------------------------------

        /// <summary>Winkelkorrektur Fenster F_W [–] — EPOS-Vorgabe.</summary>
        internal const double F_W = 0.9;

        /// <summary>Konvektiver Anteil des Fenstersolars a_kon [–].</summary>
        internal const double A_KON_SOLAR = 0.09;

        /// <summary>Anteil der inneren Lasten, der konvektiv an die Luft geht [–] (E4: 0,5/0,5).</summary>
        internal const double ANTEIL_INNERE_LASTEN_KONVEKTIV = 0.5;

        /// <summary>Tiefe der Erdreichtemperatur z [m] — EPOS-Ergänzung.</summary>
        internal const double ERDREICH_TIEFE_M = 1.0;

        /// <summary>Temperaturleitfähigkeit des Erdreichs α_Erd [m²/d] (E6).</summary>
        internal const double ERDREICH_TEMPERATURLEITFAEHIGKEIT_M2D = 0.06;

        /// <summary>Vorlauf des Jahreslaufs [d] (Konzept 4.6, Rechenschritte 7.2).</summary>
        internal const int VORLAUF_TAGE = 30;

        // ---- Plausibilitätsgrenzen (Konzept 4.8) ---------------------------------------

        /// <summary>Untergrenze Bauweise je Nutzfläche [Wh/(m²K)].</summary>
        internal const double BAUWEISE_JE_M2_MIN = 5.0;

        /// <summary>Obergrenze Bauweise je Nutzfläche [Wh/(m²K)].</summary>
        internal const double BAUWEISE_JE_M2_MAX = 200.0;

        /// <summary>Untergrenze eines U-Werts [W/(m²K)].</summary>
        internal const double U_MIN = 0.1;

        /// <summary>Obergrenze eines U-Werts [W/(m²K)].</summary>
        internal const double U_MAX = 6.0;

        /// <summary>Toleranz der Summe der Fensterflächen gegen die gesamte Fensterfläche [m²].</summary>
        internal const double FENSTERSUMME_TOLERANZ_M2 = 0.01;

        // ---- Vorgaben bei NULL (Rechenschritte 1.1) -------------------------------------

        /// <summary>Rahmenanteil 1 − F_F bei NULL.</summary>
        internal const double VORGABE_RAHMENANTEIL = 0.3;

        /// <summary>Verschattungsfaktor F_S bei NULL.</summary>
        internal const double VORGABE_VERSCHATTUNGSFAKTOR = 0.9;

        /// <summary>Kellertemperatur θ_NR [°C] bei NULL.</summary>
        internal const double VORGABE_KELLERTEMPERATUR = 10.0;

        /// <summary>Masseanteil der Außenbauteile a_AW bei NULL.</summary>
        internal const double VORGABE_MASSEANTEIL_AUSSEN = 0.3;

        /// <summary>Innenflächenfaktor f_IW bei NULL.</summary>
        internal const double VORGABE_INNENFLAECHENFAKTOR = 2.5;

        /// <summary>Strahlungsanteil der Heizübergabe a_str,H bei NULL.</summary>
        internal const double VORGABE_HEIZUNG_STRAHLUNGSANTEIL = 0.3;

        /// <summary>Stunden des Tagsollwerts, 1-basiert (E8): erste Stunde.</summary>
        internal const int TAG_ERSTE_STUNDE = 7;

        /// <summary>Stunden des Tagsollwerts, 1-basiert (E8): letzte Stunde.</summary>
        internal const int TAG_LETZTE_STUNDE = 22;

        /// <summary>Der Wochenendsollwert wirkt nur über diesem Wert [°C] (E8).</summary>
        internal const double WOCHENENDE_SOLLWERT_SCHWELLE = 5.0;

        /// <summary>Der Ferienfahrplan ist aktiv, wenn das Flag über diesem Wert liegt (E8).</summary>
        internal const double FERIEN_FLAG_SCHWELLE = 0.9;

        /// <summary>… und der Feriensollwert mindestens diesen Wert trägt [°C] (E8).</summary>
        internal const double FERIEN_SOLLWERT_MIN = 1.0;
    }
}
