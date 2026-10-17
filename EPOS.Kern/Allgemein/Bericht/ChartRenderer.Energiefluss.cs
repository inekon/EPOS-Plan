﻿using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Energieflussbild (Sankey)</b> — Konzept Energieflussbild, Abschnitt 3, Variante A „Gestraffte Kette“ (SK-2c):
    /// Strom, Wärme und Kälte eines Jahres als Kette aus so vielen Spalten, wie die Anlage braucht; in jeder Spalte die
    /// Spuren Strom (oben), Gemischt, Wärme, Kälte (unten); Wandler als beschriftete Kästen, Busse als dunkle Balken,
    /// Quellen und Senken als schmale Balken mit Namen außen; Verluste als kurze graue Abzweige ohne Knotenbalken; die
    /// Legende der Energieformen unter dem Bild.
    ///
    /// <para><b>Layout.</b> Spalte = n − 1 − längster Weg zur Senke (jeder Knoten so weit rechts wie möglich, Senken in der
    /// letzten Spalte). Ein Band über mehr als eine Lücke bekommt je Zwischenspalte einen unsichtbaren Wegpunkt seiner Spur
    /// und läuft so nie hinter einem Kasten hindurch. Die Reihenfolge je Spalte: erst nach Spur, darin Schwerpunkt-Relaxation
    /// (<see cref="EF_RELAX_DURCHLAEUFE"/> Durchläufe wie d3-sankey, Umordnung nur innerhalb der Spur), dann ein
    /// Tauschdurchlauf benachbarter Knoten derselben Spur gegen die gewichtete Kreuzungszahl (auch kombiniert mit einem Tausch
    /// in der Nachbarspalte), dann <see cref="EF_RELAX_NACH"/> Durchläufe ohne Umordnung. Alles ist deterministisch.</para>
    ///
    /// <para><b>Ein reines Pixelbild</b> wie Versionsbalken und Kesselkennlinie: keine Zeichenfläche, kein Zoom. Jedes Band
    /// trägt die Marke <c>fluss:&lt;von&gt;:&lt;nach&gt;</c>, jeder Knoten <c>knoten:&lt;Kennung&gt;</c>, beide ihren Zeigertext als
    /// Wert — die Zeigerzeile des Bausteins <c>DiagrammSvg</c> greift ohne Änderung am Baustein. Ein Verlustabzweig trägt
    /// die Marke seines Bandes, auch seine Beschriftung.</para>
    ///
    /// <para><b>Beschriftung</b> (Anwenderentscheid SK-Q12): Am Bildschirm tragen die Knoten nur ihren Namen und die Bänder
    /// keine Zahl — die Werte stehen im Zeigertext. Die Berichtsfassung (<c>knotenwerte</c>) nennt an jedem Knoten seine
    /// Jahressumme in MWh/a (Kästen „Kurzname · Wert“, Verluste „Verlust Wert“); Bänder bleiben ohne Zahl.</para>
    ///
    /// <para><b>Bänder</b> sind geschlossene Pfade: Oberkante durch alle Wegpunkte als abgetastete S-Kurven
    /// y(t) = y0 + (y1 − y0)·(3t² − 2t³) mit <see cref="EF_STUETZSTELLEN"/> Stützstellen je Teilstück, Unterkante zurück;
    /// Füllung in der Rolle mit Deckung 0,9, weißer Saum; benachbarte Bänder in einen Knoten wechseln zwischen zwei Tönungen
    /// (<see cref="EF_BANDDECKUNG"/>, <see cref="EF_BANDDECKUNG_HELL"/>). Das Zeichenmodell kennt keine Bézierkurve, PNG und
    /// SVG zeichnen denselben Polygonzug.</para>
    ///
    /// <para><b>Stufen</b> (Variante B, Anwenderentscheid SK-Q17, SK-2d): vier feste Spalten nach der Knotenart — Einsatz
    /// (Quellen) | Anlagen (Wandler) | Energieform (Busse) | Nutzen (Senken) — mit Titeln aus <see cref="EnergieflussBildDaten.Stufentitel"/>
    /// (Marke <c>stufe:&lt;Spalte&gt;</c>); ein Band aus einem Bus in einen Wandler (Strom an Wärmepumpe, Heizstab, Elektrokessel,
    /// Kälteerzeuger) ist ein <b>Verbinder</b>: am Bus ein Stummel mit Pfeilspitze und Beschriftung „→ Gerät“, am Gerät ein
    /// Stummel mit offenem Kreis und der Kurzform des Busses („Strom“), beide unter derselben Marke und demselben Zeigertext
    /// (<see cref="Verbinderregel"/>). Dieselbe Verbinderregel gilt in der Kette der <b>schmalen Fassung</b> und als Rückfall, wenn
    /// die Kette mehr als <see cref="EF_MAX_SPALTEN_KETTE"/> Spalten bräuchte; dort zählt ein Verbinder nicht in den längsten Weg
    /// zur Senke, zieht keine Knoten zueinander, bekommt keinen Wegpunkt und zählt nicht als Kreuzung.</para>
    ///
    /// <para><b>Formfilter</b> (Alle | Strom | Wärme | Kälte) sind keine Sache des Renderers: <see cref="EnergieflussAbbildung.Gefiltert"/>
    /// liefert je Filter eigene Bilddaten; leere Bilddaten mit <see cref="EnergieflussBildDaten.Hinweis"/> ergeben ein Bild, das
    /// nur den Hinweis trägt (Marke <c>hinweis</c>).</para>
    /// </summary>
    public static partial class ChartRenderer
    {
        /// <summary>Die breite Fassung am Bildschirm [px]: 430 px Bild und die Legende darunter.</summary>
        public static readonly Bildmass ENERGIEFLUSS_BREIT = new Bildmass(1180, 460);

        /// <summary>Die schmale Fassung am Bildschirm [px] (Stilblatt wählt sie unter 900 px Breite): die Kette mit Kurznamen und der Verbinderregel (SK-Q13).</summary>
        public static readonly Bildmass ENERGIEFLUSS_SCHMAL = new Bildmass(680, 560);

        /// <summary>Mehr Spalten bräuchte die Kette nicht: darüber fällt sie auf die Verbinderregel zurück (SK-2d).</summary>
        public const int EF_MAX_SPALTEN_KETTE = 6;

        /// <summary>Die vier festen Spalten der Darstellung Stufen: Einsatz | Anlagen | Energieform | Nutzen.</summary>
        public const int EF_STUFEN_SPALTEN = 4;

        /// <summary>Höchste Länge eines Verbinderstummels [px]; sonst 30 % des Spaltenabstands.</summary>
        public const float EF_STUMMEL_MAX = 90f;

        /// <summary>
        /// Ein ausgehendes Band, dessen Ziel so viel tiefer liegt als seine Mitte am Knoten [px], sinkt in der Länge eines
        /// Stummels (30 % des Weges, Ease 0,216) tiefer als der Stummel (6 px) — die Stummel stehen vor ihm.
        /// </summary>
        private const float EF_STUMMEL_VORRANG = 28f;

        /// <summary>Die Berichtsfassung [px] — Faktor 2 auf 620 × 245 pt.</summary>
        public static readonly Bildmass ENERGIEFLUSS_BERICHT = new Bildmass(1240, 490);

        /// <summary>Unter dieser Breite [px] gelten die Regeln der schmalen Fassung (Kurznamen, kleinere Schrift, engere Lücken).</summary>
        public const int ENERGIEFLUSS_SCHMAL_UNTER = 900;

        /// <summary>Breite eines Balkens (Quelle, Senke, Bus) [px]; Kästen sind so breit wie ihre Beschriftung.</summary>
        public const float EF_KNOTENBREITE = 10f;

        /// <summary>Mindestbreite eines Bandes [px] — ein kleinerer Fluss wird so breit gezeichnet und zählt so in die Knotenhöhe.</summary>
        public const float EF_MINDESTBAND = 2f;

        /// <summary>Ab dieser Höhe [px] ist ein Wandler ein beschrifteter Kasten; darunter ein Balken mit Namen darüber.</summary>
        public const float EF_KASTEN_AB = 15f;

        /// <summary>Stützstellen je Bandkante und Teilstück.</summary>
        public const int EF_STUETZSTELLEN = 24;

        /// <summary>Deckung der Bandflächen: 0,9.</summary>
        public const byte EF_BANDDECKUNG = 230;

        /// <summary>Die zweite Tönung benachbarter Bänder in einen Knoten: 0,65.</summary>
        public const byte EF_BANDDECKUNG_HELL = 165;

        /// <summary>Deckung der Balken von Quellen und Senken (heller als Kästen und Busse).</summary>
        public const byte EF_BALKENDECKUNG = 150;

        /// <summary>Deckung der gestrichelten Bänder „nicht zugeordnet“ und „ungedeckt“.</summary>
        public const byte EF_RESTDECKUNG = 128;

        /// <summary>Stärke des weißen Saums um jedes Band [px].</summary>
        public const float EF_SAUM = 0.8f;

        /// <summary>Höhe der Legende unter dem Bild [px].</summary>
        public const float EF_LEGENDE_HOEHE = 30f;

        /// <summary>Relaxationsdurchläufe mit Umordnung innerhalb der Spur, Tauschdurchläufe, Durchläufe danach ohne Umordnung.</summary>
        public const int EF_RELAX_DURCHLAEUFE = 28, EF_TAUSCH_DURCHLAEUFE = 3, EF_RELAX_NACH = 10;

        /// <summary>Mindesthöhe eines Knotens [px].</summary>
        private const float EF_MIN_KNOTEN = 4f;

        private enum EfArt { Quelle = 0, Wandler = 1, Weg = 2, Bus = 3, Senke = 4 }

        /// <summary>Ein Knoten im Layout (auch ein Wegpunkt).</summary>
        private sealed class EfKnoten
        {
            public EnergieflussBildKnoten Daten;
            public int Index, Spalte, Spur;
            public EfArt Art;
            public float X, Y, W, H;
            public double Zu, Ab;
            public bool Kasten;
            public readonly List<EfBand> Ein = new List<EfBand>(), Aus = new List<EfBand>();
            public readonly List<EfSegment> EinS = new List<EfSegment>(), AusS = new List<EfSegment>();
            public readonly List<EfVerlust> Verl = new List<EfVerlust>();
            public float Mitte => Y + H / 2f;
            public double Menge => Math.Max(Zu, Ab);
            public double Wert => Daten != null && double.IsFinite(Daten.Mwh) ? Daten.Mwh : Menge;
        }

        /// <summary>Ein Band im Layout: ein oder mehrere Teilstücke über Wegpunkte.</summary>
        private sealed class EfBand
        {
            public EnergieflussBildBand Daten;
            public int Index;
            public EfKnoten Von, Nach;
            /// <summary>
            /// Verbinder (Variante B, SK-2d): das Band wird nicht durch das Bild geführt, sondern am Bus als Stummel „→ Gerät“
            /// ausgeleitet und am Gerät als Stummel „Strom“ eingeleitet; es zieht die Knoten nicht zueinander und zählt
            /// nicht als Kreuzung. In der Kette ist kein Band ein Verbinder.
            /// </summary>
            public bool Verbinder;
            public readonly List<EfSegment> Segs = new List<EfSegment>();
        }

        /// <summary>Ein Teilstück eines Bandes zwischen zwei Knoten benachbarter Spalten.</summary>
        private sealed class EfSegment
        {
            public EfBand Band;
            public EfKnoten Von, Nach;
            public float D, Y0, Y1;
            public double Wert => Band.Daten.Mwh;
        }

        /// <summary>Ein Verlustabzweig an seinem Eigentümer.</summary>
        private sealed class EfVerlust
        {
            public EnergieflussBildBand Daten;
            public EnergieflussBildKnoten Knoten;
            public int Index;
            public EfKnoten Von;
            public float D, Y0, X1, Y1;
        }

        private readonly struct EfZeile
        {
            public EfZeile(string text, Schriftmass f, Farbrolle rolle, float x, float y) { Text = text; F = f; Rolle = rolle; X = x; Y = y; }
            public readonly string Text;
            public readonly Schriftmass F;
            public readonly Farbrolle Rolle;
            public readonly float X, Y;
        }

        /// <summary>Das Energieflussbild als PNG (für die Proben und den Bericht).</summary>
        public static byte[] EnergieflussBild(EnergieflussBildDaten daten, Bildmass? mass = null, bool knotenwerte = false,
                                              EnergieflussDarstellung darstellung = EnergieflussDarstellung.Kette)
            => SkiaMaler.Png(EnergieflussModell(daten, mass, knotenwerte, darstellung));

        /// <summary>
        /// <b>Das Zeichenmodell des Energieflussbilds.</b> Ohne <paramref name="mass"/> die breite Fassung
        /// (<see cref="ENERGIEFLUSS_BREIT"/>); unter <see cref="ENERGIEFLUSS_SCHMAL_UNTER"/> Breite gelten die Regeln der
        /// schmalen Fassung. Bänder ohne endlichen positiven Wert oder mit unbekanntem Knoten fallen weg, ebenso Knoten
        /// ohne Band; das Bild ist deterministisch.
        /// </summary>
        /// <param name="daten">Knoten, Bänder und Legende.</param>
        /// <param name="mass">Die Bildgröße; <c>null</c> = breit.</param>
        /// <param name="knotenwerte">Berichtsfassung: jeder Knoten nennt seine Jahressumme (MWh/a). Am Bildschirm
        /// <c>false</c> — dort stehen die Werte nur im Zeigertext.</param>
        /// <param name="darstellung">Die Darstellungsart (SK-Q17): <see cref="EnergieflussDarstellung.Kette"/> (Vorgabe, auch im
        /// Bericht) oder <see cref="EnergieflussDarstellung.Stufen"/> (vier feste Spalten mit Titeln und Verbindern);
        /// <see cref="EnergieflussDarstellung.Zuordnung"/> zeichnet wie die Kette (ihre Bilddaten kommen aus der eigenen Bilanz).</param>
        public static Zeichenmodell EnergieflussModell(EnergieflussBildDaten daten, Bildmass? mass = null, bool knotenwerte = false,
                                                       EnergieflussDarstellung darstellung = EnergieflussDarstellung.Kette)
        {
            int W = Bildmass.BreiteOder(mass, ENERGIEFLUSS_BREIT.Breite);
            int H = Bildmass.HoeheOder(mass, ENERGIEFLUSS_BREIT.Hoehe);
            var z = Modell(W, H);
            bool schmal = W < ENERGIEFLUSS_SCHMAL_UNTER;
            bool stufen = darstellung == EnergieflussDarstellung.Stufen;

            // ---------------------------------------------------------------- Knoten und Bänder sammeln
            var knoten = new Dictionary<string, EfKnoten>(StringComparer.Ordinal);
            var verlustknoten = new Dictionary<string, EnergieflussBildKnoten>(StringComparer.Ordinal);
            var reihenfolge = new List<EfKnoten>();
            IReadOnlyList<EnergieflussBildKnoten> eingabeKnoten = daten?.Knoten ?? Array.Empty<EnergieflussBildKnoten>();
            for (int i = 0; i < eingabeKnoten.Count; i++)
            {
                EnergieflussBildKnoten k = eingabeKnoten[i];
                if (k == null || string.IsNullOrEmpty(k.Kennung) || knoten.ContainsKey(k.Kennung) || verlustknoten.ContainsKey(k.Kennung)) continue;
                if (k.Art == EnergieflussKnotenart.Verlust) { verlustknoten[k.Kennung] = k; continue; }
                var ek = new EfKnoten
                {
                    Daten = k, Index = i, Spur = (int)k.Spur,
                    Art = k.Art == EnergieflussKnotenart.Quelle ? EfArt.Quelle
                        : k.Art == EnergieflussKnotenart.Bus ? EfArt.Bus
                        : k.Art == EnergieflussKnotenart.Senke ? EfArt.Senke
                        : EfArt.Wandler,
                };
                knoten[k.Kennung] = ek;
                reihenfolge.Add(ek);
            }

            var baender = new List<EfBand>();
            var verluste = new List<EfVerlust>();
            IReadOnlyList<EnergieflussBildBand> eingabeBaender = daten?.Baender ?? Array.Empty<EnergieflussBildBand>();
            for (int i = 0; i < eingabeBaender.Count; i++)
            {
                EnergieflussBildBand b = eingabeBaender[i];
                if (b == null || !double.IsFinite(b.Mwh) || b.Mwh <= 0) continue;
                if (b.Von == null || b.Nach == null || b.Von == b.Nach) continue;
                if (!knoten.TryGetValue(b.Von, out EfKnoten von)) continue;   // aus einem Verlustknoten fließt nichts weiter
                if (verlustknoten.TryGetValue(b.Nach, out EnergieflussBildKnoten vk))
                {
                    if (verluste.Any(v => v.Knoten == vk)) continue;           // ein Verlustknoten hat genau ein Band
                    var v = new EfVerlust { Daten = b, Knoten = vk, Index = i, Von = von };
                    verluste.Add(v);
                    von.Verl.Add(v); von.Ab += b.Mwh;
                    continue;
                }
                if (!knoten.TryGetValue(b.Nach, out EfKnoten nach)) continue;
                var eb = new EfBand { Daten = b, Index = i, Von = von, Nach = nach };
                baender.Add(eb);
                von.Aus.Add(eb); von.Ab += b.Mwh;
                nach.Ein.Add(eb); nach.Zu += b.Mwh;
            }
            List<EfKnoten> sichtbar = reihenfolge.Where(k => k.Ein.Count + k.Aus.Count + k.Verl.Count > 0).ToList();
            if (baender.Count == 0)
            {
                // Ein leeres Bild — etwa ein Formfilter ohne Fluss dieser Form — trägt nur seinen Hinweis.
                string hinweis = daten?.Hinweis;
                if (!string.IsNullOrEmpty(hinweis))
                    using (var hf = Schrift(schmal ? 10f : 11f))
                    {
                        float hw = hf.MeasureText(hinweis);
                        z.Markiert("hinweis", zh => Text(zh, hinweis, hf, Farbrolle.TEXT, Math.Max(2f, (W - hw) / 2f), (H - EF_LEGENDE_HOEHE - hf.Hoehe) / 2f));
                    }
                return z;
            }

            // ---------------------------------------------------------------- Spalten und Verbinder
            // Stufen: vier feste Spalten nach der Knotenart. Kette: Spalte = n − 1 − längster Weg zur Senke, ein Verbinder
            // zählt nicht in den Weg. Die Verbinderregel gilt in den Stufen und in der schmalen Fassung — und als Rückfall,
            // wenn die Kette mehr als EF_MAX_SPALTEN_KETTE Spalten bräuchte.
            int Spalten(bool mitVerbinder)
            {
                if (stufen) foreach (EfKnoten k in sichtbar) k.Spalte = StufenSpalte(k.Art);
                foreach (EfBand b in baender) b.Verbinder = Verbinderregel(b, darstellung, mitVerbinder);
                if (stufen) return EF_STUFEN_SPALTEN;
                var tiefe = new Dictionary<EfKnoten, int>();
                var besuch = new HashSet<EfKnoten>();
                int Tiefe(EfKnoten k)
                {
                    if (tiefe.TryGetValue(k, out int t)) return t;
                    if (!besuch.Add(k)) return 0;                               // ein Kreis zählt nicht weiter
                    t = 0;
                    foreach (EfBand b in k.Aus) if (!b.Verbinder) t = Math.Max(t, 1 + Tiefe(b.Nach));
                    tiefe[k] = t;
                    return t;
                }
                int n = 0;
                foreach (EfKnoten k in sichtbar) n = Math.Max(n, Tiefe(k));
                foreach (EfKnoten k in sichtbar) k.Spalte = n - Tiefe(k);
                return n + 1;
            }
            bool verbinder = stufen || schmal;
            int nSp = Spalten(verbinder);
            if (!verbinder && nSp > EF_MAX_SPALTEN_KETTE)
            {
                verbinder = true;
                nSp = Spalten(true);
            }

            // ---------------------------------------------------------------- Wegpunkte
            var wege = new List<EfKnoten>();
            var segmente = new List<EfSegment>();
            EfSegment Seg(EfBand b, EfKnoten von, EfKnoten nach)
            {
                var s = new EfSegment { Band = b, Von = von, Nach = nach };
                segmente.Add(s); von.AusS.Add(s); nach.EinS.Add(s); b.Segs.Add(s);
                return s;
            }
            foreach (EfBand b in baender)
            {
                if (b.Nach.Spalte - b.Von.Spalte <= 1 || b.Verbinder) { Seg(b, b.Von, b.Nach); continue; }
                EfKnoten vorher = b.Von;
                for (int c = b.Von.Spalte + 1; c < b.Nach.Spalte; c++)
                {
                    var w = new EfKnoten { Art = EfArt.Weg, Spur = b.Nach.Spur, Spalte = c, Index = 100000 + wege.Count, Zu = b.Daten.Mwh, Ab = b.Daten.Mwh };
                    wege.Add(w);
                    Seg(b, vorher, w);
                    vorher = w;
                }
                Seg(b, vorher, b.Nach);
            }
            List<EfKnoten> alle = sichtbar.Concat(wege).ToList();
            var spalten = new List<EfKnoten>[nSp];
            for (int c = 0; c < nSp; c++) spalten[c] = new List<EfKnoten>();
            foreach (EfKnoten k in alle) spalten[k.Spalte].Add(k);
            float ArtRang(EfArt a) => a == EfArt.Weg ? 1.5f : (int)a > (int)EfArt.Weg ? (int)a - 1 : (int)a;
            for (int c = 0; c < nSp; c++)
                spalten[c] = spalten[c].OrderBy(k => k.Spur).ThenBy(k => ArtRang(k.Art)).ThenBy(k => k.Index).ToList();

            // ---------------------------------------------------------------- Geometrie und Maßstab
            float randL = schmal ? 92f : 130f, randR = schmal ? 92f : 150f;
            float oben = stufen ? (schmal ? 46f : 52f) : (schmal ? 30f : 34f), unten = EF_LEGENDE_HOEHE + (schmal ? 24f : 30f);
            float luecke = schmal ? 14f : 20f, spurabstand = schmal ? 18f : 30f;
            float gap = (W - randL - randR - EF_KNOTENBREITE) / Math.Max(1, nSp - 1);
            float X(int sp) => randL + sp * gap;
            float nutz = H - oben - unten;
            float Abstand(EfKnoten a, EfKnoten b)
                => ((a.Art == EfArt.Weg || b.Art == EfArt.Weg) ? luecke * 0.5f : luecke) + (a.Spur != b.Spur ? spurabstand : 0f);
            float Luecken(List<EfKnoten> s)
            {
                float a = 0f;
                for (int i = 0; i < s.Count - 1; i++) a += Abstand(s[i], s[i + 1]);
                return a;
            }
            double m = double.PositiveInfinity;
            foreach (List<EfKnoten> s in spalten)
            {
                double summe = s.Sum(k => k.Menge);
                if (summe > 0) m = Math.Min(m, (nutz - Luecken(s)) / summe);
            }
            if (!double.IsFinite(m) || m <= 0) m = 1.0;
            float Dicke(double mwh) => Math.Max((float)(mwh * m), EF_MINDESTBAND);
            float Hoehe(EfKnoten k) => Math.Max(k.Art == EfArt.Weg ? 1f : EF_MIN_KNOTEN,
                Math.Max(k.EinS.Sum(s => Dicke(s.Wert)), k.AusS.Sum(s => Dicke(s.Wert)) + k.Verl.Sum(v => Dicke(v.Daten.Mwh))));
            for (int versuch = 0; versuch < 80; versuch++)
            {
                float groesste = spalten.Max(s => s.Sum(Hoehe) + Luecken(s));
                if (groesste <= nutz + 0.01f) break;
                m *= 0.97;
            }

            using (var tf = Schrift(schmal ? 10f : 11f, true))
            using (var lf = Schrift(schmal ? 10f : 11f))
            using (var vf = Schrift(schmal ? 9f : 10f))
            using (var xf = Schrift(schmal ? 8.5f : 9.5f))
            {
                string Kastentext(EfKnoten k) => KnotenName(k, true) + (knotenwerte ? " · " + EfZahl(k.Wert) : "");

                // Knotenbreite: Wandler als beschriftete Kästen (Kurzname innen), Busse, Quellen und Senken schmale Balken,
                // Wegpunkte breitenlos in der Mitte der Spalte.
                foreach (EfKnoten k in alle)
                {
                    k.H = Hoehe(k);
                    k.Kasten = k.Art == EfArt.Wandler && k.H >= EF_KASTEN_AB;
                    k.W = k.Art == EfArt.Weg ? 0f : k.Kasten ? Math.Max(EF_KNOTENBREITE, tf.MeasureText(Kastentext(k)) + 14f) : EF_KNOTENBREITE;
                    k.X = X(k.Spalte) - (k.W - EF_KNOTENBREITE) / 2f + (k.Art == EfArt.Weg ? EF_KNOTENBREITE / 2f : 0f);
                }

                // ------------------------------------------------------------ Reihenfolge: Relaxation und Tausch
                foreach (List<EfKnoten> s in spalten)
                {
                    float ges = s.Sum(k => k.H) + Luecken(s);
                    float y = oben + (nutz - ges) / 2f;
                    for (int i = 0; i < s.Count; i++)
                    {
                        s[i].Y = y;
                        y += s[i].H + (i < s.Count - 1 ? Abstand(s[i], s[i + 1]) : 0f);
                    }
                }
                float? Ziel(EfKnoten k, bool ein)
                {
                    double summe = 0, w = 0;
                    foreach (EfSegment s in ein ? k.EinS : k.AusS)
                    {
                        if (s.Band.Verbinder) continue;
                        EfKnoten o = ein ? s.Von : s.Nach;
                        summe += s.Wert * o.Mitte; w += s.Wert;
                    }
                    return w > 0 ? (float)(summe / w) : (float?)null;
                }
                void Ordne(int c, bool sortieren)
                {
                    List<EfKnoten> s = spalten[c];
                    if (sortieren) spalten[c] = s = s.OrderBy(k => k.Spur).ThenBy(k => k.Mitte).ThenBy(k => k.Index).ToList();
                    for (int i = 0; i < s.Count; i++)
                    {
                        float minY = i == 0 ? oben : s[i - 1].Y + s[i - 1].H + Abstand(s[i - 1], s[i]);
                        if (s[i].Y < minY) s[i].Y = minY;
                    }
                    for (int i = s.Count - 1; i >= 0; i--)
                    {
                        float maxY = i == s.Count - 1 ? oben + nutz - s[i].H : s[i + 1].Y - Abstand(s[i], s[i + 1]) - s[i].H;
                        if (s[i].Y > maxY) s[i].Y = maxY;
                    }
                }
                void Relax(int durchlaeufe, bool sortieren)
                {
                    for (int it = 0; it < durchlaeufe; it++)
                    {
                        float alpha = 0.6f * (float)Math.Pow(0.92, it);
                        for (int c = 0; c < nSp; c++)
                        {
                            foreach (EfKnoten k in spalten[c]) { float? t = Ziel(k, true); if (t.HasValue) k.Y += (t.Value - k.Mitte) * alpha; }
                            Ordne(c, sortieren);
                        }
                        for (int c = nSp - 1; c >= 0; c--)
                        {
                            foreach (EfKnoten k in spalten[c]) { float? t = Ziel(k, false); if (t.HasValue) k.Y += (t.Value - k.Mitte) * alpha; }
                            Ordne(c, sortieren);
                        }
                    }
                }
                Relax(EF_RELAX_DURCHLAEUFE, true);

                // Gewichtete Kreuzungen: je Spalte die Paare ausgehender Teilstücke, deren Reihenfolge am Ziel kippt.
                double Kreuzungen()
                {
                    double k = 0;
                    foreach (List<EfKnoten> s in spalten)
                    {
                        var li = new List<(int P, float Key, double W)>();
                        for (int i = 0; i < s.Count; i++)
                            foreach (EfSegment seg in s[i].AusS) if (!seg.Band.Verbinder) li.Add((i, seg.Nach.Mitte, seg.Wert));
                        for (int i = 0; i < li.Count; i++)
                            for (int j = i + 1; j < li.Count; j++)
                                if ((li[i].P - li[j].P) * (li[i].Key - li[j].Key) < 0) k += li[i].W * li[j].W;
                    }
                    return k;
                }
                void Tausch(int c, int i)
                {
                    List<EfKnoten> s = spalten[c];
                    List<float> ys = s.Select(k => k.Y).ToList();
                    (s[i], s[i + 1]) = (s[i + 1], s[i]);
                    for (int j = 0; j < s.Count; j++) s[j].Y = ys[j];
                    Ordne(c, false);
                }
                List<int> Paare(int c)
                {
                    var r = new List<int>();
                    for (int i = 0; i < spalten[c].Count - 1; i++) if (spalten[c][i].Spur == spalten[c][i + 1].Spur) r.Add(i);
                    return r;
                }
                for (int sweep = 0; sweep < EF_TAUSCH_DURCHLAEUFE; sweep++)
                    for (int c = 0; c < nSp; c++)
                        foreach (int i in Paare(c))
                        {
                            double vor = Kreuzungen();
                            Tausch(c, i);
                            if (Kreuzungen() < vor - 1e-9) continue;
                            bool besser = false;
                            foreach (int c2 in new[] { c - 1, c + 1 })
                            {
                                if (c2 < 0 || c2 >= nSp || besser) continue;
                                foreach (int j in Paare(c2))
                                {
                                    Tausch(c2, j);
                                    if (Kreuzungen() < vor - 1e-9) { besser = true; break; }
                                    Tausch(c2, j);
                                }
                            }
                            if (!besser) Tausch(c, i);
                        }
                Relax(EF_RELAX_NACH, false);

                // ------------------------------------------------------------ Bandlagen
                foreach (EfSegment s in segmente) s.D = Dicke(s.Wert);
                foreach (EfVerlust v in verluste) v.D = Dicke(v.Daten.Mwh);
                // Ein Verlustabzweig sinkt über seine Länge um max(4 px; 0,35 · Dicke); er zielt so weit unter den Fuß seines
                // Knotens, dass ein Band vor ihm (über ihm) auf dieser Länge höchstens ebenso tief sinkt und ein Band nach ihm
                // tiefer — sonst kreuzte er eines von beiden. Die Spaltenlücke bestimmt, wie schnell ein Band sinkt (Ease).
                float verlustLaenge = Math.Min(44f, gap * 0.26f);
                float Ease(float t) => 3f * t * t - 2f * t * t * t;
                float Senkung(EfVerlust v) => Math.Max(4f, v.D * 0.35f);
                float VerlustZiel(EfKnoten k, EfVerlust v) => k.Y + k.H + Math.Max(12f, Senkung(v) / Math.Max(0.02f, Ease(verlustLaenge / Math.Max(1f, gap))));
                // Ausgehend nach der Lage des Ziels; ein Verlustabzweig nach seinem Ziel unter dem Fuß des Knotens und liegt so
                // über den Bändern, die weiter nach unten gehen (sonst kreuzte er sie). Die Verbinderstummel laufen fast
                // waagerecht nach rechts: Sie stehen vor dem ersten Band, das in der Länge eines Stummels tiefer sinkt, als
                // der Stummel selbst — spätestens vor den Verlusten —, sonst liefe dieses Band durch sie hindurch.
                // Eingehend nach der Lage der Quelle.
                foreach (EfKnoten k in alle)
                {
                    float y = k.Y;
                    var aus = k.AusS.Where(s => !s.Band.Verbinder).Select(s => (Key: s.Nach.Mitte, Index: s.Band.Index, Seg: s, Verl: (EfVerlust)null))
                               .Concat(k.Verl.Select(v => (Key: VerlustZiel(k, v), Index: 1000000 + v.Index, Seg: (EfSegment)null, Verl: v)))
                               .OrderBy(e => e.Key).ThenBy(e => e.Index);
                    List<EfSegment> verbinderAus = k.AusS.Where(s => s.Band.Verbinder).OrderBy(s => s.Nach.Mitte).ThenBy(s => s.Band.Index).ToList();
                    bool eingefuegt = verbinderAus.Count == 0;
                    void Stummel() { foreach (EfSegment s in verbinderAus) { s.Y0 = y; y += s.D; } eingefuegt = true; }
                    foreach (var e in aus)
                    {
                        float d = e.Seg != null ? e.Seg.D : e.Verl.D;
                        if (!eingefuegt && (e.Verl != null || e.Key - (y + d / 2f) > EF_STUMMEL_VORRANG)) Stummel();
                        if (e.Seg != null) { e.Seg.Y0 = y; y += e.Seg.D; }
                        else { e.Verl.Y0 = y; y += e.Verl.D; }
                    }
                    if (!eingefuegt) Stummel();
                    y = k.Y;
                    foreach (EfSegment s in k.EinS.OrderBy(s => s.Von.Mitte).ThenBy(s => s.Band.Index)) { s.Y1 = y; y += s.D; }
                }
                // Tönung: Bänder in einen Knoten wechseln nach ihrer Lage zwischen Grund- und hellerem Ton.
                var hell = new HashSet<EfBand>();
                foreach (EfKnoten k in sichtbar)
                {
                    int i = 0;
                    foreach (EfSegment s in k.EinS.OrderBy(s => s.Y1).ThenBy(s => s.Band.Index)) if (i++ % 2 == 1) hell.Add(s.Band);
                }
                foreach (EfVerlust v in verluste)
                {
                    v.X1 = v.Von.X + v.Von.W + verlustLaenge;
                    v.Y1 = Math.Min(v.Y0 + Senkung(v), oben + nutz - v.D);                 // sanfter Bogen: kreuzt kein Band, das tiefer geht
                }

                // ------------------------------------------------------------ Beschriftung planen
                // Eine Beschriftung, die eine gesetzte überdeckte, weicht in Schritten nach unten bzw. oben aus; Kästen sind
                // belegt; größere Knoten zuerst, Verluste zuletzt.
                var belegt = new List<Rahmen>();
                float schritt = lf.Hoehe + 2f;
                float Platz(float x, float y, float w, float h, bool lieberOben)
                {
                    float[] folge = lieberOben ? new[] { 0f, -1f, 1f, -2f, 2f, -3f, 3f } : new[] { 0f, 1f, -1f, 2f, -2f, 3f, -3f };
                    foreach (float f in folge)
                    {
                        var r = new Rahmen(x, y + f * schritt, w, h);
                        if (r.Y < 0f || r.Unten > H - EF_LEGENDE_HOEHE) continue;
                        if (!belegt.Any(o => Ueberlappt(r, o))) { belegt.Add(r); return r.Y; }
                    }
                    belegt.Add(new Rahmen(x, y, w, h));
                    return y;
                }
                foreach (EfKnoten k in sichtbar) belegt.Add(new Rahmen(k.X, k.Y, k.W, k.H));   // Kästen und Balken sind belegt

                var zeilen = new Dictionary<EfKnoten, List<EfZeile>>();
                foreach (EfKnoten k in sichtbar.OrderByDescending(k => k.H).ThenBy(k => k.Index))
                {
                    var liste = new List<EfZeile>();
                    string name = KnotenName(k, schmal);
                    string zahl = EfZahl(k.Wert);
                    if (k.Art == EfArt.Quelle || k.Art == EfArt.Senke)
                    {
                        bool links = k.Art == EfArt.Quelle;
                        float platz = (links ? randL : randR) - 12f;
                        // Passt der Name nicht in den Rand, steht die Kurzform, bevor gekürzt wird.
                        if (lf.MeasureText(name) > platz && !string.IsNullOrEmpty(k.Daten.Kurzname) && lf.MeasureText(k.Daten.Kurzname) < lf.MeasureText(name))
                            name = k.Daten.Kurzname;
                        string t = Gekuerzt(name, lf, platz);
                        string v = knotenwerte ? Gekuerzt(zahl + " MWh/a", vf, platz) : null;
                        float wT = lf.MeasureText(t), wV = v != null ? vf.MeasureText(v) : 0f;
                        float hoehe = lf.Hoehe + (v != null ? vf.Hoehe : 0f);
                        float xT = links ? k.X - 7f - wT : k.X + k.W + 7f;
                        float xV = links ? k.X - 7f - wV : k.X + k.W + 7f;
                        float y = Platz(Math.Min(xT, xV), k.Mitte - hoehe / 2f, Math.Max(wT, wV) + 4f, hoehe, false);
                        liste.Add(new EfZeile(t, lf, Farbrolle.TEXT, xT, y));
                        if (v != null) liste.Add(new EfZeile(v, vf, Farbrolle.TEXT, xV, y + lf.Hoehe));
                    }
                    else if (k.Kasten)
                    {
                        string t = Kastentext(k);
                        liste.Add(new EfZeile(t, tf, Farbrolle.HINTERGRUND, k.X + k.W / 2f - tf.MeasureText(t) / 2f, k.Mitte - tf.Hoehe / 2f));
                    }
                    else
                    {
                        Schriftmass f = k.Art == EfArt.Bus ? tf : lf;
                        string t = Gekuerzt((k.Art == EfArt.Bus ? name : KnotenName(k, true)) + (knotenwerte ? " · " + zahl : ""), f, Math.Max(60f, gap * 1.4f));
                        float w = f.MeasureText(t);
                        float x = Math.Max(2f, Math.Min(W - w - 2f, k.X + k.W / 2f - w / 2f));
                        float y = Platz(x, k.Y - 4f - f.Hoehe, w + 4f, f.Hoehe, true);
                        liste.Add(new EfZeile(t, f, Farbrolle.TEXT, x, y));
                    }
                    zeilen[k] = liste;
                }
                // Verbinder: „→ Gerät“ rechts der Pfeilspitze am Bus, die Kurzform des Busses links des Kreises am Gerät. Die
                // Stummel fächern zu ihren Beschriftungen auf (von oben nach unten geplant): Das Ende des Stummels liegt in der
                // Mitte seiner Zeile, so bleiben auch drei dünne Stummel an einem Bus unterscheidbar.
                float stummel = Math.Min(EF_STUMMEL_MAX, gap * 0.3f);
                var stummelzeilen = new Dictionary<EfBand, (EfZeile Aus, EfZeile Ein, float YA, float YE)>();
                foreach (EfBand b in baender.Where(b => b.Verbinder).OrderBy(b => b.Segs[0].Y0).ThenBy(b => b.Index))
                {
                    EfSegment s = b.Segs[0];
                    string wert = knotenwerte ? " " + EfZahl(b.Daten.Mwh) : "";
                    string tA = "→ " + KnotenName(b.Nach, schmal) + wert, tE = KnotenName(b.Von, true) + wert;
                    float wA = xf.MeasureText(tA), wE = xf.MeasureText(tE);
                    float xA = s.Von.X + s.Von.W + stummel + 11f, xE = s.Nach.X - stummel - 12f - wE;
                    float yA = Platz(xA, s.Y0 + 6f + s.D / 2f - xf.Hoehe / 2f, wA + 4f, xf.Hoehe, false);
                    float yE = Platz(xE, s.Y1 - 6f + s.D / 2f - xf.Hoehe / 2f, wE + 4f, xf.Hoehe, true);
                    stummelzeilen[b] = (new EfZeile(tA, xf, Farbrolle.TEXT, xA, yA), new EfZeile(tE, xf, Farbrolle.TEXT, xE, yE),
                                        yA + xf.Hoehe / 2f, yE + xf.Hoehe / 2f);
                }
                var verlustzeilen = new Dictionary<EfVerlust, EfZeile>();
                foreach (EfVerlust v in verluste.OrderByDescending(v => v.D).ThenBy(v => v.Index))
                {
                    string name = string.IsNullOrEmpty(v.Knoten.Name) ? v.Knoten.Kennung : v.Knoten.Name;
                    string t = name + (knotenwerte ? " " + EfZahl(v.Daten.Mwh) : "");
                    float w = xf.MeasureText(t);
                    float x = Math.Min(v.X1 + 2f, W - w - 2f);
                    float y = Platz(x, v.Y1 + v.D + 2f, w + 4f, xf.Hoehe, false);
                    verlustzeilen[v] = new EfZeile(t, xf, Farbrolle.TEXT, x, y);
                }

                // ------------------------------------------------------------ Bänder
                var saum = Stift(Farbrolle.HINTERGRUND, EF_SAUM);
                foreach (EfBand b in baender.OrderBy(b => b.Verbinder ? 1 : 0).ThenBy(b => b.Index))   // Verbinder zuletzt: kein Band überdeckt ihre Beschriftung
                {
                    EnergieflussBildBand d = b.Daten;
                    Farbrolle rolle = d.Rolle ?? (d.Art == EnergieflussBandart.Verlust ? Farbrolle.VERLUST
                                                : d.Art == EnergieflussBandart.NichtZugeordnet ? Farbrolle.REST
                                                : Farbrolle.UNBENANNT);
                    bool rest = d.Art == EnergieflussBandart.NichtZugeordnet;
                    Zeichnung.Stift rand = rest ? Stift(rolle, 1f, new Strichmuster(4f, 3f)) : saum;
                    byte deckung = rest ? EF_RESTDECKUNG : hell.Contains(b) ? EF_BANDDECKUNG_HELL : EF_BANDDECKUNG;
                    var fuellung = Flaeche(Farbton.Aus(rolle).MitDeckung(deckung));
                    string wert = d.Zeigertext ?? (KnotenName(b.Von, false) + " → " + KnotenName(b.Nach, false) + ": " + EfZahl(d.Mwh) + " MWh/a");
                    string marke = "fluss:" + b.Von.Daten.Kennung + ":" + b.Nach.Daten.Kennung;
                    if (b.Verbinder)
                    {
                        // Zwei Stummel unter einer Marke: am Bus nach rechts mit Pfeilspitze, am Gerät von links mit
                        // offenem Kreis; der Zeigertext verbindet beide Hälften.
                        EfSegment s = b.Segs[0];
                        var (zA, zE, ya, ye) = stummelzeilen[b];
                        float x0 = s.Von.X + s.Von.W, x1 = s.Nach.X;
                        var aus = new List<Punkt>(2 * EF_STUETZSTELLEN);
                        Kurve(aus, x0, s.Y0, x0 + stummel, ya - s.D / 2f);
                        Kurve(aus, x0 + stummel, ya + s.D / 2f, x0, s.Y0 + s.D);
                        var pfeil = new List<Punkt> { new Punkt(x0 + stummel, ya - 5f), new Punkt(x0 + stummel + 8f, ya), new Punkt(x0 + stummel, ya + 5f) };
                        var ein = new List<Punkt>(2 * EF_STUETZSTELLEN);
                        Kurve(ein, x1 - stummel, ye - s.D / 2f, x1, s.Y1);
                        Kurve(ein, x1, s.Y1 + s.D, x1 - stummel, ye + s.D / 2f);
                        var ring = Stift(rolle, 2f);
                        z.Markiert(marke, wert, zb =>
                        {
                            zb.Pfad(aus, true, rand, fuellung);
                            zb.Pfad(pfeil, true, null, fuellung);
                            Text(zb, zA.Text, zA.F, zA.Rolle, zA.X, zA.Y);
                            zb.Pfad(ein, true, rand, fuellung);
                            zb.Kreis(x1 - stummel - 5f, ye, 4f, ring, Flaeche(Farbrolle.HINTERGRUND));
                            Text(zb, zE.Text, zE.F, zE.Rolle, zE.X, zE.Y);
                        });
                        continue;
                    }
                    var punkte = new List<Punkt>(2 * EF_STUETZSTELLEN * b.Segs.Count);
                    foreach (EfSegment s in b.Segs) Kurve(punkte, s.Von.X + s.Von.W, s.Y0, s.Nach.X, s.Y1);
                    for (int i = b.Segs.Count - 1; i >= 0; i--)
                    {
                        EfSegment s = b.Segs[i];
                        Kurve(punkte, s.Nach.X, s.Y1 + s.D, s.Von.X + s.Von.W, s.Y0 + s.D);
                    }
                    z.Markiert(marke, wert, zb => zb.Pfad(punkte, true, rand, fuellung));
                }

                // ------------------------------------------------------------ Verlustabzweige
                foreach (EfVerlust v in verluste)
                {
                    float x0 = v.Von.X + v.Von.W;
                    var punkte = new List<Punkt>(2 * EF_STUETZSTELLEN);
                    Kurve(punkte, x0, v.Y0, v.X1, v.Y1);
                    Kurve(punkte, v.X1, v.Y1 + v.D, x0, v.Y0 + v.D);
                    Farbrolle rolle = v.Daten.Rolle ?? Farbrolle.VERLUST;
                    var fuellung = Flaeche(Farbton.Aus(rolle).MitDeckung(EF_BANDDECKUNG));
                    string vName = string.IsNullOrEmpty(v.Knoten.Name) ? v.Knoten.Kennung : v.Knoten.Name;
                    string wert = v.Daten.Zeigertext ?? (KnotenName(v.Von, false) + " → " + vName + ": " + EfZahl(v.Daten.Mwh) + " MWh/a");
                    EfZeile zeile = verlustzeilen[v];
                    z.Markiert("fluss:" + v.Von.Daten.Kennung + ":" + v.Knoten.Kennung, wert, zv =>
                    {
                        zv.Pfad(punkte, true, saum, fuellung);
                        Text(zv, zeile.Text, zeile.F, zeile.Rolle, zeile.X, zeile.Y);
                    });
                }

                // ------------------------------------------------------------ Knoten
                foreach (EfKnoten k in sichtbar)
                {
                    Farbton ton = Farbton.Aus(k.Daten.Rolle ?? Farbrolle.KNOTEN);
                    if (k.Art == EfArt.Quelle || k.Art == EfArt.Senke) ton = ton.MitDeckung(EF_BALKENDECKUNG);
                    var fuellung = Flaeche(ton);
                    string wert = k.Daten.Zeigertext ?? (KnotenName(k, false) + ": " + EfZahl(k.Wert) + " MWh/a");
                    zeilen.TryGetValue(k, out List<EfZeile> liste);
                    z.Markiert("knoten:" + k.Daten.Kennung, wert, zk =>
                    {
                        zk.Rechteck(k.X, k.Y, k.W, k.H, null, fuellung);
                        if (liste != null)
                            foreach (EfZeile t in liste) Text(zk, t.Text, t.F, t.Rolle, t.X, t.Y);
                    });
                }

                // ------------------------------------------------------------ Stufentitel
                if (stufen)
                {
                    IReadOnlyList<string> titel = daten.Stufentitel ?? Array.Empty<string>();
                    for (int c = 0; c < nSp && c < titel.Count; c++)
                    {
                        string t = titel[c];
                        if (string.IsNullOrEmpty(t)) continue;
                        float w = tf.MeasureText(t);
                        float x = Math.Max(2f, Math.Min(W - w - 2f, X(c) + EF_KNOTENBREITE / 2f - w / 2f));
                        z.Markiert("stufe:" + c, zt => Text(zt, t, tf, Farbrolle.TEXT, x, 8f));
                    }
                }

                // ------------------------------------------------------------ Legende
                IReadOnlyList<EnergieflussBildLegende> legende = daten.Legende ?? Array.Empty<EnergieflussBildLegende>();
                float lx = 16f, ly = H - EF_LEGENDE_HOEHE + (EF_LEGENDE_HOEHE - vf.Hoehe) / 2f;
                foreach (EnergieflussBildLegende e in legende)
                {
                    if (e == null || e.Rolle == null) continue;
                    string t = e.Text ?? "";
                    float w = vf.MeasureText(t);
                    if (lx + 19f + w > W - 8f) break;
                    float x = lx, feldH = Math.Max(8f, vf.Hoehe - 4f);
                    Zeichnung.Stift rand = e.Gestrichelt ? Stift(e.Rolle, 1f, new Strichmuster(3f, 2f)) : null;
                    var fuellung = Flaeche(Farbton.Aus(e.Rolle).MitDeckung(e.Gestrichelt ? EF_RESTDECKUNG : EF_BANDDECKUNG));
                    z.Markiert("legende:" + e.Rolle.Name.ToLowerInvariant(), zl =>
                    {
                        zl.Rechteck(x, ly + (vf.Hoehe - feldH) / 2f, 14f, feldH, rand, fuellung);
                        Text(zl, t, vf, Farbrolle.TEXT, x + 19f, ly);
                    });
                    lx += 19f + w + 18f;
                }
            }
            return z;
        }

        /// <summary>
        /// <b>Die Verbinderregel</b> (Variante B, Konzept 3.1, SK-Q13/SK-Q17): Ob ein Band als Verbinder statt als
        /// durchgehendes Band gezeichnet wird. Nur wenn <paramref name="verbinder"/> gilt (Stufen, schmale Fassung, Rückfall
        /// der Kette über <see cref="EF_MAX_SPALTEN_KETTE"/> Spalten): ein Band aus einem Bus in einen Wandler (Strom an
        /// Wärmepumpe, Heizstab, Elektrokessel, Kälteerzeuger) — und in den Stufen jedes Band, das nicht nach rechts liefe.
        /// Die Naht sitzt hier, damit Spalten, Layout (Ziel, Kreuzungen, Wegpunkte) und Zeichnung sie gemeinsam nutzen.
        /// </summary>
        private static bool Verbinderregel(EfBand b, EnergieflussDarstellung darstellung, bool verbinder)
        {
            if (!verbinder) return false;
            if (b.Von.Art == EfArt.Bus && b.Nach.Art == EfArt.Wandler) return true;
            return darstellung == EnergieflussDarstellung.Stufen && b.Nach.Spalte <= b.Von.Spalte;
        }

        /// <summary>Die feste Spalte eines Knotens in den Stufen: Einsatz 0, Anlagen 1, Energieform 2, Nutzen 3.</summary>
        private static int StufenSpalte(EfArt art)
            => art == EfArt.Quelle ? 0 : art == EfArt.Wandler ? 1 : art == EfArt.Bus ? 2 : 3;

        /// <summary>Die abgetastete S-Kurve von (x0, y0) nach (x1, y1) mit <see cref="EF_STUETZSTELLEN"/> Punkten, angehängt an <paramref name="punkte"/>.</summary>
        private static void Kurve(List<Punkt> punkte, float x0, float y0, float x1, float y1)
        {
            for (int i = 0; i < EF_STUETZSTELLEN; i++)
            {
                float s = i / (float)(EF_STUETZSTELLEN - 1);
                float e = 3f * s * s - 2f * s * s * s;
                punkte.Add(new Punkt(x0 + (x1 - x0) * s, y0 + (y1 - y0) * e));
            }
        }

        /// <summary>
        /// <b>Die Kreuzungszahl eines gezeichneten Energieflussbilds</b> (Messgröße, Konzept 3.1): die Zahl der Paare von
        /// Bändern (Marke <c>fluss:</c>), deren Mittellinien sich schneiden. Die Mittellinie ist der Mittelwert aus Ober- und
        /// Unterkante des Polygons; zwei Bänder, die nur in einem Knoten aneinanderstoßen, zählen nicht. Ein Band ist genau
        /// ein Pfad; eine Marke mit mehreren Pfaden ist ein Verbinder (Stummel und Pfeilspitze) und zählt nicht.
        /// </summary>
        public static int EnergieflussKreuzungen(Zeichenmodell modell) => EnergieflussKreuzungspaare(modell).Count;

        /// <summary>Die Paare sich schneidender Bänder (Marken), die <see cref="EnergieflussKreuzungen(Zeichenmodell)"/> zählt.</summary>
        public static IReadOnlyList<(string A, string B)> EnergieflussKreuzungspaare(Zeichenmodell modell)
        {
            var paare = new List<(string A, string B)>();
            if (modell == null) return paare;
            var pfade = modell.Befehle.OfType<Pfad>().Where(p => p.Marke != null && p.Marke.StartsWith("fluss:", StringComparison.Ordinal)).ToList();
            var jeMarke = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (Pfad p in pfade) jeMarke[p.Marke] = jeMarke.TryGetValue(p.Marke, out int c) ? c + 1 : 1;
            var linien = new List<(string Marke, Punkt[] Mitte)>();
            foreach (Pfad p in pfade)
            {
                if (jeMarke[p.Marke] > 1) continue;                            // ein Verbinder zählt nicht
                int n = p.Punkte.Count / 2;
                if (n < 2) continue;
                var mitte = new Punkt[n];
                for (int i = 0; i < n; i++)
                    mitte[i] = new Punkt((p.Punkte[i].X + p.Punkte[2 * n - 1 - i].X) / 2f, (p.Punkte[i].Y + p.Punkte[2 * n - 1 - i].Y) / 2f);
                linien.Add((p.Marke, mitte));
            }
            for (int a = 0; a < linien.Count; a++)
                for (int b = a + 1; b < linien.Count; b++)
                    if (Schneiden(linien[a].Mitte, linien[b].Mitte)) paare.Add((linien[a].Marke, linien[b].Marke));
            return paare;
        }

        /// <summary>Dieselbe Kreuzungszahl für Bilddaten in einer Fassung und Darstellung.</summary>
        public static int EnergieflussKreuzungen(EnergieflussBildDaten daten, Bildmass? mass = null, bool knotenwerte = false,
                                                 EnergieflussDarstellung darstellung = EnergieflussDarstellung.Kette)
            => EnergieflussKreuzungen(EnergieflussModell(daten, mass, knotenwerte, darstellung));

        private static bool Schneiden(Punkt[] a, Punkt[] b)
        {
            float minAx = a.Min(p => p.X), maxAx = a.Max(p => p.X), minBx = b.Min(p => p.X), maxBx = b.Max(p => p.X);
            if (maxAx <= minBx || maxBx <= minAx) return false;
            for (int i = 0; i < a.Length - 1; i++)
                for (int j = 0; j < b.Length - 1; j++)
                    if (Schneiden(a[i], a[i + 1], b[j], b[j + 1])) return true;
            return false;
        }

        /// <summary>Echter Schnitt zweier Strecken (Berührung in einem Endpunkt zählt nicht).</summary>
        private static bool Schneiden(Punkt p1, Punkt p2, Punkt q1, Punkt q2)
        {
            float d1 = Kreuz(q1, q2, p1), d2 = Kreuz(q1, q2, p2), d3 = Kreuz(p1, p2, q1), d4 = Kreuz(p1, p2, q2);
            return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
        }

        private static float Kreuz(Punkt a, Punkt b, Punkt c) => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

        /// <summary>Name eines Knotens, kurz die Kurzform (sonst der Name).</summary>
        private static string KnotenName(EfKnoten k, bool kurz)
        {
            EnergieflussBildKnoten d = k.Daten;
            if (d == null) return "";
            if (kurz && !string.IsNullOrEmpty(d.Kurzname)) return d.Kurzname;
            return string.IsNullOrEmpty(d.Name) ? d.Kennung : d.Name;
        }

        /// <summary>Eine Jahressumme mit einer Nachkommastelle in der Zahlkultur des Renderers.</summary>
        private static string EfZahl(double mwh)
            => (double.IsFinite(mwh) ? mwh : 0.0).ToString("N1", Zahlkultur);

        /// <summary>Überdecken sich zwei Rahmen?</summary>
        private static bool Ueberlappt(Rahmen a, Rahmen b)
            => a.X < b.Rechts && b.X < a.Rechts && a.Y < b.Unten && b.Y < a.Unten;
    }
}
