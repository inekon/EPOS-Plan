using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Die Art eines Werts der Konditionierungssätze — der Schreiber wählt danach den IFC-Typ.</summary>
    internal enum IfcSatzwertart
    {
        /// <summary>°C, geschrieben als <c>IfcThermodynamicTemperatureMeasure</c> in Kelvin.</summary>
        Temperatur,
        /// <summary>Eine Zahl als <c>IfcReal</c> (1/h oder Anteil 0 … 1, die Einheit nennt die Beschreibung).</summary>
        Zahl,
        /// <summary>Watt als <c>IfcPowerMeasure</c>.</summary>
        Leistung,
        /// <summary>Ein Kennwort als <c>IfcLabel</c>.</summary>
        Kennwort,
        /// <summary>Ein Text als <c>IfcText</c>.</summary>
        Text,
        /// <summary>Ein Schalter als <c>IfcBoolean</c>.</summary>
        Wahrheit,
    }

    /// <summary>
    /// Ein Wert eines Konditionierungssatzes: Name, Art, Inhalt und Beschreibung — als Ressourcenschlüssel
    /// (<paramref name="Beschreibung"/>) oder wörtlich (<paramref name="BeschreibungWoertlich"/>, der Bezeichner einer Periode).
    /// </summary>
    internal sealed record IfcSatzwert(string Name, IfcSatzwertart Art, double? Zahl = null, string Text = null, bool? Wahr = null,
                                       string Beschreibung = null, string BeschreibungWoertlich = null);

    /// <summary>
    /// <b>Die Konditionierung einer Zone als IFC-Eigenschaften</b> (Datenaustauschkonzept 6.3, 16.3) — formatfrei, für den
    /// Schreiber (<see cref="IfcSchreiber"/>), die Anreicherung (<see cref="IfcAnreicherung"/>) und den Leser
    /// (<c>IfcAbbildBauer</c>) dieselbe Festlegung:
    /// <list type="bullet">
    /// <item><c>EPOS_Zone</c> ergänzt <c>Nutzung</c> (<c>IfcLabel</c>), <c>Heizsollwert_Tag</c>, <c>Heizsollwert_Nacht</c>,
    /// <c>Kuehlsollwert</c> (Temperatur) und <c>Luftwechsel_Nutzer</c> (<c>IfcReal</c>, 1/h) — die Matrixzellen, wie die
    /// Zone sie rechnet.</item>
    /// <item>Je Größe mit Kalender ein Satz <c>EPOS_Kalender_&lt;Größe&gt;</c> (HEIZSOLL, KUEHLSOLL, LUEFTUNG, GERAETE,
    /// PERSONEN) mit genau einer Grundangabe — <c>Grundwert</c>, <c>Aus</c> (wahr) oder <c>Woche</c> (die 168 Zellen als Text
    /// wie in der Datenbank, Montag 0 Uhr zuerst, „aus“ = abgeschaltet) —, dazu <c>Nennwert_W</c>, <c>Bemerkung</c> und je
    /// Periode <c>Periode_&lt;Rang&gt;</c>.</item>
    /// <item>Der Periodentext lautet <c>Art;Beginn;Ende;Feiertagsregel;Angabe</c> (invariante Kultur): <c>Art</c> eine der
    /// Periodenarten, <c>Beginn</c> und <c>Ende</c> Tag 1 … 365 (0 bei einer Feiertagsregel), <c>Feiertagsregel</c> leer bei
    /// einem Zeitraum, <c>Angabe</c> eines von <c>wert=&lt;Zahl&gt;</c>, <c>aus</c>, <c>woche=&lt;168 Zellen&gt;</c> oder
    /// <c>wochentag=&lt;1 … 7&gt;</c>. Der Bezeichner der Periode steht in der Beschreibung der Eigenschaft.</item>
    /// </list>
    /// Werte in der Einheit der Größe: °C bei Heiz- und Kühlsollwert (die Grundangabe als Temperatur in Kelvin, die Zellen
    /// der Woche in °C), 1/h bei der Lüftung, Anteil 0 … 1 bei Geräten und Personen.
    /// </summary>
    internal static class IfcKonditionierungssatz
    {
        /// <summary>Der Präfix der Kalendersätze.</summary>
        internal const string PRAEFIX_KALENDER = "EPOS_Kalender_";

        /// <summary>Der Präfix der Periodeneigenschaften.</summary>
        internal const string PRAEFIX_PERIODE = "Periode_";

        internal const string NUTZUNG = "Nutzung";
        internal const string HEIZSOLL_TAG = "Heizsollwert_Tag";
        internal const string HEIZSOLL_NACHT = "Heizsollwert_Nacht";
        internal const string KUEHLSOLL = "Kuehlsollwert";
        internal const string LUFTWECHSEL_NUTZER = "Luftwechsel_Nutzer";
        internal const string GRUNDWERT = "Grundwert";
        internal const string AUS = "Aus";
        internal const string WOCHE = "Woche";
        internal const string NENNWERT = "Nennwert_W";
        internal const string BEMERKUNG = "Bemerkung";

        private const char TRENNER = ';';
        private const string ANGABE_WERT = "wert=";
        private const string ANGABE_WOCHE = "woche=";
        private const string ANGABE_WOCHENTAG = "wochentag=";

        /// <summary>Der Name des Kalendersatzes einer Größe.</summary>
        internal static string Satzname(Konditionierungsgroesse g) => PRAEFIX_KALENDER + Konditionierungsgroessen.Kennwort(g);

        /// <summary>Die Namen aller fünf Kalendersätze in fester Reihenfolge.</summary>
        internal static IEnumerable<string> Satznamen => Konditionierungsgroessen.Alle.Select(Satzname);

        /// <summary>Die Werte, um die <c>EPOS_Zone</c> wächst; leer ohne Konditionierung.</summary>
        internal static List<IfcSatzwert> Zonenwerte(AbbildKonditionierung k)
        {
            var l = new List<IfcSatzwert>();
            if (k == null) return l;
            if (!string.IsNullOrWhiteSpace(k.Nutzung)) l.Add(new IfcSatzwert(NUTZUNG, IfcSatzwertart.Kennwort, Text: k.Nutzung.Trim()));
            if (k.HeizsollTagC.HasValue) l.Add(new IfcSatzwert(HEIZSOLL_TAG, IfcSatzwertart.Temperatur, k.HeizsollTagC));
            if (k.HeizsollNachtC.HasValue) l.Add(new IfcSatzwert(HEIZSOLL_NACHT, IfcSatzwertart.Temperatur, k.HeizsollNachtC));
            if (k.KuehlsollC.HasValue) l.Add(new IfcSatzwert(KUEHLSOLL, IfcSatzwertart.Temperatur, k.KuehlsollC));
            if (k.LuftwechselNutzerJeH.HasValue)
                l.Add(new IfcSatzwert(LUFTWECHSEL_NUTZER, IfcSatzwertart.Zahl, k.LuftwechselNutzerJeH, Beschreibung: "GEXP_IFC_LUFTWECHSEL_NUTZER"));
            return l;
        }

        /// <summary>Die Werte des Kalendersatzes einer Größe — Grundangabe, Nennwert, Bemerkung, Perioden nach Rang aufsteigend.</summary>
        internal static List<IfcSatzwert> Kalenderwerte(AbbildKalender a)
        {
            var l = new List<IfcSatzwert>();
            Konditionierungskalender k = a?.Kalender;
            if (k == null) return l;
            Konditionierungsgroesse g = k.Groesse;
            switch (k.Grundangabe.Art)
            {
                case Angabeart.Wert:
                    l.Add(Temperaturgroesse(g)
                        ? new IfcSatzwert(GRUNDWERT, IfcSatzwertart.Temperatur, k.Grundangabe.Wert)
                        : new IfcSatzwert(GRUNDWERT, IfcSatzwertart.Zahl, k.Grundangabe.Wert, Beschreibung: "GEXP_IFC_KALENDER_GRUNDWERT"));
                    break;
                case Angabeart.Aus:
                    l.Add(new IfcSatzwert(AUS, IfcSatzwertart.Wahrheit, Wahr: true));
                    break;
                case Angabeart.Woche:
                    l.Add(new IfcSatzwert(WOCHE, IfcSatzwertart.Text, Text: Kalenderwoche.Schreiben(k.Standardwoche, g), Beschreibung: "GEXP_IFC_KALENDER_WOCHE"));
                    break;
            }
            if (k.Nennwert.HasValue) l.Add(new IfcSatzwert(NENNWERT, IfcSatzwertart.Leistung, k.Nennwert));
            if (!string.IsNullOrWhiteSpace(a.Bemerkung)) l.Add(new IfcSatzwert(BEMERKUNG, IfcSatzwertart.Text, Text: a.Bemerkung.Trim()));
            foreach (Kalenderregel r in k.Perioden.OrderBy(p => p.Rang))
                l.Add(new IfcSatzwert(PRAEFIX_PERIODE + r.Rang.ToString(CultureInfo.InvariantCulture), IfcSatzwertart.Text,
                                      Text: Periodentext(r, g), BeschreibungWoertlich: r.Bezeichner));
            return l;
        }

        /// <summary>Wird die Größe in °C geführt (Heiz- und Kühlsollwert)?</summary>
        internal static bool Temperaturgroesse(Konditionierungsgroesse g)
            => g == Konditionierungsgroesse.Heizsoll || g == Konditionierungsgroesse.Kuehlsoll;

        /// <summary><b>Der Periodentext</b> <c>Art;Beginn;Ende;Feiertagsregel;Angabe</c> (Regeln: Klassenkopf).</summary>
        internal static string Periodentext(Kalenderregel r, Konditionierungsgroesse g)
            => string.Join(TRENNER.ToString(), r.Art, Ganz(r.Beginn), Ganz(r.Ende), r.Feiertagsregel ?? "", Angabetext(r.Angabe, g));

        private static string Angabetext(Kalenderangabe a, Konditionierungsgroesse g)
        {
            switch (a.Art)
            {
                case Angabeart.Wert: return ANGABE_WERT + Kalenderwoche.Text(a.Wert);
                case Angabeart.Aus: return DbWerte.KOND_WOCHE_AUS;
                case Angabeart.Woche: return ANGABE_WOCHE + Kalenderwoche.Schreiben(a.Woche, g);
                default: return ANGABE_WOCHENTAG + Ganz(a.WieWochentag);
            }
        }

        /// <summary>
        /// <b>Liest einen Periodentext zurück</b>; <c>null</c>, wenn Format, Art, Tage, Feiertagsregel oder Angabe nicht passen
        /// (der Leser überspringt die Periode dann benannt).
        /// </summary>
        internal static Kalenderregel PeriodeLesen(string name, string text, string bezeichner, Konditionierungsgroesse g)
        {
            if (name == null || !name.StartsWith(PRAEFIX_PERIODE, StringComparison.Ordinal)) return null;
            if (!int.TryParse(name.Substring(PRAEFIX_PERIODE.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int rang)) return null;
            string[] teile = (text ?? "").Split(TRENNER, 5);
            if (teile.Length != 5) return null;
            string art = teile[0].Trim();
            if (!DbWerte.KOND_ARTEN.Contains(art)) return null;
            if (!int.TryParse(teile[1].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int beginn)
                || !int.TryParse(teile[2].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int ende)) return null;
            Kalenderangabe angabe = AngabeLesen(teile[4].Trim(), g);
            if (angabe == null) return null;
            string regel = teile[3].Trim();
            string bez = string.IsNullOrWhiteSpace(bezeichner) ? name : bezeichner.Trim();
            try
            {
                return regel.Length > 0 || art == DbWerte.KOND_ART_FEIERTAG
                    ? Kalenderregel.Feiertag(rang, bez, regel, angabe)
                    : Kalenderregel.Zeitraum(rang, art, bez, beginn, ende, angabe);
            }
            catch (ArgumentException)
            {
                return null;   // Rang, Tag oder Feiertagsregel außerhalb
            }
        }

        private static Kalenderangabe AngabeLesen(string t, Konditionierungsgroesse g)
        {
            if (string.Equals(t, DbWerte.KOND_WOCHE_AUS, StringComparison.OrdinalIgnoreCase)) return Kalenderangabe.Abgeschaltet;
            if (t.StartsWith(ANGABE_WERT, StringComparison.Ordinal))
                return double.TryParse(t.Substring(ANGABE_WERT.Length), NumberStyles.Float, CultureInfo.InvariantCulture, out double w)
                       && double.IsFinite(w) && Konditionierungsgroessen.ImBereich(g, w)
                    ? Kalenderangabe.AusWert(w) : null;
            if (t.StartsWith(ANGABE_WOCHE, StringComparison.Ordinal))
            {
                Wochenlesung l = Kalenderwoche.Lesen(t.Substring(ANGABE_WOCHE.Length), g);
                return l.Befund == Wochenbefund.Gelesen ? Kalenderangabe.AusWoche(l.Werte) : null;
            }
            if (t.StartsWith(ANGABE_WOCHENTAG, StringComparison.Ordinal)
                && int.TryParse(t.Substring(ANGABE_WOCHENTAG.Length), NumberStyles.None, CultureInfo.InvariantCulture, out int tag)
                && tag >= 1 && tag <= 7)
                return Kalenderangabe.AlsWochentag(tag);
            return null;
        }

        /// <summary>
        /// <b>Der Kalender aus den Werten eines Satzes</b> — Grundangabe (Woche vor „aus“ vor Grundwert), Nennwert und die
        /// Perioden; <c>null</c> ohne lesbare Grundangabe. Nicht lesbare Perioden landen namentlich in
        /// <paramref name="uebersprungen"/>.
        /// </summary>
        internal static AbbildKalender KalenderLesen(Konditionierungsgroesse g, IReadOnlyList<IfcSatzwert> werte, List<string> uebersprungen)
        {
            IfcSatzwert W(string n) => werte.FirstOrDefault(w => string.Equals(w.Name, n, StringComparison.Ordinal));
            Kalenderangabe grund = null;
            if (W(WOCHE)?.Text is string woche)
            {
                Wochenlesung l = Kalenderwoche.Lesen(woche, g);
                if (l.Befund == Wochenbefund.Gelesen) grund = Kalenderangabe.AusWoche(l.Werte);
            }
            if (grund == null && W(AUS)?.Wahr == true) grund = Kalenderangabe.Abgeschaltet;
            if (grund == null && W(GRUNDWERT)?.Zahl is double gw && double.IsFinite(gw))
            {
                gw = Math.Round(gw, Kalenderwoche.NACHKOMMASTELLEN);
                if (Konditionierungsgroessen.ImBereich(g, gw)) grund = Kalenderangabe.AusWert(gw);
            }
            if (grund == null)
            {
                uebersprungen.Add(Satzname(g));
                return null;
            }
            double? nennwert = Konditionierungsgroessen.HatNennwert(g) && W(NENNWERT)?.Zahl is double n && double.IsFinite(n)
                ? Math.Round(n, Kalenderwoche.NACHKOMMASTELLEN) : null;
            var perioden = new List<Kalenderregel>();
            var raenge = new HashSet<int>();
            foreach (IfcSatzwert w in werte.Where(x => x.Name != null && x.Name.StartsWith(PRAEFIX_PERIODE, StringComparison.Ordinal))
                                           .OrderBy(x => x.Name, StringComparer.Ordinal))
            {
                Kalenderregel r = PeriodeLesen(w.Name, w.Text, w.BeschreibungWoertlich, g);
                if (r == null || perioden.Count >= Kalenderregel.PERIODEN_MAX || !raenge.Add(r.Rang))
                {
                    uebersprungen.Add(Satzname(g) + "." + w.Name);
                    continue;
                }
                perioden.Add(r);
            }
            try
            {
                return new AbbildKalender(new Konditionierungskalender(g, grund, nennwert, perioden), W(BEMERKUNG)?.Text);
            }
            catch (ArgumentException)
            {
                uebersprungen.Add(Satzname(g));
                return null;
            }
        }

        /// <summary>
        /// Die Bemerkung des übernommenen Kalenders: „aus IFC-Datei (EPOS)“ und der mitgereiste Vermerk, gekürzt auf
        /// <see cref="KonditionierungSchema.BEMERKUNG_MAX_ZEICHEN"/>.
        /// </summary>
        internal static string Herkunftsbemerkung(string vermerk)
        {
            string t = MyResource.Resource.IMP_IFC_KOND_HERKUNFT;
            if (!string.IsNullOrWhiteSpace(vermerk)) t += Kalenderherkunft.TRENNER + vermerk.Trim();
            return t.Length <= KonditionierungSchema.BEMERKUNG_MAX_ZEICHEN ? t : t.Substring(0, KonditionierungSchema.BEMERKUNG_MAX_ZEICHEN);
        }

        private static string Ganz(int w) => w.ToString(CultureInfo.InvariantCulture);
    }
}
