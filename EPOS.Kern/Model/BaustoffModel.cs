namespace WindowsFormsApplication1
{
    /// <summary>
    /// EIN Baustoff — eine Zeile aus <c>Tab_Baustoff_STAMM</c> (Katalog) oder
    /// <c>Tab_Baustoff</c> (Projektkopie); Schemaschritt S-A, <see cref="BaustoffSchema"/>.
    ///
    /// <para><b>Öffentliche Felder statt Eigenschaften</b> — Hausmuster aller <c>*Model</c>-Klassen
    /// (<see cref="AnlageStrangModel"/>): reine Datenträger zwischen Controller und Oberfläche.
    /// Die Feldnamen folgen den Spaltennamen.</para>
    ///
    /// <para><b>Durchgehend nullbar, wo NULL eine Aussage trägt:</b> Ein Stoffwert NULL heißt
    /// „nicht angegeben" — eine 0 wäre ein anderer Stoff (λ = 0 ist ein unendlicher
    /// Wärmewiderstand, Mehrzonenkonzept 3.5). <see cref="Hersteller"/> NULL heißt
    /// herstellerneutral.</para>
    /// </summary>
    public class BaustoffModel
    {
        /// <summary>Primärschlüssel; 0 = noch nicht gespeichert.</summary>
        public int ID;

        /// <summary>Projekt der Kopie; <c>null</c> = Katalogsatz (<c>Tab_Baustoff_STAMM</c>).</summary>
        public int? ID_Projekt;

        /// <summary>Name des Stoffes (Pflicht, höchstens 80 Zeichen).</summary>
        public string Bezeichner = "";

        /// <summary>Ordnungsgruppe; <c>null</c> = ohne Gruppe.</summary>
        public string Gruppe;

        /// <summary>Hersteller; <c>null</c> = herstellerneutral (Norm- oder Richtwert).</summary>
        public string Hersteller;

        /// <summary>Wärmeleitfähigkeit [W/(m·K)]; <c>null</c> = nicht angegeben.</summary>
        public double? Lambda;

        /// <summary>Rohdichte [kg/m³]; <c>null</c> = nicht angegeben.</summary>
        public double? Rho;

        /// <summary>Spezifische Wärmekapazität [J/(kg·K)] (Spalte <c>cp</c>); <c>null</c> = nicht angegeben.</summary>
        public double? Cp;

        /// <summary>Regelwerk oder Dateiname des Imports; <c>null</c> = nicht angegeben.</summary>
        public string Quelle;

        /// <summary>Herkunft (<see cref="DbWerte.HERKUENFTE"/>); <c>null</c> = nicht angegeben.</summary>
        public string Herkunft;

        /// <summary>Kennung der Quellentität eines Imports; <c>null</c> = keine.</summary>
        public string Quellkennung;

        /// <summary>„Gehört zur Auslieferung" — nur im Katalog; schützt vor Ändern und Löschen.</summary>
        public bool ReadOnly;

        /// <summary>Eine entkoppelte Kopie.</summary>
        public BaustoffModel Kopie() => (BaustoffModel)MemberwiseClone();
    }
}
