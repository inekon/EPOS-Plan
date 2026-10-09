using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Nennleistung einer PV-Ganglinie beim Import</b>: Vorbelegung aus dem Dateikopf, sonst aus der Spitze
    /// der Reihe; der Prüfhinweis, wenn die Spitze über Nennleistung × 1,1 liegt; der Wert am Katalogsatz
    /// (<c>Tab_PvGanglinie_STAMM.Nennleistung_kWp</c>) und in der Katalogliste.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class PvGanglinieNennleistungTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _ordner =
            Path.Combine(Path.GetTempPath(), "epos-pvgn-" + Guid.NewGuid().ToString("N").Substring(0, 8));

        public PvGanglinieNennleistungTests() => Directory.CreateDirectory(_ordner);

        public void Dispose()
        {
            try { Directory.Delete(_ordner, true); } catch { }
            _kultur.Dispose();
        }

        /// <summary>Eine Stundenreihe mit Tagesbogen bis <paramref name="spitzeKw"/> und dem Kopf <paramref name="kopf"/>.</summary>
        private string Datei(string name, string kopf, double spitzeKw)
        {
            var sb = new StringBuilder();
            sb.Append(kopf).Append('\n');
            for (int i = 0; i < 8760; i++)
            {
                int stunde = i % 24;
                double w = stunde >= 6 && stunde < 18 ? Math.Round(Math.Sin((stunde - 6 + 0.5) / 12.0 * Math.PI) * spitzeKw, 3) : 0.0;
                sb.Append(w.ToString("0.000", CultureInfo.InvariantCulture)).Append('\n');
            }
            string pfad = Path.Combine(_ordner, name);
            File.WriteAllText(pfad, sb.ToString(), new UTF8Encoding(false));
            return pfad;
        }

        [Theory]
        [InlineData("Anlage Süd 9,8 kWp", 9.8)]
        [InlineData("P_AC [kW] Nennleistung 12.5kWp", 12.5)]
        [InlineData("Anlage 30 KWP", 30.0)]
        public void Der_Dateikopf_nennt_die_Nennleistung(string kopf, double erwartet)
            => Assert.Equal(erwartet, PvGanglinieImportCtrl.NennleistungAusKopf(kopf));

        [Theory]
        [InlineData("P_AC [kW]")]
        [InlineData("")]
        [InlineData("0 kWp")]
        public void Ohne_kWp_im_Kopf_gibt_es_keine_Nennleistung(string kopf)
            => Assert.Null(PvGanglinieImportCtrl.NennleistungAusKopf(kopf));

        [Fact]
        public void Der_Hinweis_kommt_erst_ueber_der_Toleranz_von_zehn_Prozent()
        {
            Assert.Equal("", PvGanglinieImportCtrl.Pruefhinweis(11.0, 10.0));
            Assert.Equal("", PvGanglinieImportCtrl.Pruefhinweis(50.0, null));
            string hinweis = PvGanglinieImportCtrl.Pruefhinweis(11.5, 10.0);
            Assert.Contains("11,5", hinweis);
            Assert.Contains("10,00", hinweis);
        }

        [Fact]
        public void Die_Vorbelegung_nimmt_den_Kopf_sonst_die_Spitze()
        {
            PvGanglinieVorschlag kopf = PvGanglinieImportCtrl.Vorschlagen(Datei("Kopf.csv", "Anlage 9,8 kWp", 8.0));
            Assert.True(kopf.AusDateikopf);
            Assert.Equal(9.8, kopf.VorschlagKwp);
            Assert.True(kopf.SpitzeKw > 7.5 && kopf.SpitzeKw <= 8.0, kopf.SpitzeKw.ToString(CultureInfo.InvariantCulture));

            PvGanglinieVorschlag spitze = PvGanglinieImportCtrl.Vorschlagen(Datei("Spitze.csv", "P_AC [kW]", 8.0));
            Assert.False(spitze.AusDateikopf);
            Assert.Equal(Math.Round(spitze.SpitzeKw, 2), spitze.VorschlagKwp);

            Assert.Null(PvGanglinieImportCtrl.Vorschlagen(Path.Combine(_ordner, "fehlt.csv")).VorschlagKwp);
        }

        [Fact]
        public void Der_Import_haelt_die_Nennleistung_am_Satz_und_meldet_eine_zu_kleine()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            PvGanglinieImportBericht gut = PvGanglinieImportCtrl.Einlesen(Datei("PV Nenn gut.csv", "P_AC [kW]", 8.0), 9.0);
            Assert.True(gut.Erfolgreich, gut.Meldung);
            Assert.Equal("", gut.Hinweis);
            Assert.Equal(9.0, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Nennleistung_kWp FROM Tab_PvGanglinie_STAMM WHERE ID = ?", new DbParam("@i", gut.IdStamm)),
                CultureInfo.InvariantCulture));

            PvGanglinieImportBericht klein = PvGanglinieImportCtrl.Einlesen(Datei("PV Nenn klein.csv", "P_AC [kW]", 8.0), 5.0);
            Assert.True(klein.Erfolgreich, klein.Meldung);
            Assert.NotEqual("", klein.Hinweis);
            Assert.EndsWith(klein.Hinweis, klein.Protokoll);

            Katalogfilterzeile zeile = ZeitreihenKatalogCtrl.Katalogfilterzeilen(Zeitreihenart.PvGanglinie)
                .First(z => z.Bezeichner == "PV Nenn gut");
            Assert.Equal(9.0, zeile.Zahl(Katalogfilterprofil.SpNennleistungKwp));
            Assert.Contains(Katalogfilterprofil.FuerZeitreihe(Zeitreihenart.PvGanglinie).Spalten,
                            s => s.Schluessel == Katalogfilterprofil.SpNennleistungKwp);
        }
            [Fact]
        public void Die_Nennleistung_eines_Katalogsatzes_wird_nachtraeglich_gesetzt_die_Projektkopie_bleibt()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            PvGanglinieImportBericht b = PvGanglinieImportCtrl.Einlesen(Datei("PV Nachtrag.csv", "P_AC [kW]", 8.0), 9.0);
            Assert.True(b.Erfolgreich, b.Meldung);
            Assert.True(PvGanglinieStammCtrl.ZuordnungenSchreiben(1030, new[] { "PV Nachtrag" }));

            double? Katalogwert() => Wert("SELECT Nennleistung_kWp FROM Tab_PvGanglinie_STAMM WHERE Bezeichner = ?");
            double? Kopiewert() => Wert("SELECT Nennleistung_kWp FROM Tab_PvGanglinie WHERE Bezeichner = ? AND ID_Projekt = 1030");

            Assert.Equal("", PvGanglinieStammCtrl.Pruefhinweis("PV Nachtrag", 9.0));
            Assert.NotEqual("", PvGanglinieStammCtrl.Pruefhinweis("PV Nachtrag", 5.0));

            Assert.Equal(PvNennleistungSchreibergebnis.Geschrieben, PvGanglinieStammCtrl.NennleistungSetzen("PV Nachtrag", 5.0));
            Assert.Equal(5.0, Katalogwert());
            Assert.Equal(9.0, Kopiewert());
            Assert.Equal(5.0, ZeitreihenKatalogCtrl.Katalogfilterzeilen(Zeitreihenart.PvGanglinie)
                .First(z => z.Bezeichner == "PV Nachtrag").Zahl(Katalogfilterprofil.SpNennleistungKwp));

            Assert.Equal(PvNennleistungSchreibergebnis.Geschrieben, PvGanglinieStammCtrl.NennleistungSetzen("PV Nachtrag", null));
            Assert.Null(Katalogwert());

            Assert.Equal(PvNennleistungSchreibergebnis.Ungueltig, PvGanglinieStammCtrl.NennleistungSetzen("PV Nachtrag", 0.0));
            Assert.Equal(PvNennleistungSchreibergebnis.Ungueltig, PvGanglinieStammCtrl.NennleistungSetzen("PV Nachtrag", double.NaN));
            Assert.Equal(PvNennleistungSchreibergebnis.Unbekannt, PvGanglinieStammCtrl.NennleistungSetzen("PV gibt es nicht", 5.0));

            DataRepository.ExecuteNonQuery("UPDATE Tab_PvGanglinie_STAMM SET ReadOnly = 1 WHERE Bezeichner = ?",
                                           new DbParam("@b", "PV Nachtrag"));
            Assert.Equal(PvNennleistungSchreibergebnis.Schreibgeschuetzt, PvGanglinieStammCtrl.NennleistungSetzen("PV Nachtrag", 7.0));
            Assert.Null(Katalogwert());
        }

        private static double? Wert(string sql)
        {
            object v = DataRepository.ExecuteScalar(sql, new DbParam("@b", "PV Nachtrag"));
            return v == null || v == DBNull.Value ? (double?)null : Convert.ToDouble(v, CultureInfo.InvariantCulture);
        }
    }
}
