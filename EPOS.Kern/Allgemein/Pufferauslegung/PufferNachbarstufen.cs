using System;
using System.Collections.Generic;
using System.Linq;
using static WindowsFormsApplication1.Textbaustein;

namespace WindowsFormsApplication1
{
    // ====================================================================================
    // NUTZEN-AUFWAND-ZEILE DER PUFFERSPEICHER-AUSLEGUNG (Konzept 6, V47, E-P32): die Empfehlung
    // und je zwei Stufen der Nenninhaltsliste darunter und darueber, je Stufe der Durchlauf (D1)
    // und die Zweipunktsimulation (D2) mit festem Volumen - ohne Bisektion -, dazu der
    // Bereitschaftsverlust (K11) und ein qualitativer Hinweis zur Jahresarbeitszahl. Dazu die
    // Speicher-gegen-Leistung-Kurve der Brauchwasserzone nach dem Ecosizer-Weg.
    // Rein funktional: kein Zugriff auf Datenbank, Dienste oder Uhr.
    // ====================================================================================

    /// <summary>Eine Stufe der Nutzen-Aufwand-Zeile.</summary>
    public sealed record PufferNachbarstufe
    {
        /// <summary>Abstand zur Empfehlung in Rasterstufen (−2 … +2; 0 = Empfehlung).</summary>
        public int Abstand { get; init; }
        /// <summary>Nenninhalt der Stufe [l].</summary>
        public double VolumenL { get; init; }
        /// <summary>Ist die Stufe die Empfehlung?</summary>
        public bool Empfehlung => Abstand == 0;
        /// <summary>Mehrvolumen gegenüber der Empfehlung [l] (negativ = kleiner).</summary>
        public double MehrvolumenL { get; init; }
        /// <summary>Deckungsgrad des Durchlaufs (D1); <c>null</c> = keine Reihe.</summary>
        public double? Deckungsgrad { get; init; }
        /// <summary>Starts je Tag der Heizperiode (D2); <c>null</c> = keine Reihe.</summary>
        public double? StartsJeTag { get; init; }
        /// <summary>Starts je Heizperiode (D2); <c>null</c> = keine Reihe.</summary>
        public int? StartsHeizperiode { get; init; }
        /// <summary>Bereitschaftsverlust der Stufe (K11, einheitlich Klasse-C-Grenze für den Vergleich).</summary>
        public PufferVerlust Verlust { get; init; }
        /// <summary>Qualitativer Hinweis zur Wirkung auf Jahresarbeitszahl bzw. Nutzungsgrad — keine Zahl.</summary>
        public Textbaustein JazHinweis { get; init; } = Textbaustein.Leer;
    }

    /// <summary>Ein Punkt der Speicher-gegen-Leistung-Kurve (V47).</summary>
    public sealed record PufferLeistungspunkt
    {
        /// <summary>Erzeugerleistung [kW].</summary>
        public double LeistungKw { get; init; }
        /// <summary>Anteil an der Ladeleistung des Zapfprofils (1 = Ladeleistung).</summary>
        public double Anteil { get; init; }
        /// <summary>Nötiges Speichervolumen [l] = Laufvolumen ÷ η_s.</summary>
        public double VolumenL { get; init; }
        /// <summary>Laufvolumen des Spitzenereignisses als Wärme [kWh].</summary>
        public double LaufvolumenKwh { get; init; }
        /// <summary>Tägliche Verdichterlaufzeit [h] = Tagesbedarf ÷ Leistung; <c>null</c> = Tagesbedarf unbekannt.</summary>
        public double? LaufzeitH { get; init; }
        /// <summary>Liegt die Laufzeit im Band 16–20 h des Ecosizer-Wegs?</summary>
        public bool ImLaufzeitband { get; init; }
    }

    /// <summary>Die Nutzen-Aufwand-Zeile samt Kurve.</summary>
    public sealed record PufferNachbarstufen
    {
        /// <summary>Die Stufen aufsteigend nach Volumen; leer = keine Empfehlung.</summary>
        public IReadOnlyList<PufferNachbarstufe> Stufen { get; init; } = Array.Empty<PufferNachbarstufe>();
        /// <summary>Die Zone, deren Reihe die Simulation trug; <c>null</c> = keine Reihe (nur Volumen und Verlust).</summary>
        public PufferZone? Simulationszone { get; init; }
        /// <summary>Die Speicher-gegen-Leistung-Kurve, fallend nach Leistung aufsteigend; leer = keine Kurve.</summary>
        public IReadOnlyList<PufferLeistungspunkt> Kurve { get; init; } = Array.Empty<PufferLeistungspunkt>();
        /// <summary>Warum es keine Kurve gibt bzw. wie sie entstand.</summary>
        public Textbaustein KurveHinweis { get; init; } = Textbaustein.Leer;
        /// <summary>Herkunft der Zeile.</summary>
        public Textbaustein HerkunftBaustein { get; init; } = Textbaustein.Leer;
    }

