using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Ferien des Zapfprofils aus dem Heizkalender des gebundenen Gebäudes</b> (Entwurf KP2,
    /// Welle K1, Festlegung 10; Teilkonzept Konditionierungsprofile 5.5): Trägt das Gebäude einen
    /// angelegten Heizkalender, kommen die Ferien einer Zone ohne eigene Ferien aus dessen Perioden der
    /// Art FERIEN — höchstens vier, die ranghöchsten; mehr nennt ein benannter Hinweis. Sonst wörtlich
    /// der Bestandszweig (<c>Tab_Gebaeude</c>-Spalten mit Merker, A8 „nur vorbelegen").
    ///
    /// <para>Arbeitskopie der Testdatenbank, Projekt 1006 mit dem fiktiven Testkatalog (wie
    /// <c>ZapfprofilSpeichernTests</c>); alle Werte erfunden und rund.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZapfprofilFerienKalenderTests : IDisposable
    {
        private const int PROJEKT = 1006;
        private const string VERSION = "TEST-1";

        private readonly TestDatenbank _db = new TestDatenbank();

        /// <summary>Gibt die Arbeitskopie frei (<c>TestDatenbankEntsorgungWacheTests</c>).</summary>
        public void Dispose() => _db.Dispose();

        private bool Bereit() => _db.Vorhanden && KonditionierungSchema.Lesbar();

        /// <summary>
        /// Der Kalender geht vor den Gebäudespalten: Die Zone ohne eigene Ferien bekommt die zwei
        /// FERIEN-Perioden des Heizkalenders, die ranghöhere zuerst, samt einer über den Jahreswechsel;
        /// eine Periode anderer Art und eine Feiertagsregel zählen nicht.
        /// </summary>
        [Fact]
        public void Mit_Heizkalender_kommen_die_Ferien_aus_seinen_Ferienperioden()
        {
            if (!Bereit()) return;
            int gebaeude = GebaeudeMitBestandsferien();
            Assert.True(new KonditionierungCtrl().Schreiben(KonditionierungCtrl.Eigner.Gebaeude(gebaeude), Heizkalender(
                Ferien(Standardfahrplan.RANG_FERIEN, "Winter", 355, 6),
                Ferien(Standardfahrplan.RANG_FERIEN + 1, "Ostern", 90, 100),
                Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN, DbWerte.KOND_ART_ZEITRAUM, "Betriebsausflug", 150, 150,
                                       Kalenderangabe.AusWert(16.0)),
                Kalenderregel.Feiertag(Standardfahrplan.RANG_FEIERTAG, "Neujahr", DbWerte.KOND_FEIERTAG_NEUJAHR,
                                       Kalenderangabe.AlsWochentag(7)))).Ok);

            Zapfprofileingang e = Eingang(gebaeude);
            Assert.Equal(new int?[] { 90, 355, null, null }, e.Zonen[0].Ferienbeginn);
            Assert.Equal(new int?[] { 100, 6, null, null }, e.Zonen[0].Ferienende);
            Assert.Empty(e.Vorhinweise);

            // Im Rechenweg: drei Fenster, die Winterferien über den Jahreswechsel geteilt.
            IReadOnlyList<Ferienfenster> fenster = Zapfkalender.FensterDerZone(e.Zonen[0]);
            Assert.Equal(new[] { (90, 100), (355, 365), (1, 6) }, fenster.Select(f => (f.Beginn, f.Ende)));
        }

        /// <summary>
        /// Mehr FERIEN-Perioden, als eine Zone Paare führt: Die vier ranghöchsten werden Ferien (die zwei
        /// eigenen über den vier des Generators), und ein benannter Hinweis nennt Gebäude, Zahl und Grenze —
        /// der Satz steht unter <c>KOND_MSG_ZAPF_FERIEN_GEKUERZT</c> in beiden Sprachen.
        /// </summary>
        [Fact]
        public void Mehr_als_vier_Ferienperioden_nehmen_die_ranghoechsten_und_nennen_es()
        {
            if (!Bereit()) return;
            int gebaeude = GebaeudeMitBestandsferien();
            string name = Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Gebaeudename FROM Tab_Gebaeude WHERE ID = ?", new DbParam("@id", gebaeude)));
            Assert.True(new KonditionierungCtrl().Schreiben(KonditionierungCtrl.Eigner.Gebaeude(gebaeude), SechsFerien()).Ok);

            Zapfprofileingang e = Eingang(gebaeude);
            Assert.Equal(new int?[] { 250, 240, 300, 200 }, e.Zonen[0].Ferienbeginn);
            Assert.Equal(new int?[] { 255, 245, 305, 210 }, e.Zonen[0].Ferienende);

            ZapfHinweis h = Assert.Single(e.Vorhinweise);
            Assert.Equal(ZapfprofilCtrl.HINWEIS_KALENDERFERIEN_GEKUERZT, h.Code);
            Assert.Equal("Ferienzone", h.Zone);
            Assert.Equal(nameof(WindowsFormsApplication1.MyResource.Resource.KOND_MSG_ZAPF_FERIEN_GEKUERZT), h.Satz.Schluessel);
            Assert.Equal(new object[] { name, 6, 4 }, h.Satz.Werte);
            Assert.Equal("Der Heizkalender des Gebäudes „" + name + "“ trägt 6 Ferienperioden; das Zapfprofil übernimmt die 4 ranghöchsten.",
                         h.Text);
            var en = System.Globalization.CultureInfo.GetCultureInfo("en-US");
            Assert.Equal("The heating calendar of building “" + name + "” has 6 holiday periods; the draw-off profile takes the 4 highest-ranked.",
                         h.Satz.Text(k => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(k, en), en));

            // Der Hinweis erreicht das Ergebnis des Rechenwegs wie jeder Vorhinweis.
            ZapfprofilErgebnis r = ZapfprofilRechner.Rechnen(e, ZapfprofilCtrl.Katalog());
            Assert.Contains(r.Hinweise, x => x.Code == ZapfprofilCtrl.HINWEIS_KALENDERFERIEN_GEKUERZT);
        }

        /// <summary>
        /// Ein Heizkalender ohne FERIEN-Periode belegt keine Ferien vor — die Spalten und der Merker des
        /// Gebäudes ruhen für diese Größe.
        /// </summary>
        [Fact]
        public void Ohne_Ferienperiode_im_Heizkalender_kommen_keine_Ferien_auch_mit_Merker()
        {
            if (!Bereit()) return;
            int gebaeude = GebaeudeMitBestandsferien();
            Assert.True(new KonditionierungCtrl().Schreiben(KonditionierungCtrl.Eigner.Gebaeude(gebaeude), Heizkalender()).Ok);

            Zapfprofileingang e = Eingang(gebaeude);
            Assert.Equal(new int?[4], e.Zonen[0].Ferienbeginn);
            Assert.Equal(new int?[4], e.Zonen[0].Ferienende);
            Assert.Empty(e.Vorhinweise);
        }

        /// <summary>Eigene Ferien der Zone gehen vor (A8) — auch vor einem Kalender mit sechs Perioden, ohne Hinweis.</summary>
        [Fact]
        public void Eigene_Ferien_der_Zone_gehen_vor_ohne_Hinweis()
        {
            if (!Bereit()) return;
            int gebaeude = GebaeudeMitBestandsferien();
            Assert.True(new KonditionierungCtrl().Schreiben(KonditionierungCtrl.Eigner.Gebaeude(gebaeude), SechsFerien()).Ok);

            Zapfprofileingang e = Eingang(gebaeude, new int?[] { 100, null, null, null }, new int?[] { 110, null, null, null });
            Assert.Equal(new int?[] { 100, null, null, null }, e.Zonen[0].Ferienbeginn);
            Assert.Equal(new int?[] { 110, null, null, null }, e.Zonen[0].Ferienende);
            Assert.Empty(e.Vorhinweise);
        }

        /// <summary>
        /// Ohne Heizkalender wörtlich der Bestandszweig: die Gebäudespalten mit Merker — ein Kalender
        /// einer anderen Größe ändert daran nichts.
        /// </summary>
        [Fact]
        public void Ohne_Heizkalender_gilt_wortlich_der_Bestandszweig()
        {
            if (!Bereit()) return;
            int gebaeude = GebaeudeMitBestandsferien();
            var kuehlen = new Konditionierungskalender(Konditionierungsgroesse.Kuehlsoll, Kalenderangabe.AusWert(26.0), null,
                                                       new[] { Ferien(Standardfahrplan.RANG_FERIEN, "Sommer", 180, 200) });
            Assert.True(new KonditionierungCtrl().Schreiben(KonditionierungCtrl.Eigner.Gebaeude(gebaeude), kuehlen).Ok);

            Zapfprofileingang e = Eingang(gebaeude);
            Assert.Equal(new int?[] { 200, 0, 0, 0 }, e.Zonen[0].Ferienbeginn);
            Assert.Equal(new int?[] { 210, 0, 0, 0 }, e.Zonen[0].Ferienende);

            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET Ferien = 0.0 WHERE ID = ?", new DbParam("@id", gebaeude)));
            Assert.Equal(new int?[4], Eingang(gebaeude).Zonen[0].Ferienbeginn);
        }

        /// <summary>
        /// Der Hinweissatz steht in beiden Sprachen mit denselben Platzhaltern — die Wache der ZPG-Sätze
        /// sieht ihn nicht, weil er der Konditionierung gehört (<see cref="ZapfSatz.AusRessource"/>).
        /// </summary>
        [Fact]
        public void Der_Hinweissatz_steht_in_beiden_Sprachen_mit_denselben_Platzhaltern()
        {
            const string schluessel = nameof(WindowsFormsApplication1.MyResource.Resource.KOND_MSG_ZAPF_FERIEN_GEKUERZT);
            string de = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(
                schluessel, System.Globalization.CultureInfo.InvariantCulture);
            string en = WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(
                schluessel, System.Globalization.CultureInfo.GetCultureInfo("en-US"));
            Assert.False(string.IsNullOrWhiteSpace(de));
            Assert.False(string.IsNullOrWhiteSpace(en));
            Assert.NotEqual(de, en);
            string[] P(string t) => System.Text.RegularExpressions.Regex.Matches(t, @"\{\d+\}").Select(m => m.Value).OrderBy(x => x).ToArray();
            Assert.Equal(new[] { "{0}", "{1}", "{2}" }, P(de));
            Assert.Equal(P(de), P(en));

            // Gleiche Sätze sind gleich, ein gleich benannter der ZPG-Familie nicht.
            Assert.Equal(ZapfSatz.AusRessource(schluessel, "A", 6, 4), ZapfSatz.AusRessource(schluessel, "A", 6, 4));
            Assert.NotEqual(ZapfSatz.AusRessource(schluessel, "A", 6, 4), ZapfSatz.Neu(schluessel, "A", 6, 4));
        }

        // =================================================================================
        // Handwerkszeug
        // =================================================================================

        /// <summary>
        /// Ein Heizkalender mit sechs FERIEN-Perioden: die vier des Generators (Rang 200 … 203) und zwei
        /// eigene (310, 311); dazu eine Periode anderer Art mit höherem Rang.
        /// </summary>
        private static Konditionierungskalender SechsFerien() => Heizkalender(
            Ferien(Standardfahrplan.RANG_FERIEN, "Ferien 1", 350, 5),
            Ferien(Standardfahrplan.RANG_FERIEN + 1, "Ferien 2", 80, 90),
            Ferien(Standardfahrplan.RANG_FERIEN + 2, "Ferien 3", 200, 210),
            Ferien(Standardfahrplan.RANG_FERIEN + 3, "Ferien 4", 300, 305),
            Ferien(Standardfahrplan.RANG_EIGEN, "Brückentage", 240, 245),
            Ferien(Standardfahrplan.RANG_EIGEN + 1, "Betriebsferien", 250, 255),
            Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN + 2, DbWerte.KOND_ART_ZEITRAUM, "Umbau", 100, 120,
                                   Kalenderangabe.Abgeschaltet));

        /// <summary>Ein Gebäude des Projekts mit Bestandsferien 200 … 210 und gesetztem Merker (A8).</summary>
        private static int GebaeudeMitBestandsferien()
        {
            int gebaeude = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT MIN(ID) FROM Tab_Gebaeude WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT)));
            Assert.True(gebaeude > 0);
            Assert.True(DataRepository.ExecuteSQL(
                "UPDATE Tab_Gebaeude SET Ferien = 1.0, Ferienbeginn_1 = 200.0, Ferienende_1 = 210.0, " +
                "Ferienbeginn_2 = 0.0, Ferienende_2 = 0.0, Ferienbeginn_3 = 0.0, Ferienende_3 = 0.0, " +
                "Ferienbeginn_4 = 0.0, Ferienende_4 = 0.0 WHERE ID = ?", new DbParam("@id", gebaeude)));
            return gebaeude;
        }

        /// <summary>Der Eingang mit einer an das Gebäude gebundenen Zone ohne eigene Ferien.</summary>
        private static Zapfprofileingang Eingang(int gebaeude, int?[] eigeneBeginne = null, int?[] eigeneEnden = null)
        {
            var zone = new ZonenStand
            {
                IdNutzungsart = Nutzung("Testnutzung B (fiktiv)"),
                IdGebaeude = gebaeude,
                Name = "Ferienzone",
                Bezugsmenge = 20.0,
                Ferienbeginn = eigeneBeginne ?? new int?[4],
                Ferienende = eigeneEnden ?? new int?[4],
            };
            return ZapfprofilCtrl.Eingang(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { zone }, null),
                                          0, new bool[365]);
        }

        private static int Nutzung(string bezeichner) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM Tab_TwwNutzungsart_STAMM WHERE Bezeichner = ? AND Katalogversion = ?",
            new DbParam("@b", bezeichner), new DbParam("@k", VERSION)));

        private static Kalenderregel Ferien(int rang, string name, int beginn, int ende)
            => Kalenderregel.Zeitraum(rang, DbWerte.KOND_ART_FERIEN, name, beginn, ende, Kalenderangabe.AusWert(16.0));

        /// <summary>Ein Heizkalender mit 20 °C und den übergebenen Perioden.</summary>
        private static Konditionierungskalender Heizkalender(params Kalenderregel[] perioden)
            => new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(20.0), null, perioden);
    }
}
