using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE VORGABEN DER PUFFERSPEICHER-AUSLEGUNG - EINE Quelle der Zahlen (Konzept
    // Pufferspeicher-Auslegung, Abschnitt 4.1). Aus dieser Liste speisen sich
    //   (1) die Saat des Schemaschritts PufferAuslegungSchema (INSERT OR IGNORE in
    //       Tab_PufferAuslegungParameter_STAMM) und
    //   (2) der Rueckfall von PufferAuslegungParameter, wenn die Tabelle fehlt oder ein
    //       Schluessel nicht in ihr steht.
    // Wer eine Zahl aendert, aendert sie HIER; die Saat bestehender Datenbanken bleibt
    // (INSERT OR IGNORE), der Rueckfall folgt sofort.
    //
    // Normen und Richtlinien stehen nur als Quellenangabe (Zitat), nie als abgeschriebener
    // Text. Keine Produkt- oder Herstellerdaten; Beispielwerte sind runde, neutrale Zahlen.
    // ====================================================================================

    /// <summary>Die Art der Herkunft eines Vorgabewerts (Spalte <c>Herkunftsart</c>).</summary>
    public static class PufferHerkunftsart
    {
        /// <summary>Norm oder Richtlinie (auch Entwurf), zitiert.</summary>
        public const string RICHTLINIE = "RICHTLINIE";
        /// <summary>Feldtest, Studie, Whitepaper.</summary>
        public const string STUDIE = "STUDIE";
        /// <summary>Festlegung von EPOS-Plan (Konzept oder Kern).</summary>
        public const string SETZUNG = "SETZUNG";
        /// <summary>Sekundärquelle (Fachportal, Planungsunterlage ohne Normrang).</summary>
        public const string SEKUNDAER = "SEKUNDAER";

        /// <summary>Die vier zulässigen Werte in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<string> ALLE = new[] { RICHTLINIE, STUDIE, SETZUNG, SEKUNDAER };
    }

    /// <summary>Ein Vorgabewert: Schlüssel, Wert, Einheit, Quelle (Zitat) und Herkunftsart.</summary>
    public sealed record PufferVorgabe(string Schluessel, double Wert, string Einheit, string Quelle, string Herkunftsart);

    /// <summary>
    /// Die Vorgabewerte der Pufferspeicher-Auslegung (Konzept 4.1) — die eine Liste, aus der Saat
    /// und Rückfall gespeist werden. Alle Schlüssel tragen das Präfix <see cref="PRAEFIX"/>.
    /// </summary>
    public static class PufferAuslegungVorgaben
    {
        /// <summary>Präfix aller Schlüssel.</summary>
        public const string PRAEFIX = "Pufferauslegung.";

        // ---- Quellen (Zitate, kein Normtext) ----
        private const string Q_KERN = "EPOS-Plan Rechenkern (ProjektPuffer.WH_JE_LITER_KELVIN)";
        private const string Q_VDI_783 = "VDI 4645 E 2026-03, Abschnitt 7.8.3";
        private const string Q_VDI_784 = "VDI 4645 E 2026-03, Abschnitt 7.8.4";
        private const string Q_VDI_88 = "VDI 4645 E 2026-03, Abschnitt 8.8";
        private const string Q_VDI_T14 = "VDI 4645 E 2026-03, Tabelle 14";
        private const string Q_VDI_T15 = "VDI 4645 E 2026-03, Tabelle 15";
        private const string Q_VDI_GL23 = "VDI 4645 E 2026-03, Gleichung 23 (Raumtemperatur)";
        private const string Q_VDI_ANH_I = "VDI 4645 E 2026-03, Anhang I";
        private const string Q_BACOGA = "BaCoGa, Anlagenwasserinhalt je Übergabeart (Sekundärquelle)";
        private const string Q_WHITEPAPER = "VDI-Whitepaper Thermische Speicher in Wärmepumpensystemen (RWTH)";
        private const string Q_VDZ = "Herstellerangabe nach VdZ 2025 (Sekundärquelle)";
        private const string Q_WPQS = "Fraunhofer ISE, Feldtest WP-QS";
        private const string Q_SEKUNDAER_TAKT = "Fachportale zur Taktung (Sekundärquelle)";
        private const string Q_TOOL = "Wärmespeicher-Tool (Quellen/Waermespeicher-Tool), Lastgang-Simulation";
        private const string Q_EN15450 = "DIN EN 15450";
        private const string Q_BIMSCHV = "1. BImSchV § 5";
        private const string Q_BEG = "BEG Einzelmaßnahmen, Fördervoraussetzung Pufferspeicher";
        private const string Q_EN303 = "DIN EN 303-5";
        private const string Q_SOLAR = "Planungshilfen Solarthermie (Sekundärquelle), Bezug DIN EN 12977";
        private const string Q_RUNDE2 = "Recherche Pufferspeicher-Auslegung Runde 2, Abschnitt 4.1";
        private const string Q_ZIRK = "Fachportale zur Zirkulation (Sekundärquelle)";
        private const string Q_ECOSIZER = "Ecosizer, Zirkulationsverlust je Wohneinheit (Sekundärquelle)";
        private const string Q_BHKW = "Fachportal KWK-Flexibilisierung (Sekundärquelle)";
        private const string Q_EN15332 = "DIN EN 15332, Abschnitt 5.3";
        private const string Q_812 = "Delegierte Verordnung (EU) 812/2013";
        private const string Q_IEA = "IEA SHC Task/Annex 46";
        private const string Q_EN15316 = "prEN 15316-5:2024, Tabelle B.4";
        private const string Q_V39 = "Recherche Pufferspeicher-Auslegung Runde 3, Vorschlag V39";
        private const string Q_PUFFER = "EPOS-Plan Vorgabe des Projektpuffers (Schwellen)";
        private const string Q_VORLAGE = "Recherche Pufferspeicher-Auslegung Runde 1, Abschnitt 6";
        private const string Q_V30 = "Konzept Pufferauslegung V30 / KP3";

        /// <summary>Die Ressourcenschlüssel der Quellen (Zitate) — <see cref="Quellentext"/>.</summary>
        private static readonly Dictionary<string, string> QUELLENSCHLUESSEL = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [Q_KERN] = "PAUS_HERK_Q_KERN",
            [Q_VDI_783] = "PAUS_HERK_Q_VDI_783",
            [Q_VDI_784] = "PAUS_HERK_Q_VDI_784",
            [Q_VDI_88] = "PAUS_HERK_Q_VDI_88",
            [Q_VDI_T14] = "PAUS_HERK_Q_VDI_T14",
            [Q_VDI_T15] = "PAUS_HERK_Q_VDI_T15",
            [Q_VDI_GL23] = "PAUS_HERK_Q_VDI_GL23",
            [Q_VDI_ANH_I] = "PAUS_HERK_Q_VDI_ANH_I",
            [Q_BACOGA] = "PAUS_HERK_Q_BACOGA",
            [Q_WHITEPAPER] = "PAUS_HERK_Q_WHITEPAPER",
            [Q_VDZ] = "PAUS_HERK_Q_VDZ",
            [Q_WPQS] = "PAUS_HERK_Q_WPQS",
            [Q_SEKUNDAER_TAKT] = "PAUS_HERK_Q_SEKUNDAER_TAKT",
            [Q_TOOL] = "PAUS_HERK_Q_TOOL",
            [Q_EN15450] = "PAUS_HERK_Q_EN15450",
            [Q_BIMSCHV] = "PAUS_HERK_Q_BIMSCHV",
            [Q_BEG] = "PAUS_HERK_Q_BEG",
            [Q_EN303] = "PAUS_HERK_Q_EN303",
            [Q_SOLAR] = "PAUS_HERK_Q_SOLAR",
            [Q_RUNDE2] = "PAUS_HERK_Q_RUNDE2",
            [Q_ZIRK] = "PAUS_HERK_Q_ZIRK",
            [Q_ECOSIZER] = "PAUS_HERK_Q_ECOSIZER",
            [Q_BHKW] = "PAUS_HERK_Q_BHKW",
            [Q_EN15332] = "PAUS_HERK_Q_EN15332",
            [Q_812] = "PAUS_HERK_Q_812",
            [Q_IEA] = "PAUS_HERK_Q_IEA",
            [Q_EN15316] = "PAUS_HERK_Q_EN15316",
            [Q_V39] = "PAUS_HERK_Q_V39",
            [Q_PUFFER] = "PAUS_HERK_Q_PUFFER",
            [Q_VORLAGE] = "PAUS_HERK_Q_VORLAGE",
            [Q_V30] = "PAUS_HERK_Q_V30",
        };

        /// <summary>
        /// Die Quelle eines Vorgabewerts (Spalte <c>Quelle</c>) als <see cref="Textbaustein"/>: eine Quelle der
        /// eingebauten Liste trägt ihren Ressourcenschlüssel <c>PAUS_HERK_Q_*</c>, eine fremde bleibt Klartext.
        /// </summary>
        public static Textbaustein Quellentext(string quelle)
        {
            if (string.IsNullOrEmpty(quelle)) return Textbaustein.Leer;
            return QUELLENSCHLUESSEL.TryGetValue(quelle, out string schluessel)
                ? Textbaustein.T(schluessel, quelle)
                : Textbaustein.Klar(quelle);
        }

        // ---- Die Schlüssel, die der Rechenkern liest (ohne Präfix) ----
        public const string KONSTANTE = "Konstante.Wh_je_l_K";
        public const string VORPRUEFUNG = "Vorpruefung.Anlagenvolumen_l_kW";
        public const string ANLAGENVOLUMEN = "Anlagenvolumen.";                 // + <Uebergabe>_l_kW
        public const string FAUST_FIXED = "Faustwert.FixedSpeed_l_kW";
        public const string FAUST_GEREGELT = "Faustwert.Geregelt_l_kW";
        public const string ABTAU_RESERVE = "Abtau.Reserve_l_kW";
        public const string MINDESTLAUFZEIT = "Mindestlaufzeit_min";
        public const string STILLSTAND = "Sperrzeit.Stillstand.";               // + <HG>.<Uebergabe>_h
        public const string UEBERTEMPERATUR = "Sperrzeit.Uebertemperatur.";     // + <Uebergabe>_K
        public const string UEBERLADUNG = "Sperrzeit.Ueberladung_K";
        public const string RAUMTEMPERATUR_HEIZUNG = "Sperrzeit.Raumtemperatur_C";
        public const string STARTZIEL = "Takt.Startziel_je_Tag";
        public const string WARNSCHWELLE = "Takt.Warnschwelle_je_Tag";
        public const string HEIZPERIODE_MAX = "Takt.Heizperiode_Max";
        public const string DECKUNG_ZIEL = "Deckung.Ziel";
        public const string PRAXISGRENZE = "Deckung.Praxisgrenze_l";
        public const string BAND = "Band.";                                     // + <Art>.Min_l_kW / .Max_l_kW
        public const string FB_SCHEITHOLZ = "Festbrennstoff.Scheitholz_l_kW";
        public const string FB_PELLETS = "Festbrennstoff.Pellets_l_kW";
        public const string FB_GESETZ = "Festbrennstoff.Gesetz_l_kW";
        public const string FB_ABBRAND = "Festbrennstoff.Abbrandperiode_h";
        public const string SOLAR_FLACH = "Solar.Flach_l_m2";
        public const string SOLAR_ROEHRE = "Solar.Roehre_l_m2";
        public const string FW_OBEN = "Frischwasser.T_Oben_C";
        public const string FW_RUECKLAUF = "Frischwasser.T_Ruecklauf_C";
        public const string FW_ZUSCHLAG = "Frischwasser.Zuschlag";
        public const string ZIRK_ANTEIL = "Zirkulation.Anteil";
        public const string ZIRK_W_JE_WE = "Zirkulation.W_je_WE";
        public const string BHKW_VERSCHIEBEDAUER = "BHKW.Verschiebedauer_h";
        public const string BEREIT_RAUM = "Bereitschaft.Raumtemperatur_C";
        public const string BEREIT_DELTA = "Bereitschaft.Pruef_DeltaT_K";
        public const string BEREIT_EXTRAPOLATION = "Bereitschaft.Extrapolation_ab_l";
        public const string BW_MIN = "Brauchwasser.Bedarf_Min_l_P";
        public const string BW_MAX = "Brauchwasser.Bedarf_Max_l_P";
        public const string BW_UEBER = "Brauchwasser.Ueberdimensionierung_Faktor";
        public const string ZONEN = "Zonen.";                                   // + Vorgabe_Oben … Vorgabe_Unten
        public const string WP_MINDESTANTEIL = "WP.Mindestleistung_Anteil";
        public const string SCHWELLE_EIN = "Puffer.Schwelle_Ein";
        public const string SCHWELLE_AUS = "Puffer.Schwelle_Aus";
        public const string VORLAGE = "Vorlage.";                               // + <Typ>.<Glied>
        public const string AUFHEIZ_DAUER = "Aufheiz.Dauer_h";
        public const string AUFHEIZ_NUTZUNG = "Aufheiz.Nutzungsprofil.";       // + <Nutzungsprofil>, Schalter 0/1

        /// <summary>Die sieben Vorlagen-Typen in Anlegereihenfolge (Spalte <c>Vorlage</c>).</summary>
        public static readonly IReadOnlyList<string> VORLAGEN = new[]
        {
            "WP_MONO", "WP_BIVALENT", "BHKW", "KESSEL", "FESTBRENNSTOFF", "SOLAR", "PROZESS"
        };

        /// <summary>Die Kriterienschalter je Vorlage (0/1) in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<string> VORLAGE_SCHALTER = new[]
        {
            "K1", "K2", "K3", "K4", "D1", "D2", "K9", "K10", "KV"
        };

        /// <summary>Die Beispielwerte je Vorlage in Anlegereihenfolge.</summary>
        public static readonly IReadOnlyList<string> VORLAGE_WERTE = new[]
        {
            "Mindestlaufzeit_min", "Startziel_je_Tag", "Beispiel_Leistung_kW", "Faustwert_l_kW"
        };

        /// <summary>Der volle Schlüssel (mit Präfix).</summary>
        public static string Voll(string glied) => PRAEFIX + glied;

        /// <summary>Der volle Schlüssel eines Vorlagenglieds.</summary>
        public static string VorlageSchluessel(string typ, string glied) => PRAEFIX + VORLAGE + typ + "." + glied;

        private static PufferVorgabe V(string glied, double wert, string einheit, string quelle, string art)
            => new PufferVorgabe(PRAEFIX + glied, wert, einheit, quelle, art);

        /// <summary>Die Liste aller Vorgaben — Saat und Rückfall.</summary>
        public static readonly IReadOnlyList<PufferVorgabe> EINTRAEGE = Bauen();

        private static List<PufferVorgabe> Bauen()
        {
            string R = PufferHerkunftsart.RICHTLINIE, S = PufferHerkunftsart.STUDIE,
                   E = PufferHerkunftsart.SETZUNG, K = PufferHerkunftsart.SEKUNDAER;
            var l = new List<PufferVorgabe>
            {
                V(KONSTANTE, ProjektPuffer.WH_JE_LITER_KELVIN, "Wh/(l·K)", Q_KERN, E),
                V(VORPRUEFUNG, 3, "l/kW", Q_VDI_783, R),
                V(ANLAGENVOLUMEN + "FLAECHE_l_kW", 15, "l/kW", Q_BACOGA, K),
                V(ANLAGENVOLUMEN + "RADIATOR_l_kW", 10, "l/kW", Q_BACOGA, K),
                V(ANLAGENVOLUMEN + "KONVEKTOR_l_kW", 6, "l/kW", Q_BACOGA, K),
                V(FAUST_FIXED, 20, "l/kW", Q_VDI_784, R),
                V(FAUST_GEREGELT, 3, "l/kW", Q_VDI_784, R),
                V(ABTAU_RESERVE, 20, "l/kW", Q_WHITEPAPER, S),
                V(MINDESTLAUFZEIT, 10, "min", Q_VDZ, K),
                V(STILLSTAND + "15.RADIATOR_h", 1, "h", Q_VDI_T14, R),
                V(STILLSTAND + "15.FLAECHE_h", 1.5, "h", Q_VDI_T14, R),
                V(STILLSTAND + "12.RADIATOR_h", 2, "h", Q_VDI_T14, R),
                V(STILLSTAND + "12.FLAECHE_h", 2.5, "h", Q_VDI_T14, R),
                V(STILLSTAND + "10.RADIATOR_h", 3, "h", Q_VDI_T14, R),
                V(STILLSTAND + "10.FLAECHE_h", 3.5, "h", Q_VDI_T14, R),
                V(UEBERTEMPERATUR + "FLAECHE_K", 5, "K", Q_VDI_T15, R),
                V(UEBERTEMPERATUR + "RADIATOR_K", 15, "K", Q_VDI_T15, R),
                V(UEBERTEMPERATUR + "KONVEKTOR_K", 20, "K", Q_VDI_T15, R),
                V(UEBERTEMPERATUR + "LUEFTER_K", 15, "K", Q_VDI_T15, R),
                V(UEBERLADUNG, 5, "K", Q_VDI_88, R),
                V(RAUMTEMPERATUR_HEIZUNG, 20, "°C", Q_VDI_GL23, E),
                V(STARTZIEL, 6, "1/d", Q_WPQS, S),
                V(WARNSCHWELLE, 15, "1/d", Q_WPQS, S),
                V(HEIZPERIODE_MAX, 3000, "1/a", Q_SEKUNDAER_TAKT, K),
                V(DECKUNG_ZIEL, 1.0, "–", Q_TOOL, E),
                V(PRAXISGRENZE, 100000, "l", Q_TOOL, E),
                V(BAND + "FLAECHE.Min_l_kW", 10, "l/kW", Q_WHITEPAPER, S),
                V(BAND + "FLAECHE.Max_l_kW", 20, "l/kW", Q_WHITEPAPER, S),
                V(BAND + "RADIATOR.Min_l_kW", 25, "l/kW", Q_WHITEPAPER, S),
                V(BAND + "RADIATOR.Max_l_kW", 45, "l/kW", Q_WHITEPAPER, S),
                V(BAND + "WP.Min_l_kW", 12, "l/kW", Q_EN15450, R),
                V(BAND + "WP.Max_l_kW", 35, "l/kW", Q_EN15450, R),
                V(FB_SCHEITHOLZ, 55, "l/kW", Q_BEG, R),
                V(FB_PELLETS, 30, "l/kW", Q_BEG, R),
                V(FB_GESETZ, 20, "l/kW", Q_BIMSCHV, R),
                V(FB_ABBRAND, 4, "h", Q_EN303, R),
                V(SOLAR_FLACH, 50, "l/m²", Q_SOLAR, K),
                V(SOLAR_ROEHRE, 65, "l/m²", Q_SOLAR, K),
                V(FW_OBEN, 65, "°C", Q_RUNDE2, E),
                V(FW_RUECKLAUF, 25, "°C", Q_RUNDE2, E),
                V(FW_ZUSCHLAG, 0.15, "–", Q_VDI_ANH_I, R),
                V(ZIRK_ANTEIL, 0.35, "–", Q_ZIRK, K),
                V(ZIRK_W_JE_WE, 100, "W", Q_ECOSIZER, K),
                V(BHKW_VERSCHIEBEDAUER, 2, "h", Q_BHKW, K),
                V(BEREIT_RAUM, 15, "°C", Q_EN15332, R),
                V(BEREIT_DELTA, 45, "K", Q_EN15332, R),
                V(BEREIT_EXTRAPOLATION, 2000, "l", Q_812, R),
                V(BW_MIN, 28, "l", Q_IEA, S),
                V(BW_MAX, 50, "l", Q_IEA, S),
                V(BW_UEBER, 2.0, "–", Q_IEA, S),
                V(ZONEN + "Vorgabe_Oben", 0.10, "–", Q_EN15316, R),
                V(ZONEN + "Vorgabe_MitteOben", 0.16, "–", Q_EN15316, R),
                V(ZONEN + "Vorgabe_MitteUnten", 0.37, "–", Q_EN15316, R),
                V(ZONEN + "Vorgabe_Unten", 0.37, "–", Q_EN15316, R),
                V(WP_MINDESTANTEIL, 0.3, "–", Q_V39, E),
                V(SCHWELLE_EIN, 0.10, "–", Q_PUFFER, E),
                V(SCHWELLE_AUS, 0.95, "–", Q_PUFFER, E),
                // Aufheizkriterium K12 (Welle P4d): gesät mit Schritt 179, in Schritt 169 für frische Datenbanken.
                V(AUFHEIZ_DAUER, 2, "h", Q_V30, E),
                V(AUFHEIZ_NUTZUNG + nameof(PufferNutzungsprofil.WOHNEN), 0, "0/1", Q_V30, E),
                V(AUFHEIZ_NUTZUNG + nameof(PufferNutzungsprofil.BEHERBERGUNG), 0, "0/1", Q_V30, E),
                V(AUFHEIZ_NUTZUNG + nameof(PufferNutzungsprofil.PFLEGE), 0, "0/1", Q_V30, E),
                V(AUFHEIZ_NUTZUNG + nameof(PufferNutzungsprofil.BUERO_SCHULE), 1, "0/1", Q_V30, E),
                V(AUFHEIZ_NUTZUNG + nameof(PufferNutzungsprofil.GEWERBE), 0, "0/1", Q_V30, E),
            };

            // Vorlagen: Kriterienschalter K1 K2 K3 K4 D1 D2 K9 K10 KV, dann Mindestlaufzeit [min],
            // Startziel [1/d], Beispielleistung [kW] und Faustwert der Gegenprobe [l/kW bzw. l/m²].
            Vorlage(l, "WP_MONO",        new[] { 1, 1, 1, 1, 1, 1, 0, 0, 0 }, 10, 6, 20, 20);
            Vorlage(l, "WP_BIVALENT",    new[] { 1, 1, 1, 1, 1, 1, 0, 0, 0 }, 10, 6, 40, 20);
            Vorlage(l, "BHKW",           new[] { 0, 0, 1, 0, 1, 1, 0, 0, 1 }, 60, 2, 50, 60);
            Vorlage(l, "KESSEL",         new[] { 0, 0, 1, 0, 0, 1, 0, 0, 0 }, 10, 12, 100, 30);
            Vorlage(l, "FESTBRENNSTOFF", new[] { 0, 0, 0, 0, 0, 0, 1, 0, 0 }, 0, 2, 30, 55);
            Vorlage(l, "SOLAR",          new[] { 0, 0, 0, 0, 0, 0, 0, 1, 0 }, 0, 0, 0, 50);
            Vorlage(l, "PROZESS",        new[] { 0, 0, 1, 0, 1, 1, 0, 0, 0 }, 10, 6, 100, 20);
            return l;
        }

        private static void Vorlage(List<PufferVorgabe> l, string typ, int[] schalter,
                                    double mindestlaufzeit, double startziel, double leistung, double faustwert)
        {
            for (int i = 0; i < VORLAGE_SCHALTER.Count; i++)
                l.Add(V(VORLAGE + typ + "." + VORLAGE_SCHALTER[i], schalter[i], "0/1", Q_VORLAGE, PufferHerkunftsart.SETZUNG));
            double[] werte = { mindestlaufzeit, startziel, leistung, faustwert };
            string[] einheiten = { "min", "1/d", "kW", typ == "SOLAR" ? "l/m²" : "l/kW" };
            for (int i = 0; i < VORLAGE_WERTE.Count; i++)
                l.Add(V(VORLAGE + typ + "." + VORLAGE_WERTE[i], werte[i], einheiten[i], Q_VORLAGE, PufferHerkunftsart.SETZUNG));
        }

        /// <summary>Die Vorgaben als Wörterbuch Schlüssel → Wert (der Rückfall).</summary>
        public static IReadOnlyDictionary<string, double> AlsWoerterbuch()
        {
            var d = new Dictionary<string, double>(System.StringComparer.Ordinal);
            foreach (PufferVorgabe v in EINTRAEGE) d[v.Schluessel] = v.Wert;
            return d;
        }

        /// <summary>Die Zahl der Saatzeilen (Nachweis und Test).</summary>
        public static int Anzahl => EINTRAEGE.Count;

        /// <summary>Eine Zahl als Text, kulturunabhängig (Bericht).</summary>
        internal static string Text(double w) => w.ToString("0.###", CultureInfo.InvariantCulture);
    }
}
