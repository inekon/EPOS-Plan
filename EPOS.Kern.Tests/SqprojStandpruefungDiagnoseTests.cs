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
    /// <b>Diagnose der Standprüfung an der Anwenderdatei</b> unter <c>Quellen/</c> (Anwenderentscheid vom 08.10.2026): IFC und
    /// Projektdatei derselben Datei stammen aus zwei Projektständen — die Prüfung schlägt an, mehr als die Hälfte der Hüllfläche
    /// weicht ab, die Projektdatei ist eine Kopie nach dem Modellstand der IFC. Mit der Wahl „Projektdatei“ trifft das U der
    /// Außenwand im Satz den Weg „nur Projektdatei“ auf 1 %, mit „IFC“ bleibt es beim heutigen Stand. Die Datei wird nur gelesen;
    /// der Test nennt keine Werte oder Namen der Datei, nur relative Größen; ohne Datei übersprungen (Vorbild
    /// <see cref="SqprojQuelldateiImportDiagnoseTests"/>).
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

        private static double UAussenwand(GebaeudeImportAblauf a) => a.Zuordnen(0, null).Zeile(GebaeudeZielfelder.U_AUSSENWAND)?.Wert ?? 0.0;

        private static string Prozent(double x) => (100.0 * x).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + " %";

        [Fact]
        public void Die_Pruefung_schlaegt_an_und_die_Wahl_trifft_beide_Wege()
        {
            string sq = Pfad(STAMM + ".sqproj"), ifc = Pfad(STAMM + ".ifc");
            if (sq == null || ifc == null)
            {
                _aus.WriteLine("Anwenderdatei fehlt — übersprungen.");
                return;
            }
            GebaeudeImportAblauf a = Lesen(ifc);
            double uHeute = UAussenwand(a);
            SqprojStand stand;
            using (var f = new FileStream(sq, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                stand = a.ProjektdateiLesen(f, Path.GetFileName(sq), 0);
            Assert.False(stand.Abgelehnt, stand.Ablehnung?.ToString());
            Standpruefung p = stand.Standpruefung;
            Assert.NotNull(p);
            _aus.WriteLine("abweichender Flächenanteil: " + Prozent(p.Anteil) + "; Bauteile abweichend/verglichen: "
                           + (p.Verglichen > 0 ? Prozent((double)p.Abweichend / p.Verglichen) : "—")
                           + "; Anzeichen: " + string.Join(", ", p.Anzeichen));
            Assert.True(p.Angeschlagen);
            Assert.True(p.Anteil > 0.5, "Anteil " + Prozent(p.Anteil));
            Assert.Contains(Standanzeichen.KopieNachModellstand, p.Anzeichen);
            Assert.Equal(Aufbauquelle.Offen, stand.Aufbauquelle);
            Assert.NotNull(a.Aufbauquellenpruefung());

            double uPdWeg = UAussenwand(Lesen(sq));
            Assert.True(a.AufbauquelleWaehlen(Aufbauquelle.Projektdatei));
            double uProjektdatei = UAussenwand(a);
            Assert.True(a.AufbauquelleWaehlen(Aufbauquelle.Ifc));
            double uIfc = UAussenwand(a);
            double abwPd = (uProjektdatei - uPdWeg) / uPdWeg, abwIfc = (uIfc - uHeute) / uHeute;
            _aus.WriteLine("U Außenwand, Wahl Projektdatei gegen Weg „nur Projektdatei“: " + Prozent(abwPd));
            _aus.WriteLine("U Außenwand, Wahl IFC gegen heute: " + Prozent(abwIfc)
                           + "; Wahl Projektdatei gegen IFC: " + Prozent((uProjektdatei - uIfc) / uIfc));
            Assert.True(Math.Abs(abwPd) <= TOLERANZ, "Projektdatei: " + Prozent(abwPd));
            Assert.Equal(uHeute, uIfc, 12);
        }
    }
}
