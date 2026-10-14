using System;
using System.Data;
using System.Globalization;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die fünf Kennlinienfelder des Heizkessels an der Datenbankgrenze (Konzept Kesselkennlinie 3.1,
    /// Etappe E1): <see cref="KesselKennlinieWerte"/> und die beiden Controller.
    ///
    /// <para><b>Geprüft wird:</b> die Prozentregel; die Plausibilität (Bereiche, Mindestleistung
    /// höchstens die Nennleistung, der Schalter nur beim Brennwertkessel, leer immer zulässig); der
    /// Rundlauf durch den Katalog (Anlegen, Lesen, Überschreiben), die Projektkopie aus dem Katalog
    /// und der Schreibweg der Projektkopie — leer bleibt leer, der Schalter geht nie ohne
    /// <c>Brennwert</c> in die Datenbank.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KesselKennlinieWerteTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Ein Katalogkessel der Testdatenbank (Brennwert-Kessel, Erdgas, 22 kW).</summary>
        private const string KESSEL = "GC7000F 22 23 - MX25";

        /// <summary>Projekt 1007 mit der Projektkopie 1007239.</summary>
        private const int PROJEKT = 1007;

        // =============================================================================
        //  Teil 1 - Regeln ohne Datenbank
        // =============================================================================

        [Theory]
        [InlineData(1.05, 1.05)]
        [InlineData(105.0, 1.05)]
        [InlineData(1.5, 1.5)]
        [InlineData(0.93, 0.93)]
        public void Die_Prozentregel_gilt_ueber_eins_komma_fuenf(double eingabe, double faktor)
        {
            Assert.Equal(faktor, KesselKennlinieWerte.AlsFaktor(eingabe).Value, 12);
            Assert.Null(KesselKennlinieWerte.AlsFaktor(null));
        }

        [Fact]
        public void Leere_Felder_sind_immer_zulaessig()
        {
            var m = new HeizkesselModel { Ptherm = 20 };
            Assert.Null(m.Wirkungsgrad_Teillast30);
            Assert.False(m.Kennlinie_Brennwert);
            Assert.Null(m.Mindestleistung);
            Assert.Null(m.Anfahrverlust_kWh);
            Assert.Null(m.Mindestlaufzeit_min);
            Assert.Null(KesselKennlinieWerte.Verstoss(m));
        }

        [Fact]
        public void Gepflegte_Kennlinie_im_Bereich_ist_zulaessig()
        {
            var m = new HeizkesselModel
            {
                Ptherm = 20, Brennwert = true, Kennlinie_Brennwert = true,
                Wirkungsgrad_Teillast30 = 107.5, Mindestleistung = 4, Anfahrverlust_kWh = 0.02,
                Mindestlaufzeit_min = 10
            };
            Assert.Null(KesselKennlinieWerte.Verstoss(m));
        }

        [Fact]
        public void Verstoesse_werden_benannt_abgelehnt()
        {
            using var _ = new Kulturvorrichtung();

            Assert.NotNull(KesselKennlinieWerte.Verstoss(new HeizkesselModel { Wirkungsgrad_Teillast30 = 0.3 }));
            Assert.NotNull(KesselKennlinieWerte.Verstoss(new HeizkesselModel { Wirkungsgrad_Teillast30 = 1.3 }));
            Assert.NotNull(KesselKennlinieWerte.Verstoss(new HeizkesselModel { Wirkungsgrad_Teillast30 = 130 }));
            Assert.NotNull(KesselKennlinieWerte.Verstoss(new HeizkesselModel { Ptherm = 20, Mindestleistung = 25 }));
            Assert.NotNull(KesselKennlinieWerte.Verstoss(new HeizkesselModel { Mindestleistung = -1 }));
            Assert.NotNull(KesselKennlinieWerte.Verstoss(new HeizkesselModel { Anfahrverlust_kWh = -0.1 }));
            Assert.NotNull(KesselKennlinieWerte.Verstoss(new HeizkesselModel { Mindestlaufzeit_min = 0 }));
            Assert.NotNull(KesselKennlinieWerte.Verstoss(new HeizkesselModel { Mindestlaufzeit_min = 61 }));

            string ohneBrennwert = KesselKennlinieWerte.Verstoss(
                new HeizkesselModel { Brennwert = false, Kennlinie_Brennwert = true });
            Assert.Contains("Brennwertkessel", ohneBrennwert);
        }

        [Fact]
        public void Der_Schalter_geht_nur_mit_Brennwert_in_die_Datenbank()
        {
            Assert.Equal(1, KesselKennlinieWerte.SchalterZumSchreiben(
                new HeizkesselModel { Brennwert = true, Kennlinie_Brennwert = true }));
            Assert.Equal(0, KesselKennlinieWerte.SchalterZumSchreiben(
                new HeizkesselModel { Brennwert = false, Kennlinie_Brennwert = true }));
            Assert.Equal(0, KesselKennlinieWerte.SchalterZumSchreiben(
                new HeizkesselModel { Brennwert = true, Kennlinie_Brennwert = false }));

            DbParam[] p = KesselKennlinieWerte.Parameter(new HeizkesselModel());
            Assert.Equal(5, p.Length);
            Assert.Equal(DBNull.Value, p[0].Wert);
            Assert.Equal(0, p[1].Wert);
            Assert.Equal(DBNull.Value, p[2].Wert);
            Assert.Equal(DBNull.Value, p[3].Wert);
            Assert.Equal(DBNull.Value, p[4].Wert);
        }

        // =============================================================================
        //  Teil 2 - Katalog, Projektkopie
        // =============================================================================

        /// <summary>
        /// Anlegen mit Kennlinie, lesen, überschreiben mit leeren Feldern: Die Werte kommen an, und
        /// ein geleertes Feld steht danach als NULL (nicht als 0) im Katalog.
        /// </summary>
        [Fact]
        public void Katalog_Rundlauf_traegt_die_Kennlinie()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var vorlage = new HeizkesselStammCtrl();
            vorlage.ReadSingle(KESSEL);
            Assert.Equal(1, vorlage.rows);

            HeizkesselModel daten = Kopie(vorlage);
            daten.Brennwert = true;
            daten.Kennlinie_Brennwert = true;
            daten.Wirkungsgrad_Teillast30 = 1.07;
            daten.Mindestleistung = 4.5;
            daten.Anfahrverlust_kWh = 0.02;
            daten.Mindestlaufzeit_min = 12;

            HeizkesselStammCtrl.SpeicherErgebnis angelegt = HeizkesselStammCtrl.Anlegen(daten, "Probe Kennlinie");
            Assert.True(angelegt.Ok, angelegt.Meldung);

            var gelesen = new HeizkesselStammCtrl();
            gelesen.ReadSingle("Probe Kennlinie");
            Assert.Equal(1.07, gelesen.Wirkungsgrad_Teillast30);
            Assert.True(gelesen.Kennlinie_Brennwert);
            Assert.Equal(4.5, gelesen.Mindestleistung);
            Assert.Equal(0.02, gelesen.Anfahrverlust_kWh);
            Assert.Equal(12, gelesen.Mindestlaufzeit_min);

            HeizkesselModel geleert = Kopie(gelesen);
            geleert.Wirkungsgrad_Teillast30 = null;
            geleert.Kennlinie_Brennwert = false;
            geleert.Mindestleistung = null;
            geleert.Anfahrverlust_kWh = null;
            geleert.Mindestlaufzeit_min = null;
            HeizkesselStammCtrl.SpeicherErgebnis ueber = HeizkesselStammCtrl.Ueberschreiben(geleert);
            Assert.True(ueber.Ok, ueber.Meldung);

            DataTable dt = DataRepository.GetDataTable(
                "SELECT Wirkungsgrad_Teillast30, Kennlinie_Brennwert, Mindestleistung, Anfahrverlust_kWh, " +
                "Mindestlaufzeit_min FROM Tab_Heizkessel_STAMM WHERE Bezeichner = ?", new DbParam("?", "Probe Kennlinie"));
            Assert.Equal(1, dt.Rows.Count);
            Assert.Equal(DBNull.Value, dt.Rows[0][0]);
            Assert.Equal(0L, Convert.ToInt64(dt.Rows[0][1], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, dt.Rows[0][2]);
            Assert.Equal(DBNull.Value, dt.Rows[0][3]);
            Assert.Equal(DBNull.Value, dt.Rows[0][4]);
        }

        /// <summary>
        /// Die Katalogwege lehnen den Schalter ohne Brennwert ab — beim Anlegen und beim Überschreiben
        /// —, und nichts wird geschrieben.
        /// </summary>
        [Fact]
        public void Katalog_lehnt_die_Brennwertkennlinie_ohne_Brennwert_ab()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            var vorlage = new HeizkesselStammCtrl();
            vorlage.ReadSingle(KESSEL);
            HeizkesselModel daten = Kopie(vorlage);
            daten.Brennwert = false;
            daten.Kennlinie_Brennwert = true;

            HeizkesselStammCtrl.SpeicherErgebnis neu = HeizkesselStammCtrl.Anlegen(daten, "Probe ohne Brennwert");
            Assert.False(neu.Ok);
            Assert.Equal(0, HeizkesselStammCtrl.AnzahlMitBezeichner("Probe ohne Brennwert"));

            HeizkesselStammCtrl.SpeicherErgebnis ueber = HeizkesselStammCtrl.Ueberschreiben(daten);
            Assert.False(ueber.Ok);
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT Kennlinie_Brennwert FROM Tab_Heizkessel_STAMM WHERE ID = ?", new DbParam("?", vorlage.ID)),
                CultureInfo.InvariantCulture));
        }

        /// <summary>
        /// Die Projektkopie aus dem Katalog trägt die Kennlinie; ihr Schreibweg hält leer als leer und
        /// den Schalter nur mit Brennwert.
        /// </summary>
        [Fact]
        public void Projektkopie_traegt_die_Kennlinie_und_haelt_den_Schalter()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            int stammId = HeizkesselStammCtrl.IdZu(KESSEL);
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Heizkessel_STAMM SET Brennwert = 1, Kennlinie_Brennwert = 1, Wirkungsgrad_Teillast30 = ?, " +
                "Mindestleistung = ?, Anfahrverlust_kWh = ?, Mindestlaufzeit_min = ? WHERE ID = ?",
                new DbParam("?", 1.06), new DbParam("?", 4.0), new DbParam("?", 0.05), new DbParam("?", 15),
                new DbParam("?", stammId)));

            int kopie = new HeizkesselCtrl().CopyFromStamm(stammId, PROJEKT);
            Assert.True(kopie > 0);

            var projekt = new HeizkesselCtrl();
            projekt.ReadAll("ID = " + kopie.ToString(CultureInfo.InvariantCulture));
            Assert.Single(projekt.items);
            HeizkesselModel k = projekt.items[0];
            Assert.Equal(1.06, k.Wirkungsgrad_Teillast30);
            Assert.True(k.Kennlinie_Brennwert);
            Assert.Equal(4.0, k.Mindestleistung);
            Assert.Equal(0.05, k.Anfahrverlust_kWh);
            Assert.Equal(15, k.Mindestlaufzeit_min);

            // Der Schreibweg der Projektkopie: Brennwert weg -> der Schalter faellt mit; ein
            // geleertes Feld bleibt NULL.
            var schreiber = new HeizkesselCtrl();
            schreiber.ReadAll("ID = " + kopie.ToString(CultureInfo.InvariantCulture));
            HeizkesselModel geladen = schreiber.items[0];
            schreiber.ID = geladen.ID;
            schreiber.Name = geladen.Name;
            schreiber.Ptherm = geladen.Ptherm;
            schreiber.Brennstoff = geladen.Brennstoff;
            schreiber.Brennwert = false;
            schreiber.Kennlinie_Brennwert = true;
            schreiber.Wirkungsgrad_Teillast30 = null;
            schreiber.Mindestleistung = 3.0;
            Assert.True(schreiber.Update());

            DataTable dt = DataRepository.GetDataTable(
                "SELECT Kennlinie_Brennwert, Wirkungsgrad_Teillast30, Mindestleistung FROM Tab_Heizkessel WHERE ID = ?",
                new DbParam("?", kopie));
            Assert.Equal(0L, Convert.ToInt64(dt.Rows[0][0], CultureInfo.InvariantCulture));
            Assert.Equal(DBNull.Value, dt.Rows[0][1]);
            Assert.Equal(3.0, Convert.ToDouble(dt.Rows[0][2], CultureInfo.InvariantCulture));
        }

        // -----------------------------------------------------------------------------
        //  Hilfen
        // -----------------------------------------------------------------------------

        /// <summary>Ein unabhängiges Modell mit denselben Feldern (samt ID und Kennlinie).</summary>
        private static HeizkesselModel Kopie(HeizkesselModel q)
        {
            var m = new HeizkesselModel
            {
                ID = q.ID, Name = q.Name, Firma = q.Firma, Beschreibung = q.Beschreibung, Ptherm = q.Ptherm,
                Brennstoff = q.Brennstoff, Wirkungsgrad_Gas = q.Wirkungsgrad_Gas, Wirkungsgrad_Oel = q.Wirkungsgrad_Oel,
                Investitionskosten = q.Investitionskosten, Raumbedarf = q.Raumbedarf, Wartungskosten = q.Wartungskosten,
                Wartungskosten_Einheit = q.Wartungskosten_Einheit, Nutzungsdauer = q.Nutzungsdauer, CO2 = q.CO2,
                SO2 = q.SO2, NOx = q.NOx, CO = q.CO, Staub = q.Staub,
                Betriebsbereitschaftverlust = q.Betriebsbereitschaftverlust, Brennwert = q.Brennwert,
                Vorlauf = q.Vorlauf, Ruecklauf = q.Ruecklauf
            };
            KesselKennlinieWerte.Uebertragen(q, m);
            return m;
        }
    }
}
