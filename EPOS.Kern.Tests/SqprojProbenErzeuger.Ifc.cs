using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>Das IFC-Abbild zur Standardprobe — mit Kerntypen, deshalb nur in <c>EPOS.Kern.Tests</c>.</summary>
    internal sealed partial class SqprojProbenErzeuger
    {
        /// <summary>
        /// <b>Das passende IFC-Abbild</b> der Standardprobe (HottCAD-Gebäude, <c>TModelBuilding</c>): Räume A, B (EG), C,
        /// X (OG; die GlobalId aus <see cref="GID_D"/>) und Y (OG, ohne Gegenstück); Fläche wie in der Probe.
        /// </summary>
        internal static GebaeudeAbbild IfcAbbild(string art = "TModelBuilding")
        {
            var a = new GebaeudeAbbild { Format = GebaeudeQuelle.FORMAT_IFC };
            var g = new AbbildGebaeude { Kennung = "0000000000000000000GEB", Name = "Probegebäude", Art = art };
            g.Geschosse.Add(new AbbildGeschoss { Kennung = "0000000000000000000G01", Name = "EG" });
            g.Geschosse.Add(new AbbildGeschoss { Kennung = "0000000000000000000G02", Name = "OG" });
            void R(string kennung, string name, string geschoss, double flaeche, string raumtyp = null)
                => g.Raeume.Add(new AbbildRaum { Kennung = kennung, Name = name, GeschossKennung = geschoss, FlaecheM2 = flaeche, VolumenM3 = flaeche * 3.0, Raumtyp = raumtyp });
            R("0000000000000000000R0A", "Raum A", "0000000000000000000G01", 20.0, "mrtOffice");
            R("0000000000000000000R0B", "raum b ", "0000000000000000000G01", 30.0);
            R("0000000000000000000R0C", "Raum C", "0000000000000000000G02", 40.0, "mrtOffice");
            R(SqprojRaumabgleich.IfcKennung(GID_D), "Raum X", "0000000000000000000G02", 10.0);
            R("0000000000000000000R0Y", "Raum Y", "0000000000000000000G02", 10.0);
            a.Gebaeude.Add(g);
            return a;
        }
    }
}
