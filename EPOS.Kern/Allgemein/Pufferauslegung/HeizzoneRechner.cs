using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using static WindowsFormsApplication1.Textbaustein;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // DIE HEIZZONE DER PUFFERSPEICHER-AUSLEGUNG (Konzept 3.3): K1 Vorpruefung, K2 Faustwert,
    // K3 Mindestlaufzeit (VDI 4645 Gl. 22), K4 Sperrzeit nach VDI 4645 Gl. 23 mit Tabellen 14/15
    // (Standardweg), K4e Sperrzeit aus dem Lastgang (Expertenweg, Tool), D1 Deckung und D2
    // Taktziel (Betriebssimulation), KV Verschiebedauer des BHKW, K8 Gegenprobe gegen das Band,
    // K9 Festbrennstoff (Faustwert und DIN EN 303-5), K10 Solar, K11 Bereitschaftsverlust.
    // Normen nur zitiert. Konstante 1,16 Wh/(l*K) aus ProjektPuffer.WH_JE_LITER_KELVIN bzw. der
    // Vorgabe Pufferauslegung.Konstante.Wh_je_l_K.
    // ====================================================================================

    /// <summary>Die Größen, die alle Zonen teilen (Konzept 3.1).</summary>
    internal sealed class PufferRechengroessen
    {
        public PufferAuslegungEingang E;
        public PufferAuslegungParameter P;
        public string Typ;
        /// <summary>c [Wh/(l·K)].</summary>
        public double C;
        public double SEin, SAus;
        /// <summary>η_s = s_aus − s_ein.</summary>
        public double Eta;
        /// <summary>Δϑ = ϑ_VL − ϑ_RL [K].</summary>
        public double DeltaT;
        public double PraxisgrenzeL;
        public double Deckungsziel;
        public double Startziel;
        public double[] Maske;
        public List<PufferWarnung> Warnungen = new List<PufferWarnung>();

        public static PufferRechengroessen Aus(PufferAuslegungEingang e)
        {
            PufferAuslegungParameter p = e.Parameter ?? PufferAuslegungParameter.Vorgabe();
            var g = new PufferRechengroessen { E = e, P = p, Typ = e.Vorlage.ToString() };
            g.C = p.WertOder(PufferAuslegungVorgaben.KONSTANTE, ProjektPuffer.WH_JE_LITER_KELVIN);
            g.SEin = e.SchwelleEin ?? p.Wert(PufferAuslegungVorgaben.SCHWELLE_EIN);
            g.SAus = e.SchwelleAus ?? p.Wert(PufferAuslegungVorgaben.SCHWELLE_AUS);
            g.Eta = g.SAus - g.SEin;
            if (!(g.Eta > 0) || g.Eta > 1)
                throw new ArgumentException("Der nutzbare Anteil s_aus − s_ein muss in (0, 1] liegen (s_ein " +
                                            Zahl(g.SEin) + ", s_aus " + Zahl(g.SAus) + ").");
            g.DeltaT = e.VorlaufC - e.RuecklaufC;
            g.PraxisgrenzeL = p.Wert(PufferAuslegungVorgaben.PRAXISGRENZE);
            g.Deckungsziel = e.Deckungsziel ?? p.Wert(PufferAuslegungVorgaben.DECKUNG_ZIEL);
            double vorlageStart = p.VorlageWert(g.Typ, "Startziel_je_Tag", 0);
            g.Startziel = e.StartzielJeTag ?? (vorlageStart > 0 ? vorlageStart : p.Wert(PufferAuslegungVorgaben.STARTZIEL));
            return g;
        }

        /// <summary>Liter je kWh nutzbarer Energie: 1000 / (c · Δϑ · η_s).</summary>
        public double LiterJeKwhNutzbar => 1000.0 / (C * DeltaT * Eta);

        public void Warnung(string code, PufferStufe stufe, Textbaustein text, Textbaustein herkunft, PufferZone? zone)
        {
            foreach (PufferWarnung w in Warnungen)
                if (w.Code == code && w.Zone == zone) return;
            Warnungen.Add(new PufferWarnung(code, stufe, text, herkunft, zone));
        }

        private static readonly CultureInfo DE = CultureInfo.GetCultureInfo("de-DE");

        /// <summary>Eine Zahl für den Klartext (de-DE, höchstens drei Nachkommastellen) — unabhängig von der Kultur des Fadens.</summary>
        public static string Zahl(double w) => w.ToString("#,##0.###", DE);
    }

    /// <summary>Die Kriterien der Heizzone (Konzept 3.3) — jede Formel auch einzeln rufbar.</summary>
    public static class HeizzoneRechner
    {
        // ---- Herkunft (Zitate) ----
        public static readonly Textbaustein HERKUNFT_K1 = T("PAUS_HERK_K1", "VDI 4645 E 2026-03, 7.8.3");
        public static readonly Textbaustein HERKUNFT_K2 = T("PAUS_HERK_K2", "VDI 4645 E 2026-03, 7.8.4");
        public static readonly Textbaustein HERKUNFT_K3 = T("PAUS_HERK_K3", "VDI 4645 E 2026-03, Gleichung 22; Wärmespeicher-Tool C.6.2");
        public static readonly Textbaustein HERKUNFT_K4 = T("PAUS_HERK_K4", "VDI 4645 E 2026-03, Gleichung 23 mit Tabellen 14 und 15");
        public static readonly Textbaustein HERKUNFT_K4E = T("PAUS_HERK_K4E", "Wärmespeicher-Tool C.6.3 (rollierendes Lastmittel über die Sperrdauer)");
        public static readonly Textbaustein HERKUNFT_D1 = T("PAUS_HERK_D1", "Wärmespeicher-Tool, Durchlauf mit Bisektion auf das Deckungsziel");
        public static readonly Textbaustein HERKUNFT_D2 = T("PAUS_HERK_D2", "Wärmespeicher-Tool, Zweipunktsimulation mit den Schwellen des Puffers");
        public static readonly Textbaustein HERKUNFT_KV = T("PAUS_HERK_KV", "Fachportal KWK-Flexibilisierung (Sekundärquelle)");
        public static readonly Textbaustein HERKUNFT_K9 = T("PAUS_HERK_K9", "1. BImSchV § 5; BEG");
        public static readonly Textbaustein HERKUNFT_K9E = T("PAUS_HERK_K9E", "DIN EN 303-5");
        public static readonly Textbaustein HERKUNFT_K10 = T("PAUS_HERK_K10", "Planungshilfen Solarthermie (Sekundärquelle), Bezug DIN EN 12977");
        public static readonly Textbaustein HERKUNFT_BAND = T("PAUS_HERK_BAND", "VDI-Whitepaper Thermische Speicher in Wärmepumpensystemen; DIN EN 15450");
        public static readonly Textbaustein HERKUNFT_K11 = T("PAUS_HERK_K11", "Delegierte Verordnungen (EU) 812/2013 und 814/2013 (Klasse C); prEN 15316-5 Formel 3; DIN EN 15332");
        public static readonly Textbaustein HERKUNFT_ANHANG_F = T("PAUS_HERK_VDI_ANHANG_F", "VDI 4645 E 2026-03, Anhang F");
        public static readonly Textbaustein HERKUNFT_VDI_88 = T("PAUS_HERK_VDI_88", "VDI 4645 E 2026-03, 8.8");
        public static readonly Textbaustein HERKUNFT_WHITEPAPER = T("PAUS_HERK_WHITEPAPER", "VDI-Whitepaper Thermische Speicher in Wärmepumpensystemen");

        /// <summary>Faktor der DIN-EN-303-5-Formel [l/kWh] (Zitat der Formel, Konzept K9).</summary>
        public const double DIN_EN_303_5_FAKTOR = 15.0;
        /// <summary>Abzugsfaktor der DIN-EN-303-5-Formel.</summary>
        public const double DIN_EN_303_5_ABZUG = 0.3;

        private static string Z(double w) => PufferRechengroessen.Zahl(w);

        /// <summary>Der Gerätetyp im Rechenweg von K2 (leistungsgeregelt bzw. Fixed-Speed).</summary>
        internal static Textbaustein Geraetetyp(bool geregelt) =>
            geregelt ? T("PAUS_WEG_GEREGELT", "leistungsgeregelt") : T("PAUS_WEG_FIXED_SPEED", "Fixed-Speed");

        /// <summary>Die Brennstoffart im Rechenweg von K9.</summary>
        internal static Textbaustein Brennstofftext(PufferBrennstoff b)
        {
            switch (b)
            {
                case PufferBrennstoff.Scheitholz: return T("PAUS_WEG_BRENNSTOFF_SCHEITHOLZ", "Scheitholz");
                case PufferBrennstoff.Pellets: return T("PAUS_WEG_BRENNSTOFF_PELLETS", "Pellets");
                case PufferBrennstoff.Hackschnitzel: return T("PAUS_WEG_BRENNSTOFF_HACKSCHNITZEL", "Hackschnitzel");
                default: return T("PAUS_WEG_BRENNSTOFF_KEINER", "keiner");
            }
        }

        /// <summary>Die Kollektorart im Rechenweg von K10.</summary>
        internal static Textbaustein Kollektortext(PufferKollektorart a) =>
            a == PufferKollektorart.Roehre ? T("PAUS_WEG_KOLLEKTOR_ROEHRE", "Röhrenkollektor") : T("PAUS_WEG_KOLLEKTOR_FLACH", "Flachkollektor");

        // =================================================================
        //  Einzelformeln
        // =================================================================

        /// <summary>Volumen [l] aus nutzbarer Energie [kWh]: E · 1000 / (c · Δϑ · η_s).</summary>
        public static double Volumen(double energieKwh, double c, double deltaT, double eta) =>
            energieKwh * 1000.0 / (c * deltaT * eta);

        /// <summary>K2: V = v_spez · Q̇_WP (Fixed-Speed bzw. leistungsgeregelt).</summary>
        public static double K2Faustwert(double leistungKw, bool geregelt, PufferAuslegungParameter p) =>
            (geregelt ? p.Wert(PufferAuslegungVorgaben.FAUST_GEREGELT) : p.Wert(PufferAuslegungVorgaben.FAUST_FIXED)) *
            Math.Max(leistungKw, 0);

        /// <summary>K3 (VDI 4645 Gl. 22 mit η_s): V = Q̇ · t_min / (c · Δϑ · η_s).</summary>
        public static double K3Mindestlaufzeit(double leistungKw, double tMinMin, double c, double deltaT, double eta) =>
            Volumen(Math.Max(leistungKw, 0) * Math.Max(tMinMin, 0) / 60.0, c, deltaT, eta);

        /// <summary>Tabelle 14: akzeptable Stillstandzeit t_aus,max [h] nach Heizgrenze und Übergabeart (vorsichtig: die nächsthöhere Heizgrenze der Tabelle).</summary>
        public static double Stillstand(double heizgrenzeC, string uebergabe, PufferAuslegungParameter p)
        {
            string zeile = heizgrenzeC > 12 ? "15" : heizgrenzeC > 10 ? "12" : "10";
            string spalte = uebergabe == "FLAECHE" || uebergabe == null ? "FLAECHE" : "RADIATOR";
            return p.Wert(PufferAuslegungVorgaben.STILLSTAND + zeile + "." + spalte + "_h");
        }

        /// <summary>Tabelle 15: kleinste nutzbare Übertemperatur dT_Wü,min [K] der Übergabe.</summary>
        public static double Uebertemperatur(string uebergabe, PufferAuslegungParameter p)
        {
            string art = uebergabe == null ? "FLAECHE" : uebergabe;
            string s = PufferAuslegungVorgaben.UEBERTEMPERATUR + art + "_K";
            return p.Hat(s) ? p.Wert(s) : p.Wert(PufferAuslegungVorgaben.UEBERTEMPERATUR + "RADIATOR_K");
        }

        /// <summary>
        /// K4 (VDI 4645 Gl. 23): V = Q̇_Ausl · (t_sperr − t_aus,max) · 1000 / (c · ((ϑ_VL + dT_SP) − (ϑ_R + dT_Wü,min))) − V_Hz,
        /// nicht negativ; <c>null</c>, wenn der Nenner nicht positiv ist.
        /// </summary>
        public static double? K4Standard(double qAuslKw, double tSperrH, double tAusMaxH, double vorlaufC, double ueberladungK,
                                         double raumC, double uebertemperaturK, double anlagenvolumenL, double c)
        {
            double nenner = (vorlaufC + ueberladungK) - (raumC + uebertemperaturK);
            if (!(nenner > 0)) return null;
            double v = qAuslKw * Math.Max(tSperrH - tAusMaxH, 0) * 1000.0 / (c * nenner) - anlagenvolumenL;
            return Math.Max(v, 0);
        }

        /// <summary>K4e (Tool): V = Q̄_sperr · t_sperr · 1000 / (c · Δϑ · η_s).</summary>
        public static double K4Experte(double qMittelKw, double tSperrH, double c, double deltaT, double eta) =>
            Volumen(Math.Max(qMittelKw, 0) * Math.Max(tSperrH, 0), c, deltaT, eta);

        /// <summary>K9 Faustwert: Scheitholz 55, Pellets/Hackschnitzel 30 (BEG, ≥ gesetzlich 20), sonst gesetzlich 20 l/kW.</summary>
        public static double K9Faustwert(PufferBrennstoff b, double leistungKw, PufferAuslegungParameter p)
        {
            double gesetz = p.Wert(PufferAuslegungVorgaben.FB_GESETZ);
            double spez = b switch
            {
                PufferBrennstoff.Scheitholz => Math.Max(p.Wert(PufferAuslegungVorgaben.FB_SCHEITHOLZ), gesetz),
                PufferBrennstoff.Pellets or PufferBrennstoff.Hackschnitzel => Math.Max(p.Wert(PufferAuslegungVorgaben.FB_PELLETS), gesetz),
                _ => gesetz
            };
            return spez * Math.Max(leistungKw, 0);
        }

        /// <summary>K9 zweiter Weg (DIN EN 303-5): V = 15 · Q_K · T_B · (1 − 0,3 · Q_H / Q_K,min), nicht negativ.</summary>
        public static double K9Din303(double qKKw, double tBH, double qHKw, double qKMinKw)
        {
            if (!(qKMinKw > 0)) return 0;
            return Math.Max(DIN_EN_303_5_FAKTOR * qKKw * tBH * (1 - DIN_EN_303_5_ABZUG * qHKw / qKMinKw), 0);
        }

        /// <summary>K10: V = v_spez · Aperturfläche (Flach 50, Röhre 65 l/m²).</summary>
        public static double K10Solar(double flaecheM2, PufferKollektorart art, PufferAuslegungParameter p) =>
            Math.Max(flaecheM2, 0) * p.Wert(art == PufferKollektorart.Roehre ? PufferAuslegungVorgaben.SOLAR_ROEHRE : PufferAuslegungVorgaben.SOLAR_FLACH);

        /// <summary>Klasse-C-Grenze des Warmhalteverlusts [W]: S = 16,66 + 8,33 · V^0,4.</summary>
        public static double KlasseCGrenzeW(double volumenL) => 16.66 + 8.33 * Math.Pow(Math.Max(volumenL, 0), 0.4);

        /// <summary>
        /// K11: Bereitschaftsverlust des Volumens — Katalogsatz vor Klasse-C-Grenze; dazu W/K bei der
        /// Prüfspreizung und der Betriebsverlust mit ϑ_mittel = (ϑ_VL + ϑ_RL)/2 gegen die Raumtemperatur.
        /// </summary>
        public static PufferVerlust Bereitschaftsverlust(double volumenL, double? katalogKwhD, double vorlaufC, double ruecklaufC,
                                                         PufferAuslegungParameter p)
        {
            double pruef = p.Wert(PufferAuslegungVorgaben.BEREIT_DELTA);
            double raum = p.Wert(PufferAuslegungVorgaben.BEREIT_RAUM);
            double grenze = p.Wert(PufferAuslegungVorgaben.BEREIT_EXTRAPOLATION);
            bool katalog = katalogKwhD.HasValue && katalogKwhD.Value > 0;
            double qb = katalog ? katalogKwhD.Value : KlasseCGrenzeW(volumenL) * 24.0 / 1000.0;
            double faktor = ((vorlaufC + ruecklaufC) / 2.0 - raum) / pruef;
            return new PufferVerlust
            {
                VolumenL = volumenL,
                KwhJeTag = qb,
                WJeK = qb * 1000.0 / (24.0 * pruef),
                Betriebsfaktor = faktor,
                KwhJeJahr = qb * Math.Max(faktor, 0) * 365.0,
                AusKatalog = katalog,
                Extrapoliert = !katalog && volumenL > grenze,
                HerkunftBaustein = katalog ? T("PAUS_HERK_K11_KATALOG", "Katalogsatz (Prüfwert bei {0} K, DIN EN 15332)", pruef) : HERKUNFT_K11
            };
        }

        /// <summary>Das Plausibilitätsband nach Übergabeart [l] (FLAECHE bzw. RADIATOR/KONVEKTOR/LÜFTER) × Heizlast.</summary>
        public static (double Min, double Max) Band(string uebergabe, double qAuslKw, PufferAuslegungParameter p)
        {
            string art = uebergabe == null || uebergabe == "FLAECHE" ? "FLAECHE" : "RADIATOR";
            return (p.Wert(PufferAuslegungVorgaben.BAND + art + ".Min_l_kW") * qAuslKw,
                    p.Wert(PufferAuslegungVorgaben.BAND + art + ".Max_l_kW") * qAuslKw);
        }

        /// <summary>Das Plausibilitätsband nach Wärmepumpenleistung [l] (DIN EN 15450).</summary>
        public static (double Min, double Max) BandWp(double leistungKw, PufferAuslegungParameter p) =>
            (p.Wert(PufferAuslegungVorgaben.BAND + "WP.Min_l_kW") * leistungKw,
             p.Wert(PufferAuslegungVorgaben.BAND + "WP.Max_l_kW") * leistungKw);

        /// <summary>Die Auslegungsheizlast: Eingabe, sonst Maximum der Reihe.</summary>
        public static double Auslegungsheizlast(PufferAuslegungEingang e)
        {
            if (e.AuslegungsheizlastKw.HasValue) return e.AuslegungsheizlastKw.Value;
            double m = 0;
            if (e.ReiheHeizung != null)
                foreach (double w in e.ReiheHeizung) if (w > m) m = w;
            return m;
        }

        /// <summary>Das Anlagenvolumen: Eingabe, sonst Übergabeart × Q̇_Ausl (FLAECHE 15, RADIATOR 10, KONVEKTOR 6 l/kW).</summary>
        public static double Anlagenvolumen(PufferAuslegungEingang e, double qAuslKw, PufferAuslegungParameter p)
        {
            if (e.AnlagenvolumenL.HasValue) return e.AnlagenvolumenL.Value;
            string art = e.Uebergabeart ?? "FLAECHE";
            string s = PufferAuslegungVorgaben.ANLAGENVOLUMEN + art + "_l_kW";
            return (p.Hat(s) ? p.Wert(s) : 0) * qAuslKw;
        }

        internal static bool HatReihe(IReadOnlyList<double> r)
        {
            if (r == null || r.Count == 0) return false;
            foreach (double w in r) if (w > 0) return true;
            return false;
        }

        internal static double Mindestleistung(PufferErzeuger z, PufferAuslegungParameter p) =>
            z.MindestleistungKw ?? (z.IstWaermepumpe ? p.Wert(PufferAuslegungVorgaben.WP_MINDESTANTEIL) * z.NennleistungKw : 0);

        // =================================================================
        //  Die Zone
        // =================================================================

        /// <summary>Rechnet die Heizzone; Warnungen landen in <paramref name="g"/>.</summary>
        internal static PufferZonenergebnis Rechnen(PufferRechengroessen g, out double qAusl, out double vHz)
        {
            PufferAuslegungEingang e = g.E;
            PufferAuslegungParameter p = g.P;
            PufferErzeuger z = e.Erzeuger ?? new PufferErzeuger();
            const PufferZone ZONE = PufferZone.Heizung;
            var k = new List<PufferKriterium>();
            if (!(g.DeltaT > 0))
                throw new ArgumentException("Die Spreizung des Puffers ϑ_VL − ϑ_RL muss positiv sein (" +
                                            Z(e.VorlaufC) + " / " + Z(e.RuecklaufC) + " °C).");

            qAusl = Auslegungsheizlast(e);
            vHz = Anlagenvolumen(e, qAusl, p);
            bool reihe = HatReihe(e.ReiheHeizung);
            bool istWp = z.IstWaermepumpe || e.Vorlage == PufferVorlage.WP_MONO || e.Vorlage == PufferVorlage.WP_BIVALENT;
            double nenn = z.NennleistungKw;
            double pMin = Mindestleistung(z, p);
            string typ = g.Typ;

            if (e.Uebergabeart == null)
                g.Warnung(PufferWarncode.UEBERGABE_UNBEKANNT, PufferStufe.Hinweis,
                          Textbaustein.T("PA_UEBERGABE_UNBEKANNT_TEXT", "Keine Übergabeart am Gebäude: Die Heizzone rechnet wie Flächenheizung."), HERKUNFT_K4, ZONE);
            if (!reihe)
                g.Warnung(PufferWarncode.KEINE_REIHE, PufferStufe.Warnung,
                          Textbaustein.T("PA_KEINE_REIHE_TEXT", "Die Heizreihe ist leer: Lastgang-Kriterien (K4e, D1, D2) entfallen."), HERKUNFT_D1, ZONE);

            // ---- K1 Vorprüfung ----
            bool keinPuffer = false;
            if (p.VorlageAn(typ, "K1") && istWp)
            {
                double grenze = p.Wert(PufferAuslegungVorgaben.VORPRUEFUNG) * nenn;
                bool positiv = !e.Einzelraumregelung && vHz >= grenze;
                k.Add(new PufferKriterium
                {
                    Kennung = PufferKriteriumKennung.K1, Bezeichnung = "Vorprüfung Anlagenvolumen", VolumenL = null,
                    Aktiv = false, HerkunftBaustein = HERKUNFT_K1,
                    RechenwegBaustein = T("PAUS_WEG_K1", "V_Hz = {0} l {1} {2} l/kW · {3} kW = {4} l{5}",
                                          vHz, positiv ? "≥" : "<", p.Wert(PufferAuslegungVorgaben.VORPRUEFUNG), nenn, grenze,
                                          e.Einzelraumregelung
                                              ? T("PAUS_WEG_K1_EINZELRAUM", "; Einzelraumregelung: absperrbar, keine Gutschrift für K1")
                                              : Leer)
                });
                if (positiv && z.Geregelt)
                {
                    keinPuffer = true;
                    g.Warnung(PufferWarncode.KEIN_PUFFER, PufferStufe.Hinweis,
                              Textbaustein.T("PA_KEIN_PUFFER_TEXT", "Das nicht absperrbare Anlagenvolumen reicht: kein Heizungspuffer erforderlich."), HERKUNFT_K1, ZONE);
                }
                else if (positiv)
                    g.Warnung(PufferWarncode.OHNE_PUFFER_GEREGELT, PufferStufe.Warnung,
                              Textbaustein.T("PA_OHNE_PUFFER_GEREGELT_TEXT", "Ohne Puffer nur mit leistungsgeregelter Wärmepumpe: Das Gerät ist Fixed-Speed, der Puffer bleibt."),
                              HERKUNFT_ANHANG_F, ZONE);
            }

            // ---- K2 Faustwert ----
            if (p.VorlageAn(typ, "K2"))
            {
                double spez = z.Geregelt ? p.Wert(PufferAuslegungVorgaben.FAUST_GEREGELT) : p.Wert(PufferAuslegungVorgaben.FAUST_FIXED);
                k.Add(new PufferKriterium
                {
                    Kennung = PufferKriteriumKennung.K2, Bezeichnung = "Faustwert nach Gerätetyp",
                    VolumenL = K2Faustwert(nenn, z.Geregelt, p), Aktiv = true, HerkunftBaustein = HERKUNFT_K2,
                    RechenwegBaustein = T("PAUS_WEG_K2", "{0} l/kW ({1}) · {2} kW", spez, Geraetetyp(z.Geregelt), nenn)
                });
            }

            // ---- K3 Mindestlaufzeit ----
            if (p.VorlageAn(typ, "K3"))
            {
                double vorlage = p.VorlageWert(typ, "Mindestlaufzeit_min", 0);
                double tMin = e.MindestlaufzeitMin ?? (vorlage > 0 ? vorlage : p.Wert(PufferAuslegungVorgaben.MINDESTLAUFZEIT));
                double leistung = z.Geregelt ? pMin : nenn;
                k.Add(new PufferKriterium
                {
                    Kennung = PufferKriteriumKennung.K3, Bezeichnung = "Mindestlaufzeit",
                    VolumenL = K3Mindestlaufzeit(leistung, tMin, g.C, g.DeltaT, g.Eta), Aktiv = true,
                    EnthaeltNutzanteil = true, HerkunftBaustein = HERKUNFT_K3,
                    RechenwegBaustein = T("PAUS_WEG_K3", "{0} kW · {1} min / ({2} · {3} K · {4})", leistung, tMin, g.C, g.DeltaT, g.Eta)
                });
            }

            // ---- K4 / K4e Sperrzeit ----
            List<PufferSperrfenster> fenster = (e.Sperrfenster ?? Array.Empty<PufferSperrfenster>()).Where(f => f != null && f.DauerH > 0).ToList();
            if (p.VorlageAn(typ, "K4") && fenster.Count > 0)
            {
                bool entfaellt = z.ZweiterzeugerFrei && !z.Heizstab;
                if (entfaellt)
                    g.Warnung(PufferWarncode.ZWEITERZEUGER_FREI, PufferStufe.Hinweis,
                              Textbaustein.T("PA_ZWEITERZEUGER_FREI_TEXT", "Der Zweiterzeuger ist in der Sperre freigegeben: Die Sperrzeit bemisst den Puffer nicht."), HERKUNFT_VDI_88, ZONE);
                if (z.Heizstab)
                    g.Warnung(PufferWarncode.HEIZSTAB_GESPERRT, PufferStufe.Hinweis,
                              Textbaustein.T("PA_HEIZSTAB_GESPERRT_TEXT", "Der Heizstab gilt in der Sperre als mitgesperrt: Die Sperrzeit bemisst den Puffer."), HERKUNFT_WHITEPAPER, ZONE);

                double hg = e.HeizgrenzeC ?? SimulationSPK.HEIZGRENZE_VORGABE_C;
                double tAus = Stillstand(hg, e.Uebergabeart, p);
                double dTw = Uebertemperatur(e.Uebergabeart, p);
                double dSp = p.Wert(PufferAuslegungVorgaben.UEBERLADUNG);
                double raum = p.Wert(PufferAuslegungVorgaben.RAUMTEMPERATUR_HEIZUNG);
                double tSperr = fenster.Max(f => f.DauerH);
                double? v4 = K4Standard(qAusl, tSperr, tAus, e.VorlaufC, dSp, raum, dTw, vHz, g.C);
                k.Add(new PufferKriterium
                {
                    Kennung = PufferKriteriumKennung.K4, Bezeichnung = "Sperrzeit (Standardweg)",
                    VolumenL = v4, Aktiv = !entfaellt && !e.SperrzeitExpertenweg, Gueltig = v4.HasValue, HerkunftBaustein = HERKUNFT_K4,
                    RechenwegBaustein = T("PAUS_WEG_K4", "{0} kW · ({1} h − {2} h) · 1000 / ({3} · (({4} + {5}) − ({6} + {7}))) − {8} l{9}",
                                          qAusl, tSperr, tAus, g.C, e.VorlaufC, dSp, raum, dTw, vHz,
                                          v4.HasValue ? Leer : T("PAUS_WEG_K4_NENNER", "; Nenner nicht positiv"))
                });

                double qMittel = reihe ? fenster.Max(f => Betriebssimulation.MaxMittelleistung(e.ReiheHeizung, f.DauerH)) : nenn;
                double v4e = 0;
                foreach (PufferSperrfenster f in fenster)
                {
                    double q = reihe ? Betriebssimulation.MaxMittelleistung(e.ReiheHeizung, f.DauerH) : nenn;
                    v4e = Math.Max(v4e, K4Experte(q, f.DauerH, g.C, g.DeltaT, g.Eta));
                }
                k.Add(new PufferKriterium
                {
                    Kennung = PufferKriteriumKennung.K4E, Bezeichnung = "Sperrzeit (Expertenweg, Lastgang)",
                    VolumenL = v4e, Aktiv = !entfaellt && e.SperrzeitExpertenweg, EnthaeltNutzanteil = true, HerkunftBaustein = HERKUNFT_K4E,
                    RechenwegBaustein = T("PAUS_WEG_K4E", "{0} kW ({1}) · {2} h / ({3} · {4} K · {5})",
                                          qMittel,
                                          reihe ? T("PAUS_WEG_K4E_MITTEL", "größtes rollierendes Mittel")
                                                : T("PAUS_WEG_K4E_ERSATZ", "Ersatzwert Nennleistung"),
                                          tSperr, g.C, g.DeltaT, g.Eta)
                });
            }

            // ---- D1, D2 ----
            if (reihe)
                k.AddRange(Simulationskriterien(g, e.ReiheHeizung, z, p.VorlageAn(typ, "D1"), p.VorlageAn(typ, "D2"), ZONE));

            // ---- KV Verschiebedauer BHKW ----
            if (p.VorlageAn(typ, "KV"))
            {
                double t = e.BhkwVerschiebedauerH ?? p.Wert(PufferAuslegungVorgaben.BHKW_VERSCHIEBEDAUER);
                k.Add(new PufferKriterium
                {
                    Kennung = PufferKriteriumKennung.KV, Bezeichnung = "Verschiebedauer BHKW",
                    VolumenL = Volumen(nenn * t, g.C, g.DeltaT, g.Eta), Aktiv = t > 0, EnthaeltNutzanteil = true, HerkunftBaustein = HERKUNFT_KV,
                    RechenwegBaustein = T("PAUS_WEG_KV", "{0} kW · {1} h / ({2} · {3} K · {4})", nenn, t, g.C, g.DeltaT, g.Eta)
                });
            }

            // ---- K9 Festbrennstoff ----
            if (p.VorlageAn(typ, "K9"))
            {
                k.Add(new PufferKriterium
                {
                    Kennung = PufferKriteriumKennung.K9, Bezeichnung = "Festbrennstoff (Mindestvolumen)",
                    VolumenL = K9Faustwert(z.Brennstoff, nenn, p), Aktiv = true, HerkunftBaustein = HERKUNFT_K9,
                    RechenwegBaustein = T("PAUS_WEG_K9", "Brennstoff {0} · {1} kW", Brennstofftext(z.Brennstoff), nenn)
                });
                double tB = z.AbbrandperiodeH ?? p.Wert(PufferAuslegungVorgaben.FB_ABBRAND);
                bool daten = z.MindestleistungKw.HasValue && z.MindestleistungKw.Value > 0 && qAusl > 0;
                k.Add(new PufferKriterium
                {
                    Kennung = PufferKriteriumKennung.K9E, Bezeichnung = "Festbrennstoff (DIN EN 303-5)",
                    VolumenL = daten ? K9Din303(nenn, tB, qAusl, z.MindestleistungKw.Value) : (double?)null,
                    Aktiv = daten, Gueltig = daten, HerkunftBaustein = HERKUNFT_K9E,
                    RechenwegBaustein = daten
                        ? T("PAUS_WEG_K9E", "{0} · {1} kW · {2} h · (1 − {3} · {4} kW / {5} kW)",
                            DIN_EN_303_5_FAKTOR, nenn, tB, DIN_EN_303_5_ABZUG, qAusl, z.MindestleistungKw.Value)
                        : T("PAUS_WEG_K9E_FEHLT", "Mindestleistung des Kessels oder Heizlast fehlt")
                });
            }

            // ---- K10 Solar ----
            if (p.VorlageAn(typ, "K10"))
            {
                bool flaeche = z.KollektorflaecheM2 > 0;
                k.Add(new PufferKriterium
                {
                    Kennung = PufferKriteriumKennung.K10, Bezeichnung = "Solarthermie",
                    VolumenL = flaeche ? K10Solar(z.KollektorflaecheM2, z.Kollektorart, p) : (double?)null,
                    Aktiv = flaeche, Gueltig = flaeche, HerkunftBaustein = HERKUNFT_K10,
                    RechenwegBaustein = flaeche
                        ? T("PAUS_WEG_K10", "{0} m² ({1})", z.KollektorflaecheM2, Kollektortext(z.Kollektorart))
                        : T("PAUS_WEG_K10_KEINE", "keine Kollektorfläche")
                });
            }

            PufferZonenergebnis zone = Bemessen(ZONE, k, keinPuffer);

            // ---- K8 Gegenprobe gegen das Band ----
            if (!keinPuffer && qAusl > 0 && zone.VolumenL > 0)
            {
                (double min, double max) = Band(e.Uebergabeart, qAusl, p);
                if (zone.VolumenL < min)
                    g.Warnung(PufferWarncode.BAND_UNTER, PufferStufe.Hinweis,
                              Textbaustein.T("PA_BAND_UNTER_TEXT", "Das Volumen der Heizzone ({0} l) liegt unter dem Band {1}–{2} l.", (double)zone.VolumenL, (double)min, (double)max),
                              HERKUNFT_BAND, ZONE);
                else if (zone.VolumenL > max)
                    g.Warnung(PufferWarncode.BAND_UEBER, PufferStufe.Hinweis,
                              Textbaustein.T("PA_BAND_UEBER_TEXT", "Das Volumen der Heizzone ({0} l) liegt über dem Band {1}–{2} l.", (double)zone.VolumenL, (double)min, (double)max),
                              HERKUNFT_BAND, ZONE);
            }

            // ---- Abtau-Gegenprobe bei Trinkwasservorrang (V45) ----
            if (istWp && e.Trinkwasservorrang && !keinPuffer)
            {
                double reserve = p.Wert(PufferAuslegungVorgaben.ABTAU_RESERVE) * qAusl;
                if (zone.VolumenL < reserve)
                    g.Warnung(PufferWarncode.ABTAU_VORRANG, PufferStufe.Hinweis,
                              Textbaustein.T("PA_ABTAU_VORRANG_TEXT", "Trinkwasservorrang: Unter {0} l fehlt Abtaureserve für die Heizung.", (double)reserve),
                              PufferAuslegungVorgaben.Quellentext(p.Quelle(PufferAuslegungVorgaben.ABTAU_RESERVE)), ZONE);
            }

            // ---- Betriebsbild mit dem Zonenvolumen ----
            if (reihe && !keinPuffer && zone.VolumenL > 0)
                zone = zone with { Betriebsbild = Betriebsbild(g, e.ReiheHeizung, z, zone.VolumenL, ZONE) };
            return zone;
        }

        /// <summary>D1 und D2 auf einer Reihe (Heiz- und Prozesszone).</summary>
        internal static IEnumerable<PufferKriterium> Simulationskriterien(PufferRechengroessen g, IReadOnlyList<double> reihe,
                                                                          PufferErzeuger z, bool d1, bool d2, PufferZone zone)
        {
            int n = reihe.Count;
            double[] maske = Betriebssimulation.Verfuegbarkeit(n, g.E.Sperrfenster);
            bool frei = z.ZweiterzeugerFrei && !z.Heizstab;
            double[] pVerf = Betriebssimulation.Erzeugerleistung(n, z.NennleistungKw, z.ZweiterzeugerKw, frei, maske);
            var liste = new List<PufferKriterium>();
            if (d1)
            {
                var (v, ok, lauf) = Betriebssimulation.MindestvolumenDeckung(reihe, pVerf, g.Deckungsziel,
                                                                              g.C * g.DeltaT * g.Eta / 1000.0, g.PraxisgrenzeL);
                if (!ok)
                    g.Warnung(PufferWarncode.PRAXISGRENZE, PufferStufe.Warnung,
                              Textbaustein.T("PA_PRAXISGRENZE_DECKUNG_TEXT", "Das Deckungsziel {0} ist auch mit {1} l nicht erreichbar: Die Erzeugerleistung ist zu klein; die Deckung bemisst nicht.", (double)g.Deckungsziel, (double)g.PraxisgrenzeL), HERKUNFT_D1, zone);
                liste.Add(new PufferKriterium
                {
                    Kennung = PufferKriteriumKennung.D1, Bezeichnung = "Deckungsgrad (Durchlauf)", VolumenL = v,
                    Aktiv = true, Gueltig = ok, EnthaeltNutzanteil = true, HerkunftBaustein = HERKUNFT_D1,
                    RechenwegBaustein = T("PAUS_WEG_D1", "kleinstes Volumen mit Deckungsgrad ≥ {0} bei {1} kW; erreicht {2}",
                                          g.Deckungsziel, z.NennleistungKw + z.ZweiterzeugerKw, lauf.Deckungsgrad)
                });
            }
            if (d2)
            {
                double pMin = z.Geregelt ? Mindestleistung(z, g.P) : 0;
                var (v, ok, bild) = Betriebssimulation.MindestvolumenStarts(reihe, pVerf, g.SEin, g.SAus, pMin, z.Geregelt,
                                                                            g.C * g.DeltaT / 1000.0, g.Startziel, g.PraxisgrenzeL);
                if (!ok)
                    g.Warnung(PufferWarncode.PRAXISGRENZE, PufferStufe.Warnung,
                              Textbaustein.T("PA_PRAXISGRENZE_START_TEXT", "Das Startziel {0} je Tag ist auch mit {1} l nicht zu halten; das Taktziel bemisst nicht.", (double)g.Startziel, (double)g.PraxisgrenzeL), HERKUNFT_D2, zone);
                liste.Add(new PufferKriterium
                {
                    Kennung = PufferKriteriumKennung.D2, Bezeichnung = "Taktziel (Zweipunkt)", VolumenL = v,
                    Aktiv = true, Gueltig = ok, EnthaeltNutzanteil = true, HerkunftBaustein = HERKUNFT_D2,
                    RechenwegBaustein = T("PAUS_WEG_D2", "kleinstes Volumen mit ≤ {0} Starts je Tag ({1}, Schwellen {2}/{3}); erreicht {4} je Tag",
                                          g.Startziel,
                                          z.Geregelt ? T("PAUS_WEG_D2_MODULATION", "Modulationsmodus") : T("PAUS_WEG_D2_EINAUS", "Ein/Aus"),
                                          g.SEin, g.SAus, bild.StartsJeTag)
                });
            }
            return liste;
        }

        /// <summary>Das Betriebsbild mit dem Zonenvolumen samt Warnungen zu Starts (Konzept 3.3 D2, V42).</summary>
        internal static PufferBetriebsbild Betriebsbild(PufferRechengroessen g, IReadOnlyList<double> reihe, PufferErzeuger z,
                                                        double volumenL, PufferZone zone)
        {
            int n = reihe.Count;
            double[] maske = Betriebssimulation.Verfuegbarkeit(n, g.E.Sperrfenster);
            bool frei = z.ZweiterzeugerFrei && !z.Heizstab;
            double[] pVerf = Betriebssimulation.Erzeugerleistung(n, z.NennleistungKw, z.ZweiterzeugerKw, frei, maske);
            double pMin = z.Geregelt ? Mindestleistung(z, g.P) : 0;
            double kap = volumenL * g.C * g.DeltaT / 1000.0;
            ZweipunktErgebnis sim = Betriebssimulation.Zweipunkt(reihe, pVerf, kap, g.SEin, g.SAus, pMin, z.Geregelt, g.SEin * kap);
            PufferBetriebsbild b = Betriebssimulation.Bild(sim, reihe, volumenL);
            double warn = g.P.Wert(PufferAuslegungVorgaben.WARNSCHWELLE);
            double jahr = g.P.Wert(PufferAuslegungVorgaben.HEIZPERIODE_MAX);
            if (b.StartsJeTag > warn)
                g.Warnung(PufferWarncode.STARTS_TAG, PufferStufe.Warnung,
                          Textbaustein.T("PA_STARTS_TAG_TEXT", "{0} Starts je Tag über der Warnschwelle {1}.", (double)b.StartsJeTag, (double)warn),
                          PufferAuslegungVorgaben.Quellentext(g.P.Quelle(PufferAuslegungVorgaben.WARNSCHWELLE)), zone);
            if (b.StartsHeizperiode > jahr)
                g.Warnung(PufferWarncode.STARTS_JAHR, PufferStufe.Hinweis,
                          Textbaustein.T("PA_STARTS_JAHR_TEXT", "{0} Starts je Heizperiode über {1}.", (double)b.StartsHeizperiode, (double)jahr),
                          PufferAuslegungVorgaben.Quellentext(g.P.Quelle(PufferAuslegungVorgaben.HEIZPERIODE_MAX)), zone);
            return b;
        }

        /// <summary>Bemessend = Maximum der aktiven, gültigen Kriterien (Konzept 3.4); K1 positiv → 0.</summary>
        internal static PufferZonenergebnis Bemessen(PufferZone zone, List<PufferKriterium> k, bool keinPuffer)
        {
            string bem = null;
            double v = 0;
            if (!keinPuffer)
                foreach (PufferKriterium x in k)
                    if (x.Aktiv && x.Gueltig && x.VolumenL.HasValue && x.VolumenL.Value > v)
                    {
                        v = x.VolumenL.Value;
                        bem = x.Kennung;
                    }
            return new PufferZonenergebnis { Zone = zone, Kriterien = k, Bemessend = bem, VolumenL = v, KeinPuffer = keinPuffer };
        }
    }
}
