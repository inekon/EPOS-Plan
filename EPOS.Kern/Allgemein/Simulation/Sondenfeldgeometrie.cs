using System;

namespace WindowsFormsApplication1
{
    /// <summary>Anordnung der Sonden eines Feldes (Konzept Simulationsablauf 23.3).</summary>
    public enum Sondenanordnung
    {
        /// <summary>Möglichst quadratisches Raster, zeilenweise gefüllt.</summary>
        Quadratisch = 0,
    }

    /// <summary>
    /// <b>Geometrie und Bohrlochkennwerte eines Sondenfeldes</b> (Konzept Simulationsablauf 23.3) —
    /// was das Feld außer Länge, Zahl, Boden und ungestörter Temperatur braucht. Die Vorgabe
    /// <see cref="Norm"/> trägt die Bezugswerte der Tabelle B2 nach VDI 4640 Blatt 2. Jeder Wert ist
    /// so abgelegt, dass er später je Anlage aus <c>Tab_Energieanlagen</c> kommen kann (vorgesehene
    /// Spalte in Klammern); bis dahin rechnet jedes Feld mit der Norm.
    /// </summary>
    public sealed class Sondenfeldgeometrie
    {
        /// <summary>Sondenabstand B [m] (vorgesehen: <c>WQ_Sondenabstand</c> REAL, Vorgabe 6,0).</summary>
        public double AbstandM { get; set; } = Erdsondenfeld.SONDENABSTAND_M;

        /// <summary>Bohrlochradius r_b [m] (vorgesehen: <c>WQ_Bohrlochdurchmesser</c> REAL in mm, Vorgabe 150).</summary>
        public double BohrlochradiusM { get; set; } = Erdsondenfeld.BOHRLOCHRADIUS_M;

        /// <summary>Bohrlochwiderstand R_b [m·K/W] (vorgesehen: <c>WQ_Bohrlochwiderstand</c> REAL, Vorgabe 0,10).</summary>
        public double Bohrlochwiderstand { get; set; } = Erdsondenfeld.BOHRLOCHWIDERSTAND;

        /// <summary>Kopfüberdeckung D [m] (vorgesehen: <c>WQ_Kopfueberdeckung</c> REAL, Vorgabe 2,0).</summary>
        public double KopfueberdeckungM { get; set; } = Erdsondenfeld.KOPFUEBERDECKUNG_M;

        /// <summary>
        /// Betrachtungsjahr n ≥ 1: Das Rechenjahr folgt auf n − 1 Vorjahre (vorgesehen:
        /// <c>WQ_Betrachtungsjahr</c> INTEGER, Vorgabe 10).
        /// </summary>
        public int Betrachtungsjahr { get; set; } = Erdsondenfeld.BETRACHTUNGSJAHR;

        /// <summary>Anordnung der Sonden (vorgesehen: <c>WQ_Sondenanordnung</c> TEXT, Vorgabe „Quadratisch").</summary>
        public Sondenanordnung Anordnung { get; set; } = Sondenanordnung.Quadratisch;

        /// <summary>Die Bezugswerte nach VDI 4640 Blatt 2, Tabelle B2 (eine neue Instanz je Aufruf).</summary>
        public static Sondenfeldgeometrie Norm { get { return new Sondenfeldgeometrie(); } }

        /// <summary>
        /// Eine Kopie, in der jeder unbrauchbare Wert (nicht endlich, nicht positiv, bei Kopfüberdeckung
        /// und Bohrlochwiderstand negativ, Betrachtungsjahr unter 1) durch die Norm ersetzt ist.
        /// </summary>
        public Sondenfeldgeometrie Bereinigt()
        {
            var n = Norm;
            return new Sondenfeldgeometrie
            {
                AbstandM = Positiv(AbstandM) ? AbstandM : n.AbstandM,
                BohrlochradiusM = Positiv(BohrlochradiusM) ? BohrlochradiusM : n.BohrlochradiusM,
                Bohrlochwiderstand = NichtNegativ(Bohrlochwiderstand) ? Bohrlochwiderstand : n.Bohrlochwiderstand,
                KopfueberdeckungM = NichtNegativ(KopfueberdeckungM) ? KopfueberdeckungM : n.KopfueberdeckungM,
                Betrachtungsjahr = Betrachtungsjahr >= 1 ? Betrachtungsjahr : n.Betrachtungsjahr,
                Anordnung = Anordnung,
            };
        }

        private static bool Positiv(double x) { return double.IsFinite(x) && x > 0; }

        private static bool NichtNegativ(double x) { return double.IsFinite(x) && x >= 0; }
    }
}
