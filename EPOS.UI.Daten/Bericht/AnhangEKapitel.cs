using System;
using System.Collections.Generic;
using System.Globalization;
using R = WindowsFormsApplication1.MyResource.Resource;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// BV-E2 (Konzept Berichtsvorlagen 9.5, 11 Nr. 3) — <b>welches Kapitel des Word-Berichts je Punkt
    /// der Anhang-E-Checkliste den Nachweis trägt</b>: die Zuordnung Punktnummer → Bausteinschlüssel
    /// (<c>BerichtsKonfiguration.B_*</c>), aus der die Hülle mit den Kapitelstellen der gewählten Vorlage
    /// die Stelle je Punkt bildet („„5 Wirtschaftliche Bewertung“", „„Anhang“ nicht im Bericht").
    ///
    /// <para><b>Woher die Zuordnung kommt.</b> Aus der Spalte „Stelle im Bericht" der Checkliste
    /// (<c>WIRT_AE_*_STELLE</c>): Deckblatt, Projektbeschreibung und Komponenten, Ergebnisse und
    /// Variantenvergleich, das Kapitel „Wirtschaftlichkeit" mit Parameterzeile, Mehrjahresübersicht,
    /// Kennzahlen, Sensitivität, Szenarien und Vorschlag, dazu der Anhang. Eine Wache hält, dass jeder
    /// Punkt der Checkliste eine Zeile hat.</para>
    /// </summary>
    internal static class AnhangEKapitel
    {
        /// <summary>Je Punktnummer (<c>ChecklistenPunkt.Nummer</c>) die Bausteine, deren Kapitel den Nachweis tragen.</summary>
        internal static readonly IReadOnlyDictionary<string, string[]> Zuordnung =
            new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["0.1"] = new[] { BerichtsKonfiguration.B_DECKBLATT },
                ["0.2"] = new[] { BerichtsKonfiguration.B_PROJEKT, BerichtsKonfiguration.B_KOMPONENTEN },
                ["1"] = new[] { BerichtsKonfiguration.B_WIRTSCHAFT },
                ["2a"] = new[] { BerichtsKonfiguration.B_ERGEBNISSE, BerichtsKonfiguration.B_VERGLEICH },
                ["2b"] = new[] { BerichtsKonfiguration.B_WIRTSCHAFT },
                ["3a"] = new[] { BerichtsKonfiguration.B_WIRTSCHAFT },
                ["3b"] = new[] { BerichtsKonfiguration.B_WIRTSCHAFT },
                ["4"] = new[] { BerichtsKonfiguration.B_WIRTSCHAFT },
                ["5"] = new[] { BerichtsKonfiguration.B_WIRTSCHAFT },
                ["6"] = new[] { BerichtsKonfiguration.B_WIRTSCHAFT },
                ["7"] = new[] { BerichtsKonfiguration.B_WIRTSCHAFT },
                ["8"] = new[] { BerichtsKonfiguration.B_WIRTSCHAFT },
                ["9"] = new[] { BerichtsKonfiguration.B_WIRTSCHAFT },
                ["10"] = new[] { BerichtsKonfiguration.B_WIRTSCHAFT },
                ["11"] = new[] { BerichtsKonfiguration.B_WIRTSCHAFT, BerichtsKonfiguration.B_ANHANG },
            };

        /// <summary>
        /// Je Punkt die Stelle in der Vorlage aus ihren Kapitelstellen (Bausteinschlüssel → Überschrift,
        /// <c>null</c> = nicht im Bericht). <paramref name="titel"/> reicht ein Prüfstand herein; <c>null</c>
        /// = die Titel der Berichtsseite (<see cref="BerichtSeiteGaben.Bausteintitel(string, Func{string, string})"/>).
        /// </summary>
        internal static IReadOnlyDictionary<string, string> Stellen(IReadOnlyDictionary<string, string> kapitel,
                                                                    Func<string, string> titel = null)
        {
            var stellen = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (KeyValuePair<string, string[]> z in Zuordnung) stellen[z.Key] = Stelle(z.Value, kapitel, titel);
            return stellen;
        }

        /// <summary>
        /// Die Stelle eines Punktes: je Baustein die Überschrift seines Kapitels in Anführungszeichen — ein
        /// Kapitel ohne eigene Überschrift mit dem Titel des Bausteins —, ein fehlendes Kapitel als „„…“
        /// nicht im Bericht"; führt die Vorlage keines der Kapitel, allein „nicht im Bericht".
        /// </summary>
        internal static string Stelle(IReadOnlyList<string> bausteine, IReadOnlyDictionary<string, string> kapitel,
                                      Func<string, string> titel = null)
        {
            var teile = new List<string>();
            int imBericht = 0;
            foreach (string baustein in bausteine ?? Array.Empty<string>())
            {
                string ueberschrift = null;
                bool da = kapitel != null && kapitel.TryGetValue(baustein, out ueberschrift) && ueberschrift != null;
                if (da)
                {
                    imBericht++;
                    string name = string.IsNullOrWhiteSpace(ueberschrift) ? Titel(baustein, titel) : ueberschrift.Trim();
                    teile.Add(Format(R.WIRT_AE_STELLE_KAPITEL, name));
                }
                else
                {
                    teile.Add(Format(R.WIRT_AE_STELLE_FEHLT, Titel(baustein, titel)));
                }
            }
            return imBericht == 0 ? R.WIRT_AE_NICHT_IM_BERICHT : string.Join(", ", teile);
        }

        private static string Titel(string baustein, Func<string, string> titel)
        {
            if (titel == null) return BerichtSeiteGaben.Bausteintitel(baustein);
            try { return titel(baustein) ?? baustein ?? ""; }
            catch (Exception) { return baustein ?? ""; }
        }

        private static string Format(string muster, string wert)
        {
            try { return string.Format(CultureInfo.CurrentCulture, muster ?? "", wert); }
            catch (FormatException) { return muster ?? ""; }
        }
    }
}
