using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace ZapfprofilValidierung.Tests
{
    /// <summary>
    /// <b>Der Ordner der nicht ausgelieferten Messreihen</b>
    /// (<c>Referenzlaeufe/Messreihen_INEKON/</c>). Bauform wie <c>EPOS.Kern.Tests/Normzahlen</c>:
    /// Suche aufwärts, Kennzeichen <see cref="Vorhanden"/>, und der Fall darüber beginnt mit
    /// <c>if (!Vorhanden) return;</c>.
    ///
    /// <para><b>Warum lokal und gitignoriert.</b> Messreihen echter Objekte sind Objektdaten und
    /// gehören nie ins Repositorium (Konzept Kapitel 9 K5). Versioniert ist dort allein das
    /// <c>LIESMICH.md</c>, das sagt, was der Anwender wohin legt.</para>
    /// </summary>
    internal sealed class Messreihenordner
    {
        /// <summary>Der Ordner relativ zur Repowurzel.</summary>
        internal static readonly string[] Ordnerpfad = { "Referenzlaeufe", "Messreihen_INEKON" };

        internal Messreihenordner() : this(AppContext.BaseDirectory) { }

        internal Messreihenordner(string startordner)
        {
            Ordner = Suchen(startordner);
            if (Ordner == null) return;
            Objektordner = Directory.EnumerateDirectories(Ordner)
                                    .Where(d => File.Exists(Path.Combine(d, Einstieg.OBJEKTDATEI)))
                                    .OrderBy(d => Path.GetFileName(d), StringComparer.Ordinal)
                                    .ToList();
            Vorhanden = Objektordner.Count > 0;
        }

        /// <summary>Liegen Messobjekte? Sonst schweigt der Fall.</summary>
        internal bool Vorhanden { get; }

        /// <summary>Der gefundene Ordner, oder <c>null</c>.</summary>
        internal string Ordner { get; }

        /// <summary>Die Objektordner darin.</summary>
        internal List<string> Objektordner { get; } = new List<string>();

        /// <summary>Suche aufwärts, höchstens zehn Ebenen.</summary>
        internal static string Suchen(string startordner)
        {
            var d = new DirectoryInfo(startordner);
            for (int i = 0; i < 10 && d != null; i++, d = d.Parent)
            {
                string kandidat = Path.Combine(new[] { d.FullName }.Concat(Ordnerpfad).ToArray());
                if (Directory.Exists(kandidat)) return kandidat;
            }
            return null;
        }
    }

    /// <summary>
    /// Der Lauf gegen die <b>lokalen</b> Messreihen des Anwenders und die Proben des vierten
    /// Kriteriums (der √N-Skalierung über alle Objekte).
    /// </summary>
    public sealed class MessreihenordnerTests
    {
        [Fact]
        public void Die_lokalen_Messreihen_laufen_durch_und_der_Bericht_bleibt_frei_von_Absolutwerten()
        {
            var ordner = new Messreihenordner();
            if (!ordner.Vorhanden) return;      // in der CI und bei jedem, der keine Reihen hat

            using var v = new Vorrichtung();
            string ziel = v.Neu("inekon");
            (int code, string aus, string fehler) = v.Lauf(ordner.Ordner, "--ziel", ziel);

            // Ein rotes Objekt ist ein BEFUND, kein Testfehler - die Validierung darf rot sein. Was
            // nicht sein darf: ein Aufruffehler, ein Schreibfehler oder ein Fund der Berichtswache.
            Assert.True(code == Einstieg.OK || code == Einstieg.NICHT_ABGENOMMEN,
                "Rueckgabe " + code + Environment.NewLine + aus + Environment.NewLine + fehler);
            Assert.True(File.Exists(Path.Combine(ziel, Einstieg.SAMMEL_MD)), "Der Sammelbericht fehlt.");
            Assert.Equal(ordner.Objektordner.Count,
                         Directory.EnumerateFiles(ziel, "*.md").Count() - 1);
        }

        [Fact]
        public void Der_Ordner_wird_aufwaerts_gefunden_und_nur_mit_Objekten_gemeldet()
        {
            using var v = new Vorrichtung();
            string baum = v.Neu("baum");
            string tief = Path.Combine(baum, "a", "b");
            Directory.CreateDirectory(tief);
            string reihen = Path.Combine(baum, "Referenzlaeufe", "Messreihen_INEKON");
            Directory.CreateDirectory(reihen);

            var leer = new Messreihenordner(tief);
            Assert.Equal(reihen, leer.Ordner);
            Assert.False(leer.Vorhanden);     // ein Ordner ohne Objekt ist kein Bestand

            string objekt = Path.Combine(reihen, "OBJ-1");
            Directory.CreateDirectory(objekt);
            File.WriteAllText(Path.Combine(objekt, Einstieg.OBJEKTDATEI), "{}");
            var mit = new Messreihenordner(tief);
            Assert.True(mit.Vorhanden);
            Assert.Single(mit.Objektordner);

            Assert.Null(Messreihenordner.Suchen(Path.GetTempPath()));
        }

        [Fact]
        public void Das_vierte_Kriterium_braucht_drei_Objekte_mit_verschiedenem_N()
        {
            Kriterium keins = Sammelkriterium.Bilden(new Objektbefund[0]);
            Assert.Equal(Ampel.Gelb, keins.Ampel);
            Assert.Null(keins.Mass);

            // Drei Objekte, deren Spitzenverhaeltnis genau wie 1/Wurzel N faellt: Steigung -0,5.
            var gut = new[] { Befund(4, 0.5), Befund(16, 0.25), Befund(64, 0.125) };
            Kriterium gruen = Sammelkriterium.Bilden(gut);
            Assert.Equal(Ampel.Gruen, gruen.Ampel);
            Assert.Equal(Sammelkriterium.STEIGUNG_SOLL, gruen.Mass.Value, 6);

            // Drei Objekte ohne Zusammenhang: Steigung 0 - ausserhalb des Bands.
            var flach = new[] { Befund(4, 0.9), Befund(16, 0.9), Befund(64, 0.9) };
            Kriterium rot = Sammelkriterium.Bilden(flach);
            Assert.Equal(Ampel.Rot, rot.Ampel);
            Assert.Equal(0.0, rot.Mass.Value, 9);

            // Gleiche Einheitenzahl: die Steigung ist nicht bildbar.
            Kriterium gleich = Sammelkriterium.Bilden(new[] { Befund(16, 0.9), Befund(16, 0.8), Befund(16, 0.7) });
            Assert.Equal(Ampel.Gelb, gleich.Ampel);
            Assert.Null(gleich.Mass);

            // Zwei Objekte genuegen nicht.
            Assert.Equal(Ampel.Gelb, Sammelkriterium.Bilden(new[] { Befund(4, 0.5), Befund(16, 0.25) }).Ampel);

            // Ein Objekt mit Abbruch zaehlt nicht mit.
            var mitAbbruch = new List<Objektbefund>(gut) { new Objektbefund { Abbruch = "kein Katalog" } };
            Assert.Equal(Ampel.Gruen, Sammelkriterium.Bilden(mitAbbruch).Ampel);
        }

        private static Objektbefund Befund(int einheiten, double spitzenverhaeltnis)
            => new Objektbefund { Einheiten = einheiten, Spitzenverhaeltnis = spitzenverhaeltnis };
    }
}
