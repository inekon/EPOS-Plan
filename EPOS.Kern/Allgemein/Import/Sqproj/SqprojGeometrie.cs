using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>Die Rohgeometrie der Projektdatei, wie der <see cref="SqprojLeser"/> sie liest (Datenaustauschkonzept 17.1).</summary>
    internal sealed partial class SqprojAbbild
    {
        /// <summary>Je Level-3-Hüllfläche (<c>UUID</c>) das <c>GeoDesc</c>-XML; fehlt der Eintrag, trägt die Fläche keine Geometrie.</summary>
        internal Dictionary<string, string> Flaechengeometrie { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Je Level-3-Hüllfläche die Dicke an der Fläche (<c>Thickness</c> [m], positiv, ohne Platzhalter).</summary>
        internal Dictionary<string, double> Flaechendicke { get; } = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Je Raum (<c>BmData.ReferenceUUID</c> = <c>BmRoom.UUID</c>) das Raum-XML aus <c>BmData.ClassValue</c>.</summary>
        internal Dictionary<string, string> Raumdaten { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>Die Raumpolygone der Räume, aus denen der Grundriss je Raum unmittelbar entsteht (17.2 „Grundriss“).</summary>
    internal sealed partial class SqprojGebaeudeAbbild
    {
        /// <summary>Je Raumkennung die gelesenen Polygone (Außenring mit Höhen je Punkt, Löcher); fehlt der Eintrag, keines.</summary>
        internal Dictionary<string, IReadOnlyList<Raumpolygon>> Raumpolygone { get; } = new Dictionary<string, IReadOnlyList<Raumpolygon>>(StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Eine Schleife einer Hüllfläche aus <c>GeoDesc</c> (<c>geoLoopData</c> mit ihren <c>geoPointData</c>): die Bezugsregel
    /// (<c>LongDesc</c> ohne laufende Nummer: <c>Inner</c>, <c>DIN18599_2011</c>, <c>DIN18599</c>, <c>EN12831</c>), die
    /// Nummer und die Punkte in Weltkoordinaten [m], mit der Matrix der Fläche umgerechnet, ohne Schlusspunkt.
    /// </summary>
    internal sealed class SqprojSchleife
    {
        internal string Regel { get; init; } = "";
        internal int Nummer { get; init; }
        internal IReadOnlyList<double[]> PunkteM { get; init; } = Array.Empty<double[]>();

        /// <summary>Der Flächeninhalt des Rings [m²] (Newell, Betrag).</summary>
        internal double FlaecheM2 => Math.Sqrt(Polygonnetz.Punkt(Polygonnetz.Newell(PunkteM), Polygonnetz.Newell(PunkteM))) / 2.0;
    }

    /// <summary>
    /// <b>Die Geometrie der Projektdatei</b> (Datenaustauschkonzept 17.2 bis 17.5, Stufe K2): bildet aus dem Raum-XML und den
    /// Flächenschleifen (<c>GeoDesc</c>) der Datei die Körper derselben Art wie beim IFC-Import — mit Quelle
    /// <see cref="Koerperquelle.AusFlaechen"/>, nie gespeichert, nie exportiert.
    /// <list type="bullet">
    /// <item><b>Raumkörper</b> (17.2 Weg a): das Bodenpolygon des Raum-XML (<c>geometry/Room/room</c>), Boden und Decke je Punkt
    /// aus <c>heights/height_ext</c>; weitere Polygone (<c>polygons/plg</c>) als weitere Bestandteile, ein Polygon ganz im
    /// ersten als Loch (<c>with_holes</c>); deren Decke aus den Deckenebenen (<c>top_planes</c>). Der Mantel wird an den Grenzen
    /// der Wandflächen des Raums geteilt, Boden und Decke tragen die Fläche mit der größten Überdeckung (17.4); ohne
    /// Zuordnung tragen sie sprechende Ersatzkennungen (<c>Raum:Mantel…</c>, <c>Raum:Boden…</c>, <c>Raum:Decke…</c>).</item>
    /// <item><b>Bauteilkörper</b> (17.3): die Schleife nach <see cref="RANG_OPAK"/> bzw. <see cref="RANG_OEFFNUNG"/>, Dicke aus dem
    /// Aufbau, sonst an der Fläche, sonst <see cref="Vorgabedicke"/>; die Richtung aus der gemessenen Lage der Schleife gegen
    /// den Raumkörper (<see cref="Richtung"/>); Öffnungen als Loch mit Laibung und als eigener Körper mittig in der Wand.</item>
    /// <item><b>Randpunkte</b>: <see cref="AbbildBauteil.RandpunkteM"/> ist der gewählte Außenring, so umlaufen, dass seine
    /// Normale vom Raum weg zeigt — über die Raumseite, nicht über den Umlauf der Datei (17.1).</item>
    /// <item><b>Benannte Fehlschläge</b>: Was nicht schließt, trägt keinen Körper, sondern eine Meldung mit Grund
    /// (<see cref="Koerperergebnis.Grund"/>); die Ansicht zeigt dann das Umrissprisma.</item>
    /// </list>
    /// Deterministisch (feste Reihenfolge nach Kennung), ohne Datenbank und ohne Plattformzugriff.
    /// </summary>
    internal static class SqprojGeometrie
    {
        // ---------------- Meldungen ----------------

        /// <summary>I — {0}/{1} Raumkörper, {2}/{3} Bauteilkörper, {4} Vorgabedicke, {5} Bezugsebene angenommen.</summary>
        internal const string GEOMETRIE = SqprojGebaeudeLeser.PRAEFIX + "GEOMETRIE";
        /// <summary>I — Die Datei führt keine lesbare Geometrie; die Ansicht zeigt Umrissprismen.</summary>
        internal const string GEOMETRIE_FEHLT = SqprojGebaeudeLeser.PRAEFIX + "GEOMETRIE_FEHLT";
        /// <summary>W — {0} Raum, {1} Grund: kein lesbares Raumpolygon, Rückfall Umrissprisma.</summary>
        internal const string RAUM_OHNE_POLYGON = SqprojGebaeudeLeser.PRAEFIX + "RAUM_OHNE_POLYGON";
        /// <summary>W — {0} Raum, {1} Grund, {2} Werte: Raumkörper nicht gebildet, Rückfall Umrissprisma.</summary>
        internal const string KOERPER_RAUM = SqprojGebaeudeLeser.PRAEFIX + "KOERPER_RAUM";
        /// <summary>W (am Bauteil) — {0} Grund, {1} Werte: Bauteilkörper nicht gebildet.</summary>
        internal const string KOERPER_BAUTEIL = SqprojGebaeudeLeser.PRAEFIX + "KOERPER_BAUTEIL";
        /// <summary>I (am Bauteil) — keine lesbare Schleife.</summary>
        internal const string OHNE_GEOMETRIE = SqprojGebaeudeLeser.PRAEFIX + "OHNE_GEOMETRIE";
        /// <summary>W (an der Öffnung) — keine Wandfläche desselben Raums trägt die Öffnung (17.3 Nr. 4).</summary>
        internal const string OEFFNUNG_OHNE_WAND = SqprojGebaeudeLeser.PRAEFIX + "OEFFNUNG_OHNE_WAND";
        /// <summary>I (an der Öffnung) — die Öffnung reicht an den Rand ihrer Wand; die Aussparung entfällt.</summary>
        internal const string OEFFNUNG_RAND = SqprojGebaeudeLeser.PRAEFIX + "OEFFNUNG_RAND";

        // ---------------- Schleifen ----------------

        internal const string REGEL_INNER = "Inner";
        internal const string REGEL_DIN18599_2011 = "DIN18599_2011";
        internal const string REGEL_DIN18599 = "DIN18599";
        internal const string REGEL_EN12831 = "EN12831";

        /// <summary>Die Rangfolge der Schleifen einer opaken Fläche: die Bruttofläche steht in <c>DIN18599_2011</c> (17.1).</summary>
        internal static readonly string[] RANG_OPAK = { REGEL_DIN18599_2011, REGEL_DIN18599, REGEL_EN12831, REGEL_INNER };

        /// <summary>Die Rangfolge der Schleifen einer Öffnung: die Bruttofläche steht in <c>Inner</c> (17.1).</summary>
        internal static readonly string[] RANG_OEFFNUNG = { REGEL_INNER, REGEL_DIN18599_2011, REGEL_DIN18599, REGEL_EN12831 };

        // ---------------- Festwerte ----------------

        /// <summary>Lage „in der Innenoberfläche“: höchstens 1 cm vom Raumkörper (17.3 Nr. 3).</summary>
        internal const double INNENFLAECHE_M = 0.01;

        /// <summary>Zuschlag auf die Dicke beim Suchen der Hüllfläche einer Raumfläche (17.4: Dicke plus 2 cm).</summary>
        internal const double ZUSCHLAG_ZUORDNUNG_M = 0.02;

        /// <summary>Wie weit hinter der Dicke eine Raumfläche noch als Gegenüber einer Bauteilfläche gilt [m].</summary>
        internal const double SUCHWEITE_M = 0.30;

        /// <summary>Parallel heißt: höchstens 1° zwischen den Normalen (17.4).</summary>
        internal static readonly double COS_PARALLEL = Math.Cos(Math.PI / 180.0);

        /// <summary>Senkrecht (Wand) heißt: die Normale höchstens 1° neben der Waagerechten.</summary>
        internal static readonly double SIN_WAAGERECHT = Math.Sin(Math.PI / 180.0);

        /// <summary>Abstand eines Lochs zum Rand seiner Wand, unter dem die Aussparung entfällt [m].</summary>
        internal const double LOCHRAND_M = 0.002;

        /// <summary>
        /// Die benannte Vorgabedicke je Bauteilart [m] (17.3 Nr. 2) — gilt, wenn weder ein Aufbau mit Schichten noch eine
        /// Dicke an der Fläche vorliegt; der Körper trägt dann den Vermerk <see cref="Koerpervermerk.Vorgabedicke"/>.
        /// </summary>
        internal static double Vorgabedicke(Bauteilart art) => art switch
        {
            Bauteilart.Aussenwand => 0.30,
            Bauteilart.Innenwand => 0.15,
            Bauteilart.Decke => 0.20,
            Bauteilart.Dach => 0.25,
            Bauteilart.Bodenplatte => 0.25,
            Bauteilart.Fenster => 0.08,
            Bauteilart.Tuer => 0.06,
            _ => 0.20,
        };

        // ==================================================================
        //  Lesen
        // ==================================================================

        /// <summary>
        /// Die Schleifen eines <c>GeoDesc</c>-XML: Matrix (<c>geoDataData</c> M11…M33, Mx, My, Mz) als Zeilenvektor-Abbildung
        /// p′ = p·M + t angewandt (in den bekannten Dateien die Einheit), je <c>geoLoopData</c> die folgenden
        /// <c>geoPointData</c> (Px, Py, Pz) in der Reihenfolge von <c>SortNum</c>. Unlesbar → leer.
        /// </summary>
        internal static IReadOnlyList<SqprojSchleife> SchleifenLesen(string geoDesc)
        {
            var liste = new List<SqprojSchleife>();
            if (string.IsNullOrWhiteSpace(geoDesc)) return liste;
            XDocument x;
            try
            {
                x = XDocument.Parse(geoDesc);
            }
            catch (XmlException)
            {
                return liste;
            }
            double[] m = { 1, 0, 0, 0, 1, 0, 0, 0, 1, 0, 0, 0 };
            XElement d = x.Descendants("geoDataData").FirstOrDefault();
            if (d != null)
            {
                string[] namen = { "M11", "M12", "M13", "M21", "M22", "M23", "M31", "M32", "M33", "Mx", "My", "Mz" };
                for (int i = 0; i < namen.Length; i++)
                    if (Zahl((string)d.Attribute(namen[i])) is double w) m[i] = w;
            }
            foreach (XElement schleife in x.Descendants("geoLoop"))
            {
                string regel = null;
                int nummer = 0;
                foreach (XElement e in schleife.Elements())
                {
                    if (e.Name.LocalName == "geoLoopData")
                    {
                        (regel, nummer) = Regel((string)e.Attribute("LongDesc"));
                        continue;
                    }
                    if (e.Name.LocalName != "geoPoint" || regel == null) continue;
                    var punkte = new List<(int Sort, int Stelle, double[] P)>();
                    int stelle = 0;
                    foreach (XElement p in e.Elements("geoPointData"))
                    {
                        if (Zahl((string)p.Attribute("Px")) is not double px || Zahl((string)p.Attribute("Py")) is not double py
                            || Zahl((string)p.Attribute("Pz")) is not double pz) continue;
                        int sort = int.TryParse((string)p.Attribute("SortNum"), NumberStyles.Integer, CultureInfo.InvariantCulture, out int s) ? s : stelle;
                        double[] q =
                        {
                            px * m[0] + py * m[3] + pz * m[6] + m[9],
                            px * m[1] + py * m[4] + pz * m[7] + m[10],
                            px * m[2] + py * m[5] + pz * m[8] + m[11],
                        };
                        punkte.Add((sort, stelle++, q));
                    }
                    List<double[]> ring = Polygonnetz.Bereinigt(punkte.OrderBy(p => p.Sort).ThenBy(p => p.Stelle).Select(p => p.P));
                    if (ring.Count >= 3) liste.Add(new SqprojSchleife { Regel = regel, Nummer = nummer, PunkteM = ring });
                    regel = null;
                }
            }
            return liste;
        }

        /// <summary>Die Bezugsregel aus <c>LongDesc</c> („DIN18599_2011 0“ → Regel „DIN18599_2011“, Nummer 0).</summary>
        private static (string, int) Regel(string text)
        {
            string t = (text ?? "").Trim();
            int i = t.LastIndexOf(' ');
            if (i > 0 && int.TryParse(t.Substring(i + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int n))
                return (t.Substring(0, i).Trim(), n);
            return (t, 0);
        }

        /// <summary>
        /// Die Schleifen einer Fläche nach der Rangfolge: die erste Regel, die eine Schleife trägt; darin die größte als
        /// Außenring, eine Schleife ganz in ihm als Loch, jede andere als weiterer Bestandteil. <c>null</c> = keine Schleife.
        /// </summary>
        internal static (List<double[]> Aussen, List<List<double[]>> Loecher, List<List<double[]>> Weitere, string Regel)? Waehlen(
            IReadOnlyList<SqprojSchleife> schleifen, IReadOnlyList<string> rang)
        {
            foreach (string regel in rang)
            {
                List<SqprojSchleife> l = schleifen.Where(s => string.Equals(s.Regel, regel, StringComparison.OrdinalIgnoreCase))
                                                  .OrderByDescending(s => s.FlaecheM2).ThenBy(s => s.Nummer).ToList();
                if (l.Count == 0) continue;
                var aussen = l[0].PunkteM.ToList();
                var loecher = new List<List<double[]>>();
                var weitere = new List<List<double[]>>();
                (double[] o, double[] e1, double[] e2) = Ebene(aussen);
                foreach (SqprojSchleife s in l.Skip(1))
                {
                    bool innen = e1 != null && s.PunkteM.All(p => Math.Abs(Polygonnetz.Punkt(Polygonnetz.Minus(p, o), Polygonnetz.Kreuz(e1, e2))) <= 0.01
                                                                   && Innen2D(Flach(aussen, o, e1, e2), Flach(new[] { p }, o, e1, e2)[0]));
                    (innen ? loecher : weitere).Add(s.PunkteM.ToList());
                }
                return (aussen, loecher, weitere, regel);
            }
            return null;
        }

        /// <summary>
        /// Die Polygone eines Raum-XML (<c>geometry/Room/room</c>): Polygon 0 aus <c>points</c> mit den Höhen je Kante
        /// (<c>heights/height_ext</c>: Boden am Anfang, Boden am Ende, Decke am Anfang, Decke am Ende); passen die Höhen nicht,
        /// Boden aus den Punkten und Decke aus den Deckenebenen. Weitere Polygone aus <c>polygons/plg</c> (Boden aus den
        /// Punkten, Decke aus ihren Deckenebenen), ganz im ersten liegend als dessen Loch. <c>null</c> = keines; dann
        /// nennt <paramref name="grund"/> den Grund (<c>XML</c>, <c>PUNKTE</c>, <c>HOEHEN</c>).
        /// </summary>
        internal static List<Raumpolygon> RaumpolygoneLesen(string xml, string raumKennung, out string grund)
        {
            grund = "XML";
            if (string.IsNullOrWhiteSpace(xml)) return null;
            XElement raum;
            try
            {
                raum = XDocument.Parse(xml).Descendants("room").FirstOrDefault();
            }
            catch (XmlException)
            {
                return null;
            }
            if (raum == null) return null;
            grund = "PUNKTE";
            List<double[]> punkte = Punkte(raum.Element("points"));
            if (punkte.Count < 3) return null;
            double? hoehe = Zahl((string)raum.Attribute("height"));
            List<List<double[]>> ebenenRaum = Ebenen(raum.Element("top_planes"));
            List<XElement> plgs = raum.Element("polygons")?.Elements("plg").ToList() ?? new List<XElement>();

            // Polygon 0: die Punkte des Raums mit den Höhen je Kante.
            List<double[]> kanten = (raum.Element("heights")?.Elements("height_ext") ?? Enumerable.Empty<XElement>())
                .Select(h => (h.Value ?? "").Split(';').Select(t => Zahl(t) ?? double.NaN).ToArray())
                .Where(h => h.Length >= 4 && h.Take(4).All(v => !double.IsNaN(v))).ToList();
            XElement erstes = plgs.FirstOrDefault(p => GleichePunkte(Punkte(p.Element("points")), punkte));
            List<List<double[]>> ebenen0 = erstes != null ? Ebenen(erstes.Element("top_planes")) : new List<List<double[]>>();
            if (ebenen0.Count == 0) ebenen0 = ebenenRaum;
            Raumring ring0 = kanten.Count == punkte.Count
                ? Raumring.AusKantenhoehen(punkte.Select(p => new[] { p[0], p[1] }).ToList(), kanten, Koerperbildner.TOLERANZ_M, out _)
                : null;
            ring0 ??= RingAusEbenen(punkte, ebenen0, hoehe);
            grund = "HOEHEN";
            if (ring0 == null) return null;

            bool mitLoechern = !string.Equals((string)(erstes ?? plgs.FirstOrDefault())?.Attribute("with_holes"), "False", StringComparison.OrdinalIgnoreCase);
            var loecher = new List<Raumring>();
            var weitere = new List<Raumring>();
            foreach (XElement p in plgs)
            {
                if (ReferenceEquals(p, erstes)) continue;
                List<double[]> q = Punkte(p.Element("points"));
                if (q.Count < 3) continue;
                List<List<double[]>> eb = Ebenen(p.Element("top_planes"));
                Raumring r = RingAusEbenen(q, eb.Count > 0 ? eb : ebenen0, Zahl((string)p.Attribute("height")) ?? hoehe);
                if (r == null) continue;
                bool loch = mitLoechern && q.All(x => Innen2D(punkte, x) && !AufRand2D(punkte, x));
                (loch ? loecher : weitere).Add(r);
            }
            var liste = new List<Raumpolygon> { new Raumpolygon { Kennung = raumKennung + ":Polygon0", Aussen = ring0, Loecher = loecher } };
            for (int i = 0; i < weitere.Count; i++)
                liste.Add(new Raumpolygon { Kennung = raumKennung + ":Polygon" + Z(i + 1), Aussen = weitere[i] });
            grund = "";
            return liste;
        }

        /// <summary>Ein Ring mit dem Boden aus den Punkten und der Decke aus den Deckenebenen (sonst Boden + Höhe).</summary>
        private static Raumring RingAusEbenen(List<double[]> punkte, List<List<double[]>> ebenen, double? hoehe)
        {
            var boden = new double[punkte.Count];
            var decke = new double[punkte.Count];
            for (int i = 0; i < punkte.Count; i++)
            {
                boden[i] = punkte[i][2];
                double? d = Deckenhoehe(ebenen, punkte[i][0], punkte[i][1]);
                if (d == null && hoehe > 0.0) d = boden[i] + hoehe.Value;
                if (d == null || d.Value <= boden[i]) return null;
                decke[i] = d.Value;
            }
            return new Raumring { Punkte = punkte.Select(p => new[] { p[0], p[1] }).ToList(), Boden = boden, Decke = decke };
        }

        /// <summary>Die Höhe der Deckenebenen über (x, y): die höchste Ebene, deren Grundriss den Punkt fasst, sonst die höchste.</summary>
        private static double? Deckenhoehe(List<List<double[]>> ebenen, double x, double y)
        {
            double? fassend = null, alle = null;
            foreach (List<double[]> e in ebenen)
            {
                double[] n = Polygonnetz.Normiert(Polygonnetz.Newell(e));
                if (n == null || Math.Abs(n[2]) < 1e-6) continue;
                double z = e[0][2] - (n[0] * (x - e[0][0]) + n[1] * (y - e[0][1])) / n[2];
                alle = alle == null ? z : Math.Max(alle.Value, z);
                if (Innen2D(e, new[] { x, y }) || AufRand2D(e, new[] { x, y })) fassend = fassend == null ? z : Math.Max(fassend.Value, z);
            }
            return fassend ?? alle;
        }

        private static List<List<double[]>> Ebenen(XElement topPlanes)
            => topPlanes?.Elements("it").Select(it => Punkte(it.Element("points"))).Where(p => p.Count >= 3).ToList() ?? new List<List<double[]>>();

        private static List<double[]> Punkte(XElement punkte)
        {
            var l = new List<double[]>();
            if (punkte == null) return l;
            foreach (XElement p in punkte.Elements("p"))
            {
                double?[] w = (p.Value ?? "").Split(';').Select(Zahl).ToArray();
                if (w.Length >= 2 && w[0] is double x && w[1] is double y) l.Add(new[] { x, y, w.Length >= 3 && w[2] is double z ? z : 0.0 });
            }
            return Polygonnetz.Bereinigt(l);
        }

        private static bool GleichePunkte(List<double[]> a, List<double[]> b)
            => a.Count == b.Count && a.All(p => b.Any(q => Math.Abs(p[0] - q[0]) <= 1e-6 && Math.Abs(p[1] - q[1]) <= 1e-6));

        private static double? Zahl(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            return double.TryParse(text.Trim().Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double w)
                   && !double.IsNaN(w) && !double.IsInfinity(w) ? w : null;
        }

        // ==================================================================
        //  Bilden
        // ==================================================================

        /// <summary>Eine Bauteilfläche mit ihrem gewählten Ring, Dicke, Räumen und — nach der Messung — Richtung.</summary>
        private sealed class Flaeche
        {
            internal AbbildBauteil Bauteil;
            internal List<double[]> Aussen;
            internal List<List<double[]>> Loecher = new List<List<double[]>>();
            internal List<string> Lochkennungen = new List<string>();
            internal List<List<double[]>> Weitere = new List<List<double[]>>();
            internal double[] Normale;
            internal double Dicke;
            internal bool Vorgabe;
            /// <summary>Die Dicke an der Fläche (<c>Thickness</c>), wenn sie nicht schon <see cref="Dicke"/> ist; <c>null</c> = keine.</summary>
            internal double? Flaechendicke;
            internal List<string> Raeume = new List<string>();
            internal bool Wand => Math.Abs(Normale[2]) <= SIN_WAAGERECHT;
            internal bool Oeffnung => Bauteil.Art == Bauteilart.Fenster || Bauteil.Art == Bauteilart.Tuer;
            internal double ZMin => Aussen.Min(p => p[2]);
            internal double ZMax => Aussen.Max(p => p[2]);

            // nach der Messung
            internal Extrusionsrichtung Richtung = Extrusionsrichtung.Beidseitig;
            internal double[] ZumRaum;
            internal bool Angenommen;
        }

        /// <summary>
        /// Bildet Raumkörper, Grundrisspolygone, Randpunkte und Bauteilkörper des Abbilds aus der Rohgeometrie
        /// (<see cref="SqprojAbbild.Raumdaten"/>, <see cref="SqprojAbbild.Flaechengeometrie"/>) und meldet das Ergebnis.
        /// Trägt die Datei keine Geometrie, bleibt alles leer, mit genau einer Meldung <see cref="GEOMETRIE_FEHLT"/>.
        /// </summary>
        internal static void Bilden(SqprojGebaeudeAbbild abbild, SqprojAbbild p)
        {
            if (abbild == null || p == null) return;
            if (p.Raumdaten.Count == 0 && p.Flaechengeometrie.Count == 0)
            {
                abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, GEOMETRIE_FEHLT));
                return;
            }
            var bauteile = abbild.Gebaeude.SelectMany(g => g.Bauteile).Concat(abbild.BauteileOhneGebaeude)
                                 .GroupBy(b => b.Kennung, StringComparer.OrdinalIgnoreCase).Select(g => g.First())
                                 .OrderBy(b => b.Kennung, StringComparer.Ordinal).ToList();
            var raeume = abbild.Gebaeude.SelectMany(g => g.Raeume).GroupBy(r => r.Kennung, StringComparer.OrdinalIgnoreCase)
                               .Select(g => g.First()).ToList();

            // 1) Flächen der Bauteile (ohne Öffnungen in Wänden): Ring, Normale, Dicke.
            var flaechen = new List<Flaeche>();
            foreach (AbbildBauteil b in bauteile)
            {
                Flaeche f = FlaecheVon(b, p, b.Art == Bauteilart.Fenster || b.Art == Bauteilart.Tuer);
                if (f != null) flaechen.Add(f);
            }

            // 2) Raumkörper aus dem Raumpolygon, der Mantel an den Wandflächen geteilt.
            var koerper = new Dictionary<string, Dateikoerper>(StringComparer.OrdinalIgnoreCase);
            int raumGebildet = 0;
            foreach (AbbildRaum r in raeume)
            {
                string name = string.IsNullOrWhiteSpace(r.Name) ? r.Kennung : r.Name;
                if (!p.Raumdaten.TryGetValue(r.Kennung, out string xml))
                {
                    abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, RAUM_OHNE_POLYGON, name, "XML"));
                    continue;
                }
                List<Raumpolygon> polygone = RaumpolygoneLesen(xml, r.Kennung, out string grund);
                if (polygone == null)
                {
                    abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, RAUM_OHNE_POLYGON, name, grund));
                    continue;
                }
                abbild.Raumpolygone[r.Kennung] = polygone;
                List<Flaeche> eigene = flaechen.Where(f => !f.Oeffnung && f.Raeume.Contains(r.Kennung, StringComparer.OrdinalIgnoreCase)).ToList();
                List<Raumpolygon> zugeordnet = polygone.Select((pg, i) => Zuordnen(pg, r.Kennung, i, eigene)).ToList();
                Koerperergebnis e = Raumkoerper(zugeordnet);
                if (e.Gebildet)
                {
                    r.Koerper = e.Koerper;
                    koerper[r.Kennung] = e.Koerper;
                    raumGebildet++;
                }
                else
                    abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, KOERPER_RAUM, name, e.Grund, string.Join(",", e.Werte)));
            }

            // 3) Lage der Bezugsebene je Fläche, Randpunkte über die Raumseite.
            foreach (Flaeche f in flaechen)
            {
                Messen(f, koerper, abbild.Raumpolygone);
                f.Bauteil.RandpunkteM = Gerichtet(f.Aussen, f.ZumRaum);
            }

            // 4) Öffnungen: Wand suchen, als Loch eintragen; dann Wände extrudieren, dann die Öffnungskörper.
            var wandVon = new Dictionary<AbbildBauteil, Flaeche>(ReferenceEqualityComparer.Instance);
            var oeffnungen = new List<(AbbildBauteil Oeffnung, Flaeche Eigen, Flaeche Wand)>();
            foreach (Flaeche f in flaechen.Where(x => !x.Oeffnung))
                foreach (AbbildBauteil o in f.Bauteil.Oeffnungen.OrderBy(o => o.Kennung, StringComparer.Ordinal))
                {
                    Flaeche of = FlaecheVon(o, p, true);
                    if (of == null) continue;
                    of.Raeume = f.Raeume;
                    oeffnungen.Add((o, of, Wand(of, f, flaechen)));
                }
            foreach (Flaeche f in flaechen.Where(x => x.Oeffnung))
                oeffnungen.Add((f.Bauteil, f, Wand(f, null, flaechen)));
            foreach ((AbbildBauteil o, Flaeche of, Flaeche wand) in oeffnungen)
            {
                if (wand == null)
                {
                    o.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, OEFFNUNG_OHNE_WAND));
                    continue;
                }
                List<double[]> loch = Projiziert(of.Aussen, wand);
                if (!StrengInnen(loch, wand))
                {
                    o.Meldungen.Add(new PruefMeldung(PruefStufe.Info, OEFFNUNG_RAND));
                    continue;
                }
                wand.Loecher.Add(loch);
                wand.Lochkennungen.Add(o.Kennung);
            }

            int bauteilGebildet = 0, bauteilZahl = 0, vorgabe = 0, angenommen = 0;
            foreach (Flaeche f in flaechen.Where(x => !x.Oeffnung))
            {
                bauteilZahl++;
                if (Extrudieren(f)) bauteilGebildet++;
                if (f.Vorgabe) vorgabe++;
                if (f.Angenommen) angenommen++;
            }
            foreach ((AbbildBauteil o, Flaeche of, Flaeche wand) in oeffnungen)
            {
                bauteilZahl++;
                if (wand != null)
                {
                    // Mittig in der Wanddicke, in der Ebene der Wand, mit der Raumseite der Wand.
                    double mitte = wand.Richtung switch
                    {
                        Extrusionsrichtung.NachInnen => wand.Dicke / 2.0,
                        Extrusionsrichtung.NachAussen => -wand.Dicke / 2.0,
                        _ => 0.0,
                    };
                    double[] versatz = Polygonnetz.Mal(wand.ZumRaum, mitte);
                    of.Aussen = Projiziert(of.Aussen, wand).Select(q => Polygonnetz.Plus(q, versatz)).ToList();
                    of.ZumRaum = wand.ZumRaum;
                    of.Dicke = Math.Min(of.Dicke, wand.Dicke);
                    of.Richtung = Extrusionsrichtung.Beidseitig;
                    o.RandpunkteM = Gerichtet(Projiziert(of.Aussen, wand), wand.ZumRaum);
                }
                else
                {
                    Messen(of, koerper, abbild.Raumpolygone);
                    o.RandpunkteM = Gerichtet(of.Aussen, of.ZumRaum);
                }
                if (Extrudieren(of)) bauteilGebildet++;
                if (of.Vorgabe) vorgabe++;
                if (of.Angenommen) angenommen++;
            }
            foreach (AbbildBauteil b in bauteile.Concat(bauteile.SelectMany(x => x.Oeffnungen)))
                if (b.RandpunkteM == null && !b.Meldungen.Any(m => m.Schluessel == OHNE_GEOMETRIE))
                    b.Meldungen.Add(new PruefMeldung(PruefStufe.Info, OHNE_GEOMETRIE));

            abbild.Meldungen.Add(new PruefMeldung(PruefStufe.Info, GEOMETRIE, Z(raumGebildet), Z(raeume.Count), Z(bauteilGebildet), Z(bauteilZahl),
                                                  Z(vorgabe), Z(angenommen)));
        }

        /// <summary>Die Fläche eines Bauteils aus seiner Schleife; <c>null</c> = keine lesbare Schleife.</summary>
        private static Flaeche FlaecheVon(AbbildBauteil b, SqprojAbbild p, bool oeffnung)
        {
            if (!p.Flaechengeometrie.TryGetValue(b.Kennung, out string geo)) return null;
            var wahl = Waehlen(SchleifenLesen(geo).Select(s => new SqprojSchleife { Regel = s.Regel, Nummer = s.Nummer, PunkteM = Gestrafft(s.PunkteM) })
                                                  .Where(s => s.PunkteM.Count >= 3).ToList(), oeffnung ? RANG_OEFFNUNG : RANG_OPAK);
            if (wahl == null) return null;
            double[] n = Polygonnetz.Normiert(Polygonnetz.Newell(wahl.Value.Aussen));
            if (n == null) return null;
            double dicke;
            bool vorgabe = false;
            if (b.DickeM > 0.0) dicke = b.DickeM.Value;
            else if (AufbauDicke(b.Kennung, p) is double s) dicke = s;
            else if (p.Flaechendicke.TryGetValue(b.Kennung, out double d) && d > 0.0) dicke = d;
            else
            {
                dicke = Vorgabedicke(b.Art);
                vorgabe = true;
            }
            return new Flaeche
            {
                Bauteil = b, Aussen = wahl.Value.Aussen, Loecher = wahl.Value.Loecher, Weitere = wahl.Value.Weitere,
                Lochkennungen = wahl.Value.Loecher.Select(_ => b.Kennung).ToList(),
                Normale = n, Dicke = dicke, Vorgabe = vorgabe,
                Flaechendicke = p.Flaechendicke.TryGetValue(b.Kennung, out double fd) && fd > 0.0 && Math.Abs(fd - dicke) > 1e-9 ? fd : null,
                Raeume = b.Nachbarn.Select(x => x.Kennung).Where(k => k != null).ToList(),
            };
        }

        /// <summary>Die Summe der Schichtdicken des Aufbaus der Fläche (über <c>CatalogDimUUID</c>); <c>null</c> = kein Aufbau mit Schichtdicken.</summary>
        private static double? AufbauDicke(string kennung, SqprojAbbild p)
        {
            SqprojHuellflaeche h = p.Huellflaechen.FirstOrDefault(x => string.Equals(x.Uuid, kennung, StringComparison.OrdinalIgnoreCase));
            if (h?.AufbauKennung == null || !p.Aufbauten.TryGetValue(h.AufbauKennung, out SqprojAufbau a)) return null;
            double summe = a.Schichten.Where(s => s.DickeM > 0.0).Sum(s => s.DickeM.Value);
            return summe > 0.0 ? summe : a.DickeM > 0.0 ? a.DickeM : null;
        }

        // ---------------- Raumkörper: Zuordnung Dreieck → Hüllfläche (17.4) ----------------

        /// <summary>
        /// Das Polygon mit den Kennungen seiner Hüllflächen: jede Kante an den Grenzen der parallelen Wandflächen des Raums
        /// geteilt (Abstand höchstens Dicke + 2 cm), jedes Teilstück mit der Fläche der größten Überdeckung in der Höhe;
        /// Boden und Decke mit der Fläche der größten Überdeckung im Grundriss.
        /// </summary>
        private static Raumpolygon Zuordnen(Raumpolygon pg, string raum, int nummer, List<Flaeche> eigene)
        {
            List<Flaeche> waende = eigene.Where(f => f.Wand).ToList();
            string praefix = raum + ":";
            Raumring Teilen(Raumring ring, int r)
            {
                var punkte = new List<double[]>();
                var boden = new List<double>();
                var decke = new List<double>();
                var kennungen = new List<string>();
                int n = ring.Punkte.Count;
                for (int k = 0; k < n; k++)
                {
                    int j = (k + 1) % n;
                    double[] a = ring.Punkte[k], b = ring.Punkte[j];
                    double lx = b[0] - a[0], ly = b[1] - a[1], laenge = Math.Sqrt(lx * lx + ly * ly);
                    string ersatz = praefix + "Mantel" + Z(nummer) + "." + Z(r) + "." + Z(k);
                    punkte.Add(a);
                    boden.Add(ring.Boden[k]);
                    decke.Add(ring.Decke[k]);
                    if (laenge < 1e-6)
                    {
                        kennungen.Add(ersatz);
                        continue;
                    }
                    double[] u = { lx / laenge, ly / laenge, 0.0 }, w = { u[1], -u[0], 0.0 };
                    double unten = Math.Min(ring.Boden[k], ring.Boden[j]), oben = Math.Max(ring.Decke[k], ring.Decke[j]);
                    var kandidaten = new List<(Flaeche F, double Von, double Bis, double Abstand, double Hoehe)>();
                    foreach (Flaeche f in waende)
                    {
                        if (Math.Abs(f.Normale[0] * w[0] + f.Normale[1] * w[1]) < COS_PARALLEL) continue;
                        double abstand = Math.Abs((f.Aussen[0][0] - a[0]) * f.Normale[0] + (f.Aussen[0][1] - a[1]) * f.Normale[1]);
                        if (abstand > f.Dicke + ZUSCHLAG_ZUORDNUNG_M) continue;
                        double hoehe = Math.Min(oben, f.ZMax) - Math.Max(unten, f.ZMin);
                        if (hoehe <= Koerperbildner.TOLERANZ_M) continue;
                        double von = f.Aussen.Min(q => (q[0] - a[0]) * u[0] + (q[1] - a[1]) * u[1]);
                        double bis = f.Aussen.Max(q => (q[0] - a[0]) * u[0] + (q[1] - a[1]) * u[1]);
                        von = Math.Max(0.0, von);
                        bis = Math.Min(laenge, bis);
                        if (bis - von > Koerperbildner.TOLERANZ_M) kandidaten.Add((f, von, bis, abstand, hoehe));
                    }
                    var stellen = new List<double> { 0.0, laenge };
                    foreach (var c in kandidaten) { stellen.Add(c.Von); stellen.Add(c.Bis); }
                    stellen = stellen.OrderBy(t => t).ToList();
                    var schnitte = new List<double>();
                    foreach (double t in stellen)
                        if (schnitte.Count == 0 || t - schnitte[^1] > Koerperbildner.TOLERANZ_M) schnitte.Add(t);
                    if (laenge - schnitte[^1] > Koerperbildner.TOLERANZ_M) schnitte.Add(laenge);
                    else schnitte[^1] = laenge;
                    int teile = schnitte.Count - 1;
                    for (int s = 0; s < teile; s++)
                    {
                        double t0 = schnitte[s], t1 = schnitte[s + 1];
                        if (s > 0)
                        {
                            double v = t0 / laenge;
                            punkte.Add(new[] { a[0] + u[0] * t0, a[1] + u[1] * t0 });
                            boden.Add(ring.Boden[k] + (ring.Boden[j] - ring.Boden[k]) * v);
                            decke.Add(ring.Decke[k] + (ring.Decke[j] - ring.Decke[k]) * v);
                        }
                        var traeger = kandidaten.Where(c => c.Von <= t0 + Koerperbildner.TOLERANZ_M && c.Bis >= t1 - Koerperbildner.TOLERANZ_M)
                                                .OrderByDescending(c => Math.Round(c.Hoehe, 3)).ThenBy(c => c.Abstand)
                                                .ThenBy(c => c.F.Bauteil.Kennung, StringComparer.Ordinal).FirstOrDefault();
                        kennungen.Add(traeger.F?.Bauteil.Kennung ?? (teile > 1 ? ersatz + "." + Z(s) : ersatz));
                    }
                }
                return new Raumring { Punkte = punkte, Boden = boden, Decke = decke, Kantenkennungen = kennungen };
            }

            Raumring aussen = Teilen(pg.Aussen, 0);
            var loecher = pg.Loecher.Select((l, i) => Teilen(l, i + 1)).ToList();
            string bodenK = Deckel(pg, eigene, true) ?? praefix + "Boden" + Z(nummer);
            string deckeK = Deckel(pg, eigene, false) ?? praefix + "Decke" + Z(nummer);
            return new Raumpolygon { Kennung = pg.Kennung, Aussen = aussen, Loecher = loecher, BodenKennung = bodenK, DeckenKennung = deckeK };
        }

        /// <summary>
        /// Die Hüllfläche von Boden bzw. Decke eines Polygons: nicht senkrecht, höchstens Dicke + 2 cm von der Boden- bzw.
        /// Deckenhöhe, mit den meisten Stichpunkten des Polygons (Raster 10 × 10) in ihrem Grundriss; <c>null</c> = keine.
        /// </summary>
        private static string Deckel(Raumpolygon pg, List<Flaeche> eigene, bool boden)
        {
            List<double[]> ring = pg.Aussen.Punkte.ToList();
            double xmin = ring.Min(q => q[0]), xmax = ring.Max(q => q[0]), ymin = ring.Min(q => q[1]), ymax = ring.Max(q => q[1]);
            var stich = new List<double[]>();
            for (int i = 0; i < 10; i++)
                for (int j = 0; j < 10; j++)
                {
                    double[] q = { xmin + (xmax - xmin) * (i + 0.5) / 10.0, ymin + (ymax - ymin) * (j + 0.5) / 10.0 };
                    if (Innen2D(ring, q) && !pg.Loecher.Any(l => Innen2D(l.Punkte.ToList(), q))) stich.Add(q);
                }
            if (stich.Count == 0) return null;
            double hoehe = boden ? pg.Aussen.Boden.Average() : pg.Aussen.Decke.Average();
            string beste = null;
            int besteZahl = 0;
            double besterAbstand = double.MaxValue;
            foreach (Flaeche f in eigene.Where(x => !x.Wand).OrderBy(x => x.Bauteil.Kennung, StringComparer.Ordinal))
            {
                if (Math.Abs(f.Normale[2]) < 1e-6) continue;
                double abstand = stich.Select(q => Math.Abs(f.Aussen[0][2] - (f.Normale[0] * (q[0] - f.Aussen[0][0]) + f.Normale[1] * (q[1] - f.Aussen[0][1])) / f.Normale[2] - hoehe)).Min();
                if (abstand > f.Dicke + ZUSCHLAG_ZUORDNUNG_M) continue;
                int zahl = stich.Count(q => Innen2D(f.Aussen, q));
                if (zahl > besteZahl || (zahl == besteZahl && zahl > 0 && abstand < besterAbstand - 1e-9))
                {
                    beste = f.Bauteil.Kennung;
                    besteZahl = zahl;
                    besterAbstand = abstand;
                }
            }
            return besteZahl > 0 ? beste : null;
        }

        /// <summary>
        /// Der Raumkörper aus allen Polygonen: jedes Polygon (samt Löchern) als eigener geschlossener Bestandteil — zwei
        /// aneinanderstoßende Polygone teilten sonst eine Kante unter vier Dreiecken —, danach zu einem Körper vereint.
        /// </summary>
        private static Koerperergebnis Raumkoerper(List<Raumpolygon> polygone)
        {
            var teile = new List<Dateikoerper>();
            foreach (Raumpolygon pg in polygone)
            {
                Koerperergebnis e = Koerperbildner.Raumprisma(new[] { pg });
                if (!e.Gebildet) return e;
                teile.Add(e.Koerper);
            }
            return new Koerperergebnis { Koerper = Vereint(teile, Koerperbildner.ART_RAUMPOLYGON, Enumerable.Empty<Koerpervermerk>()) };
        }

        /// <summary>Mehrere Körper als einer: Punkte, Dreiecke, Normalen, Randkanten und Quellflächen hintereinander.</summary>
        private static Dateikoerper Vereint(List<Dateikoerper> teile, string art, IEnumerable<Koerpervermerk> zusatz)
        {
            var punkte = new List<double[]>();
            var dreiecke = new List<int[]>();
            var normalen = new List<double[]>();
            var kanten = new List<int[]>();
            var quelle = new List<string>();
            var vermerke = new SortedSet<Koerpervermerk>(zusatz);
            foreach (Dateikoerper k in teile)
            {
                int o = punkte.Count;
                punkte.AddRange(k.PunkteM);
                dreiecke.AddRange(k.Dreiecke.Select(d => new[] { d[0] + o, d[1] + o, d[2] + o }));
                normalen.AddRange(k.Normalen);
                kanten.AddRange(k.Randkanten.Select(x => new[] { x[0] + o, x[1] + o }));
                quelle.AddRange(k.Quellflaechen);
                foreach (Koerpervermerk v in k.Vermerke) vermerke.Add(v);
            }
            return new Dateikoerper
            {
                PunkteM = punkte, Dreiecke = dreiecke, Normalen = normalen, Randkanten = kanten, Art = art,
                Vermerke = vermerke.ToList(), Quelle = Koerperquelle.AusFlaechen, Quellflaechen = quelle,
            };
        }

        // ---------------- Bauteilkörper (17.3) ----------------

        /// <summary>
        /// Die Lage der Fläche gegen die Raumkörper ihrer Räume: je Stichpunkt (Schwerpunkt und die Ecken halb zum Schwerpunkt)
        /// längs der Normale auf ein paralleles Dreieck eines Raumkörpers projiziert; der kleinste Abstand zählt. Daraus die
        /// Richtung (<see cref="Richtung"/>) und die Raumseite (Gegenrichtung der Dreiecksnormale, die nach außen zeigt). Ohne
        /// Treffer: die Raumseite zum Schwerpunkt des Raumpolygons, die Achse mit Vermerk.
        /// </summary>
        private static void Messen(Flaeche f, Dictionary<string, Dateikoerper> koerper, Dictionary<string, IReadOnlyList<Raumpolygon>> polygone)
        {
            double[] c = Schwerpunkt(f.Aussen);
            var stich = new List<double[]> { c };
            stich.AddRange(f.Aussen.Select(q => Polygonnetz.Mal(Polygonnetz.Plus(q, c), 0.5)));
            double bester = double.MaxValue;
            double[] zumRaum = null;
            foreach (string raum in f.Raeume.OrderBy(x => x, StringComparer.Ordinal))
            {
                if (!koerper.TryGetValue(raum, out Dateikoerper k)) continue;
                for (int t = 0; t < k.Dreiecke.Count; t++)
                {
                    double[] tn = k.Normalen[t];
                    double cos = Polygonnetz.Punkt(tn, f.Normale);
                    if (Math.Abs(cos) < COS_PARALLEL) continue;
                    double[] a = k.PunkteM[k.Dreiecke[t][0]], b = k.PunkteM[k.Dreiecke[t][1]], d = k.PunkteM[k.Dreiecke[t][2]];
                    foreach (double[] s in stich)
                    {
                        double weg = Polygonnetz.Punkt(Polygonnetz.Minus(a, s), tn) / cos;
                        double abstand = Math.Abs(weg);
                        if (abstand > f.Dicke + SUCHWEITE_M || abstand >= bester - 1e-9) continue;
                        if (!ImDreieck(Polygonnetz.Plus(s, Polygonnetz.Mal(f.Normale, weg)), a, b, d, tn)) continue;
                        bester = abstand;
                        zumRaum = Polygonnetz.Mal(tn, -1.0);
                    }
                }
            }
            if (zumRaum != null)
            {
                f.ZumRaum = zumRaum;
                (f.Richtung, f.Angenommen) = Richtung(bester, f.Dicke);
                // Befund an der Projektdatei: Der Aufbau trägt oft nur die bauphysikalischen Schichten, die Bezugsebene liegt
                // aber um die Dicke an der Fläche (Thickness, die gezeichnete Wanddicke) vor der Innenoberfläche. Passt die
                // Messung nur zu dieser Dicke, gilt sie — gemessen, nicht angenommen.
                if (f.Angenommen && f.Flaechendicke is double fd && !Richtung(bester, fd).Angenommen)
                {
                    f.Dicke = fd;
                    f.Vorgabe = false;
                    (f.Richtung, f.Angenommen) = Richtung(bester, fd);
                }
                return;
            }
            // Kein Raumkörper trifft: Raumseite zum Schwerpunkt des Raumpolygons, sonst die Gegenrichtung der Dateinormale.
            f.Richtung = Extrusionsrichtung.Beidseitig;
            f.Angenommen = true;
            foreach (string raum in f.Raeume.OrderBy(x => x, StringComparer.Ordinal))
                if (polygone.TryGetValue(raum, out IReadOnlyList<Raumpolygon> pg) && pg.Count > 0)
                {
                    Raumring r = pg[0].Aussen;
                    double[] m =
                    {
                        r.Punkte.Average(q => q[0]), r.Punkte.Average(q => q[1]), (r.Boden.Average() + r.Decke.Average()) / 2.0,
                    };
                    double s = Polygonnetz.Punkt(Polygonnetz.Minus(m, c), f.Normale);
                    if (Math.Abs(s) > 1e-9)
                    {
                        f.ZumRaum = Polygonnetz.Mal(f.Normale, Math.Sign(s));
                        return;
                    }
                }
            f.ZumRaum = Polygonnetz.Mal(f.Normale, -1.0);
        }

        /// <summary>
        /// Die Extrusionsrichtung aus dem gemessenen Abstand der Bezugsebene zur Innenoberfläche (17.3 Nr. 3): höchstens 1 cm →
        /// vom Raum weg; eine halbe Dicke → beidseitig; eine Dicke → zum Raum hin; sonst die Achse, angenommen. Die Toleranz der
        /// beiden letzten Fälle ist ein Viertel der Dicke, höchstens 5 cm, mindestens 1 cm.
        /// </summary>
        internal static (Extrusionsrichtung Richtung, bool Angenommen) Richtung(double abstandM, double dickeM)
        {
            if (abstandM <= INNENFLAECHE_M) return (Extrusionsrichtung.NachAussen, false);
            double tol = Math.Max(INNENFLAECHE_M, Math.Min(0.25 * dickeM, 0.05));
            if (Math.Abs(abstandM - dickeM / 2.0) <= tol) return (Extrusionsrichtung.Beidseitig, false);
            if (Math.Abs(abstandM - dickeM) <= tol) return (Extrusionsrichtung.NachInnen, false);
            return (Extrusionsrichtung.Beidseitig, true);
        }

        /// <summary>Extrudiert die Fläche samt Löchern und weiteren Bestandteilen; setzt Körper oder Meldung. <c>true</c> = gebildet.</summary>
        private static bool Extrudieren(Flaeche f)
        {
            var teile = new List<Dateikoerper>();
            var ringe = new List<(List<double[]> Aussen, List<List<double[]>> Loecher, List<string> Kennungen)> { (f.Aussen, f.Loecher, f.Lochkennungen) };
            ringe.AddRange(f.Weitere.Select(w => (w, new List<List<double[]>>(), new List<string>())));
            foreach (var r in ringe)
            {
                var q = new Quellflaeche
                {
                    Kennung = f.Bauteil.Kennung, Aussen = r.Aussen, Loecher = r.Loecher.Cast<IReadOnlyList<double[]>>().ToList(), Lochkennungen = r.Kennungen,
                };
                Koerperergebnis e = Koerperbildner.Extrusion(q, f.Dicke, f.Richtung, f.ZumRaum);
                if (!e.Gebildet)
                {
                    f.Bauteil.Koerper = null;
                    f.Bauteil.Meldungen.Add(new PruefMeldung(PruefStufe.Warnung, KOERPER_BAUTEIL, e.Grund, string.Join(",", e.Werte)));
                    return false;
                }
                teile.Add(e.Koerper);
            }
            var zusatz = new List<Koerpervermerk>();
            if (f.Vorgabe) zusatz.Add(Koerpervermerk.Vorgabedicke);
            if (f.Angenommen) zusatz.Add(Koerpervermerk.Bezugsebene_angenommen);
            f.Bauteil.Koerper = Vereint(teile, Koerperbildner.ART_FLAECHENEXTRUSION, zusatz);
            return true;
        }

        // ---------------- Öffnungen (17.3 Nr. 4) ----------------

        /// <summary>
        /// Die Wand einer Öffnung: zuerst ihr Wirt, sonst eine Wandfläche desselben Raums, parallel und höchstens Dicke + 30 cm
        /// entfernt, in deren Ring der projizierte Schwerpunkt der Öffnung liegt; <c>null</c> = keine.
        /// </summary>
        private static Flaeche Wand(Flaeche oeffnung, Flaeche wirt, List<Flaeche> flaechen)
        {
            double[] c = Schwerpunkt(oeffnung.Aussen);
            bool Traegt(Flaeche w)
            {
                if (w == null || w.Oeffnung || Math.Abs(Polygonnetz.Punkt(w.Normale, oeffnung.Normale)) < COS_PARALLEL) return false;
                double abstand = Math.Abs(Polygonnetz.Punkt(Polygonnetz.Minus(c, w.Aussen[0]), w.Normale));
                if (abstand > w.Dicke + SUCHWEITE_M) return false;
                (double[] o, double[] e1, double[] e2) = Ebene(w.Aussen);
                return e1 != null && Innen2D(Flach(w.Aussen, o, e1, e2), Flach(new[] { c }, o, e1, e2)[0]);
            }
            if (Traegt(wirt)) return wirt;
            return flaechen.Where(w => !ReferenceEquals(w, wirt) && w.Raeume.Intersect(oeffnung.Raeume, StringComparer.OrdinalIgnoreCase).Any())
                           .OrderBy(w => w.Bauteil.Kennung, StringComparer.Ordinal).FirstOrDefault(Traegt);
        }

        /// <summary>Der Ring, senkrecht in die Ebene der Wand projiziert.</summary>
        private static List<double[]> Projiziert(List<double[]> ring, Flaeche wand)
            => ring.Select(q => Polygonnetz.Minus(q, Polygonnetz.Mal(wand.Normale, Polygonnetz.Punkt(Polygonnetz.Minus(q, wand.Aussen[0]), wand.Normale)))).ToList();

        /// <summary>Liegt der Lochring ganz im Wandring, jede Ecke mindestens <see cref="LOCHRAND_M"/> vom Rand und von den übrigen Löchern frei?</summary>
        private static bool StrengInnen(List<double[]> loch, Flaeche wand)
        {
            (double[] o, double[] e1, double[] e2) = Ebene(wand.Aussen);
            if (e1 == null) return false;
            List<double[]> w = Flach(wand.Aussen, o, e1, e2), l = Flach(loch, o, e1, e2);
            if (!l.All(q => Innen2D(w, q) && RandAbstand2D(w, q) >= LOCHRAND_M)) return false;
            foreach (List<double[]> anderes in wand.Loecher)
            {
                List<double[]> a = Flach(anderes, o, e1, e2);
                if (l.Any(q => Innen2D(a, q)) || a.Any(q => Innen2D(l, q))) return false;
            }
            return true;
        }

        // ==================================================================
        //  Grundriss (17.2 „Grundriss“)
        // ==================================================================

        /// <summary>
        /// Der Grundriss eines Raums unmittelbar aus seinem Raumpolygon: die Ringe der Polygone (Löcher im Uhrzeigersinn),
        /// Boden die tiefste Bodenhöhe, Höhe bis zur höchsten Decke; Herleitung <see cref="Umrissherleitung.Boden"/>, Vermerk
        /// <see cref="Grundrissvermerk.Raumpolygon"/>. <c>null</c> = kein Raumpolygon.
        /// </summary>
        internal static Raumgrundriss Grundriss(SqprojGebaeudeAbbild abbild, AbbildRaum r, string geschoss, double? geschossLageM)
        {
            if (abbild == null || r == null || !abbild.Raumpolygone.TryGetValue(r.Kennung, out IReadOnlyList<Raumpolygon> polygone)) return null;
            var ringe = new List<Grundrissring>();
            foreach (Raumpolygon pg in polygone)
            {
                Grundrissring a = GebaeudeRaumgrundrisse.Ring(pg.Aussen.Punkte);
                if (a == null) continue;
                ringe.Add(a);
                foreach (Raumring l in pg.Loecher)
                    if (GebaeudeRaumgrundrisse.Ring(l.Punkte) is Grundrissring g) ringe.Add(new Grundrissring(g.PunkteMm.Reverse().ToList()));
            }
            List<Raumring> alle = polygone.SelectMany(pg => new[] { pg.Aussen }.Concat(pg.Loecher)).ToList();
            double boden = alle.SelectMany(x => x.Boden).Min();
            double hoehe = alle.SelectMany(x => x.Decke).Max() - boden;
            string name = string.IsNullOrWhiteSpace(r.Name) ? null : r.Name;
            return Raumgrundriss.Bilden(r.Kennung, name, geschoss, geschossLageM, r.FlaecheM2, r.VolumenM3, Umrissherleitung.Boden, ringe,
                                        boden, hoehe, new[] { Grundrissvermerk.Raumpolygon }, false);
        }

        // ==================================================================
        //  Hilfen
        // ==================================================================

        /// <summary>Der Ring, so umlaufen, dass seine Newell-Normale von der Raumseite weg zeigt; ohne Raumseite wie gelesen.</summary>
        private static IReadOnlyList<double[]> Gerichtet(List<double[]> ring, double[] zumRaum)
        {
            var r = ring.Select(q => new[] { q[0], q[1], q[2] }).ToList();
            if (zumRaum != null && Polygonnetz.Punkt(Polygonnetz.Newell(r), zumRaum) > 0.0) r.Reverse();
            return r;
        }

        /// <summary>
        /// Der Ring ohne kollineare Zwischenpunkte und ohne Stichkanten (hin und auf derselben Linie zurück, 1 mm) — die
        /// Datei führt solche Punkte an T-Stößen; ein Mantelviereck auf einer Stichkante schlösse den Körper nie.
        /// </summary>
        internal static List<double[]> Gestrafft(IReadOnlyList<double[]> ring)
        {
            var r = Polygonnetz.Bereinigt(ring);
            bool weiter = true;
            while (weiter && r.Count >= 3)
            {
                weiter = false;
                for (int i = 0; i < r.Count && r.Count >= 3; i++)
                {
                    double[] a = r[(i + r.Count - 1) % r.Count], b = r[i], c = r[(i + 1) % r.Count];
                    double[] ab = Polygonnetz.Minus(b, a), ac = Polygonnetz.Minus(c, a);
                    double lac = Math.Sqrt(Polygonnetz.Punkt(ac, ac));
                    double[] k = Polygonnetz.Kreuz(ab, ac);
                    double abstand = lac > 1e-12 ? Math.Sqrt(Polygonnetz.Punkt(k, k)) / lac : 0.0;   // a = c: Stichkante
                    if (abstand > Koerperbildner.TOLERANZ_M) continue;
                    r.RemoveAt(i);
                    r = Polygonnetz.Bereinigt(r);
                    weiter = true;
                    break;
                }
            }
            return r;
        }

        private static double[] Schwerpunkt(IReadOnlyList<double[]> ring)
            => new[] { ring.Average(q => q[0]), ring.Average(q => q[1]), ring.Average(q => q[2]) };

        /// <summary>Ursprung und zwei Achsen der Ebene eines Rings; Achsen <c>null</c> bei entartetem Ring.</summary>
        private static (double[], double[], double[]) Ebene(IReadOnlyList<double[]> ring)
        {
            double[] n = Polygonnetz.Normiert(Polygonnetz.Newell(ring));
            if (n == null) return (ring.Count > 0 ? ring[0] : new double[3], null, null);
            double[] hilf = Math.Abs(n[2]) < 0.9 ? new[] { 0.0, 0.0, 1.0 } : new[] { 1.0, 0.0, 0.0 };
            double[] e1 = Polygonnetz.Normiert(Polygonnetz.Kreuz(hilf, n));
            double[] e2 = Polygonnetz.Kreuz(n, e1);
            return (ring[0], e1, e2);
        }

        private static List<double[]> Flach(IEnumerable<double[]> ring, double[] o, double[] e1, double[] e2)
            => ring.Select(q => { double[] d = Polygonnetz.Minus(q, o); return new[] { Polygonnetz.Punkt(d, e1), Polygonnetz.Punkt(d, e2) }; }).ToList();

        /// <summary>Punkt im Vieleck (gerade-ungerade-Regel), nur x und y.</summary>
        internal static bool Innen2D(IReadOnlyList<double[]> ring, double[] x)
        {
            bool innen = false;
            for (int i = 0, j = ring.Count - 1; i < ring.Count; j = i++)
            {
                double[] a = ring[i], b = ring[j];
                if ((a[1] > x[1]) != (b[1] > x[1]) && x[0] < (b[0] - a[0]) * (x[1] - a[1]) / (b[1] - a[1]) + a[0]) innen = !innen;
            }
            return innen;
        }

        private static bool AufRand2D(IReadOnlyList<double[]> ring, double[] x) => RandAbstand2D(ring, x) <= Koerperbildner.TOLERANZ_M;

        private static double RandAbstand2D(IReadOnlyList<double[]> ring, double[] x)
        {
            double best = double.MaxValue;
            for (int i = 0; i < ring.Count; i++)
            {
                double[] a = ring[i], b = ring[(i + 1) % ring.Count];
                double dx = b[0] - a[0], dy = b[1] - a[1], l2 = dx * dx + dy * dy;
                double t = l2 < 1e-18 ? 0.0 : Math.Clamp(((x[0] - a[0]) * dx + (x[1] - a[1]) * dy) / l2, 0.0, 1.0);
                double px = a[0] + t * dx - x[0], py = a[1] + t * dy - x[1];
                best = Math.Min(best, Math.Sqrt(px * px + py * py));
            }
            return best;
        }

        /// <summary>Liegt der Punkt (in der Ebene des Dreiecks) im Dreieck, mit 1 mm Toleranz?</summary>
        private static bool ImDreieck(double[] p, double[] a, double[] b, double[] c, double[] n)
        {
            double Seite(double[] u, double[] v)
            {
                double[] kante = Polygonnetz.Minus(v, u);
                double l = Math.Sqrt(Polygonnetz.Punkt(kante, kante));
                if (l < 1e-12) return 0.0;
                return Polygonnetz.Punkt(Polygonnetz.Kreuz(kante, Polygonnetz.Minus(p, u)), n) / l;
            }
            double tol = -Koerperbildner.TOLERANZ_M;
            return Seite(a, b) >= tol && Seite(b, c) >= tol && Seite(c, a) >= tol;
        }

        private static string Z(int n) => n.ToString(CultureInfo.InvariantCulture);
    }
}
