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

        /// <summary>Legt das Stundenergebnis an.</summary>
        public KaeltemaschinenStunde(double kaelte, double verdichter, double hilfs, double kapazitaet,
                                     bool frei, bool rand, bool takt)
        {
            KaelteKwh = kaelte;
            VerdichterKwh = verdichter;
            HilfsstromKwh = hilfs;
            KapazitaetKw = kapazitaet;
            FreieKuehlung = frei;
            Randwert = rand;
            Takt = takt;
        }

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

        /// <summary>Kaltwassertemperatur des Laufs [°C].</summary>
        public double Kaltwassertemperatur;

        /// <summary>Die Kennlinie der Projektkopie.</summary>
        public KaeltemaschinenKennlinie Kennlinie;

        /// <summary>Rückkühltemperatur je Stunde [°C].</summary>
        public double[] Rueckkuehltemperatur_stuendlich;

        /// <summary>Kann die Maschine frei kühlen (Trocken- oder Nasskühler)?</summary>
        public bool FreieKuehlungMoeglich =>
            Rueckkuehlart == KaeltemaschineSchema.RUECKKUEHLART_TROCKENKUEHLER ||
            Rueckkuehlart == KaeltemaschineSchema.RUECKKUEHLART_NASSKUEHLER;

        /// <summary>Kühlt die Maschine in Stunde <paramref name="h"/> frei?</summary>
        public bool FreieKuehlung(int h)
        {
            if (!FreieKuehlungMoeglich || Rueckkuehltemperatur_stuendlich == null ||
                h < 0 || h >= Rueckkuehltemperatur_stuendlich.Length) return false;
            return Rueckkuehltemperatur_stuendlich[h] <= Kaltwassertemperatur - KaelteFestwerte.FREIE_KUEHLUNG_ABSTAND_K;
        }

        /// <summary>
        /// Rechnet eine Stunde: wie viel der Last <paramref name="lastKwh"/> die Maschine deckt und mit
        /// welchem Strom. Die Last ist der offene Kältebedarf der Stunde [kWh].
        /// </summary>
        public KaeltemaschinenStunde Stunde(int h, double lastKwh)
        {
            if (!(lastKwh > 0)) return default;
            double rk = Rueckkuehltemperatur_stuendlich != null && h >= 0 && h < Rueckkuehltemperatur_stuendlich.Length
                ? Rueckkuehltemperatur_stuendlich[h] : KaelteFestwerte.RUECKKUEHLTEMPERATUR_WASSER_C;

            if (FreieKuehlung(h))
            {
                double kap = NennleistungKw;
                if (!(kap > 0)) return default;
                double q = Math.Min(lastKwh, kap);
                double lauf = q / kap;
                return new KaeltemaschinenStunde(q, q / KaelteFestwerte.FREIE_KUEHLUNG_EER,
                                                 HilfsstromRueckkuehlungKw * lauf, kap, true, false, false);
            }

            if (Kennlinie == null || Kennlinie.Leer) return default;
            KaeltemaschinenPunkt p = Kennlinie.Auswerten(rk, Kaltwassertemperatur);
            if (!(p.LeistungKw > 0) || !(p.Eer > 0))
                return new KaeltemaschinenStunde(0, 0, 0, 0, false, p.Randwert, false);

            double deckung = Math.Min(lastKwh, p.LeistungKw);
            double mindest = Math.Min(Mindestteillast * NennleistungKw, p.LeistungKw);
            bool takt = mindest > 0 && deckung < mindest;
            double anteil = deckung / p.LeistungKw;
            return new KaeltemaschinenStunde(deckung, deckung / p.Eer, HilfsstromRueckkuehlungKw * anteil,
                                             p.LeistungKw, false, p.Randwert, takt);
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
        /// Baut die Rechenklasse aus der Projektkopie. Die Kaltwassertemperatur ist die kleinste
        /// Kaltwasser-Stützstelle der Kennlinie (Muster K21), mindestens <c>Kaltwasser_Vorlauf_Min</c>;
        /// <paramref name="angehoben"/> sagt, ob die Grenze gegriffen hat.
        /// </summary>
        public static Kaeltemaschine AusModell(KaeltemaschineModel m, out bool angehoben)
        {
            angehoben = false;
            if (m == null) return null;
            var k = new KaeltemaschinenKennlinie(m.Kennlinie.Select(p =>
                (p.Rueckkuehltemperatur, p.Kaltwassertemperatur, p.EER, p.Kaelteleistung_kW)));
            double kw = k.Kaltwasserstuetzstellen.Count > 0 ? k.Kaltwasserstuetzstellen[0] : 0.0;
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
                Kennlinie = k
            };
        }
    }
}
