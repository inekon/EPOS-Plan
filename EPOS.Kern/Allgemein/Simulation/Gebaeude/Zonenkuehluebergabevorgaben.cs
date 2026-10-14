using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Werte der Kühlübergabe des GEBÄUDES, aus denen eine Zone erbt (Entwurf KK, KZ1, Festlegung 15) — so, wie
    /// sie in der Projektkopie stehen; <c>null</c> heißt „leer". Auslegungspunkt und Vorlaufgrenze stehen allein am
    /// Gebäude; die Zone trägt nur Art, Exponent und Nennleistung (Schritt 137).
    /// </summary>
    internal sealed record Gebaeudekuehluebergabe(
        string Art, double? Exponent, double? AuslegungVorlaufC, double? AuslegungRuecklaufC, double? AuslegungRaumtemperaturC)
    {
        /// <summary>Die Werte eines Projektgebäudes, wie der Lauf es liest.</summary>
        internal static Gebaeudekuehluebergabe Aus(ProjektGebaeudeModel g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            return new Gebaeudekuehluebergabe(g.Kuehl_Uebergabe_Art, g.Kuehl_Uebergabe_Exponent, g.Kuehl_Auslegung_Vorlauf,
                                              g.Kuehl_Auslegung_Ruecklauf, g.Kuehl_Auslegung_Raumtemperatur);
        }
    }

    /// <summary>
    /// Die wirksame Kühlübergabe EINER Zone (Entwurf KK, KZ1) — Ergebnis von <see cref="Zonenkuehluebergabevorgaben.Aufloesen"/>.
    /// Rechnet die Zone ideal (<see cref="Ideal"/>), sind alle Zahlen <c>NaN</c>.
    /// </summary>
    /// <param name="Art">Die wirksame Art (<c>DbWerte.KUEHLUEBERGABE_*</c>); <c>null</c> = keine (ideal).</param>
    /// <param name="Ideal">Rechnet die Zone ideal (Art <c>IDEAL</c>, leer oder unbekannt)?</param>
    /// <param name="Exponent">Exponent der Kühlübergabe [–].</param>
    /// <param name="AuslegungVorlaufC">Auslegungsvorlauf [°C] (Gebäude, sonst Vorgabe der Zonenart).</param>
    /// <param name="AuslegungRuecklaufC">Auslegungsrücklauf [°C] (Gebäude, sonst Vorgabe der Zonenart).</param>
    /// <param name="AuslegungRaumtemperaturC">Auslegungsraumtemperatur [°C] (Gebäude, sonst die der Zone).</param>
    /// <param name="NennleistungW">Nennleistung am Auslegungspunkt [W], sensibel.</param>
    /// <param name="NennleistungHerkunft"><see cref="Vorgabeherkunft.Zone"/> oder <see cref="Vorgabeherkunft.GebaeudeAnteilig"/>.</param>
    internal readonly record struct Zonenkuehluebergabe(
        string Art, bool Ideal, double Exponent, double AuslegungVorlaufC, double AuslegungRuecklaufC,
        double AuslegungRaumtemperaturC, double NennleistungW, Vorgabeherkunft NennleistungHerkunft);

    /// <summary>
    /// <b>Die Kaskade der Kühlübergabe je Zone</b> (Entwurf KK, KZ1; Festlegungen 15, 16; Spiegel von
    /// <see cref="Zonenuebergabevorgaben"/>) — als reine Funktion, ohne Datenbank und ohne Zustand:
    /// <list type="bullet">
    /// <item><b>Art</b>: Zone, sonst Gebäude. <c>IDEAL</c> an der Zone heißt: diese Zone rechnet ideal.</item>
    /// <item><b>Exponent</b>: Zone, sonst ausdrücklicher Gebäudewert, sonst Vorgabe der wirksamen ZONENart.</item>
    /// <item><b>Auslegungsvorlauf und -rücklauf</b>: ausdrücklicher Gebäudewert, sonst Vorgabe der wirksamen Zonenart —
    /// eine eigene Spalte an der Zone gibt es nicht (Festlegung 15).</item>
    /// <item><b>Auslegungsraumtemperatur</b>: ausdrücklicher Gebäudewert, sonst die der Zone (niedrigster wirksamer
    /// Kühlsollwert, Konzept 3.6).</item>
    /// <item><b>Nennleistung</b>: Zone [kW] → 1000·kW (Skalierungsfaktor 1 wie die Heizseite des Mehrzonenwegs), sonst
    /// Gebäudenennleistung [W] × Nutzflächenanteil der Zone an den GEKÜHLTEN Zonen (<see cref="FlaechenanteilGekuehlt"/>).</item>
    /// </list>
    /// Die Prüfung der Bänder und Vorlauf &lt; Rücklauf &lt; Raum geschieht im Eingang, nicht hier.
    /// </summary>
    internal static class Zonenkuehluebergabevorgaben
    {
        /// <summary>
        /// Der Anteil der Nutzfläche der Zone <paramref name="index"/> an der Summe der Nutzflächen der GEKÜHLTEN Zonen [–]
        /// — der Spiegel von <see cref="Zonenuebergabevorgaben.FlaechenanteilBeheizt"/>: eine ungekühlte Zone 0, eine einzige
        /// gekühlte Zone 1, sonst A_z / Σ A; fehlt eine Nutzfläche oder ist die Summe nicht positiv, <c>NaN</c>.
        /// </summary>
        internal static double FlaechenanteilGekuehlt(int index, IReadOnlyList<(bool Gekuehlt, double? Nutzflaeche)> zonen)
        {
            if (zonen == null) throw new ArgumentNullException(nameof(zonen));
            if (index < 0 || index >= zonen.Count) throw new ArgumentOutOfRangeException(nameof(index));
            if (!zonen[index].Gekuehlt) return 0.0;
            int zahl = 0;
            double summe = 0.0;
            bool luecke = false;
            foreach (var z in zonen)
            {
                if (!z.Gekuehlt) continue;
                zahl++;
                if (z.Nutzflaeche.HasValue) summe += z.Nutzflaeche.Value;
                else luecke = true;
            }
            if (zahl <= 1) return 1.0;
            if (luecke) return double.NaN;
            return summe > 0.0 && zonen[index].Nutzflaeche.HasValue ? zonen[index].Nutzflaeche.Value / summe : double.NaN;
        }

        /// <summary>Löst die Kühlübergabe EINER Zone auf (Regeln: Klassenkopf).</summary>
        /// <param name="zone">Die Eingaben der Zone (drei Kühlübergabefelder, <c>null</c> = leer).</param>
        /// <param name="gebaeude">Die Kühlübergabewerte des Gebäudes.</param>
        /// <param name="auslegungsraumZoneC">Die Auslegungsraumtemperatur der Kälte der Zone [°C].</param>
        /// <param name="gebaeudeNennleistungW">Die Nennleistung des Gebäudes [W], ausdrücklich oder hergeleitet.</param>
        /// <param name="flaechenanteilGekuehlt">Der Anteil aus <see cref="FlaechenanteilGekuehlt"/>.</param>
        internal static Zonenkuehluebergabe Aufloesen(Zoneneingaben zone, Gebaeudekuehluebergabe gebaeude, double auslegungsraumZoneC,
                                                      double gebaeudeNennleistungW, double flaechenanteilGekuehlt)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (gebaeude == null) throw new ArgumentNullException(nameof(gebaeude));

            string art = zone.KuehlUebergabeArt ?? gebaeude.Art;
            if (!Kuehluebergabe.ArtBekannt(art))
                return new Zonenkuehluebergabe(art == DbWerte.KUEHLUEBERGABE_IDEAL ? art : null, true, double.NaN, double.NaN,
                                               double.NaN, double.NaN, double.NaN, Vorgabeherkunft.Leer);

            double n = zone.KuehlUebergabeExponent ?? gebaeude.Exponent ?? Kuehluebergabe.VorgabeExponent(art);
            double vN = gebaeude.AuslegungVorlaufC ?? Kuehluebergabe.VorgabeVorlaufC(art);
            double rN = gebaeude.AuslegungRuecklaufC ?? Kuehluebergabe.VorgabeRuecklaufC(art);
            double iN = gebaeude.AuslegungRaumtemperaturC ?? auslegungsraumZoneC;

            double phiN;
            Vorgabeherkunft herkunft;
            if (zone.KuehlUebergabeLeistungNennKw is double kw)
            {
                phiN = double.IsPositiveInfinity(kw) ? double.PositiveInfinity : 1000.0 * kw;
                herkunft = Vorgabeherkunft.Zone;
            }
            else
            {
                phiN = gebaeudeNennleistungW * flaechenanteilGekuehlt;
                herkunft = Vorgabeherkunft.GebaeudeAnteilig;
            }
            return new Zonenkuehluebergabe(art, false, n, vN, rN, iN, phiN, herkunft);
        }
    }

    /// <summary>
    /// <b>Der Kühlkreis des Gebäudes im Mehrzonenweg</b> (Entwurf KK, KZ1; Festlegung 14; Spiegel von
    /// <see cref="Gebaeudeheizkreis"/>) — die Werte, die EINMAL am Gebäude gebildet und an alle kühlgekoppelten Zonen
    /// gereicht werden (<see cref="GebaeudeModellEingang.ZonenKuehlkopplungAufloesen"/>): Kühlübergabe des Gebäudes,
    /// gemeinsamer Kühlvorlauf samt Herkunft und Vorlaufgrenze, Auslegungskühllast als Summe der gekühlten Zonen. Daraus
    /// bildet <see cref="KuehlkreisErgebnis.Bilden(Gebaeudekuehlkreis, double[], double[], double[], double, double, double, double[], double)"/>
    /// das Kühlkreisergebnis des Gebäudes. Keine Rechengröße des Lösers — jede Zone rechnet mit ihren eigenen Kennwerten
    /// am gemeinsamen Vorlauf.
    /// </summary>
    internal sealed class Gebaeudekuehlkreis
    {
        /// <summary>Die Kühlübergabeart des Gebäudes (<c>DbWerte.KUEHLUEBERGABE_*</c>).</summary>
        internal string UebergabeArt { get; init; }
        /// <summary>Die Kennwerte der Kühlübergabe des Gebäudes, wie eingegeben (V &lt; R &lt; θ_i,N).</summary>
        internal Uebergabekennwerte Uebergabe { get; init; }
        /// <summary>Ist die Nennleistung des Gebäudes hergeleitet (Summe der Auslegungskühllasten der Zonen)?</summary>
        internal bool NennleistungHergeleitet { get; init; }
        /// <summary>Die Auslegungskühllast des Gebäudes [W]: Summe der gekühlten Zonen; NaN, wenn die Nennleistung eingetragen war.</summary>
        internal double AuslegungskuehllastW { get; init; } = double.NaN;
        /// <summary>Der Auslegungstag der Kühlung; −1, wenn die Nennleistung eingetragen war.</summary>
        internal int AuslegungstagKuehlung { get; init; } = -1;
        /// <summary>Das Proportionalband des Gebäudes [K].</summary>
        internal double ReglerbandK { get; init; }
        /// <summary>Woher der Kühlvorlauf kommt.</summary>
        internal Vorlaufquelle Vorlaufquelle { get; init; }
        /// <summary>Der feste Kühlvorlauf am Gebäude [°C] = max(Quelle, Vorlaufgrenze); NaN mit Kühlkurve (die Reihe <see cref="VorlaufC"/> gilt).</summary>
        internal double VorlaufFestC { get; set; } = double.NaN;
        /// <summary>Der Kühlvorlauf der Quelle vor dem Hochmischen [°C].</summary>
        internal double VorlaufQuelleC { get; init; } = double.NaN;
        /// <summary>Steht der Vorlauf an der Vorlaufgrenze, weil die Quelle kälter liefert?</summary>
        internal bool VorlaufGekappt { get; init; }
        /// <summary>Die Vorlaufgrenze des Gebäudes [°C]; NaN = keine.</summary>
        internal double VorlaufgrenzeC { get; init; } = double.NaN;
        /// <summary>Der gemeinsame Kühlvorlauf je Stunde [°C].</summary>
        internal double[] VorlaufC { get; init; }
        /// <summary>Der Strahlungsanteil der Kühlübergabe des Gebäudes [–] (Vorgabe der Art).</summary>
        internal double Strahlungsanteil { get; init; }

        // ------------------------------------------------------------------ KZ2: die Kühlkurve im Mehrzonenweg

        /// <summary>
        /// Die Kühlkurve des Kühlkreises (Entwurf KK 2.8, KZ2; Festlegung 14); <c>null</c> = fester Vorlauf (unter AK3 oder ohne <c>Kuehlkurve_Aktiv</c>). Mit ihr ist <see cref="VorlaufC"/> die Kurvenreihe am Bezug
        /// <see cref="SollwertC"/>.
        /// </summary>
        internal Kuehlkurve Kuehlkurve { get; set; }

        /// <summary>Der kälteste erreichbare Erzeugervorlauf als Untergrenze der Kurve [°C]; NaN = keine.</summary>
        internal double KurveErzeugerC { get; set; } = double.NaN;

        /// <summary>Der Raumeinfluss der Kühlkurve k_K des Gebäudes [K/K]; 0 = ohne Absenkung.</summary>
        internal double RaumeinflussKK { get; set; }

        /// <summary>
        /// Der Bezug der Kurvenreihe je Stunde [°C]: der niedrigste Kühlsollwert der gekühlten, gekoppelten Zonen in dieser
        /// Stunde; +∞, wenn keine kühlt (Kalender „aus“ oder Heiztagesart der Zonensperre).
        /// </summary>
        internal double[] SollwertC { get; set; }

        /// <summary>Steht der Kurvenvorlauf der Stunde an der Vorlaufgrenze (Festlegung 5)? Mit der Kurve, sonst <c>null</c>.</summary>
        internal bool[] VorlaufAnGrenze { get; set; }
    }
}
