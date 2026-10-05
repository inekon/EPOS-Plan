using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Raumtemperatur der Datei als Heizsollwert</b> — die optionale Übernahme auf Wunsch des Anwenders
    /// (Schalter „Raumtemperatur der Datei als Heizsollwert übernehmen" im Zuordnungsdialog, Vorgabe aus).
    ///
    /// <para>Der Heizsollwert eines importierten Gebäudes und seiner Zonen ist sonst die Vorgabe (Normtemperatur)
    /// oder die Eingabe des Anwenders; der Zonenname trägt keinen Sollwert. Trägt die Datei keinen
    /// Norm-Sollwert (<see cref="AbbildRaum.SollHeizenC"/>), aber für beheizte Räume eine CAD-Raumtemperatur
    /// (<see cref="AbbildRaum.RaumtemperaturC"/>), kann der Anwender sie übernehmen: der Tagsollwert des Gebäudes
    /// und jeder beheizten Zone ist dann das flächengewichtete Mittel der Raumtemperaturen ihrer beheizten Räume,
    /// gerundet auf 0,1 °C. Räume ohne Temperatur bleiben außen vor und werden gezählt.</para>
    ///
    /// <para>Ohne Datenbank, ohne Zustand, deterministisch (Räume in Dateireihenfolge).</para>
    /// </summary>
    internal static class GebaeudeCadSollwert
    {
        /// <summary>Über dieser Spanne [K] der gemittelten Raumtemperaturen steht ein Hinweis.</summary>
        internal const double SPANNE_GRENZE_K = 2.0;

        /// <summary>Der Meldungsname des Belegs je Zone (mit dem Präfix des Formats).</summary>
        internal const string ZONE_SOLLWERT_CAD = "ZONE_SOLLWERT_CAD";

        /// <summary>Der Meldungsname des Hinweises auf eine große Spanne (mit dem Präfix des Formats).</summary>
        internal const string SOLLWERT_CAD_SPANNE = "SOLLWERT_CAD_SPANNE";

        /// <summary>Der Belegschlüssel des Tagsollwerts aus den Raumtemperaturen.</summary>
        internal const string BELEG = "GIMP_BELEG_SOLLWERT_CAD";

        /// <summary>Das Mittel einer Raumgruppe.</summary>
        /// <param name="Wert">Das flächengewichtete Mittel [°C], gerundet auf 0,1 °C.</param>
        /// <param name="Raeume">Die Zahl der Räume mit Temperatur.</param>
        /// <param name="OhneTemperatur">Die Zahl der Räume ohne Temperatur (außen vor).</param>
        /// <param name="MinC">Die kleinste Raumtemperatur [°C].</param>
        /// <param name="MaxC">Die größte Raumtemperatur [°C].</param>
        internal readonly record struct Mittel(double Wert, int Raeume, int OhneTemperatur, double MinC, double MaxC)
        {
            /// <summary>Liegt die Spanne über <see cref="SPANNE_GRENZE_K"/>?</summary>
            internal bool SpanneGross => MaxC - MinC > SPANNE_GRENZE_K + 1e-9;
        }

        /// <summary>Die gültige Raumtemperatur eines Raums; <c>null</c> = keine.</summary>
        internal static double? Temperatur(AbbildRaum r)
            => r?.RaumtemperaturC is double t && !double.IsNaN(t) && !double.IsInfinity(t) ? t : (double?)null;

        /// <summary>
        /// Lässt sich der Schalter anbieten? Mindestens ein beheizter Raum trägt eine CAD-Raumtemperatur und kein
        /// beheizter Raum einen Norm-Sollwert — sonst gilt der Weg über <see cref="AbbildRaum.SollHeizenC"/>.
        /// </summary>
        internal static bool Moeglich(IEnumerable<AbbildRaum> beheizt)
        {
            List<AbbildRaum> liste = beheizt?.Where(r => r != null).ToList() ?? new List<AbbildRaum>();
            return liste.Any(r => Temperatur(r).HasValue) && !liste.Any(r => r.SollHeizenC.HasValue);
        }

        /// <summary>
        /// Das flächengewichtete Mittel der Raumtemperaturen; Gewicht ist die Raumfläche, trägt kein Raum mit
        /// Temperatur eine Fläche, das ungewichtete Mittel. <c>null</c>, wenn kein Raum eine Temperatur trägt.
        /// </summary>
        internal static Mittel? Bilden(IEnumerable<AbbildRaum> raeume)
        {
            List<AbbildRaum> liste = raeume?.Where(r => r != null).ToList() ?? new List<AbbildRaum>();
            var mit = liste.Select(r => (T: Temperatur(r), A: r.FlaecheM2 > 0.0 ? r.FlaecheM2.Value : 0.0))
                           .Where(x => x.T.HasValue).Select(x => (T: x.T.Value, x.A)).ToList();
            if (mit.Count == 0) return null;
            double gewicht = mit.Sum(x => x.A);
            double mittel = gewicht > 0.0 ? mit.Sum(x => x.T * x.A) / gewicht : mit.Average(x => x.T);
            return new Mittel(Math.Round(mittel, 1, MidpointRounding.AwayFromZero), mit.Count, liste.Count - mit.Count,
                              mit.Min(x => x.T), mit.Max(x => x.T));
        }
    }
}
