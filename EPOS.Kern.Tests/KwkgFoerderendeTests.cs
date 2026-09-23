using System;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.MyResource;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// ETAPPE E7c — <b>A20, das Förderende 2030 als Ende der Frist zur Inbetriebnahme</b>
    /// (Entscheid E7‑Q3, Lesart b, 23.09.2026). Die feste Realisierungsfrist
    /// <c>KWKG_REALISIERUNG_JAHRE = 4</c> ist entfallen; an ihre Stelle tritt das
    /// Katalogdatum <c>KWKG_INBETRIEBNAHME_FRISTENDE</c> (31.12.2030). Eine Anlage, die
    /// danach in Betrieb geht, bekommt keinen Zuschlag; eine Anlage davor rechnet ihre
    /// Reihe bis zum Ende des Kontingents — eine Höchstdauer in Kalenderjahren gibt es
    /// nicht (Rechenweg 05: Jahr 12 = 2037).
    ///
    /// <para><b>Der Prüfstand</b> ist Projekt 1030: zwei BHKW-Anlagen mit eigenem Stichtag
    /// (01.09.2026) und eigener Inbetriebnahme (01.03.2027), Kontingent 30.000 h,
    /// gerechnet über den Ankerweg.</para>
    ///
    /// <para><b>Gemessen (E7c1, Scratchpad-Messung, Ankerweg, Jahr 1):</b> Stichtag
    /// 01.06.2025 / Inbetriebnahme 01.06.2030 vorher 0,00 € (Frist bis 31.12.2029), nachher
    /// 5.899,97 €; ohne Stichtag, Inbetriebnahme 01.03.2031 vorher 5.899,97 € (ungeprüft),
    /// nachher 0,00 €; Inbetriebnahme 31.12.2030 und 01.03.2031 mit Stichtag 2026
    /// unverändert (5.899,97 € bzw. 0,00 €).</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class KwkgFoerderendeTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1030;
        private const string ANLAGEN = "14920, 14921";

        // =================================================================
        //  Das Katalogdatum
        // =================================================================

        [Fact]
        public void Das_Fristende_ist_ein_Katalogdatum_mit_Schluessel_und_Herkunft()
        {
            GesetzParameter p = GesetzKatalog.Vorbelegung()
                .Single(x => x.Schluessel == DbWerte.GESETZ_KWKG_INBETRIEBNAHME_FRISTENDE);
            Assert.Equal(DbWerte.GESETZ_KLASSE_KWKG, p.Klasse);
            Assert.Equal(2030.0, p.Wert);
            Assert.Equal(DbWerte.GESETZ_EINHEIT_JAHR, p.Einheit);
            Assert.Equal(DbWerte.GESETZ_STATUS_GESICHERT, p.Status);
            Assert.Equal(8, p.Generation);
            Assert.Contains("§ 6", p.Quelle);

            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            GesetzKatalog.StelleKatalogSicher();
            Assert.Equal(2030.0, new GesetzKatalog().Wert(DbWerte.GESETZ_KWKG_INBETRIEBNAHME_FRISTENDE, 2027));
        }

        // =================================================================
        //  Die Prüfkette je Anlage
        // =================================================================

        /// <summary>
        /// Stichtag 01.06.2025, Inbetriebnahme 01.06.2030: Nach der entfallenen Frist (vier
        /// Jahre ab Stichtag, bis 31.12.2029) kein Zuschlag — nach dem Katalogdatum
        /// (bis 31.12.2030) sehr wohl. Und die Inbetriebnahme am letzten Tag der Frist
        /// zählt noch.
        /// </summary>
        [Fact]
        public void Inbetriebnahme_bis_zum_Fristende_bekommt_den_Zuschlag()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Anlagen("KWKG_Stichtag = '2025-06-01 00:00:00', KWKG_Inbetriebnahme = '2030-06-01 00:00:00'");
            WirtschaftlichkeitErgebnis e = Rechne();
            Assert.True(e.KwkgErloesJahr1 > 5000, "Jahr 1: " + e.KwkgErloesJahr1);
            Assert.Equal(5899.965028, e.KwkgErloesJahr1, 3);
            Assert.DoesNotContain("Ende der Frist zur Inbetriebnahme", e.Hinweis ?? "");

            Anlagen("KWKG_Stichtag = '2026-09-01 00:00:00', KWKG_Inbetriebnahme = '2030-12-31 00:00:00'");
            Assert.Equal(5899.965028, Rechne().KwkgErloesJahr1, 3);
        }

        /// <summary>
        /// Inbetriebnahme 01.03.2031 — nach dem Fristende: kein Zuschlag, und die Zeile
        /// nennt Anlage, Datum und Katalogherkunft. Das gilt auch OHNE Stichtag: Die
        /// entfallene Frist hing am Stichtag und prüfte ohne ihn gar nicht.
        /// </summary>
        [Fact]
        public void Inbetriebnahme_nach_dem_Fristende_bekommt_keinen_Zuschlag()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Anlagen("KWKG_Inbetriebnahme = '2031-03-01 00:00:00'");
            WirtschaftlichkeitErgebnis e = Rechne();
            Assert.Equal(0.0, e.KwkgErloesJahr1);
            Assert.Contains(string.Format(Resource.WIRT_KWKG_ANLAGE_FRIST, "EC-POWER XRGI 9 (9 kW)",
                                          "31.12.2030", Herkunft()), e.Hinweis ?? "");

            Anlagen("KWKG_Stichtag = NULL, KWKG_Inbetriebnahme = '2031-03-01 00:00:00'");
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ProjektWirtschaftlichkeit SET KWKG_Stichtag = NULL WHERE ID_Projekt = ?",
                new DbParam("@p", PROJEKT));
            Assert.Equal(0.0, Rechne().KwkgErloesJahr1);
        }

        /// <summary>
        /// Der Projektblock (keine Anlage trägt ein eigenes Datum): Förderbeginn
        /// 01.03.2031 — kein Zuschlag, mit der Zeile „nach dem Ende der Frist".
        /// </summary>
        [Fact]
        public void Der_Projektblock_prueft_dasselbe_Fristende()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Anlagen("KWKG_Stichtag = NULL, KWKG_Inbetriebnahme = NULL");
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ProjektWirtschaftlichkeit SET KWKG_Inbetriebnahme = '2031-03-01 00:00:00' " +
                "WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));
            WirtschaftlichkeitErgebnis e = Rechne();

            Assert.Equal(0.0, e.KwkgErloesJahr1);
            Assert.Contains(string.Format(BerichtTexte.Kultur, Resource.WIRT_KWKG_NACH_FRISTENDE,
                                          "01.03.2031", "31.12.2030", Herkunft()), e.Hinweis ?? "");

            // Förderbeginn im Jahr des Fristendes: Zuschlag (vorher, mit der Frist von
            // vier Jahren ab dem Stichtag 2025, keiner).
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_ProjektWirtschaftlichkeit SET KWKG_Stichtag = '2025-06-01 00:00:00', " +
                "KWKG_Inbetriebnahme = '2030-06-01 00:00:00' WHERE ID_Projekt = ?", new DbParam("@p", PROJEKT));
            Assert.True(Rechne().KwkgErloesJahr1 > 5000);
        }

        /// <summary>
        /// <b>Ohne Katalogwert keine stille Vorgabe.</b> Fehlt die Zeile, bleibt die Anlage
        /// förderfähig, und eine Herleitungszeile sagt, dass die Frist ungeprüft ist — die
        /// entfallene Konstante springt nicht ein.
        /// </summary>
        [Fact]
        public void Ohne_Katalogwert_bleibt_die_Frist_ungeprueft_statt_still_vorgegeben()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            GesetzKatalog.StelleKatalogSicher();
            DataRepository.ExecuteNonQuery(
                "DELETE FROM Tab_Gesetzesparameter WHERE Schluessel = ?",
                new DbParam("@s", DbParamTyp.VarWChar, 60) { Wert = DbWerte.GESETZ_KWKG_INBETRIEBNAHME_FRISTENDE });

            Anlagen("KWKG_Inbetriebnahme = '2031-03-01 00:00:00'");
            WirtschaftlichkeitErgebnis e = Rechne();

            Assert.True(e.KwkgErloesJahr1 > 5000, "Jahr 1: " + e.KwkgErloesJahr1);
            Assert.Contains("kein Ende der Frist zur Inbetriebnahme", e.Hinweis ?? "");
            Assert.Contains(DbWerte.GESETZ_KWKG_INBETRIEBNAHME_FRISTENDE, e.Hinweis ?? "");
        }

        // =================================================================
        //  Keine Höchstdauer in Kalenderjahren
        // =================================================================

        /// <summary>
        /// Inbetriebnahme 01.10.2026 wie im Beispiel des Rechenwegs 05: Deckel 3.300 ·
        /// 3.100 · 2.900 · 2.700, ab 2030 je 2.500 h — die Reihe zahlt über 2030 hinaus,
        /// bis das Kontingent im Jahr 12 (2037) mit 500 h erschöpft ist; ab Jahr 13 ist sie 0.
        /// </summary>
        [Fact]
        public void Die_Reihe_laeuft_nach_2030_bis_zum_Ende_des_Kontingents()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Anlagen("KWKG_Stichtag = '2026-09-01 00:00:00', KWKG_Inbetriebnahme = '2026-10-01 00:00:00'");
            double[] r = Reihe();

            for (int t = 1; t <= 12; t++)
                Assert.True(r[t] > 0, "Jahr " + t + " (" + (2025 + t) + ") ohne Zuschlag.");
            for (int t = 13; t < r.Length; t++)
                Assert.Equal(0.0, r[t]);

            // 2030 bis 2036 stehen auf dem Deckel 2.500 h — gleich hoch; 2037 zahlt den Rest
            // von 500 h, also ein Fünftel davon.
            for (int t = 6; t <= 11; t++)
                Assert.Equal(r[5], r[t], 6);
            Assert.Equal(r[5] * 500.0 / 2500.0, r[12], 6);
        }

        // =================================================================
        //  Werkzeug
        // =================================================================

        private static string Herkunft()
        {
            GesetzParameter p = GesetzKatalog.Vorbelegung()
                .Single(x => x.Schluessel == DbWerte.GESETZ_KWKG_INBETRIEBNAHME_FRISTENDE);
            return p.Schluessel + ", " + p.Quelle;
        }

        private static void Anlagen(string setze)
        {
            DataRepository.ExecuteNonQuery(
                "UPDATE Tab_Energieanlagen SET " + setze + " WHERE ID IN (" + ANLAGEN + ")");
        }

        private static WirtschaftlichkeitErgebnis Rechne()
        {
            BerichtsDaten daten = Daten(out WirtschaftlichkeitParameter p);
            WirtschaftlichkeitErgebnis e = new WirtschaftlichkeitCtrl().Berechne(daten, p).FirstOrDefault(
                x => x.Szenario == WirtschaftlichkeitSzenario.ERWARTET && x.IdProjekt == PROJEKT);
            Assert.NotNull(e);
            return e;
        }

        private static double[] Reihe()
        {
            BerichtsDaten daten = Daten(out WirtschaftlichkeitParameter p);
            WirtschaftlichkeitVerlauf vl = new WirtschaftlichkeitCtrl().BerechneVerlauf(
                daten, p, Math.Max(1, p.Betrachtungszeitraum), WirtschaftlichkeitSzenario.ERWARTET);
            VerlaufSerie s = vl.Absolut.FirstOrDefault(x => x.IdProjekt == PROJEKT && x.Bild != null);
            Assert.NotNull(s);
            KapitalwertRechner.ErloesReihe r = s.Bild.ErloesReihen.FirstOrDefault(
                x => x.Name == KapitalwertRechner.ErloesReihe.KWKG);
            Assert.NotNull(r);
            return r.JeJahr;
        }

        private static BerichtsDaten Daten(out WirtschaftlichkeitParameter p)
        {
            p = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT,
                IstStamm = true,
                Projektname = "Prüffall A20",
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT)
            };
            KostenEmissionRechner.Berechne(v);
            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            return daten;
        }
    }
}
