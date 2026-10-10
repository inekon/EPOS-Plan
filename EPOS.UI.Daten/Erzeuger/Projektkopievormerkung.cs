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
    /// <para><b>Auch die Trägervariante</b> (Nachzug zu A5): „In das Projekt übernehmen“ legt bei Kessel und BHKW
    /// vor der Kopie die Energieträgervariante an (<c>EnergietraegerVarianteCtrl.Anlegen</c>: Katalogträger,
    /// Preis und Projektzuordnung). Was davon in dieser Sitzung NEU entstand, merkt <see cref="TraegerAngelegt"/>;
    /// Abbrechen nimmt es über den zweiten Delegaten zurück (<c>EnergietraegerVarianteCtrl.AnlageZuruecknehmen</c>,
    /// der nur löscht, worauf keine Anlagenzeile und keine Preis-, Emissions- oder Ergebniszeile verweist).
    /// OK lässt die Träger stehen — sie sind dann Teil des Projekts.</para>
    ///
    /// <para><b>Auch der Pufferspeicher</b> (<c>Tab_Pufferspeicher</c>, Katalogauswahl V1 Stufe 3) legt seine Kopie beim
    /// Übernehmen an und meldet eine neue hier; sein Entfernen räumt weiter der Aufrufer nach OK über
    /// <c>PufferSpCtrl.ProjektWaisenEntfernen</c> ab.</para>
    ///
    /// <para>Plattformfrei und ohne Datenbank: Gelöscht wird über den Delegaten der Hülle
    /// (<c>…Ctrl.DeleteFromProjekt(bezeichner, projekt)</c>).</para>
    /// </summary>
    internal sealed class Projektkopievormerkung
    {
        private readonly Action<string> _loeschen;
        private readonly List<(string Bezeichner, int KopieId)> _entfernt = new List<(string, int)>();
        private readonly List<(string Bezeichner, int KopieId)> _angelegt = new List<(string, int)>();
        private readonly Action<int, bool, bool> _traegerZuruecknehmen;
        private readonly List<(int CarrierId, bool ZuordnungNeu, bool KatalogNeu)> _traeger =
            new List<(int, bool, bool)>();

        /// <param name="loeschen">Löscht die Projektkopie mit diesem Bezeichner (im Projekt der Hülle).</param>
        /// <param name="traegerZuruecknehmen">Nimmt eine neu angelegte Trägervariante zurück (Träger-Id, Zuordnung
        /// neu, Katalogträger neu); <c>null</c> = der Dialog legt keine Träger an.</param>
        internal Projektkopievormerkung(Action<string> loeschen, Action<int, bool, bool> traegerZuruecknehmen = null)
        {
            _loeschen = loeschen ?? throw new ArgumentNullException(nameof(loeschen));
            _traegerZuruecknehmen = traegerZuruecknehmen;
        }

        /// <summary>Die in dieser Sitzung neu angelegten Trägervarianten (Prüfhilfe).</summary>
        internal IReadOnlyList<(int CarrierId, bool ZuordnungNeu, bool KatalogNeu)> AngelegteTraeger => _traeger;

        /// <summary>
        /// „In das Projekt übernehmen“ hat eine Trägervariante angelegt: <paramref name="zuordnungNeu"/> = Preis und
        /// Projektzuordnung sind neu, <paramref name="katalogNeu"/> = auch der Katalogträger. Ist beides
        /// <c>false</c> (Träger war schon zugeordnet), bleibt nichts vorzumerken.
        /// </summary>
        internal void TraegerAngelegt(int carrierId, bool zuordnungNeu, bool katalogNeu)
        {
            if (carrierId > 0 && (zuordnungNeu || katalogNeu)) _traeger.Add((carrierId, zuordnungNeu, katalogNeu));
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
        /// löschen, auf die keine Zeile mehr verweist; sonst nur die neu angelegten Kopien und danach die neu
        /// angelegten Trägervarianten. Danach ist die Vormerkung leer.
        /// </summary>
        /// <param name="nochReferenziert">Verweist eine Zeile der Anlagenliste noch auf diese Kopie-Id?</param>
        internal void Abschliessen(bool ok, Func<int, bool> nochReferenziert)
        {
            IEnumerable<(string Bezeichner, int KopieId)> kandidaten = ok
                ? _entfernt.Concat(_angelegt).Where(e => nochReferenziert == null || !nochReferenziert(e.KopieId))
                : _angelegt;

            foreach (var e in kandidaten.GroupBy(e => e.KopieId).Select(g => g.First()).ToList())
                _loeschen(e.Bezeichner);

            // Abbrechen: die neu angelegten Trägervarianten zurücknehmen — NACH den Kopien, in umgekehrter
            // Reihenfolge; je Träger einmal, mit allem, was in der Sitzung an ihm neu war.
            if (!ok && _traegerZuruecknehmen != null)
            {
                foreach (var g in _traeger.GroupBy(t => t.CarrierId).Reverse().ToList())
                    _traegerZuruecknehmen(g.Key, g.Any(t => t.ZuordnungNeu), g.Any(t => t.KatalogNeu));
            }

            _entfernt.Clear();
            _angelegt.Clear();
            _traeger.Clear();
        }
    }
}
