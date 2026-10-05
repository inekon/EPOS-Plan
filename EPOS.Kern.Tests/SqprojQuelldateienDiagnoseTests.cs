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
                    _aus.WriteLine("  IFC " + Path.GetFileName(ifc) + ": abgeglichen " + stand.Abgeglichen + " (Kennung " + stand.Abgleich?.UeberKennung +
                                   ", Name " + stand.Abgleich?.UeberName + "), ohne Treffer " + stand.NichtAbgeglichen + ", IFC ohne Gegenstück " + stand.IfcOhneGegenstueck +
                                   (stand.Abgelehnt ? ", " + stand.Ablehnung : ""));
                    if (stand.Abgelehnt) continue;
                    Zonenplan plan = Zonenplan.Vorschlag(ablauf.Abbild, 0);
                    SqprojZonenergebnis e = ablauf.ProjektdateiUebernehmen(plan);
                    foreach (SqprojPlanzone z in e.Zonen)
                        _aus.WriteLine("  Zone " + z.Zone.Name + " [" + (plan.Zone(z.Schluessel).Nutzung ?? "—") + "] Räume " + plan.RaeumeVon(z.Schluessel).Count +
                                       ", " + string.Join(", ", z.Konditionierung.Groessen.Select(g => Konditionierungsgroessen.Kennwort(g.Groesse) + "=" + g.Herkunft)));
                    _aus.WriteLine("  leer " + e.LeereZonen.Count + ", nicht zugeordnet " + plan.NichtZugeordnet.Count);
                }
                finally
                {
                    File.Delete(kopie);
                }
            }
        }
    }
}
