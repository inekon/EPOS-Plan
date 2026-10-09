using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Projektkopien eines Erzeugerdialogs bis OK</b> (Anwenderwunsch 08.10.2026: „Aus dem Projekt
    /// entfernen“ und Abbrechen). Die Erzeugerdialoge mit eigener Projektkopie — Heizkessel
    /// (<c>Tab_Heizkessel</c>), BHKW (<c>Tab_BHKW</c>), Solarkollektoren (<c>Tab_Solarkollektoren</c>) — löschten
    /// die Kopie beim Entfernen sofort; ein Abbrechen danach ließ die Anlagenzeile stehen, aber ohne Kopie.
    ///
    /// <para><b>Entfernen wird nur VORGEMERKT</b> und erst beim OK ausgeführt — und dann nur für eine Kopie, auf
    /// die keine Zeile mehr verweist (mehrere Zeilen desselben Geräts teilen sich EINE Kopie). Abbrechen verwirft
    /// die Vormerkung; die Zeile steht beim nächsten Öffnen wieder da, weil der Aufrufer die Anlagenliste dann
    /// nicht zurückschreibt.</para>
    ///
    /// <para><b>„In das Projekt übernehmen“ schreibt weiter sofort</b>: Der Dialog zeigt danach den Detailblock,
    /// die Senkenzeile und „Alle Daten anzeigen“ der PROJEKTKOPIE (Lesen über ihre Id), die es dafür geben muss.
    /// Deshalb merkt sich die Vormerkung jede Kopie, die in dieser Sitzung NEU entstand (vorher gab es keine
    /// gleichnamige im Projekt): Abbrechen entfernt sie wieder, OK entfernt sie nur, wenn keine Zeile mehr auf
    /// sie verweist. Eine schon vorhandene Kopie, die die Übernahme nur wiederverwendet (der Kern kopiert
    /// idempotent über den Namen), bleibt beim Abbrechen stehen.</para>
    ///
    /// <para>Plattformfrei und ohne Datenbank: Gelöscht wird über den Delegaten der Hülle
    /// (<c>…Ctrl.DeleteFromProjekt(bezeichner, projekt)</c>).</para>
    /// </summary>
    internal sealed class Projektkopievormerkung
    {
        private readonly Action<string> _loeschen;
        private readonly List<(string Bezeichner, int KopieId)> _entfernt = new List<(string, int)>();
        private readonly List<(string Bezeichner, int KopieId)> _angelegt = new List<(string, int)>();

        /// <param name="loeschen">Löscht die Projektkopie mit diesem Bezeichner (im Projekt der Hülle).</param>
        internal Projektkopievormerkung(Action<string> loeschen)
        {
            _loeschen = loeschen ?? throw new ArgumentNullException(nameof(loeschen));
        }

        /// <summary>Die vorgemerkten Entfernungen (Prüfhilfe).</summary>
        internal IReadOnlyList<(string Bezeichner, int KopieId)> Entfernte => _entfernt;

        /// <summary>Die in dieser Sitzung neu angelegten Kopien (Prüfhilfe).</summary>
        internal IReadOnlyList<(string Bezeichner, int KopieId)> Angelegte => _angelegt;

        /// <summary>„In das Projekt übernehmen“ hat eine NEUE Projektkopie angelegt.</summary>
        internal void Angelegt(string bezeichner, int kopieId)
        {
            if (kopieId > 0) _angelegt.Add((bezeichner ?? "", kopieId));
        }

        /// <summary>„Aus dem Projekt entfernen“: die Kopie der entfernten Zeile vormerken.</summary>
        internal void Entfernt(string bezeichner, int kopieId)
        {
            if (kopieId > 0) _entfernt.Add((bezeichner ?? "", kopieId));
        }

        /// <summary>
        /// Der Dialog ist zu: <paramref name="ok"/> = mit OK — dann jede vorgemerkte und jede neu angelegte Kopie
        /// löschen, auf die keine Zeile mehr verweist; sonst nur die neu angelegten. Danach ist die Vormerkung leer.
        /// </summary>
        /// <param name="nochReferenziert">Verweist eine Zeile der Anlagenliste noch auf diese Kopie-Id?</param>
        internal void Abschliessen(bool ok, Func<int, bool> nochReferenziert)
        {
            IEnumerable<(string Bezeichner, int KopieId)> kandidaten = ok
                ? _entfernt.Concat(_angelegt).Where(e => nochReferenziert == null || !nochReferenziert(e.KopieId))
                : _angelegt;

            foreach (var e in kandidaten.GroupBy(e => e.KopieId).Select(g => g.First()).ToList())
                _loeschen(e.Bezeichner);

            _entfernt.Clear();
            _angelegt.Clear();
        }
    }
}
