using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using WindowsFormsApplication1.Referenzlauf;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Plausibilitätsprüfung des Referenzlaufs</b> (<c>Referenzlauf/Plausibilitaet.cs</c>,
    /// Modus <c>pruefen</c>) und ihre eine benannte Ausnahme: Vorlauf und Rücklauf des Heiz- und
    /// des Kältekreises eines gekoppelten Gebäudes tragen in den Stunden ohne Betrieb NaN — die
    /// gewollte Lücke der Reihe (Anlagenkopplung 8.3). Nur diese vier Dateimuster dürfen NaN als
    /// Lücke tragen; Inf, Text und eine Reihe nur aus Lücken bleiben beanstandet, jede andere
    /// Datei behandelt NaN als ungültigen Wert.
    /// </summary>
    public class ReferenzlaufPlausibilitaetTests
    {
        [Theory]
        [InlineData("vorlauf_0.csv", true)]
        [InlineData("ruecklauf_0.csv", true)]
        [InlineData("kuehlvorlauf_0.csv", true)]
        [InlineData("kuehlruecklauf_12.csv", true)]
        [InlineData("uebergabe_0.csv", false)]
        [InlineData("kuehluebergabe_0.csv", false)]
        [InlineData("vorlauf.csv", false)]
        [InlineData("vorlauf_x.csv", false)]
        [InlineData("Vorlauf_0.csv", false)]
        [InlineData("waermebedarf.csv", false)]
        [InlineData("raumtemperatur_0.csv", false)]
        [InlineData("vorlauf_0.csv.bak", false)]
        public void Nur_die_vier_Reihen_des_Heiz_und_Kaeltekreises_duerfen_Luecken_tragen(string datei, bool erlaubt)
        {
            Assert.Equal(erlaubt, Plausibilitaet.IstLueckenreihe(datei));
        }

        [Fact]
        public void NaN_ist_in_einer_Lueckenreihe_eine_Luecke_und_sonst_ungueltig()
        {
            string inhalt = Reihe(new[] { "35.5", "NaN", "40", "NaN" });

            Plausibilitaet.Reihenbefund luecke = Plausibilitaet.ReiheLesen("vorlauf_0.csv", new StringReader(inhalt));
            Assert.Equal(4, luecke.Zeilen);
            Assert.Equal(2, luecke.Luecken);
            Assert.Equal(0, luecke.Ungueltig);
            Assert.Equal(75.5, luecke.Summe, 12);

            Plausibilitaet.Reihenbefund sonst = Plausibilitaet.ReiheLesen("waermebedarf.csv", new StringReader(inhalt));
            Assert.Equal(0, sonst.Luecken);
            Assert.Equal(2, sonst.Ungueltig);
            Assert.Equal(75.5, sonst.Summe, 12);
        }

        [Fact]
        public void Inf_und_Text_bleiben_auch_in_einer_Lueckenreihe_ungueltig()
        {
            string inhalt = Reihe(new[] { "Infinity", "-Infinity", "abc", "NaN", "20" });
            Plausibilitaet.Reihenbefund b = Plausibilitaet.ReiheLesen("ruecklauf_3.csv", new StringReader(inhalt));
            Assert.Equal(5, b.Zeilen);
            Assert.Equal(3, b.Ungueltig);
            Assert.Equal(1, b.Luecken);
            Assert.Equal(20.0, b.Summe, 12);
        }

        /// <summary>
        /// Der ganze Modus an einem Projektordner: Lücken in <c>vorlauf_0.csv</c> sind kein Grund zur
        /// Beanstandung — dieselbe Datei nur aus Lücken ist einer, und NaN in einer anderen
        /// Reihe ebenso.
        /// </summary>
        [Fact]
        public void Pruefen_nimmt_Luecken_hin_und_beanstandet_eine_Reihe_nur_aus_Luecken()
        {
            string ordner = Path.Combine(Path.GetTempPath(), "epos-plausi-" + Guid.NewGuid().ToString("N").Substring(0, 8));
            try
            {
                string projekt = Path.Combine(ordner, "Projekt_1");
                Directory.CreateDirectory(projekt);
                File.WriteAllText(Path.Combine(projekt, "aggregate.csv"), "Groesse;Wert\nLauf.ID_Projekt;1\n");
                File.WriteAllText(Path.Combine(projekt, "waermebedarf.csv"),
                                  Reihe(Enumerable.Range(0, 8760).Select(h => "1").ToArray()));
                File.WriteAllText(Path.Combine(projekt, "vorlauf_0.csv"),
                                  Reihe(Enumerable.Range(0, 8760).Select(h => h % 3 == 0 ? "NaN" : "42.5").ToArray()));

                Assert.Equal(0, Plausibilitaet.Pruefen(ordner));

                File.WriteAllText(Path.Combine(projekt, "vorlauf_0.csv"),
                                  Reihe(Enumerable.Range(0, 8760).Select(h => "NaN").ToArray()));
                Assert.Equal(1, Plausibilitaet.Pruefen(ordner));

                File.WriteAllText(Path.Combine(projekt, "vorlauf_0.csv"),
                                  Reihe(Enumerable.Range(0, 8760).Select(h => "42.5").ToArray()));
                File.WriteAllText(Path.Combine(projekt, "uebergabe_0.csv"),
                                  Reihe(Enumerable.Range(0, 8760).Select(h => h == 7 ? "NaN" : "0").ToArray()));
                Assert.Equal(1, Plausibilitaet.Pruefen(ordner));
            }
            finally
            {
                try { Directory.Delete(ordner, true); } catch { /* Aufraeumen darf nicht scheitern */ }
            }
        }

        /// <summary>Eine Vektordatei im Format des Exports: Kopf, dann <c>Index;Wert</c>.</summary>
        private static string Reihe(string[] werte)
        {
            var sb = new StringBuilder("Index;Wert\n");
            for (int i = 0; i < werte.Length; i++)
                sb.Append(i.ToString(CultureInfo.InvariantCulture)).Append(';').Append(werte[i]).Append('\n');
            return sb.ToString();
        }
    }
}
