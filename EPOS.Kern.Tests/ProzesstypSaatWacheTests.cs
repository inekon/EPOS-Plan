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
    /// <b>Wache des Katalogs typischer Betriebsweisen</b> (Entscheidungsvorlage Modellgrenzen, PW5;
    /// Saat bei <see cref="ProzesstypSaat"/>, gerufen im Schritt <see cref="ProzesswaermeTemperaturSchema"/>).
    ///
    /// <para>Sie hält die acht ausgelieferten Sätze gegen eine EIGENE Tafel — Namen, Summe des
    /// Wochenprofils, Temperaturpaar — und gegen die gehobene Datenbank (die Testkopie fährt den
    /// Schritt): Kopfsatz und Wochenprofil je Name mit <c>ReadOnly = 1</c>, Jahresmenge 100 MWh,
    /// Vermerk „Schichtmodell, keine Messung", keine Zuordnung in einem Projekt. Dazu: die Saat ist
    /// wiederholbar und überschreibt keinen eigenen Satz.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ProzesstypSaatWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Die Tafel der Entscheidungsvorlage, PW5 — Name, Wochensumme, Vorlauf, Rücklauf.</summary>
        private static readonly (string Name, double Woche, double Vorlauf, double Ruecklauf)[] TAFEL =
        {
            ("Einschicht 5 Tage",       42.5,  60, 40),
            ("Zweischicht 5 Tage",      80.0,  70, 50),
            ("Dreischicht 5 Tage",     120.0,  80, 60),
            ("Durchlaufbetrieb 7 Tage", 162.4, 90, 70),
            ("Reinigung/Spülen (CIP)",  10.0,  75, 40),
            ("Trocknung/Lackierung",    82.5, 120, 90),
            ("Waschen/Bäder",           61.0,  60, 45),
            ("Raumlufttechnik Halle",   75.0,  50, 30),
        };

        [Fact]
        public void Die_Saat_haelt_die_Tafel()
        {
            Assert.Equal(TAFEL.Select(t => t.Name), ProzesstypSaat.Alle.Select(s => s.Name));
            foreach (var (name, woche, vorlauf, ruecklauf) in TAFEL)
            {
                ProzesstypSaatsatz s = ProzesstypSaat.Alle.Single(x => x.Name == name);
                Assert.Equal(168, s.Woche.Length);
                Assert.Equal(woche, s.Wochensumme(), 9);
                Assert.Equal(vorlauf, s.Vorlauf);
                Assert.Equal(ruecklauf, s.Ruecklauf);
                Assert.Equal(12, s.Monatsfaktoren.Length);
                Assert.Equal(ProzesstypSaat.JAHRESMENGE_MWH, s.Monatswerte().Sum(), 9);
                Assert.Contains("Schichtmodell, keine Messung", s.Beschreibung);
                Assert.Null(Prozesstemperatur.Paarpruefung(s.Vorlauf, s.Ruecklauf));
            }

            // Stichproben der Regeln: Einschicht Montag 5 Uhr halbe Last, 6 bis 13 Uhr voll, Samstag
            // nichts; Dreischicht Montag 6 Uhr bis Samstag 5 Uhr; Raumlufttechnik im Juli ein Zehntel
            // des Januars je Tag.
            ProzesstypSaatsatz e = ProzesstypSaat.Alle[0];
            Assert.Equal(0.5, e.Woche[5]);
            Assert.Equal(1.0, e.Woche[13]);
            Assert.Equal(0.0, e.Woche[14]);
            Assert.Equal(0.0, e.Woche[5 * 24 + 8]);
            ProzesstypSaatsatz d = ProzesstypSaat.Alle[2];
            Assert.Equal(0.0, d.Woche[5]);
            Assert.Equal(1.0, d.Woche[6]);
            Assert.Equal(1.0, d.Woche[5 * 24 + 5]);
            Assert.Equal(0.0, d.Woche[5 * 24 + 6]);
            double[] rlt = ProzesstypSaat.Alle[7].Monatswerte();
            Assert.Equal(0.1, rlt[6] / rlt[0], 9);
            double[] ferien = e.Monatswerte();
            Assert.Equal(0.4, ferien[7] / ferien[0], 9);     // August
        }

        [Fact]
        public void Die_gehobene_Datenbank_traegt_die_acht_Saetze()
        {
            if (!_db.Vorhanden) return;
            Assert.True(ProzesswaermeTemperaturSchema.Vollstaendig());

            foreach (var (name, woche, vorlauf, ruecklauf) in TAFEL)
            {
                DataTable kopf = DataRepository.GetDataTable(
                    "SELECT * FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = ?", new DbParam("@b", name));
                Assert.Equal(1, kopf.Rows.Count);
                DataRow k = kopf.Rows[0];
                Assert.Equal(1L, Convert.ToInt64(k["ReadOnly"], CultureInfo.InvariantCulture));
                Assert.Equal(name, Convert.ToString(k["Typ"], CultureInfo.InvariantCulture));
                Assert.Equal(vorlauf, Convert.ToDouble(k["Vorlauf"], CultureInfo.InvariantCulture));
                Assert.Equal(ruecklauf, Convert.ToDouble(k["Ruecklauf"], CultureInfo.InvariantCulture));
                double jahr = 0;
                for (int m = 1; m <= 12; m++) jahr += Convert.ToDouble(k["Monat_" + m], CultureInfo.InvariantCulture);
                Assert.Equal(100.0, jahr, 9);
                Assert.Contains("Schichtmodell, keine Messung", Convert.ToString(k["Beschreibung"], CultureInfo.InvariantCulture));

                DataTable typ = DataRepository.GetDataTable(
                    "SELECT * FROM Tab_Prozesstyp_STAMM WHERE Bezeichner = ?", new DbParam("@b", name));
                Assert.Equal(1, typ.Rows.Count);
                DataRow t = typ.Rows[0];
                Assert.Equal(1L, Convert.ToInt64(t["ReadOnly"], CultureInfo.InvariantCulture));
                double summe = 0;
                for (int j = 1; j <= 168; j++) summe += Convert.ToDouble(t[j.ToString(CultureInfo.InvariantCulture)], CultureInfo.InvariantCulture);
                Assert.Equal(woche, summe, 9);
            }

            // Kein Projekt ordnet einen Satz zu: Die Referenzbasis bleibt unberührt.
            object n = DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Z_Projekt_Prozesswaerme z INNER JOIN Tab_Prozesswaerme p ON p.ID = z.ID_Prozesswaerme " +
                "WHERE p.Bezeichner IN (SELECT Bezeichner FROM Tab_Prozesswaerme_STAMM WHERE ReadOnly = 1 AND Beschreibung LIKE ?)",
                new DbParam("@v", "%Schichtmodell, keine Messung%"));
            Assert.Equal(0L, Convert.ToInt64(n, CultureInfo.InvariantCulture));
        }

        [Fact]
        public void Die_Saat_ist_wiederholbar_und_ueberschreibt_keinen_eigenen_Satz()
        {
            if (!_db.Vorhanden) return;

            var zweiter = new List<string>();
            ProzesstypSaat.Bericht b = ProzesstypSaat.Ausfuehren(zweiter);
            Assert.Equal(0, b.Koepfe);
            Assert.Equal(0, b.Profile);

            // Ein eigener gleichnamiger Satz des Anwenders bleibt, wie er ist.
            string name = TAFEL[4].Name;
            DataRepository.ExecuteSQL("DELETE FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = ?", new DbParam("@b", name));
            DataRepository.ExecuteSQL("DELETE FROM Tab_Prozesstyp_STAMM WHERE Bezeichner = ?", new DbParam("@b", name));
            DataRepository.ExecuteSQL(
                "INSERT INTO Tab_Prozesswaerme_STAMM (Bezeichner, Typ, Beschreibung, ReadOnly) VALUES (?, ?, ?, 0)",
                new DbParam("@b", name), new DbParam("@t", name), new DbParam("@d", "eigen"));
            // Der eigene Satz belegt den Namen - der Katalog gilt als vollständig.
            Assert.True(ProzesswaermeTemperaturSchema.Vollstaendig());

            var bericht = new List<string>();
            b = ProzesstypSaat.Ausfuehren(bericht);
            Assert.Equal(0, b.Koepfe);
            Assert.Equal(0, b.Profile);
            Assert.Contains(name, b.Eigene);
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT COUNT(*) FROM Tab_Prozesstyp_STAMM WHERE Bezeichner = ?", new DbParam("@b", name)),
                CultureInfo.InvariantCulture));
            Assert.Equal("eigen", Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Beschreibung FROM Tab_Prozesswaerme_STAMM WHERE Bezeichner = ?", new DbParam("@b", name)),
                CultureInfo.InvariantCulture));

            // Fehlt nur ein ausgelieferter Teil, sät der Schritt ihn nach.
            DataRepository.ExecuteSQL("DELETE FROM Tab_Prozesstyp_STAMM WHERE Bezeichner = ?", new DbParam("@b", TAFEL[0].Name));
            Assert.False(ProzesswaermeTemperaturSchema.Vollstaendig());
            b = ProzesstypSaat.Ausfuehren(null);
            Assert.Equal(1, b.Profile);
            Assert.True(ProzesswaermeTemperaturSchema.Vollstaendig());
        }
    }
}
