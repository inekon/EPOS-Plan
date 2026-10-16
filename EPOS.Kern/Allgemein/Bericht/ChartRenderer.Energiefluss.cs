using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Das Energieflussbild (Sankey)</b> — Konzept Energieflussbild, Abschnitt 3: Strom, Wärme und Kälte eines Jahres in
    /// sieben Spalten (Quellen, Wandler, Bus Strom, Wandler, Busse Wärme und Kälte, Speicher, Senken), die Bänder als
    /// S-Kurven, Verluste in schmalen Knoten in der Lücke neben ihrem Erzeuger.
    ///
    /// <para><b>Ein reines Pixelbild</b> wie Versionsbalken und Kesselkennlinie: keine Zeichenfläche, kein Zoom, keine
    /// Legende. Jedes Band trägt die Marke <c>fluss:&lt;von&gt;:&lt;nach&gt;</c>, jeder Knoten <c>knoten:&lt;Kennung&gt;</c>, beide
    /// ihren Zeigertext als Wert — die Zeigerzeile des Bausteins <c>DiagrammSvg</c> greift ohne Änderung am Baustein.</para>
    ///
    /// <para><b>Beschriftung</b> (Anwenderentscheid SK-Q12): Am Bildschirm tragen die Knoten nur ihren Namen und die Bänder
    /// keine Zahl — die Werte stehen im Zeigertext. Die Berichtsfassung (<c>knotenwerte</c>) nennt an jedem Knoten seine
    /// Jahressumme in MWh/a; Bänder bleiben ohne Zahl.</para>
    ///
    /// <para><b>Bänder</b> sind geschlossene Pfade: Oberkante als abgetastete S-Kurve y(t) = y0 + (y1 − y0)·(3t² − 2t³) mit
    /// <see cref="EF_STUETZSTELLEN"/> Stützstellen, Unterkante zurück; Füllung in der Rolle mit Deckung 0,55. Das
    /// Zeichenmodell kennt keine Bézierkurve, PNG und SVG zeichnen denselben Polygonzug.</para>
    /// </summary>
    public static partial class ChartRenderer
    {
        /// <summary>Die breite Fassung am Bildschirm [px].</summary>
        public static readonly Bildmass ENERGIEFLUSS_BREIT = new Bildmass(1180, 560);

        /// <summary>Die schmale Fassung am Bildschirm [px] (Stilblatt wählt sie unter 900 px Breite).</summary>
        public static readonly Bildmass ENERGIEFLUSS_SCHMAL = new Bildmass(680, 640);

        /// <summary>Die Berichtsfassung [px] — Faktor 2 auf 620 × 310 pt.</summary>
        public static readonly Bildmass ENERGIEFLUSS_BERICHT = new Bildmass(1240, 620);

        /// <summary>Unter dieser Breite [px] gelten die Regeln der schmalen Fassung (Kurznamen, kleinere Schrift).</summary>
        public const int ENERGIEFLUSS_SCHMAL_UNTER = 900;

        /// <summary>Breite eines Knotens [px].</summary>
        public const float EF_KNOTENBREITE = 14f;

        /// <summary>Mindestbreite eines Bandes [px] — ein kleinerer Fluss wird so breit gezeichnet und zählt so in die Knotenhöhe.</summary>
        public const float EF_MINDESTBAND = 2f;

        /// <summary>Unter dieser Höhe [px] trägt ein innerer Knoten keine Beschriftung im Bild (Busse ausgenommen).</summary>
        public const float EF_MIN_BESCHRIFTUNG = 12f;

        /// <summary>Stützstellen je Bandkante.</summary>
        public const int EF_STUETZSTELLEN = 24;

        /// <summary>Deckung der Bandflächen: 0,55.</summary>
        public const byte EF_BANDDECKUNG = 140;

        /// <summary>Mindesthöhe eines Knotens [px].</summary>
        private const float EF_MIN_KNOTEN = 4f;

        /// <summary>Spaltenzahl des Bildes (Konzept 2.2); mehr Spalten nur, wenn die Daten sie verlangen.</summary>
        private const int EF_SPALTEN = 7;

        /// <summary>Ein Knoten im Layout.</summary>
        private sealed class EfKnoten
        {
            public EnergieflussBildKnoten Daten;
            public int Index;
            public bool Verlust;
            public float X, Y, H;
            public double Ein, Aus;
            public readonly List<EfBand> Eingang = new List<EfBand>();
            public readonly List<EfBand> Ausgang = new List<EfBand>();
            public double Wert => Daten != null && double.IsFinite(Daten.Mwh) ? Daten.Mwh : Math.Max(Ein, Aus);
        }

        /// <summary>Ein Band im Layout.</summary>
        private sealed class EfBand
        {
            public EnergieflussBildBand Daten;
            public int Index;
            public EfKnoten Von, Nach;
            public float Dicke, Y0, Y1;
        }

        /// <summary>Das Energieflussbild als PNG (für die Proben und den Bericht).</summary>
        public static byte[] EnergieflussBild(EnergieflussBildDaten daten, Bildmass? mass = null, bool knotenwerte = false)
            => SkiaMaler.Png(EnergieflussModell(daten, mass, knotenwerte));

        /// <summary>
        /// <b>Das Zeichenmodell des Energieflussbilds.</b> Ohne <paramref name="mass"/> die breite Fassung
        /// (<see cref="ENERGIEFLUSS_BREIT"/>); unter <see cref="ENERGIEFLUSS_SCHMAL_UNTER"/> Breite gelten die Regeln der
        /// schmalen Fassung. Bänder ohne endlichen positiven Wert oder mit unbekanntem Knoten fallen weg, ebenso Knoten
        /// ohne Band; das Bild ist deterministisch.
        /// </summary>
        /// <param name="daten">Knoten, Bänder und Spaltentitel.</param>
        /// <param name="mass">Die Bildgröße; <c>null</c> = breit.</param>
        /// <param name="knotenwerte">Berichtsfassung: jeder Knoten nennt seine Jahressumme (MWh/a). Am Bildschirm
        /// <c>false</c> — dort stehen die Werte nur im Zeigertext.</param>
        public static Zeichenmodell EnergieflussModell(EnergieflussBildDaten daten, Bildmass? mass = null, bool knotenwerte = false)
        {
            int W = Bildmass.BreiteOder(mass, ENERGIEFLUSS_BREIT.Breite);
            int H = Bildmass.HoeheOder(mass, ENERGIEFLUSS_BREIT.Hoehe);
            var z = Modell(W, H);
            bool schmal = W < ENERGIEFLUSS_SCHMAL_UNTER;

            // ---------------------------------------------------------------- Knoten und Bänder sammeln
            var knoten = new Dictionary<string, EfKnoten>(StringComparer.Ordinal);
            var reihenfolge = new List<EfKnoten>();
            IReadOnlyList<EnergieflussBildKnoten> eingabeKnoten = daten?.Knoten ?? Array.Empty<EnergieflussBildKnoten>();
            for (int i = 0; i < eingabeKnoten.Count; i++)
            {
                EnergieflussBildKnoten k = eingabeKnoten[i];
                if (k == null || string.IsNullOrEmpty(k.Kennung) || knoten.ContainsKey(k.Kennung)) continue;
                var ek = new EfKnoten { Daten = k, Index = i, Verlust = k.Art == EnergieflussKnotenart.Verlust };
                knoten[k.Kennung] = ek;
                reihenfolge.Add(ek);
            }

            var baender = new List<EfBand>();
            IReadOnlyList<EnergieflussBildBand> eingabeBaender = daten?.Baender ?? Array.Empty<EnergieflussBildBand>();
            for (int i = 0; i < eingabeBaender.Count; i++)
            {
                EnergieflussBildBand b = eingabeBaender[i];
                if (b == null || !double.IsFinite(b.Mwh) || b.Mwh <= 0) continue;
                if (b.Von == null || b.Nach == null || b.Von == b.Nach) continue;
                if (!knoten.TryGetValue(b.Von, out EfKnoten von) || !knoten.TryGetValue(b.Nach, out EfKnoten nach)) continue;
                if (von.Verlust) continue;   // aus einem Verlustknoten fließt nichts weiter
                var eb = new EfBand { Daten = b, Index = i, Von = von, Nach = nach };
                baender.Add(eb);
                von.Ausgang.Add(eb); von.Aus += b.Mwh;
                nach.Eingang.Add(eb); nach.Ein += b.Mwh;
            }
            List<EfKnoten> sichtbar = reihenfolge.Where(k => k.Eingang.Count + k.Ausgang.Count > 0).ToList();
            if (baender.Count == 0) return z;

            // ---------------------------------------------------------------- Geometrie
            int spalten = Math.Max(EF_SPALTEN, sichtbar.Max(k => Math.Max(0, k.Daten.Spalte) + (k.Verlust ? 2 : 1)));
            float randL = schmal ? 100f : 150f, randR = schmal ? 100f : 150f;
            float oben = schmal ? 46f : 34f, unten = 28f;
            float luecke = schmal ? 26f : 30f;
            float spaltAbst = (W - randL - randR - EF_KNOTENBREITE) / (spalten - 1);
            float X(double sp) => randL + (float)sp * spaltAbst;
            float nutzH = H - oben - unten;

            var proSpalte = new SortedDictionary<int, List<EfKnoten>>();
            foreach (EfKnoten k in sichtbar.Where(k => !k.Verlust))
            {
                int sp = Math.Max(0, k.Daten.Spalte);
                if (!proSpalte.TryGetValue(sp, out List<EfKnoten> liste)) proSpalte[sp] = liste = new List<EfKnoten>();
                liste.Add(k);
            }

            // Maßstab aus der höchsten Spaltensumme über die nutzbare Höhe; die Mindestbandbreite kann die Spalte
            // verlängern, dann schrumpft der Maßstab, bis jede Spalte passt.
            double maxSumme = 0; int maxN = 1;
            foreach (List<EfKnoten> liste in proSpalte.Values)
            {
                double s = liste.Sum(k => Math.Max(k.Ein, k.Aus));
                if (s > maxSumme) { maxSumme = s; maxN = liste.Count; }
            }
            double massstab = maxSumme > 0 ? Math.Max(1e-9, (nutzH - luecke * (maxN - 1)) / maxSumme) : 1.0;
            float Dicke(double mwh) => Math.Max((float)(mwh * massstab), EF_MINDESTBAND);
            float Hoehe(EfKnoten k) => Math.Max(EF_MIN_KNOTEN,
                Math.Max(k.Eingang.Sum(b => Dicke(b.Daten.Mwh)), k.Ausgang.Sum(b => Dicke(b.Daten.Mwh))));
            for (int versuch = 0; versuch < 60; versuch++)
            {
                float groesste = proSpalte.Values.Max(l => l.Sum(Hoehe) + luecke * (l.Count - 1));
                if (groesste <= nutzH + 0.01f) break;
                massstab *= 0.97;
            }
            foreach (EfBand b in baender) b.Dicke = Dicke(b.Daten.Mwh);

            // Knoten je Spalte in Eingabereihenfolge, die Spalte vertikal zentriert.
            foreach (KeyValuePair<int, List<EfKnoten>> e in proSpalte)
            {
                float gesamt = e.Value.Sum(Hoehe) + luecke * (e.Value.Count - 1);
                float y = oben + (nutzH - gesamt) / 2f;
                foreach (EfKnoten k in e.Value)
                {
                    k.X = X(e.Key); k.Y = y; k.H = Hoehe(k);
                    y += k.H + luecke;
                }
            }

            // Verlustknoten: in der Lücke rechts neben ihrem Erzeuger, unter dessen Fuß; mehrere in einer Lücke
            // stapeln sich nach der Lage ihres Erzeugers.
            var unterkante = new Dictionary<int, float>();
            foreach (EfKnoten k in sichtbar.Where(k => k.Verlust)
                                           .OrderBy(k => k.Eingang.Count > 0 ? k.Eingang[0].Von.Y : 0f).ThenBy(k => k.Index))
            {
                int sp = Math.Max(0, k.Daten.Spalte);
                EfKnoten q = k.Eingang.Count > 0 ? k.Eingang[0].Von : null;
                k.H = Hoehe(k);
                k.X = X(sp + 0.5) - EF_KNOTENBREITE / 2f;
                float y = q != null ? q.Y + q.H + 8f : oben;
                if (unterkante.TryGetValue(sp, out float frei)) y = Math.Max(y, frei + 4f);
                y = Math.Max(oben, Math.Min(y, H - unten - k.H));
                k.Y = y;
                unterkante[sp] = y + k.H;
            }

            // Bandlagen je Knoten: ausgehend nach der Lage des Ziels (Verluste zuunterst), eingehend nach der der Quelle.
            foreach (EfKnoten k in sichtbar)
            {
                float y = k.Y;
                foreach (EfBand b in k.Ausgang.OrderBy(b => (b.Nach.Verlust ? 1e5f : 0f) + b.Nach.Y).ThenBy(b => b.Index))
                { b.Y0 = y; y += b.Dicke; }
                y = k.Y;
                foreach (EfBand b in k.Eingang.OrderBy(b => b.Von.Y).ThenBy(b => b.Index))
                { b.Y1 = y; y += b.Dicke; }
            }

            using (var tf = Schrift(schmal ? 10f : 11f, true))
            using (var lf = Schrift(schmal ? 10f : 11f))
            using (var vf = Schrift(schmal ? 9f : 10f))
            {
                // ------------------------------------------------------------ Spaltentitel
                IReadOnlyList<string> titel = daten.Spaltentitel ?? Array.Empty<string>();
                foreach (int sp in proSpalte.Keys)
                {
                    string t = sp < titel.Count ? titel[sp] ?? "" : "";
                    if (t.Length == 0) continue;
                    t = Gekuerzt(t, tf, Math.Max(40f, spaltAbst * (schmal ? 1.8f : 0.95f)));
                    float w = tf.MeasureText(t);
                    float x = Math.Max(2f, Math.Min(W - w - 2f, X(sp) + EF_KNOTENBREITE / 2f - w / 2f));
                    float y = schmal && sp % 2 == 1 ? 4f + tf.Hoehe : 4f;
                    z.Markiert("spaltentitel", zt => Text(zt, t, tf, Farbrolle.TEXT, x, y));
                }

                // ------------------------------------------------------------ Bänder
                foreach (EfBand b in baender)
                {
                    EnergieflussBildBand d = b.Daten;
                    Farbrolle rolle = d.Rolle ?? (d.Art == EnergieflussBandart.Verlust ? Farbrolle.VERLUST
                                                : d.Art == EnergieflussBandart.NichtZugeordnet ? Farbrolle.REST
                                                : Farbrolle.UNBENANNT);
                    float x0 = b.Von.X + EF_KNOTENBREITE, x1 = b.Nach.X;
                    var punkte = new List<Punkt>(2 * EF_STUETZSTELLEN);
                    for (int i = 0; i < EF_STUETZSTELLEN; i++)
                    {
                        float s = i / (float)(EF_STUETZSTELLEN - 1);
                        float e = 3f * s * s - 2f * s * s * s;
                        punkte.Add(new Punkt(x0 + (x1 - x0) * s, b.Y0 + (b.Y1 - b.Y0) * e));
                    }
                    for (int i = EF_STUETZSTELLEN - 1; i >= 0; i--)
                    {
                        float s = i / (float)(EF_STUETZSTELLEN - 1);
                        float e = 3f * s * s - 2f * s * s * s;
                        punkte.Add(new Punkt(x0 + (x1 - x0) * s, b.Y0 + b.Dicke + (b.Y1 - b.Y0) * e));
                    }
                    Zeichnung.Stift rand = d.Art == EnergieflussBandart.NichtZugeordnet
                        ? Stift(rolle, 1f, new Strichmuster(4f, 3f)) : null;
                    var fuellung = Flaeche(Farbton.Aus(rolle).MitDeckung(EF_BANDDECKUNG));
                    string wert = d.Zeigertext ?? (KnotenName(b.Von, false) + " → " + KnotenName(b.Nach, false) + ": " +
                                                   EfZahl(d.Mwh) + " MWh/a");
                    z.Markiert("fluss:" + b.Von.Daten.Kennung + ":" + b.Nach.Daten.Kennung, wert,
                               zb => zb.Pfad(punkte, true, rand, fuellung));
                }

                // ------------------------------------------------------------ Beschriftung planen
                // Erst die Lage jeder Beschriftung, dann gezeichnet: Eine Beschriftung, die eine schon gesetzte
                // überdecken würde, entfällt (größere Knoten zuerst) — ihr Name steht im Zeigertext.
                var beschriftung = new Dictionary<EfKnoten, List<(string Text, Schriftmass F, float X, float Y)>>();
                var belegt = new List<Rahmen>();
                foreach (EfKnoten k in sichtbar.OrderByDescending(k => k.H).ThenBy(k => k.Index))
                {
                    var zeilen = Beschriftung(k, schmal, knotenwerte, spalten, spaltAbst, randL, randR, lf, vf);
                    if (zeilen.Count == 0) continue;
                    var rahmen = zeilen.Select(t => new Rahmen(t.X - 2f, t.Y, t.F.MeasureText(t.Text) + 4f, t.F.Hoehe)).ToList();
                    if (rahmen.Any(r => belegt.Any(o => Ueberlappt(r, o)))) continue;
                    belegt.AddRange(rahmen);
                    beschriftung[k] = zeilen;
                }

                // ------------------------------------------------------------ Knoten
                var randStift = Stift(Farbrolle.RAHMEN, 1f);
                foreach (EfKnoten k in sichtbar)
                {
                    Farbrolle rolle = k.Daten.Rolle ?? (k.Verlust ? Farbrolle.VERLUST : Farbrolle.KNOTEN);
                    string wert = k.Daten.Zeigertext ?? (KnotenName(k, false) + ": " + EfZahl(k.Wert) + " MWh/a");
                    beschriftung.TryGetValue(k, out var zeilen);
                    z.Markiert("knoten:" + k.Daten.Kennung, wert, zk =>
                    {
                        zk.Rechteck(k.X, k.Y, EF_KNOTENBREITE, k.H, randStift, Flaeche(rolle));
                        if (zeilen != null)
                            foreach (var t in zeilen) Text(zk, t.Text, t.F, Farbrolle.TEXT, t.X, t.Y);
                    });
                }
            }
            return z;

            // Die Zeilen der Beschriftung eines Knotens samt Lage (linke obere Ecke).
            List<(string Text, Schriftmass F, float X, float Y)> Beschriftung(EfKnoten k, bool eng, bool mitWert, int nSpalten,
                float abstand, float rl, float rr, Schriftmass lf, Schriftmass vf)
            {
                var zeilen = new List<(string Text, Schriftmass F, float X, float Y)>();
                string name = KnotenName(k, eng);
                string zahl = EfZahl(k.Wert);
                float cy = k.Y + k.H / 2f;
                if (k.Verlust)
                {
                    string t = Gekuerzt(mitWert ? name + " " + zahl : name, vf, Math.Max(30f, abstand - EF_KNOTENBREITE - 4f));
                    float w = vf.MeasureText(t);
                    zeilen.Add((t, vf, k.X + EF_KNOTENBREITE / 2f - w / 2f, k.Y + k.H + 2f));
                    return zeilen;
                }
                int sp = Math.Max(0, k.Daten.Spalte);
                bool links = k.Daten.Art == EnergieflussKnotenart.Quelle || sp == 0;
                bool rechts = !links && (k.Daten.Art == EnergieflussKnotenart.Senke || sp >= nSpalten - 1);
                if (links || rechts)
                {
                    float breite = (links ? rl : rr) - 10f;
                    string t = Gekuerzt(name, lf, breite);
                    string v = mitWert ? Gekuerzt(zahl + " MWh/a", vf, breite) : null;
                    float hoehe = lf.Hoehe + (v != null ? vf.Hoehe : 0f);
                    float y = cy - hoehe / 2f;
                    float xT = links ? k.X - 6f - lf.MeasureText(t) : k.X + EF_KNOTENBREITE + 6f;
                    zeilen.Add((t, lf, xT, y));
                    if (v != null)
                        zeilen.Add((v, vf, links ? k.X - 6f - vf.MeasureText(v) : k.X + EF_KNOTENBREITE + 6f, y + lf.Hoehe));
                    return zeilen;
                }
                if (k.H < EF_MIN_BESCHRIFTUNG && k.Daten.Art != EnergieflussKnotenart.Bus) return zeilen;
                string innen = Gekuerzt(mitWert ? name + " · " + zahl : name, lf, Math.Max(40f, abstand - 6f));
                float wi = lf.MeasureText(innen);
                float xi = Math.Max(2f, Math.Min(W - wi - 2f, k.X + EF_KNOTENBREITE / 2f - wi / 2f));
                zeilen.Add((innen, lf, xi, Math.Max(oben - lf.Hoehe, k.Y - 3f - lf.Hoehe)));
                return zeilen;
            }
        }

        /// <summary>Name eines Knotens, schmal die Kurzform (sonst der Name).</summary>
        private static string KnotenName(EfKnoten k, bool schmal)
        {
            EnergieflussBildKnoten d = k.Daten;
            if (schmal && !string.IsNullOrEmpty(d.Kurzname)) return d.Kurzname;
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
