using System;
using System.Collections.Generic;

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

        // =====================================================================
        //  Stufe G1 — was der VDI-Weg zusätzlich liest (Umsetzungskonzept 1.2)
        // =====================================================================

        /// <summary>
        /// Die ganze Solarreihe der Klimaregion in Ortszeit, wie sie
        /// <c>SolardatenCtrl.ReadOrtszeit</c> liefert — mit der UTC-Herkunft an jeder Zeile
        /// (Umsetzungskonzept 1.2 Zeile 2). <c>null</c>, solange nicht gelesen.
        /// </summary>
        internal IReadOnlyList<SolardatenModel> SolarOrtszeit { get; set; }

        /// <summary>Längengrad der Klimaregion [°]; NaN = nicht gelesen.</summary>
        internal double Laengengrad { get; set; } = double.NaN;

        /// <summary>Breitengrad der Klimaregion [°]; NaN = nicht gelesen.</summary>
        internal double Breitengrad { get; set; } = double.NaN;

        /// <summary>Referenzjahr der Zeitbasis (<c>SolardatenCtrl.Referenzjahr</c>); 0 = nicht gelesen.</summary>
        internal int Referenzjahr { get; set; }

        /// <summary>
        /// Die Wochenendmaske auf dem <b>Ortszeit-Kalender</b> (Entscheid U7, E27; F-Ü8):
        /// Samstag und Sonntag ab dem 1. Januar des <see cref="Referenzjahr"/>es, 365 Tage.
        /// Sie gehört dem VDI-Weg; der Tagesbilanz-Weg liest weiter <see cref="WE"/> aus
        /// <c>Tab_Klimadaten</c>. <c>null</c>, solange nicht gebildet.
        /// </summary>
        internal bool[] WochenendeOrtszeit { get; set; }

        /// <summary>
        /// Die Probe der Wochenendmaske gegen <see cref="WE"/> (<c>Tab_Klimadaten.WE</c>
        /// derselben Klimaregion): Zahl der Tage, an denen beide verschieden sind. Eine
        /// Abweichung ist ein Befund der Probe, kein Rechenfehler (Umsetzungskonzept 1.2).
        /// </summary>
        internal int WochenendProbeAbweichungen { get; set; }

        /// <summary>
        /// Bildet die Wochenendmaske eines Jahres: Tag d (0 … 364) ist Wochenende, wenn der
        /// d-te Tag nach dem 1. Januar von <paramref name="jahr"/> ein Samstag oder Sonntag
        /// ist. Ohne Datenbank, ohne Uhr.
        /// </summary>
        internal static bool[] WochenendmaskeBilden(int jahr)
        {
            if (jahr < 1 || jahr > 9998) throw new ArgumentOutOfRangeException(nameof(jahr));
            var maske = new bool[365];
            DateTime jan1 = new DateTime(jahr, 1, 1);
            for (int d = 0; d < 365; d++)
            {
                DayOfWeek w = jan1.AddDays(d).DayOfWeek;
                maske[d] = w == DayOfWeek.Saturday || w == DayOfWeek.Sunday;
            }
            return maske;
        }

        /// <summary>Zahl der Tage, an denen zwei Wochenendmasken verschieden sind.</summary>
        internal static int Abweichungen(bool[] a, bool[] b)
        {
            if (a == null || b == null) return -1;
            int n = Math.Min(a.Length, b.Length), k = 0;
            for (int d = 0; d < n; d++) if (a[d] != b[d]) k++;
            return k;
        }
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
