namespace WindowsFormsApplication1
{
    /// <summary>
    /// EINE Schicht eines Bauteilaufbaus — eine Zeile aus <c>Tab_Bauteilschicht(_STAMM)</c>
    /// (Schemaschritt S-B). Innen → außen, <see cref="Reihenfolge"/> lückenlos ab 1.
    ///
    /// <para><b>Die Stoffwerte sind eine KOPIE</b> zum Zeitpunkt der Zuordnung: Eine spätere
    /// Katalogänderung verschiebt kein gerechnetes Ergebnis rückwirkend.
    /// <see cref="ID_Baustoff"/> <c>null</c> heißt freie Eingabe.</para>
    /// </summary>
    public class BauteilschichtModel
    {
        /// <summary>Primärschlüssel; 0 = noch nicht gespeichert.</summary>
        public int ID;

        /// <summary>Der Aufbau der Schicht (Eltern, Kaskade).</summary>
        public int ID_Aufbau;

        /// <summary>Lage innen → außen, lückenlos ab 1 — vergibt der Schreibweg.</summary>
        public int Reihenfolge;

        /// <summary>Stoff der Schicht (je Seite die eigene Ablage, W11); <c>null</c> = freie Eingabe.</summary>
        public int? ID_Baustoff;

        /// <summary>Schichtdicke [m] (Pflicht, größer als null).</summary>
        public double Dicke;

        /// <summary>Ruhende Luftschicht — Widerstand nach DIN EN ISO 6946.</summary>
        public bool IstLuftschicht;

        /// <summary>Wärmeleitfähigkeit [W/(m·K)] — Kopie; <c>null</c> = nicht angegeben.</summary>
        public double? Lambda;

        /// <summary>Rohdichte [kg/m³] — Kopie; <c>null</c> = nicht angegeben.</summary>
        public double? Rho;

        /// <summary>Spezifische Wärmekapazität [J/(kg·K)] — Kopie; <c>null</c> = nicht angegeben.</summary>
        public double? Cp;

        /// <summary>Eine entkoppelte Kopie.</summary>
        public BauteilschichtModel Kopie() => (BauteilschichtModel)MemberwiseClone();
    }
}
