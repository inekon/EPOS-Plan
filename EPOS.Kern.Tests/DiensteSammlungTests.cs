using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Der Wächter über die Testsammlung der Dienste-Tauscher (Befund iU5‑O‑1).
    ///
    /// <para><b>Was schiefging.</b> Die Windows-CI meldete am 06.09.2026 (Lauf
    /// 34018913888 auf <c>002c937</c>) einen roten Fall in
    /// <c>DiensteTests.Dialogdienst_ist_austauschbar_und_traegt_die_Meldehaken</c>:
    /// Erwartet war „Meldung|mit Titel|Titel", in der Mitschrift stand
    /// „Meldung|Fehler beim Laden der Daten: Type…". Der nächste Lauf war grün — ein
    /// Wettlauf, kein Rechenfehler. Ursache: <c>Dienste.Dialog</c> ist PROZESSWEIT.
    /// Während der Fall seine eigene Mitschrift eingelegt hatte, lief in einer ANDEREN
    /// Sammlung ein Datenbanktest in den Fehlerpfad von
    /// <c>SqliteDatenzugriff.GetDataTable</c>, und dessen
    /// <c>DataRepository.FehlerMelden</c> schrieb in die fremde Mitschrift.</para>
    ///
    /// <para><b>Warum eine EIGENE Sammlung nicht half.</b> xunit trennt nur INNERHALB
    /// einer Sammlung; zwei VERSCHIEDENE Sammlungen laufen immer nebeneinander. Die
    /// Tauscher standen in drei Gruppen — „Dienste", „Testdatenbank" und (ohne Angabe)
    /// je Klasse eine eigene. Jede Gruppe konnte jede andere stören, und nicht nur beim
    /// Dialog: Getauscht werden Dialog, Einstellungen, Projekt, Datei, GeräteId,
    /// Lizenzablage, Navigation, Pfade und Sprache.</para>
    ///
    /// <para><b>Die Regel.</b> Es gibt EINE serielle Sammlung, und das ist
    /// <c>[Collection("Testdatenbank")]</c> — dieselbe, in der alles steht, was über
    /// <c>FehlerMelden</c> melden kann. Wer in <c>EPOS.Kern.Tests</c> ein
    /// <c>Dienste.*</c> tauscht, trägt dieses Attribut. Nicht mehr genügt es, sich nur
    /// von den anderen Tauschern abzugrenzen: Der Störer muss selbst kein Tauscher
    /// sein.</para>
    ///
    /// <para>Der Wächter liest die Quelldateien dieses Projekts — nicht die Metadaten
    /// der Assembly. Ein vergessenes Attribut soll auffallen, BEVOR jemand den
    /// flatterhaften Lauf sucht.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public class DiensteSammlungTests
    {
        /// <summary>
        /// Eine Zuweisung an ein Feld von <c>Dienste</c> — <c>Dienste.Dialog = …</c>.
        /// Das <c>(?!=)</c> hält den Vergleich <c>Dienste.Dialog == …</c> heraus.
        /// </summary>
        private static readonly Regex Tausch =
            new Regex(@"\bDienste\.[A-Za-zÄÖÜäöüß]+\s*=(?!=)", RegexOptions.Compiled);

        /// <summary>Das Attribut, das die serielle Sammlung nennt.</summary>
        private const string Sammlung = "[Collection(\"Testdatenbank\")]";

        // =====================================================================
        //  Der Wächter
        // =====================================================================

        [Fact]
        public void Jede_Datei_die_einen_Dienst_tauscht_steht_in_der_seriellen_Sammlung()
        {
            var funde = new List<string>();

            foreach (string datei in Quelldateien())
            {
                string quelltext = File.ReadAllText(datei);
                if (quelltext.Contains(Sammlung, StringComparison.Ordinal)) continue;

                string[] zeilen = quelltext.Replace("\r\n", "\n").Split('\n');
                for (int i = 0; i < zeilen.Length; i++)
                {
                    if (!Tausch.IsMatch(zeilen[i])) continue;
                    if (IstKommentar(zeilen[i])) continue;
                    funde.Add(Path.GetFileName(datei) + ":" + (i + 1) + "  " + zeilen[i].Trim());
                }
            }

            Assert.True(funde.Count == 0,
                "Diese Stellen tauschen ein prozessweites Dienste.* , ohne dass ihre Datei " +
                Sammlung + " trägt (Befund iU5‑O‑1). xunit fährt zwei verschiedene Sammlungen " +
                "IMMER nebeneinander; der Tausch wirkt dann in fremde Tests hinein.\n" +
                string.Join("\n", funde));
        }

        /// <summary>
        /// <b>Gegenprobe:</b> Der Leser findet das Muster wirklich — sonst wäre der Fall
        /// oben stumm und niemand merkte es. Geprüft wird beides: dass die Zuweisung
        /// trifft und dass der Vergleich es nicht tut.
        /// </summary>
        [Fact]
        public void Der_Leser_erkennt_das_Muster()
        {
            Assert.Matches(Tausch, "                Dienste.Dialog = mitschrift;");
            Assert.Matches(Tausch, "            finally { Dienste.Einstellungen = vorher; }");
            Assert.Matches(Tausch, "Dienste.GeraeteId=neu;");

            Assert.DoesNotMatch(Tausch, "            Assert.Same(vorher, Dienste.Dialog);");
            Assert.DoesNotMatch(Tausch, "            if (Dienste.Projekt == null) return;");
            Assert.DoesNotMatch(Tausch, "            var x = Dienste.Sprache.Kuerzel;");
        }

        /// <summary>
        /// <b>Gegenprobe zur Kommentarschonung:</b> Die Klassenköpfe ERKLÄREN die Regel
        /// und nennen dabei das Muster — das darf den Wächter nicht auslösen.
        /// </summary>
        [Fact]
        public void Ein_Kommentar_zaehlt_nicht_als_Tausch()
        {
            Assert.True(IstKommentar("        // Dienste.Dialog = neu;"));
            Assert.True(IstKommentar("    /// <para>… <c>Dienste.Einstellungen = x</c> …</para>"));
            Assert.True(IstKommentar("     * Dienste.Pfade = alt;"));
            Assert.False(IstKommentar("        Dienste.Dialog = neu;"));
        }

        /// <summary>
        /// <b>Gegenprobe zum Bestand:</b> Der Wächter läuft über wirklich vorhandene
        /// Dateien und findet dort auch wirklich Tauscher — ein leerer Bestand liefe
        /// sonst grün durch, ohne je etwas geprüft zu haben.
        /// </summary>
        [Fact]
        public void Der_Waechter_sieht_den_Bestand()
        {
            string[] dateien = Quelldateien();
            Assert.True(dateien.Length > 50, "Nur " + dateien.Length + " Quelldateien gefunden.");

            int tauscher = dateien.Count(d => File.ReadAllText(d)
                                                  .Replace("\r\n", "\n").Split('\n')
                                                  .Any(z => Tausch.IsMatch(z) && !IstKommentar(z)));
            Assert.True(tauscher >= 8, "Nur " + tauscher + " Tauscherdateien gefunden (erwartet: mindestens 8).");
        }

        // =====================================================================
        //  Hilfen
        // =====================================================================

        /// <summary>
        /// Steht die Zeile in einem Kommentar? Die Klassenköpfe nennen das Muster
        /// absichtlich; geprüft wird nur, was der Übersetzer sieht.
        /// </summary>
        private static bool IstKommentar(string zeile)
        {
            string s = zeile.TrimStart();
            return s.StartsWith("//", StringComparison.Ordinal)
                || s.StartsWith("*", StringComparison.Ordinal)
                || s.StartsWith("/*", StringComparison.Ordinal);
        }

        /// <summary>
        /// Alle <c>.cs</c>-Dateien DIESES Testprojekts.
        ///
        /// <para>Der Weg dorthin führt über <see cref="CallerFilePathAttribute"/> — die
        /// Datei kennt ihren eigenen Ort. Steht der Quelltext nicht dort (ein Lauf aus
        /// verschobenen Binärdateien), wird wie in den Wächtern von <c>EPOS.UI.Tests</c>
        /// vom Ausgabeordner aufwärts gesucht, bis <c>TestDatenbank.cs</c> auftaucht.</para>
        /// </summary>
        private static string[] Quelldateien()
        {
            string ordner = EigenerOrdner();
            if (!Directory.Exists(ordner)) ordner = OrdnerAusDemArbeitsbaum();

            Assert.True(ordner != null && Directory.Exists(ordner),
                        "Die Quelldateien von EPOS.Kern.Tests sind nicht zu finden.");

            return Directory.GetFiles(ordner, "*.cs", SearchOption.AllDirectories)
                            .Where(p => p.IndexOf(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar,
                                                  StringComparison.Ordinal) < 0
                                     && p.IndexOf(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar,
                                                  StringComparison.Ordinal) < 0)
                            .OrderBy(p => p, StringComparer.Ordinal)
                            .ToArray();
        }

        private static string EigenerOrdner([CallerFilePath] string eigeneDatei = null)
        {
            return string.IsNullOrEmpty(eigeneDatei) ? null : Path.GetDirectoryName(eigeneDatei);
        }

        private static string OrdnerAusDemArbeitsbaum()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null)
            {
                string kandidat = Path.Combine(d.FullName, "EPOS.Kern.Tests");
                if (File.Exists(Path.Combine(kandidat, "TestDatenbank.cs"))) return kandidat;
                d = d.Parent;
            }
            return null;
        }
    }
}
