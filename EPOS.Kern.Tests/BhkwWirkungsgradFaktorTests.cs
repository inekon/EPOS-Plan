using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERENTSCHEID vom 19.09.2026 — Schemaschritt <b>98</b>: Der BHKW-Wirkungsgrad
    /// ist ein FAKTOR, kein Prozentwert.
    ///
    /// <para><b>Der Befund.</b> <c>Tab_BHKW[_STAMM].Wirkungsgrad</c> trägt den
    /// GESAMTwirkungsgrad als Faktor — so sagt es die Maske, so rechnet
    /// <c>SimulationBHKW.Auswertung</c>
    /// (<c>Verbrauch = (Wärme + Strom) / Wirkungsgrad</c>). Ein Teil des Katalogs führte
    /// dort einen Prozentwert, und zwar den des ELEKTRISCHEN Wirkungsgrads; der
    /// Brennstoff fiel dadurch um rund Faktor 32 zu klein aus.</para>
    ///
    /// <para>Geprüft wird viererlei: die RECHNUNG samt Band an einer synthetischen Zeile,
    /// die WIEDERHOLBARKEIT (die nachgezogene Arbeitskopie steht bereits auf 98, also
    /// findet der Schritt dort nichts mehr), die AUSWEISUNG der Zeilen, die der Schritt
    /// stehen lässt — und die eine Zahl, um die es geht: der Gasverbrauch des Moduls
    /// „EC-POWER XRGI 15" bei 1 016 Volllaststunden.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BhkwWirkungsgradFaktorTests
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        /// <summary>Das Modul, an dem der Anwender den Fehler gesehen hat.</summary>
        private const string XRGI15 = "EC-POWER XRGI 15";

        /// <summary>Die Volllaststunden des Abnahmepunkts A-BW1-2.</summary>
        private const double VOLLLASTSTUNDEN = 1016.0;

        // =============================================================================
        //  Teil 1 - Zielstand, Rechnung und Band
        // =============================================================================

        /// <summary>Der Zielstand ist 98, und die Kennzahlen des Schrittes stehen fest.</summary>
        [Fact]
        public void Der_Zielstand_ist_98_und_das_Band_steht_bei_0_5_bis_1_05()
        {
            using var _ = new Kulturvorrichtung();

            Assert.True(SchemaStand.Zielversion >= 98,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 98.");

            Assert.Equal(1.0, BhkwWirkungsgradFaktor.GRENZE);
            Assert.Equal(0.5, BhkwWirkungsgradFaktor.BAND_VON);
            Assert.Equal(1.05, BhkwWirkungsgradFaktor.BAND_BIS);
            Assert.Equal(100.0, BhkwWirkungsgradFaktor.PROZENT);
            Assert.Equal(4, BhkwWirkungsgradFaktor.STELLEN);

            // Beide Tabellen, Katalog zuerst - die Projektkopien rechnen mit.
            Assert.Equal(new[] { "Tab_BHKW_STAMM", "Tab_BHKW" }, BhkwWirkungsgradFaktor.Tabellen);
        }

        /// <summary>
        /// Die Rechnung ist die Umkehrung der Herleitung: Der Prozentwert ist der
        /// ELEKTRISCHE Wirkungsgrad, der Brennstoff daraus <c>Pel / eta_el</c>, und der
        /// Gesamtwirkungsgrad <c>(Ptherm + Pel)</c> durch diesen Brennstoff.
        /// </summary>
        [Fact]
        public void Die_Rechnung_fuehrt_XRGI_15_von_29_5_auf_0_9216()
        {
            using var _ = new Kulturvorrichtung();

            const double ptherm = 30.8, pel = 14.5, prozent = 29.5;

            double brennstoff = pel / (prozent / 100.0);          // 49,15 kW
            double gesamt = (ptherm + pel) / brennstoff;          // 0,9216

            Assert.Equal(49.153, brennstoff, 3);
            Assert.Equal(0.9216, System.Math.Round(gesamt, 4));

            // Und genau das rechnet die Anweisung: (Ptherm + Pel) * W / 100 / Pel.
            Assert.Equal(System.Math.Round(gesamt, 4),
                         System.Math.Round((ptherm + pel) * prozent / 100.0 / pel, 4));
        }

        /// <summary>
        /// Die Umrechnung an einer synthetischen Zeile — samt dem Band: Ein Wert, dessen
        /// Rechnung daneben fällt, bleibt stehen und wird ausgewiesen.
        /// </summary>
        [Fact]
        public void Der_Schritt_rechnet_um_weist_aus_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            const int idProzent = 987654301;     // 29,3 -> 0,9474, im Band
            const int idBrennwert = 987654302;   // 1,03 ist schon ein Faktor, ausserhalb
            const int idOhnePel = 987654303;     // ohne elektrische Leistung

            try
            {
                Einfuegen(idProzent, "BW1 Prozentwert", 20.1, 9.0, 29.3);
                Einfuegen(idBrennwert, "BW1 Brennwert", 46.0, 21.0, 1.03);
                Einfuegen(idOhnePel, "BW1 ohne Pel", 20.0, 0.0, 33.0);

                // Genau EINE der drei Zeilen ist umzurechnen.
                List<int> offen = Offene();
                Assert.Contains(idProzent, offen);
                Assert.DoesNotContain(idBrennwert, offen);
                Assert.DoesNotContain(idOhnePel, offen);

                Anweisungen_ausfuehren();

                Assert.Equal(0.9474, Wirkungsgrad(idProzent), 4);
                Assert.Equal(1.03, Wirkungsgrad(idBrennwert), 4);    // unveraendert
                Assert.Equal(33.0, Wirkungsgrad(idOhnePel), 4);      // unveraendert

                // Beide stehen gebliebenen Zeilen sind BENANNT ausgewiesen - mit Id,
                // Namen, altem und gerechnetem Wert.
                List<BhkwWirkungsgradFaktor.Ausweis> aus =
                    BhkwWirkungsgradFaktor.Ausgewiesene(BhkwWirkungsgradFaktor.TAB_STAMM);
                BhkwWirkungsgradFaktor.Ausweis brennwert = aus.FirstOrDefault(a => a.Id == idBrennwert);
                Assert.NotNull(brennwert);
                Assert.Equal("BW1 Brennwert", brennwert.Bezeichner);
                Assert.Equal(1.03, brennwert.Alt, 4);
                Assert.True(brennwert.Gerechnet.HasValue && brennwert.Gerechnet.Value < 0.5,
                            "Die Rechnung des Brennwertsatzes muesste weit unter dem Band liegen.");
                Assert.Contains("BW1 Brennwert", brennwert.Zeile());

                Assert.Contains(aus, a => a.Id == idOhnePel);

                // WIEDERHOLBAR: Der zweite Lauf findet nichts mehr.
                Assert.DoesNotContain(idProzent, Offene());
                Anweisungen_ausfuehren();
                Assert.Equal(0.9474, Wirkungsgrad(idProzent), 4);
            }
            finally
            {
                foreach (int id in new[] { idProzent, idBrennwert, idOhnePel })
                    DataRepository.ExecuteNonQuery(
                        "DELETE FROM [Tab_BHKW_STAMM] WHERE [ID] = ?", new DbParam("@id", id));
            }
        }

        // =============================================================================
        //  Teil 2 - der Bestand der Testdatenbank
        // =============================================================================

        /// <summary>
        /// Die nachgezogene Arbeitskopie steht auf 98: Kein Katalogsatz und keine
        /// Projektkopie trägt mehr einen Prozentwert, der ins Band fiele.
        /// </summary>
        [Fact]
        public void Die_Arbeitskopie_steht_auf_98_und_haelt_keinen_Prozentwert_mehr()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            foreach (string t in BhkwWirkungsgradFaktor.Tabellen)
                Assert.Equal(0, BhkwWirkungsgradFaktor.Offen(t));

            Assert.Equal(0, BhkwWirkungsgradFaktor.OffenGesamt());
        }

        /// <summary>
        /// <b>Die Probe des Schrittes:</b> Nach ihm steht in keiner der beiden Tabellen
        /// ein Wert über der Pflegegrenze 1,05 — also kein Prozentwert mehr. Was über 1
        /// blieb, ist ein Brennwertfaktor (1,023 … 1,048), den auch der Katalogdialog
        /// zulässt.
        /// </summary>
        [Fact]
        public void Nach_dem_Schritt_liegt_kein_Wert_mehr_ueber_der_Pflegegrenze()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            foreach (string t in BhkwWirkungsgradFaktor.Tabellen)
                Assert.Equal(0, BhkwWirkungsgradFaktor.UeberDerPflegegrenze(t));

            // Und die Zeilen ueber 1 sind allesamt Faktoren im zugelassenen Band.
            DataTable t2 = DataRepository.GetDataTable(
                "SELECT [ID], [Bezeichner], [Wirkungsgrad] FROM [Tab_BHKW_STAMM] " +
                "WHERE [Wirkungsgrad] > ? ORDER BY [ID]",
                new DbParam("@g", BhkwWirkungsgradFaktor.GRENZE));

            Assert.NotNull(t2);
            foreach (DataRow r in t2.Rows)
            {
                double w = System.Convert.ToDouble(r["Wirkungsgrad"], CultureInfo.InvariantCulture);
                Assert.InRange(w, BhkwWirkungsgradFaktor.GRENZE, BhkwWirkungsgradFaktor.BAND_BIS);
            }
        }

        /// <summary>
        /// Die Bestandsaufnahme wird VOR dem Schreiben gezogen — sonst stünde eine
        /// gerade umgerechnete Brennwertzeile (Faktor über 1) als „ausgewiesen" da.
        /// Auf der bereits umgerechneten Arbeitskopie meldet sie deshalb 0 umzurechnende
        /// Zeilen, und der Bericht nennt jede ausgewiesene Zeile mit ihrer Id.
        /// </summary>
        [Fact]
        public void Die_Bestandsaufnahme_traegt_Zaehlung_und_Ausweisung()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            BhkwWirkungsgradFaktor.Aufnahme a = BhkwWirkungsgradFaktor.Bestandsaufnahme();

            Assert.Equal(2, a.Gesamt.Count);
            foreach (string t in BhkwWirkungsgradFaktor.Tabellen)
                Assert.Equal(0, a.Umzurechnen[t]);

            string bericht = BhkwWirkungsgradFaktor.Bericht(a);
            Assert.Contains("Tab_BHKW_STAMM: 0 umgerechnet", bericht);
            foreach (BhkwWirkungsgradFaktor.Ausweis aus in a.Ausgewiesen)
                Assert.Contains(aus.Id.ToString(CultureInfo.InvariantCulture), bericht);
        }

        /// <summary>
        /// Die Projektkopien des Moduls „EC-POWER XRGI 15" tragen den Faktor — und die
        /// beiden Referenzprojekte 1018 und 1030 rechnen ab hier damit.
        /// </summary>
        [Fact]
        public void Die_Projektkopien_tragen_den_Faktor()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            DataTable t = DataRepository.GetDataTable(
                "SELECT [ID_Projekt], [Wirkungsgrad] FROM [Tab_BHKW] WHERE [Bezeichner] = ?",
                new DbParam("@b", XRGI15));

            Assert.NotNull(t);
            Assert.NotEmpty(t.Rows.Cast<DataRow>());

            foreach (DataRow r in t.Rows)
                Assert.Equal(0.9216, System.Convert.ToDouble(r["Wirkungsgrad"],
                                                             CultureInfo.InvariantCulture), 4);
        }

        // =============================================================================
        //  Teil 3 - die Zahl, um die es geht (A-BW1-2)
        // =============================================================================

        /// <summary>
        /// A-BW1-2: <b>1 016 Volllaststunden am XRGI 15 ergeben rund 49,9 MWh/a Gas</b>
        /// statt der 1,56 MWh/a, die der Prozentwert lieferte.
        ///
        /// <para>Gerechnet wird mit der EINEN Zeile des Laufs
        /// (<c>SimulationBHKW.Auswertung</c>): <c>Verbrauch = (Wärme + Strom) /
        /// Wirkungsgrad</c>, gelesen aus dem KATALOGSATZ der Arbeitskopie — nicht aus
        /// einer im Test hinterlegten Zahl.</para>
        /// </summary>
        [Fact]
        public void XRGI_15_verbraucht_bei_1016_Volllaststunden_rund_49_9_MWh()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            BHKWStammModel m = new BHKWStammCtrl().ReadModel(XRGI15);
            Assert.NotNull(m);
            Assert.Equal(0.9216, m.m_Wirkungsgrad, 4);

            double waermeMwh = m.m_Ptherm * VOLLLASTSTUNDEN / 1000.0;
            double stromMwh = m.m_Pel * VOLLLASTSTUNDEN / 1000.0;

            // Die eine Zeile des Rechenwegs.
            double verbrauchMwh = (waermeMwh + stromMwh) / m.m_Wirkungsgrad;

            Assert.InRange(verbrauchMwh, 49.9 * 0.99, 49.9 * 1.01);

            // Und die Groesse des frueheren Fehlgriffs: derselbe Weg mit dem
            // Prozentwert 29,5 haette 1,56 MWh/a geliefert.
            double mitProzentwert = (waermeMwh + stromMwh) / 29.5;
            Assert.InRange(mitProzentwert, 1.54, 1.58);
            Assert.True(verbrauchMwh / mitProzentwert > 30.0,
                        "Der Fehlgriff lag bei rund Faktor 32.");
        }

        // =============================================================================
        //  Teil 4 - der Schutz vor Wiederholung
        // =============================================================================

        /// <summary>
        /// Der Katalogschreibweg weist einen Prozentwert BENANNT ab — und lässt den
        /// Faktor durch. Geprüft am Weg <c>AnzeigefelderSchreiben</c>, den der
        /// Aufklapper „Alle Daten anzeigen" zieht.
        /// </summary>
        [Fact]
        public void Der_Katalogweg_weist_29_5_ab_und_laesst_0_92_durch()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            const string satz = XRGI15;

            BHKWStammModel vorher = new BHKWStammCtrl().ReadModel(satz);
            Assert.NotNull(vorher);

            var abgelehnt = new BHKWStammCtrl.AnzeigefelderBhkw(
                vorher.m_szFirma, vorher.m_Ptherm, vorher.m_Pel, vorher.m_Grenzleistung,
                vorher.m_Vorlauf, vorher.m_Ruecklauf, Wirkungsgrad: 29.5);

            BHKWStammCtrl.SpeicherErgebnis ergebnis =
                BHKWStammCtrl.AnzeigefelderSchreiben(satz, abgelehnt, true);
            Assert.False(ergebnis.Ok);
            Assert.False(string.IsNullOrEmpty(ergebnis.Meldung));
            Assert.Contains("Faktor", ergebnis.Meldung);

            // Der Satz steht unveraendert da.
            Assert.Equal(vorher.m_Wirkungsgrad, new BHKWStammCtrl().ReadModel(satz).m_Wirkungsgrad, 6);

            // Und der Faktor geht durch.
            var erlaubt = new BHKWStammCtrl.AnzeigefelderBhkw(
                vorher.m_szFirma, vorher.m_Ptherm, vorher.m_Pel, vorher.m_Grenzleistung,
                vorher.m_Vorlauf, vorher.m_Ruecklauf, Wirkungsgrad: 0.92);

            Assert.True(BHKWStammCtrl.AnzeigefelderSchreiben(satz, erlaubt, true).Ok);
            Assert.Equal(0.92, new BHKWStammCtrl().ReadModel(satz).m_Wirkungsgrad, 4);

            // Zuruecksetzen - die Arbeitskopie soll bleiben, wie der Schritt sie liess.
            var zurueck = new BHKWStammCtrl.AnzeigefelderBhkw(
                vorher.m_szFirma, vorher.m_Ptherm, vorher.m_Pel, vorher.m_Grenzleistung,
                vorher.m_Vorlauf, vorher.m_Ruecklauf, Wirkungsgrad: vorher.m_Wirkungsgrad);
            Assert.True(BHKWStammCtrl.AnzeigefelderSchreiben(satz, zurueck, true).Ok);
        }

        // =============================================================================
        //  Handreichungen
        // =============================================================================

        private static void Einfuegen(int id, string bezeichner, double ptherm, double pel,
                                      double wirkungsgrad)
        {
            DataRepository.ExecuteNonQuery(
                "INSERT INTO [Tab_BHKW_STAMM] ([ID], [Bezeichner], [Ptherm], [Pel], [Wirkungsgrad]) " +
                "VALUES (?, ?, ?, ?, ?)",
                new DbParam("@id", id), new DbParam("@b", bezeichner),
                new DbParam("@pt", ptherm), new DbParam("@pe", pel),
                new DbParam("@w", wirkungsgrad));
        }

        private static double Wirkungsgrad(int id)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT [Wirkungsgrad] FROM [Tab_BHKW_STAMM] WHERE [ID] = ?",
                new DbParam("@id", id));
            return o == null || o == System.DBNull.Value
                       ? 0 : System.Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Die Ids, die der Schritt gerade umrechnen würde.</summary>
        private static List<int> Offene()
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT [ID] FROM [Tab_BHKW_STAMM] WHERE [Wirkungsgrad] > ? AND [Pel] > 0 " +
                "AND [Ptherm] > 0 AND ROUND(([Ptherm] + [Pel]) * [Wirkungsgrad] / ? / [Pel], ?) " +
                "BETWEEN ? AND ?",
                BhkwWirkungsgradFaktor.ParameterZaehlen());

            var ids = new List<int>();
            if (t == null) return ids;
            foreach (DataRow r in t.Rows)
                ids.Add(System.Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture));
            return ids;
        }

        private static void Anweisungen_ausfuehren()
        {
            foreach (KeyValuePair<string, BhkwWirkungsgradFaktor.Anweisung> a
                     in BhkwWirkungsgradFaktor.Anweisungen)
                DataRepository.ExecuteNonQuery(a.Value.Sql, a.Value.Parameter);
        }
    }
}
