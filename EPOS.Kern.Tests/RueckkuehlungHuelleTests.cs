using System;
using System.Globalization;
using System.Linq;
using EPOS.UI.Dialoge.Erzeuger;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Gruppe „Rückkühlung“ über die Hülle</b> (K-F5b, Entwurf Split/VRF/Rückkühlwerk 5.2–5.8, 8): Die Hülle liest
    /// Wahl und Lesezeilen der Projektkopie, baut die Katalogsätze samt „Vorgabe: …“ und die Luftfeuchte des Projektklimas,
    /// und schreibt die Wahl im OK-Weg über <see cref="KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen"/> — nur, wenn sie
    /// abweicht. Gegen die Testdatenbank.
    /// </summary>
    [Collection("Testdatenbank")]
    public class RueckkuehlungHuelleTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1017;
        private const string TROCKENKUEHLER = "Kältemaschine 200 kW wassergekühlt mit Trockenkühler";
        private const string LUFTGEKUEHLT = "Kältemaschine 50 kW luftgekühlt";

        private static int StammKm(string bezeichner) => Convert.ToInt32(DataRepository.ExecuteScalar(
            "SELECT ID FROM Tab_Kaeltemaschine_STAMM WHERE Bezeichner = ?", new DbParam("?", bezeichner)), CultureInfo.InvariantCulture);

        private static KaeltemaschineAnlageDaten Lies(int anlage)
            => KaeltemaschineAnlageHuelle.Liste(PROJEKT).Single(d => d.AnlagenId == anlage);

        [Fact]
        public void Die_Wahl_schreibt_erst_der_OK_Weg_und_nur_wenn_sie_abweicht()
        {
            if (!_db.Vorhanden) return;
            int stamm = RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel { Bezeichner = "RK Hülle", Bauart = "KUEHLTURM_OFFEN" }).Id;
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, StammKm(TROCKENKUEHLER), "KM Hülle");

            KaeltemaschineAnlageDaten d = Lies(anlage);
            Assert.False(d.Geraet.Luftgekuehlt);
            Assert.Null(d.RueckkuehlwerkWahl);
            Assert.Null(d.Rueckkuehlwerk);

            d.RueckkuehlwerkWahl = stamm;
            Assert.True(string.IsNullOrEmpty(KaeltemaschineAnlageHuelle.Pruefen(d)));
            Assert.Null(KaeltemaschineAnlageHuelle.Speichern(d));
            int kopie = KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk.Value;

            KaeltemaschineAnlageDaten neu = Lies(anlage);
            Assert.Equal(stamm, neu.RueckkuehlwerkWahl);
            Assert.Equal(("RK Hülle", (int?)stamm, true), (neu.Rueckkuehlwerk.Bezeichner, neu.Rueckkuehlwerk.StammId,
                                                         neu.Rueckkuehlwerk.Verdunstungskuehlanlage));

            // Ein zweites OK mit derselben Wahl wiederholt nichts: die Projektkopie bleibt dieselbe.
            Assert.Null(KaeltemaschineAnlageHuelle.Speichern(neu));
            Assert.Equal(kopie, KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk);

            // „keins“ löst die Kopie.
            neu.RueckkuehlwerkWahl = null;
            Assert.Null(KaeltemaschineAnlageHuelle.Speichern(neu));
            Assert.Null(KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk);
            Assert.Null(RueckkuehlwerkCtrl.Laden(kopie));
            KaeltemaschineAnlageCtrl.Loeschen(anlage);
        }

        [Fact]
        public void Eine_Projektkopie_ohne_Katalogsatz_bleibt_stehen()
        {
            if (!_db.Vorhanden) return;
            int stamm = RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel { Bezeichner = "RK Waise" }).Id;
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, StammKm(TROCKENKUEHLER), "KM Waise");
            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, stamm));
            int kopie = KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk.Value;
            DataRepository.ExecuteNonQuery("UPDATE Tab_Rueckkuehlwerk SET ID_Stamm = NULL WHERE ID = ?", new DbParam("?", kopie));

            KaeltemaschineAnlageDaten d = Lies(anlage);
            Assert.Equal(KaeltemaschineAnlageDaten.RUECKKUEHLWERK_PROJEKTKOPIE, d.RueckkuehlwerkWahl);
            Assert.Null(KaeltemaschineAnlageHuelle.Speichern(d));
            Assert.Equal(kopie, KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk);
            KaeltemaschineAnlageCtrl.Loeschen(anlage);
        }

        [Fact]
        public void Eine_luftgekuehlte_Maschine_lehnt_die_Wahl_in_der_Pruefung_ab()
        {
            if (!_db.Vorhanden) return;
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, StammKm(LUFTGEKUEHLT), "KM Luft Hülle");
            KaeltemaschineAnlageDaten d = Lies(anlage);
            Assert.True(d.Geraet.Luftgekuehlt);
            d.RueckkuehlwerkWahl = 1;
            Assert.Equal(R.RKW_MSG_LUFT_UNVERTRAEGLICH, KaeltemaschineAnlageHuelle.Pruefen(d));
            KaeltemaschineAnlageCtrl.Loeschen(anlage);
        }

        [Fact]
        public void Die_Gaben_tragen_die_Katalogsaetze_mit_Vorgaben_und_die_Luftfeuchte_des_Klimas()
        {
            if (!_db.Vorhanden) return;
            int leer = RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel { Bezeichner = "RK leer" }).Id;
            int gepflegt = RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel
            {
                Bezeichner = "RK gepflegt", Bauart = "HYBRID", Nennleistung_kW = 150, Annaeherung_Nenn_K = 4,
                Annaeherung_Weg = "LASTABHAENGIG", Ventilator_Nenn_kW = 3.5, Ventilator_Regelung = "DREHZAHL", Freikuehlung_Schaltung = "REIHE"
            }).Id;

            RueckkuehlungGaben g = KaeltemaschineAnlageHuelle.Rueckkuehlung(PROJEKT);
            RueckkuehlwerkLesung l = g.Katalog.Single(e => e.Id == leer).Lesung;
            Assert.All(l.Zeilen.Where(z => z.Bezeichnung != R.RKW_LBL_NENNLEISTUNG && z.Bezeichnung != R.RKW_GRUPPE_VENTILATOR),
                       z => Assert.StartsWith(string.Format(CultureInfo.CurrentCulture, R.RKW_VORGABE, ""), z.Wert));
            Assert.Contains(l.Zeilen, z => z.Wert == R.RKW_VORGABE_VENTILATOR);
            Assert.False(l.Verdunstungskuehlanlage);
            Assert.Null(l.VentilatorKw);

            RueckkuehlwerkEintrag e = g.Katalog.Single(x => x.Id == gepflegt);
            Assert.Equal("RK gepflegt · " + R.RKW_BAUART_HYBRID, e.Text);
            Assert.True(e.Lesung.Verdunstungskuehlanlage);
            Assert.Equal(3.5, e.Lesung.VentilatorKw);
            Assert.Equal(R.RKW_SCHALTUNG_REIHE, e.Lesung.Zeilen.Single(z => z.Bezeichnung == R.RKW_GRUPPE_FREIKUEHLUNG).Wert);
            Assert.Equal("4 K · " + R.RKW_WEG_LASTABHAENGIG, e.Lesung.Zeilen.Single(z => z.Bezeichnung == R.RKW_GRUPPE_ANNAEHERUNG).Wert);

            // Die Testdatenbank führt keine Luftfeuchte; mit Feuchte in jeder Stunde der Region steht sie da.
            Assert.False(g.KlimaMitFeuchte);
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Solar SET Luftfeuchte = 60 WHERE ID_Klimaregion = (SELECT ID_Klimaregion FROM Tab_Projekt WHERE ID = ?)",
                new DbParam("?", PROJEKT));
            Assert.True(KaeltemaschineAnlageCtrl.KlimaMitFeuchte(PROJEKT));
            Assert.False(KaeltemaschineAnlageCtrl.KlimaMitFeuchte(0));
        }
    }
}
