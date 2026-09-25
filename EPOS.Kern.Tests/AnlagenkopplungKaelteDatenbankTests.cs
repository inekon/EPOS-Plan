using System;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kälteseite der Anlagenkopplung mit Datenbank</b> (E37; Anlagenkopplung 7.2, 8.3,
    /// 8.4, 9.5, H7) — auf einer Arbeitskopie der Testdatenbank, Projekt 1017 (ein Gebäude auf dem
    /// VDI-Weg mit wirksamer Kühlung, eine Wärmepumpe im Kühlbetrieb mit <c>Kuehl_Vorlauf</c>
    /// 18 °C): der Kaltwasser-Vorlauf des Kältekanals, Grenzfall A mit Datenbank und Hinweisen,
    /// der kühlgekoppelte Lauf mit den Ergebnisspalten (KAK-S3), das Hochmischen zu kalten
    /// Kaltwassers, beide Läufe der Verhältnisrechnung mit fester Nennleistung und die Auskunft
    /// des Gebäudedialogs.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AnlagenkopplungKaelteDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public AnlagenkopplungKaelteDatenbankTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1017;
        private const int GEBAEUDE = 10599;       // das Gebäude von 1017 (VDI-Weg, Kühlung wirksam, 24 °C)
        private const int PROJEKT_OHNE_KAELTE = 1045;

        private static void Kuehlkoppeln(bool schalter = true, string art = DbWerte.KUEHLUEBERGABE_KUEHLDECKE)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Kuehluebergabe_Aktiv = ?, Kuehl_Uebergabe_Art = ? WHERE ID = ?",
                new DbParam("@s", schalter ? 1 : 0), new DbParam("@art", (object)art ?? DBNull.Value), new DbParam("@id", GEBAEUDE)));
        }

        private static int Klimaregion(int idProjekt)
        {
            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(idProjekt);
            return ctrl.m_ID_Klimaregion;
        }

        private static SimulationWaermebedarf Bedarf(int idProjekt)
        {
            SimulationProtokoll.NeuStarten();
            var sim = new SimulationWaermebedarf();
            sim.Waermebedarf_berechnen(idProjekt, Klimaregion(idProjekt));
            return sim;
        }

        private static DataRow Zeile(string tabelle, int kopfId)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM " + tabelle + " WHERE ID_Ergebnis = ?", new DbParam("@k", kopfId));
            Assert.NotNull(dt);
            return Assert.Single(dt.Rows.Cast<DataRow>());
        }

        // =====================================================================
        //  Der Kaltwasser-Vorlauf des Kältekanals (7.2, WPCtrl)
        // =====================================================================

        /// <summary>
        /// Der kälteste wirksame <c>Kuehl_Vorlauf</c> der Wärmepumpen im Kühlbetrieb; NULL heißt der
        /// kleinste Stützwert der Kühlkennlinie (K21); ohne Kälteerzeuger NaN.
        /// </summary>
        [Fact]
        public void Der_Kaltwasser_Vorlauf_des_Kaeltekanals()
        {
            if (!_db.Vorhanden) return;
            Assert.Equal(18.0, WPCtrl.KuehlVorlaufDesKaeltekanals(PROJEKT));
            Assert.True(double.IsNaN(WPCtrl.KuehlVorlaufDesKaeltekanals(PROJEKT_OHNE_KAELTE)));
            Assert.True(double.IsNaN(WPCtrl.KuehlVorlaufDesKaeltekanals(0)));

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_WP SET Kuehl_Vorlauf = NULL WHERE ID IN (SELECT a.ID_WP FROM Tab_Energieanlagen a WHERE a.ID_Projekt = ?)",
                new DbParam("@p", PROJEKT)));
            object stuetz = DataRepository.ExecuteScalar(
                "SELECT MIN(k.Vorlauf) FROM Tab_Kenndaten_Kuehlung k JOIN Tab_Energieanlagen a ON a.ID_WP = k.ID_WP WHERE a.ID_Projekt = ?",
                new DbParam("@p", PROJEKT));
            Assert.Equal(Convert.ToDouble(stuetz), WPCtrl.KuehlVorlaufDesKaeltekanals(PROJEKT));
        }

        // =====================================================================
        //  Grenzfall A mit Datenbank (F-A17, 9.5)
        // =====================================================================

        /// <summary>
        /// Stufe AK1 und Kühldecke, aber der Schalter aus — oder Schalter an und Stufe aus: kein
        /// Unterschied in den Reihen, Bit für Bit, und im zweiten Fall nennt der Lauf, dass die
        /// Eingaben der Kühlübergabe ruhen.
        /// </summary>
        [Fact]
        public void Ohne_wirksame_Kaelteseite_aendert_sich_keine_Zahl_und_der_Lauf_nennt_es()
        {
            if (!_db.Vorhanden) return;
            SimulationWaermebedarf vorher = Bedarf(PROJEKT);
            double[] heizVorher = (double[])vorher.Waermebedarf_Gebaeude.Clone();
            double[] kuehlVorher = (double[])vorher.GebaeudeErgebnisse.Ergebnis(0).KuehlbedarfKwh.Clone();

            Kuehlkoppeln(schalter: false);
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));
            SimulationWaermebedarf ohneSchalter = Bedarf(PROJEKT);
            Assert.Null(ohneSchalter.Kuehlkreis);
            Assert.Null(ohneSchalter.GebaeudeErgebnisse.Ergebnis(0).Kuehlkreis);
            for (int h = 0; h < 8760; h++)
            {
                Assert.True(heizVorher[h].Equals(ohneSchalter.Waermebedarf_Gebaeude[h]), "Heizung, Stunde " + h);
                Assert.True(kuehlVorher[h].Equals(ohneSchalter.GebaeudeErgebnisse.Ergebnis(0).KuehlbedarfKwh[h]), "Kälte, Stunde " + h);
            }

            Kuehlkoppeln(schalter: true);
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, null));
            SimulationWaermebedarf ohneStufe = Bedarf(PROJEKT);
            Assert.Null(ohneStufe.Kuehlkreis);
            for (int h = 0; h < 8760; h++)
                Assert.True(kuehlVorher[h].Equals(ohneStufe.GebaeudeErgebnisse.Ergebnis(0).KuehlbedarfKwh[h]), "Kälte, Stunde " + h);
            Assert.Contains(SimulationProtokoll.Aktuell.Hinweise, t => t.Contains("Eingaben der Kühlübergabe ruhen"));
        }

        // =====================================================================
        //  Der kühlgekoppelte Lauf (8.3, KAK-S3)
        // =====================================================================

        /// <summary>
        /// <b>Der kühlgekoppelte Lauf</b> (Probe an 1017 mit Kühldecke und Vorgaben): Ohne Kopplung
        /// stehen die Spalten aus KAK-S3 auf NULL; mit Stufe AK1 und Kühlübergabe tragen sie die
        /// Zahlen des Kältekreises — je Projekt und je Gebäude —, und das Laden liest sie zurück.
        /// Die Wärmepumpe rechnet weiter am <c>Kuehl_Vorlauf</c> (7.4 Punkt 5).
        /// </summary>
        [Fact]
        public void Der_kuehlgekoppelte_Lauf_schreibt_die_Ergebnisspalten()
        {
            if (!_db.Vorhanden) return;
            var bestand = new SimulationRunner();
            int kopfBestand = bestand.SimuliereUndSpeichere(PROJEKT, out string fehlerBestand);
            Assert.True(kopfBestand > 0, fehlerBestand);
            DataRow ohne = Zeile("Tab_ErgebnisEnergiebedarf", kopfBestand);
            foreach (SchemaSpalte s in KuehluebergabeSchema.Ergebnisspalten)
                Assert.Equal(DBNull.Value, ohne[s.Name]);
            DataRow ohneGeb = Zeile(ErgebnisGebaeudeSchema.TAB, kopfBestand);
            foreach (var s in KuehluebergabeSchema.SpaltenKuehlkreis)
                Assert.Equal(DBNull.Value, ohneGeb[s.Key]);
            GebaeudeModellErgebnis ideal = bestand.simulation_Waermebedarf.GebaeudeErgebnisse.Ergebnis(0);

            Kuehlkoppeln();
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, fehler);

            KuehlkreisProjekt kp = lauf.simulation_Waermebedarf.Kuehlkreis;
            Assert.NotNull(kp);
            Assert.Null(lauf.simulation_Waermebedarf.Heizkreis);
            GebaeudeModellErgebnis r = lauf.simulation_Waermebedarf.GebaeudeErgebnisse.Ergebnis(0);
            KuehlkreisErgebnis kk = r.Kuehlkreis;
            Assert.NotNull(kk);
            Assert.Equal(18.0, kk.VorlaufFestC);
            Assert.Equal(Vorlaufquelle.Anlage, kk.Vorlaufquelle);
            Assert.False(kk.VorlaufGekappt);

            DataRow mit = Zeile("Tab_ErgebnisEnergiebedarf", kopf);
            Assert.Equal(Math.Round(kp.VorlaufMittelC, 2, MidpointRounding.AwayFromZero), Convert.ToDouble(mit[KuehluebergabeSchema.SPALTE_KUEHL_VORLAUF_MITTEL]));
            Assert.Equal(Math.Round(kp.RuecklaufMittelC, 2, MidpointRounding.AwayFromZero), Convert.ToDouble(mit[KuehluebergabeSchema.SPALTE_KUEHL_RUECKLAUF_MITTEL]));
            Assert.Equal(Math.Round(kp.UebergabeBegrenztStundenH, 2, MidpointRounding.AwayFromZero), Convert.ToDouble(mit[KuehluebergabeSchema.SPALTE_KUEHL_UEBERGABE_BEGRENZT_STUNDEN]));
            DataRow geb = Zeile(ErgebnisGebaeudeSchema.TAB, kopf);
            Assert.Equal(DbWerte.KUEHLUEBERGABE_KUEHLDECKE, Convert.ToString(geb[KuehluebergabeSchema.SPALTE_ERGEBNIS_KUEHL_UEBERGABE_ART]));
            Assert.Equal(kk.VorlaufMittelC, Convert.ToDouble(geb[KuehluebergabeSchema.SPALTE_KUEHL_VORLAUF_MITTEL_C]));
            Assert.Equal(kk.VorlaufgrenzeStundenH, Convert.ToDouble(geb[KuehluebergabeSchema.SPALTE_KUEHL_VORLAUFGRENZE_H]));
            Assert.Equal(DBNull.Value, geb[ErgebnisGebaeudeSchema.SPALTE_UEBERGABE_ART]);

            ErgebnisModel gelesen = new ErgebnisCtrl().Load(PROJEKT);
            Assert.Equal(kopf, gelesen.ID);
            Assert.NotNull(gelesen.Energiebedarf.KuehlVorlaufMittelC);
            ErgebnisGebaeudeModel g = Assert.Single(gelesen.Gebaeude);
            Assert.True(g.IstKuehlgekoppelt);
            Assert.False(g.IstGekoppelt);
            Assert.Equal(kk.UebergabeBegrenztStundenH, g.KuehlUebergabeBegrenztStundenH);

            _aus.WriteLine($"1017 Kühldecke (Vorgaben): Kältebedarf {ideal.KuehlenergieMwh:0.000} → {r.KuehlenergieMwh:0.000} MWh; " +
                           $"Spitze {ideal.KuehlbedarfKwh.Max():0.00} → {r.KuehlbedarfKwh.Max():0.00} kW; " +
                           $"Kühlvorlauf {kk.VorlaufMittelC:0.00} °C, Kühlrücklauf {kk.RuecklaufMittelC:0.00} °C; " +
                           $"begrenzt {kk.UebergabeBegrenztStundenH:0.0} h, davon Vorlaufgrenze {kk.VorlaufgrenzeStundenH:0.0} h, " +
                           $"Kuehlleistung_Max {kk.KuehlleistungMaxStundenH:0.0} h; Überhitzung {ideal.Ueberhitzungsstunden} → {r.Ueberhitzungsstunden} h; " +
                           $"Nennleistung {kk.UebergabeNennKw:0.00} kW aus dem Auslegungstag {GebaeudeModellEingang.TagText(kk.AuslegungstagKuehlung, System.Globalization.CultureInfo.GetCultureInfo("de-DE"))}");
            Assert.True(r.KuehlenergieMwh <= ideal.KuehlenergieMwh);
        }

        /// <summary>
        /// Kaltwasser kälter als nötig (7.2): <c>Kuehl_Vorlauf</c> leer heißt der kleinste Stützwert;
        /// die Kühldecke mischt auf ihre Grenze 16 °C hoch, und der Lauf sagt es. Der Grund
        /// „Vorlaufgrenze" steht nur in gesättigten Stunden.
        /// </summary>
        [Fact]
        public void Kaltwasser_kaelter_als_noetig_wird_hochgemischt_und_benannt()
        {
            if (!_db.Vorhanden) return;
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_WP SET Kuehl_Vorlauf = NULL WHERE ID IN (SELECT a.ID_WP FROM Tab_Energieanlagen a WHERE a.ID_Projekt = ?)",
                new DbParam("@p", PROJEKT)));
            Kuehlkoppeln();
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));
            SimulationWaermebedarf sim = Bedarf(PROJEKT);
            KuehlkreisErgebnis kk = sim.GebaeudeErgebnisse.Ergebnis(0).Kuehlkreis;
            Assert.NotNull(kk);
            Assert.True(kk.VorlaufGekappt);
            Assert.Equal(16.0, kk.VorlaufFestC);
            Assert.True(kk.VorlaufQuelleC < 16.0);
            Assert.True(kk.VorlaufgrenzeStundenH <= kk.UebergabeBegrenztStundenH);
            Assert.Contains(SimulationProtokoll.Aktuell.Hinweise, t => t.Contains("Kaltwasser kälter als nötig"));
            Assert.Contains(SimulationProtokoll.Aktuell.Hinweise, t => t.Contains("ohne Entfeuchtung"));
            Assert.Contains(SimulationProtokoll.Aktuell.Hinweise, t => t.Contains("Auslegungstags"));
        }

        // =====================================================================
        //  H7 — die feste Nennleistung der Kälteseite (Fassade)
        // =====================================================================

        private static ProjektGebaeudeModel Gebaeudezeile(int idProjekt)
        {
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            return ctrl.items[0];
        }

        private static SimulationWaermebedarf Fassade(int idProjekt)
        {
            SimulationProtokoll.NeuStarten();
            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.KlimakalenderLesen(Klimaregion(idProjekt));
            sim.AnlagenkopplungProjekt = DbWerte.ANLAGENKOPPLUNG_AK1;
            sim.KuehlbetriebProjekt = true;
            sim.KuehlVorlaufAnlageC = 18.0;
            return sim;
        }

        /// <summary>
        /// <b>H7 auf der Kälteseite</b>: Hergeleitet bleibt es EIN Lauf je Gebäude — Heiz- und
        /// Kältereihe aus diesem einen Lauf („ein Lauf, zwei Reihen", E21). Eine fest eingetragene
        /// Nennleistung der Kühlübergabe gilt dem wirklichen Gebäude: bei Verbrauchsangabe zwei
        /// Läufe, und das wirkliche Gebäude trägt genau die eingetragene Leistung; bei Flächenangabe
        /// ein Lauf.
        /// </summary>
        [Fact]
        public void H7_Die_feste_Nennleistung_der_Kaelteseite_rechnet_hoechstens_zwei_Laeufe()
        {
            if (!_db.Vorhanden) return;
            var ziel = new double[8760];

            SimulationWaermebedarf a = Fassade(PROJEKT);
            ProjektGebaeudeModel za = Gebaeudezeile(PROJEKT);
            za.Kuehluebergabe_Aktiv = true; za.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_KUEHLDECKE;
            za.Einheit = "Verbrauch  [MWh/a]"; za.Z_AuswahlWohnflaeche = 50.0;
            int vorA = a.Vdi6007weg.Aufrufe;
            Assert.True(a.HeizwaermeEinesGebaeudes(za, 0, ziel));
            Assert.Equal(1, a.Vdi6007weg.Aufrufe - vorA);
            KuehlkreisErgebnis kkA = a.GebaeudeErgebnisse.Ergebnis(0).Kuehlkreis;
            Assert.True(kkA.UebergabeNennleistungHergeleitet);
            Assert.Equal(kkA.AuslegungskuehllastKw, kkA.UebergabeNennKw, 9);

            SimulationWaermebedarf b = Fassade(PROJEKT);
            ProjektGebaeudeModel zb = Gebaeudezeile(PROJEKT);
            zb.Kuehluebergabe_Aktiv = true; zb.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_KUEHLDECKE;
            zb.Einheit = "Verbrauch  [MWh/a]"; zb.Z_AuswahlWohnflaeche = 50.0; zb.Kuehl_Uebergabe_Leistung_Nenn = 12.0;
            int vorB = b.Vdi6007weg.Aufrufe;
            Assert.True(b.HeizwaermeEinesGebaeudes(zb, 0, ziel));
            Assert.Equal(2, b.Vdi6007weg.Aufrufe - vorB);
            KuehlkreisErgebnis kkB = b.GebaeudeErgebnisse.Ergebnis(0).Kuehlkreis;
            Assert.False(kkB.UebergabeNennleistungHergeleitet);
            Assert.Equal(12.0, kkB.UebergabeNennKw, 9);

            SimulationWaermebedarf c = Fassade(PROJEKT);
            ProjektGebaeudeModel zc = Gebaeudezeile(PROJEKT);
            zc.Kuehluebergabe_Aktiv = true; zc.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_KUEHLDECKE;
            zc.Einheit = GebaeudeVorbereitung.EINHEIT_FLAECHE; zc.Z_AuswahlWohnflaeche = zc.Nutzflaeche * 2.0;
            zc.Kuehl_Uebergabe_Leistung_Nenn = 12.0;
            int vorC = c.Vdi6007weg.Aufrufe;
            Assert.True(c.HeizwaermeEinesGebaeudes(zc, 0, ziel));
            Assert.Equal(1, c.Vdi6007weg.Aufrufe - vorC);
            Assert.Equal(12.0, c.GebaeudeErgebnisse.Ergebnis(0).Kuehlkreis.UebergabeNennKw, 9);
            _aus.WriteLine($"hergeleitet {kkA.UebergabeNennKw:0.00} kW (Auslegungstag); fest 12 kW bei Verbrauchs- und Flächenangabe");
        }

        // =====================================================================
        //  Die Auskunft des Gebäudedialogs (8.4, 9.1)
        // =====================================================================

        /// <summary>
        /// Die Herleitung der Kälteseite für den Gebäudedialog: Auslegungstag und Kühllast, Quelle und
        /// Höhe des Kaltwasser-Vorlaufs, Grenze — aus demselben Eingangsbauer wie der Lauf, also mit
        /// seiner Zahl (vor der Skalierung); ohne Kühlübergabeart oder Kühlsollwert keine Zahl, bei
        /// widersprüchlichen Eingaben der benannte Grund.
        /// </summary>
        [Fact]
        public void Die_Herleitung_der_Kaelteseite_fuer_den_Dialog()
        {
            if (!_db.Vorhanden) return;
            Kuehlkoppeln();
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));
            KuehlkreisErgebnis lauf = Bedarf(PROJEKT).GebaeudeErgebnisse.Ergebnis(0)?.Kuehlkreis;
            Assert.NotNull(lauf);

            // Das Gebäude, wie es im Projekt steht - als Katalogsatz (dieselben Spalten).
            var zuordnungen = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            ProjektGebaeudeModel pg = GebaeudeBedarfCtrl.Projektgebaeude(PROJEKT, zuordnungen[0].ID_Z);
            var satz = new GebaeudeModel();
            foreach (System.Reflection.FieldInfo f in typeof(GebaeudeModel).GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
            {
                System.Reflection.FieldInfo q = typeof(ProjektGebaeudeModel).GetField(f.Name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                if (q != null && q.FieldType == f.FieldType) f.SetValue(satz, q.GetValue(pg));
            }
            Assert.Equal(DbWerte.KUEHLUEBERGABE_KUEHLDECKE, satz.Kuehl_Uebergabe_Art);

            var quelle = new UebergabeHerleitungsquelle(PROJEKT);
            KuehluebergabeHerleitung h = quelle.HerleitenKuehlung(satz);
            Assert.NotNull(h);
            Assert.True(string.IsNullOrEmpty(h.Befund), h.Befund);
            Assert.Equal(lauf.AuslegungstagKuehlung, h.Auslegungstag);
            Assert.Equal(lauf.AuslegungskuehllastKw / lauf.Skalierungsfaktor, h.AuslegungskuehllastKw.Value, 9);
            Assert.Equal(Vorlaufquelle.Anlage, h.Vorlaufquelle);
            Assert.Equal(18.0, h.VorlaufC);
            Assert.Equal(16.0, h.VorlaufgrenzeC);
            Assert.False(h.Gekappt);
            KuehluebergabeHerleitung nochmal = quelle.HerleitenKuehlung(satz);
            Assert.Equal(h.AuslegungskuehllastKw, nochmal.AuslegungskuehllastKw);
            Assert.Equal(1, quelle.Klimalesungen);

            satz.Kuehl_Auslegung_Vorlauf = 20.0;
            Assert.False(string.IsNullOrEmpty(quelle.HerleitenKuehlung(satz).Befund));
            satz.Kuehl_Sollwert = null;
            Assert.Null(quelle.HerleitenKuehlung(satz));
            satz.Kuehl_Sollwert = 26.0;
            satz.Kuehl_Uebergabe_Art = DbWerte.KUEHLUEBERGABE_IDEAL;
            Assert.Null(quelle.HerleitenKuehlung(satz));
        }
    }
}
