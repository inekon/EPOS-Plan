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
    /// angelegten Heizkalender mit einer Periode der Art FERIEN, kommen die Ferien einer Zone ohne eigene Ferien aus
    /// allen Ferienperioden des gemeinsamen Kalenders, ohne ihn aus allen FERIEN-Perioden des Heizkalenders. Sonst
    /// wörtlich der Bestandszweig (<c>Tab_Gebaeude</c>-Spalten mit Merker, A8 „nur vorbelegen"). Das Wochenende kommt aus
    /// <c>Wochenendtage</c> des Gebäudes.
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
        /// Mehr FERIEN-Perioden, als die Zone Spalten führt: Ohne gemeinsamen Kalender werden ALLE FERIEN-Perioden des
        /// Heizkalenders Ferien (die ranghöchsten zuerst), ohne Kürzung und ohne Hinweis.
        /// </summary>
        [Fact]
        public void Mehr_als_vier_Ferienperioden_werden_alle_uebernommen()
        {
            if (!Bereit()) return;
            int gebaeude = GebaeudeMitBestandsferien();
            Assert.True(new KonditionierungCtrl().Schreiben(KonditionierungCtrl.Eigner.Gebaeude(gebaeude), SechsFerien()).Ok);

            Zapfprofileingang e = Eingang(gebaeude);
            Assert.Equal(new int?[] { 250, 240, 300, 200, 80, 350 }, e.Zonen[0].Ferienbeginn);
            Assert.Equal(new int?[] { 255, 245, 305, 210, 90, 5 }, e.Zonen[0].Ferienende);
            Assert.Empty(e.Vorhinweise);
        }

        /// <summary>
        /// <b>Die Leser der Stufe 2</b> (Konzept 7.8): Die Ferien kommen aus ALLEN Ferienperioden des gemeinsamen Kalenders
        /// (hier sieben, Rang 200 … 206) statt aus den vier Spalten oder den FERIEN-Perioden des Heizkalenders, das
        /// Wochenende aus <c>Wochenendtage</c> (Montag und Dienstag): Montag und Dienstag tragen den Sonntagsgang, der Samstag
        /// wird Werktag, jeder der sieben Zeiträume Ruhetag.
        /// </summary>
        [Fact]
        public void Ferienliste_und_Wochenende_des_Gebaeudes_wirken_im_Zapfkalender()
        {
            if (!Bereit() || !Kalendergemeinschaft.SchrittSteht()) return;
            int gebaeude = GebaeudeMitBestandsferien();
            Assert.True(new KonditionierungCtrl().Schreiben(KonditionierungCtrl.Eigner.Gebaeude(gebaeude),
                Heizkalender(Ferien(Standardfahrplan.RANG_FERIEN, "Ferien 1", 200, 210))).Ok);
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET Wochenendtage = 3 WHERE ID = ?", new DbParam("@id", gebaeude)));
            Assert.True(DataRepository.ExecuteSQL(
                "INSERT INTO Tab_Konditionierungskalender (ID_Gebaeude, Groesse, Aus) VALUES (?, 'ALLE', 0)", new DbParam("@g", gebaeude)));
            long kalender = Convert.ToInt64(DataRepository.ExecuteScalar(
                "SELECT ID FROM Tab_Konditionierungskalender WHERE ID_Gebaeude = ? AND ID_Zone IS NULL AND Groesse = 'ALLE'",
                new DbParam("@g", gebaeude)));
            var ferien = new[] { (20, 22), (40, 42), (60, 62), (80, 82), (100, 102), (120, 122), (300, 302) };
            for (int i = 0; i < ferien.Length; i++)
                Assert.True(DataRepository.ExecuteSQL(
                    "INSERT INTO Tab_Konditionierungsperiode (ID_Kalender, Rang, Art, Bezeichner, Beginn, Ende, Aus, Gilt_Fuer) " +
                    "VALUES (?, ?, 'FERIEN', ?, ?, ?, 0, 31)", new DbParam("@k", kalender), new DbParam("@r", 200 + i),
                    new DbParam("@b", "F" + (i + 1)), new DbParam("@v", ferien[i].Item1), new DbParam("@e", ferien[i].Item2)));

            Zapfprofileingang e = Eingang(gebaeude);
            ZonenStand z = e.Zonen[0];
            Assert.Equal(ferien.Select(f => (int?)f.Item1), z.Ferienbeginn);
            Assert.Equal(ferien.Select(f => (int?)f.Item2), z.Ferienende);
            Assert.Equal(3, z.Wochenendtage);

            ZapfTagtyp[] k = Zapfkalender.Bilden(0, Zapfkalender.KennzeichenDerZone(0, new bool[365], z), Zapfkalender.FensterDerZone(z));
            Assert.Equal(ZapfTagtyp.SonnFeiertag, k[0]);       // Montag, 1. Januar
            Assert.Equal(ZapfTagtyp.SonnFeiertag, k[1]);       // Dienstag
            Assert.Equal(ZapfTagtyp.Werktag, k[2]);
            Assert.Equal(ZapfTagtyp.Werktag, k[5]);            // Samstag
            Assert.Equal(7 * 3, k.Count(t => t == ZapfTagtyp.Ruhetag));
            Assert.Equal(ZapfTagtyp.Ruhetag, k[300]);          // im siebten Zeitraum

            ZapfprofilErgebnis r = ZapfprofilRechner.Rechnen(e, ZapfprofilCtrl.Katalog());
            Assert.NotNull(r);
        }

        /// <summary>Leer und Samstag + Sonntag lassen die Kennzeichen der Klimaregion unverändert (dasselbe Feld).</summary>
        [Fact]
        public void Das_Wochenende_der_Vorgabe_laesst_die_Kennzeichen_unveraendert()
        {
            var we = new bool[365];
            we[5] = we[6] = we[2] = true;                     // Samstag, Sonntag und ein Feiertag am Mittwoch
            Assert.Same(we, Zapfkalender.Kennzeichen(0, we, null));
            Assert.Same(we, Zapfkalender.Kennzeichen(0, we, KalenderbedienungSchema.WOCHENENDE_VORGABE));
            bool[] fr = Zapfkalender.Kennzeichen(0, we, 1 << 4);    // nur Freitag
            Assert.True(fr[4]);
            Assert.True(fr[2]);                               // der Feiertag am Werktag bleibt
            Assert.False(fr[5]);
            Assert.False(fr[6]);
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
