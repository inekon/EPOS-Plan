using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>„Tagesgang…" der Katalogpflege</b> (Umsetzungskonzept Zapfprofilgenerator 3.2, 3.3,
    /// 5.4): <see cref="TwwNutzungsartCtrl.TagesgangSpeichern"/> schreibt Tagesgänge und
    /// Wochenfaktoren in EINEM Vorgang; ein benutzter oder ausgelieferter Satz nur als neue
    /// Zeile, eine gesperrte Nutzungsart per „Speichern unter". Alle Werte erfunden, jede Probe
    /// in einer eigenen leeren Datei (<see cref="TwwTestdatenbank"/>).
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class TwwTagesgangSpeichernTests
    {
        /// <summary>Die Wochenfaktoren von <see cref="TwwTestdatenbank.NutzungsartAnlegen"/>.</summary>
        private static readonly double[] WocheAlt = { 0.2, 0.2, 0.2, 0.2, 0.2, 0.0, 0.0 };

        private static readonly double[] WocheNeu = { 0.1, 0.1, 0.2, 0.2, 0.2, 0.1, 0.1 };

        /// <summary>Ein Tagesgang mit dem ganzen Tag auf <paramref name="stunde"/> (1 … 24).</summary>
        private static double[] Spitze(int stunde)
            => Enumerable.Range(1, 24).Select(h => h == stunde ? 1.0 : 0.0).ToArray();

        /// <summary>Die vier Tagesgänge von <see cref="TwwTestdatenbank.TagesgangsatzAnlegen"/> (je ½ auf 7 und 19 Uhr).</summary>
        private static List<double[]> GaengeAlt()
            => Enumerable.Range(0, 4)
                         .Select(_ => Enumerable.Range(1, 24).Select(h => h == 7 || h == 19 ? 0.5 : 0.0).ToArray())
                         .ToList();

        private static List<double[]> GaengeNeu()
        {
            List<double[]> g = GaengeAlt();
            g[1] = Spitze(10);                                 // nur der Samstag geändert
            return g;
        }

        private static Tagesgangsatz Satz(int id) => ZapfprofilCtrl.Tagesgangsaetze().Single(s => s.Id == id);

        private static long Zahl(string sql) => System.Convert.ToInt64(DataRepository.ExecuteScalar(sql));

        private static readonly Provenienz Eigen =
            new Provenienz(TwwNutzungsartCtrl.QUELLE_EIGENKONSTRUKTION, null, "T1", Herkunftsart.Eigenkonstruktion);

        // =================================================================================

        [Fact]
        public void Eine_freie_Nutzungsart_mit_eigenem_Satz_schreibt_an_Ort_und_Stelle()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int id = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            DataRepository.ExecuteNonQuery("UPDATE \"Tab_TwwNutzungsart_STAMM\" SET \"Freigabe\" = 'geprueft', \"Beleg\" = 'intern'");

            Assert.False(TwwNutzungsartCtrl.TagesgangsatzIstReadOnly(satz));
            Assert.True(TwwNutzungsartCtrl.TagesgangsatzIstBenutzt(satz));    // von der eigenen Nutzungsart

            TwwTagesgangErgebnis erg = TwwNutzungsartCtrl.TagesgangSpeichern(id, GaengeNeu(), WocheNeu);
            Assert.True(erg.Ok);
            Assert.Equal((id, satz), (erg.IdNutzungsart, erg.IdTagesgangsatz));

            Tagesgangsatz s = Satz(satz);
            Assert.Equal(1.0, s.Anteile[1, 9]);                                  // Samstag, 10 Uhr
            Assert.Equal(0.5, s.Anteile[0, 6]);                                  // Werktag unverändert
            Assert.Equal(Eigen, s.JeTagtyp[1]);
            Assert.Equal(Herkunftsart.Fiktiv, s.JeTagtyp[0].Art);
            Assert.Equal(4, Zahl("SELECT COUNT(*) FROM \"Tab_TwwTagesgang_STAMM\""));

            Nutzungsart n = TwwNutzungsartCtrl.Lies(id);
            Assert.Equal(WocheNeu, n.Wochenfaktoren);
            Assert.Equal(Eigen, n.Herkunft.Wochengang);
            Assert.Equal(Herkunftsart.Fiktiv, n.Herkunft.Bedarf.Art);
            Assert.Null(n.Freigabe);
            Assert.Null(DataRepository.ExecuteScalar("SELECT \"Beleg\" FROM \"Tab_TwwNutzungsart_STAMM\""));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"Tab_TwwNutzungsart_STAMM\""));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"Tab_TwwTagesgangsatz_STAMM\""));
        }

        [Fact]
        public void Ein_Satz_zweier_Nutzungsarten_entsteht_neu_und_nur_die_eine_haengt_um()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int a = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            int b = TwwTestdatenbank.NutzungsartAnlegen("Nutzung B", "T1", satz);
            TwwTestdatenbank.NutzungsartAnlegen("Nutzung C", "T1", satz);

            // Ohne Katalogversion für die neue Zeile: benannt abgelehnt, nichts geschrieben.
            Assert.Equal(TwwKatalogAusgang.EntwurfUnvollstaendig,
                         TwwNutzungsartCtrl.TagesgangSpeichern(a, GaengeNeu(), WocheAlt).Ausgang);
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"Tab_TwwTagesgangsatz_STAMM\""));

            TwwTagesgangErgebnis erg = TwwNutzungsartCtrl.TagesgangSpeichern(a, GaengeNeu(), WocheAlt, "T2");
            Assert.True(erg.Ok);
            Assert.Equal(a, erg.IdNutzungsart);
            Assert.NotEqual(satz, erg.IdTagesgangsatz);

            Tagesgangsatz neu = Satz(erg.IdTagesgangsatz);
            Assert.Equal(("Satz A", "T2", ZapfKatalogstatus.Eigen, false), (neu.Bezeichner, neu.Katalogversion, neu.Status, neu.ReadOnly));
            Assert.True(neu.Vollstaendig);
            Assert.Equal(1.0, neu.Anteile[1, 9]);
            Assert.Equal(Eigen with { Version = "T2" }, neu.JeTagtyp[1]);
            Assert.Equal(Satz(satz).JeTagtyp[0], neu.JeTagtyp[0]);               // unveränderter Tagtyp: Provenienz mit

            Assert.Equal(erg.IdTagesgangsatz, TwwNutzungsartCtrl.Lies(a).Tagesgaenge.Id);
            Assert.Equal(satz, TwwNutzungsartCtrl.Lies(b).Tagesgaenge.Id);
            Assert.Equal(0.5, Satz(satz).Anteile[1, 6]);                         // der alte Satz bleibt
            Assert.Equal(WocheAlt, TwwNutzungsartCtrl.Lies(a).Wochenfaktoren);
            Assert.Equal(Herkunftsart.Fiktiv, TwwNutzungsartCtrl.Lies(a).Herkunft.Wochengang.Art);

            // B teilt den alten Satz weiter mit C; derselbe natürliche Schlüssel ein zweites Mal:
            // benannt belegt, nichts geschrieben.
            Assert.Equal(TwwKatalogAusgang.NameBelegt,
                         TwwNutzungsartCtrl.TagesgangSpeichern(b, GaengeNeu(), WocheAlt, "T2").Ausgang);
            Assert.Equal(2, Zahl("SELECT COUNT(*) FROM \"Tab_TwwTagesgangsatz_STAMM\""));
            Assert.Equal(8, Zahl("SELECT COUNT(*) FROM \"Tab_TwwTagesgang_STAMM\""));
        }

        [Fact]
        public void Ein_ausgelieferter_Satz_oder_ein_Satz_einer_Zone_entsteht_neu()
        {
            using var db = new TwwTestdatenbank();
            int ausgeliefert = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1", readOnly: true);
            int a = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", ausgeliefert);
            Assert.True(TwwNutzungsartCtrl.TagesgangsatzIstReadOnly(ausgeliefert));

            TwwTagesgangErgebnis e1 = TwwNutzungsartCtrl.TagesgangSpeichern(a, GaengeNeu(), WocheAlt, "T2");
            Assert.True(e1.Ok);
            Assert.NotEqual(ausgeliefert, e1.IdTagesgangsatz);
            Assert.Equal(0.5, Satz(ausgeliefert).Anteile[1, 6]);

            // Ein Satz, den eine Zone als Expertenwahl trägt, ist ebenso gesperrt.
            int zonensatz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz Z", "T1");
            int b = TwwTestdatenbank.NutzungsartAnlegen("Nutzung B", "T1", zonensatz);
            int andere = TwwTestdatenbank.NutzungsartAnlegen("Nutzung C", "T1", ausgeliefert);
            int zone = TwwTestdatenbank.ZoneAnlegen(1, andere, "Zone", 10.0);
            DataRepository.ExecuteNonQuery("UPDATE \"Tab_TwwZone\" SET \"ID_Tagesgangsatz\" = ? WHERE \"ID\" = ?",
                                           new DbParam("@s", zonensatz), new DbParam("@z", zone));

            TwwTagesgangErgebnis e2 = TwwNutzungsartCtrl.TagesgangSpeichern(b, GaengeNeu(), WocheAlt, "T2");
            Assert.True(e2.Ok);
            Assert.Equal(b, e2.IdNutzungsart);
            Assert.NotEqual(zonensatz, e2.IdTagesgangsatz);
            Assert.Equal(0.5, Satz(zonensatz).Anteile[1, 6]);
        }

        [Fact]
        public void Eine_benutzte_Nutzungsart_entsteht_per_Speichern_unter_neu()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int alt = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz, beleg: "intern");
            int zone = TwwTestdatenbank.ZoneAnlegen(1, alt, "Zone", 10.0);

            TwwTagesgangErgebnis erg = TwwNutzungsartCtrl.TagesgangSpeichern(alt, GaengeNeu(), WocheNeu, "T2");
            Assert.True(erg.Ok);
            Assert.NotEqual(alt, erg.IdNutzungsart);
            Assert.NotEqual(satz, erg.IdTagesgangsatz);

            Nutzungsart neu = TwwNutzungsartCtrl.Lies(erg.IdNutzungsart);
            Assert.Equal(("Nutzung A", "T2", alt), (neu.Name, neu.Katalogversion, neu.IdVorlage));
            Assert.Equal(ZapfKatalogstatus.Eigen, neu.Status);
            Assert.Equal(WocheNeu, neu.Wochenfaktoren);
            Assert.Equal(Eigen with { Version = "T2" }, neu.Herkunft.Wochengang);
            Assert.Equal(TwwNutzungsartCtrl.Lies(alt).Herkunft.Bedarf, neu.Herkunft.Bedarf);
            Assert.Equal(erg.IdTagesgangsatz, neu.Tagesgaenge.Id);
            Assert.Null(DataRepository.ExecuteScalar("SELECT \"Beleg\" FROM \"Tab_TwwNutzungsart_STAMM\" WHERE \"ID\" = ?",
                                                     new DbParam("@id", erg.IdNutzungsart)));

            // Die alte Zeile, ihr Satz und die Zone bleiben, wie sie waren.
            Nutzungsart danach = TwwNutzungsartCtrl.Lies(alt);
            Assert.Equal(WocheAlt, danach.Wochenfaktoren);
            Assert.Equal(satz, danach.Tagesgaenge.Id);
            Assert.Equal(0.5, Satz(satz).Anteile[1, 6]);
            Assert.Equal(alt, ZapfprofilCtrl.Lies(1).Zonen.Single(z => z.Id == zone).IdNutzungsart);

            // Nur die Wochenfaktoren: neue Nutzungsart, der (unveränderte) Satz bleibt geteilt.
            TwwTagesgangErgebnis nurWoche = TwwNutzungsartCtrl.TagesgangSpeichern(alt, GaengeAlt(), WocheNeu, "T3");
            Assert.True(nurWoche.Ok);
            Assert.Equal(satz, nurWoche.IdTagesgangsatz);
            Assert.Equal(satz, TwwNutzungsartCtrl.Lies(nurWoche.IdNutzungsart).Tagesgaenge.Id);
        }

        [Fact]
        public void Ohne_Aenderung_wird_nichts_geschrieben()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int id = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);
            TwwTestdatenbank.ZoneAnlegen(1, id, "Zone", 10.0);

            TwwTagesgangErgebnis erg = TwwNutzungsartCtrl.TagesgangSpeichern(id, GaengeAlt(), WocheAlt);
            Assert.True(erg.Ok);
            Assert.Equal((id, satz), (erg.IdNutzungsart, erg.IdTagesgangsatz));
            Assert.Equal(1, Zahl("SELECT COUNT(*) FROM \"Tab_TwwNutzungsart_STAMM\""));
            Assert.Equal(Herkunftsart.Fiktiv, Satz(satz).JeTagtyp[1].Art);
        }

        [Fact]
        public void Die_Raster_muessen_vollstaendig_nicht_negativ_und_Summe_eins_sein()
        {
            using var db = new TwwTestdatenbank();
            int satz = TwwTestdatenbank.TagesgangsatzAnlegen("Satz A", "T1");
            int id = TwwTestdatenbank.NutzungsartAnlegen("Nutzung A", "T1", satz);

            List<double[]> halb = GaengeNeu();
            halb[2] = Enumerable.Repeat(0.5 / 24, 24).ToArray();                   // Σ ½
            List<double[]> negativ = GaengeNeu();
            negativ[0] = Enumerable.Range(1, 24).Select(h => h == 1 ? -1.0 : h == 2 ? 2.0 : 0.0).ToArray();
            List<double[]> kurz = GaengeNeu();
            kurz[3] = new double[23];

            Assert.Equal(TwwKatalogAusgang.RasterUngueltig, TwwNutzungsartCtrl.TagesgangSpeichern(id, halb, WocheAlt).Ausgang);
            Assert.Equal(TwwKatalogAusgang.RasterUngueltig, TwwNutzungsartCtrl.TagesgangSpeichern(id, negativ, WocheAlt).Ausgang);
            Assert.Equal(TwwKatalogAusgang.RasterUngueltig, TwwNutzungsartCtrl.TagesgangSpeichern(id, kurz, WocheAlt).Ausgang);
            Assert.Equal(TwwKatalogAusgang.RasterUngueltig, TwwNutzungsartCtrl.TagesgangSpeichern(id, GaengeNeu().Take(3).ToList(), WocheAlt).Ausgang);
            Assert.Equal(TwwKatalogAusgang.RasterUngueltig, TwwNutzungsartCtrl.TagesgangSpeichern(id, null, WocheAlt).Ausgang);
            Assert.Equal(TwwKatalogAusgang.RasterUngueltig,
                         TwwNutzungsartCtrl.TagesgangSpeichern(id, GaengeNeu(), new[] { 0.2, 0.2, 0.2, 0.2, 0.2, 0.2, 0.0 }).Ausgang);
            Assert.Equal(TwwKatalogAusgang.RasterUngueltig,
                         TwwNutzungsartCtrl.TagesgangSpeichern(id, GaengeNeu(), new[] { 0.5, 0.5 }).Ausgang);

            Assert.Equal(0.5, Satz(satz).Anteile[1, 6]);
            Assert.Equal(WocheAlt, TwwNutzungsartCtrl.Lies(id).Wochenfaktoren);
        }

        [Fact]
        public void Fehlende_Nutzungsart_und_fehlende_Tabellen_sind_benannt()
        {
            using (var db = new TwwTestdatenbank())
            {
                Assert.Equal(TwwKatalogAusgang.NichtGefunden,
                             TwwNutzungsartCtrl.TagesgangSpeichern(9999, GaengeNeu(), WocheAlt, "T2").Ausgang);
            }
            using (var ohne = new TwwTestdatenbank(mitTwwSchema: false))
            {
                Assert.Equal(TwwKatalogAusgang.TabellenFehlen,
                             TwwNutzungsartCtrl.TagesgangSpeichern(1, GaengeNeu(), WocheAlt, "T2").Ausgang);
                Assert.False(TwwNutzungsartCtrl.TagesgangsatzIstBenutzt(1));
                Assert.False(TwwNutzungsartCtrl.TagesgangsatzIstReadOnly(1));
            }
        }
    }
}
