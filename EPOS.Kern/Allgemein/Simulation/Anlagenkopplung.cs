using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein gekoppeltes Gebäude im Kreis</b> (AK3-W3b): sein Gebäude-Stepper und der Faktor vom
    /// Rechenmaßstab des Modells auf das wirkliche Gebäude (Verhältnisrechnung E8; mit Zone 1).
    /// </summary>
    internal sealed class Kopplungsgebaeude
    {
        internal Kopplungsgebaeude(int index, long id, string bezeichnung, GebaeudeStepper stepper, double faktor)
        {
            Index = index;
            Id = id;
            Bezeichnung = bezeichnung ?? "";
            Stepper = stepper ?? throw new ArgumentNullException(nameof(stepper));
            Faktor = faktor > 0.0 && !double.IsInfinity(faktor) ? faktor : 1.0;
        }

        /// <summary>Zeilenindex des Gebäudes im Lauf (Merkplatz der Ergebnisse).</summary>
        internal int Index { get; }

        /// <summary>Gebäude-Id — Ordnung des Rundungsrests der Verteilung.</summary>
        internal long Id { get; }

        /// <summary>Bezeichnung für Meldungen.</summary>
        internal string Bezeichnung { get; }

        /// <summary>Der Stepper (unskaliert, Rechenmaßstab des Modells).</summary>
        internal GebaeudeStepper Stepper { get; }

        /// <summary>Faktor Modell → wirkliches Gebäude (E8), &gt; 0.</summary>
        internal double Faktor { get; set; }
    }

    /// <summary>Das Ergebnis einer gekoppelten Stunde (AK3-W3b).</summary>
    internal sealed class Kopplungsstunde
    {
        /// <summary>Durchläufe der Stunde (1 = der unbegrenzte Probeschritt passte unter die Schranke).</summary>
        internal int Durchlaeufe { get; set; }

        /// <summary>Heizleistung je Gebäude [W], Maßstab des wirklichen Gebäudes (Reihenfolge der Gebäude).</summary>
        internal double[] HeizlastW { get; set; }

        /// <summary>Bedarfsgewichteter Vorlauf der Stunde [°C]; NaN ohne gekoppelten Bedarf.</summary>
        internal double VorlaufC { get; set; }

        /// <summary>Bedarfsgewichteter Rücklauf der Stunde [°C]; NaN ohne gekoppelten Bedarf.</summary>
        internal double RuecklaufC { get; set; }

        /// <summary>Das Angebot des letzten Durchlaufs.</summary>
        internal Stundenangebot Angebot { get; set; }

        /// <summary>true: Die Schranke des Angebots (Leistung oder Vorlauf) hat gegriffen.</summary>
        internal bool SchrankeGreift { get; set; }
    }

    /// <summary>
    /// <b>Benannter Fehler des Kreises</b> (Entwurf AK3 2.4, F-A15): Höchstzahl der Durchläufe oder
    /// Produktschranke der Mehrzonen gerissen — Gebäude, Stunde, Beteiligte, größte verbleibende Abweichung
    /// und letzter Stand; keine stille Näherung.
    /// </summary>
    internal sealed class AnlagenkopplungException : Exception
    {
        internal AnlagenkopplungException(string gebaeude, int stunde, string beteiligte, string abweichung,
                                          string letzterStand)
            : base(string.Format(CultureInfo.InvariantCulture,
                "Anlagenkopplung AK3: Der Kreis konvergiert nicht — Gebäude {0}, Stunde {1}, Beteiligte {2}, " +
                "größte Abweichung {3}, letzter Stand {4}.", gebaeude, stunde, beteiligte, abweichung, letzterStand))
        {
            Gebaeude = gebaeude;
            Stunde = stunde;
            Beteiligte = beteiligte;
            Abweichung = abweichung;
            LetzterStand = letzterStand;
        }

        internal string Gebaeude { get; }
        internal int Stunde { get; }
        internal string Beteiligte { get; }
        internal string Abweichung { get; }
        internal string LetzterStand { get; }
    }

    /// <summary>
    /// <b>Der Iterationsrahmen der Anlagenkopplung AK3</b> (Entwurf AK3 2.3, 2.4; Festlegungen 5 bis 12; E102)
    /// — je Stunde der geschlossene Kreis zwischen den Gebäude-Steppern und der befragbaren Kaskade
    /// (<see cref="Angebotsfunktion"/>). Die echte Kaskadenstunde läuft danach einmal (Festlegung 4).
    /// <list type="number">
    /// <item><b>Durchlauf 1</b> — der unbegrenzte Probeschritt jedes Gebäudes am Projektvorlauf V₀
    /// (Pass 1 bzw. Heizkurve, mit der H2-Naht <see cref="Vorlaufkorrektur"/>). Er ist zugleich der
    /// Verteilschlüssel (Festlegung 9). Liegt sein Bedarf unter dem Angebot S₀ = Angebot(h, V₀) und
    /// verlangt kein Gebäude mehr Vorlauf, als angeboten wird, ist er die Lösung: ein Durchlauf, Zeichen für
    /// Zeichen der Schritt ohne Kopplung (Gate „ohne Grenzen bitgleich zu AK1“).</item>
    /// <item><b>Durchlauf k ≥ 2</b> — S_k verteilt mit <see cref="Stundenverteilung"/> auf Gebäude und
    /// Zonen, je Gebäude <see cref="GebaeudeStepper.Schritt"/> mit der Schranke (Rand
    /// <see cref="Stundenrand.MitVerfuegbarkeit"/>), V_k+1 bedarfsgewichtet (Festlegung 12), S_k+1 neu.</item>
    /// <item><b>Abbruch</b>: |Δθ_i| ≤ 0,01 K, |ΔΦ| ≤ 0,1 W, |ΔV| ≤ 0,05 K, |ΔS| ≤ 0,1 W — oder V und S
    /// unverändert (der nächste Schritt wäre derselbe). Höchstzahl 20, Mehrzonen Zonenzahl × Durchläufe ≤ 120;
    /// darüber <see cref="AnlagenkopplungException"/>.</item>
    /// <item><b>Fallwechsel</b> (2.4): die Stützstelle der Wärmepumpe am Vorlauf und der Betriebsfall je Zone
    /// (<see cref="Stundenergebnis.Begrenzungsgrund"/>) des ersten begrenzten Durchlaufs werden festgehalten;
    /// jeder Wechsel zwischen zwei Durchläufen wird gezählt.</item>
    /// </list>
    /// <para>Übernahme mit <see cref="Festschreiben"/>; den Heizbedarf gibt der Aufrufer über die Naht
    /// <see cref="IStundenbedarf"/> an die Kaskadenstunde. Ohne Datenbank, ohne Protokoll, deterministisch.</para>
    /// </summary>
    internal sealed class Anlagenkopplung
    {
        /// <summary>Abbruchmaß Raumluft [K] (Konzept 6.3).</summary>
        internal const double ABBRUCH_THETA_K = 0.01;
        /// <summary>Abbruchmaß Heizleistung [W].</summary>
        internal const double ABBRUCH_PHI_W = 0.1;
        /// <summary>Abbruchmaß Vorlauf [K].</summary>
        internal const double ABBRUCH_VORLAUF_K = 0.05;
        /// <summary>Abbruchmaß Schranke [W].</summary>
        internal const double ABBRUCH_SCHRANKE_W = 0.1;
        /// <summary>Höchstzahl der Anlagendurchläufe je Stunde (H-F9).</summary>
        internal const int HOECHSTZAHL = 20;
        /// <summary>Höchstes Produkt Zonen × Anlagendurchläufe im Mehrzonenfall.</summary>
        internal const int PRODUKT_MEHRZONEN = 120;

        private readonly Kopplungsgebaeude[] _gebaeude;
        private readonly IReadOnlyList<IErzeugerkapazitaet> _erzeuger;
        private readonly ISpeicherangebot _speicher;
        private readonly long[] _ids;
        private readonly bool[] _alle;

        /// <param name="gebaeude">Die gekoppelten Gebäude (eingeschwungen, <see cref="GebaeudeStepper.Beginnen"/>).</param>
        /// <param name="erzeuger">Die Heizerzeuger als Kapazitäten (Festlegung 5).</param>
        /// <param name="speicher">Der Speicheranteil (Festlegung 6); <c>null</c> = keiner.</param>
        internal Anlagenkopplung(IReadOnlyList<Kopplungsgebaeude> gebaeude, IReadOnlyList<IErzeugerkapazitaet> erzeuger,
                                 ISpeicherangebot speicher)
        {
            if (gebaeude == null || gebaeude.Count == 0) throw new ArgumentException("Der Kreis braucht ein gekoppeltes Gebäude.", nameof(gebaeude));
            _gebaeude = gebaeude.ToArray();
            _erzeuger = erzeuger ?? Array.Empty<IErzeugerkapazitaet>();
            _speicher = speicher;
            _ids = _gebaeude.Select(g => g.Id).ToArray();
            _alle = Enumerable.Repeat(true, _gebaeude.Length).ToArray();
            DurchlaeufeVerteilung = new int[HOECHSTZAHL + 1];
        }

        /// <summary>Die Gebäude des Kreises.</summary>
        internal IReadOnlyList<Kopplungsgebaeude> Gebaeude => _gebaeude;

        /// <summary>
        /// <b>Naht H2</b> (Q-AK3-2, Welle W4): korrigiert den Projektvorlauf V der Stunde (aus den Raumtemperaturen
        /// derselben Stunde). <c>null</c> = keine Korrektur (bis W4).
        /// </summary>
        internal Func<int, double, double> Vorlaufkorrektur { get; set; }

        /// <summary>Stützstelle der Wärmepumpe zum Vorlauf (Fallwechsel); <c>null</c> = nicht gezählt.</summary>
        internal Func<double, int> Stuetzstelle { get; set; }

        /// <summary>Zahl der Stunden je Durchlaufzahl (Index = Durchläufe).</summary>
        internal int[] DurchlaeufeVerteilung { get; }

        /// <summary>Gerechnete Stunden.</summary>
        internal int Stunden { get; private set; }

        /// <summary>Stunden, in denen die Schranke gegriffen hat.</summary>
        internal int StundenAnDerSchranke { get; private set; }

        /// <summary>Gezählte Wechsel der Stützstelle zwischen zwei Durchläufen.</summary>
        internal int StuetzstellenWechsel { get; private set; }

        /// <summary>Gezählte Wechsel des Betriebsfalls einer Zone zwischen zwei begrenzten Durchläufen.</summary>
        internal int FallWechsel { get; private set; }

        /// <summary>Die größte Zahl der Durchläufe einer Stunde.</summary>
        internal int DurchlaeufeMax { get; private set; }

        /// <summary>Mittel der Durchläufe je Stunde.</summary>
        internal double DurchlaeufeMittel
        {
            get
            {
                long s = 0;
                for (int k = 0; k < DurchlaeufeVerteilung.Length; k++) s += (long)k * DurchlaeufeVerteilung[k];
                return Stunden == 0 ? 0.0 : (double)s / Stunden;
            }
        }

        /// <summary>
        /// <b>Die gekoppelte Stunde <paramref name="h"/></b>: iteriert bis zum Abbruch und lässt in jedem Stepper
        /// den Schritt der Lösung offen (<see cref="Festschreiben"/> übernimmt ihn).
        /// </summary>
        /// <param name="h">Die Jahresstunde.</param>
        /// <param name="vorlaufStartC">Der Projektvorlauf zum Start (Pass 1 bzw. Heizkurve); NaN = unbekannt.</param>
        /// <param name="vorrang">Der Vorrang der Stunde (Festlegung 7).</param>
        /// <exception cref="AnlagenkopplungException">Höchstzahl oder Produktschranke gerissen.</exception>
        internal Kopplungsstunde Stunde(int h, double vorlaufStartC, Stundenvorrang vorrang)
        {
            int n = _gebaeude.Length;
            double v = vorlaufStartC;
            if (Vorlaufkorrektur != null) v = Vorlaufkorrektur(h, v);
            Stundenangebot s = Angebotsfunktion.Angebot(h, v, _erzeuger, _speicher, vorrang);

            // Durchlauf 1: der unbegrenzte Probeschritt — Lösung, wenn er unter die Schranke passt.
            var probeW = new double[n];
            var probe = new IReadOnlyList<Stundenergebnis>[n];
            var probeZonen = new IReadOnlyList<double>[n];
            int zonenMax = 1;
            bool vorlaufZuHoch = false;
            for (int i = 0; i < n; i++)
            {
                Kopplungsgebaeude g = _gebaeude[i];
                IReadOnlyList<Stundenergebnis> e = Kopie(g.Stepper.Schritt(h));
                probe[i] = e;
                probeW[i] = HeizlastW(e, g.Faktor);
                if (e.Count > 1) probeZonen[i] = e.Select(z => Math.Max(z.HeizleistungW, 0.0)).ToArray();
                if (e.Count > zonenMax) zonenMax = e.Count;
                foreach (Stundenergebnis z in e)
                    if (!double.IsNaN(s.VorlaufC) && !double.IsNaN(z.VorlaufC) && z.HeizleistungW > 0.0 && s.VorlaufC < z.VorlaufC)
                        vorlaufZuHoch = true;
            }
            double bedarfKw = 0.0;
            for (int i = 0; i < n; i++) bedarfKw += probeW[i] / 1000.0;
            if (!vorlaufZuHoch && bedarfKw <= s.LeistungKw)
                return Abschliessen(1, probe, probeW, s, false);

            // Durchläufe 2 …: die Schranke verteilt nach dem Schlüssel des Probeschritts.
            double[] schluessel = n > 1 ? probeW : null;
            IReadOnlyList<Stundenergebnis>[] vorher = null;
            int stelleVorher = Stuetzstelle != null && !double.IsNaN(v) ? Stuetzstelle(v) : int.MinValue;
            double[] heizW = new double[n];
            for (int k = 2; ; k++)
            {
                if (k > HOECHSTZAHL || (zonenMax > 1 && zonenMax * k > PRODUKT_MEHRZONEN))
                    throw Fehler(h, k - 1, v, s, vorher, zonenMax > 1 && k <= HOECHSTZAHL);

                Anlagenverfuegbarkeit[][] verteilt = Stundenverteilung.Verteilen(s.AlsVerfuegbarkeit(), _ids, _alle, schluessel, probeZonen);
                var jetzt = new IReadOnlyList<Stundenergebnis>[n];
                for (int i = 0; i < n; i++)
                {
                    Kopplungsgebaeude g = _gebaeude[i];
                    Anlagenverfuegbarkeit[] anteil = verteilt[i];
                    double faktor = g.Faktor;
                    jetzt[i] = Kopie(g.Stepper.Schritt(h, (int zone, int stunde, in Stundenrand r) =>
                    {
                        Anlagenverfuegbarkeit a = anteil[zone < anteil.Length ? zone : anteil.Length - 1];
                        return r.MitVerfuegbarkeit(a.LeistungKw * 1000.0 / faktor, a.Grund, a.VorlaufC);
                    }));
                    heizW[i] = HeizlastW(jetzt[i], faktor);
                }

                if (vorher != null) FaelleZaehlen(vorher, jetzt);
                (double vNeu, _) = Kreis(jetzt, Faktoren());
                if (double.IsNaN(vNeu)) vNeu = v;
                if (Vorlaufkorrektur != null) vNeu = Vorlaufkorrektur(h, vNeu);
                Stundenangebot sNeu = Angebotsfunktion.Angebot(h, vNeu, _erzeuger, _speicher, vorrang);
                if (Stuetzstelle != null && !double.IsNaN(vNeu))
                {
                    int stelle = Stuetzstelle(vNeu);
                    if (stelleVorher != int.MinValue && stelle != stelleVorher) StuetzstellenWechsel++;
                    stelleVorher = stelle;
                }

                bool gleich = Gleich(v, vNeu) && sNeu.LeistungKw == s.LeistungKw && Gleich(sNeu.VorlaufC, s.VorlaufC)
                              && sNeu.Grund == s.Grund;
                bool klein = Math.Abs(vNeu - v) <= ABBRUCH_VORLAUF_K
                             && Math.Abs(sNeu.LeistungKw - s.LeistungKw) * 1000.0 <= ABBRUCH_SCHRANKE_W
                             && vorher != null && Abweichung(vorher, jetzt, out _, out _) == 0;
                if (gleich || klein)
                    return Abschliessen(k, jetzt, heizW, s, true);

                vorher = jetzt;
                v = vNeu;
                s = sNeu;
            }
        }

        /// <summary>Übernimmt die Stunde in allen Steppern (Reihen, Zähler, Kreise).</summary>
        internal void Festschreiben(int h)
        {
            foreach (Kopplungsgebaeude g in _gebaeude) g.Stepper.Festschreiben(h);
        }

        // ----------------------------------------------------------------------------------------------

        private double[] Faktoren() => _gebaeude.Select(g => g.Faktor).ToArray();

        private Kopplungsstunde Abschliessen(int durchlaeufe, IReadOnlyList<Stundenergebnis>[] loesung, double[] heizW,
                                             Stundenangebot s, bool schranke)
        {
            Stunden++;
            DurchlaeufeVerteilung[durchlaeufe]++;
            if (durchlaeufe > DurchlaeufeMax) DurchlaeufeMax = durchlaeufe;
            if (schranke) StundenAnDerSchranke++;
            // Die offenen Schritte der Stepper sind die Lösung; Vorlauf und Rücklauf aus ihnen (Festlegung 12).
            (double vor, double rueck) = Kreis(loesung, Faktoren());
            return new Kopplungsstunde
            {
                Durchlaeufe = durchlaeufe,
                HeizlastW = (double[])heizW.Clone(),
                Angebot = s,
                SchrankeGreift = schranke,
                VorlaufC = vor,
                RuecklaufC = rueck,
            };
        }

        /// <summary>Heizleistung eines Gebäudes [W] im Maßstab des wirklichen Gebäudes: Σ max(Φ_h,z, 0) × Faktor.</summary>
        internal static double HeizlastW(IReadOnlyList<Stundenergebnis> e, double faktor)
        {
            if (e.Count == 1) return e[0].HeizleistungW * faktor;
            double s = 0.0;
            foreach (Stundenergebnis z in e) if (z.HeizleistungW > 0.0) s += z.HeizleistungW;
            return s * faktor;
        }

        /// <summary>
        /// Bedarfsgewichteter Vorlauf und Rücklauf über alle Zonen der Gebäude, Gewicht die Heizleistung im Maßstab des
        /// wirklichen Gebäudes (Regel <see cref="Kreisprojekt"/>; ein Beitrag: seine Zahlen ohne Rundung).
        /// </summary>
        internal static (double VorlaufC, double RuecklaufC) Kreis(IReadOnlyList<IReadOnlyList<Stundenergebnis>> e, IReadOnlyList<double> faktor)
        {
            double gewicht = 0.0, gv = 0.0, gr = 0.0;
            int beitraege = 0;
            double einV = double.NaN, einR = double.NaN;
            for (int i = 0; i < e.Count; i++)
                foreach (Stundenergebnis z in e[i])
                {
                    double q = z.HeizleistungW * faktor[i];
                    if (!(q > 0.0) || double.IsNaN(z.VorlaufC)) continue;
                    beitraege++;
                    einV = z.VorlaufC;
                    einR = z.RuecklaufC;
                    gewicht += q;
                    gv += q * z.VorlaufC;
                    gr += q * z.RuecklaufC;
                }
            if (beitraege == 0) return (double.NaN, double.NaN);
            return beitraege == 1 ? (einV, einR) : (gv / gewicht, gr / gewicht);
        }

        private static IReadOnlyList<Stundenergebnis> Kopie(IReadOnlyList<Stundenergebnis> e) => e.ToArray();

        private static bool Gleich(double a, double b) => a.Equals(b);

        /// <summary>Zahl der Zonen, deren θ oder Φ über dem Abbruchmaß wechselt; dazu die größten Abweichungen.</summary>
        private static int Abweichung(IReadOnlyList<Stundenergebnis>[] a, IReadOnlyList<Stundenergebnis>[] b,
                                      out double dTheta, out double dPhi)
        {
            int zahl = 0;
            dTheta = 0.0;
            dPhi = 0.0;
            for (int i = 0; i < a.Length; i++)
                for (int z = 0; z < a[i].Count && z < b[i].Count; z++)
                {
                    double t = Math.Abs(a[i][z].ThetaAirMittel - b[i][z].ThetaAirMittel);
                    double p = Math.Abs(a[i][z].HeizleistungW - b[i][z].HeizleistungW);
                    if (t > dTheta) dTheta = t;
                    if (p > dPhi) dPhi = p;
                    if (t > ABBRUCH_THETA_K || p > ABBRUCH_PHI_W) zahl++;
                }
            return zahl;
        }

        private void FaelleZaehlen(IReadOnlyList<Stundenergebnis>[] vorher, IReadOnlyList<Stundenergebnis>[] jetzt)
        {
            for (int i = 0; i < vorher.Length; i++)
                for (int z = 0; z < vorher[i].Count && z < jetzt[i].Count; z++)
                    if (vorher[i][z].Begrenzungsgrund != jetzt[i][z].Begrenzungsgrund) FallWechsel++;
        }

        private AnlagenkopplungException Fehler(int h, int durchlaeufe, double v, Stundenangebot s,
                                                IReadOnlyList<Stundenergebnis>[] letzte, bool produkt)
        {
            string gebaeude = string.Join(", ", _gebaeude.Select(g => g.Bezeichnung + " (" + g.Id.ToString(CultureInfo.InvariantCulture) + ")"));
            string beteiligte = string.Join(", ", _erzeuger.Select(e => e.Bezeichner)) + (_speicher != null && _speicher.Vorhanden ? ", Speicher" : "");
            double dt = 0.0, dp = 0.0;
            if (letzte != null) Abweichung(letzte, letzte, out dt, out dp);
            string abw = string.Format(CultureInfo.InvariantCulture, "θ {0:0.###} K, Φ {1:0.###} W{2}", dt, dp,
                                       produkt ? " (Produkt Zonen × Durchläufe > " + PRODUKT_MEHRZONEN + ")" : "");
            string stand = string.Format(CultureInfo.InvariantCulture, "{0} Durchläufe, Vorlauf {1:0.##} °C, Schranke {2:0.###} kW",
                                         durchlaeufe, v, s.LeistungKw);
            return new AnlagenkopplungException(gebaeude, h, beteiligte, abw, stand);
        }
    }
}
