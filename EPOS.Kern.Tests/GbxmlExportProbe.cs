using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Schema;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Vorrichtung der gbXML-Exportproben</b> (Stufe G7a): Profil mit fester Uhr, das
    /// Probenabbild eines Gebäudes, Schreiben und Rücklesen über den eigenen Leser und — nur lokal —
    /// die Schemaprüfung gegen die Schemakopie unter <c>Referenzlaeufe/Schemakopien/</c> (D17).
    /// </summary>
    internal static class GbxmlExportProbe
    {
        /// <summary>Die feste Uhr der Proben.</summary>
        internal static readonly DateTime Zeitpunkt = new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Unspecified);

        /// <summary>Die Programmversion der Proben.</summary>
        internal const string VERSION = "0.0-probe";

        /// <summary>Ein Profil mit fester Uhr.</summary>
        internal static GebaeudeExportProfil Profil(DateTime? zeit = null, string sprache = "de-DE", bool testlizenz = false)
        {
            DateTime t = zeit ?? Zeitpunkt;
            return new GebaeudeExportProfil(CultureInfo.GetCultureInfo(sprache), () => t, testlizenz, VERSION);
        }

        /// <summary>Schreibt ein Abbild in den Speicher.</summary>
        internal static byte[] Schreiben(GebaeudeAbbild abbild, GebaeudeExportProfil profil = null)
            => Schreiben(abbild, profil, out _);

        /// <summary>Schreibt ein Abbild in den Speicher und liefert die Bilanz.</summary>
        internal static byte[] Schreiben(GebaeudeAbbild abbild, GebaeudeExportProfil profil, out GebaeudeExportBilanz bilanz)
        {
            using (var ziel = new MemoryStream())
            {
                bilanz = new GbxmlSchreiber().Schreiben(abbild, ziel, profil ?? Profil(), CancellationToken.None);
                return ziel.ToArray();
            }
        }

        /// <summary>Liest eine geschriebene Datei mit dem eigenen Leser.</summary>
        internal static GbxmlAbbild Lesen(byte[] datei)
            => (GbxmlAbbild)new GbxmlLeser().Lesen(new MemoryStream(datei), new GbxmlImportProfil(), null, CancellationToken.None);

        // ==================================================================
        //  Das Probenabbild
        // ==================================================================

        internal const int GEB = 7;
        internal const int ZONE = 11;

        /// <summary>
        /// <b>Das Probenabbild:</b> Gebäude 7 mit einer beheizten Zone 11 (100 m², 250 m³, 3 Personen)
        /// und dem Platzhalter „unbeheizt"; vier Außenwände (Süd mit Fenster und Tür), eine Wand und
        /// eine Kellerdecke gegen unbeheizt (diese mit gekennzeichneter Ersatzschichtung), Flachdach mit
        /// ruhender Luftschicht (nur R), Bodenplatte, eine Innenwand innerhalb der Zone. Die Außenwand
        /// ist unsymmetrisch (Innendämmung) und steht innen → außen im Abbild; dieselben Schichten
        /// tragen zwei Übergangsfälle (Außenluft, unbeheizt).
        /// </summary>
        internal static GebaeudeAbbild Abbild(string plz = "01067")
        {
            var a = new GbxmlAbbild { CampusKennung = GebaeudeExportKennung.Campus(GEB), Plz = plz, NordwinkelGrad = 0.0 };
            var g = new AbbildGebaeude
            {
                Kennung = GebaeudeExportKennung.Gebaeude(GEB), Name = "Probehaus", Art = "SingleFamily",
                Beschreibung = "Vermerk zum Campus\nzweite Zeile",
            };
            a.Gebaeude.Add(g);

            string raum = GebaeudeExportKennung.Raum(ZONE), platz = GebaeudeExportKennung.Unbeheizt(GEB);
            g.Raeume.Add(new AbbildRaum
            {
                Kennung = raum, Name = "Zone 1", FlaecheM2 = 100.0, VolumenM3 = 250.0, Beheizt = true,
                Zustandsangabe = GbxmlVokabular.Heated, LuftwechselJeH = 0.35, Personen = 3.0, GeraeteWm2 = 2.5,
                SollHeizenC = 20.0, ZonenKennung = GebaeudeExportKennung.Zone(ZONE),
                Beschreibung = "Mittelwert ohne Zeitplan", ZonenBeschreibung = "Ausweis der Zone",
            });
            g.Raeume.Add(new AbbildRaum
            {
                Kennung = platz, Name = "unbeheizt", Beheizt = false, Zustandsangabe = GbxmlVokabular.Unconditioned,
            });

            AbbildAufbau awAussen = Aussenwand(Waermestromrichtung.Horizontal, Bauteilrand.Aussenluft, 0.3);
            AbbildAufbau awUnbeheizt = Aussenwand(Waermestromrichtung.Horizontal, Bauteilrand.Unbeheizt, 0.29);

            AbbildBauteil sued = Flaeche(101, GbxmlVokabular.ExteriorWall, awAussen, 10.0, 2.5, 180.0, 90.0, N(raum));
            sued.Oeffnungen.Add(new AbbildBauteil
            {
                Kennung = GebaeudeExportKennung.Oeffnung(111), Quelltyp = "Opening", Name = "Fenster Süd",
                Quellart = GbxmlVokabular.FixedWindow, Art = Bauteilart.Fenster,
                FenstertypKennung = GebaeudeExportKennung.Fenstertyp(111),
                UWertWm2K = 1.1, GWert = 0.6, BreiteM = 2.0, HoeheM = 1.5, BruttoflaecheM2 = 3.0,
                AzimutGrad = 180.0, NeigungGrad = 90.0,
            });
            sued.Oeffnungen.Add(new AbbildBauteil
            {
                Kennung = GebaeudeExportKennung.Oeffnung(112), Quelltyp = "Opening", Name = "Haustür",
                Quellart = GbxmlVokabular.NonSlidingDoor, Art = Bauteilart.Tuer,
                UWertWm2K = 1.8, BreiteM = 1.0, HoeheM = 2.0, BruttoflaecheM2 = 2.0, AzimutGrad = 180.0, NeigungGrad = 90.0,
            });
            g.Bauteile.Add(sued);
            g.Bauteile.Add(Flaeche(102, GbxmlVokabular.ExteriorWall, awAussen, 10.0, 2.5, 0.0, 90.0, N(raum)));
            g.Bauteile.Add(Flaeche(103, GbxmlVokabular.ExteriorWall, awAussen, 8.0, 2.5, 90.0, 90.0, N(raum)));
            g.Bauteile.Add(Flaeche(104, GbxmlVokabular.ExteriorWall, awAussen, 8.0, 2.5, 270.0, 90.0, N(raum)));
            g.Bauteile.Add(Flaeche(105, GbxmlVokabular.InteriorWall, awUnbeheizt, 4.0, 2.5, 270.0, 90.0, N(raum), N(platz)));
            g.Bauteile.Add(Flaeche(106, GbxmlVokabular.Roof, Dach(), 10.0, 10.0, null, 0.0, N(raum, GbxmlVokabular.Roof)));
            g.Bauteile.Add(Flaeche(107, GbxmlVokabular.SlabOnGrade, Bodenplatte(), 10.0, 8.0, null, 180.0, N(raum, GbxmlVokabular.SlabOnGrade)));
            g.Bauteile.Add(Flaeche(108, GbxmlVokabular.InteriorFloor, Ersatz(108, 0.5), 4.0, 5.0, null, 180.0,
                                   N(raum, GbxmlVokabular.InteriorFloor), N(platz)));
            g.Bauteile.Add(Flaeche(109, GbxmlVokabular.InteriorWall, Innenwand(), 12.0, 2.5, 90.0, 90.0, N(raum), N(raum)));
            return a;
        }

        internal static AbbildNachbar N(string raum, string sicht = null) => new AbbildNachbar(raum, sicht);

        internal static AbbildBauteil Flaeche(int id, string flaechenart, AbbildAufbau aufbau, double breite, double hoehe,
                                              double? azimut, double? neigung, params AbbildNachbar[] nachbarn)
        {
            var b = new AbbildBauteil
            {
                Kennung = GebaeudeExportKennung.Bauteil(id), Quelltyp = "Surface", Name = "Bauteil " + id.ToString(CultureInfo.InvariantCulture),
                Quellart = flaechenart, Art = GbxmlVokabular.Flaechenarten[flaechenart].Art,
                Randbedingung = GbxmlVokabular.Flaechenarten[flaechenart].Rand,
                BreiteM = breite, HoeheM = hoehe, BruttoflaecheM2 = breite * hoehe, AzimutGrad = azimut, NeigungGrad = neigung,
                Aufbau = aufbau, UWertWm2K = aufbau.UWertWm2K,
            };
            b.Nachbarn.AddRange(nachbarn);
            return b;
        }

        /// <summary>Außenwand mit Innendämmung, innen → außen: Gipskarton, Dämmung, Ziegel, Putz (Aufbau 201).</summary>
        internal static AbbildAufbau Aussenwand(Waermestromrichtung richtung, Bauteilrand rand, double u)
        {
            var a = new AbbildAufbau
            {
                Kennung = GebaeudeExportKennung.Aufbau(201, richtung, rand), Name = "Außenwand Innendämmung", UWertWm2K = u,
                Richtung = Schichtrichtung.InnenNachAussen, RichtungAngenommen = false, Status = Aufbaustatus.Vollstaendig,
            };
            a.Schichten.Add(Schicht(201, 1, "Gipskarton", 0.0125, 0.25, 900.0, 1000.0));
            a.Schichten.Add(Schicht(201, 2, "Mineralwolle", 0.08, 0.035, 30.0, 1030.0));
            a.Schichten.Add(Schicht(201, 3, "Hochlochziegel", 0.24, 0.5, 1200.0, 1000.0));
            a.Schichten.Add(Schicht(201, 4, "Kalkzementputz", 0.02, 0.87, 1800.0, 1000.0));
            return a;
        }

        /// <summary>Flachdach, außen → innen: Dämmung, ruhende Luftschicht (nur R), Stahlbeton (Aufbau 202).</summary>
        internal static AbbildAufbau Dach()
        {
            var a = new AbbildAufbau
            {
                Kennung = GebaeudeExportKennung.Aufbau(202, Waermestromrichtung.Aufwaerts, Bauteilrand.Aussenluft),
                Name = "Flachdach", UWertWm2K = 0.2, Richtung = Schichtrichtung.AussenNachInnen,
            };
            a.Schichten.Add(Schicht(202, 1, "Polystyrol", 0.16, 0.035, 20.0, 1500.0));
            a.Schichten.Add(new AbbildSchicht
            {
                Kennung = GebaeudeExportKennung.SchichtRuhendeLuft(202, 2, Waermestromrichtung.Aufwaerts),
                BaustoffKennung = GebaeudeExportKennung.StoffRuhendeLuft(202, 2, Waermestromrichtung.Aufwaerts),
                Name = "Luftschicht ruhend", DickeM = 0.04, RWertM2KW = 0.16,
            });
            a.Schichten.Add(Schicht(202, 3, "Stahlbeton", 0.2, 2.3, 2400.0, 1000.0));
            return a;
        }

        /// <summary>Bodenplatte, außen → innen: Dämmung, Beton (Aufbau 203).</summary>
        internal static AbbildAufbau Bodenplatte()
        {
            var a = new AbbildAufbau
            {
                Kennung = GebaeudeExportKennung.Aufbau(203, Waermestromrichtung.Abwaerts, Bauteilrand.Erdreich),
                Name = "Bodenplatte", UWertWm2K = 0.25, Richtung = Schichtrichtung.AussenNachInnen,
            };
            a.Schichten.Add(Schicht(203, 1, "Extrudiertes Polystyrol", 0.12, 0.035, 35.0, 1450.0));
            a.Schichten.Add(Schicht(203, 2, "Beton", 0.2, 2.0, 2400.0, 1000.0));
            return a;
        }

        /// <summary>Innenwand innerhalb der Zone (Aufbau 204).</summary>
        internal static AbbildAufbau Innenwand()
        {
            var a = new AbbildAufbau
            {
                Kennung = GebaeudeExportKennung.Aufbau(204, Waermestromrichtung.Horizontal, Bauteilrand.Innen),
                Name = "Innenwand", Richtung = Schichtrichtung.InnenNachAussen,
            };
            a.Schichten.Add(Schicht(204, 1, "Kalksandstein", 0.115, 0.99, 1800.0, 1000.0));
            return a;
        }

        /// <summary>Eine gekennzeichnete Ersatzschichtung des Bauteils <paramref name="bauteil"/>.</summary>
        internal static AbbildAufbau Ersatz(int bauteil, double u)
        {
            var a = new AbbildAufbau
            {
                Kennung = GebaeudeExportKennung.ErsatzAufbau(bauteil), Name = "Ersatzschichtung", UWertWm2K = u, IstErsatz = true,
                Beschreibung = "Ersatzschichtung: trifft U-Wert und Gesamtwärmekapazität, nicht die Lage der Masse",
            };
            a.Schichten.Add(new AbbildSchicht
            {
                Kennung = GebaeudeExportKennung.ErsatzSchicht(bauteil), BaustoffKennung = GebaeudeExportKennung.ErsatzStoff(bauteil),
                Name = "Ersatzstoff", DickeM = 0.3, LambdaWmK = 0.18, RhoKgM3 = 1500.0, CpJkgK = 1000.0,
            });
            return a;
        }

        internal static AbbildSchicht Schicht(int aufbau, int reihenfolge, string name, double d, double lambda, double rho, double cp)
            => new AbbildSchicht
            {
                Kennung = GebaeudeExportKennung.Schicht(aufbau, reihenfolge), BaustoffKennung = GebaeudeExportKennung.Stoff(aufbau, reihenfolge),
                Name = name, DickeM = d, LambdaWmK = lambda, RhoKgM3 = rho, CpJkgK = cp,
            };

        // ==================================================================
        //  Die Schemakopie (nur lokal, D17)
        // ==================================================================

        /// <summary>Der Dateiname der Schemakopie.</summary>
        internal const string SCHEMAKOPIE = "GreenBuildingXML_Ver8.01.xsd";

        /// <summary>Sucht die Schemakopie vom Laufordner aufwärts; <c>null</c> = sie liegt nicht bei (CI).</summary>
        internal static string Schemakopie()
        {
            DirectoryInfo d = new DirectoryInfo(AppContext.BaseDirectory);
            for (int i = 0; i < 8 && d != null; i++, d = d.Parent)
            {
                string pfad = Path.Combine(d.FullName, "Referenzlaeufe", "Schemakopien", SCHEMAKOPIE);
                if (File.Exists(pfad)) return pfad;
            }
            return null;
        }

        /// <summary>
        /// Prüft die Datei gegen die Schemakopie und liefert jede Meldung des Prüfers (Fehler und
        /// Warnungen); die Schemakopie wird ohne Auflöser und ohne DTD geladen, nie aus dem Netz.
        /// </summary>
        internal static string[] Schemapruefung(string schemakopie, byte[] datei)
        {
            var satz = new XmlSchemaSet { XmlResolver = null };
            var ladeEinstellungen = new XmlReaderSettings { DtdProcessing = DtdProcessing.Prohibit, XmlResolver = null };
            using (XmlReader r = XmlReader.Create(schemakopie, ladeEinstellungen))
                satz.Add(null, r);
            satz.Compile();

            var befunde = new System.Collections.Generic.List<string>();
            var einstellungen = new XmlReaderSettings
            {
                ValidationType = ValidationType.Schema,
                Schemas = satz,
                DtdProcessing = DtdProcessing.Prohibit,
                XmlResolver = null,
                ValidationFlags = XmlSchemaValidationFlags.ReportValidationWarnings,
            };
            einstellungen.ValidationEventHandler += (_, e) =>
                befunde.Add(e.Severity + " " + e.Exception?.LineNumber.ToString(CultureInfo.InvariantCulture) + ": " + e.Message);
            using (XmlReader r = XmlReader.Create(new MemoryStream(datei), einstellungen))
                while (r.Read()) { }
            return befunde.ToArray();
        }

        /// <summary>Die Elemente einer Datei mit Zahlentext, in Dokumentreihenfolge (für den Kulturvergleich).</summary>
        internal static string[] Zahlen(byte[] datei)
        {
            XDocument d = XDocument.Load(new MemoryStream(datei));
            return d.Descendants()
                    .Where(e => !e.HasElements && double.TryParse(e.Value, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                    .Select(e => e.Name.LocalName + "=" + e.Value).ToArray();
        }
    }
}
