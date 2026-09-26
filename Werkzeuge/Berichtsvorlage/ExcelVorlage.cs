using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using WindowsFormsApplication1;

namespace Berichtsvorlage
{
    /// <summary>
    /// <b>excel-ausfuehrlich</b> — die ausführliche Excel-Vorlage der Sprache (BV-E9): Der Kern baut sie aus dem Katalog
    /// (<see cref="ExcelAusfuehrlich.Erzeuge(bool, int)"/>), das Werkzeug prüft sie — <see cref="OpenXmlValidator"/> Office 2016
    /// und der Excel-Prüfer der Engine ohne Fehler — und schreibt sie nur, wenn sich der Inhalt geändert hat. Die Namen legen
    /// die Sprache fest wie beim Kurzbericht: <c>Berichtsvorlage_Excel_Ausfuehrlich.xlsx</c> deutsch, <c>…_en.xlsx</c> englisch.
    /// </summary>
    internal static class ExcelVorlage
    {
        /// <summary>Die Validatorfassung der Excel-Mappen (wie <c>Exceldiagrammbefund.Validierungsfehler</c> der Kerntests).</summary>
        private const FileFormatVersions FASSUNG = FileFormatVersions.Office2016;

        internal static int Ausfuehren(string ziel, bool englisch, int katalogfassung, TextWriter aus)
        {
            if (!string.Equals(Path.GetExtension(ziel), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("Das Ziel muss eine .xlsx-Datei sein.");
                return Program.DATEI;
            }
            string erwartet = BerichtsvorlagenCtrl.DateiExcelAusfuehrlich(englisch);
            string anders = BerichtsvorlagenCtrl.DateiExcelAusfuehrlich(!englisch);
            string name = Path.GetFileName(ziel);
            if (string.Equals(name, anders, StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine(name + " entsteht nur mit --sprache " + (englisch ? "de" : "en") + " — erwartet ist " + erwartet + ".");
                return Program.AUFRUF;
            }

            aus.WriteLine("Ausführliche Excel-Vorlage (" + (englisch ? "en" : "de") + ", Katalogfassung " + katalogfassung + "): " + ziel);
            byte[] bytes = ExcelAusfuehrlich.Erzeuge(englisch, katalogfassung);

            List<string> fehler = Validieren(bytes);
            foreach (string f in fehler.Take(10)) Console.Error.WriteLine("  Validator: " + f);
            Pruefbefund befund = ExcelVorlagenpruefer.Pruefe(bytes, Pruefstufe.Voll,
                new Pruefkontext { Englisch = englisch, Dateiname = name, Ausgabe = Vorlagenausgabe.Excel });
            foreach (Pruefmeldung m in befund.Meldungen)
                aus.WriteLine("  Prüfer " + m.Stufe + ": " + m.Text);
            if (fehler.Count > 0 || befund.HatFehler)
            {
                Console.Error.WriteLine("Die ausführliche Excel-Vorlage ist nicht in Ordnung — das Ziel bleibt unverändert.");
                return Program.PRUEFUNG;
            }
            return Schreibe(ziel, bytes, aus);
        }

        /// <summary>Schreibt die Bytes, wenn der Inhalt vom Ziel abweicht (Vergleich ohne Zeitstempel und Zufallskennungen).</summary>
        internal static int Schreibe(string ziel, byte[] bytes, TextWriter aus)
        {
            if (File.Exists(ziel)
                && string.Equals(BerichtsvorlagenCtrl.Inhaltsschluessel(File.ReadAllBytes(ziel)), BerichtsvorlagenCtrl.Inhaltsschluessel(bytes),
                                 StringComparison.Ordinal))
            {
                aus.WriteLine("  unverändert — nichts zu tun.");
                return Program.OK;
            }
            string ordner = Path.GetDirectoryName(Path.GetFullPath(ziel));
            if (!string.IsNullOrEmpty(ordner)) Directory.CreateDirectory(ordner);
            File.WriteAllBytes(ziel, bytes);
            aus.WriteLine("  geschrieben (" + bytes.Length + " Byte).");
            return Program.OK;
        }

        /// <summary>Die Befunde des Validators.</summary>
        internal static List<string> Validieren(byte[] bytes)
        {
            using (var strom = new MemoryStream(bytes, false))
            using (SpreadsheetDocument doc = SpreadsheetDocument.Open(strom, false))
                return new OpenXmlValidator(FASSUNG).Validate(doc)
                    .Select(f => f.Description + " @ " + f.Path?.XPath + " (" + f.Part?.Uri + ")").ToList();
        }
    }

    /// <summary>Ein Schritt des Sammelbefehls: die Datei im Vorlagenordner und wie sie aus der Stilvorlage entsteht.</summary>
    internal sealed class Sammelschritt
    {
        internal Sammelschritt(string datei, Func<string, string, TextWriter, int> lauf)
        {
            Datei = datei;
            Lauf = lauf;
        }

        /// <summary>Der Dateiname im Vorlagenordner.</summary>
        internal string Datei { get; }

        /// <summary>Erzeugt die Datei: Stilvorlage, Ziel, Ausgabe → Rückgabewert des Modus.</summary>
        internal Func<string, string, TextWriter, int> Lauf { get; }
    }

    /// <summary>
    /// <b>alle &lt;vorlagenordner&gt;</b> — der Sammelbefehl (BV-E9): erzeugt jede mitgelieferte Vorlage aus dem aktuellen Katalog
    /// neu — Beispiel- und Standardvorlage, Kurzbericht und ausführliche Vorlage je Sprache, die ausführliche Excel-Vorlage je
    /// Sprache —, nachdem die Stilvorlage <c>Berichtsvorlage.docx</c> bereinigt ist (ein zweiter Lauf findet nichts). Jeder
    /// Schritt schreibt nur bei grüner Prüfung und nur bei geändertem Inhalt; zweimal gezogen ist der Ordner inhaltsgleich. Die
    /// Aktualitätswache <c>EPOS.Kern.Tests/AuslieferungsvorlagenAktualitaetWacheTests</c> zieht dieselben <see cref="Schritte"/>.
    /// </summary>
    internal static class Sammellauf
    {
        /// <summary>Die Stilvorlage im Vorlagenordner — Quelle aller Word-Vorlagen.</summary>
        internal const string STILVORLAGE = "Berichtsvorlage.docx";

        /// <summary>
        /// Die Schritte in ihrer Folge. <b>Erweiterungsstelle:</b> Weitere mitgelieferte Vorlagen (etwa die <c>.dotx</c> mit den
        /// Schnellbausteinen) kommen hier als eigener Schritt hinzu — dann erzeugt sie auch <c>alle</c>, und die Aktualitätswache
        /// hält sie.
        /// </summary>
        internal static IReadOnlyList<Sammelschritt> Schritte()
        {
            int word = Vorlagenfeldkatalog.KatalogfassungWord;
            int excel = Vorlagenfeldkatalog.KATALOGFASSUNG;
            return new List<Sammelschritt>
            {
                new Sammelschritt("Berichtsvorlage_Beispiel.docx", (q, z, a) => Beispielvorlage.Ausfuehren(q, z, Vorlagenart.Beispiel, word, a)),
                new Sammelschritt(BerichtsvorlagenCtrl.DATEI_STANDARD, (q, z, a) => Beispielvorlage.Ausfuehren(q, z, Vorlagenart.Standard, word, a)),
                new Sammelschritt(BerichtsvorlagenCtrl.DATEI_KURZBERICHT, (q, z, a) => Kurzbericht.Ausfuehren(q, z, false, word, a)),
                new Sammelschritt(BerichtsvorlagenCtrl.DATEI_KURZBERICHT_EN, (q, z, a) => Kurzbericht.Ausfuehren(q, z, true, word, a)),
                new Sammelschritt(BerichtsvorlagenCtrl.DATEI_AUSFUEHRLICH, (q, z, a) => Ausfuehrlich.Ausfuehren(q, z, false, word, a)),
                new Sammelschritt(BerichtsvorlagenCtrl.DATEI_AUSFUEHRLICH_EN, (q, z, a) => Ausfuehrlich.Ausfuehren(q, z, true, word, a)),
                new Sammelschritt(BerichtsvorlagenCtrl.DATEI_EXCEL_AUSFUEHRLICH, (q, z, a) => ExcelVorlage.Ausfuehren(z, false, excel, a)),
                new Sammelschritt(BerichtsvorlagenCtrl.DATEI_EXCEL_AUSFUEHRLICH_EN, (q, z, a) => ExcelVorlage.Ausfuehren(z, true, excel, a)),
                // Erweiterungsstelle (BV-E9, .dotx): new Sammelschritt("Berichtsvorlage_….dotx", (q, z, a) => …),
            };
        }

        /// <summary>Zieht alle Schritte im Vorlagenordner; der erste rote Schritt beendet den Lauf mit seinem Rückgabewert.</summary>
        internal static int Ausfuehren(string ordner, TextWriter aus)
        {
            string stil = Path.Combine(ordner, STILVORLAGE);
            if (!File.Exists(stil))
            {
                Console.Error.WriteLine("Stilvorlage nicht gefunden: " + stil);
                return Program.DATEI;
            }
            int rc = Stilbereinigung.Ausfuehren(stil, aus);
            if (rc != Program.OK) return rc;
            foreach (Sammelschritt s in Schritte())
            {
                aus.WriteLine();
                rc = s.Lauf(stil, Path.Combine(ordner, s.Datei), aus);
                if (rc != Program.OK)
                {
                    Console.Error.WriteLine("Schritt " + s.Datei + " rot (" + rc + ") — der Sammellauf bricht ab.");
                    return rc;
                }
            }
            aus.WriteLine();
            aus.WriteLine("Alle " + Schritte().Count + " mitgelieferten Vorlagen sind aktuell.");
            return Program.OK;
        }
    }
}
