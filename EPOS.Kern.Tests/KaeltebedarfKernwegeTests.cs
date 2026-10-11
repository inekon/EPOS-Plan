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
    /// <b>Die Kernwege der Bedarfsart Kälte, Stufe K2</b> (Konzept Kältebedarf, Abschnitt 7): Vorschau über
    /// <see cref="BedarfsVorschauCtrl"/>, Deckungsart der Lastgangzeilen im Kanal Kühlung
    /// (<see cref="WizardCtrl.Add_WaermebedarfExtern"/>, Nachladen, Abdruck des Assistenten), Kühlzeilen speichern lässt
    /// Wärmezeilen unverändert, Duplizieren und Projekttransfer mit Kopf, Typ und Zuordnung samt Deckung.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaeltebedarfKernwegeTests : IDisposable
    {
        private const int PROJEKT = 1017;
        private const string BUERO = "Raumkühlung Büro";
        private const string KUEHLRAUM = "Kühlraum";
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        // ---- Vorschau -------------------------------------------------------------------------------------------------

        [Fact]
        public void Die_Katalogvorschau_rechnet_das_Kaelteprofil()
        {
            if (!_db.Vorhanden) return;
            BedarfsVorschau v = BedarfsVorschauCtrl.Rechnen(BedarfsArt.Kaelte, 0, KUEHLRAUM);
            Assert.True(v.Erfolgreich);
            Assert.Equal(BedarfsArt.Kaelte, v.Art);
            Assert.Null(v.Waerme);
            Assert.Null(v.Strom);
            Assert.Equal(8760, v.KaelteStunde.Length);
            Assert.All(v.KaelteStunde, x => Assert.True(x > 0));          // Kühlraum: rund um die Uhr
            Assert.True(v.Kaeltebedarf > 0);
            Assert.Equal(v.Kaeltebedarf, v.KaelteMonat.Sum(), 6);         // Monate [MWh] = Jahr [MWh]
            Assert.True(v.KaelteMonat[6] > v.KaelteMonat[0]);             // Sommeranstieg

            Assert.False(BedarfsVorschauCtrl.Rechnen(BedarfsArt.Kaelte, 0, "").Erfolgreich);
        }

        [Fact]
        public void Die_Projektvorschau_folgt_der_Jahressumme_des_Dialogs()
        {
            if (!_db.Vorhanden) return;
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(PROJEKT, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = StammId(KUEHLRAUM), Bezeichner = KUEHLRAUM, Summe = 50 }
            }));

            BedarfsVorschau gespeichert = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Kaelte, PROJEKT,
                                                                             new[] { KUEHLRAUM });
            Assert.True(gespeichert.Erfolgreich);
            Assert.Equal(50.0, gespeichert.Kaeltebedarf, 6);

            BedarfsVorschau offen = BedarfsVorschauCtrl.ProjektVorschau(BedarfsArt.Kaelte, PROJEKT, new[] { KUEHLRAUM },
                null, new Dictionary<string, double> { { KUEHLRAUM, 80 } });
            Assert.Equal(80.0, offen.Kaeltebedarf, 6);
            Assert.Null(offen.Waerme);                                     // fällt nicht in den Brauchwasserzweig
        }

        // ---- Lastgangzeilen mit Deckungsart --------------------------------------------------------------------------

        [Fact]
        public void Die_Lastgangzeile_im_Kanal_Kuehlung_traegt_die_Deckung_die_Waermezeile_bleibt_zentral()
        {
            if (!_db.Vorhanden) return;
            int projekt = ProjektAnlegen("K2 Lastgang Deckung");
            Ganglinien("K2_Heizung", "K2_Kuehlung");
            int traeger = Traeger();

            var liste = new List<Z_ProjWaermebedarfModel>
            {
                // Eine Split-Angabe an einer Heizzeile ist wirkungslos und wird geleert.
                Split(new Z_ProjWaermebedarfModel { m_szBezeichner = "K2_Heizung", Kanal = DbWerte.KANAL_HEIZUNG }, traeger),
                Split(new Z_ProjWaermebedarfModel { m_szBezeichner = "K2_Kuehlung", Kanal = DbWerte.KANAL_KUEHLUNG }, traeger),
            };
            Assert.True(new WizardCtrl().Add_WaermebedarfExtern(projekt, liste));

            List<Z_ProjWaermebedarfModel> gelesen = Z_ProjektGebGanglinieCtrl.LiesProjekt(projekt);
            Z_ProjWaermebedarfModel heiz = gelesen.Single(z => z.Kanal == DbWerte.KANAL_HEIZUNG);
            Z_ProjWaermebedarfModel kuehl = gelesen.Single(z => z.Kanal == DbWerte.KANAL_KUEHLUNG);
            Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, heiz.Deckung);
            Assert.True(heiz.EerWeg == null && heiz.Eer1 == null && heiz.KuehlIdCarrier == null && !heiz.KuehlEigenerZaehler);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Z_ProjektWaermebedarf WHERE ID_Projekt = ? AND Kanal = 'Heizung' " +
                                  "AND Deckung = 'zentral' AND Split_EER_1 IS NULL AND Kuehl_ID_Carrier IS NULL", projekt));

            Assert.Equal(KaeltebedarfSchema.DECKUNG_SPLIT, kuehl.Deckung);
            Assert.Equal(KaeltebedarfSchema.EER_LINEAR, kuehl.EerWeg);
            Assert.Equal((3.0, 35.0, 4.0, 25.0), (kuehl.Eer1.Value, kuehl.Taussen1.Value, kuehl.Eer2.Value, kuehl.Taussen2.Value));
            Assert.Equal(traeger, kuehl.KuehlIdCarrier);
            Assert.True(kuehl.KuehlEigenerZaehler);

            // Der Nachlader trägt die Deckung in eine Liste, die sie nicht kennt (Assistent, Startseite).
            var ohne = new List<Z_ProjWaermebedarfModel>
            {
                new Z_ProjWaermebedarfModel { m_ID_Z = kuehl.m_ID_Z, m_szBezeichner = "K2_Kuehlung" }
            };
            Z_ProjektGebGanglinieCtrl.KanaeleNachladen(projekt, ohne);
            Assert.Equal(DbWerte.KANAL_KUEHLUNG, ohne[0].Kanal);
            Assert.Equal(KaeltebedarfSchema.DECKUNG_SPLIT, ohne[0].Deckung);
            Assert.Equal(4.0, ohne[0].Eer2);

            // Ein Speichern über Löschen + Neuanlegen behält die Deckung.
            Assert.True(new WizardCtrl().Del_WaermebedarfExtern(projekt));
            Assert.True(new WizardCtrl().Add_WaermebedarfExtern(projekt, gelesen));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Z_ProjektWaermebedarf WHERE ID_Projekt = ? AND Kanal = 'Kuehlung' " +
                                  "AND Deckung = 'split' AND Split_EER_Weg = 'linear' AND Kuehl_EigenerZaehler = 1", projekt));
        }

        [Fact]
        public void Eine_unzulaessige_Deckung_am_Lastgang_wird_nicht_gespeichert()
        {
            if (!_db.Vorhanden) return;
            int projekt = ProjektAnlegen("K2 Lastgang falsch");
            Ganglinien("K2_Kuehlung");
            var falsch = new Z_ProjWaermebedarfModel
            {
                m_szBezeichner = "K2_Kuehlung", Kanal = DbWerte.KANAL_KUEHLUNG, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
                EerWeg = KaeltebedarfSchema.EER_LINEAR, Eer1 = 3, Taussen1 = 30, Eer2 = 4, Taussen2 = 30
            };
            Assert.NotNull(Z_ProjektKaeltebedarfCtrl.Deckungspruefung(falsch));     // T₂ = T₁
            Assert.False(new WizardCtrl().Add_WaermebedarfExtern(projekt, new List<Z_ProjWaermebedarfModel> { falsch }));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Z_ProjektWaermebedarf WHERE ID_Projekt = ?", projekt));

            // Außerhalb des Kanals Kühlung ist dieselbe Angabe zulässig - sie wird geleert.
            falsch.Kanal = DbWerte.KANAL_BRAUCHWASSER;
            Assert.Null(Z_ProjektKaeltebedarfCtrl.Deckungspruefung(falsch));
            Z_ProjektKaeltebedarfCtrl.Normalisieren(falsch);
            Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, falsch.Deckung);
            Assert.Null(falsch.Eer2);
        }

        /// <summary>
        /// <b>Kühlzeilen speichern lässt Wärmezeilen unverändert</b> (Nachweis K2): Speichern nur des Kanals Kühlung über
        /// <c>Del_WaermebedarfExtern(projekt, Kuehlung)</c> + <c>Add_WaermebedarfExtern</c> ändert an den Zeilen der übrigen
        /// Kanäle keine Spalte — auch nicht ihre Deckungsspalten — und lässt die Profilzuordnungen der Kälte stehen.
        /// </summary>
        [Fact]
        public void Kuehlzeilen_speichern_laesst_Waermezeilen_unveraendert()
        {
            if (!_db.Vorhanden) return;
            int projekt = ProjektAnlegen("K2 Kanalfilter");
            Ganglinien("K2_Heizung", "K2_Kuehlung", "K2_Kuehlung_neu");
            WizardCtrl wiz = new WizardCtrl();
            Assert.True(wiz.Add_WaermebedarfExtern(projekt, new List<Z_ProjWaermebedarfModel>
            {
                new Z_ProjWaermebedarfModel { m_szBezeichner = "K2_Heizung", Kanal = DbWerte.KANAL_HEIZUNG },
                new Z_ProjWaermebedarfModel { m_szBezeichner = "K2_Heizung", Kanal = DbWerte.KANAL_BRAUCHWASSER },
                new Z_ProjWaermebedarfModel { m_szBezeichner = "K2_Heizung", Kanal = DbWerte.KANAL_PROZESS },
                new Z_ProjWaermebedarfModel { m_szBezeichner = "K2_Kuehlung", Kanal = DbWerte.KANAL_KUEHLUNG },
            }));
            Assert.True(wiz.Add_Projekt_Kaelte(projekt, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = StammId(BUERO), Bezeichner = BUERO, Summe = 10 }
            }));

            string waerme = Bild("SELECT * FROM Z_ProjektWaermebedarf WHERE ID_Projekt = ? AND Kanal <> 'Kuehlung' ORDER BY ID_Z", projekt);
            string profile = Bild("SELECT * FROM Z_Projekt_Kaeltebedarf WHERE ID_Projekt = ? ORDER BY ID", projekt);

            Assert.True(wiz.Del_WaermebedarfExtern(projekt, DbWerte.KANAL_KUEHLUNG));
            Assert.True(wiz.Add_WaermebedarfExtern(projekt, new List<Z_ProjWaermebedarfModel>
            {
                Split(new Z_ProjWaermebedarfModel { m_szBezeichner = "K2_Kuehlung_neu", Kanal = DbWerte.KANAL_KUEHLUNG }, null),
            }));

            Assert.Equal(waerme, Bild("SELECT * FROM Z_ProjektWaermebedarf WHERE ID_Projekt = ? AND Kanal <> 'Kuehlung' ORDER BY ID_Z", projekt));
            Assert.Equal(profile, Bild("SELECT * FROM Z_Projekt_Kaeltebedarf WHERE ID_Projekt = ? ORDER BY ID", projekt));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Z_ProjektWaermebedarf WHERE ID_Projekt = ? AND Kanal = 'Kuehlung' " +
                                  "AND Bezeichner = 'K2_Kuehlung_neu' AND Deckung = 'split'", projekt));
        }

        [Fact]
        public void Der_Abdruck_des_Assistenten_sieht_die_Deckung_nur_bei_Split()
        {
            var lauf = new AssistentCtrl();
            lauf.Waermebedarf.Add(new Z_ProjWaermebedarfModel { m_szBezeichner = "A", Kanal = DbWerte.KANAL_KUEHLUNG });
            string zentral = AssistentAbgleich.Abdruck(lauf, AssistentGewerk.Waermebedarf);

            var vergleich = new AssistentCtrl();
            vergleich.Waermebedarf.Add(new Z_ProjWaermebedarfModel { m_szBezeichner = "A", Kanal = DbWerte.KANAL_KUEHLUNG,
                                                                    Deckung = KaeltebedarfSchema.DECKUNG_ZENTRAL });
            Assert.Equal(zentral, AssistentAbgleich.Abdruck(vergleich, AssistentGewerk.Waermebedarf));

            Split(lauf.Waermebedarf[0], null);
            string split = AssistentAbgleich.Abdruck(lauf, AssistentGewerk.Waermebedarf);
            Assert.NotEqual(zentral, split);
            lauf.Waermebedarf[0].Eer1 = 3.5;
            Assert.NotEqual(split, AssistentAbgleich.Abdruck(lauf, AssistentGewerk.Waermebedarf));
        }

        // ---- Duplizieren und Transfer ----------------------------------------------------------------------------------

        [Fact]
        public void Das_Duplizieren_traegt_Kopf_Typ_und_Zuordnung_samt_Deckung_auf_die_eigene_Kopie()
        {
            if (!_db.Vorhanden) return;
            int traeger = Traeger();
            ZuordnungenAnlegen(PROJEKT, traeger);

            int kopie = new ProjektDuplizierenCtrl().Duplizieren(Projektname(PROJEKT), "K2 Kopie");
            Assert.True(kopie > 0, "Duplizieren fehlgeschlagen.");

            PruefeKopie(kopie, traeger, ursprungErwartet: true);
            // Die Quelle bleibt unverändert.
            Assert.Equal(2, Z_ProjektKaeltebedarfCtrl.LiesProjekt(PROJEKT).Count);
        }

        [Fact]
        public void Der_Projekttransfer_traegt_Kopf_Typ_und_Zuordnung_ohne_Ursprung()
        {
            if (!_db.Vorhanden) return;
            int traeger = Traeger();
            ZuordnungenAnlegen(PROJEKT, traeger);

            string ordner = Path.Combine(Path.GetTempPath(), "K2_Transfer_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(ordner);
            try
            {
                string paket = Path.Combine(ordner, "kaelte.wpx");
                var io = new ProjektExportImportCtrl();
                Assert.True(io.Exportieren(Projektname(PROJEKT), paket));
                int neu = io.Importieren(paket, "K2 Transfer", ProjektExportImportCtrl.BeiVorhandenem.NeuerName,
                                         null, out string fehler);
                Assert.True(neu > 0, "Import fehlgeschlagen: " + fehler);
                Assert.NotEqual(PROJEKT, neu);

                PruefeKopie(neu, traeger, ursprungErwartet: false);
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { }
            }
        }

        // ---- Helfer ---------------------------------------------------------------------------------------------------

        /// <summary>Zwei Profilzeilen (zentral, Split) und eine Split-Lastgangzeile im Kanal Kühlung.</summary>
        private static void ZuordnungenAnlegen(int projekt, int traeger)
        {
            int stamm = StammId(BUERO);
            Assert.True(new WizardCtrl().Add_Projekt_Kaelte(projekt, new List<Z_ProjektKaeltebedarfModel>
            {
                new Z_ProjektKaeltebedarfModel { ID_Kaeltebedarf = stamm, Bezeichner = BUERO, Summe = 40 },
                new Z_ProjektKaeltebedarfModel
                {
                    ID_Kaeltebedarf = stamm, Bezeichner = BUERO, Summe = 12, Deckung = KaeltebedarfSchema.DECKUNG_SPLIT,
                    EerWeg = KaeltebedarfSchema.EER_FEST, Eer1 = 3.2, Taussen1 = 35, KuehlIdCarrier = traeger,
                    KuehlEigenerZaehler = true
                },
            }));
            Ganglinien("K2_Kuehlung");
            var zeilen = Z_ProjektGebGanglinieCtrl.LiesProjekt(projekt);
            zeilen.Add(Split(new Z_ProjWaermebedarfModel { m_szBezeichner = "K2_Kuehlung", Kanal = DbWerte.KANAL_KUEHLUNG }, traeger));
            Assert.True(new WizardCtrl().Del_WaermebedarfExtern(projekt));
            Assert.True(new WizardCtrl().Add_WaermebedarfExtern(projekt, zeilen));
        }

        private static void PruefeKopie(int ziel, int traeger, bool ursprungErwartet)
        {
            List<Z_ProjektKaeltebedarfModel> z = Z_ProjektKaeltebedarfCtrl.LiesProjekt(ziel);
            Assert.Equal(2, z.Count);
            int kopf = z[0].ID_Kaeltebedarf;
            Assert.Equal(kopf, z[1].ID_Kaeltebedarf);
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltebedarf WHERE ID = " + kopf + " AND ID_Projekt = ?", ziel));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltetyp WHERE ID_Kaeltebedarf = " + kopf + " AND ID_Projekt = ?", ziel));
            Assert.Equal(0L, Zahl("SELECT COUNT(*) FROM Tab_Kaeltebedarf k JOIN Tab_Kaeltebedarf q ON q.ID = k.ID " +
                                  "WHERE k.ID = " + kopf + " AND q.ID_Projekt = ? AND k.ID_Projekt <> q.ID_Projekt", ziel));
            object stamm = DataRepository.ExecuteScalar("SELECT ID_Stamm FROM Tab_Kaeltebedarf WHERE ID = ?", new DbParam("@k", kopf));
            if (ursprungErwartet) Assert.Equal((long)StammId(BUERO), Convert.ToInt64(stamm, CultureInfo.InvariantCulture));
            else Assert.True(stamm == null || stamm == DBNull.Value, "Der Ursprung reist nicht.");
            Assert.Equal(((double?)16, (double?)19), (z[0].Vorlauf, z[0].Ruecklauf));

            Assert.Equal((40.0, KaeltebedarfSchema.DECKUNG_ZENTRAL), (z[0].Summe, z[0].Deckung));
            Assert.Equal((12.0, KaeltebedarfSchema.DECKUNG_SPLIT, KaeltebedarfSchema.EER_FEST), (z[1].Summe, z[1].Deckung, z[1].EerWeg));
            Assert.Equal(3.2, z[1].Eer1);
            Assert.True(z[1].KuehlEigenerZaehler);
            Assert.Equal(TraegerName(traeger), TraegerName(z[1].KuehlIdCarrier.Value));

            Z_ProjWaermebedarfModel lastgang = Z_ProjektGebGanglinieCtrl.LiesProjekt(ziel).Single(w => w.Kanal == DbWerte.KANAL_KUEHLUNG);
            Assert.Equal(KaeltebedarfSchema.DECKUNG_SPLIT, lastgang.Deckung);
            Assert.Equal(TraegerName(traeger), TraegerName(lastgang.KuehlIdCarrier.Value));
            Assert.Equal(1L, Zahl("SELECT COUNT(*) FROM Tab_Waermebedarf WHERE ID = " + lastgang.m_ID_Ganglinie + " AND ID_Projekt = ?", ziel));
        }

        private static Z_ProjWaermebedarfModel Split(Z_ProjWaermebedarfModel w, int? traeger)
        {
            w.Deckung = KaeltebedarfSchema.DECKUNG_SPLIT;
            w.EerWeg = KaeltebedarfSchema.EER_LINEAR;
            w.Eer1 = 3; w.Taussen1 = 35; w.Eer2 = 4; w.Taussen2 = 25;
            w.KuehlIdCarrier = traeger;
            w.KuehlEigenerZaehler = true;
            return w;
        }

        private static void Ganglinien(params string[] namen)
        {
            var werte = new List<double> { 1.0, 2.0, 3.0, 4.0 };
            foreach (string n in namen)
                if (Zahl("SELECT COUNT(*) FROM Tab_Waermebedarf_STAMM WHERE Bezeichner = ?", n) == 0)
                    Assert.True(new WaermebedarfStammCtrl().ImportGanglinie(n, werte));
        }

        private static int Traeger()
            => (int)Zahl("SELECT MIN(id) FROM energy_carrier WHERE id > ?", 0);

        private static string TraegerName(int id)
            => Convert.ToString(DataRepository.ExecuteScalar("SELECT name FROM energy_carrier WHERE id = ?", new DbParam("@id", id)),
                                CultureInfo.InvariantCulture);

        private static int StammId(string bezeichner)
            => (int)Zahl("SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?", bezeichner);

        private static string Projektname(int id)
            => Convert.ToString(DataRepository.ExecuteScalar("SELECT Projektname FROM Tab_Projekt WHERE ID = ?",
                                                             new DbParam("@id", id)), CultureInfo.InvariantCulture);

        private static int ProjektAnlegen(string name)
        {
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Projekt (Projektname) VALUES (?)",
                                           new DbParam("@name", DbParamTyp.VarWChar) { Wert = name });
            return (int)Zahl("SELECT MAX(ID) FROM Tab_Projekt WHERE ID > ?", 0);
        }

        /// <summary>Alle Zeilen einer Abfrage als ein Text — für den Vorher-nachher-Vergleich.</summary>
        private static string Bild(string sql, object p)
        {
            System.Data.DataTable dt = DataRepository.GetDataTable(sql, new DbParam("@p", p));
            return string.Join("\n", dt.Rows.Cast<System.Data.DataRow>().Select(r =>
                string.Join("|", r.ItemArray.Select(x => Convert.ToString(x, CultureInfo.InvariantCulture)))));
        }

        private static long Zahl(string sql, object p)
        {
            object o = DataRepository.ExecuteScalar(sql, new DbParam("@p", p));
            return o == null || o == DBNull.Value ? -1 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
