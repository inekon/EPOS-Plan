using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Zeile „Rückkühlwerk“ der Tafel der Kälteerzeuger</b> (K-F5b, Entwurf Split/VRF/Rückkühlwerk 5.8): je
    /// Kältemaschine, deren Ergebnis die Größen des Rückkühlwerks trägt, eine Zeile unter der Tafel mit Bauart,
    /// Ventilatorstrom, Wasser, Teil-Freikühlung und mittlerer Rückkühltemperatur; ohne Rückkühlwerk bleibt die Tafel, wie
    /// sie ist. Ohne Datenbank, dazu der Sammelweg gegen die Testdatenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public class RueckkuehlwerkBerichtTests : IDisposable
    {
        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private static List<ErgebnisKaeltemaschineModel> Maschinen(bool mitRk) => new()
        {
            new()
            {
                ID_Kaeltemaschine = 70, Bezeichner = "KM Halle", Kaelteproduktion_MWh = 30, Stromverbrauch_MWh = 10,
                Ventilatorstrom_MWh = mitRk ? 1.234 : null, Wasser_m3 = mitRk ? 456.4 : null,
                TeilFreikuehlung_MWh = mitRk ? 2.5 : null, Rueckkuehltemperatur_Mittel = mitRk ? 24.56 : null
            },
            new() { ID_Kaeltemaschine = 71, Bezeichner = "KM Dach", Kaelteproduktion_MWh = 12, Stromverbrauch_MWh = 4 },
        };

        private static readonly List<RueckkuehlwerkBerichtszeile> Werke = new()
        {
            new() { IdKaeltemaschine = 70, Anlage = "KM Halle", Bezeichner = "Turm 1", Bauart = RueckkuehlwerkSchema.BAUART_KUEHLTURM_OFFEN }
        };

        [Fact]
        public void Ohne_Rueckkuehlwerk_bleibt_die_Tafel_unveraendert()
        {
            Berichtstabelle ohne = Berichtstabellen.Kaelteerzeuger(null, _ => "", false, DE, Maschinen(false));
            Berichtstabelle mitListe = Berichtstabellen.Kaelteerzeuger(null, _ => "", false, DE, Maschinen(false), null, null, Werke);
            Assert.Equal(ohne.ToString(), mitListe.ToString());
            Assert.Empty(mitListe.Hinweise);
            Assert.Equal(2, mitListe.Zeilen.Count);
        }

        [Fact]
        public void Mit_Rueckkuehlwerk_steht_eine_Zeile_mit_Bauart_und_Ergebnissen()
        {
            Berichtstabelle t = Berichtstabellen.Kaelteerzeuger(null, _ => "", false, DE, Maschinen(true), null, null, Werke);
            Assert.Equal(2, t.Zeilen.Count);                                   // die Spalten der Tafel bleiben
            string zeile = Assert.Single(t.Hinweise);
            Assert.Equal("Rückkühlwerk an „KM Halle“ (Turm 1, Kühlturm, offen): Ventilatorstrom 1,23 MWh/a · Wasser 456 m³/a · "
                         + "Teil-Freikühlung 2,50 MWh/a · mittlere Rückkühltemperatur 24,6 °C", zeile);

            Berichtstabelle en = Berichtstabellen.Kaelteerzeuger(null, _ => "", true, CultureInfo.InvariantCulture, Maschinen(true), null, null, Werke);
            Assert.StartsWith("Heat rejection unit at “KM Halle” (Turm 1, Cooling tower, open): fan electricity 1.23 MWh/a", Assert.Single(en.Hinweise));
        }

        [Fact]
        public void Ohne_gesammelten_Satz_steht_ein_Strich_statt_der_Bauart()
        {
            Berichtstabelle t = Berichtstabellen.Kaelteerzeuger(null, _ => "", false, DE, Maschinen(true));
            Assert.StartsWith("Rückkühlwerk an „KM Halle“ (—): Ventilatorstrom 1,23", Assert.Single(t.Hinweise));
        }

        [Fact]
        public void Jede_Bauart_hat_einen_englischen_Berichtstext()
        {
            foreach (string b in RueckkuehlwerkSchema.BAUARTEN)
                Assert.NotEqual(RueckkuehlwerkBerichtszeile.BauartText(b), BerichtTexte.T(RueckkuehlwerkBerichtszeile.BauartText(b), true));
            Assert.Equal("Trockenkühler", RueckkuehlwerkBerichtszeile.BauartText(null));
        }

        [Fact]
        public void Der_Sammler_liest_Name_und_Bauart_je_Anlage()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;
            const int projekt = 1017;
            int stamm = RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel { Bezeichner = "RK Bericht", Bauart = "HYBRID" }).Id;
            int km = Convert.ToInt32(DataRepository.ExecuteScalar("SELECT ID FROM Tab_Kaeltemaschine_STAMM WHERE Bezeichner = ?",
                new DbParam("?", "Kältemaschine 200 kW wassergekühlt mit Trockenkühler")), CultureInfo.InvariantCulture);
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(projekt, km, "KM Bericht");
            Assert.Empty(RueckkuehlwerkBerichtszeile.Sammeln(projekt).Where(z => z.Anlage == "KM Bericht"));
            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, stamm));

            RueckkuehlwerkBerichtszeile z = RueckkuehlwerkBerichtszeile.Sammeln(projekt).Single(x => x.Anlage == "KM Bericht");
            Assert.Equal(("RK Bericht", "HYBRID", KaeltemaschineAnlageCtrl.Laden(anlage).IdKaeltemaschine),
                         (z.Bezeichner, z.Bauart, z.IdKaeltemaschine));
        }
    }
}
