using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>H2 — der Raumeinfluss der Heizkurve im Kreis</b> (Entwurf AK3, Festlegung 23; Anwenderentscheid E102, Q-AK3-2):
    /// θ_V = HK(θ_out) + k_R · (θ_soll − θ_i,ref), gekappt oben am Auslegungsvorlauf θ_V,N der Übergabe und am
    /// Vorlaufangebot der Anlage (<c>Vorlauf_Max</c>, Rückfall <c>Vorlauf</c>). Je Stunde, mit der Raumtemperatur
    /// <b>derselben</b> Stunde, im Kreis iteriert; Führungsgröße ist je Gebäude die Zone mit der größten Unterschreitung.
    /// k_R kommt aus <c>Tab_Gebaeude.Heizkurve_Raumeinfluss</c> und wirkt nur mit <c>Heizkurve_Aktiv</c>; NULL oder 0 = aus.
    /// <list type="bullet">
    /// <item><b>Unterschreitung</b> u = max über die beheizten Zonen (θ_soll,z − θ_air,z), nach unten bei 0 begrenzt:
    /// Eine Überschreitung senkt den Vorlauf nicht (benannt; die Übergabe liefert dann ohnehin nichts).</item>
    /// <item><b>Iteration</b>: Durchlauf 1 rechnet mit der Anhebung 0, jeder weitere mit der Anhebung aus der Lösung des
    /// vorigen Durchlaufs; der Kreis bricht erst ab, wenn sich die Anhebung um höchstens
    /// <see cref="Anlagenkopplung.ABBRUCH_VORLAUF_K"/> ändert.</item>
    /// <item><b>Vorstunden-Variante</b> (<see cref="Vorstunde"/>, nur Vergleich in den Proben): die Anhebung aus der
    /// festgeschriebenen Lösung der Vorstunde, nicht iteriert.</item>
    /// </list>
    /// Ohne wirksames k_R (alle 0) baut der Lauf keinen Raumeinfluss; ein Raumeinfluss mit lauter 0 rechnet Bit für Bit
    /// wie keiner (Probe „H2 aus bitgleich zu AK3 ohne H2“).
    /// </summary>
    internal sealed class Raumeinfluss
    {
        private readonly double[] _kr;
        private readonly double[] _anhebung;
        private readonly double[] _vorstunde;
        private readonly List<double>[] _soll;

        /// <param name="kr">k_R je Gebäude des Kreises [K/K] in der Reihenfolge der Kopplungsgebäude; 0 = aus.</param>
        internal Raumeinfluss(IReadOnlyList<double> kr)
        {
            if (kr == null) throw new ArgumentNullException(nameof(kr));
            _kr = new double[kr.Count];
            for (int i = 0; i < kr.Count; i++)
                _kr[i] = double.IsNaN(kr[i]) || kr[i] < 0.0 ? 0.0 : kr[i];
            _anhebung = new double[_kr.Length];
            _vorstunde = new double[_kr.Length];
            _soll = new List<double>[_kr.Length];
            for (int i = 0; i < _soll.Length; i++) _soll[i] = new List<double>();
        }

        /// <summary>
        /// k_R je Gebäude aus den Projektgebäuden: nur mit <c>Heizkurve_Aktiv</c> und k_R &gt; 0, sonst 0;
        /// <c>null</c>, wenn kein Gebäude einen wirksamen Raumeinfluss führt (dann rechnet der Kreis ohne H2).
        /// </summary>
        internal static Raumeinfluss AusGebaeuden(IReadOnlyList<ProjektGebaeudeModel> gebaeude)
        {
            if (gebaeude == null || gebaeude.Count == 0) return null;
            var kr = new double[gebaeude.Count];
            bool wirksam = false;
            for (int i = 0; i < gebaeude.Count; i++)
            {
                ProjektGebaeudeModel g = gebaeude[i];
                if (g != null && g.Heizkurve_Aktiv && g.Heizkurve_Raumeinfluss is double k && k > 0.0)
                {
                    kr[i] = k;
                    wirksam = true;
                }
            }
            return wirksam ? new Raumeinfluss(kr) : null;
        }

        /// <summary>Vergleichsvariante der Proben: Raumtemperatur der Vorstunde, nicht iteriert (Vorgabe <c>false</c>).</summary>
        internal bool Vorstunde { get; init; }

        /// <summary>k_R des Gebäudes <paramref name="i"/> [K/K].</summary>
        internal double Kr(int i) => _kr[i];

        /// <summary>Die Anhebung des Gebäudes <paramref name="i"/> im laufenden Durchlauf [K] (vor der Kappung).</summary>
        internal double Anhebung(int i) => _anhebung[i];

        /// <summary>Die größte Anhebung des laufenden Durchlaufs über alle Gebäude [K].</summary>
        internal double AnhebungMax()
        {
            double m = 0.0;
            foreach (double a in _anhebung) if (a > m) m = a;
            return m;
        }

        /// <summary>Stunden, in denen die Lösung eine Anhebung &gt; 0 trug (Kennzahl der Proben und Messung).</summary>
        internal int StundenAngehoben { get; private set; }

        /// <summary>Summe der größten Anhebung je Stunde [Kh] (Mittel über <see cref="StundenAngehoben"/>).</summary>
        internal double AnhebungSummeKh { get; private set; }

        /// <summary>Beginn einer Stunde: Anhebung 0 (gleiche Stunde) bzw. die der Vorstunde (Vergleichsvariante).</summary>
        internal void StundeBeginnen()
        {
            for (int i = 0; i < _anhebung.Length; i++)
            {
                _anhebung[i] = Vorstunde ? _vorstunde[i] : 0.0;
                _soll[i].Clear();
            }
        }

        /// <summary>
        /// Der Rand einer Zone im Durchlauf: merkt θ_soll der Zone und hebt den Vorlauf der Übergabe um die Anhebung des
        /// Gebäudes, gekappt oben an θ_V,N und am Vorlaufangebot <paramref name="angebotC"/> (NaN = keine Grenze) — nie
        /// über die Grenze hinaus und nie unter den Vorlauf der Heizkurve. Ohne Anhebung derselbe Rand.
        /// </summary>
        internal Stundenrand Anheben(int i, int zone, in Stundenrand r, double angebotC)
        {
            List<double> soll = _soll[i];
            while (soll.Count <= zone) soll.Add(double.NaN);
            soll[zone] = r.ThetaSoll;
            double a = _anhebung[i];
            if (!(a > 0.0) || !r.MitUebergabe) return r;
            double oben = r.Uebergabe.AuslegungVorlaufC;
            if (!double.IsNaN(angebotC) && (double.IsNaN(oben) || angebotC < oben)) oben = angebotC;
            return r.MitAnhebung(a, oben);
        }

        /// <summary>
        /// Nach einem Durchlauf: die Anhebung je Gebäude aus der Lösung derselben Stunde neu (k_R · u, u die größte
        /// Unterschreitung über die beheizten Zonen). Rückgabe: die größte Änderung der Anhebung [K] — 0, wenn sie
        /// Bit für Bit blieb oder die Vorstunden-Variante rechnet.
        /// </summary>
        internal double Nachfuehren(IReadOnlyList<IReadOnlyList<Stundenergebnis>> loesung)
        {
            if (Vorstunde || loesung == null) return 0.0;
            double d = 0.0;
            for (int i = 0; i < _anhebung.Length && i < loesung.Count; i++)
            {
                double neu = Soll(i, loesung[i]);
                double alt = _anhebung[i];
                if (neu.Equals(alt)) continue;
                d = Math.Max(d, Math.Abs(neu - alt));
                _anhebung[i] = neu;
            }
            return d;
        }

        /// <summary>Die Stunde ist festgeschrieben: Kennzahlen und — für die Vorstunden-Variante — die Anhebung der nächsten.</summary>
        internal void Festschreiben(IReadOnlyList<IReadOnlyList<Stundenergebnis>> loesung)
        {
            double m = AnhebungMax();
            if (m > 0.0)
            {
                StundenAngehoben++;
                AnhebungSummeKh += m;
            }
            if (!Vorstunde || loesung == null) return;
            for (int i = 0; i < _vorstunde.Length && i < loesung.Count; i++) _vorstunde[i] = Soll(i, loesung[i]);
        }

        /// <summary>k_R · max(0, max_z(θ_soll,z − θ_air,z)) über die beheizten Zonen des Gebäudes.</summary>
        private double Soll(int i, IReadOnlyList<Stundenergebnis> zonen)
        {
            double k = _kr[i];
            if (!(k > 0.0) || zonen == null) return 0.0;
            List<double> soll = _soll[i];
            double u = 0.0;
            for (int z = 0; z < zonen.Count && z < soll.Count; z++)
            {
                double s = soll[z];
                if (double.IsNaN(s)) continue;
                double diff = s - zonen[z].ThetaAirMittel;
                if (diff > u) u = diff;
            }
            return k * u;
        }
    }
}
