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
    /// <b>Das Sperrprofil der Wärmepumpe</b> (Welle V14): die Stundenmaske (Altfenster wie zuvor,
    /// Übertrag über Mitternacht, Wochentage, mehrere Fenster, Heizstab), der Lauf auf einer
    /// Projektkopie mit Wärmepumpe, die Wache der Referenzprojekte, Duplizieren, Schreibweg und
    /// Pufferauslegung.
    /// </summary>
    [Collection("Testdatenbank")]
    public class SperrprofilTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        /// <summary>Projekt 1007: eine Wärmepumpe mit Heizstab (Anlage 10353).</summary>
        private const int PROJEKT = 1007;
        private const int ANLAGE = 10353;

        /// <summary>Die 16 Projekte der Referenzbasis.</summary>
        private static readonly int[] REFERENZPROJEKTE =
            { 1007, 1008, 1017, 1018, 1023, 1024, 1030, 1039, 1040, 1041, 1042, 1045, 1046, 1047, 1049, 1050 };

        // =====================================================================
        //  Die Maske
        // =====================================================================

        /// <summary>Das Altfenster ergibt genau die Stunden der bisherigen Inline-Bedingung.</summary>
        [Theory]
        [InlineData(true, 14, 17)]
        [InlineData(true, 22, 2)]
        [InlineData(true, 0, 24)]
        [InlineData(false, 14, 17)]
        public void Das_Altfenster_ist_die_bisherige_Bedingung(bool sperrung, int von, int bis)
        {
            Sperrprofil p = Sperrprofil.Bilden(sperrung, von, bis, null, 3);
            for (int h = 0; h < Sperrprofil.STUNDEN; h++)
            {
                int std = h % 24;
                Assert.Equal(std >= von && std < bis && sperrung, p.Gesperrt(h));
                Assert.False(p.HeizstabGesperrt(h));
            }
            Assert.Equal(0, p.FensterAnzahl);
        }

        [Fact]
        public void Ohne_Fenster_und_ohne_Altfenster_ist_nichts_gesperrt()
        {
            Sperrprofil p = Sperrprofil.Bilden(false, 14, 17, new List<Sperrfenster>(), 0);
            Assert.Equal(0, p.GesperrteStunden);
            Assert.DoesNotContain(true, p.Verdichter);
        }

        [Fact]
        public void Ein_Fenster_ueber_Mitternacht_laeuft_in_den_Folgetag()
        {
            var f = new Sperrfenster { VonH = 22, DauerH = 4 };
            Sperrprofil p = Sperrprofil.Bilden(false, 0, 0, new[] { f }, 0);
            Assert.True(p.Gesperrt(22) && p.Gesperrt(23) && p.Gesperrt(24) && p.Gesperrt(25));
            Assert.False(p.Gesperrt(21));
            Assert.False(p.Gesperrt(26));
            // Der 31. Dezember läuft in den 1. Januar.
            Assert.True(p.Gesperrt(0) && p.Gesperrt(1));
            Assert.Equal(365 * 4, p.GesperrteStunden);
            Assert.True(p.HeizstabGesperrt(23));
        }

        [Fact]
        public void Die_Wochentage_gelten_ab_dem_Wochentag_des_ersten_Januar()
        {
            // Nur Samstag (Bit 6 = 32); der 1. Januar ist ein Donnerstag (3) -> Samstag ist Tag 2.
            var f = new Sperrfenster { VonH = 11, DauerH = 2, Wochentage = 32, HeizstabGesperrt = false };
            Sperrprofil p = Sperrprofil.Bilden(false, 0, 0, new[] { f }, 3);
            Assert.False(p.Gesperrt(0 * 24 + 11));
            Assert.False(p.Gesperrt(1 * 24 + 11));
            Assert.True(p.Gesperrt(2 * 24 + 11));
            Assert.True(p.Gesperrt(2 * 24 + 12));
            Assert.False(p.Gesperrt(2 * 24 + 13));
            Assert.True(p.Gesperrt(9 * 24 + 11));
            Assert.False(p.HeizstabGesperrt(2 * 24 + 11));
            Assert.Equal(52 * 2, p.GesperrteStunden);   // 52 Samstage im Rechenjahr ab Donnerstag
        }

        [Fact]
        public void Zwei_Fenster_und_das_Altfenster_ergaenzen_sich()
        {
            var fenster = SperrfensterCtrl.Vorlage("ZWEI_MAL_ZWEI");
            Assert.Equal(2, fenster.Count);
            Sperrprofil p = Sperrprofil.Bilden(true, 2, 3, fenster, 0);
            Assert.Equal(365 * 5, p.GesperrteStunden);
            Assert.True(p.Gesperrt(2) && p.Gesperrt(11) && p.Gesperrt(12) && p.Gesperrt(17) && p.Gesperrt(18));
            Assert.False(p.HeizstabGesperrt(2));
            Assert.True(p.HeizstabGesperrt(11));
            Assert.Equal(2, p.FensterAnzahl);
        }

        [Fact]
        public void Ein_halbes_Fenster_sperrt_die_Stunden_deren_Beginn_darin_liegt()
        {
            Sperrprofil p = Sperrprofil.Bilden(false, 0, 0, new[] { new Sperrfenster { VonH = 11.5, DauerH = 2 } }, 0);
            Assert.False(p.Gesperrt(11));
            Assert.True(p.Gesperrt(12) && p.Gesperrt(13));
            Assert.False(p.Gesperrt(14));
        }

        // =====================================================================
        //  Datenbank
        // =====================================================================

        /// <summary>Kein Referenzprojekt trägt ein Altfenster oder ein Sperrfenster.</summary>
        [Fact]
        public void Die_Referenzprojekte_tragen_keine_Sperre()
        {
            if (!_db.Vorhanden) return;
            string liste = string.Join(",", REFERENZPROJEKTE.Select(p => p.ToString(CultureInfo.InvariantCulture)));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Energieanlagen WHERE Sperrung <> 0 AND ID_Projekt IN (" + liste + ")"));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Sperrfenster s JOIN Tab_Energieanlagen e ON e.ID = s.ID_Energieanlage " +
                                  "WHERE e.ID_Projekt IN (" + liste + ")"));
        }

        [Fact]
        public void Schreiben_ersetzt_die_Fenster_und_ueberfuehrt_das_Altfenster()
        {
            if (!_db.Vorhanden) return;
            DataRepository.ExecuteNonQuery("UPDATE Tab_Energieanlagen SET Sperrung = 1, Sperrzeit_von = 14, Sperrzeit_bis = 17 WHERE ID = ?",
                                           new DbParam("@id", ANLAGE));
            List<Sperrfenster> anzeige = SperrfensterCtrl.MitAltfenster(ANLAGE, true, 14, 17);
            Sperrfenster alt = Assert.Single(anzeige);
            Assert.Equal(14, alt.VonH);
            Assert.Equal(3, alt.DauerH);
            Assert.False(alt.HeizstabGesperrt);

            Assert.True(SperrfensterCtrl.Schreiben(ANLAGE, anzeige));
            Assert.Equal(0L, Zahl("SELECT Sperrung FROM Tab_Energieanlagen WHERE ID = " + ANLAGE));
            Sperrfenster gelesen = Assert.Single(SperrfensterCtrl.Lesen(ANLAGE));
            Assert.Equal(14, gelesen.VonH);
            Assert.False(gelesen.HeizstabGesperrt);

            Assert.True(SperrfensterCtrl.Schreiben(ANLAGE, SperrfensterCtrl.Vorlage("DREI_MAL_ZWEI")));
            Assert.Equal(3, SperrfensterCtrl.Lesen(ANLAGE).Count);
            Assert.False(SperrfensterCtrl.Schreiben(ANLAGE, new[] { new Sperrfenster { VonH = 3, DauerH = 0 } }));
            Assert.Equal(3, SperrfensterCtrl.Lesen(ANLAGE).Count);
            Assert.True(SperrfensterCtrl.Schreiben(ANLAGE, new List<Sperrfenster>()));
            Assert.Empty(SperrfensterCtrl.Lesen(ANLAGE));
        }

        /// <summary>
        /// Der Lauf: ohne Zeilen rechnet die Wärmepumpe wie ohne Tabelle; mit 2 × 2 h liefern Verdichter
        /// und Heizstab in den Fenstern nichts, die Protokollzeile steht nur dann.
        /// </summary>
        [Fact]
        public void Der_Lauf_ohne_Zeilen_ist_unveraendert_und_mit_Fenstern_gesperrt()
        {
            if (!_db.Vorhanden) return;

            Lauf(out double[] mitTabelle, out _, out List<string> hinweiseOhne);
            DataRepository.ExecuteNonQuery("DROP TABLE Tab_Sperrfenster");
            Lauf(out double[] ohneTabelle, out _, out _);
            WaermepumpeSperrprofilSchema.Ausfuehren(null);
            Assert.Equal(ohneTabelle, mitTabelle);
            string kopf = WindowsFormsApplication1.MyResource.Resource.SIMENG_SPERRPROFIL_ZEILE.Split('{')[0];
            Assert.DoesNotContain(hinweiseOhne, t => t.Contains(kopf));
            Assert.True(mitTabelle.Sum() > 0);

            Assert.True(SperrfensterCtrl.Schreiben(ANLAGE, SperrfensterCtrl.Vorlage("ZWEI_MAL_ZWEI")));
            Lauf(out double[] gesperrt, out double[] stab, out List<string> hinweise);
            int[] stunden = { 11, 12, 17, 18 };
            double vorher = 0, nachher = 0;
            for (int h = 0; h < Sperrprofil.STUNDEN; h++)
            {
                if (!stunden.Contains(h % 24)) continue;
                vorher += mitTabelle[h];
                nachher += gesperrt[h];
                Assert.Equal(0.0, stab[h]);
            }
            Assert.True(vorher > 0, "Die Wärmepumpe lief vorher in den Fensterstunden nicht.");
            Assert.Equal(0.0, nachher);
            Assert.Contains(hinweise, t => t.Contains(kopf) && t.Contains("1460"));
        }

        [Fact]
        public void Duplizieren_traegt_die_Fenster_mit()
        {
            if (!_db.Vorhanden) return;
            Assert.True(SperrfensterCtrl.Schreiben(ANLAGE, SperrfensterCtrl.Vorlage("ZWEI_MAL_ZWEI")));
            string name = Convert.ToString(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                                                                        new DbParam("@id", PROJEKT)));
            int neu = new ProjektDuplizierenCtrl().Duplizieren(name, name + " Sperrprofil");
            Assert.True(neu > 0, "Duplizieren fehlgeschlagen.");
            int kopie = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = 1", new DbParam("@p", neu)));
            Assert.NotEqual(ANLAGE, kopie);
            List<Sperrfenster> f = SperrfensterCtrl.Lesen(kopie);
            Assert.Equal(new[] { 11.0, 17.0 }, f.Select(x => x.VonH));
            Assert.Equal(2, SperrfensterCtrl.Lesen(ANLAGE).Count);
        }

        /// <summary>
        /// Der Speicherweg der Anlagen (Löschen + Neuanlegen): Ohne Liste am Modell kommen die Fenster
        /// über die Rettung zurück; eine gesetzte Liste — auch leer — ist die neue Wahrheit.
        /// </summary>
        [Fact]
        public void Loeschen_und_Neuanlegen_rettet_die_Fenster_und_die_Dialogliste_gilt()
        {
            if (!_db.Vorhanden) return;
            Assert.True(SperrfensterCtrl.Schreiben(ANLAGE, SperrfensterCtrl.Vorlage("ZWEI_MAL_ZWEI")));

            WErzeugerModel m = Modell();
            var wizard = new WizardCtrl();
            Assert.True(wizard.Del_Projekt_Waermeerzeuger(PROJEKT, WizardItemClass.WP_TYP));
            Assert.True(wizard.Add_WP_Waermeerzeuger(PROJEKT, new List<WErzeugerModel> { m }));
            int neu = WpAnlage();
            Assert.NotEqual(ANLAGE, neu);
            Assert.Equal(new[] { 11.0, 17.0 }, SperrfensterCtrl.Lesen(neu).Select(x => x.VonH));

            m = Modell(neu);
            m.WP_Sperrfenster = new List<Sperrfenster> { new Sperrfenster { VonH = 22, DauerH = 4, Wochentage = 31 } };
            wizard = new WizardCtrl();
            Assert.True(wizard.Del_Projekt_Waermeerzeuger(PROJEKT, WizardItemClass.WP_TYP));
            Assert.True(wizard.Add_WP_Waermeerzeuger(PROJEKT, new List<WErzeugerModel> { m }));
            Sperrfenster f = Assert.Single(SperrfensterCtrl.Lesen(WpAnlage()));
            Assert.Equal(22, f.VonH);
            Assert.Equal(31, f.Wochentage);
        }

        private static WErzeugerModel Modell(int id = ANLAGE)
        {
            var ctrl = new WErzeugerCtrl();
            ctrl.ReadAllFilter("ID=" + id);
            return ctrl.items[0];
        }

        private static int WpAnlage() => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM Tab_Energieanlagen WHERE ID_Projekt = ? AND ID_Type = 1", new DbParam("@p", PROJEKT)));

        [Fact]
        public void Die_Pufferauslegung_liest_die_Fenster_und_schreibt_nur_auf_Zuruf()
        {
            if (!_db.Vorhanden) return;
            const int projekt = 1045, puffer = 1054212, wp = 14924;
            Assert.Empty(PufferAuslegungCtrl.Vorbelegen(projekt, puffer).Eingang.Sperrfenster);

            Assert.True(SperrfensterCtrl.Schreiben(wp, SperrfensterCtrl.Vorlage("DREI_MAL_ZWEI")));
            IReadOnlyList<PufferSperrfenster> gelesen = PufferAuslegungCtrl.Vorbelegen(projekt, puffer).Eingang.Sperrfenster;
            Assert.Equal(new[] { 6.0, 11.0, 17.0 }, gelesen.Select(x => x.BeginnH));

            Assert.Equal(1, PufferAuslegungCtrl.SperrprofilSchreiben(projekt, PufferSperrprofil.Fenster("ZWEI_MAL_ZWEI")));
            Assert.Equal(new[] { 11.0, 17.0 }, SperrfensterCtrl.Lesen(wp).Select(x => x.VonH));
        }

        private static void Lauf(out double[] wp, out double[] stab, out List<string> hinweise)
        {
            var lauf = new SimulationRunner();
            Assert.True(lauf.Simuliere(PROJEKT, out string fehler), fehler);
            wp = (double[])lauf.sim.simulation_wp.WP_Waermeproduktion_stuendlich.Clone();
            stab = (double[])lauf.sim.simulation_wp.Heizstab_stuendlich.Clone();
            hinweise = lauf.Protokoll.Hinweise.ToList();
        }

        private static long Zahl(string sql) =>
            Convert.ToInt64(DataRepository.ExecuteScalar(sql), CultureInfo.InvariantCulture);
    }
}
