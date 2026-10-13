using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Rundlauf IFC über die Standardsätze</b> (Welle DA-3, E138 F5): ein Gebäude mit zwei Zonen, Lasten, Sollwerten, Kalendern,
    /// Fenstern und Schichten wird exportiert, mit dem eigenen Leser zurückgelesen und Feld für Feld verglichen. Dazu die
    /// Gegenproben: eine Datei nur mit Standardsätzen (ohne <c>EPOS_*</c>-Kalender) ergibt die Konditionierung „aus IFC-Datei
    /// (Standardsätze)“, die herstellereigene Eigenschaft nur einen Vorschlag, eine abweichende Optik eine Meldung.
    /// Alle Werte sind runde Phantasiewerte (synthetische Probe).
    /// </summary>
    public class IfcExportRundlaufTests
    {
        private const int ZONE2 = 12;
        private const double BREITE = 51.05, LAENGE = 13.74;

        /// <summary>Das Probenabbild des gbXML-Rundlaufs, erweitert um eine zweite Zone, Lage, Rahmenanteil und Wärmebrücke.</summary>
        internal static GebaeudeAbbild Abbild(bool mitKalender = true)
        {
            GebaeudeAbbild a = GbxmlExportRundlaufTests.Abbild();
            a.BreiteGrad = BREITE;
            a.LaengeGrad = LAENGE;
            AbbildGebaeude g = a.Gebaeude[0];
            g.Baujahr = 1990;
            AbbildRaum r = g.Raeume.Single(x => x.Kennung == GebaeudeExportKennung.Raum(GbxmlExportProbe.ZONE));
            if (!mitKalender) r.Konditionierung = null;
            else
            {
                r.Konditionierung.Nutzung = "Probenutzung";
                r.Konditionierung.HeizsollTagC = 20.0;
                r.Konditionierung.HeizsollNachtC = 16.0;
            }
            g.Raeume.Add(new AbbildRaum
            {
                Kennung = GebaeudeExportKennung.Raum(ZONE2), Name = "Zone 2", FlaecheM2 = 50.0, VolumenM3 = 150.0, Beheizt = true,
                FlaecheJePersonM2 = 10.0, GeraeteWm2 = 4.0, LichtWm2 = 0.0, SollHeizenC = 21.0, SollKuehlenC = 25.0, LuftwechselJeH = 0.5,
                LuftwechselNutzerJeH = 1.0, ZonenKennung = GebaeudeExportKennung.Zone(ZONE2), ZonenName = "Zone 2",
                GbxmlLasten = new GbxmlRaumlasten { WaermeJePersonW = 100.0, AussenluftJePersonM3h = 40.0 },
            });
            AbbildBauteil sued = g.Bauteile.First(b => b.Oeffnungen.Any(o => o.Art == Bauteilart.Fenster));
            sued.WaermebrueckeWK = 1.5;
            sued.Oeffnungen.First(o => o.Art == Bauteilart.Fenster).Rahmenanteil = 0.3;
            return a;
        }

        private static (GebaeudeAbbild Quelle, GebaeudeAbbild Gelesen, byte[] Datei) Rundlauf(bool mitKalender = true)
        {
            GebaeudeAbbild quelle = Abbild(mitKalender);
            byte[] datei = IfcExportProbe.Schreiben(quelle, null, null, null, out GebaeudeExportBilanz bilanz);
            Assert.True(datei.Length > 0, string.Join(" | ", bilanz.Meldungen.Select(m => m.Schluessel + ": " + string.Join("; ", m.Werte ?? Array.Empty<string>()))));
            return (quelle, IfcExportProbe.Lesen(datei), datei);
        }

        [Fact]
        public void Der_Export_schreibt_die_Standardsaetze_und_die_Raumzonen_bytegleich()
        {
            (_, _, byte[] datei) = Rundlauf();
            string step = Encoding.UTF8.GetString(datei);
            foreach (string s in new[] { "'Pset_SpaceOccupancyRequirements'", "'OccupancyNumber'", "'AreaPerOccupant'", "'Pset_SpaceThermalLoad'",
                                         "'People'", "'Lighting'", "'EquipmentSensible'", "'Pset_SpaceThermalDesign'", "'HeatingDryBulb'",
                                         "'CoolingDryBulb'", "'Pset_SpaceLightingRequirements'", "'ArtificialLighting'", "'GlazingAreaFraction'",
                                         "'Pset_MaterialOptical'", "'SolarReflectanceFront'", "'ThermalIrEmissivityFront'", "IFCSPATIALZONE(",
                                         ".THERMAL.", "'AussenluftJePerson'", "'WaermeJePersonW'" })
                Assert.Contains(s, step, StringComparison.Ordinal);
            Assert.Equal(datei, IfcExportProbe.Schreiben(Abbild()));
        }

        [Fact]
        public void Export_und_Import_geben_Zonen_Lasten_Sollwerte_Fenster_und_Schichten_ohne_Verlust_zurueck()
        {
            (GebaeudeAbbild quelle, GebaeudeAbbild gelesen, _) = Rundlauf();
            int felder = 0;
            void G(double? soll, double? ist, string was)
            {
                Assert.True(soll.HasValue == ist.HasValue && (!soll.HasValue || Math.Abs(soll.Value - ist.Value) <= 1e-6), was + ": " + soll + " ≠ " + ist);
                felder++;
            }
            G(quelle.BreiteGrad, gelesen.BreiteGrad, "Breite");
            G(quelle.LaengeGrad, gelesen.LaengeGrad, "Länge");
            AbbildGebaeude gq = quelle.Gebaeude[0], gl = Assert.Single(gelesen.Gebaeude);
            Assert.Equal(gq.Art, gl.Art);
            Assert.Equal(gq.Baujahr, gl.Baujahr);
            felder += 2;

            foreach (AbbildRaum q in gq.Raeume.Where(r => r.Beheizt))
            {
                AbbildRaum l = gl.Raeume.Single(r => r.Name == q.Name);
                string n = q.Name + " ";
                G(q.FlaecheM2, l.FlaecheM2, n + "Fläche");
                G(q.VolumenM3, l.VolumenM3, n + "Volumen");
                G(q.SollHeizenC, l.SollHeizenC, n + "Heizsoll");
                G(q.SollKuehlenC, l.SollKuehlenC, n + "Kühlsoll");
                G(q.LuftwechselJeH, l.LuftwechselJeH, n + "Infiltration");
                G(q.LuftwechselNutzerJeH, l.LuftwechselNutzerJeH, n + "Nutzerluftwechsel");
                G(q.Personen, l.Personen, n + "Personen");
                G(q.FlaecheJePersonM2, l.FlaecheJePersonM2, n + "Fläche je Person");
                G(q.GeraeteWm2, l.GeraeteWm2, n + "Geräte");
                G(q.LichtWm2, l.LichtWm2, n + "Licht");
                Assert.NotNull(l.GbxmlLasten);
                Assert.True(l.GbxmlLasten.AusIfc);
                G(q.GbxmlLasten.WaermeJePersonW, l.GbxmlLasten.WaermeJePersonW, n + "Wärme je Person");
                G(q.GbxmlLasten.AussenluftJePersonM3h, l.GbxmlLasten.AussenluftJePersonM3h, n + "Außenluft je Person");
                G(q.LuftwechselNutzerJeH, l.GbxmlLasten.ZonenLuftwechselJeH, n + "Luftwechsel der Zone");
                Assert.Equal(q.ZonenName ?? q.Name, l.ZonenName);
                Assert.NotNull(l.ZonenKennung);
                felder += 2;
                // Die Lastfelder der Zone (Schemaschritt DA-2) über denselben Weg wie beim gbXML-Import.
                var zq = new ZoneModel();
                var zl = new ZoneModel();
                GbxmlKonditionierung.Lastfelder(zq, new[] { q }, q.FlaecheM2);
                GbxmlKonditionierung.Lastfelder(zl, new[] { l }, l.FlaecheM2);
                G(zq.Licht_W_m2, zl.Licht_W_m2, n + "Licht_W_m2");
                G(zq.Personen_Waerme_W, zl.Personen_Waerme_W, n + "Personen_Waerme_W");
                G(zq.Aussenluft_Person, zl.Aussenluft_Person, n + "Aussenluft_Person");
                Assert.Equal(q.Konditionierung?.Nutzung, l.Konditionierung?.Nutzung);
                G(q.Konditionierung?.HeizsollNachtC, l.Konditionierung?.HeizsollNachtC, n + "Heizsoll Nacht");
                // Die Kalender (EPOS_Kalender_*, F6 a) Stunde für Stunde.
                foreach (AbbildKalender k in q.Konditionierung?.Kalender ?? new List<AbbildKalender>())
                {
                    AbbildKalender z = l.Konditionierung.KalenderVon(k.Kalender.Groesse);
                    double[] a = k.Kalender.Auswerten(3, 0), b = z.Kalender.Auswerten(3, 0);
                    for (int h = 0; h < a.Length; h++) Assert.Equal(a[h], b[h], 9);
                    Assert.Equal(k.Kalender.Nennwert, z.Kalender.Nennwert);
                    felder += a.Length + 1;
                }
            }
            Assert.NotEqual(gl.Raeume.Single(r => r.Name == "Zone 1").ZonenKennung, gl.Raeume.Single(r => r.Name == "Zone 2").ZonenKennung);

            // Fenster und Bauteile (nach dem Namen), Schichten nach der Lage.
            foreach (AbbildBauteil q in gq.Bauteile.Concat(gq.Bauteile.SelectMany(b => b.Oeffnungen)).Where(b => b.Name != null))
            {
                AbbildBauteil l = gl.Bauteile.Concat(gl.Bauteile.SelectMany(b => b.Oeffnungen)).FirstOrDefault(b => b.Name == q.Name);
                if (l == null) continue;
                G(q.UWertWm2K, l.UWertWm2K, q.Name + " U");
                if (q.Art == Bauteilart.Fenster)
                {
                    G(q.GWert, l.GWert, q.Name + " g");
                    G(q.Rahmenanteil, l.Rahmenanteil, q.Name + " Rahmenanteil");
                }
                if (q.WaermebrueckeWK.HasValue) G(q.WaermebrueckeWK, l.WaermebrueckeWK, q.Name + " ψ·L");
                if (q.Aufbau == null || q.Aufbau.Schichten.Count == 0 || q.Aufbau.IstErsatz) continue;
                Assert.Equal(q.Aufbau.Schichten.Count, l.Aufbau.Schichten.Count);
                // Die Reihenfolge der Datei ist außen → innen (EPOS_Bauteil.Schichtrichtung); verglichen wird je Schicht.
                List<AbbildSchicht> lq = q.Aufbau.Schichten.OrderBy(x => x.DickeM).ThenBy(x => x.LambdaWmK).ToList();
                List<AbbildSchicht> ll = l.Aufbau.Schichten.OrderBy(x => x.DickeM).ThenBy(x => x.LambdaWmK).ToList();
                for (int i = 0; i < lq.Count; i++)
                {
                    AbbildSchicht sq = lq[i], sl = ll[i];
                    G(sq.DickeM, sl.DickeM, q.Name + " Dicke " + i);
                    G(sq.LambdaWmK, sl.LambdaWmK, q.Name + " λ " + i);
                    G(sq.RhoKgM3, sl.RhoKgM3, q.Name + " ρ " + i);
                    G(sq.CpJkgK, sl.CpJkgK, q.Name + " c " + i);
                    G(sq.RWertM2KW, sl.RWertM2KW, q.Name + " R " + i);
                }
            }
            Assert.DoesNotContain(gelesen.Meldungen, m => m.Schluessel.EndsWith("OPTIK_ABWEICHUNG", StringComparison.Ordinal));
            // 5 Kalender × (8760 Stunden + Nennwert) = 43 805, dazu 186 Einzelfelder (Lage, Gebäude, Räume, Bauteile, Schichten).
            Assert.Equal(43991, felder);
        }

        [Fact]
        public void Ohne_EPOS_Kalender_bildet_der_Zonenplan_die_Konditionierung_aus_den_Standardsaetzen()
        {
            (_, GebaeudeAbbild gelesen, _) = Rundlauf(mitKalender: false);
            AbbildRaum r = gelesen.Gebaeude[0].Raeume.Single(x => x.Name == "Zone 1");
            Assert.Null(r.Konditionierung);
            Zonenkonditionierung k = GbxmlKonditionierung.Bilden("Zone 1", new[] { r }, r.FlaecheM2, r.VolumenM3);
            Assert.NotNull(k);
            Assert.True(k.AusIfcStandard);
            Assert.False(k.AusGbxml);
            Groessenkonditionierung personen = k.Groesse(Konditionierungsgroesse.Personen);
            Assert.Equal(Konditionierungsherkunft.IfcDatei, personen.Herkunft);
            Assert.Equal(240.0, personen.NennwertDatei);
            // Nennwert ohne Zeitplan: der Zeitplan des Profils bleibt (wie DA-1).
            Assert.Contains(k.Meldungen, m => m.Schluessel.EndsWith("NENNWERT_OHNE_PLAN", StringComparison.Ordinal));
            Assert.Equal(2.0, k.Groesse(Konditionierungsgroesse.Lueftung).Vorgabe(DbWerte.KOND_ZEILE_TAG).Wert);
            Assert.Contains(gelesen.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_STANDARDSAETZE");
        }

        [Fact]
        public void Herstellereigener_Mindestluftwechsel_ist_nur_ein_genannter_Vorschlag_und_abweichende_Optik_wird_gemeldet()
        {
            (_, _, byte[] datei) = Rundlauf(mitKalender: false);
            string step = Encoding.UTF8.GetString(datei);
            Assert.Contains("'NaturalVentilationRate'", step, StringComparison.Ordinal);
            Assert.Contains("IFCNORMALISEDRATIOMEASURE(0.4)", step, StringComparison.Ordinal);
            step = step.Replace("'NaturalVentilationRate'", "'RatedMinAirExchangeRate'")
                       .Replace("IFCNORMALISEDRATIOMEASURE(0.4)", "IFCNORMALISEDRATIOMEASURE(0.2)");
            GebaeudeAbbild gelesen = IfcExportProbe.Lesen(Encoding.UTF8.GetBytes(step));
            AbbildRaum r = gelesen.Gebaeude[0].Raeume.Single(x => x.Name == "Zone 1");
            Assert.Null(r.LuftwechselNutzerJeH);
            Assert.Null(r.GbxmlLasten.ZonenLuftwechselJeH);
            Assert.Contains(gelesen.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_HERSTELLER_VORSCHLAG" && m.Werte[0] == "2"
                                                    && m.Werte[2] == "1" && m.Werte[3] == "2" && m.Werte[4] == "Zone 1, Zone 2");
            Assert.Contains(gelesen.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_OPTIK_ABWEICHUNG");
        }
    }
}
