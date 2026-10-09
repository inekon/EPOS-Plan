namespace EPOS.UI.Bausteine
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>Die zwei Bereiche der <see cref="Zweispaltenauswahl"/>, auf die eine Wahl wirkt.</summary>
    public enum Auswahlbereich
    {
        /// <summary>Der Bereich „Im Projekt“ (oben).</summary>
        Projekt,

        /// <summary>Der Bereich „Katalog (Datenbank)“ (darunter).</summary>
        Katalog
    }

    /// <summary>Was die Detailzeile „gewählter Satz“ zeigt — die Marke im Kopf sagt, was gespeichert wird.</summary>
    public enum Satzmarke
    {
        /// <summary>Kein Satz gewählt oder der Wirt sagt es nicht: Marke „Gewählter Satz“.</summary>
        Keiner,

        /// <summary>Die Projektkopie einer Projektzeile.</summary>
        Projektsatz,

        /// <summary>Ein Satz des Katalogs.</summary>
        Katalogsatz
    }

    /// <summary>Stand des Kopfhäkchens „alle wählen“ über der gefilterten Liste.</summary>
    public enum Kopfwahl
    {
        /// <summary>Keine sichtbare Zeile gewählt.</summary>
        Keine,

        /// <summary>Ein Teil der sichtbaren Zeilen gewählt.</summary>
        Teil,

        /// <summary>Alle sichtbaren Zeilen gewählt.</summary>
        Alle
    }

    /// <summary>
    /// <b>Die Mehrfachwahl eines Bereichs der Katalogauswahl</b> (Konzept Projektdialoge mit
    /// Katalogauswahl, 4.5, Entscheid KA‑E‑5). Sie rechnet über <b>Schlüssel</b>, nicht über
    /// Listenindizes: Ein Filter blendet Zeilen aus, hebt ihre Wahl aber nicht stillschweigend
    /// auf — die Kopfleiste nennt dann „3 gewählt, 1 verborgen“.
    ///
    /// <para><b>Die Regel:</b> Klick auf die Zeile wählt allein diese Zeile, Strg+Klick und
    /// das Kästchen schalten eine Zeile um, Umschalt+Klick wählt den Bereich vom Anker bis zur
    /// Zeile in der Reihenfolge der gefilterten Liste. Das Kopfhäkchen wählt alle Zeilen der
    /// gefilterten Liste oder hebt deren Wahl auf; verborgene bleiben, wie sie sind.
    /// <see cref="Zuletzt"/> ist die zuletzt angeklickte Zeile — sie zeigt die Detailzeile.</para>
    /// </summary>
    public sealed class Bereichswahl
    {
        private readonly List<string> _gewaehlt = new();

        /// <summary>Die gewählten Schlüssel in der Reihenfolge der Wahl.</summary>
        public IReadOnlyList<string> Gewaehlte => _gewaehlt;

        /// <summary>Zahl der gewählten Zeilen, sichtbar oder nicht.</summary>
        public int Anzahl => _gewaehlt.Count;

        /// <summary>Die zuletzt angeklickte Zeile (Anker des Umschalt-Klicks); <c>null</c> vor dem ersten Klick.</summary>
        public string? Zuletzt { get; private set; }

        /// <summary>Ist diese Zeile gewählt?</summary>
        public bool IstGewaehlt(string schluessel) => _gewaehlt.Contains(schluessel);

        /// <summary>
        /// Ein Klick auf eine Zeile. <paramref name="sichtbar"/> ist die gefilterte Liste in
        /// Anzeigereihenfolge — auf ihr rechnet der Bereich des Umschalt-Klicks.
        /// </summary>
        public void Klick(string schluessel, bool strg, bool umschalt, IReadOnlyList<string> sichtbar)
        {
            ArgumentNullException.ThrowIfNull(schluessel);
            ArgumentNullException.ThrowIfNull(sichtbar);

            if (umschalt && Zuletzt is not null)
            {
                int von = IndexIn(sichtbar, Zuletzt), bis = IndexIn(sichtbar, schluessel);
                if (von >= 0 && bis >= 0)
                {
                    if (!strg) _gewaehlt.Clear();
                    for (int i = Math.Min(von, bis); i <= Math.Max(von, bis); i++)
                        Hinzu(sichtbar[i]);
                    return;                       // der Anker bleibt, wo er war
                }
            }

            if (strg) Umschalten(schluessel);
            else
            {
                _gewaehlt.Clear();
                _gewaehlt.Add(schluessel);
                Zuletzt = schluessel;
            }
        }

        /// <summary>Schaltet eine Zeile um (Kästchen, Strg+Klick, Leertaste).</summary>
        public void Umschalten(string schluessel)
        {
            ArgumentNullException.ThrowIfNull(schluessel);
            if (!_gewaehlt.Remove(schluessel)) _gewaehlt.Add(schluessel);
            Zuletzt = schluessel;
        }

        /// <summary>
        /// Das Kopfhäkchen (und Strg+A): wählt alle Zeilen der gefilterten Liste oder hebt
        /// deren Wahl auf. Verborgene Zeilen behalten ihren Stand.
        /// </summary>
        public void Alle(bool waehlen, IReadOnlyList<string> sichtbar)
        {
            ArgumentNullException.ThrowIfNull(sichtbar);
            if (waehlen) foreach (var s in sichtbar) Hinzu(s);
            else _gewaehlt.RemoveAll(s => sichtbar.Contains(s));
        }

        /// <summary>Stand des Kopfhäkchens über der gefilterten Liste.</summary>
        public Kopfwahl Kopfstand(IReadOnlyList<string> sichtbar)
        {
            ArgumentNullException.ThrowIfNull(sichtbar);
            int n = sichtbar.Count(IstGewaehlt);
            return n == 0 ? Kopfwahl.Keine : n == sichtbar.Count ? Kopfwahl.Alle : Kopfwahl.Teil;
        }

        /// <summary>Gewählte Zeilen, die der Filter gerade verbirgt.</summary>
        public int Verborgen(IReadOnlyList<string> sichtbar)
        {
            ArgumentNullException.ThrowIfNull(sichtbar);
            return _gewaehlt.Count(s => !sichtbar.Contains(s));
        }

        /// <summary>
        /// Übernimmt die Wahl einer Liste, die ihre Kästchen selbst führt (Kästchenmodus der
        /// <c>Katalogliste</c>): genau diese Schlüssel, in dieser Reihenfolge.
        /// </summary>
        public void Setzen(IEnumerable<string> schluessel)
        {
            ArgumentNullException.ThrowIfNull(schluessel);
            _gewaehlt.Clear();
            foreach (var s in schluessel) Hinzu(s);
            if (_gewaehlt.Count > 0 && (Zuletzt is null || !_gewaehlt.Contains(Zuletzt)))
                Zuletzt = _gewaehlt[^1];
        }

        /// <summary>Hebt jede Wahl auf (nach Übernehmen, Entfernen, Löschen).</summary>
        public void Leeren()
        {
            _gewaehlt.Clear();
            Zuletzt = null;
        }

        /// <summary>Nimmt Schlüssel heraus, die es nicht mehr gibt.</summary>
        public void Bereinigen(IEnumerable<string> vorhanden)
        {
            var menge = new HashSet<string>(vorhanden);
            _gewaehlt.RemoveAll(s => !menge.Contains(s));
            if (Zuletzt is not null && !menge.Contains(Zuletzt)) Zuletzt = null;
        }

        private void Hinzu(string schluessel)
        {
            if (!_gewaehlt.Contains(schluessel)) _gewaehlt.Add(schluessel);
        }

        private static int IndexIn(IReadOnlyList<string> liste, string schluessel)
        {
            for (int i = 0; i < liste.Count; i++)
                if (liste[i] == schluessel) return i;
            return -1;
        }
    }
}
