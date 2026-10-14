using System;
using System.Collections.Generic;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Mehrfach-Bearbeiten der Wärmepumpen in EINER Transaktion</b> (Konzept Projektdialoge mit Katalogauswahl 4.6,
    /// Entscheid KA‑E‑8, Stufe 3): <see cref="WPStammCtrl.SammelfelderSchreibenAlle"/> schreibt Hersteller, Beschreibung
    /// und Modulkosten aller Sätze — Projektkopien oder Katalogsätze — oder keinen; Name, Kennlinien und übrige Spalten
    /// bleiben. Geschrieben wird in der Kopie der Testdatenbank (<see cref="TestDatenbank"/>), nie an den Geräten der
    /// Referenzprojekte.
    /// </summary>
    [Collection("Testdatenbank")]
    public class WaermepumpeMehrfachbearbeitungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        public void Dispose() => _db.Dispose();

        /// <summary>Projektkopien, die KEINEM Referenzprojekt gehören (die Einfrierregeln halten deren Geräte).</summary>
        private static List<int> Kopien(int anzahl)
        {
            var ids = new List<int>();
            var dt = DataRepository.GetDataTable(
                "SELECT ID FROM Tab_WP WHERE ID_Projekt NOT IN (1007,1017,1029,1030,1045,1046,1047,1049,1051,1052,1054," +
                "1055,1056,1057,1058,1059,1060,1061,1062,1063) ORDER BY ID LIMIT " + anzahl.ToString(CultureInfo.InvariantCulture));
            foreach (System.Data.DataRow r in dt.Rows) ids.Add(Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture));
            return ids;
        }

        private static int Katalogsatz(bool gesperrt)
            => Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_WP_STAMM WHERE ReadOnly = ? ORDER BY ID LIMIT 1", new DbParam("@r", gesperrt ? 1 : 0)),
                CultureInfo.InvariantCulture);

        private static object Roh(string tabelle, int id, string spalte)
            => DataRepository.ExecuteScalar("SELECT [" + spalte + "] FROM [" + tabelle + "] WHERE ID = ?", new DbParam("?", id));

        private static long Kennlinienzeilen(string tabelle, int id)
            => Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM [" + tabelle + "] WHERE ID_WP = ?",
                                                            new DbParam("?", id)), CultureInfo.InvariantCulture);

        [Fact]
        public void SammelsatzLesen_liest_Kopie_und_Katalog_nach_ID()
        {
            if (!_db.Vorhanden) return;
            int p = Kopien(1)[0];
            int k = Katalogsatz(false);
            Assert.Equal(Convert.ToString(Roh("Tab_WP", p, "Bezeichner"), CultureInfo.InvariantCulture),
                         WPStammCtrl.SammelsatzLesen(true, p).Bezeichner);
            Assert.Equal(Convert.ToString(Roh("Tab_WP_STAMM", k, "Bezeichner"), CultureInfo.InvariantCulture),
                         WPStammCtrl.SammelsatzLesen(false, k).Bezeichner);
            Assert.False(WPStammCtrl.SammelsatzLesen(true, p).Gesperrt);
            Assert.Null(WPStammCtrl.SammelsatzLesen(true, 987654));
        }

        [Fact]
        public void Alle_Projektkopien_werden_geschrieben_Name_Kennlinien_und_Katalog_bleiben()
        {
            if (!_db.Vorhanden) return;
            var ids = Kopien(2);
            Assert.Equal(2, ids.Count);
            int katalog = Katalogsatz(false);
            object katalogVorher = Roh("Tab_WP_STAMM", katalog, "Firma");
            string name0 = Convert.ToString(Roh("Tab_WP", ids[0], "Bezeichner"), CultureInfo.InvariantCulture);
            long kennlinien0 = Kennlinienzeilen("Tab_Kenndaten", ids[0]);
            object leistung0 = Roh("Tab_WP", ids[0], "Nennleistung");

            var e = WPStammCtrl.SammelfelderSchreibenAlle(true, new[]
            {
                new WPStammCtrl.Sammelaenderung(ids[0], new WPStammCtrl.Sammelfelder("Neuwerk", "Text A", 12000)),
                new WPStammCtrl.Sammelaenderung(ids[1], new WPStammCtrl.Sammelfelder("Neuwerk", "Text B", null)),
            });

            Assert.True(e.Ok, e.Meldung);
            Assert.Equal("Neuwerk", Roh("Tab_WP", ids[0], "Firma"));
            Assert.Equal("Text B", Roh("Tab_WP", ids[1], "Beschreibung"));
            Assert.Equal(12000.0, Convert.ToDouble(Roh("Tab_WP", ids[0], "Modulkosten"), CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, Roh("Tab_WP", ids[1], "Modulkosten") ?? DBNull.Value);
            Assert.Equal(name0, Roh("Tab_WP", ids[0], "Bezeichner"));
            Assert.Equal(kennlinien0, Kennlinienzeilen("Tab_Kenndaten", ids[0]));
            Assert.Equal(leistung0, Roh("Tab_WP", ids[0], "Nennleistung"));
            Assert.Equal(katalogVorher, Roh("Tab_WP_STAMM", katalog, "Firma"));
        }

        [Fact]
        public void Ein_gesperrter_Katalogsatz_rollt_alles_zurueck_und_wird_genannt()
        {
            if (!_db.Vorhanden) return;
            int frei = Katalogsatz(false);
            int gesperrt = Katalogsatz(true);
            object freiVorher = Roh("Tab_WP_STAMM", frei, "Firma");

            var e = WPStammCtrl.SammelfelderSchreibenAlle(false, new[]
            {
                new WPStammCtrl.Sammelaenderung(frei, new WPStammCtrl.Sammelfelder("Neuwerk", "", 1)),
                new WPStammCtrl.Sammelaenderung(gesperrt, new WPStammCtrl.Sammelfelder("Neuwerk", "", 1)),
            });

            Assert.False(e.Ok);
            Assert.Equal(Convert.ToString(Roh("Tab_WP_STAMM", gesperrt, "Bezeichner"), CultureInfo.InvariantCulture), e.Name);
            Assert.Equal(freiVorher, Roh("Tab_WP_STAMM", frei, "Firma"));
        }

        [Fact]
        public void Negative_Modulkosten_und_fehlende_ID_schreiben_nichts()
        {
            if (!_db.Vorhanden) return;
            var ids = Kopien(1);
            object vorher = Roh("Tab_WP", ids[0], "Firma");

            var negativ = WPStammCtrl.SammelfelderSchreibenAlle(true, new[]
            {
                new WPStammCtrl.Sammelaenderung(ids[0], new WPStammCtrl.Sammelfelder("Neuwerk", "", -5)),
            });
            var fehlt = WPStammCtrl.SammelfelderSchreibenAlle(true, new[]
            {
                new WPStammCtrl.Sammelaenderung(ids[0], new WPStammCtrl.Sammelfelder("Neuwerk", "", 5)),
                new WPStammCtrl.Sammelaenderung(987654, new WPStammCtrl.Sammelfelder("Neuwerk", "", 5)),
            });

            Assert.False(negativ.Ok);
            Assert.False(fehlt.Ok);
            Assert.Contains("987654", fehlt.Meldung);
            Assert.Equal(vorher, Roh("Tab_WP", ids[0], "Firma"));
        }

        [Fact]
        public void Keine_Aenderung_ist_kein_Fehler()
        {
            Assert.True(WPStammCtrl.SammelfelderSchreibenAlle(true, Array.Empty<WPStammCtrl.Sammelaenderung>()).Ok);
        }
    }
}
