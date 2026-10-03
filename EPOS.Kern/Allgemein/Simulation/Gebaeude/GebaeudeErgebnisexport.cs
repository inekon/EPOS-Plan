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
                                    IReadOnlyList<KeyValuePair<string, double>> skalare,
                                    IReadOnlyList<KeyValuePair<string, string>> texte = null)
        {
            Index = index;
            Modell = modell;
            Reihen = reihen;
            Skalare = skalare;
            Texte = texte ?? Array.Empty<KeyValuePair<string, string>>();
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

        /// <summary>
        /// <b>Die Textskalare der Aufheizoptimierung</b> (Entwurf KP3, Festlegung 28, Muster E32) in fester
        /// Reihenfolge: Schlüssel (mit Präfix <c>Geb[i].</c>) → Text — Zustand, Bemessung und Quelle des Gebäudes,
        /// danach Zustand und Quelle je Zone. Der Export schreibt sie gleich hinter <c>Geb[i].Modell</c>. Leer bei
        /// ausgeschalteter Aufheizoptimierung — dann entsteht kein Schlüssel.
        /// </summary>
        public IReadOnlyList<KeyValuePair<string, string>> Texte { get; }
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
    /// <para><b>Aufheizoptimierung, Nachtauskühlung, Sommerlüftung nur, wenn sie wirken</b> (Entwurf KP3,
    /// Festlegungen 27, 28; Muster E32): <c>Geb[n].Aufheizzustand</c> (Text, mit Bemessung und Quelle) und die
    /// Zahlen der Ergebniszeile (<c>Geb[n].AufheizzeitMaxH</c> … <c>Geb[n].HeizleistungMaxStundenH</c>, die
    /// Feldnamen von <see cref="ErgebnisGebaeudeModel"/>) nur bei Zustand ≠ NULL — dieselben Werte und
    /// NULL-Regeln wie die Ergebniszeile (<see cref="GebaeudeKennzahlen.Aufheizwerte"/>): Eine Zahl, die dort
    /// NULL ist, hat hier keinen Schlüssel (so auch P_auf = +∞ der Testnaht). <c>Geb[n].Nachtauskuehlstunden</c>
    /// nur mit Nachtauskühlung, <c>Geb[n].Sommerlueftungsstunden</c> nur mit Sommerlüftung — je Zone ebenso.
    /// Die Sollwertreihe <c>heizsollwert_&lt;n&gt;.csv</c> in °C (NaN = „aus", Muster <c>vorlauf_&lt;n&gt;.csv</c>)
    /// nur mit Heizkalender oder eingeschalteter Aufheizoptimierung. Keines der Referenzprojekte erfüllt eine
    /// dieser Bedingungen; ihre Ordner bleiben byte-gleich.</para>
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

            // Stufe KP3 (Festlegung 28): die Sollwertreihe nur mit Heizkalender oder eingeschalteter
            // Aufheizoptimierung - NaN heisst "aus" wie im Vorlauf des Heizkreises.
            if (e.Heizsollwert != null && (e.HeizkalenderWirksam || e.Aufheizung != null))
                reihen.Add(new KeyValuePair<string, double[]>("heizsollwert_" + n + ".csv", e.Heizsollwert));

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
            // Stufe KP3 (Festlegung 28): Lüftungsstunden und Aufheizwerte nur, wenn sie wirken (E32).
            var texte = new List<KeyValuePair<string, string>>();
            Wirkend(p, e, GebaeudeKennzahlen.Aufheizwerte(e.Aufheizung, zone: false), skalare, texte, gebaeude: true);
            Messung(p, e.Innenumkehr, skalare);

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
                    Wirkend(q, r, GebaeudeKennzahlen.Aufheizwerte(r.Aufheizung, zone: true), skalare, texte, gebaeude: false);
                    Messung(q, r.Innenumkehr, skalare);
                }
            return new GebaeudeExportsatz(e.Index, e.Modell ?? "", reihen, skalare, texte);
        }

        /// <summary>
        /// Die Schlüssel, die nur bei Wirkung entstehen (Festlegung 28): Nachtauskühl- und Sommerlüftungsstunden,
        /// dann die Zahlen der Aufheizwerte in der Reihenfolge der Ergebniszeile; Zustand, Bemessung und Quelle als
        /// Text. <paramref name="a"/> <c>null</c> = Schalter aus: kein Aufheizschlüssel. Am Gebäude dazu (E59,
        /// Festlegung 39) <c>Geb[n].Aufheizart</c> als Text neben der Bemessung und <c>Geb[n].Aufheizzeit_Manuell</c>
        /// nur, wenn das Gebäude mit manueller Aufheizzeit gerechnet hat.
        /// </summary>
        private static void Wirkend(string praefix, GebaeudeModellErgebnis e, Aufheizkennzahlen a,
                                    List<KeyValuePair<string, double>> skalare, List<KeyValuePair<string, string>> texte,
                                    bool gebaeude)
        {
            if (e.StundenMitNachtauskuehlung is int nacht) skalare.Add(Paar(praefix + "Nachtauskuehlstunden", nacht));
            if (GebaeudeKennzahlen.Sommerlueftungsstunden(e) is int sommer) skalare.Add(Paar(praefix + "Sommerlueftungsstunden", sommer));
            if (a == null) return;
            texte.Add(new KeyValuePair<string, string>(praefix + "Aufheizzustand", a.AufheizZustand));
            if (a.AufheizBemessung != null)
                texte.Add(new KeyValuePair<string, string>(praefix + "Aufheizbemessung", a.AufheizBemessung));
            if (gebaeude && a.AufheizArt != null)
                texte.Add(new KeyValuePair<string, string>(praefix + "Aufheizart", a.AufheizArt));
            if (a.AufheizLeistungsquelle != null)
                texte.Add(new KeyValuePair<string, string>(praefix + "Aufheizleistungsquelle", a.AufheizLeistungsquelle));
            foreach (KeyValuePair<string, double> z in a.Zahlen())
                skalare.Add(Paar(praefix + z.Key, z.Value));
            if (gebaeude && a.AufheizzeitManuellH is int manuell)
                skalare.Add(Paar(praefix + "Aufheizzeit_Manuell", manuell));
        }

        /// <summary>
        /// Die Messung der inneren Lastumkehr (Rechenweg RP2a) — nur mit eingeschalteter Messung
        /// (<see cref="Zonenmodell2K.SCHALTER_INNENUMKEHR"/>); ohne sie entsteht kein Schlüssel.
        /// </summary>
        private static void Messung(string praefix, Innenumkehrmessung m, List<KeyValuePair<string, double>> skalare)
        {
            if (m == null) return;
            skalare.Add(Paar(praefix + "Innenumkehr_Stunden", m.StundenUmkehr));
            skalare.Add(Paar(praefix + "Innenumkehr_Abschnitte", m.AbschnitteUmkehr));
            skalare.Add(Paar(praefix + "Innenumkehr_Kwh", m.UmkehrKwh));
            skalare.Add(Paar(praefix + "Bandverletzung_Stunden", m.StundenBand));
            skalare.Add(Paar(praefix + "Bandverletzung_Abschnitte", m.AbschnitteBand));
            skalare.Add(Paar(praefix + "Bandverletzung_Kh", m.BandKh));
        }

        private static KeyValuePair<string, double> Paar(string k, double v) => new KeyValuePair<string, double>(k, v);
    }
}
