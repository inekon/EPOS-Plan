using System;

namespace WindowsFormsApplication1
{
    /// <summary>
    /// Festwerte des Rechenwegs Kältebedarf ohne Gebäudemodell (Konzept Kältebedarf 4.2).
    /// </summary>
    public static class KaeltebedarfFestwerte
    {
        /// <summary>Untergrenze des EER einer dezentralen (Split-)Deckung — auch nach der linearen Extrapolation nie darunter.</summary>
        public const double EER_MIN = 1.0;

        /// <summary>Unter diesem Jahres-EER der dezentralen Deckung gibt der Lauf einen Hinweis (Konzept 4.4).</summary>
        public const double JAHRES_EER_HINWEIS = 2.0;
    }

    /// <summary>
    /// <b>Die Deckungsart einer Kältebedarfszeile</b> (Konzept Kältebedarf 3.4, 4.2; Entscheid E-K2) — für eine Lastgangzeile
    /// im Kanal Kühlung (<c>Z_ProjektWaermebedarf</c>) und eine Profilzeile (<c>Z_Projekt_Kaeltebedarf</c>) gleich:
    /// „zentral“ geht in die Kältefolge, „dezentral (Split)“ deckt die Zeile neben der Kaskade mit eigenem EER, ohne
    /// Kaltwasser und ohne Leistungsgrenze. Die Deckungsart greift nie in Gebäude oder Zonen.
    /// </summary>
    public sealed class Kaeltedeckung
    {
        /// <summary>Die Vorgabe jeder Zeile ohne Angabe: zentral.</summary>
        public static readonly Kaeltedeckung Zentral = new Kaeltedeckung();

        /// <summary>Bezeichner der Zeile für Meldungen.</summary>
        public string Bezeichner = "";

        /// <summary><c>true</c> = dezentral (Split); <c>false</c> = zentral (Kältefolge).</summary>
        public bool Split;

        /// <summary><c>true</c> = EER linear über der Außentemperatur; <c>false</c> = fest.</summary>
        public bool Linear;

        /// <summary>EER am Punkt 1 bzw. der feste EER.</summary>
        public double? Eer1;

        /// <summary>Außentemperatur am Punkt 1 [°C].</summary>
        public double? Taussen1;

        /// <summary>EER am Punkt 2 (nur linear).</summary>
        public double? Eer2;

        /// <summary>Außentemperatur am Punkt 2 [°C] (nur linear).</summary>
        public double? Taussen2;

        /// <summary>Kühlträger des Split-Stroms; <c>null</c> = Stromträger des Projekts.</summary>
        public int? KuehlIdCarrier;

        /// <summary>Eigener Zähler des Split-Stroms.</summary>
        public bool EigenerZaehler;

        /// <summary>
        /// Die ID der Zuordnungszeile (<c>Z_Projekt_Kaeltebedarf.ID</c> bzw. <c>Z_ProjektWaermebedarf.ID</c>) — die Ergebniszeile
        /// je Split-Zeile nennt sie (Schemaschritt 224); <c>null</c> ohne Zeile.
        /// </summary>
        public int? IdZuordnung;

        /// <summary>
        /// <c>true</c> = die Zeile ist Prozesskälte (KWN-0, <c>Kaelte_Art = PROZESS</c>): Sie bleibt im Kühlkanal und geht
        /// zusätzlich in den Unterposten <c>Kaeltebedarf_Prozess</c>. Unabhängig von der Deckungsart.
        /// </summary>
        public bool Prozess;

        /// <summary>Der Grund, warum die Split-Felder nicht rechnen (<c>null</c> = sie rechnen) — dieselbe Regel wie beim Speichern.</summary>
        public string Pruefgrund;

        /// <summary>Aus einer Profilzeile; <c>null</c> ergibt <see cref="Zentral"/>.</summary>
        public static Kaeltedeckung AusProfilzeile(Z_ProjektKaeltebedarfModel z)
        {
            if (z == null) return Zentral;
            bool prozess = KaelteArtSchema.IstProzess(z.KaelteArt);
            if (z.Deckung != KaeltebedarfSchema.DECKUNG_SPLIT)
                return prozess ? new Kaeltedeckung { Bezeichner = z.Bezeichner ?? "", Prozess = true } : Zentral;
            return new Kaeltedeckung
            {
                Bezeichner = z.Bezeichner ?? "",
                Prozess = prozess,
                Split = true,
                Linear = z.EerWeg == KaeltebedarfSchema.EER_LINEAR,
                Eer1 = z.Eer1, Taussen1 = z.Taussen1, Eer2 = z.Eer2, Taussen2 = z.Taussen2,
                KuehlIdCarrier = z.KuehlIdCarrier, EigenerZaehler = z.KuehlEigenerZaehler,
                IdZuordnung = z.ID_Z > 0 ? z.ID_Z : (int?)null,
                Pruefgrund = Z_ProjektKaeltebedarfCtrl.Deckungspruefung(z),
            };
        }

        /// <summary>Aus einer Lastgangzeile; außerhalb des Kanals Kühlung und ohne „split“ <see cref="Zentral"/>.</summary>
        public static Kaeltedeckung AusLastgang(Z_ProjWaermebedarfModel w)
        {
            if (w == null || !Z_ProjektKaeltebedarfCtrl.IstKuehlkanal(w)) return Zentral;
            bool prozess = KaelteArtSchema.IstProzess(w.KaelteArt);
            if (w.Deckung != KaeltebedarfSchema.DECKUNG_SPLIT)
                return prozess ? new Kaeltedeckung { Bezeichner = w.m_szBezeichner ?? "", Prozess = true } : Zentral;
            return new Kaeltedeckung
            {
                Bezeichner = w.m_szBezeichner ?? "",
                Prozess = prozess,
                Split = true,
                Linear = w.EerWeg == KaeltebedarfSchema.EER_LINEAR,
                Eer1 = w.Eer1, Taussen1 = w.Taussen1, Eer2 = w.Eer2, Taussen2 = w.Taussen2,
                KuehlIdCarrier = w.KuehlIdCarrier, EigenerZaehler = w.KuehlEigenerZaehler,
                IdZuordnung = w.m_ID_Z > 0 ? w.m_ID_Z : (int?)null,
                Pruefgrund = Z_ProjektKaeltebedarfCtrl.Deckungspruefung(w),
            };
        }

        /// <summary>
        /// Der EER bei der Außentemperatur <paramref name="theta"/> [°C] (Konzept 4.2): fest = <see cref="Eer1"/>; linear =
        /// EER₁ + (EER₂ − EER₁)·(θ − T₁)/(T₂ − T₁), außerhalb [min(T₁,T₂), max(T₁,T₂)] auf den Randwert geklemmt; nie unter
        /// <see cref="KaeltebedarfFestwerte.EER_MIN"/>. NaN, wenn die Felder nicht rechnen (<see cref="Pruefgrund"/>).
        /// </summary>
        public double Eer(double theta)
        {
            if (!Split || Pruefgrund != null || !(Eer1 > 0)) return double.NaN;
            double eer = Eer1.Value;
            if (Linear)
            {
                double t1 = Taussen1.Value, t2 = Taussen2.Value, e2 = Eer2.Value;
                double t = Math.Min(Math.Max(theta, Math.Min(t1, t2)), Math.Max(t1, t2));
                eer = Eer1.Value + (e2 - Eer1.Value) * (t - t1) / (t2 - t1);
            }
            return eer < KaeltebedarfFestwerte.EER_MIN ? KaeltebedarfFestwerte.EER_MIN : eer;
        }
    }
}
