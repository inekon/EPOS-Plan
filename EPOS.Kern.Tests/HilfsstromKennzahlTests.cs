using System;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// 6b (Katalog v26) — <b>die Kennzahlen des Hilfsstroms</b>: Menge (<c>energie.hilfsstrom</c>), Emissionen
    /// (<c>em.co2_hilfsstrom</c>) und Stromsteuer (<c>ko.stromsteuer_hilfsstrom</c> gesondert angesetzt,
    /// <c>ko.stromsteuer_hilfsstrom_gesamt</c> zum Regelsatz). Am Referenzprojekt 1030 mit 2 % Hilfsenergieanteil
    /// an beiden BHKW; Gegenprobe ohne Anteil: keine Zeile.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class HilfsstromKennzahlTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();
        public void Dispose() => _kultur.Dispose();

        private const int PROJEKT = 1030;
        private const int BHKW_GROSS = 14920;
        private const int BHKW_KLEIN = 14921;

        [Fact]
        public void Die_Kennzahlen_stehen_im_Katalog_der_Fassung_26()
        {
            foreach (string s in new[]
                     {
                         KennzahlenKatalog.SCHLUESSEL_HILFSSTROM, KennzahlenKatalog.SCHLUESSEL_HILFSSTROM_CO2,
                         KennzahlenKatalog.SCHLUESSEL_HILFSSTROM_STROMSTEUER,
                         KennzahlenKatalog.SCHLUESSEL_HILFSSTROM_STROMSTEUER_GESAMT,
                     })
            {
                Kennzahl k = KennzahlenKatalog.Alle().Single(x => x.Schluessel == s);
                Assert.Equal(26, k.Seit);
                Assert.False(string.IsNullOrWhiteSpace(k.LabelDe));
                Assert.False(string.IsNullOrWhiteSpace(k.LabelEn));
                Assert.NotEqual(k.LabelDe, k.LabelEn);
            }
            Assert.Equal("Hilfsstrom BHKW und Kessel",
                KennzahlenKatalog.Alle().Single(x => x.Schluessel == KennzahlenKatalog.SCHLUESSEL_HILFSSTROM).LabelDe);
            Assert.Equal("Electricity tax on auxiliary electricity, charged separately",
                KennzahlenKatalog.Alle().Single(x => x.Schluessel == KennzahlenKatalog.SCHLUESSEL_HILFSSTROM_STROMSTEUER).LabelEn);
        }

        private static void Anteil(int anlage, double? prozent)
        {
            DataRepository.ExecuteSQL(
                "UPDATE Tab_Energieanlagen SET Hilfsenergie_Anteil = ? WHERE ID = ?",
                new DbParam("@w", DbParamTyp.Double) { Wert = prozent.HasValue ? (object)prozent.Value : DBNull.Value },
                new DbParam("@a", DbParamTyp.Integer) { Wert = anlage });
        }

        /// <summary>Wie der Berichtssammler: Kosten und Emissionen, Kennzahlen, dann die Wirtschaftlichkeit.</summary>
        private static VariantenDaten Rechne()
        {
            var v = new VariantenDaten
            {
                IdProjekt = PROJEKT, IstStamm = true, Projektname = "Hilfsstrom " + PROJEKT,
                Ergebnis = new ErgebnisCtrl().Load(PROJEKT)
            };
            KostenEmissionRechner.Berechne(v);
            KennzahlenKatalog.Berechne(v);
            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            var daten = new BerichtsDaten { IdStamm = PROJEKT, Stammprojektname = v.Projektname };
            daten.Varianten.Add(v);
            new WirtschaftlichkeitCtrl().Berechne(daten, p);
            return v;
        }

        [Fact]
        public void Ohne_Anteil_tragen_die_Kennzahlen_keinen_Wert()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            VariantenDaten v = Rechne();
            Assert.Null(v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_HILFSSTROM]);
            Assert.Null(v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_HILFSSTROM_CO2]);
            Assert.Null(v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_HILFSSTROM_STROMSTEUER]);
            Assert.Null(v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_HILFSSTROM_STROMSTEUER_GESAMT]);
        }

        /// <summary>
        /// 2 % an beiden BHKW: Hilfsstrom 2 % des BHKW-Brennstoffs; die Emission ist Hilfsstrom × Faktor des
        /// Netzstroms (das Feld des Standes); ohne Stromsteuer im Arbeitspreis wird der Regelsatz voll gesondert
        /// angesetzt — gesondert = gesamt = Hilfsstrom × Regelsatz, wie die Nachweiszeile der Wirtschaftlichkeit.
        /// </summary>
        [Fact]
        public void Mit_Anteil_tragen_die_Kennzahlen_Menge_Emission_und_Steuer()
        {
            using var db = new TestDatenbank();
            if (!db.Vorhanden) return;

            Anteil(BHKW_GROSS, 2.0);
            Anteil(BHKW_KLEIN, 2.0);
            double hilfs = new ErgebnisCtrl().Load(PROJEKT).BHKW.Module.Sum(x => x.Verbrauch) * 0.02;

            VariantenDaten v0 = Rechne();
            int traeger = v0.EnergiekostenJeTraeger.First(t => t.Netzstrom).CarrierId;
            var zerlegung = new StrompreisZerlegungCtrl();
            StrompreisZerlegungModel z = zerlegung.Read(PROJEKT, traeger);
            z.ID_Projekt = PROJEKT; z.ID_Energietraeger = traeger;
            z.Stromsteuer = 2.05; z.Stromsteuer_Aktiv = false;
            Assert.True(zerlegung.Update(z));

            VariantenDaten v = Rechne();
            Assert.Equal(hilfs, v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_HILFSSTROM].Value, 9);
            Assert.True(v.HilfsstromCO2t > 0);
            Assert.Equal(v.HilfsstromCO2t, v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_HILFSSTROM_CO2].Value, 9);

            WirtschaftlichkeitParameter p = new WirtschaftlichkeitCtrl().LadeParameter(PROJEKT);
            int jahr = p.KwkgInbetriebnahme.HasValue ? p.KwkgInbetriebnahme.Value.Year : DateTime.Now.Year + 1;
            double regel = new GesetzKatalog().Wert(DbWerte.GESETZ_STROMST_REGELSATZ, jahr).Value;
            Assert.Equal(hilfs * regel, v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_HILFSSTROM_STROMSTEUER].Value, 6);
            Assert.Equal(hilfs * regel, v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_HILFSSTROM_STROMSTEUER_GESAMT].Value, 6);

            // Anteil in Höhe des Regelsatzes: enthalten, nichts gesondert — der Betrag zum Regelsatz bleibt.
            z.Stromsteuer = regel / 10.0; z.Stromsteuer_Aktiv = true;
            Assert.True(zerlegung.Update(z));
            v = Rechne();
            Assert.Equal(0.0, v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_HILFSSTROM_STROMSTEUER].Value, 9);
            Assert.Equal(hilfs * regel, v.Kennzahlen[KennzahlenKatalog.SCHLUESSEL_HILFSSTROM_STROMSTEUER_GESAMT].Value, 6);
        }
    }
}
