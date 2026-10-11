using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Xbim.Common;
using Xbim.Ifc4.Interfaces;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Körper eines Raums aus der IFC-Datei ohne Geometriekern</b> (Datenaustauschkonzept 15.2, Stufe G7f-1;
    /// ADR-003): liest die Darstellung „Body“ eines <c>IfcSpace</c> über die Schnittstellen <c>IIfc*</c> — damit gleich
    /// für IFC2X3 und IFC4 — und ergibt je Darstellungsträger ein Dreiecksnetz im System des Raums, aneinandergehängt,
    /// nicht vereinigt; über die Placement-Kette (<see cref="IfcPlatzierung.Weltrahmen"/>) in Weltkoordinaten, in der
    /// Längeneinheit der Datei, am Ende in Meter und auf <see cref="Zonenkoerper.STELLEN"/> gerundet.
    ///
    /// <list type="bullet">
    /// <item><b>Extrusion</b> (<c>IfcExtrudedAreaSolid</c>): Rechteck, Kreis (Sehnenzug, Vermerk <c>Bogen</c>),
    /// beliebiges geschlossenes Profil aus <c>IfcPolyline</c>, <c>IfcIndexedPolyCurve</c> oder <c>IfcCompositeCurve</c>
    /// (Bögen als Sehnenzug mit <see cref="Dateikoerper.SEHNEN_VOLLKREIS"/> Sehnen je Vollkreis), Profil mit Löchern
    /// (Brückenkante, sonst nur der Außenring mit Vermerk <c>Loch</c>); Richtung beliebig, Tiefe in Dateieinheit.</item>
    /// <item><b>Flächen</b> (<c>IfcFacetedBrep</c>, <c>…WithVoids</c> nur die Außenschale mit Vermerk <c>Mehrschale</c>,
    /// <c>IfcShellBasedSurfaceModel</c>, <c>IfcFaceBasedSurfaceModel</c>, offene Schale mit Vermerk <c>Offen</c>): je
    /// Fläche ein ebenes Vieleck mit Ohrenschnitt; nicht eben: Fächer vom ersten Punkt, Vermerk <c>Uneben</c>.</item>
    /// <item><b>Tessellation</b> (<c>IfcTriangulatedFaceSet</c> direkt, <c>IfcPolygonalFaceSet</c> je Fläche).</item>
    /// <item><b><c>IfcMappedItem</c></b> über <c>MappingOrigin</c> und <c>MappingTarget</c>, auch mit Maßstab;
    /// <b><c>IfcBooleanResult</c></b> nur mit dem ersten Operanden, Vermerk <c>OhneBeschnitt</c>.</item>
    /// </list>
    /// Jede übrige Art (<c>IfcAdvancedBrep</c>, <c>IfcCsgSolid</c>, <c>IfcSweptDiskSolid</c>,
    /// <c>IfcRevolvedAreaSolid</c> …) ist benannt nicht lesbar: ihr EXPRESS-Name landet in der Liste des Aufrufers
    /// (<c>IMP_IFC_PROT_KOERPER_ART</c>). Nichts wird geschlossen oder repariert (15.6 Nr. 2).
    ///
    /// <para><b>Deterministisch und linear:</b> Träger in der Reihenfolge der Datei, Ohrenschnitt mit fester
    /// Startecke, keine Hash-Reihenfolge in der Ausgabe; der Aufwand wächst mit der Zahl der Träger und Flächen (der
    /// Ohrenschnitt nur mit der Eckenzahl einer Fläche).</para>
    /// </summary>
    internal static class IfcRaumkoerper
    {
        /// <summary>Die Kennung der Körperdarstellung.</summary>
        internal const string BODY = "Body";

        /// <summary>Die größte Schachtelungstiefe von <c>IfcMappedItem</c> und booleschen Körpern (Schutz vor Kreisen).</summary>
        private const int TIEFE_MAX = 16;

        // ==================================================================
        //  Einstieg
        // ==================================================================

        /// <summary>
        /// Der Körper eines Raums; <c>null</c> = keine Darstellung oder kein lesbarer Träger. Jeder nicht lesbare
        /// Träger kommt mit seinem EXPRESS-Namen nach <paramref name="nichtLesbar"/>.
        /// </summary>
        /// <param name="raum">Der Raum.</param>
        /// <param name="welt">Sein Weltrahmen (Placement-Kette), in Längeneinheiten der Datei.</param>
        /// <param name="laenge">Der Faktor der Längeneinheit nach Meter.</param>
        /// <param name="winkel">Der Faktor der Winkeleinheit nach Radiant (Parameter getrimmter Kreise).</param>
        /// <param name="nichtLesbar">Nimmt die Arten der nicht lesbaren Träger auf.</param>
        internal static Dateikoerper Lesen(IIfcProduct raum, IfcRahmen welt, double laenge, double winkel, ICollection<string> nichtLesbar)
        {
            IIfcShapeRepresentation darstellung = Darstellung(raum);
            if (darstellung == null) return null;
            var netz = new Netz(laenge, winkel);
            var arten = new List<string>();
            Abb wurzel = Abb.Aus(welt);
            foreach (IIfcRepresentationItem item in darstellung.Items)
            {
                int[] stand = netz.Stand();
                string fehlt;
                try { fehlt = Traeger(item, wurzel, netz, 0); }
                catch (Exception ex) when (ex is NotSupportedException || ex is InvalidCastException || ex is NullReferenceException
                                           || ex is ArgumentException || ex is IndexOutOfRangeException || ex is InvalidOperationException)
                {
                    fehlt = Typ(item);
                }
                if (fehlt != null)
                {
                    netz.Zuruecksetzen(stand);
                    nichtLesbar?.Add(fehlt);
                    continue;
                }
                string art = Schluessel(item);
                if (!arten.Contains(art)) arten.Add(art);
            }
            return arten.Count == 0 ? null : netz.Abschluss(string.Join("+", arten));
        }

        /// <summary>Die Darstellung „Body“, sonst die erste mit einem 3D-Träger; <c>null</c> = keine.</summary>
        internal static IIfcShapeRepresentation Darstellung(IIfcProduct raum)
        {
            List<IIfcShapeRepresentation> alle = raum?.Representation?.Representations?.OfType<IIfcShapeRepresentation>().ToList();
            if (alle == null || alle.Count == 0) return null;
            return alle.FirstOrDefault(d => string.Equals(IfcEigenschaften.Text(d.RepresentationIdentifier), BODY, StringComparison.OrdinalIgnoreCase))
                   ?? alle.FirstOrDefault(d => d.Items.Any(Raeumlich));
        }

        /// <summary>Trägt der Träger einen Körper oder eine Fläche im Raum (keine Kurve, kein Punkt)?</summary>
        private static bool Raeumlich(IIfcRepresentationItem i)
            => i is IIfcSolidModel || i is IIfcFaceBasedSurfaceModel || i is IIfcShellBasedSurfaceModel || i is IIfcTessellatedFaceSet
               || i is IIfcMappedItem || i is IIfcBooleanResult;

        /// <summary>
        /// Der Faktor der Winkeleinheit nach Radiant aus den Einheiten des Projekts (<c>PLANEANGLEUNIT</c>); ohne
        /// Angabe Radiant.
        /// </summary>
        internal static double Winkelfaktor(IIfcProject projekt)
        {
            IEnumerable<IIfcNamedUnit> einheiten = projekt?.UnitsInContext?.Units?.OfType<IIfcNamedUnit>() ?? Enumerable.Empty<IIfcNamedUnit>();
            foreach (IIfcNamedUnit e in einheiten)
            {
                if (e.UnitType != IfcUnitEnum.PLANEANGLEUNIT) continue;
                if (e is IIfcSIUnit si) return IfcEinheiten.PrefixFaktor(si.Prefix);
                if (e is IIfcConversionBasedUnit k && k.ConversionFactor != null)
                {
                    double f = k.ConversionFactor.ValueComponent is IExpressValueType w ? IfcEigenschaften.Wert(w) : double.NaN;
                    if (f > 0.0) return f;
                }
            }
            return 1.0;
        }

        private static string Typ(object e) => (e as IPersistEntity)?.ExpressType?.ExpressName ?? e?.GetType().Name ?? "";

        /// <summary>Der sprachneutrale Schlüssel eines Trägers: EXPRESS-Name ohne „Ifc“.</summary>
        internal static string Schluessel(object e)
        {
            string n = Typ(e);
            return n.StartsWith("Ifc", StringComparison.Ordinal) ? n.Substring(3) : n;
        }

        // ==================================================================
        //  Träger
        // ==================================================================

        /// <summary>Liest einen Träger in das Netz; Ergebnis <c>null</c> = gelesen, sonst die nicht lesbare Art.</summary>
        private static string Traeger(IIfcRepresentationItem item, Abb a, Netz n, int tiefe)
        {
            if (tiefe > TIEFE_MAX) return Typ(item);
            switch (item)
            {
                case IIfcExtrudedAreaSolidTapered _:
                    return Typ(item);
                case IIfcExtrudedAreaSolid e:
                    return Extrusion(e, a, n);
                case IIfcFacetedBrepWithVoids bv:
                    n.Vermerk(Koerpervermerk.Mehrschale);
                    Flaechensatz(bv.Outer, a, n, offen: false);
                    return null;
                case IIfcFacetedBrep b:
                    Flaechensatz(b.Outer, a, n, offen: false);
                    return null;
                case IIfcShellBasedSurfaceModel sb:
                    foreach (IIfcShell schale in sb.SbsmBoundary)
                        Flaechensatz(schale as IIfcConnectedFaceSet, a, n, offen: !(schale is IIfcClosedShell));
                    return null;
                case IIfcFaceBasedSurfaceModel fb:
                    foreach (IIfcConnectedFaceSet satz in fb.FbsmFaces)
                        Flaechensatz(satz, a, n, offen: !(satz is IIfcClosedShell));
                    return null;
                case IIfcTriangulatedFaceSet t:
                    return Dreiecksnetz(t, a, n);
                case IIfcPolygonalFaceSet p:
                    return Vieleckssatz(p, a, n);
                case IIfcMappedItem m:
                {
                    IIfcRepresentationMap quelle = m.MappingSource;
                    if (quelle?.MappedRepresentation == null) return Typ(item);
                    Abb b = a.Dann(Abb.Aus(m.MappingTarget)).Dann(Abb.Aus(IfcPlatzierung.Lokal(quelle.MappingOrigin)));
                    bool gelesen = false;
                    string fehlt = null;
                    foreach (IIfcRepresentationItem innen in quelle.MappedRepresentation.Items)
                    {
                        int[] stand = n.Stand();
                        string f = Traeger(innen, b, n, tiefe + 1);
                        if (f == null) gelesen = true;
                        else { n.Zuruecksetzen(stand); fehlt ??= f; }
                    }
                    return gelesen ? null : fehlt ?? Typ(item);
                }
                case IIfcBooleanResult br:
                {
                    if (!(br.FirstOperand is IIfcRepresentationItem erster)) return Typ(item);
                    string f = Traeger(erster, a, n, tiefe + 1);
                    if (f != null) return f;
                    n.Vermerk(Koerpervermerk.OhneBeschnitt);
                    return null;
                }
                default:
                    return Typ(item);
            }
        }

        // ==================================================================
        //  Extrusion
        // ==================================================================

        /// <summary>Ein Punkt eines Rings in der Profilebene; <c>Ecke</c> = kein Zwischenpunkt eines Bogens.</summary>
        private readonly struct Ringpunkt
        {
            public Ringpunkt(double x, double y, bool ecke) { X = x; Y = y; Ecke = ecke; }
            public double X { get; }
            public double Y { get; }
            public bool Ecke { get; }
        }

        private static string Extrusion(IIfcExtrudedAreaSolid e, Abb a, Netz n)
        {
            var ringe = new List<List<Ringpunkt>>();
            string fehlt = Profil(e.SweptArea, n, ringe);
            if (fehlt != null) return Typ(e) + "/" + fehlt;
            double[] d = IfcPlatzierung.Richtung(e.ExtrudedDirection);
            double tiefe = IfcEigenschaften.Wert(e.Depth);
            double l = d == null ? 0.0 : Math.Sqrt(d[0] * d[0] + d[1] * d[1] + d[2] * d[2]);
            if (!(l > 1e-12) || !(tiefe > 0.0) || Math.Abs(d[2] / l) < 1e-9) return Typ(e);
            double[] w = { d[0] / l * tiefe, d[1] / l * tiefe, d[2] / l * tiefe };
            Abb b = e.Position == null ? a : a.Dann(Abb.Aus(IfcPlatzierung.Lokal(e.Position)));
            // Mit negativem z-Anteil liegt der Körper unter der Profilebene — der Umlauf kehrt sich um.
            bool kehren = w[2] < 0.0;

            // Die Deckflächen: Ohrenschnitt des Profils (außen gegen den Uhrzeigersinn, Löcher im Uhrzeigersinn).
            List<double[]> eben = ringe.SelectMany(r => r).Select(p => new[] { p.X, p.Y }).ToList();
            var ringEben = new List<List<int>>();
            int k = 0;
            foreach (List<Ringpunkt> r in ringe)
            {
                ringEben.Add(Enumerable.Range(k, r.Count).ToList());
                k += r.Count;
            }
            List<int[]> deckel = Dreiecke2D(eben, ringEben, out List<int> verloren);
            if (verloren.Count > 0) n.Vermerk(Koerpervermerk.Loch);

            var unten = new int[eben.Count];
            var oben = new int[eben.Count];
            for (int i = 0; i < eben.Count; i++)
            {
                unten[i] = n.Punkt(b, new[] { eben[i][0], eben[i][1], 0.0 });
                oben[i] = n.Punkt(b, new[] { eben[i][0] + w[0], eben[i][1] + w[1], w[2] });
            }
            foreach (int[] t in deckel)
            {
                n.Dreieck(b, unten[t[0]], unten[t[2]], unten[t[1]], kehren);
                n.Dreieck(b, oben[t[0]], oben[t[1]], oben[t[2]], kehren);
            }
            // Die Mantelflächen je Ring; ein nicht angebundenes Loch entfällt ganz.
            for (int r = 0; r < ringe.Count; r++)
            {
                if (verloren.Contains(r)) continue;
                List<int> ring = ringEben[r];
                for (int i = 0; i < ring.Count; i++)
                {
                    int p = ring[i], q = ring[(i + 1) % ring.Count];
                    n.Dreieck(b, unten[p], unten[q], oben[q], kehren);
                    n.Dreieck(b, unten[p], oben[q], oben[p], kehren);
                    n.Kante(unten[p], unten[q]);
                    n.Kante(oben[p], oben[q]);
                    if (ringe[r][i].Ecke) n.Kante(unten[p], oben[p]);
                }
            }
            return null;
        }

        /// <summary>Die Ringe eines Profils in der Profilebene; <c>null</c> = gelesen, sonst die nicht lesbare Art.</summary>
        private static string Profil(IIfcProfileDef profil, Netz n, List<List<Ringpunkt>> ringe)
        {
            switch (profil)
            {
                case IIfcRoundedRectangleProfileDef _:
                case IIfcRectangleHollowProfileDef _:
                case IIfcCircleHollowProfileDef _:
                    return Typ(profil);
                case IIfcRectangleProfileDef r:
                {
                    double hx = IfcEigenschaften.Wert(r.XDim) / 2.0, hy = IfcEigenschaften.Wert(r.YDim) / 2.0;
                    if (!(hx > 0.0) || !(hy > 0.0)) return Typ(profil);
                    var ring = new[] { (-hx, -hy), (hx, -hy), (hx, hy), (-hx, hy) }
                        .Select(p => new Ringpunkt(p.Item1, p.Item2, true)).ToList();
                    ringe.Add(Gelegt(ring, r.Position));
                    return null;
                }
                case IIfcCircleProfileDef c:
                {
                    double rad = IfcEigenschaften.Wert(c.Radius);
                    if (!(rad > 0.0)) return Typ(profil);
                    var ring = new List<Ringpunkt>();
                    for (int i = 0; i < Dateikoerper.SEHNEN_VOLLKREIS; i++)
                    {
                        double phi = 2.0 * Math.PI * i / Dateikoerper.SEHNEN_VOLLKREIS;
                        ring.Add(new Ringpunkt(rad * Math.Cos(phi), rad * Math.Sin(phi), false));
                    }
                    n.Vermerk(Koerpervermerk.Bogen);
                    ringe.Add(Gelegt(ring, c.Position));
                    return null;
                }
                case IIfcArbitraryClosedProfileDef ap:
                {
                    List<Ringpunkt> aussen = Ring(ap.OuterCurve, n, out string fehlt);
                    if (aussen == null) return Typ(profil) + "/" + fehlt;
                    ringe.Add(Gerichtet(aussen, gegenUhrzeiger: true));
                    if (ap is IIfcArbitraryProfileDefWithVoids mitLoch)
                        foreach (IIfcCurve innen in mitLoch.InnerCurves)
                        {
                            List<Ringpunkt> loch = Ring(innen, n, out _);
                            if (loch == null) { n.Vermerk(Koerpervermerk.Loch); continue; }
                            ringe.Add(Gerichtet(loch, gegenUhrzeiger: false));
                        }
                    return null;
                }
                default:
                    return Typ(profil);
            }
        }

        /// <summary>Ein Ring über die <c>Position</c> eines Profils in dessen Ebene gelegt.</summary>
        private static List<Ringpunkt> Gelegt(List<Ringpunkt> ring, IIfcAxis2Placement2D lage)
        {
            if (lage == null) return ring;
            IfcRahmen r = IfcPlatzierung.Lokal(lage);
            return ring.Select(p =>
            {
                double[] q = IfcPlatzierung.Abbilden(r, new[] { p.X, p.Y, 0.0 });
                return new Ringpunkt(q[0], q[1], p.Ecke);
            }).ToList();
        }

        /// <summary>Ein Ring im gewünschten Umlauf.</summary>
        private static List<Ringpunkt> Gerichtet(List<Ringpunkt> ring, bool gegenUhrzeiger)
        {
            double f = 0.0;
            for (int i = 0; i < ring.Count; i++)
            {
                Ringpunkt p = ring[i], q = ring[(i + 1) % ring.Count];
                f += p.X * q.Y - q.X * p.Y;
            }
            if ((f > 0.0) != gegenUhrzeiger) ring.Reverse();
            return ring;
        }

        /// <summary>
        /// Der geschlossene Ring einer Profilkurve in der Ebene (x, y); <c>null</c> = nicht lesbar
        /// (<paramref name="fehlt"/> nennt die Art). Ein roh angehängter Schlusspunkt und doppelte Punkte entfallen.
        /// </summary>
        private static List<Ringpunkt> Ring(IIfcCurve kurve, Netz n, out string fehlt)
        {
            fehlt = null;
            List<Ringpunkt> zug;
            if (kurve is IIfcCircle kreis)
            {
                zug = Kreisbogen(kreis, 0.0, 2.0 * Math.PI, true, n);
                if (zug != null && zug.Count > 1) zug.RemoveAt(zug.Count - 1);
            }
            else zug = Zug(kurve, n, 0);
            if (zug == null) { fehlt = Typ(kurve); return null; }
            var ring = new List<Ringpunkt>();
            foreach (Ringpunkt p in zug)
            {
                if (ring.Count > 0 && Gleich(ring[ring.Count - 1], p))
                {
                    if (p.Ecke && !ring[ring.Count - 1].Ecke) ring[ring.Count - 1] = p;
                    continue;
                }
                ring.Add(p);
            }
            while (ring.Count > 1 && Gleich(ring[0], ring[ring.Count - 1]))
            {
                Ringpunkt letzt = ring[ring.Count - 1];
                if (letzt.Ecke && !ring[0].Ecke) ring[0] = letzt;
                ring.RemoveAt(ring.Count - 1);
            }
            if (ring.Count < 3) { fehlt = Typ(kurve); return null; }
            return ring;
        }

        private static bool Gleich(Ringpunkt a, Ringpunkt b) => Math.Abs(a.X - b.X) <= 1e-9 && Math.Abs(a.Y - b.Y) <= 1e-9;

        /// <summary>Der offene Zug einer Kurve (Anfang bis Ende); <c>null</c> = nicht lesbar.</summary>
        private static List<Ringpunkt> Zug(IIfcCurve kurve, Netz n, int tiefe)
        {
            if (tiefe > TIEFE_MAX) return null;
            switch (kurve)
            {
                case IIfcPolyline pl:
                    return pl.Points.Select(IfcPlatzierung.Punkt).Select(p => new Ringpunkt(p[0], p[1], true)).ToList();
                case IIfcIndexedPolyCurve ip:
                    return Indexzug(ip, n);
                case IIfcCompositeCurve cc:
                {
                    var zug = new List<Ringpunkt>();
                    foreach (IIfcCompositeCurveSegment s in cc.Segments)
                    {
                        List<Ringpunkt> teil = Zug(s.ParentCurve, n, tiefe + 1);
                        if (teil == null) return null;
                        if (!(bool)s.SameSense) teil.Reverse();
                        if (zug.Count > 0 && teil.Count > 0 && Gleich(zug[zug.Count - 1], teil[0])) teil.RemoveAt(0);
                        zug.AddRange(teil);
                    }
                    return zug;
                }
                case IIfcTrimmedCurve tc:
                    return Getrimmt(tc, n);
                default:
                    return null;
            }
        }

        /// <summary>Ein <c>IfcIndexedPolyCurve</c>: Linienstücke direkt, Bogenstücke (drei Punkte) als Sehnenzug.</summary>
        private static List<Ringpunkt> Indexzug(IIfcIndexedPolyCurve ip, Netz n)
        {
            List<double[]> punkte;
            switch (ip.Points)
            {
                case IIfcCartesianPointList2D l2:
                    punkte = l2.CoordList.Select(c => Koordinaten(c)).ToList();
                    break;
                case IIfcCartesianPointList3D l3:
                    punkte = l3.CoordList.Select(c => Koordinaten(c)).ToList();
                    break;
                default:
                    return null;
            }
            var zug = new List<Ringpunkt>();
            if (ip.Segments == null || ip.Segments.Count == 0)
                return punkte.Select(p => new Ringpunkt(p[0], p[1], true)).ToList();
            foreach (IIfcSegmentIndexSelect s in ip.Segments)
            {
                List<int> i = Ganzzahlen((s as IExpressValueType)?.Value).Select(x => x - 1).ToList();
                if (i.Count < 2 || i.Any(x => x < 0 || x >= punkte.Count)) return null;
                List<Ringpunkt> teil;
                if (s.GetType().Name == "IfcArcIndex" && i.Count == 3)
                    teil = Dreipunktbogen(punkte[i[0]], punkte[i[1]], punkte[i[2]], n);
                else
                    teil = i.Select(x => new Ringpunkt(punkte[x][0], punkte[x][1], true)).ToList();
                if (zug.Count > 0 && Gleich(zug[zug.Count - 1], teil[0])) teil.RemoveAt(0);
                zug.AddRange(teil);
            }
            return zug;
        }

        /// <summary>Die Indizes einer Indexliste (1-basiert, wie in der Datei); ein unlesbarer Eintrag wird 0.</summary>
        private static List<int> Indizes(IEnumerable folge)
        {
            var r = new List<int>();
            foreach (object w in folge)
            {
                double v = w is IExpressValueType e ? IfcEigenschaften.Wert(e) : double.NaN;
                r.Add(double.IsNaN(v) ? 0 : (int)v);
            }
            return r;
        }

        /// <summary>Die Ganzzahlen eines Indexwerts (Liste von <c>IfcPositiveInteger</c>).</summary>
        private static IEnumerable<int> Ganzzahlen(object werte)
        {
            if (!(werte is IEnumerable folge)) yield break;
            foreach (object w in folge)
            {
                double v = w is IExpressValueType e ? IfcEigenschaften.Wert(e) : double.NaN;
                yield return double.IsNaN(v) ? -1 : (int)v;
            }
        }

        private static double[] Koordinaten(IEnumerable folge)
        {
            var c = new double[3];
            int i = 0;
            foreach (object w in folge)
            {
                if (i >= 3) break;
                double v = w is IExpressValueType e ? IfcEigenschaften.Wert(e) : double.NaN;
                c[i++] = double.IsNaN(v) ? 0.0 : v;
            }
            return c;
        }

        /// <summary>Die Sehnenzahl eines Bogens mit dem Öffnungswinkel <paramref name="winkel"/> [rad]: anteilig, mindestens zwei.</summary>
        internal static int Sehnen(double winkel)
            => Math.Max(2, (int)Math.Ceiling(Dateikoerper.SEHNEN_VOLLKREIS * Math.Abs(winkel) / (2.0 * Math.PI) - 1e-9));

        /// <summary>Der Bogen durch drei Punkte als Sehnenzug (kollinear: die Strecke).</summary>
        private static List<Ringpunkt> Dreipunktbogen(double[] p1, double[] p2, double[] p3, Netz n)
        {
            double ax = p1[0], ay = p1[1], bx = p2[0], by = p2[1], cx = p3[0], cy = p3[1];
            double d = 2.0 * (ax * (by - cy) + bx * (cy - ay) + cx * (ay - by));
            if (Math.Abs(d) < 1e-12)
                return new List<Ringpunkt> { new Ringpunkt(ax, ay, true), new Ringpunkt(cx, cy, true) };
            double a2 = ax * ax + ay * ay, b2 = bx * bx + by * by, c2 = cx * cx + cy * cy;
            double mx = (a2 * (by - cy) + b2 * (cy - ay) + c2 * (ay - by)) / d;
            double my = (a2 * (cx - bx) + b2 * (ax - cx) + c2 * (bx - ax)) / d;
            double r = Math.Sqrt((ax - mx) * (ax - mx) + (ay - my) * (ay - my));
            double t1 = Math.Atan2(ay - my, ax - mx), tm = Math.Atan2(by - my, bx - mx), t3 = Math.Atan2(cy - my, cx - mx);
            double vor = Positiv(t3 - t1), mitte = Positiv(tm - t1);
            double weg = mitte <= vor ? vor : vor - 2.0 * Math.PI;   // gegen den Uhrzeigersinn, sonst mit ihm
            List<Ringpunkt> zug = Bogen(mx, my, r, t1, weg, n);
            zug[0] = new Ringpunkt(ax, ay, true);
            zug[zug.Count - 1] = new Ringpunkt(cx, cy, true);
            return zug;
        }

        private static double Positiv(double w)
        {
            double v = w % (2.0 * Math.PI);
            return v < 0.0 ? v + 2.0 * Math.PI : v;
        }

        /// <summary>Ein Kreisbogen um (mx, my) ab <paramref name="start"/> über <paramref name="weg"/> [rad] als Sehnenzug.</summary>
        private static List<Ringpunkt> Bogen(double mx, double my, double r, double start, double weg, Netz n)
        {
            int s = Sehnen(weg);
            var zug = new List<Ringpunkt>(s + 1);
            for (int i = 0; i <= s; i++)
            {
                double phi = start + weg * i / s;
                zug.Add(new Ringpunkt(mx + r * Math.Cos(phi), my + r * Math.Sin(phi), i == 0 || i == s));
            }
            n.Vermerk(Koerpervermerk.Bogen);
            return zug;
        }

        /// <summary>Ein Kreis oder Kreisbogen in seiner Lage (<c>IfcCircle.Position</c>).</summary>
        private static List<Ringpunkt> Kreisbogen(IIfcCircle kreis, double start, double weg, bool vollkreis, Netz n)
        {
            double r = IfcEigenschaften.Wert(kreis.Radius);
            if (!(r > 0.0)) return null;
            IfcRahmen lage = IfcPlatzierung.Lokal(kreis.Position);
            int s = Sehnen(weg);
            var zug = new List<Ringpunkt>(s + 1);
            for (int i = 0; i <= s; i++)
            {
                double phi = start + weg * i / s;
                double[] q = IfcPlatzierung.Abbilden(lage, new[] { r * Math.Cos(phi), r * Math.Sin(phi), 0.0 });
                zug.Add(new Ringpunkt(q[0], q[1], !vollkreis && (i == 0 || i == s)));
            }
            n.Vermerk(Koerpervermerk.Bogen);
            return zug;
        }

        /// <summary>Ein <c>IfcTrimmedCurve</c> über einem Kreis oder einer Geraden.</summary>
        private static List<Ringpunkt> Getrimmt(IIfcTrimmedCurve tc, Netz n)
        {
            bool sinn = (bool)tc.SenseAgreement;
            switch (tc.BasisCurve)
            {
                case IIfcCircle kreis:
                {
                    IfcRahmen lage = IfcPlatzierung.Lokal(kreis.Position);
                    double? a1 = Kreiswinkel(tc.Trim1, lage, n), a2 = Kreiswinkel(tc.Trim2, lage, n);
                    if (!a1.HasValue || !a2.HasValue) return null;
                    double weg = sinn ? Positiv(a2.Value - a1.Value) : -Positiv(a1.Value - a2.Value);
                    if (Math.Abs(weg) < 1e-12) weg = sinn ? 2.0 * Math.PI : -2.0 * Math.PI;
                    return Kreisbogen(kreis, a1.Value, weg, false, n);
                }
                case IIfcLine linie:
                {
                    double[] p0 = IfcPlatzierung.Punkt(linie.Pnt);
                    double[] v = IfcPlatzierung.Richtung(linie.Dir?.Orientation);
                    if (p0 == null || v == null) return null;
                    double[] e = IfcPlatzierung.Normiert(v);
                    if (e == null) return null;
                    double m = IfcEigenschaften.Wert(linie.Dir.Magnitude);
                    if (!(m > 0.0)) m = 1.0;
                    double[] Ende(IItemSet<IIfcTrimmingSelect> trim)
                    {
                        IIfcCartesianPoint pt = trim.OfType<IIfcCartesianPoint>().FirstOrDefault();
                        if (pt != null) return IfcPlatzierung.Punkt(pt);
                        IExpressValueType t = trim.OfType<IExpressValueType>().FirstOrDefault();
                        double w = t == null ? double.NaN : IfcEigenschaften.Wert(t);
                        return double.IsNaN(w) ? null : new[] { p0[0] + e[0] * m * w, p0[1] + e[1] * m * w, 0.0 };
                    }
                    double[] s = Ende(tc.Trim1), z = Ende(tc.Trim2);
                    if (s == null || z == null) return null;
                    var zug = new List<Ringpunkt> { new Ringpunkt(s[0], s[1], true), new Ringpunkt(z[0], z[1], true) };
                    return zug;
                }
                default:
                    return null;
            }
        }

        /// <summary>Der Winkel einer Trimmung am Kreis [rad]: aus dem Punkt, sonst aus dem Parameter in Winkeleinheit.</summary>
        private static double? Kreiswinkel(IItemSet<IIfcTrimmingSelect> trim, IfcRahmen lage, Netz n)
        {
            IIfcCartesianPoint pt = trim.OfType<IIfcCartesianPoint>().FirstOrDefault();
            if (pt != null)
            {
                double[] p = IfcPlatzierung.Minus(IfcPlatzierung.Punkt(pt), lage.Ursprung);
                return Math.Atan2(IfcPlatzierung.Punktprodukt(p, lage.Y), IfcPlatzierung.Punktprodukt(p, lage.X));
            }
            IExpressValueType t = trim.OfType<IExpressValueType>().FirstOrDefault();
            double w = t == null ? double.NaN : IfcEigenschaften.Wert(t);
            return double.IsNaN(w) ? (double?)null : w * n.Winkel;
        }

        // ==================================================================
        //  Flächen und Tessellation
        // ==================================================================

        /// <summary>Die Flächen eines Flächensatzes; eine offene Schale oder eine nicht lesbare Fläche ergibt den Vermerk „offen“.</summary>
        private static void Flaechensatz(IIfcConnectedFaceSet satz, Abb a, Netz n, bool offen)
        {
            if (satz == null) { n.Vermerk(Koerpervermerk.Offen); return; }
            if (offen) n.Vermerk(Koerpervermerk.Offen);
            foreach (IIfcFace f in satz.CfsFaces)
            {
                IIfcFaceBound aussen = f.Bounds.OfType<IIfcFaceOuterBound>().FirstOrDefault() ?? f.Bounds.FirstOrDefault();
                List<double[]> ring = Schleife(aussen);
                if (ring == null) { n.Vermerk(Koerpervermerk.Offen); continue; }
                var loecher = new List<List<double[]>>();
                foreach (IIfcFaceBound g in f.Bounds)
                {
                    if (ReferenceEquals(g, aussen)) continue;
                    List<double[]> loch = Schleife(g);
                    if (loch == null) n.Vermerk(Koerpervermerk.Loch);
                    else loecher.Add(loch);
                }
                Flaeche(ring, loecher, a, n);
            }
        }

        /// <summary>Die Punkte einer Berandung im Umlauf ihrer <c>Orientation</c>; <c>null</c> = keine <c>IfcPolyLoop</c>.</summary>
        private static List<double[]> Schleife(IIfcFaceBound g)
        {
            if (!(g?.Bound is IIfcPolyLoop schleife)) return null;
            List<double[]> p = schleife.Polygon.Select(IfcPlatzierung.Punkt).ToList();
            if (!(bool)g.Orientation) p.Reverse();
            return p;
        }

        /// <summary>
        /// Ein Vieleck im Raum mit Löchern: eben → Ohrenschnitt in der Ebene (Umlauf und Normale wie der Außenring);
        /// nicht eben → Fächer vom ersten Punkt, Vermerk „uneben“ (Löcher entfallen mit Vermerk).
        /// </summary>
        private static void Flaeche(List<double[]> aussenRoh, List<List<double[]>> loecherRoh, Abb a, Netz n)
        {
            List<double[]> aussen = Bereinigt(aussenRoh);
            if (aussen.Count < 3) return;
            double[] nrm = IfcPlatzierung.Normiert(Newell(aussen));
            if (nrm == null) return;
            double[] p0 = aussen[0];
            bool eben = aussen.All(p => Math.Abs(IfcPlatzierung.Punktprodukt(IfcPlatzierung.Minus(p, p0), nrm)) * n.Laenge <= IfcGrenzgeometrie.EBEN_TOLERANZ_M);
            var loecher = new List<List<double[]>>();
            foreach (List<double[]> l in loecherRoh)
            {
                List<double[]> b = Bereinigt(l);
                if (b.Count < 3) continue;
                if (b.Any(p => Math.Abs(IfcPlatzierung.Punktprodukt(IfcPlatzierung.Minus(p, p0), nrm)) * n.Laenge > IfcGrenzgeometrie.EBEN_TOLERANZ_M))
                    eben = false;
                loecher.Add(b);
            }

            var index = new List<int>();
            foreach (double[] p in aussen) index.Add(n.Punkt(a, p));
            for (int i = 0; i < aussen.Count; i++) n.Kante(index[i], index[(i + 1) % aussen.Count]);
            if (!eben)
            {
                n.Vermerk(Koerpervermerk.Uneben);
                if (loecher.Count > 0) n.Vermerk(Koerpervermerk.Loch);
                for (int i = 1; i + 1 < aussen.Count; i++) n.Dreieck(a, index[0], index[i], index[i + 1], false);
                return;
            }

            // In der Ebene: u längs der ersten Kante, v = n × u — der Außenring läuft dort gegen den Uhrzeigersinn.
            double[] u = null;
            for (int i = 1; i < aussen.Count && u == null; i++)
            {
                double[] d = IfcPlatzierung.Minus(aussen[i], p0);
                u = IfcPlatzierung.Normiert(IfcPlatzierung.Minus(d, IfcPlatzierung.Mal(nrm, IfcPlatzierung.Punktprodukt(d, nrm))));
            }
            if (u == null) return;
            double[] v = IfcPlatzierung.Kreuz(nrm, u);
            var eben2 = new List<double[]>();
            var ringe = new List<List<int>>();
            void Ring(List<double[]> r, bool gegenUhrzeiger, List<int> idx)
            {
                var stellen = new List<int>();
                var punkte = r.Select(p => { double[] d = IfcPlatzierung.Minus(p, p0); return new[] { IfcPlatzierung.Punktprodukt(d, u), IfcPlatzierung.Punktprodukt(d, v) }; }).ToList();
                bool ccw = Flaeche2D(punkte) > 0.0;
                if (ccw != gegenUhrzeiger) { punkte.Reverse(); idx.Reverse(); }
                foreach (double[] p in punkte) { stellen.Add(eben2.Count); eben2.Add(p); }
                ringe.Add(stellen);
            }
            Ring(aussen, true, index);
            var lochIndex = new List<List<int>>();
            foreach (List<double[]> l in loecher)
            {
                var li = l.Select(p => n.Punkt(a, p)).ToList();
                for (int i = 0; i < li.Count; i++) n.Kante(li[i], li[(i + 1) % li.Count]);
                Ring(l, false, li);
                lochIndex.Add(li);
            }
            var global = new List<int>(index);
            foreach (List<int> li in lochIndex) global.AddRange(li);
            List<int[]> dreiecke = Dreiecke2D(eben2, ringe, out List<int> verloren);
            if (verloren.Count > 0) n.Vermerk(Koerpervermerk.Loch);
            foreach (int[] t in dreiecke) n.Dreieck(a, global[t[0]], global[t[1]], global[t[2]], false);
        }

        /// <summary>Ohne Schlusspunkt und ohne doppelte Folgepunkte.</summary>
        private static List<double[]> Bereinigt(List<double[]> ring)
        {
            var r = new List<double[]>();
            foreach (double[] p in ring)
                if (p != null && (r.Count == 0 || !Gleich3(r[r.Count - 1], p))) r.Add(p);
            while (r.Count > 1 && Gleich3(r[0], r[r.Count - 1])) r.RemoveAt(r.Count - 1);
            return r;
        }

        private static bool Gleich3(double[] a, double[] b)
            => Math.Abs(a[0] - b[0]) <= 1e-9 && Math.Abs(a[1] - b[1]) <= 1e-9 && Math.Abs(a[2] - b[2]) <= 1e-9;

        private static double[] Newell(List<double[]> p)
        {
            var s = new double[3];
            for (int i = 0; i < p.Count; i++)
            {
                double[] a = p[i], b = p[(i + 1) % p.Count];
                s[0] += (a[1] - b[1]) * (a[2] + b[2]);
                s[1] += (a[2] - b[2]) * (a[0] + b[0]);
                s[2] += (a[0] - b[0]) * (a[1] + b[1]);
            }
            return s;
        }

        private static double Flaeche2D(List<double[]> p)
        {
            double f = 0.0;
            for (int i = 0; i < p.Count; i++)
            {
                double[] a = p[i], b = p[(i + 1) % p.Count];
                f += a[0] * b[1] - b[0] * a[1];
            }
            return f / 2.0;
        }

        /// <summary>Die Punkte eines Punktsatzes mit dem Index des optionalen <c>PnIndex</c> (1-basiert); <c>null</c> = ungültig.</summary>
        private static List<double[]> Punktliste(IIfcTessellatedFaceSet satz, IEnumerable pn, out List<int> abbildung)
        {
            abbildung = null;
            List<double[]> punkte = satz.Coordinates?.CoordList.Select(c => Koordinaten(c)).ToList();
            if (punkte == null) return null;
            List<int> folge = pn == null ? null : Indizes(pn);
            if (folge != null && folge.Count > 0) abbildung = folge.Select(x => x - 1).ToList();
            return punkte;
        }

        private static int Aufloesen(int index1, List<double[]> punkte, List<int> abbildung)
        {
            int i = index1 - 1;
            if (abbildung != null) i = i >= 0 && i < abbildung.Count ? abbildung[i] : -1;
            if (i < 0 || i >= punkte.Count) throw new IndexOutOfRangeException();
            return i;
        }

        /// <summary>
        /// <c>IfcTriangulatedFaceSet</c>: die Dreiecke direkt, Normalen aus den Dreiecken (nicht aus <c>Normals</c>);
        /// Randkanten sind die Kanten am Rand des Netzes und an Knicken über <see cref="KNICK_GRAD"/>.
        /// </summary>
        private static string Dreiecksnetz(IIfcTriangulatedFaceSet t, Abb a, Netz n)
        {
            List<double[]> punkte = Punktliste(t, t.PnIndex, out List<int> abb);
            if (punkte == null) return Typ(t);
            if (t.Closed == false) n.Vermerk(Koerpervermerk.Offen);
            var idx = new int[punkte.Count];
            for (int i = 0; i < idx.Length; i++) idx[i] = -1;
            int P(int i) => idx[i] >= 0 ? idx[i] : idx[i] = n.Punkt(a, punkte[i]);
            var dreiecke = new List<int[]>();
            foreach (IEnumerable d in t.CoordIndex)
            {
                List<int> i = Indizes(d).Select(x => Aufloesen(x, punkte, abb)).ToList();
                if (i.Count != 3) return Typ(t);
                dreiecke.Add(new[] { i[0], i[1], i[2] });
            }
            // Kanten: je ungerichtete Kante die Dreiecke daran — in der Reihenfolge der Dreiecke ausgegeben.
            var normalen = dreiecke.Select(d => IfcPlatzierung.Normiert(IfcPlatzierung.Kreuz(
                IfcPlatzierung.Minus(punkte[d[1]], punkte[d[0]]), IfcPlatzierung.Minus(punkte[d[2]], punkte[d[0]])))).ToList();
            var kanten = new Dictionary<(int, int), List<int>>();
            var folge = new List<(int, int)>();
            for (int k = 0; k < dreiecke.Count; k++)
                for (int e = 0; e < 3; e++)
                {
                    int p = dreiecke[k][e], q = dreiecke[k][(e + 1) % 3];
                    (int, int) s = p < q ? (p, q) : (q, p);
                    if (!kanten.TryGetValue(s, out List<int> an)) { an = new List<int>(2); kanten[s] = an; folge.Add(s); }
                    an.Add(k);
                }
            foreach (int[] d in dreiecke) n.Dreieck(a, P(d[0]), P(d[1]), P(d[2]), false);
            double knick = Math.Cos(KNICK_GRAD * Math.PI / 180.0);
            foreach ((int, int) s in folge)
            {
                List<int> an = kanten[s];
                bool rand = an.Count != 2;
                if (!rand)
                {
                    double[] n1 = normalen[an[0]], n2 = normalen[an[1]];
                    rand = n1 == null || n2 == null || IfcPlatzierung.Punktprodukt(n1, n2) < knick;
                }
                if (rand) n.Kante(P(s.Item1), P(s.Item2));
            }
            return null;
        }

        /// <summary>Der Knickwinkel [°], ab dem eine innere Kante eines Dreiecksnetzes als Randkante gilt.</summary>
        internal const double KNICK_GRAD = 1.0;

        /// <summary><c>IfcPolygonalFaceSet</c>: je <c>IfcIndexedPolygonalFace(WithVoids)</c> wie eine Fläche des BRep.</summary>
        private static string Vieleckssatz(IIfcPolygonalFaceSet s, Abb a, Netz n)
        {
            List<double[]> punkte = Punktliste(s, s.PnIndex, out List<int> abb);
            if (punkte == null) return Typ(s);
            if (s.Closed == false) n.Vermerk(Koerpervermerk.Offen);
            List<double[]> Auf(IEnumerable i) => Indizes(i).Select(x => punkte[Aufloesen(x, punkte, abb)]).ToList();
            foreach (IIfcIndexedPolygonalFace f in s.Faces)
            {
                var loecher = new List<List<double[]>>();
                if (f is IIfcIndexedPolygonalFaceWithVoids mit)
                    foreach (IEnumerable innen in mit.InnerCoordIndices) loecher.Add(Auf(innen));
                Flaeche(Auf(f.CoordIndex), loecher, a, n);
            }
            return null;
        }

        // ==================================================================
        //  Ohrenschnitt mit Brückenkanten
        // ==================================================================

        /// <summary>Der Ohrenschnitt mit Brückenkanten, formatfrei in <see cref="Polygonnetz.Dreiecke2D"/>.</summary>
        internal static List<int[]> Dreiecke2D(List<double[]> p, List<List<int>> ringe, out List<int> verloren)
            => Polygonnetz.Dreiecke2D(p, ringe, out verloren);

        /// <summary>Der Ohrenschnitt eines einfachen Vielecks, formatfrei in <see cref="Polygonnetz.Ohrenschnitt"/>.</summary>
        internal static List<int[]> Ohrenschnitt(List<double[]> p, List<int> vieleck) => Polygonnetz.Ohrenschnitt(p, vieleck);

        // ==================================================================
        //  Abbildung und Netz
        // ==================================================================

        /// <summary>
        /// Eine affine Abbildung p → T + M·p (Spalten von M: die Bilder der Achsen). <see cref="IfcRahmen"/> bleibt
        /// maßstabfrei; ein Maßstab aus <c>MappingTarget</c> steht allein hier (15.2).
        /// </summary>
        private readonly struct Abb
        {
            private Abb(double[] x, double[] y, double[] z, double[] t) { X = x; Y = y; Z = z; T = t; }

            public double[] X { get; }
            public double[] Y { get; }
            public double[] Z { get; }
            public double[] T { get; }

            /// <summary>Kehrt die Abbildung den Umlauf um (Determinante negativ)?</summary>
            public bool Spiegelt => IfcPlatzierung.Punktprodukt(IfcPlatzierung.Kreuz(X, Y), Z) < 0.0;

            public static Abb Aus(IfcRahmen r) => new Abb(r.X, r.Y, r.Z, r.Ursprung);

            /// <summary>
            /// Ein <c>IfcCartesianTransformationOperator</c> (2D, 3D, ungleichmäßig): Achsen wie <c>IfcBaseAxis</c>
            /// (z aus <c>Axis3</c>, x aus <c>Axis1</c> senkrecht dazu, y nach <c>Axis2</c> ausgerichtet), Maßstab je Achse.
            /// </summary>
            public static Abb Aus(IIfcCartesianTransformationOperator o)
            {
                if (o == null) return Aus(IfcRahmen.Welt);
                double s1 = o.Scale.HasValue ? IfcEigenschaften.Wert(o.Scale.Value) : 1.0;
                if (double.IsNaN(s1) || s1 == 0.0) s1 = 1.0;
                double s2 = s1, s3 = s1;
                if (o is IIfcCartesianTransformationOperator3DnonUniform u)
                {
                    if (u.Scale2.HasValue) s2 = IfcEigenschaften.Wert(u.Scale2.Value);
                    if (u.Scale3.HasValue) s3 = IfcEigenschaften.Wert(u.Scale3.Value);
                    if (double.IsNaN(s2) || s2 == 0.0) s2 = s1;
                    if (double.IsNaN(s3) || s3 == 0.0) s3 = s1;
                }
                if (o is IIfcCartesianTransformationOperator2DnonUniform u2 && u2.Scale2.HasValue)
                {
                    s2 = IfcEigenschaften.Wert(u2.Scale2.Value);
                    if (double.IsNaN(s2) || s2 == 0.0) s2 = s1;
                }
                double[] achse3 = o is IIfcCartesianTransformationOperator3D o3 ? IfcPlatzierung.Richtung(o3.Axis3) : null;
                IfcRahmen r = IfcPlatzierung.Achsen3D(IfcPlatzierung.Punkt(o.LocalOrigin), achse3, IfcPlatzierung.Richtung(o.Axis1));
                double[] y = r.Y;
                double[] achse2 = IfcPlatzierung.Richtung(o.Axis2);
                if (achse2 != null && IfcPlatzierung.Punktprodukt(achse2, y) < 0.0) y = IfcPlatzierung.Mal(y, -1.0);
                return new Abb(IfcPlatzierung.Mal(r.X, s1), IfcPlatzierung.Mal(y, s2), IfcPlatzierung.Mal(r.Z, s3), r.Ursprung);
            }

            /// <summary>Die Verkettung: erst <paramref name="innen"/>, dann diese Abbildung.</summary>
            public Abb Dann(Abb innen)
                => new Abb(Drehen(innen.X), Drehen(innen.Y), Drehen(innen.Z), Anwenden(innen.T));

            public double[] Drehen(double[] v)
                => new[]
                {
                    X[0] * v[0] + Y[0] * v[1] + Z[0] * v[2],
                    X[1] * v[0] + Y[1] * v[1] + Z[1] * v[2],
                    X[2] * v[0] + Y[2] * v[1] + Z[2] * v[2],
                };

            public double[] Anwenden(double[] p) => IfcPlatzierung.Plus(T, Drehen(p));
        }

        /// <summary>Das wachsende Netz eines Raums in Weltkoordinaten (Dateieinheit), mit Vermerken.</summary>
        private sealed class Netz
        {
            private readonly List<double[]> _p = new List<double[]>();
            private readonly List<int[]> _d = new List<int[]>();
            private readonly List<int[]> _k = new List<int[]>();
            private readonly bool[] _vermerke = new bool[Enum.GetValues(typeof(Koerpervermerk)).Length];
            private readonly List<bool[]> _vermerkStand = new List<bool[]>();

            public Netz(double laenge, double winkel)
            {
                Laenge = laenge > 0.0 && !double.IsNaN(laenge) ? laenge : 1.0;
                Winkel = winkel > 0.0 && !double.IsNaN(winkel) ? winkel : 1.0;
            }

            public double Laenge { get; }

            public double Winkel { get; }

            public int Punkt(Abb a, double[] lokal)
            {
                _p.Add(a.Anwenden(lokal));
                return _p.Count - 1;
            }

            public void Dreieck(Abb a, int i, int j, int k, bool kehren)
            {
                if (a.Spiegelt ^ kehren) _d.Add(new[] { i, k, j });
                else _d.Add(new[] { i, j, k });
            }

            public void Kante(int i, int j) => _k.Add(new[] { i, j });

            public void Vermerk(Koerpervermerk v) => _vermerke[(int)v] = true;

            /// <summary>Der Stand vor einem Träger (zum Zurücksetzen, wenn er nicht lesbar ist).</summary>
            public int[] Stand()
            {
                _vermerkStand.Add((bool[])_vermerke.Clone());
                return new[] { _p.Count, _d.Count, _k.Count, _vermerkStand.Count - 1 };
            }

            public void Zuruecksetzen(int[] stand)
            {
                _p.RemoveRange(stand[0], _p.Count - stand[0]);
                _d.RemoveRange(stand[1], _d.Count - stand[1]);
                _k.RemoveRange(stand[2], _k.Count - stand[2]);
                Array.Copy(_vermerkStand[stand[3]], _vermerke, _vermerke.Length);
            }

            /// <summary>In Meter, gerundet, gleiche Punkte zusammengelegt (Reihenfolge des ersten Auftretens).</summary>
            public Dateikoerper Abschluss(string art)
            {
                var punkte = new List<double[]>();
                var stelle = new Dictionary<(double, double, double), int>();
                var neu = new int[_p.Count];
                for (int i = 0; i < _p.Count; i++)
                {
                    double[] q = { R(_p[i][0] * Laenge), R(_p[i][1] * Laenge), R(_p[i][2] * Laenge) };
                    (double, double, double) s = (q[0], q[1], q[2]);
                    if (!stelle.TryGetValue(s, out int j))
                    {
                        j = punkte.Count;
                        stelle[s] = j;
                        punkte.Add(q);
                    }
                    neu[i] = j;
                }
                var dreiecke = new List<int[]>(_d.Count);
                var normalen = new List<double[]>(_d.Count);
                foreach (int[] d in _d)
                {
                    int[] t = { neu[d[0]], neu[d[1]], neu[d[2]] };
                    dreiecke.Add(t);
                    double[] nrm = IfcPlatzierung.Normiert(IfcPlatzierung.Kreuz(IfcPlatzierung.Minus(punkte[t[1]], punkte[t[0]]),
                                                                                IfcPlatzierung.Minus(punkte[t[2]], punkte[t[0]])))
                                   ?? new[] { 0.0, 0.0, 0.0 };
                    normalen.Add(new[] { R(nrm[0]), R(nrm[1]), R(nrm[2]) });
                }
                var kanten = new List<int[]>();
                var gesehen = new HashSet<(int, int)>();
                foreach (int[] k in _k)
                {
                    int a = neu[k[0]], b = neu[k[1]];
                    if (a == b) continue;
                    (int, int) s = a < b ? (a, b) : (b, a);
                    if (gesehen.Add(s)) kanten.Add(new[] { s.Item1, s.Item2 });
                }
                var vermerke = new List<Koerpervermerk>();
                for (int i = 0; i < _vermerke.Length; i++) if (_vermerke[i]) vermerke.Add((Koerpervermerk)i);
                return new Dateikoerper
                {
                    PunkteM = punkte,
                    Dreiecke = dreiecke,
                    Normalen = normalen,
                    Randkanten = kanten,
                    Art = art,
                    Vermerke = vermerke,
                };
            }

            private static double R(double v) => Math.Round(v, Zonenkoerper.STELLEN, MidpointRounding.ToEven) + 0.0;
        }
    }
}
