namespace WindowsFormsApplication1
{
    // ---------------------------------------------------------------------------
    // Detail: eine Pufferspeicher-Zeile eines Simulationslaufs
    // (Tab_ErgebnisPufferspeicher, Konzept 6.6).
    //
    // Je Lauf wird eine Zeile je beteiligtem Speicher geschrieben - für den
    // Senkenspeicher der Wärmepumpe (Verwendung "Heizung") ebenso wie für jeden
    // Quellspeicher eines WP-Moduls (Verwendung "Quelle"). Die Werte stammen aus
    // denselben SimulationPufferspeicher-Objekten, die auch Navigator, CSV-Export
    // und die Ergebnistabelle der Detailansicht speisen (eine Quelle der Wahrheit).
    //
    // Die Tabelle wird in Schritt 3 der SchemaMigration angelegt; ErgebnisCtrl
    // hält mit StellePufferTabelleSicher() eine Rückfallebene für Datenbanken,
    // deren Migration noch nicht gelaufen ist.
    // ---------------------------------------------------------------------------
    public class ErgebnisPufferspeicherModel
    {
        /// <summary>Speicherdatensatz (Tab_Pufferspeicher bzw. _STAMM), 0 = unbekannt.</summary>
        public int ID_Pufferspeicher;

        /// <summary>Bezeichner des Speichers (aus dem Speicherobjekt).</summary>
        public string Bezeichner = "";

        /// <summary>Rolle im Lauf: "Heizung" (Senke) oder "Quelle" - TEXT(50).</summary>
        public string Verwendung = "";

        public double Q_max;             // kWh (nutzbare Kapazität)
        public double Ladung_gesamt;     // kWh/a
        public double Entladung_gesamt;  // kWh/a
        public double Verluste_gesamt;   // kWh/a
        public double SOC_Ende;          // kWh (Füllstand in Stunde 8759)
        public double SOC_Mittel;        // kWh (Jahresmittel der Ganglinie)
        public double SOC_Max;           // kWh (Jahresmaximum der Ganglinie)
        public double Vollzyklen;        // - (Ladung_gesamt / Q_max)
    }
}
