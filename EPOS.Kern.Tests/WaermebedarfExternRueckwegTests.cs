using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Rückweg des Dialogs „Wärmebedarf extern“ / „Kältebedarf extern“ verliert kein Feld</b> (Konzept
    /// Kältebedarf 3.2, 3.4 und 5): Die Hülle lädt die Zuordnungen (<see cref="Z_ProjektGebGanglinieCtrl.LiesProjekt"/>),
    /// der Dialog meldet jede Änderung über den Rückruf <see cref="WaermebedarfExternAbbildung.Rueckweg"/>, und die
    /// Startseite speichert die Liste mit Löschen + Neuanlegen (<see cref="WizardCtrl.Del_WaermebedarfExtern(int, DbVorgang)"/>,
    /// <see cref="WizardCtrl.Add_WaermebedarfExtern"/>). Der Dialog kennt die Deckungsfelder nicht — der Rückweg muss
    /// sie aus dem geladenen Modell fortschreiben, sonst fiele eine Split-Zeile still auf „zentral“ zurück.
    /// </summary>
    [Collection("Testdatenbank")]
    public class WaermebedarfExternRueckwegTests : IDisposable
    {
        private const string HEIZUNG = "RW_Heizung";
        private const string PROZESS = "RW_Prozess";
        private const string KUEHLUNG = "RW_Kuehlung";
        private const string NEU = "RW_Neu";
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Unveraendert_gespeichert_bleiben_alle_Felder_gleich()
        {
            if (!_db.Vorhanden) return;
            int projekt = Anlegen("RW unverändert", out string vorher);

            Speichern(projekt, (zeilen, rueckweg) => rueckweg());

            Assert.Equal(vorher, Abdruck(projekt));
        }

        [Fact]
        public void Eine_Aenderung_an_einer_anderen_Zeile_laesst_die_Splitzeile_unveraendert()
        {
            if (!_db.Vorhanden) return;
            int projekt = Anlegen("RW andere Zeile", out string vorher);

            // Der Dialog ändert die Kälte-Art einer ANDEREN Zeile — wie jeder Klick, der „Geaendert“ auslöst.
            Speichern(projekt, (zeilen, rueckweg) =>
            {
                zeilen.Single(z => z.Bezeichner == HEIZUNG).KaelteArt = KaelteArtSchema.PROZESS;
                rueckweg();
            });

            Assert.Equal(vorher, Abdruck(projekt));
        }

        [Fact]
        public void Die_Kaelteart_der_Splitzeile_folgt_dem_Dialog_die_Deckung_bleibt()
        {
            if (!_db.Vorhanden) return;
            int projekt = Anlegen("RW Kälte-Art", out _);

            Speichern(projekt, (zeilen, rueckweg) =>
            {
                zeilen.Single(z => z.Bezeichner == KUEHLUNG).KaelteArt = KaelteArtSchema.PROZESS;
                rueckweg();
            });

            Z_ProjWaermebedarfModel kuehl = Z_ProjektGebGanglinieCtrl.LiesProjekt(projekt)
                .Single(m => m.m_szBezeichner == KUEHLUNG);
            Assert.Equal(KaelteArtSchema.PROZESS, kuehl.KaelteArt);
            Assert.Equal(KaeltebedarfSchema.DECKUNG_SPLIT, kuehl.Deckung);
            Assert.Equal(3.0, kuehl.Eer1);
            Assert.True(kuehl.KuehlEigenerZaehler);
            Assert.NotNull(kuehl.KuehlIdCarrier);
        }

        [Fact]
        public void Eine_neue_Zeile_steht_auf_zentral_die_alten_behalten_ihre_Felder()
        {
            if (!_db.Vorhanden) return;
            int projekt = Anlegen("RW neue Zeile", out string vorher);
            Ganglinien(NEU);

            Speichern(projekt, (zeilen, rueckweg) =>
            {
                zeilen.Add(new WaermebedarfExternZeile
                {
                    IdZ = 0, IdGanglinie = StammId(NEU), Bezeichner = NEU, Kanal = DbWerte.KANAL_KUEHLUNG
                });
                rueckweg();
            });

            List<Z_ProjWaermebedarfModel> nachher = Z_ProjektGebGanglinieCtrl.LiesProjekt(projekt);
            Z_ProjWaermebedarfModel neu = nachher.Single(m => m.m_szBezeichner == NEU);
            Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, neu.Deckung);
            Assert.Null(neu.Eer1);
            Assert.False(neu.KuehlEigenerZaehler);
            Assert.Equal(vorher, Abdruck(nachher.Where(m => m.m_szBezeichner != NEU)));
        }

        [Fact]
        public void Ein_Kanalwechsel_weg_von_Kuehlung_leert_die_Splitfelder()
        {
            if (!_db.Vorhanden) return;
            int projekt = Anlegen("RW Kanalwechsel", out _);

            Speichern(projekt, (zeilen, rueckweg) =>
            {
                zeilen.Single(z => z.Bezeichner == KUEHLUNG).Kanal = DbWerte.KANAL_HEIZUNG;
                rueckweg();
            });

            Z_ProjWaermebedarfModel z = Z_ProjektGebGanglinieCtrl.LiesProjekt(projekt).Single(m => m.m_szBezeichner == KUEHLUNG);
            Assert.Equal(DbWerte.KANAL_HEIZUNG, z.Kanal);
            Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, z.Deckung);
            Assert.Null(z.Eer1);
            Assert.Null(z.KuehlIdCarrier);
            Assert.False(z.KuehlEigenerZaehler);
        }

        // ---- K5a: Modus „nur Kühlung“ und der Speicherweg der Hülle (WaermebedarfExternAbbildung.Speichern) ------------

        [Fact]
        public void NurKuehlung_laesst_jede_Waermezeile_samt_Kennung_unveraendert()
        {
            if (!_db.Vorhanden) return;
            int projekt = Anlegen("RW nur Kühlung", out _);
            Ganglinien(NEU);
            string waerme = WaermeAbdruck(projekt);

            List<Z_ProjWaermebedarfModel> liste = WaermebedarfExternAbbildung.Modelle(
                Z_ProjektGebGanglinieCtrl.LiesProjekt(projekt), WaermebedarfExternModus.NurKuehlung);
            Assert.Single(liste);
            List<WaermebedarfExternZeile> zeilen = WaermebedarfExternAbbildung.Zeilen(liste);
            Action rueckweg = WaermebedarfExternAbbildung.Rueckweg(zeilen, liste, projekt);
            zeilen[0].Eer1 = 2.8;
            zeilen.Add(new WaermebedarfExternZeile
            {
                IdZ = 0, IdGanglinie = StammId(NEU), Bezeichner = NEU, Kanal = DbWerte.KANAL_KUEHLUNG
            });
            rueckweg();

            Assert.Null(WaermebedarfExternAbbildung.Speichern(projekt, liste, WaermebedarfExternModus.NurKuehlung));

            List<Z_ProjWaermebedarfModel> nachher = Z_ProjektGebGanglinieCtrl.LiesProjekt(projekt);
            Assert.Equal(waerme, WaermeAbdruck(projekt));
            Assert.Equal(4, nachher.Count);
            Assert.Equal(2.8, nachher.Single(m => m.m_szBezeichner == KUEHLUNG).Eer1);
            Assert.Equal(DbWerte.KANAL_KUEHLUNG, nachher.Single(m => m.m_szBezeichner == NEU).Kanal);
        }

        [Fact]
        public void NurKuehlung_schreibt_auch_bei_Waermezeilen_im_Aufruf_nur_die_Kuehlzeilen()
        {
            if (!_db.Vorhanden) return;
            int projekt = Anlegen("RW nur Kühlung Filter", out _);
            string waerme = WaermeAbdruck(projekt);

            // Eine Liste MIT Wärmezeilen (ein falsch verdrahteter Aufrufer): Der Speicherweg filtert selbst.
            List<Z_ProjWaermebedarfModel> alle = Z_ProjektGebGanglinieCtrl.LiesProjekt(projekt);
            Assert.Null(WaermebedarfExternAbbildung.Speichern(projekt, alle, WaermebedarfExternModus.NurKuehlung));

            Assert.Equal(waerme, WaermeAbdruck(projekt));
            Assert.Equal(3, Z_ProjektGebGanglinieCtrl.LiesProjekt(projekt).Count);
        }

        [Fact]
        public void Alle_speichert_wie_bisher()
        {
            if (!_db.Vorhanden) return;
            int projekt = Anlegen("RW Alle", out string vorher);

            List<Z_ProjWaermebedarfModel> liste = Z_ProjektGebGanglinieCtrl.LiesProjekt(projekt);
            List<WaermebedarfExternZeile> zeilen = WaermebedarfExternAbbildung.Zeilen(liste);
            WaermebedarfExternAbbildung.Rueckweg(zeilen, liste, projekt)();
            Assert.Null(WaermebedarfExternAbbildung.Speichern(projekt, liste, WaermebedarfExternModus.Alle));

            Assert.Equal(vorher, Abdruck(projekt));
        }

        [Fact]
        public void Split_ohne_EER_wird_benannt_abgelehnt_und_nichts_geschrieben()
        {
            if (!_db.Vorhanden) return;
            int projekt = Anlegen("RW ohne EER", out string vorher);
            string mitKennung = WaermeAbdruck(projekt, alle: true);

            foreach (WaermebedarfExternModus modus in new[] { WaermebedarfExternModus.Alle, WaermebedarfExternModus.NurKuehlung })
            {
                List<Z_ProjWaermebedarfModel> liste = Z_ProjektGebGanglinieCtrl.LiesProjekt(projekt);
                liste.Single(m => m.m_szBezeichner == KUEHLUNG).Eer1 = 0;

                string grund = WaermebedarfExternAbbildung.Speichern(projekt, liste, modus);

                Assert.NotNull(grund);
                Assert.StartsWith(WindowsFormsApplication1.MyResource.Resource.DECK_MSG_OHNE_EER, grund);
                Assert.Equal(vorher, Abdruck(projekt));
                Assert.Equal(mitKennung, WaermeAbdruck(projekt, alle: true));
            }
        }

        /// <summary>Die Wärmezeilen (alle außer Kühlung) samt Kennung ID_Z — „byte-gleich“ heißt hier: jede Spalte.</summary>
        private static string WaermeAbdruck(int projekt, bool alle = false)
        {
            return string.Join("\n", Z_ProjektGebGanglinieCtrl.LiesProjekt(projekt)
                .Where(m => alle || !Z_ProjektKaeltebedarfCtrl.IstKuehlkanal(m))
                .OrderBy(m => m.m_ID_Z)
                .Select(m => m.m_ID_Z + "|" + Abdruck(new[] { m })));
        }

        // ---- Ablauf wie Startseite → Hülle → Dialog → Speichern -------------------------------------------------------

        /// <summary>
        /// Der Weg <c>StartseiteHuelle.WaermebedarfDaten</c>: laden, Kanäle nachladen (<c>WaermebedarfExternHuelle.Gaben</c>),
        /// Dialogzeilen bauen, die Dialoghandlung ausführen, dann Löschen + Neuanlegen.
        /// </summary>
        private static void Speichern(int projekt, Action<List<WaermebedarfExternZeile>, Action> dialog)
        {
            List<Z_ProjWaermebedarfModel> liste = Z_ProjektGebGanglinieCtrl.LiesProjekt(projekt);
            Z_ProjektGebGanglinieCtrl.KanaeleNachladen(projekt, liste);
            List<WaermebedarfExternZeile> zeilen = WaermebedarfExternAbbildung.Zeilen(liste);
            Action rueckweg = WaermebedarfExternAbbildung.Rueckweg(zeilen, liste, projekt);

            dialog(zeilen, rueckweg);

            var wiz = new WizardCtrl();
            Assert.True(wiz.Del_WaermebedarfExtern(projekt));
            Assert.True(wiz.Add_WaermebedarfExtern(projekt, liste));
        }

        /// <summary>Ein Projekt mit einer Heiz-, einer Prozess- und einer Split-Kühlzeile; Abdruck nach dem Anlegen.</summary>
        private static int Anlegen(string name, out string abdruck)
        {
            int projekt = ProjektAnlegen(name);
            Ganglinien(HEIZUNG, PROZESS, KUEHLUNG);
            int traeger = (int)Zahl("SELECT MIN(id) FROM energy_carrier WHERE id > ?", 0);
            var liste = new List<Z_ProjWaermebedarfModel>
            {
                new Z_ProjWaermebedarfModel { m_ID_Ganglinie = StammId(HEIZUNG), m_szBezeichner = HEIZUNG, Kanal = DbWerte.KANAL_HEIZUNG },
                new Z_ProjWaermebedarfModel { m_ID_Ganglinie = StammId(PROZESS), m_szBezeichner = PROZESS, Kanal = DbWerte.KANAL_PROZESS },
                new Z_ProjWaermebedarfModel
                {
                    m_ID_Ganglinie = StammId(KUEHLUNG), m_szBezeichner = KUEHLUNG, Kanal = DbWerte.KANAL_KUEHLUNG,
                    Deckung = KaeltebedarfSchema.DECKUNG_SPLIT, EerWeg = KaeltebedarfSchema.EER_LINEAR,
                    Eer1 = 3, Taussen1 = 35, Eer2 = 4, Taussen2 = 25, KuehlIdCarrier = traeger, KuehlEigenerZaehler = true,
                    KaelteArt = KaelteArtSchema.RAUM
                },
            };
            Assert.True(new WizardCtrl().Add_WaermebedarfExtern(projekt, liste));
            abdruck = Abdruck(projekt);
            Assert.Contains(KaeltebedarfSchema.DECKUNG_SPLIT, abdruck);
            return projekt;
        }

        private static string Abdruck(int projekt) => Abdruck(Z_ProjektGebGanglinieCtrl.LiesProjekt(projekt));

        /// <summary>Alle Felder der Zuordnungen außer der Kennung, nach Bezeichner geordnet.</summary>
        private static string Abdruck(IEnumerable<Z_ProjWaermebedarfModel> liste)
        {
            return string.Join("\n", liste.OrderBy(m => m.m_szBezeichner, StringComparer.Ordinal).Select(m => string.Join("|",
                m.m_szBezeichner, m.m_ID_Ganglinie, m.Kanal, m.Deckung, m.EerWeg, F(m.Eer1), F(m.Taussen1), F(m.Eer2),
                F(m.Taussen2), m.KuehlIdCarrier, m.KuehlEigenerZaehler, m.KaelteArt)));
        }

        private static string F(double? d) => d?.ToString("R", CultureInfo.InvariantCulture) ?? "";

        private static void Ganglinien(params string[] namen)
        {
            var werte = new List<double> { 1.0, 2.0, 3.0, 4.0 };
            foreach (string n in namen)
                if (Zahl("SELECT COUNT(*) FROM Tab_Waermebedarf_STAMM WHERE Bezeichner = ?", n) == 0)
                    Assert.True(new WaermebedarfStammCtrl().ImportGanglinie(n, werte));
        }

        private static int StammId(string name) => new WaermebedarfStammCtrl().GetStammId(name);

        private static int ProjektAnlegen(string name)
        {
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Projekt (Projektname) VALUES (?)",
                                           new DbParam("@name", DbParamTyp.VarWChar) { Wert = name });
            return (int)Zahl("SELECT MAX(ID) FROM Tab_Projekt WHERE ID > ?", 0);
        }

        private static long Zahl(string sql, object p)
            => Convert.ToInt64(DataRepository.ExecuteScalar(sql, new DbParam("@p", p)), CultureInfo.InvariantCulture);
    }
}
