using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    // =========================================================================================
    //  AK3-K — das Kälteangebot (Entwurf AK3-K 4.2, 4.3, 4.5; Festlegungen 11, 13, 14)
    //
    //  Kälteangebot(h, V_K) = Σ Kapazität der am Tag verfügbaren Kälteerzeuger am festen Kühlvorlauf V_K
    //                       + Σ aus den Kältespeichern entnehmbar am Stundenbeginn (min(SOC, Entladeleistung))
    //                       − Prozesskälte der Stunde (Vorrang vor der Raumkühlung, sofern gerechnet)
    //
    //  Alles hier ist eine ABFRAGE ohne Nebenwirkung, wie die Angebotsfunktion der Wärme: keine Zählung, kein
    //  Takt, kein Speicherzustand. Gerechnet wird allein in der Kältestunde (Kaeltekaskade.StundeRechnen).
    // =========================================================================================

    /// <summary>
    /// <b>Die Naht Kälteerzeugerkapazität</b> (AK3-K 4.3, Festlegung 11): Was ein Kälteerzeuger in Stunde h am festen
    /// Kühlvorlauf anbieten kann — zustandsfrei wie <see cref="IErzeugerkapazitaet"/>.
    /// </summary>
    internal interface IKaelteerzeugerkapazitaet
    {
        /// <summary>Bezeichner der Anlage — für Meldungen.</summary>
        string Bezeichner { get; }

        /// <summary>Das Kälteangebot in Stunde <paramref name="stunde"/> am Kühlvorlauf <paramref name="kuehlVorlaufC"/> (NaN = der des Geräts).</summary>
        Erzeugerangebot Abfragen(int stunde, double kuehlVorlaufC);
    }

    /// <summary>
    /// <b>Die Wärmepumpe im Kühlbetrieb als Kapazität</b> (4.3, 4.5): nur am Kühltag der Wärmepumpe, im Zeitanteil der
    /// Stunde, den die Vorrangschätzung des Heizbetriebs (Brauchwasser, Prozess) übrig lässt; <c>Pkuehl</c> der
    /// Kühlkennlinie (am festen Kühlvorlauf gebildet, über den Vorlauf interpoliert nach AK3-I) an der Quelltemperatur der
    /// Stunde, dazu die freie Sole-Kühlung bis <c>Kuehl_Frei_Leistung_kW</c> — dieselbe Rechnung wie die Kältestunde.
    /// Am Heiztag ist der Kälteanteil null (Grund <see cref="Verfuegbarkeitsgrund.Umschaltung"/>), in einer Sperrstunde
    /// ebenso (Grund <see cref="Verfuegbarkeitsgrund.Sperrzeit"/>, die Sperre bindet ohnehin).
    /// </summary>
    internal sealed class WaermepumpeKaeltekapazitaet : IKaelteerzeugerkapazitaet
    {
        private readonly Kaelteerzeuger _e;
        private readonly bool[] _kuehltage;
        private readonly bool[] _sperre;
        private readonly bool _extrapolation;

        /// <param name="erzeuger">Der Kälteerzeuger (Kennlinie, Quelltemperatur, freie Kühlung).</param>
        /// <param name="kuehltage">Die Erzeugertagesart (K8a); <c>null</c> = jeder Tag ein Kühltag.</param>
        /// <param name="sperre">Die Stundenmaske der Sperre (<see cref="Sperrprofil.Verdichter"/>); <c>null</c> = keine.</param>
        /// <param name="extrapolationErlaubt"><c>Tab_Einstellungen.Extrapolation_erlaubt</c>.</param>
        internal WaermepumpeKaeltekapazitaet(Kaelteerzeuger erzeuger, bool[] kuehltage, bool[] sperre, bool extrapolationErlaubt)
        {
            _e = erzeuger ?? throw new ArgumentNullException(nameof(erzeuger));
            _kuehltage = kuehltage;
            _sperre = sperre;
            _extrapolation = extrapolationErlaubt;
        }

        public string Bezeichner => _e.Bezeichner;

        /// <summary>Der Kälteerzeuger (Proben).</summary>
        internal Kaelteerzeuger Erzeuger => _e;

        /// <summary>
        /// <b>Die Vorrangschätzung</b> (4.5, Festlegung 14): der geschätzte Heizzeitanteil der Stunde am Stundenbeginn
        /// (0 … 1); <c>null</c> = 0. Die echte Kältestunde rechnet mit dem wirklichen Heizzeitanteil; eine Abweichung
        /// erscheint als Kälte-Restbedarf.
        /// </summary>
        internal Func<int, double> Heizzeitanteil { get; set; }

        public Erzeugerangebot Abfragen(int stunde, double kuehlVorlaufC)
        {
            double kapazitaet = KapazitaetBei(stunde, kuehlVorlaufC);
            int tag = stunde / 24;
            if (_kuehltage != null && (tag >= _kuehltage.Length || !_kuehltage[tag]))
                return new Erzeugerangebot(kapazitaet, 0.0, Verfuegbarkeitsgrund.Umschaltung, false, kuehlVorlaufC, false);
            if (_sperre != null && stunde < _sperre.Length && _sperre[stunde])
                return new Erzeugerangebot(kapazitaet, 0.0, Verfuegbarkeitsgrund.Sperrzeit, false, kuehlVorlaufC, false);
            double heiz = Heizzeitanteil != null ? Heizzeitanteil(stunde) : 0.0;
            if (!(heiz > 0.0)) heiz = 0.0;
            double anteil = heiz >= 1.0 ? 0.0 : 1.0 - heiz;
            // Den Zeitanteil des Heizbetriebs nimmt die reversible Maschine der Kälteseite — der Ausfall heißt Umschaltung.
            return new Erzeugerangebot(kapazitaet, anteil * kapazitaet,
                                       anteil < 1.0 ? Verfuegbarkeitsgrund.Umschaltung : Verfuegbarkeitsgrund.KeineBegrenzung,
                                       anteil > 0.0, kuehlVorlaufC, false);
        }

        /// <summary>Die Kälteleistung bei vollem Zeitanteil [kW]: Verdichter nach Kennlinie plus freie Sole-Kühlung.</summary>
        private double KapazitaetBei(int stunde, double kuehlVorlaufC)
        {
            if (_e.Kennlinie == null) return 0.0;
            double t = _e.Quelltemperatur != null && stunde < _e.Quelltemperatur.Length ? _e.Quelltemperatur[stunde] : 0.0;
            KennlinienPunkt p = _e.Kennlinie.Auswerten(t, _extrapolation);
            double verdichter = p.Pkuehl > 0.0 && p.Eer > 0.0 ? p.Pkuehl : 0.0;
            double vorlauf = double.IsNaN(kuehlVorlaufC) ? _e.KuehlVorlaufC : kuehlVorlaufC;
            double frei = 0.0;
            if (_e.FreieKuehlungSole && t + _e.FreieKuehlungGraedigkeitK <= vorlauf)
            {
                double grenze = _e.FreieKuehlungLeistungKw ?? p.Pkuehl;
                if (grenze > 0.0) frei = grenze;
            }
            double k = verdichter + frei;
            return k > 0.0 && !double.IsInfinity(k) ? k : 0.0;
        }
    }

    /// <summary>
    /// <b>Die Kältemaschine als Kapazität</b> (4.3): <see cref="Kaeltemaschine.Stunde"/> mit unbegrenzter Last —
    /// zustandsfrei, liest nur die Rückkühlreihe; die Kennlinie an Rückkühl- und Kaltwassertemperatur, in einer Stunde
    /// mit kaltem Rückkühler die freie Kühlung als Kapazität. Der Kaltwasservorlauf der Maschine ist fest (W6).
    /// </summary>
    internal sealed class KaeltemaschineKapazitaet : IKaelteerzeugerkapazitaet
    {
        /// <summary>Last, die jede Kapazität übersteigt [kWh].</summary>
        private const double UNBEGRENZT_KWH = 1e12;

        private readonly Kaelteerzeuger _e;

        internal KaeltemaschineKapazitaet(Kaelteerzeuger erzeuger)
        {
            _e = erzeuger ?? throw new ArgumentNullException(nameof(erzeuger));
            if (_e.Maschine == null) throw new ArgumentException("Der Kälteerzeuger ist keine Kältemaschine.", nameof(erzeuger));
        }

        public string Bezeichner => _e.Bezeichner;

        public Erzeugerangebot Abfragen(int stunde, double kuehlVorlaufC)
        {
            double k = _e.Maschine.Stunde(stunde, UNBEGRENZT_KWH).KapazitaetKw;
            if (!(k > 0.0) || double.IsInfinity(k)) k = 0.0;
            return new Erzeugerangebot(k, k, Verfuegbarkeitsgrund.KeineBegrenzung, k > 0.0, kuehlVorlaufC, false);
        }
    }

    /// <summary>
    /// <b>Der Kältespeicherleser</b> (4.3, Festlegung 11): je Kältespeicher min(SOC, Entladeleistung) am Stundenbeginn —
    /// der Stand nach der Kältestunde h − 1 —, ohne einen Speicher zu verändern. Gegenstück zu <see cref="Speicherleser"/>,
    /// der nur Puffer liest, die den Heizkanal bedienen.
    /// </summary>
    internal sealed class Kaeltespeicherleser : ISpeicherangebot
    {
        private readonly SimulationPufferspeicher[] _speicher;

        internal Kaeltespeicherleser(IEnumerable<SimulationPufferspeicher> speicher)
        {
            _speicher = (speicher ?? Enumerable.Empty<SimulationPufferspeicher>()).Where(s => s != null).ToArray();
        }

        /// <summary>Die teilnehmenden Kältespeicher.</summary>
        internal IReadOnlyList<SimulationPufferspeicher> Speicher => _speicher;

        public bool Vorhanden => _speicher.Length > 0;

        public double EntnehmbarKwh(int stunde)
        {
            double summe = 0.0;
            foreach (SimulationPufferspeicher sp in _speicher)
            {
                double e = EntnehmbarAmStundenbeginn(sp);
                if (e > 0.0) summe += e;
            }
            return summe;
        }

        /// <summary>min(SOC, Entladeleistung) eines Kältespeichers [kWh]; 0 = leer. Ohne Nebenwirkung.</summary>
        internal static double EntnehmbarAmStundenbeginn(SimulationPufferspeicher sp)
        {
            if (sp == null || !(sp.Q_max > 0.0)) return 0.0;
            double m = sp.EntladeleistungMax > 0.0 ? Math.Min(sp.SOC, sp.EntladeleistungMax) : sp.SOC;
            return m > 0.0 ? m : 0.0;
        }
    }

    /// <summary>Das Kälteangebot einer Stunde und seine Teile (AK3-K 4.3).</summary>
    internal readonly struct Kaeltestundenangebot
    {
        internal Kaeltestundenangebot(double erzeugerKw, double speicherKw, double vorrangKw, double kuehlVorlaufC,
                                      Verfuegbarkeitsgrund grund)
        {
            ErzeugerKw = erzeugerKw;
            SpeicherKw = speicherKw;
            VorrangKw = vorrangKw;
            KuehlVorlaufC = kuehlVorlaufC;
            Grund = grund;
            double s = erzeugerKw + speicherKw - vorrangKw;
            LeistungKw = s > 0.0 ? s : 0.0;
        }

        /// <summary>Σ verfügbare Kapazität der Kälteerzeuger [kW].</summary>
        internal double ErzeugerKw { get; }

        /// <summary>Σ aus den Kältespeichern entnehmbar am Stundenbeginn [kW].</summary>
        internal double SpeicherKw { get; }

        /// <summary>Die Prozesskälte der Stunde [kW] — Vorrang vor der Raumkühlung.</summary>
        internal double VorrangKw { get; }

        /// <summary>Die Kälteschranke der Raumkühlung: max(0, Erzeuger + Speicher − Vorrang) [kW].</summary>
        internal double LeistungKw { get; }

        /// <summary>Der feste Kühlvorlauf, an dem das Angebot gebildet wurde [°C]; NaN = keiner.</summary>
        internal double KuehlVorlaufC { get; }

        /// <summary>Der Grund mit dem größten Ausfall; ohne Ausfall <see cref="Verfuegbarkeitsgrund.KeineBegrenzung"/>.</summary>
        internal Verfuegbarkeitsgrund Grund { get; }

        /// <summary>Das Angebot als Naht ins Gebäude (Leistung, Kühlvorlauf, Grund).</summary>
        internal Anlagenverfuegbarkeit AlsVerfuegbarkeit() => new Anlagenverfuegbarkeit(LeistungKw, KuehlVorlaufC, Grund);
    }

    /// <summary>
    /// <b>Die Kälteangebotsfunktion</b> (AK3-K 4.3): eine zustandsfreie Abfrage der Kälteseite. Grund nach der Regel der
    /// Wärme: der Ausfall mit der größten Leistung (Umschaltung, Sperrzeit); ohne Kälteerzeuger und ohne Kältespeicher
    /// <see cref="Verfuegbarkeitsgrund.KeinErzeuger"/>.
    /// </summary>
    internal static class Kaelteangebotsfunktion
    {
        internal static Kaeltestundenangebot Angebot(int stunde, double kuehlVorlaufC, IReadOnlyList<IKaelteerzeugerkapazitaet> erzeuger,
                                                     ISpeicherangebot speicher, double prozesskaelteKw)
        {
            double speicherKw = speicher != null && speicher.Vorhanden ? speicher.EntnehmbarKwh(stunde) : 0.0;
            if (!(speicherKw > 0.0)) speicherKw = 0.0;
            double vorrangKw = prozesskaelteKw > 0.0 && !double.IsInfinity(prozesskaelteKw) ? prozesskaelteKw : 0.0;
            if ((erzeuger == null || erzeuger.Count == 0) && !(speicher != null && speicher.Vorhanden))
                return new Kaeltestundenangebot(0.0, 0.0, vorrangKw, kuehlVorlaufC, Verfuegbarkeitsgrund.KeinErzeuger);

            int gruende = Enum.GetValues(typeof(Verfuegbarkeitsgrund)).Length;
            var ausfall = new double[gruende];
            double summe = 0.0;
            if (erzeuger != null)
                foreach (IKaelteerzeugerkapazitaet e in erzeuger)
                {
                    Erzeugerangebot a = e.Abfragen(stunde, kuehlVorlaufC);
                    summe += a.VerfuegbarKw;
                    double weg = a.KapazitaetKw - a.VerfuegbarKw;
                    if (weg > 0.0) ausfall[(int)a.Grund] += weg;
                }

            int grund = 0;
            double groesster = 0.0;
            for (int g = 1; g < gruende; g++)
                if (ausfall[g] > groesster)
                {
                    groesster = ausfall[g];
                    grund = g;
                }
            return new Kaeltestundenangebot(summe, speicherKw, vorrangKw, kuehlVorlaufC, (Verfuegbarkeitsgrund)grund);
        }
    }

    /// <summary>
    /// <b>Die Kälteschranke des Kreises</b> (AK3-K 4.2, 4.5): Kälteerzeuger, Kältespeicher, fester Kühlvorlauf und die
    /// Prozesskälte der Stunde (Naht; <c>null</c> = kein Prozesskältekanal) — befragt einmal am Stundenbeginn. Der Kühlvorlauf
    /// ist fest, das Angebot hängt daher nicht am Durchlauf.
    /// </summary>
    internal sealed class Kaelteschranke
    {
        internal Kaelteschranke(IReadOnlyList<IKaelteerzeugerkapazitaet> erzeuger, ISpeicherangebot speicher, double kuehlVorlaufC)
        {
            Erzeuger = erzeuger ?? Array.Empty<IKaelteerzeugerkapazitaet>();
            Speicher = speicher;
            KuehlVorlaufC = kuehlVorlaufC;
        }

        /// <summary>Die Kälteerzeuger in Kaskadenreihenfolge.</summary>
        internal IReadOnlyList<IKaelteerzeugerkapazitaet> Erzeuger { get; }

        /// <summary>Der Kältespeicheranteil; <c>null</c> = keiner.</summary>
        internal ISpeicherangebot Speicher { get; }

        /// <summary>Der feste Kühlvorlauf der Anlage [°C]; NaN = der des Geräts.</summary>
        internal double KuehlVorlaufC { get; }

        /// <summary>Die Prozesskälte der Stunde [kW] (Vorrang vor der Raumkühlung); <c>null</c> = keine.</summary>
        internal Func<int, double> Prozesskaelte { get; set; }

        /// <summary>
        /// Der Vorrang der Wärmeseite in der befragten Stunde (Brauchwasser, Prozess) — die Eingabe der Vorrangschätzung
        /// (4.5), gesetzt von <see cref="Angebot"/> vor der Befragung der Erzeuger.
        /// </summary>
        internal Stundenvorrang VorrangDerStunde { get; private set; }

        /// <summary>Das Kälteangebot der Stunde mit dem Vorrang der Wärmeseite <paramref name="vorrang"/>.</summary>
        internal Kaeltestundenangebot Angebot(int stunde, Stundenvorrang vorrang)
        {
            VorrangDerStunde = vorrang;
            return Kaelteangebotsfunktion.Angebot(stunde, KuehlVorlaufC, Erzeuger, Speicher,
                                                  Prozesskaelte != null ? Prozesskaelte(stunde) : 0.0);
        }

        /// <summary>
        /// <b>Die Vorrangschätzung des Heizzeitanteils</b> (4.5, Festlegung 14): der Teil von (Brauchwasser + Prozess) der
        /// Stunde, den die Erzeuger vor der Wärmepumpe in der Kaskade nicht schon tragen (<paramref name="vorgelagertKw"/>,
        /// ihr Angebot am Stundenbeginn), durch die Heizkapazität der Wärmepumpe, höchstens 1 — eine benannte Näherung am
        /// Stundenbeginn, zustandsfrei und ohne Nachiteration (der Speicher zählt nicht). Die echte Kältestunde rechnet
        /// mit dem wirklichen Heizzeitanteil.
        /// </summary>
        /// <param name="vorrang">Der Vorrang der Wärmeseite der Stunde.</param>
        /// <param name="heizkapazitaetKw">Die Heizkapazität der Wärmepumpe [kW].</param>
        /// <param name="vorgelagertKw">Das verfügbare Angebot der Erzeuger vor der Wärmepumpe in der Kaskade [kW]; 0 = sie
        /// steht vorn.</param>
        internal static double Heizzeitanteil(Stundenvorrang vorrang, double heizkapazitaetKw, double vorgelagertKw = 0.0)
        {
            double bw = vorrang.BrauchwasserKw > 0.0 && !double.IsInfinity(vorrang.BrauchwasserKw) ? vorrang.BrauchwasserKw : 0.0;
            double pz = vorrang.ProzessKw > 0.0 && !double.IsInfinity(vorrang.ProzessKw) ? vorrang.ProzessKw : 0.0;
            double vor = vorgelagertKw > 0.0 && !double.IsInfinity(vorgelagertKw) ? vorgelagertKw : 0.0;
            double last = bw + pz - vor;
            if (!(last > 0.0)) return 0.0;
            if (!(heizkapazitaetKw > 0.0)) return 1.0;
            double a = last / heizkapazitaetKw;
            return a >= 1.0 ? 1.0 : a;
        }
    }
}
