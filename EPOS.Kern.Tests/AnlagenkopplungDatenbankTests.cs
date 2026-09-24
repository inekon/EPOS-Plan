using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Anlagenkopplung mit Datenbank</b> (AK1; 6.1, 8.3, 9.5, 11.3, H7, F-A8, F-A17,
    /// F-A18) — auf einer Arbeitskopie der Testdatenbank, Projekt 1045 (ein Gebäude auf dem
    /// VDI-Weg, eine Wärmepumpe mit Kennlinien je Vorlauf) und 1040 (das Gebäude auf dem
    /// Bestandsweg): Projektschalter aus bei gesetztem Gebäudeschalter, der Altweg-Hinweis,
    /// der gekoppelte Lauf mit den drei Ergebnisspalten und der Kennlinienwahl am gerechneten
    /// Vorlauf, die Extrapolationsregel, beide Läufe der Verhältnisrechnung und die Auskunft
    /// des Gebäudedialogs gegen den Lauf.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AnlagenkopplungDatenbankTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public AnlagenkopplungDatenbankTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1045;
        private const int GEBAEUDE = 10651;       // das Gebäude von 1045 (VDI-Weg)
        private const int PROJEKT_ALTWEG = 1040;
        private const int GEBAEUDE_ALTWEG = 10645; // das Gebäude von 1040 (Tagesbilanz)

        private static void Koppeln(int idGebaeude, string art = DbWerte.UEBERGABE_RADIATOR, bool heizkurve = true)
        {
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Heizkreis_Aktiv = 1, Uebergabe_Art = ?, Heizkurve_Aktiv = ? WHERE ID = ?",
                new DbParam("@art", art), new DbParam("@kurve", heizkurve ? 1 : 0), new DbParam("@id", idGebaeude)));
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

        private static DataRow Energiezeile(int kopfId)
        {
            DataTable dt = DataRepository.GetDataTable("SELECT * FROM Tab_ErgebnisEnergiebedarf WHERE ID_Ergebnis = ?",
                                                       new DbParam("@k", kopfId));
            Assert.NotNull(dt);
            return Assert.Single(dt.Rows.Cast<DataRow>());
        }

        // =====================================================================
        //  Schalter (F-A17, F-A18, 11.3)
        // =====================================================================

        /// <summary>
        /// <b>Projektschalter aus, Gebäudeschalter ein</b> (F-A17, 11.3): kein Unterschied in den
        /// Zahlen — Bit für Bit —, und der Lauf nennt, dass die Eingaben ruhen.
        /// </summary>
        [Fact]
        public void Projektstufe_aus_bei_eingeschalteter_Uebergabe_aendert_keine_Zahl_und_nennt_es()
        {
            if (!_db.Vorhanden) return;
            double[] vorher = (double[])Bedarf(PROJEKT).Waermebedarf_Gebaeude.Clone();

            Koppeln(GEBAEUDE);
            SimulationWaermebedarf nachher = Bedarf(PROJEKT);
            Assert.Null(nachher.Heizkreis);
            for (int h = 0; h < 8760; h++) Assert.True(vorher[h].Equals(nachher.Waermebedarf_Gebaeude[h]), "Stunde " + h);
            Assert.Contains(SimulationProtokoll.Aktuell.Hinweise, t => t.Contains("ohne Anlagenkopplung"));
        }

        /// <summary>
        /// <b>Ein Gebäude auf dem Bestandsweg</b> (F-A18): Es rechnet mit eingeschalteter
        /// Übergabe und Projektstufe AK1 wie bisher, und der Hinweis nennt den Grund.
        /// </summary>
        [Fact]
        public void Ein_Altweg_Gebaeude_rechnet_ohne_Kopplung_und_nennt_es()
        {
            if (!_db.Vorhanden) return;
            double[] vorher = (double[])Bedarf(PROJEKT_ALTWEG).Waermebedarf_Gebaeude.Clone();

            Koppeln(GEBAEUDE_ALTWEG);
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT_ALTWEG, DbWerte.ANLAGENKOPPLUNG_AK1));
            SimulationWaermebedarf nachher = Bedarf(PROJEKT_ALTWEG);
            Assert.Null(nachher.Heizkreis);
            for (int h = 0; h < 8760; h++) Assert.True(vorher[h].Equals(nachher.Waermebedarf_Gebaeude[h]), "Stunde " + h);
            Assert.Contains(SimulationProtokoll.Aktuell.Hinweise, t => t.Contains("Tagesbilanz (Bestandsweg) rechnet keine Anlagenkopplung"));
        }

        // =====================================================================
        //  Der gekoppelte Lauf (6.1, 8.3, F-A8)
        // =====================================================================

        /// <summary>
        /// <b>Der gekoppelte Lauf</b>: Ohne Kopplung stehen die drei Spalten aus Schritt 123 auf
        /// NULL; mit Stufe AK1 und Wärmeübergabe tragen sie die Zahlen des Heizkreises, die
        /// Wärmepumpe wählt ihre Kennlinie je Stunde am gerechneten Vorlauf und meldet es, und
        /// ihre Arbeitszahl steigt mit dem niedrigeren Vorlauf.
        /// </summary>
        [Fact]
        public void Der_gekoppelte_Lauf_schreibt_die_Ergebnisspalten_und_waehlt_die_Kennlinie_je_Stunde()
        {
            if (!_db.Vorhanden) return;
            var bestand = new SimulationRunner();
            int kopfBestand = bestand.SimuliereUndSpeichere(PROJEKT, out string fehlerBestand);
            Assert.True(kopfBestand > 0, fehlerBestand);
            DataRow ohne = Energiezeile(kopfBestand);
            Assert.Equal(DBNull.Value, ohne[AnlagenkopplungSchema.SPALTE_VORLAUF_MITTEL]);
            Assert.Equal(DBNull.Value, ohne[AnlagenkopplungSchema.SPALTE_RUECKLAUF_MITTEL]);
            Assert.Equal(DBNull.Value, ohne[AnlagenkopplungSchema.SPALTE_UEBERGABE_BEGRENZT_STUNDEN]);
            Assert.Null(bestand.sim.simulation_wp.Heizkreisvorlauf);

            Koppeln(GEBAEUDE);
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));
            var lauf = new SimulationRunner();
            int kopf = lauf.SimuliereUndSpeichere(PROJEKT, out string fehler);
            Assert.True(kopf > 0, fehler);

            HeizkreisProjekt hk = lauf.simulation_Waermebedarf.Heizkreis;
            Assert.NotNull(hk);
            DataRow mit = Energiezeile(kopf);
            Assert.Equal(Math.Round(hk.VorlaufMittelC, 2, MidpointRounding.AwayFromZero), Convert.ToDouble(mit[AnlagenkopplungSchema.SPALTE_VORLAUF_MITTEL]));
            Assert.Equal(Math.Round(hk.RuecklaufMittelC, 2, MidpointRounding.AwayFromZero), Convert.ToDouble(mit[AnlagenkopplungSchema.SPALTE_RUECKLAUF_MITTEL]));
            Assert.Equal(Math.Round(hk.UebergabeBegrenztStundenH, 2, MidpointRounding.AwayFromZero), Convert.ToDouble(mit[AnlagenkopplungSchema.SPALTE_UEBERGABE_BEGRENZT_STUNDEN]));

            // Die Wärmepumpe rechnet am gerechneten Vorlauf: Reihe übergeben, Hinweis im Protokoll.
            SimulationWaermepumpe wp = lauf.sim.simulation_wp;
            Assert.Same(hk.VorlaufC, wp.Heizkreisvorlauf);
            Assert.Contains(lauf.Protokoll.Hinweise, t => t.Contains("Kennlinie je Stunde am gerechneten Vorlauf"));

            double azBestand = bestand.sim.simulation_wp.WpWaermeproduktionGesamtKwh / bestand.sim.simulation_wp.WpStrombedarfGesamtKwh;
            double azKopplung = wp.WpWaermeproduktionGesamtKwh / wp.WpStrombedarfGesamtKwh;
            _aus.WriteLine($"Vorlauf {hk.VorlaufMittelC:0.00} °C, Rücklauf {hk.RuecklaufMittelC:0.00} °C, begrenzt {hk.UebergabeBegrenztStundenH:0.0} h; " +
                           $"Wärmebedarf {bestand.simulation_Waermebedarf.Waermebedarf_Gebaeude_Gesamt:0.00} → {lauf.simulation_Waermebedarf.Waermebedarf_Gebaeude_Gesamt:0.00} MWh; " +
                           $"Arbeitszahl {azBestand:0.000} → {azKopplung:0.000}");
            Assert.True(azKopplung > azBestand, "der niedrigere Vorlauf verbessert die Arbeitszahl");

            // Gelesen: NULL bleibt null, ein Wert bleibt ein Wert.
            ErgebnisModel gelesen = new ErgebnisCtrl().Load(PROJEKT);
            Assert.Equal(kopf, gelesen.ID);
            Assert.NotNull(gelesen?.Energiebedarf?.VorlaufMittelC);
        }

        /// <summary>
        /// <b>Die Regel des Bestands außerhalb der Stützstellen</b> (F-A8, 9.5; benannte Lesart
        /// in <c>SimulationWaermepumpe.KenndatenDerStunde</c>): Unter der untersten Stützstelle
        /// rechnet die unterste Kennlinie mit Hinweis — auch bei verbotener Extrapolation, denn
        /// eine Flächenheizung fährt dort regelmäßig. Über der obersten gilt mit erlaubter
        /// Extrapolation die oberste Kennlinie mit Hinweis.
        /// </summary>
        [Fact]
        public void Die_Regel_des_Bestands_gilt_fuer_den_gerechneten_Vorlauf_zweiseitig()
        {
            if (!_db.Vorhanden) return;
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Einstellungen SET Extrapolation_erlaubt = 0 WHERE ID_Projekt = ?",
                                                  new DbParam("@p", PROJEKT)));
            var bestand = new SimulationRunner();
            Assert.True(bestand.Simuliere(PROJEKT, out string fehlerBestand), fehlerBestand);

            // Unter der untersten Stützstelle: die Flächenheizung rechnet, mit Hinweis.
            Koppeln(GEBAEUDE, DbWerte.UEBERGABE_FLAECHE);
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));
            var flaeche = new SimulationRunner();
            Assert.True(flaeche.Simuliere(PROJEKT, out string fehlerFlaeche), fehlerFlaeche);
            Assert.Contains(flaeche.Protokoll.Hinweise, t => t.Contains("unter der untersten Kennlinien-Stützstelle"));

            // Über der obersten Stützstelle (75 °C): ein Auslegungsvorlauf von 85 °C, Extrapolation
            // erlaubt - gerechnet mit der obersten Kennlinie und gemeldet. Den Abbruch bei
            // verbotener Extrapolation hält die Probe ohne Datenbank (die oberste Kennlinie
            // hat hier weniger Quellstützstellen, und deren Bestandsregel griffe zuerst).
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Uebergabe_Art = ?, Auslegung_Vorlauf = 85, Auslegung_Ruecklauf = 70 WHERE ID = ?",
                new DbParam("@art", DbWerte.UEBERGABE_RADIATOR), new DbParam("@id", GEBAEUDE)));
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Einstellungen SET Extrapolation_erlaubt = 1 WHERE ID_Projekt = ?",
                                                  new DbParam("@p", PROJEKT)));
            var erlaubt = new SimulationRunner();
            Assert.True(erlaubt.Simuliere(PROJEKT, out string fehlerErlaubt), fehlerErlaubt);
            Assert.Contains(erlaubt.Protokoll.Hinweise, t => t.Contains("über der obersten Kennlinien-Stützstelle"));
        }

        // =====================================================================
        //  H7 — beide Läufe der Verhältnisrechnung
        // =====================================================================

        private static ProjektGebaeudeModel Zeile(int idProjekt)
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
            sim.AnlagenVorlaufC = 45.0;
            return sim;
        }

        /// <summary>
        /// <b>H7</b>: Mit hergeleiteter Nennleistung skaliert die Heizfläche mit dem Gebäude —
        /// EIN Lauf, und die Verbrauchsrückrechnung trifft den angegebenen Verbrauch. Eine fest
        /// eingetragene Nennleistung gilt dem wirklichen Gebäude: Bei Verbrauchsangabe rechnet
        /// die Fassade zwei Läufe (Probelauf, dann mit der eingetragenen Leistung), das
        /// wirkliche Gebäude trägt genau die eingetragene Nennleistung, und die Abweichung vom
        /// Verbrauch wird benannt; bei Flächenangabe genügt ein Lauf.
        /// </summary>
        [Fact]
        public void H7_Beide_Laeufe_der_Verhaeltnisrechnung_tragen_die_Kopplung()
        {
            if (!_db.Vorhanden) return;
            var ziel = new double[8760];

            // (a) Verbrauchsangabe, hergeleitete Nennleistung: ein Lauf, der Verbrauch wird getroffen.
            SimulationWaermebedarf a = Fassade(PROJEKT);
            ProjektGebaeudeModel za = Zeile(PROJEKT);
            za.Heizkreis_Aktiv = true; za.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR; za.Heizkurve_Aktiv = true;
            za.Einheit = "Verbrauch  [MWh/a]"; za.Z_AuswahlWohnflaeche = 50.0;
            int vorA = a.Vdi6007weg.Aufrufe;
            Assert.True(a.HeizwaermeEinesGebaeudes(za, 0, ziel));
            Assert.Equal(1, a.Vdi6007weg.Aufrufe - vorA);
            Assert.True(Math.Abs(ziel.Sum() / 1000.0 - 50000.0) <= 1e-9 * 50000.0, "Verbrauch getroffen: " + ziel.Sum() / 1000.0);
            HeizkreisErgebnis hkA = a.GebaeudeErgebnisse.Ergebnis(0).Heizkreis;
            Assert.True(hkA.UebergabeNennleistungHergeleitet);
            Assert.Equal(hkA.AuslegungsheizlastKw, hkA.UebergabeNennKw, 9);   // skalierte Auslegungslast

            // (b) Verbrauchsangabe, feste Nennleistung: zwei Läufe, genau die eingetragene Leistung, Hinweis.
            SimulationWaermebedarf b = Fassade(PROJEKT);
            ProjektGebaeudeModel zb = Zeile(PROJEKT);
            zb.Heizkreis_Aktiv = true; zb.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR; zb.Heizkurve_Aktiv = true;
            zb.Einheit = "Verbrauch  [MWh/a]"; zb.Z_AuswahlWohnflaeche = 50.0; zb.Uebergabe_Leistung_Nenn = 20.0;
            int vorB = b.Vdi6007weg.Aufrufe;
            Assert.True(b.HeizwaermeEinesGebaeudes(zb, 0, ziel));
            Assert.Equal(2, b.Vdi6007weg.Aufrufe - vorB);
            HeizkreisErgebnis hkB = b.GebaeudeErgebnisse.Ergebnis(0).Heizkreis;
            Assert.False(hkB.UebergabeNennleistungHergeleitet);
            Assert.Equal(20.0, hkB.UebergabeNennKw, 9);
            Assert.Contains(SimulationProtokoll.Aktuell.Hinweise, t => t.Contains("nicht mehr proportional"));
            _aus.WriteLine($"fest 20 kW: Jahr {ziel.Sum() / 1e6:0.000} MWh gegen angegeben 50 MWh; hergeleitet {hkA.UebergabeNennKw:0.00} kW");

            // (c) Flächenangabe, feste Nennleistung: ein Lauf, genau die eingetragene Leistung.
            SimulationWaermebedarf c = Fassade(PROJEKT);
            ProjektGebaeudeModel zc = Zeile(PROJEKT);
            zc.Heizkreis_Aktiv = true; zc.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR; zc.Heizkurve_Aktiv = true;
            zc.Uebergabe_Leistung_Nenn = 20.0;
            Assert.Equal(GebaeudeVorbereitung.EINHEIT_FLAECHE, zc.Einheit);
            int vorC = c.Vdi6007weg.Aufrufe;
            Assert.True(c.HeizwaermeEinesGebaeudes(zc, 0, ziel));
            Assert.Equal(1, c.Vdi6007weg.Aufrufe - vorC);
            Assert.Equal(20.0, c.GebaeudeErgebnisse.Ergebnis(0).Heizkreis.UebergabeNennKw, 9);
        }

        // =====================================================================
        //  Die Auskunft ruft den Lauf (11.3)
        // =====================================================================

        /// <summary>
        /// <b>Die Auskunft des Gebäudedialogs liefert mit Kopplung dieselbe Zahl wie der Lauf</b>
        /// (11.3, Muster <c>GebaeudeBedarfCtrlTests</c>) — beide rufen dieselbe Fassade.
        /// </summary>
        [Fact]
        public void Die_Auskunft_liefert_mit_Kopplung_die_Zahl_des_Laufs()
        {
            if (!_db.Vorhanden) return;
            Koppeln(GEBAEUDE);
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));
            List<Z_ProjGebModel> zuordnungen = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            GebaeudeBedarfErgebnis e = GebaeudeBedarfCtrl.Rechnen(PROJEKT, Klimaregion(PROJEKT), zuordnungen[0].ID_Z);
            Assert.True(e.Erfolgreich);
            SimulationWaermebedarf lauf = Bedarf(PROJEKT);
            Assert.NotNull(lauf.Heizkreis);
            Assert.Equal(lauf.Waermebedarf_Gebaeude_Gesamt, e.HeizwaermeMwh);
        }
    }
}
