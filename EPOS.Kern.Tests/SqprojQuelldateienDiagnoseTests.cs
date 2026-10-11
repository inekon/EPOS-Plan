using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Diagnose des Projektdatei-Lesers an Anwenderdateien unter <c>Quellen/*.sqproj</c></b>: schreibt je Datei Fassung,
    /// Räume, Zonen, Zeitprofile, Abschnitte, Übersprungenes und — liegt eine IFC-Datei gleichen Namens daneben — den
    /// Raumabgleich und die Zonenübernahme ins Testprotokoll. Die Dateien sind Anwenderdaten, liegen nie im Repositorium
    /// (<c>.gitignore</c>) und können fehlen — dann endet der Test ohne Prüfung. Die Regeln halten die synthetischen Proben.
    /// </summary>
    public sealed class SqprojQuelldateienDiagnoseTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();
        private readonly ITestOutputHelper _aus;

        public SqprojQuelldateienDiagnoseTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        private static string Quellen([CallerFilePath] string eigeneDatei = null)
        {
            string o = Path.GetDirectoryName(eigeneDatei);
            while (o != null && !File.Exists(Path.Combine(o, "WP-Plan.Kern.slnf")))
                o = Path.GetDirectoryName(o);
            return o == null ? null : Path.Combine(o, "Quellen");
        }

        [Fact]
        public void Projektdateien_unter_Quellen()
        {
            string ordner = Quellen();
            if (ordner == null || !Directory.Exists(ordner)) return;
            foreach (string pfad in Directory.GetFiles(ordner, "*.sqproj").OrderBy(p => p, StringComparer.Ordinal))
            {
                if (!SqprojLeser.IstSqlite(pfad)) continue;   // LFS-Zeiger oder fremde Datei
                string kopie = Path.Combine(Path.GetTempPath(), "epos-sqproj-test", Guid.NewGuid().ToString("N") + ".sqproj");
                Directory.CreateDirectory(Path.GetDirectoryName(kopie));
                File.Copy(pfad, kopie);
                try
                {
                    SqprojAbbild a = SqprojLeser.Lesen(kopie);
                    _aus.WriteLine(Path.GetFileName(pfad) + ": " + (a.Abgelehnt ? "ABGELEHNT " + a.Ablehnung : "Fassung " + a.Fassung));
                    if (a.Abgelehnt) continue;
                    _aus.WriteLine("  Räume " + a.Raeume.Count + ", Zonen 5/6 " + a.Zonen.Count + " (mit Raum " + a.Zonen.Count(z => z.Raeume.Count > 0) +
                                   "), Zeitprofile " + a.Zeitprofile + ", Abschnitte " + a.Abschnitte + ", Gebäudegruppe " + (a.Gebaeudegruppe?.Name ?? "—"));
                    foreach (var m in a.Meldungen) _aus.WriteLine("  " + m.Stufe + " " + m);
                    string ifc = Path.ChangeExtension(pfad, ".ifc");
                    if (!File.Exists(ifc)) continue;
                    var ablauf = new GebaeudeImportAblauf();
                    using (FileStream s = File.OpenRead(ifc))
                        ablauf.Lesen(s, ifc, GebaeudeImportProfil.FuerDatei(ifc));
                    if (ablauf.Abbild == null) continue;
                    SqprojStand stand = ablauf.ProjektdateiLesen(pfad, 0);
                    _aus.WriteLine("  IFC " + Path.GetFileName(ifc) + ": abgeglichen " + stand.Abgeglichen + " (GUID " + stand.Abgleich?.UeberGuid + ", Kennung " + stand.Abgleich?.UeberKennung +
                                   ", Name " + stand.Abgleich?.UeberName + "), ohne Treffer " + stand.NichtAbgeglichen + ", IFC ohne Gegenstück " + stand.IfcOhneGegenstueck +
                                   (stand.Abgelehnt ? ", " + stand.Ablehnung : ""));
                    if (stand.Abgelehnt) continue;
                    _aus.WriteLine("  IFC-Räume mit HottCAD-GUID " + ablauf.Abbild.Gebaeude[0].Raeume.Count(r => r.HottcadGuid != null) + " von " +
                                   ablauf.Abbild.Gebaeude[0].Raeume.Count + ", Bauteile mit GUID " + ablauf.Abbild.Gebaeude[0].Bauteile.Count(x => x.HottcadGuid != null) +
                                   " von " + ablauf.Abbild.Gebaeude[0].Bauteile.Count + ", GUID mehrdeutig " + stand.Abgleich.GuidMehrdeutig.Count);
                    foreach (var m in stand.Abgleich.Meldungen) _aus.WriteLine("  Abgleich " + m.Stufe + " " + m);
                    Zonenplan plan = Zonenplan.Vorschlag(ablauf.Abbild, 0);
                    SqprojZonenergebnis e = ablauf.ProjektdateiUebernehmen(plan);
                    foreach (SqprojPlanzone z in e.Zonen)
                        _aus.WriteLine("  Zone " + z.Zone.Name + " [" + (plan.Zone(z.Schluessel).Nutzung ?? "—") + "] Räume " + plan.RaeumeVon(z.Schluessel).Count +
                                       ", " + string.Join(", ", z.Konditionierung.Groessen.Select(g => Konditionierungsgroessen.Kennwort(g.Groesse) + "=" + g.Herkunft)));
                    _aus.WriteLine("  leer " + e.LeereZonen.Count + ", nicht zugeordnet " + plan.NichtZugeordnet.Count);
                    // Gegenprobe ohne HottCAD-GUID: der Abgleich nur über GlobalId und Name je Geschoss.
                    foreach (AbbildRaum r in ablauf.Abbild.Gebaeude[0].Raeume) r.HottcadGuid = null;
                    SqprojRaumabgleich ohne = SqprojRaumabgleich.Bilden(stand.Abbild, ablauf.Abbild.Gebaeude[0]);
                    _aus.WriteLine("  ohne GUID: abgeglichen " + ohne.Abgeglichen + " (Kennung " + ohne.UeberKennung + ", Name " + ohne.UeberName + ")");
                }
                finally
                {
                    File.Delete(kopie);
                }
            }
        }

        /// <summary>
        /// <b>Raumtabelle samt Herleitung</b> je Datei unter <c>Quellen/</c> (<c>*.sqproj</c> und <c>*.ifc</c>): je Raum Geschoss,
        /// Name, die Fläche des Abbilds (so zeigt sie der Gebäudeimport), ihre Herkunft, die Fläche des Grundrisspolygons
        /// (Projektdatei: Raum-XML abzüglich Löcher; IFC: Profilring der Körperdarstellung), Volumen, Höhe, Beheizung mit
        /// Zustandsangabe und Zone der Datei; danach die Summen wie im Kopf des Dialogs. Stehen Projektdatei und IFC gleichen
        /// Namens nebeneinander, kommt je Raum der Gegenwert der IFC über die HottCAD-GUID dazu.
        /// </summary>
        [Fact]
        public void Raumtabelle_mit_Herleitung()
        {
            string ordner = Quellen();
            if (ordner == null || !Directory.Exists(ordner)) return;
            var dateien = Directory.GetFiles(ordner, "*.sqproj").Concat(Directory.GetFiles(ordner, "*.ifc"))
                                   .OrderBy(p => p, StringComparer.Ordinal).ToList();
            foreach (string pfad in dateien)
            {
                bool projektdatei = pfad.EndsWith(".sqproj", StringComparison.OrdinalIgnoreCase);
                if (projektdatei ? !SqprojLeser.IstSqlite(pfad) : new FileInfo(pfad).Length < 1024) continue;   // LFS-Zeiger
                AbbildGebaeude g = Lesen(pfad, out GebaeudeAbbild abbild);
                if (g == null) { _aus.WriteLine(Path.GetFileName(pfad) + ": kein Gebäude"); continue; }
                AbbildGebaeude gegen = null;
                string ifc = Path.ChangeExtension(pfad, ".ifc");
                if (projektdatei && File.Exists(ifc)) gegen = Lesen(ifc, out _);
                var ifcJeGuid = gegen?.Raeume.Where(r => r.HottcadGuid != null).GroupBy(r => r.HottcadGuid, StringComparer.OrdinalIgnoreCase)
                                     .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);
                _aus.WriteLine("=== " + Path.GetFileName(pfad) + " — Räume " + g.Raeume.Count);
                _aus.WriteLine("Geschoss | Raum | Fläche Abbild | Herkunft | Fläche Polygon | Boden Körper | Volumen | Volumen Körper | Höhe | Beheizt (Zustand) | Zone"
                               + (gegen != null ? " | IFC Raum | IFC Fläche | Δ" : ""));
                var geschoss = g.Geschosse.Where(x => x.Kennung != null).GroupBy(x => x.Kennung, StringComparer.OrdinalIgnoreCase)
                                  .ToDictionary(x => x.Key, x => x.First().Anzeigename, StringComparer.OrdinalIgnoreCase);
                string Geschoss(AbbildRaum r) => r.GeschossName ?? (r.GeschossKennung != null && geschoss.TryGetValue(r.GeschossKennung, out string n) ? n : "—");
                foreach (AbbildRaum r in g.Raeume.OrderBy(r => Geschoss(r), StringComparer.Ordinal).ThenBy(r => r.Name, StringComparer.Ordinal))
                {
                    double? polygon = projektdatei ? PolygonflaecheProjektdatei(abbild, r.Kennung) : Polygonflaeche(r.GrundrissM);
                    string herkunft = projektdatei ? "BmRoom.Area" : r.FlaecheAusGrundriss ? "Grundriss" : "Menge";
                    (double? boden, double? raum) = Koerpermasse(r.Koerper);
                    string zeile = Geschoss(r) + " | " + r.Name + " | " + Z(r.FlaecheM2) + " | " + herkunft + " | " + Z(polygon)
                                   + " | " + Z(boden) + " | " + Z(r.VolumenM3) + " | " + Z(raum) + " | " + Z(r.HoeheM) + " | " + (r.Beheizt ? "ja" : "nein") + " (" + (r.Zustandsangabe ?? "—")
                                   + ", " + r.BeheiztQuelle + ") | " + (r.ZonenName ?? "—");
                    if (ifcJeGuid != null)
                    {
                        AbbildRaum i = r.HottcadGuid != null && ifcJeGuid.TryGetValue(r.HottcadGuid, out AbbildRaum t) ? t : null;
                        zeile += " | " + (i?.Name ?? "—") + " | " + Z(i?.FlaecheM2) + " | " + Z(i?.FlaecheM2 - r.FlaecheM2);
                    }
                    _aus.WriteLine(zeile);
                }
                var warm = g.Raeume.Where(r => r.Beheizt).ToList();
                _aus.WriteLine("Summe: Fläche alle " + Z(g.Raeume.Sum(r => r.FlaecheM2 ?? 0)) + " m², beheizt " + Z(warm.Sum(r => r.FlaecheM2 ?? 0))
                               + " m² (" + warm.Count + " Räume), Volumen beheizt " + Z(warm.Sum(r => r.VolumenM3 ?? 0)) + " m³, ohne Zone "
                               + string.Join(", ", g.Raeume.Where(r => r.ZonenName == null).Select(r => r.Name)));
            }
        }

        private static AbbildGebaeude Lesen(string pfad, out GebaeudeAbbild abbild)
        {
            var ablauf = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                ablauf.Lesen(s, pfad, GebaeudeImportProfil.FuerDatei(pfad));
            abbild = ablauf.Abbild;
            return abbild == null || abbild.Gebaeude.Count == 0 ? null : abbild.Gebaeude[0];
        }

        /// <summary>Fläche der Raumpolygone der Projektdatei: Außenringe abzüglich Löcher, waagerecht projiziert [m²].</summary>
        private static double? PolygonflaecheProjektdatei(GebaeudeAbbild abbild, string kennung)
        {
            if (abbild is not SqprojGebaeudeAbbild s || !s.Raumpolygone.TryGetValue(kennung, out var polygone)) return null;
            return polygone.Sum(p => (Polygonflaeche(p.Aussen?.Punkte) ?? 0) - p.Loecher.Sum(l => Polygonflaeche(l.Punkte) ?? 0));
        }

        /// <summary>Gaußsche Trapezformel in der Waagerechten (x, y) [m²]; <c>null</c> = kein Ring.</summary>
        private static double? Polygonflaeche(System.Collections.Generic.IReadOnlyList<double[]> p)
        {
            if (p == null || p.Count < 3) return null;
            double a = 0;
            for (int i = 0; i < p.Count; i++)
            {
                double[] u = p[i], v = p[(i + 1) % p.Count];
                a += u[0] * v[1] - v[0] * u[1];
            }
            return Math.Abs(a) / 2.0;
        }

        /// <summary>
        /// Aus dem Körper des Raums: die Bodenfläche (Dreiecke mit Normale nach unten, waagerecht projiziert) und das
        /// Volumen (Divergenzsatz) [m², m³]; <c>null</c> = kein Körper.
        /// </summary>
        private static (double?, double?) Koerpermasse(Dateikoerper k)
        {
            if (k == null || k.Dreiecke.Count == 0) return (null, null);
            double boden = 0, volumen = 0;
            foreach (int[] d in k.Dreiecke)
            {
                double[] a = k.PunkteM[d[0]], b = k.PunkteM[d[1]], c = k.PunkteM[d[2]];
                double nx = (b[1] - a[1]) * (c[2] - a[2]) - (b[2] - a[2]) * (c[1] - a[1]);
                double ny = (b[2] - a[2]) * (c[0] - a[0]) - (b[0] - a[0]) * (c[2] - a[2]);
                double nz = (b[0] - a[0]) * (c[1] - a[1]) - (b[1] - a[1]) * (c[0] - a[0]);
                double betrag = Math.Sqrt(nx * nx + ny * ny + nz * nz);
                if (betrag > 0 && nz / betrag < -0.99) boden += betrag / 2.0;
                volumen += (a[0] * (b[1] * c[2] - b[2] * c[1]) - a[1] * (b[0] * c[2] - b[2] * c[0]) + a[2] * (b[0] * c[1] - b[1] * c[0])) / 6.0;
            }
            return (boden, Math.Abs(volumen));
        }

        private static string Z(double? x) => x.HasValue ? x.Value.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "—";
    }
}
