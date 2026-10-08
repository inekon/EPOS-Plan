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
    /// <b>Orientierung bei Mengensatz</b> (N4, Befund G5-A 4.1): Die Kleinhausprobe mit Mengensätzen und Raumgrenzen ohne
    /// Azimut- und Neigungsangabe bekommt die Orientierung aus der Normale ihrer Raumgrenzen bzw. aus dem Bauteilkörper — die
    /// Fläche bleibt die des Mengensatzes. Ohne jede Ergänzung im Test läuft der Zonenvorschlag durch, die Außenwände tragen
    /// die Azimute ihrer Fassaden, das Pultdach 36,87° nach Süd, die Fenster die Richtung ihrer Wand, und die gespeicherten
    /// Zeilen gleichen denen des Körperwegs der Probe ohne Mengensätze in Fläche, Neigung, Azimut, Randbedingung und Nachbarzone.
    /// </summary>
    [Collection("Testdatenbank")]
    public sealed class OrientierungMengensatzTests : IDisposable
    {
        private const int PROJEKT = 1045;
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _ausgabe;

        public OrientierungMengensatzTests(ITestOutputHelper ausgabe) => _ausgabe = ausgabe;

        public void Dispose() => _kultur.Dispose();

        private static string F(double? w) => w.HasValue ? w.Value.ToString("0.###", CultureInfo.InvariantCulture) : "-";

        private static readonly double DACHNEIGUNG = Math.Acos(0.8) * 180.0 / Math.PI;

        [Fact]
        public void Abbild_traegt_Azimut_je_Fassade_Dachneigung_und_Fensterrichtung()
        {
            GebaeudeImportAblauf a = KoerperflaechenTests.Lesen(KoerperflaechenTests.MIT);
            List<AbbildBauteil> bauteile = a.Abbild.Gebaeude.SelectMany(g => g.Bauteile).ToList();
            foreach (AbbildBauteil b in bauteile)
                _ausgabe.WriteLine(b.Name + " | " + b.Art + " | " + b.Randbedingung + " | N " + F(b.NeigungGrad) + " | A " + F(b.AzimutGrad)
                    + " | Körper N " + F(b.Koerperflaeche?.NeigungGrad) + " A " + F(b.Koerperflaeche?.AzimutGrad) + " | " + b.Flaechenherkunft);

            // Die Flächen bleiben die des Mengensatzes.
            Assert.All(bauteile.Where(b => b.Koerperflaeche != null), b => Assert.Equal(Flaechenherkunft.Mengensatz, b.Flaechenherkunft));

            // Außenwände: senkrecht, Azimut der Fassade aus dem Namen (Süd 180°, Nord 0°, West 270°, Ost 90°).
            List<AbbildBauteil> waende = bauteile.Where(b => b.Art == Bauteilart.Aussenwand).ToList();
            Assert.NotEmpty(waende);
            foreach (AbbildBauteil w in waende)
            {
                double soll = w.Name.Contains("Süd", StringComparison.Ordinal) ? 180.0 : w.Name.Contains("Nord", StringComparison.Ordinal) ? 0.0
                            : w.Name.Contains("West", StringComparison.Ordinal) ? 270.0 : 90.0;
                Assert.True(w.AzimutGrad.HasValue, w.Name + ": kein Azimut");
                Assert.Equal(soll, w.AzimutGrad.Value, 6);
                Assert.Equal(90.0, w.NeigungGrad.Value, 6);
            }

            // Dach: Neigung 36,87° nach Süd.
            AbbildBauteil dach = Assert.Single(bauteile, b => b.Art == Bauteilart.Dach);
            Assert.Equal(DACHNEIGUNG, dach.NeigungGrad.Value, 6);
            Assert.Equal(180.0, dach.AzimutGrad.Value, 6);

            // Fenster: senkrecht mit der Richtung ihrer Wand.
            var fenster = bauteile.SelectMany(b => b.Oeffnungen.Where(o => o.Art == Bauteilart.Fenster).Select(o => (Wirt: b, Fenster: o))).ToList();
            Assert.Equal(3, fenster.Count);
            Assert.All(fenster, f =>
            {
                Assert.True(f.Fenster.AzimutGrad.HasValue, f.Fenster.Name + ": kein Azimut");
                Assert.Equal(f.Wirt.AzimutGrad.Value, f.Fenster.AzimutGrad.Value, 6);
                Assert.Equal(90.0, f.Fenster.NeigungGrad.Value, 6);
            });

            // Die Herkunft steht als Info, keine Warnung „Seite unbestimmt“ bleibt.
            PruefMeldung info = Assert.Single(a.Abbild.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_ORIENTIERUNG_ERGAENZT");
            Assert.Equal(PruefStufe.Info, info.Stufe);
            _ausgabe.WriteLine("Info: " + string.Join("; ", info.Werte));
            Assert.DoesNotContain(a.Abbild.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_SEITE_UNBESTIMMT");
        }

        [Fact]
        public void Probe_ohne_Mengensaetze_bleibt_ohne_Ergaenzung()
        {
            GebaeudeImportAblauf a = KoerperflaechenTests.Lesen(KoerperflaechenTests.OHNE);
            Assert.DoesNotContain(a.Abbild.Meldungen, m => m.Schluessel == "IMP_IFC_PROT_ORIENTIERUNG_ERGAENZT");
        }

        [Fact]
        public void Zonenvorschlag_laeuft_durch_und_gleicht_dem_Koerperweg_je_Zeile()
        {
            Lauf mit = Rechnen(KoerperflaechenTests.Lesen(KoerperflaechenTests.MIT));
            if (mit == null) return;
            Assert.False(mit.Abgelehnt, mit.Ablehnung);
            Lauf koerper = Rechnen(KoerperflaechenTests.Lesen(KoerperflaechenTests.OHNE));
            Assert.False(koerper.Abgelehnt, koerper.Ablehnung);
            _ausgabe.WriteLine("Mengensatz: " + F(mit.Mwh) + " MWh/a, Spitze " + F(mit.SpitzeKw) + " kW; Körperweg: " + F(koerper.Mwh)
                + " MWh/a, Spitze " + F(koerper.SpitzeKw) + " kW");
            for (int i = 0; i < Math.Max(mit.Zeilen.Count, koerper.Zeilen.Count); i++)
                _ausgabe.WriteLine((i < mit.Zeilen.Count ? mit.Zeilen[i].ToString() : "-") + "  ||  " + (i < koerper.Zeilen.Count ? koerper.Zeilen[i].ToString() : "-"));

            Assert.Equal(koerper.Zeilen.Count, mit.Zeilen.Count);
            for (int i = 0; i < mit.Zeilen.Count; i++)
            {
                Zeile x = mit.Zeilen[i], y = koerper.Zeilen[i];
                Assert.Equal(y.Zone, x.Zone);
                Assert.Equal(y.Name, x.Name);
                Assert.Equal(y.Art, x.Art);
                Assert.Equal(y.Flaeche, x.Flaeche, 3);
                Assert.Equal(y.Neigung.HasValue, x.Neigung.HasValue);
                if (y.Neigung.HasValue) Assert.Equal(y.Neigung.Value, x.Neigung.Value, 3);
                Assert.Equal(y.Azimut.HasValue, x.Azimut.HasValue);
                if (y.Azimut.HasValue) Assert.Equal(y.Azimut.Value, x.Azimut.Value, 3);
                Assert.Equal(y.Rand, x.Rand);
                Assert.Equal(y.Nachbarzone, x.Nachbarzone);
            }
            Assert.Equal(koerper.Mwh, mit.Mwh, 3);
            Assert.Equal(koerper.SpitzeKw, mit.SpitzeKw, 3);
        }

        private sealed class Zeile
        {
            public string Zone, Name, Art, Rand, Nachbarzone;
            public double Flaeche;
            public double? Neigung, Azimut;

            public override string ToString() => Zone + " | " + Name + " | " + Art + " | " + F(Flaeche) + " | N " + F(Neigung) + " | A " + F(Azimut)
                                                 + " | " + Rand + (Nachbarzone != null ? " → " + Nachbarzone : "");
        }

        private sealed class Lauf
        {
            public bool Abgelehnt;
            public string Ablehnung;
            public double Mwh, SpitzeKw;
            public List<Zeile> Zeilen = new List<Zeile>();
        }

        /// <summary>Schreibt den Zonenvorschlag in eine frische Arbeitskopie, liest die Bauteile zurück und rechnet nach VDI 6007.</summary>
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
                    "SELECT z.Bezeichner AS Zone, b.Bezeichner, b.Bauteilart, b.Flaeche, b.Neigung, b.Azimut, b.Randbedingung, n.Bezeichner AS Nachbar " +
                    "FROM Tab_Bauteil b JOIN Tab_Zone z ON z.ID = b.ID_Zone LEFT JOIN Tab_Zone n ON n.ID = b.ID_Nachbarzone " +
                    "WHERE z.ID_Gebaeude = ? ORDER BY z.Rang, b.Rang, b.ID", new DbParam("@g", g.ID_Gebaeude));
                foreach (DataRow r in t.Rows)
                    l.Zeilen.Add(new Zeile
                    {
                        Zone = r[0] as string, Name = r[1] as string, Art = r[2] as string, Flaeche = Convert.ToDouble(r[3], CultureInfo.InvariantCulture),
                        Neigung = r[4] is DBNull ? (double?)null : Convert.ToDouble(r[4], CultureInfo.InvariantCulture),
                        Azimut = r[5] is DBNull ? (double?)null : Convert.ToDouble(r[5], CultureInfo.InvariantCulture),
                        Rand = r[6] as string, Nachbarzone = r[7] as string,
                    });

                ctrl = new ProjektGebaeudeCtrl();
                ctrl.ReadAll(PROJEKT);
                ProjektGebaeudeModel mit = ctrl.items[0];
                mit.Gebaeude_Modell = DbWerte.GEBAEUDE_MODELL_VDI6007;
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
                return l;
            }
        }
    }
}
