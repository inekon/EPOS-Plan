using System;
using System.Collections.Generic;
using System.Globalization;

namespace KiKern
{
    /// <summary>
    /// Ein GEPRUEFTER Aufruf: Aktionsname plus die bereits in ihre Zieltypen ueberfuehrten
    /// Parameterwerte.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Ein <see cref="KiAufruf"/> entsteht ausschliesslich in <see cref="KiPruefung"/>.
    /// Dass es keinen oeffentlichen Weg gibt, ihn von Hand zusammenzusetzen, ist Absicht:
    /// Der Ausfuehrer darf sich darauf verlassen, dass Pflichtfelder da sind, Typen
    /// stimmen und Wertebereiche eingehalten wurden - er prueft nicht ein zweites Mal.
    /// </para>
    /// <para>
    /// Werte liegen typrein vor: <see cref="KiParameterTyp.Ganzzahl"/> als
    /// <c>long</c>, <see cref="KiParameterTyp.Zahl"/> als <c>double</c>,
    /// <see cref="KiParameterTyp.Text"/> und <see cref="KiParameterTyp.Aufzaehlung"/>
    /// als <c>string</c>, <see cref="KiParameterTyp.Wahrheitswert"/> als <c>bool</c>,
    /// <see cref="KiParameterTyp.GanzzahlListe"/> als <c>long[]</c>.
    /// </para>
    /// </remarks>
    public sealed class KiAufruf
    {
        private readonly Dictionary<string, object> _werte;

        internal KiAufruf(KiAktion aktion, Dictionary<string, object> werte)
        {
            Aktion = aktion ?? throw new ArgumentNullException(nameof(aktion));
            _werte = werte ?? throw new ArgumentNullException(nameof(werte));
        }

        /// <summary>Die aufgerufene Aktion.</summary>
        public KiAktion Aktion { get; }

        /// <summary>Name der aufgerufenen Aktion.</summary>
        public string Name => Aktion.Name;

        /// <summary>Die gepruefen Werte, Schluessel ist der Parametername.</summary>
        public IReadOnlyDictionary<string, object> Werte => _werte;

        /// <summary>Fuehrt der Aufruf diesen Parameter?</summary>
        public bool Hat(string parameter) => _werte.ContainsKey(parameter);

        /// <summary>Ganzzahliger Wert; <paramref name="ersatz"/>, wenn der Parameter fehlt.</summary>
        public long Ganzzahl(string parameter, long ersatz = 0)
            => _werte.TryGetValue(parameter, out object? o) && o is long l ? l : ersatz;

        /// <summary>Ganzzahliger Wert als <c>int</c> - der Regelfall fuer IDs des Bestands.</summary>
        public int Id(string parameter, int ersatz = 0)
        {
            long l = Ganzzahl(parameter, ersatz);
            if (l > int.MaxValue || l < int.MinValue) return ersatz;
            return (int)l;
        }

        /// <summary>Gleitkommawert; <paramref name="ersatz"/>, wenn der Parameter fehlt.</summary>
        public double Zahl(string parameter, double ersatz = 0.0)
        {
            if (!_werte.TryGetValue(parameter, out object? o)) return ersatz;
            if (o is double d) return d;
            if (o is long l) return l;
            return ersatz;
        }

        /// <summary>Text- oder Aufzaehlungswert; <paramref name="ersatz"/>, wenn der Parameter fehlt.</summary>
        public string Text(string parameter, string ersatz = "")
            => _werte.TryGetValue(parameter, out object? o) && o is string s ? s : ersatz;

        /// <summary>Wahrheitswert; <paramref name="ersatz"/>, wenn der Parameter fehlt.</summary>
        public bool Wahrheit(string parameter, bool ersatz = false)
            => _werte.TryGetValue(parameter, out object? o) && o is bool b ? b : ersatz;

        /// <summary>Zahlenliste; leeres Feld, wenn der Parameter fehlt.</summary>
        public long[] GanzzahlListe(string parameter)
            => _werte.TryGetValue(parameter, out object? o) && o is long[] a ? a : Array.Empty<long>();

        /// <summary>Zahlenliste als <c>int</c>-Feld - der Regelfall fuer Projektlisten.</summary>
        public int[] IdListe(string parameter)
        {
            long[] roh = GanzzahlListe(parameter);
            var ziel = new int[roh.Length];
            for (int i = 0; i < roh.Length; i++) ziel[i] = (int)roh[i];
            return ziel;
        }

        /// <summary>
        /// Die Parameterwerte als kompaktes JSON-Objekt - das Feld „Parameter" der
        /// Protokollzeile (Fachkonzept 3.6). Zahlen invariant, Reihenfolge wie deklariert.
        /// </summary>
        public string AlsJson() => KiSchema.WerteAlsJson(this);

        /// <summary>
        /// Die Parameterwerte als Klartextliste fuer Bestaetigung und Chat - Zahlen in der
        /// uebergebenen Kultur (Fachkonzept 3.2, Kulturregel).
        /// </summary>
        public IReadOnlyList<string> AlsKlartext(CultureInfo? kultur = null)
        {
            CultureInfo k = kultur ?? CultureInfo.CurrentCulture;
            var zeilen = new List<string>();

            foreach (KiParameter p in Aktion.Parameter)
            {
                if (!_werte.TryGetValue(p.Name, out object? wert)) continue;
                string text = KiSchema.WertAlsText(wert, k);
                if (p.Einheit.Length > 0) text += " " + p.Einheit;
                zeilen.Add(p.Anzeigename + ": " + text);
            }
            return zeilen;
        }
    }
}
