#nullable enable

using System;

namespace WindowsFormsApplication1
{
    /// <summary>Die hydraulische Einbindung der Wärmepumpe (<c>Tab_Energieanlagen.Einbindung</c>, Fachkonzept 2.8).</summary>
    internal enum Einbindungsart
    {
        /// <summary><c>DIREKT</c>: ohne Puffer, mit Überströmventil (ṁ_WP = max(ṁ_HK, ṁ_min)).</summary>
        Direkt = 0,

        /// <summary><c>PUFFER</c>: Reihen- oder Parallelpuffer; gerechnet wird die Ladeseite.</summary>
        Puffer = 1,

        /// <summary><c>WEICHE</c>: hydraulische Weiche bzw. Trennspeicher, Mischung an der Trennstelle.</summary>
        Weiche = 2,
    }

    /// <summary>Die Eingänge der Hydraulikgrenze einer Stunde; Kapazitätsströme ṁ·c_p in kW/K.</summary>
    internal readonly record struct Hydraulikeingang
    {
        /// <summary>Die Einbindung der Anlage.</summary>
        internal Einbindungsart Einbindung { get; init; }

        /// <summary>W_H = ṁ_HK·c_p — der Kapazitätsstrom des Heizkreises bei Auslegungsmassenstrom [kW/K].</summary>
        internal double WH { get; init; }

        /// <summary>Der Vorlauf, den die Wärmepumpe stellen soll: min(θ_V,soll, θ_WP,max) [°C].</summary>
        internal double VorlaufWpC { get; init; }

        /// <summary>θ_R,HK — der Heizkreisrücklauf der Stunde [°C]; NaN = unbekannt.</summary>
        internal double RuecklaufHeizkreisC { get; init; }

        /// <summary>Die unterste Pufferzone am Stundenbeginn [°C] (nur <see cref="Einbindungsart.Puffer"/>); NaN = ohne.</summary>
        internal double PufferUntenC { get; init; }

        /// <summary>θ_WP,max [°C].</summary>
        internal double HoechstvorlaufC { get; init; }

        /// <summary>Φ_WP,N — Nennleistung der Wärmepumpe [kW]; NaN oder ≤ 0 = unbekannt (ohne ṁ_N, ohne ṁ_min).</summary>
        internal double NennleistungKw { get; init; }

        /// <summary>Φ_WP — Kennfeldleistung der Stunde [kW] (für σ_max,eff).</summary>
        internal double KennfeldKw { get; init; }

        /// <summary>σ_A [K].</summary>
        internal double SpreizungAuslegungK { get; init; }

        /// <summary>σ_max [K].</summary>
        internal double SpreizungMaxK { get; init; }

        /// <summary>σ_min [K].</summary>
        internal double SpreizungMinK { get; init; }

        /// <summary>Mindestvolumenstrom als Anteil des Nennvolumenstroms [–]; 0 = ohne.</summary>
        internal double MindestvolumenstromAnteil { get; init; }
    }

    /// <summary>Das Ergebnis der Hydraulikgrenze einer Stunde.</summary>
    /// <param name="LeistungKw">Φ_Hydraulik [kW]; +∞, wenn die Hydraulik nicht begrenzt.</param>
    /// <param name="Grund"><c>SPREIZUNG_MAX</c> (σ_max,eff bindet), <c>UEBERGABE_HOECHSTVORLAUF</c> (Weiche: θ_WP,max bindet am
    /// gemischten Vorlauf) oder <see cref="Verfuegbarkeitsgrund.KeineBegrenzung"/>.</param>
    /// <param name="Taktet">σ_WP &lt; σ_min — die Stunde taktet (Energie wie heute, Zähler „Spreizung unterschritten").</param>
    /// <param name="RuecklaufWpC">θ_R,WP — der Rücklauf zur Wärmepumpe (direkt, gemischt nach 2.8, unterste Pufferzone).</param>
    /// <param name="VorlaufHeizkreisC">θ_V,HK — der Heizkreisvorlauf (Weiche mit ṁ_WP &lt; ṁ_HK gemischt); NaN ohne Rechnung.</param>
    /// <param name="SpreizungMaxEffK">σ_max,eff = min(σ_max, Φ_WP/(ṁ_min·c_p)) [K].</param>
    internal readonly record struct Hydraulikergebnis(double LeistungKw, Verfuegbarkeitsgrund Grund, bool Taktet,
                                                     double RuecklaufWpC, double VorlaufHeizkreisC, double SpreizungMaxEffK)
    {
        /// <summary>Ohne Hydraulikdaten: unbegrenzt, Rücklauf unbekannt.</summary>
        internal static readonly Hydraulikergebnis Unbegrenzt = new Hydraulikergebnis(
            double.PositiveInfinity, Verfuegbarkeitsgrund.KeineBegrenzung, false, double.NaN, double.NaN, double.NaN);
    }

