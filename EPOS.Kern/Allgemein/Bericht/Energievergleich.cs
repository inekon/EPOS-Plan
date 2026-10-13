using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Eine Zeile des Energievergleichs der Übersicht: eine Kennzahl des <see cref="KennzahlenKatalog"/>, je Version ein
    /// Wert, die Abweichung zum Stamm entschieden.
    /// </summary>
    public sealed class EnergievergleichZeile
    {
        /// <summary>Der sprachneutrale Gruppenschlüssel (<see cref="Energievergleich.GRUPPE_BEDARF"/> …).</summary>
        public string Gruppe = "";

        /// <summary>Der Katalogschlüssel der Kennzahl.</summary>
        public string Schluessel = "";

        /// <summary>Die Beschriftung der Kennzahl in der Anzeigesprache.</summary>
        public string Bezeichnung = "";

        /// <summary>Die Einheit, sprachneutral; leer bei einer dimensionslosen Zahl.</summary>
        public string Einheit = "";

        /// <summary>Je Version der Wert, der Stamm zuerst; <c>null</c> = nicht verfügbar oder nicht simuliert.</summary>
        public List<double?> Werte = new List<double?>();

        /// <summary>Je Version der Wert im Format der Kennzahl; „—“ ohne Wert.</summary>
        public List<string> Texte = new List<string>();

        /// <summary>Je Version: weicht der Wert vom Stamm ab? Beim Stamm immer <c>false</c>.</summary>
        public List<bool> WeichtAb = new List<bool>();

        /// <summary>
        /// Je Version die Abweichung in Prozent zum Stamm („+12,3 %“); leer beim Stamm, bei einer Kennzahl ohne
        /// Δ-Ausweis (<see cref="Kennzahl.DeltaAnzeigen"/>) und wo sie nicht berechenbar ist.
        /// </summary>
        public List<string> Abweichung = new List<string>();

        /// <summary>Weicht wenigstens eine Version vom Stamm ab?</summary>
        public bool IrgendeineAbweichung => WeichtAb.Any(w => w);
    }

    /// <summary>Das Ergebnis des Energievergleichs: je Version, ob sie simuliert ist, und die Zeilen.</summary>
    public sealed class EnergievergleichErgebnis
    {
        /// <summary>Je Version: liegt ein Simulationsergebnis vor? Der Stamm zuerst.</summary>
        public List<bool> Simuliert = new List<bool>();

        /// <summary>Die Zeilen in Gruppen- und Katalogreihenfolge; nur Kennzahlen, die wenigstens eine Version führt.</summary>
        public List<EnergievergleichZeile> Zeilen = new List<EnergievergleichZeile>();

        /// <summary>Ist keine Version simuliert?</summary>
        public bool KeineSimuliert => !Simuliert.Any(s => s);
    }

    /// <summary>
    /// Der Energievergleich der Übersicht (Anwenderauftrag 13.10.2026): die ENERGIE-Kennzahlen des
    /// <see cref="KennzahlenKatalog"/> — dieselben Zeilen und Werte wie die Kennzahlentafel des Berichts — je Version
    /// nebeneinander, gegliedert in Bedarf, Erzeugung, Strom, Endenergie, Deckung, Kälte, Effizienz, Gebäude und
    /// Emissionen (CO₂ wie im Katalog, Ergänzung des Anwenders vom 13.10.2026). Kosten gehören nicht hinein (sie stehen
    /// in der Wirtschaftlichkeit). Ohne Datenbank: Die Versionen
    /// kommen gesammelt herein (<see cref="EnergievergleichCtrl"/>).
    /// </summary>
    public static class Energievergleich
    {
        /// <summary>Wärme-, Kälte- und Strombedarf samt Höchstlasten.</summary>
        public const string GRUPPE_BEDARF = "bedarf";

        /// <summary>Wärme je Erzeuger.</summary>
        public const string GRUPPE_ERZEUGUNG = "erzeugung";

        /// <summary>Stromerzeugung, Einspeisung, Eigenverbrauch und Autarkie.</summary>
        public const string GRUPPE_STROM = "strom";

        /// <summary>Brennstoffeinsatz und Netzbezug.</summary>
        public const string GRUPPE_ENDENERGIE = "endenergie";

        /// <summary>Deckungsgrade und ungedeckter Bedarf.</summary>
        public const string GRUPPE_DECKUNG = "deckung";

        /// <summary>Die Erzeugerkennzahlen der Kälte ohne Kosten und CO₂.</summary>
        public const string GRUPPE_KAELTE = "kaelte";

        /// <summary>Jahresarbeitszahl, Vollbenutzungsstunden und die übrigen Effizienzkennzahlen.</summary>
        public const string GRUPPE_EFFIZIENZ = "effizienz";

        /// <summary>Die Gebäudekennzahlen (Lüftung, Aufheizen, Vorheizen).</summary>
        public const string GRUPPE_GEBAEUDE = "gebaeude";

        /// <summary>Die CO₂-Kennzahlen des Katalogs (gesamt, spezifisch, Kältestrom) — am Ende des Abschnitts.</summary>
        public const string GRUPPE_EMISSIONEN = "emissionen";

        /// <summary>Die Gruppen in Anzeigereihenfolge.</summary>
        public static readonly IReadOnlyList<string> GRUPPEN = new[]
        {
            GRUPPE_BEDARF, GRUPPE_ERZEUGUNG, GRUPPE_STROM, GRUPPE_ENDENERGIE,
            GRUPPE_DECKUNG, GRUPPE_KAELTE, GRUPPE_EFFIZIENZ, GRUPPE_GEBAEUDE, GRUPPE_EMISSIONEN
        };

        /// <summary>Die Zuordnung der Energiebilanz-Kennzahlen zu den Gruppen der Übersicht; was fehlt, steht unter Erzeugung.</summary>
        private static readonly Dictionary<string, string> s_zuordnung = new Dictionary<string, string>
        {
            ["energie.waermebedarf"] = GRUPPE_BEDARF,
            ["energie.waermebedarf_heizung"] = GRUPPE_BEDARF,
            ["energie.waermebedarf_brauchwasser"] = GRUPPE_BEDARF,
            ["energie.waermebedarf_prozess"] = GRUPPE_BEDARF,
            ["energie.waermelast"] = GRUPPE_BEDARF,
            [KennzahlenKatalog.SCHLUESSEL_KAELTE_JAHRESBEDARF] = GRUPPE_BEDARF,
            [KennzahlenKatalog.SCHLUESSEL_KAELTE_SPITZE] = GRUPPE_BEDARF,
            [KennzahlenKatalog.SCHLUESSEL_KAELTE_STUNDEN] = GRUPPE_BEDARF,
            ["energie.strombedarf"] = GRUPPE_BEDARF,
            ["energie.strommax"] = GRUPPE_BEDARF,

            ["energie.wp_waerme"] = GRUPPE_ERZEUGUNG,
            ["energie.bhkw_waerme"] = GRUPPE_ERZEUGUNG,
            ["energie.kessel_waerme"] = GRUPPE_ERZEUGUNG,
            ["energie.solar_waerme"] = GRUPPE_ERZEUGUNG,

            ["energie.bhkw_strom"] = GRUPPE_STROM,
            ["energie.pv_strom"] = GRUPPE_STROM,
            ["energie.einspeisung"] = GRUPPE_STROM,
            ["eff.pv_eigen"] = GRUPPE_STROM,
            ["eff.autarkie"] = GRUPPE_STROM,

            ["energie.brennstoff"] = GRUPPE_ENDENERGIE,
            ["energie.netzbezug"] = GRUPPE_ENDENERGIE,

            ["energie.wp_deckung"] = GRUPPE_DECKUNG,
            ["energie.waermerest"] = GRUPPE_DECKUNG,
            ["energie.deckung_heizung"] = GRUPPE_DECKUNG,
            ["energie.deckung_brauchwasser"] = GRUPPE_DECKUNG,
            ["energie.deckung_prozess"] = GRUPPE_DECKUNG,
            [KennzahlenKatalog.SCHLUESSEL_KAELTE_DECKUNGSGRAD] = GRUPPE_DECKUNG,

            [KennzahlenKatalog.SCHLUESSEL_KAELTE_CO2] = GRUPPE_EMISSIONEN,
        };

        /// <summary>
        /// Die Kennzahlen des Vergleichs: der Katalog ohne die Gruppe Kosten und ohne die Kostenzeile der Kälte; die
        /// CO₂-Beschriftung im Modus <paramref name="modus"/> (<see cref="EmissionsAusweis.ModusAusVarianten"/>).
        /// </summary>
        public static List<Kennzahl> Kennzahlen(string modus = DbWerte.EMISSION_MODUS_CO2)
        {
            return KennzahlenKatalog.Alle(modus).Where(GehoertDazu).ToList();
        }

        /// <summary>Gehört die Kennzahl in den Vergleich? Energie und Emissionen ja, Kosten nein.</summary>
        public static bool GehoertDazu(Kennzahl k)
        {
            if (k == null) return false;
            if (k.Gruppe == KennzahlenKatalog.GR_KOSTEN) return false;
            if (k.Schluessel == KennzahlenKatalog.SCHLUESSEL_KAELTE_KOSTEN) return false;
            return (k.Einheit ?? "").IndexOf('€') < 0;
        }

        /// <summary>Der Gruppenschlüssel der Übersicht für eine Kennzahl.</summary>
        public static string Gruppe(Kennzahl k)
        {
            if (s_zuordnung.TryGetValue(k.Schluessel, out string g)) return g;
            if (k.Gruppe == KennzahlenKatalog.GR_KAELTE) return GRUPPE_KAELTE;
            if (k.Gruppe == KennzahlenKatalog.GR_EFFIZIENZ) return GRUPPE_EFFIZIENZ;
            if (k.Gruppe == KennzahlenKatalog.GR_GEBAEUDE) return GRUPPE_GEBAEUDE;
            if (k.Gruppe == KennzahlenKatalog.GR_EMISSION) return GRUPPE_EMISSIONEN;
            return GRUPPE_ERZEUGUNG;
        }

        /// <summary>
        /// Stellt die Versionen gegenüber, der Stamm zuerst. Eine Version ohne Simulationsergebnis (<c>null</c> oder ohne
        /// <see cref="VariantenDaten.Ergebnis"/>) führt in jeder Zeile „—“ und weicht nicht ab; eine Zeile, die keine Version
        /// führt, entfällt. Abweichend ist ein Wert, dessen Anzeige im Format der Kennzahl von der des Stamms verschieden
        /// ist — oder der dort fehlt, wo der simulierte Stamm einen führt (und umgekehrt).
        /// </summary>
        public static EnergievergleichErgebnis Vergleiche(IReadOnlyList<VariantenDaten> versionen, bool englisch,
                                                          CultureInfo kultur)
        {
            var erg = new EnergievergleichErgebnis();
            if (versionen == null || versionen.Count == 0) return erg;
            kultur ??= CultureInfo.CurrentCulture;

            foreach (VariantenDaten v in versionen) erg.Simuliert.Add(v != null && v.Ergebnis != null);

            var jeGruppe = GRUPPEN.ToDictionary(g => g, g => new List<EnergievergleichZeile>());
            string modus = EmissionsAusweis.ModusAusVarianten(versionen.Where(v => v != null && v.Ergebnis != null));
            foreach (Kennzahl k in Kennzahlen(modus))
            {
                var z = new EnergievergleichZeile
                {
                    Gruppe = Gruppe(k),
                    Schluessel = k.Schluessel,
                    Bezeichnung = k.Label(englisch),
                    Einheit = k.Einheit == "–" ? "" : (k.Einheit ?? "")
                };
                for (int i = 0; i < versionen.Count; i++)
                {
                    double? w = erg.Simuliert[i] ? Wert(versionen[i], k) : null;
                    z.Werte.Add(w);
                    z.Texte.Add(Tabellenformat.FW(w, k.Format, kultur));
                }
                if (z.Werte.All(w => !w.HasValue)) continue;

                for (int i = 0; i < versionen.Count; i++)
                {
                    bool ab = i > 0 && erg.Simuliert[0] && erg.Simuliert[i] && z.Texte[i] != z.Texte[0];
                    z.WeichtAb.Add(ab);
                    z.Abweichung.Add(ab && k.DeltaAnzeigen ? Prozent(z.Werte[0], z.Werte[i], kultur) : "");
                }
                jeGruppe[z.Gruppe].Add(z);
            }
            foreach (string g in GRUPPEN) erg.Zeilen.AddRange(jeGruppe[g]);
            return erg;
        }

        /// <summary>Der Wert einer Kennzahl: aus dem berechneten Satz, sonst frisch; nicht endlich = <c>null</c>.</summary>
        private static double? Wert(VariantenDaten v, Kennzahl k)
        {
            double? w;
            if (v.Kennzahlen != null && v.Kennzahlen.TryGetValue(k.Schluessel, out double? gespeichert)) w = gespeichert;
            else
            {
                try { w = k.Wert(v); }
                catch { w = null; }   // eine defekte Kennzahl kippt nicht den Vergleich
            }
            return w.HasValue && !double.IsNaN(w.Value) && !double.IsInfinity(w.Value) ? w : null;
        }

        /// <summary>Die Abweichung in Prozent zum Stamm („+12,3 %“); leer, wo sie nicht berechenbar ist.</summary>
        private static string Prozent(double? stamm, double? wert, CultureInfo kultur)
        {
            double? p = Tabellenformat.DeltaProzentWert(stamm, wert);
            if (!p.HasValue) return "";
            string betrag = Math.Abs(p.Value).ToString("N1", kultur) + " %";
            if (p.Value > 0.05) return "+" + betrag;
            if (p.Value < -0.05) return "−" + betrag;
            return "±" + 0.0.ToString("N1", kultur) + " %";
        }
    }
}
