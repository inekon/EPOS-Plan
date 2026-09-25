using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wache der Parameterschlüssel des Zapfprofilgenerators</b> (Umsetzungskonzept
    /// Zapfprofilgenerator, Kapitel 9 Zeile ZU31): Der Anwender-Katalogimport nimmt Parameter an und
    /// lehnt einen Schlüssel ab, den kein Rechenweg liest. Dafür führt
    /// <see cref="TwwParameterkatalog"/> die Liste aller gelesenen Schlüssel samt Einheit und
    /// Bereich — und diese Wache hält sie gegen die Konstanten, bei denen sie gelesen werden:
    /// <list type="bullet">
    /// <item>jede Schlüsselkonstante von <see cref="ZapfAuslegungParameter"/>,
    /// <see cref="ZapfParameter"/> und <see cref="ZapfStochastikParameter"/> steht in der Liste;</item>
    /// <item>jeder Eintrag der Liste gehört zu einer dieser Konstanten — kein toter Schlüssel;</item>
    /// <item>jeder Eintrag trägt einen brauchbaren Bereich, und jeder Vorsatz nimmt nur einen
    /// Schlüssel MIT Rest an;</item>
    /// <item>jeder Parameter des freien Paketteils ist bekannt, in seiner Einheit und in seinem
    /// Bereich — sonst lehnte der Import die eigene Auslieferung ab.</item>
    /// </list>
    /// </summary>
    public sealed class TwwParameterschluesselWacheTests
    {
        private static readonly Type[] KLASSEN =
        {
            typeof(ZapfAuslegungParameter), typeof(ZapfParameter), typeof(ZapfStochastikParameter)
        };

        /// <summary>Jede Zeichenkettenkonstante der drei Klassen: Klasse, Name und Wert.</summary>
        private static IEnumerable<(string Klasse, string Name, string Wert)> Konstanten()
            => KLASSEN.SelectMany(k => k.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
                                        .Where(f => f.IsLiteral && f.FieldType == typeof(string))
                                        .Select(f => (k.Name, f.Name, (string)f.GetRawConstantValue())));

        [Fact]
        public void Jede_Schluesselkonstante_steht_im_Parameterkatalog()
        {
            var funde = new List<string>();
            foreach ((string klasse, string name, string wert) in Konstanten())
                if (!TwwParameterkatalog.ALLE.Any(p => string.Equals(p.Schluessel, wert, StringComparison.Ordinal)))
                    funde.Add(klasse + "." + name + " = \"" + wert + "\"");

            Assert.True(funde.Count == 0,
                "Diese Parameterschlüssel des Kerns fehlen in TwwParameterkatalog.ALLE — der Katalogimport " +
                "lehnte sie als unbekannt ab:" + Environment.NewLine + string.Join(Environment.NewLine, funde));
        }

        [Fact]
        public void Jeder_Eintrag_des_Parameterkatalogs_gehoert_zu_einer_Konstante()
        {
            var werte = new HashSet<string>(Konstanten().Select(k => k.Wert), StringComparer.Ordinal);
            string[] tot = TwwParameterkatalog.ALLE.Select(p => p.Schluessel)
                                                   .Where(s => !werte.Contains(s))
                                                   .OrderBy(s => s, StringComparer.Ordinal).ToArray();
            Assert.True(tot.Length == 0, "Einträge ohne Konstante im Kern (toter Schlüssel): " + string.Join(", ", tot));
        }

        [Fact]
        public void Jeder_Eintrag_traegt_einen_brauchbaren_Bereich_und_eine_Einheit_oder_keine()
        {
            var funde = new List<string>();
            foreach (TwwParameterschluessel p in TwwParameterkatalog.ALLE)
            {
                if (p.Schluessel.Trim().Length == 0) funde.Add("leerer Schlüssel");
                if (double.IsNaN(p.Min) || double.IsNaN(p.Max) || double.IsInfinity(p.Min) || double.IsInfinity(p.Max)
                    || !(p.Min < p.Max)) funde.Add(p.Schluessel + ": Bereich " + TwwParameterkatalog.Bereichstext(p));
                if (p.Einheit != null && p.Einheit.Trim().Length == 0) funde.Add(p.Schluessel + ": leere Einheit");
            }
            Assert.True(funde.Count == 0, string.Join(Environment.NewLine, funde));
            Assert.True(TwwParameterkatalog.ALLE.Count >= 60,
                        "Nur " + TwwParameterkatalog.ALLE.Count + " Einträge im Parameterkatalog.");
        }

        /// <summary>
        /// Ein Vorsatz nimmt nur einen Schlüssel MIT Rest an — der Vorsatz allein ist keiner; und der
        /// genaue Schlüssel geht dem Vorsatz vor.
        /// </summary>
        [Fact]
        public void Ein_Vorsatz_nimmt_nur_einen_Schluessel_mit_Rest_an()
        {
            foreach (TwwParameterschluessel p in TwwParameterkatalog.ALLE.Where(x => x.Vorsatz))
            {
                Assert.False(TwwParameterkatalog.Bekannt(p.Schluessel), p.Schluessel + " allein ist kein Schlüssel.");
                Assert.Same(p, TwwParameterkatalog.Finden(p.Schluessel + "Rest"));
            }

            // Die vier Familien mit einem echten Glied.
            Assert.True(TwwParameterkatalog.Bekannt(ZapfStochastikParameter.QUANTIL + "95"));
            Assert.True(TwwParameterkatalog.Bekannt(ZapfAuslegungParameter.DIN4708_PROFIL_BLOCK + "1.Beginn"));
            Assert.True(TwwParameterkatalog.Bekannt(ZapfAuslegungParameter.NENNINHALT_LISTE + "3"));
            Assert.True(TwwParameterkatalog.Bekannt(ZapfAuslegungParameter.KONSTRUKTOR_REGEL + "Dusche.Dauer"));

            // Ein erfundener Schlüssel ist nicht bekannt.
            Assert.False(TwwParameterkatalog.Bekannt("Erfunden.Kein.Parameter"));
            Assert.False(TwwParameterkatalog.Bekannt(""));
            Assert.Null(TwwParameterkatalog.Finden(null));

            // Der genaue Schlüssel geht vor: „DIN4708.Profil.Bloecke" ist kein Glied der Blockfamilie.
            TwwParameterschluessel bloecke = TwwParameterkatalog.Finden(ZapfAuslegungParameter.DIN4708_PROFIL_BLOECKE);
            Assert.NotNull(bloecke);
            Assert.False(bloecke.Vorsatz);
        }

        /// <summary>
        /// <b>Der freie Paketteil bleibt einspielbar</b>: Jeder Parameter, den
        /// <c>Referenzlaeufe/Katalogpaket_frei/Tab_TwwParameter_STAMM.csv</c> führt, ist dem Programm
        /// bekannt, trägt die Einheit der Liste und liegt in ihrem Bereich. Ohne Repositorium (Lauf
        /// außerhalb des Arbeitsbaums) schweigt der Fall.
        /// </summary>
        [Fact]
        public void Jeder_Parameter_des_freien_Paketteils_ist_bekannt_und_liegt_im_Bereich()
        {
            string wurzel = Berichtsdatenproben.Repowurzel();
            if (wurzel == null) return;
            string datei = Path.Combine(wurzel, "Referenzlaeufe", "Katalogpaket_frei", "Tab_TwwParameter_STAMM.csv");
            if (!File.Exists(datei)) return;

            string[] zeilen = File.ReadAllLines(datei, Encoding.UTF8);
            Assert.True(zeilen.Length > 1, "Die Parameterdatei des Paketteils ist leer.");
            string[] kopf = zeilen[0].Split(';');
            int iS = Array.IndexOf(kopf, "Schluessel");
            int iW = Array.IndexOf(kopf, "Wert");
            int iE = Array.IndexOf(kopf, "Einheit");
            Assert.True(iS >= 0 && iW >= 0, "Die Spalten Schluessel und Wert fehlen.");

            var funde = new List<string>();
            for (int z = 1; z < zeilen.Length; z++)
            {
                if (zeilen[z].Trim().Length == 0) continue;
                string[] f = zeilen[z].Split(';');
                string schluessel = f[iS].Trim();
                TwwParameterschluessel p = TwwParameterkatalog.Finden(schluessel);
                if (p == null) { funde.Add(schluessel + ": unbekannt"); continue; }
                if (iE >= 0 && iE < f.Length && !p.EinheitPasst(f[iE]))
                    funde.Add(schluessel + ": Einheit \"" + f[iE].Trim() + "\" statt \"" + p.Einheit + "\"");
                if (!double.TryParse(f[iW].Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out double wert))
                    funde.Add(schluessel + ": „" + f[iW].Trim() + "\" ist keine Zahl");
                else if (!p.ImBereich(wert))
                    funde.Add(schluessel + ": " + wert.ToString(CultureInfo.InvariantCulture) + " außerhalb "
                              + TwwParameterkatalog.Bereichstext(p));
            }
            Assert.True(funde.Count == 0,
                "Der freie Paketteil führt Parameter, die der Katalogimport ablehnte:" + Environment.NewLine
                + string.Join(Environment.NewLine, funde));
        }
    }
}