    public static partial class PufferAuslegung
    {
        /// <summary>Die Leistungsstufen der Kurve als Anteil der Ladeleistung (60 % bis 140 %).</summary>
        public static readonly IReadOnlyList<double> KURVE_ANTEILE = new[] { 0.6, 0.8, 1.0, 1.2, 1.4 };

        /// <summary>Das Laufzeitband des Ecosizer-Wegs [h].</summary>
        public const double LAUFZEIT_MIN_H = 16.0, LAUFZEIT_MAX_H = 20.0;

        public static readonly Textbaustein HERKUNFT_NACHBARSTUFEN =
            T("PAUS_HERK_NACHBARSTUFEN", "Konzept Pufferauslegung 6 (Nutzen-Aufwand-Zeile); Durchlauf D1 und Zweipunkt D2 mit festem Volumen, Verlust K11 nach Klasse-C-Grenze");
        public static readonly Textbaustein HERKUNFT_KURVE =
            T("PAUS_HERK_KURVE", "Ecosizer (Ecotope 2020), Speicher gegen Leistung: Laufvolumen des Spitzenereignisses ÷ η_s");

        /// <summary>
        /// Die Nutzen-Aufwand-Zeile: die Empfehlung und je <paramref name="anzahl"/> Stufen der Nenninhaltsliste
        /// darunter und darüber (über dem Listenende im Raster). Jede Stufe rechnet D1 und D2 der Heizzone (sonst
        /// der Prozesszone) mit festem Volumen — die übrigen Zonen behalten ihr Volumen, das Mehr oder Weniger
        /// trägt die simulierte Zone. Dazu die Speicher-gegen-Leistung-Kurve der Brauchwasserzone.
        /// </summary>
        public static PufferNachbarstufen Nachbarstufen(PufferAuslegungEingang eingang, PufferAuslegungErgebnis ergebnis, int anzahl = 2)
        {
            if (eingang == null) throw new ArgumentNullException(nameof(eingang));
            if (ergebnis == null) throw new ArgumentNullException(nameof(ergebnis));
            PufferRechengroessen g = PufferRechengroessen.Aus(eingang);
            PufferAuslegungParameter p = g.P;
            var (kurve, hinweis) = Leistungskurve(eingang, g);
            var leer = new PufferNachbarstufen { Kurve = kurve, KurveHinweis = hinweis, HerkunftBaustein = HERKUNFT_NACHBARSTUFEN };
            if (!(ergebnis.EmpfehlungL > 0)) return leer;

            List<double> volumina = Rasterstufen(ergebnis.EmpfehlungL, eingang.Nenninhalte ?? NENNINHALTE_VORGABE,
                                                 eingang.NenninhaltRasterL ?? RASTER_VORGABE_L, Math.Max(anzahl, 0));

            // ---- Die simulierte Zone: Heizung vor Prozess, nur mit Reihe ----
            PufferErzeuger z = eingang.Erzeuger ?? new PufferErzeuger();
            PufferZone? simZone = null;
            IReadOnlyList<double> reihe = null;
            PufferRechengroessen gs = g;
            if (eingang.KlasseHeizung && HeizzoneRechner.HatReihe(eingang.ReiheHeizung) && g.DeltaT > 0)
            {
                simZone = PufferZone.Heizung;
                reihe = eingang.ReiheHeizung;
            }
            else if (eingang.KlasseProzess && HeizzoneRechner.HatReihe(eingang.ReiheProzess))
            {
                simZone = PufferZone.Prozess;
                reihe = eingang.ReiheProzess;
                if (eingang.ProzessVorlaufC.HasValue && eingang.ProzessRuecklaufC.HasValue &&
                    eingang.ProzessVorlaufC.Value > eingang.ProzessRuecklaufC.Value)
                    gs = g.MitSpreizung(eingang.ProzessVorlaufC.Value - eingang.ProzessRuecklaufC.Value);
                if (!(gs.DeltaT > 0)) { simZone = null; reihe = null; }
            }
            double andere = simZone.HasValue
                ? ergebnis.Zonen.Where(x => x.Zone != simZone.Value).Sum(x => x.VolumenL)
                : 0;
            double[] pVerf = null;
            if (reihe != null)
            {
                double[] maske = Betriebssimulation.Verfuegbarkeit(reihe.Count, eingang.Sperrfenster);
                bool frei = z.ZweiterzeugerFrei && !z.Heizstab;
                pVerf = Betriebssimulation.Erzeugerleistung(reihe.Count, z.NennleistungKw, z.ZweiterzeugerKw, frei, maske);
            }

            bool wp = z.IstWaermepumpe || eingang.Vorlage == PufferVorlage.WP_MONO || eingang.Vorlage == PufferVorlage.WP_BIVALENT;
            int iEmpf = volumina.IndexOf(ergebnis.EmpfehlungL);
            var stufen = new List<PufferNachbarstufe>();
            for (int i = 0; i < volumina.Count; i++)
            {
                double v = volumina[i];
                int abstand = i - iEmpf;
                double? deckung = null, jeTag = null;
                int? heizperiode = null;
                if (reihe != null)
                {
                    double vZone = Math.Max(v - andere, 0);
                    // Ein frischer Satz Rechengrößen: Warnungen der Stufen gehören nicht in die Auslegung.
                    PufferRechengroessen gl = gs.MitSpreizung(gs.DeltaT);
                    gl.Warnungen = new List<PufferWarnung>();
                    DurchlaufErgebnis d1 = Betriebssimulation.Durchlauf(reihe, pVerf, vZone * gl.C * gl.DeltaT * gl.Eta / 1000.0);
                    PufferBetriebsbild bild = HeizzoneRechner.Betriebsbild(gl, reihe, z, vZone, simZone.Value);
                    deckung = d1.Deckungsgrad;
                    jeTag = bild.StartsJeTag;
                    heizperiode = bild.StartsHeizperiode;
                }
                stufen.Add(new PufferNachbarstufe
                {
                    Abstand = abstand,
                    VolumenL = v,
                    MehrvolumenL = v - ergebnis.EmpfehlungL,
                    Deckungsgrad = deckung,
                    StartsJeTag = jeTag,
                    StartsHeizperiode = heizperiode,
                    Verlust = HeizzoneRechner.Bereitschaftsverlust(v, null, eingang.VorlaufC, eingang.RuecklaufC, p),
                    JazHinweis = JazHinweis(abstand, wp)
                });
            }
            return leer with { Stufen = stufen, Simulationszone = simZone };
        }

