using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    [Collection("Testdatenbank")]
    public sealed class AbErkundungTests
    {
        private readonly ITestOutputHelper _aus;
        public AbErkundungTests(ITestOutputHelper aus) => _aus = aus;

        private async Task Lauf(string datei, string regel = "Z4")
        {
            var h = new GebaeudeImportHuelle();
            var gaben = h.Gaben();
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            GebaeudeLesestand st = await lesen(datei, null, CancellationToken.None);
            _aus.WriteLine($"{Path.GetFileName(datei)} {regel} gelesen={st.Gelesen}");
            var zu = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"];
            GebaeudeImportStand s = zu(new GebaeudeZuordnungsanfrage(0, null, new Dictionary<string, bool>(), null, null, regel, null, false, null, null));
            var a = s.Ansicht;
            _aus.WriteLine($"  HatDK={a.HatDateikoerper} Rand={a.RandbedingungWaehlbar} Stufen={a.Bauteilstufen.Count} Befunde={a.Bauteilbefunde.Count} AufbauW={a.AufbauWaehlbar} BefundW={a.BefundWaehlbar}");
            foreach (var k in a.Koerperraeume)
            {
                var je = k.Dreiecksbauteile;
                if (je == null) { _aus.WriteLine($"  {k.Kennung}: DK={k.Dateikoerper != null} ohne Dreiecksbauteile"); continue; }
                int n = je.Count, ohne = je.Count(x => x == null), treffer = je.Count(x => x != null && a.Bauteilstufen.ContainsKey(x));
                _aus.WriteLine($"  {k.Kennung}: n={n} ohne={ohne} treffer={treffer} kennungen={string.Join(",", je.Where(x => x != null).Distinct().Take(8))}");
            }
            _aus.WriteLine("  Stufenschluessel=" + string.Join(",", a.Bauteilstufen.Keys.Take(12)));
        }

        [Fact]
        public async Task Sqproj() => await Lauf(SqprojProbenErzeuger.Geometriehaus().Schreiben(SqprojProbenErzeuger.TempPfad("ab")));

        [Fact]
        public async Task SqprojX1() { await Lauf(SqprojProbenErzeuger.Geometriehaus().Schreiben(SqprojProbenErzeuger.TempPfad("ab")), "X1"); await Lauf(SqprojProbenErzeuger.Geometriehaus().Schreiben(SqprojProbenErzeuger.TempPfad("ab")), "X3"); }

        [Fact]
        public async Task Gbxml() => await Lauf(GbxmlImportTests.Probe("gbxml_g5_flaechen.xml"));

        [Fact]
        public async Task Ifc() => await Lauf(GbxmlImportTests.Probe("ifc4_koerper_bauteile.ifc"));
    }
}
