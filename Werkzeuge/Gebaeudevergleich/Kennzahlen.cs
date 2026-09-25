using System;
using System.Linq;

namespace Gebaeudevergleich
{
    /// <summary>
    /// <b>Die Kennzahlen, die der Controller nicht schon liefert</b> — reine Funktionen über die
    /// Stundenreihe [kW]. Jahreswärme, Spitze, größtes 24-h-Mittel, Q95, Vollbenutzungsstunden
    /// und Monatswerte kommen aus <c>GebaeudeBedarfErgebnis</c> (dieselbe Stelle wie Dialog und
    /// Bericht); hier stehen Nachtanteil, Heizstunden, Katalog- und Verbrauchstreffer und Δ.
    ///
    /// <para><b>Die Spitzen sind Spitzen der Stundenreihe, keine Normheizlast</b> — der Bericht
    /// sagt das an jeder Stelle, an der sie stehen.</para>
    /// </summary>
    internal static class Kennzahlen
    {
        /// <summary>Erste Nachtstunde (22 Uhr) — Nachtanteil 22–6 Uhr.</summary>
        internal const int NACHT_BEGINN_H = 22;

        /// <summary>Erste Tagstunde (6 Uhr).</summary>
        internal const int NACHT_ENDE_H = 6;

        /// <summary>Eine Stunde zählt als Heizstunde, wenn ihre Last diesen Anteil der Spitze übersteigt (0,1 %).</summary>
        internal const double HEIZSTUNDE_ANTEIL_DER_SPITZE = 0.001;

        /// <summary>Anteil der Wärme zwischen 22 und 6 Uhr an der Jahreswärme [%]; Stunde 0 = 0–1 Uhr des 1. Januar.</summary>
        internal static double NachtanteilProzent(double[] stundenKw)
        {
            if (stundenKw == null || stundenKw.Length == 0) return double.NaN;
            double summe = 0.0, nacht = 0.0;
            for (int h = 0; h < stundenKw.Length; h++)
            {
                summe += stundenKw[h];
                int uhr = h % 24;
                if (uhr >= NACHT_BEGINN_H || uhr < NACHT_ENDE_H) nacht += stundenKw[h];
            }
            return summe > 0.0 ? nacht / summe * 100.0 : double.NaN;
        }

        /// <summary>Stunden mit Last &gt; 0,1 % der Spitze.</summary>
        internal static int Heizstunden(double[] stundenKw)
        {
            if (stundenKw == null || stundenKw.Length == 0) return 0;
            double spitze = stundenKw.Max();
            if (!(spitze > 0.0)) return 0;
            double schwelle = HEIZSTUNDE_ANTEIL_DER_SPITZE * spitze;
            return stundenKw.Count(w => w > schwelle);
        }

        /// <summary>Enthält die Reihe einen nicht endlichen Wert?</summary>
        internal static bool NichtEndlich(double[] reihe)
            => reihe != null && reihe.Any(w => double.IsNaN(w) || double.IsInfinity(w));

        /// <summary>
        /// Katalogtreffer [%] = Jahreswärme / (spez · Bezugsfläche). Die Bezugsfläche ist
        /// <c>Z_AuswahlWohnflaeche</c>, nicht die Nutzfläche (Konzept 5.5: 1007 mit 340 m² bei
        /// 74 m² Nutzfläche). Nur bei Flächenangabe und <c>spez &gt; 0</c>; sonst <c>null</c>.
        /// </summary>
        internal static double? KatalogtrefferProzent(double jahrMwh, bool istFlaeche, double spezKwhM2, double bezugsflaecheM2)
        {
            if (!istFlaeche || !(spezKwhM2 > 0.0) || !(bezugsflaecheM2 > 0.0) || !Endlich(jahrMwh)) return null;
            return jahrMwh * 1000.0 / (spezKwhM2 * bezugsflaecheM2) * 100.0;
        }

        /// <summary>Verbrauchstreffer [%] = Jahreswärme / angegebener Verbrauch; nur Verbrauchsangabe.</summary>
        internal static double? VerbrauchstrefferProzent(double jahrMwh, bool istFlaeche, double verbrauchKwh)
        {
            if (istFlaeche || !(verbrauchKwh > 0.0) || !Endlich(jahrMwh)) return null;
            return jahrMwh * 1000.0 / verbrauchKwh * 100.0;
        }

        /// <summary>Δ absolut = neu − alt; <c>NaN</c>, wenn eine Seite fehlt.</summary>
        internal static double Delta(double alt, double neu)
            => Endlich(alt) && Endlich(neu) ? neu - alt : double.NaN;

        /// <summary>Δ relativ [%] = (neu / alt − 1) · 100; <c>NaN</c> bei alt = 0 oder fehlender Seite.</summary>
        internal static double DeltaProzent(double alt, double neu)
            => Endlich(alt) && Endlich(neu) && alt != 0.0 ? (neu / alt - 1.0) * 100.0 : double.NaN;

        internal static bool Endlich(double w) => !double.IsNaN(w) && !double.IsInfinity(w);

        /// <summary>Summe zweier Stundenreihen gleicher Länge (Projekt-Spitze aus addierten Reihen).</summary>
        internal static void Addieren(double[] ziel, double[] reihe)
        {
            if (reihe == null) return;
            for (int h = 0; h < Math.Min(ziel.Length, reihe.Length); h++) ziel[h] += reihe[h];
        }
    }
}
