using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Wächter über das PROGRAMMSYMBOL der Windows-Anwendung (Auftrag #229,
    /// Anwenderwunsch 11.09.2026: „nehme das EPOS-ICON als Programm-Symbol (für
    /// taskleiste etc.)").
    ///
    /// <para><b>Die Regel, die er hält.</b> Drei Dinge müssen zusammenpassen, sonst
    /// zeigen Taskleiste, Alt-Tab und Explorer wieder das .NET-Standardsymbol:</para>
    /// <list type="number">
    ///   <item><c>WindowsFormsApplication1.csproj</c> trägt ein
    ///   <c>&lt;ApplicationIcon&gt;</c>.</item>
    ///   <item>Die dort genannte Datei liegt tatsächlich im Arbeitsbaum und ist ein
    ///   GÜLTIGES ICO mit mindestens den Größen 16, 32 und 48 px.</item>
    ///   <item><c>Setup/EPOS-Plan.iss</c> nennt für <c>SetupIconFile</c> DIESELBE
    ///   Datei — eine Quelle, kein zweites Bild, das beim nächsten Icon-Wechsel
    ///   unbemerkt auseinanderläuft.</item>
    /// </list>
    ///
    /// <para>Dieses Projekt kann <c>WindowsFormsApplication1</c> nicht referenzieren
    /// (net10.0 ohne <c>-windows</c>, siehe Klassenkopf der <c>.csproj</c>) — der
    /// Wächter liest deshalb, wie die anderen Wächter dieses Ordners, den QUELLTEXT
    /// der `.csproj` und der `.iss`-Datei und die ICO-Bytes selbst, ohne zu bauen.
    /// </para>
    /// </summary>
    public class ProgrammsymbolWacheTests
    {
        private const string CsprojRelativpfad = "WindowsFormsApplication1/WindowsFormsApplication1.csproj";
        private const string IssRelativpfad = "Setup/EPOS-Plan.iss";

        // =====================================================================
        //  1) <ApplicationIcon> ist gesetzt
        // =====================================================================

        [Fact]
        public void Das_csproj_der_Windows_Anwendung_setzt_ApplicationIcon()
        {
            string inhalt = CsprojInhalt();

            Match treffer = Regex.Match(inhalt, @"<ApplicationIcon>\s*(?<pfad>[^<]+?)\s*</ApplicationIcon>");

            Assert.True(treffer.Success,
                "WindowsFormsApplication1.csproj setzt kein <ApplicationIcon> - " +
                "die Exe traegt dann das .NET-Standardsymbol (Auftrag #229).");
            Assert.False(string.IsNullOrWhiteSpace(treffer.Groups["pfad"].Value),
                "<ApplicationIcon> steht leer im csproj.");
        }

        // =====================================================================
        //  2) Die genannte Datei liegt vor und ist ein gueltiges, mehrstufiges ICO
        // =====================================================================

        [Fact]
        public void Die_ApplicationIcon_Datei_liegt_vor_und_ist_ein_gueltiges_ICO_mit_16_32_48_px()
        {
            string relativ = ApplicationIconRelativpfad();
            string absolut = AufWindowsAnwendung(relativ);

            Assert.True(File.Exists(absolut), "Programmsymbol nicht gefunden: " + absolut);

            byte[] bytes = File.ReadAllBytes(absolut);
            int[] groessen = IcoGroessen(bytes, absolut);

            foreach (int erwartet in new[] { 16, 32, 48 })
            {
                Assert.True(groessen.Contains(erwartet),
                    $"Das ICO {absolut} führt keine {erwartet}x{erwartet}-Stufe " +
                    $"(vorhanden: {string.Join(", ", groessen)}).");
            }
        }

        /// <summary>
        /// Liest den ICONDIR-Kopf (6 Byte) und je Eintrag die ersten zwei Byte
        /// (Breite/Höhe, 0 bedeutet 256 px nach der ICO-Spezifikation) — ohne die
        /// eingebetteten PNG- oder BMP-Bilder selbst zu entpacken, das genügt für
        /// die Größenliste.
        /// </summary>
        private static int[] IcoGroessen(byte[] bytes, string pfad)
        {
            Assert.True(bytes.Length >= 6, "Datei zu kurz für einen ICO-Kopf: " + pfad);

            ushort reserviert = BitConverter.ToUInt16(bytes, 0);
            ushort typ = BitConverter.ToUInt16(bytes, 2);
            ushort anzahl = BitConverter.ToUInt16(bytes, 4);

            Assert.True(reserviert == 0 && typ == 1,
                $"Kein gültiger ICO-Kopf (reserviert={reserviert}, typ={typ}) in {pfad} " +
                "- erwartet reserviert=0, typ=1 (Icon, kein Cursor).");
            Assert.True(anzahl > 0, "ICO ohne ein einziges Bild: " + pfad);
            Assert.True(bytes.Length >= 6 + anzahl * 16,
                "ICO-Verzeichnis reicht über das Dateiende hinaus: " + pfad);

            int[] groessen = new int[anzahl];
            for (int i = 0; i < anzahl; i++)
            {
                int basis = 6 + i * 16;
                int breite = bytes[basis];
                int hoehe = bytes[basis + 1];
                Assert.True(breite == hoehe,
                    $"Stufe {i} ist nicht quadratisch ({breite}x{hoehe}) in {pfad}.");
                groessen[i] = breite == 0 ? 256 : breite;
            }

            return groessen;
        }

        // =====================================================================
        //  3) Setup und Anwendung meinen DIESELBE Datei
        // =====================================================================

        [Fact]
        public void Setup_und_Anwendung_nennen_dasselbe_Programmsymbol()
        {
            string ausAnwendung = Normalisiert(ApplicationIconRelativpfad());
            // Das csproj-Icon liegt relativ zu WindowsFormsApplication1/ - fuer den
            // Vergleich mit dem Setup-Pfad (relativ zur Repo-Wurzel) den Ordner voranstellen.
            string ausAnwendungAbRepo = Normalisiert("WindowsFormsApplication1/" + ausAnwendung);

            string issInhalt = IssInhalt();
            Match treffer = Regex.Match(issInhalt,
                @"SetupIconFile\s*=\s*\{#(?:RepoDir|SetupDir)\}(?<pfad>[^\r\n]+)");

            Assert.True(treffer.Success,
                "Setup/EPOS-Plan.iss setzt kein SetupIconFile - der Installer traegt dann " +
                "das Inno-Setup-Standardsymbol statt des EPOS-Symbols.");

            string ausSetup = Normalisiert(treffer.Groups["pfad"].Value);

            Assert.Equal(ausAnwendungAbRepo, ausSetup);
        }

        // =====================================================================
        //  Hilfsmittel
        // =====================================================================

        private static string ApplicationIconRelativpfad()
        {
            Match treffer = Regex.Match(CsprojInhalt(),
                @"<ApplicationIcon>\s*(?<pfad>[^<]+?)\s*</ApplicationIcon>");
            Assert.True(treffer.Success, "<ApplicationIcon> nicht gefunden.");
            return treffer.Groups["pfad"].Value;
        }

        private static string Normalisiert(string pfad)
            => pfad.Trim().Replace('\\', '/').TrimStart('/');

        private static string CsprojInhalt()
            => File.ReadAllText(Path.Combine(Arbeitsbaum(), Auf(CsprojRelativpfad)));

        private static string IssInhalt()
            => File.ReadAllText(Path.Combine(Arbeitsbaum(), Auf(IssRelativpfad)));

        /// <summary>Löst einen mit "/" geschriebenen Repo-Pfad plattformgerecht auf.</summary>
        private static string Auf(string relativMitSchraegstrich)
            => Path.Combine(relativMitSchraegstrich.Split('/'));

        private static string AufWindowsAnwendung(string relativZurAnwendung)
            => Path.Combine(Arbeitsbaum(), "WindowsFormsApplication1",
                             Path.Combine(relativZurAnwendung.Split('\\')));

        /// <summary>
        /// Die Wurzel des Arbeitsbaums. Der Weg dorthin führt über
        /// <see cref="CallerFilePathAttribute"/> — die Datei kennt ihren eigenen Ort.
        /// Steht der Quelltext nicht dort (ein Lauf aus verschobenen Binärdateien), wird
        /// wie in den anderen Wächtern vom Ausgabeordner aufwärts gesucht.
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

            Assert.Fail("Die Wurzel des Arbeitsbaums (WP-Plan.sln) ist nicht zu finden.");
            return null;
        }
    }
}
