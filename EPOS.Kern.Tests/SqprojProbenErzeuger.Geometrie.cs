using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.Data.Sqlite;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// Die Geometrie der Proben (Datenaustauschkonzept 17.1, synthetisch): das Raum-XML in <c>BmData.ClassValue</c> und das
    /// <c>GeoDesc</c>-XML samt <c>Thickness</c> der Hüllflächen — Aufbau wie in der Projektdatei, Werte frei gewählt.
    /// </summary>
    internal sealed partial class SqprojProbenErzeuger
    {
        private readonly List<(string Uuid, string GeoDesc, double? Dicke)> _geometrie = new List<(string, string, double?)>();

        /// <summary>Das Raum-XML eines Raums (<c>BmData</c>, <c>ReferenceUUID</c> = Raum).</summary>
        internal SqprojProbenErzeuger Raumgeometrie(string raum, string xml)
            => Zeile("BmData", "D-" + raum, raum, xml);

        /// <summary>Das <c>GeoDesc</c>-XML und die Dicke an der Fläche (<c>Thickness</c>) einer schon angelegten Hüllfläche.</summary>
        internal SqprojProbenErzeuger Geometrie(string flaeche, string geoDesc, double? dicke = null)
        {
            _geometrie.Add((flaeche, geoDesc, dicke));
            return this;
        }

        private void GeometrieNachtragen(SqliteConnection c, SqliteTransaction t)
        {
            foreach ((string uuid, string geo, double? dicke) in _geometrie)
                using (SqliteCommand k = c.CreateCommand())
                {
                    k.Transaction = t;
                    k.CommandText = "UPDATE BmElement SET GeoDesc = $g, Thickness = $d WHERE UUID = $u";
                    k.Parameters.AddWithValue("$g", geo);
                    k.Parameters.AddWithValue("$d", dicke.HasValue ? dicke.Value : PLATZHALTER);
                    k.Parameters.AddWithValue("$u", uuid);
                    k.ExecuteNonQuery();
                }
        }

        private static string T(double w) => w.ToString("R", CultureInfo.InvariantCulture);

        private static string Punkte(IEnumerable<double[]> punkte)
        {
            List<double[]> l = punkte.ToList();
            var s = new StringBuilder("<points count=\"" + l.Count + "\">");
            foreach (double[] p in l) s.Append("<p>").Append(T(p[0])).Append(';').Append(T(p[1])).Append(';').Append(T(p.Length > 2 ? p[2] : 0.0)).Append("</p>");
            return s.Append("</points>").ToString();
        }

        private static string Deckenebene(IEnumerable<double[]> grundriss, Func<double, double, double> decke)
            => "<top_planes count=\"1\"><it ref=\"x\" thickness=\"0.2\">" + Punkte(grundriss.Select(p => new[] { p[0], p[1], decke(p[0], p[1]) })) + "</it></top_planes>";

        /// <summary>
        /// Ein Raum-XML: Polygon 0 aus <paramref name="grundriss"/> (x, y) mit dem Boden <paramref name="boden"/> und der Decke
        /// <paramref name="decke"/>(x, y) — die Höhen je Kante wie in der Datei (Boden Anfang, Boden Ende, Decke Anfang,
        /// Decke Ende) —, dazu weitere Polygone mit ebener Decke.
        /// </summary>
        internal static string RaumXml(IReadOnlyList<double[]> grundriss, double boden, Func<double, double, double> decke,
                                       params (IReadOnlyList<double[]> Grundriss, double Boden, double Decke)[] weitere)
        {
            int n = grundriss.Count;
            var s = new StringBuilder("<?xml version=\"1.0\"?><geometry><Room count=\"1\"><room name=\"Probe\" height=\"2.5\">");
            s.Append(Punkte(grundriss.Select(p => new[] { p[0], p[1], boden })));
            s.Append(Deckenebene(grundriss, decke));
            s.Append("<polygons count=\"").Append(1 + weitere.Length).Append("\">");
            s.Append("<plg height=\"2.5\" type=\"MRoomPolygon\" with_holes=\"True\">").Append(Deckenebene(grundriss, decke))
             .Append("<extensions count=\"0\" />").Append(Punkte(grundriss.Select(p => new[] { p[0], p[1], boden }))).Append("</plg>");
            foreach (var w in weitere)
                s.Append("<plg height=\"").Append(T(w.Decke - w.Boden)).Append("\" type=\"MRoomPolygon\" with_holes=\"True\">")
                 .Append(Deckenebene(w.Grundriss, (_, _) => w.Decke)).Append("<extensions count=\"0\" />")
                 .Append(Punkte(w.Grundriss.Select(p => new[] { p[0], p[1], w.Boden }))).Append("</plg>");
            s.Append("</polygons><heights count=\"").Append(n).Append("\">");
            for (int i = 0; i < n; i++)
            {
                double[] a = grundriss[i], b = grundriss[(i + 1) % n];
                s.Append("<height_ext>").Append(T(boden)).Append(';').Append(T(boden)).Append(';')
                 .Append(T(decke(a[0], a[1]))).Append(';').Append(T(decke(b[0], b[1]))).Append("</height_ext>");
            }
            return s.Append("</heights></room></Room></geometry>").ToString();
        }

        /// <summary>Ein Raum-XML eines Rechtecks mit ebener Decke.</summary>
        internal static string RaumXml(double x0, double y0, double x1, double y1, double boden, double decke)
            => RaumXml(new[] { new[] { x0, y0 }, new[] { x1, y0 }, new[] { x1, y1 }, new[] { x0, y1 } }, boden, (_, _) => decke);

        /// <summary>
        /// Ein <c>GeoDesc</c>-XML: Matrix (Vorgabe Einheit; sonst 12 Werte M11…M33, Mx, My, Mz) und je Schleife ihre Regel
        /// (<c>LongDesc</c> mit laufender Nummer) und Punkte.
        /// </summary>
        internal static string GeoXml(double[] matrix, params (string Regel, double[][] Punkte)[] schleifen)
        {
            matrix ??= new double[] { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 };
            string[] namen = { "M11", "M12", "M13", "M21", "M22", "M23", "M31", "M32", "M33", "Mx", "My", "Mz" };
            var s = new StringBuilder("<hsetu><meta version=\"1.0\" exporter=\"probe\" /><dataContent><data><GEO><geoData><geoDataData ComponentUUID=\"{0}\" UUID=\"{1}\"");
            for (int i = 0; i < 12; i++) s.Append(' ').Append(namen[i]).Append("=\"").Append(T(matrix[i])).Append('"');
            s.Append(" /></geoData><geoLoop>");
            var nummer = new Dictionary<string, int>();
            foreach ((string regel, double[][] punkte) in schleifen)
            {
                nummer[regel] = nummer.TryGetValue(regel, out int k) ? k + 1 : 0;
                s.Append("<geoLoopData UUID=\"{L}\" LongDesc=\"").Append(regel).Append(' ').Append(nummer[regel]).Append("\" GeometryType=\"6\" PolyloopType=\"1\" /><geoPoint>");
                for (int i = 0; i < punkte.Length; i++)
                    s.Append("<geoPointData SortNum=\"").Append(i).Append("\" LongDesc=\"P").Append(i).Append("\" Px=\"").Append(T(punkte[i][0]))
                     .Append("\" Py=\"").Append(T(punkte[i][1])).Append("\" Pz=\"").Append(T(punkte[i][2])).Append("\" />");
                s.Append("</geoPoint>");
            }
            return s.Append("</geoLoop></GEO></data></dataContent></hsetu>").ToString();
        }

        /// <summary>Ein Rechteck senkrecht zur y-Achse in der Ebene y, von x0 bis x1 und z0 bis z1 (Umlauf gegen den Uhrzeigersinn von −y gesehen).</summary>
        internal static double[][] RechteckY(double y, double x0, double x1, double z0, double z1)
            => new[] { new[] { x0, y, z0 }, new[] { x1, y, z0 }, new[] { x1, y, z1 }, new[] { x0, y, z1 } };

        /// <summary>Ein Rechteck senkrecht zur x-Achse in der Ebene x.</summary>
        internal static double[][] RechteckX(double x, double y0, double y1, double z0, double z1)
            => new[] { new[] { x, y0, z0 }, new[] { x, y1, z0 }, new[] { x, y1, z1 }, new[] { x, y0, z1 } };

        /// <summary>Ein waagerechtes Rechteck in der Höhe z.</summary>
        internal static double[][] RechteckZ(double z, double x0, double x1, double y0, double y1)
            => new[] { new[] { x0, y0, z }, new[] { x1, y0, z }, new[] { x1, y1, z }, new[] { x0, y1, z } };

        /// <summary>
        /// <b>Das Geometriehaus</b> (Probe 40): Raum R1 (0…4 × 0…5 m, Boden 0, Decke 2,5 m) und R2 (4,24…8,24 × 0…5 m) im
        /// Innenmaß, getrennt durch die Innenwand IW (0,24 m, Schleife in der Achse x = 4,12). R1 trägt die Außenwand AW Süd
        /// (Aufbau 0,30 m, Schleife <c>DIN18599_2011</c> in der Außenoberfläche y = −0,30, im Uhrzeigersinn umlaufen), darin das
        /// Fenster FS (<c>Inner</c>, 1 × 1 m), die Bodenplatte BP (ohne Aufbau, ohne Dicke: Vorgabedicke) und das Dach DA
        /// (Dicke an der Fläche 0,20 m). R2 trägt nur seine Bodenplatte BP2; Raum R3 hat kein Raum-XML.
        /// </summary>
        internal static SqprojProbenErzeuger Geometriehaus()
        {
            const string A = "{AAAA0001-0000-0000-0000-000000000001}";
            return new SqprojProbenErzeuger()
                .Geschoss("FE", "Erdgeschoss", 0.0, 2.5)
                .Raum("R1", "Raum Eins", "FE", null, 20.0, heizung: 1, hoehe: 2.5)
                .Raum("R2", "Raum Zwei", "FE", null, 20.0, heizung: 1, hoehe: 2.5)
                .Raum("R3", "Raum Drei", "FE", null, 10.0, heizung: 1, hoehe: 2.5)
                .Raumgeometrie("R1", RaumXml(0.0, 0.0, 4.0, 5.0, 0.0, 2.5))
                .Raumgeometrie("R2", RaumXml(4.24, 0.0, 8.24, 5.0, 0.0, 2.5))
                .Aufbau(A, "Wand 30", 0.5).Schicht("L1", A, 1, "Schicht", 0.30, 0.8, 1800.0, 1.0)
                // Außenwand Süd: DIN18599_2011 in der Außenoberfläche, bis zur Achse der Innenwand; Umlauf im Uhrzeigersinn.
                .Flaeche("AW", 1, 3, 10.05, 11.05, 180.0, 90.0, aufbau: A, u: 0.5).Bezug("E1", "R1", "AW", 1)
                .Geometrie("AW", GeoXml(null,
                    ("Inner", RechteckY(0.0, 0.0, 4.0, 0.0, 2.5)),
                    ("DIN18599_2011", RechteckY(-0.30, -0.30, 4.12, 0.0, 2.5).Reverse().ToArray())))
                .Flaeche("FS", 3, 3, 1.0, 1.0, 180.0, 90.0, eltern: "AW", u: 1.3).Bezug("E2", "R1", "FS", 5)
                .Geometrie("FS", GeoXml(null, ("Inner", RechteckY(-0.30, 1.0, 2.0, 1.0, 2.0))))
                // Innenwand in der Achse, ein Bauteil mit beiden Räumen.
                .Flaeche("IW", 1, 1, 12.5, 12.5, 90.0, 90.0, u: 1.5).Bezug("E3", "R1", "IW", 2).Bezug("E4", "R2", "IW", 2, 1)
                .Geometrie("IW", GeoXml(null, ("DIN18599_2011", RechteckX(4.12, 0.0, 5.0, 0.0, 2.5))), 0.24)
                .Flaeche("BP", 11, 5, 20.0, 20.0, null, 0.0).Bezug("E5", "R1", "BP", 8)
                .Geometrie("BP", GeoXml(null, ("DIN18599_2011", RechteckZ(0.0, 0.0, 4.0, 0.0, 5.0))))
                .Flaeche("BP2", 11, 5, 20.0, 20.0, null, 0.0).Bezug("E6", "R2", "BP2", 8)
                .Geometrie("BP2", GeoXml(null, ("DIN18599_2011", RechteckZ(0.0, 4.24, 8.24, 0.0, 5.0))))
                .Flaeche("DA", 5, 3, 20.0, 20.0, null, 0.0).Bezug("E7", "R1", "DA", 9)
                .Geometrie("DA", GeoXml(null, ("DIN18599_2011", RechteckZ(2.5, 0.0, 4.0, 0.0, 5.0))), 0.20);
        }
    }
}
