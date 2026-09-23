using System;
using System.Linq;
using System.Reflection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Altweg;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G1.0 der Gebäudesimulation — Fassade, Vorbereitung und Weiche</b>
    /// (Entscheid E20, ADR-006; Umsetzungskonzept 1.1, 1.5). Ohne Datenbank: die Regel der
    /// Weiche, der modellfreie Vorbereitungsschritt und der Zuschnitt des Klimakalenders.
    ///
    /// <para><b>Regel der Weiche in G1.0:</b> <c>TAGESBILANZ</c> führt auf den
    /// Tagesbilanz-Weg; <c>NULL</c> und <c>VDI6007</c> gehören nach E1 dem VDI-Weg, der erst
    /// mit der Anbindung in G1 entsteht — bis dahin rechnet <b>jedes</b> Gebäude auf dem
    /// Tagesbilanz-Weg. Die Anbindung ändert die beiden letzten Fälle bewusst.</para>
    /// </summary>
    public class GebaeudeWeicheTests
    {
        [Theory]
        [InlineData(DbWerte.GEBAEUDE_MODELL_TAGESBILANZ)]
        [InlineData(null)]
        [InlineData(DbWerte.GEBAEUDE_MODELL_VDI6007)]
        public void In_G1_0_fuehrt_die_Weiche_jedes_Gebaeude_auf_den_Tagesbilanz_Weg(string modell)
        {
            var sim = new SimulationWaermebedarf();
            var item = new ProjektGebaeudeModel { Gebaeude_Modell = modell };

            IGebaeudeRechenweg weg = sim.RechenwegWaehlen(item);

            Assert.IsType<TagesbilanzRechenweg>(weg);
            Assert.Same(sim.Tagesbilanzweg, weg);
        }

        [Fact]
        public void Die_Vorbereitung_rechnet_den_Verbrauch_um_und_schreibt_die_Zeile_nicht()
        {
            var item = new ProjektGebaeudeModel
            {
                Einheit = "Ölverbrauch [l/a]",
                Z_AuswahlWohnflaeche = 1000.0,
                Jahresnutzungsgrad = 0.9,
                Wohnflaeche_gesamt = 140.0,
                Flaeche_Nutzer = 35.0,
            };

            GebaeudeVorbereitung v = GebaeudeVorbereitung.Bilden(null, item);

            Assert.False(v.IstFlaeche);
            Assert.Equal(1000.0 * 0.9 * 10.08, v.VerbrauchNeu);
            Assert.Equal(140.0, v.FlaecheAlt);
            Assert.Equal(35.0, v.Flaeche_Nutzer);
            Assert.Equal(0.9, v.Jahresnutzungsgrad);
            Assert.Equal(1000.0, item.Z_AuswahlWohnflaeche);
        }

        [Fact]
        public void Bei_einer_Flaechenangabe_gibt_es_keinen_Verbrauch()
        {
            var item = new ProjektGebaeudeModel { Einheit = GebaeudeVorbereitung.EINHEIT_FLAECHE, Z_AuswahlWohnflaeche = 120.0 };

            GebaeudeVorbereitung v = GebaeudeVorbereitung.Bilden(null, item);

            Assert.True(v.IstFlaeche);
            Assert.Equal(0.0, v.VerbrauchNeu);
        }

        /// <summary>
        /// Über die Naht bekommt ein Rechenweg allein den gemeinsamen Teil des Kalenders
        /// (F-Ü3) — die Tagesmittel und Tagestypen des Altwegs stehen nicht darin.
        /// </summary>
        [Fact]
        public void Die_Naht_reicht_nur_den_gemeinsamen_Teil_des_Kalenders()
        {
            MethodInfo rechnen = typeof(IGebaeudeRechenweg).GetMethod("Rechnen");
            Assert.NotNull(rechnen);
            Assert.Equal(typeof(KlimakalenderGemeinsam), rechnen.GetParameters()[3].ParameterType);

            string[] namen = typeof(KlimakalenderGemeinsam)
                .GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .Select(m => m.Name).ToArray();
            foreach (string altweg in new[] { "Sol_N", "Sol_w", "Sol_O", "Sol_S", "A_Temp", "TagTyp_W", "TagTyp_NW" })
                Assert.DoesNotContain(namen, n => n.Contains(altweg, StringComparison.Ordinal));
        }
    }

    /// <summary>
    /// <b>Stufe G1.0 — die Fälle mit Datenbank.</b> Der Rechenweg wird gelesen
    /// (<c>Gebaeude_Modell</c>, in der Testdatenbank NULL) und ändert in G1.0 nichts: Ein
    /// ausdrücklich auf Tagesbilanz gestelltes Gebäude rechnet bitgleich wie dasselbe
    /// Gebäude ohne Angabe — auch auf dem Weg der Verbrauchs-Rückrechnung.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeWeicheDatenbankTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public GebaeudeWeicheDatenbankTests(TestDatenbank db) { _db = db; }

        private static double[] Rechne(int idProjekt, string modell, bool verbrauch)
        {
            var projekt = new ProjektCtrl();
            projekt.ReadSingle(idProjekt);

            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);

            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            ProjektGebaeudeModel item = ctrl.items[0];
            Assert.Null(item.Gebaeude_Modell);   // die Testdatenbank führt die Spalte NULL
            item.Gebaeude_Modell = modell;
            if (verbrauch)
            {
                item.Einheit = "Verbrauch  [MWh/a]";
                item.Z_AuswahlWohnflaeche = 20;
            }

            var werte = new double[8760];
            Assert.True(sim.HeizwaermeEinesGebaeudes(item, 0, werte));
            return werte;
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Tagesbilanz_ausdruecklich_rechnet_wie_ohne_Angabe(bool verbrauch)
        {
            if (!_db.Vorhanden) return;

            double[] ohne = Rechne(1007, null, verbrauch);
            double[] tagesbilanz = Rechne(1007, DbWerte.GEBAEUDE_MODELL_TAGESBILANZ, verbrauch);

            Assert.True(ohne.Sum() > 0, "Das Probegebäude heizt gar nicht — der Fall prüft dann nichts.");
            Assert.Equal(ohne, tagesbilanz);
        }
    }
}
