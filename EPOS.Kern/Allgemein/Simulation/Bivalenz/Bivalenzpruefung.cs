#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>Woher ein Lesewert der Gerätegrenzen stammt (Umsetzungskonzept Übergabegrenze 6.2).</summary>
    internal enum Geraetegrenzherkunft
    {
        /// <summary>Gepflegter Wert des Geräts (Projektkopie bzw. Katalog).</summary>
        Katalog = 0,

        /// <summary>Vorgabe der Kältemittelklasse aus <see cref="Bivalenzvorgaben"/>.</summary>
        VorgabeKaeltemittel = 1,

        /// <summary>Allgemeine (unterkritische) Vorgabe aus <see cref="Bivalenzvorgaben"/>.</summary>
        Vorgabe = 2,

        /// <summary>Abgeleitet: Rücklaufgrenze = Höchstvorlauf − Mindestspreizung.</summary>
        Abgeleitet = 3,
    }

    /// <summary>
    /// Die Lesewerte der Gerätegrenzen in der Gruppe „Bivalenz und Übergabe" (Fachkonzept 6.2, Tafel 6.3): Spreizungen
    /// [K], Mindestvolumenstrom [–], höchster Rücklauf [°C] je mit Herkunft; bei R744 Bezugsrücklauf [°C], Abwertung
    /// [%/K] und Rücklaufgrenze [°C], sonst <c>null</c>.
    /// </summary>
    internal sealed record Geraetegrenzwerte(
        double SpreizungAuslegungK,
        double SpreizungMaxK,
        double SpreizungMinK,
        Geraetegrenzherkunft SpreizungHerkunft,
        double MindestvolumenstromAnteil,
        Geraetegrenzherkunft MindestvolumenstromHerkunft,
        double RuecklaufMaxC,
        Geraetegrenzherkunft RuecklaufHerkunft,
        double? BezugsruecklaufC,
        double? AbwertungProzentJeK,
        double? RuecklaufGrenzeR744C);

    /// <summary>Die weichen Sperren und Hinweise der Gruppe „Bivalenz und Übergabe" (Fachkonzept 6.2, Umsetzungskonzept 6.2).</summary>
    internal enum Bivalenzbefundart
    {
        /// <summary>σ_min ≥ σ_max — kein Betriebspunkt. Wert1 = σ_min, Wert2 = σ_max.</summary>
        Spreizung = 0,

        /// <summary>Höchstvorlauf unter dem Auslegungsvorlauf einer Flächenheizung. Wert1 = θ_WP,max, Wert2 = θ_V,N der Fläche.</summary>
        Hoechstvorlauf = 1,

        /// <summary>Rücklaufgrenze unter dem Auslegungsrücklauf aller Zonen. Wert1 = Grenze, Wert2 = kleinster θ_R,N.</summary>
        RuecklaufNie = 2,

        /// <summary>Vorwärmbetrieb ohne Kessel oder Heizstab in der Kaskade.</summary>
        VorwaermOhneKessel = 3,

        /// <summary>Vorwärmbetrieb, aber die Wärmepumpe steht in der Kaskade hinter dem Kessel.</summary>
        Kaskade = 4,

        /// <summary>Hinweis: Hybrid-Mindestanteil nach § 43 GModG unterschritten. Wert1 = Anteil, Wert2 = Mindestanteil (Brüche).</summary>
        Gmodg = 5,

        /// <summary>Hinweis: Die Übergabe begrenzt stärker als das Kennfeld. Wert1 = erster Bivalenzpunkt [°C].</summary>
        UebergabeBegrenzt = 6,
    }

    /// <summary>Ein Befund der Dialogprüfung; <see cref="NurHinweis"/> = Stufe Hinweis statt Warnung. Speichern bleibt immer möglich.</summary>
    internal readonly record struct Bivalenzbefund(Bivalenzbefundart Art, bool NurHinweis, double Wert1, double Wert2);

    /// <summary>Der Eingang der Dialogprüfung — alles ohne Datenbank; NaN bzw. <c>null</c> = unbekannt, die Regel ruht.</summary>
    internal sealed class Bivalenzpruefeingang
    {
        /// <summary>θ_WP,max [°C].</summary>
        internal double HoechstvorlaufC { get; init; } = double.NaN;

        /// <summary>Die Gerätegrenzen (Spreizungen, Rücklaufgrenze).</summary>
        internal Geraetegrenzwerte? Grenzen { get; init; }

        /// <summary>Größter Auslegungsvorlauf der gekoppelten Flächenheizungen [°C]; NaN = keine Fläche.</summary>
        internal double FlaechenVorlaufC { get; init; } = double.NaN;

        /// <summary>
        /// Rücklauf an der Übergabegrenze θ_R,UE [°C] — Gleichgewicht bei θ_WP,max, Auslegungsmassenstrom und
        /// Auslegungsraumtemperatur, über mehrere Zonen massenstromgewichtet (<see cref="Bivalenzherleitung.RuecklaufUeC"/>);
        /// NaN = keine Übergabe beschrieben (ohne Kopplung, unvollständig).
        /// </summary>
        internal double RuecklaufUebergabeC { get; init; } = double.NaN;

        /// <summary>Bivalenter Betrieb eingeschaltet?</summary>
        internal bool Bivalent { get; init; }

        /// <summary>Betriebsart.</summary>
        internal Bivalenzbetriebsart Betriebsart { get; init; } = Bivalenzbetriebsart.Teilparallel;

        /// <summary>Vorwärmbetrieb eingeschaltet?</summary>
        internal bool Vorwaermbetrieb { get; init; }

        /// <summary>Die Kaskade <c>Tool_1</c> … <c>Tool_4</c>; <c>null</c> = unbekannt (Kaskadenregeln ruhen).</summary>
        internal IReadOnlyList<string?>? Kaskade { get; init; }

        /// <summary>Führt die Anlage einen Heizstab?</summary>
        internal bool Heizstab { get; init; }

        /// <summary>Anteil der Wärmepumpe an der Kesselleistung bei −7 °C [–]; NaN = ohne Kessel.</summary>
        internal double HybridAnteil { get; init; } = double.NaN;

        /// <summary>Mindestanteil nach § 43 GModG [–].</summary>
        internal double HybridMindestanteil { get; init; } = double.NaN;

        /// <summary>Begrenzt die Übergabe stärker als das Kennfeld (<see cref="Bivalenzpunkte.UebergabeBegrenzt"/>)?</summary>
        internal bool UebergabeBegrenzt { get; init; }

        /// <summary>Erster Bivalenzpunkt [°C] zur Meldung „Übergabe begrenzt".</summary>
        internal double ErsterBivalenzpunktC { get; init; } = double.NaN;
    }

    /// <summary>
    /// <b>Die Dialogprüfung und die Lesewerte der Gruppe „Bivalenz und Übergabe"</b> (Fachkonzept Übergabegrenze 6.2,
    /// Umsetzungskonzept 6.2–6.4, UB‑E2): Einbindung (Werte, Vorbelegung), Gerätegrenzen nach Kältemittel und die weichen
    /// Sperren. Die Oberfläche zeigt nur; gerechnet wird hier, ohne Datenbank.
    /// </summary>
    internal static class Bivalenzpruefung
    {
        /// <summary>Einbindung direkt (ohne Puffer).</summary>
        internal const string EINBINDUNG_DIREKT = "DIREKT";

        /// <summary>Einbindung über einen Heizungspuffer (Entladeseite als Näherung).</summary>
        internal const string EINBINDUNG_PUFFER = "PUFFER";

        /// <summary>Einbindung über eine hydraulische Weiche.</summary>
        internal const string EINBINDUNG_WEICHE = "WEICHE";

        /// <summary>Die drei Werte der Spalte <c>Einbindung</c> in der Reihenfolge der Klappliste.</summary>
        internal static readonly IReadOnlyList<string> Einbindungen = new[] { EINBINDUNG_DIREKT, EINBINDUNG_PUFFER, EINBINDUNG_WEICHE };

        /// <summary>Der gespeicherte Wert zu einer Eingabe: einer der drei Codes (Groß-/Kleinschreibung gleichgültig), sonst <c>null</c>.</summary>
        internal static string? EinbindungNormiert(string? einbindung)
        {
            if (string.IsNullOrWhiteSpace(einbindung)) return null;
            string c = einbindung.Trim();
            return Einbindungen.FirstOrDefault(e => string.Equals(e, c, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Die Vorbelegung der Einbindung (Umsetzungskonzept 6.2): Eine <b>neue</b> Anlage bekommt <c>PUFFER</c>, wenn ihr ein
        /// Heizungspuffer zugeordnet ist, sonst <c>DIREKT</c>; eine Bestandsanlage bleibt leer (U‑1: die Übergabegrenze ruht).
        /// </summary>
        internal static string? EinbindungVorbelegung(bool neueAnlage, bool mitHeizungspuffer)
            => !neueAnlage ? null : mitHeizungspuffer ? EINBINDUNG_PUFFER : EINBINDUNG_DIREKT;

        /// <summary>Ist der Vorwärmbetrieb bei dieser Betriebsart wählbar? Bei alternativ nicht (Fachkonzept 6.2).</summary>
        internal static bool VorwaermbetriebWaehlbar(Bivalenzbetriebsart art) => art != Bivalenzbetriebsart.Alternativ;

        /// <summary>
        /// Die Gerätegrenzen nach Kältemittel ohne gepflegte Gerätespalten (<see cref="Geraetegrenzen"/> mit leeren Spalten):
        /// Herkunft „Vorgabe nach Kältemittel" für eine Klasse der Tafel 6.3, sonst „Vorgabe". Der höchste Rücklauf ist
        /// min(Grenze der Klasse, θ_WP,max − σ_min), ohne Grenze der Klasse abgeleitet (NaN ohne Höchstvorlauf).
        /// </summary>
        internal static Geraetegrenzwerte Grenzen(string? kaeltemittel, double hoechstvorlaufC)
            => Geraetegrenzen.Bilden(Geraetespalten.NurKaeltemittel(kaeltemittel), hoechstvorlaufC).Werte();

        /// <summary>
        /// Die Gerätegrenzen aus den gepflegten Gerätespalten (UB‑E3): ein gepflegter Wert trägt die Herkunft „Katalog",
        /// ein leerer die Vorgabe der Kältemittelklasse.
        /// </summary>
        internal static Geraetegrenzwerte GrenzenAusSpalten(Geraetespalten? spalten, double hoechstvorlaufC)
            => Geraetegrenzen.Bilden(spalten, hoechstvorlaufC).Werte();

        /// <summary>
        /// Die weichen Sperren (Stufe Warnung) und Hinweise in fester Reihenfolge. Speichern bleibt immer möglich; eine Regel,
        /// deren Eingang fehlt (NaN, <c>null</c>), ruht.
        /// </summary>
        internal static IReadOnlyList<Bivalenzbefund> Pruefen(Bivalenzpruefeingang e)
        {
            if (e == null) throw new ArgumentNullException(nameof(e));
            var befunde = new List<Bivalenzbefund>();
            Geraetegrenzwerte? g = e.Grenzen;

            if (g != null && g.SpreizungMinK >= g.SpreizungMaxK)
                befunde.Add(new Bivalenzbefund(Bivalenzbefundart.Spreizung, false, g.SpreizungMinK, g.SpreizungMaxK));

            if (!double.IsNaN(e.HoechstvorlaufC) && !double.IsNaN(e.FlaechenVorlaufC) && e.HoechstvorlaufC < e.FlaechenVorlaufC)
                befunde.Add(new Bivalenzbefund(Bivalenzbefundart.Hoechstvorlauf, false, e.HoechstvorlaufC, e.FlaechenVorlaufC));

            // Die Wärmepumpe fährt höchstens θ_WP,max: maßgebend ist der Rücklauf an der Übergabegrenze, nicht der
            // Auslegungsrücklauf. Sperre genau dann, wenn θ_R,UE über θ_R,grenz liegt.
            if (g != null && !double.IsNaN(g.RuecklaufMaxC) && !double.IsNaN(e.RuecklaufUebergabeC)
                && e.RuecklaufUebergabeC > g.RuecklaufMaxC)
                befunde.Add(new Bivalenzbefund(Bivalenzbefundart.RuecklaufNie, false, g.RuecklaufMaxC, e.RuecklaufUebergabeC));

            bool vorwaerm = e.Bivalent && e.Vorwaermbetrieb && VorwaermbetriebWaehlbar(e.Betriebsart);
            if (vorwaerm && e.Kaskade != null)
            {
                int wp = Platz(e.Kaskade, DbWerte.ERZEUGER_WAERMEPUMPE);
                int kessel = Platz(e.Kaskade, DbWerte.ERZEUGER_HEIZKESSEL);
                if (kessel < 0 && !e.Heizstab)
                    befunde.Add(new Bivalenzbefund(Bivalenzbefundart.VorwaermOhneKessel, false, double.NaN, double.NaN));
                if (kessel >= 0 && wp > kessel)
                    befunde.Add(new Bivalenzbefund(Bivalenzbefundart.Kaskade, false, wp + 1, kessel + 1));
            }

            if (e.Bivalent && !double.IsNaN(e.HybridAnteil) && !double.IsNaN(e.HybridMindestanteil)
                && e.HybridAnteil < e.HybridMindestanteil)
                befunde.Add(new Bivalenzbefund(Bivalenzbefundart.Gmodg, true, e.HybridAnteil, e.HybridMindestanteil));

            if (e.UebergabeBegrenzt && !double.IsNaN(e.ErsterBivalenzpunktC))
                befunde.Add(new Bivalenzbefund(Bivalenzbefundart.UebergabeBegrenzt, true, e.ErsterBivalenzpunktC, double.NaN));

            return befunde;
        }

        /// <summary>Der erste Platz des Erzeugers in <c>Tool_1</c> … <c>Tool_4</c>; −1 = nicht in der Kaskade.</summary>
        private static int Platz(IReadOnlyList<string?> kaskade, string erzeuger)
        {
            for (int i = 0; i < 4 && i < kaskade.Count; i++)
                if (string.Equals(kaskade[i], erzeuger, StringComparison.Ordinal)) return i;
            return -1;
        }
    }
}
