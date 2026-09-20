using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ANWENDERENTSCHEID vom 20.09.2026 — Schemaschritt <b>99</b>: Der BHKW-Katalog
    /// führt den ELEKTRISCHEN und den THERMISCHEN Wirkungsgrad; der Gesamtwirkungsgrad
    /// ergibt sich daraus.
    ///
    /// <para><b>Was geprüft wird.</b> Die eine Rechenstelle
    /// (<see cref="BhkwWirkungsgrad"/>) mit ihren drei Regeln und ihren Grenzfällen; der
    /// Datenteil des Schrittes (<see cref="BhkwWirkungsgradAnteile"/>) an drei
    /// synthetischen Sätzen samt Ausweisung und Wiederholbarkeit; der Bestand der
    /// Arbeitskopie (Summe der Anteile = Gesamtwirkungsgrad, Zeile für Zeile); und die
    /// zwei Schreibwege des Kerns — <c>BHKWStammCtrl.Update</c> zieht die Summe nach,
    /// <c>BHKWCtrl.CopyFromStamm</c> nimmt beide Anteile ins Projekt mit.</para>
    ///
    /// <para><b>Der Rechenweg bleibt unberührt:</b> <c>SimulationBHKW</c> liest die
    /// Spalte <c>Wirkungsgrad</c>, und die ändert dieser Schritt nicht. Der
    /// Referenzlauf bleibt byte-gleich — die Basis R10 gilt weiter.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class BhkwWirkungsgradAnteileTests
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        /// <summary>Das Modul, dessen Datenblattwerte die Aufteilung bestätigen.</summary>
        private const string XRGI15 = "EC-POWER XRGI 15";

        // =============================================================================
        //  Teil 1 - die eine Rechenstelle
        // =============================================================================

        /// <summary>Der Zielstand ist 99, und die Kennzahlen der Rechenstelle stehen fest.</summary>
        [Fact]
        public void Der_Zielstand_ist_99_und_das_Band_steht_fest()
        {
            using var _ = new Kulturvorrichtung();

            Assert.True(SchemaStand.Zielversion >= 99,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 99.");

            Assert.Equal(0.0, BhkwWirkungsgrad.ANTEIL_VON);
            Assert.Equal(1.0, BhkwWirkungsgrad.ANTEIL_BIS);
            Assert.Equal(1.05, BhkwWirkungsgrad.GESAMT_BIS);
            Assert.Equal(4, BhkwWirkungsgrad.STELLEN);

            // Die Obergrenze kommt aus DERSELBEN Quelle wie in Schritt 98.
            Assert.Equal(BhkwWirkungsgradFaktor.BAND_BIS, BhkwWirkungsgrad.GESAMT_BIS);

            // Beide Tabellen, Katalog zuerst.
            Assert.Equal(new[] { "Tab_BHKW_STAMM", "Tab_BHKW" }, BhkwWirkungsgradAnteile.Tabellen);

            // Vier Spalten, je zwei an beiden Tabellen - eine Spalte nur auf einer Seite
            // waere beim Kopieren ins Projekt sofort ein Datenverlust.
            Assert.Equal(4, SchemaKatalog.Schritt99_BhkwWirkungsgradAnteile.Length);
            Assert.All(SchemaKatalog.Schritt99_BhkwWirkungsgradAnteile,
                       s => Assert.Equal("DOUBLE", s.TypDefinition));
        }

        /// <summary>Der Gesamtwirkungsgrad ist die SUMME — mehr ist er nicht.</summary>
        [Fact]
        public void Gesamt_ist_die_Summe_und_ein_halbes_Paar_ergibt_nichts()
        {
            using var _ = new Kulturvorrichtung();

            Assert.Equal(0.9216, BhkwWirkungsgrad.Gesamt(0.295, 0.6266), 6);

            Assert.Equal(0.92, BhkwWirkungsgrad.Gesamt((double?)0.30, (double?)0.62).Value, 6);
            Assert.Null(BhkwWirkungsgrad.Gesamt((double?)0.30, null));
            Assert.Null(BhkwWirkungsgrad.Gesamt(null, (double?)0.62));
            Assert.Null(BhkwWirkungsgrad.Gesamt(null, null));
        }

        /// <summary>
        /// Die Aufteilung im Verhältnis der Leistungen — und die Probe am Datenblatt:
        /// XRGI 15 führt 30,8 kW thermisch, 14,5 kW elektrisch und 0,9216 gesamt; die
        /// Rechnung liefert 0,295 elektrisch, also GENAU den Datenblattwert, aus dem
        /// Schritt 98 den Gesamtwirkungsgrad gewonnen hat.
        /// </summary>
        [Fact]
        public void Aufteilen_trifft_am_XRGI_15_den_Datenblattwert()
        {
            using var _ = new Kulturvorrichtung();

            BhkwWirkungsgrad.Aufteilung a = BhkwWirkungsgrad.Aufteilen(0.9216, 14.5, 30.8);

            Assert.Equal(0.295, a.El.Value, 4);
            Assert.Equal(0.6266, a.Th.Value, 4);
            Assert.Equal(0.9216, a.El.Value + a.Th.Value, 4);
        }

        /// <summary>Ohne beide Leistungen gibt es nichts zu teilen — und nichts wird erfunden.</summary>
        [Fact]
        public void Ohne_Leistung_oder_Gesamtwert_bleibt_die_Aufteilung_leer()
        {
            using var _ = new Kulturvorrichtung();

            Assert.Null(BhkwWirkungsgrad.Aufteilen(0.9, 0.0, 30.0).El);
            Assert.Null(BhkwWirkungsgrad.Aufteilen(0.9, 14.0, 0.0).Th);
            Assert.Null(BhkwWirkungsgrad.Aufteilen(0.9, null, 30.0).El);
            Assert.Null(BhkwWirkungsgrad.Aufteilen(null, 14.0, 30.0).El);
            Assert.Null(BhkwWirkungsgrad.Aufteilen(0.0, 14.0, 30.0).El);
        }

        /// <summary>
        /// Die drei Regeln samt ihren Grenzfällen: jeder Anteil in (0; 1), die Summe in
        /// (0; 1,05]. 1,05 geht durch (Brennwert), 1,051 nicht.
        /// </summary>
        [Fact]
        public void Pruefen_haelt_die_drei_Regeln_mit_ihren_Grenzfaellen()
        {
            using var _ = new Kulturvorrichtung();

            // Nichts gepflegt - der Gesamtwert bleibt, wie er ist.
            Assert.Null(BhkwWirkungsgrad.Pruefen(null, null));

            // Der Regelfall.
            Assert.Null(BhkwWirkungsgrad.Pruefen(0.30, 0.60));

            // Ein HALBES Paar geht nicht durch.
            Assert.False(string.IsNullOrEmpty(BhkwWirkungsgrad.Pruefen(0.30, null)));
            Assert.False(string.IsNullOrEmpty(BhkwWirkungsgrad.Pruefen(null, 0.60)));

            // Regel 1: der elektrische Anteil in (0; 1) - 0 und 1 fallen heraus.
            Assert.Contains("elektrische", BhkwWirkungsgrad.Pruefen(0.0, 0.60));
            Assert.Contains("elektrische", BhkwWirkungsgrad.Pruefen(1.0, 0.60));
            Assert.Contains("elektrische", BhkwWirkungsgrad.Pruefen(29.5, 0.60));
            Assert.Contains("elektrische", BhkwWirkungsgrad.Pruefen(-0.1, 0.60));

            // Regel 2: der thermische Anteil in (0; 1).
            Assert.Contains("thermische", BhkwWirkungsgrad.Pruefen(0.30, 0.0));
            Assert.Contains("thermische", BhkwWirkungsgrad.Pruefen(0.30, 1.0));

            // Regel 3: die Summe in (0; 1,05]. 0,30 + 0,80 = 1,10 ist zu viel
            // (Abnahmepunkt A-BW2-2); 1,05 geht durch, 1,051 nicht.
            Assert.False(string.IsNullOrEmpty(BhkwWirkungsgrad.Pruefen(0.30, 0.80)));
            Assert.Null(BhkwWirkungsgrad.Pruefen(0.45, 0.60));                 // genau 1,05
            Assert.False(string.IsNullOrEmpty(BhkwWirkungsgrad.Pruefen(0.451, 0.60)));  // 1,051
            Assert.Null(BhkwWirkungsgrad.Pruefen(0.30, 0.70));                 // genau 1,00
        }

        /// <summary>
        /// Der Wert, den der Schreibweg in die Spalte <c>Wirkungsgrad</c> stellt: die
        /// Summe, solange beide Anteile stehen — sonst der Altbestand, unverändert.
        /// </summary>
        [Fact]
        public void GesamtZumSchreiben_zieht_nach_oder_laesst_den_Altbestand_stehen()
        {
            using var _ = new Kulturvorrichtung();

            Assert.Equal(0.92, BhkwWirkungsgrad.GesamtZumSchreiben(0.30, 0.62, 0.11), 6);
            Assert.Equal(0.11, BhkwWirkungsgrad.GesamtZumSchreiben(null, null, 0.11), 6);
            Assert.Equal(0.11, BhkwWirkungsgrad.GesamtZumSchreiben(0.30, null, 0.11), 6);
        }

        // =============================================================================
        //  Teil 2 - der Datenteil des Schrittes
        // =============================================================================

        /// <summary>
        /// Drei synthetische Sätze: einer wird aufgeteilt, einer bleibt ohne
        /// elektrische Leistung stehen, einer ohne Gesamtwirkungsgrad. Beide
        /// stehengebliebenen werden BENANNT ausgewiesen, und der zweite Lauf fasst
        /// nichts mehr an.
        /// </summary>
        [Fact]
        public void Der_Schritt_teilt_auf_weist_aus_und_ist_wiederholbar()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            const int idTeilbar = 987654401;    // 0,90 bei 9 kWel und 20,1 kWth
            const int idOhnePel = 987654402;    // ohne elektrische Leistung
            const int idOhneEta = 987654403;    // ohne Gesamtwirkungsgrad

            try
            {
                Einfuegen(idTeilbar, "BW2 teilbar", 20.1, 9.0, 0.90);
                Einfuegen(idOhnePel, "BW2 ohne Pel", 20.0, 0.0, 0.85);
                Einfuegen(idOhneEta, "BW2 ohne Eta", 20.0, 9.0, 0.0);

                List<int> offen = Offene();
                Assert.Contains(idTeilbar, offen);
                Assert.DoesNotContain(idOhnePel, offen);
                Assert.DoesNotContain(idOhneEta, offen);

                Anweisungen_ausfuehren();

                // 0,90 * 9 / 29,1 = 0,2784; 0,90 * 20,1 / 29,1 = 0,6216; Summe 0,90.
                Assert.Equal(0.2784, Anteil(idTeilbar, BhkwWirkungsgrad.SPALTE_EL).Value, 4);
                Assert.Equal(0.6216, Anteil(idTeilbar, BhkwWirkungsgrad.SPALTE_TH).Value, 4);
                Assert.Equal(0.90,
                             Anteil(idTeilbar, BhkwWirkungsgrad.SPALTE_EL).Value +
                             Anteil(idTeilbar, BhkwWirkungsgrad.SPALTE_TH).Value, 4);

                // Die zwei anderen bleiben leer - nichts wird erfunden.
                Assert.Null(Anteil(idOhnePel, BhkwWirkungsgrad.SPALTE_EL));
                Assert.Null(Anteil(idOhneEta, BhkwWirkungsgrad.SPALTE_TH));

                // ... und sind BENANNT ausgewiesen, mit Id, Namen und Grund.
                List<BhkwWirkungsgradAnteile.Ausweis> aus =
                    BhkwWirkungsgradAnteile.Ausgewiesene(BhkwWirkungsgradAnteile.TAB_STAMM);

                BhkwWirkungsgradAnteile.Ausweis ohnePel = aus.FirstOrDefault(a => a.Id == idOhnePel);
                Assert.NotNull(ohnePel);
                Assert.Equal("BW2 ohne Pel", ohnePel.Bezeichner);
                Assert.Contains("elektrische Leistung", ohnePel.Grund);
                Assert.Contains("BW2 ohne Pel", ohnePel.Zeile());

                BhkwWirkungsgradAnteile.Ausweis ohneEta = aus.FirstOrDefault(a => a.Id == idOhneEta);
                Assert.NotNull(ohneEta);
                Assert.Contains("Gesamtwirkungsgrad", ohneEta.Grund);

                // Der aufgeteilte Satz steht NICHT mehr in der Ausweisung.
                Assert.DoesNotContain(aus, a => a.Id == idTeilbar);

                // WIEDERHOLBAR: Der zweite Lauf findet nichts mehr und aendert nichts.
                Assert.DoesNotContain(idTeilbar, Offene());
                Anweisungen_ausfuehren();
                Assert.Equal(0.2784, Anteil(idTeilbar, BhkwWirkungsgrad.SPALTE_EL).Value, 4);

                // Eine von Hand gepflegte Aufteilung bleibt unberuehrt - auch wenn sie
                // nicht dem Verhaeltnis der Leistungen folgt.
                DataRepository.ExecuteNonQuery(
                    "UPDATE [Tab_BHKW_STAMM] SET [Wirkungsgrad_el] = ?, [Wirkungsgrad_th] = ? WHERE [ID] = ?",
                    new DbParam("@el", 0.31), new DbParam("@th", 0.59), new DbParam("@id", idTeilbar));
                Anweisungen_ausfuehren();
                Assert.Equal(0.31, Anteil(idTeilbar, BhkwWirkungsgrad.SPALTE_EL).Value, 4);
            }
            finally
            {
                foreach (int id in new[] { idTeilbar, idOhnePel, idOhneEta })
                    DataRepository.ExecuteNonQuery(
                        "DELETE FROM [Tab_BHKW_STAMM] WHERE [ID] = ?", new DbParam("@id", id));
            }
        }

        /// <summary>
        /// <b>Die Probe des Schrittes</b> an der Arbeitskopie: Sie steht auf 99, der
        /// Schritt findet nichts mehr, und in JEDER aufgeteilten Zeile beider Tabellen
        /// ergeben die zwei Anteile den Gesamtwirkungsgrad — auf 1e-4 genau.
        /// </summary>
        [Fact]
        public void Die_Arbeitskopie_steht_auf_99_und_jede_Summe_trifft()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            foreach (string t in BhkwWirkungsgradAnteile.Tabellen)
            {
                Assert.True(BhkwWirkungsgradAnteile.Vorhanden(t), t + " fuehrt die zwei Spalten nicht.");
                Assert.Equal(0, BhkwWirkungsgradAnteile.Offen(t));
                Assert.Equal(0, BhkwWirkungsgradAnteile.SummeWeichtAb(t));
                Assert.True(BhkwWirkungsgradAnteile.Aufgeteilt(t) > 0,
                            t + " fuehrt keine einzige aufgeteilte Zeile.");
            }

            Assert.Equal(0, BhkwWirkungsgradAnteile.OffenGesamt());

            // Und Zeile fuer Zeile, ohne SQL-Kunstgriff.
            foreach (string t in BhkwWirkungsgradAnteile.Tabellen)
            {
                DataTable dt = DataRepository.GetDataTable(
                    "SELECT [ID], [Wirkungsgrad], [Wirkungsgrad_el], [Wirkungsgrad_th] FROM [" + t + "] " +
                    "WHERE [Wirkungsgrad_el] IS NOT NULL ORDER BY [ID]");
                Assert.NotNull(dt);

                foreach (DataRow r in dt.Rows)
                {
                    double gesamt = System.Convert.ToDouble(r["Wirkungsgrad"], CultureInfo.InvariantCulture);
                    double el = System.Convert.ToDouble(r["Wirkungsgrad_el"], CultureInfo.InvariantCulture);
                    double th = System.Convert.ToDouble(r["Wirkungsgrad_th"], CultureInfo.InvariantCulture);

                    // TOLERANZ 1e-4, nicht "vier Stellen": Beide Anteile sind je fuer
                    // sich auf vier Stellen gerundet, und zwei Rundungen tragen zusammen
                    // bis zu 1e-4 - genau die Schranke, gegen die auch SummeWeichtAb
                    // haelt.
                    Assert.True(System.Math.Abs(gesamt - BhkwWirkungsgrad.Gesamt(el, th)) <= 1e-4,
                                t + " Id " + r["ID"] + ": " + el + " + " + th + " != " + gesamt);
                    Assert.True(el > 0 && el < 1, t + " Id " + r["ID"] + ": el = " + el);
                    Assert.True(th > 0 && th < 1, t + " Id " + r["ID"] + ": th = " + th);
                }
            }
        }

        /// <summary>
        /// Der Katalogsatz des XRGI 15 trägt nach dem Schritt 0,295 elektrisch — den
        /// Datenblattwert, aus dem Schritt 98 seinen Gesamtwirkungsgrad gewonnen hat.
        /// Die Aufteilung ist damit am Bestand belegt, nicht nur an der Formel.
        /// </summary>
        [Fact]
        public void Der_Bestandssatz_des_XRGI_15_traegt_0_295_elektrisch()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            BHKWStammModel m = new BHKWStammCtrl().ReadModel(XRGI15);
            if (m == null) return;

            Assert.Equal(0.9216, m.m_Wirkungsgrad, 4);
            Assert.Equal(0.295, m.m_Wirkungsgrad_el.Value, 4);
            Assert.Equal(0.6266, m.m_Wirkungsgrad_th.Value, 4);
            Assert.Equal(m.m_Wirkungsgrad,
                         BhkwWirkungsgrad.Gesamt(m.m_Wirkungsgrad_el, m.m_Wirkungsgrad_th).Value, 4);
        }

        /// <summary>
        /// Die Bestandsaufnahme wird VOR dem Schreiben gezogen; der Bericht nennt je
        /// Tabelle aufgeteilt, ausgewiesen und schon aufgeteilt.
        /// </summary>
        [Fact]
        public void Die_Bestandsaufnahme_traegt_Zaehlung_und_Ausweisung()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            BhkwWirkungsgradAnteile.Aufnahme a = BhkwWirkungsgradAnteile.Bestandsaufnahme();

            foreach (string t in BhkwWirkungsgradAnteile.Tabellen)
            {
                Assert.True(a.Gesamt.ContainsKey(t));
                Assert.Equal(0, a.Aufzuteilen[t]);       // die Kopie ist nachgezogen
            }

            string bericht = BhkwWirkungsgradAnteile.Bericht(a);
            Assert.Contains("Tab_BHKW_STAMM:", bericht);
            Assert.Contains("aufgeteilt", bericht);
        }

        // =============================================================================
        //  Teil 3 - die Schreibwege des Kerns
        // =============================================================================

        /// <summary>
        /// <b>Eine Wahrheit:</b> <c>BHKWStammCtrl.Update</c> schreibt den
        /// Gesamtwirkungsgrad IMMER als Summe der zwei Anteile — auch wenn im Modell ein
        /// anderer Wert stünde. Ohne gepflegte Anteile (Altbestand) bleibt er stehen.
        /// </summary>
        [Fact]
        public void Das_Speichern_zieht_den_Gesamtwirkungsgrad_aus_den_Anteilen_nach()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            const int id = 987654411;

            try
            {
                Einfuegen(id, "BW2 Schreibweg", 20.0, 10.0, 0.11);

                var ctrl = new BHKWStammCtrl();
                BHKWStammModel m = ctrl.ReadModel("BW2 Schreibweg");
                Assert.NotNull(m);

                // Beide Anteile gepflegt: der Gesamtwert wird nachgezogen, der im
                // Modell stehende 0,11 zaehlt nicht.
                m.m_Wirkungsgrad_el = 0.30;
                m.m_Wirkungsgrad_th = 0.62;
                Assert.True(new BHKWStammCtrl { model = m }.Update());

                BHKWStammModel neu = new BHKWStammCtrl().ReadModel("BW2 Schreibweg");
                Assert.Equal(0.92, neu.m_Wirkungsgrad, 4);
                Assert.Equal(0.30, neu.m_Wirkungsgrad_el.Value, 4);
                Assert.Equal(0.62, neu.m_Wirkungsgrad_th.Value, 4);

                // ALTBESTAND: ohne Anteile bleibt der Gesamtwert stehen, wie er kommt -
                // der Rechenweg liest ihn unveraendert weiter.
                neu.m_Wirkungsgrad_el = null;
                neu.m_Wirkungsgrad_th = null;
                neu.m_Wirkungsgrad = 0.77;
                Assert.True(new BHKWStammCtrl { model = neu }.Update());

                BHKWStammModel alt = new BHKWStammCtrl().ReadModel("BW2 Schreibweg");
                Assert.Equal(0.77, alt.m_Wirkungsgrad, 4);
                Assert.Null(alt.m_Wirkungsgrad_el);
                Assert.Null(alt.m_Wirkungsgrad_th);
            }
            finally
            {
                DataRepository.ExecuteNonQuery(
                    "DELETE FROM [Tab_BHKW_STAMM] WHERE [ID] = ?", new DbParam("@id", id));
            }
        }

        /// <summary>
        /// <b>Die Katalogübernahme</b> (Katalog → Projekt) kopiert beide Anteile mit —
        /// eine Spalte nur auf der Katalogseite wäre hier sofort ein Datenverlust.
        /// </summary>
        [Fact]
        public void Die_Kataloguebernahme_kopiert_beide_Anteile_ins_Projekt()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            const int idStamm = 987654421;
            const string name = "BW2 Uebernahme";

            int idProjekt = ErstesProjekt();
            if (idProjekt <= 0) return;

            int neueId = -1;

            try
            {
                Einfuegen(idStamm, name, 20.0, 10.0, 0.90);
                DataRepository.ExecuteNonQuery(
                    "UPDATE [Tab_BHKW_STAMM] SET [Wirkungsgrad_el] = ?, [Wirkungsgrad_th] = ? WHERE [ID] = ?",
                    new DbParam("@el", 0.30), new DbParam("@th", 0.60), new DbParam("@id", idStamm));

                neueId = new BHKWCtrl().CopyFromStamm(idStamm, idProjekt);
                Assert.True(neueId > 0, "Die Uebernahme hat keine Projektzeile angelegt.");

                DataTable dt = DataRepository.GetDataTable(
                    "SELECT [Wirkungsgrad], [Wirkungsgrad_el], [Wirkungsgrad_th] FROM [Tab_BHKW] WHERE [ID] = ?",
                    new DbParam("@id", neueId));
                Assert.NotNull(dt);
                Assert.Single(dt.Rows);

                Assert.Equal(0.90, System.Convert.ToDouble(dt.Rows[0]["Wirkungsgrad"], CultureInfo.InvariantCulture), 4);
                Assert.Equal(0.30, System.Convert.ToDouble(dt.Rows[0]["Wirkungsgrad_el"], CultureInfo.InvariantCulture), 4);
                Assert.Equal(0.60, System.Convert.ToDouble(dt.Rows[0]["Wirkungsgrad_th"], CultureInfo.InvariantCulture), 4);
            }
            finally
            {
                if (neueId > 0)
                    DataRepository.ExecuteNonQuery(
                        "DELETE FROM [Tab_BHKW] WHERE [ID] = ?", new DbParam("@id", neueId));
                DataRepository.ExecuteNonQuery(
                    "DELETE FROM [Tab_BHKW_STAMM] WHERE [ID] = ?", new DbParam("@id", idStamm));
            }
        }

        /// <summary>
        /// <b>Der Rückfall des Altbestands:</b> Ein Satz ohne Aufteilung liefert über
        /// <see cref="BhkwWirkungsgrad.Aufteilen"/> einen Vorschlag, sobald seine
        /// Leistungen gepflegt sind — gespeichert wird er erst beim Speichern.
        /// </summary>
        [Fact]
        public void Ein_Altbestandssatz_bekommt_einen_Vorschlag_aber_keinen_Eintrag()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            const int id = 987654431;

            try
            {
                Einfuegen(id, "BW2 Altbestand", 20.1, 9.0, 0.90);   // ohne Anteile

                BHKWStammModel m = new BHKWStammCtrl().ReadModel("BW2 Altbestand");
                Assert.Null(m.m_Wirkungsgrad_el);
                Assert.Null(m.m_Wirkungsgrad_th);

                BhkwWirkungsgrad.Aufteilung vorschlag =
                    BhkwWirkungsgrad.Aufteilen(m.m_Wirkungsgrad, m.m_Pel, m.m_Ptherm);
                Assert.Equal(0.2784, vorschlag.El.Value, 4);
                Assert.Equal(0.6216, vorschlag.Th.Value, 4);

                // Der Vorschlag steht NICHT in der Datenbank - er ist ein Angebot.
                Assert.Null(Anteil(id, BhkwWirkungsgrad.SPALTE_EL));
            }
            finally
            {
                DataRepository.ExecuteNonQuery(
                    "DELETE FROM [Tab_BHKW_STAMM] WHERE [ID] = ?", new DbParam("@id", id));
            }
        }

        // =============================================================================
        //  Teil 6 (BW-3) - der Aufklapper "Alle Daten" zeigt den Gesamtwert nur noch
        // =============================================================================

        /// <summary>
        /// <b>Anwenderentscheid vom 20.09.2026:</b> Im Aufklapper „Alle Daten anzeigen"
        /// ist der Gesamtwirkungsgrad reine ANZEIGE — eingegeben werden der elektrische
        /// und der thermische Anteil, wie im Katalogeditor.
        /// </summary>
        /// <remarks>
        /// Geprüft wird die STRUKTUR: Das Profil führt die zwei Anteile editierbar und
        /// den Gesamtwert nicht, und der Datensatz des Speicherwegs nimmt einen
        /// Gesamtwert gar nicht erst entgegen — es gibt keinen Weg mehr, ihn zu
        /// liefern, und damit auch keinen Verteilungsweg, der eine gepflegte Aufteilung
        /// durch eine geschätzte ersetzte.
        /// </remarks>
        [Fact]
        public void Der_Aufklapper_nimmt_den_Gesamtwirkungsgrad_nicht_mehr_entgegen()
        {
            using var _ = new Kulturvorrichtung();

            KatalogBrowserProfil profil = KatalogBrowserProfil.Finde(KatalogBrowserArt.Bhkw);

            BrowserDetailfeld gesamt = profil.Detailfelder.Single(
                f => f.Schluessel == KatalogBrowserProfil.FeldWirkungsgrad);
            BrowserDetailfeld el = profil.Detailfelder.Single(
                f => f.Schluessel == KatalogBrowserProfil.FeldWirkungsgradEl);
            BrowserDetailfeld th = profil.Detailfelder.Single(
                f => f.Schluessel == KatalogBrowserProfil.FeldWirkungsgradTh);

            Assert.False(gesamt.Editierbar);
            Assert.True(el.Editierbar);
            Assert.True(th.Editierbar);

            // Die zwei Anteile stehen VOR der Summe - erst die Eingabe, dann, was
            // daraus folgt.
            var schluessel = profil.Detailfelder.Select(f => f.Schluessel).ToList();
            Assert.True(schluessel.IndexOf(KatalogBrowserProfil.FeldWirkungsgradEl)
                        < schluessel.IndexOf(KatalogBrowserProfil.FeldWirkungsgrad));

            var namen = typeof(BHKWStammCtrl.AnzeigefelderBhkw)
                        .GetConstructors().Single()
                        .GetParameters().Select(p => p.Name).ToList();
            Assert.DoesNotContain("Wirkungsgrad", namen);
            Assert.Contains("WirkungsgradEl", namen);
            Assert.Contains("WirkungsgradTh", namen);
        }

        /// <summary>
        /// Der Speicherweg des Aufklappers schreibt den Gesamtwirkungsgrad als
        /// <c>Gesamt(el, th)</c> — und die Anzeige zeigt dieselbe Summe, auf drei
        /// Stellen.
        /// </summary>
        [Fact]
        public void Der_Aufklapper_schreibt_den_Gesamtwirkungsgrad_als_Summe()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            const int id = 987654431;
            const string name = "BW3 Aufklapper";

            try
            {
                Einfuegen(id, name, 30.0, 15.0, 0.50);

                var felder = new BHKWStammCtrl.AnzeigefelderBhkw(
                    "Probe GmbH", 30.0, 15.0, 50, 80, 60,
                    WirkungsgradEl: 0.31, WirkungsgradTh: 0.55);

                BHKWStammCtrl.SpeicherErgebnis e =
                    BHKWStammCtrl.AnzeigefelderSchreiben(name, felder, false);
                Assert.True(e.Ok, e.Meldung);

                // Die Spalte traegt die SUMME, nicht den alten Wert 0,50.
                Assert.Equal(0.86, Gesamtwert(id), 4);
                Assert.Equal(0.31, Anteil(id, BhkwWirkungsgrad.SPALTE_EL).Value, 4);
                Assert.Equal(0.55, Anteil(id, BhkwWirkungsgrad.SPALTE_TH).Value, 4);

                // Und die Anzeige zeigt sie - drei Stellen, wie im Katalogeditor.
                var satz = BHKWStammCtrl.KatalogsatzAnzeige(name);
                Assert.NotNull(satz);
                Assert.Equal("0,86", satz[KatalogBrowserProfil.FeldWirkungsgrad]);

                // Ein Anteil ausserhalb des Bandes wird BENANNT abgewiesen, und der
                // Satz bleibt stehen, wie er war.
                var abgelehnt = new BHKWStammCtrl.AnzeigefelderBhkw(
                    "Probe GmbH", 30.0, 15.0, 50, 80, 60,
                    WirkungsgradEl: 55.0, WirkungsgradTh: 0.55);
                BHKWStammCtrl.SpeicherErgebnis nein =
                    BHKWStammCtrl.AnzeigefelderSchreiben(name, abgelehnt, false);
                Assert.False(nein.Ok);
                Assert.Contains("Faktor", nein.Meldung);
                Assert.Equal(0.86, Gesamtwert(id), 4);
            }
            finally
            {
                DataRepository.ExecuteNonQuery(
                    "DELETE FROM [Tab_BHKW_STAMM] WHERE [ID] = ?", new DbParam("@id", id));
            }
        }

        /// <summary>
        /// <b>Ein Altbestandssatz ohne Aufteilung behält seinen Gesamtwirkungsgrad</b>:
        /// Wer im Aufklapper ein anderes Feld pflegt, lässt ihn stehen, und die Anzeige
        /// zeigt weiterhin den gespeicherten Wert.
        /// </summary>
        [Fact]
        public void Ein_Altbestandssatz_behaelt_seinen_Gesamtwirkungsgrad()
        {
            if (!_db.Vorhanden) return;
            using var _ = new Kulturvorrichtung();

            const int id = 987654432;
            const string name = "BW3 Altbestand";

            try
            {
                Einfuegen(id, name, 30.0, 15.0, 0.9216);

                var satz = BHKWStammCtrl.KatalogsatzAnzeige(name);
                Assert.NotNull(satz);
                Assert.Equal("", satz[KatalogBrowserProfil.FeldWirkungsgradEl]);
                Assert.Equal("", satz[KatalogBrowserProfil.FeldWirkungsgradTh]);
                Assert.Equal("0,922", satz[KatalogBrowserProfil.FeldWirkungsgrad]);

                var felder = new BHKWStammCtrl.AnzeigefelderBhkw(
                    "Probe GmbH", 30.0, 15.0, 50, 80, 60, Motortyp: "Ottomotor");

                BHKWStammCtrl.SpeicherErgebnis e =
                    BHKWStammCtrl.AnzeigefelderSchreiben(name, felder, false);
                Assert.True(e.Ok, e.Meldung);

                Assert.Equal(0.9216, Gesamtwert(id), 4);
                Assert.Null(Anteil(id, BhkwWirkungsgrad.SPALTE_EL));
                Assert.Null(Anteil(id, BhkwWirkungsgrad.SPALTE_TH));
            }
            finally
            {
                DataRepository.ExecuteNonQuery(
                    "DELETE FROM [Tab_BHKW_STAMM] WHERE [ID] = ?", new DbParam("@id", id));
            }
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

        /// <summary>Der Gesamtwirkungsgrad einer Katalogzeile.</summary>
        private static double Gesamtwert(int id)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT [Wirkungsgrad] FROM [Tab_BHKW_STAMM] WHERE [ID] = ?",
                new DbParam("@id", id));
            return (o == null || o == System.DBNull.Value)
                 ? 0.0
                 : System.Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Ein Anteil der Katalogzeile; <c>null</c> heisst „nicht gepflegt".</summary>
        private static double? Anteil(int id, string spalte)
        {
            object o = DataRepository.ExecuteScalar(
                "SELECT [" + spalte + "] FROM [Tab_BHKW_STAMM] WHERE [ID] = ?",
                new DbParam("@id", id));
            if (o == null || o == System.DBNull.Value) return null;
            return System.Convert.ToDouble(o, CultureInfo.InvariantCulture);
        }

        /// <summary>Die Ids, die der Schritt gerade aufteilen würde.</summary>
        private static List<int> Offene()
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT [ID] FROM [Tab_BHKW_STAMM] WHERE [Wirkungsgrad_el] IS NULL " +
                "AND [Wirkungsgrad_th] IS NULL AND [Wirkungsgrad] > ? AND [Pel] > ? AND [Ptherm] > ?",
                BhkwWirkungsgradAnteile.ParameterZaehlen());

            var ids = new List<int>();
            if (t == null) return ids;
            foreach (DataRow r in t.Rows)
                ids.Add(System.Convert.ToInt32(r["ID"], CultureInfo.InvariantCulture));
            return ids;
        }

        private static void Anweisungen_ausfuehren()
        {
            foreach (KeyValuePair<string, BhkwWirkungsgradFaktor.Anweisung> a
                     in BhkwWirkungsgradAnteile.Anweisungen)
                DataRepository.ExecuteNonQuery(a.Value.Sql, a.Value.Parameter);
        }

        /// <summary>Irgendein vorhandenes Projekt — die Projektzeile braucht einen Anker.</summary>
        private static int ErstesProjekt()
        {
            object o = DataRepository.ExecuteScalar("SELECT MIN([ID]) FROM [Tab_Projekt]");
            if (o == null || o == System.DBNull.Value) return 0;
            return System.Convert.ToInt32(o, CultureInfo.InvariantCulture);
        }
    }
}
