using System;
using System.IO;
using System.Linq;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Die HottCAD-Kennung im IFC-Abbild</b> (Befund HottCAD-Projektdatei N.6): <c>HSETU_BauteilAllgemein.GUID</c> am
    /// <c>IfcSpace</c> und am Bauteil kommt in der Normalform an (<see cref="IfcAbbildBauer.GuidNormalform"/>); ein Text, der
    /// keine GUID ist, und ein fehlender Satz bleiben <c>null</c>. Probe <see cref="IfcProbenErzeuger.HottcadKennung"/>.
    /// </summary>
    public sealed class IfcAbbildHottcadGuidTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new();

        public void Dispose() => _kultur.Dispose();

        private static AbbildGebaeude Lesen()
        {
            var a = new GebaeudeImportAblauf();
            using (var s = new MemoryStream(IfcProbenErzeuger.HottcadKennung()))
                a.Lesen(s, "ifc4_hottcad_guid.ifc", new IfcImportProfil());
            return a.Abbild.Gebaeude[0];
        }

        [Fact]
        public void Die_GUID_der_Raeume_kommt_in_der_Normalform_an()
        {
            AbbildGebaeude g = Lesen();
            string Guid(string name) => g.Raeume.Single(r => r.Name == name).HottcadGuid;
            Assert.Equal("3b6425e5-4435-40c6-8821-6f4e16e47855", Guid("Wohnen"));
            Assert.Equal("adef8666-4b52-4f84-b45f-6fc96a684d24", Guid("Küche"));
            Assert.Null(Guid("Bad"));
            Assert.Null(Guid("Flur"));
        }

        [Fact]
        public void Die_GUID_der_Bauteile_kommt_in_der_Normalform_an()
        {
            AbbildGebaeude g = Lesen();
            Assert.Equal("ef9ba72c-b7c1-4d90-8d1f-b7f0ee884795", g.Bauteile.Single(b => b.Name == "Außenwand").HottcadGuid);
            Assert.Null(g.Bauteile.Single(b => b.Name == "Bodenplatte").HottcadGuid);
        }
    }
}
