namespace WindowsFormsApplication1
{
    
    public class WPModel
    {
        public WPModel[] items;
        public int ID;
        public int ID_Projekt;
        public string WPName;
        public string Firma;
        public string Beschreibung;
        public string Typ;
        public int Baujahr;
        public string Aufstellung;
        public int Nennleistung;
        public int maxPTherm;
        public double Heizung;
        public string Regelung;
        public int Modulkosten;
        public string Leistungsstufen;
        public double Kuehlleistung;
        public int MaxVorlauf;
        public int MinVorlauf;
        public string Bauart;
        public bool m_bReadOnly;

        // =============================================================================
        // KU-S3 - der Kuehlbetrieb am Erzeuger (Schemaschritt 114; Kuehlkonzept 7.3; E15, E33)
        // =============================================================================
        //
        // Dieselben drei Spalten an Tab_WP und Tab_WP_STAMM. Gelesen und geschrieben werden
        // sie NULL-treu (WPCtrl.KuehlfelderLesen, WPCtrl.Insert/CopyFromStamm,
        // WPStammCtrl.Insert/UebernehmenAusProjekt): NULL traegt bei Vorlauf und
        // Hilfsstromanteil eine eigene Aussage. KEIN Rechenweg liest sie vor KU2 Welle 2.

        /// <summary>
        /// <c>Kuehlbetrieb</c> - „diese Maschine wird im Projekt auch zum Kuehlen benutzt".
        /// 0/1 in der Datenbank, nie NULL; Vorgabe <c>false</c>.
        /// </summary>
        public bool Kuehlbetrieb;

        /// <summary>
        /// <c>Kuehl_Vorlauf</c> [Grad C] - der Kaltwasser-Vorlauf, der die Kuehlkennlinie waehlt
        /// (aus deren Stuetzstellen, K21). <b><c>null</c> = kleinster Stuetzwert</b> der Kennlinie.
        /// </summary>
        public int? KuehlVorlauf;

        /// <summary>
        /// <c>Kuehl_Hilfsstromanteil</c> [-] - Anteil Hilfsstrom an der Verdichterarbeit des
        /// Kuehlbetriebs, je Anlage (K23). <b><c>null</c> = kein Zuschlag.</b>
        /// </summary>
        public double? KuehlHilfsstromanteil;
        
        public WPModel()
        {
            items = null;
            ID = 0;
            ID_Projekt = 0;
            WPName = "";
            Firma = "";
            Beschreibung = "";
            Typ = "";
            Baujahr = 2000;
            Aufstellung = "";
            Nennleistung = 0;
            maxPTherm = 0;
            Heizung = 0;
            Regelung = "";
            Modulkosten = 0;
            Leistungsstufen = "";
            Kuehlleistung = 0;
            MaxVorlauf = 0;
            MinVorlauf = 0;
            Bauart = "";
            m_bReadOnly = false;
            Kuehlbetrieb = false;
            KuehlVorlauf = null;
            KuehlHilfsstromanteil = null;
        } 
    }

}
