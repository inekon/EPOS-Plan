using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using WindowsFormsApplication1;

namespace Berichtsvorlage
{
    /// <summary>
    /// <b>bausteine &lt;quelle.docx&gt; &lt;ziel.dotx&gt; --sprache de|en</b> (Etappe BV-E9): baut aus der Standardvorlage die
    /// Bausteinvorlage der Sprache — eine Dokumentvorlage mit dem Rumpf der Quelle und jedem Platzhalter des Katalogs als
    /// Schnellbaustein. Den Inhalt erzeugt der Kern (<see cref="WordBausteinvorlage.Erzeuge"/>) — derselbe Weg, an dem die
    /// Wache die ausgelieferte Datei misst; das Werkzeug stempelt die Zeiten der Quelle (so schreibt jeder Lauf dieselben
    /// Bytes), prüft Validator und Glossar und ersetzt das Ziel erst, wenn alles grün ist.
    ///
    /// <para><b>Der einzige Modus mit Verweis auf <c>EPOS.Kern</c>:</b> Die Bausteine kommen aus dem Platzhalterkatalog, und
    /// den gibt es nur im Kern. Die übrigen Modi bleiben reine Paketarbeit.</para>
    /// </summary>
    internal static class Bausteine
    {
        internal static int Ausfuehren(string quelle, string ziel, bool englisch, TextWriter aus)
        {
            if (!File.Exists(quelle))
            {
                Console.Error.WriteLine("Quelle nicht gefunden: " + quelle);
                return Program.DATEI;
            }
            if (!string.Equals(Path.GetExtension(quelle), ".docx", StringComparison.OrdinalIgnoreCase)
                || !string.Equals(Path.GetExtension(ziel), ".dotx", StringComparison.OrdinalIgnoreCase))
            {
                Console.Error.WriteLine("Die Quelle muss eine .docx (die Standardvorlage), das Ziel eine .dotx sein.");
                return Program.DATEI;
            }
            string name = Path.GetFileName(ziel);
            if ((string.Equals(name, BerichtsvorlagenCtrl.DATEI_BAUSTEINE, StringComparison.OrdinalIgnoreCase) && englisch)
                || (string.Equals(name, BerichtsvorlagenCtrl.DATEI_BAUSTEINE_EN, StringComparison.OrdinalIgnoreCase) && !englisch))
            {
                Console.Error.WriteLine(name + " gehört zur anderen Sprache — --sprache " + (englisch ? "de" : "en") + " setzen.");
                return Program.AUFRUF;
            }

            aus.WriteLine("Bausteinvorlage (" + (englisch ? "en" : "de") + "): " + quelle + " → " + ziel);
            IReadOnlyList<Schnellbaustein> soll = WordBausteinvorlage.Bausteine(Vorlagenfeldkatalog.KatalogfassungWord);
            byte[] bytes = WordBausteinvorlage.Erzeuge(File.ReadAllBytes(quelle), englisch);

            string kopie = Path.Combine(Path.GetTempPath(), "berichtsvorlage_" + Guid.NewGuid().ToString("N") + ".dotx");
            try
            {
                File.WriteAllBytes(kopie, bytes);
                Beispielvorlage.Zeitstempel(kopie, quelle);

                bool gruen = true;
                using (WordprocessingDocument doc = WordprocessingDocument.Open(kopie, false))
                {
                    if (doc.DocumentType != WordprocessingDocumentType.Template)
                    {
                        Console.Error.WriteLine("  Die Datei ist keine Dokumentvorlage.");
                        gruen = false;
                    }
                    List<DocPart> teile = doc.MainDocumentPart?.GlossaryDocumentPart?.GlossaryDocument?.DocParts?
                        .Elements<DocPart>().ToList() ?? new List<DocPart>();
                    List<string> namen = teile.Select(t => t.DocPartProperties?.GetFirstChild<DocPartName>()?.Val?.Value).ToList();
                    if (teile.Count != soll.Count || !namen.SequenceEqual(soll.Select(b => b.Name), StringComparer.Ordinal))
                    {
                        Console.Error.WriteLine("  Glossar: " + teile.Count + " Bausteine, erwartet " + soll.Count + " in Katalogfolge.");
                        gruen = false;
                    }
                    foreach (IGrouping<Bausteinkategorie, Schnellbaustein> g in soll.GroupBy(b => b.Kategorie))
                        aus.WriteLine("    " + WordBausteinvorlage.Kategoriename(g.Key, englisch).PadRight(28) + g.Count());
                    aus.WriteLine("  Glossar: " + teile.Count + " Schnellbausteine (Galerie „Schnellbausteine“)");
                }
                aus.WriteLine("  Validator:");
                if (Pruefung.Validieren(kopie, aus, true) > 0) gruen = false;

                if (!gruen)
                {
                    Console.Error.WriteLine("Die Bausteinvorlage ist nicht in Ordnung — das Ziel bleibt unverändert.");
                    return Program.PRUEFUNG;
                }
                if (File.Exists(ziel) && File.ReadAllBytes(ziel).AsSpan().SequenceEqual(File.ReadAllBytes(kopie)))
                {
                    aus.WriteLine("  Unverändert: " + ziel);
                    return Program.OK;
                }
                File.Copy(kopie, ziel, true);
                aus.WriteLine("  Geschrieben: " + ziel);
                return Program.OK;
            }
            finally { Program.Loeschen(kopie); }
        }
    }
}
