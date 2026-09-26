using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Validation;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Muster im Vorlagenordner</b> (Anwenderentscheid BV-E7-6): <see cref="BerichtsvorlagenCtrl.MusterBereitstellen"/>
    /// legt im Unterordner <see cref="BerichtsvorlagenCtrl.ORDNER_MITGELIEFERT"/> Standardvorlage, Kurzbericht und Baukasten je
    /// Sprache, die Excel-Standardmappe und die <c>LIESMICH.txt</c> an; ein zweiter Lauf schreibt nichts, eine geänderte
    /// Quelle erneuert nur ihre Datei, Eigenes und Fremdes bleibt unberührt, eine schreibgeschützte alte Fassung wird ersetzt,
    /// ein nicht beschreibbarer Ordner ist ein benannter Befund. Die Muster erscheinen nicht als eigene Vorlagen.
    ///
    /// <para><b>Rahmen.</b> Temp-Ordner je Fall, Pfade und Einstellungen hereingereicht — kein <c>Dienste.*</c> wird
    /// getauscht. Die mitgelieferten Word-Dateien sind die des Repositoriums
    /// (<see cref="BerichtsvorlageDateiWacheTests.ORDNER_REPO"/>).</para>
    /// </summary>
    public class BerichtsvorlagenMusterTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly string _wurzel = Probevorlagen.TempOrdner("epos-bv-muster");
        private readonly string _dokumente;
        private readonly string _app;
        private readonly FluechtigeEinstellungen _einstellungen = new FluechtigeEinstellungen();
        private readonly BerichtsvorlagenCtrl _ctrl;

        private static readonly string[] Ausgeliefert =
        {
            BerichtsvorlagenCtrl.DATEI_STANDARD, BerichtsvorlagenCtrl.DATEI_KURZBERICHT, BerichtsvorlagenCtrl.DATEI_KURZBERICHT_EN,
        };

        public BerichtsvorlagenMusterTests()
        {
            _dokumente = Directory.CreateDirectory(Path.Combine(_wurzel, "Dokumente")).FullName;
            _app = Directory.CreateDirectory(Path.Combine(_wurzel, "App", "Vorlagen")).FullName;
            foreach (string datei in Ausgeliefert.Append(BerichtsvorlagenCtrl.DATEI_RUECKFALL))
            {
                string quelle = BerichtsvorlageDateiWacheTests.Pfad(datei);
                Assert.True(quelle != null && File.Exists(quelle), "Vorlage fehlt im Repository: " + datei);
                File.Copy(quelle, Path.Combine(_app, datei));
            }
            _ctrl = new BerichtsvorlagenCtrl(new Probepfade(_dokumente, _app), _einstellungen, () => "Lizenz GmbH");
        }

        public void Dispose()
        {
            _kultur.Dispose();
            Probevorlagen.Aufraeumen(_wurzel);
        }

        private sealed class Probepfade : StandardPfade
        {
            private readonly string _dokumente;
            private readonly string _vorlagen;

            public Probepfade(string dokumente, string vorlagen)
            {
                _dokumente = dokumente;
                _vorlagen = vorlagen;
            }

            public override string Dokumente { get { return _dokumente; } }

            public override string Berichtsvorlagen { get { return _vorlagen; } }
        }

        private string Muster => Path.Combine(_dokumente, "EPOS-Plan", "Berichtsvorlagen", BerichtsvorlagenCtrl.ORDNER_MITGELIEFERT);

        private string MusterPfad(string datei) => Path.Combine(Muster, datei);

        private Dictionary<string, DateTime> Zeitstempel()
        {
            return Directory.EnumerateFiles(Muster).ToDictionary(Path.GetFileName, File.GetLastWriteTimeUtc);
        }

        private static bool Schreibgeschuetzt(string pfad) => (File.GetAttributes(pfad) & FileAttributes.ReadOnly) != 0;

        private static void Beschreibbar(string pfad) => File.SetAttributes(pfad, File.GetAttributes(pfad) & ~FileAttributes.ReadOnly);

        /// <summary>Setzt die Zeitstempel der Dateien zurück — so zeigt ein späterer Vergleich jedes Schreiben.</summary>
        private void Altern()
        {
            DateTime alt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            foreach (string datei in Directory.EnumerateFiles(Muster))
            {
                bool schutz = Schreibgeschuetzt(datei);
                if (schutz) Beschreibbar(datei);
                File.SetLastWriteTimeUtc(datei, alt);
                if (schutz) File.SetAttributes(datei, File.GetAttributes(datei) | FileAttributes.ReadOnly);
            }
        }

        // =====================================================================
        //  Erstanlage, Gültigkeit, Liste
        // =====================================================================

        [Fact]
        public void Erstanlage_schreibt_alle_Muster_gueltig_und_schreibgeschuetzt()
        {
            Musterbefund befund = _ctrl.MusterBereitstellen();

            Assert.True(befund.Erfolg, befund.Meldung);
            Assert.Equal(Muster, befund.Ordner);
            Assert.Equal(Muster, _ctrl.Musterordner);
            Assert.Equal(BerichtsvorlagenCtrl.Musterdateien, befund.Geschrieben);
            Assert.Equal(7, befund.Geschrieben.Count);
            Assert.Equal(BerichtsvorlagenCtrl.Musterdateien.OrderBy(d => d, StringComparer.Ordinal),
                         Directory.EnumerateFiles(Muster).Select(Path.GetFileName).OrderBy(d => d, StringComparer.Ordinal));
            foreach (string datei in BerichtsvorlagenCtrl.Musterdateien)
                Assert.True(Schreibgeschuetzt(MusterPfad(datei)), datei + " ist nicht schreibgeschützt");

            // Die mitgelieferten sind Kopien, byte-gleich.
            foreach (string datei in Ausgeliefert)
                Assert.Equal(File.ReadAllBytes(Path.Combine(_app, datei)), File.ReadAllBytes(MusterPfad(datei)));
            Assert.False(File.Exists(MusterPfad(BerichtsvorlagenCtrl.DATEI_RUECKFALL)));

            // Word: Validator und Prüfer ohne Fehler.
            foreach (string datei in new[]
                     {
                         BerichtsvorlagenCtrl.DATEI_STANDARD, BerichtsvorlagenCtrl.DATEI_KURZBERICHT, BerichtsvorlagenCtrl.DATEI_KURZBERICHT_EN,
                         BerichtsvorlagenCtrl.DATEI_BAUKASTEN, BerichtsvorlagenCtrl.DATEI_BAUKASTEN_EN,
                     })
            {
                using (WordprocessingDocument doc = WordprocessingDocument.Open(MusterPfad(datei), false))
                {
                    foreach (FileFormatVersions fassung in WordBerichtSvgWacheTests.Fassungen.Where(f => f >= FileFormatVersions.Office2010))
                    {
                        List<ValidationErrorInfo> fehler = new OpenXmlValidator(fassung).Validate(doc).Take(5).ToList();
                        Assert.True(fehler.Count == 0, datei + " " + fassung + ": " + string.Join(" | ", fehler.Select(f => f.Description)));
                    }
                }
                bool englisch = datei.EndsWith("_en.docx", StringComparison.Ordinal);
                Pruefbefund pruef = Vorlagenpruefer.Pruefe(File.ReadAllBytes(MusterPfad(datei)), Pruefstufe.Voll,
                    new Pruefkontext { AnzahlVarianten = 2, Sicht = 1, Englisch = englisch, Dateiname = datei });
                Assert.Empty(pruef.UnbekannteSchluessel);
                Assert.DoesNotContain(pruef.Meldungen, m => m.Stufe == Befundstufe.Fehler);
            }

            // Excel: ClosedXML lädt die Mappe, die Blattmarken stehen darin, der Prüfer meldet keinen Fehler.
            byte[] mappe = File.ReadAllBytes(MusterPfad(BerichtsvorlagenCtrl.DATEI_EXCEL_STANDARD));
            using (var strom = new MemoryStream(mappe))
            using (var wb = new XLWorkbook(strom))
            {
                Assert.Equal(ExcelVorlagenmappe.Blattmarken.Count, wb.Worksheets.Count);
                Assert.All(wb.Worksheets, ws => Assert.StartsWith("{{", ws.Cell(1, 1).GetString()));
            }
            Pruefbefund xl = ExcelVorlagenpruefer.Pruefe(mappe, Pruefstufe.Voll,
                new Pruefkontext { Dateiname = BerichtsvorlagenCtrl.DATEI_EXCEL_STANDARD, Ausgabe = Vorlagenausgabe.Excel });
            Assert.DoesNotContain(xl.Meldungen, m => m.Stufe == Befundstufe.Fehler);

            // LIESMICH: beide Sprachen, UTF-8 mit BOM.
            byte[] liesmich = File.ReadAllBytes(MusterPfad(BerichtsvorlagenCtrl.DATEI_LIESMICH));
            Assert.Equal(new byte[] { 0xEF, 0xBB, 0xBF }, liesmich.Take(3).ToArray());
            string text = Encoding.UTF8.GetString(liesmich, 3, liesmich.Length - 3);
            Assert.Contains("Diese Dateien erneuert EPOS-Plan bei jedem Start.", text);
            Assert.Contains("„Neue Vorlage…“", text);
            Assert.Contains("EPOS-Plan renews these files at every start.", text);

            // Die Muster erscheinen nicht als eigene Vorlagen.
            Assert.Equal(new[] { BerichtsvorlagenCtrl.ID_STANDARD }, _ctrl.Liste().Select(e => e.Id));
            Assert.Equal(new[] { BerichtsvorlagenCtrl.ID_OHNE }, _ctrl.ListeExcel().Select(e => e.Id));
            Assert.Null(_ctrl.Finde(BerichtsvorlagenCtrl.ID_PRAEFIX_EIGEN + BerichtsvorlagenCtrl.DATEI_BAUKASTEN));
            Assert.Null(_ctrl.FindeExcel(BerichtsvorlagenCtrl.ID_PRAEFIX_EIGEN + BerichtsvorlagenCtrl.DATEI_EXCEL_STANDARD));
        }

        // =====================================================================
        //  Aktualisieren nur bei Änderung
        // =====================================================================

        [Fact]
        public void Zweiter_Lauf_schreibt_nichts()
        {
            Assert.True(_ctrl.MusterBereitstellen().Erfolg);
            Altern();
            Dictionary<string, DateTime> vorher = Zeitstempel();

            Musterbefund zweiter = _ctrl.MusterBereitstellen();

            Assert.True(zweiter.Erfolg, zweiter.Meldung);
            Assert.Empty(zweiter.Geschrieben);
            Assert.All(zweiter.Dateien, d => Assert.Equal(Musterzustand.Unveraendert, d.Zustand));
            Assert.Equal(vorher, Zeitstempel());
        }

        [Fact]
        public void Geaenderte_Quelle_erneuert_nur_ihre_Datei()
        {
            Assert.True(_ctrl.MusterBereitstellen().Erfolg);
            Altern();
            Dictionary<string, DateTime> vorher = Zeitstempel();

            // Ein Update liefert einen anderen Kurzbericht (hier: den Inhalt der Standardvorlage).
            byte[] neu = File.ReadAllBytes(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_STANDARD));
            File.WriteAllBytes(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_KURZBERICHT), neu);

            Musterbefund befund = _ctrl.MusterBereitstellen();

            Assert.True(befund.Erfolg, befund.Meldung);
            Assert.Equal(new[] { BerichtsvorlagenCtrl.DATEI_KURZBERICHT }, befund.Geschrieben);
            Assert.Equal(neu, File.ReadAllBytes(MusterPfad(BerichtsvorlagenCtrl.DATEI_KURZBERICHT)));
            Assert.True(Schreibgeschuetzt(MusterPfad(BerichtsvorlagenCtrl.DATEI_KURZBERICHT)));
            Dictionary<string, DateTime> nachher = Zeitstempel();
            foreach (var paar in vorher.Where(p => p.Key != BerichtsvorlagenCtrl.DATEI_KURZBERICHT))
                Assert.Equal(paar.Value, nachher[paar.Key]);
            Assert.NotEqual(vorher[BerichtsvorlagenCtrl.DATEI_KURZBERICHT], nachher[BerichtsvorlagenCtrl.DATEI_KURZBERICHT]);
        }

        [Fact]
        public void Schreibgeschuetzte_alte_Fassung_wird_ersetzt()
        {
            Directory.CreateDirectory(Muster);
            string alt = MusterPfad(BerichtsvorlagenCtrl.DATEI_BAUKASTEN_EN);
            File.WriteAllBytes(alt, Probevorlagen.AusAbsaetzen("{{projekt.kunde}}"));
            File.SetAttributes(alt, File.GetAttributes(alt) | FileAttributes.ReadOnly);

            Musterbefund befund = _ctrl.MusterBereitstellen();

            Assert.True(befund.Erfolg, befund.Meldung);
            Assert.Contains(BerichtsvorlagenCtrl.DATEI_BAUKASTEN_EN, befund.Geschrieben);
            Assert.Equal(BerichtsvorlagenCtrl.Inhaltsschluessel(WordBaukasten.Erzeuge(true)),
                         BerichtsvorlagenCtrl.Inhaltsschluessel(File.ReadAllBytes(alt)));
            Assert.True(Schreibgeschuetzt(alt));
            Assert.Empty(Directory.EnumerateFiles(Muster, ".*"));   // keine Zwischendatei übrig
        }

        /// <summary>
        /// Der Inhaltsschlüssel übergeht die Zeitstempel eines Pakets: Baukasten und Excel-Standardmappe entstehen bei jedem
        /// Start neu, ihre Bytes tragen die Zeit — gleich bleiben muss der Schlüssel, sonst schriebe jeder Start.
        /// </summary>
        [Fact]
        public void Inhaltsschluessel_uebergeht_Zeitstempel_im_Paket()
        {
            foreach (byte[] paket in new[] { ExcelVorlagenfueller.Standardmappe(), WordBaukasten.Erzeuge(false) })
            {
                byte[] gealtert = Gealtert(paket);
                Assert.NotEqual(paket, gealtert);
                Assert.Equal(BerichtsvorlagenCtrl.Inhaltsschluessel(paket), BerichtsvorlagenCtrl.Inhaltsschluessel(gealtert));
            }
            Assert.Equal(BerichtsvorlagenCtrl.Inhaltsschluessel(ExcelVorlagenfueller.Standardmappe()),
                         BerichtsvorlagenCtrl.Inhaltsschluessel(ExcelVorlagenfueller.Standardmappe()));
            Assert.Equal(BerichtsvorlagenCtrl.Inhaltsschluessel(WordBaukasten.Erzeuge(true)),
                         BerichtsvorlagenCtrl.Inhaltsschluessel(WordBaukasten.Erzeuge(true)));
            Assert.NotEqual(BerichtsvorlagenCtrl.Inhaltsschluessel(WordBaukasten.Erzeuge(false)),
                            BerichtsvorlagenCtrl.Inhaltsschluessel(WordBaukasten.Erzeuge(true)));
            Assert.NotEqual(BerichtsvorlagenCtrl.Inhaltsschluessel(Encoding.UTF8.GetBytes("a")),
                            BerichtsvorlagenCtrl.Inhaltsschluessel(Encoding.UTF8.GetBytes("b")));
        }

        /// <summary>Das Paket mit anderen Zeitstempeln aller Einträge und einer anderen Erstellzeit in den Kerneigenschaften.</summary>
        private static byte[] Gealtert(byte[] paket)
        {
            using (var ms = new MemoryStream())
            {
                ms.Write(paket, 0, paket.Length);
                using (var zip = new ZipArchive(ms, ZipArchiveMode.Update, true))
                {
                    foreach (ZipArchiveEntry e in zip.Entries.ToList())
                    {
                        if (string.Equals(e.FullName, "docProps/core.xml", StringComparison.OrdinalIgnoreCase) ||
                            e.FullName.EndsWith(".psmdcp", StringComparison.OrdinalIgnoreCase))
                        {
                            string name = e.FullName;
                            string text;
                            using (var r = new StreamReader(e.Open())) text = r.ReadToEnd();
                            e.Delete();
                            ZipArchiveEntry neu = zip.CreateEntry(name);
                            using (var w = new StreamWriter(neu.Open())) w.Write(text.Replace("20", "19"));
                            neu.LastWriteTime = new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero);
                        }
                        else
                        {
                            e.LastWriteTime = new DateTimeOffset(2001, 2, 3, 4, 5, 6, TimeSpan.Zero);
                        }
                    }
                }
                return ms.ToArray();
            }
        }

        // =====================================================================
        //  Eigenes und Fremdes bleibt
        // =====================================================================

        [Fact]
        public void Eigene_und_fremde_Dateien_bleiben_unberuehrt()
        {
            string vorlagen = _ctrl.Vorlagenordner;
            Directory.CreateDirectory(Muster);
            string eigen = Path.Combine(vorlagen, "Meine Vorlage.docx");
            byte[] eigenBytes = Probevorlagen.AusAbsaetzen("{{projekt.kunde}}");
            File.WriteAllBytes(eigen, eigenBytes);
            string eigenStandard = Path.Combine(vorlagen, BerichtsvorlagenCtrl.DATEI_STANDARD);   // gleichnamig, aber eigen
            File.WriteAllBytes(eigenStandard, eigenBytes);
            string fremd = MusterPfad("Notiz.txt");
            File.WriteAllText(fremd, "meins");
            DateTime alt = new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);
            foreach (string p in new[] { eigen, eigenStandard, fremd }) File.SetLastWriteTimeUtc(p, alt);

            Assert.True(_ctrl.MusterBereitstellen().Erfolg);
            Assert.True(_ctrl.MusterBereitstellen().Erfolg);

            foreach (string p in new[] { eigen, eigenStandard, fremd })
            {
                Assert.Equal(alt, File.GetLastWriteTimeUtc(p));
                Assert.False(Schreibgeschuetzt(p));
            }
            Assert.Equal(eigenBytes, File.ReadAllBytes(eigen));
            Assert.Equal(eigenBytes, File.ReadAllBytes(eigenStandard));
            Assert.Equal("meins", File.ReadAllText(fremd));

            // Die Liste zeigt die eigenen — und kein Muster.
            Assert.Equal(new[] { BerichtsvorlagenCtrl.ID_STANDARD, "eigen:" + BerichtsvorlagenCtrl.DATEI_STANDARD, "eigen:Meine Vorlage.docx" },
                         _ctrl.Liste().Select(e => e.Id));
            Assert.False(File.Exists(Path.Combine(vorlagen, BerichtsvorlagenCtrl.ABLAGEDATEI)));
        }

        // =====================================================================
        //  Befunde
        // =====================================================================

        [Fact]
        public void Nicht_beschreibbarer_Musterordner_ist_ein_benannter_Befund()
        {
            // Wo der Unterordner stehen soll, liegt eine Datei: Er lässt sich nicht anlegen — auf jeder Plattform.
            Directory.CreateDirectory(_ctrl.Vorlagenordner);
            File.WriteAllText(Muster, "keine Mappe");

            Musterbefund befund = _ctrl.MusterBereitstellen();

            Assert.False(befund.Erfolg);
            Assert.True(befund.Schreibfehler);
            Assert.Empty(befund.Dateien);
            Assert.StartsWith("Der Ordner der mitgelieferten Muster " + Muster + " konnte nicht angelegt werden: ", befund.Ordnerfehler);
            Assert.Equal(befund.Ordnerfehler, befund.Meldung);
            Assert.Equal("keine Mappe", File.ReadAllText(Muster));
        }

        [Fact]
        public void Nicht_schreibbare_Datei_ist_ein_benannter_Befund_und_haelt_die_uebrigen_nicht_auf()
        {
            // Wo die LIESMICH stehen soll, liegt ein Ordner — sie lässt sich nicht schreiben.
            Directory.CreateDirectory(MusterPfad(BerichtsvorlagenCtrl.DATEI_LIESMICH));

            Musterbefund befund = _ctrl.MusterBereitstellen();

            Assert.False(befund.Erfolg);
            Musterdatei liesmich = befund.Dateien.Single(d => d.Datei == BerichtsvorlagenCtrl.DATEI_LIESMICH);
            Assert.Equal(Musterzustand.Fehler, liesmich.Zustand);
            Assert.True(befund.Schreibfehler);
            Assert.StartsWith("Das Muster LIESMICH.txt im Vorlagenordner konnte nicht geschrieben werden: ", liesmich.Meldung);
            Assert.Equal(6, befund.Geschrieben.Count);
            Assert.Empty(Directory.EnumerateFiles(Muster, ".*"));
        }

        [Fact]
        public void Gesperrte_Datei_ist_ein_benannter_Befund()
        {
            if (!OperatingSystem.IsWindows()) return;   // Dateisperren gibt es so nur unter Windows
            Assert.True(_ctrl.MusterBereitstellen().Erfolg);
            File.WriteAllBytes(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_KURZBERICHT_EN),
                               File.ReadAllBytes(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_STANDARD)));
            using (new FileStream(MusterPfad(BerichtsvorlagenCtrl.DATEI_KURZBERICHT_EN), FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Musterbefund befund = _ctrl.MusterBereitstellen();
                Assert.False(befund.Erfolg);
                Assert.Equal(Musterzustand.Fehler, befund.Dateien.Single(d => d.Datei == BerichtsvorlagenCtrl.DATEI_KURZBERICHT_EN).Zustand);
            }
        }

        [Fact]
        public void Fehlende_Quelle_wird_benannt()
        {
            File.Delete(Path.Combine(_app, BerichtsvorlagenCtrl.DATEI_KURZBERICHT_EN));

            Musterbefund befund = _ctrl.MusterBereitstellen();

            Assert.False(befund.Erfolg);
            Musterdatei fehlt = befund.Dateien.Single(d => d.Datei == BerichtsvorlagenCtrl.DATEI_KURZBERICHT_EN);
            Assert.Equal(Musterzustand.QuelleFehlt, fehlt.Zustand);
            Assert.False(befund.Schreibfehler);
            Assert.Equal("Das mitgelieferte Muster " + BerichtsvorlagenCtrl.DATEI_KURZBERICHT_EN + " fehlt im Auslieferungsordner " + _app,
                         fehlt.Meldung);
            Assert.Equal(6, befund.Geschrieben.Count);
            Assert.False(File.Exists(MusterPfad(BerichtsvorlagenCtrl.DATEI_KURZBERICHT_EN)));
        }

        [Fact]
        public void Eingestellter_Ordner_wird_nie_angelegt_der_Wechsel_bringt_die_Muster_mit()
        {
            string fehlt = Path.Combine(_wurzel, "Buero-offline");
            _einstellungen.Schreib(BerichtsvorlagenCtrl.EINSTELLUNG_ORDNER, fehlt);

            Musterbefund befund = _ctrl.MusterBereitstellen();

            Assert.False(befund.Erfolg);
            Assert.Equal("Der Vorlagenordner " + fehlt + " ist nicht erreichbar", befund.Ordnerfehler);
            Assert.False(Directory.Exists(fehlt));

            string buero = Directory.CreateDirectory(Path.Combine(_wurzel, "Buero")).FullName;
            Assert.True(_ctrl.SetzeVorlagenordner(buero).Erfolg);
            Musterbefund danach = _ctrl.MusterBereitstellen();
            Assert.True(danach.Erfolg, danach.Meldung);
            Assert.Equal(Path.Combine(buero, BerichtsvorlagenCtrl.ORDNER_MITGELIEFERT), danach.Ordner);
            Assert.Equal(7, Directory.EnumerateFiles(danach.Ordner).Count());
        }
    }
}
