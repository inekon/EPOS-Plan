using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ZapfprofilValidierung
{
    /// <summary>
    /// <b>Das vierte Kriterium der Stufe Z5 — die √N-Skalierung der Überschätzung</b> (Kapitel 7
    /// Zeile Z5). Es ist <b>objektübergreifend</b>: Die Spitze je Einheit fällt mit der Zahl der
    /// Einheiten wie 1/√N (Gleichzeitigkeit, 4.4). Geprüft wird deshalb, wie das gemessene
    /// Spitzenverhältnis mit N läuft:
    ///
    /// <code>
    /// ln(Spitzenverhaeltnis) = a + b · ln(N)      Ausgleichsgerade über alle Objekte
    /// b = −0,5  ⇔  die Überschätzung folgt genau dem 1/√N-Gesetz
    /// </code>
    ///
    /// <para><b>Die Schranke ± 0,25 ist eine numerische Setzung des Werkzeugs</b>, keine Fachgrenze:
    /// Das Konzept nennt für die Steigung kein Band. Genommen ist die halbe Strecke zwischen „kein
    /// Zusammenhang" (b = 0) und „doppelt so steil" (b = −1); sie steht in jedem Bericht, damit
    /// niemand sie für eine Norm nimmt. Unter drei auswertbaren Objekten mit verschiedenem N ist die
    /// Steigung keine Aussage — dann bleibt das Kriterium <b>gelb</b>, nie grün.</para>
    /// </summary>
    internal static class Sammelkriterium
    {
        /// <summary>Die erwartete Steigung: −0,5 (1/√N).</summary>
        internal const double STEIGUNG_SOLL = -0.5;

        /// <summary>Wie weit die Steigung abweichen darf — numerische Setzung des Werkzeugs.</summary>
        internal const double STEIGUNG_BAND = 0.25;

        /// <summary>Wie viele auswertbare Objekte mit verschiedenem N die Steigung braucht.</summary>
        internal const int MINDESTENS_OBJEKTE = 3;

        /// <summary>
        /// Bildet das Kriterium über alle Befunde. Ergebnis ist ein <see cref="Kriterium"/> wie die
        /// der Objekte; <c>Mass</c> ist die Steigung.
        /// </summary>
        internal static Kriterium Bilden(IReadOnlyList<Objektbefund> befunde)
        {
            var punkte = (befunde ?? new Objektbefund[0])
                .Where(b => b.Abbruch == null && b.Einheiten > 0 && b.Spitzenverhaeltnis is > 0.0 && b.EinheitenBelastbar)
                .Select(b => (X: Math.Log(b.Einheiten), Y: Math.Log(b.Spitzenverhaeltnis.Value)))
                .ToList();
            int ausgelassen = (befunde ?? new Objektbefund[0])
                .Count(b => b.Abbruch == null && !b.EinheitenBelastbar);
            string ohne = ausgelassen == 0 ? ""
                : " " + ausgelassen.ToString(CultureInfo.InvariantCulture)
                  + " Objekte mit Platzhalter- oder unbekannter Bezugsmenge tragen nicht bei.";
            string band = STEIGUNG_SOLL.ToString("0.##", CultureInfo.InvariantCulture) + " ± "
                          + STEIGUNG_BAND.ToString("0.##", CultureInfo.InvariantCulture);

            int verschieden = punkte.Select(p => Math.Round(p.X, 9)).Distinct().Count();
            if (punkte.Count < MINDESTENS_OBJEKTE || verschieden < 2)
                return new Kriterium("Wurzel-N-Skalierung (über alle Objekte)", Ampel.Gelb, null, band,
                    "Die Steigung braucht mindestens " + MINDESTENS_OBJEKTE.ToString(CultureInfo.InvariantCulture)
                    + " auswertbare Objekte mit verschiedener Einheitenzahl; vorhanden sind "
                    + punkte.Count.ToString(CultureInfo.InvariantCulture) + " mit "
                    + verschieden.ToString(CultureInfo.InvariantCulture) + " verschiedenen." + ohne);

            double xm = punkte.Average(p => p.X);
            double ym = punkte.Average(p => p.Y);
            double sxy = punkte.Sum(p => (p.X - xm) * (p.Y - ym));
            double sxx = punkte.Sum(p => (p.X - xm) * (p.X - xm));
            if (!(sxx > 0.0))
                return new Kriterium("Wurzel-N-Skalierung (über alle Objekte)", Ampel.Gelb, null, band,
                    "Alle Objekte haben dieselbe Einheitenzahl - die Steigung ist nicht bildbar.");

            double b2 = sxy / sxx;
            Ampel ampel = Math.Abs(b2 - STEIGUNG_SOLL) <= STEIGUNG_BAND ? Ampel.Gruen : Ampel.Rot;
            return new Kriterium("Wurzel-N-Skalierung (über alle Objekte)", ampel, b2, band,
                "Steigung der Ausgleichsgeraden von ln(Spitzenverhaeltnis) ueber ln(N) aus "
                + punkte.Count.ToString(CultureInfo.InvariantCulture) + " Objekten; -0,5 heisst: "
                + "Die Ueberschaetzung folgt dem 1/Wurzel-N-Gesetz." + ohne);
        }

        /// <summary>
        /// Die Steigung über <b>alle</b> auswertbaren Objekte, auch mit Platzhaltermengen — nur zum
        /// Vergleich im Bericht (so rechnete der erste Lauf); <c>null</c> = nicht bildbar.
        /// </summary>
        internal static (double? Steigung, int Objekte) SteigungAlle(IReadOnlyList<Objektbefund> befunde)
        {
            var punkte = (befunde ?? new Objektbefund[0])
                .Where(b => b.Abbruch == null && b.Einheiten > 0 && b.Spitzenverhaeltnis is > 0.0)
                .Select(b => (X: Math.Log(b.Einheiten), Y: Math.Log(b.Spitzenverhaeltnis.Value)))
                .ToList();
            if (punkte.Count < MINDESTENS_OBJEKTE) return (null, punkte.Count);
            double xm = punkte.Average(p => p.X);
            double ym = punkte.Average(p => p.Y);
            double sxx = punkte.Sum(p => (p.X - xm) * (p.X - xm));
            if (!(sxx > 0.0)) return (null, punkte.Count);
            return (punkte.Sum(p => (p.X - xm) * (p.Y - ym)) / sxx, punkte.Count);
        }
    }
}