    /// <summary>
    /// <b>Die Hydraulikgrenze der Wärmepumpe</b> Φ_Hydraulik je <c>Einbindung</c> (Fachkonzept Übergabegrenze 4.4, 2.8;
    /// Umsetzungskonzept 3.1, 3.2 Schritt 4). Kapazitätsströme statt Massenströmen (ṁ·c_p in kW/K); W_H ist der des
    /// Heizkreises, ṁ_N·c_p = Φ_WP,N/σ_A der Nennstrom der Wärmepumpe, ṁ_min·c_p = Anteil·ṁ_N·c_p.
    /// <list type="bullet">
    /// <item><b>Mindestvolumenstrom als Höchstspreizung:</b> σ_max,eff = min(σ_max, Φ_WP/(ṁ_min·c_p)) — gelten
    /// Mindestvolumenstrom und σ_max beide, gilt die strengere.</item>
    /// <item><b>DIREKT</b> mit Überströmventil: ṁ_WP = max(ṁ_HK, ṁ_min); bei ṁ_WP &gt; ṁ_HK steigt der Rücklauf der
    /// Wärmepumpe durch Beimischung (Zweig ṁ_WP ≥ ṁ_HK der Mischungsbilanz 2.8). Verlangt die Stunde
    /// σ_WP = W_H·(θ_V,WP − θ_R)/ṁ_WP &gt; σ_max,eff, ist Φ = ṁ_WP·σ_max,eff (Grund <c>SPREIZUNG_MAX</c>; ohne
    /// Beimischung W_H·σ_max). σ_WP &lt; σ_min: die Stunde taktet — Φ_HK &lt; ṁ_min·c_p·σ_min mit Überströmventil.</item>
    /// <item><b>WEICHE</b>: ṁ_WP = ṁ_N. ṁ_WP ≥ ṁ_HK wie direkt, ohne σ-Grenze aus dem Heizkreis, Rücklauf gemischt;
    /// ṁ_WP &lt; ṁ_HK: θ_R,WP = θ_R,HK, Φ = ṁ_WP·min(θ_WP,max − θ_R, σ_max,eff), der Heizkreisvorlauf ist die Mischung
    /// θ_V,HK = [ṁ_WP·θ_V,WP + (ṁ_HK − ṁ_WP)·θ_R]/ṁ_HK.</item>
    /// <item><b>PUFFER</b>: die Ladeseite, Φ = ṁ_N·σ_max,eff; der Rücklauf ist die unterste Pufferzone. Die
    /// Mischungsbilanz der Entladeseite wird <b>benannt nicht gerechnet</b> (Näherung: Entnahme mit <c>TNutz</c> des
    /// Kanals, wie AK3 Festlegung 6) — vertretbar bei ṁ_WP ≥ ṁ_HK oder geschichtetem Puffer.</item>
    /// </list>
    /// Ohne Heizkreistemperaturen (NaN) und ohne Nennleistung begrenzt die Hydraulik nicht (+∞). Reine Zahlen, deterministisch.
    /// </summary>
    internal static class Hydraulikgrenze
    {
        /// <summary>Die Einbindung aus dem Code der Anlage (<c>DIREKT</c>/<c>PUFFER</c>/<c>WEICHE</c>); unbekannt = direkt.</summary>
        internal static Einbindungsart Art(string? einbindung)
        {
            string? n = Bivalenzpruefung.EinbindungNormiert(einbindung);
            if (string.Equals(n, Bivalenzpruefung.EINBINDUNG_PUFFER, StringComparison.Ordinal)) return Einbindungsart.Puffer;
            if (string.Equals(n, Bivalenzpruefung.EINBINDUNG_WEICHE, StringComparison.Ordinal)) return Einbindungsart.Weiche;
            return Einbindungsart.Direkt;
        }

        /// <summary>ṁ_N·c_p = Φ_WP,N/σ_A [kW/K]; NaN ohne Nennleistung oder Auslegungsspreizung.</summary>
        internal static double Nennstrom(double nennleistungKw, double spreizungAuslegungK)
            => nennleistungKw > 0.0 && spreizungAuslegungK > 0.0 ? nennleistungKw / spreizungAuslegungK : double.NaN;

        /// <summary>σ_max,eff = min(σ_max, Φ_WP/(ṁ_min·c_p)); ohne Mindeststrom oder Leistung σ_max.</summary>
        internal static double SpreizungMaxEff(double spreizungMaxK, double leistungKw, double mindeststromKwJeK)
        {
            if (!(mindeststromKwJeK > 0.0) || !(leistungKw > 0.0) || double.IsInfinity(leistungKw)) return spreizungMaxK;
            return Math.Min(spreizungMaxK, leistungKw / mindeststromKwJeK);
        }

        /// <summary>
        /// Mischungsbilanz 2.8 an der Trennstelle: (θ_V,HK, θ_R,WP) aus den Kapazitätsströmen und θ_V,WP, θ_R,HK.
        /// Beide Zweige energieerhaltend und am Umschaltpunkt stetig.
        /// </summary>
        internal static (double VorlaufHeizkreisC, double RuecklaufWpC) Mischung(double stromWp, double stromHk,
                                                                                double vorlaufWpC, double ruecklaufHkC)
        {
            if (stromHk > stromWp)
                return ((stromWp * vorlaufWpC + (stromHk - stromWp) * ruecklaufHkC) / stromHk, ruecklaufHkC);
            if (!(stromWp > stromHk)) return (vorlaufWpC, ruecklaufHkC);   // ṁ_WP = ṁ_HK: ungemischt
            return (vorlaufWpC, (stromHk * ruecklaufHkC + (stromWp - stromHk) * vorlaufWpC) / stromWp);
        }

