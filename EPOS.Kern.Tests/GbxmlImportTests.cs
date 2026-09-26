using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die gbXML-Importproben</b> (Stufe G4c Welle 1; Datenaustauschkonzept Kapitel 9, Proben 4
    /// bis 8, 21, 24 und die Entscheide U13/U14): Leser und gemeinsame Zuordnung gegen die selbst
    /// erzeugten Dateien unter <c>Referenzlaeufe/Importproben/gbxml_*.xml</c> (Quellenvermerk in
    /// <c>LIESMICH_Importproben.md</c> dort).
    ///
    /// <para><b>Das Probenhaus</b> (<c>gbxml_haus_si.xml</c>): drei beheizte Räume (120 m²,
    /// 300 m³, 5 Personen) über einem unbeheizten Keller, Außenwände in vier Himmelsrichtungen mit
    /// 23 m² Fenstern und einer Außentür von 2 m², Flachdach, Kellerdecke, Innenwand,
    /// Geschossdecken; die Außenwand ist ein unsymmetrischer Aufbau mit Innendämmung. Die
    /// Erwartungswerte stehen von Hand hier und folgen aus der Geometrie der Datei.</para>
    ///
    /// <para><b>Ohne Datenbank und ohne Oberfläche.</b> Die Rechnung „U-Wert und Masse aus
    /// Schichten" kommt mit G3 (<c>Bauteilreduktion</c>); die Fälle, die sie brauchen, stehen
    /// benannt übersprungen am Ende.</para>
    /// </summary>
    public sealed class GbxmlImportTests
    {
        private const string P = "IMP_GBXML_PROT_";
        private const double Genau = 1e-9;

        // ==================================================================
        //  Zugang zu den Proben
        // ==================================================================

        /// <summary>Sucht <c>Referenzlaeufe/Importproben</c> aufwärts vom Laufordner (wie <c>KatalogImportTests</c>).</summary>
        internal static string Probe(string name)
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string pfad = Path.Combine(d.FullName, "Referenzlaeufe", "Importproben", name);
                if (File.Exists(pfad)) return pfad;
            }
            Assert.Fail("Die Probe fehlt: Referenzlaeufe/Importproben/" + name);
            return null;
        }

        /// <summary>Liest eine Probendatei mit dem gbXML-Profil.</summary>
        internal static GebaeudeImportAblauf LesenDatei(string name, GebaeudeImportProfil profil = null)
        {
            var ablauf = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(Probe(name)))
                ablauf.Lesen(s, Probe(name), profil ?? new GbxmlImportProfil());
            return ablauf;
        }

        /// <summary>Liest eine Datei und ordnet ihr erstes Gebäude zu.</summary>
        internal static GebaeudeImportSatz Satz(string name, char? klasse = 'E')
        {
            GebaeudeImportAblauf a = LesenDatei(name);
            Assert.True(a.Gebaeude.Count > 0, "Nichts gelesen: " + string.Join(" | ", a.Meldungen));
            return a.Zuordnen(0, klasse);
        }

        internal static double? Wert(GebaeudeImportSatz s, string feld) => s.Zeile(feld).Wert;

        internal static bool Hat(IEnumerable<PruefMeldung> meldungen, string schluessel, PruefStufe? stufe = null)
            => meldungen.Any(m => m.Schluessel == schluessel && (!stufe.HasValue || m.Stufe == stufe.Value));

        private static void Nah(double erwartet, double? ist, double toleranz = Genau)
        {
            Assert.True(ist.HasValue, "Wert fehlt, erwartet " + erwartet);
            Assert.True(Math.Abs(erwartet - ist.Value) <= toleranz, "erwartet " + erwartet + ", ist " + ist.Value);
        }

        // ==================================================================
        //  Das Probenhaus (Meter/Celsius, UTF-8)
        // ==================================================================

        [Fact]
        public void Probenhaus_liefert_die_Zielfelder_des_Einzonenwegs()
        {
            GebaeudeImportSatz s = Satz("gbxml_haus_si.xml", 'E');

            // Kenngrößen: drei beheizte Räume, der Keller bleibt draußen.
            Nah(120.0, Wert(s, GebaeudeZielfelder.NUTZFLAECHE));
            Nah(300.0, Wert(s, GebaeudeZielfelder.VOLUMEN));
            Nah(2.5, Wert(s, GebaeudeZielfelder.RAUMHOEHE));
            Nah(24.0, Wert(s, GebaeudeZielfelder.FLAECHE_JE_NUTZER));
            Assert.Equal(Importherkunft.GbXml, s.Zeile(GebaeudeZielfelder.NUTZFLAECHE).Herkunft);
            Assert.False(s.Zeile(GebaeudeZielfelder.VOLUMEN).Uebernehmen);   // nur Prüfgröße

            // Innere Gewinne: die Auslegungsleistung ist kein mittlerer Gewinn — sie steht nur als Vorschlag
            // im Beleg; übernommen wird die ausgewiesene Vorgabe 5 W/m² × 120 m² (E43).
            GebaeudeFeldzeile gewinne = s.Zeile(GebaeudeZielfelder.INNERE_GEWINNE);
            Assert.Equal(600.0, gewinne.Wert);
            Assert.Equal(Importherkunft.Vorgabe, gewinne.Herkunft);
            Assert.True(gewinne.Uebernehmen);
            Assert.Equal("GIMP_BELEG_GEWINNE_JE_FLAECHE_VORSCHLAG", gewinne.Beleg.Schluessel);
            Assert.Equal(new[] { "5", "120", "600", "600", "3" }, gewinne.Beleg.Werte);
            Assert.Equal("GIMP_BELEG_GEWINNE_JE_FLAECHE", gewinne.VorgabeBeleg.Schluessel);

            // Außenwand: 170 m² brutto − 23 m² Fenster − 2 m² Außentür (U14).
            GebaeudeFeldzeile wand = s.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND);
            Nah(145.0, wand.Wert);
            Nah(170.0, wand.Bruttowert);
            Nah(0.4, Wert(s, GebaeudeZielfelder.U_AUSSENWAND));
            Assert.Equal(Importherkunft.GbXml, s.Zeile(GebaeudeZielfelder.U_AUSSENWAND).Herkunft);

            // Fenster je Himmelsrichtung — die Ostwand des Obergeschosses nur als Polygon (Newell).
            Nah(4.0, Wert(s, GebaeudeZielfelder.FENSTER_NORD));
            Nah(2.0, Wert(s, GebaeudeZielfelder.FENSTER_OST));
            Nah(14.0, Wert(s, GebaeudeZielfelder.FENSTER_SUED));
            Nah(3.0, Wert(s, GebaeudeZielfelder.FENSTER_WEST));
            Nah(23.0, Wert(s, GebaeudeZielfelder.FENSTER_GESAMT));
            Nah(1.1, Wert(s, GebaeudeZielfelder.U_FENSTER));
            Nah(0.6, Wert(s, GebaeudeZielfelder.G_WERT));   // der Wert bei 0°, nicht der bei 60°

            Nah(60.0, Wert(s, GebaeudeZielfelder.FLAECHE_DACH));
            Nah(0.25, Wert(s, GebaeudeZielfelder.U_DACH));

            // Kellerdecke: Boden des beheizten Raums gegen den unbeheizten Keller.
            Nah(60.0, Wert(s, GebaeudeZielfelder.FLAECHE_GRUND));
            Nah(0.4, Wert(s, GebaeudeZielfelder.U_GRUND));
            Assert.Equal(DbWerte.GRUND_KELLER, s.Zeile(GebaeudeZielfelder.GRUND_RANDBEDINGUNG).Textwert);

            // Sonstige: nur die Außentür, U-Wert am Vorkommnis.
            Nah(2.0, Wert(s, GebaeudeZielfelder.FLAECHE_SONSTIGE));
            Nah(1.8, Wert(s, GebaeudeZielfelder.U_SONSTIGE));

            // Lüftung nach D12, Sollwert aus der Zone.
            Nah(0.5, Wert(s, GebaeudeZielfelder.LUFTWECHSEL_INFILTRATION));
            Assert.Null(Wert(s, GebaeudeZielfelder.LUFTWECHSEL_NUTZER));
            Nah(20.0, Wert(s, GebaeudeZielfelder.SOLL_TAG));

            // Klasse, Bauart, Bauweise.
            Assert.Equal("E", s.Zeile(GebaeudeZielfelder.BAUALTERSKLASSE).Textwert);
            Assert.Equal(Importherkunft.Manuell, s.Zeile(GebaeudeZielfelder.BAUALTERSKLASSE).Herkunft);
            // Welle 2: Alle Hüllbauteile tragen vollständige Aufbauten — die Bauart kommt aus den
            // Schichten (33,83 Wh/(m²K), eingerastet auf „schwer"), die Bauweise bleibt leer.
            Assert.Equal(GebaeudeZielfelder.BAUART_SCHWER, s.Zeile(GebaeudeZielfelder.BAUART).Textwert);
            Assert.Equal(Importherkunft.GbXml, s.Zeile(GebaeudeZielfelder.BAUART).Herkunft);
            Assert.Equal("GIMP_BELEG_BAUART_SCHICHTEN", s.Zeile(GebaeudeZielfelder.BAUART).Beleg.Schluessel);
            Assert.Null(Wert(s, GebaeudeZielfelder.BAUWEISE));

            // Meldungen: kein Norden (Annahme 0°), Verschattung übergangen, Zonenvorschlag X1.
            Assert.True(Hat(s.Meldungen, P + "KEIN_NORDEN", PruefStufe.Warnung));
            Assert.True(Hat(s.Meldungen, P + "UEBERGANGEN", PruefStufe.Info));
            Assert.True(Hat(s.Meldungen, P + "ZONENVORSCHLAG", PruefStufe.Info));
            Assert.DoesNotContain(s.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            Assert.Equal("X4", s.Zonenregel);
            Assert.Equal("X1", s.Zonenvorschlag);
            Assert.Empty(GebaeudeImportAblauf.Pruefen(s));
        }

        [Fact]
        public void Die_Quelle_traegt_Name_Hash_Groesse_und_Schemastand()
        {
            GebaeudeImportAblauf a = LesenDatei("gbxml_haus_si.xml");
            byte[] inhalt = File.ReadAllBytes(Probe("gbxml_haus_si.xml"));
            GebaeudeQuelle q = a.Quelle;

            Assert.Equal(GebaeudeQuelle.FORMAT_GBXML, q.Format);
            Assert.Equal("gbxml_haus_si.xml", q.Dateiname);   // nur der Name, nie der Pfad
            Assert.Equal(Convert.ToHexStringLower(SHA256.HashData(inhalt)), q.Hash);
            Assert.Equal(64, q.Hash.Length);
            Assert.Equal(inhalt.LongLength, q.Groesse);
            Assert.Equal("0.37", q.Schemastand);
            Assert.Equal("X4", q.Zonenregel);
            Assert.Equal(0, q.FehlendeEntitaeten);
            Assert.Matches(@"^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}[+-]\d{2}:\d{2}$", q.Zeitpunkt);
            Assert.Equal(new[] { "Haus 1" }, a.Gebaeude);
        }

        [Fact]
        public void Die_Quellzuordnungen_paaren_Gebaeude_beheizte_Raeume_und_Huellbauteile()
        {
            GebaeudeImportSatz s = Satz("gbxml_haus_si.xml");
            Assert.All(s.Quellzuordnungen, z => Assert.Equal(ImportZiel.Gebaeude, z.Ziel));
            Assert.Contains(s.Quellzuordnungen, z => z.Quelltyp == "Building" && z.Quellkennung == "geb-1");
            Assert.Equal(new[] { "raum-kueche", "raum-schlafen", "raum-wohnen" },
                         s.Quellzuordnungen.Where(z => z.Quelltyp == "Space").Select(z => z.Quellkennung).OrderBy(k => k, StringComparer.Ordinal));
            Assert.Contains(s.Quellzuordnungen, z => z.Quelltyp == "Surface" && z.Quellkennung == "aw-og-ost");
            Assert.Contains(s.Quellzuordnungen, z => z.Quelltyp == "Opening" && z.Quellkennung == "tuer-eg-nord");
            // Innenbauteile und Kellerflächen sind keine Hülle.
            Assert.DoesNotContain(s.Quellzuordnungen, z => z.Quellkennung == "iw-eg" || z.Quellkennung == "kw-nord");
        }

        [Fact]
        public void Die_Schichtfolge_bleibt_aussen_nach_innen_und_ist_benannt()
        {
            GebaeudeImportAblauf a = LesenDatei("gbxml_haus_si.xml");
            AbbildBauteil wand = a.Abbild.Gebaeude[0].Bauteile.Single(b => b.Kennung == "aw-eg-sued-wohnen");
            AbbildAufbau aufbau = wand.Aufbau;

            Assert.Equal(Aufbaustatus.Vollstaendig, aufbau.Status);
            Assert.Equal(Schichtrichtung.AussenNachInnen, aufbau.Richtung);
            Assert.True(aufbau.RichtungAngenommen);
            // Unsymmetrisch: schwer außen, Dämmung innen — eine vergessene Umkehr fiele auf (Probe 1).
            Assert.Equal(new[] { "mat-aussenputz", "mat-mauerwerk", "mat-innendaemmung", "mat-innenputz" },
                         aufbau.Schichten.Select(x => x.BaustoffKennung));
            AbbildSchicht mauerwerk = aufbau.Schichten[1];
            Nah(0.24, mauerwerk.DickeM);
            Nah(0.8, mauerwerk.LambdaWmK);
            Nah(1600.0, mauerwerk.RhoKgM3);
            Nah(1000.0, mauerwerk.CpJkgK);

            // Mehrere LayerId je Construction werden ebenso in Dateifolge gelesen.
            AbbildAufbau dach = a.Abbild.Gebaeude[0].Bauteile.Single(b => b.Kennung == "dach-og").Aufbau;
            Assert.Equal(new[] { "mat-dachdaemmung", "mat-beton-20", "mat-innenputz" }, dach.Schichten.Select(x => x.BaustoffKennung));
        }

        [Fact]
        public void Das_Polygon_ergibt_Flaeche_Azimut_und_Neigung_nach_Newell()
        {
            GebaeudeImportAblauf a = LesenDatei("gbxml_haus_si.xml");
            AbbildBauteil ost = a.Abbild.Gebaeude[0].Bauteile.Single(b => b.Kennung == "aw-og-ost");
            Nah(12.5, ost.BruttoflaecheM2);
            Nah(90.0, ost.AzimutGrad, 1e-9);
            Nah(90.0, ost.NeigungGrad, 1e-9);
        }

        // ==================================================================
        //  Probe 5 — UTF-16LE mit BOM liefert dieselben Zahlen wie UTF-8
        // ==================================================================

        [Fact]
        public void Probe5_UTF16_liefert_dieselben_Zahlen_wie_UTF8()
        {
            byte[] roh = File.ReadAllBytes(Probe("gbxml_haus_utf16.xml"));
            Assert.Equal(new byte[] { 0xFF, 0xFE }, roh.Take(2));

            GebaeudeImportAblauf a8 = LesenDatei("gbxml_haus_si.xml");
            GebaeudeImportAblauf a16 = LesenDatei("gbxml_haus_utf16.xml");
            GebaeudeImportSatz s8 = a8.Zuordnen(0, 'E');
            GebaeudeImportSatz s16 = a16.Zuordnen(0, 'E');

            Assert.Equal(s8.Zeilen.Select(z => z.ToString()), s16.Zeilen.Select(z => z.ToString()));
            Assert.Equal(a8.Abbild.Gebaeude[0].Raeume.Select(r => r.Name), a16.Abbild.Gebaeude[0].Raeume.Select(r => r.Name));
            Assert.Contains("Küche", a16.Abbild.Gebaeude[0].Raeume.Select(r => r.Name));
            Assert.NotEqual(a8.Quelle.Hash, a16.Quelle.Hash);
        }

        // ==================================================================
        //  Probe 6 — Einheiten
        // ==================================================================

        [Fact]
        public void Probe6_Fuss_und_Fahrenheit_liefern_dieselben_SI_Werte()
        {
            GebaeudeImportAblauf si = LesenDatei("gbxml_haus_si.xml");
            GebaeudeImportAblauf fuss = LesenDatei("gbxml_haus_fuss.xml");
            GebaeudeImportSatz s1 = si.Zuordnen(0, 'E');
            GebaeudeImportSatz s2 = fuss.Zuordnen(0, 'E');

            foreach (GebaeudeFeldzeile z1 in s1.Zeilen)
            {
                GebaeudeFeldzeile z2 = s2.Zeile(z1.Zielfeld);
                Assert.Equal(z1.Herkunft, z2.Herkunft);
                Assert.Equal(z1.Textwert, z2.Textwert);
                Assert.Equal(z1.Wert.HasValue, z2.Wert.HasValue);
                if (z1.Wert.HasValue) Relativ(z1.Wert.Value, z2.Wert.Value, z1.Zielfeld);
            }

            // Jede Schicht: Dicke, λ, ρ, c auf 1e-6 relativ.
            var schichten1 = si.Abbild.Gebaeude[0].Bauteile.Where(b => b.Aufbau != null).SelectMany(b => b.Aufbau.Schichten).ToList();
            var schichten2 = fuss.Abbild.Gebaeude[0].Bauteile.Where(b => b.Aufbau != null).SelectMany(b => b.Aufbau.Schichten).ToList();
            Assert.Equal(schichten1.Count, schichten2.Count);
            for (int i = 0; i < schichten1.Count; i++)
            {
                Relativ(schichten1[i].DickeM.Value, schichten2[i].DickeM.Value, "Dicke");
                Relativ(schichten1[i].LambdaWmK.Value, schichten2[i].LambdaWmK.Value, "Lambda");
                Relativ(schichten1[i].RhoKgM3.Value, schichten2[i].RhoKgM3.Value, "Rho");
                Relativ(schichten1[i].CpJkgK.Value, schichten2[i].CpJkgK.Value, "c");
            }

            // Der Kühlsollwert steht global in Fahrenheit (78,8 °F = 26 °C), der Heizsollwert lokal in °C.
            AbbildRaum wohnen = fuss.Abbild.Gebaeude[0].Raeume.Single(r => r.Kennung == "raum-wohnen");
            Relativ(26.0, wohnen.SollKuehlenC.Value, "DesignCoolT");
            Relativ(20.0, wohnen.SollHeizenC.Value, "DesignHeatT");
            // Die dritte Personenart: Fläche je Person in Quadratfuß.
            Relativ(20.0, wohnen.FlaecheJePersonM2.Value, "SquareFtPerPerson");
        }

        private static void Relativ(double erwartet, double ist, string was)
        {
            double rand = 1e-6 * Math.Max(Math.Abs(erwartet), 1e-12);
            Assert.True(Math.Abs(erwartet - ist) <= rand, was + ": erwartet " + erwartet + ", ist " + ist);
        }

        [Fact]
        public void Probe6_lokales_unit_schlaegt_globales()
        {
            string xml = Klein.Datei(
                Klein.Wand("aw-1", 0, "<Width unit=\"Meters\">10</Width><Height>8.2020997375328086</Height>"),
                laenge: "Feet");
            GebaeudeImportAblauf a = Klein.Lesen(xml);
            AbbildBauteil wand = a.Abbild.Gebaeude[0].Bauteile.Single();
            // 10 m lokal × 8,2021 ft global = 10 m × 2,5 m
            Nah(25.0, wand.BruttoflaecheM2, 1e-9);
        }

        [Fact]
        public void Probe6_unbekanntes_unit_ergibt_null_nie_0()
        {
            string xml = Klein.Datei(Klein.Wand("aw-1", 0, "<Width>10</Width><Height>2.5</Height>"),
                                     raum: "<Space id=\"raum-1\" conditionType=\"Heated\"><Area unit=\"Acres\">1</Area><Volume>125</Volume></Space>");
            GebaeudeImportAblauf a = Klein.Lesen(xml);
            AbbildRaum r = a.Abbild.Gebaeude[0].Raeume.Single();
            Assert.Null(r.FlaecheM2);
            Assert.True(Hat(a.Meldungen, P + "EINHEIT_UNBEKANNT", PruefStufe.Warnung));
            PruefMeldung m = a.Meldungen.First(x => x.Schluessel == P + "EINHEIT_UNBEKANNT");
            Assert.Equal(new[] { "Area", "Acres" }, m.Werte);

            GebaeudeImportSatz s = a.Zuordnen(0, 'E');
            Assert.Null(Wert(s, GebaeudeZielfelder.NUTZFLAECHE));   // leer, nicht 0
            Assert.True(Hat(GebaeudeImportAblauf.Pruefen(s), "IMP_GEB_PROT_PFLICHT_FEHLT", PruefStufe.Fehler));
        }

        [Fact]
        public void Die_Einheiten_rechnen_mit_den_Definitionen()
        {
            Assert.Equal(1.0, GbxmlEinheiten.Laenge(1.0, "Meters"));
            Assert.Equal(0.3048, GbxmlEinheiten.Laenge(1.0, "Feet"));
            Assert.Equal(0.09290304, GbxmlEinheiten.Flaeche(1.0, "SquareFeet").Value, 15);
            Assert.Equal(0.028316846592, GbxmlEinheiten.Volumen(1.0, "CubicFeet").Value, 15);
            Assert.Equal(20.0, GbxmlEinheiten.Temperatur(68.0, "F").Value, 12);
            Assert.Equal(0.0, GbxmlEinheiten.Temperatur(273.15, "K").Value, 12);
            Assert.Equal(0.0, GbxmlEinheiten.Temperatur(491.67, "R").Value, 12);
            Assert.Equal(4186.8, GbxmlEinheiten.Waermekapazitaet(1.0, "BTUPerLbF").Value, 9);
            Assert.Equal(100.0, GbxmlEinheiten.Leitfaehigkeit(1.0, "WPerCmC").Value, 12);
            // densityUnitEnum (Ver8.01): GramsPerCubicCm, nicht „KgPerCubicCm" — das Schema kennt es nicht.
            Assert.Equal(1800.0, GbxmlEinheiten.Dichte(1.8, "GramsPerCubicCm").Value, 9);
            Assert.Null(GbxmlEinheiten.Dichte(1.8, "KgPerCubicCm"));
            Assert.Equal(16.018463373960138, GbxmlEinheiten.Dichte(1.0, "LbsPerCubicFt").Value, 9);
            Assert.Equal(1.0, GbxmlEinheiten.RWert(1.0, "HrSquareFtFPerBTU").Value * GbxmlEinheiten.UWert(1.0, "BtuPerHourSquareFtF").Value, 12);
            Assert.Equal(0.6, GbxmlEinheiten.Anteil(60.0, "Percent").Value, 12);
            Assert.Null(GbxmlEinheiten.Laenge(1.0, "Parsecs"));
            Assert.Null(GbxmlEinheiten.UWert(1.0, "WPerSquareFootK"));
            Assert.Null(GbxmlEinheiten.FlaecheJePerson(3.0, GbxmlEinheiten.PERSONEN_ANZAHL));
        }

        // ==================================================================
        //  Probe 7 — leere Bauphysik
        // ==================================================================

        [Fact]
        public void Probe7_ohne_Konstruktionen_ist_jede_U_Zeile_Vorgabe_oder_leer()
        {
            GebaeudeImportAblauf a = LesenDatei("gbxml_ohne_konstruktionen.xml");
            Assert.True(Hat(a.Meldungen, P + "KEINE_KONSTRUKTIONEN", PruefStufe.Warnung));
            Assert.True(Hat(a.Meldungen, P + "OHNE_AUFBAU", PruefStufe.Warnung));

            string[] uZeilen = { GebaeudeZielfelder.U_AUSSENWAND, GebaeudeZielfelder.U_FENSTER, GebaeudeZielfelder.U_DACH,
                                 GebaeudeZielfelder.U_GRUND, GebaeudeZielfelder.U_SONSTIGE, GebaeudeZielfelder.G_WERT };
            GebaeudeImportSatz mitKlasse = a.Zuordnen(0, 'E');
            foreach (string f in uZeilen)
            {
                Assert.Equal(Importherkunft.Vorgabe, mitKlasse.Zeile(f).Herkunft);
                Assert.Equal(GebaeudeVorgaben.Wert('E', f), mitKlasse.Zeile(f).Wert);
            }

            // Klasse M (ab 2021) liefert ihre Vorgaben aus den Katalogsätzen (E51).
            GebaeudeImportSatz neubau = a.Zuordnen(0, 'M');
            Assert.True(GebaeudeVorgaben.Fuer('M').Katalogsaetze > 0);
            foreach (string f in uZeilen)
            {
                Assert.Equal(Importherkunft.Vorgabe, neubau.Zeile(f).Herkunft);
                Assert.Equal(GebaeudeVorgaben.Wert('M', f), neubau.Zeile(f).Wert);
                Assert.Equal(GebaeudeVorgaben.BELEG_KLASSE, neubau.Zeile(f).Beleg.Schluessel);
            }

            // Hätte M keinen Katalogsatz (Lesenaht), gälte der freie Wert nach Stein/Loga - sichtbar.
            GebaeudeImportSatz frei;
            using (GebaeudeVorgaben.KatalogOhne("M"))
                frei = a.Zuordnen(0, 'M');
            foreach (string f in uZeilen)
            {
                Assert.Equal(Importherkunft.VorgabeFrei, frei.Zeile(f).Herkunft);
                Assert.Equal(GebaeudeVorgaben.BELEG_FREI, frei.Zeile(f).Beleg.Schluessel);
            }
            Assert.Equal(GebaeudeVorgaben.Frei('M').UAussenwand, frei.Zeile(GebaeudeZielfelder.U_AUSSENWAND).Wert);
            Assert.Equal(GebaeudeVorgaben.Frei('M').GWert, frei.Zeile(GebaeudeZielfelder.G_WERT).Wert);
            Assert.DoesNotContain(mitKlasse.Zeilen.Concat(neubau.Zeilen).Concat(frei.Zeilen),
                                  z => uZeilen.Contains(z.Zielfeld) && z.Herkunft == Importherkunft.GbXml);

            // Die Geometrie trägt trotzdem: Flächen und Fenster kommen aus der Datei.
            Nah(75.0 - 1.0 - 3.0, Wert(mitKlasse, GebaeudeZielfelder.FLAECHE_AUSSENWAND));
            Nah(3.0, Wert(mitKlasse, GebaeudeZielfelder.FENSTER_SUED));
        }

        // ==================================================================
        //  Probe 8 — masseloser Aufbau
        // ==================================================================

        [Fact]
        public void Probe8_masseloser_Aufbau_warnt_und_fuehrt_keine_Masse()
        {
            GebaeudeImportAblauf a = LesenDatei("gbxml_rwert_schicht.xml");
            Assert.True(Hat(a.Meldungen, P + "AUFBAU_MASSELOS", PruefStufe.Warnung));

            AbbildAufbau aufbau = a.Abbild.Gebaeude[0].Bauteile.First(b => b.Kennung == "aw-nord").Aufbau;
            Assert.Equal(Aufbaustatus.Masselos, aufbau.Status);
            Assert.True(aufbau.Schichten[0].Vollstaendig);
            Assert.True(aufbau.Schichten[1].NurRWert);
            Assert.Equal(0.18, aufbau.Schichten[1].RWertM2KW);
            Assert.Null(aufbau.Schichten[1].RhoKgM3);
            Assert.Null(aufbau.UWertWm2K);   // die Datei trägt keinen U-Wert
        }

        [Fact]
        public void Probe8_der_U_Wert_kommt_aus_der_Schichtung_die_Masse_nicht()
        {
            // Mauerwerk 0,24/0,8 = 0,30 + Luftschicht 0,18 (eingetragener R-Wert, keine Luftschicht
            // nach Tabelle 8) + R_si 0,13 + R_se 0,04 = 0,65 m²K/W → U = 1/0,65.
            GebaeudeImportSatz s = Satz("gbxml_rwert_schicht.xml");
            Nah(1.0 / 0.65, Wert(s, GebaeudeZielfelder.U_AUSSENWAND), 1e-12);
            Assert.Equal(Importherkunft.GbXml, s.Zeile(GebaeudeZielfelder.U_AUSSENWAND).Herkunft);
            Assert.Equal(Importherkunft.Vorgabe, s.Zeile(GebaeudeZielfelder.BAUART).Herkunft);   // Masse aus der Vorgabe

            AbbildAufbau aufbau = LesenDatei("gbxml_rwert_schicht.xml").Abbild.Gebaeude[0].Bauteile.First(b => b.Kennung == "aw-nord").Aufbau;
            Assert.Null(SchichtwerteNaht.KapazitaetAusSchichten(aufbau));
        }

        /// <summary>
        /// Die Aufbauten des Probenhauses von Hand nachgerechnet (DIN EN ISO 6946: R_si 0,13 waagerecht,
        /// 0,10 aufwärts, 0,17 abwärts; R_se 0,04 an Außenluft, = R_si gegen Unbeheizt).
        /// </summary>
        [Fact]
        public void Schichtaufbau_U_und_Masse_der_Aufbauten_des_Probenhauses()
        {
            GebaeudeImportAblauf a = LesenDatei("gbxml_haus_si.xml");
            AbbildAufbau Aufbau(string kennung) => a.Abbild.Gebaeude[0].Bauteile.Single(b => b.Kennung == kennung).Aufbau;

            // Außenwand mit Innendämmung: R = 0,02 + 0,30 + 2,00 + 0,01 = 2,33 → 1/(0,13 + 2,33 + 0,04) = 0,4;
            // C = 0,02·1800·1000 + 0,24·1600·1000 + 0,08·100·1000 + 0,01·1400·1000 = 442 000 J/(m²K).
            AbbildAufbau wand = Aufbau("aw-eg-nord-wohnen");
            Assert.Equal(0.4, SchichtwerteNaht.UWertAusSchichten(wand, 90.0, Randbedingung.Aussenluft, out string grund).Value, 12);
            Assert.Null(grund);
            Assert.Equal(442000.0, SchichtwerteNaht.KapazitaetAusSchichten(wand).Value, 6);

            // Flachdach, Wärmestrom aufwärts: R = 3,75 + 0,10 + 0,01 = 3,86 → 1/(0,10 + 3,86 + 0,04) = 0,25.
            Assert.Equal(0.25, SchichtwerteNaht.UWertAusSchichten(Aufbau("dach-og"), 0.0, Randbedingung.Aussenluft, out _).Value, 12);

            // Kellerdecke, Wärmestrom abwärts gegen Unbeheizt: R = 2,00 + 0,08 + 0,08 = 2,16 → 1/(0,17 + 2,16 + 0,17) = 0,4.
            Assert.Equal(0.4, SchichtwerteNaht.UWertAusSchichten(Aufbau("decke-kg-eg-wohnen"), 180.0, Randbedingung.Unbeheizt, out _).Value, 12);

            // Ohne Neigung oder bei unbekannter Randbedingung wird nichts gerechnet.
            Assert.Null(SchichtwerteNaht.UWertAusSchichten(wand, null, Randbedingung.Aussenluft, out _));
            Assert.Null(SchichtwerteNaht.UWertAusSchichten(wand, 90.0, Randbedingung.Unbekannt, out _));
        }

        // ==================================================================
        //  Probe 21 — Norddrehung, Probe 24 — überlange Kennung
        // ==================================================================

        [Fact]
        public void Probe21_Norddrehung_wird_gemeldet_und_nicht_aufaddiert()
        {
            GebaeudeImportAblauf a = LesenDatei("gbxml_norddrehung.xml");
            Assert.Equal(60.0, a.Abbild.NordwinkelGrad);
            PruefMeldung m = a.Meldungen.Single(x => x.Schluessel == P + "NORDDREHUNG");
            Assert.Equal(PruefStufe.Warnung, m.Stufe);
            Assert.Equal("60", m.Werte[0]);
            Assert.False(Hat(a.Meldungen, P + "KEIN_NORDEN"));

            GebaeudeImportSatz s = a.Zuordnen(0, 'E');
            Nah(3.0, Wert(s, GebaeudeZielfelder.FENSTER_SUED));   // 180° bleibt Süd, nicht 240° = West
            Nah(0.0, Wert(s, GebaeudeZielfelder.FENSTER_WEST));
            Assert.Equal(180.0, a.Abbild.Gebaeude[0].Bauteile.Single(b => b.Kennung == "aw-sued").AzimutGrad);
        }

        [Fact]
        public void Probe24_ueberlange_Kennung_wird_mit_SHA256_Praefix_gekuerzt()
        {
            string lang = "aussenwand-nord-" + new string('x', 64);
            GebaeudeImportAblauf a = LesenDatei("gbxml_kennung_lang.xml");
            PruefMeldung m = a.Meldungen.Single(x => x.Schluessel == P + "KENNUNG_GEKUERZT");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal("Surface", m.Werte[0]);
            Assert.Equal("80", m.Werte[2]);

            string erwartet = lang.Substring(0, 56)
                              + Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(lang))).Substring(0, 8);
            Assert.Equal(erwartet, m.Werte[1]);
            Assert.Equal(64, erwartet.Length);

            GebaeudeImportSatz s = a.Zuordnen(0, 'E');
            Assert.Contains(s.Quellzuordnungen, z => z.Quelltyp == "Surface" && z.Quellkennung == erwartet);
            Assert.All(s.Quellzuordnungen, z => Assert.True(z.Quellkennung.Length <= Quellkennung.MAX_LAENGE));
            // Das Abbild behält die volle Kennung.
            Assert.Contains(a.Abbild.Gebaeude[0].Bauteile, b => b.Kennung == lang);
        }

        [Fact]
        public void Quellkennung_bis_64_Zeichen_bleibt_unveraendert()
        {
            string k64 = new string('a', 64);
            Assert.Same(k64, Quellkennung.Kuerzen(k64, out bool gekuerzt));
            Assert.False(gekuerzt);
            string k65 = new string('a', 65);
            string kurz = Quellkennung.Kuerzen(k65, out gekuerzt);
            Assert.True(gekuerzt);
            Assert.Equal(64, kurz.Length);
            Assert.Equal(kurz, Quellkennung.Kuerzen(k65));   // deterministisch
            Assert.NotEqual(kurz, Quellkennung.Kuerzen(new string('a', 66)));
            Assert.Equal("", Quellkennung.Kuerzen(null));
        }

        // ==================================================================
        //  U13, U14 und die Nachbarschaft
        // ==================================================================

        [Fact]
        public void U13_zwei_Gebaeude_ergeben_zwei_Eintraege_je_Lauf_eines()
        {
            GebaeudeImportAblauf a = LesenDatei("gbxml_zwei_gebaeude.xml");
            Assert.Equal(new[] { "Haus A", "Haus B" }, a.Gebaeude);

            GebaeudeImportSatz s0 = a.Zuordnen(0, 'E');
            GebaeudeImportSatz s1 = a.Zuordnen(1, 'E');
            Nah(100.0, Wert(s0, GebaeudeZielfelder.NUTZFLAECHE));
            Nah(50.0, Wert(s1, GebaeudeZielfelder.NUTZFLAECHE));
            Nah(25.0, Wert(s0, GebaeudeZielfelder.FLAECHE_AUSSENWAND));
            Nah(15.0, Wert(s1, GebaeudeZielfelder.FLAECHE_AUSSENWAND));
            Assert.Equal("geb-b", s1.Gebaeudekennung);
            Assert.Throws<ArgumentOutOfRangeException>(() => a.Zuordnen(2, 'E'));
        }

        [Fact]
        public void U14_negative_Nettoflaeche_wird_0_und_die_Zeile_Fehler()
        {
            GebaeudeImportSatz s = Satz("gbxml_nettoflaeche_negativ.xml");
            GebaeudeFeldzeile wand = s.Zeile(GebaeudeZielfelder.FLAECHE_AUSSENWAND);
            Nah(25.0, wand.Wert);          // Nordwand 25 m², Südwand 10 − 12 → 0
            Nah(35.0, wand.Bruttowert);
            Assert.Equal(PruefStufe.Fehler, wand.Markierung);
            PruefMeldung m = s.Meldungen.Single(x => x.Schluessel == P + "NETTOFLAECHE_NEGATIV");
            Assert.Equal(PruefStufe.Fehler, m.Stufe);
            Assert.Equal(new[] { "aw-sued", "10", "12" }, m.Werte);
            Nah(12.0, Wert(s, GebaeudeZielfelder.FENSTER_SUED));

            IReadOnlyList<PruefMeldung> pruefung = GebaeudeImportAblauf.Pruefen(s);
            Assert.True(GebaeudeImportAblauf.Blockiert(pruefung));
            Assert.Contains(pruefung, x => x.Schluessel == "IMP_GEB_PROT_ZEILE_FEHLER" && x.Werte[0] == GebaeudeZielfelder.FLAECHE_AUSSENWAND);

            // Nimmt der Anwender den Haken weg, sperrt die Zeile nicht mehr.
            wand.Uebernehmen = false;
            Assert.False(GebaeudeImportAblauf.Blockiert(GebaeudeImportAblauf.Pruefen(s)));
        }

        [Fact]
        public void Ein_Nachbar_ins_Leere_gilt_als_unbeheizt()
        {
            GebaeudeImportSatz s = Satz("gbxml_nachbar_leer.xml");
            PruefMeldung m = s.Meldungen.Single(x => x.Schluessel == P + "NACHBAR_UNBEKANNT");
            Assert.Equal(new[] { "iw-ost", "raum-gibt-es-nicht" }, m.Werte);
            GebaeudeFeldzeile sonstige = s.Zeile(GebaeudeZielfelder.FLAECHE_SONSTIGE);
            Nah(12.5, sonstige.Wert);
            Assert.Equal(PruefStufe.Warnung, sonstige.Markierung);
            Assert.True(Hat(s.Meldungen, "IMP_GEB_PROT_FLAECHE_GEGEN_UNBEHEIZT", PruefStufe.Info));
        }

        [Fact]
        public void Eine_Flaeche_ohne_Nachbarangabe_wird_gemeldet_und_nicht_gezaehlt()
        {
            GebaeudeImportAblauf a = LesenDatei("gbxml_ohne_nachbar.xml");
            PruefMeldung m = a.Meldungen.Single(x => x.Schluessel == P + "OHNE_NACHBAR");
            Assert.Equal("aw-ohne-raum", m.Werte[0]);
            Assert.Single(a.Abbild.BauteileOhneGebaeude);
            // Die Verschattung wird übergangen, nicht als „ohne Nachbar" gemeldet.
            PruefMeldung u = a.Meldungen.Single(x => x.Schluessel == P + "UEBERGANGEN");
            Assert.Equal(new[] { "Shade", "1" }, u.Werte);

            GebaeudeImportSatz s = a.Zuordnen(0, 'E');
            Nah(50.0, Wert(s, GebaeudeZielfelder.FLAECHE_AUSSENWAND));
            Assert.Contains(s.Meldungen, x => x.Schluessel == P + "OHNE_NACHBAR");
        }

        // ==================================================================
        //  Ablehnungen und Lesefehler — Meldungen, keine Ausnahmen
        // ==================================================================

        /// <summary>Ein Profil mit frei gesetzter Grenze und einem zählenden Leser.</summary>
        private sealed class Pruefprofil : GebaeudeImportProfil
        {
            internal int Aufrufe;

            internal Pruefprofil(long maxBytes)
                : base(GebaeudeQuelle.FORMAT_GBXML, GbxmlImportProfil.DATEIFILTER, maxBytes, new[] { ZONENREGEL_X4 },
                       "", GbxmlImportProfil.MELDUNGSPRAEFIX, "GIMP_SCHEMA_GBXML") { }

            public override IGebaeudeLeser LeserErzeugen() => new ZaehlenderLeser(this);

            private sealed class ZaehlenderLeser : IGebaeudeLeser
            {
                private readonly Pruefprofil _profil;
                internal ZaehlenderLeser(Pruefprofil profil) { _profil = profil; }

                public GebaeudeAbbild Lesen(Stream quelle, GebaeudeImportProfil profil, IProgress<ImportFortschritt> melder, CancellationToken abbruch)
                {
                    _profil.Aufrufe++;
                    return new GbxmlLeser().Lesen(quelle, profil, melder, abbruch);
                }
            }
        }

        /// <summary>Ein Strom ohne Längenangabe — wie ein Netzstrom oder ein iOS-Dateiwähler.</summary>
        private sealed class OhneLaenge : Stream
        {
            private readonly Stream _innen;
            internal OhneLaenge(Stream innen) { _innen = innen; }
            public override bool CanRead => true;
            public override bool CanSeek => false;
            public override bool CanWrite => false;
            public override long Length => throw new NotSupportedException();
            public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
            public override void Flush() { }
            public override int Read(byte[] buffer, int offset, int count) => _innen.Read(buffer, offset, count);
            public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
            public override void SetLength(long value) => throw new NotSupportedException();
            public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        }

        [Fact]
        public void Eine_zu_grosse_Datei_wird_vor_dem_Parsen_benannt_abgelehnt()
        {
            byte[] inhalt = File.ReadAllBytes(Probe("gbxml_haus_si.xml"));
            var profil = new Pruefprofil(1000);
            Assert.False(GebaeudeImportAblauf.GroesseZulaessig(inhalt.LongLength, profil));

            foreach (Stream strom in new Stream[] { new MemoryStream(inhalt), new OhneLaenge(new MemoryStream(inhalt)) })
            {
                var a = new GebaeudeImportAblauf();
                int n = a.Lesen(strom, "gbxml_haus_si.xml", profil);
                Assert.Equal(0, n);
                Assert.Null(a.Abbild);
                PruefMeldung m = a.Meldungen.Single();
                Assert.Equal(PruefStufe.Fehler, m.Stufe);
                Assert.Equal(P + "ZU_GROSS", m.Schluessel);
                Assert.Equal("1000", m.Werte[1]);
            }
            Assert.Equal(0, profil.Aufrufe);   // der Leser wurde nie gerufen

            // Innerhalb der Grenze liest derselbe Weg.
            var gross = new Pruefprofil(inhalt.LongLength);
            Assert.Equal(1, new GebaeudeImportAblauf().Lesen(new OhneLaenge(new MemoryStream(inhalt)), "x.xml", gross));
            Assert.Equal(1, gross.Aufrufe);
        }

        [Fact]
        public void Die_Grenzen_je_Plattform_stehen_im_Profil()
        {
            Assert.Equal(25L * 1024 * 1024, GbxmlImportProfil.MAX_BYTES_WINDOWS);
            Assert.Equal(25L * 1024 * 1024, GbxmlImportProfil.MAX_BYTES_IOS);
            Assert.Equal(GbxmlImportProfil.MAX_BYTES_WINDOWS, new GbxmlImportProfil().MaxBytes);
            Assert.Equal(GbxmlImportProfil.MAX_BYTES_IOS, new GbxmlImportProfil(GbxmlImportProfil.MAX_BYTES_IOS).MaxBytes);
            var p = new GbxmlImportProfil();
            Assert.Equal("GBXML", p.Format);
            Assert.Equal("(*.xml;*.gbxml)|*.xml;*.gbxml", p.Dateifilter);
            Assert.Equal(new[] { "X4" }, p.Zonierungsregeln);
            Assert.Equal("IMP_GBXML_PROT_ZU_GROSS", p.Meldung("ZU_GROSS"));
            Assert.IsType<GbxmlLeser>(p.LeserErzeugen());
        }

        [Theory]
        [InlineData("<gbXML><Campus id=\"c\">")]
        [InlineData("kein XML")]
        public void Ein_Lesefehler_ist_eine_Meldung_keine_Ausnahme(string xml)
        {
            var a = new GebaeudeImportAblauf();
            int n = a.Lesen(new MemoryStream(Encoding.UTF8.GetBytes(xml)), "kaputt.xml", new GbxmlImportProfil());
            Assert.Equal(0, n);
            Assert.Null(a.Abbild);
            Assert.Empty(a.Gebaeude);
            Assert.Contains(a.Meldungen, m => m.Stufe == PruefStufe.Fehler && m.Schluessel == P + "LESEFEHLER");
        }

        [Theory]
        [InlineData("<!DOCTYPE gbXML [<!ENTITY fremd SYSTEM \"http://127.0.0.1:9/fremd.xml\">]><gbXML version=\"0.37\">&fremd;</gbXML>")]
        [InlineData("<!DOCTYPE gbXML [<!ENTITY a \"aaaaaaaaaa\"><!ENTITY b \"&a;&a;&a;&a;\">]><gbXML version=\"0.37\">&b;</gbXML>")]
        public void Eine_DTD_ist_ein_benannter_Lesefehler_ohne_Netzzugriff(string xml)
        {
            var a = new GebaeudeImportAblauf();
            Assert.Equal(0, a.Lesen(new MemoryStream(Encoding.UTF8.GetBytes(xml)), "dtd.xml", new GbxmlImportProfil()));
            PruefMeldung m = a.Meldungen.Single();
            Assert.Equal(PruefStufe.Fehler, m.Stufe);
            Assert.Equal(P + "LESEFEHLER", m.Schluessel);
            Assert.Contains("DTD", m.Werte[0], StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public void Eine_Datei_ohne_Gebaeude_ist_KEIN_CAMPUS()
        {
            var a = new GebaeudeImportAblauf();
            string xml = "<gbXML xmlns=\"http://www.gbxml.org/schema\" version=\"0.37\" lengthUnit=\"Meters\"><Campus id=\"c\"/></gbXML>";
            Assert.Equal(0, a.Lesen(new MemoryStream(Encoding.UTF8.GetBytes(xml)), "leer.xml", new GbxmlImportProfil()));
            Assert.Contains(a.Meldungen, m => m.Stufe == PruefStufe.Fehler && m.Schluessel == P + "KEIN_CAMPUS");
            Assert.Throws<InvalidOperationException>(() => a.Zuordnen(0, 'E'));
        }

        [Fact]
        public void Ein_Versionswert_ausserhalb_der_Liste_wird_nur_vermerkt()
        {
            string xml = Klein.Datei(Klein.Wand("aw-1", 0, "<Width>10</Width><Height>2.5</Height>"), version: "8.01");
            GebaeudeImportAblauf a = Klein.Lesen(xml);
            Assert.Single(a.Gebaeude);
            PruefMeldung m = a.Meldungen.Single(x => x.Schluessel == P + "VERSION_UNBEKANNT");
            Assert.Equal(PruefStufe.Info, m.Stufe);
            Assert.Equal("8.01", m.Werte[0]);
            Assert.Equal("8.01", a.Quelle.Schemastand);
            Assert.DoesNotContain(Klein.Lesen(Klein.Datei(Klein.Wand("aw-1", 0, "<Width>1</Width><Height>1</Height>"))).Meldungen,
                                  x => x.Schluessel == P + "VERSION_UNBEKANNT");
        }

        [Fact]
        public void Ein_Abbruch_wirft_OperationCanceledException()
        {
            using var quelle = new CancellationTokenSource();
            quelle.Cancel();
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(Probe("gbxml_haus_si.xml")))
                Assert.ThrowsAny<OperationCanceledException>(() => a.Lesen(s, "gbxml_haus_si.xml", new GbxmlImportProfil(), null, quelle.Token));
            Assert.Null(a.Abbild);
            Assert.Empty(a.Gebaeude);
        }

        [Fact]
        public void Der_Melder_bekommt_Beginn_und_Ende()
        {
            var berichte = new List<ImportFortschritt>();
            var a = new GebaeudeImportAblauf();
            using (FileStream s = File.OpenRead(Probe("gbxml_haus_si.xml")))
                a.Lesen(s, "C:\\Ordner\\gbxml_haus_si.xml", new GbxmlImportProfil(), new Synchron(berichte.Add));
            Assert.Equal("IMP_GEB_PROT_LESEN", berichte.First().Schluessel);
            Assert.Equal("gbxml_haus_si.xml", berichte.First().Werte[0]);
            Assert.Equal("IMP_GEB_PROT_GELESEN", berichte.Last().Schluessel);
            Assert.Equal("1", berichte.Last().Werte[0]);
            Assert.Equal("gbxml_haus_si.xml", a.Quelle.Dateiname);
        }

        /// <summary>Ein Melder ohne Synchronisationskontext — <see cref="Progress{T}"/> meldete verzögert.</summary>
        private sealed class Synchron : IProgress<ImportFortschritt>
        {
            private readonly Action<ImportFortschritt> _ziel;
            internal Synchron(Action<ImportFortschritt> ziel) { _ziel = ziel; }
            public void Report(ImportFortschritt value) => _ziel(value);
        }

        // ==================================================================
        //  Probe 4 — der Import validiert nie und liest nie als Text
        // ==================================================================

        [Fact]
        public void Probe4_der_Leser_validiert_nie_und_liest_die_Datei_nie_als_Text()
        {
            string ordner = Path.Combine(Wurzel(), "EPOS.Kern", "Allgemein", "Import", "Gbxml");
            string[] dateien = Directory.GetFiles(ordner, "*.cs");
            Assert.Contains(dateien, d => Path.GetFileName(d) == "GbxmlLeser.cs");

            var funde = new List<string>();
            foreach (string datei in dateien)
            {
                string text = File.ReadAllText(datei);
                foreach (string verboten in new[] { "XmlSchemaSet", "ValidationType.Schema", "File.ReadAllText", "XDocument.Parse", "XElement.Parse", "DtdProcessing.Parse" })
                    if (text.Contains(verboten, StringComparison.Ordinal)) funde.Add(Path.GetFileName(datei) + ": " + verboten);
            }
            Assert.True(funde.Count == 0, "Der gbXML-Leser validiert oder liest als Text (3.1):\n" + string.Join("\n", funde));

            string leser = File.ReadAllText(Path.Combine(ordner, "GbxmlLeser.cs"));
            Assert.Contains("DtdProcessing.Prohibit", leser, StringComparison.Ordinal);
            Assert.Contains("XmlResolver = null", leser, StringComparison.Ordinal);
        }

        /// <summary>
        /// Probe 4, erweitert auf den Export (Stufe G7a; ADR-004): Auch der Schreiber validiert nie im
        /// Kern (die Schemaprüfung läuft nur im Test gegen die lokale Schemakopie, D17), kennt keinen
        /// Serialisierer und keinen Dateizugriff — er schreibt LINQ to XML über einen <c>XmlWriter</c> in
        /// einen Strom.
        /// </summary>
        [Fact]
        public void Probe4_der_Export_validiert_nie_und_schreibt_ohne_Serialisierer_in_einen_Strom()
        {
            var funde = new List<string>();
            var dateien = new List<string>();
            foreach (string teil in new[] { "Gbxml", "Gebaeude" })
            {
                string ordner = Path.Combine(Wurzel(), "EPOS.Kern", "Allgemein", "Export", teil);
                Assert.True(Directory.Exists(ordner), "Der Exportordner fehlt: Export/" + teil);
                dateien.AddRange(Directory.GetFiles(ordner, "*.cs"));
            }
            Assert.Contains(dateien, d => Path.GetFileName(d) == "GbxmlSchreiber.cs");

            string[] verboten =
            {
                "XmlSchemaSet", "ValidationType.Schema", "XmlSerializer", "DataContractSerializer", "XDocument.Parse",
                "XElement.Parse", "DtdProcessing.Parse", "File.", "Directory.", "FileStream",
            };
            foreach (string datei in dateien)
            {
                string text = File.ReadAllText(datei);
                foreach (string v in verboten)
                    if (text.Contains(v, StringComparison.Ordinal)) funde.Add(Path.GetFileName(datei) + ": " + v);
            }
            Assert.True(funde.Count == 0, "Der gbXML-Export validiert, serialisiert oder greift auf Dateien zu (ADR-004, D17):\n" + string.Join("\n", funde));

            string schreiber = File.ReadAllText(dateien.Single(d => Path.GetFileName(d) == "GbxmlSchreiber.cs"));
            Assert.Contains("XmlWriter.Create", schreiber, StringComparison.Ordinal);
            Assert.Contains("new UTF8Encoding(false)", schreiber, StringComparison.Ordinal);
        }

        private static string Wurzel([CallerFilePath] string eigeneDatei = null)
        {
            string ordner = Path.GetDirectoryName(eigeneDatei);
            while (ordner != null && !File.Exists(Path.Combine(ordner, "WP-Plan.Kern.slnf")))
                ordner = Path.GetDirectoryName(ordner);
            Assert.True(ordner != null, "Die Wurzel des Arbeitsbaums ist nicht zu finden.");
            return ordner;
        }
    }

    /// <summary>Kleinstdateien für die Regelproben — ein Gebäude, ein beheizter Raum, Flächen nach Wahl.</summary>
    internal static class Klein
    {
        internal static string Datei(string flaechen, string raum = null, string laenge = "Meters", string version = "0.37",
                                     string katalog = "", string nord = "0", string weitereRaeume = "")
        {
            raum ??= "<Space id=\"raum-1\" conditionType=\"Heated\"><Name>Wohnraum</Name><Area>50</Area><Volume>125</Volume></Space>";
            return "<?xml version=\"1.0\" encoding=\"UTF-8\"?>"
                 + "<gbXML xmlns=\"http://www.gbxml.org/schema\" version=\"" + version + "\" temperatureUnit=\"C\" lengthUnit=\"" + laenge
                 + "\" areaUnit=\"SquareMeters\" volumeUnit=\"CubicMeters\" useSIUnitsForResults=\"true\">"
                 + "<Campus id=\"c\"><Location><ZipcodeOrPostalCode>0</ZipcodeOrPostalCode>"
                 + (nord == null ? "" : "<CADModelAzimuth>" + nord + "</CADModelAzimuth>") + "</Location>"
                 + "<Building id=\"geb-1\" buildingType=\"SingleFamily\"><Name>Haus</Name>" + raum + weitereRaeume + "</Building>"
                 + flaechen + "</Campus>" + katalog + "</gbXML>";
        }

        /// <summary>Eine senkrechte Fläche mit freiem Geometrieinhalt.</summary>
        internal static string Wand(string id, double? azimut, string masse, string typ = "ExteriorWall", string kon = null,
                                    string oeffnungen = "", params string[] nachbarn)
        {
            if (nachbarn.Length == 0) nachbarn = new[] { "raum-1" };
            return "<Surface id=\"" + id + "\" surfaceType=\"" + typ + "\"" + (kon == null ? "" : " constructionIdRef=\"" + kon + "\"") + ">"
                 + string.Concat(nachbarn.Select(n => "<AdjacentSpaceId spaceIdRef=\"" + n + "\"/>"))
                 + "<RectangularGeometry>" + (azimut.HasValue ? "<Azimuth>" + azimut.Value.ToString(System.Globalization.CultureInfo.InvariantCulture) + "</Azimuth>" : "")
                 + "<Tilt>90</Tilt>" + masse + "</RectangularGeometry>" + oeffnungen + "</Surface>";
        }

        internal static GebaeudeImportAblauf Lesen(string xml, GebaeudeImportProfil profil = null)
        {
            var a = new GebaeudeImportAblauf();
            a.Lesen(new MemoryStream(Encoding.UTF8.GetBytes(xml)), "probe.xml", profil ?? new GbxmlImportProfil());
            return a;
        }
    }
}
