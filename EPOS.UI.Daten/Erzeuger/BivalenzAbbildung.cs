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
    /// <para><b>Einbindung (U‑1).</b> Die Spalte <c>Einbindung</c> kommt erst mit dem Schemaschritt der Etappe E2.
    /// Solange sie fehlt, gilt sie als nicht gesetzt: Mit Übergabedaten steht das Kennzeichen deshalb immer auf
    /// <see cref="BivalenzKennzeichen.NichtWirksam"/>, die Zeile nennt „Einbindung nicht gesetzt — Übergabegrenze ruht"
    /// und zeigt die gerechneten Werte trotzdem.</para>
    ///
    /// <para><b>Kein Vorwärmbetrieb, kein Kältemittel am Gerät</b> in E1: Beide Spalten fehlen noch; gerechnet wird ohne
    /// Vorwärmbetrieb und mit der Mindestspreizung der allgemeinen Vorgabe.</para>
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
                try { return Werte(p, Geraet(daten, kennfeld)); }
                catch (ArgumentException ex) { return new WaermepumpeBivalenzWerte { Kennzeichen = BivalenzKennzeichen.Befund, Befund = ex.Message }; }
            };
        }

        /// <summary>Der Höchstvorlauf des Arbeitsstands: <c>Vorlauf_Max</c>, sonst der projektierte Vorlauf; NaN ohne beide.</summary>
        internal static double Hoechstvorlauf(WaermepumpeAnlageDaten d)
            => d.VorlaufMax is double v && v > 0.0 ? v
               : d.Vorlauf is int p && p > 0 ? p
               : double.NaN;

        /// <summary>Die Geräteseite aus dem Arbeitsstand — ohne Einbindung (U‑1, siehe Klassenkopf) und ohne Vorwärmbetrieb.</summary>
        internal static BivalenzGeraetedaten Geraet(WaermepumpeAnlageDaten d, IReadOnlyList<Kennfeldpunkt> kennfeld)
        {
            Bivalenzbetriebsart art = d.Betriebsart switch
            {
                DbWerte.WP_BETRIEBSART_PARALLEL => Bivalenzbetriebsart.Parallel,
                DbWerte.WP_BETRIEBSART_ALTERNATIV => Bivalenzbetriebsart.Alternativ,
                _ => Bivalenzbetriebsart.Teilparallel,
            };
            return new BivalenzGeraetedaten
            {
                HoechstvorlaufC = Hoechstvorlauf(d),
                Kennfeld = kennfeld,
                Betriebsart = art,
                AbschaltpunktC = d.BivalenterBetrieb ? d.Abschaltpunkt : null,
                Vorwaermbetrieb = false,
                Einbindung = null,
            };
        }

        /// <summary>
        /// Die Werte der Zeile aus Projekt- und Gerätedaten — ohne Datenbank. Ohne Kopplung, unvollständig oder mit
        /// Befund bleibt es beim Kennzeichen; ohne Kennlinie stehen nur die Übergabewerte da.
        /// </summary>
        internal static WaermepumpeBivalenzWerte Werte(BivalenzProjektdaten p, BivalenzGeraetedaten g)
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

            Bivalenzherleitung h = Bivalenzherleitung.Rechnen(gebaeude, new BivalenzGeraetedaten
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
        };
    }
}
