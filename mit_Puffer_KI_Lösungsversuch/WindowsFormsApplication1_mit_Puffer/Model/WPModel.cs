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
        } 
    }

}
