using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Xml;
using SpeicherEngine;
using Xbim.Common;
using Xbim.Common.Step21;
using Xbim.IO.Memory;
using Xbim.IO.Xml;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der IFC-Leser</b> (Stufe G4a; Umsetzungskonzept 3.2–3.5, ADR-003): Strom hinein, normiertes
    /// <see cref="IfcGebaeudeAbbild"/> heraus, über <c>Xbim.IO.MemoryModel</c> — das Modell im
    /// Arbeitsspeicher, ohne Esent und ohne Geometriekern, plattformfrei.
    ///
    /// <para><b>Drei Inhaltsarten, erkannt am Inhalt, nicht am Namen</b> (der Strom hat keinen):
    /// STEP (<c>ISO-10303-21;</c> am Anfang), ifcXML (<c>&lt;</c>) und der <c>.ifczip</c>-Behälter
    /// (<c>PK</c>). Beim Behälter gilt die Größengrenze für die ENTPACKTE Größe des IFC-Eintrags, wie das
    /// Zip-Verzeichnis sie ausweist — geprüft, bevor ein Byte entpackt wird; ein nicht lesbarer Behälter
    /// oder einer ohne IFC-Eintrag ist eine eigene benannte Ablehnung.</para>
    ///
    /// <para><b>Das Schema</b> wird über den Kopf bestimmt; angenommen werden genau IFC2X3, IFC4 und
    /// IFC4X3 (<see cref="IfcSchemaStaende.AusXbim"/>), alles andere ist
    /// <c>IMP_IFC_PROT_SCHEMA_UNBEKANNT</c>.</para>
    ///
    /// <para><b>Die Protokollsenke gehört dem Lauf</b> (<see cref="IfcProtokoll"/>): Das Modell wird
    /// über den Konstruktor mit einer eigenen Fabrik angelegt und mit <c>LoadStep21</c> bzw.
    /// <c>LoadXml</c> gefüllt — nie über den globalen Dienstanbieter der Bibliothek. Es werden
    /// grundsätzlich keine Entitätstypen beim Parsen übergangen: Eine übergangene Entität wäre ein
    /// Verlust, den kein Kanal zählt (Wächtertest).</para>
    ///
    /// <para><b>Vertrag</b> (<see cref="IGebaeudeLeser"/>): Ein Fehler ist eine Meldung der Stufe Fehler,
    /// keine Ausnahme; einzig <see cref="OperationCanceledException"/> verlässt den Leser.</para>
    /// </summary>
    internal sealed class IfcLeser : IGebaeudeLeser
    {
        private const string P = IfcImportProfil.MELDUNGSPRAEFIX;

        /// <summary>Inhaltsart STEP (ISO 10303-21).</summary>
        public const string INHALT_STEP = "STEP";

        /// <summary>Inhaltsart ifcXML.</summary>
        public const string INHALT_XML = "XML";

        /// <summary>Der Anteil des Fortschrittsbalkens, den das Parsen belegt; der Rest ist das Abbild.</summary>
        private const double ANTEIL_PARSEN = 0.8;

        /// <inheritdoc />
        public GebaeudeAbbild Lesen(Stream quelle, GebaeudeImportProfil profil,
                                    IProgress<ImportFortschritt> melder, CancellationToken abbruch)
        {
            if (quelle == null) throw new ArgumentNullException(nameof(quelle));
            var abbild = new IfcGebaeudeAbbild();
            abbruch.ThrowIfCancellationRequested();

            byte[] daten = Einlesen(quelle, abbruch);
            byte[] inhalt = Auspacken(daten, profil, abbild, abbruch);
            if (inhalt == null) return abbild;   // abgelehnt — gemeldet

            var protokoll = new IfcProtokoll();
            IModel modell = Oeffnen(inhalt, abbild, protokoll, melder, abbruch);
            if (modell == null) return abbild;   // Schema oder Lesefehler — gemeldet
            using (modell as IDisposable)
            {
                abbruch.ThrowIfCancellationRequested();
                new IfcAbbildBauer(modell, abbild, melder, abbruch, ANTEIL_PARSEN).Bauen();
            }

            Verluste(abbild, protokoll);
            return abbild;
        }

        // ==================================================================
        //  Inhalt: STEP, ifcXML oder Behälter
        // ==================================================================

        private static byte[] Einlesen(Stream quelle, CancellationToken abbruch)
        {
            if (quelle is MemoryStream ms && ms.Position == 0 && ms.TryGetBuffer(out ArraySegment<byte> segment)
                && segment.Offset == 0 && segment.Count == segment.Array.Length)
                return segment.Array;
            var ziel = new MemoryStream();
            var block = new byte[81920];
            int n;
            while ((n = quelle.Read(block, 0, block.Length)) > 0)
            {
                abbruch.ThrowIfCancellationRequested();
                ziel.Write(block, 0, n);
            }
            return ziel.ToArray();
        }

        /// <summary>
        /// Liefert den IFC-Inhalt (STEP oder XML): die Daten selbst oder den IFC-Eintrag eines Behälters —
        /// dessen entpackte Größe wird VOR dem Entpacken gegen die Grenze gehalten. <c>null</c> = abgelehnt.
        /// </summary>
        internal static byte[] Auspacken(byte[] daten, GebaeudeImportProfil profil, IfcGebaeudeAbbild abbild,
                                         CancellationToken abbruch)
        {
            if (!IstZip(daten))
            {
                string art = Inhaltsart(daten);
                if (art == null)
                {
                    abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, P + "FORMAT_UNBEKANNT"));
                    return null;
                }
                abbild.Inhaltsart = art;
                return daten;
            }

            ZipArchive behaelter;
            try
            {
                behaelter = new ZipArchive(new MemoryStream(daten, false), ZipArchiveMode.Read, false);
            }
            catch (Exception ex) when (ex is InvalidDataException || ex is IOException || ex is ArgumentException)
            {
                abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, P + "BEHAELTER_UNLESBAR", ex.Message));
                return null;
            }

            using (behaelter)
            {
                List<ZipArchiveEntry> eintraege;
                try
                {
                    eintraege = behaelter.Entries
                        .Where(e => !string.IsNullOrEmpty(e.Name)
                                    && (e.Name.EndsWith(".ifc", StringComparison.OrdinalIgnoreCase)
                                        || e.Name.EndsWith(".ifcxml", StringComparison.OrdinalIgnoreCase)))
                        .OrderBy(e => e.FullName, StringComparer.Ordinal)
                        .ToList();
                }
                catch (Exception ex) when (ex is InvalidDataException || ex is IOException)
                {
                    abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, P + "BEHAELTER_UNLESBAR", ex.Message));
                    return null;
                }
                if (eintraege.Count == 0)
                {
                    abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, P + "BEHAELTER_OHNE_IFC"));
                    return null;
                }

                ZipArchiveEntry eintrag = eintraege[0];
                if (eintraege.Count > 1)
                    abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, P + "BEHAELTER_MEHRERE",
                        Ganz(eintraege.Count), eintrag.FullName));
                abbild.Behaeltereintrag = eintrag.FullName;
                abbild.EntpackteGroesse = eintrag.Length;

                // Die Grenze gilt der ENTPACKTEN Größe — aus dem Verzeichnis, vor dem Entpacken.
                if (!GebaeudeImportAblauf.GroesseZulaessig(eintrag.Length, profil))
                {
                    ZuGrossEntpackt(eintrag.Length, profil, abbild);
                    return null;
                }

                byte[] inhalt;
                try
                {
                    inhalt = Entpacken(eintrag, profil, abbruch);
                }
                catch (Exception ex) when (ex is InvalidDataException || ex is IOException)
                {
                    abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, P + "BEHAELTER_UNLESBAR", ex.Message));
                    return null;
                }
                if (inhalt == null)
                {
                    // Das Verzeichnis hat gelogen: der Eintrag ist entpackt größer als ausgewiesen.
                    ZuGrossEntpackt(profil.MaxBytes + 1, profil, abbild);
                    return null;
                }

                string art = Inhaltsart(inhalt);
                if (art == null)
                {
                    abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, P + "FORMAT_UNBEKANNT"));
                    return null;
                }
                abbild.Inhaltsart = art;
                return inhalt;
            }
        }

        /// <summary>Entpackt höchstens <c>MaxBytes</c> + 1 Byte; <c>null</c> = mehr als die Grenze.</summary>
        private static byte[] Entpacken(ZipArchiveEntry eintrag, GebaeudeImportProfil profil, CancellationToken abbruch)
        {
            long grenze = profil != null && profil.MaxBytes > 0 ? profil.MaxBytes : long.MaxValue;
            using (Stream s = eintrag.Open())
            {
                var ziel = new MemoryStream();
                var block = new byte[81920];
                int n;
                while ((n = s.Read(block, 0, block.Length)) > 0)
                {
                    abbruch.ThrowIfCancellationRequested();
                    ziel.Write(block, 0, n);
                    if (ziel.Length > grenze) return null;
                }
                return ziel.ToArray();
            }
        }

        private static void ZuGrossEntpackt(long bytes, GebaeudeImportProfil profil, IfcGebaeudeAbbild abbild)
            => abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, P + "ZU_GROSS_ENTPACKT",
                   bytes.ToString(CultureInfo.InvariantCulture),
                   (profil?.MaxBytes ?? 0).ToString(CultureInfo.InvariantCulture)));

        /// <summary>Beginnt der Inhalt mit einer Zip-Kennung (<c>PK\x03\x04</c>, leer <c>PK\x05\x06</c>)?</summary>
        internal static bool IstZip(byte[] d)
            => d != null && d.Length >= 4 && d[0] == 0x50 && d[1] == 0x4B
               && ((d[2] == 0x03 && d[3] == 0x04) || (d[2] == 0x05 && d[3] == 0x06));

        /// <summary><see cref="INHALT_STEP"/>, <see cref="INHALT_XML"/> oder <c>null</c> — nach BOM und Leerraum.</summary>
        internal static string Inhaltsart(byte[] d)
        {
            if (d == null || d.Length == 0) return null;
            // UTF-16 mit BOM ist nur für XML denkbar.
            if (d.Length >= 2 && ((d[0] == 0xFF && d[1] == 0xFE) || (d[0] == 0xFE && d[1] == 0xFF))) return INHALT_XML;
            int i = d.Length >= 3 && d[0] == 0xEF && d[1] == 0xBB && d[2] == 0xBF ? 3 : 0;
            while (i < d.Length && (d[i] == (byte)' ' || d[i] == (byte)'\t' || d[i] == (byte)'\r' || d[i] == (byte)'\n')) i++;
            if (i >= d.Length) return null;
            if (d[i] == (byte)'<') return INHALT_XML;
            const string kopf = "ISO-10303-21";
            if (d.Length - i >= kopf.Length && Encoding.ASCII.GetString(d, i, kopf.Length) == kopf) return INHALT_STEP;
            return null;
        }

        // ==================================================================
        //  Schema und Modell
        // ==================================================================

        /// <summary>
        /// Bestimmt das Schema über den Kopf und lädt das Modell mit der Fabrik dieses Schemas und der
        /// Protokollsenke des Laufs. <c>null</c> = abgelehnt oder Lesefehler — gemeldet.
        /// </summary>
        private static IModel Oeffnen(byte[] inhalt, IfcGebaeudeAbbild abbild, IfcProtokoll protokoll,
                                      IProgress<ImportFortschritt> melder, CancellationToken abbruch)
        {
            bool step = abbild.Inhaltsart == INHALT_STEP;
            XbimSchemaVersion fassung;
            string roh;
            try
            {
                if (step)
                {
                    fassung = MemoryModel.GetStepFileXbimSchemaVersion(new MemoryStream(inhalt, false));
                    roh = SchemaImKopf(inhalt);
                }
                else
                {
                    fassung = XmlSchema(inhalt);
                    roh = IfcSchemaStaende.Kurzform(IfcSchemaStaende.AusXbim(fassung));
                }
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, P + "LESEFEHLER", ex.Message));
                return null;
            }

            IfcSchemaStand stand = IfcSchemaStaende.AusXbim(fassung);
            abbild.SchemaStand = stand;
            abbild.Schemastand = string.IsNullOrWhiteSpace(roh) ? IfcSchemaStaende.Kurzform(stand) : roh;
            if (stand == IfcSchemaStand.Unbekannt)
            {
                string gelesen = string.IsNullOrWhiteSpace(roh) ? fassung.ToString() : roh;
                abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, P + "SCHEMA_UNBEKANNT", gelesen));
                return null;
            }

            ReportProgressDelegate fortschritt = (prozent, zustand) =>
            {
                // Ein Abbruch hier wird von der Bibliothek in eine Parserausnahme gewickelt; der Leser
                // erkennt ihn am Zeichen und wirft ihn unten wieder als OperationCanceledException.
                abbruch.ThrowIfCancellationRequested();
                melder?.Report(new ImportFortschritt(Math.Max(0, Math.Min(100, prozent)) / 100.0 * ANTEIL_PARSEN,
                    P + "FORTSCHRITT", Ganz(prozent)));
            };

            MemoryModel modell = null;
            try
            {
                modell = new MemoryModel(MemoryModel.GetFactory(fassung), (Microsoft.Extensions.Logging.ILoggerFactory)protokoll, 0);
                using (var strom = new MemoryStream(inhalt, false))
                {
                    // Vierter Wert null: KEINE Entitätstypen übergehen (Datenaustauschkonzept 4, Ergänzung 3).
                    if (step) modell.LoadStep21(strom, inhalt.LongLength, fortschritt, null);
                    else modell.LoadXml(strom, inhalt.LongLength, fortschritt);
                }
                return modell;
            }
            catch (Exception ex)
            {
                modell?.Dispose();
                if (abbruch.IsCancellationRequested) throw new OperationCanceledException(abbruch);
                if (ex is OperationCanceledException) throw;
                abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Fehler, P + "LESEFEHLER", ex.Message));
                return null;
            }
        }

        /// <summary>Die Schemakennung(en) im STEP-Kopf, wie gelesen (<c>IFC4</c>, <c>IFC4X3_ADD2</c> …).</summary>
        private static string SchemaImKopf(byte[] inhalt)
        {
            try
            {
                IStepFileHeader kopf = Xbim.Common.Model.StepModel.LoadStep21Header(new MemoryStream(inhalt, false));
                IList<string> schemata = kopf?.FileSchema?.Schemas;
                return schemata == null || schemata.Count == 0 ? null : string.Join(",", schemata);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Das Schema einer ifcXML-Datei über den Namensraum der Wurzel — mit einem eigenen, sicheren
        /// Leser: DTD verboten, kein Auflöser (keine externe Entität, kein Netzzugriff).
        /// </summary>
        private static XbimSchemaVersion XmlSchema(byte[] inhalt)
        {
            var einstellungen = new XmlReaderSettings
            {
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                IgnoreComments = true,
                IgnoreProcessingInstructions = true,
                CloseInput = false,
            };
            using (XmlReader leser = XmlReader.Create(new MemoryStream(inhalt, false), einstellungen))
            {
                switch (XbimXmlReader4.ReadSchemaVersion(leser))
                {
                    case XmlSchemaVersion.Ifc2x3: return XbimSchemaVersion.Ifc2X3;
                    case XmlSchemaVersion.Ifc4:
                    case XmlSchemaVersion.Ifc4Add1:
                    case XmlSchemaVersion.Ifc4Add2: return XbimSchemaVersion.Ifc4;
                    default: return XbimSchemaVersion.Unsupported;
                }
            }
        }

        // ==================================================================
        //  Verluste
        // ==================================================================

        /// <summary>Legt beide Verlustkanäle ins Abbild; eine Summe über null ist eine Warnung.</summary>
        private static void Verluste(IfcGebaeudeAbbild abbild, IfcProtokoll protokoll)
        {
            abbild.VerlusteNichtAngelegt = protokoll.NichtAngelegt;
            abbild.VerlusteVerweise = protokoll.VerweiseInsLeere;
            abbild.FehlendeEntitaeten = protokoll.Summe;
            if (protokoll.Summe > 0)
                abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, P + "ENTITAETEN_VERLOREN",
                    Ganz(protokoll.Summe), Ganz(protokoll.NichtAngelegt), Ganz(protokoll.VerweiseInsLeere),
                    protokoll.Beispiele.Count > 0 ? protokoll.Beispiele[0] : ""));
        }

        private static string Ganz(int w) => w.ToString(CultureInfo.InvariantCulture);
    }
}
