using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Welle KB-7c2 — die Berichtskennzahl Wasserkosten des Rückkühlwerks</b> (Entwurf
    /// <c>Dialoge/2026-10-16_Entwurf_KB7_Wasserkosten_Rueckkuehlwerk.md</c>, Abschnitte 5 und 9, Anwenderentscheid F4;
    /// Katalog v24).
    ///
    /// <para><c>kaelte.rk.wasser_kosten</c> steht neben <c>kaelte.rk.wasser_m3</c>: Menge × Arbeitspreis des
    /// Wasserträgers im ersten Betrachtungsjahr — dieselbe Rechnung wie die Nachweiszeile der Wirtschaftlichkeit;
    /// ohne Preis und ohne Rückkühlwerk leer (nie 0 €); mit den Trägerpreisen des gerechneten Szenarios.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class WasserkostenKennzahlTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int PROJEKT = 1030;           // gespeicherter Lauf mit bepreisten Trägern
        private const double MENGE = 1000.0;        // m³/a, zwei Maschinen 400 + 600
        private const double PREIS = 2.5;           // €/m³

        /// <summary>Der Eintrag: Gruppe Kälte, €/a, Fassung 24, Grenze „sensibel“, nicht im Energievergleich.</summary>
        [Fact]
        public void Der_Eintrag_steht_neben_der_Wassermenge()
        {
            var alle = KennzahlenKatalog.Alle().Select(k => k.Schluessel).ToList();
            int menge = alle.IndexOf(KennzahlenKatalog.SCHLUESSEL_RK_WASSER);
            Assert.Equal(menge + 1, alle.IndexOf(KennzahlenKatalog.SCHLUESSEL_RK_WASSER_KOSTEN));
            Kennzahl k = Eintrag();
            Assert.Equal("kaelte.rk.wasser_kosten", k.Schluessel);
            Assert.Equal(KennzahlenKatalog.GR_KAELTE, k.Gruppe);
            Assert.Equal("€/a", k.Einheit);
            Assert.Equal(24, k.Seit);
            Assert.Equal(Vorlagenfeldkatalog.FASSUNG_WASSERKOSTEN_RUECKKUEHLWERK, k.Seit);
            Assert.Equal("Wasserkosten Rückkühlwerke (sensibel)", k.LabelDe);
            Assert.Equal("Heat rejection water cost (sensible)", k.LabelEn);
            Assert.False(Energievergleich.GehoertDazu(k));
        }

        /// <summary>Mit Preis: Menge × Preis, gleich der Nachweiszeile der Wirtschaftlichkeit.</summary>
        [Fact]
        public void Mit_Preis_Menge_mal_Preis()
        {
            if (!_db.Vorhanden) return;
            Zuordnen(PROJEKT, Katalogtraeger(), PREIS);
            VariantenDaten v = Gerechnet(PROJEKT, MENGE, null);
            Assert.Equal(MENGE * PREIS, Eintrag().Wert(v).Value, 9);
            KostenPositionNachweis zeile = Wasserkosten.Nachweis(Wasserkosten.Rechne(PROJEKT, v, null));
            Assert.Equal(zeile.BetragJahr, Eintrag().Wert(v).Value, 9);
        }

        /// <summary>Ohne Preis (leer oder 0) bleibt die Kennzahl leer, nicht 0 — die Menge steht weiter.</summary>
        [Fact]
        public void Ohne_Preis_leer()
        {
            if (!_db.Vorhanden) return;
            VariantenDaten v = Gerechnet(PROJEKT, MENGE, null);
            Assert.Null(Eintrag().Wert(v));
            Assert.Equal(MENGE, KennzahlenKatalog.Alle().Single(k => k.Schluessel == KennzahlenKatalog.SCHLUESSEL_RK_WASSER).Wert(v).Value, 9);

            Zuordnen(PROJEKT, Katalogtraeger(), 0.0);
            Assert.Null(Eintrag().Wert(Gerechnet(PROJEKT, MENGE, null)));
        }

        /// <summary>Ohne Rückkühlwerk mit Wasser bleibt die Kennzahl leer, auch mit Preis.</summary>
        [Fact]
        public void Ohne_Rueckkuehlwerk_leer()
        {
            if (!_db.Vorhanden) return;
            Zuordnen(PROJEKT, Katalogtraeger(), PREIS);
            Assert.Null(Eintrag().Wert(Gerechnet(PROJEKT, 0.0, null)));
            Assert.Null(Eintrag().Wert(new VariantenDaten { IdProjekt = PROJEKT }));
        }

        /// <summary>Das Szenario folgt der Wirtschaftlichkeit: im Best-Fall der Szenariopreis, im Erwartungsfall
        /// der Projektpreis.</summary>
        [Fact]
        public void Das_Szenario_folgt_der_Wirtschaftlichkeit()
        {
            if (!_db.Vorhanden) return;
            int wasser = Katalogtraeger();
            Zuordnen(PROJEKT, wasser, PREIS);
            Assert.True(EnergietraegerPreisCtrl.SzenarioSchreiben(PROJEKT, wasser,
                new TraegerpreisSzenario { ArbeitspreisBest = 1.0 }));
            Assert.Equal(MENGE * PREIS, Eintrag().Wert(Gerechnet(PROJEKT, MENGE, WirtschaftlichkeitSzenario.ERWARTET)).Value, 9);
            Assert.Equal(MENGE * 1.0, Eintrag().Wert(Gerechnet(PROJEKT, MENGE, WirtschaftlichkeitSzenario.BEST)).Value, 9);
            Assert.Equal(MENGE * PREIS, Eintrag().Wert(Gerechnet(PROJEKT, MENGE, null)).Value, 9);
        }

        private static Kennzahl Eintrag() =>
            KennzahlenKatalog.Alle().Single(k => k.Schluessel == KennzahlenKatalog.SCHLUESSEL_RK_WASSER_KOSTEN);

        private static int Katalogtraeger() =>
            (int)(WasserpreisTraegerkarteSchema.TraegerId() ?? 0);

        /// <summary>Ordnet dem Projekt den Träger mit Arbeitspreis zu (Zeile anlegen oder Preis setzen).</summary>
        private static void Zuordnen(int projekt, int traeger, double preis)
        {
            Assert.True(traeger > 0, "Kein Wasserträger im Katalog");
            object id = DataRepository.ExecuteScalar(
                "SELECT ID FROM energy_project_settings WHERE ID_Projekt = ? AND [ID_Energieträger] = ?",
                new DbParam("@p", projekt), new DbParam("@t", traeger));
            if (id == null || id == DBNull.Value)
                DataRepository.ExecuteNonQuery(
                    "INSERT INTO energy_project_settings (ID_Projekt, [ID_Energieträger], custom_price_work) VALUES (?, ?, ?)",
                    new DbParam("@p", projekt), new DbParam("@t", traeger), new DbParam("@w", preis));
            else
                DataRepository.ExecuteNonQuery(
                    "UPDATE energy_project_settings SET custom_price_work = ? WHERE ID = ?",
                    new DbParam("@w", preis), new DbParam("@id", id));
        }

        /// <summary>Der gespeicherte Lauf des Projekts mit <paramref name="wasserM3"/> m³ Wasser (40 % / 60 % auf
        /// zwei Maschinen, eine dritte trocken), durch den Kostenrechner im Szenario <paramref name="szenario"/>.</summary>
        private static VariantenDaten Gerechnet(int projekt, double wasserM3, string szenario)
        {
            ErgebnisModel erg = new ErgebnisCtrl().Load(projekt);
            Assert.NotNull(erg);
            erg.Kaeltemaschinen = wasserM3 <= 0
                ? new System.Collections.Generic.List<ErgebnisKaeltemaschineModel>()
                : new System.Collections.Generic.List<ErgebnisKaeltemaschineModel>
                  {
                      new ErgebnisKaeltemaschineModel { Bezeichner = "KM 1", Wasser_m3 = wasserM3 * 0.4 },
                      new ErgebnisKaeltemaschineModel { Bezeichner = "KM 2", Wasser_m3 = wasserM3 * 0.6 },
                      new ErgebnisKaeltemaschineModel { Bezeichner = "KM 3 trocken", Wasser_m3 = null }
                  };
            var v = new VariantenDaten { IdProjekt = projekt, Ergebnis = erg };
            KostenEmissionRechner.Berechne(v, szenario);
            return v;
        }
    }
}
