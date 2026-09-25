using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.CompilerServices;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die IFC-Importproben sind die Messlatte</b> (Stufe G4a Welle 1): Die Dateien unter
    /// <c>Referenzlaeufe/Importproben/ifc*</c> sind mit <see cref="IfcProbenErzeuger"/> selbst erzeugt und
    /// eingecheckt; die Tests lesen die abgelegten Dateien, nicht die frisch erzeugten.
    ///
    /// <para><b>Erzeugt wird von Hand:</b> Der Erzeuger ist ein normal übersprungener Test
    /// (<see cref="Erzeuger_schreibt_die_Proben"/>) — zum Neuschreiben das <c>Skip</c> örtlich
    /// entfernen, einmal laufen lassen, zurücksetzen. Der zweite Test hält fest, dass eine erneute
    /// Erzeugung byte-gleich wäre; einzige Ausnahme ist der <c>.ifczip</c>-Behälter: Die Deflate-Ausgabe
    /// hängt an der zlib-Fassung der Laufzeit (zlib-ng unter .NET 9+, je Plattform gebaut) und ist über
    /// Laufzeiten hinweg nicht zugesagt — verglichen werden dort Eintragsname und entpackter Inhalt.</para>
    ///
    /// <para><c>ifc4_verlust.ifc</c> ist dagegen von Hand geschrieben (&lt; 5 KB) und hat keinen Erzeuger.</para>
    /// </summary>
    public sealed class IfcProbenTests
    {
        [Fact(Skip = "Erzeuger: schreibt die IFC-Importproben nach Referenzlaeufe/Importproben — nur von Hand, siehe Klassenkopf.")]
        public void Erzeuger_schreibt_die_Proben()
        {
            string ordner = Ordner();
            foreach (KeyValuePair<string, byte[]> p in IfcProbenErzeuger.Alle())
                File.WriteAllBytes(Path.Combine(ordner, p.Key), p.Value);
        }

        [Fact]
        public void Die_abgelegten_Proben_sind_byte_gleich_neu_erzeugbar()
        {
            string ordner = Ordner();
            var funde = new List<string>();
            foreach (KeyValuePair<string, byte[]> p in IfcProbenErzeuger.Alle())
            {
                string pfad = Path.Combine(ordner, p.Key);
                if (!File.Exists(pfad)) { funde.Add(p.Key + ": fehlt"); continue; }
                byte[] abgelegt = File.ReadAllBytes(pfad);
                if (p.Key.EndsWith(".ifczip", StringComparison.Ordinal))
                {
                    (string name, byte[] inhalt) a = Eintrag(abgelegt), n = Eintrag(p.Value);
                    if (a.name != n.name || !a.inhalt.SequenceEqual(n.inhalt)) funde.Add(p.Key + ": Eintrag weicht ab");
                }
                else if (!abgelegt.SequenceEqual(p.Value))
                    funde.Add(p.Key + ": " + abgelegt.Length + " Byte abgelegt, " + p.Value.Length + " Byte neu erzeugt");
            }
            Assert.True(funde.Count == 0, "Die Proben weichen vom Erzeuger ab (neu erzeugen, siehe Klassenkopf):\n" + string.Join("\n", funde));
        }

        [Fact]
        public void Die_Proben_sind_klein()
        {
            foreach (string pfad in Directory.GetFiles(Ordner(), "ifc*"))
            {
                var info = new FileInfo(pfad);
                Assert.True(info.Length < 1024 * 1024, info.Name + " ist " + info.Length + " Byte groß.");
                if (info.Name == "ifc4_verlust.ifc") Assert.True(info.Length < 5 * 1024, "Die Kleinstdatei ist größer als 5 KB.");
            }
        }

        private static (string, byte[]) Eintrag(byte[] zip)
        {
            using (var a = new ZipArchive(new MemoryStream(zip), ZipArchiveMode.Read))
            {
                ZipArchiveEntry e = a.Entries.Single();
                using (Stream s = e.Open())
                using (var m = new MemoryStream())
                {
                    s.CopyTo(m);
                    return (e.FullName, m.ToArray());
                }
            }
        }

        internal static string Ordner([CallerFilePath] string eigeneDatei = null)
        {
            string o = Path.GetDirectoryName(eigeneDatei);
            while (o != null && !File.Exists(Path.Combine(o, "WP-Plan.Kern.slnf")))
                o = Path.GetDirectoryName(o);
            Assert.True(o != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return Path.Combine(o, "Referenzlaeufe", "Importproben");
        }
    }
}
