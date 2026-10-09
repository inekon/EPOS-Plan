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

            // Die Feiertage kommen aus den Regeln des Kerns, nicht aus den (hier leeren) Kennzeichen der Klimaregion.
            Assert.NotNull(z.Feiertage);
            Assert.Contains(1, z.Feiertage);                   // Neujahr
            Assert.Contains(276, z.Feiertage);                 // 3. Oktober
            Assert.Equal(ZapfTagtyp.SonnFeiertag, k[275]);     // 3. Oktober, ein Mittwoch

            ZapfprofilErgebnis r = ZapfprofilRechner.Rechnen(e, ZapfprofilCtrl.Katalog());
            Assert.NotNull(r);
        }

        /// <summary>
        /// Leer und Samstag + Sonntag nehmen zu den Kennzeichen der Klimaregion nur die Feiertage des Kerns hinzu (E112);
        /// ohne Feiertage bleibt es dasselbe Feld.
        /// </summary>
        [Fact]
        public void Das_Wochenende_der_Vorgabe_nimmt_nur_die_Feiertage_hinzu()
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

            // Mit Feiertagen des Kerns kommen unter der Vorgabe genau die Feiertage hinzu, sonst bleibt jedes Kennzeichen.
            var feiertage = new[] { 6, 10 };
            Assert.Same(we, Zapfkalender.Kennzeichen(0, we, null, new int[0]));
            foreach (int? maske in new int?[] { null, KalenderbedienungSchema.WOCHENENDE_VORGABE })
            {
                bool[] k = Zapfkalender.Kennzeichen(0, we, maske, feiertage);
                Assert.NotSame(we, k);
                for (int d = 1; d <= 365; d++)
                    Assert.Equal(we[d - 1] || d == 6 || d == 10, k[d - 1]);
                Assert.Equal(we.Count(x => x) + 1, k.Count(x => x));   // der 6. war schon gekennzeichnet
            }
            Assert.Equal(Zapfkalender.Bilden(0, we, null), Zapfkalender.Bilden(0, we, null, KalenderbedienungSchema.WOCHENENDE_VORGABE));
        }

        /// <summary>
        /// Vorgabe Samstag + Sonntag (E112): Ein Feiertag am Donnerstag und ein Feiertag am Samstag tragen den Sonntagsgang,
        /// ein gewöhnlicher Samstag den Samstagsgang — leer und Sa + So gleich, im Formvektor- wie im Typtagweg.
        /// </summary>
        [Fact]
        public void Feiertage_tragen_unter_der_Vorgabe_den_Sonntagsgang()
        {
            var we = new bool[365];
            for (int d = 6; d <= 365; d += 7) { we[d - 1] = true; if (d < 365) we[d] = true; }   // Samstag, Sonntag
            var feiertage = new[] { 4, 13 };                  // Donnerstag 4. Januar, Samstag 13. Januar
            foreach (int? maske in new int?[] { null, KalenderbedienungSchema.WOCHENENDE_VORGABE })
            {
                ZapfTagtyp[] t = Zapfkalender.Bilden(0, Zapfkalender.Kennzeichen(0, we, maske, feiertage), null, maske, feiertage);
                Assert.Equal(ZapfTagtyp.SonnFeiertag, t[3]);    // Feiertag am Donnerstag
                Assert.Equal(ZapfTagtyp.Werktag, t[2]);         // Mittwoch
                Assert.Equal(ZapfTagtyp.Samstag, t[5]);         // gewöhnlicher Samstag
                Assert.Equal(ZapfTagtyp.SonnFeiertag, t[6]);    // Sonntag
                Assert.Equal(ZapfTagtyp.SonnFeiertag, t[12]);   // Feiertag am Samstag

                // Ohne die Feiertage bleibt es der Kalender der Klimaregion.
                ZapfTagtyp[] ohne = Zapfkalender.Bilden(0, Zapfkalender.Kennzeichen(0, we, maske), null, maske);
                Assert.Equal(ZapfTagtyp.Werktag, ohne[3]);
                Assert.Equal(ZapfTagtyp.Samstag, ohne[12]);
                Assert.Equal(2, Enumerable.Range(0, 365).Count(d => t[d] != ohne[d]));
            }
        }

        /// <summary>
        /// Der Eingang belegt die Feiertage jeder Zone (E112): mit gebundenem Gebäude der Vorgabe nach dessen Feiertagsland,
        /// ohne Gebäude die bundeseinheitlichen — beide für das Bezugsjahr des Projekts.
        /// </summary>
        [Fact]
        public void Der_Eingang_belegt_die_Feiertage_jeder_Zone()
        {
            if (!Bereit()) return;
            int gebaeude = GebaeudeMitBestandsferien();
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Gebaeude SET Wochenendtage = NULL WHERE ID = ?", new DbParam("@id", gebaeude)));
            int jahr = Konditionierungdatenweg.Bezugsjahr(PROJEKT);

            ZonenStand gebunden = Eingang(gebaeude).Zonen[0];
            Assert.NotNull(gebunden.Feiertage);
            Assert.Contains(1, gebunden.Feiertage);           // Neujahr
            Assert.Contains(276, gebunden.Feiertage);         // 3. Oktober

            var frei = new ZonenStand
            {
                IdNutzungsart = Nutzung("Testnutzung B (fiktiv)"), Name = "Freie Zone", Bezugsmenge = 20.0,
                Ferienbeginn = new int?[4], Ferienende = new int?[4],
            };
            Zapfprofileingang e = ZapfprofilCtrl.Eingang(PROJEKT, new ZapfprofilStand(BrauchwasserWeg.Generator, new[] { frei }, null),
                                                         0, new bool[365]);
            Assert.Equal(Landesfeiertage.Jahrestage(null, jahr), e.Zonen[0].Feiertage);
            ZapfTagtyp[] k = Zapfkalender.Bilden(0, Zapfkalender.KennzeichenDerZone(0, new bool[365], e.Zonen[0]),
                                                 Zapfkalender.FensterDerZone(e.Zonen[0]), e.Zonen[0].Wochenendtage, e.Zonen[0].Feiertage);
            Assert.Equal(ZapfTagtyp.SonnFeiertag, k[0]);      // Neujahr
            Assert.Equal(ZapfTagtyp.SonnFeiertag, k[275]);    // 3. Oktober
        }

        /// <summary>
        /// Wochenende Montag + Dienstag: Ein Feiertag des Kerns an einem Samstag trägt den Sonntagsgang, ein gewöhnlicher
        /// Samstag und der Sonntag sind Werktage, ein Feiertag an einem Mittwoch trägt den Sonntagsgang.
        /// </summary>
        [Fact]
        public void Feiertag_am_Samstag_traegt_unter_Montag_Dienstag_den_Sonntagsgang()
        {
            const int montagDienstag = 3;
            var we = new bool[365];
            for (int d = 6; d <= 365; d += 7) { we[d - 1] = true; if (d < 365) we[d] = true; }   // Samstag, Sonntag
            var feiertage = new[] { 6, 10 };                  // Samstag 6. Januar, Mittwoch 10. Januar

            bool[] k = Zapfkalender.Kennzeichen(0, we, montagDienstag, feiertage);
            ZapfTagtyp[] t = Zapfkalender.Bilden(0, k, null, montagDienstag);
            Assert.Equal(ZapfTagtyp.SonnFeiertag, t[0]);        // Montag
            Assert.Equal(ZapfTagtyp.SonnFeiertag, t[1]);        // Dienstag
            Assert.Equal(ZapfTagtyp.SonnFeiertag, t[5]);        // Feiertag am Samstag
            Assert.Equal(ZapfTagtyp.Werktag, t[6]);             // Sonntag ohne Maske
            Assert.Equal(ZapfTagtyp.SonnFeiertag, t[9]);        // Feiertag am Mittwoch
            Assert.Equal(ZapfTagtyp.Werktag, t[12]);            // gewöhnlicher Samstag
            Assert.DoesNotContain(ZapfTagtyp.Samstag, t);

            // Ohne die Feiertage des Kerns ginge der Samstagsfeiertag verloren (die Klimaregion trennt ihn nicht).
            Assert.Equal(ZapfTagtyp.Werktag, Zapfkalender.Bilden(0, Zapfkalender.Kennzeichen(0, we, montagDienstag), null, montagDienstag)[5]);

            // Eine Maske mit Samstag: der gewöhnliche Samstag trägt den Samstagsgang, der Feiertag am Samstag den
            // Sonntagsgang (Feiertag geht vor, E112).
            const int samstagMontag = (1 << Zapfkalender.SAMSTAG) | 1;
            ZapfTagtyp[] s = Zapfkalender.Bilden(0, Zapfkalender.Kennzeichen(0, we, samstagMontag, feiertage), null, samstagMontag,
                                                 feiertage);
            Assert.Equal(ZapfTagtyp.SonnFeiertag, s[5]);
            Assert.Equal(ZapfTagtyp.Samstag, s[12]);
            Assert.Equal(ZapfTagtyp.Werktag, s[6]);
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
