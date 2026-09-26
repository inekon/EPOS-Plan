using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Zonenschleife eines Mehrzonengebäudes</b> (Stufe G6b, Welle W4; Mehrzonenkonzept 2.4,
    /// 2.9; ADR-005) — Gauß-Seidel je Stunde über die Zonen, jede mit ihrem Zonenlauf
    /// (<see cref="Zonenlauf"/>) und ihrem Eingang (<see cref="ZonenEingang"/>).
    ///
    /// <list type="bullet">
    /// <item><b>Teilgruppen:</b> Zonen, die über eine Trennfläche der Außengruppe oder einen Luftstrom
    /// zusammenhängen (Union-Find), iterieren gemeinsam; eine Zone für sich rechnet ohne Iteration.</item>
    /// <item><b>Je Stunde:</b> die Sommerlüftungsregel jeder Zone einmal vor den Durchläufen; die Massen
    /// gesichert; Durchläufe in der festen Reihenfolge (Rang, dann Kennung), Start mit θ̄_air der
    /// Vorstunde, jede Zone mit den zuletzt bekannten Nachbartemperaturen derselben Stunde. Abbruch ab
    /// dem zweiten Durchlauf, wenn alle |Δθ̄_air| &lt; 0,01 K und alle |ΔΦ_h|, |ΔΦ_c| &lt; 0,1 W
    /// (Festlegung 6; Φ_c ergänzt ADR-005 Nr. 2), höchstens <see cref="HOECHSTZAHL_DURCHLAEUFE"/>;
    /// darüber der benannte Fehler <see cref="GebaeudeModellFehler.ZonenkopplungKonvergiertNicht"/>.
    /// Reihen und Zähler übernimmt der Zonenlauf erst nach der Konvergenz.</item>
    /// <item><b>Mustertreue</b> (Mehrzonenkonzept 2.4, Probe 8): Wechselt die Fallfolge einer Zone
    /// gegenüber dem ersten Durchlauf, wird das Muster des ersten Durchlaufs festgehalten
    /// (<see cref="Zonenmodell2K.SchrittMitMuster"/>) und der Wechsel gezählt
    /// (<see cref="Musterwechsel"/>). Ist es unter dem neuen Rand nicht haltbar (Abschnittsregel),
    /// gilt die neue Fallfolge, gezählt in <see cref="MusterNichtHaltbar"/>.</item>
    /// <item><b>Vorlauf nur ab N ≥ 2</b> (Anwenderentscheid A3 = M6 (a)): 720 Stunden gekoppelt, dann
    /// eine Wiederholung ab dem Endzustand; weicht die Endtemperatur einer Zone um mehr als
    /// <see cref="VORLAUF_PROBE_K"/> ab, weitere <see cref="VORLAUF_LANG_H"/> Stunden (90 Tage),
    /// benannt (<see cref="VorlaufVerlaengert"/>). Startwert einer beheizten Zone ist ihr Sollwert der
    /// ersten Vorlaufstunde, einer unbeheizten das Mittel ihres θ_eq über die 720 Vorlaufstunden
    /// (Festlegung 7).</item>
    /// </list>
    /// Deterministisch: feste Reihenfolge, feste Schwellen, keine Parallelität. Ohne Datenbank, ohne
    /// Protokoll — die Befunde meldet der Aufrufer.
    /// </summary>
    internal sealed class Zonenschleife
    {
        /// <summary>Höchstzahl der Durchläufe je Stunde (Mehrzonenkonzept 2.4).</summary>
        internal const int HOECHSTZAHL_DURCHLAEUFE = 50;

        /// <summary>Abbruchschwelle der Raumluft [K] (Festlegung 6).</summary>
        internal const double SCHWELLE_TEMPERATUR_K = 0.01;

        /// <summary>Abbruchschwelle der Heiz- und Kühlleistung [W] (Festlegung 6).</summary>
        internal const double SCHWELLE_LEISTUNG_W = 0.1;

        /// <summary>Die Probe des Vorlaufs [K]: die halbe Druckstelle aus Entscheid E10 (A3 = M6 (a)).</summary>
        internal const double VORLAUF_PROBE_K = 0.05;

        /// <summary>Der verlängerte Vorlauf [h]: 90 Tage (A3 = M6 (a)).</summary>
        internal const int VORLAUF_LANG_H = 90 * 24;

        /// <summary>Jacobi-Schritte für den Startwert unbeheizter Zonen (Festlegung 7).</summary>
        private const int STARTWERT_SCHRITTE = 20;

        private readonly IReadOnlyList<ZonenEingang> _zonen;
        private readonly Zonenlauf[] _laeufe;
        private readonly int[][] _gruppen;
        private readonly string _wer;
        private readonly double[] _luft;
        private readonly bool[] _sommer;
        private readonly Stundenergebnis[] _ergebnis;
        private readonly Stundenmuster[] _muster;
        private readonly double[] _sicherAw, _sicherIw, _vorAir, _vorH, _vorC;
        private readonly bool[] _gehalten, _nichtHaltbar;
        private readonly int[] _musterJeZone, _durchlaeufeMaxJeZone;

        private readonly bool[] _umschaltung = new bool[8760];
        private readonly bool[] _heizen = new bool[8760];
        private readonly bool[] _kuehlen = new bool[8760];
        private readonly bool[] _sommerStunde = new bool[8760];

        /// <param name="zonen">Die Zonen des Gebäudes in der Rechenreihenfolge (<see cref="ZonenEingang.Bauen"/>).</param>
        /// <param name="wer">Das Gebäude für Meldungen.</param>
        internal Zonenschleife(IReadOnlyList<ZonenEingang> zonen, string wer)
        {
            if (zonen == null || zonen.Count == 0) throw new ArgumentException("Keine Zone.", nameof(zonen));
            _zonen = zonen;
            _wer = wer ?? "";
            int n = zonen.Count;
            _laeufe = new Zonenlauf[n];
            for (int i = 0; i < n; i++)
            {
                if (zonen[i].Index != i) throw new ArgumentException("Die Zonen stehen nicht in ihrer Rechenreihenfolge.", nameof(zonen));
                _laeufe[i] = new Zonenlauf(zonen[i]);
            }
            _gruppen = Teilgruppen(zonen);
            _luft = new double[n];
            _sommer = new bool[n];
            _ergebnis = new Stundenergebnis[n];
            _muster = new Stundenmuster[n];
            _sicherAw = new double[n];
            _sicherIw = new double[n];
            _vorAir = new double[n];
            _vorH = new double[n];
            _vorC = new double[n];
            _gehalten = new bool[n];
            _nichtHaltbar = new bool[n];
            _musterJeZone = new int[n];
            _durchlaeufeMaxJeZone = new int[n];
            for (int i = 0; i < n; i++) _durchlaeufeMaxJeZone[i] = 1;
        }

        /// <summary>Zonenstunden mit gehaltenem oder nicht haltbarem Muster, je Zone (Tab_ErgebnisZone).</summary>
        internal IReadOnlyList<int> MusterwechselJeZone => _musterJeZone;

        /// <summary>Die größte Zahl der Durchläufe einer Stunde je Zone (1 ohne Iteration).</summary>
        internal IReadOnlyList<int> DurchlaeufeMaxJeZone => _durchlaeufeMaxJeZone;

        /// <summary>Die Zonenläufe, in der Rechenreihenfolge.</summary>
        internal IReadOnlyList<Zonenlauf> Laeufe => _laeufe;

        /// <summary>
        /// Die Abbruchschwellen [K, W] — Vorgabe nach Festlegung 6; umgestellt allein in den Proben
        /// (5a: Fixpunkt des Stundenmittelmodells, 8: erzwungene Nichtkonvergenz).
        /// </summary>
        internal (double TemperaturK, double LeistungW) Schwellen { get; set; } = (SCHWELLE_TEMPERATUR_K, SCHWELLE_LEISTUNG_W);

        /// <summary>Die Höchstzahl der Durchläufe; Vorgabe <see cref="HOECHSTZAHL_DURCHLAEUFE"/>, umgestellt allein in Probe 5a.</summary>
        internal int Hoechstzahl { get; set; } = HOECHSTZAHL_DURCHLAEUFE;

        /// <summary>Die Probe des Vorlaufs [K]; Vorgabe <see cref="VORLAUF_PROBE_K"/>, umgestellt allein in der Probe der Verlängerung.</summary>
        internal double VorlaufProbeK { get; set; } = VORLAUF_PROBE_K;

        /// <summary>
        /// Kopplung über die Vorstunde statt Gauß-Seidel (Weg A des Mehrzonenkonzepts 2.4) — ein
        /// Schalter allein für Probe 6, nicht im Lauf: je Stunde ein Durchlauf mit den Lufttemperaturen
        /// der Vorstunde.
        /// </summary>
        internal bool VorstundeFuerProbe { get; set; }

        /// <summary>
        /// Eine vorgegebene Lufttemperatur je (Zone, Stunde) [°C], NaN = keine — allein für Probe 1:
        /// Die Nachbarn sehen die Zone auf dem vorgegebenen Wert „ideal gehalten"; ihr eigener Löser
        /// rechnet weiter. Im Lauf nie gesetzt.
        /// </summary>
        internal Func<int, int, double> LuftvorgabeFuerProbe { get; set; }

        /// <summary>Wird je übernommener Jahresstunde und Zone gerufen (Zone, Stunde, Ergebnis) — allein für Probe 4.</summary>
        internal Action<int, int, Stundenergebnis> BeobachterFuerProbe { get; set; }

        /// <summary>Die Teilgruppen (Stellen der Zonen, aufsteigend), nach ihrer ersten Zone geordnet.</summary>
        internal IReadOnlyList<IReadOnlyList<int>> Gruppen => _gruppen;

        /// <summary>Die Stunden des Vorlaufs, die gerechnet wurden (720 + 720, verlängert + 2160).</summary>
        internal int VorlaufStunden { get; private set; }

        /// <summary>Die größte Abweichung der Endtemperatur zwischen dem Vorlauf und seiner Wiederholung [K].</summary>
        internal double VorlaufAbweichungK { get; private set; }

        /// <summary>Wurde der Vorlauf auf 90 Tage verlängert (A3 = M6 (a))?</summary>
        internal bool VorlaufVerlaengert { get; private set; }

        /// <summary>Die Startwerte der Zonen [°C] (Festlegung 7).</summary>
        internal IReadOnlyList<double> Startwerte { get; private set; } = Array.Empty<double>();

        /// <summary>Stunden × Teilgruppen, die iteriert wurden (Jahr und Vorlauf).</summary>
        internal long IterierteStunden { get; private set; }

        /// <summary>Summe der Durchläufe der iterierten Stunden.</summary>
        internal long DurchlaeufeSumme { get; private set; }

        /// <summary>Die größte Zahl der Durchläufe einer Stunde.</summary>
        internal int DurchlaeufeMax { get; private set; }

        /// <summary>Das Mittel der Durchläufe je iterierter Stunde; 0 ohne Iteration.</summary>
        internal double DurchlaeufeMittel => IterierteStunden == 0 ? 0.0 : (double)DurchlaeufeSumme / IterierteStunden;

        /// <summary>Zone × Stunde, in denen das Muster des ersten Durchlaufs festgehalten wurde.</summary>
        internal int Musterwechsel { get; private set; }

        /// <summary>Zone × Stunde, in denen das Muster des ersten Durchlaufs unter dem neuen Rand nicht haltbar war.</summary>
        internal int MusterNichtHaltbar { get; private set; }

        /// <summary>Jahresstunden, in denen mindestens eine Zone mehr als einen Abschnitt rechnete.</summary>
        internal IReadOnlyList<bool> StundenMitUmschaltung => _umschaltung;

        /// <summary>Jahresstunden, in denen mindestens eine Zone heizte.</summary>
        internal IReadOnlyList<bool> StundenMitHeizen => _heizen;

        /// <summary>Jahresstunden, in denen mindestens eine Zone kühlte.</summary>
        internal IReadOnlyList<bool> StundenMitKuehlen => _kuehlen;

        /// <summary>Jahresstunden, in denen mindestens eine beheizte Zone sommerlich lüftete.</summary>
        internal IReadOnlyList<bool> StundenMitSommerlueftung => _sommerStunde;

        // =====================================================================
        //  Vorlauf und Jahr
        // =====================================================================

        /// <summary>
        /// Der Vorlauf (Klassenkopf): Startwerte nach Festlegung 7, 720 Stunden gekoppelt, die
        /// Wiederholung ab dem Endzustand und, bei mehr als <see cref="VORLAUF_PROBE_K"/>, 90 Tage.
        /// </summary>
        internal void Vorlauf()
        {
            int kurz = Vdi6007Rechenweg.VORLAUF_H;
            int start = 8760 - kurz;
            double[] s0 = StartwerteBilden(start);
            Startwerte = s0;
            for (int z = 0; z < _laeufe.Length; z++)
            {
                _laeufe[z].Beginnen(s0[z]);
                _luft[z] = Vorgabe(z, start, s0[z]);
            }

            Durchlauf(start, jahr: false);
            var ende1 = (double[])_luft.Clone();
            Durchlauf(start, jahr: false);
            double abweichung = 0.0;
            for (int z = 0; z < _luft.Length; z++) abweichung = Math.Max(abweichung, Math.Abs(_luft[z] - ende1[z]));
            VorlaufAbweichungK = abweichung;
            VorlaufStunden = 2 * kurz;
            if (abweichung > VorlaufProbeK)
            {
                Durchlauf(8760 - VORLAUF_LANG_H, jahr: false);
                VorlaufStunden += VORLAUF_LANG_H;
                VorlaufVerlaengert = true;
            }
        }

        /// <summary>Das Jahr, 8 760 Stunden.</summary>
        internal void Jahr() => Durchlauf(0, jahr: true);

        /// <summary>Die unskalierten Ergebnisse je Zone nach dem Jahr (<see cref="Zonenlauf.Ergebnis"/>).</summary>
        internal GebaeudeModellErgebnis[] Zonenergebnisse(int index, int idGebaeude)
        {
            var e = new GebaeudeModellErgebnis[_laeufe.Length];
            for (int z = 0; z < e.Length; z++) e[z] = _laeufe[z].Ergebnis(index, idGebaeude);
            return e;
        }

        private void Durchlauf(int start, bool jahr)
        {
            for (int h = start; h < 8760; h++) Stunde(h, jahr);
        }

        /// <summary>Eine Stunde aller Zonen (Klassenkopf).</summary>
        private void Stunde(int h, bool jahr)
        {
            int n = _laeufe.Length;
            for (int z = 0; z < n; z++) _sommer[z] = _laeufe[z].Sommerlueftung();

            foreach (int[] gruppe in _gruppen)
            {
                if (VorstundeFuerProbe && gruppe.Length > 1)
                {
                    var vorstunde = (double[])_luft.Clone();
                    foreach (int z in gruppe)
                    {
                        Stundenrand r = _zonen[z].Rand(h, _sommer[z], vorstunde);
                        Stundenergebnis s = _laeufe[z].Modell.Schritt(in r);
                        _ergebnis[z] = s;
                        _luft[z] = s.ThetaAirMittel;
                    }
                }
                else if (gruppe.Length == 1)
                {
                    int z = gruppe[0];
                    Stundenrand r = _zonen[z].Rand(h, _sommer[z], _luft);
                    Stundenergebnis s = _laeufe[z].Modell.Schritt(in r);
                    _ergebnis[z] = s;
                    _luft[z] = Vorgabe(z, h, s.ThetaAirMittel);
                }
                else
                    Iterieren(h, gruppe);
            }

            for (int z = 0; z < n; z++)
            {
                Stundenergebnis s = _ergebnis[z];
                if (!jahr)
                {
                    _laeufe[z].VorlaufUebernehmen(h, in s);
                    continue;
                }
                _laeufe[z].Uebernehmen(h, _sommer[z], in s);
                BeobachterFuerProbe?.Invoke(z, h, s);
                if (s.Abschnitte > 1) _umschaltung[h] = true;
                if (s.HeizleistungW > 0.0) _heizen[h] = true;
                if (s.KuehlleistungW > 0.0) _kuehlen[h] = true;
                if (_sommer[z] && _zonen[z].IstBeheizt) _sommerStunde[h] = true;
            }
        }

        /// <summary>Die Lufttemperatur, die die Nachbarn von Zone <paramref name="z"/> sehen: ohne Vorgabe (immer im Lauf) die gerechnete.</summary>
        private double Vorgabe(int z, int h, double gerechnet)
        {
            if (LuftvorgabeFuerProbe == null) return gerechnet;
            double v = LuftvorgabeFuerProbe(z, h);
            return double.IsNaN(v) ? gerechnet : v;
        }

        /// <summary>Gauß-Seidel über eine Teilgruppe (Klassenkopf).</summary>
        private void Iterieren(int h, int[] gruppe)
        {
            foreach (int z in gruppe)
            {
                _sicherAw[z] = _laeufe[z].Modell.ThetaMAw;
                _sicherIw[z] = _laeufe[z].Modell.ThetaMIw;
                _gehalten[z] = false;
                _nichtHaltbar[z] = false;
            }

            double groessteK = 0.0, groessteW = 0.0;
            (double schwelleK, double schwelleW) = Schwellen;
            for (int k = 1; k <= Hoechstzahl; k++)
            {
                groessteK = 0.0;
                groessteW = 0.0;
                foreach (int z in gruppe)
                {
                    Zonenmodell2K m = _laeufe[z].Modell;
                    if (k > 1) m.Zuruecksetzen(_sicherAw[z], _sicherIw[z]);
                    Stundenrand r = _zonen[z].Rand(h, _sommer[z], _luft);
                    Stundenergebnis s = m.Schritt(in r);
                    if (k == 1) _muster[z] = m.LetztesMuster;
                    else if (!m.LetzteFolgeGleich(_muster[z]))
                    {
                        // Das Muster des ersten Durchlaufs festhalten (Mehrzonenkonzept 2.4).
                        m.Zuruecksetzen(_sicherAw[z], _sicherIw[z]);
                        try
                        {
                            s = m.SchrittMitMuster(in r, _muster[z]);
                            _gehalten[z] = true;
                        }
                        catch (GebaeudeModellException ex) when (ex.Grund == GebaeudeModellFehler.AbschnittsregelVerletzt)
                        {
                            m.Zuruecksetzen(_sicherAw[z], _sicherIw[z]);
                            s = m.Schritt(in r);
                            _muster[z] = m.LetztesMuster;
                            _nichtHaltbar[z] = true;
                        }
                    }
                    if (k > 1)
                    {
                        groessteK = Math.Max(groessteK, Math.Abs(s.ThetaAirMittel - _vorAir[z]));
                        groessteW = Math.Max(groessteW, Math.Max(Math.Abs(s.HeizleistungW - _vorH[z]), Math.Abs(s.KuehlleistungW - _vorC[z])));
                    }
                    _vorAir[z] = s.ThetaAirMittel;
                    _vorH[z] = s.HeizleistungW;
                    _vorC[z] = s.KuehlleistungW;
                    _luft[z] = Vorgabe(z, h, s.ThetaAirMittel);
                    _ergebnis[z] = s;
                }
                if (k >= 2 && groessteK < schwelleK && groessteW < schwelleW)
                {
                    IterierteStunden++;
                    DurchlaeufeSumme += k;
                    if (k > DurchlaeufeMax) DurchlaeufeMax = k;
                    foreach (int z in gruppe)
                    {
                        if (_gehalten[z]) Musterwechsel++;
                        if (_nichtHaltbar[z]) MusterNichtHaltbar++;
                        if (_gehalten[z] || _nichtHaltbar[z]) _musterJeZone[z]++;
                        if (k > _durchlaeufeMaxJeZone[z]) _durchlaeufeMaxJeZone[z] = k;
                    }
                    return;
                }
            }

            // Festlegung 6/12: kein stiller Rückfall auf den letzten Stand.
            double stromM3h = 0.0;
            foreach (int z in gruppe)
                foreach (Luftkopplung l in _zonen[z].Eingang.Luftkopplungen) stromM3h += l.Leitwert_WK / GebaeudeFestwerte.C_RHO_LUFT;
            stromM3h *= 0.5;   // jedes Paar steht in beiden Zonen
            CultureInfo kultur = CultureInfo.CurrentCulture;
            throw new GebaeudeModellException(GebaeudeModellFehler.ZonenkopplungKonvergiertNicht,
                string.Format(kultur, MyResource.Resource.SIMENG_G6_KONVERGIERT_NICHT, _wer,
                              string.Join(", ", gruppe.Select(z => _zonen[z].Bezeichnung)),
                              h.ToString(CultureInfo.InvariantCulture), Hoechstzahl.ToString(kultur),
                              groessteK.ToString("0.####", kultur), groessteW.ToString("0.###", kultur),
                              stromM3h.ToString("0.#", kultur)));
        }

        // =====================================================================
        //  Startwerte und Teilgruppen
        // =====================================================================

        /// <summary>
        /// Die Startwerte (Festlegung 7): beheizt der Sollwert der ersten Vorlaufstunde, unbeheizt das
        /// Mittel von θ_eq über die 720 Vorlaufstunden — mit den Startwerten der Nachbarn in den
        /// Nachbargliedern, für unbeheizte Nachbarn in wenigen Jacobi-Schritten ab dem Mittel der
        /// Außenluft (EPOS-Regel).
        /// </summary>
        private double[] StartwerteBilden(int start)
        {
            int n = _zonen.Count;
            var s = new double[n];
            double aussen = 0.0;
            for (int h = start; h < 8760; h++) aussen += _zonen[0].Eingang.ThetaOut[h];
            aussen /= 8760 - start;
            bool unbeheizt = false;
            for (int z = 0; z < n; z++)
            {
                if (_zonen[z].IstBeheizt) s[z] = _zonen[z].Eingang.ThetaSoll[start];
                else
                {
                    s[z] = aussen;
                    unbeheizt = true;
                }
            }
            if (!unbeheizt) return s;

            var neu = new double[n];
            for (int schritt = 0; schritt < STARTWERT_SCHRITTE; schritt++)
            {
                Array.Copy(s, neu, n);
                for (int z = 0; z < n; z++)
                {
                    if (_zonen[z].IstBeheizt) continue;
                    double summe = 0.0;
                    for (int h = start; h < 8760; h++) summe += _zonen[z].ThetaEq(h, s);
                    neu[z] = summe / (8760 - start);
                }
                Array.Copy(neu, s, n);
            }
            return s;
        }

        /// <summary>Die Teilgruppen über Trennflächen der Außengruppe und Luftströme (Union-Find), deterministisch geordnet.</summary>
        internal static int[][] Teilgruppen(IReadOnlyList<ZonenEingang> zonen)
        {
            int n = zonen.Count;
            var eltern = new int[n];
            for (int i = 0; i < n; i++) eltern[i] = i;
            int Wurzel(int i)
            {
                while (eltern[i] != i)
                {
                    eltern[i] = eltern[eltern[i]];
                    i = eltern[i];
                }
                return i;
            }
            void Vereinen(int a, int b)
            {
                int ra = Wurzel(a), rb = Wurzel(b);
                if (ra == rb) return;
                if (ra < rb) eltern[rb] = ra;
                else eltern[ra] = rb;
            }
            for (int i = 0; i < n; i++)
            {
                foreach (int j in zonen[i].NachbarIndex) Vereinen(i, j);
                foreach (int j in zonen[i].LuftIndex) Vereinen(i, j);
            }
            var gruppen = new SortedDictionary<int, List<int>>();
            for (int i = 0; i < n; i++)
            {
                int w = Wurzel(i);
                if (!gruppen.TryGetValue(w, out List<int> l)) gruppen[w] = l = new List<int>();
                l.Add(i);
            }
            return gruppen.Values.Select(l => l.ToArray()).ToArray();
        }
    }
}
