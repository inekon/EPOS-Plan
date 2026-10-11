using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Vorausschau des Vorheizens</b> (Entwurf Vorheizrampe Fassung 2, 2.4, Welle V3a): Von den Massen des Vorlaufs am
    /// Ende der Stunde h_s − t − 1 aus rechnet ein eigenes <see cref="Zonenmodell2K"/> derselben Parameter t Stunden mit dem
    /// Sollwert θ_T (Kühlkappe wie im Plan) und der Grenze P_V und mit den echten Randwerten der Stunden (Außenluft, Sonne,
    /// Gewinne, Lüftung, Erdreich, Nachbarn auf der Vorlaufbahn), dann prüft es die Ankunft am Beginn von h_s mit derselben
    /// Regel wie der Nachweis (<see cref="Zonenmodell2K.LuftAmBeginn"/>, δθ ≤ ε über <see cref="Rechenrand"/>).
    ///
    /// <para><b>Ohne Wirkung auf den Lauf:</b> Das Modell der Vorausschau gehört ihr allein; Eingang, Zonenlauf, Zähler und
    /// Ergebnisse der Zone bleiben unberührt. Jede Vorausschau beginnt mit <see cref="Zonenmodell2K.Zuruecksetzen(double, double)"/>
    /// auf dem Vorlaufzustand — sie ist rein und wiederholbar.</para>
    ///
    /// <para><b>Zustand außerhalb der Massen</b> (V1): Die Sommerlüftungs- und die Nachtauskühlregel bekommen eigene Regeln
    /// desselben Eingangs, je Vorausschau inaktiv begonnen, mit der Raumluft der Vorstunde aus dem Vorlauf; die Nachbarluft
    /// einer gekoppelten Zone liegt auf der Vorlaufbahn (2.7, zur sicheren Seite). Die Stunde h_s rechnet ohne Lüftungsregel
    /// wie der Nachweis.</para>
    /// </summary>
    internal sealed class Vorheizvorausschau
    {
        private const int STUNDEN = 8760;

        private readonly ZonenEingang _zone;
        private readonly GebaeudeModellEingang _e;
        private readonly double[] _soll;
        private readonly double[] _kuehl;
        private readonly GebaeudeModellErgebnis _vorlauf;
        private readonly IReadOnlyList<GebaeudeModellErgebnis> _alle;
        private readonly double[] _nachbarn;
        private readonly Zonenmodell2K _modell;
        private readonly Sommerlueftungsregel _regel;
        private readonly Sommerlueftungsregel _nachtregel;

        /// <param name="zone">Die Zone (Rand, Parameter).</param>
        /// <param name="soll">Die Heizsollwertreihe ohne Vorheizen (Kalender, 8 760 Stunden).</param>
        /// <param name="vorlauf">Das Ergebnis des Vorlaufs der Zone samt Massen am Stundenende.</param>
        /// <param name="alle">Die Vorlaufergebnisse aller Zonen in Rechenreihenfolge (Nachbarluft); <c>null</c> für Einzone.</param>
        /// <param name="vorheizleistungW">P_V im Fenster [W]; +∞ = keine Grenze.</param>
        /// <param name="deckelW">P_K ab h_s [W]; +∞ = keine Grenze.</param>
        /// <param name="genauigkeitK">ε [K].</param>
        internal Vorheizvorausschau(ZonenEingang zone, double[] soll, GebaeudeModellErgebnis vorlauf,
                                    IReadOnlyList<GebaeudeModellErgebnis> alle, double vorheizleistungW, double deckelW,
                                    double genauigkeitK)
        {
            _zone = zone ?? throw new ArgumentNullException(nameof(zone));
            _soll = soll ?? throw new ArgumentNullException(nameof(soll));
            _vorlauf = vorlauf ?? throw new ArgumentNullException(nameof(vorlauf));
            if (vorlauf.MassenEndeAw == null || vorlauf.MassenEndeIw == null)
                throw new ArgumentException("Der Vorlauf hat die Massen nicht erfasst.", nameof(vorlauf));
            _e = zone.Eingang;
            _kuehl = _e.ThetaMax;
            _alle = zone.Gekoppelt ? alle : null;
            _nachbarn = _alle == null ? null : new double[_alle.Count];
            _modell = new Zonenmodell2K(_e.Parameter, _e.Bezeichnung);
            _regel = Vdi6007Rechenweg.LueftungsregelBilden(_e);
            _nachtregel = Vdi6007Rechenweg.NachtauskuehlregelBilden(_e);
            VorheizleistungW = vorheizleistungW;
            DeckelW = deckelW;
            GenauigkeitK = genauigkeitK;
        }

        /// <summary>P_V [W] (+∞ = keine Grenze).</summary>
        internal double VorheizleistungW { get; }

        /// <summary>P_K [W] (+∞ = keine Grenze).</summary>
        internal double DeckelW { get; }

        /// <summary>ε [K].</summary>
        internal double GenauigkeitK { get; }

        /// <summary>Die Zahl der gerechneten Vorausschauen (Rechenzeit, 2.4).</summary>
        internal int Vorausschauen { get; private set; }

        /// <summary>Die Zahl der gerechneten Modellstunden (Rechenzeit, 2.4).</summary>
        internal long Schritte { get; private set; }

        /// <summary>
        /// <b>Eine Vorausschau</b>: kommt die Zone mit t Stunden Vorheizen vor h_s unter P_V an? <paramref name="deltaThetaK"/>
        /// ist das Luftdefizit am Beginn von h_s [K], nicht negativ.
        /// </summary>
        /// <param name="phiRefW">Φ_ref(h_s) [W] — die Grenze der Stunde h_s ist max(P_K, Φ_ref) wie im Plan.</param>
        internal bool Ankunft(int hs, int t, double phiRefW, out double deltaThetaK)
        {
            if (t < 1 || t >= STUNDEN) throw new ArgumentOutOfRangeException(nameof(t));
            double thetaT = _soll[hs];
            int h0 = Ring(hs - t), hv = Ring(h0 - 1);
            _modell.Zuruecksetzen(_vorlauf.MassenEndeAw[hv], _vorlauf.MassenEndeIw[hv]);
            _regel?.Setzen(false);
            _nachtregel?.Setzen(false);
            double luftVor = _vorlauf.Raumtemperatur[hv], aussenVor = _e.ThetaOut[hv];
            double grenzeFenster = Grenze(VorheizleistungW);
            for (int k = 0; k < t; k++)
            {
                int h = Ring(h0 + k);
                bool sommer = _regel != null && _regel.Stunde(h, luftVor, aussenVor);
                bool nacht = _nachtregel != null && _nachtregel.Stunde(h, luftVor, aussenVor);
                Stundenrand r = _zone.Rand(h, sommer, Nachbarn(h), nacht);
                double wert = thetaT;
                double kuehl = _kuehl == null ? double.PositiveInfinity : _kuehl[h];
                if (!double.IsNaN(kuehl) && !double.IsInfinity(kuehl))
                {
                    double kappe = Aufheizoptimierung.Kuehlkappe(kuehl);
                    if (kappe < wert) wert = kappe;
                }
                double s = _soll[h];
                if (!double.IsNaN(s) && s > wert) wert = s;
                r = r.MitVorheizen(wert, grenzeFenster);
                Stundenergebnis erg = _modell.Schritt(in r);
                luftVor = erg.ThetaAirMittel;
                aussenVor = _e.ThetaOut[h];
                Schritte++;
            }
            Vorausschauen++;
            Stundenrand rs = _zone.Rand(hs, false, Nachbarn(hs), false)
                .MitVorheizen(thetaT, Grenze(Math.Max(DeckelW, phiRefW)));
            deltaThetaK = Math.Max(0.0, thetaT - _modell.LuftAmBeginn(in rs));
            return Rechenrand.SchwelleErreicht(GenauigkeitK, deltaThetaK);
        }

        /// <summary>
        /// <b>Der Bedarf t_nötig(h_s)</b> (2.1, 2.3): das kleinste t ∈ {1 … <paramref name="tMax"/>} mit Ankunft, durch Bisektion
        /// über die Monotonie in t — höchstens ⌈log2(tMax + 1)⌉ Vorausschauen (tMax = 47: sechs). Kommt kein t an, ist der Sprung
        /// <paramref name="unerreichbar"/> und der Bedarf tMax (die Absenkung entfällt).
        /// </summary>
        internal int Bedarf(int hs, int tMax, double phiRefW, out bool unerreichbar)
        {
            if (tMax < 1) throw new ArgumentOutOfRangeException(nameof(tMax));
            int lo = 1, hi = tMax + 1;
            while (lo < hi)
            {
                int mitte = (lo + hi) / 2;
                if (Ankunft(hs, mitte, phiRefW, out _)) hi = mitte;
                else lo = mitte + 1;
            }
            unerreichbar = lo > tMax;
            return unerreichbar ? tMax : lo;
        }

        /// <summary>Der Bedarf durch lineare Suche (Probe der Bisektion, Monotonie): das erste t mit Ankunft.</summary>
        internal int BedarfLinear(int hs, int tMax, double phiRefW, out bool unerreichbar)
        {
            for (int t = 1; t <= tMax; t++)
                if (Ankunft(hs, t, phiRefW, out _))
                {
                    unerreichbar = false;
                    return t;
                }
            unerreichbar = true;
            return tMax;
        }

        private double Grenze(double w)
        {
            double skalar = _e.HeizleistungMaxW;
            if (double.IsPositiveInfinity(w) || double.IsNaN(w)) return skalar;
            return double.IsNaN(skalar) || w < skalar ? w : skalar;
        }

        private ReadOnlySpan<double> Nachbarn(int h)
        {
            if (_nachbarn == null) return ReadOnlySpan<double>.Empty;
            for (int i = 0; i < _alle.Count; i++) _nachbarn[i] = _alle[i].Raumtemperatur[h];
            return _nachbarn;
        }

        private static int Ring(int h) => Aufheizoptimierung.Ring(h);
    }
}
