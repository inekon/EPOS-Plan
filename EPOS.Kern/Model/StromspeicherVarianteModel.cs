namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Betriebsfuehrung EINER Speichervariante (eine Zeile in Tab_StromspeicherVariante,
    // 1:1 zu Tab_Energieanlagen; Fachkonzept Stromspeicher 7.3).
    //
    // Abgrenzung zu StromspeicherModel: Dort steht das GERAET (Kapazitaet, Leistung,
    // Wirkungsgrad, Degradation, Investitionssaetze) - es kommt aus dem Katalog und ist
    // fuer alle Varianten desselben Geraets gleich. Hier steht, WIE dieses Geraet in
    // dieser Variante gefahren und bewertet wird.
    //
    // Bewusst NICHT als weitere Spalten in Tab_Energieanlagen (Fachkonzept 7.3): die
    // Tabelle wird von allen Gewerken geteilt und traegt bereits 57 Spalten.
    // ---------------------------------------------------------------------------
    public class StromspeicherVarianteModel
    {
        /// <summary>Vorgabe des nutzbaren SoC-Bands [%] (Fachkonzept 5.1).</summary>
        public const double SOC_MIN_VORGABE = 10.0;
        public const double SOC_MAX_VORGABE = 90.0;

        /// <summary>Vorgabe fuer Kapitalzins [%/a] und Nutzungsdauer [a] (Fachkonzept 5.1).</summary>
        public const double KAPITALZINS_VORGABE = 3.0;
        public const double NUTZUNGSDAUER_VORGABE = 20.0;

        public int ID;

        /// <summary>
        /// Verweis auf die Anlagenzeile (Tab_Energieanlagen.ID) mit
        /// ID_Type = SP_TYP bzw. REF_SP_TYP. 0 = nicht zugeordnet; in der Datenbank
        /// steht dafuer NULL (FK-Regel des Spaltenkatalogs).
        /// </summary>
        public int ID_Energieanlage;

        // --- Quellen-Matrix (Fachkonzept 2.1) ---

        /// <summary>Gruen- oder Graustrom; Werte aus DbWerte.SP_BETRIEBSART_*.</summary>
        public string Betriebsart = DbWerte.SP_BETRIEBSART_GRUENSTROM;

        /// <summary>PV-Ueberschuss ist zulaessige Ladequelle.</summary>
        public bool PV_Zulaessig = true;

        /// <summary>BHKW-UEBERSCHUSS ist zulaessige Ladequelle (waermegefuehrter Betrieb bleibt unberuehrt).</summary>
        public bool BHKW_Ueberschuss_Zulaessig = true;

        /// <summary>
        /// Stromgefuehrtes BHKW-Nachladen zugelassen. Nur waehlbar, wenn das Projekt
        /// auf stromgefuehrt steht (Tab_Einstellungen.Betriebsart = 1); der Rechenzweig
        /// kommt spaeter (Fachkonzept 2.1).
        /// </summary>
        public bool BHKW_Stromgefuehrt;

        /// <summary>Aktiver Verkauf ins Netz zulaessig - unabhaengig von der Betriebsart (AP10).</summary>
        public bool Netzentladung;

        // --- Betriebsband und Rechenweg ---

        /// <summary>Untere Grenze des nutzbaren Ladezustandsbands [% von C_nom].</summary>
        public double SoC_Min_Prozent = SOC_MIN_VORGABE;

        /// <summary>Obere Grenze des nutzbaren Ladezustandsbands [% von C_nom].</summary>
        public double SoC_Max_Prozent = SOC_MAX_VORGABE;

        /// <summary>Berechnungsart; Werte aus DbWerte.SP_BERECHNUNG_*.</summary>
        public string Berechnungsart = DbWerte.SP_BERECHNUNG_DAUERNUTZUNG;

        /// <summary>Herkunft der Bezugspreisreihe; Werte aus DbWerte.SP_PREISQUELLE_*.</summary>
        public string Preisquelle = DbWerte.SP_PREISQUELLE_FIXPREIS;

        /// <summary>
        /// Gewaehlte Preisreihe (Tab_Preisreihe.ID) bei Preisquelle = Spotmarkt.
        /// 0 = keine gewaehlt; dann sucht der Controller die zum Simulationsjahr
        /// passende Reihe selbst (Stichtagsregel Fachkonzept 4.1). AP4.
        /// </summary>
        public int ID_Preisreihe;

        /// <summary>
        /// Gewaehltes Kostenprofil (Tab_Kostenprofil.ID) bei Preisquelle = Profil.
        /// 0 = keines gewaehlt; dann faellt der Controller auf den Fixpreis zurueck und
        /// vermerkt es im Protokoll. AP4.
        /// </summary>
        public int ID_Kostenprofil;

        /// <summary>
        /// Aufschlaege auf die gewaehlte Preisquelle anwenden (Fachkonzept 4.2: "je
        /// Quelle existiert das Flag 'Aufschlag anwenden'"). Vorbelegung WAHR - der
        /// Bestandsfall Fixpreis rechnet den Arbeitspreis des Kostenmoduls plus
        /// Aufschlaege. Auf FALSCH gesetzt ist die gewaehlte Reihe bereits ein
        /// Vollpreis. AP4.
        /// </summary>
        public bool Aufschlag_Anwenden = true;

        /// <summary>
        /// Excel-Kompatibilitaetsmodus (Fachkonzept 5.2) - ausschliesslich fuer
        /// Referenztests gegen die V7-Mappe, nie fuer Produktivergebnisse.
        /// </summary>
        public bool Kompatibilitaetsmodus;

        // --- Wirtschaftlichkeit je Variante ---

        /// <summary>Kalkulatorischer Kapitalzins i_z [%/a] - Anzeigeeinheit, die Engine bekommt den Bruch.</summary>
        public double Kapitalzins = KAPITALZINS_VORGABE;

        /// <summary>Nutzungsdauer N [a].</summary>
        public double Nutzungsdauer = NUTZUNGSDAUER_VORGABE;

        /// <summary>Leistungspreis des Netzes L_P [EUR/(kW*a)] - Monetarisierung des Peak-Shavings (4.4).</summary>
        public double L_P;

        /// <summary>Aufschlag auf Netzladestrom a_netzlade [ct/kWh] (4.4).</summary>
        public double A_Netzlade;

        // --- Verwaltung ---

        /// <summary>
        /// Die aktive Variante speist Uebersichtsanzeige und Gesamtsimulation
        /// (Fachkonzept 5.5/7.3). Hoechstens eine je Projekt.
        /// </summary>
        public bool Aktiv;

        /// <summary>
        /// Uebernahme aus Tab_Einstellungen.Ladeschwellwert (Fachkonzept 5.6). Wird auf
        /// den Preissteuerungs-Schwellwert der Arbitrage abgebildet (6.5); bis AP10 ohne
        /// Wirkung.
        /// </summary>
        public double Ladeschwellwert;

        public StromspeicherVarianteModel()
        {
        }
    }
}
