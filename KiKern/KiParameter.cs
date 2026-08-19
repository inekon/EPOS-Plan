using System;
using System.Collections.Generic;
using System.Globalization;

namespace KiKern
{
    /// <summary>
    /// Die zulaessigen Parameterarten. Bewusst klein gehalten: Fachkonzept 3.2 laesst
    /// nur Primitive (Zahl, Text, Wahrheitswert, Aufzaehlung) und IDs zu, die aus einer
    /// Leseaktion stammen. Alles Zusammengesetzte gehoert nicht in einen Modellaufruf.
    /// </summary>
    public enum KiParameterTyp
    {
        /// <summary>Ganze Zahl - der Regelfall fuer IDs.</summary>
        Ganzzahl = 0,

        /// <summary>Gleitkommazahl. Uebergabe invariant, Anzeige in der Anwenderkultur.</summary>
        Zahl = 1,

        /// <summary>Freier Text. Nur dort, wo der Bestand ihn ohnehin prueft.</summary>
        Text = 2,

        /// <summary>Wahrheitswert.</summary>
        Wahrheitswert = 3,

        /// <summary>Feste Werteliste. Persistenzwerte stammen aus <c>Allgemein\DbWerte.cs</c>.</summary>
        Aufzaehlung = 4,

        /// <summary>Liste ganzer Zahlen - fuer Aktionen ueber mehrere Projekte.</summary>
        GanzzahlListe = 5
    }

    /// <summary>
    /// Ein Parameter einer <see cref="KiAktion"/> - zugleich Schemaquelle, Pruefregel und
    /// Klartextbaustein (Fachkonzept 3.2, „eine Deklaration, drei Verwendungen").
    /// </summary>
    /// <remarks>
    /// Die Klasse ist unveraenderlich: einmal deklariert, kann sie zwischen Schema,
    /// Pruefung und Bestaetigungstext nicht mehr auseinanderlaufen.
    /// </remarks>
    public sealed class KiParameter
    {
        /// <summary>
        /// Erzeugt einen Parameter.
        /// </summary>
        /// <param name="name">Sprachneutraler Schluessel, ASCII, klein, mit Unterstrich.</param>
        /// <param name="typ">Parameterart.</param>
        /// <param name="erlaeuterung">Ein Satz Klartext - geht an das Modell UND in die Bestaetigung.</param>
        /// <param name="pflicht">Muss der Aufruf ihn fuehren?</param>
        /// <param name="anzeigename">Klartextname fuer Bestaetigung und Chat; leer = <paramref name="name"/>.</param>
        /// <param name="min">Untergrenze einschliesslich (nur Zahltypen).</param>
        /// <param name="max">Obergrenze einschliesslich (nur Zahltypen).</param>
        /// <param name="werte">Zulaessige Werte (nur <see cref="KiParameterTyp.Aufzaehlung"/>).</param>
        /// <param name="einheit">Einheit fuer die Anzeige, z. B. „kWh".</param>
        /// <param name="maxLaenge">Hoechstlaenge (nur <see cref="KiParameterTyp.Text"/>).</param>
        public KiParameter(string name,
                           KiParameterTyp typ,
                           string erlaeuterung,
                           bool pflicht = true,
                           string? anzeigename = null,
                           double? min = null,
                           double? max = null,
                           IReadOnlyList<string>? werte = null,
                           string? einheit = null,
                           int? maxLaenge = null)
        {
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Ein Parameter braucht einen Namen.", nameof(name));
            if (!KiName.IstGueltig(name))
                throw new ArgumentException(
                    "Parametername '" + name + "' ist nicht zulaessig (erlaubt: a-z, 0-9, _; hoechstens 64 Zeichen).",
                    nameof(name));
            if (typ == KiParameterTyp.Aufzaehlung && (werte == null || werte.Count == 0))
                throw new ArgumentException(
                    "Der Aufzaehlungsparameter '" + name + "' braucht eine nicht leere Werteliste.", nameof(werte));
            if (typ != KiParameterTyp.Aufzaehlung && werte != null)
                throw new ArgumentException(
                    "Nur Aufzaehlungsparameter duerfen eine Werteliste fuehren ('" + name + "').", nameof(werte));
            if (min.HasValue && max.HasValue && min.Value > max.Value)
                throw new ArgumentException("Untergrenze groesser als Obergrenze bei '" + name + "'.", nameof(min));

            Name = name;
            Typ = typ;
            Erlaeuterung = erlaeuterung ?? "";
            Pflicht = pflicht;
            Anzeigename = string.IsNullOrWhiteSpace(anzeigename) ? name : anzeigename!;
            Min = min;
            Max = max;
            Werte = werte ?? Array.Empty<string>();
            Einheit = einheit ?? "";
            MaxLaenge = maxLaenge;
        }

