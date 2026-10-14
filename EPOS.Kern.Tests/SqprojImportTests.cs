using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Der Import allein aus der Projektdatei</b> (<c>.sqproj</c>, Importoption „nur Projektdatei“, Datenaustauschkonzept
    /// Kapitel 17) ohne Datenbank: Profil und Ablauf, der Selbstbezug der Projektdatei, die Zonierung nach den Zonen der Datei,
    /// die Herkunft <c>SQPROJ</c>, die Nordrichtung als Annahme und die Decke über Außenluft.
    /// </summary>
    public sealed class SqprojImportTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly List<string> _dateien = new List<string>();

        public void Dispose()
        {
            _kultur.Dispose();
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { }
        }

        internal const string AUFBAU = "{A2A2A2A2-0000-0000-0000-000000000001}";

        /// <summary>
        /// <b>Das Sporthaus</b>: EG (Lage 0 m) mit Halle und Umkleide, OG (Lage 3 m) mit Büro und unbeheiztem Lager;
        /// Simulationszonen „Sport“ (Halle, Umkleide) und „Verwaltung“ (Büro, Lager), Nutzungszonen quer dazu. Hülle: zwei
        /// Außenwände mit Aufbau aus zwei Schichten, ein Fenster, zwei Bodenplatten, zwei Dächer, die Geschossdecke Halle|Büro,
        /// eine Innenwand und eine Decke gegen Außenluft, die das Büro als Boden sieht (Rolle 8).
        /// </summary>
        internal static SqprojProbenErzeuger Sporthaus()
            => new SqprojProbenErzeuger { Baujahr = "1970-05-01 00:00:00" }
                .Geschoss("F1", "EG", 0.0, 3.0).Geschoss("F2", "OG", 3.0, 3.0)
                .Raum("R1", "Halle", "F1", "{31111111-1111-1111-1111-111111111111}", 50.0, null, 1, 3.0)
                .Raum("R2", "Umkleide", "F1", "{32222222-2222-2222-2222-222222222222}", 20.0, null, 1, 3.0)
                .Raum("R3", "Buero", "F2", "{33333333-3333-3333-3333-333333333333}", 30.0, null, 1, 3.0)
                .Raum("R4", "Lager", "F2", null, 10.0, null, 2, 3.0)
                .Zone("Z1", "Sport", 6, null, "R1", "R2")
                .Zone("Z2", "Verwaltung", 6, null, "R3", "R4")
                .Zone("Z3", "Nutzung Halle", 5, null, "R1")
                .Zone("Z4", "Nutzung Rest", 5, null, "R2", "R3", "R4")
                .Aufbau(AUFBAU, "Wand Sporthaus", 0.5)
                .Schicht("{A2A2A2A2-0000-0000-0000-0000000000L1}", AUFBAU, 1, "Stoff Alpha", 0.02, 0.71, 1410.0, 1.0)
                .Schicht("{A2A2A2A2-0000-0000-0000-0000000000L2}", AUFBAU, 2, "Stoff Beta", 0.30, 0.43, 1210.0, 1.0)
                .Flaeche("W1", 1, 3, 30.0, 34.0, 180.0, 90.0, cad: "C1", aufbau: AUFBAU, u: 0.5).Bezug("E1", "R1", "W1", 1)
                .Flaeche("FE1", 3, 3, 4.0, 4.0, 180.0, 90.0, eltern: "W1", cad: "CF1", u: 1.3).Bezug("E2", "R1", "FE1", 5).Fenster("FE1", 0.6, 30.0)
                .Flaeche("W2", 1, 3, 25.0, 25.0, 0.0, 90.0, cad: "C2", aufbau: AUFBAU, u: 0.5).Bezug("E3", "R3", "W2", 1)
                .Flaeche("BP1", 11, 5, 50.0, 50.0, null, 0.0, cad: "C3", u: 0.8).Bezug("E4", "R1", "BP1", 8)
                .Flaeche("BP2", 11, 5, 20.0, 20.0, null, 0.0, cad: "C4", u: 0.8).Bezug("E5", "R2", "BP2", 8)
                .Flaeche("DA1", 5, 3, 30.0, 30.0, null, 0.0, cad: "C5", u: 0.4).Bezug("E6", "R3", "DA1", 9)
                .Flaeche("DA2", 5, 3, 10.0, 10.0, null, 0.0, cad: "C6", u: 0.4).Bezug("E7", "R4", "DA2", 9)
                .Flaeche("D1", 4, 1, 30.0, 30.0, null, 0.0, cad: "C7", u: 1.0).Bezug("E8", "R1", "D1", 7).Bezug("E9", "R3", "D1", 8, 1)
                .Flaeche("W3", 1, 1, 15.0, 15.0, 90.0, 90.0, cad: "C8", u: 1.5).Bezug("E10", "R1", "W3", 2).Bezug("E11", "R2", "W3", 2, 1)
                .Flaeche("DU", 4, 3, 6.0, 6.0, null, 0.0, cad: "C9", u: 0.6).Bezug("E12", "R3", "DU", 8);

        internal string Schreiben(SqprojProbenErzeuger e, string name = "sporthaus")
        {
            string pfad = e.Schreiben(SqprojProbenErzeuger.TempPfad(name));
            _dateien.Add(pfad);
            return pfad;
        }

        /// <summary>Liest eine Projektdatei über den Ablauf wie die Hülle: Profil aus der Endung, der Strom ohne Puffer.</summary>
        internal static GebaeudeImportAblauf Lesen(string pfad)
        {
            var ablauf = new GebaeudeImportAblauf();
            GebaeudeImportProfil profil = GebaeudeImportProfil.FuerDatei(pfad);
            using (FileStream f = File.OpenRead(pfad))
                Assert.Equal(1, ablauf.Lesen(f, pfad, profil));
            return ablauf;
        }

        private static AbbildBauteil Bauteil(GebaeudeAbbild a, string kennung)
            => a.Gebaeude.SelectMany(g => g.Bauteile).FirstOrDefault(b => b.Kennung == kennung);

        [Fact]
        public void Die_Endung_sqproj_waehlt_das_Profil_und_steht_im_gemeinsamen_Filter()
        {
            Assert.IsType<SqprojImportProfil>(GebaeudeImportProfil.FuerDatei(@"C:\Ordner\Haus.SQPROJ"));
            Assert.Contains("*.sqproj", GebaeudeImportProfil.DATEIFILTER_ALLE.Split('|')[1].Split(';'));
            Assert.Equal(GebaeudeQuelle.FORMAT_SQPROJ, GebaeudeImportProfil.FuerDatei("haus.sqproj").Format);
        }

        [Fact]
        public void Der_Ablauf_belegt_die_Projektdatei_im_Selbstbezug_ohne_zweites_Lesen()
        {
            string pfad = Schreiben(Sporthaus());
            GebaeudeImportAblauf a = Lesen(pfad);
            byte[] inhalt = File.ReadAllBytes(pfad);

            Assert.True(a.AlleinAusProjektdatei);
            Assert.Equal(GebaeudeQuelle.FORMAT_SQPROJ, a.Quelle.Format);
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(inhalt)), a.Quelle.Hash);
            Assert.Equal(inhalt.LongLength, a.Quelle.Groesse);

            // Der Stand der Projektdatei: derselbe gelesene Inhalt, jeder Raum trifft sich selbst — ohne Meldung.
            SqprojStand p = a.Projektdatei;
            Assert.NotNull(p);
            Assert.False(p.Abgelehnt);
            Assert.Same(((SqprojGebaeudeAbbild)a.Abbild).Projektdatei, p.Abbild);
            Assert.Equal(4, p.Abgeglichen);
            Assert.Equal(0, p.NichtAbgeglichen);
            Assert.Equal(0, p.IfcOhneGegenstueck);
            foreach (AbbildRaum r in a.Abbild.Gebaeude[0].Raeume) Assert.Equal(r.Kennung, p.Abgleich.IfcRaum(r.Kennung));
            Assert.Empty(p.Meldungen);
            Assert.Equal(SqprojZonierung.Simulation, p.Gewaehlt);
            // Der Satz und der Vorschlag tragen denselben Stand.
            Assert.Same(p, a.Zuordnen(0, null).Projektdatei);
        }

        [Fact]
        public void Die_Datei_kann_nicht_als_Projektdatei_zu_sich_selbst_dazugeladen_werden()
        {
            string pfad = Schreiben(Sporthaus());
            GebaeudeImportAblauf a = Lesen(pfad);
            Assert.False(GebaeudeImportAblauf.IstHottcad(a.Abbild, 0));
        }

        [Fact]
        public void Die_Zonierung_folgt_den_Zonen_der_Projektdatei()
        {
            GebaeudeImportAblauf a = Lesen(Schreiben(Sporthaus()));
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a.Abbild, 0);
            Assert.Equal(GebaeudeImportProfil.ZONENREGEL_X1, z.Vorgabe);
            Assert.Contains(GebaeudeImportProfil.ZONENREGEL_X2, z.Regeln);
            // Die Zonen der Datei; das unbeheizte Lager trennt die Zonierung wie bei jedem Format ab.
            Assert.Equal(new[] { "Sport", "Verwaltung", "Verwaltung (unbeheizt)" }, z.Zonen.Select(x => x.Name).OrderBy(x => x, StringComparer.Ordinal));
            // Die Meldungen der Zonierung tragen den formatfreien Text (Präfix des gbXML-Wegs), nie den des IFC-Wegs.
            Assert.DoesNotContain(z.Meldungen, m => m.Schluessel.StartsWith(IfcImportProfil.MELDUNGSPRAEFIX, StringComparison.Ordinal));

            // Auch wenn jeder Raum seine eigene Zone ist, bleiben die Zonen der Datei die Vorgabe.
            GebaeudeImportAblauf b = Lesen(Schreiben(new SqprojProbenErzeuger()
                .Geschoss("F1", "EG", 0.0, 3.0)
                .Raum("R1", "A", "F1", null, 20.0, null, 1).Raum("R2", "B", "F1", null, 20.0, null, 1)
                .Zone("Z1", "Zone A", 6, null, "R1").Zone("Z2", "Zone B", 6, null, "R2")
                .Flaeche("W1", 1, 3, 10.0, 10.0, 180.0, 90.0, cad: "C1", u: 0.5).Bezug("E1", "R1", "W1", 1)
                .Flaeche("W2", 1, 3, 10.0, 10.0, 0.0, 90.0, cad: "C2", u: 0.5).Bezug("E2", "R2", "W2", 1), "je_raum"));
            Assert.Equal(GebaeudeImportProfil.ZONENREGEL_X1, GebaeudeZonierung.Bilden(b.Abbild, 0).Vorgabe);
        }

        [Fact]
        public void Der_Satz_traegt_die_Herkunft_Projektdatei_und_sie_wird_zurueckgelesen()
        {
            GebaeudeImportAblauf a = Lesen(Schreiben(Sporthaus()));
            GebaeudeImportSatz satz = a.Zuordnen(0, null);
            Assert.Contains(satz.Zeilen, z => z.Herkunft == Importherkunft.Sqproj);
            Assert.DoesNotContain(satz.Zeilen, z => z.Herkunft == Importherkunft.Ifc || z.Herkunft == Importherkunft.GbXml);
            Assert.NotNull(GebaeudeZuordnungsModell.KlasseDerDatei(satz));     // das Baujahr der Datei führt

            Assert.Equal(ImportherkunftWerte.SQPROJ, ImportherkunftWerte.Wert(Importherkunft.Sqproj));
            Assert.Equal(Importherkunft.Sqproj, ImportherkunftWerte.AusFormat(DbWerte.IMPORT_FORMAT_SQPROJ));
            Assert.Equal(Importherkunft.Sqproj, GebaeudeZuordnungsModell.HerkunftAusSchluessel(DbWerte.HERKUNFT_SQPROJ));
            Assert.True(ImportherkunftWerte.IstDatei(Importherkunft.Sqproj));
            Assert.Equal("Projektdatei", GebaeudeZuordnungsModell.HerkunftText(Importherkunft.Sqproj));
            Assert.Equal("Projektdatei", BaustoffCtrl.HerkunftText(DbWerte.HERKUNFT_SQPROJ));
            Assert.Equal("Projektdatei", GebaeudeZuordnungsModell.FormatText(new SqprojImportProfil()));
            using (new Kulturvorrichtung("en-US"))
            {
                Assert.Equal("Project file", GebaeudeZuordnungsModell.HerkunftText(Importherkunft.Sqproj));
                Assert.Equal("Project file", BaustoffCtrl.HerkunftText(DbWerte.HERKUNFT_SQPROJ));
            }

            // Der Vorschlag: Zonen und Bauteile mit Herkunft SQPROJ, Baustoffe der Projektdatei ebenso, Quelle „Projektdatei“.
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            Assert.All(v.Zonen, z => Assert.Equal(DbWerte.HERKUNFT_SQPROJ, z.Herkunft));
            Assert.All(v.Zeilen, z => Assert.Contains(z.Bauteil.Herkunft, new[] { DbWerte.HERKUNFT_SQPROJ, DbWerte.HERKUNFT_VORGABE }));
            Assert.DoesNotContain(v.Aufbauten, x => x.Aufbau.Herkunft == DbWerte.HERKUNFT_IFC);
            Assert.All(v.Aufbauten.SelectMany(x => x.Projektstoffe).Where(s => s != null), s =>
            {
                Assert.Equal(DbWerte.HERKUNFT_SQPROJ, s.Herkunft);
                Assert.Equal(GebaeudeBauteilvorschlag.QUELLE_PROJEKTDATEI, s.Quelle);
            });
        }

        [Fact]
        public void Ohne_Nordwinkel_gilt_die_Annahme_und_die_Vorgabe_dreht_ohne_Dateizugriff()
        {
            string pfad = Schreiben(Sporthaus());
            GebaeudeImportAblauf a = Lesen(pfad);
            Assert.Equal(Nordwinkelherkunft.Annahme, a.Quelle.NordwinkelHerkunft);
            Assert.Null(a.Quelle.NordwinkelGrad);
            Assert.Contains(a.Meldungen, m => m.Schluessel == SqprojGebaeudeLeser.KEIN_NORDEN && m.Stufe == PruefStufe.Warnung);
            Assert.Equal(180.0, Bauteil(a.Abbild, "W1").AzimutGrad);

            // Die Nordrichtungsabfrage: eine Vorgabe des Anwenders — die Datei ist dann schon fort.
            File.Delete(pfad);
            Assert.Equal(1, a.NordwinkelVorgeben(30.0));
            Assert.Equal(Nordwinkelherkunft.Eingabe, a.Quelle.NordwinkelHerkunft);
            Assert.Equal(30.0, a.Quelle.NordwinkelGrad);
            Assert.Equal(150.0, Bauteil(a.Abbild, "W1").AzimutGrad.Value, 6);
            Assert.DoesNotContain(a.Meldungen, m => m.Schluessel == SqprojGebaeudeLeser.KEIN_NORDEN);
            Assert.NotNull(a.Projektdatei);
            Assert.Equal(4, a.Projektdatei.Abgeglichen);
        }

        [Fact]
        public void Eine_Decke_gegen_Aussenluft_als_Boden_ihres_Raums_ist_eine_Decke_ueber_Aussenluft()
        {
            GebaeudeImportAblauf a = Lesen(Schreiben(Sporthaus()));
            AbbildBauteil du = Bauteil(a.Abbild, "DU");
            Assert.Equal(Bauteilart.Decke, du.Art);
            Assert.Equal(Randbedingung.Aussenluft, du.Randbedingung);
            // Die Grundfläche bleibt die der Bodenplatten — die Decke über Außenluft zählt nicht dazu.
            GebaeudeImportSatz satz = a.Zuordnen(0, null);
            Assert.Equal(70.0, satz.Zeile(GebaeudeZielfelder.FLAECHE_GRUND).Wert.Value, 6);
            // Gegenprobe: eine Bodenplatte bleibt eine Bodenplatte, ein Dach ein Dach.
            Assert.Equal(Bauteilart.Bodenplatte, Bauteil(a.Abbild, "BP1").Art);
            Assert.Equal(Bauteilart.Dach, Bauteil(a.Abbild, "DA1").Art);
        }

        [Fact]
        public void Neulesen_liest_die_Projektdatei_gegen_den_gespeicherten_Hash()
        {
            string pfad = Schreiben(Sporthaus());
            ImportquelleModel q = GebaeudeImportNeulesenTests.QuelleZu(pfad, DbWerte.IMPORT_FORMAT_SQPROJ);
            NeulesenErgebnis e;
            using (FileStream s = File.OpenRead(pfad))
                e = GebaeudeNeulesen.Lesen(s, pfad, q, "", false);
            Assert.Equal(NeulesenZustand.Passend, e.Zustand);
            Assert.NotNull(e.Abbild);
            Assert.Equal(GebaeudeImportProfil.ZONENREGEL_X1, e.Zonenregel);
            Assert.Equal(q.Hash, e.HashDatei);
            Assert.False(e.Hottcad);

            // Ein anderer Inhalt: Hash abweichend, ohne Abbild.
            string anders = Schreiben(Sporthaus().Raum("R9", "Abstell", "F2", null, 3.0, null, 2), "anders");
            using (FileStream s = File.OpenRead(anders))
                e = GebaeudeNeulesen.Lesen(s, anders, q, "", false);
            Assert.Equal(NeulesenZustand.HashAbweichend, e.Zustand);
            Assert.Null(e.Abbild);
            // Ein IFC-Name mit SQPROJ-Quelle: Format unbekannt.
            using (FileStream s = File.OpenRead(pfad))
                Assert.Equal(NeulesenZustand.FormatUnbekannt, GebaeudeNeulesen.Lesen(s, "haus.ifc", q, "", false).Zustand);
        }
    }
}
