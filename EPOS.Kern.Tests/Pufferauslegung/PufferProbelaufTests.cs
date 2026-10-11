using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using EPOS.UI.Seiten.Pufferspeicher;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests.Pufferauslegung
{
    /// <summary>
    /// Welle P4b: der Probelauf der Jahressimulation mit dem empfohlenen Puffervolumen — Starts aus dem
    /// Lauf, Füllstand, Abweichungs-Hinweis und dass nichts in die Datenbank geschrieben wird.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class PufferProbelaufTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public PufferProbelaufTests() => PufferProbelaufCtrl.Vergessen();

        public void Dispose()
        {
            PufferProbelaufCtrl.Vergessen();
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int P_ZAPF = 1045;
        private const int PUFFER_1045_KOMBI = 1054210;
        /// <summary>Der Puffer, den die Senken von 1045 laden (im Rechenpfad des Laufs).</summary>
        private const int PUFFER_1045_LAUF = 1054212;

        private static object Wert(string sql, params object[] p) =>
            DataRepository.ExecuteScalar(sql, p.Select((w, i) => new DbParam("@p" + i, w)).ToArray());

        private static string Projektname(int id) => Convert.ToString(Wert("SELECT Projektname FROM Tab_Projekt WHERE ID = ?", id));

        private static int Kopie(int idProjekt, string zusatz)
        {
            string name = Projektname(idProjekt);
            int kopie = new ProjektDuplizierenCtrl().Duplizieren(name, name + " " + zusatz);
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            return kopie;
        }

        private static int PufferDerKopie(int idKopie, int idQuellpuffer) =>
            Convert.ToInt32(Wert("SELECT ID FROM Tab_Pufferspeicher WHERE ID_Projekt = ? AND Bezeichner = " +
                                 "(SELECT Bezeichner FROM Tab_Pufferspeicher WHERE ID = ?)", idKopie, idQuellpuffer));

        /// <summary>Zeilenzahl aller Ergebnistabellen und der Auslegungstabelle — der Fingerabdruck „nichts geschrieben".</summary>
        private static string Fingerabdruck()
        {
            DataTable t = DataRepository.GetDataTable(
                "SELECT name FROM sqlite_master WHERE type = 'table' AND (name LIKE 'Tab_Ergebnis%' OR name = 'Tab_PufferAuslegung') ORDER BY name");
            var teile = new List<string>();
            foreach (DataRow r in t.Rows)
            {
                string name = Convert.ToString(r[0]);
                teile.Add(name + "=" + Convert.ToString(Wert("SELECT COUNT(*) FROM [" + name + "]")));
            }
            return string.Join(";", teile);
        }

        private static string Pufferzeile(int idPuffer)
        {
            DataTable t = DataRepository.GetDataTable("SELECT * FROM Tab_Pufferspeicher WHERE ID = ?", new DbParam("@id", idPuffer));
            Assert.Equal(1, t.Rows.Count);
            return string.Join("|", t.Rows[0].ItemArray.Select(v => Convert.ToString(v, System.Globalization.CultureInfo.InvariantCulture)));
        }

        // =============================================================================
        //  Ohne Datenbank
        // =============================================================================

        [Theory]
        [InlineData(10.0, 13.0, false)]
        [InlineData(10.0, 7.0, false)]
        [InlineData(10.0, 13.01, true)]
        [InlineData(10.0, 6.9, true)]
        [InlineData(0.0, 0.0, false)]
        [InlineData(0.0, 1.0, true)]
        public void Die_Abweichung_greift_ueber_30_Prozent(double auslegung, double lauf, bool erwartet)
            => Assert.Equal(erwartet, PufferProbelaufCtrl.Abweichung(auslegung, lauf));

        [Fact]
        public void Ohne_beide_Werte_keine_Abweichung_und_der_Text_kommt_aus_den_Ressourcen()
        {
            Assert.False(PufferProbelaufCtrl.Abweichung(null, 5));
            Assert.False(PufferProbelaufCtrl.Abweichung(5, null));
            Assert.Equal("PA-STARTS-ABWEICHUNG", PufferWarncode.STARTS_ABWEICHUNG);
            Assert.DoesNotContain(PufferWarncode.STARTS_ABWEICHUNG, PufferWarncode.ALLE);
            Textbaustein t = PufferProbelaufCtrl.AbweichungText(5.3, 8.0);
            Assert.Equal("PA_STARTS_ABWEICHUNG", t.Schluessel);
            Assert.Equal("Die Jahressimulation zählt 8 Starts je Tag, die Auslegung schätzt 5,3 – Abweichung über 30 %.",
                         t.Aufloesen(new System.Globalization.CultureInfo("de-DE")));
            Assert.Contains("deviation above 30 %", t.Aufloesen(new System.Globalization.CultureInfo("en-US")));
        }

        [Fact]
        public void Die_Heizperiode_teilt_die_Starts_nach_Einschaltflanken()
        {
            var waerme = new double[8760];
            var heiz = new bool[8760];
            for (int i = 0; i < 4000; i++) heiz[i] = true;
            // drei Flanken in der Heizperiode, eine im Sommer
            waerme[10] = 5; waerme[100] = 5; waerme[101] = 5; waerme[300] = 5; waerme[6000] = 5;
            Assert.Equal(0.75, PufferProbelaufCtrl.AnteilHeizperiode(waerme, heiz), 10);
            // Durchlauf ohne Flanke: Laufstunden zählen
            var dauer = Enumerable.Repeat(1.0, 8760).ToArray();
            Assert.Equal(4000 / 8760.0, PufferProbelaufCtrl.AnteilHeizperiode(dauer, heiz), 10);
            Assert.Equal(1.0, PufferProbelaufCtrl.AnteilHeizperiode(null, heiz));
        }

        [Fact]
        public void Einschaltflanken_zaehlen_ueber_den_Jahreswechsel_und_in_der_Heizperiode()
        {
            var w = new double[8760];
            var heiz = new bool[8760];
            for (int i = 0; i < 100; i++) heiz[i] = true;
            w[0] = 1; w[8759] = 1;      // läuft über den Jahreswechsel: keine Flanke in Stunde 0
            w[50] = 1; w[51] = 1;       // eine Flanke in der Heizperiode
            w[5000] = 1;                // eine im Sommer
            // Flanken: Stunde 50, 5000 und 8759 (die Stunde davor ist aus) — Stunde 0 folgt auf die laufende 8759.
            Assert.Equal(3, PufferProbelaufCtrl.Einschaltflanken(w, heiz, out int hp));
            Assert.Equal(1, hp);
        }

        [Fact]
        public void Monatswerte_und_kaelteste_Woche_folgen_dem_festen_Raster()
        {
            var f = new double[8760];
            for (int i = 0; i < 8760; i++) f[i] = i < 744 ? 0.2 : 0.8;
            f[5] = 0.0;
            f[743] = 1.0;
            IReadOnlyList<PufferFuellstandMonat> m = PufferProbelaufCtrl.Monatswerte(f);
            Assert.Equal(12, m.Count);
            Assert.Equal(0.0, m[0].Min);
            Assert.Equal(1.0, m[0].Max);
            Assert.Equal(0.8, m[1].Mittel, 10);
            var t = new double[8760];
            for (int i = 0; i < 8760; i++) t[i] = 10;
            for (int i = 2000; i < 2168; i++) t[i] = -10;
            Assert.Equal(2000, PufferProbelaufCtrl.KaeltesteWoche(t));
            Assert.Equal(-1, PufferProbelaufCtrl.KaeltesteWoche(null));
        }

        [Fact]
        public void Die_Naht_ersetzt_das_Volumen_nur_im_Bereich()
        {
            if (!_db.Vorhanden) return;
            int vorher = WaermesenkeClass.PufferLesen(PUFFER_1045_KOMBI).Gesamtvolumen;
            using (WaermesenkeClass.ProbelaufVolumen(PUFFER_1045_KOMBI, vorher + 1234))
            {
                Assert.Equal(vorher + 1234, WaermesenkeClass.PufferLesen(PUFFER_1045_KOMBI).Gesamtvolumen);
                Assert.Equal(vorher + 1234.0, WaermesenkeClass.ProbelaufVolumenOder(PUFFER_1045_KOMBI, 1.0));
                Assert.Equal(7, WaermesenkeClass.ProbelaufVolumenOder(PUFFER_1045_KOMBI + 1, 7));
            }
            Assert.Equal(vorher, WaermesenkeClass.PufferLesen(PUFFER_1045_KOMBI).Gesamtvolumen);
        }

        // =============================================================================
        //  Mit Lauf
        // =============================================================================

        [Fact]
        public void Starts_aus_einem_Lauf_der_Kopie_lesen_die_Zaehler_des_Laufs()
        {
            if (!_db.Vorhanden) return;
            int kopie = Kopie(P_ZAPF, "Probelauf Starts");
            var runner = new SimulationRunner();
            Assert.True(runner.Simuliere(kopie, out string fehler), fehler);
            IReadOnlyList<PufferStartsLauf> starts = PufferProbelaufCtrl.StartsAusLauf(runner.sim, PufferProbelaufCtrl.Heizstunden(null),
                                                                                      ProjektPuffer.TYP_WP);
            // Der Kessel taktet (Mindestleistung): seine Starts sind die Zähler des Laufs.
            PufferStartsLauf kessel = starts.Single(s => s.Typ == ProjektPuffer.TYP_KESSEL);
            Assert.False(kessel.Rang1);
            Assert.False(kessel.AusReihe);
            Assert.True(kessel.StartsJahr > 0);
            Assert.Equal(runner.sim.simulation_spk.Starts_Spk.Sum(), kessel.StartsJahr);
            // Die Wärmepumpe ohne Mindestleistung zählt der Lauf nicht: Einschaltflanken ihrer Wärmereihe.
            PufferStartsLauf wp = starts.Single(s => s.Typ == ProjektPuffer.TYP_WP);
            Assert.True(wp.Rang1);
            Assert.Equal(0, runner.sim.simulation_wp.Starts_WP.Sum());
            Assert.True(wp.AusReihe);
            Assert.Equal(PufferProbelaufCtrl.Einschaltflanken(runner.sim.simulation_wp.WP_Waermeproduktion_stuendlich, null, out _),
                         wp.StartsJahr);
            // Ohne Heizreihe ist das ganze Jahr Heizperiode.
            Assert.Equal(wp.StartsJahr, wp.StartsHeizperiode);
            Assert.Equal(wp.StartsJahr / 365.0, wp.StartsJeTag, 6);
        }

        [Fact]
        public void Der_Probelauf_auf_der_Kopie_liefert_Starts_und_Fuellstand_und_schreibt_nichts()
        {
            if (!_db.Vorhanden) return;
            int kopie = Kopie(P_ZAPF, "Probelauf");
            int puffer = PufferDerKopie(kopie, PUFFER_1045_LAUF);
            string abdruckVorher = Fingerabdruck();
            string pufferVorher = Pufferzeile(puffer);
            string quelleVorher = Pufferzeile(PUFFER_1045_LAUF);
            int gespeichert = Convert.ToInt32(Wert("SELECT Gesamtvolumen FROM Tab_Pufferspeicher WHERE ID = ?", puffer));

            PufferAuslegungReihen reihen = PufferAuslegungCtrl.Reihen(kopie);
            PufferProbelaufErgebnis e = PufferProbelaufCtrl.Probelauf(kopie, puffer, gespeichert + 1500, reihen.Heizung,
                                                                       ProjektPuffer.TYP_WP);
            Assert.True(e.Erfolgreich, e.Fehlertext);
            Assert.Equal(gespeichert + 1500, e.VolumenL);
            Assert.NotNull(e.Rang1);
            Assert.True(e.Rang1.Rang1);
            Assert.True(e.Starts.Single(x => x.Typ == ProjektPuffer.TYP_KESSEL).StartsJahr > 0);
            Assert.True(e.Rang1.StartsHeizperiode <= e.Rang1.StartsJahr);
            Assert.True(e.Heizstunden > 0 && e.Heizstunden < 8760);
            Assert.NotNull(e.Fuellstand);
            Assert.Equal(8760, e.Fuellstand.Length);
            Assert.All(e.Fuellstand, f => Assert.InRange(f, 0.0, 1.0));
            Assert.Equal(12, e.Monate.Count);
            Assert.InRange(e.KaeltesteWocheAb, 0, 8760 - 168);
            Assert.True(e.Deckung.HasValue);
            Assert.True(e.Dauer > TimeSpan.Zero);
            Assert.Same(e, PufferProbelaufCtrl.Letzter(kopie, puffer));
            Assert.Null(PufferProbelaufCtrl.Letzter(P_ZAPF, PUFFER_1045_LAUF));

            // Nichts geschrieben: Ergebnistabellen, Auslegungstabelle, Puffer der Kopie und der Quelle unverändert.
            Assert.Equal(abdruckVorher, Fingerabdruck());
            Assert.Equal(pufferVorher, Pufferzeile(puffer));
            Assert.Equal(quelleVorher, Pufferzeile(PUFFER_1045_LAUF));
            // Mit dem ersetzten Volumen steht im Füllstand eine andere Kapazität: Der zweite Lauf mit dem
            // gespeicherten Volumen liefert eine andere Reihe.
            PufferProbelaufErgebnis gleich = PufferProbelaufCtrl.Probelauf(kopie, puffer, gespeichert, reihen.Heizung, ProjektPuffer.TYP_WP);
            Assert.True(gleich.Erfolgreich, gleich.Fehlertext);
            Assert.NotEqual(gleich.Fuellstand, e.Fuellstand);
            Assert.Equal(abdruckVorher, Fingerabdruck());
            Assert.Equal(gespeichert, WaermesenkeClass.PufferLesen(puffer).Gesamtvolumen);
        }

        [Fact]
        public void Die_Huelle_haelt_den_Probelauf_gegen_die_Auslegung_und_lehnt_einen_neuen_Puffer_benannt_ab()
        {
            if (!_db.Vorhanden) return;
            int kopie = Kopie(P_ZAPF, "Probelauf Huelle");
            int puffer = PufferDerKopie(kopie, PUFFER_1045_LAUF);
            var huelle = new PufferAuslegungHuelle(new PufferAuslegungAuftrag { IdProjekt = kopie, IdPuffer = puffer });
            PufferAuslegungStartDaten start = huelle.Start();
            Assert.Null(start.Probelauf);
            string abdruckVorher = Fingerabdruck();

            PufferProbelaufDaten d = huelle.Probelauf(start.Eingabe);
            Assert.True(d.Erfolg, d.Fehler);
            Assert.Equal(start.Ergebnis.EmpfehlungL, d.VolumenL, 0);
            Assert.NotNull(d.Rang1);
            Assert.Equal("Wärmepumpe", d.Rang1.Erzeuger);
            Assert.True(d.AuslegungStartsJeTag.HasValue);
            Assert.Equal(PufferProbelaufCtrl.Abweichung(d.AuslegungStartsJeTag, d.Rang1.StartsJeTag), d.Abweichung);
            Assert.Equal(d.Abweichung, d.AbweichungText.Length > 0);
            Assert.Equal(12, d.Monate.Count);
            Assert.Equal(abdruckVorher, Fingerabdruck());

            // Beim nächsten Öffnen steht der Lauf der Sitzung schon da.
            PufferAuslegungStartDaten wieder = new PufferAuslegungHuelle(new PufferAuslegungAuftrag { IdProjekt = kopie, IdPuffer = puffer }).Start();
            Assert.NotNull(wieder.Probelauf);
            Assert.Equal(d.Rang1.StartsJahr, wieder.Probelauf.Rang1.StartsJahr);

            // Neuer Puffer: benannt abgelehnt, kein Lauf.
            var neu = new PufferAuslegungHuelle(new PufferAuslegungAuftrag { IdProjekt = kopie, IdPuffer = null });
            PufferProbelaufDaten n = neu.Probelauf(neu.Start().Eingabe);
            Assert.False(n.Erfolg);
            Assert.Contains(WindowsFormsApplication1.MyResource.Resource.PAUS_PROBELAUF_NEU, n.Fehler);
        }
    }
}
