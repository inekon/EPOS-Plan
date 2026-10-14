using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Eingabearten Monatswerte und Wochenprofil der Kältebedarfsprofile</b> (Konzept Kältebedarfsprofile 3.2, 3.3,
    /// 3.5; Welle KBP-3a): die reine Rechnung (<see cref="Kaelteprofileingabe"/>), die Ablage in der Projektkopie über
    /// <see cref="WizardCtrl.Add_Projekt_Kaelte"/> samt Kopie bei Bearbeitung, die unveränderte Kette
    /// <see cref="ProfilBedarf"/> (Jahressumme, Monatssummen, Wochentage, Feiertag gleich Sonntag), 'jahr' byte-gleich zum
    /// heutigen Weg und die Mitzieher Duplizieren und Projektpaket für Eingabeart und Kälte-Art.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaelteprofileingabeTests : IDisposable
    {
        private const int PROJEKT = 1017;
        private const string BUERO = "Raumkühlung Büro";
        private readonly TestDatenbank _db = new TestDatenbank();

        public KaelteprofileingabeTests()
        {
            // Die Testdatenbank steht vor dem Schritt der Eingabeart; er läuft hier wie beim Programmstart.
            if (_db.Vorhanden) KaeltebedarfsprofilVorlagenSchema.Ausfuehren(null);
        }

        public void Dispose() => _db.Dispose();

        // ---- Die reine Rechnung --------------------------------------------------------------------------------------

        [Fact]
        public void Monatswerte_in_MWh_ergeben_Jahressumme_und_Anteile_und_zurueck()
        {
            double?[] mwh = { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 0, 5 };
            (double summe, double[] anteile) = Kaelteprofileingabe.AusMwh(mwh);
            Assert.Equal(60.0, summe, 12);
            Assert.Equal(100.0, anteile.Sum(), 9);
            Assert.Equal(0.0, anteile[10]);
            double?[] zurueck = Kaelteprofileingabe.NachMwh(anteile, summe);
            for (int m = 0; m < 12; m++) Assert.Equal(mwh[m]!.Value, zurueck[m]!.Value, 9);
            Assert.Null(Kaelteprofileingabe.PruefeMonate(mwh, prozent: false));
            Assert.NotNull(Kaelteprofileingabe.PruefeMonate(new double?[12], prozent: false));          // Summe 0
            Assert.NotNull(Kaelteprofileingabe.PruefeMonate(new double?[] { -1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1 }, false));
        }

        [Fact]
        public void In_Prozent_muss_die_Summe_100_ergeben()
        {
            double?[] gut = { 5, 5, 5, 10, 10, 15, 15, 10, 10, 5, 5, 5 };
            Assert.Null(Kaelteprofileingabe.PruefeMonate(gut, prozent: true));
            double?[] schlecht = (double?[])gut.Clone();
            schlecht[0] = 6;                                                                               // 101 %
            Assert.NotNull(Kaelteprofileingabe.PruefeMonate(schlecht, prozent: true));
            schlecht[0] = null;
            Assert.NotNull(Kaelteprofileingabe.PruefeMonate(schlecht, prozent: true));
        }

        [Fact]
        public void Die_Dreitagesansicht_schreibt_Montag_bis_Freitag_gleich()
        {
            double[] woche = Kaelteprofileingabe.WocheAusTagen(Tag(1.0), Tag(0.5), Tag(0.25));
            Assert.Equal(168, woche.Length);
            Assert.True(Kaelteprofileingabe.WerktageGleich(woche));
            Assert.Equal(Tag(1.0), Kaelteprofileingabe.Tag(woche, 4));
            Assert.Equal(Tag(0.5), Kaelteprofileingabe.Tag(woche, Kaelteprofileingabe.SAMSTAG));
            Assert.Equal(Tag(0.25), Kaelteprofileingabe.Tag(woche, Kaelteprofileingabe.SONNTAG));
            woche[24 + 3] = 7;                                                                             // Dienstag 3 Uhr
            Assert.False(Kaelteprofileingabe.WerktageGleich(woche));
            Assert.Null(Kaelteprofileingabe.PruefeWoche(woche));
            Assert.NotNull(Kaelteprofileingabe.PruefeWoche(new double[168]));
        }

        // ---- Ablage und Reihe ------------------------------------------------------------------------------------------

        [Fact]
        public void Monatswerte_in_MWh_ergeben_die_8760_Reihe_mit_Jahres_und_Monatssummen()
        {
            if (!_db.Vorhanden) return;
            double?[] mwh = { 1, 1.5, 2, 3, 4, 6, 8, 7, 5, 3, 0, 1 };
            (double summe, double[] anteile) = Kaelteprofileingabe.AusMwh(mwh);
            Speichern(new Z_ProjektKaeltebedarfModel
            {
                ID_Kaeltebedarf = StammId(BUERO), Bezeichner = BUERO, Summe = summe,
                Eingabeart = DbWerte.KAELTE_EINGABE_MONAT, Monatsanteile = anteile
            });

            Z_ProjektKaeltebedarfModel z = Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT).Single();
            Assert.Equal(DbWerte.KAELTE_EINGABE_MONAT, z.Eingabeart);
            Assert.Equal(summe, z.Summe, 12);
            double[] kopie = KaeltebedarfStammCtrl.ProjektMonate(z.ID_Kaeltebedarf);
            for (int m = 0; m < 12; m++) Assert.Equal(anteile[m], kopie[m], 12);
            Assert.NotEqual(Katalogmonate(BUERO), kopie);                                                // Katalog unberührt

            (double[] reihe, double[] monate, int[] a, int[] e) = Rechnen();
            Assert.Equal(8760, reihe.Length);
            Assert.Equal(summe * 1000, reihe.Sum(), 6);
            for (int m = 0; m < 12; m++) Assert.Equal(mwh[m]!.Value, monate[m], 9);
            for (int h = a[10]; h <= e[10]; h++) Assert.Equal(0.0, reihe[h]);                              // November 0 MWh
        }

        [Fact]
        public void Monatswerte_in_Prozent_behalten_die_Jahressumme()
        {
            if (!_db.Vorhanden) return;
            double[] anteile = { 2, 3, 5, 8, 10, 14, 16, 15, 12, 8, 4, 3 };
            Speichern(new Z_ProjektKaeltebedarfModel
            {
                ID_Kaeltebedarf = StammId(BUERO), Bezeichner = BUERO, Summe = 30,
                Eingabeart = DbWerte.KAELTE_EINGABE_MONAT, Monatsanteile = anteile
            });
            (double[] reihe, double[] monate, _, _) = Rechnen();
            Assert.Equal(30_000.0, reihe.Sum(), 6);
            for (int m = 0; m < 12; m++) Assert.Equal(30.0 * anteile[m] / 100.0, monate[m], 9);
        }

        [Fact]
        public void Wochenprofil_Werktag_Samstag_Sonntag_und_168h_tragen_die_Wochentage()
        {
            if (!_db.Vorhanden) return;
            double[] woche = Kaelteprofileingabe.WocheAusTagen(Tag(1.0), Tag(0.5), Tag(0.25));
            woche[24 + 10] = 3.0;                                                                         // 168 h: Dienstag 10 Uhr
            Speichern(new Z_ProjektKaeltebedarfModel
            {
                ID_Kaeltebedarf = StammId(BUERO), Bezeichner = BUERO, Summe = 40,
                Eingabeart = DbWerte.KAELTE_EINGABE_WOCHE, Wochenprofil = woche
            });
            Z_ProjektKaeltebedarfModel z = Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT).Single();
            Assert.Equal(DbWerte.KAELTE_EINGABE_WOCHE, z.Eingabeart);
            Assert.Equal(woche, KaeltebedarfStammCtrl.ProjektWoche(z.ID_Kaeltebedarf));

            (double[] reihe, _, int[] a, _) = Rechnen();
            Assert.Equal(40_000.0, reihe.Sum(), 6);
            int w0 = Konditionierungdatenweg.Raster(PROJEKT, ProfilBedarf.WochentagJan1AusKlimaregion(Klimaregion())).W0;
            // Im Juli (ohne bundeseinheitlichen Feiertag): Werktag : Samstag : Sonntag = 1 : 0,5 : 0,25 je Stunde.
            int mo = TagImMonat(6, w0, 0, a), di = TagImMonat(6, w0, 1, a);
            int sa = TagImMonat(6, w0, Kaelteprofileingabe.SAMSTAG, a), so = TagImMonat(6, w0, Kaelteprofileingabe.SONNTAG, a);
            double basis = reihe[mo * 24 + 5];
            Assert.True(basis > 0);
            Assert.Equal(0.5, reihe[sa * 24 + 5] / basis, 9);
            Assert.Equal(0.25, reihe[so * 24 + 5] / basis, 9);
            Assert.Equal(3.0, reihe[di * 24 + 10] / basis, 9);
        }

        [Fact]
        public void Mit_Betriebskalender_zaehlt_ein_Feiertag_wie_Sonntag()
        {
            if (!_db.Vorhanden) return;
            int kal = BetriebskalenderCtrl.Speichern(new Betriebskalender { Bezeichner = "KBP-3a Feiertage", FeiertagWieSonntag = true });
            Assert.True(kal > 0);
            Speichern(new Z_ProjektKaeltebedarfModel
            {
                ID_Kaeltebedarf = StammId(BUERO), Bezeichner = BUERO, Summe = 40, ID_Betriebskalender = kal,
                Eingabeart = DbWerte.KAELTE_EINGABE_WOCHE,
                Wochenprofil = Kaelteprofileingabe.WocheAusTagen(Tag(1.0), Tag(0.5), Tag(0.25))
            });
            (double[] reihe, _, int[] a, _) = Rechnen();

            int w0In = ProfilBedarf.WochentagJan1AusKlimaregion(Klimaregion());
            Gemeinjahrkalender raster = Konditionierungdatenweg.Raster(PROJEKT, w0In);
            int geprueft = 0;
            foreach (int t in Landesfeiertage.Jahrestage(null, raster))
            {
                int tag = t - 1;
                int wt = ((raster.W0 + tag) % 7 + 7) % 7;
                if (wt >= Kaelteprofileingabe.SAMSTAG) continue;                                            // nur Werktags-Feiertage
                int monat = Monat(tag, a);
                int sonntag = TagImMonat(monat, raster.W0, Kaelteprofileingabe.SONNTAG, a);
                for (int s = 0; s < 24; s++)
                    Assert.Equal(reihe[sonntag * 24 + s], reihe[tag * 24 + s], 12);
                geprueft++;
            }
            Assert.True(geprueft > 0, "Kein Feiertag an einem Werktag im Raster.");
        }

        [Fact]
        public void Jahr_rechnet_byte_gleich_wie_der_heutige_Weg()
        {
            if (!_db.Vorhanden) return;
            Speichern(new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = StammId(BUERO), Bezeichner = BUERO, Summe = 25 });
            (double[] heute, double[] mHeute, _, _) = Rechnen();
            double[] kopieVorher = KaeltebedarfStammCtrl.ProjektMonate(Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT).Single().ID_Kaeltebedarf);

            List<Z_ProjektKaeltebedarfModel> zeilen = Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT);
            Assert.Equal(DbWerte.KAELTE_EINGABE_JAHR, zeilen[0].Eingabeart);
            zeilen[0].Eingabeart = DbWerte.KAELTE_EINGABE_JAHR;
            Assert.True(new WizardCtrl().Del_Projekt_Kaelte(PROJEKT, 0));
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, zeilen));
            (double[] neu, double[] mNeu, _, _) = Rechnen();

            for (int h = 0; h < 8760; h++)
                Assert.Equal(BitConverter.DoubleToInt64Bits(heute[h]), BitConverter.DoubleToInt64Bits(neu[h]));
            Assert.Equal(mHeute, mNeu);
            Assert.Equal(Katalogmonate(BUERO), kopieVorher);
        }

        [Fact]
        public void Eine_geteilte_Kopie_wird_beim_Bearbeiten_abgespalten()
        {
            if (!_db.Vorhanden) return;
            int stamm = StammId(BUERO);
            Speichern(new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = stamm, Bezeichner = BUERO, Summe = 10 },
                      new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = stamm, Bezeichner = BUERO, Summe = 20 });
            List<Z_ProjektKaeltebedarfModel> zeilen = Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT);
            Assert.Equal(zeilen[0].ID_Kaeltebedarf, zeilen[1].ID_Kaeltebedarf);                           // eine Kopie, zwei Zeilen
            int geteilt = zeilen[0].ID_Kaeltebedarf;

            double[] anteile = { 0, 0, 5, 10, 15, 20, 20, 15, 10, 5, 0, 0 };
            zeilen[1].Eingabeart = DbWerte.KAELTE_EINGABE_MONAT;
            zeilen[1].Monatsanteile = anteile;
            Assert.True(new WizardCtrl().Del_Projekt_Kaelte(PROJEKT, 0));
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, zeilen));

            List<Z_ProjektKaeltebedarfModel> danach = Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT);
            Assert.Equal(geteilt, danach[0].ID_Kaeltebedarf);
            Assert.NotEqual(geteilt, danach[1].ID_Kaeltebedarf);
            Assert.Equal(Katalogmonate(BUERO), KaeltebedarfStammCtrl.ProjektMonate(geteilt));             // unbearbeitet
            Assert.Equal(anteile, KaeltebedarfStammCtrl.ProjektMonate(danach[1].ID_Kaeltebedarf));
            Assert.NotNull(KaeltebedarfStammCtrl.ProjektWoche(danach[1].ID_Kaeltebedarf));               // Typ reist mit
            Assert.Equal(2L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltebedarf WHERE ID_Projekt = ?", PROJEKT));
        }

        // ---- Mitzieher ---------------------------------------------------------------------------------------------------

        [Fact]
        public void Das_Duplizieren_traegt_Eingabeart_und_Kaelte_Art()
        {
            if (!_db.Vorhanden) return;
            MitEingabeartUndArt();
            int kopie = new ProjektDuplizierenCtrl().Duplizieren(Projektname(PROJEKT), "KBP-3a Kopie");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");
            PruefeEingabeartUndArt(kopie);
        }

        [Fact]
        public void Das_Projektpaket_traegt_Eingabeart_und_Kaelte_Art()
        {
            if (!_db.Vorhanden) return;
            MitEingabeartUndArt();
            string ordner = Path.Combine(Path.GetTempPath(), "KBP3a_Paket_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "kbp3a.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(Projektname(PROJEKT), paket));
                int neu = io.Importieren(paket, "KBP-3a Paket", ProjektExportImportCtrl.BeiVorhandenem.NeuerName, null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                PruefeEingabeartUndArt(neu);
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { }
            }
        }

        // ---- Helfer ---------------------------------------------------------------------------------------------------

        private static readonly double[] WOCHE_MITZIEHER = Kaelteprofileingabe.WocheAusTagen(Tag(2.0), Tag(1.0), Tag(0.5));

        private static void MitEingabeartUndArt()
        {
            Speichern(new Z_ProjektKaeltebedarfModel
            {
                ID_Kaeltebedarf = StammId(BUERO), Bezeichner = BUERO, Summe = 15, KaelteArt = KaelteArtSchema.PROZESS,
                Eingabeart = DbWerte.KAELTE_EINGABE_WOCHE, Wochenprofil = WOCHE_MITZIEHER
            });
            int kopie = Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT).Single().ID_Kaeltebedarf;
            Assert.True(DataRepository.ExecuteSQL("UPDATE Tab_Kaeltebedarf SET Kaelte_Art = ? WHERE ID = ?",
                                                  new DbParam("@a", KaelteArtSchema.PROZESS), new DbParam("@id", kopie)));
        }

        private static void PruefeEingabeartUndArt(int ziel)
        {
            Assert.NotEqual(PROJEKT, ziel);
            Z_ProjektKaeltebedarfModel z = Z_ProjektKaeltebedarfCtrl.LiesProjekt(ziel).Single();
            Assert.Equal(DbWerte.KAELTE_EINGABE_WOCHE, z.Eingabeart);
            Assert.Equal(KaelteArtSchema.PROZESS, z.KaelteArt);
            Assert.Equal(KaelteArtSchema.PROZESS, Convert.ToString(DataRepository.ExecuteScalar(
                "SELECT Kaelte_Art FROM Tab_Kaeltebedarf WHERE ID = ? AND ID_Projekt = ?",
                new DbParam("@id", z.ID_Kaeltebedarf), new DbParam("@p", ziel)), CultureInfo.InvariantCulture));
            Assert.Equal(WOCHE_MITZIEHER, KaeltebedarfStammCtrl.ProjektWoche(z.ID_Kaeltebedarf));
        }

        private static void Speichern(params Z_ProjektKaeltebedarfModel[] zeilen)
        {
            Assert.True(new WizardCtrl().Del_Projekt_Kaelte(PROJEKT, 0));
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, zeilen.ToList()));
        }

        private static (double[] reihe, double[] monate, int[] a, int[] e) Rechnen()
        {
            (int[] a, int[] e) = Monate();
            var ziel = new double[8760];
            var monate = new double[12];
            ProfilBedarf.Rechnen(ProfilQuelle.Kaelte(ProfilQuellmodus.Projektrechnung), PROJEKT, null,
                                 ProfilBedarf.WochentagJan1AusKlimaregion(Klimaregion()), a, e, ziel, monate);
            return (ziel, monate, a, e);
        }

        private static (int[] anfang, int[] ende) Monate()
        {
            var a = new int[12];
            var e = new int[12];
            int h = 0;
            for (int m = 0; m < 12; m++)
            {
                a[m] = h;
                h += Feiertage.TageJeMonat[m] * 24;
                e[m] = h - 1;
            }
            return (a, e);
        }

        /// <summary>Der erste Tag (0-basiert) des Monats <paramref name="monat"/> mit dem Wochentag <paramref name="wochentag"/>.</summary>
        private static int TagImMonat(int monat, int w0, int wochentag, int[] a)
        {
            for (int t = a[monat] / 24; ; t++)
                if (((w0 + t) % 7 + 7) % 7 == wochentag) return t;
        }

        private static int Monat(int tag, int[] a)
        {
            int m = 0;
            while (m < 11 && a[m + 1] / 24 <= tag) m++;
            return m;
        }

        private static double[] Tag(double wert) => Enumerable.Repeat(wert, 24).ToArray();

        private static double[] Katalogmonate(string bezeichner)
        {
            System.Data.DataTable dt = DataRepository.GetDataTable("SELECT * FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?",
                                                                   new DbParam("@b", bezeichner));
            return Enumerable.Range(1, 12).Select(m => dt.Rows[0]["Monat_" + m] == DBNull.Value ? 0.0
                : Convert.ToDouble(dt.Rows[0]["Monat_" + m], CultureInfo.InvariantCulture)).ToArray();
        }

        private static int Klimaregion()
            => (int)Zahl("SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = ?", PROJEKT);

        private static int StammId(string bezeichner)
            => (int)Zahl("SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?", bezeichner);

        private static string Projektname(int id)
            => Convert.ToString(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                                                             new DbParam("@id", id)), CultureInfo.InvariantCulture);

        private static long Zahl(string sql, object p)
        {
            object o = DataRepository.ExecuteScalar(sql, new DbParam("@p", p));
            return o == null || o == DBNull.Value ? -1 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
