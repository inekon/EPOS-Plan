using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine abweichende Rangfolge</b> zweier gekoppelter Zeilen (Konzept 7.8): In <see cref="Oben"/> steht
    /// <see cref="Erste"/> über <see cref="Zweite"/>, in <see cref="Unten"/> darunter.
    /// </summary>
    public sealed record Rangabweichung(Zuordnungsschluessel Erste, Zuordnungsschluessel Zweite,
                                        Konditionierungsgroesse Oben, Konditionierungsgroesse Unten);

    public static partial class Kalenderbedienung
    {
        /// <summary>
        /// <b>Die Warnzeile der Zuordnung</b> (Konzept 7.8, Rangregel): je Paar gekoppelter Zeilen, die in zwei Größen
        /// gemeinsam stehen, aber dort in umgekehrter Folge — ein Tag, an dem sie sich überlappen, rechnet dann je
        /// Größe mit einer anderen Zeile. Leer, wenn die Folge überall dieselbe ist; jedes Paar höchstens einmal.
        /// </summary>
        public static IReadOnlyList<Rangabweichung> Rangabweichungen(Konditionierungsarbeitsstand stand, long? zone)
        {
            if (stand == null) throw new ArgumentNullException(nameof(stand));
            var liste = new List<Rangabweichung>();
            if (stand.Ebene(zone) == null) return liste;
            IReadOnlyList<Zuordnungszeile> zeilen = Zuordnungen(stand, zone);
            for (int i = 0; i < zeilen.Count; i++)
                for (int j = i + 1; j < zeilen.Count; j++)
                {
                    Zuordnungszeile a = zeilen[i], b = zeilen[j];
                    Konditionierungsgroesse? oben = null, unten = null;
                    foreach (Konditionierungsgroesse g in Konditionierungsgroessen.Alle)
                    {
                        if (!a.Raenge.TryGetValue(g, out int ra) || !b.Raenge.TryGetValue(g, out int rb)) continue;
                        if (ra > rb) oben ??= g;
                        else unten ??= g;
                    }
                    if (oben.HasValue && unten.HasValue)
                        liste.Add(new Rangabweichung(a.Schluessel, b.Schluessel, oben.Value, unten.Value));
                }
            return liste;
        }
    }
}
