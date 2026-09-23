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
    /// ETAPPE E7c — <b>Schritt E (Schemaschritt 107): Ersatz und Restwert je Position
    /// entkoppelt</b> (Entscheid A6 vom 20.09.2026, Mockup U39, Konzept § 2.13 (3)).
    ///
    /// <para>Drei Teile: (1) der Schemaschritt — Zielstand, Spaltenliste samt
    /// SQLite-Typ, die nachgezogene Arbeitskopie mit leerem Bestand und die
    /// <c>CHECK</c>-Bedingung; (2) die Kennzeichen-Logik in
    /// <see cref="KapitalwertRechner.Ersatz"/> — leer und ja rechnen bitgleich wie
    /// bisher, nein schaltet Kette bzw. Restwert ab, entkoppelt; (3) der Lese- und
    /// Schreibweg samt Vorlagenübernahme und die Probe an 1024 mit beiden
    /// Kennzeichen.</para>
    ///
    /// <para><b>A/B an 1024</b> (Nutzungsdauern Wärmepumpe 15 a, Heizkessel 25 a;
    /// T = 20 a, i = 3 %): ohne Kennzeichen −2.897.442,20 €; Wärmepumpe „Ersatz nein"
    /// −2.895.805,46 € (+1.636,74 €: Ersatz 15 entfällt samt seinem Restwert);
    /// Heizkessel „Restwert nein" −2.897.995,88 € (−553,68 €); beide Positionen
    /// „nein/nein" −2.896.359,13 € — genau der Anker ohne Nutzungsdauern.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErsatzRestwertKennzeichenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1024;
        private const int ZEILE_WP = 101600076;      // Wärmepumpe, 6.001 €
        private const int ZEILE_KESSEL = 101600081;  // Heizkessel, 5.000 €

        // =====================================================================
        //  (1) Der Schemaschritt
        // =====================================================================

        [Fact]
        public void Der_Zielstand_ist_107_und_der_Schritt_hat_vier_nullbare_Spalten()
        {
            Assert.True(SchemaStand.Zielversion >= 107,
                        "Zielstand " + SchemaStand.Zielversion + " liegt unter 107.");

            SchemaSpalte[] spalten = SchemaKatalog.Schritt107_ErsatzRestwertKennzeichen;
            Assert.Equal(4, spalten.Length);
            Assert.Equal(new[] { "Tab_ProjektWerte", "Tab_ProjektWerte",
                                 "Tab_KostenVorlagePosition", "Tab_KostenVorlagePosition" },
                         spalten.Select(s => s.Tabelle).ToArray());
            Assert.Equal(new[] { "ErsatzFuehren", "RestwertAnsetzen", "ErsatzFuehren", "RestwertAnsetzen" },
                         spalten.Select(s => s.Name).ToArray());
            Assert.All(spalten, s => Assert.Equal("YESNO_NULL", s.TypDefinition));

            // Nullbar mit CHECK, ohne NOT NULL und ohne Vorgabe — NULL ist „wie bisher".
            Assert.Equal("INTEGER CHECK (\"ErsatzFuehren\" IN (0,1))",
                         StilleDb.SqliteSpaltenTyp(spalten[0].Name, spalten[0].TypDefinition));
            Assert.Equal("INTEGER CHECK (\"RestwertAnsetzen\" IN (0,1))",
                         StilleDb.SqliteSpaltenTyp(spalten[1].Name, spalten[1].TypDefinition));
        }

        [Fact]
        public void Die_Arbeitskopie_fuehrt_die_Spalten_nullbar_und_der_Bestand_ist_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            foreach (SchemaSpalte s in SchemaKatalog.Schritt107_ErsatzRestwertKennzeichen)
            {
                Assert.True(DataRepository.SpalteVorhanden(s.Tabelle, s.Name), s.Tabelle + "." + s.Name + " fehlt.");
                Assert.Equal(1, Zahl("SELECT COUNT(*) FROM pragma_table_info('" + s.Tabelle + "') " +
                                     "WHERE name = '" + s.Name + "'"));
                Assert.Equal(0, Zahl("SELECT \"notnull\" FROM pragma_table_info('" + s.Tabelle + "') " +
                                     "WHERE name = '" + s.Name + "'"));
                Assert.Equal(0, Zahl("SELECT COUNT(*) FROM [" + s.Tabelle + "] WHERE [" + s.Name + "] IS NOT NULL"));
            }
            Assert.True(ErsatzRestwertKennzeichen.SpaltenVorhanden(SchemaKatalog.TAB_PROJEKTWERTE));
            Assert.True(ErsatzRestwertKennzeichen.SpaltenVorhanden(SchemaKatalog.TAB_KOSTENVORLAGEPOSITION));

            foreach (string tabelle in new[] { "Tab_ProjektWerte", "Tab_KostenVorlagePosition" })
            {
                string ddl = Convert.ToString(DataRepository.ExecuteScalar(
                    "SELECT sql FROM sqlite_master WHERE type = 'table' AND name = ?",
                    new DbParam("@t", tabelle)));
                Assert.EndsWith("STRICT", ddl.TrimEnd());
                Assert.Contains("CHECK (\"ErsatzFuehren\" IN (0,1))", ddl, StringComparison.Ordinal);
            }
        }

        [Fact]
        public void Das_Kennzeichen_nimmt_nur_0_1_und_leer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(ErsatzRestwertKennzeichen.Schreibe(SchemaKatalog.TAB_PROJEKTWERTE, ZEILE_WP, false, true));
            Assert.Equal(0, Zahl("SELECT ErsatzFuehren FROM Tab_ProjektWerte WHERE ID = " + ZEILE_WP));
            Assert.Equal(1, Zahl("SELECT RestwertAnsetzen FROM Tab_ProjektWerte WHERE ID = " + ZEILE_WP));

            using (DataRepository.EngineModus())
                DataRepository.ExecuteNonQuery("UPDATE Tab_ProjektWerte SET ErsatzFuehren = 2 WHERE ID = ?",
                                               new DbParam("@id", ZEILE_WP));
            DataRepository.StilleFehlerAbholen();
            Assert.Equal(0, Zahl("SELECT ErsatzFuehren FROM Tab_ProjektWerte WHERE ID = " + ZEILE_WP));

            Assert.True(ErsatzRestwertKennzeichen.Schreibe(SchemaKatalog.TAB_PROJEKTWERTE, ZEILE_WP, null, null));
            Assert.Equal(0, Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ID = " + ZEILE_WP +
                                 " AND (ErsatzFuehren IS NOT NULL OR RestwertAnsetzen IS NOT NULL)"));
        }

        // =====================================================================
        //  (2) Die Kennzeichen-Logik — ohne Datenbank
        // =====================================================================

        private static KapitalwertRechner.InvestPosition Pos(double betrag, double n,
                                                             bool? ersatz = null, bool? restwert = null)
        {
            return new KapitalwertRechner.InvestPosition
            {
                Betrag = betrag, Nutzungsdauer = n, ErsatzFuehren = ersatz, RestwertAnsetzen = restwert
            };
        }

        /// <summary>Leer und „ja" rechnen bitgleich den Weg vor dem Schritt — auch mit
        /// Preisänderungssatz p_I.</summary>
        [Theory]
        [InlineData(15.0, 0.0)]
        [InlineData(15.0, 2.0)]
        [InlineData(7.0, 1.5)]
        [InlineData(25.0, 0.0)]
        public void Leer_und_ja_rechnen_wie_bisher(double n, double pI)
        {
            KapitalwertRechner.Ersatzbild alt = KapitalwertRechner.Ersatz(
                new KapitalwertRechner.InvestPosition { Betrag = 6001, Nutzungsdauer = n }, 20, pI);
            KapitalwertRechner.Ersatzbild ja = KapitalwertRechner.Ersatz(Pos(6001, n, true, true), 20, pI);

            Assert.Equal(alt.Ersatzjahre, ja.Ersatzjahre);
            Assert.Equal(alt.Ersatzbetraege, ja.Ersatzbetraege);
            Assert.Equal(alt.Restwert, ja.Restwert);   // bitgleich
            Assert.False(ja.ErsatzAbgewaehlt);
            Assert.False(ja.RestwertAbgewaehlt);
        }

        /// <summary>„Ersatz nein": keine Kette — und weil die Erstbeschaffung nach 15
        /// von 20 Jahren verbraucht ist, auch kein Restwert.</summary>
        [Fact]
        public void Ersatz_nein_streicht_die_Kette()
        {
            KapitalwertRechner.Ersatzbild leer = KapitalwertRechner.Ersatz(Pos(6001, 15), 20);
            Assert.Equal(new List<int> { 15 }, leer.Ersatzjahre);
            Assert.Equal(6001.0 * 10.0 / 15.0, leer.Restwert, 9);

            KapitalwertRechner.Ersatzbild nein = KapitalwertRechner.Ersatz(Pos(6001, 15, ersatz: false), 20);
            Assert.True(nein.ErsatzAbgewaehlt);
            Assert.Empty(nein.Ersatzjahre);
            Assert.Equal(0, nein.LetzteBeschaffung);
            Assert.Equal(0.0, nein.Restwert);
        }

        /// <summary>Entkoppelt: Eine nicht ersetzte Position, die über T hinaus hält,
        /// behält ihren Restwert aus der Erstbeschaffung.</summary>
        [Fact]
        public void Ersatz_nein_laesst_den_Restwert_der_Erstbeschaffung()
        {
            KapitalwertRechner.Ersatzbild nein = KapitalwertRechner.Ersatz(Pos(5000, 25, ersatz: false), 20);
            Assert.Empty(nein.Ersatzjahre);
            Assert.Equal(1000.0, nein.Restwert, 9);   // 5.000 × 5/25
        }

        /// <summary>„Restwert nein": die Kette bleibt, der Restwert ist 0.</summary>
        [Fact]
        public void Restwert_nein_laesst_die_Kette()
        {
            KapitalwertRechner.Ersatzbild nein = KapitalwertRechner.Ersatz(Pos(6001, 15, restwert: false), 20);
            Assert.True(nein.RestwertAbgewaehlt);
            Assert.Equal(new List<int> { 15 }, nein.Ersatzjahre);
            Assert.Equal(new List<double> { 6001.0 }, nein.Ersatzbetraege);
            Assert.Equal(0.0, nein.Restwert);
        }

        /// <summary>
        /// Die Wirkung im Kapitalwert: „Ersatz nein" spart den Barwert der
        /// Ersatzbeschaffung und verliert den Barwert ihres Restwerts; „Restwert nein"
        /// verliert den Barwert des Restwerts.
        /// </summary>
        [Fact]
        public void Die_Wirkung_im_Kapitalwert_ist_die_Differenz_der_Barwerte()
        {
            const double i = 3.0;
            const int T = 20;
            Func<bool?, bool?, double> kw = (ersatzWp, restwertKessel) =>
                KapitalwertRechner.Rechne(
                    new List<KapitalwertRechner.InvestPosition>
                    {
                        Pos(6001, 15, ersatzWp, null),
                        Pos(5000, 25, null, restwertKessel)
                    },
                    0, 0, 0, i, T, 0, 0).Kapitalwert;

            double basis = kw(null, null);
            Assert.Equal(basis, kw(true, true));   // ja = wie bisher, bitgleich

            double ohneErsatz = kw(false, null);
            double erwartet = KapitalwertRechner.Barwert(6001.0, 15, i)
                            - KapitalwertRechner.Barwert(6001.0 * 10.0 / 15.0, T, i);
            Assert.Equal(erwartet, ohneErsatz - basis, 6);
            Assert.Equal(1636.74, ohneErsatz - basis, 2);

            double ohneRestwert = kw(null, false);
            Assert.Equal(-KapitalwertRechner.Barwert(1000.0, T, i), ohneRestwert - basis, 6);
            Assert.Equal(-553.68, ohneRestwert - basis, 2);
        }

        [Fact]
        public void Die_Auswahl_der_Klappliste_uebersetzt_dreiwertig()
        {
            Assert.Null(ErsatzRestwertKennzeichen.AlsAuswahl(null));
            Assert.Equal(ErsatzRestwertKennzeichen.JA, ErsatzRestwertKennzeichen.AlsAuswahl(true));
            Assert.Equal(ErsatzRestwertKennzeichen.NEIN, ErsatzRestwertKennzeichen.AlsAuswahl(false));
            Assert.Null(ErsatzRestwertKennzeichen.AusAuswahl(null));
            Assert.True(ErsatzRestwertKennzeichen.AusAuswahl(ErsatzRestwertKennzeichen.JA));
            Assert.False(ErsatzRestwertKennzeichen.AusAuswahl(ErsatzRestwertKennzeichen.NEIN));

            Assert.Equal(new[] { "ja", "nein" }, ErsatzRestwertKennzeichen.Eintraege().Select(e => e.Text).ToArray());
            Assert.Equal("", ErsatzRestwertKennzeichen.Herleitung(null, null, CultureInfo.GetCultureInfo("de-DE")));
            Assert.Equal("Kennzeichen der Position — Ersatzbeschaffung: nein · Restwert: wie bisher.",
                         ErsatzRestwertKennzeichen.Herleitung(false, null, CultureInfo.GetCultureInfo("de-DE")));
        }

        // =====================================================================
        //  (3) Lesen, Schreiben, Übernahme und die Probe an 1024
        // =====================================================================

        /// <summary>Die Tafel „Ersatz und Restwert" nennt den Grund je Position.</summary>
        [Fact]
        public void Die_Tafel_nennt_das_abgewaehlte_Kennzeichen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            var eingaben = new List<ErsatzRestwertTafel.Eingabe>
            {
                new ErsatzRestwertTafel.Eingabe { Bezeichnung = "WP", Betrag = 6001, Nutzungsdauer = 15, ErsatzFuehren = false },
                new ErsatzRestwertTafel.Eingabe { Bezeichnung = "Kessel", Betrag = 5000, Nutzungsdauer = 25, RestwertAnsetzen = false }
            };
            IList<ErsatzRestwertTafel.Zeile> zeilen = ErsatzRestwertTafel.Zeilen(eingaben, 1, "Technik", 3.0, 20, 0);

            Assert.Equal(3, zeilen.Count);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ND_TAFEL_ERSATZ_AUS, zeilen[0].Ersatz);
            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.ND_TAFEL_RESTWERT_AUS, zeilen[1].Restwert);
            Assert.Equal("", zeilen[1].RestwertBarwert);
        }

        /// <summary>Die Kaskade liest die Kennzeichen, die Investitionsliste reicht sie
        /// an die Kapitalwertrechnung durch.</summary>
        [Fact]
        public void Die_Kaskade_liest_die_Kennzeichen_und_reicht_sie_durch()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Assert.True(ErsatzRestwertKennzeichen.Schreibe(SchemaKatalog.TAB_PROJEKTWERTE, ZEILE_WP, false, null));
            Assert.True(ErsatzRestwertKennzeichen.Schreibe(SchemaKatalog.TAB_PROJEKTWERTE, ZEILE_KESSEL, null, false));

            Dictionary<int, ErsatzRestwertKennzeichen.Paar> karte = ErsatzRestwertKennzeichen.LiesProjekt(PROJEKT);
            Assert.Equal(2, karte.Count);
            Assert.False(karte[ZEILE_WP].ErsatzFuehren);
            Assert.Null(karte[ZEILE_WP].RestwertAnsetzen);
            Assert.False(karte[ZEILE_KESSEL].RestwertAnsetzen);

            Dictionary<int, InvestKaskade.Zeile> kaskade = InvestKaskade.NachId(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET);
            Assert.False(kaskade[ZEILE_WP].ErsatzFuehren);
            Assert.False(kaskade[ZEILE_KESSEL].RestwertAnsetzen);

            double zuschuss;
            List<KapitalwertRechner.InvestPosition> liste =
                WirtschaftlichkeitCtrl.LiesInvestitionen(PROJEKT, WirtschaftlichkeitSzenario.ERWARTET, out zuschuss);
            Assert.Contains(liste, p => p.Betrag == 6001 && p.ErsatzFuehren == false);
            Assert.Contains(liste, p => p.Betrag == 5000 && p.RestwertAnsetzen == false);
        }

        /// <summary>Die Vorlagenübernahme trägt die Kennzeichen in die frische Projektzeile.</summary>
        [Fact]
        public void Die_Vorlagenuebernahme_traegt_die_Kennzeichen_mit()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            object o = DataRepository.ExecuteScalar(
                "SELECT MIN(p.ID) FROM Tab_KostenVorlagePosition AS p INNER JOIN Tab_KostenVorlage AS v " +
                "ON p.VorlageID = v.ID WHERE v.KategorieID = 1");
            Assert.True(o != null && o != DBNull.Value, "Die Testdatenbank führt keine Investitionsvorlage.");
            int positionId = Convert.ToInt32(o);
            int vorlageId = Zahl("SELECT VorlageID FROM Tab_KostenVorlagePosition WHERE ID = " + positionId);

            Assert.True(ErsatzRestwertKennzeichen.Schreibe(SchemaKatalog.TAB_KOSTENVORLAGEPOSITION, positionId, false, true));
            KostenVorlagenPosition gelesen = KostenVorlagenCtrl.Positionen(vorlageId).Single(p => p.Id == positionId);
            Assert.False(gelesen.ErsatzFuehren);
            Assert.True(gelesen.RestwertAnsetzen);

            KostenVorlageKopf kopf = KostenVorlagenCtrl.Vorlagen(
                    Zahl("SELECT KomponentenID FROM Tab_KostenVorlage WHERE ID = " + vorlageId), 1)
                .Single(k => k.Id == vorlageId);
            int neuesProjekt = Zahl("SELECT MAX(ID) FROM Tab_Projekt");
            int vorher = Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ErsatzFuehren = 0 AND RestwertAnsetzen = 1");
            UebernahmeErgebnis e = KostenVorlagenUebernahmeCtrl.AusVorlage(neuesProjekt, kopf);
            Assert.True(e.Angelegt > 0 || e.Uebersprungen > 0, string.Join(" ", e.Meldungen));
            if (e.Angelegt > 0)
                Assert.True(Zahl("SELECT COUNT(*) FROM Tab_ProjektWerte WHERE ErsatzFuehren = 0 AND RestwertAnsetzen = 1") > vorher);
        }

        /// <summary>
        /// <b>A/B an 1024</b> mit gepflegten Nutzungsdauern (Wärmepumpe 15 a, Kessel
        /// 25 a): ohne Kennzeichen, mit „Ersatz nein" an der Wärmepumpe, mit „Restwert
        /// nein" am Kessel und mit „nein/nein" an beiden. Gemessen 23.09.2026 auf der
        /// Arbeitskopie (Schemastand 107).
        /// </summary>
        [Fact]
        public void Probe_an_1024_mit_beiden_Kennzeichen()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            DataRepository.ExecuteNonQuery("UPDATE Tab_ProjektWerte SET Nutzungsdauer = 15 WHERE ID = " + ZEILE_WP);
            DataRepository.ExecuteNonQuery("UPDATE Tab_ProjektWerte SET Nutzungsdauer = 25 WHERE ID = " + ZEILE_KESSEL);
            Assert.Equal(-2897442.20, Kapitalwert(), 2);

            ErsatzRestwertKennzeichen.Schreibe(SchemaKatalog.TAB_PROJEKTWERTE, ZEILE_WP, false, null);
            Assert.Equal(-2895805.46, Kapitalwert(), 2);

            ErsatzRestwertKennzeichen.Schreibe(SchemaKatalog.TAB_PROJEKTWERTE, ZEILE_WP, null, null);
            ErsatzRestwertKennzeichen.Schreibe(SchemaKatalog.TAB_PROJEKTWERTE, ZEILE_KESSEL, null, false);
            Assert.Equal(-2897995.88, Kapitalwert(), 2);

            ErsatzRestwertKennzeichen.Schreibe(SchemaKatalog.TAB_PROJEKTWERTE, ZEILE_WP, false, false);
            ErsatzRestwertKennzeichen.Schreibe(SchemaKatalog.TAB_PROJEKTWERTE, ZEILE_KESSEL, false, false);
            Assert.Equal(-2896359.13, Kapitalwert(), 2);   // = der Anker ohne Nutzungsdauern
        }

        private static double Kapitalwert()
        {
            var ctrl = new WirtschaftlichkeitCtrl();
            WirtschaftlichkeitParameter p = ctrl.LadeParameter(PROJEKT);
            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT, IstStamm = true, Projektname = "Probe " + PROJEKT,
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT)
            };
            KostenEmissionRechner.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            WirtschaftlichkeitErgebnis e = new WirtschaftlichkeitCtrl().Berechne(daten, p).First(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == PROJEKT);
            Assert.True(e.Kapitalwert.HasValue, "Kapitalwert fehlt.");
            return e.Kapitalwert.Value;
        }

        private static int Zahl(string sql)
        {
            object o = DataRepository.ExecuteScalar(sql);
            return o == null || o == DBNull.Value ? 0 : Convert.ToInt32(o);
        }
    }
}
