using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Jahresganglinie des Grafikreiters im Ergebnisdialog der Bedarfsprofile folgt der
    /// gewählten Energieeinheit über ihre Leistungseinheit: MWh zeigt MW (Werte durch 1000),
    /// kWh zeigt kW (Werte unverändert). Achsentitel, Datenreihen und CSV-Spalten — sowohl die
    /// Spalten der Hülle als auch der Ganglinienexport aus dem Modell — gehen mit.
    /// Gehalten an <see cref="BedarfErgebnisHuelle.KaelteDaten"/>, der Weg ohne Datenbank; Strom und
    /// Wärme bauen dieselbe Bildquelle.
    /// </summary>
    public sealed class BedarfErgebnisGanglinieEinheitTests
    {
        private static double[] Reihe()
        {
            var r = new double[8760];
            for (int i = 0; i < r.Length; i++) r[i] = 100.0 + (i % 24) * 12.5;
            return r;
        }

        private static Bedarfsgrafikquelle Quelle(double[] reihe)
            => BedarfErgebnisHuelle.KaelteDaten(reihe, new double[12], 0.0, 0).Grafik;

        private static IEnumerable<Text> Texte(IEnumerable<Zeichenbefehl> befehle)
        {
            foreach (Zeichenbefehl b in befehle)
            {
                if (b is Text t) yield return t;
                else if (b is Gruppe g)
                    foreach (Text u in Texte(g.Befehle)) yield return u;
            }
        }

        [Fact]
        public void Jahr_in_MWh_zeigt_MW_und_teilt_durch_tausend()
        {
            double[] reihe = Reihe();
            Zeichenmodell m = Quelle(reihe).Modell(new[] { 0 }, Bedarfsraster.Jahr, Energieeinheit.MWh);

            Assert.NotNull(m);
            Datenreihe d = Assert.Single(m.Reihen);
            Assert.Equal(reihe.Length, d.Werte.Length);
            for (int i = 0; i < reihe.Length; i += 97)
                Assert.Equal(reihe[i] / 1000.0, d.Werte[i], 12);
            Assert.Contains(Texte(m.Befehle), t => t.Inhalt.EndsWith("[MW]"));
            Assert.DoesNotContain(Texte(m.Befehle), t => t.Inhalt.EndsWith("[kW]"));
        }

        [Fact]
        public void Jahr_in_kWh_zeigt_kW_und_laesst_die_Werte_stehen()
        {
            double[] reihe = Reihe();
            Zeichenmodell m = Quelle(reihe).Modell(new[] { 0 }, Bedarfsraster.Jahr, Energieeinheit.KWh);

            Datenreihe d = Assert.Single(m.Reihen);
            Assert.Equal(reihe, d.Werte);
            Assert.Contains(Texte(m.Befehle), t => t.Inhalt.EndsWith("[kW]"));
            Assert.DoesNotContain(Texte(m.Befehle), t => t.Inhalt.EndsWith("[MW]"));
        }

        [Fact]
        public void Ohne_Einheit_gilt_die_Vorgabe_MW()
        {
            double[] reihe = Reihe();
            Zeichenmodell m = Quelle(reihe).Modell(new[] { 0 }, Bedarfsraster.Jahr, null);
            Assert.Equal(reihe[5] / 1000.0, Assert.Single(m.Reihen).Werte[5], 12);
        }

        [Theory]
        [InlineData("MWh", "MW", 1000.0)]
        [InlineData("kWh", "kW", 1.0)]
        public void Der_CSV_Export_der_Ganglinie_folgt_der_Einheit(string wahl, string leistung, double teiler)
        {
            double[] reihe = Reihe();
            Energieeinheit e = Energieeinheit.AusText(wahl);
            Bedarfsgrafikquelle q = Quelle(reihe);

            ZeitreihenSpalte spalte = Assert.Single(q.Spalten(new[] { 0 }, Bedarfsraster.Jahr, e));
            Assert.Equal(leistung, spalte.Einheit);
            Assert.Equal(8760, spalte.Werte.Length);
            Assert.Equal(reihe[13] / teiler, spalte.Werte[13], 12);

            // Der Ganglinienexport der Kaskade schreibt die Reihen des Modells.
            ZeitreihenSpalte ausModell = Assert.Single(ZeitreihenCsv.AusModell(q.Modell(new[] { 0 }, Bedarfsraster.Jahr, e)));
            Assert.Equal(8760, ausModell.Werte.Length);
            Assert.Equal(reihe[13] / teiler, ausModell.Werte[13], 12);
        }

        [Fact]
        public void Die_Summen_bleiben_Energiemengen_in_der_Wahl()
        {
            Bedarfsgrafikquelle q = Quelle(Reihe());
            Assert.Equal("MWh", Assert.Single(q.Spalten(new[] { 0 }, Bedarfsraster.Monat, Energieeinheit.MWh)).Einheit);
            Assert.Equal("kWh", Assert.Single(q.Spalten(new[] { 0 }, Bedarfsraster.Tag, Energieeinheit.KWh)).Einheit);
        }
    }
}
