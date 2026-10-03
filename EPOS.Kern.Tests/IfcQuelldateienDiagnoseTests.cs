using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using EPOS.UI.Dialoge.Import;
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
                                   + Z(satz.Zeile(GebaeudeZielfelder.FENSTER_GESAMT).Wert) + " m², Grund "
                                   + Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_GRUND).Wert) + " m², Sonstige "
                                   + Z(satz.Zeile(GebaeudeZielfelder.FLAECHE_SONSTIGE).Wert) + " m²");
                    _aus.WriteLine("U-Werte: Außenwand " + Z(satz.Zeile(GebaeudeZielfelder.U_AUSSENWAND).Wert) + ", Dach "
                                   + Z(satz.Zeile(GebaeudeZielfelder.U_DACH).Wert) + ", Fenster "
                                   + Z(satz.Zeile(GebaeudeZielfelder.U_FENSTER).Wert) + ", Grund "
                                   + Z(satz.Zeile(GebaeudeZielfelder.U_GRUND).Wert) + ", Sonstige "
                                   + Z(satz.Zeile(GebaeudeZielfelder.U_SONSTIGE).Wert) + " W/(m²K)");

                    GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, gi, null);
                    // Je Gruppe (Summenfeld, Randbedingung): Zahl der Zeilen, Fläche und flächengewichteter U-Wert.
                    foreach (var gr in v.Zeilen.GroupBy(z => (z.Summenfeld ?? "IW") + " " + (z.Bauteil.Randbedingung ?? "(innen)"))
                                               .OrderBy(x => x.Key, StringComparer.Ordinal))
                    {
                        double flaeche = gr.Sum(z => z.Bauteil.Flaeche);
                        double mitU = gr.Where(z => z.Bauteil.U_Wert.HasValue).Sum(z => z.Bauteil.Flaeche);
                        double ua = gr.Where(z => z.Bauteil.U_Wert.HasValue).Sum(z => (z.Bauteil.Flaeche) * z.Bauteil.U_Wert.Value);
                        _aus.WriteLine("  Gruppe " + gr.Key + ": " + gr.Count() + " Zeilen, " + Z(flaeche) + " m², U "
                                       + Z(mitU > 0.0 ? ua / mitU : (double?)null) + " W/(m²K) über " + Z(mitU) + " m²");
                    }
                    _aus.WriteLine("Vorschlag: " + Text(v.Meldungen));
                }
            }
        }

        /// <summary>
        /// <b>Die Anwenderdateien mit Schichtdicken in Millimetern</b> unter der Längeneinheit <c>METRE</c>:
        /// Der Leser rechnet die Schichtsätze um und übergeht Folien, jede Schicht liegt danach im Band der
        /// Schichtdicke, und der Bauteilvorschlag läuft mit und ohne Namensabgleich bis zum Ende — ein- und
        /// mehrzonig. Fehlt die Datei (oder liegt nur ein LFS-Zeiger), endet der Fall ohne Prüfung.
        /// </summary>
        [Theory]
        [InlineData("Produktion_groß_mit_Verwaltung_EG55-2026.ifc")]
        [InlineData("MFH-Klein-unsaniert-1964.ifc")]
        public void Schichtdicken_in_Millimetern_laufen_bis_zum_Bauteilvorschlag(string datei)
        {
            string pfad = Quellen() == null ? null : Path.Combine(Quellen(), datei);
            if (pfad == null || !File.Exists(pfad) || new FileInfo(pfad).Length < 1024) { _aus.WriteLine(datei + " fehlt — übersprungen."); return; }
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, new IfcImportProfil());
            Assert.True(a.Abbild != null && a.Abbild.Gebaeude.Count > 0, Text(a.Meldungen));
            // Gebündelt: genau eine Warnung und genau ein Hinweis je Datei.
            PruefMeldung mm = Assert.Single(a.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_SCHICHTDICKE_MM");
            Assert.Equal(PruefStufe.Warnung, mm.Stufe);
            PruefMeldung duenn = Assert.Single(a.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_SCHICHT_DUENN");
            Assert.Equal(PruefStufe.Info, duenn.Stufe);
            _aus.WriteLine(datei + ": " + Text(new[] { mm, duenn }));

            var abgleich = new Baustoffabgleich(BaustoffabgleichDaten.AusSaat());
            for (int gi = 0; gi < a.Abbild.Gebaeude.Count; gi++)
            {
                foreach (AbbildBauteil b in a.Abbild.Gebaeude[gi].Bauteile.Where(x => x.Aufbau != null))
                    foreach (AbbildSchicht x in b.Aufbau.Schichten.Where(x => x.DickeM.HasValue))
                        Assert.True(x.DickeM >= GebaeudeFestwerte.SCHICHT_DICKE_MIN_M && x.DickeM <= GebaeudeFestwerte.SCHICHT_DICKE_MAX_M,
                                    b.Aufbau.Kennung + " " + x.Name + ": d = " + Z(x.DickeM) + " m");
                foreach (Baustoffabgleich mit in new[] { null, abgleich })
                {
                    GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, gi, 'E', null, mit);
                    GebaeudeBauteilvorschlag z = GebaeudeBauteilvorschlag.BildenMitZonen(a, gi, 'E', null, null, mit);
                    Assert.False(v.Abgelehnt, Text(v.Meldungen));
                    Assert.False(z.Abgelehnt, Text(z.Meldungen));

                    // Die Bauteilliste des Dialogs (Katalogliste mit Suche und Trichtern): eine Zeile je Bauteil mit
                    // eindeutigem Schlüssel; Sortierung nach der Fläche und Suche laufen über alle Zeilen.
                    GebaeudeBauteileDaten daten = GebaeudeImportHuelle.BauteileDaten(v);
                    Assert.NotNull(daten.Profil);
                    Assert.Equal(v.Zeilen.Count, daten.Liste.Count);
                    Assert.Equal(daten.Liste.Count, daten.Liste.Select(l => l.Schluessel).Distinct(StringComparer.Ordinal).Count());
                    var sortiert = new Katalogfilterstand { Sortierspalte = GebaeudeImportZonen.SP_FLAECHE, Aufsteigend = false };
                    Assert.Equal(daten.Liste.Count, Katalogfilter.Anwenden(daten.Profil, daten.Liste, sortiert).Count);
                    string erster = daten.Liste[0].Bezeichner;
                    var gesucht = new Katalogfilterstand { Suche = erster };
                    Assert.Contains(Katalogfilter.Anwenden(daten.Profil, daten.Liste, gesucht), l => l.Bezeichner == erster);
                    _aus.WriteLine(datei + (mit == null ? " ohne" : " mit") + " Abgleich: " + v.Aufbauten.Count + " Aufbauten, "
                                   + v.Zeilen.Count + " Zeilen; " + Text(v.Meldungen.Where(m => m.Stufe != PruefStufe.Info)));
                }
            }
        }
    }
}
