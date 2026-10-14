using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Vermerke eines Raumgrundrisses (Konzept HottCAD-Verbund 11.2): sprachneutrale Schlüssel, nie Anzeigetext — den
    /// Text wählt die Ansicht. Gespeichert mit Komma in <c>Tab_Raumgrundriss.Vermerke</c>, in dieser Reihenfolge.
    /// </summary>
    internal enum Grundrissvermerk
    {
        /// <summary>Die Bodendreiecke liegen mehr als 0,05 m auseinander — das Prisma beginnt am tiefsten Boden.</summary>
        Stufen,

        /// <summary>Überlappende Dreiecke oder ineinanderliegende Außenringe — einmal genommen.</summary>
        Ueberlappung,

        /// <summary>Ein Ring unter 0,01 m² ist entfallen.</summary>
        Splitter,

        /// <summary>Der Grundriss ist die konvexe Hülle aller Punkte — Einbuchtungen gehen verloren.</summary>
        Konvex,

        /// <summary>Die Höhe (Spanne des Körpers) übersteigt V/A um mehr als 10 % bzw. der Beschnitt fehlt (F9).</summary>
        Dachschraege,

        /// <summary>Der Boden weicht mehr als 0,5 m von der Lage des Geschosses ab.</summary>
        Geschosslage,

        /// <summary>Die Ringfläche weicht mehr als 10 % von der Raumfläche des Mengensatzes ab (F8).</summary>
        Flaeche,

        /// <summary>Der Grundriss stammt unmittelbar aus dem Raumpolygon der Projektdatei (Datenaustauschkonzept 17.2), nicht aus einem Körper.</summary>
        Raumpolygon,
    }

    /// <summary>
    /// <b>Ein Ring eines Raumgrundrisses</b> in ganzen Millimetern, absolut im Modellsystem (x Ost, y Nord des Modells),
    /// ohne Schlusspunkt. Außenring gegen den Uhrzeigersinn, Loch im Uhrzeigersinn; der erste Punkt ist der kleinste
    /// (x, dann y). Unveränderlich, ohne Datenbank.
    /// </summary>
    internal sealed class Grundrissring
    {
        internal Grundrissring(IReadOnlyList<long[]> punkteMm)
        {
            PunkteMm = punkteMm ?? Array.Empty<long[]>();
            long doppelt = Koerpergrundriss.DoppelteFlaeche(PunkteMm);
            IstLoch = doppelt < 0;
            FlaecheM2 = Math.Abs(doppelt) / 2.0 / 1e6;
        }

        /// <summary>Die Punkte, je Punkt { x, y } in ganzen Millimetern.</summary>
        internal IReadOnlyList<long[]> PunkteMm { get; }

        /// <summary>Im Uhrzeigersinn: ein Loch des vorangehenden Außenrings.</summary>
        internal bool IstLoch { get; }

        /// <summary>Der Flächeninhalt [m²], Betrag.</summary>
        internal double FlaecheM2 { get; }

        /// <summary>Die Punkte in Metern, je Punkt { x, y }.</summary>
        internal IReadOnlyList<double[]> PunkteM => PunkteMm.Select(p => new[] { p[0] / 1000.0, p[1] / 1000.0 }).ToList();

        public override string ToString() => (IstLoch ? "Loch " : "Ring ") + PunkteMm.Count.ToString(CultureInfo.InvariantCulture) + " Punkte";
    }

    /// <summary>
    /// <b>Der Grundriss eines importierten Raums</b> (HC-5, Konzept HottCAD-Verbund 11.2 und 11.3) — das Kernobjekt, das
    /// beim Import entsteht, in <c>Tab_Raumgrundriss</c> steht und von dort gelesen wird
    /// (<see cref="GebaeudeImportCtrl.LesenRaumgrundrisse"/>). Ein Prisma von <see cref="BodenM"/> um <see cref="HoeheM"/>
    /// über den <see cref="Ringe"/>n, in Weltkoordinaten des Modellsystems [m]; der Nordwinkel dreht nur Azimute.
    ///
    /// <para><b>Nur Anzeige und Export:</b> Kein Rechenweg liest ihn; er speist weder <c>AbbildRaum.GrundrissM</c> noch die
    /// Raumfläche (11.5).</para>
    /// </summary>
    internal sealed class Raumgrundriss
    {
        /// <summary>Die Zeile in <c>Tab_Raumgrundriss</c>; <c>null</c> = noch nicht gespeichert.</summary>
        internal int? ID { get; init; }

        /// <summary>Die Importquelle; <c>null</c> = noch nicht gespeichert.</summary>
        internal int? IdImportquelle { get; init; }

        /// <summary>Die Zone, der der Raum beim Import zugeordnet wurde; <c>null</c> = keine.</summary>
        internal int? IdZone { get; init; }

        /// <summary>Die Kennung des Raums in der Datei (<c>GlobalId</c> bzw. gbXML-<c>id</c>), gekürzt auf 64 Zeichen.</summary>
        internal string Quellkennung { get; init; } = "";

        /// <summary>Der Name des Raums; <c>null</c> = keiner.</summary>
        internal string Raumname { get; init; }

        /// <summary>Der Name des Geschosses; <c>null</c> = keiner.</summary>
        internal string Geschoss { get; init; }

        /// <summary>Die Höhenlage des Geschosses der Datei [m]; <c>null</c> = unbekannt.</summary>
        internal double? GeschossLageM { get; init; }

        /// <summary>Die Höhenlage des Bodens im Modellsystem [m].</summary>
        internal double BodenM { get; init; }

        /// <summary>Die Höhe des Prismas [m] (&gt; 0).</summary>
        internal double HoeheM { get; init; }

        /// <summary>Die Ringe: je Außenring seine Löcher dahinter.</summary>
        internal IReadOnlyList<Grundrissring> Ringe { get; init; } = Array.Empty<Grundrissring>();

        /// <summary>Außenringe minus Löcher [m²].</summary>
        internal double RingflaecheM2 { get; init; }

        /// <summary>(Ringfläche − Raumfläche) / Raumfläche; <c>null</c> = keine Raumfläche.</summary>
        internal double? Abweichung { get; init; }

        /// <summary>Wie der Grundriss hergeleitet ist: <c>KoerperBoden</c>, <c>KoerperDecke</c>, <c>KoerperHuelle</c>, <c>Boden</c>, <c>Decke</c>.</summary>
        internal Umrissherleitung Herleitung { get; init; }

        /// <summary>Die Vermerke, aufsteigend und ohne Doppel.</summary>
        internal IReadOnlyList<Grundrissvermerk> Vermerke { get; init; } = Array.Empty<Grundrissvermerk>();

        /// <summary>
        /// HC-5c: die Drehung der Prismenkanten gegen Nord [°] — wahrer Azimut = Modellazimut − Drehung; aus der Quelle gelesen
        /// (<see cref="GebaeudeImportCtrl.Nordangabe"/>). <c>null</c> = die Drehung des Eingangs (Import: frisch abgeleitet).
        /// Nicht gespeichert: Der Nordwinkel steht an der Quelle.
        /// </summary>
        internal double? DrehungGrad { get; init; }

        /// <summary>HC-5c: Nennt die Quelle keinen Nordwinkel (Modell-Nord = Nord angenommen)? Nicht gespeichert.</summary>
        internal bool NordwinkelUnbekannt { get; init; }

        /// <summary>Die Herkunft der Geometrie: aus dem Dateikörper bzw. — Herleitung Boden/Decke — aus den Raumgrenzen.</summary>
        internal Geometrieherkunft Herkunft
            => Herleitung == Umrissherleitung.Boden || Herleitung == Umrissherleitung.Decke
               ? Geometrieherkunft.Raumgrenzen : Geometrieherkunft.Dateikoerper;

        /// <summary>Die Textform der Ringe (Spalte <c>Ringe</c>).</summary>
        internal string RingeText => RingeSchreiben(Ringe);

        /// <summary>Die Vermerke mit Komma (Spalte <c>Vermerke</c>); <c>null</c> = keine.</summary>
        internal string VermerkeText => Vermerke.Count == 0 ? null : string.Join(",", Vermerke.Select(v => v.ToString()));

        /// <summary>Die Größen der Abgleichsregeln (11.2, Tabelle „Maße je Raum“).</summary>
        internal const double SCHWELLE_FLAECHE = 0.10;

        /// <summary>Übersteigt die Höhe V/A um mehr als diesen Anteil, gilt <see cref="Grundrissvermerk.Dachschraege"/> (F9).</summary>
        internal const double SCHWELLE_HOEHE = 0.10;

        /// <summary>Weicht der Boden mehr als so viele Meter von der Geschosslage ab, gilt <see cref="Grundrissvermerk.Geschosslage"/>.</summary>
        internal const double SCHWELLE_GESCHOSS_M = 0.5;

        /// <summary>
        /// <b>Bildet den Grundriss eines Raums</b> mit dem Abgleich gegen den Mengensatz (11.2): Abweichung der Ringfläche
        /// (Vermerk <c>Flaeche</c> über 10 %), Höhe gegen V/A (Vermerk <c>Dachschraege</c> über 10 % oder ohne Beschnitt),
        /// Boden gegen die Geschosslage (Vermerk <c>Geschosslage</c> über 0,5 m). <c>null</c>, wenn nichts zu speichern ist
        /// (keine Kennung, kein Ring, keine Fläche, keine Höhe).
        /// </summary>
        internal static Raumgrundriss Bilden(string quellkennung, string raumname, string geschoss, double? geschossLageM,
                                             double? flaecheM2, double? volumenM3, Umrissherleitung herleitung,
                                             IReadOnlyList<Grundrissring> ringe, double bodenM, double hoeheM,
                                             IEnumerable<Grundrissvermerk> vermerke, bool ohneBeschnitt)
        {
            string kennung = WindowsFormsApplication1.Quellkennung.Kuerzen(quellkennung ?? "");
            if (kennung.Length == 0 || ringe == null || ringe.Count == 0) return null;
            double flaeche = ringe.Sum(r => r.IstLoch ? -r.FlaecheM2 : r.FlaecheM2);
            if (!(flaeche > 0) || !(hoeheM > 0) || double.IsNaN(bodenM) || double.IsInfinity(bodenM)) return null;

            var v = new SortedSet<Grundrissvermerk>(vermerke ?? Array.Empty<Grundrissvermerk>());
            double? abweichung = null;
            if (flaecheM2 is double a && a > 0)
            {
                abweichung = (flaeche - a) / a;
                if (Math.Abs(abweichung.Value) > SCHWELLE_FLAECHE) v.Add(Grundrissvermerk.Flaeche);
            }
            double? va = flaecheM2 is double f && f > 0 && volumenM3 is double vol && vol > 0 ? vol / f : (double?)null;
            if (ohneBeschnitt || (va.HasValue && hoeheM > va.Value * (1.0 + SCHWELLE_HOEHE))) v.Add(Grundrissvermerk.Dachschraege);
            if (geschossLageM is double lage && Math.Abs(bodenM - lage) > SCHWELLE_GESCHOSS_M) v.Add(Grundrissvermerk.Geschosslage);

            return new Raumgrundriss
            {
                Quellkennung = kennung,
                Raumname = Kuerzen(raumname),
                Geschoss = Kuerzen(geschoss),
                GeschossLageM = geschossLageM,
                BodenM = bodenM,
                HoeheM = hoeheM,
                Ringe = ringe,
                RingflaecheM2 = flaeche,
                Abweichung = abweichung,
                Herleitung = herleitung,
                Vermerke = v.ToList(),
            };
        }

        private static string Kuerzen(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string t = text.Trim();
            return t.Length <= RaumgrundrissSchema.NAME_MAX ? t : t.Substring(0, RaumgrundrissSchema.NAME_MAX);
        }

        /// <summary>
        /// Die Textform der Ringe: Ringe durch <c>|</c>, Punkte durch <c>;</c>, je Punkt <c>x,y</c> in ganzen Millimetern,
        /// invariant, ohne Schlusspunkt (11.3).
        /// </summary>
        internal static string RingeSchreiben(IEnumerable<Grundrissring> ringe)
        {
            var t = new StringBuilder();
            foreach (Grundrissring r in ringe ?? Array.Empty<Grundrissring>())
            {
                if (t.Length > 0) t.Append('|');
                for (int i = 0; i < r.PunkteMm.Count; i++)
                {
                    if (i > 0) t.Append(';');
                    t.Append(r.PunkteMm[i][0].ToString(CultureInfo.InvariantCulture)).Append(',')
                     .Append(r.PunkteMm[i][1].ToString(CultureInfo.InvariantCulture));
                }
            }
            return t.ToString();
        }

        /// <summary>Liest die Textform der Ringe; eine nicht lesbare Form ergibt eine leere Liste (nie eine Ausnahme).</summary>
        internal static IReadOnlyList<Grundrissring> RingeLesen(string text)
        {
            var ringe = new List<Grundrissring>();
            if (string.IsNullOrWhiteSpace(text)) return ringe;
            foreach (string ring in text.Split('|'))
            {
                var punkte = new List<long[]>();
                foreach (string punkt in ring.Split(';'))
                {
                    string[] xy = punkt.Split(',');
                    if (xy.Length != 2
                        || !long.TryParse(xy[0], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long x)
                        || !long.TryParse(xy[1], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long y))
                        return Array.Empty<Grundrissring>();
                    punkte.Add(new[] { x, y });
                }
                if (punkte.Count < 3) return Array.Empty<Grundrissring>();
                ringe.Add(new Grundrissring(punkte));
            }
            return ringe;
        }

        /// <summary>Liest die Vermerke; unbekannte Schlüssel fallen weg.</summary>
        internal static IReadOnlyList<Grundrissvermerk> VermerkeLesen(string text)
        {
            var v = new SortedSet<Grundrissvermerk>();
            if (string.IsNullOrWhiteSpace(text)) return v.ToList();
            foreach (string s in text.Split(','))
                if (Enum.TryParse(s.Trim(), false, out Grundrissvermerk g) && Enum.IsDefined(typeof(Grundrissvermerk), g)
                    && !int.TryParse(s.Trim(), out _))
                    v.Add(g);
            return v.ToList();
        }

        public override string ToString()
            => Quellkennung + " (" + Herleitung + ", " + RingflaecheM2.ToString("0.###", CultureInfo.InvariantCulture) + " m²"
               + (Vermerke.Count > 0 ? ", " + VermerkeText : "") + ")";
    }

    /// <summary>
    /// <b>Der Grundriss aus dem Dateikörper</b> (HC-5, Konzept HottCAD-Verbund 11.2) — ohne Geometriekern, ohne Datenbank,
    /// ohne Oberfläche; wirft nicht.
    ///
    /// <para><b>Stufe 1 (Boden):</b> die Dreiecke mit Außennormale nach unten innerhalb 10° auf jeder Höhe (bei geschlossener
    /// Schale nach dem Vorzeichen des Volumens gerichtet; bei offener die waagerechten Dreiecke im untersten Band 0,05 m),
    /// in die Grundrissebene auf ganze Millimeter projiziert, an T-Stößen geteilt; gegenläufige Kanten heben sich auf, die
    /// Randkanten werden deterministisch zu Außenringen und Löchern verkettet, kollineare Punkte unter 1 mm und Ringe unter
    /// 0,01 m² entfallen. <b>Stufe 2 (Decke):</b> dasselbe mit den Dreiecken nach oben. <b>Stufe 3 (Hülle):</b> die konvexe
    /// Hülle aller Punkte. Ohne Ergebnis bleibt der Raum beim Rechteck (Stufe 4, nicht hier).</para>
    ///
    /// <para><b>Deterministisch:</b> ganzzahlige Kanten, Verkettung mit der kleinsten Linksdrehung über ganzzahlige
    /// Kreuzprodukte (kein <c>Atan2</c>), Start am kleinsten Punkt (x, dann y).</para>
    /// </summary>
    internal sealed class Koerpergrundriss
    {
        /// <summary>Der Winkel gegen die Waagerechte, bis zu dem ein Dreieck Boden bzw. Decke ist [°].</summary>
        internal const double WINKEL_GRAD = 10.0;

        /// <summary>Das unterste bzw. oberste Band einer offenen Schale und die Schwelle des Vermerks <c>Stufen</c> [m].</summary>
        internal const double BAND_M = 0.05;

        /// <summary>Der Abstand, bis zu dem ein Punkt auf einer Kante liegt bzw. kollinear ist [mm].</summary>
        internal const double PUNKT_MM = 1.0;

        /// <summary>Die kleinste Fläche eines projizierten Dreiecks [m²].</summary>
        internal const double DREIECK_MIN_M2 = 1e-6;

        /// <summary>Die kleinste Fläche eines Rings [m²]; darunter Vermerk <c>Splitter</c>.</summary>
        internal const double SPLITTER_M2 = 0.01;

        private static readonly double COS_GRENZE = Math.Cos(WINKEL_GRAD * Math.PI / 180.0);

        private Koerpergrundriss() { }

        /// <summary>Die Herleitung; <c>null</c> = kein Grundriss (Stufe 4, das Rechteck bleibt).</summary>
        internal Umrissherleitung? Herleitung { get; private init; }

        /// <summary>Die Ringe: je Außenring seine Löcher dahinter; leer ohne Grundriss.</summary>
        internal IReadOnlyList<Grundrissring> Ringe { get; private init; } = Array.Empty<Grundrissring>();

        /// <summary>Höhenlage des Bodens [m]: tiefster Punkt der Bodendreiecke, ohne Stufe 1 der tiefste Punkt des Körpers.</summary>
        internal double BodenM { get; private init; }

        /// <summary>Die Spanne des Körpers [m] (höchster minus tiefster Punkt, F9).</summary>
        internal double HoeheM { get; private init; }

        /// <summary>Die geometrischen Vermerke (<c>Stufen</c>, <c>Ueberlappung</c>, <c>Splitter</c>, <c>Konvex</c>).</summary>
        internal IReadOnlyList<Grundrissvermerk> Vermerke { get; private init; } = Array.Empty<Grundrissvermerk>();

        /// <summary>Trägt die Ableitung einen Grundriss?</summary>
        internal bool Traegt => Herleitung.HasValue && Ringe.Count > 0;

        /// <summary>Außenringe minus Löcher [m²].</summary>
        internal double RingflaecheM2 => Ringe.Sum(r => r.IstLoch ? -r.FlaecheM2 : r.FlaecheM2);

        /// <summary>Die Ausgabe in invarianter Kultur (Determinismus): Herleitung, Boden, Höhe, Vermerke, Ringe.</summary>
        internal string Text()
            => (Herleitung?.ToString() ?? "Keine") + " Boden " + BodenM.ToString("R", CultureInfo.InvariantCulture)
               + " Hoehe " + HoeheM.ToString("R", CultureInfo.InvariantCulture)
               + " Vermerke " + string.Join(",", Vermerke) + " Ringe " + Raumgrundriss.RingeSchreiben(Ringe);

        /// <summary><b>Leitet den Grundriss ab</b> (Regeln: Klassenkopf). Ohne Körper oder ohne Dreiecke: kein Grundriss.</summary>
        internal static Koerpergrundriss Ableiten(Dateikoerper k)
        {
            if (k == null || k.DreieckZahl == 0 || k.PunkteM.Count == 0) return new Koerpergrundriss();
            double zmin = k.PunkteM.Min(p => p[2]), zmax = k.PunkteM.Max(p => p[2]);
            double hoehe = zmax - zmin;
            bool offen = k.Vermerke.Contains(Koerpervermerk.Offen);
            double richtung = 1.0;
            if (!offen && Volumen(k) < 0) richtung = -1.0;

            // Stufe 1 - Boden
            List<int> boden = Auswahl(k, unten: true, offen, richtung, zmin, zmax);
            var v1 = new SortedSet<Grundrissvermerk>();
            List<Grundrissring> ringe = boden.Count > 0 ? RingeAus(k, boden, v1) : new List<Grundrissring>();
            if (ringe.Count > 0)
            {
                double bmin = boden.SelectMany(t => k.Dreiecke[t]).Min(i => k.PunkteM[i][2]);
                double bmax = boden.SelectMany(t => k.Dreiecke[t]).Max(i => k.PunkteM[i][2]);
                if (bmax - bmin > BAND_M) v1.Add(Grundrissvermerk.Stufen);
                return new Koerpergrundriss
                {
                    Herleitung = Umrissherleitung.KoerperBoden, Ringe = ringe, BodenM = bmin, HoeheM = hoehe, Vermerke = v1.ToList()
                };
            }

            // Stufe 2 - Decke
            List<int> decke = Auswahl(k, unten: false, offen, richtung, zmin, zmax);
            var v2 = new SortedSet<Grundrissvermerk>();
            ringe = decke.Count > 0 ? RingeAus(k, decke, v2) : new List<Grundrissring>();
            if (ringe.Count > 0)
                return new Koerpergrundriss
                {
                    Herleitung = Umrissherleitung.KoerperDecke, Ringe = ringe, BodenM = zmin, HoeheM = hoehe, Vermerke = v2.ToList()
                };

            // Stufe 3 - konvexe Hülle
            List<long[]> huelle = Huelle(k.PunkteM.Select(Projiziert));
            if (huelle.Count >= 3 && DoppelteFlaeche(huelle) / 2.0 / 1e6 >= SPLITTER_M2)
                return new Koerpergrundriss
                {
                    Herleitung = Umrissherleitung.KoerperHuelle, Ringe = new[] { new Grundrissring(huelle) }, BodenM = zmin,
                    HoeheM = hoehe, Vermerke = new[] { Grundrissvermerk.Konvex }
                };
            return new Koerpergrundriss { BodenM = zmin, HoeheM = hoehe };
        }

        // ------------------------------------------------------------------
        //  Auswahl und Projektion
        // ------------------------------------------------------------------

        /// <summary>Das Volumen über den Divergenzsatz; positiv, wenn die Dreiecke nach außen umlaufen.</summary>
        internal static double Volumen(Dateikoerper k)
        {
            double v = 0.0;
            foreach (int[] d in k.Dreiecke)
            {
                double[] a = k.PunkteM[d[0]], b = k.PunkteM[d[1]], c = k.PunkteM[d[2]];
                v += (a[0] * (b[1] * c[2] - b[2] * c[1]) - a[1] * (b[0] * c[2] - b[2] * c[0]) + a[2] * (b[0] * c[1] - b[1] * c[0])) / 6.0;
            }
            return v;
        }

        private static List<int> Auswahl(Dateikoerper k, bool unten, bool offen, double richtung, double zmin, double zmax)
        {
            var liste = new List<int>();
            for (int t = 0; t < k.DreieckZahl; t++)
            {
                double nz = k.Normalen[t][2];
                if (offen)
                {
                    if (Math.Abs(nz) < COS_GRENZE) continue;
                    bool imBand = k.Dreiecke[t].All(i => unten ? k.PunkteM[i][2] <= zmin + BAND_M : k.PunkteM[i][2] >= zmax - BAND_M);
                    if (imBand) liste.Add(t);
                }
                else if (unten ? nz * richtung <= -COS_GRENZE : nz * richtung >= COS_GRENZE)
                    liste.Add(t);
            }
            return liste;
        }

        private static long[] Projiziert(double[] p)
            => new[] { (long)Math.Round(p[0] * 1000.0, MidpointRounding.AwayFromZero), (long)Math.Round(p[1] * 1000.0, MidpointRounding.AwayFromZero) };

        private static long Kreuz(long ax, long ay, long bx, long by) => ax * by - ay * bx;

        /// <summary>Die doppelte vorzeichenbehaftete Fläche [mm²], relativ zum ersten Punkt (keine Auslöschung bei großen Lagen).</summary>
        internal static long DoppelteFlaeche(IReadOnlyList<long[]> ring)
        {
            if (ring == null || ring.Count < 3) return 0;
            long x0 = ring[0][0], y0 = ring[0][1], s = 0;
            for (int i = 1; i + 1 < ring.Count; i++)
                s += Kreuz(ring[i][0] - x0, ring[i][1] - y0, ring[i + 1][0] - x0, ring[i + 1][1] - y0);
            return s;
        }

        // ------------------------------------------------------------------
        //  Kanten, Ringe, Vereinfachung
        // ------------------------------------------------------------------

        private readonly record struct Punkt(long X, long Y) : IComparable<Punkt>
        {
            public int CompareTo(Punkt o) => X != o.X ? X.CompareTo(o.X) : Y.CompareTo(o.Y);
        }

        private static List<Grundrissring> RingeAus(Dateikoerper k, List<int> dreiecke, SortedSet<Grundrissvermerk> vermerke)
        {
            // 3. Projektion, je Dreieck gegen den Uhrzeigersinn gerichtet.
            var kanten = new List<(Punkt A, Punkt B)>();
            var punkte = new SortedSet<Punkt>();
            foreach (int t in dreiecke)
            {
                int[] d = k.Dreiecke[t];
                long[] a = Projiziert(k.PunkteM[d[0]]), b = Projiziert(k.PunkteM[d[1]]), c = Projiziert(k.PunkteM[d[2]]);
                long doppelt = Kreuz(b[0] - a[0], b[1] - a[1], c[0] - a[0], c[1] - a[1]);
                if (Math.Abs(doppelt) / 2.0 / 1e6 < DREIECK_MIN_M2) continue;
                if (doppelt < 0) (b, c) = (c, b);
                Punkt pa = new Punkt(a[0], a[1]), pb = new Punkt(b[0], b[1]), pc = new Punkt(c[0], c[1]);
                kanten.Add((pa, pb));
                kanten.Add((pb, pc));
                kanten.Add((pc, pa));
                punkte.Add(pa);
                punkte.Add(pb);
                punkte.Add(pc);
            }
            if (kanten.Count == 0) return new List<Grundrissring>();

            // 4. T-Stöße: jede Kante an jedem Punkt geteilt, der auf ihr liegt.
            Punkt[] sortiert = punkte.ToArray();
            var zaehler = new Dictionary<(Punkt, Punkt), int>();
            foreach ((Punkt a, Punkt b) in kanten)
            {
                List<Punkt> teile = Teilpunkte(a, b, sortiert);
                Punkt von = a;
                foreach (Punkt p in teile.Append(b))
                {
                    if (p != von) zaehler[(von, p)] = zaehler.TryGetValue((von, p), out int n) ? n + 1 : 1;
                    von = p;
                }
            }

            // 5. Gegenläufige Kanten heben sich auf; gleichläufige Doppel bleiben einmal.
            var rand = new SortedDictionary<Punkt, List<Punkt>>();
            int randkanten = 0;
            foreach (KeyValuePair<(Punkt, Punkt), int> e in zaehler.OrderBy(x => x.Key.Item1).ThenBy(x => x.Key.Item2))
            {
                (Punkt a, Punkt b) = e.Key;
                zaehler.TryGetValue((b, a), out int gegen);
                int netto = e.Value - gegen;
                if (netto <= 0) continue;
                if (netto > 1) vermerke.Add(Grundrissvermerk.Ueberlappung);
                if (!rand.TryGetValue(a, out List<Punkt> aus)) rand[a] = aus = new List<Punkt>();
                aus.Add(b);
                randkanten++;
            }

            // 6. Ringe verketten: Start am kleinsten Punkt, je Schritt die kleinste Linksdrehung.
            var roh = new List<List<long[]>>();
            while (randkanten > 0)
            {
                Punkt start = rand.First(x => x.Value.Count > 0).Key;
                var ring = new List<Punkt> { start };
                Punkt aktuell = start;
                long dx = 0, dy = -1;                       // gedachte Ankunft von oben
                bool geschlossen = false;
                int grenze = randkanten;
                for (int schritt = 0; schritt < grenze; schritt++)
                {
                    if (!rand.TryGetValue(aktuell, out List<Punkt> aus) || aus.Count == 0) break;
                    int wahl = 0;
                    for (int i = 1; i < aus.Count; i++)
                        if (Drehung(dx, dy, aus[i].X - aktuell.X, aus[i].Y - aktuell.Y,
                                    aus[wahl].X - aktuell.X, aus[wahl].Y - aktuell.Y) < 0)
                            wahl = i;
                    Punkt naechster = aus[wahl];
                    aus.RemoveAt(wahl);
                    randkanten--;
                    dx = naechster.X - aktuell.X;
                    dy = naechster.Y - aktuell.Y;
                    aktuell = naechster;
                    if (aktuell == start)
                    {
                        geschlossen = true;
                        break;
                    }
                    ring.Add(aktuell);
                }
                if (geschlossen && ring.Count >= 3) roh.Add(ring.Select(p => new[] { p.X, p.Y }).ToList());
            }

            // 7. Vereinfachen, Splitter weg, Löcher an ihre Außenringe.
            var aussen = new List<Grundrissring>();
            var loecher = new List<Grundrissring>();
            foreach (List<long[]> r in roh)
            {
                List<long[]> einfach = Vereinfacht(r);
                if (einfach.Count < 3) continue;
                var ring = new Grundrissring(Normiert(einfach));
                if (ring.FlaecheM2 < SPLITTER_M2)
                {
                    vermerke.Add(Grundrissvermerk.Splitter);
                    continue;
                }
                (ring.IstLoch ? loecher : aussen).Add(ring);
            }
            return Ordnen(aussen, loecher, vermerke);
        }

        /// <summary>Die Punkte auf der Kante a→b (Abstand ≤ 1 mm, echt zwischen den Enden), nach ihrem Abstand von a.</summary>
        private static List<Punkt> Teilpunkte(Punkt a, Punkt b, Punkt[] sortiert)
        {
            var treffer = new List<(double T, Punkt P)>();
            long xmin = Math.Min(a.X, b.X) - 1, xmax = Math.Max(a.X, b.X) + 1;
            long ymin = Math.Min(a.Y, b.Y) - 1, ymax = Math.Max(a.Y, b.Y) + 1;
            int i = UntereGrenze(sortiert, xmin);
            double lx = b.X - a.X, ly = b.Y - a.Y, l2 = lx * lx + ly * ly;
            if (l2 <= 0) return new List<Punkt>();
            double l = Math.Sqrt(l2);
            for (; i < sortiert.Length && sortiert[i].X <= xmax; i++)
            {
                Punkt p = sortiert[i];
                if (p.Y < ymin || p.Y > ymax || p == a || p == b) continue;
                double px = p.X - a.X, py = p.Y - a.Y;
                double t = (px * lx + py * ly) / l2;
                if (t <= 0 || t >= 1) continue;
                if (Math.Abs(px * ly - py * lx) / l > PUNKT_MM) continue;
                treffer.Add((t, p));
            }
            return treffer.OrderBy(x => x.T).ThenBy(x => x.P).Select(x => x.P).ToList();
        }

        private static int UntereGrenze(Punkt[] sortiert, long x)
        {
            int lo = 0, hi = sortiert.Length;
            while (lo < hi)
            {
                int m = (lo + hi) / 2;
                if (sortiert[m].X < x) lo = m + 1;
                else hi = m;
            }
            return lo;
        }

        /// <summary>
        /// Vergleicht die Drehung zweier Richtungen u und w gegen die Ankunft d im Bereich (−180°, 180°]: negativ, wenn u
        /// weniger nach links dreht als w. Ganzzahlig über Halbebenen und Kreuzprodukte — kein <c>Atan2</c>.
        /// </summary>
        private static int Drehung(long dx, long dy, long ux, long uy, long wx, long wy)
        {
            int gu = Halbebene(dx, dy, ux, uy), gw = Halbebene(dx, dy, wx, wy);
            if (gu != gw) return gu.CompareTo(gw);
            if (gu == 0 || gu == 2)
            {
                long k = Kreuz(ux, uy, wx, wy);
                if (k != 0) return k > 0 ? -1 : 1;
            }
            // gleiche Richtung: die kürzere Kante zuerst, dann der kleinere Punkt
            long lu = ux * ux + uy * uy, lw = wx * wx + wy * wy;
            if (lu != lw) return lu.CompareTo(lw);
            return ux != wx ? ux.CompareTo(wx) : uy.CompareTo(wy);
        }

        /// <summary>0 = rechts (−180°, 0°), 1 = geradeaus, 2 = links (0°, 180°), 3 = zurück (180°).</summary>
        private static int Halbebene(long dx, long dy, long ux, long uy)
        {
            long k = Kreuz(dx, dy, ux, uy);
            if (k < 0) return 0;
            if (k > 0) return 2;
            return dx * ux + dy * uy > 0 ? 1 : 3;
        }

        /// <summary>Entfernt Doppelpunkte, Spitzen und kollineare Punkte (Abstand zur Sehne der Nachbarn unter 1 mm).</summary>
        private static List<long[]> Vereinfacht(List<long[]> ring)
        {
            var r = new List<long[]>(ring);
            bool geaendert = true;
            while (geaendert && r.Count >= 3)
            {
                geaendert = false;
                for (int i = 0; i < r.Count && r.Count >= 3; i++)
                {
                    long[] a = r[(i - 1 + r.Count) % r.Count], p = r[i], c = r[(i + 1) % r.Count];
                    bool weg;
                    if (p[0] == a[0] && p[1] == a[1]) weg = true;
                    else if (a[0] == c[0] && a[1] == c[1]) weg = true;            // Spitze a → p → a
                    else
                    {
                        double sx = c[0] - a[0], sy = c[1] - a[1];
                        double abstand = Math.Abs(sx * (p[1] - a[1]) - sy * (p[0] - a[0])) / Math.Sqrt(sx * sx + sy * sy);
                        weg = abstand < PUNKT_MM;
                    }
                    if (!weg) continue;
                    r.RemoveAt(i);
                    i = Math.Max(-1, i - 2);
                    geaendert = true;
                }
            }
            return r;
        }

        /// <summary>Dreht den Ring so, dass er am kleinsten Punkt (x, dann y) beginnt.</summary>
        private static List<long[]> Normiert(List<long[]> ring)
        {
            int start = 0;
            for (int i = 1; i < ring.Count; i++)
                if (ring[i][0] < ring[start][0] || (ring[i][0] == ring[start][0] && ring[i][1] < ring[start][1]))
                    start = i;
            return ring.Skip(start).Concat(ring.Take(start)).ToList();
        }

        /// <summary>
        /// Ordnet die Ringe: ein Außenring in einem anderen (außerhalb dessen Löchern) entfällt mit <c>Ueberlappung</c>; jedes
        /// Loch hängt am kleinsten Außenring, der es enthält; Außenringe nach ihrem ersten Punkt, je mit ihren Löchern.
        /// </summary>
        private static List<Grundrissring> Ordnen(List<Grundrissring> aussen, List<Grundrissring> loecher, SortedSet<Grundrissvermerk> vermerke)
        {
            aussen = aussen.OrderByDescending(r => r.FlaecheM2).ThenBy(r => r.PunkteMm[0][0]).ThenBy(r => r.PunkteMm[0][1]).ToList();
            var jeAussen = aussen.ToDictionary(r => r, r => new List<Grundrissring>());
            foreach (Grundrissring loch in loecher.OrderBy(r => r.PunkteMm[0][0]).ThenBy(r => r.PunkteMm[0][1]))
            {
                double[] probe = Probepunkt(loch);
                Grundrissring traeger = aussen.Where(a => Innen(a, probe)).OrderBy(a => a.FlaecheM2).FirstOrDefault();
                if (traeger != null) jeAussen[traeger].Add(loch);
            }
            var bleibt = new List<Grundrissring>();
            foreach (Grundrissring a in aussen)
            {
                double[] probe = Probepunkt(a);
                bool liegtIn = bleibt.Any(b => Innen(b, probe) && !jeAussen[b].Any(l => Innen(l, probe)));
                if (liegtIn)
                {
                    vermerke.Add(Grundrissvermerk.Ueberlappung);
                    continue;
                }
                bleibt.Add(a);
            }
            var ergebnis = new List<Grundrissring>();
            foreach (Grundrissring a in bleibt.OrderBy(r => r.PunkteMm[0][0]).ThenBy(r => r.PunkteMm[0][1]))
            {
                ergebnis.Add(a);
                ergebnis.AddRange(jeAussen[a]);
            }
            return ergebnis;
        }

        /// <summary>Ein Punkt knapp innerhalb des Rings: die Mitte der ersten Kante, 1 mm nach innen (links bzw. beim Loch rechts).</summary>
        private static double[] Probepunkt(Grundrissring r)
        {
            long[] a = r.PunkteMm[0], b = r.PunkteMm[1];
            double mx = (a[0] + b[0]) / 2.0, my = (a[1] + b[1]) / 2.0;
            double dx = b[0] - a[0], dy = b[1] - a[1], l = Math.Sqrt(dx * dx + dy * dy);
            double s = r.IstLoch ? -1.0 : 1.0;
            return new[] { mx - s * dy / l, my + s * dx / l };
        }

        /// <summary>Punkt im Polygon (Strahlverfahren).</summary>
        private static bool Innen(Grundrissring r, double[] p)
        {
            bool innen = false;
            IReadOnlyList<long[]> q = r.PunkteMm;
            for (int i = 0, j = q.Count - 1; i < q.Count; j = i++)
            {
                double xi = q[i][0], yi = q[i][1], xj = q[j][0], yj = q[j][1];
                if ((yi > p[1]) != (yj > p[1]) && p[0] < (xj - xi) * (p[1] - yi) / (yj - yi) + xi) innen = !innen;
            }
            return innen;
        }

        /// <summary>Die konvexe Hülle (monotone Kette) gegen den Uhrzeigersinn, ohne kollineare Punkte, Start am kleinsten Punkt.</summary>
        private static List<long[]> Huelle(IEnumerable<long[]> punkte)
        {
            List<long[]> p = punkte.GroupBy(x => (x[0], x[1])).Select(g => g.First())
                                   .OrderBy(x => x[0]).ThenBy(x => x[1]).ToList();
            if (p.Count < 3) return p;
            var unten = new List<long[]>();
            foreach (long[] x in p)
            {
                while (unten.Count >= 2 && Kreuz(unten[^1][0] - unten[^2][0], unten[^1][1] - unten[^2][1],
                                                 x[0] - unten[^2][0], x[1] - unten[^2][1]) <= 0)
                    unten.RemoveAt(unten.Count - 1);
                unten.Add(x);
            }
            var oben = new List<long[]>();
            for (int i = p.Count - 1; i >= 0; i--)
            {
                long[] x = p[i];
                while (oben.Count >= 2 && Kreuz(oben[^1][0] - oben[^2][0], oben[^1][1] - oben[^2][1],
                                                x[0] - oben[^2][0], x[1] - oben[^2][1]) <= 0)
                    oben.RemoveAt(oben.Count - 1);
                oben.Add(x);
            }
            unten.RemoveAt(unten.Count - 1);
            oben.RemoveAt(oben.Count - 1);
            return unten.Concat(oben).ToList();
        }
    }
}
