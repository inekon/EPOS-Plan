using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using DocumentFormat.OpenXml.Packaging;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Wache über die ausgelieferten Word-Vorlagen</b> (Konzept Berichtsvorlagen 6.3, 8.4 und
    /// 12, Zeile „Auslieferung, Werkzeug“; Etappe BV-E1): Jede mitgelieferte Vorlage liegt als Datei
    /// vor, besteht den Validator und steht in <b>beiden Lieferwegen</b> — unter Windows als
    /// <c>None</c>-Eintrag in <c>WindowsFormsApplication1.csproj</c>, der sie mit <c>Link</c> und
    /// <c>TargetPath</c> <c>Vorlagen\&lt;Datei&gt;</c> neben die EXE (<c>{app}\Vorlagen</c>) kopiert,
    /// auf iOS als <c>MauiAsset</c> in <c>EPOS.iOS/EPOS.iOS.csproj</c> mit <c>LogicalName</c>
    /// <c>Vorlagen\&lt;Datei&gt;</c> im App-Bundle.
    ///
    /// <para><b>Welche Dateien.</b> Ausgeliefert werden die Stilvorlage <c>Berichtsvorlage.docx</c>
    /// — Rückfall des Codes und Quelle der Bereinigung; sie bleibt ohne Übernahmeschritt und ohne
    /// <c>[InstallDelete]</c> (Entscheid BV-E1-1) — und die Standardvorlage
    /// <c>Berichtsvorlage_Standard.docx</c>: Kapitel für Kapitel mit Kapitelkopf und
    /// <c>{{kapitel.&lt;name&gt;}}</c>, erzeugt wie die Beispielvorlage, aber ohne Kommentare (Werkzeug,
    /// <c>beispiel --standard</c>; BV-E2), dazu der Kurzbericht je Sprache <c>Berichtsvorlage_Kurzbericht.docx</c> und
    /// <c>Berichtsvorlage_Kurzbericht_en.docx</c> (BV-E5, Werkzeug <c>kurzbericht</c>; nur als Kopie über „Neue Vorlage…“
    /// wählbar, <c>BerichtsvorlagenCtrl.Musterpfad</c> findet ihn im selben Ordner) und ebenso die ausführliche Vorlage je
    /// Sprache <c>Berichtsvorlage_Ausfuehrlich.docx</c> und <c>Berichtsvorlage_Ausfuehrlich_en.docx</c> (BV-E8-4, Werkzeug
    /// <c>ausfuehrlich</c>), die Bausteinvorlage je Sprache <c>Berichtsvorlage_Bausteine.dotx</c> und
    /// <c>Berichtsvorlage_Bausteine_en.dotx</c> (BV-E9, Werkzeug <c>bausteine</c>; eine Dokumentvorlage mit jedem Platzhalter als
    /// Schnellbaustein, im Musterordner) und die ausführliche Excel-Vorlage je Sprache
    /// <c>Berichtsvorlage_Excel_Ausfuehrlich.xlsx</c> und <c>…_en.xlsx</c> (BV-E9, Werkzeug <c>excel-ausfuehrlich</c>; in der
    /// Zeile „Excel-Vorlage“ direkt wählbar). Die Beispielvorlage <c>Berichtsvorlage_Beispiel.docx</c> ist
    /// Anschauung und steht in keinem Lieferweg. Jede Vorlage im Vorlagenordner steht in einer der beiden
    /// Listen: Eine neue Vorlage wird bewusst ausgeliefert oder bewusst nicht.</para>
    ///
    /// <para><b>Warum die Projektdateien gelesen werden.</b> Der Kern-Filter baut keine der beiden
    /// Schalen, die iOS-Schale baut nur auf macOS: Ein vergessener <c>MauiAsset</c> fiele erst auf
    /// dem Gerät auf, wenn der Bericht seine Vorlage nicht findet. Die Wache liest beide
    /// Projektdateien als XML, ohne zu bauen (wie <see cref="ProgrammsymbolWacheTests"/>);
    /// Kommentare zählen nicht. Stilregeln und Platzhalter der Dateien hält
    /// <see cref="BerichtsvorlageDateiWacheTests"/>.</para>
    /// </summary>
    public class AuslieferungsvorlagenWacheTests
    {
        private const string WINDOWS_PROJEKT = "WindowsFormsApplication1/WindowsFormsApplication1.csproj";
        private const string IOS_PROJEKT = "EPOS.iOS/EPOS.iOS.csproj";

        /// <summary>Die Quelle einer Vorlage im Windows-Projekt (relativ zu dessen Ordner).</summary>
        private const string QUELLE_WINDOWS = @"Allgemein\Bericht\Vorlagen\";

        /// <summary>Die Quelle einer Vorlage im iOS-Projekt (relativ zu dessen Ordner).</summary>
        private const string QUELLE_IOS = @"..\WindowsFormsApplication1\Allgemein\Bericht\Vorlagen\";

        /// <summary>
        /// Das Ziel in beiden Lieferwegen: der Unterordner <c>Vorlagen</c> neben der EXE bzw. im
        /// App-Bundle — dort sucht <c>WordBerichtGenerator.FindeVorlage</c>.
        /// </summary>
        private const string ZIEL = @"Vorlagen\";

        /// <summary>Die ausgelieferten Vorlagen.</summary>
        internal static readonly string[] Ausgeliefert =
        {
            BerichtsvorlageDateiWacheTests.STILVORLAGE,
            BerichtsvorlageDateiWacheTests.STANDARD,
            BerichtsvorlageDateiWacheTests.KURZBERICHT,
            BerichtsvorlageDateiWacheTests.KURZBERICHT_EN,
            BerichtsvorlageDateiWacheTests.AUSFUEHRLICH,
            BerichtsvorlageDateiWacheTests.AUSFUEHRLICH_EN,
            BerichtsvorlageDateiWacheTests.BAUSTEINE,
            BerichtsvorlageDateiWacheTests.BAUSTEINE_EN,
            WindowsFormsApplication1.BerichtsvorlagenCtrl.DATEI_EXCEL_AUSFUEHRLICH,
            WindowsFormsApplication1.BerichtsvorlagenCtrl.DATEI_EXCEL_AUSFUEHRLICH_EN,
        };

        /// <summary>Die Vorlagen des Ordners, die bewusst nicht ausgeliefert werden.</summary>
        internal static readonly string[] NichtAusgeliefert =
        {
            BerichtsvorlageDateiWacheTests.BEISPIEL,
        };

        /// <summary>Was im Vorlagenordner als Vorlage zählt (Konzept 10.3, Zeile „Liste“, dazu <c>.docm</c>).</summary>
        private static readonly string[] Vorlagenendungen = { ".docx", ".dotx", ".docm", ".xlsx", ".xltx" };

        public static IEnumerable<object[]> AusgelieferteVorlagen()
            => Ausgeliefert.Select(datei => new object[] { datei });

        public static IEnumerable<object[]> Lieferwege()
        {
            yield return new object[] { WINDOWS_PROJEKT };
            yield return new object[] { IOS_PROJEKT };
        }

        // =====================================================================
        //  Die Dateien
        // =====================================================================

        /// <summary>
        /// Jede Vorlage im Ordner ist eingeordnet: ausgeliefert oder bewusst nicht. Offene
        /// Word-Sperrdateien (<c>~$…</c>) zählen nicht.
        /// </summary>
        [Fact]
        public void Jede_Vorlage_im_Ordner_ist_ausgeliefert_oder_bewusst_nicht()
        {
            string wurzel = Berichtsdatenproben.Repowurzel();
            if (wurzel == null) return;
            string ordner = Path.Combine(wurzel, BerichtsvorlageDateiWacheTests.ORDNER_REPO.Replace('/', Path.DirectorySeparatorChar));

            List<string> vorhanden = Directory.GetFiles(ordner)
                .Select(Path.GetFileName)
                .Where(n => !n.StartsWith("~$", StringComparison.Ordinal)
                            && Vorlagenendungen.Contains(Path.GetExtension(n), StringComparer.OrdinalIgnoreCase))
                .ToList();
            List<string> eingeordnet = Ausgeliefert.Concat(NichtAusgeliefert).ToList();

            List<string> fremd = vorhanden.Except(eingeordnet, StringComparer.Ordinal).ToList();
            List<string> fehlt = eingeordnet.Except(vorhanden, StringComparer.Ordinal).ToList();
            Assert.True(fremd.Count == 0 && fehlt.Count == 0,
                "Vorlagenordner " + BerichtsvorlageDateiWacheTests.ORDNER_REPO + ": nicht eingeordnet "
                + (fremd.Count == 0 ? "—" : string.Join(", ", fremd)) + "; eingeordnet, aber nicht vorhanden "
                + (fehlt.Count == 0 ? "—" : string.Join(", ", fehlt))
                + ". Eine neue Vorlage kommt in Ausgeliefert (und in beide Lieferwege) oder in NichtAusgeliefert.");
        }

        /// <summary>Jede ausgelieferte Vorlage liegt vor und ist gültiges OpenXML in jeder Fassung von Office 2007 bis 2021.</summary>
        [Theory]
        [MemberData(nameof(AusgelieferteVorlagen))]
        public void Jede_ausgelieferte_Vorlage_liegt_vor_und_besteht_den_Validator(string datei)
        {
            if (Path.GetExtension(datei) == ".xlsx")
            {
                // BV-E9: eine Excel-Vorlage — derselbe Validator wie für die Berichtsmappen (Office 2016).
                string wurzel = Berichtsdatenproben.Repowurzel();
                if (wurzel == null) return;
                string pfad = Path.Combine(wurzel, BerichtsvorlageDateiWacheTests.ORDNER_REPO.Replace('/', Path.DirectorySeparatorChar), datei);
                Assert.True(File.Exists(pfad), "Vorlage fehlt: " + pfad);
                List<string> befunde = Exceldiagrammbefund.Validierungsfehler(pfad);
                Assert.True(befunde.Count == 0, datei + ": " + befunde.Count + " Fehler — " + string.Join(" | ", befunde.Take(5)));
                return;
            }
            using WordprocessingDocument doc = BerichtsvorlageDateiWacheTests.Oeffnen(datei);
            if (doc == null) return;

            List<string> fehler = BerichtsvorlageDateiWacheTests.Validatorfehler(doc);
            Assert.True(fehler.Count == 0, datei + ": " + fehler.Count + " Fehler — " + string.Join(" | ", fehler.Take(5)));
        }

        // =====================================================================
        //  Die Lieferwege
        // =====================================================================

        /// <summary>
        /// Windows: genau ein <c>None</c>-Eintrag je Vorlage, der sie in den Ausgabeordner
        /// kopiert, mit <c>Link</c> und <c>TargetPath</c> <c>Vorlagen\&lt;Datei&gt;</c> — so legt
        /// die Veröffentlichung sie nach <c>{app}\Vorlagen</c>, und das Setup nimmt sie mit.
        /// </summary>
        [Theory]
        [MemberData(nameof(AusgelieferteVorlagen))]
        public void Der_Windows_Lieferweg_fuehrt_jede_ausgelieferte_Vorlage(string datei)
        {
            XDocument projekt = Projekt(WINDOWS_PROJEKT);
            if (projekt == null) return;

            List<XElement> eintraege = Eintraege(projekt, "None")
                .Where(e => Quellen(e).Contains(QUELLE_WINDOWS + datei, StringComparer.Ordinal))
                .ToList();
            Assert.True(eintraege.Count == 1,
                WINDOWS_PROJEKT + ": " + eintraege.Count + " None-Einträge für " + QUELLE_WINDOWS + datei + ", erwartet genau einer.");

            XElement eintrag = eintraege[0];
            string kopieren = Metadatum(eintrag, "CopyToOutputDirectory");
            Assert.True(kopieren == "PreserveNewest" || kopieren == "Always",
                WINDOWS_PROJEKT + ": " + datei + " wird nicht in den Ausgabeordner kopiert (CopyToOutputDirectory „" + kopieren + "“).");
            Assert.Equal(ZIEL + datei, Metadatum(eintrag, "Link"));
            Assert.Equal(ZIEL + datei, Metadatum(eintrag, "TargetPath"));
        }

        /// <summary>
        /// iOS: genau ein <c>MauiAsset</c> je Vorlage mit <c>LogicalName</c>
        /// <c>Vorlagen\&lt;Datei&gt;</c> — derselbe Unterordner wie unter Windows.
        /// </summary>
        [Theory]
        [MemberData(nameof(AusgelieferteVorlagen))]
        public void Der_iOS_Lieferweg_fuehrt_jede_ausgelieferte_Vorlage(string datei)
        {
            XDocument projekt = Projekt(IOS_PROJEKT);
            if (projekt == null) return;

            List<XElement> eintraege = Eintraege(projekt, "MauiAsset")
                .Where(e => Quellen(e).Contains(QUELLE_IOS + datei, StringComparer.Ordinal))
                .ToList();
            Assert.True(eintraege.Count == 1,
                IOS_PROJEKT + ": " + eintraege.Count + " MauiAsset-Einträge für " + QUELLE_IOS + datei + ", erwartet genau einer.");
            Assert.Equal(ZIEL + datei, Metadatum(eintraege[0], "LogicalName"));
        }

        /// <summary>
        /// Die Beispielvorlage steht in keinem Lieferweg, und jeder Eintrag aus dem Vorlagenordner
        /// nennt eine ausgelieferte Vorlage ausdrücklich — ein Muster wie <c>Vorlagen\*.docx</c>
        /// nähme die Beispielvorlage still mit. <c>Remove</c> und Kommentare zählen nicht.
        /// </summary>
        [Theory]
        [MemberData(nameof(Lieferwege))]
        public void Kein_Lieferweg_fuehrt_die_Beispielvorlage(string projektdatei)
        {
            XDocument projekt = Projekt(projektdatei);
            if (projekt == null) return;

            foreach (XElement e in projekt.Descendants())
            {
                IEnumerable<string> werte = e.Attributes().Where(a => a.Name.LocalName != "Remove").Select(a => a.Value);
                if (!e.HasElements) werte = werte.Append(e.Value);
                foreach (string wert in werte)
                    foreach (string datei in NichtAusgeliefert)
                        Assert.False(wert.Contains(datei, StringComparison.OrdinalIgnoreCase),
                            projektdatei + ": <" + e.Name.LocalName + "> nennt " + datei
                            + " — die Beispielvorlage wird nicht ausgeliefert (Anschauung bis BV-E2).");

                foreach (string quelle in Quellen(e))
                {
                    string normiert = quelle.Replace('/', '\\');
                    if (!normiert.Contains(@"Bericht\Vorlagen\", StringComparison.OrdinalIgnoreCase)) continue;
                    string name = normiert.Substring(normiert.LastIndexOf('\\') + 1);
                    Assert.True(Ausgeliefert.Contains(name, StringComparer.Ordinal),
                        projektdatei + ": <" + e.Name.LocalName + "> „" + quelle + "“ nennt keine ausgelieferte Vorlage ("
                        + string.Join(", ", Ausgeliefert) + ").");
                }
            }
        }

        // =====================================================================
        //  Helfer
        // =====================================================================

        /// <summary>Eine Projektdatei als XML; <c>null</c> außerhalb des Repositoriums.</summary>
        private static XDocument Projekt(string relativ)
        {
            string wurzel = Berichtsdatenproben.Repowurzel();
            if (wurzel == null) return null;
            string pfad = Path.Combine(wurzel, relativ.Replace('/', Path.DirectorySeparatorChar));
            Assert.True(File.Exists(pfad), "Projektdatei fehlt: " + relativ);
            return XDocument.Load(pfad);
        }

        /// <summary>Die Einträge einer Art (<c>None</c>, <c>MauiAsset</c>), gleich in welchem Namensraum.</summary>
        private static IEnumerable<XElement> Eintraege(XDocument projekt, string art)
            => projekt.Descendants().Where(e => e.Name.LocalName == art);

        /// <summary>Die Quellen eines Eintrags: <c>Include</c> und <c>Update</c>, Listen am Semikolon getrennt.</summary>
        private static IEnumerable<string> Quellen(XElement e)
            => new[] { "Include", "Update" }
                .Select(name => (string)e.Attribute(name))
                .Where(wert => wert != null)
                .SelectMany(wert => wert.Split(';'))
                .Select(wert => wert.Trim())
                .Where(wert => wert.Length > 0);

        /// <summary>Ein Metadatum als Attribut oder als Kindelement — MSBuild erlaubt beides.</summary>
        private static string Metadatum(XElement e, string name)
            => ((string)e.Attribute(name) ?? e.Elements().FirstOrDefault(k => k.Name.LocalName == name)?.Value)?.Trim();
    }
}
