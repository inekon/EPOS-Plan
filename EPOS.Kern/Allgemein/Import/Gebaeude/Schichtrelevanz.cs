using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>Warum eine Schicht beim Import aus dem Aufbau fällt (<see cref="Schichtrelevanz"/>).</summary>
    internal enum Schichtrelevanzgrund
    {
        /// <summary>Regel (a): Dicke ≤ 5 mm und je unter 2 % an Σ d/λ und an Σ ρ·c·d.</summary>
        Duenn = 1,

        /// <summary>Regel (b): Folie, Sperre oder Abdichtung (Stoffgruppe „Abdichtungen"), unter 2 % an Σ d/λ.</summary>
        Sperre = 2,
    }

    /// <summary>Eine weggelassene Schicht: Stelle in der übergebenen Folge, Grund und die beiden Anteile [–].</summary>
    internal readonly record struct SchichtrelevanzBefund(int Stelle, Schichtrelevanzgrund Grund, double AnteilR, double AnteilC);

    /// <summary>
    /// <b>Die Relevanzregel für Schichten</b> (Konzept Bauteilaufbau beim Import 5.1, E95-2) — die eine Stelle
    /// im Kern, die entscheidet, welche Schicht der Datei für die Simulation nicht zählt:
    /// <list type="bullet">
    /// <item>(a) Dicke ≤ <see cref="DICKE_GRENZE_M"/> <b>und</b> Anteil unter <see cref="ANTEIL_GRENZE"/> an
    /// Σ d/λ <b>und</b> Anteil unter <see cref="ANTEIL_GRENZE"/> an Σ ρ·c·d;</item>
    /// <item>(b) Folie, Sperre oder Abdichtung, unabhängig von Dicke und Kapazität, solange ihr Anteil an
    /// Σ d/λ unter <see cref="ANTEIL_GRENZE"/> bleibt. (Der Sonderfall „kein Stoff" — Schraffur, leer — fällt
    /// schon im Namensabgleich weg.)</item>
    /// </list>
    /// Die Anteile beziehen sich auf die vollständige Folge vor dem Weglassen. Eine Dämmschicht oder eine
    /// speichernde Schicht kann die Regel nicht treffen, weil sie über einem der beiden Anteile liegt. Die Regel
    /// wirkt <b>nur im Import</b> beim Bilden des vorgeschlagenen Aufbaus — nie auf gespeicherte Aufbauten oder
    /// den Katalog — und verwirft nichts still: Jede weggelassene Schicht wird am Aufbau und im Protokoll benannt.
    /// </summary>
    internal static class Schichtrelevanz
    {
        /// <summary>Dickengrenze der Regel (a) [m] — 5 mm, einschließlich.</summary>
        internal const double DICKE_GRENZE_M = 0.005;

        /// <summary>Anteilsgrenze an R und an C [–] — 2 %, ausschließlich.</summary>
        internal const double ANTEIL_GRENZE = 0.02;

        /// <summary>Die Stoffgruppe des Katalogs, deren Schichten als Folie, Sperre oder Abdichtung gelten.</summary>
        internal const string GRUPPE_ABDICHTUNGEN = "Abdichtungen";

        /// <summary>Rundungsspiel der Dickengrenze [m], damit 5 mm aus der Datei (0,005 als Gleitkommazahl) noch zählen.</summary>
        private const double DICKE_SPIEL_M = 1e-9;

        /// <summary>
        /// Die Schichten, die weggelassen werden — in der Folge der Eingabe. <paramref name="sperre"/> trägt je
        /// Schicht, ob sie als Folie, Sperre oder Abdichtung erkannt ist (<c>null</c> = keine). Eine ruhende
        /// Luftschicht zählt mit ihrem Widerstand nach Tabelle 8 (waagerechter Wärmestrom) und ohne Kapazität.
        /// Die Regel läuft vor der Bandprüfung (<see cref="Bauteilreduktion.Pruefen"/>), damit eine Folie unter der
        /// kleinsten Schichtdicke nicht den ganzen Aufbau verwirft; ist ein Widerstand oder eine Kapazität nicht
        /// bestimmbar (λ ≤ 0, Luftschicht über 0,3 m …), wird nichts weggelassen und die Bandprüfung entscheidet.
        /// Bleibt nach der Regel keine Schicht übrig, wird ebenfalls nichts weggelassen.
        /// </summary>
        internal static IReadOnlyList<SchichtrelevanzBefund> Unerheblich(IReadOnlyList<Schicht> schichten, IReadOnlyList<bool> sperre = null)
        {
            if (schichten == null) throw new ArgumentNullException(nameof(schichten));
            if (sperre != null && sperre.Count != schichten.Count)
                throw new ArgumentException("Je Schicht ein Sperrkennzeichen.", nameof(sperre));
            int n = schichten.Count;
            var r = new double[n];
            var c = new double[n];
            double summeR = 0.0, summeC = 0.0;
            for (int i = 0; i < n; i++)
            {
                Schicht s = schichten[i];
                if (!(s.Dicke_M > 0.0) || double.IsInfinity(s.Dicke_M)) return Array.Empty<SchichtrelevanzBefund>();
                try
                {
                    r[i] = Bauteilreduktion.Waermedurchlasswiderstand(s, Waermestromrichtung.Horizontal);
                }
                catch (GebaeudeModellException)
                {
                    return Array.Empty<SchichtrelevanzBefund>();
                }
                if (!(r[i] > 0.0) || double.IsInfinity(r[i])) return Array.Empty<SchichtrelevanzBefund>();
                c[i] = Bauteilreduktion.FlaechenbezogeneKapazitaet(s);
                if (double.IsNaN(c[i]) || c[i] < 0.0) c[i] = 0.0;
                if (double.IsInfinity(c[i])) return Array.Empty<SchichtrelevanzBefund>();
                summeR += r[i];
                summeC += c[i];
            }

            var befunde = new List<SchichtrelevanzBefund>();
            for (int i = 0; i < n; i++)
            {
                double anteilR = summeR > 0.0 ? r[i] / summeR : 0.0;
                double anteilC = summeC > 0.0 ? c[i] / summeC : 0.0;
                if (!(anteilR < ANTEIL_GRENZE)) continue;
                if (sperre != null && sperre[i])
                    befunde.Add(new SchichtrelevanzBefund(i, Schichtrelevanzgrund.Sperre, anteilR, anteilC));
                else if (schichten[i].Dicke_M <= DICKE_GRENZE_M + DICKE_SPIEL_M && anteilC < ANTEIL_GRENZE)
                    befunde.Add(new SchichtrelevanzBefund(i, Schichtrelevanzgrund.Duenn, anteilR, anteilC));
            }
            if (befunde.Count == n) befunde.Clear();
            return befunde;
        }
    }
}
