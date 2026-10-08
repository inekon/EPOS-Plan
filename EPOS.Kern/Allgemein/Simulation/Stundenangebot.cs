using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    // =========================================================================================
    //  AK3-W3a — die befragbare Kaskade (Entwurf AK3 2.3 bis 2.5, Festlegungen 5 bis 8, E102 Q-AK3-3)
    //
    //  Angebot(h, V) = Σ Kapazität der in h verfügbaren Heizerzeuger beim Vorlauf V      (Festlegung 5)
    //                + Σ aus den Heizungspuffern entnehmbar am Stundenbeginn              (Festlegung 6)
    //                − Vorrang der Stunde (Brauchwasser, Prozess, Netzverlust, Altweg-Last) (Festlegung 7)
    //
    //  Alles hier ist eine ABFRAGE ohne Nebenwirkung: keine Zählung, keine Rücknahme, kein Speicherzustand
    //  (Festlegungen 4 und 8). Im heutigen Lauf ruft niemand diese Klassen — der Kreis (Klasse
    //  Anlagenkopplung) und die Stufe AK3 kommen mit W3b.
    // =========================================================================================

    /// <summary>
    /// <b>Die Naht Erzeugerkapazität</b> (AK3-W3a, Festlegung 5): Was ein Heizerzeuger in Stunde h beim
    /// Vorlauf V anbieten kann. Die Abfrage ist zustandsfrei — beliebig oft je Stunde, ohne zu zählen und
    /// ohne etwas zurückzunehmen. Die Proben der Orakel O1/O2 (W3b) setzen hier Testdoubles ein.
    /// </summary>
    internal interface IErzeugerkapazitaet
    {
        /// <summary>Bezeichner der Anlage — für Meldungen.</summary>
        string Bezeichner { get; }

        /// <summary>Das Angebot des Erzeugers in Stunde <paramref name="stunde"/> beim Vorlauf <paramref name="vorlaufC"/>.</summary>
        Erzeugerangebot Abfragen(int stunde, double vorlaufC);
    }

    /// <summary>Das Angebot eines Erzeugers in einer Stunde (AK3-W3a).</summary>
    internal readonly struct Erzeugerangebot
    {
        internal Erzeugerangebot(double kapazitaetKw, double verfuegbarKw, Verfuegbarkeitsgrund grund,
                                 bool freigegeben, double vorlaufAngebotC, bool vorlaufNichtErreicht,
                                 double ausfallZeitprogrammKw = double.NaN)
        {
            KapazitaetKw = kapazitaetKw;
            VerfuegbarKw = verfuegbarKw;
            Grund = grund;
            Freigegeben = freigegeben;
            VorlaufAngebotC = vorlaufAngebotC;
            VorlaufNichtErreicht = vorlaufNichtErreicht;
            AusfallZeitprogrammKw = ausfallZeitprogrammKw;
        }

        /// <summary>
        /// AK3-W3b (offener Punkt aus W3a): der Teil des Ausfalls, den das Zeitprogramm verursacht [kW], wenn in
        /// derselben Stunde auch der Abschaltpunkt greift — der Rest des Ausfalls gehört dann dem Abschaltpunkt,
        /// dieselbe Grundaufteilung wie <see cref="Anlagenfahrplan"/>. NaN = der ganze Ausfall gehört
        /// <see cref="Grund"/> (Testdoubles, eine Ursache).
        /// </summary>
        internal double AusfallZeitprogrammKw { get; }

        /// <summary>Kapazität beim Vorlauf, ohne Fahrplan [kW], ≥ 0.</summary>
        internal double KapazitaetKw { get; }

        /// <summary>Was der Fahrplan davon in dieser Stunde freigibt [kW], 0 … <see cref="KapazitaetKw"/>.</summary>
        internal double VerfuegbarKw { get; }

        /// <summary>
        /// Warum <see cref="VerfuegbarKw"/> unter der Kapazität liegt (Sperrzeit, Zeitprogramm, Abschaltpunkt);
        /// <see cref="Verfuegbarkeitsgrund.KeineBegrenzung"/> sonst, auch wenn allein der Vorlauf fehlt.
        /// </summary>
        internal Verfuegbarkeitsgrund Grund { get; }

        /// <summary>true: Der Fahrplan lässt den Erzeuger in der Stunde (ganz oder teilweise) liefern.</summary>
        internal bool Freigegeben { get; }

        /// <summary>Das Vorlaufangebot des Erzeugers [°C], NaN = keine Grenze.</summary>
        internal double VorlaufAngebotC { get; }

        /// <summary>true: Der Erzeuger erreicht den verlangten Vorlauf nicht (Kapazität 0).</summary>
        internal bool VorlaufNichtErreicht { get; }
    }

    /// <summary>
    /// <b>Gemeinsamer Teil der Kapazitäten:</b> die Verfügbarkeit aus dem Fahrplan AK2 (Sperre geht vor, sonst
    /// Faktor des Zeitprogramms, 0 unter dem Abschaltpunkt — dieselbe Regel wie <see cref="Anlagenfahrplan"/>)
    /// und das Vorlaufangebot (<c>Vorlauf_Max</c>, Rückfall <c>Vorlauf</c>). Ein Erzeuger, dessen
    /// Vorlaufangebot unter dem verlangten Vorlauf liegt, liefert bei diesem Vorlauf nichts.
    /// </summary>
    internal abstract class Fahrplankapazitaet : IErzeugerkapazitaet
    {
        protected Fahrplankapazitaet(Fahrplanerzeuger fahrplan)
        {
            Fahrplan = fahrplan ?? throw new ArgumentNullException(nameof(fahrplan));
        }

        /// <summary>Masken und Vorlaufangebot des Erzeugers (AK2); die Nennleistung liest nur der feste Erzeuger.</summary>
        internal Fahrplanerzeuger Fahrplan { get; }

        public string Bezeichner => Fahrplan.Bezeichner;

        /// <summary>Das Vorlaufangebot [°C], NaN = keine Grenze.</summary>
        internal virtual double VorlaufAngebotC => Fahrplan.VorlaufAngebotC;

        /// <summary>
        /// Ist der Heizkanal des Erzeugers in Stunde <paramref name="stunde"/> umgeschaltet (AK3-K, Fehler 1.1 (b))? Dann
        /// gibt der Fahrplan nichts frei, Grund <see cref="Verfuegbarkeitsgrund.Umschaltung"/>. Vorgabe: nie.
        /// </summary>
        protected virtual bool Umgeschaltet(int stunde) => false;

        /// <summary>Die Kapazität beim Vorlauf ohne Fahrplan [kW]; 0, wenn der Erzeuger den Vorlauf nicht stellt.</summary>
        protected abstract double KapazitaetBei(int stunde, double vorlaufC, out bool vorlaufNichtErreicht);

        public Erzeugerangebot Abfragen(int stunde, double vorlaufC)
        {
            double faktor = Verfuegbarkeit(Fahrplan, stunde, out Verfuegbarkeitsgrund grund, out double zeitprogrammAnteil);
            if (faktor > 0.0 && Umgeschaltet(stunde))
            {
                // AK3-K (Fehler 1.1 (b), K8a): Die reversible Wärmepumpe ist am Kühltag Kältemaschine.
                faktor = 0.0;
                grund = Verfuegbarkeitsgrund.Umschaltung;
                zeitprogrammAnteil = 0.0;
            }
            double angebot = VorlaufAngebotC;
            bool nichtErreicht = false;
            double kapazitaet;
            if (!double.IsNaN(vorlaufC) && !double.IsNaN(angebot) && !Rechenrand.SchwelleErreicht(angebot, vorlaufC))
            {
                kapazitaet = 0.0;
                nichtErreicht = true;
            }
            else
            {
                kapazitaet = KapazitaetBei(stunde, vorlaufC, out nichtErreicht);
                if (!(kapazitaet > 0.0) || double.IsInfinity(kapazitaet)) kapazitaet = 0.0;
            }
            return new Erzeugerangebot(kapazitaet, faktor * kapazitaet, grund, faktor > 0.0, angebot, nichtErreicht,
                                       zeitprogrammAnteil * kapazitaet);
        }

        /// <summary>
        /// Die Verfügbarkeit des Erzeugers in Stunde <paramref name="stunde"/> (0 … 1) nach der Regel des
        /// <see cref="Anlagenfahrplan"/>: 0 in einer Sperrstunde (Grund Sperrzeit, geht vor), sonst der Faktor
        /// des Zeitprogramms (Grund Zeitprogramm unter 1), 0 unter dem Abschaltpunkt (Grund Abschaltpunkt).
        /// </summary>
        internal static double Verfuegbarkeit(Fahrplanerzeuger e, int stunde, out Verfuegbarkeitsgrund grund)
            => Verfuegbarkeit(e, stunde, out grund, out _);

        /// <summary>
        /// Wie <see cref="Verfuegbarkeit(Fahrplanerzeuger, int, out Verfuegbarkeitsgrund)"/>, dazu der Anteil der
        /// Kapazität, den das Zeitprogramm wegnimmt (0 … 1): Greifen Zeitprogramm und Abschaltpunkt in derselben
        /// Stunde, zählt <c>1 − f</c> beim Zeitprogramm und <c>f</c> beim Abschaltpunkt — die Grundaufteilung des
        /// <see cref="Anlagenfahrplan"/>; die Angebotsfunktion summiert beide Teile je Grund über die Erzeuger.
        /// </summary>
        internal static double Verfuegbarkeit(Fahrplanerzeuger e, int stunde, out Verfuegbarkeitsgrund grund,
                                              out double zeitprogrammAnteil)
        {
            grund = Verfuegbarkeitsgrund.KeineBegrenzung;
            zeitprogrammAnteil = 0.0;
            if (e.Gesperrt != null && e.Gesperrt[stunde])
            {
                grund = Verfuegbarkeitsgrund.Sperrzeit;
                return 0.0;
            }
            double faktor = e.Zeitprogramm != null ? e.Zeitprogramm.Faktor(stunde) : 1.0;
            if (faktor < 1.0)
            {
                grund = Verfuegbarkeitsgrund.Zeitprogramm;
                zeitprogrammAnteil = 1.0 - faktor;
            }
            if (faktor > 0.0 && e.Abgeschaltet != null && e.Abgeschaltet[stunde])
            {
                // Wie im Fahrplan: der freigegebene Teil fällt unter dem Abschaltpunkt aus; der größere
                // Ausfallteil benennt den Grund des Erzeugers.
                if (faktor >= 1.0 - faktor) grund = Verfuegbarkeitsgrund.Abschaltpunkt;
                faktor = 0.0;
            }
            return faktor;
        }
    }

    /// <summary>
    /// <b>Fester Erzeuger</b> (Festlegung 5): Heizkessel mit <c>Ptherm</c>, BHKW mit <c>Ptherm</c> bzw. der
    /// Grenzleistung nach der Rangfolge Anlage, Katalog, Projekt — die Zahl steht in
    /// <see cref="Fahrplanerzeuger.NennleistungKw"/>, die Datenbankseite füllt sie (W3b). Ohne Temperaturbezug
    /// (Festlegung 19): Die Kapazität hängt nicht am Vorlauf, nur am Vorlaufangebot.
    /// </summary>
    internal sealed class FesteKapazitaet : Fahrplankapazitaet
    {
        internal FesteKapazitaet(Fahrplanerzeuger fahrplan) : base(fahrplan) { }

        protected override double KapazitaetBei(int stunde, double vorlaufC, out bool vorlaufNichtErreicht)
        {
            vorlaufNichtErreicht = false;
            return Fahrplan.NennleistungKw;
        }
    }

    /// <summary>
    /// <b>Die Quelltemperatur einer Wärmepumpe am Stundenbeginn</b> (Festlegung 21, Naht für R40): ohne
    /// Nebenwirkung gelesen. Das Jahresprofil der Quelle liefert <see cref="Quellprofil"/>; eine Sole-Wärmepumpe am
    /// Erdsondenfeld (R40) liest den Feldzustand am Stundenbeginn über <see cref="Sondenquelle"/> (AK3-W3d).
    /// </summary>
    internal interface IQuellzustand
    {
        /// <summary>Quelltemperatur [°C] in Stunde <paramref name="stunde"/>, wie sie am Stundenbeginn steht.</summary>
        double TemperaturAmStundenbeginn(int stunde);

        /// <summary>
        /// true für eine temperaturgekoppelte Pufferquelle (Booster, F13): unter der untersten Stützstelle der
        /// Quellachse wird gekappt statt extrapoliert — dieselbe Regel wie <c>berechne_wptherm</c>.
        /// </summary>
        bool KapptUnten { get; }
    }

    /// <summary>Die Quelle als Jahresprofil (8760 Werte) — die Reihe, die der Bestand je Modul führt.</summary>
    internal sealed class Quellprofil : IQuellzustand
    {
        private readonly double[] _reihe;

        internal Quellprofil(double[] reihe, bool kapptUnten = false)
        {
            _reihe = reihe ?? throw new ArgumentNullException(nameof(reihe));
            KapptUnten = kapptUnten;
        }

        public double TemperaturAmStundenbeginn(int stunde) => _reihe[stunde];

        public bool KapptUnten { get; }
    }

    /// <summary>
    /// <b>Die Wärmepumpe als Kapazität</b> (Festlegungen 5, 8, 11; I-1, I-2): <c>Ptherm</c> aus der Kennlinie an der
    /// Stützstelle zum Vorlauf und an der Quelltemperatur der Stunde — nicht aus <c>Tab_WP.Nennleistung</c>.
    /// Die Stützstelle wählt <see cref="SimulationWaermepumpe.VorlaufAuswerten"/> (<c>StuetzstelleWaehlen</c>),
    /// nicht die zählende <c>Kennlinienwahl</c>: gezählt wird allein in der echten Kaskadenstunde
    /// (Festlegung 8). Mit der Interpolation über den Vorlauf (<see cref="VorlaufInterpolation"/>, gilt)
    /// und einem Vorlauf streng zwischen zwei Stützstellen wird <c>Ptherm</c> beider Kennlinien linear im Vorlauf
    /// gewichtet (I-1). Ränder (F-A8): unter der untersten die unterste Kennlinie, über der obersten bei
    /// erlaubter Extrapolation die oberste, bei verbotener Kapazität 0 — die Wärmepumpe stellt den Vorlauf nicht
    /// (Festlegung 11: ihr Vorlaufangebot endet an der obersten Stützstelle).
    /// </summary>
    internal sealed class WaermepumpeKapazitaet : Fahrplankapazitaet
    {
        private readonly SimulationWaermepumpe._Kenndaten[] _kurven;
        private readonly int[] _vorlaeufe;
        private readonly SimulationWaermepumpe._Kenndaten _fest;
        private readonly IQuellzustand _quelle;
        private readonly bool _extrapolationErlaubt;

        /// <param name="fahrplan">Masken und Vorlaufangebot (AK2).</param>
        /// <param name="kurven">Die Kennlinien des Geräts, aufsteigend nach Vorlauf (wie <c>KennlinienwahlLaden</c>).</param>
        /// <param name="fest">Die Kennlinie des projektierten Vorlaufs — gilt in Stunden ohne Vorlauf (NaN); <c>null</c> = unterste.</param>
        /// <param name="quelle">Die Quelltemperatur am Stundenbeginn (Naht R40).</param>
        /// <param name="extrapolationErlaubt"><c>Tab_Einstellungen.Extrapolation_erlaubt</c>.</param>
        /// <param name="anzahl">Zahl gleicher Module der Anlagenzeile (≥ 1).</param>
        /// <param name="interpolieren">Interpolation über den Vorlauf (I-1, gilt); <c>false</c> allein für Proben der Stützstellenwahl.</param>
        internal WaermepumpeKapazitaet(Fahrplanerzeuger fahrplan, IReadOnlyList<SimulationWaermepumpe._Kenndaten> kurven,
                                       SimulationWaermepumpe._Kenndaten fest, IQuellzustand quelle, bool extrapolationErlaubt,
                                       int anzahl = 1, bool interpolieren = true)
            : base(fahrplan)
        {
            if (kurven == null || kurven.Count == 0) throw new ArgumentException("Die Wärmepumpe braucht eine Kennlinie.", nameof(kurven));
            _kurven = kurven.ToArray();
            _vorlaeufe = _kurven.Select(k => k.Vorlauf).ToArray();
            _fest = fest ?? _kurven[0];
            _quelle = quelle ?? throw new ArgumentNullException(nameof(quelle));
            _extrapolationErlaubt = extrapolationErlaubt;
            Anzahl = anzahl > 1 ? anzahl : 1;
            Interpolieren = interpolieren;
        }

        /// <summary>Zahl gleicher Module.</summary>
        internal int Anzahl { get; }

        /// <summary>
        /// <b>Die Heizsperre am Kühltag</b> (AK3-K, Fehler 1.1 (b); Kühlkonzept 5.2, K8a): true in einer Stunde, in der
        /// das Modul im Kühlbetrieb Kältemaschine ist (<see cref="SimulationWaermepumpe.HeizkanalGesperrt"/>). <c>null</c> =
        /// keine Sperre.
        /// </summary>
        internal Func<int, bool> Kuehltag { get; set; }

        protected override bool Umgeschaltet(int stunde) => Kuehltag != null && Kuehltag(stunde);

        /// <summary>Die Quelle der Stunde (Jahresprofil oder Feldzustand der Erdsonde); für Proben.</summary>
        internal IQuellzustand Quelle => _quelle;

        /// <summary>
        /// Die Stützstelle zum Vorlauf (Index in die Kennlinien, <c>StuetzstelleWaehlen</c>, ohne Zählung) — für den
        /// Fallwechsel des Kreises (Entwurf AK3 2.4); −1 bei verbotener Extrapolation über der obersten.
        /// </summary>
        internal int Stuetzstelle(double vorlaufC)
        {
            var lage = SimulationWaermepumpe.VorlaufAuswerten(_vorlaeufe, vorlaufC, _extrapolationErlaubt, out int stelle);
            return lage == SimulationWaermepumpe.Vorlauflage.Verboten ? -1 : stelle;
        }

        /// <summary>Ob über den Vorlauf interpoliert wird (I-1).</summary>
        internal bool Interpolieren { get; }

        /// <summary>
        /// Das Vorlaufangebot (N-A8, Festlegung 11): <c>Vorlauf_Max</c> bzw. <c>Vorlauf</c> der Anlage; bei verbotener
        /// Extrapolation höchstens die oberste Stützstelle.
        /// </summary>
        internal override double VorlaufAngebotC
        {
            get
            {
                double a = Fahrplan.VorlaufAngebotC;
                if (_extrapolationErlaubt) return a;
                double oben = _vorlaeufe[_vorlaeufe.Length - 1];
                return double.IsNaN(a) ? oben : Math.Min(a, oben);
            }
        }

        protected override double KapazitaetBei(int stunde, double vorlaufC, out bool vorlaufNichtErreicht)
        {
            vorlaufNichtErreicht = false;
            double tq = _quelle.TemperaturAmStundenbeginn(stunde);
            // AK3-W3b (offener Punkt aus W3a): an der Quellachse wie die Kaskade — ist die Extrapolation
            // verboten, wird unter der untersten Quelltemperatur nicht verlängert (der Lauf bricht dort ohnehin
            // benannt ab); die gekoppelte Pufferquelle kappt immer.
            bool kappen = _quelle.KapptUnten || !_extrapolationErlaubt;
            if (double.IsNaN(vorlaufC) || double.IsInfinity(vorlaufC))
                return Anzahl * Leistung(_fest, tq, kappen);

            var lage = SimulationWaermepumpe.VorlaufAuswerten(_vorlaeufe, vorlaufC, _extrapolationErlaubt, out int stelle);
            if (lage == SimulationWaermepumpe.Vorlauflage.Verboten)
            {
                vorlaufNichtErreicht = true;
                return 0.0;
            }
            if (Interpolieren && lage == SimulationWaermepumpe.Vorlauflage.Innerhalb
                && VorlaufInterpolation.Einschliessend(_vorlaeufe, vorlaufC, out int unten, out double gewicht))
            {
                double pu = Leistung(_kurven[unten], tq, kappen);
                double po = Leistung(_kurven[unten + 1], tq, kappen);
                return Anzahl * (pu + gewicht * (po - pu));
            }
            return Anzahl * Leistung(_kurven[stelle], tq, kappen);
        }

        /// <summary>
        /// <c>Ptherm</c> einer Kennlinie an der Quelltemperatur [kW] — dieselbe Rechnung wie
        /// <c>SimulationWaermepumpe.berechne_wptherm</c> (Kappung oben; unten Kappung für die gekoppelte
        /// Pufferquelle, sonst lineare Verlängerung der beiden untersten Punkte; dazwischen linear), aber ohne
        /// Zähler, ohne Meldung und ohne Abbruch: Ob eine Extrapolation verboten ist, entscheidet die echte
        /// Kaskadenstunde.
        /// </summary>
        internal static double Leistung(SimulationWaermepumpe._Kenndaten k, double temperatur, bool kapptUnten)
        {
            int anz = k.anz;
            if (anz < 2) return k.dat[0].Leistung;
            if (temperatur >= k.dat[0].Temperatur) return k.dat[0].Leistung;
            if (temperatur < k.dat[anz - 1].Temperatur)
            {
                if (kapptUnten) return k.dat[anz - 1].Leistung;
                return SimulationWaermepumpe.Interp(
                    new[] { k.dat[anz - 1].Temperatur, k.dat[anz - 2].Temperatur },
                    new[] { k.dat[anz - 1].Leistung, k.dat[anz - 2].Leistung },
                    new[] { temperatur, 0.0 });
            }
            for (int i = 1; i < anz; i++)
            {
                if (temperatur >= k.dat[i].Temperatur)
                    return SimulationWaermepumpe.Interp(
                        new[] { k.dat[i - 1].Temperatur, k.dat[i].Temperatur },
                        new[] { k.dat[i - 1].Leistung, k.dat[i].Leistung },
                        new[] { temperatur, 0.0 });
            }
            return 0.0;
        }
    }

    /// <summary>
    /// <b>Der Speicheranteil des Angebots</b> (Festlegung 6): was die Heizungspuffer am Stundenbeginn hergeben
    /// [kWh je Stunde = kW]. Naht für Proben neben <see cref="IErzeugerkapazitaet"/>.
    /// </summary>
    internal interface ISpeicherangebot
    {
        /// <summary>true, wenn mindestens ein Heizungspuffer teilnimmt.</summary>
        bool Vorhanden { get; }

        /// <summary>Σ entnehmbar am Beginn der Stunde <paramref name="stunde"/> [kWh].</summary>
        double EntnehmbarKwh(int stunde);
    }

    /// <summary>
    /// <b>Der Speicherleser</b> (Entwurf AK3 2.5): liest je Heizungspuffer
    /// <see cref="SimulationPufferspeicher.EntnehmbarAmStundenbeginn"/> — den Stand nach
    /// <c>StundeAbschliessen(h − 1)</c> — und verändert keinen Speicher. Teil nehmen die Puffer, die den Heizkanal
    /// bedienen. Der Profilweg (AK1/AK2: Vorrat Σ <c>Q_max</c> je Sperrblock) bleibt, wie er ist.
    /// </summary>
    internal sealed class Speicherleser : ISpeicherangebot
    {
        private readonly SimulationPufferspeicher[] _puffer;

        internal Speicherleser(IEnumerable<SimulationPufferspeicher> speicher)
        {
            _puffer = (speicher ?? Enumerable.Empty<SimulationPufferspeicher>())
                .Where(s => s != null && s.BedientKanal(Kanal.HEIZUNG)).ToArray();
        }

        /// <summary>Die teilnehmenden Heizungspuffer.</summary>
        internal IReadOnlyList<SimulationPufferspeicher> Puffer => _puffer;

        public bool Vorhanden => _puffer.Length > 0;

        public double EntnehmbarKwh(int stunde)
        {
            double summe = 0.0;
            foreach (SimulationPufferspeicher sp in _puffer)
            {
                double e = sp.EntnehmbarAmStundenbeginn(stunde, Kanal.HEIZUNG);
                if (e > 0.0) summe += e;
            }
            return summe;
        }
    }

    /// <summary>
    /// <b>Der Vorrang der Stunde</b> (Festlegung 7) [kW]: Brauchwasser, Prozess, Netzverlust der Stunde und die
    /// Last der Altweg-Gebäude (Festlegung 17) zehren zuerst; der Rest ist die Schranke der Heizung.
    /// </summary>
    internal readonly struct Stundenvorrang
    {
        internal Stundenvorrang(double brauchwasserKw, double prozessKw, double netzverlustKw, double altwegLastKw)
        {
            BrauchwasserKw = brauchwasserKw;
            ProzessKw = prozessKw;
            NetzverlustKw = netzverlustKw;
            AltwegLastKw = altwegLastKw;
        }

        internal double BrauchwasserKw { get; }
        internal double ProzessKw { get; }
        internal double NetzverlustKw { get; }
        internal double AltwegLastKw { get; }

        /// <summary>Die Summe der vier Teile; negative oder nicht endliche Teile zählen als 0.</summary>
        internal double SummeKw => Gueltig(BrauchwasserKw) + Gueltig(ProzessKw) + Gueltig(NetzverlustKw) + Gueltig(AltwegLastKw);

        private static double Gueltig(double x) => x > 0.0 && !double.IsInfinity(x) ? x : 0.0;
    }

    /// <summary>Das Angebot einer Stunde und seine Teile (AK3-W3a).</summary>
    internal readonly struct Stundenangebot
    {
        internal Stundenangebot(double erzeugerKw, double speicherKw, double vorrangKw, double vorlaufC,
                                Verfuegbarkeitsgrund grund)
        {
            ErzeugerKw = erzeugerKw;
            SpeicherKw = speicherKw;
            VorrangKw = vorrangKw;
            VorlaufC = vorlaufC;
            Grund = grund;
            double s = erzeugerKw + speicherKw - vorrangKw;
            LeistungKw = s > 0.0 ? s : 0.0;
        }

        /// <summary>Σ verfügbare Kapazität der Heizerzeuger beim Vorlauf [kW].</summary>
        internal double ErzeugerKw { get; }

        /// <summary>Σ aus den Heizungspuffern entnehmbar am Stundenbeginn [kW].</summary>
        internal double SpeicherKw { get; }

        /// <summary>Der Vorrang der Stunde [kW].</summary>
        internal double VorrangKw { get; }

        /// <summary>Die Schranke der Heizung: max(0, Erzeuger + Speicher − Vorrang) [kW].</summary>
        internal double LeistungKw { get; }

        /// <summary>Höchstes Vorlaufangebot der freigegebenen Erzeuger [°C]; NaN = keine Grenze bekannt.</summary>
        internal double VorlaufC { get; }

        /// <summary>Der Grund mit dem größten Ausfall (wie im Fahrplan); ohne Ausfall <see cref="Verfuegbarkeitsgrund.KeineBegrenzung"/>.</summary>
        internal Verfuegbarkeitsgrund Grund { get; }

        /// <summary>Das Angebot als Naht ins Gebäude (<see cref="Anlagenverfuegbarkeit"/>).</summary>
        internal Anlagenverfuegbarkeit AlsVerfuegbarkeit() => new Anlagenverfuegbarkeit(LeistungKw, VorlaufC, Grund);
    }

    /// <summary>
    /// <b>Die Angebotsfunktion</b> <c>Angebot(h, V)</c> (Entwurf AK3 2.3, Festlegungen 5 bis 8; E102 Q-AK3-3) —
    /// eine zustandsfreie Abfrage der befragbaren Kaskade.
    /// <list type="bullet">
    /// <item><b>Erzeuger:</b> Σ <see cref="Erzeugerangebot.VerfuegbarKw"/> aller Heizerzeuger beim Vorlauf V.</item>
    /// <item><b>Speicher:</b> <see cref="ISpeicherangebot.EntnehmbarKwh"/> am Stundenbeginn (eine Stunde = kW).</item>
    /// <item><b>Vorrang:</b> <see cref="Stundenvorrang.SummeKw"/>; reicht das Angebot dafür nicht, ist die Schranke 0.</item>
    /// <item><b>Vorlauf:</b> das höchste Vorlaufangebot der freigegebenen Erzeuger; trägt einer davon keines, NaN.</item>
    /// <item><b>Grund</b> (Regel des Fahrplans): der Ausfall mit der größten Leistung; den Ausfall der Sperrzeit füllt
    /// der Speicher — reicht er nicht, heißt der Rest <see cref="Verfuegbarkeitsgrund.SpeicherLeer"/> (2.5). Ohne
    /// Erzeuger <see cref="Verfuegbarkeitsgrund.KeinErzeuger"/>. Ein Erzeuger, der nur den Vorlauf nicht erreicht,
    /// setzt keinen Grund — die Vorlaufgrenze reist als <see cref="Stundenangebot.VorlaufC"/> (wie AK2).</item>
    /// </list>
    /// </summary>
    internal static class Angebotsfunktion
    {
        internal static Stundenangebot Angebot(int stunde, double vorlaufC, IReadOnlyList<IErzeugerkapazitaet> erzeuger,
                                               ISpeicherangebot speicher, Stundenvorrang vorrang)
        {
            double speicherKw = speicher != null && speicher.Vorhanden ? speicher.EntnehmbarKwh(stunde) : 0.0;
            if (!(speicherKw > 0.0)) speicherKw = 0.0;
            double vorrangKw = vorrang.SummeKw;

            // AK3-W3b (offener Punkt aus W3a): ohne Erzeuger wie der Fahrplan — Leistung 0, kein Speicheranteil
            // (der Vorrat füllt dort nur die Sperrlücke eines Erzeugers).
            if (erzeuger == null || erzeuger.Count == 0)
                return new Stundenangebot(0.0, 0.0, vorrangKw, double.NaN, Verfuegbarkeitsgrund.KeinErzeuger);

            int gruende = Enum.GetValues(typeof(Verfuegbarkeitsgrund)).Length;
            var ausfall = new double[gruende];
            double summe = 0.0, hoechsterVorlauf = double.NegativeInfinity;
            bool ohneAngebot = false;
            foreach (IErzeugerkapazitaet e in erzeuger)
            {
                Erzeugerangebot a = e.Abfragen(stunde, vorlaufC);
                summe += a.VerfuegbarKw;
                double weg = a.KapazitaetKw - a.VerfuegbarKw;
                if (weg > 0.0)
                {
                    // Zeitprogramm und Abschaltpunkt in derselben Stunde: Aufteilung wie im Fahrplan.
                    double zp = a.AusfallZeitprogrammKw;
                    if (a.Grund != Verfuegbarkeitsgrund.Sperrzeit && zp > 0.0 && zp < weg)
                    {
                        ausfall[(int)Verfuegbarkeitsgrund.Zeitprogramm] += zp;
                        ausfall[(int)Verfuegbarkeitsgrund.Abschaltpunkt] += weg - zp;
                    }
                    else ausfall[(int)a.Grund] += weg;
                }
                if (a.Freigegeben)
                {
                    if (double.IsNaN(a.VorlaufAngebotC)) ohneAngebot = true;
                    else hoechsterVorlauf = Math.Max(hoechsterVorlauf, a.VorlaufAngebotC);
                }
            }

            // Den Ausfall der Sperrzeit füllt der Speicher am Stundenbeginn (2.5) — wie der Vorrat im Fahrplan.
            if (speicher != null && speicher.Vorhanden && ausfall[(int)Verfuegbarkeitsgrund.Sperrzeit] > 0.0)
            {
                double luecke = ausfall[(int)Verfuegbarkeitsgrund.Sperrzeit];
                double rest = luecke - Math.Min(luecke, speicherKw);
                ausfall[(int)Verfuegbarkeitsgrund.Sperrzeit] = 0.0;
                ausfall[(int)Verfuegbarkeitsgrund.SpeicherLeer] = rest > 0.0 ? rest : 0.0;
            }

            int grund = 0;
            double groesster = 0.0;
            for (int g = 1; g < gruende; g++)
                if (ausfall[g] > groesster)
                {
                    groesster = ausfall[g];
                    grund = g;
                }

            double vorlauf = ohneAngebot || double.IsNegativeInfinity(hoechsterVorlauf) ? double.NaN : hoechsterVorlauf;
            return new Stundenangebot(summe, speicherKw, vorrangKw, vorlauf, (Verfuegbarkeitsgrund)grund);
        }
    }
}
