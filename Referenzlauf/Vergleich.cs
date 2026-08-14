using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>Eine einzelne Abweichung zwischen Referenz- und Vergleichslauf.</summary>
    internal sealed class Abweichung
    {
        public string Datei;
        public string Schluessel;
        public string Referenz;
        public string Neu;

        /// <summary>Abweichung als Vielfaches der erlaubten Toleranz. &gt; 1 = Verletzung.</summary>
        public double Schwere;

        public string Beschreibung;

        public override string ToString()
        {
            if (Beschreibung != null)
                return Datei + " [" + Schluessel + "]: " + Beschreibung;

            return string.Format(CultureInfo.InvariantCulture,
                "{0} [{1}]: ref={2} neu={3} ({4:0.###}x Toleranz)",
                Datei, Schluessel, Referenz, Neu, Schwere);
        }
    }

    /// <summary>
    /// Toleranzvergleich zweier Referenzlauf-Ordner.
    ///
    /// Toleranz (fuer Skalare wie fuer jedes Vektorelement gleich):
    ///  - Werte ab 1 (Betrag): relative Abweichung bis 1e-4,
    ///  - Werte unter 1:       absolute Abweichung bis 0,01.
    /// Nichtnumerische Werte (Modulnamen, Schalter) muessen exakt uebereinstimmen.
    /// </summary>
    internal static class Vergleich
    {
        public const double TOLERANZ_RELATIV = 1e-4;
        public const double TOLERANZ_ABSOLUT = 0.01;
        private const int TOP_N = 10;

        /// <summary>Fuehrt den Vergleich aus. Rueckgabe: 0 = alles PASS, 1 = mindestens ein FAIL.</summary>
        public static int Ausfuehren(string refOrdner, string neuOrdner)
        {
            if (!Directory.Exists(refOrdner))
            {
                Console.WriteLine("FEHLER: Referenzordner nicht gefunden: " + refOrdner);
                return 1;
            }
            if (!Directory.Exists(neuOrdner))
            {
                Console.WriteLine("FEHLER: Vergleichsordner nicht gefunden: " + neuOrdner);
                return 1;
            }

            Console.WriteLine("Referenz : " + Path.GetFullPath(refOrdner));
            Console.WriteLine("Vergleich: " + Path.GetFullPath(neuOrdner));
            Console.WriteLine("Toleranz : relativ " + TOLERANZ_RELATIV.ToString("G3", CultureInfo.InvariantCulture) +
                              " ab Betrag 1, sonst absolut " + TOLERANZ_ABSOLUT.ToString("G3", CultureInfo.InvariantCulture));
            Console.WriteLine();

            var refProjekte = ProjektOrdner(refOrdner);
            var neuProjekte = ProjektOrdner(neuOrdner);

            var alleNamen = refProjekte.Keys.Union(neuProjekte.Keys)
                                            .OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            if (alleNamen.Count == 0)
            {
                Console.WriteLine("FEHLER: In keinem der beiden Ordner liegen Projekt_*-Unterordner.");
                return 1;
            }

            bool allesGut = true;
            int gepruefteWerte = 0;

            foreach (string name in alleNamen)
            {
                if (!refProjekte.ContainsKey(name))
                {
                    Console.WriteLine(name + ": FAIL - nur im Vergleichslauf vorhanden.");
                    allesGut = false;
                    continue;
                }
                if (!neuProjekte.ContainsKey(name))
                {
                    Console.WriteLine(name + ": FAIL - im Vergleichslauf nicht vorhanden.");
                    allesGut = false;
                    continue;
                }

                int werte;
                int dateien;
                List<Abweichung> abweichungen =
                    ProjektVergleichen(refProjekte[name], neuProjekte[name], out dateien, out werte);
                gepruefteWerte += werte;

                if (abweichungen.Count == 0)
                {
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0}: PASS ({1} Dateien, {2} Werte)", name, dateien, werte));
                    continue;
                }

                allesGut = false;
                Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "{0}: FAIL ({1} Dateien, {2} Werte, {3} Abweichungen)",
                    name, dateien, werte, abweichungen.Count));

                foreach (var a in abweichungen.OrderByDescending(a => a.Schwere).Take(TOP_N))
                    Console.WriteLine("    " + a);

                if (abweichungen.Count > TOP_N)
                    Console.WriteLine("    ... und " + (abweichungen.Count - TOP_N) + " weitere.");
            }

            Console.WriteLine();
            Console.WriteLine(allesGut
                ? "GESAMT: PASS (" + gepruefteWerte + " Werte innerhalb der Toleranz)"
                : "GESAMT: FAIL");
            return allesGut ? 0 : 1;
        }

        private static Dictionary<string, string> ProjektOrdner(string wurzel)
        {
            return Directory.GetDirectories(wurzel, "Projekt_*")
                            .ToDictionary(d => Path.GetFileName(d), d => d,
                                          StringComparer.OrdinalIgnoreCase);
        }

        private static List<Abweichung> ProjektVergleichen(string refOrdner, string neuOrdner,
                                                           out int dateien, out int werte)
        {
            var ergebnis = new List<Abweichung>();
            werte = 0;

            var refDateien = Dateiliste(refOrdner);
            var neuDateien = Dateiliste(neuOrdner);
            var alle = refDateien.Keys.Union(neuDateien.Keys)
                                      .OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
            dateien = alle.Count;

            foreach (string datei in alle)
            {
                if (!refDateien.ContainsKey(datei))
                {
                    ergebnis.Add(new Abweichung
                    {
                        Datei = datei, Schluessel = "-", Schwere = double.MaxValue,
                        Beschreibung = "Datei nur im Vergleichslauf vorhanden"
                    });
                    continue;
                }
                if (!neuDateien.ContainsKey(datei))
                {
                    ergebnis.Add(new Abweichung
                    {
                        Datei = datei, Schluessel = "-", Schwere = double.MaxValue,
                        Beschreibung = "Datei fehlt im Vergleichslauf"
                    });
                    continue;
                }

                var a = CsvLesen(refDateien[datei]);
                var b = CsvLesen(neuDateien[datei]);
                werte += Math.Max(a.Count, b.Count);
                DateiVergleichen(datei, a, b, ergebnis);
            }

            return ergebnis;
        }

        private static Dictionary<string, string> Dateiliste(string ordner)
        {
            return Directory.GetFiles(ordner, "*.csv")
                            .ToDictionary(f => Path.GetFileName(f), f => f,
                                          StringComparer.OrdinalIgnoreCase);
        }

        private static void DateiVergleichen(string datei,
                                             Dictionary<string, string> a,
                                             Dictionary<string, string> b,
                                             List<Abweichung> ergebnis)
        {
            foreach (var paar in a)
            {
                string neuWert;
                if (!b.TryGetValue(paar.Key, out neuWert))
                {
                    ergebnis.Add(new Abweichung
                    {
                        Datei = datei, Schluessel = paar.Key, Schwere = double.MaxValue,
                        Beschreibung = "Eintrag fehlt im Vergleichslauf (ref=" + paar.Value + ")"
                    });
                    continue;
                }

                double schwere;
                if (WerteGleich(paar.Value, neuWert, out schwere)) continue;

                ergebnis.Add(new Abweichung
                {
                    Datei = datei, Schluessel = paar.Key,
                    Referenz = paar.Value, Neu = neuWert, Schwere = schwere
                });
            }

            foreach (var paar in b)
            {
                if (a.ContainsKey(paar.Key)) continue;
                ergebnis.Add(new Abweichung
                {
                    Datei = datei, Schluessel = paar.Key, Schwere = double.MaxValue,
                    Beschreibung = "Eintrag nur im Vergleichslauf (neu=" + paar.Value + ")"
                });
            }
        }

        /// <summary>
        /// Vergleicht zwei Werte. <paramref name="schwere"/> ist das Vielfache der erlaubten
        /// Toleranz und dient nur der Sortierung der Top-Abweichungen.
        /// </summary>
        internal static bool WerteGleich(string refWert, string neuWert, out double schwere)
        {
            schwere = 0;
            if (string.Equals(refWert, neuWert, StringComparison.Ordinal)) return true;

            double x, y;
            bool xOk = double.TryParse(refWert, NumberStyles.Float, CultureInfo.InvariantCulture, out x);
            bool yOk = double.TryParse(neuWert, NumberStyles.Float, CultureInfo.InvariantCulture, out y);

            if (!xOk || !yOk)
            {
                // Text gegen Text bzw. Text gegen Zahl: muss exakt passen.
                schwere = double.MaxValue;
                return false;
            }

            if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y))
            {
                schwere = double.MaxValue;
                return false;
            }

            double abweichung = Math.Abs(x - y);
            double bezug = Math.Max(Math.Abs(x), Math.Abs(y));

            if (bezug < 1.0)
            {
                schwere = abweichung / TOLERANZ_ABSOLUT;
                return abweichung <= TOLERANZ_ABSOLUT;
            }

            double relativ = abweichung / bezug;
            schwere = relativ / TOLERANZ_RELATIV;
            return relativ <= TOLERANZ_RELATIV;
        }

        /// <summary>Liest eine CSV im Format "Schluessel;Wert" (erste Zeile ist die Kopfzeile).</summary>
        private static Dictionary<string, string> CsvLesen(string datei)
        {
            var werte = new Dictionary<string, string>(StringComparer.Ordinal);
            using (var leser = new StreamReader(datei))
            {
                leser.ReadLine();   // Kopfzeile
                string zeile;
                while ((zeile = leser.ReadLine()) != null)
                {
                    if (zeile.Length == 0) continue;
                    int trenner = zeile.IndexOf(';');
                    if (trenner < 0) continue;
                    werte[zeile.Substring(0, trenner)] = zeile.Substring(trenner + 1);
                }
            }
            return werte;
        }
    }
}
