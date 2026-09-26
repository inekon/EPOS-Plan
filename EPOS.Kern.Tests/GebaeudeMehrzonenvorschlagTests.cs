using System;
using System.Collections.Generic;
using System.Linq;
using SpeicherEngine;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Stufe G6c, Welle A — der Bauteilvorschlag mehrerer Zonen</b> (<see cref="GebaeudeBauteilvorschlag.BildenMitZonen"/>)
    /// ohne Datenbank: je Zone Bauteile, Trennflächen mit Nachbarzone und Randbedingung <c>ZONE</c>, die
    /// innere Masse nach E45, die Paarungen der Räume; der Einzonenfall Z5 bzw. X4 bleibt der Vorschlag aus G4b.
    /// </summary>
    public sealed class GebaeudeMehrzonenvorschlagTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung();
        private readonly ITestOutputHelper _aus;

        public GebaeudeMehrzonenvorschlagTests(ITestOutputHelper aus) => _aus = aus;

        public void Dispose() => _kultur.Dispose();

        private void Zeigen(GebaeudeBauteilvorschlag v)
        {
            foreach (PruefMeldung m in v.Meldungen) _aus.WriteLine(m.ToString());
            for (int i = 0; i < v.Zonen.Count; i++)
            {
                ZoneModel z = v.Zonen[i];
                _aus.WriteLine("Zone " + z.ID + " " + z.Bezeichner + " A=" + z.Nutzflaeche + " beheizt=" + z.IstBeheizt);
                foreach (GebaeudeBauteilzeile x in v.Zeilen.Where(x => x.Zone == i))
                    _aus.WriteLine("   " + x + " N=" + x.Bauteil.Neigung + " nz=" + x.Bauteil.ID_Nachbarzone);
            }
        }

        private static List<GebaeudeBauteilzeile> Trennzeilen(GebaeudeBauteilvorschlag v)
            => v.Zeilen.Where(z => z.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_ZONE).ToList();

        // ==================================================================
        //  Das Probenhaus (IFC, Z4)
        // ==================================================================

        [Fact]
        public void Das_Probenhaus_wird_ein_Vorschlag_mit_drei_Zonen_und_zwei_Trenndecken()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen("ifc4_haus.ifc");
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null);
            Zeigen(v);
            Assert.False(v.Abgelehnt, string.Join(" | ", v.Meldungen.Where(m => m.Stufe == PruefStufe.Fehler)));
            Assert.True(v.Mehrzonig);
            Assert.Equal(new[] { -1, -2, -3 }, v.Zonen.Select(z => z.ID));
            Assert.Equal(new[] { "Kellergeschoss", "Erdgeschoss", "Obergeschoss" }, v.Zonen.Select(z => z.Bezeichner));
            Assert.Equal(new double?[] { 70.0, 65.0, 65.0 }, v.Zonen.Select(z => z.Nutzflaeche));
            Assert.False(v.Zonen[0].IstBeheizt);
            Assert.All(v.Zonen, z => Assert.Equal(DbWerte.HERKUNFT_IFC, z.Herkunft));
            Assert.Same(v.Zonen[0], v.Zone);

            // Die Trenndecken führt die beheizte bzw. die Zone mit dem kleineren Rang — beide das Erdgeschoss.
            List<GebaeudeBauteilzeile> trenn = Trennzeilen(v);
            Assert.Equal(2, trenn.Count);
            Assert.All(trenn, z => Assert.Equal(1, z.Zone));
            GebaeudeBauteilzeile kd = Assert.Single(trenn, z => z.Bauteil.Bezeichner == "Kellerdecke");
            Assert.Equal(-1, kd.Bauteil.ID_Nachbarzone);
            Assert.Equal(180.0, kd.Bauteil.Neigung);   // der Boden des Erdgeschosses
            Assert.Equal(80.0, kd.Bauteil.Flaeche);
            GebaeudeBauteilzeile gd = Assert.Single(trenn, z => z.Bauteil.Bezeichner == "Geschossdecke");
            Assert.Equal(-3, gd.Bauteil.ID_Nachbarzone);
            Assert.Equal(0.0, gd.Bauteil.Neigung);     // die Decke des Erdgeschosses

            // Der Keller trägt seine Wände und die Bodenplatte gegen Erdreich; kein Bauteil gegen „unbeheizt".
            Assert.Equal(5, v.Zeilen.Count(z => z.Zone == 0));
            Assert.All(v.Zeilen.Where(z => z.Zone == 0), z => Assert.Equal(DbWerte.RANDBEDINGUNG_ERDREICH, z.Bauteil.Randbedingung));
            Assert.DoesNotContain(v.Zeilen, z => z.Bauteil.Randbedingung == DbWerte.RANDBEDINGUNG_UNBEHEIZT);
            // Die Räume zeigen auf ihre Zone.
            Assert.Equal(5, v.Raeume.Count);
            Assert.Equal(-1, Assert.Single(v.Raeume, r => a.Abbild.Gebaeude[0].Raeume.Single(x => x.Kennung == r.Quellkennung).Name == "Keller").ZielId);
            Assert.Equal(2, v.Raeume.Count(r => r.ZielId == -3));
            Assert.Contains(v.Meldungen, m => m.Schluessel == GebaeudeBauteilvorschlag.MEHRZONEN);
        }

        [Fact]
        public void Der_Einzonenfall_Z5_ist_der_Vorschlag_aus_G4b()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen("ifc4_haus.ifc");
            GebaeudeBauteilvorschlag alt = GebaeudeBauteilvorschlag.Bilden(a, 0, null);
            GebaeudeBauteilvorschlag neu = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null, "Z5");
            Assert.False(neu.Mehrzonig);
            Assert.Null(neu.Zonierung);
            Assert.Equal(alt.Zeilen.Select(z => z.ToString()), neu.Zeilen.Select(z => z.ToString()));
            Assert.Equal(alt.Meldungen.Select(m => m.ToString()), neu.Meldungen.Select(m => m.ToString()));
            Assert.Single(neu.Zonen);

            GebaeudeImportAblauf g = BauteilvorschlagProbe.Lesen("gbxml_haus_si.xml");
            Assert.Equal(GebaeudeBauteilvorschlag.Bilden(g, 0, null).Zeilen.Select(z => z.ToString()),
                         GebaeudeBauteilvorschlag.BildenMitZonen(g, 0, null, "X4").Zeilen.Select(z => z.ToString()));
        }

        // ==================================================================
        //  Das gbXML-Haus (X2) — Aufbauten und innere Masse
        // ==================================================================

        /// <summary>
        /// Das gbXML-Haus nach Geschossen: Zeilen, Aufbauten und Trennflächen stehen — und die Probe lehnt
        /// den Vorschlag benannt ab, weil der unbeheizte Keller als eigene Zone nur Betonwände ohne Dämmung
        /// gegen Erdreich trägt: Ihr Leitwert übersteigt den der inneren Oberfläche, Gl. (28) der VDI 6007
        /// hat dafür keinen Setzwert (im Einzonenweg liegt der Keller außerhalb der Zone).
        /// </summary>
        [Fact]
        public void Das_gbXML_Haus_nach_Geschossen_traegt_Aufbauten_und_Trennflaechen()
        {
            GebaeudeImportAblauf a = BauteilvorschlagProbe.Lesen("gbxml_haus_si.xml");
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.BildenMitZonen(a, 0, null, "X2");
            Zeigen(v);
            PruefMeldung f = Assert.Single(v.Meldungen, m => m.Stufe == PruefStufe.Fehler);
            Assert.Equal(GebaeudeBauteilvorschlag.BAUTEILWEG, f.Schluessel);
            Assert.StartsWith("Keller:", f.Werte[0], StringComparison.Ordinal);
            Assert.Equal(new[] { "Erdgeschoss", "Obergeschoss", "Keller" }, v.Zonen.Select(z => z.Bezeichner));
            Assert.Equal(new[] { true, true, false }, v.Zonen.Select(z => z.IstBeheizt));
            List<GebaeudeBauteilzeile> trenn = Trennzeilen(v);
            Assert.Equal(4, trenn.Count);   // zwei Geschossdecken zum OG, zwei Kellerdecken zum Keller
            Assert.All(trenn, z => Assert.Equal(0, z.Zone));
            Assert.Equal(2, trenn.Count(z => z.Bauteil.ID_Nachbarzone == -2));
            Assert.Equal(2, trenn.Count(z => z.Bauteil.ID_Nachbarzone == -3));
            Assert.All(trenn, z => Assert.True(z.Bauteil.ID_Aufbau.HasValue, z.ToString()));
            Assert.Equal(0.0, trenn.First(z => z.Bauteil.ID_Nachbarzone == -2).Bauteil.Neigung);
            Assert.Equal(180.0, trenn.First(z => z.Bauteil.ID_Nachbarzone == -3).Bauteil.Neigung);
            // Kellerwände und Bodenplatte in der unbeheizten Zone.
            Assert.Equal(5, v.Zeilen.Count(z => z.Zone == 2));
        }

        // ==================================================================
        //  Ablehnungen
        // ==================================================================

        [Fact]
        public void Mehr_als_fuenfzig_Zonen_werden_benannt_abgelehnt()
        {
            (GebaeudeAbbild a, AbbildGebaeude g) = GebaeudeZonierungTests.Abbild(GebaeudeQuelle.FORMAT_GBXML);
            for (int i = 0; i < 60; i++)
            {
                string r = "r" + i.ToString("D2", System.Globalization.CultureInfo.InvariantCulture);
                GebaeudeZonierungTests.Raum(g, r, 10, i < 30 ? "eg" : "og");
                AbbildBauteil w = GebaeudeZonierungTests.Bauteil(g, "aw-" + r, Bauteilart.Aussenwand, Randbedingung.Aussenluft, 10, 0.3);
                w.Nachbarn.Add(new AbbildNachbar(r, null));
            }
            GebaeudeZonierung z = GebaeudeZonierung.Bilden(a, 0, "X3");
            GebaeudeBauteilvorschlag v = GebaeudeBauteilvorschlag.Bilden(a, 0, 'H', null, new GbxmlImportProfil(), zonierung: z);
            Assert.True(v.Abgelehnt);
            PruefMeldung m = Assert.Single(v.Meldungen, x => x.Schluessel == GebaeudeBauteilvorschlag.ZU_VIELE_ZONEN);
            Assert.Equal(new[] { "60", "50", "X2" }, m.Werte);

            GebaeudeBauteilvorschlag x2 = GebaeudeBauteilvorschlag.Bilden(a, 0, 'H', null, new GbxmlImportProfil(), zonierung: GebaeudeZonierung.Bilden(a, 0, "X2"));
            Zeigen(x2);
            Assert.False(x2.Abgelehnt, string.Join(" | ", x2.Meldungen.Where(x => x.Stufe == PruefStufe.Fehler)));
            Assert.Equal(2, x2.Zonen.Count);
        }
    }
}
