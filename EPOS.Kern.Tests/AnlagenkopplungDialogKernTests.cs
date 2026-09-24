using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kernseite der Masken der Anlagenkopplung</b> (AK1 Welle 3; Konzept 9.1, 9.2, 9.4):
    /// die öffentlichen Vorgaben und Grenzen des Gebäudedialogs sind die des Eingangsbauers, die
    /// Bestandswoche des Wochenrasters rechnet byte-gleich wie der Bestandsfahrplan, der
    /// Projektschalter folgt der Vormerksatz-Regel, und die hergeleiteten Vorgaben des Dialogs
    /// sind die Zahlen des Laufs.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class AnlagenkopplungDialogKernTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static readonly SolardatenModel[] Klima = Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang);

        // =====================================================================
        //  Vorgaben und Grenzen (9.1): EINE Quelle für Dialog und Eingangsbauer
        // =====================================================================

        [Fact]
        public void Die_Vorgaben_der_Art_sind_die_des_Eingangsbauers()
        {
            foreach (string art in new[] { DbWerte.UEBERGABE_RADIATOR, DbWerte.UEBERGABE_FLAECHE, DbWerte.UEBERGABE_KONVEKTOR })
            {
                Assert.True(Waermeuebergabevorgaben.ArtRechnet(art));
                Assert.Equal(Waermeuebergabe.VorgabeExponent(art), Waermeuebergabevorgaben.Exponent(art));
                Assert.Equal(Waermeuebergabe.VorgabeVorlaufC(art), Waermeuebergabevorgaben.Vorlauf(art));
                Assert.Equal(Waermeuebergabe.VorgabeRuecklaufC(art), Waermeuebergabevorgaben.Ruecklauf(art));
                Assert.Equal(Waermeuebergabe.VorgabeStrahlungsanteil(art), Waermeuebergabevorgaben.Strahlungsanteil(art));
            }
            foreach (string ideal in new[] { DbWerte.UEBERGABE_IDEAL, null, "", "UNBEKANNT" })
            {
                Assert.False(Waermeuebergabevorgaben.ArtRechnet(ideal));
                Assert.Null(Waermeuebergabevorgaben.Exponent(ideal));
                Assert.Null(Waermeuebergabevorgaben.Vorlauf(ideal));
            }
            Assert.Equal(new[] { DbWerte.UEBERGABE_IDEAL, DbWerte.UEBERGABE_RADIATOR, DbWerte.UEBERGABE_FLAECHE, DbWerte.UEBERGABE_KONVEKTOR },
                         Waermeuebergabevorgaben.Arten);

            // Die Vorgaben und Grenzen sind die Festwerte des Kerns (E25: Band 0 … 5 K, Vorgabe 1 K).
            Assert.Equal(1.0, Waermeuebergabevorgaben.Proportionalband);
            Assert.Equal(new[] { 0.5, 1.0, 2.0 }, Waermeuebergabevorgaben.ProportionalbandSchnellwahl);
            Assert.Equal(0.0, Waermeuebergabevorgaben.BAND_MIN);
            Assert.Equal(5.0, Waermeuebergabevorgaben.BAND_MAX);
            Assert.Equal(GebaeudeFestwerte.UEBERGABE_EXPONENT_MAX, Waermeuebergabevorgaben.EXPONENT_MAX);
            Assert.Equal(GebaeudeFestwerte.AUSLEGUNG_VORLAUF_MIN, Waermeuebergabevorgaben.VORLAUF_MIN);
            Assert.Equal(GebaeudeFestwerte.SOLLWERTPROFIL_MAX_C, Waermeuebergabevorgaben.SOLLWERT_MAX);
        }

        [Fact]
        public void Gebaut_und_waehlbar_sind_nur_aus_und_AK1()
        {
            Assert.Equal(new[] { DbWerte.ANLAGENKOPPLUNG_AUS, DbWerte.ANLAGENKOPPLUNG_AK1, DbWerte.ANLAGENKOPPLUNG_AK2, DbWerte.ANLAGENKOPPLUNG_AK3 },
                         Waermeuebergabevorgaben.Stufen);
            Assert.True(Waermeuebergabevorgaben.StufeGebaut(null));
            Assert.True(Waermeuebergabevorgaben.StufeGebaut(DbWerte.ANLAGENKOPPLUNG_AUS));
            Assert.True(Waermeuebergabevorgaben.StufeGebaut(DbWerte.ANLAGENKOPPLUNG_AK1));
            Assert.False(Waermeuebergabevorgaben.StufeGebaut(DbWerte.ANLAGENKOPPLUNG_AK2));
            Assert.False(Waermeuebergabevorgaben.StufeGebaut(DbWerte.ANLAGENKOPPLUNG_AK3));
        }

        // =====================================================================
        //  Die Bestandswoche des Wochenrasters (9.2)
        // =====================================================================

        /// <summary>
        /// „Wer daraus ein Profil macht, sieht, was er bekommt" (9.2): Die Woche, die das Raster
        /// ohne Zeitprogramm zeigt, ergibt als Zeitprogramm byte-gleiche Sollwerte — mit schwachem
        /// und mit wirksamem Wochenendwert.
        /// </summary>
        [Theory]
        [InlineData(0.0)]
        [InlineData(16.5)]
        public void Die_Bestandswoche_als_Zeitprogramm_ergibt_byte_gleiche_Sollwerte(double wochenende)
        {
            ProjektGebaeudeModel bestand = Vdi6007Probe.Gebaeude();
            bestand.Raumsolltemperatur_Wochenende = wochenende;
            bestand.Wochenende = wochenende > 0 ? 1.0 : 0.0;
            GebaeudeModellEingang ohne = Vdi6007Probe.Eingang(bestand, Klima);

            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Raumsolltemperatur_Wochenende = wochenende;
            g.Wochenende = bestand.Wochenende;
            g.Heizkreis_Aktiv = true;
            g.Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR;
            double[] woche = Waermeuebergabevorgaben.Bestandswoche(g.Raumsolltemperatur_Tag,
                                                                   g.Raumsolltemperatur_Nachtabsenkung,
                                                                   g.Raumsolltemperatur_Wochenende);
            Assert.Equal(168, woche.Length);
            g.Sollwertprofil = AnlagenkopplungSchema.WochenprofilSchreiben(woche);

            GebaeudeModellEingang mit = GebaeudeModellEingang.Bauen(g, Klima, Vdi6007Probe.Wochenende(),
                Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false,
                DbWerte.ANLAGENKOPPLUNG_AK1, double.NaN, 1.0);
            Assert.True(mit.SollwertprofilWirksam);
            for (int h = 0; h < 8760; h++) Assert.True(ohne.ThetaSoll[h].Equals(mit.ThetaSoll[h]), "Stunde " + h);
        }

        // =====================================================================
        //  Der Katalogsatz als Projektgebäude (Herleitung, 8.4)
        // =====================================================================

        /// <summary>
        /// Jedes Feld des Projektgebäudes, das nicht allein dem Projekt gehört, kommt aus dem
        /// Katalogsatz — auch jedes, das ein künftiger Schemaschritt beiden Modellen gibt.
        /// </summary>
        [Fact]
        public void Der_Katalogsatz_traegt_jedes_Feld_des_Gebaeudes_ins_Projektmodell()
        {
            var nurProjekt = new HashSet<string>(StringComparer.Ordinal)
            {
                "items", "ID_Projekt", "ID_Gebaeude", "Z_AuswahlWohnflaeche", "Einheit", "Jahresnutzungsgrad", "DezentralWarmwasser"
            };
            IReadOnlyList<string> uebertragen = UebergabeHerleitungsquelle.Uebertragen();
            foreach (FieldInfo f in typeof(ProjektGebaeudeModel).GetFields(BindingFlags.Public | BindingFlags.Instance))
                if (!nurProjekt.Contains(f.Name))
                    Assert.Contains(f.Name, uebertragen);

            // Werte kommen an - auch NULL.
            var satz = new GebaeudeModel
            {
                Gebaeudename = "Probe", Raumsolltemperatur_Tag = 21.5, k_Wert_Fenster = 1.1,
                Heizkreis_Aktiv = true, Uebergabe_Art = DbWerte.UEBERGABE_FLAECHE, Uebergabe_Exponent = null,
                Auslegung_Vorlauf = 38.0, Regler_Proportionalband = 0.5, Sollwertprofil = "x"
            };
            ProjektGebaeudeModel g = UebergabeHerleitungsquelle.AusKatalogsatz(satz);
            Assert.Equal("Probe", g.Gebaeudename);
            Assert.Equal(21.5, g.Raumsolltemperatur_Tag);
            Assert.Equal(1.1, g.k_Wert_Fenster);
            Assert.True(g.Heizkreis_Aktiv);
            Assert.Equal(DbWerte.UEBERGABE_FLAECHE, g.Uebergabe_Art);
            Assert.Null(g.Uebergabe_Exponent);
            Assert.Equal(38.0, g.Auslegung_Vorlauf);
            Assert.Equal(0.5, g.Regler_Proportionalband);
            Assert.Equal("x", g.Sollwertprofil);
        }

        /// <summary>Ohne Projekt, ohne Übergabeart: keine Herleitung (die Zeile nennt die Regel ohne Zahl).</summary>
        [Fact]
        public void Ohne_Projekt_oder_ohne_Uebergabeart_gibt_es_keine_Zahl()
        {
            var ohneArt = new GebaeudeModel { Uebergabe_Art = DbWerte.UEBERGABE_IDEAL };
            Assert.Null(new UebergabeHerleitungsquelle(1045).Herleiten(ohneArt));
            var quelle = new UebergabeHerleitungsquelle(0);
            Assert.Null(quelle.Herleiten(new GebaeudeModel { Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR }));
            Assert.Equal(0, quelle.Klimalesungen);
        }

        /// <summary>
        /// <b>Die Zahl des Dialogs ist die Zahl des Laufs</b> (Kern-Regel „Eine Auskunft ruft den
        /// Rechenweg des Laufs"): An Projekt 1045 liefert die Herleitung für das Gebäude, wie es im
        /// Projekt steht, dieselbe Auslegungs-Außentemperatur und dieselbe (unskalierte)
        /// Auslegungsheizlast wie der gekoppelte Lauf — und liest die Klimareihe einmal, wie oft
        /// sie auch gerufen wird.
        /// </summary>
        [Fact]
        public void Die_hergeleiteten_Vorgaben_sind_die_Zahlen_des_Laufs()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            const int PROJEKT = 1045, GEBAEUDE = 10651;

            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Heizkreis_Aktiv = 1, Uebergabe_Art = ?, Heizkurve_Aktiv = 1 WHERE ID = ?",
                new DbParam("@art", DbWerte.UEBERGABE_RADIATOR), new DbParam("@id", GEBAEUDE)));
            Assert.True(KonfigurationCtrl.AnlagenkopplungSchreiben(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));

            var projekt = new ProjektCtrl();
            projekt.ReadSingle(PROJEKT);
            var sim = new SimulationWaermebedarf();
            sim.Waermebedarf_berechnen(PROJEKT, projekt.m_ID_Klimaregion);
            HeizkreisErgebnis lauf = sim.GebaeudeErgebnisse.Ergebnis(0)?.Heizkreis;
            Assert.NotNull(lauf);

            // Das Gebäude, wie es im Projekt steht - als Katalogsatz (dieselben Spalten).
            List<Z_ProjGebModel> zuordnungen = Z_ProjGebCtrl.LiesProjekt(PROJEKT);
            ProjektGebaeudeModel pg = GebaeudeBedarfCtrl.Projektgebaeude(PROJEKT, zuordnungen[0].ID_Z);
            var satz = new GebaeudeModel();
            foreach (FieldInfo f in typeof(GebaeudeModel).GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                FieldInfo q = typeof(ProjektGebaeudeModel).GetField(f.Name, BindingFlags.Public | BindingFlags.Instance);
                if (q != null && q.FieldType == f.FieldType) f.SetValue(satz, q.GetValue(pg));
            }

            var quelle = new UebergabeHerleitungsquelle(PROJEKT);
            UebergabeHerleitung h = quelle.Herleiten(satz);
            Assert.NotNull(h);
            Assert.Equal("", h.Befund);
            Assert.Equal(lauf.AuslegungAussenC, h.AuslegungAussenC);
            Assert.Equal(lauf.AuslegungsheizlastKw / lauf.Skalierungsfaktor, h.AuslegungsheizlastKw.Value, 9);
            Assert.True(h.AuslegungsheizlastKw > 0);

            quelle.Herleiten(satz);
            Assert.Equal(1, quelle.Klimalesungen);
        }

        // =====================================================================
        //  Der Projektschalter (9.4): Vormerksatz-Regel wie beim Kühlschalter
        // =====================================================================

        private static long Saetze(int id) => Convert.ToInt64(DataRepository.ExecuteScalar(
            "SELECT COUNT(*) FROM Tab_Einstellungen WHERE ID_Projekt = ?", new DbParam("@p", id)));

        private static DataRow Satz(int id) => DataRepository.GetDataTable(
            "SELECT * FROM Tab_Einstellungen WHERE ID_Projekt = ?", new DbParam("@p", id)).Rows[0];

        [Fact]
        public void Der_Projektschalter_schreibt_die_Stufe_und_aus_ist_NULL()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            const int PROJEKT = 1045;
            Assert.Equal(1L, Saetze(PROJEKT));
            Assert.Null(KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));

            Assert.True(KonfigurationCtrl.AnlagenkopplungSetzen(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));
            Assert.Equal(DbWerte.ANLAGENKOPPLUNG_AK1, KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));
            Assert.True(KonfigurationCtrl.AnlagenkopplungSetzen(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AUS));
            Assert.Null(KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));
            Assert.Equal(DBNull.Value, Satz(PROJEKT)[AnlagenkopplungSchema.SPALTE_ANLAGENKOPPLUNG]);
            Assert.True(KonfigurationCtrl.AnlagenkopplungSetzen(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));
            Assert.True(KonfigurationCtrl.AnlagenkopplungSetzen(PROJEKT, null));
            Assert.Null(KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));

            // Ein fremder Wert scheitert an der Prüfung der Spalte; der Stand bleibt.
            Assert.False(KonfigurationCtrl.AnlagenkopplungSetzen(PROJEKT, "AK9"));
            Assert.Null(KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));
            Assert.Equal(1L, Saetze(PROJEKT));
        }

        [Fact]
        public void Ohne_Satz_schreibt_aus_nichts_und_eine_Stufe_legt_den_Vormerksatz_an()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            const int PROJEKT = 1045;
            DataRepository.ExecuteNonQuery("DELETE FROM Tab_Einstellungen WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));
            Assert.Equal(0L, Saetze(PROJEKT));

            Assert.True(KonfigurationCtrl.AnlagenkopplungSetzen(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AUS));
            Assert.Equal(0L, Saetze(PROJEKT));

            Assert.True(KonfigurationCtrl.AnlagenkopplungSetzen(PROJEKT, DbWerte.ANLAGENKOPPLUNG_AK1));
            Assert.Equal(1L, Saetze(PROJEKT));
            Assert.True(KonfigurationCtrl.IstVormerksatz(Satz(PROJEKT)));
            Assert.Equal(DbWerte.ANLAGENKOPPLUNG_AK1, KonfigurationCtrl.AnlagenkopplungLesen(PROJEKT));

            Assert.False(KonfigurationCtrl.AnlagenkopplungSetzen(0, DbWerte.ANLAGENKOPPLUNG_AK1));
        }
    }
}
