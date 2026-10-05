using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Importproben 13 bis 18 an öffentlichen Beispieldateien</b> (Mehrzonenkonzept Kapitel 8): liest jede
    /// <c>*.ifc</c> des Ordners aus der Umgebungsvariablen <c>EPOS_IFC_BEISPIELE</c> und schreibt die Kennzahlen —
    /// Zonen, Grenzen, Paare, U-Werte, Schichten (13), Polygonflächen (14), beheizte Fläche je Fassung (15), die
    /// Einzonenhülle unter Z5 (16), Raumseitenmaß gegen Bruttomaß der Außenbauteile (17) und die Flächen ohne
    /// Gegenstück (18). Die Dateien liegen nie im Repositorium; ohne Variable ist die Probe übersprungen. Diagnose,
    /// keine Messlatte: Sie prüft nur, dass jede Datei ohne Fehler bis zum Bauteilvorschlag kommt.
    /// </summary>
    public sealed class IfcBeispieldateienProbenTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();
        private readonly ITestOutputHelper _aus;

        public IfcBeispieldateienProbenTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        private static string Z(double? w) => w.HasValue ? w.Value.ToString("0.0", CultureInfo.InvariantCulture) : "—";

        [Fact]
        public void Beispieldateien_werden_gelesen_und_gezaehlt()
        {
            string ordner = Environment.GetEnvironmentVariable("EPOS_IFC_BEISPIELE");
            if (string.IsNullOrEmpty(ordner) || !Directory.Exists(ordner)) { _aus.WriteLine("EPOS_IFC_BEISPIELE fehlt — übersprungen."); return; }
            foreach (string pfad in Directory.GetFiles(ordner, "*.ifc").OrderBy(d => d, StringComparer.Ordinal))
            {
                if (new FileInfo(pfad).Length < 1024) continue;
                var uhr = Stopwatch.StartNew();
                var a = new GebaeudeImportAblauf();
                using (FileStream s = File.OpenRead(pfad))
                    a.Lesen(s, pfad, new IfcImportProfil());
                long lesen = uhr.ElapsedMilliseconds;
                _aus.WriteLine("=== " + Path.GetFileName(pfad) + " | Schema " + a.Quelle?.Schemastand + " | " + lesen + " ms | Leserfehler "
                               + a.Meldungen.Count(m => m.Stufe == PruefStufe.Fehler) + " | Warnungen " + string.Join(",", a.Meldungen
                                   .Where(m => m.Stufe == PruefStufe.Warnung).Select(m => m.Schluessel.Replace("IMP_IFC_PROT_", "")).Distinct()));
                Assert.NotNull(a.Abbild);
                for (int gi = 0; gi < a.Abbild.Gebaeude.Count; gi++)
                {
                    AbbildGebaeude g = a.Abbild.Gebaeude[gi];
                    List<AbbildBauteil> alle = g.Bauteile.Concat(g.Bauteile.SelectMany(b => b.Oeffnungen)).ToList();
                    List<AbbildGrenze> grenzen = g.Bauteile.SelectMany(b => b.Grenzen).Concat(g.Bauteile.SelectMany(b => b.Oeffnungen).SelectMany(o => o.Grenzen)).ToList();
                    _aus.WriteLine("P13 Gebäude " + g.Anzeigename + ": Geschosse " + g.Geschosse.Count + ", Räume " + g.Raeume.Count + " (beheizt "
                                   + g.Raeume.Count(r => r.Beheizt) + ", Grundriss " + g.Raeume.Count(r => r.GrundrissM != null) + ", Fläche aus Grundriss "
                                   + g.Raeume.Count(r => r.FlaecheAusGrundriss) + "), Raumfläche "
                                   + Z(g.Raeume.Sum(r => r.FlaecheM2 ?? 0)) + " m² (beheizt " + Z(g.Raeume.Where(r => r.Beheizt).Sum(r => r.FlaecheM2 ?? 0))
                                   + "), Grenzen " + g.ZahlGrenzen + " (2. Ebene " + g.ZahlGrenzenZweiteEbene + ", mit Polygon "
                                   + grenzen.Count(x => x.FlaecheM2.HasValue) + ", mit Gegenstück " + grenzen.Count(x => x.GegenstueckKennung != null)
                                   + "), Bauteile " + g.Bauteile.Count + " + Öffnungen " + g.Bauteile.Sum(b => b.Oeffnungen.Count) + ", mit U "
                                   + alle.Count(b => b.UWertWm2K.HasValue) + ", mit Aufbau " + alle.Count(b => b.Aufbau != null) + ", Schichten "
                                   + alle.Where(b => b.Aufbau != null).Sum(b => b.Aufbau.Schichten.Count) + ", Trenndecken Bezug/Geschoss/Grundriss "
                                   + g.Bauteile.Count(b => b.Trenndeckenherkunft == AbbildBauteil.TRENNDECKE_BEZUG) + "/"
                                   + g.Bauteile.Count(b => b.Trenndeckenherkunft == AbbildBauteil.TRENNDECKE_GESCHOSS) + "/"
                                   + g.Bauteile.Count(b => b.Trenndeckenherkunft == AbbildBauteil.TRENNDECKE_GRUNDRISS));
                    _aus.WriteLine("P15 Beheizungsregeln: " + string.Join(", ", g.Raeume.GroupBy(r => (r.Beheizungsregel ?? "—") + (r.Beheizt ? "+" : "-"))
                                   .OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => x.Key + " " + x.Count())));
                    _aus.WriteLine("P14 Grenzen ohne Polygon: " + grenzen.Count(x => !x.FlaecheM2.HasValue) + "; ohne Bauteil: " + string.Join("; ", a.Meldungen
                                   .Where(m => m.Schluessel == "IMP_IFC_PROT_GRENZEN_OHNE_BAUTEIL" || m.Schluessel == "IMP_IFC_PROT_GRENZEN_DACHPLATTE")
                                   .Select(m => m.Schluessel.Replace("IMP_IFC_PROT_", "") + " " + string.Join("/", m.Werte))));
                    _aus.WriteLine("P14 Polygonflächen Σ " + Z(grenzen.Where(x => !x.Virtuell).Sum(x => x.FlaecheM2 ?? 0)) + " m² (Bauteile "
                                   + Z(g.Bauteile.SelectMany(b => b.Grenzen).Where(x => !x.Virtuell).Sum(x => x.FlaecheM2 ?? 0)) + ", Öffnungen "
                                   + Z(g.Bauteile.SelectMany(b => b.Oeffnungen).SelectMany(o => o.Grenzen).Sum(x => x.FlaecheM2 ?? 0))
                                   + "), Bruttomengen Σ " + Z(g.Bauteile.Sum(b => b.BruttoflaecheM2 ?? 0)) + " m²");

                    GebaeudeZonierung v = GebaeudeZonierung.Bilden(a.Abbild, gi);
                    _aus.WriteLine("P13 Zonierung: Regeln " + string.Join(",", v.Regeln) + ", Vorgabe " + v.Vorgabe + ", Zonen " + v.Zonen.Count
                                   + " (beheizt " + v.Zonen.Count(z => z.IstBeheizt) + ", Σ " + Z(v.Zonen.Sum(z => z.FlaecheM2 ?? 0)) + " m²), Seiten "
                                   + v.Flaechen.Count + ", Paare " + v.Trennungen.Count + " (" + Z(v.Trennungen.Sum(t => Math.Max(t.FlaecheA, t.FlaecheB)))
                                   + " m²), ohne Gegenstück " + v.Flaechen.Count(f => f.OhneGegenstueck) + ", Meldungen "
                                   + string.Join(",", v.Meldungen.Where(m => m.Stufe != PruefStufe.Info).Select(m => m.Schluessel.Replace("IMP_IFC_PROT_", "")).Distinct()));
                    List<string> ohne = v.Flaechen.Where(f => f.OhneGegenstueck).Select(f => (f.Bauteil.Quelltyp ?? "") + " " + (f.Bauteil.Name ?? f.Bauteil.Kennung))
                                                  .Distinct().Take(8).ToList();
                    if (ohne.Count > 0) _aus.WriteLine("P18 ohne Gegenstück (bis 8): " + string.Join("; ", ohne));

                    foreach (string regel in new[] { v.Vorgabe, IfcImportProfil.ZONENREGEL_Z5 }.Distinct())
                    {
                        GebaeudeBauteilvorschlag b = GebaeudeBauteilvorschlag.BildenMitZonen(a, gi, null, regel);
                        string Gruppe(string feld)
                        {
                            List<GebaeudeBauteilzeile> z = b.Zeilen.Where(x => x.Summenfeld == feld).ToList();
                            double f = z.Sum(x => x.Bauteil.Flaeche);
                            double mitU = z.Where(x => x.Bauteil.U_Wert.HasValue).Sum(x => x.Bauteil.Flaeche);
                            double ua = z.Where(x => x.Bauteil.U_Wert.HasValue).Sum(x => x.Bauteil.Flaeche * x.Bauteil.U_Wert.Value);
                            return Z(f) + " m² U " + (mitU > 0 ? (ua / mitU).ToString("0.00", CultureInfo.InvariantCulture) : "—");
                        }
                        _aus.WriteLine((regel == IfcImportProfil.ZONENREGEL_Z5 ? "P16 " : "P13 ") + "Vorschlag " + regel + ": " + b.Zeilen.Count + " Zeilen, Fehler "
                                       + b.Meldungen.Count(m => m.Stufe == PruefStufe.Fehler) + ", Zone " + Z(b.Zeilen.Where(x => x.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE)
                                           .Sum(x => x.Bauteil.Flaeche)) + " m², Außenwand " + Gruppe(GebaeudeZielfelder.FLAECHE_AUSSENWAND) + ", Dach "
                                       + Gruppe(GebaeudeZielfelder.FLAECHE_DACH) + ", Grund " + Gruppe(GebaeudeZielfelder.FLAECHE_GRUND) + ", Fenster "
                                       + Gruppe(GebaeudeZielfelder.FENSTER_GESAMT));
                        foreach (PruefMeldung m in b.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler).Take(3)) _aus.WriteLine("    Fehler " + m);
                    }

                    // P17: Raumseitenmaß (Polygone der äußeren Grenzen) gegen Bruttomaß (Menge) derselben Außenbauteile.
                    List<AbbildBauteil> aussen = g.Bauteile.Where(b => (b.Randbedingung == Randbedingung.Aussenluft || b.Randbedingung == Randbedingung.Erdreich)
                                                                       && b.BruttoflaecheM2.HasValue && b.Grenzen.Any(x => x.FlaecheM2.HasValue)).ToList();
                    double raumseite = aussen.Sum(b => b.Grenzen.Where(x => x.FlaecheM2.HasValue && !x.Virtuell).Sum(x => x.FlaecheM2.Value));
                    double brutto = aussen.Sum(b => b.BruttoflaecheM2.Value);
                    _aus.WriteLine("P17 Außenbauteile mit Polygon " + aussen.Count + ": Raumseite " + Z(raumseite) + " m², Brutto " + Z(brutto) + " m², Abstand "
                                   + (brutto > 0 ? ((raumseite - brutto) / brutto * 100).ToString("0.0", CultureInfo.InvariantCulture) + " %" : "—"));
                }
            }
        }
    }
}
