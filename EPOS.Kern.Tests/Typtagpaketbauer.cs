using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.Json;
using WindowsFormsApplication1;

namespace EPOS.Kern.Tests
{
    /// <summary>
    /// <b>Baut ein Normformvektorpaket für die Proben</b> (Umsetzungskonzept Zapfprofilgenerator
    /// Kapitel 6, ZU19; Stufe Z4b).
    ///
    /// <para><b>Zwei Quellen, beide ohne einen Wert der Richtlinie.</b>
    /// <see cref="Erfunden"/> und <see cref="MitBewoelkung"/> sind rein erfundene Pakete mit
    /// runden Zahlen — sie genügen für Struktur, Ablehnungen und Rechenweg.
    /// <see cref="AusAbgeleiteterJson"/> baut ein Paket aus
    /// <c>Referenzlaeufe/Skripte/vdi4655_abgeleitet.json</c>, deren Werte nach der Regel ZU19
    /// abgeleitet sind (kein Wert gleich dem Original); ohne die Datei gibt sie <c>null</c>
    /// zurück, und der Fall schweigt.</para>
    ///
    /// <para>Geschrieben wird nur in den Temp-Ordner (<see cref="AlsZip"/>); im Repositorium
    /// entsteht keine Datei.</para>
    /// </summary>
    internal sealed class Typtagpaketbauer
    {
        /// <summary>Die Kategorien: Code, Jahreszeit, Tagart, Bewölkung (Schreibweise der Datei).</summary>
        internal List<(string Code, string Jahreszeit, string Tagart, string Bewoelkung)> Kategorien { get; } = new();

        /// <summary>Die Klimazonen.</summary>
        internal List<int> Zonen { get; } = new();

        /// <summary>Die Gebäudearten.</summary>
        internal List<string> Arten { get; } = new();

        /// <summary>Die Zahl der Kalendertage je (Zone, Gebäudeart, Typtag).</summary>
        internal Dictionary<(int Zone, string Art, string Typtag), int> Anzahl { get; } = new();

        /// <summary>Der Faktor der Tagesenergie je (Zone, Gebäudeart, Typtag).</summary>
        internal Dictionary<(int Zone, string Art, string Typtag), double> Faktor { get; } = new();

        /// <summary>Die Kennwerte als Zahl.</summary>
        internal Dictionary<string, double> Kennwerte { get; } = new();

        /// <summary>Die Kennwerte als Text (<c>quelle</c>, <c>ausgabe</c>).</summary>
        internal Dictionary<string, string> Texte { get; } = new();

        /// <summary>Die wahlfreien Tagesgänge.</summary>
        internal List<(string Art, string Typtag, int AufloesungMin, double[] Anteile)> Gaenge { get; } = new();

        // =================================================================================
        //  Erfundene Pakete
        // =================================================================================

        /// <summary>
        /// Ein erfundenes Paket <b>ohne</b> Bewölkungsunterscheidung: sechs Kategorien
        /// (drei Jahreszeiten × Werktag/Sonntag), eine Zone, eine Gebäudeart, runde Zahlen. Die
        /// Kalendertage summieren sich auf 365, und der letzte Faktor ist so gewählt, dass die
        /// Prüfsumme Σ n·F genau 0 ergibt.
        /// </summary>
        internal static Typtagpaketbauer Erfunden(int zone = 3, string art = "probehaus")
        {
            var b = new Typtagpaketbauer();
            b.Zonen.Add(zone);
            b.Arten.Add(art);
            string[] js = { "uebergang", "sommer", "winter" };
            string[] ta = { "werktag", "sonntag" };
            int[] tage = { 90, 20, 100, 25, 105, 25 };
            // Die Faktoren bleiben klein gegen 1/365: so bleibt jede Tagesmenge auch bei zehn
            // Einheiten positiv, und die Klemmung wird nur dort geprueft, wo sie gemeint ist.
            double[] f = { 0.00001, -0.00001, 0.00002, -0.00002, 0.00003, 0.0 };
            int i = 0;
            foreach (string j in js)
                foreach (string t in ta)
                {
                    string code = "T" + (i + 1).ToString("00", CultureInfo.InvariantCulture);
                    b.Kategorien.Add((code, j, t, "ohne"));
                    b.Anzahl[(zone, art, code)] = tage[i];
                    b.Faktor[(zone, art, code)] = f[i];
                    i++;
                }
            b.PruefsummeSchliessen(zone, art);
            b.Texte["quelle"] = "Anwenderpaket (erfunden, Probe)";
            b.Texte["ausgabe"] = "2026-09";
            b.Kennwerte["wintergrenze"] = 5.0;
            b.Kennwerte["heizgrenze." + art] = 15.0;
            b.Kennwerte["pruefsumme.toleranz"] = 1e-9;
            return b;
        }

