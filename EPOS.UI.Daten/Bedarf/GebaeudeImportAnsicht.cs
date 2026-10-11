using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Bedarf;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Grundriss eines Gebäudeimports als Daten der Ansicht</b> (Stufe G6c, Welle D; Entscheid E11,
    /// Mehrzonenkonzept 6.7) — plattformfrei neben der <see cref="GebaeudeImportHuelle"/>, ohne Datenbank.
    /// Übersetzt die <see cref="Zonengeometrie"/> des Kerns in das DTO der Komponente
    /// <c>GebaeudeAnsicht</c>: je Geschoss die Räume mit ihren Polygonen in Metern, der Zone als Schlüssel
    /// und der Herkunft als Wert; je Zone Schlüssel, Name (derselbe wie in der Zonenliste), Stelle und
    /// Beheizung; die Hinweise der Geometrie als Anzeigetexte; je Raum den Körper aus der Datei (Punkte relativ
    /// zum Bezugspunkt des Gebäudes) und die Herkunft des Körpers als Wert. Gerechnet wird hier nichts — die Geometrie
    /// ist die des Kerns, nur ihre Gestalt wechselt.
    /// </summary>
    internal static class GebaeudeImportAnsicht
    {
        /// <summary>
        /// Die Daten der Ansicht; <c>null</c> ohne Geometrie.
        /// </summary>
        /// <param name="geometrie">Die Zonengeometrie der Anfrage.</param>
        /// <param name="umhaengbar">Lässt die Regel Zuordnungen von Hand zu (mehrere Zonen)?</param>
        /// <param name="zonenname">Der Name einer Zone nach ihrer Stelle, wie ihn die Zonenliste zeigt; <c>null</c> = der der Geometrie.</param>
        /// <param name="gebaeude">Das Gebäude des Abbilds mit seiner Flächenklassifikation (HottCAD-Verbund 4.2); <c>null</c> oder
        /// ohne Klassifikation = keine Gruppen nach Randbedingung.</param>
        internal static GebaeudeAnsichtDaten AnsichtDaten(Zonengeometrie geometrie, bool umhaengbar, Func<int, string> zonenname = null,
                                                          AbbildGebaeude gebaeude = null)
        {
            if (geometrie == null) return null;
            var geschosse = new List<GebaeudeAnsichtGeschoss>(geometrie.Geschosse.Count);
            foreach (Geschossangabe g in geometrie.Geschosse)
            {
                List<GebaeudeAnsichtRaum> raeume = geometrie.RaeumeIm(g.Stelle).Select(r => Raum(geometrie, r)).ToList();
                geschosse.Add(new GebaeudeAnsichtGeschoss(
                    g.Kennung ?? "", string.IsNullOrWhiteSpace(g.Name) ? g.Kennung ?? "" : g.Name.Trim(), g.Schematisch,
                    g.MinX, g.MinY, g.MaxX, g.MaxY, raeume));
            }
            List<GebaeudeAnsichtZone> zonen = geometrie.Zonen
                .Select(z => new GebaeudeAnsichtZone(z.Schluessel, zonenname?.Invoke(z.Stelle) ?? z.Name, z.Stelle, z.IstBeheizt,
                                                     z.Herkunft == Geometrieherkunft.Schematisch, z.Handgeaendert)
                             { AusDateikoerper = z.Herkunft == Geometrieherkunft.Dateikoerper })
                .ToList();
            double[] bezug = Bezugspunkt(geometrie);
            var daten = new GebaeudeAnsichtDaten
            {
                Geschosse = geschosse,
                Zonen = zonen,
                Schematisch = geometrie.Schematisch,
                Umhaengbar = umhaengbar,
                Hinweise = geometrie.Meldungen.Select(GebaeudeZuordnungsModell.MeldungText).ToList(),
                Koerperraeume = geometrie.Raeume.Select(r => Koerperraum(geometrie, r, bezug)).ToList(),
                Bezugspunkt = bezug,
                Dreiecksgrenze = Dateikoerper.DREIECKSGRENZE,
                Geschosslagen = geometrie.Geschosse.Select(g => new GebaeudeAnsichtGeschosslage(g.Kennung ?? "", g.LageM)).ToList(),
            };
            if (gebaeude == null) return daten;
            return Randgruppen(daten, geometrie, gebaeude, bezug) with { Koerperwege = Koerperwege(gebaeude) };
        }

        // ------------------------------------------------------------------
        //  Gruppen nach Randbedingung (HottCAD-Verbund 4.3, HC-2)
        // ------------------------------------------------------------------

        /// <summary>Der größte Abstand eines Dreiecks einer senkrechten Fläche von der Geraden einer Kante [m].</summary>
        internal const double KANTE_ABSTAND_M = 0.15;

        /// <summary>Der größte Abstand des Schwerpunkts einer Öffnung von der Geraden einer Kante [m] (Wanddicke).</summary>
        internal const double OEFFNUNG_ABSTAND_M = 0.6;

        /// <summary>Die Toleranz der Richtung einer Fläche gegen die Normale der Kante [°] (wie die Klassifikation).</summary>
        internal const double KANTE_WINKEL_GRAD = Flaechenklassifikation.ORIENTIERUNG_TOLERANZ_GRAD;

        /// <summary>
        /// Trägt die Flächenklassifikation in die Daten der Ansicht: je Raumkörper das Gruppenbyte je Dreieck, die Gruppenliste
        /// (Bilanz der Klassifikation, Dreiecke der Raumkörper), die Bauteilkörper mit Gruppe und Kennung und für den Grundriss je
        /// Kante die Gruppe ihrer senkrechten Flächen, die Öffnungen als Marken und die Gruppe des Bodens. Übersteigen Raum- und
        /// Bauteilkörper zusammen die Dreiecksgrenze, bleiben die Gruppen weg und die Daten sagen es (4.4).
        /// </summary>
        private static GebaeudeAnsichtDaten Randgruppen(GebaeudeAnsichtDaten daten, Zonengeometrie geometrie, AbbildGebaeude g, double[] bezug)
        {
            if (g.Flaechengruppen == null || g.FlaechengruppenBilanzM2 == null || !daten.HatDateikoerper) return daten;
            var jeKennung = new Dictionary<string, AbbildBauteil>(StringComparer.Ordinal);
            foreach (AbbildBauteil b in g.Bauteile)
            {
                if (b.Kennung != null) jeKennung.TryAdd(b.Kennung, b);
                foreach (AbbildBauteil o in b.Oeffnungen)
                    if (o.Kennung != null) jeKennung.TryAdd(o.Kennung, o);
            }
            var bauteile = new List<(Flaechengruppenzeile Zeile, AbbildBauteil Bauteil)>();
            foreach (Flaechengruppenzeile z in g.Flaechengruppen)
                if (z.Raumkennung == null && z.Bauteilkennung != null && jeKennung.TryGetValue(z.Bauteilkennung, out AbbildBauteil b) && b.Koerper != null)
                    bauteile.Add((z, b));
            long dreiecke = daten.DateikoerperDreiecke + bauteile.Sum(x => (long)x.Bauteil.Koerper.DreieckZahl);
            if (dreiecke > Dateikoerper.DREIECKSGRENZE) return daten with { RandgruppenZuGross = true };

            var zaehler = new int[GebaeudeAnsichtRandgruppen.ZAHL];
            var oeffnungen = bauteile.Where(x => x.Zeile.Gruppe == Flaechengruppe.R7).Select(x => x.Bauteil).ToList();
            var raeume = new List<GebaeudeAnsichtKoerperraum>(daten.Koerperraeume.Count);
            foreach (GebaeudeAnsichtKoerperraum k in daten.Koerperraeume)
            {
                Raumumriss r = geometrie.Raeume.FirstOrDefault(x => string.Equals(x.RaumKennung, k.Kennung, StringComparison.Ordinal));
                if (k.Dateikoerper == null || r?.Koerper == null) { raeume.Add(k); continue; }
                bool schematisch = r.Herkunft == Geometrieherkunft.Schematisch;
                byte[] gruppen = Enumerable.Repeat(GebaeudeAnsichtRandgruppen.KEINE, r.Koerper.DreieckZahl).ToArray();
                var jeDreieck = new string[r.Koerper.DreieckZahl];
                foreach (Flaechengruppenzeile z in g.Flaechengruppen)
                    if (string.Equals(z.Raumkennung, k.Kennung, StringComparison.Ordinal))
                        foreach (int t in z.Dreiecksindizes)
                            if (t >= 0 && t < gruppen.Length)
                            {
                                gruppen[t] = (byte)z.Gruppe;
                                jeDreieck[t] = z.Bauteilkennung;
                            }
                // BA-3: das Bauteil je Kante — dieselbe Gewichtung wie die Gruppe, nach Bauteil statt nach Gruppe.
                var bauteilliste = jeDreieck.Where(x => x != null).Distinct(StringComparer.Ordinal).ToList();
                var bauteilIndex = new Dictionary<string, int>(StringComparer.Ordinal);
                for (int i = 0; i < bauteilliste.Count; i++) bauteilIndex[bauteilliste[i]] = i;
                int[] schluessel = jeDreieck.Select((x, t) => x != null && gruppen[t] < GebaeudeAnsichtRandgruppen.ZAHL ? bauteilIndex[x] : -1).ToArray();
                foreach (byte x in gruppen)
                    if (x < GebaeudeAnsichtRandgruppen.ZAHL) zaehler[x]++;
                raeume.Add(k with
                {
                    Dateikoerper = k.Dateikoerper with { Gruppen = gruppen },
                    Kantengruppen = r.Polygone.Select(p => (IReadOnlyList<Randgruppe?>)Enumerable.Range(0, p.Punkte.Count)
                                                        .Select(e => Kantengruppe(r.Koerper, gruppen, p.Punkte, e, schematisch)).ToList()).ToList(),
                    Dreiecksbauteile = jeDreieck,
                    Kantenbauteile = r.Polygone.Select(p => (IReadOnlyList<string>)Enumerable.Range(0, p.Punkte.Count)
                                                        .Select(e => Groesster(Kantengewichte(r.Koerper, schluessel, bauteilliste.Count, p.Punkte, e, schematisch)) is int b
                                                                     ? bauteilliste[b] : null).ToList()).ToList(),
                    Kantenmarken = schematisch ? Array.Empty<GebaeudeAnsichtKantenmarke>() : Kantenmarken(r, oeffnungen),
                    Bodengruppe = Bodengruppe(r.Koerper, gruppen),
                });
            }
            return daten with
            {
                Koerperraeume = raeume,
                Flaechengruppen = Enumerable.Range(0, GebaeudeAnsichtRandgruppen.ZAHL)
                    .Select(x => new GebaeudeAnsichtFlaechengruppe((Randgruppe)x,
                        g.FlaechengruppenBilanzM2.TryGetValue((Flaechengruppe)x, out double m2) ? m2 : 0.0, zaehler[x]))
                    .ToList(),
                Bauteilkoerper = bauteile.Select(x => new GebaeudeAnsichtBauteilkoerper(x.Zeile.Bauteilkennung, (Randgruppe)x.Zeile.Gruppe,
                                                                                         Koerper(x.Bauteil.Koerper, bezug))).ToList(),
            };
        }

        /// <summary>
        /// Die Gruppe der Kante <paramref name="e"/> eines Polygons: die senkrechten Dreiecke des Körpers, deren Normale zur Normale
        /// der Kante steht (Toleranz <see cref="KANTE_WINKEL_GRAD"/>), die nicht weiter als <see cref="KANTE_ABSTAND_M"/> von ihrer
        /// Geraden liegen und sie entlang überdecken, je Gruppe nach der überdeckten Fläche gewogen; die flächengrößte gewinnt (bei
        /// Gleichstand die kleinere Nummer). Keine Fläche an der Kante = <c>null</c>, nichts Erfundenes.
        /// <para>Ein <b>schematischer</b> Umriss (Fläche und Seitenverhältnis, die Lage erfunden — etwa HottCAD ohne Raumgrenzen)
        /// hat keine Lage, nur Richtungen: Dort zählen alle senkrechten Dreiecke, deren äußere Normale in die äußere Normale der
        /// Kante zeigt (gegen den Uhrzeigersinn: rechts der Laufrichtung), ohne Abstand und Überdeckung.</para>
        /// </summary>
        private static Randgruppe? Kantengruppe(Dateikoerper k, byte[] gruppen, IReadOnlyList<double[]> punkte, int e, bool schematisch)
        {
            int[] schluessel = gruppen.Select(x => x < GebaeudeAnsichtRandgruppen.ZAHL ? (int)x : -1).ToArray();
            return Groesster(Kantengewichte(k, schluessel, GebaeudeAnsichtRandgruppen.ZAHL, punkte, e, schematisch)) is int g ? (Randgruppe)g : null;
        }

        /// <summary>Der Schlüssel mit dem größten Gewicht; ohne Gewicht <c>null</c>, bei Gleichstand der kleinere.</summary>
        private static int? Groesster(double[] gewicht)
        {
            int beste = -1;
            for (int i = 0; i < gewicht.Length; i++)
                if (gewicht[i] > 1e-9 && (beste < 0 || gewicht[i] > gewicht[beste] + 1e-9)) beste = i;
            return beste < 0 ? null : beste;
        }

        /// <summary>
        /// Die Gewichte der Kante <paramref name="e"/> je Schlüssel (0 … <paramref name="zahl"/> − 1; −1 = ohne) nach der Regel von
        /// <see cref="Kantengruppe"/> — die Gruppe (HC-2) bzw. das Bauteil (BA-3) teilen dieselbe Geometrie.
        /// </summary>
        private static double[] Kantengewichte(Dateikoerper k, int[] schluessel, int zahl, IReadOnlyList<double[]> punkte, int e, bool schematisch)
        {
            var gewicht = new double[zahl];
            double[] a = punkte[e], b = punkte[(e + 1) % punkte.Count];
            double dx = b[0] - a[0], dy = b[1] - a[1], laenge = Math.Sqrt(dx * dx + dy * dy);
            if (laenge < 1e-9) return gewicht;
            double ux = dx / laenge, uy = dy / laenge, nx = uy, ny = -ux;
            double sinWand = Math.Sin(Koerpernachbarschaft.WAND_NEIGUNG_GRAD * Math.PI / 180.0);
            double cosKante = Math.Cos(KANTE_WINKEL_GRAD * Math.PI / 180.0);
            for (int t = 0; t < k.Dreiecke.Count; t++)
            {
                if (t >= schluessel.Length || schluessel[t] < 0 || schluessel[t] >= zahl) continue;
                if (!Normale(k, t, out double[] n, out double flaeche) || Math.Abs(n[2]) > sinWand) continue;
                double h = Math.Sqrt(n[0] * n[0] + n[1] * n[1]);
                double richtung = (n[0] * nx + n[1] * ny) / h;
                if (schematisch)
                {
                    if (richtung >= cosKante) gewicht[schluessel[t]] += flaeche;
                    continue;
                }
                if (Math.Abs(richtung) < cosKante) continue;
                double tMin = double.PositiveInfinity, tMax = double.NegativeInfinity;
                bool nah = true;
                foreach (int i in k.Dreiecke[t])
                {
                    double px = k.PunkteM[i][0] - a[0], py = k.PunkteM[i][1] - a[1];
                    if (Math.Abs(px * nx + py * ny) > KANTE_ABSTAND_M) { nah = false; break; }
                    double s = px * ux + py * uy;
                    tMin = Math.Min(tMin, s);
                    tMax = Math.Max(tMax, s);
                }
                if (!nah) continue;
                double ueber = Math.Min(tMax, laenge) - Math.Max(tMin, 0.0);
                if (ueber <= 1e-9) continue;
                double spanne = tMax - tMin;
                gewicht[schluessel[t]] += flaeche * (spanne < 1e-9 ? 1.0 : ueber / spanne);
            }
            return gewicht;
        }

        /// <summary>Die Gruppe des Bodens: die flächengrößte unter den Dreiecken mit Normale nach unten.</summary>
        private static Randgruppe? Bodengruppe(Dateikoerper k, byte[] gruppen)
        {
            double sinWand = Math.Sin(Koerpernachbarschaft.WAND_NEIGUNG_GRAD * Math.PI / 180.0);
            var gewicht = new double[GebaeudeAnsichtRandgruppen.ZAHL];
            for (int t = 0; t < k.Dreiecke.Count; t++)
                if (gruppen[t] < GebaeudeAnsichtRandgruppen.ZAHL && Normale(k, t, out double[] n, out double flaeche) && n[2] < -sinWand)
                    gewicht[gruppen[t]] += flaeche;
            return Groesste(gewicht);
        }

        /// <summary>
        /// Die Öffnungen (R7) als Marken auf den Kanten des Raums: je Öffnung die nächste Kante, deren Gerade ihr Schwerpunkt nicht
        /// weiter als <see cref="OEFFNUNG_ABSTAND_M"/> fernliegt, die sie längs ausrichtet (Ausdehnung längs größer als quer) und
        /// die sie überdeckt; der Abschnitt als Anteil der Kante.
        /// </summary>
        private static IReadOnlyList<GebaeudeAnsichtKantenmarke> Kantenmarken(Raumumriss r, List<AbbildBauteil> oeffnungen)
        {
            var marken = new List<GebaeudeAnsichtKantenmarke>();
            foreach (AbbildBauteil o in oeffnungen)
            {
                IReadOnlyList<double[]> q = o.Koerper.PunkteM;
                if (q.Count == 0) continue;
                double sx = q.Average(p => p[0]), sy = q.Average(p => p[1]);
                GebaeudeAnsichtKantenmarke beste = null;
                double besterAbstand = double.PositiveInfinity;
                for (int pi = 0; pi < r.Polygone.Count; pi++)
                {
                    IReadOnlyList<double[]> punkte = r.Polygone[pi].Punkte;
                    for (int e = 0; e < punkte.Count; e++)
                    {
                        double[] a = punkte[e], b = punkte[(e + 1) % punkte.Count];
                        double dx = b[0] - a[0], dy = b[1] - a[1], laenge = Math.Sqrt(dx * dx + dy * dy);
                        if (laenge < 1e-9) continue;
                        double ux = dx / laenge, uy = dy / laenge;
                        double abstand = Math.Abs((sx - a[0]) * uy - (sy - a[1]) * ux);
                        if (abstand > OEFFNUNG_ABSTAND_M || abstand >= besterAbstand) continue;
                        double tMin = double.PositiveInfinity, tMax = double.NegativeInfinity, sMin = double.PositiveInfinity, sMax = double.NegativeInfinity;
                        foreach (double[] p in q)
                        {
                            double t = (p[0] - a[0]) * ux + (p[1] - a[1]) * uy, s = (p[0] - a[0]) * uy - (p[1] - a[1]) * ux;
                            tMin = Math.Min(tMin, t); tMax = Math.Max(tMax, t); sMin = Math.Min(sMin, s); sMax = Math.Max(sMax, s);
                        }
                        double von = Math.Max(tMin, 0.0), bis = Math.Min(tMax, laenge);
                        if (bis - von <= 1e-9 || tMax - tMin <= sMax - sMin) continue;
                        besterAbstand = abstand;
                        beste = new GebaeudeAnsichtKantenmarke(pi, e, Math.Round(von / laenge, 6), Math.Round(bis / laenge, 6), o.Kennung);
                    }
                }
                if (beste != null) marken.Add(beste);
            }
            return marken;
        }

        /// <summary>Die Einheitsnormale und Fläche eines Dreiecks; <c>false</c> bei einem entarteten.</summary>
        private static bool Normale(Dateikoerper k, int t, out double[] n, out double flaeche)
        {
            int[] d = k.Dreiecke[t];
            double[] a = k.PunkteM[d[0]], b = k.PunkteM[d[1]], c = k.PunkteM[d[2]];
            double ux = b[0] - a[0], uy = b[1] - a[1], uz = b[2] - a[2], vx = c[0] - a[0], vy = c[1] - a[1], vz = c[2] - a[2];
            n = new[] { uy * vz - uz * vy, uz * vx - ux * vz, ux * vy - uy * vx };
            double l = Math.Sqrt(n[0] * n[0] + n[1] * n[1] + n[2] * n[2]);
            flaeche = l / 2.0;
            if (l < 1e-12) return false;
            for (int i = 0; i < 3; i++) n[i] /= l;
            return true;
        }

        /// <summary>Die Gruppe mit dem größten Gewicht; ohne Gewicht <c>null</c>, bei Gleichstand die kleinere Nummer.</summary>
        private static Randgruppe? Groesste(double[] gewicht)
        {
            int beste = -1;
            for (int i = 0; i < gewicht.Length; i++)
                if (gewicht[i] > 1e-9 && (beste < 0 || gewicht[i] > gewicht[beste] + 1e-9)) beste = i;
            return beste < 0 ? null : (Randgruppe)beste;
        }

        /// <summary>
        /// Die Körperangaben eines Raums (G7b): die Raumhöhe — die des Raums, sonst die der Zone (V/A), sonst keine —
        /// und je Kante, Boden und Decke die Art des ersten Bauteils, das die Geometrie dort kennt.
        /// </summary>
        private static GebaeudeAnsichtKoerperraum Koerperraum(Zonengeometrie geometrie, Raumumriss r, double[] bezug)
        {
            double? hoehe = r.HoeheM ?? (r.Zone >= 0 && r.Zone < geometrie.Zonen.Count ? geometrie.Zonen[r.Zone].HoeheM : null);
            List<IReadOnlyList<string>> kanten = r.Polygone
                .Select(p => (IReadOnlyList<string>)p.Kanten.Select(k => Art(k.Grenzen)).ToList())
                .ToList();
            return new GebaeudeAnsichtKoerperraum(r.RaumKennung, hoehe, kanten, Art(r.Boden), Art(r.Decke))
            {
                Dateikoerper = r.Koerper == null ? null : Koerper(r.Koerper, bezug),
                Herkunft = r.Koerper != null ? (r.Koerper.IstBeleg ? Koerperherkunft.Datei : Koerperherkunft.Abgeleitet)
                    : r.AusGrundriss ? Koerperherkunft.Grundriss
                    : r.Herkunft == Geometrieherkunft.Schematisch ? Koerperherkunft.Schematisch : Koerperherkunft.Umriss,
                // HC-5: das Prisma je Polygon aus dem Grundriss (Boden und Höhe des Grundrisses, nicht V/A).
                Prismen = r.AusGrundriss
                    ? r.Polygone.Select(p => new GebaeudeAnsichtPrismenlage(p.PrismaBodenM.Value, p.PrismaHoeheM.Value)).ToList()
                    : null,
                Grundrissvermerke = r.Grundrissvermerke.Select(v => v.ToString()).ToList(),
            };
        }

        /// <summary>
        /// Der Weg des Körpers je Bauteil für die Zeile „Körper" des Steckbriefs (17.5): der eigene Körper des Bauteils bzw. der
        /// Öffnung, sonst der erste Raumkörper, dessen Dreiecke das Bauteil als Quellfläche tragen.
        /// </summary>
        internal static IReadOnlyDictionary<string, GebaeudeAnsichtKoerperweg> Koerperwege(AbbildGebaeude g)
        {
            var wege = new Dictionary<string, GebaeudeAnsichtKoerperweg>(StringComparer.Ordinal);
            static GebaeudeAnsichtKoerperweg Weg(Dateikoerper k)
                => new GebaeudeAnsichtKoerperweg(!k.IstBeleg, k.Art ?? "", k.Vermerke.Select(v => v.ToString()).ToList());
            foreach (AbbildBauteil b in g.Bauteile)
                foreach (AbbildBauteil x in new[] { b }.Concat(b.Oeffnungen))
                    if (x.Kennung != null && x.Koerper != null) wege.TryAdd(x.Kennung, Weg(x.Koerper));
            foreach (AbbildRaum r in g.Raeume)
                if (r.Koerper != null)
                    foreach (string q in r.Koerper.Quellflaechen.Distinct(StringComparer.Ordinal))
                        if (q != null) wege.TryAdd(q, Weg(r.Koerper));
            return wege;
        }

        /// <summary>
        /// Der Bezugspunkt der Dateikörper (15.4): je Achse der kleinste Punkt aller Körper; ohne Körper (0, 0, 0).
        /// </summary>
        private static double[] Bezugspunkt(Zonengeometrie geometrie)
        {
            var bezug = new[] { double.PositiveInfinity, double.PositiveInfinity, double.PositiveInfinity };
            foreach (Raumumriss r in geometrie.Raeume)
            {
                if (r.Koerper == null) continue;
                foreach (double[] p in r.Koerper.PunkteM)
                    for (int i = 0; i < 3; i++) bezug[i] = Math.Min(bezug[i], p[i]);
            }
            return double.IsPositiveInfinity(bezug[0]) ? new double[3] : bezug;
        }

        /// <summary>Der Dateikörper als Folgen für die Ansicht: Punkte relativ zum Bezugspunkt (float), Indizes flach.</summary>
        private static GebaeudeAnsichtDateikoerper Koerper(Dateikoerper k, double[] bezug)
        {
            var punkte = new float[k.PunkteM.Count * 3];
            for (int i = 0; i < k.PunkteM.Count; i++)
                for (int a = 0; a < 3; a++) punkte[3 * i + a] = (float)(k.PunkteM[i][a] - bezug[a]);
            var dreiecke = new int[k.Dreiecke.Count * 3];
            for (int i = 0; i < k.Dreiecke.Count; i++)
                for (int a = 0; a < 3; a++) dreiecke[3 * i + a] = k.Dreiecke[i][a];
            var kanten = new int[k.Randkanten.Count * 2];
            for (int i = 0; i < k.Randkanten.Count; i++)
                for (int a = 0; a < 2; a++) kanten[2 * i + a] = k.Randkanten[i][a];
            return new GebaeudeAnsichtDateikoerper(punkte, dreiecke, kanten, k.DreieckZahl, k.Art,
                                                   k.Vermerke.Select(v => v.ToString()).ToList());
        }

        /// <summary>Die Bauteilart der ersten Grenze als sprachneutraler Schlüssel; <c>null</c> ohne Grenze.</summary>
        private static string Art(IReadOnlyList<Grenzverweis> grenzen)
            => grenzen != null && grenzen.Count > 0 ? grenzen[0].Bauteilart.ToString() : null;

        private static GebaeudeAnsichtRaum Raum(Zonengeometrie geometrie, Raumumriss r)
        {
            string zone = r.Zone >= 0 && r.Zone < geometrie.Zonen.Count ? geometrie.Zonen[r.Zone].Schluessel : null;
            double? flaeche = r.FlaecheM2 ?? (r.Polygone.Count > 0 ? r.PolygonflaecheM2 : (double?)null);
            List<IReadOnlyList<GebaeudeAnsichtPunkt>> polygone = r.Polygone
                .Select(p => (IReadOnlyList<GebaeudeAnsichtPunkt>)p.Punkte.Select(q => new GebaeudeAnsichtPunkt(q[0], q[1])).ToList())
                .ToList();
            return new GebaeudeAnsichtRaum(
                r.RaumKennung, r.Name, zone, r.Beheizt, r.Herkunft == Geometrieherkunft.Schematisch,
                flaeche.HasValue ? GebaeudeZuordnungsModell.ZahlText(Math.Round(flaeche.Value, 2)) + " m²" : MyResource.Resource.GIMP_WERT_LEER,
                polygone)
            {
                AusDateikoerper = r.Herkunft == Geometrieherkunft.Dateikoerper,
            };
        }
    }
}
