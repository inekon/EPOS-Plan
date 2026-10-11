using System;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Erzeuger;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Kern-Rundreise der Klappliste „Kältemittel“</b> im Katalogdialog der Kältemaschine (KM4-E3-a, Konzept
    /// Kältemittel als Stammdatum 3.2 und 7.1): Eine Wahl aus der Liste speichert den Code als Text, der Schreibweg des
    /// Kerns setzt die ID; ein Bestandstext ohne Tabellenzeile bleibt stehen, die ID bleibt leer. Dazu die GWP-Abweichung
    /// nach KM4-Q17 ohne Datenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class KaeltemaschineKatalogKaeltemittelTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private static int Stamm(string bezeichner) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM " + KaeltemaschineSchema.TAB_STAMM + " WHERE Bezeichner = ?", new DbParam("?", bezeichner)),
            CultureInfo.InvariantCulture);

        /// <summary>Ein eigener Satz als Kopie des ersten Auslieferungssatzes, über den Weg der Hülle.</summary>
        private static KaeltemaschineDaten EigenerSatz(string name)
        {
            KaeltemaschineSpeicherErgebnis e = KaeltemaschineKatalogHuelle.Duplizieren(
                Stamm(KaeltemaschineSchema.SAAT[0].Bezeichner), name);
            Assert.True(e.Ok, e.Meldung);
            return KaeltemaschineKatalogHuelle.AlsDaten(KaeltemaschineStammCtrl.Laden(e.Id));
        }

        [Fact]
        public void Die_Wahl_aus_der_Liste_setzt_die_ID_ein_Bestandstext_laesst_sie_leer()
        {
            if (!_db.Vorhanden) return;
            var liste = KaeltemaschineKatalogHuelle.Kaeltemittelliste();
            Assert.NotEmpty(liste);
            KaeltemittelListeneintrag r32 = liste.Single(w => w.Code == "R32");
            Assert.Equal(KaeltemittelCtrl.IdZumCode("R32"), r32.Id);

            KaeltemaschineDaten d = EigenerSatz("Kältemittel Rundreise");
            d.Kaeltemittel = r32.Code;                       // die Wahl der Klappliste
            KaeltemaschineSpeicherErgebnis s = KaeltemaschineKatalogHuelle.Speichern(d);
            Assert.True(s.Ok, s.Meldung);
            KaeltemaschineModel m = KaeltemaschineStammCtrl.Laden(s.Id);
            Assert.Equal("R32", m.Kaeltemittel);
            Assert.Equal(r32.Id, m.ID_Kaeltemittel);

            // Bestandstext ohne Tabellenzeile: Text bleibt, ID leer.
            d = KaeltemaschineKatalogHuelle.AlsDaten(m);
            d.Kaeltemittel = "Frigen X";
            Assert.True(KaeltemaschineKatalogHuelle.Speichern(d).Ok);
            m = KaeltemaschineStammCtrl.Laden(s.Id);
            Assert.Equal("Frigen X", m.Kaeltemittel);
            Assert.Null(m.ID_Kaeltemittel);
            Assert.Equal(-1, KaeltemittelLesung.Platz(liste, m.Kaeltemittel));
        }

        [Fact]
        public void Die_Abweichung_zaehlt_erst_ueber_einem_Prozent()
        {
            Assert.False(KaeltemittelZulaessigkeitszeile.GwpWeichtAb(675, 681));
            Assert.True(KaeltemittelZulaessigkeitszeile.GwpWeichtAb(675, 682));
            Assert.True(KaeltemittelZulaessigkeitszeile.GwpWeichtAb(675, 600));
            Assert.False(KaeltemittelZulaessigkeitszeile.GwpWeichtAb(null, 600));
            Assert.False(KaeltemittelZulaessigkeitszeile.GwpWeichtAb(675, null));
            Assert.True(KaeltemittelZulaessigkeitszeile.GwpWeichtAb(0, 1));
            Assert.False(KaeltemittelZulaessigkeitszeile.GwpWeichtAb(0, 0));
        }

        [Fact]
        public void Die_Lesewerte_ohne_GWP_lassen_nur_das_GWP_weg()
        {
            var s = new KaeltemittelCtrl.Kaeltemittelsatz(1, "R32", "Difluormethan", "HFKW", 675, "AR5", true, false, "A2L",
                                                          null, null, null, 1, true);
            string mit = KaeltemittelZulaessigkeitszeile.Lesewerte(s);
            string ohne = KaeltemittelZulaessigkeitszeile.LesewerteOhneGwp(s);
            Assert.StartsWith(string.Format(CultureInfo.CurrentCulture, WindowsFormsApplication1.MyResource.Resource.KM_LESE_GWP, "675"), mit);
            Assert.EndsWith(ohne, mit);
            Assert.DoesNotContain("675", ohne);
            Assert.Equal("", KaeltemittelZulaessigkeitszeile.LesewerteOhneGwp(null));
        }
    }
}
