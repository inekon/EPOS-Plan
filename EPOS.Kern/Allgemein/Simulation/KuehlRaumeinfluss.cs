using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>KK3 — der Raumeinfluss der Kühlkurve im Kreis und der gleitende Erzeugervorlauf</b> (Entwurf KK 2.2, 2.3, 2.5;
    /// Festlegungen 6, 8, 9; E106 Q-KK-1 (a), Q-KK-3 (a)) — der Spiegel von <see cref="Raumeinfluss"/> (H2):
    /// θ_V,K = KK(θ_out) − k_K · max(0, θ_air − θ_max), unten gekappt am kältesten erreichbaren Erzeugervorlauf und an der
    /// Vorlaufgrenze, die immer gewinnt (Festlegung 5). Je Stunde mit der Raumluft <b>derselben</b> Stunde im Kreis
    /// iteriert; Führungsgröße je Gebäude die Zone mit der größten Überschreitung ihres Kühlsollwerts.
    /// <list type="bullet">
    /// <item><b>Überschreitung</b> ü = max über die gekühlten Zonen (θ_air,z − θ_max,z), nach unten bei 0 begrenzt: eine
    /// Unterschreitung hebt den Vorlauf nicht (die Kurve trägt die Führung über die Außentemperatur).</item>
    /// <item><b>Iteration</b>: Durchlauf 1 mit der Absenkung 0, jeder weitere mit der Absenkung aus der Lösung des vorigen;
    /// Abbruchglied |ΔK2| ≤ <see cref="Anlagenkopplung.ABBRUCH_VORLAUF_K"/> (Festlegung 8); Pendelregel und Höchstzahl
    /// wie AK3.</item>
    /// <item><b>Gleitender Erzeuger</b> (2.3, Festlegung 9): je Durchlauf sammelt der Raumeinfluss den verlangten Vorlauf
    /// jedes kühlgekoppelten Gebäudes (der Rand nach der Absenkung); der kälteste geht an die Kälteschranke
    /// (<see cref="Kaelteschranke.Angebot(int, Stundenvorrang, double)"/>) und an die Kältestunde des Kreises.</item>
    /// </list>
    /// Wirkt nur mit dem Kernschalter (<see cref="KuehlkurveKernschalter"/>), Stufe AK3 und einer wirksamen Kühlkurve
    /// (<see cref="GebaeudeModellEingang.KuehlkurveWirksam"/>); ohne solche Kurve baut der Kreis keinen (Schalter aus bitgleich).
    /// Ein k_K von 0 an jedem Gebäude rechnet den gleitenden Vorlauf ohne Absenkung.
    /// </summary>
    internal sealed class KuehlRaumeinfluss
    {
        private readonly double[] _kk;
        private readonly double[] _erzeugerC;
        private readonly double[] _grenzeC;
        private readonly double[][] _reihe;
        private readonly double[][] _thetaMax;
        private readonly double[] _absenkung;
        private readonly List<double>[] _max;
        private readonly double[] _verlangt;
        private readonly bool[] _anGrenze;

        /// <param name="kk">k_K je Gebäude des Kreises [K/K]; 0 = keine Absenkung.</param>
        /// <param name="erzeugerC">Der kälteste erreichbare Erzeugervorlauf je Gebäude [°C]; NaN = keine Grenze.</param>
        /// <param name="grenzeC">Die Vorlaufgrenze je Gebäude [°C]; NaN = keine.</param>
        /// <param name="reihe">Die Kühlvorlaufreihe je Gebäude (Startwert des ersten Durchlaufs); <c>null</c> = keine Kälteseite.</param>
        /// <param name="thetaMax">Die Kühlsollwertreihe je Gebäude; <c>null</c> = jede Stunde zählt.</param>
        internal KuehlRaumeinfluss(IReadOnlyList<double> kk, IReadOnlyList<double> erzeugerC, IReadOnlyList<double> grenzeC,
                                   IReadOnlyList<double[]> reihe = null, IReadOnlyList<double[]> thetaMax = null)
        {
            if (kk == null) throw new ArgumentNullException(nameof(kk));
            int n = kk.Count;
            _kk = new double[n];
            _erzeugerC = new double[n];
            _grenzeC = new double[n];
            _reihe = new double[n][];
            _thetaMax = new double[n][];
            for (int i = 0; i < n; i++)
            {
                _kk[i] = double.IsNaN(kk[i]) || kk[i] < 0.0 ? 0.0 : kk[i];
                _erzeugerC[i] = erzeugerC != null && i < erzeugerC.Count ? erzeugerC[i] : double.NaN;
                _grenzeC[i] = grenzeC != null && i < grenzeC.Count ? grenzeC[i] : double.NaN;
                _reihe[i] = reihe != null && i < reihe.Count ? reihe[i] : null;
                _thetaMax[i] = thetaMax != null && i < thetaMax.Count ? thetaMax[i] : null;
            }
            _absenkung = new double[n];
            _verlangt = new double[n];
            _anGrenze = new bool[n];
            _max = new List<double>[n];
            for (int i = 0; i < n; i++) _max[i] = new List<double>();
            VerlangterC = double.NaN;
            KuehlVorlaufMinC = double.NaN;
        }

        /// <summary>
        /// Der Raumeinfluss aus den Eingängen der Kopplungsgebäude (Einzonenweg; <c>null</c> = Mehrzonen oder ohne Kälteseite):
        /// <c>null</c>, wenn kein Gebäude eine wirksame Kühlkurve rechnet — dann bleibt der Kreis am festen Kühlvorlauf.
        /// </summary>
        internal static KuehlRaumeinfluss AusEingaengen(IReadOnlyList<GebaeudeModellEingang> eingaenge)
        {
            if (eingaenge == null || eingaenge.Count == 0) return null;
            int n = eingaenge.Count;
            var kk = new double[n];
            var erzeuger = new double[n];
            var grenze = new double[n];
            var reihe = new double[n][];
            var thetaMax = new double[n][];
            bool kurve = false;
            for (int i = 0; i < n; i++)
            {
                GebaeudeModellEingang e = eingaenge[i];
                erzeuger[i] = grenze[i] = double.NaN;
                if (e == null || !e.KuehlKopplungWirksam || e.KuehlVorlaufC == null) continue;
                reihe[i] = e.KuehlVorlaufC;
                thetaMax[i] = e.ThetaMax;
                if (!e.KuehlkurveWirksam || e.Kuehlkurve == null) continue;
                kurve = true;
                kk[i] = e.KuehlkurveRaumeinflussKK > 0.0 ? e.KuehlkurveRaumeinflussKK : 0.0;
                erzeuger[i] = e.KuehlkurveErzeugerC;
                grenze[i] = e.Kuehlkurve.VorlaufgrenzeC;
            }
            return kurve ? new KuehlRaumeinfluss(kk, erzeuger, grenze, reihe, thetaMax) : null;
        }

        /// <summary>k_K des Gebäudes <paramref name="i"/> [K/K].</summary>
        internal double Kk(int i) => _kk[i];

        /// <summary>Die Absenkung des Gebäudes <paramref name="i"/> im laufenden Durchlauf [K] (vor der Kappung).</summary>
        internal double Absenkung(int i) => _absenkung[i];

        /// <summary>Die größte Absenkung des laufenden Durchlaufs über alle Gebäude [K].</summary>
        internal double AbsenkungMax()
        {
            double m = 0.0;
            foreach (double a in _absenkung) if (a > m) m = a;
            return m;
        }

        /// <summary>
        /// Der kälteste verlangte Vorlauf des laufenden Durchlaufs [°C] (<see cref="Kaeltevorlauf.KaeltesterVerlangter"/>) über
        /// die Gebäude, die in der Stunde kühlen; NaN, solange keines kühlt.
        /// </summary>
        internal double VerlangterC { get; private set; }

        // ------------------------------------------------------------------ Kennzahlen (Festlegung 12; Spalten mit KK4)

        /// <summary>Kühlstunden des Kreises: Stunden, in denen ein Gebäude Kälte abnahm und ein Vorlauf verlangt war.</summary>
        internal int Kuehlstunden { get; private set; }

        /// <summary>Summe des Erzeugervorlaufs über die Kühlstunden [°C·h] (Mittel über <see cref="Kuehlstunden"/>).</summary>
        internal double KuehlVorlaufSummeCh { get; private set; }

        /// <summary>Der mittlere Erzeugervorlauf der Kühlstunden [°C]; NaN ohne Kühlstunde.</summary>
        internal double KuehlVorlaufMittelC => Kuehlstunden > 0 ? KuehlVorlaufSummeCh / Kuehlstunden : double.NaN;

        /// <summary>Der kleinste Erzeugervorlauf einer Kühlstunde [°C]; NaN ohne Kühlstunde.</summary>
        internal double KuehlVorlaufMinC { get; private set; }

        /// <summary>Kühlstunden, in denen ein Gebäude an seiner Vorlaufgrenze stand.</summary>
        internal int StundenAnVorlaufgrenze { get; private set; }

        /// <summary>Stunden, in denen die Lösung eine Absenkung &gt; 0 trug.</summary>
        internal int StundenAbgesenkt { get; private set; }

        /// <summary>Summe der größten Absenkung je Stunde [Kh].</summary>
        internal double AbsenkungSummeKh { get; private set; }

        /// <summary>Stunden, in denen die Absenkung pendelte und die des ersten Durchlaufs festgehalten wurde.</summary>
        internal int StundenFestgehalten { get; private set; }

        // ------------------------------------------------------------------ Kreis

        /// <summary>Beginn einer Stunde: Absenkung 0 (Muster <see cref="Raumeinfluss.StundeBeginnen"/>).</summary>
        internal void StundeBeginnen()
        {
            for (int i = 0; i < _absenkung.Length; i++)
            {
                _absenkung[i] = 0.0;
                _max[i].Clear();
            }
            DurchlaufBeginnen();
        }

        /// <summary>Beginn eines Durchlaufs: die gesammelten Vorläufe und Befunde der Vorlaufgrenze leeren.</summary>
        internal void DurchlaufBeginnen()
        {
            for (int i = 0; i < _verlangt.Length; i++)
            {
                _verlangt[i] = double.NaN;
                _anGrenze[i] = false;
            }
            VerlangterC = double.NaN;
        }

        /// <summary>
        /// Der Startvorlauf des ersten Durchlaufs [°C]: der kälteste Vorlauf der Reihen in Stunde <paramref name="h"/> über
        /// die Gebäude mit endlichem Kühlsollwert; NaN = keines kühlt nach der Reihe.
        /// </summary>
        internal double StartC(int h)
        {
            double v = double.NaN;
            for (int i = 0; i < _reihe.Length; i++)
            {
                double[] r = _reihe[i];
                if (r == null || h < 0 || h >= r.Length) continue;
                double[] tm = _thetaMax[i];
                if (tm != null && h < tm.Length && (double.IsNaN(tm[h]) || double.IsInfinity(tm[h]))) continue;
                double x = r[h];
                if (double.IsNaN(x) || double.IsInfinity(x)) continue;
                if (double.IsNaN(v) || x < v) v = x;
            }
            return v;
        }

        /// <summary>
        /// Der Rand einer Zone im Durchlauf: merkt θ_max der Zone, senkt den Kühlvorlauf um die Absenkung des Gebäudes —
        /// unten nie unter den Erzeuger, an der Vorlaufgrenze gekappt (Befund <c>KuehlVorlaufGekappt</c>), nie über den
        /// Vorlauf der Kurve — und sammelt den verlangten Vorlauf. Ohne Absenkung derselbe Rand.
        /// </summary>
        internal Stundenrand Absenken(int i, int zone, in Stundenrand r)
        {
            List<double> max = _max[i];
            while (max.Count <= zone) max.Add(double.NaN);
            max[zone] = r.MitKuehlung ? r.ThetaMax : double.NaN;
            if (!r.MitKuehluebergabe || double.IsNaN(r.KuehlVorlaufC)) return r;
            Stundenrand aus = r;
            double a = _absenkung[i];
            if (a > 0.0)
            {
                double ziel = r.KuehlVorlaufC - a;
                double erz = _erzeugerC[i];
                if (!double.IsNaN(erz) && ziel < erz) ziel = erz;
                double grenze = _grenzeC[i];
                bool anGrenze = !double.IsNaN(grenze) && ziel < grenze;
                if (anGrenze) ziel = grenze;
                if (ziel < r.KuehlVorlaufC)
                    aus = r.MitKuehlvorlauf(ziel, anGrenze || r.KuehlVorlaufGekappt);
            }
            if (r.MitKuehlung)
            {
                double v = aus.KuehlVorlaufC;
                if (double.IsNaN(_verlangt[i]) || v < _verlangt[i]) _verlangt[i] = v;
                if (aus.KuehlVorlaufGekappt) _anGrenze[i] = true;
                if (double.IsNaN(VerlangterC) || v < VerlangterC) VerlangterC = v;
            }
            return aus;
        }

        /// <summary>
        /// Nach einem Durchlauf: die Absenkung je Gebäude aus der Lösung derselben Stunde neu (k_K · ü). Rückgabe: die
        /// größte Änderung der Absenkung [K] (ΔK2) — 0, wenn sie Bit für Bit blieb.
        /// </summary>
        internal double Nachfuehren(IReadOnlyList<IReadOnlyList<Stundenergebnis>> loesung)
        {
            if (loesung == null) return 0.0;
            double d = 0.0;
            for (int i = 0; i < _absenkung.Length && i < loesung.Count; i++)
            {
                double neu = Soll(i, loesung[i]);
                double alt = _absenkung[i];
                if (neu.Equals(alt)) continue;
                d = Math.Max(d, Math.Abs(neu - alt));
                _absenkung[i] = neu;
            }
            return d;
        }

        /// <summary>Pendelregel (Festlegung 8, wie AK3): ein letzter Durchlauf mit der Absenkung des ersten (0).</summary>
        internal void Festhalten()
        {
            Array.Clear(_absenkung, 0, _absenkung.Length);
            StundenFestgehalten++;
        }

        /// <summary>Die Stunde ist festgeschrieben: die Kennzahlen aus dem letzten Durchlauf.</summary>
        internal void Festschreiben(IReadOnlyList<IReadOnlyList<Stundenergebnis>> loesung)
        {
            double m = AbsenkungMax();
            if (m > 0.0)
            {
                StundenAbgesenkt++;
                AbsenkungSummeKh += m;
            }
            if (double.IsNaN(VerlangterC) || loesung == null) return;
            bool kuehlt = false, grenze = false;
            for (int i = 0; i < loesung.Count && i < _verlangt.Length; i++)
            {
                if (double.IsNaN(_verlangt[i]) || loesung[i] == null) continue;
                foreach (Stundenergebnis z in loesung[i])
                    if (z.KuehlleistungW > 0.0)
                    {
                        kuehlt = true;
                        if (_anGrenze[i]) grenze = true;
                    }
            }
            if (!kuehlt) return;
            Kuehlstunden++;
            KuehlVorlaufSummeCh += VerlangterC;
            if (double.IsNaN(KuehlVorlaufMinC) || VerlangterC < KuehlVorlaufMinC) KuehlVorlaufMinC = VerlangterC;
            if (grenze) StundenAnVorlaufgrenze++;
        }

        /// <summary>k_K · max(0, max_z(θ_air,z − θ_max,z)) über die gekühlten Zonen des Gebäudes.</summary>
        private double Soll(int i, IReadOnlyList<Stundenergebnis> zonen)
        {
            double k = _kk[i];
            if (!(k > 0.0) || zonen == null) return 0.0;
            List<double> max = _max[i];
            double ue = 0.0;
            for (int z = 0; z < zonen.Count && z < max.Count; z++)
            {
                double m = max[z];
                if (double.IsNaN(m) || double.IsInfinity(m)) continue;
                double diff = zonen[z].ThetaAirMittel - m;
                if (diff > ue) ue = diff;
            }
            return k * ue;
        }
    }
}
