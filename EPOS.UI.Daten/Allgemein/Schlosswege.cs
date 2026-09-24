using System;
using System.Collections.Generic;
using EPOS.UI.Bausteine;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Der Weg „Schloss setzen…" / „Schloss aufheben…" für die zehn Verwaltungen</b>
    /// (Konzept Administrationsdialoge, Entscheid AD-Q15) — aus dem Einzeiler eines
    /// Stamm-Controllers der <see cref="Schlossweg"/> der Oberfläche.
    ///
    /// <para><b>EINE Stelle für alle Hüllen</b>, die plattformfreien hier und die der
    /// Windows-Schale: Sie bildet das Kernergebnis
    /// (<see cref="Auslieferungskennzeichen.Ergebnis"/>) auf das der Oberfläche ab und stellt
    /// die Frage nach dem Lesemodus der Lizenz über die <see cref="Schreibnaht"/> — die Ansicht
    /// stellt keine Lizenzfragen. Jede Hülle reicht nur ihren Controller herein:
    /// <c>Schlosswege.Aus(BHKWStammCtrl.SchlossSetzen)</c>.</para>
    /// </summary>
    internal static class Schlosswege
    {
        /// <summary>Der Weg zu einem Einzeiler <c>…StammCtrl.SchlossSetzen</c>.</summary>
        internal static Schlossweg Aus(Func<IReadOnlyList<int>, bool, Auslieferungskennzeichen.Ergebnis> setzen)
        {
            if (setzen is null) throw new ArgumentNullException(nameof(setzen));
            return new Schlossweg((ids, gesperrt) => Abbild(setzen(ids, gesperrt)))
            {
                Lesemodus = () => !Schreibnaht.DarfSchreiben()
            };
        }

        /// <summary>Das Kernergebnis als Ergebnis der Oberfläche.</summary>
        internal static SchlossErgebnis Abbild(Auslieferungskennzeichen.Ergebnis e)
        {
            if (e is null) return new SchlossErgebnis(false, "");
            return new SchlossErgebnis(e.Ok, e.Meldung ?? "")
            {
                Geaendert = e.Geaendert ?? Array.Empty<int>(),
                Unveraendert = e.Unveraendert ?? Array.Empty<int>()
            };
        }
    }
}
