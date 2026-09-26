using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using SpeicherEngine;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>Was mit einer Musterdatei im Ordner <see cref="BerichtsvorlagenCtrl.ORDNER_MITGELIEFERT"/> geschah.</summary>
    public enum Musterzustand
    {
        /// <summary>Neu angelegt oder erneuert.</summary>
        Geschrieben,

        /// <summary>Inhalt gleich — nicht angefasst.</summary>
        Unveraendert,

        /// <summary>Die Quelle fehlt (mitgelieferte Datei nicht im Auslieferungsordner) — nichts geschrieben.</summary>
        QuelleFehlt,

        /// <summary>Schreiben oder Erzeugen schlug fehl — benannt in <see cref="Musterdatei.Meldung"/>.</summary>
        Fehler,
    }

    /// <summary>Eine Datei des Musterordners und was mit ihr geschah.</summary>
    public sealed class Musterdatei
    {
        internal Musterdatei(string datei, Musterzustand zustand, string meldung)
        {
            Datei = datei;
            Zustand = zustand;
            Meldung = meldung ?? "";
        }

        /// <summary>Der Dateiname im Musterordner.</summary>
        public string Datei { get; }

        /// <summary>Was geschah.</summary>
        public Musterzustand Zustand { get; }

        /// <summary>Die benannte Meldung bei <see cref="Musterzustand.QuelleFehlt"/> und <see cref="Musterzustand.Fehler"/>; sonst leer.</summary>
        public string Meldung { get; }

        /// <inheritdoc />
        public override string ToString() { return Datei + ": " + Zustand; }
    }

    /// <summary>
    /// Der Befund von <see cref="BerichtsvorlagenCtrl.MusterBereitstellen"/>: der Musterordner, je Datei ihr Zustand und
    /// die benannten Meldungen. Eine Ausnahme wirft die Bereitstellung nie — was misslingt, steht hier.
    /// </summary>
    public sealed class Musterbefund
    {
        internal Musterbefund(string ordner, IReadOnlyList<Musterdatei> dateien, string ordnerfehler)
        {
            Ordner = ordner ?? "";
            Dateien = dateien ?? Array.Empty<Musterdatei>();
            Ordnerfehler = ordnerfehler ?? "";
        }

        /// <summary>Der Musterordner (Vorlagenordner + <see cref="BerichtsvorlagenCtrl.ORDNER_MITGELIEFERT"/>).</summary>
        public string Ordner { get; }

        /// <summary>Je Musterdatei ihr Zustand; leer, wenn schon der Ordner scheiterte.</summary>
        public IReadOnlyList<Musterdatei> Dateien { get; }

        /// <summary>Die Meldung, wenn der Ordner nicht angelegt oder erreicht werden konnte; sonst leer.</summary>
        public string Ordnerfehler { get; }

        /// <summary>Ging alles gut — Ordner erreicht, keine Datei mit Fehler oder fehlender Quelle?</summary>
        public bool Erfolg
        {
            get { return Ordnerfehler.Length == 0 && Dateien.All(d => d.Zustand == Musterzustand.Geschrieben || d.Zustand == Musterzustand.Unveraendert); }
        }

        /// <summary>
        /// Scheiterte ein Schreiben — der Ordner oder eine Datei (<see cref="Musterzustand.Fehler"/>)? Eine fehlende Quelle
        /// zählt hier nicht: Sie ist ein Befund der Installation, kein Schreibfehler im Vorlagenordner.
        /// </summary>
        public bool Schreibfehler
        {
            get { return Ordnerfehler.Length > 0 || Dateien.Any(d => d.Zustand == Musterzustand.Fehler); }
        }

        /// <summary>Die geschriebenen Dateien.</summary>
        public IReadOnlyList<string> Geschrieben
        {
            get { return Dateien.Where(d => d.Zustand == Musterzustand.Geschrieben).Select(d => d.Datei).ToList(); }
        }

        /// <summary>Alle benannten Meldungen (Ordner, dann je Datei).</summary>
        public IReadOnlyList<string> Meldungen
        {
            get
            {
                var liste = new List<string>();
                if (Ordnerfehler.Length > 0) liste.Add(Ordnerfehler);
                liste.AddRange(Dateien.Where(d => d.Meldung.Length > 0).Select(d => d.Meldung));
                return liste;
            }
        }

        /// <summary>Die Meldungen in einem Text.</summary>
        public string Meldung { get { return string.Join(Environment.NewLine, Meldungen); } }
    }

    /// <summary>
    /// <b>Die Muster im Vorlagenordner</b> (Anwenderentscheid BV-E7-6): Im Unterordner <see cref="ORDNER_MITGELIEFERT"/> des
    /// Vorlagenordners liegen die mitgelieferten Vorlagen als Ausgangspunkt eigener Vorlagen — Standardvorlage, Kurzbericht und
    /// ausführliche Vorlage je Sprache aus <see cref="IPfade.Berichtsvorlagen"/>, der Baukasten je Sprache aus dem Katalog, die Excel-Standardmappe
    /// mit Blattmarken, die ausführliche Excel-Vorlage je Sprache (Auslieferung), der Excel-Baukasten je Sprache (Katalog, BV-E9) und eine
    /// <see cref="DATEI_LIESMICH"/>.
    ///
    /// <para><b>Regeln.</b> EPOS schreibt nur in diesen Unterordner und nur die eigenen Dateinamen — fremde Dateien darin und
    /// alles im Vorlagenordner selbst bleiben unberührt. Geschrieben wird nur bei geändertem Inhalt (Vergleich über
    /// <see cref="Inhaltsschluessel"/>, der Zeitstempel, Kerneigenschaften und Zufallskennungen im Paket übergeht), damit kein Start die
    /// Dateien umsonst neu schreibt. Die Dateien werden schreibgeschützt markiert, wo die Plattform das kann; eine
    /// schreibgeschützte alte Fassung wird ersetzt. Die Liste der eigenen Vorlagen liest nur die oberste Ebene des
    /// Vorlagenordners — die Muster erscheinen dort nicht.</para>
    /// </summary>
    public partial class BerichtsvorlagenCtrl
    {
        /// <summary>Der Unterordner des Vorlagenordners mit den Mustern — sprachneutral wie die Dateinamen.</summary>
        public const string ORDNER_MITGELIEFERT = "Mitgeliefert";

        /// <summary>Der Baukasten auf Deutsch im Musterordner.</summary>
        public const string DATEI_BAUKASTEN = "Berichtsvorlage_Baukasten.docx";

        /// <summary>Der Baukasten auf Englisch im Musterordner.</summary>
        public const string DATEI_BAUKASTEN_EN = "Berichtsvorlage_Baukasten_en.docx";

        /// <summary>Die Excel-Standardmappe mit Blattmarken (<see cref="ExcelVorlagenfueller.Standardmappe"/>) im Musterordner.</summary>
        public const string DATEI_EXCEL_STANDARD = "Berichtsvorlage_Excel_Standard.xlsx";

        /// <summary>Der Excel-Baukasten auf Deutsch (<see cref="ExcelBaukasten"/>, BV-E9) im Musterordner.</summary>
        public const string DATEI_EXCEL_BAUKASTEN = "Berichtsvorlage_Excel_Baukasten.xlsx";

        /// <summary>Der Excel-Baukasten auf Englisch im Musterordner.</summary>
        public const string DATEI_EXCEL_BAUKASTEN_EN = "Berichtsvorlage_Excel_Baukasten_en.xlsx";

        /// <summary>Die Erläuterung des Musterordners, zweisprachig.</summary>
        public const string DATEI_LIESMICH = "LIESMICH.txt";

        /// <summary>Die Kerneigenschaften eines Pakets — sie tragen Erstell- und Änderungszeit und zählen im Vergleich nicht.</summary>
        private const string TEIL_KERNEIGENSCHAFTEN = "docProps/core.xml";

        /// <summary>Hält zwei Bereitstellungen (Programmstart, Ordnerwechsel) auseinander.</summary>
        private static readonly object MusterRiegel = new object();

        /// <summary>Die Musterdateien in ihrer Reihenfolge — die Namen, die <see cref="MusterBereitstellen"/> schreibt.</summary>
        public static IReadOnlyList<string> Musterdateien
        {
            get
            {
                return new[]
                {
                    DATEI_STANDARD, DATEI_KURZBERICHT, DATEI_KURZBERICHT_EN, DATEI_AUSFUEHRLICH, DATEI_AUSFUEHRLICH_EN,
                    DATEI_BAUKASTEN, DATEI_BAUKASTEN_EN, DATEI_EXCEL_STANDARD, DATEI_EXCEL_AUSFUEHRLICH, DATEI_EXCEL_AUSFUEHRLICH_EN,
                    DATEI_EXCEL_BAUKASTEN, DATEI_EXCEL_BAUKASTEN_EN, DATEI_LIESMICH,
                };
            }
        }

        /// <summary>Der Musterordner: Vorlagenordner + <see cref="ORDNER_MITGELIEFERT"/>; leer bei ungültigem Vorlagenordner.</summary>
        public string Musterordner
        {
            get
            {
                string ordner = Vorlagenordner;
                return IstGueltigerOrdner(ordner) ? Path.Combine(ordner, ORDNER_MITGELIEFERT) : "";
            }
        }

        /// <summary>
        /// <b>Stellt die Muster im Vorlagenordner bereit</b> (BV-E7-6): legt <see cref="Musterordner"/> an (den Vorgabeordner
        /// dazu bei Bedarf, einen eingestellten nie) und schreibt jede Musterdatei, deren Inhalt fehlt oder abweicht. Wirft
        /// nicht; was misslingt, benennt der <see cref="Musterbefund"/>.
        /// </summary>
        public Musterbefund MusterBereitstellen()
        {
            lock (MusterRiegel)
            {
                try
                {
                    return Bereitstellen();
                }
                catch (Exception ex)
                {
                    return new Musterbefund(SicherMusterordner(), null, T(nameof(R.BV_MUSTER_ORDNER_FEHLER), SicherMusterordner(), ex.Message));
                }
            }
        }

        /// <summary>
        /// <see cref="MusterBereitstellen"/> im Hintergrund über <see cref="Kulturweitergabe"/> — der Weg des Programmstarts
        /// beider Schalen. <paramref name="nachher"/> bekommt den Befund auf dem Arbeitsfaden (Protokoll); eine Ausnahme
        /// darin geht nicht weiter.
        /// </summary>
        public static Task<Musterbefund> MusterImHintergrundBereitstellen(Action<Musterbefund> nachher = null)
        {
            return Kulturweitergabe.Starten(() =>
            {
                Musterbefund befund = new BerichtsvorlagenCtrl().MusterBereitstellen();
                try { nachher?.Invoke(befund); }
                catch (Exception) { /* das Protokoll ist Beiwerk */ }
                return befund;
            });
        }

        private string SicherMusterordner()
        {
            try { return Musterordner; }
            catch (Exception) { return ""; }
        }

        private Musterbefund Bereitstellen()
        {
            string vorlagen = Vorlagenordner;
            if (!IstGueltigerOrdner(vorlagen))
                return new Musterbefund("", null, T(nameof(R.BV_VORLAGEN_ORDNER_UNGUELTIG), vorlagen ?? ""));
            string muster = Path.Combine(vorlagen, ORDNER_MITGELIEFERT);
            if (!Directory.Exists(vorlagen) && !IstVorgabeordner)
                return new Musterbefund(muster, null, T(nameof(R.BV_VORLAGEN_ORDNER_NICHT_ERREICHBAR), vorlagen));
            try
            {
                Directory.CreateDirectory(muster);
            }
            catch (Exception ex)
            {
                return new Musterbefund(muster, null, T(nameof(R.BV_MUSTER_ORDNER_FEHLER), muster, ex.Message));
            }

            string auslieferung = Pfade.Berichtsvorlagen ?? "";
            var dateien = new List<Musterdatei>
            {
                Kopiere(muster, auslieferung, DATEI_STANDARD),
                Kopiere(muster, auslieferung, DATEI_KURZBERICHT),
                Kopiere(muster, auslieferung, DATEI_KURZBERICHT_EN),
                Kopiere(muster, auslieferung, DATEI_AUSFUEHRLICH),
                Kopiere(muster, auslieferung, DATEI_AUSFUEHRLICH_EN),
                Erzeuge(muster, DATEI_BAUKASTEN, () => Baukasten(false)),
                Erzeuge(muster, DATEI_BAUKASTEN_EN, () => Baukasten(true)),
                Erzeuge(muster, DATEI_EXCEL_STANDARD, ExcelVorlagenfueller.Standardmappe),
                Kopiere(muster, auslieferung, DATEI_EXCEL_AUSFUEHRLICH),
                Kopiere(muster, auslieferung, DATEI_EXCEL_AUSFUEHRLICH_EN),
                Erzeuge(muster, DATEI_EXCEL_BAUKASTEN, () => BaukastenExcel(false)),
                Erzeuge(muster, DATEI_EXCEL_BAUKASTEN_EN, () => BaukastenExcel(true)),
                Erzeuge(muster, DATEI_LIESMICH, Liesmich),
            };
            return new Musterbefund(muster, dateien, null);
        }

        private static Musterdatei Kopiere(string muster, string auslieferung, string datei)
        {
            string quelle = Path.Combine(auslieferung, datei);
            if (!File.Exists(quelle))
                return new Musterdatei(datei, Musterzustand.QuelleFehlt, T(nameof(R.BV_MUSTER_QUELLE_FEHLT), datei, auslieferung));
            return Erzeuge(muster, datei, () => LiesDatei(quelle));
        }

        private static Musterdatei Erzeuge(string muster, string datei, Func<byte[]> quelle)
        {
            byte[] bytes;
            try { bytes = quelle(); }
            catch (Exception ex)
            {
                return new Musterdatei(datei, Musterzustand.Fehler, T(nameof(R.BV_MUSTER_DATEI_FEHLER), datei, ex.Message));
            }
            return Schreibe(Path.Combine(muster, datei), bytes);
        }

        /// <summary>Schreibt <paramref name="bytes"/> nach <paramref name="ziel"/>, wenn der Inhalt abweicht; danach schreibgeschützt.</summary>
        private static Musterdatei Schreibe(string ziel, byte[] bytes)
        {
            string datei = Path.GetFileName(ziel);
            try
            {
                if (File.Exists(ziel))
                {
                    byte[] alt = null;
                    try { alt = File.ReadAllBytes(ziel); }
                    catch (Exception) { alt = null; }   // unlesbar: neu schreiben
                    if (alt != null && string.Equals(Inhaltsschluessel(alt), Inhaltsschluessel(bytes), StringComparison.Ordinal))
                    {
                        Schreibschutz(ziel, true);
                        return new Musterdatei(datei, Musterzustand.Unveraendert, null);
                    }
                }

                string ordner = Path.GetDirectoryName(ziel);
                string zwischen = Path.Combine(ordner, "." + datei + ".neu");
                try
                {
                    File.WriteAllBytes(zwischen, bytes);
                    if (File.Exists(ziel)) Schreibschutz(ziel, false);
                    File.Move(zwischen, ziel, overwrite: true);
                }
                finally
                {
                    try { if (File.Exists(zwischen)) File.Delete(zwischen); }
                    catch (Exception) { /* bleibt als verstecktes Überbleibsel */ }
                }
                Schreibschutz(ziel, true);
                return new Musterdatei(datei, Musterzustand.Geschrieben, null);
            }
            catch (Exception ex)
            {
                return new Musterdatei(datei, Musterzustand.Fehler, T(nameof(R.BV_MUSTER_DATEI_FEHLER), datei, ex.Message));
            }
        }

        /// <summary>Setzt oder löscht das Attribut „schreibgeschützt“ (unter Unix: die Schreibrechte); ein Fehler bleibt still.</summary>
        private static void Schreibschutz(string pfad, bool an)
        {
            try
            {
                FileAttributes a = File.GetAttributes(pfad);
                FileAttributes neu = an ? a | FileAttributes.ReadOnly : a & ~FileAttributes.ReadOnly;
                if (neu != a) File.SetAttributes(pfad, neu);
            }
            catch (Exception)
            {
                // Der Schreibschutz ist ein Hinweis an den Anwender, keine Bedingung.
            }
        }

        /// <summary>
        /// Der Schlüssel des Inhalts: bei einem Paket (ZIP — <c>.docx</c>, <c>.xlsx</c>) die SHA-256 über Namen und Inhalt
        /// aller Teile in Namensfolge, sonst die SHA-256 der Bytes. Was ein Erzeuger bei jedem Lauf anders schreibt, zählt
        /// nicht: die Zeitstempel der Einträge, die Kerneigenschaften (<c>docProps/core.xml</c> bzw. der
        /// <c>*.psmdcp</c>-Teil mit Erstell- und Änderungszeit) und die zufälligen Kennungen, die das OpenXML-SDK für
        /// Beziehungen (<c>R</c> + 16 Hexziffern) und den <c>*.psmdcp</c>-Teil (32 Hexziffern) vergibt — sie werden in der
        /// Reihenfolge ihres ersten Auftretens durchnummeriert, so bleiben Verweise zwischen den Teilen im Schlüssel.
        /// </summary>
        internal static string Inhaltsschluessel(byte[] bytes)
        {
            if (bytes == null) return "";
            if (bytes.Length >= 4 && bytes[0] == (byte)'P' && bytes[1] == (byte)'K')
            {
                try
                {
                    using (var ms = new MemoryStream(bytes, false))
                    using (var zip = new ZipArchive(ms, ZipArchiveMode.Read))
                    using (var summe = IncrementalHash.CreateHash(HashAlgorithmName.SHA256))
                    {
                        var kennungen = new Dictionary<string, string>(StringComparer.Ordinal);
                        foreach (ZipArchiveEntry e in zip.Entries.OrderBy(e => e.FullName, StringComparer.Ordinal))
                        {
                            if (IstKerneigenschaft(e.FullName)) continue;
                            byte[] inhalt;
                            using (Stream s = e.Open())
                            using (var puffer = new MemoryStream())
                            {
                                s.CopyTo(puffer);
                                inhalt = puffer.ToArray();
                            }
                            if (IstTextteil(e.FullName))
                                inhalt = Encoding.UTF8.GetBytes(Kennungen(Encoding.UTF8.GetString(inhalt), kennungen));
                            summe.AppendData(Encoding.UTF8.GetBytes(Kennungen(e.FullName, kennungen) + "\n"));
                            summe.AppendData(BitConverter.GetBytes((long)inhalt.Length));
                            summe.AppendData(inhalt);
                        }
                        return "zip:" + Convert.ToHexString(summe.GetHashAndReset()).ToLowerInvariant();
                    }
                }
                catch (Exception)
                {
                    // kein lesbares Paket — dann zählen die Bytes
                }
            }
            return Vorlagenpruefer.Pruefsumme(bytes);
        }

        /// <summary>Die zufälligen Kennungen des SDK: Beziehungen <c>R</c> + 16 Hexziffern, der Name des <c>*.psmdcp</c>-Teils.</summary>
        private static readonly Regex ZufallsKennung = new Regex(@"\bR[0-9a-f]{16}\b|\b[0-9a-f]{32}(?=\.psmdcp\b)",
                                                                 RegexOptions.CultureInvariant | RegexOptions.Compiled);

        private static string Kennungen(string text, Dictionary<string, string> kennungen)
        {
            return ZufallsKennung.Replace(text, m =>
            {
                if (!kennungen.TryGetValue(m.Value, out string ersatz))
                {
                    ersatz = "#" + kennungen.Count.ToString(CultureInfo.InvariantCulture);
                    kennungen.Add(m.Value, ersatz);
                }
                return ersatz;
            });
        }

        private static bool IstKerneigenschaft(string teil)
        {
            return string.Equals(teil, TEIL_KERNEIGENSCHAFTEN, StringComparison.OrdinalIgnoreCase) ||
                   teil.EndsWith(".psmdcp", StringComparison.OrdinalIgnoreCase);
        }

        private static bool IstTextteil(string teil)
        {
            return teil.EndsWith(".xml", StringComparison.OrdinalIgnoreCase) || teil.EndsWith(".rels", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>Die <see cref="DATEI_LIESMICH"/>: der Text auf Deutsch, dann auf Englisch — UTF-8 mit BOM, Zeilen CRLF.</summary>
        private static byte[] Liesmich()
        {
            string de = Text(nameof(R.BV_MUSTER_LIESMICH), new CultureInfo("de-DE"));
            string en = Text(nameof(R.BV_MUSTER_LIESMICH), new CultureInfo("en-US"));
            string text = (de + "\n\n" + en + "\n").Replace("\r\n", "\n").Replace("\n", "\r\n");
            return new UTF8Encoding(true).GetPreamble().Concat(new UTF8Encoding(false).GetBytes(text)).ToArray();
        }

        private static string Text(string schluessel, CultureInfo kultur)
        {
            try { return R.ResourceManager.GetString(schluessel, kultur) ?? ""; }
            catch (Exception) { return ""; }
        }
    }
}
