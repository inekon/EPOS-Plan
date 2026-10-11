using System.Text;
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

        // ==================================================================
        //  Das Standpaar (Standprüfung, Anwenderentscheid vom 08.10.2026)
        // ==================================================================

        internal const string STAND_RAUM = "{e1000000-0000-0000-0000-00000000000a}";
        internal const string STAND_AW = "{e2000000-0000-0000-0000-000000000001}";
        internal const string STAND_DACH = "{e2000000-0000-0000-0000-000000000002}";

        /// <summary>Die GId der Hüllfläche <paramref name="n"/> des Standpaars (Wand 1, 2, Dach 3, Fenster 4).</summary>
        internal static string StandGid(int n) => "{e3000000-0000-0000-0000-00000000000" + n.ToString(System.Globalization.CultureInfo.InvariantCulture) + "}";

        /// <summary>U der Bestandsaufbauten der Projektdatei: Mauerwerk 0,30 m (λ 0,5), Dach Beton 0,20 m + Dämmung 0,04 m.</summary>
        internal static readonly double U_AW_BESTAND = 1.0 / (0.13 + 0.30 / 0.5 + 0.04);
        internal static readonly double U_DACH_BESTAND = 1.0 / (0.10 + 0.20 / 2.0 + 0.04 / 0.04 + 0.04);
        internal const double U_FENSTER_BESTAND = 1.9;

        /// <summary>Der Modellstand der IFC des Standpaars und der Zeitpunkt der Kopie im Journal der Projektdatei (acht Minuten später).</summary>
        internal const string STAND_MODELLSTAND = "23.06.2026 14:37:59";
        internal const string STAND_KOPIE = "23.06.2026 14:45:44";

        /// <summary>
        /// <b>Die Projektdatei des Standpaars</b> (Bestand): ein Raum A, zwei Außenwände (je 40 m², Aufbau „AW Bestand“, gezeichnete
        /// Dicke 0,40 m statt der Schichtsumme 0,30 m), Dach (50 m², „Dach Bestand“, Dicke passt), Fenster (10 m², U 1,9); Baujahr
        /// 1970; im Journal eine Kopie <see cref="STAND_KOPIE"/>, wenn <paramref name="mitKopie"/>.
        /// </summary>
        internal static SqprojProbenErzeuger Standpaar(bool mitKopie = true)
        {
            var e = new SqprojProbenErzeuger { Baujahr = "1970-01-01" }
                .Geschoss("F1", "EG")
                .Raum("R1", "Raum A", "F1", STAND_RAUM, 100.0)
                .Aufbau(STAND_AW, "AW Bestand", U_AW_BESTAND)
                .Schicht("LA1", STAND_AW, 0, "Mauerwerk", 0.30, 0.5, 1200.0, 1.0)
                .Aufbau(STAND_DACH, "Dach Bestand", U_DACH_BESTAND, 0.1, 0.04)
                .Schicht("LD1", STAND_DACH, 0, "Beton", 0.20, 2.0, 2400.0, 1.0)
                .Schicht("LD2", STAND_DACH, 1, "Daemmstoff", 0.04, 0.04, 30.0, 1.5, daemmung: true)
                .Huellflaeche("H1", StandGid(1), SqprojBauteilcodes.ELEMENT_WAND, STAND_AW, U_AW_BESTAND, 40.0).Bezug("B1", "R1", "H1", 1)
                .Huellflaeche("H2", StandGid(2), SqprojBauteilcodes.ELEMENT_WAND, STAND_AW, U_AW_BESTAND, 40.0).Bezug("B2", "R1", "H2", 1)
                .Huellflaeche("H3", StandGid(3), SqprojBauteilcodes.ELEMENT_DACH, STAND_DACH, U_DACH_BESTAND, 50.0).Bezug("B3", "R1", "H3", SqprojBauteilcodes.ROLLE_DACH)
                .Huellflaeche("H4", StandGid(4), SqprojBauteilcodes.ELEMENT_FENSTER, null, U_FENSTER_BESTAND, 10.0).Bezug("B4", "R1", "H4", 1)
                .Geometrie("H1", "", 0.40).Geometrie("H2", "", 0.40).Geometrie("H3", "", 0.24);
            if (mitKopie)
            {
                e.Zeile("PrJournalEntry", "J1", "10.09.2024 12:25:59 >JRN\r\nmodel=\"6.6.0\"\r\n10.09.2024 12:25:59 >DBL:LOAD {}");
                e.Zeile("PrJournalEntry", "J2", STAND_KOPIE + " >JRN\r\nmodel=\"6.6.0\"\r\n" + STAND_KOPIE + " >DBL:COPY {\"TModelProject\":{}}");
            }
            return e;
        }

        /// <summary>
        /// <b>Die IFC des Standpaars</b> (HottCAD, <c>TModelBuilding</c>): dieselbe Geometrie und dieselben GUIDs, aber sanierte
        /// Aufbauten (Wand U 0,148, Dach 0,135, Fenster 0,9) und Baujahr 1995; <paramref name="stimmig"/> = die U der Projektdatei
        /// und ihr Baujahr (keine Abweichung).
        /// </summary>
        internal static GebaeudeAbbild StandIfc(bool stimmig = false)
        {
            var a = new GebaeudeAbbild { Format = GebaeudeQuelle.FORMAT_IFC };
            var g = new AbbildGebaeude { Kennung = "0000000000000000STAND0", Name = "Standgebäude", Art = "TModelBuilding", Baujahr = stimmig ? 1970 : 1995 };
            g.Geschosse.Add(new AbbildGeschoss { Kennung = "0000000000000000000G01", Name = "EG" });
            g.Raeume.Add(new AbbildRaum { Kennung = "RA", Quelltyp = "IfcSpace", Name = "Raum A", HottcadGuid = IfcAbbildBauer.GuidNormalform(STAND_RAUM),
                                          GeschossKennung = "0000000000000000000G01", FlaecheM2 = 100.0, VolumenM3 = 300.0 });
            AbbildBauteil B(string kennung, Bauteilart art, double flaeche, double u, double? neigung, double? azimut, int n, string aufbau)
            {
                AbbildBauteil b = BauteilvorschlagProbe.Flaeche(kennung, art, Randbedingung.Aussenluft, flaeche, u, neigung, azimut, "RA");
                b.HottcadGuid = IfcAbbildBauer.GuidNormalform(StandGid(n));
                b.Name = kennung;
                if (aufbau != null) b.Aufbau = new AbbildAufbau { Kennung = aufbau, Name = aufbau };
                return b;
            }
            g.Bauteile.Add(B("Wand 1", Bauteilart.Aussenwand, 40.0, stimmig ? U_AW_BESTAND : 0.148, 90, 0, 1, "AW saniert"));
            g.Bauteile.Add(B("Wand 2", Bauteilart.Aussenwand, 40.0, stimmig ? U_AW_BESTAND : 0.148, 90, 180, 2, "AW saniert"));
            g.Bauteile.Add(B("Dach", Bauteilart.Dach, 50.0, stimmig ? U_DACH_BESTAND : 0.135, 0, null, 3, "Dach saniert"));
            g.Bauteile.Add(B("Fenster", Bauteilart.Fenster, 10.0, stimmig ? U_FENSTER_BESTAND : 0.9, 90, 0, 4, null));
            a.Gebaeude.Add(g);
            return a;
        }

        /// <summary>
        /// Der STEP-Text einer IFC mit dem Gebäude des Standpaars und seinem <c>StampEdit</c> (<see cref="STAND_MODELLSTAND"/>) — der
        /// Puffer, aus dem der Ablauf den Modellstand liest.
        /// </summary>
        internal static byte[] StandIfcText()
            => Encoding.ASCII.GetBytes(
                "ISO-10303-21;\nDATA;\n" +
                "#1=IFCBUILDING('0000000000000000STAND0',#2,'Geb\\X\\E4ude',$,'TModelBuilding',$,$,$,.ELEMENT.,$,$,$);\n" +
                "#10=IFCPROPERTYSINGLEVALUE('StampCreate','Zeitpunkt der Erstellung',IFCLABEL('10.09.2024 12:25:59'),$);\n" +
                "#11=IFCPROPERTYSINGLEVALUE('StampEdit','Zeitpunkt der letzten Bearbeitung',IFCLABEL('" + STAND_MODELLSTAND + "'),$);\n" +
                "#12=IFCPROPERTYSET('0lW5mGt2rEMeIyNB6w4Rvl',#2,'HSETU_BauteilAllgemein','Allgemeine Eigenschaften (Bauteile)',(#10,#11));\n" +
                "#13=IFCRELDEFINESBYPROPERTIES('2d1OTAL4TAsv37w6F32mSj',#2,'PropertySet to BuildingElement relation',$,(#1),#12);\n" +
                "#20=IFCPROPERTYSINGLEVALUE('StampEdit','Zeitpunkt der letzten Bearbeitung',IFCLABEL('24.12.2030 10:00:00'),$);\n" +
                "#21=IFCPROPERTYSET('1lW5mGt2rEMeIyNB6w4Rvl',#2,'HSETU_BauteilAllgemein',$,(#20));\n" +
                "#22=IFCRELDEFINESBYPROPERTIES('3d1OTAL4TAsv37w6F32mSj',#2,$,$,(#99),#21);\n" +
                "ENDSEC;\nEND-ISO-10303-21;\n");
    }
}
