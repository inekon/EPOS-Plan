using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EPOS.UI.Bausteine;
using WindowsFormsApplication1.Zeichnung;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Ganglinie in der Detailzeile eines Ganglinien-Dialogs</b> (DZ1, Konzept
    /// Projektdialoge mit Katalogauswahl 4.9) — plattformfrei: die Kennzahlen aus
    /// <see cref="GanglinienAuswertungCtrl"/>, das Zeichenmodell aus
    /// <see cref="ChartRenderer.GanglinieNormiertModell"/>, Farbwahl und Anzeigeeinheit wie in
    /// den Bedarfsansichten. Die Solarthermie- und die PV-Ganglinie gehen diesen Weg.
    /// </summary>
    internal static class GanglinienGrafikGaben
    {
        /// <summary>
        /// Die Gaben <c>Kennzahlen</c>, <c>Bildauftrag</c>, <c>FarbeSetzen</c>,
        /// <c>FarbeZuruecksetzen</c>, <c>Einheit</c> und <c>EinheitGewaehlt</c> — mit EINEM
        /// Vorrat je Dialog (gehalten wird die zuletzt gelesene Reihe).
        /// </summary>
        internal static Dictionary<string, object> Gaben(GanglinienQuelle quelle, string titel,
                                                         string achse, Farbrolle farbe)
        {
            var vorrat = new Vorrat(quelle, titel, achse, farbe);
            return new Dictionary<string, object>
            {
                ["Kennzahlen"] = new Func<GanglinienWahl, Task<GanglinienKennzahlen>>(
                    w => Task.FromResult(vorrat.Kennzahlen(w))),
                ["Bildauftrag"] = new Func<GanglinienWahl, bool, Zeichenmodell>((w, s) => vorrat.Modell(w, s, 0, 0)),
                ["BildauftragMass"] = new Func<GanglinienWahl, bool, int, int, Zeichenmodell>(vorrat.Modell),
                ["FarbeSetzen"] = new Func<Farbrolle, Farbe, Task>((r, f) =>
                {
                    Diagrammfarben.Setze(r, f);
                    return Task.CompletedTask;
                }),
                ["FarbeZuruecksetzen"] = new Func<Farbrolle, Task>(r =>
                {
                    Diagrammfarben.Zuruecksetzen(r);
                    return Task.CompletedTask;
                }),
                ["Einheit"] = BedarfEinheitWahl.Lies(),
                ["EinheitGewaehlt"] = new Action<Energieeinheit>(BedarfEinheitWahl.Schreib)
            };
        }

        /// <summary>Fügt die Gaben einem Parametersatz hinzu.</summary>
        internal static IReadOnlyDictionary<string, object> Mit(IReadOnlyDictionary<string, object> gaben,
                                                                GanglinienQuelle quelle, string titel,
                                                                string achse, Farbrolle farbe)
        {
            var alle = new Dictionary<string, object>(gaben);
            foreach (KeyValuePair<string, object> g in Gaben(quelle, titel, achse, farbe)) alle[g.Key] = g.Value;
            return alle;
        }

        /// <summary>Die gelesene Reihe der zuletzt markierten Ganglinie — genau eine.</summary>
        private sealed class Vorrat
        {
            private readonly GanglinienQuelle _quelle;
            private readonly string _titel;
            private readonly string _achse;
            private readonly Farbrolle _farbe;
            private string _schluessel;
            private GanglinienAuswertung _stand;

            internal Vorrat(GanglinienQuelle quelle, string titel, string achse, Farbrolle farbe)
            {
                _quelle = quelle;
                _titel = titel;
                _achse = achse;
                _farbe = farbe;
            }

            internal GanglinienKennzahlen Kennzahlen(GanglinienWahl wahl)
            {
                GanglinienAuswertung a = Lesen(wahl);
                if (a == null || !a.Erfolgreich) return null;
                return new GanglinienKennzahlen(a.JahresarbeitMwh, a.SpitzeKw, a.VollbenutzungsstundenH);
            }

            internal Zeichenmodell Modell(GanglinienWahl wahl, bool sortiert, int breite, int hoehe)
            {
                GanglinienAuswertung a = Lesen(wahl);
                if (a == null || !a.Erfolgreich) return null;
                var reihen = new List<ChartRenderer.Reihe>
                {
                    new ChartRenderer.Reihe(_achse, (double[])a.Stundenwerte.Clone(), _farbe)
                };
                return ChartRenderer.GanglinieNormiertModell(_titel, reihen, _achse,
                    sortiert ? ChartRenderer.Achse.Jahresstunden : ChartRenderer.Achse.Monate, sortiert,
                    breite: breite, hoehe: hoehe);
            }

            private GanglinienAuswertung Lesen(GanglinienWahl wahl)
            {
                if (wahl == null) return null;
                string schluessel = (wahl.AusKatalog ? "K|" : "P|") + wahl.GanglinieId + "|" + wahl.Bezeichner;
                if (schluessel == _schluessel) return _stand;
                _schluessel = schluessel;
                _stand = wahl.AusKatalog
                    ? GanglinienAuswertungCtrl.AusKatalog(_quelle, wahl.Bezeichner)
                    : GanglinienAuswertungCtrl.AusProjekt(_quelle, wahl.GanglinieId, wahl.Bezeichner);
                return _stand;
            }
        }
    }
}
