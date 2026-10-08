using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Abnahme der Stufe G5 am Rechenweg</b> (Abstimmung G5, A1–A6; Sitzung Gebäudesimulation): hält, was die Abnahme an den
    /// Importproben gemessen hat — ohne den Import zu ändern und ohne eingefrorene Zahlen.
    /// <list type="bullet">
    /// <item><b>A5:</b> An jeder Probe mit Mengensätzen bleibt der Mengensatz Quelle der Fläche, und jede Abweichung des
    /// Bauteilkörpers über <see cref="IfcBauteilkoerper.ABWEICHUNG_GRENZE"/> steht in der Zusammenfassung ihrer Bauteilart
    /// (ab <see cref="IfcBauteilkoerper.EINZELWARNUNG_GRENZE"/> zusätzlich als Warnung) — keine stille Abweichung.</item>
    /// <item><b>A1–A4:</b> Die Kleinhausprobe ohne Mengensätze geht über den Körperweg in eine Arbeitskopie der Testdatenbank;
    /// jede gespeicherte Fläche trägt ihre Herkunft (<c>Tab_Bauteil.Flaechenherkunft</c>), Öffnungen sind eigene Bauteile,
    /// Trennflächen tragen Nachbarzone und Zuordnung.</item>
    /// <item><b>A6:</b> Die Jahresheizwärme des Körperwegs liegt näher an der Gegenprobe mit Mengensätzen als der Rückfall
    /// ohne Raumzuordnung (<see cref="GebaeudeImportProfil.KoerperflaechenAus"/>) — als Aussage, nicht als Zahl.</item>
    /// </list>
    /// Die Klasse rechnet drei Jahresläufe nach VDI 6007 auf je einer Arbeitskopie (Klima und Einstellungen des
    /// Referenzprojekts 1045); zusammen rund eine halbe Minute — die Aussage A6 braucht alle drei Läufe.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class G5AbnahmeRechenwegTests : IDisposable
    {
        private const int PROJEKT = 1045;
        private const string P = "IMP_IFC_PROT_";
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;

        public G5AbnahmeRechenwegTests(ITestOutputHelper ausgabe) => _ausgabe = ausgabe;

        public void Dispose() => _kultur.Dispose();

        private static string F(double w) => w.ToString("0.###", CultureInfo.InvariantCulture);

        // ==================================================================
        //  A5 — Körperfläche gegen Mengensatz
        // ==================================================================

        /// <summary>Die Schlüssel der Zusammenfassung je Gruppe (<see cref="IfcAbbildBauer.Koerpergruppe"/>).</summary>
        private static readonly string[] ZUSAMMENFASSUNG =
        {
            P + "KOERPER_ABWEICHUNGEN_AUSSENWAND", P + "KOERPER_ABWEICHUNGEN_INNENWAND", P + "KOERPER_ABWEICHUNGEN_DACH",
            P + "KOERPER_ABWEICHUNGEN_BODEN_DECKE", P + "KOERPER_ABWEICHUNGEN_SONSTIGE",
        };

        /// <summary>Eine Vergleichseinheit: ein Bauteil mit Körper und eigenem Mengensatz oder ein Körper mit seinen Teilen.</summary>
        private sealed class Vergleich
        {
            public string Name;
            public Bauteilart Art;
            public double KoerperM2;
            public double BruttoM2;
            public double NettoM2;
            public double Abweichung;
            public bool Teile;
            /// <summary>Körper ohne eigenen Mengensatz mit Teilen: Er trägt nur den Rest (Info <c>KOERPER_REST</c>), kein Vergleich.</summary>
            public bool Rest;
            public bool MengensatzQuelle;
        }

        /// <summary>Misst je Bauteil (bzw. je Körper mit Teilen ohne Darstellung) Körper gegen Mengensatz.</summary>
        private static List<Vergleich> Messen(GebaeudeImportAblauf a)
        {
            var liste = new List<Vergleich>();
            foreach (AbbildGebaeude g in a.Abbild.Gebaeude)
            {
                List<AbbildBauteil> alle = g.Bauteile.Concat(g.Bauteile.SelectMany(b => b.Oeffnungen)).Distinct().ToList();
                foreach (AbbildBauteil b in alle.Where(x => x.Koerperflaeche != null && x.Name != null))
                {
                    string stamm = IfcAbbildBauer.Namensstamm(b.Name);
                    List<AbbildBauteil> teile = alle.Where(t => t != b && t.Koerperflaeche == null && t.Name != null
                        && t.Quelltyp == b.Quelltyp && IfcAbbildBauer.Namensstamm(t.Name) == stamm && t.BruttoflaecheM2.HasValue).ToList();
                    bool eigene = b.Flaechenherkunft == Flaechenherkunft.Mengensatz;
                    if (!eigene && teile.Count == 0) continue;
                    List<AbbildBauteil> mengen = eigene ? teile.Prepend(b).ToList() : teile;
                    double brutto = mengen.Sum(x => x.BruttoflaecheM2.Value);
                    double netto = mengen.Sum(x => x.NettoflaecheM2 ?? x.BruttoflaecheM2.Value);
                    double k = b.Koerperflaeche.FlaecheM2;
                    liste.Add(new Vergleich
                    {
                        Name = b.Name, Art = b.Art, KoerperM2 = k, BruttoM2 = brutto, NettoM2 = netto, Teile = teile.Count > 0, Rest = !eigene,
                        Abweichung = Math.Min(IfcBauteilkoerper.Abweichung(brutto, k), IfcBauteilkoerper.Abweichung(netto, k)),
                        MengensatzQuelle = mengen.All(x => x.Flaechenherkunft == Flaechenherkunft.Mengensatz),
                    });
                }
            }
            return liste;
        }

        public static IEnumerable<object[]> ProbenMitMengen() => new[]
        {
            new object[] { "ifc4_g5_kleinhaus_mit_mengen.ifc" },
            new object[] { "ifc4_g5_mengen_gegenprobe.ifc" },
            new object[] { "ifc4_g5_oeffnungen_mengen.ifc" },
            new object[] { "ifc4_g5_abweichungen.ifc" },
            new object[] { "ifc4_g5_flachdach_teile.ifc" },
            new object[] { "ifc4_g5_aussparung.ifc" },
        };

        [Theory]
        [MemberData(nameof(ProbenMitMengen))]
        public void A5_Mengensatz_bleibt_Quelle_und_keine_Abweichung_ueber_2_Prozent_bleibt_still(string probe)
        {
            GebaeudeImportAblauf a = KoerperflaechenTests.Lesen(probe);
            List<Vergleich> v = Messen(a);
            Assert.NotEmpty(v);
            foreach (Vergleich x in v)
                _ausgabe.WriteLine("A5 " + probe + " | " + x.Name + " | " + x.Art + " | Körper " + F(x.KoerperM2) + " | Menge brutto " + F(x.BruttoM2)
                    + " netto " + F(x.NettoM2) + " | " + F(x.Abweichung * 100.0) + " % | Teile " + x.Teile + " | Rest " + x.Rest + " | Quelle Mengensatz " + x.MengensatzQuelle);
            foreach (PruefMeldung m in a.Abbild.Meldungen.Where(m => m.Schluessel.StartsWith(P + "KOERPER", StringComparison.Ordinal)))
                _ausgabe.WriteLine("A5 " + probe + " Meldung " + m.Stufe + " " + m.Schluessel + " [" + string.Join("; ", m.Werte) + "]");

            // Der Mengensatz bleibt Quelle der Fläche.
            Assert.All(v, x => Assert.True(x.MengensatzQuelle, x.Name + ": Mengensatz nicht Quelle"));

            // Je Bauteilart: so viele Abweichungen über der Grenze, wie die Zusammenfassung nennt.
            for (int gr = 0; gr < ZUSAMMENFASSUNG.Length; gr++)
            {
                int gemessen = v.Count(x => !x.Rest && IfcAbbildBauer.Koerpergruppe(x.Art) == gr && x.Abweichung > IfcBauteilkoerper.ABWEICHUNG_GRENZE);
                PruefMeldung m = a.Abbild.Meldungen.SingleOrDefault(y => y.Schluessel == ZUSAMMENFASSUNG[gr]);
                int gemeldet = m == null ? 0 : int.Parse(m.Werte[0], CultureInfo.InvariantCulture);
                Assert.True(gemessen == gemeldet, probe + ", " + ZUSAMMENFASSUNG[gr] + ": gemessen " + gemessen + ", gemeldet " + gemeldet);
            }
            // Ab der Einzelwarnung steht das Bauteil mit Namen in einer Warnung.
            // Trägt ein Körper ohne eigenen Mengensatz mehr als seine Teile, steht der Rest in einer Info.
            foreach (Vergleich x in v.Where(x => x.Rest && x.Abweichung > IfcBauteilkoerper.ABWEICHUNG_GRENZE))
                Assert.Contains(a.Abbild.Meldungen, m => m.Schluessel == P + "KOERPER_REST" && m.Werte.Length > 1 && m.Werte[1].Contains(IfcAbbildBauer.Namensstamm(x.Name), StringComparison.Ordinal));
            foreach (Vergleich x in v.Where(x => !x.Rest && x.Abweichung >= IfcBauteilkoerper.EINZELWARNUNG_GRENZE))
                Assert.Contains(a.Abbild.Meldungen, m => m.Stufe == PruefStufe.Warnung && m.Schluessel.StartsWith(P + "KOERPER_ABWEICHUNG", StringComparison.Ordinal)
                                                       && m.Werte.Length > 0 && x.Name.StartsWith(m.Werte[0], StringComparison.Ordinal));
        }

        // ==================================================================
        //  A1–A4 und A6 — Arbeitskopie und Jahreslauf
        // ==================================================================

        /// <summary>Eine gespeicherte Bauteilzeile der Arbeitskopie.</summary>
        private sealed class Zeile
        {
            public string Zone, Name, Art, Rand, Trennflaeche, Herkunft;
            public double Flaeche;
            public double? Neigung, Azimut;
            public long? Nachbarzone;
        }

        private sealed class Lauf
        {
            public bool Abgelehnt;
            public string Ablehnung;
            public double Mwh, SpitzeKw;
            public int Zonen;
            public List<Zeile> Zeilen = new List<Zeile>();
            public List<string> Zonenliste = new List<string>();
            public double Opak => Zeilen.Where(z => z.Art != DbWerte.BAUTEILART_FENSTER && z.Art != DbWerte.BAUTEILART_TUER).Sum(z => z.Flaeche);
        }

        /// <summary>Schreibt den Zonenvorschlag in eine frische Arbeitskopie, liest die Bauteile zurück und rechnet.</summary>
        private static Lauf Rechnen(GebaeudeImportAblauf ablauf)
        {
            using (var db = new TestDatenbank())
            {
                if (!db.Vorhanden) return null;
                var l = new Lauf();
                var ctrl = new ProjektGebaeudeCtrl();
                ctrl.ReadAll(PROJEKT);
                ProjektGebaeudeModel g = ctrl.items[0];
                GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(ablauf, 0, null);
                if (v.Abgelehnt)
                {
                    l.Abgelehnt = true;
                    l.Ablehnung = string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler).Select(m => m.Schluessel + " [" + string.Join("; ", m.Werte) + "]"));
                    return l;
                }
                using (DbVorgang vorgang = DataRepository.Vorgang())
                {
                    GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v, vorgang);
                    Assert.True(e.Ok, e.Meldung);
                    vorgang.Commit();
                }
                DataTable t = DataRepository.GetDataTable(
                    "SELECT z.Bezeichner AS Zone, b.Bezeichner, b.Bauteilart, b.Flaeche, b.Neigung, b.Azimut, b.Randbedingung, b.ID_Nachbarzone, " +
                    "b.Trennflaeche_Zuordnung, b.Flaechenherkunft FROM Tab_Bauteil b JOIN Tab_Zone z ON z.ID = b.ID_Zone " +
                    "WHERE z.ID_Gebaeude = ? ORDER BY z.Rang, b.Rang, b.ID", new DbParam("@g", g.ID_Gebaeude));
                foreach (DataRow r in t.Rows)
                    l.Zeilen.Add(new Zeile
                    {
                        Zone = r[0] as string, Name = r[1] as string, Art = r[2] as string, Flaeche = Convert.ToDouble(r[3], CultureInfo.InvariantCulture),
                        Neigung = r[4] is DBNull ? (double?)null : Convert.ToDouble(r[4], CultureInfo.InvariantCulture),
                        Azimut = r[5] is DBNull ? (double?)null : Convert.ToDouble(r[5], CultureInfo.InvariantCulture),
                        Rand = r[6] as string, Nachbarzone = r[7] is DBNull ? (long?)null : Convert.ToInt64(r[7], CultureInfo.InvariantCulture),
                        Trennflaeche = r[8] as string, Herkunft = r[9] as string,
                    });

                foreach (DataRow r in DataRepository.GetDataTable("SELECT ID, Bezeichner, IstBeheizt FROM Tab_Zone WHERE ID_Gebaeude = ? ORDER BY Rang, ID",
                                                                  new DbParam("@g", g.ID_Gebaeude)).Rows)
                    l.Zonenliste.Add(Convert.ToString(r[0], CultureInfo.InvariantCulture) + " " + r[1] + " beheizt " + Convert.ToString(r[2], CultureInfo.InvariantCulture));

                ctrl = new ProjektGebaeudeCtrl();
                ctrl.ReadAll(PROJEKT);
                ProjektGebaeudeModel mit = ctrl.items[0];
                mit.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
                Assert.All(mit.Zonen, z => Assert.Null(z.Lesefehler));
                var projekt = new ProjektCtrl();
                projekt.ReadSingle(PROJEKT);
                var sim = new SimulationWaermebedarf { m_ID_Projekt = PROJEKT };
                sim.KlimakalenderLesen(projekt.m_ID_Klimaregion);
                var werte = new double[8760];
                SimulationProtokoll p = SimulationProtokoll.NeuStarten();
                Assert.True(sim.HeizwaermeEinesGebaeudes(mit, 0, werte), string.Join(" | ", p.Hinweise));
                Assert.True(p.IstFehlerfrei, string.Join(" | ", p.Hinweise));
                l.Mwh = sim.GebaeudeErgebnisse.Ergebnis(0).JahresheizwaermeMwh;
                l.SpitzeKw = sim.GebaeudeErgebnisse.Ergebnis(0).SpitzeKw;
                l.Zonen = mit.Zonen.Count;
                return l;
            }
        }

        private void Ausgeben(string weg, Lauf l)
        {
            if (l.Abgelehnt) { _ausgabe.WriteLine("A6 " + weg + ": abgelehnt — " + l.Ablehnung); return; }
            _ausgabe.WriteLine("A6 " + weg + ": " + F(l.Mwh) + " MWh/a, Spitze " + F(l.SpitzeKw) + " kW, " + l.Zonen + " Zonen, opak " + F(l.Opak)
                + " m², Herkunft " + string.Join(", ", l.Zeilen.GroupBy(z => z.Herkunft ?? "NULL").Select(x => x.Key + " " + x.Count()))
                + ", Zonen: " + string.Join(", ", l.Zonenliste));
            foreach (Zeile z in l.Zeilen)
                _ausgabe.WriteLine("   " + weg + " | " + z.Zone + " | " + z.Name + " | " + z.Art + " | " + F(z.Flaeche) + " m² | N "
                    + (z.Neigung.HasValue ? F(z.Neigung.Value) : "-") + " | A " + (z.Azimut.HasValue ? F(z.Azimut.Value) : "-") + " | " + z.Rand
                    + (z.Nachbarzone.HasValue ? " → " + z.Nachbarzone + " (" + z.Trennflaeche + ")" : "") + " | " + (z.Herkunft ?? "NULL"));
        }

        /// <summary>
        /// Die Gegenprobe mit Mengensätzen und Raumgrenzen trägt an Wänden, Fenstern und Dach keinen Azimut und am Dach die
        /// Neigung 0° (Befund an die Sitzung IFC, Protokoll G5-A): Der Vorschlag lehnt sie mit <c>IMP_BAUTEIL_PROT_AZIMUT_FEHLT</c>
        /// ab. Für den Bezug der Heizwärme ergänzt die Abnahme die Orientierung im gelesenen Abbild aus dem Bauteilkörper (Fenster:
        /// die des Wirts) — die Flächen bleiben die des Mengensatz- und Raumgrenzenwegs. Der Import bleibt unverändert.
        /// </summary>
        private static GebaeudeImportAblauf OrientierungErgaenzen(GebaeudeImportAblauf a, out int ergaenzt)
        {
            ergaenzt = 0;
            foreach (AbbildGebaeude g in a.Abbild.Gebaeude)
                foreach (AbbildBauteil b in g.Bauteile.Where(x => x.Koerperflaeche != null))
                {
                    if (!b.AzimutGrad.HasValue && b.Koerperflaeche.AzimutGrad.HasValue) { b.AzimutGrad = b.Koerperflaeche.AzimutGrad; ergaenzt++; }
                    if (b.Art == Bauteilart.Dach && b.NeigungGrad != b.Koerperflaeche.NeigungGrad) { b.NeigungGrad = b.Koerperflaeche.NeigungGrad; ergaenzt++; }
                    foreach (AbbildBauteil o in b.Oeffnungen.Where(o => !o.AzimutGrad.HasValue)) { o.AzimutGrad = b.AzimutGrad; ergaenzt++; }
                }
            return a;
        }

        [Fact]
        public void A1_bis_A4_und_A6_Koerperweg_gegen_Mengensatz_und_Rueckfall()
        {
            GebaeudeImportAblauf ohne = KoerperflaechenTests.Lesen(KoerperflaechenTests.OHNE);
            _ausgabe.WriteLine("A2 Nordwinkel: " + ohne.Abbild.NordwinkelHerkunft + " " + (ohne.Abbild.NordwinkelWirksamGrad.HasValue ? F(ohne.Abbild.NordwinkelWirksamGrad.Value) : "-"));
            Lauf koerper = Rechnen(ohne);
            if (koerper == null) return;
            Lauf rueckfall = Rechnen(KoerperflaechenTests.Lesen(KoerperflaechenTests.OHNE, koerperflaechenAus: true));
            GebaeudeImportAblauf mit = KoerperflaechenTests.Lesen(KoerperflaechenTests.MIT);
            Lauf roh = Rechnen(mit);
            _ausgabe.WriteLine("A6 Mengensatz roh: " + (roh.Abgelehnt ? "abgelehnt — " + roh.Ablehnung : "angenommen"));
            Lauf bezug = Rechnen(OrientierungErgaenzen(KoerperflaechenTests.Lesen(KoerperflaechenTests.MIT), out int ergaenzt));
            _ausgabe.WriteLine("A6 Mengensatz: Orientierung ergänzt an " + ergaenzt + " Stellen");
            Ausgeben("Körperweg", koerper);
            Ausgeben("Rückfall", rueckfall);
            Ausgeben("Mengensatz", bezug);
            Assert.False(koerper.Abgelehnt, koerper.Ablehnung);
            Assert.False(rueckfall.Abgelehnt, rueckfall.Ablehnung);
            Assert.False(bezug.Abgelehnt, bezug.Ablehnung);

            // Der Rückfall „schematisch“ im engeren Sinn — keine gelesene Fläche (Herkunft SCHEMATISCH): Für dieses Haus greift
            // keine Vorgabe der Flächen, der Vorschlag lehnt ab; verglichen wird deshalb mit dem Rückfall ohne Raumzuordnung.
            GebaeudeImportAblauf leer = KoerperflaechenTests.Lesen(KoerperflaechenTests.OHNE, koerperflaechenAus: true);
            foreach (AbbildBauteil b in leer.Abbild.Gebaeude[0].Bauteile) { b.BruttoflaecheM2 = null; b.NettoflaecheM2 = null; b.Flaechenherkunft = null; }
            GebaeudeBauteilvorschlag sv = GebaeudeBauteilvorschlag.BildenMitZonen(leer, 0, null);
            _ausgabe.WriteLine("A6 schematisch: " + (sv.Abgelehnt ? "abgelehnt — " + string.Join(" | ", sv.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler).Select(m => m.Schluessel))
                                                                  : sv.Zeilen.Count + " Zeilen, schematisch " + sv.Zeilen.Count(z => z.Flaechenherkunft == Flaechenherkunft.Schematisch)));

            double dKoerper = (koerper.Mwh - bezug.Mwh) / bezug.Mwh, dRueckfall = (rueckfall.Mwh - bezug.Mwh) / bezug.Mwh;
            _ausgabe.WriteLine("A6 Heizwärme gegen Mengensatz: Körperweg " + F(dKoerper * 100.0) + " %, Rückfall " + F(dRueckfall * 100.0)
                + " %; Spitze Körperweg " + F((koerper.SpitzeKw - bezug.SpitzeKw) / bezug.SpitzeKw * 100.0) + " %, Rückfall "
                + F((rueckfall.SpitzeKw - bezug.SpitzeKw) / bezug.SpitzeKw * 100.0) + " %");

            // A6: Der Körperweg liegt näher am Mengensatzweg als der Rückfall.
            Assert.True(Math.Abs(dKoerper) < Math.Abs(dRueckfall), "Körperweg " + F(dKoerper * 100.0) + " %, Rückfall " + F(dRueckfall * 100.0) + " %");

            // A4: jede gespeicherte Fläche trägt ihre Herkunft; der Körperweg schreibt keine schematische Fläche.
            Assert.All(koerper.Zeilen, z => Assert.False(string.IsNullOrEmpty(z.Herkunft), z.Name + ": keine Flächenherkunft"));
            Assert.All(koerper.Zeilen.Where(z => z.Art != DbWerte.BAUTEILART_FENSTER && z.Art != DbWerte.BAUTEILART_TUER),
                       z => Assert.Equal(FlaechenherkunftWerte.KOERPER, z.Herkunft));
            Assert.All(bezug.Zeilen, z => Assert.False(string.IsNullOrEmpty(z.Herkunft), z.Name + ": keine Flächenherkunft"));

            // A1: Nettoflächen positiv, die drei Fenster als eigene Bauteile.
            Assert.All(koerper.Zeilen, z => Assert.True(z.Flaeche > 0.0, z.Name));
            Assert.Equal(3, koerper.Zeilen.Count(z => z.Art == DbWerte.BAUTEILART_FENSTER));

            // A2: Wände und Fenster senkrecht mit Azimut auf den Achsen, das Pultdach 3 : 4 nach Süd.
            foreach (Zeile z in koerper.Zeilen.Where(z => z.Art == DbWerte.BAUTEILART_AUSSENWAND || z.Art == DbWerte.BAUTEILART_FENSTER))
            {
                Assert.Equal(90.0, z.Neigung.Value, 6);
                Assert.Contains(z.Azimut.Value, new[] { 0.0, 90.0, 180.0, 270.0 });
            }
            Zeile dach = Assert.Single(koerper.Zeilen, z => z.Art == DbWerte.BAUTEILART_DACH);
            Assert.Equal(Math.Acos(0.8) * 180.0 / Math.PI, dach.Neigung.Value, 6);
            Assert.Equal(180.0, dach.Azimut.Value, 6);

            // A3: Kellerwände und Boden gegen Erdreich, Decken als Trennflächen zu Nachbarzonen, der Keller als unbeheizte Zone.
            Assert.Equal(5, koerper.Zeilen.Count(z => z.Rand == DbWerte.RANDBEDINGUNG_ERDREICH));
            Assert.All(koerper.Zeilen.Where(z => z.Art == DbWerte.BAUTEILART_DECKE), z => Assert.True(z.Nachbarzone.HasValue, z.Name));
            Assert.Contains(koerper.Zonenliste, z => z.Contains("beheizt 0", StringComparison.Ordinal));
        }
    }
}
