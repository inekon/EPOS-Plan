using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Zentrale Kennzahl-Definitionen für den Variantenvergleich
    /// (Konzept Kap. 5; vier Gruppen). Word- und Excel-Ausgabe führen dieselben
    /// Zeilen — genau eine Quelle für Schlüssel, Beschriftung, Einheit und Format.
    /// </summary>
    public class Kennzahl
    {
        public string Schluessel;      // stabil, z. B. "energie.waermebedarf"
        public string LabelDe;
        public string LabelEn;
        public string Einheit;         // sprachneutral
        public string Gruppe;          // KennzahlenKatalog.GR_*
        public string Format;          // .NET-Zahlenformat, z. B. "N0", "N1"
        public bool DeltaAnzeigen;     // Abweichung zu Stamm ausweisen?

        /// <summary>Wertzugriff; null = für dieses Projekt nicht verfügbar (Anzeige „—").</summary>
        public Func<VariantenDaten, double?> Wert;

        public Kennzahl(string schluessel, string de, string en, string einheit,
                        string gruppe, string format, bool delta, Func<VariantenDaten, double?> wert)
        {
            Schluessel = schluessel; LabelDe = de; LabelEn = en; Einheit = einheit;
            Gruppe = gruppe; Format = format; DeltaAnzeigen = delta; Wert = wert;
        }

        public string Label(bool englisch) { return englisch ? LabelEn : LabelDe; }
    }

    public static class KennzahlenKatalog
    {
        public const string GR_ENERGIE = "Energiebilanz";
        public const string GR_EFFIZIENZ = "Effizienz";
        public const string GR_EMISSION = "Emissionen";
        public const string GR_KOSTEN = "Kosten";

        /// <summary>
        /// Die Gruppe der ERZEUGERkennzahlen der Kälte (Kühlkonzept 6.4, K15): Kälteerzeugung,
        /// ungedeckte Kälte, Jahresarbeitszahl Kälte, Kältestrom samt Netzbezug, Kosten und
        /// Emissionen. Die Kanalkennzahlen der Kälte (Jahreskälte, Spitze, Stunden, Deckungsgrad)
        /// stehen dagegen bei den Wärmekanälen in <see cref="GR_ENERGIE"/>. Ein Projekt ohne
        /// Kälteerzeuger führt in dieser Gruppe keinen Wert — die Gruppe entfällt.
        /// </summary>
        public const string GR_KAELTE = "Kälte";

        /// <summary>Die Gruppen in der Reihenfolge der Kennzahlentabellen (Word und Excel).</summary>
        public static readonly string[] GRUPPEN = { GR_ENERGIE, GR_EFFIZIENZ, GR_KAELTE, GR_EMISSION, GR_KOSTEN };

        // Kurzzugriffe (null-tolerant) --------------------------------------

        private static ErgebnisEnergiebedarfModel E(VariantenDaten v) { return v?.Ergebnis?.Energiebedarf; }
        private static ErgebnisWaermepumpeModel WP(VariantenDaten v) { return v?.Ergebnis?.Waermepumpe; }
        private static ErgebnisBHKWModel BH(VariantenDaten v) { return v?.Ergebnis?.BHKW; }
        private static ErgebnisHeizkesselModel HK(VariantenDaten v) { return v?.Ergebnis?.Heizkessel; }
        private static ErgebnisSolarthermieModel SO(VariantenDaten v) { return v?.Ergebnis?.Solarthermie; }
        private static ErgebnisPhotovoltaikModel PV(VariantenDaten v) { return v?.Ergebnis?.Photovoltaik; }

        // ------------------------------------------------------------------
        // PAKET E1 (Konzept 4.4) — die drei Bedarfskanäle im Bericht
        // ------------------------------------------------------------------

        /// <summary>
        /// Jahres-Wärmebedarf eines Kanals [MWh/a]; null, wenn kein Ergebnis vorliegt.
        ///
        /// <para>0 wird ausdrücklich ALS 0 gemeldet, nicht als „—": Ein Projekt ohne
        /// Prozesswärme hat einen Prozessbedarf von null, und das ist eine Aussage.
        /// Zeilen aus Läufen VOR Paket E1 tragen die Spalten nicht — dort steht in allen
        /// drei Kanälen 0, was an der Gesamtsumme unmittelbar erkennbar ist.</para>
        /// </summary>
        private static double? BedarfKanal(VariantenDaten v, int kanal)
        {
            var e = E(v);
            if (e == null || e.Waermebedarf_Kanal == null || kanal >= e.Waermebedarf_Kanal.Length)
                return null;
            return e.Waermebedarf_Kanal[kanal];
        }

        /// <summary>
        /// DECKUNGSGRAD eines Kanals über ALLE Erzeuger [%] — „wie viel des
        /// Brauchwasserbedarfs wurde gedeckt?".
        ///
        /// <para>Die gespeicherten Spalten <c>Deckung_&lt;Kanal&gt;</c> sind die
        /// Aufschlüsselung der Erzeuger-Deckung und beziehen sich auf den
        /// PROJEKT-Gesamtbedarf (siehe <see cref="ErgebnisWaermepumpeModel.Deckung_Kanal"/>).
        /// Der Deckungsgrad DIESES Kanals ist daraus die Umrechnung auf den Kanalbedarf:
        /// Σ Erzeugeranteile · Gesamtbedarf / Kanalbedarf. Es ist die einzige Stelle, an
        /// der diese Umrechnung steht — als eigene Ergebnisspalte wäre sie eine zweite
        /// Wahrheit.</para>
        ///
        /// <para>null (Anzeige „—") ohne Ergebnis oder ohne Bedarf in diesem Kanal: Ein
        /// Deckungsgrad ohne Bedarf ist keine 0, sondern undefiniert.</para>
        /// </summary>
        private static double? DeckungKanal(VariantenDaten v, int kanal)
        {
            var e = E(v);
            if (e == null || e.Waermebedarf_Kanal == null || kanal >= e.Waermebedarf_Kanal.Length)
                return null;

            double kanalbedarf = e.Waermebedarf_Kanal[kanal];
            if (kanalbedarf <= 0 || e.Waermebedarf_Gesamt <= 0) return null;

            double anteil = 0;
            if (WP(v) != null) anteil += Kanalwert(WP(v).Deckung_Kanal, kanal);
            if (BH(v) != null) anteil += Kanalwert(BH(v).Deckung_Kanal, kanal);
            if (HK(v) != null) anteil += Kanalwert(HK(v).Deckung_Kanal, kanal);
            if (SO(v) != null) anteil += Kanalwert(SO(v).Deckung_Kanal, kanal);

            return anteil * e.Waermebedarf_Gesamt / kanalbedarf;
        }

        private static double Kanalwert(double[] zeile, int kanal)
        {
            return (zeile != null && kanal < zeile.Length) ? zeile[kanal] : 0.0;
        }

        /// <summary>
        /// <b>DECKUNGSGRAD DES KÜHLKANALS</b> [%] — der EIGENE Zweig der Kälteseite (Stufe KU2,
        /// Kühlkonzept 6.4, K16; <c>kaelte.deckungsgrad</c>), kein vierter Fall von
        /// <see cref="DeckungKanal"/>: Dessen Erzeugerliste ist eine WÄRMEliste und sein Nenner
        /// <c>Waermebedarf_Gesamt</c> enthält den Kältebedarf ausdrücklich nicht — ein Aufruf mit
        /// <see cref="Kanal.KUEHLUNG"/> lieferte eine Zahl mit dem falschen Nenner und ohne
        /// Fehlermeldung.
        ///
        /// <para>Summiert wird über die KÄLTEerzeuger — in KU2 die reversible Wärmepumpe, deren
        /// Spalte <c>Deckung_Kuehlung</c> ihren Anteil am Kältebedarf trägt —, umgerechnet mit
        /// <c>Kaeltebedarf_Gesamt</c> auf den Kanalbedarf. Mit dem einen Kältekanal ist der Faktor
        /// 1; er trennt sich, sobald ein zweiter Kältekanal hinzukommt.</para>
        ///
        /// <para>null (Anzeige „—") ohne Ergebnis, ohne erhobene Kälte oder ohne Kältebedarf: Ein
        /// Deckungsgrad ohne Bedarf ist keine 0, sondern undefiniert.</para>
        /// </summary>
        public static double? DeckungKanalKaelte(VariantenDaten v)
        {
            var e = E(v);
            if (e == null || e.Waermebedarf_Kanal == null || Kanal.KUEHLUNG >= e.Waermebedarf_Kanal.Length)
                return null;
            if (!e.Kaeltebedarf_Gesamt.HasValue) return null;

            double kanalbedarf = e.Waermebedarf_Kanal[Kanal.KUEHLUNG];
            double gesamt = e.Kaeltebedarf_Gesamt.Value;
            if (kanalbedarf <= 0 || gesamt <= 0) return null;

            double anteil = 0;
            if (WP(v) != null) anteil += Kanalwert(WP(v).Deckung_Kanal, Kanal.KUEHLUNG);

            return anteil * gesamt / kanalbedarf;
        }

        // ------------------------------------------------------------------
        // STUFE KU1 (Kühlkonzept 6.4) — die Kanalkennzahlen der Kälte
        // ------------------------------------------------------------------

        /// <summary>Jahreskälte = Summe des Kühlkanals [MWh/a] — Katalogschlüssel der Softwarearchitektur 4.3.</summary>
        public const string SCHLUESSEL_KAELTE_JAHRESBEDARF = "kaelte.jahresbedarf";

        /// <summary>Kältespitze [kW] (<c>Kaeltelast_Max</c>).</summary>
        public const string SCHLUESSEL_KAELTE_SPITZE = "kaelte.spitze";

        /// <summary>Stunden mit Kühlbedarf [h/a], gezählt am Kanalvektor.</summary>
        public const string SCHLUESSEL_KAELTE_STUNDEN = "kaelte.stunden";

        // ------------------------------------------------------------------
        // STUFE KU2 WELLE 3 (Kühlkonzept 6.2–6.4; E21, E34, K5, K15) — Deckung und Erzeuger
        // ------------------------------------------------------------------

        /// <summary>Deckungsgrad des Kühlkanals [%] (<see cref="DeckungKanalKaelte"/>) — Katalogschlüssel der Softwarearchitektur 4.3.</summary>
        public const string SCHLUESSEL_KAELTE_DECKUNGSGRAD = "kaelte.deckungsgrad";

        /// <summary>Kälteerzeugung der Wärmepumpen [MWh/a] — Gegenstück zu <c>energie.wp_waerme</c>.</summary>
        public const string SCHLUESSEL_KAELTE_ERZEUGUNG = "kaelte.erzeugung";

        /// <summary>Ungedeckte Kälte [MWh/a] (<c>Kaelterestbedarf</c>) — Gegenstück zu <c>energie.waermerest</c>.</summary>
        public const string SCHLUESSEL_KAELTE_REST = "kaelte.rest";

        /// <summary>Jahresarbeitszahl Kälte (EER-Jahreswert) = Kälteerzeugung / Kältestrom — Gegenstück zu <c>eff.jaz</c>.</summary>
        public const string SCHLUESSEL_KAELTE_JAZ = "kaelte.jaz";

        /// <summary>Kältestrom samt Hilfsstrom [MWh/a] (Kühlkonzept 6.1).</summary>
        public const string SCHLUESSEL_KAELTE_STROM = "kaelte.strom";

        /// <summary>Netzbezug des Kältestroms [MWh/a] — sein Anteil am Netzbezug bzw. der eigene Zähler (E34).</summary>
        public const string SCHLUESSEL_KAELTE_NETZBEZUG = "kaelte.netzbezug";

        /// <summary>Arbeitskosten des Kältestroms [€/a] (E34; ein Ausweis, in den Energiekosten enthalten).</summary>
        public const string SCHLUESSEL_KAELTE_KOSTEN = "kaelte.kosten";

        /// <summary>Emissionen des Kältestroms [t/a] (E34; ein Ausweis, in den Emissionen enthalten).</summary>
        public const string SCHLUESSEL_KAELTE_CO2 = "kaelte.co2";

        /// <summary>
        /// Jahresarbeitszahl Kälte aus dem gespeicherten Ergebnis: Kälteerzeugung / Kältestrom der
        /// Wärmepumpen; <c>null</c> ohne Kälteerzeugung oder ohne Kältestrom.
        /// </summary>
        public static double? JahresarbeitszahlKaelte(VariantenDaten v)
        {
            var w = WP(v);
            if (w == null || !w.Kaelteproduktion_WP.HasValue || !w.Stromverbrauch_Kuehlung.HasValue) return null;
            return w.Stromverbrauch_Kuehlung.Value > 0
                ? (double?)(w.Kaelteproduktion_WP.Value / w.Stromverbrauch_Kuehlung.Value) : null;
        }

        /// <summary>Nur mit gerechneter Kälteerzeugung (<c>Kaelteproduktion_WP</c> gesetzt) — sonst keine Kältezahl der Gruppe <see cref="GR_KAELTE"/>.</summary>
        private static bool MitKaelteerzeugung(VariantenDaten v)
        {
            return WP(v)?.Kaelteproduktion_WP != null;
        }

        /// <summary>
        /// Stunden mit Kühlbedarf [h/a] (<c>kaelte.stunden</c>) — gezählt am KANALVEKTOR, nicht als
        /// Summe der Gebäudewerte (Kühlkonzept 6.4). Die Ergebnistabellen führen die Zahl nicht;
        /// sie entsteht aus der Kanalreihe eines für den Bericht gerechneten Laufs
        /// (<see cref="ZeitreihenSatz"/>, nur Kanäle mit Bedarf tragen eine Reihe). <c>null</c> ohne
        /// erhobene Kälte oder ohne Reihe; 0, wenn die erhobene Jahreskälte 0 ist.
        /// </summary>
        private static double? KaelteStunden(VariantenDaten v)
        {
            double? jahr = E(v)?.Kaeltebedarf_Gesamt;
            if (!jahr.HasValue) return null;
            if (jahr.Value <= 0) return 0.0;

            double[] reihe = v.Zeitreihen?.Hole(ZeitreihenSatz.BedarfSchluessel(Kanal.KUEHLUNG));
            if (reihe == null) return null;

            int stunden = 0;
            for (int h = 0; h < reihe.Length; h++) if (reihe[h] > 0.0) stunden++;
            return stunden;
        }

        // ------------------------------------------------------------------
        // PAKET P2 (Konzept 7.4) — die Speichertemperaturen des Schichtmodells
        // ------------------------------------------------------------------

        /// <summary>
        /// Mittlere Temperatur der obersten Schicht über ALLE Speicher des Laufs [°C];
        /// null, wenn kein Speicher einen Wert trägt.
        ///
        /// <para><b>Ungewichtetes Mittel über die Speicher mit Wert</b> — bewusst die
        /// einfachste nachvollziehbare Zusammenfassung. Im Regelfall (ein Senkenspeicher)
        /// ist sie der Wert dieses Speichers; bei mehreren beantwortet die Kennzahl die
        /// Vergleichsfrage „liegt diese Variante insgesamt wärmer?". Die
        /// AUFSCHLÜSSELUNG je Speicher steht im Baustein Projektbeschreibung — eine
        /// Katalogzeile trägt genau EINEN Wert je Variante, mehr gäbe die
        /// Vergleichstabelle nicht her.</para>
        ///
        /// <para>Speicher ohne Wert werden übergangen, nicht als 0 gezählt: Quellspeicher
        /// tragen keine Schichttemperatur (Konzept 8.2), und Ergebniszeilen aus Läufen
        /// VOR Paket P1 haben die Spalte nie gefüllt. Eine 0 °C stünde in beiden Fällen
        /// als Messwert da, den es nicht gibt.</para>
        /// </summary>
        private static double? TObenMittel(VariantenDaten v)
        {
            var liste = v?.Ergebnis?.Pufferspeicher;
            if (liste == null) return null;

            double summe = 0;
            int n = 0;
            foreach (ErgebnisPufferspeicherModel p in liste)
                if (p != null && p.T_oben_Mittel.HasValue) { summe += p.T_oben_Mittel.Value; n++; }

            return n > 0 ? (double?)(summe / n) : null;
        }

        /// <summary>
        /// KLEINSTE Temperatur der obersten Schicht über alle Speicher [°C] — der
        /// ungünstigste Punkt des Jahres im ungünstigsten Speicher; null ohne Wert.
        ///
        /// <para>Anders als beim Mittel ist das Minimum über mehrere Speicher wieder ein
        /// Minimum und braucht keine Konvention.</para>
        /// </summary>
        private static double? TObenMin(VariantenDaten v)
        {
            var liste = v?.Ergebnis?.Pufferspeicher;
            if (liste == null) return null;

            double? kleinster = null;
            foreach (ErgebnisPufferspeicherModel p in liste)
                if (p != null && p.T_oben_Min.HasValue &&
                    (!kleinster.HasValue || p.T_oben_Min.Value < kleinster.Value))
                    kleinster = p.T_oben_Min.Value;

            return kleinster;
        }

        /// <summary>Summe der Brennstoffverbräuche eines Erzeugers (MWh/a).</summary>
        private static double Brennstoffsumme(ErgebnisBHKWModel b)
        {
            if (b == null) return 0;
            return b.Gasverbrauch + b.Oelverbrauch + b.Koks + b.Rapsoelverbrauch + b.Holzverbrauch
                 + b.Kohle + b.Sonstigverbrauch + b.Pellets + b.TierischeFette;
        }
        private static double Brennstoffsumme(ErgebnisHeizkesselModel h)
        {
            if (h == null) return 0;
            return h.Gasverbrauch + h.Oelverbrauch + h.Koks + h.Rapsoelverbrauch + h.Holzverbrauch
                 + h.Kohle + h.Sonstigverbrauch + h.Pellets + h.TierischeFette;
        }

        /// <summary>
        /// Der komplette Katalog in Anzeige-Reihenfolge, mit den CO₂-Zeilen im Modus
        /// <c>CO2</c> beschriftet. Für einen Ausweis nach dem tatsächlich gerechneten
        /// Modus (Etappe E5, Konzept F7) die Überladung
        /// <see cref="Alle(string)"/> verwenden.
        /// </summary>
        public static List<Kennzahl> Alle()
        {
            return Alle(DbWerte.EMISSION_MODUS_CO2);
        }

        /// <summary>
        /// Der komplette Katalog in Anzeige-Reihenfolge.
        /// Emissions- und Kostenkennzahlen liefern bis zur Umsetzung der Verrechnung
        /// (Emissionsfaktoren × Verbräuche bzw. Menge × Preis über carrier_id, Phase 5)
        /// bewusst null — sie erscheinen als „—", nie als 0 (Konzept Kap. 5).
        /// </summary>
        /// <param name="modus">Berechnungsmodus der CO₂-Kennzahlen, wie ihn der
        /// Rechenlauf an <see cref="VariantenDaten.EmissionsModus"/> vermerkt hat
        /// (bei einem Vergleich über <see cref="EmissionsAusweis.ModusAusVarianten"/>
        /// zu bilden). Er wirkt AUSSCHLIESSLICH auf die Beschriftung — welchen Wert
        /// <c>em.co2</c> trägt, hat der Rechenlauf längst entschieden.</param>
        public static List<Kennzahl> Alle(string modus)
        {
            var l = new List<Kennzahl>();

            // ---------------- Energiebilanz ----------------
            l.Add(new Kennzahl("energie.waermebedarf", "Wärmebedarf gesamt", "Total heat demand", "MWh/a", GR_ENERGIE, "N0", true,
                v => E(v) == null ? (double?)null : E(v).Waermebedarf_Gesamt));
            // PAKET E1 (Konzept 4.4): der Gesamtbedarf aufgeschlüsselt auf die drei
            // Kanäle — unmittelbar unter der Summe, damit die Zerlegung als solche
            // lesbar bleibt. Die drei Zeilen addieren sich zum Wärmebedarf gesamt.
            l.Add(new Kennzahl("energie.waermebedarf_heizung", "davon Heizung", "of which space heating",
                "MWh/a", GR_ENERGIE, "N0", true, v => BedarfKanal(v, Kanal.HEIZUNG)));
            l.Add(new Kennzahl("energie.waermebedarf_brauchwasser", "davon Brauchwasser", "of which domestic hot water",
                "MWh/a", GR_ENERGIE, "N0", true, v => BedarfKanal(v, Kanal.BRAUCHWASSER)));
            l.Add(new Kennzahl("energie.waermebedarf_prozess", "davon Prozesswärme", "of which process heat",
                "MWh/a", GR_ENERGIE, "N0", true, v => BedarfKanal(v, Kanal.PROZESS)));

            l.Add(new Kennzahl("energie.waermelast", "Wärmelast max.", "Peak heat load", "kW", GR_ENERGIE, "N0", true,
                v => E(v) == null ? (double?)null : E(v).Waermelast_Max));

            // STUFE KU1 (Kühlkonzept 6.4, K15; E21, K5, K18): die Kanalkennzahlen der Kälte, in
            // derselben Gruppe wie die drei Wärmekanäle, unmittelbar nach der Wärmelast. null —
            // also keine Zeile, kein „—" —, wenn der Lauf keine Kälte ERHOBEN hat: Ein Projekt
            // ohne Kühlung zeigt keine Kühlnullen. Die Beschriftung trägt die Grenze der Zahl
            // (sensible Kälte ohne Entfeuchtung) in jede Tabelle, in der sie erscheint.
            l.Add(new Kennzahl(SCHLUESSEL_KAELTE_JAHRESBEDARF, "Kältebedarf gesamt (sensibel)",
                "Total cooling demand (sensible)", "MWh/a", GR_ENERGIE, "N1", true,
                v => E(v)?.Kaeltebedarf_Gesamt));
            l.Add(new Kennzahl(SCHLUESSEL_KAELTE_SPITZE, "Kältelast max. (sensibel)",
                "Peak cooling load (sensible)", "kW", GR_ENERGIE, "N1", true,
                v => E(v)?.Kaeltelast_Max));
            l.Add(new Kennzahl(SCHLUESSEL_KAELTE_STUNDEN, "Stunden mit Kühlbedarf (sensibel)",
                "Hours with cooling demand (sensible)", "h/a", GR_ENERGIE, "N0", false,
                KaelteStunden));
            l.Add(new Kennzahl("energie.strombedarf", "Strombedarf gesamt", "Total electricity demand", "MWh/a", GR_ENERGIE, "N0", true,
                v => E(v) == null ? (double?)null : E(v).Strombedarf_Gesamt));
            l.Add(new Kennzahl("energie.strommax", "Strombedarf max.", "Peak electric load", "kW", GR_ENERGIE, "N0", true,
                v => E(v) == null ? (double?)null : E(v).Strombedarf_Max));

            l.Add(new Kennzahl("energie.wp_waerme", "Wärmeerzeugung Wärmepumpe", "Heat pump heat output", "MWh/a", GR_ENERGIE, "N0", true,
                v => WP(v) == null ? (double?)null : WP(v).Waermeproduktion_WP));
            l.Add(new Kennzahl("energie.wp_deckung", "Wärmedeckung Wärmepumpe", "Heat pump heat coverage", "%", GR_ENERGIE, "N1", false,
                v => WP(v) == null ? (double?)null : WP(v).Waermebedarfsdeckung));
            l.Add(new Kennzahl("energie.bhkw_waerme", "Wärmeerzeugung BHKW", "CHP heat output", "MWh/a", GR_ENERGIE, "N0", true,
                v => BH(v) == null ? (double?)null : BH(v).Waermeproduktion));
            l.Add(new Kennzahl("energie.bhkw_strom", "Stromerzeugung BHKW", "CHP electricity output", "MWh/a", GR_ENERGIE, "N0", true,
                v => BH(v) == null ? (double?)null : BH(v).Stromproduktion));
            l.Add(new Kennzahl("energie.kessel_waerme", "Wärmeerzeugung Spitzenkessel", "Boiler heat output", "MWh/a", GR_ENERGIE, "N0", true,
                v => HK(v) == null ? (double?)null : HK(v).Waermeproduktion));
            l.Add(new Kennzahl("energie.solar_waerme", "Wärmeerzeugung Solarthermie", "Solar thermal heat output", "MWh/a", GR_ENERGIE, "N0", true,
                v => SO(v) == null ? (double?)null : SO(v).Waermeproduktion));
            l.Add(new Kennzahl("energie.pv_strom", "Stromerzeugung PV", "PV electricity output", "MWh/a", GR_ENERGIE, "N0", true,
                v => PV(v) == null ? (double?)null : PV(v).Stromproduktion));

            l.Add(new Kennzahl("energie.brennstoff", "Brennstoffeinsatz gesamt", "Total fuel input", "MWh/a", GR_ENERGIE, "N0", true,
                v => (BH(v) == null && HK(v) == null) ? (double?)null : Brennstoffsumme(BH(v)) + Brennstoffsumme(HK(v))));
            l.Add(new Kennzahl("energie.netzbezug", "Netzbezug Strom", "Grid electricity import", "MWh/a", GR_ENERGIE, "N0", true,
                v => E(v) == null ? (double?)null : E(v).Stromrestbedarf));
            l.Add(new Kennzahl("energie.einspeisung", "Netzeinspeisung PV", "PV grid export", "MWh/a", GR_ENERGIE, "N0", true,
                v => PV(v) == null ? (double?)null : PV(v).Ueberschuss));
            l.Add(new Kennzahl("energie.waermerest", "Wärmerestbedarf (ungedeckt)", "Uncovered heat demand", "MWh/a", GR_ENERGIE, "N0", true,
                v => E(v) == null ? (double?)null : E(v).Waermerestbedarf));

            // PAKET E1: die Deckungsgrade JE BEDARFSART, über alle Erzeuger. Sie
            // beantworten die Frage, die der Gesamtdeckungsgrad verdeckt — ob die
            // Auslegung Warmwasser und Prozess ebenso trägt wie die Heizung. „—", wenn
            // das Projekt in diesem Kanal keinen Bedarf hat (Konzept 4.4).
            l.Add(new Kennzahl("energie.deckung_heizung", "Deckungsgrad Heizung", "Coverage space heating",
                "%", GR_ENERGIE, "N1", false, v => DeckungKanal(v, Kanal.HEIZUNG)));
            l.Add(new Kennzahl("energie.deckung_brauchwasser", "Deckungsgrad Brauchwasser", "Coverage domestic hot water",
                "%", GR_ENERGIE, "N1", false, v => DeckungKanal(v, Kanal.BRAUCHWASSER)));
            l.Add(new Kennzahl("energie.deckung_prozess", "Deckungsgrad Prozesswärme", "Coverage process heat",
                "%", GR_ENERGIE, "N1", false, v => DeckungKanal(v, Kanal.PROZESS)));
            // STUFE KU2 WELLE 3 (Kühlkonzept 6.4; E21): der Deckungsgrad des Kühlkanals neben den drei
            // Wärmekanälen — über den EIGENEN Zweig mit dem Kältenenner. null ohne Kälteerzeugung.
            l.Add(new Kennzahl(SCHLUESSEL_KAELTE_DECKUNGSGRAD, "Deckungsgrad Kühlung (sensibel)",
                "Coverage cooling (sensible)", "%", GR_ENERGIE, "N1", false,
                v => MitKaelteerzeugung(v) ? DeckungKanalKaelte(v) : null));

            // ---------------- Effizienz ----------------
            l.Add(new Kennzahl("eff.jaz", "Jahresarbeitszahl (JAZ) WP", "Heat pump SPF", "–", GR_EFFIZIENZ, "N2", true,
                v =>
                {
                    var w = WP(v);
                    if (w == null) return null;
                    double strom = w.Stromverbrauch_WP + w.Stromverbrauch_Heizstab;
                    return strom > 0 ? (double?)(w.Waermeproduktion_WP / strom) : null;
                }));
            l.Add(new Kennzahl("eff.wp_vbh", "Vollbenutzungsstunden WP", "Heat pump full-load hours", "h/a", GR_EFFIZIENZ, "N0", false,
                v => WP(v) == null ? (double?)null : WP(v).Vollbenutzungsstunden));
            // ETAPPE E2 — die Zeile hieß bis dahin „Betriebsstunden BHKW" und zeigte
            // Betriebsstunden_Gesamt. Der WERT ist unverändert, die BESCHRIFTUNG sagt jetzt,
            // was er ist: die Summe THERMISCHER Vollbenutzungsstunden über alle Module. Sie
            // kann 8.760 h überschreiten und war nie eine Betriebsstundenzahl — der
            // Rechenkern bildet keine Taktung ab. Der Schlüssel bleibt, damit gespeicherte
            // Baustein-Konfigurationen weiter greifen.
            l.Add(new Kennzahl("eff.bhkw_bh", "Vollbenutzungsstunden BHKW (thermisch, Σ Module)",
                "CHP full-load hours (thermal, sum of modules)", "h/a", GR_EFFIZIENZ, "N0", false,
                v => BH(v) == null ? (double?)null : BH(v).Betriebsstunden_Gesamt));
            // ETAPPE E2 (L6) — die für den KWK-Zuschlag maßgebliche Größe: leistungs-
            // gewichtete ELEKTRISCHE Vollbenutzungsstunden. 0 heißt „nicht erhoben"
            // (Ergebniszeile vor E2) und wird als „—" gezeigt, nicht als Null.
            l.Add(new Kennzahl("eff.bhkw_vbh_el", "Vollbenutzungsstunden BHKW (elektrisch)",
                "CHP full-load hours (electric)", "h/a", GR_EFFIZIENZ, "N0", false,
                v => (BH(v) == null || BH(v).VbhElektrisch <= 0) ? (double?)null : BH(v).VbhElektrisch));
            l.Add(new Kennzahl("eff.pv_eigen", "PV-Eigenverbrauchsquote", "PV self-consumption ratio", "%", GR_EFFIZIENZ, "N1", false,
                v =>
                {
                    var p = PV(v);
                    if (p == null || p.Stromproduktion <= 0) return null;
                    return (p.Stromproduktion - p.Ueberschuss) / p.Stromproduktion * 100.0;
                }));
            // PAKET P2 (Konzept 7.4): die Temperaturen der obersten Speicherschicht. Sie
            // beantworten, was Energiemengen nicht zeigen — ob der Vorrat auf dem
            // Temperaturniveau steht, das die Senken brauchen. „—", solange kein
            // Speicher einen Wert trägt (kein Senkenspeicher, oder ein Ergebnis von vor
            // Paket P1); die Aufschlüsselung je Speicher steht in der
            // Projektbeschreibung.
            l.Add(new Kennzahl("eff.t_oben_mittel", "Speichertemperatur oben (Mittel)",
                "Storage top temperature (mean)", "°C", GR_EFFIZIENZ, "N1", false, TObenMittel));
            l.Add(new Kennzahl("eff.t_oben_min", "Speichertemperatur oben (Minimum)",
                "Storage top temperature (minimum)", "°C", GR_EFFIZIENZ, "N1", false, TObenMin));

            l.Add(new Kennzahl("eff.autarkie", "Autarkiegrad Strom", "Electric self-sufficiency", "%", GR_EFFIZIENZ, "N1", true,
                v =>
                {
                    var e = E(v);
                    if (e == null || e.Strombedarf_Gesamt <= 0) return null;
                    // E34: Der Kältestrom über einen eigenen Zähler ist Bezug aus dem Netz, auch wenn
                    // er neben dem Netzbezug des Anschlusses steht — die Abrechnungsart hebt die
                    // Autarkie nicht (0 ohne eigenen Zähler).
                    double a = (1.0 - (e.Stromrestbedarf + v.KuehlzaehlerMWh) / e.Strombedarf_Gesamt) * 100.0;
                    return a < 0 ? 0 : a;
                }));

            // ---------------- Kälte (Stufe KU2 Welle 3; Kühlkonzept 6.2–6.4, K15, E21, E34) ----------------
            // Die Erzeugerkennzahlen der Kälte — Gegenstücke zu Wärmeerzeugung, Wärmerestbedarf und
            // JAZ der Wärmepumpe, dazu Kältestrom, sein Netzbezug, seine Kosten und Emissionen. Jede
            // Zeile trägt die Grenze der Zahl (K5: sensible Kälte ohne Entfeuchtung — auch der
            // Kältestrom enthält keine Entfeuchtungsarbeit). null ohne gerechnete Kälteerzeugung:
            // Ein Projekt ohne Kälteerzeuger zeigt die Gruppe nicht.
            l.Add(new Kennzahl(SCHLUESSEL_KAELTE_ERZEUGUNG, "Kälteerzeugung Wärmepumpe (sensibel)",
                "Heat pump cooling output (sensible)", "MWh/a", GR_KAELTE, "N1", true,
                v => WP(v)?.Kaelteproduktion_WP));
            l.Add(new Kennzahl(SCHLUESSEL_KAELTE_REST, "Kältebedarf ungedeckt (sensibel)",
                "Uncovered cooling demand (sensible)", "MWh/a", GR_KAELTE, "N1", true,
                v => MitKaelteerzeugung(v) ? E(v)?.Kaelterestbedarf : null));
            l.Add(new Kennzahl(SCHLUESSEL_KAELTE_JAZ, "Jahresarbeitszahl Kälte (sensibel)",
                "Seasonal EER (sensible)", "–", GR_KAELTE, "N2", true, JahresarbeitszahlKaelte));
            l.Add(new Kennzahl(SCHLUESSEL_KAELTE_STROM, "Kältestrom (sensibel)",
                "Cooling electricity (sensible)", "MWh/a", GR_KAELTE, "N2", true,
                v => WP(v)?.Stromverbrauch_Kuehlung));
            l.Add(new Kennzahl(SCHLUESSEL_KAELTE_NETZBEZUG, "Netzbezug Kältestrom (sensibel)",
                "Grid import for cooling (sensible)", "MWh/a", GR_KAELTE, "N2", true,
                v => MitKaelteerzeugung(v) ? v.KaeltestromNetzbezugMWh : null));
            l.Add(new Kennzahl(SCHLUESSEL_KAELTE_KOSTEN, "Kosten Kältestrom (sensibel)",
                "Cooling electricity cost (sensible)", "€/a", GR_KAELTE, "N0", true,
                v => MitKaelteerzeugung(v) ? v.KaeltestromKosten : null));
            l.Add(new Kennzahl(SCHLUESSEL_KAELTE_CO2,
                EmissionsAusweis.KennzahlKaeltestrom(modus, false),
                EmissionsAusweis.KennzahlKaeltestrom(modus, true),
                "t/a", GR_KAELTE, "N2", true,
                v => MitKaelteerzeugung(v) ? v.KaeltestromCO2t : null));

            // ---------------- Emissionen (KostenEmissionRechner; null = Faktoren fehlen) ----------------
            // Die Beschriftung NENNT DEN MODUS (Etappe E5, Konzept F7): „CO₂-Emissionen"
            // und „CO₂-Äquivalent (GWP₁₀₀)" sind zwei Größen, und zwei Berichte
            // desselben Projekts wären ohne die Angabe nicht vergleichbar.
            l.Add(new Kennzahl("em.co2",
                EmissionsAusweis.KennzahlGesamt(modus, false),
                EmissionsAusweis.KennzahlGesamt(modus, true),
                "t/a", GR_EMISSION, "N1", true,
                v => v.CO2Gesamt));
            l.Add(new Kennzahl("em.co2_spez",
                EmissionsAusweis.KennzahlSpezifisch(modus, false),
                EmissionsAusweis.KennzahlSpezifisch(modus, true),
                "g/kWh", GR_EMISSION, "N0", true,
                v => v.CO2Spezifisch));

            // ---------------- Kosten einfach (KostenEmissionRechner; null = Preise fehlen) ----------------
            l.Add(new Kennzahl("ko.energie", "Energiekosten p. a.", "Annual energy cost", "€/a", GR_KOSTEN, "N0", true,
                v => v.Energiekosten));
            l.Add(new Kennzahl("ko.stromsaldo", "Stromkosten Netzbezug", "Grid electricity cost", "€/a", GR_KOSTEN, "N0", true,
                v => v.StromkostenNetz));

            return l;
        }

        /// <summary>Berechnet alle Katalogwerte für ein Projekt in dessen Kennzahlen-Dictionary.</summary>
        public static void Berechne(VariantenDaten v)
        {
            if (v == null) return;
            v.Kennzahlen.Clear();
            foreach (Kennzahl k in Alle())
            {
                double? wert;
                try { wert = k.Wert(v); }
                catch { wert = null; }   // eine defekte Kennzahl kippt nicht den Bericht
                v.Kennzahlen[k.Schluessel] = wert;
            }
        }
    }
}
