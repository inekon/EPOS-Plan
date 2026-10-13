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
    /// <b>Rundlauf der Restdaten in beiden Formaten</b> (Welle DA-4, E138 F5): Vorheizzeit (B20), Höchstleistungen (B11), Übergabe
    /// und Kühlübergabe je Zone (B21, B22), Luftströme zwischen Zonen (G1), Erdreich (G3, G4), Zeilenbild (C8) und benannte Wochen
    /// (C9). Export über den eigenen Schreiber, Rücklesen mit dem eigenen Leser, Vergleich Feld für Feld — gbXML über den Block
    /// <c>EPOS:</c> der Beschreibung (Konzept 3.3, Weg 2), IFC über <c>EPOS_Zone</c>/<c>EPOS_Gebaeude</c>. Dazu die Gegenproben:
    /// ohne Restdaten kein Block, ein unlesbarer Wert benannt übergangen, ein fremder Partner eines Luftstroms benannt, der Import
    /// setzt die Felder der Zone. Alle Werte sind runde Phantasiewerte (synthetische Probe).
    /// </summary>
    public class RestdatenRundlaufTests
    {
        private const int ZONE1 = GbxmlExportProbe.ZONE, ZONE2 = 12;

        private static double[] Woche(double tag, double nacht)
            => Enumerable.Range(0, 168).Select(h => h % 24 < 6 ? double.NaN : h % 24 < 18 ? tag : nacht).ToArray();

        /// <summary>Das Probenabbild des IFC-Rundlaufs (zwei Zonen) mit Restdaten an beiden Zonen und Erdreich am Gebäude.</summary>
        internal static GebaeudeAbbild Abbild()
        {
            GebaeudeAbbild a = IfcExportRundlaufTests.Abbild();
            AbbildGebaeude g = a.Gebaeude[0];
            g.Erdreich = new AbbildErdreich { UmfangM = 40.0, UWirksam = 0.3, KellertemperaturC = 10.0 };
            AbbildRaum r1 = g.Raeume.Single(x => x.Kennung == GebaeudeExportKennung.Raum(ZONE1));
            AbbildRaum r2 = g.Raeume.Single(x => x.Kennung == GebaeudeExportKennung.Raum(ZONE2));
            r1.Rest = new AbbildZonenrest
            {
                Zone = GebaeudeExportKennung.Zone(ZONE1), VorheizzeitH = 3, HeizleistungMax = 5000.0, KuehlleistungMax = 3000.0,
                UebergabeArt = DbWerte.UEBERGABE_RADIATOR, UebergabeExponent = 1.3, UebergabeLeistungNenn = 6000.0, AuslegungVorlauf = 70.0,
                AuslegungRuecklauf = 50.0, AuslegungRaumtemperatur = 20.0, ReglerProportionalband = 2.0,
                KuehlUebergabeArt = Waermeuebergabevorgaben.KuehlArten[0], KuehlUebergabeExponent = 1.1, KuehlUebergabeLeistungNenn = 2500.0,
            };
            r1.Rest.Luftstroeme.Add(new AbbildLuftstrom(GebaeudeExportKennung.Zone(ZONE2), 100.0));
            r1.Rest.Zeilenbild.Add(new Vorgabezeile { Groesse = DbWerte.KOND_GROESSE_HEIZSOLL, Zeile = DbWerte.KOND_ZEILE_NACHT, Wert = 16.0, Von = 22, Bis = 6 });
            r1.Rest.Zeilenbild.Add(new Vorgabezeile { Groesse = DbWerte.KOND_GROESSE_LUEFTUNG, Zeile = DbWerte.KOND_ZEILE_WOCHENENDE, Aus = true });
            r1.Rest.Zeilenbild.Add(new Vorgabezeile { Groesse = DbWerte.KOND_GROESSE_KUEHLSOLL, Zeile = DbWerte.KOND_ZEILE_TAG, Wert = 26.0, BedingtK = 2.0 });
            r1.Rest.Wochen.Add(new BenannteWoche(0, Konditionierungsgroesse.Heizsoll, "Messewoche", Woche(21.0, 17.0)));
            r1.Rest.Wochen.Add(new BenannteWoche(0, Konditionierungsgroesse.Lueftung, "Umbau", Woche(1.5, 0.5)));
            r2.Rest = new AbbildZonenrest { Zone = GebaeudeExportKennung.Zone(ZONE2), VorheizzeitH = 0, HeizleistungMax = 2000.0 };
            return a;
        }

        private static void Gleich(AbbildZonenrest q, AbbildZonenrest l, string wo, ref int felder)
        {
            Assert.NotNull(l);
            void Z(double? a, double? b, string was)
            {
                Assert.True(a.HasValue == b.HasValue && (!a.HasValue || Math.Abs(a.Value - b.Value) <= 1e-9), wo + " " + was + ": " + a + " ≠ " + b);
            }
            Assert.Equal(q.Zone, l.Zone);
            Assert.Equal(q.VorheizzeitH, l.VorheizzeitH);
            Z(q.HeizleistungMax, l.HeizleistungMax, "Heizleistung_Max");
            Z(q.KuehlleistungMax, l.KuehlleistungMax, "Kuehlleistung_Max");
            Assert.Equal(q.UebergabeArt, l.UebergabeArt);
            Z(q.UebergabeExponent, l.UebergabeExponent, "Uebergabe_Exponent");
            Z(q.UebergabeLeistungNenn, l.UebergabeLeistungNenn, "Uebergabe_Leistung_Nenn");
            Z(q.AuslegungVorlauf, l.AuslegungVorlauf, "Auslegung_Vorlauf");
            Z(q.AuslegungRuecklauf, l.AuslegungRuecklauf, "Auslegung_Ruecklauf");
            Z(q.AuslegungRaumtemperatur, l.AuslegungRaumtemperatur, "Auslegung_Raumtemperatur");
            Z(q.ReglerProportionalband, l.ReglerProportionalband, "Regler_Proportionalband");
            Assert.Equal(q.KuehlUebergabeArt, l.KuehlUebergabeArt);
            Z(q.KuehlUebergabeExponent, l.KuehlUebergabeExponent, "Kuehl_Uebergabe_Exponent");
            Z(q.KuehlUebergabeLeistungNenn, l.KuehlUebergabeLeistungNenn, "Kuehl_Uebergabe_Leistung_Nenn");
            felder += 14;
            Assert.Equal(q.Luftstroeme, l.Luftstroeme);
            felder += 2 * q.Luftstroeme.Count;
            Assert.Equal(q.Zeilenbild.Count, l.Zeilenbild.Count);
            for (int i = 0; i < q.Zeilenbild.Count; i++)
            {
                Vorgabezeile a = q.Zeilenbild[i], b = l.Zeilenbild[i];
                Assert.Equal((a.Groesse, a.Zeile, a.Wert, a.Aus, a.Von, a.Bis, a.BedingtK), (b.Groesse, b.Zeile, b.Wert, b.Aus, b.Von, b.Bis, b.BedingtK));
                felder += 7;
            }
            Assert.Equal(q.Wochen.Count, l.Wochen.Count);
            for (int i = 0; i < q.Wochen.Count; i++)
            {
                Assert.Equal(q.Wochen[i].Groesse, l.Wochen[i].Groesse);
                Assert.Equal(q.Wochen[i].Name, l.Wochen[i].Name);
                Assert.True(q.Wochen[i].Werte.Zip(l.Wochen[i].Werte, (x, y) => double.IsNaN(x) ? double.IsNaN(y) : x == y).All(x => x), wo + " Woche " + i);
                felder += 2 + q.Wochen[i].Werte.Length;
            }
        }

        private static int Vergleichen(GebaeudeAbbild quelle, GebaeudeAbbild gelesen, Func<AbbildRaum, AbbildRaum> partner)
        {
            int felder = 0;
            AbbildGebaeude gq = quelle.Gebaeude[0], gl = Assert.Single(gelesen.Gebaeude);
            Assert.NotNull(gl.Erdreich);
            Assert.Equal(gq.Erdreich.UmfangM, gl.Erdreich.UmfangM);
            Assert.Equal(gq.Erdreich.UWirksam, gl.Erdreich.UWirksam);
            Assert.Equal(gq.Erdreich.KellertemperaturC, gl.Erdreich.KellertemperaturC);
            felder += 3;
            foreach (AbbildRaum q in gq.Raeume.Where(r => r.Rest != null))
                Gleich(q.Rest, partner(q).Rest, q.Name, ref felder);
            Assert.DoesNotContain(gelesen.Meldungen.Concat(gl.Meldungen), m => m.Schluessel.EndsWith("RESTDATEN_UEBERSPRUNGEN", StringComparison.Ordinal));
            return felder;
        }

        [Fact]
        public void Gbxml_Export_und_Import_geben_alle_Restdaten_ohne_Verlust_zurueck()
        {
            GebaeudeAbbild quelle = Abbild();
            byte[] datei = GbxmlExportProbe.Schreiben(quelle);
            string xml = Encoding.UTF8.GetString(datei);
            Assert.Contains(GebaeudeRestdaten.BLOCK, xml, StringComparison.Ordinal);
            Assert.Equal(datei, GbxmlExportProbe.Schreiben(Abbild()));
            GbxmlAbbild gelesen = GbxmlExportProbe.Lesen(datei);
            int felder = Vergleichen(quelle, gelesen, q => gelesen.Gebaeude[0].Raeume.Single(r => r.Kennung == q.Kennung));
            // Erdreich 3; Zone 1: 14 Felder, 1 Luftstrom (2), 3 Zeilen (21), 2 Wochen (2 × 170); Zone 2: 14 Felder.
            Assert.Equal(3 + 14 + 2 + 21 + 340 + 14, felder);
        }

        [Fact]
        public void Ifc_Export_und_Import_geben_alle_Restdaten_ohne_Verlust_zurueck()
        {
            GebaeudeAbbild quelle = Abbild();
            byte[] datei = IfcExportProbe.Schreiben(quelle);
            string step = Encoding.UTF8.GetString(datei);
            foreach (string s in new[] { "'Vorheizzeit_H'", "'Uebergabe_Art'", "'Luftstrom_1'", "'Zeile_HEIZSOLL_NACHT'", "'Woche_1'", "'Erdreich_Umfang'" })
                Assert.Contains(s, step, StringComparison.Ordinal);
            Assert.Equal(datei, IfcExportProbe.Schreiben(Abbild()));
            GebaeudeAbbild gelesen = IfcExportProbe.Lesen(datei);
            int felder = Vergleichen(quelle, gelesen, q => gelesen.Gebaeude[0].Raeume.Single(r => r.Name == q.Name));
            Assert.Equal(3 + 14 + 2 + 21 + 340 + 14, felder);
        }

        [Fact]
        public void Ohne_Restdaten_schreibt_keiner_der_Schreiber_etwas_Neues()
        {
            string xml = Encoding.UTF8.GetString(GbxmlExportProbe.Schreiben(IfcExportRundlaufTests.Abbild()));
            Assert.DoesNotContain(GebaeudeRestdaten.BLOCK, xml, StringComparison.Ordinal);
            string step = Encoding.UTF8.GetString(IfcExportProbe.Schreiben(IfcExportRundlaufTests.Abbild()));
            foreach (string s in new[] { "'Vorheizzeit_H'", "'Erdreich_Umfang'", "'Luftstrom_1'", "'Zeile_" })
                Assert.DoesNotContain(s, step, StringComparison.Ordinal);
            GbxmlAbbild gelesen = GbxmlExportProbe.Lesen(GbxmlExportProbe.Schreiben(IfcExportRundlaufTests.Abbild()));
            Assert.All(gelesen.Gebaeude[0].Raeume, r => Assert.Null(r.Rest));
            Assert.Null(gelesen.Gebaeude[0].Erdreich);
        }

        [Fact]
        public void Ein_unlesbarer_Wert_wird_benannt_uebergangen_die_uebrigen_bleiben()
        {
            string xml = Encoding.UTF8.GetString(GbxmlExportProbe.Schreiben(Abbild()))
                .Replace("Vorheizzeit_H=3", "Vorheizzeit_H=99", StringComparison.Ordinal)
                .Replace("Uebergabe_Art=" + DbWerte.UEBERGABE_RADIATOR, "Uebergabe_Art=KAMIN", StringComparison.Ordinal)
                .Replace("Kellertemperatur=10", "Kellertemperatur=warm", StringComparison.Ordinal);
            GbxmlAbbild gelesen = GbxmlExportProbe.Lesen(Encoding.UTF8.GetBytes(xml));
            AbbildGebaeude g = gelesen.Gebaeude[0];
            AbbildZonenrest r = g.Raeume.Single(x => x.Kennung == GebaeudeExportKennung.Raum(ZONE1)).Rest;
            Assert.Null(r.VorheizzeitH);
            Assert.Null(r.UebergabeArt);
            Assert.Equal(5000.0, r.HeizleistungMax);
            Assert.Null(g.Erdreich.KellertemperaturC);
            Assert.Equal(40.0, g.Erdreich.UmfangM);
            List<PruefMeldung> m = g.Meldungen.Where(x => x.Schluessel == GbxmlImportProfil.MELDUNGSPRAEFIX + "RESTDATEN_UEBERSPRUNGEN").ToList();
            Assert.Equal(2, m.Count);
            Assert.Contains(m, x => x.Werte.Last().Contains(GebaeudeRestdaten.VORHEIZZEIT) && x.Werte.Last().Contains(GebaeudeRestdaten.UEBERGABE_ART));
            Assert.Contains(m, x => x.Werte.Last() == GebaeudeRestdaten.KELLERTEMPERATUR);
        }

        [Fact]
        public void Der_Import_setzt_die_Zonenfelder_und_bildet_die_Luftstroeme_ueber_die_Zonenkennung()
        {
            GebaeudeAbbild a = Abbild();
            AbbildRaum r1 = a.Gebaeude[0].Raeume.Single(x => x.Kennung == GebaeudeExportKennung.Raum(ZONE1));
            AbbildRaum r2 = a.Gebaeude[0].Raeume.Single(x => x.Kennung == GebaeudeExportKennung.Raum(ZONE2));
            var z = new ZoneModel();
            GebaeudeRestdaten.Setzen(z, new[] { r1 });
            Assert.Equal((3, 5000.0, 3000.0, DbWerte.UEBERGABE_RADIATOR, 1.3, 6000.0, 70.0, 50.0, 20.0, 2.0),
                         (z.Vorheizzeit_H, z.Heizleistung_Max, z.Kuehlleistung_Max, z.Uebergabe_Art, z.Uebergabe_Exponent, z.Uebergabe_Leistung_Nenn,
                          z.Auslegung_Vorlauf, z.Auslegung_Ruecklauf, z.Auslegung_Raumtemperatur, z.Regler_Proportionalband));
            Assert.Equal((Waermeuebergabevorgaben.KuehlArten[0], 1.1, 2500.0), (z.Kuehl_Uebergabe_Art, z.Kuehl_Uebergabe_Exponent, z.Kuehl_Uebergabe_Leistung_Nenn));

            var meldungen = new List<PruefMeldung>();
            List<ZonenluftstromModel> l = GebaeudeRestdaten.Luftstroeme(new[] { -1, -2 }, new[] { r1.Rest, r2.Rest }, meldungen);
            ZonenluftstromModel s = Assert.Single(l);
            Assert.Equal((-1, -2, 100.0), (s.ID_ZoneA, s.ID_ZoneB, s.Volumenstrom));
            Assert.Empty(meldungen);
            // Gegenprobe: ein Partner, den die Datei nicht als Zone führt, und die Zone selbst — benannt übergangen.
            r2.Rest.Luftstroeme.Add(new AbbildLuftstrom("Zone-fremd", 50.0));
            r2.Rest.Luftstroeme.Add(new AbbildLuftstrom(r2.Rest.Zone, 50.0));
            r2.Rest.Luftstroeme.Add(new AbbildLuftstrom(r1.Rest.Zone, 70.0));
            l = GebaeudeRestdaten.Luftstroeme(new[] { -1, -2 }, new[] { r1.Rest, r2.Rest }, meldungen);
            Assert.Single(l);
            Assert.Equal(3, meldungen.Count(m => m.Schluessel == GebaeudeBauteilvorschlag.LUFTSTROM_PARTNER));
        }

        [Fact]
        public void Die_Verlustliste_fuehrt_die_Restdaten_als_rundlaeufig_und_nennt_sie_nicht_mehr()
        {
            foreach (string f in new[] { "Vorheizzeit_H", "Heizleistung_Max", "Kuehlleistung_Max", "Uebergabe_Art", "Uebergabe_Exponent",
                                         "Uebergabe_Leistung_Nenn", "Auslegung_Vorlauf", "Auslegung_Ruecklauf", "Auslegung_Raumtemperatur",
                                         "Regler_Proportionalband", "Kuehl_Uebergabe_Art", "Kuehl_Uebergabe_Exponent", "Kuehl_Uebergabe_Leistung_Nenn" })
                Assert.Equal(Exporteinstufung.Rundlaeufig, GebaeudeExportVerluste.Zone[f]);
            foreach (string f in new[] { "Abmessung_Anschluß_Außenwand_Kellerdecke", "Erdreich_U_Wirksam", "Kellertemperatur" })
                Assert.Equal(Exporteinstufung.Rundlaeufig, GebaeudeExportVerluste.Gebaeude[f]);
            var z = new ZoneModel { Vorheizzeit_H = 2, Heizleistung_Max = 1000.0, Kuehlleistung_Max = 1000.0, Uebergabe_Art = DbWerte.UEBERGABE_RADIATOR,
                                    Kuehl_Uebergabe_Art = Waermeuebergabevorgaben.KuehlArten[0] };
            IReadOnlyList<string> getragen = GebaeudeExportVerluste.Getragen(new ProjektGebaeudeModel { Erdreich_U_Wirksam = 0.3 }, new[] { z });
            foreach (string f in new[] { "Vorheizzeit_H", "Heizleistung_Max", "Kuehlleistung_Max", "Uebergabe_Art", "Kuehl_Uebergabe_Art", "Erdreich_U_Wirksam" })
                Assert.DoesNotContain(f, getragen);
            // Gegenprobe: was weiter kein Format trägt, bleibt benannt.
            Assert.Equal(Exporteinstufung.BenannterVerlust, GebaeudeExportVerluste.Zone["Maximaleraumtemperatur"]);
            Assert.Contains("Maximaleraumtemperatur", getragen);
        }
    }
}
