using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Generator gegen eine DHWcalc-Referenzdatei</b> (Umsetzungskonzept Zapfprofilgenerator
    /// 4.4 Ende, Kapitel 7 Zeile Z3; Kapitel 6: OpenDHW (MIT) nur als Testorakel, Attribution in
    /// <c>Proben/Zapfprofil/OpenDHW/LIESMICH.md</c>).
    ///
    /// <para><b>Datei und Parametrik.</b> Ein Jahr Minutenwerte eines Einfamilienhauses aus DHWcalc
    /// (Volumenstrom je Minute in l/h), unverändert gepackt; der SHA-256 der entpackten Datei steht
    /// im LIESMICH und wird geprüft. Die Parametrik liest der Test aus der Protokolldatei daneben:
    /// Tagesmenge, die Kategorien (mittlerer Volumenstrom, Dauer, Anteil, Streuung), der größte
    /// Volumenstrom als Kappung jeder Kategorie, der Stufentagesgang der Werk- und der Wochenendtage,
    /// das Verhältnis Wochenende/Werktag und aus dem Kopf die Einheitenzahl („single family house"
    /// → n_E = 1). Keine Zahl der Datei steht im Quelltext. Die jahreszeitliche Schwankung der Datei
    /// bildet der Test nicht nach (Monatsfaktoren 1) — sie verschiebt Tage im Jahr, nicht die
    /// verglichenen Verteilungsgrößen; den kleinsten Volumenstrom der Datei ersetzt die Kappung bei 0.</para>
    ///
    /// <para><b>Kein Bitvergleich — Verteilungsgrößen.</b> DHWcalc zieht anders (Wahrscheinlichkeit
    /// je Zeitschritt, Kappung der Summe). Verglichen werden über die Tage: (1) die mittlere
    /// Tagesmenge ±5 % (Auftrag Z3; über vier gezogene Jahre streut ihr Mittel um rund 1 %);
    /// (2) das Verhältnis der Minutenspitze eines Tages zum Tagesmittel je Minute; (3) die Zahl der
    /// Zapfminuten eines Tages und der Anteil der Zapfminuten am Jahr. <b>Korridore aus der Datei:</b>
    /// Der Median des Generators muss in der mittleren Hälfte der Tage der Datei liegen (P25 bis P75,
    /// nächster Rang), der Jahresanteil der Zapfminuten zwischen P25 und P75 der Tagesanteile der
    /// Datei. Begründung: Beide Reihen sind Stichproben von Tagen derselben Parametrik; läge der
    /// typische Tag des Generators außerhalb der mittleren Hälfte der Referenztage, wichen Spitze
    /// oder Dauer der Zapfungen systematisch ab. Der Median aus 4 · 365 gezogenen Tagen streut nur
    /// um rund 1,25 σ / √1460 ≈ 0,03 σ, die halbe Quartilsbreite beträgt rund 0,67 σ — der
    /// Korridor wird nicht zufällig verfehlt.</para>
    /// </summary>
    public sealed class ZapfprofilDhwcalcVergleichTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        public void Dispose() => _kultur.Dispose();

        private const string PROFIL = "200L_1min_4cat_sf_nods_max1200.txt.gz";
        private const string PROTOKOLL = "200L_1min_4cat_sf_nods_max1200_log.txt";

        /// <summary>SHA-256 der entpackten Originaldatei (LIESMICH) — die Probe liest, was OpenDHW liefert.</summary>
        private const string SHA256_PROFIL = "0e870c76c5fec3791471dd4ad1ee1cf76a613031d2d83e787d78e645d17d9105";

        /// <summary>Gezogene Jahre des Generators; die Datei trägt eines.</summary>
        private const int JAHRE = 4;

        /// <summary>Seed des Vergleichs (Vorgabe des Papiers, 4.4).</summary>
        private const long SEED = 1;

        /// <summary>Spreizung zur Umrechnung Liter ↔ kWh [K]; sie kürzt sich im Vergleich heraus.</summary>
        private const double SPREIZUNG_K = 50.0;

        /// <summary>Toleranz der mittleren Tagesmenge (Auftrag Z3: ±5 %).</summary>
        private const double TOLERANZ_TAGESMENGE = 0.05;

        private const int MINUTEN_JAHR = Zapfkalender.TAGE * Bedarfstag.MINUTEN;

        private static readonly Provenienz Quelle = new Provenienz("DHWcalc über OpenDHW (MIT), nur Test", null, "b62ac8b4", Herkunftsart.Frei);

        [Fact]
        public void Der_Generator_trifft_die_Verteilung_der_DHWcalc_Datei()
        {
            string ordner = Path.Combine(ZapfZufallTests.Probenordner(), "OpenDHW");
            Parametrik p = Parametrik.Lesen(File.ReadAllLines(Path.Combine(ordner, PROTOKOLL)));
            Tagesverteilung datei = Tagesverteilung.Aus(Datei(Path.Combine(ordner, PROFIL)));
            Tagesverteilung gen = Tagesverteilung.Aus(Generator(p));

            // Die Datei ist, was ihr Protokoll sagt: ein Jahr, die mittlere Tagesmenge.
            Assert.Equal(Zapfkalender.TAGE, datei.Tage);
            Assert.InRange(datei.MittelLJeTag, p.TagesmengeL * 0.999, p.TagesmengeL * 1.001);
            Assert.Equal(JAHRE * Zapfkalender.TAGE, gen.Tage);

            string bericht = "Datei: " + datei + " | Generator: " + gen;
            // (1) Mittlere Tagesmenge ±5 %.
            Assert.True(Math.Abs(gen.MittelLJeTag / datei.MittelLJeTag - 1.0) <= TOLERANZ_TAGESMENGE, bericht);
            // (2) Minutenspitze / Tagesmittel: Median des Generators in P25 … P75 der Datei.
            Assert.True(Quantil(datei.Spitzenverhaeltnis, 25) < Quantil(datei.Spitzenverhaeltnis, 75), bericht);
            Assert.InRange(Quantil(gen.Spitzenverhaeltnis, 50), Quantil(datei.Spitzenverhaeltnis, 25), Quantil(datei.Spitzenverhaeltnis, 75));
            // (3) Zapfminuten je Tag und Anteil der Zapfminuten am Jahr.
            Assert.InRange(Quantil(gen.Zapfminuten, 50), Quantil(datei.Zapfminuten, 25), Quantil(datei.Zapfminuten, 75));
            Assert.InRange(gen.AnteilZapfminuten, Quantil(datei.Zapfminuten, 25) / Bedarfstag.MINUTEN,
                           Quantil(datei.Zapfminuten, 75) / Bedarfstag.MINUTEN);
        }

        // =================================================================================
        // Datei, Generator, Verteilung
        // =================================================================================

        /// <summary>Die Minutenreihe der Datei [l/min]: entpackt, SHA-256 geprüft, l/h je Zeile durch 60.</summary>
        private static double[] Datei(string pfad)
        {
            byte[] roh;
            using (var ein = new GZipStream(File.OpenRead(pfad), CompressionMode.Decompress))
            using (var aus = new MemoryStream())
            {
                ein.CopyTo(aus);
                roh = aus.ToArray();
            }
            Assert.Equal(SHA256_PROFIL, Convert.ToHexString(SHA256.HashData(roh)).ToLowerInvariant());
            string[] zeilen = System.Text.Encoding.ASCII.GetString(roh).Split('\n');
            var werte = new List<double>(MINUTEN_JAHR);
            foreach (string z in zeilen)
            {
                string t = z.Trim();
                if (t.Length == 0) continue;
                werte.Add(double.Parse(t, NumberStyles.Float, CultureInfo.InvariantCulture) / 60.0);
            }
            return werte.ToArray();
        }

        /// <summary>
        /// JAHRE gezogene Jahre des Generators als Minutenreihe [l/min] — wie das Jahresensemble: je
        /// Jahr und Einheit ein Zufallsstrom (Realisierungsseed, Zone 0, Einheit), Tagesmengen aus dem
        /// Formvektor, je Tag die Ereignisse des Zapfereignisgenerators, über Mitternacht in den
        /// nächsten Tag und am Jahresende an den Jahresanfang.
        /// </summary>
        private static double[] Generator(Parametrik p)
        {
            var kategorien = new Zapfkategorie[p.Kategorien.Count];
            for (int i = 0; i < kategorien.Length; i++)
            {
                Kategoriewert k = p.Kategorien[i];
                kategorien[i] = new Zapfkategorie(1, "DHWcalc-Kategorie " + (i + 1), k.MittelLJeH / 60.0, k.StreuungLJeH / 60.0,
                                                  k.DauerMin, k.AnteilProzent / 100.0, Quelle)
                { KappungLJeMin = p.GroessterLJeH / 60.0 };
            }
            Zapfkategoriensatz satz = Zapfkategoriensatz.Aus(kategorien, 1, "Einfamilienhaus");

            var gaenge = new double[Tagesgangsatz.TAGTYPEN, Zapfkalender.STUNDEN_TAG];
            for (int h = 0; h < Zapfkalender.STUNDEN_TAG; h++)
            {
                gaenge[(int)ZapfTagtyp.Werktag - 1, h] = p.Werktag[h];
                gaenge[(int)ZapfTagtyp.Samstag - 1, h] = p.Wochenende[h];
                gaenge[(int)ZapfTagtyp.SonnFeiertag - 1, h] = p.Wochenende[h];
                gaenge[(int)ZapfTagtyp.Ruhetag - 1, h] = p.Werktag[h];
            }
            double w = p.WochenendeJeWerktag;
            double[] woche = { 1.0, 1.0, 1.0, 1.0, 1.0, w, w };
            double summe = woche.Sum();
            var s = new Zeitstruktur(Enumerable.Repeat(1.0, Zapfkalender.MONATE).ToArray(), woche.Select(x => x / summe).ToArray(),
                                     null, gaenge, new bool[Tagesgangsatz.TAGTYPEN]);
            bool[] we = Enumerable.Range(1, Zapfkalender.TAGE).Select(d => Zapfkalender.Wochentag(0, d) >= Zapfkalender.SAMSTAG).ToArray();
            ZapfTagtyp[] kalender = Zapfkalender.Bilden(0, we, new Ferienfenster[0]);
            double kwhJeLiter = Mengengeruest.WAERMEKAPAZITAET_WASSER_WH_JE_L_K * SPREIZUNG_K / Mengengeruest.WH_JE_KWH;
            double jahrJeEinheitKwh = p.TagesmengeL * Zapfkalender.TAGE * kwhJeLiter / p.Einheiten;
            double[] tage = Formvektor.Tagesmengen(jahrJeEinheitKwh, s, kalender, 0, Enumerable.Repeat(1.0, Zapfkalender.MONATE).ToArray());
            var dichten = new Tageszeitdichte[Tagesgangsatz.TAGTYPEN];
            for (int t = 0; t < dichten.Length; t++) dichten[t] = Tageszeitdichte.Aus(s, (ZapfTagtyp)(t + 1));

            var minuten = new double[JAHRE * MINUTEN_JAHR];
            var ereignisse = new List<Zapfereignis>();
            for (int r = 0; r < JAHRE; r++)
                for (int u = 0; u < p.Einheiten; u++)
                {
                    var zufall = new ZapfZufall(ZapfZufall.Kindseed(ZapfZufall.Kindseed(ZapfZufall.Realisierungsseed(SEED, r), 0), u));
                    for (int d = 0; d < Zapfkalender.TAGE; d++)
                    {
                        ereignisse.Clear();
                        Zapfereignisgenerator.Ziehen(zufall, satz, tage[d], SPREIZUNG_K, dichten[(int)kalender[d] - 1], ereignisse);
                        foreach (Zapfereignis e in ereignisse)
                        {
                            double literJeMinute = e.EnergieKwh / e.DauerMin / kwhJeLiter;
                            for (int k = 0; k < e.DauerMin; k++)
                                minuten[r * MINUTEN_JAHR + (d * Bedarfstag.MINUTEN + e.MinuteBeginn + k) % MINUTEN_JAHR] += literJeMinute;
                        }
                    }
                }
            return minuten;
        }

        /// <summary>Das Perzentil p einer Stichprobe nach dem nächsten Rang (wie <see cref="Perzentilwerte"/>).</summary>
        private static double Quantil(IReadOnlyList<double> werte, int p)
        {
            double[] w = werte.OrderBy(x => x).ToArray();
            return w[Perzentilwerte.Rang(w.Length, p) - 1];
        }

        /// <summary>Die Tagesgrößen einer Minutenreihe [l/min] aus ganzen Tagen.</summary>
        private sealed class Tagesverteilung
        {
            internal int Tage;
            internal double MittelLJeTag;
            internal double AnteilZapfminuten;
            internal List<double> Spitzenverhaeltnis = new List<double>();
            internal List<double> Zapfminuten = new List<double>();

            internal static Tagesverteilung Aus(double[] minuten)
            {
                Assert.Equal(0, minuten.Length % Bedarfstag.MINUTEN);
                var v = new Tagesverteilung { Tage = minuten.Length / Bedarfstag.MINUTEN };
                double summe = 0.0;
                long zapf = 0;
                foreach (double x in minuten)
                {
                    summe += x;
                    if (x > 0) zapf++;
                }
                v.MittelLJeTag = summe / v.Tage;
                v.AnteilZapfminuten = (double)zapf / minuten.Length;
                double mittelJeMinute = v.MittelLJeTag / Bedarfstag.MINUTEN;
                for (int d = 0; d < v.Tage; d++)
                {
                    double spitze = 0.0;
                    int n = 0;
                    for (int t = d * Bedarfstag.MINUTEN; t < (d + 1) * Bedarfstag.MINUTEN; t++)
                    {
                        if (minuten[t] > spitze) spitze = minuten[t];
                        if (minuten[t] > 0) n++;
                    }
                    v.Spitzenverhaeltnis.Add(spitze / mittelJeMinute);
                    v.Zapfminuten.Add(n);
                }
                return v;
            }

            public override string ToString()
                => string.Format(CultureInfo.InvariantCulture,
                       "{0:0.0} l/d, Spitze/Mittel P25/P50/P75 {1:0.0}/{2:0.0}/{3:0.0}, Zapfminuten P25/P50/P75 {4}/{5}/{6}, Anteil {7:0.0000}",
                       MittelLJeTag, Quantil(Spitzenverhaeltnis, 25), Quantil(Spitzenverhaeltnis, 50), Quantil(Spitzenverhaeltnis, 75),
                       Quantil(Zapfminuten, 25), Quantil(Zapfminuten, 50), Quantil(Zapfminuten, 75), AnteilZapfminuten);
        }

        private sealed record Kategoriewert(double MittelLJeH, int DauerMin, double AnteilProzent, double StreuungLJeH);

        /// <summary>
        /// Die Parametrik aus der DHWcalc-Protokolldatei: Tagesmenge [l/d], Zeitschritt (1 min),
        /// Einheiten (Einfamilienhaus → 1), Kategorien, größter Volumenstrom [l/h], Stufentagesgänge
        /// (24 Stundenanteile, Σ 1) und das Verhältnis Wochenende/Werktag.
        /// </summary>
        private sealed class Parametrik
        {
            internal double TagesmengeL;
            internal int Einheiten;
            internal double GroessterLJeH;
            internal double WochenendeJeWerktag;
            internal List<Kategoriewert> Kategorien = new List<Kategoriewert>();
            internal double[] Werktag;
            internal double[] Wochenende;

            internal static Parametrik Lesen(string[] zeilen)
            {
                string text = string.Join("\n", zeilen);
                var p = new Parametrik
                {
                    TagesmengeL = Zahl(text, @"Mean daily draw-off vol\.:\s*([0-9.]+)\s*l/day"),
                    GroessterLJeH = Zahl(text, @"max\. flow rate:\s*([0-9.]+)\s*l/h"),
                    WochenendeJeWerktag = Zahl(text, @"on weekend-days/on weekdays:\s*([0-9.]+)\s*%") / 100.0
                };
                Assert.Equal(1.0, Zahl(text, @"Time step duration:\s*([0-9.]+)\s*min"));
                Assert.Contains("for a single family house", text);
                p.Einheiten = 1;
                double[] mittel = Zahlen(text, @"Mean flow Rate:(.*)l/h"), dauer = Zahlen(text, @"Duration of draw-off:(.*)min"),
                         anteil = Zahlen(text, @"portion:(.*)%"), streuung = Zahlen(text, @"sigma:(.*)l/h");
                Assert.True(mittel.Length > 0 && new[] { dauer.Length, anteil.Length, streuung.Length }.All(n => n == mittel.Length));
                for (int i = 0; i < mittel.Length; i++)
                    p.Kategorien.Add(new Kategoriewert(mittel[i], (int)dauer[i], anteil[i], streuung[i]));

                var werk = new List<(int, int, double)>();
                var ende = new List<(int, int, double)>();
                List<(int, int, double)> ziel = null;
                var stufe = new Regex(@"^\s*(\d{2}):(\d{2})-(\d{2}):(\d{2})\s+([0-9.]+)\s*%");
                foreach (string z in zeilen)
                {
                    string t = z.Trim();
                    if (t == "weekdays") ziel = werk;
                    else if (t == "weekend-days") ziel = ende;
                    Match m = stufe.Match(z);
                    if (m.Success && ziel != null)
                        ziel.Add((Minute(m, 1), Minute(m, 3), double.Parse(m.Groups[5].Value, CultureInfo.InvariantCulture)));
                }
                Assert.NotEmpty(werk);
                Assert.NotEmpty(ende);
                p.Werktag = Stundenanteile(werk);
                p.Wochenende = Stundenanteile(ende);
                return p;
            }

            /// <summary>Die Stufen (Beginn, Ende [min], Anteil) gleichmäßig auf ihre Minuten, zu 24 Stundenanteilen mit Σ 1.</summary>
            private static double[] Stundenanteile(List<(int Von, int Bis, double Anteil)> stufen)
            {
                var minute = new double[Bedarfstag.MINUTEN];
                foreach (var (von, bis, anteil) in stufen)
                {
                    int ende = bis > von ? bis : bis + Bedarfstag.MINUTEN;
                    for (int t = von; t < ende; t++) minute[t % Bedarfstag.MINUTEN] += anteil / (ende - von);
                }
                double summe = minute.Sum();
                var stunden = new double[Zapfkalender.STUNDEN_TAG];
                for (int t = 0; t < Bedarfstag.MINUTEN; t++) stunden[t / Bedarfstag.MINUTEN_JE_STUNDE] += minute[t] / summe;
                return stunden;
            }

            private static int Minute(Match m, int gruppe)
                => int.Parse(m.Groups[gruppe].Value, CultureInfo.InvariantCulture) * 60 + int.Parse(m.Groups[gruppe + 1].Value, CultureInfo.InvariantCulture);

            private static double Zahl(string text, string muster)
            {
                Match m = Regex.Match(text, muster);
                Assert.True(m.Success, "Die Protokolldatei nennt „" + muster + "“ nicht.");
                return double.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            }

            private static double[] Zahlen(string text, string muster)
            {
                Match m = Regex.Match(text, muster);
                Assert.True(m.Success, "Die Protokolldatei nennt „" + muster + "“ nicht.");
                return m.Groups[1].Value.Split((char[])null, StringSplitOptions.RemoveEmptyEntries)
                        .Select(x => double.Parse(x, CultureInfo.InvariantCulture)).ToArray();
            }
        }
    }
}
