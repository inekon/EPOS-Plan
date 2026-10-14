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
    /// <b>Der Rückweg der Kältebedarfsprofile verliert kein Deckungsfeld</b> (K5b, Konzept Kältebedarf 3.3, 3.4): Die
    /// Hülle lädt die Zuordnungen (<see cref="Z_ProjektKaeltebedarfCtrl.LiesProjekt"/>), bildet sie auf die Dialogzeilen
    /// ab (<see cref="KaeltebedarfsprofilAbbildung.Zeilen"/>), der Dialog meldet jede Änderung über den Rückruf
    /// (<see cref="KaeltebedarfsprofilAbbildung.Rueckweg"/>), und die Startseite speichert mit
    /// <see cref="KaeltebedarfsprofilAbbildung.Speichern"/> — gegen eine Kopie der Testdatenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public class KaeltebedarfsprofilAbbildungTests : IDisposable
    {
        private const string BUERO = "Raumkühlung Büro";
        private const string KUEHLRAUM = "Kühlraum";
        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Die_acht_Deckungsfelder_machen_die_Rundreise_ueber_Dialog_und_Speicherweg()
        {
            if (!_db.Vorhanden) return;
            int projekt = ProjektAnlegen("K5b Rundreise");
            int traeger = Traeger();
            Assert.Null(KaeltebedarfsprofilAbbildung.Speichern(projekt, new List<Z_ProjektKaeltebedarfModel>
            {
                new() { ID_Kaeltebedarf = StammId(BUERO), Bezeichner = BUERO, Summe = 12 },
                new() { ID_Kaeltebedarf = StammId(KUEHLRAUM), Bezeichner = KUEHLRAUM, Summe = 30 }
            }));

            // Laden, im Dialog die Bürozeile auf „dezentral, linear“ stellen, zurückschreiben, speichern.
            List<Z_ProjektKaeltebedarfModel> modelle = Z_ProjektKaeltebedarfCtrl.LiesProjekt(projekt);
            List<BedarfsProfilZeile> zeilen = KaeltebedarfsprofilAbbildung.Zeilen(modelle);
            Assert.All(zeilen, z => Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, z.Deckung!.Deckungsart));
            Action rueckweg = KaeltebedarfsprofilAbbildung.Rueckweg(zeilen, modelle, projekt);

            DeckungsartDaten d = zeilen.Single(z => z.Name == BUERO).Deckung!;
            d.DeckungSetzen(KaeltebedarfSchema.DECKUNG_SPLIT);
            d.EerWegSetzen(KaeltebedarfSchema.EER_LINEAR);
            d.Eer1 = 2.8; d.Taussen1 = 32; d.Eer2 = 4.2; d.Taussen2 = 22;
            d.KuehlIdCarrier = traeger; d.KuehlEigenerZaehler = true;
            rueckweg();
            Assert.Null(KaeltebedarfsprofilAbbildung.Speichern(projekt, modelle));

            // Wieder geladen: alle acht Felder stehen, die Kühlraumzeile bleibt zentral, Summen unverändert.
            List<BedarfsProfilZeile> neu = KaeltebedarfsprofilAbbildung.Zeilen(Z_ProjektKaeltebedarfCtrl.LiesProjekt(projekt));
            Assert.Equal(2, neu.Count);
            DeckungsartDaten b = neu.Single(z => z.Name == BUERO).Deckung!;
            Assert.Equal(KaeltebedarfSchema.DECKUNG_SPLIT, b.Deckungsart);
            Assert.Equal(KaeltebedarfSchema.EER_LINEAR, b.EerWeg);
            Assert.Equal(2.8, b.Eer1);
            Assert.Equal(32.0, b.Taussen1);
            Assert.Equal(4.2, b.Eer2);
            Assert.Equal(22.0, b.Taussen2);
            Assert.Equal(traeger, b.KuehlIdCarrier);
            Assert.True(b.KuehlEigenerZaehler);
            Assert.Equal(12.0, neu.Single(z => z.Name == BUERO).Summe, 6);
            Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, neu.Single(z => z.Name == KUEHLRAUM).Deckung!.Deckungsart);

            // Zurück auf „zentral“: der Kern leert die Split-Felder beim Speichern.
            List<Z_ProjektKaeltebedarfModel> m2 = Z_ProjektKaeltebedarfCtrl.LiesProjekt(projekt);
            List<BedarfsProfilZeile> z2 = KaeltebedarfsprofilAbbildung.Zeilen(m2);
            z2.Single(z => z.Name == BUERO).Deckung!.DeckungSetzen(KaeltebedarfSchema.DECKUNG_ZENTRAL);
            KaeltebedarfsprofilAbbildung.Rueckweg(z2, m2, projekt)();
            Assert.Null(KaeltebedarfsprofilAbbildung.Speichern(projekt, m2));
            Z_ProjektKaeltebedarfModel zentral = Z_ProjektKaeltebedarfCtrl.LiesProjekt(projekt).Single(m => m.Bezeichner == BUERO);
            Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, zentral.Deckung);
            Assert.Null(zentral.Eer1);
            Assert.Null(zentral.KuehlIdCarrier);
        }

        [Fact]
        public void Split_ohne_EER_wird_benannt_abgelehnt_und_nichts_geschrieben()
        {
            if (!_db.Vorhanden) return;
            int projekt = ProjektAnlegen("K5b ohne EER");
            Assert.Null(KaeltebedarfsprofilAbbildung.Speichern(projekt, new List<Z_ProjektKaeltebedarfModel>
            {
                new() { ID_Kaeltebedarf = StammId(BUERO), Bezeichner = BUERO, Summe = 12 }
            }));

            List<Z_ProjektKaeltebedarfModel> modelle = Z_ProjektKaeltebedarfCtrl.LiesProjekt(projekt);
            List<BedarfsProfilZeile> zeilen = KaeltebedarfsprofilAbbildung.Zeilen(modelle);
            zeilen[0].Deckung!.DeckungSetzen(KaeltebedarfSchema.DECKUNG_SPLIT);
            zeilen[0].Deckung!.Eer1 = 0;
            zeilen[0].Summe = 99;
            KaeltebedarfsprofilAbbildung.Rueckweg(zeilen, modelle, projekt)();

            string grund = KaeltebedarfsprofilAbbildung.Speichern(projekt, modelle);

            Assert.Equal(WindowsFormsApplication1.MyResource.Resource.DECK_MSG_OHNE_EER + " (" + BUERO + ")", grund);
            Z_ProjektKaeltebedarfModel steht = Z_ProjektKaeltebedarfCtrl.LiesProjekt(projekt).Single();
            Assert.Equal(KaeltebedarfSchema.DECKUNG_ZENTRAL, steht.Deckung);
            Assert.Equal(12.0, steht.Summe, 6);
        }

        private static int ProjektAnlegen(string name)
        {
            DataRepository.ExecuteNonQuery("INSERT INTO Tab_Projekt (Projektname) VALUES (?)",
                                           new DbParam("@name", DbParamTyp.VarWChar) { Wert = name });
            return (int)Zahl("SELECT MAX(ID) FROM Tab_Projekt WHERE ID > ?", 0);
        }

        private static int Traeger() => (int)Zahl("SELECT MIN(id) FROM energy_carrier WHERE id > ?", 0);

        private static int StammId(string bezeichner)
            => (int)Zahl("SELECT ID FROM Tab_Kaeltebedarf_STAMM WHERE Bezeichner = ?", bezeichner);

        private static long Zahl(string sql, object p)
        {
            object o = DataRepository.ExecuteScalar(sql, new DbParam("@p", p));
            return o == null || o == DBNull.Value ? -1 : Convert.ToInt64(o, CultureInfo.InvariantCulture);
        }
    }
}
