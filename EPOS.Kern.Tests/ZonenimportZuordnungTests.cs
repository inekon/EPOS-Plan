using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Importproben mit Zuordnung statt fester Tabellen</b> (Konzept Nutzungsprofile Kapitel 8, Stufe NP2): jede
    /// IFC- und gbXML-Probe unter <c>Referenzlaeufe/Importproben</c>, die die Zonenregel Z6 bildet, belegt ihre Zonen über
    /// die ausgelieferte Zuordnung vor — für die alten Paare genau wie die feste Tabelle (über das EPOS-Muster gleicher
    /// Nutzung), für Sport, Gastronomie, Lager, Verkehr und Technik jetzt mit dem gleichnamigen Muster statt „keine“ (Q42);
    /// Sanitär und Sonstige bleiben ohne. Der Zonenplan aus dem Vorschlag trägt dieselben Profile.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZonenimportZuordnungTests : IDisposable
    {
        private static readonly string[] NEUE_MUSTER = { "Sport", "Gastronomie", "Lager", "Verkehr", "Technik" };

        private readonly TestDatenbank _db = new TestDatenbank();

        public void Dispose() => _db.Dispose();

        [Fact]
        public void Alle_Z6_Proben_belegen_ueber_die_Zuordnung_vor()
        {
            if (!_db.Vorhanden) return;
            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Lesen();
            Assert.True(v.MitKatalog);
            int zonen = 0, neu = 0;
            var gesehen = new List<string>();
            foreach (string pfad in Directory.GetFiles(IfcProbenTests.Ordner())
                         .Where(p => p.EndsWith(".ifc", StringComparison.OrdinalIgnoreCase) || p.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                         .OrderBy(p => p, StringComparer.Ordinal))
            {
                var a = new GebaeudeImportAblauf();
                using (FileStream s = File.OpenRead(pfad))
                    a.Lesen(s, pfad, GebaeudeImportProfil.FuerDatei(Path.GetFileName(pfad)));
                if (a.Abbild == null) continue;
                for (int g = 0; g < a.Abbild.Gebaeude.Count; g++)
                {
                    GebaeudeZonierung z6 = GebaeudeZonierung.Bilden(a.Abbild, g, IfcImportProfil.ZONENREGEL_Z6);
                    if (z6.Abgelehnt || z6.Regel != IfcImportProfil.ZONENREGEL_Z6) continue;
                    Zonenplan plan = Zonenplan.Vorschlag(a.Abbild, g, IfcImportProfil.ZONENREGEL_Z6, vorbelegung: v);
                    foreach (Importzone iz in z6.Zonen)
                    {
                        Planprofil ist = Zonenplan.NutzungAus(IfcImportProfil.ZONENREGEL_Z6, iz, v);
                        Planprofil vorgabe = Zonenplan.NutzungAus(IfcImportProfil.ZONENREGEL_Z6, iz, Raumnutzungsvorbelegung.Vorgabe);
                        string klasse = iz.Raeume.Count == 0 ? null : iz.Raeume.GroupBy(GebaeudeZonierung.Nutzungsklasse)
                            .Select(gr => (K: gr.Key, A: gr.Sum(r => r.FlaecheM2 > 0.0 ? r.FlaecheM2.Value : 0.0), N: gr.Count()))
                            .OrderByDescending(k => k.A).ThenByDescending(k => k.N).ThenBy(k => k.K, StringComparer.Ordinal).First().K;
                        if (!iz.IstBeheizt) Assert.Null(ist);
                        else if (vorgabe != null) Assert.Equal(v.AusKennung(vorgabe.Name), ist);          // alte Paare unverändert
                        else if (NEUE_MUSTER.Contains(klasse))
                        {
                            Assert.Equal(klasse, ist?.Name);                                               // Q42: Muster statt „keine“
                            Assert.True(ist.Id.HasValue);
                            neu++;
                        }
                        else Assert.Null(ist);                                                              // Sanitär, Sonstige
                        Assert.Contains(plan.Zonen, pz => pz.Name == iz.Name && Equals(pz.Profil, ist));
                        zonen++;
                    }
                    gesehen.Add(Path.GetFileName(pfad));
                }
            }
            Assert.NotEmpty(gesehen);
            Assert.True(zonen > 0);
            Assert.True(neu > 0, "Keine Probe belegt ein neues Muster vor: " + string.Join(", ", gesehen));
        }
    }
}
