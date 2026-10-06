using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EPOS.UI.Dialoge.Bedarf;
using EPOS.UI.Dialoge.Import;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Herleitungszeile des Zonenbaums</b> (Stufe NP2b; Konzept Nutzungsprofile 6.2, NP-F16) an der Hülle mit der
    /// Testdatenbank: je Zone Profil und Kategorie, die Quelle der Vorbelegung (IFC-Klasse bzw. Raumtyp unter Z6, DIN-Nummer der
    /// Projektdatei, von Hand), die Größen, die die Datei liefert, und die Kennwerte des Profils in Kurzform.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class ZonenbaumHerleitungTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly List<string> _dateien = new List<string>();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { }
        }

        /// <summary>Die Z6-Pläne aller Importproben mit Vorbelegung aus der Testdatenbank.</summary>
        private static IEnumerable<Zonenplan> Z6Plaene(Raumnutzungsvorbelegung v)
        {
            foreach (string pfad in Directory.GetFiles(IfcProbenTests.Ordner())
                         .Where(p => p.EndsWith(".ifc", StringComparison.OrdinalIgnoreCase))
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
                    yield return Zonenplan.Vorschlag(a.Abbild, g, IfcImportProfil.ZONENREGEL_Z6, vorbelegung: v);
                }
            }
        }

        private static GebaeudeZonenplanDaten Daten(Zonenplan plan)
            => GebaeudeImportZonen.PlanDaten(plan, null, Array.Empty<GebaeudeZonenzeileDaten>(), (null, false, 0));

        /// <summary>
        /// Unter Z6 nennt jede vorbelegte Zone Profil, Kategorie, ihre Quelle („aus IFC-Klasse …“ bzw. „aus Raumtyp …“) und die
        /// Kennwerte des Profils in Kurzform; eine Wahl von Hand heißt danach „von Hand“, „keine“ von Hand trägt keine Zeile.
        /// </summary>
        [Fact]
        public void Unter_Z6_nennt_die_Zeile_Profil_Kategorie_Quelle_und_Kennwerte()
        {
            if (!_db.Vorhanden) return;
            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Lesen();
            Assert.True(v.MitKatalog);
            RaumnutzungTexte t = RaumnutzungHuelle.Texte();
            int geprueft = 0;
            Zonenplan erster = null;
            foreach (Zonenplan plan in Z6Plaene(v))
            {
                GebaeudeZonenplanDaten daten = Daten(plan);
                for (int i = 0; i < plan.Zonen.Count; i++)
                {
                    Planzone pz = plan.Zonen[i];
                    GebaeudeProfilherleitung h = daten.Zonen[i].Herleitung;
                    if (pz.Quelle == null)
                    {
                        Assert.Null(pz.Profil);
                        Assert.Null(h);
                        continue;
                    }
                    Assert.NotNull(h);
                    Assert.Contains(pz.Quelle.Art, new[] { RaumnutzungSchema.ZUORDNUNG_IFC, RaumnutzungSchema.ZUORDNUNG_HOTTCAD });
                    Assert.Equal(pz.Quelle.Art, h.Quellart);
                    Assert.Equal((pz.Quelle.Art == RaumnutzungSchema.ZUORDNUNG_IFC ? "aus IFC-Klasse " : "aus Raumtyp ") + pz.Quelle.Schluessel, h.Quelle);
                    if (pz.Profil?.Id is not long id)
                    {
                        Assert.Equal("keine", h.Profil);    // die Zuordnung führt auf „keine“ (etwa Sanitär)
                        continue;
                    }
                    Raumnutzungsprofil p = v.Profil(id);
                    Assert.Equal(pz.Profil.Name, h.Profil);
                    Assert.Equal(v.Kategorie(id), h.Kategorie);
                    Assert.Equal(p.IstLeer ? "" : RaumnutzungHuelle.Kurzform(p, t), h.Kennwerte);
                    Assert.Equal("", h.Dateigroessen);
                    Assert.StartsWith(h.Profil + " · " + h.Kategorie + " — " + h.Quelle, h.Text);
                    erster ??= plan;
                    geprueft++;
                }
            }
            Assert.True(geprueft > 0, "Keine Z6-Probe belegt eine Zone mit einem Profil des Katalogs vor.");

            // Von Hand: dieselbe Zone heißt danach „von Hand“; „keine“ von Hand trägt keine Zeile.
            Planzone zone = erster.Zonen.First(z => z.Profil?.Id != null);
            long lager = v.Auswahl().Where(a => a.Name == RaumnutzungSaat.LAGER).Select(a => long.Parse(a.Schluessel.Substring(1))).First();
            Assert.True(erster.NutzungSetzen(zone.Schluessel, "#" + lager).Ok);
            GebaeudeProfilherleitung hand = Daten(erster).Zonen.Single(z => z.Schluessel == zone.Schluessel).Herleitung;
            Assert.Equal(Profilquelle.HAND, hand.Quellart);
            Assert.Equal(RaumnutzungSaat.LAGER + " · " + v.Kategorie(lager) + " — von Hand", hand.Text.Split(';')[0]);
            Assert.True(erster.NutzungSetzen(zone.Schluessel, null).Ok);
            Assert.Null(Daten(erster).Zonen.Single(z => z.Schluessel == zone.Schluessel).Herleitung);
        }

        /// <summary>
        /// Die Zonen der Projektdatei nennen die DIN-Nummer, aus der ihr Profil folgt, und die Größen, die die Datei liefert
        /// („… aus der Datei“, Datei vor Profil).
        /// </summary>
        [Fact]
        public async Task Die_Zonen_der_Projektdatei_nennen_DIN_Nummer_und_Groessen_aus_der_Datei()
        {
            if (!_db.Vorhanden) return;
            var h = new GebaeudeImportHuelle();
            IReadOnlyDictionary<string, object> gaben = h.Gaben();
            string ifc = Merken(SqprojProbenErzeuger.HottcadZonenhaus(Wurzel()));
            var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
            Assert.True((await lesen(ifc, null, CancellationToken.None)).Gelesen);
            var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"];
            IReadOnlyDictionary<string, bool> haken = zuordnen(new GebaeudeZuordnungsanfrage(0, null, new Dictionary<string, bool>(), null, null, "Z4", null, false, null, null))
                .Raeume.ToDictionary(r => r.Kennung, _ => true);
            string sq = Merken(SqprojProbenErzeuger.Zonenhaus().Schreiben(SqprojProbenErzeuger.TempPfad("herleitung")));
            var dazu = (Func<string, int, CancellationToken, Task<GebaeudeProjektdateiDaten>>)gaben["ProjektdateiLesen"];
            Assert.True((await dazu(sq, 0, CancellationToken.None)).Gelesen);

            var schritte = new[] { new GebaeudePlanschritt(GebaeudePlanschrittArt.PROJEKTDATEI) };
            GebaeudeZonenplanDaten plan = zuordnen(new GebaeudeZuordnungsanfrage(0, null, haken, null, null, "Z4", null, false, schritte, haken))
                .Zonierung!.Plan!;
            List<GebaeudePlanzoneDaten> mitProfil = plan.Zonen.Where(z => z.Herleitung != null).ToList();
            Assert.NotEmpty(mitProfil);
            foreach (GebaeudePlanzoneDaten z in mitProfil)
            {
                Planzone pz = h.Plan.Zone(z.Schluessel);
                Assert.Equal(RaumnutzungSchema.ZUORDNUNG_DIN, z.Herleitung.Quellart);
                Assert.Equal("aus DIN-Nr. " + pz.Quelle.Schluessel + " der Projektdatei", z.Herleitung.Quelle);
                Assert.Equal(pz.Projektdatei.Profilnummer?.ToString(System.Globalization.CultureInfo.InvariantCulture), pz.Quelle.Schluessel);
                Assert.EndsWith(" aus der Datei", z.Herleitung.Dateigroessen);
                Assert.Contains(Konditionierungsarbeit.Groessenname(Konditionierungsgroesse.Heizsoll), z.Herleitung.Dateigroessen);
                Assert.Contains("; " + z.Herleitung.Dateigroessen, z.Herleitung.Text);
            }
        }

        /// <summary>
        /// <b>Neu lesen nach dem Blatt „Nutzungsprofile…“</b> (NP2b-3): Ändert das Blatt die Zuordnung, liest die nächste Anfrage
        /// derselben Schritte Katalog und Zuordnung neu — die vorbelegte Zone folgt der geänderten Zuordnung, die Zone mit Wahl
        /// von Hand behält sie.
        /// </summary>
        [Fact]
        public async Task Nach_dem_Blatt_folgt_die_vorbelegte_Zone_der_Zuordnung_die_Handwahl_bleibt()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new RaumnutzungCtrl();
            Raumnutzungsvorbelegung v = Raumnutzungsvorbelegung.Lesen();
            long Id(string name) => long.Parse(v.Auswahl().First(a => a.Name == name).Schluessel.Substring(1), System.Globalization.CultureInfo.InvariantCulture);

            foreach (string pfad in Directory.GetFiles(IfcProbenTests.Ordner(), "*.ifc").OrderBy(p => p, StringComparer.Ordinal))
            {
                var h = new GebaeudeImportHuelle();
                IReadOnlyDictionary<string, object> gaben = h.Gaben();
                var lesen = (Func<string, IProgress<GebaeudeImportFortschritt>, CancellationToken, Task<GebaeudeLesestand>>)gaben["Lesen"];
                if (!(await lesen(pfad, null, CancellationToken.None)).Gelesen) continue;
                var zuordnen = (Func<GebaeudeZuordnungsanfrage, GebaeudeImportStand>)gaben["Zuordnen"];
                GebaeudeImportStand Anfrage(params GebaeudePlanschritt[] schritte)
                    => zuordnen(new GebaeudeZuordnungsanfrage(0, null, new Dictionary<string, bool>(), null, null, IfcImportProfil.ZONENREGEL_Z6,
                                                              null, false, schritte.Length > 0 ? schritte : null,
                                                              schritte.Length > 0 ? new Dictionary<string, bool>() : null));
                GebaeudeZonenplanDaten vorher = Anfrage().Zonierung?.Plan;
                if (vorher == null || h.Plan?.Regel != IfcImportProfil.ZONENREGEL_Z6) continue;
                Planzone a = h.Plan.Zonen.FirstOrDefault(z => z.Profil?.Id != null && z.Quelle?.Art == RaumnutzungSchema.ZUORDNUNG_IFC);
                Planzone b = h.Plan.Zonen.FirstOrDefault(z => a != null && z.Schluessel != a.Schluessel);
                if (a == null || b == null) continue;

                // Zone B von Hand auf Lager, dann ändert das Blatt die Zuordnung der Klasse von Zone A auf Schule.
                long lager = Id(RaumnutzungSaat.LAGER), schule = Id(RaumnutzungSaat.SCHULE);
                Assert.NotEqual(schule, a.Profil.Id);
                var hand = new GebaeudePlanschritt(GebaeudePlanschrittArt.NUTZUNG, Zone: b.Schluessel, Nutzung: "#" + lager);
                Assert.Equal("#" + lager, Anfrage(hand).Zonierung!.Plan!.Zonen.Single(z => z.Schluessel == b.Schluessel).Nutzung);
                Assert.True(ctrl.ZuordnungSetzen(a.Quelle.Art, a.Quelle.Schluessel, schule).Ok);

                GebaeudeZonenplanDaten nachher = Anfrage(hand).Zonierung!.Plan!;
                GebaeudePlanzoneDaten za = nachher.Zonen.Single(z => z.Schluessel == a.Schluessel);
                GebaeudePlanzoneDaten zb = nachher.Zonen.Single(z => z.Schluessel == b.Schluessel);
                Assert.Equal("#" + schule, za.Nutzung);
                Assert.Equal(RaumnutzungSaat.SCHULE, za.Herleitung!.Profil);
                Assert.Equal(RaumnutzungSchema.ZUORDNUNG_IFC, za.Herleitung.Quellart);
                Assert.Equal("#" + lager, zb.Nutzung);
                Assert.Equal(Profilquelle.HAND, zb.Herleitung!.Quellart);
                return;
            }
            Assert.Fail("Keine Z6-Probe mit zwei Zonen und einer Vorbelegung aus der Nutzungsklasse.");
        }

        private string Merken(string pfad)
        {
            _dateien.Add(pfad);
            return pfad;
        }

        private static string Wurzel()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            while (d != null && !File.Exists(Path.Combine(d.FullName, "EPOS.Kern", "EPOS.Kern.csproj")))
                d = d.Parent;
            Assert.True(d != null, "Die Repowurzel ist vom Ausgabeordner aus nicht zu finden.");
            return d.FullName;
        }
    }
}
