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
    /// <b>Der Rechenweg des Kältebedarfs ohne Gebäudemodell, Stufe K3</b> (Konzept Kältebedarf 4.1–4.4, 5): EER fest und
    /// linear samt Klemmung und Untergrenze, Buchung zentral und dezentral (Split) an der Fassade, die Abzugsstelle vor der
    /// Kaskade, Summen und Monatswerte, Bedarfs- und Deckungsprobe ohne Verletzung, der Split-Strom in der Stufenrechnung,
    /// die Ergebnisspalten nur mit Wert, ohne Kühlbetrieb leer, das Warnkriterium und die Senkenvorbelegung.
    /// </summary>
    public class KaeltebedarfRechenwegTests
    {
        private const int STUNDEN = 8760;

        private static Kaeltedeckung Fest(double eer, string name = "Split fest") => Kaeltedeckung.AusProfilzeile(
            new Z_ProjektKaeltebedarfModel
            {
                Bezeichner = name, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT, EerWeg = KaeltebedarfSchema.EER_FEST,
                Eer1 = eer, Taussen1 = 35,
            });

        private static Kaeltedeckung Linear() => Kaeltedeckung.AusLastgang(new Z_ProjWaermebedarfModel
        {
            m_szBezeichner = "Split linear", Kanal = DbWerte.KANAL_KUEHLUNG, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
            EerWeg = KaeltebedarfSchema.EER_LINEAR, Eer1 = 3, Taussen1 = 35, Eer2 = 4, Taussen2 = 25,
        });

        /// <summary>Der feste Text einer Vorlage zwischen {0} und {1} — unterscheidet die Meldungen in jeder Sprache.</summary>
        private static string Teil(string vorlage)
        {
            int a = vorlage.IndexOf("{0}", StringComparison.Ordinal) + 3;
            return vorlage.Substring(a, vorlage.IndexOf("{1}", StringComparison.Ordinal) - a);
        }

        private static double[] Konstant(double w) => Enumerable.Repeat(w, STUNDEN).ToArray();

        /// <summary>Außentemperatur im Wechsel 20 / 30 / 40 °C — unter, in und über dem linearen Band.</summary>
        private static double[] Temperaturen() => Enumerable.Range(0, STUNDEN).Select(h => 20.0 + 10.0 * (h % 3)).ToArray();

        [Fact]
        public void Der_EER_ist_fest_oder_linear_geklemmt_und_nie_unter_der_Untergrenze()
        {
            Assert.Equal(3.2, Fest(3.2).Eer(-10), 12);
            Assert.Equal(3.2, Fest(3.2).Eer(45), 12);

            Kaeltedeckung lin = Linear();
            Assert.Equal(3.5, lin.Eer(30), 12);
            Assert.Equal(3.0, lin.Eer(35), 12);
            Assert.Equal(4.0, lin.Eer(25), 12);
            Assert.Equal(3.0, lin.Eer(48), 12);                 // über max(T1,T2): Randwert
            Assert.Equal(4.0, lin.Eer(-5), 12);                 // unter min(T1,T2): Randwert

            Assert.Equal(KaeltebedarfFestwerte.EER_MIN, Fest(0.5).Eer(30), 12);
            Assert.Equal(1.0, KaeltebedarfFestwerte.EER_MIN);

            // Ohne gültige Angaben rechnet die Zeile nicht - NaN, kein stiller Wert.
            Kaeltedeckung ohne = Kaeltedeckung.AusProfilzeile(new Z_ProjektKaeltebedarfModel
            {
                Bezeichner = "ohne", Deckung = KaeltebedarfSchema.DECKUNG_SPLIT, EerWeg = KaeltebedarfSchema.EER_FEST,
            });
            Assert.NotNull(ohne.Pruefgrund);
            Assert.True(double.IsNaN(ohne.Eer(30)));

            // Zentral und ausserhalb des Kuehlkanals ist es die Vorgabe.
            Assert.Same(Kaeltedeckung.Zentral, Kaeltedeckung.AusProfilzeile(new Z_ProjektKaeltebedarfModel()));
            Assert.Same(Kaeltedeckung.Zentral, Kaeltedeckung.AusLastgang(new Z_ProjWaermebedarfModel
            {
                Kanal = DbWerte.KANAL_HEIZUNG, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT, EerWeg = KaeltebedarfSchema.EER_FEST, Eer1 = 3, Taussen1 = 35,
            }));
        }

        [Fact]
        public void Die_Fassade_bucht_zentral_und_dezentral_und_zieht_vor_der_Kaskade_ab()
        {
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            var kanaele = new Kanalsatz();
            var kaelte = new SimulationKaeltebedarf();
            kaelte.Beginnen(kanaele, true);
            double[] theta = Temperaturen();
            kaelte.Aussentemperatur = theta;

            kaelte.BedarfBuchen(Konstant(1.0), Kaeltedeckung.Zentral, true);   // Profil zentral
            kaelte.BedarfBuchen(Konstant(0.5), Fest(2.5), true);               // Profil Split fest
            kaelte.BedarfBuchen(Konstant(0.3), Linear(), false);               // Lastgang Split linear
            kaelte.GanglinieBuchen(Konstant(0.2));                             // Lastgang zentral (Bestandsweg)

            var mo = new int[12]; var moEnde = new int[12];
            WPPlan.Core.BhkwPlan.MonatsGrenzen(mo, moEnde);
            Assert.True(kaelte.Abschliessen(mo, moEnde));
            Assert.True(kaelte.Gerechnet);
            Assert.Equal(0, kaelte.Bedarfsprobe_Verletzungen);

            // Der Kanal bleibt die Summe aller Kälte; Posten je Herkunft.
            Assert.Equal(2.0 * STUNDEN / 1000.0, kaelte.Kaeltebedarf_Gesamt, 9);
            Assert.Equal(1.5 * STUNDEN / 1000.0, kaelte.Kaeltebedarf_Profil_Gesamt, 9);
            Assert.Equal(0.5 * STUNDEN / 1000.0, kaelte.Kaeltebedarf_Extern_Gesamt, 9);
            Assert.Equal(0.8 * STUNDEN / 1000.0, kaelte.Kaeltebedarf_Dezentral_Gesamt, 9);
            Assert.Equal(0.8, kaelte.Kaelte_Dezentral_Max, 12);
            Assert.Equal(kaelte.Kaeltebedarf_Gesamt, kaelte.Kaeltebedarf_Monat.Sum(), 9);
            Assert.True(kaelte.MitProfil);
            Assert.Equal(2, kaelte.DezentraleZeilen.Count);

            // Abzugsstelle: Kaskade = Kanal minus dezentral; der Rest vor der Kaskade ist nur der zentrale Teil.
            double[] kaskade = kaelte.BedarfDerKaskade();
            for (int h = 0; h < STUNDEN; h++)
            {
                Assert.Equal(kaelte.Kaeltebedarf[h] - kaelte.Kaeltebedarf_Dezentral[h], kaskade[h], 12);
                Assert.Equal(kaskade[h], kaelte.BedarfDerKaskade(h, kaelte.Kaeltebedarf[h]), 12);
            }
            Assert.Equal(1.2 * STUNDEN / 1000.0, kaelte.Kaelterestbedarf, 9);

            // Strom je Stunde = Q / EER(θ); Summe = Buchung, Jahres-EER = Σ Q / Σ P.
            double strom = 0;
            Kaeltedeckung lin = Linear();
            for (int h = 0; h < STUNDEN; h++)
            {
                double p = 0.5 / 2.5 + 0.3 / lin.Eer(theta[h]);
                Assert.Equal(p, kaelte.Strom_Dezentral[h], 12);
                strom += p;
            }
            Assert.Equal(strom / 1000.0, kaelte.Strom_Dezentral_Gesamt, 9);
            Assert.Equal(kaelte.Strom_Dezentral_Gesamt * 1000.0,
                         kaelte.DezentraleZeilen.Sum(z => z.StromKwh), 6);
            Assert.Equal(kaelte.Kaeltebedarf_Dezentral_Gesamt / kaelte.Strom_Dezentral_Gesamt, kaelte.Jahres_EER_Dezentral, 12);

            // Laufmeldung mit Menge; der EER liegt über 2 - kein Hinweis darauf.
            Assert.Single(protokoll.Hinweise, h => h.Contains(Teil(WindowsFormsApplication1.MyResource.Resource.SIMENG_KAELTE_DEZENTRAL)));
            Assert.DoesNotContain(protokoll.Hinweise, h => h.Contains(Teil(WindowsFormsApplication1.MyResource.Resource.SIMENG_KAELTE_DEZENTRAL_EER_NIEDRIG)));
            Assert.Empty(protokoll.Fehler);

            // Export nur mit Wert.
            var reihen = KaelteErgebnisexport.Reihen(kaelte);
            Assert.Contains(reihen, r => r.Key == KaelteErgebnisexport.DATEI_DEZENTRAL);
            Assert.Contains(reihen, r => r.Key == KaelteErgebnisexport.DATEI_STROM_DEZENTRAL);
            var skalare = KaelteErgebnisexport.Skalare(kaelte);
            Assert.Equal(kaelte.Kaeltebedarf_Dezentral_Gesamt, skalare.Single(s => s.Key == "Kaelte.DezentralMwh").Value);
            Assert.Equal(kaelte.Kaeltebedarf_Profil_Gesamt, skalare.Single(s => s.Key == "Kaelte.ProfilMwh").Value);
            Assert.DoesNotContain(skalare, s => s.Key == "Kaelte.ErzeugungMwh");   // keine Kaskade - keine Kaskadenschlüssel
        }

        [Fact]
        public void Ohne_Split_bleibt_die_Kaskadenreihe_der_Kaeltebedarf_selbst()
        {
            SimulationProtokoll.NeuStarten();
            var kanaele = new Kanalsatz();
            var kaelte = new SimulationKaeltebedarf();
            kaelte.Beginnen(kanaele, true);
            kaelte.Aussentemperatur = Temperaturen();
            kaelte.BedarfBuchen(Konstant(1.0), Kaeltedeckung.Zentral, true);
            Assert.True(kaelte.Abschliessen(null, null));
            Assert.Same(kaelte.Kaeltebedarf, kaelte.BedarfDerKaskade());
            Assert.Equal(kaelte.Kaeltebedarf_Gesamt, kaelte.Kaelterestbedarf);
            Assert.False(kaelte.MitDezentral);
            Assert.DoesNotContain(KaelteErgebnisexport.Skalare(kaelte), s => s.Key.StartsWith("Kaelte.Dezentral", StringComparison.Ordinal));
            Assert.DoesNotContain(KaelteErgebnisexport.Reihen(kaelte), r => r.Key == KaelteErgebnisexport.DATEI_DEZENTRAL);
        }

        [Fact]
        public void Ein_niedriger_Jahres_EER_gibt_einen_Hinweis()
        {
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            var kaelte = new SimulationKaeltebedarf();
            kaelte.Beginnen(new Kanalsatz(), true);
            kaelte.Aussentemperatur = Temperaturen();
            kaelte.BedarfBuchen(Konstant(1.0), Fest(1.5), true);
            Assert.True(kaelte.Abschliessen(null, null));
            Assert.Equal(1.5, kaelte.Jahres_EER_Dezentral, 9);
            Assert.Single(protokoll.Hinweise, h => h.Contains(Teil(WindowsFormsApplication1.MyResource.Resource.SIMENG_KAELTE_DEZENTRAL_EER_NIEDRIG)));
        }

        [Fact]
        public void Ohne_Kuehlbetrieb_bleibt_die_Kaelteseite_leer_und_nennt_die_Profile()
        {
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            var kanaele = new Kanalsatz();
            var kaelte = new SimulationKaeltebedarf();
            kaelte.Beginnen(kanaele, false);
            kaelte.Aussentemperatur = Temperaturen();
            kaelte.BedarfBuchen(Konstant(1.0), Fest(3), false);
            kaelte.ProfileZaehlen(2);
            Assert.True(kaelte.Abschliessen(null, null));

            Assert.False(kaelte.Gerechnet);
            Assert.All(kanaele.Kuehlung, w => Assert.Equal(0.0, w));
            Assert.Empty(kaelte.DezentraleZeilen);
            Assert.All(kaelte.Strom_Dezentral, w => Assert.Equal(0.0, w));
            Assert.Empty(KaelteErgebnisexport.Skalare(kaelte));
            Assert.Equal(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.SIMENG_KAELTE_PROJEKT_AUS_PROFILE, 0, 1, 2),
                         Assert.Single(protokoll.Hinweise));
        }

        [Fact]
        public void Eine_Splitzeile_ohne_EER_bricht_den_Lauf_benannt_ab()
        {
            SimulationProtokoll protokoll = SimulationProtokoll.NeuStarten();
            var kaelte = new SimulationKaeltebedarf();
            kaelte.Beginnen(new Kanalsatz(), true);
            kaelte.Aussentemperatur = Temperaturen();
            Kaeltedeckung ohne = Kaeltedeckung.AusProfilzeile(new Z_ProjektKaeltebedarfModel
            {
                Bezeichner = "Serverraum", Deckung = KaeltebedarfSchema.DECKUNG_SPLIT, EerWeg = KaeltebedarfSchema.EER_FEST,
            });
            kaelte.BedarfBuchen(Konstant(1.0), ohne, true);
            Assert.False(kaelte.Abschliessen(null, null));
            Assert.False(kaelte.Gerechnet);
            Assert.Contains("Serverraum", kaelte.Fehlertext);
            Assert.Equal(kaelte.Fehlertext, Assert.Single(protokoll.Fehler));
        }

        [Fact]
        public void Kaelte_bekommt_in_der_Senkenvorbelegung_keine_Zeile()
        {
            var kanaele = new bool[Kanal.ANZAHL];
            kanaele[Kanal.KUEHLUNG] = true;
            Assert.Empty(Senkenvorbelegung.Ableiten(kanaele));
        }
    }

    /// <summary>
    /// <b>Der Rechenweg K3 im Lauf</b> auf einer Kopie der Testdatenbank (Projekt 1017 mit Kühlbetrieb): Profil- und
    /// Lastgangzeilen zentral und dezentral, die Kaskade bekommt den Kanal ohne die dezentrale Kälte, der Split-Strom steht in
    /// der Stufenrechnung, die Ergebnisspalten tragen Werte — und ohne Kühlbetrieb bleiben sie NULL.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaeltebedarfRechenwegDatenbankTests : IDisposable
    {
        private const int PROJEKT = 1017;
        private const string BUERO = "Raumkühlung Büro";
        private const string KUEHLRAUM = "Kühlraum";
        private const string LASTGANG = "K3_Kuehlung";
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        private static int StammId(string bezeichner) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?", new DbParam("@b", bezeichner)), CultureInfo.InvariantCulture);

        private static void ZeilenAnlegen(double? eerSplit = 3.0)
        {
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = StammId(BUERO), Bezeichner = BUERO, Summe = 40 },
                new Z_ProjektKaeltebedarfModel
                {
                    ID_Kaeltebedarf = StammId(KUEHLRAUM), Bezeichner = KUEHLRAUM, Summe = 12, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
                    EerWeg = KaeltebedarfSchema.EER_FEST, Eer1 = 3.0, Taussen1 = 35,
                },
            }));
            if (eerSplit == null)
                Assert.True(DataRepository.ExecuteSQL("UPDATE Z_Projekt_Kaeltebedarf SET Split_EER_1 = NULL WHERE ID_Projekt = ? AND Deckung = ?",
                    new DbParam("@p", PROJEKT), new DbParam("@d", KaeltebedarfSchema.DECKUNG_SPLIT)));

            Assert.True(new WaermebedarfStammCtrl().ImportGanglinie(LASTGANG, Enumerable.Repeat(0.5, 8760).ToList()));
            List<Z_ProjWaermebedarfModel> zeilen = Z_ProjektGebGanglinieCtrl.LiesProjekt(PROJEKT);
            zeilen.Add(new Z_ProjWaermebedarfModel
            {
                m_szBezeichner = LASTGANG, Kanal = DbWerte.KANAL_KUEHLUNG, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
                EerWeg = KaeltebedarfSchema.EER_LINEAR, Eer1 = 3, Taussen1 = 35, Eer2 = 4, Taussen2 = 25,
            });
            Assert.True(new WizardCtrl().Del_WaermebedarfExtern(PROJEKT));
            Assert.True(new WizardCtrl().Add_WaermebedarfExtern(PROJEKT, zeilen));
        }

        private static SimulationRunner Rechnen(out int kopf, out string fehler)
        {
            SimulationProtokoll.NeuStarten();
            var lauf = new SimulationRunner();
            kopf = lauf.SimuliereUndSpeichere(PROJEKT, out fehler);
            return lauf;
        }

        private static DataRow Ergebniszeile(int kopf) => DataRepository.GetDataTable(
            "SELECT " + string.Join(", ", KaeltebedarfSchema.ERGEBNISSPALTEN) + " FROM Tab_ErgebnisEnergiebedarf WHERE ID_Ergebnis = ?",
            new DbParam("@k", kopf)).Rows[0];

        [Fact]
        public void Der_Lauf_bucht_Profil_und_Split_und_der_Strom_steht_in_der_Stufenrechnung()
        {
            if (!_db.Vorhanden) return;
            ZeilenAnlegen();
            SimulationRunner lauf = Rechnen(out int kopf, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);
            Assert.DoesNotContain(lauf.Protokoll.Fehler, f => f.StartsWith("Deckungsprobe", StringComparison.Ordinal));

            SimulationKaeltebedarf k = lauf.simulation_Kaeltebedarf;
            Assert.True(k.Gerechnet);
            Assert.Equal(0, k.Bedarfsprobe_Verletzungen);
            Assert.Equal(52.0, k.Kaeltebedarf_Profil_Gesamt, 6);                    // Summen der Zeilen
            Assert.Equal(12.0 + 0.5 * 8760 / 1000.0, k.Kaeltebedarf_Dezentral_Gesamt, 6);
            Assert.Equal(k.Kaeltebedarf_Gesamt, k.Kaeltebedarf_Monat.Sum(), 6);

            // Strom: Profil fest 12 MWh / 3; Lastgang linear über der Außentemperatur des Laufs.
            double[] theta = lauf.simulation_Waermebedarf.Stundentemperatur;
            Kaeltedeckung lin = Kaeltedeckung.AusLastgang(new Z_ProjWaermebedarfModel
            {
                m_szBezeichner = LASTGANG, Kanal = DbWerte.KANAL_KUEHLUNG, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
                EerWeg = KaeltebedarfSchema.EER_LINEAR, Eer1 = 3, Taussen1 = 35, Eer2 = 4, Taussen2 = 25,
            });
            double lastgangStrom = Enumerable.Range(0, 8760).Sum(h => 0.5 / lin.Eer(theta[h]));
            Assert.Equal(12.0 / 3.0 + lastgangStrom / 1000.0, k.Strom_Dezentral_Gesamt, 6);

            // Die Kaskade bekam den Kanal ohne die dezentrale Kälte; ihre Bilanz schließt.
            Assert.NotNull(k.Kaskade);
            for (int h = 0; h < 8760; h++)
                Assert.Equal(k.Kaeltebedarf[h], k.Kaskade.Bedarf_stuendlich[h] + k.Kaeltebedarf_Dezentral[h], 9);

            // Strombilanz: der Split-Strom steht in der Stufenrechnung (Lastreihe, Kältestrom der Bilanz).
            double[] dez = lauf.sim.Kaeltestrom_Dezentral_stuendlich;
            Assert.NotNull(dez);
            Assert.Equal(k.Strom_Dezentral_Gesamt * 1000.0, dez.Sum(), 3);
            double stufe = lauf.sim.Kaeltestrom_Stufenrechnung_stuendlich.Sum();
            if (!k.Kaskade.Erzeuger.Any(e => e.NebenDerStufenrechnung))
                Assert.Equal(k.Kaskade.StromGesamtKwh + dez.Sum(), stufe, 3);
            else
                Assert.True(stufe >= dez.Sum());

            // Ergebnisspalten mit Wert (gespeichert gerundet wie jede Spalte der Ergebniszeile).
            DataRow r = Ergebniszeile(kopf);
            Assert.Equal(k.Kaeltebedarf_Profil_Gesamt, Convert.ToDouble(r["Kaeltebedarf_Profil_MWh"], CultureInfo.InvariantCulture), 2);
            Assert.Equal(k.Kaeltebedarf_Dezentral_Gesamt, Convert.ToDouble(r["Kaeltebedarf_Dezentral_MWh"], CultureInfo.InvariantCulture), 2);
            Assert.Equal(k.Kaelte_Dezentral_Max, Convert.ToDouble(r["Kaelte_Dezentral_Spitze_kW"], CultureInfo.InvariantCulture), 2);
            Assert.Equal(k.Strom_Dezentral_Gesamt, Convert.ToDouble(r["Strom_Dezentral_MWh"], CultureInfo.InvariantCulture), 2);
            Assert.Equal(k.Jahres_EER_Dezentral, Convert.ToDouble(r["Jahres_EER_Dezentral"], CultureInfo.InvariantCulture), 2);

            // Senkenvorbelegung sieht den Kühlkanal, legt aber keine Zeile dafür an.
            Assert.True(Senkenvorbelegung.KanaeleMitBedarf(PROJEKT)[Kanal.KUEHLUNG]);
        }

        [Fact]
        public void Ohne_Kuehlbetrieb_bleiben_Spalten_und_Strom_leer()
        {
            if (!_db.Vorhanden) return;
            ZeilenAnlegen();
            Assert.True(KonfigurationCtrl.KuehlbetriebSchreiben(PROJEKT, false));
            SimulationRunner lauf = Rechnen(out int kopf, out string fehler);
            Assert.True(kopf > 0, "Lauf gescheitert: " + fehler);

            Assert.False(lauf.simulation_Kaeltebedarf.Gerechnet);
            Assert.False(lauf.simulation_Kaeltebedarf.MitDezentral);
            Assert.Null(lauf.sim.Kaeltestrom_Dezentral_stuendlich);
            DataRow r = Ergebniszeile(kopf);
            foreach (string s in KaeltebedarfSchema.ERGEBNISSPALTEN) Assert.Equal(DBNull.Value, r[s]);
            Assert.Contains(lauf.Protokoll.Hinweise, h => h.Contains(
                string.Format(CultureInfo.CurrentCulture, "{0}", 2)) && h.StartsWith(
                WindowsFormsApplication1.MyResource.Resource.SIMENG_KAELTE_PROJEKT_AUS_PROFILE.Substring(0, 20), StringComparison.Ordinal));
        }

        [Fact]
        public void Eine_Splitzeile_ohne_EER_meldet_das_Warnkriterium_und_der_Lauf_bricht_ab()
        {
            if (!_db.Vorhanden) return;
            ZeilenAnlegen(eerSplit: null);
            List<Warnbefund> befunde = Warnkriterien.PruefeProjekt(PROJEKT);
            Warnbefund b = Assert.Single(befunde, x => x.Kriterium == Warnkriterien.DEZENTRALE_KAELTE_OHNE_EER);
            Assert.Contains(KUEHLRAUM, b.Text);

            SimulationRunner lauf = Rechnen(out int kopf, out string fehler);
            Assert.True(kopf <= 0);
            Assert.Contains(lauf.Protokoll.Fehler, f => f.Contains(KUEHLRAUM));
        }
    }
}
