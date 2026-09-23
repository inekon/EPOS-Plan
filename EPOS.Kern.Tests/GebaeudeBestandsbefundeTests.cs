using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Altweg;
using WPPlan.Core;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe GB der Gebäudesimulation — die Bestandsbefunde des Tagesbilanz-Wegs</b>
    /// (Umsetzungskonzept Gebäudesimulation, Kapitel 4 Zeile GB; Befunde D, G, X 3.4;
    /// Register U9/E28). Dieser Teil rechnet ohne Datenbank: der Zustand der Vortemperatur
    /// und die Ferienmaske.
    ///
    /// <para>Die Fälle mit Datenbank — Ferienabsenkung im Jahreslauf, die Grenze von 100
    /// Gebäuden, Reihenfolgeunabhängigkeit, der gesäte Wert von Gebäude 10576 — stehen in
    /// <see cref="GebaeudeBestandsbefundeDatenbankTests"/>.</para>
    /// </summary>
    public class GebaeudeBestandsbefundeTests
    {
        // =====================================================================
        //  1 — Die Vortemperatur ist Instanzzustand
        // =====================================================================

        /// <summary>
        /// <see cref="BhkwPlan"/> und die Physik des Tagesbilanz-Wegs
        /// (<see cref="TagesbilanzPhysik"/>, seit Stufe G1.0 im Modul <c>Altweg/</c>) tragen
        /// keinen veränderlichen statischen Zustand — nur Konstanten. Ein neues statisches
        /// Feld fiele hier auf, bevor es über Gebäude, Projekte oder Fäden hinweg ein
        /// Ergebnis trägt.
        /// </summary>
        [Theory]
        [InlineData(typeof(BhkwPlan))]
        [InlineData(typeof(TagesbilanzPhysik))]
        public void BhkwPlan_hat_kein_veraenderliches_statisches_Feld(Type typ)
        {
            FieldInfo[] felder = typ
                .GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                .Where(f => !f.IsLiteral)
                .ToArray();

            Assert.True(felder.Length == 0,
                "Statische Felder in " + typ.Name + ": " + string.Join(", ", felder.Select(f => f.Name)));
        }

        /// <summary>
        /// Zwei Zustände sind voneinander getrennt: Ein Aufruf schreibt nur in den
        /// übergebenen Zustand, der andere bleibt, wie er war.
        /// </summary>
        [Fact]
        public void Ein_Aufruf_schreibt_nur_in_seinen_eigenen_Zustand()
        {
            var a = new Tagesbilanzzustand();
            var b = new Tagesbilanzzustand();

            Tag(a, day: 1, aussenTemp: -5.0);
            Assert.NotEqual(0.0, a.Vortemperatur);
            Assert.Equal(0.0, b.Vortemperatur);

            double vorher = a.Vortemperatur;
            Tag(b, day: 1, aussenTemp: 10.0);
            Assert.Equal(vorher, a.Vortemperatur);

            a.ResetState();
            Assert.Equal(0.0, a.Vortemperatur);
        }

        /// <summary>
        /// <b>Warum die Umstellung ergebnisneutral ist.</b> Tag 1 beginnt immer mit dem
        /// Nachtsollwert, gleich was der Zustand vorher trug — der Jahreslauf beginnt mit
        /// Tag 1, der Vorlauf davor (Tage 351–365) erreicht deshalb kein Ergebnis.
        /// </summary>
        [Fact]
        public void Tag_1_haengt_nicht_am_mitgebrachten_Zustand()
        {
            var frisch = new Tagesbilanzzustand();

            var vorbelastet = new Tagesbilanzzustand();
            for (int day = 351; day <= 365; day++) Tag(vorbelastet, day, aussenTemp: -12.0);
            Assert.NotEqual(0.0, vorbelastet.Vortemperatur);

            double a = Tag(frisch, day: 1, aussenTemp: -3.3);
            double b = Tag(vorbelastet, day: 1, aussenTemp: -3.3);

            Assert.Equal(a, b);
            Assert.Equal(frisch.Vortemperatur, vorbelastet.Vortemperatur);
        }

        /// <summary>Ohne Zustand gibt es keine Rechnung — kein stiller Rückfall auf einen globalen Wert.</summary>
        [Fact]
        public void Ohne_Zustand_wird_benannt_abgelehnt()
        {
            Assert.Throws<ArgumentNullException>(() => Tag(null, day: 1, aussenTemp: 0.0));
        }

        private static double Tag(Tagesbilanzzustand zustand, int day, double aussenTemp)
        {
            return TagesbilanzPhysik.TaeglHeizlastWG(
                zustand, day, weAbsenkung: 0, weTemp: 20.0, ferienAbsenkung: 0, ferienTemp: 20.0,
                raumsolltempTag: 20.0, raumsolltempNacht: 16.0,
                innereGewinne: 200.0, solareGewinne: 50.0,
                spezWaermeverluste: 137.37, gebaeudeKapazitaet: 9_000.0,
                aussenTemp: aussenTemp, maxRaumtemp: 24.0,
                gesamtflaeche: 120.0, wohnflaeche: 120.0);
        }

        // =====================================================================
        //  2 — Die Ferienmaske: dieselbe Lesart, aber benannte Warnungen
        // =====================================================================

        private const int GEBAEUDE = 4711;

        private static ProjektGebaeudeModel Gebaeude(double ferien = 1.0) => new ProjektGebaeudeModel
        {
            ID_Gebaeude = GEBAEUDE,
            Gebaeudename = "Probegebäude",
            Ferien = ferien,
            Raumsolltemperatur_Ferien = 12.0,
            Ferienbeginn_1 = 366.0, // „aus"
        };

        private static (bool[] maske, List<KeyValuePair<string, string>> warnungen) Maske(ProjektGebaeudeModel item)
        {
            var maske = new bool[365];
            for (int i = 0; i < maske.Length; i++) maske[i] = true; // wird vollständig überschrieben
            var warnungen = new List<KeyValuePair<string, string>>();
            TagesbilanzRechenweg.FerienmaskeBilden(item, maske,
                (s, t) => warnungen.Add(new KeyValuePair<string, string>(s, t)));
            return (maske, warnungen);
        }

        private static string Schluessel(int zeitraum, string art) =>
            "Ferienmaske|" + GEBAEUDE + "|" + zeitraum + "|" + art;

        [Fact]
        public void Ohne_aktiven_Fahrplan_ist_kein_Tag_abgesenkt_und_nichts_gemeldet()
        {
            var item = Gebaeude(ferien: 0.0);
            item.Ferienbeginn_2 = 10;
            item.Ferienende_2 = 400; // Unsinn, aber der Fahrplan ist aus

            var (maske, warnungen) = Maske(item);

            Assert.DoesNotContain(true, maske);
            Assert.Empty(warnungen);
        }

        /// <summary>
        /// Gültige Zeiträume ergeben Tag für Tag die Maske des Bestands: Zeitraum 1 als
        /// Jahreswechsel ohne <c>−1</c>, Zeitraum 2 als <c>Beginn − 1 … Ende</c>.
        /// </summary>
        [Fact]
        public void Gueltige_Zeitraeume_ergeben_die_Bestandsmaske_ohne_Warnung()
        {
            var item = Gebaeude();
            item.Ferienbeginn_1 = 350; item.Ferienende_1 = 10;
            item.Ferienbeginn_2 = 100; item.Ferienende_2 = 110;

            var (maske, warnungen) = Maske(item);

            var erwartet = new bool[365];
            for (int t = 350; t < 365; t++) erwartet[t] = true;
            for (int t = 0; t < 10; t++) erwartet[t] = true;
            for (int t = 99; t < 110; t++) erwartet[t] = true;

            Assert.Equal(erwartet, maske);
            Assert.Empty(warnungen);
        }

        /// <summary>
        /// Ein Ende nach Tag 365 griff bisher über das Feld <c>bool[365]</c> hinaus und brach
        /// den Lauf mit <c>IndexOutOfRangeException</c> ab. Jetzt wird der Teil innerhalb
        /// des Jahres abgesenkt und benannt gewarnt.
        /// </summary>
        [Fact]
        public void Ein_Ende_nach_Tag_365_bricht_nicht_ab_sondern_warnt()
        {
            var item = Gebaeude();
            item.Ferienbeginn_2 = 360; item.Ferienende_2 = 366;

            var (maske, warnungen) = Maske(item);

            Assert.Equal(6, maske.Count(t => t));          // Tage 359 … 364
            Assert.True(maske[359] && maske[364]);
            var w = Assert.Single(warnungen);
            Assert.Equal(Schluessel(2, "ausserhalb"), w.Key);
            Assert.Contains("Probegebäude", w.Value);
        }

        /// <summary>
        /// Zeitraum 1 mit Beginn vor dem Ende senkt als Jahreswechsel gelesen das ganze
        /// Jahr ab — gerechnet wie bisher, aber jetzt gemeldet.
        /// </summary>
        [Fact]
        public void Zeitraum_1_mit_Beginn_vor_Ende_senkt_das_ganze_Jahr_und_warnt()
        {
            var item = Gebaeude();
            item.Ferienbeginn_1 = 100; item.Ferienende_1 = 200;

            var (maske, warnungen) = Maske(item);

            Assert.DoesNotContain(false, maske);
            var w = Assert.Single(warnungen);
            Assert.Equal(Schluessel(1, "jahreswechsel"), w.Key);
        }

        [Fact]
        public void Zeitraum_1_ueber_das_Jahresende_hinaus_warnt_zweifach_und_bricht_nicht_ab()
        {
            var item = Gebaeude();
            item.Ferienbeginn_1 = 350; item.Ferienende_1 = 400;

            var (maske, warnungen) = Maske(item);

            Assert.DoesNotContain(false, maske);
            Assert.Equal(new[] { Schluessel(1, "ausserhalb"), Schluessel(1, "jahreswechsel") },
                         warnungen.Select(w => w.Key).ToArray());
        }

        [Fact]
        public void Ein_Beginn_des_Zeitraums_1_ausserhalb_des_Jahres_wirkt_nicht_und_wird_gemeldet()
        {
            var item = Gebaeude();
            item.Ferienbeginn_1 = -5; item.Ferienende_1 = 10;

            var (maske, warnungen) = Maske(item);

            Assert.DoesNotContain(true, maske);
            Assert.Equal(Schluessel(1, "ausserhalb"), Assert.Single(warnungen).Key);
        }

        [Theory]
        [InlineData(200.0, 150.0)]   // Beginn nach Ende
        [InlineData(50.0, 0.0)]      // nur der Beginn
        [InlineData(0.0, 50.0)]      // nur das Ende
        public void Ein_Zeitraum_ohne_Ferientag_wird_benannt(double beginn, double ende)
        {
            var item = Gebaeude();
            item.Ferienbeginn_3 = beginn; item.Ferienende_3 = ende;

            var (maske, warnungen) = Maske(item);

            Assert.DoesNotContain(true, maske);
            var w = Assert.Single(warnungen);
            Assert.Equal(Schluessel(3, "ohnewirkung"), w.Key);
            Assert.Contains("Probegebäude", w.Value);
        }

        /// <summary>0 und 366 heißen „nicht belegt" — so steht es in allen Referenzgebäuden.</summary>
        [Fact]
        public void Nicht_belegte_Zeitraeume_bleiben_still()
        {
            var item = Gebaeude();
            item.Ferienbeginn_1 = 366; item.Ferienende_1 = 0;
            item.Ferienbeginn_2 = 366; item.Ferienende_2 = 0;
            item.Ferienbeginn_3 = 0; item.Ferienende_3 = 0;
            item.Ferienbeginn_4 = 0; item.Ferienende_4 = 366;

            var (maske, warnungen) = Maske(item);

            Assert.DoesNotContain(true, maske);
            Assert.Empty(warnungen);
        }
    }

    /// <summary>
    /// <b>Stufe GB — die Fälle mit Datenbank.</b> Gerechnet wird über
    /// <c>SimulationWaermebedarf.HeizwaermeEinesGebaeudes</c>, also genau den Rumpf, den
    /// der Lauf je Gebäude ruft. Die Gebäudezeile wird nur im Speicher verändert und dort
    /// ausdrücklich auf den Tagesbilanz-Weg gestellt — diese Fälle prüfen den Altweg, und
    /// ohne Angabe rechnet ein Gebäude nach VDI 6007; die Arbeitskopie der Testdatenbank
    /// bleibt unberührt.
    /// </summary>
    [Collection("Testdatenbank")]
    public class GebaeudeBestandsbefundeDatenbankTests : IClassFixture<TestDatenbank>
    {
        private readonly TestDatenbank _db;

        public GebaeudeBestandsbefundeDatenbankTests(TestDatenbank db) { _db = db; }

        private static int Klimaregion(int idProjekt)
        {
            var ctrl = new ProjektCtrl();
            ctrl.ReadSingle(idProjekt);
            return ctrl.m_ID_Klimaregion;
        }

        private static SimulationWaermebedarf NeueRechnung(int idProjekt)
        {
            var sim = new SimulationWaermebedarf { m_ID_Projekt = idProjekt };
            sim.KlimakalenderLesen(Klimaregion(idProjekt));
            return sim;
        }

        /// <summary>
        /// Die Gebäudezeile <paramref name="nummer"/> des Projekts, frisch gelesen und im
        /// Speicher auf den Tagesbilanz-Weg gestellt.
        /// </summary>
        private static ProjektGebaeudeModel Zeile(int idProjekt, int nummer)
        {
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(idProjekt);
            ProjektGebaeudeModel item = ctrl.items[nummer];
            item.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_TAGESBILANZ;
            return item;
        }

        private static double[] Rechne(SimulationWaermebedarf sim, ProjektGebaeudeModel item, int index = 0)
        {
            var werte = new double[8760];
            Assert.True(sim.HeizwaermeEinesGebaeudes(item, index, werte));
            return werte;
        }

        private static double Summe(double[] werte, int vonStunde, int bisStunde)
        {
            double s = 0;
            for (int h = vonStunde; h < bisStunde; h++) s += werte[h];
            return s;
        }

        // =====================================================================
        //  1 — Die Ferienabsenkung folgt im Jahreslauf der Maske
        // =====================================================================

        private static ProjektGebaeudeModel MitFerien(double beginn, double ende)
        {
            ProjektGebaeudeModel item = Zeile(1007, 0);
            item.Ferien = 1;
            item.Raumsolltemperatur_Ferien = 12;
            item.Ferienbeginn_1 = 366;          // Zeitraum 1 aus
            item.Ferienbeginn_2 = beginn;
            item.Ferienende_2 = ende;
            return item;
        }

        /// <summary>
        /// Ferien im Dezember lassen Januar bis November <b>bitgleich</b>. Bis zur Stufe GB
        /// behielt der Jahreslauf die Ferienabsenkung des letzten Vorlauftags (Tag 365) —
        /// Dezemberferien senkten damit das ganze Jahr ab (Befund X 3.4 Punkt 8).
        /// </summary>
        [Fact]
        public void Dezemberferien_lassen_Januar_bis_November_unberuehrt()
        {
            if (!_db.Vorhanden) return;

            double[] ohne = Rechne(NeueRechnung(1007), Zeile(1007, 0));
            double[] mit = Rechne(NeueRechnung(1007), MitFerien(335, 365)); // Tage 334 … 364

            int ersterFerientag = 334 * 24;
            for (int h = 0; h < ersterFerientag; h++)
                Assert.True(ohne[h] == mit[h], $"Stunde {h}: {ohne[h]} statt {mit[h]}");

            Assert.True(Summe(mit, ersterFerientag, 8760) < Summe(ohne, ersterFerientag, 8760),
                        "Im Dezember muss die Absenkung wirken.");
        }

        /// <summary>
        /// Ferien im Januar senken den Januar. Bis zur Stufe GB wirkten sie gar nicht, weil
        /// der letzte Vorlauftag kein Ferientag war.
        /// </summary>
        [Fact]
        public void Januarferien_senken_den_Januar()
        {
            if (!_db.Vorhanden) return;

            double[] ohne = Rechne(NeueRechnung(1007), Zeile(1007, 0));
            double[] mit = Rechne(NeueRechnung(1007), MitFerien(1, 31));    // Tage 0 … 30

            Assert.True(Summe(mit, 0, 31 * 24) < Summe(ohne, 0, 31 * 24),
                        "Im Januar muss die Absenkung wirken.");
        }

        /// <summary>
        /// Eine ungültige Ferieneingabe wird je Gebäude und Art nur einmal gemeldet, obwohl
        /// die Verbrauchsrückrechnung die Maske je Gebäude zweimal bildet.
        /// </summary>
        [Fact]
        public void Eine_Ferienwarnung_steht_je_Gebaeude_nur_einmal_im_Protokoll()
        {
            if (!_db.Vorhanden) return;

            ProjektGebaeudeModel item = MitFerien(360, 400);
            item.Einheit = "Verbrauch  [MWh/a]";
            item.Z_AuswahlWohnflaeche = 20;

            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            try
            {
                Rechne(NeueRechnung(1007), item);
                Assert.Single(protokoll.Warnungen);
            }
            finally
            {
                SimulationProtokoll.NeuStarten();
            }
        }

        // =====================================================================
        //  2 — Keine feste Grenze von 100 Gebäuden (U9, E28)
        // =====================================================================

        /// <summary>
        /// Der Merkplatz 150 liefert bitgleich dieselbe Reihe wie der Merkplatz 0 — auch
        /// auf dem Weg der Verbrauchsrückrechnung, der den Merkplatz liest. Bis zur Stufe GB
        /// war das Feld <c>double[100]</c>, und jeder Index ab 100 brach mit
        /// <c>IndexOutOfRangeException</c> ab.
        /// </summary>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Ein_Gebaeude_jenseits_der_hundertsten_Zeile_rechnet_wie_das_erste(bool verbrauch)
        {
            if (!_db.Vorhanden) return;

            ProjektGebaeudeModel Probe()
            {
                ProjektGebaeudeModel item = Zeile(1007, 0);
                if (verbrauch)
                {
                    item.Einheit = "Verbrauch  [MWh/a]";
                    item.Z_AuswahlWohnflaeche = 20;
                }
                return item;
            }

            double[] erstes = Rechne(NeueRechnung(1007), Probe(), index: 0);
            var sim = NeueRechnung(1007);
            double[] spaetes = Rechne(sim, Probe(), index: 150);

            Assert.Equal(erstes, spaetes);
            Assert.True(sim.Tagesbilanzweg.HeizwaermebedarfGeb.Length >= 151);
        }

        /// <summary>
        /// Der Lauf rechnet alle Gebäude des Projekts (1039: drei, auf dem VDI-Weg je ein
        /// Ergebnis), und das tote Feld <c>MaxP</c> ist fort. Das Wachsen des Merkplatzes auf
        /// dem Tagesbilanz-Weg prüft der Fall mit Merkplatz 150.
        /// </summary>
        [Fact]
        public void Der_Lauf_dimensioniert_den_Merkplatz_nach_der_Gebaeudezahl()
        {
            if (!_db.Vorhanden) return;

            var sim = new SimulationWaermebedarf();
            sim.Waermebedarf_berechnen(1039, Klimaregion(1039));

            Assert.Equal(3, sim.Anzahl_Gebaeude);
            Assert.Equal(3, sim.GebaeudeErgebnisse.Alle.Count);
            Assert.Null(typeof(SimulationWaermebedarf).GetField("MaxP",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic));
        }

        // =====================================================================
        //  3 — Das Ergebnis hängt nicht an der Zeilenreihenfolge
        // =====================================================================

        /// <summary>
        /// Das dritte Gebäude von 1039 liefert nach den beiden anderen bitgleich dieselbe
        /// Reihe wie allein gerechnet — der Zustand der Vortemperatur wandert nicht mehr
        /// von Gebäude zu Gebäude.
        /// </summary>
        [Fact]
        public void Ein_Gebaeude_rechnet_nach_anderen_wie_allein()
        {
            if (!_db.Vorhanden) return;

            var folge = NeueRechnung(1039);
            Rechne(folge, Zeile(1039, 0), index: 0);
            Rechne(folge, Zeile(1039, 1), index: 1);
            double[] nachAnderen = Rechne(folge, Zeile(1039, 2), index: 2);

            double[] allein = Rechne(NeueRechnung(1039), Zeile(1039, 2), index: 0);

            Assert.Equal(allein, nachAnderen);
        }

        // =====================================================================
        //  4 — Die gesäte Bauweise von Gebäude 10576 (Befund D)
        // =====================================================================

        /// <summary>
        /// Gebäude 10576 (Projekt 1008, 304 m²) trägt die wirksame Wärmekapazität
        /// 50 Wh/(m²K) × 304 m² = 15 200 Wh/K statt des Rückfallwerts 50 Wh/K. Der Wert
        /// gehört zu den gesäten Gebäudedaten — wer ihn ändert, friert die Basis neu ein
        /// (Einfrierregel „gesäte Gebäudedaten", <c>Referenzlaeufe/LIESMICH.md</c>).
        /// </summary>
        [Fact]
        public void Gebaeude_10576_traegt_die_korrigierte_Bauweise()
        {
            if (!_db.Vorhanden) return;

            object wert = DataRepository.ExecuteScalar(
                "SELECT Bauweise FROM Tab_Gebaeude WHERE ID = ?", new DbParam("@id", 10576));

            Assert.Equal(15_200.0, Convert.ToDouble(wert));
        }
    }
}
