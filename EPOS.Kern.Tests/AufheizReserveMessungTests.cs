using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using EPOS.Referenzlaeufe.Skripte;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Ein Messfall</b>: übersprungen, solange die Umgebungsvariable <c>EPOS_MESSUNG</c> nicht <c>1</c> ist —
    /// das Gate und beide Workflows fahren ihn nie (sie filtern nicht, der Fall überspringt sich selbst).
    /// </summary>
    public sealed class MessungFactAttribute : FactAttribute
    {
        internal const string SCHALTER = "EPOS_MESSUNG";

        public MessungFactAttribute()
        {
            if (Environment.GetEnvironmentVariable(SCHALTER) != "1")
                Skip = "Messung - nur mit " + SCHALTER + "=1";
        }
    }

    /// <summary>
    /// <b>Messharness der Aufheizreserve ρ_min</b> (Entwurf KP3, E58 F7 (c); Vorlage für den Entscheid P14):
    /// Je VDI-6007-Gebäude der sechzehn Referenzprojekte, dazu das Gebäude von 1051 und die beheizten Zonen von
    /// 1052, und je Bemessung (a) „kälteste Stunde" und (b) „kälteste Stunde − 2 K" die kleinste Reserve ρ, bei
    /// der <b>W1 = 0</b> (kein Tag unerreichbar) und <b>W3 = 0</b> (das Nachweisband hält). Suche: Raster 0, 5,
    /// 10 … 100 %, aufsteigend bis zum ersten Treffer, dann Halbierung zwischen dem letzten Fehlschlag und dem
    /// Treffer, bis die Lücke höchstens 1 % ist; ρ_min ist die obere Grenze. Gerechnet wird mit dem echten
    /// Kern (<see cref="AufheizLauf"/>), täglich, ohne Aufschlag; ρ im Code bleibt unberührt.
    ///
    /// <para><b>Ausgabe</b>: eine CSV nach <c>EPOS_MESSUNG_ZIEL</c> (Vorgabe <c>/tmp/rp1_rho/rho_min.csv</c>),
    /// Trennzeichen „;", Zahlen invariant, eine Zeile je Einheit und Bemessung:
    /// <c>Projekt;Gebaeude;Zone;Bemessung;rho_min_Prozent;W1_20;W3_20;t_auf_max_h;Rampentage;W2;Spitze_mit_kW;Spitze_ohne_kW;Waerme_mit_MWh;Waerme_ohne_MWh;Zustand;rho_bem_Prozent;t_auf_max_bem_h;Laeufe</c>
    /// — <c>Zone</c> „–" für ein Einzonengebäude; <c>rho_min_Prozent</c> leer, wenn auch 100 % nicht reichen;
    /// <c>W1_20</c>/<c>W3_20</c> die Zähler bei der Vorgabe 20 %; <c>t_auf_max_h</c>, <c>Rampentage</c>, <c>W2</c>,
    /// Spitze und Jahreswärme „mit" bei ρ_min (ohne Treffer bei 100 %), „ohne" mit ausgeschalteter Optimierung;
    /// <c>Zustand</c> der Aufheizzustand bei ρ_min (UNERREICHBAR: der Bemessungsfall der gewählten Bemessung
    /// ist bei ρ_min nicht erreichbar, t_auf,max bleibt dann leer); <c>rho_bem_Prozent</c> die kleinste Reserve,
    /// bei der zusätzlich der Bemessungsfall erreichbar ist (Zustand nicht UNERREICHBAR), und
    /// <c>t_auf_max_bem_h</c> t_auf,max dort; <c>Laeufe</c> die Zahl der gerechneten ρ.</para>
    ///
    /// Aufruf: <c>EPOS_MESSUNG=1 dotnet test EPOS.Kern.Tests -c Release --filter "FullyQualifiedName~AufheizReserveMessung"</c>.
    /// </summary>
    [Collection("Testdatenbank")]
    [Trait("Kategorie", "Messung")]
    public sealed class AufheizReserveMessungTests : IClassFixture<TestDatenbank>
    {
        internal const string ZIEL_VARIABLE = "EPOS_MESSUNG_ZIEL";
        internal const string ZIEL_VORGABE = "/tmp/rp1_rho/rho_min.csv";
        internal const string KOPF = "Projekt;Gebaeude;Zone;Bemessung;rho_min_Prozent;W1_20;W3_20;t_auf_max_h;Rampentage;W2;" +
                                     "Spitze_mit_kW;Spitze_ohne_kW;Waerme_mit_MWh;Waerme_ohne_MWh;Zustand;rho_bem_Prozent;t_auf_max_bem_h;Laeufe";

        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public AufheizReserveMessungTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        /// <summary>Die Messwerte einer Einheit (Gebäude oder Zone) in einem Lauf.</summary>
        private sealed record Wert(string Zone, int W1, int W2, int W3, int? TaufMaxH, int Rampentage, double SpitzeKw, double WaermeMwh,
                                   string Zustand)
        {
            internal bool Haelt => W1 == 0 && W3 == 0;

            /// <summary>Dazu ist der Bemessungsfall der gewählten Bemessung erreichbar.</summary>
            internal bool HaeltBemessen => Haelt && Zustand != DbWerte.AUFHEIZ_ZUSTAND_UNERREICHBAR;
        }

        private static IEnumerable<int> Projekte()
            => AufheizLauf.Referenzprojekte.Append(Konditionierungsprojekt1051.NEU).Append(Zonenprojekt1052.NEU);

        /// <summary>Ein Lauf eines Gebäudes; je Einheit die Werte (Einzone: eine, Zonen: je Zone ohne die unbeheizten).</summary>
        private static List<Wert> Rechnen(int projekt, int gebaeude, Aufheizvorgabe v)
        {
            AufheizLauf.Gebaeudelauf l = AufheizLauf.Projekt(projekt, v, double.NaN, g => g.ID_Gebaeude == gebaeude).Single();
            Assert.True(l.Gerechnet, l + ": nicht gerechnet");
            var w = new List<Wert>();
            if (l.Mehrzonen == null)
            {
                w.Add(Messen("–", l.Ergebnis));
                return w;
            }
            for (int i = 0; i < l.Mehrzonen.Zonen.Count; i++)
            {
                // Ohne Optimierung trägt keine Zone einen Plan: dann alle Zonen (zugeordnet wird über den Namen).
                if (l.Mehrzonen.Eingaenge[i].Aufheizplan?.Unbeheizt ?? false) continue;
                w.Add(Messen(l.Mehrzonen.Eingaenge[i].Bezeichnung, l.Mehrzonen.Zonen[i]));
            }
            return w;
        }

        private static Wert Messen(string zone, GebaeudeModellErgebnis e)
        {
            Aufheizergebnis a = e.Aufheizung;
            return new Wert(zone, a?.AufheiztageUnerreichbar ?? 0, a?.AufheiztageBegrenzt ?? 0, a?.AufheiztageNachweisband ?? 0,
                            a?.AufheizzeitMaxH, a?.Aufheiztage ?? 0, e.HeizlastW.Max() / 1000.0, e.HeizlastW.Sum() / 1e6,
                            a?.AufheizZustand);
        }

        private static string Z(double x, string f) => x.ToString(f, CultureInfo.InvariantCulture);

        /// <summary>
        /// Die kleinste Reserve, bei der <paramref name="haelt"/> gilt: Raster 0, 5 … 100 % aufsteigend bis zum ersten
        /// Treffer, dann Halbierung zwischen letztem Fehlschlag und Treffer bis zur Lücke ≤ 1 %; <c>null</c> = auch 100 % nicht.
        /// </summary>
        private static double? Suchen(Func<double, bool> haelt)
        {
            double vorher = double.NaN;
            for (int k = 0; k <= 20; k++)
            {
                double rho = k * 0.05;
                if (!haelt(rho))
                {
                    vorher = rho;
                    continue;
                }
                if (double.IsNaN(vorher)) return rho;
                double lo = vorher, hi = rho;
                while (hi - lo > 0.01 + 1e-12)
                {
                    double mitte = 0.5 * (lo + hi);
                    if (haelt(mitte)) hi = mitte; else lo = mitte;
                }
                return hi;
            }
            return null;
        }

        [MessungFact]
        public void Rho_min_je_Gebaeude_und_Bemessung()
        {
            if (!_db.Vorhanden) return;
            string ziel = Environment.GetEnvironmentVariable(ZIEL_VARIABLE);
            if (string.IsNullOrWhiteSpace(ziel)) ziel = ZIEL_VORGABE;
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(ziel)));
            var uhr = Stopwatch.StartNew();
            var csv = new StringBuilder(KOPF).Append('\n');
            int laeufeGesamt = 0;

            foreach (int projekt in Projekte())
            {
                var gebaeude = new ProjektGebaeudeCtrl();
                gebaeude.ReadAll(projekt);
                for (int gi = 0; gi < gebaeude.rows; gi++)
                {
                    ProjektGebaeudeModel g = gebaeude.items[gi];
                    if (!Gebaeuderechenweg.IstVdi6007(g.Gebaeude_Modell)) continue;
                    int id = g.ID_Gebaeude;
                    List<Wert> ohne = Rechnen(projekt, id, Aufheizvorgabe.Aus);
                    laeufeGesamt++;

                    foreach ((string name, string bemessung) in new[] { ("a", DbWerte.AUFHEIZ_BEMESSUNG_STUNDE), ("b", DbWerte.AUFHEIZ_BEMESSUNG_STUNDE_ABZUG) })
                    {
                        // Ein Lauf je ρ, geteilt von den Zonen desselben Gebäudes.
                        var cache = new Dictionary<double, List<Wert>>();
                        List<Wert> Bei(double rho)
                        {
                            if (!cache.TryGetValue(rho, out List<Wert> w))
                            {
                                w = Rechnen(projekt, id, new Aufheizvorgabe(true, bemessung, null, rho, DbWerte.AUFHEIZ_ART_TAEGLICH));
                                cache[rho] = w;
                            }
                            return w;
                        }

                        List<Wert> vorgabe = Bei(0.2);
                        for (int ei = 0; ei < vorgabe.Count; ei++)
                        {
                            int e = ei;
                            double? min = Suchen(rho => Bei(rho)[e].Haelt);
                            double? bem = Suchen(rho => Bei(rho)[e].HaeltBemessen);
                            Wert bei = Bei(min ?? 1.0)[ei];
                            Wert o = ohne.Single(x => x.Zone == bei.Zone);
                            csv.Append(string.Join(";", projekt.ToString(CultureInfo.InvariantCulture), id.ToString(CultureInfo.InvariantCulture),
                                bei.Zone, name, min.HasValue ? Z(100.0 * min.Value, "0.###") : "",
                                vorgabe[ei].W1.ToString(CultureInfo.InvariantCulture), vorgabe[ei].W3.ToString(CultureInfo.InvariantCulture),
                                bei.TaufMaxH?.ToString(CultureInfo.InvariantCulture) ?? "", bei.Rampentage.ToString(CultureInfo.InvariantCulture),
                                bei.W2.ToString(CultureInfo.InvariantCulture), Z(bei.SpitzeKw, "0.000"), Z(o.SpitzeKw, "0.000"),
                                Z(bei.WaermeMwh, "0.0000"), Z(o.WaermeMwh, "0.0000"), bei.Zustand ?? "",
                                bem.HasValue ? Z(100.0 * bem.Value, "0.###") : "",
                                bem.HasValue ? Bei(bem.Value)[ei].TaufMaxH?.ToString(CultureInfo.InvariantCulture) ?? "" : "",
                                cache.Count.ToString(CultureInfo.InvariantCulture))).Append('\n');
                        }
                        laeufeGesamt += cache.Count;
                        _aus.WriteLine("{0}/{1} ({2}): {3} Läufe, {4:0} s", projekt, id, name, cache.Count, uhr.Elapsed.TotalSeconds);
                    }
                }
            }
            File.WriteAllText(ziel, csv.ToString(), new UTF8Encoding(false));
            _aus.WriteLine("ρ_min: {0} Läufe in {1:0} s nach {2}", laeufeGesamt, uhr.Elapsed.TotalSeconds, ziel);
            _aus.WriteLine(csv.ToString());
        }
    }
}
