using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Security.Cryptography;
using System.Text;
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
        /// <b>Der Einstieg der gestörten Kopie</b> — per Reflexion in der zweiten Ladekopie gerufen: die
        /// Testdatenbank des Prozesses, ein Lauf mit Schalter an, Abdruck und Hash. Liefert zuerst, ob die
        /// Störung der Naht in dieser Kopie wirkt.
        /// </summary>
        internal static string[] GestoertLaufen(string dbPfad, int projekt, int gebaeude, bool zonen)
        {
            DataRepository.PfadUeberschreibung = dbPfad;
            AufheizLauf.Gebaeudelauf l = Lauf(projekt, gebaeude, zonen);
            return new[] { Plattformrundung.UlpStoerung ? "gestört" : "ungestört", Plaene(l), Reihenhash(l) };
        }

        internal static AufheizLauf.Gebaeudelauf Lauf(int projekt, int gebaeude, bool zonen)
            => AufheizLauf.Projekt(projekt, new Aufheizvorgabe(true, null, null, null, null), double.NaN,
                                   g => g.ID_Gebaeude == gebaeude, zonen ? AufheizLauf.Mehrzonenfassung : null).Single();
    }

    /// <summary>
    /// <b>N-AH7 Determinismus, Lauf</b> (Entwurf KP3 Abschnitt 7, Welle R4; Grundsatz 6, B13) an Gebäuden der
    /// Testdatenbank mit Schalter an — Projekt 1018 (Gebäude 10632, Rampe an rund 20 Tagen) einzonig und in der
    /// Mehrzonenfassung, Projekt 1008 (Gebäude 10576): zwei Läufe bitgleich; de-DE gegen en-US
    /// (<see cref="Kulturvorrichtung"/>) bitgleich; die ulp-Störung der Naht <see cref="Plattformrundung"/>
    /// kippt kein n. Ergebniszeile und Export kommen mit D2.
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
            Assert.NotEqual(Aufheizabdruck.Reihenhash(normal), g[2]);
            Assert.False(Plattformrundung.UlpStoerung);
            Assert.False(AppContext.TryGetSwitch(Plattformrundung.SCHALTER_ULP, out bool an) && an);

            int spruenge = (normal.Mehrzonen != null ? normal.Mehrzonen.Eingaenge.Select(z => z.Aufheizplan) : new[] { normal.Plan })
                .Sum(p => p.Spruenge.Count);
            _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                "N-AH7 ulp: Projekt {0}, Gebäude {1}{2}: {3} Sprünge mit gleichem n, Heizreihe gestört (Hash verschieden)",
                projekt, gebaeude, zonen ? " (Zonen)" : "", spruenge));
        }
    }
}
