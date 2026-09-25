using System;
using System.Collections.Generic;
using System.Globalization;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// <b>Eine Zone eines Gebäudes als Eingang des Rechenkerns</b> (Stufe G3; Mehrzonenkonzept
    /// 1.2, 4.3; Softwarearchitektur 2.9) — Kennung, Bezeichnung und die Bauteile der Zone als
    /// <see cref="BauteilEingang"/>. Ein Kern-Datensatz ohne Datenbank: Gefüllt wird er vom
    /// Leser der Zonentabellen (<see cref="GebaeudeZonenanschluss"/>) oder von
    /// <see cref="GebaeudeZonenuebernahme.AlsEineZone"/>; er hängt an
    /// <see cref="ProjektGebaeudeModel.Zonen"/>.
    ///
    /// <para><b>Der Umschalter folgt der Datenlage</b> (Entscheid A14/E27): Ein Gebäude ohne
    /// Zone rechnet den Klassenweg, bitgleich wie ohne diesen Typ; mit genau einer Zone rechnet
    /// es den Bauteilweg; mehr als eine Zone ist in G3 ein benannter Fehler
    /// (<see cref="GebaeudeModellFehler.MehrereZonen"/>) — mehrere Zonen rechnet EPOS mit
    /// Stufe G6. Die Regel steht an einer Stelle: <see cref="EineZone"/>.</para>
    ///
    /// <para><b>Was G3 von der Zone liest: die Bauteile und die Nutzfläche</b> (Anwenderentscheid
    /// vom 25.09.2026 „Hochrechnen“). Die Nutzfläche der Zone (<c>Tab_Zone.Nutzflaeche</c>, NULL =
    /// Nutzfläche des Gebäudes) ist die Bezugsfläche A_f des Bauteilwegs; über den
    /// <b>Flächenschlüssel</b> (Mehrzonenkonzept 4.2: NULL = anteilig aus dem Gebäude) folgen ihr
    /// die flächenbezogenen Größen des Gebäudes — Luftvolumen, Speichermasse der Bauweise, innere
    /// Gewinne, f_IW·A_f (<see cref="GebaeudeModellEingang"/>) und die Bezugsfläche der Fassade.
    /// Alle übrigen Parameterspalten einer Zone (Raumhöhe, Volumen, Sollwerte, Luftwechsel,
    /// Leistungsgrenzen, Kühl- und Übergabeeingaben, eigene innere Gewinne und Bewohner) liest G3
    /// nicht; es gelten die Werte der Gebäudezeile. Sie gehen mit Stufe G6 ein.</para>
    ///
    /// <para><b>Eine unlesbare Zone</b> (<see cref="Unlesbar"/>) trägt statt ihrer Bauteile den
    /// benannten Fehler ihrer Abbildung (<see cref="GebaeudeZonenabbildung"/>): Das Lesen bleibt
    /// heil, und erst der Lauf, der die Zone rechnet, bricht für dieses Gebäude mit diesem Grund
    /// ab (<see cref="EineZone"/>) — kein stilles Weglassen der Zone.</para>
    ///
    /// <para>Unveränderlich; geprüft wird im Bauteilweg, nicht beim Anlegen.</para>
    /// </summary>
    internal sealed class GebaeudeZonensatz
    {
        /// <param name="zonenId">Die Kennung der Zone (<c>Tab_Zone.ID</c>); 0 = noch nicht gespeichert.</param>
        /// <param name="bezeichnung">Die Bezeichnung für Meldungen.</param>
        /// <param name="bauteile">Die Bauteile der Zone; <c>null</c> = keine.</param>
        /// <param name="nutzflaecheM2">Die Nutzfläche der Zone [m²]; NaN = die des Gebäudes.</param>
        internal GebaeudeZonensatz(int zonenId, string bezeichnung, IReadOnlyList<BauteilEingang> bauteile,
                                   double nutzflaecheM2 = double.NaN)
        {
            ZonenId = zonenId;
            Bezeichnung = bezeichnung ?? "";
            Bauteile = bauteile ?? Array.Empty<BauteilEingang>();
            Nutzflaeche_M2 = nutzflaecheM2;
        }

        /// <summary>
        /// Die Nutzfläche der Zone [m²] (<c>Tab_Zone.Nutzflaeche</c>); NaN = die Nutzfläche des
        /// Gebäudes. Geprüft wird im Eingangsbauer (<see cref="GebaeudeModellEingang"/>), nicht hier.
        /// </summary>
        internal double Nutzflaeche_M2 { get; }

        /// <summary>
        /// Die Bezugsfläche dieser Zone für das Gebäude <paramref name="g"/> [m²]: die eigene
        /// Nutzfläche, ohne Angabe (NaN) die des Gebäudes.
        /// </summary>
        internal double Bezugsflaeche(ProjektGebaeudeModel g)
            => double.IsNaN(Nutzflaeche_M2) ? (g?.Nutzflaeche ?? double.NaN) : Nutzflaeche_M2;

        /// <summary>Die Kennung der Zone (<c>Tab_Zone.ID</c>); 0 = noch nicht gespeichert.</summary>
        internal int ZonenId { get; }

        /// <summary>Die Bezeichnung der Zone für Meldungen.</summary>
        internal string Bezeichnung { get; }

        /// <summary>Die Bauteile der Zone, in der Reihenfolge des Lesers.</summary>
        internal IReadOnlyList<BauteilEingang> Bauteile { get; }

        /// <summary>Der Grund, aus dem sich die Zeilen der Zone nicht abbilden ließen; <c>null</c> = abgebildet.</summary>
        internal GebaeudeModellFehler? Lesefehlergrund { get; private init; }

        /// <summary>Die Meldung dazu (ohne das Gebäude); <c>null</c> = abgebildet.</summary>
        internal string Lesefehler { get; private init; }

        /// <summary>
        /// Eine Zone, deren Zeilen sich nicht abbilden ließen — ohne Bauteile, mit dem benannten
        /// Fehler der Abbildung. Sie zählt als Zone (<see cref="HatZonen"/>); <see cref="EineZone"/>
        /// wirft ihren Fehler, sobald der Lauf sie rechnen soll.
        /// </summary>
        internal static GebaeudeZonensatz Unlesbar(int zonenId, string bezeichnung, GebaeudeModellFehler grund, string meldung)
            => new GebaeudeZonensatz(zonenId, bezeichnung, null) { Lesefehlergrund = grund, Lesefehler = meldung ?? "" };

        /// <summary>
        /// <b>Die Regel des Umschalters</b> (A14/E27): keine Zone (<c>null</c> oder leer) →
        /// <c>null</c>, der Klassenweg rechnet; genau eine → diese Zone, der Bauteilweg rechnet;
        /// mehr als eine → benannter Fehler. Eine unlesbare Zone (<see cref="Unlesbar"/>) wirft
        /// hier den Fehler ihrer Abbildung, mit dem Gebäude davor.
        /// </summary>
        /// <param name="zonen">Die Zonen des Gebäudes (<see cref="ProjektGebaeudeModel.Zonen"/>).</param>
        /// <param name="wer">Die Bezeichnung des Gebäudes für die Meldung.</param>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.MehrereZonen"/> oder der
        /// Grund der Abbildung einer unlesbaren Zone.</exception>
        internal static GebaeudeZonensatz EineZone(IReadOnlyList<GebaeudeZonensatz> zonen, string wer)
        {
            if (zonen == null || zonen.Count == 0) return null;
            if (zonen.Count > 1)
                throw new GebaeudeModellException(GebaeudeModellFehler.MehrereZonen,
                    string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_G3_MEHRERE_ZONEN,
                                  wer, zonen.Count.ToString(CultureInfo.CurrentCulture)));
            GebaeudeZonensatz zone = zonen[0] ?? throw new ArgumentException("Die Zonenliste enthält einen leeren Eintrag.", nameof(zonen));
            if (zone.Lesefehler != null)
                throw new GebaeudeModellException(zone.Lesefehlergrund ?? GebaeudeModellFehler.BauteilUngueltig,
                                                  wer + ", " + zone.Lesefehler);
            return zone;
        }

        /// <summary>Hat das Gebäude mindestens eine Zone — rechnet es also nicht den Klassenweg?</summary>
        internal static bool HatZonen(ProjektGebaeudeModel g) => g?.Zonen != null && g.Zonen.Count > 0;
    }
}
