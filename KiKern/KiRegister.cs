using System;
using System.Collections;
using System.Collections.Generic;

namespace KiKern
{
    /// <summary>
    /// Das Aktionsregister: die abschliessende Liste dessen, was der Assistent tun kann.
    /// </summary>
    /// <remarks>
    /// Was hier nicht steht, gibt es fuer den Assistenten nicht (Fachkonzept 3.2). Der
    /// Kern haelt nur die Verwaltung; gefuellt wird das Register im Anwendungsprojekt
    /// (<c>Allgemein\KI\Aktionen\</c>), weil nur dort Controller und Datenbank stehen.
    /// </remarks>
    public sealed class KiRegister : IEnumerable<KiAktion>
    {
        private readonly List<KiAktion> _reihenfolge = new List<KiAktion>();
        private readonly Dictionary<string, KiAktion> _nachName =
            new Dictionary<string, KiAktion>(StringComparer.Ordinal);

        /// <summary>Nimmt eine Aktion auf. Doppelte Namen sind ein Programmierfehler.</summary>
        public KiRegister Aufnehmen(KiAktion aktion)
        {
            if (aktion == null) throw new ArgumentNullException(nameof(aktion));
            if (_nachName.ContainsKey(aktion.Name))
                throw new ArgumentException("Die Aktion '" + aktion.Name + "' ist bereits registriert.", nameof(aktion));

            _nachName.Add(aktion.Name, aktion);
            _reihenfolge.Add(aktion);
            return this;
        }

        /// <summary>Alle Aktionen in Registrierungsreihenfolge.</summary>
        public IReadOnlyList<KiAktion> Alle => _reihenfolge;

        /// <summary>Zahl der registrierten Aktionen.</summary>
        public int Anzahl => _reihenfolge.Count;

        /// <summary>Kennt das Register diesen Namen?</summary>
        public bool Kennt(string? name) => name != null && _nachName.ContainsKey(name);

        /// <summary>Liefert die Aktion, oder <c>null</c>.</summary>
        public KiAktion? Finde(string? name)
            => name != null && _nachName.TryGetValue(name, out KiAktion? a) ? a : null;

        /// <summary>Alle Aktionen einer Schutzstufe.</summary>
        public IReadOnlyList<KiAktion> NachStufe(Schutzstufe stufe)
        {
            var treffer = new List<KiAktion>();
            foreach (KiAktion a in _reihenfolge)
                if (a.Stufe == stufe) treffer.Add(a);
            return treffer;
        }

        /// <summary>
        /// Die Namen aller Aktionen, alphabetisch - fuer die Fehlermeldung bei einem
        /// unbekannten Aufruf (Fachkonzept 3.1: Rueckfrage statt stiller Ablehnung).
        /// </summary>
        public IReadOnlyList<string> Namen()
        {
            var namen = new List<string>(_nachName.Keys);
            namen.Sort(StringComparer.Ordinal);
            return namen;
        }

        /// <inheritdoc/>
        public IEnumerator<KiAktion> GetEnumerator() => _reihenfolge.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
