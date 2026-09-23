using System;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7c3 — <b>die Nachpflege der Katalog-Generation 9</b>
    /// (<see cref="GesetzKatalog.Nachpflege"/>): Die Generation 9 sät keine neue Zeile,
    /// sie pflegt bestehende — in jeder Datenbank, deren Saatstand darunter liegt, genau
    /// einmal, beim Start oder beim ersten Katalogzugriff, ohne Schemaschritt.
    ///
    /// <para><b>Punkt 6:</b> Der Brennstoff 24 „Sonstige" rechnet seit Schemaschritt 113
    /// in kWh (Entscheid E7c2‑Q4), sein Stamm trug aber H_i = H_s = 0 — ein neu
    /// zugeordneter Träger bekam den Heizwert 0. Die Nachpflege setzt 1,0 wie bei Strom
    /// und Fernwärme, und nur dort, wo beide Werte noch leer oder 0 sind.</para>
    ///
    /// <para>Die Fälle arbeiten auf einer eigenen Kopie der Testdatenbank und setzen den
    /// Marker ausdrücklich auf 8 — so hängen sie nicht am Saatstand der Messlatte.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KatalogNachpflegeTests
    {
        private const string TABELLE = "Tab_Gesetzesparameter";

        [Fact]
        public void Die_Generation_9_pflegt_nur_und_saet_keine_Zeile()
        {
            Assert.Equal(9, GesetzKatalog.AktuelleGeneration);
            Assert.Equal(8, GesetzKatalog.JuengsteSaatgeneration);
            Assert.DoesNotContain(GesetzKatalog.Vorbelegung(), p => p.Generation == 9);
            Assert.Contains(GesetzKatalog.Nachpflege(),
                            s => s.Generation == 9 && s.Tabelle == "Tab_Brennstoff_Stamm");
            Assert.Equal(1.0, GesetzKatalog.SONSTIGE_HEIZWERT);
        }

        [Fact]
        public void Brennstoff_24_bekommt_den_Heizwert_1_genau_einmal()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            MarkerAuf(8);
            HeizwertSonstige(0.0, 0.0);
            int zeilen = Ganzzahl("SELECT COUNT(*) FROM " + TABELLE);

            GesetzKatalog.StelleKatalogSicher();

            Assert.True(GesetzKatalog.SaatWarnungen.Count == 0,
                        "Die Nachpflege meldet Warnungen: " + string.Join(" | ", GesetzKatalog.SaatWarnungen));
            Assert.Equal(0, GesetzKatalog.ZuletztNachgesaet);          // keine neue Zeile
            Assert.True(GesetzKatalog.ZuletztNachgepflegt >= 1);
            Assert.Equal(zeilen, Ganzzahl("SELECT COUNT(*) FROM " + TABELLE));
            Assert.Equal(1.0, Kommazahl("SELECT Hi FROM Tab_Brennstoff_Stamm WHERE ID = 24"), 12);
            Assert.Equal(1.0, Kommazahl("SELECT Hs FROM Tab_Brennstoff_Stamm WHERE ID = 24"), 12);
            Assert.Equal(9, Marker());

            // Wiederholbar: Der Marker steht oben, ein zweiter Lauf pflegt nichts.
            GesetzKatalog.StelleKatalogSicher();
            Assert.Equal(0, GesetzKatalog.ZuletztNachgepflegt);
            Assert.Equal(0, GesetzKatalog.ZuletztNachgesaet);
        }

        /// <summary>Ein Heizwert, den der Anwender gepflegt hat, bleibt stehen — die
        /// Nachpflege schreibt nur über den ausgelieferten Stand 0/0.</summary>
        [Fact]
        public void Ein_gepflegter_Heizwert_bleibt_stehen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            MarkerAuf(8);
            HeizwertSonstige(2.5, 0.0);

            GesetzKatalog.StelleKatalogSicher();

            Assert.Empty(GesetzKatalog.SaatWarnungen);
            Assert.Equal(2.5, Kommazahl("SELECT Hi FROM Tab_Brennstoff_Stamm WHERE ID = 24"), 12);
            Assert.Equal(0.0, Kommazahl("SELECT Hs FROM Tab_Brennstoff_Stamm WHERE ID = 24"), 12);
            Assert.Equal(9, Marker());
        }

        /// <summary>
        /// Die Wirkung: Eine neue Zuordnung eines Trägers des Brennstoffs 24 schreibt den
        /// Heizwert aus dem Stamm in ihre Preishistorie
        /// (<c>WizardCtrl.TraegerSatzAnlegen</c>) — nach der Nachpflege 1,0 statt 0.
        /// </summary>
        [Fact]
        public void Ein_neuer_Traeger_des_Brennstoffs_24_bekommt_den_Heizwert_1()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            MarkerAuf(8);
            HeizwertSonstige(0.0, 0.0);
            GesetzKatalog.StelleKatalogSicher();

            DataRepository.ExecuteNonQuery(
                "INSERT INTO energy_carrier (ID_Brennstoff, name, billing_unit, hi_kwh_per_unit, is_active) " +
                "VALUES (?, ?, ?, ?, ?)",
                new DbParam("@b", 24), new DbParam("@n", "Probe Sonstige E7c3"), new DbParam("@u", "kWh"),
                new DbParam("@hi", 1.0), new DbParam("@a", (object)1));
            int traeger = Ganzzahl("SELECT MAX(id) FROM energy_carrier WHERE name = 'Probe Sonstige E7c3'");

            Assert.True(new WizardCtrl().TraegerSatzAnlegen(1030, traeger));

            Assert.Equal(1.0, Kommazahl(
                "SELECT heizwert FROM energy_price WHERE carrier_id = " +
                traeger.ToString(CultureInfo.InvariantCulture) + " AND id_projekt = 1030"), 12);
        }

        // =================================================================
        //  Werkzeug
        // =================================================================

        private static void MarkerAuf(int generation)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE " + TABELLE + " SET [Wert] = ? WHERE Schluessel = ?",
                new DbParam("@w", DbParamTyp.Double) { Wert = (double)generation },
                new DbParam("@s", DbParamTyp.VarWChar, 60) { Wert = DbWerte.GESETZ_KATALOG_GENERATION });
        }

        private static int Marker()
            => Ganzzahl("SELECT CAST([Wert] AS INTEGER) FROM " + TABELLE +
                        " WHERE Schluessel = '" + DbWerte.GESETZ_KATALOG_GENERATION + "'");

        private static void HeizwertSonstige(double hi, double hs)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Brennstoff_Stamm SET Hi = ?, Hs = ? WHERE ID = 24",
                new DbParam("@hi", DbParamTyp.Double) { Wert = hi },
                new DbParam("@hs", DbParamTyp.Double) { Wert = hs });
        }

        private static int Ganzzahl(string sql)
            => Convert.ToInt32(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);

        private static double Kommazahl(string sql)
            => Convert.ToDouble(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);
    }
}
