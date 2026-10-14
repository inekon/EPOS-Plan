using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Zeitplan einer Kalendergröße für den gbXML-Export</b> (Welle DA-2, Konzept Datenaustausch 4.4): Aus einem
    /// <see cref="Konditionierungskalender"/> entsteht ein <see cref="GbxmlZeitplan"/> — die Grundwoche über das ganze Jahr als
    /// erster <c>YearSchedule</c>, jede Periode <c>ZEITRAUM</c>/<c>FERIEN</c>/<c>BETRIEBSPAUSE</c> als eigener
    /// <c>YearSchedule</c> mit <c>BeginDate</c>/<c>EndDate</c> im Gemeinjahr <see cref="BEZUGSJAHR"/> (aufsteigend nach Rang:
    /// der spätere Abschnitt der Datei gilt, wie der ranghöhere im Kalender), die Feiertage als Tagtyp <c>Holiday</c> der
    /// Grundwoche. Dieselbe Bauform liest <see cref="GbxmlZeitplanleser"/>; der Rundlauf Export → Import ist damit ohne
    /// Verlust für Grundwoche, Zeiträume, Stundenwerte und Feiertagsbild (E138 F5). Ohne Datenbank.
    /// </summary>
    internal static class GbxmlZeitplanschreiber
    {
        /// <summary>Das Bezugsjahr der Daten (ein Gemeinjahr; der Leser kennt kein Schaltjahr).</summary>
        internal const int BEZUGSJAHR = 2001;

        /// <summary>Der geschriebene Heizsollwert für „aus“ [°C] — liegt unter jeder Grenze, der Leser macht daraus wieder „aus“.</summary>
        internal const double AUS_HEIZEN_C = -50.0;

        /// <summary>Der geschriebene Kühlsollwert für „aus“ [°C] — liegt über jeder Grenze, der Leser macht daraus wieder „aus“.</summary>
        internal const double AUS_KUEHLEN_C = 99.0;

        private static readonly int[] MONATSTAGE = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>Ist die Größe ein Temperaturplan (<c>Temp</c>)?</summary>
        internal static bool Temperatur(Konditionierungsgroesse g)
            => g == Konditionierungsgroesse.Heizsoll || g == Konditionierungsgroesse.Kuehlsoll;

        /// <summary>
        /// Der Zeitplan eines Kalenders. <paramref name="teiler"/> teilt die Werte (Lüftung: der Nennluftwechsel der Zone, damit
        /// der Plan Anteile trägt); 1 für alle übrigen Größen.
        /// </summary>
        internal static GbxmlZeitplan Plan(Konditionierungskalender k, string kennung, string name, double teiler = 1.0)
        {
            if (k == null) throw new ArgumentNullException(nameof(k));
            bool temp = Temperatur(k.Groesse);
            var plan = new GbxmlZeitplan
            {
                Kennung = kennung, Name = name, Typ = temp ? GbxmlZeitplan.TYP_TEMPERATUR : GbxmlZeitplan.TYP_ANTEIL,
            };
            IReadOnlyList<double> standard = k.Standardwoche;
            double[] grund = Woche(k.Grundangabe, standard, k.Groesse, teiler);
            GbxmlWochenplan hauptwoche = Wochenplan(kennung + "-W0", grund);
            Kalenderregel feiertag = k.Perioden.Where(r => r.IstFeiertag).OrderByDescending(r => r.Rang).FirstOrDefault();
            if (feiertag != null)
                hauptwoche.Tage["Holiday"] = Woche(feiertag.Angabe, standard, k.Groesse, teiler).Take(Kalenderwoche.TAGESSTUNDEN).ToArray();
            plan.Abschnitte.Add(new GbxmlJahresabschnitt { Kennung = kennung + "-Y0", Beginn = 1, Ende = 365, Woche = hauptwoche });
            int n = 1;
            foreach (Kalenderregel r in k.Perioden.Where(r => !r.IstFeiertag).OrderBy(r => r.Rang))
            {
                double[] w = Woche(r.Angabe, standard, k.Groesse, teiler);
                plan.Abschnitte.Add(new GbxmlJahresabschnitt
                {
                    Kennung = kennung + "-Y" + Ganz(n), Name = r.Bezeichner, Beginn = r.Beginn, Ende = r.Ende,
                    Woche = Wochenplan(kennung + "-W" + Ganz(n), w),
                });
                n++;
            }
            return plan;
        }

        /// <summary>Der größte endliche Wert des Kalenders über Grundwoche und alle Perioden (0 ohne einen).</summary>
        internal static double Hoechstwert(Konditionierungskalender k)
        {
            IReadOnlyList<double> standard = k.Standardwoche;
            return new[] { k.Grundangabe }.Concat(k.Perioden.Select(r => r.Angabe))
                .SelectMany(a => Woche(a, standard, k.Groesse, 1.0)).DefaultIfEmpty(0.0).Max();
        }

        /// <summary>Die 168 Werte einer Angabe, wie die Datei sie trägt („aus“ als Grenzwert bzw. 0, geteilt durch <paramref name="teiler"/>).</summary>
        private static double[] Woche(Kalenderangabe a, IReadOnlyList<double> standard, Konditionierungsgroesse g, double teiler)
        {
            var w = new double[Kalenderwoche.WOCHENWERTE];
            for (int d = 0; d < 7; d++)
                for (int h = 0; h < Kalenderwoche.TAGESSTUNDEN; h++)
                {
                    double v = a.Greift(standard) ? a.Stundenwert(d, h, standard) : double.NaN;
                    if (double.IsNaN(v) || double.IsInfinity(v))
                        v = g == Konditionierungsgroesse.Heizsoll ? AUS_HEIZEN_C : g == Konditionierungsgroesse.Kuehlsoll ? AUS_KUEHLEN_C : 0.0;
                    else if (!Temperatur(g) && teiler > 0.0) v /= teiler;
                    w[Kalenderwoche.Stelle(d, h)] = v;
                }
            return w;
        }

        /// <summary>
        /// Der Wochenplan aus 168 Werten: gleiche Tage Montag bis Freitag werden <c>Weekday</c>, gleiches Wochenende <c>Weekend</c>,
        /// sonst je Tag ein Einzeltag.
        /// </summary>
        private static GbxmlWochenplan Wochenplan(string kennung, double[] woche)
        {
            var w = new GbxmlWochenplan { Kennung = kennung };
            double[][] tage = Enumerable.Range(0, 7)
                .Select(d => woche.Skip(d * Kalenderwoche.TAGESSTUNDEN).Take(Kalenderwoche.TAGESSTUNDEN).ToArray()).ToArray();
            if (Enumerable.Range(1, 4).All(d => Gleich(tage[d], tage[0]))) w.Tage["Weekday"] = tage[0];
            else for (int d = 0; d < 5; d++) w.Tage[GbxmlWochenplan.EINZELTAGE[d]] = tage[d];
            if (Gleich(tage[5], tage[6])) w.Tage["Weekend"] = tage[5];
            else for (int d = 5; d < 7; d++) w.Tage[GbxmlWochenplan.EINZELTAGE[d]] = tage[d];
            return w;
        }

        /// <summary>
        /// Die Elemente des Plans an der Wurzel: <c>Schedule</c> (mit <c>YearSchedule</c>-Folge), die <c>WeekSchedule</c> und die
        /// <c>DaySchedule</c> (gleiche Tagesbilder eines Plans teilen einen Tagesplan). <paramref name="kennung"/> prüft und
        /// vergibt die Kennungen (eindeutig im Dokument).
        /// </summary>
        internal static IEnumerable<XElement> Elemente(GbxmlZeitplan plan, XNamespace ns, Func<string, string, string> kennung)
        {
            var schedule = new XElement(ns + "Schedule", new XAttribute("id", kennung(plan.Kennung, "Schedule")), new XAttribute("type", plan.Typ));
            var wochen = new List<XElement>();
            var tage = new List<XElement>();
            var tagJeBild = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (GbxmlJahresabschnitt a in plan.Abschnitte)
            {
                var jahr = new XElement(ns + "YearSchedule", new XAttribute("id", kennung(a.Kennung, "YearSchedule")));
                if (!string.IsNullOrWhiteSpace(a.Name)) jahr.Add(new XElement(ns + "Name", GbxmlSchreiber.Klartext(a.Name.Trim())));
                jahr.Add(new XElement(ns + "BeginDate", Datum(a.Beginn)), new XElement(ns + "EndDate", Datum(a.Ende)),
                         new XElement(ns + "WeekScheduleId", new XAttribute("weekScheduleIdRef", a.Woche.Kennung)));
                schedule.Add(jahr);
                var woche = new XElement(ns + "WeekSchedule", new XAttribute("id", kennung(a.Woche.Kennung, "WeekSchedule")), new XAttribute("type", plan.Typ));
                foreach (KeyValuePair<string, double[]> t in a.Woche.Tage)
                {
                    string bild = string.Join(";", t.Value.Select(GbxmlSchreiber.Zahl));
                    if (!tagJeBild.TryGetValue(bild, out string tagKennung))
                    {
                        tagKennung = kennung(plan.Kennung + "-D" + Ganz(tagJeBild.Count), "DaySchedule");
                        tagJeBild[bild] = tagKennung;
                        tage.Add(new XElement(ns + "DaySchedule", new XAttribute("id", tagKennung), new XAttribute("type", plan.Typ),
                            t.Value.Select(v => new XElement(ns + "ScheduleValue", GbxmlSchreiber.Zahl(v)))));
                    }
                    woche.Add(new XElement(ns + "Day", new XAttribute("dayType", t.Key), new XAttribute("dayScheduleIdRef", tagKennung)));
                }
                wochen.Add(woche);
            }
            return new[] { schedule }.Concat(wochen).Concat(tage);
        }

        /// <summary>Das Datum eines Jahrestags (1 … 365) im <see cref="BEZUGSJAHR"/> als <c>JJJJ-MM-TT</c>.</summary>
        internal static string Datum(int jahrestag)
        {
            int t = Math.Clamp(jahrestag, 1, 365), m = 0;
            while (t > MONATSTAGE[m]) t -= MONATSTAGE[m++];
            return new DateTime(BEZUGSJAHR, m + 1, t).ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        }

        private static bool Gleich(double[] a, double[] b) => a.Length == b.Length && a.Zip(b, (x, y) => x.Equals(y)).All(e => e);

        private static string Ganz(int w) => w.ToString(CultureInfo.InvariantCulture);
    }
}
