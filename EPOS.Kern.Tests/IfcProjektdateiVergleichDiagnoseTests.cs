using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Diagnose der Korrektur G5-3d an der Anwenderdatei</b> unter <c>Quellen/</c> (IFC-Weg gegen den Weg allein aus der
    /// Projektdatei derselben Datei), nur relativ — der Test nennt keine Werte, Namen oder Bezeichnungen der Datei:
    /// <list type="bullet">
    /// <item><b>Im IFC-Weg:</b> Führt die Datei Platten gegen unbeheizt, zählt keine Trenndecke aus den Raumkörpern in die
    /// Grundfläche des Einzonensatzes; die Außenwand netto ist brutto minus Öffnungen (2 %), keine Wand fällt auf die
    /// Nettofläche der Datei zurück.</item>
    /// <item><b>Gegen die Projektdatei:</b> Grundfläche des Einzonensatzes innerhalb 5 %, Außenwand netto innerhalb 2 % —
    /// nur, wo der Stand die Projektdatei als Gebäudequelle liest (<see cref="GebaeudeImportProfil.FuerDatei"/>), sonst
    /// übersprungen.</item>
    /// </list>
    /// Die Datei wird nur gelesen; fehlt sie (sie liegt nicht im Repositorium), wird übersprungen; in einem Worktree gilt
    /// der Ordner <c>Quellen/</c> des Hauptbaums.
    /// </summary>
    public sealed class IfcProjektdateiVergleichDiagnoseTests : IDisposable
    {
        private const string STAMM = "Sportheim_1970_unsaniert";
        private const double GRUND_TOLERANZ = 0.05, WAND_TOLERANZ = 0.02;

        private readonly ITestOutputHelper _aus;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public IfcProjektdateiVergleichDiagnoseTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        private static string Wurzel([CallerFilePath] string eigeneDatei = null)
        {
            string o = Path.GetDirectoryName(eigeneDatei);
            while (o != null && !File.Exists(Path.Combine(o, "WP-Plan.Kern.slnf")))
                o = Path.GetDirectoryName(o);
            return o;
        }

        /// <summary>Der Hauptbaum zu einem Worktree (<c>.git</c> ist dort eine Datei mit <c>gitdir:</c>); sonst <c>null</c>.</summary>
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

        private static GebaeudeImportAblauf Lesen(string pfad, GebaeudeImportProfil profil)
        {
            var a = new GebaeudeImportAblauf();
            using (var f = new FileStream(pfad, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                Assert.True(a.Lesen(f, pfad, profil) > 0, string.Join(" | ", a.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            return a;
        }

        private static List<Huellposten> Posten(GebaeudeImportAblauf a, string summenfeld)
            => GebaeudeHuelleneinordnung.Einordnen(a.Abbild, 0, r => r.Beheizt).Huelle
                   .Where(p => !p.Verworfen && p.Summenfeld == summenfeld).ToList();

        private static double Abweichung(double ifc, double datei)
            => Math.Abs(ifc) < 1e-9 ? (Math.Abs(datei) < 1e-9 ? 0.0 : double.PositiveInfinity) : (datei - ifc) / ifc;

        private static string Prozent(double x) => (100.0 * x).ToString("+0.00;-0.00;0.00", CultureInfo.InvariantCulture) + " %";

        [Fact]
        public void Grundflaeche_und_Aussenwand_netto_gegen_die_Projektdatei()
        {
            string ifc = Pfad(STAMM + ".ifc");
            if (ifc == null)
            {
                _aus.WriteLine("Anwenderdatei fehlt — übersprungen.");
                return;
            }
            GebaeudeImportAblauf ib = Lesen(ifc, GebaeudeImportProfil.FuerDatei(ifc));
            AbbildGebaeude g = ib.Abbild.Gebaeude[0];

            // 1. Grundfläche: Die Platten gegen unbeheizt gehen den Körperdecken vor.
            Assert.Contains(g.Bauteile, b => string.Equals(b.Quelltyp, "IfcSlab", StringComparison.OrdinalIgnoreCase)
                                             && b.Randbedingung == Randbedingung.Unbeheizt && b.Grenzen.Count == 0);
            List<Huellposten> iGrund = Posten(ib, GebaeudeZielfelder.FLAECHE_GRUND);
            Assert.DoesNotContain(iGrund, p => p.Bauteil.Trenndeckenherkunft == AbbildBauteil.TRENNDECKE_KOERPER);

            // 2. Außenwand: netto = brutto − Öffnungen, ohne Rückfall auf die Nettofläche der Datei.
            List<Huellposten> iWand = Posten(ib, GebaeudeZielfelder.FLAECHE_AUSSENWAND);
            double brutto = iWand.Sum(p => p.BruttoM2 ?? 0.0), abzug = iWand.Sum(p => p.AbzugM2), netto = iWand.Sum(p => p.NettoM2 ?? 0.0);
            _aus.WriteLine("IFC: Außenwand netto gegen brutto − Öffnungen " + Prozent(Abweichung(brutto - abzug, netto))
                           + ", Wände mit Rückfall " + iWand.Count(p => p.NettoRueckfall));
            Assert.DoesNotContain(iWand, p => p.NettoRueckfall || p.NettoNegativ);
            Assert.True(Math.Abs(Abweichung(brutto - abzug, netto)) <= WAND_TOLERANZ, "netto gegen brutto − Öffnungen");

            // 3. Gegen die Projektdatei, wo der Stand sie als Gebäudequelle liest.
            string sq = Pfad(STAMM + ".sqproj");
            GebaeudeImportProfil profil = sq == null ? null : GebaeudeImportProfil.FuerDatei(sq);
            if (profil == null)
            {
                _aus.WriteLine("Projektdatei fehlt oder wird in diesem Stand nicht als Gebäudequelle gelesen — Vergleich übersprungen.");
                return;
            }
            GebaeudeImportAblauf pa = Lesen(sq, profil);
            double Grund(GebaeudeImportAblauf x) => x.Zuordnen(0, null).Zeile(GebaeudeZielfelder.FLAECHE_GRUND)?.Wert ?? 0.0;
            double grund = Abweichung(Grund(ib), Grund(pa));
            double wand = Abweichung(netto, Posten(pa, GebaeudeZielfelder.FLAECHE_AUSSENWAND).Sum(p => p.NettoM2 ?? 0.0));
            _aus.WriteLine("Grundfläche " + Prozent(grund) + ", Außenwand netto " + Prozent(wand));
            Assert.True(Math.Abs(grund) <= GRUND_TOLERANZ, "Grundfläche " + Prozent(grund));
            Assert.True(Math.Abs(wand) <= WAND_TOLERANZ, "Außenwand netto " + Prozent(wand));
        }
    }
}
