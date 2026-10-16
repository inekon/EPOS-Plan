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
    public class RestdatenRundlaufTests : IDisposable
    {
        private readonly TestDatenbank _db = new TestDatenbank();
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();

        public void Dispose()
        {
            _kultur.Dispose();
            _db.Dispose();
        }

        private const int ZONE1 = GbxmlExportProbe.ZONE, ZONE2 = 12;

        private static double[] Woche(double tag, double nacht)
            => Enumerable.Range(0, 168).Select(h => h % 24 < 6 ? double.NaN : h % 24 < 18 ? tag : nacht).ToArray();

        /// <summary>Das Probenabbild des IFC-Rundlaufs (zwei Zonen) mit Restdaten an beiden Zonen und Erdreich am Gebäude.</summary>
        internal static GebaeudeAbbild Abbild()
        {
            GebaeudeAbbild a = IfcExportRundlaufTests.Abbild();
            AbbildGebaeude g = a.Gebaeude[0];
            g.Erdreich = new AbbildErdreich { UmfangM = 40.0, UWirksam = 0.3, KellertemperaturC = 10.0, MaximaleraumtemperaturC = 28.0 };
            AbbildRaum r1 = g.Raeume.Single(x => x.Kennung == GebaeudeExportKennung.Raum(ZONE1));
            AbbildRaum r2 = g.Raeume.Single(x => x.Kennung == GebaeudeExportKennung.Raum(ZONE2));
            r1.Rest = new AbbildZonenrest
            {
                Zone = GebaeudeExportKennung.Zone(ZONE1), VorheizzeitH = 3, HeizleistungMax = 5000.0, KuehlleistungMax = 3000.0,
                UebergabeArt = DbWerte.UEBERGABE_RADIATOR, UebergabeExponent = 1.3, UebergabeLeistungNenn = 6000.0, AuslegungVorlauf = 70.0,
                AuslegungRuecklauf = 50.0, AuslegungRaumtemperatur = 20.0, ReglerProportionalband = 2.0,
                KuehlUebergabeArt = Waermeuebergabevorgaben.KuehlArten[0], KuehlUebergabeExponent = 1.1, KuehlUebergabeLeistungNenn = 2500.0,
                Maximaleraumtemperatur = 26.0,
            };
            r1.Rest.Luftstroeme.Add(new AbbildLuftstrom(GebaeudeExportKennung.Zone(ZONE2), 100.0));
            r1.Rest.Zeilenbild.Add(new Vorgabezeile { Groesse = DbWerte.KOND_GROESSE_HEIZSOLL, Zeile = DbWerte.KOND_ZEILE_NACHT, Wert = 16.0, Von = 22, Bis = 6 });
            r1.Rest.Zeilenbild.Add(new Vorgabezeile { Groesse = DbWerte.KOND_GROESSE_LUEFTUNG, Zeile = DbWerte.KOND_ZEILE_WOCHENENDE, Aus = true });
            r1.Rest.Zeilenbild.Add(new Vorgabezeile { Groesse = DbWerte.KOND_GROESSE_KUEHLSOLL, Zeile = DbWerte.KOND_ZEILE_TAG, Wert = 26.0 });
            r1.Rest.Zeilenbild.Add(new Vorgabezeile { Groesse = DbWerte.KOND_GROESSE_LUEFTUNG, Zeile = DbWerte.KOND_ZEILE_NACHT, Wert = 0.5, BedingtK = 2.0 });
            r1.Rest.Wochen.Add(new BenannteWoche(0, Konditionierungsgroesse.Heizsoll, "Messewoche", Woche(21.0, 17.0)));
            r1.Rest.Wochen.Add(new BenannteWoche(0, Konditionierungsgroesse.Lueftung, "Umbau", Woche(1.5, 0.5)));
            // Nachzug N2: eine Periode verweist auf die benannte Woche (der Bezeichner darf das Trennzeichen tragen).
            r1.Rest.Periodenwochen.Add(new AbbildPeriodenwoche(Konditionierungsgroesse.Heizsoll, 100, 110, "Messe; Halle 1", "Messewoche"));
            // Nachzug N4: eine Feiertagsperiode verweist über ihre Regel auf dieselbe Woche.
            r1.Rest.Periodenwochen.Add(new AbbildPeriodenwoche(Konditionierungsgroesse.Heizsoll, 0, 0, "Neujahr; Betriebsruhe", "Messewoche",
                                                               DbWerte.KOND_FEIERTAG_NEUJAHR));
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
            Z(q.Maximaleraumtemperatur, l.Maximaleraumtemperatur, "Maximaleraumtemperatur");
            felder += 15;
            Assert.Equal(q.Periodenwochen, l.Periodenwochen);
            felder += 5 * q.Periodenwochen.Count;
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
            Assert.Equal(gq.Erdreich.MaximaleraumtemperaturC, gl.Erdreich.MaximaleraumtemperaturC);
            felder += 4;
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
            // Gebäude 4 (Erdreich 3, Höchsttemperatur 1); Zone 1: 15 Felder, 1 Luftstrom (2), 4 Zeilen (28), 2 Wochen (2 × 170),
            // 2 Periodenverweise (Zeitraum und Feiertag, 2 × 5); Zone 2: 15 Felder.
            Assert.Equal(4 + 15 + 2 + 28 + 340 + 10 + 15, felder);
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
            Assert.Equal(4 + 15 + 2 + 28 + 340 + 10 + 15, felder);
        }

        [Fact]
        public void Ifc_Rundlauf_traegt_DIN_Nummer_der_DA_5_und_Restdaten_der_DA_4_zusammen_in_EPOS_Zone()
        {
            // Beide Standardsätze in einem Satz EPOS_Zone: die DIN-Nummer (Kennwort, DA-5) neben den Textwerten der Restdaten (DA-4).
            GebaeudeAbbild quelle = Abbild();
            AbbildRaum r1 = quelle.Gebaeude[0].Raeume.Single(x => x.Kennung == GebaeudeExportKennung.Raum(ZONE1));
            r1.Konditionierung ??= new AbbildKonditionierung();
            r1.Konditionierung.DinNummer = 7;
            byte[] datei = IfcExportProbe.Schreiben(quelle);
            string step = Encoding.UTF8.GetString(datei);
            foreach (string s in new[] { "'" + IfcKonditionierungssatz.DIN_NUMMER + "'", "'Vorheizzeit_H'", "'Luftstrom_1'", "'Woche_1'" })
                Assert.Contains(s, step, StringComparison.Ordinal);
            GebaeudeAbbild gelesen = IfcExportProbe.Lesen(datei);
            AbbildRaum l1 = gelesen.Gebaeude[0].Raeume.Single(r => r.Name == r1.Name);
            Assert.Equal(7, l1.Konditionierung?.DinNummer);
            // Die Restdaten kommen vollständig und ohne übergangenen Wert zurück — die DIN-Nummer stört den Leser der Restdaten nicht.
            int felder = Vergleichen(quelle, gelesen, q => gelesen.Gebaeude[0].Raeume.Single(r => r.Name == q.Name));
            Assert.Equal(4 + 15 + 2 + 28 + 340 + 10 + 15, felder);
            // Gegenprobe: ohne Restdaten bleibt die DIN-Nummer allein und kommt ebenso zurück.
            GebaeudeAbbild ohne = IfcExportRundlaufTests.Abbild();
            AbbildRaum o1 = ohne.Gebaeude[0].Raeume.Single(x => x.Kennung == GebaeudeExportKennung.Raum(ZONE1));
            o1.Konditionierung ??= new AbbildKonditionierung();
            o1.Konditionierung.DinNummer = 7;
            GebaeudeAbbild ohneGelesen = IfcExportProbe.Lesen(IfcExportProbe.Schreiben(ohne));
            AbbildRaum ol = ohneGelesen.Gebaeude[0].Raeume.Single(r => r.Name == o1.Name);
            Assert.Equal(7, ol.Konditionierung?.DinNummer);
            Assert.Null(ol.Rest);
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
            // Nachzug N2: die Höchsttemperatur der Zone reist; Gegenprobe: was weiter kein Format trägt, bleibt benannt.
            Assert.Equal(Exporteinstufung.Rundlaeufig, GebaeudeExportVerluste.Zone["Maximaleraumtemperatur"]);
            Assert.Equal(Exporteinstufung.BenannterVerlust, GebaeudeExportVerluste.Zone["Heizung_Strahlungsanteil"]);
            Assert.Contains("Raumsolltemperatur_Wochenende", getragen);
        }

        [Fact]
        public void Der_Mehrzonenvorschlag_schreibt_Zonenfelder_Luftstrom_Zeilenbild_und_Wochen_in_die_Datenbank()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(1045);
            ProjektGebaeudeModel g = ctrl.items[0];
            Assert.True(g.Zonen == null || g.Zonen.Count == 0, "Das Probengebäude trägt schon eine Zone.");
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen("ifc4_haus.ifc");
            GebaeudeAbbild a = Abbild();
            AbbildZonenrest quelle = a.Gebaeude[0].Raeume.Single(x => x.Kennung == GebaeudeExportKennung.Raum(ZONE1)).Rest;
            // Die Restdaten an die Räume des Erd- und Obergeschosses; das Erdgeschoss trägt den Luftstrom zum Obergeschoss.
            var eg = new AbbildZonenrest
            {
                Zone = "EG", VorheizzeitH = quelle.VorheizzeitH, HeizleistungMax = quelle.HeizleistungMax, UebergabeArt = quelle.UebergabeArt,
                UebergabeExponent = quelle.UebergabeExponent, KuehlUebergabeArt = quelle.KuehlUebergabeArt,
            };
            eg.Luftstroeme.Add(new AbbildLuftstrom("OG", 100.0));
            eg.Zeilenbild.AddRange(quelle.Zeilenbild);
            eg.Wochen.AddRange(quelle.Wochen);
            var og = new AbbildZonenrest { Zone = "OG", KuehlleistungMax = 3000.0 };
            foreach (AbbildRaum r in ablauf.Abbild.Gebaeude[0].Raeume.Where(r => r.Beheizt))
                r.Rest = r.Name == "Schlafen" || r.Name == "Bad" ? og : eg;
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(ablauf, 0, null);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            Assert.Single(v.Luftstroeme);
            Assert.DoesNotContain(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.LUFTSTROM_PARTNER);
            GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v);
            Assert.True(e.Ok, e.Meldung);

            List<ZoneModel> zonen = new GebaeudeZonenCtrl().LesenJeGebaeude(g.ID_Gebaeude).ToList();
            ZoneModel zeg = zonen.Single(z => z.Bezeichner == "Erdgeschoss"), zog = zonen.Single(z => z.Bezeichner == "Obergeschoss");
            Assert.Equal((3, 5000.0, DbWerte.UEBERGABE_RADIATOR, 1.3, Waermeuebergabevorgaben.KuehlArten[0]),
                         (zeg.Vorheizzeit_H, zeg.Heizleistung_Max, zeg.Uebergabe_Art, zeg.Uebergabe_Exponent, zeg.Kuehl_Uebergabe_Art));
            Assert.Equal(3000.0, zog.Kuehlleistung_Max);
            ZonenluftstromModel l = Assert.Single(new GebaeudeZonenCtrl().LuftstroemeJeGebaeude(g.ID_Gebaeude));
            Assert.Equal((Math.Min(zeg.ID, zog.ID), Math.Max(zeg.ID, zog.ID), 100.0), (l.ID_ZoneA, l.ID_ZoneB, l.Volumenstrom));

            Konditionierungsstand stand = new KonditionierungCtrl().StandLesen(KonditionierungCtrl.Eigner.Zone(g.ID_Gebaeude, zeg.ID), out string meldung);
            Assert.NotNull(stand);
            foreach (Vorgabezeile q in quelle.Zeilenbild)
            {
                Assert.True(Konditionierungsgroessen.AusKennwort(q.Groesse, out Konditionierungsgroesse gr));
                Matrixzelle c = stand.Vorgabe(gr, q.Zeile);
                // Sollwerte der Zeilen stehen teils in den Bestandsspalten der Zone (etwa Raumsolltemperatur_Nachtabsenkung,
                // Kuehl_Sollwert): der Wert wird an der gebildeten Matrix gemessen, Zeiten, ΔT und „aus“ an der Zelle.
                Matrixzelle m = stand.Matrix().Spalte(gr).Zeile(q.Zeile);
                double? ist = m.Belegt && !m.Aus ? m.Wert : null;
                Assert.Equal((q.Aus, q.Wert, q.Von, q.Bis, q.BedingtK), (c.Aus, ist, c.Von, c.Bis, c.BedingtK));
            }
            foreach (BenannteWoche q in quelle.Wochen)
            {
                BenannteWoche w = Assert.Single(stand.Wochen, x => x.Name == q.Name && x.Groesse == q.Groesse);
                Assert.True(w.Id > 0);
                Assert.True(q.Werte.Zip(w.Werte, (x, y) => double.IsNaN(x) ? double.IsNaN(y) : x == y).All(x => x));
            }
        }

        /// <summary>
        /// Nachzug N2 (DA-4): Eine Periode, die in der Datei auf eine benannte Woche verweist, verweist nach dem Import wieder auf sie
        /// (<c>ID_Woche</c>) und wird ohne Verlust wieder exportiert; die Gegenprobe — eine Periode ohne Verweis — bleibt eine
        /// eingebettete Woche. Die Höchsttemperatur der Zone (B10) kommt an die Zonenzeile.
        /// </summary>
        [Fact]
        public void Eine_Periode_mit_Verweis_auf_eine_benannte_Woche_verweist_nach_dem_Import_wieder_auf_sie()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(1045);
            ProjektGebaeudeModel g = ctrl.items[0];
            Assert.True(g.Zonen == null || g.Zonen.Count == 0, "Das Probengebäude trägt schon eine Zone.");
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen("ifc4_haus.ifc");
            double[] messe = Woche(21.0, 17.0), revision = Woche(19.0, 15.0);
            var kalender = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(20.0), null, new[]
            {
                Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN, DbWerte.KOND_ART_ZEITRAUM, "Messe", 100, 110, Kalenderangabe.AusWoche(messe)),
                Kalenderregel.Zeitraum(Standardfahrplan.RANG_EIGEN + 1, DbWerte.KOND_ART_ZEITRAUM, "Revision", 200, 210, Kalenderangabe.AusWoche(revision)),
            });
            var k = new AbbildKonditionierung();
            k.Kalender.Add(new AbbildKalender(kalender, null));
            var rest = new AbbildZonenrest { Zone = "EG", Maximaleraumtemperatur = 27.0 };
            rest.Wochen.Add(new BenannteWoche(0, Konditionierungsgroesse.Heizsoll, "Messewoche", messe));
            rest.Periodenwochen.Add(new AbbildPeriodenwoche(Konditionierungsgroesse.Heizsoll, 100, 110, "Messe", "Messewoche"));
            foreach (AbbildRaum r in ablauf.Abbild.Gebaeude[0].Raeume.Where(r => r.Beheizt))
            {
                r.Konditionierung = k;
                r.Rest = rest;
            }
            // Wie der Importdialog: über den Zonenplan, der die Konditionierung der Datei je Zone mitnimmt.
            GebaeudeZonierung zonierung = Zonenplan.Vorschlag(ablauf.Abbild, 0).Zonieren();
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null, zonierung: zonierung);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v);
            Assert.True(e.Ok, e.Meldung);

            ZoneModel z = new GebaeudeZonenCtrl().LesenJeGebaeude(g.ID_Gebaeude).Single(x => x.Bezeichner == "Erdgeschoss");
            Assert.Equal(27.0, z.Maximaleraumtemperatur);
            Konditionierungsstand stand = new KonditionierungCtrl().StandLesen(KonditionierungCtrl.Eigner.Zone(g.ID_Gebaeude, z.ID), out string meldung);
            Assert.True(stand != null, meldung);
            BenannteWoche w = Assert.Single(stand.Wochen, x => x.Name == "Messewoche");
            Assert.True(w.Id > 0);
            Kalenderregel pm = Assert.Single(stand.Kalender(Konditionierungsgroesse.Heizsoll).Perioden, p => p.Bezeichner == "Messe");
            Kalenderregel pr = Assert.Single(stand.Kalender(Konditionierungsgroesse.Heizsoll).Perioden, p => p.Bezeichner == "Revision");
            Assert.Equal(w.Id, pm.Angabe.IdWoche);
            Assert.Equal(messe, pm.Angabe.Woche.ToArray());
            // Gegenprobe: ohne Verweis bleibt die Woche eingebettet.
            Assert.Null(pr.Angabe.IdWoche);
            Assert.Equal(Angabeart.Woche, pr.Angabe.Art);
            Assert.Equal(revision, pr.Angabe.Woche.ToArray());

            // Wieder exportiert: genau der eine Verweis, über Nummer und Namen der Woche.
            AbbildZonenrest aus = GebaeudeRestdaten.AusZone(z, "EG", null, stand);
            AbbildPeriodenwoche p1 = Assert.Single(aus.Periodenwochen);
            Assert.Equal(new AbbildPeriodenwoche(Konditionierungsgroesse.Heizsoll, 100, 110, "Messe", "Messewoche"), p1);
            Assert.Contains(GebaeudeRestdaten.Werte(aus), x => x.Schluessel == GebaeudeRestdaten.PERIODENWOCHE + "1");
            Assert.Contains(GebaeudeRestdaten.Werte(aus), x => x.Schluessel == GebaeudeRestdaten.MAXIMALERAUMTEMPERATUR && x.Wert == "27");
        }

        /// <summary>
        /// Nachzug N4: Eine <b>Feiertagsperiode</b>, die auf eine benannte Woche verweist, verweist nach dem Import wieder auf sie
        /// (<c>ID_Woche</c>) und wird mit ihrer Feiertagsregel wieder exportiert. Der Verweis findet die Periode über die Regel — auch
        /// mit anderem Bezeichner (gbXML benennt die Feiertage beim Import nach der Saat); die Gegenprobe, ein Feiertag ohne Verweis,
        /// bleibt eingebettet.
        /// </summary>
        [Fact]
        public void Eine_Feiertagsperiode_mit_Verweis_auf_eine_benannte_Woche_verweist_nach_dem_Import_wieder_auf_sie()
        {
            if (!_db.Vorhanden) return;
            var ctrl = new ProjektGebaeudeCtrl();
            ctrl.ReadAll(1045);
            ProjektGebaeudeModel g = ctrl.items[0];
            Assert.True(g.Zonen == null || g.Zonen.Count == 0, "Das Probengebäude trägt schon eine Zone.");
            GebaeudeImportAblauf ablauf = BauteilvorschlagProbe.Lesen("ifc4_haus.ifc");
            double[] ruhe = Woche(16.0, 15.0), mai = Woche(18.0, 16.0);
            var kalender = new Konditionierungskalender(Konditionierungsgroesse.Heizsoll, Kalenderangabe.AusWert(20.0), null, new[]
            {
                Kalenderregel.Feiertag(Standardfahrplan.RANG_FEIERTAG, "Neujahr", DbWerte.KOND_FEIERTAG_NEUJAHR, Kalenderangabe.AusWoche(ruhe)),
                Kalenderregel.Feiertag(Standardfahrplan.RANG_FEIERTAG + 1, "Tag der Arbeit", DbWerte.KOND_FEIERTAG_ERSTER_MAI, Kalenderangabe.AusWoche(mai)),
            });
            var k = new AbbildKonditionierung();
            k.Kalender.Add(new AbbildKalender(kalender, null));
            var rest = new AbbildZonenrest { Zone = "EG" };
            rest.Wochen.Add(new BenannteWoche(0, Konditionierungsgroesse.Heizsoll, "Betriebsruhe", ruhe));
            // Der Bezeichner der Datei weicht ab: gefunden wird die Periode über die Feiertagsregel.
            rest.Periodenwochen.Add(new AbbildPeriodenwoche(Konditionierungsgroesse.Heizsoll, 0, 0, "Neujahrstag", "Betriebsruhe",
                                                            DbWerte.KOND_FEIERTAG_NEUJAHR));
            foreach (AbbildRaum r in ablauf.Abbild.Gebaeude[0].Raeume.Where(r => r.Beheizt))
            {
                r.Konditionierung = k;
                r.Rest = rest;
            }
            GebaeudeZonierung zonierung = Zonenplan.Vorschlag(ablauf.Abbild, 0).Zonieren();
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(ablauf, 0, null, zonierung: zonierung);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            GebaeudeZonenCtrl.Vorschlagsergebnis e = new GebaeudeZonenCtrl().VorschlagSchreiben(g.ID_Gebaeude, v);
            Assert.True(e.Ok, e.Meldung);

            ZoneModel z = new GebaeudeZonenCtrl().LesenJeGebaeude(g.ID_Gebaeude).Single(x => x.Bezeichner == "Erdgeschoss");
            Konditionierungsstand stand = new KonditionierungCtrl().StandLesen(KonditionierungCtrl.Eigner.Zone(g.ID_Gebaeude, z.ID), out string meldung);
            Assert.True(stand != null, meldung);
            BenannteWoche w = Assert.Single(stand.Wochen, x => x.Name == "Betriebsruhe");
            Assert.True(w.Id > 0);
            Kalenderregel pn = Assert.Single(stand.Kalender(Konditionierungsgroesse.Heizsoll).Perioden, p => p.Feiertagsregel == DbWerte.KOND_FEIERTAG_NEUJAHR);
            Kalenderregel pm = Assert.Single(stand.Kalender(Konditionierungsgroesse.Heizsoll).Perioden, p => p.Feiertagsregel == DbWerte.KOND_FEIERTAG_ERSTER_MAI);
            Assert.True(pn.IstFeiertag);
            Assert.Equal(w.Id, pn.Angabe.IdWoche);
            Assert.Equal(ruhe, pn.Angabe.Woche.ToArray());
            // Gegenprobe: der Feiertag ohne Verweis bleibt eine eingebettete Woche.
            Assert.Null(pm.Angabe.IdWoche);
            Assert.Equal(mai, pm.Angabe.Woche.ToArray());

            // Wieder exportiert: genau ein Verweis — über die Feiertagsregel, mit dem Bezeichner der Datenbank.
            AbbildZonenrest aus = GebaeudeRestdaten.AusZone(z, "EG", null, stand);
            AbbildPeriodenwoche p1 = Assert.Single(aus.Periodenwochen);
            Assert.Equal(new AbbildPeriodenwoche(Konditionierungsgroesse.Heizsoll, 0, 0, "Neujahr", "Betriebsruhe", DbWerte.KOND_FEIERTAG_NEUJAHR), p1);
            List<(string Schluessel, string Wert)> werte = GebaeudeRestdaten.Werte(aus);
            Assert.Contains(werte, x => x.Schluessel == GebaeudeRestdaten.FEIERTAGSWOCHE + "1");
            Assert.DoesNotContain(werte, x => x.Schluessel.StartsWith(GebaeudeRestdaten.PERIODENWOCHE, StringComparison.Ordinal));
        }

        /// <summary>
        /// Nachzug N4: Die <b>obere Raumtemperatur des Gebäudes</b> (<c>Tab_Gebaeude.Maximaleraumtemperatur</c>, Inventar B10) reist in
        /// beiden Formaten neben dem Erdreich, kommt im Import an das Zielfeld <c>MAX_RAUMTEMPERATUR</c> (mit Haken) und über die Hülle an
        /// das Editorfeld. Gegenprobe: der Vorgabewert 0 der Modellzeile reist nicht, die Zeile bleibt leer und ohne Haken.
        /// </summary>
        [Fact]
        public void Die_obere_Raumtemperatur_des_Gebaeudes_reist_in_beiden_Formaten_und_kommt_an_das_Zielfeld()
        {
            AbbildErdreich e = GebaeudeRestdaten.AusGebaeude(new ProjektGebaeudeModel { Maximaleraumtemperatur = 27.5 });
            Assert.Equal(27.5, e.MaximaleraumtemperaturC);
            Assert.Contains((GebaeudeRestdaten.GEBAEUDE_MAXIMALERAUMTEMPERATUR, "27.5"), GebaeudeRestdaten.Werte(e));
            Assert.Null(GebaeudeRestdaten.AusGebaeude(new ProjektGebaeudeModel { Maximaleraumtemperatur = 0.0 }));
            Assert.Equal(Exporteinstufung.Rundlaeufig, GebaeudeExportVerluste.Gebaeude["Maximaleraumtemperatur"]);
            Assert.DoesNotContain("Maximaleraumtemperatur", GebaeudeExportVerluste.Getragen(new ProjektGebaeudeModel { Maximaleraumtemperatur = 27.5 }, null));

            foreach (bool gbxml in new[] { true, false })
            {
                GebaeudeAbbild quelle = IfcExportRundlaufTests.Abbild();
                quelle.Gebaeude[0].Erdreich = e;
                byte[] datei = gbxml ? GbxmlExportProbe.Schreiben(quelle) : IfcExportProbe.Schreiben(quelle);
                Assert.Contains(GebaeudeRestdaten.GEBAEUDE_MAXIMALERAUMTEMPERATUR, Encoding.UTF8.GetString(datei), StringComparison.Ordinal);
                var a = new GebaeudeImportAblauf();
                using (var s = new System.IO.MemoryStream(datei))
                    a.Lesen(s, gbxml ? "probe.xml" : "probe.ifc", gbxml ? new GbxmlImportProfil() : new IfcImportProfil());
                Assert.Equal(27.5, a.Abbild.Gebaeude[0].Erdreich?.MaximaleraumtemperaturC);
                GebaeudeFeldzeile zeile = a.Zuordnen(0, null).Zeilen.Single(x => x.Zielfeld == GebaeudeZielfelder.MAX_RAUMTEMPERATUR);
                Assert.Equal(27.5, zeile.Wert);
                Assert.True(zeile.Uebernehmen, gbxml ? "gbXML" : "IFC");
            }
            // Gegenprobe: ohne die Angabe bleibt die Zeile leer und ohne Haken.
            var ohne = new GebaeudeImportAblauf();
            using (var s = new System.IO.MemoryStream(GbxmlExportProbe.Schreiben(IfcExportRundlaufTests.Abbild())))
                ohne.Lesen(s, "probe.xml", new GbxmlImportProfil());
            GebaeudeFeldzeile leer = ohne.Zuordnen(0, null).Zeilen.Single(x => x.Zielfeld == GebaeudeZielfelder.MAX_RAUMTEMPERATUR);
            Assert.False(leer.HatWert);
            Assert.False(leer.Uebernehmen);

            // Die Hülle setzt das Editorfeld aus der Zeile mit Haken.
            var ergebnis = new EPOS.UI.Dialoge.Import.GebaeudeImportErgebnis(0, null, "Probe", new Dictionary<string, bool>(), new List<EPOS.UI.Dialoge.Import.GebaeudeFeldzeileDaten>
            {
                new EPOS.UI.Dialoge.Import.GebaeudeFeldzeileDaten { Zielfeld = GebaeudeZielfelder.MAX_RAUMTEMPERATUR, Wert = 27.5, Haken = true },
            });
            Assert.Equal(27.5, GebaeudeImportHuelle.NachKatalogdaten(new EPOS.UI.Dialoge.Bedarf.GebaeudeKatalogDaten { MaxTemperatur = 24.0 }, ergebnis).MaxTemperatur);
        }

        /// <summary>
        /// Nachzug N4, rückwärtsverträglich: Ein Block ohne Feiertagsverweis und ohne Höchsttemperatur des Gebäudes (Dateien von vor N4)
        /// liest wie bisher, ohne übergangenen Wert; ein Feiertagsverweis mit unbekannter Regel wird benannt übergangen.
        /// </summary>
        [Fact]
        public void Ein_Block_ohne_die_Angaben_des_Nachzugs_N4_liest_wie_bisher()
        {
            string xml = Encoding.UTF8.GetString(GbxmlExportProbe.Schreiben(Abbild()));
            // Die Zeilen des Blocks stehen getrennt durch einen Zeilenumbruch (in der Datei auch als Zeichenreferenz).
            string alt = System.Text.RegularExpressions.Regex.Replace(xml, "(\\r?\\n|&#xA;|&#10;)(" + GebaeudeRestdaten.FEIERTAGSWOCHE + "[0-9]+|"
                                                                           + GebaeudeRestdaten.GEBAEUDE_MAXIMALERAUMTEMPERATUR + ")=[^\\r\\n&<]*", "");
            Assert.NotEqual(xml, alt);
            GbxmlAbbild gelesen = GbxmlExportProbe.Lesen(Encoding.UTF8.GetBytes(alt));
            AbbildGebaeude g = gelesen.Gebaeude[0];
            Assert.Null(g.Erdreich.MaximaleraumtemperaturC);
            Assert.Equal(10.0, g.Erdreich.KellertemperaturC);
            AbbildZonenrest r = g.Raeume.Single(x => x.Kennung == GebaeudeExportKennung.Raum(ZONE1)).Rest;
            AbbildPeriodenwoche p = Assert.Single(r.Periodenwochen);
            Assert.False(p.IstFeiertag);
            Assert.DoesNotContain(gelesen.Meldungen.Concat(g.Meldungen), m => m.Schluessel.EndsWith("RESTDATEN_UEBERSPRUNGEN", StringComparison.Ordinal));
            // Gegenprobe: eine unbekannte Feiertagsregel wird benannt übergangen, der Verweis des Zeitraums bleibt.
            GbxmlAbbild falsch = GbxmlExportProbe.Lesen(Encoding.UTF8.GetBytes(
                xml.Replace(";" + DbWerte.KOND_FEIERTAG_NEUJAHR + ";", ";KEIN_FEIERTAG;", StringComparison.Ordinal)));
            AbbildGebaeude gf = falsch.Gebaeude[0];
            Assert.Single(gf.Raeume.Single(x => x.Kennung == GebaeudeExportKennung.Raum(ZONE1)).Rest.Periodenwochen);
            Assert.Contains(gf.Meldungen, m => m.Schluessel.EndsWith("RESTDATEN_UEBERSPRUNGEN", StringComparison.Ordinal)
                                               && m.Werte.Last().Contains(GebaeudeRestdaten.FEIERTAGSWOCHE + "1", StringComparison.Ordinal));
        }

        /// <summary>
        /// Befund der Welle DA-7 (gemeinsamer Rundlauf): Die Außenwand Ost kam aus IFC mit 270° zurück. Gehalten werden die vier
        /// Himmelsrichtungen, die Neigung und der Nachbarraum jedes Bauteils im IFC-Rundlauf.
        /// </summary>
        [Fact]
        public void Ifc_Rundlauf_gibt_Azimut_aller_vier_Himmelsrichtungen_Neigung_und_Nachbarn_zurueck()
        {
            GebaeudeAbbild quelle = IfcExportRundlaufTests.Abbild();
            GebaeudeAbbild gelesen = IfcExportProbe.Lesen(IfcExportProbe.Schreiben(quelle));
            List<AbbildBauteil> lq = quelle.Gebaeude[0].Bauteile, ll = gelesen.Gebaeude[0].Bauteile;
            var fehler = new List<string>();
            var richtungen = new HashSet<double>();
            foreach (AbbildBauteil q in lq)
            {
                AbbildBauteil l = ll.FirstOrDefault(b => b.Name == q.Name);
                if (l == null) { fehler.Add(q.Name + " fehlt"); continue; }
                // Außenbauteile tragen Neigung und Azimut; ein inneres Bauteil darf sie offen lassen (der Import nimmt die Vorgabe
                // der Bauteilart), trägt es sie, müssen sie stimmen.
                bool aussen = q.Randbedingung == Randbedingung.Aussenluft || q.Randbedingung == Randbedingung.Erdreich;
                if (q.NeigungGrad.HasValue && (aussen || l.NeigungGrad.HasValue)
                    && !(Math.Abs(q.NeigungGrad.Value - (l.NeigungGrad ?? double.NaN)) <= 1e-6))
                    fehler.Add(q.Name + " Neigung " + q.NeigungGrad + " ≠ " + l.NeigungGrad);
                if (q.AzimutGrad.HasValue && q.NeigungGrad is > 1.0 and < 179.0 && (aussen || l.AzimutGrad.HasValue))
                {
                    double d = Math.Abs(((q.AzimutGrad.Value - (l.AzimutGrad ?? double.NaN)) % 360.0 + 540.0) % 360.0 - 180.0);
                    if (!(d <= 1e-6)) fehler.Add(q.Name + " Azimut " + q.AzimutGrad + " ≠ " + l.AzimutGrad);
                    else if (q.Randbedingung == Randbedingung.Aussenluft) richtungen.Add(q.AzimutGrad.Value);
                }
                // Die Nachbarräume nach dem Namen (eine Innenwand im selben Raum hat zwei Seiten, aber einen Raum).
                string Namen(AbbildBauteil b, GebaeudeAbbild a) => string.Join(",", b.Nachbarn.Select(n => a.Gebaeude[0].Raeume
                    .FirstOrDefault(r => r.Kennung == n.Kennung)?.Name ?? "?").Distinct().OrderBy(x => x, StringComparer.Ordinal));
                if (Namen(q, quelle) != Namen(l, gelesen)) fehler.Add(q.Name + " Nachbarn " + Namen(q, quelle) + " ≠ " + Namen(l, gelesen));
            }
            Assert.True(fehler.Count == 0, string.Join(" | ", fehler));
            Assert.Equal(new[] { 0.0, 90.0, 180.0, 270.0 }, richtungen.OrderBy(x => x));
        }
    }
}
