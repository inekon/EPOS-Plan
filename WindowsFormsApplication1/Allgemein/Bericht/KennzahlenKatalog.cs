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

        // Kurzzugriffe (null-tolerant) --------------------------------------

        private static ErgebnisEnergiebedarfModel E(VariantenDaten v) { return v?.Ergebnis?.Energiebedarf; }
        private static ErgebnisWaermepumpeModel WP(VariantenDaten v) { return v?.Ergebnis?.Waermepumpe; }
        private static ErgebnisBHKWModel BH(VariantenDaten v) { return v?.Ergebnis?.BHKW; }
        private static ErgebnisHeizkesselModel HK(VariantenDaten v) { return v?.Ergebnis?.Heizkessel; }
        private static ErgebnisSolarthermieModel SO(VariantenDaten v) { return v?.Ergebnis?.Solarthermie; }
        private static ErgebnisPhotovoltaikModel PV(VariantenDaten v) { return v?.Ergebnis?.Photovoltaik; }

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
        /// Der komplette Katalog in Anzeige-Reihenfolge.
        /// Emissions- und Kostenkennzahlen liefern bis zur Umsetzung der Verrechnung
        /// (Emissionsfaktoren × Verbräuche bzw. Menge × Preis über carrier_id, Phase 5)
        /// bewusst null — sie erscheinen als „—", nie als 0 (Konzept Kap. 5).
        /// </summary>
        public static List<Kennzahl> Alle()
        {
            var l = new List<Kennzahl>();

            // ---------------- Energiebilanz ----------------
            l.Add(new Kennzahl("energie.waermebedarf", "Wärmebedarf gesamt", "Total heat demand", "MWh/a", GR_ENERGIE, "N0", true,
                v => E(v) == null ? (double?)null : E(v).Waermebedarf_Gesamt));
            l.Add(new Kennzahl("energie.waermelast", "Wärmelast max.", "Peak heat load", "kW", GR_ENERGIE, "N0", true,
                v => E(v) == null ? (double?)null : E(v).Waermelast_Max));
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
            l.Add(new Kennzahl("eff.autarkie", "Autarkiegrad Strom", "Electric self-sufficiency", "%", GR_EFFIZIENZ, "N1", true,
                v =>
                {
                    var e = E(v);
                    if (e == null || e.Strombedarf_Gesamt <= 0) return null;
                    double a = (1.0 - e.Stromrestbedarf / e.Strombedarf_Gesamt) * 100.0;
                    return a < 0 ? 0 : a;
                }));

            // ---------------- Emissionen (KostenEmissionRechner; null = Faktoren fehlen) ----------------
            l.Add(new Kennzahl("em.co2", "CO₂-Emissionen gesamt", "Total CO₂ emissions", "t/a", GR_EMISSION, "N1", true,
                v => v.CO2Gesamt));
            l.Add(new Kennzahl("em.co2_spez", "CO₂ spezifisch (Wärme)", "Specific CO₂ (heat)", "g/kWh", GR_EMISSION, "N0", true,
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
