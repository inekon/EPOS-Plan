using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
using EPOS.Referenzlaeufe.Skripte;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Abdruck eines Aufheizlaufs</b> für den Determinismus (N-AH7): je Plan Zustand, Bemessung und
    /// je Sprung (h_s, n), dazu die Aufheizwerte des Gebäudes — und getrennt ein Hash der Heizreihe. Ohne
    /// Typen aus <c>xunit</c>, weil <see cref="AufheizDeterminismusTests"/> ihn auch in einer zweiten Kopie
    /// der Assemblys ruft (gestörter Lauf).
    /// </summary>
    internal static class Aufheizabdruck
    {
        internal static string Plaene(AufheizLauf.Gebaeudelauf l)
        {
            var sb = new StringBuilder();
            IEnumerable<Aufheizplan> plaene = l.Mehrzonen != null
                ? l.Mehrzonen.Eingaenge.Select(z => z.Aufheizplan)
                : new[] { l.Plan };
            int i = 0;
            foreach (Aufheizplan p in plaene)
            {
                sb.Append("Zone ").Append(i++).Append(": ").Append(p.Zustand);
                if (p.Bemessung != null)
                    sb.Append(", t_auf,max ").Append(Text(p.Bemessung.VarianteA.AufheizzeitMaxH)).Append('/')
                      .Append(Text(p.Bemessung.VarianteB.AufheizzeitMaxH));
                sb.Append(", Sprünge:");
                foreach (Aufheizsprung sp in p.Spruenge)
                    sb.Append(' ').Append(sp.Sprungstunde.ToString(CultureInfo.InvariantCulture)).Append(':')
                      .Append(sp.N.ToString(CultureInfo.InvariantCulture));
                sb.Append('\n');
            }
            Aufheizergebnis a = l.Ergebnis.Aufheizung;
            sb.Append("Gebäude: ").Append(a.AufheizZustand).Append(", Tage ").Append(Text(a.Aufheiztage))
              .Append(", W1 ").Append(Text(a.AufheiztageUnerreichbar)).Append(", W2 ").Append(Text(a.AufheiztageBegrenzt))
              .Append(", W3 ").Append(Text(a.AufheiztageNachweisband)).Append(", W4 ").Append(Text(a.AufheizspruengeAus))
              .Append(", Σ ").Append(Text(a.AufheizstundenH)).Append(", längste ").Append(Text(a.AufheizzeitLaengsteH));
            return sb.ToString();
        }

        internal static string Reihenhash(AufheizLauf.Gebaeudelauf l)
        {
            var bytes = new byte[8 * 8760];
            for (int h = 0; h < 8760; h++)
                BitConverter.GetBytes(BitConverter.DoubleToInt64Bits(l.Ergebnis.HeizlastW[h])).CopyTo(bytes, 8 * h);
            return Convert.ToHexString(SHA256.HashData(bytes));
        }

        private static string Text(int? x) => x.HasValue ? x.Value.ToString(CultureInfo.InvariantCulture) : "NULL";

        /// <summary>
        /// <b>Ergebniszeile und Export</b> eines Laufs (N-AH7, Welle D2): die Zeile aus
        /// <see cref="GebaeudeKennzahlen.Bilden"/> (Gebäude und Zonen) und der Satz aus
        /// <see cref="GebaeudeErgebnisexport.Satz"/>. <paramref name="mitZahlen"/>: jede Gleitkommazahl Bit für Bit
        /// (zwei Läufe, beide Kulturen); sonst nur Zustände, Texte, Ganzzahlen und Schlüssel — der Vergleich des
        /// gestörten Laufs, in dem die letzten Bits von P_auf und T_a abweichen dürfen, n und die Zähler nicht.
        /// </summary>
        internal static string Zeile(AufheizLauf.Gebaeudelauf l, bool mitZahlen)
        {
            double[] kw = (double[])l.Ziel.Clone();
            WPPlan.Core.BhkwPlan.WattToKw(kw);
            ErgebnisGebaeudeModel z = GebaeudeKennzahlen.Bilden(0, l.Gebaeude, "", DbWerte.GEBAEUDE_MODELL_VDI6007, kw, l.Ergebnis);
            GebaeudeExportsatz satz = GebaeudeErgebnisexport.Satz(l.Ergebnis);
            var sb = new StringBuilder();
            if (mitZahlen)
            {
                sb.Append("Zeile ").Append(AufheizKennzahlenTests.Abdruck(z)).Append(" | ").Append(Bits(z.HeizwaermeMwh))
                  .Append(' ').Append(Bits(z.SpitzeKw)).Append(' ').Append(Text(z.SommerlueftungsstundenH)).Append('\n');
                foreach (ErgebnisZoneModel zz in z.Zonen) sb.Append("Zone ").Append(AufheizKennzahlenTests.Abdruck(zz)).Append('\n');
            }
            else
            {
                sb.Append("Zeile ").Append(Ganz(z.AufheizZustand, z.AufheizBemessung, z.AufheizLeistungsquelle, z.AufheizzeitMaxH,
                    z.Aufheiztage, z.AufheiztageBegrenzt, z.AufheiztageUnerreichbar, z.AufheiztageNachweisband, z.AufheizstundenH,
                    z.AufheizzeitLaengsteH, z.AufheizspruengeAus)).Append('\n');
                foreach (ErgebnisZoneModel zz in z.Zonen)
                    sb.Append("Zone ").Append(Ganz(zz.AufheizZustand, null, zz.AufheizLeistungsquelle, zz.AufheizzeitMaxH, zz.Aufheiztage,
                        zz.AufheiztageBegrenzt, zz.AufheiztageUnerreichbar, zz.AufheiztageNachweisband, zz.AufheizstundenH,
                        zz.AufheizzeitLaengsteH, zz.AufheizspruengeAus)).Append('\n');
            }
            foreach (KeyValuePair<string, string> t in satz.Texte) sb.Append(t.Key).Append('=').Append(t.Value).Append('\n');
            foreach (KeyValuePair<string, double> k in satz.Skalare)
                sb.Append(k.Key).Append(mitZahlen ? "=" + Bits(k.Value) : "").Append('\n');
            foreach (KeyValuePair<string, double[]> r in satz.Reihen)
            {
                sb.Append(r.Key);
                if (mitZahlen)
                {
                    var bytes = new byte[8 * r.Value.Length];
                    for (int h = 0; h < r.Value.Length; h++)
                        BitConverter.GetBytes(BitConverter.DoubleToInt64Bits(r.Value[h])).CopyTo(bytes, 8 * h);
                    sb.Append('=').Append(Convert.ToHexString(SHA256.HashData(bytes)));
                }
                sb.Append('\n');
            }
            return sb.ToString();
        }

        private static string Bits(double x) => BitConverter.DoubleToInt64Bits(x).ToString("X16", CultureInfo.InvariantCulture);

        private static string Bits(double? x) => x.HasValue ? Bits(x.Value) : "NULL";

        private static string Ganz(string zustand, string bemessung, string quelle, params int?[] zahlen)
            => (zustand ?? "NULL") + "|" + (bemessung ?? "NULL") + "|" + (quelle ?? "NULL") + "|" + string.Join("|", zahlen.Select(Text));

        /// <summary>
        /// <b>Der Einstieg der gestörten Kopie</b> — per Reflexion in der zweiten Ladekopie gerufen: die
        /// Testdatenbank des Prozesses, ein Lauf mit Schalter an, Abdruck und Hash. Liefert zuerst, ob die
        /// Störung der Naht in dieser Kopie wirkt.
        /// </summary>
        internal static string[] GestoertLaufen(string dbPfad, int projekt, int gebaeude, bool zonen)
        {
            DataRepository.PfadUeberschreibung = dbPfad;
            AufheizLauf.Gebaeudelauf l = Lauf(projekt, gebaeude, zonen);
            return new[] { Plattformrundung.UlpStoerung ? "gestört" : "ungestört", Plaene(l), Reihenhash(l), Zeile(l, false) };
        }

        /// <summary>
        /// Ein Lauf mit Schalter an: mit der Aufheizvorgabe des Projekts, wenn sie an ist (das Referenzprojekt 1051
        /// mit der Bemessung (b), RP1b), sonst mit den Vorgaben.
        /// </summary>
        internal static AufheizLauf.Gebaeudelauf Lauf(int projekt, int gebaeude, bool zonen)
        {
            Aufheizvorgabe v = KonfigurationCtrl.AufheizvorgabeLesen(projekt);
            if (!v.An) v = new Aufheizvorgabe(true, null, null, null, null);
            return AufheizLauf.Projekt(projekt, v, double.NaN,
                                       g => g.ID_Gebaeude == gebaeude, zonen ? AufheizLauf.Mehrzonenfassung : null).Single();
        }
    }

    /// <summary>
    /// <b>N-AH7 Determinismus, Lauf</b> (Entwurf KP3 Abschnitt 7, Welle R4; Grundsatz 6, B13) an Gebäuden der
    /// Testdatenbank mit Schalter an — Projekt 1018 (Gebäude 10632, Rampe an rund 20 Tagen) einzonig und in der
    /// Mehrzonenfassung, Projekt 1008 (Gebäude 10576) und das Referenzprojekt 1051 (Gebäude 10657, mit seiner
    /// Aufheizvorgabe: Bemessung (b) 2 K, Kalender aller fünf Größen; Plattformprobe RP1b): zwei Läufe bitgleich; de-DE gegen en-US
    /// (<see cref="Kulturvorrichtung"/>) bitgleich; die ulp-Störung der Naht <see cref="Plattformrundung"/>
    /// kippt kein n. Ergebniszeile und Export (Welle D2): zwei Läufe und beide Kulturen bitgleich, im gestörten Lauf
    /// dieselben Zustände, Zähler und Schlüssel.
    ///
    /// <para><b>Die Störung im Test.</b> <see cref="Plattformrundung.UlpStoerung"/> wird einmal je Prozess
    /// gelesen und darf in den Tests nie wirken (<c>PlattformrundungTests</c>). Deshalb rechnet der gestörte
    /// Lauf in einer <b>zweiten Ladekopie</b> von <c>EPOS.Kern</c> und dieser Testassembly
    /// (<see cref="AssemblyLoadContext"/>): Die Naht des Prozesses ist vorher gelesen (ungestört), der Schalter
    /// steht nur, solange die Kopie ihre Naht zum ersten Mal liest, und ist danach wieder zurückgesetzt. Die
    /// Kopie liest dieselbe Arbeitskopie der Testdatenbank und gibt nur Zeichenketten zurück.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AufheizDeterminismusTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;
        private readonly ITestOutputHelper _aus;

        public AufheizDeterminismusTests(TestDatenbank db, ITestOutputHelper aus)
        {
            _db = db;
            _aus = aus;
        }

        public static IEnumerable<object[]> Gebaeude() => new[]
        {
            new object[] { 1018, 10632, false },
            new object[] { 1018, 10632, true },
            new object[] { 1008, 10576, false },
            new object[] { Konditionierungsprojekt1051.NEU, KonditionierungReferenzprojektWacheTests.GEBAEUDE, false },
        };

        private static void Gleich(AufheizLauf.Gebaeudelauf a, AufheizLauf.Gebaeudelauf b, string wo)
        {
            Assert.True(a.Gerechnet && b.Gerechnet, wo);
            AufheizGrenzfallTests.Bitgleich(a.Ziel, b.Ziel, wo + ", Heizreihe");
            AufheizGrenzfallTests.Bitgleich(a.Ergebnis, b.Ergebnis, wo);
            AufheizGrenzfallTests.Bitgleich(a.Ergebnis.HeizleistungMaxAnteil, b.Ergebnis.HeizleistungMaxAnteil, wo + ", Kappung");
            Aufheizergebnis x = a.Ergebnis.Aufheizung, y = b.Ergebnis.Aufheizung;
            Assert.Equal(x with { Rampenmaske = null, Nachweisbandtage = null }, y with { Rampenmaske = null, Nachweisbandtage = null });
            Assert.Equal(x.Rampenmaske, y.Rampenmaske);
            Assert.Equal(x.Nachweisbandtage, y.Nachweisbandtage);
            Assert.Equal(Aufheizabdruck.Plaene(a), Aufheizabdruck.Plaene(b));
            if (a.Mehrzonen != null)
                for (int z = 0; z < a.Mehrzonen.Zonen.Count; z++)
                {
                    AufheizGrenzfallTests.Bitgleich(a.Mehrzonen.Zonen[z], b.Mehrzonen.Zonen[z], wo + ", Zone " + z);
                    AufheizGrenzfallTests.Bitgleich(a.Mehrzonen.Eingaenge[z].Aufheizplan.Reihe, b.Mehrzonen.Eingaenge[z].Aufheizplan.Reihe,
                                                    wo + ", Reihe Zone " + z);
                }
            else
                AufheizGrenzfallTests.Bitgleich(a.Plan.Reihe, b.Plan.Reihe, wo + ", Reihe");
        }

        /// <summary>Zwei Läufe und beide Kulturen sind bitgleich — Reihen, Kennzahlen, Pläne, Aufheizwerte.</summary>
        [Theory]
        [MemberData(nameof(Gebaeude))]
        public void N_AH7_zwei_Laeufe_und_beide_Kulturen_sind_bitgleich(int projekt, int gebaeude, bool zonen)
        {
            if (!_db.Vorhanden) return;
            string wo = string.Format(CultureInfo.InvariantCulture, "Projekt {0}, Gebäude {1}{2}", projekt, gebaeude, zonen ? " (Zonen)" : "");
            AufheizLauf.Gebaeudelauf eins = Aufheizabdruck.Lauf(projekt, gebaeude, zonen);
            AufheizLauf.Gebaeudelauf zwei = Aufheizabdruck.Lauf(projekt, gebaeude, zonen);
            AufheizLauf.Gebaeudelauf de, en;
            using (new Kulturvorrichtung("de-DE")) de = Aufheizabdruck.Lauf(projekt, gebaeude, zonen);
            using (new Kulturvorrichtung("en-US")) en = Aufheizabdruck.Lauf(projekt, gebaeude, zonen);
            Assert.True(eins.Ergebnis.Aufheizung.Aufheiztage > 0, wo + ": keine Rampe");
            Assert.Equal(zonen, eins.Mehrzonen != null);
            Gleich(eins, zwei, wo + ", zweiter Lauf");
            Gleich(de, en, wo + ", de-DE gegen en-US");
            Gleich(eins, de, wo + ", de-DE");
            _aus.WriteLine(wo + "\n" + Aufheizabdruck.Plaene(eins).Split('\n').Last());
        }

        /// <summary>
        /// <b>N-AH7 Ergebniszeile und Export</b> (Welle D2): Die Zeile aus <see cref="GebaeudeKennzahlen.Bilden"/> —
        /// Gebäude und Zonen, alle Aufheizwerte — und der Exportsatz (Texte, Schlüssel mit Werten, Reihen samt
        /// <c>heizsollwert_&lt;n&gt;.csv</c>) sind über zwei Läufe und de-DE gegen en-US Bit für Bit gleich.
        /// </summary>
        [Theory]
        [MemberData(nameof(Gebaeude))]
        public void N_AH7_Ergebniszeile_und_Export_sind_bitgleich(int projekt, int gebaeude, bool zonen)
        {
            if (!_db.Vorhanden) return;
            string eins = Aufheizabdruck.Zeile(Aufheizabdruck.Lauf(projekt, gebaeude, zonen), true);
            string zwei = Aufheizabdruck.Zeile(Aufheizabdruck.Lauf(projekt, gebaeude, zonen), true);
            string de, en;
            using (new Kulturvorrichtung("de-DE")) de = Aufheizabdruck.Zeile(Aufheizabdruck.Lauf(projekt, gebaeude, zonen), true);
            using (new Kulturvorrichtung("en-US")) en = Aufheizabdruck.Zeile(Aufheizabdruck.Lauf(projekt, gebaeude, zonen), true);
            Assert.Equal(eins, zwei);
            Assert.Equal(de, en);
            Assert.Equal(eins, de);
            Assert.Contains("Aufheizzustand=", eins, StringComparison.Ordinal);
            Assert.Contains("heizsollwert_", eins, StringComparison.Ordinal);
            Assert.Contains("AufheizLeistungKw=", eins, StringComparison.Ordinal);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture, "N-AH7 Zeile/Export: Projekt {0}, Gebäude {1}{2}: {3} Zeilen bitgleich",
                projekt, gebaeude, zonen ? " (Zonen)" : "", eins.Split('\n').Length - 1));
        }

        // =====================================================================
        //  ulp-Störung der Naht (B13)
        // =====================================================================

        /// <summary>Die zweite Ladekopie: jede Assembly des Testordners noch einmal, alles Übrige geteilt.</summary>
        private sealed class Ladekopie : AssemblyLoadContext
        {
            private readonly string _ordner;

            internal Ladekopie(string ordner) : base("aufheiz-ulp", isCollectible: true)
            {
                _ordner = ordner;
            }

            protected override Assembly Load(AssemblyName name)
            {
                string pfad = Path.Combine(_ordner, name.Name + ".dll");
                return File.Exists(pfad) ? LoadFromAssemblyPath(pfad) : null;
            }
        }

        private static string[] Gestoert(string dbPfad, int projekt, int gebaeude, bool zonen)
        {
            // Die Naht des Prozesses ist gelesen, bevor der Schalter steht: Sie bleibt ungestört.
            Assert.False(Plattformrundung.UlpStoerung, "In den Tests darf die Störung nie eingeschaltet sein.");
            string ordner = Path.GetDirectoryName(typeof(AufheizDeterminismusTests).Assembly.Location);
            var kopie = new Ladekopie(ordner);
            try
            {
                Assembly tests = kopie.LoadFromAssemblyPath(Path.Combine(ordner, "EPOS.Kern.Tests.dll"));
                Assert.NotSame(typeof(AufheizDeterminismusTests).Assembly, tests);
                MethodInfo m = tests.GetType("EPOS.Kern.Tests.Aufheizabdruck", true)
                                    .GetMethod(nameof(Aufheizabdruck.GestoertLaufen), BindingFlags.Static | BindingFlags.NonPublic);
                AppContext.SetSwitch(Plattformrundung.SCHALTER_ULP, true);
                try
                {
                    return (string[])m.Invoke(null, new object[] { dbPfad, projekt, gebaeude, zonen });
                }
                finally
                {
                    AppContext.SetSwitch(Plattformrundung.SCHALTER_ULP, false);
                }
            }
            finally
            {
                kopie.Unload();
            }
        }

        /// <summary>
        /// <b>Die ulp-Störung kippt kein n</b> (Grundsatz 6, B13): Der gestörte Lauf (±1 ulp an jedem
        /// sechzehnten Ergebnis von <c>Exp</c>, <c>Sin</c>, <c>Cos</c>, <c>Asin</c>, <c>Acos</c>) hat je Sprung
        /// dasselbe n, dieselbe Bemessung und dieselben Zähler; dass die Störung wirkt, zeigt die Heizreihe, die
        /// sich in den letzten Bits unterscheidet. Die Prozessnaht bleibt danach ungestört.
        /// </summary>
        [Theory]
        [MemberData(nameof(Gebaeude))]
        public void N_AH7_ulp_Stoerung_kippt_kein_n(int projekt, int gebaeude, bool zonen)
        {
            if (!_db.Vorhanden) return;
            AufheizLauf.Gebaeudelauf normal = Aufheizabdruck.Lauf(projekt, gebaeude, zonen);
            string dbPfad = DataRepository.PfadUeberschreibung;
            Assert.False(string.IsNullOrEmpty(dbPfad));

            string[] g = Gestoert(dbPfad, projekt, gebaeude, zonen);
            Assert.Equal("gestört", g[0]);
            Assert.Equal(Aufheizabdruck.Plaene(normal), g[1]);
            // Welle D2: Ergebniszeile und Export tragen dieselben Zustände, Zähler und Schlüssel.
            Assert.Equal(Aufheizabdruck.Zeile(normal, false), g[3]);
            Assert.NotEqual(Aufheizabdruck.Reihenhash(normal), g[2]);
            Assert.False(Plattformrundung.UlpStoerung);
            Assert.False(AppContext.TryGetSwitch(Plattformrundung.SCHALTER_ULP, out bool an) && an);

            List<Aufheizsprung> alle = (normal.Mehrzonen != null ? normal.Mehrzonen.Eingaenge.Select(z => z.Aufheizplan) : new[] { normal.Plan })
                .SelectMany(p => p.Spruenge).ToList();
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "N-AH7 ulp: Projekt {0}, Gebäude {1}{2}: {3} Sprünge mit gleichem n (n: {4}), Heizreihe gestört (Hash verschieden); {5}",
                projekt, gebaeude, zonen ? " (Zonen)" : "", alle.Count,
                string.Join(", ", alle.GroupBy(sp => sp.N).OrderBy(x => x.Key).Select(x => x.Key + "×" + x.Count())),
                Aufheizabdruck.Plaene(normal).Split('\n').Last()));
        }
    }
}
