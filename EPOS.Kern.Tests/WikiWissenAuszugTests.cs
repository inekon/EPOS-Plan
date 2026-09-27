using System;
using System.Linq;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Wiki-Auszug folgt der Frage</b> (Auftrag #571): Ein Abschnitt tief in einer langen Seite
    /// kommt in den Auszug des Hilfe-Assistenten, wenn die Frage ihn trifft — die Kappung vom
    /// Seitenanfang her schnitt ihn ab. Die Höchstlänge bleibt <see cref="WikiWissen.MAX_ZEICHEN"/>.
    /// </summary>
    public sealed class WikiWissenAuszugTests
    {
        private static string LangeSeite()
        {
            var sb = new StringBuilder("Die Gebäude sind die Grundlage der Heizwärmeberechnung.\n");
            for (int i = 0; i < 12; i++)
                sb.Append("=== Abschnitt ").Append(i).Append(" ===\n").Append(new string('x', 1500)).Append('\n');
            sb.Append("=== Reiter „Temperaturen und Ferien“ ===\n")
              .Append("Soll am Wochenende: absolute Solltemperatur, ganztägig; 0 heißt keine Wochenendabsenkung.\n");
            for (int i = 12; i < 15; i++)
                sb.Append("=== Abschnitt ").Append(i).Append(" ===\n").Append(new string('y', 1500)).Append('\n');
            return sb.ToString();
        }

        [Fact]
        public void Ein_tiefer_Abschnitt_kommt_in_den_Auszug_wenn_die_Frage_ihn_trifft()
        {
            string seite = LangeSeite();
            Assert.DoesNotContain("Temperaturen und Ferien", WikiWissen.Kappen(seite));

            string auszug = WikiWissen.Kappen(seite, "wie ist die Wochenendabsenkung zu verstehen - differenz oder absolut?");
            Assert.Contains("Temperaturen und Ferien", auszug);
            Assert.Contains("keine Wochenendabsenkung", auszug);
            Assert.StartsWith("Die Gebäude sind die Grundlage", auszug);
            Assert.True(auszug.Length <= WikiWissen.MAX_ZEICHEN, "Länge " + auszug.Length);
        }

        [Fact]
        public void Ohne_Treffer_und_bei_kurzen_Seiten_bleibt_die_Kappung_vom_Anfang()
        {
            string seite = LangeSeite();
            Assert.Equal(WikiWissen.Kappen(seite), WikiWissen.Kappen(seite, "Photovoltaikmodul Wechselrichter"));
            Assert.Equal("kurz", WikiWissen.Kappen("kurz", "Wochenendabsenkung"));
        }

        [Fact]
        public void Ein_Treffer_ueber_dem_Rahmen_kommt_gekappt()
        {
            string seite = "Anfang\n=== A ===\n" + new string('z', 3000) + "\n=== B ===\nWochenendabsenkung " + new string('w', 9000) + "\n";
            string auszug = WikiWissen.Kappen(seite, "Wochenendabsenkung");
            Assert.Contains("=== B ===", auszug);
            Assert.True(auszug.Length <= WikiWissen.MAX_ZEICHEN, "Länge " + auszug.Length);
        }
    }
}
