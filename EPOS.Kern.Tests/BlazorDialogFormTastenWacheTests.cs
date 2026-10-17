using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Wächter über die TASTENWEGE der Windows-Schale: die Geste Strg+Umschalt+F12 im
    /// Dialog „Über EPOS-Plan" (<c>BlazorDialogForm.Befehlstaste</c>) und F1 im Hauptfenster
    /// (<c>BlazorSeite.Befehlstaste</c>, gesetzt vom <c>Hauptfensterrahmen</c>).
    ///
    /// <para><b>Die Regel, die er hält.</b> Liegt der Fokus im Browserfenster der WebView2
    /// (eigener Prozess), laufen Tastenmeldungen nie durch <c>ProcessCmdKey</c> der Form;
    /// sie kommen nur über das Tastenereignis der WebView2 (AcceleratorKeyPressed →
    /// <c>KeyDown</c> des Steuerelements). Jeder Befehlstastenhaken braucht deshalb beide
    /// Wege: <c>ProcessCmdKey</c> für den Fokus auf der WinForms-Seite und das <c>KeyDown</c>
    /// der WebView2 mit <c>Handled = true</c> für den Fokus im Browser.</para>
    ///
    /// <para>Dieses Projekt kann <c>WindowsFormsApplication1</c> nicht referenzieren — der
    /// Wächter liest wie die anderen Wächter den QUELLTEXT.</para>
    /// </summary>
    public class BlazorDialogFormTastenWacheTests
    {
        private const string DialogformPfad = "WindowsFormsApplication1/Allgemein/Blazor/BlazorDialogForm.cs";
        private const string SeitePfad = "WindowsFormsApplication1/Allgemein/Blazor/BlazorSeite.cs";
        private const string RahmenPfad = "WindowsFormsApplication1/Views/Hauptformular/Hauptfensterrahmen.cs";

        [Fact]
        public void Die_Dialogform_fragt_den_Haken_in_ProcessCmdKey()
        {
            string methode = Methode(Lies(DialogformPfad), "ProcessCmdKey");
            Assert.Matches(@"Befehlstaste\s*\(\s*taste\s*\)", methode);
            Assert.Contains("return true", methode);
        }

        [Fact]
        public void Die_Dialogform_haengt_den_Haken_an_das_KeyDown_der_WebView2()
        {
            PrueftKeyDownWeg(Lies(DialogformPfad), "BlazorDialogForm");
        }

        [Fact]
        public void Die_Seite_haengt_den_Haken_an_das_KeyDown_der_WebView2()
        {
            PrueftKeyDownWeg(Lies(SeitePfad), "BlazorSeite");
        }

        [Fact]
        public void Der_Hauptfensterrahmen_faengt_F1_auf_beiden_Wegen()
        {
            string quelle = Lies(RahmenPfad);
            string methode = Methode(quelle, "ProcessCmdKey");
            Assert.Contains("Keys.F1", methode);

            Match haken = Regex.Match(quelle, @"_bild\.Befehlstaste\s*=\s*\w+\s*=>\s*\{(?<rumpf>.*?)\};",
                RegexOptions.Singleline);
            Assert.True(haken.Success,
                "Hauptfensterrahmen setzt _bild.Befehlstaste nicht - F1 wirkt dann nicht bei Fokus im Browserfenster der WebView2.");
            Assert.Contains("Keys.F1", haken.Groups["rumpf"].Value);
            Assert.Contains("BeginInvoke", haken.Groups["rumpf"].Value);
        }

        private static void PrueftKeyDownWeg(string quelle, string klasse)
        {
            Match anhaengen = Regex.Match(quelle, @"_web\.WebView\.KeyDown\s*\+=\s*(?<name>\w+)\s*;");
            Assert.True(anhaengen.Success,
                klasse + " haengt keinen Handler an _web.WebView.KeyDown - bei Fokus im Browserfenster " +
                "der WebView2 erreicht die Taste ProcessCmdKey nie.");

            string methode = Methode(quelle, anhaengen.Groups["name"].Value);
            Assert.Matches(@"Befehlstaste\s*\(\s*e\.KeyData\s*\)", methode);
            Assert.Matches(@"e\.Handled\s*=\s*true", methode);
        }

        /// <summary>Der Rumpf der ersten Methode dieses Namens (Klammern gezählt).</summary>
        private static string Methode(string quelle, string name)
        {
            Match kopf = Regex.Match(quelle, @"\b(?:void|bool)\s+" + Regex.Escape(name) + @"\s*\(");
            Assert.True(kopf.Success, "Methode " + name + " nicht gefunden.");
            int beginn = quelle.IndexOf('{', kopf.Index);
            int tiefe = 0;
            for (int i = beginn; i < quelle.Length; i++)
            {
                if (quelle[i] == '{') tiefe++;
                else if (quelle[i] == '}' && --tiefe == 0) return quelle.Substring(beginn, i - beginn + 1);
            }
            throw new InvalidOperationException("Rumpf von " + name + " ist nicht geschlossen.");
        }

        private static string Lies(string relativ)
            => File.ReadAllText(Path.Combine(Arbeitsbaum(), Path.Combine(relativ.Split('/'))));

        /// <summary>
        /// Die Wurzel des Arbeitsbaums über <see cref="CallerFilePathAttribute"/>, sonst vom
        /// Ausgabeordner aufwärts (wie die anderen Wächter).
        /// </summary>
        private static string Arbeitsbaum([CallerFilePath] string eigeneDatei = null)
        {
            if (!string.IsNullOrEmpty(eigeneDatei))
            {
                string ordner = Path.GetDirectoryName(eigeneDatei);
                string wurzel = ordner == null ? null : Path.GetDirectoryName(ordner);
                if (wurzel != null && File.Exists(Path.Combine(wurzel, "WP-Plan.sln"))) return wurzel;
            }

            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                if (File.Exists(Path.Combine(d.FullName, "WP-Plan.sln"))) return d.FullName;
                d = d.Parent;
            }
            throw new DirectoryNotFoundException("Repo-Wurzel (WP-Plan.sln) nicht gefunden.");
        }
    }
}
