using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Diagnose des Imports allein aus der Projektdatei an der Anwenderdatei</b> unter <c>Quellen/</c>: Dieselbe Datei
    /// einmal über den IFC-Weg und einmal allein aus ihrer Projektdatei gelesen — beheizte Fläche, beheiztes Volumen,
    /// Fensterfläche und Fläche gegen Erdreich stimmen je innerhalb 1 % überein. Die Anwenderdatei wird nur gelesen; der Test
    /// nennt keine Werte, Namen oder Bezeichnungen der Datei, nur die relativen Abweichungen. Fehlt die Datei (sie liegt
    /// nicht im Repositorium), wird übersprungen; in einem Worktree gilt der Ordner <c>Quellen/</c> des Hauptbaums.
    /// </summary>
    public sealed class SqprojQuelldateiImportDiagnoseTests : IDisposable
    {
        private const string STAMM = "Sportheim_1970_unsaniert";
        private const double TOLERANZ = 0.01;

        private readonly ITestOutputHelper _aus;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public SqprojQuelldateiImportDiagnoseTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        /// <summary>Die Wurzel des Arbeitsbaums dieser Datei (der Ordner mit <c>WP-Plan.Kern.slnf</c>).</summary>
        private static string Wurzel([CallerFilePath] string eigeneDatei = null)
        {
            string o = Path.GetDirectoryName(eigeneDatei);
            while (o != null && !File.Exists(Path.Combine(o, "WP-Plan.Kern.slnf")))
                o = Path.GetDirectoryName(o);
            return o;
        }

        /// <summary>
        /// Der Hauptbaum zu einem Worktree: <c>.git</c> ist dort eine Datei <c>gitdir: &lt;haupt&gt;/.git/worktrees/&lt;name&gt;</c>;
        /// <c>null</c> im Hauptbaum selbst oder ohne lesbaren Verweis.
        /// </summary>
        private static string Hauptbaum(string wurzel)
        {
            string datei = wurzel == null ? null : Path.Combine(wurzel, ".git");
            if (datei == null || !File.Exists(datei)) return null;
            string zeile = File.ReadLines(datei).FirstOrDefault() ?? "";
            const string PRAEFIX = "gitdir:";
            if (!zeile.StartsWith(PRAEFIX, StringComparison.Ordinal)) return null;
            string gitdir = zeile.Substring(PRAEFIX.Length).Trim();
            int i = gitdir.Replace('\\', '/').LastIndexOf("/.git/worktrees/", StringComparison.Ordinal);
            return i < 0 ? null : gitdir.Substring(0, i);
        }

        /// <summary>Eine Anwenderdatei unter <c>Quellen/</c> dieses Baums, sonst des Hauptbaums; <c>null</c>, wenn sie fehlt.</summary>
        private static string Pfad(string datei)
        {
            string wurzel = Wurzel();
            foreach (string baum in new[] { wurzel, Hauptbaum(wurzel) })
            {
                if (baum == null) continue;
                string pfad = Path.Combine(baum, "Quellen", datei);
                if (File.Exists(pfad) && new FileInfo(pfad).Length >= 1024) return pfad;
            }
            return null;
        }

        private static GebaeudeImportAblauf Lesen(string pfad)
        {
            var a = new GebaeudeImportAblauf();
            using (var f = new FileStream(pfad, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                Assert.True(a.Lesen(f, pfad, GebaeudeImportProfil.FuerDatei(pfad)) > 0,
                            string.Join(" | ", a.Meldungen.Where(m => m.Stufe == SpeicherEngine.PruefStufe.Fehler)));
            return a;
        }

        /// <summary>Die vier Vergleichsgrößen des Gebäudes 0: beheizte Fläche, beheiztes Volumen, Fensterfläche des Satzes, Fläche gegen Erdreich.</summary>
        private static (double Flaeche, double Volumen, double Fenster, double Erdreich) Messen(GebaeudeImportAblauf a)
        {
            AbbildGebaeude g = a.Abbild.Gebaeude[0];
            List<AbbildRaum> warm = g.Raeume.Where(r => r.Beheizt).ToList();
            GebaeudeImportSatz satz = a.Zuordnen(0, null);
            double Feld(string f) => satz.Zeile(f)?.Wert ?? 0.0;
            // Erdreich: die Hüllfläche gegen Erdreich (Bodenplatten und Wände am Erdreich, brutto).
            double erdreich = g.Bauteile.Where(b => b.Randbedingung == Randbedingung.Erdreich).Sum(b => b.BruttoflaecheM2 ?? 0.0);
            return (warm.Sum(r => r.FlaecheM2 ?? 0.0), warm.Sum(r => r.VolumenM3 ?? 0.0), Feld(GebaeudeZielfelder.FENSTER_GESAMT), erdreich);
        }

        private static double Abweichung(double ifc, double sqproj)
            => Math.Abs(ifc) < 1e-9 ? (Math.Abs(sqproj) < 1e-9 ? 0.0 : double.PositiveInfinity) : (sqproj - ifc) / ifc;

        [Fact]
        public void Projektdatei_allein_trifft_den_IFC_Weg_derselben_Datei()
        {
            string sq = Pfad(STAMM + ".sqproj"), ifc = Pfad(STAMM + ".ifc");
            if (sq == null || ifc == null)
            {
                _aus.WriteLine("Anwenderdatei fehlt — übersprungen.");
                return;
            }
            GebaeudeImportAblauf ib = Lesen(ifc);
            var ia = Messen(ib);
            GebaeudeImportAblauf pa = Lesen(sq);
            Assert.True(pa.AlleinAusProjektdatei);
            var pm = Messen(pa);
            var abweichungen = new (string Groesse, double Wert)[]
            {
                ("beheizte Fläche", Abweichung(ia.Flaeche, pm.Flaeche)),
                ("beheiztes Volumen", Abweichung(ia.Volumen, pm.Volumen)),
                ("Fenster", Abweichung(ia.Fenster, pm.Fenster)),
                ("Erdreich", Abweichung(ia.Erdreich, pm.Erdreich)),
            };
            // Nur zur Auskunft (keine Abnahmegröße): die Grundfläche des Einzonenwegs, sie hängt an der Einordnung der Decken zu
            // unbeheizten Räumen.
            double Grund(GebaeudeImportAblauf x) => x.Zuordnen(0, null).Zeile(GebaeudeZielfelder.FLAECHE_GRUND)?.Wert ?? 0.0;
            _aus.WriteLine("Grundfläche des Einzonenwegs (Auskunft): "
                           + (100.0 * Abweichung(Grund(ib), Grund(pa))).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + " %");
            foreach ((string groesse, double wert) in abweichungen)
                _aus.WriteLine(groesse + ": " + (100.0 * wert).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + " %");
            Assert.All(abweichungen, x => Assert.True(Math.Abs(x.Wert) <= TOLERANZ,
                x.Groesse + ": " + (100.0 * x.Wert).ToString("0.00", CultureInfo.InvariantCulture) + " % > 1 %"));
        }
    }
}
