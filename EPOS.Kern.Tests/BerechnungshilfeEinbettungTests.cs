using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die WACHE über die EINBETTUNG der Hilferubrik „Berechnung" — Paket
    /// <b>H13</b> (Anwenderwunsch vom 06.09.2026).
    ///
    /// <para><b>Worum es geht.</b> Die Seiten der Rubrik liegen als
    /// MediaWiki-Markup in <c>Allgemein/Hilfe/Berechnung/*.wiki</c> und kommen
    /// über ein GLOB in die Assembly (<c>EPOS.Kern.csproj</c>, LogicalName
    /// <c>EPOS.Kern.Hilfe.Berechnung.&lt;Datei&gt;</c>). Ein Glob ist bequem und
    /// still: Verrutscht der Ordner, ändert jemand den LogicalName oder legt eine
    /// Seite mit falscher Endung an, fällt das beim Übersetzen NICHT auf — die
    /// Ressource fehlt dann einfach, und jeder Leser bekommt <c>null</c>.</para>
    ///
    /// <para><b>Was hier gehalten wird.</b> Zu jeder Datei auf dem Datenträger gibt
    /// es eine Ressource mit genau diesem Namen, und ihr Inhalt ist Zeichen für
    /// Zeichen derselbe. Der Fall setzt KEINEN Lader voraus — er fragt die
    /// Assembly unmittelbar; ein Lader kann darauf aufbauen, muss es aber nicht.</para>
    ///
    /// <para>Die INHALTLICHE Prüfung der Seiten (Kopfblock, die sechs Abschnitte
    /// der Bauform, die Zuordnungen in <c>help_mapping.txt</c>) steht in
    /// <c>EPOS.UI.Tests/BerechnungshilfeTests</c> — dort, wo auch die Razor-Wirte
    /// der Infoknöpfe liegen.</para>
    ///
    /// <para>Keine Datenbank, keine <c>Dienste.*</c> — deshalb ohne Sammlung.</para>
    /// </summary>
    public class BerechnungshilfeEinbettungTests
    {
        /// <summary>Der Namensraum, unter dem das Glob die Seiten einbettet.</summary>
        private const string PRAEFIX = "EPOS.Kern.Hilfe.Berechnung.";

        /// <summary>
        /// Kleinste Zahl von Seiten, unter der der Leser als kaputt gilt. Teil B
        /// der Rubrik liefert sieben (Heizkessel, BHKW, Wärmepumpe,
        /// Pufferspeicher, Solarthermie, Photovoltaik, Stromspeicher).
        /// </summary>
        private const int MINDESTSEITEN = 7;

        [Fact]
        public void Der_Ordner_der_Rubrik_traegt_Seiten()
        {
            string[] dateien = Seitendateien();

            Assert.True(dateien.Length >= MINDESTSEITEN,
                        "Nur " + dateien.Length + " Seiten in " + Seitenordner() +
                        " gefunden (erwartet: mindestens " + MINDESTSEITEN + ").");
        }

        /// <summary>
        /// Jede Datei des Ordners steht als eingebettete Ressource in der Assembly —
        /// unter genau dem Namen, den das Glob verspricht.
        /// </summary>
        [Fact]
        public void Jede_Seite_steht_als_eingebettete_Ressource()
        {
            Assembly kern = typeof(WindowsFormsApplication1.SimulationPV).Assembly;
            string[] namen = kern.GetManifestResourceNames();

            var fehlend = Seitendateien()
                .Select(Path.GetFileName)
                .Where(d => !namen.Contains(PRAEFIX + d))
                .ToArray();

            Assert.True(fehlend.Length == 0,
                        "Diese Seiten sind nicht eingebettet:\n  " +
                        string.Join("\n  ", fehlend) +
                        "\nVorhanden sind:\n  " +
                        string.Join("\n  ", namen.Where(n => n.StartsWith(PRAEFIX, StringComparison.Ordinal))));
        }

        /// <summary>
        /// Der eingebettete Inhalt ist derselbe wie auf dem Datenträger. Sonst
        /// liefe ein Leser gegen einen Stand, den niemand mehr sieht.
        /// </summary>
        [Fact]
        public void Der_eingebettete_Inhalt_ist_der_Dateiinhalt()
        {
            Assembly kern = typeof(WindowsFormsApplication1.SimulationPV).Assembly;

            foreach (string datei in Seitendateien())
            {
                string name = PRAEFIX + Path.GetFileName(datei);

                using Stream strom = kern.GetManifestResourceStream(name);
                Assert.True(strom != null, "Die Ressource " + name + " gibt es nicht.");

                using var leser = new StreamReader(strom, new UTF8Encoding(false), true);
                string eingebettet = leser.ReadToEnd().Replace("\r\n", "\n");
                string aufPlatte = File.ReadAllText(datei).Replace("\r\n", "\n");

                Assert.Equal(aufPlatte, eingebettet);
            }
        }

        // =====================================================================
        //  Lesen
        // =====================================================================

        /// <summary>
        /// Die Seiten des Ordners. Dateien mit führendem <c>_</c> sind KEINE
        /// Wikiseiten (Bauform: <c>_Index.wiki</c>, <c>_Bezuege.wiki</c>) und
        /// bleiben hier trotzdem drin — eingebettet müssen sie ebenso sein.
        /// </summary>
        private static string[] Seitendateien()
        {
            string ordner = Seitenordner();
            if (!Directory.Exists(ordner)) return Array.Empty<string>();

            return Directory.GetFiles(ordner, "*.wiki", SearchOption.TopDirectoryOnly)
                            .OrderBy(p => p, StringComparer.Ordinal)
                            .ToArray();
        }

        private static string Seitenordner()
            => Path.Combine(Wurzel(), "EPOS.Kern", "Allgemein", "Hilfe", "Berechnung");

        /// <summary>
        /// Der Aufstieg zur Repowurzel — dasselbe Vorgehen wie in
        /// <c>DiensteSammlungTests.OrdnerAusDemArbeitsbaum</c>: vom Ausgabeordner
        /// aufwärts, bis eine bekannte Datei auftaucht.
        /// </summary>
        private static string Wurzel()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null &&
                   !File.Exists(Path.Combine(d.FullName, "EPOS.Kern", "EPOS.Kern.csproj")))
                d = d.Parent;

            Assert.True(d != null, "Die Repowurzel ist vom Ausgabeordner aus nicht zu finden.");
            return d.FullName;
        }
    }
}
