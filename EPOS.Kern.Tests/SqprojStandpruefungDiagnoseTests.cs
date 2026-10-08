using System;
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
    /// <b>Diagnose der Standprüfung an der Anwenderdatei</b> unter <c>Quellen/</c> (Anwenderentscheid E108 vom 08.10.2026): IFC und
    /// Projektdatei derselben Datei stammen aus demselben Projektstand — die Prüfung schlägt nicht an, kein Bauteil der Hülle weicht
    /// über die Schwelle ab, kein Anzeichen ist belegt, die Aufbauquelle steht ohne Wahl auf „IFC“. Die U-Werte des Satzes treffen
    /// auf dem Weg „IFC + Projektdatei“ den Weg „nur Projektdatei“ auf 1 %, mit der Wahl „IFC“ wie mit der Wahl „Projektdatei“.
    /// Die Datei wird nur gelesen; der Test nennt keine Werte oder Namen der Datei, nur relative Größen; ohne Datei übersprungen
    /// (Vorbild <see cref="SqprojQuelldateiImportDiagnoseTests"/>).
    /// </summary>
    public sealed class SqprojStandpruefungDiagnoseTests : IDisposable
    {
        private const string STAMM = "Sportheim_1970_unsaniert";
        private const double TOLERANZ = 0.01;

        private readonly ITestOutputHelper _aus;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public SqprojStandpruefungDiagnoseTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        private static string Wurzel([CallerFilePath] string eigeneDatei = null)
        {
            string o = Path.GetDirectoryName(eigeneDatei);
            while (o != null && !File.Exists(Path.Combine(o, "WP-Plan.Kern.slnf")))
                o = Path.GetDirectoryName(o);
            return o;
        }

        /// <summary>Der Hauptbaum zu einem Worktree (<c>.git</c> als Datei <c>gitdir: …/.git/worktrees/…</c>); sonst <c>null</c>.</summary>
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

        private static readonly string[] U_FELDER =
        {
            GebaeudeZielfelder.U_AUSSENWAND, GebaeudeZielfelder.U_DACH, GebaeudeZielfelder.U_GRUND, GebaeudeZielfelder.U_FENSTER,
        };

        private static double U(GebaeudeImportAblauf a, string feld) => a.Zuordnen(0, null).Zeile(feld)?.Wert ?? 0.0;

        private static string Prozent(double x) => (100.0 * x).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + " %";

        [Fact]
        public void Die_Pruefung_schlaegt_nicht_an_und_die_U_Werte_treffen_beide_Wege()
        {
            string sq = Pfad(STAMM + ".sqproj"), ifc = Pfad(STAMM + ".ifc");
            if (sq == null || ifc == null)
            {
                _aus.WriteLine("Anwenderdatei fehlt — übersprungen.");
                return;
            }
            GebaeudeImportAblauf a = Lesen(ifc);
            SqprojStand stand;
            using (var f = new FileStream(sq, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                stand = a.ProjektdateiLesen(f, Path.GetFileName(sq), 0);
            Assert.False(stand.Abgelehnt, stand.Ablehnung?.ToString());
            Standpruefung p = stand.Standpruefung;
            Assert.NotNull(p);
            _aus.WriteLine("abweichender Flächenanteil: " + Prozent(p.Anteil) + "; Bauteile abweichend/verglichen: "
                           + (p.Verglichen > 0 ? Prozent((double)p.Abweichend / p.Verglichen) : "—")
                           + "; Anzeichen: " + string.Join(", ", p.Anzeichen));
            foreach (StandpruefungArt art in p.JeArt)
                _aus.WriteLine("  " + art.Art + ": abweichend/verglichen " + Prozent((double)art.Abweichend / Math.Max(art.Verglichen, 1))
                               + ", Median U Projektdatei gegen IFC " + Prozent(art.MedianUProjektdatei / art.MedianUIfc - 1.0));
            Assert.False(p.Angeschlagen);
            Assert.True(p.Verglichen > 0);
            Assert.True(p.Anteil <= Standpruefung.FLAECHEN_SCHWELLE, "Anteil " + Prozent(p.Anteil));
            Assert.Empty(p.Anzeichen);
            Assert.Equal(Aufbauquelle.Ifc, stand.Aufbauquelle);
            Assert.Null(a.Aufbauquellenpruefung());

            GebaeudeImportAblauf nurPd = Lesen(sq);
            double[] uPdWeg = U_FELDER.Select(feld => U(nurPd, feld)).ToArray();
            double[] uIfc = U_FELDER.Select(feld => U(a, feld)).ToArray();
            Assert.True(a.AufbauquelleWaehlen(Aufbauquelle.Projektdatei));
            double[] uProjektdatei = U_FELDER.Select(feld => U(a, feld)).ToArray();
            for (int i = 0; i < U_FELDER.Length; i++)
            {
                Assert.True(uPdWeg[i] > 0.0, U_FELDER[i] + " fehlt beim Weg „nur Projektdatei“");
                double abwIfc = (uIfc[i] - uPdWeg[i]) / uPdWeg[i], abwPd = (uProjektdatei[i] - uPdWeg[i]) / uPdWeg[i];
                _aus.WriteLine(U_FELDER[i] + " gegen Weg „nur Projektdatei“: Wahl IFC " + Prozent(abwIfc) + ", Wahl Projektdatei " + Prozent(abwPd));
                Assert.True(Math.Abs(abwIfc) <= TOLERANZ, U_FELDER[i] + ", Wahl IFC: " + Prozent(abwIfc));
                Assert.True(Math.Abs(abwPd) <= TOLERANZ, U_FELDER[i] + ", Wahl Projektdatei: " + Prozent(abwPd));
            }
        }
    }
}
