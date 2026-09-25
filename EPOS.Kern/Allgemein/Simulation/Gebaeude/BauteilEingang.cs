using System;
using System.Collections.Generic;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Die Art eines Bauteils — die Kern-Aufzählung der neun Persistenzwerte AUSSENWAND, DACH,
    /// BODENPLATTE, FENSTER, TUER, INNENWAND, DECKE, VORHANGFASSADE, SONSTIGES (Stufe G3;
    /// Mehrzonenkonzept 4.2). Die Art legt die Vorgabe der Neigung fest und, ob das Bauteil
    /// transparent rechnet; die Gruppe im 2-K-Modell entscheidet die Randbedingung.
    /// </summary>
    internal enum Bauteilart
    {
        /// <summary>Außenwand (Vorgabeneigung 90°).</summary>
        Aussenwand,

        /// <summary>Dach (Vorgabeneigung 0°).</summary>
        Dach,

        /// <summary>Bodenplatte (Vorgabeneigung 180°).</summary>
        Bodenplatte,

        /// <summary>Fenster — Fensterzweig nach VDI 6007-1 Gl. (25)/(26) (Vorgabeneigung 90°).</summary>
        Fenster,

        /// <summary>Tür (Vorgabeneigung 90°).</summary>
        Tuer,

        /// <summary>Innenwand (Vorgabeneigung 90°).</summary>
        Innenwand,

        /// <summary>Decke (Vorgabeneigung 0°).</summary>
        Decke,

        /// <summary>Vorhangfassade — rechnet wie ein Fenster (Vorgabeneigung 90°).</summary>
        Vorhangfassade,

        /// <summary>Sonstiges Bauteil (Vorgabeneigung 90°, wie „Sonstiges" im Klassenweg senkrecht).</summary>
        Sonstiges,
    }

    /// <summary>Die Gruppe eines Bauteils im 2-K-Modell (VDI 6007-1, 6.4; Mehrzonenkonzept 2.2).</summary>
    internal enum Bauteilgruppe
    {
        /// <summary>Opakes Außenbauteil oder Bauteil zu einem unbeheizten Raum — einseitig reduziert (R₁, C₁,korr).</summary>
        Aussen,

        /// <summary>Fenster oder Vorhangfassade — Fensterzweig nach Gl. (25)/(26), nach den Wänden parallel.</summary>
        Fenster,

        /// <summary>Innenbauteil innerhalb der Zone — symmetrisch reduziert (R₁, C₁).</summary>
        Innen,
    }

    /// <summary>Welcher Weg die Ersatzgrößen einer Gruppe gebildet hat (Mehrzonenkonzept 3.6).</summary>
    internal enum Gruppenweg
    {
        /// <summary>Klassenweg: R₁ = 1/(h_ms·A), Kapazität aus der Bauweise (Rechenschritte A1, A4, A5).</summary>
        Klassenweg = 0,

        /// <summary>Bauteilweg: Reduktion der Schichtaufbauten nach Gl. (1)–(24).</summary>
        Bauteilweg,
    }

    /// <summary>
    /// <b>Ein Bauteil als Eingang des Bauteilwegs</b> (Stufe G3; Mehrzonenkonzept 3.1–3.6) —
    /// der Kern-Eingangstyp, aus dem <see cref="ErsatzparameterRC.AusBauteilweg(GebaeudeModellEingang, IReadOnlyList{BauteilEingang})"/>
    /// die Ersatzgrößen bildet. Ohne Datenbank; die Lesestelle der Tabellen baut eine spätere
    /// Welle.
    ///
    /// <para><b>NaN heißt „nicht angegeben"</b> und trägt je Feld eine benannte Regel:
    /// U-Wert NaN = aus den Schichten; Neigung NaN = nach Art (Dach/Decke 0°, Bodenplatte 180°,
    /// sonst 90°); Azimut NaN nur bei waagerechten Flächen (0°/180°) oder abseits der Außenluft;
    /// Rahmenanteil und Verschattung NaN = Vorgabe aus <see cref="GebaeudeFestwerte"/> — im Lauf
    /// füllt der Eingangsbauer g-Wert, Rahmenanteil und Verschattung eines Fensters vorher mit
    /// dem Gebäudewert (<see cref="MitGebaeudewerten"/>);
    /// α_kon,i und α_kon,a NaN = Vorgabe des Wegs (<see cref="ErsatzparameterRC.AusBauteilweg(GebaeudeModellEingang, IReadOnlyList{BauteilEingang})"/>).
    /// Der Azimut folgt der Datenbankkonvention <b>0° = Nord</b>, im Uhrzeigersinn (Ost 90°,
    /// Süd 180°, West 270°); die Umrechnung in die Konvention des Klimawegs steht in
    /// <see cref="GebaeudeKlimaweg.AzimutAusDatenbank"/>.</para>
    ///
    /// <para>Unveränderlich; geprüft wird im Bauteilweg (<see cref="Pruefen"/>), nicht beim
    /// Anlegen.</para>
    /// </summary>
    internal sealed class BauteilEingang
    {
        internal BauteilEingang(
            string bezeichnung,
            Bauteilart art,
            double flaeche_M2,
            Bauteilrand rand,
            double uWert_WM2K = double.NaN,
            IReadOnlyList<Schicht> schichten = null,
            double neigungGrad = double.NaN,
            double azimutGrad = double.NaN,
            double gWert = double.NaN,
            double rahmenanteil = double.NaN,
            double verschattungsfaktor = double.NaN,
            double psiL_WK = 0.0,
            double alphaKonInnen_WM2K = double.NaN,
            double alphaKonAussen_WM2K = double.NaN)
        {
            Bezeichnung = bezeichnung;
            Art = art;
            Flaeche_M2 = flaeche_M2;
            Rand = rand;
            UWert_WM2K = uWert_WM2K;
            Schichten = schichten ?? Array.Empty<Schicht>();
            NeigungGrad = neigungGrad;
            AzimutGrad = azimutGrad;
            GWert = gWert;
            Rahmenanteil = rahmenanteil;
            Verschattungsfaktor = verschattungsfaktor;
            PsiL_WK = psiL_WK;
            AlphaKonInnen_WM2K = alphaKonInnen_WM2K;
            AlphaKonAussen_WM2K = alphaKonAussen_WM2K;
        }

        /// <summary>Bezeichnung für Meldungen und die Herleitung.</summary>
        internal string Bezeichnung { get; }

        /// <summary>Art des Bauteils.</summary>
        internal Bauteilart Art { get; }

        /// <summary>Fläche [m²]: Außenbauteile brutto, Innenbauteile netto, Fenster mit Rahmen (Mehrzonenkonzept 2.2).</summary>
        internal double Flaeche_M2 { get; }

        /// <summary>Randbedingung der Außenseite.</summary>
        internal Bauteilrand Rand { get; }

        /// <summary>Eingetragener U-Wert [W/(m²K)]; NaN = aus den Schichten.</summary>
        internal double UWert_WM2K { get; }

        /// <summary>Schichten, beginnend mit der raumseitigen; leer = keine.</summary>
        internal IReadOnlyList<Schicht> Schichten { get; }

        /// <summary>Eingetragene Neigung [°] (0 = waagerecht nach oben, 90 = senkrecht, 180 = waagerecht nach unten); NaN = nach Art.</summary>
        internal double NeigungGrad { get; }

        /// <summary>Azimut [°] in der Datenbankkonvention 0° = Nord, im Uhrzeigersinn; NaN = keiner.</summary>
        internal double AzimutGrad { get; }

        /// <summary>Gesamtenergiedurchlassgrad g [–] (Fenster, Vorhangfassade).</summary>
        internal double GWert { get; }

        /// <summary>Rahmenanteil 1 − F_F [–]; NaN = Vorgabe.</summary>
        internal double Rahmenanteil { get; }

        /// <summary>Verschattungsfaktor F_S [–]; NaN = Vorgabe.</summary>
        internal double Verschattungsfaktor { get; }

        /// <summary>Wärmebrückenleitwert Σψ·L des Bauteils [W/K]; geht in den masselosen Zweig R_ext.</summary>
        internal double PsiL_WK { get; }

        /// <summary>Konvektiver Übergang raumseitig α_kon,i [W/(m²K)]; NaN = Vorgabe.</summary>
        internal double AlphaKonInnen_WM2K { get; }

        /// <summary>Konvektiver Übergang außen bzw. nachbarseitig α_kon,a [W/(m²K)]; NaN = Vorgabe.</summary>
        internal double AlphaKonAussen_WM2K { get; }

        /// <summary>Wahr für Fenster und Vorhangfassade: Fensterzweig nach Gl. (25)/(26).</summary>
        internal bool IstTransparent => Art == Bauteilart.Fenster || Art == Bauteilart.Vorhangfassade;

        /// <summary>Wahr, wenn das Bauteil einen Schichtaufbau trägt.</summary>
        internal bool HatSchichten => Schichten.Count > 0;

        /// <summary>Die Gruppe im 2-K-Modell: transparent → Fenster, innerhalb der Zone → Innen, sonst Außen.</summary>
        internal Bauteilgruppe Gruppe => IstTransparent ? Bauteilgruppe.Fenster
                                         : Rand == Bauteilrand.Innen ? Bauteilgruppe.Innen
                                         : Bauteilgruppe.Aussen;

        /// <summary>Die wirksame Neigung [°]: eingetragen, sonst nach Art (Dach/Decke 0°, Bodenplatte 180°, sonst 90°).</summary>
        internal double NeigungWirksamGrad => !double.IsNaN(NeigungGrad) ? NeigungGrad : VorgabeNeigung(Art);

        /// <summary>Der wirksame Rahmenanteil [–]: eingetragen, sonst <see cref="GebaeudeFestwerte.VORGABE_RAHMENANTEIL"/>.</summary>
        internal double RahmenanteilWirksam => double.IsNaN(Rahmenanteil) ? GebaeudeFestwerte.VORGABE_RAHMENANTEIL : Rahmenanteil;

        /// <summary>Der wirksame Verschattungsfaktor [–]: eingetragen, sonst <see cref="GebaeudeFestwerte.VORGABE_VERSCHATTUNGSFAKTOR"/>.</summary>
        internal double VerschattungsfaktorWirksam => double.IsNaN(Verschattungsfaktor) ? GebaeudeFestwerte.VORGABE_VERSCHATTUNGSFAKTOR : Verschattungsfaktor;

        /// <summary>
        /// Das Bauteil mit den Fensterwerten des Gebäudes für jedes nicht angegebene Feld
        /// (Stufe G3, Eingangsbauer): Trägt ein transparentes Bauteil g-Wert, Rahmenanteil oder
        /// Verschattung als NaN, gilt der Wert der Gebäudezeile — der seinerseits schon die
        /// Vorgabe trägt, wenn die Zeile leer ist. Ein opakes Bauteil und ein vollständig
        /// angegebenes Fenster kommen unverändert zurück (dieselbe Instanz).
        /// </summary>
        internal BauteilEingang MitGebaeudewerten(double gWert, double rahmenanteil, double verschattungsfaktor)
        {
            if (!IstTransparent || (!double.IsNaN(GWert) && !double.IsNaN(Rahmenanteil) && !double.IsNaN(Verschattungsfaktor)))
                return this;
            return new BauteilEingang(Bezeichnung, Art, Flaeche_M2, Rand, UWert_WM2K, Schichten, NeigungGrad, AzimutGrad,
                                      double.IsNaN(GWert) ? gWert : GWert,
                                      double.IsNaN(Rahmenanteil) ? rahmenanteil : Rahmenanteil,
                                      double.IsNaN(Verschattungsfaktor) ? verschattungsfaktor : Verschattungsfaktor,
                                      PsiL_WK, AlphaKonInnen_WM2K, AlphaKonAussen_WM2K);
        }

        /// <summary>Die Vorgabeneigung einer Art [°].</summary>
        internal static double VorgabeNeigung(Bauteilart art)
        {
            switch (art)
            {
                case Bauteilart.Dach:
                case Bauteilart.Decke:
                    return 0.0;
                case Bauteilart.Bodenplatte:
                    return 180.0;
                default:
                    return 90.0;
            }
        }

        /// <summary>
        /// Prüft das Bauteil hart (Mehrzonenkonzept 5.3): Fläche größer null; Neigung 0 … 180°;
        /// Azimut 0 … 360° oder, nur bei waagerechten Flächen und abseits der Außenluft, keiner;
        /// Randbedingung nicht „Nachbarzone" (G6b); U-Wert oder Schichten (ein Innenbauteil ohne
        /// Schichten trägt nur Fläche und braucht keinen); ein eingetragener U-Wert
        /// in <see cref="GebaeudeFestwerte.U_MIN"/> … <see cref="GebaeudeFestwerte.U_MAX"/>;
        /// Fenster ohne Schichten, mit U-Wert, 0 &lt; g ≤ 1, an Außenluft oder unbeheiztem Raum;
        /// Rahmenanteil [0, 1), Verschattung (0, 1], α größer null, ψ·L nicht negativ; jede Schicht
        /// im Plausibilitätsband.
        /// </summary>
        /// <exception cref="GebaeudeModellException">mit dem benannten Grund.</exception>
        internal void Pruefen(string wer)
        {
            if (!(Flaeche_M2 > 0.0) || double.IsInfinity(Flaeche_M2))
                throw Bereich(GebaeudeModellFehler.BauteilUngueltig, wer, "A", Flaeche_M2, "(0; ∞) m²");
            if (Rand == Bauteilrand.Zone)
                throw new GebaeudeModellException(GebaeudeModellFehler.RandbedingungNichtAbgebildet,
                    Bauteilreduktion.Format(MyResource.Resource.SIMENG_G3_RAND_ZONE, wer));

            double neigung = NeigungWirksamGrad;
            Bauteilreduktion.RichtungAusNeigung(neigung, wer);   // 0 … 180°, sonst benannt

            if (double.IsNaN(AzimutGrad))
            {
                bool waagerecht = neigung == 0.0 || neigung == 180.0;
                if (Rand == Bauteilrand.Aussenluft && !waagerecht)
                    throw new GebaeudeModellException(GebaeudeModellFehler.AzimutFehlt,
                        Bauteilreduktion.Format(MyResource.Resource.SIMENG_G3_AZIMUT_FEHLT, wer, Bauteilreduktion.Text(neigung)));
            }
            else if (!(AzimutGrad >= 0.0 && AzimutGrad <= 360.0))
                throw Bereich(GebaeudeModellFehler.BauteilUngueltig, wer, "Azimut", AzimutGrad, "0 … 360°");

            if (!double.IsNaN(AlphaKonInnen_WM2K) && !PositivEndlich(AlphaKonInnen_WM2K))
                throw Bereich(GebaeudeModellFehler.BauteilUngueltig, wer, "α_kon,i", AlphaKonInnen_WM2K, "(0; ∞) W/(m²K)");
            if (!double.IsNaN(AlphaKonAussen_WM2K) && !PositivEndlich(AlphaKonAussen_WM2K))
                throw Bereich(GebaeudeModellFehler.BauteilUngueltig, wer, "α_kon,a", AlphaKonAussen_WM2K, "(0; ∞) W/(m²K)");
            if (!(PsiL_WK >= 0.0) || double.IsInfinity(PsiL_WK))
                throw Bereich(GebaeudeModellFehler.BauteilUngueltig, wer, "ψ·L", PsiL_WK, "[0; ∞) W/K");

            if (!double.IsNaN(UWert_WM2K) && !(UWert_WM2K >= GebaeudeFestwerte.U_MIN && UWert_WM2K <= GebaeudeFestwerte.U_MAX))
                throw new GebaeudeModellException(GebaeudeModellFehler.UWertUnplausibel,
                    Bauteilreduktion.Format(MyResource.Resource.SIMENG_G3_UWERT_BEREICH, wer, Bauteilreduktion.Text(UWert_WM2K),
                                            Bauteilreduktion.Text(GebaeudeFestwerte.U_MIN), Bauteilreduktion.Text(GebaeudeFestwerte.U_MAX)));

            if (IstTransparent)
            {
                if (HatSchichten)
                    throw new GebaeudeModellException(GebaeudeModellFehler.BauteilUngueltig,
                        Bauteilreduktion.Format(MyResource.Resource.SIMENG_G3_TRANSPARENT_SCHICHTEN, wer));
                if (Rand != Bauteilrand.Aussenluft && Rand != Bauteilrand.Unbeheizt)
                    throw new GebaeudeModellException(GebaeudeModellFehler.BauteilUngueltig,
                        Bauteilreduktion.Format(MyResource.Resource.SIMENG_G3_TRANSPARENT_RAND, wer, Rand.ToString()));
                if (double.IsNaN(UWert_WM2K))
                    throw new GebaeudeModellException(GebaeudeModellFehler.BauteilUngueltig,
                        Bauteilreduktion.Format(MyResource.Resource.SIMENG_G3_UWERT_FEHLT, wer));
                if (!(GWert > 0.0 && GWert <= 1.0))
                    throw Bereich(GebaeudeModellFehler.GWertUnplausibel, wer, "g", GWert, "(0; 1]");
                double rahmen = RahmenanteilWirksam, schatten = VerschattungsfaktorWirksam;
                if (!(rahmen >= 0.0 && rahmen < 1.0))
                    throw Bereich(GebaeudeModellFehler.BauteilUngueltig, wer, "1 − F_F", rahmen, "[0; 1)");
                if (!(schatten > 0.0 && schatten <= 1.0))
                    throw Bereich(GebaeudeModellFehler.BauteilUngueltig, wer, "F_S", schatten, "(0; 1]");
                return;
            }

            // Ein Innenbauteil ohne Schichten trägt nur Fläche; sein U-Wert geht nirgends ein.
            if (HatSchichten) Bauteilreduktion.Pruefen(Schichten, wer);
            else if (double.IsNaN(UWert_WM2K) && Rand != Bauteilrand.Innen)
                throw new GebaeudeModellException(GebaeudeModellFehler.BauteilUngueltig,
                    Bauteilreduktion.Format(MyResource.Resource.SIMENG_G3_UWERT_FEHLT, wer));
        }

        private static bool PositivEndlich(double w) => w > 0.0 && !double.IsInfinity(w);

        internal static GebaeudeModellException Bereich(GebaeudeModellFehler grund, string wer, string groesse, double wert, string bereich)
            => new GebaeudeModellException(grund,
                Bauteilreduktion.Format(MyResource.Resource.SIMENG_G3_BAUTEIL_BEREICH, wer, groesse, Bauteilreduktion.Text(wert), bereich));
    }

    /// <summary>
    /// Die Gebäudegrößen, die der Bauteilweg außerhalb der Bauteile braucht (Stufe G3):
    /// Nutzfläche, Bauweise, Masseanteil außen und Innenflächenfaktor für eine Gruppe, die
    /// mangels Schichten den Klassenweg rechnet (Mehrzonenkonzept 3.6), und der Leitwert der
    /// Lüftung H_ve [W/K] für R_ext. NaN = nicht vorhanden; gebraucht wird eine Größe nur, wenn
    /// eine Gruppe sie verlangt, und dann benannt geprüft.
    /// </summary>
    internal readonly record struct BauteilwegGebaeude(
        string Bezeichnung,
        double Nutzflaeche_M2,
        double Bauweise_WhK,
        double MasseanteilAussen,
        double Innenflaechenfaktor,
        double Lueftungsleitwert_WK);

    /// <summary>
    /// Die Herleitung EINES Bauteils im Bauteilweg — was der Dialog und das Protokoll je
    /// Bauteil ausweisen: Gruppe, ob es masselos rechnet, die gewählte Bezugsperiode samt den
    /// Verhältnissen der Kriterien (10a)–(10d), R₁ und C₁ (bzw. C₁,korr) am Bauteil, den aus den
    /// Schichten gerechneten U-Wert, den in Gl. (27) wirksamen und den Hinweis, wenn ein
    /// eingetragener U-Wert um mehr als <see cref="GebaeudeFestwerte.UWERT_ABWEICHUNG_HINWEIS"/>
    /// vom gerechneten abweicht (Mehrzonenkonzept 3.4). NaN = trifft nicht zu.
    /// </summary>
    internal sealed record BauteilHerleitung(
        string Bezeichnung,
        Bauteilgruppe Gruppe,
        bool Masselos,
        double Bezugsperiode_d,
        double R1rel,
        double C1rel,
        double R1_KW,
        double C1_Jk,
        double UGerechnet_WM2K,
        double UWirksam_WM2K)
    {
        /// <summary>Wahr, wenn ein eingetragener U-Wert um mehr als 10 % vom gerechneten abweicht.</summary>
        internal bool UAbweichungHinweis =>
            !double.IsNaN(UGerechnet_WM2K) && !double.IsNaN(UWirksam_WM2K)
            && Math.Abs(UWirksam_WM2K / UGerechnet_WM2K - 1.0) > GebaeudeFestwerte.UWERT_ABWEICHUNG_HINWEIS;

        /// <summary>Der Hinweistext zur Abweichung, oder <c>null</c>, wenn keiner gilt.</summary>
        internal string Hinweis => !UAbweichungHinweis ? null
            : Bauteilreduktion.Format(MyResource.Resource.SIMENG_G3_UWERT_ABWEICHUNG, Bezeichnung,
                                      Bauteilreduktion.Text(UWirksam_WM2K), Bauteilreduktion.Text(UGerechnet_WM2K),
                                      Bauteilreduktion.Text(100.0 * (UWirksam_WM2K / UGerechnet_WM2K - 1.0)));
    }
}
