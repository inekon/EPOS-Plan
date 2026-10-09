#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using EPOS.UI.Dialoge.Waermepumpe;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Die Herleitungszeile „Übergabe und Bivalenz" und die Schnellwahl des Höchstvorlaufs</b> (Übergabegrenze
    /// UB‑E1; Umsetzungskonzept 5.1, 6.2–6.4): füllt aus <see cref="BivalenzQuelle"/> (Gebäude, Kessel, Kennfeld) über
    /// <see cref="Bivalenzherleitung"/> die Lesewerte in <see cref="WaermepumpeAnlageDaten"/>. Muster
    /// <see cref="BetriebszeitenAbbildung"/>; plattformfrei, die Windows-Hülle der Wärmepumpe und der Lese- und
    /// Schreibweg der Simulationskonfiguration rufen sie. Keine Rechenwirkung, kein Schema.
    ///
    /// <para><b>Einbindung und Vorwärmbetrieb (UB‑E2).</b> Beide reisen mit der Anlagenzeile (<see cref="AnlageLesen"/>,
    /// <see cref="AnlageSchreiben"/>, schmaler Schreibweg <see cref="MitUebergabe"/>). Leer heißt „nicht gewählt": Das
    /// Kennzeichen steht dann auf <see cref="BivalenzKennzeichen.NichtWirksam"/>, die Zeile nennt „Einbindung nicht
    /// gesetzt — Übergabegrenze ruht" (U‑1). Eine neue Anlage wird vorbelegt (<see cref="Vorbelegen"/>).</para>
    ///
    /// <para><b>Kältemittel am Gerät.</b> Die Klappliste zeigt das gespeicherte <c>Kaeltemittel</c> der Projektkopie;
    /// <see cref="GeraetSchreiben"/> schreibt die Wahl zurück. Lesewerte und weiche Sperren rechnet
    /// <see cref="Bivalenzpruefung"/>.</para>
    /// </summary>
    public static class BivalenzAbbildung
    {
        /// <summary>Die Klappliste der Kältemittel aus <see cref="Bivalenzvorgaben"/>, in der Reihenfolge des Kerns.</summary>
        public static IReadOnlyList<KaeltemittelEintrag> Kaeltemittelliste()
            => Bivalenzvorgaben.Kaeltemittelcodes.Select(code =>
            {
                Kaeltemittelvorgabe v = Bivalenzvorgaben.Vorgabe(code);
                return new KaeltemittelEintrag(code, v.HoechstvorlaufC, v.RuecklaufGrenzeC, v.BezugsruecklaufC);
            }).ToList();

        /// <summary>
        /// Setzt Klappliste und Rechenweg der Herleitung in den Feldsatz. Die Datenbank wird erst beim ersten Zeichnen
        /// gelesen und dann je Dialog gehalten; das Kennfeld je Höchstvorlauf einmal. Ein Lese- oder Rechenfehler
        /// bringt den Dialog nicht zu Fall, er steht benannt in der Zeile (<see cref="BivalenzKennzeichen.Befund"/>).
        /// </summary>
        public static void Lesen(WErzeugerModel m, WaermepumpeAnlageDaten d)
        {
            if (m == null || d == null) return;
            int idProjekt = m.ID_Projekt;
            int idWp = m.ID_WP;
            d.Kaeltemittelliste = Kaeltemittelliste();
            try { d.Kaeltemittel = WaermepumpeGeraeteCtrl.KaeltemittelLesen(idWp > 0 ? idWp : d.IdWp); }
            catch (Exception) { /* ohne Spalte oder Datenbank bleibt die Wahl leer */ }
            var projekt = new Lazy<BivalenzProjektdaten>(() =>
            {
                try { return BivalenzQuelle.Projekt(idProjekt); }
                catch (Exception ex) { return new BivalenzProjektdaten { Befund = ex.Message }; }
            });
            var kennfelder = new Dictionary<double, IReadOnlyList<Kennfeldpunkt>>();
            d.BivalenzRechnen = daten =>
            {
                BivalenzProjektdaten p = projekt.Value;
                double vorlauf = Hoechstvorlauf(daten);
                if (!kennfelder.TryGetValue(vorlauf, out IReadOnlyList<Kennfeldpunkt>? kennfeld))
                {
                    try { kennfeld = BivalenzQuelle.Kennfeld(daten.IdWp > 0 ? daten.IdWp : idWp, vorlauf); }
                    catch (Exception ex) { return new WaermepumpeBivalenzWerte { Kennzeichen = BivalenzKennzeichen.Befund, Befund = ex.Message }; }
                    kennfelder[vorlauf] = kennfeld;
                }
                try
                {
                    return Werte(p, Geraet(daten, kennfeld), Bivalenzpruefung.Grenzen(daten.Kaeltemittel, vorlauf),
                                 daten.Heizstab, daten.BivalenterBetrieb);
                }
                catch (ArgumentException ex) { return new WaermepumpeBivalenzWerte { Kennzeichen = BivalenzKennzeichen.Befund, Befund = ex.Message }; }
            };
        }

        /// <summary>Der Höchstvorlauf des Arbeitsstands: <c>Vorlauf_Max</c>, sonst der projektierte Vorlauf; NaN ohne beide.</summary>
        internal static double Hoechstvorlauf(WaermepumpeAnlageDaten d)
            => d.VorlaufMax is double v && v > 0.0 ? v
               : d.Vorlauf is int p && p > 0 ? p
               : double.NaN;

        /// <summary>
        /// Die Geräteseite aus dem Arbeitsstand: Einbindung (normiert, leer = nicht gewählt), Vorwärmbetrieb nur bivalent
        /// und nicht alternativ, Mindestspreizung nach Kältemittel.
        /// </summary>
        internal static BivalenzGeraetedaten Geraet(WaermepumpeAnlageDaten d, IReadOnlyList<Kennfeldpunkt> kennfeld)
        {
            Bivalenzbetriebsart art = Betriebsart(d);
            double vorlauf = Hoechstvorlauf(d);
            return new BivalenzGeraetedaten
            {
                HoechstvorlaufC = vorlauf,
                Kennfeld = kennfeld,
                SpreizungMinK = Bivalenzpruefung.Grenzen(d.Kaeltemittel, vorlauf).SpreizungMinK,
                Betriebsart = art,
                AbschaltpunktC = d.BivalenterBetrieb ? d.Abschaltpunkt : null,
                Vorwaermbetrieb = d.BivalenterBetrieb && d.Vorwaermbetrieb && Bivalenzpruefung.VorwaermbetriebWaehlbar(art),
                Einbindung = Bivalenzpruefung.EinbindungNormiert(d.Einbindung),
            };
        }

        /// <summary>Die Betriebsart des Arbeitsstands (Rückfall teilparallel).</summary>
        internal static Bivalenzbetriebsart Betriebsart(WaermepumpeAnlageDaten d) => d.Betriebsart switch
        {
            DbWerte.WP_BETRIEBSART_PARALLEL => Bivalenzbetriebsart.Parallel,
            DbWerte.WP_BETRIEBSART_ALTERNATIV => Bivalenzbetriebsart.Alternativ,
            _ => Bivalenzbetriebsart.Teilparallel,
        };

        /// <summary>
        /// Die Werte der Zeile aus Projekt- und Gerätedaten — ohne Datenbank. Ohne Kopplung, unvollständig oder mit
        /// Befund bleibt es beim Kennzeichen; ohne Kennlinie stehen nur die Übergabewerte da.
        /// </summary>
        internal static WaermepumpeBivalenzWerte Werte(BivalenzProjektdaten p, BivalenzGeraetedaten g,
                                                       Geraetegrenzwerte? grenzen = null, bool heizstab = false,
                                                       bool bivalent = false)
        {
            grenzen ??= Bivalenzpruefung.Grenzen(null, g.HoechstvorlaufC);
            Bivalenzherleitung? h = null;
            WaermepumpeBivalenzWerte w = Grundwerte(p, g, ref h);
            IReadOnlyList<Bivalenzbefund> befunde = Bivalenzpruefung.Pruefen(new Bivalenzpruefeingang
            {
                HoechstvorlaufC = g.HoechstvorlaufC,
                Grenzen = grenzen,
                FlaechenVorlaufC = p.FlaechenVorlaufC,
                AuslegungRuecklaufMinC = p.AuslegungRuecklaufMinC,
                Bivalent = bivalent,
                Betriebsart = g.Betriebsart,
                Vorwaermbetrieb = g.Vorwaermbetrieb,
                Kaskade = p.Kaskade,
                Heizstab = heizstab,
                HybridAnteil = h?.HybridAnteil ?? double.NaN,
                HybridMindestanteil = h?.HybridMindestanteil ?? double.NaN,
                UebergabeBegrenzt = h != null && h.Zustand != Herleitungszustand.OhneKopplung && h.Punkte.UebergabeBegrenzt,
                ErsterBivalenzpunktC = h?.Punkte.ErsterC ?? double.NaN,
            });
            return w with
            {
                Grenzen = Abbild(grenzen, g.HoechstvorlaufC),
                Befunde = befunde.Select(b => new WaermepumpeBivalenzBefund(
                    (BivalenzBefundArt)(int)b.Art, b.NurHinweis, b.Wert1, b.Wert2)).ToList(),
            };
        }

        /// <summary>Die Lesewerte als DTO der Oberfläche.</summary>
        internal static WaermepumpeGrenzwerte Abbild(Geraetegrenzwerte g, double hoechstvorlaufC) => new(
            g.SpreizungAuslegungK, g.SpreizungMaxK, g.SpreizungMinK, (GrenzwertHerkunft)(int)g.SpreizungHerkunft,
            g.MindestvolumenstromAnteil, (GrenzwertHerkunft)(int)g.MindestvolumenstromHerkunft,
            g.RuecklaufMaxC, (GrenzwertHerkunft)(int)g.RuecklaufHerkunft, hoechstvorlaufC,
            g.BezugsruecklaufC, g.AbwertungProzentJeK, g.RuecklaufGrenzeR744C);

        /// <summary>Die Werte der Herleitungszeile ohne Lesewerte und Befunde; <paramref name="h"/> = die gerechnete Herleitung.</summary>
        private static WaermepumpeBivalenzWerte Grundwerte(BivalenzProjektdaten p, BivalenzGeraetedaten g, ref Bivalenzherleitung? h)
        {
            if (!string.IsNullOrEmpty(p.Befund))
                return new WaermepumpeBivalenzWerte { Kennzeichen = BivalenzKennzeichen.Befund, Befund = p.Befund };
            if (p.Unvollstaendig) return new WaermepumpeBivalenzWerte { Kennzeichen = BivalenzKennzeichen.Unvollstaendig };
            if (p.Gebaeude == null || double.IsNaN(g.HoechstvorlaufC))
                return new WaermepumpeBivalenzWerte { Kennzeichen = BivalenzKennzeichen.OhneKopplung };

            BivalenzGebaeudedaten gebaeude = p.Gebaeude;
            if (g.Kennfeld.Count == 0)
            {
                Gebaeudegrenze grenze = Uebergabegrenze.Gebaeude(gebaeude.Zonen, gebaeude.Gebaeude, g.HoechstvorlaufC,
                                                                 gebaeude.AuslegungRaumC);
                return new WaermepumpeBivalenzWerte
                {
                    Kennzeichen = BivalenzKennzeichen.OhneKennfeld,
                    HoechstvorlaufC = g.HoechstvorlaufC,
                    UebergabeKw = grenze.PhiUeMax,
                    HeizlastKw = gebaeude.HeizlastN,
                    Anteil = gebaeude.HeizlastN > 0.0 ? grenze.PhiUeMax / gebaeude.HeizlastN : double.NaN,
                    RuecklaufC = grenze.RuecklaufC,
                    SpreizungK = grenze.SpreizungK,
                };
            }

            h = Bivalenzherleitung.Rechnen(gebaeude, new BivalenzGeraetedaten
            {
                HoechstvorlaufC = g.HoechstvorlaufC,
                Kennfeld = g.Kennfeld,
                SpreizungMinK = g.SpreizungMinK,
                Betriebsart = g.Betriebsart,
                AbschaltpunktC = g.AbschaltpunktC,
                Vorwaermbetrieb = g.Vorwaermbetrieb,
                Kesselleistung = p.KesselleistungKw,
                Einbindung = g.Einbindung,
            });
            return Werte(h, g.Vorwaermbetrieb);
        }

        /// <summary>Die Werte einer gerechneten Herleitung.</summary>
        internal static WaermepumpeBivalenzWerte Werte(Bivalenzherleitung h, bool vorwaermbetrieb) => new()
        {
            Kennzeichen = h.Zustand switch
            {
                Herleitungszustand.Wirksam => BivalenzKennzeichen.Wirksam,
                Herleitungszustand.NichtWirksam => BivalenzKennzeichen.NichtWirksam,
                _ => BivalenzKennzeichen.OhneKopplung,
            },
            HoechstvorlaufC = h.HoechstvorlaufC,
            UebergabeKw = h.PhiUeMax,
            HeizlastKw = h.HeizlastN,
            Anteil = h.Anteil,
            RuecklaufC = h.RuecklaufUeC,
            SpreizungK = h.SpreizungUeK,
            ErsterC = h.Punkte.ErsterC,
            KennfeldAlleinC = h.Punkte.KennfeldAlleinC,
            ZweiterC = h.Punkte.ZweiterC,
            Vorwaermbetrieb = vorwaermbetrieb,
            KesselAnteil = h.HybridAnteil,
            KesselMindestanteil = h.HybridMindestanteil,
            AbschaltpunktC = h.Punkte.AbschaltpunktC,
            MassgebendC = h.Punkte.MassgebendC,
        };

        // ---- Einbindung, Vorwärmbetrieb und Kältemittel (UB‑E2) ------------------------------------------------

        /// <summary>Aus der Anlagenzeile in den Feldsatz — Einbindung normiert (leer = nicht gewählt), Vorwärmbetrieb.</summary>
        public static void AnlageLesen(WErzeugerModel m, WaermepumpeAnlageDaten d)
        {
            if (m == null || d == null) return;
            d.Einbindung = Bivalenzpruefung.EinbindungNormiert(m.Einbindung);
            d.Vorwaermbetrieb = m.Vorwaermbetrieb;
        }

        /// <summary>Zurück in die Anlagenzeile; eine unbekannte Einbindung wird leer (NULL).</summary>
        public static void AnlageSchreiben(WaermepumpeAnlageDaten d, WErzeugerModel m)
        {
            if (m == null || d == null) return;
            m.Einbindung = Bivalenzpruefung.EinbindungNormiert(d.Einbindung);
            m.Vorwaermbetrieb = d.Vorwaermbetrieb;
        }

        /// <summary>
        /// Die Vorbelegung einer <b>neuen</b> Anlagenzeile (Umsetzungskonzept 6.2): <c>PUFFER</c>, wenn ihr ein
        /// Heizungspuffer zugeordnet ist (<c>WS_ID_Puffer</c> oder <c>WS_ID_Puffer2</c>), sonst <c>DIREKT</c>. Nur für
        /// den Neu-Weg der Verwaltung; Bestandsanlagen bleiben, wie sie gelesen wurden.
        /// </summary>
        public static void Vorbelegen(WErzeugerModel m)
        {
            if (m == null) return;
            bool puffer = (m.WS_ID_Puffer ?? 0) > 0 || (m.WS_ID_Puffer2 ?? 0) > 0;
            m.Einbindung = Bivalenzpruefung.EinbindungVorbelegung(true, puffer);
        }

        /// <summary>Die Felder des schmalen Schreibwegs (<see cref="WErzeugerCtrl.KonfigurationSchreiben"/>).</summary>
        internal static WErzeugerCtrl.KonfigurationFelder MitUebergabe(WErzeugerCtrl.KonfigurationFelder felder,
                                                                    WaermepumpeAnlageDaten d)
            => d == null
                ? felder
                : felder with
                {
                    Uebergabe = true,
                    Einbindung = Bivalenzpruefung.EinbindungNormiert(d.Einbindung),
                    Vorwaermbetrieb = d.Vorwaermbetrieb,
                };

        /// <summary>
        /// Schreibt das gewählte Kältemittel in die Projektkopie des Geräts (Schnellwahl, U‑2) — nur, wenn es vom
        /// gespeicherten abweicht. <c>true</c> = geschrieben oder unverändert; <c>false</c> = keine Projektkopie oder Fehler.
        /// </summary>
        public static bool GeraetSchreiben(WaermepumpeAnlageDaten d)
        {
            if (d == null || d.IdWp <= 0) return false;
            try
            {
                string? alt = WaermepumpeGeraeteCtrl.KaeltemittelLesen(d.IdWp);
                string? neu = string.IsNullOrWhiteSpace(d.Kaeltemittel) ? null : d.Kaeltemittel!.Trim();
                if (string.Equals(alt, neu, StringComparison.Ordinal)) return true;
                return WaermepumpeGeraeteCtrl.KaeltemittelSchreiben(d.IdWp, neu);
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}
