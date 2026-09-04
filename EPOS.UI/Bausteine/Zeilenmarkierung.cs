using System;
using System.Collections.Generic;
using System.Linq;

namespace EPOS.UI.Bausteine
{
    /// <summary>
    /// Die Mehrfachmarkierung einer Rasterliste (iU9-W13.0l).
    ///
    /// <para><b>Warum es sie gibt.</b> Vier Einlesemasken des Bestands fuehren
    /// eine <c>ListBox</c> mit <c>SelectionMode.MultiExtended</c>: Ein Klick
    /// waehlt EINE Zeile, <c>Strg</c> nimmt eine dazu oder weg, <c>Umschalt</c>
    /// waehlt den Bereich vom zuletzt gesetzten Anker bis hierher. Windows
    /// erledigt das im Steuerelement; ein <see cref="EPOS.UI.Standards.Raster{TZeile}"/>
    /// kennt keine Markierung und kann es nicht. Diese Regel steht deshalb
    /// einmal hier statt zweimal in <c>KatalogImportDialog</c> und
    /// <c>PvModulImportDialog</c>.</para>
    ///
    /// <para><b>Sie zaehlt Indizes, keine Zeilen.</b> Der Index ist der Platz in
    /// der GEFILTERTEN Anzeigeliste; welcher Quellsatz dahintersteht, weiss der
    /// Wirt (im Bestand die Zuordnung <c>_anzeigeIndex</c>). Damit bleibt die
    /// Markierung von der Zeilenklasse unabhaengig und der Baustein ohne
    /// Fachbezug.</para>
    ///
    /// <para><b>Der Anker ueberlebt einen Filterwechsel nicht.</b> Wer die
    /// Anzeigeliste neu aufbaut, ruft <see cref="AufAnzahlBegrenzen"/>: Was
    /// hinter der neuen Liste liegt, faellt aus der Markierung, und der Anker
    /// wird gueltig oder null. Das ist dieselbe Zusage wie
    /// <c>VdiAuswahlFilter.QuellIndizes</c> im Kern — eine veraltete Markierung
    /// bleibt ohne Wirkung, statt den falschen Datensatz zu treffen.</para>
    /// </summary>
    public sealed class Zeilenmarkierung
    {
        private readonly SortedSet<int> _gewaehlt = new SortedSet<int>();
        private int? _anker;

        /// <summary>Die markierten Anzeigeindizes, aufsteigend.</summary>
        public IReadOnlyList<int> Gewaehlt => _gewaehlt.ToList();

        /// <summary>Zahl der markierten Zeilen.</summary>
        public int Anzahl => _gewaehlt.Count;

        /// <summary>Der Anker der Bereichswahl, oder <c>null</c>.</summary>
        public int? Anker => _anker;

        /// <summary>Ist diese Anzeigezeile markiert?</summary>
        public bool IstGewaehlt(int index) => _gewaehlt.Contains(index);

        /// <summary>
        /// Ein Klick auf die Zeile <paramref name="index"/>.
        ///
        /// <para>Ohne Zusatztaste: nur diese Zeile, der Anker steht hier.
        /// Mit <paramref name="strg"/>: diese Zeile umschalten, der Anker steht
        /// hier. Mit <paramref name="umschalt"/>: der Bereich vom Anker bis
        /// hierher, der Anker BLEIBT stehen — so laesst sich der Bereich mit
        /// weiteren Umschalt-Klicks vergroessern und verkleinern, wie in der
        /// <c>ListBox</c>.</para>
        ///
        /// <para>Umschalt ohne Anker verhaelt sich wie ein einfacher Klick.</para>
        /// </summary>
        public void Anklicken(int index, bool strg, bool umschalt)
        {
            if (index < 0) return;

            if (umschalt && _anker.HasValue)
            {
                int von = Math.Min(_anker.Value, index);
                int bis = Math.Max(_anker.Value, index);
                _gewaehlt.Clear();
                for (int i = von; i <= bis; i++) _gewaehlt.Add(i);
                return;
            }

            if (strg)
            {
                if (!_gewaehlt.Remove(index)) _gewaehlt.Add(index);
                _anker = index;
                return;
            }

            _gewaehlt.Clear();
            _gewaehlt.Add(index);
            _anker = index;
        }

        /// <summary>Alle Zeilen von 0 bis <paramref name="anzahl"/>-1 markieren.</summary>
        public void AlleWaehlen(int anzahl)
        {
            _gewaehlt.Clear();
            for (int i = 0; i < anzahl; i++) _gewaehlt.Add(i);
            _anker = anzahl > 0 ? 0 : (int?)null;
        }

        /// <summary>Markierung und Anker aufheben.</summary>
        public void Leeren()
        {
            _gewaehlt.Clear();
            _anker = null;
        }

        /// <summary>
        /// Nach einem Filterwechsel: alles ab <paramref name="anzahl"/> faellt aus
        /// der Markierung, ein ungueltig gewordener Anker faellt weg.
        /// </summary>
        public void AufAnzahlBegrenzen(int anzahl)
        {
            if (anzahl < 0) anzahl = 0;
            foreach (int i in _gewaehlt.Where(i => i >= anzahl).ToList()) _gewaehlt.Remove(i);
            if (_anker.HasValue && _anker.Value >= anzahl) _anker = null;
        }

        /// <summary>
        /// Bildet die markierten Anzeigezeilen auf die Quellindizes ab — der
        /// Zwilling von <c>VdiAuswahlFilter.QuellIndizes</c> fuer die
        /// Razor-Seite. Zeilen ausserhalb der Zuordnung werden uebergangen,
        /// jeder Quellindex kommt hoechstens einmal vor.
        /// </summary>
        public List<int> QuellIndizes(IReadOnlyList<int> anzeigeIndex)
        {
            var treffer = new List<int>();
            if (anzeigeIndex == null) return treffer;

            foreach (int zeile in _gewaehlt)
            {
                if (zeile < 0 || zeile >= anzeigeIndex.Count) continue;
                if (!treffer.Contains(anzeigeIndex[zeile])) treffer.Add(anzeigeIndex[zeile]);
            }
            return treffer;
        }
    }
}