        /// <summary>Φ_Hydraulik der Stunde samt Grund, Taktmarke und Rücklauf der Wärmepumpe.</summary>
        internal static Hydraulikergebnis Rechnen(in Hydraulikeingang e)
        {
            double nenn = Nennstrom(e.NennleistungKw, e.SpreizungAuslegungK);
            double mindest = !double.IsNaN(nenn) && e.MindestvolumenstromAnteil > 0.0 ? e.MindestvolumenstromAnteil * nenn : 0.0;
            double sigmaEff = SpreizungMaxEff(e.SpreizungMaxK, e.KennfeldKw, mindest);

            if (e.Einbindung == Einbindungsart.Puffer)
            {
                // Ladeseite: ṁ_WP·c_p·σ_max,eff; Entladeseite benannt nicht gerechnet (siehe Klassenkommentar).
                double laden = double.IsNaN(nenn) ? double.PositiveInfinity : nenn * sigmaEff;
                Verfuegbarkeitsgrund g = laden < e.KennfeldKw ? Verfuegbarkeitsgrund.SpreizungMax : Verfuegbarkeitsgrund.KeineBegrenzung;
                return new Hydraulikergebnis(laden, g, false, e.PufferUntenC, double.NaN, sigmaEff);
            }

            double vorlauf = e.VorlaufWpC, ruecklauf = e.RuecklaufHeizkreisC;
            if (double.IsNaN(vorlauf) || double.IsNaN(ruecklauf) || double.IsInfinity(vorlauf) || double.IsInfinity(ruecklauf)
                || !(e.WH > 0.0))
                return Hydraulikergebnis.Unbegrenzt with { SpreizungMaxEffK = sigmaEff, RuecklaufWpC = ruecklauf };
            double heizkreis = e.WH * Math.Max(0.0, vorlauf - ruecklauf);

            if (e.Einbindung == Einbindungsart.Weiche && !double.IsNaN(nenn))
            {
                if (nenn >= e.WH)
                {
                    // ṁ_WP ≥ ṁ_HK: wie direkt, ohne σ-Grenze aus dem Heizkreis; der Rücklauf der Wärmepumpe ist gemischt.
                    var m = Mischung(nenn, e.WH, vorlauf, ruecklauf);
                    bool takt = heizkreis > 0.0 && heizkreis < nenn * e.SpreizungMinK;
                    return new Hydraulikergebnis(double.PositiveInfinity, Verfuegbarkeitsgrund.KeineBegrenzung, takt,
                                                 m.RuecklaufWpC, m.VorlaufHeizkreisC, sigmaEff);
                }
                // ṁ_WP < ṁ_HK: der Heizkreisvorlauf ist die Mischung; die Wärmepumpe hebt höchstens auf θ_WP,max und um σ_max,eff.
                double hub = e.HoechstvorlaufC - ruecklauf;
                double grenzeK = Math.Min(hub, sigmaEff);
                double grenzeW = nenn * Math.Max(0.0, grenzeK);
                double leistung = Math.Min(heizkreis, grenzeW);
                double vorlaufWp = ruecklauf + leistung / nenn;
                var mw = Mischung(nenn, e.WH, vorlaufWp, ruecklauf);
                Verfuegbarkeitsgrund gw = heizkreis > grenzeW
                    ? (hub <= sigmaEff ? Verfuegbarkeitsgrund.UebergabeHoechstvorlauf : Verfuegbarkeitsgrund.SpreizungMax)
                    : Verfuegbarkeitsgrund.KeineBegrenzung;
                bool taktW = leistung > 0.0 && leistung < nenn * e.SpreizungMinK;
                return new Hydraulikergebnis(heizkreis > grenzeW ? grenzeW : double.PositiveInfinity, gw, taktW,
                                             ruecklauf, mw.VorlaufHeizkreisC, sigmaEff);
            }

            // DIREKT mit Überströmventil (und Weiche ohne Nennstrom): ṁ_WP = max(ṁ_HK, ṁ_min).
            double strom = Math.Max(e.WH, mindest);
            var md = Mischung(strom, e.WH, vorlauf, ruecklauf);
            double spreizungWp = heizkreis / strom;
            bool taktD = heizkreis > 0.0 && spreizungWp < e.SpreizungMinK;
            if (spreizungWp > sigmaEff)
                return new Hydraulikergebnis(strom * sigmaEff, Verfuegbarkeitsgrund.SpreizungMax, taktD,
                                             md.RuecklaufWpC, vorlauf, sigmaEff);
            return new Hydraulikergebnis(double.PositiveInfinity, Verfuegbarkeitsgrund.KeineBegrenzung, taktD,
                                         md.RuecklaufWpC, vorlauf, sigmaEff);
        }
    }
}
