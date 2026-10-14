using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Woher der Nordwinkel eines Imports stammt</b> (Abstimmungspapier G5, Abschnitt 7, N6): aus der Datei, aus der
    /// Eingabe des Anwenders oder als Annahme „Planoberseite = Nord“ (Nordwinkel 0°). Gespeichert an der Quelle in
    /// <c>Tab_Importquelle.Nordwinkel_Herkunft</c> (<see cref="NordrichtungSchema"/>); die Werte stehen in
    /// <see cref="NordwinkelherkunftWerte"/>.
    /// </summary>
    public enum Nordwinkelherkunft
    {
        /// <summary>Die Datei nennt keine Nordrichtung, und niemand hat eine eingegeben: Planoberseite = Nord.</summary>
        Annahme = 0,

        /// <summary>Die Datei nennt die Nordrichtung, und sie ist angewandt.</summary>
        Datei = 1,

        /// <summary>Der Anwender hat die Richtung der Planoberseite eingegeben.</summary>
        Eingabe = 2,
    }

    /// <summary>
    /// <b>Die gespeicherten Werte der Nordwinkelherkunft</b> — die EINE Quelle der Wertliste für Schemaschritt
    /// (<see cref="NordrichtungSchema.WERTE"/>), Schreiben und Lesen.
    /// </summary>
    internal static class NordwinkelherkunftWerte
    {
        internal const string ANNAHME = "ANNAHME";
        internal const string DATEI = "DATEI";
        internal const string EINGABE = "EINGABE";

        /// <summary>Der gespeicherte Wert einer Herkunft.</summary>
        internal static string Wert(Nordwinkelherkunft h) => h switch
        {
            Nordwinkelherkunft.Datei => DATEI,
            Nordwinkelherkunft.Eingabe => EINGABE,
            _ => ANNAHME,
        };

        /// <summary>Die Herkunft eines gespeicherten Werts; <c>null</c> = leer oder unbekannt.</summary>
        internal static Nordwinkelherkunft? Aus(string wert) => (wert ?? "").Trim() switch
        {
            DATEI => Nordwinkelherkunft.Datei,
            EINGABE => Nordwinkelherkunft.Eingabe,
            ANNAHME => Nordwinkelherkunft.Annahme,
            _ => null,
        };
    }

    /// <summary>
    /// <b>Die Nordrichtung eines Gebäudeimports</b> (G5, Abschnitt 7): Umrechnung zwischen der Eingabe „Wohin zeigt die
    /// Planoberseite (+y der Datei)?“ und dem Nordwinkel des Modells, dazu die Schnellwahl der acht Himmelsrichtungen.
    ///
    /// <para><b>Beziehung (N1):</b> Wahrer Azimut = Modellazimut − Nordwinkel; Azimut 0° = Nord, im Uhrzeigersinn, +y des
    /// Modells hat den Modellazimut 0°. Zeigt die Planoberseite nach α, ist der Nordwinkel (360° − α) mod 360° — und
    /// umgekehrt, die Umrechnung ist ihre eigene Umkehrung. Beide Werte liegen normiert in [0, 360).</para>
    /// </summary>
    public static class Nordrichtung
    {
        /// <summary>Die Schnellwahl: Kürzel und Richtung der Planoberseite [°] — N, NO, O, SO, S, SW, W, NW.</summary>
        public static readonly IReadOnlyList<(string Kuerzel, double PlanoberseiteGrad)> Schnellwahl = new[]
        {
            ("N", 0.0), ("NO", 45.0), ("O", 90.0), ("SO", 135.0), ("S", 180.0), ("SW", 225.0), ("W", 270.0), ("NW", 315.0),
        };

        /// <summary>Ein Winkel normiert auf [0, 360) und auf 1e-9 ° gerundet; ein nicht endlicher Wert ergibt <c>null</c>.</summary>
        public static double? Normiert(double? grad) => RaumgrundrissSchema.Normiert(grad);

        /// <summary>Der Nordwinkel zur Richtung der Planoberseite α (N1): (360° − α) mod 360°; <c>null</c> bleibt <c>null</c>.</summary>
        public static double? NordwinkelAusPlanoberseite(double? planoberseiteGrad)
            => Normiert(planoberseiteGrad) is double a ? Normiert(360.0 - a) : null;

        /// <summary>Die Richtung der Planoberseite zum Nordwinkel (Umkehrung von N1, dieselbe Formel); <c>null</c> bleibt <c>null</c>.</summary>
        public static double? PlanoberseiteAusNordwinkel(double? nordwinkelGrad) => NordwinkelAusPlanoberseite(nordwinkelGrad);

        /// <summary>
        /// Ein Azimut nach einer Änderung des Nordwinkels von <paramref name="altGrad"/> auf <paramref name="neuGrad"/>:
        /// azimut − (neu − alt), normiert; <c>null</c> bleibt <c>null</c> (N5).
        /// </summary>
        public static double? Gedreht(double? azimutGrad, double altGrad, double neuGrad)
            => azimutGrad is double a ? Normiert(a - (neuGrad - altGrad)) : null;

        /// <summary>Das Kürzel der Schnellwahl zu einer Richtung der Planoberseite; <c>null</c> = keine der acht.</summary>
        public static string KuerzelZu(double? planoberseiteGrad)
        {
            if (!(Normiert(planoberseiteGrad) is double a)) return null;
            foreach ((string k, double g) in Schnellwahl)
                if (Math.Abs(a - g) <= 1e-9) return k;
            return null;
        }
    }
}
