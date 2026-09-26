using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Werkzeug der Proben des Bauteilvorschlags (Stufe G4b, Welle A): Importproben lesen, den
    /// Vorschlag bilden, Zeilen finden.
    /// </summary>
    internal static class BauteilvorschlagProbe
    {
        /// <summary>Liest eine Importprobe mit dem Profil ihrer Endung.</summary>
        internal static GebaeudeImportAblauf Lesen(string name)
        {
            string pfad = Path.Combine(IfcProbenTests.Ordner(), name);
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(pfad))
                a.Lesen(s, pfad, GebaeudeImportProfil.FuerDatei(name));
            Assert.True(a.Abbild != null, name + ": " + string.Join(" | ", a.Meldungen.Select(m => m.ToString())));
            return a;
        }

        /// <summary>Der Vorschlag eines Gebäudes einer Importprobe.</summary>
        internal static GebaeudeBauteilvorschlag Vorschlag(string name, int index = 0, char? klasse = null,
                                                           IReadOnlyDictionary<string, bool> uebersteuert = null)
            => GebaeudeBauteilvorschlag.Bilden(Lesen(name), index, klasse, uebersteuert);

        /// <summary>Die eine Zeile einer Quellentität (bei innerer Masse: einer Seite).</summary>
        internal static GebaeudeBauteilzeile Zeile(GebaeudeBauteilvorschlag v, string kennung, string seite = null)
            => Assert.Single(v.Zeilen, z => z.Kennung == kennung && z.Seite == seite);

        /// <summary>Der Aufbau einer Zeile.</summary>
        internal static BauteilaufbauModel Aufbau(GebaeudeBauteilvorschlag v, GebaeudeBauteilzeile z)
        {
            Assert.True(z.Bauteil.ID_Aufbau.HasValue, z.ToString());
            return v.AufbautenJeId[z.Bauteil.ID_Aufbau.Value];
        }

        internal static void Nah(double erwartet, double? ist, double toleranz = 1e-9)
        {
            Assert.True(ist.HasValue, "kein Wert, erwartet " + erwartet.ToString("R", CultureInfo.InvariantCulture));
            Assert.True(Math.Abs(ist.Value - erwartet) <= toleranz * Math.Max(1.0, Math.Abs(erwartet)),
                        "erwartet " + erwartet.ToString("R", CultureInfo.InvariantCulture) + ", ist " + ist.Value.ToString("R", CultureInfo.InvariantCulture));
        }

        /// <summary>Die Meldungen einer Stufe als Schlüssel.</summary>
        internal static string[] Schluessel(GebaeudeBauteilvorschlag v, PruefStufe stufe)
            => v.Meldungen.Where(m => m.Stufe == stufe).Select(m => m.Schluessel).ToArray();

        /// <summary>Ein gbXML-Abbild ohne Datei: ein beheizter Raum (50 m², 125 m³), Dach und Bodenplatte mit U-Wert.</summary>
        internal static GbxmlAbbild Synthetisch()
        {
            var a = new GbxmlAbbild();
            var g = new AbbildGebaeude { Kennung = "G1", Name = "Synthetisch" };
            g.Raeume.Add(new AbbildRaum { Kennung = "R1", Quelltyp = "Space", Name = "Raum", FlaecheM2 = 50, VolumenM3 = 125 });
            g.Bauteile.Add(Flaeche("dach", Bauteilart.Dach, Randbedingung.Aussenluft, 50, 0.2, 0, null, "R1"));
            g.Bauteile.Add(Flaeche("boden", Bauteilart.Bodenplatte, Randbedingung.Erdreich, 50, 0.3, 180, null, "R1"));
            a.Gebaeude.Add(g);
            return a;
        }

        internal static AbbildBauteil Flaeche(string kennung, Bauteilart art, Randbedingung rand, double brutto, double? u,
                                              double? neigung, double? azimut, params string[] nachbarn)
        {
            var b = new AbbildBauteil
            {
                Kennung = kennung, Quelltyp = "Surface", Art = art, Randbedingung = rand, BruttoflaecheM2 = brutto,
                UWertWm2K = u, NeigungGrad = neigung, AzimutGrad = azimut,
            };
            foreach (string n in nachbarn) b.Nachbarn.Add(new AbbildNachbar(n, null));
            return b;
        }

        /// <summary>Ein vollständiger, symmetrischer Aufbau: Putz, Kalksandstein, Putz (Folge wie gbXML: erste Schicht außen).</summary>
        internal static AbbildAufbau Massiv(string kennung)
        {
            var a = new AbbildAufbau { Kennung = kennung, Name = "Massivwand", Status = Aufbaustatus.Vollstaendig };
            a.Schichten.Add(new AbbildSchicht { DickeM = 0.01, LambdaWmK = 0.7, RhoKgM3 = 1400, CpJkgK = 1000 });
            a.Schichten.Add(new AbbildSchicht { DickeM = 0.115, LambdaWmK = 1.0, RhoKgM3 = 1800, CpJkgK = 1000 });
            a.Schichten.Add(new AbbildSchicht { DickeM = 0.01, LambdaWmK = 0.7, RhoKgM3 = 1400, CpJkgK = 1000 });
            return a;
        }

        internal static GebaeudeBauteilvorschlag Bilden(GebaeudeAbbild a, char? klasse = null)
            => GebaeudeBauteilvorschlag.Bilden(a, 0, klasse, null, new GbxmlImportProfil());
    }

    /// <summary>
    /// <b>Stufe G4b, Welle A — der Bauteilvorschlag des Imports</b>, ohne Datenbank: Zahl und Art der
    /// Zeilen, Flächensummen je Gruppe gleich den Summenfeldern der Zuordnung, Fensterabzug, Azimut
    /// mit Nordwinkel, Schichtfolge innen → außen, Rückfälle, Herkunft, benannte Ablehnungen, die
    /// innere Masse nach Datenlage (Anwenderentscheid vom 25.09.2026) und die Abbildung über
    /// <see cref="GebaeudeZonenabbildung.AlsZonensatz"/> und <see cref="GebaeudeModellEingang"/>.
    /// </summary>
    public class GebaeudeBauteilvorschlagTests
    {
        private static GebaeudeBauteilvorschlag Vorschlag(string name, int index = 0, char? klasse = null)
            => BauteilvorschlagProbe.Vorschlag(name, index, klasse);

        private static void Nah(double erwartet, double? ist, double toleranz = 1e-9) => BauteilvorschlagProbe.Nah(erwartet, ist, toleranz);

        // =====================================================================
        //  Zahl und Art der Zeilen
        // =====================================================================

        [Fact]
        public void Das_Probenhaus_gbXML_ergibt_eine_Zone_mit_Huelle_und_innerer_Masse()
        {
            GebaeudeBauteilvorschlag v = Vorschlag("gbxml_haus_si.xml");
            Assert.False(v.Abgelehnt);
            Assert.Empty(BauteilvorschlagProbe.Schluessel(v, PruefStufe.Fehler));
            Assert.Empty(BauteilvorschlagProbe.Schluessel(v, PruefStufe.Warnung));
            Assert.Equal(GebaeudeQuelle.FORMAT_GBXML, v.Format);
            Assert.Equal("gbxml_haus_si.xml", v.Dateiname);

            // Die eine Zone: die drei beheizten Räume, nicht der Keller.
            ZoneModel z = v.Zone;
            Assert.Equal(-1, z.ID);
            Assert.Equal("Haus 1", z.Bezeichner);
            Assert.Equal(120.0, z.Nutzflaeche);
            Assert.Equal(300.0, z.Volumen);
            Assert.Equal(2.5, z.Raumhoehe);
            Assert.True(z.IstBeheizt);
            Assert.Equal(DbWerte.HERKUNFT_GBXML, z.Herkunft);
            Assert.Equal("geb-1", z.Quellkennung);
            Assert.Equal(Importherkunft.GbXml, v.HerkunftNutzflaeche);
            Assert.Equal(new[] { "raum-wohnen", "raum-kueche", "raum-schlafen" }, v.Raeume.Select(r => r.Quellkennung));
            Assert.All(v.Raeume, r => Assert.Equal(ImportZiel.Zone, r.Ziel));

            // Zahl und Art der Zeilen: zehn Außenwände, vierzehn Fenster, eine Tür, ein Dach, zwei
            // Decken gegen den Keller; innere Masse: Innenwand und zwei Geschossdecken je zweiseitig.
            var arten = v.Zeilen.GroupBy(x => x.Bauteil.Bauteilart).ToDictionary(g => g.Key, g => g.Count());
            Assert.Equal(10, arten[DbWerte.BAUTEILART_AUSSENWAND]);
            Assert.Equal(14, arten[DbWerte.BAUTEILART_FENSTER]);
            Assert.Equal(1, arten[DbWerte.BAUTEILART_TUER]);
            Assert.Equal(1, arten[DbWerte.BAUTEILART_DACH]);
            Assert.Equal(6, arten[DbWerte.BAUTEILART_DECKE]);
            Assert.Equal(2, arten[DbWerte.BAUTEILART_INNENWAND]);
            Assert.Equal(34, v.Zeilen.Count);
            Assert.Equal(6, v.Zeilen.Count(x => x.Summenfeld == null));
            Assert.Equal(Innenweg.Bauteile, v.Innenweg);
            Assert.Null(v.Innenflaechenfaktor);
            Assert.Equal(Importherkunft.Leer, v.HerkunftInnenflaechenfaktor);

            // Die Zone trägt dieselben Modelle, vorläufige Ids −1 … −34.
            Assert.Equal(v.Zeilen.Select(x => x.Bauteil), z.Bauteile);
            Assert.Equal(Enumerable.Range(1, 34).Select(i => -i), z.Bauteile.Select(b => b.ID));

            // Die Kellerdecke: Boden gegen den unbeheizten Keller — Grundfläche, Randbedingung unbeheizt.
            GebaeudeBauteilzeile kd = BauteilvorschlagProbe.Zeile(v, "decke-kg-eg-wohnen");
            Assert.Equal(DbWerte.BAUTEILART_DECKE, kd.Bauteil.Bauteilart);
            Assert.Equal(DbWerte.RANDBEDINGUNG_UNBEHEIZT, kd.Bauteil.Randbedingung);
            Assert.Equal(GebaeudeZielfelder.FLAECHE_GRUND, kd.Summenfeld);
            Assert.Equal(180.0, kd.Bauteil.Neigung);
            Assert.Contains(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.UNBEHEIZT && m.Werte[0] == "2" && m.Werte[1] == "60");

            // Die Quellentität je Zeile: Flächen, Öffnungen — Kennung gekürzt in der Zeile.
            Assert.All(v.Zeilen, x => Assert.Equal(x.Kennung, x.Bauteil.Quellkennung));
            Assert.Equal("Opening", BauteilvorschlagProbe.Zeile(v, "fenster-eg-s1").Quelltyp);
            Assert.Equal("Surface", BauteilvorschlagProbe.Zeile(v, "aw-og-nord").Quelltyp);

            // Sechs Aufbauten: je Konstruktion einer, die Geschossdecke zweimal (je Seite).
            Assert.Equal(new[] { "Außenwand mit Innendämmung", "Flachdach", "Kellerdecke", "Innenwand", "Geschossdecke", "Geschossdecke (Gegenseite)" },
                         v.Aufbauten.Select(a => a.Aufbau.Bezeichner));
            Assert.All(v.Aufbauten, a => Assert.Equal(DbWerte.HERKUNFT_GBXML, a.Aufbau.Herkunft));
            Assert.All(v.Aufbauten, a => Assert.Equal("gbxml_haus_si.xml", a.Aufbau.Quelle));
            Assert.All(v.Aufbauten, a => Assert.Equal("Construction", a.Quelltyp));
            Assert.All(v.Aufbauten, a => Assert.True(a.RichtungAngenommen));
            Assert.Equal(new[] { false, false, false, false, false, true }, v.Aufbauten.Select(a => a.Gegenseite));
        }

        [Fact]
        public void Das_Probenhaus_IFC_traegt_U_Werte_ohne_Aufbau_und_die_Innenflaeche_als_Faktor()
        {
            GebaeudeBauteilvorschlag v = Vorschlag("ifc4_haus.ifc");
            Assert.False(v.Abgelehnt);
            Assert.Equal('E', v.Baualtersklasse);   // aus dem Baujahr 1965 der Datei (E47: 1958 bis 1968)
            Assert.Equal(DbWerte.HERKUNFT_IFC, v.Zone.Herkunft);
            Assert.Equal(130.0, v.Zone.Nutzflaeche);
            Assert.Empty(v.Aufbauten);
            Assert.Equal(17, v.Zeilen.Count);

            // Ohne Stoffwerte keine Aufbauten, nur der U-Wert der Datei (Herkunft IFC).
            Assert.All(v.Zeilen, x =>
            {
                Assert.NotNull(x.Summenfeld);
                Assert.Null(x.Bauteil.ID_Aufbau);
                Assert.True(x.Bauteil.U_Wert.HasValue);
                Assert.Equal(Importherkunft.Ifc, x.HerkunftU);
                Assert.Equal(DbWerte.HERKUNFT_IFC, x.Bauteil.Herkunft);
            });
            // Innenwand und Geschossdecke ohne Stoffwerte: keine Innenzeilen, sondern der
            // Innenflächenfaktor aus der Datei — beide Seiten, 2 × (20,8 + 80) m² ÷ 130 m².
            Assert.Equal(Innenweg.Innenflaechenfaktor, v.Innenweg);
            Assert.Equal(2, v.Innenflaechen);
            Assert.Equal(2, v.InnenflaechenUnvollstaendig);
            Nah(201.6, v.InnenflaecheDateiM2);
            Nah(201.6 / 130.0, v.Innenflaechenfaktor);
            Assert.Equal(Importherkunft.Ifc, v.HerkunftInnenflaechenfaktor);
            Assert.Equal(0.0, v.FlaecheInnen);
            PruefMeldung m = Assert.Single(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.INNEN_FAKTOR_STOFFWERTE);
            Assert.Equal(new[] { "1.5508", "201.6", "2", "2" }, m.Werte);

            // IFC2X3 liefert dasselbe Haus mit denselben Zeilen.
            GebaeudeBauteilvorschlag w = Vorschlag("ifc2x3_haus.ifc");
            Assert.False(w.Abgelehnt);
            Assert.Equal(v.Zeilen.Select(x => x.ToString()), w.Zeilen.Select(x => x.ToString()));
        }

        // =====================================================================
        //  Flächensummen je Gruppe gleich den Summenfeldern der Zuordnung
        // =====================================================================

        [Theory]
        [InlineData("gbxml_haus_si.xml", 0, null)]
        [InlineData("gbxml_haus_fuss.xml", 0, null)]
        [InlineData("gbxml_haus_utf16.xml", 0, null)]
        [InlineData("gbxml_ohne_konstruktionen.xml", 0, 'E')]
        [InlineData("gbxml_rwert_schicht.xml", 0, 'E')]
        [InlineData("gbxml_norddrehung.xml", 0, 'E')]
        [InlineData("gbxml_nachbar_leer.xml", 0, 'E')]
        [InlineData("gbxml_ohne_nachbar.xml", 0, 'E')]
        [InlineData("gbxml_kennung_lang.xml", 0, 'E')]
        [InlineData("gbxml_zwei_gebaeude.xml", 0, 'E')]
        [InlineData("gbxml_zwei_gebaeude.xml", 1, 'E')]
        [InlineData("gbxml_nettoflaeche_negativ.xml", 0, 'E')]
        [InlineData("gbxml_innenflaechen_teilweise.xml", 0, null)]
        [InlineData("ifc4_haus.ifc", 0, null)]
        [InlineData("ifc4_haus.ifczip", 0, null)]
        [InlineData("ifc2x3_haus.ifc", 0, null)]
        [InlineData("ifc4_mapconversion.ifc", 0, 'E')]
        [InlineData("ifc4_schichten.ifc", 0, 'E')]
        [InlineData("ifc4_schichten_nullwerte.ifc", 0, 'E')]
        [InlineData("ifc2x3_schichten.ifc", 0, 'E')]
        [InlineData("ifc4_rueckfaelle.ifc", 0, 'E')]
        [InlineData("ifc4_zwei_gebaeude.ifc", 0, 'E')]
        [InlineData("ifc4_zwei_gebaeude.ifc", 1, 'E')]
        [InlineData("ifc4_ohne_mengen.ifc", 0, 'E')]
        [InlineData("ifc4_vorhangfassade.ifc", 0, 'E')]
        public void Jede_Gruppe_summiert_die_Flaeche_des_Summenfelds(string name, int index, char? klasse)
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen(name);
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, index, klasse);
            GebaeudeImportSatz satz = a.Zuordnen(index, klasse);   // die Zuordnung des Einzonenwegs, unabhängig gebildet
            foreach (string feld in GebaeudeBauteilvorschlag.Summenfelder)
                Nah(satz.Zeile(feld).Wert ?? 0.0, v.Summe(feld));
            Assert.DoesNotContain(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.SUMME_ABWEICHUNG);

            // Die Zone trägt die Kenngrößen der Zuordnung.
            Assert.Equal(satz.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Wert, v.Zone.Nutzflaeche);
            Assert.Equal(satz.Zeile(GebaeudeZielfelder.RAUMHOEHE).Wert, v.Zone.Raumhoehe);
            Assert.Equal(satz.Zeile(GebaeudeZielfelder.VOLUMEN).Wert, v.Zone.Volumen);
        }

        // =====================================================================
        //  Fensterabzug
        // =====================================================================

        [Fact]
        public void Wandzeilen_tragen_die_Nettoflaeche_und_jede_Oeffnung_eine_eigene_Zeile()
        {
            GebaeudeBauteilvorschlag v = Vorschlag("gbxml_haus_si.xml");
            // 4 × 2,5 m² − Fenster 1 m² − Haustür 2 m².
            Nah(7.0, BauteilvorschlagProbe.Zeile(v, "aw-eg-nord-kueche").Bauteil.Flaeche);
            // 8 × 2,5 m² − 2 × 3 m².
            Nah(14.0, BauteilvorschlagProbe.Zeile(v, "aw-eg-sued-wohnen").Bauteil.Flaeche);
            // Die Wand aus dem PolyLoop: 5 × 2,5 m² − 1 m².
            Nah(11.5, BauteilvorschlagProbe.Zeile(v, "aw-og-ost").Bauteil.Flaeche);

            GebaeudeBauteilzeile fenster = BauteilvorschlagProbe.Zeile(v, "fenster-eg-s1");
            Nah(3.0, fenster.Bauteil.Flaeche);
            Assert.Equal(GebaeudeZielfelder.FENSTER_GESAMT, fenster.Summenfeld);
            Assert.Equal(1.1, fenster.Bauteil.U_Wert);
            Assert.Equal(0.6, fenster.Bauteil.g_Wert);   // der Wert bei 0°, nicht der bei 60°
            Assert.Null(fenster.Bauteil.Rahmenanteil);
            Assert.Null(fenster.Bauteil.ID_Aufbau);

            GebaeudeBauteilzeile tuer = BauteilvorschlagProbe.Zeile(v, "tuer-eg-nord");
            Assert.Equal(DbWerte.BAUTEILART_TUER, tuer.Bauteil.Bauteilart);
            Nah(2.0, tuer.Bauteil.Flaeche);
            Assert.Equal(1.8, tuer.Bauteil.U_Wert);
            Assert.Equal(GebaeudeZielfelder.FLAECHE_SONSTIGE, tuer.Summenfeld);
            Assert.Equal(0.0, tuer.Bauteil.Azimut);   // der Azimut der Wand

            // Brutto = Netto + Fenster + Tür: der Bruttowert, den die Zuordnung mitführt.
            GebaeudeFeldzeile brutto = v.Satz.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND);
            Nah(170.0, brutto.Bruttowert);
            Nah(brutto.Bruttowert.Value, v.Summe(GebaeudeZielfelder.FLAECHE_AUSSENWAND) + v.Summe(GebaeudeZielfelder.FENSTER_GESAMT) + tuer.Bauteil.Flaeche);
        }

        // =====================================================================
        //  Azimut samt Nordwinkel
        // =====================================================================

        [Fact]
        public void Beim_gbXML_Weg_steht_der_Azimut_der_Datei_und_die_Norddrehung_daneben()
        {
            GebaeudeBauteilvorschlag v = Vorschlag("gbxml_norddrehung.xml", 0, 'E');
            Assert.False(v.Abgelehnt);
            Assert.Equal(60.0, v.NordwinkelGrad);
            Assert.False(v.NordwinkelAngewandt);
            Assert.Equal(180.0, BauteilvorschlagProbe.Zeile(v, "aw-sued").Bauteil.Azimut);   // nicht 240°
            Assert.Equal(0.0, BauteilvorschlagProbe.Zeile(v, "aw-nord").Bauteil.Azimut);
            Assert.Equal(180.0, BauteilvorschlagProbe.Zeile(v, "fenster-sued").Bauteil.Azimut);
            PruefMeldung m = Assert.Single(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.NORDWINKEL_NICHT_ANGEWANDT);
            Assert.Equal(new[] { "60" }, m.Werte);
        }

        [Fact]
        public void Beim_IFC_Weg_tragen_die_Azimute_die_Drehung_der_Datei()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen("ifc4_mapconversion.ifc");
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, 'E');
            Assert.False(v.Abgelehnt);
            Assert.True(v.NordwinkelAngewandt);
            Assert.Equal(90.0, v.NordwinkelGrad);
            // Die Nordwand der Datei liegt nach der Drehung der IfcMapConversion im Westen.
            GebaeudeBauteilzeile wand = Assert.Single(v.Zeilen, x => x.Bauteil.Bauteilart == DbWerte.BAUTEILART_AUSSENWAND);
            AbbildBauteil quelle = a.Abbild.Gebaeude[0].Bauteile.Single(b => b.Kennung == wand.Kennung);
            Assert.Equal(quelle.AzimutGrad, wand.Bauteil.Azimut);
            Nah(270.0, wand.Bauteil.Azimut);
            Nah(270.0, Assert.Single(v.Zeilen, x => x.Bauteil.Bauteilart == DbWerte.BAUTEILART_FENSTER).Bauteil.Azimut);
            Assert.Contains(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.NORDWINKEL_ANGEWANDT && x.Werte[0] == "90");
        }

        // =====================================================================
        //  Schichtfolge innen → außen
        // =====================================================================

        [Fact]
        public void Die_Schichten_laufen_innen_nach_aussen_je_Seite()
        {
            GebaeudeBauteilvorschlag v = Vorschlag("gbxml_haus_si.xml");

            // Außenwand: die Datei nennt den Außenputz zuerst (Annahme „erste Schicht außen").
            BauteilaufbauModel aw = BauteilvorschlagProbe.Aufbau(v, BauteilvorschlagProbe.Zeile(v, "aw-og-nord"));
            Assert.Equal(new[] { 1400.0, 100.0, 1600.0, 1800.0 }, aw.Schichten.Select(s => s.Rho.Value));   // Innenputz … Außenputz
            Assert.Equal(new[] { 1, 2, 3, 4 }, aw.Schichten.Select(s => s.Reihenfolge));
            Assert.Equal(new[] { 0.01, 0.08, 0.24, 0.02 }, aw.Schichten.Select(s => s.Dicke));
            Assert.All(aw.Schichten, s => { Assert.False(s.IstLuftschicht); Assert.Null(s.ID_Baustoff); });

            // Kellerdecke aus Sicht des Wohnraums darüber: Estrich innen, Dämmung zum Keller.
            BauteilaufbauModel kd = BauteilvorschlagProbe.Aufbau(v, BauteilvorschlagProbe.Zeile(v, "decke-kg-eg-wohnen"));
            Assert.Equal(new[] { 2000.0, 2400.0, 30.0 }, kd.Schichten.Select(s => s.Rho.Value));

            // Geschossdecke: von unten (Seite A, Wohnen) der Beton, von oben (Seite B, Schlafen) der Estrich.
            GebaeudeBauteilzeile a = BauteilvorschlagProbe.Zeile(v, "decke-eg-og-wohnen", "A");
            GebaeudeBauteilzeile b = BauteilvorschlagProbe.Zeile(v, "decke-eg-og-wohnen", "B");
            Assert.Equal(new[] { 2400.0, 2000.0 }, BauteilvorschlagProbe.Aufbau(v, a).Schichten.Select(s => s.Rho.Value));
            Assert.Equal(new[] { 2000.0, 2400.0 }, BauteilvorschlagProbe.Aufbau(v, b).Schichten.Select(s => s.Rho.Value));
            Assert.Equal(0.0, a.Bauteil.Neigung);     // Decke des unteren Raums
            Assert.Equal(180.0, b.Bauteil.Neigung);   // Boden des oberen Raums

            // Die symmetrische Innenwand: ein Aufbau für beide Seiten.
            Assert.Equal(BauteilvorschlagProbe.Zeile(v, "iw-eg", "A").Bauteil.ID_Aufbau, BauteilvorschlagProbe.Zeile(v, "iw-eg", "B").Bauteil.ID_Aufbau);
        }

        [Fact]
        public void Beim_IFC_Weg_entscheidet_die_Schichtnutzung_ueber_innen()
        {
            GebaeudeBauteilvorschlag v = Vorschlag("ifc4_schichten.ifc", 0, 'E');
            Assert.False(v.Abgelehnt);
            // Zwei Wandtypen, einer innen zuerst, einer außen zuerst gezählt: beide Aufbauten beginnen
            // raumseitig mit dem Putz und enden mit der Dämmung.
            GebaeudeAufbauzeile[] waende = v.Aufbauten.Where(x => x.Aufbau.Bauteilart == DbWerte.BAUTEILART_AUSSENWAND).ToArray();
            Assert.Equal(2, waende.Length);
            Assert.All(waende, x => Assert.Equal(new[] { 1400.0, 1200.0, 30.0 }, x.Aufbau.Schichten.Select(s => s.Rho.Value)));
            Assert.All(waende, x => Assert.False(x.RichtungAngenommen));
            Assert.All(v.Aufbauten, x => Assert.Equal("IfcMaterialLayerSet", x.Quelltyp));
            Assert.All(v.Aufbauten, x => Assert.Equal(DbWerte.HERKUNFT_IFC, x.Aufbau.Herkunft));
            // Das Dach: der Beton raumseitig.
            GebaeudeAufbauzeile dach = Assert.Single(v.Aufbauten, x => x.Aufbau.Bauteilart == DbWerte.BAUTEILART_DACH);
            Assert.Equal(new[] { 2400.0, 30.0 }, dach.Aufbau.Schichten.Select(s => s.Rho.Value));
            // Der U-Wert folgt aus den Schichten; die Zeile trägt keinen.
            Assert.All(v.Zeilen.Where(x => x.Bauteil.ID_Aufbau.HasValue), x => { Assert.Null(x.Bauteil.U_Wert); Assert.True(x.USchichten > 0.0); });
        }

        // =====================================================================
        //  Rückfälle
        // =====================================================================

        [Fact]
        public void Ohne_Konstruktionen_gilt_die_Vorgabe_der_Baualtersklasse()
        {
            GebaeudeBauteilvorschlag v = Vorschlag("gbxml_ohne_konstruktionen.xml", 0, 'E');
            Assert.False(v.Abgelehnt);
            Assert.Empty(v.Aufbauten);
            Baualtersvorgabe e = GebaeudeVorgaben.Fuer('E');
            Assert.All(v.Zeilen, x =>
            {
                Assert.Equal(Importherkunft.Vorgabe, x.HerkunftU);
                Assert.Equal(DbWerte.HERKUNFT_VORGABE, x.Bauteil.Herkunft);
                Assert.Equal(Importherkunft.GbXml, x.HerkunftFlaeche);
            });
            Assert.Equal(e.UAussenwand, BauteilvorschlagProbe.Zeile(v, "aw-nord").Bauteil.U_Wert);
            Assert.Equal(e.UFenster, BauteilvorschlagProbe.Zeile(v, "fenster-sued").Bauteil.U_Wert);
            Assert.Equal(e.UDach, BauteilvorschlagProbe.Zeile(v, "dach").Bauteil.U_Wert);
            Assert.Equal(e.UGrund, BauteilvorschlagProbe.Zeile(v, "bodenplatte").Bauteil.U_Wert);
            Assert.Null(BauteilvorschlagProbe.Zeile(v, "fenster-sued").Bauteil.g_Wert);   // Wert des Gebäudes
            Assert.Contains(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.U_VORGABE && m.Werte[0] == "8" && m.Werte[1] == "E");

            // Ohne Baualtersklasse gibt es keine Vorgabe: benannt abgelehnt.
            GebaeudeBauteilvorschlag ohne = Vorschlag("gbxml_ohne_konstruktionen.xml");
            Assert.True(ohne.Abgelehnt);
            PruefMeldung f = Assert.Single(ohne.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            Assert.Equal(GebaeudeBauteilvorschlag.UWERT_FEHLT, f.Schluessel);
            Assert.Equal("8", f.Werte[0]);
        }

        /// <summary>
        /// E51: Ein Neubau der Klasse M ohne U-Werte und ohne Schichten wird NICHT mehr abgelehnt — die
        /// Klasse hat Katalogsätze. Ohne Katalogsatz (Lesenaht) trägt er den freien Wert nach Stein/Loga,
        /// mit eigener Herkunft und eigener Meldung, gespeichert als VORGABE.
        /// </summary>
        [Fact]
        public void Ein_Neubau_der_Klasse_M_ohne_Konstruktionen_bekommt_die_Vorgabe()
        {
            GebaeudeBauteilvorschlag v = Vorschlag("gbxml_ohne_konstruktionen.xml", 0, 'M');
            Assert.False(v.Abgelehnt);
            Assert.DoesNotContain(v.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            Baualtersvorgabe m = GebaeudeVorgaben.Fuer('M');
            Assert.All(v.Zeilen, x =>
            {
                Assert.Equal(Importherkunft.Vorgabe, x.HerkunftU);
                Assert.Equal(DbWerte.HERKUNFT_VORGABE, x.Bauteil.Herkunft);
            });
            Assert.Equal(m.UAussenwand, BauteilvorschlagProbe.Zeile(v, "aw-nord").Bauteil.U_Wert);
            Assert.Equal(m.UFenster, BauteilvorschlagProbe.Zeile(v, "fenster-sued").Bauteil.U_Wert);
            Assert.Contains(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.U_VORGABE && x.Werte[0] == "8" && x.Werte[1] == "M");
            Assert.DoesNotContain(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.U_VORGABE_FREI);

            // Der ruhende Rückfall: ohne Katalogsatz der freie Wert der Klasse, nie abgelehnt.
            GebaeudeBauteilvorschlag frei;
            using (GebaeudeVorgaben.KatalogOhne("M"))
                frei = Vorschlag("gbxml_ohne_konstruktionen.xml", 0, 'M');
            Assert.False(frei.Abgelehnt);
            Baualtersvorgabe f = GebaeudeVorgaben.Frei('M');
            Assert.All(frei.Zeilen, x =>
            {
                Assert.Equal(Importherkunft.VorgabeFrei, x.HerkunftU);
                Assert.Equal(DbWerte.HERKUNFT_VORGABE, x.Bauteil.Herkunft);   // kein neuer Datenbankwert
            });
            Assert.Equal(f.UAussenwand, BauteilvorschlagProbe.Zeile(frei, "aw-nord").Bauteil.U_Wert);
            Assert.Equal(f.UDach, BauteilvorschlagProbe.Zeile(frei, "dach").Bauteil.U_Wert);
            PruefMeldung info = Assert.Single(frei.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.U_VORGABE_FREI);
            Assert.Equal(PruefStufe.Info, info.Stufe);
            Assert.Equal(new[] { "8", "M", "2021–2025" }, info.Werte);
            Assert.DoesNotContain(frei.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.U_VORGABE);
        }

        [Fact]
        public void Ohne_vollstaendige_Stoffwerte_gibt_es_keinen_Aufbau_sondern_den_U_Wert()
        {
            // IFC4: die Dämmung mit ρ = 0 und c = 0 — der U-Wert aus der masselosen Schichtung.
            GebaeudeBauteilvorschlag n = Vorschlag("ifc4_schichten_nullwerte.ifc", 0, 'E');
            GebaeudeBauteilvorschlag s = Vorschlag("ifc4_schichten.ifc", 0, 'E');
            Assert.False(n.Abgelehnt);
            Assert.Empty(n.Aufbauten);
            Assert.Equal(4, n.Meldungen.Count(m => m.Schluessel == GebaeudeBauteilvorschlag.STOFFWERTE_UNVOLLSTAENDIG && m.Stufe == PruefStufe.Warnung));
            foreach (GebaeudeBauteilzeile x in n.Zeilen.Where(z => z.Bauteil.Bauteilart != DbWerte.BAUTEILART_FENSTER))
            {
                Assert.Null(x.Bauteil.ID_Aufbau);
                Assert.Equal(Importherkunft.Ifc, x.HerkunftU);
                Assert.Equal(DbWerte.HERKUNFT_IFC, x.Bauteil.Herkunft);
                // Derselbe U-Wert wie aus den vollständigen Schichten des Schichtenhauses.
                Nah(s.Zeilen.Single(z => z.Bauteil.Bezeichner == x.Bauteil.Bezeichner).USchichten.Value, x.Bauteil.U_Wert, 1e-12);
            }

            // IFC2X3: die Stoffwerte werden nicht gelesen — Vorgabe der Klasse, ohne Klasse abgelehnt.
            GebaeudeBauteilvorschlag x3 = Vorschlag("ifc2x3_schichten.ifc", 0, 'E');
            Assert.False(x3.Abgelehnt);
            Assert.Empty(x3.Aufbauten);
            Assert.Equal(GebaeudeVorgaben.Fuer('E').UAussenwand, x3.Zeilen.First(z => z.Bauteil.Bauteilart == DbWerte.BAUTEILART_AUSSENWAND).Bauteil.U_Wert);
            Assert.Equal(DbWerte.HERKUNFT_VORGABE, x3.Zeilen.First(z => z.Bauteil.Bauteilart == DbWerte.BAUTEILART_AUSSENWAND).Bauteil.Herkunft);
            Assert.True(Vorschlag("ifc2x3_schichten.ifc").Abgelehnt);

            // gbXML: eine Schicht nur mit R-Wert — U = 1/(0,13 + 0,24/0,8 + 0,18 + 0,04).
            GebaeudeBauteilvorschlag r = Vorschlag("gbxml_rwert_schicht.xml", 0, 'E');
            Assert.False(r.Abgelehnt);
            Assert.Empty(r.Aufbauten);
            GebaeudeBauteilzeile wand = BauteilvorschlagProbe.Zeile(r, "aw-nord");
            Nah(1.0 / (0.13 + 0.3 + 0.18 + 0.04), wand.Bauteil.U_Wert);
            Assert.Equal(DbWerte.HERKUNFT_GBXML, wand.Bauteil.Herkunft);
            Assert.Contains(r.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.STOFFWERTE_UNVOLLSTAENDIG && m.Werte[0] == "kon-masselos");
        }

        [Fact]
        public void Eine_Flaechenvorgabe_der_Zuordnung_traegt_das_eine_Bauteil_oder_eine_Vorgabezeile()
        {
            // Dach- und Bodenplatte ohne Mengen: das eine Bauteil der Gruppe trägt die Vorgabe.
            GebaeudeBauteilvorschlag v = Vorschlag("ifc4_rueckfaelle.ifc", 0, 'E');
            Assert.False(v.Abgelehnt);
            GebaeudeBauteilzeile dach = Assert.Single(v.Zeilen, x => x.Summenfeld == GebaeudeZielfelder.FLAECHE_DACH);
            Assert.NotNull(dach.Kennung);
            Nah(70.0, dach.Bauteil.Flaeche);
            Assert.Equal(Importherkunft.Vorgabe, dach.HerkunftFlaeche);
            Assert.Equal(Importherkunft.Ifc, dach.HerkunftU);
            Assert.Equal(DbWerte.HERKUNFT_VORGABE, dach.Bauteil.Herkunft);
            Nah(55.0, Assert.Single(v.Zeilen, x => x.Summenfeld == GebaeudeZielfelder.FLAECHE_GRUND).Bauteil.Flaeche);
            Assert.Equal(2, v.Meldungen.Count(m => m.Schluessel == GebaeudeBauteilvorschlag.FLAECHE_VORGABE));
            Assert.Null(v.Zone.Volumen);   // Räume ohne Volumen
            Assert.Equal(Importherkunft.Vorgabe, v.HerkunftRaumhoehe);

            // Ohne Dach und Bodenplatte in der Datei: je eine Vorgabezeile ohne Quellentität.
            GebaeudeBauteilvorschlag m = Vorschlag("ifc4_mapconversion.ifc", 0, 'E');
            GebaeudeBauteilzeile vd = Assert.Single(m.Zeilen, x => x.Summenfeld == GebaeudeZielfelder.FLAECHE_DACH);
            Assert.Equal(GebaeudeBauteilvorschlag.VORGABEZEILE_DACH, vd.Bauteil.Bezeichner);
            Assert.Null(vd.Kennung);
            Assert.Null(vd.Quelltyp);
            Assert.Null(vd.Bauteil.Quellkennung);
            Assert.Equal(DbWerte.HERKUNFT_VORGABE, vd.Bauteil.Herkunft);
            Assert.Equal(GebaeudeVorgaben.Fuer('E').UDach, vd.Bauteil.U_Wert);
            GebaeudeBauteilzeile vg = Assert.Single(m.Zeilen, x => x.Summenfeld == GebaeudeZielfelder.FLAECHE_GRUND);
            Assert.Equal(GebaeudeBauteilvorschlag.VORGABEZEILE_GRUND, vg.Bauteil.Bezeichner);
            Assert.Equal(DbWerte.RANDBEDINGUNG_ERDREICH, vg.Bauteil.Randbedingung);
        }

        // =====================================================================
        //  Herkunft
        // =====================================================================

        [Fact]
        public void Herkunft_je_Zeile_und_je_Wert()
        {
            GebaeudeBauteilvorschlag v = Vorschlag("gbxml_haus_si.xml");
            Assert.All(v.Zeilen, x => Assert.Equal(DbWerte.HERKUNFT_GBXML, x.Bauteil.Herkunft));
            GebaeudeBauteilzeile wand = BauteilvorschlagProbe.Zeile(v, "aw-og-nord");
            Assert.Equal(Importherkunft.GbXml, wand.HerkunftFlaeche);
            Assert.Equal(Importherkunft.GbXml, wand.HerkunftU);
            Assert.Equal(Importherkunft.GbXml, wand.HerkunftAufbau);
            Assert.Equal(Importherkunft.GbXml, wand.HerkunftAzimut);
            Assert.Equal(Importherkunft.GbXml, wand.HerkunftNeigung);
            Assert.Equal(Importherkunft.Leer, wand.HerkunftG);
            GebaeudeBauteilzeile fenster = BauteilvorschlagProbe.Zeile(v, "fenster-og-n1");
            Assert.Equal(Importherkunft.GbXml, fenster.HerkunftG);
            Assert.Equal(Importherkunft.Leer, fenster.HerkunftAufbau);
            GebaeudeBauteilzeile innen = BauteilvorschlagProbe.Zeile(v, "iw-eg", "A");
            Assert.Equal(Importherkunft.Leer, innen.HerkunftAzimut);
        }

        [Fact]
        public void Eine_lange_Kennung_wird_fuer_die_Zeile_gekuerzt()
        {
            GebaeudeBauteilvorschlag v = Vorschlag("gbxml_kennung_lang.xml", 0, 'E');
            GebaeudeBauteilzeile z = Assert.Single(v.Zeilen, x => x.Kennung.Length > 64);
            Assert.Equal(80, z.Kennung.Length);
            Assert.Equal(Quellkennung.Kuerzen(z.Kennung), z.Bauteil.Quellkennung);
            Assert.Equal(64, z.Bauteil.Quellkennung.Length);
            Assert.True(z.Bauteil.Bezeichner.Length <= 80);
        }

        // =====================================================================
        //  Benannte Ablehnungen
        // =====================================================================

        [Fact]
        public void Eine_negative_Nettoflaeche_wird_benannt_abgelehnt()
        {
            GebaeudeBauteilvorschlag v = Vorschlag("gbxml_nettoflaeche_negativ.xml", 0, 'E');
            Assert.True(v.Abgelehnt);
            PruefMeldung f = Assert.Single(v.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            Assert.Equal(GbxmlImportProfil.MELDUNGSPRAEFIX + "NETTOFLAECHE_NEGATIV", f.Schluessel);
            Assert.Equal("aw-sued", f.Werte[0]);
            Assert.DoesNotContain(v.Zeilen, x => x.Kennung == "aw-sued");   // Fläche 0: keine Zeile
            Assert.Contains(v.Zeilen, x => x.Kennung == "fenster-gross");
        }

        [Fact]
        public void Was_sich_nicht_abbilden_laesst_wird_benannt_abgelehnt()
        {
            // Kein Gebäude unter dem Index — auch nicht bei zwei Gebäuden in der Datei.
            GebaeudeImportAblauf zwei = BauteilvorschlagProbe.Lesen("gbxml_zwei_gebaeude.xml");
            GebaeudeBauteilvorschlag k = GebaeudeBauteilvorschlag.Bilden(zwei, 2, 'E');
            Assert.True(k.Abgelehnt);
            Assert.Null(k.Zone);
            Assert.Equal(new[] { GebaeudeBauteilvorschlag.KEIN_GEBAEUDE }, BauteilvorschlagProbe.Schluessel(k, PruefStufe.Fehler));
            Assert.Equal(new[] { "2", "2" }, k.Meldungen[0].Werte);
            Assert.True(GebaeudeBauteilvorschlag.Bilden((GebaeudeAbbild)null, 0, null, null, new GbxmlImportProfil()).Abgelehnt);
            // Beide Gebäude einzeln: je ihre eigene Zone.
            Assert.Equal("Haus A", GebaeudeBauteilvorschlag.Bilden(zwei, 0, 'E').Zone.Bezeichner);
            Assert.Equal("Haus B", GebaeudeBauteilvorschlag.Bilden(zwei, 1, 'E').Zone.Bezeichner);

            // Kein beheizter Raum (Haken der Raumliste).
            var aus = new Dictionary<string, bool> { ["raum-1"] = false };
            GebaeudeBauteilvorschlag kalt = BauteilvorschlagProbe.Vorschlag("gbxml_ohne_konstruktionen.xml", 0, 'E', aus);
            Assert.True(kalt.Abgelehnt);
            Assert.Equal(new[] { GebaeudeBauteilvorschlag.KEINE_BEHEIZTEN_RAEUME }, BauteilvorschlagProbe.Schluessel(kalt, PruefStufe.Fehler));

            // Keine Fläche mit Mengen: kein Außenbauteil.
            GebaeudeBauteilvorschlag leer = Vorschlag("ifc4_ohne_mengen.ifc", 0, 'E');
            Assert.True(leer.Abgelehnt);
            Assert.Contains(GebaeudeBauteilvorschlag.KEINE_AUSSENBAUTEILE, BauteilvorschlagProbe.Schluessel(leer, PruefStufe.Fehler));
            Assert.Contains(leer.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.OHNE_FLAECHE && m.Werte[0] == "4");

            // Eine Fläche ohne Nachbarraum zählt nirgends — benannt, ohne Absturz.
            GebaeudeBauteilvorschlag ohne = Vorschlag("gbxml_ohne_nachbar.xml", 0, 'E');
            Assert.False(ohne.Abgelehnt);
            Assert.DoesNotContain(ohne.Zeilen, x => x.Kennung == "aw-ohne-raum");
            PruefMeldung w = Assert.Single(ohne.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.OHNE_NACHBAR);
            Assert.Equal(PruefStufe.Warnung, w.Stufe);
            Assert.Equal(new[] { "1", "aw-ohne-raum" }, w.Werte);
        }

        [Fact]
        public void Eine_Aussenwand_ohne_Azimut_wird_benannt_abgelehnt_ein_waagerechtes_Dach_nicht()
        {
            GbxmlAbbild a = BauteilvorschlagProbe.Synthetisch();
            a.Gebaeude[0].Bauteile.Add(BauteilvorschlagProbe.Flaeche("wand-ohne-azimut", Bauteilart.Aussenwand, Randbedingung.Aussenluft,
                                                                     25, 0.3, 90, null, "R1"));
            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Bilden(a);
            Assert.True(v.Abgelehnt);
            PruefMeldung f = Assert.Single(v.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            Assert.Equal(GebaeudeBauteilvorschlag.AZIMUT_FEHLT, f.Schluessel);
            Assert.Equal(new[] { "1", "wand-ohne-azimut" }, f.Werte);
            // Das Dach (0°) und die Bodenplatte (180°) brauchen keinen.
            Assert.Null(BauteilvorschlagProbe.Zeile(v, "dach").Bauteil.Azimut);

            // Mit Azimut ist derselbe Vorschlag gültig.
            a.Gebaeude[0].Bauteile[2].AzimutGrad = 90;
            Assert.False(BauteilvorschlagProbe.Bilden(a).Abgelehnt);
        }

        [Fact]
        public void Ein_U_Wert_neben_Schichten_wird_verglichen_es_rechnet_der_Aufbau()
        {
            GbxmlAbbild a = BauteilvorschlagProbe.Synthetisch();
            // Eine Wand: 1 cm Putz + 24 cm Mauerwerk; U aus Schichten 1/(0,13 + 0,01 + 0,3 + 0,04) = 2,0833.
            var aufbau = new AbbildAufbau { Kennung = "kon-1", Name = "Wand", Status = Aufbaustatus.Vollstaendig, UWertWm2K = 2.5 };
            aufbau.Schichten.Add(new AbbildSchicht { DickeM = 0.24, LambdaWmK = 0.8, RhoKgM3 = 1600, CpJkgK = 1000 });
            aufbau.Schichten.Add(new AbbildSchicht { DickeM = 0.01, LambdaWmK = 1.0, RhoKgM3 = 1400, CpJkgK = 1000 });
            AbbildBauteil wand = BauteilvorschlagProbe.Flaeche("wand", Bauteilart.Aussenwand, Randbedingung.Aussenluft, 25, 2.5, 90, 180, "R1");
            wand.Aufbau = aufbau;
            a.Gebaeude[0].Bauteile.Add(wand);

            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Bilden(a);
            Assert.False(v.Abgelehnt);
            GebaeudeBauteilzeile z = BauteilvorschlagProbe.Zeile(v, "wand");
            Assert.Null(z.Bauteil.U_Wert);                 // der Bauteilweg rechnet aus den Schichten
            Assert.Equal(2.5, z.UDatei);
            Nah(1.0 / (0.13 + 0.01 + 0.3 + 0.04), z.USchichten, 1e-12);
            PruefMeldung w = Assert.Single(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.U_ABWEICHUNG);
            Assert.Equal(PruefStufe.Warnung, w.Stufe);
            Assert.Equal("kon-1", w.Werte[0]);
            Assert.Equal("20", w.Werte[3]);   // (2,5 / 2,0833 − 1) · 100 = 20 %

            // Innerhalb von 5 % keine Meldung.
            aufbau.UWertWm2K = 2.1;
            wand.UWertWm2K = 2.1;
            Assert.DoesNotContain(BauteilvorschlagProbe.Bilden(a).Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.U_ABWEICHUNG);
        }

        [Fact]
        public void Gegen_unbeheizte_Raeume_gilt_die_Randbedingung_unbeheizt_die_Flaeche_zaehlt_wie_im_Einzonenweg()
        {
            // Ein Nachbar, den es nicht gibt, gilt als unbeheizt: Innenwand → Sonstige, Randbedingung unbeheizt.
            GebaeudeBauteilvorschlag v = Vorschlag("gbxml_nachbar_leer.xml", 0, 'E');
            GebaeudeBauteilzeile iw = BauteilvorschlagProbe.Zeile(v, "iw-ost");
            Assert.Equal(DbWerte.BAUTEILART_INNENWAND, iw.Bauteil.Bauteilart);
            Assert.Equal(DbWerte.RANDBEDINGUNG_UNBEHEIZT, iw.Bauteil.Randbedingung);
            Assert.Equal(GebaeudeZielfelder.FLAECHE_SONSTIGE, iw.Summenfeld);
            Assert.Equal(GebaeudeVorgaben.Fuer('E').USonstige, iw.Bauteil.U_Wert);

            // Die Haken der Raumliste: die Küche als unbeheizt — die Innenwand wird Hülle, Summen wie die Zuordnung.
            var ueb = new Dictionary<string, bool> { ["raum-kueche"] = false };
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen("gbxml_haus_si.xml");
            GebaeudeBauteilvorschlag w = GebaeudeBauteilvorschlag.Bilden(a, 0, null, ueb);
            Assert.False(w.Abgelehnt, string.Join(" | ", w.Meldungen));
            GebaeudeBauteilzeile wand = BauteilvorschlagProbe.Zeile(w, "iw-eg");
            Assert.Equal(DbWerte.RANDBEDINGUNG_UNBEHEIZT, wand.Bauteil.Randbedingung);
            Assert.Equal(GebaeudeZielfelder.FLAECHE_SONSTIGE, wand.Summenfeld);
            Assert.NotNull(wand.Bauteil.ID_Aufbau);
            GebaeudeImportSatz satz = a.Zuordnen(0, null, ueb);
            foreach (string feld in GebaeudeBauteilvorschlag.Summenfelder)
                Nah(satz.Zeile(feld).Wert ?? 0.0, w.Summe(feld));
            Assert.Equal(new[] { "raum-wohnen", "raum-schlafen" }, w.Raeume.Select(r => r.Quellkennung));
        }

        // =====================================================================
        //  Vorhangfassaden — transparent (Anwenderentscheid)
        // =====================================================================

        /// <summary>
        /// <b>Eine Vorhangfassade rechnet transparent</b>: eine Zeile der Art <c>VORHANGFASSADE</c> mit dem
        /// U-Wert der Datei, g, Rahmenanteil und Verschattung leer; im Summenfeld steht sie wie im
        /// Einzonenweg unter „Sonstige Flächen", und die Summenprobe hält Sonstige samt Fassade dagegen.
        /// </summary>
        [Fact]
        public void Eine_Vorhangfassade_rechnet_transparent_und_zaehlt_im_Summenfeld_Sonstige()
        {
            GbxmlAbbild a = BauteilvorschlagProbe.Synthetisch();
            a.Gebaeude[0].Bauteile.Add(BauteilvorschlagProbe.Flaeche("wand", Bauteilart.Aussenwand, Randbedingung.Aussenluft,
                                                                     30, 0.3, 90, 0, "R1"));
            a.Gebaeude[0].Bauteile.Add(BauteilvorschlagProbe.Flaeche("fassade", Bauteilart.Vorhangfassade, Randbedingung.Aussenluft,
                                                                     20, 1.4, 90, 180, "R1"));
            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Bilden(a);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen));

            GebaeudeBauteilzeile z = BauteilvorschlagProbe.Zeile(v, "fassade");
            Assert.Equal(DbWerte.BAUTEILART_VORHANGFASSADE, z.Bauteil.Bauteilart);
            Assert.Equal(DbWerte.RANDBEDINGUNG_AUSSENLUFT, z.Bauteil.Randbedingung);
            Assert.Equal(GebaeudeZielfelder.FLAECHE_SONSTIGE, z.Summenfeld);
            Assert.Equal(20.0, z.Bauteil.Flaeche);
            Assert.Equal(1.4, z.Bauteil.U_Wert);
            Assert.Equal(Importherkunft.GbXml, z.HerkunftU);
            Assert.Null(z.Bauteil.g_Wert);
            Assert.Equal(Importherkunft.Leer, z.HerkunftG);
            Assert.Null(z.Bauteil.Rahmenanteil);
            Assert.Null(z.Bauteil.Verschattungsfaktor);
            Assert.Null(z.Bauteil.ID_Aufbau);
            Assert.Equal(180.0, z.Bauteil.Azimut);
            Assert.Equal(DbWerte.HERKUNFT_GBXML, z.Bauteil.Herkunft);

            PruefMeldung m = Assert.Single(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.VORHANGFASSADE);
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "1" }, m.Werte);

            // Die Summenfelder bleiben die des Einzonenwegs: Sonstige samt Fassade gegen das Feld Sonstige.
            GebaeudeImportSatz satz = v.Satz;
            Assert.Equal(20.0, satz.Zeile(GebaeudeZielfelder.FLAECHE_SONSTIGE).Wert);
            foreach (string feld in GebaeudeBauteilvorschlag.Summenfelder)
                Nah(satz.Zeile(feld).Wert ?? 0.0, v.Summe(feld));
        }

        /// <summary>
        /// <b>Was „leer" an einer Vorhangfassade heißt</b> (<see cref="BauteilEingang.MitGebaeudewerten"/>,
        /// <see cref="GebaeudeModellEingang"/>): g nimmt den Wert des Gebäudes, Rahmenanteil und Verschattung
        /// die des Gebäudes bzw. ohne sie die Vorgaben; die Zeile rechnet im Fensterzweig.
        /// </summary>
        [Fact]
        public void Eine_leere_Vorhangfassade_nimmt_g_des_Gebaeudes_und_die_Vorgaben_fuer_Rahmen_und_Verschattung()
        {
            GbxmlAbbild a = BauteilvorschlagProbe.Synthetisch();
            a.Gebaeude[0].Bauteile.Add(BauteilvorschlagProbe.Flaeche("wand", Bauteilart.Aussenwand, Randbedingung.Aussenluft,
                                                                     30, 0.3, 90, 0, "R1"));
            a.Gebaeude[0].Bauteile.Add(BauteilvorschlagProbe.Flaeche("fassade", Bauteilart.Vorhangfassade, Randbedingung.Aussenluft,
                                                                     20, 1.4, 90, 180, "R1"));
            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Bilden(a);
            Assert.False(v.Abgelehnt);

            // Die Zeile bildet sich leer ab (NaN = Vorgabe) …
            GebaeudeZonensatz satz = GebaeudeZonenabbildung.AlsZonensatz(v.Zone, v.AufbautenJeId);
            BauteilEingang roh = Assert.Single(satz.Bauteile, b => b.Art == Bauteilart.Vorhangfassade);
            Assert.True(double.IsNaN(roh.GWert));
            Assert.True(double.IsNaN(roh.Rahmenanteil));
            Assert.True(double.IsNaN(roh.Verschattungsfaktor));

            // … und der Eingangsbauer füllt sie mit den Werten des Gebäudes (g 0,75 der Probe; Rahmenanteil
            // und Verschattung trägt das Probengebäude nicht — dann die Vorgaben).
            Parameter(v, out GebaeudeModellEingang e);
            Assert.True(e.Bauteilweg);
            BauteilEingang b = Assert.Single(e.Bauteile, x => x.Art == Bauteilart.Vorhangfassade);
            Assert.True(b.IstTransparent);
            Assert.Equal(Bauteilgruppe.Fenster, b.Gruppe);
            Assert.Equal(0.75, b.GWert);
            Assert.Equal(GebaeudeFestwerte.VORGABE_RAHMENANTEIL, b.Rahmenanteil);
            Assert.Equal(GebaeudeFestwerte.VORGABE_VERSCHATTUNGSFAKTOR, b.Verschattungsfaktor);
            Assert.Equal(1.4, b.UWert_WM2K);
        }

        /// <summary>
        /// <b>Die Probe mit Vorhangfassaden</b> (<c>ifc4_vorhangfassade.ifc</c>): Süd mit U-Wert aus der Datei,
        /// West ohne — mit Baualtersklasse die Fenstervorgabe (Herkunft VORGABE), ohne Klasse die benannte
        /// Ablehnung. Die Summen bleiben die des Einzonenwegs.
        /// </summary>
        [Fact]
        public void Die_Probe_mit_Vorhangfassaden_nimmt_U_aus_der_Datei_sonst_die_Fenstervorgabe()
        {
            GebaeudeBauteilvorschlag v = Vorschlag("ifc4_vorhangfassade.ifc", 0, 'E');
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen));

            List<GebaeudeBauteilzeile> fassaden = v.Zeilen.Where(z => z.Bauteil.Bauteilart == DbWerte.BAUTEILART_VORHANGFASSADE).ToList();
            Assert.Equal(2, fassaden.Count);
            GebaeudeBauteilzeile sued = Assert.Single(fassaden, z => z.Bauteil.Bezeichner == "Glasfassade Süd");
            GebaeudeBauteilzeile west = Assert.Single(fassaden, z => z.Bauteil.Bezeichner == "Glasfassade West");
            Assert.Equal("IfcCurtainWall", sued.Quelltyp);
            Assert.Equal(25.0, sued.Bauteil.Flaeche);
            Assert.Equal(1.3, sued.Bauteil.U_Wert);
            Assert.Equal(Importherkunft.Ifc, sued.HerkunftU);
            Assert.Equal(DbWerte.HERKUNFT_IFC, sued.Bauteil.Herkunft);
            Assert.Equal(20.0, west.Bauteil.Flaeche);
            Assert.Equal(GebaeudeVorgaben.Fuer('E').UFenster, west.Bauteil.U_Wert);
            Assert.Equal(Importherkunft.Vorgabe, west.HerkunftU);
            Assert.Equal(DbWerte.HERKUNFT_VORGABE, west.Bauteil.Herkunft);
            Assert.All(fassaden, z =>
            {
                Assert.Equal(GebaeudeZielfelder.FLAECHE_SONSTIGE, z.Summenfeld);
                Assert.Equal(DbWerte.RANDBEDINGUNG_AUSSENLUFT, z.Bauteil.Randbedingung);
                Assert.Null(z.Bauteil.g_Wert);
                Assert.True(z.Bauteil.Azimut.HasValue, z.ToString());
            });
            Nah(180.0, sued.Bauteil.Azimut);
            Nah(270.0, west.Bauteil.Azimut);
            Assert.Equal(new[] { "2" }, Assert.Single(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.VORHANGFASSADE).Werte);

            // Die Summenfelder: Sonstige = beide Fassaden, wie im Einzonenweg.
            Assert.Equal(45.0, v.Satz.Zeile(GebaeudeZielfelder.FLAECHE_SONSTIGE).Wert);
            foreach (string feld in GebaeudeBauteilvorschlag.Summenfelder)
                Nah(v.Satz.Zeile(feld).Wert ?? 0.0, v.Summe(feld));

            // Ohne Baualtersklasse hat die Westfassade keinen U-Wert: benannt abgelehnt.
            GebaeudeBauteilvorschlag ohne = Vorschlag("ifc4_vorhangfassade.ifc");
            Assert.True(ohne.Abgelehnt);
            PruefMeldung f = Assert.Single(ohne.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            Assert.Equal(GebaeudeBauteilvorschlag.UWERT_FEHLT, f.Schluessel);
            Assert.Equal("1", f.Werte[0]);
            Assert.Equal(west.Kennung, f.Werte[1]);
        }

        /// <summary>
        /// <b>Die Meldungen des Vorschlags haben Texte in beiden Sprachen</b>, über denselben Weg wie die
        /// Meldungen des Lesers (<see cref="GebaeudeZuordnungsModell.MeldungText"/>): Zielfeldschlüssel
        /// erscheinen mit ihrer Beschriftung, Zahlen in der Anzeigekultur.
        /// </summary>
        [Fact]
        public void Die_Meldungen_des_Vorschlags_stehen_in_beiden_Sprachen()
        {
            var summe = new PruefMeldung(PruefStufe.Fehler, GebaeudeBauteilvorschlag.SUMME_ABWEICHUNG,
                                         GebaeudeZielfelder.FLAECHE_SONSTIGE, "20.5", "45");
            var fassade = new PruefMeldung(PruefStufe.Info, GebaeudeBauteilvorschlag.VORHANGFASSADE, "2");
            using (new Kulturvorrichtung("de-DE"))
            {
                Assert.Equal("Sonstige Flächen: Die Bauteile summieren 20,5 m², die Zuordnung 45 m² — der Vorschlag passt nicht zu den Summenfeldern.",
                             GebaeudeZuordnungsModell.MeldungText(summe));
                Assert.Equal("2 Vorhangfassaden rechnen transparent mit Sonneneintrag; in den Summenfeldern stehen sie unter „Sonstige Flächen“.",
                             GebaeudeZuordnungsModell.MeldungText(fassade));
            }
            using (new Kulturvorrichtung("en-US"))
            {
                Assert.Equal("Other areas: the components add up to 20.5 m², the assignment to 45 m² — the proposal does not match the sum fields.",
                             GebaeudeZuordnungsModell.MeldungText(summe));
                Assert.StartsWith("2 curtain walls are computed as transparent", GebaeudeZuordnungsModell.MeldungText(fassade));
            }
        }

        [Fact]
        public void Eine_Vorhangfassade_ohne_Azimut_wird_benannt_abgelehnt_und_an_Erdreich_rechnet_sie_an_Aussenluft()
        {
            GbxmlAbbild a = BauteilvorschlagProbe.Synthetisch();
            a.Gebaeude[0].Bauteile.Add(BauteilvorschlagProbe.Flaeche("fassade", Bauteilart.Vorhangfassade, Randbedingung.Aussenluft,
                                                                     20, 1.4, 90, null, "R1"));
            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Bilden(a);
            Assert.True(v.Abgelehnt);
            PruefMeldung f = Assert.Single(v.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            Assert.Equal(GebaeudeBauteilvorschlag.AZIMUT_FEHLT, f.Schluessel);
            Assert.Equal(new[] { "1", "fassade" }, f.Werte);

            // An Erdreich: Außenluft wie ein Fenster, gemeldet.
            a.Gebaeude[0].Bauteile[2].AzimutGrad = 90;
            a.Gebaeude[0].Bauteile[2].Randbedingung = Randbedingung.Erdreich;
            GebaeudeBauteilvorschlag e = BauteilvorschlagProbe.Bilden(a);
            GebaeudeBauteilzeile z = BauteilvorschlagProbe.Zeile(e, "fassade");
            Assert.Equal(DbWerte.RANDBEDINGUNG_AUSSENLUFT, z.Bauteil.Randbedingung);
            Assert.Contains(e.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.FENSTER_ERDREICH);
        }

        // =====================================================================
        //  Innere Masse — beide Seiten zählen
        // =====================================================================

        /// <summary>
        /// <b>Beide Seiten zählen</b> (Anwenderentscheid vom 25.09.2026): Eine Trennfläche zweier beheizter
        /// Räume derselben Zone wird bei vollständiger Datenlage zu zwei Zeilen innerhalb der Zone, je Seite
        /// eine mit der Nettofläche. So ist A_IW die Fläche beider Seiten.
        /// </summary>
        [Fact]
        public void Innere_Trennflaechen_zaehlen_mit_beiden_Seiten()
        {
            GebaeudeBauteilvorschlag v = Vorschlag("gbxml_haus_si.xml");
            GebaeudeBauteilzeile[] innen = v.Zeilen.Where(x => x.Summenfeld == null).ToArray();
            Assert.Equal(6, innen.Length);
            Assert.All(innen, x =>
            {
                Assert.Null(x.Bauteil.Randbedingung);   // innerhalb der Zone
                Assert.Null(x.Bauteil.U_Wert);
                Assert.Null(x.Bauteil.Azimut);
                Assert.Contains(x.Seite, new[] { "A", "B" });
            });
            // 2 × (Innenwand 12,5 + Geschossdecken 40 + 20) m² — jede Trennfläche mit vollständigen
            // Stoffwerten, 145 m² ÷ 120 m² = 1,21 im Band 1 … 5: die Innenbauteile werden übernommen.
            Assert.Equal(Innenweg.Bauteile, v.Innenweg);
            Nah(2.0 * (12.5 + 40.0 + 20.0), v.FlaecheInnen);
            Nah(v.FlaecheInnen, v.InnenflaecheDateiM2);
            Assert.Equal(0, v.InnenflaechenUnvollstaendig);
            Assert.Equal("iw-eg (Seite A)", BauteilvorschlagProbe.Zeile(v, "iw-eg", "A").Bauteil.Bezeichner);
            PruefMeldung m = Assert.Single(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.INNEN_BAUTEILE);
            Assert.Equal(new[] { "3", "6", "145", "1.2083" }, m.Werte);

            // Die Zone rechnet die Innenbauteilgruppe aus den Zeilen: A_IW = 145 m², Bauteilweg.
            ErsatzparameterRC p = Parameter(v);
            Nah(145.0, p.A_IW_M2);
            Assert.Equal(Gruppenweg.Bauteilweg, p.WegInnen);
        }

        /// <summary>
        /// Warum beidseitig: Die symmetrische Reduktion nach VDI 6007-1 Gl. (12)–(16) trägt je Seite die
        /// Masse bis zur Mittelebene — bei einer dünnen homogenen Schicht C₁ → ρ·c·d·A/2. Liegen beide
        /// Seiten in der Zone, trägt die Innenbauteilgruppe die ganze Wand.
        /// </summary>
        [Fact]
        public void Die_symmetrische_Reduktion_traegt_je_Seite_die_halbe_Masse()
        {
            var putz = new Schicht(0.01, 1.0, 1400.0, 1000.0);
            const double A = 10.0;
            Bauteilkennwerte seite = Bauteilreduktion.Reduzieren(new[] { putz }, A, GebaeudeFestwerte.BEZUGSPERIODE_BAUTEIL_D, Waermestromrichtung.Horizontal);
            double ganz = 1400.0 * 1000.0 * 0.01 * A;
            Nah(ganz / 2.0, seite.C1_Jk, 1e-3);
            (double r1, double c1) = Bauteilreduktion.Parallel(new[] { (seite.R1_KW, seite.C1_Jk), (seite.R1_KW, seite.C1_Jk) });
            Nah(ganz, c1, 1e-3);
            Nah(seite.R1_KW / 2.0, r1, 1e-9);
        }

        [Fact]
        public void Eine_Trennflaeche_zu_einem_anderen_Gebaeude_zaehlt_nur_mit_der_eigenen_Seite()
        {
            GbxmlAbbild a = BauteilvorschlagProbe.Synthetisch();
            a.Gebaeude[0].Bauteile.Add(BauteilvorschlagProbe.Flaeche("wand-sued", Bauteilart.Aussenwand, Randbedingung.Aussenluft,
                                                                     25, 0.3, 90, 180, "R1"));
            var nachbar = new AbbildGebaeude { Kennung = "G2", Name = "Nachbar" };
            nachbar.Raeume.Add(new AbbildRaum { Kennung = "R2", Quelltyp = "Space", FlaecheM2 = 50, VolumenM3 = 125 });
            a.Gebaeude.Add(nachbar);
            AbbildBauteil trenn = BauteilvorschlagProbe.Flaeche("brandwand", Bauteilart.Innenwand, Randbedingung.Innen, 60, null, 90, 90, "R1", "R2");
            trenn.Aufbau = BauteilvorschlagProbe.Massiv("kon-brandwand");
            a.Gebaeude[0].Bauteile.Add(trenn);
            nachbar.Bauteile.Add(trenn);

            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Bilden(a);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen));
            Assert.Equal(Innenweg.Bauteile, v.Innenweg);
            GebaeudeBauteilzeile z = Assert.Single(v.Zeilen, x => x.Kennung == "brandwand");
            Assert.Null(z.Seite);
            Nah(60.0, z.Bauteil.Flaeche);
            Assert.Null(z.Bauteil.Randbedingung);
            Nah(60.0, v.InnenflaecheDateiM2);   // nur die eigene Seite: 60 m² ÷ 50 m² = 1,2
            Assert.Contains(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.GEBAEUDETRENNFLAECHE && m.Werte[0] == "1");
        }

        // =====================================================================
        //  Innere Masse nach Datenlage (Anwenderentscheid vom 25.09.2026)
        // =====================================================================

        [Fact]
        public void Fehlen_einer_Innenflaeche_die_Stoffwerte_traegt_der_Vorschlag_den_Innenflaechenfaktor()
        {
            GebaeudeBauteilvorschlag v = Vorschlag("gbxml_innenflaechen_teilweise.xml");
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen));
            Assert.Equal(Innenweg.Innenflaechenfaktor, v.Innenweg);
            Assert.DoesNotContain(v.Zeilen, x => x.Summenfeld == null);
            Assert.Equal(3, v.Innenflaechen);
            Assert.Equal(1, v.InnenflaechenUnvollstaendig);   // die Ständerwand nur mit R-Wert
            Nah(66.0, v.InnenflaecheDateiM2);                   // 2 × (9 + 9 + 15) m²
            Nah(1.1, v.Innenflaechenfaktor);                    // ÷ 60 m² Nutzfläche
            Assert.Equal(Importherkunft.GbXml, v.HerkunftInnenflaechenfaktor);
            PruefMeldung m = Assert.Single(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.INNEN_FAKTOR_STOFFWERTE);
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal(new[] { "1.1", "66", "1", "3" }, m.Werte);
            Assert.DoesNotContain(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.INNEN_FAKTOR_UNPLAUSIBEL);
            Assert.DoesNotContain(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.STOFFWERTE_UNVOLLSTAENDIG);

            // Die Masse aus der Bauweise, A_IW gemessen: die Innengruppe rechnet den Klassenweg mit 66 m².
            ErsatzparameterRC p = Parameter(v);
            Nah(66.0, p.A_IW_M2);
            Assert.Equal(Gruppenweg.Klassenweg, p.WegInnen);
            Assert.Equal(Gruppenweg.Bauteilweg, p.WegAussen);
        }

        [Fact]
        public void Die_Grenze_der_Vollstaendigkeit_ist_die_eine_Staenderwand()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen("gbxml_innenflaechen_teilweise.xml");
            AbbildBauteil staender = a.Abbild.Gebaeude[0].Bauteile.Single(b => b.Kennung == "iw-kueche-bad");
            Assert.Equal(Aufbaustatus.Masselos, staender.Aufbau.Status);

            // Dieselbe Datei mit massiver dritter Innenwand: jede Innenfläche vollständig — Innenbauteile.
            staender.Aufbau = BauteilvorschlagProbe.Massiv("kon-staender-massiv");
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, null);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen));
            Assert.Equal(Innenweg.Bauteile, v.Innenweg);
            Assert.Equal(6, v.Zeilen.Count(x => x.Summenfeld == null));
            Assert.All(v.Zeilen.Where(x => x.Summenfeld == null), x => Assert.NotNull(x.Bauteil.ID_Aufbau));
            Nah(66.0, v.FlaecheInnen);
            Assert.Null(v.Innenflaechenfaktor);
            PruefMeldung m = Assert.Single(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.INNEN_BAUTEILE);
            Assert.Equal(new[] { "3", "6", "66", "1.1" }, m.Werte);
            ErsatzparameterRC p = Parameter(v);
            Nah(66.0, p.A_IW_M2);
            Assert.Equal(Gruppenweg.Bauteilweg, p.WegInnen);
        }

        [Theory]
        [InlineData(50.0, true)]     // 2 × 50 m² ÷ 100 m² = 1,0 — die untere Grenze gehört zum Band
        [InlineData(49.9, false)]
        [InlineData(250.0, true)]    // 5,0 — die obere Grenze gehört zum Band
        [InlineData(250.1, false)]
        public void Das_Band_der_Innenflaeche_entscheidet_bei_vollstaendigen_Stoffwerten(double wand, bool bauteile)
        {
            GbxmlAbbild a = BauteilvorschlagProbe.Synthetisch();
            a.Gebaeude[0].Raeume.Add(new AbbildRaum { Kennung = "R3", Quelltyp = "Space", Name = "Raum 3", FlaecheM2 = 50, VolumenM3 = 125 });
            AbbildBauteil iw = BauteilvorschlagProbe.Flaeche("iw", Bauteilart.Innenwand, Randbedingung.Innen, wand, null, 90, null, "R1", "R3");
            iw.Aufbau = BauteilvorschlagProbe.Massiv("kon-iw");
            a.Gebaeude[0].Bauteile.Add(iw);

            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Bilden(a);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen));
            Assert.Equal(0, v.InnenflaechenUnvollstaendig);
            Nah(2.0 * wand, v.InnenflaecheDateiM2);
            if (bauteile)
            {
                Assert.Equal(Innenweg.Bauteile, v.Innenweg);
                Assert.Equal(2, v.Zeilen.Count(x => x.Summenfeld == null));
                Assert.DoesNotContain(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.INNEN_FAKTOR_UNPLAUSIBEL);
            }
            else
            {
                Assert.Equal(Innenweg.Innenflaechenfaktor, v.Innenweg);
                Assert.DoesNotContain(v.Zeilen, x => x.Summenfeld == null);
                Nah(2.0 * wand / 100.0, v.Innenflaechenfaktor);
                PruefMeldung w = Assert.Single(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.INNEN_FAKTOR_UNPLAUSIBEL);
                Assert.Equal(PruefStufe.Warnung, w.Stufe);
                Assert.Equal(new[] { "1", "5" }, w.Werte.Skip(2));
                Assert.DoesNotContain(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.INNEN_FAKTOR_STOFFWERTE);
            }
        }

        [Fact]
        public void Mit_dem_Bad_als_unbeheizt_ist_die_Innenflaeche_zu_klein()
        {
            var ueb = new Dictionary<string, bool> { ["raum-bad"] = false };
            GebaeudeBauteilvorschlag v = BauteilvorschlagProbe.Vorschlag("gbxml_innenflaechen_teilweise.xml", 0, null, ueb);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen));
            // Innen bleibt nur die massive Wand Wohnen|Küche: 2 × 9 m² ÷ 45 m² = 0,4 — vollständig, aber zu klein.
            Assert.Equal(Innenweg.Innenflaechenfaktor, v.Innenweg);
            Assert.Equal(1, v.Innenflaechen);
            Assert.Equal(0, v.InnenflaechenUnvollstaendig);
            Nah(0.4, v.Innenflaechenfaktor);
            PruefMeldung w = Assert.Single(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.INNEN_FAKTOR_UNPLAUSIBEL);
            Assert.Equal(new[] { "0.4", "18", "1", "5" }, w.Werte);
            // Die Wände zum Bad werden Hülle gegen einen unbeheizten Raum; die Ständerwand rechnet mit
            // ihrem R-Wert: U = 1/(0,13 + 0,5 + 0,13).
            GebaeudeBauteilzeile st = BauteilvorschlagProbe.Zeile(v, "iw-kueche-bad");
            Assert.Equal(DbWerte.RANDBEDINGUNG_UNBEHEIZT, st.Bauteil.Randbedingung);
            Nah(1.0 / (0.13 + 0.5 + 0.13), st.Bauteil.U_Wert);
        }

        [Fact]
        public void Ohne_Innenflaechen_bleibt_der_Innenflaechenfaktor_leer()
        {
            GebaeudeBauteilvorschlag v = Vorschlag("gbxml_ohne_konstruktionen.xml", 0, 'E');
            Assert.Equal(Innenweg.Vorgabe, v.Innenweg);
            Assert.Null(v.Innenflaechenfaktor);
            Assert.Equal(Importherkunft.Leer, v.HerkunftInnenflaechenfaktor);
            Assert.Equal(0.0, v.InnenflaecheDateiM2);
            PruefMeldung m = Assert.Single(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.INNEN_VORGABE);
            Assert.Equal(new[] { "2.5" }, m.Werte);
            Nah(2.5 * 50.0, Parameter(v).A_IW_M2);
        }

        /// <summary>
        /// Was <see cref="ErsatzparameterRC.AusBauteilweg(BauteilwegGebaeude, IReadOnlyList{BauteilEingang})"/>
        /// von der inneren Masse erwartet: je Seite eine Zeile innerhalb der Zone — zwei Seiten zu je A
        /// gleichen einer Zeile mit 2A (symmetrischer Aufbau); eine unsymmetrische Decke trägt je Seite
        /// ihre Schichtfolge, die Gruppe ist die komplexe Parallelschaltung beider Seiten; ohne Innenzeile
        /// gilt A_IW = f_IW · A_f mit der Masse aus der Bauweise.
        /// </summary>
        [Fact]
        public void Der_Bauteilweg_erwartet_je_Seite_eine_Zeile_innerhalb_der_Zone()
        {
            var putz = new Schicht(0.01, 0.7, 1400.0, 1000.0);
            var ks = new Schicht(0.115, 1.0, 1800.0, 1000.0);
            var estrich = new Schicht(0.05, 1.4, 2000.0, 1000.0);
            var beton = new Schicht(0.18, 2.0, 2400.0, 1000.0);
            var wand = new BauteilEingang("Außenwand", Bauteilart.Aussenwand, 80.0, Bauteilrand.Aussenluft,
                                          schichten: new[] { putz, new Schicht(0.24, 0.8, 1600.0, 1000.0) }, azimutGrad: 180.0);
            var g = new BauteilwegGebaeude("Probe", 100.0, 5000.0, 0.3, 2.5, 0.0);
            BauteilEingang Innen(string name, double a, Schicht[] s, Bauteilart art, double neigung)
                => new BauteilEingang(name, art, a, Bauteilrand.Innen, schichten: s, neigungGrad: neigung);

            Schicht[] sym = { putz, ks, putz };
            ErsatzparameterRC zwei = ErsatzparameterRC.AusBauteilweg(g, new[]
            {
                wand, Innen("Seite A", 20.0, sym, Bauteilart.Innenwand, 90.0), Innen("Seite B", 20.0, sym, Bauteilart.Innenwand, 90.0),
            });
            ErsatzparameterRC eine = ErsatzparameterRC.AusBauteilweg(g, new[] { wand, Innen("beide Seiten", 40.0, sym, Bauteilart.Innenwand, 90.0) });
            Nah(40.0, zwei.A_IW_M2);
            Nah(eine.C_IW_Jk, zwei.C_IW_Jk, 1e-12);
            Nah(eine.R_1_IW_KW, zwei.R_1_IW_KW, 1e-12);
            Assert.Equal(Gruppenweg.Bauteilweg, zwei.WegInnen);

            ErsatzparameterRC decke = ErsatzparameterRC.AusBauteilweg(g, new[]
            {
                wand, Innen("von unten", 30.0, new[] { beton, estrich }, Bauteilart.Decke, 0.0),
                Innen("von oben", 30.0, new[] { estrich, beton }, Bauteilart.Decke, 180.0),
            });
            Bauteilkennwerte unten = Bauteilreduktion.BezugsperiodeWaehlen(new[] { beton, estrich }, 30.0, Waermestromrichtung.Aufwaerts).Kennwerte;
            Bauteilkennwerte oben = Bauteilreduktion.BezugsperiodeWaehlen(new[] { estrich, beton }, 30.0, Waermestromrichtung.Abwaerts).Kennwerte;
            (double r1, double c1) = Bauteilreduktion.Parallel(new[] { (unten.R1_KW, unten.C1_Jk), (oben.R1_KW, oben.C1_Jk) });
            Nah(60.0, decke.A_IW_M2);
            Nah(c1, decke.C_IW_Jk, 1e-12);
            Nah(r1, decke.R_1_IW_KW, 1e-12);
            Assert.True(Math.Abs(unten.C1_Jk / oben.C1_Jk - 1.0) > 0.01, "Die Seiten der unsymmetrischen Decke tragen verschiedene Masse.");

            ErsatzparameterRC faktor = ErsatzparameterRC.AusBauteilweg(new BauteilwegGebaeude("Probe", 100.0, 5000.0, 0.3, 1.1, 0.0), new[] { wand });
            Nah(110.0, faktor.A_IW_M2);
            Nah(0.7 * 5000.0 * 3600.0, faktor.C_IW_Jk);
            Assert.Equal(Gruppenweg.Klassenweg, faktor.WegInnen);
        }

        // =====================================================================
        //  Abbildung über die Zeilen und den Eingangsbauer
        // =====================================================================

        [Theory]
        [InlineData("gbxml_haus_si.xml", null)]
        [InlineData("gbxml_haus_fuss.xml", null)]
        [InlineData("gbxml_ohne_konstruktionen.xml", 'E')]
        [InlineData("gbxml_rwert_schicht.xml", 'E')]
        [InlineData("gbxml_norddrehung.xml", 'E')]
        [InlineData("gbxml_nachbar_leer.xml", 'E')]
        [InlineData("gbxml_kennung_lang.xml", 'E')]
        [InlineData("gbxml_innenflaechen_teilweise.xml", null)]
        [InlineData("ifc4_haus.ifc", null)]
        [InlineData("ifc2x3_haus.ifc", null)]
        [InlineData("ifc4_mapconversion.ifc", 'E')]
        [InlineData("ifc4_schichten.ifc", 'E')]
        [InlineData("ifc4_schichten_nullwerte.ifc", 'E')]
        [InlineData("ifc2x3_schichten.ifc", 'E')]
        [InlineData("ifc4_rueckfaelle.ifc", 'E')]
        [InlineData("ifc4_vorhangfassade.ifc", 'E')]
        public void Jeder_Vorschlag_bildet_sich_ab_und_baut_den_Bauteilweg(string name, char? klasse)
        {
            GebaeudeBauteilvorschlag v = Vorschlag(name, 0, klasse);
            Assert.False(v.Abgelehnt, name + ": " + string.Join(" | ", v.Meldungen));
            Assert.Null(GebaeudeZonenCtrl.Pruefen(new List<ZoneModel> { v.Zone }));

            GebaeudeZonensatz satz = GebaeudeZonenabbildung.AlsZonensatz(v.Zone, v.AufbautenJeId);
            Assert.Null(satz.Lesefehler);
            Assert.Equal(v.Zeilen.Count, satz.Bauteile.Count);
            Assert.Equal(v.Zone.Nutzflaeche ?? double.NaN, satz.Nutzflaeche_M2);
            Assert.Equal(v.Zeilen.Count(z => z.Bauteil.ID_Aufbau.HasValue), satz.Bauteile.Count(b => b.HatSchichten));

            ErsatzparameterRC p = Parameter(v, out GebaeudeModellEingang e);
            Assert.True(e.Bauteilweg);
            Assert.Equal(v.Zeilen.Count, e.Bauteile.Count);
            // Die Innengruppe nach dem Weg des Vorschlags: Zeilen, gemessener Faktor oder Vorgabe.
            double af = e.Nutzflaeche_M2;   // die Bezugsfläche des Bauteilwegs: die Nutzfläche der Zone
            if (v.Zone.Nutzflaeche.HasValue) Nah(v.Zone.Nutzflaeche.Value, af);
            switch (v.Innenweg)
            {
                case Innenweg.Bauteile:
                    Nah(v.InnenflaecheDateiM2, p.A_IW_M2);
                    break;
                case Innenweg.Innenflaechenfaktor:
                    Nah(v.InnenflaecheDateiM2, p.A_IW_M2);
                    Assert.Equal(Gruppenweg.Klassenweg, p.WegInnen);
                    break;
                default:
                    Nah(GebaeudeFestwerte.VORGABE_INNENFLAECHENFAKTOR * af, p.A_IW_M2);
                    Assert.Equal(Gruppenweg.Klassenweg, p.WegInnen);
                    break;
            }
        }

        /// <summary>
        /// Die Ersatzparameter des Vorschlags im Probegebäude — mit dem Innenflächenfaktor des
        /// Vorschlags an der Gebäudezeile, wie ihn die Zielfelder schreiben (Welle B).
        /// </summary>
        private static ErsatzparameterRC Parameter(GebaeudeBauteilvorschlag v, out GebaeudeModellEingang e)
        {
            ProjektGebaeudeModel g = Vdi6007Probe.Gebaeude();
            g.Zonen = new[] { GebaeudeZonenabbildung.AlsZonensatz(v.Zone, v.AufbautenJeId) };
            g.Innenflaechenfaktor = v.Innenflaechenfaktor;
            e = GebaeudeModellEingang.Bauen(g, Vdi6007Probe.Klima(Vdi6007Probe.Jahresgang), Vdi6007Probe.Wochenende(),
                                            Vdi6007Probe.LAENGE, Vdi6007Probe.BREITE, GebaeudeKlimaweg.ZEITBEZUG_VORGABE, false, null);
            return e.Parameter;
        }

        private static ErsatzparameterRC Parameter(GebaeudeBauteilvorschlag v) => Parameter(v, out _);
    }
}
