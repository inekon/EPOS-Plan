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
        Woche = 6
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

            for (int i = 0; i < zeilen; i++)
            {
                sb.Append((i + 1).ToString(CultureInfo.InvariantCulture));
                foreach (ZeitreihenSpalte s in spalten)
                {
                    sb.Append(SEP);
                    if (s.Werte != null && i < s.Werte.Length && double.IsFinite(s.Werte[i]))
                        sb.Append(s.Werte[i].ToString("0.0##", kultur));
                }
                sb.Append("\r\n");
            }
            return sb.ToString();
        }

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
