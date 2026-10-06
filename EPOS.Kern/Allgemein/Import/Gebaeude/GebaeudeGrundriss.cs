using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Grundriss eines importierten Gebäudes</b> (Stufe G6c, Welle D; Entscheid E11,
    /// Mehrzonenkonzept 6.7): bildet aus dem <see cref="GebaeudeAbbild"/> und der
    /// <see cref="GebaeudeZonierung"/> den formatfreien <see cref="Umrisseingang"/> und daraus die
    /// <see cref="Zonengeometrie"/>. Ohne Datenbank, ohne Oberfläche; schreibt nichts.
    ///
    /// <list type="bullet">
    /// <item><b>Räume</b> in der Reihenfolge der Datei, jeder mit seiner Zone (Stelle der Zonierung; ohne
    /// Zonierung oder außerhalb der Zonen −1 — unter Z5/X4 die unbeheizten Räume) und seinem wirksamen
    /// Beheizungszustand samt den Haken der Raumliste.</item>
    /// <item><b>Seiten:</b> je Bauteil die Raumgrenzen seiner Räume (IFC), ohne Raumgrenzen die Nachbarn
    /// (gbXML) — Fenster und Türen stehen an ihrem Wirt und zählen nicht. Der Ring ist der der Raumgrenze
    /// bzw. der <c>PolyLoop</c> der Fläche.</item>
    /// <item><b>Stellung</b> aus Sicht des Raums: die Normale der Raumgrenze (vom Raum weg; |n_z| höchstens
    /// <see cref="SENKRECHT_NZ"/> heißt Wand, sonst nach unten Boden, nach oben Decke), sonst die Art (Wand,
    /// Bodenplatte, Dach), sonst die Neigung (60°–120° Wand), sonst Boden oder Decke nach der Sicht der Datei
    /// samt Neigung und Flächenart (<see cref="GebaeudeHuelleneinordnung.Boden"/>), sonst nach der Höhenlage
    /// der Geschosse beider Räume; bleibt es offen, ist die Stellung unbestimmt (benannt am Raum).</item>
    /// <item><b>Himmelsrichtung</b> einer Wand: aus der Normalen der Raumgrenze, gedreht wie die Bauteile,
    /// sonst der Azimut des Bauteils — aus Sicht des zweiten Nachbarn gespiegelt;
    /// <see cref="GebaeudeAggregation.Sektor"/>.</item>
    /// <item><b>Fläche</b> einer Seite: das Polygon der Raumgrenze, sonst die Bruttofläche des Bauteils —
    /// außen nach der Zahl seiner Räume geteilt, innen bei mehr als zwei Räumen zu je 2/n (wie die
    /// Zonierung, Festlegung 4).</item>
    /// </list>
    /// </summary>
    internal static class GebaeudeGrundriss
    {
        /// <summary>Größter Betrag der senkrechten Normalenkomponente einer Wand (cos 60°).</summary>
        internal const double SENKRECHT_NZ = 0.5;

        /// <summary>Die Zonengeometrie eines Gebäudes der Datei; ohne Zonierung stehen alle Räume ohne Zone.</summary>
        /// <param name="abbild">Das gelesene Abbild.</param>
        /// <param name="index">Das Gebäude (eines je Lauf).</param>
        /// <param name="zonierung">Die Zonierung desselben Gebäudes; <c>null</c> = keine.</param>
        /// <param name="grundrisse">Die Grundrisse je Raum (HC-5) nach ihrer Quellkennung; <c>null</c> = die am Raum des Abbilds
        /// (<see cref="AbbildRaum.Grundrisse"/>, Export).</param>
        internal static Zonengeometrie Bilden(GebaeudeAbbild abbild, int index, GebaeudeZonierung zonierung = null,
                                              IReadOnlyList<Raumgrundriss> grundrisse = null)
            => Zonengeometrie.AusRaumgrenzen(Eingang(abbild, index, zonierung, grundrisse));

        /// <summary>
        /// <b>Die Zonengeometrie mit dem frisch abgeleiteten Grundriss je Raum</b> (HC-5; Import und „Datei erneut lesen“): dieselben
        /// Grundrisse, die der Import speichert (<see cref="GebaeudeRaumgrundrisse.Bilden"/>), stehen vor dem Rechteckersatz.
        /// </summary>
        internal static Zonengeometrie BildenMitGrundriss(GebaeudeAbbild abbild, int index, GebaeudeZonierung zonierung,
                                                          out IReadOnlyList<Raumgrundriss> grundrisse)
        {
            grundrisse = GebaeudeRaumgrundrisse.Bilden(abbild, index);
            return Bilden(abbild, index, zonierung, grundrisse);
        }

        /// <summary>Der formatfreie Eingang der Zonengeometrie (Regeln: Klassenkopf).</summary>
        internal static Umrisseingang Eingang(GebaeudeAbbild abbild, int index, GebaeudeZonierung zonierung = null,
                                              IReadOnlyList<Raumgrundriss> grundrisse = null)
        {
            if (abbild == null || index < 0 || index >= abbild.Gebaeude.Count) return new Umrisseingang();
            AbbildGebaeude g = abbild.Gebaeude[index];
            bool ifc = string.Equals(abbild.Format, GebaeudeQuelle.FORMAT_IFC, StringComparison.Ordinal);
            double drehung = ifc ? abbild.NordwinkelGrad ?? 0.0 : 0.0;
            var e = new Umrisseingang { NordwinkelGrad = abbild.NordwinkelGrad, NordwinkelAngewandt = ifc };

            // Geschosse: die der Datei (IFC), dazu je Raum Kennung, Name und Lage (gbXML).
            foreach (AbbildGeschoss s in g.Geschosse)
                if (!e.Geschosse.Any(x => string.Equals(x.Kennung, s.Kennung, StringComparison.Ordinal)))
                    e.Geschosse.Add(new Umrissgeschoss(s.Kennung, s.Name, s.LageM));
            foreach (AbbildRaum r in g.Raeume)
                if (r.GeschossKennung != null && !e.Geschosse.Any(x => string.Equals(x.Kennung, r.GeschossKennung, StringComparison.Ordinal)))
                    e.Geschosse.Add(new Umrissgeschoss(r.GeschossKennung, r.GeschossName, r.GeschossLageM));

            // Zonen in der Rangfolge der Zonierung.
            bool mitZonen = zonierung != null && !zonierung.Abgelehnt && ReferenceEquals(zonierung.Gebaeude, g);
            if (mitZonen)
                foreach (Importzone z in zonierung.Zonen)
                    e.Zonen.Add(new Umrisszone(z.Schluessel, z.Name, z.IstBeheizt, z.FlaecheM2, z.VolumenM3, z.HoeheM, z.Handgeaendert));

            // Grundrisse je Raum nach ihrer (gekürzten) Quellkennung.
            var jeKennung = new Dictionary<string, Raumgrundriss>(StringComparer.Ordinal);
            foreach (Raumgrundriss gr in grundrisse ?? Array.Empty<Raumgrundriss>())
                if (gr?.Quellkennung != null) jeKennung.TryAdd(gr.Quellkennung, gr);

            // Räume in der Reihenfolge der Datei.
            var jeRaum = new Dictionary<string, (AbbildRaum Abbild, Umrissraum Umriss)>(StringComparer.Ordinal);
            foreach (AbbildRaum r in g.Raeume)
            {
                if (r.Kennung == null || jeRaum.ContainsKey(r.Kennung)) continue;
                var u = new Umrissraum
                {
                    Kennung = r.Kennung,
                    Name = string.IsNullOrWhiteSpace(r.Name) ? r.Kennung : r.Name.Trim(),
                    GeschossKennung = r.GeschossKennung,
                    Zone = mitZonen ? zonierung.ZoneVon(r.Kennung) : -1,
                    Beheizt = zonierung != null ? zonierung.RaumBeheizt(r) : r.Beheizt,
                    FlaecheM2 = r.FlaecheM2,
                    VolumenM3 = r.VolumenM3,
                    HoeheM = r.HoeheM,
                    Koerper = r.Koerper,
                    Grundrisse = grundrisse == null
                        ? r.Grundrisse ?? Array.Empty<Raumgrundriss>()
                        : jeKennung.TryGetValue(Quellkennung.Kuerzen(r.Kennung), out Raumgrundriss eigener) ? new[] { eigener } : Array.Empty<Raumgrundriss>(),
                };
                jeRaum[r.Kennung] = (r, u);
                e.Raeume.Add(u);
            }

            // Seiten je Bauteil in der Reihenfolge der Bauteile.
            foreach (AbbildBauteil b in g.Bauteile)
                if (b.Art != Bauteilart.Fenster && b.Art != Bauteilart.Tuer)
                    Seiten(b, g, jeRaum, drehung);
            return e;
        }

        private static void Seiten(AbbildBauteil b, AbbildGebaeude g, Dictionary<string, (AbbildRaum Abbild, Umrissraum Umriss)> jeRaum,
                                   double drehung)
        {
            var seiten = new List<(AbbildRaum Raum, Umrissraum Umriss, AbbildGrenze Grenze)>();
            if (b.Grenzen.Count > 0)
            {
                foreach (AbbildGrenze gr in b.Grenzen)
                    if (!gr.Virtuell && gr.RaumKennung != null && jeRaum.TryGetValue(gr.RaumKennung, out var t))
                        seiten.Add((t.Abbild, t.Umriss, gr));
            }
            else
            {
                foreach (AbbildNachbar n in b.Nachbarn)
                    if (n.Kennung != null && jeRaum.TryGetValue(n.Kennung, out var t))
                        seiten.Add((t.Abbild, t.Umriss, null));
            }
            if (seiten.Count == 0) return;

            int raeume = seiten.Select(s => s.Raum.Kennung).Distinct(StringComparer.Ordinal).Count();
            foreach (var s in seiten)
            {
                int pos = b.Nachbarn.FindIndex(n => string.Equals(n.Kennung, s.Raum.Kennung, StringComparison.Ordinal));
                Randbedingung lage = s.Grenze == null || s.Grenze.Lage == Randbedingung.Unbekannt ? b.Randbedingung : s.Grenze.Lage;
                Grenzstellung stellung = Stellung(b, s.Grenze, s.Raum, pos, g, jeRaum);
                s.Umriss.Seiten.Add(new Umrissseite
                {
                    Verweis = new Grenzverweis(s.Grenze?.Kennung ?? b.Kennung, b.Kennung, b.Art, stellung, lage, s.Grenze?.GegenstueckKennung),
                    RandpunkteM = s.Grenze != null ? s.Grenze.RandpunkteM : b.RandpunkteM,
                    FlaecheM2 = s.Grenze?.FlaecheM2 ?? Anteil(b.BruttoflaecheM2, lage, raeume),
                    Sektor = stellung == Grenzstellung.Wand ? Sektor(b, s.Grenze, pos, drehung) : null,
                    AzimutGrad = stellung == Grenzstellung.Wand ? Azimut(b, s.Grenze, pos, drehung) : null,
                });
            }
        }

        /// <summary>Wand, Boden oder Decke aus Sicht des Raums (Regeln: Klassenkopf).</summary>
        internal static Grenzstellung Stellung(AbbildBauteil b, AbbildGrenze grenze, AbbildRaum raum, int pos, AbbildGebaeude g,
                                               IReadOnlyDictionary<string, (AbbildRaum Abbild, Umrissraum Umriss)> jeRaum)
        {
            if (grenze?.Normale is double[] n && n.Length >= 3)
            {
                if (Math.Abs(n[2]) <= SENKRECHT_NZ) return Grenzstellung.Wand;
                return n[2] < 0.0 ? Grenzstellung.Boden : Grenzstellung.Decke;
            }
            switch (b.Art)
            {
                case Bauteilart.Aussenwand:
                case Bauteilart.Innenwand:
                case Bauteilart.Vorhangfassade:
                    return Grenzstellung.Wand;
                case Bauteilart.Bodenplatte:
                    return Grenzstellung.Boden;
                case Bauteilart.Dach:
                    return Grenzstellung.Decke;
            }
            if (b.NeigungGrad is double t && Math.Abs(Math.Cos(t * Math.PI / 180.0)) <= SENKRECHT_NZ) return Grenzstellung.Wand;

            if (pos >= 0)
            {
                int anderer = b.Nachbarn.FindIndex(x => !string.Equals(x.Kennung, raum.Kennung, StringComparison.Ordinal));
                if (GebaeudeHuelleneinordnung.Boden(b, pos, anderer) is bool boden) return boden ? Grenzstellung.Boden : Grenzstellung.Decke;
            }
            string andererRaum = b.Grenzen.Where(x => !x.Virtuell && x.RaumKennung != null).Select(x => x.RaumKennung)
                                  .Concat(b.Nachbarn.Select(x => x.Kennung))
                                  .FirstOrDefault(k => k != null && !string.Equals(k, raum.Kennung, StringComparison.Ordinal) && jeRaum.ContainsKey(k));
            if (andererRaum != null && Lage(raum, g) is double eigen && Lage(jeRaum[andererRaum].Abbild, g) is double andere && eigen != andere)
                return eigen > andere ? Grenzstellung.Boden : Grenzstellung.Decke;
            return Grenzstellung.Unbestimmt;
        }

        /// <summary>Die Höhenlage des Geschosses eines Raums [m]: die des Geschosses der Datei, sonst die am Raum (gbXML).</summary>
        private static double? Lage(AbbildRaum r, AbbildGebaeude g)
            => r?.GeschossKennung == null ? null
             : g.Geschosse.FirstOrDefault(s => string.Equals(s.Kennung, r.GeschossKennung, StringComparison.Ordinal))?.LageM ?? r.GeschossLageM;

        /// <summary>
        /// Der Himmelsrichtungssektor einer Wand aus Sicht des Raums: aus der Normalen der Raumgrenze (vom Raum
        /// weg), gedreht wie die Bauteile; sonst der Azimut des Bauteils, aus Sicht des zweiten Nachbarn
        /// gespiegelt; <c>null</c> = keine Himmelsrichtung.
        /// </summary>
        private static int? Sektor(AbbildBauteil b, AbbildGrenze grenze, int pos, double drehung)
        {
            if (grenze?.Normale is double[] n && n.Length >= 2 && Math.Sqrt(n[0] * n[0] + n[1] * n[1]) > 1e-6)
                return GebaeudeAggregation.Sektor(Zonengeometrie.ModellAzimut(n[0], n[1]) - drehung);
            if (b.AzimutGrad is double a) return GebaeudeAggregation.Sektor(pos > 0 ? a + 180.0 : a);
            return null;
        }

        /// <summary>HC-5c: der wahre Azimut einer Wand aus Sicht des Raums [°] — dieselbe Quelle wie <see cref="Sektor"/>, in [0, 360).</summary>
        private static double? Azimut(AbbildBauteil b, AbbildGrenze grenze, int pos, double drehung)
        {
            double? a = null;
            if (grenze?.Normale is double[] n && n.Length >= 2 && Math.Sqrt(n[0] * n[0] + n[1] * n[1]) > 1e-6)
                a = Zonengeometrie.ModellAzimut(n[0], n[1]) - drehung;
            else if (b.AzimutGrad is double w) a = pos > 0 ? w + 180.0 : w;
            if (!a.HasValue) return null;
            double r = a.Value % 360.0;
            return r < 0.0 ? r + 360.0 : r + 0.0;
        }

        /// <summary>
        /// Die Fläche einer Seite ohne eigenes Polygon: die Bruttofläche des Bauteils — außen nach der Zahl
        /// seiner Räume geteilt, innen bei mehr als zwei Räumen zu je 2/n; <c>null</c> = keine.
        /// </summary>
        private static double? Anteil(double? brutto, Randbedingung lage, int raeume)
        {
            if (!(brutto > 0.0)) return null;
            if (raeume <= 1) return brutto;
            if (lage == Randbedingung.Aussenluft || lage == Randbedingung.Erdreich) return brutto.Value / raeume;
            return raeume <= 2 ? brutto : brutto.Value * 2.0 / raeume;
        }
    }
}
