namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Aufschlagsblock und Verguetungssaetze EINER Zeile von energy_project_settings
    // (Fachkonzept Stromspeicher 4.2/4.3, Arbeitspaket AP4).
    //
    // Abgrenzung: Der ARBEITSPREIS steht weiterhin in energy_price (stichtagsversioniert
    // ueber valid_from/valid_to) und wird von ucFuelSettings gepflegt. Hier stehen die
    // AUFSCHLAEGE, die auf jeden Bezugspreis addiert werden - unabhaengig davon, ob der
    // Energiepreis aus dem Fixpreis, aus einem Kostenprofil oder aus einer Spotreihe
    // kommt.
    //
    // Alle Werte in ct/kWh. Die Umrechnung aus dem EUR/kWh-Arbeitspreis macht der
    // Controller (StromAufschlagCtrl), nicht die Datenbank.
    // ---------------------------------------------------------------------------
    public class StromAufschlagModel
    {
        // --- Vorschlagswerte des Fachkonzepts 4.2 ---
        //
        // EINE Wahrheit fuer Migrationsschritt 12d, Leseseite und Oberflaeche. Ein
        // zweiter Satz Zahlen an einer der drei Stellen waere genau die Doppelpflege,
        // die der Spaltenkatalog fuer die Schemaseite schon vermeidet.

        /// <summary>Netzentgelt Arbeit [ct/kWh].</summary>
        public const double NETZENTGELT_VORGABE = 6.440;

        /// <summary>Umlagen [ct/kWh] - Summe aus 0,446 + 1,559 + 0,941 (Fachkonzept 4.2).</summary>
        public const double UMLAGEN_VORGABE = 2.946;

        /// <summary>Stromsteuer im Regelfall [ct/kWh].</summary>
        public const double STROMSTEUER_REGELFALL = 2.050;

        /// <summary>
        /// Stromsteuer fuer energieintensive Unternehmen mit Stromsteuerreduktion
        /// [ct/kWh]. Die zweite Schnellwahl der Oberflaeche; sie erklaert den
        /// Widerspruch der V7-Mappe (Parameterblock 0,05 - Variantenblaetter 2,05).
        /// </summary>
        public const double STROMSTEUER_REDUZIERT = 0.050;

        /// <summary>Konzessionsabgabe [ct/kWh].</summary>
        public const double KONZESSION_VORGABE = 0.110;

        /// <summary>Vertrieb [ct/kWh].</summary>
        public const double VERTRIEB_VORGABE = 0.200;

        /// <summary>Summe der Vorschlagswerte im Regelfall [ct/kWh] - 11,746.</summary>
        public const double SUMME_REGELFALL =
            NETZENTGELT_VORGABE + UMLAGEN_VORGABE + STROMSTEUER_REGELFALL +
            KONZESSION_VORGABE + VERTRIEB_VORGABE;

        /// <summary>Summe im reduzierten Stromsteuerfall [ct/kWh] - 9,746.</summary>
        public const double SUMME_REDUZIERT =
            NETZENTGELT_VORGABE + UMLAGEN_VORGABE + STROMSTEUER_REDUZIERT +
            KONZESSION_VORGABE + VERTRIEB_VORGABE;

        /// <summary>
        /// Feste Einspeiseverguetung PV [ct/kWh] (Fachkonzept 4.3, Vorschlag 5) -
        /// wertgleich dem bisherigen Platzhalter
        /// <c>StromspeicherSimCtrl.VERGUETUNG_PV_CT_KWH</c>, damit die Umstellung auf
        /// AP4 an dieser Stelle ergebnisneutral bleibt.
        /// </summary>
        public const double VERGUETUNG_PV_VORGABE = 5.0;

        /// <summary>
        /// Einspeise-/KWK-Erloes BHKW [ct/kWh]. Real liegt er meist ueber dem PV-Wert -
        /// erst das macht die Merit-Order "PV vor BHKW" wirksam (Fachkonzept 2.2). Die
        /// Vorbelegung bleibt trotzdem beim PV-Wert, weil eine hoehere Zahl eine
        /// Behauptung ueber einen konkreten KWK-Vertrag waere.
        /// </summary>
        public const double VERGUETUNG_BHKW_VORGABE = 5.0;

        // --- Zeilenbezug ---

        /// <summary>Projekt (energy_project_settings.ID_Projekt).</summary>
        public int ID_Projekt;

        /// <summary>Energietraeger (energy_project_settings.ID_Energietraeger).</summary>
        public int ID_Energietraeger;

        // --- Komponenten (Werte in ct/kWh) ---

        /// <summary>Netzentgelt Arbeit.</summary>
        public double Netzentgelt = NETZENTGELT_VORGABE;
        public bool Netzentgelt_Aktiv = true;

        /// <summary>Umlagen (Summenwert; die Einzelposten sind rein informativ).</summary>
        public double Umlagen = UMLAGEN_VORGABE;
        public bool Umlagen_Aktiv = true;

        /// <summary>Stromsteuer - Regelfall oder reduziert (Fachkonzept 4.2).</summary>
        public double Stromsteuer = STROMSTEUER_REGELFALL;
        public bool Stromsteuer_Aktiv = true;

        /// <summary>Konzessionsabgabe.</summary>
        public double Konzession = KONZESSION_VORGABE;
        public bool Konzession_Aktiv = true;

        /// <summary>Vertrieb.</summary>
        public double Vertrieb = VERTRIEB_VORGABE;
        public bool Vertrieb_Aktiv = true;

        // --- Modus ---

        /// <summary>Werte aus <see cref="DbWerte"/>.SP_AUFSCHLAG_MODUS_*.</summary>
        public string Modus = DbWerte.SP_AUFSCHLAG_MODUS_AUFGESCHLUESSELT;

        /// <summary>Gesamtaufschlag im Override-Modus [ct/kWh].</summary>
        public double Override;

        // --- Verguetung (Fachkonzept 4.3) ---

        /// <summary>v_pv [ct/kWh].</summary>
        public double Verguetung_PV = VERGUETUNG_PV_VORGABE;

        /// <summary>v_bhkw [ct/kWh].</summary>
        public double Verguetung_BHKW = VERGUETUNG_BHKW_VORGABE;

        /// <summary>
        /// true, wenn die Zeile aus der Datenbank stammt. false heisst: Es gab keine
        /// Zeile (oder die Spalten fehlen noch), und alles oben sind Vorgabewerte. Die
        /// Oberflaeche weist das aus, statt eine Pflege vorzutaeuschen.
        /// </summary>
        public bool AusDatenbank;

        public StromAufschlagModel()
        {
        }
    }
}
