using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die Probensätze des Gebäudeexports ohne Datenbank</b> (Stufe G7a, Welle 2): ein Satz, wie ihn
    /// <see cref="GebaeudeExportSatz.Lesen"/> aus der Datenbank läse — Projektgebäude, eine Zone mit
    /// Bauteilen, die Aufbauten und Baustoffe des Projekts. Zwei Gebäude: das <b>Schichtenhaus</b>
    /// (Aufbauten mit Innendämmung und ruhender Luftschicht, Fenster und Tür, Wand gegen unbeheizt,
    /// Innenpaar mit gespiegeltem Aufbau, Innendecke ohne Partner) und das <b>U-Wert-Haus</b> (nur
    /// U-Werte — die Ersatzschichtung trägt die Masse, dazu die Ersatzfläche innerer Masse).
    /// </summary>
    internal static class ExportSatzProbe
    {
        internal const int GEB = 501;
        internal const int ZONE = 601;
        internal const int AUFBAU_WAND = 701, AUFBAU_DECKE = 702, AUFBAU_INNEN = 703, AUFBAU_INNEN_GESPIEGELT = 704;
        internal const double NUTZFLAECHE = 120.0;
        internal const double BAUWEISE_WHK = NUTZFLAECHE * 60.0;

        /// <summary>Das Projektgebäude, wie der Lauf es liest.</summary>
        internal static ProjektGebaeudeModel Gebaeude(int id = GEB) => new ProjektGebaeudeModel
        {
            ID_Projekt = 1, ID_Gebaeude = id, Gebaeudename = "Probehaus", Gebaeudeart = "Einfamilienhaus",
            Nutzflaeche = NUTZFLAECHE, Raumhoehe = 2.5, Bauweise = BAUWEISE_WHK, Interne_Waermegewinne = 600.0,
            Flaeche_Nutzer = 40.0, Raumsolltemperatur_Tag = 20.0, Raumsolltemperatur_Nachtabsenkung = 16.0,
            Luftwechsel_Infiltration = 0.2, Fensterdurchlassgrad = 0.6, Kuehl_Sollwert = 26.0, Kuehlung_Aktiv = true,
            Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007,
        };

        /// <summary>Der Satz eines Gebäudes mit einer Zone.</summary>
        internal static GebaeudeExportSatz Satz(ZoneModel zone, IEnumerable<BauteilaufbauModel> aufbauten = null,
                                                string plz = "01067", bool kuehlbetrieb = false, ProjektGebaeudeModel gebaeude = null,
                                                IEnumerable<ZoneModel> weitere = null)
        {
            var zonen = new List<ZoneModel> { zone };
            if (weitere != null) zonen.AddRange(weitere);
            return new GebaeudeExportSatz
            {
                IdProjekt = 1, IdZ = 1,
                Gebaeude = gebaeude ?? Gebaeude(),
                Zonen = zonen,
                Aufbauten = (aufbauten ?? Aufbauten()).ToDictionary(a => a.ID),
                Baustoffe = new Dictionary<int, BaustoffModel>
                {
                    [801] = new BaustoffModel { ID = 801, Bezeichner = "Gipskarton" },
                    [802] = new BaustoffModel { ID = 802, Bezeichner = "Hochlochziegel" },
                },
                Kuehlbetrieb = kuehlbetrieb,
                Klimaregion = "Probenregion",
                Plz = plz,
            };
        }

        /// <summary>Die Aufbauten des Projekts; die Schicht-IDs lassen sich versetzen (neu gespeichert).</summary>
        internal static List<BauteilaufbauModel> Aufbauten(int schichtId = 9000)
        {
            int n = schichtId;
            BauteilschichtModel S(int reihenfolge, double d, double? l, double? r, double? c, int? stoff = null, bool luft = false)
                => new BauteilschichtModel { ID = n++, Reihenfolge = reihenfolge, Dicke = d, Lambda = l, Rho = r, Cp = c, ID_Baustoff = stoff, IstLuftschicht = luft };
            var wand = new BauteilaufbauModel
            {
                ID = AUFBAU_WAND, Bezeichner = "Außenwand Innendämmung", Bauteilart = DbWerte.BAUTEILART_AUSSENWAND,
                Schichten =
                {
                    S(1, 0.0125, 0.25, 900.0, 1000.0, 801), S(2, 0.06, 0.035, 30.0, 1030.0),
                    S(3, 0.02, null, null, null, luft: true), S(4, 0.24, 0.5, 1200.0, 1000.0, 802), S(5, 0.02, 0.87, 1800.0, 1000.0),
                },
            };
            var decke = new BauteilaufbauModel
            {
                ID = AUFBAU_DECKE, Bezeichner = "Massivdecke",
                Schichten = { S(1, 0.01, 0.7, 1400.0, 1000.0), S(2, 0.18, 2.3, 2400.0, 1000.0), S(3, 0.01, 0.7, 1400.0, 1000.0) },
            };
            var innen = new BauteilaufbauModel
            {
                ID = AUFBAU_INNEN, Bezeichner = "Innenwand gedämmt",
                Schichten = { S(1, 0.115, 0.99, 1800.0, 1000.0), S(2, 0.04, 0.04, 30.0, 1400.0) },
            };
            var gespiegelt = new BauteilaufbauModel
            {
                ID = AUFBAU_INNEN_GESPIEGELT, Bezeichner = "Innenwand gedämmt (Gegenseite)",
                Schichten = { S(1, 0.04, 0.04, 30.0, 1400.0), S(2, 0.115, 0.99, 1800.0, 1000.0) },
            };
            return new List<BauteilaufbauModel> { wand, decke, innen, gespiegelt };
        }

        internal static BauteilModel B(int id, string art, double flaeche, string rand, int? aufbau = null, double? u = null,
                                       double? neigung = null, double? azimut = null, int? nachbarzone = null)
            => new BauteilModel
            {
                ID = id, ID_Zone = ZONE, Bezeichner = "Bauteil " + id.ToString(CultureInfo.InvariantCulture), Bauteilart = art,
                Flaeche = flaeche, Randbedingung = rand, ID_Aufbau = aufbau, U_Wert = u, Neigung = neigung, Azimut = azimut,
                ID_Nachbarzone = nachbarzone,
            };

        /// <summary>
        /// <b>Das Schichtenhaus</b>: vier Außenwände (Süd mit Fenster und Tür), Dach, Bodenplatte, eine Wand
        /// gegen unbeheizt, ein Innenpaar mit gespiegeltem Aufbau (je 40 m²) und eine Innendecke ohne
        /// Partner (60 m²). Die Tür trägt nur einen U-Wert in einer Gruppe mit Schichten (gemischt).
        /// </summary>
        internal static ZoneModel Schichtenhaus(int id = ZONE)
        {
            var z = new ZoneModel { ID = id, ID_Gebaeude = GEB, Rang = 1, Bezeichner = "Wohnen", IstBeheizt = true };
            z.Bauteile.AddRange(new[]
            {
                B(1001, DbWerte.BAUTEILART_AUSSENWAND, 20.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, AUFBAU_WAND, neigung: 90.0, azimut: 180.0),
                B(1002, DbWerte.BAUTEILART_FENSTER, 4.0, null, u: 1.1, neigung: 90.0, azimut: 180.0),
                B(1003, DbWerte.BAUTEILART_TUER, 2.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, u: 1.8, neigung: 90.0, azimut: 180.0),
                B(1004, DbWerte.BAUTEILART_AUSSENWAND, 26.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, AUFBAU_WAND, neigung: 90.0, azimut: 0.0),
                B(1005, DbWerte.BAUTEILART_AUSSENWAND, 20.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, AUFBAU_WAND, neigung: 90.0, azimut: 90.0),
                B(1006, DbWerte.BAUTEILART_AUSSENWAND, 18.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, AUFBAU_WAND, neigung: 90.0, azimut: 270.0),
                B(1007, DbWerte.BAUTEILART_DACH, 120.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, AUFBAU_DECKE, neigung: 0.0),
                B(1008, DbWerte.BAUTEILART_BODENPLATTE, 120.0, DbWerte.RANDBEDINGUNG_ERDREICH, AUFBAU_DECKE, neigung: 180.0),
                B(1009, DbWerte.BAUTEILART_AUSSENWAND, 8.0, DbWerte.RANDBEDINGUNG_UNBEHEIZT, AUFBAU_WAND, neigung: 90.0, azimut: 270.0),
                B(1010, DbWerte.BAUTEILART_INNENWAND, 40.0, null, AUFBAU_INNEN, neigung: 90.0),
                B(1011, DbWerte.BAUTEILART_INNENWAND, 40.0, null, AUFBAU_INNEN_GESPIEGELT, neigung: 90.0),
                B(1012, DbWerte.BAUTEILART_DECKE, 60.0, null, AUFBAU_DECKE, neigung: 0.0),
            });
            return z;
        }

        /// <summary>
        /// <b>Das U-Wert-Haus</b>: vier Außenwände zu 30 m² (U 0,3), ein Südfenster (6 m², U 1,3), Dach und
        /// Bodenplatte zu 120 m² (U 0,2 bzw. 0,35) — keine Schichten, keine Innenbauteile.
        /// </summary>
        internal static ZoneModel UWertHaus()
        {
            var z = new ZoneModel { ID = ZONE, ID_Gebaeude = GEB, Rang = 1, Bezeichner = "Wohnen", IstBeheizt = true };
            z.Bauteile.AddRange(new[]
            {
                B(2001, DbWerte.BAUTEILART_AUSSENWAND, 30.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, u: 0.3, neigung: 90.0, azimut: 0.0),
                B(2002, DbWerte.BAUTEILART_AUSSENWAND, 30.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, u: 0.3, neigung: 90.0, azimut: 90.0),
                B(2003, DbWerte.BAUTEILART_AUSSENWAND, 30.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, u: 0.3, neigung: 90.0, azimut: 180.0),
                B(2004, DbWerte.BAUTEILART_AUSSENWAND, 30.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, u: 0.3, neigung: 90.0, azimut: 270.0),
                B(2005, DbWerte.BAUTEILART_FENSTER, 6.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, u: 1.3, neigung: 90.0, azimut: 180.0),
                B(2006, DbWerte.BAUTEILART_DACH, 120.0, DbWerte.RANDBEDINGUNG_AUSSENLUFT, u: 0.2, neigung: 0.0),
                B(2007, DbWerte.BAUTEILART_BODENPLATTE, 120.0, DbWerte.RANDBEDINGUNG_ERDREICH, u: 0.35, neigung: 180.0),
            });
            return z;
        }

        /// <summary>Bereitet vor und verlangt einen schreibbaren Plan.</summary>
        internal static GebaeudeExportPlan Plan(GebaeudeExportSatz satz, GebaeudeExportProfil profil = null)
        {
            GebaeudeExportPlan plan = new GebaeudeExportAblauf().Vorbereiten(satz, profil ?? GbxmlExportProbe.Profil());
            Assert.False(plan.Abgelehnt, plan.Ablehnung?.ToString());
            return plan;
        }

        /// <summary>Schreibt einen Plan in den Speicher.</summary>
        internal static byte[] Datei(GebaeudeExportPlan plan, GebaeudeExportProfil profil = null)
        {
            using (var ziel = new MemoryStream())
            {
                new GebaeudeExportAblauf().Schreiben(plan, ziel, profil ?? GbxmlExportProbe.Profil(), CancellationToken.None);
                return ziel.ToArray();
            }
        }

        /// <summary>Der Rückimport: Leser und Bauteilvorschlag mit dem Namensabgleich der Auslieferung.</summary>
        internal static GebaeudeBauteilvorschlag Rueckimport(byte[] datei, out GbxmlAbbild abbild)
        {
            abbild = GbxmlExportProbe.Lesen(datei);
            return GebaeudeBauteilvorschlag.Bilden(abbild, 0, 'E', null, new GbxmlImportProfil(), null,
                                                   new Baustoffabgleich(BaustoffabgleichDaten.AusSaat()));
        }
    }
}
