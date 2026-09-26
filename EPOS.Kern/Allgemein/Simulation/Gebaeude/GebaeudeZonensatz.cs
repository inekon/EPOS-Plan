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
    /// es den Bauteilweg; ab zwei Zonen rechnet die Zonenschleife jede Zone für sich (Stufe G6b,
    /// <see cref="ZonenEingang"/>), bis zur <see cref="GebaeudeZonenregeln.PFLEGEGRENZE"/>. Der
    /// Einzonenweg nimmt höchstens eine Zone; die Regel steht an einer Stelle:
    /// <see cref="EineZone"/>.</para>
    ///
    /// <para><b>Die Bauteile und die Nutzfläche</b> (Anwenderentscheid
    /// vom 25.09.2026 „Hochrechnen“). Die Nutzfläche der Zone (<c>Tab_Zone.Nutzflaeche</c>, NULL =
    /// Nutzfläche des Gebäudes) ist die Bezugsfläche A_f des Bauteilwegs; über den
    /// <b>Flächenschlüssel</b> (Mehrzonenkonzept 4.2: NULL = anteilig aus dem Gebäude) folgen ihr
    /// die flächenbezogenen Größen des Gebäudes — Luftvolumen, Speichermasse der Bauweise, innere
    /// Gewinne, f_IW·A_f (<see cref="GebaeudeModellEingang"/>) und die Bezugsfläche der Fassade.</para>
    ///
    /// <para><b>Die übrigen Spalten der Zone</b> (<see cref="Eingaben"/>: Raumhöhe, Volumen,
    /// Sollwerte, obere Raumtemperatur, Strahlungsanteil, Heizleistungsgrenze, Luftwechsel, eigene
    /// innere Gewinne, „beheizt") gehen über die Vorgabenkaskade (<see cref="Zonenvorgaben"/>) in den
    /// Lauf — nach Anwenderentscheid A5 (a) für jede Zahl der Zonen, auch für genau eine (Stufe G6b,
    /// Welle W3). Eine Zone ohne eigenen Wert rechnet bitgleich wie mit dem Wert des Gebäudes. Die
    /// Kühl- und Übergabespalten der Zone bleiben ungelesen (Anwenderentscheid A4 (a)).</para>
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
        /// <param name="eingaben">Die Spalten der Zone für die Vorgabenkaskade (Stufe G6b); <c>null</c> = keine
        /// über die Nutzfläche hinaus — jeder andere Wert ist der des Gebäudes.</param>
        /// <param name="rang">Die Reihenfolge der Zone im Gebäude (<c>Tab_Zone.Rang</c>); die Zonenschleife
        /// rechnet nach Rang, dann nach Kennung.</param>
        internal GebaeudeZonensatz(int zonenId, string bezeichnung, IReadOnlyList<BauteilEingang> bauteile,
                                   double nutzflaecheM2 = double.NaN, Zoneneingaben eingaben = null, int rang = 0)
        {
            ZonenId = zonenId;
            Bezeichnung = bezeichnung ?? "";
            Bauteile = bauteile ?? Array.Empty<BauteilEingang>();
            Nutzflaeche_M2 = nutzflaecheM2;
            Eingaben = eingaben;
            Rang = rang;
        }

        /// <summary>
        /// Die Spalten der Zone für die Vorgabenkaskade (<see cref="Zonenvorgaben"/>; Stufe G6b,
        /// Anwenderentscheid A5 (a): auch bei genau einer Zone); <c>null</c> = keine — dann gelten
        /// außer der Nutzfläche alle Werte des Gebäudes.
        /// </summary>
        internal Zoneneingaben Eingaben { get; }

        /// <summary>Die Reihenfolge der Zone im Gebäude (<c>Tab_Zone.Rang</c>).</summary>
        internal int Rang { get; }

        /// <summary>Wird die Zone beheizt? Ohne Eingaben ja (Festlegung 2: <c>IstBeheizt = 0</c> heißt frei schwingend).</summary>
        internal bool IstBeheizt => Eingaben?.IstBeheizt ?? true;

        /// <summary>
        /// Die Eingaben der Kaskade: die gelesenen, sonst allein die Nutzfläche des Satzes (NaN = keine).
        /// </summary>
        internal Zoneneingaben EingabenOderNutzflaeche()
            => Eingaben ?? new Zoneneingaben(Nutzflaeche: double.IsNaN(Nutzflaeche_M2) ? (double?)null : Nutzflaeche_M2);

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
        /// <b>Die Regel des Einzonenwegs</b> (A14/E27): keine Zone (<c>null</c> oder leer) →
        /// <c>null</c>, der Klassenweg rechnet; genau eine → diese Zone, der Bauteilweg rechnet; ab
        /// zwei Zonen → benannter Fehler, keine stille Auswahl einer Zone. Mehrere Zonen rechnet die
        /// Zonenschleife (<see cref="Vdi6007Rechenweg"/>, Stufe G6b) bis zur
        /// <see cref="GebaeudeZonenregeln.PFLEGEGRENZE"/>; hierher kommen sie nur über eine Auskunft des
        /// Einzonenwegs (Übergabe- und Kühlherleitung) oder jenseits der Grenze — der Fehler nennt
        /// dann die Grenze. Eine unlesbare Zone (<see cref="Unlesbar"/>) wirft hier den Fehler ihrer
        /// Abbildung, mit dem Gebäude davor.
        /// </summary>
        /// <param name="zonen">Die Zonen des Gebäudes (<see cref="ProjektGebaeudeModel.Zonen"/>).</param>
        /// <param name="wer">Die Bezeichnung des Gebäudes für die Meldung.</param>
        /// <exception cref="GebaeudeModellException"><see cref="GebaeudeModellFehler.MehrereZonen"/> oder der
        /// Grund der Abbildung einer unlesbaren Zone.</exception>
        internal static GebaeudeZonensatz EineZone(IReadOnlyList<GebaeudeZonensatz> zonen, string wer)
        {
            if (zonen == null || zonen.Count == 0) return null;
            if (zonen.Count >= 2)
                throw new GebaeudeModellException(GebaeudeModellFehler.MehrereZonen,
                    GebaeudeZonenregeln.Rechenbar(zonen.Count)
                        ? string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_G6_MEHRZONEN_EINZELWEG,
                                        wer, zonen.Count.ToString(CultureInfo.CurrentCulture))
                        : string.Format(CultureInfo.CurrentCulture, MyResource.Resource.SIMENG_G3_MEHRERE_ZONEN,
                                        wer, zonen.Count.ToString(CultureInfo.CurrentCulture),
                                        GebaeudeZonenregeln.PFLEGEGRENZE.ToString(CultureInfo.CurrentCulture)));
            GebaeudeZonensatz zone = zonen[0] ?? throw new ArgumentException("Die Zonenliste enthält einen leeren Eintrag.", nameof(zonen));
            if (zone.Lesefehler != null)
                throw new GebaeudeModellException(zone.Lesefehlergrund ?? GebaeudeModellFehler.BauteilUngueltig,
                                                  wer + ", " + zone.Lesefehler);
            return zone;
        }

        /// <summary>
        /// Die Bezugsfläche eines Gebäudes mit Zonen [m²] (Stufe G6b): bei genau einer Zone deren
        /// Bezugsfläche (<see cref="Bezugsflaeche(ProjektGebaeudeModel)"/> der Zone, wie in G3), ab zwei
        /// Zonen die Summe der beheizten — eine unbeheizte Zone (Keller, Treppenhaus) ist keine
        /// Wohn- oder Nutzfläche.
        /// </summary>
        internal static double GebaeudeBezugsflaeche(ProjektGebaeudeModel g)
        {
            if (g?.Zonen == null || g.Zonen.Count == 0) return double.NaN;
            if (g.Zonen.Count == 1) return g.Zonen[0].Bezugsflaeche(g);
            double summe = 0.0;
            foreach (GebaeudeZonensatz z in g.Zonen)
                if (z != null && z.IstBeheizt) summe += z.Bezugsflaeche(g);
            return summe;
        }

        /// <summary>Hat das Gebäude mindestens eine Zone — rechnet es also nicht den Klassenweg?</summary>
        internal static bool HatZonen(ProjektGebaeudeModel g) => g?.Zonen != null && g.Zonen.Count > 0;
    }
}
