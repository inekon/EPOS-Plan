namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Kopf einer Preisreihe (eine Zeile in Tab_Preisreihe; Fachkonzept Stromspeicher
    // 4.1 a und 8.4, angelegt von SchemaMigration Schritt 12b).
    //
    // Die WERTE stehen in Tab_PreisreiheDaten und werden ueber PreisreiheCtrl gelesen
    // und geschrieben - dieselbe Kopf/Daten-Trennung wie bei Tab_Stromganglinie(Daten).
    // ---------------------------------------------------------------------------
    public class PreisreiheModel
    {
        /// <summary>Primaerschluessel (MAX(ID)+1-Hausmuster).</summary>
        public int ID;

        /// <summary>
        /// Projekt, zu dem die Reihe gehoert. 0 = STAMMREIHE: Sie steht allen Projekten
        /// zur Verfuegung; in der Datenbank steht dafuer NULL (FK-Regel des
        /// Spaltenkatalogs: "nicht gesetzt" ist NULL, nicht 0).
        /// </summary>
        public int ID_Projekt;

        /// <summary>Anzeigename, vom Anwender vergeben (Vorschlag: Dateiname + Jahr).</summary>
        public string Bezeichner = "";

        /// <summary>
        /// Kalenderjahr der Reihe - aus der Datei gelesen, nicht vom Anwender getippt.
        /// Er entscheidet, welche Reihe eine Simulation ueber die Stichtagsregel
        /// (Fachkonzept 4.1) zieht.
        /// </summary>
        public int Jahr;

        /// <summary>Werte aus <see cref="DbWerte"/>.PREISREIHE_AUFLOESUNG_*.</summary>
        public string Aufloesung = DbWerte.PREISREIHE_AUFLOESUNG_STUNDE;

        /// <summary>Einheit, stets <see cref="DbWerte.PREISREIHE_EINHEIT_CT_KWH"/>.</summary>
        public string Einheit = DbWerte.PREISREIHE_EINHEIT_CT_KWH;

        /// <summary>
        /// Anzahl der hinterlegten Werte - NICHT persistiert, sondern beim Auflisten
        /// mitgezaehlt. Sie steht im Modell, damit die Auswahllisten "8760 Werte"
        /// anzeigen koennen, ohne die Reihe zu laden.
        /// </summary>
        public int Werteanzahl;

        public PreisreiheModel()
        {
        }

        /// <summary>true, wenn die Reihe keinem Projekt zugeordnet ist (Stammreihe).</summary>
        public bool IstStamm
        {
            get { return ID_Projekt <= 0; }
        }
    }
}
