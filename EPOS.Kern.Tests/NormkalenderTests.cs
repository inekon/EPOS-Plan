using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Normkalender der Kältevorlage 8</b> (Konzept Kältebedarfsprofile 6.2, 8; Entscheid des Anwenders vom
    /// 16.10.2026; Welle KBP-5b): genau die Normtage als Nutzungstage im Gemeinjahr-Raster für jedes Feiertagsland und
    /// jeden Wochentag des 1. Januar, die Jahressumme bleibt erhalten, ein Kalender gleichen Namens wird wiederverwendet,
    /// Vorlagen ohne Normtage bekommen keine Vorwahl.
    /// </summary>
    [Collection("Testdatenbank")]
    public class NormkalenderTests : IDisposable
    {
        private const int PROJEKT = 1017;
        private const string VORLAGE_8 = "Prozesskälte einschichtig";
        private const string BUERO = "Raumkühlung Büro";
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly ITestOutputHelper _aus;

        public NormkalenderTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _db.Dispose();

        /// <summary>Das Wochenprofil der Vorlage 8: Grundlast 0,1 rund um die Uhr, Montag bis Freitag 7–16 Uhr 1,0.</summary>
        private static double[] Woche8()
        {
            var w = new double[168];
            for (int h = 0; h < 168; h++) w[h] = (h / 24 < 5 && h % 24 >= 7 && h % 24 < 16) ? 1.0 : 0.1;
            return w;
        }

        [Fact]
        public void Jedes_Land_und_jeder_Wochentag_ergibt_genau_230_Nutzungstage()
        {
            var laender = new List<string> { null };
            laender.AddRange(Landesfeiertage.BUNDESLAENDER);
            for (int w0 = 0; w0 < 7; w0++)
                foreach (string land in laender)
                {
                    var raster = new Gemeinjahrkalender(w0);
                    Betriebskalender k = NormkalenderCtrl.Bilden(230, land, raster, Woche8());
                    Assert.Equal(230, NormkalenderCtrl.Nutzungstage(k, raster));
                    Assert.Null(k.Pruefen());
                    Assert.InRange(k.Ferien.Count, 1, BedarfNetzKalenderSchema.FERIEN_ANZAHL);
                    Assert.Equal(NormkalenderCtrl.WINTER_BEGINN, k.Ferien[0].Von);
                    Assert.True(k.FeiertagWieSonntag);
                    Assert.False(k.FerienKuerzen);
                    Assert.Equal(land, k.Bundesland);
                }
        }

        [Fact]
        public void Ferienfaktor_traegt_die_Tagesmenge_des_Sonntags()
        {
            // Sonntag 24 · 0,1 = 2,4; Tagesmittel 15 · 0,1 + 9 · 5,2/7 = 8,1857 → 0,29.
            Assert.Equal(0.29, NormkalenderCtrl.Ferienfaktor(Woche8()), 12);
            Assert.Equal(0.0, NormkalenderCtrl.Ferienfaktor(null));
            Assert.Equal(0.0, NormkalenderCtrl.Ferienfaktor(new double[168]));
        }

        [Fact]
        public void In_der_Testdatenbank_ergeben_zwei_Laender_230_Tage_und_die_Jahressumme_bleibt()
        {
            if (!_db.Vorhanden) return;
            Gemeinjahrkalender raster = Konditionierungdatenweg.Raster(PROJEKT);
            int kopie = KaeltebedarfStammCtrl.CopyFromStamm(VORLAGE_8, PROJEKT);
            Assert.True(kopie > 0);
            Assert.Equal(230, KaeltebedarfStammCtrl.ProjektNutzungstage(kopie));
            double[] woche = KaeltebedarfStammCtrl.ProjektWoche(kopie);
            Assert.NotNull(woche);

            // Monatsgrenzen des Gemeinjahrs in Stunden.
            int[] tage = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };
            int[] anfang = new int[12], ende = new int[12];
            for (int m = 0, h = 0; m < 12; h += tage[m] * 24, m++) { anfang[m] = h; ende[m] = h + tage[m] * 24 - 1; }
            double[] monate = { 3, 3, 4, 5, 6, 8, 9, 9, 7, 5, 4, 3 };

            foreach (string land in new[] { "BY", "NW", null })
            {
                Betriebskalender k = NormkalenderCtrl.Bilden(230, land, raster, woche);
                Assert.Equal(230, NormkalenderCtrl.Nutzungstage(k, raster));
                var jahr = new double[8760];
                Assert.True(Betriebskalenderschicht.WocheZuJahr(woche, monate, jahr, anfang, ende, raster.W0,
                                                                k.Tagesarten(raster), k.Ferienfaktor, k.FerienKuerzen));
                Assert.Equal(monate.Sum() * 1000.0, jahr.Sum(), 6);
                int feiertageWerktag = Landesfeiertage.Jahrestage(land, raster).Count(t => (raster.W0 + t - 1) % 7 < 5);
                _aus.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "w0={0} Land={1} Feiertage Mo-Fr={2} f={3} Ferien={4}", raster.W0, land ?? "(bund)", feiertageWerktag,
                    k.Ferienfaktor, string.Join("; ", k.Ferien.Select(f => f.Von + "-" + f.Bis))));
            }
        }

        [Fact]
        public void Uebernahme_der_Vorlage_8_waehlt_den_Normkalender_vor_und_nimmt_ihn_wieder()
        {
            if (!_db.Vorhanden) return;
            int vorher = BetriebskalenderCtrl.Liste().Count;
            int kopie = KaeltebedarfStammCtrl.CopyFromStamm(VORLAGE_8, PROJEKT);
            var zeile = new EPOS.UI.Dialoge.Bedarf.BedarfsProfilZeile { IdStamm = kopie, Name = VORLAGE_8 };
            KaeltebedarfsprofilAbbildung.Aufgenommen(zeile, VORLAGE_8, PROJEKT);
            Assert.Equal(230, zeile.Normtage);
            Assert.True(zeile.KalenderId is int id && id > 0);

            Betriebskalender k = BetriebskalenderCtrl.Lies(zeile.KalenderId!.Value);
            Assert.Equal(NormkalenderCtrl.Name(230), k.Bezeichner);
            Assert.Equal(230, NormkalenderCtrl.Nutzungstage(k, Konditionierungdatenweg.Raster(PROJEKT)));
            Assert.Equal(NormkalenderCtrl.Feiertagsland(PROJEKT), k.Bundesland);

            // Ein zweites Übernehmen legt keinen zweiten Kalender an.
            var zweite = new EPOS.UI.Dialoge.Bedarf.BedarfsProfilZeile { IdStamm = kopie, Name = VORLAGE_8 };
            KaeltebedarfsprofilAbbildung.Aufgenommen(zweite, VORLAGE_8, PROJEKT);
            Assert.Equal(zeile.KalenderId, zweite.KalenderId);
            Assert.Equal(vorher + 1, BetriebskalenderCtrl.Liste().Count);

            // Eine Vorlage ohne Normtage bekommt keine Vorwahl; im Assistenten (ohne Projekt) bleibt die Wahl leer.
            var buero = new EPOS.UI.Dialoge.Bedarf.BedarfsProfilZeile
            { IdStamm = KaeltebedarfStammCtrl.CopyFromStamm(BUERO, PROJEKT), Name = BUERO };
            KaeltebedarfsprofilAbbildung.Aufgenommen(buero, BUERO, PROJEKT);
            Assert.Null(buero.Normtage);
            Assert.Null(buero.KalenderId);
            var assistent = new EPOS.UI.Dialoge.Bedarf.BedarfsProfilZeile { IdStamm = 8, Name = VORLAGE_8 };
            KaeltebedarfsprofilAbbildung.Aufgenommen(assistent, VORLAGE_8);
            Assert.Equal(230, assistent.Normtage);
            Assert.Null(assistent.KalenderId);
        }

        [Fact]
        public void Ein_Kalender_gleichen_Namens_des_Projekts_wird_wiederverwendet()
        {
            if (!_db.Vorhanden) return;
            // Ein Kalender dieses Namens mit ANDEREM Inhalt, den eine Zuordnung des Projekts schon nutzt.
            int eigen = BetriebskalenderCtrl.Speichern(new Betriebskalender { Bezeichner = NormkalenderCtrl.Name(230), Ferienfaktor = 0.5 });
            Assert.True(eigen > 0);
            int zuordnung = Convert.ToInt32(DataRepository.ExecuteScalar(
                "SELECT ID FROM Z_Projekt_Stromverbraucher WHERE ID_Projekt = ? ORDER BY ID LIMIT 1", new DbParam("?", PROJEKT))
                ?? 0, CultureInfo.InvariantCulture);
            Assert.True(zuordnung > 0, "Projekt 1017 traegt keine Stromverbraucher-Zuordnung.");
            Assert.True(BetriebskalenderCtrl.ZuordnungKalenderSetzen("Z_Projekt_Stromverbraucher", zuordnung, eigen));

            int kopie = KaeltebedarfStammCtrl.CopyFromStamm(VORLAGE_8, PROJEKT);
            Assert.Equal(eigen, NormkalenderCtrl.Vorwahl(PROJEKT, kopie));
            Assert.Single(BetriebskalenderCtrl.Liste(), k => k.Bezeichner == NormkalenderCtrl.Name(230));
        }
    }
}
