using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Nachpflege des Kesselkatalogs aus VDI 3805 Blatt 3</b> (Konzept Kesselkennlinie, Etappe
    /// E1; Anwenderentscheide F2 und F3 vom 29.09.2026; <see cref="KesselkatalogNachpflege"/>).
    ///
    /// <para><b>Geprüft wird:</b> der Vergleichsschlüssel der Bezeichner; die Nachpflege an den
    /// Importproben (η₃₀, η₁₀₀ aus Satz 710.01, kleinste Leistung, Brennwert nur gesetzt), der
    /// Trockenlauf, die Wiederholbarkeit — und dass <c>Tab_Heizkessel</c> (die Projektkopien) Zelle
    /// für Zelle unberührt bleibt. Dazu die Gegenprobe gegen alle Herstellerdateien: Die
    /// Testdatenbank ist nachgepflegt, ein weiterer Lauf ändert nichts (nur mit Git LFS).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KesselkatalogNachpflegeTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly string _ordner;

        public KesselkatalogNachpflegeTests()
        {
            _ordner = Path.Combine(Path.GetTempPath(), "epos-kesselkatalog-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            Directory.CreateDirectory(_ordner);
        }

        public void Dispose()
        {
            _db.Dispose();
            try { Directory.Delete(_ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
        }

        /// <summary>Ein Katalogsatz, der in der ersten Importprobe steht (Brennwert-Kessel, Erdgas, 19,3 kW).</summary>
        private const string KESSEL = "ecoVIT VKK 186/5";

        [Theory]
        [InlineData("Vitocrossal 200 CM2 raumluftabh�ngig", "vitocrossal200cm2raumluftabh?ngig")]
        [InlineData("Vitocrossal 200 CM2 raumluftabhängig", "vitocrossal200cm2raumluftabh?ngig")]
        [InlineData("GB125-18 - Logamatic  MC110", "gb125-18-logamaticmc110")]
        public void Der_Vergleichsschluessel_ueberbrueckt_Ersatzzeichen_und_Leerraum(string name, string schluessel)
        {
            Assert.Equal(schluessel, KesselkatalogNachpflege.Schluessel(name));
        }

        /// <summary>
        /// Ein Katalogsatz ohne Kennlinie und ohne Brennwertkennzeichen: Die Nachpflege aus der
        /// Importprobe trägt η₃₀, η₁₀₀ (Satz 710.01), die kleinste Leistung und Brennwert nach — und
        /// keine Projektkopie ändert sich. Ein zweiter Lauf ändert nichts.
        /// </summary>
        [Fact]
        public void Nachpflege_traegt_die_Kennlinie_in_den_Katalog_und_nie_in_ein_Projekt()
        {
            if (!_db.Vorhanden) return;
            ProbeAblegen("heizkessel_vaillant.vdi");

            int id = HeizkesselStammCtrl.IdZu(KESSEL);
            Assert.True(id > 0);
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Heizkessel_STAMM SET Brennwert = 0, Wirkungsgrad_Teillast30 = NULL, Mindestleistung = NULL, " +
                "Wirkungsgrad_Gas = 0.874 WHERE ID = ?", new DbParam("?", id)));
            List<string> projektVorher = Abzug("Tab_Heizkessel");

            var trocken = new KesselkatalogNachpflege.Ergebnis();
            KesselkatalogNachpflege.Nachpflegen(KesselkatalogNachpflege.Lesen(_ordner, trocken, 25), trocken, trocken: true);
            Assert.Contains(trocken.Aenderungen, a => a.Id == id && a.Spalte == "Brennwert");
            Assert.Equal(0L, Zahl("SELECT Brennwert FROM Tab_Heizkessel_STAMM WHERE ID = ?", id));

            var e = new KesselkatalogNachpflege.Ergebnis();
            KesselkatalogNachpflege.Nachpflegen(KesselkatalogNachpflege.Lesen(_ordner, e, 25), e, trocken: false);
            Assert.Equal(1, e.Dateien);
            Assert.Equal(5, e.Dateisaetze);
            Assert.True(e.Nachgepflegt >= 1);

            DataTable dt = DataRepository.GetDataTable(
                "SELECT Brennwert, Wirkungsgrad_Teillast30, Mindestleistung, Wirkungsgrad_Gas, Kennlinie_Brennwert, " +
                "Anfahrverlust_kWh, Mindestlaufzeit_min FROM Tab_Heizkessel_STAMM WHERE ID = ?", new DbParam("?", id));
            DataRow r = dt.Rows[0];
            Assert.Equal(1L, Convert.ToInt64(r[0], CultureInfo.InvariantCulture));
            Assert.Equal(1.079, Convert.ToDouble(r[1], CultureInfo.InvariantCulture), 9);
            Assert.Equal(6.0, Convert.ToDouble(r[2], CultureInfo.InvariantCulture), 9);
            Assert.Equal(0.96, Convert.ToDouble(r[3], CultureInfo.InvariantCulture), 9);
            Assert.Equal(0L, Convert.ToInt64(r[4], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, r[5]);
            Assert.Equal(DBNull.Value, r[6]);

            Assert.Equal(projektVorher, Abzug("Tab_Heizkessel"));

            var zweiter = new KesselkatalogNachpflege.Ergebnis();
            KesselkatalogNachpflege.Nachpflegen(KesselkatalogNachpflege.Lesen(_ordner, zweiter, 25), zweiter, trocken: false);
            Assert.Empty(zweiter.Aenderungen);
        }

        /// <summary>
        /// Brennwert wird nie gelöscht: Sagt der Katalog 1 und die Datei nicht, bleibt 1, und der Lauf
        /// nennt den Satz als Hinweis.
        /// </summary>
        [Fact]
        public void Brennwert_wird_gesetzt_aber_nie_geloescht()
        {
            if (!_db.Vorhanden) return;
            ProbeAblegen("heizkessel_sonderfaelle.vdi");

            int id = HeizkesselStammCtrl.IdZu("atmoTEC exclusiv VC 104/4-7 E");
            Assert.True(id > 0);
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Heizkessel_STAMM SET Brennwert = 1 WHERE ID = ?",
                                                  new DbParam("?", id)));

            var e = new KesselkatalogNachpflege.Ergebnis();
            KesselkatalogNachpflege.Nachpflegen(KesselkatalogNachpflege.Lesen(_ordner, e, 25), e, trocken: false);
            Assert.Equal(1L, Zahl("SELECT Brennwert FROM Tab_Heizkessel_STAMM WHERE ID = ?", id));
            Assert.Contains(e.Hinweise, h => h.StartsWith(id + " ", StringComparison.Ordinal));
        }

        /// <summary>
        /// Die Gegenprobe gegen ALLE Herstellerdateien: Die Testdatenbank trägt die Nachpflege schon —
        /// ein weiterer Lauf ändert nichts, und 46 zugeordnete Katalogsätze sind Brennwertgeräte. Ohne
        /// geholte Dateien (CI, LFS-Zeiger) schweigt der Fall.
        /// </summary>
        [Fact]
        public void Die_Testdatenbank_ist_aus_allen_Herstellerdateien_nachgepflegt()
        {
            if (!_db.Vorhanden) return;
            if (KesselImport710Tests.Herstellerdateien().Count == 0) return;

            string wurzel = Repowurzel();
            string ordner = Path.Combine(wurzel, "VDI-3805-Daten", "SPK-Daten");
            var e = new KesselkatalogNachpflege.Ergebnis();
            List<KesselkatalogNachpflege.Dateisatz> saetze = KesselkatalogNachpflege.Lesen(ordner, e, 25);
            if (e.Zeiger.Count > 0) return;                          // teilweise geholt: keine Aussage
            KesselkatalogNachpflege.Nachpflegen(saetze, e, trocken: true);

            Assert.True(e.Aenderungen.Count == 0,
                        "Die Testdatenbank ist nicht nachgepflegt: " +
                        string.Join("; ", e.Aenderungen.Take(5).Select(a => a.ToString())));
            Assert.Equal(46, e.Brennwertgeraete);
            Assert.Equal(60, e.Zugeordnet);
            Assert.Empty(e.Mehrdeutig);
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        private void ProbeAblegen(string name)
        {
            string quelle = Path.Combine(Repowurzel(), "Referenzlaeufe", "Importproben", name);
            Assert.True(File.Exists(quelle), "Die Probe fehlt: " + quelle);
            File.Copy(quelle, Path.Combine(_ordner, name));
        }

        private static long Zahl(string sql, int id)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, new DbParam("?", id)), CultureInfo.InvariantCulture);

        /// <summary>Jede Zeile einer Tabelle mit allen Spalten — der Zellvergleich.</summary>
        private static List<string> Abzug(string tabelle)
        {
            var liste = new List<string>();
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + tabelle + " ORDER BY ID");
            foreach (DataRow r in dt.Rows)
                liste.Add(string.Join("|", r.ItemArray.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture))));
            return liste;
        }

        private static string Repowurzel()
        {
            for (DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory); d != null; d = d.Parent)
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
            return null;
        }
    }
}
