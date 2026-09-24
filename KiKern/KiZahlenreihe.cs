using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace KiKern
{
    /// <summary>
    /// Die FORM einer Zahlenreihe auf einer Maske (Welle #458 Stufe 3b): wie viele Werte
    /// sie traegt und wie jede Stelle heisst - „Januar" bis „Dezember", „Stunde 1" bis
    /// „Stunde 24", „Montag, Stunde 1" bis „Sonntag, Stunde 24".
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Warum ein eigener Feldtyp und keine Spalte.</b> Die Spaltenform
    /// (<see cref="KiEigenschaftspfad.Sammlungszeichen"/>) traegt Tabellen mit BENANNTEN
    /// Zeilen - Kostenpositionen, Stuetzstellen einer Kennlinie, Ferienzeitraeume: Jede
    /// Zeile wird ein eigenes Feld mit eigener Bestaetigungszeile. Fuer eine GLEICHARTIGE
    /// Folge von Zahlen traegt sie nicht: Zwoelf Monatswerte waeren zwoelf Felder, eine
    /// Wochenreihe 168 - 168 Zuweisungen „feld=wert" in <c>formular_ausfuellen</c> (weit
    /// ueber dessen 2000 Zeichen), 168 Zeilen in <c>dialog_lesen</c> und 168 Zeilen in der
    /// Bestaetigung. Die Zahlenreihe ist deshalb EIN Feld mit EINEM Wert - der Liste -, und
    /// gesetzt wird sie mit <c>reihe_setzen</c>: ganz oder ab einer Stelle.
    /// </para>
    /// <para>
    /// <b>Die Stellen stehen in der Deklaration, nicht im Aufruf.</b> Laenge und Namen
    /// sind Eigenschaften der Maske (zwoelf Monate, 24 Stunden, 7 × 24 Wochenstunden); das
    /// Modell nennt nur Zahlen und, fuer einen Ausschnitt, die erste Stelle. So prueft die
    /// Laenge der Kern und nicht die Hoffnung.
    /// </para>
    /// <para>
    /// <b>Der Text entsteht an drei Stellen, alle hier oder in <see cref="KiFeldBlock"/>:</b>
    /// <see cref="Liste"/> ist die VOLLE Reihe (lesen, Feldblock an das Modell),
    /// <see cref="Kurz"/> die gekuerzte fuer die Angaben der Bestaetigung,
    /// <see cref="KiFeldBlock.Reihe"/> die geaenderten Stellen „alt → neu". Getrennt wird
    /// mit Strichpunkt - das Komma ist in de-DE das Dezimalzeichen.
    /// </para>
    /// </remarks>
    public sealed class KiZahlenreihe
    {
        /// <summary>Hoechstlaenge einer Zahlenreihe - Jahresreihen gehoeren in den Dateiweg.</summary>
        public const int MaxLaenge = 1000;

        /// <summary>Trennt die Werte einer Reihe im Text.</summary>
        public const string Trenner = "; ";

        /// <summary>So viele Werte bzw. geaenderte Stellen zeigt eine gekuerzte Anzeige.</summary>
        public const int Kurzlaenge = 12;

        /// <summary>Legt die Form einer Zahlenreihe an.</summary>
        /// <param name="stellen">
        /// Der Name jeder Stelle in ihrer Reihenfolge; die Zahl der Namen IST die Laenge.
        /// </param>
        public KiZahlenreihe(IReadOnlyList<string> stellen)
        {
            if (stellen == null) throw new ArgumentNullException(nameof(stellen));
            if (stellen.Count < 2)
                throw new ArgumentException("Eine Zahlenreihe hat mindestens zwei Stellen.", nameof(stellen));
            if (stellen.Count > MaxLaenge)
                throw new ArgumentException(
                    "Eine Zahlenreihe hat hoechstens " + MaxLaenge.ToString(CultureInfo.InvariantCulture) +
                    " Stellen.", nameof(stellen));

            var namen = new string[stellen.Count];
            for (int i = 0; i < stellen.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(stellen[i]))
                    throw new ArgumentException(
                        "Die Stelle " + (i + 1).ToString(CultureInfo.InvariantCulture) +
                        " der Zahlenreihe braucht einen Namen.", nameof(stellen));
                namen[i] = stellen[i].Trim();
            }

            Stellen = namen;
        }

        /// <summary>Die Namen der Stellen in ihrer Reihenfolge.</summary>
        public IReadOnlyList<string> Stellen { get; }

        /// <summary>Die Zahl der Werte.</summary>
        public int Laenge => Stellen.Count;

        /// <summary>Der Name der Stelle mit dieser Nummer (bei 1 beginnend); sonst die Nummer.</summary>
        public string Stellenname(int nummer)
            => nummer >= 1 && nummer <= Laenge
                   ? Stellen[nummer - 1]
                   : nummer.ToString(CultureInfo.InvariantCulture);

        /// <summary>„12 Werte, Januar bis Dezember" - der Umfang fuer Erklaerung und Lesen.</summary>
        public string Umfang()
            => string.Format(CultureInfo.CurrentCulture, KiTexte.ReiheUmfang,
                             Laenge, Stellen[0], Stellen[Laenge - 1]);

        /// <inheritdoc/>
        public override string ToString() => Umfang();

        // ================================================================== Werte

        /// <summary>
        /// Die Zahlen eines Rohwertes - <c>double?[]</c>, <c>double[]</c>, <c>int?[]</c>
        /// oder jede andere Folge von Zahlen; <c>null</c>, wenn der Wert keine ist.
        /// </summary>
        /// <remarks>
        /// Ein leeres Glied bleibt LEER (<c>null</c>) und wird nicht zu 0: Ein leeres
        /// Monatsfeld ist auf der Maske etwas anderes als eine Null, und der Assistent soll
        /// dasselbe lesen wie der Anwender.
        /// </remarks>
        public static IReadOnlyList<double?>? Werte(object? roh)
        {
            if (roh == null || roh is string) return null;
            if (!(roh is IEnumerable folge)) return null;

            var liste = new List<double?>();
            foreach (object? glied in folge)
            {
                switch (glied)
                {
                    case null: liste.Add(null); break;
                    case double d: liste.Add(d); break;
                    case float f: liste.Add(f); break;
                    case decimal m: liste.Add((double)m); break;
                    case long l: liste.Add(l); break;
                    case int i: liste.Add(i); break;
                    case short s: liste.Add(s); break;
                    default: return null;
                }
            }

            return liste;
        }

        /// <summary>Eine Zahl der Reihe als Anzeigetext; leer bei einem leeren Glied.</summary>
        public static string Zahl(double? wert, CultureInfo kultur)
            => wert.HasValue ? wert.Value.ToString(kultur ?? CultureInfo.CurrentCulture) : "";

        /// <summary>
        /// Die VOLLE Reihe als Text: jeder Wert, Strichpunkt getrennt, ein leeres Glied
        /// benannt. Das ist, was der Assistent liest.
        /// </summary>
        public static string Liste(IReadOnlyList<double?>? werte, CultureInfo kultur)
        {
            if (werte == null || werte.Count == 0) return "";

            var sb = new StringBuilder();
            for (int i = 0; i < werte.Count; i++)
            {
                if (i > 0) sb.Append(Trenner);
                string text = Zahl(werte[i], kultur);
                sb.Append(text.Length == 0 ? KiTexte.WertLeer : text);
            }
            return sb.ToString();
        }

        /// <summary>
        /// Die GEKUERZTE Reihe: die ersten <paramref name="hoechstens"/> Werte und die
        /// Gesamtzahl - fuer die Angaben der Bestaetigung.
        /// </summary>
        public static string Kurz(IReadOnlyList<double?>? werte, CultureInfo kultur,
                                  int hoechstens = Kurzlaenge)
        {
            if (werte == null || werte.Count == 0) return "";
            if (hoechstens < 1) hoechstens = 1;
            if (werte.Count <= hoechstens) return Liste(werte, kultur);

            var anfang = new List<double?>(hoechstens);
            for (int i = 0; i < hoechstens; i++) anfang.Add(werte[i]);

            return Liste(anfang, kultur) + Trenner +
                   string.Format(kultur ?? CultureInfo.CurrentCulture, KiTexte.ReiheGekuerzt, werte.Count);
        }
    }
}