        /// <summary>
        /// Ein erfundenes Paket <b>mit</b> Bewölkungsunterscheidung: zehn Kategorien — je
        /// Jahreszeit und Tagart einer für heiter und einer für bewölkt, im Sommer einer ohne
        /// Unterscheidung. Dazu die Schwelle als Kennwert.
        /// </summary>
        internal static Typtagpaketbauer MitBewoelkung(int zone = 3, string art = "probehaus",
                                                       double schwelle = 5.0)
        {
            var b = new Typtagpaketbauer();
            b.Zonen.Add(zone);
            b.Arten.Add(art);
            var muster = new List<(string Js, string Ta, string Bw, int Tage, double F)>
            {
                ("uebergang", "werktag", "heiter",  50,  0.00001),
                ("uebergang", "werktag", "bewoelkt", 60, -0.00001),
                ("uebergang", "sonntag", "heiter",  10,  0.00002),
                ("uebergang", "sonntag", "bewoelkt", 12, -0.00002),
                ("sommer",    "werktag", "ohne",    70,  0.00003),
                ("sommer",    "sonntag", "ohne",    12, -0.00003),
                ("winter",    "werktag", "heiter",  50,  0.00004),
                ("winter",    "werktag", "bewoelkt", 71, -0.00004),
                ("winter",    "sonntag", "heiter",  10,  0.00005),
                ("winter",    "sonntag", "bewoelkt", 20, 0.0)
            };
            for (int i = 0; i < muster.Count; i++)
            {
                string code = "K" + (i + 1).ToString("00", CultureInfo.InvariantCulture);
                b.Kategorien.Add((code, muster[i].Js, muster[i].Ta, muster[i].Bw));
                b.Anzahl[(zone, art, code)] = muster[i].Tage;
                b.Faktor[(zone, art, code)] = muster[i].F;
            }
            b.PruefsummeSchliessen(zone, art);
            b.Texte["quelle"] = "Anwenderpaket (erfunden, Probe mit Bewoelkung)";
            b.Kennwerte["wintergrenze"] = 5.0;
            b.Kennwerte["heizgrenze." + art] = 15.0;
            b.Kennwerte["bewoelkung.schwelle"] = schwelle;
            return b;
        }

        /// <summary>
        /// Setzt den letzten Faktor einer Zone und Gebäudeart so, dass Σ n·F = 0 ist — die
        /// Prüfsumme der Methodik. Die übrigen Werte bleiben, wie sie sind.
        /// </summary>
        private void PruefsummeSchliessen(int zone, string art)
        {
            string letzter = Kategorien[Kategorien.Count - 1].Code;
            double summe = Kategorien.Take(Kategorien.Count - 1)
                                     .Sum(k => Anzahl[(zone, art, k.Code)] * Faktor[(zone, art, k.Code)]);
            Faktor[(zone, art, letzter)] = -summe / Anzahl[(zone, art, letzter)];
        }

        /// <summary>Die Kalendertage aller Kategorien einer Zone und Gebäudeart.</summary>
        internal int Tagesumme(int zone, string art)
            => Kategorien.Sum(k => Anzahl.TryGetValue((zone, art, k.Code), out int n) ? n : 0);

        // =================================================================================
        //  Das Paket aus der abgeleiteten JSON-Datei (ZU19)
        // =================================================================================

