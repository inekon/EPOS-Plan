using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Klima eines Gebäudes, einmal für alle seine Zonen</b> (Stufe G6b, Welle W3;
    /// Mehrzonenkonzept 2.1, Auftrag G6b W3 „GebaeudeKlima herauslösen") — was der Eingangsbauer
    /// (<see cref="GebaeudeModellEingang.Bauen(ProjektGebaeudeModel, GebaeudeKlima, bool, string, double, double, double)"/>)
    /// aus dem Klimakalender des Laufs bildet und was jede Zone desselben Gebäudes teilt: die
    /// Außenlufttemperatur θ_out, die Einstrahlung auf die vier Fassaden, die Einstrahlung je
    /// (Neigung, Azimut) eines Bauteils, die Erdreichtemperatur nach Kusuda, die Zahl der Stunden
    /// mit Gegenstrahlung und der Kalender (Wochenendmaske des Ortszeit-Kalenders). Die Nachtzeit
    /// und die Ferien kommen vom Gebäude (Festlegung 1 des Auftrags; <see cref="GebaeudeModellEingang.Daten"/>).
    ///
    /// <para><b>Die Operationen behalten ihre Reihenfolge</b> (Auftrag G6b, Risiko 1): Jede Reihe
    /// entsteht mit denselben Aufrufen des Klimawegs (<see cref="GebaeudeKlimaweg"/>) wie vor der
    /// Herauslösung, in derselben Reihenfolge und Arithmetik — das Netz der Einzonenreihen
    /// (<c>GebaeudeEinzonennetzTests</c>) hält die Bitgleichheit. <see cref="Bereitstellen"/> prüft
    /// und rechnet erst dort, wo der Eingangsbauer es vorher tat (nach den Gebäudedaten und den
    /// Ersatzparametern); ein zweiter Aufruf rechnet nichts neu.</para>
    ///
    /// <para><b>Zwischenspeicher, kein Rechenzustand:</b> Die Einstrahlung je (Neigung, Azimut)
    /// und die Erdreichreihe entstehen beim ersten Bedarf und werden für jede weitere Zone
    /// wiederverwendet — dieselben Werte, weil jede Reihe eine reine Funktion der Klimazeilen ist.
    /// Ohne Datenbank, ohne Protokoll, einfädig.</para>
    /// </summary>
    internal sealed class GebaeudeKlima
    {
        private readonly Dictionary<(double Neigung, double Azimut), double[]> _einstrahlung =
            new Dictionary<(double Neigung, double Azimut), double[]>();

        private double[] _erdreich;
        private bool _erdreichAusKlima;

        /// <param name="zeilen">Die Klimazeilen in Ortszeit, 8 760, mit UTC-Herkunft.</param>
        /// <param name="wochenende">Die Wochenendmaske des Ortszeit-Kalenders, 365 Tage (U7).</param>
        /// <param name="laengengrad">Längengrad der Klimaregion [°].</param>
        /// <param name="breitengrad">Breitengrad der Klimaregion [°].</param>
        /// <param name="zeitbezug">Zeitbezug der Sonnengeometrie (U6).</param>
        internal GebaeudeKlima(IReadOnlyList<SolardatenModel> zeilen, bool[] wochenende,
                               double laengengrad, double breitengrad,
                               Zeitbezug zeitbezug = GebaeudeKlimaweg.ZEITBEZUG_VORGABE)
        {
            Zeilen = zeilen;
            Wochenende = wochenende;
            Laengengrad = laengengrad;
            Breitengrad = breitengrad;
            Zeitbezug = zeitbezug;
        }

        /// <summary>Die Klimazeilen in Ortszeit.</summary>
        internal IReadOnlyList<SolardatenModel> Zeilen { get; }

        /// <summary>Die Wochenendmaske des Ortszeit-Kalenders (365 Tage) — der Kalender aller Zonen.</summary>
        internal bool[] Wochenende { get; }

        /// <summary>Längengrad der Klimaregion [°].</summary>
        internal double Laengengrad { get; }

        /// <summary>Breitengrad der Klimaregion [°].</summary>
        internal double Breitengrad { get; }

        /// <summary>Der Zeitbezug der Sonnengeometrie (U6).</summary>
        internal Zeitbezug Zeitbezug { get; }

        /// <summary>Ist das Klima geprüft und gerechnet?</summary>
        internal bool Bereit { get; private set; }

        /// <summary>Außenlufttemperatur [°C] (E1); nach <see cref="Bereitstellen"/>.</summary>
        internal double[] ThetaOut { get; private set; }

        /// <summary>Die Einstrahlung auf die vier Fassaden [W/m²] (E2); nach <see cref="Bereitstellen"/>.</summary>
        internal Fassadenstrahlung Strahlung { get; private set; }

        /// <summary>Zahl der Stunden mit Gegenstrahlung (NULL-Regel E5); nach <see cref="Bereitstellen"/>.</summary>
        internal int StundenMitGegenstrahlung { get; private set; }

        /// <summary>
        /// Prüft die Klimazeilen und den Kalender und rechnet θ_out, die Fassadenstrahlung und die
        /// Zahl der Stunden mit Gegenstrahlung — in der Reihenfolge des Eingangsbauers vor der
        /// Herauslösung. Ein zweiter Aufruf tut nichts.
        /// </summary>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.KlimadatenUnvollstaendig"/>.</exception>
        internal void Bereitstellen()
        {
            if (Bereit) return;
            GebaeudeKlimaweg.Pruefen(Zeilen, Laengengrad, Breitengrad);
            if (Wochenende == null || Wochenende.Length != 365)
                throw new GebaeudeModellException(GebaeudeModellFehler.KlimadatenUnvollstaendig,
                    "Die Wochenendmaske des Ortszeit-Kalenders fehlt (365 Tage erwartet).");

            ThetaOut = GebaeudeKlimaweg.Aussentemperatur(Zeilen);
            Strahlung = GebaeudeKlimaweg.Fassaden(Zeilen, Laengengrad, Breitengrad, Zeitbezug);
            StundenMitGegenstrahlung = GebaeudeKlimaweg.StundenMitGegenstrahlung(Zeilen);

            // Die vier Fassaden sind die Einstrahlung senkrechter Flächen (bitgleich zu
            // GebaeudeKlimaweg.Einstrahlung, Stufe G3) - vorbelegt, wie im Bauteilweg vorher.
            _einstrahlung[(GebaeudeKlimaweg.NEIGUNG_FASSADE, GebaeudeKlimaweg.AZIMUT_SUED)] = Strahlung.Sued;
            _einstrahlung[(GebaeudeKlimaweg.NEIGUNG_FASSADE, GebaeudeKlimaweg.AZIMUT_OST)] = Strahlung.Ost;
            _einstrahlung[(GebaeudeKlimaweg.NEIGUNG_FASSADE, GebaeudeKlimaweg.AZIMUT_WEST)] = Strahlung.West;
            _einstrahlung[(GebaeudeKlimaweg.NEIGUNG_FASSADE, GebaeudeKlimaweg.AZIMUT_NORD)] = Strahlung.Nord;
            Bereit = true;
        }

        /// <summary>
        /// Die Einstrahlung auf eine Fläche der Neigung <paramref name="neigungGrad"/> und des Azimuts
        /// <paramref name="azimutKlimaweg"/> (Konvention des Klimawegs, Süd 0°; NaN = keiner) [W/m²] —
        /// einmal gerechnet (<see cref="GebaeudeKlimaweg.EinstrahlungBauteil"/>), danach aus dem
        /// Zwischenspeicher. Waagerecht nach oben ist der Azimut gleichgültig (Globalstrahlung).
        /// </summary>
        internal double[] Einstrahlung(double neigungGrad, double azimutKlimaweg)
        {
            Bereitstellen();
            (double, double) schluessel = neigungGrad == 0.0 ? (0.0, 0.0)
                : (neigungGrad, double.IsNaN(azimutKlimaweg) ? 0.0 : azimutKlimaweg);
            if (!_einstrahlung.TryGetValue(schluessel, out double[] r))
            {
                r = GebaeudeKlimaweg.EinstrahlungBauteil(Zeilen, Laengengrad, Breitengrad, azimutKlimaweg, neigungGrad, Zeitbezug);
                _einstrahlung[schluessel] = r;
            }
            return r;
        }

        /// <summary>
        /// Die Erdreichtemperatur nach Kusuda [°C] (E6) — einmal gerechnet; <paramref name="ausKlima"/>
        /// <c>false</c>, wenn der Jahresgang auf die Ersatzwerte des Erdreichmodells zurückfiel.
        /// </summary>
        internal double[] Erdreich(out bool ausKlima)
        {
            Bereitstellen();
            if (_erdreich == null)
                _erdreich = GebaeudeKlimaweg.Grundtemperatur(DbWerte.GRUND_ERDREICH, GebaeudeFestwerte.VORGABE_KELLERTEMPERATUR,
                                                              ThetaOut, out _erdreichAusKlima);
            ausKlima = _erdreichAusKlima;
            return _erdreich;
        }
    }
}
