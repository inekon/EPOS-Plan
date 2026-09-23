namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Klimakalender eines Laufs, in zwei Teilen</b> (Umsetzungskonzept
    /// Gebäudesimulation 1.1 Punkt 2, F-Ü3; Softwarearchitektur 1.3). Gelesen wird er einmal je
    /// Lauf in <see cref="SimulationWaermebedarf.KlimakalenderLesen"/>, Anweisung für Anweisung
    /// wie zuvor; er ist Teil des modellfreien Vorbereitungsschritts
    /// (<see cref="GebaeudeVorbereitung"/>).
    ///
    /// <para>Die Weiche reicht dem VDI-Weg allein <see cref="Gemeinsam"/>, dem Tagesbilanz-Weg
    /// beide Teile — den <see cref="Altweg"/>-Teil beim Aufbau je Lauf. Der Altweg-Teil fällt
    /// mit der Stufe GA (Löschliste 6.1).</para>
    /// </summary>
    internal sealed class Klimakalender
    {
        /// <summary>Was beide Rechenwege lesen dürfen.</summary>
        internal KlimakalenderGemeinsam Gemeinsam { get; }

        /// <summary>Was allein der Tagesbilanz-Weg liest.</summary>
        internal KlimakalenderAltweg Altweg { get; }

        internal Klimakalender(KlimakalenderGemeinsam gemeinsam, KlimakalenderAltweg altweg)
        {
            Gemeinsam = gemeinsam;
            Altweg = altweg;
        }
    }

    /// <summary>
    /// Der gemeinsame Teil des Klimakalenders: die Wochenendtage <c>WE[365]</c>, die
    /// Stundentemperaturen <c>[8760]</c> (Ortszeit), der Wochentag des 1. Januar und die
    /// Monatsgrenzen. Die Felder sind Referenzen auf die Felder der Fassade, die der Lauf
    /// ohnehin führt; gefüllt wird in place.
    /// </summary>
    internal sealed class KlimakalenderGemeinsam
    {
        internal KlimakalenderGemeinsam(bool[] we, double[] stundentemperatur, int[] moAnfang, int[] moEnde)
        {
            WE = we;
            Stundentemperatur = stundentemperatur;
            MoAnfang = moAnfang;
            MoEnde = moEnde;
        }

        /// <summary>Wochenendtag je Tag des Jahres (365).</summary>
        internal bool[] WE { get; }

        /// <summary>Außentemperatur je Stunde in °C (8 760, Ortszeit).</summary>
        internal double[] Stundentemperatur { get; }

        /// <summary>Wochentag des 1. Januar, Montag = 0 … Sonntag = 6.</summary>
        internal int WochentagJan1 { get; set; } = ProfilBedarf.WOCHENTAG_ALTKONVENTION;

        /// <summary>Erste Stunde je Monat (12).</summary>
        internal int[] MoAnfang { get; }

        /// <summary>Letzte Stunde je Monat, einschließlich (12).</summary>
        internal int[] MoEnde { get; }
    }

    /// <summary>
    /// Der Altweg-Teil des Klimakalenders: die isotropen Tagesmittel der Einstrahlung je
    /// Himmelsrichtung, die Tagesmitteltemperatur und die Tagestypen (Wohngebäude,
    /// Nichtwohngebäude) — was allein die Tagesbilanz braucht.
    /// </summary>
    internal sealed class KlimakalenderAltweg
    {
        internal readonly double[] Sol_N = new double[365];
        internal readonly double[] Sol_w = new double[365];
        internal readonly double[] Sol_O = new double[365];
        internal readonly double[] Sol_S = new double[365];
        internal readonly double[] A_Temp = new double[365];
        internal readonly int[] TagTyp_W = new int[365];
        internal readonly int[] TagTyp_NW = new int[365];
    }
}
