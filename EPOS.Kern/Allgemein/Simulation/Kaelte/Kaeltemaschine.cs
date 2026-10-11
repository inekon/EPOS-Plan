using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Ein ausgewerteter Punkt der Kennlinie einer Kältemaschine.</summary>
    public readonly struct KaeltemaschinenPunkt
    {
        /// <summary>Kälteleistung [kW]; 0, wenn die Kennlinie keine führt.</summary>
        public readonly double LeistungKw;

        /// <summary>EER [—]; 0, wenn die Kennlinie keinen führt.</summary>
        public readonly double Eer;

        /// <summary><c>true</c> = mindestens eine Achse lag außerhalb der Stützstellen; der Randwert gilt.</summary>
        public readonly bool Randwert;

        /// <summary>Legt den Punkt an.</summary>
        public KaeltemaschinenPunkt(double leistungKw, double eer, bool randwert)
        {
            LeistungKw = leistungKw;
            Eer = eer;
            Randwert = randwert;
        }
    }

    /// <summary>
    /// <b>Die Kennlinie einer Kältemaschine</b> über Rückkühl- und Kaltwassertemperatur (KU3-2;
    /// <c>Tab_Kenndaten_Kaeltemaschine</c>).
    ///
    /// <para><b>Bilinear im Raster:</b> erst linear längs der Rückkühltemperatur innerhalb jeder
    /// Kaltwasserzeile, dann linear zwischen den beiden einschließenden Zeilen. Auf einem vollen
    /// Rechteckraster ist das die bilineare Interpolation; ein lückiges Raster rechnet mit den Punkten,
    /// die es führt. Leistung und EER werden je für sich gelesen — ein Punkt ohne EER zählt für die
    /// Leistung und umgekehrt.</para>
    ///
    /// <para><b>Außerhalb der Stützstellen gilt der Randwert</b> — keine Extrapolation; der Punkt
    /// trägt dann <see cref="KaeltemaschinenPunkt.Randwert"/>, und der Lauf meldet die Stunden
    /// (Muster der Kühlkennlinie der Wärmepumpe, Kühlkonzept 5.1).</para>
    /// </summary>
    public sealed class KaeltemaschinenKennlinie
    {
        private sealed class Zeile
        {
            public double Kaltwasser;
            public double[] Rueckkuehl;
            public double[] Wert;
        }

        private readonly List<Zeile> _leistung;
        private readonly List<Zeile> _eer;

        /// <summary>Die Kaltwasser-Stützstellen aufsteigend [°C].</summary>
        public IReadOnlyList<double> Kaltwasserstuetzstellen { get; }

        /// <summary>Kleinste und größte Rückkühl-Stützstelle [°C]; 0, wenn leer.</summary>
        public double RueckkuehlMin { get; }

        /// <summary>Größte Rückkühl-Stützstelle [°C]; 0, wenn leer.</summary>
        public double RueckkuehlMax { get; }

        /// <summary><c>true</c> = kein Punkt mit Leistung oder kein Punkt mit EER — die Kennlinie rechnet nicht.</summary>
        public bool Leer => _leistung.Count == 0 || _eer.Count == 0;

        /// <summary>Baut die Kennlinie aus den Punkten (Rückkühl, Kaltwasser, EER, Leistung).</summary>
        public KaeltemaschinenKennlinie(IEnumerable<(double Rueckkuehl, double Kaltwasser, double? Eer, double? Leistung)> punkte)
        {
            var liste = (punkte ?? Enumerable.Empty<(double, double, double?, double?)>())
                .Where(p => !double.IsNaN(p.Item1) && !double.IsNaN(p.Item2)).ToList();
            _leistung = Zeilen(liste.Where(p => p.Item4.HasValue && p.Item4.Value >= 0)
                                    .Select(p => (p.Item1, p.Item2, p.Item4.Value)));
            _eer = Zeilen(liste.Where(p => p.Item3.HasValue && p.Item3.Value > 0)
                               .Select(p => (p.Item1, p.Item2, p.Item3.Value)));
            Kaltwasserstuetzstellen = liste.Select(p => p.Item2).Distinct().OrderBy(x => x).ToList();
            RueckkuehlMin = liste.Count > 0 ? liste.Min(p => p.Item1) : 0.0;
            RueckkuehlMax = liste.Count > 0 ? liste.Max(p => p.Item1) : 0.0;
        }

        private static List<Zeile> Zeilen(IEnumerable<(double Rk, double Kw, double W)> punkte)
        {
            var zeilen = new List<Zeile>();
            foreach (var g in punkte.GroupBy(p => p.Kw).OrderBy(g => g.Key))
            {
                // Doppelte Rückkühlstellen einer Zeile: der erste Punkt gilt (die Tabelle schließt sie aus).
                var sortiert = g.GroupBy(p => p.Rk).Select(x => x.First()).OrderBy(p => p.Rk).ToList();
                zeilen.Add(new Zeile
                {
                    Kaltwasser = g.Key,
                    Rueckkuehl = sortiert.Select(p => p.Rk).ToArray(),
                    Wert = sortiert.Select(p => p.W).ToArray()
                });
            }
            return zeilen;
        }

        /// <summary>Wertet die Kennlinie bei Rückkühl- und Kaltwassertemperatur aus.</summary>
        public KaeltemaschinenPunkt Auswerten(double rueckkuehl, double kaltwasser)
        {
            if (Leer) return new KaeltemaschinenPunkt(0.0, 0.0, false);
            bool rand = false;
            double p = Lesen(_leistung, rueckkuehl, kaltwasser, ref rand);
            double e = Lesen(_eer, rueckkuehl, kaltwasser, ref rand);
            return new KaeltemaschinenPunkt(p, e, rand);
        }

        private static double Lesen(List<Zeile> zeilen, double rk, double kw, ref bool rand)
        {
            if (kw <= zeilen[0].Kaltwasser)
            {
                if (kw < zeilen[0].Kaltwasser) rand = true;
                return Linear(zeilen[0], rk, ref rand);
            }
            Zeile letzte = zeilen[zeilen.Count - 1];
            if (kw >= letzte.Kaltwasser)
            {
                if (kw > letzte.Kaltwasser) rand = true;
                return Linear(letzte, rk, ref rand);
            }
            for (int i = 1; i < zeilen.Count; i++)
            {
                if (kw > zeilen[i].Kaltwasser) continue;
                Zeile a = zeilen[i - 1], b = zeilen[i];
                double wa = Linear(a, rk, ref rand);
                double wb = Linear(b, rk, ref rand);
                double t = (kw - a.Kaltwasser) / (b.Kaltwasser - a.Kaltwasser);
                return wa + t * (wb - wa);
            }
            return Linear(letzte, rk, ref rand);
        }

        private static double Linear(Zeile z, double rk, ref bool rand)
        {
            double[] x = z.Rueckkuehl, y = z.Wert;
            if (rk <= x[0]) { if (rk < x[0]) rand = true; return y[0]; }
            int n = x.Length - 1;
            if (rk >= x[n]) { if (rk > x[n]) rand = true; return y[n]; }
            for (int i = 1; i <= n; i++)
            {
                if (rk > x[i]) continue;
                double t = (rk - x[i - 1]) / (x[i] - x[i - 1]);
                return y[i - 1] + t * (y[i] - y[i - 1]);
            }
            return y[n];
        }
    }

    /// <summary>Das Ergebnis einer Stunde der Kältemaschine.</summary>
    public readonly struct KaeltemaschinenStunde
    {
        /// <summary>Gelieferte Kälte [kWh].</summary>
        public readonly double KaelteKwh;

        /// <summary>Strom des Verdichters [kWh] — in freier Kühlung der Strom des EER-Ersatzes.</summary>
        public readonly double VerdichterKwh;

        /// <summary>Hilfsstrom der Rückkühlung [kWh] (Nennhilfsstrom × Laufanteil).</summary>
        public readonly double HilfsstromKwh;

        /// <summary>Verfügbare Kälteleistung der Stunde [kW].</summary>
        public readonly double KapazitaetKw;

        /// <summary><c>true</c> = die Stunde lief in freier Kühlung.</summary>
        public readonly bool FreieKuehlung;

        /// <summary><c>true</c> = die Kennlinie wurde am Rand gelesen.</summary>
        public readonly bool Randwert;

        /// <summary><c>true</c> = die Last lag unter der Mindestteillast; die Maschine taktet.</summary>
        public readonly bool Takt;

        /// <summary>KM3: Mehrstrom des Taktens [kWh] — Teil von <see cref="VerdichterKwh"/>; 0 ohne <c>Teillast_Weg</c>.</summary>
        public readonly double MehrstromKwh;

        /// <summary>KM3: Starts der Taktstunde aller laufenden Maschinen; 0 ohne Takt oder ohne <c>Teillast_Weg</c>.</summary>
        public readonly int Starts;

        /// <summary>KM3: Lastgrad PLR je laufender Maschine; 0 ohne <c>Teillast_Weg</c> oder in freier Kühlung.</summary>
        public readonly double Lastgrad;

        /// <summary>KM3: <c>true</c> = der EER wurde mit dem Gütegrad über den Kennfeldrand fortgesetzt.</summary>
        public readonly bool Extrapoliert;

        /// <summary>KM3: laufende Maschinen der Stunde nach der Folgeschaltung; 0 ohne <c>Teillast_Weg</c>.</summary>
        public readonly int Laufend;

        /// <summary>Legt das Stundenergebnis an.</summary>
        public KaeltemaschinenStunde(double kaelte, double verdichter, double hilfs, double kapazitaet,
                                     bool frei, bool rand, bool takt)
            : this(kaelte, verdichter, hilfs, kapazitaet, frei, rand, takt, 0.0, 0, 0.0, false, 0)
        {
        }

        /// <summary>KM3: Legt das Stundenergebnis samt Teillast, Takt und Randweg an.</summary>
        public KaeltemaschinenStunde(double kaelte, double verdichter, double hilfs, double kapazitaet,
                                     bool frei, bool rand, bool takt, double mehrstrom, int starts, double lastgrad,
                                     bool extrapoliert, int laufend)
        {
            KaelteKwh = kaelte;
            VerdichterKwh = verdichter;
            HilfsstromKwh = hilfs;
            KapazitaetKw = kapazitaet;
            FreieKuehlung = frei;
            Randwert = rand;
            Takt = takt;
            MehrstromKwh = mehrstrom;
            Starts = starts;
            Lastgrad = lastgrad;
            Extrapoliert = extrapoliert;
            Laufend = laufend;
        }

        /// <summary>K-F2: <c>true</c> = die Maschine hat ein Rückkühlwerk, <see cref="Rueckkuehlung"/> ist belegt.</summary>
        public readonly bool MitRueckkuehlwerk;

        /// <summary>K-F2: die Stunde des Rückkühlwerks (Ventilator, Wasser, Rückkühltemperatur); leer ohne Rückkühlwerk.</summary>
        public readonly RueckkuehlwerkStunde Rueckkuehlung;

        private KaeltemaschinenStunde(KaeltemaschinenStunde s, double hilfs, RueckkuehlwerkStunde r)
            : this(s.KaelteKwh, s.VerdichterKwh, hilfs, s.KapazitaetKw, s.FreieKuehlung, s.Randwert, s.Takt,
                   s.MehrstromKwh, s.Starts, s.Lastgrad, s.Extrapoliert, s.Laufend)
        {
            MitRueckkuehlwerk = true;
            Rueckkuehlung = r;
        }

        /// <summary>
        /// K-F2: dieselbe Stunde mit der Bilanz des Rückkühlwerks und dem Hilfsstrom der Rückkühlung
        /// <paramref name="hilfs"/> [kWh] (mit gepflegtem Ventilator dessen Strom, sonst unverändert).
        /// </summary>
        public KaeltemaschinenStunde MitRueckkuehlung(double hilfs, RueckkuehlwerkStunde r) => new KaeltemaschinenStunde(this, hilfs, r);

        /// <summary>
        /// K-F3: Kälte der Teil-Freikühlung in einer Mischstunde [kWh] — der Anteil, den der Rückkühler in Reihe vorgekühlt
        /// hat, während der Verdichter den Rest deckte; Teil von <see cref="KaelteKwh"/>. 0 in einer Stunde ganz in freier
        /// Kühlung (<see cref="FreieKuehlung"/>), ohne Verdichter und ohne Schaltung <c>REIHE</c>.
        /// </summary>
        public readonly double TeilFreiKwh;

        /// <summary>K-F3: Kälte des Verdichters der Stunde [kWh] — <see cref="KaelteKwh"/> ohne die Teil-Freikühlung.</summary>
        public double VerdichterKaelteKwh => KaelteKwh - TeilFreiKwh;

        private KaeltemaschinenStunde(KaeltemaschinenStunde v, double kaelte, double verdichter, double hilfs, double kapazitaet,
                                      bool frei, double teilFrei, RueckkuehlwerkStunde r)
            : this(kaelte, verdichter, hilfs, kapazitaet, frei, v.Randwert, v.Takt, v.MehrstromKwh, v.Starts, v.Lastgrad,
                   v.Extrapoliert, v.Laufend)
        {
            TeilFreiKwh = teilFrei;
            MitRueckkuehlwerk = true;
            Rueckkuehlung = r;
        }

        /// <summary>
        /// K-F3: die Stunde einer Maschine mit Rückkühlwerk in Reihe — Verdichteranteil <paramref name="v"/> (Randwert, Takt,
        /// Teillast bleiben seine) und die Summen der Stunde samt Teil-Freikühlung und Bilanz des Rückkühlwerks.
        /// </summary>
        internal static KaeltemaschinenStunde Reihenstunde(KaeltemaschinenStunde v, double kaelte, double verdichter,
                                                           double hilfs, double kapazitaet, bool frei, double teilFrei,
                                                           RueckkuehlwerkStunde r) =>
            new KaeltemaschinenStunde(v, kaelte, verdichter, hilfs, kapazitaet, frei, teilFrei, r);

        /// <summary>Strom der Stunde [kWh] — Verdichter und Rückkühlung.</summary>
        public double StromKwh => VerdichterKwh + HilfsstromKwh;
    }

    /// <summary>
    /// <b>Die Rechenklasse der Kältemaschine</b> (KU3-2; Kühlkonzept 5.3, 5.4, 6.1; Entscheide E67, E68).
    ///
    /// <para><b>Rückkühltemperatur je Stunde</b> aus dem Klima (<see cref="Rueckkuehltemperatur"/>):
    /// luftgekühlt und Trockenkühler Außentemperatur plus Grädigkeit, wassergekühlt fest 25 °C,
    /// Nasskühler Feuchtkugeltemperatur plus Grädigkeit (ohne Luftfeuchte Außentemperatur − 3 K).</para>
    ///
    /// <para><b>Verdichterbetrieb:</b> Kapazität und EER aus der Kennlinie bei Rückkühl- und
    /// Kaltwassertemperatur; Teillast linear mit dem EER der Stunde. Unter der Mindestteillast taktet
    /// die Maschine — sie liefert die Last der Stunde, ohne Anfahrverlust. Strom = Kälte / EER +
    /// Hilfsstrom der Rückkühlung × Laufanteil (NULL = 0, im EER enthalten).</para>
    ///
    /// <para><b>Freie Kühlung</b> (K8, E33: ein Betriebsfall, kein Erzeuger): Mit Trocken- oder
    /// Nasskühler deckt der Rückkühler die Kälte direkt, wenn seine Temperatur mindestens
    /// <see cref="KaelteFestwerte.FREIE_KUEHLUNG_ABSTAND_K"/> unter der Kaltwassertemperatur liegt —
    /// bis zur Nennkälteleistung, mit dem EER-Ersatz <see cref="KaelteFestwerte.FREIE_KUEHLUNG_EER"/>
    /// und dem Hilfsstrom der Rückkühlung. Luftgekühlt und wassergekühlt kennen keine freie Kühlung.</para>
    /// </summary>
    public sealed class Kaeltemaschine
    {
        /// <summary><c>Tab_Kaeltemaschine.ID</c> der Projektkopie.</summary>
        public int Id;

        /// <summary>Anzeigename.</summary>
        public string Bezeichner = "";

        /// <summary>Rückkühlart (Persistenzwert aus <see cref="KaeltemaschineSchema.RUECKKUEHLARTEN"/>).</summary>
        public string Rueckkuehlart = KaeltemaschineSchema.RUECKKUEHLART_LUFT;

        /// <summary>Nennkälteleistung [kW] — Bezug der Mindestteillast und Grenze der freien Kühlung.</summary>
        public double NennleistungKw;

        /// <summary>Mindestteillast als Anteil der Nennkälteleistung [0…1].</summary>
        public double Mindestteillast;

        /// <summary>Elektrische Leistung der Rückkühlung im Nennpunkt [kW]; 0 = im EER enthalten.</summary>
        public double HilfsstromRueckkuehlungKw;

        /// <summary>Kaltwassertemperatur des Laufs [°C] — der feste Vorlauf; am Stundenvorlauf (KK2) die Vorgabe ohne Stundenwert.</summary>
        public double Kaltwassertemperatur;

        /// <summary>
        /// KK2 (Entwurf KK 2.3): die Untergrenze des Kaltwasservorlaufs am Stundenvorlauf [°C] — <c>Kaltwasser_Vorlauf_Min</c>
        /// der Projektkopie, ohne ihn die kleinste Kaltwasser-Stützstelle der Kennlinie; NaN = keine.
        /// </summary>
        public double KaltwasserVorlaufMinC = double.NaN;

        /// <summary>
        /// KK2: die Kaltwassertemperatur der Stunde am verlangten Vorlauf <paramref name="vorlaufC"/> [°C] — nie kälter als
        /// <see cref="KaltwasserVorlaufMinC"/>; NaN = <see cref="Kaltwassertemperatur"/> (der feste Vorlauf).
        /// </summary>
        public double KaltwasserAmVorlauf(double vorlaufC)
        {
            if (double.IsNaN(vorlaufC) || double.IsInfinity(vorlaufC)) return Kaltwassertemperatur;
            return !double.IsNaN(KaltwasserVorlaufMinC) && vorlaufC < KaltwasserVorlaufMinC ? KaltwasserVorlaufMinC : vorlaufC;
        }

        /// <summary>
        /// Anzahl gleicher Maschinen der Anlagenzeile (KU3-4, <c>Kaeltemaschine_Anzahl</c>), mindestens 1: Die
        /// Last teilt sich gleich auf, Kälte, Strom und Kapazität zählen das Vielfache.
        /// </summary>
        public int Anzahl = 1;

        /// <summary>Die Kennlinie der Projektkopie.</summary>
        public KaeltemaschinenKennlinie Kennlinie;

        /// <summary>
        /// KM3 (Fachkonzept Teillast und Takten 3, 5): die wirksame Teillast der Projektkopie
        /// (<see cref="Kaeltemaschinenteillast.AusModell"/>); <c>null</c> oder Bestandsweg mit Randweg RANDWERT = der
        /// heutige Weg Zeichen für Zeichen.
        /// </summary>
        public Kaeltemaschinenteillast Teillast;

        /// <summary>KM3: <c>true</c> = <c>Teillast_Weg</c> ist gesetzt — Lastachse, Takten mit Mehrstrom und Folgeschaltung.</summary>
        public bool TeillastWirksam => Teillast != null && !Teillast.Bestandsweg;

        /// <summary>KM3: <c>true</c> = <c>Kennfeld_Randweg = GUETEGRAD</c> — der EER wird über den Kennfeldrand fortgesetzt.</summary>
        public bool GuetegradWirksam => Teillast != null && KaeltemaschinenRand.IstGuetegrad(Teillast.Randweg);

        /// <summary>KM3: rechnet die Maschine auf einem der neuen Wege (Teillast oder Randweg)? Ohne beide der heutige Weg.</summary>
        public bool MitWeg => TeillastWirksam || GuetegradWirksam;

        /// <summary>Rückkühltemperatur je Stunde [°C].</summary>
        public double[] Rueckkuehltemperatur_stuendlich;

        /// <summary>
        /// Das gewählte Rückkühlwerk (K-F1, <c>ID_Rueckkuehlwerk</c> der Anlagenzeile); <c>null</c> = die Rückkühlart rechnet
        /// mit den Festwerten. Gesetzt über <see cref="RueckkuehlwerkSetzen"/>.
        /// </summary>
        public Rueckkuehlwerk Rueckkuehlwerk { get; private set; }

        /// <summary>
        /// Setzt das Rückkühlwerk (Entwurf 5.2): Seine Bauart gilt statt der Rückkühlart der Maschine — so nehmen
        /// Rückkühltemperatur und freie Kühlung (<see cref="FreieKuehlungMoeglich"/>) denselben Weg. <c>null</c> lässt die
        /// Maschine unverändert.
        /// </summary>
        public void RueckkuehlwerkSetzen(Rueckkuehlwerk r)
        {
            if (r == null) return;
            Rueckkuehlwerk = r;
            Rueckkuehlart = r.Rueckkuehlart;
        }

        /// <summary>
        /// Die Rückkühltemperaturen eines Jahres für DIESE Maschine: mit Rückkühlwerk über
        /// <see cref="Rueckkuehlwerk.RueckkuehltemperaturenBilden"/>, sonst über die Rückkühlart
        /// (<see cref="RueckkuehltemperaturenBilden(string, double[], double[], out int)"/>) — unverändert.
        /// </summary>
        public double[] RueckkuehltemperaturenBilden(double[] aussenC, double[] feuchteProzent, out int stundenOhneFeuchte) =>
            Rueckkuehlwerk != null
                ? Rueckkuehlwerk.RueckkuehltemperaturenBilden(aussenC, feuchteProzent, out stundenOhneFeuchte)
                : RueckkuehltemperaturenBilden(Rueckkuehlart, aussenC, feuchteProzent, out stundenOhneFeuchte);

        /// <summary>Kann die Maschine frei kühlen (Trocken- oder Nasskühler)?</summary>
        public bool FreieKuehlungMoeglich =>
            Rueckkuehlart == KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER ||
            Rueckkuehlart == KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER;

        /// <summary>Kühlt die Maschine in Stunde <paramref name="h"/> frei?</summary>
        public bool FreieKuehlung(int h) => FreieKuehlung(h, Kaltwassertemperatur);

        /// <summary>KK2: Kühlt die Maschine in Stunde <paramref name="h"/> bei der Kaltwassertemperatur <paramref name="kaltwasserC"/> frei?</summary>
        public bool FreieKuehlung(int h, double kaltwasserC)
        {
            if (!FreieKuehlungMoeglich || Rueckkuehltemperatur_stuendlich == null ||
                h < 0 || h >= Rueckkuehltemperatur_stuendlich.Length) return false;
            return Rueckkuehltemperatur_stuendlich[h] <= kaltwasserC - KaelteFestwerte.FREIE_KUEHLUNG_ABSTAND_K;
        }

        /// <summary>
        /// Rechnet eine Stunde: wie viel der Last <paramref name="lastKwh"/> die Maschine deckt und mit
        /// welchem Strom. Die Last ist der offene Kältebedarf der Stunde [kWh].
        /// </summary>
        public KaeltemaschinenStunde Stunde(int h, double lastKwh) => Stunde(h, lastKwh, Kaltwassertemperatur);

        /// <summary>
        /// KK2 (Entwurf KK 2.4): eine Stunde bei der Kaltwassertemperatur <paramref name="kaltwasserC"/> [°C] — die
        /// zweidimensionale Kennlinie trägt die Achse; mit <see cref="Kaltwassertemperatur"/> Zeichen für Zeichen
        /// <see cref="Stunde(int, double)"/>.
        /// </summary>
        public KaeltemaschinenStunde Stunde(int h, double lastKwh, double kaltwasserC) =>
            Stunde(h, lastKwh, kaltwasserC, double.NaN);

        /// <summary>
        /// K-F3 (Entwurf Split/VRF/Rückkühlwerk 5.5): eine Stunde mit dem Kaltwasserrücklauf der Stunde
        /// <paramref name="ruecklaufC"/> [°C] — auf Stufe AK3 der gerechnete Kühlrücklauf, NaN = Vorlauf +
        /// <see cref="ReiheSpreizungK"/>. Den Rücklauf liest nur ein Rückkühlwerk in Reihe; sonst Zeichen für Zeichen
        /// <see cref="Stunde(int, double, double)"/>.
        /// </summary>
        public KaeltemaschinenStunde Stunde(int h, double lastKwh, double kaltwasserC, double ruecklaufC)
        {
            if (Rueckkuehlwerk == null) return StundeBei(h, lastKwh, kaltwasserC, RueckkuehltemperaturDerStunde(h));
            if (Rueckkuehlwerk.Reihe) return StundeReihe(h, lastKwh, kaltwasserC, ruecklaufC);
            return StundeMitRueckkuehlwerk(h, lastKwh, kaltwasserC);
        }

        /// <summary>
        /// K-F3: Auslegungsspreizung des Kaltwassers [K] für die Teil-Freikühlung in Reihe — die kleinste gepflegte Spreizung
        /// der Kühlübergabe der Gebäude (<c>Kuehl_Auslegung_Ruecklauf − Kuehl_Auslegung_Vorlauf</c>), ohne Kühlübergabe
        /// <see cref="KaelteFestwerte.REIHE_SPREIZUNG_K"/>. Bezug des Kaltwasserstroms und des Rücklaufs ohne AK3.
        /// </summary>
        public double ReiheSpreizungK = KaelteFestwerte.REIHE_SPREIZUNG_K;

        /// <summary>
        /// Kühlt die Maschine in Stunde <paramref name="h"/> parallel frei (heutige freie Kühlung)? In Reihe nie — dort trägt
        /// <see cref="StundeReihe"/> die freie Kühlung als Vorkühlung des Rücklaufs.
        /// </summary>
        private bool ParallelFrei(int h, double kaltwasserC) =>
            (Rueckkuehlwerk == null || !Rueckkuehlwerk.Reihe) && FreieKuehlung(h, kaltwasserC);

        /// <summary>
        /// K-F3: der Kaltwasserrücklauf der Stunde [°C] — <paramref name="ruecklaufC"/> (AK3), sonst Vorlauf +
        /// <see cref="ReiheSpreizungK"/>; nie unter dem Vorlauf.
        /// </summary>
        internal double RuecklaufDerStunde(double kaltwasserC, double ruecklaufC)
        {
            double r = double.IsNaN(ruecklaufC) || double.IsInfinity(ruecklaufC) ? kaltwasserC + ReiheSpreizungK : ruecklaufC;
            return r < kaltwasserC ? kaltwasserC : r;
        }

        /// <summary>
        /// K-F3 (Entwurf 5.5): eine Stunde mit Rückkühlwerk in Reihe. Liegt die Fluidtemperatur des Rückkühlers (Bezug +
        /// Nenn-Annäherung) mindestens <see cref="KaelteFestwerte.FREIE_KUEHLUNG_ABSTAND_K"/> unter dem Rücklauf, kühlt er ihn
        /// vor: Q̇_FK = min(Last, Q̇_Nenn · (θ_R − θ_FK) / Δθ, Rückkühlerleistung) (<see cref="Rueckkuehlwerk.TeilFreikuehlungKwh"/>;
        /// Rückkühlerleistung = <c>Nennleistung_kW</c> des Rückkühlwerks, leer die Nennkälteleistung der Anlagenzeile). Den
        /// Rest deckt der Verdichter am Kaltwasservorlauf mit seinem EER an der Rückkühltemperatur der Stunde (mit
        /// <c>LASTABHAENGIG</c> genau ein Schätzschritt wie in K-F2) — Vorbild ist die freie Kühlung über die Wärmequelle
        /// (KU3-6). Strom der freien Kühlung: mit <c>Ventilator_Nenn_kW</c> allein der Ventilator (die Bilanz des Rückkühlwerks
        /// trägt Vorkühl- und Verflüssigerwärme), sonst der EER-Ersatz <see cref="KaelteFestwerte.FREIE_KUEHLUNG_EER"/> und der
        /// Hilfsstrom der Rückkühlung nach Laufanteil (zusammen mit dem Verdichteranteil höchstens der Nennhilfsstrom).
        /// </summary>
        private KaeltemaschinenStunde StundeReihe(int h, double lastKwh, double kaltwasserC, double ruecklaufC)
        {
            Rueckkuehlwerk r = Rueckkuehlwerk;
            if (!(lastKwh > 0)) return default;
            int anzahl = Math.Max(1, Anzahl);
            double nenn = NennleistungKw * anzahl;
            double rkNenn = RueckkuehltemperaturDerStunde(h);
            double fk = FreieKuehlungMoeglich
                ? Rueckkuehlwerk.TeilFreikuehlungKwh(lastKwh, nenn, ReiheSpreizungK, RuecklaufDerStunde(kaltwasserC, ruecklaufC),
                                                     rkNenn, r.NennleistungKw ?? nenn)
                : 0.0;
            if (!(fk > 0)) return StundeMitRueckkuehlwerk(h, lastKwh, kaltwasserC);

            double rest = lastKwh - fk;
            if (rest < Rechenrand.ABSOLUT) rest = 0.0;
            double rk = rkNenn;
            KaeltemaschinenStunde v = rest > 0 ? Verdichterstunde(h, rest, kaltwasserC, rkNenn, out rk) : default;
            bool verdichter = v.KaelteKwh > 0;
            double kaelte = verdichter ? fk + v.KaelteKwh : fk;
            double abwaerme = verdichter ? kaelte + v.VerdichterKwh : fk;
            double kap = verdichter ? v.KapazitaetKw : nenn;
            double lauf = r.Lastabhaengig && verdichter ? LaufanteilDerStunde(v) : double.NaN;
            RueckkuehlwerkStunde b = r.Stunde(h, abwaerme, kaelte, kap, verdichter ? rk : rkNenn, lauf);

            double strom, hilfs;
            if (r.MitVentilator)
            {
                strom = v.VerdichterKwh;
                hilfs = b.VentilatorKwh;
            }
            else
            {
                strom = v.VerdichterKwh + fk / KaelteFestwerte.FREIE_KUEHLUNG_EER;
                hilfs = v.HilfsstromKwh + (NennleistungKw > 0 ? HilfsstromRueckkuehlungKw * fk / NennleistungKw : 0.0);
                double hilfsMax = HilfsstromRueckkuehlungKw * anzahl;
                if (hilfs > hilfsMax) hilfs = hilfsMax;
            }
            return KaeltemaschinenStunde.Reihenstunde(v, kaelte, strom, hilfs, kap, !verdichter, verdichter ? fk : 0.0, b);
        }

        /// <summary>
        /// K-F2/K-F3: die Verdichterstunde mit Rückkühlwerk — an der Nenn-Rückkühltemperatur <paramref name="rkNenn"/>, mit
        /// <c>LASTABHAENGIG</c> genau ein Schätzschritt über die Abwärme; <paramref name="rk"/> ist die Rückkühltemperatur, mit
        /// der die Stunde gerechnet wurde.
        /// </summary>
        private KaeltemaschinenStunde Verdichterstunde(int h, double lastKwh, double kaltwasserC, double rkNenn, out double rk)
        {
            Rueckkuehlwerk r = Rueckkuehlwerk;
            rk = rkNenn;
            KaeltemaschinenStunde s = StundeBei(h, lastKwh, kaltwasserC, rkNenn);
            if (!(s.KaelteKwh > 0)) return s;
            if (r.Lastabhaengig && !s.FreieKuehlung)
            {
                double x0 = r.Lastanteil(s.KaelteKwh + s.VerdichterKwh, s.KaelteKwh, s.KapazitaetKw);
                rk = r.RueckkuehltemperaturLastabhaengig(rkNenn, x0);
                if (BitConverter.DoubleToInt64Bits(rk) != BitConverter.DoubleToInt64Bits(rkNenn))
                    s = StundeBei(h, lastKwh, kaltwasserC, rk);
            }
            return s;
        }

        /// <summary>Die Rückkühltemperatur der Stunde mit Nenn-Annäherung [°C]; ohne Reihe die des fremden Kühlwassers.</summary>
        private double RueckkuehltemperaturDerStunde(int h) =>
            Rueckkuehltemperatur_stuendlich != null && h >= 0 && h < Rueckkuehltemperatur_stuendlich.Length
                ? Rueckkuehltemperatur_stuendlich[h] : KaelteFestwerte.RUECKKUEHLTEMPERATUR_WASSER_C;

        /// <summary>Eine Stunde der Anlagenzeile bei der Rückkühltemperatur <paramref name="rk"/> [°C].</summary>
        private KaeltemaschinenStunde StundeBei(int h, double lastKwh, double kaltwasserC, double rk)
        {
            // KM3: nur mit Teillast_Weg oder Randweg GUETEGRAD der neue Weg; freie Kühlung bleibt unverändert.
            if (MitWeg && lastKwh > 0 && !ParallelFrei(h, kaltwasserC)) return StundeMitWeg(h, lastKwh, kaltwasserC, rk);
            if (Anzahl <= 1) return StundeEinzeln(h, lastKwh, kaltwasserC, rk);
            KaeltemaschinenStunde s = StundeEinzeln(h, lastKwh / Anzahl, kaltwasserC, rk);
            return new KaeltemaschinenStunde(s.KaelteKwh * Anzahl, s.VerdichterKwh * Anzahl, s.HilfsstromKwh * Anzahl,
                                             s.KapazitaetKw * Anzahl, s.FreieKuehlung, s.Randwert, s.Takt);
        }

        /// <summary>
        /// K-F2 (Entwurf Split/VRF/Rückkühlwerk 5.3): eine Stunde mit Rückkühlwerk. Die Stunde rechnet zuerst mit der
        /// Nenn-Annäherung; mit Annäherung <c>LASTABHAENGIG</c> schätzt sie daraus die Abwärme (Kälte + Verdichterstrom),
        /// bildet die lastabhängige Rückkühltemperatur und rechnet die Stunde damit GENAU EINMAL neu — deterministisch, ohne
        /// Iteration. In freier Kühlung gilt die Nenn-Annäherung (die Entscheidung fiel an ihr). Dann die Bilanz des
        /// Rückkühlwerks; mit gepflegtem Ventilator ersetzt sein Strom den Hilfsstrom der Rückkühlung.
        /// K-F3: In freier Kühlung (<c>PARALLEL</c>) mit gepflegtem Ventilator ist der Ventilator der ganze Strom der freien
        /// Kühlung — der EER-Ersatz entfällt; ohne Ventilator bleibt er wie heute.
        /// </summary>
        private KaeltemaschinenStunde StundeMitRueckkuehlwerk(int h, double lastKwh, double kaltwasserC)
        {
            Rueckkuehlwerk r = Rueckkuehlwerk;
            KaeltemaschinenStunde s = Verdichterstunde(h, lastKwh, kaltwasserC, RueckkuehltemperaturDerStunde(h), out double rk);
            if (!(s.KaelteKwh > 0)) return s;
            double abwaerme = s.FreieKuehlung ? s.KaelteKwh : s.KaelteKwh + s.VerdichterKwh;
            // Der Laufanteil zählt nur an der lastabhängigen Annäherung (EIN_AUS läuft dann voll, solange die Maschine läuft).
            double lauf = r.Lastabhaengig && !s.FreieKuehlung ? LaufanteilDerStunde(s) : double.NaN;
            RueckkuehlwerkStunde b = r.Stunde(h, abwaerme, s.KaelteKwh, s.KapazitaetKw, rk, lauf);
            if (r.MitVentilator && s.FreieKuehlung)
                return KaeltemaschinenStunde.Reihenstunde(s, s.KaelteKwh, 0.0, b.VentilatorKwh, s.KapazitaetKw, true, 0.0, b);
            return s.MitRueckkuehlung(r.MitVentilator ? b.VentilatorKwh : s.HilfsstromKwh, b);
        }

        /// <summary>
        /// K-F2: der Laufanteil der Verdichterstunde [—] — 1, solange die Maschine moduliert; in einer Taktstunde (unter der
        /// Mindestteillast, Kennzeichen <see cref="KaeltemaschinenStunde.Takt"/>, linear wie auf dem Weg von KM3) die Zeit am
        /// Mindestpunkt: Kälte je laufender Maschine / min(Mindestteillast · Nennleistung, Kapazität je Maschine).
        /// </summary>
        internal double LaufanteilDerStunde(KaeltemaschinenStunde s)
        {
            if (!(s.KaelteKwh > 0)) return 0.0;
            if (!s.Takt) return 1.0;
            int anzahl = Math.Max(1, Anzahl);
            int laufend = s.Laufend > 0 ? s.Laufend : anzahl;
            double kapJe = s.KapazitaetKw / anzahl;
            double pMin = Math.Min(Mindestteillast * NennleistungKw, kapJe);
            return pMin > 0 ? Math.Min(1.0, s.KaelteKwh / laufend / pMin) : 1.0;
        }

        /// <summary>Eine Stunde EINER Maschine der Anlagenzeile.</summary>
        private KaeltemaschinenStunde StundeEinzeln(int h, double lastKwh, double kaltwasserC, double rk)
        {
            if (!(lastKwh > 0)) return default;

            if (ParallelFrei(h, kaltwasserC))
            {
                double kap = NennleistungKw;
                if (!(kap > 0)) return default;
                double q = Math.Min(lastKwh, kap);
                double lauf = q / kap;
                return new KaeltemaschinenStunde(q, q / KaelteFestwerte.FREIE_KUEHLUNG_EER,
                                                 HilfsstromRueckkuehlungKw * lauf, kap, true, false, false);
            }

            if (Kennlinie == null || Kennlinie.Leer) return default;
            KaeltemaschinenPunkt p = Kennlinie.Auswerten(rk, kaltwasserC);
            if (!(p.LeistungKw > 0) || !(p.Eer > 0))
                return new KaeltemaschinenStunde(0, 0, 0, 0, false, p.Randwert, false);

            double deckung = Math.Min(lastKwh, p.LeistungKw);
            double mindest = Math.Min(Mindestteillast * NennleistungKw, p.LeistungKw);
            bool takt = mindest > 0 && deckung < mindest;
            double anteil = deckung / p.LeistungKw;
            return new KaeltemaschinenStunde(deckung, deckung / p.Eer, HilfsstromRueckkuehlungKw * anteil,
                                             p.LeistungKw, false, p.Randwert, takt);
        }

        /// <summary>
        /// KM3 (Fachkonzept Teillast und Takten 5.1): eine Verdichterstunde der Anlagenzeile auf dem neuen Weg —
        /// Kennfeldpunkt nach Randweg, Folgeschaltung (nur mit <c>Teillast_Weg</c>, sonst gleichmäßige Teilung auf alle),
        /// Lastgrad, Strom nach Lastachse bzw. Takten mit Mehrstrom, Hilfsstrom der Rückkühlung nach Laufanteil.
        /// </summary>
        private KaeltemaschinenStunde StundeMitWeg(int h, double lastKwh, double kaltwasserC, double rk)
        {
            if (Kennlinie == null || Kennlinie.Leer) return default;
            int anzahl = Math.Max(1, Anzahl);
            KaeltemaschinenRandpunkt p = KaeltemaschinenRand.Auswerten(Kennlinie, rk, kaltwasserC, Teillast.Randweg);
            if (!(p.LeistungKw > 0) || !(p.Eer > 0))
                return new KaeltemaschinenStunde(0, 0, 0, 0, false, p.Randwert, false, 0.0, 0, 0.0, p.Extrapoliert, 0);

            int laufend;
            double lastJe;
            if (TeillastWirksam)
            {
                (laufend, lastJe) = Kaeltemaschinenteillast.Folgeschaltung(lastKwh, p.LeistungKw, anzahl);
            }
            else
            {
                laufend = anzahl;
                lastJe = lastKwh / anzahl;
            }
            KaeltemaschinenTeillaststunde t = Teillast.Stunde(lastJe, p.LeistungKw, p.Eer, NennleistungKw, Mindestteillast);
            if (!(t.KaelteKwh > 0))
                return new KaeltemaschinenStunde(0, 0, 0, p.LeistungKw * anzahl, false, p.Randwert, false, 0.0, 0, 0.0, p.Extrapoliert, 0);
            double anteil = t.KaelteKwh / p.LeistungKw;
            return new KaeltemaschinenStunde(t.KaelteKwh * laufend, t.VerdichterKwh * laufend,
                                             HilfsstromRueckkuehlungKw * anteil * laufend, p.LeistungKw * anzahl,
                                             false, p.Randwert, t.Takt, t.MehrstromKwh * laufend, t.Starts * laufend,
                                             t.Lastgrad, p.Extrapoliert, laufend);
        }

        // =====================================================================
        //  Rückkühltemperatur
        // =====================================================================

        /// <summary>
        /// Die Rückkühltemperatur einer Stunde [°C] nach Rückkühlart. <paramref name="ohneFeuchte"/> ist
        /// <c>true</c>, wenn ein Nasskühler ohne Luftfeuchte auf Außentemperatur − 3 K ausweicht.
        /// </summary>
        /// <param name="art">Rückkühlart (Persistenzwert); unbekannt oder leer = luftgekühlt.</param>
        /// <param name="aussenC">Außentemperatur [°C].</param>
        /// <param name="feuchteProzent">Relative Luftfeuchte [%]; <c>null</c> = nicht verfügbar.</param>
        /// <param name="ohneFeuchte">Ausweichweg des Nasskühlers genommen.</param>
        public static double Rueckkuehltemperatur(string art, double aussenC, double? feuchteProzent, out bool ohneFeuchte)
        {
            ohneFeuchte = false;
            switch (art)
            {
                case KaeltemaschineSchema.RUECKKUEHLART_WASSER:
                    return KaelteFestwerte.RUECKKUEHLTEMPERATUR_WASSER_C;
                case KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER:
                    return aussenC + KaelteFestwerte.GRAEDIGKEIT_TROCKENKUEHLER_K;
                case KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER:
                    if (feuchteProzent.HasValue && !double.IsNaN(feuchteProzent.Value))
                        return Feuchtkugeltemperatur(aussenC, feuchteProzent.Value) + KaelteFestwerte.GRAEDIGKEIT_NASSKUEHLER_K;
                    ohneFeuchte = true;
                    return aussenC - KaelteFestwerte.NASSKUEHLER_OHNE_FEUCHTE_ABSCHLAG_K;
                default:
                    return aussenC + KaelteFestwerte.GRAEDIGKEIT_LUFT_K;
            }
        }

        /// <summary>
        /// Feuchtkugeltemperatur [°C] nach Stull (2011) aus Lufttemperatur und relativer Feuchte —
        /// gültig für 5 % bis 99 % und −20 °C bis 50 °C; die Feuchte wird in dieses Band geklemmt, und das
        /// Ergebnis liegt nie über der Lufttemperatur.
        /// </summary>
        public static double Feuchtkugeltemperatur(double tC, double rhProzent)
        {
            double rh = Math.Max(5.0, Math.Min(99.0, rhProzent));
            double tw = tC * Math.Atan(0.151977 * Math.Sqrt(rh + 8.313659))
                        + Math.Atan(tC + rh) - Math.Atan(rh - 1.676331)
                        + 0.00391838 * Math.Pow(rh, 1.5) * Math.Atan(0.023101 * rh)
                        - 4.686035;
            return Math.Min(tw, tC);
        }

        /// <summary>
        /// Die Rückkühltemperaturen eines Jahres. <paramref name="stundenOhneFeuchte"/> zählt die Stunden,
        /// in denen ein Nasskühler ohne Luftfeuchte ausgewichen ist.
        /// </summary>
        public static double[] RueckkuehltemperaturenBilden(string art, double[] aussenC, double[] feuchteProzent,
                                                            out int stundenOhneFeuchte)
        {
            stundenOhneFeuchte = 0;
            int n = aussenC != null ? aussenC.Length : 0;
            var r = new double[n];
            for (int h = 0; h < n; h++)
            {
                double? f = feuchteProzent != null && h < feuchteProzent.Length && !double.IsNaN(feuchteProzent[h])
                    ? feuchteProzent[h] : (double?)null;
                r[h] = Rueckkuehltemperatur(art, aussenC[h], f, out bool ohne);
                if (ohne) stundenOhneFeuchte++;
            }
            return r;
        }

        // =====================================================================
        //  Aufbau aus der Projektkopie
        // =====================================================================

        /// <summary>
        /// Baut die Rechenklasse aus der Projektkopie. Die Kaltwassertemperatur ist <c>Kuehl_Vorlauf</c> der
        /// Projektkopie, ohne ihn die kleinste Kaltwasser-Stützstelle der Kennlinie (Muster K21), mindestens
        /// <c>Kaltwasser_Vorlauf_Min</c>;
        /// <paramref name="angehoben"/> sagt, ob die Grenze gegriffen hat.
        /// </summary>
        public static Kaeltemaschine AusModell(KaeltemaschineModel m, out bool angehoben)
        {
            angehoben = false;
            if (m == null) return null;
            var k = new KaeltemaschinenKennlinie(m.Kennlinie.Select(p =>
                (p.Rueckkuehltemperatur, p.Kaltwassertemperatur, p.EER, p.Kaelteleistung_kW)));
            double kw = k.Kaltwasserstuetzstellen.Count > 0 ? k.Kaltwasserstuetzstellen[0] : 0.0;
            // KK2: die Untergrenze am Stundenvorlauf — Kaltwasser_Vorlauf_Min, ohne ihn die kleinste Stützstelle.
            double untergrenze = m.Kaltwasser_Vorlauf_Min.HasValue ? m.Kaltwasser_Vorlauf_Min.Value
                : (k.Kaltwasserstuetzstellen.Count > 0 ? k.Kaltwasserstuetzstellen[0] : double.NaN);
            // KU3-4: der Kaltwasservorlauf der Projektkopie (Kuehl_Vorlauf) vor der kleinsten Stützstelle.
            if (m.Kuehl_Vorlauf.HasValue) kw = m.Kuehl_Vorlauf.Value;
            if (m.Kaltwasser_Vorlauf_Min.HasValue && kw < m.Kaltwasser_Vorlauf_Min.Value)
            {
                kw = m.Kaltwasser_Vorlauf_Min.Value;
                angehoben = true;
            }
            double nenn = m.Nennkaelteleistung_kW ?? 0.0;
            if (!(nenn > 0))
                nenn = m.Kennlinie.Where(p => p.Kaelteleistung_kW.HasValue)
                                  .Select(p => p.Kaelteleistung_kW.Value).DefaultIfEmpty(0.0).Max();
            return new Kaeltemaschine
            {
                Id = m.Id,
                Bezeichner = string.IsNullOrEmpty(m.Bezeichner) ? m.Id.ToString(System.Globalization.CultureInfo.CurrentCulture) : m.Bezeichner,
                Rueckkuehlart = string.IsNullOrEmpty(m.Rueckkuehlart) ? KaeltemaschineSchema.RUECKKUEHLART_LUFT : m.Rueckkuehlart,
                NennleistungKw = nenn,
                Mindestteillast = Math.Max(0.0, Math.Min(100.0, m.Mindestteillast_Prozent ?? 0.0)) / 100.0,
                HilfsstromRueckkuehlungKw = Math.Max(0.0, m.Hilfsstrom_Rueckkuehlung_kW ?? 0.0),
                Kaltwassertemperatur = kw,
                KaltwasserVorlaufMinC = untergrenze,
                Kennlinie = k,
                // KM3: die wirksame Teillast der Projektkopie, einmal je Maschine gebildet.
                Teillast = Kaeltemaschinenteillast.AusModell(m)
            };
        }
    }
}
