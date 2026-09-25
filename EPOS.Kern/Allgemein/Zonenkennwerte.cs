using System;
using System.Collections.Generic;
using System.Linq;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Ein Bauteil einer Zone, wie <see cref="Zonenkennwerte"/> es liest: Bauteilart und
    /// Randbedingung als Persistenzwerte (<c>DbWerte.BAUTEILART_*</c>, <c>DbWerte.RANDBEDINGUNG_*</c>;
    /// <c>null</c> = die Vorgabe nach Bauteilart), die Fläche [m²], der wirksame U-Wert [W/(m²K)]
    /// (eingetragen, sonst der des Aufbaus; <c>null</c> = nicht bestimmbar) und ψ·L [W/K]
    /// (<c>null</c> = keine Wärmebrücke).
    /// </summary>
    public readonly record struct Zonenbauteil(string Bauteilart, string Randbedingung, double Flaeche,
                                               double? UWert, double? PsiL);

    /// <summary>
    /// <b>Die Kennwerte EINER Zone</b> (Gebäudesimulation Stufe G6a) — die EINE Formel, aus der die
    /// Zonenliste des Gebäudedialogs, die abgeleitete Hülle, die KI-Sicht und die Zonentabelle des
    /// Berichts ihre Zahlen nehmen. Eine reine, plattformfreie Rechnung ohne Datenbank neben
    /// <see cref="Gebaeudehuellbilanz"/>, auf der sie aufsetzt.
    ///
    /// <list type="bullet">
    /// <item><b>Fläche</b>: die Nutzfläche der Zone; <c>null</c> heißt „die Nutzfläche des Gebäudes"
    /// (<see cref="FlaecheVomGebaeude"/>) — ab zwei Zonen ist sie Pflicht
    /// (<see cref="GebaeudeZonenCtrl.Pruefen(IList{ZoneModel})"/>).</item>
    /// <item><b>Volumen</b>: das der Zone, sonst Fläche × Raumhöhe der Zone bzw. des Gebäudes —
    /// dann als abgeleitet gekennzeichnet (<see cref="VolumenAbgeleitet"/>).</item>
    /// <item><b>H_T</b> = Σ U·A der Transmissionsgruppen
    /// (<see cref="Gebaeudehuellbilanz.Zonenzeilen"/>, Summenregel Mehrzonenkonzept 4.3) + Σ ψ·L —
    /// ungewichtet wie im Stundenmodell; U aus dem Aufbau, wo keiner eingetragen ist.</item>
    /// <item><b>H_ve</b> = n · A · H · 0,34 Wh/(m³K) nach der Regel des Laufs: A die Fläche der Zone,
    /// H die Raumhöhe des GEBÄUDES (<c>GebaeudeModellEingang</c> liest die Raumhöhe der Zone noch
    /// nicht, sie geht mit Stufe G6b ein), n der Luftwechsel des Rechenwegs — auf dem VDI-Weg
    /// <see cref="Gebaeudemodellvorgaben.WirksamerLuftwechsel(double?, double?, double?)"/>
    /// (<see cref="Luftwechsel"/>).</item>
    /// <item><b>Bauteile</b>: ihre Zahl, Innenbauteile eingeschlossen.</item>
    /// </list>
    ///
    /// <para>Die Summe über die Zonen (H_T, Fläche) gilt, solange keine Zone an eine Nachbarzone
    /// grenzt — Trennflächen kommen mit Stufe G6b.</para>
    /// </summary>
    /// <param name="Nutzflaeche">Die Fläche der Zone [m²]; <c>null</c>, wenn weder Zone noch Gebäude eine tragen.</param>
    /// <param name="FlaecheVomGebaeude">Gilt die Nutzfläche des Gebäudes (die Zone trägt keine eigene)?</param>
    /// <param name="Volumen">Das Luftvolumen [m³]; <c>null</c> = nicht bestimmbar.</param>
    /// <param name="VolumenAbgeleitet">Ist das Volumen aus Fläche × Raumhöhe gebildet (die Zone trägt keins)?</param>
    /// <param name="HT">Der Transmissionsleitwert H_T [W/K].</param>
    /// <param name="HVe">Der Lüftungsleitwert H_ve [W/K].</param>
    /// <param name="Bauteile">Die Zahl der Bauteile.</param>
    public sealed record Zonenkennwerte(double? Nutzflaeche, bool FlaecheVomGebaeude, double? Volumen,
                                        bool VolumenAbgeleitet, double HT, double HVe, int Bauteile)
    {
        /// <summary>
        /// Bildet die Kennwerte einer Zone aus ihren Werten und denen des Gebäudes.
        /// </summary>
        /// <param name="nutzflaecheZone">Die Nutzfläche der Zone [m²]; <c>null</c> = die des Gebäudes.</param>
        /// <param name="raumhoeheZone">Die Raumhöhe der Zone [m]; <c>null</c> = die des Gebäudes.</param>
        /// <param name="volumenZone">Das Volumen der Zone [m³]; <c>null</c> = abgeleitet.</param>
        /// <param name="bauteile">Die Bauteile der Zone.</param>
        /// <param name="nutzflaecheGebaeude">Die Nutzfläche des Gebäudes [m²].</param>
        /// <param name="raumhoeheGebaeude">Die Raumhöhe des Gebäudes [m].</param>
        /// <param name="luftwechsel">Der Luftwechsel des Rechenwegs [1/h] (<see cref="Luftwechsel"/>);
        /// <c>null</c> zählt als 0.</param>
        public static Zonenkennwerte Bilden(double? nutzflaecheZone, double? raumhoeheZone, double? volumenZone,
                                            IEnumerable<Zonenbauteil> bauteile,
                                            double? nutzflaecheGebaeude, double? raumhoeheGebaeude, double? luftwechsel)
        {
            List<Zonenbauteil> liste = (bauteile ?? Enumerable.Empty<Zonenbauteil>()).ToList();

            double? flaeche = nutzflaecheZone ?? nutzflaecheGebaeude;
            bool vomGebaeude = !nutzflaecheZone.HasValue;

            double? hoehe = raumhoeheZone ?? raumhoeheGebaeude;
            double? volumen = volumenZone ?? (flaeche.HasValue && hoehe.HasValue ? flaeche.Value * hoehe.Value : (double?)null);
            bool abgeleitet = !volumenZone.HasValue;

            // H_T: dieselbe Gruppierung wie die abgeleitete Hülle, dazu Σ ψ·L in Listenfolge.
            IReadOnlyList<Huellzeile> zeilen = Gebaeudehuellbilanz.Zonenzeilen(
                liste.Select(b => (b.Bauteilart, b.Randbedingung, b.Flaeche, b.UWert)));
            double psi = 0.0;
            foreach (Zonenbauteil b in liste) psi += b.PsiL ?? 0.0;
            double ht = Gebaeudehuellbilanz.TransmissionWK(zeilen) + psi;

            // H_ve nach der Regel des Laufs: Fläche der Zone mal Raumhöhe des Gebäudes.
            double hve = Gebaeudehuellbilanz.LueftungWK(luftwechsel, flaeche, raumhoeheGebaeude);

            return new Zonenkennwerte(flaeche, vomGebaeude, volumen, abgeleitet, ht, hve, liste.Count);
        }

        /// <summary>
        /// Dasselbe für eine gespeicherte Zone (<see cref="ZoneModel"/>): Ein Bauteil ohne eigenen
        /// U-Wert nimmt den seines Aufbaus (<paramref name="uAufbau"/>, <c>null</c> = keiner bestimmbar).
        /// </summary>
        public static Zonenkennwerte Bilden(ZoneModel zone, Func<int, double?> uAufbau,
                                            double? nutzflaecheGebaeude, double? raumhoeheGebaeude, double? luftwechsel)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));
            IEnumerable<Zonenbauteil> bauteile = (zone.Bauteile ?? new List<BauteilModel>()).Where(b => b != null)
                .Select(b => new Zonenbauteil(b.Bauteilart, b.Randbedingung, b.Flaeche,
                                              b.U_Wert ?? (b.ID_Aufbau is int a && uAufbau != null ? uAufbau(a) : null),
                                              b.Psi_L));
            return Bilden(zone.Nutzflaeche, zone.Raumhoehe, zone.Volumen, bauteile,
                          nutzflaecheGebaeude, raumhoeheGebaeude, luftwechsel);
        }

        /// <summary>
        /// Der Luftwechsel, mit dem der Rechenweg <paramref name="modell"/> H_ve bildet [1/h]: auf dem
        /// VDI-Weg der wirksame (<see cref="Gebaeudemodellvorgaben.WirksamerLuftwechsel(double?, double?, double?)"/>),
        /// auf dem Tagesbilanz-Weg die Luftwechselrate — dieselbe Regel wie im Gebäudedialog.
        /// </summary>
        public static double? Luftwechsel(string modell, double? luftwechselrate, double? infiltration, double? nutzer)
            => Gebaeuderechenweg.IstVdi6007(modell)
                ? Gebaeudemodellvorgaben.WirksamerLuftwechsel(luftwechselrate, infiltration, nutzer)
                : luftwechselrate;
    }
}