        /// <summary>
        /// Baut ein Paket aus <c>Referenzlaeufe/Skripte/vdi4655_abgeleitet.json</c>. <c>null</c>,
        /// wenn die Datei fehlt. Die Kalendertage stehen dort je Gebäudeart, die Faktoren
        /// ebenso; Quelle und Ausgabe nennt der Kopf der Datei.
        /// </summary>
        internal static Typtagpaketbauer AusAbgeleiteterJson()
        {
            string pfad = AbgeleiteteJson();
            if (pfad == null) return null;
            using JsonDocument d = JsonDocument.Parse(File.ReadAllText(pfad, Encoding.UTF8));
            JsonElement w = d.RootElement;
            var b = new Typtagpaketbauer();
            foreach (JsonElement t in w.GetProperty("typtage").EnumerateArray())
                b.Kategorien.Add((t.GetProperty("code").GetString(), t.GetProperty("jahreszeit").GetString(),
                                  t.GetProperty("tagart").GetString(), t.GetProperty("bewoelkung").GetString()));
            foreach (JsonElement z in w.GetProperty("klimazonen").EnumerateArray()) b.Zonen.Add(z.GetInt32());
            foreach (JsonElement a in w.GetProperty("gebaeudearten").EnumerateArray()) b.Arten.Add(a.GetString());
            foreach (JsonProperty art in w.GetProperty("typtage_je_zone").EnumerateObject())
                foreach (JsonProperty zone in art.Value.EnumerateObject())
                    foreach (JsonProperty tt in zone.Value.EnumerateObject())
                        b.Anzahl[(int.Parse(zone.Name, CultureInfo.InvariantCulture), art.Name, tt.Name)] = tt.Value.GetInt32();
            foreach (JsonProperty art in w.GetProperty("f_twe_tt").EnumerateObject())
                foreach (JsonProperty zone in art.Value.EnumerateObject())
                    foreach (JsonProperty tt in zone.Value.EnumerateObject())
                        b.Faktor[(int.Parse(zone.Name, CultureInfo.InvariantCulture), art.Name, tt.Name)] = tt.Value.GetDouble();
            foreach (JsonProperty kw in w.GetProperty("kennwerte").EnumerateObject())
                b.Kennwerte[kw.Name] = kw.Value.GetDouble();
            b.Texte["quelle"] = w.GetProperty("kopf").GetProperty("quelle").GetString();
            b.Texte["ausgabe"] = "2021-07 (abgeleitet)";
            return b;
        }

        /// <summary>
        /// Der Pfad von <c>Referenzlaeufe/Skripte/vdi4655_abgeleitet.json</c>, aufwärts gesucht;
        /// <c>null</c>, wenn sie fehlt.
        /// </summary>
        internal static string AbgeleiteteJson([System.Runtime.CompilerServices.CallerFilePath] string eigeneDatei = null)
        {
            foreach (string start in new[] { Path.GetDirectoryName(eigeneDatei ?? ""), AppContext.BaseDirectory })
            {
                DirectoryInfo o = string.IsNullOrEmpty(start) ? null : new DirectoryInfo(start);
                for (int i = 0; i < 8 && o != null; i++, o = o.Parent)
                {
                    string kandidat = Path.Combine(o.FullName, "Referenzlaeufe", "Skripte", "vdi4655_abgeleitet.json");
                    if (File.Exists(kandidat)) return kandidat;
                }
            }
            return null;
        }

        // =================================================================================
        //  Schreiben
        // =================================================================================

