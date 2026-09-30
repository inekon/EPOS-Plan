using System;
using System.Collections.Generic;
using System.Data;
using System.Reflection;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Vorbelegung von Vor- und Rücklauf im Projektdialog</b> (Anwenderauftrag
    /// 30.09.2026: „Dialogfelder Vorlauf und Rücklauftemperatur übersichtlicher und
    /// Vorbelegung, falls 0, mit sinnvollen Vorgaben").
    ///
    /// <para><b>Was hier geprüft wird.</b> Die reine Regel des Heizkessels in allen drei
    /// Stufen (Anlage, Kesseldatensatz, Vorgabe 70/50 °C) samt der unvollständigen und
    /// vertauschten Paare, die Bindung der Vorgabe an den Rückfall der Simulation und der
    /// Brennwertkennlinie, die Herleitungszeilen, die Rücklaufregel der Wärmepumpe — und
    /// der GLEICHLAUF: Für jede Kesselanlage der Testdatenbank (und für die Stufen, die sie
    /// nicht von selbst führt) belegt der Dialog genau das Paar vor, mit dem die Simulation
    /// ohne Eintrag rechnet (<c>SimulationControl.KesselTemperaturpaarGepflegt</c> und ihr
    /// Rückfall). Darauf beruht, dass das Speichern der Vorbelegung kein Ergebnis ändert.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class AnlagenTemperaturVorbelegungTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public AnlagenTemperaturVorbelegungTests(TestDatenbank db) { _db = db; }

        // =================================================================================
        // Die reine Regel des Heizkessels
        // =================================================================================

        /// <summary>Stufe 1: Ein vollständiges Paar der Anlage bleibt, wie es ist — auch neben einem Kesselpaar.</summary>
        [Fact]
        public void Ein_Paar_der_Anlage_bleibt_stehen()
        {
            AnlagenTemperaturen.PaarVorbelegung p = AnlagenTemperaturen.KesselPaar(80, 60, 85, 65);

            Assert.Equal(AnlagenTemperaturen.PaarHerkunft.Anlage, p.Herkunft);
            Assert.False(p.Vorbelegt);
            Assert.Equal(80, p.Vorlauf);
            Assert.Equal(60, p.Ruecklauf);
        }

        /// <summary>
        /// Ein VERTAUSCHTES Paar ist eine Eingabe, keine Lücke: Es wird nicht still
        /// überschrieben (die Vorbelegung gilt nur „falls 0").
        /// </summary>
        [Fact]
        public void Ein_vertauschtes_Paar_der_Anlage_wird_nicht_ueberschrieben()
        {
            AnlagenTemperaturen.PaarVorbelegung p = AnlagenTemperaturen.KesselPaar(50, 70, 85, 65);

            Assert.Equal(AnlagenTemperaturen.PaarHerkunft.Anlage, p.Herkunft);
            Assert.Equal(50, p.Vorlauf);
            Assert.Equal(70, p.Ruecklauf);
        }

        /// <summary>Stufe 2: Ohne Paar an der Anlage gilt das des Kesseldatensatzes — leer wie 0.</summary>
        [Theory]
        [InlineData(0, 0)]
        [InlineData(null, null)]
        [InlineData(0, null)]
        public void Ohne_Paar_an_der_Anlage_gilt_das_des_Kessels(int? vorlauf, int? ruecklauf)
        {
            AnlagenTemperaturen.PaarVorbelegung p = AnlagenTemperaturen.KesselPaar(vorlauf, ruecklauf, 85, 65);

            Assert.Equal(AnlagenTemperaturen.PaarHerkunft.Geraet, p.Herkunft);
            Assert.True(p.Vorbelegt);
            Assert.Equal(85, p.Vorlauf);
            Assert.Equal(65, p.Ruecklauf);
        }

        /// <summary>
        /// Ein HALBES Paar der Anlage (60/0) ist unvollständig: Die Simulation übergeht es,
        /// also zeigt der Dialog das ganze Paar, mit dem sie rechnet — nicht 60 mit einem
        /// ergänzten Rücklauf, das wäre ein neues, nie gerechnetes Paar.
        /// </summary>
        [Theory]
        [InlineData(60, 0)]
        [InlineData(0, 40)]
        [InlineData(60, null)]
        public void Ein_halbes_Paar_der_Anlage_wird_ganz_ersetzt(int? vorlauf, int? ruecklauf)
        {
            AnlagenTemperaturen.PaarVorbelegung mitKessel = AnlagenTemperaturen.KesselPaar(vorlauf, ruecklauf, 85, 65);
            Assert.Equal((85, 65), (mitKessel.Vorlauf!.Value, mitKessel.Ruecklauf!.Value));

            AnlagenTemperaturen.PaarVorbelegung ohneKessel = AnlagenTemperaturen.KesselPaar(vorlauf, ruecklauf, null, null);
            Assert.Equal(AnlagenTemperaturen.PaarHerkunft.Vorgabe, ohneKessel.Herkunft);
            Assert.Equal((70, 50), (ohneKessel.Vorlauf!.Value, ohneKessel.Ruecklauf!.Value));
        }

        /// <summary>
        /// Stufe 3: Weder Anlage noch Kessel — die Vorgabe 70/50 °C. Ein halbes oder
        /// vertauschtes Kesselpaar zählt nicht (<c>ProjektPuffer.IstTemperaturpaar</c>, wie in
        /// der Simulation).
        /// </summary>
        [Theory]
        [InlineData(null, null)]
        [InlineData(0, 0)]
        [InlineData(90, 0)]
        [InlineData(50, 70)]
        [InlineData(60, 60)]
        public void Ohne_brauchbares_Kesselpaar_gilt_die_Vorgabe(int? kesselVorlauf, int? kesselRuecklauf)
        {
            AnlagenTemperaturen.PaarVorbelegung p = AnlagenTemperaturen.KesselPaar(0, 0, kesselVorlauf, kesselRuecklauf);

            Assert.Equal(AnlagenTemperaturen.PaarHerkunft.Vorgabe, p.Herkunft);
            Assert.True(p.Vorbelegt);
            Assert.Equal(70, p.Vorlauf);
            Assert.Equal(50, p.Ruecklauf);
        }

        /// <summary>
        /// Die Vorgabe IST der Rückfall der Simulation und der Brennwertkennlinie — nicht
        /// abgeschrieben, sondern referenziert; eine gebrochene Zahl dort fiele hier auf.
        /// </summary>
        [Fact]
        public void Die_Vorgabe_ist_der_Rueckfall_der_Simulation_und_der_Kennlinie()
        {
            Assert.Equal(SimulationControl.KESSEL_VORLAUF_RUECKFALL, (double)AnlagenTemperaturen.KESSEL_VORLAUF_VORGABE);
            Assert.Equal(SimulationControl.KESSEL_RUECKLAUF_RUECKFALL, (double)AnlagenTemperaturen.KESSEL_RUECKLAUF_VORGABE);
            Assert.Equal(Kesselkennlinie.RUECKLAUF_RUECKFALL_C, (double)AnlagenTemperaturen.KESSEL_RUECKLAUF_VORGABE);
            Assert.Equal(70, AnlagenTemperaturen.KESSEL_VORLAUF_VORGABE);
            Assert.Equal(50, AnlagenTemperaturen.KESSEL_RUECKLAUF_VORGABE);
        }

        /// <summary>Unvollständig heißt: 0 oder leer in mindestens einem Feld.</summary>
        [Theory]
        [InlineData(70, 50, false)]
        [InlineData(50, 70, false)]
        [InlineData(70, 0, true)]
        [InlineData(0, 50, true)]
        [InlineData(null, 50, true)]
        [InlineData(70, null, true)]
        [InlineData(-5, 50, true)]
        public void Unvollstaendig_heisst_null_oder_leer(int? vorlauf, int? ruecklauf, bool erwartet)
            => Assert.Equal(erwartet, AnlagenTemperaturen.PaarUnvollstaendig(vorlauf, ruecklauf));

        // =================================================================================
        // Die Herleitungszeilen
        // =================================================================================

        [Fact]
        public void Die_Herleitung_nennt_Vorgabe_und_Kesseldatensatz()
        {
            using var _ = new Kulturvorrichtung();

            Assert.Equal("Vorgabe 70/50 °C — so rechnet die Simulation ohne Eintrag.",
                         AnlagenTemperaturen.Herleitung(AnlagenTemperaturen.KesselPaar(0, 0, null, null)));
            Assert.Equal("Aus dem Kesseldatensatz: 85/65 °C — so rechnet die Simulation ohne Eintrag.",
                         AnlagenTemperaturen.Herleitung(AnlagenTemperaturen.KesselPaar(0, 0, 85, 65)));
            Assert.Equal("", AnlagenTemperaturen.Herleitung(AnlagenTemperaturen.KesselPaar(80, 60, null, null)));
        }

        /// <summary>Beide Sprachen führen die Zeilen — Englisch ist kein stiller deutscher Rückfall.</summary>
        [Fact]
        public void Die_Herleitungen_stehen_auch_englisch()
        {
            using var _ = new Kulturvorrichtung("en-US");

            Assert.Equal("Default 70/50 °C — the simulation uses this pair when nothing is entered.",
                         AnlagenTemperaturen.Herleitung(AnlagenTemperaturen.KesselPaar(0, 0, null, null)));
            Assert.StartsWith("From the boiler data record: 85/65 °C",
                              AnlagenTemperaturen.Herleitung(AnlagenTemperaturen.KesselPaar(0, 0, 85, 65)));
            Assert.Equal("Return pre-filled: flow 55 °C − 10 K = 45 °C (fallback spread).",
                         AnlagenTemperaturen.WaermepumpeRuecklaufHerleitung(55, 45));
        }

        [Fact]
        public void Die_Herleitung_der_Waermepumpe_nennt_Vorlauf_und_Spreizung()
        {
            using var _ = new Kulturvorrichtung();

            Assert.Equal("Rücklauf vorbelegt: Vorlauf 55 °C − 10 K = 45 °C (Rückfall-Spreizung).",
                         AnlagenTemperaturen.WaermepumpeRuecklaufHerleitung(55, 45));
        }

        // =================================================================================
        // Die Rücklaufregel der Wärmepumpe
        // =================================================================================

        /// <summary>
        /// Rücklauf 0 oder leer: Vorlauf − 10 K. Ein eingetragener Rücklauf bleibt — auch ein
        /// unpassender, den meldet die Prüfung des Dialogs; ohne Vorlauf gibt es nichts, woraus
        /// er folgen könnte.
        /// </summary>
        [Theory]
        [InlineData(55, 0, 45)]
        [InlineData(55, null, 45)]
        [InlineData(35, 0, 25)]
        [InlineData(55, 30, null)]
        [InlineData(35, 40, null)]
        [InlineData(null, 0, null)]
        [InlineData(0, 0, null)]
        [InlineData(10, 0, null)]
        [InlineData(11, null, 1)]
        public void Der_Ruecklauf_der_Waermepumpe_folgt_dem_Vorlauf(int? vorlauf, int? ruecklauf, int? erwartet)
            => Assert.Equal(erwartet, AnlagenTemperaturen.WaermepumpeRuecklaufVorgabe(vorlauf, ruecklauf));

        /// <summary>Die Spreizung ist die Rückfall-Spreizung des Laufs, nicht eine zweite Zahl.</summary>
        [Fact]
        public void Die_Spreizung_der_Waermepumpe_ist_die_Rueckfall_Spreizung_des_Laufs()
        {
            Assert.Equal(Warnkriterien.RUECKFALL_DELTA_T, (double)AnlagenTemperaturen.WAERMEPUMPE_SPREIZUNG_VORGABE_K);
            Assert.Equal(10, AnlagenTemperaturen.WAERMEPUMPE_SPREIZUNG_VORGABE_K);
        }

        // =================================================================================
        // Der Leseweg und der Gleichlauf mit der Simulation
        // =================================================================================

        /// <summary>Eine andere Anlagenart bleibt unberührt — auch mit 0/0.</summary>
        [Fact]
        public void Eine_Waermepumpe_bekommt_kein_Kesselpaar()
        {
            var item = new WErzeugerModel { ID_Type = WizardItemClass.WP_TYP, Vorlauf = 0, Ruecklauf = 0 };

            AnlagenTemperaturen.PaarVorbelegung p = AnlagenTemperaturen.KesselPaarVorbelegen(item, false);

            Assert.False(p.Vorbelegt);
            Assert.Equal(0, item.Vorlauf);
            Assert.Equal(0, item.Ruecklauf);
        }

        /// <summary>
        /// <b>Der Gleichlauf.</b> Für jede Kesselanlage der Testdatenbank legt
        /// <see cref="AnlagenTemperaturen.KesselPaarVorbelegen"/> genau das Paar in den
        /// Feldsatz, mit dem die Simulation ohne Eintrag rechnet: das gepflegte Paar der Kette
        /// Anlage → Kessel (<c>SimulationControl.KesselTemperaturpaarGepflegt</c>), sonst ihren
        /// Rückfall. Die Stufe „Kesseldatensatz" führt die Testdatenbank nicht (alle Kessel
        /// tragen NULL); der Fall setzt sie deshalb an einer Anlage selbst — auch mit halbem
        /// Anlagenpaar und vertauschtem Kesselpaar — und stellt den Stand danach wieder her.
        /// </summary>
        [Fact]
        public void Die_Vorbelegung_ist_das_Paar_der_Simulation_ohne_Eintrag()
        {
            if (!_db.Vorhanden) return;

            var stufen = new HashSet<AnlagenTemperaturen.PaarHerkunft>();
            int geprueft = 0;
            DataTable anlagen = Anlagen();
            foreach (DataRow r in anlagen.Rows)
            {
                GleichlaufPruefen(Convert.ToInt32(r["ID"]), stufen);
                geprueft++;
            }
            Assert.True(geprueft >= 20, "Nur " + geprueft + " Kesselanlagen in der Testdatenbank.");

            // Die Stufe „Kesseldatensatz" von Hand — an der ersten Anlage ohne Paar.
            int anlage = -1, kessel = -1;
            foreach (DataRow r in anlagen.Rows)
                if (Zahl(r["Vorlauf"]) == 0 && Zahl(r["ID_Kessel"]) > 0)
                { anlage = Convert.ToInt32(r["ID"]); kessel = Zahl(r["ID_Kessel"]); break; }
            Assert.True(anlage > 0, "Keine Kesselanlage ohne Paar in der Testdatenbank.");

            try
            {
                KesselpaarSetzen(kessel, 80, 60);
                Assert.Equal(AnlagenTemperaturen.PaarHerkunft.Geraet, GleichlaufPruefen(anlage, stufen));

                AnlagenpaarSetzen(anlage, 60, 0);                 // halbes Anlagenpaar
                Assert.Equal(AnlagenTemperaturen.PaarHerkunft.Geraet, GleichlaufPruefen(anlage, stufen));

                KesselpaarSetzen(kessel, 50, 70);                 // vertauschtes Kesselpaar
                Assert.Equal(AnlagenTemperaturen.PaarHerkunft.Vorgabe, GleichlaufPruefen(anlage, stufen));
            }
            finally
            {
                KesselpaarSetzen(kessel, null, null);
                AnlagenpaarSetzen(anlage, 0, 0);
            }

            Assert.Contains(AnlagenTemperaturen.PaarHerkunft.Anlage, stufen);
            Assert.Contains(AnlagenTemperaturen.PaarHerkunft.Geraet, stufen);
            Assert.Contains(AnlagenTemperaturen.PaarHerkunft.Vorgabe, stufen);
        }

        /// <summary>
        /// Vergleicht die Vorbelegung einer Anlage mit dem Paar der Simulation; ein
        /// VERTAUSCHTES Anlagenpaar wird nicht vorbelegt und bleibt außen vor.
        /// </summary>
        private static AnlagenTemperaturen.PaarHerkunft GleichlaufPruefen(
            int idAnlage, HashSet<AnlagenTemperaturen.PaarHerkunft> stufen)
        {
            WErzeugerModel item = Modell(idAnlage);
            bool vertauscht = !AnlagenTemperaturen.PaarUnvollstaendig(item.Vorlauf, item.Ruecklauf) &&
                              item.Vorlauf <= item.Ruecklauf;

            AnlagenTemperaturen.PaarVorbelegung p = AnlagenTemperaturen.KesselPaarVorbelegen(item, false);
            stufen.Add(p.Herkunft);
            if (vertauscht) return p.Herkunft;

            object[] args = { idAnlage, 0.0, 0.0 };
            bool gepflegt = (bool)KetteDerSimulation().Invoke(null, args)!;
            double vorlauf = gepflegt ? (double)args[1] : SimulationControl.KESSEL_VORLAUF_RUECKFALL;
            double ruecklauf = gepflegt ? (double)args[2] : SimulationControl.KESSEL_RUECKLAUF_RUECKFALL;

            Assert.Equal(vorlauf, (double)item.Vorlauf);
            Assert.Equal(ruecklauf, (double)item.Ruecklauf);
            Assert.Equal(p.Vorlauf, item.Vorlauf);
            Assert.Equal(p.Ruecklauf, item.Ruecklauf);
            return p.Herkunft;
        }

        /// <summary>
        /// Die gepflegte Kette der Simulation — privat in <see cref="SimulationControl"/>; der
        /// Fall liest sie über Reflexion, damit er genau DIE Kette vergleicht, die der Lauf
        /// nimmt. Heißt sie einmal anders, fällt der Fall auf, statt still zu vergleichen.
        /// </summary>
        private static MethodInfo KetteDerSimulation()
        {
            MethodInfo? m = typeof(SimulationControl).GetMethod(
                "KesselTemperaturpaarGepflegt", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(m);
            return m!;
        }

        private static DataTable Anlagen()
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID, Vorlauf, ID_Kessel FROM Tab_Energieanlagen WHERE ID_Type IN (?, ?) ORDER BY ID",
                new DbParam("@t1", WizardItemClass.KESSEL_TYP), new DbParam("@t2", WizardItemClass.REF_KESSEL_TYP));
            Assert.NotNull(dt);
            return dt;
        }

        /// <summary>Die Anlagenzeile als Feldsatz — nur die Spalten, die die Vorbelegung liest.</summary>
        private static WErzeugerModel Modell(int idAnlage)
        {
            DataTable dt = DataRepository.GetDataTable(
                "SELECT ID_Type, Vorlauf, [Rücklauf] AS Ruecklauf, ID_Kessel FROM Tab_Energieanlagen WHERE ID = ?",
                new DbParam("@id", idAnlage));
            DataRow r = dt.Rows[0];
            return new WErzeugerModel
            {
                ID = idAnlage,
                ID_Type = Zahl(r["ID_Type"]),
                Vorlauf = Zahl(r["Vorlauf"]),
                Ruecklauf = Zahl(r["Ruecklauf"]),
                ID_Kessel = Zahl(r["ID_Kessel"])
            };
        }

        private static void KesselpaarSetzen(int idKessel, int? vorlauf, int? ruecklauf)
        {
            if (idKessel <= 0) return;
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Heizkessel SET Vorlauf = ?, Ruecklauf = ? WHERE ID = ?",
                new DbParam("@vl", vorlauf.HasValue ? (object)vorlauf.Value : DBNull.Value),
                new DbParam("@rl", ruecklauf.HasValue ? (object)ruecklauf.Value : DBNull.Value),
                new DbParam("@id", idKessel)));
        }

        private static void AnlagenpaarSetzen(int idAnlage, int vorlauf, int ruecklauf)
        {
            if (idAnlage <= 0) return;
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET Vorlauf = ?, [Rücklauf] = ? WHERE ID = ?",
                new DbParam("@vl", vorlauf), new DbParam("@rl", ruecklauf), new DbParam("@id", idAnlage)));
        }

        private static int Zahl(object v)
            => (v == null || v == DBNull.Value) ? 0 : Convert.ToInt32(v);
    }
}
