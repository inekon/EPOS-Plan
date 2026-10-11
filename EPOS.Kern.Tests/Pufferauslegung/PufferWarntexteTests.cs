using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests.Pufferauslegung
{
    /// <summary>
    /// Die Klartexte der Warnliste als Textbausteine (Welle P4c): Jeder Code der Auslegungsliste
    /// (<see cref="PufferWarncode.ALLE"/>) hat mindestens einen Schlüssel <c>PA_&lt;CODE&gt;[_VARIANTE]_TEXT</c>
    /// in beiden Sprachen; in en-US steht kein deutsches Wort, die Platzhalter stimmen überein, und die
    /// Zahlen formatiert die Auflösung in der Sprache. Ohne Datenbank, ohne Kulturpinnung (Kulturen ausdrücklich).
    /// </summary>
    public class PufferWarntexteTests
    {
        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");
        private static readonly CultureInfo EN = CultureInfo.GetCultureInfo("en-US");

        private static readonly Regex DEUTSCH = new Regex(
            @"\b(der|die|das|den|dem|des|und|ist|nicht|über|unter|für|mit|auch|je|kein|keine|bleibt|Puffer|Speicher|Heizung|Gebäude|Wärmepumpe|Sperre)\b",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static string Schluesselpraefix(string code) => "PA_" + code.Substring(3).Replace('-', '_') + "_";

        private static IReadOnlyList<string> TextSchluessel() =>
            typeof(WindowsFormsApplication1.MyResource.Resource).GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Select(p => p.Name).Where(n => n.StartsWith("PA_", StringComparison.Ordinal) && n.EndsWith("_TEXT", StringComparison.Ordinal))
                .ToList();

        private static string Text(string schluessel, CultureInfo k) => WindowsFormsApplication1.MyResource.Resource.ResourceManager.GetString(schluessel, k) ?? "";

        [Fact]
        public void Jeder_Code_hat_einen_Klartext_in_beiden_Sprachen_ohne_deutsches_Wort_in_en_US()
        {
            IReadOnlyList<string> alle = TextSchluessel();
            foreach (string code in PufferWarncode.ALLE)
            {
                List<string> eigene = alle.Where(s => s.StartsWith(Schluesselpraefix(code), StringComparison.Ordinal)).ToList();
                Assert.True(eigene.Count > 0, "Kein Klartextschlüssel für " + code);
                foreach (string s in eigene)
                {
                    string de = Text(s, DE), en = Text(s, EN);
                    Assert.False(string.IsNullOrWhiteSpace(de), s);
                    Assert.False(string.IsNullOrWhiteSpace(en), s);
                    Assert.NotEqual(de, en);
                    Match m = DEUTSCH.Match(en);
                    Assert.False(m.Success, s + ": deutsches Wort „" + m.Value + "“ in „" + en + "“");
                    Assert.Equal(Platzhalter(de), Platzhalter(en));
                }
            }
        }

        private static string Platzhalter(string muster) =>
            string.Join(",", Regex.Matches(muster, @"\{\d+\}").Select(m => m.Value).Distinct().OrderBy(x => x, StringComparer.Ordinal));

        [Fact]
        public void Die_Aufloesung_formatiert_Zahlen_in_der_Sprache_und_der_Klartext_bleibt_deutsch()
        {
            Textbaustein t = Textbaustein.T("PA_STARTS_TAG_TEXT", "{0} Starts je Tag über der Warnschwelle {1}.", 1234.5, 8.0);
            Assert.Equal("1.234,5 Starts je Tag über der Warnschwelle 8.", t.Klartext);
            Assert.Equal("1,234.5 starts per day above the warning threshold of 8.", t.Aufloesen(EN));
            Assert.Equal(t.Klartext, t.Aufloesen(DE));

            var w = new PufferWarnung(PufferWarncode.STARTS_TAG, PufferStufe.Warnung, t, Textbaustein.Leer, PufferZone.Heizung);
            Assert.Equal(t.Klartext, w.Text);
            Assert.Same(t, w.KlartextBaustein);
            var alt = new PufferWarnung(PufferWarncode.STARTS_TAG, PufferStufe.Warnung, "Klartext", "", null);
            Assert.Equal("Klartext", alt.KlartextBaustein.Aufloesen(EN));
        }
    }
}
