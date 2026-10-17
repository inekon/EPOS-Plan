using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Saatwache der sechs Kältebedarfe</b> (<see cref="KaeltetypSaat"/>, Konzept Kältebedarf 3.3, gerufen im Schritt
    /// <see cref="KaeltebedarfSchema"/>): Tafel, 168 Wochenstunden, Monatsgang, ReadOnly, Katalogschlüssel, Wiederholbarkeit.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaeltetypSaatTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Die Tafel — Name, Wochensumme, Vorlauf, Rücklauf (Angabe).</summary>
        private static readonly (string Name, double Woche, double Vorlauf, double Ruecklauf)[] TAFEL =
        {
            ("Raumkühlung Büro",        45.0,  16,  19),
            ("Raumkühlung Handel",      67.2,  16,  19),
            ("Prozesskälte Dauerlast", 168.0,   6,  12),
            ("Kühlraum",               157.5,  -2,   4),
            ("Tiefkühlraum",           161.0, -30, -24),
            ("Serverraum",             168.0,  18,  24),
        };

        [Fact]
        public void Die_Saat_haelt_die_Tafel()
        {
            Assert.Equal(TAFEL.Select(t => t.Name), KaeltetypSaat.Alle.Select(s => s.Name));
            foreach (var (name, woche, vorlauf, ruecklauf) in TAFEL)
            {
                KaeltetypSaatsatz s = KaeltetypSaat.Alle.Single(x => x.Name == name);
                Assert.Equal(168, s.Woche.Length);
                Assert.Equal(woche, s.Wochensumme(), 9);
                Assert.Equal(vorlauf, s.Vorlauf);
                Assert.Equal(ruecklauf, s.Ruecklauf);
                Assert.Null(KaeltebedarfSchema.Paarpruefung(s.Vorlauf, s.Ruecklauf));
                Assert.Equal(12, s.Monatsfaktoren.Length);
                Assert.Equal(KaeltetypSaat.JAHRESMENGE_MWH, s.Monatswerte().Sum(), 9);
                Assert.All(s.Woche, w => Assert.InRange(w, 0.0, 1.0));
                Assert.EndsWith(KaeltetypSaat.VERMERK, s.Beschreibung, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Raumkuehlung_ist_sommerbetont_Lager_und_Dauerlast_flach()
        {
            double[] buero = KaeltetypSaat.Alle[0].Monatswerte();
            Assert.Equal(0.0, buero[0] + buero[1] + buero[2] + buero[3] + buero[9] + buero[10] + buero[11], 9);
            Assert.True(buero[6] > buero[5] && buero[5] > buero[4]);
            double[] w = KaeltetypSaat.Alle[0].Woche;
            Assert.Equal(0.0, w.Skip(5 * 24).Sum(), 9);                 // Wochenende ohne Kühlung
            Assert.Equal(1.0, w[15]);                                    // Montag 15 Uhr: Spitze nachmittags
            Assert.Equal(0.0, KaeltetypSaat.Alle[1].Woche.Skip(6 * 24).Sum(), 9);   // Handel: Sonntag 0
            Assert.All(KaeltetypSaat.Alle[2].Monatsfaktoren, f => Assert.Equal(1.0, f));
            Assert.All(KaeltetypSaat.Alle[5].Monatsfaktoren, f => Assert.Equal(1.0, f));
            Assert.True(KaeltetypSaat.Alle[3].Monatsfaktoren[6] > KaeltetypSaat.Alle[3].Monatsfaktoren[0]);
            Assert.True(KaeltetypSaat.Alle[3].Woche[12] > KaeltetypSaat.Alle[3].Woche[2]);   // tags erhöht
        }

        [Fact]
        public void Die_Testkopie_traegt_sechs_gesperrte_Saetze_mit_Schluessel()
        {
            if (!_db.Vorhanden) return;
            Assert.True(KaeltetypSaat.Vollstaendig());
            foreach (KaeltetypSaatsatz s in KaeltetypSaat.Alle)
            {
                DataTable k = DataRepository.GetDataTable("SELECT * FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?", new DbParam("@b", s.Name));
                DataRow r = Assert.Single(k.Rows.Cast<DataRow>());
                Assert.Equal(1L, Convert.ToInt64(r["ReadOnly"], CultureInfo.InvariantCulture));
                Assert.Equal(s.Name, r["Typ"]);
                Assert.StartsWith("KB:", Convert.ToString(r["Katalog_Schluessel"], CultureInfo.InvariantCulture), StringComparison.Ordinal);
                Assert.Equal(64, Convert.ToString(r["Katalog_Pruefsumme"], CultureInfo.InvariantCulture).Length);
                double summe = Enumerable.Range(1, 12).Sum(m => Convert.ToDouble(r["Monat_" + m], CultureInfo.InvariantCulture));
                Assert.Equal(KaeltetypSaat.JAHRESMENGE_MWH, summe, 6);

                DataTable t = DataRepository.GetDataTable("SELECT * FROM Tab_Kaeltetyp_STAMM WHERE Bezeichner = ?", new DbParam("@b", s.Name));
                DataRow tr = Assert.Single(t.Rows.Cast<DataRow>());
                Assert.Equal(1L, Convert.ToInt64(tr["ReadOnly"], CultureInfo.InvariantCulture));
                Assert.StartsWith("KBT:", Convert.ToString(tr["Katalog_Schluessel"], CultureInfo.InvariantCulture), StringComparison.Ordinal);
                double woche = Enumerable.Range(1, 168).Sum(h => Convert.ToDouble(tr[h.ToString(CultureInfo.InvariantCulture)], CultureInfo.InvariantCulture));
                Assert.Equal(s.Wochensumme(), woche, 9);
                Assert.True(woche > 0);
            }
        }

        [Fact]
        public void Die_Saat_ist_wiederholbar_und_ueberschreibt_keinen_eigenen_Satz()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = 'Serverraum'");
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Kaeltetyp_STAMM WHERE Bezeichner = 'Serverraum'");
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Kaeltetyp_STAMM (Bezeichner, ReadOnly) VALUES ('Serverraum', 0)");

            var bericht = new List<string>();
            KaeltetypSaat.Bericht b = KaeltetypSaat.Ausfuehren(bericht);
            Assert.Equal(0, b.Koepfe);
            Assert.Equal(new[] { "Serverraum" }, b.Eigene);
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = 'Serverraum'"), CultureInfo.InvariantCulture));

            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Kaeltetyp_STAMM WHERE Bezeichner = 'Serverraum'");
            b = KaeltetypSaat.Ausfuehren(bericht);
            Assert.Equal(1, b.Koepfe);
            Assert.Equal(1, b.Profile);
            Assert.Equal(0, KaeltetypSaat.Ausfuehren(null).Koepfe);
        }
    }
}
