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

        /// <summary>
        /// <c>Mindestleistung_kW</c> [kW] - die kleinste Modulationsleistung (Welle M4, WP1).
        /// <b><c>null</c> = keine Taktrechnung</b>; das Gerät moduliert dann bis null wie zuvor.
        /// </summary>
        public double? MindestleistungKw;

        /// <summary>
        /// <c>Taktverlustfaktor_Cd</c> [-] - der Teillastkoeffizient nach EN 14825 (Welle M4, WP1).
        /// <b><c>null</c> = Vorgabe 0,9.</b>
        /// </summary>
        public double? TaktverlustfaktorCd;

        /// <summary>
        /// Die acht Gerätespalten der Übergabegrenze (UB‑E3, <see cref="GeraetegrenzWerte"/>), soweit ein Speicherweg sie
        /// mitbringt. <b><c>null</c> = nicht angefasst</b> — der Speicherweg lässt die Spalten dann stehen.
        /// </summary>
        internal Geraetespalten Grenzspalten;
        
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
