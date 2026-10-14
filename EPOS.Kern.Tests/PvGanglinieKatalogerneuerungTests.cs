using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Projektkopie und Katalog der PV-Ganglinie</b> (E113): <see cref="PvGanglinieStammCtrl.AbweichungZumKatalog"/>
    /// erkennt, worin die Projektkopie vom Katalogsatz gleichen Namens abweicht (Nennleistung, Raster, Jahressumme,
    /// Reihe), und <see cref="PvGanglinieStammCtrl.AusKatalogErneuern"/> ersetzt Kopf und Reihe der Kopie — Zuordnung
    /// und ID der Kopie bleiben, das Projekt gilt als geändert.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class PvGanglinieKatalogerneuerungTests : IDisposable
    {
        private const int PROJEKT = 1030;
        private const string NAME = "PV Erneuerung";

        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _ordner =
            Path.Combine(Path.GetTempPath(), "epos-pvge-" + Guid.NewGuid().ToString("N").Substring(0, 8));

        public PvGanglinieKatalogerneuerungTests() => Directory.CreateDirectory(_ordner);

        public void Dispose()
        {
            try { Directory.Delete(_ordner, true); } catch { }
            _kultur.Dispose();
        }

        /// <summary>Eine Stundenreihe mit Tagesbogen bis <paramref name="spitzeKw"/>.</summary>
        private string Datei(string datei, double spitzeKw)
        {
            var sb = new StringBuilder();
            sb.Append("P_AC [kW]").Append('\n');
            for (int i = 0; i < 8760; i++)
            {
                int stunde = i % 24;
                double w = stunde >= 6 && stunde < 18 ? Math.Round(Math.Sin((stunde - 6 + 0.5) / 12.0 * Math.PI) * spitzeKw, 3) : 0.0;
                sb.Append(w.ToString("0.000", CultureInfo.InvariantCulture)).Append('\n');
            }
            string pfad = Path.Combine(_ordner, datei);
            File.WriteAllText(pfad, sb.ToString(), new UTF8Encoding(false));
            return pfad;
        }

        /// <summary>Legt den Katalogsatz <see cref="NAME"/> an und nimmt ihn in das Projekt auf.</summary>
        private void Anlegen(double spitzeKw, double? nennKwp)
        {
            PvGanglinieImportBericht b = PvGanglinieImportCtrl.Einlesen(Datei(NAME + ".csv", spitzeKw), nennKwp);
            Assert.True(b.Erfolgreich, b.Meldung);
            Assert.True(PvGanglinieStammCtrl.ZuordnungenSchreiben(PROJEKT, new[] { NAME }));
        }

        private static int KopieId() => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM Tab_PvGanglinie WHERE Bezeichner = ? AND ID_Projekt = ?",
            new DbParam("@b", NAME), new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture);

        private static int ZuordnungId() => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM Z_ProjektPvGanglinie WHERE Bezeichner = ? AND ID_Projekt = ?",
            new DbParam("@b", NAME), new DbParam("@p", PROJEKT)), CultureInfo.InvariantCulture);

        private static List<double> Reihe(string sql, int id)
        {
            DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@g", id));
            return dt.Rows.Cast<DataRow>().Select(r => Convert.ToDouble(r[0], CultureInfo.InvariantCulture)).ToList();
        }

        [Fact]
        public void Eine_frische_Kopie_weicht_nicht_ab_und_das_Erneuern_meldet_gleich()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Anlegen(8.0, 9.0);

            PvKatalogabweichung a = PvGanglinieStammCtrl.AbweichungZumKatalog(PROJEKT, NAME);
            Assert.False(a.KatalogsatzFehlt);
            Assert.False(a.ProjektkopieFehlt);
            Assert.False(a.Weicht);
            Assert.Equal(PvKatalogerneuerung.Gleich, PvGanglinieStammCtrl.AusKatalogErneuern(PROJEKT, NAME));
        }

        [Fact]
        public void Eine_geaenderte_Nennleistung_wird_erkannt_und_erneuert_Zuordnung_und_ID_bleiben()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Anlegen(8.0, 9.0);
            int kopie = KopieId(), zuordnung = ZuordnungId();
            DataRepository.ExecuteNonQuery("UPDATE Tab_Projekt SET Aenderungsdatum = NULL WHERE ID = ?", new DbParam("@p", PROJEKT));

            Assert.Equal(PvNennleistungSchreibergebnis.Geschrieben, PvGanglinieStammCtrl.NennleistungSetzen(NAME, 7.5));
            PvKatalogabweichung a = PvGanglinieStammCtrl.AbweichungZumKatalog(PROJEKT, NAME);
            Assert.True(a.Weicht);
            Assert.True(a.Nennleistung);
            Assert.False(a.Raster);
            Assert.False(a.Jahressumme);
            Assert.False(a.Reihe);

            Assert.Equal(PvKatalogerneuerung.Erneuert, PvGanglinieStammCtrl.AusKatalogErneuern(PROJEKT, NAME));
            Assert.Equal(7.5, Convert.ToDouble(DataRepository.ExecuteScalar(
                "SELECT Nennleistung_kWp FROM Tab_PvGanglinie WHERE ID = ?", new DbParam("@i", kopie)), CultureInfo.InvariantCulture));
            Assert.Equal(kopie, KopieId());
            Assert.Equal(zuordnung, ZuordnungId());
            Assert.Equal(kopie, Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID_Ganglinie FROM Z_ProjektPvGanglinie WHERE ID = ?", new DbParam("@i", zuordnung)), CultureInfo.InvariantCulture));
            Assert.False(PvGanglinieStammCtrl.AbweichungZumKatalog(PROJEKT, NAME).Weicht);

            // Das Projekt gilt als geändert — ein gespeichertes Ergebnis ist veraltet.
            object datum = DataRepository.ExecuteScalar("SELECT Aenderungsdatum FROM Tab_Projekt WHERE ID = ?", new DbParam("@p", PROJEKT));
            Assert.True(datum != null && datum != DBNull.Value);
        }

        [Fact]
        public void Ein_erneuter_Import_desselben_Namens_ersetzt_Kopf_und_Reihe_der_Kopie()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Anlegen(8.0, 9.0);
            int kopie = KopieId();

            // Der Katalogsatz wird gelöscht und mit einer anderen Reihe neu eingelesen; die Kopie bleibt dabei stehen.
            Assert.True(PvGanglinieStammCtrl.Loeschen(NAME));
            PvGanglinieImportBericht neu = PvGanglinieImportCtrl.Einlesen(Datei(NAME + ".csv", 12.0), 13.0);
            Assert.True(neu.Erfolgreich, neu.Meldung);

            PvKatalogabweichung a = PvGanglinieStammCtrl.AbweichungZumKatalog(PROJEKT, NAME);
            Assert.True(a.Weicht);
            Assert.True(a.Nennleistung);
            Assert.True(a.Jahressumme);
            Assert.True(a.Reihe);
            Assert.False(a.Raster);

            Assert.Equal(PvKatalogerneuerung.Erneuert, PvGanglinieStammCtrl.AusKatalogErneuern(PROJEKT, NAME));
            Assert.Equal(kopie, KopieId());
            List<double> stamm = Reihe("SELECT Wert FROM Tab_PvGanglinieDaten_STAMM WHERE ID_Ganglinie = ? ORDER BY ID", neu.IdStamm);
            List<double> projekt = Reihe("SELECT Wert FROM Tab_PvGanglinieDaten WHERE ID_Ganglinie = ? ORDER BY ID", kopie);
            Assert.Equal(8760, projekt.Count);
            Assert.Equal(stamm, projekt);
            Assert.Equal(Convert.ToDouble(DataRepository.ExecuteScalar(
                    "SELECT Jahresarbeit_kWh FROM Tab_PvGanglinie_STAMM WHERE ID = ?", new DbParam("@i", neu.IdStamm)), CultureInfo.InvariantCulture),
                Convert.ToDouble(DataRepository.ExecuteScalar(
                    "SELECT Jahresarbeit_kWh FROM Tab_PvGanglinie WHERE ID = ?", new DbParam("@i", kopie)), CultureInfo.InvariantCulture));
            Assert.False(PvGanglinieStammCtrl.AbweichungZumKatalog(PROJEKT, NAME).Weicht);
        }

        [Fact]
        public void Ohne_Katalogsatz_oder_ohne_Kopie_wird_nichts_geschrieben()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            Anlegen(8.0, 9.0);
            int kopie = KopieId();
            int werte = Reihe("SELECT Wert FROM Tab_PvGanglinieDaten WHERE ID_Ganglinie = ? ORDER BY ID", kopie).Count;

            // Ein Projekt ohne Kopie dieses Namens.
            PvKatalogabweichung ohneKopie = PvGanglinieStammCtrl.AbweichungZumKatalog(1007, NAME);
            Assert.True(ohneKopie.ProjektkopieFehlt);
            Assert.False(ohneKopie.Weicht);
            Assert.Equal(PvKatalogerneuerung.KeineProjektkopie, PvGanglinieStammCtrl.AusKatalogErneuern(1007, NAME));

            // Der Katalogsatz ist weg: Die Kopie bleibt, wie sie ist.
            Assert.True(PvGanglinieStammCtrl.Loeschen(NAME));
            PvKatalogabweichung ohneKatalog = PvGanglinieStammCtrl.AbweichungZumKatalog(PROJEKT, NAME);
            Assert.True(ohneKatalog.KatalogsatzFehlt);
            Assert.False(ohneKatalog.Weicht);
            Assert.Equal(PvKatalogerneuerung.KeinKatalogsatz, PvGanglinieStammCtrl.AusKatalogErneuern(PROJEKT, NAME));
            Assert.Equal(kopie, KopieId());
            Assert.Equal(werte, Reihe("SELECT Wert FROM Tab_PvGanglinieDaten WHERE ID_Ganglinie = ? ORDER BY ID", kopie).Count);
        }
    }
}
