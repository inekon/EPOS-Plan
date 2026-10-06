using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Konstanten der BDEW-Standardlastprofile Strom 2025</b> (<see cref="StandardlastprofilSaattabelle"/>,
    /// erzeugt von <c>Werkzeuge/Standardlastprofile/ableiten.py</c>) — ohne Datenbank.
    ///
    /// <para><b>Geprüft wird:</b> die drei Profile H25, G25, L25 mit festen Namen; je Profil 168 Wochenwerte
    /// größer null und zwölf Monatswerte mit der Summe 1.000 MWh; Montag bis Freitag gleich, Samstag und
    /// Sonntag mit eigener Form; die Beschreibungen; Quelle und Prüfsumme der Excel im Repositorium. Dass die
    /// Zahlen dem Stand der Excel entsprechen, hält das Werkzeug im Prüfmodus (ohne Argument) — hier wird
    /// nicht nachgerechnet.</para>
    /// </summary>
    public class StandardlastprofilSaatTests
    {
        private static IReadOnlyList<StandardlastprofilSaat> Saat => StandardlastprofilSaattabelle.Alle;

        /// <summary>Drei Profile in der Folge H25, G25, L25 mit den festgelegten Namen, alle verschieden.</summary>
        [Fact]
        public void Drei_Profile_H25_G25_L25_mit_festen_Namen()
        {
            Assert.Equal(new[] { "H25", "G25", "L25" }, Saat.Select(s => s.Kuerzel));
            Assert.Equal(new[] { "BDEW_H25_Haushalt", "BDEW_G25_Gewerbe", "BDEW_L25_Landwirtschaft" }, Saat.Select(s => s.Bezeichner));
            Assert.Equal(new[] { "BDEW_H25", "BDEW_G25", "BDEW_L25" }, Saat.Select(s => s.Typname));
            foreach (StandardlastprofilSaat s in Saat)
            {
                Assert.Equal("BDEW_" + s.Kuerzel, s.Typname);
                Assert.StartsWith(s.Typname + "_", s.Bezeichner, StringComparison.Ordinal);
            }
            Assert.Equal(6, Saat.SelectMany(s => new[] { s.Bezeichner, s.Typname }).Distinct(StringComparer.OrdinalIgnoreCase).Count());
        }

        /// <summary>Je Profil 168 Wochenwerte und zwölf Monatswerte, alle endlich und größer null.</summary>
        [Fact]
        public void Je_Profil_168_Wochenwerte_und_zwoelf_Monatswerte_groesser_null()
        {
            foreach (StandardlastprofilSaat s in Saat)
            {
                Assert.Equal(StandardlastprofilSchema.WOCHENSTUNDEN, s.Wochenwerte.Count);
                Assert.Equal(StandardlastprofilSchema.MONATE, s.Monatswerte.Count);
                Assert.All(s.Wochenwerte, w => Assert.True(double.IsFinite(w) && w > 0, s.Kuerzel + ": Wochenwert " + w));
                Assert.All(s.Monatswerte, m => Assert.True(double.IsFinite(m) && m > 0, s.Kuerzel + ": Monatswert " + m));
            }
        }

        /// <summary>
        /// Montag bis Freitag tragen denselben Tag (Werktag), Samstag und Sonntag je eine eigene Form — der
        /// Sonntag ist der Sonn- und Feiertag des BDEW.
        /// </summary>
        [Fact]
        public void Montag_bis_Freitag_gleich_Samstag_und_Sonntag_eigen()
        {
            foreach (StandardlastprofilSaat s in Saat)
            {
                double[] werktag = Tag(s, 0);
                for (int tag = 1; tag < 5; tag++)
                    Assert.Equal(werktag, Tag(s, tag));
                double[] samstag = Tag(s, 5), sonntag = Tag(s, 6);
                Assert.NotEqual(werktag, samstag);
                Assert.NotEqual(werktag, sonntag);
                Assert.NotEqual(samstag, sonntag);
            }
        }

        /// <summary>Die zwölf Monatswerte summieren auf 1.000 MWh (BDEW: 1 Mio. kWh Jahresverbrauch).</summary>
        [Fact]
        public void Die_Monatswerte_summieren_auf_1000_MWh()
        {
            Assert.Equal(1000.0, StandardlastprofilSaattabelle.JAHRESSUMME_MWH);
            foreach (StandardlastprofilSaat s in Saat)
                Assert.True(Math.Abs(s.Monatswerte.Sum() - 1000.0) <= 0.01, s.Kuerzel + ": Jahressumme " + s.Monatswerte.Sum());
        }

        /// <summary>
        /// Die Form der Profile in groben Zügen: Der Haushalt verbraucht am Sonntag mehr als am Werktag, das
        /// Gewerbe am Werktag rund doppelt so viel wie am Sonntag, die Landwirtschaft an allen Tagen ähnlich;
        /// Januar und Dezember liegen über dem Juni.
        /// </summary>
        [Fact]
        public void Die_Typtage_und_Monate_tragen_die_Form_des_Profils()
        {
            StandardlastprofilSaat h = Saat[0], g = Saat[1], l = Saat[2];
            Assert.True(Tag(h, 6).Sum() > Tag(h, 0).Sum(), "H25: Sonntag nicht über dem Werktag");
            double gewerbe = Tag(g, 0).Sum() / Tag(g, 6).Sum();
            Assert.True(gewerbe > 1.9 && gewerbe < 2.3, "G25: Werktag/Sonntag " + gewerbe);
            double land = Tag(l, 0).Sum() / Tag(l, 6).Sum();
            Assert.True(land > 0.9 && land < 1.1, "L25: Werktag/Sonntag " + land);
            foreach (StandardlastprofilSaat s in Saat)
            {
                Assert.True(s.Monatswerte[0] > s.Monatswerte[5], s.Kuerzel + ": Januar nicht über Juni");
                Assert.True(s.Monatswerte[11] > s.Monatswerte[5], s.Kuerzel + ": Dezember nicht über Juni");
            }
        }

        /// <summary>
        /// Die Beschreibungen nennen Profil, Normierung, Skalierung im Projekt und den Betriebskalender; das
        /// Typprofil nennt seine Typtage und seine Skala.
        /// </summary>
        [Fact]
        public void Die_Beschreibungen_nennen_Normierung_Skalierung_und_Kalender()
        {
            foreach (StandardlastprofilSaat s in Saat)
            {
                Assert.StartsWith("BDEW-Standardlastprofil 2025 " + s.Kuerzel + " (", s.Beschreibung, StringComparison.Ordinal);
                Assert.Contains("normiert auf 1.000 MWh/a", s.Beschreibung, StringComparison.Ordinal);
                Assert.Contains("im Projekt auf den Jahresverbrauch skalieren", s.Beschreibung, StringComparison.Ordinal);
                Assert.Contains("Feiertage über den Betriebskalender", s.Beschreibung, StringComparison.Ordinal);
                Assert.StartsWith("BDEW-Standardlastprofil 2025 " + s.Kuerzel + " (", s.Typbeschreibung, StringComparison.Ordinal);
                Assert.Contains("Mo–Fr Werktag, Sa Samstag, So Sonn- und Feiertag", s.Typbeschreibung, StringComparison.Ordinal);
                Assert.Contains("kWh je Stunde bei 1.000 MWh/a", s.Typbeschreibung, StringComparison.Ordinal);
            }
        }

        /// <summary>
        /// Die Konstanten nennen ihre Quelle, und die Excel im Repositorium ist dieselbe, aus der sie abgeleitet
        /// sind (SHA-256) — ändert sich die Excel, schreibt das Werkzeug die Datei neu.
        /// </summary>
        [Fact]
        public void Quelle_und_Pruefsumme_der_Excel()
        {
            Assert.Contains("BDEW", StandardlastprofilSaattabelle.QUELLE, StringComparison.Ordinal);
            Assert.Contains("17.03.2025", StandardlastprofilSaattabelle.QUELLE, StringComparison.Ordinal);
            Assert.Matches("^[0-9a-f]{64}$", StandardlastprofilSaattabelle.QUELLDATEI_SHA256);

            string wurzel = Repowurzel();
            if (wurzel == null) return;
            string excel = Path.Combine(wurzel, StandardlastprofilSaattabelle.QUELLDATEI.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(excel), "Die Excel fehlt: " + StandardlastprofilSaattabelle.QUELLDATEI);
            string sha = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(excel))).ToLowerInvariant();
            Assert.Equal(StandardlastprofilSaattabelle.QUELLDATEI_SHA256, sha);
            Assert.True(File.Exists(Path.Combine(wurzel, "Werkzeuge", "Standardlastprofile", "ableiten.py")));
        }

        // -----------------------------------------------------------------------------

        private static double[] Tag(StandardlastprofilSaat s, int wochentag)
            => s.Wochenwerte.Skip(24 * wochentag).Take(24).ToArray();

        /// <summary>Die Wurzel des Repositoriums, aufwärts gesucht; sonst <c>null</c>.</summary>
        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
