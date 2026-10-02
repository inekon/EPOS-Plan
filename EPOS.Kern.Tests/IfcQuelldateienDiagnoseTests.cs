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
    /// <b>Diagnose des IFC-Imports an Anwenderdateien unter <c>Quellen/*.ifc</c></b>: schreibt je Datei
    /// Schema, Gebäude, Räume (beheizt), Fläche, Volumen, Bauteile und die Meldungen von Leser und
    /// Bauteilvorschlag ins Testprotokoll. Die Dateien sind Anwenderdaten und können fehlen — dann
    /// endet der Test ohne Prüfung. Die Regel selbst hält die synthetische Probe
    /// <c>ifc2x3_enthaltensein.ifc</c>.
    /// </summary>
    public sealed class IfcQuelldateienDiagnoseTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();
        private readonly ITestOutputHelper _aus;

        public IfcQuelldateienDiagnoseTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        private static string Quellen([CallerFilePath] string eigeneDatei = null)
        {
            string o = Path.GetDirectoryName(eigeneDatei);
            while (o != null && !File.Exists(Path.Combine(o, "WP-Plan.Kern.slnf")))
                o = Path.GetDirectoryName(o);
            return o == null ? null : Path.Combine(o, "Quellen");
        }

        private static string Z(double? w) => w.HasValue ? w.Value.ToString("0.##", CultureInfo.InvariantCulture) : "—";

        private static string Text(IEnumerable<PruefMeldung> meldungen)
            => string.Join(" | ", meldungen.Select(m => m.Stufe + " " + m.Schluessel + "(" + string.Join(";", m.Werte) + ")"));

        [Fact]
        public void Quelldateien_IFC_werden_gelesen_und_protokolliert()
        {
            string ordner = Quellen();
            if (ordner == null || !Directory.Exists(ordner)) { _aus.WriteLine("Quellen/ fehlt — übersprungen."); return; }
            string[] dateien = Directory.GetFiles(ordner, "*.ifc").OrderBy(d => d, StringComparer.Ordinal).ToArray();
            if (dateien.Length == 0) { _aus.WriteLine("Keine Quellen/*.ifc — übersprungen."); return; }

            foreach (string pfad in dateien)
            {
                // Eine Zeigerdatei von Git LFS ist keine IFC-Datei.
                if (new FileInfo(pfad).Length < 1024) { _aus.WriteLine(Path.GetFileName(pfad) + ": zu klein — übersprungen."); continue; }
                var a = new GebaeudeImportAblauf();
                using (FileStream s = File.OpenRead(pfad))
                    a.Lesen(s, pfad, new IfcImportProfil());
                _aus.WriteLine("=== " + Path.GetFileName(pfad) + " — Schema " + a.Quelle?.Schemastand);
                _aus.WriteLine("Leser: " + Text(a.Meldungen));
                if (a.Abbild == null || a.Abbild.Gebaeude.Count == 0) { _aus.WriteLine("kein Gebäude"); continue; }
                for (int gi = 0; gi < a.Abbild.Gebaeude.Count; gi++)
                {
                    AbbildGebaeude g = a.Abbild.Gebaeude[gi];
                    List<AbbildRaum> beheizt = g.Raeume.Where(r => r.Beheizt).ToList();
                    _aus.WriteLine("Gebäude " + g.Anzeigename + ": Geschosse " + g.Geschosse.Count + ", Räume " + g.Raeume.Count
                                   + ", beheizt " + beheizt.Count
                                   + ", Fläche beheizt " + Z(beheizt.Sum(r => r.FlaecheM2 ?? 0)) + " m², Volumen beheizt "
                                   + Z(beheizt.Sum(r => r.VolumenM3 ?? 0)) + " m³, Fläche alle " + Z(g.Raeume.Sum(r => r.FlaecheM2 ?? 0))
                                   + " m², Bauteile " + g.Bauteile.Count);
                    foreach (IGrouping<string, AbbildRaum> gr in g.Raeume.GroupBy(r => (r.Name ?? "?") + (r.Beheizt ? " [beheizt " : " [unbeheizt ") + r.BeheiztQuelle + "]"))
                        _aus.WriteLine("  " + gr.Count() + "× " + gr.Key + ", Fläche " + Z(gr.Sum(r => r.FlaecheM2 ?? 0))
                                       + " m², Volumen " + Z(gr.Sum(r => r.VolumenM3 ?? 0)) + " m³");
                    _aus.WriteLine("Gebäudemeldungen: " + Text(g.Meldungen));

                    GebaeudeImportSatz satz = a.Zuordnen(gi, null);
                    _aus.WriteLine("Satz: Nutzfläche " + Z(satz.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert) + " m², Volumen "
                                   + Z(satz.Zeile(GebaeudeZielfelder.VOLUMEN).Wert) + " m³, Raumhöhe "
                                   + Z(satz.Zeile(GebaeudeZielfelder.RAUMHOEHE).Wert) + " m, Außenwand "
                                   + Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND).Wert) + " m², Dach "
                                   + Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_DACH).Wert) + " m², Fenster "
                                   + Z(satz.Zeile(GebaeudeZielfelder.FENSTER_GESAMT).Wert) + " m²");

                    GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, gi, null);
                    _aus.WriteLine("Vorschlag: " + Text(v.Meldungen));
                }
            }
        }
    }
}
