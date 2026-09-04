// Die Kulturgrenze der Werkzeugliste (iU9-W15b.7).
//
// Der Vorlaeufer hatte diese Rechnung als private Methode WerteSammeln im
// Formular (Form_KiChat.cs:1365-1393) - und mit ihr die eine Zeile, an der die
// Anzeigeschreibweise in die invariante uebergeht:
//
//     werte[p.Name] = text.Replace(",", ".");        (:1385)
//
// Sie ist die Kulturgrenze der Werkzeugliste (Fachkonzept 3.2). Geht sie
// verloren, schickt ein deutscher Arbeitsplatz "12,5" an eine Aktion, die
// invariant parst - und bekommt 125 oder eine Ablehnung. Deshalb steht sie hier
// im Kern und nicht in der Oberflaeche: Beide Huellen brauchen sie, und ohne
// Bildschirm laesst sie sich nachrechnen (Risiko R-W15b-6).

using System;
using System.Collections.Generic;
using KiKern;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Wandelt die Rohwerte der Werkzeugliste in die Uebergabewerte einer Aktion.
    /// </summary>
    public static class KiWerkzeugWerte
    {
        /// <summary>Trennzeichen einer Ganzzahlliste (Bestand <c>:1380-1381</c>).</summary>
        private static readonly char[] TRENNER = { ',', ';', ' ', '\t' };

        /// <summary>
        /// Sammelt die Werte je Parameter der Aktion.
        /// </summary>
        /// <param name="aktion">Die gewaehlte Aktion; ihre Parameter geben die Typen vor.</param>
        /// <param name="rohwerte">
        /// Was der Anwender eingetippt bzw. gewaehlt hat, je Parametername. Fehlende
        /// Eintraege gelten als „nicht angegeben".
        /// </param>
        /// <remarks>
        /// <para><b>Ein leeres Feld ist NICHT ANGEGEBEN</b> (<c>:1376</c>), nicht „leerer
        /// Text": Sonst waere jeder ausgelassene Wahlparameter eine leere Zeichenkette,
        /// und die Pflichtpruefung des Registers liefe ins Leere.</para>
        /// <para><b>Zahlen werden hier - und nur hier - in die invariante Schreibweise
        /// gebracht.</b> Das ist die Kulturgrenze (Fachkonzept 3.2). Betroffen sind
        /// <c>Zahl</c> und <c>Ganzzahl</c>; Text bleibt Text, und eine Ganzzahlliste
        /// wird an Komma, Strichpunkt, Leerzeichen und Tabulator zerlegt.</para>
        /// </remarks>
        public static IReadOnlyDictionary<string, object> Sammeln(
            KiAktion aktion, IReadOnlyDictionary<string, string> rohwerte)
        {
            var werte = new Dictionary<string, object>(StringComparer.Ordinal);
            if (aktion == null || rohwerte == null) return werte;

            foreach (KiParameter p in aktion.Parameter)
            {
                string roh;
                if (!rohwerte.TryGetValue(p.Name, out roh)) continue;

                string text = (roh ?? "").Trim();
                if (text.Length == 0) continue;          // Leeres Feld = nicht angegeben

                if (p.Typ == KiParameterTyp.GanzzahlListe)
                {
                    werte[p.Name] = text.Split(TRENNER, StringSplitOptions.RemoveEmptyEntries);
                }
                else if (p.Typ == KiParameterTyp.Zahl || p.Typ == KiParameterTyp.Ganzzahl)
                {
                    werte[p.Name] = text.Replace(",", ".");
                }
                else
                {
                    werte[p.Name] = text;
                }
            }

            return werte;
        }
    }
}
