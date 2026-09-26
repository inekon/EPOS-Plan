namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Woher eine Zahl des Gebäudeimports kommt</b> (Datenaustauschkonzept 1.4 Nr. 1, 2.2;
    /// Frage D8). Ein WERT, kein Anzeigetext — die Beschriftung liefert
    /// <see cref="GebaeudeZuordnungsModell.HerkunftText"/> (Drei-Schichten-Regel, Muster
    /// <see cref="ImportKonfliktModell"/>).
    ///
    /// <para>Je Format ein eigener Wert (<see cref="Ifc"/>, <see cref="GbXml"/>) statt eines
    /// gemeinsamen „Datei": Der Anwender soll in der Zelle sehen, woher die Zahl kommt. Die Regel
    /// aus dem Mehrzonenkonzept 6.4 gilt für beide Formate: <i>eine Zelle ohne Beleg ist eine
    /// Vorgabe.</i></para>
    /// </summary>
    internal enum Importherkunft
    {
        /// <summary>Nichts gefunden, nichts vorbelegt.</summary>
        Leer = 0,

        /// <summary>Aus der IFC-Datei gelesen (Stufe G4a).</summary>
        Ifc = 1,

        /// <summary>Aus der gbXML-Datei gelesen (Stufe G4c).</summary>
        GbXml = 2,

        /// <summary>Aus dem Bauteil- oder Baustoffkatalog übernommen (Weg <c>CopyFromStamm</c>, G3).</summary>
        Katalog = 3,

        /// <summary>Aus der Baualtersklasse vorbelegt (<see cref="GebaeudeVorgaben"/>).</summary>
        Vorgabe = 4,

        /// <summary>Vom Anwender im Zuordnungsdialog gesetzt oder geändert.</summary>
        Manuell = 5,

        /// <summary>
        /// Aus dem FREIEN Wert nach Stein/Loga (2025) vorbelegt, weil die Baualtersklasse keinen
        /// Katalogsatz hat (Entscheid E51, <see cref="GebaeudeVorgaben"/>). Ein eigener Wert nur im
        /// Speicher — gespeichert wird er wie <see cref="Vorgabe"/> als <see cref="ImportherkunftWerte.VORGABE"/>;
        /// unterschieden wird danach über den Beleg (<see cref="GebaeudeVorgaben.BELEG_FREI"/>).
        /// </summary>
        VorgabeFrei = 6,
    }

    /// <summary>
    /// Die Persistenzwerte der Herkunft je ZEILE (<c>Tab_Zone.Herkunft</c>, <c>Tab_Bauteil.Herkunft</c>
    /// und die beiden Aufbau-/Baustofftabellen, Datenaustauschkonzept 7.3):
    /// <c>CHECK (Herkunft IN ('GBXML','IFC','KATALOG','MANUELL','VORGABE'))</c>. Datenbankwerte,
    /// nie übersetzt (Glossar § 10).
    /// </summary>
    internal static class ImportherkunftWerte
    {
        /// <summary>Persistenzwert „aus einer gbXML-Datei".</summary>
        public const string GBXML = "GBXML";

        /// <summary>Persistenzwert „aus einer IFC-Datei".</summary>
        public const string IFC = "IFC";

        /// <summary>Persistenzwert „aus dem Katalog kopiert".</summary>
        public const string KATALOG = "KATALOG";

        /// <summary>Persistenzwert „vom Anwender gesetzt".</summary>
        public const string MANUELL = "MANUELL";

        /// <summary>Persistenzwert „aus der Baualtersklasse vorbelegt".</summary>
        public const string VORGABE = "VORGABE";

        /// <summary>
        /// Der Persistenzwert einer Herkunft; <see cref="Importherkunft.Leer"/> hat keinen und
        /// ergibt <c>null</c> (die Spalte bleibt NULL). Der freie Wert
        /// (<see cref="Importherkunft.VorgabeFrei"/>) wird wie jede Vorgabe als <see cref="VORGABE"/>
        /// gespeichert — kein neuer Datenbankwert (E51).
        /// </summary>
        public static string Wert(Importherkunft herkunft)
        {
            switch (herkunft)
            {
                case Importherkunft.GbXml: return GBXML;
                case Importherkunft.Ifc: return IFC;
                case Importherkunft.Katalog: return KATALOG;
                case Importherkunft.Manuell: return MANUELL;
                case Importherkunft.Vorgabe:
                case Importherkunft.VorgabeFrei: return VORGABE;
                default: return null;
            }
        }

        /// <summary>Ist die Herkunft eine Vorgabe — aus dem Katalog oder der freie Wert (E51)?</summary>
        public static bool IstVorgabe(Importherkunft herkunft)
            => herkunft == Importherkunft.Vorgabe || herkunft == Importherkunft.VorgabeFrei;
    }
}
