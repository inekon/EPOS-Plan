using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>Das Zeitraster einer exportierten Zeitreihe — es benennt die erste Spalte der Datei.</summary>
    public enum Zeitraster
    {
        /// <summary>8 760 Stundenwerte.</summary>
        Stunde = 0,

        /// <summary>35 040 Viertelstundenwerte.</summary>
        Viertelstunde = 1,

        /// <summary>365 Tageswerte.</summary>
        Tag = 2,

        /// <summary>12 Monatswerte.</summary>
        Monat = 3,

        /// <summary>Jahreswerte (Betrachtungsjahre, Kapitalwertverlauf).</summary>
        Jahr = 4,

        /// <summary>Eine Folge ohne Zeitbezug — die erste Spalte zählt die Stützstellen.</summary>
        Index = 5,

        /// <summary>52 Wochenwerte des Gemeinjahrs; die 52. Woche trägt den 365. Tag mit (<see cref="Zeitsummen"/>).</summary>
        Woche = 6,

        /// <summary>24 Stunden eines Tagesprofils — erste Spalte „Stunde“ 1…24.</summary>
        Tagesstunde = 7,

        /// <summary>168 Stunden eines Wochenprofils — erste Spalte „Wochenstunde“ 1…168.</summary>
        Wochenstunde = 8,

        /// <summary>
        /// Die Tafel eines Kalenderteppichs: 365 Zeilen (Tage des Gemeinjahrs, Datum „TT.MM.“ und
        /// Wochentag des Rasters), 24 Wertspalten „0 h“ … „23 h“ — <see cref="ZeitreihenCsv.Kalenderteppich(Tagesstundentafel)"/>.
        /// </summary>
        Kalendertag = 9
    }

    /// <summary>Eine Spalte des Zeitreihenexports: Reihenname, Einheit (darf leer sein) und Werte.</summary>
    public sealed record ZeitreihenSpalte(string Name, string Einheit, double[] Werte);

    /// <summary>
    /// <b>Der eine Schreiber für jede Zeitreihe eines Diagramms</b> (CSV am Diagramm): erste Spalte
    /// die laufende Nummer im Zeitraster (Stunde 1…8 760, Viertelstunde, Tag, Monat, Jahr), danach je
    /// Reihe eine Spalte „Name [Einheit]“. Kultur und Trennzeichen wie <see cref="CsvExportClass"/>:
    /// Semikolon, Dezimalkomma (de-DE), Format <c>0.0##</c>, UTF-8 mit BOM.
    ///
    /// <para>Die benannten Exporte der Simulation (Energiebedarf, Kälte, Wärmepumpe, Heizkessel,
    /// Stromspeicher, Wärme- und Stromgang) bleiben bei <see cref="CsvExportClass.Export"/> mit
    /// Zeitstempel und Außentemperatur; dieser Schreiber trägt die Diagramme ohne eigenen Export.</para>
    /// </summary>
    public static class ZeitreihenCsv
    {
        private const string SEP = ";";

        /// <summary>Das Raster aus der Länge einer Reihe; unbekannte Längen zählen als <see cref="Zeitraster.Index"/>.</summary>
        public static Zeitraster RasterAus(int laenge) => laenge switch
        {
            8760 => Zeitraster.Stunde,
            35040 => Zeitraster.Viertelstunde,
            365 => Zeitraster.Tag,
            52 => Zeitraster.Woche,
            12 => Zeitraster.Monat,
            168 => Zeitraster.Wochenstunde,
            24 => Zeitraster.Tagesstunde,
            _ => Zeitraster.Index
        };

        /// <summary>Der Kopf der ersten Spalte je Raster.</summary>
        public static string Rasterkopf(Zeitraster raster) => raster switch
        {
            Zeitraster.Stunde => "Stunde",
            Zeitraster.Viertelstunde => "Viertelstunde",
            Zeitraster.Tag => "Tag",
            Zeitraster.Woche => "Woche",
            Zeitraster.Monat => "Monat",
            Zeitraster.Jahr => "Jahr",
            Zeitraster.Tagesstunde => "Stunde",
            Zeitraster.Wochenstunde => "Wochenstunde",
            Zeitraster.Kalendertag => MyResource.Resource.CSV_KOPF_DATUM,
            _ => "Nr."
        };

        /// <summary>
        /// <b>Die Reihen, die das Diagramm zeigt</b>, als Spalten: je Linie oder Fläche mit Werten
        /// eine Spalte. Eine Stapelschicht (Werte = Summe bis zur Schicht, <c>Unten</c> = Summe
        /// darunter) exportiert ihren BEITRAG, wie die Zeigerzeile ihn nennt. Punktwolken (x je Wert)
        /// sind keine Zeitreihe und fallen weg.
        /// </summary>
        public static IReadOnlyList<ZeitreihenSpalte> AusModell(Zeichenmodell modell)
        {
            var spalten = new List<ZeitreihenSpalte>();
            if (modell == null) return spalten;
            foreach (Datenreihe r in modell.Reihen)
            {
                if (r?.Werte == null || r.Werte.Length == 0) continue;
                if (r.Art == Reihenart.Punkte) continue;
                double[] werte = r.Werte;
                if (r.Unten != null && r.Unten.Length == r.Werte.Length)
                {
                    werte = new double[r.Werte.Length];
                    for (int i = 0; i < werte.Length; i++) werte[i] = r.Werte[i] - r.Unten[i];
                }
                spalten.Add(new ZeitreihenSpalte(r.Name ?? "", r.Einheit ?? "", werte));
            }
            return spalten;
        }

        /// <summary>Das Raster eines Modells: aus der längsten Reihe.</summary>
        public static Zeitraster RasterAus(IReadOnlyList<ZeitreihenSpalte> spalten)
        {
            int n = 0;
            foreach (ZeitreihenSpalte s in spalten ?? Array.Empty<ZeitreihenSpalte>())
                if (s?.Werte != null) n = Math.Max(n, s.Werte.Length);
            return RasterAus(n);
        }

        /// <summary>
        /// Der Dateiinhalt: Kopfzeile und je Stützstelle eine Zeile; eine kürzere Reihe bleibt in den
        /// übrigen Zeilen leer. Zeilenende <c>\r\n</c> wie der StreamWriter des Bestandsexports.
        /// </summary>
        public static string Text(Zeitraster raster, IReadOnlyList<ZeitreihenSpalte> spalten)
        {
            CultureInfo kultur = new CultureInfo("de-DE");
            var sb = new StringBuilder();
            sb.Append(Rasterkopf(raster));
            var zaehler = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int zeilen = 0;
            foreach (ZeitreihenSpalte s in spalten)
            {
                sb.Append(SEP).Append(Spaltenkopf(s, zaehler));
                if (s.Werte != null) zeilen = Math.Max(zeilen, s.Werte.Length);
            }
            sb.Append("\r\n");

            // Die Betrachtungsjahre zählen wie Tafel und Bild ab dem Investitionsjahr 0; jedes
            // andere Raster zählt seine Stützstellen ab 1.
            int erste = Erste(raster);
            for (int i = 0; i < zeilen; i++)
            {
                sb.Append((i + erste).ToString(CultureInfo.InvariantCulture));
                foreach (ZeitreihenSpalte s in spalten)
                {
                    sb.Append(SEP);
                    if (s.Werte != null && i < s.Werte.Length) sb.Append(Zahl(s.Werte[i], kultur));
                }
                sb.Append("\r\n");
            }
            return sb.ToString();
        }

        /// <summary>
        /// Die Nummer der ersten Zeile: <see cref="Zeitraster.Jahr"/> beginnt mit dem Jahr 0 (das
        /// Investitionsjahr der Mehrjahrestafel, des Zahlungsstroms und des Kapitalwertverlaufs),
        /// jedes andere Raster mit 1.
        /// </summary>
        public static int Erste(Zeitraster raster) => raster == Zeitraster.Jahr ? 0 : 1;

        /// <summary>Ein Wert der Datei: Dezimalkomma, Format <c>0.0##</c>; nicht endlich = leere Zelle.</summary>
        private static string Zahl(double wert, CultureInfo kultur)
            => double.IsFinite(wert) ? wert.ToString("0.0##", kultur) : "";

        /// <summary>
        /// <b>Die Tafel eines Kalenderteppichs</b> — eine Tabelle wie das Bild: Kopf
        /// „Datum;Wochentag;0 h;…;23 h“ (aus den Ressourcen, je Stundenspalte mit „[Einheit]“, wenn die
        /// Tafel eine trägt), danach 365 Zeilen, je Tag des Gemeinjahrs das Datum „TT.MM.“ ohne Jahr
        /// (kein Schaltjahr), das Wochentagskürzel aus dem Wochentagsraster der Tafel
        /// (<c>KOND_MSG_TEPPICH_WOCHENTAGE</c>) und die 24 Stundenwerte. Zahlformat, Trenner und
        /// Zeilenende wie <see cref="Text"/>; eine Stunde „aus“ bleibt leer.
        /// </summary>
        public static string Kalenderteppich(Tagesstundentafel tafel)
            => Kalenderteppich(tafel, MyResource.Resource.CSV_KOPF_DATUM, MyResource.Resource.CSV_KOPF_WOCHENTAG,
                               MyResource.Resource.CSV_KOPF_TAGESSTUNDE, MyResource.Resource.KOND_MSG_TEPPICH_WOCHENTAGE);

        /// <summary>Wie <see cref="Kalenderteppich(Tagesstundentafel)"/> mit ausdrücklichen Kopftexten.</summary>
        /// <param name="kopfDatum">Kopf der Datumsspalte.</param>
        /// <param name="kopfWochentag">Kopf der Wochentagsspalte.</param>
        /// <param name="kopfStunde">Kopf einer Stundenspalte, {0} = Stunde 0…23.</param>
        /// <param name="wochentage">Sieben Kürzel ab Montag, durch Semikolon getrennt.</param>
        public static string Kalenderteppich(Tagesstundentafel tafel, string kopfDatum, string kopfWochentag,
                                             string kopfStunde, string wochentage)
        {
            ArgumentNullException.ThrowIfNull(tafel);
            const int TAGE = 365, STUNDEN = 24;
            CultureInfo kultur = new CultureInfo("de-DE");
            string[] kuerzel = (wochentage ?? "").Split(';');
            if (kuerzel.Length != 7) kuerzel = new[] { "Mo", "Di", "Mi", "Do", "Fr", "Sa", "So" };
            string einheit = Entschaerft(tafel.Einheit);

            var sb = new StringBuilder();
            sb.Append(Entschaerft(kopfDatum)).Append(SEP).Append(Entschaerft(kopfWochentag));
            for (int h = 0; h < STUNDEN; h++)
            {
                sb.Append(SEP).Append(Entschaerft(string.Format(CultureInfo.InvariantCulture, kopfStunde ?? "{0} h", h)));
                if (einheit.Length > 0) sb.Append(" [").Append(einheit).Append(']');
            }
            sb.Append("\r\n");

            double[] werte = tafel.Werte ?? Array.Empty<double>();
            int w0 = ((tafel.WochentagDesErstenTags % 7) + 7) % 7;
            int tag = 0;
            for (int m = 0; m < 12; m++)
                for (int t = 1; t <= Feiertage.TageJeMonat[m]; t++, tag++)
                {
                    sb.Append(t.ToString("00", CultureInfo.InvariantCulture)).Append('.')
                      .Append((m + 1).ToString("00", CultureInfo.InvariantCulture)).Append('.');
                    sb.Append(SEP).Append(Entschaerft(kuerzel[(w0 + tag) % 7]));
                    for (int h = 0; h < STUNDEN; h++)
                    {
                        sb.Append(SEP);
                        int i = tag * STUNDEN + h;
                        if (i < werte.Length) sb.Append(Zahl(werte[i], kultur));
                    }
                    sb.Append("\r\n");
                }
            System.Diagnostics.Debug.Assert(tag == TAGE);
            return sb.ToString();
        }

        /// <summary>Ein Kopftext ohne Trennzeichen und Umbruch.</summary>
        private static string Entschaerft(string text)
            => (text ?? "").Replace(SEP, ",").Replace("\r", " ").Replace("\n", " ").Trim();

        /// <summary>„Name [Einheit]“, entschärft (kein Trennzeichen, kein Umbruch) und eindeutig (_2, _3 …).</summary>
        private static string Spaltenkopf(ZeitreihenSpalte s, Dictionary<string, int> zaehler)
        {
            string name = (s.Name ?? "").Replace(SEP, ",").Replace("\r", " ").Replace("\n", " ").Trim();
            if (name.Length == 0) name = "Reihe";
            string einheit = (s.Einheit ?? "").Replace(SEP, ",").Trim();
            if (einheit.Length > 0) name += " [" + einheit + "]";
            if (zaehler.TryGetValue(name, out int n))
            {
                zaehler[name] = n + 1;
                return name + "_" + (n + 1);
            }
            zaehler[name] = 1;
            return name;
        }

        /// <summary>
        /// Ein Dateiname aus einem Anzeigetitel: Zeichen außerhalb von Buchstaben, Ziffern, „-“
        /// werden zu „_“, Umlaute bleiben (dasselbe Muster wie <c>Energiebedarf_Projekt_{0}.csv</c>).
        /// </summary>
        public static string Dateistamm(string titel)
        {
            var sb = new StringBuilder();
            foreach (char c in titel ?? "")
                sb.Append(char.IsLetterOrDigit(c) || c == '-' ? c : '_');
            string stamm = sb.ToString();
            while (stamm.Contains("__")) stamm = stamm.Replace("__", "_");
            stamm = stamm.Trim('_');
            return stamm.Length == 0 ? "Zeitreihe" : stamm;
        }
    }
}
