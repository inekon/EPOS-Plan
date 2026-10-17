using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Referenzlauf;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Hinweis „Geräteart Absorption rechnet elektrisch“</b> (SOK-0; Konzept Sorptionskälte 1.2, Frage SOK-Q10,
    /// E168): Eine Kältemaschine mit Geräteart <c>ABSORPTION</c> rechnet bis zum Bau von SOK wie eine elektrische
    /// Kältemaschine. Die Konfigurationsprüfung meldet das als weichen Befund an der Anlage, der Lauf als Hinweis — ohne
    /// Zahlenwirkung: Referenzprojekt 1063 rechnet mit und ohne Geräteart byte-gleich. Ein Datenbanktest, nicht in der Basis.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineSorptionHinweisTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1063;

        /// <summary>Die Kältemaschinen-Anlage (Typ 13) des Projekts — genau eine.</summary>
        private static int Anlage(int projekt) =>
            Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = ?",
                new DbParam("?", projekt), new DbParam("?", KaeltemaschineAnlageSchema.TYP_KAELTEMASCHINE)), CultureInfo.InvariantCulture);

        /// <summary>Setzt die Geräteart der Projektkopie hinter der Kältemaschinen-Anlage.</summary>
        private static void GeraeteartSetzen(int projekt, string geraeteart)
        {
            int idKm = KaeltemaschineAnlageCtrl.Laden(Anlage(projekt)).IdKaeltemaschine.Value;
            DataRepository.ExecuteNonQuery("UPDATE Tab_Kaeltemaschine SET Geraeteart = ? WHERE ID = ?",
                new DbParam("?", geraeteart), new DbParam("?", idKm));
        }

        private static List<Warnbefund> Befunde(int projekt) => Warnkriterien.PruefeProjekt(projekt)
            .Where(b => b.Kriterium == Warnkriterien.KAELTEMASCHINE_SORPTION_ELEKTRISCH).ToList();

        private static string Ordner(string name)
        {
            string o = Path.Combine(Path.GetTempPath(), "epos-sok0-" + name + "-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(o);
            return o;
        }

        private static void Export(int projekt, string ziel)
        {
            var log = new Protokoll();
            int n = Ergebnisexport.ProjektAusfuehren(projekt, ziel, log);
            Assert.True(n > 0, "Der Lauf von " + projekt + " scheiterte (" + log.Fehler + " Fehler).");
        }

        /// <summary>Die Ergebniszeilen der Kältemaschine des jüngsten Laufs, jede Zahl rundungsfrei als Text.</summary>
        private static List<string> KaelteZeilen(int projekt)
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT k.* FROM Tab_ErgebnisKaeltemaschine k WHERE k.ID_Ergebnis = " +
                "(SELECT MAX(ID) FROM Tab_Ergebnis WHERE ID_Projekt = ?) ORDER BY k.ID_Kaeltemaschine, k.ID",
                new DbParam("?", projekt));
            var zeilen = new List<string>();
            foreach (DataRow r in t.Rows)
                zeilen.Add(string.Join("|", t.Columns.Cast<DataColumn>()
                    .Where(c => c.ColumnName != "ID" && c.ColumnName != "ID_Ergebnis")
                    .Select(c => c.ColumnName + "=" + (r[c] is double d ? d.ToString("R", CultureInfo.InvariantCulture)
                                                       : Convert.ToString(r[c], CultureInfo.InvariantCulture)))));
            return zeilen;
        }

        [Theory]
        [InlineData("ABSORPTION", true)]
        [InlineData(" absorption ", true)]
        [InlineData("KWS_WASSER", false)]
        [InlineData("KWS_LUFT", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void Nur_die_Geraeteart_Absorption_rechnet_elektrisch_mit_Hinweis(string geraeteart, bool erwartet)
            => Assert.Equal(erwartet, Warnkriterien.SorptionAlsElektrisch(geraeteart));

        [Fact]
        public void Der_Text_nennt_die_Maschine_und_die_elektrische_Rechnung()
        {
            string t = Warnkriterien.SorptionAlsElektrischText("KM Probe");
            Assert.Contains("„KM Probe“", t, StringComparison.Ordinal);
            Assert.Contains("Absorption", t, StringComparison.Ordinal);
            Assert.Contains("elektrische Kältemaschine", t, StringComparison.Ordinal);
        }

        [Fact]
        public void Die_Konfigurationspruefung_meldet_Absorption_und_schweigt_bei_elektrischer_Maschine()
        {
            if (!_db.Vorhanden) return;
            Assert.Empty(Befunde(PROJEKT));

            GeraeteartSetzen(PROJEKT, KaelteKatalogfelderSchema.GERAETEART_ABSORPTION);
            Warnbefund b = Assert.Single(Befunde(PROJEKT));
            Assert.False(b.Hart);
            Assert.Equal(Anlage(PROJEKT), b.ID_Anlage);
            Assert.Contains("Absorption", b.Text, StringComparison.Ordinal);

            GeraeteartSetzen(PROJEKT, KaelteKatalogfelderSchema.GERAETEART_KWS_LUFT);
            Assert.Empty(Befunde(PROJEKT));
        }

        [Fact]
        public void Der_Lauf_meldet_einen_Hinweis_und_rechnet_byte_gleich()
        {
            if (!_db.Vorhanden) return;
            string ohne = Ordner("ohne"), mit = Ordner("mit");
            try
            {
                Export(PROJEKT, ohne);
                List<string> zeilenOhne = KaelteZeilen(PROJEKT);
                Assert.NotEmpty(zeilenOhne);

                GeraeteartSetzen(PROJEKT, KaelteKatalogfelderSchema.GERAETEART_ABSORPTION);
                Export(PROJEKT, mit);

                string[] da = Directory.GetFiles(ohne).Select(Path.GetFileName).Where(f => f != "protokoll.txt")
                                       .OrderBy(f => f, StringComparer.Ordinal).ToArray();
                string[] dm = Directory.GetFiles(mit).Select(Path.GetFileName).Where(f => f != "protokoll.txt")
                                       .OrderBy(f => f, StringComparer.Ordinal).ToArray();
                Assert.NotEmpty(da);
                Assert.Equal(da, dm);
                foreach (string f in da)
                    Assert.True(File.ReadAllBytes(Path.Combine(ohne, f)).AsSpan().SequenceEqual(File.ReadAllBytes(Path.Combine(mit, f))),
                                f + " weicht mit Geräteart Absorption ab.");
                Assert.Equal(zeilenOhne, KaelteZeilen(PROJEKT));

                // Der Laufhinweis: genau einmal als Hinweis, nie als Warnung.
                var lauf = new SimulationRunner();
                Assert.True(lauf.SimuliereUndSpeichere(PROJEKT, out string fehler) > 0, "Lauf gescheitert: " + fehler);
                Assert.Single(lauf.Protokoll.Hinweise, h => h.Contains("Geräteart Absorption", StringComparison.Ordinal));
                Assert.DoesNotContain(lauf.Protokoll.Warnungen, w => w.Contains("Geräteart Absorption", StringComparison.Ordinal));
                Assert.Equal(zeilenOhne, KaelteZeilen(PROJEKT));
            }
            finally
            {
                foreach (string o in new[] { ohne, mit })
                    try { Directory.Delete(o, true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
            }
        }

        [Fact]
        public void Ohne_Geraeteart_Absorption_meldet_der_Lauf_keinen_Hinweis()
        {
            if (!_db.Vorhanden) return;
            var lauf = new SimulationRunner();
            Assert.True(lauf.SimuliereUndSpeichere(PROJEKT, out string fehler) > 0, "Lauf gescheitert: " + fehler);
            Assert.DoesNotContain(lauf.Protokoll.Hinweise, h => h.Contains("Geräteart Absorption", StringComparison.Ordinal));
        }
    }
}
