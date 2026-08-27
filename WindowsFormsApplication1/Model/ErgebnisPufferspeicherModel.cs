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

        /// <summary>
        /// PAKET E1 (Migrationsschritt 52): Energieanlage, zu der dieser Speicher gehört -
        /// belegt bei QUELLspeichern (SimulationPufferspeicher.ID_Anlage, Serienschlüssel
        /// QUELLE_&lt;AnlagenID&gt;), 0 bei Senkenspeichern. Ohne ihn waren zwei Module am
        /// selben Quellpuffer in der Persistenz nicht unterscheidbar, und die
        /// Ganglinien-Dateien quellspeicher_&lt;AnlagenID&gt;_*.csv liessen sich der Zeile
        /// nicht zuordnen. Geschrieben wird NULL statt 0 (keine Anlage), gelesen wird
        /// beides als 0.
        /// </summary>
        public int ID_Anlage;

        public double Q_max;             // kWh (nutzbare Kapazität)
        public double Ladung_gesamt;     // kWh/a
        public double Entladung_gesamt;  // kWh/a
        public double Verluste_gesamt;   // kWh/a
        public double SOC_Ende;          // kWh (Füllstand in Stunde 8759)
        public double SOC_Mittel;        // kWh (Jahresmittel der Ganglinie)
        public double SOC_Max;           // kWh (Jahresmaximum der Ganglinie)
        public double Vollzyklen;        // - (Ladung_gesamt / Q_max)

        /// <summary>
        /// PAKET E1 (Konzept 4.4): bedarfsdeckende Entladung JE KANAL [kWh/a], indiziert
        /// mit Kanal.HEIZUNG/BRAUCHWASSER/PROZESS.
        ///
        /// <para>Gebucht wird an derselben Stelle wie Entladung_gesamt
        /// (SimulationPufferspeicher.Entladen), mit dem Kanal des Durchlaufs aus der
        /// Entladeordnung - die Summe der drei Werte ist deshalb Entladung_gesamt; der
        /// Skalar bleibt getrennt akkumuliert und verschiebt sich durch die Aufteilung
        /// nicht.</para>
        ///
        /// <para>QUELLSPEICHER: Die Entnahme eines Moduls aus seinem Quellpuffer traegt
        /// keinen Bedarfskanal und wird - wie schon in
        /// Kaskadenschleife.Anteil_Entladen(sp, gedeckt) - auf dem HEIZKANAL gebucht
        /// (altverhaltenserhaltende Vorbelegung des Kanalmodells, Konzept 4.2/F18).</para>
        /// </summary>
        public double[] Entladung_Kanal = new double[Kanal.ANZAHL];

        /// <summary>
        /// PAKET E1 (Befund N6): durchgeflossene Aufnahme [kWh/a] - was der Speicher in
        /// derselben Stunde wieder abgegeben hat und deshalb nie Speicherinhalt war.
        /// Ohne Durchlass exakt 0. Bis Schritt 52 stand die Groesse nur am Objekt.
        /// </summary>
        public double Durchsatz_Geladen;

        /// <summary>PAKET E1 (Befund N6): wieder abgegebene Durchflussmenge [kWh/a];
        /// siehe Durchsatz_Geladen.</summary>
        public double Durchsatz_Entladen;

        /// <summary>
        /// P1-VORGRIFF (Migrationsschritt 52 legt nur die Spalte an): mittlere Temperatur
        /// der obersten Schicht [Grad C]. Das heutige Ein-Zonen-Modell kennt keine
        /// oberste Schicht; gefuellt wird erst mit dem Schichtmodell (Paket P1). Bis
        /// dahin schreibt der Runner NULL - null waere eine Behauptung.
        /// </summary>
        public double? T_oben_Mittel;

        /// <summary>P1-VORGRIFF: Jahresminimum der obersten Schicht [Grad C];
        /// siehe T_oben_Mittel.</summary>
        public double? T_oben_Min;
    }
}
