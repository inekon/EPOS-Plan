using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using WindowsFormsApplication1;
using Xunit;
using Xunit.Abstractions;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die gbXML-Raumtyptafel</b> (Welle DA-5): nur Nummern der Tabelle 6 der DIN/TS 18599-10:2025-10 (1 bis 43), kein Name
    /// zugleich zugeordnet und mehrdeutig, Schreibung egal; liegt die Schemakopie lokal bei, steht jeder Name der Tafel in
    /// <c>spaceTypeEnum</c> (sonst benannter Verzicht wie Probe 3).
    /// </summary>
    public sealed class GbxmlRaumtyptafelTests
    {
        private readonly ITestOutputHelper _aus;

        public GbxmlRaumtyptafelTests(ITestOutputHelper aus) { _aus = aus; }

        [Fact]
        public void Tafel_traegt_nur_Normnummern_und_trennt_mehrdeutige_Typen()
        {
            Assert.NotEmpty(GbxmlRaumtyptafel.NUMMER_JE_RAUMTYP);
            Assert.All(GbxmlRaumtyptafel.NUMMER_JE_RAUMTYP.Values, n => Assert.InRange(n, 1, 43));
            Assert.Empty(GbxmlRaumtyptafel.NUMMER_JE_RAUMTYP.Keys.Where(k => GbxmlRaumtyptafel.MEHRDEUTIG.Contains(k)));
            Assert.Equal(1, GbxmlRaumtyptafel.DinNummer(" officeenclosed "));
            // Gegenprobe: mehrdeutig, unbekannt, leer → keine Nummer.
            Assert.Null(GbxmlRaumtyptafel.DinNummer("ParkingAreaPedestrianParkingGarage"));
            Assert.Null(GbxmlRaumtyptafel.DinNummer("Phantasieraum"));
            Assert.Null(GbxmlRaumtyptafel.DinNummer(null));
            Assert.False(GbxmlRaumtyptafel.Mehrdeutig("OfficeEnclosed"));
        }

        [Fact]
        public void Jeder_Name_der_Tafel_steht_in_spaceTypeEnum_der_Schemakopie()
        {
            string schema = GbxmlExportProbe.Schemakopie();
            if (schema == null)
            {
                _aus.WriteLine("Abgleich übersprungen: Referenzlaeufe/Schemakopien/" + GbxmlExportProbe.SCHEMAKOPIE
                               + " liegt nicht bei (D17, lokal beizustellen).");
                return;
            }
            XNamespace xs = "http://www.w3.org/2001/XMLSchema";
            XElement typ = XDocument.Load(schema).Descendants(xs + "simpleType").FirstOrDefault(e => (string)e.Attribute("name") == "spaceTypeEnum")
                           ?? XDocument.Load(schema).Descendants(xs + "attribute").Where(e => (string)e.Attribute("name") == "spaceType")
                                  .Select(e => e.Element(xs + "simpleType")).FirstOrDefault(e => e != null);
            Assert.NotNull(typ);
            var werte = new HashSet<string>(typ.Descendants(xs + "enumeration").Select(e => (string)e.Attribute("value")), StringComparer.Ordinal);
            List<string> fremd = GbxmlRaumtyptafel.AlleNamen.Where(n => !werte.Contains(n)).ToList();
            _aus.WriteLine("spaceTypeEnum: " + werte.Count + " Werte; in der Tafel " + GbxmlRaumtyptafel.AlleNamen.Count()
                           + "; nicht eingestuft: " + string.Join(", ", werte.Where(w => !GbxmlRaumtyptafel.AlleNamen.Contains(w, StringComparer.OrdinalIgnoreCase))));
            Assert.True(fremd.Count == 0, "Namen der Tafel, die das Schema nicht kennt: " + string.Join(", ", fremd));
        }
    }
}
