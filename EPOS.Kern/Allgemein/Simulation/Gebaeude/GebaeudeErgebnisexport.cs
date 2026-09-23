using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Gebäude auf dem VDI-Weg, so wie der Ergebnisexport es schreibt (Stufe G2;
    /// Umsetzungskonzept 1.8): drei Reihen und die acht Kennzahlen samt Kennung und Rechenweg.
    /// </summary>
    public sealed class GebaeudeExportsatz
    {
        internal GebaeudeExportsatz(int index, string modell, IReadOnlyList<KeyValuePair<string, double[]>> reihen,
                                    IReadOnlyList<KeyValuePair<string, double>> skalare)
        {
            Index = index;
            Modell = modell;
            Reihen = reihen;
            Skalare = skalare;
        }

        /// <summary>Der Rechenweg (<c>DbWerte.GEBAEUDE_MODELL_*</c>) — der Skalar <c>Geb[i].Modell</c> als Text.</summary>
        public string Modell { get; }

        /// <summary>Der Merkplatz des Gebäudes im Lauf (ab 0) — der Index <c>n</c> der Dateinamen.</summary>
        public int Index { get; }

        /// <summary>
        /// Die drei Reihen je Stunde in fester Reihenfolge: Dateiname → Werte. Temperaturen in
        /// °C, der Kühlbedarf in kWh (Einheitenregel 1 des Kerns).
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, double[]>> Reihen { get; }

        /// <summary>
        /// Die Zahlenskalare in fester Reihenfolge: Schlüssel (mit Präfix <c>Geb[i].</c>) → Wert;
        /// die Einheit steht im Namen, Jahressummen in MWh. Die Schreibweise wählt der Export,
        /// damit sie der übrigen <c>aggregate.csv</c> gleicht.
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, double>> Skalare { get; }
    }

    /// <summary>
    /// <b>Die Kernseite des Ergebnisexports der Gebäudesimulation</b> (Stufe G2;
    /// Umsetzungskonzept 1.8). Sie legt fest, <b>was</b> je VDI-Gebäude exportiert wird —
    /// Dateinamen, Schlüssel, Reihenfolge und Schreibweise —, und liefert es aus dem
    /// Ergebnisträger eines Laufs.
    ///
    /// <para><b>Nur für Gebäude des VDI-Wegs.</b> Ein Gebäude auf dem Tagesbilanz-Weg hat
    /// keinen Eintrag im Ergebnisträger und erzeugt deshalb keinen einzigen Satz — so bleibt
    /// ein Bestandsordner byte-gleich (Muster Erdreichblock, Umsetzungskonzept 1.8).</para>
    ///
    /// <para><b>Wer schreibt.</b> Die CSV-Dateien und die Skalare in <c>aggregate.csv</c>
    /// schreibt <c>Referenzlauf/Ergebnisexport.cs</c> (beide Referenzlauf-Werkzeuge und die
    /// iOS-Prüfung teilen die Datei).</para>
    ///
    /// <para>Öffentlich, weil der Referenzlauf den Kern ohne <c>InternalsVisibleTo</c>
    /// liest; ohne Datenbank, ohne Zustand.</para>
    /// </summary>
    public static class GebaeudeErgebnisexport
    {
        /// <summary>Die Sätze aller VDI-Gebäude des Laufs, nach Merkplatz geordnet; leer ohne VDI-Gebäude.</summary>
        public static IReadOnlyList<GebaeudeExportsatz> Saetze(SimulationWaermebedarf wb)
        {
            if (wb == null) throw new ArgumentNullException(nameof(wb));
            var saetze = new List<GebaeudeExportsatz>();
            foreach (GebaeudeModellErgebnis e in wb.GebaeudeErgebnisse.Alle)
                saetze.Add(Satz(e));
            return saetze;
        }

        /// <summary>Der Satz eines Ergebnisses — Dateinamen und Schlüssel nach Umsetzungskonzept 1.8.</summary>
        internal static GebaeudeExportsatz Satz(GebaeudeModellErgebnis e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            string n = e.Index.ToString(System.Globalization.CultureInfo.InvariantCulture);
            var reihen = new List<KeyValuePair<string, double[]>>
            {
                new KeyValuePair<string, double[]>("raumtemperatur_" + n + ".csv", e.Raumtemperatur),
                new KeyValuePair<string, double[]>("operative_temperatur_" + n + ".csv", e.OperativeTemperatur),
                new KeyValuePair<string, double[]>("kuehlbedarf_" + n + ".csv", e.KuehlbedarfKwh),
            };

            string p = "Geb[" + n + "].";
            var skalare = new List<KeyValuePair<string, double>>
            {
                Paar(p + "ID_Gebaeude", e.ID_Gebaeude),
                Paar(p + "JahresheizwaermeMwh", e.JahresheizwaermeMwh),
                Paar(p + "SpitzeKw", e.SpitzeKw),
                Paar(p + "SpitzeTagesmittelKw", e.SpitzeTagesmittelKw),
                Paar(p + "Spitze95Kw", e.Spitze95Kw),
                Paar(p + "KuehlenergieMwh", e.KuehlenergieMwh),
                Paar(p + "StundenMitKuehlbedarf", e.StundenMitKuehlbedarf),
                Paar(p + "MittlereRaumtemperaturHeizzeit", e.MittlereRaumtemperaturHeizzeit),
                Paar(p + "Ueberhitzungsstunden", e.Ueberhitzungsstunden),
            };
            return new GebaeudeExportsatz(e.Index, e.Modell ?? "", reihen, skalare);
        }

        private static KeyValuePair<string, double> Paar(string k, double v) => new KeyValuePair<string, double>(k, v);
    }
}
