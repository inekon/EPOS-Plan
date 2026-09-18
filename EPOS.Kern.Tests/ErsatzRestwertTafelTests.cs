using System;
using System.Collections.Generic;
using WindowsFormsApplication1;
using Xunit;
using MyResource = WindowsFormsApplication1.MyResource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Nachweis von U30</b> — die Tafel „Ersatz und Restwert" unter dem
    /// Investitionsraster der Kostenverwaltung, gerechnet am Beispiel des Mockups
    /// <c>Dialog_Formel_Zahlenprobe.html</c> (Abschnitt 1, Zone „Ersatz und
    /// Restwert").
    ///
    /// <para><b>Die eigentliche Prüfung ist die ZWEITE Klasse von Fällen:</b> dass
    /// <see cref="KapitalwertRechner.Ersatz"/> und
    /// <see cref="KapitalwertRechner.Rechne"/> dieselben Zahlen liefern. U30 hat die
    /// Ersatz- und Restwertlogik aus <c>Rechne</c> herausgelöst, damit die Tafel sie
    /// MITBENUTZT statt sie nachzubauen; wäre die Herauslösung schief, stünden im
    /// Dialog andere Zahlen als im Kapitalwert — genau der Zustand, den der
    /// Anwenderbefund W5‑B‑7 bei der Investitionskaskade vorgefunden hat.</para>
    ///
    /// <para><b>Ohne Datenbank</b>, wo es ohne geht: Ersatz, Restwert und Barwert
    /// sind reine Rechnung. Die Texte der Tafel (Herkunft der Dauer, Hinweis)
    /// brauchen die Nutzungsdauertabelle und stehen deshalb in der Sammlung
    /// „Testdatenbank".</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class ErsatzRestwertTafelTests : IClassFixture<TestDatenbank>, IDisposable
    {
        private readonly TestDatenbank _db;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        /// <summary><c>Tab_KostenKomponente.ID</c> des BHKW (Saat des Schritts 75).</summary>
        private const int BHKW = 7;

        public ErsatzRestwertTafelTests(TestDatenbank db)
        {
            _db = db;
            NutzungsdauerCtrl.ProbeVergessen();
        }

        public void Dispose() => _kultur.Dispose();

        // =================================================================
        //  Die Zahlen des Mockups
        // =================================================================

        /// <summary>
        /// Das BHKW des Mockups: 196.080,00 € Modul und 9.804,00 € Montage mit
        /// n = 15, 13.000,00 € Hydraulik mit n = 20 = T, 21.888,40 € Planung ohne
        /// Dauer. Bei i = 3 %, T = 20 a und p_I = 0 fällt EIN Ersatz im Jahr 15 über
        /// 205.884,00 € an; sein Barwert ist 132.149 €.
        /// </summary>
        [Fact]
        public void Ersatz_faellt_im_Jahr_15_und_ist_132149_Euro_wert()
        {
            KapitalwertRechner.Ersatzbild modul = KapitalwertRechner.Ersatz(
                Position(196080.00, 15), 20);

            Assert.Equal(new[] { 15 }, modul.Ersatzjahre);
            Assert.Equal(196080.00, modul.Ersatzbetraege[0], 6);

            double ersatz = 205884.00;   // Modul + Montage
            Assert.Equal(132149, Math.Round(KapitalwertRechner.Barwert(ersatz, 15, 3.0)));
        }

        /// <summary>
        /// Der lineare Restwert derselben zwei Positionen: Von den fünfzehn Jahren
        /// der zweiten Beschaffung sind im Jahr 20 noch zehn offen, also
        /// 205.884,00 × 10/15 = 137.256,00 € nominal — abgezinst 75.995 €.
        /// </summary>
        [Fact]
        public void Restwert_ist_137256_Euro_nominal_und_75995_Euro_als_Barwert()
        {
            KapitalwertRechner.Ersatzbild modul = KapitalwertRechner.Ersatz(
                Position(196080.00, 15), 20);
            KapitalwertRechner.Ersatzbild montage = KapitalwertRechner.Ersatz(
                Position(9804.00, 15), 20);

            double restwert = modul.Restwert + montage.Restwert;
            Assert.Equal(137256.00, Math.Round(restwert, 2));
            Assert.Equal(75995, Math.Round(KapitalwertRechner.Barwert(restwert, 20, 3.0)));
        }

        /// <summary>
        /// n = T heißt: kein Ersatz (im letzten Jahr wird nicht mehr ersetzt) und
        /// kein Restwert (die Position ist aufgebraucht). Ohne gepflegte Dauer
        /// dasselbe — nur dass die Position das als <c>OhneDauer</c> ausweist und
        /// die Tafel dort einen Gedankenstrich zeigt statt einer 0.
        /// </summary>
        [Fact]
        public void Dauer_gleich_T_und_keine_Dauer_ergeben_weder_Ersatz_noch_Restwert()
        {
            KapitalwertRechner.Ersatzbild hydraulik = KapitalwertRechner.Ersatz(
                Position(13000.00, 20), 20);
            Assert.Empty(hydraulik.Ersatzjahre);
            Assert.Equal(0.0, hydraulik.Restwert);
            Assert.False(hydraulik.OhneDauer);

            KapitalwertRechner.Ersatzbild planung = KapitalwertRechner.Ersatz(
                Position(21888.40, 0), 20);
            Assert.Empty(planung.Ersatzjahre);
            Assert.Equal(0.0, planung.Restwert);
            Assert.True(planung.OhneDauer);
            Assert.Equal(20.0, planung.Nutzungsdauer);   // still wie T
        }

        /// <summary>
        /// Der Wechselrichter der Photovoltaik: 24.000,00 € mit n = 12. Ohne p_I ist
        /// der Barwert 16.833 €, mit p_I = 2 %/a kostet der Tausch im Jahr 12
        /// 30.437,80 € und der Barwert steigt auf 21.348 € (Mockup, Zeile „Wirkung
        /// von p_I").
        /// </summary>
        [Fact]
        public void Der_Preisaenderungssatz_indiziert_die_Ersatzbeschaffung()
        {
            KapitalwertRechner.Ersatzbild ohne = KapitalwertRechner.Ersatz(
                Position(24000.00, 12), 20);
            Assert.Equal(16833, Math.Round(
                KapitalwertRechner.Barwert(ohne.Ersatzbetraege[0], 12, 3.0)));

            KapitalwertRechner.Ersatzbild mit = KapitalwertRechner.Ersatz(
                Position(24000.00, 12), 20, 2.0);
            Assert.Equal(30437.80, Math.Round(mit.Ersatzbetraege[0], 2));
            Assert.Equal(21348, Math.Round(
                KapitalwertRechner.Barwert(mit.Ersatzbetraege[0], 12, 3.0)));
        }

        // =================================================================
        //  DIESELBE Rechnung wie die Kapitalwertrechnung
        // =================================================================

        /// <summary>
        /// <b>Die Kernprüfung.</b> Dieselben vier Positionen durch
        /// <see cref="KapitalwertRechner.Rechne"/> gerechnet müssen Zahl für Zahl
        /// dasselbe ergeben wie die Summe der Einzelbilder — sonst hätte die Tafel
        /// eine zweite Wahrheit.
        /// </summary>
        [Fact]
        public void Rechne_und_Ersatz_liefern_dieselbe_Ersatzreihe_und_denselben_Restwert()
        {
            var positionen = new List<KapitalwertRechner.InvestPosition>
            {
                Position(196080.00, 15), Position(9804.00, 15),
                Position(13000.00, 20), Position(21888.40, 0)
            };

            KapitalwertRechner.Zahlungsbild bild = KapitalwertRechner.Rechne(
                positionen, 0, 0, 0, 3.0, 20, 0, 0);

            double[] ersatzJeJahr = new double[21];
            double restwert = 0;
            foreach (KapitalwertRechner.InvestPosition pos in positionen)
            {
                KapitalwertRechner.Ersatzbild b = KapitalwertRechner.Ersatz(pos, 20);
                for (int n = 0; n < b.Ersatzjahre.Count; n++)
                    ersatzJeJahr[b.Ersatzjahre[n]] += b.Ersatzbetraege[n];
                restwert += b.Restwert;
            }

            for (int t = 0; t <= 20; t++)
                Assert.Equal(bild.ErsatzJeJahr[t], ersatzJeJahr[t]);
            Assert.Equal(bild.RestwertNominal, restwert);
            Assert.Equal(205884.00, Math.Round(ersatzJeJahr[15], 2));
            Assert.Equal(137256.00, Math.Round(restwert, 2));
        }

        /// <summary>Dasselbe mit p_I und einem verschobenen Startjahr (KD6).</summary>
        [Fact]
        public void Rechne_und_Ersatz_stimmen_auch_mit_pI_und_Startjahr_ueberein()
        {
            var positionen = new List<KapitalwertRechner.InvestPosition>
            {
                Position(24000.00, 12), Position(96000.00, 25),
                new KapitalwertRechner.InvestPosition
                {
                    Betrag = 45000.00, Nutzungsdauer = 8, StartJahr = 3
                }
            };

            KapitalwertRechner.Zahlungsbild bild = KapitalwertRechner.Rechne(
                positionen, 0, 0, 0, 3.0, 20, 0, 0, preisstInvestProzent: 2.0);

            double[] ersatzJeJahr = new double[21];
            double restwert = 0;
            foreach (KapitalwertRechner.InvestPosition pos in positionen)
            {
                KapitalwertRechner.Ersatzbild b = KapitalwertRechner.Ersatz(pos, 20, 2.0);
                if (b.StartJahr > 0) ersatzJeJahr[b.StartJahr] += pos.Betrag;
                for (int n = 0; n < b.Ersatzjahre.Count; n++)
                    ersatzJeJahr[b.Ersatzjahre[n]] += b.Ersatzbetraege[n];
                restwert += b.Restwert;
            }

            for (int t = 0; t <= 20; t++)
                Assert.Equal(bild.ErsatzJeJahr[t], ersatzJeJahr[t]);
            Assert.Equal(bild.RestwertNominal, restwert);
        }

        // =================================================================
        //  Die Tafel und ihr Hinweis
        // =================================================================

        /// <summary>
        /// Die Tafel des Mockups: vier Positionszeilen und eine Summenzeile mit
        /// „3 von 4", dem Ersatzjahr, den Barwerten und der Vorgabe der Technik.
        /// </summary>
        [Fact]
        public void Die_Tafel_zeigt_je_Position_eine_Zeile_und_eine_Summenzeile()
        {
            if (!_db.Vorhanden) return;

            IList<ErsatzRestwertTafel.Zeile> zeilen = ErsatzRestwertTafel.Zeilen(
                Mockup(), BHKW, "Blockheizkraftwerk", 3.0, 20, 0);

            Assert.Equal(5, zeilen.Count);
            Assert.Equal("196.080,00", zeilen[0].Betrag);
            Assert.Equal("15 a", zeilen[0].Dauer);
            Assert.Equal("Jahr 15", zeilen[0].Ersatz);
            Assert.Equal("130.720,00", zeilen[0].Restwert);   // 196.080 × 10/15

            // Hydraulik: n = T, also kein Ersatz und Restwert 0,00.
            Assert.Equal(MyResource.Resource.ND_TAFEL_GLEICH_T, zeilen[2].Ersatz);
            Assert.Equal("0,00", zeilen[2].Restwert);

            // Planung: keine Dauer, also Gedankenstriche statt Nullen.
            Assert.Equal(MyResource.Resource.ND_TAFEL_STRICH, zeilen[3].Dauer);
            Assert.Equal(MyResource.Resource.ND_TAFEL_WIE_T, zeilen[3].Ersatz);
            Assert.Equal(MyResource.Resource.ND_TAFEL_STRICH, zeilen[3].Restwert);

            ErsatzRestwertTafel.Zeile summe = zeilen[4];
            Assert.True(summe.IstSumme);
            Assert.Equal("Blockheizkraftwerk", summe.Position);
            Assert.Equal("240.772,40", summe.Betrag);
            Assert.Equal("3 von 4", summe.Dauer);
            Assert.Contains("205.884,00", summe.Ersatz);
            Assert.Contains("132.149", summe.ErsatzBarwert);
            Assert.Equal("137.256,00", summe.Restwert);
            Assert.Contains("75.995", summe.RestwertBarwert);
            Assert.Contains("15", summe.Herkunft);
        }

        /// <summary>
        /// Die Herkunft der Dauer: Der Wert der Standardzeile gilt als „Vorgabe der
        /// Technik", ein abweichender als „eigener Wert", keiner als „keine Dauer
        /// gepflegt".
        /// </summary>
        [Fact]
        public void Die_Herkunft_unterscheidet_Vorgabe_eigenen_Wert_und_keine_Dauer()
        {
            if (!_db.Vorhanden) return;

            Assert.Equal(MyResource.Resource.ND_HERK_TECHNIK,
                         NutzungsdauerCtrl.Herkunft(BHKW, null, 15));
            Assert.Equal(MyResource.Resource.ND_QUELLE_EIGEN,
                         NutzungsdauerCtrl.Herkunft(BHKW, null, 17));
            Assert.Equal(MyResource.Resource.ND_HERK_KEINE,
                         NutzungsdauerCtrl.Herkunft(BHKW, null, null));

            // Mit Positionsart: die Zeile „MSR" des BHKW trägt 12 a.
            int msr = NutzungsdauerSchema.ZeileZu(BHKW, "MSR");
            Assert.True(msr > 0);
            Assert.Contains("MSR", NutzungsdauerCtrl.Herkunft(BHKW, msr, 12));
        }

        /// <summary>
        /// Die Herleitungszeile des Rasters nennt Wert UND Herkunft; ohne Dauer
        /// bleibt sie leer (dort steht nichts im Feld, und der Grund gehört unter
        /// die Tafel).
        /// </summary>
        [Fact]
        public void Die_Herleitungszeile_nennt_Wert_und_Herkunft()
        {
            if (!_db.Vorhanden) return;

            string s = NutzungsdauerCtrl.Herleitungszeile(BHKW, null, 15);
            Assert.Contains("15", s);
            Assert.Contains(MyResource.Resource.ND_HERK_TECHNIK, s);
            Assert.Equal("", NutzungsdauerCtrl.Herleitungszeile(BHKW, null, null));
        }

        /// <summary>
        /// Der Hinweis über der Tafel (Konzept Wirtschaftlichkeit § 2.13 (3)): T über
        /// der Vorgabe der Technik UND die Zahl der Positionen ohne Dauer samt
        /// Betrag; dazu der Schlusssatz, der ihn als Prüfauftrag kenntlich macht.
        /// </summary>
        [Fact]
        public void Der_Hinweis_nennt_T_die_Vorgabe_und_die_Positionen_ohne_Dauer()
        {
            if (!_db.Vorhanden) return;

            string s = ErsatzRestwertTafel.Hinweis(Mockup(), BHKW, "Blockheizkraftwerk", 20);

            Assert.Contains("20", s);
            Assert.Contains("15", s);
            Assert.Contains("Blockheizkraftwerk", s);
            Assert.Contains("1 von 4", s);
            Assert.Contains("Planung und Genehmigung", s);
            Assert.Contains("21.888,40", s);
            Assert.Contains(MyResource.Resource.ND_TAFEL_HINWEIS_SCHLUSS, s);
        }

        /// <summary>
        /// Trägt jede Position ihre Dauer und liegt T nicht über der Vorgabe, gibt es
        /// nichts zu prüfen — dann steht kein Hinweis.
        /// </summary>
        [Fact]
        public void Ohne_Anlass_bleibt_der_Hinweis_leer()
        {
            if (!_db.Vorhanden) return;

            var alleGepflegt = new List<ErsatzRestwertTafel.Eingabe>
            {
                Eingabe("Modul", 196080.00, 15)
            };
            // T = 15 = Vorgabe der Technik, jede Position hat eine Dauer.
            Assert.Equal("", ErsatzRestwertTafel.Hinweis(
                alleGepflegt, BHKW, "Blockheizkraftwerk", 15));

            Assert.Equal("", ErsatzRestwertTafel.Hinweis(
                new List<ErsatzRestwertTafel.Eingabe>(), BHKW, "Blockheizkraftwerk", 20));
        }

        /// <summary>
        /// Eine Erlös-/Zuschusszeile bekommt weder Ersatz noch Restwert (K5) — sie
        /// steht gar nicht erst in der Tafel und zählt auch im Hinweis nicht mit.
        /// </summary>
        [Fact]
        public void Zuschusszeilen_stehen_nicht_in_der_Tafel()
        {
            if (!_db.Vorhanden) return;

            var mit = new List<ErsatzRestwertTafel.Eingabe>
            {
                Eingabe("Modul", 196080.00, 15),
                new ErsatzRestwertTafel.Eingabe
                {
                    Bezeichnung = "Zuschuss", Betrag = 6000.00, IstErloes = true
                }
            };

            IList<ErsatzRestwertTafel.Zeile> zeilen = ErsatzRestwertTafel.Zeilen(
                mit, BHKW, "Blockheizkraftwerk", 3.0, 20, 0);

            Assert.Equal(2, zeilen.Count);            // eine Position + Summe
            Assert.Equal("1 von 1", zeilen[1].Dauer);
        }

        /// <summary>Ohne Betrachtungszeitraum gibt es keine Tafel und keinen Hinweis.</summary>
        [Fact]
        public void Ohne_Betrachtungszeitraum_bleibt_die_Tafel_leer()
        {
            Assert.Empty(ErsatzRestwertTafel.Zeilen(Mockup(), BHKW, "BHKW", 3.0, 0, 0));
            Assert.Equal("", ErsatzRestwertTafel.Hinweis(Mockup(), BHKW, "BHKW", 0));
            Assert.Empty(ErsatzRestwertTafel.Zeilen(null, BHKW, "BHKW", 3.0, 20, 0));
        }

        // ----------------------------------------------------------- Hilfen ---

        private static KapitalwertRechner.InvestPosition Position(double betrag, double n)
            => new KapitalwertRechner.InvestPosition { Betrag = betrag, Nutzungsdauer = n };

        private static ErsatzRestwertTafel.Eingabe Eingabe(string name, double betrag,
                                                           double? n)
            => new ErsatzRestwertTafel.Eingabe
            {
                Bezeichnung = name, Betrag = betrag, Nutzungsdauer = n
            };

        /// <summary>Die vier Positionen des Blockheizkraftwerks aus dem Mockup.</summary>
        private static List<ErsatzRestwertTafel.Eingabe> Mockup()
            => new List<ErsatzRestwertTafel.Eingabe>
            {
                Eingabe("BHKW-Modul", 196080.00, 15),
                Eingabe("Montage und Inbetriebnahme", 9804.00, 15),
                Eingabe("Hydraulik und Einbindung", 13000.00, 20),
                Eingabe("Planung und Genehmigung", 21888.40, null)
            };
    }
}
