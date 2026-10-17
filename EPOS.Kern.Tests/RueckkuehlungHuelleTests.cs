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

        // =================================================================
        //  K-F5c: Abbrechen nach einer Zwischenspeicherung stellt den Stand beim Öffnen exakt her
        // =================================================================

        private static long Waisen() => Convert.ToInt64(DataRepository.ExecuteScalar(
            "SELECT COUNT(*) FROM Tab_Rueckkuehlwerk r WHERE NOT EXISTS " +
            "(SELECT 1 FROM Tab_Energieanlagen a WHERE a.ID_Rueckkuehlwerk = r.ID)"), CultureInfo.InvariantCulture);

        private static int Satz(string name) => RueckkuehlwerkStammCtrl.Speichern(new RueckkuehlwerkModel
        {
            Bezeichner = name, Bauart = "TROCKEN", Nennleistung_kW = 100
        }).Id;

        /// <summary>Zwischenspeichern mit <paramref name="wahl"/>, dann Abbrechen (der Dialog schreibt den Stand beim Öffnen).</summary>
        private static void ZwischenspeichernUndAbbrechen(int anlage, int? wahl, Func<KaeltemaschineAnlageDaten, string> speichern)
        {
            KaeltemaschineAnlageDaten original = Lies(anlage);
            KaeltemaschineAnlageDaten arbeit = Lies(anlage);
            arbeit.RueckkuehlwerkWahl = wahl;
            Assert.Null(speichern(arbeit));
            Assert.Null(speichern(original));
        }

        [Fact]
        public void Abbrechen_stellt_die_Projektkopie_mit_ihren_eigenen_Werten_wieder_her()
        {
            if (!_db.Vorhanden) return;
            int s = Satz("RK Start"), t = Satz("RK Wechsel");
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, StammKm(TROCKENKUEHLER), "KM Abbrechen eigen");
            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, s));
            int kopie = KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk.Value;
            DataRepository.ExecuteNonQuery("UPDATE Tab_Rueckkuehlwerk SET Nennleistung_kW = 123.5, Beschreibung = 'eigen', " +
                                           "Annaeherung_Nenn_K = 4.5 WHERE ID = ?", new DbParam("?", kopie));

            var sitzung = new KaeltemaschineAnlageHuelle.Rueckkuehlwerksitzung();
            KaeltemaschineAnlageDaten original = Lies(anlage);
            KaeltemaschineAnlageDaten arbeit = Lies(anlage);
            arbeit.RueckkuehlwerkWahl = t;
            Assert.Null(KaeltemaschineAnlageHuelle.Speichern(arbeit, sitzung));
            int wechsel = KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk.Value;
            Assert.NotEqual(kopie, wechsel);
            Assert.Null(RueckkuehlwerkCtrl.Laden(kopie));

            Assert.Null(KaeltemaschineAnlageHuelle.Speichern(original, sitzung));
            Assert.Equal(kopie, KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk);
            RueckkuehlwerkModel m = RueckkuehlwerkCtrl.Laden(kopie);
            Assert.Equal((123.5, "eigen", 4.5, (int?)s, (int?)PROJEKT, "RK Start"),
                         (m.Nennleistung_kW.Value, m.Beschreibung, m.Annaeherung_Nenn_K.Value, m.IdStamm, m.IdProjekt, m.Bezeichner));
            Assert.Null(RueckkuehlwerkCtrl.Laden(wechsel));
            Assert.Equal(0L, Waisen());
            Assert.Equal(s, Lies(anlage).RueckkuehlwerkWahl);
            KaeltemaschineAnlageCtrl.Loeschen(anlage);
            Assert.Equal(0L, Waisen());
        }

        [Fact]
        public void Abbrechen_stellt_eine_Projektkopie_ohne_Katalogsatz_wieder_her()
        {
            if (!_db.Vorhanden) return;
            int s = Satz("RK ohne Satz"), t = Satz("RK Wechsel 2");
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, StammKm(TROCKENKUEHLER), "KM Abbrechen ohne Satz");
            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, s));
            int kopie = KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk.Value;
            DataRepository.ExecuteNonQuery("UPDATE Tab_Rueckkuehlwerk SET ID_Stamm = NULL, Nennleistung_kW = 77 WHERE ID = ?",
                                           new DbParam("?", kopie));
            Assert.Equal(KaeltemaschineAnlageDaten.RUECKKUEHLWERK_PROJEKTKOPIE, Lies(anlage).RueckkuehlwerkWahl);

            // Wechsel auf einen Katalogsatz, dann auf „keins“ - jedes Mal abgebrochen.
            foreach (int? wahl in new int?[] { t, null })
            {
                var sitzung = new KaeltemaschineAnlageHuelle.Rueckkuehlwerksitzung();
                ZwischenspeichernUndAbbrechen(anlage, wahl, d => KaeltemaschineAnlageHuelle.Speichern(d, sitzung));
                Assert.Equal(kopie, KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk);
                RueckkuehlwerkModel m = RueckkuehlwerkCtrl.Laden(kopie);
                Assert.Null(m.IdStamm);
                Assert.Equal(77.0, m.Nennleistung_kW);
                Assert.Equal(0L, Waisen());
            }
            KaeltemaschineAnlageCtrl.Loeschen(anlage);
        }

        [Fact]
        public void Abbrechen_stellt_keins_wieder_her_und_laesst_keine_Waise()
        {
            if (!_db.Vorhanden) return;
            int s = Satz("RK Neu");
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, StammKm(TROCKENKUEHLER), "KM Abbrechen keins");
            Assert.Null(Lies(anlage).RueckkuehlwerkWahl);

            var sitzung = new KaeltemaschineAnlageHuelle.Rueckkuehlwerksitzung();
            ZwischenspeichernUndAbbrechen(anlage, s, d => KaeltemaschineAnlageHuelle.Speichern(d, sitzung));
            Assert.Null(KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk);
            Assert.Equal(0L, Convert.ToInt64(DataRepository.ExecuteScalar("SELECT COUNT(*) FROM Tab_Rueckkuehlwerk"), CultureInfo.InvariantCulture));
            KaeltemaschineAnlageCtrl.Loeschen(anlage);
        }

        [Fact]
        public void Der_Speicherweg_der_Gaben_fuehrt_die_Sitzung_je_Dialog()
        {
            if (!_db.Vorhanden) return;
            int s = Satz("RK Gaben"), t = Satz("RK Gaben 2");
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, StammKm(TROCKENKUEHLER), "KM Gaben");
            Assert.Null(KaeltemaschineAnlageCtrl.RueckkuehlwerkWaehlen(anlage, s));
            int kopie = KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk.Value;
            DataRepository.ExecuteNonQuery("UPDATE Tab_Rueckkuehlwerk SET Beschreibung = 'Gaben' WHERE ID = ?", new DbParam("?", kopie));

            var speichern = (Func<KaeltemaschineAnlageDaten, string>)KaeltemaschineAnlageHuelle.Gaben(PROJEKT)["Speichern"];
            ZwischenspeichernUndAbbrechen(anlage, t, speichern);
            Assert.Equal(kopie, KaeltemaschineAnlageCtrl.Laden(anlage).IdRueckkuehlwerk);
            Assert.Equal("Gaben", RueckkuehlwerkCtrl.Laden(kopie).Beschreibung);
            Assert.Equal(0L, Waisen());

            // Ohne Sitzung (Konfiguration im Bereich Kälte, einmaliges Schreiben) bleibt der Weg über den Katalogsatz.
            KaeltemaschineAnlageDaten d = Lies(anlage);
            d.RueckkuehlwerkWahl = t;
            Assert.Null(KaeltemaschineAnlageHuelle.Speichern(d));
            Assert.Equal(t, Lies(anlage).RueckkuehlwerkWahl);
            Assert.Null(RueckkuehlwerkCtrl.Laden(kopie));
            KaeltemaschineAnlageCtrl.Loeschen(anlage);
            Assert.Equal(0L, Waisen());
        }

        [Fact]
        public void Eine_leere_Rueckkuehlart_gilt_in_Huelle_und_Pruefung_als_luftgekuehlt()
        {
            if (!_db.Vorhanden) return;
            int anlage = KaeltemaschineAnlageCtrl.Anlegen(PROJEKT, StammKm(TROCKENKUEHLER), "KM leer Hülle");
            int geraet = KaeltemaschineAnlageCtrl.Laden(anlage).IdKaeltemaschine.Value;
            DataRepository.ExecuteNonQuery("UPDATE Tab_Kaeltemaschine SET Rueckkuehlart = NULL WHERE ID = ?", new DbParam("?", geraet));

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
