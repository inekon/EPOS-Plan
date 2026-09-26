using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Gebäude auf dem VDI-Weg, so wie der Ergebnisexport es schreibt (Stufe G2;
    /// Umsetzungskonzept 1.8): drei Reihen und die acht Kennzahlen samt Kennung und Rechenweg —
    /// ohne wirksame Kühlung zwei Reihen und sechs Kennzahlen (Entscheid E32).
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
        /// Die Reihen je Stunde in fester Reihenfolge: Dateiname → Werte. Temperaturen in °C, der
        /// Kühlbedarf in kWh (Einheitenregel 1 des Kerns) — ihn nur bei wirksamer Kühlung (E32).
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
    /// <para><b>Die Kühlreihe nur bei wirksamer Kühlung (Entscheid E32).</b> Ein Gebäude ohne
    /// wirksame Kühlung läuft frei und hat keine Kühlreihe; es schreibt weder
    /// <c>kuehlbedarf_&lt;n&gt;.csv</c> noch <c>Geb[n].KuehlenergieMwh</c> und
    /// <c>Geb[n].StundenMitKuehlbedarf</c> — nicht mit Nullen gefüllt (Befund W 3).</para>
    ///
    /// <para><b>Die drei Reihen des Heizkreises nur mit wirksamer Kopplung</b> (Anlagenkopplung AK1,
    /// Konzept 8.3): <c>vorlauf_&lt;n&gt;.csv</c> und <c>ruecklauf_&lt;n&gt;.csv</c> in °C — NaN in
    /// den Stunden ohne Heizbetrieb —, <c>uebergabe_&lt;n&gt;.csv</c> mit dem Anteil der Stunde, in
    /// dem die Übergabe die Grenze war (0 … 1). Ein ungekoppeltes Gebäude schreibt keine davon: Eine
    /// Datei, die nur im neuen Lauf liegt, ist im Vergleich FAIL, und dagegen gibt es keinen
    /// Schalter. Neue Skalare kommen nicht dazu — die Kennzahlen des Heizkreises stehen in
    /// <c>Tab_ErgebnisGebaeude</c> (Schritt 128).</para>
    ///
    /// <para><b>Die Zonen nur ab zwei Zonen</b> (Stufe G6b): je Zone die Kennzahlen als
    /// <c>Geb[n].Zone[k].*</c> — <c>k</c> ist der Platz der Zone in der Rechenreihenfolge (ab 0) —,
    /// keine Reihen je Zone: Die Gebäudereihen sind ihre Summe bzw. ihr Flächenmittel (Festlegung 10),
    /// und ein Gebäude mit höchstens einer Zone schreibt keinen Zonenschlüssel. Die Energie nur für
    /// eine beheizte Zone, die Kühlenergie nur bei wirksamer Kühlung, Δϑ_max nur mit Nachbarzone —
    /// wie in <c>Tab_ErgebnisZone</c> nie mit Nullen gefüllt.</para>
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
            };
            if (e.KuehlbedarfKwh != null)
                reihen.Add(new KeyValuePair<string, double[]>("kuehlbedarf_" + n + ".csv", e.KuehlbedarfKwh));

            // Anlagenkopplung AK1 (8.3): die drei Reihen nur mit wirksamer Kopplung.
            if (e.Heizkreis != null)
            {
                reihen.Add(new KeyValuePair<string, double[]>("vorlauf_" + n + ".csv", e.Heizkreis.VorlaufC));
                reihen.Add(new KeyValuePair<string, double[]>("ruecklauf_" + n + ".csv", e.Heizkreis.RuecklaufC));
                reihen.Add(new KeyValuePair<string, double[]>("uebergabe_" + n + ".csv", e.Heizkreis.UebergabeBegrenztAnteil));
            }

            // Kälteseite (E37, 8.3): die drei Reihen des Kältekreises nur mit wirksamer Kühlkopplung.
            if (e.Kuehlkreis != null)
            {
                reihen.Add(new KeyValuePair<string, double[]>("kuehlvorlauf_" + n + ".csv", e.Kuehlkreis.VorlaufC));
                reihen.Add(new KeyValuePair<string, double[]>("kuehlruecklauf_" + n + ".csv", e.Kuehlkreis.RuecklaufC));
                reihen.Add(new KeyValuePair<string, double[]>("kuehluebergabe_" + n + ".csv", e.Kuehlkreis.UebergabeBegrenztAnteil));
            }

            string p = "Geb[" + n + "].";
            var skalare = new List<KeyValuePair<string, double>>
            {
                Paar(p + "ID_Gebaeude", e.ID_Gebaeude),
                Paar(p + "JahresheizwaermeMwh", e.JahresheizwaermeMwh),
                Paar(p + "SpitzeKw", e.SpitzeKw),
                Paar(p + "SpitzeTagesmittelKw", e.SpitzeTagesmittelKw),
                Paar(p + "Spitze95Kw", e.Spitze95Kw),
            };
            if (e.KuehlenergieMwh is double kuehlMwh) skalare.Add(Paar(p + "KuehlenergieMwh", kuehlMwh));
            if (e.StundenMitKuehlbedarf is int kuehlStunden) skalare.Add(Paar(p + "StundenMitKuehlbedarf", kuehlStunden));
            skalare.Add(Paar(p + "MittlereRaumtemperaturHeizzeit", e.MittlereRaumtemperaturHeizzeit));
            skalare.Add(Paar(p + "Ueberhitzungsstunden", e.Ueberhitzungsstunden));

            // Stufe G6b (W5): je Zone die Kennzahlen, nur ab zwei Zonen.
            if (e.Zonen != null)
                for (int k = 0; k < e.Zonen.Count; k++)
                {
                    GebaeudeZonenergebnis z = e.Zonen[k];
                    GebaeudeModellErgebnis r = z.Ergebnis;
                    string q = p + "Zone[" + k.ToString(System.Globalization.CultureInfo.InvariantCulture) + "].";
                    skalare.Add(Paar(q + "ID_Zone", z.ZonenId));
                    skalare.Add(Paar(q + "IstBeheizt", z.IstBeheizt ? 1 : 0));
                    if (z.IstBeheizt)
                    {
                        skalare.Add(Paar(q + "JahresheizwaermeMwh", r.JahresheizwaermeMwh));
                        skalare.Add(Paar(q + "SpitzeKw", r.SpitzeKw));
                    }
                    if (r.KuehlenergieMwh is double zoneKuehlMwh) skalare.Add(Paar(q + "KuehlenergieMwh", zoneKuehlMwh));
                    skalare.Add(Paar(q + "MittlereRaumtemperaturHeizzeit", r.MittlereRaumtemperaturHeizzeit));
                    skalare.Add(Paar(q + "Ueberhitzungsstunden", r.Ueberhitzungsstunden));
                    if (!double.IsNaN(z.DeltaThetaMaxK)) skalare.Add(Paar(q + "DeltaThetaMaxK", z.DeltaThetaMaxK));
                }
            return new GebaeudeExportsatz(e.Index, e.Modell ?? "", reihen, skalare);
        }

        private static KeyValuePair<string, double> Paar(string k, double v) => new KeyValuePair<string, double>(k, v);
    }
}
