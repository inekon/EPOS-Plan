using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Übergabewerte des GEBÄUDES, aus denen eine Zone erbt (E63, AK1z) — so, wie sie in der
    /// Projektkopie stehen; <c>null</c> heißt „leer". Heizkreisschalter, Heizkurve, Vorlaufreihe und
    /// Auslegungsaußentemperatur bleiben allein am Gebäude und stehen deshalb nicht hier.
    /// </summary>
    internal sealed record Gebaeudeuebergabe(
        string Art, double? Exponent, double? AuslegungVorlaufC, double? AuslegungRuecklaufC,
        double? AuslegungRaumtemperaturC, double? ReglerProportionalbandK)
    {
        /// <summary>Die Übergabewerte eines Projektgebäudes, wie der Lauf es liest.</summary>
        internal static Gebaeudeuebergabe Aus(ProjektGebaeudeModel g)
        {
            if (g == null) throw new ArgumentNullException(nameof(g));
            return new Gebaeudeuebergabe(g.Uebergabe_Art, g.Uebergabe_Exponent, g.Auslegung_Vorlauf, g.Auslegung_Ruecklauf,
                                         g.Auslegung_Raumtemperatur, g.Regler_Proportionalband);
        }
    }

    /// <summary>
    /// Die wirksame Übergabe EINER Zone (E63) — Ergebnis von <see cref="Zonenuebergabevorgaben.Aufloesen"/>.
    /// Rechnet die Zone ideal (<see cref="Ideal"/>), sind alle Zahlen <c>NaN</c>.
    /// </summary>
    /// <param name="Art">Die wirksame Art (<c>DbWerte.UEBERGABE_*</c>); <c>null</c> = keine (ideal).</param>
    /// <param name="Ideal">Rechnet die Zone ideal (Art <c>IDEAL</c>, leer oder unbekannt)?</param>
    /// <param name="Exponent">Exponent der Übergabe [–].</param>
    /// <param name="AuslegungVorlaufC">Auslegungsvorlauf [°C].</param>
    /// <param name="AuslegungRuecklaufC">Auslegungsrücklauf [°C].</param>
    /// <param name="AuslegungRaumtemperaturC">Auslegungsraumtemperatur [°C].</param>
    /// <param name="ReglerProportionalbandK">Proportionalband des Reglers [K].</param>
    /// <param name="NennleistungW">Nennleistung am Auslegungspunkt [W], auf den Katalogbau umgerechnet.</param>
    /// <param name="NennleistungHerkunft"><see cref="Vorgabeherkunft.Zone"/> oder <see cref="Vorgabeherkunft.GebaeudeAnteilig"/>.</param>
    internal readonly record struct Zonenuebergabe(
        string Art, bool Ideal, double Exponent, double AuslegungVorlaufC, double AuslegungRuecklaufC,
        double AuslegungRaumtemperaturC, double ReglerProportionalbandK, double NennleistungW,
        Vorgabeherkunft NennleistungHerkunft);

    /// <summary>
    /// <b>Die Kaskade der Wärmeübergabe je Zone</b> (E63, AK1z) — als reine Funktion, ohne Datenbank und
    /// ohne Zustand. Die Regeln je Wert:
    /// <list type="bullet">
    /// <item><b>Art</b>: Zone, sonst Gebäude. <c>IDEAL</c> an der Zone heißt: diese Zone rechnet ideal, auch
    /// wenn das Gebäude gekoppelt ist.</item>
    /// <item><b>Exponent, Auslegungsvorlauf, -rücklauf</b>: Zone, sonst ausdrücklicher Gebäudewert, sonst
    /// Vorgabe der wirksamen ZONENart (<see cref="Waermeuebergabe"/>).</item>
    /// <item><b>Auslegungsraumtemperatur</b>: Zone, sonst ausdrücklicher Gebäudewert, sonst wirksame
    /// Tag-Raumsolltemperatur der Zone.</item>
    /// <item><b>Proportionalband</b>: Zone, sonst Gebäude, sonst
    /// <see cref="GebaeudeFestwerte.VORGABE_REGLER_PROPORTIONALBAND_K"/>.</item>
    /// <item><b>Nennleistung</b>: Zone [kW] → 1000·kW / Skalierung (wie am Gebäude), sonst
    /// Gebäudenennleistung [W] (ausdrücklich oder hergeleitet, schon umgerechnet) × Nutzflächenanteil
    /// der Zone an der Summe der beheizten Zonen (<see cref="FlaechenanteilBeheizt"/>).</item>
    /// </list>
    /// Die Prüfung der Bänder und Vorlauf &gt; Rücklauf &gt; Raum geschieht im Eingang, nicht hier.
    /// </summary>
    internal static class Zonenuebergabevorgaben
    {
        /// <summary>
        /// Der Anteil der Nutzfläche einer Zone an der Summe der Nutzflächen der BEHEIZTEN Zonen [–]. Eine
        /// unbeheizte Zone hat den Anteil 0; eine einzige beheizte Zone den Anteil 1, auch ohne eigene
        /// Nutzfläche. Ab zwei beheizten Zonen braucht jede ihre Nutzfläche (Zonenregel); fehlt sie
        /// dennoch oder ist die Summe nicht positiv, ist der Anteil <c>NaN</c>.
        /// </summary>
        /// <param name="zone">Die Zone, deren Anteil gesucht ist (Referenz aus <paramref name="alleZonen"/>).</param>
        /// <param name="alleZonen">Alle Zonen des Gebäudes.</param>
        internal static double FlaechenanteilBeheizt(Zoneneingaben zone, IReadOnlyList<Zoneneingaben> alleZonen)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (alleZonen == null) throw new ArgumentNullException(nameof(alleZonen));
            if (!zone.IstBeheizt) return 0.0;
            List<Zoneneingaben> beheizt = alleZonen.Where(z => z != null && z.IstBeheizt).ToList();
            if (beheizt.Count <= 1) return 1.0;
            if (beheizt.Any(z => !z.Nutzflaeche.HasValue)) return double.NaN;
            double summe = beheizt.Sum(z => z.Nutzflaeche.Value);
            return summe > 0.0 && zone.Nutzflaeche.HasValue ? zone.Nutzflaeche.Value / summe : double.NaN;
        }

        /// <summary>
        /// Löst die Übergabe EINER Zone auf (Regeln: Klassenkopf).
        /// </summary>
        /// <param name="zone">Die Eingaben der Zone (sieben Übergabefelder, <c>null</c> = leer).</param>
        /// <param name="gebaeude">Die Übergabewerte des Gebäudes.</param>
        /// <param name="sollTagZoneC">Die wirksame Tag-Raumsolltemperatur der Zone [°C] (aus <see cref="Zonenvorgaben"/>).</param>
        /// <param name="gebaeudeNennleistungW">Die Nennleistung des Gebäudes [W], ausdrücklich oder hergeleitet,
        /// schon auf den Katalogbau umgerechnet (wie <c>Uebergabekennwerte</c> des Gebäudes).</param>
        /// <param name="flaechenanteilBeheizt">Der Anteil aus <see cref="FlaechenanteilBeheizt"/>.</param>
        /// <param name="nennleistungSkalierung">Verhältnis wirkliches Gebäude : Katalogbau (E8), wie am Gebäude.</param>
        internal static Zonenuebergabe Aufloesen(Zoneneingaben zone, Gebaeudeuebergabe gebaeude, double sollTagZoneC,
                                                 double gebaeudeNennleistungW, double flaechenanteilBeheizt,
                                                 double nennleistungSkalierung = 1.0)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            if (gebaeude == null) throw new ArgumentNullException(nameof(gebaeude));

            string art = zone.UebergabeArt ?? gebaeude.Art;
            if (!Waermeuebergabe.ArtBekannt(art))
                return new Zonenuebergabe(art == DbWerte.UEBERGABE_IDEAL ? art : null, true, double.NaN, double.NaN,
                                          double.NaN, double.NaN, double.NaN, double.NaN, Vorgabeherkunft.Leer);

            double n = zone.UebergabeExponent ?? gebaeude.Exponent ?? Waermeuebergabe.VorgabeExponent(art);
            double vN = zone.AuslegungVorlaufC ?? gebaeude.AuslegungVorlaufC ?? Waermeuebergabe.VorgabeVorlaufC(art);
            double rN = zone.AuslegungRuecklaufC ?? gebaeude.AuslegungRuecklaufC ?? Waermeuebergabe.VorgabeRuecklaufC(art);
            double iN = zone.AuslegungRaumtemperaturC ?? gebaeude.AuslegungRaumtemperaturC ?? sollTagZoneC;
            double xp = zone.ReglerProportionalbandK ?? gebaeude.ReglerProportionalbandK
                        ?? GebaeudeFestwerte.VORGABE_REGLER_PROPORTIONALBAND_K;

            double phiN;
            Vorgabeherkunft herkunft;
            if (zone.UebergabeLeistungNennKw is double kw)
            {
                phiN = double.IsPositiveInfinity(kw) ? double.PositiveInfinity : 1000.0 * kw / nennleistungSkalierung;
                herkunft = Vorgabeherkunft.Zone;
            }
            else
            {
                phiN = gebaeudeNennleistungW * flaechenanteilBeheizt;
                herkunft = Vorgabeherkunft.GebaeudeAnteilig;
            }
            return new Zonenuebergabe(art, false, n, vN, rN, iN, xp, phiN, herkunft);
        }
    }
}