        /// <summary>
        /// Die Volumina der Zeile aufsteigend: bis zu <paramref name="anzahl"/> Nenninhalte unter der Empfehlung,
        /// die Empfehlung, bis zu <paramref name="anzahl"/> darüber (über dem Listenende im Raster weiter).
        /// </summary>
        public static List<double> Rasterstufen(double empfehlungL, IReadOnlyList<double> liste, double rasterL, int anzahl)
        {
            var sortiert = (liste ?? NENNINHALTE_VORGABE).Where(w => w > 0).Distinct().OrderBy(w => w).ToList();
            var unten = sortiert.Where(w => w < empfehlungL).ToList();
            var l = unten.Skip(Math.Max(unten.Count - anzahl, 0)).ToList();
            l.Add(empfehlungL);
            var oben = sortiert.Where(w => w > empfehlungL).Take(anzahl).ToList();
            double r = rasterL > 0 ? rasterL : RASTER_VORGABE_L;
            double letzte = oben.Count > 0 ? oben[oben.Count - 1] : empfehlungL;
            while (oben.Count < anzahl)
            {
                letzte = Math.Floor(letzte / r + 1e-9) * r + r;
                oben.Add(letzte);
            }
            l.AddRange(oben);
            return l;
        }

        /// <summary>Der qualitative Hinweis je Stufe — die Recherche nennt die Richtung, keine Formel.</summary>
        internal static Textbaustein JazHinweis(int abstand, bool waermepumpe)
        {
            if (abstand == 0) return T("PAUS_JAZ_EMPFEHLUNG", "Empfehlung");
            if (abstand > 0)
                return waermepumpe
                    ? T("PAUS_JAZ_GROESSER", "größerer Speicher: mehr Bereitschaftsverlust und höhere mittlere Puffertemperatur, JAZ sinkt")
                    : T("PAUS_JAZ_GROESSER_OHNE_WP", "größerer Speicher: mehr Bereitschaftsverlust, weniger Starts");
            return waermepumpe
                ? T("PAUS_JAZ_KLEINER", "kleinerer Speicher: weniger Bereitschaftsverlust, aber mehr Starts — häufiges Takten belastet Verdichter und JAZ")
                : T("PAUS_JAZ_KLEINER_OHNE_WP", "kleinerer Speicher: weniger Bereitschaftsverlust, mehr Starts");
        }