        /// <summary>Sprachneutraler Schluessel (ASCII).</summary>
        public string Name { get; }

        /// <summary>Parameterart.</summary>
        public KiParameterTyp Typ { get; }

        /// <summary>Ein Satz Klartext fuer Modell, Bestaetigung und Protokoll.</summary>
        public string Erlaeuterung { get; }

        /// <summary>Pflichtangabe?</summary>
        public bool Pflicht { get; }

        /// <summary>Klartextname fuer die Anzeige.</summary>
        public string Anzeigename { get; }

        /// <summary>Untergrenze einschliesslich, oder <c>null</c>.</summary>
        public double? Min { get; }

        /// <summary>Obergrenze einschliesslich, oder <c>null</c>.</summary>
        public double? Max { get; }

        /// <summary>Zulaessige Werte einer Aufzaehlung; sonst leer.</summary>
        public IReadOnlyList<string> Werte { get; }

        /// <summary>Einheit fuer die Anzeige; leer, wenn keine.</summary>
        public string Einheit { get; }

        /// <summary>Hoechstlaenge eines Textes, oder <c>null</c>.</summary>
        public int? MaxLaenge { get; }

        /// <summary>
        /// Die Erlaeuterung mit angehaengtem Wertebereich - genau der Text, der im Schema
        /// und in der Werkzeugliste steht.
        /// </summary>
        /// <remarks>
        /// Der Bereich steht bewusst IM BESCHREIBUNGSTEXT und nicht als
        /// <c>minimum</c>/<c>maximum</c>-Schluesselwort: Der Werkzeugkatalog des Anbieters
        /// nimmt nur eine Teilmenge des JSON-Schemas an (siehe <see cref="KiSchema"/>).
        /// Die Grenze selbst wird in C# geprueft (<see cref="KiPruefung"/>), also aus
        /// derselben Deklaration - sie kann nicht auseinanderlaufen.
        /// </remarks>
        public string SchemaBeschreibung()
        {
            string text = Erlaeuterung;
            if (Einheit.Length > 0) text += " [" + Einheit + "]";

            if (Min.HasValue && Max.HasValue)
                text += " (zulaessig " + Zahltext(Min.Value) + " bis " + Zahltext(Max.Value) + ")";
            else if (Min.HasValue)
                text += " (mindestens " + Zahltext(Min.Value) + ")";
            else if (Max.HasValue)
                text += " (hoechstens " + Zahltext(Max.Value) + ")";

            if (MaxLaenge.HasValue)
                text += " (hoechstens " + MaxLaenge.Value.ToString(CultureInfo.InvariantCulture) + " Zeichen)";

            return text;
        }

        private static string Zahltext(double wert)
        {
            return wert == Math.Floor(wert) && Math.Abs(wert) < 1e15
                ? ((long)wert).ToString(CultureInfo.InvariantCulture)
                : wert.ToString("0.####", CultureInfo.InvariantCulture);
        }
    }

    /// <summary>
    /// Namensregel fuer Aktionen und Parameter. Der Werkzeugkatalog des Anbieters laesst
    /// nur ASCII und hoechstens 64 Zeichen zu (Fachkonzept 3.2); die Regel steht hier an
    /// EINER Stelle, damit Register und Parameter dieselbe pruefen.
    /// </summary>
    public static class KiName
    {
        /// <summary>Hoechstlaenge eines Aktions- oder Parameternamens.</summary>
        public const int MaxLaenge = 64;

        /// <summary>Erlaubt sind Kleinbuchstaben a-z, Ziffern und Unterstrich; erstes Zeichen ein Buchstabe.</summary>
        public static bool IstGueltig(string? name)
        {
            if (string.IsNullOrEmpty(name)) return false;
            if (name!.Length > MaxLaenge) return false;
            if (name[0] < 'a' || name[0] > 'z') return false;

            foreach (char c in name)
            {
                bool ok = (c >= 'a' && c <= 'z') || (c >= '0' && c <= '9') || c == '_';
                if (!ok) return false;
            }
            return true;
        }
    }
}
