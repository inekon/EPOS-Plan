namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // „Strompreis Details" - die ZERLEGUNG des Arbeitspreises EINER Zeile von
    // energy_project_settings, dazu die Verguetungssaetze (Fachkonzept Stromspeicher
    // 4.2/4.3 in der Fassung des Anwenderentscheids SP-E-2 vom 17.09.2026).
    //
    // BEDEUTUNG (SP-E-2 a): Die Anteile sind KEIN Aufschlag mehr. Sie sagen, woraus
    // der Arbeitspreis besteht:
    //
    //     Arbeitspreis = Beschaffung + Vertrieb + Arbeitspreis Netz
    //                    + Stromsteuer + Konzessionsabgabe + Umlagen
    //
    // Der ARBEITSPREIS der Traegerkarte (energy_price bzw.
    // energy_project_settings.custom_price_work) ist die eine Wahrheit fuer jeden
    // Leser; dieser Satz wird mit ihm verglichen, nie zu ihm addiert. Die einzige
    // Ausnahme ist die Spot- bzw. Profilreihe: Dort IST die Reihe die Beschaffung,
    // und die uebrigen aktiven Anteile kommen darauf (Fachkonzept 4.1 a/b).
    //
    // Alle Werte in ct/kWh. Die Umrechnung aus dem EUR/kWh-Arbeitspreis macht der
    // Controller (StrompreisZerlegungCtrl), nicht die Datenbank.
    // ---------------------------------------------------------------------------
    public class StrompreisZerlegungModel
    {
        // --- Vorschlagswerte ------------------------------------------------
        //
        // EINE Wahrheit fuer Leseseite und Oberflaeche. Sie stehen als VORSCHLAG in
        // den Feldern; gerechnet wird mit ihnen erst, wenn der Anwender den
        // zugehoerigen Haken setzt - die Aktiv-Schalter stehen bewusst auf false
        // (Restpunkt „Nach #266" und E5-Restpunkt „Aktiv-Flags kein verlaessliches
        // Aus": Ein Projekt, an dem niemand etwas eingestellt hat, rechnet mit einer
        // Anteilssumme von 0, nicht mit 11,746 ct/kWh).

        /// <summary>Arbeitspreis Netz [ct/kWh].</summary>
        public const double NETZENTGELT_VORGABE = 6.440;

        /// <summary>
        /// KWKG-Umlage [ct/kWh], Stichjahr 2026 (Anwenderangabe vom 17.09.2026;
        /// 2025: 0,277). Katalogschluessel <c>DbWerte.GESETZ_UMLAGE_KWKG</c> -
        /// diese Konstante ist die Rueckfallebene, nicht die Quelle.
        /// </summary>
        public const double UMLAGE_KWKG_VORGABE = 0.446;

        /// <summary>
        /// Offshore-Netzumlage [ct/kWh], Stichjahr 2026 (2025: 0,816).
        /// Katalogschluessel <c>DbWerte.GESETZ_UMLAGE_OFFSHORE</c>.
        /// </summary>
        public const double UMLAGE_OFFSHORE_VORGABE = 0.941;

        /// <summary>
        /// § 19 StromNEV-Umlage [ct/kWh], Stichjahr 2026 (2025: 1,558).
        /// Katalogschluessel <c>DbWerte.GESETZ_UMLAGE_STROMNEV19</c>.
        /// </summary>
        public const double UMLAGE_STROMNEV19_VORGABE = 1.559;

        /// <summary>
        /// Umlagen als Summenwert [ct/kWh] - 0,446 + 0,941 + 1,559 = 2,946.
        ///
        /// <para>Bis SP-W2/W3 stand hier die namenlose Klammer „0,446 + 1,559 +
        /// 0,941" des Fachkonzepts; seit SP-E-3-Q1 tragen die drei Summanden ihre
        /// Namen (KWKG, Offshore, § 19 StromNEV) und koennen einzeln gepflegt
        /// werden. Der Summenwert bleibt wertgleich.</para>
        /// </summary>
        public const double UMLAGEN_VORGABE =
            UMLAGE_KWKG_VORGABE + UMLAGE_OFFSHORE_VORGABE + UMLAGE_STROMNEV19_VORGABE;

        /// <summary>
        /// Stromsteuer im Regelfall [ct/kWh].
        ///
        /// <para><b>Rueckfallebene, nicht Quelle.</b> Die Schnellwahl der Oberflaeche
        /// liest den Gesetzeskatalog (<c>DbWerte.GESETZ_STROMST_REGELSATZ</c>,
        /// 20,50 EUR/MWh ab 2026 = 2,050 ct/kWh) und nicht diese Konstante. Der Wert
        /// hier greift nur, wenn der Katalog fuer das Bilanzjahr nichts liefert, und
        /// ist mit dem Katalogsatz ausdruecklich WERTGLEICH: Er darf nicht fuer sich
        /// fortgeschrieben werden. Eine Satzaenderung ist eine neue Jahreszeile im
        /// Katalog.</para>
        /// </summary>
        public const double STROMSTEUER_REGELFALL = 2.050;

        /// <summary>
        /// Stromsteuer fuer energieintensive Unternehmen mit Stromsteuerreduktion
        /// [ct/kWh], § 9b StromStG. Die zweite Schnellwahl der Oberflaeche.
        ///
        /// <para><b>Rueckfallebene, nicht Quelle</b> - wie
        /// <see cref="STROMSTEUER_REGELFALL"/>. Der Katalogschluessel
        /// <c>DbWerte.GESETZ_STROMST_REDUZIERT</c> ist mit der Saatgeneration 7
        /// eingesaet (Restpunkt S-6) und mit dieser Konstante wertgleich.</para>
        /// </summary>
        public const double STROMSTEUER_REDUZIERT = 0.050;

        /// <summary>Konzessionsabgabe [ct/kWh].</summary>
        public const double KONZESSION_VORGABE = 0.110;

        /// <summary>Vertrieb [ct/kWh].</summary>
        public const double VERTRIEB_VORGABE = 0.200;

        /// <summary>
        /// Summe der Vorschlagswerte OHNE Beschaffung im Regelfall [ct/kWh] - 11,746.
        /// Das ist der Satz, der auf eine Spot- oder Profilreihe gehoert, wenn alle
        /// Anteile aktiv sind.
        /// </summary>
        public const double SUMME_OHNE_BESCHAFFUNG_REGELFALL =
            NETZENTGELT_VORGABE + UMLAGEN_VORGABE + STROMSTEUER_REGELFALL +
            KONZESSION_VORGABE + VERTRIEB_VORGABE;

        /// <summary>Dieselbe Summe im reduzierten Stromsteuerfall [ct/kWh] - 9,746.</summary>
        public const double SUMME_OHNE_BESCHAFFUNG_REDUZIERT =
            NETZENTGELT_VORGABE + UMLAGEN_VORGABE + STROMSTEUER_REDUZIERT +
            KONZESSION_VORGABE + VERTRIEB_VORGABE;

        // --- Zeilenbezug ---

        /// <summary>Projekt (energy_project_settings.ID_Projekt).</summary>
        public int ID_Projekt;

        /// <summary>Energietraeger (energy_project_settings.ID_Energietraeger).</summary>
        public int ID_Energietraeger;

        // --- Gruppe 1: Beschaffung und Vertrieb (Werte in ct/kWh) ---

        /// <summary>
        /// Beschaffung - der Anteil, der NICHT zu Netz, Steuern und Umlagen gehoert
        /// (Anwenderbefund 16.09.2026: „ausserdem fehlt noch der Arbeitspreis, der
        /// nicht zu den Aufschlaegen gehoert"). Keine Vorbelegung: Die Oberflaeche
        /// schlaegt ihn als REST aus Arbeitspreis minus uebrige aktive Anteile vor,
        /// schreibt ihn aber nie still.
        /// </summary>
        public double Beschaffung;
        public bool Beschaffung_Aktiv;

        /// <summary>Vertrieb.</summary>
        public double Vertrieb = VERTRIEB_VORGABE;
        public bool Vertrieb_Aktiv;

        // --- Gruppe 2: Netzentgelte ---

        /// <summary>Arbeitspreis Netz.</summary>
        public double Netzentgelt = NETZENTGELT_VORGABE;
        public bool Netzentgelt_Aktiv;

        // --- Gruppe 3: Steuern, Abgaben und Umlagen ---

        /// <summary>Stromsteuer - Regelfall oder reduziert (§ 3 / § 9b StromStG).</summary>
        public double Stromsteuer = STROMSTEUER_REGELFALL;
        public bool Stromsteuer_Aktiv;

        /// <summary>Konzessionsabgabe.</summary>
        public double Konzession = KONZESSION_VORGABE;
        public bool Konzession_Aktiv;

        /// <summary>
        /// Umlagen als SUMMENWERT. Wirksam, solange
        /// <see cref="Umlagen_Einzeln"/> false ist; sonst tragen die drei Einzelposten.
        /// </summary>
        public double Umlagen = UMLAGEN_VORGABE;
        public bool Umlagen_Aktiv;

        /// <summary>
        /// true: Statt des Summenfelds werden KWKG-Umlage, Offshore-Netzumlage und
        /// § 19 StromNEV-Umlage einzeln gepflegt; ihre Summe IST dann der Umlagenwert
        /// (Anwenderwortlaut: „Umlagen als Summe und wahlweise Einzelkomponenten").
        /// </summary>
        public bool Umlagen_Einzeln;

        /// <summary>KWKG-Umlage.</summary>
        public double Umlage_KWKG = UMLAGE_KWKG_VORGABE;
        public bool Umlage_KWKG_Aktiv;

        /// <summary>Offshore-Netzumlage.</summary>
        public double Umlage_Offshore = UMLAGE_OFFSHORE_VORGABE;
        public bool Umlage_Offshore_Aktiv;

        /// <summary>§ 19 StromNEV-Umlage.</summary>
        public double Umlage_StromNEV19 = UMLAGE_STROMNEV19_VORGABE;
        public bool Umlage_StromNEV19_Aktiv;

        // --- Verguetung: NICHT MEHR HIER (Anwenderentscheid SP-E-5 (a), 17.09.2026) ---
        //
        // Die Traegerkarte trug bis zu diesem Entscheid v_pv und v_bhkw als eigenen
        // Block. Gelesen hat ihn nur die Speicherwelt, die Wirtschaftlichkeit rechnete
        // laengst mit Tab_ProjektWirtschaftlichkeit.Einspeiseverguetung und
        // Einspeiseverguetung_KWK - zwei Wahrheiten fuer denselben eingespeisten Strom.
        // Beide Verguetungen kommen ab hier aus den Wirtschaftlichkeitsparametern; die
        // Quellenkette steht an StromPreisCtrl.VerguetungenBauen. Die Spalten
        // Verguetung_PV/_BHKW gibt es im Schema nicht mehr: Schemaschritt 84 hat ihren
        // Inhalt in die Parameter umgezogen, Schritt 85 sie entfernt
        // (StrompreisAltspalten).

        /// <summary>
        /// true, wenn die Zeile aus der Datenbank stammt. false heisst: Es gab keine
        /// Zeile (oder die Spalten fehlen noch), und alles oben sind Vorgabewerte. Die
        /// Oberflaeche weist das aus, statt eine Pflege vorzutaeuschen.
        /// </summary>
        public bool AusDatenbank;

        public StrompreisZerlegungModel()
        {
        }
    }
}
