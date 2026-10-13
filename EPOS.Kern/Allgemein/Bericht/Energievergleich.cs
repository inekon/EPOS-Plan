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

        /// <summary>
        /// Der stabile Schlüssel der Zeile: der Katalogschlüssel der Kennzahl, bei einer Trägerzeile der Endenergie
        /// <c>endenergie:&lt;CarrierId&gt;</c> bzw. <c>endenergie:kaeltestrom:&lt;CarrierId&gt;</c>
        /// (<see cref="Energievergleich.SchluesselTraeger"/>). Nie der Anzeigetext.
        /// </summary>
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

    /// <summary>
    /// Eine Gruppe des Energievergleichs als Daten: stabiler Schlüssel und Vorgabezustand. Ein späterer Benutzerzustand
    /// (offen, zugeklappt, ausgeblendet) überschreibt allein <see cref="VorgabeOffen"/>.
    /// </summary>
    public sealed class EnergievergleichGruppe
    {
        /// <summary>Der sprachneutrale, stabile Gruppenschlüssel („bedarf“ …).</summary>
        public string Schluessel { get; }

        /// <summary>Steht die Gruppe beim Öffnen offen (in der Tafel) oder zugeklappt (als Klappgruppe darunter)?</summary>
        public bool VorgabeOffen { get; }

        public EnergievergleichGruppe(string schluessel, bool vorgabeOffen)
        {
            Schluessel = schluessel;
            VorgabeOffen = vorgabeOffen;
        }
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

        /// <summary>Die schlanke Effizienzgruppe: Jahresarbeitszahlen (Wärmepumpe, Kälte) und Vollbenutzungsstunden.</summary>
        public const string GRUPPE_EFFIZIENZ = "effizienz";

        /// <summary>Die übrigen Effizienzkennzahlen: Bivalenz, Kennlinienstunden, Speichertemperaturen, Takt und Teillast.</summary>
        public const string GRUPPE_EFFIZIENZ_WEITERE = "effizienz_weitere";

        /// <summary>Die Gebäudekennzahlen (Lüftung, Aufheizen, Vorheizen).</summary>
        public const string GRUPPE_GEBAEUDE = "gebaeude";

        /// <summary>Die CO₂-Kennzahlen des Katalogs (gesamt, spezifisch, Kältestrom) — am Ende des Abschnitts.</summary>
        public const string GRUPPE_EMISSIONEN = "emissionen";

        /// <summary>
        /// Die Gruppen in Anzeigereihenfolge mit ihrem Vorgabezustand (Anwenderentscheid 13.10.2026): offen Bedarf,
        /// Wärmeerzeugung, Strom, Endenergie, Deckung und Effizienz; zugeklappt Kälte, weitere Effizienzwerte, Gebäude
        /// und am Ende die Emissionen. Die einzige Stelle der Vorgabe.
        /// </summary>
        public static readonly IReadOnlyList<EnergievergleichGruppe> GRUPPEN_DATEN = new[]
        {
            new EnergievergleichGruppe(GRUPPE_BEDARF, true),
            new EnergievergleichGruppe(GRUPPE_ERZEUGUNG, true),
            new EnergievergleichGruppe(GRUPPE_STROM, true),
            new EnergievergleichGruppe(GRUPPE_ENDENERGIE, true),
            new EnergievergleichGruppe(GRUPPE_DECKUNG, true),
            new EnergievergleichGruppe(GRUPPE_EFFIZIENZ, true),
            new EnergievergleichGruppe(GRUPPE_KAELTE, false),
            new EnergievergleichGruppe(GRUPPE_EFFIZIENZ_WEITERE, false),
            new EnergievergleichGruppe(GRUPPE_GEBAEUDE, false),
            new EnergievergleichGruppe(GRUPPE_EMISSIONEN, false)
        };

        /// <summary>Die Gruppenschlüssel in Anzeigereihenfolge.</summary>
        public static readonly IReadOnlyList<string> GRUPPEN = GRUPPEN_DATEN.Select(g => g.Schluessel).ToArray();

        /// <summary>Der Vorgabezustand einer Gruppe: offen beim Öffnen? Unbekannte Gruppen stehen offen.</summary>
        public static bool VorgabeOffen(string gruppe)
            => GRUPPEN_DATEN.FirstOrDefault(g => g.Schluessel == gruppe)?.VorgabeOffen ?? true;

        /// <summary>Der Schlüsselanfang der Trägerzeilen der Endenergie.</summary>
        public const string PRAEFIX_TRAEGER = "endenergie:";

        /// <summary>Der Schlüsselanfang der Trägerzeilen des Kältestroms mit abweichendem Kühlträger.</summary>
        public const string PRAEFIX_KAELTESTROM = "endenergie:kaeltestrom:";

        /// <summary>Der stabile Schlüssel einer Trägerzeile.</summary>
        public static string SchluesselTraeger(int carrierId, bool kaeltestrom)
            => (kaeltestrom ? PRAEFIX_KAELTESTROM : PRAEFIX_TRAEGER) + carrierId.ToString(CultureInfo.InvariantCulture);

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

            ["eff.jaz"] = GRUPPE_EFFIZIENZ,
            [KennzahlenKatalog.SCHLUESSEL_KAELTE_JAZ] = GRUPPE_EFFIZIENZ,
            ["eff.wp_vbh"] = GRUPPE_EFFIZIENZ,
            ["eff.bhkw_bh"] = GRUPPE_EFFIZIENZ,
            ["eff.bhkw_vbh_el"] = GRUPPE_EFFIZIENZ,

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
            if (k.Gruppe == KennzahlenKatalog.GR_EFFIZIENZ) return GRUPPE_EFFIZIENZ_WEITERE;
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
            // Die Trägerzeilen stehen am Ende der Endenergie, unter Brennstoffeinsatz und Netzbezug.
            jeGruppe[GRUPPE_ENDENERGIE].AddRange(Traegerzeilen(versionen, erg.Simuliert, englisch, kultur));
            foreach (string g in GRUPPEN) erg.Zeilen.AddRange(jeGruppe[g]);
            return erg;
        }

        /// <summary>
        /// Die Trägerzeilen der Endenergie (Anwenderentscheid 13.10.2026): je Energieträger des Kostennachweises
        /// (<see cref="VariantenDaten.EnergiekostenJeTraeger"/>) die Menge in MWh/a, ohne Euro, in der Reihenfolge des
        /// ersten Auftretens. Der Eintrag des Netzstroms entfällt: Er ist der Netzbezug der Zeile „Netzbezug Strom“
        /// (ohne abweichenden Kühlträger Zahl für Zahl derselbe, sonst derselbe ohne den Kältestrom). Kältestrom eines
        /// abweichenden Kühlträgers steht als eigene, ausdrücklich benannte Zeile. Ein Träger, den eine Version nicht
        /// führt, zeigt dort „—“.
        /// </summary>
        private static List<EnergievergleichZeile> Traegerzeilen(IReadOnlyList<VariantenDaten> versionen, List<bool> simuliert,
                                                                 bool englisch, CultureInfo kultur)
        {
            const string FORMAT = "N0";   // MWh ganzzahlig wie Brennstoffeinsatz und Netzbezug des Katalogs
            var schluessel = new List<string>();
            var namen = new Dictionary<string, string>();
            var mengen = new List<Dictionary<string, double>>();
            for (int i = 0; i < versionen.Count; i++)
            {
                var je = new Dictionary<string, double>();
                mengen.Add(je);
                if (!simuliert[i] || versionen[i].EnergiekostenJeTraeger == null) continue;
                foreach (EnergieTraegerNachweis t in versionen[i].EnergiekostenJeTraeger)
                {
                    if (t == null || t.Netzstrom) continue;
                    string s = SchluesselTraeger(t.CarrierId, IstKaeltestrom(t));
                    if (!namen.ContainsKey(s))
                    {
                        schluessel.Add(s);
                        namen[s] = string.IsNullOrWhiteSpace(t.Traeger)
                            ? t.CarrierId.ToString(CultureInfo.InvariantCulture) : t.Traeger.Trim();
                    }
                    je[s] = (je.TryGetValue(s, out double m) ? m : 0.0) + t.MengeMWh;
                }
            }

            CultureInfo sprache = CultureInfo.GetCultureInfo(englisch ? "en-US" : "de-DE");
            var zeilen = new List<EnergievergleichZeile>();
            foreach (string s in schluessel)
            {
                bool kaelte = s.StartsWith(PRAEFIX_KAELTESTROM, StringComparison.Ordinal);
                string format = MyResource.Resource.ResourceManager.GetString(
                    kaelte ? "BK_ERG_ENDENERGIE_KAELTESTROM" : "BK_ERG_ENDENERGIE_TRAEGER", sprache) ?? "{0}";
                var z = new EnergievergleichZeile
                {
                    Gruppe = GRUPPE_ENDENERGIE, Schluessel = s, Bezeichnung = string.Format(format, namen[s]), Einheit = "MWh/a"
                };
                for (int i = 0; i < versionen.Count; i++)
                {
                    double? w = simuliert[i] && mengen[i].TryGetValue(s, out double m) && !double.IsNaN(m) && !double.IsInfinity(m)
                        ? m : (double?)null;
                    z.Werte.Add(w);
                    z.Texte.Add(Tabellenformat.FW(w, FORMAT, kultur));
                }
                if (z.Werte.All(w => !w.HasValue)) continue;
                for (int i = 0; i < versionen.Count; i++)
                {
                    bool ab = i > 0 && simuliert[0] && simuliert[i] && z.Texte[i] != z.Texte[0];
                    z.WeichtAb.Add(ab);
                    z.Abweichung.Add(ab ? Prozent(z.Werte[0], z.Werte[i], kultur) : "");
                }
                zeilen.Add(z);
            }
            return zeilen;
        }

        /// <summary>
        /// Ist ein Trägereintrag Kältestrom eines abweichenden Kühlträgers? So legt ihn der Kostenrechner an: kein
        /// Netzstrom, keine Wärmemenge eines Erzeugers.
        /// </summary>
        private static bool IstKaeltestrom(EnergieTraegerNachweis t) => !t.Netzstrom && t.WaermeMengeMWh == 0.0;

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
