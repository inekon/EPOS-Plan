using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Das Gebäudeabbild allein aus der Projektdatei</b> (<see cref="SqprojGebaeudeLeser"/>, Auftrag T1): Räume samt
    /// Beheizung, Bauteile je Hüllfläche mit beiden Räumen (eine Zeile mit zwei Bezügen, ein Seitenpaar über dieselbe
    /// <c>RepositoryElementUUID</c>), Öffnungen über <c>ParentUUID</c>, Randbedingung über den Nachbarraum bzw. den Code,
    /// fehlende Nordrichtung, Flächenherkunft Mengensatz, die benannte Ablehnung ohne Hülle und die Gegenprobe gegen das
    /// IFC-Abbild der gleichartigen Probe. Synthetische Proben, keine Anwenderdaten.
    /// </summary>
    public sealed class SqprojGebaeudeLeserTests : IDisposable
    {
        private readonly List<string> _dateien = new List<string>();

        public void Dispose()
        {
            foreach (string d in _dateien)
                try { File.Delete(d); } catch (IOException) { }
        }

        // ------------------------------------------------------------------
        //  Die Hausprobe
        // ------------------------------------------------------------------

        /// <summary>
        /// <b>Die Hausprobe</b>: EG (Lage 0 m) mit R1 beheizt und R2 unbeheizt, OG (Lage 3 m) mit R3 beheizt, R4 getrennt beheizt
        /// und R5 ohne Beheizungscode. Hüllflächen: W1 Außenwand Süd an R1 mit Fenster FE1; W2 Innenwand R1|R2 in EINER Zeile mit
        /// Code 7; W3a/W3b die zwei Seiten einer Wand R3|R4 (CAD-Objekt C3, Code 1); W6 Außenwand an R3 auf demselben CAD-Objekt
        /// wie W1 (kein Seitenpaar); D1 Decke R1|R3; BP Bodenplatte an R1; DA Dach an R3; W4 Wand R1 gegen unbeheizt (Code 2);
        /// W5 Wand R2 gegen Erdreich; X1 Wand R2 mit Code 6; W7 Wand R5 mit Code 1 ohne zweiten Raum.
        /// </summary>
        private const string AUFBAU = "{A1A1A1A1-0000-0000-0000-000000000001}";

        private static SqprojProbenErzeuger Haus()
        {
            var e = new SqprojProbenErzeuger { Baujahr = "1970-05-01 00:00:00", Standort = "S1" }
                .Ort("S1", "Musterstadt", "12345", 50.0, 8.0)
                .Geschoss("F1", "EG", 0.0, 3.0).Geschoss("F2", "OG", 3.0, 3.0)
                .Raum("R1", "Wohnen", "F1", "{11111111-1111-1111-1111-111111111111}", 20.0, 14, 1, 2.5)
                .Raum("R2", "Keller", "F1", null, 10.0, null, 2, 2.2)
                .Raum("R3", "Schlafen", "F2", null, 20.0, null, 1, 2.5)
                .Raum("R4", "Bad", "F2", null, 6.0, null, 4, 2.5)
                .Raum("R5", "Flur", "F2", null, 5.0)
                .Zone("Z1", "Simulation", 6, null, "R1", "R3")
                .Aufbau(AUFBAU, "Wand alt", 0.5)
                .Schicht("L1", AUFBAU, 1, "Putz", 0.015, 0.7, 1400.0, 1.0)
                .Schicht("L2", AUFBAU, 2, "Mauerwerk", 0.30, 0.6, 1600.0, 1.0)
                .Flaeche("W1", 1, 3, 8.0, 10.0, 180.0, 90.0, cad: "C1", aufbau: AUFBAU, u: 0.5, gid: "{21111111-1111-1111-1111-111111111111}").Bezug("E1", "R1", "W1", 1)
                .Flaeche("FE1", 3, 3, 2.0, 2.0, 180.0, 90.0, eltern: "W1", cad: "CF1", u: 1.3).Bezug("E2", "R1", "FE1", 5).Fenster("FE1", 0.6, 30.0)
                .Flaeche("W2", 1, 7, 12.0, 12.0, cad: "C2", u: 1.5).Bezug("E3", "R1", "W2", 2).Bezug("E4", "R2", "W2", 2, 1)
                .Flaeche("W3a", 1, 1, 9.0, 9.0, 90.0, 90.0, cad: "C3").Bezug("E5", "R3", "W3a", 2)
                .Flaeche("W3b", 1, 1, 9.0, 9.0, 270.0, 90.0, cad: "C3").Bezug("E6", "R4", "W3b", 2)
                .Flaeche("W6", 1, 3, 8.0, 8.0, 180.0, 90.0, cad: "C1").Bezug("E7", "R3", "W6", 1)
                .Flaeche("D1", 4, 1, 20.0, 20.0, null, 0.0, cad: "C4").Bezug("E8", "R1", "D1", 7).Bezug("E9", "R3", "D1", 8, 1)
                .Flaeche("BP", 11, 5, 20.0, 20.0, null, 0.0, cad: "C5").Bezug("E10", "R1", "BP", 8)
                .Flaeche("DA", 5, 3, 20.0, 20.0, null, 0.0, cad: "C6").Bezug("E11", "R3", "DA", 9)
                .Flaeche("W4", 1, 2, 5.0, 5.0, 0.0, 90.0, cad: "C7").Bezug("E12", "R1", "W4", 1)
                .Flaeche("W5", 1, 5, 6.0, 6.0, 0.0, 90.0, cad: "C8").Bezug("E13", "R2", "W5", 1)
                .Flaeche("X1", 1, 6, 4.0, 4.0, 270.0, 90.0, cad: "C9").Bezug("E14", "R2", "X1", 1)
                .Flaeche("W7", 1, 1, 3.0, 3.0, 90.0, 90.0, cad: "C10").Bezug("E15", "R5", "W7", 1);
            return e;
        }

        private string Schreiben(SqprojProbenErzeuger e, string name)
        {
            string pfad = e.Schreiben(SqprojProbenErzeuger.TempPfad(name));
            _dateien.Add(pfad);
            return pfad;
        }

        private GebaeudeAbbild Lesen(SqprojProbenErzeuger e, GebaeudeImportProfil profil = null)
        {
            string pfad = Schreiben(e, "gebaeude");
            profil ??= new SqprojImportProfil();
            using (FileStream f = File.OpenRead(pfad))
                return profil.LeserErzeugen().Lesen(f, profil, null, CancellationToken.None);
        }

        private static AbbildBauteil Bauteil(GebaeudeAbbild a, string kennung)
            => a.Gebaeude.SelectMany(g => g.Bauteile).Concat(a.BauteileOhneGebaeude).FirstOrDefault(b => b.Kennung == kennung);

        private static bool Hat(GebaeudeAbbild a, string schluessel, PruefStufe stufe, params string[] werte)
            => a.Meldungen.Any(m => m.Schluessel == schluessel && m.Stufe == stufe && werte.Select((w, i) => m.Werte.Length > i && m.Werte[i] == w).All(x => x));

        // ------------------------------------------------------------------
        //  Profil
        // ------------------------------------------------------------------

        [Fact]
        public void Profil_traegt_Format_Filter_Grenze_und_Leser()
        {
            var p = new SqprojImportProfil();
            Assert.Equal("SQPROJ", p.Format);
            Assert.Equal(DbWerte.IMPORT_FORMAT_SQPROJ, p.Format);
            Assert.Contains("*.sqproj", p.Dateifilter);
            Assert.Equal(SqprojProfil.MAX_BYTES, p.MaxBytes);
            Assert.Equal(SqprojProfil.MAX_BYTES_IOS, p.GrenzeFuerPlattform(true));
            Assert.Equal(SqprojProfil.MAX_BYTES, p.GrenzeFuerPlattform(false));
            Assert.Equal(GebaeudeImportProfil.ZONENREGEL_X4, p.Zonenregel);
            Assert.IsType<SqprojGebaeudeLeser>(p.LeserErzeugen());
            Assert.Equal("IMP_SQPROJ_PROT_ZU_GROSS", p.Meldung("ZU_GROSS"));
            Assert.NotNull(R.ResourceManager.GetString(p.Meldung("ZU_GROSS")));
            Assert.NotNull(R.ResourceManager.GetString(p.Meldung("LESEFEHLER")));
            Assert.NotNull(R.ResourceManager.GetString(p.SchemaanzeigeSchluessel));
            Assert.DoesNotContain(DbWerte.IMPORT_FORMAT_SQPROJ, DbWerte.IMPORT_FORMATE);
        }

        // ------------------------------------------------------------------
        //  Räume
        // ------------------------------------------------------------------

        [Fact]
        public void Raeume_tragen_Beheizung_Hoehe_Geschoss_und_Zone()
        {
            GebaeudeAbbild a = Lesen(Haus());
            Assert.Equal(GebaeudeQuelle.FORMAT_SQPROJ, a.Format);
            AbbildGebaeude g = Assert.Single(a.Gebaeude);
            Assert.Equal("B1", g.Kennung);
            Assert.Equal(1970, g.Baujahr);
            Assert.Equal("12345 Musterstadt", a.Ort);
            Assert.Equal(50.0, a.BreiteGrad);
            Assert.Equal(new[] { "EG", "OG" }, g.Geschosse.Select(x => x.Name));
            Assert.Equal(3.0, g.Geschosse[1].LageM);
            Assert.Equal(5, g.Raeume.Count);

            AbbildRaum R(string k) => g.Raeume.Single(r => r.Kennung == k);
            Assert.True(R("R1").Beheizt);
            Assert.Equal(BeheiztQuelle.Attribut, R("R1").BeheiztQuelle);
            Assert.False(R("R2").Beheizt);
            Assert.Equal(BeheiztQuelle.Attribut, R("R2").BeheiztQuelle);
            Assert.True(R("R4").Beheizt);
            Assert.Equal("4", R("R4").Zustandsangabe);
            Assert.True(R("R5").Beheizt);
            Assert.Equal(BeheiztQuelle.Annahme, R("R5").BeheiztQuelle);

            Assert.Equal(20.0, R("R1").FlaecheM2);
            Assert.Equal(60.0, R("R1").VolumenM3);
            Assert.Equal(2.5, R("R1").HoeheM);
            Assert.Equal("14", R("R1").Raumtyp);
            Assert.Equal("F2", R("R3").GeschossKennung);
            Assert.Equal("OG", R("R3").GeschossName);
            Assert.Equal(3.0, R("R3").GeschossLageM);
            Assert.Equal("Z1", R("R1").ZonenKennung);
            Assert.Null(R("R2").ZonenKennung);
            Assert.Equal(1, g.ZahlZonen);
            Assert.Equal(2, g.ZahlGeschosseMitRaeumen);
            Assert.Equal("11111111-1111-1111-1111-111111111111", R("R1").HottcadGuid, StringComparer.OrdinalIgnoreCase);
        }

        [Fact]
        public void Unbekannter_Beheizungscode_wird_gemeldet_und_als_beheizt_angenommen()
        {
            GebaeudeAbbild a = Lesen(Haus().Raum("R6", "Abstell", "F1", null, 3.0, null, 9));
            AbbildRaum r = a.Gebaeude[0].Raeume.Single(x => x.Kennung == "R6");
            Assert.True(r.Beheizt);
            Assert.Equal(BeheiztQuelle.Annahme, r.BeheiztQuelle);
            Assert.True(Hat(a, SqprojGebaeudeLeser.BEHEIZUNG_UNBEKANNT, PruefStufe.Warnung, "9", "1"));
        }

        // ------------------------------------------------------------------
        //  Bauteile und Nachbarn
        // ------------------------------------------------------------------

        [Fact]
        public void Seitenpaar_gleicher_RepositoryElementUUID_wird_ein_Bauteil_mit_beiden_Raeumen()
        {
            GebaeudeAbbild a = Lesen(Haus());
            AbbildBauteil w3 = Bauteil(a, "W3a");
            Assert.NotNull(w3);
            Assert.Null(Bauteil(a, "W3b"));
            Assert.Equal(new[] { "R3", "R4" }, w3.Nachbarn.Select(n => n.Kennung));
            Assert.Equal(Bauteilart.Innenwand, w3.Art);
            Assert.Equal(Randbedingung.Innen, w3.Randbedingung);
            Assert.Equal(9.0, w3.NettoflaecheM2);
            // Gegenprobe: zwei Außenwände desselben CAD-Objekts bleiben zwei Bauteile.
            Assert.Equal("R1", Assert.Single(Bauteil(a, "W1").Nachbarn).Kennung);
            Assert.Equal("R3", Assert.Single(Bauteil(a, "W6").Nachbarn).Kennung);
            Assert.True(Hat(a, SqprojGebaeudeLeser.HUELLE, PruefStufe.Info, "11", "1", "1"));
        }

        [Fact]
        public void Eine_Zeile_mit_zwei_Bezuegen_traegt_beide_Raeume()
        {
            GebaeudeAbbild a = Lesen(Haus());
            Assert.Equal(new[] { "R1", "R2" }, Bauteil(a, "W2").Nachbarn.Select(n => n.Kennung));
            AbbildBauteil d1 = Bauteil(a, "D1");
            Assert.Equal(new[] { "R1", "R3" }, d1.Nachbarn.Select(n => n.Kennung));
            Assert.Equal(new[] { "Ceiling", "InteriorFloor" }, d1.Nachbarn.Select(n => n.Sicht));
            Assert.Equal(Bauteilart.Decke, d1.Art);
            Assert.Equal(0.0, d1.NeigungGrad);
            Assert.Null(d1.AzimutGrad);
        }

        [Fact]
        public void Fenster_haengt_ueber_ParentUUID_an_seiner_Wand()
        {
            GebaeudeAbbild a = Lesen(Haus());
            Assert.Null(Bauteil(a, "FE1"));
            AbbildBauteil w1 = Bauteil(a, "W1");
            AbbildBauteil fe = Assert.Single(w1.Oeffnungen);
            Assert.Equal("FE1", fe.Kennung);
            Assert.Equal(Bauteilart.Fenster, fe.Art);
            Assert.Equal(2.0, fe.NettoflaecheM2);
            Assert.Equal(1.3, fe.UWertWm2K);
            Assert.Equal(0.6, fe.GWert);
            Assert.Equal(0.3, fe.Rahmenanteil.Value, 9);
            Assert.Equal(SqprojGebaeudeLeser.BELEG_RAHMENANTEIL, fe.RahmenanteilBeleg);
            Assert.Equal(Randbedingung.Aussenluft, fe.Randbedingung);
            Assert.Equal(180.0, fe.AzimutGrad);
            Assert.Equal(10.0, w1.BruttoflaecheM2);
            Assert.Equal(8.0, w1.NettoflaecheM2);
        }

        [Fact]
        public void Randbedingung_aus_dem_Nachbarraum_sonst_aus_dem_Code()
        {
            GebaeudeAbbild a = Lesen(Haus());
            // Zwei Räume: die Beheizung entscheidet, Code 7 ist nur gemeldet.
            AbbildBauteil w2 = Bauteil(a, "W2");
            Assert.Equal(Randbedingung.Innen, w2.Randbedingung);
            Assert.Equal(Randbedingung.Innen, w2.RandbedingungSeiteA);
            Assert.Equal(Randbedingung.Unbeheizt, w2.RandbedingungSeiteB);
            Assert.Equal(Randbedingung.Unbeheizt, w2.RandbedingungWirksam);
            Assert.True(Hat(a, SqprojGebaeudeLeser.NACHBARART_HERGELEITET, PruefStufe.Info, "7", "1"));
            Assert.Equal(Randbedingung.Innen, Bauteil(a, "W3a").RandbedingungWirksam);
            // Ein Raum: der Code.
            Assert.Equal(Randbedingung.Innen, Bauteil(a, "W7").Randbedingung);            // 1
            Assert.Equal(Randbedingung.Unbeheizt, Bauteil(a, "W4").Randbedingung);        // 2
            Assert.Equal(Randbedingung.Aussenluft, Bauteil(a, "W1").Randbedingung);       // 3
            Assert.Equal(Bauteilart.Aussenwand, Bauteil(a, "W1").Art);
            Assert.Equal(Randbedingung.Erdreich, Bauteil(a, "W5").Randbedingung);         // 5
            Assert.Equal(Bauteilart.Aussenwand, Bauteil(a, "W5").Art);
            Assert.Null(Bauteil(a, "W1").RandbedingungWirksam);
            // Unbekannt ohne zweiten Raum: unbestimmt und gemeldet, nie still.
            Assert.Equal(Randbedingung.Unbekannt, Bauteil(a, "X1").Randbedingung);
            Assert.True(Hat(a, SqprojGebaeudeLeser.NACHBARART_UNBEKANNT, PruefStufe.Warnung, "6", "1"));
        }

        [Fact]
        public void Boden_Dach_und_Wand_tragen_Neigung_und_Azimut_der_Abbildkonvention()
        {
            GebaeudeAbbild a = Lesen(Haus());
            AbbildBauteil bp = Bauteil(a, "BP");
            Assert.Equal(Bauteilart.Bodenplatte, bp.Art);
            Assert.Equal(Randbedingung.Erdreich, bp.Randbedingung);
            Assert.Equal(180.0, bp.NeigungGrad);
            AbbildBauteil da = Bauteil(a, "DA");
            Assert.Equal(Bauteilart.Dach, da.Art);
            Assert.Equal(0.0, da.NeigungGrad);
            Assert.Null(da.AzimutGrad);
            Assert.Equal(90.0, Bauteil(a, "W1").NeigungGrad);
            Assert.Equal(180.0, Bauteil(a, "W1").AzimutGrad);
            Assert.Equal(270.0, Bauteil(a, "X1").AzimutGrad);
            Assert.Equal("F1", Bauteil(a, "W1").GeschossKennung);
        }

        [Fact]
        public void Flaechenherkunft_ist_der_Mengensatz()
        {
            GebaeudeAbbild a = Lesen(Haus());
            List<AbbildBauteil> alle = a.Gebaeude.SelectMany(g => g.Bauteile).ToList();
            Assert.Equal(11, alle.Count);
            Assert.All(alle, b => Assert.Equal(Flaechenherkunft.Mengensatz, b.Flaechenherkunft));
            Assert.All(alle.SelectMany(b => b.Oeffnungen), b => Assert.Equal(Flaechenherkunft.Mengensatz, b.Flaechenherkunft));
            Assert.All(alle, b => Assert.Null(b.RandpunkteM));
            Assert.Empty(a.BauteileOhneGebaeude);
        }

        [Fact]
        public void Aufbau_ueber_CatalogDimUUID_mit_Schichten_der_Projektdatei()
        {
            GebaeudeAbbild a = Lesen(Haus());
            AbbildBauteil w1 = Bauteil(a, "W1");
            Assert.Equal(0.5, w1.UWertWm2K);
            Assert.Equal(SqprojGebaeudeLeser.QUELLTYP_BAUTEIL, w1.UWertQuelle);
            Assert.NotNull(w1.Aufbau);
            Assert.Equal(2, w1.Aufbau.Schichten.Count);
            Assert.All(w1.Aufbau.Schichten, s => Assert.True(s.AusProjektdatei));
            Assert.Equal(Schichtrichtung.InnenNachAussen, w1.Aufbau.Richtung);
            Assert.Equal(Aufbaustatus.Vollstaendig, w1.Aufbau.Status);
            Assert.Null(Bauteil(a, "W2").Aufbau);
            Assert.Equal(1.5, Bauteil(a, "W2").UWertWm2K);
        }

        // ------------------------------------------------------------------
        //  Nordrichtung
        // ------------------------------------------------------------------

        [Fact]
        public void Ohne_Nordwinkel_meldet_der_Leser_die_Annahme()
        {
            GebaeudeAbbild a = Lesen(Haus());
            Assert.Null(a.NordwinkelGrad);
            Assert.Null(a.NordwinkelWirksamGrad);
            Assert.Equal(Nordwinkelherkunft.Annahme, a.NordwinkelHerkunft);
            Assert.True(Hat(a, SqprojGebaeudeLeser.KEIN_NORDEN, PruefStufe.Warnung));
        }

        [Fact]
        public void Die_Vorgabe_des_Anwenders_dreht_die_Azimute_einmal()
        {
            var profil = new SqprojImportProfil { NordwinkelVorgabeGrad = 90.0 };
            GebaeudeAbbild a = Lesen(Haus(), profil);
            Assert.Equal(90.0, a.NordwinkelWirksamGrad);
            Assert.Equal(Nordwinkelherkunft.Eingabe, a.NordwinkelHerkunft);
            Assert.False(Hat(a, SqprojGebaeudeLeser.KEIN_NORDEN, PruefStufe.Warnung));
            Assert.Equal(90.0, Bauteil(a, "W1").AzimutGrad);
            Assert.Equal(90.0, Bauteil(a, "W1").Oeffnungen[0].AzimutGrad);
        }

        // ------------------------------------------------------------------
        //  Ablehnungen
        // ------------------------------------------------------------------

        [Fact]
        public void Datei_ohne_Huellflaechen_wird_benannt_abgelehnt()
        {
            GebaeudeAbbild a = Lesen(SqprojProbenErzeuger.Standard());
            Assert.Empty(a.Gebaeude);
            Assert.True(Hat(a, SqprojGebaeudeLeser.HUELLE_UNVOLLSTAENDIG, PruefStufe.Fehler));
            Assert.NotNull(R.ResourceManager.GetString(SqprojGebaeudeLeser.HUELLE_UNVOLLSTAENDIG));

            // Nur Öffnungen sind keine Hülle.
            GebaeudeAbbild b = Lesen(SqprojProbenErzeuger.Standard().Flaeche("FX", 3, 3, 2.0).Bezug("EX", "R1", "FX", 5));
            Assert.True(Hat(b, SqprojGebaeudeLeser.HUELLE_UNVOLLSTAENDIG, PruefStufe.Fehler));
        }

        [Fact]
        public void Keine_SQLite_Datei_und_zu_grosse_Datei_werden_benannt_abgelehnt()
        {
            var profil = new SqprojImportProfil();
            using (var m = new MemoryStream(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17 }))
            {
                GebaeudeAbbild a = profil.LeserErzeugen().Lesen(m, profil, null, CancellationToken.None);
                Assert.True(Hat(a, SqprojGebaeudeLeser.KEINE_DATEI, PruefStufe.Fehler));
                Assert.Empty(a.Gebaeude);
            }
            var klein = new SqprojImportProfil(10);
            using (var m = new MemoryStream(new byte[100]))
            {
                GebaeudeAbbild a = klein.LeserErzeugen().Lesen(m, klein, null, CancellationToken.None);
                Assert.True(Hat(a, SqprojGebaeudeLeser.ZU_GROSS, PruefStufe.Fehler, "100", "10"));
            }
            GebaeudeAbbild t = Lesen(Haus().Ohne("BmZone"));
            Assert.True(Hat(t, SqprojGebaeudeLeser.TABELLE_FEHLT, PruefStufe.Fehler, "BmZone"));
        }

        [Fact]
        public void Abbruch_verlaesst_den_Leser()
        {
            string pfad = Schreiben(Haus(), "abbruch");
            var profil = new SqprojImportProfil();
            using (var quelle = new CancellationTokenSource())
            using (FileStream f = File.OpenRead(pfad))
            {
                quelle.Cancel();
                Assert.Throws<OperationCanceledException>(() => profil.LeserErzeugen().Lesen(f, profil, null, quelle.Token));
            }
        }

        // ------------------------------------------------------------------
        //  Die formatneutralen Schritte
        // ------------------------------------------------------------------

        [Fact]
        public void Satz_Zonierung_Bauteilvorschlag_Zonenplan_und_Grundriss_laufen_auf_dem_Abbild()
        {
            var profil = new SqprojImportProfil();
            var ablauf = new GebaeudeImportAblauf();
            using (FileStream f = File.OpenRead(Schreiben(Haus(), "ablauf")))
                Assert.Equal(1, ablauf.Lesen(f, "haus.sqproj", profil));
            Assert.Equal(GebaeudeQuelle.FORMAT_SQPROJ, ablauf.Quelle.Format);
            GebaeudeImportSatz satz = ablauf.Zuordnen(0, 'E');
            Assert.NotNull(satz);
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(ablauf.Abbild, 0);
            Assert.NotNull(z);
            Assert.NotNull(GebaeudeBauteilvorschlag.Bilden(ablauf.Abbild, 0, 'E', ablauf.Quelle, profil, zonierung: z));
            Assert.NotNull(Zonenplan.Vorschlag(ablauf.Abbild, 0));
            Assert.NotNull(GebaeudeGrundriss.Bilden(ablauf.Abbild, 0, z));
        }

        // ------------------------------------------------------------------
        //  Gegenprobe gegen das IFC-Abbild
        // ------------------------------------------------------------------

        [Fact]
        public void Raeume_gleichen_dem_IFC_Abbild_der_gleichartigen_Probe()
        {
            GebaeudeAbbild sq = Lesen(SqprojProbenErzeuger.Standard().Flaeche("W1", 1, 3, 8.0, 8.0, 0.0, 90.0).Bezug("E1", "R1", "W1", 1));
            GebaeudeAbbild ifc = SqprojProbenErzeuger.IfcAbbild();
            AbbildGebaeude gs = Assert.Single(sq.Gebaeude), gi = Assert.Single(ifc.Gebaeude);
            Assert.Equal(gi.Raeume.Count, gs.Raeume.Count);
            Assert.Equal(gi.Geschosse.Select(g => g.Name), gs.Geschosse.Select(g => g.Name));
            foreach (string name in new[] { "Raum A", "Raum C" })
            {
                AbbildRaum s = gs.Raeume.Single(r => r.Name == name), i = gi.Raeume.Single(r => r.Name == name);
                Assert.Equal(i.FlaecheM2, s.FlaecheM2);
                Assert.Equal(i.VolumenM3, s.VolumenM3);
                Assert.Equal(gi.Geschosse.Single(g => g.Kennung == i.GeschossKennung).Name, s.GeschossName);
            }
            // Raum D trägt die GUID, die das IFC-Abbild als GlobalId des Raums X führt.
            AbbildRaum d = gs.Raeume.Single(r => r.Name == "Raum D");
            Assert.Equal(SqprojRaumabgleich.IfcKennung(SqprojProbenErzeuger.GID_D), SqprojRaumabgleich.IfcKennung(d.HottcadGuid));
            Assert.Contains(gi.Raeume, r => r.Kennung == SqprojRaumabgleich.IfcKennung(d.HottcadGuid));
            // Gleiche Konvention wie die IFC-Seite ohne Nordwinkel: Azimut im Modellsystem, 0 = Modell-Nord.
            Assert.Equal(0.0, Bauteil(sq, "W1").AzimutGrad);
            Assert.Null(ifc.NordwinkelWirksamGrad);
            Assert.Null(sq.NordwinkelWirksamGrad);
        }
    }
}
