using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Grundrisse je Raum eines importierten Gebäudes</b> (HC-5, Konzept HottCAD-Verbund 11.2 und 11.3; E94) — was
    /// beim Import in <c>Tab_Raumgrundriss</c> geschrieben wird. Ohne Datenbank, wirft nicht.
    ///
    /// <para><b>Rangfolge je Raum:</b> Trägt der Raum einen Umriss aus Raumgrenzen bzw. aus den <c>PolyLoop</c>s der
    /// gbXML-Flächen (<see cref="Geometrieherkunft.Raumgrenzen"/>, Herleitung <c>Boden</c>/<c>Decke</c>), wird dieser
    /// gespeichert, nicht neu abgeleitet (F6, E73). Sonst leitet <see cref="Koerpergrundriss"/> ihn aus dem Dateikörper ab.
    /// Ohne beides bleibt der Raum ohne Zeile (Rechteck wie heute).</para>
    ///
    /// <para><b>Höhenlage und Höhe:</b> mit Körper dessen tiefster Punkt (Stufe 1: der Bodendreiecke) und seine Spanne (F9);
    /// ohne Körper aus der Ebene der Boden- bzw. Deckengrenzen und der Höhe des Umrisses (V/A, sonst der Datei).</para>
    ///
    /// <para><b>Nichts an der Rechnung ändert sich (11.5):</b> Die Grundrisse stehen in eigenen Feldern und speisen weder
    /// <c>AbbildRaum.GrundrissM</c> noch die Raumfläche noch die Trenndecken.</para>
    /// </summary>
    internal static class GebaeudeRaumgrundrisse
    {
        /// <summary>Die Grundrisse der Räume eines Gebäudes der Datei, in der Reihenfolge der Datei, je Kennung einer.</summary>
        internal static IReadOnlyList<Raumgrundriss> Bilden(GebaeudeAbbild abbild, int index)
        {
            var liste = new List<Raumgrundriss>();
            if (abbild == null || index < 0 || index >= abbild.Gebaeude.Count) return liste;
            AbbildGebaeude g = abbild.Gebaeude[index];
            Zonengeometrie geo;
            try
            {
                geo = GebaeudeGrundriss.Bilden(abbild, index);
            }
            catch (Exception)
            {
                return liste;                       // nur Anzeige und Export: nie den Import scheitern lassen
            }
            var umrisse = new Dictionary<string, Raumumriss>(StringComparer.Ordinal);
            foreach (Raumumriss u in geo.Raeume)
                if (u.RaumKennung != null && !umrisse.ContainsKey(u.RaumKennung)) umrisse[u.RaumKennung] = u;

            var gesehen = new HashSet<string>(StringComparer.Ordinal);
            foreach (AbbildRaum r in g.Raeume)
            {
                string kennung = Quellkennung.Kuerzen(r.Kennung ?? "");
                if (kennung.Length == 0 || !gesehen.Add(kennung)) continue;
                umrisse.TryGetValue(r.Kennung, out Raumumriss u);
                Raumgrundriss gr = Raum(g, r, u);
                if (gr != null) liste.Add(gr);
            }
            return liste;
        }

        /// <summary>Der Grundriss eines Raums (Regeln: Klassenkopf); <c>null</c> = keiner.</summary>
        internal static Raumgrundriss Raum(AbbildGebaeude g, AbbildRaum r, Raumumriss u)
        {
            if (r == null) return null;
            (string geschoss, double? lage) = Geschoss(g, r);
            string name = string.IsNullOrWhiteSpace(r.Name) ? null : r.Name;
            bool ohneBeschnitt = r.Koerper != null && r.Koerper.Vermerke.Contains(Koerpervermerk.OhneBeschnitt);

            // F6: Umriss aus Raumgrenzen bzw. gbXML-PolyLoops - gespeichert, nicht neu abgeleitet.
            if (u != null && u.Herkunft == Geometrieherkunft.Raumgrenzen && u.Polygone.Count > 0
                && (u.Herleitung == Umrissherleitung.Boden || u.Herleitung == Umrissherleitung.Decke))
            {
                List<Grundrissring> ringe = u.Polygone.Select(p => Ring(p.Punkte)).Where(x => x != null).ToList();
                double? boden, hoehe;
                if (r.Koerper != null && r.Koerper.PunkteM.Count > 0)
                {
                    boden = r.Koerper.PunkteM.Min(p => p[2]);
                    hoehe = r.Koerper.PunkteM.Max(p => p[2]) - boden;
                }
                else
                {
                    double? ebene = u.Polygone.Where(p => p.EbeneM.HasValue).Select(p => p.EbeneM).Min();
                    hoehe = u.HoeheM;
                    boden = ebene.HasValue && u.Herleitung == Umrissherleitung.Decke ? ebene - hoehe : ebene;
                }
                if (ringe.Count == 0 || !boden.HasValue || !hoehe.HasValue) return null;
                return Raumgrundriss.Bilden(r.Kennung, name, geschoss, lage, r.FlaecheM2, r.VolumenM3, u.Herleitung, ringe,
                                            boden.Value, hoehe.Value, Array.Empty<Grundrissvermerk>(), ohneBeschnitt);
            }

            // Weg 1: aus dem Dateikörper.
            if (r.Koerper == null) return null;
            Koerpergrundriss k = Koerpergrundriss.Ableiten(r.Koerper);
            if (!k.Traegt) return null;
            return Raumgrundriss.Bilden(r.Kennung, name, geschoss, lage, r.FlaecheM2, r.VolumenM3, k.Herleitung.Value, k.Ringe,
                                        k.BodenM, k.HoeheM, k.Vermerke, ohneBeschnitt);
        }

        /// <summary>Name und Lage des Geschosses: am Raum (gbXML), sonst aus der Geschossliste des Gebäudes (IFC).</summary>
        private static (string Name, double? LageM) Geschoss(AbbildGebaeude g, AbbildRaum r)
        {
            string name = r.GeschossName;
            double? lage = r.GeschossLageM;
            if (r.GeschossKennung != null && (name == null || !lage.HasValue))
            {
                AbbildGeschoss s = g.Geschosse.FirstOrDefault(x => string.Equals(x.Kennung, r.GeschossKennung, StringComparison.Ordinal));
                if (s != null)
                {
                    name ??= s.Name;
                    lage ??= s.LageM;
                }
            }
            return (string.IsNullOrWhiteSpace(name) ? null : name, lage);
        }

        /// <summary>Ein Polygon [m] als Ring in ganzen Millimetern, gegen den Uhrzeigersinn, am kleinsten Punkt beginnend.</summary>
        private static Grundrissring Ring(IReadOnlyList<double[]> punkteM)
        {
            if (punkteM == null) return null;
            var p = new List<long[]>();
            foreach (double[] x in punkteM)
            {
                long[] q = { (long)Math.Round(x[0] * 1000.0, MidpointRounding.AwayFromZero), (long)Math.Round(x[1] * 1000.0, MidpointRounding.AwayFromZero) };
                if (p.Count > 0 && p[^1][0] == q[0] && p[^1][1] == q[1]) continue;
                p.Add(q);
            }
            if (p.Count > 1 && p[0][0] == p[^1][0] && p[0][1] == p[^1][1]) p.RemoveAt(p.Count - 1);
            if (p.Count < 3) return null;
            if (Koerpergrundriss.DoppelteFlaeche(p) < 0) p.Reverse();
            if (Koerpergrundriss.DoppelteFlaeche(p) == 0) return null;
            int start = 0;
            for (int i = 1; i < p.Count; i++)
                if (p[i][0] < p[start][0] || (p[i][0] == p[start][0] && p[i][1] < p[start][1])) start = i;
            return new Grundrissring(p.Skip(start).Concat(p.Take(start)).ToList());
        }
    }
}
