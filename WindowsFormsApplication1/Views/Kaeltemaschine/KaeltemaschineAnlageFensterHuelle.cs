using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Windows-Wege des Erzeugerdialogs „Kältemaschinen im Projekt"</b> (Katalogauswahl V1, Stufe 5): die
    /// plattformfreien Gaben der <see cref="KaeltemaschineAnlageHuelle"/> und dazu, was nur die Windows-Schale
    /// beisteuert — die Kostenknöpfe (Investition, Betrieb) über das Kostenfenster der Anlage
    /// (<see cref="ErzeugerKostenwege"/>, Kostenkomponente 11) und der Katalogeditor samt „Import…" über den
    /// Dateiwähler (<see cref="KaeltemaschineKatalogFensterHuelle.Gaben"/>). Ohne diese Naht (iOS) fehlen die
    /// Kostenknöpfe, und der Katalogeditor lehnt „Import…" benannt ab.
    /// </summary>
    internal static class KaeltemaschineAnlageFensterHuelle
    {
        /// <summary>Die Gaben des Dialogs für ein Projekt; <c>null</c> = kein Projekt.</summary>
        internal static IReadOnlyDictionary<string, object> Gaben(int projektId)
        {
            IReadOnlyDictionary<string, object> basis = KaeltemaschineAnlageHuelle.Gaben(projektId);
            if (basis == null) return null;
            return new Dictionary<string, object>(basis)
            {
                ["KostenOeffnen"] = new Func<int, bool, Task>(
                    (anlagenId, betrieb) => ErzeugerKostenwege.Kosten(null, projektId, DbWerte.ERZEUGER_KAELTEMASCHINE,
                                                                     anlagenId, betrieb)),
                ["KatalogGaben"] = new Func<IReadOnlyDictionary<string, object>>(KaeltemaschineKatalogFensterHuelle.Gaben)
            };
        }
    }
}