        /// <summary>
        /// Das Laufvolumen [kWh] einer Zapfreihe bei der Erzeugerleistung: das größte Integral aus Zapfung minus
        /// Erzeugung ab Beginn einer Spitze (Lindley-Rekursion, Start leer).
        /// </summary>
        public static double Laufvolumen(IReadOnlyList<double> zapfung, double leistungKw)
        {
            double s = 0, m = 0;
            if (zapfung == null) return 0;
            foreach (double q in zapfung)
            {
                s = Math.Max(s + q - leistungKw, 0);
                if (s > m) m = s;
            }
            return m;
        }

        /// <summary>
        /// V47: die Speicher-gegen-Leistung-Kurve der Brauchwasserzone — für 60 % bis 140 % der Ladeleistung des
        /// Zapfprofils das Laufvolumen des Spitzenereignisses (an der Ladeleistung = D_max des Zapfprofils, die
        /// Zapfreihe trägt die Änderung mit der Leistung) und das Volumen = Laufvolumen ÷ (c · ΔT_B · η_s).
        /// Ohne Ladeleistung, D_max oder Zapfreihe: keine Kurve mit Hinweis.
        /// </summary>
        internal static (IReadOnlyList<PufferLeistungspunkt>, Textbaustein) Leistungskurve(PufferAuslegungEingang e, PufferRechengroessen g)
        {
            PufferZapfprofil zp = e.Zapfprofil;
            if (!e.KlasseBrauchwasser || zp == null)
                return (Array.Empty<PufferLeistungspunkt>(), T("PAUS_KURVE_KEIN_ZAPFPROFIL", "keine Kurve: kein Zapfprofil-Ergebnis"));
            if (!(zp.LadeleistungKw > 0) || !(zp.DmaxKwh > 0))
                return (Array.Empty<PufferLeistungspunkt>(), T("PAUS_KURVE_OHNE_LADELEISTUNG", "keine Kurve: Das Zapfprofil liefert keine Ladeleistung oder kein D_max"));
            if (!HeizzoneRechner.HatReihe(e.ReiheBrauchwasser))
                return (Array.Empty<PufferLeistungspunkt>(), T("PAUS_KURVE_OHNE_REIHE", "keine Kurve: Die Zapfreihe ist leer"));
            double dTB = zp.Topologie == PufferBwTopologie.Speicher
                ? BrauchwasserzoneRechner.ZAPF_C - BrauchwasserzoneRechner.KALT_C
                : e.DeltaTBK ?? ((e.TPufferObenC ?? g.P.Wert(PufferAuslegungVorgaben.FW_OBEN)) - g.P.Wert(PufferAuslegungVorgaben.FW_RUECKLAUF));
            if (!(dTB > 0))
                return (Array.Empty<PufferLeistungspunkt>(), T("PAUS_KURVE_OHNE_REIHE", "keine Kurve: Die Zapfreihe ist leer"));
            double pLade = zp.LadeleistungKw.Value;
            double basis = Laufvolumen(e.ReiheBrauchwasser, pLade);
            double? tag = BrauchwasserzoneRechner.TagesbedarfKwh(zp, g.C);
            var l = new List<PufferLeistungspunkt>();
            foreach (double a in KURVE_ANTEILE)
            {
                double pk = pLade * a;
                double lauf = Math.Max(zp.DmaxKwh + Laufvolumen(e.ReiheBrauchwasser, pk) - basis, 0);
                double? h = tag.HasValue && pk > 0 ? tag.Value / pk : (double?)null;
                l.Add(new PufferLeistungspunkt
                {
                    LeistungKw = pk,
                    Anteil = a,
                    LaufvolumenKwh = lauf,
                    VolumenL = lauf * 1000.0 / (g.C * dTB * g.Eta),
                    LaufzeitH = h,
                    ImLaufzeitband = h.HasValue && h.Value >= LAUFZEIT_MIN_H - 1e-9 && h.Value <= LAUFZEIT_MAX_H + 1e-9
                });
            }
            return (l, T("PAUS_KURVE_HINWEIS", "Laufvolumen bei {0} kW = D_max {1} kWh; die Zapfreihe trägt die Änderung mit der Leistung; Volumen bei ΔT_B {2} K und η_s {3}",
                         pLade, zp.DmaxKwh, dTB, g.Eta));
        }
    }
}
