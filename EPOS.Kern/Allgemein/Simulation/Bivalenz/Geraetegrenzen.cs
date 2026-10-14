#nullable enable

using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Rohwerte der acht Gerätespalten der Wärmepumpe (Schemaschritt <see cref="UebergabegrenzeSchema"/>);
    /// <c>null</c> = leer. Gelesen aus der Projektkopie <c>Tab_WP</c> (Lauf) bzw. Projektkopie vor Stammkatalog
    /// (Dialog, wie <c>WaermepumpeGeraeteCtrl.KaeltemittelLesen</c>).
    /// </summary>
    internal sealed record Geraetespalten(
        double? SpreizungAuslegungK,
        double? SpreizungMaxK,
        double? SpreizungMinK,
        double? MindestvolumenstromProzent,
        double? RuecklaufMaxC,
        double? RuecklaufBezugC,
        double? RuecklaufAbwertungProzentJeK,
        string? Kaeltemittel)
    {
        /// <summary>Alle Spalten leer — das Gerät rechnet mit den Vorgaben des Kerns.</summary>
        internal static readonly Geraetespalten Leer = new Geraetespalten(null, null, null, null, null, null, null, null);

        /// <summary>Nur das Kältemittel (Schnellwahl im Dialog), alle Zahlspalten leer.</summary>
        internal static Geraetespalten NurKaeltemittel(string? kaeltemittel)
            => Leer with { Kaeltemittel = kaeltemittel };
    }

    /// <summary>
    /// <b>Die Gerätegrenzen einer Wärmepumpe</b> (Fachkonzept Übergabegrenze 4.4, 5.1, 2.11; Umsetzungskonzept 3.1, U‑2):
    /// die acht Gerätespalten, je leerem Wert die Vorgabe der Kältemittelklasse aus <see cref="Bivalenzvorgaben"/> (leeres,
    /// unbekanntes Kältemittel oder <c>SONSTIGES</c>: die allgemeine Vorgabe), dazu die abgeleitete Rücklaufgrenze
    /// <code>
    /// θ_R,grenz = min( Ruecklauf_Max , θ_WP,max − σ_min )      leeres Feld (ohne Vorgabe der Klasse): nur der zweite Term
    /// </code>
    /// Je Wert die Herkunft <c>Katalog</c> (gepflegt) / <c>Vorgabe nach Kältemittel</c> / <c>Vorgabe</c> / <c>abgeleitet</c>.
    /// Reine Zahlen, ohne Datenbank; deterministisch.
    /// </summary>
    internal sealed class Geraetegrenzen
    {
        private Geraetegrenzen() { }

        /// <summary>Spreizung am Verflüssiger in der Auslegung σ_A [K].</summary>
        internal double SpreizungAuslegungK { get; private init; }

        /// <summary>Höchstspreizung σ_max [K].</summary>
        internal double SpreizungMaxK { get; private init; }

        /// <summary>Mindestspreizung σ_min [K].</summary>
        internal double SpreizungMinK { get; private init; }

        /// <summary>Mindestvolumenstrom als Anteil des Nennvolumenstroms [–].</summary>
        internal double MindestvolumenstromAnteil { get; private init; }

        /// <summary>Der Höchstvorlauf θ_WP,max, zu dem die Grenze gebildet wurde [°C]; NaN = unbekannt.</summary>
        internal double HoechstvorlaufC { get; private init; }

        /// <summary>Das Feld <c>Ruecklauf_Max</c> bzw. die Grenze der Kältemittelklasse [°C]; NaN = leer (nur abgeleitet).</summary>
        internal double RuecklaufFeldC { get; private init; }

        /// <summary>θ_R,grenz = min(Feld, θ_WP,max − σ_min) [°C]; NaN = ohne Grenze.</summary>
        internal double RuecklaufGrenzeC { get; private init; }

        /// <summary>Abwertung k je K über dem Bezugsrücklauf [%/K]; 0 = keine Abwertung.</summary>
        internal double AbwertungProzentJeK { get; private init; }

        /// <summary>Bezugsrücklauf θ_R,bez der Abwertung [°C]; NaN ohne Abwertung.</summary>
        internal double BezugsruecklaufC { get; private init; }

        /// <summary>Wird die Leistung oberhalb des Bezugsrücklaufs abgewertet (R744-Faktor)?</summary>
        internal bool Abwertung => AbwertungProzentJeK > 0.0;

        /// <summary>Der wirksame Kältemittelcode (getrimmt); leer = allgemeine Vorgabe.</summary>
        internal string Kaeltemittel { get; private init; } = string.Empty;

        internal Geraetegrenzherkunft SpreizungAuslegungHerkunft { get; private init; }
        internal Geraetegrenzherkunft SpreizungMaxHerkunft { get; private init; }
        internal Geraetegrenzherkunft SpreizungMinHerkunft { get; private init; }
        internal Geraetegrenzherkunft MindestvolumenstromHerkunft { get; private init; }
        internal Geraetegrenzherkunft RuecklaufHerkunft { get; private init; }
        internal Geraetegrenzherkunft BezugHerkunft { get; private init; }
        internal Geraetegrenzherkunft AbwertungHerkunft { get; private init; }

        /// <summary>
        /// Bildet die Grenzen aus den Spalten <paramref name="spalten"/> (<c>null</c> = alle leer) und dem Höchstvorlauf
        /// <paramref name="hoechstvorlaufC"/> (NaN = unbekannt: die Rücklaufgrenze ist dann nur das Feld).
        /// </summary>
        internal static Geraetegrenzen Bilden(Geraetespalten? spalten, double hoechstvorlaufC)
        {
            Geraetespalten s = spalten ?? Geraetespalten.Leer;
            Kaeltemittelvorgabe v = Bivalenzvorgaben.Vorgabe(s.Kaeltemittel);
            Geraetegrenzherkunft vorgabe = v.Herkunft == Kaeltemittelherkunft.Kaeltemittelklasse
                ? Geraetegrenzherkunft.VorgabeKaeltemittel
                : Geraetegrenzherkunft.Vorgabe;

            double Wert(double? feld, double vorgabeWert, out Geraetegrenzherkunft herkunft)
            {
                if (Gepflegt(feld)) { herkunft = Geraetegrenzherkunft.Katalog; return feld!.Value; }
                herkunft = vorgabe;
                return vorgabeWert;
            }

            double sigmaA = Wert(s.SpreizungAuslegungK, v.SpreizungAuslegungK, out Geraetegrenzherkunft hA);
            double sigmaMax = Wert(s.SpreizungMaxK, v.SpreizungMaxK, out Geraetegrenzherkunft hMax);
            double sigmaMin = Wert(s.SpreizungMinK, v.SpreizungMinK, out Geraetegrenzherkunft hMin);
            double anteil = Gepflegt(s.MindestvolumenstromProzent) ? s.MindestvolumenstromProzent!.Value / 100.0 : v.MindestvolumenstromAnteil;
            Geraetegrenzherkunft hMv = Gepflegt(s.MindestvolumenstromProzent) ? Geraetegrenzherkunft.Katalog : vorgabe;

            // Das Feld Ruecklauf_Max, leer die Grenze der Kältemittelklasse (R744), sonst keines.
            double feldC = double.NaN;
            Geraetegrenzherkunft hFeld = vorgabe;
            if (Gepflegt(s.RuecklaufMaxC)) { feldC = s.RuecklaufMaxC!.Value; hFeld = Geraetegrenzherkunft.Katalog; }
            else if (v.RuecklaufGrenzeC.HasValue) feldC = v.RuecklaufGrenzeC.Value;
            double abgeleitet = hoechstvorlaufC - sigmaMin;
            double grenze;
            Geraetegrenzherkunft hR;
            if (double.IsNaN(abgeleitet) || double.IsInfinity(abgeleitet)) { grenze = feldC; hR = hFeld; }
            else if (!double.IsNaN(feldC) && feldC <= abgeleitet) { grenze = feldC; hR = hFeld; }
            else { grenze = abgeleitet; hR = Geraetegrenzherkunft.Abgeleitet; }

            // Abwertung (R744-Faktor): das Feld, leer die Vorgabe der Klasse (nur R744), sonst keine.
            double k = 0.0;
            Geraetegrenzherkunft hK = vorgabe;
            if (s.RuecklaufAbwertungProzentJeK.HasValue && !double.IsNaN(s.RuecklaufAbwertungProzentJeK.Value))
            {
                k = Math.Max(0.0, s.RuecklaufAbwertungProzentJeK.Value);
                hK = Geraetegrenzherkunft.Katalog;
            }
            else if (v.AbwertungProzentJeK.HasValue) k = v.AbwertungProzentJeK.Value;
            double bezug = double.NaN;
            Geraetegrenzherkunft hB = vorgabe;
            if (k > 0.0)
            {
                if (Gepflegt(s.RuecklaufBezugC)) { bezug = s.RuecklaufBezugC!.Value; hB = Geraetegrenzherkunft.Katalog; }
                else bezug = v.BezugsruecklaufC ?? Bivalenzvorgaben.BEZUGSRUECKLAUF_R744_C;
            }

            return new Geraetegrenzen
            {
                SpreizungAuslegungK = sigmaA, SpreizungAuslegungHerkunft = hA,
                SpreizungMaxK = sigmaMax, SpreizungMaxHerkunft = hMax,
                SpreizungMinK = sigmaMin, SpreizungMinHerkunft = hMin,
                MindestvolumenstromAnteil = anteil, MindestvolumenstromHerkunft = hMv,
                HoechstvorlaufC = hoechstvorlaufC,
                RuecklaufFeldC = feldC,
                RuecklaufGrenzeC = grenze, RuecklaufHerkunft = hR,
                AbwertungProzentJeK = k, AbwertungHerkunft = hK,
                BezugsruecklaufC = bezug, BezugHerkunft = hB,
                Kaeltemittel = string.IsNullOrWhiteSpace(s.Kaeltemittel) ? string.Empty : s.Kaeltemittel!.Trim(),
            };
        }

        /// <summary>
        /// Die Lesewerte der Gruppe „Bivalenz und Übergabe" (<see cref="Geraetegrenzwerte"/>): die Spreizungen tragen
        /// „Katalog", sobald eine der drei gepflegt ist; der R744-Teil (Bezug, Abwertung, Grenze) nur mit Abwertung.
        /// </summary>
        internal Geraetegrenzwerte Werte()
        {
            Geraetegrenzherkunft spreizung =
                SpreizungAuslegungHerkunft == Geraetegrenzherkunft.Katalog || SpreizungMaxHerkunft == Geraetegrenzherkunft.Katalog
                || SpreizungMinHerkunft == Geraetegrenzherkunft.Katalog
                    ? Geraetegrenzherkunft.Katalog : SpreizungMinHerkunft;
            double? grenzeR744 = Abwertung ? (double.IsNaN(RuecklaufFeldC) ? RuecklaufGrenzeC : RuecklaufFeldC) : (double?)null;
            return new Geraetegrenzwerte(
                SpreizungAuslegungK, SpreizungMaxK, SpreizungMinK, spreizung,
                MindestvolumenstromAnteil, MindestvolumenstromHerkunft,
                RuecklaufGrenzeC, RuecklaufHerkunft,
                Abwertung ? BezugsruecklaufC : (double?)null, Abwertung ? AbwertungProzentJeK : (double?)null, grenzeR744);
        }

        private static bool Gepflegt(double? w) => w.HasValue && !double.IsNaN(w.Value) && !double.IsInfinity(w.Value);
    }
}
