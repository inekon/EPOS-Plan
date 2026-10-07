using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using WindowsFormsApplication1;
using Xunit;

namespace EPOS.Kern.Tests
{
    public sealed class KoerperbildnerTests : IDisposable
    {
        private readonly Kulturvorrichtung _kultur = new Kulturvorrichtung("de-DE");

        public void Dispose() => _kultur.Dispose();

        /// <summary>Die Körperproben, deren Raum- und Bauteilkörper vor und nach dem Herausziehen des Ohrenschnitts byteweise gleich sind.</summary>
        private static readonly string[] IFC_PROBEN =
        {
            "ifc2x3_koerper_brep.ifc", "ifc4_koerper_abgebildet.ifc", "ifc4_koerper_bauteile.ifc", "ifc4_koerper_beschnitt.ifc",
            "ifc4_koerper_dreiecksnetz.ifc", "ifc4_koerper_extrusion_bogen.ifc", "ifc4_koerper_extrusion_loch.ifc",
            "ifc4_koerper_extrusion_polygon.ifc", "ifc4_koerper_offen.ifc", "ifc4_koerper_platzierung.ifc", "ifc4_koerper_vieleckssatz.ifc",
        };

        /// <summary>SHA-256 der Körpertexte aller <see cref="IFC_PROBEN"/>, aufgenommen vor dem Herausziehen.</summary>
        private const string IFC_KOERPER_SHA256 = "8545A70009FFE85EE58ADBEE9EA5FCDB8B1225B69D46B1E116228597E8EE6C4E";

        [Fact]
        public void Ifc_Koerper_bleiben_nach_dem_Herausziehen_des_Ohrenschnitts_byte_gleich()
        {
            var t = new StringBuilder();
            foreach (string datei in IFC_PROBEN)
            {
                string pfad = Path.Combine(IfcProbenTests.Ordner(), datei);
                var a = new GebaeudeImportAblauf();
                using (FileStream s = File.OpenRead(pfad))
                    a.Lesen(s, pfad, new IfcImportProfil());
                t.Append("# ").Append(datei).Append('\n');
                foreach (AbbildGebaeude g in a.Abbild.Gebaeude)
                {
                    foreach (var r in g.Raeume) t.Append("R ").Append(r.Name).Append('\n').Append(r.Koerper?.Text());
                    foreach (AbbildBauteil b in g.Bauteile)
                    {
                        t.Append("B ").Append(b.Name).Append('\n').Append(b.Koerper?.Text());
                        foreach (AbbildBauteil o in b.Oeffnungen) t.Append("O ").Append(o.Name).Append('\n').Append(o.Koerper?.Text());
                    }
                }
            }
            byte[] bytes = Encoding.UTF8.GetBytes(t.ToString());
            Assert.True(bytes.Length > 10_000, "zu wenig Körpertext: " + bytes.Length);
            string h = Convert.ToHexString(SHA256.HashData(bytes));
            Assert.True(h == IFC_KOERPER_SHA256, "SHA-256 der Körpertexte: " + h);
        }
    }
}