        /// <summary>Die sechs Dateien des Pakets (die Tagesgänge nur, wenn es welche führt).</summary>
        internal List<TwwPaketdatei> Dateien()
        {
            var dateien = new List<TwwPaketdatei>
            {
                new TwwPaketdatei(Normformvektorleser.DATEI_TYPTAGE, Zeilen("code;jahreszeit;tagart;bewoelkung",
                    Kategorien.Select(k => Feld(k.Code) + ";" + Feld(k.Jahreszeit) + ";" + Feld(k.Tagart) + ";" + Feld(k.Bewoelkung)))),
                new TwwPaketdatei(Normformvektorleser.DATEI_KLIMAZONEN, Zeilen("zone;bezeichnung",
                    Zonen.Select(z => Zahl(z) + ";Zone " + Zahl(z)))),
                new TwwPaketdatei(Normformvektorleser.DATEI_ANZAHL, Zeilen("zone;gebaeudeart;typtag;anzahl",
                    Anzahl.OrderBy(x => x.Key.Zone).ThenBy(x => x.Key.Art, StringComparer.Ordinal)
                          .ThenBy(x => x.Key.Typtag, StringComparer.Ordinal)
                          .Select(x => Zahl(x.Key.Zone) + ";" + Feld(x.Key.Art) + ";" + Feld(x.Key.Typtag) + ";" + Zahl(x.Value)))),
                new TwwPaketdatei(Normformvektorleser.DATEI_FAKTOREN, Zeilen("gebaeudeart;zone;typtag;faktor",
                    Faktor.OrderBy(x => x.Key.Art, StringComparer.Ordinal).ThenBy(x => x.Key.Zone)
                          .ThenBy(x => x.Key.Typtag, StringComparer.Ordinal)
                          .Select(x => Feld(x.Key.Art) + ";" + Zahl(x.Key.Zone) + ";" + Feld(x.Key.Typtag) + ";" + Zahl(x.Value)))),
                new TwwPaketdatei(Normformvektorleser.DATEI_KENNWERTE, Zeilen("schluessel;wert;text",
                    Texte.OrderBy(x => x.Key, StringComparer.Ordinal).Select(x => Feld(x.Key) + ";;" + Feld(x.Value))
                         .Concat(Kennwerte.OrderBy(x => x.Key, StringComparer.Ordinal)
                                          .Select(x => Feld(x.Key) + ";" + Zahl(x.Value) + ";"))))
            };
            if (Gaenge.Count > 0)
                dateien.Add(new TwwPaketdatei(Normformvektorleser.DATEI_GAENGE,
                    Zeilen("gebaeudeart;typtag;aufloesung_min;index;anteil",
                        Gaenge.SelectMany(g => g.Anteile.Select((a, i) =>
                            Feld(g.Art) + ";" + Feld(g.Typtag) + ";" + Zahl(g.AufloesungMin) + ";" + Zahl(i) + ";" + Zahl(a))))));
            return dateien;
        }

        /// <summary>Schreibt das Paket als ZIP in den Ordner und gibt den Pfad zurück.</summary>
        internal string AlsZip(string ordner, string name = "typtage.zip")
        {
            Directory.CreateDirectory(ordner);
            string pfad = Path.Combine(ordner, name);
            if (File.Exists(pfad)) File.Delete(pfad);
            using (FileStream fs = File.Create(pfad))
            using (var zip = new ZipArchive(fs, ZipArchiveMode.Create))
                foreach (TwwPaketdatei d in Dateien())
                {
                    ZipArchiveEntry e = zip.CreateEntry(d.Name);
                    using StreamWriter s = new StreamWriter(e.Open(), new UTF8Encoding(false));
                    s.Write(d.Inhalt);
                }
            return pfad;
        }

        /// <summary>Ersetzt den Inhalt einer Datei der Liste (für die Proben der Strukturfehler).</summary>
        internal static List<TwwPaketdatei> Ersetzen(List<TwwPaketdatei> dateien, string name, string inhalt)
        {
            var neu = new List<TwwPaketdatei>(dateien);
            int i = neu.FindIndex(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase));
            if (i < 0) neu.Add(new TwwPaketdatei(name, inhalt));
            else neu[i] = new TwwPaketdatei(name, inhalt);
            return neu;
        }

        /// <summary>Entfernt eine Datei aus der Liste.</summary>
        internal static List<TwwPaketdatei> Ohne(List<TwwPaketdatei> dateien, string name)
            => dateien.Where(d => !string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();

        /// <summary>Der Inhalt einer Datei der Liste.</summary>
        internal static string Inhalt(List<TwwPaketdatei> dateien, string name)
            => dateien.First(d => string.Equals(d.Name, name, StringComparison.OrdinalIgnoreCase)).Inhalt;

        private static string Zeilen(string kopf, IEnumerable<string> zeilen)
            => kopf + "\n" + string.Join("\n", zeilen) + "\n";

        private static string Feld(string s) => s ?? "";

        private static string Zahl(int i) => i.ToString(CultureInfo.InvariantCulture);

        private static string Zahl(double d) => d.ToString("R", CultureInfo.InvariantCulture);
    }
}
