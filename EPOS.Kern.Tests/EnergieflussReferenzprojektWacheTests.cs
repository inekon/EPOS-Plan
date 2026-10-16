using System;
using System.Linq;
using WindowsFormsApplication1;
using WindowsFormsApplication1.Zeichnung;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Wache des Energieflussbilds an vier Referenzprojekten</b> (Konzept Energieflussbild, Abschnitte 1.2 und 6):
    /// rechnet 1018 (BHKW, Kessel, Puffer), 1046 (Wärmepumpe mit Heizstab, Kessel, Photovoltaik, Speicherflotte),
    /// 1058 (AK3: Wärmepumpe heizt und kühlt, BHKW, Elektrokessel, Puffer, Stromspeicher) und 1063 (Kältemaschine mit
    /// Kältespeicher und eigenem Zähler, BHKW, Elektrokessel, Stromspeicher) auf der Testdatenbank, lädt das Flussmodell
    /// über <see cref="EnergieflussCtrl.Lade"/> aus dem gespeicherten Ergebnis und hält die Bänder gegen die Zahlen der
    /// Bilanztabelle 1.2 (Toleranz 0,01 MWh/a), jeden geprüften Knoten geschlossen; danach zeichnet sie jede Bilanz über
    /// <see cref="EnergieflussAbbildung.Bild"/> in allen drei Fassungen (SK-12, <see cref="Gezeichnet"/>).
    /// <para><b>Abweichung von der Tabelle 1.2:</b> Bei 1058 und 1063 rechnet der Flussbau den Stromspeicher mit
    /// (Ladung, Verlust, Füllstandsänderung), bei 1063 den Kältestrom des eigenen Zählers neben dem Stromknoten
    /// (<c>Kaeltekaskade.NebenDerStufenrechnung</c>); die bilanzierte BHKW-Einspeisung trifft damit die Einspeisung der
    /// Speicherzeile (<c>Einspeisung_Mit</c>, ein unabhängiges Aggregat des Laufs), nicht den Rest der Tabelle.</para>
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class EnergieflussReferenzprojektWacheTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public EnergieflussReferenzprojektWacheTests(ITestOutputHelper aus) { _aus = aus; }

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const double TOL = 0.01;

        private (EnergieflussBilanz m, VariantenDaten v) Rechne(int projekt)
        {
            var lauf = new SimulationRunner();
            int k = lauf.SimuliereUndSpeichere(projekt, out string fehler);
            Assert.True(k > 0, "Lauf " + projekt + " gescheitert: " + fehler);
            EnergieflussBilanz m = EnergieflussCtrl.Lade(projekt, true, false);
            Assert.True(m.Simuliert);
            Assert.False(m.Leer);
            foreach (EnergieflussFluss f in m.Fluesse) _aus.WriteLine(f.Zeigertext);
            foreach (string h in m.Hinweise) _aus.WriteLine("Hinweis: " + h);
            // Jeder geprüfte Knoten schließt (samt Ausgleichsband).
            foreach (EnergieflussKnoten n in m.Knoten.Where(n => n.Geprueft))
                Assert.True(Math.Abs(n.ZuflussMwh - n.AbflussMwh) <= Energiefluss.Toleranz(n.ZuflussMwh), n.Id);
            Gezeichnet(m, projekt);
            return (m, EnergievergleichCtrl.Lade(projekt, true));
        }

        /// <summary>Die Umgebungsvariable, unter deren Ordner die Wache die Bilder (PNG) zur Sichtprüfung ablegt; leer = keine Ablage.</summary>
        public const string PNG_ORDNER = "EPOS_ENERGIEFLUSS_PNG_ORDNER";

        /// <summary>
        /// Bilanz → Bilddaten → Zeichenmodell in breit, schmal und Bericht (SK-12): ohne Ausnahme, deterministisch, jedes
        /// Band und jeder Knoten gezeichnet, jedes Band schließt an seine Knoten an, jeder Verlustknoten steht in der Lücke
        /// rechts neben seinem Eigentümer. Ist <see cref="PNG_ORDNER"/> gesetzt, liegen die PNG danach dort.
        /// </summary>
        private static void Gezeichnet(EnergieflussBilanz m, int projekt)
        {
            const float KB = ChartRenderer.EF_KNOTENBREITE;
            EnergieflussBildDaten d = EnergieflussAbbildung.Bild(m);
            Assert.Equal(m.Knoten.Count, d.Knoten.Count);
            Assert.Equal(m.Fluesse.Count, d.Baender.Count);
            string ordner = Environment.GetEnvironmentVariable(PNG_ORDNER);
            foreach (var (name, mass, werte) in new[] { ("breit", ChartRenderer.ENERGIEFLUSS_BREIT, false),
                                                       ("schmal", ChartRenderer.ENERGIEFLUSS_SCHMAL, false),
                                                       ("bericht", ChartRenderer.ENERGIEFLUSS_BERICHT, true) })
            {
                Zeichenmodell z = ChartRenderer.EnergieflussModell(d, mass, werte);
                Assert.True(z.Gleicht(ChartRenderer.EnergieflussModell(d, mass, werte)), projekt + " " + name + " deterministisch");
                var rechtecke = z.Befehle.OfType<Rechteck>().Where(r => r.Marke != null && r.Marke.StartsWith("knoten:", StringComparison.Ordinal))
                                 .ToDictionary(r => r.Marke.Substring("knoten:".Length), r => r);
                var baender = z.Befehle.OfType<Pfad>().Where(p => p.Marke != null && p.Marke.StartsWith("fluss:", StringComparison.Ordinal)).ToList();
                Assert.Equal(m.Knoten.Count, rechtecke.Count);
                Assert.Equal(m.Fluesse.Count, baender.Count);
                int n = ChartRenderer.EF_STUETZSTELLEN;
                foreach (EnergieflussFluss f in m.Fluesse)
                {
                    Pfad p = Assert.Single(baender, x => x.Marke == f.Marke);
                    Rechteck von = rechtecke[f.Von], nach = rechtecke[f.Nach];
                    Assert.Equal(von.X + KB, p.Punkte[0].X, 3);
                    Assert.Equal(nach.X, p.Punkte[n - 1].X, 3);
                    Assert.InRange(p.Punkte[0].Y, von.Y - 0.01f, von.Y + von.Hoehe + 0.01f);
                    Assert.InRange(p.Punkte[n - 1].Y, nach.Y - 0.01f, nach.Y + nach.Hoehe + 0.01f);
                }
                foreach (EnergieflussKnoten k in m.Knoten.Where(k => k.Art == EnergieflussKnotenart.Verlust))
                {
                    string eigentuemer = Assert.Single(m.Fluesse, f => f.Nach == k.Id).Von;
                    Rechteck o = rechtecke[eigentuemer], v = rechtecke[k.Id];
                    float naechste = rechtecke.Where(r => m.KnotenMit(r.Key).Art != EnergieflussKnotenart.Verlust && r.Value.X > o.X + KB)
                                              .Select(r => r.Value.X).DefaultIfEmpty(z.Breite).Min();
                    Assert.True(v.X > o.X + KB && v.X + v.Breite < naechste, projekt + " " + name + ": " + k.Id + " in der Lücke");
                }
                if (!string.IsNullOrEmpty(ordner))
                {
                    System.IO.Directory.CreateDirectory(ordner);
                    System.IO.File.WriteAllBytes(System.IO.Path.Combine(ordner, projekt + "_" + name + ".png"), ChartRenderer.EnergieflussBild(d, mass, werte));
                }
            }
        }

        private static string Id(EnergieflussBilanz m, string anfang)
            => Assert.Single(m.Knoten, k => k.Id.StartsWith(anfang, StringComparison.Ordinal)).Id;

        private static void Nahe(double erwartet, double ist, string was)
            => Assert.True(Math.Abs(erwartet - ist) <= TOL, was + ": erwartet " + erwartet + ", ist " + ist);

        private static void AlleGeschlossen(EnergieflussBilanz m)
        {
            Assert.All(m.Knoten.Where(k => k.Geprueft), k => Assert.True(k.Geschlossen, k.Id));
            Assert.Empty(m.Hinweise);
            Assert.Null(m.KnotenMit(Energiefluss.NICHT_ZUGEORDNET));
            Assert.Null(m.KnotenMit(Energiefluss.NICHT_ZUGEORDNET_QUELLE));
        }

        [Fact]
        public void Projekt_1018_BHKW_Kessel_Puffer_schliesst_mit_bilanzierter_Einspeisung()
        {
            if (!_db.Vorhanden) return;
            var (m, _) = Rechne(1018);
            Nahe(53.890, m.KnotenMit(Energiefluss.WAERME).ZuflussMwh, "Wärme Zufluss");
            string p = Id(m, Energiefluss.PUFFER);
            Nahe(38.949, m.Fluss(Energiefluss.WAERME, p), "Puffer Ladung");
            Nahe(37.781, m.Fluss(p, Energiefluss.HEIZUNG), "Puffer Entladung");
            Nahe(1.158 + 0.011, m.Fluss(p, Energiefluss.VERLUST + p), "Puffer Verlust samt Füllstand");
            Nahe(52.720, m.KnotenMit(Energiefluss.HEIZUNG).ZuflussMwh, "Heizung");
            EnergieflussFluss e = m.FlussMit(Energiefluss.STROM, Energiefluss.EINSPEISUNG_BHKW);
            Nahe(18.070, e.Mwh, "Einspeisung BHKW");
            Assert.Equal(EnergieflussHerkunft.Bilanziert, e.Herkunft);
            Assert.Null(m.KnotenMit(Energiefluss.STROMBEDARF));
            AlleGeschlossen(m);
        }

        [Fact]
        public void Projekt_1046_Waermepumpe_Flotte_zeigt_den_Eigenverbrauch_als_nicht_zugeordnet()
        {
            if (!_db.Vorhanden) return;
            var (m, _) = Rechne(1046);
            Nahe(71.410, m.KnotenMit(Energiefluss.WAERME).ZuflussMwh, "Wärme Zufluss");
            Nahe(37.810, m.Fluss(Energiefluss.HEIZSTAB, Energiefluss.WAERME), "Heizstab 1:1");
            Nahe(37.810, m.Fluss(Energiefluss.STROM, Energiefluss.HEIZSTAB), "Heizstab Strom");
            Nahe(25.090, m.Fluss(Energiefluss.WP_HEIZEN, Energiefluss.WAERME), "Wärmepumpe");
            Nahe(69.110, m.KnotenMit(Energiefluss.STROM).ZuflussMwh, "Strom Zufluss");
            string s = Id(m, Energiefluss.STROMSPEICHER);
            Nahe(1.208, m.Fluss(Energiefluss.STROM, s), "Stromspeicher Ladung");
            Nahe(1.045, m.Fluss(s, Energiefluss.STROMBEDARF), "Stromspeicher Entladung");
            Nahe(0.148, m.Fluss(s, Energiefluss.VERLUST + s), "Stromspeicher Verlust");
            Nahe(0.014, m.Fluss(s, Energiefluss.FUELLSTAND + s), "Stromspeicher Füllstand");
            Nahe(0.903, m.Fluss(Energiefluss.STROM, Energiefluss.EINSPEISUNG_PV), "Einspeisung PV");
            // Der Eigenverbrauch des Speichersystems (0,438) steht nicht im Ergebnisbaum: Band „nicht zugeordnet“.
            EnergieflussFluss nz = m.FlussMit(Energiefluss.STROM, Energiefluss.NICHT_ZUGEORDNET);
            Assert.NotNull(nz);
            Nahe(0.44, nz.Mwh, "nicht zugeordnet");
            Assert.False(m.KnotenMit(Energiefluss.STROM).Geschlossen);
            Assert.Equal("Bilanz am Knoten Strom schließt nicht: 0,44 MWh/a nicht zugeordnet", Assert.Single(m.Hinweise));
            Assert.Null(m.KnotenMit(Energiefluss.EINSPEISUNG_BHKW));
            Assert.True(m.KnotenMit(Energiefluss.WAERME).Geschlossen);
        }

        [Fact]
        public void Projekt_1058_AK3_belegt_die_Umweltwaerme_und_schliesst_Waerme_Kaelte_Strom()
        {
            if (!_db.Vorhanden) return;
            var (m, v) = Rechne(1058);
            Nahe(72.030, m.KnotenMit(Energiefluss.WAERME).ZuflussMwh, "Wärme Zufluss");
            EnergieflussFluss u = m.FlussMit(Energiefluss.UMWELTWAERME, Energiefluss.WP_HEIZEN);
            Nahe(43.090, u.Mwh, "Umweltwärme");
            Assert.Equal(EnergieflussHerkunft.Abgeleitet, u.Herkunft);
            // Die Ableitung deckt sich mit dem gespeicherten Erdreichentzug (Konzept 1.3).
            double entzug = ErdreichErgebnisSpeicher.Lesen(1058).Where(z => z.Pruefzeile == ErdreichErgebnisSpeicher.ZEILE_JAHRESENTZUG).Sum(z => z.Istwert ?? 0.0);
            Nahe(Energiefluss.AusKwh(entzug), u.Mwh, "Erdreichentzug");
            string p = Id(m, Energiefluss.PUFFER);
            Nahe(72.024, m.Fluss(Energiefluss.WAERME, p), "Puffer Ladung");
            Nahe(70.125, m.Fluss(p, Energiefluss.HEIZUNG), "Puffer Entladung");
            Nahe(1.733 + 0.165, m.Fluss(p, Energiefluss.VERLUST + p), "Puffer Verlust samt Füllstand");
            Nahe(3.810, m.KnotenMit(Energiefluss.KAELTE).ZuflussMwh, "Kälte Zufluss");
            Nahe(3.810, m.KnotenMit(Energiefluss.KUEHLUNG).ZuflussMwh, "Kühlung");
            Assert.Equal("Wärmepumpe (Kühlen)", m.KnotenMit(Energiefluss.WP_KUEHLEN).Name);
            Nahe(35.140, m.KnotenMit(Energiefluss.STROM).ZuflussMwh, "Strom Zufluss");
            EnergieflussFluss e = m.FlussMit(Energiefluss.STROM, Energiefluss.EINSPEISUNG_BHKW);
            Assert.Equal(EnergieflussHerkunft.Bilanziert, e.Herkunft);
            Nahe(Energiefluss.AusKwh(Assert.Single(v.Ergebnis.Stromspeicher).Einspeisung_Mit), e.Mwh, "Einspeisung BHKW gegen Speicherzeile");
            AlleGeschlossen(m);
        }

        [Fact]
        public void Projekt_1063_Kaeltemaschine_mit_Kaeltespeicher_und_eigenem_Zaehler_schliesst()
        {
            if (!_db.Vorhanden) return;
            var (m, v) = Rechne(1063);
            Nahe(75.320, m.KnotenMit(Energiefluss.WAERME).ZuflussMwh, "Wärme Zufluss");
            Nahe(0.06, m.Fluss(Energiefluss.UNGEDECKT, Energiefluss.HEIZUNG), "ungedeckt");
            Nahe(4.153, m.KnotenMit(Energiefluss.KAELTE).ZuflussMwh, "Kälte Zufluss");
            string ks = Id(m, Energiefluss.KAELTESPEICHER);
            Nahe(2.478, m.Fluss(Energiefluss.KAELTE, ks), "Kältespeicher Ladung");
            Nahe(2.376, m.Fluss(ks, Energiefluss.KUEHLUNG), "Kältespeicher Entladung");
            Nahe(0.102, m.Fluss(ks, Energiefluss.VERLUST + ks), "Kältespeicher Verlust");
            Nahe(4.051, m.KnotenMit(Energiefluss.KUEHLUNG).ZuflussMwh, "Kühlung");
            string km = Id(m, Energiefluss.KAELTEMASCHINE);
            ErgebnisKaeltemaschineModel z = Assert.Single(v.Ergebnis.Kaeltemaschinen);
            Assert.True(z.Kuehl_EigenerZaehler);
            Nahe(z.Stromverbrauch_MWh + z.Hilfsstrom_MWh, m.Fluss(Energiefluss.KUEHLSTROM_EIGEN, km), "Kältestrom eigener Zähler");
            Assert.Equal(0.0, m.Fluss(Energiefluss.STROM, km));
            Nahe(42.000, m.KnotenMit(Energiefluss.STROM).ZuflussMwh, "Strom Zufluss");
            EnergieflussFluss e = m.FlussMit(Energiefluss.STROM, Energiefluss.EINSPEISUNG_BHKW);
            Assert.Equal(EnergieflussHerkunft.Bilanziert, e.Herkunft);
            Nahe(Energiefluss.AusKwh(Assert.Single(v.Ergebnis.Stromspeicher).Einspeisung_Mit), e.Mwh, "Einspeisung BHKW gegen Speicherzeile");
            AlleGeschlossen(m);
        }
    }
}
