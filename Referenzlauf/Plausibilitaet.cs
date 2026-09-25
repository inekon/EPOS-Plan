using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace WindowsFormsApplication1.Referenzlauf
{
    /// <summary>
    /// Grobpruefung eines eingefrorenen Laufs, bevor er als Referenz gilt:
    /// Rasterlaenge (8760 Stunden bzw. 35040 Viertelstunden), keine NaN/Inf,
    /// und Summen groesser null dort, wo eine Null auf einen stillen Fehlschlag hindeutet.
    ///
    /// <para><b>Die eine benannte Ausnahme: die Lueckenreihen der Anlagenkopplung</b>
    /// (Konzept Anlagenkopplung 8.3, <c>GebaeudeErgebnisexport</c>). Vorlauf und Ruecklauf
    /// des Heiz- und des Kaeltekreises eines gekoppelten Gebaeudes tragen in den Stunden
    /// ohne Betrieb NaN — das ist die gewollte Luecke der Reihe, kein Rechenfehler. Nur
    /// fuer genau diese vier Dateimuster (<see cref="IstLueckenreihe"/>) zaehlt NaN als
    /// Luecke; Inf, Text und eine Reihe, die nur aus Luecken besteht, bleiben beanstandet,
    /// und jede andere Datei behandelt NaN wie bisher als ungueltigen Wert.</para>
    /// </summary>
    internal static class Plausibilitaet
    {
        /// <summary>
        /// <c>vorlauf_&lt;n&gt;.csv</c>, <c>ruecklauf_&lt;n&gt;.csv</c>, <c>kuehlvorlauf_&lt;n&gt;.csv</c>,
        /// <c>kuehlruecklauf_&lt;n&gt;.csv</c> — n ist der Merkplatz des Gebaeudes. Genau, ohne
        /// Gross-/Kleinschreibung zu ignorieren: So heissen die Dateien des Exports, nichts sonst.
        /// </summary>
        private static readonly Regex Lueckenreihe =
            new Regex(@"^(kuehl)?(vorlauf|ruecklauf)_[0-9]+\.csv$", RegexOptions.CultureInvariant);

        /// <summary>Darf diese Datei NaN als Luecke tragen?</summary>
        internal static bool IstLueckenreihe(string dateiname)
        {
            return dateiname != null && Lueckenreihe.IsMatch(dateiname);
        }

        /// <summary>Was eine Vektordatei enthaelt — gezaehlt, ohne zu urteilen.</summary>
        internal sealed class Reihenbefund
        {
            public int Zeilen;
            public int Ungueltig;
            public int Luecken;
            public double Summe;
        }

        /// <summary>
        /// Liest eine Vektordatei (<c>Index;Wert</c>, erste Zeile Kopf). NaN zaehlt als
        /// <see cref="Reihenbefund.Luecken"/>, wenn die Datei eine Lueckenreihe ist, sonst als
        /// ungueltig; Inf und nicht lesbare Werte sind immer ungueltig.
        /// </summary>
        internal static Reihenbefund ReiheLesen(string dateiname, TextReader leser)
        {
            var b = new Reihenbefund();
            bool lueckenErlaubt = IstLueckenreihe(dateiname);
            leser.ReadLine();   // Kopfzeile
            string z;
            while ((z = leser.ReadLine()) != null)
            {
                if (z.Length == 0) continue;
                b.Zeilen++;
                int t = z.IndexOf(';');
                if (t < 0) { b.Ungueltig++; continue; }
                string wert = z.Substring(t + 1);
                double d;
                if (!double.TryParse(wert, NumberStyles.Float, CultureInfo.InvariantCulture, out d)
                    || double.IsInfinity(d))
                {
                    b.Ungueltig++;
                    continue;
                }
                if (double.IsNaN(d))
                {
                    if (lueckenErlaubt) b.Luecken++;
                    else b.Ungueltig++;
                    continue;
                }
                b.Summe += d;
            }
            return b;
        }

        /// <summary>
        /// Vektoren, deren Jahressumme groesser null sein muss - je Erzeuger aber nur dann,
        /// wenn dem Projekt ueberhaupt ein Modul zugeordnet ist. Ein aktiviertes Gewerk ohne
        /// Modul (z. B. Projekt 1007: Solarthermie an, kein Kollektor gepflegt) liefert
        /// zwangslaeufig null und ist ein Datenzustand, kein Rechenfehler.
        /// Wert = Schluessel in aggregate.csv, dessen Vorhandensein die Forderung ausloest;
        /// null = immer gefordert.
        /// </summary>
        private static readonly Dictionary<string, string> MussPositivSein =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "waermebedarf.csv",     null },
                { "wp_produktion.csv",    "WaermepumpeModul[0].Modul" },
                { "kessel_leistung.csv",  "HeizkesselModul[0].Modul" },
                { "bhkw_waerme.csv",      "BHKWModul[0].Modul" },
                { "solar_produktion.csv", "SolarthermieModul[0].Modul" },
                { "pv_produktion.csv",    "PhotovoltaikModul[0].Modul" }
            };

        public static int Pruefen(string ordner)
        {
            if (!Directory.Exists(ordner))
            {
                Console.WriteLine("FEHLER: Ordner nicht gefunden: " + ordner);
                return 1;
            }

            var projekte = Directory.GetDirectories(ordner, "Projekt_*")
                                    .OrderBy(d => d, StringComparer.OrdinalIgnoreCase).ToList();
            if (projekte.Count == 0)
            {
                Console.WriteLine("FEHLER: Keine Projekt_*-Unterordner in " + ordner);
                return 1;
            }

            Console.WriteLine("Plausibilitaetspruefung: " + Path.GetFullPath(ordner));
            Console.WriteLine();

            bool allesGut = true;

            foreach (string projekt in projekte)
            {
                var beanstandungen = new List<string>();
                var hinweise = new List<string>();
                string name = Path.GetFileName(projekt);

                string aggregat = Path.Combine(projekt, "aggregate.csv");
                var aggregatSchluessel = new HashSet<string>(StringComparer.Ordinal);
                if (!File.Exists(aggregat))
                {
                    beanstandungen.Add("aggregate.csv fehlt");
                }
                else
                {
                    string[] zeilenAggregat = File.ReadAllLines(aggregat);
                    if (zeilenAggregat.Length < 2) beanstandungen.Add("aggregate.csv ist leer");
                    for (int i = 1; i < zeilenAggregat.Length; i++)
                    {
                        int t = zeilenAggregat[i].IndexOf(';');
                        if (t > 0) aggregatSchluessel.Add(zeilenAggregat[i].Substring(0, t));
                    }
                }

                var vektoren = Directory.GetFiles(projekt, "*.csv")
                                        .Where(f => !Path.GetFileName(f).Equals("aggregate.csv",
                                                        StringComparison.OrdinalIgnoreCase))
                                        .OrderBy(f => f, StringComparer.OrdinalIgnoreCase).ToList();

                if (vektoren.Count == 0) beanstandungen.Add("keine Vektor-CSV vorhanden");

                int zeilenGesamt = 0;

                foreach (string datei in vektoren)
                {
                    string kurz = Path.GetFileName(datei);
                    Reihenbefund befund;
                    using (var leser = new StreamReader(datei))
                        befund = ReiheLesen(kurz, leser);
                    int zeilen = befund.Zeilen;
                    double summe = befund.Summe;

                    zeilenGesamt += zeilen;

                    if (zeilen != 8760 && zeilen != 35040)
                        beanstandungen.Add(kurz + ": " + zeilen + " Zeilen (erwartet 8760 oder 35040)");

                    if (befund.Ungueltig > 0)
                        beanstandungen.Add(kurz + ": " + befund.Ungueltig + " NaN/Inf/ungueltige Werte");

                    if (befund.Luecken > 0 && befund.Luecken == zeilen)
                        beanstandungen.Add(kurz + ": nur Luecken (NaN), kein einziger Wert");
                    else if (befund.Luecken > 0)
                        hinweise.Add(kurz + ": " + befund.Luecken +
                                     " Luecken (NaN in Stunden ohne Betrieb, Anlagenkopplung 8.3)");

                    string bedingung;
                    if (MussPositivSein.TryGetValue(kurz, out bedingung) && summe <= 0)
                    {
                        bool gefordert = bedingung == null || aggregatSchluessel.Contains(bedingung);
                        string meldung = kurz + ": Jahressumme " +
                            summe.ToString("G6", CultureInfo.InvariantCulture);
                        if (gefordert) beanstandungen.Add(meldung + " (erwartet > 0)");
                        else hinweise.Add(meldung + " - Gewerk aktiviert, aber kein Modul zugeordnet");
                    }
                }

                if (beanstandungen.Count == 0)
                {
                    Console.WriteLine(string.Format(CultureInfo.InvariantCulture,
                        "{0}: OK ({1} Vektoren, {2} Werte)", name, vektoren.Count, zeilenGesamt));
                }
                else
                {
                    allesGut = false;
                    Console.WriteLine(name + ": BEANSTANDET");
                    foreach (string b in beanstandungen) Console.WriteLine("    " + b);
                }

                foreach (string h in hinweise) Console.WriteLine("    Hinweis: " + h);
            }

            Console.WriteLine();
            Console.WriteLine(allesGut ? "GESAMT: plausibel" : "GESAMT: Beanstandungen vorhanden");
            return allesGut ? 0 : 1;
        }
    }
}
