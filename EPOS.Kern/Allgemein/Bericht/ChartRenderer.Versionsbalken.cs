using System;
using System.Collections.Generic;
using System.Linq;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>Eine Serie der Versionsbalken: eine Version (Stamm oder Variante) mit ihrer Farbrolle.</summary>
    public sealed class Versionsbalkenserie
    {
        /// <summary>Der Anzeigename der Version.</summary>
        public string Name { get; set; } = "";

        /// <summary>Die Farbrolle der Version — in allen Bildern eines Vergleichs dieselbe.</summary>
        public Farbrolle Rolle { get; set; } = Farbrolle.STAMM;
    }

    /// <summary>Eine Kennzahl der Versionsbalken: je Serie ein Wert samt fertiger Texte.</summary>
    public sealed class Versionsbalkenzeile
    {
        /// <summary>Die Beschriftung der Kennzahl ohne Einheit (die Einheit trägt die Achse).</summary>
        public string Bezeichnung { get; set; } = "";

        /// <summary>Je Serie der Wert; <c>null</c> = kein Balken (nicht simuliert, kein Wert).</summary>
        public IReadOnlyList<double?> Werte { get; set; } = Array.Empty<double?>();

        /// <summary>Je Serie die Beschriftung am Balkenende, fertig formatiert (Wert, Abweichung).</summary>
        public IReadOnlyList<string> Texte { get; set; } = Array.Empty<string>();

        /// <summary>Je Serie der Wert am Zeiger, fertig formatiert („Variante · Kennzahl: 12,3 MWh/a (+5,0 %)“).</summary>
        public IReadOnlyList<string> Zeigertexte { get; set; } = Array.Empty<string>();

        /// <summary>Je Serie: weicht der Wert vom Stamm ab? Dann trägt der Balken einen Randstrich.</summary>
        public IReadOnlyList<bool> WeichtAb { get; set; } = Array.Empty<bool>();
    }

    /// <summary>
    /// <b>Gruppierte waagrechte Balken je Version</b> — die grafische Darstellung des Abschnitts „Ergebnisse — Energie“
    /// der Übersicht: je Kennzahl ein Block, darin ein Balken je Version in der Farbe ihrer Rolle.
    /// </summary>
    public static partial class ChartRenderer
    {
        /// <summary>Vorgabebreite der Versionsbalken [px].</summary>
        public const int VERSIONSBALKEN_BREITE = 560;

        /// <summary>Höhe eines Balkens [px].</summary>
        private const float VB_BALKEN = 16f;

        /// <summary>Abstand zweier Balken eines Blocks [px].</summary>
        private const float VB_LUECKE = 4f;

        /// <summary>Höhe der Kennzahlbeschriftung über einem Block [px].</summary>
        private const float VB_KOPF = 28f;

        /// <summary>Abstand zweier Blöcke [px].</summary>
        private const float VB_BLOCKABSTAND = 12f;

        /// <summary>
        /// <b>Das Zeichenmodell der Versionsbalken.</b> Ein reines Pixelbild wie <see cref="BalkenHorizontalModell"/>:
        /// keine Zeichenfläche, keine Legende im Bild (die Legende der Versionen steht einmal über allen Bildern). Jeder
        /// Balken steht in der Klammer <c>reihe:&lt;Version&gt;</c> und trägt seinen Zeigertext als Wert; ein Wert, der vom
        /// Stamm abweicht, bekommt am Balkenende einen dunklen Randstrich. Eine Serie ohne Wert bekommt keinen Balken.
        /// Die Höhe folgt der Zahl der Kennzahlen und Serien; die Achse unten trägt die Einheit in eckigen Klammern.
        /// Ein neues Bild ohne Wirkung auf die Bestandsbilder.
        /// </summary>
        /// <param name="einheit">Die Einheit der Achse („MWh/a“); leer = ohne Achsentitel.</param>
        /// <param name="serien">Die Versionen, der Stamm zuerst.</param>
        /// <param name="zeilen">Die Kennzahlen, je Serie ein Eintrag.</param>
        /// <param name="breite">Die Bildbreite [px]; die Höhe folgt dem Inhalt.</param>
        public static Zeichenmodell VersionsbalkenModell(string einheit, IReadOnlyList<Versionsbalkenserie> serien,
                                                         IReadOnlyList<Versionsbalkenzeile> zeilen,
                                                         int breite = VERSIONSBALKEN_BREITE)
        {
            serien ??= Array.Empty<Versionsbalkenserie>();
            zeilen ??= Array.Empty<Versionsbalkenzeile>();
            int n = Math.Max(1, serien.Count);
            int W = Math.Max(240, breite);
            float block = VB_KOPF + n * VB_BALKEN + (n - 1) * VB_LUECKE;
            float oben = 10f;
            float achse = string.IsNullOrEmpty(einheit) ? 30f : 50f;
            int H = (int)Math.Ceiling(oben + zeilen.Count * block + Math.Max(0, zeilen.Count - 1) * VB_BLOCKABSTAND + 10f + achse);
            var z = Modell(W, H);

            // Der Wertebereich schliesst die Null ein; nicht endliche Werte fallen weg.
            var alle = zeilen.SelectMany(r => r.Werte ?? Array.Empty<double?>())
                             .Where(w => w.HasValue && double.IsFinite(w.Value)).Select(w => w.Value).ToList();
            double hi = alle.Count == 0 ? 0 : Math.Max(0, alle.Max());
            double lo = alle.Count == 0 ? 0 : Math.Min(0, alle.Min());
            hi = hi > 0 ? Nice(hi) : 0;
            lo = lo < 0 ? -Nice(-lo) : 0;
            if (hi - lo <= 0) hi = 1;

            using (var lf = Schrift(15f))
            using (var wf = Schrift(13f))
            using (var af = Schrift(12f))
            {
                // Rechts bleibt Platz für die längste Beschriftung am Balkenende (höchstens zwei Fünftel der Breite).
                float textmax = 0f;
                foreach (Versionsbalkenzeile r in zeilen)
                    foreach (string t in r.Texte ?? Array.Empty<string>())
                        textmax = Math.Max(textmax, wf.MeasureText(t ?? ""));
                float links = 16f;
                float rechts = W - 16f - Math.Min(W * 0.4f, textmax + 10f);
                float unten = H - achse;
                float spanne = rechts - links;
                float X(double w) => links + (float)((w - lo) / (hi - lo)) * spanne;
                float null0 = X(0);

                // Raster und Achse: fünf Teilungen über den Bereich, beschriftet in der Zahlkultur des Renderers.
                double schritt = Skala.Rund((hi - lo) / 5.0);
                string format = schritt >= 1 ? "N0" : schritt >= 0.1 ? "N1" : "N2";
                z.Markiert("xachse", zx =>
                {
                    for (double t = Math.Ceiling(lo / schritt) * schritt; t <= hi + schritt * 1e-6; t += schritt)
                    {
                        float x = X(t);
                        zx.Linie(x, oben, x, unten, Stift(Farbrolle.RASTER, 1f));
                        string s = t.ToString(format, Zahlkultur);
                        Text(zx, s, af, Farbrolle.ACHSE, x - af.MeasureText(s) / 2f, unten + 4f);
                    }
                    if (!string.IsNullOrEmpty(einheit))
                    {
                        string titel = "[" + einheit + "]";
                        Text(zx, titel, af, Farbrolle.ACHSE, links + spanne / 2f - af.MeasureText(titel) / 2f, unten + 24f);
                    }
                });

                var rahmen = Stift(Farbrolle.LEGENDENRAHMEN, 1f);
                var strich = Stift(Farbrolle.TEXT, 3f);
                for (int k = 0; k < zeilen.Count; k++)
                {
                    Versionsbalkenzeile r = zeilen[k];
                    float y0 = oben + k * (block + VB_BLOCKABSTAND);
                    string titel = Gekuerzt(r.Bezeichnung ?? "", lf, W - 2f * links);
                    z.Markiert("kennzahl", zk => Text(zk, titel, lf, Farbrolle.TEXT, links, y0 + 2f));

                    for (int i = 0; i < serien.Count; i++)
                    {
                        double? w = r.Werte != null && i < r.Werte.Count ? r.Werte[i] : null;
                        if (!w.HasValue || !double.IsFinite(w.Value)) continue;
                        float y = y0 + VB_KOPF + i * (VB_BALKEN + VB_LUECKE);
                        float xw = X(w.Value);
                        float xa = Math.Min(null0, xw), laenge = Math.Abs(xw - null0);
                        string text = r.Texte != null && i < r.Texte.Count ? r.Texte[i] ?? "" : "";
                        string zeiger = r.Zeigertexte != null && i < r.Zeigertexte.Count ? r.Zeigertexte[i] ?? "" : "";
                        bool ab = r.WeichtAb != null && i < r.WeichtAb.Count && r.WeichtAb[i];
                        Versionsbalkenserie s = serien[i];
                        z.Markiert("reihe:" + (s.Name ?? ""), zeiger, zb =>
                        {
                            zb.Rechteck(xa, y, Math.Max(laenge, 1f), VB_BALKEN, null, Flaeche(s.Rolle ?? Farbrolle.STAMM));
                            zb.Rechteck(xa, y, Math.Max(laenge, 1f), VB_BALKEN, rahmen);
                            float xr = w.Value < 0 ? xa : xa + Math.Max(laenge, 1f);
                            if (ab) zb.Linie(xr, y - 2f, xr, y + VB_BALKEN + 2f, strich);
                            float xt = Math.Max(xa + Math.Max(laenge, 1f), null0) + 6f;
                            Text(zb, text, wf, Farbrolle.TEXT, xt, y - 2f);
                        });
                    }
                }
                z.Linie(null0, oben, null0, unten, Stift(Farbrolle.ACHSE, 1.5f));
            }
            return z;
        }

        /// <summary>Kürzt einen Text mit „…“ auf die Breite.</summary>
        private static string Gekuerzt(string text, Schriftmass f, float breite)
        {
            if (f.MeasureText(text) <= breite) return text;
            string t = text;
            while (t.Length > 1 && f.MeasureText(t + "…") > breite) t = t.Substring(0, t.Length - 1);
            return t.TrimEnd() + "…";
        }
    }
}
