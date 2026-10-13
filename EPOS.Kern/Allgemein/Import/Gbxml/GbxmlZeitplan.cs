using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml.Linq;
using SpeicherEngine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Ein Zeitplan der gbXML-Datei</b> (<c>Schedule</c>, Welle DA-1, Konzept Datenaustausch der Simulationsdaten 4.1):
    /// Typ, Jahresabschnitte (<c>YearSchedule</c>) mit Beginn und Ende im Gemeinjahr und je Abschnitt der Wochenplan
    /// (<c>WeekSchedule</c>) mit den Tagesbildern je Tagtyp (<c>DaySchedule</c>, 24 Stundenwerte). Der Plan hält die
    /// Struktur der Datei verlustfrei — Tagtypen, Abschnittsfolge, Namen —, damit ein später exportierter <c>Schedule</c>
    /// ohne Verlust zurückkommt (E138 F5). Temperaturen stehen in °C, Anteile 0 … 1 (bei <c>OnOff</c> 0 oder 1).
    /// </summary>
    internal sealed class GbxmlZeitplan
    {
        /// <summary>Typ <c>Temp</c> — Sollwerte [°C].</summary>
        public const string TYP_TEMPERATUR = "Temp";

        /// <summary>Typ <c>Fraction</c> — Anteil 0 … 1 eines Nennwerts.</summary>
        public const string TYP_ANTEIL = "Fraction";

        /// <summary>Typ <c>OnOff</c> — 0 oder 1.</summary>
        public const string TYP_EINAUS = "OnOff";

        /// <summary>Die Kennung (<c>Schedule/@id</c>).</summary>
        public string Kennung { get; set; } = "";

        /// <summary>Der Name (<c>Schedule/Name</c>); <c>null</c> = keiner.</summary>
        public string Name { get; set; }

        /// <summary>Der Typ wie gelesen (<c>Schedule/@type</c>, sonst der des ersten Tagesplans); <c>null</c> = keiner.</summary>
        public string Typ { get; set; }

        /// <summary>Die Jahresabschnitte in der Reihenfolge der Datei.</summary>
        public List<GbxmlJahresabschnitt> Abschnitte { get; } = new List<GbxmlJahresabschnitt>();

        /// <summary>Der Grund, aus dem der Plan nicht abbildbar ist (Meldungsname ohne Präfix); <c>null</c> = abbildbar.</summary>
        public string Verworfen { get; set; }

        /// <summary>Wie viele Tagesbilder aus feinerem Zeitschritt auf Stundenmittel verdichtet wurden.</summary>
        public int VerdichteteTage { get; set; }

        /// <summary>Anzeigename: Name, sonst Kennung.</summary>
        public string Anzeige => string.IsNullOrWhiteSpace(Name) ? Kennung : Name.Trim();

        /// <summary>Ist der Typ ein Temperaturplan?</summary>
        public bool IstTemperatur => string.Equals(Typ, TYP_TEMPERATUR, StringComparison.OrdinalIgnoreCase);

        /// <summary>Ist der Typ ein Anteil- oder An/Aus-Plan?</summary>
        public bool IstAnteil => string.Equals(Typ, TYP_ANTEIL, StringComparison.OrdinalIgnoreCase)
                                 || string.Equals(Typ, TYP_EINAUS, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>Ein Jahresabschnitt (<c>YearSchedule</c>): Tage 1 … 365 im Gemeinjahr, Beginn nach Ende heißt über den Jahreswechsel.</summary>
    internal sealed class GbxmlJahresabschnitt
    {
        /// <summary>Die Kennung (<c>YearSchedule/@id</c>); <c>null</c> = keine.</summary>
        public string Kennung { get; set; }

        /// <summary>Der Name (<c>YearSchedule/Name</c>, Annahme); <c>null</c> = keiner.</summary>
        public string Name { get; set; }

        /// <summary>Der erste Tag 1 … 365.</summary>
        public int Beginn { get; set; } = 1;

        /// <summary>Der letzte Tag 1 … 365.</summary>
        public int Ende { get; set; } = 365;

        /// <summary>Der Wochenplan des Abschnitts.</summary>
        public GbxmlWochenplan Woche { get; set; }

        /// <summary>Die Zahl der Tage des Abschnitts.</summary>
        public int Tage => Ende >= Beginn ? Ende - Beginn + 1 : 365 - Beginn + 1 + Ende;

        /// <summary>Enthält der Abschnitt den Tag 1 … 365?</summary>
        public bool Enthaelt(int tag) => Ende >= Beginn ? tag >= Beginn && tag <= Ende : tag >= Beginn || tag <= Ende;
    }

    /// <summary>
    /// Ein Wochenplan (<c>WeekSchedule</c>): je Tagtyp der Datei ein Tagesbild mit 24 Stundenwerten. <see cref="Woche"/> setzt
    /// daraus die 168 Zellen (Montag 0 Uhr zuerst) nach der Rangfolge Einzeltag (<c>Mon</c> … <c>Sun</c>) vor
    /// <c>Weekday</c>/<c>Weekend</c> vor <c>WeekendOrHoliday</c> vor <c>All</c>.
    /// </summary>
    internal sealed class GbxmlWochenplan
    {
        /// <summary>Die Einzeltage in der Reihenfolge der Kalenderwoche (0 = Montag).</summary>
        public static readonly string[] EINZELTAGE = { "Mon", "Tue", "Wed", "Thu", "Fri", "Sat", "Sun" };

        /// <summary>Die Tagtypen, die der Import abbildet (Schema 6.01, <c>dayTypeEnum</c>, Annahme ohne Schemakopie).</summary>
        public static readonly string[] TAGTYPEN = EINZELTAGE.Concat(new[] { "Weekday", "Weekend", "WeekendOrHoliday", "Holiday", "All" }).ToArray();

        /// <summary>Die Kennung (<c>WeekSchedule/@id</c>).</summary>
        public string Kennung { get; set; } = "";

        /// <summary>Je Tagtyp (wie in der Datei geschrieben) das Tagesbild.</summary>
        public Dictionary<string, double[]> Tage { get; } = new Dictionary<string, double[]>(StringComparer.OrdinalIgnoreCase);

        /// <summary>Das Tagesbild eines Wochentags (0 = Montag); <c>null</c> = keins.</summary>
        public double[] Tag(int wochentag)
        {
            if (Tage.TryGetValue(EINZELTAGE[wochentag], out double[] t)) return t;
            if (Tage.TryGetValue(wochentag < 5 ? "Weekday" : "Weekend", out t)) return t;
            if (wochentag >= 5 && Tage.TryGetValue("WeekendOrHoliday", out t)) return t;
            return Tage.TryGetValue("All", out t) ? t : null;
        }

        /// <summary>Das Tagesbild der Feiertage (<c>Holiday</c>, sonst <c>WeekendOrHoliday</c>); <c>null</c> = die Datei unterscheidet sie nicht.</summary>
        public double[] Feiertag
            => Tage.TryGetValue("Holiday", out double[] t) ? t : Tage.TryGetValue("WeekendOrHoliday", out t) ? t : null;

        /// <summary>Die 168 Zellen der Woche; <c>null</c>, wenn ein Wochentag kein Tagesbild hat.</summary>
        public double[] Woche()
        {
            var w = new double[Kalenderwoche.WOCHENWERTE];
            for (int d = 0; d < 7; d++)
            {
                double[] tag = Tag(d);
                if (tag == null) return null;
                Array.Copy(tag, 0, w, d * Kalenderwoche.TAGESSTUNDEN, Kalenderwoche.TAGESSTUNDEN);
            }
            return w;
        }
    }

    /// <summary>
    /// <b>Liest die Zeitplankette der gbXML-Datei</b> (<c>Schedule</c> → <c>YearSchedule</c> → <c>WeekSchedule</c> →
    /// <c>DaySchedule</c>/<c>ScheduleValue</c>) vollständig und benennt, was nicht abbildbar ist (E138 F3): Tagesbilder mit
    /// einer Wertezahl, die kein Vielfaches von 24 ist; Wochen ohne Tagesbild für einen Wochentag; Typen außerhalb
    /// <c>Temp</c>/<c>Fraction</c>/<c>OnOff</c>; unlesbare Daten. Feinere Zeitschritte (24·k Werte) werden auf Stundenmittel
    /// verdichtet, Auslegungstage (<c>HeatingDesignDay</c>, <c>CoolingDesignDay</c>) übergangen. Ohne Datenbank.
    /// </summary>
    internal static class GbxmlZeitplanleser
    {
        private static readonly int[] MONATSTAGE = { 31, 28, 31, 30, 31, 30, 31, 31, 30, 31, 30, 31 };

        /// <summary>Die Pläne der Datei je Kennung; <paramref name="melden"/> nimmt Stufe, Meldungsname (ohne Präfix) und Werte.</summary>
        internal static Dictionary<string, GbxmlZeitplan> Lesen(XElement wurzel, Func<double, double?> temperatur,
                                                                Action<PruefStufe, string, string[]> melden)
        {
            var tagesplaene = new Dictionary<string, XElement>(StringComparer.Ordinal);
            var wochenplaene = new Dictionary<string, XElement>(StringComparer.Ordinal);
            var plaene = new Dictionary<string, GbxmlZeitplan>(StringComparer.Ordinal);
            foreach (XElement e in wurzel.Elements())
            {
                string id = Attr(e, "id");
                if (id == null) continue;
                if (e.Name.LocalName == "DaySchedule") tagesplaene.TryAdd(id, e);
                else if (e.Name.LocalName == "WeekSchedule") wochenplaene.TryAdd(id, e);
            }
            var wochen = new Dictionary<string, GbxmlWochenplan>(StringComparer.Ordinal);
            foreach (XElement s in wurzel.Elements().Where(e => e.Name.LocalName == "Schedule"))
            {
                string id = Attr(s, "id");
                if (id == null || plaene.ContainsKey(id)) continue;
                List<XElement> jahre = s.Elements().Where(e => e.Name.LocalName == "YearSchedule").ToList();
                // Der Typ: am Plan, sonst am ersten Wochen- bzw. Tagesplan (Annahme: Schema 6.01 führt type an allen dreien).
                string typ = Attr(s, "type");
                if (typ == null)
                    foreach (string wref in jahre.Select(y => Attr(Kind(y, "WeekScheduleId"), "weekScheduleIdRef")))
                        if (wref != null && wochenplaene.TryGetValue(wref, out XElement we))
                        {
                            typ = Attr(we, "type") ?? we.Elements().Select(d => Attr(d, "dayScheduleIdRef"))
                                                         .Where(r => r != null && tagesplaene.ContainsKey(r))
                                                         .Select(r => Attr(tagesplaene[r], "type")).FirstOrDefault(t => t != null);
                            if (typ != null) break;
                        }
                var plan = new GbxmlZeitplan { Kennung = id, Name = Text(Kind(s, "Name")), Typ = typ };
                plaene[id] = plan;
                if (!plan.IstTemperatur && !plan.IstAnteil)
                {
                    Verwerfen(plan, "PLAN_TYP", melden, plan.Typ ?? "");
                    continue;
                }
                foreach (XElement y in jahre)
                {
                    var a = new GbxmlJahresabschnitt { Kennung = Attr(y, "id"), Name = Text(Kind(y, "Name")) };
                    int? beginn = Jahrestag(Text(Kind(y, "BeginDate")));
                    int? ende = Jahrestag(Text(Kind(y, "EndDate")));
                    if ((Kind(y, "BeginDate") != null && !beginn.HasValue) || (Kind(y, "EndDate") != null && !ende.HasValue))
                    {
                        Verwerfen(plan, "PLAN_DATUM", melden, Text(Kind(y, "BeginDate")) ?? "", Text(Kind(y, "EndDate")) ?? "");
                        break;
                    }
                    a.Beginn = beginn ?? 1;
                    a.Ende = ende ?? 365;
                    string wref = Attr(Kind(y, "WeekScheduleId"), "weekScheduleIdRef");
                    if (wref == null || !wochenplaene.ContainsKey(wref))
                    {
                        melden(PruefStufe.Warnung, "VERWEIS_LEER", new[] { "weekScheduleIdRef", wref ?? "", id });
                        Verwerfen(plan, "PLAN_WOCHE_LUECKE", melden, wref ?? "");
                        break;
                    }
                    // Je Wochenplan und Art (Temperatur in °C umgerechnet oder Anteil) genau eine Lesung.
                    string schluessel = (plan.IstTemperatur ? "T|" : "A|") + wref;
                    if (!wochen.TryGetValue(schluessel, out GbxmlWochenplan w))
                        wochen[schluessel] = w = Wochenplan(wochenplaene[wref], tagesplaene, plan, plan.IstTemperatur ? temperatur : null, melden);
                    a.Woche = w;
                    plan.Abschnitte.Add(a);
                }
                if (plan.Verworfen != null) continue;
                if (plan.Abschnitte.Count == 0) Verwerfen(plan, "PLAN_WOCHE_LUECKE", melden, "");
                else if (plan.Abschnitte.FirstOrDefault(x => x.Woche.Woche() == null) is GbxmlJahresabschnitt luecke)
                    Verwerfen(plan, "PLAN_WOCHE_LUECKE", melden, luecke.Woche.Kennung);
                if (plan.VerdichteteTage > 0)
                    melden(PruefStufe.Info, "PLAN_ZEITSCHRITT", new[] { plan.Anzeige, Ganz(plan.VerdichteteTage) });
            }
            return plaene;
        }

        private static GbxmlWochenplan Wochenplan(XElement we, Dictionary<string, XElement> tagesplaene, GbxmlZeitplan plan,
                                                  Func<double, double?> temperatur, Action<PruefStufe, string, string[]> melden)
        {
            var w = new GbxmlWochenplan { Kennung = Attr(we, "id") ?? "" };
            foreach (XElement d in we.Elements().Where(e => e.Name.LocalName == "Day"))
            {
                string typ = Attr(d, "dayType") ?? "";
                string dref = Attr(d, "dayScheduleIdRef");
                if (!GbxmlWochenplan.TAGTYPEN.Contains(typ, StringComparer.OrdinalIgnoreCase))
                {
                    melden(PruefStufe.Info, "PLAN_TAGTYP", new[] { plan.Anzeige, typ });
                    continue;
                }
                if (dref == null || !tagesplaene.TryGetValue(dref, out XElement de))
                {
                    melden(PruefStufe.Warnung, "VERWEIS_LEER", new[] { "dayScheduleIdRef", dref ?? "", w.Kennung });
                    continue;
                }
                List<double> werte = new List<double>();
                bool lesbar = true;
                foreach (XElement v in de.Elements().Where(e => e.Name.LocalName == "ScheduleValue"))
                {
                    if (double.TryParse(Text(v), NumberStyles.Float, CultureInfo.InvariantCulture, out double x) && double.IsFinite(x)) werte.Add(x);
                    else lesbar = false;
                }
                int n = werte.Count;
                if (!lesbar || n == 0 || n % Kalenderwoche.TAGESSTUNDEN != 0)
                {
                    melden(PruefStufe.Warnung, "PLAN_STUNDEN", new[] { plan.Anzeige, dref, Ganz(n) });
                    continue;
                }
                int k = n / Kalenderwoche.TAGESSTUNDEN;
                var tag = new double[Kalenderwoche.TAGESSTUNDEN];
                for (int h = 0; h < tag.Length; h++)
                {
                    double mittel = werte.Skip(h * k).Take(k).Average();
                    tag[h] = temperatur == null ? mittel : temperatur(mittel) ?? double.NaN;
                }
                if (k > 1) plan.VerdichteteTage++;
                w.Tage[typ] = tag;
            }
            return w;
        }

        private static void Verwerfen(GbxmlZeitplan plan, string grund, Action<PruefStufe, string, string[]> melden, params string[] werte)
        {
            if (plan.Verworfen != null) return;
            plan.Verworfen = grund;
            melden(PruefStufe.Warnung, grund, new[] { plan.Anzeige }.Concat(werte).ToArray());
        }

        /// <summary>
        /// Der Tag 1 … 365 im Gemeinjahr aus einem Datum <c>JJJJ-MM-TT</c> (auch <c>--MM-TT</c>); der 29. Februar wird der
        /// 28. (das Raster kennt kein Schaltjahr); <c>null</c> = leer oder unlesbar.
        /// </summary>
        internal static int? Jahrestag(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            string t = text.Trim();
            if (t.StartsWith("--", StringComparison.Ordinal)) t = "2001-" + t.Substring(2);
            if (t.Length > 10) t = t.Substring(0, 10);
            if (!DateTime.TryParseExact(t, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime d)) return null;
            int tag = Math.Min(d.Day, MONATSTAGE[d.Month - 1]);
            for (int m = 0; m < d.Month - 1; m++) tag += MONATSTAGE[m];
            return tag;
        }

        private static XElement Kind(XElement e, string name) => e?.Elements().FirstOrDefault(k => k.Name.LocalName == name);

        private static string Attr(XElement e, string name)
        {
            string w = e?.Attributes().FirstOrDefault(a => a.Name.LocalName == name)?.Value.Trim();
            return string.IsNullOrEmpty(w) ? null : w;
        }

        private static string Text(XElement e)
        {
            string t = e?.Value.Trim();
            return string.IsNullOrEmpty(t) ? null : t;
        }

        private static string Ganz(int w) => w.ToString(CultureInfo.InvariantCulture);
    }
}
